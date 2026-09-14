namespace SfmPackageBuilder.Core.Readme;

public sealed class ReadmeContext
{
    public IReadOnlyList<string> ModelPaths { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> InstallRoots { get; init; } = Array.Empty<string>();

    public bool PackageReadmeIncluded { get; init; }

    public bool DocsFolderIncluded { get; init; }

    public bool OtherSupportingFilesIncluded { get; init; }

    public bool ControlGroupsFileDetected { get; init; }
}
