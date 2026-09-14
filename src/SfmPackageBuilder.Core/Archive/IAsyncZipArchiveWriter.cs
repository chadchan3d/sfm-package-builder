using SfmPackageBuilder.Core.Build;
using SfmPackageBuilder.Core.Staging;

namespace SfmPackageBuilder.Core.Archive;

public interface IAsyncZipArchiveWriter
{
    Task CreateFromStagingAsync(
        StagingSession session,
        string archivePath,
        IProgress<BuildProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
