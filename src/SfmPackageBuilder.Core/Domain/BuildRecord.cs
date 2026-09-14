namespace SfmPackageBuilder.Core.Domain;

public sealed class BuildRecord
{
    public string Version { get; set; } = string.Empty;

    public string ArchiveName { get; set; } = string.Empty;

    public DateTimeOffset BuildTimestamp { get; set; }
}
