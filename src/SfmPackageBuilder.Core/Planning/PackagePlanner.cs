using SfmPackageBuilder.Core.Domain;
using SfmPackageBuilder.Core.Expansion;
using SfmPackageBuilder.Core.FileSystem;
using SfmPackageBuilder.Core.Mdl;
using SfmPackageBuilder.Core.Paths;
using SfmPackageBuilder.Core.Readme;
using SfmPackageBuilder.Core.SharedFiles;

namespace SfmPackageBuilder.Core.Planning;

public sealed class PackagePlanner
{
    private readonly SourceExpansionService sourceExpansionService;
    private readonly PathAnchorDetector anchorDetector = new();
    private readonly PathValidation pathValidation = new();
    private readonly OverlapNormalizer overlapNormalizer = new();
    private readonly SharedFileRegistry sharedFileRegistry;
    private readonly ReadmeResolver readmeResolver;
    private readonly ReadmeContextBuilder readmeContextBuilder = new();
    private readonly IMdlMetadataReader mdlMetadataReader;

    public PackagePlanner(IFileSystem fileSystem)
        : this(
            new SourceExpansionService(fileSystem),
            new SharedFileRegistry(),
            new ReadmeResolver(fileSystem),
            new MdlV49MetadataReader(fileSystem))
    {
    }

    public PackagePlanner(
        SourceExpansionService sourceExpansionService,
        SharedFileRegistry sharedFileRegistry,
        ReadmeResolver readmeResolver,
        IMdlMetadataReader? mdlMetadataReader = null)
    {
        this.sourceExpansionService = sourceExpansionService;
        this.sharedFileRegistry = sharedFileRegistry;
        this.readmeResolver = readmeResolver;
        this.mdlMetadataReader = mdlMetadataReader ?? new MdlV49MetadataReader();
    }

    public PackagePlan CreatePlan(PackageProject project)
    {
        ArgumentNullException.ThrowIfNull(project);

        var draft = CreateDraftPlan(project);
        var sharedCandidates = draft.OrdinaryEntries
            .Where(entry => entry.DestinationRelativePath is not null)
            .Select(entry => new SharedFileCandidate(
                entry.Id,
                entry.DestinationRelativePath!,
                entry.SourcePath,
                ToSharedFileOrigin(entry.EntryType)))
            .ToArray();
        var sharedNotices = sharedFileRegistry.Analyze(sharedCandidates);
        var sharedByCandidate = sharedNotices
            .GroupBy(notice => notice.CandidateId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);

        var ordinaryEntries = draft.OrdinaryEntries
            .Select(entry => sharedByCandidate.TryGetValue(entry.Id, out var notices)
                ? WithRiskMetadata(entry, notices)
                : entry)
            .ToArray();

        var readmeEntry = CreateReadmeEntry(project, ordinaryEntries, sharedNotices);
        var finalEntries = readmeEntry is null
            ? ordinaryEntries
            : ordinaryEntries.Concat(new[] { readmeEntry }).ToArray();
        var modelMaterialFacts = InspectModelMaterials(project, ordinaryEntries);

        return new PackagePlan(finalEntries, sharedNotices, modelMaterialFacts);
    }

    private DraftPackagePlan CreateDraftPlan(PackageProject project)
    {
        var entries = new List<PackagePlanEntry>();
        var expansion = sourceExpansionService.Resolve(new PackageProject
        {
            Models = project.Models,
            MaterialSources = overlapNormalizer.NormalizeMaterialSources(project.MaterialSources).ToList(),
            Extras = overlapNormalizer.NormalizeExtras(project.Extras).ToList()
        });

        foreach (var family in expansion.ModelFamilies)
        {
            entries.AddRange(CreateModelEntries(family));
        }

        foreach (var inventory in expansion.MaterialInventories)
        {
            entries.AddRange(CreateSourceEntries(inventory, PackagePlanEntryType.Material));
        }

        foreach (var inventory in expansion.ExtraInventories)
        {
            entries.AddRange(CreateSourceEntries(inventory, PackagePlanEntryType.Extra));
        }

        return new DraftPackagePlan(OrderEntries(entries));
    }

