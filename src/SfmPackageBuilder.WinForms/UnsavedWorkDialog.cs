using SfmPackageBuilder.WinForms.Presentation;

namespace SfmPackageBuilder.WinForms;

public sealed class UnsavedWorkDialog : Form
{
    private UnsavedWorkChoice choice = UnsavedWorkChoice.Cancel;

    public UnsavedWorkDialog(UnsavedWorkPromptContent content)
    {
        ArgumentNullException.ThrowIfNull(content);

        Text = content.Title;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(460, 150);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            ColumnCount = 1,
            RowCount = 2
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var message = new Label
        {
            Text = content.Message,
            Dock = DockStyle.Fill,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft
        };
        layout.Controls.Add(message, 0, 0);

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        var cancel = DialogButton("Cancel", UnsavedWorkChoice.Cancel, DialogResult.Cancel);
        var discard = DialogButton("Don't Save", UnsavedWorkChoice.Discard, DialogResult.No);
        var save = DialogButton("Save", UnsavedWorkChoice.Save, DialogResult.OK);
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(discard);
        buttons.Controls.Add(save);
        layout.Controls.Add(buttons, 0, 1);

        AcceptButton = save;
        CancelButton = cancel;
        Controls.Add(layout);
    }

    public UnsavedWorkChoice Choice => choice;

    private Button DialogButton(string text, UnsavedWorkChoice buttonChoice, DialogResult dialogResult)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            MinimumSize = new Size(96, 30),
            DialogResult = dialogResult,
            Margin = new Padding(6, 8, 0, 0)
        };
        button.Click += (_, _) => choice = buttonChoice;
        return button;
    }
}
