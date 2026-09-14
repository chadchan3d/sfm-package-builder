using SfmPackageBuilder.Core.Planning;

namespace SfmPackageBuilder.WinForms.Presentation;

public sealed class PackageTreeNodeModel
{
    public PackageTreeNodeModel(string name, string fullPath)
    {
        Name = name;
        FullPath = fullPath;
    }

    public string Name { get; }

    public string FullPath { get; }

    public PackagePlanEntry? Entry { get; set; }

    public List<PackageTreeNodeModel> Children { get; } = new();
}
