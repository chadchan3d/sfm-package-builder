namespace SfmPackageBuilder.Core.Archive;

public sealed class ArchiveVerificationResult
{
    private ArchiveVerificationResult(
        bool succeeded,
        IReadOnlyList<string> expectedEntries,
        IReadOnlyList<string> actualEntries,
        string? failureMessage,
        string? technicalDetail)
    {
        Succeeded = succeeded;
        ExpectedEntries = expectedEntries.ToArray();
        ActualEntries = actualEntries.ToArray();
        FailureMessage = failureMessage;
        TechnicalDetail = technicalDetail;
    }

    public bool Succeeded { get; }

    public IReadOnlyList<string> ExpectedEntries { get; }

    public IReadOnlyList<string> ActualEntries { get; }

    public string? FailureMessage { get; }

    public string? TechnicalDetail { get; }

    public static ArchiveVerificationResult Success(IReadOnlyList<string> expectedEntries, IReadOnlyList<string> actualEntries) =>
        new(true, expectedEntries, actualEntries, null, null);

    public static ArchiveVerificationResult Failed(
        IReadOnlyList<string> expectedEntries,
        IReadOnlyList<string> actualEntries,
        string failureMessage,
        string? technicalDetail = null) =>
        new(false, expectedEntries, actualEntries, failureMessage, technicalDetail);
}
