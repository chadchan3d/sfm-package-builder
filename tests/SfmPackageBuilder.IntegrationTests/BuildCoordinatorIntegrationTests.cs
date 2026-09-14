using System.IO.Compression;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SfmPackageBuilder.Core.Archive;
using SfmPackageBuilder.Core.Build;
using SfmPackageBuilder.Core.Domain;

namespace SfmPackageBuilder.IntegrationTests;

[TestClass]
public sealed class BuildCoordinatorIntegrationTests
{
    [TestMethod]
    public void NativeZipBuildCoordinatorCreatesVerifiedPackageAndCommitsHistoryThenCleansStaging()
    {
        var root = Path.Combine(Path.GetTempPath(), "Sfm Package Builder Coordinator Integration", Guid.NewGuid().ToString("N"));
        try
        {
            var appRoot = Path.Combine(root, "app root 椅子");
            var sourceRoot = Path.Combine(root, "source root");
            var outputRoot = Path.Combine(root, "output root");
            Directory.CreateDirectory(appRoot);
            Directory.CreateDirectory(sourceRoot);
            Directory.CreateDirectory(outputRoot);

            Write(sourceRoot, @"game\usermod\models\Creator\chair_dev\chair_dev.mdl", "mdl");
            Write(sourceRoot, @"game\usermod\models\Creator\chair_dev\chair_dev.vvd", "vvd");
            Write(sourceRoot, @"game\usermod\models\Creator\chair_dev\chair_dev.dx90.vtx", "dx90");
            Write(sourceRoot, @"game\usermod\materials\models\Creator\chair\chair.vmt", "vmt");
            Write(sourceRoot, @"game\usermod\materials\models\Creator\chair\body.vtf", "vtf");
            var sourceSnapshot = Snapshot(sourceRoot);

            var project = new PackageProject
            {
                AssetName = "Chair Prop",
                CurrentVersion = "1.0",
                ChangesThisVersion = new List<string> { "Initial release." },
                ArchiveName = "chair release 椅子.zip",
                Models = new List<ModelEntry>
                {
                    new()
                    {
                        Role = ModelRole.Primary,
                        SourceMdlPath = Path.Combine(sourceRoot, @"game\usermod\models\Creator\chair_dev\chair_dev.mdl"),
                        SourceStem = "chair_dev",
                        ReleaseStem = "chair_release"
                    }
                },
                MaterialSources = new List<SourceEntry>
                {
                    new()
                    {
                        Kind = SourceEntryKind.Material,
                        SourcePath = Path.Combine(sourceRoot, @"game\usermod\materials\models\Creator\chair"),
                        IsFolder = true,
                        IncludeRecursively = true
                    }
                },
                Readme = new ReadmeConfig { Mode = ReadmeMode.Generated }
            };

            var summary = new BuildCoordinator(appRoot).Build(new BuildRequest
            {
                Project = project,
                OutputDirectory = outputRoot
            });

            Assert.IsTrue(summary.Succeeded, string.Join(Environment.NewLine, summary.Log.Entries.Select(entry => entry.Stage + ": " + entry.Message + " " + entry.TechnicalDetail)));
            Assert.IsTrue(File.Exists(summary.ArchivePath!));
            CollectionAssert.AreEqual(
                new[]
                {
                    @"materials/models/Creator/chair/body.vtf",
                    @"materials/models/Creator/chair/chair.vmt",
                    @"models/Creator/chair_dev/chair_release.dx90.vtx",
                    @"models/Creator/chair_dev/chair_release.mdl",
                    @"models/Creator/chair_dev/chair_release.vvd",
                    "README.txt"
                },
                ReadArchiveEntries(summary.ArchivePath!));
            Assert.AreEqual(0, project.ReleaseHistory.Count);
            Assert.AreEqual(1, project.BuildHistory.Count);
            Assert.AreEqual("1.0", project.BuildHistory[0].Version);
            Assert.AreEqual("chair release 椅子.zip", project.BuildHistory[0].ArchiveName);
            Assert.IsFalse(Directory.Exists(summary.StagingResult!.Session!.StagingRoot));
            CollectionAssert.AreEqual(sourceSnapshot, Snapshot(sourceRoot));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }

                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static void Write(string root, string relativePath, string contents)
    {
        var path = Path.Combine(root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents, Encoding.UTF8);
    }

    private static string[] Snapshot(string root) =>
        Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/') + ":" + Convert.ToHexString(File.ReadAllBytes(path)) + ":" + File.GetAttributes(path))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static string[] ReadArchiveEntries(string archivePath)
    {
        using var archive = ZipFile.OpenRead(archivePath);
        return archive.Entries
            .Where(entry => !string.IsNullOrEmpty(entry.Name))
            .Select(entry => entry.FullName.Replace('\\', '/'))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
