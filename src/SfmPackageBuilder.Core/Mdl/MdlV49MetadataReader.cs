using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using SfmPackageBuilder.Core.FileSystem;

namespace SfmPackageBuilder.Core.Mdl;

public sealed class MdlV49MetadataReader : IMdlMetadataReader
{
    public const int SupportedVersion = 49;

    // Implementation guard only. The handoff explicitly says this is not a Source-format limit.
    public const int MaximumReasonableCount = 100_000;

    private const string SupportedMagic = "IDST";
    private const int HeaderMinimumLength = 232;
    private const int NameOffset = 12;
    private const int NameLength = 64;
    private const int TextureRecordStride = 64;
    private readonly IFileSystem fileSystem;

    public MdlV49MetadataReader(IFileSystem? fileSystem = null)
    {
        this.fileSystem = fileSystem ?? new PhysicalFileSystem();
    }

    public MdlMetadataReadResult Read(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        using var stream = fileSystem.OpenRead(path);
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return Read(memory.ToArray());
    }

    public MdlMetadataReadResult Read(ReadOnlySpan<byte> bytes)
    {
        var diagnostics = new List<MdlMetadataDiagnostic>();
        if (bytes.Length < 8)
        {
            diagnostics.Add(Malformed(MdlMetadataSection.Header, "mdl-header-too-short", "The MDL header is too short to contain magic and version."));
            return new MdlMetadataReadResult(MdlMetadataReadStatus.MalformedMetadata, null, diagnostics);
        }

        var magic = Encoding.ASCII.GetString(bytes.Slice(0, 4));
        var version = ReadInt32(bytes, 4);
        var checksum = bytes.Length >= 12 ? ReadInt32(bytes, 8) : 0;
        var declaredLength = bytes.Length >= 80 ? ReadInt32(bytes, 76) : 0;

        if (!string.Equals(magic, SupportedMagic, StringComparison.Ordinal))
        {
            diagnostics.Add(Malformed(MdlMetadataSection.Header, "unsupported-mdl-magic", "The file does not use the IDST MDL magic."));
            return new MdlMetadataReadResult(
                MdlMetadataReadStatus.UnsupportedFormatOrVersion,
                new MdlV49Metadata(magic, version, checksum, null, declaredLength, Array.Empty<string>(), Array.Empty<string>(), null),
                diagnostics);
        }

        if (version != SupportedVersion)
        {
            diagnostics.Add(Malformed(MdlMetadataSection.Header, "unsupported-mdl-version", "The MDL version is not the supported v49 format."));
            return new MdlMetadataReadResult(
                MdlMetadataReadStatus.UnsupportedFormatOrVersion,
                new MdlV49Metadata(magic, version, checksum, null, declaredLength, Array.Empty<string>(), Array.Empty<string>(), null),
                diagnostics);
        }

        if (bytes.Length < HeaderMinimumLength)
        {
            diagnostics.Add(Malformed(MdlMetadataSection.Header, "mdl-v49-header-too-short", "The MDL v49 header is too short to contain the required metadata offsets."));
            return new MdlMetadataReadResult(MdlMetadataReadStatus.MalformedMetadata, null, diagnostics);
        }

        var rawName = TryReadFixedAsciiString(bytes.Slice(NameOffset, NameLength), MdlMetadataSection.RawHeaderModelName, diagnostics);
        var textureReferences = ReadTextureReferences(bytes, diagnostics);
        var materialSearchPaths = ReadMaterialSearchPaths(bytes, diagnostics);
        var skinTable = ReadSkinTable(bytes, textureReferences, diagnostics);

        var metadata = new MdlV49Metadata(
            magic,
            version,
            checksum,
            rawName,
            declaredLength,
            textureReferences ?? Array.Empty<string>(),
            materialSearchPaths ?? Array.Empty<string>(),
            skinTable);

        var status = DetermineStatus(metadata, diagnostics);
        return new MdlMetadataReadResult(status, metadata, diagnostics);
    }