    private IEnumerable<PackagePlanEntry> CreateModelEntries(ModelFamily family)
    {
        return family.Files
            .Where(file => file.Kind == ModelFamilyFileKind.SelectedMdl || file.UserSelection == CompanionUserSelection.Include)
            .Select((file, index) => CreateModelEntry(family, file, index));
    }

    private PackagePlanEntry CreateModelEntry(ModelFamily family, ModelFamilyFile file, int index)
    {
        var destination = ResolveModelDestination(family, file);
        var status = ResolveStatus(file.ObservationStatus, destination);
        return new PackagePlanEntry(
            CreateEntryId("model", family.ModelEntry.Id, index),
            file.Kind == ModelFamilyFileKind.SelectedMdl ? PackagePlanEntryType.Model : PackagePlanEntryType.ModelCompanion,
            status,
            destination.Destination,
            file.SourcePath,
            family.ModelEntry.Id,
            family.ModelEntry.Id,
            isGenerated: false,
            issueDetail: destination.IssueDetail);
    }

    private DestinationResolution ResolveModelDestination(ModelFamily family, ModelFamilyFile file)
    {
        if (family.ModelEntry.DestinationOverride is not null)
        {
            var overrideResolution = ResolveOverrideDestination(
                family.ModelEntry.DestinationOverride,
                Path.GetFileName(file.SourcePath));

            return overrideResolution.IsResolved
                ? overrideResolution.WithDestination(ReplaceFileNameWithReleaseStem(
                    overrideResolution.Destination!,
                    GetReleaseStem(family),
                    file.RuntimeSuffix))
                : overrideResolution;
        }

        var natural = anchorDetector.Resolve(file.SourcePath, SourceAnchorKind.Models);
        if (!natural.IsResolved)
        {
            return DestinationResolution.Unresolved(PackagePlanEntryStatus.MissingAnchor, "A models anchor could not be inferred.");
        }

        return DestinationResolution.Resolved(ReplaceFileNameWithReleaseStem(
            natural.DestinationRelativePath!,
            GetReleaseStem(family),
            file.RuntimeSuffix));
    }

    private IEnumerable<PackagePlanEntry> CreateSourceEntries(SourceInventory inventory, PackagePlanEntryType entryType)
    {
        if (inventory.Status is SourceInventoryStatus.EnumerationFailed)
        {
            return new[]
            {
                new PackagePlanEntry(
                    CreateEntryId(entryType.ToString().ToLowerInvariant(), inventory.Origin.Id, 0),
                    entryType,
                    PackagePlanEntryStatus.SourceEnumerationFailed,
                    null,
                    inventory.Origin.SourcePath,
                    inventory.Origin.Id,
                    null,
                    isGenerated: false,
                    issueDetail: inventory.FailureDetail)
            };
        }

        if (inventory.Status is SourceInventoryStatus.MissingSelectedSource && inventory.Items.Count == 0)
        {
            return new[]
            {
                new PackagePlanEntry(
                    CreateEntryId(entryType.ToString().ToLowerInvariant(), inventory.Origin.Id, 0),
                    entryType,
                    PackagePlanEntryStatus.MissingSource,
                    null,
                    inventory.Origin.SourcePath,
                    inventory.Origin.Id,
                    null,
                    isGenerated: false,
                    issueDetail: inventory.FailureDetail)
            };
        }

        return inventory.Items
            .OrderBy(item => item.RelativePathWithinSelection, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.SourcePath, StringComparer.OrdinalIgnoreCase)
            .Select((item, index) => CreateSourceEntry(inventory, item, entryType, index))
            .ToArray();
    }

