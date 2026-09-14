using SfmPackageBuilder.Core.FileSystem;
using SfmPackageBuilder.Core.Planning;

namespace SfmPackageBuilder.Core.Staging;

public sealed class StagedPackageVerifier
{
    private readonly IFileSystem fileSystem;

    public StagedPackageVerifier(IFileSystem fileSystem)
    {
        this.fileSystem = fileSystem;
    }

    public StagedPackageVerificationResult Verify(PackagePlan plan, string stagingRoot)
    {
        return VerifyAsync(plan, stagingRoot).GetAwaiter().GetResult();
    }

    public async Task<StagedPackageVerificationResult> VerifyAsync(
        PackagePlan plan,
        string stagingRoot,
        CancellationToken cancellationToken = default)
    {
        var preflight = new StagingPlanPreflight(fileSystem).Prepare(plan, stagingRoot);
        return preflight.Succeeded
            ? await VerifyAsync(preflight.Entries, stagingRoot, cancellationToken).ConfigureAwait(false)
            : StagedPackageVerificationResult.Failed(preflight.Failure!);
    }

    internal StagedPackageVerificationResult Verify(IReadOnlyList<StagingPlanEntry> entries, string stagingRoot)
    {
        return VerifyAsync(entries, stagingRoot).GetAwaiter().GetResult();
    }

    internal async Task<StagedPackageVerificationResult> VerifyAsync(
        IReadOnlyList<StagingPlanEntry> entries,
        string stagingRoot,
        CancellationToken cancellationToken = default)
    {
        var expected = entries
            .GroupBy(entry => entry.Path.NormalizedDestination, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderBy(entry => entry.Entry.Id, StringComparer.Ordinal).First())
            .ToArray();

        foreach (var entry in expected)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!File.Exists(entry.Path.FullPath))
            {
                return StagedPackageVerificationResult.Failed(new StagingFailure(
                    StagingFailureKind.VerificationFailed,
                    "An expected staged file is missing.",
                    stagingRoot,
                    entry.Entry.Id,
                    entry.Entry.SourcePath,
                    entry.Entry.DestinationRelativePath));
            }

            var failure = await VerifyEntryBytesAsync(entry, stagingRoot, cancellationToken).ConfigureAwait(false);
            if (failure is not null)
            {
                return StagedPackageVerificationResult.Failed(failure);
            }
        }

        var expectedDestinations = expected
            .Select(entry => entry.Path.NormalizedDestination)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var stagedFile in Directory.EnumerateFiles(stagingRoot, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(stagingRoot, stagedFile);
            var normalized = string.Join('\\', relative.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries));
            if (!expectedDestinations.Contains(normalized))
            {
                return StagedPackageVerificationResult.Failed(new StagingFailure(
                    StagingFailureKind.VerificationFailed,
                    "The staging tree contains an unexpected package file.",
                    stagingRoot,
                    destinationRelativePath: normalized,
                    technicalDetail: stagedFile));
            }
        }

        return StagedPackageVerificationResult.Success();
    }

    private async Task<StagingFailure?> VerifyEntryBytesAsync(
        StagingPlanEntry entry,
        string stagingRoot,
        CancellationToken cancellationToken)
    {
        if (entry.IsSourceBacked)
        {
            if (entry.WriteKind == StagingWriteKind.ImportedReadmeCopy)
            {
                return await VerifySourceBytesAsync(entry, stagingRoot, cancellationToken).ConfigureAwait(false);
            }

            return VerifySourceSize(entry, stagingRoot);
        }

        var stagedBytes = await File.ReadAllBytesAsync(entry.Path.FullPath, cancellationToken).ConfigureAwait(false);
        if (!stagedBytes.SequenceEqual(entry.ContentBytes ?? Array.Empty<byte>()))
        {
            return new StagingFailure(
                StagingFailureKind.VerificationFailed,
                "The staged README bytes do not match the resolved package plan bytes.",
                stagingRoot,
                entry.Entry.Id,
                entry.Entry.SourcePath,
                entry.Entry.DestinationRelativePath);
        }

        return null;
    }

    private StagingFailure? VerifySourceSize(StagingPlanEntry entry, string stagingRoot)
    {
        try
        {
            using var source = fileSystem.OpenRead(entry.Entry.SourcePath!);
            var stagedLength = new FileInfo(entry.Path.FullPath).Length;
            if (source.Length != stagedLength)
            {
                return new StagingFailure(
                    StagingFailureKind.VerificationFailed,
                    "The staged source-backed file size does not match the source file size.",
                    stagingRoot,
                    entry.Entry.Id,
                    entry.Entry.SourcePath,
                    entry.Entry.DestinationRelativePath,
                    $"Source length: {source.Length}; staged length: {stagedLength}");
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return new StagingFailure(
                StagingFailureKind.SourceReadFailed,
                "The source file could not be read during staging verification.",
                stagingRoot,
                entry.Entry.Id,
                entry.Entry.SourcePath,
                entry.Entry.DestinationRelativePath,
                ex.Message);
        }

        return null;
    }

    private async Task<StagingFailure?> VerifySourceBytesAsync(
        StagingPlanEntry entry,
        string stagingRoot,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var source = fileSystem.OpenRead(entry.Entry.SourcePath!);
            await using var staged = File.OpenRead(entry.Path.FullPath);
            if (!await StreamsEqualAsync(source, staged, cancellationToken).ConfigureAwait(false))
            {
                return new StagingFailure(
                    StagingFailureKind.VerificationFailed,
                    "The imported README staged bytes do not match the source file bytes.",
                    stagingRoot,
                    entry.Entry.Id,
                    entry.Entry.SourcePath,
                    entry.Entry.DestinationRelativePath);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new StagingFailure(
                StagingFailureKind.SourceReadFailed,
                "The imported README source could not be read during staging verification.",
                stagingRoot,
                entry.Entry.Id,
                entry.Entry.SourcePath,
                entry.Entry.DestinationRelativePath,
                ex.Message);
        }

        return null;
    }

    private static async Task<bool> StreamsEqualAsync(Stream left, Stream right, CancellationToken cancellationToken)
    {
        var leftBuffer = new byte[8192];
        var rightBuffer = new byte[8192];

        while (true)
        {
            var leftRead = await left.ReadAsync(leftBuffer, cancellationToken).ConfigureAwait(false);
            var rightRead = await right.ReadAsync(rightBuffer, cancellationToken).ConfigureAwait(false);
            if (leftRead != rightRead)
            {
                return false;
            }

            if (leftRead == 0)
            {
                return true;
            }

            if (!leftBuffer.AsSpan(0, leftRead).SequenceEqual(rightBuffer.AsSpan(0, rightRead)))
            {
                return false;
            }
        }
    }
}
