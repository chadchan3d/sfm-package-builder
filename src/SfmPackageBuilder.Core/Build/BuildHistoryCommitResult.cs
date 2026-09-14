using SfmPackageBuilder.Core.Domain;

namespace SfmPackageBuilder.Core.Build;

public sealed class BuildHistoryCommitResult
{
    public BuildHistoryCommitResult(BuildRecord record)
    {
        Record = record;
    }

    public BuildRecord Record { get; }
}
