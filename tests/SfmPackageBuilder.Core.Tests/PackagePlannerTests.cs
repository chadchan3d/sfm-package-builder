using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SfmPackageBuilder.Core.Domain;
using SfmPackageBuilder.Core.FileSystem;
using SfmPackageBuilder.Core.Planning;
using SfmPackageBuilder.Core.Readme;
using SfmPackageBuilder.Core.SharedFiles;

namespace SfmPackageBuilder.Core.Tests;

[TestClass]
public sealed class PackagePlannerTests
{
    [TestMethod]
    public void SimplePropPlansModelMaterialAndReadme()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl");
        fileSystem.AddFile(@"E:\SFM\game\usermod\materials\models\Creator\chair\chair.vmt", Array.Empty<byte>());

        var plan = CreatePlan(fileSystem, SimpleProject());

        AssertDestination(plan, PackagePlanEntryType.Model, @"models\Creator\chair\chair_release.mdl");
        AssertDestination(plan, PackagePlanEntryType.Material, @"materials\models\Creator\chair\chair.vmt");
        Assert.AreEqual(1, ReadmeEntries(plan).Count);
    }

    [TestMethod]
    public void NaturalSourceRootsAreRetained()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl");
        fileSystem.AddFile(@"E:\SFM\game\usermod\materials\models\Creator\chair\body.vtf", Array.Empty<byte>());

        var plan = CreatePlan(fileSystem, SimpleProject());

        StringAssert.StartsWith(Entry(plan, PackagePlanEntryType.Model).DestinationRelativePath, @"models\");
        StringAssert.StartsWith(Entry(plan, PackagePlanEntryType.Material).DestinationRelativePath, @"materials\");
    }

    [TestMethod]
    public void PlanningIsDeterministicOnRepeatedCalls()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl", "chair.vvd");
        fileSystem.AddFile(@"E:\SFM\game\usermod\materials\models\Creator\chair\b.vtf", Array.Empty<byte>());
        fileSystem.AddFile(@"E:\SFM\game\usermod\materials\models\Creator\chair\a.vmt", Array.Empty<byte>());
        var project = SimpleProject();

        var first = CreatePlan(fileSystem, project);
        var second = CreatePlan(fileSystem, project);

        CollectionAssert.AreEqual(Signature(first), Signature(second));
    }

    [TestMethod]
    public void PrimaryMdlIsRenamedInPlanOnly()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair_dev.mdl");
        var project = SimpleProject(stem: "chair_dev", releaseStem: "chair_release");

        var plan = CreatePlan(fileSystem, project);

        AssertDestination(plan, PackagePlanEntryType.Model, @"models\Creator\chair_dev\chair_release.mdl");
        Assert.IsTrue(fileSystem.FileExists(@"E:\SFM\game\usermod\models\Creator\chair_dev\chair_dev.mdl"));
    }

    [TestMethod]
    public void PrimaryCompanionsUseReleaseStem()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "house.mdl", "house.vvd", "house.dx90.vtx", "house.dx80.vtx", "house.sw.vtx", "house.phy");
        var project = SimpleProject(stem: "house", releaseStem: "house_release");

        var plan = CreatePlan(fileSystem, project);

        CollectionAssert.AreEqual(
            new[]
            {
                @"models\Creator\house\house_release.vvd",
                @"models\Creator\house\house_release.dx90.vtx",
                @"models\Creator\house\house_release.dx80.vtx",
                @"models\Creator\house\house_release.sw.vtx",
                @"models\Creator\house\house_release.phy"
            },
            plan.Entries
                .Where(entry => entry.EntryType == PackagePlanEntryType.ModelCompanion)
                .Select(entry => entry.DestinationRelativePath)
                .ToArray());
    }

    [TestMethod]
    public void BlankPrimaryReleaseStemUsesSourceStemForPlanning()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl");
        var project = SimpleProject(stem: "chair", releaseStem: string.Empty);

        var plan = CreatePlan(fileSystem, project);

        AssertDestination(plan, PackagePlanEntryType.Model, @"models\Creator\chair\chair.mdl");
    }

    [TestMethod]
    public void SeveralPrimaryCompanionCombinationsResolve()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "house.mdl", "house.vvd", "house.sw.vtx", "house.phy");

        var plan = CreatePlan(fileSystem, SimpleProject(stem: "house", releaseStem: "house_release"));

        CollectionAssert.AreEqual(
            new[]
            {
                @"models\Creator\house\house_release.vvd",
                @"models\Creator\house\house_release.sw.vtx",
                @"models\Creator\house\house_release.phy"
            },
            plan.Entries
                .Where(entry => entry.EntryType == PackagePlanEntryType.ModelCompanion)
                .Select(entry => entry.DestinationRelativePath)
                .ToArray());
    }

    [TestMethod]
    public void NewlyDetectedCompanionIsIncluded()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl", "chair.vvd");

        var plan = CreatePlan(fileSystem, SimpleProject());

        AssertDestination(plan, PackagePlanEntryType.ModelCompanion, @"models\Creator\chair\chair_release.vvd");
    }

    [TestMethod]
    public void SavedIncludeCompanionIsIncluded()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl", "chair.vvd");
        var project = SimpleProject();
        project.Models[0].Companions.Add(new ModelCompanionSelection
        {
            RuntimeSuffix = ".vvd",
            SourcePath = @"E:\SFM\game\usermod\models\Creator\chair\chair.vvd",
            UserSelection = CompanionUserSelection.Include
        });

        var plan = CreatePlan(fileSystem, project);

        AssertDestination(plan, PackagePlanEntryType.ModelCompanion, @"models\Creator\chair\chair_release.vvd");
    }

    [TestMethod]
    public void SavedExcludeCompanionIsAbsent()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl", "chair.vvd");
        var project = SimpleProject();
        project.Models[0].Companions.Add(new ModelCompanionSelection
        {
            RuntimeSuffix = ".vvd",
            SourcePath = @"E:\SFM\game\usermod\models\Creator\chair\chair.vvd",
            UserSelection = CompanionUserSelection.Exclude
        });

        var plan = CreatePlan(fileSystem, project);

        Assert.IsFalse(plan.Entries.Any(entry => entry.DestinationRelativePath?.EndsWith(".vvd", StringComparison.OrdinalIgnoreCase) == true));
    }

    [TestMethod]
    public void IntendedMissingCompanionRemainsRepresentedForValidation()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl");
        var project = SimpleProject();
        project.Models[0].Companions.Add(new ModelCompanionSelection
        {
            RuntimeSuffix = ".vvd",
            SourcePath = @"E:\SFM\game\usermod\models\Creator\chair\chair.vvd",
            UserSelection = CompanionUserSelection.Include
        });

        var plan = CreatePlan(fileSystem, project);
        var missing = plan.Entries.Single(entry => entry.EntryType == PackagePlanEntryType.ModelCompanion);

        Assert.AreEqual(PackagePlanEntryStatus.MissingSource, missing.Status);
        Assert.AreEqual(@"models\Creator\chair\chair_release.vvd", missing.DestinationRelativePath);
    }

    [TestMethod]
    public void AdditionalModelDefaultRuntimeNameAndCompanionsUseSourceStem()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl");
        fileSystem.AddFile(@"E:\SFM\game\usermod\models\Creator\shared\hinge.mdl", Array.Empty<byte>());
        fileSystem.AddFile(@"E:\SFM\game\usermod\models\Creator\shared\hinge.vvd", Array.Empty<byte>());
        var project = SimpleProject();
        project.Models.Add(new ModelEntry
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Role = ModelRole.Additional,
            SourceMdlPath = @"E:\SFM\game\usermod\models\Creator\shared\hinge.mdl",
            SourceStem = "hinge",
            ReleaseStem = string.Empty
        });

        var plan = CreatePlan(fileSystem, project);

        Assert.IsTrue(plan.Entries.Any(entry => entry.DestinationRelativePath == @"models\Creator\shared\hinge.mdl"));
        Assert.IsTrue(plan.Entries.Any(entry => entry.DestinationRelativePath == @"models\Creator\shared\hinge.vvd"));
    }

    [TestMethod]
    public void AdditionalModelReleaseStemRenamesRuntimeFamily()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl");
        fileSystem.AddFile(@"E:\SFM\game\usermod\models\Creator\shared\worgen_tail.mdl", Array.Empty<byte>());
        fileSystem.AddFile(@"E:\SFM\game\usermod\models\Creator\shared\worgen_tail.vvd", Array.Empty<byte>());
        fileSystem.AddFile(@"E:\SFM\game\usermod\models\Creator\shared\worgen_tail.dx90.vtx", Array.Empty<byte>());
        var project = SimpleProject();
        var additional = new ModelEntry
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Role = ModelRole.Additional,
            SourceMdlPath = @"E:\SFM\game\usermod\models\Creator\shared\worgen_tail.mdl",
            SourceStem = "worgen_tail",
            ReleaseStem = "Gwen_Worgen_Tail"
        };
        project.Models.Add(additional);

        var plan = CreatePlan(fileSystem, project);
        var additionalDestinations = plan.Entries
            .Where(entry => entry.ModelEntryId == additional.Id)
            .Select(entry => entry.DestinationRelativePath)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        CollectionAssert.AreEqual(
            new[]
            {
                @"models\Creator\shared\Gwen_Worgen_Tail.dx90.vtx",
                @"models\Creator\shared\Gwen_Worgen_Tail.mdl",
                @"models\Creator\shared\Gwen_Worgen_Tail.vvd"
            },
            additionalDestinations);
        Assert.IsTrue(fileSystem.FileExists(@"E:\SFM\game\usermod\models\Creator\shared\worgen_tail.mdl"));
    }

    [TestMethod]
    public void AdditionalModelReleaseStemRevertRemovesRenamedDestinations()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl");
        fileSystem.AddFile(@"E:\SFM\game\usermod\models\Creator\shared\worgen_tail.mdl", Array.Empty<byte>());
        fileSystem.AddFile(@"E:\SFM\game\usermod\models\Creator\shared\worgen_tail.vvd", Array.Empty<byte>());
        var project = SimpleProject();
        var additional = new ModelEntry
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Role = ModelRole.Additional,
            SourceMdlPath = @"E:\SFM\game\usermod\models\Creator\shared\worgen_tail.mdl",
            SourceStem = "worgen_tail",
            ReleaseStem = string.Empty
        };
        project.Models.Add(additional);

        var plan = CreatePlan(fileSystem, project);

        Assert.IsTrue(plan.Entries.Any(entry => entry.ModelEntryId == additional.Id && entry.DestinationRelativePath == @"models\Creator\shared\worgen_tail.mdl"));
        Assert.IsTrue(plan.Entries.Any(entry => entry.ModelEntryId == additional.Id && entry.DestinationRelativePath == @"models\Creator\shared\worgen_tail.vvd"));
        Assert.IsFalse(plan.Entries.Any(entry => entry.ModelEntryId == additional.Id && entry.DestinationRelativePath?.Contains("Gwen_Worgen_Tail", StringComparison.OrdinalIgnoreCase) == true));
    }

    [TestMethod]
    public void ReopenedAdditionalModelReleaseStemAppliesToRefreshedCompanions()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl");
        fileSystem.AddFile(@"E:\SFM\game\usermod\models\Creator\shared\shirt_dev04.mdl", Array.Empty<byte>());
        fileSystem.AddFile(@"E:\SFM\game\usermod\models\Creator\shared\shirt_dev04.vvd", Array.Empty<byte>());
        var project = SimpleProject();
        project.Models.Add(new ModelEntry
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Role = ModelRole.Additional,
            SourceMdlPath = @"E:\SFM\game\usermod\models\Creator\shared\shirt_dev04.mdl",
            SourceStem = "shirt_dev04",
            ReleaseStem = "Gwen_Shirt"
        });
        var serializer = new SfmPackageBuilder.Core.Persistence.ProjectSerializer();
        var reopened = serializer.Deserialize(serializer.Serialize(project)).Project!;
        fileSystem.AddFile(@"E:\SFM\game\usermod\models\Creator\shared\shirt_dev04.dx90.vtx", Array.Empty<byte>());

        var plan = CreatePlan(fileSystem, reopened);

        Assert.AreEqual("Gwen_Shirt", reopened.Models.Single(model => model.Role == ModelRole.Additional).ReleaseStem);
        Assert.IsTrue(plan.Entries.Any(entry => entry.DestinationRelativePath == @"models\Creator\shared\Gwen_Shirt.vvd"));
        Assert.IsTrue(plan.Entries.Any(entry => entry.DestinationRelativePath == @"models\Creator\shared\Gwen_Shirt.dx90.vtx"));
        Assert.IsFalse(plan.Entries.Any(entry => entry.DestinationRelativePath == @"models\Creator\shared\shirt_dev04.dx90.vtx"));
    }

    [TestMethod]
    public void MultipleAdditionalModelFamiliesRemainIndependent()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl");
        fileSystem.AddFile(@"E:\SFM\game\usermod\models\Creator\a\a.mdl", Array.Empty<byte>());
        fileSystem.AddFile(@"E:\SFM\game\usermod\models\Creator\b\b.mdl", Array.Empty<byte>());
        var project = SimpleProject();
        project.Models.Add(AdditionalModel("a", Guid.Parse("22222222-2222-2222-2222-222222222222")));
        project.Models.Add(AdditionalModel("b", Guid.Parse("33333333-3333-3333-3333-333333333333")));

        var plan = CreatePlan(fileSystem, project);

        Assert.IsTrue(plan.Entries.Any(entry => entry.ModelEntryId == project.Models[1].Id && entry.DestinationRelativePath == @"models\Creator\a\a_release.mdl"));
        Assert.IsTrue(plan.Entries.Any(entry => entry.ModelEntryId == project.Models[2].Id && entry.DestinationRelativePath == @"models\Creator\b\b_release.mdl"));
    }

    [TestMethod]
    public void IndividualMaterialFileMapsThroughMaterialsAnchor()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl");
        fileSystem.AddFile(@"E:\SFM\game\usermod\materials\models\Creator\chair\body.vtf", Array.Empty<byte>());
        var project = SimpleProject();
        project.MaterialSources.Clear();
        project.MaterialSources.Add(new SourceEntry
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Kind = SourceEntryKind.Material,
            SourcePath = @"E:\SFM\game\usermod\materials\models\Creator\chair\body.vtf",
            IsFolder = false
        });

        var plan = CreatePlan(fileSystem, project);

        AssertDestination(plan, PackagePlanEntryType.Material, @"materials\models\Creator\chair\body.vtf");
    }

    [TestMethod]
    public void RecursiveMaterialFolderPreservesNestedStructure()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl");
        fileSystem.AddFile(@"E:\SFM\game\usermod\materials\models\Creator\chair\normals\chair_n.vtf", Array.Empty<byte>());

        var plan = CreatePlan(fileSystem, SimpleProject());

        AssertDestination(plan, PackagePlanEntryType.Material, @"materials\models\Creator\chair\normals\chair_n.vtf");
    }

    [TestMethod]
    public void MultipleMaterialSourcesAndSpacesUnicodeResolve()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl");
        fileSystem.AddFile(@"Z:\Steam Library\game\user mod\materials\models\Crëator\椅子\body.vtf", Array.Empty<byte>());
        fileSystem.AddFile(@"Q:\SFM\materials\models\Shared Labels\label.vtf", Array.Empty<byte>());
        var project = SimpleProject();
        project.MaterialSources.Clear();
        project.MaterialSources.Add(MaterialFolder(@"Z:\Steam Library\game\user mod\materials\models\Crëator\椅子"));
        project.MaterialSources.Add(MaterialFolder(@"Q:\SFM\materials\models\Shared Labels"));

        var plan = CreatePlan(fileSystem, project);

        Assert.IsTrue(plan.Entries.Any(entry => entry.DestinationRelativePath == @"materials\models\Crëator\椅子\body.vtf"));
        Assert.IsTrue(plan.Entries.Any(entry => entry.DestinationRelativePath == @"materials\models\Shared Labels\label.vtf"));
    }

    [TestMethod]
    public void ExtraRootMiscAndCustomFilesResolve()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl");
        fileSystem.AddFile(@"E:\Release\LICENSE.txt", Array.Empty<byte>());
        fileSystem.AddFile(@"E:\Release\preview.png", Array.Empty<byte>());
        fileSystem.AddFile(@"E:\Release\doc.txt", Array.Empty<byte>());
        var project = SimpleProject();
        project.Extras.Add(ExtraFile(@"E:\Release\LICENSE.txt", DestinationOverrideKind.Root, string.Empty));
        project.Extras.Add(ExtraFile(@"E:\Release\preview.png", DestinationOverrideKind.Misc, string.Empty));
        project.Extras.Add(ExtraFile(@"E:\Release\doc.txt", DestinationOverrideKind.Custom, "Docs"));

        var plan = CreatePlan(fileSystem, project);

        Assert.IsTrue(plan.Entries.Any(entry => entry.EntryType == PackagePlanEntryType.Extra && entry.DestinationRelativePath == "LICENSE.txt"));
        Assert.IsTrue(plan.Entries.Any(entry => entry.EntryType == PackagePlanEntryType.Extra && entry.DestinationRelativePath == @"Misc\preview.png"));
        Assert.IsTrue(plan.Entries.Any(entry => entry.EntryType == PackagePlanEntryType.Extra && entry.DestinationRelativePath == @"Docs\doc.txt"));
    }

    [TestMethod]
    public void RecursiveExtraFolderPreservesNestedStructure()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl");
        fileSystem.AddFile(@"E:\Release\Screens\a\b\preview.png", Array.Empty<byte>());
        var project = SimpleProject();
        project.Extras.Add(ExtraFolder(@"E:\Release\Screens", DestinationOverrideKind.Misc, string.Empty));

        var plan = CreatePlan(fileSystem, project);

        Assert.IsTrue(plan.Entries.Any(entry => entry.EntryType == PackagePlanEntryType.Extra && entry.DestinationRelativePath == @"Misc\a\b\preview.png"));
    }

    [TestMethod]
    public void InvalidCustomDestinationRemainsUnresolved()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl");
        fileSystem.AddFile(@"E:\Release\doc.txt", Array.Empty<byte>());
        var project = SimpleProject();
        project.Extras.Add(ExtraFile(@"E:\Release\doc.txt", DestinationOverrideKind.Custom, @"..\outside"));

        var plan = CreatePlan(fileSystem, project);

        var extra = plan.Entries.Single(entry => entry.EntryType == PackagePlanEntryType.Extra);
        Assert.AreEqual(PackagePlanEntryStatus.InvalidDestinationOverride, extra.Status);
        Assert.IsNull(extra.DestinationRelativePath);
    }

    [TestMethod]
    public void MissingMaterialAnchorWithoutOverrideRemainsUnresolved()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl");
        fileSystem.AddFile(@"D:\RandomExports\body.vtf", Array.Empty<byte>());
        var project = SimpleProject();
        project.MaterialSources.Clear();
        project.MaterialSources.Add(new SourceEntry
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Kind = SourceEntryKind.Material,
            SourcePath = @"D:\RandomExports\body.vtf",
            IsFolder = false
        });

        var plan = CreatePlan(fileSystem, project);

        Assert.AreEqual(PackagePlanEntryStatus.MissingAnchor, Entry(plan, PackagePlanEntryType.Material).Status);
    }

    [TestMethod]
    public void MissingSelectedMaterialFolderRemainsRepresentedForValidation()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl");
        var project = SimpleProject();
        project.MaterialSources.Clear();
        project.MaterialSources.Add(MaterialFolder(@"E:\SFM\game\usermod\materials\models\Creator\missing"));

        var plan = CreatePlan(fileSystem, project);
        var material = Entry(plan, PackagePlanEntryType.Material);

        Assert.AreEqual(PackagePlanEntryStatus.MissingSource, material.Status);
        Assert.AreEqual(@"E:\SFM\game\usermod\materials\models\Creator\missing", material.SourcePath);
    }

    [TestMethod]
    public void MissingMaterialAnchorWithValidOverrideResolves()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl");
        fileSystem.AddFile(@"D:\RandomExports\body.vtf", Array.Empty<byte>());
        var project = SimpleProject();
        project.MaterialSources.Clear();
        project.MaterialSources.Add(new SourceEntry
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Kind = SourceEntryKind.Material,
            SourcePath = @"D:\RandomExports\body.vtf",
            IsFolder = false,
            DestinationOverride = new DestinationOverride
            {
                Kind = DestinationOverrideKind.Custom,
                RelativePath = @"materials\models\Creator\chair"
            }
        });

        var plan = CreatePlan(fileSystem, project);

        AssertDestination(plan, PackagePlanEntryType.Material, @"materials\models\Creator\chair\body.vtf");
    }

    [TestMethod]
    public void ParentRecursiveMaterialSelectionCollapsesContainedChildWhenMappingIdentical()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl");
        fileSystem.AddFile(@"E:\SFM\materials\models\Creator\props\chair.vmt", Array.Empty<byte>());
        var project = SimpleProject();
        project.MaterialSources.Clear();
        project.MaterialSources.Add(MaterialFolder(@"E:\SFM\materials\models\Creator"));
        project.MaterialSources.Add(MaterialFolder(@"E:\SFM\materials\models\Creator\props"));

        var plan = CreatePlan(fileSystem, project);

        Assert.AreEqual(1, plan.Entries.Count(entry => entry.EntryType == PackagePlanEntryType.Material));
        AssertDestination(plan, PackagePlanEntryType.Material, @"materials\models\Creator\props\chair.vmt");
    }

    [TestMethod]
    public void RedundantMaterialChildSelectionIsNotIndependentlyEnumerated()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl");
        fileSystem.AddFile(@"E:\SFM\materials\models\Creator\props\chair.vmt", Array.Empty<byte>());
        fileSystem.FailEnumeration(@"E:\SFM\materials\models\Creator\props");
        var project = SimpleProject();
        project.MaterialSources.Clear();
        project.MaterialSources.Add(MaterialFolder(@"E:\SFM\materials\models\Creator"));
        project.MaterialSources.Add(MaterialFolder(@"E:\SFM\materials\models\Creator\props"));

        var plan = CreatePlan(fileSystem, project);

        Assert.AreEqual(1, plan.Entries.Count(entry => entry.EntryType == PackagePlanEntryType.Material));
        Assert.IsFalse(plan.Entries.Any(entry => entry.Status == PackagePlanEntryStatus.SourceEnumerationFailed));
        AssertDestination(plan, PackagePlanEntryType.Material, @"materials\models\Creator\props\chair.vmt");
    }

    [TestMethod]
    public void ExtraOverlapWithEquivalentDefaultRootMappingRetainsDistinctIntent()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl");
        fileSystem.AddFile(@"E:\Release\docs\readme.txt", Array.Empty<byte>());
        var project = SimpleProject();
        project.Extras.Add(ExtraFolder(@"E:\Release", DestinationOverrideKind.Root, string.Empty));
        project.Extras.Add(ExtraFolder(@"E:\Release\docs", DestinationOverrideKind.Root, string.Empty));

        var plan = CreatePlan(fileSystem, project);

        Assert.AreEqual(2, plan.Entries.Count(entry => entry.EntryType == PackagePlanEntryType.Extra));
    }

    [TestMethod]
    public void RedundantExtraChildWithIdenticalMappingIsNormalizedBeforeEnumeration()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl");
        fileSystem.AddFile(@"E:\Release\docs\readme.txt", Array.Empty<byte>());
        fileSystem.FailEnumeration(@"E:\Release\docs");
        var project = SimpleProject();
        project.Extras.Add(ExtraFolder(@"E:\Release", DestinationOverrideKind.Custom, "PackageRoot"));
        project.Extras.Add(ExtraFolder(@"E:\Release\docs", DestinationOverrideKind.Custom, @"PackageRoot\docs"));

        var plan = CreatePlan(fileSystem, project);

        Assert.AreEqual(1, plan.Entries.Count(entry => entry.EntryType == PackagePlanEntryType.Extra));
        Assert.IsFalse(plan.Entries.Any(entry => entry.Status == PackagePlanEntryStatus.SourceEnumerationFailed));
        Assert.IsTrue(plan.Entries.Any(entry => entry.EntryType == PackagePlanEntryType.Extra
            && entry.DestinationRelativePath == @"PackageRoot\docs\readme.txt"));
    }

    [TestMethod]
    public void DifferentlyMappedExtraChildIsRetainedAndIndependentlyEnumerated()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl");
        fileSystem.AddFile(@"E:\Release\docs\readme.txt", Array.Empty<byte>());
        fileSystem.FailEnumeration(@"E:\Release\docs");
        var project = SimpleProject();
        project.Extras.Add(ExtraFolder(@"E:\Release", DestinationOverrideKind.Custom, "PackageRoot"));
        project.Extras.Add(ExtraFolder(@"E:\Release\docs", DestinationOverrideKind.Custom, @"DifferentDocs"));

        var plan = CreatePlan(fileSystem, project);

        Assert.IsTrue(plan.Entries.Any(entry => entry.EntryType == PackagePlanEntryType.Extra
            && entry.DestinationRelativePath == @"PackageRoot\docs\readme.txt"));
        Assert.IsTrue(plan.Entries.Any(entry => entry.EntryType == PackagePlanEntryType.Extra
            && entry.Status == PackagePlanEntryStatus.SourceEnumerationFailed
            && entry.SourcePath == @"E:\Release\docs"));
    }

    [TestMethod]
    public void ChildWithOverrideIsRetainedDuringOverlapNormalization()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl");
        fileSystem.AddFile(@"E:\SFM\materials\models\Creator\props\chair.vmt", Array.Empty<byte>());
        var project = SimpleProject();
        project.MaterialSources.Clear();
        project.MaterialSources.Add(MaterialFolder(@"E:\SFM\materials\models\Creator"));
        var child = MaterialFolder(@"E:\SFM\materials\models\Creator\props");
        child.DestinationOverride = new DestinationOverride
        {
            Kind = DestinationOverrideKind.Custom,
            RelativePath = @"materials\custom"
        };
        project.MaterialSources.Add(child);

        var plan = CreatePlan(fileSystem, project);

        Assert.AreEqual(2, plan.Entries.Count(entry => entry.EntryType == PackagePlanEntryType.Material));
        Assert.IsTrue(plan.Entries.Any(entry => entry.DestinationRelativePath == @"materials\custom\chair.vmt"));
    }

    [TestMethod]
    public void OverlapNormalizationIsDeterministic()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl");
        fileSystem.AddFile(@"E:\SFM\materials\models\Creator\props\chair.vmt", Array.Empty<byte>());
        var project = SimpleProject();
        project.MaterialSources.Clear();
        project.MaterialSources.Add(MaterialFolder(@"E:\SFM\materials\models\Creator"));
        project.MaterialSources.Add(MaterialFolder(@"E:\SFM\materials\models\Creator\props"));

        CollectionAssert.AreEqual(Signature(CreatePlan(fileSystem, project)), Signature(CreatePlan(fileSystem, project)));
    }

    [TestMethod]
    public void SharedFileFromPlannedExtraAttachesNoticeWithoutLegacyReadmeSection()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl");
        fileSystem.AddFile(@"E:\Release\sfm_defaultanimationgroups.txt", Array.Empty<byte>());
        var project = SimpleProject();
        project.Extras.Add(ExtraFile(@"E:\Release\sfm_defaultanimationgroups.txt", DestinationOverrideKind.Root, string.Empty));

        var plan = CreatePlan(fileSystem, project);
        var extra = plan.Entries.Single(entry => entry.EntryType == PackagePlanEntryType.Extra);
        var readme = ReadmeEntries(plan).Single();

        Assert.AreEqual(1, extra.RiskMetadata.SharedFileNotices.Count);
        Assert.IsTrue(plan.SharedFileNotices.Any());
        Assert.IsFalse(readme.TextContent!.Contains("SFM CONTROL GROUPS", StringComparison.Ordinal));
    }

    [TestMethod]
    public void SharedFileFromRecursivelyExpandedExtraFolderIsDetected()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl");
        fileSystem.AddFile(@"E:\Release\cfg\sfm_defaultanimationgroups.txt", Array.Empty<byte>());
        var project = SimpleProject();
        project.Extras.Add(ExtraFolder(@"E:\Release", DestinationOverrideKind.Root, string.Empty));

        var plan = CreatePlan(fileSystem, project);

        Assert.IsTrue(plan.SharedFileNotices.Any(notice => notice.CandidateId.StartsWith("extra:", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void ControlGroupsLegacyProjectOptionDoesNotAffectGeneratedReadme()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl");
        fileSystem.AddFile(@"E:\Release\sfm_defaultanimationgroups.txt", Array.Empty<byte>());
        var project = SimpleProject();
        project.Readme.IncludeControlGroupsInfo = true;
        project.Extras.Add(ExtraFile(@"E:\Release\sfm_defaultanimationgroups.txt", DestinationOverrideKind.Root, string.Empty));

        var plan = CreatePlan(fileSystem, project);

        Assert.IsFalse(ReadmeEntries(plan).Single().TextContent!.Contains("SFM CONTROL GROUPS", StringComparison.Ordinal));
    }

    [TestMethod]
    public void GeneratedReadmeProducesExactlyOneReadmeEntryWithRenamedModelPath()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl");

        var plan = CreatePlan(fileSystem, SimpleProject());
        var readme = ReadmeEntries(plan).Single();

        Assert.AreEqual(ReadmePlanEntryKind.GeneratedText, readme.ReadmeKind);
        StringAssert.Contains(readme.TextContent!, @"models\Creator\chair\chair_release.mdl");
    }

    [TestMethod]
    public void GeneratedReadmeModelListIncludesOnlyResolvedMdlDestinations()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl", "chair.vvd", "chair.dx90.vtx", "chair.phy");
        fileSystem.AddFile(@"E:\SFM\game\usermod\materials\models\Creator\chair\chair.vmt", Array.Empty<byte>());

        var readme = ReadmeEntries(CreatePlan(fileSystem, SimpleProject())).Single();

        StringAssert.Contains(readme.TextContent!, "MODEL INCLUDED\r\n--------------\r\n- models\\Creator\\chair\\chair_release.mdl");
        Assert.IsFalse(readme.TextContent.Contains("chair_release.vvd", StringComparison.Ordinal));
        Assert.IsFalse(readme.TextContent.Contains("chair_release.dx90.vtx", StringComparison.Ordinal));
        Assert.IsFalse(readme.TextContent.Contains("chair_release.phy", StringComparison.Ordinal));
        Assert.IsFalse(readme.TextContent.Contains("chair.vmt", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ImportedReadmeProducesOneReadmeEntryWithCopyIntent()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl");
        fileSystem.AddFile(@"E:\Docs\README.txt", Encoding.UTF8.GetBytes("Imported"));
        var project = SimpleProject();
        project.Readme.Mode = ReadmeMode.Custom;
        project.Readme.CustomSource = ReadmeCustomSource.ImportedFile;
        project.Readme.ImportedReadmePath = @"E:\Docs\README.txt";

        var readme = ReadmeEntries(CreatePlan(fileSystem, project)).Single();

        Assert.AreEqual(ReadmePlanEntryKind.ImportedFile, readme.ReadmeKind);
        Assert.AreEqual(@"E:\Docs\README.txt", readme.SourcePath);
        Assert.AreEqual("Imported", readme.TextContent);
    }

    [TestMethod]
    public void ProjectTextCustomReadmeProducesOneReadmeEntry()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl");
        var project = SimpleProject();
        project.Readme.Mode = ReadmeMode.Custom;
        project.Readme.CustomSource = ReadmeCustomSource.ProjectText;
        project.Readme.CustomReadmeText = "Custom text";

        var readme = ReadmeEntries(CreatePlan(fileSystem, project)).Single();

        Assert.AreEqual(ReadmePlanEntryKind.CustomText, readme.ReadmeKind);
        Assert.AreEqual("Custom text", readme.TextContent);
    }

    [TestMethod]
    public void NoReadmeProducesNoReadmeEntry()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl");
        var project = SimpleProject();
        project.Readme.Mode = ReadmeMode.None;

        Assert.AreEqual(0, ReadmeEntries(CreatePlan(fileSystem, project)).Count);
    }

    [TestMethod]
    public void MultipleReadmeModelPathsUseProjectOrder()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl");
        fileSystem.AddFile(@"E:\SFM\game\usermod\models\Creator\a\a.mdl", Array.Empty<byte>());
        var project = SimpleProject();
        project.Models.Add(AdditionalModel("a", Guid.Parse("22222222-2222-2222-2222-222222222222")));

        var readme = ReadmeEntries(CreatePlan(fileSystem, project)).Single();

        StringAssert.Contains(readme.TextContent!, "- models\\Creator\\chair\\chair_release.mdl\r\n- models\\Creator\\a\\a_release.mdl");
    }

    [TestMethod]
    public void TwoDistinctSourcesResolvingToSameDestinationBothRemainVisible()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl");
        fileSystem.AddFile(@"E:\SFM\materials\models\foo\body.vtf", Array.Empty<byte>());
        fileSystem.AddFile(@"D:\backup\materials\models\foo\body.vtf", Array.Empty<byte>());
        var project = SimpleProject();
        project.MaterialSources.Clear();
        project.MaterialSources.Add(MaterialFile(@"E:\SFM\materials\models\foo\body.vtf", Guid.Parse("44444444-4444-4444-4444-444444444444")));
        project.MaterialSources.Add(MaterialFile(@"D:\backup\materials\models\foo\body.vtf", Guid.Parse("55555555-5555-5555-5555-555555555555")));

        var plan = CreatePlan(fileSystem, project);
        var collisions = plan.Entries.Where(entry => entry.DestinationRelativePath == @"materials\models\foo\body.vtf").ToArray();

        Assert.AreEqual(2, collisions.Length);
        Assert.AreNotEqual(collisions[0].SourcePath, collisions[1].SourcePath);
    }

    [TestMethod]
    public void FinalEntryCollectionCannotBeExternallyMutated()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl");
        var plan = CreatePlan(fileSystem, SimpleProject());

        Assert.IsFalse(plan.Entries is List<PackagePlanEntry>);
        Assert.ThrowsExactly<NotSupportedException>(() => ((IList<PackagePlanEntry>)plan.Entries).Add(Entry(plan, PackagePlanEntryType.Model)));
    }

    [TestMethod]
    public void ChangingProjectAfterPlanningDoesNotMutateExistingPlan()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl");
        var project = SimpleProject();
        var plan = CreatePlan(fileSystem, project);

        project.Models[0].ReleaseStem = "changed";
        project.AssetName = "Changed";

        AssertDestination(plan, PackagePlanEntryType.Model, @"models\Creator\chair\chair_release.mdl");
        StringAssert.Contains(ReadmeEntries(plan).Single().TextContent!, "CHAIR PROP");
    }

    [TestMethod]
    public void ChangingSourceInputsAfterPlanningDoesNotMutateExistingPlan()
    {
        var fileSystem = new FakeFileSystem();
        AddModel(fileSystem, "chair.mdl");
        var plan = CreatePlan(fileSystem, SimpleProject());

        fileSystem.AddFile(@"E:\SFM\game\usermod\models\Creator\chair\chair.vvd", Array.Empty<byte>());

        Assert.AreEqual(0, plan.Entries.Count(entry => entry.EntryType == PackagePlanEntryType.ModelCompanion));
    }

    [TestMethod]
    public void FilesystemEnumerationOrderDoesNotChangePlanOrdering()
    {
        var first = new FakeFileSystem();
        AddModel(first, "chair.mdl");
        first.AddFile(@"E:\SFM\game\usermod\materials\models\Creator\chair\b.vtf", Array.Empty<byte>());
        first.AddFile(@"E:\SFM\game\usermod\materials\models\Creator\chair\a.vtf", Array.Empty<byte>());
        var second = new FakeFileSystem();
        AddModel(second, "chair.mdl");
        second.AddFile(@"E:\SFM\game\usermod\materials\models\Creator\chair\a.vtf", Array.Empty<byte>());
        second.AddFile(@"E:\SFM\game\usermod\materials\models\Creator\chair\b.vtf", Array.Empty<byte>());

        CollectionAssert.AreEqual(Signature(CreatePlan(first, SimpleProject())), Signature(CreatePlan(second, SimpleProject())));
    }

    [TestMethod]
    public void PlanningDoesNotMutatePhysicalSourceFixtures()
    {
        var root = Path.Combine(Path.GetTempPath(), "SfmPackageBuilderPlannerTests", Guid.NewGuid().ToString("N"));
        var modelDir = Path.Combine(root, "models", "Creator", "chair");
        var materialDir = Path.Combine(root, "materials", "models", "Creator", "chair");
        Directory.CreateDirectory(modelDir);
        Directory.CreateDirectory(materialDir);
        var modelPath = Path.Combine(modelDir, "chair.mdl");
        var materialPath = Path.Combine(materialDir, "chair.vmt");
        File.WriteAllText(modelPath, "model");
        File.WriteAllText(materialPath, "material");
        File.SetAttributes(modelPath, FileAttributes.ReadOnly);

        try
        {
            var beforeNames = Directory.GetFileSystemEntries(modelDir).Concat(Directory.GetFileSystemEntries(materialDir)).Select(Path.GetFileName).Order().ToArray();
            var beforeBytes = new[] { File.ReadAllBytes(modelPath), File.ReadAllBytes(materialPath) };
            var beforeAttributes = new[] { File.GetAttributes(modelPath), File.GetAttributes(materialPath) };
            var project = SimpleProject();
            project.Models[0].SourceMdlPath = modelPath;
            project.MaterialSources[0].SourcePath = materialDir;

            _ = CreatePlan(new PhysicalFileSystem(), project);

            var afterNames = Directory.GetFileSystemEntries(modelDir).Concat(Directory.GetFileSystemEntries(materialDir)).Select(Path.GetFileName).Order().ToArray();
            CollectionAssert.AreEqual(beforeNames, afterNames);
            CollectionAssert.AreEqual(beforeBytes.Select(Convert.ToBase64String).ToArray(), new[] { File.ReadAllBytes(modelPath), File.ReadAllBytes(materialPath) }.Select(Convert.ToBase64String).ToArray());
            CollectionAssert.AreEqual(beforeAttributes, new[] { File.GetAttributes(modelPath), File.GetAttributes(materialPath) });
        }
        finally
        {
            File.SetAttributes(modelPath, FileAttributes.Normal);
            Directory.Delete(root, recursive: true);
        }
    }

    private static PackagePlan CreatePlan(FakeFileSystem fileSystem, PackageProject project) =>
        new PackagePlanner(fileSystem).CreatePlan(project);

    private static PackagePlan CreatePlan(PhysicalFileSystem fileSystem, PackageProject project) =>
        new PackagePlanner(fileSystem).CreatePlan(project);

    private static PackageProject SimpleProject(string stem = "chair", string releaseStem = "chair_release")
    {
        return new PackageProject
        {
            AssetName = "Chair Prop",
            CurrentVersion = "1.0",
            ChangesThisVersion = new List<string> { "Initial release." },
            Models = new List<ModelEntry>
            {
                new()
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Role = ModelRole.Primary,
                    SourceMdlPath = $@"E:\SFM\game\usermod\models\Creator\{stem}\{stem}.mdl",
                    SourceStem = stem,
                    ReleaseStem = releaseStem
                }
            },
            MaterialSources = new List<SourceEntry>
            {
                MaterialFolder(@"E:\SFM\game\usermod\materials\models\Creator\chair")
            },
            Readme = new ReadmeConfig
            {
                Mode = ReadmeMode.Generated
            }
        };
    }

    private static ModelEntry AdditionalModel(string stem, Guid id) => new()
    {
        Id = id,
        Role = ModelRole.Additional,
        SourceMdlPath = $@"E:\SFM\game\usermod\models\Creator\{stem}\{stem}.mdl",
        SourceStem = stem,
        ReleaseStem = $"{stem}_release"
    };

    private static SourceEntry MaterialFolder(string path) => new()
    {
        Id = Guid.NewGuid(),
        Kind = SourceEntryKind.Material,
        SourcePath = path,
        IsFolder = true,
        IncludeRecursively = true
    };

    private static SourceEntry MaterialFile(string path, Guid id) => new()
    {
        Id = id,
        Kind = SourceEntryKind.Material,
        SourcePath = path,
        IsFolder = false
    };

    private static SourceEntry ExtraFile(string path, DestinationOverrideKind kind, string relativePath) => new()
    {
        Id = Guid.NewGuid(),
        Kind = SourceEntryKind.Extra,
        SourcePath = path,
        IsFolder = false,
        DestinationOverride = new DestinationOverride
        {
            Kind = kind,
            RelativePath = relativePath
        }
    };

    private static SourceEntry ExtraFolder(string path, DestinationOverrideKind kind, string relativePath) => new()
    {
        Id = Guid.NewGuid(),
        Kind = SourceEntryKind.Extra,
        SourcePath = path,
        IsFolder = true,
        IncludeRecursively = true,
        DestinationOverride = new DestinationOverride
        {
            Kind = kind,
            RelativePath = relativePath
        }
    };

    private static void AddModel(FakeFileSystem fileSystem, params string[] names)
    {
        foreach (var name in names)
        {
            var stem = Path.GetFileNameWithoutExtension(name);
            if (name.EndsWith(".dx90.vtx", StringComparison.OrdinalIgnoreCase))
            {
                stem = name[..^".dx90.vtx".Length];
            }
            else if (name.EndsWith(".dx80.vtx", StringComparison.OrdinalIgnoreCase))
            {
                stem = name[..^".dx80.vtx".Length];
            }
            else if (name.EndsWith(".sw.vtx", StringComparison.OrdinalIgnoreCase))
            {
                stem = name[..^".sw.vtx".Length];
            }

            fileSystem.AddFile($@"E:\SFM\game\usermod\models\Creator\{stem}\{name}", Array.Empty<byte>());
        }
    }

    private static PackagePlanEntry Entry(PackagePlan plan, PackagePlanEntryType entryType)
    {
        return plan.Entries.First(entry => entry.EntryType == entryType);
    }

    private static List<PackagePlanEntry> ReadmeEntries(PackagePlan plan)
    {
        return plan.Entries.Where(entry => entry.EntryType == PackagePlanEntryType.Readme).ToList();
    }

    private static void AssertDestination(PackagePlan plan, PackagePlanEntryType entryType, string destination)
    {
        Assert.IsTrue(
            plan.Entries.Any(entry => entry.EntryType == entryType && entry.DestinationRelativePath == destination),
            $"Expected {entryType} destination '{destination}'. Actual: {string.Join(", ", plan.Entries.Select(entry => entry.DestinationRelativePath))}");
    }

    private static string[] Signature(PackagePlan plan)
    {
        return plan.Entries
            .Select(entry => $"{entry.EntryType}|{entry.Status}|{entry.SourcePath}|{entry.DestinationRelativePath}|{entry.TextContent}")
            .ToArray();
    }
}
