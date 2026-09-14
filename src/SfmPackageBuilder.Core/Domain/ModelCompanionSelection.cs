namespace SfmPackageBuilder.Core.Domain;

public sealed class ModelCompanionSelection
{
    public string SourcePath { get; set; } = string.Empty;

    public PersistedSourceReference? SourceReference { get; set; }

    public string RuntimeSuffix { get; set; } = string.Empty;

    public CompanionUserSelection UserSelection { get; set; } = CompanionUserSelection.Include;
}
