namespace SfmPackageBuilder.Core.Validation;

public interface IOutputEnvironment
{
    bool DirectoryExists(string path);

    bool FileExists(string path);

    OutputProbeResult CanWriteToDirectory(string path);
}
