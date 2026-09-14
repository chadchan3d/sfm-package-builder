using SfmPackageBuilder.Core.FileSystem;
using SfmPackageBuilder.Core.Build;

namespace SfmPackageBuilder.Core.Staging;

public sealed class CopyPlanExecutor
{
    private readonly IFileSystem fileSystem;

    public CopyPlanExecutor(IFileSystem fileSystem)
    {
        this.fileSystem = fileSystem;
    }

    internal StagingExecutionResult Execute(IReadOnlyList<StagingPlanEntry> entries, string stagingRoot)
    {
        var files = new List<StagedPackageFile>();
        var duplicates = new List<StagingDuplicateWrite>();
        var orderedGroups = entries
            .GroupBy(entry => entry.Path.NormalizedDestination, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var group in orderedGroups)
        {
            var groupedEntries = group
                .OrderBy(entry => entry.Entry.Id, StringComparer.Ordinal)
                .ToArray();
            var entry = groupedEntries[0];

            if (groupedEntries.Length > 1 && entry.Entry.SourcePath is not null)
            {
                duplicates.Add(new StagingDuplicateWrite(
                    entry.Path.NormalizedDestination,
                    entry.Entry.SourcePath,
                    groupedEntries.Skip(1).Select(duplicate => duplicate.Entry.Id)));
            }

            try
            {
                var parent = Path.GetDirectoryName(entry.Path.FullPath);
                if (!string.IsNullOrEmpty(parent))
                {
                    Directory.CreateDirectory(parent);
                }

                if (File.Exists(entry.Path.FullPath))
                {
                    return StagingExecutionResult.Failed(new StagingFailure(
                        StagingFailureKind.DestinationAlreadyExists,
                        "The staged destination already exists unexpectedly.",
                        stagingRoot,
                        entry.Entry.Id,
                        entry.Entry.SourcePath,
                        entry.Entry.DestinationRelativePath), files, duplicates);
                }

                if (entry.IsSourceBacked)
                {
                    fileSystem.CopyFile(entry.Entry.SourcePath!, entry.Path.FullPath, overwrite: false);
                }
                else
                {
                    File.WriteAllBytes(entry.Path.FullPath, entry.ContentBytes ?? Array.Empty<byte>());
                }

                ClearReadOnlyOnStagedCopy(entry.Path.FullPath);

                files.Add(new StagedPackageFile(
                    entry.Entry.Id,
                    entry.Path.NormalizedDestination,
                    entry.Path.FullPath,
                    entry.WriteKind,
                    entry.Entry.SourcePath,
                    new FileInfo(entry.Path.FullPath).Length));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return StagingExecutionResult.Failed(new StagingFailure(
                    StagingFailureKind.WriteFailed,
                    "The package entry could not be staged.",
                    stagingRoot,
                    entry.Entry.Id,
                    entry.Entry.SourcePath,
                    entry.Entry.DestinationRelativePath,
                    ex.Message), files, duplicates);
            }
        }

        return StagingExecutionResult.Success(files, duplicates);
    }

    internal async Task<StagingExecutionResult> ExecuteAsync(
        IReadOnlyList<StagingPlanEntry> entries,
        string stagingRoot,
        IProgress<BuildProgress>? progress,
        CancellationToken cancellationToken)
    {
        var files = new List<StagedPackageFile>();
        var duplicates = new List<StagingDuplicateWrite>();
        var orderedGroups = entries
            .GroupBy(entry => entry.Path.NormalizedDestination, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var total = orderedGroups.Length;
        var completed = 0;

        foreach (var group in orderedGroups)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var groupedEntries = group
                .OrderBy(entry => entry.Entry.Id, StringComparer.Ordinal)
                .ToArray();
            var entry = groupedEntries[0];
            progress?.Report(new BuildProgress(BuildProgressPhase.PreparingFiles, completed, total, entry.Path.NormalizedDestination));

            if (groupedEntries.Length > 1 && entry.Entry.SourcePath is not null)
            {
                duplicates.Add(new StagingDuplicateWrite(
                    entry.Path.NormalizedDestination,
                    entry.Entry.SourcePath,
                    groupedEntries.Skip(1).Select(duplicate => duplicate.Entry.Id)));
            }

            try
            {
                var parent = Path.GetDirectoryName(entry.Path.FullPath);
                if (!string.IsNullOrEmpty(parent))
                {
                    Directory.CreateDirectory(parent);
                }

                if (File.Exists(entry.Path.FullPath))
                {
                    return StagingExecutionResult.Failed(new StagingFailure(
                        StagingFailureKind.DestinationAlreadyExists,
                        "The staged destination already exists unexpectedly.",
                        stagingRoot,
                        entry.Entry.Id,
                        entry.Entry.SourcePath,
                        entry.Entry.DestinationRelativePath), files, duplicates);
                }

                if (entry.IsSourceBacked)
                {
                    await CopySourceFileAsync(entry.Entry.SourcePath!, entry.Path.FullPath, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await File.WriteAllBytesAsync(entry.Path.FullPath, entry.ContentBytes ?? Array.Empty<byte>(), cancellationToken).ConfigureAwait(false);
                }

                ClearReadOnlyOnStagedCopy(entry.Path.FullPath);

                files.Add(new StagedPackageFile(
                    entry.Entry.Id,
                    entry.Path.NormalizedDestination,
                    entry.Path.FullPath,
                    entry.WriteKind,
                    entry.Entry.SourcePath,
                    new FileInfo(entry.Path.FullPath).Length));
                completed++;
                progress?.Report(new BuildProgress(BuildProgressPhase.PreparingFiles, completed, total, entry.Path.NormalizedDestination));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return StagingExecutionResult.Failed(new StagingFailure(
                    StagingFailureKind.WriteFailed,
                    CreateWriteFailureMessage(ex),
                    stagingRoot,
                    entry.Entry.Id,
                    entry.Entry.SourcePath,
                    entry.Entry.DestinationRelativePath,
                    ex.Message), files, duplicates);
            }
        }

        return StagingExecutionResult.Success(files, duplicates);
    }

    private async Task CopySourceFileAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken)
    {
        if (fileSystem is not PhysicalFileSystem)
        {
            cancellationToken.ThrowIfCancellationRequested();
            fileSystem.CopyFile(sourcePath, destinationPath, overwrite: false);
            return;
        }

        await using var source = fileSystem.OpenRead(sourcePath);
        await using var destination = new FileStream(
            destinationPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 1024 * 64,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        await source.CopyToAsync(destination, 1024 * 64, cancellationToken).ConfigureAwait(false);
    }

    private static string CreateWriteFailureMessage(Exception ex) =>
        IsDiskFull(ex)
            ? "There isn't enough free space to build this ZIP."
            : "The package entry could not be staged.";

    private static bool IsDiskFull(Exception ex) =>
        ex is IOException io && ((uint)io.HResult == 0x80070070 || (uint)io.HResult == 0x80070027);

    private static void ClearReadOnlyOnStagedCopy(string stagedPath)
    {
        var attributes = File.GetAttributes(stagedPath);
        if ((attributes & FileAttributes.ReadOnly) == 0)
        {
            return;
        }

        File.SetAttributes(stagedPath, attributes & ~FileAttributes.ReadOnly);
    }
}
