namespace SfmPackageBuilder.Core.Persistence;

public sealed class RecoveryRemapPreview
{
    public RecoveryRemapPreview(
        string formerRoot,
        string replacementRoot,
        IReadOnlyList<RecoveryRemapCandidate> candidates)
    {
        FormerRoot = formerRoot;
        ReplacementRoot = replacementRoot;
        Candidates = candidates.ToArray();
    }

    public string FormerRoot { get; }

    public string ReplacementRoot { get; }

    public IReadOnlyList<RecoveryRemapCandidate> Candidates { get; }

    public IReadOnlyList<RecoveryRemapCandidate> MatchedCandidates =>
        Candidates.Where(candidate => candidate.IsMatched).ToArray();

    public IReadOnlyList<RecoveryRemapCandidate> UnresolvedCandidates =>
        Candidates.Where(candidate => !candidate.IsMatched).ToArray();

    public bool HasMatches => Candidates.Any(candidate => candidate.IsMatched);

    public bool IsFullMatch => Candidates.Count > 0 && Candidates.All(candidate => candidate.IsMatched);
}
