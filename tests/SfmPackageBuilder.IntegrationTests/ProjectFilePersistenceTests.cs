using Microsoft.VisualStudio.TestTools.UnitTesting;
using SfmPackageBuilder.Core.Domain;
using SfmPackageBuilder.Core.Persistence;

namespace SfmPackageBuilder.IntegrationTests;

[TestClass]
public sealed class ProjectFilePersistenceTests
{
    [TestMethod]
    public void CompleteProjectCanSaveAndReopenFromSfmpackFile()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "SfmPackageBuilderTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var path = Path.Combine(tempDirectory, "Chair_Prop.sfmpack");
            var serializer = new ProjectSerializer();
            var project = new PackageProject
            {
                AssetName = "Chair Prop",
                CurrentVersion = "1.0",
                ArchiveName = "Chair_Prop_v1.0.zip",
                Models = new List<ModelEntry>
                {
                    new()
                    {
                        Role = ModelRole.Primary,
                        SourceMdlPath = @"E:\SFM\game\usermod\models\Creator\props\chair\chair_dev.mdl",
                        SourceStem = "chair_dev",
                        ReleaseStem = "chair_release"
                    }
                },
                Readme = new ReadmeConfig
                {
                    Mode = ReadmeMode.Generated
                },
                ReleaseHistory = new List<ReleaseRecord>
                {
                    new()
                    {
                        Version = "1.0",
                        Changes = new List<string> { "Initial release." }
                    }
                }
            };

            serializer.Save(path, project);
            var result = serializer.Load(path);

            Assert.AreEqual(ProjectLoadStatus.Success, result.Status);
            Assert.IsNotNull(result.Project);
            Assert.AreEqual("Chair Prop", result.Project!.AssetName);
            Assert.AreEqual("chair_release", result.Project.Models[0].ReleaseStem);
            Assert.AreEqual("Initial release.", result.Project.ReleaseHistory[0].Changes[0]);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }
}
