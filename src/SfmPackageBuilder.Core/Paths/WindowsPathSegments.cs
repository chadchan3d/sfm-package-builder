namespace SfmPackageBuilder.Core.Paths;

internal static class WindowsPathSegments
{
    public static IReadOnlyList<string> Split(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return path
            .Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(segment => segment.Length > 0)
            .ToArray();
    }
}
