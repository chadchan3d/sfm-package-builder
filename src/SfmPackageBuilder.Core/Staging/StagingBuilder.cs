using SfmPackageBuilder.Core.FileSystem;
using SfmPackageBuilder.Core.Planning;
using SfmPackageBuilder.Core.Build;

namespace SfmPackageBuilder.Core.Staging;

public sealed class StagingBuilder
{
    private readonly string applicationRoot;
    private readonly IFileSystem fileSystem;
    private readonly CopyPlanExecutor executor;
    private readonly StagedPackageVerifier verifier;

    public StagingBuilder(
        string applicationRoot,
        IFileSystem? fileSystem = null,
        CopyPlanExecutor? executor = null,
        StagedPackageVerifier? verifier = null)
    {
        this.applicationRoot = Path.GetFullPath(applicationRoot);
        this.fileSystem = fileSystem ?? new PhysicalFileSystem();
        this.executor = executor ?? new CopyPlanExecutor(this.fileSystem);
        this.verifier = verifier ?? new StagedPackageVerifier(this.fileSystem);
    }

    public StagingResult Stage(PackagePlan plan)
    {
        return StageAsync(plan).GetAwaiter().GetResult();
    }

    public async Task<StagingResult> StageAsync(
        PackagePlan plan,
        IProgress<BuildProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var sessionId = Guid.NewGuid();
        var stagingRoot = Path.GetFullPath(Path.Combine(applicationRoot, ".staging", sessionId.ToString("N")));
        Directory.CreateDirectory(stagingRoot);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var preflight = new StagingPlanPreflight(fileSystem).Prepare(plan, stagingRoot);
            if (!preflight.Succeeded)
            {
                return StagingResult.Failed(preflight.Failure!);
            }

            var execution = await executor.ExecuteAsync(preflight.Entries, stagingRoot, progress, cancellationToken).ConfigureAwait(false);
            if (!execution.Succeeded)
            {
                return StagingResult.Failed(execution.Failure!);
            }

            var verification = await verifier.VerifyAsync(preflight.Entries, stagingRoot, cancellationToken).ConfigureAwait(false);
            if (!verification.Succeeded)
            {
                return StagingResult.Failed(verification.Failure!);
            }

            return StagingResult.Success(new StagingSession(
                sessionId,
                applicationRoot,
                stagingRoot,
                execution.Files,
                execution.SkippedDuplicateWrites));
        }
        catch (OperationCanceledException)
        {
            var session = new StagingSession(sessionId, applicationRoot, stagingRoot, Array.Empty<StagedPackageFile>(), Array.Empty<StagingDuplicateWrite>());
            return StagingResult.CancelledResult(
                new StagingFailure(
                    StagingFailureKind.Cancelled,
                    "Build cancelled.",
                    stagingRoot),
                session);
        }
    }
}
