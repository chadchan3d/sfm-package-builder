namespace SfmPackageBuilder.Core.Domain;

public sealed class PackageProject
{
    public int SchemaVersion { get; set; } = ProjectSchemaVersion.Current;

    public string AssetName { get; set; } = string.Empty;

    public string CurrentVersion { get; set; } = string.Empty;

    public List<string> ChangesThisVersion { get; set; } = new();

    public string Credits { get; set; } = string.Empty;

    public string ArchiveName { get; set; } = string.Empty;

    public List<ModelEntry> Models { get; set; } = new();

    public List<SourceEntry> MaterialSources { get; set; } = new();

    public List<SourceEntry> Extras { get; set; } = new();

    public ReadmeConfig Readme { get; set; } = new();

    public List<ReleaseRecord> ReleaseHistory { get; set; } = new();

    public List<BuildRecord> BuildHistory { get; set; } = new();

    public List<ProjectNoticeState> InformationalState { get; set; } = new();

    public void BeginNewRelease(string version, IEnumerable<string>? changes = null)
    {
        PreserveCurrentRelease();
        CurrentVersion = version?.Trim() ?? string.Empty;
        ChangesThisVersion = NormalizeChanges(changes).ToList();
    }

    public void EditReleaseEntry(int index, string version, IEnumerable<string>? changes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        if (index >= ReleaseHistory.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        ReleaseHistory[index].Version = version?.Trim() ?? string.Empty;
        ReleaseHistory[index].Changes = NormalizeChanges(changes).ToList();
    }

    public void RemoveReleaseEntry(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        if (index >= ReleaseHistory.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        ReleaseHistory.RemoveAt(index);
    }

    public IReadOnlyList<ReleaseRecord> GetReadmeReleaseHistory()
    {
        var records = new List<ReleaseRecord>();
        var currentHasChangelogEntry = HasReadmeChangelogEntry(ChangesThisVersion);
        if (currentHasChangelogEntry)
        {
            records.Add(new ReleaseRecord
            {
                Version = CurrentVersion.Trim(),
                Changes = NormalizeChanges(ChangesThisVersion).ToList()
            });
        }

        records.AddRange(ReleaseHistory
            .Where(record => HasReadmeChangelogEntry(record.Changes))
            .Where(record => !currentHasChangelogEntry
                || !string.Equals(record.Version?.Trim(), CurrentVersion.Trim(), StringComparison.OrdinalIgnoreCase))
            .Select(record => new ReleaseRecord
            {
                Version = record.Version.Trim(),
                Changes = NormalizeChanges(record.Changes).ToList()
            }));

        return records;
    }

    internal void PreserveCurrentRelease()
    {
        if (!HasMeaningfulRelease(CurrentVersion, ChangesThisVersion))
        {
            return;
        }

        var record = new ReleaseRecord
        {
            Version = CurrentVersion.Trim(),
            Changes = NormalizeChanges(ChangesThisVersion).ToList()
        };
        var existingIndex = ReleaseHistory.FindIndex(entry =>
            string.Equals(entry.Version?.Trim(), record.Version, StringComparison.OrdinalIgnoreCase));

        if (existingIndex >= 0)
        {
            ReleaseHistory[existingIndex] = record;
        }
        else
        {
            ReleaseHistory.Insert(0, record);
        }
    }

    internal static bool HasMeaningfulRelease(string? version, IEnumerable<string>? changes) =>
        !string.IsNullOrWhiteSpace(version)
        || NormalizeChanges(changes).Any();

    public static bool HasReadmeChangelogEntry(IEnumerable<string>? changes) =>
        NormalizeChanges(changes).Any(change => !string.IsNullOrWhiteSpace(change));

    private static IEnumerable<string> NormalizeChanges(IEnumerable<string>? changes) =>
        (changes ?? Array.Empty<string>()).Select(change => change?.Trim() ?? string.Empty);
}
