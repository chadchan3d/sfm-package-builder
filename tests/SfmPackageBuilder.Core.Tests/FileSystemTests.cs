using Microsoft.VisualStudio.TestTools.UnitTesting;
using SfmPackageBuilder.Core.FileSystem;

namespace SfmPackageBuilder.Core.Tests;

[TestClass]
public sealed class FileSystemTests
{
    [TestMethod]
    public void FakeFileSystemSupportsNarrowInspectionAndReadOperations()
    {
        var fileSystem = new FakeFileSystem();
        fileSystem.AddFile(@"E:\SFM\game\usermod\models\Creator\chair.mdl", new byte[] { 1, 2, 3 });

        Assert.IsTrue(fileSystem.FileExists(@"E:\SFM\game\usermod\models\Creator\chair.mdl"));
        Assert.IsTrue(fileSystem.DirectoryExists(@"E:\SFM\game\usermod\models\Creator"));
        CollectionAssert.AreEqual(
            new[] { @"E:\SFM\game\usermod\models\Creator\chair.mdl" },
            fileSystem.EnumerateFiles(@"E:\SFM\game\usermod\models\Creator", recursive: false).ToArray());

        using var stream = fileSystem.OpenRead(@"E:\SFM\game\usermod\models\Creator\chair.mdl");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);

        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, memory.ToArray());
    }

    [TestMethod]
    public void PathAndReadOperationsDoNotMutateSourceFixture()
    {
        var root = Path.Combine(Path.GetTempPath(), "SfmPackageBuilderPathTests", Guid.NewGuid().ToString("N"));
        var sourceDirectory = Path.Combine(root, "models", "Creator");
        var sourcePath = Path.Combine(sourceDirectory, "chair.mdl");
        Directory.CreateDirectory(sourceDirectory);
        File.WriteAllText(sourcePath, "source bytes");
        File.SetAttributes(sourcePath, FileAttributes.ReadOnly);

        try
        {
            var beforeFileName = Path.GetFileName(sourcePath);
            var beforeBytes = File.ReadAllBytes(sourcePath);
            var beforeAttributes = File.GetAttributes(sourcePath);
            var beforeEntries = Directory.GetFileSystemEntries(sourceDirectory).Select(Path.GetFileName).Order().ToArray();

            var fileSystem = new PhysicalFileSystem();

            Assert.IsTrue(fileSystem.FileExists(sourcePath));
            Assert.IsTrue(fileSystem.DirectoryExists(sourceDirectory));
            _ = fileSystem.GetAttributes(sourcePath);
            _ = fileSystem.EnumerateFiles(sourceDirectory, recursive: false);
            using (var stream = fileSystem.OpenRead(sourcePath))
            {
                Assert.IsTrue(stream.Length > 0);
            }

            var afterFileName = Path.GetFileName(sourcePath);
            var afterBytes = File.ReadAllBytes(sourcePath);
            var afterAttributes = File.GetAttributes(sourcePath);
            var afterEntries = Directory.GetFileSystemEntries(sourceDirectory).Select(Path.GetFileName).Order().ToArray();

            Assert.AreEqual(beforeFileName, afterFileName);
            CollectionAssert.AreEqual(beforeBytes, afterBytes);
            Assert.AreEqual(beforeAttributes, afterAttributes);
            CollectionAssert.AreEqual(beforeEntries, afterEntries);
        }
        finally
        {
            File.SetAttributes(sourcePath, FileAttributes.Normal);
            Directory.Delete(root, recursive: true);
        }
    }
}
