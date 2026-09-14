using SfmPackageBuilder.Core.Paths;

namespace SfmPackageBuilder.Core.Staging;

internal sealed class StagingPath
{
    private StagingPath(string normalizedDestination, string fullPath)
    {
        NormalizedDestination = normalizedDestination;
        FullPath = fullPath;
    }

    public string NormalizedDestination { get; }

    public string FullPath { get; }

    public static bool TryCreate(
        string stagingRoot,
        string? destinationRelativePath,
        out StagingPath? stagingPath,
        out StagingFailureKind failureKind,
        out string failureMessage,
        out string? technicalDetail)
    {
        stagingPath = null;
        technicalDetail = null;

        if (string.IsNullOrWhiteSpace(destinationRelativePath))
        {
            failureKind = StagingFailureKind.MissingDestination;
            failureMessage = "The plan entry does not have a package destination.";
            return false;
        }

        var validation = DestinationPath.Validate(destinationRelativePath, allowEmpty: false);
        if (!validation.IsValid)
        {
            failureKind = StagingFailureKind.UnsafeDestination;
            failureMessage = "The package destination is not safe to stage.";
            technicalDetail = validation.Message;
            return false;
        }

        var rootFullPath = EnsureTrailingSeparator(Path.GetFullPath(stagingRoot));
        var candidate = Path.GetFullPath(Path.Combine(rootFullPath, validation.NormalizedPath!));
        var relative = Path.GetRelativePath(rootFullPath, candidate);
        if (relative.StartsWith("..", StringComparison.Ordinal)
            || Path.IsPathRooted(relative)
            || relative.Length == 0)
        {
            failureKind = StagingFailureKind.UnsafeDestination;
            failureMessage = "The package destination resolves outside the staging root.";
            technicalDetail = $"Resolved destination: {candidate}";
            return false;
        }

        failureKind = default;
        failureMessage = string.Empty;
        stagingPath = new StagingPath(validation.NormalizedPath!, candidate);
        return true;
    }

    private static string EnsureTrailingSeparator(string path) =>
        path.EndsWith(Path.DirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
}
