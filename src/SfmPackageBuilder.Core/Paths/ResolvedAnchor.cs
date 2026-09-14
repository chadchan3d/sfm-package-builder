namespace SfmPackageBuilder.Core.Paths;

public sealed class ResolvedAnchor
{
    private ResolvedAnchor(
        AnchorResolutionStatus status,
        string sourcePath,
        SourceAnchorKind anchorKind,
        string anchorSegment,
        int? selectedSegmentIndex,
        IReadOnlyList<int> candidateSegmentIndexes,
        string? destinationRelativePath)
    {
        Status = status;
        SourcePath = sourcePath;
        AnchorKind = anchorKind;
        AnchorSegment = anchorSegment;
        SelectedSegmentIndex = selectedSegmentIndex;
        CandidateSegmentIndexes = candidateSegmentIndexes;
        DestinationRelativePath = destinationRelativePath;
    }

    public AnchorResolutionStatus Status { get; }

    public bool IsResolved => Status == AnchorResolutionStatus.Resolved;

    public string SourcePath { get; }

    public SourceAnchorKind AnchorKind { get; }

    public string AnchorSegment { get; }

    public int? SelectedSegmentIndex { get; }

    public IReadOnlyList<int> CandidateSegmentIndexes { get; }

    public string? DestinationRelativePath { get; }

    public static ResolvedAnchor Resolved(
        string sourcePath,
        SourceAnchorKind anchorKind,
        string anchorSegment,
        int selectedSegmentIndex,
        IReadOnlyList<int> candidateSegmentIndexes,
        string destinationRelativePath)
    {
        return new ResolvedAnchor(
            AnchorResolutionStatus.Resolved,
            sourcePath,
            anchorKind,
            anchorSegment,
            selectedSegmentIndex,
            candidateSegmentIndexes,
            destinationRelativePath);
    }

    public static ResolvedAnchor Missing(string sourcePath, SourceAnchorKind anchorKind, string anchorSegment)
    {
        return new ResolvedAnchor(
            AnchorResolutionStatus.MissingAnchor,
            sourcePath,
            anchorKind,
            anchorSegment,
            null,
            Array.Empty<int>(),
            null);
    }
}
