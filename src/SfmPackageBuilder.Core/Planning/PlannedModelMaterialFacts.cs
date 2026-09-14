using SfmPackageBuilder.Core.Domain;
using SfmPackageBuilder.Core.Mdl;

namespace SfmPackageBuilder.Core.Planning;

public sealed class PlannedModelMaterialFacts
{
    public PlannedModelMaterialFacts(
        Guid modelEntryId,
        ModelRole modelRole,
        string sourceMdlPath,
        string? modelDestinationRelativePath,
        MdlMetadataReadStatus readStatus,
        int? mdlVersion,
        int? checksum,
        string? rawHeaderModelName,
        IReadOnlyList<string> textureReferences,
        IReadOnlyList<string> materialSearchPaths,
        MdlSkinTable? skinTable,
        IReadOnlyList<PlannedModelMaterialDiagnostic> diagnostics)
    {
        ModelEntryId = modelEntryId;
        ModelRole = modelRole;
        SourceMdlPath = sourceMdlPath;
        ModelDestinationRelativePath = modelDestinationRelativePath;
        ReadStatus = readStatus;
        MdlVersion = mdlVersion;
        Checksum = checksum;
        RawHeaderModelName = rawHeaderModelName;
        TextureReferences = textureReferences.ToArray();
        MaterialSearchPaths = materialSearchPaths.ToArray();
        SkinTable = skinTable;
        Diagnostics = diagnostics.ToArray();
    }

    public Guid ModelEntryId { get; }

    public ModelRole ModelRole { get; }

    public string SourceMdlPath { get; }

    public string? ModelDestinationRelativePath { get; }

    public MdlMetadataReadStatus ReadStatus { get; }

    public int? MdlVersion { get; }

    public int? Checksum { get; }

    public string? RawHeaderModelName { get; }

    public IReadOnlyList<string> TextureReferences { get; }

    public IReadOnlyList<string> MaterialSearchPaths { get; }

    public MdlSkinTable? SkinTable { get; }

    public IReadOnlyList<PlannedModelMaterialDiagnostic> Diagnostics { get; }
}
