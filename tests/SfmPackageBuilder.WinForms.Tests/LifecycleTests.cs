using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Buffers.Binary;
using System.Globalization;
using System.Reflection;
using System.Text;
using SfmPackageBuilder.Core.Build;
using SfmPackageBuilder.Core.Domain;
using SfmPackageBuilder.Core.Persistence;
using SfmPackageBuilder.Core.Planning;
using SfmPackageBuilder.WinForms.Presentation;

namespace SfmPackageBuilder.WinForms.Tests;

[TestClass]
public sealed class LifecycleTests
{
    private static readonly BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    [TestMethod]
    public void UnsavedWorkResolverHonorsSaveDiscardCancelAndNestedSaveFailure()
    {
        var cases = new[]
        {
            (Dirty: false, Choice: UnsavedWorkChoice.Cancel, SaveSucceeds: true, Expected: UnsavedWorkResult.Saved, ExpectedSaveCalls: 0),
            (Dirty: true, Choice: UnsavedWorkChoice.Discard, SaveSucceeds: true, Expected: UnsavedWorkResult.Discarded, ExpectedSaveCalls: 0),
            (Dirty: true, Choice: UnsavedWorkChoice.Cancel, SaveSucceeds: true, Expected: UnsavedWorkResult.Cancelled, ExpectedSaveCalls: 0),
            (Dirty: true, Choice: UnsavedWorkChoice.Save, SaveSucceeds: true, Expected: UnsavedWorkResult.Saved, ExpectedSaveCalls: 1),
            (Dirty: true, Choice: UnsavedWorkChoice.Save, SaveSucceeds: false, Expected: UnsavedWorkResult.Cancelled, ExpectedSaveCalls: 1)
        };

        foreach (var testCase in cases)
        {
            var saveCalls = 0;
            var resolver = new UnsavedWorkResolver(_ => testCase.Choice, () =>
            {
                saveCalls++;
                return testCase.SaveSucceeds;
            });

            var result = resolver.Resolve(testCase.Dirty, UnsavedWorkAction.NewProject);

            Assert.AreEqual(testCase.Expected, result);
            Assert.AreEqual(testCase.ExpectedSaveCalls, saveCalls);
        }
    }

    [TestMethod]
    public void UnsavedPromptTextIdentifiesOuterOperation()
    {
        var presenter = new UnsavedWorkPromptPresenter();

        StringAssert.Contains(presenter.ContentFor(UnsavedWorkAction.NewProject).Message, "starting a new one");
        StringAssert.Contains(presenter.ContentFor(UnsavedWorkAction.OpenProject).Message, "opening another one");
        StringAssert.Contains(presenter.ContentFor(UnsavedWorkAction.Exit).Message, "before exiting");
    }

    [STATestMethod]
    public void UnsavedWorkDialogUsesSaveDiscardCancelButtonSemantics()
    {
        using var dialog = new UnsavedWorkDialog(new UnsavedWorkPromptContent("Save?", "Unsaved"));
        var buttons = AllControls(dialog).OfType<Button>().ToDictionary(button => button.Text);

        CollectionAssert.AreEquivalent(new[] { "Save", "Don't Save", "Cancel" }, buttons.Keys.ToArray());
        Assert.AreSame(buttons["Save"], dialog.AcceptButton);
        Assert.AreSame(buttons["Cancel"], dialog.CancelButton);
        Assert.AreEqual(DialogResult.OK, buttons["Save"].DialogResult);
        Assert.AreEqual(DialogResult.No, buttons["Don't Save"].DialogResult);
        Assert.AreEqual(DialogResult.Cancel, buttons["Cancel"].DialogResult);
    }

    [STATestMethod]
    public void NewPackageResetsWorkflowReviewBuildAndProjectPathButRetainsDefaults()
    {
        using var form = new MainForm();
        SetField(form, "settings", new AppSettings
        {
            DefaultOutputDirectory = @"D:\exports",
            CreatorDefaults = new CreatorDefaults { Author = "Creator", Website = "https://example.test", License = "CC0" }
        });
        SetField(form, "projectPath", @"D:\old\old.sfmpack");
        SetField(form, "lastBuildOutputFolder", @"D:\old");
        GetField<Label>(form, "lastBuildStatusLabel").Text = "Last build completed";
        GetField<Label>(form, "lastBuildStatusLabel").Visible = true;
        GetField<Button>(form, "lastBuildOpenFolderButton").Visible = true;
        InvokePrivate(form, "ShowEditor");
        GetField<TabControl>(form, "workflowTabs").SelectedIndex = 4;
        GetField<TreeView>(form, "reviewPackageTree").Nodes.Add("old");
        GetField<ListView>(form, "reviewCheckList").Items.Add("old");

        InvokePrivate(form, "NewPackage");

        var project = GetField<PackageProject>(form, "project");
        Assert.AreEqual(0, GetField<TabControl>(form, "workflowTabs").SelectedIndex);
        Assert.IsNull(GetFieldValue<string?>(form, "projectPath"));
        Assert.AreEqual("Creator", project.Readme.Author);
        Assert.AreEqual(@"D:\exports", GetField<TextBox>(form, "outputFolderText").Text);
        Assert.AreEqual(0, GetField<TreeView>(form, "reviewPackageTree").Nodes.Count);
        Assert.AreEqual(0, GetField<ListView>(form, "reviewCheckList").Items.Count);
        Assert.IsFalse(GetField<Label>(form, "lastBuildStatusLabel").Visible);
        Assert.IsFalse(GetField<Button>(form, "lastBuildOpenFolderButton").Visible);
        Assert.IsFalse(GetFieldValue<bool>(form, "isDirty"));
    }

    [STATestMethod]
    public void CommittingReplacementProjectStartsAtFirstStageAndClearsProjectSpecificState()
    {
        using var form = new MainForm();
        InvokePrivate(form, "ShowEditor");
        GetField<TabControl>(form, "workflowTabs").SelectedIndex = 4;
        GetField<TreeView>(form, "reviewPackageTree").Nodes.Add("old");
        GetField<ListView>(form, "reviewCheckList").Items.Add("old");
        GetField<Label>(form, "lastBuildStatusLabel").Visible = true;

        InvokePrivate(form, "CommitProjectSession", new PackageProject { AssetName = "Opened" }, @"D:\opened.sfmpack", false, null);

        Assert.AreEqual(0, GetField<TabControl>(form, "workflowTabs").SelectedIndex);
        Assert.AreEqual("Opened", GetField<PackageProject>(form, "project").AssetName);
        Assert.AreEqual(@"D:\opened.sfmpack", GetFieldValue<string?>(form, "projectPath"));
        Assert.AreEqual(0, GetField<TreeView>(form, "reviewPackageTree").Nodes.Count);
        Assert.AreEqual(0, GetField<ListView>(form, "reviewCheckList").Items.Count);
        Assert.IsFalse(GetField<Label>(form, "lastBuildStatusLabel").Visible);
        Assert.IsFalse(GetFieldValue<bool>(form, "isDirty"));
    }

