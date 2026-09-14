namespace SfmPackageBuilder.Core.Paths;

public sealed class DestinationPath
{
    private DestinationPath(string relativePath)
    {
        RelativePath = relativePath;
    }

    public string RelativePath { get; }

    public static PathValidationResult Validate(string relativePath, bool allowEmpty = true)
    {
        return new PathValidation().ValidatePackageRelativePath(relativePath, allowEmpty);
    }

    public static string ToArchiveEntryName(string normalizedRelativePath) =>
        string.Join('/', normalizedRelativePath.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries));

    public static bool TryCreate(string relativePath, out DestinationPath? destinationPath, out PathValidationResult validationResult)
    {
        validationResult = Validate(relativePath);
        if (!validationResult.IsValid)
        {
            destinationPath = null;
            return false;
        }

        destinationPath = new DestinationPath(validationResult.NormalizedPath!);
        return true;
    }

    public override string ToString() => RelativePath;
}
