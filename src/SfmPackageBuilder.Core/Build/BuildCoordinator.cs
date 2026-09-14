using SfmPackageBuilder.Core.Archive;
using SfmPackageBuilder.Core.Domain;
using SfmPackageBuilder.Core.FileSystem;
using SfmPackageBuilder.Core.Planning;
using SfmPackageBuilder.Core.Staging;
using SfmPackageBuilder.Core.Validation;

namespace SfmPackageBuilder.Core.Build;

public sealed class BuildCoordinator
{
    private readonly string applicationRoot;
    private readonly PackageCheckService packageCheckService;
    private readonly StagingBuilder stagingBuilder;
    private readonly ArchiveService archiveService;
    private readonly BuildHistoryCommitter buildHistoryCommitter;
    private readonly IStagingCleanupService cleanupService;
    private readonly StoragePreflightService storagePreflightService;
    private readonly Func<DateTimeOffset> clock;

    public BuildCoordinator(
        string applicationRoot,
        IFileSystem? fileSystem = null,
        IOutputEnvironment? outputEnvironment = null,
        Func<DateTimeOffset>? clock = null,
        IStorageSpaceProvider? storageSpaceProvider = null,
        PackageCheckService? packageCheckService = null,
        StagingBuilder? stagingBuilder = null,
        ArchiveService? archiveService = null,
        BuildHistoryCommitter? buildHistoryCommitter = null,
        IStagingCleanupService? cleanupService = null)
    {
        var resolvedFileSystem = fileSystem ?? new PhysicalFileSystem();
        var resolvedOutputEnvironment = outputEnvironment ?? new PhysicalOutputEnvironment();
        this.applicationRoot = Path.GetFullPath(applicationRoot);
        this.clock = clock ?? (() => DateTimeOffset.Now);
        this.packageCheckService = packageCheckService ?? new PackageCheckService(resolvedFileSystem, resolvedOutputEnvironment);
        this.stagingBuilder = stagingBuilder ?? new StagingBuilder(this.applicationRoot, resolvedFileSystem);
        this.archiveService = archiveService ?? new ArchiveService();
        this.buildHistoryCommitter = buildHistoryCommitter ?? new BuildHistoryCommitter(this.clock);
        this.cleanupService = cleanupService ?? new StagingCleanupService();
        storagePreflightService = new StoragePreflightService(resolvedFileSystem, storageSpaceProvider ?? new PhysicalStorageSpaceProvider());
    }

    public PackagePlan PreviewPackage(PackageProject project) =>
        packageCheckService.PreviewPackage(project);

    public ReadmePreviewResult PreviewReadme(PackageProject project) =>
        packageCheckService.PreviewReadme(project);

    public PackageCheckResult CheckPackage(PackageProject project, string outputDirectory) =>
        packageCheckService.CheckPackage(project, outputDirectory);

    public BuildSummary Build(BuildRequest request)
    {
        return BuildAsync(request).GetAwaiter().GetResult();
    }

    public async Task<BuildSummary> BuildAsync(
        BuildRequest request,
        IProgress<BuildProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Project);

        var timestamp = clock();
        var log = new BuildLogBuilder(timestamp, request.Project.AssetName, request.Project.CurrentVersion);
        var resolvedDecisions = new List<string>();
        var archivePath = Path.Combine(request.OutputDirectory, request.Project.ArchiveName);

        log.Add("start", "Build started.", $"Asset: {request.Project.AssetName}; Version: {request.Project.CurrentVersion}");
        progress?.Report(new BuildProgress(BuildProgressPhase.Checking));
        var check = packageCheckService.CheckPackage(request.Project, request.OutputDirectory);
        log.Add("planning", "Fresh package plan created.", $"Entries: {check.Plan.Entries.Count}");
        log.Add("validation", "Package validation completed.", CreateValidationDetail(check.Validation));

        if (check.Validation.HasErrors)
        {
            log.Add("validation", "Build stopped because validation contains errors.");
            return Summary(
                BuildStatus.ValidationFailed,
                request,
                archivePath,
                check.Plan,
                check.Validation,
                null,
                null,
                null,
                null,
                check.Validation.RequiredDecisions,
                resolvedDecisions,
                log);
        }

