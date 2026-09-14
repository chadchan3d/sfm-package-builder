namespace SfmPackageBuilder.Core.Staging;

public sealed class StagedPackageVerificationResult
{
    private StagedPackageVerificationResult(StagingFailure? failure)
    {
        Failure = failure;
    }

    public bool Succeeded => Failure is null;

    public StagingFailure? Failure { get; }

    public static StagedPackageVerificationResult Success() => new(null);

    public static StagedPackageVerificationResult Failed(StagingFailure failure) => new(failure);
}
