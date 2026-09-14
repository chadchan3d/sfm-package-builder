using SfmPackageBuilder.Core.Domain;
using SfmPackageBuilder.Core.FileSystem;

namespace SfmPackageBuilder.Core.Expansion;

public sealed class SourceExpansionService
{
    private readonly ModelCompanionDetector modelCompanionDetector;
    private readonly FolderExpander folderExpander;

    public SourceExpansionService(IFileSystem fileSystem, IEnumerable<CompanionRule>? companionRules = null)
    {
        modelCompanionDetector = new ModelCompanionDetector(fileSystem, companionRules);
        folderExpander = new FolderExpander(fileSystem);
    }

    public SourceExpansionResult Resolve(PackageProject project)
    {
        ArgumentNullException.ThrowIfNull(project);

        var modelFamilies = project.Models
            .Select(modelCompanionDetector.Resolve)
            .ToArray();
        var materialInventories = project.MaterialSources
            .Select(folderExpander.Resolve)
            .ToArray();
        var extraInventories = project.Extras
            .Select(folderExpander.Resolve)
            .ToArray();

        return new SourceExpansionResult(modelFamilies, materialInventories, extraInventories);
    }

    public ModelFamily ResolveModel(ModelEntry modelEntry) => modelCompanionDetector.Resolve(modelEntry);

    public SourceInventory ResolveSource(SourceEntry sourceEntry) => folderExpander.Resolve(sourceEntry);
}
