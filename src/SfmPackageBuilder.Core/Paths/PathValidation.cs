namespace SfmPackageBuilder.Core.Paths;

public sealed class PathValidation
{
    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON",
        "PRN",
        "AUX",
        "NUL",
        "COM1",
        "COM2",
        "COM3",
        "COM4",
        "COM5",
        "COM6",
        "COM7",
        "COM8",
        "COM9",
        "LPT1",
        "LPT2",
        "LPT3",
        "LPT4",
        "LPT5",
        "LPT6",
        "LPT7",
        "LPT8",
        "LPT9"
    };

    public PathValidationResult ValidatePackageRelativePath(string relativePath, bool allowEmpty = true)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return allowEmpty
                ? PathValidationResult.Valid(string.Empty)
                : PathValidationResult.Invalid(PathValidationStatus.Empty, "A package-relative path is required.");
        }

        if (IsRootedOrAbsolute(relativePath))
        {
            return PathValidationResult.Invalid(
                PathValidationStatus.RootedPath,
                "Package destinations must be relative paths, not rooted or absolute paths.");
        }

        var rawSegments = WindowsPathSegments.Split(relativePath);
        var normalizedSegments = new List<string>();

        foreach (var segment in rawSegments)
        {
            if (string.IsNullOrEmpty(segment))
            {
                continue;
            }

            if (segment == ".")
            {
                continue;
            }

            if (segment == "..")
            {
                return PathValidationResult.Invalid(
                    PathValidationStatus.Traversal,
                    "Package destinations cannot contain traversal outside the package root.");
            }

            if (ContainsInvalidSegmentCharacters(segment))
            {
                return PathValidationResult.Invalid(
                    PathValidationStatus.InvalidCharacters,
                    "Package destinations cannot contain invalid path characters.");
            }

            if (segment.EndsWith(' ') || segment.EndsWith('.'))
            {
                return PathValidationResult.Invalid(
                    PathValidationStatus.AmbiguousSegment,
                    "Package destination segments cannot end with a space or period.");
            }

            var deviceStem = segment.Split('.', 2)[0];
            if (ReservedNames.Contains(deviceStem))
            {
                return PathValidationResult.Invalid(
                    PathValidationStatus.ReservedDeviceName,
                    "Package destinations cannot use reserved Windows device names.");
            }

            normalizedSegments.Add(segment);
        }

        if (normalizedSegments.Count == 0)
        {
            return allowEmpty
                ? PathValidationResult.Valid(string.Empty)
                : PathValidationResult.Invalid(PathValidationStatus.Empty, "A package-relative path is required.");
        }

        return PathValidationResult.Valid(string.Join('\\', normalizedSegments));
    }

    private static bool IsRootedOrAbsolute(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return true;
        }

        if (path.StartsWith(@"\\", StringComparison.Ordinal) || path.StartsWith("//", StringComparison.Ordinal))
        {
            return true;
        }

        if (path.StartsWith(@"\\?\", StringComparison.Ordinal) || path.StartsWith(@"\\.\", StringComparison.Ordinal))
        {
            return true;
        }

        return path.Length >= 2 && char.IsLetter(path[0]) && path[1] == ':';
    }

    private static bool ContainsInvalidSegmentCharacters(string segment)
    {
        if (segment.Any(char.IsControl))
        {
            return true;
        }

        return segment.IndexOfAny(new[] { '<', '>', ':', '"', '|', '?', '*' }) >= 0
            || segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0;
    }
}
