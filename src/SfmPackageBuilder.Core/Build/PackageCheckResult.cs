using SfmPackageBuilder.Core.Planning;
using SfmPackageBuilder.Core.Validation;

namespace SfmPackageBuilder.Core.Build;

public sealed class PackageCheckResult
{
    public PackageCheckResult(PackagePlan plan, ValidationResult validation)
    {
        Plan = plan;
        Validation = validation;
    }

    public PackagePlan Plan { get; }

    public ValidationResult Validation { get; }
}
