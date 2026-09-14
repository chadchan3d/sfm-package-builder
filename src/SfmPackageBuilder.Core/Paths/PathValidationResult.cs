namespace SfmPackageBuilder.Core.Paths;

public sealed class PathValidationResult
{
    private PathValidationResult(PathValidationStatus status, string? normalizedPath, string? message)
    {
        Status = status;
        NormalizedPath = normalizedPath;
        Message = message;
    }

    public PathValidationStatus Status { get; }

    public bool IsValid => Status == PathValidationStatus.Valid;

    public string? NormalizedPath { get; }

    public string? Message { get; }

    public static PathValidationResult Valid(string normalizedPath) =>
        new(PathValidationStatus.Valid, normalizedPath, null);

    public static PathValidationResult Invalid(PathValidationStatus status, string message) =>
        new(status, null, message);
}
