namespace SfmPackageBuilder.WinForms.Presentation;

public sealed class BrowseStartResolver
{
    public string ResolveMaterialsStart(
        string lastRelevantFolder,
        string defaultSfmContentFolder,
        Func<string, bool> directoryExists)
    {
        ArgumentNullException.ThrowIfNull(directoryExists);

        var materialModelsRoot = CombineIfPresent(defaultSfmContentFolder, "materials", "models");
        var materialRoot = CombineIfPresent(defaultSfmContentFolder, "materials");
        var materialBrowseHistory = IsSameDirectory(lastRelevantFolder, materialRoot)
            ? null
            : lastRelevantFolder;

        foreach (var candidate in new[]
        {
            materialBrowseHistory,
            materialModelsRoot,
            materialRoot,
            defaultSfmContentFolder
        })
        {
            if (!string.IsNullOrWhiteSpace(candidate) && directoryExists(candidate))
            {
                return candidate;
            }
        }

        return string.Empty;
    }

    public string ResolveSourceStart(
        string currentSourcePath,
        string lastRelevantFolder,
        string defaultSfmContentFolder,
        Func<string, bool> directoryExists)
    {
        ArgumentNullException.ThrowIfNull(directoryExists);

        foreach (var candidate in new[]
        {
            GetExistingSourceDirectory(currentSourcePath, directoryExists),
            lastRelevantFolder,
            CombineIfPresent(defaultSfmContentFolder, "models"),
            defaultSfmContentFolder
        })
        {
            if (!string.IsNullOrWhiteSpace(candidate) && directoryExists(candidate))
            {
                return candidate;
            }
        }

        return string.Empty;
    }

    private static string? GetExistingSourceDirectory(string sourcePath, Func<string, bool> directoryExists)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return null;
        }

        var directory = Path.GetDirectoryName(sourcePath);
        return !string.IsNullOrWhiteSpace(directory) && directoryExists(directory)
            ? directory
            : null;
    }

    private static string? CombineIfPresent(string root, params string[] segments) =>
        string.IsNullOrWhiteSpace(root)
            ? null
            : Path.Combine(new[] { root }.Concat(segments).ToArray());

    private static bool IsSameDirectory(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        return string.Equals(
            NormalizeDirectoryForComparison(left),
            NormalizeDirectoryForComparison(right),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeDirectoryForComparison(string path)
    {
        try
        {
            path = Path.GetFullPath(path);
        }
        catch (ArgumentException)
        {
        }
        catch (NotSupportedException)
        {
        }
        catch (PathTooLongException)
        {
        }

        return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
