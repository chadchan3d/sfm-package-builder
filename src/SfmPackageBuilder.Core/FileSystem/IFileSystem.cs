namespace SfmPackageBuilder.Core.FileSystem;

public interface IFileSystem
{
    bool FileExists(string path);

    bool DirectoryExists(string path);

    Stream OpenRead(string path);

    IReadOnlyList<string> EnumerateFiles(string directoryPath, bool recursive);

    IReadOnlyList<string> EnumerateDirectories(string directoryPath, bool recursive);

    FileAttributes GetAttributes(string path);

    void CopyFile(string sourcePath, string destinationPath, bool overwrite);
}
