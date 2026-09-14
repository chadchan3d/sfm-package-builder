namespace SfmPackageBuilder.Core.Staging;

internal sealed class StagingPlanPreflightResult
{
    private StagingPlanPreflightResult(IReadOnlyList<StagingPlanEntry> entries, StagingFailure? failure)
    {
        Entries = entries.ToArray();
        Failure = failure;
    }

    public bool Succeeded => Failure is null;

    public IReadOnlyList<StagingPlanEntry> Entries { get; }

    public StagingFailure? Failure { get; }

    public static StagingPlanPreflightResult Success(IReadOnlyList<StagingPlanEntry> entries) => new(entries, null);

    public static StagingPlanPreflightResult Failed(StagingFailure failure) =>
        new(Array.Empty<StagingPlanEntry>(), failure);
}
