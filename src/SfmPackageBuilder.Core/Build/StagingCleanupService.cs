using SfmPackageBuilder.Core.Staging;

namespace SfmPackageBuilder.Core.Build;

public sealed class StagingCleanupService : IStagingCleanupService
{
    public StagingCleanupResult CleanSuccessfulStaging(StagingSession session)
    {
        return CleanStaging(session);
    }

    public StagingCleanupResult CleanCancelledStaging(StagingSession session)
    {
        return CleanStaging(session);
    }

    private static StagingCleanupResult CleanStaging(StagingSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (!session.IsApplicationOwned)
        {
            return new StagingCleanupResult(
                StagingCleanupStatus.SkippedNotApplicationOwned,
                session.StagingRoot,
                "The staging session root does not match the application-owned staging root for its session id.");
        }

        try
        {
            if (Directory.Exists(session.StagingRoot))
            {
                foreach (var file in Directory.EnumerateFiles(session.StagingRoot, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }

                Directory.Delete(session.StagingRoot, recursive: true);
            }

            return new StagingCleanupResult(StagingCleanupStatus.Succeeded, session.StagingRoot);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new StagingCleanupResult(StagingCleanupStatus.Failed, session.StagingRoot, ex.Message);
        }
    }
}
