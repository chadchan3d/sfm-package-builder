using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using SfmPackageBuilder.Core.Archive;
using SfmPackageBuilder.Core.Build;
using SfmPackageBuilder.Core.Discovery;
using SfmPackageBuilder.Core.Domain;
using SfmPackageBuilder.Core.FileSystem;
using SfmPackageBuilder.Core.Mdl;
using SfmPackageBuilder.Core.Persistence;
using SfmPackageBuilder.Core.Planning;
using SfmPackageBuilder.Core.Readme;
using SfmPackageBuilder.Core.Validation;
using SfmPackageBuilder.WinForms.Presentation;

namespace SfmPackageBuilder.WinForms;

public sealed class MainForm : Form
{
    public static Color ExtrasHeaderBackgroundColor => SystemColors.Window;
    public static FontStyle ExtrasHeaderFontStyle => FontStyle.Regular;
    public static Color RecentWorkspaceBackColor => Color.FromArgb(226, 226, 226);
    public static Color RecentCardNormalBackColor => SystemColors.Window;
    public static Color RecentCardHoverBackColor => Color.FromArgb(245, 249, 255);

    private readonly SettingsService settingsService = new();
    private readonly ProjectSerializer projectSerializer = new();
    private readonly ArchiveNameSuggester archiveNameSuggester = new();
    private readonly ArchiveNameSuggestionState archiveNameState;
    private readonly IFileSystem fileSystem = new PhysicalFileSystem();
    private readonly MaterialSourceDiscoveryService materialSourceDiscoveryService;
    private readonly BrowseStartResolver browseStartResolver = new();
    private readonly DecisionPresenter decisionPresenter = new();
    private readonly ExtraDestinationPresenter extraDestinationPresenter = new();
    private readonly BuildHistoryPersistencePolicy buildHistoryPersistencePolicy = new();
    private readonly PackagePlanTreeBuilder packagePlanTreeBuilder = new();
    private readonly ProjectCloneService projectCloneService = new();
    private readonly ProjectDirectoryPreferenceService projectDirectoryPreferenceService = new();
    private readonly ReleaseModelNamePresenter releaseModelNamePresenter = new();
    private readonly UnsavedWorkPromptPresenter unsavedWorkPromptPresenter = new();
    private readonly ValidationPresenter validationPresenter = new();
    private readonly ToolTip toolTip = new();

    private AppSettings settings;
    private RecentProjectsService recentProjectsService;
    private PackageProject project = new();
    private string? projectPath;
    private bool isDirty;
    private bool loadingControls;
    private bool updatingArchiveSuggestion;
    private bool refreshingReview;
    private string lastModelBrowseFolder = string.Empty;
    private string lastMaterialBrowseFolder = string.Empty;
    private string lastExtraBrowseFolder = string.Empty;

