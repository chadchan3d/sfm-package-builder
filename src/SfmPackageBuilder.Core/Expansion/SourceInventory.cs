using SfmPackageBuilder.Core.Domain;

namespace SfmPackageBuilder.Core.Expansion;

public sealed class SourceInventory
{
    public SourceInventory(
        SourceEntry origin,
        SourceInventoryStatus status,
        IReadOnlyList<SourceInventoryItem> items,
        string? failureDetail = null)
    {
        Origin = origin;
        Status = status;
        Items = items;
        FailureDetail = failureDetail;
    }

    public SourceEntry Origin { get; }

    public SourceInventoryStatus Status { get; }

    public IReadOnlyList<SourceInventoryItem> Items { get; }

    public string? FailureDetail { get; }
}
