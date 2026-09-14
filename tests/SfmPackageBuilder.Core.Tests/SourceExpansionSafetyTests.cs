using Microsoft.VisualStudio.TestTools.UnitTesting;
using SfmPackageBuilder.Core.Domain;
using SfmPackageBuilder.Core.Expansion;
using SfmPackageBuilder.Core.FileSystem;

namespace SfmPackageBuilder.Core.Tests;

[TestClass]
public sealed class SourceExpansionSafetyTests
{
    [TestMethod]
    public void PhysicalDetectionAndExpansionDoNotMutateSourceFixtures()
    {
        var root = Path.Combine(Path.GetTempPath(), "SfmPackageBuilderExpansionTests", Guid.NewGuid().ToString("N"));
        var modelDirectory = Path.Combine(root, "models", "Creator");
        var materialDirectory = Path.Combine(root, "materials", "models", "Creator", "chair");
        Directory.CreateDirectory(modelDirectory);
        Directory.CreateDirectory(materialDirectory);

        var modelPath = Path.Combine(modelDirectory, "chair.mdl");
        var companionPath = Path.Combine(modelDirectory, "chair.vvd");
        var materialPath = Path.Combine(materialDirectory, "chair.vmt");
        File.WriteAllText(modelPath, "model bytes");
        File.WriteAllText(companionPath, "companion bytes");
        File.WriteAllText(materialPath, "material bytes");
        File.SetAttributes(modelPath, FileAttributes.ReadOnly);

        try
        {
            var watchedPaths = new[] { modelPath, companionPath, materialPath };
            var before = Snapshot(watchedPaths, modelDirectory, materialDirectory);
            var service = new SourceExpansionService(new PhysicalFileSystem());
            var project = new PackageProject
            {
                Models = new List<ModelEntry>
                {
                    new()
                    {
                        Role = ModelRole.Primary,
                        SourceMdlPath = modelPath,
                        SourceStem = "chair",
                        ReleaseStem = "chair_release"
                    }
                },
                MaterialSources = new List<SourceEntry>
                {
                    new()
                    {
                        Kind = SourceEntryKind.Material,
                        SourcePath = materialDirectory,
                        IsFolder = true,
                        IncludeRecursively = true
                    }
                }
            };

            var result = service.Resolve(project);

            Assert.AreEqual(ModelFamilyResolutionStatus.Resolved, result.ModelFamilies[0].Status);
            Assert.AreEqual(SourceInventoryStatus.Resolved, result.MaterialInventories[0].Status);
            var after = Snapshot(watchedPaths, modelDirectory, materialDirectory);

            CollectionAssert.AreEqual(before.FileNames, after.FileNames);
            CollectionAssert.AreEqual(before.Bytes.Select(Convert.ToBase64String).ToArray(), after.Bytes.Select(Convert.ToBase64String).ToArray());
            CollectionAssert.AreEqual(before.Attributes, after.Attributes);
            CollectionAssert.AreEqual(before.DirectoryEntries, after.DirectoryEntries);
        }
        finally
        {
            File.SetAttributes(modelPath, FileAttributes.Normal);
            Directory.Delete(root, recursive: true);
        }
    }

    private static SourceSnapshot Snapshot(string[] watchedPaths, params string[] directories)
    {
        return new SourceSnapshot(
            watchedPaths.Select(Path.GetFileName).ToArray()!,
            watchedPaths.Select(File.ReadAllBytes).ToArray(),
            watchedPaths.Select(File.GetAttributes).ToArray(),
            directories
                .SelectMany(directory => Directory.GetFileSystemEntries(directory))
                .Select(Path.GetFileName)
                .Order()
                .ToArray()!);
    }

    private sealed record SourceSnapshot(
        string[] FileNames,
        byte[][] Bytes,
        FileAttributes[] Attributes,
        string[] DirectoryEntries);
}
