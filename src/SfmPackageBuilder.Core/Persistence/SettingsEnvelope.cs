using SfmPackageBuilder.Core.Domain;

namespace SfmPackageBuilder.Core.Persistence;

internal sealed class SettingsEnvelope
{
    public int SchemaVersion { get; set; } = SettingsService.CurrentSchemaVersion;

    public AppSettings Settings { get; set; } = new();
}
