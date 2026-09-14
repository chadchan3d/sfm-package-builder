using SfmPackageBuilder.Core.Domain;
using SfmPackageBuilder.Core.FileSystem;

namespace SfmPackageBuilder.Core.Expansion;

public sealed class FolderExpander
{
    private readonly IFileSystem fileSystem;

    public FolderExpander(IFileSystem fileSystem)
    {
        this.fileSystem = fileSystem;
    }

    public SourceInventory Resolve(SourceEntry sourceEntry)
    {
        ArgumentNullException.ThrowIfNull(sourceEntry);

        if (!sourceEntry.IsFolder)
        {
            return ResolveFile(sourceEntry);
        }

        if (!fileSystem.DirectoryExists(sourceEntry.SourcePath))
        {
            return new SourceInventory(
                sourceEntry,
                SourceInventoryStatus.MissingSelectedSource,
                Array.Empty<SourceInventoryItem>());
        }

        IReadOnlyList<string> files;
        try
        {
            files = fileSystem.EnumerateFiles(sourceEntry.SourcePath, sourceEntry.IncludeRecursively);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new SourceInventory(
                sourceEntry,
                SourceInventoryStatus.EnumerationFailed,
                Array.Empty<SourceInventoryItem>(),
                ex.Message);
        }

        var items = files
            .Select(path => new SourceInventoryItem(
                sourceEntry,
                path,
                GetRelativePath(sourceEntry.SourcePath, path),
                SourceObservationStatus.Exists))
            .ToArray();

        return new SourceInventory(sourceEntry, SourceInventoryStatus.Resolved, items);
    }

    private SourceInventory ResolveFile(SourceEntry sourceEntry)
    {
        if (!fileSystem.FileExists(sourceEntry.SourcePath))
        {
            return new SourceInventory(
                sourceEntry,
                SourceInventoryStatus.MissingSelectedSource,
                new[]
                {
                    new SourceInventoryItem(
                        sourceEntry,
                        sourceEntry.SourcePath,
                        Path.GetFileName(sourceEntry.SourcePath),
                        SourceObservationStatus.Missing)
                });
        }

        try
        {
            using var stream = fileSystem.OpenRead(sourceEntry.SourcePath);
            _ = stream.CanRead;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new SourceInventory(
                sourceEntry,
                SourceInventoryStatus.ReadFailed,
                new[]
                {
                    new SourceInventoryItem(
                        sourceEntry,
                        sourceEntry.SourcePath,
                        Path.GetFileName(sourceEntry.SourcePath),
                        SourceObservationStatus.ReadFailed)
                },
                ex.Message);
        }

        return new SourceInventory(
            sourceEntry,
            SourceInventoryStatus.Resolved,
            new[]
            {
                new SourceInventoryItem(
                    sourceEntry,
                    sourceEntry.SourcePath,
                    Path.GetFileName(sourceEntry.SourcePath),
                    SourceObservationStatus.Exists)
            });
    }

    private static string GetRelativePath(string rootPath, string childPath)
    {
        return Path.GetRelativePath(rootPath, childPath).Replace('/', '\\');
    }
}
