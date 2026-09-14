using System.Text.Json;
using System.Text.Json.Serialization;
using SfmPackageBuilder.Core.Domain;
using SfmPackageBuilder.Core.FileSystem;

namespace SfmPackageBuilder.Core.Persistence;

public sealed class ProjectSerializer
{
    private readonly ProjectMigrationService migrationService;
    private readonly ProjectSourceReferenceService sourceReferenceService;
    private readonly JsonSerializerOptions options;

    public ProjectSerializer(ProjectMigrationService? migrationService = null, IFileSystem? fileSystem = null)
    {
        this.migrationService = migrationService ?? new ProjectMigrationService();
        sourceReferenceService = new ProjectSourceReferenceService(fileSystem ?? new PhysicalFileSystem());
        options = CreateOptions();
    }

    public string Serialize(PackageProject project)
    {
        project.SchemaVersion = ProjectSchemaVersion.Current;
        project.Readme.IncludeControlGroupsInfo = false;
        sourceReferenceService.EnsureReferences(project);
        return JsonSerializer.Serialize(project, options);
    }

    public ProjectLoadResult Deserialize(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var migrationResult = migrationService.MigrateToCurrent(document, json);
            if (!migrationResult.IsSuccess)
            {
                return ProjectLoadResult.Failure(migrationResult.Status, migrationResult.Message ?? "The project file could not be read.");
            }

            var project = JsonSerializer.Deserialize<PackageProject>(migrationResult.MigratedJson ?? json, options);
            if (project is null)
            {
                return ProjectLoadResult.Failure(ProjectLoadStatus.InvalidJson, "The project file could not be read.");
            }

            migrationService.NormalizeAfterDeserialize(project, migrationResult.SchemaVersion);
            sourceReferenceService.EnsureReferences(project);
            return ProjectLoadResult.Success(project);
        }
        catch (JsonException ex)
        {
            return ProjectLoadResult.Failure(ProjectLoadStatus.InvalidJson, ex.Message);
        }
        catch (NotSupportedException ex)
        {
            return ProjectLoadResult.Failure(ProjectLoadStatus.IncompatibleProjectState, ex.Message);
        }
    }

    public void Save(string path, PackageProject project)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(project);

        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        sourceReferenceService.PrepareForSave(project, path);
        var tempPath = CreateTempPath(path);
        try
        {
            File.WriteAllText(tempPath, Serialize(project));
            if (File.Exists(path))
            {
                File.Replace(tempPath, path, destinationBackupFileName: null);
            }
            else
            {
                File.Move(tempPath, path);
            }
        }
        finally
        {
            TryDeleteTemp(tempPath);
        }
    }

    public ProjectLoadResult Load(string path)
    {
        try
        {
            var result = Deserialize(File.ReadAllText(path));
            if (result.Project is not null)
            {
                sourceReferenceService.ResolveAfterLoad(result.Project, path);
            }

            return result;
        }
        catch (FileNotFoundException ex)
        {
            return ProjectLoadResult.Failure(ProjectLoadStatus.FileNotFound, ex.Message);
        }
        catch (DirectoryNotFoundException ex)
        {
            return ProjectLoadResult.Failure(ProjectLoadStatus.FileNotFound, ex.Message);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return ProjectLoadResult.Failure(ProjectLoadStatus.FileAccessError, ex.Message);
        }
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        };

        serializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return serializerOptions;
    }

    private static string CreateTempPath(string path)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path)) ?? Directory.GetCurrentDirectory();
        var fileName = Path.GetFileName(path);
        string candidate;
        do
        {
            candidate = Path.Combine(directory, $".{fileName}.sfmpack-{Guid.NewGuid():N}.tmp");
        }
        while (File.Exists(candidate) || candidate.Equals(path, StringComparison.OrdinalIgnoreCase));

        return candidate;
    }

    private static void TryDeleteTemp(string tempPath)
    {
        try
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
