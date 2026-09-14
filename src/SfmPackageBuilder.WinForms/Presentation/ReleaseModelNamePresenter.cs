using SfmPackageBuilder.Core.Planning;

namespace SfmPackageBuilder.WinForms.Presentation;

public sealed class ReleaseModelNamePresenter
{
    public IReadOnlyList<ReleaseModelRenameMapping> PresentMappings(
        string sourceModelPath,
        string releaseStem,
        IEnumerable<PackagePlanEntry> modelEntries,
        Guid? modelEntryId = null)
    {
        var originalStem = Path.GetFileNameWithoutExtension(sourceModelPath);
        if (string.IsNullOrWhiteSpace(originalStem) || string.IsNullOrWhiteSpace(releaseStem)
            || originalStem.Equals(releaseStem.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return Array.Empty<ReleaseModelRenameMapping>();
        }

        var entries = modelEntries
            .Where(entry => entry.EntryType is PackagePlanEntryType.Model or PackagePlanEntryType.ModelCompanion)
            .Where(entry => entry.SourcePath is not null && entry.DestinationRelativePath is not null)
            .Where(entry => modelEntryId is null || entry.ModelEntryId == modelEntryId)
            .ToArray();

        var mappings = new List<ReleaseModelRenameMapping>();
        foreach (var entry in entries)
        {
            var original = Path.GetFileName(entry.SourcePath!);
            var zip = Path.GetFileName(entry.DestinationRelativePath!);
            if (string.IsNullOrWhiteSpace(original) || string.IsNullOrWhiteSpace(zip)
                || original.Equals(zip, StringComparison.OrdinalIgnoreCase)
                || mappings.Any(mapping =>
                    mapping.Original.Equals(original, StringComparison.OrdinalIgnoreCase)
                    && mapping.Zip.Equals(zip, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            mappings.Add(new ReleaseModelRenameMapping(original, zip));
        }

        return mappings;
    }

    public string Present(string sourceModelPath, string releaseStem, IEnumerable<PackagePlanEntry> modelEntries)
    {
        var pairs = PresentMappings(sourceModelPath, releaseStem, modelEntries);
        if (pairs.Count == 0)
        {
            var originalStem = Path.GetFileNameWithoutExtension(sourceModelPath);
            if (string.IsNullOrWhiteSpace(originalStem) || string.IsNullOrWhiteSpace(releaseStem)
                || originalStem.Equals(releaseStem.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return "Release filename: unchanged";
            }

            return "Files renamed in ZIP" + Environment.NewLine +
                "Original".PadRight(34) + "ZIP" + Environment.NewLine +
                Path.GetFileName(sourceModelPath).PadRight(34) + "→  " + releaseStem.Trim() + ".mdl";
        }

        var originalWidth = Math.Min(34, Math.Max("Original files".Length, pairs.Max(pair => pair.Original.Length)));
        var lines = new List<string>
        {
            "Files renamed in ZIP",
            "Original".PadRight(originalWidth + 4) + "ZIP"
        };
        lines.AddRange(pairs.Select(pair => TrimMiddle(pair.Original, originalWidth).PadRight(originalWidth + 4) + "→  " + pair.Zip));

        return string.Join(Environment.NewLine, lines);
    }

    private static string TrimMiddle(string text, int width)
    {
        if (text.Length <= width)
        {
            return text;
        }

        if (width <= 3)
        {
            return text[..width];
        }

        var leftLength = (width - 3) / 2;
        var rightLength = width - 3 - leftLength;
        return text[..leftLength] + "..." + text[^rightLength..];
    }
}

public sealed record ReleaseModelRenameMapping(string Original, string Zip);
