using SfmPackageBuilder.Core.Planning;
using SfmPackageBuilder.WinForms.Presentation;

namespace SfmPackageBuilder.WinForms;

public sealed class PackagePreviewForm : Form
{
    private readonly TreeView tree = new();
    private readonly TextBox detailText = new();
    private readonly PackagePlanTreeBuilder treeBuilder = new();

    public PackagePreviewForm(PackagePlan plan)
    {
        Text = "Package Preview";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(760, 520);
        Size = new Size(900, 640);
        AutoScaleMode = AutoScaleMode.Dpi;

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = 420
        };
        tree.Dock = DockStyle.Fill;
        tree.HideSelection = false;
        tree.AfterSelect += (_, _) => ShowDetail();
        split.Panel1.Controls.Add(tree);

        detailText.Dock = DockStyle.Fill;
        detailText.Multiline = true;
        detailText.ReadOnly = true;
        detailText.ScrollBars = ScrollBars.Both;
        split.Panel2.Controls.Add(detailText);
        Controls.Add(split);

        LoadPlan(plan);
    }

    private void LoadPlan(PackagePlan plan)
    {
        tree.Nodes.Clear();
        foreach (var node in treeBuilder.Build(plan))
        {
            tree.Nodes.Add(ToTreeNode(node));
        }

        tree.ExpandAll();
    }

    private static TreeNode ToTreeNode(PackageTreeNodeModel model)
    {
        var node = new TreeNode(model.Name) { Tag = model };
        foreach (var child in model.Children)
        {
            node.Nodes.Add(ToTreeNode(child));
        }

        return node;
    }

    private void ShowDetail()
    {
        if (tree.SelectedNode?.Tag is not PackageTreeNodeModel model || model.Entry is null)
        {
            detailText.Text = modelText(tree.SelectedNode?.Tag as PackageTreeNodeModel);
            return;
        }

        var source = model.Entry.EntryType == PackagePlanEntryType.Readme && model.Entry.SourcePath is null
            ? "Generated"
            : model.Entry.SourcePath ?? "Generated";
        detailText.Text =
            "Source:" + Environment.NewLine +
            source + Environment.NewLine + Environment.NewLine +
            "Destination:" + Environment.NewLine +
            (model.Entry.DestinationRelativePath ?? model.FullPath) + Environment.NewLine + Environment.NewLine +
            "Status:" + Environment.NewLine +
            model.Entry.Status;
    }

    private static string modelText(PackageTreeNodeModel? model) =>
        model is null ? string.Empty : "Destination:" + Environment.NewLine + model.FullPath;
}
