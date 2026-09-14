using SfmPackageBuilder.Core.Planning;
using SfmPackageBuilder.Core.Paths;

namespace SfmPackageBuilder.Core.Validation;

public sealed class CollisionValidator
{
    public IReadOnlyList<ValidationMessage> Validate(PackagePlan plan)
    {
        var messages = new List<ValidationMessage>();
        var groups = plan.Entries
            .Where(entry => entry.DestinationRelativePath is not null)
            .GroupBy(entry => NormalizeDestination(entry.DestinationRelativePath!), StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            var entries = group.ToArray();
            if (entries.Length < 2)
            {
                continue;
            }

            var sourceKeys = entries
                .Select(GetSourceIdentity)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var sources = entries
                .Select(entry => entry.SourcePath ?? $"generated:{entry.Id}")
                .ToArray();

            if (sourceKeys.Length == 1)
            {
                messages.Add(new ValidationMessage(
                    ValidationSeverity.Information,
                    "duplicate-source-destination",
                    "The same source is included more than once for the same package destination.",
                    sources,
                    entries.Select(entry => entry.Id),
                    entries[0].DestinationRelativePath));
                continue;
            }

            messages.Add(new ValidationMessage(
                ValidationSeverity.Error,
                "destination-collision",
                "Two files would use the same location in the ZIP.",
                sources,
                entries.Select(entry => entry.Id),
                entries[0].DestinationRelativePath));
        }

        return messages;
    }

    private static string GetSourceIdentity(PackagePlanEntry entry)
    {
        return entry.SourcePath is null
            ? $"generated:{entry.Id}"
            : NormalizeSource(entry.SourcePath);
    }

    private static string NormalizeDestination(string path)
    {
        var validation = DestinationPath.Validate(path, allowEmpty: false);
        return validation.NormalizedPath
            ?? string.Join('\\', path.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries));
    }

    private static string NormalizeSource(string path)
    {
        return path.Replace('/', '\\').TrimEnd('\\');
    }
}
