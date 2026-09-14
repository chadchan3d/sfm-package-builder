namespace SfmPackageBuilder.Core.Build;

public enum BuildStatus
{
    Succeeded,
    DecisionRequired,
    ChangeRequired,
    Cancelled,
    ValidationFailed,
    StorageFailed,
    StagingFailed,
    ArchiveFailed
}
