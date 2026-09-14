namespace SfmPackageBuilder.WinForms.Presentation;

public sealed class UnsavedWorkResolver
{
    private readonly Func<UnsavedWorkAction, UnsavedWorkChoice> promptForChoice;
    private readonly Func<bool> save;

    public UnsavedWorkResolver(
        Func<UnsavedWorkAction, UnsavedWorkChoice> promptForChoice,
        Func<bool> save)
    {
        this.promptForChoice = promptForChoice;
        this.save = save;
    }

    public UnsavedWorkResult Resolve(bool hasUnsavedWork, UnsavedWorkAction action)
    {
        if (!hasUnsavedWork)
        {
            return UnsavedWorkResult.Saved;
        }

        return promptForChoice(action) switch
        {
            UnsavedWorkChoice.Discard => UnsavedWorkResult.Discarded,
            UnsavedWorkChoice.Save => save() ? UnsavedWorkResult.Saved : UnsavedWorkResult.Cancelled,
            _ => UnsavedWorkResult.Cancelled
        };
    }
}
