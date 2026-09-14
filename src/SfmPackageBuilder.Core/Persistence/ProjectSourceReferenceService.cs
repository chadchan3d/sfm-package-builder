using SfmPackageBuilder.Core.Domain;
using SfmPackageBuilder.Core.FileSystem;
using SfmPackageBuilder.Core.Paths;

namespace SfmPackageBuilder.Core.Persistence;

public sealed class ProjectSourceReferenceService
{
    private readonly IFileSystem fileSystem;
    private readonly PathAnchorDetector anchorDetector = new();

    public ProjectSourceReferenceService(IFileSystem fileSystem)
    {
        this.fileSystem = fileSystem;
    }

    public void EnsureReferences(PackageProject project)
    {
        foreach (var reference in Enumerate(project))
        {
            if (reference.Reference is null && !string.IsNullOrWhiteSpace(reference.Path))
            {
                reference.SetReference(new PersistedSourceReference { AbsolutePath = reference.Path });
            }
        }
    }

    public void PrepareForSave(PackageProject project, string projectPath)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);

        var projectDirectory = Path.GetDirectoryName(Path.GetFullPath(projectPath));
        foreach (var reference in Enumerate(project))
        {
            var path = reference.Path;
            if (string.IsNullOrWhiteSpace(path))
            {
                reference.SetReference(null);
                continue;
            }

            var absolute = ToAbsolutePath(path);
            reference.SetReference(CreateReference(projectDirectory, absolute, reference.IsFolder, reference.AnchorKind, anchorDetector));
        }
    }

    public void ResolveAfterLoad(PackageProject project, string projectPath)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);

        var projectDirectory = Path.GetDirectoryName(Path.GetFullPath(projectPath));
        foreach (var reference in Enumerate(project))
        {
            var persisted = reference.Reference;
            if (persisted is null)
            {
                if (!string.IsNullOrWhiteSpace(reference.Path))
                {
                    reference.SetReference(new PersistedSourceReference { AbsolutePath = ToAbsolutePath(reference.Path) });
                }

                continue;
            }

            var relativeCandidate = ResolveRelative(projectDirectory, persisted.RelativePath);
            if (relativeCandidate is not null && Exists(relativeCandidate, reference.IsFolder))
            {
                reference.SetPath(relativeCandidate);
                continue;
            }

            if (!string.IsNullOrWhiteSpace(persisted.AbsolutePath) && Exists(persisted.AbsolutePath, reference.IsFolder))
            {
                reference.SetPath(persisted.AbsolutePath);
                continue;
            }

            var fallback = !string.IsNullOrWhiteSpace(persisted.AbsolutePath)
                ? persisted.AbsolutePath
                : relativeCandidate ?? reference.Path;
            reference.SetPath(fallback);
        }
    }

    internal static IReadOnlyList<SourceReferenceAccessor> Enumerate(PackageProject project)
    {
        var references = new List<SourceReferenceAccessor>();

        foreach (var model in project.Models)
        {
            references.Add(new SourceReferenceAccessor(
                model.SourceMdlPath,
                isFolder: false,
                SourceAnchorKind.Models,
                model.SourceReference,
                path => model.SourceMdlPath = path,
                sourceReference => model.SourceReference = sourceReference));

            foreach (var companion in model.Companions)
            {
                references.Add(new SourceReferenceAccessor(
                    companion.SourcePath,
                    isFolder: false,
                    SourceAnchorKind.Models,
                    companion.SourceReference,
                    path => companion.SourcePath = path,
                    sourceReference => companion.SourceReference = sourceReference));
            }
        }

        foreach (var source in project.MaterialSources)
        {
            references.Add(new SourceReferenceAccessor(
                source.SourcePath,
                source.IsFolder,
                SourceAnchorKind.Materials,
                source.SourceReference,
                path => source.SourcePath = path,
                sourceReference => source.SourceReference = sourceReference));
        }

        foreach (var source in project.Extras)
        {
            references.Add(new SourceReferenceAccessor(
                source.SourcePath,
                source.IsFolder,
                null,
                source.SourceReference,
                path => source.SourcePath = path,
                sourceReference => source.SourceReference = sourceReference));
        }

        references.Add(new SourceReferenceAccessor(
            project.Readme.ImportedReadmePath ?? string.Empty,
            isFolder: false,
            null,
            project.Readme.ImportedReadmeReference,
            path => project.Readme.ImportedReadmePath = string.IsNullOrWhiteSpace(path) ? null : path,
            sourceReference => project.Readme.ImportedReadmeReference = sourceReference));

        return references;
    }

    internal static PersistedSourceReference CreateReferenceForProjectPath(string sourcePath, string projectPath)
    {
        return CreateReferenceForProjectPath(sourcePath, projectPath, isFolder: false, anchorKind: null);
    }

    internal static PersistedSourceReference CreateReferenceForSourcePath(
        string sourcePath,
        bool isFolder,
        SourceAnchorKind? anchorKind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        return CreateReference(null, ToAbsolutePath(sourcePath), isFolder, anchorKind, new PathAnchorDetector());
    }

    internal static PersistedSourceReference CreateReferenceForProjectPath(
        string sourcePath,
        string projectPath,
        bool isFolder,
        SourceAnchorKind? anchorKind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);

        var projectDirectory = Path.GetDirectoryName(Path.GetFullPath(projectPath));
        return CreateReference(projectDirectory, ToAbsolutePath(sourcePath), isFolder, anchorKind, new PathAnchorDetector());
    }

    private bool Exists(string path, bool isFolder) =>
        isFolder ? fileSystem.DirectoryExists(path) : fileSystem.FileExists(path);

    private static string ToAbsolutePath(string path) =>
        Path.GetFullPath(path).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

    private static PersistedSourceReference CreateReference(
        string? projectDirectory,
        string absolutePath,
        bool isFolder,
        SourceAnchorKind? anchorKind,
        PathAnchorDetector anchorDetector)
    {
        var recoveryLocation = CreateRecoveryLocation(absolutePath, isFolder, anchorKind, anchorDetector);
        return new PersistedSourceReference
        {
            AbsolutePath = absolutePath,
            RelativePath = CreateRelativePath(projectDirectory, absolutePath),
            RecoveryRootPath = recoveryLocation.RootPath,
            RecoveryRelativePath = recoveryLocation.RelativePath
        };
    }

    private static (string? RootPath, string? RelativePath) CreateRecoveryLocation(
        string absolutePath,
        bool isFolder,
        SourceAnchorKind? anchorKind,
        PathAnchorDetector anchorDetector)
    {
        if (anchorKind is not null)
        {
            var anchor = anchorDetector.Resolve(absolutePath, anchorKind.Value);
            if (anchor.IsResolved && !string.IsNullOrWhiteSpace(anchor.DestinationRelativePath))
            {
                var normalizedAbsolute = ToAbsolutePath(absolutePath).TrimEnd(Path.DirectorySeparatorChar);
                var normalizedRelative = anchor.DestinationRelativePath!.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                var suffix = Path.DirectorySeparatorChar + normalizedRelative.TrimStart(Path.DirectorySeparatorChar);
                if (normalizedAbsolute.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    return (normalizedAbsolute[..^suffix.Length], normalizedRelative);
                }
            }
        }

        var root = Path.GetDirectoryName(absolutePath);
        var remainder = Path.GetFileName(absolutePath);
        if (isFolder && string.IsNullOrWhiteSpace(remainder))
        {
            return (absolutePath, null);
        }

        return (string.IsNullOrWhiteSpace(root) ? null : ToAbsolutePath(root), string.IsNullOrWhiteSpace(remainder) ? null : remainder);
    }

    private static string? CreateRelativePath(string? projectDirectory, string absolutePath)
    {
        if (string.IsNullOrWhiteSpace(projectDirectory))
        {
            return null;
        }

        try
        {
            var relative = Path.GetRelativePath(projectDirectory, absolutePath)
                .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            return Path.IsPathFullyQualified(relative) ? null : relative;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    private static string? ResolveRelative(string? projectDirectory, string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(projectDirectory) || string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        try
        {
            return Path.GetFullPath(Path.Combine(projectDirectory, relativePath))
                .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    internal sealed class SourceReferenceAccessor
    {
        private readonly Action<string> setPath;
        private readonly Action<PersistedSourceReference?> setReference;

        public SourceReferenceAccessor(
            string path,
            bool isFolder,
            SourceAnchorKind? anchorKind,
            PersistedSourceReference? reference,
            Action<string> setPath,
            Action<PersistedSourceReference?> setReference)
        {
            Path = path;
            IsFolder = isFolder;
            AnchorKind = anchorKind;
            Reference = reference;
            this.setPath = setPath;
            this.setReference = setReference;
        }

        public string Path { get; private set; }

        public bool IsFolder { get; }

        public SourceAnchorKind? AnchorKind { get; }

        public PersistedSourceReference? Reference { get; private set; }

        public void SetPath(string path)
        {
            Path = path;
            setPath(path);
        }

        public void SetReference(PersistedSourceReference? reference)
        {
            Reference = reference;
            setReference(reference);
        }
    }
}
