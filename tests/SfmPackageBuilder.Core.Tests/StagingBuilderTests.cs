using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SfmPackageBuilder.Core.FileSystem;
using SfmPackageBuilder.Core.Planning;
using SfmPackageBuilder.Core.SharedFiles;
using SfmPackageBuilder.Core.Staging;

namespace SfmPackageBuilder.Core.Tests;

[TestClass]
public sealed class StagingBuilderTests
{
    [TestMethod]
    public void FreshUniqueApplicationOwnedStagingDirectoryEachCall()
    {
        using var workspace = TempWorkspace.Create();
        var source = workspace.WriteSource(@"sources\chair.mdl", "mdl");
        var plan = Plan(Entry("model", PackagePlanEntryType.Model, source, @"models\Creator\chair\chair.mdl"));

        var first = Stage(workspace, plan);
        var second = Stage(workspace, plan);

        Assert.IsTrue(first.Succeeded, first.Failure?.Message);
        Assert.IsTrue(second.Succeeded, second.Failure?.Message);
        Assert.AreNotEqual(first.Session!.StagingRoot, second.Session!.StagingRoot);
        Assert.IsTrue(first.Session.IsApplicationOwned);
        Assert.IsTrue(second.Session.IsApplicationOwned);
        StringAssert.StartsWith(first.Session.StagingRoot, Path.Combine(workspace.ApplicationRoot, ".staging"));
        Assert.IsTrue(File.Exists(Path.Combine(first.Session.StagingRoot, @"models\Creator\chair\chair.mdl")));
        Assert.IsFalse(Directory.Exists(Path.Combine(first.Session.StagingRoot, "Chair_Prop_v1")), "Staging must not add an archive wrapper folder.");
    }

    [TestMethod]
    public void ModelFamilyCopiesDirectlyToFinalRenamedDestinations()
    {
        using var workspace = TempWorkspace.Create();
        var plan = Plan(
            Entry("mdl", PackagePlanEntryType.Model, workspace.WriteSource(@"src\house_chadfixv05.mdl", "mdl"), @"models\Creator\house\house_chadchan3d.mdl"),
            Entry("vvd", PackagePlanEntryType.ModelCompanion, workspace.WriteSource(@"src\house_chadfixv05.vvd", "vvd"), @"models\Creator\house\house_chadchan3d.vvd"),
            Entry("dx90", PackagePlanEntryType.ModelCompanion, workspace.WriteSource(@"src\house_chadfixv05.dx90.vtx", "dx90"), @"models\Creator\house\house_chadchan3d.dx90.vtx"),
            Entry("dx80", PackagePlanEntryType.ModelCompanion, workspace.WriteSource(@"src\house_chadfixv05.dx80.vtx", "dx80"), @"models\Creator\house\house_chadchan3d.dx80.vtx"),
            Entry("sw", PackagePlanEntryType.ModelCompanion, workspace.WriteSource(@"src\house_chadfixv05.sw.vtx", "sw"), @"models\Creator\house\house_chadchan3d.sw.vtx"),
            Entry("phy", PackagePlanEntryType.ModelCompanion, workspace.WriteSource(@"src\house_chadfixv05.phy", "phy"), @"models\Creator\house\house_chadchan3d.phy"),
            Entry("additional", PackagePlanEntryType.Model, workspace.WriteSource(@"src\hinge.mdl", "hinge"), @"models\Creator\shared\hinge.mdl"));

        var result = Stage(workspace, plan);

        Assert.IsTrue(result.Succeeded, result.Failure?.Message);
        var root = result.Session!.StagingRoot;
        AssertExists(root, @"models\Creator\house\house_chadchan3d.mdl");
        AssertExists(root, @"models\Creator\house\house_chadchan3d.vvd");
        AssertExists(root, @"models\Creator\house\house_chadchan3d.dx90.vtx");
        AssertExists(root, @"models\Creator\house\house_chadchan3d.dx80.vtx");
        AssertExists(root, @"models\Creator\house\house_chadchan3d.sw.vtx");
        AssertExists(root, @"models\Creator\house\house_chadchan3d.phy");
        AssertExists(root, @"models\Creator\shared\hinge.mdl");
        Assert.IsFalse(File.Exists(Path.Combine(root, @"models\Creator\house\house_chadfixv05.mdl")));
        Assert.IsFalse(File.Exists(Path.Combine(root, @"models\Creator\house\house_chadfixv05.dx90.vtx")));
    }

