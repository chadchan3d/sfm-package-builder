namespace SfmPackageBuilder.Core.Staging;

public sealed class StagingExecutionResult
{
    private StagingExecutionResult(
        IReadOnlyList<StagedPackageFile> files,
        IReadOnlyList<StagingDuplicateWrite> skippedDuplicateWrites,
        StagingFailure? failure)
    {
        Files = files.ToArray();
        SkippedDuplicateWrites = skippedDuplicateWrites.ToArray();
        Failure = failure;
    }

    public bool Succeeded => Failure is null;

    public IReadOnlyList<StagedPackageFile> Files { get; }

    public IReadOnlyList<StagingDuplicateWrite> SkippedDuplicateWrites { get; }

    public StagingFailure? Failure { get; }

    public static StagingExecutionResult Success(
        IReadOnlyList<StagedPackageFile> files,
        IReadOnlyList<StagingDuplicateWrite> skippedDuplicateWrites) =>
        new(files, skippedDuplicateWrites, null);

    public static StagingExecutionResult Failed(
        StagingFailure failure,
        IReadOnlyList<StagedPackageFile>? files = null,
        IReadOnlyList<StagingDuplicateWrite>? skippedDuplicateWrites = null) =>
        new(files ?? Array.Empty<StagedPackageFile>(), skippedDuplicateWrites ?? Array.Empty<StagingDuplicateWrite>(), failure);
}
