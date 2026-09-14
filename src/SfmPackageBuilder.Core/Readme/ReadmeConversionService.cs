using SfmPackageBuilder.Core.Domain;

namespace SfmPackageBuilder.Core.Readme;

public sealed class ReadmeConversionService
{
    private readonly ReadmeFactsFingerprint fingerprint = new();

    public ReadmeConfig UseGeneratedTextAsCustomReadme(
        ReadmeConfig currentConfig,
        string generatedReadmeText,
        string? generatedFactsFingerprint = null)
    {
        ArgumentNullException.ThrowIfNull(currentConfig);
        ArgumentNullException.ThrowIfNull(generatedReadmeText);

        return new ReadmeConfig
        {
            Mode = ReadmeMode.Custom,
            CustomSource = ReadmeCustomSource.ProjectText,
            Description = currentConfig.Description,
            Author = currentConfig.Author,
            Website = currentConfig.Website,
            License = currentConfig.License,
            AdditionalResources = currentConfig.AdditionalResources,
            IncludeInstallInstructions = currentConfig.IncludeInstallInstructions,
            IncludeModelPath = currentConfig.IncludeModelPath,
            IncludeVersionChanges = currentConfig.IncludeVersionChanges,
            ImportedReadmePath = null,
            ImportedReadmeReference = null,
            CustomReadmeText = generatedReadmeText,
            CustomReadmeGeneratedFromFingerprint = generatedFactsFingerprint ?? fingerprint.Create(generatedReadmeText)
        };
    }
}
