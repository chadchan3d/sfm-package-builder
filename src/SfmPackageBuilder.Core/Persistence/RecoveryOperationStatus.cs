namespace SfmPackageBuilder.Core.Persistence;

public enum RecoveryOperationStatus
{
    Success,
    IssueNotFound,
    ReplacementMissing,
    ReplacementInaccessible,
    DestinationDecisionNeeded,
    CannotRemoveRequiredPrimaryModel
}
