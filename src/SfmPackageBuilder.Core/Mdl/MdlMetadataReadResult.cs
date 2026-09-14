namespace SfmPackageBuilder.Core.Mdl;

public sealed class MdlMetadataReadResult
{
    public MdlMetadataReadResult(
        MdlMetadataReadStatus status,
        MdlV49Metadata? metadata,
        IReadOnlyList<MdlMetadataDiagnostic> diagnostics)
    {
        Status = status;
        Metadata = metadata;
        Diagnostics = diagnostics.ToArray();
    }

    public MdlMetadataReadStatus Status { get; }

    public MdlV49Metadata? Metadata { get; }

    public IReadOnlyList<MdlMetadataDiagnostic> Diagnostics { get; }

    public bool HasMetadata => Metadata is not null;
}
