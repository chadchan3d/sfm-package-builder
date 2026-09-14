using System.Text;

namespace SfmPackageBuilder.Core.Readme;

public sealed class ResolvedReadme
{
    private ResolvedReadme(
        ResolvedReadmeKind kind,
        ReadmeResolutionStatus status,
        string? text,
        string? previewText,
        string? importedSourcePath,
        string? failureDetail)
    {
        Kind = kind;
        Status = status;
        Text = text;
        PreviewText = previewText;
        ImportedSourcePath = importedSourcePath;
        FailureDetail = failureDetail;
    }

    public ResolvedReadmeKind Kind { get; }

    public ReadmeResolutionStatus Status { get; }

    public bool IsResolved => Status == ReadmeResolutionStatus.Resolved;

    public string? Text { get; }

    public string? PreviewText { get; }

    public string? ImportedSourcePath { get; }

    public string? FailureDetail { get; }

    public byte[] GetGeneratedOrCustomBytes()
    {
        return Text is null
            ? Array.Empty<byte>()
            : new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(Text);
    }

    public static ResolvedReadme Generated(string text) =>
        new(ResolvedReadmeKind.GeneratedText, ReadmeResolutionStatus.Resolved, text, text, null, null);

    public static ResolvedReadme Custom(string text) =>
        new(ResolvedReadmeKind.CustomText, ReadmeResolutionStatus.Resolved, text, text, null, null);

    public static ResolvedReadme Imported(string sourcePath, string previewText) =>
        new(ResolvedReadmeKind.ImportedFile, ReadmeResolutionStatus.Resolved, null, previewText, sourcePath, null);

    public static ResolvedReadme None() =>
        new(ResolvedReadmeKind.None, ReadmeResolutionStatus.Resolved, null, null, null, null);

    public static ResolvedReadme Failure(ReadmeResolutionStatus status, string failureDetail) =>
        new(ResolvedReadmeKind.Failure, status, null, null, null, failureDetail);
}
