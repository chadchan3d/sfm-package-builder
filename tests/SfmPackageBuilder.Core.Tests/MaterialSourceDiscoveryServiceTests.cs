using Microsoft.VisualStudio.TestTools.UnitTesting;
using SfmPackageBuilder.Core.Discovery;
using SfmPackageBuilder.Core.Domain;
using SfmPackageBuilder.Core.Expansion;
using SfmPackageBuilder.Core.FileSystem;
using SfmPackageBuilder.Core.Mdl;
using SfmPackageBuilder.Core.Planning;
using SfmPackageBuilder.Core.Readme;
using SfmPackageBuilder.Core.SharedFiles;
using SfmPackageBuilder.Core.Validation;

namespace SfmPackageBuilder.Core.Tests;

[TestClass]
public sealed class MaterialSourceDiscoveryServiceTests
{
    [TestMethod]
    public void ModelBeneathModelsRootDerivesContentRoot()
    {
        var result = Discover(
            @"X:\SFM\game\usermod\models\foo\bar.mdl",
            null,
            Metadata(@"models\foo\"));

        CollectionAssert.AreEqual(new[] { @"X:\SFM\game\usermod" }, result.ContentRoots.ToArray());
    }

    [TestMethod]
    public void ModelsSegmentIsCaseInsensitive()
    {
        var result = Discover(
            @"X:\SFM\game\USERMOD\MODELS\foo\bar.mdl",
            null,
            Metadata(@"models\foo\"));

        CollectionAssert.AreEqual(new[] { @"X:\SFM\game\USERMOD" }, result.ContentRoots.ToArray());
    }

    [TestMethod]
    public void MisleadingSubstringDoesNotInventModelDerivedRoot()
    {
        var result = Discover(
            @"X:\my_models_backup\foo.mdl",
            null,
            Metadata(@"models\foo\"));

        Assert.AreEqual(0, result.ContentRoots.Count);
    }

    [TestMethod]
    public void NoModelsSegmentDoesNotInventModelDerivedRootButConfiguredRootMayParticipate()
    {
        var fileSystem = new FakeFileSystem();
        fileSystem.AddDirectory(@"Y:\SFM\game\usermod");

        var result = Discover(
            fileSystem,
            @"X:\loose\foo.mdl",
            @"Y:\SFM\game\usermod",
            Metadata(@"models\foo\"));

        CollectionAssert.AreEqual(new[] { @"Y:\SFM\game\usermod" }, result.ContentRoots.ToArray());
    }

    [TestMethod]
    public void OneSearchPathWithExistingCandidateDiscoversOneMaterialFolder()
    {
        var fileSystem = new FakeFileSystem();
        fileSystem.AddDirectory(@"X:\SFM\game\usermod\materials\models\foo");

        var result = Discover(
            fileSystem,
            @"X:\SFM\game\usermod\models\foo\bar.mdl",
            null,
            Metadata(@"models\foo\"));

        CollectionAssert.AreEqual(new[] { @"X:\SFM\game\usermod\materials\models\foo" }, result.MaterialSources.Select(source => source.SourcePath).ToArray());
        AssertMaterialFolder(result.MaterialSources[0]);
    }

    [TestMethod]
    public void MultipleSearchPathsDiscoverMultipleExistingMaterialFolders()
    {
        var fileSystem = new FakeFileSystem();
        fileSystem.AddDirectory(@"X:\SFM\game\usermod\materials\models\character\milo");
        fileSystem.AddDirectory(@"X:\SFM\game\usermod\materials\models\shared\eyes");
        fileSystem.AddDirectory(@"X:\SFM\game\usermod\materials\models\shared\clothing");

        var result = Discover(
            fileSystem,
            @"X:\SFM\game\usermod\models\character\milo\milo.mdl",
            null,
            Metadata(@"models\character\milo\", @"models\shared\eyes\", @"models\shared\clothing\"));

        CollectionAssert.AreEqual(
            new[]
            {
                @"X:\SFM\game\usermod\materials\models\character\milo",
                @"X:\SFM\game\usermod\materials\models\shared\clothing",
                @"X:\SFM\game\usermod\materials\models\shared\eyes"
            },
            result.MaterialSources.Select(source => source.SourcePath).ToArray());
    }

    [TestMethod]
    public void MissingCandidateIsNotAdded()
    {
        var result = Discover(
            @"X:\SFM\game\usermod\models\foo\bar.mdl",
            null,
            Metadata(@"models\foo\"));

        Assert.AreEqual(0, result.MaterialSources.Count);
    }

    [TestMethod]
    public void DuplicateSearchPathsProduceOneMaterialSource()
    {
        var fileSystem = new FakeFileSystem();
        fileSystem.AddDirectory(@"X:\SFM\game\usermod\materials\models\foo");

        var result = Discover(
            fileSystem,
            @"X:\SFM\game\usermod\models\foo\bar.mdl",
            null,
            Metadata(@"models\foo\", @"MODELS/FOO/", @"materials\models\foo\"));

        CollectionAssert.AreEqual(new[] { @"X:\SFM\game\usermod\materials\models\foo" }, result.MaterialSources.Select(source => source.SourcePath).ToArray());
    }

    [TestMethod]
    public void EmptyAndSpecificSearchPathsDoNotAddBroadMaterialRoots()
    {
        var fileSystem = new FakeFileSystem();
        fileSystem.AddDirectory(@"X:\SFM\game\usermod\materials");
        fileSystem.AddDirectory(@"X:\SFM\game\usermod\materials\models");
        fileSystem.AddDirectory(@"X:\SFM\game\usermod\materials\models\AnnoAD\foxbase\mia");

        var result = Discover(
            fileSystem,
            @"X:\SFM\game\usermod\models\AnnoAD\foxbase\mia\mia.mdl",
            null,
            Metadata(string.Empty, @"models\AnnoAD\foxbase\mia\"));

        CollectionAssert.AreEqual(
            new[] { @"X:\SFM\game\usermod\materials\models\AnnoAD\foxbase\mia" },
            result.MaterialSources.Select(source => source.SourcePath).ToArray());
        Assert.IsFalse(result.MaterialSources.Any(source => string.Equals(source.SourcePath, @"X:\SFM\game\usermod\materials", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(result.MaterialSources.Any(source => string.Equals(source.SourcePath, @"X:\SFM\game\usermod\materials\models", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(".")]
    [DataRow("\\")]
    [DataRow("/")]
    [DataRow("models")]
    [DataRow("models\\")]
    [DataRow("materials")]
    [DataRow("materials\\models\\")]
    public void BroadSearchPathsDoNotCreateUnsafeAutomaticRecursiveMaterialSource(string searchPath)
    {
        var fileSystem = new FakeFileSystem();
        fileSystem.AddDirectory(@"X:\SFM\game\usermod\materials");
        fileSystem.AddDirectory(@"X:\SFM\game\usermod\materials\models");

        var result = Discover(
            fileSystem,
            @"X:\SFM\game\usermod\models\foo\bar.mdl",
            null,
            Metadata(searchPath));

        Assert.AreEqual(0, result.MaterialSources.Count);
    }

    [TestMethod]
    public void SpecificModelMaterialPathStillDiscoversNormally()
    {
        var fileSystem = new FakeFileSystem();
        fileSystem.AddDirectory(@"X:\SFM\game\usermod\materials\models\character\mia");

        var result = Discover(
            fileSystem,
            @"X:\SFM\game\usermod\models\character\mia\mia.mdl",
            null,
            Metadata(@"models\character\mia\"));

        CollectionAssert.AreEqual(
            new[] { @"X:\SFM\game\usermod\materials\models\character\mia" },
            result.MaterialSources.Select(source => source.SourcePath).ToArray());
    }

    [TestMethod]
    public void DifferentRootsProducingSamePhysicalSourceAreDeduplicated()
    {
        var fileSystem = new FakeFileSystem();
        fileSystem.AddDirectory(@"X:\SFM\game\usermod");
        fileSystem.AddDirectory(@"X:\SFM\game\usermod\materials\models\foo");

        var result = Discover(
            fileSystem,
            @"X:\SFM\game\USERMOD\models\foo\bar.mdl",
            @"X:\SFM\game\usermod",
            Metadata(@"models\foo\"));

        CollectionAssert.AreEqual(new[] { @"X:\SFM\game\USERMOD\materials\models\foo" }, result.MaterialSources.Select(source => source.SourcePath).ToArray());
    }

    [TestMethod]
    public void ValidDistinctRootsPreserveDistinctMaterialSources()
    {
        var fileSystem = new FakeFileSystem();
        fileSystem.AddDirectory(@"X:\SFM\game\usermod\materials\models\foo");
        fileSystem.AddDirectory(@"Y:\OtherGame\game\usermod");
        fileSystem.AddDirectory(@"Y:\OtherGame\game\usermod\materials\models\foo");

        var result = Discover(
            fileSystem,
            @"X:\SFM\game\usermod\models\foo\bar.mdl",
            @"Y:\OtherGame\game\usermod",
            Metadata(@"models\foo\"));

        CollectionAssert.AreEqual(
            new[]
            {
                @"X:\SFM\game\usermod\materials\models\foo",
                @"Y:\OtherGame\game\usermod\materials\models\foo"
            },
            result.MaterialSources.Select(source => source.SourcePath).ToArray());
    }

    [TestMethod]
    public void UnsupportedMetadataDoesNotInventMaterialSources()
    {
        var fileSystem = new FakeFileSystem();
        fileSystem.AddDirectory(@"X:\SFM\game\usermod\materials\models\foo");

        var result = Discover(
            fileSystem,
            @"X:\SFM\game\usermod\models\foo\bar.mdl",
            null,
            new MdlMetadataReadResult(MdlMetadataReadStatus.UnsupportedFormatOrVersion, null, Array.Empty<MdlMetadataDiagnostic>()));

        Assert.AreEqual(MdlMetadataReadStatus.UnsupportedFormatOrVersion, result.MetadataStatus);
        Assert.AreEqual(0, result.MaterialSources.Count);
    }

    [TestMethod]
    public void DiscoveredFolderParticipatesInNormalPackagePlanAndValidation()
    {
        var fileSystem = new FakeFileSystem();
        var metadata = Metadata(new[] { "body" }, new[] { @"models\creator\a\" });
        var modelPath = @"X:\SFM\game\usermod\models\creator\a\a.mdl";
        var materialPath = @"X:\SFM\game\usermod\materials\models\creator\a";
        fileSystem.AddFile(modelPath, Array.Empty<byte>());
        fileSystem.AddFile(Path.Combine(materialPath, "body.vmt"), Array.Empty<byte>());

        var project = new PackageProject
        {
            AssetName = "A",
            CurrentVersion = "1.0",
            ArchiveName = "a.zip",
            Readme = new ReadmeConfig { Mode = ReadmeMode.None },
            Models =
            {
                new ModelEntry
                {
                    Role = ModelRole.Primary,
                    SourceMdlPath = modelPath,
                    SourceStem = "a",
                    ReleaseStem = "a"
                }
            }
        };
        project.MaterialSources.AddRange(new MaterialSourceDiscoveryService(fileSystem, new FakeReader(metadata)).Discover(modelPath, null).MaterialSources);

        var plan = new PackagePlanner(
            new SourceExpansionService(fileSystem),
            new SharedFileRegistry(),
            new ReadmeResolver(fileSystem),
            new FakeReader(metadata))
            .CreatePlan(project);
        var validation = new PackageValidator(fileSystem, new FakeOutputEnvironment())
            .Validate(project, plan, new PackageValidationContext { OutputDirectory = @"C:\out" });

        Assert.IsTrue(plan.Entries.Any(entry => entry.SourcePath == Path.Combine(materialPath, "body.vmt")));
        Assert.IsFalse(validation.Messages.Any(message => message.Code == "mdl-material-referenced-not-packaged"));
        Assert.IsFalse(validation.Messages.Any(message => message.Code == "mdl-material-possibly-misplaced"));
    }

    private static MaterialSourceDiscoveryResult Discover(string modelPath, string? configuredRoot, MdlMetadataReadResult metadata) =>
        Discover(new FakeFileSystem(), modelPath, configuredRoot, metadata);

    private static MaterialSourceDiscoveryResult Discover(IFileSystem fileSystem, string modelPath, string? configuredRoot, MdlMetadataReadResult metadata) =>
        new MaterialSourceDiscoveryService(fileSystem, new FakeReader(metadata)).Discover(modelPath, configuredRoot);

    private static MdlMetadataReadResult Metadata(params string[] searchPaths) =>
        Metadata(Array.Empty<string>(), searchPaths);

    private static MdlMetadataReadResult Metadata(string[] textures, string[] searchPaths) =>
        new(
            MdlMetadataReadStatus.Available,
            new MdlV49Metadata("IDST", 49, 1, "model.mdl", 4096, textures, searchPaths, null),
            Array.Empty<MdlMetadataDiagnostic>());

    private static void AssertMaterialFolder(SourceEntry source)
    {
        Assert.AreEqual(SourceEntryKind.Material, source.Kind);
        Assert.IsTrue(source.IsFolder);
        Assert.IsTrue(source.IncludeRecursively);
        Assert.IsNotNull(source.SourceReference);
        Assert.AreEqual(source.SourcePath, source.SourceReference!.AbsolutePath);
        Assert.AreEqual(@"X:\SFM\game\usermod", source.SourceReference.RecoveryRootPath);
        Assert.AreEqual(@"materials\models\foo", source.SourceReference.RecoveryRelativePath);
    }

    private sealed class FakeReader : IMdlMetadataReader
    {
        private readonly MdlMetadataReadResult result;

        public FakeReader(MdlMetadataReadResult result)
        {
            this.result = result;
        }

        public MdlMetadataReadResult Read(string path) => result;
    }
}
