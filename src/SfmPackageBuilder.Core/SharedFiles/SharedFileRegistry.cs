namespace SfmPackageBuilder.Core.SharedFiles;

public sealed class SharedFileRegistry
{
    private readonly IReadOnlyList<SharedFileRule> rules;

    public SharedFileRegistry(IEnumerable<SharedFileRule>? rules = null)
    {
        this.rules = (rules ?? KnownSharedFiles.CreateDefaultRules()).ToArray();
    }

    public IReadOnlyList<SharedFileNotice> Analyze(IEnumerable<SharedFileCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        var notices = new List<SharedFileNotice>();

        foreach (var candidate in candidates)
        {
            var normalizedDestination = NormalizePackageRelativePath(candidate.DestinationRelativePath);
            var filename = GetFileName(normalizedDestination);

            foreach (var rule in rules)
            {
                if (!string.Equals(filename, rule.FilenamePattern, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                notices.Add(new SharedFileNotice(
                    SharedFileNoticeKind.Information,
                    rule.Key,
                    candidate.CandidateId,
                    normalizedDestination,
                    rule.InformationalTitle,
                    rule.InformationalText,
                    rule.ProvidesControlGroupsReadmeContext,
                    candidate.SourcePath));

                if (IsKnownLiveDestination(rule, normalizedDestination))
                {
                    notices.Add(new SharedFileNotice(
                        SharedFileNoticeKind.LiveDestinationWarning,
                        rule.Key,
                        candidate.CandidateId,
                        normalizedDestination,
                        rule.InformationalTitle,
                        rule.InformationalText,
                        rule.ProvidesControlGroupsReadmeContext,
                        candidate.SourcePath));
                }
            }
        }

        return notices;
    }

    public static bool HasControlGroupsReadmeContext(IEnumerable<SharedFileNotice> notices)
    {
        ArgumentNullException.ThrowIfNull(notices);
        return notices.Any(notice => notice.ProvidesControlGroupsReadmeContext);
    }

    private static bool IsKnownLiveDestination(SharedFileRule rule, string normalizedDestination)
    {
        return rule.KnownLiveDestinations
            .Select(NormalizePackageRelativePath)
            .Any(destination => string.Equals(destination, normalizedDestination, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetFileName(string normalizedDestination)
    {
        var segments = normalizedDestination.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length == 0 ? string.Empty : segments[^1];
    }

    private static string NormalizePackageRelativePath(string path)
    {
        return string.Join(
            '\\',
            path.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(segment => segment != "."));
    }
}
