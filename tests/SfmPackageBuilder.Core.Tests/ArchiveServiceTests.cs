using System.IO.Compression;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SfmPackageBuilder.Core.Archive;
using SfmPackageBuilder.Core.Staging;

namespace SfmPackageBuilder.Core.Tests;

[TestClass]
public sealed class ArchiveServiceTests
{
    [TestMethod]
    public void NativeZipCreatesPackageRootWithoutStagingWrapper()
    {
        using var workspace = ArchiveWorkspace.Create("app with spaces");
        var session = workspace.CreateSession(
            (@"models\Creator\chair\chair.mdl", Bytes("mdl")),
            (@"materials\models\Creator\chair\chair.vmt", Bytes("vmt")),
            ("README.txt", Bytes("readme")),
            (@"Docs\guide.txt", Bytes("guide")),
            (@"Extras\custom folder\extra.txt", Bytes("extra")));
        var target = Path.Combine(workspace.OutputRoot, "archive with spaces.zip");

        var result = new ArchiveService().CreateArchive(session, target);

        Assert.IsTrue(result.Succeeded, result.Failure?.Message);
        Assert.AreEqual(ArchiveReplacementBehavior.TargetCreated, result.ReplacementBehavior);
        AssertArchiveEntries(
            target,
            @"Docs/guide.txt",
            @"Extras/custom folder/extra.txt",
            @"materials/models/Creator/chair/chair.vmt",
            @"models/Creator/chair/chair.mdl",
            "README.txt");
        Assert.IsFalse(ReadArchiveEntries(target).Any(entry => entry.StartsWith(session.SessionId.ToString("N") + "/", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void NewArchiveWritesVerifiedTemporaryZipThenCommitsFinalTarget()
    {
        using var workspace = ArchiveWorkspace.Create();
        var session = workspace.CreateSession((@"models\a.mdl", Bytes("model")));
        var target = Path.Combine(workspace.OutputRoot, "new.zip");
        var writer = new RecordingZipArchiveWriter();

        var result = new ArchiveService(writer).CreateArchive(session, target);

        Assert.IsTrue(result.Succeeded, result.Failure?.Message);
        Assert.IsNotNull(writer.WrittenPath);
        Assert.AreNotEqual(Path.GetFullPath(target), Path.GetFullPath(writer.WrittenPath!));
        StringAssert.StartsWith(Path.GetFullPath(writer.WrittenPath!), Path.GetFullPath(workspace.OutputRoot));
        Assert.IsTrue(File.Exists(target));
        AssertArchiveEntries(target, @"models/a.mdl");
        AssertNoTemporaryArchives(workspace.OutputRoot);
    }

    [TestMethod]
    public void NativeZipPreservesExactStagedMembershipAndBytes()
    {
        using var workspace = ArchiveWorkspace.Create();
        var binary = Enumerable.Range(0, 512).Select(value => (byte)(value % 251)).ToArray();
        var session = workspace.CreateSession(
            (@"models\a.mdl", binary),
            (@"materials\a.vmt", Bytes("material")),
            (@"Misc\notes.txt", Bytes("notes")));
        var target = Path.Combine(workspace.OutputRoot, "package.zip");

        var result = new ArchiveService().CreateArchive(session, target);

        Assert.IsTrue(result.Succeeded, result.Failure?.Message);
        AssertArchiveBytes(target, @"models/a.mdl", binary);
        AssertArchiveBytes(target, @"materials/a.vmt", Bytes("material"));
        AssertArchiveBytes(target, @"Misc/notes.txt", Bytes("notes"));
        CollectionAssert.AreEqual(
            session.Files.Select(file => file.DestinationRelativePath.Replace('\\', '/')).Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            ReadArchiveEntries(target));
    }

    [TestMethod]
    public void NativeZipSupportsSpacesUnicodeAndDeepPaths()
    {
        using var workspace = ArchiveWorkspace.Create("app unicode 椅子");
        var deepRelative = Path.Combine(
            "Extras",
            "深い folder",
            string.Join(Path.DirectorySeparatorChar, Enumerable.Range(0, 12).Select(index => "level " + index)),
            "file ü.txt");
        var session = workspace.CreateSession((deepRelative, Bytes("deep")));
        var target = Path.Combine(workspace.OutputRoot, "release 深い 椅子.zip");

        var result = new ArchiveService().CreateArchive(session, target);

        Assert.IsTrue(result.Succeeded, result.Failure?.Message);
        AssertArchiveEntries(target, deepRelative.Replace('\\', '/'));
        AssertArchiveBytes(target, deepRelative.Replace('\\', '/'), Bytes("deep"));
    }

    [TestMethod]
    public void ExistingOutputRequiresExplicitDecisionAndCancelOrChooseAnotherNameDoNotMutate()
    {
        using var workspace = ArchiveWorkspace.Create();
        var session = workspace.CreateSession((@"models\a.mdl", Bytes("model")));
        var target = Path.Combine(workspace.OutputRoot, "existing.zip");
        File.WriteAllBytes(target, Bytes("original archive"));
        var before = File.ReadAllBytes(target);

        var none = new ArchiveService().CreateArchive(session, target);
        var cancel = new ArchiveService().CreateArchive(session, target, ExistingOutputDecision.Cancel);
        var choose = new ArchiveService().CreateArchive(session, target, ExistingOutputDecision.ChooseAnotherName);

        Assert.AreEqual(ArchiveFailureKind.ExistingOutputRequiresDecision, none.Failure!.Kind);
        Assert.AreEqual(ArchiveFailureKind.ExistingOutputCancelled, cancel.Failure!.Kind);
        Assert.AreEqual(ArchiveFailureKind.ChooseAnotherNameRequiresDifferentTarget, choose.Failure!.Kind);
        CollectionAssert.AreEqual(before, File.ReadAllBytes(target));
    }

    [TestMethod]
    public void ReplaceUsesVerifiedTemporaryArchiveAndFailedCreationLeavesOriginalIntact()
    {
        using var workspace = ArchiveWorkspace.Create();
        var session = workspace.CreateSession((@"models\a.mdl", Bytes("replacement")));
        var target = Path.Combine(workspace.OutputRoot, "existing.zip");
        CreateZip(target, ("old.txt", Bytes("old")));
        var before = File.ReadAllBytes(target);

        var failed = new ArchiveService(new ThrowingZipArchiveWriter()).CreateArchive(session, target, ExistingOutputDecision.Replace);

        Assert.IsFalse(failed.Succeeded);
        Assert.AreEqual(ArchiveFailureKind.ZipCreationFailed, failed.Failure!.Kind);
        CollectionAssert.AreEqual(before, File.ReadAllBytes(target));
        AssertNoTemporaryArchives(workspace.OutputRoot);

        var succeeded = new ArchiveService().CreateArchive(session, target, ExistingOutputDecision.Replace);

        Assert.IsTrue(succeeded.Succeeded, succeeded.Failure?.Message);
        Assert.AreEqual(ArchiveReplacementBehavior.ReplacedExisting, succeeded.ReplacementBehavior);
        AssertArchiveEntries(target, @"models/a.mdl");
        AssertArchiveBytes(target, @"models/a.mdl", Bytes("replacement"));
    }

    [TestMethod]
    public void FailedVerificationLeavesStagingAndExistingArchiveIntactAndCleansTemporaryArchive()
    {
        using var workspace = ArchiveWorkspace.Create();
        var session = workspace.CreateSession((@"models\a.mdl", Bytes("model")));
        var target = Path.Combine(workspace.OutputRoot, "existing.zip");
        CreateZip(target, ("old.txt", Bytes("old")));
        var before = File.ReadAllBytes(target);

        var result = new ArchiveService(new WrappedZipArchiveWriter()).CreateArchive(session, target, ExistingOutputDecision.Replace);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(ArchiveFailureKind.ArchiveVerificationFailed, result.Failure!.Kind);
        Assert.IsTrue(Directory.Exists(session.StagingRoot));
        Assert.IsTrue(File.Exists(Path.Combine(session.StagingRoot, @"models\a.mdl")));
        CollectionAssert.AreEqual(before, File.ReadAllBytes(target));
        AssertNoTemporaryArchives(workspace.OutputRoot);
    }

    [TestMethod]
    public void LockedExistingArchiveReplacementFailureLeavesOriginalByteIdentical()
    {
        using var workspace = ArchiveWorkspace.Create();
        var session = workspace.CreateSession((@"models\a.mdl", Bytes("replacement")));
        var target = Path.Combine(workspace.OutputRoot, "existing.zip");
        CreateZip(target, ("old.txt", Bytes("old")));
        var before = File.ReadAllBytes(target);

        using var lockStream = new FileStream(target, FileMode.Open, FileAccess.Read, FileShare.None);
        var result = new ArchiveService().CreateArchive(session, target, ExistingOutputDecision.Replace);

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(ArchiveFailureKind.ReplaceFailed, result.Failure!.Kind);
        StringAssert.Contains(result.Failure.Message, "couldn't be replaced");
        lockStream.Dispose();
        CollectionAssert.AreEqual(before, File.ReadAllBytes(target));
        AssertNoTemporaryArchives(workspace.OutputRoot);
    }

    [TestMethod]
    public void FailureCasesReturnStructuredFailuresAndRetainStaging()
    {
        using var workspace = ArchiveWorkspace.Create();
        var emptySession = workspace.CreateEmptySession();
        var missingRootSession = workspace.CreateSession((@"models\a.mdl", Bytes("model")));
        Directory.Delete(missingRootSession.StagingRoot, recursive: true);
        var validSession = workspace.CreateSession((@"models\b.mdl", Bytes("model")));

        var empty = new ArchiveService().CreateArchive(emptySession, Path.Combine(workspace.OutputRoot, "empty.zip"));
        var missingRoot = new ArchiveService().CreateArchive(missingRootSession, Path.Combine(workspace.OutputRoot, "missing-root.zip"));
        var missingArchive = new ArchiveService(new NoFileZipArchiveWriter()).CreateArchive(validSession, Path.Combine(workspace.OutputRoot, "missing.zip"));
        var corrupt = new ArchiveService(new CorruptZipArchiveWriter()).CreateArchive(validSession, Path.Combine(workspace.OutputRoot, "corrupt.zip"));

        Assert.AreEqual(ArchiveFailureKind.EmptyStagingSession, empty.Failure!.Kind);
        Assert.AreEqual(ArchiveFailureKind.StagingRootMissing, missingRoot.Failure!.Kind);
        Assert.AreEqual(ArchiveFailureKind.ArchiveMissing, missingArchive.Failure!.Kind);
        Assert.AreEqual(ArchiveFailureKind.ArchiveVerificationFailed, corrupt.Failure!.Kind);
        Assert.IsTrue(Directory.Exists(validSession.StagingRoot));
        Assert.IsTrue(File.Exists(Path.Combine(validSession.StagingRoot, @"models\b.mdl")));
        Assert.IsFalse(File.Exists(Path.Combine(workspace.OutputRoot, "corrupt.zip")));
    }

    [TestMethod]
    public void VerifierDetectsMissingUnexpectedWrapperCorruptAndByteMismatchArchives()
    {
        using var workspace = ArchiveWorkspace.Create();
        var session = workspace.CreateSession((@"models\a.mdl", Bytes("model")));
        var verifier = new ArchiveVerifier();

        var missingEntryArchive = Path.Combine(workspace.OutputRoot, "missing-entry.zip");
        CreateZip(missingEntryArchive, ("README.txt", Bytes("readme")));
        var unexpectedArchive = Path.Combine(workspace.OutputRoot, "unexpected.zip");
        CreateZip(unexpectedArchive, (@"models/a.mdl", Bytes("model")), ("extra.txt", Bytes("extra")));
        var wrappedArchive = Path.Combine(workspace.OutputRoot, "wrapped.zip");
        CreateZip(wrappedArchive, ($"{session.SessionId:N}/models/a.mdl", Bytes("model")));
        var corruptArchive = Path.Combine(workspace.OutputRoot, "corrupt.zip");
        File.WriteAllText(corruptArchive, "not zip", Encoding.UTF8);
        var byteMismatchArchive = Path.Combine(workspace.OutputRoot, "bytes.zip");
        CreateZip(byteMismatchArchive, (@"models/a.mdl", Bytes("different")));

        Assert.IsFalse(verifier.Verify(missingEntryArchive, session).Succeeded);
        Assert.IsFalse(verifier.Verify(unexpectedArchive, session).Succeeded);
        Assert.IsFalse(verifier.Verify(wrappedArchive, session).Succeeded);
        Assert.IsFalse(verifier.Verify(corruptArchive, session).Succeeded);
        Assert.IsFalse(verifier.Verify(byteMismatchArchive, session).Succeeded);
    }

    [TestMethod]
    public void SuccessfulAndFailedArchiveDoNotChangeStagingSourceOrUnrelatedOutputFiles()
    {
        using var workspace = ArchiveWorkspace.Create();
        var sourcePath = Path.Combine(workspace.Root, "source", "a.mdl");
        Directory.CreateDirectory(Path.GetDirectoryName(sourcePath)!);
        File.WriteAllBytes(sourcePath, Bytes("source"));
        var session = workspace.CreateSession((@"models\a.mdl", Bytes("model"), sourcePath));
        var unrelated = Path.Combine(workspace.OutputRoot, "keep.txt");
        File.WriteAllText(unrelated, "keep", Encoding.UTF8);
        var beforeStaging = workspace.StagingSnapshot(session);
        var beforeSource = File.ReadAllBytes(sourcePath);
        var beforeUnrelated = File.ReadAllBytes(unrelated);

        var success = new ArchiveService().CreateArchive(session, Path.Combine(workspace.OutputRoot, "success.zip"));
        var failure = new ArchiveService(new ThrowingZipArchiveWriter()).CreateArchive(session, Path.Combine(workspace.OutputRoot, "failure.zip"));

        Assert.IsTrue(success.Succeeded, success.Failure?.Message);
        Assert.IsFalse(failure.Succeeded);
        workspace.AssertStagingUnchanged(session, beforeStaging);
        CollectionAssert.AreEqual(beforeSource, File.ReadAllBytes(sourcePath));
        CollectionAssert.AreEqual(beforeUnrelated, File.ReadAllBytes(unrelated));
    }

    private static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);

    private static void AssertNoTemporaryArchives(string outputRoot)
    {
        Assert.IsFalse(Directory.EnumerateFiles(outputRoot, "*.tmp.zip").Any());
        Assert.IsFalse(Directory.EnumerateFiles(outputRoot, ".*.sfmpack-*.tmp.zip").Any());
    }

    private static void AssertArchiveEntries(string archivePath, params string[] expected)
    {
        CollectionAssert.AreEqual(
            expected.Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            ReadArchiveEntries(archivePath));
    }

    private static void AssertArchiveBytes(string archivePath, string entryName, byte[] expectedBytes)
    {
        using var archive = ZipFile.OpenRead(archivePath);
        var entry = archive.Entries.Single(item => string.Equals(item.FullName.Replace('\\', '/'), entryName, StringComparison.OrdinalIgnoreCase));
        using var stream = entry.Open();
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        CollectionAssert.AreEqual(expectedBytes, memory.ToArray());
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

    private static void CreateZip(string archivePath, params (string Path, byte[] Contents)[] files)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(archivePath)!);
        using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create);
        foreach (var file in files)
        {
            var entry = archive.CreateEntry(file.Path.Replace('\\', '/'));
            using var stream = entry.Open();
            stream.Write(file.Contents);
        }
    }

    private sealed class ThrowingZipArchiveWriter : IZipArchiveWriter
    {
        public void CreateFromStaging(StagingSession session, string archivePath) =>
            throw new IOException("Simulated ZIP write failure.");
    }

    private sealed class RecordingZipArchiveWriter : IZipArchiveWriter
    {
        public string? WrittenPath { get; private set; }

        public void CreateFromStaging(StagingSession session, string archivePath)
        {
            WrittenPath = archivePath;
            using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create);
            foreach (var file in session.Files)
            {
                var relative = Path.GetRelativePath(session.StagingRoot, file.StagedPath).Replace('\\', '/');
                archive.CreateEntryFromFile(file.StagedPath, relative);
            }
        }
    }

