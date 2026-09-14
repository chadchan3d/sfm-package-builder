namespace SfmPackageBuilder.Core.Staging;

public enum StagingFailureKind
{
    UnresolvedPlanEntry,
    MissingDestination,
    UnsafeDestination,
    MissingSource,
    SourceReadFailed,
    DestinationCollision,
    DestinationAlreadyExists,
    WriteFailed,
    VerificationFailed,
    Cancelled
}
