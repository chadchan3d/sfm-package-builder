namespace SfmPackageBuilder.Core.Expansion;

public sealed class CompanionRule
{
    public CompanionRule(string runtimeSuffix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runtimeSuffix);
        RuntimeSuffix = runtimeSuffix;
    }

    public string RuntimeSuffix { get; }
}
