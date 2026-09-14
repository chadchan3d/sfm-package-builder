namespace SfmPackageBuilder.Core.Readme;

public sealed class SfmInstallRootResolution
{
    public SfmInstallRootResolution(
        IReadOnlyList<string> installRoots,
        bool hasDocsFolder,
        bool hasOtherSupportingFiles)
    {
        InstallRoots = installRoots.ToArray();
        HasDocsFolder = hasDocsFolder;
        HasOtherSupportingFiles = hasOtherSupportingFiles;
    }

    public IReadOnlyList<string> InstallRoots { get; }

    public bool HasDocsFolder { get; }

    public bool HasOtherSupportingFiles { get; }
}
