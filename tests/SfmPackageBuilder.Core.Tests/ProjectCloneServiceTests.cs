using Microsoft.VisualStudio.TestTools.UnitTesting;
using SfmPackageBuilder.Core.Domain;

namespace SfmPackageBuilder.Core.Tests;

[TestClass]
public sealed class ProjectCloneServiceTests
{
    [TestMethod]
    public void OrdinaryNewProjectUsesMachineCreatorDefaults()
    {
        var settings = new AppSettings
        {
            CreatorDefaults = new CreatorDefaults
            {
                Author = "Creator",
                Website = "https://example.test",
                License = "CC0"
            }
        };

        var project = new ProjectCloneService().CreateNewProject(settings);

        Assert.AreEqual("Creator", project.Readme.Author);
        Assert.AreEqual("https://example.test", project.Readme.Website);
        Assert.AreEqual("CC0", project.Readme.License);
        Assert.AreEqual(0, project.Models.Count);
        Assert.AreEqual(0, project.MaterialSources.Count);
        Assert.AreEqual(0, project.Extras.Count);
    }

    [TestMethod]
    public void NewFromCurrentPreservesReusableFieldsAndResetsAssetSpecificFields()
    {
        var current = CompleteProject();

        var clone = new ProjectCloneService().CreateFromCurrent(current);

        Assert.AreEqual("Creator", clone.Readme.Author);
        Assert.AreEqual("https://example.test", clone.Readme.Website);
        Assert.AreEqual("Use freely.", clone.Readme.License);
        Assert.AreEqual("Docs: https://example.test/docs", clone.Readme.AdditionalResources);
        Assert.AreEqual(1, clone.MaterialSources.Count);
        Assert.AreEqual(1, clone.Extras.Count);
        Assert.AreNotEqual(current.MaterialSources[0].Id, clone.MaterialSources[0].Id);
        Assert.AreNotEqual(current.Extras[0].Id, clone.Extras[0].Id);
        Assert.AreEqual(current.MaterialSources[0].SourcePath, clone.MaterialSources[0].SourcePath);
        Assert.AreEqual(current.Extras[0].DestinationOverride!.RelativePath, clone.Extras[0].DestinationOverride!.RelativePath);

        Assert.AreEqual(string.Empty, clone.AssetName);
        Assert.AreEqual(string.Empty, clone.CurrentVersion);
        Assert.AreEqual(0, clone.Models.Count);
        Assert.AreEqual(0, clone.ReleaseHistory.Count);
        Assert.AreEqual(0, clone.BuildHistory.Count);
        Assert.AreEqual(string.Empty, clone.Credits);
        Assert.AreEqual(string.Empty, clone.ArchiveName);
        Assert.AreEqual(0, clone.InformationalState.Count);
    }

    [TestMethod]
    public void NewFromCurrentPreservesMaterialAndExtraSourceReferencesAsDeepCopies()
    {
        var current = CompleteProject();

        var clone = new ProjectCloneService().CreateFromCurrent(current);

        var originalMaterial = current.MaterialSources[0];
        var clonedMaterial = clone.MaterialSources[0];
        var originalExtra = current.Extras[0];
        var clonedExtra = clone.Extras[0];

        Assert.AreNotEqual(originalMaterial.Id, clonedMaterial.Id);
        Assert.AreNotEqual(originalExtra.Id, clonedExtra.Id);

        Assert.IsNotNull(clonedMaterial.SourceReference);
        Assert.AreEqual(@"D:\materials", clonedMaterial.SourceReference!.AbsolutePath);
        Assert.AreEqual(@"sources\materials", clonedMaterial.SourceReference.RelativePath);
        Assert.AreEqual(@"D:\oldRoot", clonedMaterial.SourceReference.RecoveryRootPath);
        Assert.AreEqual(@"materials", clonedMaterial.SourceReference.RecoveryRelativePath);

        Assert.IsNotNull(clonedExtra.SourceReference);
        Assert.AreEqual(@"D:\docs\readme.pdf", clonedExtra.SourceReference!.AbsolutePath);
        Assert.AreEqual(@"docs\readme.pdf", clonedExtra.SourceReference.RelativePath);
        Assert.AreEqual(@"D:\docs", clonedExtra.SourceReference.RecoveryRootPath);
        Assert.AreEqual("readme.pdf", clonedExtra.SourceReference.RecoveryRelativePath);

        Assert.IsNotNull(clonedExtra.DestinationOverride);
        Assert.AreEqual(DestinationOverrideKind.Custom, clonedExtra.DestinationOverride!.Kind);
        Assert.AreEqual("Docs", clonedExtra.DestinationOverride.RelativePath);

        Assert.AreNotSame(originalMaterial.SourceReference, clonedMaterial.SourceReference);
        Assert.AreNotSame(originalExtra.SourceReference, clonedExtra.SourceReference);

        originalMaterial.SourceReference!.AbsolutePath = @"E:\changed\materials";
        originalMaterial.SourceReference.RelativePath = @"changed\materials";
        originalMaterial.SourceReference.RecoveryRootPath = @"E:\changed";
        originalMaterial.SourceReference.RecoveryRelativePath = "materials";
        originalExtra.SourceReference!.AbsolutePath = @"E:\changed\readme.pdf";
        originalExtra.SourceReference.RelativePath = @"changed\readme.pdf";
        originalExtra.SourceReference.RecoveryRootPath = @"E:\changed";
        originalExtra.SourceReference.RecoveryRelativePath = "readme.pdf";

        Assert.AreEqual(@"D:\materials", clonedMaterial.SourceReference.AbsolutePath);
        Assert.AreEqual(@"sources\materials", clonedMaterial.SourceReference.RelativePath);
        Assert.AreEqual(@"D:\oldRoot", clonedMaterial.SourceReference.RecoveryRootPath);
        Assert.AreEqual(@"materials", clonedMaterial.SourceReference.RecoveryRelativePath);
        Assert.AreEqual(@"D:\docs\readme.pdf", clonedExtra.SourceReference.AbsolutePath);
        Assert.AreEqual(@"docs\readme.pdf", clonedExtra.SourceReference.RelativePath);
        Assert.AreEqual(@"D:\docs", clonedExtra.SourceReference.RecoveryRootPath);
        Assert.AreEqual("readme.pdf", clonedExtra.SourceReference.RecoveryRelativePath);
    }

