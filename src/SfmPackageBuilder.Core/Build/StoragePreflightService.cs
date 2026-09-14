using SfmPackageBuilder.Core.FileSystem;
using SfmPackageBuilder.Core.Planning;

namespace SfmPackageBuilder.Core.Build;

public sealed class StoragePreflightService
{
    private const long FixedOverheadBytes = 1024 * 1024;
    private readonly IFileSystem fileSystem;
    private readonly IStorageSpaceProvider storageSpaceProvider;

    public StoragePreflightService(IFileSystem fileSystem, IStorageSpaceProvider storageSpaceProvider)
    {
        this.fileSystem = fileSystem;
        this.storageSpaceProvider = storageSpaceProvider;
    }

    public StoragePreflightResult Check(PackagePlan plan, string stagingRoot, string archivePath)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var packageBytes = EstimatePackageBytes(plan);
        if (packageBytes < 0)
        {
            return StoragePreflightResult.Success();
        }

        var stagingNeed = AddOverhead(packageBytes);
        var archiveNeed = AddOverhead(packageBytes);
        var stagingSpace = storageSpaceProvider.GetAvailableFreeBytes(stagingRoot);
        var archiveSpace = storageSpaceProvider.GetAvailableFreeBytes(archivePath);

        if (!stagingSpace.Available || !archiveSpace.Available)
        {
            return StoragePreflightResult.Success();
        }

        if (string.Equals(stagingSpace.VolumeRoot, archiveSpace.VolumeRoot, StringComparison.OrdinalIgnoreCase))
        {
            var combinedNeed = stagingNeed + archiveNeed;
            return stagingSpace.FreeBytes >= combinedNeed
                ? StoragePreflightResult.Success()
                : Insufficient(stagingSpace.FreeBytes, combinedNeed, stagingSpace.VolumeRoot);
        }

        if (stagingSpace.FreeBytes < stagingNeed)
        {
            return Insufficient(stagingSpace.FreeBytes, stagingNeed, stagingSpace.VolumeRoot);
        }

        return archiveSpace.FreeBytes >= archiveNeed
            ? StoragePreflightResult.Success()
            : Insufficient(archiveSpace.FreeBytes, archiveNeed, archiveSpace.VolumeRoot);
    }

    private long EstimatePackageBytes(PackagePlan plan)
    {
        long total = 0;
        foreach (var entry in plan.ResolvedEntries)
        {
            if (entry.ContentBytes is { } bytes)
            {
                total = checked(total + bytes.LongLength);
                continue;
            }

            if (string.IsNullOrWhiteSpace(entry.SourcePath))
            {
                continue;
            }

            try
            {
                using var stream = fileSystem.OpenRead(entry.SourcePath);
                total = checked(total + stream.Length);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or OverflowException)
            {
                return -1;
            }
        }

        return total;
    }

    private static long AddOverhead(long bytes)
    {
        try
        {
            return checked(bytes + Math.Max(FixedOverheadBytes, bytes / 20));
        }
        catch (OverflowException)
        {
            return long.MaxValue;
        }
    }

    private static StoragePreflightResult Insufficient(long available, long needed, string? volume) =>
        StoragePreflightResult.Failed(
            "There isn't enough free space to build this ZIP.",
            $"Volume: {volume ?? "unknown"}{Environment.NewLine}Available: {available:N0} bytes{Environment.NewLine}Estimated needed: {needed:N0} bytes");
}
