using SfmPackageBuilder.Core.Paths;

namespace SfmPackageBuilder.WinForms;

public sealed class TextInputDialog : Form
{
    private readonly TextBox input = new();
    private readonly Label errorLabel = new();
    private readonly bool validatePackagePath;

    public TextInputDialog(string title, string prompt, string initialValue = "", bool validatePackagePath = false)
    {
        this.validatePackagePath = validatePackagePath;
        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        ClientSize = new Size(520, 150);
        AutoScaleMode = AutoScaleMode.Dpi;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            RowCount = 4,
            ColumnCount = 1
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        layout.Controls.Add(new Label { AutoSize = true, Text = prompt }, 0, 0);
        input.Dock = DockStyle.Top;
        input.Text = initialValue;
        layout.Controls.Add(input, 0, 1);

        errorLabel.AutoSize = true;
        errorLabel.ForeColor = Color.Firebrick;
        layout.Controls.Add(errorLabel, 0, 2);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, AutoSize = true };
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
        ok.Click += (_, args) =>
        {
            if (!ValidateInput())
            {
                args = EventArgs.Empty;
                DialogResult = DialogResult.None;
            }
        };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        layout.Controls.Add(buttons, 0, 3);

        Controls.Add(layout);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    public string Value => input.Text.Trim();

    private bool ValidateInput()
    {
        if (!validatePackagePath)
        {
            return true;
        }

        var result = DestinationPath.Validate(Value, allowEmpty: true);
        if (result.IsValid)
        {
            return true;
        }

        errorLabel.Text = result.Message;
        input.Focus();
        return false;
    }
}
