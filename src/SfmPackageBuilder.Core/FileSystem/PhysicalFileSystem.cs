namespace SfmPackageBuilder.Core.FileSystem;

public sealed class PhysicalFileSystem : IFileSystem
{
    public bool FileExists(string path) => File.Exists(path);

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public Stream OpenRead(string path) => File.OpenRead(path);

    public IReadOnlyList<string> EnumerateFiles(string directoryPath, bool recursive)
    {
        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return Directory.EnumerateFiles(directoryPath, "*", option).ToList();
    }

    public IReadOnlyList<string> EnumerateDirectories(string directoryPath, bool recursive)
    {
        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return Directory.EnumerateDirectories(directoryPath, "*", option).ToList();
    }

    public FileAttributes GetAttributes(string path) => File.GetAttributes(path);

    public void CopyFile(string sourcePath, string destinationPath, bool overwrite)
    {
        File.Copy(sourcePath, destinationPath, overwrite);
    }
}
