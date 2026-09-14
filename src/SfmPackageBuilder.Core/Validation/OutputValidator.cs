namespace SfmPackageBuilder.Core.Validation;

public sealed class OutputValidator
{
    private readonly IOutputEnvironment outputEnvironment;

    public OutputValidator(IOutputEnvironment outputEnvironment)
    {
        this.outputEnvironment = outputEnvironment;
    }

    public OutputValidationResult Validate(string outputDirectory, string archiveName)
    {
        var messages = new List<ValidationMessage>();
        var decisions = new List<RequiredDecision>();

        if (string.IsNullOrWhiteSpace(outputDirectory) || Path.IsPathRooted(archiveName))
        {
            messages.Add(new ValidationMessage(
                ValidationSeverity.Error,
                "invalid-output-location",
                "Choose a valid output folder and archive name."));
            return new OutputValidationResult(messages, decisions);
        }

        if (!WindowsFileNameValidator.IsValidFileName(archiveName, requireZipExtension: true, out var archiveReason))
        {
            messages.Add(new ValidationMessage(
                ValidationSeverity.Error,
                "invalid-archive-name",
                "The archive name is not a valid Windows ZIP filename.",
                technicalDetail: archiveReason));
        }

        if (!outputEnvironment.DirectoryExists(outputDirectory))
        {
            messages.Add(new ValidationMessage(
                ValidationSeverity.Error,
                "output-directory-missing",
                "The selected output folder could not be found.",
                sourcePaths: new[] { outputDirectory }));
            return new OutputValidationResult(messages, decisions);
        }

        var probe = outputEnvironment.CanWriteToDirectory(outputDirectory);
        if (!probe.CanWrite)
        {
            messages.Add(new ValidationMessage(
                ValidationSeverity.Error,
                "output-directory-unwritable",
                "The selected output folder is not writable.",
                sourcePaths: new[] { outputDirectory },
                technicalDetail: probe.TechnicalDetail));
        }

        if (messages.All(message => message.Code != "invalid-archive-name"))
        {
            var archivePath = Path.Combine(outputDirectory, archiveName);
            if (outputEnvironment.FileExists(archivePath))
            {
                decisions.Add(new RequiredDecision(
                    RequiredDecisionKind.ExistingOutputArchive,
                    "existing-output-archive",
                    "The output archive already exists.",
                    new[]
                    {
                        RequiredDecisionOption.Replace,
                        RequiredDecisionOption.ChooseAnotherName,
                        RequiredDecisionOption.Cancel
                    }));
            }
        }

        return new OutputValidationResult(messages, decisions);
    }
}
