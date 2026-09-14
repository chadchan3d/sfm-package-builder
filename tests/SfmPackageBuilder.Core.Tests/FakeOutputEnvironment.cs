using SfmPackageBuilder.Core.Validation;

namespace SfmPackageBuilder.Core.Tests;

internal sealed class FakeOutputEnvironment : IOutputEnvironment
{
    private readonly HashSet<string> directories = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> files = new(StringComparer.OrdinalIgnoreCase);

    public int WriteProbeCount { get; private set; }

    public bool CanWrite { get; set; } = true;

    public void AddDirectory(string path) => directories.Add(Normalize(path));

    public void AddFile(string path) => files.Add(Normalize(path));

    public bool DirectoryExists(string path) => directories.Contains(Normalize(path));

    public bool FileExists(string path) => files.Contains(Normalize(path));

    public OutputProbeResult CanWriteToDirectory(string path)
    {
        WriteProbeCount++;
        return CanWrite
            ? OutputProbeResult.Success()
            : OutputProbeResult.Failure("Simulated unwritable output directory.");
    }

    private static string Normalize(string path) => path.Replace('/', '\\').TrimEnd('\\');
}
