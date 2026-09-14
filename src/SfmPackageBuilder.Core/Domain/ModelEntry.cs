namespace SfmPackageBuilder.Core.Domain;

public sealed class ModelEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public ModelRole Role { get; set; } = ModelRole.Additional;

    public string SourceMdlPath { get; set; } = string.Empty;

    public PersistedSourceReference? SourceReference { get; set; }

    public string SourceStem { get; set; } = string.Empty;

    public string ReleaseStem { get; set; } = string.Empty;

    public List<ModelCompanionSelection> Companions { get; set; } = new();

    public DestinationOverride? DestinationOverride { get; set; }
}