    private PackagePlanEntry CreateSourceEntry(
        SourceInventory inventory,
        SourceInventoryItem item,
        PackagePlanEntryType entryType,
        int index)
    {
        var destination = entryType switch
        {
            PackagePlanEntryType.Material => ResolveMaterialDestination(inventory.Origin, item),
            PackagePlanEntryType.Extra => ResolveExtraDestination(inventory.Origin, item),
            _ => throw new ArgumentOutOfRangeException(nameof(entryType), entryType, null)
        };
        var status = inventory.Status == SourceInventoryStatus.ReadFailed
            ? PackagePlanEntryStatus.SourceReadFailed
            : ResolveStatus(item.ObservationStatus, destination);

        return new PackagePlanEntry(
            CreateEntryId(entryType.ToString().ToLowerInvariant(), inventory.Origin.Id, index),
            entryType,
            status,
            destination.Destination,
            item.SourcePath,
            inventory.Origin.Id,
            null,
            isGenerated: false,
            issueDetail: destination.IssueDetail ?? inventory.FailureDetail);
    }

    private DestinationResolution ResolveMaterialDestination(SourceEntry origin, SourceInventoryItem item)
    {
        if (origin.DestinationOverride is not null)
        {
            return ResolveOverrideDestination(origin.DestinationOverride, origin.IsFolder
                ? item.RelativePathWithinSelection
                : Path.GetFileName(item.SourcePath));
        }

        var natural = anchorDetector.Resolve(item.SourcePath, SourceAnchorKind.Materials);
        return natural.IsResolved
            ? DestinationResolution.Resolved(natural.DestinationRelativePath!)
            : DestinationResolution.Unresolved(PackagePlanEntryStatus.MissingAnchor, "A materials anchor could not be inferred.");
    }

    private DestinationResolution ResolveExtraDestination(SourceEntry origin, SourceInventoryItem item)
    {
        var basePath = origin.DestinationOverride is null
            ? string.Empty
            : GetExtraOverrideBase(origin.DestinationOverride);

        var validation = pathValidation.ValidatePackageRelativePath(basePath);
        if (!validation.IsValid)
        {
            return DestinationResolution.Unresolved(
                PackagePlanEntryStatus.InvalidDestinationOverride,
                validation.Message ?? "The destination override is invalid.");
        }

        var childPath = origin.IsFolder ? item.RelativePathWithinSelection : Path.GetFileName(item.SourcePath);
        return DestinationResolution.Resolved(CombinePackagePath(validation.NormalizedPath!, childPath));
    }

    private DestinationResolution ResolveOverrideDestination(DestinationOverride destinationOverride, string childPath)
    {
        var basePath = destinationOverride.Kind == DestinationOverrideKind.Misc
            ? GetExtraOverrideBase(destinationOverride)
            : destinationOverride.RelativePath;
        var validation = pathValidation.ValidatePackageRelativePath(basePath);
        if (!validation.IsValid)
        {
            return DestinationResolution.Unresolved(
                PackagePlanEntryStatus.InvalidDestinationOverride,
                validation.Message ?? "The destination override is invalid.");
        }

        return DestinationResolution.Resolved(CombinePackagePath(validation.NormalizedPath!, childPath));
    }