    [TestMethod]
    public void MaterialsAndExtrasPreserveExactPlannedTree()
    {
        using var workspace = TempWorkspace.Create();
        var plan = Plan(
            Entry("material", PackagePlanEntryType.Material, workspace.WriteSource(@"src\chair.vmt", "vmt"), @"materials\models\Creator\chair\chair.vmt"),
            Entry("nested-material", PackagePlanEntryType.Material, workspace.WriteSource(@"src\nested\body.vtf", "vtf"), @"materials\models\Creator\chair\skins\red\body.vtf"),
            Entry("root-extra", PackagePlanEntryType.Extra, workspace.WriteSource(@"extras\LICENSE.txt", "license"), "LICENSE.txt"),
            Entry("misc-extra", PackagePlanEntryType.Extra, workspace.WriteSource(@"extras\notes.txt", "notes"), @"Misc\notes.txt"),
            Entry("custom-extra", PackagePlanEntryType.Extra, workspace.WriteSource(@"extras\custom\guide.txt", "guide"), @"Docs\Guides\guide.txt"),
            Entry("nested-extra", PackagePlanEntryType.Extra, workspace.WriteSource(@"extras\folder\a\b\credits.txt", "credits"), @"Extras\folder\a\b\credits.txt"));

        var result = Stage(workspace, plan);

        Assert.IsTrue(result.Succeeded, result.Failure?.Message);
        var root = result.Session!.StagingRoot;
        AssertExists(root, @"materials\models\Creator\chair\chair.vmt");
        AssertExists(root, @"materials\models\Creator\chair\skins\red\body.vtf");
        AssertExists(root, "LICENSE.txt");
        AssertExists(root, @"Misc\notes.txt");
        AssertExists(root, @"Docs\Guides\guide.txt");
        AssertExists(root, @"Extras\folder\a\b\credits.txt");
    }

    [TestMethod]
    public void GeneratedAndProjectTextReadmeUseResolvedPlanBytes()
    {
        using var workspace = TempWorkspace.Create();
        var generatedBytes = Encoding.UTF8.GetBytes("Generated\r\nREADME");
        var customBytes = Encoding.UTF8.GetBytes("Custom project text\r\nREADME");

        var generated = Stage(workspace, Plan(Readme("readme-generated", ReadmePlanEntryKind.GeneratedText, generatedBytes, "display text")));
        var custom = Stage(workspace, Plan(Readme("readme-custom", ReadmePlanEntryKind.CustomText, customBytes, "display text")));

        CollectionAssert.AreEqual(generatedBytes, File.ReadAllBytes(Path.Combine(generated.Session!.StagingRoot, "README.txt")));
        CollectionAssert.AreEqual(customBytes, File.ReadAllBytes(Path.Combine(custom.Session!.StagingRoot, "README.txt")));
    }

    [TestMethod]
    public void ImportedReadmeCopiesOriginalBytesAndIgnoresPreviewText()
    {
        using var workspace = TempWorkspace.Create();
        var importedBytes = new byte[] { 0xff, 0xfe, 0x00, 0x41, 0x0d, 0x0a };
        var imported = workspace.WriteSource(@"docs\README-source.txt", importedBytes);
        var plan = Plan(new PackagePlanEntry(
            "readme-imported",
            PackagePlanEntryType.Readme,
            PackagePlanEntryStatus.Resolved,
            "README.txt",
            imported,
            null,
            null,
            isGenerated: false,
            readmeKind: ReadmePlanEntryKind.ImportedFile,
            textContent: "Preview text must never be staged."));

        var result = Stage(workspace, plan);

        Assert.IsTrue(result.Succeeded, result.Failure?.Message);
        CollectionAssert.AreEqual(importedBytes, File.ReadAllBytes(Path.Combine(result.Session!.StagingRoot, "README.txt")));
    }

    [TestMethod]
    public void NoReadmePlanEntryCreatesNoReadmeFile()
    {
        using var workspace = TempWorkspace.Create();
        var source = workspace.WriteSource(@"src\model.mdl", "mdl");

        var result = Stage(workspace, Plan(Entry("model", PackagePlanEntryType.Model, source, @"models\a\model.mdl")));

        Assert.IsTrue(result.Succeeded, result.Failure?.Message);
        Assert.IsFalse(File.Exists(Path.Combine(result.Session!.StagingRoot, "README.txt")));
    }

