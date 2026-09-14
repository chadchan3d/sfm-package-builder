using System.Buffers.Binary;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SfmPackageBuilder.Core.Mdl;

namespace SfmPackageBuilder.Core.Tests;

[TestClass]
public sealed class MdlV49MetadataReaderTests
{
    [TestMethod]
    public void ReadsValidIdentityPositiveChecksumAndDeclaredLengthCrossCheck()
    {
        var bytes = new SyntheticMdl()
            .WithChecksum(123456)
            .WithName("models\\Creator\\chair.mdl")
            .WithDeclaredLength(42)
            .ToArray();

        var result = Read(bytes);

        Assert.AreEqual(MdlMetadataReadStatus.Available, result.Status);
        Assert.AreEqual("IDST", result.Metadata!.Magic);
        Assert.AreEqual(49, result.Metadata.Version);
        Assert.AreEqual(123456, result.Metadata.Checksum);
        Assert.AreEqual("models\\Creator\\chair.mdl", result.Metadata.RawHeaderModelName);
        Assert.AreEqual(42, result.Metadata.DeclaredLength);
    }

    [TestMethod]
    public void ReadsNegativeSignedChecksum()
    {
        var result = Read(new SyntheticMdl().WithChecksum(-612190634).ToArray());

        Assert.AreEqual(MdlMetadataReadStatus.Available, result.Status);
        Assert.AreEqual(-612190634, result.Metadata!.Checksum);
    }

    [TestMethod]
    public void WrongMagicAndUnsupportedVersionReturnUnsupportedResults()
    {
        var wrongMagic = Read(new SyntheticMdl().WithMagic("NOPE").ToArray());
        var unsupportedVersion = Read(new SyntheticMdl().WithVersion(48).ToArray());

        Assert.AreEqual(MdlMetadataReadStatus.UnsupportedFormatOrVersion, wrongMagic.Status);
        Assert.AreEqual("NOPE", wrongMagic.Metadata!.Magic);
        Assert.AreEqual(MdlMetadataReadStatus.UnsupportedFormatOrVersion, unsupportedVersion.Status);
        Assert.AreEqual(48, unsupportedVersion.Metadata!.Version);
    }

    [TestMethod]
    public void FullSixtyFourByteHeaderNameIsReadWithoutScanningBeyondField()
    {
        var name = new string('a', 64);

        var result = Read(new SyntheticMdl().WithRawNameBytes(Enumerable.Repeat((byte)'a', 64).ToArray()).ToArray());

        Assert.AreEqual(MdlMetadataReadStatus.Available, result.Status);
        Assert.AreEqual(name, result.Metadata!.RawHeaderModelName);
        Assert.IsTrue(result.Diagnostics.Any(diagnostic => diagnostic.Code == "fixed-string-not-nul-terminated"));
    }

    [TestMethod]
    public void NonAsciiHeaderNameProducesPartialUnavailableNameWithoutReplacementText()
    {
        var result = Read(new SyntheticMdl().WithRawNameBytes(new byte[] { (byte)'a', 0xFF, 0 }).WithTextures("body").ToArray());

        Assert.AreEqual(MdlMetadataReadStatus.PartialMetadata, result.Status);
        Assert.IsNull(result.Metadata!.RawHeaderModelName);
        CollectionAssert.AreEqual(new[] { "body" }, result.Metadata.TextureReferences.ToArray());
        Assert.IsTrue(result.Diagnostics.Any(diagnostic => diagnostic.Code == "fixed-string-non-ascii"));
    }

    [TestMethod]
    public void ReadsZeroOneAndSeveralTextureReferencesWithSixtyFourByteStride()
    {
        var zero = Read(new SyntheticMdl().ToArray());
        var one = Read(new SyntheticMdl().WithTextures("body").ToArray());
        var several = Read(new SyntheticMdl().WithTextures("body", "eyeball_r", "tail").ToArray());

        Assert.AreEqual(0, zero.Metadata!.TextureReferences.Count);
        CollectionAssert.AreEqual(new[] { "body" }, one.Metadata!.TextureReferences.ToArray());
        CollectionAssert.AreEqual(new[] { "body", "eyeball_r", "tail" }, several.Metadata!.TextureReferences.ToArray());
    }

