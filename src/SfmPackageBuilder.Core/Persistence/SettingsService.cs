using System.Text.Json;
using System.Text.Json.Serialization;
using SfmPackageBuilder.Core.Domain;

namespace SfmPackageBuilder.Core.Persistence;

public sealed class SettingsService
{
    public const int CurrentSchemaVersion = 1;
    private readonly string settingsPath;
    private readonly JsonSerializerOptions options;

    public SettingsService(string? settingsPath = null)
    {
        this.settingsPath = settingsPath ?? GetDefaultSettingsPath();
        options = CreateOptions();
    }

    public string SettingsPath => settingsPath;

    public SettingsLoadResult Load()
    {
        if (!File.Exists(settingsPath))
        {
            return SettingsLoadResult.Defaults(SettingsLoadStatus.FirstRunDefaults);
        }

        try
        {
            var json = File.ReadAllText(settingsPath);
            var envelope = JsonSerializer.Deserialize<SettingsEnvelope>(json, options);
            if (envelope is null)
            {
                return SettingsLoadResult.Defaults(SettingsLoadStatus.MalformedSettings, "Settings file was empty.");
            }

            if (envelope.SchemaVersion > CurrentSchemaVersion)
            {
                return SettingsLoadResult.Defaults(SettingsLoadStatus.UnsupportedFutureSchema, $"Settings schema {envelope.SchemaVersion} is not supported.");
            }

            return SettingsLoadResult.Success(Sanitize(envelope.Settings));
        }
        catch (JsonException ex)
        {
            return SettingsLoadResult.Defaults(SettingsLoadStatus.MalformedSettings, ex.Message);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return SettingsLoadResult.Defaults(SettingsLoadStatus.FileAccessError, ex.Message);
        }
    }

    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var directory = Path.GetDirectoryName(settingsPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = Path.Combine(directory ?? Directory.GetCurrentDirectory(), $".settings.sfmpack-{Guid.NewGuid():N}.tmp");
        try
        {
            var sanitized = Sanitize(settings);
            File.WriteAllText(tempPath, JsonSerializer.Serialize(new SettingsEnvelope { Settings = sanitized }, options));
            if (File.Exists(settingsPath))
            {
                File.Replace(tempPath, settingsPath, destinationBackupFileName: null);
            }
            else
            {
                File.Move(tempPath, settingsPath);
            }
        }
        finally
        {
            TryDelete(tempPath);
        }
    }

    public void DisableRememberRecentProjects(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.RememberRecentProjects = false;
        Save(settings);
    }

    private static AppSettings Sanitize(AppSettings settings)
    {
        settings.DefaultSfmContentFolder = NormalizeDefaultSfmContentFolder(settings.DefaultSfmContentFolder);
        settings.LastProjectDirectory = Directory.Exists(settings.LastProjectDirectory)
            ? Path.GetFullPath(settings.LastProjectDirectory)
            : string.Empty;
        settings.RecentProjects = settings.RecentProjects
            .Where(entry => !string.IsNullOrWhiteSpace(entry.ProjectPath))
            .OrderByDescending(entry => entry.LastAccessed)
            .ToList();
        settings.RecentProjectPaths = settings.RecentProjectPaths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return settings;
    }

    private static string NormalizeDefaultSfmContentFolder(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        try
        {
            var fullPath = NormalizePath(path);
            foreach (var candidate in new[]
            {
                fullPath,
                Path.Combine(fullPath, "game", "usermod"),
                Path.Combine(fullPath, "usermod")
            })
            {
                var normalizedCandidate = NormalizePath(candidate);
                if (IsUsableUsermodFolder(normalizedCandidate))
                {
                    return normalizedCandidate;
                }
            }

            return fullPath;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return path.Trim();
        }
    }

    private static bool IsUsableUsermodFolder(string path) =>
        Directory.Exists(path)
        && string.Equals(Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)), "usermod", StringComparison.OrdinalIgnoreCase);

    private static string NormalizePath(string path) =>
        Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    private static JsonSerializerOptions CreateOptions()
    {
        var serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        serializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return serializerOptions;
    }

    private static string GetDefaultSettingsPath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SfmPackageBuilder",
            "settings.json");

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
