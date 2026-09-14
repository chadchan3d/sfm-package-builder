using SfmPackageBuilder.Core.Domain;
using SfmPackageBuilder.Core.FileSystem;
using SfmPackageBuilder.Core.Mdl;
using SfmPackageBuilder.Core.Paths;
using SfmPackageBuilder.Core.Planning;
using SfmPackageBuilder.Core.Readme;
using SfmPackageBuilder.Core.SharedFiles;

namespace SfmPackageBuilder.Core.Validation;

public sealed class PackageValidator
{
    private readonly IFileSystem fileSystem;
    private readonly OutputValidator outputValidator;
    private readonly CollisionValidator collisionValidator = new();
    private readonly PathValidation pathValidation = new();
    private readonly ReadmeContextBuilder readmeContextBuilder = new();
    private readonly ReadmeGenerator readmeGenerator = new();
    private readonly ReadmeFactsFingerprint readmeFactsFingerprint = new();

    public PackageValidator(
        IFileSystem fileSystem,
        IOutputEnvironment outputEnvironment)
    {
        this.fileSystem = fileSystem;
        outputValidator = new OutputValidator(outputEnvironment);
    }

    public ValidationResult Validate(PackageProject project, PackagePlan plan, PackageValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(context);

        var messages = new List<ValidationMessage>();
        var decisions = new List<RequiredDecision>();

        messages.AddRange(ValidateReleaseModelNames(project));
        messages.AddRange(ValidatePlanEntries(plan));
        messages.AddRange(collisionValidator.Validate(plan));
        messages.AddRange(ValidateReadme(project, plan));
        messages.AddRange(ValidateCustomReadmeProvenance(project, plan));
        messages.AddRange(ValidateSharedFiles(plan));
        messages.AddRange(ValidateMdlMaterialCoverage(plan));
        messages.AddRange(ValidateDuplicateReleaseHistory(project));
        decisions.AddRange(ValidateVersionReuse(project));

        var output = outputValidator.Validate(context.OutputDirectory, project.ArchiveName);
        messages.AddRange(output.Messages);
        decisions.AddRange(output.RequiredDecisions);

        if (!messages.Any(message => message.Severity == ValidationSeverity.Error)
            && !decisions.Any()
            && !messages.Any(message => message.Severity is ValidationSeverity.Warning or ValidationSeverity.Information))
        {
            messages.Add(new ValidationMessage(
                ValidationSeverity.Ready,
                "ready",
                "Ready."));
        }

        return new ValidationResult(messages, decisions);
    }

    private static IEnumerable<ValidationMessage> ValidateReleaseModelNames(PackageProject project)
    {
        foreach (var model in project.Models)
        {
            var effectiveStem = ModelReleaseName.EffectiveStem(model);
            var validation = WindowsFileNameValidator.ValidateModelStem(effectiveStem);
            if (!validation.IsValid)
            {
                yield return new ValidationMessage(
                    ValidationSeverity.Error,
                    "invalid-release-model-name",
                    validation.ReviewMessage,
                    detail: validation.ReviewDetail,
                    affectedValues: string.IsNullOrWhiteSpace(effectiveStem) ? Array.Empty<string>() : new[] { effectiveStem },
                    technicalDetail: validation.Reason);
            }
        }
    }

    private IEnumerable<ValidationMessage> ValidatePlanEntries(PackagePlan plan)
    {
        foreach (var entry in plan.Entries.OrderBy(entry => entry.Id, StringComparer.Ordinal))
        {
            if (entry.Status != PackagePlanEntryStatus.Resolved)
            {
                yield return CreateUnresolvedEntryMessage(entry);
                continue;
            }

            if (entry.DestinationRelativePath is null)
            {
                yield return new ValidationMessage(
                    ValidationSeverity.Error,
                    "missing-destination",
                    "A package entry is missing its package destination.",
                    SourceList(entry.SourcePath));
            }
            else
            {
                var destinationValidation = pathValidation.ValidatePackageRelativePath(entry.DestinationRelativePath, allowEmpty: false);
                if (!destinationValidation.IsValid)
                {
                    yield return new ValidationMessage(
                        ValidationSeverity.Error,
                        "unsafe-destination",
                        "A package location isn't valid.",
                        sourcePaths: SourceList(entry.SourcePath),
                        destinationRelativePath: entry.DestinationRelativePath,
                        technicalDetail: destinationValidation.Message);
                }
            }

            if (entry.SourcePath is not null)
            {
                foreach (var message in ValidateSourceBackedEntry(entry))
                {
                    yield return message;
                }
            }
        }
    }

