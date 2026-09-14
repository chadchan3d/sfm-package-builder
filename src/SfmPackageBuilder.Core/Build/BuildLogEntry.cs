namespace SfmPackageBuilder.Core.Build;

public sealed class BuildLogEntry
{
    public BuildLogEntry(string stage, string message, string? technicalDetail = null)
    {
        Stage = stage;
        Message = message;
        TechnicalDetail = technicalDetail;
    }

    public string Stage { get; }

    public string Message { get; }

    public string? TechnicalDetail { get; }
}
