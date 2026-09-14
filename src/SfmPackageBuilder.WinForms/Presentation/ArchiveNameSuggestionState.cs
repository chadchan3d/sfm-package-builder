using SfmPackageBuilder.Core.Domain;

namespace SfmPackageBuilder.WinForms.Presentation;

public sealed class ArchiveNameSuggestionState
{
    private readonly ArchiveNameSuggester suggester;

    public ArchiveNameSuggestionState(ArchiveNameSuggester suggester)
    {
        this.suggester = suggester;
    }

    public bool HasManualName { get; private set; }

    public string CurrentName { get; private set; } = string.Empty;

    public string RefreshSuggested(string assetName, string version, AppSettings settings)
    {
        if (!HasManualName)
        {
            CurrentName = suggester.Suggest(assetName, version, settings);
        }

        return CurrentName;
    }

    public void LoadExisting(string archiveName)
    {
        CurrentName = archiveName ?? string.Empty;
        HasManualName = !string.IsNullOrWhiteSpace(CurrentName);
    }

    public void MarkManualEdit(string archiveName)
    {
        CurrentName = archiveName ?? string.Empty;
        HasManualName = true;
    }

    public string ResetToSuggested(string assetName, string version, AppSettings settings)
    {
        HasManualName = false;
        return RefreshSuggested(assetName, version, settings);
    }
}
