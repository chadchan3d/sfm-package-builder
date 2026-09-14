using System.Text.Json.Serialization;

namespace SfmPackageBuilder.Core.Domain;

public sealed class AppSettings
{
    public string DefaultOutputDirectory { get; set; } = string.Empty;

    public string DefaultSfmContentFolder { get; set; } = string.Empty;

    public string DefaultProjectDirectory { get; set; } = string.Empty;

    public string DefaultArchivePattern { get; set; } = "{AssetName}_v{Version}.zip";

    public string LastProjectDirectory { get; set; } = string.Empty;

    [JsonIgnore]
    public string DefaultReadmeTemplate { get; set; } = string.Empty;

    public bool RememberRecentProjects { get; set; } = true;

    public List<string> RecentProjectPaths { get; set; } = new();

    public List<RecentProjectEntry> RecentProjects { get; set; } = new();

    public CreatorDefaults CreatorDefaults { get; set; } = new();
}
