using SfmPackageBuilder.Core.Domain;
using SfmPackageBuilder.Core.Planning;
using SfmPackageBuilder.Core.Validation;

namespace SfmPackageBuilder.WinForms.Presentation;

public sealed class ValidationPresenter
{
    public static string FormatStatus(ValidationSeverity severity) => severity switch
    {
        ValidationSeverity.Information => "Note",
        ValidationSeverity.Warning => "Warning",
        ValidationSeverity.Error => "Error",
        ValidationSeverity.Ready => "Ready",
        _ => severity.ToString()
    };

    public IReadOnlyList<ValidationDisplayItem> Present(ValidationResult result)
    {
        return Present(result, null, null);
    }

    public IReadOnlyList<ValidationDisplayItem> Present(ValidationResult result, PackageProject? project, PackagePlan? plan)
    {
        var items = new List<ValidationDisplayItem>();
        var modelFamilyCollisions = CreateModelFamilyCollisionSummaries(result, project, plan).ToArray();
        var groupedCollisionMessages = modelFamilyCollisions
            .SelectMany(group => group.Messages)
            .ToHashSet();
        var duplicateSourceMessages = result.Messages
            .Where(message => message.Code == "duplicate-source-destination" && message.Severity == ValidationSeverity.Information)
            .ToArray();

        if (duplicateSourceMessages.Length > 0)
        {
            items.Add(CreateDuplicateSourceSummary(duplicateSourceMessages));
        }

        items.AddRange(modelFamilyCollisions.Select(group => group.Item));

        foreach (var message in result.Messages.Where(message =>
            !groupedCollisionMessages.Contains(message)
            && (message.Code != "duplicate-source-destination" || message.Severity != ValidationSeverity.Information)))
        {
            var details = CreateDetails(message);
            items.Add(new ValidationDisplayItem(message.Severity, message.Message, details));
        }

        foreach (var decision in result.RequiredDecisions)
        {
            items.Add(PresentDecision(decision));
        }

        return items;
    }

    private static ValidationDisplayItem PresentDecision(RequiredDecision decision)
    {
        return decision.Kind switch
        {
            RequiredDecisionKind.VersionReuse => new ValidationDisplayItem(
                ValidationSeverity.Warning,
                ToVersionReuseSummary(decision.Message),
                "Change the version if this is a new release."),
            RequiredDecisionKind.ExistingOutputArchive => new ValidationDisplayItem(
                ValidationSeverity.Warning,
                "The output ZIP already exists.",
                "Package Builder will ask whether to replace it, choose another name, or cancel."),
            _ => new ValidationDisplayItem(
                ValidationSeverity.Warning,
                decision.Message,
                "Review this before continuing.")
        };
    }

    private static string ToVersionReuseSummary(string message)
    {
        const string oldSuffix = " already exists in this project.";
        if (message.StartsWith("Version ", StringComparison.Ordinal)
            && message.EndsWith(oldSuffix, StringComparison.Ordinal))
        {
            return message[..^oldSuffix.Length] + " was already used for this project.";
        }

        return message;
    }

