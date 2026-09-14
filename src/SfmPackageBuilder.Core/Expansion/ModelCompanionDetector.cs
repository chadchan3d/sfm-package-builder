using SfmPackageBuilder.Core.Domain;
using SfmPackageBuilder.Core.FileSystem;

namespace SfmPackageBuilder.Core.Expansion;

public sealed class ModelCompanionDetector
{
    private static readonly IReadOnlyList<CompanionRule> DefaultRules = new[]
    {
        new CompanionRule(".vvd"),
        new CompanionRule(".dx90.vtx"),
        new CompanionRule(".dx80.vtx"),
        new CompanionRule(".sw.vtx"),
        new CompanionRule(".phy")
    };

    private readonly IFileSystem fileSystem;
    private readonly IReadOnlyList<CompanionRule> rules;

    public ModelCompanionDetector(IFileSystem fileSystem, IEnumerable<CompanionRule>? rules = null)
    {
        this.fileSystem = fileSystem;
        this.rules = (rules ?? DefaultRules).ToArray();
    }

    public ModelFamily Resolve(ModelEntry modelEntry)
    {
        ArgumentNullException.ThrowIfNull(modelEntry);

        var sourceDirectory = Path.GetDirectoryName(modelEntry.SourceMdlPath) ?? string.Empty;
        var sourceStem = Path.GetFileNameWithoutExtension(modelEntry.SourceMdlPath);
        var files = new List<ModelFamilyFile>
        {
            new(
                ModelFamilyFileKind.SelectedMdl,
                modelEntry.SourceMdlPath,
                ".mdl",
                CompanionUserSelection.Include,
                fileSystem.FileExists(modelEntry.SourceMdlPath)
                    ? SourceObservationStatus.Exists
                    : SourceObservationStatus.Missing)
        };

        if (!fileSystem.FileExists(modelEntry.SourceMdlPath))
        {
            foreach (var savedIntent in modelEntry.Companions.Where(companion => companion.UserSelection == CompanionUserSelection.Include))
            {
                files.Add(new ModelFamilyFile(
                    ModelFamilyFileKind.Companion,
                    savedIntent.SourcePath,
                    savedIntent.RuntimeSuffix,
                    savedIntent.UserSelection,
                    SourceObservationStatus.Missing));
            }

            return new ModelFamily(
                modelEntry,
                ModelFamilyResolutionStatus.MissingSelectedModel,
                sourceDirectory,
                sourceStem,
                files);
        }

        IReadOnlyList<string> directoryFiles;
        try
        {
            directoryFiles = fileSystem.EnumerateFiles(sourceDirectory, recursive: false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new ModelFamily(
                modelEntry,
                ModelFamilyResolutionStatus.EnumerationFailed,
                sourceDirectory,
                sourceStem,
                files,
                ex.Message);
        }

        var filesByName = directoryFiles
            .GroupBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key ?? string.Empty, group => group.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var rule in rules)
        {
            var expectedFileName = sourceStem + rule.RuntimeSuffix;
            var savedIntent = FindSavedIntent(modelEntry.Companions, rule.RuntimeSuffix);
            var userSelection = savedIntent?.UserSelection ?? CompanionUserSelection.Include;
            var sourcePath = filesByName.TryGetValue(expectedFileName, out var detectedPath)
                ? detectedPath
                : savedIntent?.SourcePath ?? Path.Combine(sourceDirectory, expectedFileName);

            if (filesByName.ContainsKey(expectedFileName) || savedIntent?.UserSelection == CompanionUserSelection.Include)
            {
                files.Add(new ModelFamilyFile(
                    ModelFamilyFileKind.Companion,
                    sourcePath,
                    rule.RuntimeSuffix,
                    userSelection,
                    filesByName.ContainsKey(expectedFileName)
                        ? SourceObservationStatus.Exists
                        : SourceObservationStatus.Missing));
            }
        }

        return new ModelFamily(
            modelEntry,
            ModelFamilyResolutionStatus.Resolved,
            sourceDirectory,
            sourceStem,
            files);
    }

    private static ModelCompanionSelection? FindSavedIntent(
        IEnumerable<ModelCompanionSelection> savedCompanions,
        string runtimeSuffix)
    {
        return savedCompanions.FirstOrDefault(
            companion => string.Equals(companion.RuntimeSuffix, runtimeSuffix, StringComparison.OrdinalIgnoreCase));
    }
}