    private IReadOnlyList<ValidationMessage> ValidateSourceBackedEntry(PackagePlanEntry entry)
    {
        if (!fileSystem.FileExists(entry.SourcePath!))
        {
            return new[]
            {
                new ValidationMessage(
                    ValidationSeverity.Error,
                    "source-missing",
                    "A selected source file could not be found.",
                    sourcePaths: new[] { entry.SourcePath! },
                    destinationRelativePath: entry.DestinationRelativePath,
                    detail: "Locate it or remove it from this package.")
            };
        }

        var messages = new List<ValidationMessage>();
        Stream? stream = null;
        try
        {
            stream = fileSystem.OpenRead(entry.SourcePath!);
            _ = stream.CanRead;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            messages.Add(new ValidationMessage(
                ValidationSeverity.Error,
                "source-unreadable",
                "A selected source file could not be read.",
                sourcePaths: new[] { entry.SourcePath! },
                destinationRelativePath: entry.DestinationRelativePath,
                detail: "Restore file access and try again.",
                technicalDetail: ex.Message));
        }
        finally
        {
            stream?.Dispose();
        }

        return messages;
    }

    private static ValidationMessage CreateUnresolvedEntryMessage(PackagePlanEntry entry)
    {
        var code = entry.Status switch
        {
            PackagePlanEntryStatus.MissingSource => "planned-source-missing",
            PackagePlanEntryStatus.MissingAnchor => "planned-anchor-missing",
            PackagePlanEntryStatus.InvalidDestinationOverride => "planned-invalid-destination-override",
            PackagePlanEntryStatus.SourceEnumerationFailed => "planned-source-enumeration-failed",
            PackagePlanEntryStatus.SourceReadFailed => "planned-source-read-failed",
            PackagePlanEntryStatus.ReadmeResolutionFailed => "planned-readme-resolution-failed",
            _ => "planned-entry-unresolved"
        };

        var message = entry.Status switch
        {
            PackagePlanEntryStatus.MissingSource => "A selected source could not be found.",
            PackagePlanEntryStatus.MissingAnchor => "A selected source has no models or materials anchor and needs a destination override.",
            PackagePlanEntryStatus.InvalidDestinationOverride => "A package location override is invalid.",
            PackagePlanEntryStatus.SourceEnumerationFailed => "A selected folder could not be read.",
            PackagePlanEntryStatus.SourceReadFailed => "A selected file could not be read.",
            PackagePlanEntryStatus.ReadmeResolutionFailed => "The README could not be prepared.",
            _ => "A package item could not be prepared."
        };

        return new ValidationMessage(
            ValidationSeverity.Error,
            code,
            message,
            sourcePaths: SourceList(entry.SourcePath),
            destinationRelativePath: entry.DestinationRelativePath,
            technicalDetail: entry.IssueDetail);
    }

    private static IEnumerable<ValidationMessage> ValidateReadme(PackageProject project, PackagePlan plan)
    {
        var readmes = plan.Entries.Where(entry => entry.EntryType == PackagePlanEntryType.Readme).ToArray();
        if (readmes.Length > 1)
        {
            yield return new ValidationMessage(
                ValidationSeverity.Error,
                "multiple-readme-entries",
                "More than one README.txt would be included.",
                destinationRelativePath: "README.txt");
            yield break;
        }

        if (readmes.Length == 0)
        {
            if (project.Readme.Mode == ReadmeMode.None)
            {
                yield return new ValidationMessage(
                    ValidationSeverity.Information,
                    "readme-none",
                    "No README will be included.");
            }

            yield break;
        }

        var readme = readmes[0];
        if (readme.Status != PackagePlanEntryStatus.Resolved)
        {
            yield return CreateUnresolvedEntryMessage(readme);
            yield break;
        }

        if (readme.ReadmeKind is ReadmePlanEntryKind.GeneratedText or ReadmePlanEntryKind.CustomText)
        {
            if (readme.TextContent is null || readme.ContentBytes is null)
            {
                yield return new ValidationMessage(
                    ValidationSeverity.Error,
                    "readme-content-missing",
                    "The README text is missing.",
                    destinationRelativePath: readme.DestinationRelativePath);
            }
        }
        else if (readme.ReadmeKind == ReadmePlanEntryKind.ImportedFile && readme.SourcePath is null)
        {
            yield return new ValidationMessage(
                ValidationSeverity.Error,
                "imported-readme-source-missing",
                "The imported README source file is missing.",
                destinationRelativePath: readme.DestinationRelativePath);
        }
    }

