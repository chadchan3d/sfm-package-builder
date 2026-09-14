namespace SfmPackageBuilder.Core.SharedFiles;

public static class KnownSharedFiles
{
    public const string ControlGroupsRuleKey = "sfm_defaultanimationgroups";

    public const string ControlGroupsFilename = "sfm_defaultanimationgroups.txt";

    public const string ControlGroupsInformationTitle = "SFM control-groups file included. It may overwrite the user's existing setup.";

    public const string ControlGroupsInformationText =
        "SFM uses sfm_defaultanimationgroups.txt as one shared control-groups\r\n" +
        "file for multiple models. Users who already have their own copy may\r\n" +
        "need to add this model's groups to it rather than replace the file.";

    public static IReadOnlyList<SharedFileRule> CreateDefaultRules()
    {
        return new[]
        {
            new SharedFileRule(
                ControlGroupsRuleKey,
                ControlGroupsFilename,
                Array.Empty<string>(),
                ControlGroupsInformationTitle,
                ControlGroupsInformationText,
                providesControlGroupsReadmeContext: true)
        };
    }
}
