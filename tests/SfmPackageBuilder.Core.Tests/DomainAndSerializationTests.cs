using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SfmPackageBuilder.Core.Domain;
using SfmPackageBuilder.Core.Persistence;

namespace SfmPackageBuilder.Core.Tests;

[TestClass]
public sealed class DomainAndSerializationTests
{
    [TestMethod]
    public void DefaultDomainState()
    {
        var project = new PackageProject();

        Assert.AreEqual(ProjectSchemaVersion.Current, project.SchemaVersion);
        Assert.AreEqual(string.Empty, project.AssetName);
        Assert.AreEqual(string.Empty, project.CurrentVersion);
        Assert.AreEqual(string.Empty, project.Credits);
        Assert.AreEqual(string.Empty, project.ArchiveName);
        Assert.AreEqual(0, project.Models.Count);
        Assert.AreEqual(0, project.MaterialSources.Count);
        Assert.AreEqual(0, project.Extras.Count);
        Assert.AreEqual(ReadmeMode.Generated, project.Readme.Mode);
        Assert.IsTrue(project.Readme.IncludeInstallInstructions);
        Assert.IsTrue(project.Readme.IncludeModelPath);
        Assert.IsTrue(project.Readme.IncludeVersionChanges);
        Assert.AreEqual(0, project.ReleaseHistory.Count);
        Assert.AreEqual(0, project.BuildHistory.Count);
    }

    [TestMethod]
    public void MultipleModelEntries()
    {
        var project = TestProjectFactory.CreateRepresentativeProject();

        Assert.AreEqual(2, project.Models.Count);
        Assert.AreEqual(@"E:\SFM\game\usermod\models\Creator\props\chair\chair_dev.mdl", project.Models[0].SourceMdlPath);
        Assert.AreEqual(@"E:\SFM\game\usermod\models\Creator\shared\hinge.mdl", project.Models[1].SourceMdlPath);
    }

    [TestMethod]
    public void PrimaryAdditionalModelRepresentation()
    {
        var project = TestProjectFactory.CreateRepresentativeProject();

        Assert.AreEqual(ModelRole.Primary, project.Models[0].Role);
        Assert.AreEqual(ModelRole.Additional, project.Models[1].Role);
        Assert.AreEqual("chair_release", project.Models[0].ReleaseStem);
        Assert.AreEqual("hinge", project.Models[1].ReleaseStem);
    }

    [TestMethod]
    public void AdditionalModelReleaseStemPersists()
    {
        var project = TestProjectFactory.CreateRepresentativeProject();
        project.Models[1].SourceStem = "shirt_dev04";
        project.Models[1].ReleaseStem = "Gwen_Shirt";

        var reopened = RoundTrip(project);

        Assert.AreEqual(ModelRole.Additional, reopened.Models[1].Role);
        Assert.AreEqual("shirt_dev04", reopened.Models[1].SourceStem);
        Assert.AreEqual("Gwen_Shirt", reopened.Models[1].ReleaseStem);
    }

    [TestMethod]
    public void CompanionPersistenceStoresUserIntentOnly()
    {
        var project = TestProjectFactory.CreateRepresentativeProject();

        Assert.AreEqual(CompanionUserSelection.Include, project.Models[0].Companions[0].UserSelection);
        Assert.AreEqual(CompanionUserSelection.Exclude, project.Models[1].Companions[0].UserSelection);

        var reopened = RoundTrip(project);

        Assert.AreEqual(CompanionUserSelection.Include, reopened.Models[0].Companions[0].UserSelection);
        Assert.AreEqual(CompanionUserSelection.Exclude, reopened.Models[1].Companions[0].UserSelection);
    }

    [TestMethod]
    public void ReadmeConfigurationPersistence()
    {
        var project = TestProjectFactory.CreateRepresentativeProject();

        project.Readme.Mode = ReadmeMode.Generated;
        project.Readme.Description = "A chair prop.";
        project.Readme.Author = "Creator";
        project.Readme.Website = "https://example.test";
        project.Readme.License = "Custom";
        project.Readme.AdditionalResources = "Blend file available separately.";
        project.Credits = "Original model by Example Creator.";
        project.Readme.IncludeInstallInstructions = false;
        project.Readme.IncludeModelPath = false;
        project.Readme.IncludeVersionChanges = false;

        var reopened = RoundTrip(project);

        Assert.AreEqual(ReadmeMode.Generated, reopened.Readme.Mode);
        Assert.AreEqual("A chair prop.", reopened.Readme.Description);
        Assert.AreEqual("Creator", reopened.Readme.Author);
        Assert.AreEqual("https://example.test", reopened.Readme.Website);
        Assert.AreEqual("Custom", reopened.Readme.License);
        Assert.AreEqual("Blend file available separately.", reopened.Readme.AdditionalResources);
        Assert.AreEqual("Original model by Example Creator.", reopened.Credits);
        Assert.IsFalse(reopened.Readme.IncludeInstallInstructions);
        Assert.IsFalse(reopened.Readme.IncludeModelPath);
        Assert.IsFalse(reopened.Readme.IncludeVersionChanges);
    }

