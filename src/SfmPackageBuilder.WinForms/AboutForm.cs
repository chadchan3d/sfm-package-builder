using System.Diagnostics;
using System.Reflection;

namespace SfmPackageBuilder.WinForms;

public sealed class AboutForm : Form
{
    public const string AssetsUrl = "https://chadchan3d.com/category/assets/";
    public const string Cc0Url = "https://creativecommons.org/publicdomain/zero/1.0/";
    public const string ApplicationLicense = "CC0 1.0 Universal";

    public AboutForm()
    {
        Text = "About SFM Package Builder";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(560, 250);
        AutoScaleMode = AutoScaleMode.Dpi;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 2,
            RowCount = 8
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var emblem = new PictureBox
        {
            Image = AssetImages.LoadEmblem(),
            SizeMode = PictureBoxSizeMode.Zoom,
            Width = 76,
            Height = 76,
            Margin = new Padding(0, 2, 14, 0),
            TabStop = false
        };
        layout.Controls.Add(emblem, 0, 0);
        layout.SetRowSpan(emblem, 3);

        layout.Controls.Add(new Label
        {
            Text = "SFM Package Builder",
            AutoSize = true,
            Font = new Font(Font.FontFamily, 14, FontStyle.Bold)
        }, 1, 0);
        layout.Controls.Add(Label("Version " + GetApplicationVersion()), 1, 1);
        layout.Controls.Add(Label("Build ZIP packages for Source Filmmaker model releases."), 1, 2);
        layout.Controls.Add(Label("Developed by ChadChan3D"), 1, 3);
        layout.Controls.Add(Label("SFM Package Builder's original code and assets are dedicated to the public domain under CC0 1.0 Universal."), 1, 4);

        var cc0Link = new LinkLabel
        {
            Text = "View CC0 1.0 license",
            AutoSize = true,
            Margin = new Padding(0, 4, 0, 4)
        };
        cc0Link.LinkClicked += (_, _) => OpenUrl(Cc0Url, "View CC0 1.0 license");
        layout.Controls.Add(cc0Link, 1, 5);

        var link = new LinkLabel
        {
            Text = "ChadChan3D Asset Library",
            AutoSize = true,
            Margin = new Padding(0, 4, 0, 4)
        };
        link.LinkClicked += (_, _) => OpenUrl(AssetsUrl, "ChadChan3D Asset Library");
        layout.Controls.Add(link, 1, 6);

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft
        };
        var close = new Button { Text = "Close", DialogResult = DialogResult.OK, AutoSize = true };
        buttons.Controls.Add(close);
        layout.Controls.Add(buttons, 0, 7);
        layout.SetColumnSpan(buttons, 2);
        AcceptButton = close;
        CancelButton = close;
        Controls.Add(layout);
    }

    public static string GetApplicationVersion() =>
        GetApplicationVersion(typeof(AboutForm).Assembly);

    public static string GetApplicationVersion(Assembly assembly)
    {
        var version = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;
        version = string.IsNullOrWhiteSpace(version)
            ? assembly.GetName().Version?.ToString()
            : version;
        return StripBuildMetadata(version ?? "unknown");
    }

    private static string StripBuildMetadata(string version)
    {
        var metadata = version.IndexOf('+', StringComparison.Ordinal);
        return metadata < 0 ? version : version[..metadata];
    }

    private static Label Label(string text) => new()
    {
        Text = text,
        AutoSize = true,
        MaximumSize = new Size(470, 0),
        Margin = new Padding(0, 4, 0, 4)
    };

    private void OpenUrl(string url, string caption)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Could not open the link.\r\n\r\n" + url + "\r\n\r\n" + ex.Message, caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
