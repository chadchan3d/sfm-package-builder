using System.IO.Compression;
using SfmPackageBuilder.Core.Build;
using SfmPackageBuilder.Core.Paths;
using SfmPackageBuilder.Core.Staging;

namespace SfmPackageBuilder.Core.Archive;

public sealed class ArchiveVerifier
{
    public ArchiveVerificationResult Verify(string archivePath, StagingSession session)
    {
        return VerifyAsync(archivePath, session).GetAwaiter().GetResult();
    }

    public async Task<ArchiveVerificationResult> VerifyAsync(
        string archivePath,
        StagingSession session,
        IProgress<BuildProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var expected = session.Files
            .Select(file => NormalizeArchivePath(Path.GetRelativePath(session.StagingRoot, file.StagedPath)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        try
        {
            using var archive = ZipFile.OpenRead(archivePath);
            var actual = archive.Entries
                .Where(entry => !string.IsNullOrEmpty(entry.Name))
                .Select(entry => NormalizeArchivePath(entry.FullName))
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (ContainsWrapperDirectory(actual, session.SessionId))
            {
                return ArchiveVerificationResult.Failed(
                    expected,
                    actual,
                    "The archive contains the staging directory wrapper instead of package contents.");
            }

            var missing = expected.Except(actual, StringComparer.OrdinalIgnoreCase).ToArray();
            if (missing.Length > 0)
            {
                return ArchiveVerificationResult.Failed(
                    expected,
                    actual,
                    "The archive is missing expected package entries.",
                    string.Join(Environment.NewLine, missing));
            }

            var unexpected = actual.Except(expected, StringComparer.OrdinalIgnoreCase).ToArray();
            if (unexpected.Length > 0)
            {
                return ArchiveVerificationResult.Failed(
                    expected,
                    actual,
                    "The archive contains unexpected package entries.",
                    string.Join(Environment.NewLine, unexpected));
            }

            var files = session.Files.ToArray();
            var completed = 0;
            foreach (var stagedFile in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var expectedEntryName = NormalizeArchivePath(Path.GetRelativePath(session.StagingRoot, stagedFile.StagedPath));
                progress?.Report(new BuildProgress(BuildProgressPhase.VerifyingArchive, completed, files.Length, stagedFile.DestinationRelativePath));
                var entry = archive.Entries.SingleOrDefault(entry =>
                    string.Equals(NormalizeArchivePath(entry.FullName), expectedEntryName, StringComparison.OrdinalIgnoreCase));
                if (entry is null)
                {
                    continue;
                }

                if (!await BytesMatchAsync(stagedFile.StagedPath, entry, cancellationToken).ConfigureAwait(false))
                {
                    return ArchiveVerificationResult.Failed(
                        expected,
                        actual,
                        "The archive entry bytes do not match the staged package file.",
                        expectedEntryName);
                }

                completed++;
                progress?.Report(new BuildProgress(BuildProgressPhase.VerifyingArchive, completed, files.Length, stagedFile.DestinationRelativePath));
            }

            return ArchiveVerificationResult.Success(expected, actual);
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            return ArchiveVerificationResult.Failed(
                expected,
                Array.Empty<string>(),
                "The produced archive could not be read as a ZIP file.",
                ex.Message);
        }
    }

    private static bool ContainsWrapperDirectory(IReadOnlyList<string> entries, Guid sessionId)
    {
        var sessionName = sessionId.ToString("N");
        return entries.Any(entry =>
            entry.StartsWith(sessionName + "/", StringComparison.OrdinalIgnoreCase)
            || entry.StartsWith(sessionName + "\\", StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeArchivePath(string path) =>
        DestinationPath.ToArchiveEntryName(path);

    private static async Task<bool> BytesMatchAsync(
        string stagedPath,
        ZipArchiveEntry entry,
        CancellationToken cancellationToken)
    {
        await using var staged = new FileStream(
            stagedPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 1024 * 64,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        await using var archived = entry.Open();
        if (staged.Length != entry.Length)
        {
            return false;
        }

        var stagedBuffer = new byte[8192];
        var archivedBuffer = new byte[8192];
        while (true)
        {
            var stagedRead = await ReadBlockAsync(staged, stagedBuffer, cancellationToken).ConfigureAwait(false);
            var archivedRead = await ReadBlockAsync(archived, archivedBuffer, cancellationToken).ConfigureAwait(false);
            if (stagedRead != archivedRead)
            {
                return false;
            }

            if (stagedRead == 0)
            {
                return true;
            }

            if (!stagedBuffer.AsSpan(0, stagedRead).SequenceEqual(archivedBuffer.AsSpan(0, archivedRead)))
            {
                return false;
            }
        }
    }

    private static async Task<int> ReadBlockAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(total), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            total += read;
        }

        return total;
    }
}
