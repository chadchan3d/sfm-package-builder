using SfmPackageBuilder.Core.Domain;
using SfmPackageBuilder.Core.FileSystem;

namespace SfmPackageBuilder.Core.Readme;

public sealed class ReadmeResolver
{
    private readonly IFileSystem fileSystem;
    private readonly ReadmeGenerator generator;

    public ReadmeResolver(IFileSystem fileSystem, ReadmeGenerator? generator = null)
    {
        this.fileSystem = fileSystem;
        this.generator = generator ?? new ReadmeGenerator();
    }

    public ResolvedReadme Resolve(PackageProject project, ReadmeContext context)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(context);

        return project.Readme.Mode switch
        {
            ReadmeMode.Generated => ResolvedReadme.Generated(generator.Generate(project, context)),
            ReadmeMode.Custom => ResolveCustom(project.Readme),
            ReadmeMode.None => ResolvedReadme.None(),
            _ => throw new ArgumentOutOfRangeException(nameof(project), project.Readme.Mode, null)
        };
    }

    private ResolvedReadme ResolveImported(string? importedReadmePath)
    {
        if (string.IsNullOrWhiteSpace(importedReadmePath) || !fileSystem.FileExists(importedReadmePath))
        {
            return ResolvedReadme.Failure(
                ReadmeResolutionStatus.MissingImportedReadme,
                "The imported README file is missing.");
        }

        try
        {
            using var stream = fileSystem.OpenRead(importedReadmePath);
            using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
            var previewText = reader.ReadToEnd();
            return ResolvedReadme.Imported(importedReadmePath, previewText);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return ResolvedReadme.Failure(ReadmeResolutionStatus.ImportedReadmeUnreadable, ex.Message);
        }
    }

    private ResolvedReadme ResolveCustom(ReadmeConfig readmeConfig)
    {
        return readmeConfig.CustomSource switch
        {
            ReadmeCustomSource.ImportedFile => ResolveImported(readmeConfig.ImportedReadmePath),
            ReadmeCustomSource.ProjectText => ResolveCustomText(readmeConfig.CustomReadmeText),
            _ => throw new ArgumentOutOfRangeException(nameof(readmeConfig), readmeConfig.CustomSource, null)
        };
    }

    private static ResolvedReadme ResolveCustomText(string? customReadmeText)
    {
        return customReadmeText is null
            ? ResolvedReadme.Failure(
                ReadmeResolutionStatus.CustomTextMissing,
                "The custom README text is missing.")
            : ResolvedReadme.Custom(customReadmeText);
    }
}
