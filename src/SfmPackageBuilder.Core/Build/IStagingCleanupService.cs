using SfmPackageBuilder.Core.Staging;

namespace SfmPackageBuilder.Core.Build;

public interface IStagingCleanupService
{
    StagingCleanupResult CleanSuccessfulStaging(StagingSession session);

    StagingCleanupResult CleanCancelledStaging(StagingSession session) =>
        CleanSuccessfulStaging(session);
}
