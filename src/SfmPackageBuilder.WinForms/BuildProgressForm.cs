namespace SfmPackageBuilder.WinForms;

using SfmPackageBuilder.Core.Build;

public sealed class BuildProgressForm : Form
{
    private readonly Label statusLabel = new();
    private readonly ProgressBar progressBar = new();
    private readonly Button cancelButton = new();
    private bool canClose;
    private bool cancellationRequested;

    public BuildProgressForm(Action cancelBuild)
    {
        ArgumentNullException.ThrowIfNull(cancelBuild);

        Text = "Build ZIP";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(460, 150);
        AutoScaleMode = AutoScaleMode.Dpi;

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), RowCount = 3 };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        statusLabel.Text = "Checking package";
        statusLabel.AutoSize = true;
        layout.Controls.Add(statusLabel, 0, 0);
        progressBar.Style = ProgressBarStyle.Marquee;
        progressBar.Dock = DockStyle.Top;
        layout.Controls.Add(progressBar, 0, 1);
        cancelButton.Text = "Cancel Build";
        cancelButton.AutoSize = true;
        cancelButton.Anchor = AnchorStyles.Right;
        cancelButton.Click += (_, _) => RequestCancel(cancelBuild);
        layout.Controls.Add(cancelButton, 0, 2);
        CancelButton = cancelButton;
        FormClosing += (_, e) =>
        {
            if (canClose)
            {
                return;
            }

            RequestCancel(cancelBuild);
            e.Cancel = true;
        };
        Controls.Add(layout);
    }

    public void Report(BuildProgress progress)
    {
        var text = progress.Phase switch
        {
            BuildProgressPhase.Checking => "Checking package...",
            BuildProgressPhase.PreparingFiles => "Preparing files...",
            BuildProgressPhase.CreatingArchive => "Creating ZIP...",
            BuildProgressPhase.VerifyingArchive => "Verifying ZIP...",
            BuildProgressPhase.Finishing => "Finishing...",
            _ => "Building..."
        };

        if (progress.Completed is not null && progress.Total is not null)
        {
            text += $" {progress.Completed} of {progress.Total}";
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Minimum = 0;
            progressBar.Maximum = Math.Max(progress.Total.Value, 1);
            progressBar.Value = Math.Min(progress.Completed.Value, progressBar.Maximum);
        }
        else
        {
            progressBar.Style = ProgressBarStyle.Marquee;
        }

        statusLabel.Text = text;
    }

    public void AllowClose()
    {
        canClose = true;
    }

    private void RequestCancel(Action cancelBuild)
    {
        if (cancellationRequested)
        {
            return;
        }

        cancellationRequested = true;
        cancelButton.Enabled = false;
        statusLabel.Text = "Cancelling...";
        progressBar.Style = ProgressBarStyle.Marquee;
        cancelBuild();
    }
}
