namespace SfmPackageBuilder.Core.Domain;

public sealed class ProjectCloneService
{
    public PackageProject CreateNewProject(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new PackageProject
        {
            Readme = new ReadmeConfig
            {
                Author = settings.CreatorDefaults.Author,
                Website = settings.CreatorDefaults.Website,
                License = settings.CreatorDefaults.License
            }
        };
    }

    public PackageProject CreateFromCurrent(PackageProject current)
    {
        ArgumentNullException.ThrowIfNull(current);

        return new PackageProject
        {
            MaterialSources = current.MaterialSources.Select(CloneSource).ToList(),
            Extras = current.Extras.Select(CloneSource).ToList(),
            Readme = CloneReusableReadme(current.Readme)
        };
    }

    private static SourceEntry CloneSource(SourceEntry source) =>
        new()
        {
            Kind = source.Kind,
            SourcePath = source.SourcePath,
            IsFolder = source.IsFolder,
            IncludeRecursively = source.IncludeRecursively,
            NaturalDestination = source.NaturalDestination,
            SourceReference = CloneSourceReference(source.SourceReference),
            DestinationOverride = CloneDestination(source.DestinationOverride)
        };

    private static PersistedSourceReference? CloneSourceReference(PersistedSourceReference? sourceReference) =>
        sourceReference is null
            ? null
            : new PersistedSourceReference
            {
                AbsolutePath = sourceReference.AbsolutePath,
                RelativePath = sourceReference.RelativePath,
                RecoveryRootPath = sourceReference.RecoveryRootPath,
                RecoveryRelativePath = sourceReference.RecoveryRelativePath
            };

    private static DestinationOverride? CloneDestination(DestinationOverride? destination) =>
        destination is null
            ? null
            : new DestinationOverride
            {
                Kind = destination.Kind,
                RelativePath = destination.RelativePath,
                Reason = destination.Reason
            };

    private static ReadmeConfig CloneReusableReadme(ReadmeConfig readme)
    {
        var clone = new ReadmeConfig
        {
            Mode = readme.Mode,
            CustomSource = readme.CustomSource,
            Author = readme.Author,
            Website = readme.Website,
            License = readme.License,
            AdditionalResources = readme.AdditionalResources,
            IncludeInstallInstructions = readme.IncludeInstallInstructions,
            IncludeModelPath = readme.IncludeModelPath,
            IncludeVersionChanges = readme.IncludeVersionChanges
        };

        if (readme.Mode == ReadmeMode.Custom && readme.CustomSource == ReadmeCustomSource.ProjectText)
        {
            clone.CustomReadmeText = string.Empty;
        }

        return clone;
    }
}