    [TestMethod]
    public void ReadOnlySourceStagesSuccessfullyAndSourceAttributesRemainUnchanged()
    {
        using var workspace = TempWorkspace.Create();
        var source = workspace.WriteSource(@"src\readonly.vtf", "readonly");
        File.SetAttributes(source, File.GetAttributes(source) | FileAttributes.ReadOnly);
        var before = SourceSnapshot.Capture(source);

        var result = Stage(workspace, Plan(Entry("material", PackagePlanEntryType.Material, source, @"materials\readonly.vtf")));

        Assert.IsTrue(result.Succeeded, result.Failure?.Message);
        var staged = Path.Combine(result.Session!.StagingRoot, @"materials\readonly.vtf");
        Assert.IsTrue(File.Exists(staged));
        Assert.IsFalse((File.GetAttributes(staged) & FileAttributes.ReadOnly) != 0);
        before.AssertUnchanged(source);
    }

    [TestMethod]
    public void SamePhysicalSourceSameDestinationCopiesOnceAndRecordsDuplicate()
    {
        using var workspace = TempWorkspace.Create();
        var source = workspace.WriteSource(@"src\shared.txt", "shared");
        var tracking = new TrackingFileSystem();
        var plan = Plan(
            Entry("first", PackagePlanEntryType.Extra, source, @"Misc\shared.txt"),
            Entry("second", PackagePlanEntryType.Extra, source, @"Misc\shared.txt"));

        var result = new StagingBuilder(workspace.ApplicationRoot, tracking).Stage(plan);

        Assert.IsTrue(result.Succeeded, result.Failure?.Message);
        Assert.AreEqual(1, tracking.CopyFileCallCount);
        Assert.AreEqual(1, result.Session!.SkippedDuplicateWrites.Count);
        CollectionAssert.AreEqual(new[] { "second" }, result.Session.SkippedDuplicateWrites[0].SkippedEntryIds.ToArray());
    }

    [TestMethod]
    public void DifferentSourcesSameDestinationFailsWithoutOverwrite()
    {
        using var workspace = TempWorkspace.Create();
        var first = workspace.WriteSource(@"src\first.txt", "first");
        var second = workspace.WriteSource(@"src\second.txt", "second");
        var firstBefore = SourceSnapshot.Capture(first);
        var secondBefore = SourceSnapshot.Capture(second);
        var plan = Plan(
            Entry("first", PackagePlanEntryType.Extra, first, @"Misc\same.txt"),
            Entry("second", PackagePlanEntryType.Extra, second, @"Misc\same.txt"));

        var result = Stage(workspace, plan);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(StagingFailureKind.DestinationCollision, result.Failure!.Kind);
        Assert.IsTrue(Directory.Exists(result.Failure.StagingRoot), "Failed staging directories are retained.");
        Assert.IsFalse(File.Exists(Path.Combine(result.Failure.StagingRoot, @"Misc\same.txt")));
        firstBefore.AssertUnchanged(first);
        secondBefore.AssertUnchanged(second);
    }

    [TestMethod]
    public void UnsafeAndUnresolvedPlanEntriesFailSafely()
    {
        using var workspace = TempWorkspace.Create();
        var source = workspace.WriteSource(@"src\source.txt", "source");

        AssertFailure(workspace, Plan(UnresolvedEntry("missing", source, @"Misc\source.txt")), StagingFailureKind.UnresolvedPlanEntry);
        AssertFailure(workspace, Plan(Entry("traversal", PackagePlanEntryType.Extra, source, @"..\outside.txt")), StagingFailureKind.UnsafeDestination);
        AssertFailure(workspace, Plan(Entry("rooted", PackagePlanEntryType.Extra, source, @"C:\outside.txt")), StagingFailureKind.UnsafeDestination);
        AssertFailure(workspace, Plan(Entry("missing-source", PackagePlanEntryType.Extra, Path.Combine(workspace.SourceRoot, "missing.txt"), @"Misc\missing.txt")), StagingFailureKind.MissingSource);
    }

    [TestMethod]
    public void FailedStagingRetainsDirectoryAndDoesNotMutateSource()
    {
        using var workspace = TempWorkspace.Create();
        var source = workspace.WriteSource(@"src\source.txt", "source");
        var before = SourceSnapshot.CaptureDirectory(workspace.SourceRoot);

        var result = Stage(workspace, Plan(Entry("bad", PackagePlanEntryType.Extra, source, @"..\outside.txt")));

        Assert.IsFalse(result.Succeeded);
        Assert.IsTrue(Directory.Exists(result.Failure!.StagingRoot));
        before.AssertUnchangedDirectory(workspace.SourceRoot);
    }

