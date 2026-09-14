using SfmPackageBuilder.Core.Domain;

namespace SfmPackageBuilder.WinForms.Presentation;

public sealed class ArchiveNameSuggester
{
    public string Suggest(string assetName, string version, AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(assetName) || string.IsNullOrWhiteSpace(version))
        {
            return "Package.zip";
        }

        var safeAsset = MakeSafeToken(assetName);
        var safeVersion = MakeSafeToken(version);
        var pattern = string.IsNullOrWhiteSpace(settings.DefaultArchivePattern)
            ? "{AssetName}_v{Version}.zip"
            : settings.DefaultArchivePattern;
        var archive = pattern
            .Replace("{AssetName}", safeAsset, StringComparison.OrdinalIgnoreCase)
            .Replace("{Version}", safeVersion, StringComparison.OrdinalIgnoreCase);
        if (archive.Contains('{', StringComparison.Ordinal) || archive.Contains('}', StringComparison.Ordinal))
        {
            return "Package.zip";
        }

        archive = MakeSafeFileName(archive);
        if (string.IsNullOrWhiteSpace(archive))
        {
            return "Package.zip";
        }

        return archive.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
            ? archive
            : archive + ".zip";
    }

    private static string MakeSafeToken(string text)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string((text ?? string.Empty)
            .Trim()
            .Select(ch => invalid.Contains(ch) ? '_' : ch)
            .ToArray());
        cleaned = string.Join("_", cleaned.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(cleaned) ? "Package" : cleaned;
    }

    private static string MakeSafeFileName(string text)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string((text ?? string.Empty)
            .Trim()
            .Select(ch => invalid.Contains(ch) ? '_' : ch)
            .ToArray());
        return string.IsNullOrWhiteSpace(cleaned.Trim('.', ' ')) ? string.Empty : cleaned;
    }
}
