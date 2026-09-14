namespace SfmPackageBuilder.Core.Mdl;

public sealed class MdlV49Metadata
{
    public MdlV49Metadata(
        string magic,
        int version,
        int checksum,
        string? rawHeaderModelName,
        int declaredLength,
        IReadOnlyList<string> textureReferences,
        IReadOnlyList<string> materialSearchPaths,
        MdlSkinTable? skinTable)
    {
        Magic = magic;
        Version = version;
        Checksum = checksum;
        RawHeaderModelName = rawHeaderModelName;
        DeclaredLength = declaredLength;
        TextureReferences = textureReferences.ToArray();
        MaterialSearchPaths = materialSearchPaths.ToArray();
        SkinTable = skinTable;
    }

    public string Magic { get; }

    public int Version { get; }

    public int Checksum { get; }

    public string? RawHeaderModelName { get; }

    public int DeclaredLength { get; }

    public IReadOnlyList<string> TextureReferences { get; }

    public IReadOnlyList<string> MaterialSearchPaths { get; }

    public MdlSkinTable? SkinTable { get; }
}
