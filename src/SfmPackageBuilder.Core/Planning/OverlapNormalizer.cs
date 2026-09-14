using SfmPackageBuilder.Core.Domain;
using SfmPackageBuilder.Core.Paths;

namespace SfmPackageBuilder.Core.Planning;

public sealed class OverlapNormalizer
{
    private readonly PathAnchorDetector anchorDetector = new();

    public IReadOnlyList<SourceEntry> NormalizeMaterialSources(IEnumerable<SourceEntry> sources)
    {
        return Normalize(sources, SourceAnchorKind.Materials, useExtraMapping: false);
    }

    public IReadOnlyList<SourceEntry> NormalizeExtras(IEnumerable<SourceEntry> extras)
    {
        return Normalize(extras, SourceAnchorKind.Materials, useExtraMapping: true);
    }

    private IReadOnlyList<SourceEntry> Normalize(IEnumerable<SourceEntry> entries, SourceAnchorKind anchorKind, bool useExtraMapping)
    {
        var ordered = entries.ToArray();
        var retained = new List<SourceEntry>();

        for (var index = 0; index < ordered.Length; index++)
        {
            var child = ordered[index];
            if (!child.IsFolder || !child.IncludeRecursively)
            {
                retained.Add(child);
                continue;
            }

            var isRedundant = ordered
                .Take(index)
                .Any(parent => IsRedundantChild(parent, child, anchorKind, useExtraMapping));

            if (!isRedundant)
            {
                retained.Add(child);
            }
        }

        return retained;
    }

    private bool IsRedundantChild(SourceEntry parent, SourceEntry child, SourceAnchorKind anchorKind, bool useExtraMapping)
    {
        if (!parent.IsFolder || !parent.IncludeRecursively)
        {
            return false;
        }

        if (!IsContained(parent.SourcePath, child.SourcePath))
        {
            return false;
        }

        var childRelativeToParent = Path.GetRelativePath(parent.SourcePath, child.SourcePath).Replace('/', '\\');
        if (childRelativeToParent == "." || childRelativeToParent.StartsWith("..", StringComparison.Ordinal))
        {
            return false;
        }

        var parentBase = useExtraMapping
            ? ResolveExtraBase(parent)
            : ResolveMaterialBase(parent, anchorKind);
        var childBase = useExtraMapping
            ? ResolveExtraBase(child)
            : ResolveMaterialBase(child, anchorKind);

        if (parentBase is null || childBase is null)
        {
            return false;
        }

        var mappedChildBaseFromParent = CombinePackagePath(parentBase, childRelativeToParent);
        return string.Equals(
            NormalizePackagePath(mappedChildBaseFromParent),
            NormalizePackagePath(childBase),
            StringComparison.OrdinalIgnoreCase);
    }

    private string? ResolveNaturalBase(string sourcePath, SourceAnchorKind anchorKind)
    {
        var resolved = anchorDetector.Resolve(sourcePath, anchorKind);
        return resolved.IsResolved ? resolved.DestinationRelativePath : null;
    }

    private string? ResolveMaterialBase(SourceEntry entry, SourceAnchorKind anchorKind)
    {
        if (entry.DestinationOverride is null)
        {
            return ResolveNaturalBase(entry.SourcePath, anchorKind);
        }

        var validation = DestinationPath.Validate(GetOverrideBase(entry.DestinationOverride));
        return validation.IsValid ? validation.NormalizedPath : null;
    }

    private static string? ResolveExtraBase(SourceEntry entry)
    {
        if (entry.DestinationOverride is null)
        {
            return string.Empty;
        }

        var validation = DestinationPath.Validate(GetOverrideBase(entry.DestinationOverride));
        return validation.IsValid ? validation.NormalizedPath : null;
    }

    private static string GetOverrideBase(DestinationOverride destinationOverride)
    {
        return destinationOverride.Kind switch
        {
            DestinationOverrideKind.Root => destinationOverride.RelativePath,
            DestinationOverrideKind.Misc => string.IsNullOrWhiteSpace(destinationOverride.RelativePath)
                ? "Misc"
                : destinationOverride.RelativePath,
            DestinationOverrideKind.Custom => destinationOverride.RelativePath,
            _ => destinationOverride.RelativePath
        };
    }

    private static bool IsContained(string parentPath, string childPath)
    {
        var parent = NormalizeSourcePath(parentPath).TrimEnd('\\') + "\\";
        var child = NormalizeSourcePath(childPath).TrimEnd('\\') + "\\";
        return child.StartsWith(parent, StringComparison.OrdinalIgnoreCase);
    }

    private static string CombinePackagePath(string basePath, string relativePath)
    {
        return string.IsNullOrEmpty(basePath)
            ? relativePath
            : basePath.TrimEnd('\\') + "\\" + relativePath.TrimStart('\\');
    }

    private static string NormalizePackagePath(string path)
    {
        return string.Join('\\', path.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries));
    }

    private static string NormalizeSourcePath(string path) => path.Replace('/', '\\');
}
