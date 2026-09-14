namespace SfmPackageBuilder.Core.Planning;

public sealed class PackagePlanEntry
{
    private readonly byte[]? contentBytes;

    public PackagePlanEntry(
        string id,
        PackagePlanEntryType entryType,
        PackagePlanEntryStatus status,
        string? destinationRelativePath,
        string? sourcePath,
        Guid? originProjectEntryId,
        Guid? modelEntryId,
        bool isGenerated,
        ReadmePlanEntryKind readmeKind = ReadmePlanEntryKind.None,
        string? textContent = null,
        byte[]? contentBytes = null,
        string? issueDetail = null,
        PlanRiskMetadata? riskMetadata = null)
    {
        Id = id;
        EntryType = entryType;
        Status = status;
        DestinationRelativePath = destinationRelativePath;
        SourcePath = sourcePath;
        OriginProjectEntryId = originProjectEntryId;
        ModelEntryId = modelEntryId;
        IsGenerated = isGenerated;
        ReadmeKind = readmeKind;
        TextContent = textContent;
        this.contentBytes = contentBytes?.ToArray();
        IssueDetail = issueDetail;
        RiskMetadata = riskMetadata ?? new PlanRiskMetadata();
    }

    public string Id { get; }

    public PackagePlanEntryType EntryType { get; }

    public PackagePlanEntryStatus Status { get; }

    public bool IsResolved => Status == PackagePlanEntryStatus.Resolved;

    public string? DestinationRelativePath { get; }

    public string? SourcePath { get; }

    public Guid? OriginProjectEntryId { get; }

    public Guid? ModelEntryId { get; }

    public bool IsGenerated { get; }

    public ReadmePlanEntryKind ReadmeKind { get; }

    public string? TextContent { get; }

    public byte[]? ContentBytes => contentBytes?.ToArray();

    public string? IssueDetail { get; }

    public PlanRiskMetadata RiskMetadata { get; }
}
