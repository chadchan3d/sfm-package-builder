namespace SfmPackageBuilder.Core.Expansion;

public sealed class SourceExpansionResult
{
    public SourceExpansionResult(
        IReadOnlyList<ModelFamily> modelFamilies,
        IReadOnlyList<SourceInventory> materialInventories,
        IReadOnlyList<SourceInventory> extraInventories)
    {
        ModelFamilies = modelFamilies;
        MaterialInventories = materialInventories;
        ExtraInventories = extraInventories;
    }

    public IReadOnlyList<ModelFamily> ModelFamilies { get; }

    public IReadOnlyList<SourceInventory> MaterialInventories { get; }

    public IReadOnlyList<SourceInventory> ExtraInventories { get; }
}
