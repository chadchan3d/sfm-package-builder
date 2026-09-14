using SfmPackageBuilder.Core.Validation;
using SfmPackageBuilder.WinForms.Presentation;

namespace SfmPackageBuilder.WinForms;

public sealed class CheckResultsForm : Form
{
    private readonly ListView list = new();
    private readonly TextBox details = new();
    private readonly ValidationPresenter presenter = new();

    public CheckResultsForm(ValidationResult result)
    {
        Text = "Check Package";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(760, 520);
        Size = new Size(900, 620);
        AutoScaleMode = AutoScaleMode.Dpi;

        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 320 };
        list.Dock = DockStyle.Fill;
        list.View = View.Details;
        list.FullRowSelect = true;
        list.Columns.Add("Status", 110);
        list.Columns.Add("Message", 700);
        list.SelectedIndexChanged += (_, _) => details.Text = list.SelectedItems.Count == 0 ? string.Empty : (list.SelectedItems[0].Tag as string) ?? string.Empty;
        split.Panel1.Controls.Add(list);

        details.Dock = DockStyle.Fill;
        details.Multiline = true;
        details.ReadOnly = true;
        details.ScrollBars = ScrollBars.Both;
        split.Panel2.Controls.Add(details);
        Controls.Add(split);

        LoadResult(result);
    }

    private void LoadResult(ValidationResult result)
    {
        list.Items.Clear();
        foreach (var item in presenter.Present(result))
        {
            var row = new ListViewItem(ValidationPresenter.FormatStatus(item.Severity));
            row.SubItems.Add(item.Title);
            row.Tag = item.Details;
            row.ForeColor = item.Severity switch
            {
                ValidationSeverity.Error => Color.Firebrick,
                ValidationSeverity.Warning => Color.DarkGoldenrod,
                ValidationSeverity.Information => Color.DarkBlue,
                _ => Color.DarkGreen
            };
            list.Items.Add(row);
        }
    }
}
