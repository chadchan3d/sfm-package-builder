namespace SfmPackageBuilder.Core.Planning;

public enum PackagePlanEntryStatus
{
    Resolved,
    MissingSource,
    MissingAnchor,
    InvalidDestinationOverride,
    SourceEnumerationFailed,
    SourceReadFailed,
    ReadmeResolutionFailed
}
