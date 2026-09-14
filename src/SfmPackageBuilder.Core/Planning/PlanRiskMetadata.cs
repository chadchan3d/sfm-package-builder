using SfmPackageBuilder.Core.SharedFiles;

namespace SfmPackageBuilder.Core.Planning;

public sealed class PlanRiskMetadata
{
    public PlanRiskMetadata(IEnumerable<SharedFileNotice>? sharedFileNotices = null)
    {
        SharedFileNotices = (sharedFileNotices ?? Array.Empty<SharedFileNotice>()).ToArray();
    }

    public IReadOnlyList<SharedFileNotice> SharedFileNotices { get; }
}
