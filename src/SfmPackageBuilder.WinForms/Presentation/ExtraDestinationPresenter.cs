using SfmPackageBuilder.Core.Domain;

namespace SfmPackageBuilder.WinForms.Presentation;

public sealed class ExtraDestinationPresenter
{
    public const string PackageRootChoice = "Package root";
    public const string DocumentationChoice = "Documentation (Docs\\)";
    public const string CustomChoice = "Custom folder...";

    public IReadOnlyList<string> Choices { get; } = new[] { PackageRootChoice, DocumentationChoice, CustomChoice };

    public string ChoiceFor(DestinationOverride? destinationOverride)
    {
        if (destinationOverride is null || destinationOverride.Kind == DestinationOverrideKind.Root)
        {
            return PackageRootChoice;
        }

        if (destinationOverride.Kind == DestinationOverrideKind.Custom
            && destinationOverride.RelativePath.Equals("Docs", StringComparison.OrdinalIgnoreCase))
        {
            return DocumentationChoice;
        }

        return CustomChoice;
    }

    public string CustomFolderFor(DestinationOverride? destinationOverride)
    {
        if (destinationOverride is null || destinationOverride.Kind == DestinationOverrideKind.Root)
        {
            return string.Empty;
        }

        if (destinationOverride.Kind == DestinationOverrideKind.Misc && string.IsNullOrWhiteSpace(destinationOverride.RelativePath))
        {
            return "Misc";
        }

        if (destinationOverride.Kind == DestinationOverrideKind.Custom
            && destinationOverride.RelativePath.Equals("Docs", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return destinationOverride.RelativePath;
    }

    public DestinationOverride ToOverride(string choice, string customFolder)
    {
        return choice switch
        {
            PackageRootChoice => new DestinationOverride { Kind = DestinationOverrideKind.Root, RelativePath = string.Empty },
            DocumentationChoice => new DestinationOverride { Kind = DestinationOverrideKind.Custom, RelativePath = "Docs" },
            _ => new DestinationOverride { Kind = DestinationOverrideKind.Custom, RelativePath = customFolder.Trim() }
        };
    }

    public string PackageLocationFor(SourceEntry source)
    {
        var destination = source.DestinationOverride;
        if (destination is null || destination.Kind == DestinationOverrideKind.Root)
        {
            return "Package root";
        }

        var folder = destination.Kind == DestinationOverrideKind.Misc && string.IsNullOrWhiteSpace(destination.RelativePath)
            ? "Misc"
            : destination.RelativePath;
        if (string.IsNullOrWhiteSpace(folder))
        {
            return "Package root";
        }

        return folder.TrimEnd('\\', '/') + "\\";
    }
}
