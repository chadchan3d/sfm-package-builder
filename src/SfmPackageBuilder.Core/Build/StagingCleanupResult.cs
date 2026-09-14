namespace SfmPackageBuilder.Core.Build;

public sealed class StagingCleanupResult
{
    public StagingCleanupResult(StagingCleanupStatus status, string stagingRoot, string? technicalDetail = null)
    {
        Status = status;
        StagingRoot = stagingRoot;
        TechnicalDetail = technicalDetail;
    }

    public StagingCleanupStatus Status { get; }

    public string StagingRoot { get; }

    public string? TechnicalDetail { get; }

    public bool Succeeded => Status == StagingCleanupStatus.Succeeded;
}
