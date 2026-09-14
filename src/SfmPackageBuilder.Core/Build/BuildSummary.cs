using SfmPackageBuilder.Core.Archive;
using SfmPackageBuilder.Core.Planning;
using SfmPackageBuilder.Core.Staging;
using SfmPackageBuilder.Core.Validation;

namespace SfmPackageBuilder.Core.Build;

public sealed class BuildSummary
{
    public BuildSummary(
        BuildStatus status,
        string version,
        string? archivePath,
        PackagePlan? plan,
        ValidationResult? validation,
        StagingResult? stagingResult,
        ArchiveResult? archiveResult,
        BuildHistoryCommitResult? buildHistoryCommit,
        StagingCleanupResult? cleanupResult,
        IReadOnlyList<RequiredDecision> requiredDecisions,
        IReadOnlyList<string> resolvedDecisions,
        BuildLog log)
    {
        Status = status;
        Version = version;
        ArchivePath = archivePath;
        Plan = plan;
        Validation = validation;
        StagingResult = stagingResult;
        ArchiveResult = archiveResult;
        BuildHistoryCommit = buildHistoryCommit;
        CleanupResult = cleanupResult;
        RequiredDecisions = requiredDecisions.ToArray();
        ResolvedDecisions = resolvedDecisions.ToArray();
        Log = log;
    }

    public BuildStatus Status { get; }

    public bool Succeeded => Status == BuildStatus.Succeeded;

    public string Version { get; }

    public string? ArchivePath { get; }

    public PackagePlan? Plan { get; }

    public ValidationResult? Validation { get; }

    public StagingResult? StagingResult { get; }

    public ArchiveResult? ArchiveResult { get; }

    public BuildHistoryCommitResult? BuildHistoryCommit { get; }

    public StagingCleanupResult? CleanupResult { get; }

    public IReadOnlyList<RequiredDecision> RequiredDecisions { get; }

    public IReadOnlyList<string> ResolvedDecisions { get; }

    public BuildLog Log { get; }
}
