using Microsoft.VisualStudio.TestTools.UnitTesting;
using SfmPackageBuilder.Core.Planning;
using SfmPackageBuilder.Core.Readme;

namespace SfmPackageBuilder.Core.Tests;

[TestClass]
public sealed class SfmInstallRootResolverTests
{
    [TestMethod]
    public void BasicModelAndMaterialsResolveExactly()
    {
        var result = Resolve(
            Entry(@"models\Creator\chair\chair.mdl"),
            Entry(@"materials\models\Creator\chair\chair.vmt"));

        CollectionAssert.AreEqual(new[] { "models", "materials" }, result.InstallRoots.ToArray());
    }

    [TestMethod]
    public void MapPackageCombinationUsesRegistryOrderAndDeduplicates()
    {
        var result = Resolve(
            Entry(@"maps\test.bsp"),
            Entry(@"materials\maps\test\detail.vmt"),
            Entry(@"models\props\crate.mdl"),
            Entry(@"models\props\crate.vvd"));

        CollectionAssert.AreEqual(new[] { "models", "materials", "maps" }, result.InstallRoots.ToArray());
    }

    [TestMethod]
    public void RigAndConfigurationExtrasResolveFromPackageDestination()
    {
        var result = Resolve(
            Entry(@"scripts\sfm\animset\rig_test.py", PackagePlanEntryType.Extra),
            Entry(@"cfg\example.cfg", PackagePlanEntryType.Extra));

        CollectionAssert.AreEqual(new[] { "scripts", "cfg" }, result.InstallRoots.ToArray());
    }

    [TestMethod]
    public void DocumentationReadmeAndArbitraryCustomContentAreExcluded()
    {
        var result = Resolve(
            Entry(@"Docs\instructions.pdf", PackagePlanEntryType.Extra),
            Entry(@"README.txt", PackagePlanEntryType.Readme),
            Entry(@"SourceFiles\model.blend", PackagePlanEntryType.Extra),
            Entry(@"Credits\credits.txt", PackagePlanEntryType.Extra));

        Assert.AreEqual(0, result.InstallRoots.Count);
        Assert.IsTrue(result.HasDocsFolder);
        Assert.IsTrue(result.HasOtherSupportingFiles);
    }

    [TestMethod]
    public void CaseInsensitiveRootsDoNotDuplicate()
    {
        var result = Resolve(
            Entry(@"MODELS\creator\a.mdl"),
            Entry(@"Materials\creator\a.vmt"),
            Entry(@"Scripts\sfm\animset\rig.py"),
            Entry(@"scripts\sfm\animset\other.py"));

        CollectionAssert.AreEqual(new[] { "models", "materials", "scripts" }, result.InstallRoots.ToArray());
    }

    [TestMethod]
    public void HundredsOfEntriesUnderOneRootProduceOneInstallRoot()
    {
        var entries = Enumerable.Range(0, 250)
            .Select(index => Entry($@"materials\models\creator\mat_{index}.vtf"))
            .ToArray();

        var result = Resolve(entries);

        CollectionAssert.AreEqual(new[] { "materials" }, result.InstallRoots.ToArray());
    }

    private static SfmInstallRootResolution Resolve(params PackagePlanEntry[] entries) =>
        new SfmInstallRootResolver().Resolve(entries);

    private static PackagePlanEntry Entry(string destination, PackagePlanEntryType type = PackagePlanEntryType.Model) =>
        new(
            Guid.NewGuid().ToString("N"),
            type,
            PackagePlanEntryStatus.Resolved,
            destination,
            @"E:\source\file.dat",
            Guid.NewGuid(),
            null,
            isGenerated: false);
}
