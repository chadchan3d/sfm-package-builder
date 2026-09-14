using SfmPackageBuilder.Core.Domain;

namespace SfmPackageBuilder.WinForms;

public sealed class SettingsForm : Form
{
    private readonly TextBox outputFolderText = new();
    private readonly TextBox sfmContentFolderText = new();
    private readonly TextBox projectFolderText = new();
    private readonly TextBox archivePatternText = new();
    private readonly TextBox authorText = new();
    private readonly TextBox websiteText = new();
    private readonly TextBox licenseText = new();

    public SettingsForm(AppSettings settings)
    {
        Settings = settings;
        Text = "Defaults & Preferences";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(700, 620);
        Size = new Size(780, 700);
        AutoScaleMode = AutoScaleMode.Dpi;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            AutoScroll = true,
            ColumnCount = 1,
            RowCount = 4
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.Controls.Add(FoldersGroup(), 0, 0);
        layout.Controls.Add(CreatorDefaultsGroup(), 0, 1);
        layout.Controls.Add(ZipNamingGroup(), 0, 2);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, AutoSize = true };
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        layout.Controls.Add(buttons, 0, 3);

        Controls.Add(layout);
        AcceptButton = ok;
        CancelButton = cancel;
        LoadSettings();
        ok.Click += (_, _) => SaveSettings();
    }

    public AppSettings Settings { get; }

    private void LoadSettings()
    {
        outputFolderText.Text = Settings.DefaultOutputDirectory;
        sfmContentFolderText.Text = Settings.DefaultSfmContentFolder;
        projectFolderText.Text = Settings.DefaultProjectDirectory;
        archivePatternText.Text = Settings.DefaultArchivePattern;
        authorText.Text = Settings.CreatorDefaults.Author;
        websiteText.Text = Settings.CreatorDefaults.Website;
        licenseText.Text = Settings.CreatorDefaults.License;
    }

    private void SaveSettings()
    {
        Settings.DefaultOutputDirectory = outputFolderText.Text.Trim();
        Settings.DefaultSfmContentFolder = sfmContentFolderText.Text.Trim();
        Settings.DefaultProjectDirectory = projectFolderText.Text.Trim();
        Settings.DefaultArchivePattern = archivePatternText.Text.Trim();
        Settings.CreatorDefaults.Author = authorText.Text.Trim();
        Settings.CreatorDefaults.Website = websiteText.Text.Trim();
        Settings.CreatorDefaults.License = licenseText.Text.Trim();
    }

    private GroupBox FoldersGroup()
    {
        var group = Group("Folders");
        var layout = GroupLayout(6);
        AddPathRow(layout, 0, "SFM usermod folder", sfmContentFolderText, BrowseFolder);
        AddHelperRow(layout, 1, @"Usually ...\SourceFilmmaker\game\usermod");
        AddPathRow(layout, 2, "Project folder", projectFolderText, BrowseFolder);
        AddHelperRow(layout, 3, "Starting folder for saving .sfmpack project files.");
        AddPathRow(layout, 4, "ZIP output folder", outputFolderText, BrowseFolder);
        AddHelperRow(layout, 5, "Starting folder for finished ZIP packages.");
        group.Controls.Add(layout);
        return group;
    }

    private GroupBox CreatorDefaultsGroup()
    {
        var group = Group("Creator defaults");
        var layout = GroupLayout(5);
        AddTextRow(layout, 0, "&Author", authorText);
        AddTextRow(layout, 1, "&Website", websiteText);
        AddTextRow(layout, 2, "Model usage terms", licenseText, multiline: true);
        AddHelperRow(layout, 3, "State how you want others to use or redistribute the model. Examples: CC0 1.0 or Do not redistribute.");
        group.Controls.Add(layout);
        return group;
    }

    private GroupBox ZipNamingGroup()
    {
        var group = Group("ZIP naming");
        var layout = GroupLayout(3);
        AddTextRow(layout, 0, "Automatic ZIP filename", archivePatternText);
        AddHelperRow(layout, 1, "Choose how suggested ZIP filenames are built. Use {AssetName} and {Version} where you want those values to appear. {AssetName} uses the Title.");
        AddHelperRow(layout, 2, "Example: MyModel_v1.2.zip");
        group.Controls.Add(layout);
        return group;
    }

    private static GroupBox Group(string text) => new() { Text = text, Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10), Margin = new Padding(0, 0, 0, 12) };

    private static TableLayoutPanel GroupLayout(int rows)
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 3, RowCount = rows };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        return layout;
    }

    private void BrowseFolder(TextBox textBox)
    {
        using var dialog = new FolderBrowserDialog { SelectedPath = Directory.Exists(textBox.Text) ? textBox.Text : string.Empty };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            textBox.Text = dialog.SelectedPath;
        }
    }

    private static void AddPathRow(TableLayoutPanel layout, int row, string label, TextBox textBox, Action<TextBox> browse)
    {
        AddTextRow(layout, row, label, textBox, spanRemaining: false);
        var button = new Button { Text = "Browse...", AutoSize = true };
        button.Click += (_, _) => browse(textBox);
        layout.Controls.Add(button, 2, row);
    }

    private static void AddTextRow(TableLayoutPanel layout, int row, string label, TextBox textBox, bool multiline = false, bool spanRemaining = true)
    {
        layout.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, TextAlign = ContentAlignment.MiddleLeft }, 0, row);
        textBox.Dock = DockStyle.Fill;
        textBox.Multiline = multiline;
        textBox.ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None;
        textBox.Height = multiline ? 96 : textBox.Height;
        textBox.MinimumSize = multiline ? new Size(360, 96) : new Size(360, 0);
        layout.Controls.Add(textBox, 1, row);
        if (spanRemaining)
        {
            layout.SetColumnSpan(textBox, 2);
        }
    }

    private static void AddHelperRow(TableLayoutPanel layout, int row, string text)
    {
        var helper = new Label
        {
            Text = text,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            ForeColor = SystemColors.GrayText,
            MaximumSize = new Size(460, 0)
        };
        layout.Controls.Add(helper, 1, row);
        layout.SetColumnSpan(helper, 2);
    }
}
