using Microsoft.VisualStudio.TestTools.UnitTesting;
using SfmPackageBuilder.Core.Domain;
using SfmPackageBuilder.Core.Expansion;

namespace SfmPackageBuilder.Core.Tests;

[TestClass]
public sealed class ModelCompanionDetectorTests
{
    [TestMethod]
    public void MdlOnlyResolvesWithoutWarnings()
    {
        var fileSystem = CreateModelFiles("chair.mdl");

        var family = Resolve(fileSystem);

        Assert.AreEqual(ModelFamilyResolutionStatus.Resolved, family.Status);
        Assert.AreEqual(1, family.Files.Count);
        Assert.AreEqual(ModelFamilyFileKind.SelectedMdl, family.Files[0].Kind);
        Assert.AreEqual(SourceObservationStatus.Exists, family.Files[0].ObservationStatus);
    }

    [TestMethod]
    public void MdlAndVvdResolve()
    {
        var fileSystem = CreateModelFiles("chair.mdl", "chair.vvd");

        var family = Resolve(fileSystem);

        AssertCompanions(family, ".vvd");
    }

    [TestMethod]
    public void MdlVvdAndDx90Resolve()
    {
        var fileSystem = CreateModelFiles("chair.mdl", "chair.vvd", "chair.dx90.vtx");

        var family = Resolve(fileSystem);

        AssertCompanions(family, ".vvd", ".dx90.vtx");
    }

    [TestMethod]
    public void Dx80CompanionResolves()
    {
        var fileSystem = CreateModelFiles("chair.mdl", "chair.dx80.vtx");

        var family = Resolve(fileSystem);

        AssertCompanions(family, ".dx80.vtx");
    }

    [TestMethod]
    public void SwVtxCompanionResolves()
    {
        var fileSystem = CreateModelFiles("chair.mdl", "chair.sw.vtx");

        var family = Resolve(fileSystem);

        AssertCompanions(family, ".sw.vtx");
    }

    [TestMethod]
    public void PhyCompanionResolves()
    {
        var fileSystem = CreateModelFiles("chair.mdl", "chair.phy");

        var family = Resolve(fileSystem);

        AssertCompanions(family, ".phy");
    }

    [TestMethod]
    public void SeveralOptionalCombinationsResolve()
    {
        var fileSystem = CreateModelFiles("chair.mdl", "chair.vvd", "chair.dx80.vtx", "chair.sw.vtx", "chair.phy");

        var family = Resolve(fileSystem);

        AssertCompanions(family, ".vvd", ".dx80.vtx", ".sw.vtx", ".phy");
    }