    [TestMethod]
    public void NewFromCurrentReadmeModesFollowSafeResetRules()
    {
        var cases = new[]
        {
            (OriginalMode: ReadmeMode.Generated, OriginalSource: ReadmeCustomSource.ProjectText, ClonedMode: ReadmeMode.Generated, ClonedSource: ReadmeCustomSource.ProjectText),
            (OriginalMode: ReadmeMode.None, OriginalSource: ReadmeCustomSource.ProjectText, ClonedMode: ReadmeMode.None, ClonedSource: ReadmeCustomSource.ProjectText),
            (OriginalMode: ReadmeMode.Custom, OriginalSource: ReadmeCustomSource.ProjectText, ClonedMode: ReadmeMode.Custom, ClonedSource: ReadmeCustomSource.ProjectText),
            (OriginalMode: ReadmeMode.Custom, OriginalSource: ReadmeCustomSource.ImportedFile, ClonedMode: ReadmeMode.Custom, ClonedSource: ReadmeCustomSource.ImportedFile)
        };

        foreach (var testCase in cases)
        {
            var current = CompleteProject();
            current.Readme.Mode = testCase.OriginalMode;
            current.Readme.CustomSource = testCase.OriginalSource;
            current.Readme.CustomReadmeText = "Old authored text";
            current.Readme.ImportedReadmePath = @"D:\old\README.txt";
            current.Readme.ImportedReadmeReference = new PersistedSourceReference { AbsolutePath = @"D:\old\README.txt" };
            current.Readme.CustomReadmeGeneratedFromFingerprint = "old-fingerprint";

            var clone = new ProjectCloneService().CreateFromCurrent(current);

            Assert.AreEqual(testCase.ClonedMode, clone.Readme.Mode);
            Assert.AreEqual(testCase.ClonedSource, clone.Readme.CustomSource);
            Assert.AreEqual(
                testCase.OriginalMode == ReadmeMode.Custom && testCase.OriginalSource == ReadmeCustomSource.ProjectText ? string.Empty : null,
                clone.Readme.CustomReadmeText);
            Assert.IsNull(clone.Readme.ImportedReadmePath);
            Assert.IsNull(clone.Readme.ImportedReadmeReference);
            Assert.IsNull(clone.Readme.CustomReadmeGeneratedFromFingerprint);
        }
    }

    private static PackageProject CompleteProject()
    {
        var project = new PackageProject
        {
            AssetName = "Old asset",
            CurrentVersion = "1.2",
            ChangesThisVersion = new List<string> { "Changed." },
            Credits = "Original creator should not carry to clone.",
            ArchiveName = "old.zip",
            Readme = new ReadmeConfig
            {
                Mode = ReadmeMode.Custom,
                CustomSource = ReadmeCustomSource.ProjectText,
                Description = "Old model description",
                Author = "Creator",
                Website = "https://example.test",
                License = "Use freely.",
                AdditionalResources = "Docs: https://example.test/docs",
                CustomReadmeText = "Old authored text",
                CustomReadmeGeneratedFromFingerprint = "fingerprint"
            }
        };
        project.Models.Add(new ModelEntry
        {
            Role = ModelRole.Primary,
            SourceMdlPath = @"D:\models\old.mdl",
            SourceStem = "old",
            ReleaseStem = "release",
            Companions = new List<ModelCompanionSelection>
            {
                new() { RuntimeSuffix = ".vvd", SourcePath = @"D:\models\old.vvd", UserSelection = CompanionUserSelection.Include }
            }
        });
        project.Models.Add(new ModelEntry { Role = ModelRole.Additional, SourceMdlPath = @"D:\models\extra.mdl" });
        project.MaterialSources.Add(new SourceEntry
        {
            Kind = SourceEntryKind.Material,
            SourcePath = @"D:\materials",
            IsFolder = true,
            SourceReference = new PersistedSourceReference
            {
                AbsolutePath = @"D:\materials",
                RelativePath = @"sources\materials",
                RecoveryRootPath = @"D:\oldRoot",
                RecoveryRelativePath = @"materials"
            }
        });
        project.Extras.Add(new SourceEntry
        {
            Kind = SourceEntryKind.Extra,
            SourcePath = @"D:\docs\readme.pdf",
            DestinationOverride = new DestinationOverride { Kind = DestinationOverrideKind.Custom, RelativePath = "Docs" },
            SourceReference = new PersistedSourceReference
            {
                AbsolutePath = @"D:\docs\readme.pdf",
                RelativePath = @"docs\readme.pdf",
                RecoveryRootPath = @"D:\docs",
                RecoveryRelativePath = "readme.pdf"
            }
        });
        project.ReleaseHistory.Add(new ReleaseRecord { Version = "1.1", Changes = new List<string> { "Earlier." } });
        project.BuildHistory.Add(new BuildRecord { Version = "1.2", ArchiveName = "old.zip", BuildTimestamp = DateTimeOffset.Parse("2026-01-01T00:00:00-05:00") });
        project.InformationalState.Add(new ProjectNoticeState { Key = "old" });
        return project;
    }
}
