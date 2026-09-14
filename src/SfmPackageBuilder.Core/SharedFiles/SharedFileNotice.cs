namespace SfmPackageBuilder.Core.SharedFiles;

public sealed class SharedFileNotice
{
    public SharedFileNotice(
        SharedFileNoticeKind kind,
        string ruleKey,
        string candidateId,
        string destinationRelativePath,
        string title,
        string body,
        bool providesControlGroupsReadmeContext,
        string? sourcePath = null)
    {
        Kind = kind;
        RuleKey = ruleKey;
        CandidateId = candidateId;
        DestinationRelativePath = destinationRelativePath;
        Title = title;
        Body = body;
        ProvidesControlGroupsReadmeContext = providesControlGroupsReadmeContext;
        SourcePath = sourcePath;
    }

    public SharedFileNoticeKind Kind { get; }

    public string RuleKey { get; }

    public string CandidateId { get; }

    public string DestinationRelativePath { get; }

    public string Title { get; }

    public string Body { get; }

    public bool ProvidesControlGroupsReadmeContext { get; }

    public string? SourcePath { get; }

    public bool IsBlocking => false;
}