    [TestMethod]
    public void LegacyIncludeControlGroupsInfoLoadsSafelyAndIsOmittedFromNewSave()
    {
        var project = TestProjectFactory.CreateRepresentativeProject();
        var json = new ProjectSerializer().Serialize(project);
        json = json.Replace(
            "\"includeVersionChanges\": true,",
            "\"includeVersionChanges\": true,\r\n    \"includeControlGroupsInfo\": true,",
            StringComparison.Ordinal);

        var result = new ProjectSerializer().Deserialize(json);
        var saved = new ProjectSerializer().Serialize(result.Project!);

        Assert.AreEqual(ProjectLoadStatus.Success, result.Status);
        Assert.IsFalse(result.Project!.Readme.IncludeControlGroupsInfo);
        Assert.IsFalse(saved.Contains("includeControlGroupsInfo", StringComparison.Ordinal));
    }

    [TestMethod]
    public void GeneratedCustomImportedReadmeStatesPersist()
    {
        var generated = TestProjectFactory.CreateRepresentativeProject();
        generated.Readme.Mode = ReadmeMode.Generated;
        generated.Readme.CustomReadmeText = null;
        generated.Readme.ImportedReadmePath = null;
        Assert.AreEqual(ReadmeMode.Generated, RoundTrip(generated).Readme.Mode);

        var custom = TestProjectFactory.CreateRepresentativeProject();
        custom.Readme.Mode = ReadmeMode.Custom;
        custom.Readme.CustomSource = ReadmeCustomSource.ProjectText;
        custom.Readme.CustomReadmeText = "CUSTOM README";
        var reopenedCustom = RoundTrip(custom);
        Assert.AreEqual("CUSTOM README", reopenedCustom.Readme.CustomReadmeText);
        Assert.AreEqual(ReadmeCustomSource.ProjectText, reopenedCustom.Readme.CustomSource);

        var imported = TestProjectFactory.CreateRepresentativeProject();
        imported.Readme.Mode = ReadmeMode.Custom;
        imported.Readme.CustomSource = ReadmeCustomSource.ImportedFile;
        imported.Readme.ImportedReadmePath = @"E:\ReleaseDocs\README.txt";
        var reopenedImported = RoundTrip(imported);
        Assert.AreEqual(@"E:\ReleaseDocs\README.txt", reopenedImported.Readme.ImportedReadmePath);
        Assert.AreEqual(ReadmeCustomSource.ImportedFile, reopenedImported.Readme.CustomSource);
    }

    [TestMethod]
    public void DestinationOverridesSurviveRoundTrip()
    {
        var project = TestProjectFactory.CreateRepresentativeProject();

        var reopened = RoundTrip(project);

        Assert.IsNotNull(reopened.MaterialSources[1].DestinationOverride);
        Assert.AreEqual(DestinationOverrideKind.Custom, reopened.MaterialSources[1].DestinationOverride!.Kind);
        Assert.AreEqual(@"materials\models\Creator\shared", reopened.MaterialSources[1].DestinationOverride!.RelativePath);
        Assert.IsNotNull(reopened.Extras[0].DestinationOverride);
        Assert.AreEqual(DestinationOverrideKind.Root, reopened.Extras[0].DestinationOverride!.Kind);
    }

    [TestMethod]
    public void ReleaseHistorySurvivesRoundTrip()
    {
        var project = TestProjectFactory.CreateRepresentativeProject();

        var reopened = RoundTrip(project);

        Assert.AreEqual(2, reopened.ReleaseHistory.Count);
        Assert.AreEqual("1.0", reopened.ReleaseHistory[0].Version);
        Assert.AreEqual("Initial release.", reopened.ReleaseHistory[0].Changes[0]);
        Assert.AreEqual("1.1", reopened.ReleaseHistory[1].Version);
        Assert.AreEqual("Improved collision model.", reopened.ReleaseHistory[1].Changes[1]);
    }

