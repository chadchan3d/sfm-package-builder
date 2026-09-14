namespace SfmPackageBuilder.Core.Build;

public sealed class BuildProgress
{
    public BuildProgress(
        BuildProgressPhase phase,
        int? completed = null,
        int? total = null,
        string? currentItem = null)
    {
        Phase = phase;
        Completed = completed;
        Total = total;
        CurrentItem = currentItem;
    }

    public BuildProgressPhase Phase { get; }

    public int? Completed { get; }

    public int? Total { get; }

    public string? CurrentItem { get; }
}
