namespace SfmPackageBuilder.Core.Validation;

public static class WindowsFileNameValidator
{
    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON",
        "PRN",
        "AUX",
        "NUL",
        "COM1",
        "COM2",
        "COM3",
        "COM4",
        "COM5",
        "COM6",
        "COM7",
        "COM8",
        "COM9",
        "LPT1",
        "LPT2",
        "LPT3",
        "LPT4",
        "LPT5",
        "LPT6",
        "LPT7",
        "LPT8",
        "LPT9"
    };

    public static bool IsValidFileName(string value, bool requireZipExtension, out string reason)
    {
        reason = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            reason = "The filename is empty.";
            return false;
        }

        if (value is "." or "..")
        {
            reason = "The filename cannot be . or ...";
            return false;
        }

        if (value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || value.Contains('\\', StringComparison.Ordinal) || value.Contains('/', StringComparison.Ordinal))
        {
            reason = "The filename contains characters Windows cannot use in a filename.";
            return false;
        }

        if (value.EndsWith(' ') || value.EndsWith('.'))
        {
            reason = "The filename cannot end with a space or period.";
            return false;
        }

        var stem = Path.GetFileNameWithoutExtension(value);
        if (ReservedNames.Contains(stem) || ReservedNames.Contains(value))
        {
            reason = "The filename uses a reserved Windows device name.";
            return false;
        }

        if (requireZipExtension && !value.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            reason = "The archive name must end with .zip.";
            return false;
        }

        return true;
    }

    public static bool IsValidStem(string value, out string reason)
    {
        return IsValidFileName(value, requireZipExtension: false, out reason);
    }

    public static ModelNameValidationResult ValidateModelStem(string value)
    {
        var name = value ?? string.Empty;
        if (IsValidStem(name, out var reason))
        {
            return ModelNameValidationResult.Valid;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return new ModelNameValidationResult(
                false,
                reason,
                "Model name in ZIP is empty.",
                "Enter a model name on Model & Materials.",
                "Enter a model name.");
        }

        var invalidCharacter = FirstInvalidCharacter(name);
        if (invalidCharacter is not null)
        {
            var display = invalidCharacter == '\t'
                ? "tab"
                : invalidCharacter.ToString();
            return new ModelNameValidationResult(
                false,
                reason,
                "Model name in ZIP contains a character Windows does not allow.",
                $"Name: {name}{Environment.NewLine}Remove \"{display}\" from the model name.",
                $"Model name cannot contain \"{display}\".");
        }

        if (name.EndsWith(' ') || name.EndsWith('.'))
        {
            return new ModelNameValidationResult(
                false,
                reason,
                "Model name in ZIP cannot end with a space or period.",
                $"Name: {name}{Environment.NewLine}Remove the trailing space or period.",
                "Model name cannot end with a space or period.");
        }

        var stem = Path.GetFileNameWithoutExtension(name);
        if (ReservedNames.Contains(stem) || ReservedNames.Contains(name))
        {
            return new ModelNameValidationResult(
                false,
                reason,
                "That model name is reserved by Windows.",
                $"Name: {name}{Environment.NewLine}Choose another model name.",
                "That name is reserved by Windows. Choose another model name.");
        }

        return new ModelNameValidationResult(
            false,
            reason,
            "Model name in ZIP is not valid.",
            $"Name: {name}{Environment.NewLine}{reason}",
            reason);
    }

    private static char? FirstInvalidCharacter(string value)
    {
        foreach (var character in value)
        {
            if (character == '\\'
                || character == '/'
                || Path.GetInvalidFileNameChars().Contains(character))
            {
                return character;
            }
        }

        return null;
    }
}

public sealed record ModelNameValidationResult(
    bool IsValid,
    string Reason,
    string ReviewMessage,
    string ReviewDetail,
    string InlineMessage)
{
    public static ModelNameValidationResult Valid { get; } = new(true, string.Empty, string.Empty, string.Empty, string.Empty);
}
