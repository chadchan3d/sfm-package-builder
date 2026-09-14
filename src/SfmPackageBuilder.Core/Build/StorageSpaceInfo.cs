namespace SfmPackageBuilder.Core.Build;

public sealed class StorageSpaceInfo
{
    private StorageSpaceInfo(bool available, long freeBytes, string? volumeRoot, string? technicalDetail)
    {
        Available = available;
        FreeBytes = freeBytes;
        VolumeRoot = volumeRoot;
        TechnicalDetail = technicalDetail;
    }

    public bool Available { get; }

    public long FreeBytes { get; }

    public string? VolumeRoot { get; }

    public string? TechnicalDetail { get; }

    public static StorageSpaceInfo Known(long freeBytes, string volumeRoot) =>
        new(true, freeBytes, volumeRoot, null);

    public static StorageSpaceInfo Unknown(string? technicalDetail = null) =>
        new(false, 0, null, technicalDetail);
}