    [STATestMethod]
    public void ExistingProjectCommitRevealsEditorOnlyAfterControlsAreHydrated()
    {
        var root = CreateTempRoot();
        try
        {
            var usermod = Path.Combine(root, "game", "usermod");
            var modelPath = Path.Combine(usermod, "models", "characters", "milo", "milo.mdl");
            WriteMdl(modelPath, "body", @"models\characters\milo\");
            WriteFile(Path.Combine(Path.GetDirectoryName(modelPath)!, "milo.vvd"), "vvd");
            var project = new PackageProject
            {
                AssetName = "Hydrated",
                Models =
                {
                    new ModelEntry
                    {
                        Role = ModelRole.Primary,
                        SourceMdlPath = modelPath,
                        SourceStem = "milo",
                        ReleaseStem = "milo_release"
                    }
                }
            };
            using var form = NewFormWithoutConfiguredRoot();
            form.Show();
            Application.DoEvents();
            var editorPanel = GetField<Panel>(form, "editorPanel");
            var capturedPrimary = string.Empty;
            var capturedRelated = string.Empty;
            var capturedVisible = false;
            editorPanel.VisibleChanged += (_, _) =>
            {
                if (!editorPanel.Visible)
                {
                    return;
                }

                capturedVisible = true;
                capturedPrimary = GetField<TextBox>(form, "primaryModelText").Text;
                capturedRelated = GetField<Label>(form, "modelFamilyNamesLabel").Text;
            };

            InvokePrivate(form, "CommitProjectSession", project, Path.Combine(root, "project.sfmpack"), false, null);

            Assert.IsTrue(capturedVisible);
            Assert.AreEqual(modelPath, capturedPrimary);
            Assert.AreEqual("milo_release.vvd", capturedRelated);
            Assert.AreEqual("milo_release.vvd", GetField<Label>(form, "modelFamilyNamesLabel").Text);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [STATestMethod]
    public void NewFromCurrentCreatesUnsavedDirtyCloneWithoutCarryingAssetSpecificState()
    {
        using var form = new MainForm();
        var current = new PackageProject
        {
            AssetName = "Old",
            CurrentVersion = "1.0",
            Credits = "Old credits",
            ArchiveName = "old.zip",
            Readme = new ReadmeConfig
            {
                Mode = ReadmeMode.Custom,
                CustomSource = ReadmeCustomSource.ProjectText,
                Author = "Creator",
                CustomReadmeText = "Old custom text",
                CustomReadmeGeneratedFromFingerprint = "fingerprint"
            }
        };
        current.Models.Add(new ModelEntry { Role = ModelRole.Primary, SourceMdlPath = @"D:\old.mdl" });
        current.MaterialSources.Add(new SourceEntry { Kind = SourceEntryKind.Material, SourcePath = @"D:\materials", IsFolder = true });
        current.BuildHistory.Add(new BuildRecord { Version = "1.0", ArchiveName = "old.zip", BuildTimestamp = DateTimeOffset.Now });
        SetField(form, "project", current);
        SetField(form, "projectPath", @"D:\old.sfmpack");
        InvokePrivate(form, "ShowEditor");
        InvokePrivate(form, "LoadProjectIntoControls");
        GetField<TextBox>(form, "outputFolderText").Text = @"E:\series-output";
        SetField(form, "isDirty", false);

        InvokePrivate(form, "NewPackageFromCurrent");

        var clone = GetField<PackageProject>(form, "project");
        Assert.IsNull(GetFieldValue<string?>(form, "projectPath"));
        Assert.IsTrue(GetFieldValue<bool>(form, "isDirty"));
        Assert.AreEqual("Creator", clone.Readme.Author);
        Assert.AreEqual(1, clone.MaterialSources.Count);
        Assert.AreEqual(0, clone.Models.Count);
        Assert.AreEqual(0, clone.BuildHistory.Count);
        Assert.AreEqual(string.Empty, clone.Credits);
        Assert.AreEqual(string.Empty, clone.Readme.CustomReadmeText);
        Assert.IsNull(clone.Readme.CustomReadmeGeneratedFromFingerprint);
        Assert.AreEqual(@"E:\series-output", GetField<TextBox>(form, "outputFolderText").Text);
    }

    [STATestMethod]
    public void RecentListRendersMissingProjectWithLocateAndRemoveActions()
    {
        var root = Path.Combine(Path.GetTempPath(), "SfmPackageBuilder.WinForms.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            using var form = new MainForm();
            var missingPath = Path.Combine(root, "Missing.sfmpack");
            var settings = new AppSettings
            {
                RecentProjects = new List<RecentProjectEntry>
                {
                    new() { ProjectPath = missingPath, DisplayName = "Missing Package", LastAccessed = DateTimeOffset.Now }
                }
            };
            SetField(form, "settings", settings);
            SetField(form, "recentProjectsService", new RecentProjectsService(new SettingsService(Path.Combine(root, "settings.json"))));

            InvokePrivate(form, "RefreshRecentProjects");

            var recentList = GetField<FlowLayoutPanel>(form, "recentList");
            var controls = AllControls(recentList).ToArray();
            Assert.IsTrue(controls.OfType<Label>().Any(label => label.Text == "Project file not found"));
            Assert.IsTrue(controls.OfType<Button>().Any(button => button.Text == "Locate"));
            Assert.IsTrue(controls.OfType<Button>().Any(button => button.Text == "Remove"));
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
    public void HomeNavigationPreservesDirtyActiveProjectAndReturnRestoresEditor()
    {
        using var form = new MainForm();
        var project = new PackageProject { AssetName = "Active Package" };
        SetField(form, "project", project);
        SetField(form, "projectPath", @"D:\packages\active.sfmpack");
        SetField(form, "isDirty", true);
        SetField(form, "unsavedWorkChoiceProvider", new Func<UnsavedWorkAction, UnsavedWorkChoice>(_ => throw new InvalidOperationException("Pure navigation should not prompt.")));
        InvokePrivate(form, "ShowEditor");
        form.Show();
        Application.DoEvents();
        GetField<TabControl>(form, "workflowTabs").SelectedIndex = 2;

        InvokePrivate(form, "ShowHome");

        Assert.IsTrue(GetField<Panel>(form, "launchPanel").Visible);
        Assert.IsFalse(GetField<Panel>(form, "editorPanel").Visible);
        Assert.AreSame(project, GetField<PackageProject>(form, "project"));
        Assert.AreEqual(@"D:\packages\active.sfmpack", GetFieldValue<string?>(form, "projectPath"));
        Assert.IsTrue(GetFieldValue<bool>(form, "isDirty"));
        Assert.IsTrue(AllControls(form).OfType<Button>().Any(button => button.Text == "Return to Current Package" && button.Visible));

        AllControls(form).OfType<Button>().Single(button => button.Text == "Return to Current Package").PerformClick();

        Assert.IsFalse(GetField<Panel>(form, "launchPanel").Visible);
        Assert.IsTrue(GetField<Panel>(form, "editorPanel").Visible);
        Assert.AreSame(project, GetField<PackageProject>(form, "project"));
        Assert.AreEqual(@"D:\packages\active.sfmpack", GetFieldValue<string?>(form, "projectPath"));
        Assert.AreEqual(2, GetField<TabControl>(form, "workflowTabs").SelectedIndex);
    }

    [STATestMethod]
    public void NewPackageFromHomeUsesExistingUnsavedWorkLifecycleForDirtyActiveProject()
    {
        using var form = new MainForm();
        SetField(form, "project", new PackageProject { AssetName = "Dirty" });
        SetField(form, "isDirty", true);
        InvokePrivate(form, "ShowEditor");
        form.Show();
        Application.DoEvents();
        InvokePrivate(form, "ShowHome");
        var promptCalls = 0;
        SetField(form, "unsavedWorkChoiceProvider", new Func<UnsavedWorkAction, UnsavedWorkChoice>(action =>
        {
            promptCalls++;
            Assert.AreEqual(UnsavedWorkAction.NewProject, action);
            return UnsavedWorkChoice.Cancel;
        }));

        AllControls(form).OfType<Button>().Single(button => button.Text.Replace("&", string.Empty, StringComparison.Ordinal) == "New Package").PerformClick();

        Assert.AreEqual(1, promptCalls);
        Assert.AreEqual("Dirty", GetField<PackageProject>(form, "project").AssetName);
        Assert.IsTrue(GetFieldValue<bool>(form, "isDirty"));
    }

    [STATestMethod]
    public void RecentPackagesRemainVisibleWhenEmptyAndIgnoreLegacyDisabledPreference()
    {
        var root = Path.Combine(Path.GetTempPath(), "SfmPackageBuilder.WinForms.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            using var form = new MainForm();
            SetField(form, "recentProjectsService", new RecentProjectsService(new SettingsService(Path.Combine(root, "settings.json"))));

            SetField(form, "settings", new AppSettings());
            InvokePrivate(form, "RefreshRecentProjects");
            var enabledEmptyText = string.Join("|", AllControls(form).Select(control => control.Text));
            StringAssert.Contains(enabledEmptyText, "Recent Packages");
            StringAssert.Contains(enabledEmptyText, "No recent packages yet.");

            var recentPath = Path.Combine(root, "Recent.sfmpack");
            File.WriteAllText(recentPath, "{}");
            SetField(form, "settings", new AppSettings
            {
                RecentProjects = new List<RecentProjectEntry>
                {
                    new() { ProjectPath = recentPath, DisplayName = "Recent Prop", LastAccessed = DateTimeOffset.Now }
                }
            });
            InvokePrivate(form, "RefreshRecentProjects");
            var enabledWithEntriesText = string.Join("|", AllControls(form).Select(control => control.Text));
            StringAssert.Contains(enabledWithEntriesText, "Recent Packages");
            StringAssert.Contains(enabledWithEntriesText, "Recent Prop");

            SetField(form, "settings", new AppSettings { RememberRecentProjects = false });
            InvokePrivate(form, "RefreshRecentProjects");
            var disabledText = string.Join("|", AllControls(form).Select(control => control.Text));
            StringAssert.Contains(disabledText, "Recent Packages");
            StringAssert.Contains(disabledText, "No recent packages yet.");
            Assert.IsFalse(disabledText.Contains("hidden", StringComparison.OrdinalIgnoreCase));
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
    public void RecentPackagesUseThreeColumnScrollableWorkspaceAndProjectMetadataCards()
    {
        var root = Path.Combine(Path.GetTempPath(), "SfmPackageBuilder.WinForms.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var serializer = new ProjectSerializer();
            var recentProjects = new List<RecentProjectEntry>();
            var knownDate = new DateTime(2026, 8, 21, 12, 0, 0, DateTimeKind.Local);
            for (var i = 0; i < 9; i++)
            {
                var path = Path.Combine(root, $"Project{i}.sfmpack");
                serializer.Save(path, new PackageProject
                {
                    AssetName = i == 0 ? "Mia" : $"Project {i}",
                    CurrentVersion = i == 0 ? "1.0.2" : $"1.0.{i}"
                });
                File.SetLastWriteTime(path, knownDate.AddDays(-i));
                recentProjects.Add(new RecentProjectEntry { ProjectPath = path, DisplayName = $"Fallback {i}", LastAccessed = DateTimeOffset.Now.AddDays(-i) });
            }

            using var form = new MainForm();
            form.Size = new Size(1160, 760);
            SetField(form, "settings", new AppSettings { RecentProjects = recentProjects });
            SetField(form, "recentProjectsService", new RecentProjectsService(new SettingsService(Path.Combine(root, "settings.json")), maxEntries: 20));
            form.Show();
            Application.DoEvents();
            InvokePrivate(form, "RefreshRecentProjects");
            Application.DoEvents();

            var recentList = GetField<FlowLayoutPanel>(form, "recentList");
            var heading = AllControls(form).OfType<Label>().Single(label => label.Text == "Recent Packages");
            var text = string.Join("|", AllControls(GetField<Control>(form, "recentWorkspacePanel")).Select(control => control.Text));

            Assert.AreEqual(3, GetFieldValue<int>(form, "recentGridColumnCount"));
            Assert.IsTrue(GetFieldValue<int>(form, "recentCardWidth") >= 170);
            Assert.AreEqual(new Padding(12), GetField<TableLayoutPanel>(form, "launchOuterLayout").Padding);
            Assert.AreEqual(DockStyle.Fill, GetField<Control>(form, "recentWorkspacePanel").Dock);
            Assert.AreEqual(DockStyle.Fill, recentList.Dock);
            Assert.IsTrue(recentList.AutoScroll);
            Assert.AreEqual(MainForm.RecentWorkspaceBackColor, GetField<Control>(form, "launchRecentPanel").BackColor);
            Assert.AreEqual(MainForm.RecentWorkspaceBackColor, GetField<Control>(form, "recentWorkspacePanel").BackColor);
            Assert.AreEqual(MainForm.RecentWorkspaceBackColor, recentList.BackColor);
            Assert.IsTrue(recentList.Controls.OfType<Panel>().All(card => card.BorderStyle == BorderStyle.None));
            Assert.AreEqual(ContentAlignment.MiddleCenter, heading.TextAlign);
            Assert.IsTrue(AllControls(form).OfType<PictureBox>().Any(box => box.Width == 400 && box.Height == 410));
            StringAssert.Contains(text, "Mia");
            StringAssert.Contains(text, "v1.0.2");
            StringAssert.Contains(text, knownDate.ToString("MMM d", CultureInfo.CurrentCulture));
            Assert.IsFalse(text.Contains("1.0.0", StringComparison.Ordinal));
            Assert.IsTrue(recentList.Controls.Count >= 9);
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
    public void RecentPackageCardsExposeHoverStateWithoutChangingBehavior()
    {
        var root = Path.Combine(Path.GetTempPath(), "SfmPackageBuilder.WinForms.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var projectPath = Path.Combine(root, "Mia.sfmpack");
            new ProjectSerializer().Save(projectPath, new PackageProject { AssetName = "Mia", CurrentVersion = "1.0.2" });
            using var form = new MainForm();
            SetField(form, "settings", new AppSettings
            {
                RecentProjects = new List<RecentProjectEntry>
                {
                    new() { ProjectPath = projectPath, DisplayName = "Mia", LastAccessed = DateTimeOffset.Now }
                }
            });
            SetField(form, "recentProjectsService", new RecentProjectsService(new SettingsService(Path.Combine(root, "settings.json"))));

            InvokePrivate(form, "RefreshRecentProjects");

            var card = GetField<FlowLayoutPanel>(form, "recentList").Controls.OfType<Panel>().Single();
            Assert.AreEqual(Cursors.Hand, card.Cursor);
            Assert.AreEqual(MainForm.RecentCardNormalBackColor, card.BackColor);

            InvokePrivate(form, "ApplyRecentCardHover", card, true);
            Assert.AreEqual(MainForm.RecentCardHoverBackColor, card.BackColor);

            InvokePrivate(form, "ApplyRecentCardHover", card, false);
            Assert.AreEqual(MainForm.RecentCardNormalBackColor, card.BackColor);
            Assert.AreEqual(projectPath, ((RecentProjectEntry)card.Tag!).ProjectPath);
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
    public void RecentCardOmitsUnavailableVersionAndEmptyStateStaysCentered()
    {
        var root = Path.Combine(Path.GetTempPath(), "SfmPackageBuilder.WinForms.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            using var emptyForm = new MainForm();
            emptyForm.Size = new Size(1160, 760);
            SetField(emptyForm, "settings", new AppSettings());
            SetField(emptyForm, "recentProjectsService", new RecentProjectsService(new SettingsService(Path.Combine(root, "empty-settings.json"))));
            emptyForm.Show();
            Application.DoEvents();
            InvokePrivate(emptyForm, "RefreshRecentProjects");
            Application.DoEvents();

            Assert.IsTrue(GetField<Control>(emptyForm, "launchRecentPanel").Visible);
            Assert.IsFalse(GetField<FlowLayoutPanel>(emptyForm, "recentList").Visible);
            var emptyState = GetField<Label>(emptyForm, "recentEmptyStateLabel");
            Assert.IsTrue(emptyState.Visible);
            Assert.AreEqual("No recent packages yet.", emptyState.Text);
            Assert.AreEqual(DockStyle.Fill, emptyState.Dock);
            Assert.AreEqual(ContentAlignment.MiddleCenter, emptyState.TextAlign);

            var legacyPath = Path.Combine(root, "Legacy.sfmpack");
            File.WriteAllText(legacyPath, "{}");
            using var legacyForm = new MainForm();
            SetField(legacyForm, "settings", new AppSettings
            {
                RecentProjects = new List<RecentProjectEntry>
                {
                    new() { ProjectPath = legacyPath, DisplayName = "Legacy Package", LastAccessed = DateTimeOffset.Now }
                }
            });
            SetField(legacyForm, "recentProjectsService", new RecentProjectsService(new SettingsService(Path.Combine(root, "legacy-settings.json"))));

            InvokePrivate(legacyForm, "RefreshRecentProjects");

            var text = string.Join("|", AllControls(GetField<Control>(legacyForm, "recentWorkspacePanel")).Select(control => control.Text));
            StringAssert.Contains(text, "Legacy Package");
            Assert.IsFalse(text.Contains("v1.0.0", StringComparison.Ordinal));
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
    public void PersistedRecentProjectsLoadIntoReconstructedStartScreen()
    {
        var root = Path.Combine(Path.GetTempPath(), "SfmPackageBuilder.WinForms.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var settingsPath = Path.Combine(root, "settings.json");
            var settingsService = new SettingsService(settingsPath);
            var projectPath = Path.Combine(root, "Saved Project.sfmpack");
            File.WriteAllText(projectPath, "project");
            var settings = new AppSettings { RememberRecentProjects = false };
            new RecentProjectsService(settingsService).AddOrPromote(settings, projectPath, "Saved Project");

            using var form = new MainForm();
            SetField(form, "settings", settingsService.Load().Settings);
            SetField(form, "recentProjectsService", new RecentProjectsService(settingsService));
            InvokePrivate(form, "RefreshRecentProjects");

            var text = string.Join("|", AllControls(form).Select(control => control.Text));
            StringAssert.Contains(text, "Recent Packages");
            StringAssert.Contains(text, "Saved Project");
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
    public void UnsavedProjectSaveStartUsesDefaultProjectFolderNotModelBrowseFolder()
    {
        var root = Path.Combine(Path.GetTempPath(), "SfmPackageBuilder.WinForms.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var modelBrowseFolder = Path.Combine(root, "game", "usermod", "models", "AnnoAD", "foxbase", "mia");
            var defaultProjectFolder = Path.Combine(root, "SFM Projects");
            Directory.CreateDirectory(modelBrowseFolder);
            Directory.CreateDirectory(defaultProjectFolder);
            using var form = new MainForm();
            SetField(form, "settings", new AppSettings { DefaultProjectDirectory = defaultProjectFolder });
            SetField(form, "projectPath", null);
            SetField(form, "lastModelBrowseFolder", modelBrowseFolder);

            var start = InvokePrivateString(form, "GetProjectSaveAsStartDirectory");

            Assert.AreEqual(defaultProjectFolder, start);
            Assert.AreNotEqual(modelBrowseFolder, start);
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
    public void SaveAsForExistingProjectStartsInCurrentProjectDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), "SfmPackageBuilder.WinForms.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var currentProjectFolder = Path.Combine(root, "Projects", "Mia");
            var defaultProjectFolder = Path.Combine(root, "Default SFM Projects");
            Directory.CreateDirectory(currentProjectFolder);
            Directory.CreateDirectory(defaultProjectFolder);
            using var form = new MainForm();
            SetField(form, "settings", new AppSettings { DefaultProjectDirectory = defaultProjectFolder });
            SetField(form, "projectPath", Path.Combine(currentProjectFolder, "Mia.sfmpack"));

            Assert.AreEqual(currentProjectFolder, InvokePrivateString(form, "GetProjectSaveAsStartDirectory"));
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
    public void ProjectModelAndMaterialBrowseStateRemainIndependent()
    {
        var root = Path.Combine(Path.GetTempPath(), "SfmPackageBuilder.WinForms.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var usermod = Path.Combine(root, "game", "usermod");
            var modelBrowseFolder = Path.Combine(usermod, "models", "mia");
            var materialModelsFolder = Path.Combine(usermod, "materials", "models");
            var materialBrowseFolder = Path.Combine(usermod, "materials", "models", "mia");
            var defaultProjectFolder = Path.Combine(root, "SFM Projects");
            Directory.CreateDirectory(modelBrowseFolder);
            Directory.CreateDirectory(materialBrowseFolder);
            Directory.CreateDirectory(defaultProjectFolder);
            using var form = new MainForm();
            SetField(form, "settings", new AppSettings
            {
                DefaultSfmContentFolder = usermod,
                DefaultProjectDirectory = defaultProjectFolder
            });
            SetField(form, "lastModelBrowseFolder", modelBrowseFolder);
            SetField(form, "lastMaterialBrowseFolder", materialBrowseFolder);

            Assert.AreEqual(modelBrowseFolder, InvokePrivateString(form, "GetModelBrowseStart", string.Empty));
            Assert.AreEqual(materialBrowseFolder, InvokePrivateString(form, "GetMaterialBrowseStart"));
            Assert.AreEqual(defaultProjectFolder, InvokePrivateString(form, "GetProjectSaveAsStartDirectory"));

            SetField(form, "lastMaterialBrowseFolder", Path.Combine(usermod, "materials"));
            Assert.AreEqual(modelBrowseFolder, InvokePrivateString(form, "GetModelBrowseStart", string.Empty));
            Assert.AreEqual(materialModelsFolder, InvokePrivateString(form, "GetMaterialBrowseStart"));
            Assert.AreEqual(defaultProjectFolder, InvokePrivateString(form, "GetProjectSaveAsStartDirectory"));
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
    public void RenamePreviewIsConditionalCompactAndMatchesPackagePlanForPrimaryModel()
    {
        var root = CreateTempRoot();
        try
        {
            var usermod = Path.Combine(root, "game", "usermod");
            var modelPath = Path.Combine(usermod, "models", "characters", "mia", "mia.mdl");
            var vvdPath = Path.Combine(usermod, "models", "characters", "mia", "mia.vvd");
            var dx90Path = Path.Combine(usermod, "models", "characters", "mia", "mia.dx90.vtx");
            WriteMdl(modelPath, "body", @"models\characters\mia\");
            WriteFile(vvdPath, "vvd");
            WriteFile(dx90Path, "dx90");

            using var form = NewFormWithoutConfiguredRoot();
            SetField(form, "settings", new AppSettings { DefaultSfmContentFolder = usermod });
            InvokePrivate(form, "ShowEditor");
            form.Show();
            Application.DoEvents();
            InvokePrivate(form, "SelectPrimaryModelForPackage", modelPath);
            GetField<TextBox>(form, "releaseStemText").Text = "mia";
            InvokePrivate(form, "UpdateRenameIllustration");

            Assert.AreEqual(string.Empty, GetField<Label>(form, "renamePreviewLabel").Text);

            GetField<TextBox>(form, "releaseStemText").Text = "flashlight";
            InvokePrivate(form, "UpdateRenameIllustration");

            var preview = GetField<Label>(form, "renamePreviewLabel");
            var projectBeforeAssert = GetField<PackageProject>(form, "project");
            var planBeforeAssert = new BuildCoordinator(Path.Combine(root, "appdata")).PreviewPackage(projectBeforeAssert);
            Assert.IsTrue(
                preview.Visible && preview.Text.Length > 0,
                "Preview hidden. ReleaseStem="
                + projectBeforeAssert.Models.Single(model => model.Role == ModelRole.Primary).ReleaseStem
                + "; entries="
                + string.Join(", ", planBeforeAssert.Entries
                    .Where(entry => entry.EntryType is PackagePlanEntryType.Model or PackagePlanEntryType.ModelCompanion)
                    .Select(entry => $"{entry.ModelEntryId}:{Path.GetFileName(entry.SourcePath ?? string.Empty)}->{Path.GetFileName(entry.DestinationRelativePath ?? string.Empty)}:{entry.Status}")));
            Assert.AreEqual("Preview: mia.mdl → flashlight.mdl" + Environment.NewLine + "Your original files stay unchanged.", preview.Text);
            var table = (TableLayoutPanel)preview.Parent!;
            Assert.AreEqual(4, table.GetRow(preview));
            var sourceHelper = AllControls(form).OfType<Label>().Single(label => label.Text == "Choose the .mdl file you want to package.");
            var relatedHeading = GetField<Label>(form, "modelFamilySummaryLabel");
            var relatedNames = GetField<Label>(form, "modelFamilyNamesLabel");
            Assert.AreSame(table, sourceHelper.Parent);
            Assert.AreSame(table, relatedHeading.Parent);
            Assert.AreSame(table, relatedNames.Parent);
            Assert.AreEqual(table.GetColumn(sourceHelper), table.GetColumn(preview));
            Assert.AreEqual(table.GetColumn(sourceHelper), table.GetColumn(relatedHeading));
            Assert.AreEqual(table.GetColumn(sourceHelper), table.GetColumn(relatedNames));
            Assert.AreEqual(table.GetColumnSpan(relatedHeading), table.GetColumnSpan(relatedNames));
            Assert.IsTrue(relatedHeading.Margin.Top >= 8);
            Assert.IsFalse(AllControls(form).OfType<GroupBox>().Any(group => group.Text == "Files renamed in ZIP"));
            Assert.IsFalse(AllControls(form).OfType<ListView>().Any(list =>
                list.Columns.Cast<ColumnHeader>().Select(column => column.Text).SequenceEqual(new[] { "Original", "ZIP" })));

            var project = GetField<PackageProject>(form, "project");
            var primaryId = project.Models.Single(model => model.Role == ModelRole.Primary).Id;
            var plan = new BuildCoordinator(Path.Combine(root, "appdata")).PreviewPackage(project);
            var plannedDestination = plan.Entries
                .Where(entry => entry.ModelEntryId == primaryId)
                .Single(entry => entry.EntryType is PackagePlanEntryType.Model)
                .DestinationRelativePath!;

            StringAssert.Contains(preview.Text, Path.GetFileName(plannedDestination));
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [STATestMethod]
    public void OrdinaryModelMaterialsStageWithSimpleRenameUsesPageScrollAndShowsNormalListsWithoutInternalCaps()
    {
        var root = CreateTempRoot();
        try
        {
            var usermod = Path.Combine(root, "game", "usermod");
            var modelPath = Path.Combine(usermod, "models", "characters", "mia", "mia.mdl");
            WriteMdl(modelPath, "body", @"models\characters\mia\");
            WriteFile(Path.Combine(Path.GetDirectoryName(modelPath)!, "mia.vvd"), "vvd");
            WriteFile(Path.Combine(Path.GetDirectoryName(modelPath)!, "mia.dx90.vtx"), "dx90");
            WriteFile(Path.Combine(usermod, "materials", "models", "characters", "mia", "body.vmt"), "vmt");

            using var form = NewFormWithoutConfiguredRoot();
            form.Size = new Size(1160, 813);
            SetField(form, "settings", new AppSettings { DefaultSfmContentFolder = usermod });
            InvokePrivate(form, "ShowEditor");
            form.Show();
            Application.DoEvents();
            var tabs = GetField<TabControl>(form, "workflowTabs");
            tabs.SelectedIndex = 0;
            InvokePrivate(form, "SelectPrimaryModelForPackage", modelPath);
            GetField<TextBox>(form, "releaseStemText").Text = "flashlight";
            InvokePrivate(form, "UpdateRenameIllustration");
            Application.DoEvents();

            Assert.AreEqual("Preview: mia.mdl → flashlight.mdl" + Environment.NewLine + "Your original files stay unchanged.", GetField<Label>(form, "renamePreviewLabel").Text);
            Assert.IsTrue(GetField<Label>(form, "renamePreviewLabel").Visible);
            Assert.IsTrue(tabs.TabPages[0].Controls.OfType<Panel>().Single().AutoScroll);
            Assert.AreEqual("Related model files included (2)", GetField<Label>(form, "modelFamilySummaryLabel").Text);
            Assert.AreEqual("flashlight.vvd, flashlight.dx90.vtx", GetField<Label>(form, "modelFamilyNamesLabel").Text);
            Assert.IsTrue(GetField<Label>(form, "modelFamilySummaryLabel").Visible);
            Assert.IsTrue(GetField<Label>(form, "modelFamilyNamesLabel").Visible);
            Assert.AreEqual(
                2,
                GetField<Label>(form, "renamePreviewLabel").Text.Split(new[] { Environment.NewLine }, StringSplitOptions.None).Length);
            Assert.IsTrue(GetField<Control>(form, "workflowNavigationPanel").Visible);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [STATestMethod]
    public void RemovingPreviousVersionTargetsSelectionAndDoesNotTouchBuildHistory()
    {
        using var form = new MainForm();
        var project = new PackageProject();
        project.ReleaseHistory.Add(new ReleaseRecord { Version = "1.0", Changes = new List<string> { "First." } });
        project.ReleaseHistory.Add(new ReleaseRecord { Version = "1.1", Changes = new List<string> { "Second." } });
        project.BuildHistory.Add(new BuildRecord { Version = "1.1", ArchiveName = "asset.zip", BuildTimestamp = DateTimeOffset.Now });
        SetField(form, "project", project);
        SetField(form, "confirmReleaseHistoryRemoval", new Func<ReleaseRecord, bool>(record =>
        {
            Assert.AreEqual("1.0", record.Version);
            return true;
        }));
        InvokePrivate(form, "ShowEditor");
        InvokePrivate(form, "LoadProjectIntoControls");
        form.Show();
        GetField<TabControl>(form, "workflowTabs").SelectedIndex = 1;
        Application.DoEvents();
        var list = GetField<ListView>(form, "releaseHistoryList");
        list.Items[0].Selected = true;
        list.Items[0].Focused = true;
        list.Select();

        InvokePrivate(form, "RemoveSelectedReleaseHistory");

        CollectionAssert.AreEqual(new[] { "1.1" }, project.ReleaseHistory.Select(record => record.Version).ToArray());
        Assert.AreEqual(1, project.BuildHistory.Count);
        Assert.AreEqual("asset.zip", project.BuildHistory[0].ArchiveName);
    }

    [STATestMethod]
    public void AddThisVersionToChangelogConsumesVisibleFieldsWithoutDialogDuplicatesOrBuildHistory()
    {
        using var form = new MainForm();
        var project = new PackageProject();
        project.BuildHistory.Add(new BuildRecord { Version = "1.0.1", ArchiveName = "mia.zip", BuildTimestamp = DateTimeOffset.Now });
        SetField(form, "project", project);
        InvokePrivate(form, "ShowEditor");
        InvokePrivate(form, "LoadProjectIntoControls");
        GetField<TextBox>(form, "versionText").Text = "1.0.2";
        GetField<TextBox>(form, "changesText").Text = "Fixed materials.";
        SetField(form, "isDirty", false);

        InvokePrivate(form, "AddCurrentVersionToChangelog");
        InvokePrivate(form, "AddCurrentVersionToChangelog");

        Assert.AreEqual("1.0.2", project.CurrentVersion);
        CollectionAssert.AreEqual(new[] { "Fixed materials." }, project.ChangesThisVersion);
        Assert.AreEqual(1, project.ReleaseHistory.Count);
        Assert.AreEqual("1.0.2", project.ReleaseHistory[0].Version);
        CollectionAssert.AreEqual(new[] { "Fixed materials." }, project.ReleaseHistory[0].Changes);
        Assert.AreEqual("1.0.2", GetField<TextBox>(form, "versionText").Text);
        Assert.AreEqual("Fixed materials.", GetField<TextBox>(form, "changesText").Text);
        Assert.AreEqual(1, project.BuildHistory.Count);
        Assert.AreEqual("mia.zip", project.BuildHistory[0].ArchiveName);
        Assert.IsTrue(GetFieldValue<bool>(form, "isDirty"));
    }

    [STATestMethod]
    public void BlankAddThisVersionDoesNotCreateHistoricalRelease()
    {
        using var form = new MainForm();
        var project = new PackageProject();
        SetField(form, "project", project);
        InvokePrivate(form, "ShowEditor");
        InvokePrivate(form, "LoadProjectIntoControls");

        InvokePrivate(form, "AddCurrentVersionToChangelog");

        Assert.AreEqual(0, project.ReleaseHistory.Count);
        Assert.AreEqual(0, project.BuildHistory.Count);
    }

    [STATestMethod]
    public void AddThisVersionToChangelogShowsNewestEntryFirstByInsertionOrder()
    {
        using var form = new MainForm();
        var project = new PackageProject();
        SetField(form, "project", project);
        InvokePrivate(form, "ShowEditor");
        InvokePrivate(form, "LoadProjectIntoControls");

        foreach (var version in new[] { "release-a", "release-b", "release-c" })
        {
            GetField<TextBox>(form, "versionText").Text = version;
            GetField<TextBox>(form, "changesText").Text = "Changes for " + version;
            InvokePrivate(form, "AddCurrentVersionToChangelog");
        }

        CollectionAssert.AreEqual(
            new[] { "release-c", "release-b", "release-a" },
            project.ReleaseHistory.Select(record => record.Version).ToArray());
        CollectionAssert.AreEqual(
            new[] { "release-c", "release-b", "release-a" },
            GetField<ListView>(form, "releaseHistoryList").Items.Cast<ListViewItem>().Select(item => item.Text).ToArray());
    }

    [STATestMethod]
    public void ApplyingPreferencesOnlyDoesNotDirtyUntouchedPackageButPreservesExistingDirtyState()
    {
        using var cleanForm = new MainForm();
        SetField(cleanForm, "settings", new AppSettings { DefaultOutputDirectory = @"D:\exports" });
        InvokePrivate(cleanForm, "ShowEditor");
        InvokePrivate(cleanForm, "LoadProjectIntoControls");
        SetField(cleanForm, "isDirty", false);
        var cleanPromptCalls = 0;
        SetField(cleanForm, "unsavedWorkChoiceProvider", new Func<UnsavedWorkAction, UnsavedWorkChoice>(_ =>
        {
            cleanPromptCalls++;
            return UnsavedWorkChoice.Cancel;
        }));

        InvokePrivate(cleanForm, "ApplyMachineSettingsToUi");
        InvokePrivate(cleanForm, "NewPackage");

        Assert.AreEqual(0, cleanPromptCalls);
        Assert.IsFalse(GetFieldValue<bool>(cleanForm, "isDirty"));

        using var dirtyForm = new MainForm();
        SetField(dirtyForm, "settings", new AppSettings { DefaultOutputDirectory = @"E:\exports" });
        SetField(dirtyForm, "project", new PackageProject { AssetName = "Dirty package" });
        InvokePrivate(dirtyForm, "ShowEditor");
        InvokePrivate(dirtyForm, "LoadProjectIntoControls");
        SetField(dirtyForm, "isDirty", true);
        var dirtyPromptCalls = 0;
        SetField(dirtyForm, "unsavedWorkChoiceProvider", new Func<UnsavedWorkAction, UnsavedWorkChoice>(action =>
        {
            dirtyPromptCalls++;
            Assert.AreEqual(UnsavedWorkAction.NewProject, action);
            return UnsavedWorkChoice.Cancel;
        }));

        InvokePrivate(dirtyForm, "ApplyMachineSettingsToUi");
        InvokePrivate(dirtyForm, "NewPackage");

        Assert.AreEqual(1, dirtyPromptCalls);
        Assert.IsTrue(GetFieldValue<bool>(dirtyForm, "isDirty"));
        Assert.AreEqual("Dirty package", GetField<PackageProject>(dirtyForm, "project").AssetName);
    }

    [STATestMethod]
    public void EditSelectedPreviousVersionDialogRemainsAvailable()
    {
        using var dialog = new ChangelogEntryDialog("Edit Previous Version", "1.0", new[] { "Fixed materials." });
        var text = string.Join("|", AllControls(dialog).Select(control => control.Text));

        Assert.AreEqual("Edit Previous Version", dialog.Text);
        StringAssert.Contains(text, "Version");
        StringAssert.Contains(text, "Changes");
        StringAssert.Contains(text, "One change per line.");
        Assert.AreEqual("1.0", dialog.Version);
        CollectionAssert.AreEqual(new[] { "Fixed materials." }, dialog.Changes.ToArray());
        Assert.IsNotNull(dialog.AcceptButton);
        Assert.IsNotNull(dialog.CancelButton);
    }

    [STATestMethod]
    public void CancellingPreviousVersionRemovalLeavesSelectionIntact()
    {
        using var form = new MainForm();
        var project = new PackageProject();
        project.ReleaseHistory.Add(new ReleaseRecord { Version = "1.0", Changes = new List<string> { "First." } });
        SetField(form, "project", project);
        SetField(form, "confirmReleaseHistoryRemoval", new Func<ReleaseRecord, bool>(_ => false));
        InvokePrivate(form, "ShowEditor");
        InvokePrivate(form, "LoadProjectIntoControls");
        form.Show();
        GetField<TabControl>(form, "workflowTabs").SelectedIndex = 1;
        Application.DoEvents();
        var list = GetField<ListView>(form, "releaseHistoryList");
        list.Items[0].Selected = true;
        list.Items[0].Focused = true;
        list.Select();

        InvokePrivate(form, "RemoveSelectedReleaseHistory");

        Assert.AreEqual(1, project.ReleaseHistory.Count);
        Assert.AreEqual("1.0", project.ReleaseHistory[0].Version);
    }

    [TestMethod]
    public void BuildHistoryPersistencePolicyPreservesDirtyStateRules()
    {
        var cases = new[]
        {
            (BuildSucceeded: false, WasDirty: false, ProjectPath: @"D:\project.sfmpack", Expected: BuildHistoryPersistenceStatus.NotApplicable, ExpectedDirty: false, ExpectedPersistCalls: 0),
            (BuildSucceeded: true, WasDirty: true, ProjectPath: @"D:\project.sfmpack", Expected: BuildHistoryPersistenceStatus.DeferredBecauseProjectDirty, ExpectedDirty: true, ExpectedPersistCalls: 0),
            (BuildSucceeded: true, WasDirty: false, ProjectPath: (string?)null, Expected: BuildHistoryPersistenceStatus.DeferredBecauseProjectHasNoPath, ExpectedDirty: true, ExpectedPersistCalls: 0),
            (BuildSucceeded: true, WasDirty: false, ProjectPath: @"D:\project.sfmpack", Expected: BuildHistoryPersistenceStatus.PersistedClean, ExpectedDirty: false, ExpectedPersistCalls: 1)
        };

        foreach (var testCase in cases)
        {
            var persistCalls = 0;
            var result = new BuildHistoryPersistencePolicy().Apply(testCase.BuildSucceeded, testCase.WasDirty, testCase.ProjectPath, () => persistCalls++);

            Assert.AreEqual(testCase.Expected, result.Status);
            Assert.AreEqual(testCase.ExpectedDirty, result.ShouldBeDirty);
            Assert.AreEqual(testCase.ExpectedPersistCalls, persistCalls);
        }
    }

    [TestMethod]
    public void BuildHistoryPersistenceFailureKeepsBuildEventDirtyForLaterSave()
    {
        var result = new BuildHistoryPersistencePolicy().Apply(
            buildSucceeded: true,
            wasDirtyBeforeBuild: false,
            projectPath: @"D:\project.sfmpack",
            persistProject: () => throw new IOException("disk full"));

        Assert.AreEqual(BuildHistoryPersistenceStatus.Failed, result.Status);
        Assert.IsTrue(result.ShouldBeDirty);
        StringAssert.Contains(result.Message!, "disk full");
    }

    [STATestMethod]
    public void SelectingPrimaryModelAutomaticallyAddsMatchingMaterialFolder()
    {
        var root = CreateTempRoot();
        try
        {
            var usermod = Path.Combine(root, "game", "usermod");
            var modelPath = Path.Combine(usermod, "models", "characters", "milo", "milo.mdl");
            var materialFolder = Path.Combine(usermod, "materials", "models", "characters", "milo");
            WriteMdl(modelPath, "body", @"models\characters\milo\");
            WriteFile(Path.Combine(materialFolder, "body.vmt"), "vmt");

            using var form = NewFormWithoutConfiguredRoot();
            InvokePrivate(form, "ShowEditor");

            InvokePrivate(form, "SelectPrimaryModelForPackage", modelPath);

            var project = GetField<PackageProject>(form, "project");
            CollectionAssert.AreEqual(new[] { materialFolder }, project.MaterialSources.Select(source => source.SourcePath).ToArray());
            Assert.IsTrue(project.MaterialSources[0].IsFolder);
            Assert.IsNotNull(project.MaterialSources[0].SourceReference);
            Assert.AreEqual(materialFolder, project.MaterialSources[0].SourceReference!.AbsolutePath);
            Assert.IsTrue(GetFieldValue<bool>(form, "isDirty"));
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [STATestMethod]
    public void ReleaseNameTypingUsesCachedSourceFactsWithoutRediscoveringCompanionsOrMaterials()
    {
        var root = CreateTempRoot();
        try
        {
            var usermod = Path.Combine(root, "game", "usermod");
            var modelPath = Path.Combine(usermod, "models", "characters", "milo", "milo.mdl");
            var materialFolder = Path.Combine(usermod, "materials", "models", "characters", "milo");
            WriteMdl(modelPath, "body", @"models\characters\milo\");
            WriteFile(Path.Combine(Path.GetDirectoryName(modelPath)!, "milo.vvd"), "vvd");
            using var form = NewFormWithoutConfiguredRoot();
            SetField(form, "settings", new AppSettings { DefaultSfmContentFolder = usermod });
            InvokePrivate(form, "ShowEditor");
            form.Show();
            Application.DoEvents();
            InvokePrivate(form, "SelectPrimaryModelForPackage", modelPath);
            var project = GetField<PackageProject>(form, "project");
            Assert.AreEqual(0, project.MaterialSources.Count);
            AssertPrimaryRelatedVisible(form, "Related model files included (1)", "milo.vvd");

            WriteFile(Path.Combine(Path.GetDirectoryName(modelPath)!, "milo.dx90.vtx"), "dx90");
            WriteFile(Path.Combine(materialFolder, "body.vmt"), "vmt");
            var releaseName = GetField<TextBox>(form, "releaseStemText");
            releaseName.Focus();
            releaseName.SelectionStart = releaseName.TextLength;
            releaseName.SelectedText = "_release";
            Application.DoEvents();
            InvokePrivate(form, "FlushPendingModelNamePresentationForTests");

            Assert.IsTrue(releaseName.Focused);
            Assert.AreEqual("milo_release", releaseName.Text);
            Assert.AreEqual(releaseName.TextLength, releaseName.SelectionStart);
            Assert.AreEqual(0, project.MaterialSources.Count);
            AssertPrimaryRelatedVisible(form, "Related model files included (1)", "milo_release.vvd");
            Assert.AreEqual(
                "Preview: milo.mdl → milo_release.mdl" + Environment.NewLine + "Your original files stay unchanged.",
                GetField<Label>(form, "renamePreviewLabel").Text);

            var plan = new BuildCoordinator(Path.Combine(root, "appdata")).PreviewPackage(project);
            Assert.IsTrue(plan.Entries.Any(entry => entry.DestinationRelativePath == @"models\characters\milo\milo_release.mdl"));
            Assert.IsTrue(plan.Entries.Any(entry => entry.DestinationRelativePath == @"models\characters\milo\milo_release.dx90.vtx"));
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [STATestMethod]
    public void AdditionalModelDiscoveryAddsNewMaterialsWithoutLosingPrimaryMaterials()
    {
        var root = CreateTempRoot();
        try
        {
            var usermod = Path.Combine(root, "game", "usermod");
            var primaryModel = Path.Combine(usermod, "models", "characters", "milo", "milo.mdl");
            var additionalModel = Path.Combine(usermod, "models", "props", "crate", "crate.mdl");
            var primaryMaterials = Path.Combine(usermod, "materials", "models", "characters", "milo");
            var additionalMaterials = Path.Combine(usermod, "materials", "models", "props", "crate");
            WriteMdl(primaryModel, "body", @"models\characters\milo\");
            WriteMdl(additionalModel, "wood", @"models\props\crate\");
            WriteFile(Path.Combine(primaryMaterials, "body.vmt"), "vmt");
            WriteFile(Path.Combine(additionalMaterials, "wood.vmt"), "vmt");

            using var form = NewFormWithoutConfiguredRoot();
            form.Size = new Size(1160, 813);
            InvokePrivate(form, "ShowEditor");
            form.Show();
            GetField<TabControl>(form, "workflowTabs").SelectedIndex = 0;
            Application.DoEvents();

            InvokePrivate(form, "SelectPrimaryModelForPackage", primaryModel);
            InvokePrivate(form, "AddAdditionalModelsForPackage", (object)new[] { additionalModel });
            Application.DoEvents();

            var project = GetField<PackageProject>(form, "project");
            CollectionAssert.AreEquivalent(
                new[] { primaryMaterials, additionalMaterials },
                project.MaterialSources.Select(source => source.SourcePath).ToArray());
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [STATestMethod]
    public void PrimaryRelatedFilesStayHiddenForNoSourceAndZeroCompanions()
    {
        var root = CreateTempRoot();
        try
        {
            var usermod = Path.Combine(root, "game", "usermod");
            var modelPath = Path.Combine(usermod, "models", "characters", "solo", "solo.mdl");
            WriteMdl(modelPath, "body", @"models\characters\solo\");

            using var form = NewFormWithoutConfiguredRoot();
            SetField(form, "settings", new AppSettings { DefaultSfmContentFolder = usermod });
            InvokePrivate(form, "ShowEditor");
            form.Show();
            GetField<TabControl>(form, "workflowTabs").SelectedIndex = 0;
            Application.DoEvents();

            AssertPrimaryRelatedHidden(form);
            AssertPrimaryModelNameDisabled(form);

            InvokePrivate(form, "SelectPrimaryModelForPackage", modelPath);
            Application.DoEvents();

            AssertPrimaryRelatedHidden(form);
            AssertPrimaryModelNameEnabled(form, "solo");

            var modelName = GetField<TextBox>(form, "releaseStemText");
            modelName.Text = "solo_renamed";
            InvokePrivate(form, "UpdateRenameIllustration");
            Assert.AreEqual("Preview: solo.mdl → solo_renamed.mdl" + Environment.NewLine + "Your original files stay unchanged.", GetField<Label>(form, "renamePreviewLabel").Text);
            Assert.IsTrue(GetField<Label>(form, "renamePreviewLabel").Visible);

            modelName.Text = "solo";
            InvokePrivate(form, "UpdateRenameIllustration");
            Assert.AreEqual(string.Empty, GetField<Label>(form, "renamePreviewLabel").Text);
            Assert.IsFalse(GetField<Label>(form, "renamePreviewLabel").Visible);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [STATestMethod]
    public void PrimaryRelatedFilesVisibilityFollowsCurrentSourceReplacementSequence()
    {
        var root = CreateTempRoot();
        try
        {
            var usermod = Path.Combine(root, "game", "usermod");
            var modelA = Path.Combine(usermod, "models", "characters", "a", "a.mdl");
            var modelB = Path.Combine(usermod, "models", "characters", "b", "b.mdl");
            var modelC = Path.Combine(usermod, "models", "characters", "c", "c.mdl");
            WriteMdl(modelA, "body", @"models\characters\a\");
            WriteFile(Path.Combine(Path.GetDirectoryName(modelA)!, "a.vvd"), "vvd");
            WriteFile(Path.Combine(Path.GetDirectoryName(modelA)!, "a.dx90.vtx"), "dx90");
            WriteMdl(modelB, "body", @"models\characters\b\");
            WriteMdl(modelC, "body", @"models\characters\c\");
            WriteFile(Path.Combine(Path.GetDirectoryName(modelC)!, "c.phy"), "phy");

            using var form = NewFormWithoutConfiguredRoot();
            SetField(form, "settings", new AppSettings { DefaultSfmContentFolder = usermod });
            InvokePrivate(form, "ShowEditor");
            form.Show();
            GetField<TabControl>(form, "workflowTabs").SelectedIndex = 0;
            Application.DoEvents();

            InvokePrivate(form, "SelectPrimaryModelForPackage", modelA);
            Application.DoEvents();
            AssertPrimaryRelatedVisible(form, "Related model files included (2)", "a.vvd, a.dx90.vtx");
            AssertPrimaryModelNameEnabled(form, "a");
            GetField<TextBox>(form, "releaseStemText").Text = "a_release";
            InvokePrivate(form, "UpdateRenameIllustration");
            Assert.IsTrue(GetField<Label>(form, "renamePreviewLabel").Visible);
            StringAssert.Contains(GetField<Label>(form, "renamePreviewLabel").Text, "a.mdl");

            InvokePrivate(form, "SelectPrimaryModelForPackage", modelB);
            Application.DoEvents();
            AssertPrimaryRelatedHidden(form);
            AssertPrimaryModelNameEnabled(form, "b");
            Assert.AreEqual(string.Empty, GetField<Label>(form, "renamePreviewLabel").Text);
            Assert.IsFalse(GetField<Label>(form, "renamePreviewLabel").Visible);
            Assert.IsFalse(VisibleModelFamilyText(form).Contains("a.vvd", StringComparison.OrdinalIgnoreCase));

            InvokePrivate(form, "SelectPrimaryModelForPackage", modelC);
            Application.DoEvents();
            AssertPrimaryRelatedVisible(form, "Related model files included (1)", "c.phy");
            AssertPrimaryModelNameEnabled(form, "c");
            Assert.IsFalse(VisibleModelFamilyText(form).Contains("a.vvd", StringComparison.OrdinalIgnoreCase));

            GetField<TextBox>(form, "primaryModelText").Text = string.Empty;
            Application.DoEvents();
            AssertPrimaryRelatedHidden(form);
            AssertPrimaryModelNameDisabled(form);
            Assert.IsFalse(VisibleModelFamilyText(form).Contains("c.phy", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [STATestMethod]
    public void PrimaryModelNameBlankEditingCommitsBackToSourceStemAndReviewStaysConsistent()
    {
        var root = CreateTempRoot();
        try
        {
            var usermod = Path.Combine(root, "game", "usermod");
            var output = Path.Combine(root, "out");
            Directory.CreateDirectory(output);
            var modelPath = Path.Combine(usermod, "models", "props", "chair", "chair.mdl");
            WriteMdl(modelPath, "body", @"models\props\chair\");

            using var form = NewFormWithoutConfiguredRoot();
            SetField(form, "settings", new AppSettings { DefaultSfmContentFolder = usermod, DefaultOutputDirectory = output });
            InvokePrivate(form, "ShowEditor");
            form.Show();
            GetField<TabControl>(form, "workflowTabs").SelectedIndex = 0;
            Application.DoEvents();

            InvokePrivate(form, "SelectPrimaryModelForPackage", modelPath);
            var modelName = GetField<TextBox>(form, "releaseStemText");
            Assert.AreEqual("chair", modelName.Text);

            modelName.Text = string.Empty;
            Application.DoEvents();
            Assert.AreEqual(string.Empty, modelName.Text);
            Assert.IsFalse(GetField<Label>(form, "releaseStemValidationLabel").Visible);

            InvokePrivate(form, "CommitReleaseStemText");
            Assert.AreEqual("chair", modelName.Text);

            GetField<TabControl>(form, "workflowTabs").SelectedIndex = 4;
            InvokePrivate(form, "RefreshReviewAndBuild");
            var list = GetField<ListView>(form, "reviewCheckList");
            Assert.IsFalse(list.Items.Cast<ListViewItem>().Any(item => item.SubItems[1].Text.Contains("Model name", StringComparison.OrdinalIgnoreCase)));

            var project = GetField<PackageProject>(form, "project");
            var plan = new BuildCoordinator(Path.Combine(root, "appdata")).PreviewPackage(project);
            Assert.IsTrue(plan.Entries.Any(entry => entry.EntryType == PackagePlanEntryType.Model
                && string.Equals(entry.DestinationRelativePath, @"models\props\chair\chair.mdl", StringComparison.OrdinalIgnoreCase)));
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [STATestMethod]
    public void InvalidPrimaryModelNameShowsInlineReasonAndUsefulReviewBackstop()
    {
        var root = CreateTempRoot();
        try
        {
            var usermod = Path.Combine(root, "game", "usermod");
            var output = Path.Combine(root, "out");
            Directory.CreateDirectory(output);
            var modelPath = Path.Combine(usermod, "models", "props", "chair", "chair.mdl");
            WriteMdl(modelPath, "body", @"models\props\chair\");

            using var form = NewFormWithoutConfiguredRoot();
            SetField(form, "settings", new AppSettings { DefaultSfmContentFolder = usermod, DefaultOutputDirectory = output });
            InvokePrivate(form, "ShowEditor");
            form.Show();
            GetField<TabControl>(form, "workflowTabs").SelectedIndex = 0;
            Application.DoEvents();

            InvokePrivate(form, "SelectPrimaryModelForPackage", modelPath);
            GetField<TextBox>(form, "releaseStemText").Text = "chair:model";
            Application.DoEvents();

            var inline = GetField<Label>(form, "releaseStemValidationLabel");
            Assert.IsTrue(inline.Visible);
            Assert.AreEqual("Model name cannot contain \":\".", inline.Text);

            GetField<TabControl>(form, "workflowTabs").SelectedIndex = 4;
            InvokePrivate(form, "RefreshReviewAndBuild");
            var list = GetField<ListView>(form, "reviewCheckList");
            var row = list.Items.Cast<ListViewItem>().Single(item => item.SubItems[1].Text == "Model name in ZIP contains a character Windows does not allow.");

            Assert.AreEqual("Error", row.Text);
            var detail = (string)row.Tag!;
            StringAssert.Contains(detail, "Name: chair:model" + Environment.NewLine + "Remove \":\" from the model name.");
            StringAssert.Contains(detail, "Affected values:" + Environment.NewLine + "chair:model");
            Assert.IsFalse(detail.Contains("Source path:", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(detail.Contains(modelPath, StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(GetField<Button>(form, "buildZipButton").Enabled);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [STATestMethod]
    public void PrimaryModelReplacementReplacesPrimaryCompanionsAndAutomaticMaterialsOnly()
    {
        var root = CreateTempRoot();
        try
        {
            var usermod = Path.Combine(root, "game", "usermod");
            var modelA = Path.Combine(usermod, "models", "characters", "a", "a.mdl");
            var modelB = Path.Combine(usermod, "models", "characters", "b", "b.mdl");
            var modelC = Path.Combine(usermod, "models", "characters", "c", "c.mdl");
            var additionalModel = Path.Combine(usermod, "models", "props", "crate", "crate.mdl");
            var aMaterials = Path.Combine(usermod, "materials", "models", "characters", "a");
            var bMaterials = Path.Combine(usermod, "materials", "models", "characters", "b");
            var cMaterials = Path.Combine(usermod, "materials", "models", "characters", "c");
            var additionalMaterials = Path.Combine(usermod, "materials", "models", "props", "crate");
            var manualMaterials = Path.Combine(usermod, "materials", "manual", "chosen");

            WriteModelFamily(modelA, "a", @"models\characters\a\", aMaterials, "body");
            WriteModelFamily(modelB, "b", @"models\characters\b\", bMaterials, "body");
            WriteModelFamily(modelC, "c", @"models\characters\c\", cMaterials, "body");
            WriteModelFamily(additionalModel, "crate", @"models\props\crate\", additionalMaterials, "wood");
            WriteFile(Path.Combine(manualMaterials, "manual.vmt"), "manual");

            using var form = NewFormWithoutConfiguredRoot();
            form.Size = new Size(1160, 813);
            InvokePrivate(form, "ShowEditor");
            form.Show();
            GetField<TabControl>(form, "workflowTabs").SelectedIndex = 0;

            InvokePrivate(form, "SelectPrimaryModelForPackage", modelA);
            InvokePrivate(form, "AddAdditionalModelsForPackage", (object)new[] { additionalModel });
            GetField<PackageProject>(form, "project").MaterialSources.Add(new SourceEntry
            {
                Kind = SourceEntryKind.Material,
                SourcePath = manualMaterials,
                IsFolder = true,
                IncludeRecursively = true
            });

            InvokePrivate(form, "SelectPrimaryModelForPackage", modelB);
            InvokePrivate(form, "SelectPrimaryModelForPackage", modelC);
            Application.DoEvents();

            var project = GetField<PackageProject>(form, "project");
            var primary = project.Models.Single(model => model.Role == ModelRole.Primary);
            var additional = project.Models.Single(model => model.Role == ModelRole.Additional);
            Assert.AreEqual(modelC, primary.SourceMdlPath);
            Assert.AreEqual(additionalModel, additional.SourceMdlPath);
            CollectionAssert.AreEquivalent(
                new[] { cMaterials, additionalMaterials, manualMaterials },
                project.MaterialSources.Select(source => source.SourcePath).ToArray());
            Assert.IsFalse(project.MaterialSources.Any(source => source.SourcePath == aMaterials || source.SourcePath == bMaterials));

            var visiblePrimaryCompanions = VisibleModelFamilyText(form);
            StringAssert.Contains(visiblePrimaryCompanions, "c.vvd");
            StringAssert.Contains(visiblePrimaryCompanions, "c.dx90.vtx");
            Assert.IsFalse(visiblePrimaryCompanions.Contains("a.vvd", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(visiblePrimaryCompanions.Contains("b.vvd", StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(visiblePrimaryCompanions.Contains("crate.vvd", StringComparison.OrdinalIgnoreCase));

            var plan = new BuildCoordinator(Path.Combine(root, "appdata")).PreviewPackage(project);
            var sourceNames = plan.Entries
                .Select(entry => Path.GetFileName(entry.SourcePath ?? string.Empty))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToArray();
            Assert.IsTrue(sourceNames.Contains("c.mdl"));
            Assert.IsTrue(sourceNames.Contains("c.vvd"));
            Assert.IsTrue(sourceNames.Contains("crate.mdl"));
            Assert.IsTrue(sourceNames.Contains("crate.vvd"));
            Assert.IsFalse(sourceNames.Contains("a.mdl"));
            Assert.IsFalse(sourceNames.Contains("a.vvd"));
            Assert.IsFalse(sourceNames.Contains("b.mdl"));
            Assert.IsFalse(sourceNames.Contains("b.vvd"));
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [STATestMethod]
    public void AdditionalModelRemovalRemovesOnlyItsAutomaticMaterialContribution()
    {
        var root = CreateTempRoot();
        try
        {
            var usermod = Path.Combine(root, "game", "usermod");
            var primaryModel = Path.Combine(usermod, "models", "characters", "milo", "milo.mdl");
            var additionalModel = Path.Combine(usermod, "models", "props", "crate", "crate.mdl");
            var primaryMaterials = Path.Combine(usermod, "materials", "models", "characters", "milo");
            var additionalMaterials = Path.Combine(usermod, "materials", "models", "props", "crate");
            var manualMaterials = Path.Combine(usermod, "materials", "manual", "chosen");
            WriteModelFamily(primaryModel, "milo", @"models\characters\milo\", primaryMaterials, "body");
            WriteModelFamily(additionalModel, "crate", @"models\props\crate\", additionalMaterials, "wood");
            WriteFile(Path.Combine(manualMaterials, "manual.vmt"), "manual");

            using var form = NewFormWithoutConfiguredRoot();
            form.Size = new Size(1160, 813);
            InvokePrivate(form, "ShowEditor");
            form.Show();
            GetField<TabControl>(form, "workflowTabs").SelectedIndex = 0;
            Application.DoEvents();
            InvokePrivate(form, "SelectPrimaryModelForPackage", primaryModel);
            InvokePrivate(form, "AddAdditionalModelsForPackage", (object)new[] { additionalModel });

            var project = GetField<PackageProject>(form, "project");
            project.MaterialSources.Add(new SourceEntry
            {
                Kind = SourceEntryKind.Material,
                SourcePath = manualMaterials,
                IsFolder = true,
                IncludeRecursively = true
            });
            InvokePrivate(form, "RefreshLists");
            Application.DoEvents();
            var additionalList = GetField<ListView>(form, "additionalModelsList");
            additionalList.Focus();
            additionalList.Items[0].Selected = true;

            InvokePrivate(form, "RemoveAdditionalModel");

            CollectionAssert.AreEquivalent(
                new[] { primaryMaterials, manualMaterials },
                project.MaterialSources.Select(source => source.SourcePath).ToArray());
            Assert.AreEqual(0, project.Models.Count(model => model.Role == ModelRole.Additional));
            var plan = new BuildCoordinator(Path.Combine(root, "appdata")).PreviewPackage(project);
            Assert.IsFalse(plan.Entries.Any(entry => Path.GetFileName(entry.SourcePath) == "crate.mdl"));
            Assert.IsFalse(plan.Entries.Any(entry => Path.GetFileName(entry.SourcePath) == "crate.vvd"));
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [STATestMethod]
    public void AdditionalModelsShowAuthoritativeRelatedFileCountsWithoutPrimaryFullPathText()
    {
        var root = CreateTempRoot();
        try
        {
            var usermod = Path.Combine(root, "game", "usermod");
            var primaryModel = Path.Combine(usermod, "models", "characters", "milo", "milo.mdl");
            var additionalModel = Path.Combine(usermod, "models", "props", "crate", "crate.mdl");
            WriteMdl(primaryModel, "body", @"models\characters\milo\");
            WriteMdl(additionalModel, "wood", @"models\props\crate\");
            WriteFile(Path.Combine(Path.GetDirectoryName(additionalModel)!, "crate.vvd"), "vvd");
            WriteFile(Path.Combine(Path.GetDirectoryName(additionalModel)!, "crate.dx90.vtx"), "dx90");
            WriteFile(Path.Combine(Path.GetDirectoryName(additionalModel)!, "crate.phy"), "phy");

            using var form = NewFormWithoutConfiguredRoot();
            form.Size = new Size(1160, 813);
            InvokePrivate(form, "ShowEditor");
            form.Show();
            GetField<TabControl>(form, "workflowTabs").SelectedIndex = 0;
            Application.DoEvents();

            InvokePrivate(form, "SelectPrimaryModelForPackage", primaryModel);
            InvokePrivate(form, "AddAdditionalModelsForPackage", (object)new[] { additionalModel });
            Application.DoEvents();

            var project = GetField<PackageProject>(form, "project");
            var additional = project.Models.Single(model => model.Role == ModelRole.Additional);
            var planEntries = new BuildCoordinator(Path.Combine(root, "appdata"))
                .PreviewPackage(project)
                .Entries
                .Where(entry => entry.ModelEntryId == additional.Id)
                .Where(entry => entry.EntryType is PackagePlanEntryType.ModelCompanion)
                .ToArray();
            var additionalFamilyEntries = new BuildCoordinator(Path.Combine(root, "appdata"))
                .PreviewPackage(project)
                .Entries
                .Where(entry => entry.ModelEntryId == additional.Id)
                .Where(entry => entry.EntryType is PackagePlanEntryType.Model or PackagePlanEntryType.ModelCompanion)
                .ToArray();

            var list = GetField<ListView>(form, "additionalModelsList");

            Assert.AreEqual(1, list.Items.Count);
            Assert.AreEqual("crate.mdl", list.Items[0].Text);
            Assert.AreEqual("crate", list.Items[0].SubItems[1].Text);
            Assert.AreEqual(planEntries.Length.ToString(CultureInfo.InvariantCulture), list.Items[0].SubItems[2].Text);
            Assert.AreEqual(string.Empty, GetField<Label>(form, "additionalModelCompanionDetailLabel").Text);
            list.Items[0].Selected = true;
            Application.DoEvents();
            var detailText = GetField<Label>(form, "additionalModelCompanionDetailLabel").Text;
            Assert.AreEqual("Related files included: crate.dx90.vtx, crate.phy, crate.vvd", detailText);
            Assert.IsFalse(detailText.Contains(additionalModel, StringComparison.OrdinalIgnoreCase));
            Assert.AreEqual(string.Empty, GetField<ToolTip>(form, "toolTip").GetToolTip(list));
            Assert.IsFalse(list.Items[0].Text.Contains(Path.GetPathRoot(additionalModel)!, StringComparison.OrdinalIgnoreCase));
            var columnWidth = list.Columns.Cast<ColumnHeader>().Sum(column => column.Width);
            Assert.IsTrue(
                columnWidth <= list.ClientSize.Width,
                $"Column width {columnWidth}, client width {list.ClientSize.Width}, control width {list.Width}.");
            CollectionAssert.AreEqual(new[] { "Model", "Name in ZIP", "Related files" }, list.Columns.Cast<ColumnHeader>().Select(column => column.Text).ToArray());
            Assert.IsTrue(list.Columns[2].Width <= 140);
            Assert.IsTrue(additionalFamilyEntries.Any(entry => Path.GetFileName(entry.SourcePath) == "crate.mdl"));
            Assert.IsTrue(planEntries.Any(entry => Path.GetFileName(entry.SourcePath) == "crate.vvd"));
            Assert.IsTrue(planEntries.Any(entry => Path.GetFileName(entry.SourcePath) == "crate.dx90.vtx"));
            Assert.IsTrue(planEntries.Any(entry => Path.GetFileName(entry.SourcePath) == "crate.phy"));
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [STATestMethod]
    public void AdditionalModelReleaseNameEditorUpdatesTableDetailsAndPlanWithoutRediscovery()
    {
        var root = CreateTempRoot();
        try
        {
            var usermod = Path.Combine(root, "game", "usermod");
            var primaryModel = Path.Combine(usermod, "models", "characters", "gwen", "gwen.mdl");
            var shirtModel = Path.Combine(usermod, "models", "characters", "gwen", "shirt_dev04.mdl");
            var bootsModel = Path.Combine(usermod, "models", "characters", "gwen", "boots.mdl");
            var tailModel = Path.Combine(usermod, "models", "characters", "gwen", "worgen_tail.mdl");
            var shirtMaterials = Path.Combine(usermod, "materials", "models", "characters", "gwen", "shirt");
            WriteMdl(primaryModel, "body", @"models\characters\gwen\");
            WriteMdl(shirtModel, "shirt", @"models\characters\gwen\shirt\");
            WriteMdl(bootsModel, "boots", @"models\characters\gwen\boots\");
            WriteMdl(tailModel, "tail", @"models\characters\gwen\tail\");
            WriteFile(Path.Combine(Path.GetDirectoryName(shirtModel)!, "shirt_dev04.vvd"), "shirt-vvd");
            WriteFile(Path.Combine(Path.GetDirectoryName(bootsModel)!, "boots.phy"), "boots-phy");
            WriteFile(Path.Combine(Path.GetDirectoryName(tailModel)!, "worgen_tail.dx90.vtx"), "tail-dx90");

            using var form = NewFormWithoutConfiguredRoot();
            form.Size = new Size(1160, 813);
            SetField(form, "settings", new AppSettings { DefaultSfmContentFolder = usermod });
            InvokePrivate(form, "ShowEditor");
            form.Show();
            GetField<TabControl>(form, "workflowTabs").SelectedIndex = 0;
            Application.DoEvents();

            InvokePrivate(form, "SelectPrimaryModelForPackage", primaryModel);
            InvokePrivate(form, "AddAdditionalModelsForPackage", (object)new[] { shirtModel, bootsModel, tailModel });
            Application.DoEvents();
            WriteFile(Path.Combine(Path.GetDirectoryName(shirtModel)!, "shirt_dev04.dx90.vtx"), "late-dx90");
            WriteFile(Path.Combine(shirtMaterials, "shirt.vmt"), "late-material");

            var list = GetField<ListView>(form, "additionalModelsList");
            CollectionAssert.AreEqual(new[] { "Model", "Name in ZIP", "Related files" }, list.Columns.Cast<ColumnHeader>().Select(column => column.Text).ToArray());
            Assert.AreEqual("shirt_dev04.mdl", list.Items[0].Text);
            Assert.AreEqual("shirt_dev04", list.Items[0].SubItems[1].Text);
            Assert.AreEqual("boots", list.Items[1].SubItems[1].Text);
            Assert.AreEqual("worgen_tail", list.Items[2].SubItems[1].Text);
            var columnWidth = list.Columns.Cast<ColumnHeader>().Sum(column => column.Width);
            Assert.IsTrue(columnWidth <= list.ClientSize.Width, $"Column width {columnWidth}, client width {list.ClientSize.Width}.");
            Assert.IsTrue(list.Columns[2].Width <= 140);

            list.Items[0].Selected = true;
            Application.DoEvents();
            var editor = GetField<TextBox>(form, "additionalReleaseStemText");
            var validation = GetField<Label>(form, "additionalReleaseStemValidationLabel");
            var preview = GetField<Label>(form, "additionalRenamePreviewLabel");
            var detail = GetField<Label>(form, "additionalModelCompanionDetailLabel");
            Assert.IsTrue(editor.Visible);
            Assert.AreEqual("Model name in ZIP", GetField<Label>(form, "additionalReleaseStemLabel").Text);
            Assert.AreEqual("shirt_dev04", editor.Text);
            Assert.IsFalse(preview.Visible);
            Assert.AreEqual("Related files included: shirt_dev04.vvd", detail.Text);

            editor.Focus();
            editor.SelectAll();
            editor.SelectedText = "Gwen_Shirt";
            Application.DoEvents();
            InvokePrivate(form, "FlushPendingModelNamePresentationForTests");

            Assert.IsTrue(editor.Focused);
            Assert.AreEqual("Gwen_Shirt", editor.Text);
            Assert.AreEqual(editor.TextLength, editor.SelectionStart);
            Assert.AreEqual("Gwen_Shirt", list.Items[0].SubItems[1].Text);
            Assert.AreEqual("Preview: shirt_dev04.mdl → Gwen_Shirt.mdl" + Environment.NewLine + "Your original files stay unchanged.", preview.Text);
            Assert.IsTrue(preview.Visible);
            Assert.AreEqual("Related files included: Gwen_Shirt.vvd", detail.Text);
            var project = GetField<PackageProject>(form, "project");
            Assert.AreEqual(0, project.MaterialSources.Count);
            Assert.AreEqual("Gwen_Shirt", project.Models.Single(model => model.SourceStem == "shirt_dev04").ReleaseStem);

            list.Items[0].Selected = false;
            list.Items[1].Selected = true;
            Application.DoEvents();
            Assert.AreEqual("boots", editor.Text);
            Assert.IsFalse(preview.Visible);
            Assert.AreEqual("Related files included: boots.phy", detail.Text);

            list.Items[1].Selected = false;
            list.Items[2].Selected = true;
            Application.DoEvents();
            editor.Text = "Gwen_Worgen_Tail";
            Application.DoEvents();
            InvokePrivate(form, "FlushPendingModelNamePresentationForTests");
            Assert.AreEqual("Gwen_Worgen_Tail", list.Items[2].SubItems[1].Text);
            Assert.IsFalse(project.Models.Single(model => model.SourceStem == "boots").ReleaseStem.Contains("Gwen", StringComparison.OrdinalIgnoreCase));

            list.Items[2].Selected = false;
            list.Items[0].Selected = true;
            Application.DoEvents();
            Assert.AreEqual("Gwen_Shirt", editor.Text);
            editor.Text = "shirt_dev04";
            Application.DoEvents();
            InvokePrivate(form, "CommitAdditionalReleaseStemText");
            Assert.AreEqual("shirt_dev04", list.Items[0].SubItems[1].Text);
            Assert.AreEqual(string.Empty, project.Models.Single(model => model.SourceStem == "shirt_dev04").ReleaseStem);
            Assert.IsFalse(preview.Visible);
            Assert.AreEqual("Related files included: shirt_dev04.vvd", detail.Text);

            editor.Text = "bad:name";
            Application.DoEvents();
            Assert.IsTrue(validation.Visible);
            Assert.AreEqual("Model name cannot contain \":\".", validation.Text);
            editor.Text = "shirt_dev04";
            InvokePrivate(form, "CommitAdditionalReleaseStemText");

            var plan = new BuildCoordinator(Path.Combine(root, "appdata")).PreviewPackage(project);
            Assert.IsTrue(plan.Entries.Any(entry => entry.DestinationRelativePath == @"models\characters\gwen\Gwen_Worgen_Tail.mdl"));
            Assert.IsTrue(plan.Entries.Any(entry => entry.DestinationRelativePath == @"models\characters\gwen\shirt_dev04.dx90.vtx"));
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [STATestMethod]
    public void PrimaryModelNameEnterCommitsEndsEditAndSynchronizesPresentation()
    {
        var root = CreateTempRoot();
        try
        {
            var usermod = Path.Combine(root, "game", "usermod");
            var modelPath = Path.Combine(usermod, "models", "characters", "milo", "milo.mdl");
            WriteMdl(modelPath, "body", @"models\characters\milo\");
            WriteFile(Path.Combine(Path.GetDirectoryName(modelPath)!, "milo.vvd"), "vvd");

            using var form = NewFormWithoutConfiguredRoot();
            SetField(form, "settings", new AppSettings { DefaultSfmContentFolder = usermod });
            InvokePrivate(form, "ShowEditor");
            form.Show();
            Application.DoEvents();
            InvokePrivate(form, "SelectPrimaryModelForPackage", modelPath);

            var editor = GetField<TextBox>(form, "releaseStemText");
            editor.Focus();
            editor.SelectAll();
            editor.SelectedText = "milo_release";
            Application.DoEvents();

            Assert.IsTrue(InvokePrivateBool(form, "CommitActiveModelNameEditForTests"));
            Application.DoEvents();

            Assert.IsFalse(editor.Focused);
            Assert.AreEqual("milo_release", editor.Text);
            Assert.AreEqual(
                "Preview: milo.mdl → milo_release.mdl" + Environment.NewLine + "Your original files stay unchanged.",
                GetField<Label>(form, "renamePreviewLabel").Text);
            AssertPrimaryRelatedVisible(form, "Related model files included (1)", "milo_release.vvd");
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [STATestMethod]
    public void AdditionalModelNameEnterCommitsEndsEditPreservesSelectionAndSynchronizesPresentation()
    {
        var root = CreateTempRoot();
        try
        {
            var usermod = Path.Combine(root, "game", "usermod");
            var primaryModel = Path.Combine(usermod, "models", "characters", "gwen", "gwen.mdl");
            var shirtModel = Path.Combine(usermod, "models", "characters", "gwen", "shirt_dev04.mdl");
            WriteMdl(primaryModel, "body", @"models\characters\gwen\");
            WriteMdl(shirtModel, "shirt", @"models\characters\gwen\shirt\");
            WriteFile(Path.Combine(Path.GetDirectoryName(shirtModel)!, "shirt_dev04.vvd"), "shirt-vvd");

            using var form = NewFormWithoutConfiguredRoot();
            SetField(form, "settings", new AppSettings { DefaultSfmContentFolder = usermod });
            InvokePrivate(form, "ShowEditor");
            form.Show();
            Application.DoEvents();
            InvokePrivate(form, "SelectPrimaryModelForPackage", primaryModel);
            InvokePrivate(form, "AddAdditionalModelsForPackage", (object)new[] { shirtModel });
            var list = GetField<ListView>(form, "additionalModelsList");
            list.Items[0].Selected = true;
            Application.DoEvents();

            var editor = GetField<TextBox>(form, "additionalReleaseStemText");
            editor.Focus();
            editor.SelectAll();
            editor.SelectedText = "Gwen_Shirt";
            Application.DoEvents();

            Assert.IsTrue(InvokePrivateBool(form, "CommitActiveModelNameEditForTests"));
            Application.DoEvents();

            Assert.IsFalse(editor.Focused);
            Assert.IsTrue(list.Items[0].Selected);
            Assert.AreEqual("Gwen_Shirt", list.Items[0].SubItems[1].Text);
            Assert.AreEqual(
                "Preview: shirt_dev04.mdl → Gwen_Shirt.mdl" + Environment.NewLine + "Your original files stay unchanged.",
                GetField<Label>(form, "additionalRenamePreviewLabel").Text);
            Assert.AreEqual("Related files included: Gwen_Shirt.vvd", GetField<Label>(form, "additionalModelCompanionDetailLabel").Text);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [STATestMethod]
    public void ModelNameTrailingPeriodAndSpaceValidationWaitsUntilCommit()
    {
        var root = CreateTempRoot();
        try
        {
            var usermod = Path.Combine(root, "game", "usermod");
            var modelPath = Path.Combine(usermod, "models", "props", "bunny", "bunny.mdl");
            WriteMdl(modelPath, "body", @"models\props\bunny\");

            using var form = NewFormWithoutConfiguredRoot();
            SetField(form, "settings", new AppSettings { DefaultSfmContentFolder = usermod });
            InvokePrivate(form, "ShowEditor");
            form.Show();
            Application.DoEvents();
            InvokePrivate(form, "SelectPrimaryModelForPackage", modelPath);

            var editor = GetField<TextBox>(form, "releaseStemText");
            var validation = GetField<Label>(form, "releaseStemValidationLabel");
            editor.Focus();
            editor.Text = "Bunny03.";
            Application.DoEvents();

            Assert.IsFalse(validation.Visible);
            Assert.IsFalse(InvokePrivateBool(form, "CommitActiveModelNameEditForTests"));
            Application.DoEvents();
            Assert.IsTrue(validation.Visible);
            Assert.AreEqual("Model name cannot end with a space or period.", validation.Text);
            Assert.IsTrue(editor.Focused);
            Assert.AreEqual("Bunny03.", editor.Text);

            editor.Text = "Bunny03";
            Application.DoEvents();
            Assert.IsTrue(InvokePrivateBool(form, "CommitActiveModelNameEditForTests"));
            Application.DoEvents();
            Assert.IsFalse(validation.Visible);
            Assert.IsFalse(editor.Focused);
            Assert.AreEqual("Bunny03", editor.Text);

            editor.Focus();
            editor.Text = "Bunny03 ";
            Application.DoEvents();
            Assert.IsFalse(validation.Visible);
            Assert.IsFalse(InvokePrivateBool(form, "CommitActiveModelNameEditForTests"));
            Application.DoEvents();
            Assert.IsTrue(validation.Visible);
            Assert.AreEqual("Model name cannot end with a space or period.", validation.Text);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [STATestMethod]
    public void NeutralBackgroundCommitUsesSameModelNameCommitPath()
    {
        var root = CreateTempRoot();
        try
        {
            var usermod = Path.Combine(root, "game", "usermod");
            var modelPath = Path.Combine(usermod, "models", "props", "bunny", "bunny.mdl");
            WriteMdl(modelPath, "body", @"models\props\bunny\");

            using var form = NewFormWithoutConfiguredRoot();
            SetField(form, "settings", new AppSettings { DefaultSfmContentFolder = usermod });
            InvokePrivate(form, "ShowEditor");
            form.Show();
            Application.DoEvents();
            InvokePrivate(form, "SelectPrimaryModelForPackage", modelPath);

            var editor = GetField<TextBox>(form, "releaseStemText");
            editor.Focus();
            editor.Text = "Bunny03";
            Application.DoEvents();
            Assert.IsTrue(InvokePrivateBool(form, "CommitModelNameEditFromNeutralBackgroundForTests"));
            Application.DoEvents();
            Assert.IsFalse(editor.Focused);
            Assert.AreEqual("Bunny03", editor.Text);

            editor.Focus();
            editor.Text = "Bunny03.";
            Application.DoEvents();
            Assert.IsFalse(InvokePrivateBool(form, "CommitModelNameEditFromNeutralBackgroundForTests"));
            Application.DoEvents();
            Assert.IsTrue(editor.Focused);
            Assert.AreEqual("Model name cannot end with a space or period.", GetField<Label>(form, "releaseStemValidationLabel").Text);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [STATestMethod]
    public void AdditionalModelRelatedFilesAlignWithRenameContentColumn()
    {
        var root = CreateTempRoot();
        try
        {
            var usermod = Path.Combine(root, "game", "usermod");
            var primaryModel = Path.Combine(usermod, "models", "characters", "gwen", "gwen.mdl");
            var shirtModel = Path.Combine(usermod, "models", "characters", "gwen", "shirt_dev04.mdl");
            WriteMdl(primaryModel, "body", @"models\characters\gwen\");
            WriteMdl(shirtModel, "shirt", @"models\characters\gwen\shirt\");
            WriteFile(Path.Combine(Path.GetDirectoryName(shirtModel)!, "shirt_dev04.vvd"), "shirt-vvd");

            using var form = NewFormWithoutConfiguredRoot();
            SetField(form, "settings", new AppSettings { DefaultSfmContentFolder = usermod });
            InvokePrivate(form, "ShowEditor");
            form.Show();
            Application.DoEvents();
            InvokePrivate(form, "SelectPrimaryModelForPackage", primaryModel);
            InvokePrivate(form, "AddAdditionalModelsForPackage", (object)new[] { shirtModel });
            var list = GetField<ListView>(form, "additionalModelsList");
            list.Items[0].Selected = true;
            Application.DoEvents();

            var label = GetField<Label>(form, "additionalModelCompanionDetailLabel");
            var preview = GetField<Label>(form, "additionalRenamePreviewLabel");
            Assert.IsInstanceOfType(label.Parent, typeof(TableLayoutPanel));
            var layout = (TableLayoutPanel)label.Parent!;
            Assert.AreEqual(1, layout.GetColumn(label));
            Assert.AreEqual(2, layout.GetColumnSpan(label));
            Assert.AreEqual(preview.Margin.Left, label.Margin.Left);
            Assert.AreEqual("Related files included: shirt_dev04.vvd", label.Text);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [STATestMethod]
    public void PassiveStageOneLabelClickCommitsActiveModelNameWithoutInteractiveStyling()
    {
        var root = CreateTempRoot();
        try
        {
            var usermod = Path.Combine(root, "game", "usermod");
            var primaryModel = Path.Combine(usermod, "models", "characters", "gwen", "gwen.mdl");
            var shirtModel = Path.Combine(usermod, "models", "characters", "gwen", "shirt_dev04.mdl");
            WriteMdl(primaryModel, "body", @"models\characters\gwen\");
            WriteMdl(shirtModel, "shirt", @"models\characters\gwen\shirt\");
            WriteFile(Path.Combine(Path.GetDirectoryName(shirtModel)!, "shirt_dev04.vvd"), "shirt-vvd");

            using var form = NewFormWithoutConfiguredRoot();
            SetField(form, "settings", new AppSettings { DefaultSfmContentFolder = usermod });
            InvokePrivate(form, "ShowEditor");
            form.Show();
            Application.DoEvents();
            InvokePrivate(form, "SelectPrimaryModelForPackage", primaryModel);
            InvokePrivate(form, "AddAdditionalModelsForPackage", (object)new[] { shirtModel });
            var list = GetField<ListView>(form, "additionalModelsList");
            list.Items[0].Selected = true;
            Application.DoEvents();

            var editor = GetField<TextBox>(form, "additionalReleaseStemText");
            var label = GetField<Label>(form, "additionalModelCompanionDetailLabel");
            Assert.AreSame(Cursors.Default, label.Cursor);
            editor.Focus();
            editor.Text = "Gwen_Shirt";
            Application.DoEvents();

            RaiseMouseDown(label);
            Application.DoEvents();

            Assert.IsFalse(editor.Focused);
            Assert.AreEqual("Gwen_Shirt", list.Items[0].SubItems[1].Text);
            Assert.AreEqual("Related files included: Gwen_Shirt.vvd", label.Text);

            editor.Focus();
            editor.Text = "Gwen_Shirt.";
            Application.DoEvents();
            RaiseMouseDown(label);
            Application.DoEvents();

            Assert.IsTrue(editor.Focused);
            Assert.AreEqual("Model name cannot end with a space or period.", GetField<Label>(form, "additionalReleaseStemValidationLabel").Text);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [STATestMethod]
    public void AdditionalModelRowSwitchCommitsPreviousRowWithoutLeakingValues()
    {
        var root = CreateTempRoot();
        try
        {
            var usermod = Path.Combine(root, "game", "usermod");
            var primaryModel = Path.Combine(usermod, "models", "characters", "gwen", "gwen.mdl");
            var modelA = Path.Combine(usermod, "models", "characters", "gwen", "kultiran.mdl");
            var modelB = Path.Combine(usermod, "models", "characters", "gwen", "goblin.mdl");
            WriteMdl(primaryModel, "body", @"models\characters\gwen\");
            WriteMdl(modelA, "body", @"models\characters\gwen\kultiran\");
            WriteMdl(modelB, "body", @"models\characters\gwen\goblin\");
            WriteFile(Path.Combine(Path.GetDirectoryName(modelA)!, "kultiran.vvd"), "vvd");
            WriteFile(Path.Combine(Path.GetDirectoryName(modelB)!, "goblin.phy"), "phy");

            using var form = NewFormWithoutConfiguredRoot();
            SetField(form, "settings", new AppSettings { DefaultSfmContentFolder = usermod });
            InvokePrivate(form, "ShowEditor");
            form.Show();
            Application.DoEvents();
            InvokePrivate(form, "SelectPrimaryModelForPackage", primaryModel);
            InvokePrivate(form, "AddAdditionalModelsForPackage", (object)new[] { modelA, modelB });
            var list = GetField<ListView>(form, "additionalModelsList");
            list.Items[0].Selected = true;
            Application.DoEvents();

            var editor = GetField<TextBox>(form, "additionalReleaseStemText");
            editor.Focus();
            editor.Text = "Carmilla";
            Application.DoEvents();
            list.Items[0].Selected = false;
            list.Items[1].Selected = true;
            Application.DoEvents();

            Assert.AreEqual("Carmilla", list.Items[0].SubItems[1].Text);
            Assert.IsTrue(list.Items[1].Selected);
            Assert.AreEqual("goblin", editor.Text);
            Assert.AreEqual("Related files included: goblin.phy", GetField<Label>(form, "additionalModelCompanionDetailLabel").Text);

            editor.Focus();
            editor.Text = "goblin.";
            Application.DoEvents();
            list.Items[1].Selected = false;
            list.Items[0].Selected = true;
            Application.DoEvents();

            Assert.IsTrue(list.Items[1].Selected);
            Assert.AreEqual("goblin.", editor.Text);
            Assert.AreEqual("Model name cannot end with a space or period.", GetField<Label>(form, "additionalReleaseStemValidationLabel").Text);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [STATestMethod]
    public void RenamePresentationUpdatesAreCoalescedAndCommitForcesFinalFlush()
    {
        var root = CreateTempRoot();
        try
        {
            var usermod = Path.Combine(root, "game", "usermod");
            var modelPath = Path.Combine(usermod, "models", "characters", "milo", "milo.mdl");
            WriteMdl(modelPath, "body", @"models\characters\milo\");
            WriteFile(Path.Combine(Path.GetDirectoryName(modelPath)!, "milo.vvd"), "vvd");

            using var form = NewFormWithoutConfiguredRoot();
            SetField(form, "settings", new AppSettings { DefaultSfmContentFolder = usermod });
            InvokePrivate(form, "ShowEditor");
            form.Show();
            Application.DoEvents();
            InvokePrivate(form, "SelectPrimaryModelForPackage", modelPath);

            var editor = GetField<TextBox>(form, "releaseStemText");
            editor.Focus();
            editor.Text = "A";
            editor.Text = "AB";
            editor.Text = "ABC";
            Application.DoEvents();

            Assert.IsTrue(GetField<System.Windows.Forms.Timer>(form, "modelNamePresentationTimer").Enabled);
            Assert.AreNotEqual(
                "Preview: milo.mdl → ABC.mdl" + Environment.NewLine + "Your original files stay unchanged.",
                GetField<Label>(form, "renamePreviewLabel").Text);

            InvokePrivate(form, "FlushPendingModelNamePresentationForTests");
            Assert.AreEqual(
                "Preview: milo.mdl → ABC.mdl" + Environment.NewLine + "Your original files stay unchanged.",
                GetField<Label>(form, "renamePreviewLabel").Text);

            editor.Focus();
            editor.Text = "ABCD";
            Assert.IsTrue(GetField<System.Windows.Forms.Timer>(form, "modelNamePresentationTimer").Enabled);
            Assert.IsTrue(InvokePrivateBool(form, "CommitActiveModelNameEditForTests"));
            Assert.IsFalse(GetField<System.Windows.Forms.Timer>(form, "modelNamePresentationTimer").Enabled);
            Assert.AreEqual(
                "Preview: milo.mdl → ABCD.mdl" + Environment.NewLine + "Your original files stay unchanged.",
                GetField<Label>(form, "renamePreviewLabel").Text);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [STATestMethod]
    public void AdditionalModelSelectionShowsVisibleCompanionDetailsForSelectedRow()
    {
        var root = CreateTempRoot();
        try
        {
            var usermod = Path.Combine(root, "game", "usermod");
            var primaryModel = Path.Combine(usermod, "models", "characters", "milo", "milo.mdl");
            var firstModel = Path.Combine(usermod, "models", "props", "crate", "crate.mdl");
            var secondModel = Path.Combine(usermod, "models", "props", "barrel", "barrel.mdl");
            var thirdModel = Path.Combine(usermod, "models", "props", "lamp", "lamp.mdl");
            WriteMdl(primaryModel, "body", @"models\characters\milo\");
            WriteMdl(firstModel, "wood", @"models\props\crate\");
            WriteMdl(secondModel, "metal", @"models\props\barrel\");
            WriteMdl(thirdModel, "glass", @"models\props\lamp\");
            WriteFile(Path.Combine(Path.GetDirectoryName(firstModel)!, "crate.vvd"), "vvd");
            WriteFile(Path.Combine(Path.GetDirectoryName(secondModel)!, "barrel.phy"), "phy");
            WriteFile(Path.Combine(Path.GetDirectoryName(thirdModel)!, "lamp.dx90.vtx"), "dx90");

            using var form = NewFormWithoutConfiguredRoot();
            form.Size = new Size(1160, 813);
            InvokePrivate(form, "ShowEditor");
            form.Show();
            GetField<TabControl>(form, "workflowTabs").SelectedIndex = 0;
            Application.DoEvents();

            InvokePrivate(form, "SelectPrimaryModelForPackage", primaryModel);
            InvokePrivate(form, "AddAdditionalModelsForPackage", (object)new[] { firstModel, secondModel, thirdModel });
            Application.DoEvents();

            var list = GetField<ListView>(form, "additionalModelsList");
            var detail = GetField<Label>(form, "additionalModelCompanionDetailLabel");
            var remove = GetField<Button>(form, "removeAdditionalModelButton");
            Assert.AreEqual(3, list.Items.Count);
            Assert.AreEqual(string.Empty, detail.Text);
            Assert.IsFalse(remove.Enabled);

            list.Items[0].Selected = true;
            Application.DoEvents();
            Assert.AreEqual("Related files included: crate.vvd", detail.Text);
            Assert.IsTrue(remove.Enabled);
            Assert.IsFalse(detail.Text.Contains("barrel.phy", StringComparison.OrdinalIgnoreCase));

            list.Items[0].Selected = false;
            list.Items[1].Selected = true;
            Application.DoEvents();
            Assert.AreEqual("Related files included: barrel.phy", detail.Text);
            Assert.IsFalse(detail.Text.Contains("crate.vvd", StringComparison.OrdinalIgnoreCase));

            list.Items[1].Selected = false;
            list.Items[2].Selected = true;
            Application.DoEvents();
            Assert.AreEqual("Related files included: lamp.dx90.vtx", detail.Text);

            list.Items[2].Selected = false;
            Application.DoEvents();
            Assert.AreEqual(string.Empty, detail.Text);
            Assert.IsFalse(remove.Enabled);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [STATestMethod]
    public void AdditionalModelSelectionShowsNoneAndEditorExpansionStaysStableWhileTyping()
    {
        var root = CreateTempRoot();
        try
        {
            var usermod = Path.Combine(root, "game", "usermod");
            var primaryModel = Path.Combine(usermod, "models", "characters", "milo", "milo.mdl");
            var additionalModel = Path.Combine(usermod, "models", "props", "crate", "crate.mdl");
            WriteMdl(primaryModel, "body", @"models\characters\milo\");
            WriteMdl(additionalModel, "wood", @"models\props\crate\");

            using var form = NewFormWithoutConfiguredRoot();
            form.Size = new Size(1160, 813);
            InvokePrivate(form, "ShowEditor");
            form.Show();
            GetField<TabControl>(form, "workflowTabs").SelectedIndex = 0;
            Application.DoEvents();

            InvokePrivate(form, "SelectPrimaryModelForPackage", primaryModel);
            InvokePrivate(form, "AddAdditionalModelsForPackage", (object)new[] { additionalModel });
            Application.DoEvents();

            var list = GetField<ListView>(form, "additionalModelsList");
            var detail = GetField<Label>(form, "additionalModelCompanionDetailLabel");
            var materials = GetField<ListBox>(form, "materialsList");
            var beforeTop = materials.PointToScreen(Point.Empty).Y;
            var lineHeight = TextRenderer.MeasureText("Related files", detail.Font).Height;
            var editor = GetField<TextBox>(form, "additionalReleaseStemText");

            Assert.AreEqual(string.Empty, detail.Text);
            Assert.IsFalse(editor.Visible);
            Assert.IsTrue(detail.Height > 0);
            Assert.IsTrue(detail.Height <= lineHeight * 2 + 8, $"Reserved detail height was {detail.Height}; expected approximately two lines.");
            list.Items[0].Selected = true;
            Application.DoEvents();

            Assert.AreEqual("Related files included: None", detail.Text);
            Assert.IsTrue(editor.Visible);
            Assert.IsTrue(materials.PointToScreen(Point.Empty).Y > beforeTop);
            var expandedTop = materials.PointToScreen(Point.Empty).Y;
            editor.Text = "crate_release";
            Application.DoEvents();
            InvokePrivate(form, "FlushPendingModelNamePresentationForTests");
            Assert.IsTrue(materials.PointToScreen(Point.Empty).Y > expandedTop);
            var renamedTop = materials.PointToScreen(Point.Empty).Y;
            editor.Text = "crate_release2";
            Application.DoEvents();
            InvokePrivate(form, "FlushPendingModelNamePresentationForTests");
            Assert.AreEqual(renamedTop, materials.PointToScreen(Point.Empty).Y);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [STATestMethod]
    public void DuplicateAdditionalModelSelectionIsRejectedWithFeedbackAndNoDirtyChange()
    {
        var root = CreateTempRoot();
        try
        {
            var usermod = Path.Combine(root, "game", "usermod");
            var primaryModel = Path.Combine(usermod, "models", "characters", "milo", "milo.mdl");
            var additionalModel = Path.Combine(usermod, "models", "props", "crate", "crate.mdl");
            WriteMdl(primaryModel, "body", @"models\characters\milo\");
            WriteMdl(additionalModel, "wood", @"models\props\crate\");

            using var form = NewFormWithoutConfiguredRoot();
            var feedback = new List<string>();
            SetField(form, "duplicateModelFeedback", new Action<string>(feedback.Add));
            InvokePrivate(form, "ShowEditor");
            InvokePrivate(form, "SelectPrimaryModelForPackage", primaryModel);
            SetField(form, "isDirty", false);

            InvokePrivate(form, "AddAdditionalModelsForPackage", (object)new[] { primaryModel });

            Assert.AreEqual(0, GetField<PackageProject>(form, "project").Models.Count(model => model.Role == ModelRole.Additional));
            CollectionAssert.AreEqual(new[] { "This model is already in the package." }, feedback.ToArray());
            Assert.IsFalse(GetFieldValue<bool>(form, "isDirty"));

            InvokePrivate(form, "AddAdditionalModelsForPackage", (object)new[] { additionalModel });
            SetField(form, "isDirty", false);
            InvokePrivate(form, "AddAdditionalModelsForPackage", (object)new[] { additionalModel.ToUpperInvariant() });

            Assert.AreEqual(1, GetField<PackageProject>(form, "project").Models.Count(model => model.Role == ModelRole.Additional));
            Assert.AreEqual(2, feedback.Count);
            Assert.IsFalse(GetFieldValue<bool>(form, "isDirty"));
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [STATestMethod]
    public void ReopenedProjectRefreshesCurrentSourceFactsWithoutMutatingChoicesOrDirtyState()
    {
        var root = CreateTempRoot();
        try
        {
            var usermod = Path.Combine(root, "game", "usermod");
            var modelPath = Path.Combine(usermod, "models", "characters", "milo", "milo.mdl");
            var additionalModel = Path.Combine(usermod, "models", "props", "crate", "crate.mdl");
            var materialFolder = Path.Combine(usermod, "materials", "models", "characters", "milo");
            var extraPath = Path.Combine(root, "extras", "pose.dmx");
            var projectPath = Path.Combine(root, "milo.sfmpack");
            WriteMdl(modelPath, "body", @"models\characters\milo\");
            WriteMdl(additionalModel, "wood", @"models\props\crate\");
            WriteFile(Path.Combine(Path.GetDirectoryName(modelPath)!, "milo.vvd"), "vvd");
            WriteFile(Path.Combine(Path.GetDirectoryName(additionalModel)!, "crate.vvd"), "vvd");
            WriteFile(Path.Combine(materialFolder, "body.vmt"), "vmt");
            WriteFile(extraPath, "extra");

            using (var form = NewFormWithoutConfiguredRoot())
            {
                InvokePrivate(form, "ShowEditor");
                InvokePrivate(form, "SelectPrimaryModelForPackage", modelPath);
                InvokePrivate(form, "AddAdditionalModelsForPackage", (object)new[] { additionalModel });
                GetField<ListBox>(form, "materialsList").SelectedIndex = 0;
                InvokePrivate(form, "RemoveSelectedMaterial");
                GetField<TextBox>(form, "assetNameText").Text = "Milo Asset";
                GetField<TextBox>(form, "versionText").Text = "1.0.1";
                GetField<TextBox>(form, "releaseStemText").Text = "milo_release";
                GetField<RadioButton>(form, "readmeGeneratedRadio").Checked = false;
                GetField<RadioButton>(form, "readmeImportRadio").Checked = false;
                GetField<RadioButton>(form, "readmeNoneRadio").Checked = false;
                GetField<RadioButton>(form, "readmeCustomRadio").Checked = true;
                GetField<TextBox>(form, "customReadmeText").Text = "Custom README stays put.";
                InvokePrivate(form, "SyncProjectFromControls");
                var project = GetField<PackageProject>(form, "project");
                Assert.AreEqual("Custom README stays put.", project.Readme.CustomReadmeText);
                project.Extras.Add(new SourceEntry
                {
                    Kind = SourceEntryKind.Extra,
                    SourcePath = extraPath,
                    IsFolder = false,
                    IncludeRecursively = false,
                    DestinationOverride = new DestinationOverride { Kind = DestinationOverrideKind.Root, RelativePath = string.Empty }
                });
                new ProjectSerializer().Save(projectPath, project);
            }

            File.Delete(Path.Combine(Path.GetDirectoryName(modelPath)!, "milo.vvd"));
            File.Delete(Path.Combine(Path.GetDirectoryName(additionalModel)!, "crate.vvd"));
            WriteFile(Path.Combine(Path.GetDirectoryName(modelPath)!, "milo.phy"), "phy");
            WriteFile(Path.Combine(Path.GetDirectoryName(additionalModel)!, "crate.phy"), "phy");
            WriteMdl(modelPath, "newbody", @"models\characters\milo\");
            WriteMdl(additionalModel, "newwood", @"models\props\crate\");

            var loaded = new ProjectSerializer().Load(projectPath).Project!;
            Assert.AreEqual("Custom README stays put.", loaded.Readme.CustomReadmeText);
            using var reopenedForm = NewFormWithoutConfiguredRoot();
            InvokePrivate(reopenedForm, "CommitProjectSession", loaded, projectPath, false, null);
            reopenedForm.Show();
            GetField<TabControl>(reopenedForm, "workflowTabs").SelectedIndex = 0;
            Application.DoEvents();

            var reopenedProject = GetField<PackageProject>(reopenedForm, "project");
            var visibleFamilyText = GetField<Label>(reopenedForm, "modelFamilyNamesLabel").Text;
            var plan = new BuildCoordinator(Path.Combine(root, "appdata")).PreviewPackage(reopenedProject);
            var primary = reopenedProject.Models.Single(model => model.Role == ModelRole.Primary);
            var primaryCompanionNames = plan.Entries
                .Where(entry => entry.EntryType == PackagePlanEntryType.ModelCompanion && entry.ModelEntryId == primary.Id)
                .Select(entry => Path.GetFileName(entry.DestinationRelativePath ?? string.Empty))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToArray();

            CollectionAssert.AreEqual(primaryCompanionNames, visibleFamilyText.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries));
            StringAssert.Contains(visibleFamilyText, "milo_release.phy");
            Assert.IsFalse(visibleFamilyText.Contains("milo.vvd", StringComparison.OrdinalIgnoreCase));
            var additionalList = GetField<ListView>(reopenedForm, "additionalModelsList");
            Assert.AreEqual("crate", additionalList.Items[0].SubItems[1].Text);
            Assert.AreEqual("1", additionalList.Items[0].SubItems[2].Text);
            additionalList.Items[0].Selected = true;
            Application.DoEvents();
            var additionalDetail = GetField<Label>(reopenedForm, "additionalModelCompanionDetailLabel").Text;
            StringAssert.Contains(additionalDetail, "crate.phy");
            Assert.IsFalse(additionalDetail.Contains("crate.vvd", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(plan.Entries.Any(entry => Path.GetFileName(entry.SourcePath) == "milo.phy"));
            Assert.IsFalse(plan.Entries.Any(entry => Path.GetFileName(entry.SourcePath) == "milo.vvd"));
            Assert.IsTrue(plan.Entries.Any(entry => Path.GetFileName(entry.SourcePath) == "crate.phy"));
            Assert.IsFalse(plan.Entries.Any(entry => Path.GetFileName(entry.SourcePath) == "crate.vvd"));
            Assert.AreEqual(0, reopenedProject.MaterialSources.Count);
            Assert.AreEqual("Milo Asset", reopenedProject.AssetName);
            Assert.AreEqual("1.0.1", reopenedProject.CurrentVersion);
            Assert.AreEqual("Custom README stays put.", GetField<TextBox>(reopenedForm, "customReadmeText").Text);
            Assert.AreEqual("Custom README stays put.", reopenedProject.Readme.CustomReadmeText);
            Assert.AreEqual("milo_release", reopenedProject.Models.Single(model => model.Role == ModelRole.Primary).ReleaseStem);
            Assert.AreEqual(extraPath, reopenedProject.Extras.Single().SourcePath);
            Assert.AreEqual(modelPath, reopenedProject.Models.Single(model => model.Role == ModelRole.Primary).SourceMdlPath);
            Assert.IsFalse(GetFieldValue<bool>(reopenedForm, "isDirty"));
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [STATestMethod]
    public void StartWithGeneratedReadmePopulatesCustomEditorAndPreservesCustomMode()
    {
        var root = CreateTempRoot();
        try
        {
            var usermod = Path.Combine(root, "game", "usermod");
            var modelPath = Path.Combine(usermod, "models", "props", "display_chair", "display_chair.mdl");
            WriteMdl(modelPath, "body", @"models\props\display_chair\");

            using var form = NewFormWithoutConfiguredRoot();
            SetField(form, "settings", new AppSettings { DefaultSfmContentFolder = usermod });
            InvokePrivate(form, "ShowEditor");
            form.Show();
            Application.DoEvents();
            InvokePrivate(form, "SelectPrimaryModelForPackage", modelPath);
            GetField<TextBox>(form, "assetNameText").Text = "Display Chair";
            GetField<TextBox>(form, "versionText").Text = "1.0";
            GetField<RadioButton>(form, "readmeCustomRadio").Checked = true;
            GetField<TabControl>(form, "workflowTabs").SelectedIndex = 2;
            Application.DoEvents();

            var button = GetField<Button>(form, "startFromGeneratedButton");
            Assert.IsTrue(button.Visible);
            button.PerformClick();

            var project = GetField<PackageProject>(form, "project");
            Assert.AreEqual("Generate Starting Draft", button.Text);
            Assert.AreSame(GetField<TableLayoutPanel>(form, "customReadmeHeaderPanel"), button.Parent);
            Assert.AreEqual(ReadmeMode.Custom, project.Readme.Mode);
            Assert.AreEqual(ReadmeCustomSource.ProjectText, project.Readme.CustomSource);
            StringAssert.Contains(GetField<TextBox>(form, "customReadmeText").Text, "DISPLAY CHAIR");
            Assert.IsFalse(string.IsNullOrWhiteSpace(project.Readme.CustomReadmeGeneratedFromFingerprint));
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [STATestMethod]
    public void RemovedAutomaticallyDiscoveredMaterialIsNotReaddedByReviewRefresh()
    {
        var root = CreateTempRoot();
        try
        {
            var usermod = Path.Combine(root, "game", "usermod");
            var modelPath = Path.Combine(usermod, "models", "characters", "milo", "milo.mdl");
            var materialFolder = Path.Combine(usermod, "materials", "models", "characters", "milo");
            WriteMdl(modelPath, "body", @"models\characters\milo\");
            WriteFile(Path.Combine(materialFolder, "body.vmt"), "vmt");

            using var form = NewFormWithoutConfiguredRoot();
            InvokePrivate(form, "ShowEditor");
            InvokePrivate(form, "SelectPrimaryModelForPackage", modelPath);

            var materialsList = GetField<ListBox>(form, "materialsList");
            materialsList.SelectedIndex = 0;
            InvokePrivate(form, "RemoveSelectedMaterial");
            InvokePrivate(form, "RefreshReviewAndBuild");

            Assert.AreEqual(0, GetField<PackageProject>(form, "project").MaterialSources.Count);
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [STATestMethod]
    public void AutomaticallyDiscoveredMaterialPersistsAndLoadDoesNotDuplicateOrDirtyProject()
    {
        var root = CreateTempRoot();
        try
        {
            var usermod = Path.Combine(root, "game", "usermod");
            var modelPath = Path.Combine(usermod, "models", "characters", "milo", "milo.mdl");
            var materialFolder = Path.Combine(usermod, "materials", "models", "characters", "milo");
            var projectPath = Path.Combine(root, "milo.sfmpack");
            WriteMdl(modelPath, "body", @"models\characters\milo\");
            WriteFile(Path.Combine(materialFolder, "body.vmt"), "vmt");

            using (var form = NewFormWithoutConfiguredRoot())
            {
                InvokePrivate(form, "ShowEditor");
                InvokePrivate(form, "SelectPrimaryModelForPackage", modelPath);
                new ProjectSerializer().Save(projectPath, GetField<PackageProject>(form, "project"));
            }

            var loaded = new ProjectSerializer().Load(projectPath).Project!;
            using var reopenedForm = NewFormWithoutConfiguredRoot();
            InvokePrivate(reopenedForm, "CommitProjectSession", loaded, projectPath, false, null);

            var project = GetField<PackageProject>(reopenedForm, "project");
            CollectionAssert.AreEqual(new[] { materialFolder }, project.MaterialSources.Select(source => source.SourcePath).ToArray());
            Assert.IsFalse(GetFieldValue<bool>(reopenedForm, "isDirty"));
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [STATestMethod]
    public void UnsupportedMdlSelectionSucceedsWithoutInventingMaterials()
    {
        var root = CreateTempRoot();
        try
        {
            var usermod = Path.Combine(root, "game", "usermod");
            var modelPath = Path.Combine(usermod, "models", "characters", "milo", "milo.mdl");
            var materialFolder = Path.Combine(usermod, "materials", "models", "characters", "milo");
            WriteMdl(modelPath, "body", @"models\characters\milo\", version: 48);
            Directory.CreateDirectory(materialFolder);

            using var form = NewFormWithoutConfiguredRoot();
            InvokePrivate(form, "ShowEditor");

            InvokePrivate(form, "SelectPrimaryModelForPackage", modelPath);

            var project = GetField<PackageProject>(form, "project");
            Assert.AreEqual(1, project.Models.Count);
            Assert.AreEqual(modelPath, project.Models[0].SourceMdlPath);
            Assert.AreEqual(0, project.MaterialSources.Count);
            Assert.IsTrue(AllControls(form).OfType<Button>().Any(button => button.Text == "Add Folder..."));
            Assert.IsTrue(AllControls(form).OfType<Button>().Any(button => button.Text == "Add Files..."));
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [STATestMethod]
    public void MainFormBrowseStartsUseConfiguredModelAndMaterialRootsWithoutCrossContamination()
    {
        var root = CreateTempRoot();
        try
        {
            var usermod = Path.Combine(root, "game", "usermod");
            var modelsRoot = Path.Combine(usermod, "models");
            var materialModelsRoot = Path.Combine(usermod, "materials", "models");
            Directory.CreateDirectory(modelsRoot);
            Directory.CreateDirectory(materialModelsRoot);

            using var form = NewFormWithoutConfiguredRoot();
            SetField(form, "settings", new AppSettings { DefaultSfmContentFolder = usermod });

            Assert.AreEqual(modelsRoot, InvokePrivateString(form, "GetModelBrowseStart", string.Empty));
            Assert.AreEqual(materialModelsRoot, InvokePrivateString(form, "GetMaterialBrowseStart"));

            var selectedModel = Path.Combine(modelsRoot, "characters", "mia", "mia.mdl");
            WriteMdl(selectedModel, "body", @"models\characters\mia\");
            InvokePrivate(form, "SelectPrimaryModelForPackage", selectedModel);

            Assert.AreEqual(Path.GetDirectoryName(selectedModel), InvokePrivateString(form, "GetModelBrowseStart", selectedModel));
            Assert.AreEqual(materialModelsRoot, InvokePrivateString(form, "GetMaterialBrowseStart"));
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    private static void SetField(object instance, string name, object? value) =>
        instance.GetType().GetField(name, PrivateInstance)?.SetValue(instance, value);

    private static void InvokePrivate(object instance, string name) =>
        instance.GetType().GetMethod(name, PrivateInstance)?.Invoke(instance, Array.Empty<object>());

    private static void InvokePrivate(object instance, string name, params object?[] args) =>
        instance.GetType().GetMethod(name, PrivateInstance)?.Invoke(instance, args);

    private static bool InvokePrivateBool(object instance, string name, params object?[] args) =>
        (bool)(instance.GetType().GetMethod(name, PrivateInstance)?.Invoke(instance, args)
            ?? throw new InvalidOperationException("Method not found: " + name));

    private static void RaiseMouseDown(Control control)
    {
        var method = typeof(Control).GetMethod("OnMouseDown", PrivateInstance)
            ?? throw new InvalidOperationException("Control.OnMouseDown not found.");
        method.Invoke(control, new object[] { new MouseEventArgs(MouseButtons.Left, 1, 1, 1, 0) });
    }

    private static string InvokePrivateString(object instance, string name, params object?[] args) =>
        (string)(instance.GetType().GetMethod(name, PrivateInstance)?.Invoke(instance, args)
            ?? throw new InvalidOperationException("Method not found: " + name));

    private static T GetField<T>(object instance, string name) where T : class =>
        (T)(instance.GetType().GetField(name, PrivateInstance)?.GetValue(instance)
            ?? throw new InvalidOperationException("Field not found: " + name));

    private static T? GetFieldValue<T>(object instance, string name)
    {
        var field = instance.GetType().GetField(name, PrivateInstance)
            ?? throw new InvalidOperationException("Field not found: " + name);
        return (T?)field.GetValue(instance);
    }

    private static MainForm NewFormWithoutConfiguredRoot()
    {
        var form = new MainForm();
        SetField(form, "settings", new AppSettings());
        return form;
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "SfmPackageBuilder.WinForms.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static void DeleteTempRoot(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static void WriteFile(string path, string contents)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents, Encoding.UTF8);
    }

    private static void WriteMdl(string path, string texture, string searchPath, int version = 49)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, new SyntheticMdl()
            .WithVersion(version)
            .WithTextures(texture)
            .WithCdTextures(searchPath)
            .WithSkinTable(new[] { new short[] { 0 } })
            .ToArray());
    }

    private static void WriteModelFamily(string modelPath, string stem, string searchPath, string materialFolder, string texture)
    {
        WriteMdl(modelPath, texture, searchPath);
        WriteFile(Path.Combine(Path.GetDirectoryName(modelPath)!, stem + ".vvd"), "vvd");
        WriteFile(Path.Combine(Path.GetDirectoryName(modelPath)!, stem + ".dx90.vtx"), "dx90");
        WriteFile(Path.Combine(materialFolder, texture + ".vmt"), "vmt");
    }

    private static string VisibleModelFamilyText(MainForm form)
    {
        return GetField<Label>(form, "modelFamilyNamesLabel").Text;
    }

    private static void AssertPrimaryRelatedHidden(MainForm form)
    {
        var summary = GetField<Label>(form, "modelFamilySummaryLabel");
        var names = GetField<Label>(form, "modelFamilyNamesLabel");
        Assert.IsFalse(summary.Visible);
        Assert.IsFalse(names.Visible);
        Assert.AreEqual(string.Empty, summary.Text);
        Assert.AreEqual(string.Empty, names.Text);
    }

    private static void AssertPrimaryRelatedVisible(MainForm form, string expectedSummary, string expectedNames)
    {
        var summary = GetField<Label>(form, "modelFamilySummaryLabel");
        var names = GetField<Label>(form, "modelFamilyNamesLabel");
        Assert.IsTrue(summary.Visible);
        Assert.IsTrue(names.Visible);
        Assert.AreEqual(expectedSummary, summary.Text);
        Assert.AreEqual(expectedNames, names.Text);
    }

    private static void AssertPrimaryModelNameDisabled(MainForm form)
    {
        var modelName = GetField<TextBox>(form, "releaseStemText");
        Assert.IsFalse(modelName.Enabled);
        Assert.AreEqual(string.Empty, modelName.Text);
        Assert.IsFalse(GetField<Label>(form, "renamePreviewLabel").Visible);
        Assert.AreEqual(string.Empty, GetField<Label>(form, "renamePreviewLabel").Text);
    }

    private static void AssertPrimaryModelNameEnabled(MainForm form, string expectedName)
    {
        var modelName = GetField<TextBox>(form, "releaseStemText");
        Assert.IsTrue(modelName.Enabled);
        Assert.AreEqual(expectedName, modelName.Text);
        Assert.IsFalse(GetField<Label>(form, "renamePreviewLabel").Visible);
        Assert.AreEqual(string.Empty, GetField<Label>(form, "renamePreviewLabel").Text);
    }

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

    private sealed class SyntheticMdl
    {
        private readonly byte[] bytes = new byte[4096];
        private int cursor = 512;

        public SyntheticMdl()
        {
            Encoding.ASCII.GetBytes("IDST", bytes.AsSpan(0, 4));
            WithVersion(49);
            WriteInt32(8, 1);
            WriteInt32(76, bytes.Length);
            WithTextures(Array.Empty<string>());
            WithCdTextures(Array.Empty<string>());
            WriteInt32(220, 0);
            WriteInt32(224, 0);
            WriteInt32(228, 0);
        }

        public SyntheticMdl WithVersion(int version)
        {
            WriteInt32(4, version);
            return this;
        }

        public SyntheticMdl WithTextures(params string[] names)
        {
            const int tableOffset = 256;
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

        public SyntheticMdl WithCdTextures(params string[] paths)
        {
            var tableOffset = Align(cursor, 4);
            WriteInt32(212, paths.Length);
            WriteInt32(216, tableOffset);
            cursor = tableOffset + paths.Length * 4;
            var offsets = paths.Select(WriteString).ToArray();
            for (var i = 0; i < offsets.Length; i++)
            {
                WriteInt32(tableOffset + i * 4, offsets[i]);
            }

            return this;
        }

        public SyntheticMdl WithSkinTable(short[][] remapMatrix)
        {
            var familyCount = remapMatrix.Length;
            var slotCount = familyCount == 0 ? 0 : remapMatrix[0].Length;
            var tableOffset = Align(cursor, 2);
            WriteInt32(220, slotCount);
            WriteInt32(224, familyCount);
            WriteInt32(228, tableOffset);
            cursor = tableOffset + slotCount * familyCount * 2;
            for (var family = 0; family < familyCount; family++)
            {
                for (var slot = 0; slot < slotCount; slot++)
                {
                    BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(tableOffset + 2 * (family * slotCount + slot), 2), remapMatrix[family][slot]);
                }
            }

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
}
