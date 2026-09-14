namespace SfmPackageBuilder.Core.Paths;

public enum PathValidationStatus
{
    Valid,
    Empty,
    RootedPath,
    Traversal,
    InvalidCharacters,
    ReservedDeviceName,
    AmbiguousSegment
}
