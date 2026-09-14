using SfmPackageBuilder.Core.Domain;
using SfmPackageBuilder.Core.Planning;
using SfmPackageBuilder.Core.SharedFiles;

namespace SfmPackageBuilder.Core.Readme;

public sealed class ReadmeContextBuilder
{
    private readonly SfmInstallRootResolver installRootResolver = new();

    public ReadmeContext Create(
        PackageProject project,
        IReadOnlyList<PackagePlanEntry> ordinaryEntries,
        IReadOnlyList<SharedFileNotice> sharedNotices)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(ordinaryEntries);
        ArgumentNullException.ThrowIfNull(sharedNotices);

        var modelPaths = ordinaryEntries
            .Where(entry => entry.EntryType == PackagePlanEntryType.Model
                && entry.Status == PackagePlanEntryStatus.Resolved
                && entry.DestinationRelativePath is not null)
            .Select(entry => entry.DestinationRelativePath!)
            .ToArray();
        var installRootResolution = installRootResolver.Resolve(ordinaryEntries);

        return new ReadmeContext
        {
            ModelPaths = modelPaths,
            InstallRoots = installRootResolution.InstallRoots,
            PackageReadmeIncluded = project.Readme.Mode != ReadmeMode.None,
            DocsFolderIncluded = installRootResolution.HasDocsFolder,
            OtherSupportingFilesIncluded = installRootResolution.HasOtherSupportingFiles,
            ControlGroupsFileDetected = SharedFileRegistry.HasControlGroupsReadmeContext(sharedNotices)
        };
    }
}
