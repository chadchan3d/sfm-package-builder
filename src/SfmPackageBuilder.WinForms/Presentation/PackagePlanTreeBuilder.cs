using SfmPackageBuilder.Core.Planning;

namespace SfmPackageBuilder.WinForms.Presentation;

public sealed class PackagePlanTreeBuilder
{
    public IReadOnlyList<PackageTreeNodeModel> Build(PackagePlan plan)
    {
        var roots = new List<PackageTreeNodeModel>();
        foreach (var entry in plan.Entries
            .Where(entry => entry.DestinationRelativePath is not null)
            .OrderBy(entry => entry.DestinationRelativePath, StringComparer.OrdinalIgnoreCase))
        {
            AddEntry(roots, entry);
        }

        roots.Sort(CompareRootNodes);
        return roots;
    }

    private static void AddEntry(List<PackageTreeNodeModel> roots, PackagePlanEntry entry)
    {
        var segments = entry.DestinationRelativePath!
            .Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
        var siblings = roots;
        var currentPath = string.Empty;
        PackageTreeNodeModel? current = null;

        foreach (var segment in segments)
        {
            currentPath = string.IsNullOrEmpty(currentPath) ? segment : currentPath + "\\" + segment;
            current = siblings.SingleOrDefault(node => node.Name.Equals(segment, StringComparison.OrdinalIgnoreCase));
            if (current is null)
            {
                current = new PackageTreeNodeModel(segment, currentPath);
                siblings.Add(current);
                siblings.Sort((left, right) => string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase));
            }

            siblings = current.Children;
        }

        if (current is not null)
        {
            current.Entry = entry;
        }
    }

    private static int CompareRootNodes(PackageTreeNodeModel left, PackageTreeNodeModel right)
    {
        var priority = RootPriority(left.Name).CompareTo(RootPriority(right.Name));
        return priority != 0 ? priority : string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
    }

    private static int RootPriority(string name) => name.ToLowerInvariant() switch
    {
        "models" => 0,
        "materials" => 1,
        "scripts" => 2,
        "cfg" => 3,
        "readme.txt" => 4,
        "docs" => 5,
        _ => 10
    };
}
