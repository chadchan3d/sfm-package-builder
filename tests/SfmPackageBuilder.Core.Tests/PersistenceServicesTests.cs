using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SfmPackageBuilder.Core.Archive;
using SfmPackageBuilder.Core.Build;
using SfmPackageBuilder.Core.Domain;
using SfmPackageBuilder.Core.Expansion;
using SfmPackageBuilder.Core.FileSystem;
using SfmPackageBuilder.Core.Persistence;
using SfmPackageBuilder.Core.Planning;
using SfmPackageBuilder.Core.Validation;

namespace SfmPackageBuilder.Core.Tests;

[TestClass]
public sealed class PersistenceServicesTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 16, 14, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void ProjectSaveOpenPreservesCompleteRecipeAndRejectsBadFormats()
    {
        using var workspace = TempWorkspace.Create();
        var path = Path.Combine(workspace.Root, "project.sfmpack");
        var project = TestProjectFactory.CreateRepresentativeProject();
        project.Readme.CustomSource = ReadmeCustomSource.ImportedFile;
        project.Readme.ImportedReadmePath = @"E:\ReleaseDocs\README-source.txt";
        var serializer = new ProjectSerializer();

        serializer.Save(path, project);
        var reopened = serializer.Load(path);

        Assert.AreEqual(ProjectLoadStatus.Success, reopened.Status);
        Assert.AreEqual(2, reopened.Project!.Models.Count);
        Assert.AreEqual(@"materials\models\Creator\shared", reopened.Project.MaterialSources[1].DestinationOverride!.RelativePath);
        Assert.AreEqual(@"E:\ReleaseDocs\README-source.txt", reopened.Project.Readme.ImportedReadmePath);
        Assert.AreEqual(2, reopened.Project.ReleaseHistory.Count);

        File.WriteAllText(Path.Combine(workspace.Root, "bad.sfmpack"), "{ nope", Encoding.UTF8);
        Assert.AreEqual(ProjectLoadStatus.InvalidJson, serializer.Load(Path.Combine(workspace.Root, "bad.sfmpack")).Status);

        var future = serializer.Serialize(project).Replace($"\"schemaVersion\": {ProjectSchemaVersion.Current}", "\"schemaVersion\": 999", StringComparison.Ordinal);
        File.WriteAllText(Path.Combine(workspace.Root, "future.sfmpack"), future, Encoding.UTF8);
        Assert.AreEqual(ProjectLoadStatus.UnsupportedFutureSchema, serializer.Load(Path.Combine(workspace.Root, "future.sfmpack")).Status);
        Assert.IsFalse(serializer.Serialize(project).Contains("sevenZipPath", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void FailedProjectSavePreservesPreviousValidProject()
    {
        using var workspace = TempWorkspace.Create();
        var path = Path.Combine(workspace.Root, "project.sfmpack");
        var serializer = new ProjectSerializer();
        var project = TestProjectFactory.CreateRepresentativeProject();
        serializer.Save(path, project);
        var before = File.ReadAllText(path);
        File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.ReadOnly);

        try
        {
            Assert.ThrowsExactly<UnauthorizedAccessException>(() => serializer.Save(path, project));
            Assert.AreEqual(before, File.ReadAllText(path));
        }
        finally
        {
            File.SetAttributes(path, FileAttributes.Normal);
        }
    }

    [TestMethod]
    public void SettingsPersistSeparatelyAndHandleDefaultsMalformedAndClearRecents()
    {
        using var workspace = TempWorkspace.Create();
        var settingsPath = Path.Combine(workspace.Root, "settings.json");
        var service = new SettingsService(settingsPath);
        Assert.AreEqual(SettingsLoadStatus.FirstRunDefaults, service.Load().Status);

        var settings = new AppSettings
        {
            DefaultOutputDirectory = @"D:\Output",
            DefaultSfmContentFolder = @"D:\Steam\steamapps\common\SourceFilmmaker\game\usermod",
            DefaultProjectDirectory = workspace.Root,
            DefaultArchivePattern = "{AssetName}-{Version}.zip",
            LastProjectDirectory = workspace.Root,
            DefaultReadmeTemplate = "Template",
            CreatorDefaults = new CreatorDefaults { Author = "Creator", Website = "https://example.test", License = "Custom" },
            RecentProjects = new List<RecentProjectEntry>
            {
                new() { ProjectPath = @"D:\P\A.sfmpack", DisplayName = "A", LastAccessed = Now }
            }
        };
        service.Save(settings);
        var reopened = service.Load();

        Assert.AreEqual(SettingsLoadStatus.Success, reopened.Status);
        Assert.AreEqual(@"D:\Output", reopened.Settings.DefaultOutputDirectory);
        Assert.AreEqual(@"D:\Steam\steamapps\common\SourceFilmmaker\game\usermod", reopened.Settings.DefaultSfmContentFolder);
        Assert.AreEqual(workspace.Root, reopened.Settings.DefaultProjectDirectory);
        Assert.AreEqual("{AssetName}-{Version}.zip", reopened.Settings.DefaultArchivePattern);
        Assert.AreEqual(workspace.Root, reopened.Settings.LastProjectDirectory);
        Assert.AreEqual(string.Empty, reopened.Settings.DefaultReadmeTemplate);
        Assert.AreEqual("Creator", reopened.Settings.CreatorDefaults.Author);
        Assert.IsFalse(File.ReadAllText(settingsPath).Contains("defaultReadmeTemplate", StringComparison.OrdinalIgnoreCase));

        File.WriteAllText(settingsPath, "{ bad", Encoding.UTF8);
        Assert.AreEqual(SettingsLoadStatus.MalformedSettings, service.Load().Status);

        File.WriteAllText(settingsPath, "{\"schemaVersion\":999,\"settings\":{\"futureOnly\":\"preserve me\"}}", Encoding.UTF8);
        var futureSettingsBytes = File.ReadAllBytes(settingsPath);
        var futureLoad = service.Load();
        Assert.AreEqual(SettingsLoadStatus.UnsupportedFutureSchema, futureLoad.Status);
        CollectionAssert.AreEqual(futureSettingsBytes, File.ReadAllBytes(settingsPath));
        StringAssert.Contains(new SettingsService().SettingsPath, Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

        settings.RecentProjects.Add(new RecentProjectEntry { ProjectPath = @"D:\P\B.sfmpack", DisplayName = "B", LastAccessed = Now });
        service.DisableRememberRecentProjects(settings);
        var disabled = service.Load().Settings;
        Assert.IsFalse(disabled.RememberRecentProjects);
        Assert.AreEqual(2, disabled.RecentProjects.Count);
    }

    [TestMethod]
    public void SettingsNormalizeSfmInstallOrGameRootToUsermodContentFolder()
    {
        using var workspace = TempWorkspace.Create();
        var sourceFilmmakerRoot = Path.Combine(workspace.Root, "SteamLibrary", "steamapps", "common", "SourceFilmmaker");
        var gameRoot = Path.Combine(sourceFilmmakerRoot, "game");
        var usermodRoot = Path.Combine(gameRoot, "usermod");
        Directory.CreateDirectory(Path.Combine(usermodRoot, "models"));

        AssertNormalizedSfmFolder(workspace, sourceFilmmakerRoot, usermodRoot);
        AssertNormalizedSfmFolder(workspace, gameRoot, usermodRoot);
        AssertNormalizedSfmFolder(workspace, usermodRoot, usermodRoot);
    }

    [TestMethod]
    public void SettingsSaveNormalizesSfmFolderInMemoryAndOnDisk()
    {
        using var workspace = TempWorkspace.Create();
        var sourceFilmmakerRoot = Path.Combine(workspace.Root, "SourceFilmmaker");
        var usermodRoot = Path.Combine(sourceFilmmakerRoot, "game", "usermod");
        Directory.CreateDirectory(usermodRoot);
        var settingsPath = Path.Combine(workspace.Root, "settings.json");
        var service = new SettingsService(settingsPath);
        var settings = new AppSettings { DefaultSfmContentFolder = sourceFilmmakerRoot };

        service.Save(settings);

        Assert.AreEqual(usermodRoot, settings.DefaultSfmContentFolder);
        StringAssert.Contains(File.ReadAllText(settingsPath), JsonEncoded(usermodRoot));
        Assert.AreEqual(usermodRoot, service.Load().Settings.DefaultSfmContentFolder);
    }

    [TestMethod]
    public void ProjectDirectoryPreferenceUpdatesOnlyAfterSuccessfulOperationsAndFallsBackWhenMissing()
    {
        using var workspace = TempWorkspace.Create();
        var service = new ProjectDirectoryPreferenceService();
        var settings = new AppSettings();
        var projectPath = Path.Combine(workspace.Root, "Project.sfmpack");

        Assert.IsTrue(Directory.Exists(service.ResolveOpenStartDirectory(settings)));
        Assert.IsFalse(service.RecordSuccessfulProjectPath(settings, string.Empty));
        Assert.AreEqual(string.Empty, settings.LastProjectDirectory);

        Assert.IsTrue(service.RecordSuccessfulProjectPath(settings, projectPath));
        Assert.AreEqual(workspace.Root, settings.LastProjectDirectory);
        Assert.AreEqual(workspace.Root, service.ResolveOpenStartDirectory(settings));

        settings.LastProjectDirectory = Path.Combine(workspace.Root, "missing");
        Assert.IsTrue(Directory.Exists(service.ResolveOpenStartDirectory(settings)));
    }

    [TestMethod]
    public void ProjectDirectoryPreferenceUsesDistinctOpenAndSavePriorities()
    {
        using var workspace = TempWorkspace.Create();
        var defaultProjectDirectory = Path.Combine(workspace.Root, "Default Projects");
        var lastProjectDirectory = Path.Combine(workspace.Root, "Last Projects");
        var currentProjectDirectory = Path.Combine(workspace.Root, "Current Project");
        Directory.CreateDirectory(defaultProjectDirectory);
        Directory.CreateDirectory(lastProjectDirectory);
        Directory.CreateDirectory(currentProjectDirectory);
        var service = new ProjectDirectoryPreferenceService();
        var settings = new AppSettings
        {
            DefaultProjectDirectory = defaultProjectDirectory,
            LastProjectDirectory = lastProjectDirectory
        };

        Assert.AreEqual(lastProjectDirectory, service.ResolveOpenStartDirectory(settings));
        Assert.AreEqual(defaultProjectDirectory, service.ResolveNewProjectSaveStartDirectory(settings));
        Assert.AreEqual(currentProjectDirectory, service.ResolveSaveAsStartDirectory(settings, Path.Combine(currentProjectDirectory, "Mia.sfmpack")));

        settings.DefaultProjectDirectory = Path.Combine(workspace.Root, "Missing Default");
        Assert.AreEqual(lastProjectDirectory, service.ResolveNewProjectSaveStartDirectory(settings));

        settings.LastProjectDirectory = Path.Combine(workspace.Root, "Missing Last");
        Assert.IsTrue(Directory.Exists(service.ResolveNewProjectSaveStartDirectory(settings)));
    }

    private static void AssertNormalizedSfmFolder(TempWorkspace workspace, string storedValue, string expectedUsermodRoot)
    {
        var settingsPath = Path.Combine(workspace.Root, Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(
            settingsPath,
            $$"""
            {
              "schemaVersion": 1,
              "settings": {
                "defaultSfmContentFolder": {{JsonEncoded(storedValue)}}
              }
            }
            """,
            Encoding.UTF8);

        var loaded = new SettingsService(settingsPath).Load();

        Assert.AreEqual(SettingsLoadStatus.Success, loaded.Status);
        Assert.AreEqual(expectedUsermodRoot, loaded.Settings.DefaultSfmContentFolder);
    }

    private static string JsonEncoded(string value) =>
        System.Text.Json.JsonSerializer.Serialize(value);

    [TestMethod]
    public void LegacySettingsContainingSevenZipPathAndReadmeTemplateLoadSafelyAndSaveWithoutThem()
    {
        using var workspace = TempWorkspace.Create();
        var settingsPath = Path.Combine(workspace.Root, "settings.json");
        File.WriteAllText(
            settingsPath,
            """
            {
              "schemaVersion": 1,
              "settings": {
                "sevenZipPath": "C:\\Tools\\7z.exe",
                "defaultOutputDirectory": "D:\\Output",
                "defaultArchivePattern": "{AssetName}-{Version}.zip",
                "defaultReadmeTemplate": "Template",
                "rememberRecentProjects": false,
                "recentProjects": [
                  {
                    "projectPath": "D:\\P\\A.sfmpack",
                    "displayName": "A",
                    "lastAccessed": "2026-08-16T14:00:00+00:00"
                  }
                ],
                "creatorDefaults": {
                  "author": "Creator",
                  "website": "https://example.test",
                  "license": "Custom"
                }
              }
            }
            """,
            Encoding.UTF8);
        var service = new SettingsService(settingsPath);

        var loaded = service.Load();

        Assert.AreEqual(SettingsLoadStatus.Success, loaded.Status);
        Assert.AreEqual(@"D:\Output", loaded.Settings.DefaultOutputDirectory);
        Assert.AreEqual(string.Empty, loaded.Settings.DefaultProjectDirectory);
        Assert.AreEqual("{AssetName}-{Version}.zip", loaded.Settings.DefaultArchivePattern);
        Assert.AreEqual(string.Empty, loaded.Settings.DefaultReadmeTemplate);
        Assert.AreEqual("Creator", loaded.Settings.CreatorDefaults.Author);
        Assert.IsFalse(loaded.Settings.RememberRecentProjects);
        Assert.AreEqual(1, loaded.Settings.RecentProjects.Count);

        service.Save(loaded.Settings);

        var saved = File.ReadAllText(settingsPath);
        Assert.IsFalse(saved.Contains("sevenZipPath", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(saved.Contains("defaultReadmeTemplate", StringComparison.OrdinalIgnoreCase));
        StringAssert.Contains(saved, "defaultOutputDirectory");
        StringAssert.Contains(saved, "defaultArchivePattern");
    }

    [TestMethod]
    public void RecentProjectsPromoteDedupeBoundRetainMissingAndIgnoreLegacyVisibilityFlag()
    {
        using var workspace = TempWorkspace.Create();
        var settings = new AppSettings();
        var service = new RecentProjectsService(new SettingsService(Path.Combine(workspace.Root, "settings.json")), maxEntries: 2, clock: () => Now);
        var first = Path.Combine(workspace.Root, "One.sfmpack");
        var second = Path.Combine(workspace.Root, "Two.sfmpack");
        var third = Path.Combine(workspace.Root, "Three.sfmpack");
        File.WriteAllText(first, "one");

        service.AddOrPromote(settings, first, "One");
        service.AddOrPromote(settings, second.ToUpperInvariant(), "Two");
        service.AddOrPromote(settings, third, "Three");
        service.AddOrPromote(settings, first.ToUpperInvariant(), "One again");

        Assert.AreEqual(2, settings.RecentProjects.Count);
        Assert.IsTrue(settings.RecentProjects[0].ProjectPath.EndsWith("ONE.SFMPACK", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual(1, settings.RecentProjects.Count(entry => entry.ProjectPath.EndsWith("One.sfmpack", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(service.IsMissing(settings.RecentProjects[0]));
        Assert.IsTrue(settings.RecentProjects.Any(service.IsMissing));

        settings.RememberRecentProjects = false;
        service.AddOrPromote(settings, Path.Combine(workspace.Root, "Hidden.sfmpack"), "Hidden");
        Assert.AreEqual(2, settings.RecentProjects.Count);
        Assert.AreEqual("Hidden", settings.RecentProjects[0].DisplayName);
        Assert.AreEqual(2, service.GetRecentProjects(settings).Count);
    }

    [TestMethod]
    public void MigrationServiceDistinguishesCurrentFutureAndLegacySchemas()
    {
        var migration = new ProjectMigrationService();

        Assert.IsTrue(migration.CanMigrateToCurrent(ProjectSchemaVersion.Current));
        Assert.AreEqual(ProjectLoadStatus.UnsupportedFutureSchema, migration.EvaluateSchemaVersion(ProjectSchemaVersion.Current + 1));
        Assert.AreEqual(ProjectLoadStatus.Success, migration.EvaluateSchemaVersion(1));
        Assert.IsTrue(migration.CanMigrateToCurrent(1));
    }

    [TestMethod]
    public void RecoveryFindsEverySourceTypeAndInaccessibleSource()
    {
        var fileSystem = new FakeFileSystem();
        var project = RecoveryProject();
        fileSystem.AddFile(project.Models[0].SourceMdlPath, Array.Empty<byte>());
        fileSystem.AddFile(project.MaterialSources[0].SourcePath, Array.Empty<byte>());
        fileSystem.AddDirectory(project.MaterialSources[1].SourcePath);
        fileSystem.FailEnumeration(project.MaterialSources[1].SourcePath);

        var issues = new MissingSourceRecoveryService(fileSystem).Inspect(project).Issues;

        AssertIssue(issues, RecoveryReferenceKind.AdditionalModel, RecoveryIssueStatus.Missing);
        AssertIssue(issues, RecoveryReferenceKind.ModelCompanion, RecoveryIssueStatus.Missing);
        AssertIssue(issues, RecoveryReferenceKind.MaterialFolder, RecoveryIssueStatus.Inaccessible);
        AssertIssue(issues, RecoveryReferenceKind.ExtraFile, RecoveryIssueStatus.Missing);
        AssertIssue(issues, RecoveryReferenceKind.ExtraFolder, RecoveryIssueStatus.Missing);
        AssertIssue(issues, RecoveryReferenceKind.ImportedReadme, RecoveryIssueStatus.Missing);
    }

    [TestMethod]
    public void RecoveryLocateAndRemoveTargetExactEntriesAndPreserveFolderRecipe()
    {
        var fileSystem = new FakeFileSystem();
        var project = RecoveryProject();
        var service = new MissingSourceRecoveryService(fileSystem);
        fileSystem.AddFile(@"D:\newroot\orphan.mdl", Array.Empty<byte>());
        fileSystem.AddDirectory(@"D:\newroot\materialsFolder");

        var additional = service.Inspect(project).Issues.Single(issue => issue.ReferenceKind == RecoveryReferenceKind.AdditionalModel);
        var located = service.LocateReplacement(project, additional.Id, @"D:\newroot\orphan.mdl");
        Assert.AreEqual(RecoveryOperationStatus.Success, located.Status);
        Assert.IsTrue(located.DestinationDecisionMayBeNeeded);
        Assert.AreEqual(@"D:\newroot\orphan.mdl", project.Models.Single(model => model.Role == ModelRole.Additional).SourceMdlPath);

        var materialFolder = service.Inspect(project).Issues.Single(issue => issue.ReferenceKind == RecoveryReferenceKind.MaterialFolder);
        var folderLocated = service.LocateReplacement(project, materialFolder.Id, @"D:\newroot\materialsFolder");
        Assert.AreEqual(RecoveryOperationStatus.Success, folderLocated.Status);
        Assert.IsTrue(project.MaterialSources.Single(source => source.Id == materialFolder.ProjectEntryId).IsFolder);

        var extraFile = service.Inspect(project).Issues.Single(issue => issue.ReferenceKind == RecoveryReferenceKind.ExtraFile);
        Assert.AreEqual(RecoveryOperationStatus.Success, service.RemoveFromPackage(project, extraFile.Id).Status);
        Assert.IsFalse(project.Extras.Any(source => source.Id == extraFile.ProjectEntryId));

        var primary = service.Inspect(project).Issues.Single(issue => issue.ReferenceKind == RecoveryReferenceKind.PrimaryModel);
        Assert.AreEqual(RecoveryOperationStatus.CannotRemoveRequiredPrimaryModel, service.RemoveFromPackage(project, primary.Id).Status);
        Assert.AreEqual(2, project.Models.Count);
        Assert.AreEqual(0, project.ReleaseHistory.Count);
    }

    [TestMethod]
    public void CompanionIntentSurvivesReopenAndRelocatedModelUsesNormalDetection()
    {
        using var workspace = TempWorkspace.Create();
        var model = workspace.WriteFile(@"source\game\usermod\models\Creator\new\new.mdl", "mdl");
        workspace.WriteFile(@"source\game\usermod\models\Creator\new\new.vvd", "vvd");
        var project = new PackageProject
        {
            Models = new List<ModelEntry>
            {
                new()
                {
                    Role = ModelRole.Primary,
                    SourceMdlPath = @"D:\missing\old.mdl",
                    SourceStem = "old",
                    ReleaseStem = "release",
                    Companions = new List<ModelCompanionSelection>
                    {
                        new() { RuntimeSuffix = ".phy", SourcePath = @"D:\missing\old.phy", UserSelection = CompanionUserSelection.Exclude }
                    }
                }
            }
        };
        var serializer = new ProjectSerializer();
        var reopened = serializer.Deserialize(serializer.Serialize(project)).Project!;
        Assert.AreEqual(CompanionUserSelection.Exclude, reopened.Models[0].Companions[0].UserSelection);

        var fileSystem = new PhysicalFileSystem();
        var service = new MissingSourceRecoveryService(fileSystem);
        var issue = service.Inspect(reopened).Issues.Single(issue => issue.ReferenceKind == RecoveryReferenceKind.PrimaryModel);
        service.LocateReplacement(reopened, issue.Id, model);
        var expansion = new SourceExpansionService(fileSystem).Resolve(reopened);

        Assert.IsTrue(expansion.ModelFamilies[0].Files.Any(file => file.SourcePath.EndsWith("new.vvd", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(serializer.Serialize(reopened).Contains("missingObservation", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void BuildHistoryCanBeSavedReopenedAndSameVersionRebuildAppendsBuildRecords()
    {
        using var workspace = TempWorkspace.Create();
        workspace.CreateBuildSources();
        var project = workspace.BuildProject();
        project.ReleaseHistory.Add(new ReleaseRecord { Version = "1.0", Changes = new List<string> { "Initial release." } });
        var coordinator = workspace.BuildCoordinator();
        var first = coordinator.Build(workspace.BuildRequest(project, VersionReuseDecision.RebuildExistingVersion));
        Assert.IsTrue(first.Succeeded, first.LogText());
        var rebuild = coordinator.Build(workspace.BuildRequest(project, VersionReuseDecision.RebuildExistingVersion, ExistingOutputDecision.Replace));
        Assert.IsTrue(rebuild.Succeeded, rebuild.LogText());

        var serializer = new ProjectSerializer();
        var path = Path.Combine(workspace.Root, "built.sfmpack");
        serializer.Save(path, project);
        var reopened = serializer.Load(path).Project!;

        Assert.AreEqual(1, reopened.ReleaseHistory.Count);
        Assert.AreEqual("1.0", reopened.ReleaseHistory[0].Version);
        Assert.AreEqual(2, reopened.BuildHistory.Count);
        CollectionAssert.AreEqual(new[] { "1.0", "1.0" }, reopened.BuildHistory.Select(record => record.Version).ToArray());
        project.CurrentVersion = "1.1";
        project.ArchiveName = "chair-1.1.zip";
        coordinator.Build(workspace.BuildRequest(project));
        serializer.Save(path, project);
        reopened = serializer.Load(path).Project!;
        CollectionAssert.AreEqual(new[] { "1.0" }, reopened.ReleaseHistory.Select(record => record.Version).ToArray());
        CollectionAssert.AreEqual(new[] { "1.1", "1.0", "1.0" }, reopened.BuildHistory.Select(record => record.Version).ToArray());
    }

    [TestMethod]
    public void SaveReopenBuildSaveReopenWorkflowUsesExistingPipelineAndSourceSafe()
    {
        using var workspace = TempWorkspace.Create();
        workspace.CreateBuildSources();
        var projectPath = Path.Combine(workspace.Root, "workflow.sfmpack");
        var project = workspace.BuildProject();
        var sourceSnapshot = SourceSnapshot.Capture(workspace.SourceRoot);
        var serializer = new ProjectSerializer();

        serializer.Save(projectPath, project);
        var reopened = serializer.Load(projectPath).Project!;
        var summary = workspace.BuildCoordinator().Build(workspace.BuildRequest(reopened));
        serializer.Save(projectPath, reopened);
        var final = serializer.Load(projectPath).Project!;

        Assert.IsTrue(summary.Succeeded, summary.LogText());
        Assert.AreEqual(0, final.ReleaseHistory.Count);
        Assert.AreEqual(1, final.BuildHistory.Count);
        Assert.IsFalse(new MissingSourceRecoveryService(new PhysicalFileSystem()).Inspect(final).HasIssues);
        sourceSnapshot.AssertUnchanged(workspace.SourceRoot);
    }

    [TestMethod]
    public void SourceReferencesRoundTripAndResolveRelativeWhenProjectAndAssetsMoveTogether()
    {
        using var workspace = TempWorkspace.Create();
        var oldProjectPath = Path.Combine(workspace.Root, "old", "package.sfmpack");
        var oldModel = workspace.WriteFile(@"old\assets\models\chair.mdl", "mdl");
        var project = new PackageProject
        {
            Models = new List<ModelEntry>
            {
                new() { Role = ModelRole.Primary, SourceMdlPath = oldModel, SourceStem = "chair", ReleaseStem = "chair" }
            }
        };
        var serializer = new ProjectSerializer();
        serializer.Save(oldProjectPath, project);
        Assert.AreEqual(@"assets\models\chair.mdl", project.Models[0].SourceReference!.RelativePath);

        var newProjectPath = Path.Combine(workspace.Root, "new", "package.sfmpack");
        Directory.CreateDirectory(Path.GetDirectoryName(newProjectPath)!);
        File.Copy(oldProjectPath, newProjectPath);
        var newModel = workspace.WriteFile(@"new\assets\models\chair.mdl", "mdl");
        File.Delete(oldModel);

        var reopened = serializer.Load(newProjectPath).Project!;

        Assert.AreEqual(newModel, reopened.Models[0].SourceMdlPath);
        Assert.AreEqual(oldModel, reopened.Models[0].SourceReference!.AbsolutePath);
    }

    [TestMethod]
    public void SourceReferenceFallsBackToAbsoluteWhenRelativeTargetUnavailable()
    {
        using var workspace = TempWorkspace.Create();
        var projectPath = Path.Combine(workspace.Root, "project", "package.sfmpack");
        var absoluteModel = workspace.WriteFile(@"shared\chair.mdl", "mdl");
        var project = new PackageProject
        {
            Models = new List<ModelEntry>
            {
                new() { Role = ModelRole.Primary, SourceMdlPath = absoluteModel, SourceStem = "chair", ReleaseStem = "chair" }
            }
        };
        var serializer = new ProjectSerializer();
        serializer.Save(projectPath, project);
        var movedProjectPath = Path.Combine(workspace.Root, "nested", "elsewhere", "package.sfmpack");
        Directory.CreateDirectory(Path.GetDirectoryName(movedProjectPath)!);
        File.Copy(projectPath, movedProjectPath);

        var reopened = serializer.Load(movedProjectPath).Project!;

        Assert.AreEqual(absoluteModel, reopened.Models[0].SourceMdlPath);
    }

    [TestMethod]
    public void MissingSourcesRetainStructuredReferencesAndGroupByFormerRootWithoutChangingFiles()
    {
        using var workspace = TempWorkspace.Create();
        var projectPath = Path.Combine(workspace.Root, "old", "package.sfmpack");
        var model = workspace.WriteFile(@"old\sources\models\chair.mdl", "mdl");
        var material = workspace.WriteFile(@"old\sources\materials\chair.vmt", "vmt");
        var project = new PackageProject
        {
            Models = new List<ModelEntry>
            {
                new() { Role = ModelRole.Primary, SourceMdlPath = model, SourceStem = "chair", ReleaseStem = "chair" }
            },
            MaterialSources = new List<SourceEntry>
            {
                new() { Kind = SourceEntryKind.Material, SourcePath = material, IsFolder = false, DestinationOverride = new DestinationOverride { Kind = DestinationOverrideKind.Custom, RelativePath = "materials" } }
            }
        };
        var serializer = new ProjectSerializer();
        serializer.Save(projectPath, project);
        var missingProjectPath = Path.Combine(workspace.Root, "missing", "package.sfmpack");
        Directory.CreateDirectory(Path.GetDirectoryName(missingProjectPath)!);
        File.Copy(projectPath, missingProjectPath);
        File.Delete(model);
        File.Delete(material);
        var snapshot = SourceSnapshot.Capture(workspace.Root);

        var reopened = serializer.Load(missingProjectPath).Project!;
        var recovery = new MissingSourceRecoveryService(new PhysicalFileSystem()).Inspect(reopened);

        Assert.AreEqual(2, recovery.Issues.Count);
        Assert.IsTrue(recovery.Issues.All(issue => !string.IsNullOrWhiteSpace(issue.FormerRoot)));
        Assert.IsTrue(recovery.Groups.Any(group => group.FormerRoot.EndsWith(@"\old\sources", StringComparison.OrdinalIgnoreCase) && group.Issues.Count == 2));
        snapshot.AssertUnchanged(workspace.Root);
    }

    [TestMethod]
    public void RecoveryGroupsOnlyUnresolvedSourcesByPersistedFormerRootDeterministically()
    {
        var fileSystem = new FakeFileSystem();
        var project = new PackageProject
        {
            Models = new List<ModelEntry>
            {
                new()
                {
                    Role = ModelRole.Primary,
                    SourceMdlPath = @"D:\old\asset\models\chair.mdl",
                    SourceReference = Reference(@"D:\old\asset\models\chair.mdl", recoveryRoot: @"D:\old\asset", recoveryRelative: @"models\chair.mdl")
                }
            },
            MaterialSources = new List<SourceEntry>
            {
                new()
                {
                    Kind = SourceEntryKind.Material,
                    SourcePath = @"D:\old\asset\materials\chair",
                    IsFolder = true,
                    SourceReference = Reference(@"D:\old\asset\materials\chair", recoveryRoot: @"D:\old\asset", recoveryRelative: @"materials\chair")
                },
                new()
                {
                    Kind = SourceEntryKind.Material,
                    SourcePath = @"E:\available\materials\ok.vmt",
                    IsFolder = false,
                    SourceReference = Reference(@"E:\available\materials\ok.vmt", recoveryRoot: @"E:\available", recoveryRelative: @"materials\ok.vmt")
                }
            },
            Extras = new List<SourceEntry>
            {
                new()
                {
                    Kind = SourceEntryKind.Extra,
                    SourcePath = @"C:\rigs\pose.dmx",
                    IsFolder = false,
                    SourceReference = Reference(@"C:\rigs\pose.dmx", recoveryRoot: @"C:\rigs", recoveryRelative: "pose.dmx")
                }
            }
        };
        fileSystem.AddFile(@"E:\available\materials\ok.vmt", Array.Empty<byte>());

        var result = new MissingSourceRecoveryService(fileSystem).Inspect(project);

        CollectionAssert.AreEqual(new[] { @"C:\rigs", @"D:\old\asset" }, result.Groups.Select(group => group.FormerRoot).ToArray());
        Assert.AreEqual(2, result.Groups.Single(group => group.FormerRoot == @"D:\old\asset").Issues.Count);
        Assert.IsFalse(result.Issues.Any(issue => issue.SourcePath == @"E:\available\materials\ok.vmt"));
    }

    [TestMethod]
    public void RecoveryFolderRemapPreviewsPartialMismatchAndCommitsOnlyAcceptedMatches()
    {
        var fileSystem = new FakeFileSystem();
        var projectPath = @"E:\packages\chair.sfmpack";
        var project = new PackageProject
        {
            Models = new List<ModelEntry>
            {
                new()
                {
                    Role = ModelRole.Primary,
                    SourceMdlPath = @"D:\old\asset\models\chair.mdl",
                    SourceStem = "chair",
                    ReleaseStem = "chair",
                    SourceReference = Reference(@"D:\old\asset\models\chair.mdl", recoveryRoot: @"D:\old\asset", recoveryRelative: @"models\chair.mdl")
                }
            },
            MaterialSources = new List<SourceEntry>
            {
                new()
                {
                    Kind = SourceEntryKind.Material,
                    SourcePath = @"D:\old\asset\materials\chair",
                    IsFolder = true,
                    DestinationOverride = new DestinationOverride { Kind = DestinationOverrideKind.Custom, RelativePath = @"materials\chair" },
                    SourceReference = Reference(@"D:\old\asset\materials\chair", recoveryRoot: @"D:\old\asset", recoveryRelative: @"materials\chair")
                }
            },
            Extras = new List<SourceEntry>
            {
                new()
                {
                    Kind = SourceEntryKind.Extra,
                    SourcePath = @"D:\old\asset\extras\rigs",
                    IsFolder = true,
                    SourceReference = Reference(@"D:\old\asset\extras\rigs", recoveryRoot: @"D:\old\asset", recoveryRelative: @"extras\rigs")
                }
            },
            Readme = new ReadmeConfig
            {
                Mode = ReadmeMode.Custom,
                CustomSource = ReadmeCustomSource.ImportedFile,
                ImportedReadmePath = @"D:\old\asset\README.txt",
                ImportedReadmeReference = Reference(@"D:\old\asset\README.txt", recoveryRoot: @"D:\old\asset", recoveryRelative: "README.txt")
            }
        };
        fileSystem.AddFile(@"E:\new\asset\models\chair.mdl", Array.Empty<byte>());
        fileSystem.AddDirectory(@"E:\new\asset\materials\chair");
        fileSystem.AddFile(@"E:\new\asset\extras\rigs", Array.Empty<byte>());
        fileSystem.AddFile(@"E:\new\asset\README.txt", Array.Empty<byte>());
        var service = new MissingSourceRecoveryService(fileSystem);

        var wrong = service.PreviewFolderRemap(project, @"D:\old\asset", @"E:\wrong\asset");
        Assert.IsFalse(wrong.HasMatches);
        Assert.IsTrue(wrong.Candidates.All(candidate => candidate.Status == RecoveryRemapCandidateStatus.Missing));
        Assert.AreEqual(@"D:\old\asset\models\chair.mdl", project.Models[0].SourceMdlPath);

        var preview = service.PreviewFolderRemap(project, @"D:\old\asset", @"E:\new\asset");
        Assert.IsFalse(preview.IsFullMatch);
        Assert.AreEqual(3, preview.MatchedCandidates.Count);
        Assert.AreEqual(1, preview.UnresolvedCandidates.Count);
        Assert.AreEqual(RecoveryRemapCandidateStatus.TypeMismatch, preview.UnresolvedCandidates[0].Status);

        var apply = service.ApplyFolderRemap(project, projectPath, preview);

        Assert.AreEqual(3, apply.AppliedCount);
        Assert.AreEqual(@"E:\new\asset\models\chair.mdl", project.Models[0].SourceMdlPath);
        var modelReference = project.Models[0].SourceReference!;
        Assert.AreEqual(@"E:\new\asset", modelReference.RecoveryRootPath);
        Assert.AreEqual(@"models\chair.mdl", modelReference.RecoveryRelativePath);
        Assert.AreEqual(@"E:\new\asset\materials\chair", project.MaterialSources[0].SourcePath);
        Assert.AreEqual(@"materials\chair", project.MaterialSources[0].DestinationOverride!.RelativePath);
        Assert.AreEqual(@"D:\old\asset\extras\rigs", project.Extras[0].SourcePath);
        Assert.AreEqual(@"E:\new\asset\README.txt", project.Readme.ImportedReadmePath);
        Assert.AreEqual(0, fileSystem.CopyFileCallCount);
    }

    [TestMethod]
    public void RecoverySaveReopenResolvesAndValidationClearsThroughFreshPlan()
    {
        using var workspace = TempWorkspace.Create();
        var projectPath = Path.Combine(workspace.Root, "package.sfmpack");
        var oldModel = workspace.WriteFile(@"old\game\usermod\models\Creator\chair\chair.mdl", "old mdl");
        var oldMaterial = workspace.WriteFile(@"old\game\usermod\materials\models\Creator\chair\chair.vmt", "old vmt");
        var project = new PackageProject
        {
            AssetName = "Chair",
            CurrentVersion = "1.0",
            ArchiveName = "chair.zip",
            Models = new List<ModelEntry>
            {
                new() { Role = ModelRole.Primary, SourceMdlPath = oldModel, SourceStem = "chair", ReleaseStem = "chair" }
            },
            MaterialSources = new List<SourceEntry>
            {
                new() { Kind = SourceEntryKind.Material, SourcePath = Path.GetDirectoryName(oldMaterial)!, IsFolder = true, IncludeRecursively = true }
            },
            Readme = new ReadmeConfig { Mode = ReadmeMode.None }
        };
        var serializer = new ProjectSerializer();
        serializer.Save(projectPath, project);
        File.Delete(oldModel);
        Directory.Delete(Path.GetDirectoryName(oldMaterial)!, recursive: true);
        var newModel = workspace.WriteFile(@"new\game\usermod\models\Creator\chair\chair.mdl", "new mdl");
        workspace.WriteFile(@"new\game\usermod\materials\models\Creator\chair\chair.vmt", "new vmt");
        var sourceSnapshot = SourceSnapshot.Capture(Path.Combine(workspace.Root, "new"));

        var reopened = serializer.Load(projectPath).Project!;
        var physicalFileSystem = new PhysicalFileSystem();
        var recovery = new MissingSourceRecoveryService(physicalFileSystem);
        var beforePlan = new PackagePlanner(physicalFileSystem).CreatePlan(reopened);
        var beforeValidation = new PackageValidator(physicalFileSystem, new FakeOutputEnvironment()).Validate(reopened, beforePlan, new PackageValidationContext { OutputDirectory = workspace.OutputRoot });

        var preview = recovery.PreviewFolderRemap(reopened, Path.Combine(workspace.Root, "old", "game", "usermod"), Path.Combine(workspace.Root, "new", "game", "usermod"));
        recovery.ApplyFolderRemap(reopened, projectPath, preview);
        serializer.Save(projectPath, reopened);
        var final = serializer.Load(projectPath).Project!;
        var afterPlan = new PackagePlanner(physicalFileSystem).CreatePlan(final);
        var afterValidation = new PackageValidator(physicalFileSystem, new FakeOutputEnvironment()).Validate(final, afterPlan, new PackageValidationContext { OutputDirectory = workspace.OutputRoot });

        Assert.IsTrue(beforeValidation.Messages.Any(message => message.Code is "source-missing" or "planned-source-missing"));
        Assert.IsFalse(recovery.Inspect(final).HasIssues);
        Assert.AreEqual(newModel, final.Models[0].SourceMdlPath);
        Assert.IsFalse(afterValidation.Messages.Any(message => message.Code is "source-missing" or "planned-source-missing"));
        Assert.IsTrue(afterPlan.Entries.Any(entry => entry.SourcePath == newModel));
        sourceSnapshot.AssertUnchanged(Path.Combine(workspace.Root, "new"));
    }

    [TestMethod]
    public void RecentProjectsReplaceLocateAvoidsDuplicatesAndRemoveOnlyHistoryEntry()
    {
        using var workspace = TempWorkspace.Create();
        var settingsPath = Path.Combine(workspace.Root, "settings.json");
        var service = new RecentProjectsService(new SettingsService(settingsPath), maxEntries: 5, clock: () => Now);
        var settings = new AppSettings();
        var missingOld = Path.Combine(workspace.Root, "missing", "Old.sfmpack");
        var moved = workspace.WriteFile(@"moved\Old.sfmpack", "{}");
        var duplicateMoved = moved.ToUpperInvariant();
        var other = workspace.WriteFile(@"other\Other.sfmpack", "{}");

        service.AddOrPromote(settings, missingOld, "Old");
        service.AddOrPromote(settings, duplicateMoved, "Duplicate");
        service.AddOrPromote(settings, other, "Other");
        service.ReplacePath(settings, missingOld, moved, "Moved old");

        Assert.AreEqual(moved, settings.RecentProjects[0].ProjectPath);
        Assert.AreEqual("Moved old", settings.RecentProjects[0].DisplayName);
        Assert.AreEqual(1, settings.RecentProjects.Count(entry => string.Equals(Path.GetFullPath(entry.ProjectPath), Path.GetFullPath(moved), StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(settings.RecentProjects.Any(entry => string.Equals(entry.ProjectPath, missingOld, StringComparison.OrdinalIgnoreCase)));

        service.Remove(settings, moved);

        Assert.IsFalse(settings.RecentProjects.Any(entry => string.Equals(Path.GetFullPath(entry.ProjectPath), Path.GetFullPath(moved), StringComparison.OrdinalIgnoreCase)));
        Assert.IsTrue(File.Exists(moved));
    }

    private static void AssertIssue(IReadOnlyList<RecoveryIssue> issues, RecoveryReferenceKind kind, RecoveryIssueStatus status)
    {
        Assert.IsTrue(issues.Any(issue => issue.ReferenceKind == kind && issue.Status == status), $"{kind}:{status}");
    }

    private static PersistedSourceReference Reference(
        string absolute,
        string? relative = null,
        string? recoveryRoot = null,
        string? recoveryRelative = null) =>
        new()
        {
            AbsolutePath = absolute,
            RelativePath = relative,
            RecoveryRootPath = recoveryRoot,
            RecoveryRelativePath = recoveryRelative
        };

    private static PackageProject RecoveryProject()
    {
        return new PackageProject
        {
            Models = new List<ModelEntry>
            {
                new()
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Role = ModelRole.Primary,
                    SourceMdlPath = @"E:\models\primary.mdl",
                    SourceStem = "primary",
                    ReleaseStem = "primary",
                    Companions = new List<ModelCompanionSelection>
                    {
                        new() { SourcePath = @"E:\models\primary.vvd", RuntimeSuffix = ".vvd", UserSelection = CompanionUserSelection.Include }
                    }
                },
                new()
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Role = ModelRole.Additional,
                    SourceMdlPath = @"E:\models\additional.mdl",
                    SourceStem = "additional",
                    ReleaseStem = "additional"
                }
            },
            MaterialSources = new List<SourceEntry>
            {
                new() { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Kind = SourceEntryKind.Material, SourcePath = @"E:\materials\file.vmt", IsFolder = false },
                new() { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Kind = SourceEntryKind.Material, SourcePath = @"E:\materials\folder", IsFolder = true }
            },
            Extras = new List<SourceEntry>
            {
                new() { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), Kind = SourceEntryKind.Extra, SourcePath = @"E:\extras\license.txt", IsFolder = false },
                new() { Id = Guid.Parse("66666666-6666-6666-6666-666666666666"), Kind = SourceEntryKind.Extra, SourcePath = @"E:\extras\folder", IsFolder = true }
            },
            Readme = new ReadmeConfig { Mode = ReadmeMode.Custom, CustomSource = ReadmeCustomSource.ImportedFile, ImportedReadmePath = @"E:\docs\README.txt" }
        };
    }

    private sealed class TempWorkspace : IDisposable
    {
        private TempWorkspace(string root)
        {
            Root = root;
            SourceRoot = Path.Combine(root, "source");
            OutputRoot = Path.Combine(root, "output");
            AppRoot = Path.Combine(root, "app");
            Directory.CreateDirectory(SourceRoot);
            Directory.CreateDirectory(OutputRoot);
            Directory.CreateDirectory(AppRoot);
        }

        public string Root { get; }
        public string SourceRoot { get; }
        public string OutputRoot { get; }
        public string AppRoot { get; }

        public static TempWorkspace Create() => new(Path.Combine(Path.GetTempPath(), "SfmPackageBuilder.Persistence.Tests", Guid.NewGuid().ToString("N")));

        public string WriteFile(string relativePath, string contents)
        {
            var path = Path.Combine(Root, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, contents, Encoding.UTF8);
            return path;
        }

        public void CreateBuildSources()
        {
            WriteFile(@"source\game\usermod\models\Creator\chair\chair.mdl", "mdl");
            WriteFile(@"source\game\usermod\materials\models\Creator\chair\chair.vmt", "vmt");
        }

        public PackageProject BuildProject() => new()
        {
            AssetName = "Chair",
            CurrentVersion = "1.0",
            ArchiveName = "chair.zip",
            Models = new List<ModelEntry>
            {
                new()
                {
                    Role = ModelRole.Primary,
                    SourceMdlPath = Path.Combine(SourceRoot, @"game\usermod\models\Creator\chair\chair.mdl"),
                    SourceStem = "chair",
                    ReleaseStem = "chair_release"
                }
            },
            MaterialSources = new List<SourceEntry>
            {
                new()
                {
                    Kind = SourceEntryKind.Material,
                    SourcePath = Path.Combine(SourceRoot, @"game\usermod\materials\models\Creator\chair"),
                    IsFolder = true,
                    IncludeRecursively = true
                }
            },
            Readme = new ReadmeConfig { Mode = ReadmeMode.Generated }
        };

        public BuildRequest BuildRequest(
            PackageProject project,
            VersionReuseDecision versionDecision = VersionReuseDecision.None,
            ExistingOutputDecision outputDecision = ExistingOutputDecision.None) => new()
        {
            Project = project,
            OutputDirectory = OutputRoot,
            VersionReuseDecision = versionDecision,
            ExistingOutputDecision = outputDecision
        };

        public BuildCoordinator BuildCoordinator() =>
            new(
                AppRoot,
                new PhysicalFileSystem(),
                new PhysicalOutputEnvironment(),
                () => Now);

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                foreach (var file in Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }

                Directory.Delete(Root, recursive: true);
            }
        }
    }

    private sealed class SourceSnapshot
    {
        private SourceSnapshot(string[] entries)
        {
            Entries = entries;
        }

        private string[] Entries { get; }

        public static SourceSnapshot Capture(string root) => new(
            Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(root, path) + ":" + Convert.ToHexString(File.ReadAllBytes(path)) + ":" + File.GetAttributes(path))
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray());

        public void AssertUnchanged(string root)
        {
            CollectionAssert.AreEqual(Entries, Capture(root).Entries);
        }

        public void AssertUnchangedExceptMissing(string root, params string[] removedAbsolutePaths)
        {
            var removed = removedAbsolutePaths
                .Select(path => Path.GetRelativePath(root, path))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var expected = Entries
                .Where(entry => !removed.Contains(entry.Split(':', 2)[0]))
                .ToArray();
            CollectionAssert.AreEqual(expected, Capture(root).Entries);
        }
    }
}
