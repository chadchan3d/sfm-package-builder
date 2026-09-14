namespace SfmPackageBuilder.Core.Validation;

public sealed class OutputValidationResult
{
    public OutputValidationResult(IReadOnlyList<ValidationMessage> messages, IReadOnlyList<RequiredDecision> requiredDecisions)
    {
        Messages = messages;
        RequiredDecisions = requiredDecisions;
    }

    public IReadOnlyList<ValidationMessage> Messages { get; }

    public IReadOnlyList<RequiredDecision> RequiredDecisions { get; }
}
