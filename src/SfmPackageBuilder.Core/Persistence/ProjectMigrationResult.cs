using SfmPackageBuilder.Core.Domain;

namespace SfmPackageBuilder.Core.Persistence;

public sealed class ProjectMigrationResult
{
    private ProjectMigrationResult(ProjectLoadStatus status, int schemaVersion, string? migratedJson, string? message)
    {
        Status = status;
        SchemaVersion = schemaVersion;
        MigratedJson = migratedJson;
        Message = message;
    }

    public ProjectLoadStatus Status { get; }

    public int SchemaVersion { get; }

    public string? MigratedJson { get; }

    public string? Message { get; }

    public bool IsSuccess => Status == ProjectLoadStatus.Success;

    public static ProjectMigrationResult Success(int schemaVersion) =>
        new(ProjectLoadStatus.Success, schemaVersion, null, null);

    public static ProjectMigrationResult Success(int schemaVersion, string migratedJson) =>
        new(ProjectLoadStatus.Success, schemaVersion, migratedJson, null);

    public static ProjectMigrationResult Failure(ProjectLoadStatus status, int schemaVersion, string message) =>
        new(status, schemaVersion, null, message);
}
