using SfmPackageBuilder.Core.Validation;

namespace SfmPackageBuilder.WinForms.Presentation;

public sealed class ValidationDisplayItem
{
    public ValidationDisplayItem(ValidationSeverity severity, string title, string details)
    {
        Severity = severity;
        Title = title;
        Details = details;
    }

    public ValidationSeverity Severity { get; }

    public string Title { get; }

    public string Details { get; }
}
