namespace SfmPackageBuilder.Core.Paths;

public sealed class PathAnchorDetector
{
    public ResolvedAnchor Resolve(string sourcePath, SourceAnchorKind anchorKind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        var anchorSegment = GetAnchorSegment(anchorKind);
        var segments = WindowsPathSegments.Split(sourcePath);
        var candidateIndexes = new List<int>();

        for (var index = 0; index < segments.Count; index++)
        {
            if (string.Equals(segments[index], anchorSegment, StringComparison.OrdinalIgnoreCase))
            {
                candidateIndexes.Add(index);
            }
        }

        if (candidateIndexes.Count == 0)
        {
            return ResolvedAnchor.Missing(sourcePath, anchorKind, anchorSegment);
        }

        var selectedIndex = candidateIndexes[^1];
        var destinationSegments = segments.Skip(selectedIndex).ToArray();
        var destination = string.Join('\\', destinationSegments);

        return ResolvedAnchor.Resolved(
            sourcePath,
            anchorKind,
            segments[selectedIndex],
            selectedIndex,
            candidateIndexes,
            destination);
    }

    private static string GetAnchorSegment(SourceAnchorKind anchorKind)
    {
        return anchorKind switch
        {
            SourceAnchorKind.Models => "models",
            SourceAnchorKind.Materials => "materials",
            _ => throw new ArgumentOutOfRangeException(nameof(anchorKind), anchorKind, null)
        };
    }
}
