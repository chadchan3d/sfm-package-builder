namespace SfmPackageBuilder.Core.Domain;

public sealed class SourceEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public SourceEntryKind Kind { get; set; }

    public string SourcePath { get; set; } = string.Empty;

    public PersistedSourceReference? SourceReference { get; set; }

    public bool IsFolder { get; set; }

    public bool IncludeRecursively { get; set; } = true;

    public string? NaturalDestination { get; set; }

    public DestinationOverride? DestinationOverride { get; set; }

    public List<Guid> AutomaticMaterialModelIds { get; set; } = new();
}