        var decisionStatus = ResolveRequiredDecisions(check.Validation.RequiredDecisions, request, resolvedDecisions, log);
        if (decisionStatus is not null)
        {
            return Summary(
                decisionStatus.Value,
                request,
                archivePath,
                check.Plan,
                check.Validation,
                null,
                null,
                null,
                null,
                check.Validation.RequiredDecisions,
                resolvedDecisions,
                log);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            log.Add("cancelled", "Build cancelled before staging.");
            return Summary(
                BuildStatus.Cancelled,
                request,
                archivePath,
                check.Plan,
                check.Validation,
                null,
                null,
                null,
                null,
                Array.Empty<RequiredDecision>(),
                resolvedDecisions,
                log);
        }

        var storage = storagePreflightService.Check(
            check.Plan,
            Path.Combine(applicationRoot, ".staging", "preflight"),
            archivePath);
        if (!storage.Succeeded)
        {
            log.Add("storage", storage.Message ?? "Storage preflight failed.", storage.TechnicalDetail);
            return Summary(
                BuildStatus.StorageFailed,
                request,
                archivePath,
                check.Plan,
                check.Validation,
                null,
                null,
                null,
                null,
                Array.Empty<RequiredDecision>(),
                resolvedDecisions,
                log);
        }

        progress?.Report(new BuildProgress(BuildProgressPhase.PreparingFiles, 0, check.Plan.ResolvedEntries.Count));
        var staging = await stagingBuilder.StageAsync(check.Plan, progress, cancellationToken).ConfigureAwait(false);
        if (staging.Cancelled)
        {
            var cancelledCleanup = staging.CancelledSession is null
                ? null
                : cleanupService.CleanCancelledStaging(staging.CancelledSession);
            log.Add("cancelled", "Build cancelled during staging.", cancelledCleanup?.TechnicalDetail);
            return Summary(
                BuildStatus.Cancelled,
                request,
                archivePath,
                check.Plan,
                check.Validation,
                staging,
                null,
                null,
                cancelledCleanup,
                Array.Empty<RequiredDecision>(),
                resolvedDecisions,
                log);
        }

        if (!staging.Succeeded)
        {
            log.Add("staging", "Staging failed.", staging.Failure?.TechnicalDetail ?? staging.Failure?.Message);
            return Summary(
                BuildStatus.StagingFailed,
                request,
                archivePath,
                check.Plan,
                check.Validation,
                staging,
                null,
                null,
                null,
                Array.Empty<RequiredDecision>(),
                resolvedDecisions,
                log);
        }

        log.Add(
            "staging",
            "Staging completed.",
            $"Root: {staging.Session!.StagingRoot}; Files: {staging.Session.Files.Count}; Duplicates skipped: {staging.Session.SkippedDuplicateWrites.Count}");

        ArchiveResult archive;
        try
        {
            progress?.Report(new BuildProgress(BuildProgressPhase.CreatingArchive, 0, staging.Session.Files.Count));
            archive = await archiveService.CreateArchiveAsync(
                staging.Session,
                archivePath,
                ToArchiveDecision(request.ExistingOutputDecision),
                progress,
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            var cancelledCleanup = cleanupService.CleanCancelledStaging(staging.Session);
            log.Add("cancelled", "Build cancelled.", cancelledCleanup.TechnicalDetail);
            return Summary(
                BuildStatus.Cancelled,
                request,
                archivePath,
                check.Plan,
                check.Validation,
                staging,
                null,
                null,
                cancelledCleanup,
                Array.Empty<RequiredDecision>(),
                resolvedDecisions,
                log);
        }

        if (!archive.Succeeded)
        {
            log.Add("archive", "Archive creation failed.", archive.Failure?.TechnicalDetail ?? archive.Failure?.Message);
            return Summary(
                BuildStatus.ArchiveFailed,
                request,
                archivePath,
                check.Plan,
                check.Validation,
                staging,
                archive,
                null,
                null,
                Array.Empty<RequiredDecision>(),
                resolvedDecisions,
                log);
        }

        log.Add(
            "archive",
            "Archive created and verified.",
            $"Target: {archive.ArchivePath}; Replacement: {archive.ReplacementBehavior}; Verification: {archive.Verification?.Succeeded}");

        progress?.Report(new BuildProgress(BuildProgressPhase.Finishing));
        var history = buildHistoryCommitter.Commit(request.Project);
        log.Add("history", "Build history committed.", $"Version: {history.Record.Version}; Archive: {history.Record.ArchiveName}");

        var cleanup = cleanupService.CleanSuccessfulStaging(staging.Session);
        log.Add(
            "cleanup",
            cleanup.Succeeded ? "Successful staging cleaned." : "Successful staging cleanup did not complete.",
            cleanup.TechnicalDetail ?? cleanup.Status.ToString());

        return Summary(
            BuildStatus.Succeeded,
            request,
            archive.ArchivePath,
            check.Plan,
            check.Validation,
            staging,
            archive,
            history,
            cleanup,
            Array.Empty<RequiredDecision>(),
            resolvedDecisions,
            log);
    }

