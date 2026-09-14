using SfmPackageBuilder.Core.Domain;
using SfmPackageBuilder.Core.FileSystem;
using SfmPackageBuilder.Core.Planning;
using SfmPackageBuilder.Core.Validation;

namespace SfmPackageBuilder.Core.Build;

public sealed class PackageCheckService
{
    private readonly PackagePlanner planner;
    private readonly PackageValidator validator;

    public PackageCheckService(IFileSystem fileSystem, IOutputEnvironment outputEnvironment)
        : this(new PackagePlanner(fileSystem), new PackageValidator(fileSystem, outputEnvironment))
    {
    }

    public PackageCheckService(PackagePlanner planner, PackageValidator validator)
    {
        this.planner = planner;
        this.validator = validator;
    }

    public PackagePlan PreviewPackage(PackageProject project)
    {
        ArgumentNullException.ThrowIfNull(project);
        return planner.CreatePlan(project);
    }

    public ReadmePreviewResult PreviewReadme(PackageProject project)
    {
        var plan = PreviewPackage(project);
        var readmeEntry = plan.Entries.SingleOrDefault(entry => entry.EntryType == PackagePlanEntryType.Readme);
        return new ReadmePreviewResult(plan, readmeEntry);
    }

    public PackageCheckResult CheckPackage(PackageProject project, string outputDirectory)
    {
        var plan = PreviewPackage(project);
        var validation = validator.Validate(project, plan, new PackageValidationContext
        {
            OutputDirectory = outputDirectory
        });

        return new PackageCheckResult(plan, validation);
    }
}
