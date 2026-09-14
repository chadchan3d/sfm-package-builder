using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SfmPackageBuilder.Core.Domain;
using SfmPackageBuilder.Core.Readme;

namespace SfmPackageBuilder.Core.Tests;

[TestClass]
public sealed class ReadmeCoreTests
{
    [TestMethod]
    public void BasicGeneratedPropReadmeMatchesSnapshot()
    {
        var text = Generate(BasicProject(), ModelPath("models\\Creator\\props\\chair\\chair.mdl"));

        const string expected =
            "CHAIR PROP\r\n" +
            "==========\r\n" +
            "Version 1.0\r\n" +
            "\r\n" +
            "INSTALLATION\r\n" +
            "------------\r\n" +
            "Copy these folders into your Source Filmmaker usermod folder:\r\n" +
            "- models\r\n" +
            "\r\n" +
            "Default location:\r\n" +
            "...\\SourceFilmmaker\\game\\usermod\r\n" +
            "\r\n" +
            "If Windows asks to merge folders or replace files when updating, allow it.\r\n" +
            "\r\n" +
            "MODEL INCLUDED\r\n" +
            "--------------\r\n" +
            "- models\\Creator\\props\\chair\\chair.mdl\r\n" +
            "\r\n" +
            "CHANGELOG\r\n" +
            "---------\r\n" +
            "1.0\r\n" +
            "Initial release.\r\n";

        Assert.AreEqual(expected, text);
    }

    [TestMethod]
    public void AssetNameAppearsAsTitle()
    {
        var text = Generate(BasicProject(assetName: "Cereal Box"), ModelPath("models\\food\\cereal.mdl"));

        StringAssert.StartsWith(text, "CEREAL BOX\r\n==========");
    }

