namespace SfmPackageBuilder.Core.Build;

public sealed class BuildLog
{
    public BuildLog(DateTimeOffset timestamp, string assetName, string version, IReadOnlyList<BuildLogEntry> entries)
    {
        Timestamp = timestamp;
        AssetName = assetName;
        Version = version;
        Entries = entries.ToArray();
    }

    public DateTimeOffset Timestamp { get; }

    public string AssetName { get; }

    public string Version { get; }

    public IReadOnlyList<BuildLogEntry> Entries { get; }
}
