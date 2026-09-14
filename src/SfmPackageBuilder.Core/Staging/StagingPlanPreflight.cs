using SfmPackageBuilder.Core.FileSystem;
using SfmPackageBuilder.Core.Planning;

namespace SfmPackageBuilder.Core.Staging;

internal sealed class StagingPlanPreflight
{
    private readonly IFileSystem fileSystem;

    public StagingPlanPreflight(IFileSystem fileSystem)
    {
        this.fileSystem = fileSystem;
    }

    public StagingPlanPreflightResult Prepare(PackagePlan plan, string stagingRoot)
    {
        var entries = new List<StagingPlanEntry>();

        foreach (var entry in plan.Entries)
        {
            if (entry.Status != PackagePlanEntryStatus.Resolved)
            {
                return StagingPlanPreflightResult.Failed(new StagingFailure(
                    StagingFailureKind.UnresolvedPlanEntry,
                    "The final package plan contains an unresolved entry and cannot be staged.",
                    stagingRoot,
                    entry.Id,
                    entry.SourcePath,
                    entry.DestinationRelativePath,
                    entry.Status.ToString()));
            }

            if (!StagingPath.TryCreate(
                    stagingRoot,
                    entry.DestinationRelativePath,
                    out var stagingPath,
                    out var failureKind,
                    out var failureMessage,
                    out var technicalDetail))
            {
                return StagingPlanPreflightResult.Failed(new StagingFailure(
                    failureKind,
                    failureMessage,
                    stagingRoot,
                    entry.Id,
                    entry.SourcePath,
                    entry.DestinationRelativePath,
                    technicalDetail));
            }

            var writeKind = GetWriteKind(entry);
            var contentBytes = entry.ContentBytes;
            if (RequiresPlanBytes(writeKind) && contentBytes is null)
            {
                return StagingPlanPreflightResult.Failed(new StagingFailure(
                    StagingFailureKind.SourceReadFailed,
                    "The README entry does not contain resolved bytes to stage.",
                    stagingRoot,
                    entry.Id,
                    entry.SourcePath,
                    entry.DestinationRelativePath));
            }

            if (RequiresSource(writeKind))
            {
                if (string.IsNullOrWhiteSpace(entry.SourcePath))
                {
                    return StagingPlanPreflightResult.Failed(new StagingFailure(
                        StagingFailureKind.MissingSource,
                        "The plan entry does not have a source file to copy.",
                        stagingRoot,
                        entry.Id,
                        entry.SourcePath,
                        entry.DestinationRelativePath));
                }

                if (!fileSystem.FileExists(entry.SourcePath))
                {
                    return StagingPlanPreflightResult.Failed(new StagingFailure(
                        StagingFailureKind.MissingSource,
                        "The source file is missing and cannot be staged.",
                        stagingRoot,
                        entry.Id,
                        entry.SourcePath,
                        entry.DestinationRelativePath));
                }
            }

            entries.Add(new StagingPlanEntry(entry, stagingPath!, writeKind, contentBytes));
        }

        var collisionFailure = FindCollisionFailure(entries, stagingRoot);
        return collisionFailure is null
            ? StagingPlanPreflightResult.Success(entries)
            : StagingPlanPreflightResult.Failed(collisionFailure);
    }

    private static StagingFailure? FindCollisionFailure(IReadOnlyList<StagingPlanEntry> entries, string stagingRoot)
    {
        foreach (var group in entries.GroupBy(entry => entry.Path.NormalizedDestination, StringComparer.OrdinalIgnoreCase))
        {
            var groupedEntries = group.ToArray();
            if (groupedEntries.Length < 2)
            {
                continue;
            }

            var identities = groupedEntries
                .Select(entry => entry.SourceIdentity)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (identities.Length == 1)
            {
                continue;
            }

            return new StagingFailure(
                StagingFailureKind.DestinationCollision,
                "Different package entries target the same staged destination.",
                stagingRoot,
                groupedEntries[0].Entry.Id,
                groupedEntries[0].Entry.SourcePath,
                groupedEntries[0].Entry.DestinationRelativePath,
                string.Join(Environment.NewLine, groupedEntries.Select(entry => $"{entry.Entry.Id}: {entry.SourceIdentity}")));
        }

        return null;
    }

    private static StagingWriteKind GetWriteKind(PackagePlanEntry entry)
    {
        if (entry.EntryType == PackagePlanEntryType.Readme)
        {
            return entry.ReadmeKind switch
            {
                ReadmePlanEntryKind.GeneratedText => StagingWriteKind.GeneratedReadmeWrite,
                ReadmePlanEntryKind.CustomText => StagingWriteKind.CustomReadmeWrite,
                ReadmePlanEntryKind.ImportedFile => StagingWriteKind.ImportedReadmeCopy,
                _ => StagingWriteKind.SourceCopy
            };
        }

        return StagingWriteKind.SourceCopy;
    }

    private static bool RequiresPlanBytes(StagingWriteKind writeKind) =>
        writeKind is StagingWriteKind.GeneratedReadmeWrite or StagingWriteKind.CustomReadmeWrite;

    private static bool RequiresSource(StagingWriteKind writeKind) =>
        writeKind is StagingWriteKind.SourceCopy or StagingWriteKind.ImportedReadmeCopy;
}
