using System.Security.Cryptography;
using System.Text;

namespace SfmPackageBuilder.Core.Readme;

public sealed class ReadmeFactsFingerprint
{
    public string Create(string generatedReadmeText)
    {
        ArgumentNullException.ThrowIfNull(generatedReadmeText);
        var bytes = Encoding.UTF8.GetBytes(generatedReadmeText);
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }
}
