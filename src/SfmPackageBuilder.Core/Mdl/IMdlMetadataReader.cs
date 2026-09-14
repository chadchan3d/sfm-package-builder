namespace SfmPackageBuilder.Core.Mdl;

public interface IMdlMetadataReader
{
    MdlMetadataReadResult Read(string path);
}
