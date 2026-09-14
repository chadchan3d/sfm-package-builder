namespace SfmPackageBuilder.Core.Persistence;

public sealed class RecoveryRemapCandidate
{
    public RecoveryRemapCandidate(
        RecoveryIssue issue,
        string candidatePath,
        RecoveryRemapCandidateStatus status,
        string? technicalDetail = null)
    {
        Issue = issue;
        CandidatePath = candidatePath;
        Status = status;
        TechnicalDetail = technicalDetail;
    }

    public RecoveryIssue Issue { get; }

    public string CandidatePath { get; }

    public RecoveryRemapCandidateStatus Status { get; }

    public bool IsMatched => Status == RecoveryRemapCandidateStatus.Matched;

    public string? TechnicalDetail { get; }
}
