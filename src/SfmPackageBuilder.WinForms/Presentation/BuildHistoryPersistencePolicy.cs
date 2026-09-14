namespace SfmPackageBuilder.WinForms.Presentation;

public enum BuildHistoryPersistenceStatus
{
    NotApplicable,
    PersistedClean,
    DeferredBecauseProjectDirty,
    DeferredBecauseProjectHasNoPath,
    Failed
}

public sealed class BuildHistoryPersistenceResult
{
    public BuildHistoryPersistenceResult(BuildHistoryPersistenceStatus status, bool shouldBeDirty, string? message = null)
    {
        Status = status;
        ShouldBeDirty = shouldBeDirty;
        Message = message;
    }

    public BuildHistoryPersistenceStatus Status { get; }

    public bool ShouldBeDirty { get; }

    public string? Message { get; }
}

public sealed class BuildHistoryPersistencePolicy
{
    public BuildHistoryPersistenceResult Apply(
        bool buildSucceeded,
        bool wasDirtyBeforeBuild,
        string? projectPath,
        Action persistProject)
    {
        if (!buildSucceeded)
        {
            return new BuildHistoryPersistenceResult(BuildHistoryPersistenceStatus.NotApplicable, wasDirtyBeforeBuild);
        }

        if (wasDirtyBeforeBuild)
        {
            return new BuildHistoryPersistenceResult(BuildHistoryPersistenceStatus.DeferredBecauseProjectDirty, true);
        }

        if (string.IsNullOrWhiteSpace(projectPath))
        {
            return new BuildHistoryPersistenceResult(BuildHistoryPersistenceStatus.DeferredBecauseProjectHasNoPath, true);
        }

        try
        {
            persistProject();
            return new BuildHistoryPersistenceResult(BuildHistoryPersistenceStatus.PersistedClean, false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or NotSupportedException)
        {
            return new BuildHistoryPersistenceResult(
                BuildHistoryPersistenceStatus.Failed,
                true,
                ex.Message);
        }
    }
}
