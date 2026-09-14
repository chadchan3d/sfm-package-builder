using SfmPackageBuilder.Core.Planning;

namespace SfmPackageBuilder.Core.Build;

public sealed class ReadmePreviewResult
{
    public ReadmePreviewResult(PackagePlan plan, PackagePlanEntry? readmeEntry)
    {
        Plan = plan;
        ReadmeEntry = readmeEntry;
    }

    public PackagePlan Plan { get; }

    public PackagePlanEntry? ReadmeEntry { get; }

    public bool HasReadme => ReadmeEntry is not null;

    public string? PreviewText => ReadmeEntry?.TextContent;

    public byte[]? ResolvedBytes => ReadmeEntry?.ContentBytes;
}
