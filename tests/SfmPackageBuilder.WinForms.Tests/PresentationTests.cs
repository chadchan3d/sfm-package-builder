using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Xml.Linq;
using SfmPackageBuilder.Core.Archive;
using SfmPackageBuilder.Core.Build;
using SfmPackageBuilder.Core.Domain;
using SfmPackageBuilder.Core.Persistence;
using SfmPackageBuilder.Core.Planning;
using SfmPackageBuilder.Core.SharedFiles;
using SfmPackageBuilder.Core.Validation;
using SfmPackageBuilder.WinForms.Presentation;

namespace SfmPackageBuilder.WinForms.Tests;

[TestClass]
public sealed class PresentationTests
{
    private static readonly BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    [TestMethod]
    public void PackagePreviewTreeUsesPackagePlanDestinations()
    {
        var plan = new PackagePlan(new[]
        {
            Entry("model", @"D:\dev\chair_dev.mdl", @"models\Creator\chair\chair_release.mdl"),
            Entry("material", @"D:\dev\body.vtf", @"materials\models\Creator\chair\body.vtf"),
            new PackagePlanEntry(
                "readme",
                PackagePlanEntryType.Readme,
                PackagePlanEntryStatus.Resolved,
                "README.txt",
                null,
                null,
                null,
                isGenerated: true,
                readmeKind: ReadmePlanEntryKind.GeneratedText,
                textContent: "README",
                contentBytes: new byte[] { 1 })
        }, Array.Empty<SharedFileNotice>());

        var roots = new PackagePlanTreeBuilder().Build(plan);

        CollectionAssert.AreEqual(new[] { "models", "materials", "README.txt" }, roots.Select(root => root.Name).ToArray());
        Assert.IsTrue(roots.Any(root => root.Name == "models"));
        Assert.IsTrue(roots.Any(root => root.Name == "materials"));
        Assert.IsTrue(roots.Any(root => root.Name == "README.txt" && root.Entry?.Id == "readme"));
        var modelLeaf = roots.Single(root => root.Name == "models")
            .Children.Single(node => node.Name == "Creator")
            .Children.Single(node => node.Name == "chair")
            .Children.Single(node => node.Name == "chair_release.mdl");
        Assert.AreEqual("model", modelLeaf.Entry!.Id);
    }

