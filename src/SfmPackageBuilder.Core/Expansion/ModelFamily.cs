using SfmPackageBuilder.Core.Domain;

namespace SfmPackageBuilder.Core.Expansion;

public sealed class ModelFamily
{
    public ModelFamily(
        ModelEntry modelEntry,
        ModelFamilyResolutionStatus status,
        string sourceDirectory,
        string sourceStem,
        IReadOnlyList<ModelFamilyFile> files,
        string? failureDetail = null)
    {
        ModelEntry = modelEntry;
        Status = status;
        SourceDirectory = sourceDirectory;
        SourceStem = sourceStem;
        Files = files;
        FailureDetail = failureDetail;
    }

    public ModelEntry ModelEntry { get; }

    public ModelRole Role => ModelEntry.Role;

    public ModelFamilyResolutionStatus Status { get; }

    public string SourceDirectory { get; }

    public string SourceStem { get; }

    public IReadOnlyList<ModelFamilyFile> Files { get; }

    public string? FailureDetail { get; }
}