    private static IEnumerable<ModelFamilyCollisionSummary> CreateModelFamilyCollisionSummaries(
        ValidationResult result,
        PackageProject? project,
        PackagePlan? plan)
    {
        if (project is null || plan is null)
        {
            yield break;
        }

        var primary = project.Models.FirstOrDefault(model => model.Role == ModelRole.Primary);
        if (primary is null)
        {
            yield break;
        }

        var primaryReleaseStem = string.IsNullOrWhiteSpace(primary.ReleaseStem) ? primary.SourceStem : primary.ReleaseStem;
        if (string.IsNullOrWhiteSpace(primaryReleaseStem)
            || string.Equals(primaryReleaseStem, primary.SourceStem, StringComparison.OrdinalIgnoreCase))
        {
            yield break;
        }

        var entriesById = plan.Entries.ToDictionary(entry => entry.Id, StringComparer.Ordinal);
        var modelsById = project.Models.ToDictionary(model => model.Id);
        var candidates = result.Messages
            .Where(message => message.Code == "destination-collision" && message.Severity == ValidationSeverity.Error)
            .Select(message => CreateModelFamilyCollisionCandidate(message, entriesById, modelsById, primary.Id))
            .Where(candidate => candidate is not null)
            .Select(candidate => candidate!)
            .GroupBy(candidate => candidate.AdditionalModelId)
            .Where(group => group.Count() > 1);

        foreach (var group in candidates)
        {
            var messages = group.Select(candidate => candidate.Message).ToArray();
            var destinations = messages
                .Select(message => message.DestinationRelativePath)
                .Where(destination => !string.IsNullOrWhiteSpace(destination))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(destination => destination, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var sources = messages
                .SelectMany(message => message.SourcePaths)
                .Where(source => !string.IsNullOrWhiteSpace(source))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(source => source, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var count = destinations.Length == 0 ? messages.Length : destinations.Length;
            var title = $"The renamed primary model conflicts with an Additional Model at {count} package {(count == 1 ? "location" : "locations")}.";
            var details = "Conflicting package locations:" + Environment.NewLine + Environment.NewLine +
                string.Join(Environment.NewLine, destinations);
            if (sources.Length > 0)
            {
                details += Environment.NewLine + Environment.NewLine +
                    "Competing sources:" + Environment.NewLine + Environment.NewLine +
                    string.Join(Environment.NewLine, sources);
            }

            yield return new ModelFamilyCollisionSummary(
                new ValidationDisplayItem(ValidationSeverity.Error, title, details),
                messages);
        }
    }

    private static ModelFamilyCollisionCandidate? CreateModelFamilyCollisionCandidate(
        ValidationMessage message,
        IReadOnlyDictionary<string, PackagePlanEntry> entriesById,
        IReadOnlyDictionary<Guid, ModelEntry> modelsById,
        Guid primaryModelId)
    {
        var entries = message.RelatedEntryIds
            .Where(entriesById.ContainsKey)
            .Select(id => entriesById[id])
            .Where(entry => entry.EntryType is PackagePlanEntryType.Model or PackagePlanEntryType.ModelCompanion)
            .Where(entry => entry.ModelEntryId is not null)
            .ToArray();
        if (!entries.Any(entry => entry.ModelEntryId == primaryModelId))
        {
            return null;
        }

        var additionalIds = entries
            .Select(entry => entry.ModelEntryId!.Value)
            .Where(id => id != primaryModelId)
            .Where(id => modelsById.TryGetValue(id, out var model) && model.Role == ModelRole.Additional)
            .Distinct()
            .ToArray();

        return additionalIds.Length == 1
            ? new ModelFamilyCollisionCandidate(message, additionalIds[0])
            : null;
    }

    private static ValidationDisplayItem CreateDuplicateSourceSummary(IReadOnlyList<ValidationMessage> messages)
    {
        var count = messages.Count;
        var title = count == 1
            ? "1 file is included through more than one source. It will only be packaged once."
            : $"{count} files are included through more than one source. They will only be packaged once.";
        var details = string.Join(
            Environment.NewLine + Environment.NewLine,
            messages.Select(message => CreateDetails(message)));

        return new ValidationDisplayItem(ValidationSeverity.Information, title, details);
    }

    private static string CreateDetails(ValidationMessage message)
    {
        var sections = new List<string>();
        if (!string.IsNullOrWhiteSpace(message.Detail))
        {
            sections.Add(message.Detail);
        }

        if (!string.IsNullOrWhiteSpace(message.DestinationRelativePath))
        {
            sections.Add("Package destination:" + Environment.NewLine + message.DestinationRelativePath);
        }

        if (message.SourcePaths.Count > 0)
        {
            sections.Add("Source path:" + Environment.NewLine + string.Join(Environment.NewLine, message.SourcePaths));
        }

        if (message.AffectedValues.Count > 0)
        {
            sections.Add("Affected values:" + Environment.NewLine + string.Join(Environment.NewLine, message.AffectedValues));
        }

        if (message.CandidateDestinations.Count > 0)
        {
            sections.Add("Candidate material locations:" + Environment.NewLine + string.Join(Environment.NewLine, message.CandidateDestinations));
        }

        if (string.IsNullOrWhiteSpace(message.Detail) && !string.IsNullOrWhiteSpace(message.TechnicalDetail))
        {
            sections.Add("Technical detail:" + Environment.NewLine + message.TechnicalDetail);
        }

        return string.Join(Environment.NewLine + Environment.NewLine, sections);
    }

    private sealed record ModelFamilyCollisionCandidate(ValidationMessage Message, Guid AdditionalModelId);

    private sealed record ModelFamilyCollisionSummary(ValidationDisplayItem Item, IReadOnlyList<ValidationMessage> Messages);
}
