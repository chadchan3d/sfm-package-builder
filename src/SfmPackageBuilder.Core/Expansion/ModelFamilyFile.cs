using SfmPackageBuilder.Core.Domain;

namespace SfmPackageBuilder.Core.Expansion;

public sealed class ModelFamilyFile
{
    public ModelFamilyFile(
        ModelFamilyFileKind kind,
        string sourcePath,
        string runtimeSuffix,
        CompanionUserSelection userSelection,
        SourceObservationStatus observationStatus)
    {
        Kind = kind;
        SourcePath = sourcePath;
        RuntimeSuffix = runtimeSuffix;
        UserSelection = userSelection;
        ObservationStatus = observationStatus;
    }

    public ModelFamilyFileKind Kind { get; }

    public string SourcePath { get; }

    public string RuntimeSuffix { get; }

    public CompanionUserSelection UserSelection { get; }

    public SourceObservationStatus ObservationStatus { get; }

    public bool ShouldInclude => UserSelection == CompanionUserSelection.Include;
}
