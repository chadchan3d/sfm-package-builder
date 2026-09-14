namespace SfmPackageBuilder.Core.Planning;

internal sealed class DraftPackagePlan
{
    public DraftPackagePlan(IReadOnlyList<PackagePlanEntry> ordinaryEntries)
    {
        OrdinaryEntries = ordinaryEntries.ToArray();
    }

    public IReadOnlyList<PackagePlanEntry> OrdinaryEntries { get; }
}