    [TestMethod]
    public void VerifierDetectsMissingWrongSizedWrongReadmeAndUnexpectedFiles()
    {
        using var workspace = TempWorkspace.Create();
        var source = workspace.WriteSource(@"src\file.txt", "1234");
        var sourcePlan = Plan(Entry("source", PackagePlanEntryType.Extra, source, @"Misc\file.txt"));
        var verifier = new StagedPackageVerifier(new PhysicalFileSystem());

        var missingRoot = workspace.CreateStagingRoot();
        Assert.AreEqual(StagingFailureKind.VerificationFailed, verifier.Verify(sourcePlan, missingRoot).Failure!.Kind);

        var wrongSizeRoot = workspace.CreateStagingRoot();
        workspace.WriteStaged(wrongSizeRoot, @"Misc\file.txt", "123");
        Assert.AreEqual(StagingFailureKind.VerificationFailed, verifier.Verify(sourcePlan, wrongSizeRoot).Failure!.Kind);

        var readmeRoot = workspace.CreateStagingRoot();
        var readmePlan = Plan(Readme("readme", ReadmePlanEntryKind.GeneratedText, Encoding.UTF8.GetBytes("expected"), "preview"));
        workspace.WriteStaged(readmeRoot, "README.txt", "actual");
        Assert.AreEqual(StagingFailureKind.VerificationFailed, verifier.Verify(readmePlan, readmeRoot).Failure!.Kind);

        var unexpectedRoot = workspace.CreateStagingRoot();
        workspace.WriteStaged(unexpectedRoot, @"Misc\file.txt", "1234");
        workspace.WriteStaged(unexpectedRoot, @"Misc\extra.txt", "extra");
        Assert.AreEqual(StagingFailureKind.VerificationFailed, verifier.Verify(sourcePlan, unexpectedRoot).Failure!.Kind);
    }

    [TestMethod]
    public void StagingTreeIsDeterministicForDifferentPlanOrdering()
    {
        using var workspace = TempWorkspace.Create();
        var a = workspace.WriteSource(@"src\a.txt", "a");
        var b = workspace.WriteSource(@"src\b.txt", "b");
        var c = workspace.WriteSource(@"src\c.txt", "c");

        var first = Stage(workspace, Plan(
            Entry("c", PackagePlanEntryType.Extra, c, @"Misc\c.txt"),
            Entry("a", PackagePlanEntryType.Extra, a, @"Misc\a.txt"),
            Entry("b", PackagePlanEntryType.Extra, b, @"Misc\b.txt")));
        var second = Stage(workspace, Plan(
            Entry("b", PackagePlanEntryType.Extra, b, @"Misc\b.txt"),
            Entry("c", PackagePlanEntryType.Extra, c, @"Misc\c.txt"),
            Entry("a", PackagePlanEntryType.Extra, a, @"Misc\a.txt")));

        CollectionAssert.AreEqual(TreeSignature(first.Session!.StagingRoot), TreeSignature(second.Session!.StagingRoot));
    }

    private static StagingResult Stage(TempWorkspace workspace, PackagePlan plan) =>
        new StagingBuilder(workspace.ApplicationRoot).Stage(plan);

    private static void AssertFailure(TempWorkspace workspace, PackagePlan plan, StagingFailureKind expected)
    {
        var result = Stage(workspace, plan);
        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(expected, result.Failure!.Kind);
        Assert.IsTrue(Directory.Exists(result.Failure.StagingRoot));
    }

    private static PackagePlan Plan(params PackagePlanEntry[] entries) =>
        new(entries, Array.Empty<SharedFileNotice>());

    private static PackagePlanEntry Entry(string id, PackagePlanEntryType type, string source, string destination) =>
        new(id, type, PackagePlanEntryStatus.Resolved, destination, source, Guid.NewGuid(), null, isGenerated: false);

    private static PackagePlanEntry UnresolvedEntry(string id, string source, string destination) =>
        new(id, PackagePlanEntryType.Extra, PackagePlanEntryStatus.MissingSource, destination, source, Guid.NewGuid(), null, isGenerated: false);

