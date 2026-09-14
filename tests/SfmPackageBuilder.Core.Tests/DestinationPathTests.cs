using Microsoft.VisualStudio.TestTools.UnitTesting;
using SfmPackageBuilder.Core.Paths;

namespace SfmPackageBuilder.Core.Tests;

[TestClass]
public sealed class DestinationPathTests
{
    private readonly PathValidation validation = new();

    [TestMethod]
    public void ValidNarrowDestinationOverrideIsAccepted()
    {
        var result = validation.ValidatePackageRelativePath(@"materials\models\Creator\props\chair");

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(@"materials\models\Creator\props\chair", result.NormalizedPath);
    }

    [TestMethod]
    [DataRow("..")]
    [DataRow(@".\..")]
    [DataRow(@"..\outside")]
    [DataRow(@"materials\..\outside")]
    public void TraversalOverrideIsRejected(string overridePath)
    {
        var result = validation.ValidatePackageRelativePath(overridePath);

        Assert.AreEqual(PathValidationStatus.Traversal, result.Status);
        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    [DataRow(@"C:\something")]
    [DataRow(@"D:relative-but-drive-qualified")]
    public void DrivePathOverrideIsRejected(string overridePath)
    {
        var result = validation.ValidatePackageRelativePath(overridePath);

        Assert.AreEqual(PathValidationStatus.RootedPath, result.Status);
        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    [DataRow(@"\\server\share")]
    [DataRow(@"\\?\C:\something")]
    [DataRow(@"\rooted")]
    public void UncOrRootedOverrideIsRejected(string overridePath)
    {
        var result = validation.ValidatePackageRelativePath(overridePath);

        Assert.AreEqual(PathValidationStatus.RootedPath, result.Status);
        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public void DotSegmentsAreNormalizedWithoutEscaping()
    {
        var result = validation.ValidatePackageRelativePath(@".\Misc\.\docs");

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(@"Misc\docs", result.NormalizedPath);
    }

    [TestMethod]
    public void DestinationPathTryCreateReturnsNormalizedValue()
    {
        var created = DestinationPath.TryCreate(@"Misc/docs", out var destinationPath, out var result);

        Assert.IsTrue(created);
        Assert.IsTrue(result.IsValid);
        Assert.IsNotNull(destinationPath);
        Assert.AreEqual(@"Misc\docs", destinationPath!.RelativePath);
    }
}