    [TestMethod]
    public void MalformedTextureTablesProducePartialResults()
    {
        var outOfRangeTable = Read(new SyntheticMdl().WithTextureTable(count: 1, index: 4090).WithCdTextures("models\\chair\\").ToArray());
        var outOfRangeString = Read(new SyntheticMdl().WithTextureEntryRelativeNameOffset(0, 10_000).WithTextureCount(1).WithCdTextures("models\\chair\\").ToArray());
        var negativeCount = Read(new SyntheticMdl().WithTextureTable(count: -1, index: 256).WithCdTextures("models\\chair\\").ToArray());
        var excessiveCount = Read(new SyntheticMdl().WithTextureTable(count: MdlV49MetadataReader.MaximumReasonableCount + 1, index: 256).WithCdTextures("models\\chair\\").ToArray());
        var unterminated = Read(new SyntheticMdl().WithTextureStringWithoutTerminator("unterminated").WithCdTextures("models\\chair\\").ToArray());

        AssertPartialWithCode(outOfRangeTable, "texture-table-out-of-range");
        AssertPartialWithCode(outOfRangeString, "texture-name-0-offset-out-of-range");
        AssertPartialWithCode(negativeCount, "texture-table-count-negative");
        AssertPartialWithCode(excessiveCount, "texture-table-count-excessive");
        AssertPartialWithCode(unterminated, "texture-name-0-unterminated");
    }

    [TestMethod]
    public void ReadsZeroOneAndMultipleMaterialSearchPaths()
    {
        var zero = Read(new SyntheticMdl().ToArray());
        var one = Read(new SyntheticMdl().WithCdTextures("models\\one\\").ToArray());
        var multiple = Read(new SyntheticMdl().WithCdTextures("models\\one\\", "models\\two\\").ToArray());

        Assert.AreEqual(0, zero.Metadata!.MaterialSearchPaths.Count);
        CollectionAssert.AreEqual(new[] { "models\\one\\" }, one.Metadata!.MaterialSearchPaths.ToArray());
        CollectionAssert.AreEqual(new[] { "models\\one\\", "models\\two\\" }, multiple.Metadata!.MaterialSearchPaths.ToArray());
    }

    [TestMethod]
    public void MalformedMaterialSearchPathTableInvalidatesOnlyThatTable()
    {
        var invalidOffset = Read(new SyntheticMdl().WithTextures("body").WithCdTextureOffsets(10_000).ToArray());
        var unterminated = Read(new SyntheticMdl().WithTextures("body").WithCdTextureStringWithoutTerminator("models\\bad").ToArray());

        AssertPartialWithCode(invalidOffset, "cdtexture-path-0-offset-out-of-range");
        CollectionAssert.AreEqual(new[] { "body" }, invalidOffset.Metadata!.TextureReferences.ToArray());
        AssertPartialWithCode(unterminated, "cdtexture-path-0-unterminated");
        CollectionAssert.AreEqual(new[] { "body" }, unterminated.Metadata!.TextureReferences.ToArray());
    }

    [TestMethod]
    public void ReadsSkinMatrixIncludingFamilyMajorAlternateRemaps()
    {
        var result = Read(new SyntheticMdl()
            .WithTextures("base", "red", "blue")
            .WithSkinTable(new short[][]
            {
                new short[] { 0, 1 },
                new short[] { 2, 1 },
                new short[] { 1, 2 }
            })
            .ToArray());

        Assert.AreEqual(MdlMetadataReadStatus.Available, result.Status);
        Assert.AreEqual(2, result.Metadata!.SkinTable!.MaterialSlotCount);
        Assert.AreEqual(3, result.Metadata.SkinTable.SkinFamilyCount);
        CollectionAssert.AreEqual(new short[] { 0, 1 }, result.Metadata.SkinTable.RemapMatrix[0].ToArray());
        CollectionAssert.AreEqual(new short[] { 2, 1 }, result.Metadata.SkinTable.RemapMatrix[1].ToArray());
        CollectionAssert.AreEqual(new short[] { 1, 2 }, result.Metadata.SkinTable.RemapMatrix[2].ToArray());
    }

