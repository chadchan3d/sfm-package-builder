namespace SfmPackageBuilder.Core.Staging;

public sealed class StagingDuplicateWrite
{
    public StagingDuplicateWrite(
        string destinationRelativePath,
        string sourcePath,
        IEnumerable<string> skippedEntryIds)
    {
        DestinationRelativePath = destinationRelativePath;
        SourcePath = sourcePath;
        SkippedEntryIds = skippedEntryIds.ToArray();
    }

    public string DestinationRelativePath { get; }

    public string SourcePath { get; }

    public IReadOnlyList<string> SkippedEntryIds { get; }
}
