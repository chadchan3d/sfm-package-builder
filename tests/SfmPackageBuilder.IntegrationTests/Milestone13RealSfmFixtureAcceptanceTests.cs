using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SfmPackageBuilder.Core.Archive;
using SfmPackageBuilder.Core.Build;
using SfmPackageBuilder.Core.Domain;
using SfmPackageBuilder.Core.Persistence;

namespace SfmPackageBuilder.IntegrationTests;

[TestClass]
public sealed class Milestone13RealSfmFixtureAcceptanceTests
{
    private const string SfmGameRootEnvironmentVariable = "SFM_PACKAGE_BUILDER_ACCEPTANCE_SFM_GAME_ROOT";

    [TestMethod]
    public void InstalledSfmFixturesBuildInstallAndLeaveSourcesImmutable()
    {
        var sfmGameRoot = Environment.GetEnvironmentVariable(SfmGameRootEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(sfmGameRoot))
        {
            Assert.Inconclusive(
                "Set " + SfmGameRootEnvironmentVariable + " to a local SourceFilmmaker\\game fixture tree to run this acceptance test.");
        }

        if (!Directory.Exists(sfmGameRoot))
        {
            Assert.Inconclusive("Installed SFM game fixture tree was not found.");
        }

        RequireFile(sfmGameRoot, @"tf\models\props_gameplay\ball001.mdl");
        RequireFile(sfmGameRoot, @"tf\models\player\hwm\scout.mdl");
        RequireFile(sfmGameRoot, @"hl2\models\alyx_animations.mdl");
        RequireFile(sfmGameRoot, @"usermod\cfg\sfm_defaultanimationgroups.txt");
        RequireDirectory(sfmGameRoot, @"tf\materials\models\props_gameplay");
        RequireDirectory(sfmGameRoot, @"tf\materials\models\player\scout");

        var root = Path.Combine(Path.GetTempPath(), "Sfm Package Builder M13 Real Fixtures", Guid.NewGuid().ToString("N"));
        var appRoot = Path.Combine(root, "App Root");
        var outputRoot = Path.Combine(root, "Output Root 椅子");
        var installRoot = Path.Combine(root, "Controlled SFM Install", "game", "usermod");
        Directory.CreateDirectory(appRoot);
        Directory.CreateDirectory(outputRoot);
        Directory.CreateDirectory(installRoot);

        try
        {
            var watchedSources = new[]
            {
                PathFor(sfmGameRoot, @"tf\models\props_gameplay\ball001.mdl"),
                PathFor(sfmGameRoot, @"tf\models\props_gameplay\ball001.vvd"),
                PathFor(sfmGameRoot, @"tf\models\props_gameplay\ball001.dx90.vtx"),
                PathFor(sfmGameRoot, @"tf\models\props_gameplay\ball001.phy"),
                PathFor(sfmGameRoot, @"tf\models\player\hwm\scout.mdl"),
                PathFor(sfmGameRoot, @"tf\models\player\hwm\scout.vvd"),
                PathFor(sfmGameRoot, @"tf\models\player\hwm\scout.dx90.vtx"),
                PathFor(sfmGameRoot, @"tf\models\player\hwm\scout.phy"),
                PathFor(sfmGameRoot, @"hl2\models\alyx_animations.mdl"),
                PathFor(sfmGameRoot, @"usermod\cfg\sfm_defaultanimationgroups.txt")
            };
            var watchedDirectories = new[]
            {
                DirectoryPath(sfmGameRoot, @"tf\materials\models\props_gameplay"),
                DirectoryPath(sfmGameRoot, @"tf\materials\models\player\scout")
            };
            var before = SourceSnapshot.Capture(watchedSources, watchedDirectories);
            var coordinator = new BuildCoordinator(appRoot);

            var simple = Build(
                coordinator,
                SimplePropProject(sfmGameRoot, "simple prop 椅子.zip"),
                outputRoot);
            Assert.IsTrue(simple.Succeeded, Log(simple));
            AssertArchiveHasDirectRoots(simple.ArchivePath!, "models/", "materials/", "README.txt");
            Extract(simple.ArchivePath!, installRoot);
            AssertInstalled(installRoot, @"models\props_gameplay\ball001.mdl");
            AssertInstalled(installRoot, @"materials\models\props_gameplay\ball001.vmt");

            var renamedCharacter = Build(
                coordinator,
                RenamedCharacterProject(sfmGameRoot),
                outputRoot);
            Assert.IsTrue(renamedCharacter.Succeeded, Log(renamedCharacter));
            AssertArchiveContains(
                renamedCharacter.ArchivePath!,
                @"models/player/hwm/scout_acceptance.mdl",
                @"models/player/hwm/scout_acceptance.vvd",
                @"models/player/hwm/scout_acceptance.dx90.vtx",
                @"models/player/hwm/scout_acceptance.phy");
            Extract(renamedCharacter.ArchivePath!, installRoot);
            AssertInstalled(installRoot, @"models\player\hwm\scout_acceptance.mdl");

            var combinedProject = CombinedProject(root, sfmGameRoot);
            var combined = Build(
                coordinator,
                combinedProject,
                outputRoot);
            Assert.IsTrue(combined.Succeeded, Log(combined));
            AssertArchiveContains(
                combined.ArchivePath!,
                @"models/player/hwm/scout_acceptance.mdl",
                @"models/alyx_animations.mdl",
                @"cfg/sfm_defaultanimationgroups.txt",
                "README.txt",
                @"Docs/acceptance-notes.txt");
            Extract(combined.ArchivePath!, installRoot);
            AssertInstalled(installRoot, @"models\alyx_animations.mdl");
            AssertInstalled(installRoot, @"cfg\sfm_defaultanimationgroups.txt");

            var check = coordinator.CheckPackage(combinedProject, outputRoot);
            Assert.IsTrue(check.Plan.SharedFileNotices.Any(notice => notice.ProvidesControlGroupsReadmeContext));
            Assert.IsFalse(coordinator.PreviewReadme(combinedProject).PreviewText!.Contains("SFM CONTROL GROUPS", StringComparison.Ordinal));
            var projectPath = Path.Combine(root, "combined.sfmpack");
            new ProjectSerializer().Save(projectPath, combinedProject);
            var reopened = new ProjectSerializer().Load(projectPath).Project!;
            Assert.AreEqual(ReadmeMode.Generated, reopened.Readme.Mode);

            var firstOutputBytes = File.ReadAllBytes(combined.ArchivePath!);
            combinedProject.ReleaseHistory.Add(new ReleaseRecord { Version = combinedProject.CurrentVersion, Changes = new List<string> { "Initial acceptance build." } });
            var cancel = Build(coordinator, combinedProject, outputRoot, ExistingOutputDecision.Cancel);
            Assert.AreEqual(BuildStatus.DecisionRequired, cancel.Status, "Version reuse must be decided before output handling.");
            var choose = Build(
                coordinator,
                combinedProject,
                outputRoot,
                ExistingOutputDecision.ChooseAnotherName,
                VersionReuseDecision.RebuildExistingVersion);
            Assert.AreEqual(BuildStatus.ChangeRequired, choose.Status);
            CollectionAssert.AreEqual(firstOutputBytes, File.ReadAllBytes(combined.ArchivePath!));
            var replace = Build(
                coordinator,
                combinedProject,
                outputRoot,
                ExistingOutputDecision.Replace,
                VersionReuseDecision.RebuildExistingVersion);
            Assert.IsTrue(replace.Succeeded, Log(replace));

            before.AssertUnchanged();
            Assert.IsFalse(ReadArchiveEntries(simple.ArchivePath!).Any(IsWrapperEntry));
            Assert.IsFalse(ReadArchiveEntries(renamedCharacter.ArchivePath!).Any(IsWrapperEntry));
            Assert.IsFalse(ReadArchiveEntries(combined.ArchivePath!).Any(IsWrapperEntry));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static BuildSummary Build(
        BuildCoordinator coordinator,
        PackageProject project,
        string outputRoot,
        ExistingOutputDecision outputDecision = ExistingOutputDecision.None,
        VersionReuseDecision versionDecision = VersionReuseDecision.None) =>
        coordinator.Build(new BuildRequest
        {
            Project = project,
            OutputDirectory = outputRoot,
            ExistingOutputDecision = outputDecision,
            VersionReuseDecision = versionDecision
        });

    private static PackageProject SimplePropProject(string sfmGameRoot, string archiveName) => new()
    {
        AssetName = "M13 Simple Prop",
        CurrentVersion = "1.0",
        ArchiveName = archiveName,
        Models =
        {
            new ModelEntry
            {
                Role = ModelRole.Primary,
                SourceMdlPath = PathFor(sfmGameRoot, @"tf\models\props_gameplay\ball001.mdl"),
                SourceStem = "ball001",
                ReleaseStem = "ball001"
            }
        },
        MaterialSources =
        {
            new SourceEntry
            {
                Kind = SourceEntryKind.Material,
                SourcePath = DirectoryPath(sfmGameRoot, @"tf\materials\models\props_gameplay"),
                IsFolder = true,
                IncludeRecursively = true
            }
        },
        Readme = new ReadmeConfig { Mode = ReadmeMode.Generated, Description = "Simple prop acceptance package." }
    };

    private static PackageProject RenamedCharacterProject(string sfmGameRoot) => new()
    {
        AssetName = "M13 Renamed HWM Scout",
        CurrentVersion = "1.0",
        ArchiveName = "renamed-hwm-scout.zip",
        Models =
        {
            new ModelEntry
            {
                Role = ModelRole.Primary,
                SourceMdlPath = PathFor(sfmGameRoot, @"tf\models\player\hwm\scout.mdl"),
                SourceStem = "scout",
                ReleaseStem = "scout_acceptance"
            }
        },
        MaterialSources =
        {
            new SourceEntry
            {
                Kind = SourceEntryKind.Material,
                SourcePath = DirectoryPath(sfmGameRoot, @"tf\materials\models\player\scout"),
                IsFolder = true,
                IncludeRecursively = true
            }
        },
        Readme = new ReadmeConfig { Mode = ReadmeMode.Generated, Description = "HWM character acceptance package." }
    };

    private static PackageProject CombinedProject(string root, string sfmGameRoot)
    {
        var notes = Path.Combine(root, "acceptance-notes.txt");
        File.WriteAllText(notes, "Acceptance notes.", Encoding.UTF8);
        var importedReadme = Path.Combine(root, "README-imported.txt");
        File.WriteAllText(importedReadme, "Imported README acceptance text.", Encoding.UTF8);

        return new PackageProject
        {
            AssetName = "M13 Combined Package",
            CurrentVersion = "1.0",
            ArchiveName = "combined-real-sfm-fixture.zip",
            Models =
            {
                new ModelEntry
                {
                    Role = ModelRole.Primary,
                    SourceMdlPath = PathFor(sfmGameRoot, @"tf\models\player\hwm\scout.mdl"),
                    SourceStem = "scout",
                    ReleaseStem = "scout_acceptance"
                },
                new ModelEntry
                {
                    Role = ModelRole.Additional,
                    SourceMdlPath = PathFor(sfmGameRoot, @"hl2\models\alyx_animations.mdl"),
                    SourceStem = "alyx_animations",
                    ReleaseStem = "alyx_animations"
                }
            },
            MaterialSources =
            {
                new SourceEntry
                {
                    Kind = SourceEntryKind.Material,
                    SourcePath = DirectoryPath(sfmGameRoot, @"tf\materials\models\player\scout"),
                    IsFolder = true,
                    IncludeRecursively = true
                }
            },
            Extras =
            {
                new SourceEntry
                {
                    Kind = SourceEntryKind.Extra,
                    SourcePath = PathFor(sfmGameRoot, @"usermod\cfg\sfm_defaultanimationgroups.txt"),
                    IsFolder = false,
                    DestinationOverride = new DestinationOverride { Kind = DestinationOverrideKind.Custom, RelativePath = "cfg" }
                },
                new SourceEntry
                {
                    Kind = SourceEntryKind.Extra,
                    SourcePath = notes,
                    IsFolder = false,
                    DestinationOverride = new DestinationOverride { Kind = DestinationOverrideKind.Custom, RelativePath = "Docs" }
                }
            },
            Readme = new ReadmeConfig
            {
                Mode = ReadmeMode.Generated,
                Description = "Combined real SFM fixture acceptance package.",
                ImportedReadmePath = importedReadme
            },
            ChangesThisVersion = { "Initial acceptance build." }
        };
    }

    private static void AssertArchiveHasDirectRoots(string archivePath, params string[] expectedRootsOrEntries)
    {
        var entries = ReadArchiveEntries(archivePath);
        foreach (var expected in expectedRootsOrEntries)
        {
            Assert.IsTrue(
                expected.EndsWith("/", StringComparison.Ordinal)
                    ? entries.Any(entry => entry.StartsWith(expected, StringComparison.OrdinalIgnoreCase))
                    : entries.Contains(expected, StringComparer.OrdinalIgnoreCase),
                $"Archive did not contain {expected}.");
        }

        Assert.IsFalse(entries.Any(IsWrapperEntry), "Archive must not contain a staging/session wrapper root.");
    }

    private static void AssertArchiveContains(string archivePath, params string[] expectedEntries)
    {
        var entries = ReadArchiveEntries(archivePath);
        foreach (var expected in expectedEntries)
        {
            Assert.IsTrue(entries.Contains(expected, StringComparer.OrdinalIgnoreCase), $"Archive did not contain {expected}.");
        }
    }

    private static string[] ReadArchiveEntries(string archivePath)
    {
        using var archive = ZipFile.OpenRead(archivePath);
        return archive.Entries
            .Where(entry => !string.IsNullOrEmpty(entry.Name))
            .Select(entry => entry.FullName.Replace('\\', '/'))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void Extract(string archivePath, string installRoot) =>
        ZipFile.ExtractToDirectory(archivePath, installRoot, overwriteFiles: true);

    private static void AssertInstalled(string installRoot, string relativePath) =>
        Assert.IsTrue(File.Exists(Path.Combine(installRoot, relativePath)), $"Installed file missing: {relativePath}");

    private static bool IsWrapperEntry(string entry)
    {
        var first = entry.Split('/')[0];
        return first.Length == 32 && first.All(Uri.IsHexDigit);
    }

    private static void RequireFile(string sfmGameRoot, string relativePath)
    {
        if (!File.Exists(PathFor(sfmGameRoot, relativePath)))
        {
            Assert.Inconclusive("Required SFM fixture file was not found: " + relativePath);
        }
    }

    private static void RequireDirectory(string sfmGameRoot, string relativePath)
    {
        if (!Directory.Exists(DirectoryPath(sfmGameRoot, relativePath)))
        {
            Assert.Inconclusive("Required SFM fixture directory was not found: " + relativePath);
        }
    }

    private static string PathFor(string sfmGameRoot, string relativePath) => Path.Combine(sfmGameRoot, relativePath);

    private static string DirectoryPath(string sfmGameRoot, string relativePath) => Path.Combine(sfmGameRoot, relativePath);

    private static string Log(BuildSummary summary) =>
        string.Join(Environment.NewLine, summary.Log.Entries.Select(entry => entry.Stage + ": " + entry.Message + " " + entry.TechnicalDetail));

    private sealed class SourceSnapshot
    {
        private readonly IReadOnlyDictionary<string, Entry> entries;

        private SourceSnapshot(IReadOnlyDictionary<string, Entry> entries)
        {
            this.entries = entries;
        }

        public static SourceSnapshot Capture(IEnumerable<string> files, IEnumerable<string> directories)
        {
            var allFiles = files
                .Where(File.Exists)
                .Concat(directories.Where(Directory.Exists).SelectMany(directory => Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToDictionary(path => path, CaptureEntry, StringComparer.OrdinalIgnoreCase);
            return new SourceSnapshot(allFiles);
        }

        public void AssertUnchanged()
        {
            var current = Capture(entries.Keys, Array.Empty<string>()).entries;
            CollectionAssert.AreEqual(entries.Keys.ToArray(), current.Keys.ToArray());
            foreach (var path in entries.Keys)
            {
                Assert.AreEqual(entries[path], current[path], "Source changed: " + path);
            }
        }

        private static Entry CaptureEntry(string path)
        {
            using var sha = SHA256.Create();
            using var stream = File.OpenRead(path);
            return new Entry(
                new FileInfo(path).Length,
                Convert.ToHexString(sha.ComputeHash(stream)),
                File.GetAttributes(path));
        }

        private sealed record Entry(long Length, string Sha256, FileAttributes Attributes);
    }
}
