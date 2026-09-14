using SfmPackageBuilder.Core.FileSystem;

namespace SfmPackageBuilder.Core.Tests;

internal sealed class FakeFileSystem : IFileSystem
{
    private readonly Dictionary<string, byte[]> files = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> directories = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> enumerationFailures = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> readFailures = new(StringComparer.OrdinalIgnoreCase);

    public int CopyFileCallCount { get; private set; }

    public void AddFile(string path, byte[] contents)
    {
        files[Normalize(path)] = contents;

        var directory = Path.GetDirectoryName(path);
        while (!string.IsNullOrEmpty(directory))
        {
            directories.Add(Normalize(directory));
            directory = Path.GetDirectoryName(directory);
        }
    }

    public void RemoveFile(string path)
    {
        files.Remove(Normalize(path));
    }

    public void AddDirectory(string path)
    {
        directories.Add(Normalize(path));
    }

    public void FailEnumeration(string directoryPath)
    {
        enumerationFailures.Add(Normalize(directoryPath));
    }

    public void FailRead(string path)
    {
        readFailures.Add(Normalize(path));
    }

    public bool FileExists(string path) => files.ContainsKey(Normalize(path));

    public bool DirectoryExists(string path) => directories.Contains(Normalize(path));

    public Stream OpenRead(string path)
    {
        if (readFailures.Contains(Normalize(path)))
        {
            throw new UnauthorizedAccessException("Simulated read failure.");
        }

        return files.TryGetValue(Normalize(path), out var contents)
            ? new MemoryStream(contents, writable: false)
            : throw new FileNotFoundException("The fake file was not found.", path);
    }

    public IReadOnlyList<string> EnumerateFiles(string directoryPath, bool recursive)
    {
        var normalizedDirectory = Normalize(directoryPath).TrimEnd('\\');
        if (enumerationFailures.Contains(normalizedDirectory))
        {
            throw new UnauthorizedAccessException("Simulated enumeration failure.");
        }

        var prefix = normalizedDirectory + "\\";

        return files.Keys
            .Where(path => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Where(path => recursive || !path[prefix.Length..].Contains('\\', StringComparison.Ordinal))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyList<string> EnumerateDirectories(string directoryPath, bool recursive)
    {
        var normalizedDirectory = Normalize(directoryPath).TrimEnd('\\');
        if (enumerationFailures.Contains(normalizedDirectory))
        {
            throw new UnauthorizedAccessException("Simulated enumeration failure.");
        }

        var prefix = normalizedDirectory + "\\";

        return directories
            .Where(path => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Where(path => recursive || !path[prefix.Length..].Contains('\\', StringComparison.Ordinal))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public FileAttributes GetAttributes(string path)
    {
        if (FileExists(path))
        {
            return FileAttributes.Archive;
        }

        if (DirectoryExists(path))
        {
            return FileAttributes.Directory;
        }

        throw new FileNotFoundException("The fake path was not found.", path);
    }

    public void CopyFile(string sourcePath, string destinationPath, bool overwrite)
    {
        CopyFileCallCount++;

        var source = Normalize(sourcePath);
        var destination = Normalize(destinationPath);

        if (!files.TryGetValue(source, out var contents))
        {
            throw new FileNotFoundException("The fake source file was not found.", sourcePath);
        }

        if (!overwrite && files.ContainsKey(destination))
        {
            throw new IOException("The fake destination file already exists.");
        }

        AddFile(destination, contents.ToArray());
    }

    private static string Normalize(string path) => path.Replace('/', '\\').TrimEnd('\\');
}