    [TestMethod]
    public void OldProjectWithoutCreditsLoadsBlankCredits()
    {
        var json = """
            {
              "schemaVersion": 2,
              "assetName": "Old",
              "currentVersion": "1.0",
              "changesThisVersion": [],
              "archiveName": "old.zip",
              "models": [],
              "materialSources": [],
              "extras": [],
              "readme": {},
              "releaseHistory": [],
              "buildHistory": [],
              "informationalState": []
            }
            """;

        var result = new ProjectSerializer().Deserialize(json);

        Assert.IsTrue(result.IsSuccess, result.Message);
        Assert.AreEqual(string.Empty, result.Project!.Credits);
        Assert.AreEqual(ProjectSchemaVersion.Current, result.Project.SchemaVersion);
    }

    [TestMethod]
    public void ReleaseAndBuildRecordsRemainStructurallySeparate()
    {
        var releaseProperties = typeof(ReleaseRecord).GetProperties().Select(property => property.Name).Order().ToArray();
        var buildProperties = typeof(BuildRecord).GetProperties().Select(property => property.Name).Order().ToArray();

        CollectionAssert.AreEqual(new[] { "Changes", "Version" }, releaseProperties);
        CollectionAssert.AreEqual(new[] { "ArchiveName", "BuildTimestamp", "Version" }, buildProperties);
    }

    [TestMethod]
    public void BuildHistorySupportsZeroOneOrManyBuildsPerReleaseWithoutChangingChangelog()
    {
        var project = new PackageProject
        {
            CurrentVersion = "1.0",
            ChangesThisVersion = new List<string> { "Initial release." },
            ReleaseHistory = new List<ReleaseRecord>
            {
                new() { Version = "1.0", Changes = new List<string> { "Initial release." } }
            }
        };

        var zeroBuilds = RoundTrip(project);
        Assert.AreEqual(1, zeroBuilds.ReleaseHistory.Count);
        Assert.AreEqual(0, zeroBuilds.BuildHistory.Count);

        project.BuildHistory.Add(new BuildRecord
        {
            Version = "1.0",
            ArchiveName = "chair-1.zip",
            BuildTimestamp = DateTimeOffset.Parse("2026-01-01T10:00:00-05:00")
        });
        project.BuildHistory.Add(new BuildRecord
        {
            Version = "1.0",
            ArchiveName = "chair-1-rebuild.zip",
            BuildTimestamp = DateTimeOffset.Parse("2026-01-01T11:00:00-05:00")
        });

        var manyBuilds = RoundTrip(project);

        Assert.AreEqual(1, manyBuilds.ReleaseHistory.Count);
        Assert.AreEqual(2, manyBuilds.BuildHistory.Count);
        CollectionAssert.AreEqual(new[] { "chair-1.zip", "chair-1-rebuild.zip" }, manyBuilds.BuildHistory.Select(record => record.ArchiveName).ToArray());
    }

    [TestMethod]
    public void EditingOrRemovingChangelogEntriesDoesNotAlterBuildHistory()
    {
        var project = new PackageProject
        {
            ReleaseHistory = new List<ReleaseRecord>
            {
                new() { Version = "1.0", Changes = new List<string> { "Initial." } },
                new() { Version = "0.9", Changes = new List<string> { "Beta." } }
            },
            BuildHistory = new List<BuildRecord>
            {
                new()
                {
                    Version = "1.0",
                    ArchiveName = "chair.zip",
                    BuildTimestamp = DateTimeOffset.Parse("2026-01-01T10:00:00-05:00")
                }
            }
        };

        project.EditReleaseEntry(0, "1.0 final", new[] { "Edited." });
        project.RemoveReleaseEntry(1);

        Assert.AreEqual(1, project.ReleaseHistory.Count);
        Assert.AreEqual("1.0 final", project.ReleaseHistory[0].Version);
        Assert.AreEqual(1, project.BuildHistory.Count);
        Assert.AreEqual("1.0", project.BuildHistory[0].Version);
        Assert.AreEqual("chair.zip", project.BuildHistory[0].ArchiveName);
    }

