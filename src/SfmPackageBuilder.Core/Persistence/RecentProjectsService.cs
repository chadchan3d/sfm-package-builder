using SfmPackageBuilder.Core.Domain;

namespace SfmPackageBuilder.Core.Persistence;

public sealed class RecentProjectsService
{
    public const int DefaultMaxEntries = 10;
    private readonly SettingsService settingsService;
    private readonly int maxEntries;
    private readonly Func<DateTimeOffset> clock;

    public RecentProjectsService(SettingsService settingsService, int maxEntries = DefaultMaxEntries, Func<DateTimeOffset>? clock = null)
    {
        this.settingsService = settingsService;
        this.maxEntries = maxEntries;
        this.clock = clock ?? (() => DateTimeOffset.Now);
    }

    public IReadOnlyList<RecentProjectEntry> AddOrPromote(AppSettings settings, string projectPath, string? displayName = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);

        var fullPath = Path.GetFullPath(projectPath);
        settings.RecentProjects.RemoveAll(entry => PathsEqual(entry.ProjectPath, fullPath));
        settings.RecentProjects.Insert(0, new RecentProjectEntry
        {
            ProjectPath = fullPath,
            DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? Path.GetFileNameWithoutExtension(fullPath)
                : displayName,
            LastAccessed = clock()
        });
        if (settings.RecentProjects.Count > maxEntries)
        {
            settings.RecentProjects.RemoveRange(maxEntries, settings.RecentProjects.Count - maxEntries);
        }

        settings.RecentProjectPaths = settings.RecentProjects.Select(entry => entry.ProjectPath).ToList();
        settingsService.Save(settings);
        return settings.RecentProjects;
    }

    public IReadOnlyList<RecentProjectEntry> GetRecentProjects(AppSettings settings) =>
        settings.RecentProjects.OrderByDescending(entry => entry.LastAccessed).Take(maxEntries).ToArray();

    public IReadOnlyList<RecentProjectEntry> Remove(AppSettings settings, string projectPath)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.RecentProjects.RemoveAll(entry => PathsEqual(entry.ProjectPath, projectPath));
        settings.RecentProjectPaths = settings.RecentProjects.Select(entry => entry.ProjectPath).ToList();
        settingsService.Save(settings);
        return settings.RecentProjects;
    }

    public IReadOnlyList<RecentProjectEntry> ReplacePath(AppSettings settings, string oldProjectPath, string newProjectPath, string? displayName = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(oldProjectPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(newProjectPath);

        var fullPath = Path.GetFullPath(newProjectPath);
        settings.RecentProjects.RemoveAll(entry => PathsEqual(entry.ProjectPath, oldProjectPath) || PathsEqual(entry.ProjectPath, fullPath));
        settings.RecentProjects.Insert(0, new RecentProjectEntry
        {
            ProjectPath = fullPath,
            DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? Path.GetFileNameWithoutExtension(fullPath)
                : displayName,
            LastAccessed = clock()
        });
        if (settings.RecentProjects.Count > maxEntries)
        {
            settings.RecentProjects.RemoveRange(maxEntries, settings.RecentProjects.Count - maxEntries);
        }

        settings.RecentProjectPaths = settings.RecentProjects.Select(entry => entry.ProjectPath).ToList();
        settingsService.Save(settings);
        return settings.RecentProjects;
    }

    public IReadOnlyList<RecentProjectEntry> DisableAndClear(AppSettings settings)
    {
        settingsService.DisableRememberRecentProjects(settings);
        return settings.RecentProjects;
    }

    public bool IsMissing(RecentProjectEntry entry) => !File.Exists(entry.ProjectPath);

    private static bool PathsEqual(string left, string right) =>
        string.Equals(
            Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);
}
