using System.Diagnostics;

namespace SfmPackageBuilder.WinForms;

public sealed class BuildResultForm : Form
{
    private readonly string outputFolder;
    private readonly string archivePath;
    private readonly Func<bool>? startNewPackage;
    private readonly Button openButton = new();

    public BuildResultForm(string archivePath, string? cleanupWarning, Func<bool>? startNewPackage = null)
    {
        this.archivePath = archivePath;
        this.startNewPackage = startNewPackage;
        outputFolder = Path.GetDirectoryName(archivePath) ?? string.Empty;

        Text = "Build Complete";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        ClientSize = new Size(620, cleanupWarning is null ? 250 : 310);
        MinimumSize = new Size(560, 230);
        AutoScaleMode = AutoScaleMode.Dpi;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            ColumnCount = 2,
            RowCount = 1
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var artwork = new PictureBox
        {
            Image = AssetImages.LoadSuccessArtwork(),
            SizeMode = PictureBoxSizeMode.Zoom,
            Dock = DockStyle.Fill,
            TabStop = false,
            Margin = new Padding(0, 4, 16, 4)
        };
        layout.Controls.Add(artwork, 0, 0);

        var detailsHost = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3
        };
        detailsHost.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        detailsHost.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        detailsHost.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        var details = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = cleanupWarning is null ? 4 : 5
        };
        details.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var title = new Label
        {
            Text = "Your ZIP is ready",
            AutoSize = true,
            Font = new Font(Font.FontFamily, 14, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 12)
        };
        details.Controls.Add(title, 0, 0);
        details.SetColumnSpan(title, 2);

        AddReadOnlyText(details, 1, "ZIP file", Path.GetFileName(archivePath));
        AddReadOnlyText(details, 2, "Output folder", outputFolder);

        if (!string.IsNullOrWhiteSpace(cleanupWarning))
        {
            var warning = new TextBox
            {
                Text = "Cleanup warning: " + cleanupWarning,
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Height = 70
            };
            details.Controls.Add(new Label { Text = "Warning", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 3);
            details.Controls.Add(warning, 1, 3);
            details.SetColumnSpan(warning, 1);
        }

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = new Padding(0, 12, 0, 0)
        };
        var close = new Button { Text = "Close", DialogResult = DialogResult.OK, AutoSize = true };
        var startNew = new Button { Text = "Start New Package", AutoSize = true };
        startNew.Click += (_, _) =>
        {
            if (this.startNewPackage?.Invoke() != false)
            {
                StartedNewPackage = true;
                DialogResult = DialogResult.OK;
                Close();
            }
        };
        openButton.Text = "Open Output Folder";
        openButton.AutoSize = true;
        openButton.BackColor = SystemColors.Highlight;
        openButton.ForeColor = SystemColors.HighlightText;
        openButton.UseVisualStyleBackColor = false;
        openButton.Click += (_, _) => OpenOutputFolder();
        buttons.Controls.Add(close);
        buttons.Controls.Add(startNew);
        buttons.Controls.Add(openButton);
        details.Controls.Add(buttons, 0, cleanupWarning is null ? 3 : 4);
        details.SetColumnSpan(buttons, 2);
        detailsHost.Controls.Add(details, 0, 1);
        layout.Controls.Add(detailsHost, 1, 0);

        AcceptButton = openButton;
        CancelButton = close;
        Controls.Add(layout);
        Shown += (_, _) => openButton.Focus();
    }

    public bool StartedNewPackage { get; private set; }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape)
        {
            Close();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private static void AddReadOnlyText(TableLayoutPanel layout, int row, string label, string text)
    {
        layout.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        layout.Controls.Add(new Label { Text = text, Dock = DockStyle.Fill, AutoSize = false, Height = 28, TextAlign = ContentAlignment.MiddleLeft }, 1, row);
    }

    private void OpenOutputFolder()
    {
        try
        {
            if (File.Exists(archivePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = "/select,\"" + archivePath + "\"",
                    UseShellExecute = true
                });
                return;
            }

            Process.Start(new ProcessStartInfo { FileName = outputFolder, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Could not open the output folder.\r\n\r\n" + outputFolder + "\r\n\r\n" + ex.Message, "Open Output Folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
