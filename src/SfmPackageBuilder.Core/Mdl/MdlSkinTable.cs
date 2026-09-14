namespace SfmPackageBuilder.Core.Mdl;

public sealed class MdlSkinTable
{
    public MdlSkinTable(int materialSlotCount, int skinFamilyCount, IReadOnlyList<IReadOnlyList<short>> remapMatrix)
    {
        MaterialSlotCount = materialSlotCount;
        SkinFamilyCount = skinFamilyCount;
        RemapMatrix = remapMatrix
            .Select(row => (IReadOnlyList<short>)row.ToArray())
            .ToArray();
    }

    public int MaterialSlotCount { get; }

    public int SkinFamilyCount { get; }

    public IReadOnlyList<IReadOnlyList<short>> RemapMatrix { get; }
}