    [TestMethod]
    public void ZeroSkinFamiliesAndSlotsAreValid()
    {
        var result = Read(new SyntheticMdl().WithSkinDimensions(0, 0, 0).ToArray());

        Assert.AreEqual(MdlMetadataReadStatus.Available, result.Status);
        Assert.AreEqual(0, result.Metadata!.SkinTable!.MaterialSlotCount);
        Assert.AreEqual(0, result.Metadata.SkinTable.SkinFamilyCount);
        Assert.AreEqual(0, result.Metadata.SkinTable.RemapMatrix.Count);
    }

    [TestMethod]
    public void MalformedSkinTablesAreReportedWithoutIndexingBlindly()
    {
        var invalidBounds = Read(new SyntheticMdl().WithTextures("body").WithSkinDimensions(2, 2, 4090).ToArray());
        var multiplicationOverflow = Read(new SyntheticMdl().WithTextures("body").WithSkinDimensions(100_000, 100_000, 512).ToArray());
        var outOfRangeTextureIndex = Read(new SyntheticMdl().WithTextures("body").WithSkinTable(new[] { new short[] { 2 } }).ToArray());

        AssertPartialWithCode(invalidBounds, "skin-table-out-of-range");
        AssertPartialWithCode(multiplicationOverflow, "skin-count-overflow");
        AssertPartialWithCode(outOfRangeTextureIndex, "skin-remap-texture-index-out-of-range");
    }

    [TestMethod]
    public void PartialCorruptionRetainsIndependentValidFacts()
    {
        var result = Read(new SyntheticMdl()
            .WithName("models\\chair.mdl")
            .WithCdTextures("models\\props\\chair\\")
            .WithTextureTable(count: 1, index: 4090)
            .ToArray());

        Assert.AreEqual(MdlMetadataReadStatus.PartialMetadata, result.Status);
        Assert.AreEqual("models\\chair.mdl", result.Metadata!.RawHeaderModelName);
        CollectionAssert.AreEqual(new[] { "models\\props\\chair\\" }, result.Metadata.MaterialSearchPaths.ToArray());
        Assert.AreEqual(0, result.Metadata.TextureReferences.Count);
    }

    private static MdlMetadataReadResult Read(byte[] bytes) =>
        new MdlV49MetadataReader().Read(bytes);

    private static void AssertPartialWithCode(MdlMetadataReadResult result, string code)
    {
        Assert.AreEqual(MdlMetadataReadStatus.PartialMetadata, result.Status);
        Assert.IsTrue(result.Diagnostics.Any(diagnostic => diagnostic.Code == code), code);
        Assert.IsNotNull(result.Metadata);
    }

    private sealed class SyntheticMdl
    {
        private readonly byte[] bytes = new byte[4096];
        private int cursor = 512;

        public SyntheticMdl()
        {
            WithMagic("IDST");
            WithVersion(49);
            WithChecksum(1);
            WithName("model.mdl");
            WithDeclaredLength(bytes.Length);
            WithTextureTable(0, 0);
            WithCdTextureTable(0, 0);
            WithSkinDimensions(0, 0, 0);
        }

        public SyntheticMdl WithMagic(string magic)
        {
            Encoding.ASCII.GetBytes(magic, bytes.AsSpan(0, 4));
            return this;
        }

        public SyntheticMdl WithVersion(int version)
        {
            WriteInt32(4, version);
            return this;
        }

        public SyntheticMdl WithChecksum(int checksum)
        {
            WriteInt32(8, checksum);
            return this;
        }

        public SyntheticMdl WithName(string name)
        {
            var raw = Encoding.ASCII.GetBytes(name);
            Array.Clear(bytes, 12, 64);
            raw.CopyTo(bytes.AsSpan(12));
            return this;
        }

        public SyntheticMdl WithRawNameBytes(byte[] raw)
        {
            Array.Clear(bytes, 12, 64);
            raw.AsSpan(0, Math.Min(raw.Length, 64)).CopyTo(bytes.AsSpan(12));
            return this;
        }

        public SyntheticMdl WithDeclaredLength(int declaredLength)
        {
            WriteInt32(76, declaredLength);
            return this;
        }