    [TestMethod]
    public void ValidationPresenterShowsMessagesAndRequiredDecisionsWithoutInternalDecisionLanguage()
    {
        var result = new ValidationResult(
            new[]
            {
                new ValidationMessage(ValidationSeverity.Error, "error", "Fix this.", sourcePaths: new[] { @"D:\missing.mdl" }, destinationRelativePath: @"models\a.mdl", technicalDetail: "detail")
            },
            new[]
            {
                new RequiredDecision(RequiredDecisionKind.ExistingOutputArchive, "existing-output", "Archive exists.", new[] { RequiredDecisionOption.Replace, RequiredDecisionOption.Cancel })
            });

        var items = new ValidationPresenter().Present(result);

        Assert.IsTrue(items.Any(item => item.Severity == ValidationSeverity.Error && item.Title == "Fix this."));
        var decision = items.Single(item => item.Title == "The output ZIP already exists.");
        Assert.AreEqual(ValidationSeverity.Warning, decision.Severity);
        Assert.AreEqual("Package Builder will ask whether to replace it, choose another name, or cancel.", decision.Details);
        Assert.IsFalse(decision.Details.Contains("You can still build", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(decision.Details.Contains("Decision required", StringComparison.Ordinal));
        Assert.IsFalse(decision.Details.Contains("Replace", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ValidationPresenterShowsVersionReuseAsUserFacingWarning()
    {
        var result = new ValidationResult(
            Array.Empty<ValidationMessage>(),
            new[]
            {
                new RequiredDecision(
                    RequiredDecisionKind.VersionReuse,
                    "version-reuse",
                    "Version 1.0.0 already exists in this project.",
                    new[]
                    {
                        RequiredDecisionOption.RebuildExistingVersion,
                        RequiredDecisionOption.ChangeVersion,
                        RequiredDecisionOption.Cancel
                    })
            });

        var item = new ValidationPresenter().Present(result).Single();

        Assert.AreEqual(ValidationSeverity.Warning, item.Severity);
        Assert.AreEqual("Version 1.0.0 was already used for this project.", item.Title);
        Assert.AreEqual("Change the version if this is a new release.", item.Details);
        Assert.IsFalse(item.Details.Contains("You can still build", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(item.Details.Contains("RebuildExistingVersion", StringComparison.Ordinal));
        Assert.IsFalse(item.Details.Contains("ChangeVersion", StringComparison.Ordinal));
        Assert.IsFalse(item.Details.Contains("Decision required", StringComparison.Ordinal));
    }

    [TestMethod]
    public void DecisionPresenterMapsDialogChoicesToBuildDecisions()
    {
        var presenter = new DecisionPresenter();

        Assert.AreEqual(VersionReuseDecision.RebuildExistingVersion, presenter.MapVersionChoice(VersionReuseChoice.Rebuild));
        Assert.AreEqual(VersionReuseDecision.ChangeVersion, presenter.MapVersionChoice(VersionReuseChoice.ChangeVersion));
        Assert.AreEqual(VersionReuseDecision.Cancel, presenter.MapVersionChoice(VersionReuseChoice.Cancel));
        Assert.AreEqual(ExistingOutputDecision.Replace, presenter.MapOutputChoice(ExistingOutputChoice.Replace));
        Assert.AreEqual(ExistingOutputDecision.ChooseAnotherName, presenter.MapOutputChoice(ExistingOutputChoice.ChooseAnotherName));
        Assert.AreEqual(ExistingOutputDecision.Cancel, presenter.MapOutputChoice(ExistingOutputChoice.Cancel));
    }

    [TestMethod]
    public void ArchiveNameSuggestionUsesSettingsPattern()
    {
        var settings = new AppSettings { DefaultArchivePattern = "{AssetName}-{Version}" };

        var suggested = new ArchiveNameSuggester().Suggest("Cereal Box", "1.1", settings);

        Assert.AreEqual("Cereal_Box-1.1.zip", suggested);
        Assert.AreEqual("Bad_Name-1_2.zip", new ArchiveNameSuggester().Suggest("Bad/Name", "1:2", settings));
        Assert.AreEqual("Package.zip", new ArchiveNameSuggester().Suggest(string.Empty, "1.1", settings));
        Assert.AreEqual("Package.zip", new ArchiveNameSuggester().Suggest("Cereal Box", string.Empty, settings));
        Assert.AreEqual("Package.zip", new ArchiveNameSuggester().Suggest("Cereal Box", "1.1", new AppSettings { DefaultArchivePattern = "{AssetName}_v{Unknown}.zip" }));
    }

    [TestMethod]
    public void ArchiveNameStateTracksSuggestedNameUntilManualEditThenCanReset()
    {
        var settings = new AppSettings { DefaultArchivePattern = "{AssetName}_v{Version}.zip" };
        var state = new ArchiveNameSuggestionState(new ArchiveNameSuggester());

        Assert.AreEqual("Android_ChadChan3D_v1.zip", state.RefreshSuggested("Android ChadChan3D", "1", settings));
        Assert.AreEqual("Android_ChadChan3D_v1.2.zip", state.RefreshSuggested("Android ChadChan3D", "1.2", settings));

        state.MarkManualEdit("custom.zip");

        Assert.AreEqual("custom.zip", state.RefreshSuggested("Android ChadChan3D", "1.2.3", settings));
        Assert.AreEqual("Android_ChadChan3D_v1.2.3.zip", state.ResetToSuggested("Android ChadChan3D", "1.2.3", settings));
    }

    [STATestMethod]
    public void MainFormManualZipFilenameSurvivesReleaseMetadataEditsUntilReset()
    {
        using var form = new MainForm();
        InvokePrivate(form, "ShowEditor");
        InvokePrivate(form, "LoadProjectIntoControls");
        form.Show();
        Application.DoEvents();
        var assetName = GetField<TextBox>(form, "assetNameText");
        var version = GetField<TextBox>(form, "versionText");
        var archive = GetField<TextBox>(form, "archiveNameText");
        var reset = GetField<Button>(form, "resetArchiveNameButton");

        assetName.Text = "Cereal Box";
        version.Text = "1.0";
        Assert.AreEqual("Cereal_Box_v1.0.zip", archive.Text);

        archive.Text = "hand-authored.zip";
        assetName.Text = "Different Name";
        version.Text = "2.0";

        Assert.AreEqual("hand-authored.zip", archive.Text);

        GetField<TabControl>(form, "workflowTabs").SelectedIndex = 4;
        Application.DoEvents();
        reset.PerformClick();

        Assert.AreEqual("Different_Name_v2.0.zip", archive.Text);
    }

    [TestMethod]
    public void BrowseStartPrefersConfiguredModelAndMaterialRootsWithFallbacks()
    {
        var resolver = new BrowseStartResolver();
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            @"C:\SFM\game\usermod",
            @"C:\SFM\game\usermod\models",
            @"C:\SFM\game\usermod\materials",
            @"C:\SFM\game\usermod\materials\models",
            @"D:\SFM\game\usermod\materials",
            @"E:\Last",
            @"F:\Default"
        };

        Assert.AreEqual(
            @"C:\SFM\game\usermod\models",
            resolver.ResolveSourceStart(
                string.Empty,
                string.Empty,
                @"C:\SFM\game\usermod",
                existing.Contains));

        Assert.AreEqual(
            @"D:\SFM\game\usermod\models\creator",
            resolver.ResolveSourceStart(
                @"D:\SFM\game\usermod\models\creator\prop.mdl",
                string.Empty,
                @"C:\SFM\game\usermod",
                path => existing.Contains(path) || string.Equals(path, @"D:\SFM\game\usermod\models\creator", StringComparison.OrdinalIgnoreCase)));

        Assert.AreEqual(
            @"C:\SFM\game\usermod\materials\models",
            resolver.ResolveMaterialsStart(
                string.Empty,
                @"C:\SFM\game\usermod",
                existing.Contains));

        Assert.AreEqual(
            @"C:\SFM\game\usermod\materials\models",
            resolver.ResolveMaterialsStart(
                @"C:\SFM\game\usermod\materials",
                @"C:\SFM\game\usermod",
                existing.Contains));

        existing.Remove(@"C:\SFM\game\usermod\materials\models");
        Assert.AreEqual(
            @"C:\SFM\game\usermod\materials",
            resolver.ResolveMaterialsStart(
                string.Empty,
                @"C:\SFM\game\usermod",
                existing.Contains));

        Assert.AreEqual(
            @"C:\SFM\game\usermod\materials",
            resolver.ResolveMaterialsStart(
                @"C:\SFM\game\usermod\materials",
                @"C:\SFM\game\usermod",
                existing.Contains));

        existing.Remove(@"C:\SFM\game\usermod\materials");
        Assert.AreEqual(
            @"C:\SFM\game\usermod",
            resolver.ResolveMaterialsStart(
                string.Empty,
                @"C:\SFM\game\usermod",
                existing.Contains));

        Assert.AreEqual(
            @"E:\Last",
            resolver.ResolveMaterialsStart(
            @"E:\Last",
            @"C:\SFM\game\usermod",
            existing.Contains));
    }

    [TestMethod]
    public void BrowseStartUsesNormalizedSettingsWhenStoredValueIsSourceFilmmakerRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "SfmPackageBuilder.WinForms.Tests", Guid.NewGuid().ToString("N"));
        try
        {
            var sourceFilmmakerRoot = Path.Combine(root, "SteamLibrary", "steamapps", "common", "SourceFilmmaker");
            var usermodRoot = Path.Combine(sourceFilmmakerRoot, "game", "usermod");
            var modelBrowseRoot = Path.Combine(usermodRoot, "models");
            var materialBrowseRoot = Path.Combine(usermodRoot, "materials", "models");
            Directory.CreateDirectory(modelBrowseRoot);
            Directory.CreateDirectory(materialBrowseRoot);
            var service = new SettingsService(Path.Combine(root, "settings.json"));
            service.Save(new AppSettings { DefaultSfmContentFolder = sourceFilmmakerRoot });
            var settings = service.Load().Settings;
            var resolver = new BrowseStartResolver();

            Assert.AreEqual(usermodRoot, settings.DefaultSfmContentFolder);
            Assert.AreEqual(modelBrowseRoot, resolver.ResolveSourceStart(string.Empty, string.Empty, settings.DefaultSfmContentFolder, Directory.Exists));
            Assert.AreEqual(materialBrowseRoot, resolver.ResolveMaterialsStart(string.Empty, settings.DefaultSfmContentFolder, Directory.Exists));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [TestMethod]
    public void ExtraDestinationPresenterSupportsRootDocsCustomAndLegacyMisc()
    {
        var presenter = new ExtraDestinationPresenter();

        Assert.AreEqual(DestinationOverrideKind.Root, presenter.ToOverride(ExtraDestinationPresenter.PackageRootChoice, string.Empty).Kind);
        Assert.AreEqual("Docs", presenter.ToOverride(ExtraDestinationPresenter.DocumentationChoice, string.Empty).RelativePath);
        Assert.AreEqual(@"cfg", presenter.ToOverride(ExtraDestinationPresenter.CustomChoice, @"cfg").RelativePath);
        Assert.AreEqual(
            ExtraDestinationPresenter.DocumentationChoice,
            presenter.ChoiceFor(new DestinationOverride { Kind = DestinationOverrideKind.Custom, RelativePath = "Docs" }));
        Assert.AreEqual(
            string.Empty,
            presenter.CustomFolderFor(new DestinationOverride { Kind = DestinationOverrideKind.Custom, RelativePath = "Docs" }));

        var legacy = new SourceEntry
        {
            SourcePath = @"E:\extras\notes.txt",
            DestinationOverride = new DestinationOverride { Kind = DestinationOverrideKind.Misc, RelativePath = string.Empty }
        };

        Assert.AreEqual(ExtraDestinationPresenter.CustomChoice, presenter.ChoiceFor(legacy.DestinationOverride));
        Assert.AreEqual("Misc", presenter.CustomFolderFor(legacy.DestinationOverride));
        Assert.AreEqual(@"Misc\", presenter.PackageLocationFor(legacy));
    }

    [STATestMethod]
    public void LaunchScreenUsesComposedBrandingAndSecondaryDefaultsCopy()
    {
        using var form = new MainForm();
        form.Size = new Size(1160, 760);
        form.Show();
        Application.DoEvents();
        InvokePrivate(form, "UpdateLaunchLayout");
        var text = string.Join("|", AllControls(form).Select(control => control.Text.Replace("&&", "&", StringComparison.Ordinal)));

        StringAssert.Contains(text, "Build ZIP packages for Source Filmmaker models.");
        StringAssert.Contains(text, "New Package");
        StringAssert.Contains(text, "Open Package");
        StringAssert.Contains(text, "Defaults & Preferences...");
        StringAssert.Contains(text, "About");
        StringAssert.Contains(text, "Recent Packages");
        StringAssert.Contains(text, "No recent packages yet.");
        StringAssert.Contains(text, "Set folders and creator defaults.");
        Assert.IsFalse(text.Contains("Create ZIP packages for compiled Source Filmmaker models.", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("Save your author, website, usage terms, and SFM folder for new packages.", StringComparison.Ordinal));
        var layout = GetField<TableLayoutPanel>(form, "launchContentLayout");
        Assert.AreEqual(2, layout.ColumnCount);
        Assert.IsTrue(GetField<Control>(form, "launchRecentPanel").Visible);
        Assert.IsFalse(text.Contains("Start by choosing the model you want to package.", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("Save time on future packages", StringComparison.Ordinal));
        var logo = AllControls(form).OfType<PictureBox>().Single(box => box.Image is not null);
        Assert.AreEqual(400, logo.Width);
        Assert.AreEqual(410, logo.Height);
        Assert.IsTrue(logo.Parent!.Width <= GetField<Control>(form, "launchPrimaryPanel").Width);
    }

    [TestMethod]
    public void BrandingResourcesAndDerivedIconArePresent()
    {
        var resources = typeof(MainForm).Assembly.GetManifestResourceNames();

        CollectionAssert.Contains(resources, "SfmPackageBuilder.WinForms.Assets.packagebuilderlogo_cropped.png");
        CollectionAssert.Contains(resources, "SfmPackageBuilder.WinForms.Assets.packagebuilder_justlogo.png");
        CollectionAssert.Contains(resources, "SfmPackageBuilder.WinForms.Assets.packagebuilderjusttitle.png");
        CollectionAssert.Contains(resources, "SfmPackageBuilder.WinForms.Assets.successbox.jpg");
        CollectionAssert.Contains(resources, "SfmPackageBuilder.WinForms.Assets.app-icon.ico");
        Assert.IsFalse(resources.Contains("SfmPackageBuilder.WinForms.Assets.packagebuilderlogo.png", StringComparer.Ordinal));

        var root = FindWorkspaceRoot();
        var iconPath = Path.Combine(root, "src", "SfmPackageBuilder.WinForms", "Assets", "app-icon.ico");
        using var stream = File.OpenRead(iconPath);
        using var reader = new BinaryReader(stream);
        Assert.AreEqual(0, reader.ReadUInt16());
        Assert.AreEqual(1, reader.ReadUInt16());
        Assert.IsTrue(reader.ReadUInt16() >= 4);
    }

    [TestMethod]
    public void ReleaseModelNamePresenterShowsUnchangedOrOriginalToReleaseMapping()
    {
        var presenter = new ReleaseModelNamePresenter();

        Assert.AreEqual(
            "Release filename: unchanged",
            presenter.Present(@"D:\SFM\game\usermod\models\creator\house_v13.mdl", "house_v13", Array.Empty<PackagePlanEntry>()));

        var entries = new[]
        {
            Entry("model", @"D:\dev\house_v13.mdl", @"models\creator\house_chadchan3d.mdl"),
            new PackagePlanEntry("phy", PackagePlanEntryType.ModelCompanion, PackagePlanEntryStatus.Resolved, @"models\creator\house_chadchan3d.phy", @"D:\dev\house_v13.phy", Guid.NewGuid(), Guid.NewGuid(), false)
        };

        var text = presenter.Present(@"D:\dev\house_v13.mdl", "house_chadchan3d", entries);

        StringAssert.Contains(text, "Files renamed in ZIP");
        StringAssert.Contains(text, "Original");
        StringAssert.Contains(text, "house_v13.mdl");
        StringAssert.Contains(text, "house_v13.phy");
        StringAssert.Contains(text, "ZIP");
        StringAssert.Contains(text, "→");
        StringAssert.Contains(text, "house_chadchan3d.mdl");
        StringAssert.Contains(text, "house_chadchan3d.phy");
    }

    [TestMethod]
    public void ReleaseModelNamePresenterExposesPrimaryFamilyMappingsFromPlanEntries()
    {
        var presenter = new ReleaseModelNamePresenter();
        var primaryId = Guid.NewGuid();
        var additionalId = Guid.NewGuid();
        var entries = new[]
        {
            new PackagePlanEntry("primary-mdl", PackagePlanEntryType.Model, PackagePlanEntryStatus.Resolved, @"models\creator\flashlight.mdl", @"D:\dev\mia.mdl", primaryId, primaryId, false),
            new PackagePlanEntry("primary-vvd", PackagePlanEntryType.ModelCompanion, PackagePlanEntryStatus.Resolved, @"models\creator\flashlight.vvd", @"D:\dev\mia.vvd", primaryId, primaryId, false),
            new PackagePlanEntry("primary-dx90", PackagePlanEntryType.ModelCompanion, PackagePlanEntryStatus.Resolved, @"models\creator\flashlight.dx90.vtx", @"D:\dev\mia.dx90.vtx", primaryId, primaryId, false),
            new PackagePlanEntry("additional", PackagePlanEntryType.Model, PackagePlanEntryStatus.Resolved, @"models\creator\lamp.mdl", @"D:\dev\lamp.mdl", additionalId, additionalId, false)
        };

        var mappings = presenter.PresentMappings(@"D:\dev\mia.mdl", "flashlight", entries, primaryId);

        CollectionAssert.AreEqual(
            new[] { "mia.mdl", "mia.vvd", "mia.dx90.vtx" },
            mappings.Select(mapping => mapping.Original).ToArray());
        CollectionAssert.AreEqual(
            new[] { "flashlight.mdl", "flashlight.vvd", "flashlight.dx90.vtx" },
            mappings.Select(mapping => mapping.Zip).ToArray());
        Assert.IsFalse(mappings.Any(mapping => mapping.Original == "lamp.mdl"));
        Assert.AreEqual(0, presenter.PresentMappings(@"D:\dev\mia.mdl", "mia", entries, primaryId).Count);
    }

    [STATestMethod]
    public void LoadingProjectIntoMainFormDoesNotClearReleaseModelName()
    {
        using var form = new MainForm();
        var project = new PackageProject();
        project.Models.Add(new ModelEntry
        {
            Role = ModelRole.Primary,
            SourceMdlPath = @"D:\SFM\game\usermod\models\creator\house_v13.mdl",
            SourceStem = "house_v13",
            ReleaseStem = "house_chadchan3d"
        });
        SetField(form, "project", project);

        InvokePrivate(form, "LoadProjectIntoControls");

        Assert.AreEqual("house_chadchan3d", project.Models[0].ReleaseStem);
        Assert.AreEqual("house_chadchan3d", GetField<TextBox>(form, "releaseStemText").Text);
    }

    [STATestMethod]
    public void NewPackageCopiesCreatorDefaultsButExistingProjectValuesRemainOwnedByProject()
    {
        using var form = new MainForm();
        SetField(form, "settings", new AppSettings
        {
            CreatorDefaults = new CreatorDefaults
            {
                Author = "ChadChan3D",
                Website = "https://example.test",
                License = "CC0 1.0"
            }
        });

        InvokePrivate(form, "NewPackage");
        var newProject = GetField<PackageProject>(form, "project");

        Assert.AreEqual("ChadChan3D", newProject.Readme.Author);
        Assert.AreEqual("https://example.test", newProject.Readme.Website);
        Assert.AreEqual("CC0 1.0", newProject.Readme.License);

        newProject.Readme.Author = "Project Author";
        GetField<AppSettings>(form, "settings").CreatorDefaults.Author = "Changed Default";
        InvokePrivate(form, "LoadProjectIntoControls");

        Assert.AreEqual("Project Author", newProject.Readme.Author);
        Assert.AreEqual("Project Author", GetField<TextBox>(form, "readmeAuthorText").Text);
    }

    [STATestMethod]
    public void WorkflowHasFiveFreelySelectableStagesAndBackContinueNavigate()
    {
        using var form = new MainForm();
        InvokePrivate(form, "ShowEditor");
        form.Show();
        Application.DoEvents();
        var tabs = GetField<TabControl>(form, "workflowTabs");
        var previous = GetField<Button>(form, "previousStageButton");
        var next = GetField<Button>(form, "nextStageButton");

        Assert.AreEqual(5, tabs.TabPages.Count);
        CollectionAssert.AreEqual(
            new[] { "1  Model & Materials", "2  Release Info", "3  README", "4  Extras", "5  Review & Build" },
            tabs.TabPages.Cast<TabPage>().Select(page => page.Text.Replace("&&", "&", StringComparison.Ordinal)).ToArray());
        Assert.IsTrue(tabs.TabPages.Cast<TabPage>().All(page => page.Enabled));

        tabs.SelectedIndex = 0;
        InvokePrivate(form, "UpdateWorkflowNavigation");
        Assert.AreEqual("Continue to Release Info >", next.Text);
        Assert.IsFalse(previous.UseMnemonic);
        Assert.IsFalse(next.UseMnemonic);
        next.PerformClick();
        Assert.AreEqual(1, tabs.SelectedIndex);
        previous.PerformClick();
        Assert.AreEqual(0, tabs.SelectedIndex);

        tabs.SelectedIndex = 1;
        InvokePrivate(form, "UpdateWorkflowNavigation");
        Assert.AreEqual("< Back to Model & Materials", previous.Text);
        Assert.IsFalse(previous.UseMnemonic);
        tabs.SelectedIndex = 3;
        InvokePrivate(form, "UpdateWorkflowNavigation");
        Assert.AreEqual("Continue to Review & Build >", next.Text);
        Assert.IsFalse(next.UseMnemonic);
    }

    [STATestMethod]
    public void ReleaseInfoIsSeparateAndReleaseCopyIsNotDuplicatedOnModelMaterials()
    {
        using var form = new MainForm();
        InvokePrivate(form, "ShowEditor");
        InvokePrivate(form, "RefreshModelFamilyInfo");
        var tabs = GetField<TabControl>(form, "workflowTabs");

        var modelText = string.Join("|", AllControls(tabs.TabPages[0]).Select(control => control.Text.Replace("&&", "&", StringComparison.Ordinal)));
        var releaseText = string.Join("|", AllControls(tabs.TabPages[1]).Select(control => control.Text.Replace("&&", "&", StringComparison.Ordinal)));

        StringAssert.Contains(modelText, "Source");
        StringAssert.Contains(modelText, "Choose the .mdl file you want to package.");
        Assert.IsFalse(modelText.Contains("Related model files (0)", StringComparison.Ordinal));
        Assert.IsFalse(modelText.Contains("Included automatically and renamed to match the model in the ZIP.", StringComparison.Ordinal));
        StringAssert.Contains(modelText, "Model name in ZIP");
        Assert.IsFalse(GetField<TextBox>(form, "releaseStemText").Enabled);
        Assert.AreEqual(string.Empty, GetField<TextBox>(form, "releaseStemText").Text);
        Assert.IsFalse(GetField<Label>(form, "renamePreviewLabel").Visible);
        Assert.IsFalse(modelText.Contains("Change the model's filename in the ZIP.", StringComparison.Ordinal));
        Assert.IsFalse(modelText.Contains("Name in release ZIP", StringComparison.Ordinal));
        Assert.IsFalse(modelText.Contains("Example:", StringComparison.Ordinal));
        StringAssert.Contains(modelText, "Additional models");
        Assert.IsFalse(modelText.Contains("Add another .mdl file if this ZIP should contain more than one model.", StringComparison.Ordinal));
        StringAssert.Contains(modelText, "Materials & Textures");
        StringAssert.Contains(modelText, "Materials found automatically appear here.");
        Assert.IsFalse(modelText.Contains("You can also add materials manually.", StringComparison.Ordinal));
        Assert.IsFalse(modelText.Contains("Add the materials folders for the models in this package, or select individual material files.", StringComparison.Ordinal));
        Assert.IsFalse(modelText.Contains("compiled .mdl", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual(string.Empty, GetField<Label>(form, "modelFamilyNamesLabel").Text);
        Assert.IsFalse(GetField<Label>(form, "modelFamilySummaryLabel").Visible);
        Assert.IsFalse(GetField<Label>(form, "modelFamilyNamesLabel").Visible);
        Assert.AreEqual("Add Model...", GetField<Button>(form, "addAdditionalModelButton").Text);
        Assert.AreEqual("Remove", GetField<Button>(form, "removeAdditionalModelButton").Text);
        Assert.IsFalse(GetField<Button>(form, "removeAdditionalModelButton").Enabled);
        Assert.IsFalse(modelText.Contains("Related model files are included automatically.", StringComparison.Ordinal));
        Assert.IsFalse(modelText.Contains("Release name", StringComparison.Ordinal));
        Assert.IsFalse(modelText.Contains("What's changed in this version?", StringComparison.Ordinal));

        StringAssert.Contains(releaseText, "Release Info");
        Assert.IsFalse(releaseText.Contains("RELEASE INFO", StringComparison.Ordinal));
        StringAssert.Contains(releaseText, "Title");
        Assert.IsFalse(releaseText.Contains("The name people will see for this package.", StringComparison.Ordinal));
        StringAssert.Contains(releaseText, "Version");
        StringAssert.Contains(releaseText, "Changelog");
        StringAssert.Contains(releaseText, "Describe what changed in this version.");
        StringAssert.Contains(releaseText, "Add to README Changelog");
        Assert.IsFalse(releaseText.Contains("Add This Version to Changelog", StringComparison.Ordinal));
        Assert.IsFalse(releaseText.Contains("Adds this version and its notes to the README changelog.", StringComparison.Ordinal));
        StringAssert.Contains(releaseText, "README changelog");
        StringAssert.Contains(releaseText, "Edit Selected...");
        StringAssert.Contains(releaseText, "Remove Selected");
        StringAssert.Contains(releaseText, "Select a changelog entry to edit or remove it.");
        Assert.IsFalse(releaseText.Contains("Previous versions", StringComparison.Ordinal));
        Assert.IsFalse(releaseText.Contains("These notes will be included in the README.", StringComparison.Ordinal));
        Assert.IsFalse(releaseText.Contains("Used in the README and when Package Builder names the ZIP.", StringComparison.Ordinal));
        Assert.IsFalse(releaseText.Contains("Release Details", StringComparison.Ordinal));
        Assert.IsFalse(releaseText.Contains("Package Details", StringComparison.Ordinal));
        Assert.IsFalse(releaseText.Contains("Title & Version", StringComparison.Ordinal));
        Assert.IsFalse(releaseText.Contains("Release name", StringComparison.Ordinal));
        Assert.IsFalse(releaseText.Contains("Start New Version", StringComparison.Ordinal));
        Assert.IsFalse(releaseText.Contains("What's changed in this version?", StringComparison.Ordinal));

        CollectionAssert.IsSubsetOf(
            new[] { "Model", "Materials & Textures", "Release Info", "README", "README Details", "Extras", "Package Contents", "Build Check", "Output" },
            AllControls(tabs).OfType<GroupBox>().Select(group => group.Text.Replace("&&", "&", StringComparison.Ordinal)).ToArray());
        Assert.IsFalse(AllControls(tabs).OfType<GroupBox>().Any(group => group.Text is "MODEL" or "RELEASE INFO"));
    }

    [STATestMethod]
    public void ModelMaterialsStageUsesSingleColumnWideControlsAndCompactFooter()
    {
        using var form = new MainForm();
        form.Size = new Size(1160, 813);
        InvokePrivate(form, "ShowEditor");
        form.Show();
        Application.DoEvents();
        var tabs = GetField<TabControl>(form, "workflowTabs");
        tabs.SelectedIndex = 0;
        Application.DoEvents();

        var layout = GetField<TableLayoutPanel>(form, "modelMaterialsLayout");
        var navigation = GetField<Control>(form, "workflowNavigationPanel");
        var source = GetField<TextBox>(form, "primaryModelText");
        var modelName = GetField<TextBox>(form, "releaseStemText");
        var materials = GetField<ListBox>(form, "materialsList");
        var family = GetField<Label>(form, "modelFamilyNamesLabel");
        var additional = GetField<ListView>(form, "additionalModelsList");
        var addModel = GetField<Button>(form, "addAdditionalModelButton");
        var removeModel = GetField<Button>(form, "removeAdditionalModelButton");
        var next = GetField<Button>(form, "nextStageButton");

        Assert.AreEqual(1, layout.ColumnCount);
        Assert.AreEqual(2, layout.RowCount);
        Assert.IsTrue(source.Width > form.ClientSize.Width * 0.45);
        Assert.IsTrue(modelName.Width > form.ClientSize.Width * 0.45);
        Assert.IsTrue(materials.Width > form.ClientSize.Width * 0.65);
        Assert.IsFalse(family.Visible);
        Assert.IsTrue(additional.Width > form.ClientSize.Width * 0.65);
        Assert.AreSame(addModel.Parent, removeModel.Parent);
        Assert.AreSame(addModel.Parent?.Parent, GetField<Label>(form, "additionalModelsSummaryLabel").Parent);
        Assert.AreEqual(DockStyle.Top, navigation.Dock);
        Assert.IsTrue(navigation.Padding.Top >= 8);
        Assert.IsTrue(navigation.Padding.Bottom >= 8);
        Assert.IsTrue(
            navigation.Height <= next.Height + navigation.Padding.Vertical + 18,
            $"Navigation height {navigation.Height}, next height {next.Height}, padding {navigation.Padding.Vertical}.");
    }

    [STATestMethod]
    public void OnlyMajorSectionHeadingsUseBoldHierarchy()
    {
        using var form = new MainForm();
        InvokePrivate(form, "ShowEditor");
        InvokePrivate(form, "LoadProjectIntoControls");
        var tabs = GetField<TabControl>(form, "workflowTabs");

        var majorHeadings = new[]
        {
            "Model",
            "Materials & Textures",
            "Release Info",
            "README",
            "README Details",
            "Extras",
            "Package Contents",
            "Build Check",
            "Output"
        };

        foreach (var group in AllControls(tabs).OfType<GroupBox>().Where(group => !string.IsNullOrWhiteSpace(group.Text)))
        {
            CollectionAssert.Contains(majorHeadings, group.Text.Replace("&&", "&", StringComparison.Ordinal));
            Assert.IsTrue(group.Font.Bold, group.Text);
        }

        var related = GetField<Label>(form, "modelFamilySummaryLabel");
        var additional = GetField<Label>(form, "additionalModelsSummaryLabel");
        var changelog = AllControls(tabs.TabPages[1]).OfType<Label>().Single(label => label.Text == "README changelog");

        Assert.IsFalse(related.Font.Bold);
        Assert.IsFalse(additional.Font.Bold);
        Assert.IsFalse(changelog.Font.Bold);
    }

    [STATestMethod]
    public void StageOneListsUseAdaptiveInternalHeightsAndMaterialsFillRemainingSpace()
    {
        using var form = new MainForm();
        form.Size = new Size(1160, 813);
        InvokePrivate(form, "ShowEditor");
        form.Show();
        GetField<TabControl>(form, "workflowTabs").SelectedIndex = 0;
        Application.DoEvents();

        var family = GetField<Label>(form, "modelFamilyNamesLabel");
        InvokePrivate(form, "SetModelFamilyDisplayNames", (object)new[] { "mia.mdl", "mia.vvd", "mia.dx90.vtx" });
        InvokePrivate(form, "UpdateStageOneListHeights");
        var compactFamilyHeight = family.Height;
        Assert.AreEqual("mia.mdl, mia.vvd, mia.dx90.vtx", family.Text);

        InvokePrivate(
            form,
            "SetModelFamilyDisplayNames",
            (object)new[] { "mia.mdl", "mia.vvd", "mia.dx90.vtx" }.Concat(Enumerable.Range(0, 18).Select(index => $"companion{index}.vtx")).ToArray());
        InvokePrivate(form, "UpdateStageOneListHeights");
        var largeFamilyHeight = family.Height;

        var additional = GetField<ListView>(form, "additionalModelsList");
        var emptyAdditionalHeight = additional.Height;
        additional.Items.Clear();
        for (var i = 0; i < 12; i++)
        {
            var row = new ListViewItem($"model{i}.mdl");
            row.SubItems.Add("3");
            additional.Items.Add(row);
        }
        InvokePrivate(form, "UpdateStageOneListHeights");

        var materials = GetField<ListBox>(form, "materialsList");
        materials.Items.Clear();
        materials.Items.AddRange(Enumerable.Range(0, 13).Select(index => (object)$@"materials\models\mia\mat{index}.vmt").ToArray());
        InvokePrivate(form, "UpdateStageOneListHeights");

        StringAssert.Contains(family.Text, "companion17.vtx");
        Assert.AreEqual(compactFamilyHeight, largeFamilyHeight);
        Assert.IsTrue(compactFamilyHeight <= 2 * TextRenderer.MeasureText("Mg", family.Font).Height + 12, $"Primary related-file text height was {compactFamilyHeight}.");
        Assert.IsTrue(emptyAdditionalHeight < 100, $"Empty additional-model height was {emptyAdditionalHeight}.");
        Assert.IsTrue(additional.Height > emptyAdditionalHeight);
        Assert.IsTrue(additional.Height < additional.Items.Count * (additional.Font.Height + 12));
        Assert.AreEqual(DockStyle.Fill, materials.Dock);
        Assert.IsTrue(materials.Height >= 4 * materials.ItemHeight, $"Materials height {materials.Height}.");
    }

    [STATestMethod]
    public void PrimaryRelatedFilesUsePlainTextInsteadOfListControl()
    {
        using var form = new MainForm();
        form.Size = new Size(1160, 813);
        InvokePrivate(form, "ShowEditor");
        form.Show();
        var tabs = GetField<TabControl>(form, "workflowTabs");
        tabs.SelectedIndex = 0;
        var label = GetField<Label>(form, "modelFamilyNamesLabel");

        InvokePrivate(form, "SetModelFamilyDisplayNames", (object)new[] { "model.vvd", "model.dx90.vtx", "model.phy" });
        InvokePrivate(form, "UpdateStageOneListHeights");

        Assert.AreEqual("model.vvd, model.dx90.vtx, model.phy", label.Text);
        Assert.AreEqual(BorderStyle.None, label.BorderStyle);
        Assert.IsTrue(label.AutoEllipsis);
        Assert.IsFalse(label.AutoSize);
        Assert.IsTrue(label.Height <= 2 * TextRenderer.MeasureText("Mg", label.Font).Height + 12);
        var heading = GetField<Label>(form, "modelFamilySummaryLabel");
        Assert.IsTrue(heading.Visible);
        Assert.AreEqual(heading.Margin.Left, label.Margin.Left);
        Assert.AreSame(heading.Parent, label.Parent);
        var layout = (TableLayoutPanel)heading.Parent!;
        Assert.AreEqual(layout.GetColumn(heading), layout.GetColumn(label));
        Assert.AreEqual(layout.GetColumnSpan(heading), layout.GetColumnSpan(label));
        Assert.AreEqual(1, AllControls(tabs.TabPages[0]).OfType<ListView>().Count());
        Assert.AreSame(GetField<ListView>(form, "additionalModelsList"), AllControls(tabs.TabPages[0]).OfType<ListView>().Single());
    }

    [STATestMethod]
    public void PrimarySourceInstructionHidesAfterSourceSelectionReplacementAndReturnsWhenCleared()
    {
        using var form = new MainForm();
        InvokePrivate(form, "ShowEditor");
        form.Show();
        var source = GetField<TextBox>(form, "primaryModelText");
        var instruction = GetField<Label>(form, "sourceInstructionLabel");

        Assert.AreEqual("Choose the .mdl file you want to package.", instruction.Text);
        Assert.IsTrue(instruction.Visible);

        source.Text = @"D:\SFM\game\usermod\models\creator\a\a.mdl";
        Application.DoEvents();
        Assert.IsFalse(instruction.Visible);

        source.Text = @"D:\SFM\game\usermod\models\creator\b\b.mdl";
        Application.DoEvents();
        Assert.IsFalse(instruction.Visible);

        source.Text = string.Empty;
        Application.DoEvents();
        Assert.IsTrue(instruction.Visible);
    }

    [STATestMethod]
    public void PrimaryRelatedFilesUsePackagePlanDestinationNamesThroughRenameRevertAndReplacement()
    {
        var root = Path.Combine(Path.GetTempPath(), "SfmPackageBuilder.WinForms.Tests", Guid.NewGuid().ToString("N"));
        try
        {
            var firstModel = CreateModelFamily(root, "old_source");
            var secondModel = CreateModelFamily(root, "fresh_source");
            using var form = new MainForm();
            InvokePrivate(form, "ShowEditor");
            SetField(form, "project", CreatePrimaryProject(firstModel, "release_model"));
            InvokePrivate(form, "LoadProjectIntoControls");
            form.Show();
            Application.DoEvents();
            var source = GetField<TextBox>(form, "primaryModelText");
            var releaseStem = GetField<TextBox>(form, "releaseStemText");
            var family = GetField<Label>(form, "modelFamilyNamesLabel");

            AssertRelatedNamesMatchPlannedDestinations(form, family);
            StringAssert.Contains(family.Text, "release_model.vvd");
            Assert.IsFalse(family.Text.Contains("old_source.vvd", StringComparison.Ordinal));
            Assert.IsFalse(family.Text.Contains("→", StringComparison.Ordinal));

            releaseStem.Text = "renamed_model";
            Application.DoEvents();
            InvokePrivate(form, "FlushPendingModelNamePresentationForTests");
            AssertRelatedNamesMatchPlannedDestinations(form, family);
            StringAssert.Contains(family.Text, "renamed_model.dx90.vtx");
            Assert.IsFalse(family.Text.Contains("release_model.dx90.vtx", StringComparison.Ordinal));

            releaseStem.Text = "old_source";
            Application.DoEvents();
            InvokePrivate(form, "FlushPendingModelNamePresentationForTests");
            AssertRelatedNamesMatchPlannedDestinations(form, family);
            StringAssert.Contains(family.Text, "old_source.phy");
            Assert.IsFalse(family.Text.Contains("renamed_model.phy", StringComparison.Ordinal));

            source.Text = secondModel;
            releaseStem.Text = "fresh_release";
            Application.DoEvents();
            InvokePrivate(form, "FlushPendingModelNamePresentationForTests");
            AssertRelatedNamesMatchPlannedDestinations(form, family);
            StringAssert.Contains(family.Text, "fresh_release.vvd");
            Assert.IsFalse(family.Text.Contains("old_source.vvd", StringComparison.Ordinal));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [STATestMethod]
    public void ReadmeStageContentScrollsAtSmallHeightAndFooterRemainsOutsideScroller()
    {
        using var form = new MainForm();
        form.Size = new Size(980, 520);
        InvokePrivate(form, "ShowEditor");
        form.Show();
        var tabs = GetField<TabControl>(form, "workflowTabs");
        tabs.SelectedIndex = 2;
        GetField<RadioButton>(form, "readmeGeneratedRadio").Checked = true;
        Application.DoEvents();

        var generatedLayout = tabs.TabPages[2].Controls.OfType<TableLayoutPanel>().Single();
        Assert.IsTrue(generatedLayout.AutoScroll);
        Assert.IsFalse(IsDescendant(GetField<Control>(form, "workflowNavigationPanel"), generatedLayout));

        GetField<RadioButton>(form, "readmeCustomRadio").Checked = true;
        Application.DoEvents();

        Assert.IsTrue(generatedLayout.AutoScroll);
        Assert.IsTrue(GetField<TextBox>(form, "customReadmeText").Visible);
        Assert.IsFalse(IsDescendant(GetField<Control>(form, "workflowNavigationPanel"), generatedLayout));
    }

    [STATestMethod]
    public void FileMenuExposesReturnToStartScreenNewFromCurrentAndStandardSave()
    {
        using var form = new MainForm();
        InvokePrivate(form, "ShowEditor");

        var menuText = string.Join("|", AllControls(form).OfType<MenuStrip>()
            .SelectMany(menu => menu.Items.OfType<ToolStripMenuItem>())
            .SelectMany(item => item.DropDownItems.OfType<ToolStripItem>())
            .Select(item => (item.Text ?? string.Empty).Replace("&", string.Empty, StringComparison.Ordinal)));

        StringAssert.Contains(menuText, "New From Current Project");
        StringAssert.Contains(menuText, "Return to Start Screen");
        StringAssert.Contains(menuText, "Save");
        StringAssert.Contains(menuText, "Save As...");
        StringAssert.Contains(menuText, "Defaults  Preferences...");
        Assert.IsFalse(menuText.Contains("Home", StringComparison.Ordinal));
        Assert.IsFalse(menuText.Contains("Save Package Project", StringComparison.Ordinal));
    }

    [STATestMethod]
    public void ReleaseDetailsShowsCurrentReleaseAndPreviousVersionControls()
    {
        using var form = new MainForm();
        var project = new PackageProject
        {
            AssetName = "Chair",
            CurrentVersion = "1.2",
            ChangesThisVersion = new List<string> { "Updated materials." }
        };
        project.ReleaseHistory.Add(new ReleaseRecord { Version = "1.1", Changes = new List<string> { "Initial polish." } });
        SetField(form, "project", project);
        InvokePrivate(form, "ShowEditor");
        InvokePrivate(form, "LoadProjectIntoControls");

        var tabs = GetField<TabControl>(form, "workflowTabs");
        tabs.SelectedIndex = 1;
        var pageText = string.Join("|", AllControls(tabs.TabPages[1]).Select(control => control.Text.Replace("&", string.Empty, StringComparison.Ordinal)));

        Assert.AreEqual("1.2", GetField<TextBox>(form, "versionText").Text);
        StringAssert.Contains(GetField<TextBox>(form, "changesText").Text, "Updated materials.");
        StringAssert.Contains(pageText, "Add to README Changelog");
        Assert.IsFalse(pageText.Contains("Add This Version to Changelog", StringComparison.Ordinal));
        StringAssert.Contains(pageText, "README changelog");
        var add = AllControls(tabs.TabPages[1]).OfType<Button>().Single(button => button.Text == "Add to README Changelog");
        var edit = GetField<Button>(form, "editSelectedReleaseButton");
        var remove = GetField<Button>(form, "removeSelectedReleaseButton");
        Assert.AreNotSame(add.Parent, edit.Parent);
        Assert.AreSame(edit.Parent, remove.Parent);
        Assert.IsTrue(add.Enabled);
        Assert.AreEqual(1, GetField<ListView>(form, "releaseHistoryList").Items.Count);
    }

    [STATestMethod]
    public void ReadmeChangelogAddButtonRequiresMeaningfulChangesText()
    {
        using var form = new MainForm();
        InvokePrivate(form, "ShowEditor");
        InvokePrivate(form, "LoadProjectIntoControls");
        form.Show();
        GetField<TabControl>(form, "workflowTabs").SelectedIndex = 1;
        Application.DoEvents();

        var changes = GetField<TextBox>(form, "changesText");
        var add = GetField<Button>(form, "addReleaseHistoryButton");

        changes.Text = string.Empty;
        Application.DoEvents();
        Assert.IsFalse(add.Enabled);

        changes.Text = "   ";
        Application.DoEvents();
        Assert.IsFalse(add.Enabled);

        changes.Text = "ADADAD";
        Application.DoEvents();
        Assert.IsTrue(add.Enabled);
    }

    [STATestMethod]
    public void ReadmeChangelogTableShowsOnlyEntriesWithMeaningfulChanges()
    {
        using var form = new MainForm();
        var project = new PackageProject
        {
            CurrentVersion = "1.0.5",
            ChangesThisVersion = new List<string>()
        };
        project.ReleaseHistory.Add(new ReleaseRecord { Version = "1.0.4", Changes = new List<string> { "" } });
        project.ReleaseHistory.Add(new ReleaseRecord { Version = "1.0.3", Changes = new List<string> { "ADADAD" } });
        project.ReleaseHistory.Add(new ReleaseRecord { Version = "1.0.0", Changes = new List<string> { "   " } });
        project.BuildHistory.Add(new BuildRecord { Version = "1.0.0", ArchiveName = "old.zip", BuildTimestamp = DateTimeOffset.UtcNow });
        SetField(form, "project", project);
        InvokePrivate(form, "ShowEditor");
        InvokePrivate(form, "LoadProjectIntoControls");

        var list = GetField<ListView>(form, "releaseHistoryList");

        Assert.AreEqual(1, list.Items.Count);
        Assert.AreEqual("1.0.3", list.Items[0].Text);
        Assert.AreEqual("ADADAD", list.Items[0].SubItems[1].Text);
        Assert.AreEqual(3, project.ReleaseHistory.Count);
        Assert.AreEqual(1, project.BuildHistory.Count);
    }

    [STATestMethod]
    public void BuildZipIsTheReviewFooterActionAndFollowsValidationState()
    {
        using var form = new MainForm();
        InvokePrivate(form, "ShowEditor");
        form.Show();
        var tabs = GetField<TabControl>(form, "workflowTabs");
        tabs.SelectedIndex = 4;
        Application.DoEvents();

        var build = GetField<Button>(form, "buildZipButton");
        var next = GetField<Button>(form, "nextStageButton");
        var previous = GetField<Button>(form, "previousStageButton");
        var forwardPanel = GetField<FlowLayoutPanel>(form, "workflowForwardPanel");
        var buildHost = GetField<Panel>(form, "buildZipButtonHost");

        Assert.IsTrue(build.Visible);
        Assert.IsFalse(next.Visible);
        Assert.AreEqual("< Back to Extras", previous.Text);
        Assert.AreSame(buildHost, build.Parent);
        Assert.AreSame(forwardPanel, buildHost.Parent);
        Assert.AreEqual(1, AllControls(form).OfType<Button>().Count(button => button.Text == "Build ZIP"));

        InvokePrivate(form, "LoadReviewCheck", new ValidationResult(new[]
        {
            new ValidationMessage(ValidationSeverity.Error, "error", "Fix this.")
        }, Array.Empty<RequiredDecision>()));

        Assert.IsFalse(build.Enabled);
        Assert.IsNull(form.AcceptButton);
        Assert.AreEqual(SystemColors.Control, build.BackColor);
        Assert.AreEqual(SystemColors.GrayText, build.ForeColor);

        InvokePrivate(form, "LoadReviewCheck", new ValidationResult(new[]
        {
            new ValidationMessage(ValidationSeverity.Warning, "warning", "Review this.")
        }, Array.Empty<RequiredDecision>()));

        Assert.IsTrue(build.Enabled);
        Assert.AreSame(build, form.AcceptButton);
        Assert.AreEqual(SystemColors.Highlight, build.BackColor);
        Assert.AreEqual(SystemColors.HighlightText, build.ForeColor);
    }

    [STATestMethod]
    public void BuildZipAppearanceTracksCleanWarningErrorAndCorrection()
    {
        using var form = new MainForm();
        InvokePrivate(form, "ShowEditor");
        form.Show();
        GetField<TabControl>(form, "workflowTabs").SelectedIndex = 4;
        Application.DoEvents();
        var build = GetField<Button>(form, "buildZipButton");

        InvokePrivate(form, "LoadReviewCheck", new ValidationResult(new[]
        {
            new ValidationMessage(ValidationSeverity.Ready, "ready", "Ready.")
        }, Array.Empty<RequiredDecision>()));

        Assert.IsTrue(build.Enabled);
        Assert.AreEqual(SystemColors.Highlight, build.BackColor);
        Assert.AreEqual(SystemColors.HighlightText, build.ForeColor);

        InvokePrivate(form, "LoadReviewCheck", new ValidationResult(new[]
        {
            new ValidationMessage(ValidationSeverity.Warning, "warning", "Review this.")
        }, Array.Empty<RequiredDecision>()));

        Assert.IsTrue(build.Enabled);
        Assert.AreEqual(SystemColors.Highlight, build.BackColor);
        Assert.AreEqual(SystemColors.HighlightText, build.ForeColor);

        InvokePrivate(form, "LoadReviewCheck", new ValidationResult(new[]
        {
            new ValidationMessage(ValidationSeverity.Error, "error", "Fix this.")
        }, Array.Empty<RequiredDecision>()));

        Assert.IsFalse(build.Enabled);
        Assert.AreEqual(SystemColors.Control, build.BackColor);
        Assert.AreEqual(SystemColors.GrayText, build.ForeColor);

        InvokePrivate(form, "LoadReviewCheck", new ValidationResult(new[]
        {
            new ValidationMessage(ValidationSeverity.Information, "info", "Looks good.")
        }, Array.Empty<RequiredDecision>()));

        Assert.IsTrue(build.Enabled);
        Assert.AreEqual(SystemColors.Highlight, build.BackColor);
        Assert.AreEqual(SystemColors.HighlightText, build.ForeColor);
    }

    [STATestMethod]
    public void BuildZipAndBackFooterActionsAreVerticallyAligned()
    {
        using var form = new MainForm();
        InvokePrivate(form, "ShowEditor");
        form.Show();
        GetField<TabControl>(form, "workflowTabs").SelectedIndex = 4;
        Application.DoEvents();

        var build = GetField<Button>(form, "buildZipButton");
        var previous = GetField<Button>(form, "previousStageButton");
        var forwardPanel = GetField<FlowLayoutPanel>(form, "workflowForwardPanel");
        var buildHost = GetField<Panel>(form, "buildZipButtonHost");
        var buildTop = build.PointToScreen(Point.Empty).Y;
        var previousTop = previous.PointToScreen(Point.Empty).Y;
        var buildCenter = buildTop + build.Height / 2;
        var previousCenter = previousTop + previous.Height / 2;

        Assert.AreSame(buildHost, build.Parent);
        Assert.AreSame(forwardPanel, buildHost.Parent);
        Assert.AreEqual(new Padding(0), build.Margin);
        Assert.AreEqual(0, previous.Margin.Top);
        Assert.IsTrue(Math.Abs(buildCenter - previousCenter) <= 4, $"Build ZIP center was {buildCenter}; Back center was {previousCenter}.");
    }

    [STATestMethod]
    public void ReleaseRemovalDialogUsesShortReadmeChangelogPrompt()
    {
        using var form = new MainForm();
        using var dialog = InvokePrivateResult<Form>(
            form,
            "CreateReleaseHistoryRemovalDialog",
            new ReleaseRecord { Version = "1.0.3" });

        var text = string.Join("|", AllControls(dialog).Select(control => control.Text));

        StringAssert.Contains(text, "Remove version 1.0.3 from the README changelog?");
        Assert.IsFalse(text.Contains("This removes it from the generated changelog.", StringComparison.Ordinal));
        Assert.AreEqual("Remove Changelog Entry", dialog.Text);
        CollectionAssert.AreEquivalent(
            new[] { "Remove", "Cancel" },
            AllControls(dialog).OfType<Button>().Select(button => button.Text).ToArray());
    }

    [STATestMethod]
    public void PackageDetailsHistoryButtonsFollowSelection()
    {
        using var form = new MainForm();
        var project = new PackageProject();
        project.ReleaseHistory.Add(new ReleaseRecord { Version = "1.0", Changes = new List<string> { "First." } });
        SetField(form, "project", project);
        InvokePrivate(form, "ShowEditor");
        InvokePrivate(form, "LoadProjectIntoControls");
        form.Show();
        GetField<TabControl>(form, "workflowTabs").SelectedIndex = 1;
        Application.DoEvents();
        var list = GetField<ListView>(form, "releaseHistoryList");
        var edit = GetField<Button>(form, "editSelectedReleaseButton");
        var remove = GetField<Button>(form, "removeSelectedReleaseButton");

        Assert.IsFalse(edit.Enabled);
        Assert.IsFalse(remove.Enabled);

        list.Items[0].Selected = true;
        list.Items[0].Focused = true;
        list.Select();
        Application.DoEvents();

        Assert.IsTrue(edit.Enabled);
        Assert.IsTrue(remove.Enabled);
    }

    [STATestMethod]
    public void ReadmeStageUsesSingleLineCreditsAndUsageTermsButKeepsUsefulMultilineFields()
    {
        using var form = new MainForm();
        InvokePrivate(form, "ShowEditor");
        InvokePrivate(form, "LoadProjectIntoControls");
        var tabs = GetField<TabControl>(form, "workflowTabs");
        tabs.SelectedIndex = 2;

        var pageText = string.Join("|", AllControls(tabs.TabPages[2]).Select(control => control.Text.Replace("&", string.Empty, StringComparison.Ordinal)));

        StringAssert.Contains(pageText, "Credits");
        StringAssert.Contains(pageText, "Credit the original creators, porters, or contributors.");
        Assert.IsFalse(GetField<TextBox>(form, "readmeLicenseText").Multiline);
        Assert.IsFalse(GetField<TextBox>(form, "creditsText").Multiline);
        Assert.IsTrue(GetField<TextBox>(form, "readmeDescriptionText").Multiline);
        Assert.IsTrue(GetField<TextBox>(form, "readmeResourcesText").Multiline);
        Assert.IsTrue(GetField<TextBox>(form, "customReadmeText").Multiline);
    }

    [STATestMethod]
    public void ReadmeStageUsesCommittedModeAndDetailCopy()
    {
        using var form = new MainForm();
        InvokePrivate(form, "ShowEditor");
        var tabs = GetField<TabControl>(form, "workflowTabs");
        tabs.SelectedIndex = 2;

        var pageText = string.Join("|", AllControls(tabs.TabPages[2]).Select(control => control.Text.Replace("&", string.Empty, StringComparison.Ordinal)));

        StringAssert.Contains(pageText, "Generate a README for me");
        StringAssert.Contains(pageText, "Write my own README");
        StringAssert.Contains(pageText, "Import an existing README file");
        StringAssert.Contains(pageText, "No README");
        StringAssert.Contains(pageText, "Uses your Release Info and README details.");
        Assert.IsFalse(pageText.Contains("Have SFM Package Builder create the README from your package details", StringComparison.Ordinal));
        Assert.IsFalse(pageText.Contains("Add any details you want included in the generated README.", StringComparison.Ordinal));
        StringAssert.Contains(pageText, "Credit the original creators, porters, or contributors.");
        StringAssert.Contains(pageText, "State how you want others to use or redistribute the model. Examples: CC0 1.0 or Do not redistribute.");
        Assert.IsFalse(pageText.Contains("Add any links or resources you want included in the README.", StringComparison.Ordinal));
        StringAssert.Contains(pageText, "Write it yourself, or generate a starting draft to edit.");
        StringAssert.Contains(pageText, "Generate Starting Draft");
        StringAssert.Contains(pageText, "This text will be saved as README.txt.");
        Assert.IsFalse(pageText.Contains("Start with a blank README and write the full text yourself.", StringComparison.Ordinal));
        Assert.IsFalse(pageText.Contains("Start with Generated README", StringComparison.Ordinal));
        Assert.IsFalse(pageText.Contains("Start from generated README", StringComparison.Ordinal));
        Assert.IsFalse(pageText.Contains("Generate the README from your package details first, then edit the text yourself.", StringComparison.Ordinal));
        Assert.IsFalse(pageText.Contains("Write the text that should become README.txt in the package.", StringComparison.Ordinal));
        Assert.IsFalse(pageText.Contains("ProjectText", StringComparison.Ordinal));
        Assert.IsFalse(pageText.Contains("ImportedFile", StringComparison.Ordinal));
    }

    [STATestMethod]
    public void ReadmeStageDoesNotExposeLegacyControlGroupsOptionAndKeepsResourcesCompact()
    {
        using var form = new MainForm();
        InvokePrivate(form, "ShowEditor");
        var tabs = GetField<TabControl>(form, "workflowTabs");
        tabs.SelectedIndex = 2;

        var readmePage = tabs.TabPages[2];
        var text = string.Join("|", AllControls(readmePage).Select(control => control.Text.Replace("&", string.Empty, StringComparison.Ordinal)));
        var resources = GetField<TextBox>(form, "readmeResourcesText");
        var description = GetField<TextBox>(form, "readmeDescriptionText");
        var visibleCheckboxes = AllControls(readmePage).OfType<CheckBox>().Where(checkBox => checkBox.Visible).ToArray();

        Assert.IsTrue(resources.Multiline);
        Assert.IsTrue(resources.Height < description.Height);
        Assert.IsFalse(text.Contains("Add any links or resources you want included in the README.", StringComparison.Ordinal));
        Assert.IsFalse(visibleCheckboxes.Any(checkBox => checkBox.Text == "SFM"));
        Assert.IsFalse(visibleCheckboxes.Any(checkBox => checkBox.Text.Contains("control-groups", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(text.Contains("Include control-groups information in README", StringComparison.Ordinal));
        Assert.IsTrue(FindScrollableParent(resources)?.AutoScroll ?? false);
    }

    [STATestMethod]
    public void ReadmePreviewCloseEscapeAndWindowCloseDismissWithoutCustomize()
    {
        var project = new PackageProject
        {
            AssetName = "Chair",
            Readme = new ReadmeConfig
            {
                Mode = ReadmeMode.Generated,
                CustomReadmeText = "owned"
            }
        };
        var beforeMode = project.Readme.Mode;
        var beforeCustomText = project.Readme.CustomReadmeText;
        var customizeCalls = 0;

        using (var form = new ReadmePreviewForm())
        {
            form.UseAsCustomRequested += (_, _) =>
            {
                customizeCalls++;
                project.Readme.Mode = ReadmeMode.Custom;
            };
            form.UpdatePreview("Generated text", canUseAsCustom: true);
            form.Show();
            Application.DoEvents();

            AllControls(form).OfType<Button>().Single(button => button.Text == "Close").PerformClick();
            Application.DoEvents();

            Assert.IsTrue(form.IsDisposed);
        }

        Assert.AreEqual(0, customizeCalls);
        Assert.AreEqual(beforeMode, project.Readme.Mode);
        Assert.AreEqual(beforeCustomText, project.Readme.CustomReadmeText);

        using (var form = new ReadmePreviewForm())
        {
            form.UseAsCustomRequested += (_, _) => customizeCalls++;
            form.UpdatePreview("Generated text", canUseAsCustom: true);
            form.Show();
            Application.DoEvents();

            InvokeProtectedBool(form, "ProcessCmdKey", new Message(), Keys.Escape);
            Application.DoEvents();

            Assert.IsTrue(form.IsDisposed);
        }

        using (var form = new ReadmePreviewForm())
        {
            form.UseAsCustomRequested += (_, _) => customizeCalls++;
            form.UpdatePreview("Generated text", canUseAsCustom: true);
            form.Show();
            Application.DoEvents();

            form.Close();
            Application.DoEvents();

            Assert.IsTrue(form.IsDisposed);
        }

        Assert.AreEqual(0, customizeCalls);
        Assert.AreEqual(beforeMode, project.Readme.Mode);
        Assert.AreEqual(beforeCustomText, project.Readme.CustomReadmeText);
    }

    [STATestMethod]
    public void ReadmePreviewCustomizeRemainsSeparateExplicitAction()
    {
        using var form = new ReadmePreviewForm();
        var customizeCalls = 0;
        form.UseAsCustomRequested += (_, _) => customizeCalls++;
        form.UpdatePreview("Generated text", canUseAsCustom: true);
        form.Show();
        Application.DoEvents();

        AllControls(form).OfType<Button>().Single(button => button.Text == "Customize this README").PerformClick();

        Assert.AreEqual(1, customizeCalls);
        Assert.IsFalse(form.IsDisposed);
    }

    [STATestMethod]
    public void SettingsFormUsesDefaultsPreferencesWordingAndHidesReadmeTemplate()
    {
        using var form = new SettingsForm(new AppSettings());
        var text = string.Join("|", AllControls(form).Select(control => control.Text));

        Assert.AreEqual("Defaults & Preferences", form.Text);
        StringAssert.Contains(text, "SFM usermod folder");
        StringAssert.Contains(text, @"Usually ...\SourceFilmmaker\game\usermod");
        StringAssert.Contains(text, "Project folder");
        StringAssert.Contains(text, "Starting folder for saving .sfmpack project files.");
        StringAssert.Contains(text, "ZIP output folder");
        StringAssert.Contains(text, "Starting folder for finished ZIP packages.");
        StringAssert.Contains(text, "Automatic ZIP filename");
        StringAssert.Contains(text, "Model usage terms");
        StringAssert.Contains(text, "{AssetName} uses the Title.");
        Assert.IsNotNull(GetField<TextBox>(form, "projectFolderText"));
        Assert.IsTrue(GetField<TextBox>(form, "authorText").MinimumSize.Width >= 360);
        Assert.IsTrue(GetField<TextBox>(form, "websiteText").MinimumSize.Width >= 360);
        Assert.IsTrue(GetField<TextBox>(form, "licenseText").Multiline);
        Assert.IsTrue(GetField<TextBox>(form, "licenseText").Height >= 90);
        Assert.IsTrue(GetField<TextBox>(form, "licenseText").MinimumSize.Width >= 360);
        Assert.IsFalse(text.Contains("Default README template", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("Credits", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("Archive filename pattern", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(text.Contains("General", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("Recent projects", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("Show recent packages on the start screen", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("Source models and materials.", StringComparison.Ordinal));
    }

    [STATestMethod]
    public void BuildResultInitialFocusIsOpenOutputFolder()
    {
        using var form = new BuildResultForm(@"C:\Exports\Apple_v2.zip", null);
        form.Show();
        Application.DoEvents();

        Assert.AreEqual("Build Complete", form.Text);
        Assert.AreEqual("Open Output Folder", form.ActiveControl?.Text);
    }

    [STATestMethod]
    public void BuildResultShowsThreeActionsAndLabelsOutputWithoutEditablePathBox()
    {
        using var form = new BuildResultForm(@"C:\Exports\Apple_v2.zip", null, () => true);
        form.Show();
        Application.DoEvents();

        var buttons = AllControls(form).OfType<Button>().Select(button => button.Text).ToArray();

        CollectionAssert.AreEquivalent(new[] { "Open Output Folder", "Start New Package", "Close" }, buttons);
        Assert.AreSame(AllControls(form).OfType<Button>().Single(button => button.Text == "Open Output Folder"), form.AcceptButton);
        Assert.AreSame(AllControls(form).OfType<Button>().Single(button => button.Text == "Close"), form.CancelButton);
        Assert.AreEqual(0, AllControls(form).OfType<TextBox>().Count());
        Assert.IsTrue(form.ClientSize.Width <= 640, $"Width was {form.ClientSize.Width}.");
        Assert.IsTrue(form.ClientSize.Height <= 280, $"Height was {form.ClientSize.Height}.");
        Assert.IsTrue(AllControls(form).OfType<PictureBox>().Any(box => box.Image is not null));
        var buttonParents = AllControls(form).OfType<Button>().Select(button => button.Parent).Distinct().ToArray();
        Assert.AreEqual(1, buttonParents.Length);
        Assert.IsInstanceOfType(buttonParents[0], typeof(FlowLayoutPanel));
        Assert.IsFalse(((FlowLayoutPanel)buttonParents[0]!).WrapContents);
        form.Close();
    }

    [STATestMethod]
    public void BuildResultEscapeAndWindowCloseDismissWithoutStartingNewPackage()
    {
        var startCalls = 0;
        using var escapeForm = new BuildResultForm(@"C:\Exports\Apple_v2.zip", null, () => { startCalls++; return true; });
        escapeForm.Show();
        Application.DoEvents();

        InvokeProtectedBool(escapeForm, "ProcessCmdKey", new Message(), Keys.Escape);
        Application.DoEvents();

        Assert.IsTrue(escapeForm.IsDisposed);
        Assert.AreEqual(0, startCalls);

        using var closeForm = new BuildResultForm(@"C:\Exports\Apple_v2.zip", null, () => { startCalls++; return true; });
        closeForm.Show();
        Application.DoEvents();
        closeForm.Close();
        Application.DoEvents();

        Assert.IsTrue(closeForm.IsDisposed);
        Assert.AreEqual(0, startCalls);
    }

    [STATestMethod]
    public void BuildProgressCloseRequestsCancellationAndWaitsForSafeClose()
    {
        var cancelCalls = 0;
        using var form = new BuildProgressForm(() => cancelCalls++);
        form.Show();
        Application.DoEvents();

        form.Close();
        Application.DoEvents();

        Assert.AreEqual(1, cancelCalls);
        Assert.IsFalse(form.IsDisposed);

        form.AllowClose();
        form.Close();

        Assert.IsTrue(form.IsDisposed);
    }

    [STATestMethod]
    public void ExtrasPlacementControlsFollowSelectionAndCustomChoice()
    {
        using var form = new MainForm();
        SetField(form, "project", ProjectWithExtras(
            new DestinationOverride { Kind = DestinationOverrideKind.Root, RelativePath = string.Empty },
            new DestinationOverride { Kind = DestinationOverrideKind.Custom, RelativePath = "Docs" }));
        InvokePrivate(form, "ShowEditor");
        InvokePrivate(form, "LoadProjectIntoControls");
        form.Show();
        GetField<TabControl>(form, "workflowTabs").SelectedIndex = 3;
        Application.DoEvents();

        var list = GetField<ListView>(form, "extrasList");
        var combo = GetField<ComboBox>(form, "extraDestinationCombo");
        var custom = GetField<TextBox>(form, "extraCustomDestinationText");
        var help = GetField<Label>(form, "extraSelectionHelpLabel");

        Assert.IsFalse(combo.Enabled);
        Assert.IsFalse(custom.Visible);
        Assert.IsTrue(help.Visible);

        list.Items[0].Selected = true;
        InvokePrivate(form, "LoadSelectedExtraDestination");
        Assert.IsTrue(combo.Enabled);
        Assert.AreEqual(ExtraDestinationPresenter.PackageRootChoice, combo.SelectedItem);
        Assert.IsFalse(custom.Visible);
        Assert.IsFalse(help.Visible);

        combo.SelectedItem = ExtraDestinationPresenter.CustomChoice;
        Assert.IsTrue(custom.Visible);
        Assert.IsTrue(custom.Enabled);

        list.Items[0].Selected = false;
        list.Items[1].Selected = true;
        InvokePrivate(form, "LoadSelectedExtraDestination");
        Assert.AreEqual(ExtraDestinationPresenter.DocumentationChoice, combo.SelectedItem);
        Assert.IsFalse(custom.Visible);
    }

    [STATestMethod]
    public void ExtrasStageUsesCommittedCopyAndExamples()
    {
        using var form = new MainForm();
        form.Size = new Size(1160, 813);
        InvokePrivate(form, "ShowEditor");
        InvokePrivate(form, "LoadProjectIntoControls");
        form.Show();
        var tabs = GetField<TabControl>(form, "workflowTabs");
        tabs.SelectedIndex = 3;
        Application.DoEvents();

        var pageText = string.Join("|", AllControls(tabs.TabPages[3]).Select(control => control.Text.Replace("&", string.Empty, StringComparison.Ordinal)));

        StringAssert.Contains(pageText, "Add optional files such as rigs, configs, or documentation.");
        Assert.IsFalse(pageText.Contains("Add any extra files you want included, such as rigs, configuration files, or documentation. Most releases can skip this step.", StringComparison.Ordinal));
        StringAssert.Contains(pageText, "Select a file in the table to set its location.");
        StringAssert.Contains(pageText, "Location for selected file");
        Assert.IsFalse(pageText.Contains("Choose where each extra file should appear in the ZIP.", StringComparison.Ordinal));
        Assert.IsFalse(pageText.Contains("Place selected item in:", StringComparison.Ordinal));
        StringAssert.Contains(pageText, "Folder inside ZIP");
        StringAssert.Contains(pageText, @"Examples: cfg\ or scripts\sfm\animset\.");
        var list = GetField<ListView>(form, "extrasList");
        Assert.AreEqual("No extras added.", list.Items[0].Text);
        Assert.IsFalse(list.Items[0].Text.Contains("Most releases", StringComparison.Ordinal));
        CollectionAssert.AreEqual(new[] { "File", "Package location" }, list.Columns.Cast<ColumnHeader>().Select(column => column.Text).ToArray());
        InvokePrivate(form, "UpdateExtrasColumns");
        Assert.IsTrue(list.ClientSize.Width > 0);
        Assert.IsTrue(list.Columns.Cast<ColumnHeader>().Sum(column => column.Width) <= list.ClientSize.Width);
        Assert.IsTrue(list.Columns[0].Width > list.Columns[1].Width);
        Assert.IsTrue(list.OwnerDraw);
        Assert.AreEqual(SystemColors.Window, MainForm.ExtrasHeaderBackgroundColor);
        Assert.AreEqual(FontStyle.Regular, MainForm.ExtrasHeaderFontStyle);
        Assert.AreNotEqual(SystemColors.ControlLight, MainForm.ExtrasHeaderBackgroundColor);
        var intro = AllControls(tabs.TabPages[3]).OfType<Label>()
            .Single(label => label.Text.StartsWith("Add optional files", StringComparison.Ordinal));
        Assert.IsTrue(intro.Margin.Bottom >= 10);
    }

    [STATestMethod]
    public void ReviewOutputOmitsZipFilenameHelperButKeepsResetAction()
    {
        using var form = new MainForm();
        InvokePrivate(form, "ShowEditor");
        var tabs = GetField<TabControl>(form, "workflowTabs");
        tabs.SelectedIndex = 4;

        var pageText = string.Join("|", AllControls(tabs.TabPages[4]).Select(control => control.Text.Replace("&", string.Empty, StringComparison.Ordinal)));

        Assert.IsFalse(pageText.Contains("Suggested from title and version. Type to change it.", StringComparison.Ordinal));
        Assert.IsFalse(pageText.Contains("Suggested from release name and version", StringComparison.Ordinal));
        StringAssert.Contains(pageText, "Reset to suggested name");
        var reset = GetField<Button>(form, "resetArchiveNameButton");
        var layout = (TableLayoutPanel)reset.Parent!;
        Assert.AreEqual(1, layout.GetColumn(reset));
        Assert.AreEqual(2, layout.GetRow(reset));
    }

    [STATestMethod]
    public void ReviewTreeResetsToTopAndDoesNotSelectRandomItem()
    {
        using var form = new MainForm();
        var plan = new PackagePlan(new[]
        {
            Entry("material", @"D:\dev\body.vtf", @"materials\models\Creator\chair\body.vtf"),
            Entry("model", @"D:\dev\chair_dev.mdl", @"models\Creator\chair\chair_release.mdl"),
            new PackagePlanEntry("readme", PackagePlanEntryType.Readme, PackagePlanEntryStatus.Resolved, "README.txt", null, null, null, true)
        }, Array.Empty<SharedFileNotice>());

        InvokePrivate(form, "LoadReviewTree", plan);
        var tree = GetField<TreeView>(form, "reviewPackageTree");

        Assert.AreEqual(tree.Nodes[0], tree.TopNode);
        Assert.IsNull(tree.SelectedNode);
        Assert.AreEqual("Select a file to see its source and package destination.", GetField<TextBox>(form, "reviewPackageDetailsText").Text);
    }

    [STATestMethod]
    public void ReviewTreeRestoresSelectionExpansionAndTopByStableDestination()
    {
        using var form = new MainForm();
        var plan = new PackagePlan(new[]
        {
            Entry("material-a", @"D:\dev\body.vtf", @"materials\models\Creator\chair\body.vtf"),
            Entry("material-b", @"D:\dev\eyes.vtf", @"materials\models\Creator\chair\eyes.vtf"),
            Entry("model", @"D:\dev\chair_dev.mdl", @"models\Creator\chair\chair_release.mdl"),
            new PackagePlanEntry("readme", PackagePlanEntryType.Readme, PackagePlanEntryStatus.Resolved, "README.txt", null, null, null, true)
        }, Array.Empty<SharedFileNotice>());

        InvokePrivate(form, "LoadReviewTree", plan);
        var tree = GetField<TreeView>(form, "reviewPackageTree");
        var models = FindTreeNode(tree.Nodes, "models")!;
        var materials = FindTreeNode(tree.Nodes, "materials")!;
        var body = FindTreeNodeByDestination(tree.Nodes, @"materials\models\Creator\chair\body.vtf")!;
        models.Collapse();
        tree.TopNode = materials;
        tree.SelectedNode = body;

        var refreshedPlan = new PackagePlan(new[]
        {
            Entry("material-a2", @"E:\stage\body.vtf", @"materials\models\Creator\chair\body.vtf"),
            Entry("material-b2", @"E:\stage\eyes.vtf", @"materials\models\Creator\chair\eyes.vtf"),
            Entry("model2", @"E:\stage\chair_release.mdl", @"models\Creator\chair\chair_release.mdl"),
            new PackagePlanEntry("readme2", PackagePlanEntryType.Readme, PackagePlanEntryStatus.Resolved, "README.txt", null, null, null, true)
        }, Array.Empty<SharedFileNotice>());

        InvokePrivate(form, "LoadReviewTree", refreshedPlan);

        Assert.AreEqual(@"materials\models\Creator\chair\body.vtf", GetTreeNodeDestination(tree.SelectedNode));
        Assert.AreEqual("materials", GetTreeNodePath(tree.TopNode));
        Assert.IsFalse(FindTreeNode(tree.Nodes, "models")!.IsExpanded);
    }

    [STATestMethod]
    public void ReviewPackageSplitGivesDetailsPaneReadableShare()
    {
        using var form = new MainForm();
        InvokePrivate(form, "ShowEditor");
        form.ClientSize = new Size(1100, 760);
        form.Show();
        GetField<TabControl>(form, "workflowTabs").SelectedIndex = 4;
        Application.DoEvents();
        var split = GetField<SplitContainer>(form, "reviewPackageSplit");

        InvokePrivate(form, "ConfigureReviewPackageSplitDistance");

        var usableWidth = split.ClientSize.Width - split.SplitterWidth;
        var treeRatio = split.SplitterDistance / (double)usableWidth;
        var detailsWidth = split.ClientSize.Width - split.SplitterDistance - split.SplitterWidth;
        Assert.IsTrue(treeRatio is >= 0.56 and <= 0.62, $"Tree ratio was {treeRatio}.");
        Assert.IsTrue(detailsWidth >= 350, $"Details width was {detailsWidth}.");
        Assert.IsTrue(split.SplitterDistance >= 300, $"Tree width was {split.SplitterDistance}.");
    }

    [STATestMethod]
    public void ReviewSelectedItemDetailsOmitResolvedStatus()
    {
        using var form = new MainForm();
        var plan = new PackagePlan(new[]
        {
            Entry("model", @"D:\dev\chair_dev.mdl", @"models\Creator\chair\chair_release.mdl")
        }, Array.Empty<SharedFileNotice>());

        InvokePrivate(form, "LoadReviewTree", plan);
        var tree = GetField<TreeView>(form, "reviewPackageTree");
        tree.SelectedNode = tree.Nodes[0].Nodes[0].Nodes[0].Nodes[0];
        InvokePrivate(form, "ShowReviewPackageDetail");

        var details = GetField<TextBox>(form, "reviewPackageDetailsText").Text;
        StringAssert.Contains(details, "Source:");
        StringAssert.Contains(details, "Package destination:");
        Assert.IsFalse(details.Contains("Resolved", StringComparison.Ordinal));
        Assert.IsFalse(details.Contains("Status:", StringComparison.Ordinal));
    }

    [STATestMethod]
    public void CleanPackageCheckShowsHumanReadyTextWithoutDuplicateReadyRow()
    {
        using var form = new MainForm();
        InvokePrivate(form, "ShowEditor");
        form.Show();
        GetField<TabControl>(form, "workflowTabs").SelectedIndex = 4;
        Application.DoEvents();
        var validation = new ValidationResult(new[]
        {
            new ValidationMessage(ValidationSeverity.Ready, "ready", "Ready.")
        }, Array.Empty<RequiredDecision>());

        InvokePrivate(form, "LoadReviewCheck", validation);
        var list = GetField<ListView>(form, "reviewCheckList");
        var summary = GetField<Label>(form, "reviewCheckSummaryLabel");

        Assert.AreEqual("Ready to build the ZIP.", summary.Text);
        Assert.IsFalse(list.Visible);
        Assert.AreEqual(0, list.Items.Count);
        Assert.AreEqual("Status", list.Columns[0].Text);
    }

    [STATestMethod]
    public void ReviewStageHasNoRefreshReviewSurface()
    {
        using var form = new MainForm();
        InvokePrivate(form, "ShowEditor");
        var tabs = GetField<TabControl>(form, "workflowTabs");
        tabs.SelectedIndex = 4;

        var pageText = string.Join("|", AllControls(tabs.TabPages[4]).Select(control => control.Text.Replace("&", string.Empty, StringComparison.Ordinal)));

        Assert.IsFalse(pageText.Contains("Review the files and folders that will be included in the ZIP.", StringComparison.Ordinal));
        Assert.IsTrue(AllControls(tabs.TabPages[4]).OfType<TreeView>().Any());
        Assert.IsFalse(pageText.Contains("Refresh Review", StringComparison.OrdinalIgnoreCase));
    }

    [STATestMethod]
    public void WarningPackageCheckUsesBuildStatusRows()
    {
        using var form = new MainForm();
        InvokePrivate(form, "ShowEditor");
        form.Show();
        GetField<TabControl>(form, "workflowTabs").SelectedIndex = 4;
        Application.DoEvents();
        var validation = new ValidationResult(new[]
        {
            new ValidationMessage(ValidationSeverity.Warning, "warning", "Review this.")
        }, Array.Empty<RequiredDecision>());

        InvokePrivate(form, "LoadReviewCheck", validation);
        var list = GetField<ListView>(form, "reviewCheckList");
        var summary = GetField<Label>(form, "reviewCheckSummaryLabel");

        Assert.AreEqual("You can build the ZIP. Review the warnings below.", summary.Text);
        Assert.IsTrue(list.Visible);
        Assert.AreEqual("Status", list.Columns[0].Text);
        Assert.AreEqual("Message", list.Columns[1].Text);
        Assert.AreEqual("Warning", list.Items[0].Text);
        Assert.IsTrue(list.Columns.Cast<ColumnHeader>().Sum(column => column.Width) <= list.ClientSize.Width);
        Assert.IsTrue(list.Columns[1].Width > list.Columns[0].Width);
    }

    [STATestMethod]
    public void InformationPackageCheckKeepsCleanOverallStatusWithReadableNote()
    {
        using var form = new MainForm();
        InvokePrivate(form, "ShowEditor");
        form.Show();
        GetField<TabControl>(form, "workflowTabs").SelectedIndex = 4;
        Application.DoEvents();
        var validation = new ValidationResult(new[]
        {
            new ValidationMessage(
                ValidationSeverity.Information,
                "info",
                "SFM control-groups file included. It may overwrite the user's existing setup.",
                detail: "This file is shared rather than model-specific, so package authors should include it intentionally.")
        }, Array.Empty<RequiredDecision>());

        InvokePrivate(form, "LoadReviewCheck", validation);
        var list = GetField<ListView>(form, "reviewCheckList");
        var summary = GetField<Label>(form, "reviewCheckSummaryLabel");

        Assert.AreEqual("Ready to build the ZIP.", summary.Text);
        Assert.IsTrue(list.Visible);
        Assert.AreEqual("Note", list.Items[0].Text);
        Assert.AreEqual("SFM control-groups file included. It may overwrite the user's existing setup.", list.Items[0].SubItems[1].Text);
        Assert.AreEqual("This file is shared rather than model-specific, so package authors should include it intentionally.", list.Items[0].Tag);
    }

    [STATestMethod]
    public void BuildStatusBannersAndBuildButtonMatchSeverityAndHideInternalDecisionNames()
    {
        using var form = new MainForm();
        InvokePrivate(form, "ShowEditor");
        form.Show();
        GetField<TabControl>(form, "workflowTabs").SelectedIndex = 4;
        Application.DoEvents();
        var build = GetField<Button>(form, "buildZipButton");
        var summary = GetField<Label>(form, "reviewCheckSummaryLabel");

        InvokePrivate(form, "LoadReviewCheck", new ValidationResult(new[]
        {
            new ValidationMessage(ValidationSeverity.Information, "info", "Package note.")
        }, Array.Empty<RequiredDecision>()));

        Assert.AreEqual("Ready to build the ZIP.", summary.Text);
        Assert.IsTrue(build.Enabled);

        InvokePrivate(form, "LoadReviewCheck", new ValidationResult(new[]
        {
            new ValidationMessage(ValidationSeverity.Warning, "warning", "Review this.")
        }, Array.Empty<RequiredDecision>()));

        Assert.AreEqual("You can build the ZIP. Review the warnings below.", summary.Text);
        Assert.IsTrue(build.Enabled);

        InvokePrivate(form, "LoadReviewCheck", new ValidationResult(new[]
        {
            new ValidationMessage(ValidationSeverity.Error, "error", "Fix this.")
        }, Array.Empty<RequiredDecision>()));

        Assert.AreEqual("Fix the problems below before building the ZIP.", summary.Text);
        Assert.IsFalse(build.Enabled);

        InvokePrivate(form, "LoadReviewCheck", new ValidationResult(
            Array.Empty<ValidationMessage>(),
            new[]
            {
                new RequiredDecision(
                    RequiredDecisionKind.VersionReuse,
                    "version-reuse",
                    "Version 1.0.0 already exists in this project.",
                    new[]
                    {
                        RequiredDecisionOption.RebuildExistingVersion,
                        RequiredDecisionOption.ChangeVersion,
                        RequiredDecisionOption.Cancel
                    })
            }));

        var list = GetField<ListView>(form, "reviewCheckList");
        var visibleText = summary.Text + "|" +
            string.Join("|", list.Items.Cast<ListViewItem>().Select(item => item.Text + "|" + item.SubItems[1].Text + "|" + item.Tag));
        Assert.AreEqual("You can build the ZIP. Review the warnings below.", summary.Text);
        Assert.IsTrue(build.Enabled);
        Assert.IsFalse(visibleText.Contains("RebuildExistingVersion", StringComparison.Ordinal));
        Assert.IsFalse(visibleText.Contains("ChangeVersion", StringComparison.Ordinal));
        Assert.IsFalse(visibleText.Contains("Decision required:", StringComparison.Ordinal));
    }

    [STATestMethod]
    public void ReviewCheckDetailStaysEmptyUntilMessageSelected()
    {
        using var form = new MainForm();
        InvokePrivate(form, "ShowEditor");
        form.Show();
        GetField<TabControl>(form, "workflowTabs").SelectedIndex = 4;
        Application.DoEvents();
        var validation = new ValidationResult(new[]
        {
            new ValidationMessage(
                ValidationSeverity.Warning,
                "warning",
                "Review this.",
                detail: "Full details.")
        }, Array.Empty<RequiredDecision>());

        InvokePrivate(form, "LoadReviewCheck", validation);
        var list = GetField<ListView>(form, "reviewCheckList");
        var detail = GetField<TextBox>(form, "reviewCheckDetailsText");

        Assert.AreEqual(string.Empty, detail.Text);
        list.Items[0].Selected = true;
        InvokePrivate(form, "LoadReviewCheck", validation);
        list = GetField<ListView>(form, "reviewCheckList");
        detail = GetField<TextBox>(form, "reviewCheckDetailsText");
        list.Items[0].Selected = true;
        Assert.AreEqual("Full details.", detail.Text);
    }

    [STATestMethod]
    public void ReviewCheckDetailTextUsesResponsiveSeventyPercentContentWidth()
    {
        using var form = new MainForm();
        InvokePrivate(form, "ShowEditor");
        form.Show();
        GetField<TabControl>(form, "workflowTabs").SelectedIndex = 4;
        Application.DoEvents();
        var validation = new ValidationResult(new[]
        {
            new ValidationMessage(
                ValidationSeverity.Warning,
                "warning",
                "Review this.",
                detail: "Long finding details should wrap in a deliberately readable text region.")
        }, Array.Empty<RequiredDecision>());

        InvokePrivate(form, "LoadReviewCheck", validation);
        var host = GetField<Panel>(form, "reviewCheckDetailsHostPanel");
        var detail = GetField<TextBox>(form, "reviewCheckDetailsText");
        InvokePrivate(form, "ConfigureReviewCheckDetailsWidth");
        var initialWidth = detail.Width;
        var initialRatio = detail.Width / (double)host.ClientSize.Width;

        Assert.IsTrue(initialRatio is >= 0.65 and <= 0.75, $"Initial detail ratio was {initialRatio:P1}.");

        form.Width += 180;
        Application.DoEvents();
        InvokePrivate(form, "ConfigureReviewCheckDetailsWidth");
        var expandedRatio = detail.Width / (double)host.ClientSize.Width;

        Assert.IsTrue(detail.Width > initialWidth);
        Assert.IsTrue(expandedRatio is >= 0.65 and <= 0.75, $"Expanded detail ratio was {expandedRatio:P1}.");
    }

    [TestMethod]
    public void ValidationPresenterShowsStructuredDetailFields()
    {
        var validation = new ValidationResult(new[]
        {
            new ValidationMessage(
                ValidationSeverity.Warning,
                "mdl-material-referenced-not-packaged",
                "Some materials used by this model are not included.",
                sourcePaths: new[] { @"D:\models\chair.mdl" },
                destinationRelativePath: @"models\chair.mdl",
                detail: "They may come from Source Filmmaker or another required addon.",
                affectedValues: new[] { "models/chair/body" },
                candidateDestinations: new[] { @"materials\models\chair\body.vmt" })
        }, Array.Empty<RequiredDecision>());

        var item = new ValidationPresenter().Present(validation).Single();

        Assert.AreEqual("Some materials used by this model are not included.", item.Title);
        StringAssert.Contains(item.Details, "They may come from Source Filmmaker");
        StringAssert.Contains(item.Details, "Package destination:");
        StringAssert.Contains(item.Details, @"models\chair.mdl");
        StringAssert.Contains(item.Details, "Source path:");
        StringAssert.Contains(item.Details, @"D:\models\chair.mdl");
        StringAssert.Contains(item.Details, "Affected values:");
        StringAssert.Contains(item.Details, "models/chair/body");
        StringAssert.Contains(item.Details, "Candidate material locations:");
        StringAssert.Contains(item.Details, @"materials\models\chair\body.vmt");
    }

    [TestMethod]
    public void ValidationPresenterAggregatesDuplicateSourceDestinationInformation()
    {
        var validation = new ValidationResult(new[]
        {
            new ValidationMessage(
                ValidationSeverity.Information,
                "duplicate-source-destination",
                "The same source is included more than once for the same package destination.",
                sourcePaths: new[] { @"D:\materials\a.vtf", @"D:\materials\a.vtf" },
                destinationRelativePath: @"materials\models\a.vtf"),
            new ValidationMessage(
                ValidationSeverity.Information,
                "duplicate-source-destination",
                "The same source is included more than once for the same package destination.",
                sourcePaths: new[] { @"D:\materials\b.vtf", @"D:\materials\b.vtf" },
                destinationRelativePath: @"materials\models\b.vtf"),
            new ValidationMessage(
                ValidationSeverity.Information,
                "duplicate-source-destination",
                "The same source is included more than once for the same package destination.",
                sourcePaths: new[] { @"D:\materials\c.vtf", @"D:\materials\c.vtf" },
                destinationRelativePath: @"materials\models\c.vtf")
        }, Array.Empty<RequiredDecision>());

        var item = new ValidationPresenter().Present(validation).Single();

        Assert.AreEqual(ValidationSeverity.Information, item.Severity);
        Assert.AreEqual("3 files are included through more than one source. They will only be packaged once.", item.Title);
        StringAssert.Contains(item.Details, @"materials\models\a.vtf");
        StringAssert.Contains(item.Details, @"materials\models\b.vtf");
        StringAssert.Contains(item.Details, @"materials\models\c.vtf");
        StringAssert.Contains(item.Details, @"D:\materials\a.vtf");
    }

    [TestMethod]
    public void ValidationPresenterAggregatesRenamedPrimaryAdditionalModelFamilyCollisions()
    {
        var primaryId = Guid.NewGuid();
        var additionalId = Guid.NewGuid();
        var project = new PackageProject
        {
            Models = new List<ModelEntry>
            {
                new() { Id = primaryId, Role = ModelRole.Primary, SourceStem = "gwen_chadchan3d_v1_02", ReleaseStem = "gwen_chadchan3d_v1_04" },
                new() { Id = additionalId, Role = ModelRole.Additional, SourceStem = "gwen_chadchan3d_v1_04", ReleaseStem = "gwen_chadchan3d_v1_04" }
            }
        };
        var plan = new PackagePlan(new[]
        {
            ModelFamilyEntry("primary-mdl", primaryId, PackagePlanEntryType.Model, @"E:\dev\v1_02\gwen_chadchan3d_v1_02.mdl", @"models\DevilsCry\ben10\gwen_chadchan3d_v1_04.mdl"),
            ModelFamilyEntry("additional-mdl", additionalId, PackagePlanEntryType.Model, @"E:\dev\v1_04\gwen_chadchan3d_v1_04.mdl", @"models\DevilsCry\ben10\gwen_chadchan3d_v1_04.mdl"),
            ModelFamilyEntry("primary-vvd", primaryId, PackagePlanEntryType.ModelCompanion, @"E:\dev\v1_02\gwen_chadchan3d_v1_02.vvd", @"models\DevilsCry\ben10\gwen_chadchan3d_v1_04.vvd"),
            ModelFamilyEntry("additional-vvd", additionalId, PackagePlanEntryType.ModelCompanion, @"E:\dev\v1_04\gwen_chadchan3d_v1_04.vvd", @"models\DevilsCry\ben10\gwen_chadchan3d_v1_04.vvd"),
            ModelFamilyEntry("primary-dx90", primaryId, PackagePlanEntryType.ModelCompanion, @"E:\dev\v1_02\gwen_chadchan3d_v1_02.dx90.vtx", @"models\DevilsCry\ben10\gwen_chadchan3d_v1_04.dx90.vtx"),
            ModelFamilyEntry("additional-dx90", additionalId, PackagePlanEntryType.ModelCompanion, @"E:\dev\v1_04\gwen_chadchan3d_v1_04.dx90.vtx", @"models\DevilsCry\ben10\gwen_chadchan3d_v1_04.dx90.vtx"),
            ModelFamilyEntry("primary-phy", primaryId, PackagePlanEntryType.ModelCompanion, @"E:\dev\v1_02\gwen_chadchan3d_v1_02.phy", @"models\DevilsCry\ben10\gwen_chadchan3d_v1_04.phy"),
            ModelFamilyEntry("additional-phy", additionalId, PackagePlanEntryType.ModelCompanion, @"E:\dev\v1_04\gwen_chadchan3d_v1_04.phy", @"models\DevilsCry\ben10\gwen_chadchan3d_v1_04.phy")
        }, Array.Empty<SharedFileNotice>());
        var validation = new ValidationResult(new CollisionValidator().Validate(plan), Array.Empty<RequiredDecision>());

        var coreCollisions = validation.Messages.Where(message => message.Code == "destination-collision").ToArray();
        var presented = new ValidationPresenter().Present(validation, project, plan).ToArray();

        Assert.AreEqual(4, coreCollisions.Length);
        Assert.IsTrue(coreCollisions.All(message => message.Severity == ValidationSeverity.Error));
        Assert.AreEqual(1, presented.Length);
        Assert.AreEqual(ValidationSeverity.Error, presented[0].Severity);
        Assert.AreEqual("The renamed primary model conflicts with an Additional Model at 4 package locations.", presented[0].Title);
        StringAssert.Contains(presented[0].Details, "Conflicting package locations:");
        StringAssert.Contains(presented[0].Details, @"models\DevilsCry\ben10\gwen_chadchan3d_v1_04.mdl");
        StringAssert.Contains(presented[0].Details, @"models\DevilsCry\ben10\gwen_chadchan3d_v1_04.vvd");
        StringAssert.Contains(presented[0].Details, @"models\DevilsCry\ben10\gwen_chadchan3d_v1_04.dx90.vtx");
        StringAssert.Contains(presented[0].Details, @"models\DevilsCry\ben10\gwen_chadchan3d_v1_04.phy");
        StringAssert.Contains(presented[0].Details, "Competing sources:");
        StringAssert.Contains(presented[0].Details, @"E:\dev\v1_02\gwen_chadchan3d_v1_02.mdl");
        StringAssert.Contains(presented[0].Details, @"E:\dev\v1_04\gwen_chadchan3d_v1_04.mdl");
    }

    [TestMethod]
    public void ValidationPresenterDoesNotUseRenamedPrimaryCopyForGenericCollisions()
    {
        var plan = new PackagePlan(new[]
        {
            new PackagePlanEntry("extra-a", PackagePlanEntryType.Extra, PackagePlanEntryStatus.Resolved, @"Docs\guide.txt", @"E:\a\guide.txt", Guid.NewGuid(), null, false),
            new PackagePlanEntry("extra-b", PackagePlanEntryType.Extra, PackagePlanEntryStatus.Resolved, @"Docs\guide.txt", @"E:\b\guide.txt", Guid.NewGuid(), null, false)
        }, Array.Empty<SharedFileNotice>());
        var validation = new ValidationResult(new CollisionValidator().Validate(plan), Array.Empty<RequiredDecision>());

        var item = new ValidationPresenter().Present(validation, new PackageProject(), plan).Single();

        Assert.AreEqual("Two files would use the same location in the ZIP.", item.Title);
        Assert.IsFalse(item.Title.Contains("renamed primary model", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void AboutUsesAssemblyVersionAndExactAssetsUrlAndLicense()
    {
        var assembly = typeof(AboutForm).Assembly;
        var expected = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
        var metadata = expected.IndexOf('+', StringComparison.Ordinal);
        if (metadata >= 0)
        {
            expected = expected[..metadata];
        }

        Assert.AreEqual(expected, AboutForm.GetApplicationVersion());
        Assert.AreEqual("https://chadchan3d.com/category/assets/", AboutForm.AssetsUrl);
        Assert.AreEqual("https://creativecommons.org/publicdomain/zero/1.0/", AboutForm.Cc0Url);
        Assert.AreEqual("CC0 1.0 Universal", AboutForm.ApplicationLicense);
    }

    [STATestMethod]
    public void AboutShowsSettledLicenseAndAssetLibraryLinks()
    {
        using var form = new AboutForm();
        var text = string.Join("|", AllControls(form).Select(control => control.Text));

        StringAssert.Contains(text, "Version 1.0.4");
        StringAssert.Contains(text, "Build ZIP packages for Source Filmmaker model releases.");
        StringAssert.Contains(text, "SFM Package Builder's original code and assets are dedicated to the public domain under CC0 1.0 Universal.");
        StringAssert.Contains(text, "View CC0 1.0 license");
        StringAssert.Contains(text, "ChadChan3D Asset Library");
        Assert.IsTrue(form.ClientSize.Height <= 280);
        Assert.IsFalse(text.Contains("Create ZIP packages for compiled Source Filmmaker models.", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("Program license:", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("Updates & SFM tools", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("chadchan3d.com/category/assets/", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ReleaseRuntimeDocsAndVisibleUiDoNotRequireSevenZip()
    {
        var root = FindWorkspaceRoot();
        var releaseReadme = File.ReadAllText(Path.Combine(root, "outputs", "release-assets", "README.md"));
        var bundledReadme = File.ReadAllText(Path.Combine(root, "outputs", "release-assets", "README.txt"));
        var runtimeNotices = File.ReadAllText(Path.Combine(root, "outputs", "release-assets", "licenses", "DOTNET_RUNTIME_THIRD_PARTY_NOTICES.txt"));

        StringAssert.Contains(releaseReadme, "# SFM Package Builder 1.0.4");
        StringAssert.Contains(releaseReadme, "Build ZIP packages for Source Filmmaker model releases.");
        StringAssert.Contains(releaseReadme, "A same-version warning applies when that version has already been built from the project.");
        StringAssert.Contains(releaseReadme, "No external archive application is required. ZIP creation uses the application runtime.");
        StringAssert.Contains(bundledReadme, "SFM PACKAGE BUILDER 1.0.4");
        StringAssert.Contains(bundledReadme, "1. Extract the ZIP to a folder.");
        StringAssert.Contains(bundledReadme, "2. Run SfmPackageBuilder.exe.");
        StringAssert.Contains(bundledReadme, "Windows may show an Unknown Publisher warning because the application is not code-signed.");
        StringAssert.Contains(bundledReadme, "https://chadchan3d.com/category/assets/");
        StringAssert.Contains(NormalizeLineEndings(bundledReadme), "LICENSE\n-------");
        StringAssert.Contains(bundledReadme, "Third-party runtime components retain their own licenses. See the licenses folder.");
        Assert.IsFalse(releaseReadme.Contains("1.0.0", StringComparison.Ordinal));
        Assert.IsFalse(releaseReadme.Contains("RC2", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(releaseReadme.Contains("compiled Source Filmmaker", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(releaseReadme.Contains("current version is already present in release history", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(releaseReadme.Contains("7-Zip", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(releaseReadme.Contains("7z.exe", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(bundledReadme.Contains("7-Zip", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(runtimeNotices.Contains("7-Zip", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(runtimeNotices.Contains("7z.exe", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void ActiveWinFormsReleaseMetadataMatchesCurrentVersionAndDescription()
    {
        var root = FindWorkspaceRoot();
        var projectFile = File.ReadAllText(Path.Combine(root, "src", "SfmPackageBuilder.WinForms", "SfmPackageBuilder.WinForms.csproj"));
        var coreProjectFile = File.ReadAllText(Path.Combine(root, "src", "SfmPackageBuilder.Core", "SfmPackageBuilder.Core.csproj"));
        var manifest = File.ReadAllText(Path.Combine(root, "src", "SfmPackageBuilder.WinForms", "app.manifest"));

        StringAssert.Contains(projectFile, "<Description>Build ZIP packages for Source Filmmaker model releases.</Description>");
        StringAssert.Contains(projectFile, "<Version>1.0.4</Version>");
        StringAssert.Contains(projectFile, "<AssemblyVersion>1.0.4.0</AssemblyVersion>");
        StringAssert.Contains(projectFile, "<FileVersion>1.0.4.0</FileVersion>");
        StringAssert.Contains(projectFile, "<InformationalVersion>1.0.4</InformationalVersion>");
        StringAssert.Contains(coreProjectFile, "<Version>1.0.4</Version>");
        StringAssert.Contains(coreProjectFile, "<AssemblyVersion>1.0.4.0</AssemblyVersion>");
        StringAssert.Contains(coreProjectFile, "<FileVersion>1.0.4.0</FileVersion>");
        StringAssert.Contains(coreProjectFile, "<InformationalVersion>1.0.4</InformationalVersion>");
        StringAssert.Contains(manifest, "<assemblyIdentity version=\"1.0.4.0\" name=\"SfmPackageBuilder.app\"/>");
        Assert.IsFalse(projectFile.Contains("compiled Source Filmmaker", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(coreProjectFile.Contains("1.0.1", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(manifest.Contains("version=\"1.0.0.0\"", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void ApplicationReadmeVersionsMatchAuthoritativeProductVersion()
    {
        var root = FindWorkspaceRoot();
        var version = AuthoritativeProductVersion(root);

        AssertReadmeVersion(Path.Combine(root, "README.txt"), version);
        AssertReadmeVersion(Path.Combine(root, "outputs", "release-assets", "README.txt"), version);
    }

    [STATestMethod]
    public void AboutShowsSmallEmblem()
    {
        using var form = new AboutForm();

        Assert.IsTrue(AllControls(form).OfType<PictureBox>().Any(box => box.Image is not null));
    }

    [TestMethod]
    public void ReleaseAssetsUseBundledReadmeAndLicensesFolder()
    {
        var root = FindWorkspaceRoot();
        var readmePath = Path.Combine(root, "outputs", "release-assets", "README.txt");
        var licensePath = Path.Combine(root, "outputs", "release-assets", "licenses", "DOTNET_RUNTIME_LICENSE.txt");
        var noticePath = Path.Combine(root, "outputs", "release-assets", "licenses", "DOTNET_RUNTIME_THIRD_PARTY_NOTICES.txt");
        var windowsDesktopLicensePath = Path.Combine(root, "outputs", "release-assets", "licenses", "WINDOWS_DESKTOP_RUNTIME_LICENSE.txt");

        Assert.AreEqual(ExpectedBundledReadme(), NormalizeLineEndings(File.ReadAllText(readmePath)));
        Assert.IsFalse(File.Exists(Path.Combine(root, "outputs", "release-assets", "LICENSE.txt")));
        Assert.IsFalse(File.Exists(Path.Combine(root, "outputs", "release-assets", "THIRD_PARTY_NOTICES.md")));
        Assert.IsTrue(File.Exists(Path.Combine(root, "outputs", "release-assets", "README.md")));
        Assert.IsTrue(File.Exists(licensePath), licensePath);
        Assert.IsTrue(File.Exists(noticePath), noticePath);
        Assert.IsTrue(File.Exists(windowsDesktopLicensePath), windowsDesktopLicensePath);
        StringAssert.Contains(File.ReadAllText(licensePath), "The MIT License (MIT)");
        StringAssert.Contains(File.ReadAllText(windowsDesktopLicensePath), "The MIT License (MIT)");
        Assert.IsTrue(new FileInfo(noticePath).Length > 50000);
        Assert.IsFalse(File.ReadAllText(noticePath).Contains("release candidate", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void ReleaseLicensesComeFromCurrentRuntimePacks()
    {
        var root = FindWorkspaceRoot();
        var nugetRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
        var runtimePack = Path.Combine(nugetRoot, "microsoft.netcore.app.runtime.win-x64", "10.0.11");
        var desktopPack = Path.Combine(nugetRoot, "microsoft.windowsdesktop.app.runtime.win-x64", "10.0.11");

        CollectionAssert.AreEqual(
            File.ReadAllBytes(Path.Combine(runtimePack, "LICENSE.TXT")),
            File.ReadAllBytes(Path.Combine(root, "outputs", "release-assets", "licenses", "DOTNET_RUNTIME_LICENSE.txt")));
        CollectionAssert.AreEqual(
            File.ReadAllBytes(Path.Combine(runtimePack, "THIRD-PARTY-NOTICES.TXT")),
            File.ReadAllBytes(Path.Combine(root, "outputs", "release-assets", "licenses", "DOTNET_RUNTIME_THIRD_PARTY_NOTICES.txt")));
        CollectionAssert.AreEqual(
            File.ReadAllBytes(Path.Combine(desktopPack, "LICENSE")),
            File.ReadAllBytes(Path.Combine(root, "outputs", "release-assets", "licenses", "WINDOWS_DESKTOP_RUNTIME_LICENSE.txt")));
    }

    private static PackagePlanEntry Entry(string id, string source, string destination) =>
        new(
            id,
            PackagePlanEntryType.Model,
            PackagePlanEntryStatus.Resolved,
            destination,
            source,
            Guid.NewGuid(),
            Guid.NewGuid(),
            isGenerated: false);

    private static PackagePlanEntry ModelFamilyEntry(
        string id,
        Guid modelId,
        PackagePlanEntryType type,
        string source,
        string destination) =>
        new(
            id,
            type,
            PackagePlanEntryStatus.Resolved,
            destination,
            source,
            modelId,
            modelId,
            isGenerated: false);

    private static PackageProject ProjectWithExtras(params DestinationOverride[] overrides)
    {
        var project = new PackageProject();
        for (var i = 0; i < overrides.Length; i++)
        {
            project.Extras.Add(new SourceEntry
            {
                Kind = SourceEntryKind.Extra,
                SourcePath = $@"D:\extras\file{i}.txt",
                DestinationOverride = overrides[i]
            });
        }

        return project;
    }

    private static PackageProject CreatePrimaryProject(string modelPath, string releaseStem)
    {
        var project = new PackageProject();
        project.Models.Add(new ModelEntry
        {
            Role = ModelRole.Primary,
            SourceMdlPath = modelPath,
            SourceStem = Path.GetFileNameWithoutExtension(modelPath),
            ReleaseStem = releaseStem
        });
        return project;
    }

    private static string CreateModelFamily(string root, string stem)
    {
        var directory = Path.Combine(root, "game", "usermod", "models", "creator", stem);
        Directory.CreateDirectory(directory);
        foreach (var extension in new[] { ".mdl", ".vvd", ".dx90.vtx", ".dx80.vtx", ".sw.vtx", ".phy" })
        {
            File.WriteAllText(Path.Combine(directory, stem + extension), extension);
        }

        return Path.Combine(directory, stem + ".mdl");
    }

    private static void AssertRelatedNamesMatchPlannedDestinations(MainForm form, Label family)
    {
        InvokePrivate(form, "SyncProjectFromControls");
        var project = GetField<PackageProject>(form, "project");
        var primary = project.Models.Single(model => model.Role == ModelRole.Primary);
        var coordinator = InvokePrivateResult<BuildCoordinator>(form, "CreateCoordinator");
        var expected = coordinator.PreviewPackage(project).Entries
            .Where(entry => entry.EntryType == PackagePlanEntryType.ModelCompanion)
            .Where(entry => entry.ModelEntryId == primary.Id)
            .Select(entry => Path.GetFileName(entry.DestinationRelativePath ?? string.Empty))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();
        var actual = family.Text.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);

        CollectionAssert.AreEqual(expected, actual);
        Assert.AreEqual(expected.Length > 0, family.Visible);
        Assert.AreEqual(
            expected.Length > 0 ? $"Related model files included ({expected.Length})" : string.Empty,
            GetField<Label>(form, "modelFamilySummaryLabel").Text);
    }

    private static void SetField(object instance, string name, object value) =>
        instance.GetType().GetField(name, PrivateInstance)?.SetValue(instance, value);

    private static void InvokePrivate(object instance, string name) =>
        instance.GetType().GetMethod(name, PrivateInstance)?.Invoke(instance, Array.Empty<object>());

    private static void InvokePrivate(object instance, string name, params object[] args) =>
        instance.GetType().GetMethod(name, PrivateInstance)?.Invoke(instance, args);

    private static T InvokePrivateResult<T>(object instance, string name, params object[] args) where T : class =>
        (T)(instance.GetType().GetMethod(name, PrivateInstance)?.Invoke(instance, args)
            ?? throw new InvalidOperationException("Method not found: " + name));

    private static bool InvokeProtectedBool(object instance, string name, params object[] args) =>
        (bool)(FindMethod(instance.GetType(), name)?.Invoke(instance, args)
            ?? throw new InvalidOperationException("Method not found: " + name));

    private static MethodInfo? FindMethod(Type type, string name)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            var method = current.GetMethod(name, PrivateInstance);
            if (method is not null)
            {
                return method;
            }
        }

        return null;
    }

    private static T GetField<T>(object instance, string name) where T : class =>
        (T)(instance.GetType().GetField(name, PrivateInstance)?.GetValue(instance)
            ?? throw new InvalidOperationException("Field not found: " + name));

    private static ScrollableControl? FindScrollableParent(Control control)
    {
        for (var parent = control.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent is ScrollableControl { AutoScroll: true } scrollable)
            {
                return scrollable;
            }
        }

        return null;
    }

    private static TreeNode? FindTreeNode(TreeNodeCollection nodes, string path)
    {
        foreach (TreeNode node in nodes)
        {
            if (string.Equals(GetTreeNodePath(node), path, StringComparison.OrdinalIgnoreCase))
            {
                return node;
            }

            var child = FindTreeNode(node.Nodes, path);
            if (child is not null)
            {
                return child;
            }
        }

        return null;
    }

    private static TreeNode? FindTreeNodeByDestination(TreeNodeCollection nodes, string destination)
    {
        foreach (TreeNode node in nodes)
        {
            if (string.Equals(GetTreeNodeDestination(node), destination, StringComparison.OrdinalIgnoreCase))
            {
                return node;
            }

            var child = FindTreeNodeByDestination(node.Nodes, destination);
            if (child is not null)
            {
                return child;
            }
        }

        return null;
    }

    private static string? GetTreeNodeDestination(TreeNode? node) =>
        node?.Tag is PackageTreeNodeModel { Entry: { DestinationRelativePath: not null } entry }
            ? entry.DestinationRelativePath
            : null;

    private static string? GetTreeNodePath(TreeNode? node) =>
        node?.Tag is PackageTreeNodeModel model
            ? (model.Entry?.DestinationRelativePath ?? model.FullPath).Replace('/', '\\').Trim('\\')
            : node?.FullPath;

    private static string FindWorkspaceRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SfmPackageBuilder.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Workspace root was not found.");
    }

    private static string NormalizeLineEndings(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal);

    private static string AuthoritativeProductVersion(string root)
    {
        var project = XDocument.Load(Path.Combine(root, "src", "SfmPackageBuilder.WinForms", "SfmPackageBuilder.WinForms.csproj"));
        return project.Root?
            .Elements("PropertyGroup")
            .Elements("Version")
            .Select(element => element.Value.Trim())
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
            ?? throw new InvalidOperationException("Authoritative product version was not found.");
    }

    private static void AssertReadmeVersion(string path, string expectedVersion)
    {
        var firstLine = File.ReadLines(path).First();
        Assert.AreEqual($"SFM PACKAGE BUILDER {expectedVersion}", firstLine);
    }

    private static string ExpectedBundledReadme() =>
        "SFM PACKAGE BUILDER 1.0.4\n" +
        "=========================\n" +
        "\n" +
        "Build release-ready ZIP packages for Source Filmmaker models.\n" +
        "\n" +
        "HOW TO RUN\n" +
        "----------\n" +
        "1. Extract the ZIP to a folder.\n" +
        "2. Run SfmPackageBuilder.exe.\n" +
        "\n" +
        "No separate .NET installation is required.\n" +
        "\n" +
        "Windows may show an Unknown Publisher warning because the application is not code-signed.\n" +
        "\n" +
        "More information and updates:\n" +
        "https://chadchan3d.com/category/assets/\n" +
        "\n" +
        "LICENSE\n" +
        "-------\n" +
        "SFM Package Builder's original code and application assets are dedicated\n" +
        "to the public domain under CC0 1.0 Universal.\n" +
        "\n" +
        "https://creativecommons.org/publicdomain/zero/1.0/\n" +
        "\n" +
        "Third-party runtime components retain their own licenses. See the licenses folder.\n";

    private static IEnumerable<Control> AllControls(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (var grandchild in AllControls(child))
            {
                yield return grandchild;
            }
        }
    }

    private static bool IsDescendant(Control control, Control possibleAncestor)
    {
        for (var parent = control.Parent; parent is not null; parent = parent.Parent)
        {
            if (ReferenceEquals(parent, possibleAncestor))
            {
                return true;
            }
        }

        return false;
    }
}