    [TestMethod]
    public void ReleaseHistoryDomainSupportsCurrentAndAccumulatedReleases()
    {
        var project = new PackageProject
        {
            CurrentVersion = "1.2",
            ChangesThisVersion = new List<string> { "Current release." },
            ReleaseHistory = new List<ReleaseRecord>
            {
                new() { Version = "1.1", Changes = new List<string> { "Prior release." } },
                new() { Version = "1.0", Changes = new List<string> { "Initial release." } }
            }
        };

        var history = project.GetReadmeReleaseHistory();

        CollectionAssert.AreEqual(new[] { "1.2", "1.1", "1.0" }, history.Select(record => record.Version).ToArray());
    }

    [TestMethod]
    public void BeginNewReleasePreservesPreviousCurrentReleaseOnce()
    {
        var project = new PackageProject
        {
            CurrentVersion = "1.0",
            ChangesThisVersion = new List<string> { "Initial release." }
        };

        project.CurrentVersion = "1";
        project.CurrentVersion = "1.";
        project.CurrentVersion = "1.0";
        Assert.AreEqual(0, project.ReleaseHistory.Count);

        project.BeginNewRelease("1.1", new[] { "Second release." });

        Assert.AreEqual("1.1", project.CurrentVersion);
        CollectionAssert.AreEqual(new[] { "Second release." }, project.ChangesThisVersion);
        Assert.AreEqual(1, project.ReleaseHistory.Count);
        Assert.AreEqual("1.0", project.ReleaseHistory[0].Version);
        CollectionAssert.AreEqual(new[] { "Initial release." }, project.ReleaseHistory[0].Changes);
    }

    [TestMethod]
    public void ReleaseHistoryEditRemoveBlankAndArbitraryVersionTextArePreserved()
    {
        var project = new PackageProject
        {
            ReleaseHistory = new List<ReleaseRecord>
            {
                new() { Version = "beta banana", Changes = new List<string> { "" } },
                new() { Version = "v1-final-ish", Changes = new List<string> { "Released." } }
            }
        };

        project.EditReleaseEntry(0, "beta banana rev A", new[] { "  " });
        project.RemoveReleaseEntry(1);
        var reopened = RoundTrip(project);

        Assert.AreEqual(1, reopened.ReleaseHistory.Count);
        Assert.AreEqual("beta banana rev A", reopened.ReleaseHistory[0].Version);
        Assert.AreEqual(1, reopened.ReleaseHistory[0].Changes.Count);
        Assert.AreEqual(string.Empty, reopened.ReleaseHistory[0].Changes[0]);
    }

    [TestMethod]
    public void LegacySingleReleaseProjectMigratesToReleaseHistory()
    {
        var json =
            """
            {
              "schemaVersion": 1,
              "assetName": "Chair",
              "currentVersion": "1.0",
              "changesThisVersion": [ "Initial release." ],
              "archiveName": "chair.zip",
              "models": [],
              "materialSources": [],
              "extras": [],
              "readme": {},
              "releaseHistory": [],
              "informationalState": []
            }
            """;

        var result = new ProjectSerializer().Deserialize(json);

        Assert.AreEqual(ProjectLoadStatus.Success, result.Status);
        Assert.AreEqual(ProjectSchemaVersion.Current, result.Project!.SchemaVersion);
        Assert.AreEqual(1, result.Project.ReleaseHistory.Count);
        Assert.AreEqual("1.0", result.Project.ReleaseHistory[0].Version);
        CollectionAssert.AreEqual(new[] { "Initial release." }, result.Project.ReleaseHistory[0].Changes);
        Assert.AreEqual(0, result.Project.BuildHistory.Count);
    }

