namespace SfmPackageBuilder.Core.Persistence;

public sealed class RecoveryIssue
{
    public RecoveryIssue(
        string id,
        RecoveryReferenceKind referenceKind,
        RecoveryIssueStatus status,
        string sourcePath,
        Guid? projectEntryId = null,
        string? companionRuntimeSuffix = null,
        string? formerRoot = null,
        string? relativeRemainder = null,
        bool expectsDirectory = false,
        string? message = null,
        string? technicalDetail = null)
    {
        Id = id;
        ReferenceKind = referenceKind;
        Status = status;
        SourcePath = sourcePath;
        ProjectEntryId = projectEntryId;
        CompanionRuntimeSuffix = companionRuntimeSuffix;
        FormerRoot = formerRoot;
        RelativeRemainder = relativeRemainder;
        ExpectsDirectory = expectsDirectory;
        Message = message ?? "A persisted source reference could not be resolved.";
        TechnicalDetail = technicalDetail;
    }

    public string Id { get; }

    public RecoveryReferenceKind ReferenceKind { get; }

    public RecoveryIssueStatus Status { get; }

    public string SourcePath { get; }

    public Guid? ProjectEntryId { get; }

    public string? CompanionRuntimeSuffix { get; }

    public string? FormerRoot { get; }

    public string? RelativeRemainder { get; }

    public bool ExpectsDirectory { get; }

    public string Message { get; }

    public string? TechnicalDetail { get; }
}
