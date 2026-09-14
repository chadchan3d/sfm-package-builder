namespace SfmPackageBuilder.Core.Validation;

public sealed class PhysicalOutputEnvironment : IOutputEnvironment
{
    public bool DirectoryExists(string path) => Directory.Exists(path);

    public bool FileExists(string path) => File.Exists(path);

    public OutputProbeResult CanWriteToDirectory(string path)
    {
        var probePath = Path.Combine(path, $".sfmpack_write_probe_{Guid.NewGuid():N}.tmp");

        try
        {
            File.WriteAllText(probePath, string.Empty);
            File.Delete(probePath);
            return OutputProbeResult.Success();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            try
            {
                if (File.Exists(probePath))
                {
                    File.Delete(probePath);
                }
            }
            catch (Exception cleanupException) when (cleanupException is IOException or UnauthorizedAccessException)
            {
                return OutputProbeResult.Failure(ex.Message + " Cleanup failed: " + cleanupException.Message);
            }

            return OutputProbeResult.Failure(ex.Message);
        }
    }
}
