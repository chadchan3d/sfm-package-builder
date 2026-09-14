using SfmPackageBuilder.Core.Domain;

namespace SfmPackageBuilder.Core.Expansion;

public sealed class SourceInventoryItem
{
    public SourceInventoryItem(
        SourceEntry origin,
        string sourcePath,
        string relativePathWithinSelection,
        SourceObservationStatus observationStatus)
    {
        Origin = origin;
        SourcePath = sourcePath;
        RelativePathWithinSelection = relativePathWithinSelection;
        ObservationStatus = observationStatus;
    }

    public SourceEntry Origin { get; }

    public string SourcePath { get; }

    public string RelativePathWithinSelection { get; }

    public SourceObservationStatus ObservationStatus { get; }
}
