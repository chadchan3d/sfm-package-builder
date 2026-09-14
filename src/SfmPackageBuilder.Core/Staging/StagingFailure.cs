namespace SfmPackageBuilder.Core.Staging;

public sealed class StagingFailure
{
    public StagingFailure(
        StagingFailureKind kind,
        string message,
        string stagingRoot,
        string? entryId = null,
        string? sourcePath = null,
        string? destinationRelativePath = null,
        string? technicalDetail = null)
    {
        Kind = kind;
        Message = message;
        StagingRoot = stagingRoot;
        EntryId = entryId;
        SourcePath = sourcePath;
        DestinationRelativePath = destinationRelativePath;
        TechnicalDetail = technicalDetail;
    }

    public StagingFailureKind Kind { get; }

    public string Message { get; }

    public string StagingRoot { get; }

    public string? EntryId { get; }

    public string? SourcePath { get; }

    public string? DestinationRelativePath { get; }

    public string? TechnicalDetail { get; }
}
