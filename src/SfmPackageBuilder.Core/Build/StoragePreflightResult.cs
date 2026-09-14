namespace SfmPackageBuilder.Core.Build;

public sealed class StoragePreflightResult
{
    private StoragePreflightResult(bool succeeded, string? message, string? technicalDetail)
    {
        Succeeded = succeeded;
        Message = message;
        TechnicalDetail = technicalDetail;
    }

    public bool Succeeded { get; }

    public string? Message { get; }

    public string? TechnicalDetail { get; }

    public static StoragePreflightResult Success() => new(true, null, null);

    public static StoragePreflightResult Failed(string message, string? technicalDetail = null) =>
        new(false, message, technicalDetail);
}
