namespace SfmPackageBuilder.Core.Domain;

public sealed class ReleaseRecord
{
    public string Version { get; set; } = string.Empty;

    public List<string> Changes { get; set; } = new();
}
