namespace SfmPackageBuilder.WinForms;

public sealed class ChangelogEntryDialog : Form
{
    private readonly TextBox versionText = new();
    private readonly TextBox changesText = new();

    public ChangelogEntryDialog(string title, string version = "", IEnumerable<string>? changes = null)
    {
        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.Sizable;
        ClientSize = new Size(560, 320);
        MinimumSize = new Size(500, 280);
        AutoScaleMode = AutoScaleMode.Dpi;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 2,
            RowCount = 4
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        layout.Controls.Add(new Label { Text = "Version", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        versionText.Dock = DockStyle.Fill;
        versionText.Text = version;
        layout.Controls.Add(versionText, 1, 0);

        layout.Controls.Add(new Label { Text = "Changes", AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Top, Margin = new Padding(0, 8, 0, 0) }, 0, 1);
        changesText.Dock = DockStyle.Fill;
        changesText.Multiline = true;
        changesText.ScrollBars = ScrollBars.Vertical;
        changesText.Text = string.Join(Environment.NewLine, changes ?? Array.Empty<string>());
        layout.Controls.Add(changesText, 1, 1);

        var helper = new Label
        {
            Text = "One change per line.",
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Margin = new Padding(0, 6, 0, 8)
        };
        layout.Controls.Add(helper, 1, 2);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.RightToLeft };
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        layout.Controls.Add(buttons, 0, 3);
        layout.SetColumnSpan(buttons, 2);

        Controls.Add(layout);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    public string Version => versionText.Text.Trim();

    public IReadOnlyList<string> Changes => changesText.Lines
        .Select(line => line.Trim())
        .Where(line => line.Length > 0)
        .ToArray();
}
