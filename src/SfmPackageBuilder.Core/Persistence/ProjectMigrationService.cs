using System.Text.Json;
using System.Text.Json.Nodes;
using SfmPackageBuilder.Core.Domain;

namespace SfmPackageBuilder.Core.Persistence;

public sealed class ProjectMigrationService
{
    public ProjectLoadStatus EvaluateSchemaVersion(int schemaVersion)
    {
        if (schemaVersion is 1 or ProjectSchemaVersion.ReleaseHistoryAndPortableSources or ProjectSchemaVersion.Credits or ProjectSchemaVersion.Current)
        {
            return ProjectLoadStatus.Success;
        }

        return schemaVersion > ProjectSchemaVersion.Current
            ? ProjectLoadStatus.UnsupportedFutureSchema
            : ProjectLoadStatus.UnsupportedLegacySchema;
    }

    public bool CanMigrateToCurrent(int schemaVersion) =>
        EvaluateSchemaVersion(schemaVersion) == ProjectLoadStatus.Success;

    public ProjectMigrationResult MigrateToCurrent(JsonDocument document, string json)
    {
        var hasSchemaVersion =
            document.RootElement.TryGetProperty("schemaVersion", out var schemaVersionElement)
            || document.RootElement.TryGetProperty(nameof(PackageProject.SchemaVersion), out schemaVersionElement);

        if (!hasSchemaVersion || !schemaVersionElement.TryGetInt32(out var schemaVersion))
        {
            return ProjectMigrationResult.Failure(
                ProjectLoadStatus.MissingSchemaVersion,
                0,
                "The project file does not contain a supported schema version.");
        }

        var status = EvaluateSchemaVersion(schemaVersion);
        if (status != ProjectLoadStatus.Success)
        {
            return ProjectMigrationResult.Failure(
                status,
                schemaVersion,
                $"The project schema version {schemaVersion} is not supported by this application version.");
        }

        return ProjectMigrationResult.Success(schemaVersion, MigrateCombinedReleaseBuildHistory(json));
    }

    public void NormalizeAfterDeserialize(PackageProject project, int originalSchemaVersion)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (originalSchemaVersion < ProjectSchemaVersion.ReleaseHistoryAndPortableSources
            && project.ReleaseHistory.Count == 0
            && PackageProject.HasMeaningfulRelease(project.CurrentVersion, project.ChangesThisVersion))
        {
            project.ReleaseHistory.Add(new ReleaseRecord
            {
                Version = project.CurrentVersion.Trim(),
                Changes = project.ChangesThisVersion
                    .Select(change => change?.Trim() ?? string.Empty)
                    .ToList()
            });
        }

        project.SchemaVersion = ProjectSchemaVersion.Current;
        project.Readme.IncludeControlGroupsInfo = false;
    }

    private static string MigrateCombinedReleaseBuildHistory(string json)
    {
        var root = JsonNode.Parse(json)?.AsObject();
        if (root is null)
        {
            return json;
        }

        var migratedBuildRecords = new JsonArray();
        if (root.TryGetPropertyValue("releaseHistory", out var releaseHistoryNode)
            && releaseHistoryNode is JsonArray releaseHistory)
        {
            foreach (var recordNode in releaseHistory)
            {
                if (recordNode is not JsonObject record)
                {
                    continue;
                }

                if (TryCreateBuildRecord(record, out var buildRecord))
                {
                    migratedBuildRecords.Add(buildRecord);
                }

                record.Remove("archiveName");
                record.Remove("buildTimestamp");
            }
        }

        if (migratedBuildRecords.Count > 0)
        {
            if (root.TryGetPropertyValue("buildHistory", out var buildHistoryNode)
                && buildHistoryNode is JsonArray buildHistory)
            {
                foreach (var record in migratedBuildRecords)
                {
                    buildHistory.Add(record);
                }
            }
            else
            {
                root["buildHistory"] = migratedBuildRecords;
            }
        }

        return root.ToJsonString();
    }

    private static bool TryCreateBuildRecord(JsonObject legacyReleaseRecord, out JsonObject buildRecord)
    {
        buildRecord = new JsonObject();
        if (!TryGetNonWhiteSpaceString(legacyReleaseRecord, "archiveName", out var archiveName)
            || !TryGetDateTimeOffsetNode(legacyReleaseRecord, "buildTimestamp", out var buildTimestampNode))
        {
            return false;
        }

        buildRecord["version"] = TryGetNonWhiteSpaceString(legacyReleaseRecord, "version", out var version)
            ? version
            : string.Empty;
        buildRecord["archiveName"] = archiveName;
        buildRecord["buildTimestamp"] = buildTimestampNode.DeepClone();
        return true;
    }

    private static bool TryGetNonWhiteSpaceString(JsonObject source, string propertyName, out string value)
    {
        value = string.Empty;
        if (!source.TryGetPropertyValue(propertyName, out var node))
        {
            return false;
        }

        value = node?.GetValue<string>() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool TryGetDateTimeOffsetNode(JsonObject source, string propertyName, out JsonNode value)
    {
        value = JsonValue.Create(DateTimeOffset.MinValue)!;
        if (!source.TryGetPropertyValue(propertyName, out var node) || node is null)
        {
            return false;
        }

        if (!DateTimeOffset.TryParse(node.GetValue<string>(), out _))
        {
            return false;
        }

        value = node;
        return true;
    }
}
