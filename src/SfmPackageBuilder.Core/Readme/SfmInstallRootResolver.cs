using SfmPackageBuilder.Core.Planning;

namespace SfmPackageBuilder.Core.Readme;

public sealed class SfmInstallRootResolver
{
    private readonly SfmInstallRootRegistry registry;

    public SfmInstallRootResolver()
        : this(new SfmInstallRootRegistry())
    {
    }

    public SfmInstallRootResolver(SfmInstallRootRegistry registry)
    {
        this.registry = registry;
    }

    public SfmInstallRootResolution Resolve(IEnumerable<PackagePlanEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var discoveredRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var hasDocs = false;
        var hasOtherSupporting = false;

        foreach (var entry in entries)
        {
            if (entry.EntryType == PackagePlanEntryType.Readme
                || entry.Status != PackagePlanEntryStatus.Resolved
                || string.IsNullOrWhiteSpace(entry.DestinationRelativePath))
            {
                continue;
            }

            var firstSegment = GetFirstSegment(entry.DestinationRelativePath!);
            if (string.IsNullOrWhiteSpace(firstSegment))
            {
                hasOtherSupporting = true;
                continue;
            }

            if (registry.IsInstallRoot(firstSegment))
            {
                discoveredRoots.Add(firstSegment);
                continue;
            }

            if (string.Equals(firstSegment, "Docs", StringComparison.OrdinalIgnoreCase))
            {
                hasDocs = true;
                continue;
            }

            hasOtherSupporting = true;
        }

        var orderedRoots = registry.OrderedRoots
            .Where(discoveredRoots.Contains)
            .ToArray();

        return new SfmInstallRootResolution(orderedRoots, hasDocs, hasOtherSupporting);
    }

    private static string GetFirstSegment(string destinationRelativePath)
    {
        var normalized = destinationRelativePath.Replace('/', '\\').Trim('\\');
        var separator = normalized.IndexOf('\\', StringComparison.Ordinal);
        return separator < 0 ? string.Empty : normalized[..separator];
    }
}
