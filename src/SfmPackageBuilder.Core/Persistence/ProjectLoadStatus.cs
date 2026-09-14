namespace SfmPackageBuilder.Core.Persistence;

public enum ProjectLoadStatus
{
    Success,
    InvalidJson,
    FileNotFound,
    FileAccessError,
    MissingSchemaVersion,
    UnsupportedLegacySchema,
    UnsupportedFutureSchema,
    IncompatibleProjectState
}
