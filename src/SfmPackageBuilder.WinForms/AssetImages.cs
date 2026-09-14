using System.Reflection;

namespace SfmPackageBuilder.WinForms;

internal static class AssetImages
{
    public static Image LoadLaunchLogo() =>
        Load("SfmPackageBuilder.WinForms.Assets.packagebuilderlogo_cropped.png");

    public static Image LoadEmblem() =>
        Load("SfmPackageBuilder.WinForms.Assets.packagebuilder_justlogo.png");

    public static Image LoadWordmark() =>
        Load("SfmPackageBuilder.WinForms.Assets.packagebuilderjusttitle.png");

    public static Image LoadSuccessArtwork() =>
        Load("SfmPackageBuilder.WinForms.Assets.successbox.jpg");

    public static Icon LoadApplicationIcon()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SfmPackageBuilder.WinForms.Assets.app-icon.ico")
            ?? throw new InvalidOperationException("Embedded icon resource was not found.");
        return new Icon(stream);
    }

    private static Image Load(string resourceName)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("Embedded image resource was not found: " + resourceName);
        using var image = Image.FromStream(stream);
        return new Bitmap(image);
    }
}