    private PackagePlanEntry? CreateReadmeEntry(
        PackageProject project,
        IReadOnlyList<PackagePlanEntry> ordinaryEntries,
        IReadOnlyList<SharedFileNotice> sharedNotices)
    {
        var resolvedReadme = readmeResolver.Resolve(project, readmeContextBuilder.Create(project, ordinaryEntries, sharedNotices));

        if (resolvedReadme.Kind == ResolvedReadmeKind.None)
        {
            return null;
        }

        if (!resolvedReadme.IsResolved)
        {
            return new PackagePlanEntry(
                "readme",
                PackagePlanEntryType.Readme,
                PackagePlanEntryStatus.ReadmeResolutionFailed,
                "README.txt",
                null,
                null,
                null,
                isGenerated: false,
                readmeKind: ReadmePlanEntryKind.None,
                issueDetail: resolvedReadme.FailureDetail);
        }

        return resolvedReadme.Kind switch
        {
            ResolvedReadmeKind.GeneratedText => new PackagePlanEntry(
                "readme",
                PackagePlanEntryType.Readme,
                PackagePlanEntryStatus.Resolved,
                "README.txt",
                null,
                null,
                null,
                isGenerated: true,
                readmeKind: ReadmePlanEntryKind.GeneratedText,
                textContent: resolvedReadme.Text,
                contentBytes: resolvedReadme.GetGeneratedOrCustomBytes()),
            ResolvedReadmeKind.CustomText => new PackagePlanEntry(
                "readme",
                PackagePlanEntryType.Readme,
                PackagePlanEntryStatus.Resolved,
                "README.txt",
                null,
                null,
                null,
                isGenerated: true,
                readmeKind: ReadmePlanEntryKind.CustomText,
                textContent: resolvedReadme.Text,
                contentBytes: resolvedReadme.GetGeneratedOrCustomBytes()),
            ResolvedReadmeKind.ImportedFile => new PackagePlanEntry(
                "readme",
                PackagePlanEntryType.Readme,
                PackagePlanEntryStatus.Resolved,
                "README.txt",
                resolvedReadme.ImportedSourcePath,
                null,
                null,
                isGenerated: false,
                readmeKind: ReadmePlanEntryKind.ImportedFile,
                textContent: resolvedReadme.PreviewText),
            _ => null
        };
    }

    private static PackagePlanEntry WithRiskMetadata(PackagePlanEntry entry, IReadOnlyList<SharedFileNotice> notices)
    {
        return new PackagePlanEntry(
            entry.Id,
            entry.EntryType,
            entry.Status,
            entry.DestinationRelativePath,
            entry.SourcePath,
            entry.OriginProjectEntryId,
            entry.ModelEntryId,
            entry.IsGenerated,
            entry.ReadmeKind,
            entry.TextContent,
            entry.ContentBytes,
            entry.IssueDetail,
            new PlanRiskMetadata(notices));
    }

    private IReadOnlyList<PlannedModelMaterialFacts> InspectModelMaterials(PackageProject project, IReadOnlyList<PackagePlanEntry> ordinaryEntries)
    {
        var rolesByModelId = project.Models.ToDictionary(model => model.Id, model => model.Role);
        return ordinaryEntries
            .Where(entry => entry.EntryType == PackagePlanEntryType.Model)
            .Where(entry => entry.Status == PackagePlanEntryStatus.Resolved)
            .Where(entry => entry.SourcePath is not null && entry.ModelEntryId is not null)
            .OrderBy(entry => entry.Id, StringComparer.Ordinal)
            .Select(entry => InspectModelMaterialEntry(entry, rolesByModelId.TryGetValue(entry.ModelEntryId!.Value, out var role)
                ? role
                : ModelRole.Additional))
            .ToArray();
    }

    private PlannedModelMaterialFacts InspectModelMaterialEntry(PackagePlanEntry entry, ModelRole modelRole)
    {
        try
        {
            var result = mdlMetadataReader.Read(entry.SourcePath!);
            var metadata = result.Metadata;
            return new PlannedModelMaterialFacts(
                entry.ModelEntryId!.Value,
                modelRole,
                entry.SourcePath!,
                entry.DestinationRelativePath,
                result.Status,
                metadata?.Version,
                metadata?.Checksum,
                metadata?.RawHeaderModelName,
                metadata?.TextureReferences ?? Array.Empty<string>(),
                metadata?.MaterialSearchPaths ?? Array.Empty<string>(),
                metadata?.SkinTable,
                result.Diagnostics.Select(diagnostic => new PlannedModelMaterialDiagnostic(
                    diagnostic.Section,
                    diagnostic.Severity,
                    diagnostic.Code,
                    diagnostic.Message)).ToArray());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FileNotFoundException)
        {
            return new PlannedModelMaterialFacts(
                entry.ModelEntryId!.Value,
                modelRole,
                entry.SourcePath!,
                entry.DestinationRelativePath,
                MdlMetadataReadStatus.MalformedMetadata,
                null,
                null,
                null,
                Array.Empty<string>(),
                Array.Empty<string>(),
                null,
                new[]
                {
                    new PlannedModelMaterialDiagnostic(
                        MdlMetadataSection.Header,
                        MdlMetadataDiagnosticSeverity.Malformed,
                        "mdl-metadata-read-unavailable",
                        ex.Message)
                });
        }
    }

