using System.IO.Compression;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SfmPackageBuilder.Core.Archive;
using SfmPackageBuilder.Core.Staging;

namespace SfmPackageBuilder.IntegrationTests;

[TestClass]
public sealed class ArchiveServiceIntegrationTests
{
    [TestMethod]
    public void NativeZipArchivesStagingContentsWithoutWrapperAndSupportsSpacesAndUnicode()
    {
        var root = Path.Combine(Path.GetTempPath(), "Sfm Package Builder Archive Integration", Guid.NewGuid().ToString("N"));
        try
        {
            var appRoot = Path.Combine(root, "app root with spaces 椅子");
            var outputRoot = Path.Combine(root, "output root with spaces");
            Directory.CreateDirectory(appRoot);
            Directory.CreateDirectory(outputRoot);

            var session = CreateSession(
                appRoot,
                (@"models\Creator\chair\chair.mdl", "mdl"),
                (@"materials\models\Creator\chair\body 椅子.vtf", "material"),
                ("README.txt", "readme"),
                (@"Extras\docs\guide.txt", "guide"));
            var target = Path.Combine(outputRoot, "release 椅子.zip");

            var result = new ArchiveService().CreateArchive(session, target);

            Assert.IsTrue(result.Succeeded, result.Failure?.Message + Environment.NewLine + result.Failure?.TechnicalDetail);
            CollectionAssert.AreEqual(
                new[]
                {
                    @"Extras/docs/guide.txt",
                    @"materials/models/Creator/chair/body 椅子.vtf",
                    @"models/Creator/chair/chair.mdl",
                    "README.txt"
                },
                ReadArchiveEntries(target));
            Assert.IsFalse(ReadArchiveEntries(target).Any(entry => entry.StartsWith(session.SessionId.ToString("N") + "/", StringComparison.OrdinalIgnoreCase)));
            Assert.IsTrue(File.Exists(Path.Combine(session.StagingRoot, @"models\Creator\chair\chair.mdl")));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static StagingSession CreateSession(string appRoot, params (string RelativePath, string Contents)[] files)
    {
        var sessionId = Guid.NewGuid();
        var stagingRoot = Path.Combine(appRoot, ".staging", sessionId.ToString("N"));
        Directory.CreateDirectory(stagingRoot);
        var staged = new List<StagedPackageFile>();
        foreach (var file in files)
        {
            var path = Path.Combine(stagingRoot, file.RelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, file.Contents, Encoding.UTF8);
            staged.Add(new StagedPackageFile(
                file.RelativePath,
                file.RelativePath,
                path,
                StagingWriteKind.SourceCopy,
                null,
                new FileInfo(path).Length));
        }

        return new StagingSession(sessionId, appRoot, stagingRoot, staged, Array.Empty<StagingDuplicateWrite>());
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
}
