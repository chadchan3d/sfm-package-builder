namespace SfmPackageBuilder.WinForms.Presentation;

public sealed class UnsavedWorkPromptContent
{
    public UnsavedWorkPromptContent(string message, string title)
    {
        Message = message;
        Title = title;
    }

    public string Message { get; }

    public string Title { get; }
}
