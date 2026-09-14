using SfmPackageBuilder.Core.Domain;

namespace SfmPackageBuilder.Core.Build;

public sealed class BuildHistoryCommitter
{
    private readonly Func<DateTimeOffset> clock;

    public BuildHistoryCommitter(Func<DateTimeOffset>? clock = null)
    {
        this.clock = clock ?? (() => DateTimeOffset.Now);
    }

    public BuildHistoryCommitResult Commit(PackageProject project)
    {
        ArgumentNullException.ThrowIfNull(project);

        var record = new BuildRecord
        {
            Version = project.CurrentVersion,
            ArchiveName = project.ArchiveName,
            BuildTimestamp = clock()
        };

        project.BuildHistory.Insert(0, record);
        return new BuildHistoryCommitResult(record);
    }
}
