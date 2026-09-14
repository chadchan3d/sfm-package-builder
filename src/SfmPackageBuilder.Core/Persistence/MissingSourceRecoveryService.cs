using SfmPackageBuilder.Core.Domain;
using SfmPackageBuilder.Core.FileSystem;
using SfmPackageBuilder.Core.Paths;

namespace SfmPackageBuilder.Core.Persistence;

public sealed class MissingSourceRecoveryService
{
    private readonly IFileSystem fileSystem;
    private readonly PathAnchorDetector anchorDetector = new();

    public MissingSourceRecoveryService(IFileSystem fileSystem)
    {
        this.fileSystem = fileSystem;
    }

    public RecoveryInspectionResult Inspect(PackageProject project)
    {
        ArgumentNullException.ThrowIfNull(project);
        var issues = new List<RecoveryIssue>();

        foreach (var model in project.Models)
        {
            AddPathIssue(
                issues,
                model.SourceMdlPath,
                model.Role == ModelRole.Primary ? RecoveryReferenceKind.PrimaryModel : RecoveryReferenceKind.AdditionalModel,
                model.Id,
                sourceReference: model.SourceReference);

            foreach (var companion in model.Companions.Where(companion => !string.IsNullOrWhiteSpace(companion.SourcePath)))
            {
                AddPathIssue(
                    issues,
                    companion.SourcePath,
                    RecoveryReferenceKind.ModelCompanion,
                    model.Id,
                    companion.RuntimeSuffix,
                    sourceReference: companion.SourceReference);
            }
        }

        foreach (var source in project.MaterialSources)
        {
            AddPathIssue(
                issues,
                source.SourcePath,
                source.IsFolder ? RecoveryReferenceKind.MaterialFolder : RecoveryReferenceKind.MaterialFile,
                source.Id,
                expectDirectory: source.IsFolder,
                sourceReference: source.SourceReference);
        }

        foreach (var source in project.Extras)
        {
            AddPathIssue(
                issues,
                source.SourcePath,
                source.IsFolder ? RecoveryReferenceKind.ExtraFolder : RecoveryReferenceKind.ExtraFile,
                source.Id,
                expectDirectory: source.IsFolder,
                sourceReference: source.SourceReference);
        }

        if (project.Readme.Mode == ReadmeMode.Custom
            && project.Readme.CustomSource == ReadmeCustomSource.ImportedFile
            && !string.IsNullOrWhiteSpace(project.Readme.ImportedReadmePath))
        {
            AddPathIssue(
                issues,
                project.Readme.ImportedReadmePath,
                RecoveryReferenceKind.ImportedReadme,
                null,
                sourceReference: project.Readme.ImportedReadmeReference);
        }

        return new RecoveryInspectionResult(issues.OrderBy(issue => issue.Id, StringComparer.Ordinal).ToArray());
    }

    public RecoveryRemapPreview PreviewFolderRemap(PackageProject project, string formerRoot, string replacementRoot)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(formerRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(replacementRoot);

        var group = Inspect(project).Groups.SingleOrDefault(candidate => PathsEqual(candidate.FormerRoot, formerRoot));
        if (group is null)
        {
            return new RecoveryRemapPreview(formerRoot, replacementRoot, Array.Empty<RecoveryRemapCandidate>());
        }

        var candidates = group.Issues
            .Select(issue => CreateRemapCandidate(issue, replacementRoot))
            .OrderBy(candidate => candidate.Issue.Id, StringComparer.Ordinal)
            .ToArray();
        return new RecoveryRemapPreview(group.FormerRoot, replacementRoot, candidates);
    }

    public RecoveryRemapApplyResult ApplyFolderRemap(PackageProject project, string? projectPath, RecoveryRemapPreview preview)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(preview);

        var applied = 0;
        foreach (var candidate in preview.MatchedCandidates)
        {
            ApplyReplacement(project, candidate.Issue, candidate.CandidatePath, projectPath);
            applied++;
        }

