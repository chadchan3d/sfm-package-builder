namespace SfmPackageBuilder.Core.Archive;

public sealed class ArchiveFailure
{
    public ArchiveFailure(
        ArchiveFailureKind kind,
        string message,
        string? targetArchivePath = null,
        string? temporaryArchivePath = null,
        string? technicalDetail = null)
    {
        Kind = kind;
        Message = message;
        TargetArchivePath = targetArchivePath;
        TemporaryArchivePath = temporaryArchivePath;
        TechnicalDetail = technicalDetail;
    }

    public ArchiveFailureKind Kind { get; }

    public string Message { get; }

    public string? TargetArchivePath { get; }

    public string? TemporaryArchivePath { get; }

    public string? TechnicalDetail { get; }
}