    [TestMethod]
    public void ExplicitAssetNameWinsOverModelDerivedTitle()
    {
        var project = BasicProject(assetName: "Pizza Girl");
        project.Models.Add(new ModelEntry { Role = ModelRole.Primary, SourceStem = "goblin_source", ReleaseStem = "goblin_release" });

        var text = Generate(project, ModelPath("models\\characters\\goblin_release.mdl"));

        StringAssert.StartsWith(text, "PIZZA GIRL\r\n==========");
        Assert.IsFalse(text.StartsWith("GOBLIN RELEASE", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BlankAssetNameFallsBackToPrimaryReleaseStem()
    {
        var project = BasicProject(assetName: string.Empty);
        project.Models.Add(new ModelEntry { Role = ModelRole.Primary, SourceStem = "house_v13", ReleaseStem = "goblin" });

        var text = Generate(project, ModelPath("models\\characters\\goblin.mdl"));

        StringAssert.StartsWith(text, "GOBLIN\r\n======");
        Assert.IsFalse(text.Contains("UNTITLED PACKAGE", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BlankAssetNameFallbackHumanizesUnderscoreReleaseStem()
    {
        var project = BasicProject(assetName: string.Empty);
        project.Models.Add(new ModelEntry { Role = ModelRole.Primary, ReleaseStem = "outdoor_chaise_lounge" });

        var text = Generate(project, ModelPath("models\\props\\outdoor_chaise_lounge.mdl"));

        StringAssert.StartsWith(text, "OUTDOOR CHAISE LOUNGE\r\n=====================");
    }

    [TestMethod]
    public void BlankAssetNameFallbackUsesReleaseStemInsteadOfSourceStem()
    {
        var project = BasicProject(assetName: string.Empty);
        project.Models.Add(new ModelEntry { Role = ModelRole.Primary, SourceStem = "house_v13", ReleaseStem = "house_chadchan3d" });

        var text = Generate(project, ModelPath("models\\props\\house_chadchan3d.mdl"));

        StringAssert.StartsWith(text, "HOUSE CHADCHAN3D\r\n================");
        Assert.IsFalse(text.StartsWith("HOUSE V13", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BlankAssetNameKeepsDefensiveUntitledFallbackWhenNoPrimaryIdentityExists()
    {
        var text = Generate(BasicProject(assetName: string.Empty), ModelPath("models\\chair.mdl"));

        StringAssert.StartsWith(text, "UNTITLED PACKAGE\r\n================");
    }

    [TestMethod]
    public void ModelDerivedTitleDoesNotMutateProjectMetadata()
    {
        var project = BasicProject(assetName: string.Empty);
        project.Models.Add(new ModelEntry { Role = ModelRole.Primary, ReleaseStem = "goblin" });

        _ = Generate(project, ModelPath("models\\characters\\goblin.mdl"));

        Assert.AreEqual(string.Empty, project.AssetName);
    }

    [TestMethod]
    public void VersionIsRenderedImmediatelyBelowTitleWhenPresent()
    {
        var text = Generate(BasicProject(version: "2.5 beta"), ModelPath("models\\chair.mdl"));

        StringAssert.StartsWith(text, "CHAIR PROP\r\n==========\r\nVersion 2.5 beta\r\n\r\nINSTALLATION");
        Assert.IsFalse(text.Contains("VERSION\r\n-------", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BlankVersionOmitsVersionLine()
    {
        var text = Generate(BasicProject(version: string.Empty), ModelPath("models\\chair.mdl"));

        StringAssert.StartsWith(text, "CHAIR PROP\r\n==========\r\n\r\nINSTALLATION");
        Assert.IsFalse(text.Contains("Version \r\n", StringComparison.Ordinal));
    }

    [TestMethod]
    public void DescriptionAppearsAsIntroductoryProseWithoutHeading()
    {
        var project = BasicProject();
        project.Readme.Description = "A useful prop.";

        var text = Generate(project, ModelPath("models\\chair.mdl"));

        StringAssert.StartsWith(text, "CHAIR PROP\r\n==========\r\nVersion 1.0\r\n\r\nA useful prop.\r\n\r\nINSTALLATION");
        Assert.IsFalse(text.Contains("DESCRIPTION\r\n-----------", StringComparison.Ordinal));
    }

    [TestMethod]
    public void EmptyDescriptionIsOmitted()
    {
        var project = BasicProject();
        project.Readme.Description = " ";

        var text = Generate(project, ModelPath("models\\chair.mdl"));

        Assert.IsFalse(text.Contains("DESCRIPTION", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("\r\n \r\n", StringComparison.Ordinal));
    }

    [TestMethod]
    public void GeneratedReadmeUsesCompactSectionSpacing()
    {
        var project = BasicProject();
        project.Readme.Author = "Creator";

        var text = Generate(project, ModelPath("models\\chair.mdl"));

        Assert.IsFalse(text.Contains("INSTALLATION\r\n------------\r\n\r\n", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("MODEL INCLUDED\r\n--------------\r\n\r\n", StringComparison.Ordinal));
        StringAssert.Contains(text, "INSTALLATION\r\n------------\r\nCopy these folders");
        StringAssert.Contains(text, "If Windows asks to merge folders or replace files when updating, allow it.\r\n\r\nMODEL INCLUDED");
        Assert.IsFalse(text.Contains("README.txt is for reference only.", StringComparison.Ordinal));
        StringAssert.Contains(text, "AUTHOR\r\n------\r\nCreator");
    }

    [TestMethod]
    public void InstallationSectionUsesConciseCurrentCopy()
    {
        var text = Generate(BasicProject(), ModelPath("models\\chair.mdl"));

        StringAssert.Contains(text, "INSTALLATION\r\n------------");
        StringAssert.Contains(text, "Copy these folders into your Source Filmmaker usermod folder:");
        StringAssert.Contains(text, "Default location:");
        StringAssert.Contains(text, @"...\SourceFilmmaker\game\usermod");
        Assert.IsFalse(text.Contains(@"...\SourceFilmmaker\game\usermod\", StringComparison.Ordinal));
        StringAssert.Contains(text, "If Windows asks to merge folders or replace files when updating, allow it.");
        Assert.IsFalse(text.Contains("README.txt is for reference only.", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("Copy these folders from the ZIP", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("Copy the folders themselves and keep everything inside them in place.", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("If Windows asks to merge folders, allow it.", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("If you're updating an earlier version of this package", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("README.txt is for reference and does not need to be copied into Source Filmmaker.", StringComparison.Ordinal));
    }

    [TestMethod]
    public void InstallationListsExactlyCurrentInstallRoots()
    {
        var text = Generate(BasicProject(), new ReadmeContext
        {
            ModelPaths = new[] { @"models\chair.mdl" },
            InstallRoots = new[] { "models", "materials", "scripts", "cfg" },
            PackageReadmeIncluded = true
        });

        StringAssert.Contains(text, "- models\r\n- materials\r\n- scripts\r\n- cfg");
        StringAssert.Contains(text, @"...\SourceFilmmaker\game\usermod");
        StringAssert.Contains(text, ReadmeGenerator.UpdateInstallSentence);
        Assert.IsFalse(text.Contains("- maps", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("- sound", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("README.txt\\", StringComparison.Ordinal));
    }

    [TestMethod]
    public void InstallationRootsCanIncludeRecognizedRootsBeyondModelsAndMaterials()
    {
        var text = Generate(BasicProject(), new ReadmeContext
        {
            ModelPaths = new[] { @"models\chair.mdl" },
            InstallRoots = new[] { "models", "scripts", "sound" },
            PackageReadmeIncluded = true
        });

        StringAssert.Contains(text, "- models\r\n- scripts\r\n- sound");
        Assert.IsFalse(text.Contains("- materials", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("- maps", StringComparison.Ordinal));
    }

    [TestMethod]
    public void InstallationDoesNotTreatDocsAsInstallRoot()
    {
        var text = Generate(BasicProject(), new ReadmeContext
        {
            ModelPaths = new[] { @"models\chair.mdl" },
            InstallRoots = new[] { "models" },
            DocsFolderIncluded = true,
            PackageReadmeIncluded = true
        });

        Assert.IsFalse(text.Contains("Docs\\", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("README.txt is for reference only.", StringComparison.Ordinal));
    }

    [TestMethod]
    public void InstallationDoesNotEmitSupportingFileProse()
    {
        var text = Generate(BasicProject(), new ReadmeContext
        {
            ModelPaths = new[] { @"models\chair.mdl" },
            InstallRoots = new[] { "models" },
            OtherSupportingFilesIncluded = true,
            PackageReadmeIncluded = true
        });

        Assert.IsFalse(text.Contains("Other files in this ZIP are supporting files", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("README.txt is for reference only.", StringComparison.Ordinal));
    }

    [TestMethod]
    public void NoInstallRootGeneratesNeutralStatement()
    {
        var text = Generate(BasicProject(), new ReadmeContext());

        StringAssert.Contains(text, "This package contains no automatically recognized Source Filmmaker installation folders.");
        Assert.IsFalse(text.Contains("README.txt is for reference only.", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("Copy these folders into your Source Filmmaker usermod folder:", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("Copy everything", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void OneModelPathUsesSingularHeadingAndBullet()
    {
        var text = Generate(BasicProject(), ModelPath("models\\chair.mdl"));

        StringAssert.Contains(text, "MODEL INCLUDED\r\n--------------\r\n- models\\chair.mdl");
        Assert.IsFalse(text.Contains("MODELS INCLUDED\r\n---------------", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("\r\nMODEL\r\n-----", StringComparison.Ordinal));
    }

    [TestMethod]
    public void MultipleModelPathsUsePluralHeadingAndBullets()
    {
        var text = Generate(
            BasicProject(),
            ModelPath("models\\chair.mdl", "models\\shared\\hinge.mdl"));

        StringAssert.Contains(text, "MODELS INCLUDED\r\n---------------\r\n- models\\chair.mdl\r\n- models\\shared\\hinge.mdl");
        Assert.IsFalse(text.Contains("MODEL(S)", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ModelPathSectionIsAutomaticEvenIfLegacyFlagIsFalse()
    {
        var project = BasicProject();
        project.Readme.IncludeModelPath = false;

        var text = Generate(project, ModelPath("models\\chair.mdl"));

        StringAssert.Contains(text, "MODEL INCLUDED\r\n--------------\r\n- models\\chair.mdl");
    }

    [TestMethod]
    public void AuthorWebsiteCreditsUsageTermsAndLinksAreRendered()
    {
        var project = BasicProject();
        project.Readme.Author = "Creator";
        project.Readme.Website = "https://example.test";
        project.Credits = "Original mesh by Example Artist.";
        project.Readme.License = "Custom license.";
        project.Readme.AdditionalResources = "Includes PSD files.\r\nhttps://example.test/files";

        var text = Generate(project, ModelPath("models\\chair.mdl"));

        StringAssert.Contains(text, "AUTHOR\r\n------\r\nCreator");
        StringAssert.Contains(text, "WEBSITE\r\n-------\r\nhttps://example.test");
        StringAssert.Contains(text, "CREDITS\r\n-------\r\nOriginal mesh by Example Artist.");
        StringAssert.Contains(text, "USAGE TERMS\r\n-----------\r\nCustom license.");
        StringAssert.Contains(text, "LINKS & RESOURCES\r\n-----------------\r\nIncludes PSD files.\r\nhttps://example.test/files");
    }

    [TestMethod]
    public void EmptyOptionalSectionsAreOmitted()
    {
        var project = BasicProject(version: string.Empty, changes: Array.Empty<string>());
        project.Readme.Author = " ";
        project.Readme.Website = " ";
        project.Credits = " ";
        project.Readme.License = " ";
        project.Readme.AdditionalResources = " ";

        var text = Generate(project, ModelPath("models\\chair.mdl"));

        Assert.IsFalse(text.Contains("AUTHOR\r\n------", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("WEBSITE\r\n-------", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("CREDITS\r\n-------", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("USAGE TERMS\r\n-----------", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("LINKS & RESOURCES\r\n-----------------", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("CHANGELOG\r\n---------", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RenamedHeadingsDoNotUseOldNames()
    {
        var project = BasicProject();
        project.Readme.License = "Custom license.";
        project.Readme.AdditionalResources = "https://example.test";

        var text = Generate(project, ModelPath("models\\chair.mdl"));

        StringAssert.Contains(text, "USAGE TERMS\r\n-----------");
        StringAssert.Contains(text, "LINKS & RESOURCES\r\n-----------------");
        Assert.IsFalse(text.Contains("MODEL USAGE TERMS", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("ADDITIONAL LINKS & RESOURCES", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("ADDITIONAL RESOURCES", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("\r\nMODEL\r\n-----", StringComparison.Ordinal));
    }

    [TestMethod]
    public void AllOptionalFieldsSnapshot()
    {
        var project = BasicProject(version: "1.1", changes: new[] { "Improved posing." });
        project.Readme.Description = "A useful prop.";
        project.Readme.Author = "Creator";
        project.Readme.Website = "https://example.test";
        project.Credits = "Original mesh by Example Artist.";
        project.Readme.License = "Custom license.";
        project.Readme.AdditionalResources = "Includes PSD files.";

        var text = Generate(project, ModelPath("models\\chair.mdl"));

        AssertSnapshot("all_optional_fields", text);
    }

    [TestMethod]
    public void LegacySectionFlagsDoNotDisableAutomaticGeneratedSections()
    {
        var project = BasicProject();
        project.Readme.IncludeInstallInstructions = false;
        project.Readme.IncludeModelPath = false;
        project.Readme.IncludeVersionChanges = false;

        var text = Generate(project, ModelPath("models\\chair.mdl"));

        StringAssert.Contains(text, "INSTALLATION\r\n------------\r\nCopy these folders");
        StringAssert.Contains(text, "MODEL INCLUDED\r\n--------------\r\n- models\\chair.mdl");
        StringAssert.Contains(text, "CHANGELOG\r\n---------\r\n1.0\r\nInitial release.");
    }

    [TestMethod]
    public void VersionOnlyReleaseDoesNotEmitChangelog()
    {
        var text = Generate(BasicProject(version: "2.5", changes: Array.Empty<string>()), ModelPath("models\\chair.mdl"));

        StringAssert.Contains(text, "CHAIR PROP\r\n==========\r\nVersion 2.5");
        Assert.IsFalse(text.Contains("CHANGELOG\r\n---------", StringComparison.Ordinal));
    }

    [TestMethod]
    public void VersionWithChangesRendersChangelog()
    {
        var project = BasicProject(version: "1.1", changes: new[] { "Improved posing.", "Fixed texture path." });

        var text = Generate(project, ModelPath("models\\chair.mdl"));

        StringAssert.Contains(text, "CHANGELOG\r\n---------\r\n1.1\r\nImproved posing.\r\nFixed texture path.");
        Assert.IsFalse(text.Contains("- Improved posing.", StringComparison.Ordinal));
    }

    [TestMethod]
    public void AccumulatedChangelogRendersCurrentThenPriorInExplicitOrder()
    {
        var project = BasicProject(version: "two-ish", changes: new[] { "Current update." });
        project.ReleaseHistory.Add(new ReleaseRecord { Version = "1.1b", Changes = new List<string> { "Previous beta." } });
        project.ReleaseHistory.Add(new ReleaseRecord { Version = "1.0", Changes = new List<string> { "First release." } });

        var text = Generate(project, ModelPath("models\\chair.mdl"));

        StringAssert.Contains(text, "CHANGELOG\r\n---------\r\ntwo-ish\r\nCurrent update.\r\n\r\n1.1b\r\nPrevious beta.\r\n\r\n1.0\r\nFirst release.");
    }

    [TestMethod]
    public void GeneratedChangelogUsesReleaseHistoryOnlyAndIgnoresBuildHistory()
    {
        var project = BasicProject(version: "1.0", changes: new[] { "Initial release." });
        project.BuildHistory.Add(new BuildRecord
        {
            Version = "1.0",
            ArchiveName = "chair-internal-rebuild.zip",
            BuildTimestamp = DateTimeOffset.Parse("2026-01-01T10:00:00-05:00")
        });

        var text = Generate(project, ModelPath("models\\chair.mdl"));

        StringAssert.Contains(text, "CHANGELOG\r\n---------\r\n1.0\r\nInitial release.");
        Assert.IsFalse(text.Contains("chair-internal-rebuild.zip", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("2026-01-01", StringComparison.Ordinal));
    }

    [TestMethod]
    public void BlankChangesAreHandledWithoutPlaceholderProse()
    {
        var project = BasicProject(version: "1.0", changes: new[] { "", "  " });

        var text = Generate(project, ModelPath("models\\chair.mdl"));

        Assert.IsFalse(text.Contains("CHANGELOG\r\n---------", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("No changes", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void ChangelogOmitsEmptyEntriesAndPreservesExplicitOrder()
    {
        var project = BasicProject(version: "Z", changes: Array.Empty<string>());
        project.ReleaseHistory.Add(new ReleaseRecord { Version = "1.0.4", Changes = new List<string> { "" } });
        project.ReleaseHistory.Add(new ReleaseRecord { Version = "1.0.3", Changes = new List<string> { "ADADAD" } });
        project.ReleaseHistory.Add(new ReleaseRecord { Version = "1.0.0", Changes = new List<string> { "   " } });
        project.ReleaseHistory.Add(new ReleaseRecord { Version = "1.10", Changes = new List<string> { "Still before 2." } });
        project.ReleaseHistory.Add(new ReleaseRecord { Version = "2", Changes = new List<string> { "Explicitly second." } });
        project.ReleaseHistory.Add(new ReleaseRecord { Version = "alpha", Changes = new List<string> { "Arbitrary text." } });

        var text = Generate(project, ModelPath("models\\chair.mdl"));

        StringAssert.Contains(text, "CHANGELOG\r\n---------\r\n1.0.3\r\nADADAD\r\n\r\n1.10\r\nStill before 2.\r\n\r\n2\r\nExplicitly second.\r\n\r\nalpha\r\nArbitrary text.");
        Assert.IsFalse(text.Contains("\r\nZ\r\n", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("\r\n1.0.4\r\n", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("\r\n1.0.0\r\n", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ChangelogEntryWithChangesAndBlankVersionRendersChangesOnly()
    {
        var project = BasicProject(version: string.Empty, changes: new[] { "Nameless release notes." });

        var text = Generate(project, ModelPath("models\\chair.mdl"));

        StringAssert.Contains(text, "CHANGELOG\r\n---------\r\nNameless release notes.");
    }

    [TestMethod]
    public void ControlGroupsDetectionDoesNotEmitLegacyReadmeSection()
    {
        var project = BasicProject();

        var text = Generate(project, new ReadmeContext
        {
            ModelPaths = new[] { "models\\chair.mdl" },
            ControlGroupsFileDetected = true
        });

        Assert.IsFalse(text.Contains("SFM CONTROL GROUPS", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("sfm_defaultanimationgroups.txt", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void GeneratedPreviewIsExactResolvedContent()
    {
        var project = BasicProject();
        var context = ModelPath("models\\chair.mdl");
        var fileSystem = new FakeFileSystem();
        var resolver = new ReadmeResolver(fileSystem);
        var preview = new ReadmePreviewService(resolver).ResolvePreview(project, context);
        var resolved = resolver.Resolve(project, context);

        Assert.AreEqual(resolved.Text, preview.Text);
        CollectionAssert.AreEqual(resolved.GetGeneratedOrCustomBytes(), preview.GetGeneratedOrCustomBytes());
    }

    [TestMethod]
    public void RepeatedGenerationIsDeterministic()
    {
        var project = BasicProject();
        var context = ModelPath("models\\chair.mdl");

        var first = Generate(project, context);
        var second = Generate(project, context);

        Assert.AreEqual(first, second);
    }

    [TestMethod]
    public void GeneratedToCustomConversionStoresExactTextAndSwitchesMode()
    {
        var project = BasicProject();
        var generated = Generate(project, ModelPath("models\\chair.mdl"));

        var converted = new ReadmeConversionService()
            .UseGeneratedTextAsCustomReadme(project.Readme, generated);

        Assert.AreEqual(ReadmeMode.Custom, converted.Mode);
        Assert.AreEqual(ReadmeCustomSource.ProjectText, converted.CustomSource);
        Assert.AreEqual(generated, converted.CustomReadmeText);
        Assert.IsNull(converted.ImportedReadmePath);
        Assert.IsFalse(string.IsNullOrWhiteSpace(converted.CustomReadmeGeneratedFromFingerprint));
    }

    [TestMethod]
    public void CustomTextRemainsUnchangedAfterGeneratedFieldsChange()
    {
        var project = BasicProject();
        var generated = Generate(project, ModelPath("models\\chair.mdl"));
        project.Readme = new ReadmeConversionService().UseGeneratedTextAsCustomReadme(project.Readme, generated);
        project.AssetName = "Changed Asset";
        project.CurrentVersion = "9.9";
        project.ChangesThisVersion.Add("Changed generated input.");

        var resolved = Resolve(project, new ReadmeContext
        {
            ModelPaths = new[] { "models\\other.mdl" },
            InstallRoots = new[] { "models", "materials", "scripts" },
            OtherSupportingFilesIncluded = true
        });

        Assert.AreEqual(ResolvedReadmeKind.CustomText, resolved.Kind);
        Assert.AreEqual(generated, resolved.Text);
    }

    [TestMethod]
    public void ImportedReadmeResolutionReturnsCopyIntent()
    {
        var fileSystem = new FakeFileSystem();
        fileSystem.AddFile(@"E:\ReleaseDocs\README.txt", Encoding.UTF8.GetBytes("Imported bytes"));
        var project = BasicProject();
        project.Readme.Mode = ReadmeMode.Custom;
        project.Readme.CustomSource = ReadmeCustomSource.ImportedFile;
        project.Readme.ImportedReadmePath = @"E:\ReleaseDocs\README.txt";

        var resolved = new ReadmeResolver(fileSystem).Resolve(project, new ReadmeContext());

        Assert.AreEqual(ResolvedReadmeKind.ImportedFile, resolved.Kind);
        Assert.AreEqual(ReadmeResolutionStatus.Resolved, resolved.Status);
        Assert.AreEqual(@"E:\ReleaseDocs\README.txt", resolved.ImportedSourcePath);
        Assert.IsNull(resolved.Text);
        Assert.AreEqual("Imported bytes", resolved.PreviewText);
    }

    [TestMethod]
    public void ImportedReadmePreviewDisplaysActualSelectedFileContents()
    {
        const string importedText = "CUSTOM IMPORTED README\r\n\r\nDo not rewrite me.";
        var fileSystem = new FakeFileSystem();
        fileSystem.AddFile(@"E:\ReleaseDocs\README.txt", Encoding.UTF8.GetBytes(importedText));
        var project = BasicProject();
        project.Readme.Mode = ReadmeMode.Custom;
        project.Readme.CustomSource = ReadmeCustomSource.ImportedFile;
        project.Readme.ImportedReadmePath = @"E:\ReleaseDocs\README.txt";

        var preview = new ReadmePreviewService(new ReadmeResolver(fileSystem))
            .ResolvePreview(project, new ReadmeContext());

        Assert.AreEqual(ResolvedReadmeKind.ImportedFile, preview.Kind);
        Assert.AreEqual(importedText, preview.PreviewText);
        Assert.IsNull(preview.Text);
        Assert.AreEqual(@"E:\ReleaseDocs\README.txt", preview.ImportedSourcePath);
        Assert.AreEqual(0, fileSystem.CopyFileCallCount);
    }

    [TestMethod]
    public void ImportedReadmeMissingProducesStructuredFailure()
    {
        var project = BasicProject();
        project.Readme.Mode = ReadmeMode.Custom;
        project.Readme.CustomSource = ReadmeCustomSource.ImportedFile;
        project.Readme.ImportedReadmePath = @"E:\ReleaseDocs\README.txt";

        var resolved = new ReadmeResolver(new FakeFileSystem()).Resolve(project, new ReadmeContext());

        Assert.AreEqual(ResolvedReadmeKind.Failure, resolved.Kind);
        Assert.AreEqual(ReadmeResolutionStatus.MissingImportedReadme, resolved.Status);
    }

    [TestMethod]
    public void ImportedReadmeUnreadableProducesStructuredFailure()
    {
        var fileSystem = new FakeFileSystem();
        fileSystem.AddFile(@"E:\ReleaseDocs\README.txt", Encoding.UTF8.GetBytes("Imported bytes"));
        fileSystem.FailRead(@"E:\ReleaseDocs\README.txt");
        var project = BasicProject();
        project.Readme.Mode = ReadmeMode.Custom;
        project.Readme.CustomSource = ReadmeCustomSource.ImportedFile;
        project.Readme.ImportedReadmePath = @"E:\ReleaseDocs\README.txt";

        var resolved = new ReadmeResolver(fileSystem).Resolve(project, new ReadmeContext());

        Assert.AreEqual(ResolvedReadmeKind.Failure, resolved.Kind);
        Assert.AreEqual(ReadmeResolutionStatus.ImportedReadmeUnreadable, resolved.Status);
    }

    [TestMethod]
    public void ImportedReadmeIsNotRewritten()
    {
        var fileSystem = new FakeFileSystem();
        fileSystem.AddFile(@"E:\ReleaseDocs\README.txt", Encoding.UTF8.GetBytes("Imported bytes"));
        var project = BasicProject();
        project.Readme.Mode = ReadmeMode.Custom;
        project.Readme.CustomSource = ReadmeCustomSource.ImportedFile;
        project.Readme.ImportedReadmePath = @"E:\ReleaseDocs\README.txt";

        _ = new ReadmeResolver(fileSystem).Resolve(project, new ReadmeContext());

        Assert.AreEqual(0, fileSystem.CopyFileCallCount);
    }

    [TestMethod]
    public void NoReadmeModeResolvesCleanly()
    {
        var project = BasicProject();
        project.Readme.Mode = ReadmeMode.None;

        var resolved = Resolve(project, ModelPath("models\\chair.mdl"));

        Assert.AreEqual(ResolvedReadmeKind.None, resolved.Kind);
        Assert.AreEqual(ReadmeResolutionStatus.Resolved, resolved.Status);
        Assert.IsNull(resolved.Text);
        Assert.IsNull(resolved.ImportedSourcePath);
    }

    [TestMethod]
    public void UnicodeMetadataAndContentArePreserved()
    {
        var project = BasicProject(assetName: "Crème 椅子");
        project.Readme.Description = "Descripción with 椅子.";
        project.Readme.Author = "Zoë";

        var text = Generate(project, ModelPath("models\\Créator\\椅子.mdl"));

        StringAssert.Contains(text, "CRÈME 椅子");
        StringAssert.Contains(text, "Descripción with 椅子.");
        StringAssert.Contains(text, "Zoë");
        StringAssert.Contains(text, "models\\Créator\\椅子.mdl");
    }

    [TestMethod]
    public void GeneratedReadmeUsesCrlfAndUtf8WithoutBom()
    {
        var resolved = Resolve(BasicProject(), ModelPath("models\\chair.mdl"));
        var bytes = resolved.GetGeneratedOrCustomBytes();

        StringAssert.Contains(resolved.Text!, "\r\n");
        Assert.IsFalse(resolved.Text!.Contains('\n') && resolved.Text.Contains("\n", StringComparison.Ordinal) && resolved.Text.Replace("\r\n", string.Empty).Contains('\n'));
        CollectionAssert.AreNotEqual(new byte[] { 0xEF, 0xBB, 0xBF }, bytes.Take(3).ToArray());
    }

    [TestMethod]
    public void ProjectAndSourceFilesRemainUntouchedDuringReadmeResolution()
    {
        var fileSystem = new FakeFileSystem();
        fileSystem.AddFile(@"E:\ReleaseDocs\README.txt", Encoding.UTF8.GetBytes("Imported bytes"));
        var project = BasicProject();
        project.Readme.Mode = ReadmeMode.Custom;
        project.Readme.CustomSource = ReadmeCustomSource.ImportedFile;
        project.Readme.ImportedReadmePath = @"E:\ReleaseDocs\README.txt";
        var originalAssetName = project.AssetName;

        _ = new ReadmeResolver(fileSystem).Resolve(project, new ReadmeContext());

        Assert.AreEqual(originalAssetName, project.AssetName);
        Assert.AreEqual(0, fileSystem.CopyFileCallCount);
    }

    private static string Generate(PackageProject project, ReadmeContext context)
    {
        return new ReadmeGenerator().Generate(project, context);
    }

    private static ResolvedReadme Resolve(PackageProject project, ReadmeContext context)
    {
        return new ReadmeResolver(new FakeFileSystem()).Resolve(project, context);
    }

    private static ReadmeContext ModelPath(params string[] modelPaths)
    {
        return new ReadmeContext
        {
            ModelPaths = modelPaths,
            InstallRoots = new[] { "models" },
            PackageReadmeIncluded = true
        };
    }

    private static PackageProject BasicProject(string assetName = "Chair Prop", string version = "1.0", IReadOnlyList<string>? changes = null)
    {
        return new PackageProject
        {
            AssetName = assetName,
            CurrentVersion = version,
            ChangesThisVersion = (changes ?? new[] { "Initial release." }).ToList(),
            Readme = new ReadmeConfig
            {
                Mode = ReadmeMode.Generated
            }
        };
    }

    private static void AssertSnapshot(string snapshotName, string actual)
    {
        var expected = snapshotName switch
        {
            "all_optional_fields" =>
                "CHAIR PROP\r\n" +
                "==========\r\n" +
                "Version 1.1\r\n" +
                "\r\n" +
                "A useful prop.\r\n" +
                "\r\n" +
                "INSTALLATION\r\n" +
                "------------\r\n" +
                "Copy these folders into your Source Filmmaker usermod folder:\r\n" +
                "- models\r\n" +
                "\r\n" +
                "Default location:\r\n" +
                "...\\SourceFilmmaker\\game\\usermod\r\n" +
                "\r\n" +
                "If Windows asks to merge folders or replace files when updating, allow it.\r\n" +
                "\r\n" +
                "MODEL INCLUDED\r\n" +
                "--------------\r\n" +
                "- models\\chair.mdl\r\n" +
                "\r\n" +
                "AUTHOR\r\n" +
                "------\r\n" +
                "Creator\r\n" +
                "\r\n" +
                "WEBSITE\r\n" +
                "-------\r\n" +
                "https://example.test\r\n" +
                "\r\n" +
                "CREDITS\r\n" +
                "-------\r\n" +
                "Original mesh by Example Artist.\r\n" +
                "\r\n" +
                "USAGE TERMS\r\n" +
                "-----------\r\n" +
                "Custom license.\r\n" +
                "\r\n" +
                "LINKS & RESOURCES\r\n" +
                "-----------------\r\n" +
                "Includes PSD files.\r\n" +
                "\r\n" +
                "CHANGELOG\r\n" +
                "---------\r\n" +
                "1.1\r\n" +
                "Improved posing.\r\n",
            _ => throw new ArgumentOutOfRangeException(nameof(snapshotName), snapshotName, null)
        };

        Assert.AreEqual(expected, actual);
    }
}