        public SyntheticMdl WithTextures(params string[] names)
        {
            var tableOffset = 256;
            WriteInt32(204, names.Length);
            WriteInt32(208, tableOffset);
            cursor = Math.Max(cursor, tableOffset + names.Length * 64);
            for (var i = 0; i < names.Length; i++)
            {
                var entryStart = tableOffset + i * 64;
                var stringOffset = WriteString(names[i]);
                WriteInt32(entryStart, stringOffset - entryStart);
            }

            return this;
        }

        public SyntheticMdl WithTextureCount(int count)
        {
            WriteInt32(204, count);
            WriteInt32(208, 256);
            return this;
        }

        public SyntheticMdl WithTextureTable(int count, int index)
        {
            WriteInt32(204, count);
            WriteInt32(208, index);
            return this;
        }

        public SyntheticMdl WithTextureEntryRelativeNameOffset(int entryIndex, int relativeOffset)
        {
            WriteInt32(256 + entryIndex * 64, relativeOffset);
            return this;
        }

        public SyntheticMdl WithTextureStringWithoutTerminator(string value)
        {
            WriteInt32(204, 1);
            WriteInt32(208, 256);
            var stringOffset = bytes.Length - value.Length;
            Encoding.ASCII.GetBytes(value, bytes.AsSpan(stringOffset, value.Length));
            WriteInt32(256, stringOffset - 256);
            return this;
        }

        public SyntheticMdl WithCdTextures(params string[] paths)
        {
            var tableOffset = Align(cursor, 4);
            WriteInt32(212, paths.Length);
            WriteInt32(216, tableOffset);
            cursor = tableOffset + paths.Length * 4;
            var offsets = new int[paths.Length];
            for (var i = 0; i < paths.Length; i++)
            {
                offsets[i] = WriteString(paths[i]);
            }

            for (var i = 0; i < offsets.Length; i++)
            {
                WriteInt32(tableOffset + i * 4, offsets[i]);
            }

            return this;
        }

        public SyntheticMdl WithCdTextureOffsets(params int[] offsets)
        {
            var tableOffset = Align(cursor, 4);
            return WithCdTextureTable(offsets.Length, tableOffset, offsets);
        }

        public SyntheticMdl WithCdTextureTable(int count, int index, params int[] offsets)
        {
            WriteInt32(212, count);
            WriteInt32(216, index);
            for (var i = 0; i < offsets.Length; i++)
            {
                WriteInt32(index + i * 4, offsets[i]);
            }

            cursor = Math.Max(cursor, index + offsets.Length * 4);
            return this;
        }

        public SyntheticMdl WithCdTextureStringWithoutTerminator(string value)
        {
            var stringOffset = bytes.Length - value.Length;
            Encoding.ASCII.GetBytes(value, bytes.AsSpan(stringOffset, value.Length));
            return WithCdTextureOffsets(stringOffset);
        }

        public SyntheticMdl WithSkinTable(short[][] remapMatrix)
        {
            var familyCount = remapMatrix.Length;
            var slotCount = familyCount == 0 ? 0 : remapMatrix[0].Length;
            var tableOffset = Align(cursor, 2);
            WithSkinDimensions(slotCount, familyCount, tableOffset);
            cursor = tableOffset + slotCount * familyCount * 2;
            for (var family = 0; family < familyCount; family++)
            {
                for (var slot = 0; slot < slotCount; slot++)
                {
                    BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(tableOffset + 2 * (family * slotCount + slot), 2), remapMatrix[family][slot]);
                }
            }

            return this;
        }

        public SyntheticMdl WithSkinDimensions(int slotCount, int familyCount, int index)
        {
            WriteInt32(220, slotCount);
            WriteInt32(224, familyCount);
            WriteInt32(228, index);
            return this;
        }

        public byte[] ToArray() => bytes.ToArray();

        private int WriteString(string value)
        {
            var offset = cursor;
            var encoded = Encoding.ASCII.GetBytes(value);
            encoded.CopyTo(bytes.AsSpan(offset));
            bytes[offset + encoded.Length] = 0;
            cursor += encoded.Length + 1;
            return offset;
        }

        private void WriteInt32(int offset, int value) =>
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset, 4), value);

        private static int Align(int value, int alignment) =>
            (value + alignment - 1) / alignment * alignment;
    }
}
