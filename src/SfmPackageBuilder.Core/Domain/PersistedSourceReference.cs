namespace SfmPackageBuilder.Core.Domain;

public sealed class PersistedSourceReference
{
    public string AbsolutePath { get; set; } = string.Empty;

    public string? RelativePath { get; set; }

    public string? RecoveryRootPath { get; set; }

    public string? RecoveryRelativePath { get; set; }
}
