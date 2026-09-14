namespace SfmPackageBuilder.WinForms.Presentation;

public sealed class UnsavedWorkPromptPresenter
{
    public UnsavedWorkPromptContent ContentFor(UnsavedWorkAction action)
    {
        var message = action switch
        {
            UnsavedWorkAction.NewProject or UnsavedWorkAction.NewFromCurrentProject =>
                "Save this package project before starting a new one? Saving lets you edit or rebuild this package later.",
            UnsavedWorkAction.OpenProject =>
                "Save this package project before opening another one? The opened project will replace the current project.",
            UnsavedWorkAction.Exit =>
                "Save this package project before exiting?",
            _ => "Save this package project?"
        };

        return new UnsavedWorkPromptContent(message, "Unsaved Package Project");
    }
}
