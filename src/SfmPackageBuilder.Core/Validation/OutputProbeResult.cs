namespace SfmPackageBuilder.Core.Validation;

public sealed class OutputProbeResult
{
    private OutputProbeResult(bool canWrite, string? technicalDetail)
    {
        CanWrite = canWrite;
        TechnicalDetail = technicalDetail;
    }

    public bool CanWrite { get; }

    public string? TechnicalDetail { get; }

    public static OutputProbeResult Success() => new(true, null);

    public static OutputProbeResult Failure(string technicalDetail) => new(false, technicalDetail);
}
