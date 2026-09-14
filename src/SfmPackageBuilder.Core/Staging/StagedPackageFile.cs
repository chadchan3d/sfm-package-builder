namespace SfmPackageBuilder.Core.Staging;

public sealed class StagedPackageFile
{
    public StagedPackageFile(
        string entryId,
        string destinationRelativePath,
        string stagedPath,
        StagingWriteKind writeKind,
        string? sourcePath,
        long fileSize)
    {
        EntryId = entryId;
        DestinationRelativePath = destinationRelativePath;
        StagedPath = stagedPath;
        WriteKind = writeKind;
        SourcePath = sourcePath;
        FileSize = fileSize;
    }

    public string EntryId { get; }

    public string DestinationRelativePath { get; }

    public string StagedPath { get; }

    public StagingWriteKind WriteKind { get; }

    public string? SourcePath { get; }

    public long FileSize { get; }
}
