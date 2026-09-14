namespace SfmPackageBuilder.Core.Domain;

public sealed class DestinationOverride
{
    public DestinationOverrideKind Kind { get; set; } = DestinationOverrideKind.Custom;

    public string RelativePath { get; set; } = string.Empty;

    public string? Reason { get; set; }
}
