using SfmPackageBuilder.Core.Archive;
using SfmPackageBuilder.Core.Domain;

namespace SfmPackageBuilder.Core.Build;

public sealed class BuildRequest
{
    public required PackageProject Project { get; init; }

    public string OutputDirectory { get; init; } = string.Empty;

    public VersionReuseDecision VersionReuseDecision { get; init; } = VersionReuseDecision.None;

    public ExistingOutputDecision ExistingOutputDecision { get; init; } = ExistingOutputDecision.None;
}
