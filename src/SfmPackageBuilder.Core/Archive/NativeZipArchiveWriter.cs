using System.IO.Compression;
using SfmPackageBuilder.Core.Build;
using SfmPackageBuilder.Core.Paths;
using SfmPackageBuilder.Core.Staging;

namespace SfmPackageBuilder.Core.Archive;

public sealed class NativeZipArchiveWriter : IZipArchiveWriter, IAsyncZipArchiveWriter
{
    public void CreateFromStaging(StagingSession session, string archivePath)
    {
        CreateFromStagingAsync(session, archivePath).GetAwaiter().GetResult();
    }

    public async Task CreateFromStagingAsync(
        StagingSession session,
        string archivePath,
        IProgress<BuildProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(archivePath);

        await using var file = new FileStream(
            archivePath,
            FileMode.CreateNew,
            FileAccess.ReadWrite,
            FileShare.None,
            bufferSize: 1024 * 64,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var archive = new ZipArchive(file, ZipArchiveMode.Create);
        var files = session.Files
            .OrderBy(file => file.DestinationRelativePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var completed = 0;
        foreach (var stagedFile in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new BuildProgress(BuildProgressPhase.CreatingArchive, completed, files.Length, stagedFile.DestinationRelativePath));
            var relativePath = Path.GetRelativePath(session.StagingRoot, stagedFile.StagedPath);
            var entryName = NormalizeArchivePath(relativePath);
            var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
            await using var source = new FileStream(
                stagedFile.StagedPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 1024 * 64,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            await using var destination = entry.Open();
            await source.CopyToAsync(destination, 1024 * 64, cancellationToken).ConfigureAwait(false);
            completed++;
            progress?.Report(new BuildProgress(BuildProgressPhase.CreatingArchive, completed, files.Length, stagedFile.DestinationRelativePath));
        }
    }

    private static string NormalizeArchivePath(string path) =>
        DestinationPath.ToArchiveEntryName(path);
}