        return new RecoveryRemapApplyResult(applied);
    }

    public RecoveryOperationResult LocateReplacement(PackageProject project, string issueId, string replacementPath, string? projectPath = null)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(issueId);
        ArgumentException.ThrowIfNullOrWhiteSpace(replacementPath);

        var issue = Inspect(project).Issues.SingleOrDefault(issue => issue.Id == issueId);
        if (issue is null)
        {
            return new RecoveryOperationResult(RecoveryOperationStatus.IssueNotFound, null, replacementPath, message: "The recovery issue no longer exists.");
        }

        var expectsDirectory = issue.ExpectsDirectory;
        if (expectsDirectory)
        {
            if (!fileSystem.DirectoryExists(replacementPath))
            {
                return new RecoveryOperationResult(RecoveryOperationStatus.ReplacementMissing, issue, replacementPath, message: "The replacement folder was not found.");
            }
        }
        else if (!fileSystem.FileExists(replacementPath))
        {
            return new RecoveryOperationResult(RecoveryOperationStatus.ReplacementMissing, issue, replacementPath, message: "The replacement file was not found.");
        }

        var accessibility = CheckReadable(replacementPath, expectsDirectory);
        if (accessibility is not null)
        {
            return new RecoveryOperationResult(RecoveryOperationStatus.ReplacementInaccessible, issue, replacementPath, technicalDetail: accessibility);
        }

        ApplyReplacement(project, issue, replacementPath, projectPath);
        var destinationDecisionNeeded = DestinationDecisionMayBeNeeded(project, issue, replacementPath);

        return new RecoveryOperationResult(
            RecoveryOperationStatus.Success,
            issue,
            replacementPath,
            destinationDecisionNeeded,
            destinationDecisionNeeded ? "The replacement may need an explicit destination decision." : null);
    }

    public RecoveryOperationResult RemoveFromPackage(PackageProject project, string issueId)
    {
        ArgumentNullException.ThrowIfNull(project);
        var issue = Inspect(project).Issues.SingleOrDefault(issue => issue.Id == issueId);
        if (issue is null)
        {
            return new RecoveryOperationResult(RecoveryOperationStatus.IssueNotFound, null);
        }

        switch (issue.ReferenceKind)
        {
            case RecoveryReferenceKind.PrimaryModel:
                return new RecoveryOperationResult(
                    RecoveryOperationStatus.CannotRemoveRequiredPrimaryModel,
                    issue,
                    message: "Removing the primary model would leave the project without required primary model intent.");
            case RecoveryReferenceKind.AdditionalModel:
                project.Models.RemoveAll(model => model.Id == issue.ProjectEntryId && model.Role == ModelRole.Additional);
                break;
            case RecoveryReferenceKind.ModelCompanion:
                var model = project.Models.SingleOrDefault(model => model.Id == issue.ProjectEntryId);
                model?.Companions.RemoveAll(companion => string.Equals(companion.RuntimeSuffix, issue.CompanionRuntimeSuffix, StringComparison.OrdinalIgnoreCase));
                break;
            case RecoveryReferenceKind.MaterialFile:
            case RecoveryReferenceKind.MaterialFolder:
                project.MaterialSources.RemoveAll(source => source.Id == issue.ProjectEntryId);
                break;
            case RecoveryReferenceKind.ExtraFile:
            case RecoveryReferenceKind.ExtraFolder:
                project.Extras.RemoveAll(source => source.Id == issue.ProjectEntryId);
                break;
            case RecoveryReferenceKind.ImportedReadme:
                project.Readme.ImportedReadmePath = null;
                project.Readme.Mode = ReadmeMode.None;
                break;
        }

        return new RecoveryOperationResult(RecoveryOperationStatus.Success, issue);
    }

    private void AddPathIssue(
        List<RecoveryIssue> issues,
        string sourcePath,
        RecoveryReferenceKind kind,
        Guid? projectEntryId,
        string? companionRuntimeSuffix = null,
        bool expectDirectory = false,
        PersistedSourceReference? sourceReference = null)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            issues.Add(CreateIssue(kind, RecoveryIssueStatus.Missing, sourcePath, projectEntryId, companionRuntimeSuffix, expectDirectory, sourceReference));
            return;
        }

        var exists = expectDirectory ? fileSystem.DirectoryExists(sourcePath) : fileSystem.FileExists(sourcePath);
        if (!exists)
        {
            issues.Add(CreateIssue(kind, RecoveryIssueStatus.Missing, sourcePath, projectEntryId, companionRuntimeSuffix, expectDirectory, sourceReference));
            return;
        }

        var accessibility = CheckReadable(sourcePath, expectDirectory);
        if (accessibility is not null)
        {
            issues.Add(CreateIssue(kind, RecoveryIssueStatus.Inaccessible, sourcePath, projectEntryId, companionRuntimeSuffix, expectDirectory, sourceReference, accessibility));
        }
    }

    private RecoveryRemapCandidate CreateRemapCandidate(RecoveryIssue issue, string replacementRoot)
    {
        var candidatePath = CreateCandidatePath(replacementRoot, issue.RelativeRemainder);
        var fileExists = fileSystem.FileExists(candidatePath);
        var directoryExists = fileSystem.DirectoryExists(candidatePath);

        if (issue.ExpectsDirectory)
        {
            if (directoryExists)
            {
                var accessibility = CheckReadable(candidatePath, isDirectory: true);
                return accessibility is null
                    ? new RecoveryRemapCandidate(issue, candidatePath, RecoveryRemapCandidateStatus.Matched)
                    : new RecoveryRemapCandidate(issue, candidatePath, RecoveryRemapCandidateStatus.Inaccessible, accessibility);
            }

            return fileExists
                ? new RecoveryRemapCandidate(issue, candidatePath, RecoveryRemapCandidateStatus.TypeMismatch)
                : new RecoveryRemapCandidate(issue, candidatePath, RecoveryRemapCandidateStatus.Missing);
        }

        if (fileExists)
        {
            var accessibility = CheckReadable(candidatePath, isDirectory: false);
            return accessibility is null
                ? new RecoveryRemapCandidate(issue, candidatePath, RecoveryRemapCandidateStatus.Matched)
                : new RecoveryRemapCandidate(issue, candidatePath, RecoveryRemapCandidateStatus.Inaccessible, accessibility);
        }

        return directoryExists
            ? new RecoveryRemapCandidate(issue, candidatePath, RecoveryRemapCandidateStatus.TypeMismatch)
            : new RecoveryRemapCandidate(issue, candidatePath, RecoveryRemapCandidateStatus.Missing);
    }

    private string? CheckReadable(string path, bool isDirectory)
    {
        try
        {
            if (isDirectory)
            {
                _ = fileSystem.EnumerateFiles(path, recursive: false);
            }
            else
            {
                using var stream = fileSystem.OpenRead(path);
                _ = stream.CanRead;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return ex.Message;
        }

        return null;
    }

    private static RecoveryIssue CreateIssue(
        RecoveryReferenceKind kind,
        RecoveryIssueStatus status,
        string sourcePath,
        Guid? projectEntryId,
        string? companionRuntimeSuffix,
        bool expectDirectory,
        PersistedSourceReference? sourceReference,
        string? technicalDetail = null)
    {
        var id = projectEntryId.HasValue
            ? $"{kind}:{projectEntryId:D}:{companionRuntimeSuffix ?? string.Empty}"
            : $"{kind}:readme";
        var location = CreateFormerLocation(sourcePath, expectDirectory, sourceReference);
        return new RecoveryIssue(
            id,
            kind,
            status,
            sourcePath,
            projectEntryId,
            companionRuntimeSuffix,
            location.FormerRoot,
            location.RelativeRemainder,
            expectDirectory,
            technicalDetail: technicalDetail);
    }

    private static (string? FormerRoot, string? RelativeRemainder) CreateFormerLocation(
        string sourcePath,
        bool expectDirectory,
        PersistedSourceReference? sourceReference)
    {
        if (!string.IsNullOrWhiteSpace(sourceReference?.RecoveryRootPath))
        {
            return (
                Normalize(sourceReference!.RecoveryRootPath!),
                string.IsNullOrWhiteSpace(sourceReference.RecoveryRelativePath) ? null : Normalize(sourceReference.RecoveryRelativePath!));
        }

        var absolute = string.IsNullOrWhiteSpace(sourceReference?.AbsolutePath)
            ? sourcePath
            : sourceReference!.AbsolutePath;
        if (string.IsNullOrWhiteSpace(absolute))
        {
            return (null, null);
        }

        absolute = Normalize(absolute);
        if (!string.IsNullOrWhiteSpace(sourceReference?.RelativePath))
        {
            var relative = Normalize(sourceReference.RelativePath!);
            var suffix = Path.DirectorySeparatorChar + relative.TrimStart(Path.DirectorySeparatorChar);
            if (absolute.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return (absolute[..^suffix.Length], relative);
            }
        }

        var root = Path.GetDirectoryName(absolute);
        var remainder = Path.GetFileName(absolute);
        return (string.IsNullOrWhiteSpace(root) ? null : root, string.IsNullOrWhiteSpace(remainder) ? null : remainder);
    }

    private static string Normalize(string path) =>
        path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).TrimEnd(Path.DirectorySeparatorChar);

    private static string CreateCandidatePath(string replacementRoot, string? relativeRemainder)
    {
        var combined = string.IsNullOrWhiteSpace(relativeRemainder)
            ? replacementRoot
            : Path.Combine(replacementRoot, relativeRemainder);
        return Path.GetFullPath(combined).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
    }

    private static bool PathsEqual(string left, string right) =>
        string.Equals(Normalize(Path.GetFullPath(left)), Normalize(Path.GetFullPath(right)), StringComparison.OrdinalIgnoreCase);

    private static PersistedSourceReference CreateUpdatedReference(RecoveryIssue issue, string replacementPath, string? projectPath)
    {
        var anchorKind = issue.ReferenceKind switch
        {
            RecoveryReferenceKind.PrimaryModel or RecoveryReferenceKind.AdditionalModel or RecoveryReferenceKind.ModelCompanion => SourceAnchorKind.Models,
            RecoveryReferenceKind.MaterialFile or RecoveryReferenceKind.MaterialFolder => SourceAnchorKind.Materials,
            _ => (SourceAnchorKind?)null
        };

        return CreateUpdatedReference(replacementPath, projectPath, issue.ExpectsDirectory, anchorKind);
    }

    private static PersistedSourceReference CreateUpdatedReference(
        string replacementPath,
        string? projectPath,
        bool isFolder,
        SourceAnchorKind? anchorKind) =>
        string.IsNullOrWhiteSpace(projectPath)
            ? ProjectSourceReferenceService.CreateReferenceForSourcePath(replacementPath, isFolder, anchorKind)
            : ProjectSourceReferenceService.CreateReferenceForProjectPath(replacementPath, projectPath, isFolder, anchorKind);

    private static void ApplyReplacement(PackageProject project, RecoveryIssue issue, string replacementPath, string? projectPath)
    {
        var sourceReference = CreateUpdatedReference(issue, replacementPath, projectPath);
        switch (issue.ReferenceKind)
        {
            case RecoveryReferenceKind.PrimaryModel:
            case RecoveryReferenceKind.AdditionalModel:
                var model = project.Models.Single(entry => entry.Id == issue.ProjectEntryId);
                model.SourceMdlPath = replacementPath;
                model.SourceReference = sourceReference;
                model.SourceStem = Path.GetFileNameWithoutExtension(replacementPath);
                break;
            case RecoveryReferenceKind.ModelCompanion:
                var companionModel = project.Models.Single(entry => entry.Id == issue.ProjectEntryId);
                var companion = companionModel.Companions.Single(entry => string.Equals(entry.RuntimeSuffix, issue.CompanionRuntimeSuffix, StringComparison.OrdinalIgnoreCase));
                companion.SourcePath = replacementPath;
                companion.SourceReference = sourceReference;
                break;
            case RecoveryReferenceKind.MaterialFile:
            case RecoveryReferenceKind.MaterialFolder:
                var material = project.MaterialSources.Single(entry => entry.Id == issue.ProjectEntryId);
                material.SourcePath = replacementPath;
                material.SourceReference = sourceReference;
                break;
            case RecoveryReferenceKind.ExtraFile:
            case RecoveryReferenceKind.ExtraFolder:
                var extra = project.Extras.Single(entry => entry.Id == issue.ProjectEntryId);
                extra.SourcePath = replacementPath;
                extra.SourceReference = sourceReference;
                break;
            case RecoveryReferenceKind.ImportedReadme:
                project.Readme.ImportedReadmePath = replacementPath;
                project.Readme.ImportedReadmeReference = sourceReference;
                project.Readme.Mode = ReadmeMode.Custom;
                project.Readme.CustomSource = ReadmeCustomSource.ImportedFile;
                break;
        }
    }

    private bool DestinationDecisionMayBeNeeded(PackageProject project, RecoveryIssue issue, string replacementPath)
    {
        if (issue.ReferenceKind is RecoveryReferenceKind.MaterialFile or RecoveryReferenceKind.MaterialFolder)
        {
            var source = project.MaterialSources.Single(entry => entry.Id == issue.ProjectEntryId);
            return source.DestinationOverride is null
                && !anchorDetector.Resolve(replacementPath, SourceAnchorKind.Materials).IsResolved;
        }

        if (issue.ReferenceKind is RecoveryReferenceKind.PrimaryModel or RecoveryReferenceKind.AdditionalModel)
        {
            var model = project.Models.Single(entry => entry.Id == issue.ProjectEntryId);
            return model.DestinationOverride is null
                && !anchorDetector.Resolve(replacementPath, SourceAnchorKind.Models).IsResolved;
        }

        return false;
    }
}
