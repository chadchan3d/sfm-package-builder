namespace SfmPackageBuilder.Core.Domain;

public sealed class RecentProjectEntry
{
    public string ProjectPath { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public DateTimeOffset LastAccessed { get; set; }
}
