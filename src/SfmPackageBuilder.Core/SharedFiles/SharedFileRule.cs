namespace SfmPackageBuilder.Core.SharedFiles;

public sealed class SharedFileRule
{
    public SharedFileRule(
        string key,
        string filenamePattern,
        IReadOnlyList<string> knownLiveDestinations,
        string informationalTitle,
        string informationalText,
        bool providesControlGroupsReadmeContext = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(filenamePattern);
        ArgumentException.ThrowIfNullOrWhiteSpace(informationalTitle);
        ArgumentException.ThrowIfNullOrWhiteSpace(informationalText);

        Key = key;
        FilenamePattern = filenamePattern;
        KnownLiveDestinations = knownLiveDestinations;
        InformationalTitle = informationalTitle;
        InformationalText = informationalText;
        ProvidesControlGroupsReadmeContext = providesControlGroupsReadmeContext;
    }

    public string Key { get; }

    public string FilenamePattern { get; }

    public IReadOnlyList<string> KnownLiveDestinations { get; }

    public string InformationalTitle { get; }

    public string InformationalText { get; }

    public bool ProvidesControlGroupsReadmeContext { get; }
}
