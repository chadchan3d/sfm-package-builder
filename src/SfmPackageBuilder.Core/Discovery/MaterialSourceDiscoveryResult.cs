using SfmPackageBuilder.Core.Domain;
using SfmPackageBuilder.Core.Mdl;

namespace SfmPackageBuilder.Core.Discovery;

public sealed class MaterialSourceDiscoveryResult
{
    public MaterialSourceDiscoveryResult(
        IReadOnlyList<string> contentRoots,
        IReadOnlyList<SourceEntry> materialSources,
        MdlMetadataReadStatus? metadataStatus)
    {
        ContentRoots = contentRoots.ToArray();
        MaterialSources = materialSources.ToArray();
        MetadataStatus = metadataStatus;
    }

    public IReadOnlyList<string> ContentRoots { get; }

    public IReadOnlyList<SourceEntry> MaterialSources { get; }

    public MdlMetadataReadStatus? MetadataStatus { get; }
}
