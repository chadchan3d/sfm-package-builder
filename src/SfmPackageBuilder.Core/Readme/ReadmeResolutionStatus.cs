namespace SfmPackageBuilder.Core.Readme;

public enum ReadmeResolutionStatus
{
    Resolved,
    MissingImportedReadme,
    ImportedReadmeUnreadable,
    CustomTextMissing
}
