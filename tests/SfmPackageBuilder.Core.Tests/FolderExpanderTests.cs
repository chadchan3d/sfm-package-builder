using Microsoft.VisualStudio.TestTools.UnitTesting;
using SfmPackageBuilder.Core.Domain;
using SfmPackageBuilder.Core.Expansion;

namespace SfmPackageBuilder.Core.Tests;

[TestClass]
public sealed class FolderExpanderTests
{
    [TestMethod]
    public void EmptyFolderExpandsToEmptyInventory()
    {
        var fileSystem = new FakeFileSystem();
        fileSystem.AddDirectory(@"E:\SFM\game\usermod\materials\models\Creator\chair");

        var inventory = Resolve(fileSystem, MaterialFolder());

        Assert.AreEqual(SourceInventoryStatus.Resolved, inventory.Status);
        Assert.AreEqual(0, inventory.Items.Count);
    }

    [TestMethod]
    public void OneFileFolderExpansionPreservesChildName()
    {
        var fileSystem = new FakeFileSystem();
        fileSystem.AddFile(@"E:\SFM\game\usermod\materials\models\Creator\chair\chair.vmt", Array.Empty<byte>());

        var inventory = Resolve(fileSystem, MaterialFolder());

        Assert.AreEqual(SourceInventoryStatus.Resolved, inventory.Status);
        Assert.AreEqual("chair.vmt", inventory.Items[0].RelativePathWithinSelection);
    }

    [TestMethod]
    public void NestedFolderExpansionPreservesRelativeStructure()
    {
        var fileSystem = new FakeFileSystem();
        fileSystem.AddFile(@"E:\SFM\game\usermod\materials\models\Creator\chair\chair.vmt", Array.Empty<byte>());
        fileSystem.AddFile(@"E:\SFM\game\usermod\materials\models\Creator\chair\normals\chair_n.vtf", Array.Empty<byte>());

        var inventory = Resolve(fileSystem, MaterialFolder());

        CollectionAssert.AreEqual(
            new[] { "chair.vmt", @"normals\chair_n.vtf" },
            inventory.Items.Select(item => item.RelativePathWithinSelection).Order().ToArray());
    }

    [TestMethod]
    public void DeeplyNestedFileExpands()
    {
        var fileSystem = new FakeFileSystem();
        fileSystem.AddFile(@"E:\SFM\game\usermod\materials\models\Creator\chair\a\b\c\d\body.vtf", Array.Empty<byte>());

        var inventory = Resolve(fileSystem, MaterialFolder());

        Assert.AreEqual(@"a\b\c\d\body.vtf", inventory.Items[0].RelativePathWithinSelection);
    }

    [TestMethod]
    public void SpacesInFoldersExpand()
    {
        var fileSystem = new FakeFileSystem();
        var source = new SourceEntry
        {
            Kind = SourceEntryKind.Material,
            SourcePath = @"E:\Steam Library\game\user mod\materials\models\Creator Name\big chair",
            IsFolder = true,
            IncludeRecursively = true
        };
        fileSystem.AddFile(@"E:\Steam Library\game\user mod\materials\models\Creator Name\big chair\chair color.vtf", Array.Empty<byte>());

        var inventory = Resolve(fileSystem, source);

        Assert.AreEqual("chair color.vtf", inventory.Items[0].RelativePathWithinSelection);
    }

    [TestMethod]
    public void UnicodeFoldersAndFilenamesExpand()
    {
        var fileSystem = new FakeFileSystem();
        var source = new SourceEntry
        {
            Kind = SourceEntryKind.Material,
            SourcePath = @"Z:\SFM\materials\models\Crëator\椅子",
            IsFolder = true,
            IncludeRecursively = true
        };
        fileSystem.AddFile(@"Z:\SFM\materials\models\Crëator\椅子\材質.vtf", Array.Empty<byte>());

        var inventory = Resolve(fileSystem, source);

        Assert.AreEqual("材質.vtf", inventory.Items[0].RelativePathWithinSelection);
    }

    [TestMethod]
    public void MaterialFolderExpansionPreservesOriginKind()
    {
        var fileSystem = new FakeFileSystem();
        fileSystem.AddFile(@"E:\SFM\game\usermod\materials\models\Creator\chair\chair.vmt", Array.Empty<byte>());

        var inventory = Resolve(fileSystem, MaterialFolder());

        Assert.AreEqual(SourceEntryKind.Material, inventory.Items[0].Origin.Kind);
    }

    [TestMethod]
    public void ExtraFolderExpansionPreservesOriginKind()
    {
        var fileSystem = new FakeFileSystem();
        var source = new SourceEntry
        {
            Kind = SourceEntryKind.Extra,
            SourcePath = @"E:\ReleaseDocs\screenshots",
            IsFolder = true,
            IncludeRecursively = true
        };
        fileSystem.AddFile(@"E:\ReleaseDocs\screenshots\preview.png", Array.Empty<byte>());

        var inventory = Resolve(fileSystem, source);

        Assert.AreEqual(SourceEntryKind.Extra, inventory.Items[0].Origin.Kind);
        Assert.AreEqual("preview.png", inventory.Items[0].RelativePathWithinSelection);
    }

