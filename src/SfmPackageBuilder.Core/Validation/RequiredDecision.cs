namespace SfmPackageBuilder.Core.Validation;

public sealed class RequiredDecision
{
    public RequiredDecision(
        RequiredDecisionKind kind,
        string code,
        string message,
        IReadOnlyList<RequiredDecisionOption> options)
    {
        Kind = kind;
        Code = code;
        Message = message;
        Options = options.ToArray();
    }

    public RequiredDecisionKind Kind { get; }

    public string Code { get; }

    public string Message { get; }

    public IReadOnlyList<RequiredDecisionOption> Options { get; }
}