    private static PackagePlanEntry Readme(string id, ReadmePlanEntryKind kind, byte[] bytes, string text) =>
        new(id, PackagePlanEntryType.Readme, PackagePlanEntryStatus.Resolved, "README.txt", null, null, null, isGenerated: true, kind, text, bytes);

    private static void AssertExists(string root, string relativePath) =>
        Assert.IsTrue(File.Exists(Path.Combine(root, relativePath)), relativePath);

    private static string[] TreeSignature(string root) =>
        Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(root, path).Replace('/', '\\') + ":" + Convert.ToHexString(File.ReadAllBytes(path)))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private sealed class TrackingFileSystem : IFileSystem
    {
        private readonly PhysicalFileSystem inner = new();

        public int CopyFileCallCount { get; private set; }

        public bool FileExists(string path) => inner.FileExists(path);

        public bool DirectoryExists(string path) => inner.DirectoryExists(path);

        public Stream OpenRead(string path) => inner.OpenRead(path);

        public IReadOnlyList<string> EnumerateFiles(string directoryPath, bool recursive) => inner.EnumerateFiles(directoryPath, recursive);

        public IReadOnlyList<string> EnumerateDirectories(string directoryPath, bool recursive) => inner.EnumerateDirectories(directoryPath, recursive);

        public FileAttributes GetAttributes(string path) => inner.GetAttributes(path);

        public void CopyFile(string sourcePath, string destinationPath, bool overwrite)
        {
            CopyFileCallCount++;
            inner.CopyFile(sourcePath, destinationPath, overwrite);
        }
    }

    private sealed class TempWorkspace : IDisposable
    {
        private TempWorkspace(string root)
        {
            Root = root;
            ApplicationRoot = Path.Combine(root, "app");
            SourceRoot = Path.Combine(root, "source");
            Directory.CreateDirectory(ApplicationRoot);
            Directory.CreateDirectory(SourceRoot);
        }

        public string Root { get; }

        public string ApplicationRoot { get; }

        public string SourceRoot { get; }

        public static TempWorkspace Create() =>
            new(Path.Combine(Path.GetTempPath(), "SfmPackageBuilder.Tests", Guid.NewGuid().ToString("N")));

        public string WriteSource(string relativePath, string contents) =>
            WriteSource(relativePath, Encoding.UTF8.GetBytes(contents));

        public string WriteSource(string relativePath, byte[] contents)
        {
            var path = Path.Combine(SourceRoot, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, contents);
            return path;
        }

        public string CreateStagingRoot()
        {
            var root = Path.Combine(ApplicationRoot, ".staging", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return root;
        }

        public void WriteStaged(string stagingRoot, string relativePath, string contents)
        {
            var path = Path.Combine(stagingRoot, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, contents, Encoding.UTF8);
        }

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
        private SourceSnapshot(byte[] bytes, FileAttributes attributes)
        {
            Bytes = bytes;
            Attributes = attributes;
        }

        private byte[] Bytes { get; }

        private FileAttributes Attributes { get; }

        public static SourceSnapshot Capture(string path) =>
            new(File.ReadAllBytes(path), File.GetAttributes(path));

        public void AssertUnchanged(string path)
        {
            CollectionAssert.AreEqual(Bytes, File.ReadAllBytes(path));
            Assert.AreEqual(Attributes, File.GetAttributes(path));
        }

        public static IReadOnlyList<(string Path, byte[] Bytes, FileAttributes Attributes)> CaptureDirectory(string root) =>
            Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .Select(path => (Path.GetRelativePath(root, path), File.ReadAllBytes(path), File.GetAttributes(path)))
                .OrderBy(item => item.Item1, StringComparer.OrdinalIgnoreCase)
                .ToArray();
    }
}

internal static class SourceSnapshotExtensions
{
    public static void AssertUnchangedDirectory(
        this IReadOnlyList<(string Path, byte[] Bytes, FileAttributes Attributes)> snapshot,
        string root)
    {
        var current = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Select(path => (Path.GetRelativePath(root, path), File.ReadAllBytes(path), File.GetAttributes(path)))
            .OrderBy(item => item.Item1, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.AreEqual(snapshot.Count, current.Length);
        for (var i = 0; i < snapshot.Count; i++)
        {
            Assert.AreEqual(snapshot[i].Path, current[i].Item1);
            CollectionAssert.AreEqual(snapshot[i].Bytes, current[i].Item2);
            Assert.AreEqual(snapshot[i].Attributes, current[i].Item3);
        }
    }
}