    private readonly Panel launchPanel = new() { Dock = DockStyle.Fill };
    private readonly Panel editorPanel = new() { Dock = DockStyle.Fill, Visible = false };
    private readonly Panel recentWorkspacePanel = new() { Dock = DockStyle.Fill };
    private readonly Label recentEmptyStateLabel = new() { Dock = DockStyle.Fill, Text = "No recent packages yet.", TextAlign = ContentAlignment.MiddleCenter, ForeColor = SystemColors.GrayText };
    private readonly FlowLayoutPanel recentList = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, AutoScroll = true, WrapContents = true };
    private readonly TabControl workflowTabs = new() { Dock = DockStyle.Fill };
    private TabPage? reviewTab;

    private readonly TextBox assetNameText = new();
    private readonly TextBox versionText = new();
    private readonly TextBox archiveNameText = new();
    private readonly TextBox outputFolderText = new();
    private readonly TextBox primaryModelText = new();
    private readonly Label sourceInstructionLabel = new();
    private readonly TextBox releaseStemText = new();
    private readonly Label releaseStemValidationLabel = new();
    private readonly Label renamePreviewLabel = new();
    private readonly Label modelFamilySummaryLabel = new();
    private readonly Label modelFamilyNamesLabel = new();
    private readonly Label additionalModelsSummaryLabel = new();
    private readonly Button addAdditionalModelButton = new();
    private readonly Button removeAdditionalModelButton = new();
    private readonly ListView additionalModelsList = new();
    private readonly Label additionalReleaseStemLabel = new();
    private readonly TextBox additionalReleaseStemText = new();
    private readonly Label additionalReleaseStemValidationLabel = new();
    private readonly Label additionalRenamePreviewLabel = new();
    private readonly Label additionalModelCompanionDetailLabel = new();
    private readonly ListBox materialsList = new();
    private readonly ListView extrasList = new();
    private readonly ComboBox extraDestinationCombo = new();
    private readonly TextBox extraCustomDestinationText = new();
    private readonly RadioButton readmeGeneratedRadio = new();
    private readonly RadioButton readmeCustomRadio = new();
    private readonly RadioButton readmeImportRadio = new();
    private readonly RadioButton readmeNoneRadio = new();
    private readonly TextBox readmeDescriptionText = new();
    private readonly TextBox readmeAuthorText = new();
    private readonly TextBox readmeWebsiteText = new();
    private readonly TextBox readmeLicenseText = new();
    private readonly TextBox readmeResourcesText = new();
    private readonly TextBox creditsText = new();
    private readonly TextBox importedReadmeText = new();
    private readonly TextBox customReadmeText = new();
    private readonly TextBox changesText = new();
    private readonly ListView releaseHistoryList = new();
    private readonly Button addReleaseHistoryButton = new();
    private readonly Label extraSelectionHelpLabel = new();
    private readonly Label extraCustomDestinationLabel = new();
    private readonly Label extraCustomDestinationHelperLabel = new();
    private readonly Label importedReadmeLabel = new();
    private readonly Label importedReadmeHelperLabel = new();
    private readonly Label customReadmeLabel = new();
    private readonly Label customReadmeHelperLabel = new();
    private readonly TableLayoutPanel customReadmeHeaderPanel = new();
    private readonly Label noReadmeStateLabel = new();
    private readonly Button importedReadmeBrowseButton = new();
    private readonly Button startFromGeneratedButton = new();
    private readonly Label startFromGeneratedHelperLabel = new();
    private GroupBox? readmeDetailsGroup;
    private readonly TreeView reviewPackageTree = new();
    private readonly SplitContainer reviewPackageSplit = new();
    private readonly TextBox reviewPackageDetailsText = new();
    private readonly ListView reviewCheckList = new();
    private readonly Panel reviewCheckDetailsHostPanel = new();
    private readonly TextBox reviewCheckDetailsText = new();
    private readonly Label reviewStatusLabel = new();
    private readonly Label reviewCheckSummaryLabel = new();
    private readonly Button resetArchiveNameButton = new();
    private readonly Button buildZipButton = new();
    private readonly Panel buildZipButtonHost = new();
    private readonly Button editSelectedReleaseButton = new();
    private readonly Button removeSelectedReleaseButton = new();
    private readonly Button previousStageButton = new();
    private readonly Button nextStageButton = new();
    private readonly Button returnCurrentPackageButton = new();
    private readonly Label lastBuildStatusLabel = new();
    private readonly Button lastBuildOpenFolderButton = new();
    private ToolStripMenuItem? locateMissingFilesMenuItem;
    private TableLayoutPanel? launchContentLayout;
    private Control? launchPrimaryPanel;
    private Control? launchRecentPanel;
    private TableLayoutPanel? launchOuterLayout;
    private TableLayoutPanel? modelMaterialsLayout;
    private Control? returnCurrentPackageHost;
    private Control? workflowNavigationPanel;
    private FlowLayoutPanel? workflowForwardPanel;
    private IReadOnlyList<string> modelFamilyDisplayNames = Array.Empty<string>();
    private int recentGridColumnCount;
    private int recentCardWidth;
    private string? lastBuildOutputFolder;
    private string? reviewSelectedDestination;
    private readonly HashSet<string> reviewExpandedPaths = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<PackagePlanEntry> modelPlanEntries = Array.Empty<PackagePlanEntry>();
    private IReadOnlyList<PackagePlanEntry> primaryModelPlanEntries = Array.Empty<PackagePlanEntry>();
    private string? reviewTopPath;
    private bool hasActiveEditorProject;
    private bool loadingAdditionalReleaseStemText;
    private Guid? editingAdditionalModelId;
    private ModelNameEditTarget activeModelNameEdit = ModelNameEditTarget.None;
    private readonly System.Windows.Forms.Timer modelNamePresentationTimer = new() { Interval = 50 };
    private bool pendingPrimaryModelNamePresentation;
    private bool pendingAdditionalModelNamePresentation;
    private bool restoringAdditionalModelSelection;
    private bool restoringWorkflowTabSelection;
    private int lastWorkflowTabIndex;
    private Func<ReleaseRecord, bool>? confirmReleaseHistoryRemoval;
    private Func<UnsavedWorkAction, UnsavedWorkChoice>? unsavedWorkChoiceProvider = null;
    private Action<string>? duplicateModelFeedback = null;

    public MainForm()
    {
        archiveNameState = new ArchiveNameSuggestionState(archiveNameSuggester);
        confirmReleaseHistoryRemoval = ConfirmReleaseHistoryRemoval;
        materialSourceDiscoveryService = new MaterialSourceDiscoveryService(fileSystem, new MdlV49MetadataReader(fileSystem));
        modelNamePresentationTimer.Tick += (_, _) => FlushPendingModelNamePresentation();
        toolTip.ShowAlways = true;
        Text = "SFM Package Builder";
        Icon = AssetImages.LoadApplicationIcon();
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(980, 700);
        Size = new Size(1180, 820);
        AutoScaleMode = AutoScaleMode.Dpi;

        settings = settingsService.Load().Settings;
        recentProjectsService = new RecentProjectsService(settingsService);

        Controls.Add(editorPanel);
        Controls.Add(launchPanel);
        BuildLaunchPanel();
        BuildEditorPanel();
        ConfigurePathLists();
        RefreshRecentProjects();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (ResolveUnsavedWork(UnsavedWorkAction.Exit) == UnsavedWorkResult.Cancelled)
        {
            e.Cancel = true;
            return;
        }

        base.OnFormClosing(e);
    }

    private BuildCoordinator CreateCoordinator() => new(GetApplicationDataRoot());

    private static string GetApplicationDataRoot() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SfmPackageBuilder");

    private void BuildLaunchPanel()
    {
        var outer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 1,
            RowCount = 1
        };
        launchOuterLayout = outer;
        outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        launchContentLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        launchPrimaryPanel = BuildLaunchPrimaryPanel();
        launchRecentPanel = BuildLaunchRecentPanel();

        outer.Controls.Add(launchContentLayout, 0, 0);
        launchPanel.Controls.Add(outer);
        launchPanel.SizeChanged += (_, _) => UpdateLaunchLayout();
        UpdateLaunchLayout();
    }

    private Control BuildLaunchPrimaryPanel()
    {
        var host = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3
        };
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        host.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        var layout = new FlowLayoutPanel
        {
            AutoSize = true,
            Anchor = AnchorStyles.None,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = false
        };

        var launchLogo = new PictureBox
        {
            Image = AssetImages.LoadLaunchLogo(),
            Width = 400,
            Height = 410,
            SizeMode = PictureBoxSizeMode.Zoom,
            Margin = new Padding(0, 0, 0, 14),
            TabStop = false
        };
        layout.Controls.Add(Centered(launchLogo));

        var subtitle = new Label
        {
            Text = "Build ZIP packages for Source Filmmaker models.",
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(0, 0, 0, 22)
        };
        layout.Controls.Add(Centered(subtitle));

        var newButton = PrimaryButton("&New Package", (_, _) => NewPackage());
        var openButton = new Button { Text = "&Open Package", Width = 220, Height = 36, Margin = new Padding(0, 0, 0, 4) };
        openButton.Click += (_, _) => OpenPackageDialog();
        layout.Controls.Add(Centered(newButton));
        layout.Controls.Add(Centered(openButton));

        returnCurrentPackageButton.Text = "Return to Current Package";
        returnCurrentPackageButton.Width = 220;
        returnCurrentPackageButton.Height = 34;
        returnCurrentPackageButton.Margin = new Padding(0, 10, 0, 4);
        returnCurrentPackageButton.Click += (_, _) => ShowEditor();
        returnCurrentPackageHost = Centered(returnCurrentPackageButton);
        layout.Controls.Add(returnCurrentPackageHost);

        var defaultsButton = new Button
        {
            Text = "Defaults && Preferences...",
            AutoSize = true,
            FlatStyle = FlatStyle.System,
            Margin = new Padding(0, 18, 0, 4)
        };
        defaultsButton.Click += (_, _) => ShowSettings();
        layout.Controls.Add(Centered(defaultsButton));

        var defaultsHelper = new Label
        {
            Text = "Set folders and creator defaults.",
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(0, 0, 0, 18)
        };
        layout.Controls.Add(Centered(defaultsHelper));

        var aboutButton = new Button
        {
            Text = "About",
            AutoSize = true,
            FlatStyle = FlatStyle.System,
            Margin = new Padding(0)
        };
        aboutButton.Click += (_, _) => ShowAbout();
        layout.Controls.Add(Centered(aboutButton));

        host.Controls.Add(layout, 0, 1);
        return host;
    }

    private Control BuildLaunchRecentPanel()
    {
        var host = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 0, 0, 0),
            ColumnCount = 1,
            RowCount = 2,
            BackColor = RecentWorkspaceBackColor
        };
        host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        host.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var recentTitle = new Label
        {
            Text = "Recent Packages",
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = 28,
            Font = new Font(Font.FontFamily, Font.Size, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 8),
            TextAlign = ContentAlignment.MiddleCenter
        };
        host.Controls.Add(recentTitle, 0, 0);

        recentList.AutoSize = false;
        recentList.AutoScroll = true;
        recentList.Margin = new Padding(0);
        recentList.Padding = new Padding(0);
        recentList.BackColor = RecentWorkspaceBackColor;
        recentWorkspacePanel.BackColor = RecentWorkspaceBackColor;
        recentEmptyStateLabel.BackColor = RecentWorkspaceBackColor;
        recentList.Resize += (_, _) => UpdateRecentGridLayout();
        recentWorkspacePanel.Controls.Add(recentList);
        recentWorkspacePanel.Controls.Add(recentEmptyStateLabel);
        host.Controls.Add(recentWorkspacePanel, 0, 1);

        return host;
    }

    private void UpdateLaunchLayout()
    {
        if (launchContentLayout is null || launchPrimaryPanel is null || launchRecentPanel is null)
        {
            return;
        }

        returnCurrentPackageButton.Visible = hasActiveEditorProject;
        if (returnCurrentPackageHost is not null)
        {
            returnCurrentPackageHost.Visible = hasActiveEditorProject;
        }
        launchRecentPanel.Visible = true;
        var stacked = launchPanel.ClientSize.Width is > 0 and < 860;

        launchContentLayout.SuspendLayout();
        launchContentLayout.Controls.Clear();
        launchContentLayout.ColumnStyles.Clear();
        launchContentLayout.RowStyles.Clear();

        if (stacked)
        {
            launchContentLayout.ColumnCount = 1;
            launchContentLayout.RowCount = 2;
            launchContentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            launchContentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
            launchContentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
            launchContentLayout.Controls.Add(launchPrimaryPanel, 0, 0);
            launchContentLayout.Controls.Add(launchRecentPanel, 0, 1);
        }
        else
        {
            launchContentLayout.ColumnCount = 2;
            launchContentLayout.RowCount = 1;
            launchContentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46));
            launchContentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54));
            launchContentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            launchContentLayout.Controls.Add(launchPrimaryPanel, 0, 0);
            launchContentLayout.Controls.Add(launchRecentPanel, 1, 0);
        }

        launchContentLayout.ResumeLayout();
        UpdateRecentGridLayout();
    }

    private void BuildEditorPanel()
    {
        var shell = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3 };
        shell.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        shell.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        shell.Controls.Add(BuildMenu(), 0, 0);

        workflowTabs.TabPages.Add(BuildModelMaterialsTab());
        workflowTabs.TabPages.Add(BuildPackageDetailsTab());
        workflowTabs.TabPages.Add(BuildReadmeTab());
        workflowTabs.TabPages.Add(BuildExtrasTab());
        reviewTab = BuildReviewBuildTab();
        workflowTabs.TabPages.Add(reviewTab);
        workflowTabs.SelectedIndexChanged += (_, _) =>
        {
            if (restoringWorkflowTabSelection)
            {
                return;
            }

            if (!EnsureModelNameEditsCommitted())
            {
                RestoreWorkflowTabSelection();
                return;
            }

            lastWorkflowTabIndex = workflowTabs.SelectedIndex;
            if (workflowTabs.SelectedTab == reviewTab)
            {
                RefreshReviewAndBuild();
            }
            UpdateWorkflowNavigation();
        };
        shell.Controls.Add(workflowTabs, 0, 1);
        shell.Controls.Add(BuildWorkflowNavigation(), 0, 2);
        editorPanel.Controls.Add(shell);
        UpdateWorkflowNavigation();
    }

    private MenuStrip BuildMenu()
    {
        var menu = new MenuStrip();
        var file = new ToolStripMenuItem("&File");
        file.DropDownItems.Add("&New Package", null, (_, _) => NewPackage());
        file.DropDownItems.Add("&Open Package...", null, (_, _) => OpenPackageDialog());
        file.DropDownItems.Add("New From &Current Project", null, (_, _) => NewPackageFromCurrent());
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add("&Save", null, (_, _) => SavePackage());
        file.DropDownItems.Add("Save &As...", null, (_, _) => SavePackageAs());
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add("Return to &Start Screen", null, (_, _) => ShowHome());
        file.DropDownItems.Add(new ToolStripSeparator());
        locateMissingFilesMenuItem = new ToolStripMenuItem("&Locate Missing Files...", null, (_, _) => ShowRecovery(showNoIssuesMessage: true));
        file.DropDownItems.Add(locateMissingFilesMenuItem);
        file.DropDownItems.Add("&Defaults && Preferences...", null, (_, _) => ShowSettings());
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add("E&xit", null, (_, _) => Close());
        file.DropDownOpening += (_, _) =>
        {
            if (locateMissingFilesMenuItem is not null)
            {
                locateMissingFilesMenuItem.Enabled = editorPanel.Visible && HasMissingSources();
            }
        };
        menu.Items.Add(file);
        var help = new ToolStripMenuItem("&Help");
        help.DropDownItems.Add("&About SFM Package Builder", null, (_, _) => ShowAbout());
        menu.Items.Add(help);
        return menu;
    }

    private TabPage BuildModelMaterialsTab()
    {
        var page = new TabPage("1  Model && Materials");
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(12) };
        var layout = new BufferedTableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, RowCount = 2 };
        modelMaterialsLayout = layout;
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(ModelGroup(), 0, 0);
        layout.Controls.Add(MaterialsGroup(), 0, 1);
        layout.SizeChanged += (_, _) => UpdateStageOneListHeights();
        scroll.Controls.Add(layout);
        page.Controls.Add(scroll);
        RegisterModelNameNeutralCommit(page);
        RegisterModelNameNeutralCommit(scroll);
        RegisterModelNameNeutralCommit(layout);
        RegisterStageOnePassiveLabelCommit(layout);
        return page;
    }

    private TabPage BuildPackageDetailsTab()
    {
        var page = new TabPage("2  Release Info");
        var layout = ScrollLayout();
        layout.Controls.Add(PackageDetailsGroup(), 0, 0);
        page.Controls.Add(layout);
        return page;
    }

    private TabPage BuildReadmeTab()
    {
        var page = new TabPage("3  README");
        var layout = ScrollLayout();
        layout.Controls.Add(ReadmeModeGroup(), 0, 0);
        readmeDetailsGroup = ReadmeDetailsGroup();
        layout.Controls.Add(readmeDetailsGroup, 0, 1);
        page.Controls.Add(layout);
        return page;
    }

    private TabPage BuildExtrasTab()
    {
        var page = new TabPage("4  Extras");
        var layout = ScrollLayout();
        layout.Controls.Add(ExtrasGroup(), 0, 0);
        page.Controls.Add(layout);
        return page;
    }

    private TabPage BuildReviewBuildTab()
    {
        var page = new TabPage("5  Review && Build");
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8, 8, 8, 4),
            ColumnCount = 1,
            RowCount = 3
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(ReviewPackageGroup(), 0, 0);
        layout.Controls.Add(ReviewCheckGroup(), 0, 1);
        layout.Controls.Add(OutputGroup(), 0, 2);
        page.Controls.Add(layout);
        return page;
    }

    private Control BuildWorkflowNavigation()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = Scaled(52),
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(Scaled(8), Scaled(8), Scaled(8), Scaled(8))
        };
        workflowNavigationPanel = panel;
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        previousStageButton.AutoSize = true;
        previousStageButton.Margin = new Padding(0);
        previousStageButton.UseMnemonic = false;
        previousStageButton.Click += (_, _) => NavigateStage(-1);
        nextStageButton.AutoSize = true;
        nextStageButton.Margin = new Padding(0);
        nextStageButton.UseMnemonic = false;
        nextStageButton.Click += (_, _) => NavigateStage(1);
        ConfigureBuildZipButton();

        workflowForwardPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = new Padding(0)
        };
        workflowForwardPanel.Controls.Add(buildZipButtonHost);
        workflowForwardPanel.Controls.Add(nextStageButton);

        panel.Controls.Add(previousStageButton, 0, 0);
        panel.Controls.Add(new Panel { Dock = DockStyle.Fill }, 1, 0);
        panel.Controls.Add(workflowForwardPanel, 2, 0);
        return panel;
    }

    private void ConfigureBuildZipButton()
    {
        buildZipButton.Text = "Build ZIP";
        buildZipButton.Width = 160;
        buildZipButton.Height = 38;
        buildZipButton.Margin = new Padding(0);
        buildZipButton.Location = new Point(0, -Scaled(6));
        buildZipButton.BackColor = SystemColors.Highlight;
        buildZipButton.ForeColor = SystemColors.HighlightText;
        buildZipButton.UseVisualStyleBackColor = false;
        buildZipButton.FlatStyle = FlatStyle.Standard;
        buildZipButton.Visible = false;
        buildZipButton.Enabled = false;
        buildZipButton.Click += async (_, _) => await BuildZipAsync();
        buildZipButton.EnabledChanged += (_, _) => UpdateBuildZipButtonAppearance();
        buildZipButtonHost.Width = buildZipButton.Width;
        buildZipButtonHost.Height = buildZipButton.Height;
        buildZipButtonHost.Margin = new Padding(0);
        buildZipButtonHost.Visible = false;
        buildZipButtonHost.Controls.Add(buildZipButton);
        UpdateBuildZipButtonAppearance();
    }

    private void UpdateBuildZipButtonAppearance()
    {
        if (buildZipButton.Enabled)
        {
            buildZipButton.BackColor = SystemColors.Highlight;
            buildZipButton.ForeColor = SystemColors.HighlightText;
        }
        else
        {
            buildZipButton.BackColor = SystemColors.Control;
            buildZipButton.ForeColor = SystemColors.GrayText;
        }

        buildZipButton.UseVisualStyleBackColor = false;
    }

    private void NavigateStage(int direction)
    {
        if (!EnsureModelNameEditsCommitted())
        {
            return;
        }

        var next = workflowTabs.SelectedIndex + direction;
        if (next >= 0 && next < workflowTabs.TabPages.Count)
        {
            workflowTabs.SelectedIndex = next;
        }
    }

    private void RestoreWorkflowTabSelection()
    {
        restoringWorkflowTabSelection = true;
        try
        {
            if (lastWorkflowTabIndex >= 0 && lastWorkflowTabIndex < workflowTabs.TabPages.Count)
            {
                workflowTabs.SelectedIndex = lastWorkflowTabIndex;
            }
        }
        finally
        {
            restoringWorkflowTabSelection = false;
        }

        UpdateWorkflowNavigation();
    }

    private void UpdateWorkflowNavigation()
    {
        if (workflowTabs.TabPages.Count == 0)
        {
            return;
        }

        var index = workflowTabs.SelectedIndex;
        previousStageButton.Visible = index > 0;
        previousStageButton.Text = index > 0 ? "< Back to " + StageTitle(index - 1) : string.Empty;
        nextStageButton.Visible = index < workflowTabs.TabPages.Count - 1;
        nextStageButton.Text = index < workflowTabs.TabPages.Count - 1 ? "Continue to " + StageTitle(index + 1) + " >" : string.Empty;
        var showBuildZip = index == workflowTabs.TabPages.Count - 1;
        buildZipButtonHost.Visible = showBuildZip;
        buildZipButton.Visible = showBuildZip;
        AcceptButton = showBuildZip && buildZipButton.Enabled ? buildZipButton : null;
        UpdateBuildZipButtonAppearance();
    }

    private static string StageTitle(int index) => index switch
    {
        0 => "Model & Materials",
        1 => "Release Info",
        2 => "README",
        3 => "Extras",
        4 => "Review & Build",
        _ => string.Empty
    };

    private static TableLayoutPanel ScrollLayout()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(8),
            ColumnCount = 1,
            RowCount = 6
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 6; i++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        return layout;
    }

    private GroupBox PackageDetailsGroup()
    {
        var group = Group("Release Info");
        var layout = GroupLayout(9);
        AddLabeledText(layout, 0, "&Title", assetNameText);
        AddLabeledText(layout, 1, "&Version", versionText);
        AddLabeledText(layout, 2, "Changelog", changesText, multiline: true);
        var changesHelper = Helper("Describe what changed in this version.");
        layout.Controls.Add(changesHelper, 1, 3);
        layout.SetColumnSpan(changesHelper, 2);
        ConfigureActionButton(addReleaseHistoryButton, "Add to README Changelog", (_, _) => AddCurrentVersionToChangelog());
        addReleaseHistoryButton.Enabled = false;
        layout.Controls.Add(addReleaseHistoryButton, 1, 4);
        layout.SetColumnSpan(addReleaseHistoryButton, 2);
        editSelectedReleaseButton.Text = "Edit Selected...";
        editSelectedReleaseButton.AutoSize = true;
        editSelectedReleaseButton.Enabled = false;
        editSelectedReleaseButton.Click += (_, _) => EditSelectedReleaseHistory();
        removeSelectedReleaseButton.Text = "Remove Selected";
        removeSelectedReleaseButton.AutoSize = true;
        removeSelectedReleaseButton.Enabled = false;
        removeSelectedReleaseButton.Click += (_, _) => RemoveSelectedReleaseHistory();
        var changelogHeading = SubsectionLabel("README changelog");
        layout.Controls.Add(changelogHeading, 0, 5);
        layout.SetColumnSpan(changelogHeading, 3);
        var historyButtons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        historyButtons.Controls.Add(editSelectedReleaseButton);
        historyButtons.Controls.Add(removeSelectedReleaseButton);
        layout.Controls.Add(historyButtons, 0, 6);
        layout.SetColumnSpan(historyButtons, 3);
        releaseHistoryList.Dock = DockStyle.Fill;
        releaseHistoryList.Height = 110;
        releaseHistoryList.View = View.Details;
        releaseHistoryList.FullRowSelect = true;
        releaseHistoryList.HideSelection = false;
        releaseHistoryList.Columns.Add("Version", 160);
        releaseHistoryList.Columns.Add("Changes", 680);
        releaseHistoryList.SelectedIndexChanged += (_, _) => UpdateReleaseHistoryButtons();
        layout.Controls.Add(releaseHistoryList, 0, 7);
        layout.SetColumnSpan(releaseHistoryList, 3);
        var historyHelper = Helper("Select a changelog entry to edit or remove it.");
        layout.Controls.Add(historyHelper, 0, 8);
        layout.SetColumnSpan(historyHelper, 3);
        group.Controls.Add(layout);

        assetNameText.TextChanged += (_, _) => { SuggestArchiveNameIfSafe(); MarkDirty(); };
        versionText.TextChanged += (_, _) => { SuggestArchiveNameIfSafe(); MarkDirty(); };
        changesText.Multiline = true;
        changesText.ScrollBars = ScrollBars.Vertical;
        changesText.Height = 70;
        changesText.TextChanged += (_, _) => { UpdateReleaseHistoryAddButton(); MarkDirty(); };
        return group;
    }

    private GroupBox ModelGroup()
    {
        var group = Group("Model");
        var layout = GroupLayout(13);
        AddPathRow(layout, 0, "&Source", primaryModelText, BrowsePrimaryModel);
        ConfigureHelperLabel(sourceInstructionLabel, "Choose the .mdl file you want to package.");
        layout.Controls.Add(sourceInstructionLabel, 1, 1);
        layout.SetColumnSpan(sourceInstructionLabel, 2);
        AddLabeledText(layout, 2, "&Model name in ZIP", releaseStemText);
        releaseStemText.Enabled = false;
        releaseStemValidationLabel.AutoSize = true;
        releaseStemValidationLabel.Anchor = AnchorStyles.Left;
        releaseStemValidationLabel.MaximumSize = new Size(760, 0);
        releaseStemValidationLabel.ForeColor = Color.Firebrick;
        releaseStemValidationLabel.Visible = false;
        layout.Controls.Add(releaseStemValidationLabel, 1, 3);
        layout.SetColumnSpan(releaseStemValidationLabel, 2);
        renamePreviewLabel.AutoSize = true;
        renamePreviewLabel.Anchor = AnchorStyles.Left;
        renamePreviewLabel.MaximumSize = new Size(760, 0);
        renamePreviewLabel.Margin = new Padding(0, 2, 0, 6);
        renamePreviewLabel.ForeColor = SystemColors.GrayText;
        renamePreviewLabel.Visible = false;
        layout.Controls.Add(renamePreviewLabel, 1, 4);
        layout.SetColumnSpan(renamePreviewLabel, 2);

        modelFamilySummaryLabel.AutoSize = true;
        modelFamilySummaryLabel.Text = string.Empty;
        modelFamilySummaryLabel.Visible = false;
        modelFamilySummaryLabel.Margin = new Padding(0, 8, 0, 2);
        layout.Controls.Add(modelFamilySummaryLabel, 1, 5);
        layout.SetColumnSpan(modelFamilySummaryLabel, 2);
        modelFamilyNamesLabel.AutoSize = false;
        modelFamilyNamesLabel.Dock = DockStyle.Fill;
        modelFamilyNamesLabel.Height = Scaled(38);
        modelFamilyNamesLabel.Margin = new Padding(0, 0, 0, 8);
        modelFamilyNamesLabel.ForeColor = SystemColors.ControlText;
        modelFamilyNamesLabel.Text = string.Empty;
        modelFamilyNamesLabel.UseMnemonic = false;
        modelFamilyNamesLabel.AutoEllipsis = true;
        modelFamilyNamesLabel.Visible = false;
        layout.Controls.Add(modelFamilyNamesLabel, 1, 6);
        layout.SetColumnSpan(modelFamilyNamesLabel, 2);

        additionalModelsList.Dock = DockStyle.Fill;
        additionalModelsList.Height = Scaled(58);
        additionalModelsList.View = View.Details;
        additionalModelsList.FullRowSelect = true;
        additionalModelsList.MultiSelect = false;
        additionalModelsList.HideSelection = false;
        additionalModelsList.HeaderStyle = ColumnHeaderStyle.Nonclickable;
        additionalModelsList.Columns.Add("Model", 260);
        additionalModelsList.Columns.Add("Name in ZIP", 300);
        additionalModelsList.Columns.Add("Related files", 120, HorizontalAlignment.Right);
        additionalModelsList.Resize += (_, _) => UpdateAdditionalModelColumns();
        additionalModelsList.SelectedIndexChanged += (_, _) =>
        {
            if (restoringAdditionalModelSelection)
            {
                return;
            }

            if (activeModelNameEdit == ModelNameEditTarget.Additional && editingAdditionalModelId is not null)
            {
                var selectedModel = SelectedAdditionalModel();
                if (selectedModel?.Id != editingAdditionalModelId.Value && !TryCommitAdditionalReleaseStemText(finishEditingOnSuccess: false))
                {
                    RestoreAdditionalModelSelection(editingAdditionalModelId.Value);
                    return;
                }
            }

            LoadSelectedAdditionalModelReleaseStemEditor();
            UpdateAdditionalModelCompanionDetail();
            UpdateAdditionalModelButtons();
        };
        additionalModelsSummaryLabel.Text = "Additional models (0)";
        additionalModelsSummaryLabel.AutoSize = true;
        additionalModelsSummaryLabel.Anchor = AnchorStyles.Left;
        var additionalHeader = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, ColumnCount = 2, Margin = new Padding(0, 8, 0, 3) };
        additionalHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        additionalHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        additionalModelsSummaryLabel.Margin = new Padding(4, 4, 4, 4);
        additionalHeader.Controls.Add(additionalModelsSummaryLabel, 0, 0);
        var modelButtons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = Padding.Empty };
        ConfigureActionButton(addAdditionalModelButton, "Add Model...", (_, _) => AddAdditionalModel());
        ConfigureActionButton(removeAdditionalModelButton, "Remove", (_, _) => RemoveAdditionalModel());
        removeAdditionalModelButton.Enabled = false;
        modelButtons.Controls.Add(addAdditionalModelButton);
        modelButtons.Controls.Add(removeAdditionalModelButton);
        additionalHeader.Controls.Add(modelButtons, 1, 0);
        layout.Controls.Add(additionalHeader, 0, 7);
        layout.SetColumnSpan(additionalHeader, 3);
        layout.Controls.Add(additionalModelsList, 0, 8);
        layout.SetColumnSpan(additionalModelsList, 3);
        additionalReleaseStemLabel.Text = "Model name in ZIP";
        additionalReleaseStemLabel.AutoSize = true;
        additionalReleaseStemLabel.Anchor = AnchorStyles.Left;
        additionalReleaseStemLabel.Visible = false;
        layout.Controls.Add(additionalReleaseStemLabel, 0, 9);
        additionalReleaseStemText.Dock = DockStyle.Fill;
        additionalReleaseStemText.Visible = false;
        layout.Controls.Add(additionalReleaseStemText, 1, 9);
        layout.SetColumnSpan(additionalReleaseStemText, 2);
        additionalReleaseStemValidationLabel.AutoSize = true;
        additionalReleaseStemValidationLabel.Anchor = AnchorStyles.Left;
        additionalReleaseStemValidationLabel.MaximumSize = new Size(760, 0);
        additionalReleaseStemValidationLabel.ForeColor = Color.Firebrick;
        additionalReleaseStemValidationLabel.Visible = false;
        layout.Controls.Add(additionalReleaseStemValidationLabel, 1, 10);
        layout.SetColumnSpan(additionalReleaseStemValidationLabel, 2);
        additionalRenamePreviewLabel.AutoSize = true;
        additionalRenamePreviewLabel.Anchor = AnchorStyles.Left;
        additionalRenamePreviewLabel.MaximumSize = new Size(760, 0);
        additionalRenamePreviewLabel.Margin = new Padding(0, 2, 0, 6);
        additionalRenamePreviewLabel.ForeColor = SystemColors.GrayText;
        additionalRenamePreviewLabel.Visible = false;
        layout.Controls.Add(additionalRenamePreviewLabel, 1, 11);
        layout.SetColumnSpan(additionalRenamePreviewLabel, 2);
        additionalModelCompanionDetailLabel.AutoSize = false;
        additionalModelCompanionDetailLabel.Height = Scaled(26);
        additionalModelCompanionDetailLabel.Dock = DockStyle.Fill;
        additionalModelCompanionDetailLabel.Margin = new Padding(0, 1, 4, 1);
        additionalModelCompanionDetailLabel.ForeColor = SystemColors.GrayText;
        additionalModelCompanionDetailLabel.Visible = true;
        layout.Controls.Add(additionalModelCompanionDetailLabel, 1, 12);
        layout.SetColumnSpan(additionalModelCompanionDetailLabel, 2);
        group.Controls.Add(layout);
        primaryModelText.TextChanged += (_, _) =>
        {
            UpdatePrimaryModelNameState(clearWhenNoSource: !loadingControls);
            if (!loadingControls)
            {
                RefreshModelFamilyInfo();
            }
            MarkDirty();
        };
        releaseStemText.Enter += (_, _) => activeModelNameEdit = ModelNameEditTarget.Primary;
        releaseStemText.KeyDown += ModelNameTextBoxKeyDown;
        releaseStemText.TextChanged += (_, _) => PrimaryReleaseStemTextChanged();
        releaseStemText.Leave += (_, _) => TryCommitPrimaryReleaseStemText(finishEditingOnSuccess: false);
        additionalReleaseStemText.Enter += (_, _) => activeModelNameEdit = ModelNameEditTarget.Additional;
        additionalReleaseStemText.KeyDown += ModelNameTextBoxKeyDown;
        additionalReleaseStemText.TextChanged += (_, _) => AdditionalReleaseStemTextChanged();
        additionalReleaseStemText.Leave += (_, _) => TryCommitAdditionalReleaseStemText(finishEditingOnSuccess: false);
        RegisterModelNameNeutralCommit(group);
        RegisterModelNameNeutralCommit(layout);
        return group;
    }

    private GroupBox MaterialsGroup()
    {
        var group = Group("Materials && Textures");
        var layout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, RowCount = 3, Font = Font, Padding = new Padding(4, 0, Scaled(24), 0) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var materialsHelper = Helper("Materials found automatically appear here.");
        layout.Controls.Add(materialsHelper, 0, 0);
        var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        buttons.Controls.Add(ActionButton("Add Folder...", (_, _) => AddMaterialFolder()));
        buttons.Controls.Add(ActionButton("Add Files...", (_, _) => AddMaterialFiles()));
        buttons.Controls.Add(ActionButton("Remove Selection", (_, _) => RemoveSelectedMaterial()));
        layout.Controls.Add(buttons, 0, 1);
        materialsList.Dock = DockStyle.Fill;
        materialsList.IntegralHeight = false;
        materialsList.MinimumSize = new Size(0, Scaled(132));
        toolTip.SetToolTip(materialsList, "Selected material recipes. Package Preview shows expanded contents.");
        layout.Controls.Add(materialsList, 0, 2);
        group.Controls.Add(layout);
        return group;
    }

    private GroupBox ReadmeModeGroup()
    {
        var group = Group("README");
        var layout = GroupLayout(12);
        readmeGeneratedRadio.Text = "Generate a README for me";
        readmeCustomRadio.Text = "Write my own README";
        readmeImportRadio.Text = "Import an existing README file";
        readmeNoneRadio.Text = "No README";
        readmeGeneratedRadio.AutoSize = true;
        readmeCustomRadio.AutoSize = true;
        readmeImportRadio.AutoSize = true;
        readmeNoneRadio.AutoSize = true;
        layout.Controls.Add(readmeGeneratedRadio, 1, 0);
        var generatedModeHelper = Helper("Uses your Release Info and README details.");
        layout.Controls.Add(generatedModeHelper, 1, 1);
        layout.SetColumnSpan(generatedModeHelper, 2);
        layout.Controls.Add(readmeCustomRadio, 1, 2);
        var customModeHelper = Helper("Write it yourself, or generate a starting draft to edit.");
        layout.Controls.Add(customModeHelper, 1, 3);
        layout.SetColumnSpan(customModeHelper, 2);
        layout.Controls.Add(readmeImportRadio, 1, 4);
        layout.Controls.Add(readmeNoneRadio, 1, 5);

        noReadmeStateLabel.Text = "No README.txt will be included in this package.";
        noReadmeStateLabel.AutoSize = true;
        noReadmeStateLabel.ForeColor = SystemColors.GrayText;
        noReadmeStateLabel.Visible = false;
        layout.Controls.Add(noReadmeStateLabel, 1, 6);
        layout.SetColumnSpan(noReadmeStateLabel, 2);

        importedReadmeLabel.Text = "README file";
        importedReadmeLabel.AutoSize = true;
        importedReadmeLabel.Anchor = AnchorStyles.Left;
        layout.Controls.Add(importedReadmeLabel, 0, 7);
        importedReadmeText.Dock = DockStyle.Fill;
        layout.Controls.Add(importedReadmeText, 1, 7);
        importedReadmeBrowseButton.Text = "Browse...";
        importedReadmeBrowseButton.AutoSize = true;
        importedReadmeBrowseButton.Click += (_, _) => BrowseImportedReadme(importedReadmeText);
        layout.Controls.Add(importedReadmeBrowseButton, 2, 7);
        importedReadmeHelperLabel.Text = "The selected file will be copied into the package unchanged.";
        importedReadmeHelperLabel.AutoSize = true;
        importedReadmeHelperLabel.Anchor = AnchorStyles.Left;
        importedReadmeHelperLabel.MaximumSize = new Size(760, 0);
        importedReadmeHelperLabel.ForeColor = SystemColors.GrayText;
        layout.Controls.Add(importedReadmeHelperLabel, 1, 8);
        layout.SetColumnSpan(importedReadmeHelperLabel, 2);

        customReadmeText.Multiline = true;
        customReadmeText.ScrollBars = ScrollBars.Vertical;
        customReadmeText.Dock = DockStyle.Fill;
        customReadmeText.Height = 300;
        customReadmeLabel.Text = "README contents";
        customReadmeLabel.AutoSize = true;
        customReadmeLabel.Anchor = AnchorStyles.Left;
        customReadmeLabel.Font = new Font(Font, FontStyle.Bold);
        startFromGeneratedButton.Text = "Generate Starting Draft";
        startFromGeneratedButton.AutoSize = true;
        toolTip.SetToolTip(startFromGeneratedButton, "Uses your Release Info and README details.");
        startFromGeneratedButton.Click += (_, _) => StartCustomReadmeFromGenerated();
        customReadmeHeaderPanel.Dock = DockStyle.Top;
        customReadmeHeaderPanel.AutoSize = true;
        customReadmeHeaderPanel.ColumnCount = 2;
        customReadmeHeaderPanel.RowCount = 1;
        customReadmeHeaderPanel.Margin = new Padding(0, 8, 0, 3);
        customReadmeHeaderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        customReadmeHeaderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        customReadmeHeaderPanel.Controls.Add(customReadmeLabel, 0, 0);
        customReadmeHeaderPanel.Controls.Add(startFromGeneratedButton, 1, 0);
        layout.Controls.Add(customReadmeHeaderPanel, 1, 9);
        layout.SetColumnSpan(customReadmeHeaderPanel, 2);
        layout.Controls.Add(customReadmeText, 1, 10);
        layout.SetColumnSpan(customReadmeText, 2);
        customReadmeHelperLabel.Text = "This text will be saved as README.txt.";
        customReadmeHelperLabel.AutoSize = true;
        customReadmeHelperLabel.Anchor = AnchorStyles.Left;
        customReadmeHelperLabel.MaximumSize = new Size(760, 0);
        customReadmeHelperLabel.ForeColor = SystemColors.GrayText;
        layout.Controls.Add(customReadmeHelperLabel, 1, 11);
        layout.SetColumnSpan(customReadmeHelperLabel, 2);
        group.Controls.Add(layout);

        readmeGeneratedRadio.CheckedChanged += (_, _) => { UpdateReadmeModeControls(); MarkDirty(); };
        readmeCustomRadio.CheckedChanged += (_, _) => { UpdateReadmeModeControls(); MarkDirty(); };
        readmeImportRadio.CheckedChanged += (_, _) => { UpdateReadmeModeControls(); MarkDirty(); };
        readmeNoneRadio.CheckedChanged += (_, _) => { UpdateReadmeModeControls(); MarkDirty(); };
        importedReadmeText.TextChanged += (_, _) => MarkDirty();
        customReadmeText.TextChanged += (_, _) => MarkDirty();
        return group;
    }

    private GroupBox ReadmeDetailsGroup()
    {
        var group = Group("README Details");
        var layout = GroupLayout(13);
        AddAboveLabeledText(layout, 0, 1, "Description", readmeDescriptionText, multiline: true);
        AddLabeledText(layout, 2, "Author", readmeAuthorText);
        AddLabeledText(layout, 3, "Website", readmeWebsiteText);
        AddAboveLabeledText(layout, 4, 5, "Credits", creditsText);
        var creditsHelper = Helper("Credit the original creators, porters, or contributors.");
        layout.Controls.Add(creditsHelper, 0, 6);
        layout.SetColumnSpan(creditsHelper, 3);
        AddAboveLabeledText(layout, 7, 8, "Usage terms", readmeLicenseText);
        var usageHelper = Helper("State how you want others to use or redistribute the model. Examples: CC0 1.0 or Do not redistribute.");
        layout.Controls.Add(usageHelper, 0, 9);
        layout.SetColumnSpan(usageHelper, 3);
        AddAboveLabeledText(layout, 10, 11, "Additional links && resources", readmeResourcesText, multiline: true);
        readmeResourcesText.Height = Scaled(44);
        var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        buttons.Controls.Add(ActionButton("Preview README", (_, _) => PreviewReadme()));
        layout.Controls.Add(buttons, 1, 12);
        group.Controls.Add(layout);
        foreach (var control in new Control[] { readmeDescriptionText, readmeAuthorText, readmeWebsiteText, creditsText, readmeLicenseText, readmeResourcesText })
        {
            control.TextChanged += (_, _) => MarkDirty();
        }
        return group;
    }

    private GroupBox ExtrasGroup()
    {
        var group = Group("Extras");
        var layout = GroupLayout(8);
        var extrasIntro = Helper("Add optional files such as rigs, configs, or documentation.");
        extrasIntro.Margin = new Padding(4, 3, 4, 10);
        layout.Controls.Add(extrasIntro, 0, 0);
        layout.SetColumnSpan(extrasIntro, 3);
        var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 6) };
        buttons.Controls.Add(ActionButton("Add File...", (_, _) => AddExtraFile()));
        buttons.Controls.Add(ActionButton("Add Folder...", (_, _) => AddExtraFolder()));
        buttons.Controls.Add(ActionButton("Remove Selection", (_, _) => RemoveSelectedExtra()));
        layout.Controls.Add(buttons, 1, 1);
        extrasList.Dock = DockStyle.Fill;
        extrasList.Height = 165;
        extrasList.View = View.Details;
        extrasList.FullRowSelect = true;
        extrasList.HideSelection = false;
        extrasList.GridLines = true;
        extrasList.Columns.Add("File", 520);
        extrasList.Columns.Add("Package location", 220);
        extrasList.SelectedIndexChanged += (_, _) => LoadSelectedExtraDestination();
        extrasList.Resize += (_, _) => UpdateExtrasColumns();
        ConfigureExtrasHeaderDrawing();
        layout.Controls.Add(extrasList, 1, 2);
        layout.SetColumnSpan(extrasList, 2);
        extraDestinationCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        extraDestinationCombo.Enabled = false;
        extraDestinationCombo.Items.AddRange(extraDestinationPresenter.Choices.Cast<object>().ToArray());
        extraDestinationCombo.SelectedIndexChanged += (_, _) => SaveSelectedExtraDestination();
        layout.Controls.Add(new Label { Text = "Location for selected file", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 3);
        layout.Controls.Add(extraDestinationCombo, 1, 3);
        extraSelectionHelpLabel.Text = "Select a file in the table to set its location.";
        extraSelectionHelpLabel.AutoSize = true;
        extraSelectionHelpLabel.ForeColor = SystemColors.GrayText;
        layout.Controls.Add(extraSelectionHelpLabel, 1, 4);
        layout.SetColumnSpan(extraSelectionHelpLabel, 2);
        extraCustomDestinationLabel.Text = "Folder inside ZIP";
        extraCustomDestinationLabel.AutoSize = true;
        extraCustomDestinationLabel.Anchor = AnchorStyles.Left;
        layout.Controls.Add(extraCustomDestinationLabel, 0, 5);
        extraCustomDestinationText.Dock = DockStyle.Fill;
        layout.Controls.Add(extraCustomDestinationText, 1, 5);
        layout.SetColumnSpan(extraCustomDestinationText, 2);
        extraCustomDestinationText.Enabled = false;
        extraCustomDestinationHelperLabel.Text = "Examples: cfg\\ or scripts\\sfm\\animset\\.";
        extraCustomDestinationHelperLabel.AutoSize = true;
        extraCustomDestinationHelperLabel.Anchor = AnchorStyles.Left;
        extraCustomDestinationHelperLabel.ForeColor = SystemColors.GrayText;
        layout.Controls.Add(extraCustomDestinationHelperLabel, 1, 6);
        layout.SetColumnSpan(extraCustomDestinationHelperLabel, 2);
        extraCustomDestinationText.TextChanged += (_, _) => SaveSelectedExtraDestination();
        group.Controls.Add(layout);
        return group;
    }

    private GroupBox ReviewPackageGroup()
    {
        var group = Group("Package Contents");
        group.Dock = DockStyle.Fill;
        group.AutoSize = false;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = false, ColumnCount = 1, RowCount = 2, Font = Font, Padding = new Padding(4, 0, Scaled(24), 0), Margin = new Padding(0, 0, 0, 2) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        reviewStatusLabel.AutoSize = true;
        reviewStatusLabel.Text = string.Empty;
        reviewStatusLabel.Visible = false;
        layout.Controls.Add(reviewStatusLabel, 0, 0);

        reviewPackageSplit.Dock = DockStyle.Fill;
        reviewPackageSplit.Orientation = Orientation.Vertical;
        reviewPackageSplit.Panel1MinSize = 1;
        reviewPackageSplit.Panel2MinSize = 1;
        reviewPackageSplit.SizeChanged += (_, _) => ConfigureReviewPackageSplitDistance();
        reviewPackageTree.Dock = DockStyle.Fill;
        reviewPackageTree.HideSelection = false;
        reviewPackageTree.AfterSelect += (_, _) =>
        {
            reviewSelectedDestination = GetSelectedReviewDestination();
            ShowReviewPackageDetail();
        };
        reviewPackageTree.AfterExpand += (_, _) => RememberReviewExpansionState();
        reviewPackageTree.AfterCollapse += (_, _) => RememberReviewExpansionState();
        reviewPackageSplit.Panel1.Controls.Add(reviewPackageTree);

        reviewPackageDetailsText.Dock = DockStyle.Fill;
        reviewPackageDetailsText.Multiline = true;
        reviewPackageDetailsText.ReadOnly = true;
        reviewPackageDetailsText.ScrollBars = ScrollBars.Both;
        reviewPackageDetailsText.Text = "Select a file to see its source and package destination.";
        reviewPackageSplit.Panel2.Controls.Add(reviewPackageDetailsText);
        layout.Controls.Add(reviewPackageSplit, 0, 1);
        group.Controls.Add(layout);
        return group;
    }

    private GroupBox ReviewCheckGroup()
    {
        var group = Group("Build Check");
        var layout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, RowCount = 3, Font = Font, Padding = new Padding(4, 0, Scaled(24), 0), Margin = new Padding(0, 0, 0, 2) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        reviewCheckSummaryLabel.AutoSize = true;
        reviewCheckSummaryLabel.Margin = new Padding(4, 3, 4, 6);
        layout.Controls.Add(reviewCheckSummaryLabel, 0, 0);

        reviewCheckList.Dock = DockStyle.Top;
        reviewCheckList.Height = 96;
        reviewCheckList.View = View.Details;
        reviewCheckList.FullRowSelect = true;
        reviewCheckList.HideSelection = false;
        reviewCheckList.Columns.Add("Status", 110);
        reviewCheckList.Columns.Add("Message", 760);
        reviewCheckList.Resize += (_, _) => UpdateReviewCheckColumns();
        reviewCheckList.SelectedIndexChanged += (_, _) => reviewCheckDetailsText.Text = reviewCheckList.SelectedItems.Count == 0
            ? string.Empty
            : (reviewCheckList.SelectedItems[0].Tag as string) ?? string.Empty;
        layout.Controls.Add(reviewCheckList, 0, 1);

        reviewCheckDetailsHostPanel.Dock = DockStyle.Top;
        reviewCheckDetailsHostPanel.Height = 52;
        reviewCheckDetailsHostPanel.Resize += (_, _) => ConfigureReviewCheckDetailsWidth();
        reviewCheckDetailsText.Dock = DockStyle.None;
        reviewCheckDetailsText.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom;
        reviewCheckDetailsText.Location = Point.Empty;
        reviewCheckDetailsText.Multiline = true;
        reviewCheckDetailsText.ReadOnly = true;
        reviewCheckDetailsText.ScrollBars = ScrollBars.Both;
        reviewCheckDetailsText.Height = reviewCheckDetailsHostPanel.Height;
        reviewCheckDetailsHostPanel.Controls.Add(reviewCheckDetailsText);
        layout.Controls.Add(reviewCheckDetailsHostPanel, 0, 2);
        group.Controls.Add(layout);
        return group;
    }

    private GroupBox OutputGroup()
    {
        var group = Group("Output");
        var layout = GroupLayout(4);
        AddPathRow(layout, 0, "Output folder", outputFolderText, BrowseOutputFolder);
        AddLabeledText(layout, 1, "ZIP filename", archiveNameText);
        resetArchiveNameButton.Text = "Reset to suggested name";
        resetArchiveNameButton.AutoSize = true;
        resetArchiveNameButton.Click += (_, _) => ResetArchiveNameSuggestion();
        layout.Controls.Add(resetArchiveNameButton, 1, 2);
        layout.SetColumnSpan(resetArchiveNameButton, 2);
        var lastBuildPanel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = false };
        lastBuildStatusLabel.AutoSize = true;
        lastBuildStatusLabel.ForeColor = Color.DarkGreen;
        lastBuildStatusLabel.Visible = false;
        lastBuildOpenFolderButton.Text = "Open folder";
        lastBuildOpenFolderButton.AutoSize = true;
        lastBuildOpenFolderButton.Visible = false;
        lastBuildOpenFolderButton.Click += (_, _) => OpenLastBuildFolder();
        lastBuildPanel.Controls.Add(lastBuildStatusLabel);
        lastBuildPanel.Controls.Add(lastBuildOpenFolderButton);
        layout.Controls.Add(lastBuildPanel, 1, 3);
        layout.SetColumnSpan(lastBuildPanel, 2);
        group.Controls.Add(layout);

        archiveNameText.TextChanged += (_, _) =>
        {
            if (!loadingControls && !updatingArchiveSuggestion)
            {
                archiveNameState.MarkManualEdit(archiveNameText.Text);
                resetArchiveNameButton.Enabled = true;
            }
            MarkDirty();
        };
        outputFolderText.TextChanged += (_, _) => MarkDirty();
        return group;
    }

    private GroupBox Group(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Top,
        AutoSize = true,
        Padding = new Padding(8),
        Margin = new Padding(0, 0, 0, 8),
        Font = new Font(Font, FontStyle.Bold)
    };

    private TableLayoutPanel GroupLayout(int rows)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 3,
            RowCount = rows,
            Padding = new Padding(4, 0, Scaled(24), 0),
            Font = Font
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Scaled(150)));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        return layout;
    }

    private Label SubsectionLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        Font = Font,
        Margin = new Padding(4, 8, 4, 3)
    };

    private static Button ActionButton(string text, EventHandler handler)
    {
        var button = new Button { Text = text, AutoSize = true, Margin = new Padding(4), Padding = new Padding(8, 3, 8, 3) };
        button.Click += handler;
        return button;
    }

    private static void ConfigureActionButton(Button button, string text, EventHandler handler)
    {
        button.Text = text;
        button.AutoSize = true;
        button.Margin = new Padding(4);
        button.Padding = new Padding(8, 3, 8, 3);
        button.Click += handler;
    }

    private static Button PrimaryButton(string text, EventHandler handler)
    {
        var button = new Button
        {
            Text = text,
            Width = 220,
            Height = 40,
            Margin = new Padding(0, 0, 0, 8),
            BackColor = SystemColors.Highlight,
            ForeColor = SystemColors.HighlightText,
            FlatStyle = FlatStyle.Standard,
            UseVisualStyleBackColor = false
        };
        button.Click += handler;
        return button;
    }

    private static Label Helper(string text)
    {
        var label = new Label();
        ConfigureHelperLabel(label, text);
        return label;
    }

    private static void ConfigureHelperLabel(Label label, string text)
    {
        label.Text = text;
        label.AutoSize = true;
        label.Anchor = AnchorStyles.Left;
        label.MaximumSize = new Size(760, 0);
        label.ForeColor = SystemColors.GrayText;
    }

    private static Control Centered(Control control)
    {
        var panel = new Panel
        {
            Width = control.Width,
            Height = control.Height + control.Margin.Vertical,
            Margin = new Padding(0),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        panel.MinimumSize = new Size(control.Width, 0);

        void Recenter()
        {
            panel.Height = control.Height + control.Margin.Vertical;
            control.Location = new Point(
                Math.Max(0, (panel.ClientSize.Width - control.Width) / 2),
                control.Margin.Top);
        }

        panel.Controls.Add(control);
        control.SizeChanged += (_, _) => Recenter();
        panel.SizeChanged += (_, _) => Recenter();
        Recenter();
        return panel;
    }

    private static Label EmptyState(string text) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = SystemColors.GrayText,
        Padding = new Padding(4)
    };

    private static void AddLabeledText(TableLayoutPanel layout, int row, string label, TextBox textBox, bool multiline = false)
    {
        layout.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        textBox.Dock = DockStyle.Fill;
        textBox.Multiline = multiline;
        textBox.ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None;
        if (multiline) textBox.Height = 48;
        layout.Controls.Add(textBox, 1, row);
        layout.SetColumnSpan(textBox, 2);
    }

    private void AddAboveLabeledText(TableLayoutPanel layout, int labelRow, int controlRow, string label, TextBox textBox, bool multiline = false)
    {
        var heading = SubsectionLabel(label);
        heading.Margin = new Padding(4, 8, 4, 2);
        layout.Controls.Add(heading, 0, labelRow);
        layout.SetColumnSpan(heading, 3);
        textBox.Dock = DockStyle.Fill;
        textBox.Multiline = multiline;
        textBox.ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None;
        if (multiline) textBox.Height = Scaled(64);
        layout.Controls.Add(textBox, 0, controlRow);
        layout.SetColumnSpan(textBox, 3);
    }

    private static void AddPathRow(TableLayoutPanel layout, int row, string label, TextBox textBox, Action<TextBox> browse)
    {
        layout.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        textBox.Dock = DockStyle.Fill;
        layout.Controls.Add(textBox, 1, row);
        var button = new Button { Text = "Browse...", AutoSize = true };
        button.Click += (_, _) => browse(textBox);
        layout.Controls.Add(button, 2, row);
    }

    private bool NewPackage()
    {
        return StartNewProject(UnsavedWorkAction.NewProject, () => projectCloneService.CreateNewProject(settings), dirtyAfterCommit: false);
    }

    private bool NewPackageFromCurrent()
    {
        if (!EnsureModelNameEditsCommitted())
        {
            return false;
        }

        SyncProjectFromControls();
        var outputFolder = outputFolderText.Text;
        return StartNewProject(
            UnsavedWorkAction.NewFromCurrentProject,
            () => projectCloneService.CreateFromCurrent(project),
            dirtyAfterCommit: true,
            preservedOutputDirectory: outputFolder);
    }

    private bool StartNewProject(
        UnsavedWorkAction action,
        Func<PackageProject> createProject,
        bool dirtyAfterCommit,
        string? preservedOutputDirectory = null)
    {
        if (ResolveUnsavedWork(action) == UnsavedWorkResult.Cancelled)
        {
            return false;
        }

        var replacement = createProject();
        CommitProjectSession(replacement, null, dirtyAfterCommit, preservedOutputDirectory);
        return true;
    }

    private void OpenPackageDialog()
    {
        if (ResolveUnsavedWork(UnsavedWorkAction.OpenProject) == UnsavedWorkResult.Cancelled) return;
        using var dialog = new OpenFileDialog
        {
            Filter = "SFM Package Builder project|*.sfmpack|All files|*.*",
            InitialDirectory = GetProjectOpenStartDirectory()
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            OpenPackage(dialog.FileName);
        }
    }

    private void OpenPackage(string path)
    {
        var result = projectSerializer.Load(path);
        if (!result.IsSuccess)
        {
            MessageBox.Show(this, result.Message ?? "The package project could not be opened.", "Open Package", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        CommitProjectSession(result.Project!, path, dirtyAfterCommit: false);
        TrackRecent(path);
        TrackProjectDirectory(path);
        RunRecoveryIfNeeded();
    }

    private bool TryLoadPackage(string path, out PackageProject? loadedProject)
    {
        var result = projectSerializer.Load(path);
        if (!result.IsSuccess)
        {
            loadedProject = null;
            MessageBox.Show(this, result.Message ?? "The package project could not be opened.", "Open Package", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        loadedProject = result.Project!;
        return true;
    }

    private void OpenPackageWithLifecycle(string path)
    {
        if (ResolveUnsavedWork(UnsavedWorkAction.OpenProject) == UnsavedWorkResult.Cancelled) return;
        OpenPackage(path);
    }

    private void CommitProjectSession(PackageProject replacement, string? replacementPath, bool dirtyAfterCommit, string? preservedOutputDirectory = null)
    {
        using (SuspendEditorRedraw())
        {
            project = replacement;
            projectPath = replacementPath;
            archiveNameState.LoadExisting(project.ArchiveName);
            LoadProjectIntoControls();
            if (preservedOutputDirectory is not null)
            {
                loadingControls = true;
                outputFolderText.Text = preservedOutputDirectory;
                loadingControls = false;
            }

            ResetProjectSessionUiState();
            SetDirty(dirtyAfterCommit);
            ShowEditor();
        }
    }

    private void ResetProjectSessionUiState()
    {
        modelNamePresentationTimer.Stop();
        pendingPrimaryModelNamePresentation = false;
        pendingAdditionalModelNamePresentation = false;
        activeModelNameEdit = ModelNameEditTarget.None;
        if (workflowTabs.TabPages.Count > 0)
        {
            workflowTabs.SelectedIndex = 0;
            lastWorkflowTabIndex = 0;
            ScrollControlToStart(workflowTabs.TabPages[0]);
        }

        reviewStatusLabel.Text = string.Empty;
        reviewPackageTree.Nodes.Clear();
        reviewPackageDetailsText.Text = "Select a file to see its source and package destination.";
        reviewCheckList.Items.Clear();
        reviewCheckSummaryLabel.Text = string.Empty;
        reviewCheckDetailsText.Text = string.Empty;
        reviewSelectedDestination = null;
        reviewExpandedPaths.Clear();
        reviewTopPath = null;
        lastBuildOutputFolder = null;
        lastBuildStatusLabel.Text = string.Empty;
        lastBuildStatusLabel.Visible = false;
        lastBuildOpenFolderButton.Visible = false;
        UpdateWorkflowNavigation();
    }

    private static void ScrollControlToStart(Control control)
    {
        if (control is ScrollableControl scrollable)
        {
            scrollable.AutoScrollPosition = Point.Empty;
        }

        foreach (Control child in control.Controls)
        {
            ScrollControlToStart(child);
        }
    }

    private void ShowEditor()
    {
        hasActiveEditorProject = true;
        launchPanel.Visible = false;
        editorPanel.Visible = true;
        if (workflowTabs.SelectedIndex < 0 && workflowTabs.TabPages.Count > 0)
        {
            workflowTabs.SelectedIndex = 0;
        }

        UpdateWorkflowNavigation();
    }

    private IDisposable SuspendEditorRedraw() => new RedrawScope(editorPanel);

    private sealed class RedrawScope : IDisposable
    {
        private const int WmSetRedraw = 0x000B;
        private readonly Control control;
        private readonly bool handleCreated;

        public RedrawScope(Control control)
        {
            this.control = control;
            handleCreated = control.IsHandleCreated;
            control.SuspendLayout();
            if (handleCreated)
            {
                SendMessage(control.Handle, WmSetRedraw, false, 0);
            }
        }

        public void Dispose()
        {
            if (handleCreated && !control.IsDisposed)
            {
                SendMessage(control.Handle, WmSetRedraw, true, 0);
            }

            if (!control.IsDisposed)
            {
                control.ResumeLayout(performLayout: true);
                control.Invalidate(invalidateChildren: true);
            }
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, bool wParam, int lParam);

    private void ShowHome()
    {
        RefreshRecentProjects();
        UpdateLaunchLayout();
        editorPanel.Visible = false;
        launchPanel.Visible = true;
    }

    private bool SavePackage()
    {
        if (projectPath is null)
        {
            return SavePackageAs();
        }

        return SavePackageToPath(projectPath);
    }

    private bool SavePackageToPath(string path)
    {
        if (!EnsureModelNameEditsCommitted())
        {
            return false;
        }

        SyncProjectFromControls();
        try
        {
            projectSerializer.Save(path, project);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or NotSupportedException)
        {
            MessageBox.Show(this, "The package project could not be saved.\r\n\r\n" + ex.Message, "Save Package Project", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        projectPath = path;
        TrackRecent(path);
        TrackProjectDirectory(path);
        SetDirty(false);
        return true;
    }

    private bool SavePackageAs()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "SFM Package Builder project|*.sfmpack|All files|*.*",
            DefaultExt = "sfmpack",
            AddExtension = true,
            InitialDirectory = GetProjectSaveAsStartDirectory()
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            return SavePackageToPath(dialog.FileName);
        }

        return false;
    }

    private void ShowSettings()
    {
        using var dialog = new SettingsForm(settings);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            ApplySettingsAfterDialog();
        }
    }

    private void ApplySettingsAfterDialog()
    {
        settingsService.Save(settings);
        recentProjectsService = new RecentProjectsService(settingsService);
        ApplyMachineSettingsToUi();
    }

    private void ApplyMachineSettingsToUi()
    {
        var wasLoading = loadingControls;
        loadingControls = true;
        outputFolderText.Text = settings.DefaultOutputDirectory;
        loadingControls = wasLoading;
        RefreshRecentProjects();
    }

    private string GetProjectOpenStartDirectory() =>
        projectDirectoryPreferenceService.ResolveOpenStartDirectory(settings);

    private string GetProjectSaveAsStartDirectory() =>
        projectDirectoryPreferenceService.ResolveSaveAsStartDirectory(settings, projectPath);

    private void ShowAbout()
    {
        using var dialog = new AboutForm();
        dialog.ShowDialog(this);
    }

    private UnsavedWorkResult ResolveUnsavedWork(UnsavedWorkAction action)
    {
        var resolver = new UnsavedWorkResolver(ShowUnsavedWorkPrompt, SavePackage);
        return resolver.Resolve(isDirty, action);
    }

    private UnsavedWorkChoice ShowUnsavedWorkPrompt(UnsavedWorkAction action)
    {
        if (unsavedWorkChoiceProvider is not null)
        {
            return unsavedWorkChoiceProvider(action);
        }

        var content = unsavedWorkPromptPresenter.ContentFor(action);
        using var dialog = new UnsavedWorkDialog(content);
        return dialog.ShowDialog(this) == DialogResult.OK || dialog.Choice != UnsavedWorkChoice.Cancel
            ? dialog.Choice
            : UnsavedWorkChoice.Cancel;
    }

    private void LoadProjectIntoControls()
    {
        loadingControls = true;
        try
        {
            assetNameText.Text = project.AssetName;
            versionText.Text = project.CurrentVersion;
            creditsText.Text = project.Credits;
            archiveNameState.LoadExisting(project.ArchiveName);
            archiveNameText.Text = project.ArchiveName;
            resetArchiveNameButton.Enabled = archiveNameState.HasManualName;
            outputFolderText.Text = settings.DefaultOutputDirectory;
            var primary = project.Models.FirstOrDefault(model => model.Role == ModelRole.Primary);
            primaryModelText.Text = primary?.SourceMdlPath ?? string.Empty;
            releaseStemText.Text = primary is null ? string.Empty : ModelReleaseName.EffectiveStem(primary);
            changesText.Text = string.Join(Environment.NewLine, project.ChangesThisVersion);
            RefreshReleaseHistoryList();
            LoadReadmeControls();
            var plan = TryPreviewPackageForPresentation(syncControls: false);
            CachePrimaryModelPlanEntries(plan);
            RefreshListsForPlan(plan);
            RefreshPrimaryModelPackagePresentation();
            UpdatePrimaryModelNameState(clearWhenNoSource: false);
            UpdateReleaseStemFeedback();
            UpdateReleaseHistoryAddButton();
        }
        finally
        {
            loadingControls = false;
        }

        SuggestArchiveNameIfSafe();
    }

    private void LoadReadmeControls()
    {
        importedReadmeText.Text = project.Readme.ImportedReadmePath ?? string.Empty;
        customReadmeText.Text = project.Readme.CustomReadmeText ?? string.Empty;
        readmeDescriptionText.Text = project.Readme.Description;
        readmeAuthorText.Text = project.Readme.Author;
        readmeWebsiteText.Text = project.Readme.Website;
        readmeLicenseText.Text = project.Readme.License;
        readmeResourcesText.Text = project.Readme.AdditionalResources;
        readmeGeneratedRadio.Checked = project.Readme.Mode == ReadmeMode.Generated;
        readmeCustomRadio.Checked = project.Readme.Mode == ReadmeMode.Custom && project.Readme.CustomSource != ReadmeCustomSource.ImportedFile;
        readmeImportRadio.Checked = project.Readme.Mode == ReadmeMode.Custom && project.Readme.CustomSource == ReadmeCustomSource.ImportedFile;
        readmeNoneRadio.Checked = project.Readme.Mode == ReadmeMode.None;
        UpdateReadmeModeControls();
    }

    private void SyncProjectFromControls()
    {
        project.AssetName = assetNameText.Text.Trim();
        project.CurrentVersion = versionText.Text.Trim();
        project.Credits = creditsText.Text.Trim();
        project.ArchiveName = archiveNameText.Text.Trim();
        project.ChangesThisVersion = changesText.Lines.Select(line => line.Trim()).Where(line => line.Length > 0).ToList();
        SyncPrimaryModelFromControls();
        CommitAdditionalReleaseStemText();

        project.Readme.Mode = readmeGeneratedRadio.Checked ? ReadmeMode.Generated :
            readmeNoneRadio.Checked ? ReadmeMode.None : ReadmeMode.Custom;
        project.Readme.CustomSource = readmeImportRadio.Checked
            ? ReadmeCustomSource.ImportedFile
            : ReadmeCustomSource.ProjectText;
        project.Readme.ImportedReadmePath = readmeImportRadio.Checked && !string.IsNullOrWhiteSpace(importedReadmeText.Text)
            ? importedReadmeText.Text.Trim()
            : null;
        project.Readme.CustomReadmeText = customReadmeText.Text;
        project.Readme.Description = readmeDescriptionText.Text;
        project.Readme.Author = readmeAuthorText.Text;
        project.Readme.Website = readmeWebsiteText.Text;
        project.Readme.License = readmeLicenseText.Text;
        project.Readme.AdditionalResources = readmeResourcesText.Text;
    }

    private void RefreshReleaseHistoryList()
    {
        releaseHistoryList.Items.Clear();
        foreach (var record in project.ReleaseHistory.Where(record => PackageProject.HasReadmeChangelogEntry(record.Changes)))
        {
            var row = new ListViewItem(record.Version);
            row.SubItems.Add(string.Join("; ", record.Changes.Where(change => !string.IsNullOrWhiteSpace(change))));
            row.Tag = record;
            releaseHistoryList.Items.Add(row);
        }

        UpdateReleaseHistoryButtons();
    }

    private void AddCurrentVersionToChangelog()
    {
        SyncProjectFromControls();
        if (!PackageProject.HasReadmeChangelogEntry(project.ChangesThisVersion))
        {
            UpdateReleaseHistoryAddButton();
            return;
        }

        project.BeginNewRelease(project.CurrentVersion, project.ChangesThisVersion);
        RefreshReleaseHistoryList();
        UpdateReleaseHistoryAddButton();
        MarkDirty();
    }

    private void EditSelectedReleaseHistory()
    {
        if (releaseHistoryList.SelectedItems.Count != 1 || releaseHistoryList.SelectedItems[0].Tag is not ReleaseRecord record)
        {
            return;
        }

        var index = project.ReleaseHistory.IndexOf(record);
        if (index < 0)
        {
            return;
        }

        using var dialog = new ChangelogEntryDialog("Edit Previous Version", record.Version, record.Changes);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        project.EditReleaseEntry(index, dialog.Version, dialog.Changes);
        RefreshReleaseHistoryList();
        MarkDirty();
    }

    private void RemoveSelectedReleaseHistory()
    {
        if (releaseHistoryList.SelectedItems.Count != 1 || releaseHistoryList.SelectedItems[0].Tag is not ReleaseRecord record)
        {
            return;
        }

        var index = project.ReleaseHistory.IndexOf(record);
        if (index < 0)
        {
            return;
        }

        if ((confirmReleaseHistoryRemoval ?? ConfirmReleaseHistoryRemoval)(record) == false)
        {
            return;
        }

        project.RemoveReleaseEntry(index);
        RefreshReleaseHistoryList();
        MarkDirty();
    }

    private void UpdateReleaseHistoryButtons()
    {
        var hasSelection = releaseHistoryList.SelectedItems.Count == 1;
        editSelectedReleaseButton.Enabled = hasSelection;
        removeSelectedReleaseButton.Enabled = hasSelection;
    }

    private void UpdateReleaseHistoryAddButton()
    {
        addReleaseHistoryButton.Enabled = PackageProject.HasReadmeChangelogEntry(changesText.Lines);
    }

    private bool ConfirmReleaseHistoryRemoval(ReleaseRecord record)
    {
        using var dialog = CreateReleaseHistoryRemovalDialog(record);
        return dialog.ShowDialog(this) == DialogResult.OK;
    }

    private Form CreateReleaseHistoryRemovalDialog(ReleaseRecord record)
    {
        var dialog = new Form
        {
            Text = "Remove Changelog Entry",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            ClientSize = new Size(420, 118),
            AutoScaleMode = AutoScaleMode.Dpi
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            ColumnCount = 1,
            RowCount = 2
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(new Label
        {
            Text = $"Remove version {record.Version} from the README changelog?",
            AutoSize = true,
            MaximumSize = new Size(380, 0),
            Margin = new Padding(0, 0, 0, 10)
        }, 0, 0);

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
            FlowDirection = FlowDirection.RightToLeft
        };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
        var remove = new Button { Text = "Remove", DialogResult = DialogResult.OK, AutoSize = true };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(remove);
        layout.Controls.Add(buttons, 0, 1);
        dialog.AcceptButton = remove;
        dialog.CancelButton = cancel;
        dialog.Controls.Add(layout);

        return dialog;
    }

    private void SyncPrimaryModelFromControls()
    {
        var primary = project.Models.FirstOrDefault(model => model.Role == ModelRole.Primary);
        var primaryPath = primaryModelText.Text.Trim();
        if (string.IsNullOrWhiteSpace(primaryPath))
        {
            if (primary is not null)
            {
                project.Models.Remove(primary);
            }

            return;
        }

        if (primary is null)
        {
            primary = new ModelEntry { Role = ModelRole.Primary };
            project.Models.Insert(0, primary);
        }

        primary.SourceMdlPath = primaryPath;
        primary.SourceStem = Path.GetFileNameWithoutExtension(primary.SourceMdlPath);
        primary.ReleaseStem = releaseStemText.Text;
    }

    private void PrimaryReleaseStemTextChanged()
    {
        if (loadingControls)
        {
            return;
        }

        UpdatePrimaryReleaseStemFromText();
        UpdateReleaseStemFeedback(commitAttempt: false);
        ScheduleModelNamePresentation(ModelNameEditTarget.Primary);
        MarkDirty();
    }

    private void UpdatePrimaryReleaseStemFromText()
    {
        var primary = project.Models.FirstOrDefault(model => model.Role == ModelRole.Primary);
        if (primary is null)
        {
            return;
        }

        primary.ReleaseStem = releaseStemText.Text;
    }

    private void CommitReleaseStemText() => _ = TryCommitPrimaryReleaseStemText(finishEditingOnSuccess: false);

    private bool TryCommitPrimaryReleaseStemText(bool finishEditingOnSuccess)
    {
        if (loadingControls || string.IsNullOrWhiteSpace(primaryModelText.Text))
        {
            UpdateReleaseStemFeedback(commitAttempt: true);
            return true;
        }

        if (!string.IsNullOrWhiteSpace(releaseStemText.Text))
        {
            var validation = UpdateReleaseStemFeedback(commitAttempt: true);
            if (!validation.IsValid)
            {
                FocusModelNameEditor(releaseStemText);
                return false;
            }

            UpdatePrimaryReleaseStemFromText();
            ForceModelNamePresentation(ModelNameEditTarget.Primary);
            FinishModelNameEdit(ModelNameEditTarget.Primary, finishEditingOnSuccess);
            return true;
        }

        var sourceStem = Path.GetFileNameWithoutExtension(primaryModelText.Text.Trim());
        if (string.IsNullOrWhiteSpace(sourceStem))
        {
            UpdateReleaseStemFeedback(commitAttempt: true);
            return true;
        }

        releaseStemText.Text = sourceStem;
        UpdatePrimaryReleaseStemFromText();
        UpdateReleaseStemFeedback(commitAttempt: true);
        ForceModelNamePresentation(ModelNameEditTarget.Primary);
        FinishModelNameEdit(ModelNameEditTarget.Primary, finishEditingOnSuccess);
        MarkDirty();
        return true;
    }

    private ModelNameValidationResult UpdateReleaseStemFeedback(bool commitAttempt = false)
    {
        if (loadingControls
            || string.IsNullOrWhiteSpace(primaryModelText.Text)
            || string.IsNullOrWhiteSpace(releaseStemText.Text))
        {
            releaseStemValidationLabel.Text = string.Empty;
            releaseStemValidationLabel.Visible = false;
            return ModelNameValidationResult.Valid;
        }

        var validation = WindowsFileNameValidator.ValidateModelStem(releaseStemText.Text);
        var showValidation = !validation.IsValid && (commitAttempt || !IsTerminalSpaceOrPeriodValidation(validation));
        releaseStemValidationLabel.Text = showValidation ? validation.InlineMessage : string.Empty;
        releaseStemValidationLabel.Visible = showValidation;
        return validation;
    }

    private static bool IsTerminalSpaceOrPeriodValidation(ModelNameValidationResult validation) =>
        string.Equals(validation.InlineMessage, "Model name cannot end with a space or period.", StringComparison.Ordinal);

    private void ModelNameTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter)
        {
            return;
        }

        e.SuppressKeyPress = true;
        e.Handled = true;
        _ = CommitActiveModelNameEdit(finishEditingOnSuccess: true);
    }

    private bool EnsureModelNameEditsCommitted() =>
        CommitActiveModelNameEdit(finishEditingOnSuccess: true);

    private bool CommitActiveModelNameEdit(bool finishEditingOnSuccess)
    {
        var target = ActiveModelNameEditTarget();
        return target switch
        {
            ModelNameEditTarget.Primary => TryCommitPrimaryReleaseStemText(finishEditingOnSuccess),
            ModelNameEditTarget.Additional => TryCommitAdditionalReleaseStemText(finishEditingOnSuccess),
            _ => true
        };
    }

    private ModelNameEditTarget ActiveModelNameEditTarget()
    {
        if (releaseStemText.Focused)
        {
            return ModelNameEditTarget.Primary;
        }

        if (additionalReleaseStemText.Focused)
        {
            return ModelNameEditTarget.Additional;
        }

        return activeModelNameEdit;
    }

    private void FinishModelNameEdit(ModelNameEditTarget target, bool moveFocus)
    {
        if (activeModelNameEdit == target)
        {
            activeModelNameEdit = ModelNameEditTarget.None;
        }

        if (moveFocus)
        {
            workflowTabs.Focus();
        }
    }

    private void FocusModelNameEditor(TextBox editor)
    {
        activeModelNameEdit = ReferenceEquals(editor, releaseStemText)
            ? ModelNameEditTarget.Primary
            : ModelNameEditTarget.Additional;
        BeginInvoke(() =>
        {
            if (!editor.IsDisposed && editor.CanFocus)
            {
                editor.Focus();
                editor.SelectionStart = editor.TextLength;
                editor.SelectionLength = 0;
            }
        });
    }

    private void RegisterModelNameNeutralCommit(Control control)
    {
        control.MouseDown += (_, _) => CommitModelNameEditFromNeutralBackground();
    }

    private void RegisterStageOnePassiveLabelCommit(Control root)
    {
        foreach (Control control in root.Controls)
        {
            if (control is Label)
            {
                RegisterModelNameNeutralCommit(control);
            }

            RegisterStageOnePassiveLabelCommit(control);
        }
    }

    private bool CommitModelNameEditFromNeutralBackground()
    {
        var committed = CommitActiveModelNameEdit(finishEditingOnSuccess: true);
        if (committed)
        {
            workflowTabs.Focus();
        }

        return committed;
    }

    private bool CommitModelNameEditFromNeutralBackgroundForTests() => CommitModelNameEditFromNeutralBackground();

    private bool CommitActiveModelNameEditForTests() => CommitActiveModelNameEdit(finishEditingOnSuccess: true);

    private void ScheduleModelNamePresentation(ModelNameEditTarget target)
    {
        if (target == ModelNameEditTarget.Primary)
        {
            pendingPrimaryModelNamePresentation = true;
        }
        else if (target == ModelNameEditTarget.Additional)
        {
            pendingAdditionalModelNamePresentation = true;
        }

        if (!modelNamePresentationTimer.Enabled)
        {
            modelNamePresentationTimer.Start();
        }
    }

    private void ForceModelNamePresentation(ModelNameEditTarget target)
    {
        if (target == ModelNameEditTarget.Primary)
        {
            pendingPrimaryModelNamePresentation = true;
        }
        else if (target == ModelNameEditTarget.Additional)
        {
            pendingAdditionalModelNamePresentation = true;
        }

        FlushPendingModelNamePresentation();
    }

    private void FlushPendingModelNamePresentation()
    {
        modelNamePresentationTimer.Stop();
        var refreshPrimary = pendingPrimaryModelNamePresentation;
        var refreshAdditional = pendingAdditionalModelNamePresentation;
        pendingPrimaryModelNamePresentation = false;
        pendingAdditionalModelNamePresentation = false;

        var layout = modelMaterialsLayout;
        layout?.SuspendLayout();
        if (refreshAdditional)
        {
            additionalModelsList.BeginUpdate();
        }

        try
        {
            if (refreshPrimary)
            {
                RefreshPrimaryModelPackagePresentation();
            }

            if (refreshAdditional)
            {
                RefreshAdditionalModelReleasePresentation();
            }
        }
        finally
        {
            if (refreshAdditional)
            {
                additionalModelsList.EndUpdate();
            }

            layout?.ResumeLayout(performLayout: true);
        }
    }

    private void FlushPendingModelNamePresentationForTests() => FlushPendingModelNamePresentation();

    private void RefreshLists() => RefreshListsForPlan(null);

    private void RefreshListsForPlan(PackagePlan? plan)
    {
        var presentationPlan = plan ?? TryPreviewPackageForPresentation(syncControls: true);
        CacheModelPlanEntries(presentationPlan);
        additionalModelsList.Items.Clear();
        var companionDetails = GetAdditionalModelCompanionDetails(presentationPlan);
        foreach (var model in project.Models.Where(model => model.Role == ModelRole.Additional))
        {
            var row = new ListViewItem(Path.GetFileName(model.SourceMdlPath));
            var detail = companionDetails.TryGetValue(model.Id, out var value)
                ? value
                : new AdditionalModelCompanionDetail(0, Array.Empty<string>());
            row.SubItems.Add(AdditionalModelDisplayStem(model));
            row.SubItems.Add(detail.Count.ToString(CultureInfo.InvariantCulture));
            row.Tag = new ModelListItem(model, detail);
            additionalModelsList.Items.Add(row);
        }
        if (additionalModelsList.Items.Count == 0)
        {
            var empty = new ListViewItem("No additional models.");
            empty.SubItems.Add(string.Empty);
            empty.SubItems.Add(string.Empty);
            empty.ForeColor = SystemColors.GrayText;
            additionalModelsList.Items.Add(empty);
        }
        additionalModelsSummaryLabel.Text = $"Additional models ({project.Models.Count(model => model.Role == ModelRole.Additional)})";
        LoadSelectedAdditionalModelReleaseStemEditor();
        UpdateAdditionalModelCompanionDetail();
        UpdateAdditionalModelButtons();
        materialsList.Items.Clear();
        foreach (var source in project.MaterialSources)
            materialsList.Items.Add(new SourceListItem(source));
        if (materialsList.Items.Count == 0)
            materialsList.Items.Add("No materials added yet.");
        extrasList.Items.Clear();
        foreach (var source in project.Extras)
        {
            var row = new ListViewItem(Path.GetFileName(source.SourcePath));
            row.SubItems.Add(extraDestinationPresenter.PackageLocationFor(source));
            row.Tag = new SourceListItem(source);
            toolTip.SetToolTip(extrasList, source.SourcePath);
            extrasList.Items.Add(row);
        }
        if (extrasList.Items.Count == 0)
        {
            var empty = new ListViewItem("No extras added.");
            empty.SubItems.Add(string.Empty);
            empty.ForeColor = SystemColors.GrayText;
            extrasList.Items.Add(empty);
        }
        LoadSelectedExtraDestination();
        RefreshHorizontalExtents(materialsList);
        UpdateAdditionalModelColumns();
        UpdateExtrasColumns();
        UpdateStageOneListHeights();
    }

    private Dictionary<Guid, AdditionalModelCompanionDetail> GetAdditionalModelCompanionDetails(PackagePlan? plan = null)
    {
        try
        {
            return (plan ?? CreateCoordinator().PreviewPackage(project)).Entries
                .Where(entry => entry.ModelEntryId is not null)
                .Where(entry => entry.EntryType is PackagePlanEntryType.ModelCompanion)
                .Where(entry => project.Models.Any(model => model.Role == ModelRole.Additional && model.Id == entry.ModelEntryId))
                .GroupBy(entry => entry.ModelEntryId!.Value)
                .ToDictionary(
                    group => group.Key,
                    group => new AdditionalModelCompanionDetail(
                        group.Count(),
                        group
                            .Select(entry => Path.GetFileName(entry.DestinationRelativePath ?? entry.SourcePath ?? string.Empty))
                            .Where(name => !string.IsNullOrWhiteSpace(name))
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                            .ToArray()));
        }
        catch
        {
            return new Dictionary<Guid, AdditionalModelCompanionDetail>();
        }
    }

    private void SuggestArchiveNameIfSafe()
    {
        if (loadingControls || archiveNameState.HasManualName) return;
        SetArchiveNameFromSuggestion(archiveNameState.RefreshSuggested(assetNameText.Text, versionText.Text, settings));
    }

    private void ResetArchiveNameSuggestion()
    {
        SetArchiveNameFromSuggestion(archiveNameState.ResetToSuggested(assetNameText.Text, versionText.Text, settings));
        resetArchiveNameButton.Enabled = false;
        MarkDirty();
    }

    private void SetArchiveNameFromSuggestion(string archiveName)
    {
        updatingArchiveSuggestion = true;
        archiveNameText.Text = archiveName;
        updatingArchiveSuggestion = false;
    }

    private void RefreshRenameDependentPresentation()
    {
        if (!loadingControls)
        {
            UpdatePrimaryReleaseStemFromText();
        }

        RefreshPrimaryModelPackagePresentation();
    }

    private void UpdateRenameIllustration()
    {
        RefreshPrimaryModelPackagePresentation();
    }

    private void RefreshPrimaryModelPackagePresentation()
    {
        var entries = CurrentPrimaryModelPresentationEntries();
        UpdateRenameIllustrationFromEntries(entries);
        var familyEntries = entries
            .Where(entry => entry.EntryType is PackagePlanEntryType.ModelCompanion)
            .Select(entry => Path.GetFileName(entry.DestinationRelativePath ?? string.Empty))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();
        var summaryText = familyEntries.Length > 0
            ? $"Related model files included ({familyEntries.Length})"
            : string.Empty;
        if (!string.Equals(modelFamilySummaryLabel.Text, summaryText, StringComparison.Ordinal))
        {
            modelFamilySummaryLabel.Text = summaryText;
        }

        SetModelFamilyDisplayNames(familyEntries);
    }

    private PackagePlan? TryPreviewPackageForPresentation(bool syncControls)
    {
        try
        {
            if (syncControls && !loadingControls)
            {
                SyncProjectFromControls();
            }

            return CreateCoordinator().PreviewPackage(project);
        }
        catch
        {
            return null;
        }
    }

    private void CacheModelPlanEntries(PackagePlan? plan)
    {
        modelPlanEntries = plan is null
            ? Array.Empty<PackagePlanEntry>()
            : plan.Entries
                .Where(entry => entry.EntryType is PackagePlanEntryType.Model or PackagePlanEntryType.ModelCompanion)
                .ToArray();
        CachePrimaryModelPlanEntriesFromCached();
    }

    private void CachePrimaryModelPlanEntries(PackagePlan? plan)
    {
        CacheModelPlanEntries(plan);
    }

    private void CachePrimaryModelPlanEntriesFromCached()
    {
        var primary = project.Models.FirstOrDefault(model => model.Role == ModelRole.Primary);
        primaryModelPlanEntries = primary is null
            ? Array.Empty<PackagePlanEntry>()
            : modelPlanEntries
                .Where(entry => entry.ModelEntryId == primary.Id)
                .ToArray();
    }

    private IReadOnlyList<PackagePlanEntry> CurrentPrimaryModelPresentationEntries()
    {
        var primary = project.Models.FirstOrDefault(model => model.Role == ModelRole.Primary);
        if (primary is null || primaryModelPlanEntries.Count == 0)
        {
            return Array.Empty<PackagePlanEntry>();
        }

        var sourceStem = Path.GetFileNameWithoutExtension(primary.SourceMdlPath);
        var releaseStem = string.IsNullOrWhiteSpace(releaseStemText.Text)
            ? sourceStem
            : releaseStemText.Text.Trim();
        if (string.IsNullOrWhiteSpace(sourceStem) || string.IsNullOrWhiteSpace(releaseStem))
        {
            return primaryModelPlanEntries;
        }

        return primaryModelPlanEntries
            .Select(entry => RenameModelDestination(entry, sourceStem, releaseStem))
            .ToArray();
    }

    private IReadOnlyList<PackagePlanEntry> CurrentAdditionalModelPresentationEntries(ModelEntry model)
    {
        var sourceStem = AdditionalModelSourceStem(model);
        var releaseStem = editingAdditionalModelId == model.Id
            ? additionalReleaseStemText.Text.Trim()
            : AdditionalModelDisplayStem(model);
        if (string.IsNullOrWhiteSpace(releaseStem))
        {
            releaseStem = sourceStem;
        }

        if (string.IsNullOrWhiteSpace(sourceStem) || string.IsNullOrWhiteSpace(releaseStem))
        {
            return modelPlanEntries
                .Where(entry => entry.ModelEntryId == model.Id)
                .ToArray();
        }

        return modelPlanEntries
            .Where(entry => entry.ModelEntryId == model.Id)
            .Select(entry => RenameModelDestination(entry, sourceStem, releaseStem))
            .ToArray();
    }

    private static PackagePlanEntry RenameModelDestination(PackagePlanEntry entry, string sourceStem, string releaseStem)
    {
        if (entry.DestinationRelativePath is null || entry.SourcePath is null)
        {
            return entry;
        }

        var runtimeSuffix = RuntimeSuffixFor(entry.SourcePath, sourceStem);
        if (string.IsNullOrWhiteSpace(runtimeSuffix))
        {
            return entry;
        }

        return new PackagePlanEntry(
            entry.Id,
            entry.EntryType,
            entry.Status,
            ReplaceDestinationFileName(entry.DestinationRelativePath, releaseStem + runtimeSuffix),
            entry.SourcePath,
            entry.OriginProjectEntryId,
            entry.ModelEntryId,
            entry.IsGenerated,
            entry.ReadmeKind,
            entry.TextContent,
            entry.ContentBytes,
            entry.IssueDetail,
            entry.RiskMetadata);
    }

    private static string RuntimeSuffixFor(string sourcePath, string sourceStem)
    {
        var fileName = Path.GetFileName(sourcePath);
        if (fileName.StartsWith(sourceStem, StringComparison.OrdinalIgnoreCase))
        {
            return fileName[sourceStem.Length..];
        }

        return string.Equals(Path.GetExtension(fileName), ".mdl", StringComparison.OrdinalIgnoreCase)
            ? ".mdl"
            : Path.GetExtension(fileName);
    }

    private static string ReplaceDestinationFileName(string destinationRelativePath, string fileName)
    {
        var normalized = destinationRelativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        var separatorIndex = normalized.LastIndexOf(Path.DirectorySeparatorChar);
        return separatorIndex < 0
            ? fileName
            : normalized[..(separatorIndex + 1)] + fileName;
    }

    private void UpdateRenameIllustrationFromEntries(IReadOnlyList<PackagePlanEntry> entries)
    {
        if (string.IsNullOrWhiteSpace(primaryModelText.Text))
        {
            HideRenamePreview();
            return;
        }

        try
        {
            var primary = project.Models.FirstOrDefault(model => model.Role == ModelRole.Primary);
            var mappings = releaseModelNamePresenter.PresentMappings(
                primaryModelText.Text,
                releaseStemText.Text,
                entries,
                primary?.Id);
            ShowRenamePreview(mappings);
        }
        catch
        {
            HideRenamePreview();
        }
    }

    private void ShowRenamePreview(IReadOnlyList<ReleaseModelRenameMapping> mappings)
    {
        var mdlMapping = mappings.FirstOrDefault(mapping =>
            string.Equals(Path.GetExtension(mapping.Original), ".mdl", StringComparison.OrdinalIgnoreCase));
        var hasRenamePreview = mdlMapping is not null;
        var text = hasRenamePreview
            ? $"Preview: {mdlMapping!.Original} → {mdlMapping.Zip}{Environment.NewLine}Your original files stay unchanged."
            : string.Empty;
        if (!string.Equals(renamePreviewLabel.Text, text, StringComparison.Ordinal))
        {
            renamePreviewLabel.Text = text;
        }

        if (renamePreviewLabel.Visible != hasRenamePreview)
        {
            renamePreviewLabel.Visible = hasRenamePreview;
        }
    }

    private void HideRenamePreview()
    {
        if (renamePreviewLabel.Text.Length > 0)
        {
            renamePreviewLabel.Text = string.Empty;
        }

        if (renamePreviewLabel.Visible)
        {
            renamePreviewLabel.Visible = false;
        }
    }

    private void LoadSelectedAdditionalModelReleaseStemEditor()
    {
        if (loadingAdditionalReleaseStemText)
        {
            return;
        }

        var model = SelectedAdditionalModel();
        loadingAdditionalReleaseStemText = true;
        try
        {
            editingAdditionalModelId = model?.Id;
            var visible = model is not null;
            additionalReleaseStemLabel.Visible = visible;
            additionalReleaseStemText.Visible = visible;
            additionalReleaseStemText.Enabled = visible;
            additionalReleaseStemText.Text = model is null ? string.Empty : AdditionalModelDisplayStem(model);
            additionalReleaseStemValidationLabel.Text = string.Empty;
            additionalReleaseStemValidationLabel.Visible = false;
            additionalRenamePreviewLabel.Text = string.Empty;
            additionalRenamePreviewLabel.Visible = false;
            additionalModelCompanionDetailLabel.Visible = true;
        }
        finally
        {
            loadingAdditionalReleaseStemText = false;
        }

        UpdateAdditionalReleaseStemFeedback();
        ForceModelNamePresentation(ModelNameEditTarget.Additional);
    }

    private void AdditionalReleaseStemTextChanged()
    {
        if (loadingControls || loadingAdditionalReleaseStemText)
        {
            return;
        }

        var model = SelectedAdditionalModel();
        if (model is null)
        {
            UpdateAdditionalReleaseStemFeedback();
            return;
        }

        model.ReleaseStem = NormalizeAdditionalReleaseOverride(model, additionalReleaseStemText.Text, allowBlank: true);
        UpdateAdditionalReleaseStemFeedback(commitAttempt: false);
        ScheduleModelNamePresentation(ModelNameEditTarget.Additional);
        MarkDirty();
    }

    private void CommitAdditionalReleaseStemText() => _ = TryCommitAdditionalReleaseStemText(finishEditingOnSuccess: false);

    private bool TryCommitAdditionalReleaseStemText(bool finishEditingOnSuccess)
    {
        if (loadingControls || editingAdditionalModelId is null)
        {
            return true;
        }

        var model = project.Models.FirstOrDefault(candidate => candidate.Id == editingAdditionalModelId.Value);
        if (model is null)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(additionalReleaseStemText.Text))
        {
            var validation = UpdateAdditionalReleaseStemFeedback(commitAttempt: true);
            if (!validation.IsValid)
            {
                FocusModelNameEditor(additionalReleaseStemText);
                return false;
            }
        }

        var normalized = NormalizeAdditionalReleaseOverride(model, additionalReleaseStemText.Text, allowBlank: false);
        model.ReleaseStem = normalized;
        var display = AdditionalModelDisplayStem(model);
        if (!string.Equals(additionalReleaseStemText.Text, display, StringComparison.Ordinal))
        {
            loadingAdditionalReleaseStemText = true;
            try
            {
                additionalReleaseStemText.Text = display;
            }
            finally
            {
                loadingAdditionalReleaseStemText = false;
            }
        }

        UpdateAdditionalReleaseStemFeedback(commitAttempt: true);
        UpdateAdditionalModelRow(model);
        ForceModelNamePresentation(ModelNameEditTarget.Additional);
        FinishModelNameEdit(ModelNameEditTarget.Additional, finishEditingOnSuccess);
        return true;
    }

    private ModelNameValidationResult UpdateAdditionalReleaseStemFeedback(bool commitAttempt = false)
    {
        if (loadingControls
            || editingAdditionalModelId is null
            || string.IsNullOrWhiteSpace(additionalReleaseStemText.Text))
        {
            additionalReleaseStemValidationLabel.Text = string.Empty;
            additionalReleaseStemValidationLabel.Visible = false;
            return ModelNameValidationResult.Valid;
        }

        var validation = WindowsFileNameValidator.ValidateModelStem(additionalReleaseStemText.Text);
        var showValidation = !validation.IsValid && (commitAttempt || !IsTerminalSpaceOrPeriodValidation(validation));
        additionalReleaseStemValidationLabel.Text = showValidation ? validation.InlineMessage : string.Empty;
        additionalReleaseStemValidationLabel.Visible = showValidation;
        return validation;
    }

    private void RefreshAdditionalModelReleasePresentation()
    {
        var model = SelectedAdditionalModel();
        if (model is null)
        {
            if (additionalRenamePreviewLabel.Text.Length > 0)
            {
                additionalRenamePreviewLabel.Text = string.Empty;
            }

            if (additionalRenamePreviewLabel.Visible)
            {
                additionalRenamePreviewLabel.Visible = false;
            }

            if (additionalModelCompanionDetailLabel.Text.Length > 0)
            {
                additionalModelCompanionDetailLabel.Text = string.Empty;
            }

            return;
        }

        UpdateAdditionalModelRow(model);
        UpdateAdditionalRenamePreview(model);
        UpdateAdditionalModelCompanionDetail();
    }

    private void UpdateAdditionalRenamePreview(ModelEntry model)
    {
        var sourceStem = AdditionalModelSourceStem(model);
        var releaseStem = additionalReleaseStemText.Text.Trim();
        var renamed = !string.IsNullOrWhiteSpace(sourceStem)
            && !string.IsNullOrWhiteSpace(releaseStem)
            && !string.Equals(sourceStem, releaseStem, StringComparison.OrdinalIgnoreCase);
        var text = renamed
            ? $"Preview: {sourceStem}.mdl → {releaseStem}.mdl{Environment.NewLine}Your original files stay unchanged."
            : string.Empty;

        if (!string.Equals(additionalRenamePreviewLabel.Text, text, StringComparison.Ordinal))
        {
            additionalRenamePreviewLabel.Text = text;
        }

        if (additionalRenamePreviewLabel.Visible != renamed)
        {
            additionalRenamePreviewLabel.Visible = renamed;
        }
    }

    private void UpdateAdditionalModelRow(ModelEntry model)
    {
        foreach (ListViewItem row in additionalModelsList.Items)
        {
            if (row.Tag is not ModelListItem item || item.Model.Id != model.Id)
            {
                continue;
            }

            while (row.SubItems.Count < 3)
            {
                row.SubItems.Add(string.Empty);
            }

            var entries = CurrentAdditionalModelPresentationEntries(model);
            var companionCount = entries.Count(entry => entry.EntryType == PackagePlanEntryType.ModelCompanion);
            var displayStem = AdditionalModelDisplayStem(model);
            if (!string.Equals(row.SubItems[1].Text, displayStem, StringComparison.Ordinal))
            {
                row.SubItems[1].Text = displayStem;
            }

            var companionText = companionCount.ToString(CultureInfo.InvariantCulture);
            if (!string.Equals(row.SubItems[2].Text, companionText, StringComparison.Ordinal))
            {
                row.SubItems[2].Text = companionText;
            }

            break;
        }
    }

    private void RestoreAdditionalModelSelection(Guid modelId)
    {
        restoringAdditionalModelSelection = true;
        try
        {
            foreach (ListViewItem row in additionalModelsList.Items)
            {
                row.Selected = row.Tag is ModelListItem item && item.Model.Id == modelId;
            }
        }
        finally
        {
            restoringAdditionalModelSelection = false;
        }

        FocusModelNameEditor(additionalReleaseStemText);
    }

    private void UpdateAdditionalModelColumns()
    {
        if (additionalModelsList.Columns.Count < 3)
        {
            return;
        }

        var clientWidth = additionalModelsList.ClientSize.Width > 0
            ? additionalModelsList.ClientSize.Width
            : additionalModelsList.Width;
        var width = Math.Max(Scaled(80), clientWidth - SystemInformation.VerticalScrollBarWidth - Scaled(6));
        var preferredRelatedWidth = Math.Min(Scaled(126), Math.Max(Scaled(112), TextRenderer.MeasureText("Related files", additionalModelsList.Font).Width + Scaled(22)));
        var relatedWidth = Math.Min(preferredRelatedWidth, Math.Max(Scaled(72), (int)Math.Round(width * 0.15)));
        var remaining = Math.Max(Scaled(160), width - relatedWidth);
        additionalModelsList.Columns[0].Width = Math.Max(Scaled(120), (int)Math.Round(remaining * 0.47));
        additionalModelsList.Columns[1].Width = Math.Max(Scaled(120), remaining - additionalModelsList.Columns[0].Width);
        additionalModelsList.Columns[2].Width = relatedWidth;
    }

    private void UpdateExtrasColumns()
    {
        if (extrasList.Columns.Count < 2)
        {
            return;
        }

        var clientWidth = extrasList.ClientSize.Width > 0 ? extrasList.ClientSize.Width : extrasList.Width;
        var width = Math.Max(Scaled(160), clientWidth - SystemInformation.VerticalScrollBarWidth - Scaled(6));
        var fileWidth = Math.Max(Scaled(120), (int)Math.Round(width * 0.6));
        extrasList.Columns[0].Width = fileWidth;
        extrasList.Columns[1].Width = Math.Max(Scaled(120), width - fileWidth);
    }

    private void UpdateReviewCheckColumns()
    {
        if (reviewCheckList.Columns.Count < 2)
        {
            return;
        }

        var clientWidth = reviewCheckList.ClientSize.Width > 0 ? reviewCheckList.ClientSize.Width : reviewCheckList.Width;
        var width = Math.Max(Scaled(220), clientWidth - SystemInformation.VerticalScrollBarWidth - Scaled(6));
        var severityWidth = Math.Max(Scaled(92), TextRenderer.MeasureText("Warning", reviewCheckList.Font).Width + Scaled(22));
        reviewCheckList.Columns[0].Width = severityWidth;
        reviewCheckList.Columns[1].Width = Math.Max(Scaled(120), width - severityWidth);
    }

    private void ConfigureReviewCheckDetailsWidth()
    {
        var width = reviewCheckDetailsHostPanel.ClientSize.Width > 0
            ? reviewCheckDetailsHostPanel.ClientSize.Width
            : reviewCheckDetailsHostPanel.Width;
        if (width <= 0)
        {
            return;
        }

        var targetWidth = (int)Math.Round(width * 0.70);
        var minimumWidth = Math.Min(Scaled(240), width);
        reviewCheckDetailsText.Width = Math.Clamp(targetWidth, minimumWidth, width);
        reviewCheckDetailsText.Height = reviewCheckDetailsHostPanel.ClientSize.Height > 0
            ? reviewCheckDetailsHostPanel.ClientSize.Height
            : reviewCheckDetailsHostPanel.Height;
    }

    private void ConfigureReviewPackageSplitDistance()
    {
        var width = reviewPackageSplit.ClientSize.Width;
        if (width <= 0)
        {
            return;
        }

        var availableWidth = width - reviewPackageSplit.SplitterWidth;
        if (availableWidth <= 2)
        {
            return;
        }

        var minimumTree = Math.Min(Scaled(300), availableWidth - 1);
        var minimumDetails = Math.Min(Scaled(380), availableWidth - minimumTree);
        var desired = (int)Math.Round(availableWidth * 0.60);
        var maximum = availableWidth - minimumDetails;
        reviewPackageSplit.SplitterDistance = Math.Clamp(desired, minimumTree, maximum);
    }

    private void UpdateAdditionalModelCompanionDetail()
    {
        if (additionalModelsList.SelectedItems.Count != 1
            || additionalModelsList.SelectedItems[0].Tag is not ModelListItem item)
        {
            if (additionalModelCompanionDetailLabel.Text.Length > 0)
            {
                additionalModelCompanionDetailLabel.Text = string.Empty;
            }

            return;
        }

        var names = CurrentAdditionalModelPresentationEntries(item.Model)
            .Where(entry => entry.EntryType == PackagePlanEntryType.ModelCompanion)
            .Select(entry => Path.GetFileName(entry.DestinationRelativePath ?? entry.SourcePath ?? string.Empty))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (names.Length == 0 && item.CompanionDetail.FileNames.Count > 0)
        {
            names = item.CompanionDetail.FileNames.ToArray();
        }

        var text = names.Length == 0
            ? "Related files included: None"
            : "Related files included: " + string.Join(", ", names);
        if (!string.Equals(additionalModelCompanionDetailLabel.Text, text, StringComparison.Ordinal))
        {
            additionalModelCompanionDetailLabel.Text = text;
        }
    }

    private void UpdateAdditionalModelButtons()
    {
        removeAdditionalModelButton.Enabled = additionalModelsList.SelectedItems.Count == 1
            && additionalModelsList.SelectedItems[0].Tag is ModelListItem;
    }

    private ModelEntry? SelectedAdditionalModel() =>
        additionalModelsList.SelectedItems.Count == 1
        && additionalModelsList.SelectedItems[0].Tag is ModelListItem item
            ? item.Model
            : null;

    private static string AdditionalModelDisplayStem(ModelEntry model)
    {
        var effective = ModelReleaseName.EffectiveStem(model);
        return string.IsNullOrWhiteSpace(effective)
            ? AdditionalModelSourceStem(model)
            : effective.Trim();
    }

    private static string AdditionalModelSourceStem(ModelEntry model)
    {
        if (!string.IsNullOrWhiteSpace(model.SourceStem))
        {
            return model.SourceStem.Trim();
        }

        return string.IsNullOrWhiteSpace(model.SourceMdlPath)
            ? string.Empty
            : Path.GetFileNameWithoutExtension(model.SourceMdlPath).Trim();
    }

    private static string NormalizeAdditionalReleaseOverride(ModelEntry model, string value, bool allowBlank)
    {
        var sourceStem = AdditionalModelSourceStem(model);
        var trimmed = value.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return allowBlank ? string.Empty : sourceStem;
        }

        return string.Equals(trimmed, sourceStem, StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : value;
    }

    private void UpdateStageOneListHeights()
    {
        if (additionalModelsList.IsDisposed || modelFamilyNamesLabel.IsDisposed)
        {
            return;
        }

        modelFamilyNamesLabel.Height = Math.Max(Scaled(30), TextRenderer.MeasureText("Mg", modelFamilyNamesLabel.Font).Height * 2 + Scaled(5));
        SetListViewHeight(additionalModelsList, additionalModelsList.Items.Count, 1, 8);
        SetListBoxHeight(materialsList, materialsList.Items.Count, 4, 12);
        UpdateAdditionalModelColumns();
    }

    private void SetListBoxHeight(ListBox list, int rows, int minRows, int maxRows)
    {
        var visibleRows = Math.Clamp(rows, minRows, maxRows);
        var rowHeight = Math.Max(list.ItemHeight, TextRenderer.MeasureText("Mg", list.Font).Height + Scaled(3));
        list.Height = visibleRows * rowHeight + Scaled(8);
    }

    private void SetListViewHeight(ListView list, int rows, int minRows, int maxRows)
    {
        var visibleRows = Math.Clamp(rows, minRows, maxRows);
        var rowHeight = TextRenderer.MeasureText("Mg", list.Font).Height + Scaled(6);
        var headerHeight = list.HeaderStyle == ColumnHeaderStyle.None ? 0 : rowHeight + Scaled(4);
        list.Height = headerHeight + visibleRows * rowHeight + Scaled(8);
    }

    private int Scaled(int logicalPixels) => (int)Math.Ceiling(logicalPixels * (DeviceDpi / 96.0));

    private void SetModelFamilyDisplayNames(IEnumerable<string> names)
    {
        modelFamilyDisplayNames = names.ToArray();
        var hasCompanions = modelFamilyDisplayNames.Count > 0;
        if (modelFamilySummaryLabel.Visible != hasCompanions)
        {
            modelFamilySummaryLabel.Visible = hasCompanions;
        }

        if (modelFamilyNamesLabel.Visible != hasCompanions)
        {
            modelFamilyNamesLabel.Visible = hasCompanions;
        }

        var text = hasCompanions
            ? string.Join(", ", modelFamilyDisplayNames)
            : string.Empty;
        if (!string.Equals(modelFamilyNamesLabel.Text, text, StringComparison.Ordinal))
        {
            modelFamilyNamesLabel.Text = text;
        }
    }

    private void RefreshModelFamilyInfo()
    {
        if (string.IsNullOrWhiteSpace(primaryModelText.Text))
        {
            modelPlanEntries = Array.Empty<PackagePlanEntry>();
            primaryModelPlanEntries = Array.Empty<PackagePlanEntry>();
            modelFamilySummaryLabel.Text = string.Empty;
            SetModelFamilyDisplayNames(Array.Empty<string>());
            HideRenamePreview();
            UpdateStageOneListHeights();
            return;
        }
        try
        {
            if (!loadingControls)
            {
                SyncProjectFromControls();
            }

            var plan = CreateCoordinator().PreviewPackage(project);
            CachePrimaryModelPlanEntries(plan);
            RefreshPrimaryModelPackagePresentation();
            UpdateStageOneListHeights();
        }
        catch
        {
            modelPlanEntries = Array.Empty<PackagePlanEntry>();
            primaryModelPlanEntries = Array.Empty<PackagePlanEntry>();
            SetModelFamilyDisplayNames(Array.Empty<string>());
            modelFamilySummaryLabel.Text = string.Empty;
            HideRenamePreview();
            UpdateStageOneListHeights();
        }
    }

    private void UpdatePrimaryModelNameState(bool clearWhenNoSource)
    {
        var hasSource = !string.IsNullOrWhiteSpace(primaryModelText.Text);
        sourceInstructionLabel.Visible = !hasSource;
        releaseStemText.Enabled = hasSource;
        if (!hasSource)
        {
            if (clearWhenNoSource)
            {
                releaseStemText.Text = string.Empty;
            }

            HideRenamePreview();
        }

        UpdateReleaseStemFeedback();
    }

    private void ConfigurePathLists()
    {
        materialsList.HorizontalScrollbar = true;
    }

    private void ConfigureExtrasHeaderDrawing()
    {
        extrasList.OwnerDraw = true;
        extrasList.DrawColumnHeader += (_, e) =>
        {
            using var background = new SolidBrush(ExtrasHeaderBackgroundColor);
            using var headerFont = new Font(extrasList.Font, ExtrasHeaderFontStyle);
            e.Graphics.FillRectangle(background, e.Bounds);
            TextRenderer.DrawText(
                e.Graphics,
                e.Header?.Text ?? string.Empty,
                headerFont,
                new Rectangle(e.Bounds.X + 6, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height),
                SystemColors.ControlText,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            e.Graphics.DrawLine(SystemPens.ControlDark, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
        };
        extrasList.DrawItem += (_, e) => e.DrawDefault = true;
        extrasList.DrawSubItem += (_, e) => e.DrawDefault = true;
    }

    private static void RefreshHorizontalExtents(params ListBox[] lists)
    {
        foreach (var list in lists)
        {
            var maxWidth = 0;
            foreach (var item in list.Items)
            {
                maxWidth = Math.Max(maxWidth, TextRenderer.MeasureText(item.ToString(), list.Font).Width);
            }

            list.HorizontalExtent = maxWidth + SystemInformation.VerticalScrollBarWidth + 8;
        }
    }

    private void UpdateReadmeModeControls()
    {
        var generated = readmeGeneratedRadio.Checked;
        var custom = readmeCustomRadio.Checked;
        var imported = readmeImportRadio.Checked;
            foreach (var control in new Control[] { readmeDescriptionText, readmeAuthorText, readmeWebsiteText, creditsText, readmeLicenseText, readmeResourcesText })
            control.Enabled = generated;
        importedReadmeLabel.Visible = imported;
        importedReadmeText.Visible = imported;
        importedReadmeText.Enabled = imported;
        importedReadmeBrowseButton.Visible = imported;
        importedReadmeHelperLabel.Visible = imported;
        customReadmeHeaderPanel.Visible = custom;
        customReadmeLabel.Visible = custom;
        customReadmeText.Visible = custom;
        customReadmeText.Enabled = custom;
        customReadmeHelperLabel.Visible = custom;
        startFromGeneratedButton.Visible = custom;
        startFromGeneratedHelperLabel.Visible = false;
        noReadmeStateLabel.Visible = readmeNoneRadio.Checked;
        if (readmeDetailsGroup is not null)
        {
            readmeDetailsGroup.Visible = generated;
        }
    }

    private void BrowseOutputFolder(TextBox textBox)
    {
        using var dialog = new FolderBrowserDialog { SelectedPath = Directory.Exists(textBox.Text) ? textBox.Text : string.Empty };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            textBox.Text = dialog.SelectedPath;
            settings.DefaultOutputDirectory = dialog.SelectedPath;
        }
    }

    private void BrowsePrimaryModel(TextBox textBox)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Model files|*.mdl|All files|*.*",
            InitialDirectory = GetModelBrowseStart(primaryModelText.Text)
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            SelectPrimaryModelForPackage(dialog.FileName);
        }
    }

    private string GetModelBrowseStart(string currentModelPath) =>
        browseStartResolver.ResolveSourceStart(currentModelPath, lastModelBrowseFolder, settings.DefaultSfmContentFolder, Directory.Exists);

    private string GetMaterialBrowseStart() =>
        browseStartResolver.ResolveMaterialsStart(lastMaterialBrowseFolder, settings.DefaultSfmContentFolder, Directory.Exists);

    private void SelectPrimaryModelForPackage(string file)
    {
        SyncProjectFromControls();
        var primary = project.Models.FirstOrDefault(model => model.Role == ModelRole.Primary);
        var sourceChanged = primary is null
            || !string.Equals(NormalizePathForComparison(primary.SourceMdlPath), NormalizePathForComparison(file), StringComparison.OrdinalIgnoreCase);
        if (primary is not null && sourceChanged)
        {
            RemoveAutomaticMaterialContribution(primary.Id);
            primary.Companions.Clear();
            primary.SourceReference = null;
        }

        var previousLoading = loadingControls;
        loadingControls = true;
        try
        {
            primaryModelText.Text = file;
            lastModelBrowseFolder = Path.GetDirectoryName(file) ?? lastModelBrowseFolder;
            if (sourceChanged || string.IsNullOrWhiteSpace(releaseStemText.Text))
            {
                releaseStemText.Text = Path.GetFileNameWithoutExtension(file);
            }
            UpdatePrimaryModelNameState(clearWhenNoSource: false);
        }
        finally
        {
            loadingControls = previousLoading;
        }

        SyncPrimaryModelFromControls();
        primary = project.Models.FirstOrDefault(model => model.Role == ModelRole.Primary);
        if (primary is not null)
        {
            AddDiscoveredMaterialSourcesForModel(primary);
        }

        var plan = TryPreviewPackageForPresentation(syncControls: false);
        CachePrimaryModelPlanEntries(plan);
        RefreshPrimaryModelPackagePresentation();
        RefreshListsForPlan(plan);
        MarkDirty();
    }

    private void BrowseImportedReadme(TextBox textBox)
    {
        using var dialog = new OpenFileDialog { Filter = "Text files|*.txt|All files|*.*" };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            textBox.Text = dialog.FileName;
            readmeImportRadio.Checked = true;
        }
    }

    private void AddAdditionalModel()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Model files|*.mdl|All files|*.*",
            Multiselect = true,
            InitialDirectory = GetModelBrowseStart(string.Empty)
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        AddAdditionalModelsForPackage(dialog.FileNames);
    }

    private void AddAdditionalModelsForPackage(IEnumerable<string> files)
    {
        var added = false;
        var duplicateSeen = false;
        foreach (var file in files)
        {
            if (IsModelAlreadyInPackage(file))
            {
                duplicateSeen = true;
                continue;
            }

            var model = new ModelEntry
            {
                Role = ModelRole.Additional,
                SourceMdlPath = file,
                SourceStem = Path.GetFileNameWithoutExtension(file),
                ReleaseStem = string.Empty
            };
            project.Models.Add(model);
            lastModelBrowseFolder = Path.GetDirectoryName(file) ?? lastModelBrowseFolder;
            AddDiscoveredMaterialSourcesForModel(model);
            added = true;
        }

        if (duplicateSeen)
        {
            ShowDuplicateModelFeedback("This model is already in the package.");
        }

        if (added)
        {
            RefreshLists();
            MarkDirty();
        }
    }

    private bool IsModelAlreadyInPackage(string candidatePath)
    {
        var candidate = NormalizePathForComparison(candidatePath);
        return project.Models.Any(model =>
            !string.IsNullOrWhiteSpace(model.SourceMdlPath)
            && string.Equals(NormalizePathForComparison(model.SourceMdlPath), candidate, StringComparison.OrdinalIgnoreCase));
    }

    private void ShowDuplicateModelFeedback(string message)
    {
        if (duplicateModelFeedback is not null)
        {
            duplicateModelFeedback(message);
            return;
        }

        MessageBox.Show(this, message, "Additional Model", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static string NormalizePathForComparison(string path)
    {
        try
        {
            return Path.GetFullPath(path)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return path
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }
    }

    private int AddDiscoveredMaterialSourcesForModel(ModelEntry model)
    {
        var result = materialSourceDiscoveryService.Discover(model.SourceMdlPath, settings.DefaultSfmContentFolder);
        var added = 0;
        foreach (var source in result.MaterialSources)
        {
            var existing = FindMaterialSource(source.SourcePath);
            if (existing is not null)
            {
                if (existing.AutomaticMaterialModelIds.Count > 0 && !existing.AutomaticMaterialModelIds.Contains(model.Id))
                {
                    existing.AutomaticMaterialModelIds.Add(model.Id);
                }

                continue;
            }

            source.AutomaticMaterialModelIds.Add(model.Id);
            project.MaterialSources.Add(source);
            added++;
        }

        return added;
    }

    private SourceEntry? FindMaterialSource(string sourcePath) =>
        project.MaterialSources.FirstOrDefault(source => string.Equals(
            NormalizeMaterialSourcePath(source.SourcePath),
            NormalizeMaterialSourcePath(sourcePath),
            StringComparison.OrdinalIgnoreCase));

    private static string NormalizeMaterialSourcePath(string path)
    {
        try
        {
            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }

    private void RemoveAdditionalModel()
    {
        if (additionalModelsList.SelectedItems.Count == 1
            && additionalModelsList.SelectedItems[0].Tag is ModelListItem item)
        {
            RemoveAutomaticMaterialContribution(item.Model.Id);
            project.Models.Remove(item.Model);
            RefreshLists();
            MarkDirty();
        }
    }

    private void RemoveAutomaticMaterialContribution(Guid modelId)
    {
        for (var index = project.MaterialSources.Count - 1; index >= 0; index--)
        {
            var source = project.MaterialSources[index];
            if (source.AutomaticMaterialModelIds.Count == 0)
            {
                continue;
            }

            source.AutomaticMaterialModelIds.RemoveAll(id => id == modelId);
            if (source.AutomaticMaterialModelIds.Count == 0)
            {
                project.MaterialSources.RemoveAt(index);
            }
        }
    }

    private void AddMaterialFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            SelectedPath = GetMaterialBrowseStart()
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            project.MaterialSources.Add(new SourceEntry { Kind = SourceEntryKind.Material, SourcePath = dialog.SelectedPath, IsFolder = true, IncludeRecursively = true });
            lastMaterialBrowseFolder = dialog.SelectedPath;
            RefreshLists();
            MarkDirty();
        }
    }

    private void AddMaterialFiles()
    {
        using var dialog = new OpenFileDialog
        {
            Multiselect = true,
            Filter = "Material files|*.vmt;*.vtf|All files|*.*",
            InitialDirectory = GetMaterialBrowseStart()
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        foreach (var file in dialog.FileNames)
            project.MaterialSources.Add(new SourceEntry { Kind = SourceEntryKind.Material, SourcePath = file, IsFolder = false, IncludeRecursively = false });
        lastMaterialBrowseFolder = Path.GetDirectoryName(dialog.FileNames[0]) ?? lastMaterialBrowseFolder;
        RefreshLists();
        MarkDirty();
    }

    private void RemoveSelectedMaterial()
    {
        if (materialsList.SelectedItem is SourceListItem item)
        {
            project.MaterialSources.Remove(item.Source);
            RefreshLists();
            MarkDirty();
        }
    }

    private void AddExtraFile()
    {
        using var dialog = new OpenFileDialog
        {
            Multiselect = true,
            InitialDirectory = browseStartResolver.ResolveSourceStart(string.Empty, lastExtraBrowseFolder, settings.DefaultSfmContentFolder, Directory.Exists)
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        foreach (var file in dialog.FileNames)
        {
            project.Extras.Add(new SourceEntry { Kind = SourceEntryKind.Extra, SourcePath = file, IsFolder = false, IncludeRecursively = false, DestinationOverride = new DestinationOverride { Kind = DestinationOverrideKind.Root, RelativePath = string.Empty } });
        }
        lastExtraBrowseFolder = Path.GetDirectoryName(dialog.FileNames[0]) ?? lastExtraBrowseFolder;
        RefreshLists();
        MarkDirty();
    }

    private void AddExtraFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            SelectedPath = browseStartResolver.ResolveSourceStart(string.Empty, lastExtraBrowseFolder, settings.DefaultSfmContentFolder, Directory.Exists)
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            project.Extras.Add(new SourceEntry { Kind = SourceEntryKind.Extra, SourcePath = dialog.SelectedPath, IsFolder = true, IncludeRecursively = true, DestinationOverride = new DestinationOverride { Kind = DestinationOverrideKind.Root, RelativePath = string.Empty } });
            lastExtraBrowseFolder = dialog.SelectedPath;
            RefreshLists();
            MarkDirty();
        }
    }

    private void RemoveSelectedExtra()
    {
        if (extrasList.SelectedItems.Count == 1 && extrasList.SelectedItems[0].Tag is SourceListItem item)
        {
            project.Extras.Remove(item.Source);
            RefreshLists();
            MarkDirty();
        }
    }

    private void LoadSelectedExtraDestination()
    {
        if (extrasList.SelectedItems.Count != 1 || extrasList.SelectedItems[0].Tag is not SourceListItem item)
        {
            loadingControls = true;
            extraDestinationCombo.SelectedItem = null;
            extraCustomDestinationText.Text = string.Empty;
            extraDestinationCombo.Enabled = false;
            extraCustomDestinationText.Enabled = false;
            extraSelectionHelpLabel.Visible = true;
            SetCustomExtraDestinationVisible(false);
            loadingControls = false;
            return;
        }
        var destination = item.Source.DestinationOverride;
        loadingControls = true;
        extraDestinationCombo.Enabled = true;
        extraDestinationCombo.SelectedItem = extraDestinationPresenter.ChoiceFor(destination);
        extraCustomDestinationText.Text = extraDestinationPresenter.CustomFolderFor(destination);
        extraSelectionHelpLabel.Visible = false;
        SetCustomExtraDestinationVisible(extraDestinationCombo.SelectedItem?.ToString() == ExtraDestinationPresenter.CustomChoice);
        loadingControls = false;
    }

    private void SaveSelectedExtraDestination()
    {
        if (loadingControls || extrasList.SelectedItems.Count != 1 || extrasList.SelectedItems[0].Tag is not SourceListItem item || extraDestinationCombo.SelectedItem is null) return;
        var choice = extraDestinationCombo.SelectedItem.ToString() ?? ExtraDestinationPresenter.PackageRootChoice;
        SetCustomExtraDestinationVisible(choice == ExtraDestinationPresenter.CustomChoice);
        item.Source.DestinationOverride = extraDestinationPresenter.ToOverride(choice, extraCustomDestinationText.Text);
        extrasList.SelectedItems[0].SubItems[1].Text = extraDestinationPresenter.PackageLocationFor(item.Source);
        MarkDirty();
    }

    private void SetCustomExtraDestinationVisible(bool visible)
    {
        extraCustomDestinationLabel.Visible = visible;
        extraCustomDestinationText.Visible = visible;
        extraCustomDestinationText.Enabled = visible;
        extraCustomDestinationHelperLabel.Visible = visible;
    }

    private void PreviewReadme()
    {
        if (!EnsureModelNameEditsCommitted())
        {
            return;
        }

        SyncProjectFromControls();
        var preview = CreateCoordinator().PreviewReadme(project);
        var text = preview.PreviewText ?? string.Empty;
        var form = new ReadmePreviewForm();
        form.UpdatePreview(text, preview.ReadmeEntry?.ReadmeKind == ReadmePlanEntryKind.GeneratedText);
        form.UseAsCustomRequested += (_, _) =>
        {
            project.Readme = new ReadmeConversionService().UseGeneratedTextAsCustomReadme(project.Readme, form.CurrentText);
            LoadReadmeControls();
            workflowTabs.SelectedIndex = 2;
            MarkDirty();
            form.Close();
        };
        form.Show(this);
    }

    private void StartCustomReadmeFromGenerated()
    {
        if (!EnsureModelNameEditsCommitted())
        {
            return;
        }

        SyncProjectFromControls();
        var customConfig = project.Readme;
        project.Readme = new ReadmeConfig
        {
            Mode = ReadmeMode.Generated,
            Description = readmeDescriptionText.Text,
            Author = readmeAuthorText.Text,
            Website = readmeWebsiteText.Text,
            License = readmeLicenseText.Text,
            AdditionalResources = readmeResourcesText.Text
        };

        var generated = CreateCoordinator().PreviewReadme(project).PreviewText ?? string.Empty;
        project.Readme = new ReadmeConversionService().UseGeneratedTextAsCustomReadme(customConfig, generated);
        LoadReadmeControls();
        MarkDirty();
    }

    private void PreviewPackage()
    {
        if (!EnsureModelNameEditsCommitted())
        {
            return;
        }

        SyncProjectFromControls();
        using var form = new PackagePreviewForm(CreateCoordinator().PreviewPackage(project));
        form.ShowDialog(this);
    }

    private void RefreshReviewAndBuild()
    {
        if (refreshingReview)
        {
            return;
        }

        refreshingReview = true;
        try
        {
            if (!EnsureModelNameEditsCommitted())
            {
                return;
            }

            SyncProjectFromControls();
            if (project.Models.All(model => string.IsNullOrWhiteSpace(model.SourceMdlPath)))
            {
                reviewStatusLabel.Text = "Add a model to review the files and folders that will be included in the ZIP.";
                reviewStatusLabel.Visible = true;
                reviewPackageTree.Nodes.Clear();
                reviewPackageDetailsText.Text = "Select a file to see its source and package destination.";
                reviewCheckList.Items.Clear();
                reviewCheckSummaryLabel.Text = string.Empty;
                reviewCheckDetailsText.Text = string.Empty;
                reviewCheckList.Visible = false;
                reviewCheckDetailsHostPanel.Visible = false;
                reviewCheckDetailsText.Visible = false;
                buildZipButton.Enabled = false;
                UpdateWorkflowNavigation();
                return;
            }

            var coordinator = CreateCoordinator();
            var plan = coordinator.PreviewPackage(project);
            LoadReviewTree(plan);
            var check = coordinator.CheckPackage(project, outputFolderText.Text.Trim());
            LoadReviewCheckWithPlan(check.Validation, plan);
            reviewStatusLabel.Text = string.Empty;
            reviewStatusLabel.Visible = false;
        }
        catch (Exception ex)
        {
            reviewStatusLabel.Text = "Package review could not be refreshed.";
            reviewStatusLabel.Visible = true;
            reviewPackageTree.Nodes.Clear();
            reviewPackageDetailsText.Text = ex.Message;
            reviewCheckList.Items.Clear();
            reviewCheckSummaryLabel.Text = string.Empty;
            reviewCheckDetailsText.Text = string.Empty;
            reviewCheckList.Visible = false;
            reviewCheckDetailsHostPanel.Visible = false;
            reviewCheckDetailsText.Visible = false;
            buildZipButton.Enabled = false;
            UpdateWorkflowNavigation();
        }
        finally
        {
            refreshingReview = false;
        }
    }

    private void LoadReviewTree(PackagePlan plan)
    {
        RememberReviewStateBeforeRefresh();
        var previousDestination = reviewSelectedDestination;
        var previousExpandedPaths = reviewExpandedPaths.ToArray();
        var previousTopPath = reviewTopPath;

        reviewPackageTree.BeginUpdate();
        try
        {
            reviewPackageTree.Nodes.Clear();
            foreach (var node in packagePlanTreeBuilder.Build(plan))
            {
                reviewPackageTree.Nodes.Add(ToTreeNode(node));
            }

            if (previousExpandedPaths.Length == 0
                && string.IsNullOrWhiteSpace(previousTopPath)
                && string.IsNullOrWhiteSpace(previousDestination))
            {
                reviewPackageTree.ExpandAll();
            }
            else
            {
                RestoreReviewExpansion(reviewPackageTree.Nodes, previousExpandedPaths);
            }

            TreeNode? restoredSelection = null;
            if (!string.IsNullOrWhiteSpace(previousDestination))
            {
                restoredSelection = FindNodeByDestination(reviewPackageTree.Nodes, previousDestination);
            }

            var restoredTop = string.IsNullOrWhiteSpace(previousTopPath)
                ? null
                : FindNodeByReviewPath(reviewPackageTree.Nodes, previousTopPath);

            if (restoredSelection is not null)
            {
                reviewPackageTree.SelectedNode = restoredSelection;
                reviewSelectedDestination = previousDestination;
                if (restoredTop is null)
                {
                    restoredSelection.EnsureVisible();
                }
            }
            else
            {
                reviewPackageTree.SelectedNode = null;
                reviewSelectedDestination = null;
                reviewPackageDetailsText.Text = "Select a file to see its source and package destination.";
            }

            if (restoredTop is not null)
            {
                reviewPackageTree.TopNode = restoredTop;
            }
            else if (reviewPackageTree.Nodes.Count > 0 && restoredSelection is null)
            {
                reviewPackageTree.TopNode = reviewPackageTree.Nodes[0];
            }
        }
        finally
        {
            reviewPackageTree.EndUpdate();
        }

        RememberReviewExpansionState();
        reviewTopPath = GetReviewNodePath(reviewPackageTree.TopNode);
    }

    private string? GetSelectedReviewDestination()
    {
        return reviewPackageTree.SelectedNode?.Tag is PackageTreeNodeModel { Entry: { DestinationRelativePath: not null } entry }
            ? entry.DestinationRelativePath
            : null;
    }

    private static TreeNode? FindNodeByDestination(TreeNodeCollection nodes, string destination)
    {
        foreach (TreeNode node in nodes)
        {
            if (node.Tag is PackageTreeNodeModel { Entry: { DestinationRelativePath: not null } entry }
                && string.Equals(entry.DestinationRelativePath, destination, StringComparison.OrdinalIgnoreCase))
            {
                return node;
            }

            var child = FindNodeByDestination(node.Nodes, destination);
            if (child is not null)
            {
                return child;
            }
        }

        return null;
    }

    private void RememberReviewStateBeforeRefresh()
    {
        reviewSelectedDestination = GetSelectedReviewDestination() ?? reviewSelectedDestination;
        reviewTopPath = GetReviewNodePath(reviewPackageTree.TopNode) ?? reviewTopPath;
        RememberReviewExpansionState();
    }

    private void RememberReviewExpansionState()
    {
        reviewExpandedPaths.Clear();
        AddExpandedReviewPaths(reviewPackageTree.Nodes, reviewExpandedPaths);
    }

    private static void AddExpandedReviewPaths(TreeNodeCollection nodes, ISet<string> expandedPaths)
    {
        foreach (TreeNode node in nodes)
        {
            if (node.IsExpanded && GetReviewNodePath(node) is { Length: > 0 } path)
            {
                expandedPaths.Add(path);
            }

            AddExpandedReviewPaths(node.Nodes, expandedPaths);
        }
    }

    private static void RestoreReviewExpansion(TreeNodeCollection nodes, IReadOnlyCollection<string> expandedPaths)
    {
        foreach (TreeNode node in nodes)
        {
            if (GetReviewNodePath(node) is { Length: > 0 } path && expandedPaths.Contains(path))
            {
                node.Expand();
            }

            RestoreReviewExpansion(node.Nodes, expandedPaths);
        }
    }

    private static TreeNode? FindNodeByReviewPath(TreeNodeCollection nodes, string path)
    {
        foreach (TreeNode node in nodes)
        {
            if (string.Equals(GetReviewNodePath(node), path, StringComparison.OrdinalIgnoreCase))
            {
                return node;
            }

            var child = FindNodeByReviewPath(node.Nodes, path);
            if (child is not null)
            {
                return child;
            }
        }

        return null;
    }

    private static string? GetReviewNodePath(TreeNode? node)
    {
        if (node?.Tag is not PackageTreeNodeModel model)
        {
            return node?.FullPath;
        }

        var path = model.Entry?.DestinationRelativePath ?? model.FullPath;
        return string.IsNullOrWhiteSpace(path)
            ? null
            : path.Replace('/', '\\').Trim('\\');
    }

    private static TreeNode ToTreeNode(PackageTreeNodeModel model)
    {
        var node = new TreeNode(model.Name) { Tag = model };
        foreach (var child in model.Children)
        {
            node.Nodes.Add(ToTreeNode(child));
        }

        return node;
    }

    private void ShowReviewPackageDetail()
    {
        if (reviewPackageTree.SelectedNode?.Tag is not PackageTreeNodeModel model)
        {
            reviewPackageDetailsText.Text = "Select a file to see its source and package destination.";
            return;
        }

        if (model.Entry is null)
        {
            reviewPackageDetailsText.Text = "Package destination:" + Environment.NewLine + model.FullPath;
            return;
        }

        var source = model.Entry.EntryType == PackagePlanEntryType.Readme && model.Entry.SourcePath is null
            ? "Generated"
            : model.Entry.SourcePath ?? "Generated";
        reviewPackageDetailsText.Text =
            "Source:" + Environment.NewLine +
            source + Environment.NewLine + Environment.NewLine +
            "Package destination:" + Environment.NewLine +
            (model.Entry.DestinationRelativePath ?? model.FullPath) +
            CreatePackageProblemText(model.Entry);
    }

    private static string CreatePackageProblemText(PackagePlanEntry entry)
    {
        if (entry.Status == PackagePlanEntryStatus.Resolved)
        {
            return string.Empty;
        }

        return Environment.NewLine + Environment.NewLine +
            "Problem:" + Environment.NewLine +
            DescribePackageEntryProblem(entry.Status);
    }

    private static string DescribePackageEntryProblem(PackagePlanEntryStatus status) => status switch
    {
        PackagePlanEntryStatus.MissingSource => "A selected source could not be found.",
        PackagePlanEntryStatus.MissingAnchor => "This source needs a package location.",
        PackagePlanEntryStatus.InvalidDestinationOverride => "The package location is invalid.",
        PackagePlanEntryStatus.SourceEnumerationFailed => "A selected folder could not be read.",
        PackagePlanEntryStatus.SourceReadFailed => "A selected file could not be read.",
        PackagePlanEntryStatus.ReadmeResolutionFailed => "The README could not be prepared.",
        _ => "This item needs attention."
    };

    private void LoadReviewCheck(ValidationResult result) => LoadReviewCheckWithPlan(result, null);

    private void LoadReviewCheckWithPlan(ValidationResult result, PackagePlan? plan)
    {
        reviewCheckList.Items.Clear();
        reviewCheckDetailsText.Text = string.Empty;
        buildZipButton.Enabled = !result.HasErrors;
        UpdateWorkflowNavigation();
        if (result.HasErrors)
        {
            ShowReviewCheckRows("Fix the problems below before building the ZIP.");
        }
        else if (result.HasRequiredDecisions || result.Messages.Any(message => message.Severity == ValidationSeverity.Warning))
        {
            ShowReviewCheckRows("You can build the ZIP. Review the warnings below.");
        }
        else
        {
            var noteCount = result.Messages.Count(message => message.Severity == ValidationSeverity.Information);
            reviewCheckSummaryLabel.Text = "Ready to build the ZIP.";
            reviewCheckSummaryLabel.ForeColor = Color.DarkGreen;
            if (noteCount == 0)
            {
                reviewCheckList.Visible = false;
                reviewCheckDetailsHostPanel.Visible = false;
                reviewCheckDetailsText.Visible = false;
                return;
            }

            reviewCheckList.Visible = true;
            reviewCheckDetailsHostPanel.Visible = true;
            reviewCheckDetailsText.Visible = true;
        }

        foreach (var item in validationPresenter.Present(result, project, plan).Where(item => item.Severity != ValidationSeverity.Ready))
        {
            AddReviewCheckItem(item.Severity, item.Title, item.Details);
        }

        UpdateReviewCheckColumns();
    }

    private void ShowReviewCheckRows(string summary)
    {
        reviewCheckSummaryLabel.Text = summary;
        reviewCheckSummaryLabel.ForeColor = SystemColors.ControlText;
        reviewCheckList.Visible = true;
        reviewCheckDetailsHostPanel.Visible = true;
        reviewCheckDetailsText.Visible = true;
        reviewCheckDetailsText.Text = string.Empty;
    }

    private void AddReviewCheckItem(ValidationSeverity severity, string title, string details)
    {
        var row = new ListViewItem(ValidationPresenter.FormatStatus(severity));
        row.SubItems.Add(title);
        row.Tag = details;
        row.ForeColor = severity switch
        {
            ValidationSeverity.Error => Color.Firebrick,
            ValidationSeverity.Warning => Color.DarkGoldenrod,
            ValidationSeverity.Information => Color.DarkBlue,
            _ => Color.DarkGreen
        };
        reviewCheckList.Items.Add(row);
    }

    private void CheckPackage()
    {
        if (!EnsureModelNameEditsCommitted())
        {
            return;
        }

        SyncProjectFromControls();
        var result = CreateCoordinator().CheckPackage(project, outputFolderText.Text.Trim());
        using var form = new CheckResultsForm(result.Validation);
        form.ShowDialog(this);
    }

    private async Task BuildZipAsync()
    {
        if (!EnsureModelNameEditsCommitted())
        {
            return;
        }

        SyncProjectFromControls();
        var versionDecision = VersionReuseDecision.None;
        var outputDecision = ExistingOutputDecision.None;

        while (true)
        {
            var request = new BuildRequest
            {
                Project = project,
                OutputDirectory = outputFolderText.Text.Trim(),
                VersionReuseDecision = versionDecision,
                ExistingOutputDecision = outputDecision
            };

            using var cancellation = new CancellationTokenSource();
            using var progressForm = new BuildProgressForm(cancellation.Cancel);
            var progress = new Progress<BuildProgress>(progressForm.Report);
            progressForm.Show(this);
            Enabled = false;
            BuildSummary summary;
            try
            {
                summary = await CreateCoordinator().BuildAsync(request, progress, cancellation.Token);
            }
            finally
            {
                Enabled = true;
                progressForm.AllowClose();
                progressForm.Close();
            }

            if (summary.Status == BuildStatus.DecisionRequired)
            {
                if (summary.RequiredDecisions.Any(decision => decision.Kind == RequiredDecisionKind.VersionReuse))
                {
                    var choice = ShowVersionDecision();
                    versionDecision = decisionPresenter.MapVersionChoice(choice);
                    if (versionDecision == VersionReuseDecision.ChangeVersion)
                    {
                        versionText.Focus();
                        return;
                    }
                    if (versionDecision == VersionReuseDecision.Cancel) return;
                    continue;
                }

                if (summary.RequiredDecisions.Any(decision => decision.Kind == RequiredDecisionKind.ExistingOutputArchive))
                {
                    var choice = ShowOutputDecision();
                    outputDecision = decisionPresenter.MapOutputChoice(choice);
                    if (outputDecision == ExistingOutputDecision.ChooseAnotherName)
                    {
                        archiveNameText.Focus();
                        return;
                    }
                    if (outputDecision == ExistingOutputDecision.Cancel) return;
                    continue;
                }
            }

            var wasDirtyBeforeBuild = isDirty;
            ShowBuildSummary(summary);
            if (summary.Succeeded)
            {
                ApplyBuildHistoryPersistence(wasDirtyBeforeBuild);
            }
            return;
        }
    }

    private VersionReuseChoice ShowVersionDecision()
    {
        var result = MessageBox.Show(this, $"Version {project.CurrentVersion} already exists in this project.\r\n\r\nYes = Rebuild {project.CurrentVersion}\r\nNo = Change Version\r\nCancel = Cancel", "Version Exists", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        return result switch
        {
            DialogResult.Yes => VersionReuseChoice.Rebuild,
            DialogResult.No => VersionReuseChoice.ChangeVersion,
            _ => VersionReuseChoice.Cancel
        };
    }

    private ExistingOutputChoice ShowOutputDecision()
    {
        var result = MessageBox.Show(this, "The output ZIP already exists.\r\n\r\nYes = Replace\r\nNo = Choose Another Name\r\nCancel = Cancel", "Output Exists", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        return result switch
        {
            DialogResult.Yes => ExistingOutputChoice.Replace,
            DialogResult.No => ExistingOutputChoice.ChooseAnotherName,
            _ => ExistingOutputChoice.Cancel
        };
    }

    private void ShowBuildSummary(BuildSummary summary)
    {
        if (summary.Succeeded)
        {
            if (summary.ArchivePath is not null)
            {
                var cleanup = summary.CleanupResult is { Succeeded: false } ? summary.CleanupResult.TechnicalDetail : null;
                using var form = new BuildResultForm(summary.ArchivePath, cleanup, NewPackage);
                form.ShowDialog(this);
                if (!form.StartedNewPackage)
                {
                    ShowLastBuild(summary.ArchivePath);
                }
            }

            return;
        }

        if (summary.Status == BuildStatus.ValidationFailed && summary.Validation is not null)
        {
            using var form = new CheckResultsForm(summary.Validation);
            form.ShowDialog(this);
            return;
        }

        if (summary.Status == BuildStatus.Cancelled)
        {
            MessageBox.Show(
                this,
                "Build cancelled.\r\n\r\nYour source files were not changed.",
                "Build ZIP",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var details = string.Join(Environment.NewLine, summary.Log.Entries.Select(entry => $"{entry.Stage}: {entry.Message} {entry.TechnicalDetail}"));
        MessageBox.Show(this, "Build failed: " + summary.Status + Environment.NewLine + Environment.NewLine + details, "Build ZIP", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void ApplyBuildHistoryPersistence(bool wasDirtyBeforeBuild)
    {
        var result = buildHistoryPersistencePolicy.Apply(
            buildSucceeded: true,
            wasDirtyBeforeBuild,
            projectPath,
            () =>
            {
                if (projectPath is null)
                {
                    return;
                }

                projectSerializer.Save(projectPath, project);
                TrackRecent(projectPath);
                TrackProjectDirectory(projectPath);
            });

        SetDirty(result.ShouldBeDirty);
        if (result.Status == BuildHistoryPersistenceStatus.Failed)
        {
            MessageBox.Show(
                this,
                "The ZIP was built successfully, but the build history could not be saved to the .sfmpack project yet.\r\n\r\nSave the project later to keep that history.\r\n\r\n" + result.Message,
                "Build History",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }

    private void RunRecoveryIfNeeded()
    {
        ShowRecovery(showNoIssuesMessage: false);
    }

    private void ShowRecovery(bool showNoIssuesMessage)
    {
        var service = new MissingSourceRecoveryService(new PhysicalFileSystem());
        if (!service.Inspect(project).HasIssues)
        {
            if (showNoIssuesMessage)
            {
                MessageBox.Show(this, "All project files are available.", "Locate Missing Files", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            return;
        }

        using var form = new RecoveryForm(project, service, projectPath);
        form.ShowDialog(this);
        if (form.ProjectChanged)
        {
            LoadProjectIntoControls();
            MarkDirty();
        }
    }

    private bool HasMissingSources()
    {
        var service = new MissingSourceRecoveryService(new PhysicalFileSystem());
        return service.Inspect(project).HasIssues;
    }

    private void TrackRecent(string path)
    {
        recentProjectsService.AddOrPromote(settings, path, project.AssetName);
        RefreshRecentProjects();
    }

    private void TrackProjectDirectory(string path)
    {
        if (projectDirectoryPreferenceService.RecordSuccessfulProjectPath(settings, path))
        {
            settingsService.Save(settings);
        }
    }

    private void RefreshReviewIfVisible()
    {
        if (workflowTabs.SelectedTab == reviewTab)
        {
            RefreshReviewAndBuild();
        }
    }

    private void ShowLastBuild(string archivePath)
    {
        lastBuildOutputFolder = Path.GetDirectoryName(archivePath);
        lastBuildStatusLabel.Text = "✓ Last build completed: " + Path.GetFileName(archivePath);
        lastBuildStatusLabel.Visible = true;
        lastBuildOpenFolderButton.Visible = !string.IsNullOrWhiteSpace(lastBuildOutputFolder);
    }

    private void OpenLastBuildFolder()
    {
        if (string.IsNullOrWhiteSpace(lastBuildOutputFolder))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo { FileName = lastBuildOutputFolder, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Could not open the output folder.\r\n\r\n" + lastBuildOutputFolder + "\r\n\r\n" + ex.Message, "Open folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void RefreshRecentProjects()
    {
        recentList.Controls.Clear();

        var recents = recentProjectsService.GetRecentProjects(settings).ToArray();
        if (recents.Length == 0)
        {
            recentList.Visible = false;
            recentEmptyStateLabel.Visible = true;
            recentEmptyStateLabel.BringToFront();
            UpdateLaunchLayout();
            return;
        }

        recentEmptyStateLabel.Visible = false;
        recentList.Visible = true;
        foreach (var recent in recents)
        {
            if (recentProjectsService.IsMissing(recent))
            {
                recentList.Controls.Add(MissingRecentProjectControl(recent));
            }
            else
            {
                recentList.Controls.Add(RecentProjectCard(recent));
            }
        }

        UpdateLaunchLayout();
    }

    private void UpdateRecentGridLayout()
    {
        if (recentList.IsDisposed)
        {
            return;
        }

        var width = recentList.ClientSize.Width > 0 ? recentList.ClientSize.Width : recentWorkspacePanel.ClientSize.Width;
        if (width <= 0)
        {
            return;
        }

        var availableWidth = Math.Max(1, width - recentList.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth - 2);
        var columns = availableWidth >= 390 ? 3 : availableWidth >= 260 ? 2 : 1;
        recentGridColumnCount = columns;
        var horizontalMarginPerCard = 12;
        var cardWidth = Math.Max(120, (availableWidth - columns * horizontalMarginPerCard) / columns);
        recentCardWidth = cardWidth;
        foreach (Control card in recentList.Controls)
        {
            card.Width = cardWidth;
            card.Height = 92;
        }
    }

    private Control RecentProjectCard(RecentProjectEntry recent)
    {
        var metadata = ReadRecentProjectCardMetadata(recent);
        var panel = RecentCardShell(recent);
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(8)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 27));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 28));

        var title = new Label
        {
            Text = metadata.Title,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font(Font.FontFamily, Font.Size + 1, FontStyle.Bold),
            AutoEllipsis = true
        };
        layout.Controls.Add(title, 0, 0);

        if (!string.IsNullOrWhiteSpace(metadata.Version))
        {
            layout.Controls.Add(SecondaryRecentLabel("v" + metadata.Version), 0, 1);
        }

        layout.Controls.Add(SecondaryRecentLabel(metadata.DateText), 0, 2);
        panel.Controls.Add(layout);
        AttachRecentOpenHandler(panel, recent.ProjectPath);
        AttachRecentHoverHandlers(panel, panel);
        SetRecentTooltip(panel, metadata.Tooltip);
        return panel;
    }

    private Control MissingRecentProjectControl(RecentProjectEntry recent)
    {
        var panel = RecentCardShell(recent);
        var layout = new TableLayoutPanel
        {
            ColumnCount = 3,
            RowCount = 2,
            Dock = DockStyle.Fill,
            Padding = new Padding(8)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 45));

        var title = new Label
        {
            Text = RecentDisplayName(recent),
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font(Font.FontFamily, Font.Size, FontStyle.Bold),
            AutoEllipsis = true
        };
        var missing = new Label
        {
            Text = "Project file not found",
            AutoSize = false,
            Dock = DockStyle.Fill,
            ForeColor = SystemColors.GrayText,
            TextAlign = ContentAlignment.MiddleCenter
        };
        var locate = new Button { Text = "Locate", AutoSize = true };
        var remove = new Button { Text = "Remove", AutoSize = true };

        locate.Click += (_, _) => LocateRecentProject(recent);
        remove.Click += (_, _) =>
        {
            recentProjectsService.Remove(settings, recent.ProjectPath);
            RefreshRecentProjects();
        };

        layout.Controls.Add(title, 0, 0);
        layout.Controls.Add(locate, 1, 0);
        layout.Controls.Add(remove, 2, 0);
        layout.Controls.Add(missing, 0, 1);
        layout.SetColumnSpan(missing, 3);
        panel.Controls.Add(layout);
        AttachRecentHoverHandlers(panel, panel);
        toolTip.SetToolTip(panel, recent.ProjectPath);
        toolTip.SetToolTip(title, recent.ProjectPath);
        toolTip.SetToolTip(missing, recent.ProjectPath);
        return panel;
    }

    private Panel RecentCardShell(RecentProjectEntry recent) => new()
    {
        Width = 150,
        Height = 92,
        Margin = new Padding(6),
        Padding = new Padding(0),
        BorderStyle = BorderStyle.None,
        BackColor = RecentCardNormalBackColor,
        Cursor = Cursors.Hand,
        Tag = recent
    };

    private Label SecondaryRecentLabel(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleCenter,
        ForeColor = SystemColors.GrayText,
        AutoEllipsis = true
    };

    private void AttachRecentOpenHandler(Control control, string projectFilePath)
    {
        control.Click += (_, _) => OpenPackageWithLifecycle(projectFilePath);
        foreach (Control child in control.Controls)
        {
            if (child is not Button)
            {
                AttachRecentOpenHandler(child, projectFilePath);
            }
        }
    }

    private void AttachRecentHoverHandlers(Control control, Control card)
    {
        control.MouseEnter += (_, _) => ApplyRecentCardHover(card, hovered: true);
        control.MouseLeave += (_, _) =>
        {
            if (!card.ClientRectangle.Contains(card.PointToClient(Cursor.Position)))
            {
                ApplyRecentCardHover(card, hovered: false);
            }
        };
        control.Cursor = Cursors.Hand;
        foreach (Control child in control.Controls)
        {
            AttachRecentHoverHandlers(child, card);
        }
    }

    private void ApplyRecentCardHover(Control card, bool hovered)
    {
        card.BackColor = hovered ? RecentCardHoverBackColor : RecentCardNormalBackColor;
        foreach (Control child in card.Controls)
        {
            child.BackColor = card.BackColor;
        }
    }

    private void SetRecentTooltip(Control control, string text)
    {
        toolTip.SetToolTip(control, text);
        foreach (Control child in control.Controls)
        {
            SetRecentTooltip(child, text);
        }
    }

    private RecentProjectCardMetadata ReadRecentProjectCardMetadata(RecentProjectEntry recent)
    {
        var displayName = RecentDisplayName(recent);
        var version = string.Empty;
        try
        {
            var result = projectSerializer.Load(recent.ProjectPath);
            if (result.IsSuccess && result.Project is not null)
            {
                displayName = string.IsNullOrWhiteSpace(result.Project.AssetName)
                    ? displayName
                    : result.Project.AssetName.Trim();
                version = result.Project.CurrentVersion.Trim();
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
        }

        var date = File.Exists(recent.ProjectPath)
            ? File.GetLastWriteTime(recent.ProjectPath)
            : recent.LastAccessed.LocalDateTime;
        return new RecentProjectCardMetadata(
            displayName,
            version,
            FormatRecentDate(date),
            string.IsNullOrWhiteSpace(version)
                ? recent.ProjectPath
                : $"{displayName}\r\nv{version}\r\n{recent.ProjectPath}");
    }

    private static string FormatRecentDate(DateTime date)
    {
        var format = date.Year == DateTime.Now.Year ? "MMM d" : "MMM d, yyyy";
        return date.ToString(format, CultureInfo.CurrentCulture);
    }

    private void LocateRecentProject(RecentProjectEntry recent)
    {
        if (ResolveUnsavedWork(UnsavedWorkAction.OpenProject) == UnsavedWorkResult.Cancelled)
        {
            return;
        }

        using var dialog = new OpenFileDialog
        {
            Filter = "SFM Package Builder project|*.sfmpack|All files|*.*",
            InitialDirectory = GetProjectOpenStartDirectory()
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        if (!TryLoadPackage(dialog.FileName, out var loadedProject))
        {
            return;
        }

        CommitProjectSession(loadedProject!, dialog.FileName, dirtyAfterCommit: false);
        recentProjectsService.ReplacePath(settings, recent.ProjectPath, dialog.FileName, loadedProject!.AssetName);
        RefreshRecentProjects();
        TrackProjectDirectory(dialog.FileName);
        RunRecoveryIfNeeded();
    }

    private static string RecentDisplayName(RecentProjectEntry recent) =>
        string.IsNullOrWhiteSpace(recent.DisplayName)
            ? Path.GetFileNameWithoutExtension(recent.ProjectPath)
            : recent.DisplayName;

    private sealed record AdditionalModelCompanionDetail(int Count, IReadOnlyList<string> FileNames);

    private sealed record RecentProjectCardMetadata(string Title, string Version, string DateText, string Tooltip);

    private enum ModelNameEditTarget
    {
        None,
        Primary,
        Additional
    }

    private sealed class BufferedTableLayoutPanel : TableLayoutPanel
    {
        public BufferedTableLayoutPanel()
        {
            DoubleBuffered = true;
        }
    }

    private void MarkDirty()
    {
        if (!loadingControls)
        {
            SetDirty(true);
            RefreshReviewIfVisible();
        }
    }

    private void SetDirty(bool dirty)
    {
        isDirty = dirty;
        Text = "SFM Package Builder" + (dirty ? " *" : string.Empty);
    }

    private sealed class ModelListItem
    {
        public ModelListItem(ModelEntry model, AdditionalModelCompanionDetail? companionDetail = null)
        {
            Model = model;
            CompanionDetail = companionDetail ?? new AdditionalModelCompanionDetail(0, Array.Empty<string>());
        }

        public ModelEntry Model { get; }
        public AdditionalModelCompanionDetail CompanionDetail { get; }
        public override string ToString() => Model.SourceMdlPath;
    }

    private sealed class SourceListItem
    {
        public SourceListItem(SourceEntry source) => Source = source;
        public SourceEntry Source { get; }
        public override string ToString()
        {
            var kind = Source.IsFolder ? "Folder" : "File";
            var destination = Source.DestinationOverride is null ? string.Empty : " | Package location: " + FormatDestination(Source.DestinationOverride);
            return $"{kind}: {Source.SourcePath}{destination}";
        }

        private static string FormatDestination(DestinationOverride destinationOverride)
        {
            if (destinationOverride.Kind == DestinationOverrideKind.Root)
            {
                return "Package root";
            }

            if (destinationOverride.Kind == DestinationOverrideKind.Misc && string.IsNullOrWhiteSpace(destinationOverride.RelativePath))
            {
                return "Misc\\";
            }

            return string.IsNullOrWhiteSpace(destinationOverride.RelativePath)
                ? "Package root"
                : destinationOverride.RelativePath.TrimEnd('\\', '/') + "\\";
        }
    }

    private sealed class PackageGraphicControl : Control
    {
        public PackageGraphicControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = SystemColors.Window;
            AccessibleRole = AccessibleRole.Graphic;
            AccessibleName = "Decorative package graphic";
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var graphics = e.Graphics;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var bounds = ClientRectangle;
            if (bounds.Width < 120 || bounds.Height < 90)
            {
                return;
            }

            var centerX = bounds.Left + bounds.Width / 2;
            var baseY = bounds.Top + bounds.Height - 34;
            using var shadowBrush = new SolidBrush(Color.FromArgb(26, 0, 0, 0));
            graphics.FillEllipse(shadowBrush, centerX - 118, baseY - 3, 236, 20);

            using var folderBrush = new SolidBrush(Color.FromArgb(238, 194, 84));
            using var folderDarkBrush = new SolidBrush(Color.FromArgb(220, 163, 55));
            using var paperBrush = new SolidBrush(Color.FromArgb(246, 249, 252));
            using var paperLinePen = new Pen(Color.FromArgb(115, 135, 156), 2);
            using var outlinePen = new Pen(Color.FromArgb(55, 70, 86), 2);
            using var bluePen = new Pen(Color.FromArgb(57, 119, 191), 3);

            var paper = new Rectangle(centerX - 80, baseY - 126, 62, 92);
            graphics.FillRectangle(paperBrush, paper);
            graphics.DrawRectangle(outlinePen, paper);
            graphics.DrawLine(paperLinePen, paper.Left + 12, paper.Top + 22, paper.Right - 12, paper.Top + 22);
            graphics.DrawLine(paperLinePen, paper.Left + 12, paper.Top + 38, paper.Right - 12, paper.Top + 38);
            graphics.DrawLine(paperLinePen, paper.Left + 12, paper.Top + 54, paper.Right - 18, paper.Top + 54);

            var tab = new Rectangle(centerX - 14, baseY - 92, 62, 20);
            graphics.FillRectangle(folderDarkBrush, tab);
            graphics.DrawRectangle(outlinePen, tab);
            var folder = new Rectangle(centerX - 46, baseY - 78, 132, 62);
            graphics.FillRectangle(folderBrush, folder);
            graphics.DrawRectangle(outlinePen, folder);
            graphics.DrawLine(outlinePen, folder.Left, folder.Top + 14, folder.Right, folder.Top + 14);

            var zipX = folder.Right - 25;
            for (var i = 0; i < 4; i++)
            {
                graphics.FillRectangle(Brushes.WhiteSmoke, zipX, folder.Top + 20 + i * 9, 8, 5);
                graphics.DrawRectangle(Pens.DimGray, zipX, folder.Top + 20 + i * 9, 8, 5);
            }

            graphics.DrawLine(bluePen, folder.Left + 22, folder.Bottom - 18, folder.Left + 62, folder.Bottom - 18);
            graphics.DrawLine(bluePen, folder.Left + 30, folder.Bottom - 30, folder.Left + 72, folder.Bottom - 30);
        }
    }
}
