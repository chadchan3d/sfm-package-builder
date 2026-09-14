using SfmPackageBuilder.Core.SharedFiles;

namespace SfmPackageBuilder.Core.Planning;

public sealed class PackagePlan
{
    public PackagePlan(
        IReadOnlyList<PackagePlanEntry> entries,
        IReadOnlyList<SharedFileNotice> sharedFileNotices,
        IReadOnlyList<PlannedModelMaterialFacts>? modelMaterialFacts = null)
    {
        Entries = entries.ToArray();
        SharedFileNotices = sharedFileNotices.ToArray();
        ModelMaterialFacts = (modelMaterialFacts ?? Array.Empty<PlannedModelMaterialFacts>()).ToArray();
    }

    public IReadOnlyList<PackagePlanEntry> Entries { get; }

    public IReadOnlyList<SharedFileNotice> SharedFileNotices { get; }

    public IReadOnlyList<PlannedModelMaterialFacts> ModelMaterialFacts { get; }

    public IReadOnlyList<PackagePlanEntry> ResolvedEntries =>
        Entries.Where(entry => entry.Status == PackagePlanEntryStatus.Resolved).ToArray();
}
