namespace SfmPackageBuilder.Core.SharedFiles;

public sealed class SharedFileCandidate
{
    public SharedFileCandidate(
        string candidateId,
        string destinationRelativePath,
        string? sourcePath = null,
        SharedFileCandidateOrigin origin = SharedFileCandidateOrigin.Unknown)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(candidateId);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationRelativePath);

        CandidateId = candidateId;
        DestinationRelativePath = destinationRelativePath;
        SourcePath = sourcePath;
        Origin = origin;
    }

    public string CandidateId { get; }

    public string DestinationRelativePath { get; }

    public string? SourcePath { get; }

    public SharedFileCandidateOrigin Origin { get; }
}
