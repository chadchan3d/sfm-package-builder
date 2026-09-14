namespace SfmPackageBuilder.Core.Build;

internal sealed class BuildLogBuilder
{
    private readonly DateTimeOffset timestamp;
    private readonly string assetName;
    private readonly string version;
    private readonly List<BuildLogEntry> entries = new();

    public BuildLogBuilder(DateTimeOffset timestamp, string assetName, string version)
    {
        this.timestamp = timestamp;
        this.assetName = assetName;
        this.version = version;
    }

    public void Add(string stage, string message, string? technicalDetail = null)
    {
        entries.Add(new BuildLogEntry(stage, message, technicalDetail));
    }

    public BuildLog Build() => new(timestamp, assetName, version, entries);
}