    [TestMethod]
    public void OverlappingSelectionsRemainIndependentlyRepresented()
    {
        var fileSystem = new FakeFileSystem();
        fileSystem.AddFile(@"E:\SFM\materials\models\Creator\props\chair.vmt", Array.Empty<byte>());
        var project = new PackageProject
        {
            MaterialSources = new List<SourceEntry>
            {
                new()
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Kind = SourceEntryKind.Material,
                    SourcePath = @"E:\SFM\materials\models\Creator",
                    IsFolder = true,
                    IncludeRecursively = true
                },
                new()
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Kind = SourceEntryKind.Material,
                    SourcePath = @"E:\SFM\materials\models\Creator\props",
                    IsFolder = true,
                    IncludeRecursively = true
                }
            }
        };

        var result = new SourceExpansionService(fileSystem).Resolve(project);

        Assert.AreEqual(2, result.MaterialInventories.Count);
        Assert.AreEqual(@"props\chair.vmt", result.MaterialInventories[0].Items[0].RelativePathWithinSelection);
        Assert.AreEqual("chair.vmt", result.MaterialInventories[1].Items[0].RelativePathWithinSelection);
    }

    [TestMethod]
    public void DuplicateSourceThroughDifferentEntriesIsNotCollapsed()
    {
        var fileSystem = new FakeFileSystem();
        fileSystem.AddFile(@"E:\SFM\materials\models\Creator\chair\chair.vmt", Array.Empty<byte>());
        var project = new PackageProject
        {
            MaterialSources = new List<SourceEntry>
            {
                MaterialFolder(
                    Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    @"E:\SFM\materials\models\Creator\chair"),
                MaterialFolder(
                    Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    @"E:\SFM\materials\models\Creator\chair")
            }
        };

        var result = new SourceExpansionService(fileSystem).Resolve(project);

        Assert.AreEqual(2, result.MaterialInventories.Count);
        Assert.AreEqual(result.MaterialInventories[0].Items[0].SourcePath, result.MaterialInventories[1].Items[0].SourcePath);
        Assert.AreNotEqual(result.MaterialInventories[0].Origin.Id, result.MaterialInventories[1].Origin.Id);
    }

    [TestMethod]
    public void MissingSelectedFolderProducesStructuredOutcome()
    {
        var inventory = Resolve(new FakeFileSystem(), MaterialFolder());

        Assert.AreEqual(SourceInventoryStatus.MissingSelectedSource, inventory.Status);
        Assert.AreEqual(0, inventory.Items.Count);
    }

    [TestMethod]
    public void MissingSelectedIndividualFileProducesStructuredOutcome()
    {
        var source = new SourceEntry
        {
            Kind = SourceEntryKind.Extra,
            SourcePath = @"E:\ReleaseDocs\LICENSE.txt",
            IsFolder = false
        };

        var inventory = Resolve(new FakeFileSystem(), source);

        Assert.AreEqual(SourceInventoryStatus.MissingSelectedSource, inventory.Status);
        Assert.AreEqual(SourceObservationStatus.Missing, inventory.Items[0].ObservationStatus);
    }

    [TestMethod]
    public void SimulatedEnumerationFailureProducesStructuredOutcome()
    {
        var fileSystem = new FakeFileSystem();
        fileSystem.AddDirectory(@"E:\SFM\game\usermod\materials\models\Creator\chair");
        fileSystem.FailEnumeration(@"E:\SFM\game\usermod\materials\models\Creator\chair");

        var inventory = Resolve(fileSystem, MaterialFolder());

        Assert.AreEqual(SourceInventoryStatus.EnumerationFailed, inventory.Status);
        Assert.IsNotNull(inventory.FailureDetail);
    }

    [TestMethod]
    public void SimulatedReadFailureProducesStructuredOutcome()
    {
        var fileSystem = new FakeFileSystem();
        fileSystem.AddFile(@"E:\ReleaseDocs\LICENSE.txt", Array.Empty<byte>());
        fileSystem.FailRead(@"E:\ReleaseDocs\LICENSE.txt");
        var source = new SourceEntry
        {
            Kind = SourceEntryKind.Extra,
            SourcePath = @"E:\ReleaseDocs\LICENSE.txt",
            IsFolder = false
        };

        var inventory = Resolve(fileSystem, source);

        Assert.AreEqual(SourceInventoryStatus.ReadFailed, inventory.Status);
        Assert.AreEqual(SourceObservationStatus.ReadFailed, inventory.Items[0].ObservationStatus);
    }

    private static SourceInventory Resolve(FakeFileSystem fileSystem, SourceEntry sourceEntry)
    {
        return new FolderExpander(fileSystem).Resolve(sourceEntry);
    }

    private static SourceEntry MaterialFolder(Guid? id = null, string? sourcePath = null)
    {
        return new SourceEntry
        {
            Id = id ?? Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Kind = SourceEntryKind.Material,
            SourcePath = sourcePath ?? @"E:\SFM\game\usermod\materials\models\Creator\chair",
            IsFolder = true,
            IncludeRecursively = true
        };
    }
}
