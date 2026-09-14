namespace SfmPackageBuilder.Core.Staging;

public sealed class StagingSession
{
    public StagingSession(
        Guid sessionId,
        string applicationRoot,
        string stagingRoot,
        IReadOnlyList<StagedPackageFile> files,
        IReadOnlyList<StagingDuplicateWrite> skippedDuplicateWrites)
    {
        SessionId = sessionId;
        ApplicationRoot = Path.GetFullPath(applicationRoot);
        StagingRoot = Path.GetFullPath(stagingRoot);
        Files = files.ToArray();
        SkippedDuplicateWrites = skippedDuplicateWrites.ToArray();
    }

    public Guid SessionId { get; }

    public string ApplicationRoot { get; }

    public string StagingRoot { get; }

    public IReadOnlyList<StagedPackageFile> Files { get; }

    public IReadOnlyList<StagingDuplicateWrite> SkippedDuplicateWrites { get; }

    public bool IsApplicationOwned =>
        StagingRoot.Equals(
            Path.GetFullPath(Path.Combine(ApplicationRoot, ".staging", SessionId.ToString("N"))),
            StringComparison.OrdinalIgnoreCase);
}
