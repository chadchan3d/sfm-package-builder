namespace SfmPackageBuilder.Core.Persistence;

public sealed class RecoveryIssueGroup
{
    public RecoveryIssueGroup(string formerRoot, IReadOnlyList<RecoveryIssue> issues)
    {
        FormerRoot = formerRoot;
        Issues = issues.ToArray();
    }

    public string FormerRoot { get; }

    public IReadOnlyList<RecoveryIssue> Issues { get; }
}
