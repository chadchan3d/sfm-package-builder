using SfmPackageBuilder.Core.Staging;
using SfmPackageBuilder.Core.Build;

namespace SfmPackageBuilder.Core.Archive;

public sealed class ArchiveService
{
    private readonly IZipArchiveWriter writer;
    private readonly ArchiveVerifier verifier;

    public ArchiveService(
        IZipArchiveWriter? writer = null,
        ArchiveVerifier? verifier = null)
    {
        this.writer = writer ?? new NativeZipArchiveWriter();
        this.verifier = verifier ?? new ArchiveVerifier();
    }

    public ArchiveResult CreateArchive(
        StagingSession session,
        string targetArchivePath,
        ExistingOutputDecision existingOutputDecision = ExistingOutputDecision.None)
    {
        return CreateArchiveAsync(session, targetArchivePath, existingOutputDecision).GetAwaiter().GetResult();
    }

    public async Task<ArchiveResult> CreateArchiveAsync(
        StagingSession session,
        string targetArchivePath,
        ExistingOutputDecision existingOutputDecision = ExistingOutputDecision.None,
        IProgress<BuildProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetArchivePath);

        var targetPath = Path.GetFullPath(targetArchivePath);
        var outputDirectory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        if (!Directory.Exists(session.StagingRoot))
        {
            return ArchiveResult.Failed(new ArchiveFailure(
                ArchiveFailureKind.StagingRootMissing,
                "The staging root is no longer available.",
                targetArchivePath: targetPath,
                technicalDetail: session.StagingRoot));
        }

        if (session.Files.Count == 0)
        {
            return ArchiveResult.Failed(new ArchiveFailure(
                ArchiveFailureKind.EmptyStagingSession,
                "The staging session has no package files to archive.",
                targetArchivePath: targetPath));
        }

        var targetExists = File.Exists(targetPath);
        var outputDecisionFailure = ValidateExistingOutputDecision(targetPath, targetExists, existingOutputDecision);
        if (outputDecisionFailure is not null)
        {
            return ArchiveResult.Failed(outputDecisionFailure);
        }

        var isReplacing = targetExists && existingOutputDecision == ExistingOutputDecision.Replace;
        var archivePathForWrite = CreateTemporaryArchivePath(targetPath);

        try
        {
            return await CreateAndVerifyArchiveAsync(session, targetPath, archivePathForWrite, isReplacing, progress, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            TryDeleteArchive(archivePathForWrite, preservePath: targetPath);
        }
    }

