namespace SfmPackageBuilder.Core.Validation;

public sealed class ValidationMessage
{
    public ValidationMessage(
        ValidationSeverity severity,
        string code,
        string message,
        IEnumerable<string>? sourcePaths = null,
        IEnumerable<string>? relatedEntryIds = null,
        string? destinationRelativePath = null,
        string? technicalDetail = null,
        string? detail = null,
        IEnumerable<string>? affectedValues = null,
        IEnumerable<string>? candidateDestinations = null)
    {
        Severity = severity;
        Code = code;
        Message = message;
        SourcePaths = (sourcePaths ?? Array.Empty<string>()).ToArray();
        RelatedEntryIds = (relatedEntryIds ?? Array.Empty<string>()).ToArray();
        DestinationRelativePath = destinationRelativePath;
        TechnicalDetail = technicalDetail;
        Detail = detail;
        AffectedValues = (affectedValues ?? Array.Empty<string>()).ToArray();
        CandidateDestinations = (candidateDestinations ?? Array.Empty<string>()).ToArray();
    }

    public ValidationSeverity Severity { get; }

    public string Code { get; }

    public string Message { get; }

    public IReadOnlyList<string> SourcePaths { get; }

    public IReadOnlyList<string> RelatedEntryIds { get; }

    public string? DestinationRelativePath { get; }

    public string? TechnicalDetail { get; }

    public string? Detail { get; }

    public IReadOnlyList<string> AffectedValues { get; }

    public IReadOnlyList<string> CandidateDestinations { get; }
}
