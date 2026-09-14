namespace SfmPackageBuilder.Core.Readme;

public sealed class SfmInstallRootRegistry
{
    private static readonly string[] DefaultRoots =
    {
        "models",
        "materials",
        "maps",
        "scripts",
        "sound",
        "cfg",
        "particles",
        "scenes"
    };

    private readonly HashSet<string> roots;

    public SfmInstallRootRegistry()
        : this(DefaultRoots)
    {
    }

    public SfmInstallRootRegistry(IEnumerable<string> roots)
    {
        OrderedRoots = roots
            .Where(root => !string.IsNullOrWhiteSpace(root))
            .Select(root => root.Trim().Trim('\\', '/'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        this.roots = OrderedRoots.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<string> OrderedRoots { get; }

    public bool IsInstallRoot(string firstSegment) =>
        roots.Contains(firstSegment);
}
