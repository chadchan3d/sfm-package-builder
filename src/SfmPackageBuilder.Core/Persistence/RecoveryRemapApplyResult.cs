namespace SfmPackageBuilder.Core.Persistence;

public sealed class RecoveryRemapApplyResult
{
    public RecoveryRemapApplyResult(int appliedCount)
    {
        AppliedCount = appliedCount;
    }

    public int AppliedCount { get; }

    public bool AppliedAny => AppliedCount > 0;
}
