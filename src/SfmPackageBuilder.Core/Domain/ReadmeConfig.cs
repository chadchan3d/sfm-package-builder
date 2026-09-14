using System.Text.Json.Serialization;

namespace SfmPackageBuilder.Core.Domain;

public sealed class ReadmeConfig
{
    public ReadmeMode Mode { get; set; } = ReadmeMode.Generated;

    public ReadmeCustomSource CustomSource { get; set; } = ReadmeCustomSource.ProjectText;

    public string Description { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    public string Website { get; set; } = string.Empty;

    public string License { get; set; } = string.Empty;

    public string AdditionalResources { get; set; } = string.Empty;

    public bool IncludeInstallInstructions { get; set; } = true;

    public bool IncludeModelPath { get; set; } = true;

    public bool IncludeVersionChanges { get; set; } = true;

    // Compatibility shim for old .sfmpack files. Current README generation ignores this obsolete value.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IncludeControlGroupsInfo { get; set; }

    public string? ImportedReadmePath { get; set; }

    public PersistedSourceReference? ImportedReadmeReference { get; set; }

    public string? CustomReadmeText { get; set; }

    public string? CustomReadmeGeneratedFromFingerprint { get; set; }
}
