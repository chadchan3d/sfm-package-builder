namespace SfmPackageBuilder.Core.Archive;

public enum ArchiveFailureKind
{
    ExistingOutputRequiresDecision,
    ExistingOutputCancelled,
    ChooseAnotherNameRequiresDifferentTarget,
    EmptyStagingSession,
    StagingRootMissing,
    ZipCreationFailed,
    ArchiveMissing,
    ArchiveVerificationFailed,
    ReplaceFailed,
    TemporaryArchiveCleanupFailed
}
