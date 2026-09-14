namespace SfmPackageBuilder.Core.Persistence;

public enum SettingsLoadStatus
{
    Success,
    FirstRunDefaults,
    MalformedSettings,
    UnsupportedFutureSchema,
    FileAccessError
}
