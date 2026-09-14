using SfmPackageBuilder.Core.Domain;

namespace SfmPackageBuilder.Core.Readme;

public sealed class ReadmeGenerator
{
    public const string NewLine = "\r\n";

    public const string UpdateInstallSentence =
        "If Windows asks to merge folders or replace files when updating, allow it.";

    public string Generate(PackageProject project, ReadmeContext context)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(context);

        var sections = new List<string>();
        var title = ResolveTitle(project);

        var titleBlock = CreateHeading(title, '=');
        if (!string.IsNullOrWhiteSpace(project.CurrentVersion))
        {
            titleBlock += NewLine + "Version " + project.CurrentVersion.Trim();
        }

        if (!string.IsNullOrWhiteSpace(project.Readme.Description))
        {
            titleBlock += NewLine + NewLine + project.Readme.Description.Trim();
        }

        sections.Add(titleBlock);
        sections.Add(CreateSection("INSTALLATION", CreateInstallationText(project, context)));

        if (context.ModelPaths.Count > 0)
        {
            var heading = context.ModelPaths.Count == 1 ? "MODEL INCLUDED" : "MODELS INCLUDED";
            var modelText = string.Join(NewLine, context.ModelPaths.Select(path => "- " + path));
            sections.Add(CreateSection(heading, modelText));
        }

        if (!string.IsNullOrWhiteSpace(project.Readme.Author))
        {
            sections.Add(CreateSection("AUTHOR", project.Readme.Author.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(project.Readme.Website))
        {
            sections.Add(CreateSection("WEBSITE", project.Readme.Website.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(project.Credits))
        {
            sections.Add(CreateSection("CREDITS", project.Credits.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(project.Readme.License))
        {
            sections.Add(CreateSection("USAGE TERMS", project.Readme.License.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(project.Readme.AdditionalResources))
        {
            sections.Add(CreateSection("LINKS & RESOURCES", project.Readme.AdditionalResources.Trim()));
        }

        var changelog = CreateChangelogText(project);
        if (!string.IsNullOrWhiteSpace(changelog))
        {
            sections.Add(CreateSection("CHANGELOG", changelog));
        }

        return string.Join(NewLine + NewLine, sections) + NewLine;
    }

    private static string CreateHeading(string text, char underline)
    {
        return text + NewLine + new string(underline, text.Length);
    }

    private static string CreateSection(string title, string body)
    {
        return title + NewLine + new string('-', title.Length) + NewLine + body;
    }

    private static string CreateInstallationText(PackageProject project, ReadmeContext context)
    {
        if (context.InstallRoots.Count == 0)
        {
            return "This package contains no automatically recognized Source Filmmaker installation folders.";
        }

        var installRootList = string.Join(NewLine, context.InstallRoots.Select(CreateInstallRootLine));
        return "Copy these folders into your Source Filmmaker usermod folder:" + NewLine +
            installRootList + NewLine + NewLine +
            "Default location:" + NewLine +
            @"...\SourceFilmmaker\game\usermod" + NewLine + NewLine +
            UpdateInstallSentence;
    }

    private static string ResolveTitle(PackageProject project)
    {
        if (!string.IsNullOrWhiteSpace(project.AssetName))
        {
            return project.AssetName.Trim().ToUpperInvariant();
        }

        var primary = project.Models.FirstOrDefault(model => model.Role == ModelRole.Primary);
        var fallback = primary is null
            ? string.Empty
            : FirstNonBlank(
                primary.ReleaseStem,
                primary.SourceStem,
                Path.GetFileNameWithoutExtension(primary.SourceMdlPath));
        if (string.IsNullOrWhiteSpace(fallback))
        {
            return "UNTITLED PACKAGE";
        }

        return fallback.Replace('_', ' ').Trim().ToUpperInvariant();
    }

    private static string FirstNonBlank(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;

    private static string CreateChangelogText(PackageProject project)
    {
        var records = project.GetReadmeReleaseHistory();
        if (records.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(NewLine + NewLine, records.Select(CreateReleaseText).Where(text => !string.IsNullOrWhiteSpace(text)));
    }

    private static string CreateReleaseText(ReleaseRecord record)
    {
        var lines = new List<string>();
        var changes = record.Changes
            .Where(change => !string.IsNullOrWhiteSpace(change))
            .Select(change => change.Trim())
            .ToArray();
        if (changes.Length == 0)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(record.Version))
        {
            lines.Add(record.Version.Trim());
        }

        lines.AddRange(changes);

        return string.Join(NewLine, lines);
    }

    private static string CreateInstallRootLine(string root)
    {
        return "- " + root.TrimEnd('\\', '/');
    }
}
