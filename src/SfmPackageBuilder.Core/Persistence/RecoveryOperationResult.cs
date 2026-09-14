namespace SfmPackageBuilder.Core.Persistence;

public sealed class RecoveryOperationResult
{
    public RecoveryOperationResult(
        RecoveryOperationStatus status,
        RecoveryIssue? issue,
        string? replacementPath = null,
        bool destinationDecisionMayBeNeeded = false,
        string? message = null,
        string? technicalDetail = null)
    {
        Status = status;
        Issue = issue;
        ReplacementPath = replacementPath;
        DestinationDecisionMayBeNeeded = destinationDecisionMayBeNeeded;
        Message = message;
        TechnicalDetail = technicalDetail;
    }

    public RecoveryOperationStatus Status { get; }

    public bool Succeeded => Status == RecoveryOperationStatus.Success;

    public RecoveryIssue? Issue { get; }

    public string? ReplacementPath { get; }

    public bool DestinationDecisionMayBeNeeded { get; }

    public string? Message { get; }

    public string? TechnicalDetail { get; }
}
