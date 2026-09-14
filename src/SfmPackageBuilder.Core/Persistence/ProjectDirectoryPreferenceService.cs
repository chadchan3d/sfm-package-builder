using SfmPackageBuilder.Core.Domain;

namespace SfmPackageBuilder.Core.Persistence;

public sealed class ProjectDirectoryPreferenceService
{
    public string ResolveStartDirectory(AppSettings settings)
    {
        return ResolveOpenStartDirectory(settings);
    }

    public string ResolveOpenStartDirectory(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return FirstExisting(settings.LastProjectDirectory, settings.DefaultProjectDirectory, GetSafeFallbackDirectory());
    }

    public string ResolveNewProjectSaveStartDirectory(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return FirstExisting(settings.DefaultProjectDirectory, settings.LastProjectDirectory, GetSafeFallbackDirectory());
    }

    public string ResolveSaveAsStartDirectory(AppSettings settings, string? currentProjectPath)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!string.IsNullOrWhiteSpace(currentProjectPath))
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(currentProjectPath));
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
            {
                return directory;
            }
        }

        return ResolveNewProjectSaveStartDirectory(settings);
    }

    public bool RecordSuccessfulProjectPath(AppSettings settings, string projectPath)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            return false;
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(projectPath));
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return false;
        }

        settings.LastProjectDirectory = directory;
        return true;
    }

    private static string FirstExisting(params string[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            try
            {
                if (Directory.Exists(candidate))
                {
                    return Path.GetFullPath(candidate);
                }
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
            }
        }

        return string.Empty;
    }

    private static string GetSafeFallbackDirectory()
    {
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (!string.IsNullOrWhiteSpace(documents) && Directory.Exists(documents))
        {
            return documents;
        }

        return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }
}
