namespace SfmPackageBuilder.Core.Build;

public interface IStorageSpaceProvider
{
    StorageSpaceInfo GetAvailableFreeBytes(string path);
}
