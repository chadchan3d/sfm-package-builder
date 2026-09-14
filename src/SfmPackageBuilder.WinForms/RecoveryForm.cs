using SfmPackageBuilder.Core.Domain;
using SfmPackageBuilder.Core.Persistence;

namespace SfmPackageBuilder.WinForms;

public sealed class RecoveryForm : Form
{
    private readonly MissingSourceRecoveryService recoveryService;
    private readonly PackageProject project;
    private readonly string? projectPath;
    private readonly ListView groupsList = new();
    private readonly TextBox details = new();

    public RecoveryForm(PackageProject project, MissingSourceRecoveryService recoveryService, string? projectPath = null)
    {
        this.project = project;
        this.recoveryService = recoveryService;
        this.projectPath = projectPath;
        Text = "Some project files couldn't be found";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(780, 520);
        Size = new Size(920, 620);
        AutoScaleMode = AutoScaleMode.Dpi;

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(10), RowCount = 4 };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var intro = new Label
        {
            Text = "Some project files couldn't be found. Locate the folder that now contains these files, or skip for now.",
            Dock = DockStyle.Fill,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8)
        };
        layout.Controls.Add(intro, 0, 0);

        groupsList.Dock = DockStyle.Fill;
        groupsList.View = View.Details;
        groupsList.FullRowSelect = true;
        groupsList.HideSelection = false;
        groupsList.Columns.Add("Old location", 380);
        groupsList.Columns.Add("Files", 70);
        groupsList.Columns.Add("Examples", 430);
        groupsList.SelectedIndexChanged += (_, _) => ShowDetails();
        layout.Controls.Add(groupsList, 0, 1);

        details.Dock = DockStyle.Fill;
        details.Multiline = true;
        details.ReadOnly = true;
        details.ScrollBars = ScrollBars.Both;
        layout.Controls.Add(details, 0, 2);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, AutoSize = true };
        var close = new Button { Text = "Skip For Now", DialogResult = DialogResult.OK, AutoSize = true };
        var locate = new Button { Text = "Locate Folder...", AutoSize = true };
        locate.Click += (_, _) => LocateSelectedGroup();
        buttons.Controls.Add(close);
        buttons.Controls.Add(locate);
        layout.Controls.Add(buttons, 0, 3);
        Controls.Add(layout);
        AcceptButton = locate;
        CancelButton = close;

        LoadGroups();
    }

    public bool ProjectChanged { get; private set; }

    private RecoveryIssueGroup? SelectedGroup =>
        groupsList.SelectedItems.Count == 0 ? null : groupsList.SelectedItems[0].Tag as RecoveryIssueGroup;

    private void LoadGroups()
    {
        groupsList.Items.Clear();
        var inspection = recoveryService.Inspect(project);
        foreach (var group in inspection.Groups)
        {
            var item = new ListViewItem(group.FormerRoot);
            item.SubItems.Add(group.Issues.Count.ToString());
            item.SubItems.Add(string.Join("; ", group.Issues.Take(3).Select(ExampleFor)));
            item.Tag = group;
            groupsList.Items.Add(item);
        }

        if (groupsList.Items.Count > 0)
        {
            groupsList.Items[0].Selected = true;
        }
        else
        {
            details.Text = inspection.HasIssues
                ? "Some files are still missing, but this project does not have enough saved location information to locate them as a folder group. Select those files again in the package editor."
                : "All project files are available.";
        }
    }

    private void ShowDetails()
    {
        var group = SelectedGroup;
        if (group is null)
        {
            details.Text = string.Empty;
            return;
        }

        details.Text =
            "Old location:" + Environment.NewLine +
            group.FormerRoot + Environment.NewLine + Environment.NewLine +
            "Affected files:" + Environment.NewLine +
            string.Join(Environment.NewLine, group.Issues.Select(issue => "- " + ExampleFor(issue)));
    }

    private void LocateSelectedGroup()
    {
        var group = SelectedGroup;
        if (group is null)
        {
            return;
        }

        using var folderDialog = new FolderBrowserDialog
        {
            Description = "Locate the folder that now contains these files."
        };
        if (folderDialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var preview = recoveryService.PreviewFolderRemap(project, group.FormerRoot, folderDialog.SelectedPath);
        if (!preview.HasMatches)
        {
            MessageBox.Show(
                this,
                "No matching files were found in that folder. Choose the folder that contains the same files from the old location.",
                "Locate Missing Files",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        if (!preview.IsFullMatch)
        {
            var stillMissing = string.Join(
                Environment.NewLine,
                preview.UnresolvedCandidates.Take(5).Select(candidate => "- " + ExampleFor(candidate.Issue) + " (" + StatusText(candidate.Status) + ")"));
            var decision = MessageBox.Show(
                this,
                $"Some files were found, but {preview.UnresolvedCandidates.Count} are still missing.\r\n\r\n{stillMissing}\r\n\r\nUse the {preview.MatchedCandidates.Count} files that were found?",
                "Locate Missing Files",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Information);
            if (decision != DialogResult.Yes)
            {
                return;
            }
        }
        else
        {
            MessageBox.Show(
                this,
                "All files in this group were found.",
                "Locate Missing Files",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        var result = recoveryService.ApplyFolderRemap(project, projectPath, preview);
        if (result.AppliedAny)
        {
            ProjectChanged = true;
            LoadGroups();
        }
    }

    private static string ExampleFor(RecoveryIssue issue) =>
        string.IsNullOrWhiteSpace(issue.RelativeRemainder)
            ? issue.SourcePath
            : issue.RelativeRemainder!;

    private static string StatusText(RecoveryRemapCandidateStatus status) => status switch
    {
        RecoveryRemapCandidateStatus.TypeMismatch => "something else is at that location",
        RecoveryRemapCandidateStatus.Inaccessible => "not readable",
        _ => "not found"
    };
}
