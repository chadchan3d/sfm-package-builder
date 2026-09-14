namespace SfmPackageBuilder.Core.Build;

public sealed class PhysicalStorageSpaceProvider : IStorageSpaceProvider
{
    public StorageSpaceInfo GetAvailableFreeBytes(string path)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            var root = Path.GetPathRoot(fullPath);
            if (string.IsNullOrWhiteSpace(root))
            {
                return StorageSpaceInfo.Unknown("The filesystem volume could not be determined.");
            }

            var drive = new DriveInfo(root);
            return StorageSpaceInfo.Known(drive.AvailableFreeSpace, drive.RootDirectory.FullName);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return StorageSpaceInfo.Unknown(ex.Message);
        }
    }
}
