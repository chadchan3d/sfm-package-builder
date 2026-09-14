namespace SfmPackageBuilder.Core.Persistence;

public sealed class RecoveryInspectionResult
{
    public RecoveryInspectionResult(IReadOnlyList<RecoveryIssue> issues)
    {
        Issues = issues.ToArray();
        Groups = Issues
            .Where(issue => !string.IsNullOrWhiteSpace(issue.FormerRoot))
            .GroupBy(issue => issue.FormerRoot!, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new RecoveryIssueGroup(
                group.Key,
                group.OrderBy(issue => issue.RelativeRemainder ?? issue.SourcePath, StringComparer.OrdinalIgnoreCase).ToArray()))
            .ToArray();
    }

    public IReadOnlyList<RecoveryIssue> Issues { get; }

    public IReadOnlyList<RecoveryIssueGroup> Groups { get; }

    public bool HasIssues => Issues.Count > 0;
}