    public string ComputeSha256(ReadOnlySpan<byte> bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static IReadOnlyList<string>? ReadTextureReferences(ReadOnlySpan<byte> bytes, List<MdlMetadataDiagnostic> diagnostics)
    {
        var count = ReadInt32(bytes, 204);
        var index = ReadInt32(bytes, 208);
        if (!ValidateTable(bytes.Length, count, index, TextureRecordStride, MdlMetadataSection.TextureReferences, "texture-table", diagnostics))
        {
            return null;
        }

        var textures = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            var entryStart = checked(index + i * TextureRecordStride);
            var relativeNameOffset = ReadInt32(bytes, entryStart);
            if (!TryAdd(entryStart, relativeNameOffset, out var nameOffset)
                || !TryReadAsciiZ(bytes, nameOffset, MdlMetadataSection.TextureReferences, $"texture-name-{i}", diagnostics, out var name))
            {
                return null;
            }

            textures.Add(name);
        }

        return textures;
    }

    private static IReadOnlyList<string>? ReadMaterialSearchPaths(ReadOnlySpan<byte> bytes, List<MdlMetadataDiagnostic> diagnostics)
    {
        var count = ReadInt32(bytes, 212);
        var index = ReadInt32(bytes, 216);
        if (!ValidateTable(bytes.Length, count, index, sizeof(int), MdlMetadataSection.MaterialSearchPaths, "cdtexture-table", diagnostics))
        {
            return null;
        }

        var paths = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            var offset = ReadInt32(bytes, checked(index + i * sizeof(int)));
            if (!TryReadAsciiZ(bytes, offset, MdlMetadataSection.MaterialSearchPaths, $"cdtexture-path-{i}", diagnostics, out var path))
            {
                return null;
            }

            paths.Add(path);
        }