    private static PackagePlanEntryStatus ResolveStatus(
        SourceObservationStatus observationStatus,
        DestinationResolution destination)
    {
        if (observationStatus == SourceObservationStatus.Missing)
        {
            return PackagePlanEntryStatus.MissingSource;
        }

        if (observationStatus == SourceObservationStatus.ReadFailed)
        {
            return PackagePlanEntryStatus.SourceReadFailed;
        }

        return destination.Status;
    }

    private static IReadOnlyList<PackagePlanEntry> OrderEntries(IEnumerable<PackagePlanEntry> entries)
    {
        return entries.ToArray();
    }

    private static SharedFileCandidateOrigin ToSharedFileOrigin(PackagePlanEntryType entryType)
    {
        return entryType switch
        {
            PackagePlanEntryType.Extra => SharedFileCandidateOrigin.ExtraFile,
            PackagePlanEntryType.Material => SharedFileCandidateOrigin.MaterialFile,
            PackagePlanEntryType.Model or PackagePlanEntryType.ModelCompanion => SharedFileCandidateOrigin.ModelFamilyFile,
            _ => SharedFileCandidateOrigin.Unknown
        };
    }

    private static string ReplaceFileNameWithReleaseStem(string destinationRelativePath, string releaseStem, string runtimeSuffix)
    {
        var directory = Path.GetDirectoryName(destinationRelativePath)?.Replace('/', '\\');
        var fileName = releaseStem + runtimeSuffix;
        return string.IsNullOrEmpty(directory)
            ? fileName
            : directory + "\\" + fileName;
    }

    private static string GetReleaseStem(ModelFamily family)
    {
        var effectiveStem = ModelReleaseName.EffectiveStem(family.ModelEntry);
        return string.IsNullOrWhiteSpace(effectiveStem)
            ? family.SourceStem
            : effectiveStem;
    }

    private static string GetExtraOverrideBase(DestinationOverride destinationOverride)
    {
        return destinationOverride.Kind switch
        {
            DestinationOverrideKind.Root => destinationOverride.RelativePath,
            DestinationOverrideKind.Misc => string.IsNullOrWhiteSpace(destinationOverride.RelativePath)
                ? "Misc"
                : destinationOverride.RelativePath,
            DestinationOverrideKind.Custom => destinationOverride.RelativePath,
            _ => destinationOverride.RelativePath
        };
    }

    private static string CombinePackagePath(string basePath, string childPath)
    {
        if (string.IsNullOrEmpty(basePath))
        {
            return childPath.Replace('/', '\\');
        }

        if (string.IsNullOrEmpty(childPath))
        {
            return basePath.Replace('/', '\\');
        }

        return basePath.TrimEnd('\\', '/') + "\\" + childPath.TrimStart('\\', '/').Replace('/', '\\');
    }

    private static string CreateEntryId(string prefix, Guid originId, int index)
    {
        return $"{prefix}:{originId:N}:{index:D4}";
    }

    private sealed class DestinationResolution
    {
        private DestinationResolution(PackagePlanEntryStatus status, string? destination, string? issueDetail)
        {
            Status = status;
            Destination = destination;
            IssueDetail = issueDetail;
        }

        public PackagePlanEntryStatus Status { get; }

        public bool IsResolved => Status == PackagePlanEntryStatus.Resolved;

        public string? Destination { get; }

        public string? IssueDetail { get; }

        public DestinationResolution WithDestination(string destination) => Resolved(destination);

        public static DestinationResolution Resolved(string destination) =>
            new(PackagePlanEntryStatus.Resolved, destination, null);

        public static DestinationResolution Unresolved(PackagePlanEntryStatus status, string issueDetail) =>
            new(status, null, issueDetail);
    }
}
