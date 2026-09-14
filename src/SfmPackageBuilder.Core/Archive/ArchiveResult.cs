namespace SfmPackageBuilder.Core.Archive;

public sealed class ArchiveResult
{
    private ArchiveResult(
        bool succeeded,
        string? archivePath,
        string? temporaryArchivePath,
        ArchiveReplacementBehavior replacementBehavior,
        ArchiveVerificationResult? verification,
        ArchiveFailure? failure)
    {
        Succeeded = succeeded;
        ArchivePath = archivePath;
        TemporaryArchivePath = temporaryArchivePath;
        ReplacementBehavior = replacementBehavior;
        Verification = verification;
        Failure = failure;
    }

    public bool Succeeded { get; }

    public string? ArchivePath { get; }

    public string? TemporaryArchivePath { get; }

    public ArchiveReplacementBehavior ReplacementBehavior { get; }

    public ArchiveVerificationResult? Verification { get; }

    public ArchiveFailure? Failure { get; }

    public static ArchiveResult Success(
        string archivePath,
        ArchiveReplacementBehavior replacementBehavior,
        ArchiveVerificationResult verification) =>
        new(
            true,
            archivePath,
            null,
            replacementBehavior,
            verification,
            null);

    public static ArchiveResult Failed(
        ArchiveFailure failure,
        ArchiveReplacementBehavior replacementBehavior = ArchiveReplacementBehavior.NoMutation,
        ArchiveVerificationResult? verification = null) =>
        new(
            false,
            failure.TargetArchivePath,
            failure.TemporaryArchivePath,
            replacementBehavior,
            verification,
            failure);
}
