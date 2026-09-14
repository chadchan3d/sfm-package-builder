using System.Buffers.Binary;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SfmPackageBuilder.Core.Archive;
using SfmPackageBuilder.Core.Build;
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
public sealed class MdlMaterialPlanningValidationTests
{
    private const string OutputDirectory = @"C:\out";

    [TestMethod]
    public void PrimaryAndAdditionalModelMetadataBecomePlanOwnedFacts()
    {
        var fixture = new Fixture();
        var primary = fixture.AddModel(@"C:\sfm\game\usermod\models\creator\a\a.mdl", "a", "a", ModelRole.Primary, Mdl("a.mdl", new[] { "body" }, new[] { @"models\shared\" }));
        var additional = fixture.AddModel(@"C:\sfm\game\usermod\models\creator\b\b.mdl", "b", "b", ModelRole.Additional, Mdl("b.mdl", new[] { "eyes" }, new[] { @"models\shared\" }));

        var plan = fixture.Plan();

        Assert.AreEqual(2, plan.ModelMaterialFacts.Count);
        Assert.IsTrue(plan.ModelMaterialFacts.Any(facts => facts.ModelEntryId == primary.Id && facts.ModelRole == ModelRole.Primary && facts.TextureReferences.Contains("body")));
        Assert.IsTrue(plan.ModelMaterialFacts.Any(facts => facts.ModelEntryId == additional.Id && facts.ModelRole == ModelRole.Additional && facts.TextureReferences.Contains("eyes")));
    }

    [TestMethod]
    public void ValidatorConsumesPlanFactsWithoutInvokingMdlReader()
    {
        var fixture = new Fixture();
        fixture.AddModel(@"C:\sfm\game\usermod\models\creator\a\a.mdl", "a", "a", ModelRole.Primary, Array.Empty<byte>());
        fixture.AddMaterial(@"C:\sfm\game\usermod\materials\models\shared\body.vmt", "vmt");
        var reader = new CountingReader(new MdlMetadataReadResult(
            MdlMetadataReadStatus.Available,
            new MdlV49Metadata("IDST", 49, 7, "a.mdl", 4096, new[] { "body" }, new[] { @"models\shared\" }, Skin(new short[][] { new short[] { 0 } })),
            Array.Empty<MdlMetadataDiagnostic>()));
        var plan = fixture.Plan(reader);

        Assert.AreEqual(1, reader.ReadCount);
        _ = fixture.Validate(plan);

        Assert.AreEqual(1, reader.ReadCount);
    }

    [TestMethod]
    public void UnsupportedAndCorruptMetadataDoNotPreventOrdinaryPlanningOrBuildReadiness()
    {
        var unsupported = new Fixture();
        unsupported.AddModel(@"C:\sfm\game\usermod\models\creator\a\a.mdl", "a", "a", ModelRole.Primary, new SyntheticMdl().WithVersion(48).ToArray());
        var unsupportedResult = unsupported.Validate(unsupported.Plan());

        Assert.IsFalse(unsupportedResult.HasErrors);
        Assert.IsTrue(unsupportedResult.CanBuild);
        var unsupportedMessage = unsupportedResult.Messages.Single(message => message.Code == "mdl-material-inspection-unavailable");
        Assert.AreEqual(ValidationSeverity.Information, unsupportedMessage.Severity);
        Assert.AreEqual("This model's materials could not be checked automatically.", unsupportedMessage.Message);
        Assert.AreEqual("Add material folders manually if this model uses custom materials.", unsupportedMessage.Detail);
        Assert.IsFalse(unsupportedMessage.Message.Contains("Model-aware material checks", StringComparison.Ordinal));
        Assert.IsFalse(unsupportedResult.Messages.Any(message => message.Severity == ValidationSeverity.Warning));

        var corrupt = new Fixture();
        corrupt.AddModel(@"C:\sfm\game\usermod\models\creator\a\a.mdl", "a", "a", ModelRole.Primary, Mdl("a.mdl", new[] { "body" }, new[] { @"models\shared\" }).WithTextureTable(1, 4090).ToArray());
        var corruptResult = corrupt.Validate(corrupt.Plan());

        Assert.IsFalse(corruptResult.HasErrors);
        Assert.IsTrue(corruptResult.CanBuild);
        Assert.IsTrue(corruptResult.Messages.Any(message => message.Severity == ValidationSeverity.Warning && message.Code == "mdl-material-inspection-partial"));
    }

    [TestMethod]
    public void NoPackagedMaterialsProducesAutomaticMaterialDiscoveryNote()
    {
        var fixture = new Fixture();
        fixture.AddModel(@"C:\sfm\game\usermod\models\creator\a\a.mdl", "a", "a", ModelRole.Primary, Mdl("a.mdl", Array.Empty<string>(), Array.Empty<string>()));

        var validation = fixture.Validate(fixture.Plan());

        var note = validation.Messages.Single(message => message.Code == "mdl-material-none-auto-discovered");
        Assert.AreEqual(ValidationSeverity.Information, note.Severity);
        Assert.AreEqual("No materials were found automatically for this model.", note.Message);
        Assert.AreEqual("If the model uses custom materials, add their folder or files on Model & Materials.", note.Detail);
        Assert.IsTrue(validation.CanBuild);
    }

    [TestMethod]
    public void CorrectMaterialPlacementCoversSearchPathsCaseSeparatorsRelativeReferencesAndExtension()
    {
        var onePath = new Fixture();
        onePath.AddModel(@"C:\sfm\game\usermod\models\creator\a\a.mdl", "a", "a", ModelRole.Primary, Mdl("a.mdl", new[] { "body" }, new[] { @"models\creator\a\" }));
        onePath.AddMaterial(@"C:\sfm\game\usermod\materials\models\creator\a\body.vmt", "vmt");
        AssertNoMaterialWarning(onePath.Validate(onePath.Plan()));

        var secondPath = new Fixture();
        secondPath.AddModel(@"C:\sfm\game\usermod\models\creator\a\a.mdl", "a", "a", ModelRole.Primary, Mdl("a.mdl", new[] { "body" }, new[] { @"models\other\", @"models\creator\a\" }));
        secondPath.AddMaterial(@"C:\sfm\game\usermod\materials\models\creator\a\body.vmt", "vmt");
        AssertNoMaterialWarning(secondPath.Validate(secondPath.Plan()));

        var caseAndSeparators = new Fixture();
        caseAndSeparators.AddModel(@"C:\sfm\game\usermod\models\creator\a\a.mdl", "a", "a", ModelRole.Primary, Mdl("a.mdl", new[] { "BODY" }, new[] { @"/MODELS/CREATOR/A/" }));
        caseAndSeparators.AddMaterial(@"C:\sfm\game\usermod\materials\models\creator\a\body.vmt", "vmt");
        AssertNoMaterialWarning(caseAndSeparators.Validate(caseAndSeparators.Plan()));

        var relativePath = new Fixture();
        relativePath.AddModel(@"C:\sfm\game\usermod\models\creator\a\a.mdl", "a", "a", ModelRole.Primary, Mdl("a.mdl", new[] { @"clothes/body" }, new[] { @"models\creator\a\" }));
        relativePath.AddMaterial(@"C:\sfm\game\usermod\materials\models\creator\a\clothes\body.vmt", "vmt");
        AssertNoMaterialWarning(relativePath.Validate(relativePath.Plan()));

        var extension = new Fixture();
        extension.AddModel(@"C:\sfm\game\usermod\models\creator\a\a.mdl", "a", "a", ModelRole.Primary, Mdl("a.mdl", new[] { "body.vmt" }, new[] { @"models\creator\a\" }));
        extension.AddMaterial(@"C:\sfm\game\usermod\materials\models\creator\a\body.vmt", "vmt");
        AssertNoMaterialWarning(extension.Validate(extension.Plan()));
    }

    [TestMethod]
    public void ReferencedMaterialAbsentProducesGroupedWarningThatAllowsBuild()
    {
        var fixture = new Fixture();
        fixture.AddModel(@"C:\sfm\game\usermod\models\creator\a\a.mdl", "a", "a", ModelRole.Primary, Mdl("a.mdl", new[] { "body", "eyes", "tail" }, new[] { @"models\creator\a\" }));

        var validation = fixture.Validate(fixture.Plan());

        var warning = validation.Messages.Single(message => message.Code == "mdl-material-referenced-not-packaged");
        Assert.AreEqual(ValidationSeverity.Warning, warning.Severity);
        Assert.AreEqual("Some materials used by this model are not included.", warning.Message);
        Assert.AreEqual(
            "Package Builder did not find those materials automatically. If they are custom materials, add their folder or files on Model & Materials.",
            warning.Detail);
        CollectionAssert.AreEquivalent(new[] { "body", "eyes", "tail" }, warning.AffectedValues.ToArray());
        Assert.IsTrue(validation.CanBuild);
        Assert.AreEqual(1, validation.Messages.Count(message => message.Code == "mdl-material-referenced-not-packaged"));
        Assert.IsFalse(validation.Messages.Any(message => message.Code == "mdl-material-none-auto-discovered"));
    }

    [TestMethod]
    public void NoPackagedMaterialsNoteIsSuppressedOnlyForModelWithReferencedMaterialWarning()
    {
        var fixture = new Fixture();
        var modelWithoutReferences = fixture.AddModel(
            @"C:\sfm\game\usermod\models\creator\plain\plain.mdl",
            "plain",
            "plain",
            ModelRole.Primary,
            Mdl("plain.mdl", Array.Empty<string>(), Array.Empty<string>()));
        var modelWithMissingReference = fixture.AddModel(
            @"C:\sfm\game\usermod\models\creator\a\a.mdl",
            "a",
            "a",
            ModelRole.Additional,
            Mdl("a.mdl", new[] { "body" }, new[] { @"models\creator\a\" }));

        var validation = fixture.Validate(fixture.Plan());

        var note = validation.Messages.Single(message => message.Code == "mdl-material-none-auto-discovered");
        Assert.AreEqual(ValidationSeverity.Information, note.Severity);
        Assert.AreEqual("No materials were found automatically for this model.", note.Message);
        Assert.AreEqual("If the model uses custom materials, add their folder or files on Model & Materials.", note.Detail);
        CollectionAssert.AreEqual(new[] { modelWithoutReferences.SourceMdlPath }, note.SourcePaths.ToArray());
        var warning = validation.Messages.Single(message => message.Code == "mdl-material-referenced-not-packaged");
        CollectionAssert.AreEqual(new[] { modelWithMissingReference.SourceMdlPath }, warning.SourcePaths.ToArray());
    }

    [TestMethod]
    public void PlacementWarningsAreSpecificOnlyWhenCorrespondenceIsDefensible()
    {
        var misplaced = new Fixture();
        misplaced.AddModel(@"C:\sfm\game\usermod\models\creator\a\a.mdl", "a", "a", ModelRole.Primary, Mdl("a.mdl", new[] { "body" }, new[] { @"models\creator\a\" }));
        misplaced.AddMaterial(@"C:\exports\body.vmt", "vmt", new DestinationOverride { Kind = DestinationOverrideKind.Custom, RelativePath = @"materials\wrong" });
        var misplacedValidation = misplaced.Validate(misplaced.Plan());
        var warning = misplacedValidation.Messages.Single(message => message.Code == "mdl-material-possibly-misplaced");
        Assert.AreEqual(ValidationSeverity.Warning, warning.Severity);
        StringAssert.Contains(warning.Detail!, "Material: body.vmt");
        StringAssert.Contains(warning.Detail!, @"Package location: materials\wrong\body.vmt");

        var valid = new Fixture();
        valid.AddModel(@"C:\sfm\game\usermod\models\creator\a\a.mdl", "a", "a", ModelRole.Primary, Mdl("a.mdl", new[] { "body" }, new[] { @"models\creator\a\" }));
        valid.AddMaterial(@"C:\sfm\game\usermod\materials\models\creator\a\body.vmt", "vmt");
        AssertNoMaterialWarning(valid.Validate(valid.Plan()));

        var ambiguous = new Fixture();
        ambiguous.AddModel(@"C:\sfm\game\usermod\models\creator\a\a.mdl", "a", "a", ModelRole.Primary, Mdl("a.mdl", new[] { "body" }, new[] { @"models\creator\a\" }));
        ambiguous.AddMaterial(@"C:\exports\one\body.vmt", "vmt", new DestinationOverride { Kind = DestinationOverrideKind.Custom, RelativePath = @"materials\wrong1" });
        ambiguous.AddMaterial(@"C:\exports\two\body.vmt", "vmt", new DestinationOverride { Kind = DestinationOverrideKind.Custom, RelativePath = @"materials\wrong2" });
        var ambiguousValidation = ambiguous.Validate(ambiguous.Plan());
        Assert.IsFalse(ambiguousValidation.Messages.Any(message => message.Code == "mdl-material-possibly-misplaced"));
        Assert.IsTrue(ambiguousValidation.Messages.Any(message => message.Code == "mdl-material-referenced-not-packaged"));
    }

    [TestMethod]
    public void SkinFamiliesDriveCoverageIncludingAlternateOnlyMaterialsAndDeduplication()
    {
        var covered = new Fixture();
        covered.AddModel(@"C:\sfm\game\usermod\models\creator\a\a.mdl", "a", "a", ModelRole.Primary, Mdl(
            "a.mdl",
            new[] { "body", "shirt_blue", "shirt_red" },
            new[] { @"models\creator\a\" },
            new short[][] { new short[] { 0, 1 }, new short[] { 0, 2 }, new short[] { 0, 2 } }));
        covered.AddMaterial(@"C:\sfm\game\usermod\materials\models\creator\a\body.vmt", "vmt");
        covered.AddMaterial(@"C:\sfm\game\usermod\materials\models\creator\a\shirt_blue.vmt", "vmt");
        covered.AddMaterial(@"C:\sfm\game\usermod\materials\models\creator\a\shirt_red.vmt", "vmt");
        AssertNoMaterialWarning(covered.Validate(covered.Plan()));

        var absent = new Fixture();
        absent.AddModel(@"C:\sfm\game\usermod\models\creator\a\a.mdl", "a", "a", ModelRole.Primary, Mdl(
            "a.mdl",
            new[] { "body", "shirt_blue", "shirt_red" },
            new[] { @"models\creator\a\" },
            new short[][] { new short[] { 0, 1 }, new short[] { 0, 2 }, new short[] { 0, 2 } }));
        absent.AddMaterial(@"C:\sfm\game\usermod\materials\models\creator\a\body.vmt", "vmt");
        absent.AddMaterial(@"C:\sfm\game\usermod\materials\models\creator\a\shirt_blue.vmt", "vmt");
        var absentValidation = absent.Validate(absent.Plan());
        var warning = absentValidation.Messages.Single(message => message.Code == "mdl-material-referenced-not-packaged");
        CollectionAssert.AreEqual(new[] { "shirt_red" }, warning.AffectedValues.ToArray());
    }

    [TestMethod]
    public void SharedVmtCanSatisfyMultipleModelsIndependently()
    {
        var fixture = new Fixture();
        fixture.AddModel(@"C:\sfm\game\usermod\models\creator\a\a.mdl", "a", "a", ModelRole.Primary, Mdl("a.mdl", new[] { "eyes" }, new[] { @"models\shared\" }));
        fixture.AddModel(@"C:\sfm\game\usermod\models\creator\b\b.mdl", "b", "b", ModelRole.Additional, Mdl("b.mdl", new[] { "eyes" }, new[] { @"models\shared\" }));
        fixture.AddMaterial(@"C:\sfm\game\usermod\materials\models\shared\eyes.vmt", "vmt");

        AssertNoMaterialWarning(fixture.Validate(fixture.Plan()));

        var independent = new Fixture();
        independent.AddModel(@"C:\sfm\game\usermod\models\creator\a\a.mdl", "a", "a", ModelRole.Primary, Mdl("a.mdl", new[] { "eyes" }, new[] { @"models\shared\" }));
        independent.AddModel(@"C:\sfm\game\usermod\models\creator\b\b.mdl", "b", "b", ModelRole.Additional, Mdl("b.mdl", new[] { "eyes" }, new[] { @"models\other\" }));
        independent.AddMaterial(@"C:\sfm\game\usermod\materials\models\shared\eyes.vmt", "vmt");
        var validation = independent.Validate(independent.Plan());
        Assert.AreEqual(1, validation.Messages.Count(message => message.Code == "mdl-material-possibly-misplaced"));
    }

    [TestMethod]
    public void MalformedSkinTableDoesNotCrashOrLeakBinaryJargonInSummary()
    {
        var fixture = new Fixture();
        fixture.AddModel(@"C:\sfm\game\usermod\models\creator\a\a.mdl", "a", "a", ModelRole.Primary, Mdl("a.mdl", new[] { "body", "red" }, new[] { @"models\creator\a\" }).WithSkinDimensions(1, 1, 4095).ToArray());
        fixture.AddMaterial(@"C:\sfm\game\usermod\materials\models\creator\a\body.vmt", "vmt");

        var validation = fixture.Validate(fixture.Plan());

        var partial = validation.Messages.Single(message => message.Code == "mdl-material-inspection-partial");
        Assert.AreEqual("This model's material metadata was only partially readable.", partial.Message);
        Assert.IsFalse(partial.Message.Contains("skinindex", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(partial.Message.Contains("binary", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(validation.HasErrors);
    }

    [TestMethod]
    public void ExistingErrorBehaviorRemainsErrorWhileMdlWarningsRemainNonBlockingWarnings()
    {
        var fixture = new Fixture();
        var model = fixture.AddModel(@"C:\sfm\game\usermod\models\creator\a\a.mdl", "a", "CON", ModelRole.Primary, Mdl("a.mdl", new[] { "body" }, new[] { @"models\creator\a\" }));

        var validation = fixture.Validate(fixture.Plan());

        Assert.IsTrue(validation.Messages.Any(message => message.Code == "invalid-release-model-name" && message.Severity == ValidationSeverity.Error));
        Assert.IsTrue(validation.HasErrors);
        Assert.AreEqual(model.Id, fixture.Project.Models[0].Id);
    }

    [TestMethod]
    public void MdlMaterialWarningsDoNotPreventBuild()
    {
        using var workspace = BuildWorkspace.Create();
        workspace.Write(@"source\game\usermod\models\creator\a\a.mdl", Mdl("a.mdl", new[] { "body" }, new[] { @"models\creator\a\" }).ToArray());

        var project = new PackageProject
        {
            AssetName = "A",
            CurrentVersion = "1.0",
            ArchiveName = "a.zip",
            Models =
            {
                new ModelEntry
                {
                    Role = ModelRole.Primary,
                    SourceMdlPath = Path.Combine(workspace.Root, @"source\game\usermod\models\creator\a\a.mdl"),
                    SourceStem = "a",
                    ReleaseStem = "a"
                }
            },
            Readme = new ReadmeConfig { Mode = ReadmeMode.None }
        };

        var summary = new BuildCoordinator(workspace.AppRoot).Build(new BuildRequest
        {
            Project = project,
            OutputDirectory = workspace.OutputRoot
        });

        Assert.IsTrue(summary.Validation!.Messages.Any(message => message.Code == "mdl-material-referenced-not-packaged" && message.Severity == ValidationSeverity.Warning));
        Assert.IsTrue(summary.Succeeded, summary.LogText());
    }

    private static SyntheticMdl Mdl(string name, string[] textures, string[] searchPaths, short[][]? skin = null) =>
        new SyntheticMdl()
            .WithName(name)
            .WithTextures(textures)
            .WithCdTextures(searchPaths)
            .WithSkinTable(skin ?? new[] { Enumerable.Range(0, textures.Length).Select(index => checked((short)index)).ToArray() });

    private static MdlSkinTable Skin(short[][] matrix) =>
        new(matrix.Length == 0 ? 0 : matrix[0].Length, matrix.Length, matrix);

    private static void AssertNoMaterialWarning(ValidationResult validation)
    {
        Assert.IsFalse(validation.Messages.Any(message => message.Code.StartsWith("mdl-material-", StringComparison.Ordinal) && message.Severity == ValidationSeverity.Warning),
            string.Join(Environment.NewLine, validation.Messages.Select(message => $"{message.Code}: {message.Message}")));
    }

    private sealed class CountingReader : IMdlMetadataReader
    {
        private readonly MdlMetadataReadResult result;

        public CountingReader(MdlMetadataReadResult result)
        {
            this.result = result;
        }

        public int ReadCount { get; private set; }

        public MdlMetadataReadResult Read(string path)
        {
            ReadCount++;
            return result;
        }
    }

    private sealed class Fixture
    {
        private readonly FakeFileSystem fileSystem = new();
        private readonly FakeOutputEnvironment output = new();

        public Fixture()
        {
            output.AddDirectory(OutputDirectory);
            Project = new PackageProject
            {
                AssetName = "Fixture",
                CurrentVersion = "1.0",
                ArchiveName = "fixture.zip",
                Readme = new ReadmeConfig { Mode = ReadmeMode.None }
            };
        }

        public PackageProject Project { get; }

        public ModelEntry AddModel(string path, string sourceStem, string releaseStem, ModelRole role, SyntheticMdl mdl) =>
            AddModel(path, sourceStem, releaseStem, role, mdl.ToArray());

        public ModelEntry AddModel(string path, string sourceStem, string releaseStem, ModelRole role, byte[] bytes)
        {
            fileSystem.AddFile(path, bytes);
            var model = new ModelEntry
            {
                Role = role,
                SourceMdlPath = path,
                SourceStem = sourceStem,
                ReleaseStem = releaseStem
            };
            Project.Models.Add(model);
            return model;
        }

        public void AddMaterial(string path, string contents, DestinationOverride? destinationOverride = null)
        {
            fileSystem.AddFile(path, Encoding.UTF8.GetBytes(contents));
            Project.MaterialSources.Add(new SourceEntry
            {
                Kind = SourceEntryKind.Material,
                SourcePath = path,
                IsFolder = false,
                DestinationOverride = destinationOverride
            });
        }

        public PackagePlan Plan(IMdlMetadataReader? reader = null) =>
            new PackagePlanner(
                new SourceExpansionService(fileSystem),
                new SharedFileRegistry(),
                new ReadmeResolver(fileSystem),
                reader ?? new MdlV49MetadataReader(fileSystem))
            .CreatePlan(Project);

        public ValidationResult Validate(PackagePlan plan) =>
            new PackageValidator(fileSystem, output).Validate(Project, plan, new PackageValidationContext { OutputDirectory = OutputDirectory });
    }

    private sealed class SyntheticMdl
    {
        private readonly byte[] bytes = new byte[4096];
        private int cursor = 512;

        public SyntheticMdl()
        {
            Encoding.ASCII.GetBytes("IDST", bytes.AsSpan(0, 4));
            WriteInt32(4, 49);
            WriteInt32(8, 1);
            WithName("model.mdl");
            WriteInt32(76, bytes.Length);
            WithTextures(Array.Empty<string>());
            WithCdTextures(Array.Empty<string>());
            WithSkinDimensions(0, 0, 0);
        }

        public SyntheticMdl WithVersion(int version)
        {
            WriteInt32(4, version);
            return this;
        }

        public SyntheticMdl WithName(string name)
        {
            Array.Clear(bytes, 12, 64);
            Encoding.ASCII.GetBytes(name, bytes.AsSpan(12, name.Length));
            return this;
        }

        public SyntheticMdl WithTextures(params string[] names)
        {
            var tableOffset = 256;
            WriteInt32(204, names.Length);
            WriteInt32(208, tableOffset);
            cursor = Math.Max(cursor, tableOffset + names.Length * 64);
            for (var i = 0; i < names.Length; i++)
            {
                var entryStart = tableOffset + i * 64;
                var stringOffset = WriteString(names[i]);
                WriteInt32(entryStart, stringOffset - entryStart);
            }

            return this;
        }

        public SyntheticMdl WithTextureTable(int count, int index)
        {
            WriteInt32(204, count);
            WriteInt32(208, index);
            return this;
        }

        public SyntheticMdl WithCdTextures(params string[] paths)
        {
            var tableOffset = Align(cursor, 4);
            WriteInt32(212, paths.Length);
            WriteInt32(216, tableOffset);
            cursor = Math.Max(cursor, tableOffset + paths.Length * 4);
            for (var i = 0; i < paths.Length; i++)
            {
                WriteInt32(tableOffset + i * 4, WriteString(paths[i]));
            }

            return this;
        }

        public SyntheticMdl WithSkinTable(short[][] remapMatrix)
        {
            var familyCount = remapMatrix.Length;
            var slotCount = familyCount == 0 ? 0 : remapMatrix[0].Length;
            var tableOffset = Align(cursor, 2);
            WithSkinDimensions(slotCount, familyCount, tableOffset);
            cursor = Math.Max(cursor, tableOffset + slotCount * familyCount * 2);
            for (var family = 0; family < familyCount; family++)
            {
                for (var slot = 0; slot < slotCount; slot++)
                {
                    BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(tableOffset + 2 * (family * slotCount + slot), 2), remapMatrix[family][slot]);
                }
            }

            return this;
        }

        public SyntheticMdl WithSkinDimensions(int slotCount, int familyCount, int index)
        {
            WriteInt32(220, slotCount);
            WriteInt32(224, familyCount);
            WriteInt32(228, index);
            return this;
        }

        public byte[] ToArray() => bytes.ToArray();

        private int WriteString(string value)
        {
            var offset = cursor;
            var encoded = Encoding.ASCII.GetBytes(value);
            encoded.CopyTo(bytes.AsSpan(offset));
            bytes[offset + encoded.Length] = 0;
            cursor += encoded.Length + 1;
            return offset;
        }

        private void WriteInt32(int offset, int value) =>
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset, 4), value);

        private static int Align(int value, int alignment) =>
            (value + alignment - 1) / alignment * alignment;
    }

    private sealed class BuildWorkspace : IDisposable
    {
        private BuildWorkspace(string root)
        {
            Root = root;
            AppRoot = Path.Combine(root, "app");
            OutputRoot = Path.Combine(root, "output");
            Directory.CreateDirectory(AppRoot);
            Directory.CreateDirectory(OutputRoot);
        }

        public string Root { get; }

        public string AppRoot { get; }

        public string OutputRoot { get; }

        public static BuildWorkspace Create() =>
            new(Path.Combine(Path.GetTempPath(), "SfmPackageBuilder.MdlMaterial.Tests", Guid.NewGuid().ToString("N")));

        public void Write(string relativePath, byte[] bytes)
        {
            var path = Path.Combine(Root, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, bytes);
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