    private IEnumerable<ValidationMessage> ValidateCustomReadmeProvenance(PackageProject project, PackagePlan plan)
    {
        if (project.Readme.Mode != ReadmeMode.Custom
            || project.Readme.CustomSource != ReadmeCustomSource.ProjectText
            || string.IsNullOrWhiteSpace(project.Readme.CustomReadmeGeneratedFromFingerprint))
        {
            yield break;
        }

        var ordinaryEntries = plan.Entries
            .Where(entry => entry.EntryType != PackagePlanEntryType.Readme)
            .ToArray();
        var context = readmeContextBuilder.Create(project, ordinaryEntries, plan.SharedFileNotices);
        var currentGeneratedText = readmeGenerator.Generate(project, context);
        var currentFingerprint = readmeFactsFingerprint.Create(currentGeneratedText);
        if (!string.Equals(project.Readme.CustomReadmeGeneratedFromFingerprint, currentFingerprint, StringComparison.Ordinal))
        {
            yield return new ValidationMessage(
                ValidationSeverity.Information,
                "custom-readme-package-details-changed",
                "Package details changed after you customized the README.",
                detail: "Review your custom README if it mentions those details.");
        }
    }

    private static IEnumerable<ValidationMessage> ValidateSharedFiles(PackagePlan plan)
    {
        foreach (var notice in plan.SharedFileNotices.OrderBy(notice => notice.CandidateId, StringComparer.Ordinal).ThenBy(notice => notice.Kind))
        {
            yield return new ValidationMessage(
                notice.Kind == SharedFileNoticeKind.LiveDestinationWarning ? ValidationSeverity.Warning : ValidationSeverity.Information,
                notice.Kind == SharedFileNoticeKind.LiveDestinationWarning ? "shared-file-live-destination" : "shared-file-information",
                notice.Title,
                sourcePaths: SourceList(notice.SourcePath),
                destinationRelativePath: notice.DestinationRelativePath,
                detail: notice.Body);
        }
    }

    private static IEnumerable<ValidationMessage> ValidateMdlMaterialCoverage(PackagePlan plan)
    {
        var materialDestinations = plan.ResolvedEntries
            .Where(entry => entry.EntryType == PackagePlanEntryType.Material)
            .Where(entry => entry.DestinationRelativePath is not null)
            .Where(entry => HasVmtExtension(entry.DestinationRelativePath!))
            .Select(entry => new PlannedMaterialDestination(entry.Id, entry.DestinationRelativePath!, NormalizePackagePath(entry.DestinationRelativePath!)))
            .OrderBy(entry => entry.NormalizedDestination, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var facts in plan.ModelMaterialFacts.OrderBy(facts => facts.SourceMdlPath, StringComparer.OrdinalIgnoreCase))
        {
            if (facts.ReadStatus == MdlMetadataReadStatus.UnsupportedFormatOrVersion)
            {
                yield return new ValidationMessage(
                    ValidationSeverity.Information,
                    "mdl-material-inspection-unavailable",
                    "This model's materials could not be checked automatically.",
                    sourcePaths: new[] { facts.SourceMdlPath },
                    destinationRelativePath: facts.ModelDestinationRelativePath,
                    detail: "Add material folders manually if this model uses custom materials.");
                continue;
            }

            if (facts.ReadStatus is MdlMetadataReadStatus.MalformedMetadata or MdlMetadataReadStatus.PartialMetadata
                && facts.Diagnostics.Any(diagnostic => diagnostic.Severity == MdlMetadataDiagnosticSeverity.Malformed))
            {
                yield return new ValidationMessage(
                    ValidationSeverity.Warning,
                    "mdl-material-inspection-partial",
                    "This model's material metadata was only partially readable.",
                    sourcePaths: new[] { facts.SourceMdlPath },
                    destinationRelativePath: facts.ModelDestinationRelativePath,
                    detail: "Some automatic material checks may be incomplete.");
            }

            var materialCoverageMessages = ValidateModelMaterialCoverage(
                facts,
                materialDestinations,
                noPackagedMaterials: materialDestinations.Length == 0).ToArray();
            var hasReferencedMaterialWarning = materialCoverageMessages.Any(message => message.Code == "mdl-material-referenced-not-packaged");

            if (facts.ReadStatus == MdlMetadataReadStatus.Available && materialDestinations.Length == 0 && !hasReferencedMaterialWarning)
            {
                yield return new ValidationMessage(
                    ValidationSeverity.Information,
                    "mdl-material-none-auto-discovered",
                    "No materials were found automatically for this model.",
                    sourcePaths: new[] { facts.SourceMdlPath },
                    destinationRelativePath: facts.ModelDestinationRelativePath,
                    detail: "If the model uses custom materials, add their folder or files on Model & Materials.");
            }

            foreach (var message in materialCoverageMessages)
            {
                yield return message;
            }
        }
    }