    private async Task<ArchiveResult> CreateAndVerifyArchiveAsync(
        StagingSession session,
        string targetPath,
        string archivePathForWrite,
        bool isReplacing,
        IProgress<BuildProgress>? progress,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (writer is IAsyncZipArchiveWriter asyncWriter)
            {
                await asyncWriter.CreateFromStagingAsync(session, archivePathForWrite, progress, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await Task.Run(() => writer.CreateFromStaging(session, archivePathForWrite), cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            TryDeleteArchive(archivePathForWrite, preservePath: targetPath);
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            TryDeleteArchive(archivePathForWrite, preservePath: targetPath);
            return ArchiveResult.Failed(new ArchiveFailure(
                ArchiveFailureKind.ZipCreationFailed,
                CreateArchiveFailureMessage(ex),
                targetArchivePath: targetPath,
                temporaryArchivePath: archivePathForWrite,
                technicalDetail: ex.Message));
        }

        if (!File.Exists(archivePathForWrite))
        {
            return ArchiveResult.Failed(new ArchiveFailure(
                ArchiveFailureKind.ArchiveMissing,
                "ZIP creation completed but the expected archive was not created.",
                targetArchivePath: targetPath,
                temporaryArchivePath: archivePathForWrite));
        }

        ArchiveVerificationResult verification;
        try
        {
            verification = await verifier.VerifyAsync(archivePathForWrite, session, progress, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            TryDeleteArchive(archivePathForWrite, preservePath: targetPath);
            throw;
        }
        if (!verification.Succeeded)
        {
            TryDeleteArchive(archivePathForWrite, preservePath: targetPath);
            return ArchiveResult.Failed(new ArchiveFailure(
                    ArchiveFailureKind.ArchiveVerificationFailed,
                    verification.FailureMessage ?? "The produced archive failed verification.",
                    targetArchivePath: targetPath,
                    temporaryArchivePath: archivePathForWrite,
                    technicalDetail: verification.TechnicalDetail),
                verification: verification);
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (isReplacing)
            {
                File.Replace(archivePathForWrite, targetPath, destinationBackupFileName: null);
            }
            else
            {
                File.Move(archivePathForWrite, targetPath);
            }
        }
        catch (OperationCanceledException)
        {
            TryDeleteArchive(archivePathForWrite, preservePath: targetPath);
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return ArchiveResult.Failed(new ArchiveFailure(
                    ArchiveFailureKind.ReplaceFailed,
                    isReplacing
                        ? "The existing ZIP couldn't be replaced. It may be open in another program."
                        : "The ZIP archive couldn't be finalized.",
                    targetArchivePath: targetPath,
                    temporaryArchivePath: archivePathForWrite,
                    technicalDetail: ex.Message),
                verification: verification);
        }

        return ArchiveResult.Success(
            targetPath,
            isReplacing ? ArchiveReplacementBehavior.ReplacedExisting : ArchiveReplacementBehavior.TargetCreated,
            verification);
    }

    private static ArchiveFailure? ValidateExistingOutputDecision(
        string targetPath,
        bool targetExists,
        ExistingOutputDecision decision)
    {
        if (!targetExists)
        {
            return null;
        }

        return decision switch
        {
            ExistingOutputDecision.Replace => null,
            ExistingOutputDecision.Cancel => new ArchiveFailure(
                ArchiveFailureKind.ExistingOutputCancelled,
                "Archive creation was cancelled because the target archive already exists.",
                targetArchivePath: targetPath),
            ExistingOutputDecision.ChooseAnotherName => new ArchiveFailure(
                ArchiveFailureKind.ChooseAnotherNameRequiresDifferentTarget,
                "A different target archive path must be supplied when choosing another name.",
                targetArchivePath: targetPath),
            _ => new ArchiveFailure(
                ArchiveFailureKind.ExistingOutputRequiresDecision,
                "The target archive already exists and requires an explicit output decision.",
                targetArchivePath: targetPath)
        };
    }

    private static string CreateTemporaryArchivePath(string targetPath)
    {
        var directory = Path.GetDirectoryName(targetPath) ?? Directory.GetCurrentDirectory();
        var fileName = Path.GetFileNameWithoutExtension(targetPath);
        var extension = Path.GetExtension(targetPath);
        string candidate;
        do
        {
            candidate = Path.Combine(directory, $".{fileName}.sfmpack-{Guid.NewGuid():N}.tmp{extension}");
        }
        while (File.Exists(candidate) || candidate.Equals(targetPath, StringComparison.OrdinalIgnoreCase));

        return candidate;
    }

    private static string CreateArchiveFailureMessage(Exception ex) =>
        IsDiskFull(ex)
            ? "There isn't enough free space to build this ZIP."
            : "The ZIP archive could not be created.";

    private static bool IsDiskFull(Exception ex) =>
        ex is IOException io && ((uint)io.HResult == 0x80070070 || (uint)io.HResult == 0x80070027);

    private static void TryDeleteArchive(string archivePath, string? preservePath = null)
    {
        if (preservePath is not null && archivePath.Equals(preservePath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (File.Exists(archivePath))
        {
            try
            {
                File.Delete(archivePath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Cleanup is best-effort for the unique app-created temporary archive;
                // never broaden deletion or mask the archive operation outcome.
            }
        }
    }
}