    [TestMethod]
    public void LegacyCombinedReleaseBuildRecordsMigrateIntoSeparateHistories()
    {
        var json =
            """
            {
              "schemaVersion": 2,
              "assetName": "Chair",
              "currentVersion": "1.1",
              "changesThisVersion": [ "Current." ],
              "archiveName": "chair.zip",
              "models": [],
              "materialSources": [],
              "extras": [],
              "readme": {},
              "releaseHistory": [
                {
                  "version": "1.0",
                  "changes": [ "Initial release." ],
                  "archiveName": "chair-v1.zip",
                  "buildTimestamp": "2026-01-01T10:00:00-05:00"
                },
                {
                  "version": "beta banana",
                  "changes": [ "Beta release." ],
                  "archiveName": "chair-beta.zip",
                  "buildTimestamp": "2025-12-01T10:00:00-05:00"
                }
              ],
              "informationalState": []
            }
            """;

        var result = new ProjectSerializer().Deserialize(json);

        Assert.AreEqual(ProjectLoadStatus.Success, result.Status);
        Assert.AreEqual(2, result.Project!.ReleaseHistory.Count);
        Assert.AreEqual("1.0", result.Project.ReleaseHistory[0].Version);
        CollectionAssert.AreEqual(new[] { "Initial release." }, result.Project.ReleaseHistory[0].Changes);
        Assert.AreEqual(2, result.Project.BuildHistory.Count);
        Assert.AreEqual("1.0", result.Project.BuildHistory[0].Version);
        Assert.AreEqual("chair-v1.zip", result.Project.BuildHistory[0].ArchiveName);
        Assert.AreEqual(DateTimeOffset.Parse("2026-01-01T10:00:00-05:00"), result.Project.BuildHistory[0].BuildTimestamp);
        Assert.AreEqual("beta banana", result.Project.BuildHistory[1].Version);
        Assert.AreEqual("chair-beta.zip", result.Project.BuildHistory[1].ArchiveName);

        var saved = new ProjectSerializer().Serialize(result.Project);
        using var document = JsonDocument.Parse(saved);
        var firstRelease = document.RootElement.GetProperty("releaseHistory")[0];
        Assert.IsFalse(firstRelease.TryGetProperty("archiveName", out _));
        Assert.IsFalse(firstRelease.TryGetProperty("buildTimestamp", out _));
        Assert.AreEqual("chair-v1.zip", document.RootElement.GetProperty("buildHistory")[0].GetProperty("archiveName").GetString());
    }

    [TestMethod]
    public void LegacyCombinedChangelogOnlyRecordDoesNotCreateBuildHistory()
    {
        var json =
            """
            {
              "schemaVersion": 2,
              "assetName": "Chair",
              "currentVersion": "1.0",
              "changesThisVersion": [],
              "archiveName": "chair.zip",
              "models": [],
              "materialSources": [],
              "extras": [],
              "readme": {},
              "releaseHistory": [
                {
                  "version": "1.0",
                  "changes": [ "Initial release." ]
                }
              ],
              "informationalState": []
            }
            """;

        var result = new ProjectSerializer().Deserialize(json);

        Assert.AreEqual(ProjectLoadStatus.Success, result.Status);
        Assert.AreEqual(1, result.Project!.ReleaseHistory.Count);
        Assert.AreEqual(0, result.Project.BuildHistory.Count);
    }

    [TestMethod]
    public void SchemaVersionsOneThroughFourLoadSafelyAndCurrentRoundTrips()
    {
        var serializer = new ProjectSerializer();
        foreach (var schemaVersion in new[] { 1, 2, 3, 4 })
        {
            var json =
                $$"""
                {
                  "schemaVersion": {{schemaVersion}},
                  "assetName": "Chair",
                  "currentVersion": "1.0",
                  "changesThisVersion": [ "Initial release." ],
                  "archiveName": "chair.zip",
                  "models": [
                    {
                      "id": "11111111-1111-1111-1111-111111111111",
                      "role": "primary",
                      "sourceMdlPath": "D:\\old\\models\\chair.mdl",
                      "sourceReference": {
                        "absolutePath": "D:\\old\\models\\chair.mdl",
                        "relativePath": "sources\\chair.mdl"
                      },
                      "sourceStem": "chair",
                      "releaseStem": "chair"
                    }
                  ],
                  "materialSources": [],
                  "extras": [],
                  "readme": {
                    "mode": "custom",
                    "customSource": "projectText",
                    "customReadmeText": "Legacy custom text"
                  },
                  "releaseHistory": [],
                  "informationalState": []
                }
                """;

            var result = serializer.Deserialize(json);

            Assert.AreEqual(ProjectLoadStatus.Success, result.Status, "schema " + schemaVersion);
            Assert.AreEqual(ProjectSchemaVersion.Current, result.Project!.SchemaVersion);
            Assert.AreEqual("Chair", result.Project.AssetName);
            Assert.AreEqual("Legacy custom text", result.Project.Readme.CustomReadmeText);
            Assert.IsNull(result.Project.Readme.CustomReadmeGeneratedFromFingerprint);
            Assert.IsNull(result.Project.Models[0].SourceReference!.RecoveryRootPath);
            Assert.IsNull(result.Project.Models[0].SourceReference!.RecoveryRelativePath);

            var saved = serializer.Serialize(result.Project);
            using var document = JsonDocument.Parse(saved);
            Assert.AreEqual(ProjectSchemaVersion.Current, document.RootElement.GetProperty("schemaVersion").GetInt32());
        }
    }

