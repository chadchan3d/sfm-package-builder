using SfmPackageBuilder.Core.Staging;

namespace SfmPackageBuilder.Core.Archive;

public interface IZipArchiveWriter
{
    void CreateFromStaging(StagingSession session, string archivePath);
}