        return paths;
    }

    private static MdlSkinTable? ReadSkinTable(
        ReadOnlySpan<byte> bytes,
        IReadOnlyList<string>? textureReferences,
        List<MdlMetadataDiagnostic> diagnostics)
    {
        var slotCount = ReadInt32(bytes, 220);
        var familyCount = ReadInt32(bytes, 224);
        var index = ReadInt32(bytes, 228);
        if (slotCount < 0 || familyCount < 0)
        {
            diagnostics.Add(Malformed(MdlMetadataSection.SkinTable, "skin-count-negative", "The skin table contains a negative slot or family count."));
            return null;
        }

        if (slotCount > MaximumReasonableCount || familyCount > MaximumReasonableCount)
        {
            diagnostics.Add(Malformed(MdlMetadataSection.SkinTable, "skin-count-excessive", "The skin table count exceeds the implementation sanity ceiling."));
            return null;
        }

        if (!TryMultiply(slotCount, familyCount, out var entryCount))
        {
            diagnostics.Add(Malformed(MdlMetadataSection.SkinTable, "skin-count-overflow", "The skin table dimensions overflow."));
            return null;
        }

        if (!ValidateTable(bytes.Length, entryCount, index, sizeof(short), MdlMetadataSection.SkinTable, "skin-table", diagnostics))
        {
            return null;
        }

        var rows = new List<IReadOnlyList<short>>(familyCount);
        for (var family = 0; family < familyCount; family++)
        {
            var row = new short[slotCount];
            for (var slot = 0; slot < slotCount; slot++)
            {
                var flatIndex = checked(family * slotCount + slot);
                var value = ReadInt16(bytes, checked(index + flatIndex * sizeof(short)));
                if (value < 0 || textureReferences is not null && value >= textureReferences.Count)
                {
                    diagnostics.Add(Malformed(MdlMetadataSection.SkinTable, "skin-remap-texture-index-out-of-range", "A skin remap points outside the texture reference table."));
                    return null;
                }

                row[slot] = value;
            }

            rows.Add(row);
        }

        return new MdlSkinTable(slotCount, familyCount, rows);
    }

    private static bool ValidateTable(
        int fileLength,
        int count,
        int index,
        int stride,
        MdlMetadataSection section,
        string codePrefix,
        List<MdlMetadataDiagnostic> diagnostics)
    {
        if (count < 0)
        {
            diagnostics.Add(Malformed(section, $"{codePrefix}-count-negative", "A metadata table contains a negative count."));
            return false;
        }

        if (count > MaximumReasonableCount)
        {
            diagnostics.Add(Malformed(section, $"{codePrefix}-count-excessive", "A metadata table count exceeds the implementation sanity ceiling."));
            return false;
        }

        if (index < 0)
        {
            diagnostics.Add(Malformed(section, $"{codePrefix}-index-negative", "A metadata table has a negative file offset."));
            return false;
        }

        if (count == 0)
        {
            return true;
        }

        if (!TryMultiply(count, stride, out var byteCount) || !TryAdd(index, byteCount, out var end) || end > fileLength)
        {
            diagnostics.Add(Malformed(section, $"{codePrefix}-out-of-range", "A metadata table extends outside the actual MDL file."));
            return false;
        }

        return true;
    }

    private static string? TryReadFixedAsciiString(
        ReadOnlySpan<byte> bytes,
        MdlMetadataSection section,
        List<MdlMetadataDiagnostic> diagnostics)
    {
        var end = bytes.IndexOf((byte)0);
        if (end < 0)
        {
            end = bytes.Length;
            diagnostics.Add(new MdlMetadataDiagnostic(
                section,
                MdlMetadataDiagnosticSeverity.Information,
                "fixed-string-not-nul-terminated",
                "A fixed-width MDL string used the full field with no NUL terminator; the full field was read."));
        }

        return TryDecodeAscii(bytes.Slice(0, end), section, "fixed-string-non-ascii", diagnostics, out var value)
            ? value
            : null;
    }

    private static bool TryReadAsciiZ(
        ReadOnlySpan<byte> bytes,
        int offset,
        MdlMetadataSection section,
        string codePrefix,
        List<MdlMetadataDiagnostic> diagnostics,
        out string value)
    {
        value = string.Empty;
        if (offset < 0 || offset >= bytes.Length)
        {
            diagnostics.Add(Malformed(section, $"{codePrefix}-offset-out-of-range", "A metadata string offset points outside the actual MDL file."));
            return false;
        }

        var slice = bytes.Slice(offset);
        var length = slice.IndexOf((byte)0);
        if (length < 0)
        {
            diagnostics.Add(Malformed(section, $"{codePrefix}-unterminated", "A metadata string is not NUL-terminated within the actual MDL file."));
            return false;
        }

        return TryDecodeAscii(slice.Slice(0, length), section, $"{codePrefix}-non-ascii", diagnostics, out value);
    }

    private static bool TryDecodeAscii(
        ReadOnlySpan<byte> bytes,
        MdlMetadataSection section,
        string code,
        List<MdlMetadataDiagnostic> diagnostics,
        out string value)
    {
        value = string.Empty;
        if (bytes.IndexOfAnyInRange((byte)0x80, byte.MaxValue) >= 0)
        {
            diagnostics.Add(Malformed(section, code, "A metadata string contains bytes outside the supported ASCII-compatible range."));
            return false;
        }

        value = Encoding.ASCII.GetString(bytes);
        return true;
    }

    private static MdlMetadataReadStatus DetermineStatus(
        MdlV49Metadata metadata,
        IReadOnlyList<MdlMetadataDiagnostic> diagnostics)
    {
        if (diagnostics.All(diagnostic => diagnostic.Severity != MdlMetadataDiagnosticSeverity.Malformed))
        {
            return MdlMetadataReadStatus.Available;
        }

        return metadata.RawHeaderModelName is not null
            || metadata.TextureReferences.Count > 0
            || metadata.MaterialSearchPaths.Count > 0
            || metadata.SkinTable is not null
                ? MdlMetadataReadStatus.PartialMetadata
                : MdlMetadataReadStatus.MalformedMetadata;
    }

    private static int ReadInt32(ReadOnlySpan<byte> bytes, int offset) =>
        BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(offset, sizeof(int)));

    private static short ReadInt16(ReadOnlySpan<byte> bytes, int offset) =>
        BinaryPrimitives.ReadInt16LittleEndian(bytes.Slice(offset, sizeof(short)));

    private static bool TryAdd(int left, int right, out int result)
    {
        try
        {
            result = checked(left + right);
            return result >= 0;
        }
        catch (OverflowException)
        {
            result = 0;
            return false;
        }
    }

    private static bool TryMultiply(int left, int right, out int result)
    {
        try
        {
            result = checked(left * right);
            return result >= 0;
        }
        catch (OverflowException)
        {
            result = 0;
            return false;
        }
    }

    private static MdlMetadataDiagnostic Malformed(MdlMetadataSection section, string code, string message) =>
        new(section, MdlMetadataDiagnosticSeverity.Malformed, code, message);
}