    [TestMethod]
    public void SameVersionCanBeRepresentedWithoutBuildDecision()
    {
        var project = TestProjectFactory.CreateRepresentativeProject();
        project.CurrentVersion = "1.1";

        var reopened = RoundTrip(project);

        Assert.AreEqual("1.1", reopened.CurrentVersion);
        Assert.IsTrue(reopened.ReleaseHistory.Any(record => record.Version == reopened.CurrentVersion));
    }

    [TestMethod]
    public void CompleteRepresentativeProjectRoundTrip()
    {
        var project = TestProjectFactory.CreateRepresentativeProject();
        var json = new ProjectSerializer().Serialize(project);
        var reopened = RoundTrip(project);
        var reopenedJson = new ProjectSerializer().Serialize(reopened);

        using var left = JsonDocument.Parse(json);
        using var right = JsonDocument.Parse(reopenedJson);
        Assert.AreEqual(left.RootElement.GetRawText(), right.RootElement.GetRawText());
    }

    [TestMethod]
    public void SchemaVersionSurvivesRoundTrip()
    {
        var reopened = RoundTrip(TestProjectFactory.CreateRepresentativeProject());

        Assert.AreEqual(ProjectSchemaVersion.Current, reopened.SchemaVersion);
    }

    [TestMethod]
    public void UnsupportedFutureSchemaVersionIsDeliberate()
    {
        var project = TestProjectFactory.CreateRepresentativeProject();
        var json = new ProjectSerializer().Serialize(project)
            .Replace($"\"schemaVersion\": {ProjectSchemaVersion.Current}", "\"schemaVersion\": 999", StringComparison.Ordinal);

        var result = new ProjectSerializer().Deserialize(json);

        Assert.AreEqual(ProjectLoadStatus.UnsupportedFutureSchema, result.Status);
        Assert.IsNull(result.Project);
    }

    [TestMethod]
    public void GlobalAppSettingsAreNotWrittenIntoSfmpack()
    {
        const string legacySevenZipPath = @"C:\Program Files\7-Zip\7z.exe";
        var settings = new AppSettings
        {
            DefaultOutputDirectory = @"E:\Releases",
            DefaultProjectDirectory = @"E:\SFM Projects",
            DefaultArchivePattern = "{AssetName}-{Version}.zip",
            DefaultReadmeTemplate = "Machine default README template.",
            RememberRecentProjects = false,
            RecentProjectPaths = new List<string> { @"E:\Projects\Chair.sfmpack" },
            RecentProjects = new List<RecentProjectEntry>
            {
                new() { ProjectPath = @"E:\Projects\Chair.sfmpack", DisplayName = "Chair", LastAccessed = DateTimeOffset.Parse("2026-01-01T00:00:00Z") }
            },
            CreatorDefaults = new CreatorDefaults
            {
                Author = "Machine Default Author",
                Website = "https://machine-default.example",
                License = "Machine Default License"
            }
        };

        var json = new ProjectSerializer().Serialize(TestProjectFactory.CreateRepresentativeProject());

        Assert.IsFalse(json.Contains(legacySevenZipPath, StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("sevenZipPath", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(json.Contains(settings.DefaultOutputDirectory, StringComparison.Ordinal));
        Assert.IsFalse(json.Contains(settings.DefaultProjectDirectory, StringComparison.Ordinal));
        Assert.IsFalse(json.Contains(settings.DefaultArchivePattern, StringComparison.Ordinal));
        Assert.IsFalse(json.Contains(settings.DefaultReadmeTemplate, StringComparison.Ordinal));
        Assert.IsFalse(json.Contains(settings.RecentProjectPaths[0], StringComparison.Ordinal));
        Assert.IsFalse(json.Contains(settings.CreatorDefaults.Author, StringComparison.Ordinal));
    }

    private static PackageProject RoundTrip(PackageProject project)
    {
        var serializer = new ProjectSerializer();
        var result = serializer.Deserialize(serializer.Serialize(project));
        Assert.AreEqual(ProjectLoadStatus.Success, result.Status);
        Assert.IsNotNull(result.Project);
        return result.Project!;
    }
}
