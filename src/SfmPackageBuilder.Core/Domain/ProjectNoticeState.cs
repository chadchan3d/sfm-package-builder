namespace SfmPackageBuilder.Core.Domain;

public sealed class ProjectNoticeState
{
    public string Key { get; set; } = string.Empty;

    public bool IsAcknowledged { get; set; }
}
