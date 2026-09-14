namespace SfmPackageBuilder.Core.Validation;

public sealed class ValidationResult
{
    public ValidationResult(
        IReadOnlyList<ValidationMessage> messages,
        IReadOnlyList<RequiredDecision> requiredDecisions)
    {
        Messages = messages
            .OrderBy(message => SeverityOrder(message.Severity))
            .ThenBy(message => message.Code, StringComparer.Ordinal)
            .ThenBy(message => message.DestinationRelativePath ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(message => string.Join("|", message.SourcePaths), StringComparer.OrdinalIgnoreCase)
            .ToArray();
        RequiredDecisions = requiredDecisions
            .OrderBy(decision => decision.Kind)
            .ThenBy(decision => decision.Code, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<ValidationMessage> Messages { get; }

    public IReadOnlyList<RequiredDecision> RequiredDecisions { get; }

    public bool HasErrors => Messages.Any(message => message.Severity == ValidationSeverity.Error);

    public bool HasRequiredDecisions => RequiredDecisions.Count > 0;

    public bool CanBuild => !HasErrors && !HasRequiredDecisions;

    private static int SeverityOrder(ValidationSeverity severity)
    {
        return severity switch
        {
            ValidationSeverity.Error => 0,
            ValidationSeverity.Warning => 1,
            ValidationSeverity.Information => 2,
            ValidationSeverity.Ready => 3,
            _ => 99
        };
    }
}
