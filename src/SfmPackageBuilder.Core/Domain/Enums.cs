namespace SfmPackageBuilder.Core.Domain;

public enum ModelRole
{
    Primary,
    Additional
}

public enum SourceEntryKind
{
    Material,
    Extra
}

public enum DestinationOverrideKind
{
    Root,
    Misc,
    Custom
}

public enum ReadmeMode
{
    Generated,
    Custom,
    None
}

public enum ReadmeCustomSource
{
    ImportedFile,
    ProjectText
}

public enum CompanionUserSelection
{
    Include,
    Exclude
}
