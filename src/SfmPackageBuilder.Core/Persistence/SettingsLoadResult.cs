using SfmPackageBuilder.Core.Domain;

namespace SfmPackageBuilder.Core.Persistence;

public sealed class SettingsLoadResult
{
    private SettingsLoadResult(SettingsLoadStatus status, AppSettings settings, string? message)
    {
        Status = status;
        Settings = settings;
        Message = message;
    }

    public SettingsLoadStatus Status { get; }

    public AppSettings Settings { get; }

    public string? Message { get; }

    public bool IsUsable => Settings is not null;

    public static SettingsLoadResult Success(AppSettings settings) => new(SettingsLoadStatus.Success, settings, null);

    public static SettingsLoadResult Defaults(SettingsLoadStatus status, string? message = null) => new(status, new AppSettings(), message);
}
