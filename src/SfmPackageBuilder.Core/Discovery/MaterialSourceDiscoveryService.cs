using SfmPackageBuilder.Core.Domain;
using SfmPackageBuilder.Core.FileSystem;
using SfmPackageBuilder.Core.Mdl;
using SfmPackageBuilder.Core.Persistence;
using SfmPackageBuilder.Core.Paths;

namespace SfmPackageBuilder.Core.Discovery;

public sealed class MaterialSourceDiscoveryService
{
    private readonly IFileSystem fileSystem;
    private readonly IMdlMetadataReader metadataReader;

    public MaterialSourceDiscoveryService(IFileSystem fileSystem, IMdlMetadataReader metadataReader)
    {
        this.fileSystem = fileSystem;
        this.metadataReader = metadataReader;
    }

    public MaterialSourceDiscoveryResult Discover(string selectedModelPath, string? configuredSfmContentFolder)
    {
        var roots = DetermineContentRoots(selectedModelPath, configuredSfmContentFolder).ToArray();
        var readResult = ReadMetadata(selectedModelPath);
        var searchPaths = readResult?.Metadata?.MaterialSearchPaths
            .Select(NormalizeSearchPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? Array.Empty<string>();

        if (searchPaths.Length == 0)
        {
            return new MaterialSourceDiscoveryResult(roots, Array.Empty<SourceEntry>(), readResult?.Status);
        }

        var materialSources = new List<SourceEntry>();
        var seenSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in roots)
        {
            foreach (var searchPath in searchPaths)
            {
                var candidate = BuildCandidate(root, searchPath);
                if (!IsSafeAutomaticMaterialSource(root, candidate)
                    || !seenSources.Add(NormalizeFilesystemPath(candidate))
                    || !fileSystem.DirectoryExists(candidate))
                {
                    continue;
                }

                materialSources.Add(new SourceEntry
                {
                    Kind = SourceEntryKind.Material,
                    SourcePath = candidate,
                    SourceReference = ProjectSourceReferenceService.CreateReferenceForSourcePath(candidate, isFolder: true, SourceAnchorKind.Materials),
                    IsFolder = true,
                    IncludeRecursively = true
                });
            }
        }

        return new MaterialSourceDiscoveryResult(roots, materialSources, readResult?.Status);
    }

    private static bool IsSafeAutomaticMaterialSource(string contentRoot, string candidate)
    {
        var materialRoot = NormalizeFilesystemPath(Path.Combine(contentRoot, "materials"));
        var materialModelsRoot = NormalizeFilesystemPath(Path.Combine(materialRoot, "models"));
        var normalizedCandidate = NormalizeFilesystemPath(candidate);

        return !string.Equals(normalizedCandidate, materialRoot, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(normalizedCandidate, materialModelsRoot, StringComparison.OrdinalIgnoreCase);
    }

    private IEnumerable<string> DetermineContentRoots(string selectedModelPath, string? configuredSfmContentFolder)
    {
        var seenRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var modelRoot = TryInferContentRootFromModelPath(selectedModelPath);
        if (modelRoot is not null && seenRoots.Add(NormalizeFilesystemPath(modelRoot)))
        {
            yield return modelRoot;
        }

        if (!string.IsNullOrWhiteSpace(configuredSfmContentFolder))
        {
            var configuredRoot = NormalizeFilesystemPath(configuredSfmContentFolder);
            if (fileSystem.DirectoryExists(configuredRoot) && seenRoots.Add(configuredRoot))
            {
                yield return configuredRoot;
            }
        }
    }

    private static string? TryInferContentRootFromModelPath(string selectedModelPath)
    {
        if (string.IsNullOrWhiteSpace(selectedModelPath))
        {
            return null;
        }

        var fullPath = NormalizeFilesystemPath(selectedModelPath);
        var segments = WindowsPathSegments.Split(fullPath);
        var modelsIndex = -1;
        for (var index = 0; index < segments.Count; index++)
        {
            if (string.Equals(segments[index], "models", StringComparison.OrdinalIgnoreCase))
            {
                modelsIndex = index;
            }
        }

        if (modelsIndex <= 0)
        {
            return null;
        }

        var root = string.Join(Path.DirectorySeparatorChar, segments.Take(modelsIndex));
        if (fullPath.StartsWith(@"\\", StringComparison.Ordinal) && !root.StartsWith(@"\\", StringComparison.Ordinal))
        {
            root = @"\\" + root;
        }

        return root;
    }

    private MdlMetadataReadResult? ReadMetadata(string selectedModelPath)
    {
        if (string.IsNullOrWhiteSpace(selectedModelPath))
        {
            return null;
        }

        try
        {
            return metadataReader.Read(selectedModelPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FileNotFoundException)
        {
            return null;
        }
    }

    private static string BuildCandidate(string contentRoot, string normalizedSearchPath)
    {
        var parts = new List<string> { NormalizeFilesystemPath(contentRoot), "materials" };
        parts.AddRange(normalizedSearchPath.Split('\\', StringSplitOptions.RemoveEmptyEntries));
        return Path.Combine(parts.ToArray());
    }

    private static string NormalizeSearchPath(string value)
    {
        var normalized = string.Join('\\', value
            .Replace('/', '\\')
            .Split('\\', StringSplitOptions.RemoveEmptyEntries)
            .Where(part => part != "."));
        if (normalized.StartsWith(@"materials\", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[@"materials\".Length..];
        }

        return normalized.TrimEnd('\\');
    }

    private static string NormalizeFilesystemPath(string path) =>
        Path.GetFullPath(path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar))
            .TrimEnd(Path.DirectorySeparatorChar);
}
