using SfmPackageBuilder.Core.Archive;
using SfmPackageBuilder.Core.Build;

namespace SfmPackageBuilder.WinForms.Presentation;

public enum VersionReuseChoice
{
    Rebuild,
    ChangeVersion,
    Cancel
}

public enum ExistingOutputChoice
{
    Replace,
    ChooseAnotherName,
    Cancel
}

public sealed class DecisionPresenter
{
    public VersionReuseDecision MapVersionChoice(VersionReuseChoice choice) =>
        choice switch
        {
            VersionReuseChoice.Rebuild => VersionReuseDecision.RebuildExistingVersion,
            VersionReuseChoice.ChangeVersion => VersionReuseDecision.ChangeVersion,
            VersionReuseChoice.Cancel => VersionReuseDecision.Cancel,
            _ => VersionReuseDecision.None
        };

    public ExistingOutputDecision MapOutputChoice(ExistingOutputChoice choice) =>
        choice switch
        {
            ExistingOutputChoice.Replace => ExistingOutputDecision.Replace,
            ExistingOutputChoice.ChooseAnotherName => ExistingOutputDecision.ChooseAnotherName,
            ExistingOutputChoice.Cancel => ExistingOutputDecision.Cancel,
            _ => ExistingOutputDecision.None
        };
}