    private sealed class NoFileZipArchiveWriter : IZipArchiveWriter
    {
        public void CreateFromStaging(StagingSession session, string archivePath)
        {
        }
    }

    private sealed class CorruptZipArchiveWriter : IZipArchiveWriter
    {
        public void CreateFromStaging(StagingSession session, string archivePath)
        {
            File.WriteAllText(archivePath, "not a zip", Encoding.UTF8);
        }
    }

    private sealed class WrappedZipArchiveWriter : IZipArchiveWriter
    {
        public void CreateFromStaging(StagingSession session, string archivePath)
        {
            using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create);
            foreach (var file in session.Files)
            {
                var relative = Path.GetRelativePath(session.StagingRoot, file.StagedPath).Replace('\\', '/');
                archive.CreateEntryFromFile(file.StagedPath, $"{session.SessionId:N}/{relative}");
            }
        }
    }

    private sealed class ArchiveWorkspace : IDisposable
    {
        private ArchiveWorkspace(string root)
        {
            Root = root;
            ApplicationRoot = Path.Combine(root, "app");
            OutputRoot = Path.Combine(root, "output");
            Directory.CreateDirectory(ApplicationRoot);
            Directory.CreateDirectory(OutputRoot);
        }

        public string Root { get; }

        public string ApplicationRoot { get; }

        public string OutputRoot { get; }

        public static ArchiveWorkspace Create(string appFolderName = "app") =>
            new(Path.Combine(Path.GetTempPath(), "SfmPackageBuilder.Archive.Tests", Guid.NewGuid().ToString("N"), appFolderName));

        public StagingSession CreateEmptySession()
        {
            var sessionId = Guid.NewGuid();
            var stagingRoot = Path.Combine(ApplicationRoot, ".staging", sessionId.ToString("N"));
            Directory.CreateDirectory(stagingRoot);
            return new StagingSession(sessionId, ApplicationRoot, stagingRoot, Array.Empty<StagedPackageFile>(), Array.Empty<StagingDuplicateWrite>());
        }

        public StagingSession CreateSession(params (string RelativePath, byte[] Contents)[] files) =>
            CreateSession(files.Select(file => (file.RelativePath, file.Contents, SourcePath: (string?)null)).ToArray());

        public StagingSession CreateSession(params (string RelativePath, byte[] Contents, string? SourcePath)[] files)
        {
            var sessionId = Guid.NewGuid();
            var stagingRoot = Path.Combine(ApplicationRoot, ".staging", sessionId.ToString("N"));
            Directory.CreateDirectory(stagingRoot);
            var stagedFiles = new List<StagedPackageFile>();

            foreach (var file in files)
            {
                var path = Path.Combine(stagingRoot, file.RelativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, file.Contents);
                stagedFiles.Add(new StagedPackageFile(
                    file.RelativePath,
                    file.RelativePath,
                    path,
                    StagingWriteKind.SourceCopy,
                    file.SourcePath,
                    new FileInfo(path).Length));
            }

            return new StagingSession(sessionId, ApplicationRoot, stagingRoot, stagedFiles, Array.Empty<StagingDuplicateWrite>());
        }

        public IReadOnlyList<(string RelativePath, byte[] Bytes, FileAttributes Attributes)> StagingSnapshot(StagingSession session) =>
            Directory.EnumerateFiles(session.StagingRoot, "*", SearchOption.AllDirectories)
                .Select(path => (Path.GetRelativePath(session.StagingRoot, path), File.ReadAllBytes(path), File.GetAttributes(path)))
                .OrderBy(item => item.Item1, StringComparer.OrdinalIgnoreCase)
                .ToArray();

        public void AssertStagingUnchanged(StagingSession session, IReadOnlyList<(string RelativePath, byte[] Bytes, FileAttributes Attributes)> snapshot)
        {
            var current = StagingSnapshot(session);
            Assert.AreEqual(snapshot.Count, current.Count);
            for (var i = 0; i < snapshot.Count; i++)
            {
                Assert.AreEqual(snapshot[i].RelativePath, current[i].RelativePath);
                CollectionAssert.AreEqual(snapshot[i].Bytes, current[i].Bytes);
                Assert.AreEqual(snapshot[i].Attributes, current[i].Attributes);
            }
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
