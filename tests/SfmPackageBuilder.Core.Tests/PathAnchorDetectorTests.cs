using Microsoft.VisualStudio.TestTools.UnitTesting;
using SfmPackageBuilder.Core.Paths;

namespace SfmPackageBuilder.Core.Tests;

[TestClass]
public sealed class PathAnchorDetectorTests
{
    private readonly PathAnchorDetector detector = new();

    [TestMethod]
    public void LowercaseModelsAnchorResolves()
    {
        var result = detector.Resolve(@"E:\SFM\game\usermod\models\Creator\props\chair\chair.mdl", SourceAnchorKind.Models);

        Assert.IsTrue(result.IsResolved);
        Assert.AreEqual(@"models\Creator\props\chair\chair.mdl", result.DestinationRelativePath);
    }

    [TestMethod]
    public void MixedCaseModelsAnchorResolves()
    {
        var result = detector.Resolve(@"E:\SFM\game\usermod\Models\Creator\props\chair\chair.mdl", SourceAnchorKind.Models);

        Assert.IsTrue(result.IsResolved);
        Assert.AreEqual(@"Models\Creator\props\chair\chair.mdl", result.DestinationRelativePath);
    }

    [TestMethod]
    public void LowercaseMaterialsAnchorResolves()
    {
        var result = detector.Resolve(@"E:\SFM\game\usermod\materials\models\Creator\chair\body.vtf", SourceAnchorKind.Materials);

        Assert.IsTrue(result.IsResolved);
        Assert.AreEqual(@"materials\models\Creator\chair\body.vtf", result.DestinationRelativePath);
    }

    [TestMethod]
    public void MixedCaseMaterialsAnchorResolves()
    {
        var result = detector.Resolve(@"E:\SFM\game\usermod\MATERIALS\models\Creator\chair\body.vtf", SourceAnchorKind.Materials);

        Assert.IsTrue(result.IsResolved);
        Assert.AreEqual(@"MATERIALS\models\Creator\chair\body.vtf", result.DestinationRelativePath);
    }

    [TestMethod]
    public void SpacesInPathResolveLexically()
    {
        var result = detector.Resolve(@"D:\Steam Library\Source Filmmaker\game\user mod\models\Creator Name\big chair\chair.mdl", SourceAnchorKind.Models);

        Assert.IsTrue(result.IsResolved);
        Assert.AreEqual(@"models\Creator Name\big chair\chair.mdl", result.DestinationRelativePath);
    }

    [TestMethod]
    public void UnicodePathResolvesLexically()
    {
        var result = detector.Resolve(@"Z:\SFM\game\usermod\materials\models\Crëator\椅子\body.vtf", SourceAnchorKind.Materials);

        Assert.IsTrue(result.IsResolved);
        Assert.AreEqual(@"materials\models\Crëator\椅子\body.vtf", result.DestinationRelativePath);
    }

    [TestMethod]
    public void ArbitraryNonCDriveLetterResolves()
    {
        var result = detector.Resolve(@"Q:\SFM\game\usermod\models\Creator\prop.mdl", SourceAnchorKind.Models);

        Assert.IsTrue(result.IsResolved);
        Assert.AreEqual(@"models\Creator\prop.mdl", result.DestinationRelativePath);
    }

    [TestMethod]
    [DataRow(@"E:\SFM\game\usermod\models_backup\Creator\chair.mdl")]
    [DataRow(@"E:\SFM\game\usermod\mymodels\Creator\chair.mdl")]
    [DataRow(@"E:\SFM\game\usermod\modelshop\Creator\chair.mdl")]
    public void MisleadingModelSubstringsDoNotMatchModelsAnchor(string path)
    {
        var result = detector.Resolve(path, SourceAnchorKind.Models);

        Assert.AreEqual(AnchorResolutionStatus.MissingAnchor, result.Status);
        Assert.IsNull(result.DestinationRelativePath);
    }

    [TestMethod]
    [DataRow(@"E:\SFM\game\usermod\models_backup\Creator\body.vtf")]
    [DataRow(@"E:\SFM\game\usermod\old_materials\Creator\body.vtf")]
    [DataRow(@"E:\SFM\game\usermod\usermod_materials_backup\Creator\body.vtf")]
    [DataRow(@"E:\SFM\game\usermod\materials-old\Creator\body.vtf")]
    public void MisleadingMaterialSubstringsDoNotMatchMaterialsAnchor(string path)
    {
        var result = detector.Resolve(path, SourceAnchorKind.Materials);

        Assert.AreEqual(AnchorResolutionStatus.MissingAnchor, result.Status);
        Assert.IsNull(result.DestinationRelativePath);
    }

    [TestMethod]
    public void MultipleModelsSegmentsChooseNearestOccurrence()
    {
        var result = detector.Resolve(@"E:\models\archive\game\usermod\models\Creator\chair.mdl", SourceAnchorKind.Models);

        Assert.IsTrue(result.IsResolved);
        CollectionAssert.AreEqual(new[] { 1, 5 }, result.CandidateSegmentIndexes.ToArray());
        Assert.AreEqual(5, result.SelectedSegmentIndex);
        Assert.AreEqual(@"models\Creator\chair.mdl", result.DestinationRelativePath);
    }

    [TestMethod]
    public void MultipleMaterialsSegmentsChooseNearestOccurrence()
    {
        var result = detector.Resolve(@"E:\materials\backup\game\usermod\materials\models\Creator\body.vtf", SourceAnchorKind.Materials);

        Assert.IsTrue(result.IsResolved);
        CollectionAssert.AreEqual(new[] { 1, 5 }, result.CandidateSegmentIndexes.ToArray());
        Assert.AreEqual(5, result.SelectedSegmentIndex);
        Assert.AreEqual(@"materials\models\Creator\body.vtf", result.DestinationRelativePath);
    }

    [TestMethod]
    public void PathWithNoAnchorIsUnresolved()
    {
        var result = detector.Resolve(@"D:\RandomExports\body.vtf", SourceAnchorKind.Materials);

        Assert.AreEqual(AnchorResolutionStatus.MissingAnchor, result.Status);
        Assert.IsFalse(result.IsResolved);
        Assert.IsNull(result.SelectedSegmentIndex);
        Assert.AreEqual(0, result.CandidateSegmentIndexes.Count);
        Assert.IsNull(result.DestinationRelativePath);
    }

    [TestMethod]
    public void NaturalModelDestinationRetainsModelsRoot()
    {
        var result = detector.Resolve(@"E:\SFM\game\usermod\models\Creator\props\chair\chair.mdl", SourceAnchorKind.Models);

        StringAssert.StartsWith(result.DestinationRelativePath, @"models\");
    }

    [TestMethod]
    public void NaturalMaterialDestinationRetainsMaterialsRoot()
    {
        var result = detector.Resolve(@"E:\SFM\game\usermod\materials\models\Creator\props\chair\body.vtf", SourceAnchorKind.Materials);

        StringAssert.StartsWith(result.DestinationRelativePath, @"materials\");
    }

    [TestMethod]
    public void DeepPathDoesNotHitArtificialMaxPathLimit()
    {
        var deepTail = string.Join('\\', Enumerable.Range(0, 80).Select(index => $"segment{index:D2}"));
        var path = $@"R:\SFM\game\usermod\models\{deepTail}\chair.mdl";

        Assert.IsTrue(path.Length > 260);

        var result = detector.Resolve(path, SourceAnchorKind.Models);

        Assert.IsTrue(result.IsResolved);
        StringAssert.StartsWith(result.DestinationRelativePath, @"models\");
    }
}
