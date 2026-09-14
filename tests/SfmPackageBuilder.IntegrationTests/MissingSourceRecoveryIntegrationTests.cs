using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SfmPackageBuilder.Core.Domain;
using SfmPackageBuilder.Core.FileSystem;
using SfmPackageBuilder.Core.Persistence;
using SfmPackageBuilder.Core.Planning;

namespace SfmPackageBuilder.IntegrationTests;

[TestClass]
public sealed class MissingSourceRecoveryIntegrationTests
{
    [TestMethod]
    public void SavedProjectReopensDetectsMovedModelAndPlansAfterLocateReplacement()
    {
        var root = Path.Combine(Path.GetTempPath(), "Sfm Package Builder Recovery Integration", Guid.NewGuid().ToString("N"));
        try
        {
            var oldModel = Path.Combine(root, @"old\game\usermod\models\Creator\chair\chair.mdl");
            var newModel = Path.Combine(root, @"new\game\usermod\models\Creator\chair\chair.mdl");
            var newCompanion = Path.Combine(root, @"new\game\usermod\models\Creator\chair\chair.vvd");
            Directory.CreateDirectory(Path.GetDirectoryName(oldModel)!);
            File.WriteAllText(oldModel, "old mdl", Encoding.UTF8);
            Directory.CreateDirectory(Path.GetDirectoryName(newModel)!);
            File.WriteAllText(newModel, "new mdl", Encoding.UTF8);
            File.WriteAllText(newCompanion, "new vvd", Encoding.UTF8);

            var project = new PackageProject
            {
                Models = new List<ModelEntry>
                {
                    new()
                    {
                        Role = ModelRole.Primary,
                        SourceMdlPath = oldModel,
                        SourceStem = "chair",
                        ReleaseStem = "chair_release"
                    }
                },
                Readme = new ReadmeConfig { Mode = ReadmeMode.None }
            };
            var serializer = new ProjectSerializer();
            var projectPath = Path.Combine(root, "project.sfmpack");
            serializer.Save(projectPath, project);
            File.Delete(oldModel);

            var reopened = serializer.Load(projectPath).Project!;
            var recovery = new MissingSourceRecoveryService(new PhysicalFileSystem());
            var group = recovery.Inspect(reopened).Groups.Single();
            var preview = recovery.PreviewFolderRemap(reopened, group.FormerRoot, Path.Combine(root, "new", "game", "usermod"));
            var located = recovery.ApplyFolderRemap(reopened, projectPath, preview);
            var plan = new PackagePlanner(new PhysicalFileSystem()).CreatePlan(reopened);

            Assert.AreEqual(1, located.AppliedCount);
            Assert.IsTrue(plan.Entries.Any(entry => entry.SourcePath == newModel && entry.DestinationRelativePath == @"models\Creator\chair\chair_release.mdl"));
            Assert.IsTrue(plan.Entries.Any(entry => entry.SourcePath == newCompanion && entry.DestinationRelativePath == @"models\Creator\chair\chair_release.vvd"));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
