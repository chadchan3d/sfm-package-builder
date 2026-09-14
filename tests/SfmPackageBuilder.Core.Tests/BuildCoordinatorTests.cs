using System.IO.Compression;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SfmPackageBuilder.Core.Archive;
using SfmPackageBuilder.Core.Build;
using SfmPackageBuilder.Core.Domain;
using SfmPackageBuilder.Core.Expansion;
using SfmPackageBuilder.Core.FileSystem;
using SfmPackageBuilder.Core.Planning;
using SfmPackageBuilder.Core.Readme;
using SfmPackageBuilder.Core.SharedFiles;
using SfmPackageBuilder.Core.Staging;
using SfmPackageBuilder.Core.Validation;

namespace SfmPackageBuilder.Core.Tests;

[TestClass]
public sealed class BuildCoordinatorTests
{
    private static readonly DateTimeOffset FixedTimestamp = new(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void PreviewCheckAndReadmePreviewUseFreshPlannerWithoutStagingOrArchive()
    {
        using var workspace = BuildWorkspace.Create();
        workspace.CreateStandardSources();
        var project = workspace.CreateProject();
        var coordinator = workspace.CreateCoordinator();

        var preview = coordinator.PreviewPackage(project);
        var check = coordinator.CheckPackage(project, workspace.OutputRoot);
        var readme = coordinator.PreviewReadme(project);

        Assert.IsTrue(preview.Entries.Any(entry => entry.DestinationRelativePath == @"models\Creator\chair_dev\chair_release.mdl"));
        Assert.AreEqual(preview.Entries.Count, check.Plan.Entries.Count);
        Assert.IsFalse(check.Validation.HasErrors, string.Join("|", check.Validation.Messages.Select(message => message.Code)));
        Assert.IsTrue(readme.HasReadme);
        Assert.AreEqual("README.txt", readme.ReadmeEntry!.DestinationRelativePath);
        Assert.IsFalse(Directory.Exists(Path.Combine(workspace.ApplicationRoot, ".staging")));
    }

    [TestMethod]
    public void BuildPlansValidatesStagesArchivesCommitsHistoryAndCleansStaging()
    {
        using var workspace = BuildWorkspace.Create();
        workspace.CreateStandardSources(includeCompanions: true);
        var project = workspace.CreateProject(changes: new[] { "Initial release." });
        var before = SourceSnapshot.CaptureDirectory(workspace.SourceRoot);

        var summary = workspace.CreateCoordinator().Build(workspace.Request(project));

        Assert.IsTrue(summary.Succeeded, summary.LogText());
        Assert.AreEqual(BuildStatus.Succeeded, summary.Status);
        Assert.IsTrue(File.Exists(summary.ArchivePath!));
        var plannedDestinations = summary.Plan!.ResolvedEntries
            .Select(entry => entry.DestinationRelativePath!.Replace('\\', '/'))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var stagedDestinations = summary.StagingResult!.Session!.Files
            .Select(file => file.DestinationRelativePath.Replace('\\', '/'))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        AssertArchiveEntries(
            summary.ArchivePath!,
            @"materials/models/Creator/chair/body.vtf",
            @"materials/models/Creator/chair/chair.vmt",
            @"models/Creator/chair_dev/chair_release.dx90.vtx",
            @"models/Creator/chair_dev/chair_release.mdl",
            @"models/Creator/chair_dev/chair_release.phy",
            @"models/Creator/chair_dev/chair_release.sw.vtx",
            @"models/Creator/chair_dev/chair_release.vvd",
            "README.txt");
        CollectionAssert.AreEqual(plannedDestinations, stagedDestinations);
        AssertArchiveEntries(summary.ArchivePath!, plannedDestinations);
        Assert.AreEqual(0, project.ReleaseHistory.Count);
        Assert.AreEqual(1, project.BuildHistory.Count);
        Assert.AreEqual(project.CurrentVersion, summary.BuildHistoryCommit!.Record.Version);
        Assert.AreEqual(project.ArchiveName, summary.BuildHistoryCommit.Record.ArchiveName);
        Assert.AreEqual(FixedTimestamp, summary.BuildHistoryCommit.Record.BuildTimestamp);
        Assert.AreEqual(FixedTimestamp, project.BuildHistory[0].BuildTimestamp);
        Assert.IsFalse(Directory.Exists(summary.StagingResult!.Session!.StagingRoot));
        before.AssertUnchanged(workspace.SourceRoot);
    }

    [TestMethod]
    public void BuildArchivesRenamedAdditionalModelFamilyWithoutMutatingSources()
    {
        using var workspace = BuildWorkspace.Create();
        workspace.CreateStandardSources(includeCompanions: true);
        var additionalMdl = workspace.WriteSource(@"game\usermod\models\Creator\gwen\worgen_tail.mdl", "tail-mdl");
        var additionalVvd = workspace.WriteSource(@"game\usermod\models\Creator\gwen\worgen_tail.vvd", "tail-vvd");
        var additionalDx90 = workspace.WriteSource(@"game\usermod\models\Creator\gwen\worgen_tail.dx90.vtx", "tail-dx90");
        var before = SourceSnapshot.CaptureDirectory(workspace.SourceRoot);
        var project = workspace.CreateProject(changes: new[] { "Additional model rename." });
        project.Models.Add(new ModelEntry
        {
            Role = ModelRole.Additional,
            SourceMdlPath = additionalMdl,
            SourceStem = "worgen_tail",
            ReleaseStem = "Gwen_Worgen_Tail"
        });

        var summary = workspace.CreateCoordinator().Build(workspace.Request(project));

        Assert.IsTrue(summary.Succeeded, summary.LogText());
        AssertArchiveContainsEntries(
            summary.ArchivePath!,
            @"models/Creator/gwen/Gwen_Worgen_Tail.dx90.vtx",
            @"models/Creator/gwen/Gwen_Worgen_Tail.mdl",
            @"models/Creator/gwen/Gwen_Worgen_Tail.vvd");
        AssertArchiveDoesNotContain(
            summary.ArchivePath!,
            @"models/Creator/gwen/worgen_tail.dx90.vtx",
            @"models/Creator/gwen/worgen_tail.mdl",
            @"models/Creator/gwen/worgen_tail.vvd");
        var readme = ReadArchiveText(summary.ArchivePath!, "README.txt");
        StringAssert.Contains(readme, @"models\Creator\gwen\Gwen_Worgen_Tail.mdl");
        Assert.IsFalse(readme.Contains(@"models\Creator\gwen\worgen_tail.mdl", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(File.Exists(additionalMdl));
        Assert.IsTrue(File.Exists(additionalVvd));
        Assert.IsTrue(File.Exists(additionalDx90));
        before.AssertUnchanged(workspace.SourceRoot);
    }

    [TestMethod]
    public void BuildReadsCurrentSourceBytesFromDisk()
    {
        using var workspace = BuildWorkspace.Create();
        workspace.CreateStandardSources();
        var project = workspace.CreateProject();
        var materialPath = Path.Combine(workspace.SourceRoot, @"game\usermod\materials\models\Creator\chair\body.vtf");
        File.WriteAllText(materialPath, "updated-current-bytes", Encoding.UTF8);

        var summary = workspace.CreateCoordinator().Build(workspace.Request(project));

        Assert.IsTrue(summary.Succeeded, summary.LogText());
        Assert.AreEqual(
            "updated-current-bytes",
            ReadArchiveText(summary.ArchivePath!, "materials/models/Creator/chair/body.vtf"));
    }

    [TestMethod]
    public async Task BuildAsyncReportsOrderedProgressAndCreatesVerifiedArchive()
    {
        using var workspace = BuildWorkspace.Create();
        workspace.CreateStandardSources();
        var project = workspace.CreateProject();
        var phases = new List<BuildProgressPhase>();
        var progress = new ActionProgress(reported =>
        {
            if (phases.Count == 0 || phases.Last() != reported.Phase)
            {
                phases.Add(reported.Phase);
            }

            if (reported.Completed is not null && reported.Total is not null)
            {
                Assert.IsTrue(reported.Completed <= reported.Total);
            }
        });

        var summary = await workspace.CreateCoordinator().BuildAsync(workspace.Request(project), progress);

        Assert.IsTrue(summary.Succeeded, summary.LogText());
        CollectionAssert.Contains(phases, BuildProgressPhase.Checking);
        CollectionAssert.Contains(phases, BuildProgressPhase.PreparingFiles);
        CollectionAssert.Contains(phases, BuildProgressPhase.CreatingArchive);
        CollectionAssert.Contains(phases, BuildProgressPhase.VerifyingArchive);
        Assert.AreEqual(BuildProgressPhase.Finishing, phases.Last());
    }

    [TestMethod]
    public async Task CancellationBeforeStagingReturnsCancelledWithoutHistoryArchiveOrStaging()
    {
        using var workspace = BuildWorkspace.Create();
        workspace.CreateStandardSources();
        var project = workspace.CreateProject();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var summary = await workspace.CreateCoordinator().BuildAsync(workspace.Request(project), cancellationToken: cancellation.Token);

        Assert.AreEqual(BuildStatus.Cancelled, summary.Status);
        Assert.AreEqual(0, project.BuildHistory.Count);
        Assert.IsFalse(File.Exists(Path.Combine(workspace.OutputRoot, project.ArchiveName)));
        Assert.IsFalse(Directory.Exists(Path.Combine(workspace.ApplicationRoot, ".staging")));
    }

    [TestMethod]
    public async Task CancellationDuringStagingCleansStagingAndDoesNotCreateBuildHistory()
    {
        using var workspace = BuildWorkspace.Create();
        workspace.CreateStandardSources();
        var project = workspace.CreateProject();
        using var cancellation = new CancellationTokenSource();
        var progress = new ActionProgress(reported =>
        {
            if (reported.Phase == BuildProgressPhase.PreparingFiles)
            {
                cancellation.Cancel();
            }
        });

        var summary = await workspace.CreateCoordinator().BuildAsync(workspace.Request(project), progress, cancellation.Token);

        Assert.AreEqual(BuildStatus.Cancelled, summary.Status);
        Assert.AreEqual(0, project.BuildHistory.Count);
        Assert.IsFalse(File.Exists(Path.Combine(workspace.OutputRoot, project.ArchiveName)));
        Assert.IsNotNull(summary.CleanupResult);
        Assert.IsFalse(Directory.Exists(summary.CleanupResult!.StagingRoot));
    }

    [TestMethod]
    public async Task CancellationDuringZipCreationCleansPartialArchiveAndLeavesExistingFinalUntouched()
    {
        using var workspace = BuildWorkspace.Create();
        workspace.CreateStandardSources();
        var project = workspace.CreateProject();
        var archivePath = Path.Combine(workspace.OutputRoot, project.ArchiveName);
        CreateZip(archivePath, ("old.txt", "old"));
        var before = File.ReadAllBytes(archivePath);
        using var cancellation = new CancellationTokenSource();
        var progress = new ActionProgress(reported =>
        {
            if (reported.Phase == BuildProgressPhase.CreatingArchive)
            {
                cancellation.Cancel();
            }
        });

        var summary = await workspace.CreateCoordinator().BuildAsync(
            workspace.Request(project, outputDecision: ExistingOutputDecision.Replace),
            progress,
            cancellation.Token);

        Assert.AreEqual(BuildStatus.Cancelled, summary.Status);
        Assert.AreEqual(0, project.BuildHistory.Count);
        CollectionAssert.AreEqual(before, File.ReadAllBytes(archivePath));
        Assert.IsFalse(Directory.EnumerateFiles(workspace.OutputRoot, ".*.sfmpack-*.tmp.zip").Any());
        Assert.IsFalse(Directory.Exists(summary.CleanupResult!.StagingRoot));
    }

    [TestMethod]
    public async Task CancellationDuringVerificationCleansTemporaryArchiveAndLeavesExistingFinalUntouched()
    {
        using var workspace = BuildWorkspace.Create();
        workspace.CreateStandardSources();
        var project = workspace.CreateProject();
        var archivePath = Path.Combine(workspace.OutputRoot, project.ArchiveName);
        CreateZip(archivePath, ("old.txt", "old"));
        var before = File.ReadAllBytes(archivePath);
        using var cancellation = new CancellationTokenSource();
        var progress = new ActionProgress(reported =>
        {
            if (reported.Phase == BuildProgressPhase.VerifyingArchive)
            {
                cancellation.Cancel();
            }
        });

        var summary = await workspace.CreateCoordinator().BuildAsync(
            workspace.Request(project, outputDecision: ExistingOutputDecision.Replace),
            progress,
            cancellation.Token);

        Assert.AreEqual(BuildStatus.Cancelled, summary.Status);
        CollectionAssert.AreEqual(before, File.ReadAllBytes(archivePath));
        Assert.AreEqual(0, project.BuildHistory.Count);
        Assert.IsFalse(Directory.EnumerateFiles(workspace.OutputRoot, ".*.sfmpack-*.tmp.zip").Any());
        Assert.IsFalse(Directory.Exists(summary.CleanupResult!.StagingRoot));
    }

    [TestMethod]
    public async Task CancellationAfterCommitDoesNotUndoSuccessfulZip()
    {
        using var workspace = BuildWorkspace.Create();
        workspace.CreateStandardSources();
        var project = workspace.CreateProject();
        using var cancellation = new CancellationTokenSource();
        var progress = new ActionProgress(reported =>
        {
            if (reported.Phase == BuildProgressPhase.Finishing)
            {
                cancellation.Cancel();
            }
        });

        var summary = await workspace.CreateCoordinator().BuildAsync(workspace.Request(project), progress, cancellation.Token);

        Assert.IsTrue(summary.Succeeded, summary.LogText());
        Assert.IsTrue(File.Exists(summary.ArchivePath!));
        Assert.AreEqual(1, project.BuildHistory.Count);
    }

    [TestMethod]
    public void StoragePreflightBlocksClearlyInsufficientSpaceBeforeStaging()
    {
        using var workspace = BuildWorkspace.Create();
        workspace.CreateStandardSources();
        var project = workspace.CreateProject();

        var summary = workspace.CreateCoordinator(storageSpaceProvider: new FixedStorageSpaceProvider(1)).Build(workspace.Request(project));

        Assert.AreEqual(BuildStatus.StorageFailed, summary.Status);
        Assert.IsNull(summary.StagingResult);
        Assert.IsFalse(File.Exists(Path.Combine(workspace.OutputRoot, project.ArchiveName)));
        Assert.IsTrue(summary.Log.Entries.Any(entry => entry.Stage == "storage"));
    }

    [TestMethod]
    public void UnknownStorageSpaceDoesNotBlockBuild()
    {
        using var workspace = BuildWorkspace.Create();
        workspace.CreateStandardSources();
        var project = workspace.CreateProject();

        var summary = workspace.CreateCoordinator(storageSpaceProvider: new UnknownStorageSpaceProvider()).Build(workspace.Request(project));

        Assert.IsTrue(summary.Succeeded, summary.LogText());
        Assert.IsTrue(File.Exists(summary.ArchivePath!));
    }

    [TestMethod]
    public void GeneratedReadmePreviewReviewAndBuiltZipUseSameCurrentInstallRoots()
    {
        using var workspace = BuildWorkspace.Create();
        workspace.CreateStandardSources();
        var project = workspace.CreateProject(changes: new[] { "Added rig script." });
        project.Extras.Add(new SourceEntry
        {
            Kind = SourceEntryKind.Extra,
            SourcePath = workspace.WriteSource(@"extras\rig_test.py", "rig"),
            IsFolder = false,
            DestinationOverride = new DestinationOverride { Kind = DestinationOverrideKind.Custom, RelativePath = @"scripts\sfm\animset" }
        });
        var coordinator = workspace.CreateCoordinator();

        var preview = coordinator.PreviewReadme(project);
        var reviewPlan = coordinator.PreviewPackage(project);
        var reviewReadme = reviewPlan.Entries.Single(entry => entry.EntryType == PackagePlanEntryType.Readme);
        var summary = coordinator.Build(workspace.Request(project));

        Assert.IsTrue(summary.Succeeded, summary.LogText());
        StringAssert.Contains(preview.PreviewText!, "- scripts");
        Assert.IsFalse(preview.PreviewText!.Contains("- scripts\\", StringComparison.Ordinal));
        StringAssert.Contains(preview.PreviewText!, ReadmeGenerator.UpdateInstallSentence);
        Assert.AreEqual(preview.PreviewText, reviewReadme.TextContent);
        Assert.AreEqual(preview.PreviewText, ReadArchiveText(summary.ArchivePath!, "README.txt"));
    }

    [TestMethod]
    public void GeneratedReadmeReflectsExtrasAddedAfterEarlierPreview()
    {
        using var workspace = BuildWorkspace.Create();
        workspace.CreateStandardSources();
        var project = workspace.CreateProject();
        var coordinator = workspace.CreateCoordinator();

        var before = coordinator.PreviewReadme(project).PreviewText!;
        project.Extras.Add(new SourceEntry
        {
            Kind = SourceEntryKind.Extra,
            SourcePath = workspace.WriteSource(@"extras\rig_test.py", "rig"),
            IsFolder = false,
            DestinationOverride = new DestinationOverride { Kind = DestinationOverrideKind.Custom, RelativePath = @"scripts\sfm\animset" }
        });
        var after = coordinator.PreviewReadme(project).PreviewText!;

        Assert.IsFalse(before.Contains("- scripts", StringComparison.Ordinal));
        StringAssert.Contains(after, "- scripts");
        Assert.IsFalse(after.Contains("- scripts\\", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ValidationErrorPreventsStagingArchiveAndHistoryMutation()
    {
        using var workspace = BuildWorkspace.Create();
        workspace.CreateStandardSources();
        var project = workspace.CreateProject();
        project.Models[0].ReleaseStem = "CON";
        var before = SourceSnapshot.CaptureDirectory(workspace.SourceRoot);

        var summary = workspace.CreateCoordinator().Build(workspace.Request(project));

        Assert.AreEqual(BuildStatus.ValidationFailed, summary.Status);
        Assert.IsTrue(summary.Validation!.HasErrors);
        Assert.IsNull(summary.StagingResult);
        Assert.IsNull(summary.ArchiveResult);
        Assert.AreEqual(0, project.ReleaseHistory.Count);
        Assert.AreEqual(0, project.BuildHistory.Count);
        before.AssertUnchanged(workspace.SourceRoot);
    }

    [TestMethod]
    public void WarningAndInformationAlonePermitBuild()
    {
        using var warningWorkspace = BuildWorkspace.Create();
        warningWorkspace.CreateStandardSources();
        var warningProject = warningWorkspace.CreateProject();
        warningProject.Extras.Add(new SourceEntry
        {
            Kind = SourceEntryKind.Extra,
            SourcePath = warningWorkspace.WriteSource(@"extras\live.txt", "live"),
            IsFolder = false,
            DestinationOverride = new DestinationOverride { Kind = DestinationOverrideKind.Misc, RelativePath = string.Empty }
        });
        var warningRule = new SharedFileRule("live", "live.txt", new[] { @"Misc\live.txt" }, "Live", "Live warning.");
        var warningSummary = warningWorkspace.CreateCoordinator(
            sharedFileRegistry: new SharedFileRegistry(new[] { warningRule }))
            .Build(warningWorkspace.Request(warningProject));

        using var infoWorkspace = BuildWorkspace.Create();
        infoWorkspace.CreateStandardSources();
        var infoProject = infoWorkspace.CreateProject();
        infoProject.Readme.Mode = ReadmeMode.None;
        var infoSummary = infoWorkspace.CreateCoordinator()
            .Build(infoWorkspace.Request(infoProject));

        Assert.IsTrue(warningSummary.Succeeded, warningSummary.LogText());
        Assert.IsTrue(warningSummary.Validation!.Messages.Any(message => message.Severity == ValidationSeverity.Warning));
        Assert.IsTrue(infoSummary.Succeeded, infoSummary.LogText());
        Assert.IsTrue(infoSummary.Validation!.Messages.Any(message => message.Severity == ValidationSeverity.Information));
    }

    [TestMethod]
    public void RequiredVersionDecisionsBlockOrProceedAsRequested()
    {
        using var workspace = BuildWorkspace.Create();
        workspace.CreateStandardSources();
        var project = workspace.CreateProject();
        project.ReleaseHistory.Add(new ReleaseRecord { Version = project.CurrentVersion, Changes = new List<string> { "Old" } });
        project.BuildHistory.Add(new BuildRecord { Version = project.CurrentVersion, ArchiveName = "previous.zip", BuildTimestamp = FixedTimestamp.AddDays(-1) });
        var before = SourceSnapshot.CaptureDirectory(workspace.SourceRoot);

        var noDecision = workspace.CreateCoordinator().Build(workspace.Request(project));
        var change = workspace.CreateCoordinator().Build(workspace.Request(project, versionDecision: VersionReuseDecision.ChangeVersion));
        var cancel = workspace.CreateCoordinator().Build(workspace.Request(project, versionDecision: VersionReuseDecision.Cancel));
        before.AssertUnchanged(workspace.SourceRoot);
        var rebuild = workspace.CreateCoordinator().Build(workspace.Request(project, versionDecision: VersionReuseDecision.RebuildExistingVersion));

        Assert.AreEqual(BuildStatus.DecisionRequired, noDecision.Status);
        Assert.AreEqual(BuildStatus.ChangeRequired, change.Status);
        Assert.AreEqual(BuildStatus.Cancelled, cancel.Status);
        Assert.IsTrue(rebuild.Succeeded, rebuild.LogText());
        Assert.AreEqual(1, project.ReleaseHistory.Count);
        Assert.AreEqual("Old", project.ReleaseHistory[0].Changes[0]);
        Assert.AreEqual(2, project.BuildHistory.Count);
        Assert.AreEqual(project.ArchiveName, rebuild.BuildHistoryCommit!.Record.ArchiveName);
        Assert.IsFalse(Directory.Exists(Path.Combine(workspace.ApplicationRoot, ".staging", noDecision.StagingResult?.Session?.SessionId.ToString("N") ?? "none")));
        before.AssertUnchanged(workspace.SourceRoot);
    }

    [TestMethod]
    public void ExistingArchiveDecisionsBlockCancelOrReplaceExplicitly()
    {
        using var workspace = BuildWorkspace.Create();
        workspace.CreateStandardSources();
        var project = workspace.CreateProject();
        var archivePath = Path.Combine(workspace.OutputRoot, project.ArchiveName);
        CreateZip(archivePath, ("old.txt", "old"));
        var before = File.ReadAllBytes(archivePath);

        var noDecision = workspace.CreateCoordinator().Build(workspace.Request(project));
        var choose = workspace.CreateCoordinator().Build(workspace.Request(project, outputDecision: ExistingOutputDecision.ChooseAnotherName));
        var cancel = workspace.CreateCoordinator().Build(workspace.Request(project, outputDecision: ExistingOutputDecision.Cancel));

        Assert.AreEqual(BuildStatus.DecisionRequired, noDecision.Status);
        Assert.AreEqual(BuildStatus.ChangeRequired, choose.Status);
        Assert.AreEqual(BuildStatus.Cancelled, cancel.Status);
        CollectionAssert.AreEqual(before, File.ReadAllBytes(archivePath));
        var replace = workspace.CreateCoordinator().Build(workspace.Request(project, outputDecision: ExistingOutputDecision.Replace));

        Assert.IsTrue(replace.Succeeded, replace.LogText());
        Assert.AreEqual(ArchiveReplacementBehavior.ReplacedExisting, replace.ArchiveResult!.ReplacementBehavior);
        Assert.IsTrue(replace.ResolvedDecisions.Contains("existing-output:Replace"));
        Assert.AreEqual(1, project.BuildHistory.Count);
        AssertArchiveEntries(archivePath, @"materials/models/Creator/chair/body.vtf", @"materials/models/Creator/chair/chair.vmt", @"models/Creator/chair_dev/chair_release.mdl", "README.txt");
    }

    [TestMethod]
    public void StaleDecisionCannotBypassCurrentFreshValidation()
    {
        using var workspace = BuildWorkspace.Create();
        workspace.CreateStandardSources();
        var project = workspace.CreateProject();
        var check = workspace.CreateCoordinator().CheckPackage(project, workspace.OutputRoot);
        Assert.IsFalse(check.Validation.HasErrors);
        project.Models[0].ReleaseStem = "bad/name";

        var summary = workspace.CreateCoordinator().Build(workspace.Request(project, outputDecision: ExistingOutputDecision.Replace));

        Assert.AreEqual(BuildStatus.ValidationFailed, summary.Status);
        Assert.IsNull(summary.StagingResult);
        Assert.IsTrue(summary.Validation!.Messages.Any(message => message.Code == "invalid-release-model-name"));
    }

    [TestMethod]
    public void StagingFailurePreventsArchiveAndHistoryAndRetainsFailedStaging()
    {
        using var workspace = BuildWorkspace.Create();
        workspace.CreateStandardSources();
        var project = workspace.CreateProject();
        var fileSystem = new CopyFailingFileSystem();
        var before = SourceSnapshot.CaptureDirectory(workspace.SourceRoot);

        var summary = workspace.CreateCoordinator(fileSystem: fileSystem).Build(workspace.Request(project));

        Assert.AreEqual(BuildStatus.StagingFailed, summary.Status);
        Assert.IsNull(summary.ArchiveResult);
        Assert.AreEqual(0, project.ReleaseHistory.Count);
        Assert.AreEqual(0, project.BuildHistory.Count);
        Assert.IsTrue(Directory.Exists(summary.StagingResult!.Failure!.StagingRoot));
        before.AssertUnchanged(workspace.SourceRoot);
    }

    [TestMethod]
    public void ArchiveFailureRetainsStagingAndDoesNotCommitHistory()
    {
        using var workspace = BuildWorkspace.Create();
        workspace.CreateStandardSources();
        var project = workspace.CreateProject();
        var before = SourceSnapshot.CaptureDirectory(workspace.SourceRoot);

        var summary = workspace.CreateCoordinator(archiveService: new ArchiveService(new ThrowingZipArchiveWriter())).Build(workspace.Request(project));

        Assert.AreEqual(BuildStatus.ArchiveFailed, summary.Status);
        Assert.IsTrue(Directory.Exists(summary.StagingResult!.Session!.StagingRoot));
        Assert.AreEqual(0, project.ReleaseHistory.Count);
        Assert.AreEqual(0, project.BuildHistory.Count);
        before.AssertUnchanged(workspace.SourceRoot);
    }

    [TestMethod]
    public void MidBuildDiskFullDuringStagingReturnsControlledFailureAndLeavesSourceUntouched()
    {
        using var workspace = BuildWorkspace.Create();
        workspace.CreateStandardSources();
        var project = workspace.CreateProject();
        var fileSystem = new DiskFullFileSystem();
        var before = SourceSnapshot.CaptureDirectory(workspace.SourceRoot);

        var summary = workspace.CreateCoordinator(fileSystem: fileSystem).Build(workspace.Request(project));

        Assert.AreEqual(BuildStatus.StagingFailed, summary.Status);
        Assert.IsTrue(summary.StagingResult!.Failure!.Message.Contains("free space", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(File.Exists(Path.Combine(workspace.OutputRoot, project.ArchiveName)));
        Assert.AreEqual(0, project.BuildHistory.Count);
        before.AssertUnchanged(workspace.SourceRoot);
    }

    [TestMethod]
    public void CleanupFailureAfterVerifiedArchiveReturnsSuccessWithWarning()
    {
        using var workspace = BuildWorkspace.Create();
        workspace.CreateStandardSources();
        var project = workspace.CreateProject();
        var cleanup = new FailingCleanupService();

        var summary = workspace.CreateCoordinator(cleanupService: cleanup).Build(workspace.Request(project));

        Assert.IsTrue(summary.Succeeded, summary.LogText());
        Assert.AreEqual(StagingCleanupStatus.Failed, summary.CleanupResult!.Status);
        Assert.IsTrue(File.Exists(summary.ArchivePath!));
        Assert.AreEqual(0, project.ReleaseHistory.Count);
        Assert.AreEqual(1, project.BuildHistory.Count);
    }

    [TestMethod]
    public void RebuildingSameVersionAppendsBuildEventsWithoutCreatingDuplicateChangelogEntries()
    {
        using var workspace = BuildWorkspace.Create();
        workspace.CreateStandardSources();
        var project = workspace.CreateProject(changes: new[] { "Initial release." });
        project.ReleaseHistory.Add(new ReleaseRecord { Version = project.CurrentVersion, Changes = new List<string> { "Initial release." } });

        var first = workspace.CreateCoordinator().Build(workspace.Request(project, versionDecision: VersionReuseDecision.RebuildExistingVersion));
        var second = workspace.CreateCoordinator().Build(workspace.Request(
            project,
            versionDecision: VersionReuseDecision.RebuildExistingVersion,
            outputDecision: ExistingOutputDecision.Replace));

        Assert.IsTrue(first.Succeeded, first.LogText());
        Assert.IsTrue(second.Succeeded, second.LogText());
        Assert.AreEqual(1, project.ReleaseHistory.Count);
        Assert.AreEqual(2, project.BuildHistory.Count);
        CollectionAssert.AreEqual(new[] { "1.0", "1.0" }, project.BuildHistory.Select(record => record.Version).ToArray());
        Assert.AreEqual(project.ArchiveName, project.BuildHistory[0].ArchiveName);
        Assert.AreEqual(project.ArchiveName, project.BuildHistory[1].ArchiveName);
    }

    [TestMethod]
    public void CleanupServiceRefusesNonApplicationOwnedSession()
    {
        using var workspace = BuildWorkspace.Create();
        var arbitrary = Path.Combine(workspace.Root, "not-app-owned");
        Directory.CreateDirectory(arbitrary);
        File.WriteAllText(Path.Combine(arbitrary, "keep.txt"), "keep");
        var session = new StagingSession(Guid.NewGuid(), workspace.ApplicationRoot, arbitrary, Array.Empty<StagedPackageFile>(), Array.Empty<StagingDuplicateWrite>());

        var result = new StagingCleanupService().CleanSuccessfulStaging(session);

        Assert.AreEqual(StagingCleanupStatus.SkippedNotApplicationOwned, result.Status);
        Assert.IsTrue(Directory.Exists(arbitrary));
        Assert.IsTrue(File.Exists(Path.Combine(arbitrary, "keep.txt")));
    }

    [TestMethod]
    public void BuildLogContainsPipelineAndFailureDiagnostics()
    {
        using var workspace = BuildWorkspace.Create();
        workspace.CreateStandardSources();
        var success = workspace.CreateCoordinator().Build(workspace.Request(workspace.CreateProject()));
        var failure = workspace.CreateCoordinator(archiveService: new ArchiveService(new ThrowingZipArchiveWriter()))
            .Build(workspace.Request(workspace.CreateProject(archiveName: "failure.zip")));

        AssertContainsStages(success.Log, "start", "planning", "validation", "staging", "archive", "history", "cleanup");
        Assert.IsTrue(failure.Log.Entries.Any(entry => entry.Stage == "archive" && entry.TechnicalDetail is not null));
    }

    private static void AssertContainsStages(BuildLog log, params string[] stages)
    {
        foreach (var stage in stages)
        {
            Assert.IsTrue(log.Entries.Any(entry => entry.Stage == stage), stage);
        }
    }

    private static void AssertArchiveEntries(string archivePath, params string[] expected)
    {
        using var archive = ZipFile.OpenRead(archivePath);
        var actual = archive.Entries
            .Where(entry => !string.IsNullOrEmpty(entry.Name))
            .Select(entry => entry.FullName.Replace('\\', '/'))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        CollectionAssert.AreEqual(expected.Order(StringComparer.OrdinalIgnoreCase).ToArray(), actual);
    }

    private static void AssertArchiveContainsEntries(string archivePath, params string[] expected)
    {
        using var archive = ZipFile.OpenRead(archivePath);
        var actual = archive.Entries
            .Where(entry => !string.IsNullOrEmpty(entry.Name))
            .Select(entry => entry.FullName.Replace('\\', '/'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in expected)
        {
            Assert.IsTrue(actual.Contains(entry), "Archive missing expected entry: " + entry);
        }
    }

    private static void AssertArchiveDoesNotContain(string archivePath, params string[] unexpected)
    {
        using var archive = ZipFile.OpenRead(archivePath);
        var actual = archive.Entries
            .Where(entry => !string.IsNullOrEmpty(entry.Name))
            .Select(entry => entry.FullName.Replace('\\', '/'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in unexpected)
        {
            Assert.IsFalse(actual.Contains(entry), "Archive contained unexpected entry: " + entry);
        }
    }

    private static string ReadArchiveText(string archivePath, string entryPath)
    {
        using var archive = ZipFile.OpenRead(archivePath);
        var entry = archive.GetEntry(entryPath) ?? throw new AssertFailedException("Archive entry was not found: " + entryPath);
        using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static void CreateZip(string archivePath, params (string Path, string Contents)[] files)
    {
        using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create);
        foreach (var file in files)
        {
            var entry = archive.CreateEntry(file.Path);
            using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
            writer.Write(file.Contents);
        }
    }

    private sealed class ThrowingZipArchiveWriter : IZipArchiveWriter
    {
        public void CreateFromStaging(StagingSession session, string archivePath) =>
            throw new IOException("Simulated native ZIP failure.");
    }

    private sealed class CopyFailingFileSystem : IFileSystem
    {
        private readonly PhysicalFileSystem inner = new();

        public bool FileExists(string path) => inner.FileExists(path);

        public bool DirectoryExists(string path) => inner.DirectoryExists(path);

        public Stream OpenRead(string path) => inner.OpenRead(path);

        public IReadOnlyList<string> EnumerateFiles(string directoryPath, bool recursive) => inner.EnumerateFiles(directoryPath, recursive);

        public IReadOnlyList<string> EnumerateDirectories(string directoryPath, bool recursive) => inner.EnumerateDirectories(directoryPath, recursive);

        public FileAttributes GetAttributes(string path) => inner.GetAttributes(path);

        public void CopyFile(string sourcePath, string destinationPath, bool overwrite) =>
            throw new IOException("Simulated staging copy failure.");
    }

    private sealed class DiskFullFileSystem : IFileSystem
    {
        private readonly PhysicalFileSystem inner = new();

        public bool FileExists(string path) => inner.FileExists(path);

        public bool DirectoryExists(string path) => inner.DirectoryExists(path);

        public Stream OpenRead(string path) => inner.OpenRead(path);

        public IReadOnlyList<string> EnumerateFiles(string directoryPath, bool recursive) => inner.EnumerateFiles(directoryPath, recursive);

        public IReadOnlyList<string> EnumerateDirectories(string directoryPath, bool recursive) => inner.EnumerateDirectories(directoryPath, recursive);

        public FileAttributes GetAttributes(string path) => inner.GetAttributes(path);

        public void CopyFile(string sourcePath, string destinationPath, bool overwrite) =>
            throw new IOException("Simulated disk full.", unchecked((int)0x80070070));
    }

    private sealed class FailingCleanupService : IStagingCleanupService
    {
        public StagingCleanupResult CleanSuccessfulStaging(StagingSession session) =>
            new(StagingCleanupStatus.Failed, session.StagingRoot, "Simulated cleanup failure.");
    }

    private sealed class BuildWorkspace : IDisposable
    {
        private BuildWorkspace(string root)
        {
            Root = root;
            ApplicationRoot = Path.Combine(root, "app");
            SourceRoot = Path.Combine(root, "source");
            OutputRoot = Path.Combine(root, "output");
            Directory.CreateDirectory(ApplicationRoot);
            Directory.CreateDirectory(SourceRoot);
            Directory.CreateDirectory(OutputRoot);
        }

        public string Root { get; }

        public string ApplicationRoot { get; }

        public string SourceRoot { get; }

        public string OutputRoot { get; }

        public static BuildWorkspace Create() =>
            new(Path.Combine(Path.GetTempPath(), "SfmPackageBuilder.Build.Tests", Guid.NewGuid().ToString("N")));

        public void CreateStandardSources(bool includeCompanions = false)
        {
            WriteSource(@"game\usermod\models\Creator\chair_dev\chair_dev.mdl", "mdl");
            if (includeCompanions)
            {
                WriteSource(@"game\usermod\models\Creator\chair_dev\chair_dev.vvd", "vvd");
                WriteSource(@"game\usermod\models\Creator\chair_dev\chair_dev.dx90.vtx", "dx90");
                WriteSource(@"game\usermod\models\Creator\chair_dev\chair_dev.sw.vtx", "sw");
                WriteSource(@"game\usermod\models\Creator\chair_dev\chair_dev.phy", "phy");
            }

            WriteSource(@"game\usermod\materials\models\Creator\chair\chair.vmt", "vmt");
            WriteSource(@"game\usermod\materials\models\Creator\chair\body.vtf", "vtf");
        }

        public string WriteSource(string relativePath, string contents)
        {
            var path = Path.Combine(SourceRoot, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, contents, Encoding.UTF8);
            return path;
        }

        public PackageProject CreateProject(string archiveName = "package.zip", IReadOnlyList<string>? changes = null)
        {
            return new PackageProject
            {
                AssetName = "Chair Prop",
                CurrentVersion = "1.0",
                ChangesThisVersion = (changes ?? Array.Empty<string>()).ToList(),
                ArchiveName = archiveName,
                Models = new List<ModelEntry>
                {
                    new()
                    {
                        Role = ModelRole.Primary,
                        SourceMdlPath = Path.Combine(SourceRoot, @"game\usermod\models\Creator\chair_dev\chair_dev.mdl"),
                        SourceStem = "chair_dev",
                        ReleaseStem = "chair_release"
                    }
                },
                MaterialSources = new List<SourceEntry>
                {
                    new()
                    {
                        Kind = SourceEntryKind.Material,
                        SourcePath = Path.Combine(SourceRoot, @"game\usermod\materials\models\Creator\chair"),
                        IsFolder = true,
                        IncludeRecursively = true
                    }
                },
                Readme = new ReadmeConfig { Mode = ReadmeMode.Generated }
            };
        }

        public BuildRequest Request(
            PackageProject project,
            VersionReuseDecision versionDecision = VersionReuseDecision.None,
            ExistingOutputDecision outputDecision = ExistingOutputDecision.None) =>
            new()
            {
                Project = project,
                OutputDirectory = OutputRoot,
                VersionReuseDecision = versionDecision,
                ExistingOutputDecision = outputDecision
            };

        public BuildCoordinator CreateCoordinator(
            IFileSystem? fileSystem = null,
            SharedFileRegistry? sharedFileRegistry = null,
            ArchiveService? archiveService = null,
            IStagingCleanupService? cleanupService = null,
            IStorageSpaceProvider? storageSpaceProvider = null)
        {
            var resolvedFileSystem = fileSystem ?? new PhysicalFileSystem();
            PackageCheckService? checkService = null;
            if (sharedFileRegistry is not null)
            {
                checkService = new PackageCheckService(
                    new PackagePlanner(
                        new SourceExpansionService(resolvedFileSystem),
                        sharedFileRegistry,
                        new ReadmeResolver(resolvedFileSystem)),
                    new PackageValidator(resolvedFileSystem, new PhysicalOutputEnvironment()));
            }

            return new BuildCoordinator(
                ApplicationRoot,
                resolvedFileSystem,
                new PhysicalOutputEnvironment(),
                () => FixedTimestamp,
                storageSpaceProvider,
                packageCheckService: checkService,
                archiveService: archiveService,
                cleanupService: cleanupService);
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                foreach (var file in Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }

                Directory.Delete(Root, recursive: true);
            }
        }
    }

    private sealed class FixedStorageSpaceProvider : IStorageSpaceProvider
    {
        private readonly long freeBytes;

        public FixedStorageSpaceProvider(long freeBytes)
        {
            this.freeBytes = freeBytes;
        }

        public StorageSpaceInfo GetAvailableFreeBytes(string path) =>
            StorageSpaceInfo.Known(freeBytes, Path.GetPathRoot(Path.GetFullPath(path)) ?? "test-volume");
    }

    private sealed class UnknownStorageSpaceProvider : IStorageSpaceProvider
    {
        public StorageSpaceInfo GetAvailableFreeBytes(string path) =>
            StorageSpaceInfo.Unknown("unknown in test");
    }

    private sealed class ActionProgress : IProgress<BuildProgress>
    {
        private readonly Action<BuildProgress> report;

        public ActionProgress(Action<BuildProgress> report)
        {
            this.report = report;
        }

        public void Report(BuildProgress value) => report(value);
    }

    private sealed class SourceSnapshot
    {
        private SourceSnapshot(IReadOnlyList<(string RelativePath, byte[] Bytes, FileAttributes Attributes)> files)
        {
            Files = files;
        }

        private IReadOnlyList<(string RelativePath, byte[] Bytes, FileAttributes Attributes)> Files { get; }

        public static SourceSnapshot CaptureDirectory(string root) =>
            new(Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .Select(path => (Path.GetRelativePath(root, path), File.ReadAllBytes(path), File.GetAttributes(path)))
                .OrderBy(item => item.Item1, StringComparer.OrdinalIgnoreCase)
                .ToArray());

        public void AssertUnchanged(string root)
        {
            var current = CaptureDirectory(root).Files;
            Assert.AreEqual(Files.Count, current.Count);
            for (var i = 0; i < Files.Count; i++)
            {
                Assert.AreEqual(Files[i].RelativePath, current[i].RelativePath);
                CollectionAssert.AreEqual(Files[i].Bytes, current[i].Bytes);
                Assert.AreEqual(Files[i].Attributes, current[i].Attributes);
            }
        }
    }
}

internal static class BuildSummaryTestExtensions
{
    public static string LogText(this BuildSummary summary) =>
        string.Join(Environment.NewLine, summary.Log.Entries.Select(entry => $"{entry.Stage}: {entry.Message} {entry.TechnicalDetail}"));
}
