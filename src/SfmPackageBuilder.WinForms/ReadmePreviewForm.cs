using SfmPackageBuilder.Core.Readme;

namespace SfmPackageBuilder.WinForms;

public sealed class ReadmePreviewForm : Form
{
    private readonly TextBox previewText = new();
    private readonly Button useAsCustomButton = new();
    private string currentText = string.Empty;

    public ReadmePreviewForm()
    {
        Text = "README Preview";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(640, 480);
        Size = new Size(760, 600);
        AutoScaleMode = AutoScaleMode.Dpi;

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(8), RowCount = 2 };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        previewText.Dock = DockStyle.Fill;
        previewText.Multiline = true;
        previewText.ReadOnly = true;
        previewText.ScrollBars = ScrollBars.Both;
        previewText.WordWrap = false;
        layout.Controls.Add(previewText, 0, 0);

        var footer = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2 };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var customizePanel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        useAsCustomButton.Text = "Customize this README";
        useAsCustomButton.AutoSize = true;
        useAsCustomButton.Click += (_, _) => UseAsCustomRequested?.Invoke(this, EventArgs.Empty);
        customizePanel.Controls.Add(useAsCustomButton);
        customizePanel.Controls.Add(new Label
        {
            Text = "Use the generated README as your starting point, then edit the text yourself.",
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            MaximumSize = new Size(420, 0)
        });
        footer.Controls.Add(customizePanel, 0, 0);

        var close = new Button { Text = "Close", DialogResult = DialogResult.OK, AutoSize = true, Anchor = AnchorStyles.Right };
        close.Click += (_, _) => Close();
        footer.Controls.Add(close, 1, 0);
        layout.Controls.Add(footer, 0, 1);
        Controls.Add(layout);
        AcceptButton = close;
        CancelButton = close;
        Shown += (_, _) =>
        {
            previewText.SelectionStart = 0;
            previewText.SelectionLength = 0;
            close.Focus();
        };
    }

    public event EventHandler? UseAsCustomRequested;

    public string CurrentText => currentText;

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape)
        {
            Close();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    public void UpdatePreview(string text, bool canUseAsCustom)
    {
        currentText = text;
        previewText.Text = text;
        useAsCustomButton.Enabled = canUseAsCustom;
    }
}