    [TestMethod]
    public void SameStemMatchingOnly()
    {
        var fileSystem = CreateModelFiles(
            "chair_dev.mdl",
            "chair_dev.vvd",
            "chair.mdl",
            "chair_dev_backup.vvd",
            "chair_dev2.dx90.vtx",
            "another_model.phy");

        var family = Resolve(
            fileSystem,
            selectedPath: @"E:\SFM\game\usermod\models\Creator\chair_dev.mdl",
            stem: "chair_dev");

        AssertCompanions(family, ".vvd");
        Assert.IsFalse(family.Files.Any(file => file.SourcePath.Contains("backup", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(family.Files.Any(file => file.SourcePath.Contains("dev2", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(family.Files.Any(file => file.SourcePath.Contains("another", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void CompanionMatchingUsesWindowsStyleCaseInsensitivity()
    {
        var fileSystem = CreateModelFiles("Chair.MDL", "CHAIR.VVD", "Chair.DX90.VTX");

        var family = Resolve(fileSystem, selectedPath: @"E:\SFM\game\usermod\models\Creator\chair.mdl", stem: "chair");

        AssertCompanions(family, ".vvd", ".dx90.vtx");
        Assert.IsTrue(family.Files.Any(file => file.SourcePath.EndsWith("CHAIR.VVD", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void NewlyDetectedCompanionWithoutSavedIntentDefaultsToInclude()
    {
        var fileSystem = CreateModelFiles("chair.mdl", "chair.vvd");

        var family = Resolve(fileSystem);
        var companion = family.Files.Single(file => file.RuntimeSuffix == ".vvd");

        Assert.AreEqual(CompanionUserSelection.Include, companion.UserSelection);
        Assert.IsTrue(companion.ShouldInclude);
    }

    [TestMethod]
    public void SavedIncludeOnExistingCompanionIsIncluded()
    {
        var fileSystem = CreateModelFiles("chair.mdl", "chair.vvd");
        var model = CreateModelEntry("chair");
        model.Companions.Add(new ModelCompanionSelection
        {
            RuntimeSuffix = ".vvd",
            SourcePath = @"E:\SFM\game\usermod\models\Creator\chair.vvd",
            UserSelection = CompanionUserSelection.Include
        });

        var family = new ModelCompanionDetector(fileSystem).Resolve(model);

        var companion = family.Files.Single(file => file.RuntimeSuffix == ".vvd");
        Assert.AreEqual(CompanionUserSelection.Include, companion.UserSelection);
        Assert.AreEqual(SourceObservationStatus.Exists, companion.ObservationStatus);
    }

    [TestMethod]
    public void SavedExcludeOnExistingCompanionIsExcluded()
    {
        var fileSystem = CreateModelFiles("chair.mdl", "chair.vvd");
        var model = CreateModelEntry("chair");
        model.Companions.Add(new ModelCompanionSelection
        {
            RuntimeSuffix = ".vvd",
            SourcePath = @"E:\SFM\game\usermod\models\Creator\chair.vvd",
            UserSelection = CompanionUserSelection.Exclude
        });

        var family = new ModelCompanionDetector(fileSystem).Resolve(model);

        var companion = family.Files.Single(file => file.RuntimeSuffix == ".vvd");
        Assert.AreEqual(CompanionUserSelection.Exclude, companion.UserSelection);
        Assert.IsFalse(companion.ShouldInclude);
        Assert.AreEqual(SourceObservationStatus.Exists, companion.ObservationStatus);
    }

    [TestMethod]
    public void SavedIncludeWhoseFileIsNowMissingIsReportedAsMissingIntent()
    {
        var fileSystem = CreateModelFiles("chair.mdl");
        var model = CreateModelEntry("chair");
        model.Companions.Add(new ModelCompanionSelection
        {
            RuntimeSuffix = ".vvd",
            SourcePath = @"E:\SFM\game\usermod\models\Creator\chair.vvd",
            UserSelection = CompanionUserSelection.Include
        });

        var family = new ModelCompanionDetector(fileSystem).Resolve(model);

        var companion = family.Files.Single(file => file.RuntimeSuffix == ".vvd");
        Assert.AreEqual(CompanionUserSelection.Include, companion.UserSelection);
        Assert.AreEqual(SourceObservationStatus.Missing, companion.ObservationStatus);
    }

    [TestMethod]
    public void PrimaryModelFamilyPreservesRole()
    {
        var fileSystem = CreateModelFiles("chair.mdl");
        var model = CreateModelEntry("chair", ModelRole.Primary);

        var family = new ModelCompanionDetector(fileSystem).Resolve(model);

        Assert.AreEqual(ModelRole.Primary, family.Role);
    }

    [TestMethod]
    public void AdditionalModelFamilyPreservesRole()
    {
        var fileSystem = CreateModelFiles("chair.mdl");
        var model = CreateModelEntry("chair", ModelRole.Additional);

        var family = new ModelCompanionDetector(fileSystem).Resolve(model);

        Assert.AreEqual(ModelRole.Additional, family.Role);
    }

    [TestMethod]
    public void MultipleIndependentModelFamiliesResolve()
    {
        var fileSystem = new FakeFileSystem();
        fileSystem.AddFile(@"E:\SFM\game\usermod\models\Creator\chair.mdl", Array.Empty<byte>());
        fileSystem.AddFile(@"E:\SFM\game\usermod\models\Creator\chair.vvd", Array.Empty<byte>());
        fileSystem.AddFile(@"E:\SFM\game\usermod\models\Creator\table.mdl", Array.Empty<byte>());
        fileSystem.AddFile(@"E:\SFM\game\usermod\models\Creator\table.phy", Array.Empty<byte>());
        var project = new PackageProject
        {
            Models = new List<ModelEntry>
            {
                CreateModelEntry("chair", ModelRole.Primary),
                CreateModelEntry("table", ModelRole.Additional)
            }
        };

        var result = new SourceExpansionService(fileSystem).Resolve(project);

        Assert.AreEqual(2, result.ModelFamilies.Count);
        AssertCompanions(result.ModelFamilies[0], ".vvd");
        AssertCompanions(result.ModelFamilies[1], ".phy");
    }

    [TestMethod]
    public void MissingSelectedMdlProducesStructuredOutcome()
    {
        var family = Resolve(new FakeFileSystem());

        Assert.AreEqual(ModelFamilyResolutionStatus.MissingSelectedModel, family.Status);
        Assert.AreEqual(SourceObservationStatus.Missing, family.Files[0].ObservationStatus);
    }

    [TestMethod]
    public void EnumerationFailureProducesStructuredOutcome()
    {
        var fileSystem = CreateModelFiles("chair.mdl");
        fileSystem.FailEnumeration(@"E:\SFM\game\usermod\models\Creator");

        var family = Resolve(fileSystem);

        Assert.AreEqual(ModelFamilyResolutionStatus.EnumerationFailed, family.Status);
        Assert.IsNotNull(family.FailureDetail);
    }

    [TestMethod]
    public void CustomCompanionRuleCanExtendDetection()
    {
        var fileSystem = CreateModelFiles("chair.mdl", "chair.custom");
        var detector = new ModelCompanionDetector(fileSystem, new[] { new CompanionRule(".custom") });

        var family = detector.Resolve(CreateModelEntry("chair"));

        AssertCompanions(family, ".custom");
    }

    private static FakeFileSystem CreateModelFiles(params string[] fileNames)
    {
        var fileSystem = new FakeFileSystem();
        foreach (var fileName in fileNames)
        {
            fileSystem.AddFile($@"E:\SFM\game\usermod\models\Creator\{fileName}", Array.Empty<byte>());
        }

        return fileSystem;
    }

    private static ModelFamily Resolve(FakeFileSystem fileSystem, string selectedPath = @"E:\SFM\game\usermod\models\Creator\chair.mdl", string stem = "chair")
    {
        return new ModelCompanionDetector(fileSystem).Resolve(CreateModelEntry(stem, sourcePath: selectedPath));
    }

    private static ModelEntry CreateModelEntry(
        string stem,
        ModelRole role = ModelRole.Primary,
        string? sourcePath = null)
    {
        return new ModelEntry
        {
            Role = role,
            SourceMdlPath = sourcePath ?? $@"E:\SFM\game\usermod\models\Creator\{stem}.mdl",
            SourceStem = stem,
            ReleaseStem = $"{stem}_release"
        };
    }

    private static void AssertCompanions(ModelFamily family, params string[] runtimeSuffixes)
    {
        var actualSuffixes = family.Files
            .Where(file => file.Kind == ModelFamilyFileKind.Companion)
            .Select(file => file.RuntimeSuffix)
            .ToArray();

        CollectionAssert.AreEqual(runtimeSuffixes, actualSuffixes);
    }
}