    private static IEnumerable<ValidationMessage> ValidateModelMaterialCoverage(
        PlannedModelMaterialFacts facts,
        IReadOnlyList<PlannedMaterialDestination> materialDestinations,
        bool noPackagedMaterials)
    {
        if (facts.TextureReferences.Count == 0 || facts.MaterialSearchPaths.Count == 0)
        {
            yield break;
        }

        var references = GetReferencedMaterials(facts).ToArray();
        if (references.Length == 0)
        {
            yield break;
        }

        var missing = new List<MaterialCoverageObservation>();
        foreach (var reference in references)
        {
            var candidates = CreateCandidateDestinations(reference, facts.MaterialSearchPaths).ToArray();
            if (candidates.Length == 0)
            {
                continue;
            }

            var covered = materialDestinations.FirstOrDefault(destination =>
                candidates.Any(candidate => string.Equals(candidate.NormalizedDestination, destination.NormalizedDestination, StringComparison.OrdinalIgnoreCase)));
            if (covered is not null)
            {
                continue;
            }

            var sameName = materialDestinations
                .Where(destination => string.Equals(
                    Path.GetFileName(destination.NormalizedDestination),
                    Path.GetFileName(candidates[0].NormalizedDestination),
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (sameName.Length == 1)
            {
                yield return new ValidationMessage(
                    ValidationSeverity.Warning,
                    "mdl-material-possibly-misplaced",
                    "A material may be in the wrong folder for this model.",
                    sourcePaths: new[] { facts.SourceMdlPath },
                    relatedEntryIds: new[] { sameName[0].EntryId },
                    destinationRelativePath: sameName[0].DestinationRelativePath,
                    detail: $"Material: {Path.GetFileName(candidates[0].NormalizedDestination)}{Environment.NewLine}Model uses: {FormatCandidateRoots(candidates)}{Environment.NewLine}Package location: {sameName[0].DestinationRelativePath}",
                    affectedValues: new[] { reference },
                    candidateDestinations: candidates.Select(candidate => candidate.DestinationRelativePath));
                continue;
            }

            missing.Add(new MaterialCoverageObservation(reference, candidates));
        }

        if (missing.Count > 0)
        {
            yield return new ValidationMessage(
                ValidationSeverity.Warning,
                "mdl-material-referenced-not-packaged",
                "Some materials used by this model are not included.",
                sourcePaths: new[] { facts.SourceMdlPath },
                destinationRelativePath: facts.ModelDestinationRelativePath,
                detail: noPackagedMaterials
                    ? "Package Builder did not find those materials automatically. If they are custom materials, add their folder or files on Model & Materials."
                    : "They may come from Source Filmmaker or another required addon. Review the list if this release is meant to include them.",
                affectedValues: missing.Select(item => item.MaterialReference),
                candidateDestinations: missing.SelectMany(item => item.CandidateDestinations.Select(candidate => candidate.DestinationRelativePath)));
        }
    }

    private static IEnumerable<string> GetReferencedMaterials(PlannedModelMaterialFacts facts)
    {
        var references = new List<string>();
        if (facts.SkinTable is not null)
        {
            foreach (var row in facts.SkinTable.RemapMatrix)
            {
                foreach (var index in row)
                {
                    if (index >= 0 && index < facts.TextureReferences.Count)
                    {
                        references.Add(facts.TextureReferences[index]);
                    }
                }
            }
        }
        else
        {
            references.AddRange(facts.TextureReferences);
        }

        return references
            .Where(reference => !string.IsNullOrWhiteSpace(reference))
            .GroupBy(reference => NormalizeReferenceKey(reference), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(reference => NormalizeReferenceKey(reference), StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<CandidateMaterialDestination> CreateCandidateDestinations(
        string materialReference,
        IReadOnlyList<string> materialSearchPaths)
    {
        var normalizedReference = NormalizeMaterialReference(materialReference);
        if (string.IsNullOrWhiteSpace(normalizedReference))
        {
            yield break;
        }

        foreach (var searchPath in materialSearchPaths
            .Where(path => path is not null)
            .Select(NormalizeSearchPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase))
        {
            var destination = NormalizePackagePath(CombineMaterialCandidate(searchPath, normalizedReference));
            yield return new CandidateMaterialDestination(destination, NormalizePackagePath(destination));
        }
    }

    private static string CombineMaterialCandidate(string searchPath, string materialReference)
    {
        var relative = string.IsNullOrEmpty(searchPath)
            ? materialReference
            : searchPath.TrimEnd('\\') + "\\" + materialReference.TrimStart('\\');
        return "materials\\" + relative.TrimStart('\\');
    }

    private static string NormalizeSearchPath(string value)
    {
        var normalized = NormalizePackagePath(value).TrimStart('\\');
        if (normalized.StartsWith(@"materials\", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[@"materials\".Length..];
        }

        return normalized.TrimEnd('\\');
    }

    private static string NormalizeMaterialReference(string value)
    {
        var normalized = NormalizePackagePath(value).TrimStart('\\');
        return HasVmtExtension(normalized)
            ? normalized
            : normalized + ".vmt";
    }

    private static string NormalizeReferenceKey(string value) =>
        NormalizeMaterialReference(value).ToUpperInvariant();

    private static string NormalizePackagePath(string value)
    {
        var parts = value
            .Replace('/', '\\')
            .Split('\\', StringSplitOptions.RemoveEmptyEntries)
            .Where(part => part != ".")
            .ToArray();
        return string.Join("\\", parts);
    }

    private static bool HasVmtExtension(string path) =>
        string.Equals(Path.GetExtension(path), ".vmt", StringComparison.OrdinalIgnoreCase);

    private static string FormatCandidateRoots(IReadOnlyList<CandidateMaterialDestination> candidates) =>
        string.Join(", ", candidates
            .Select(candidate => Path.GetDirectoryName(candidate.DestinationRelativePath)?.Replace('/', '\\') ?? string.Empty)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase));

    private static IEnumerable<RequiredDecision> ValidateVersionReuse(PackageProject project)
    {
        if (string.IsNullOrWhiteSpace(project.CurrentVersion))
        {
            yield break;
        }

        if (project.BuildHistory.Any(record => string.Equals(record.Version, project.CurrentVersion, StringComparison.OrdinalIgnoreCase)))
        {
            yield return new RequiredDecision(
                RequiredDecisionKind.VersionReuse,
                "version-reuse",
                $"Version {project.CurrentVersion} already exists in this project.",
                new[]
                {
                    RequiredDecisionOption.RebuildExistingVersion,
                    RequiredDecisionOption.ChangeVersion,
                    RequiredDecisionOption.Cancel
                });
        }
    }

    private static IEnumerable<ValidationMessage> ValidateDuplicateReleaseHistory(PackageProject project)
    {
        foreach (var group in project.ReleaseHistory
            .Where(record => !string.IsNullOrWhiteSpace(record.Version))
            .GroupBy(record => record.Version.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            yield return new ValidationMessage(
                ValidationSeverity.Warning,
                "duplicate-readme-changelog-version",
                $"Version {group.Key} appears more than once in the README changelog.",
                detail: "Remove or edit duplicate changelog entries if they were added by mistake.");
        }
    }

    private static IReadOnlyList<string> SourceList(string? sourcePath)
    {
        return string.IsNullOrWhiteSpace(sourcePath) ? Array.Empty<string>() : new[] { sourcePath };
    }

    private sealed record PlannedMaterialDestination(string EntryId, string DestinationRelativePath, string NormalizedDestination);

    private sealed record CandidateMaterialDestination(string DestinationRelativePath, string NormalizedDestination);

    private sealed record MaterialCoverageObservation(string MaterialReference, IReadOnlyList<CandidateMaterialDestination> CandidateDestinations);
}
