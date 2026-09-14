namespace SfmPackageBuilder.Core.Readme;

public sealed class ReadmePreviewService
{
    private readonly ReadmeResolver resolver;

    public ReadmePreviewService(ReadmeResolver resolver)
    {
        this.resolver = resolver;
    }

    public ResolvedReadme ResolvePreview(Domain.PackageProject project, ReadmeContext context)
    {
        return resolver.Resolve(project, context);
    }
}