    private static BuildStatus? ResolveRequiredDecisions(
        IReadOnlyList<RequiredDecision> requiredDecisions,
        BuildRequest request,
        List<string> resolvedDecisions,
        BuildLogBuilder log)
    {
        foreach (var decision in requiredDecisions)
        {
            if (decision.Kind == RequiredDecisionKind.VersionReuse)
            {
                var status = ResolveVersionDecision(request.VersionReuseDecision, resolvedDecisions, log);
                if (status is not null)
                {
                    return status;
                }
            }
            else if (decision.Kind == RequiredDecisionKind.ExistingOutputArchive)
            {
                var status = ResolveOutputDecision(request.ExistingOutputDecision, resolvedDecisions, log);
                if (status is not null)
                {
                    return status;
                }
            }
        }

        return null;
    }

    private static BuildStatus? ResolveVersionDecision(
        VersionReuseDecision decision,
        List<string> resolvedDecisions,
        BuildLogBuilder log)
    {
        return decision switch
        {
            VersionReuseDecision.RebuildExistingVersion => Resolve("version-reuse:RebuildExistingVersion", resolvedDecisions, log),
            VersionReuseDecision.ChangeVersion => Stop(BuildStatus.ChangeRequired, "version-reuse:ChangeVersion", resolvedDecisions, log),
            VersionReuseDecision.Cancel => Stop(BuildStatus.Cancelled, "version-reuse:Cancel", resolvedDecisions, log),
            _ => Stop(BuildStatus.DecisionRequired, "version-reuse:DecisionRequired", resolvedDecisions, log)
        };
    }

    private static BuildStatus? ResolveOutputDecision(
        ExistingOutputDecision decision,
        List<string> resolvedDecisions,
        BuildLogBuilder log)
    {
        return decision switch
        {
            ExistingOutputDecision.Replace => Resolve("existing-output:Replace", resolvedDecisions, log),
            ExistingOutputDecision.ChooseAnotherName => Stop(BuildStatus.ChangeRequired, "existing-output:ChooseAnotherName", resolvedDecisions, log),
            ExistingOutputDecision.Cancel => Stop(BuildStatus.Cancelled, "existing-output:Cancel", resolvedDecisions, log),
            _ => Stop(BuildStatus.DecisionRequired, "existing-output:DecisionRequired", resolvedDecisions, log)
        };
    }

    private static BuildStatus? Resolve(string decision, List<string> resolvedDecisions, BuildLogBuilder log)
    {
        resolvedDecisions.Add(decision);
        log.Add("decision", "Required decision resolved.", decision);
        return null;
    }

    private static BuildStatus Stop(BuildStatus status, string decision, List<string> resolvedDecisions, BuildLogBuilder log)
    {
        resolvedDecisions.Add(decision);
        log.Add("decision", "Build stopped by required decision state.", decision);
        return status;
    }

    private static ExistingOutputDecision ToArchiveDecision(ExistingOutputDecision decision) =>
        decision == ExistingOutputDecision.Replace ? ExistingOutputDecision.Replace : ExistingOutputDecision.None;

    private static string CreateValidationDetail(ValidationResult validation)
    {
        var messages = validation.Messages.Select(message => $"{message.Severity}:{message.Code}");
        var decisions = validation.RequiredDecisions.Select(decision => $"Decision:{decision.Kind}:{decision.Code}");
        return string.Join(Environment.NewLine, messages.Concat(decisions));
    }

    private static BuildSummary Summary(
        BuildStatus status,
        BuildRequest request,
        string? archivePath,
        PackagePlan? plan,
        ValidationResult? validation,
        StagingResult? staging,
        ArchiveResult? archive,
        BuildHistoryCommitResult? history,
        StagingCleanupResult? cleanup,
        IReadOnlyList<RequiredDecision> requiredDecisions,
        IReadOnlyList<string> resolvedDecisions,
        BuildLogBuilder log) =>
        new(
            status,
            request.Project.CurrentVersion,
            archivePath,
            plan,
            validation,
            staging,
            archive,
            history,
            cleanup,
            requiredDecisions,
            resolvedDecisions,
            log.Build());
}
