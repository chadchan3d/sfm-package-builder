using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SfmPackageBuilder.Core.Mdl;

namespace SfmPackageBuilder.IntegrationTests;

[TestClass]
public sealed class MdlGoldenOracleIntegrationTests
{
    [TestMethod]
    public void LocalCorpusMdlsMatchGoldenOracleWhenAvailable()
    {
        var goldenDirectory = FindDirectory("work", "package_builder_handoff_bundle", "handoff", "golden")
            ?? FindDirectory("handoff", "golden");
        if (goldenDirectory is null)
        {
            Assert.Inconclusive("Golden oracle directory is not available in this workspace.");
        }

        var bundleRoot = Directory.GetParent(goldenDirectory)!.Parent!.FullName;
        var reader = new MdlV49MetadataReader();
        var compared = 0;
        foreach (var goldenPath in Directory.EnumerateFiles(goldenDirectory, "*.json").Order(StringComparer.OrdinalIgnoreCase))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(goldenPath));
            var root = document.RootElement;
            var sourceRelativePath = root.GetProperty("source_mdl_relative_path").GetString()!;
            var mdlPath = Path.Combine(bundleRoot, sourceRelativePath);
            if (!File.Exists(mdlPath))
            {
                continue;
            }

            var bytes = File.ReadAllBytes(mdlPath);
            var result = reader.Read(bytes);

            Assert.AreEqual(MdlMetadataReadStatus.Available, result.Status, goldenPath);
            Assert.AreEqual(root.GetProperty("mdl_sha256").GetString(), reader.ComputeSha256(bytes), goldenPath);
            Assert.AreEqual(root.GetProperty("mdl_version").GetInt32(), result.Metadata!.Version, goldenPath);
            Assert.AreEqual(root.GetProperty("mdl_checksum").GetInt32(), result.Metadata.Checksum, goldenPath);
            Assert.AreEqual(root.GetProperty("internal_model_name").GetString(), result.Metadata.RawHeaderModelName, goldenPath);
            CollectionAssert.AreEqual(ReadStringArray(root.GetProperty("material_search_paths")), result.Metadata.MaterialSearchPaths.ToArray(), goldenPath);
            CollectionAssert.AreEqual(ReadStringArray(root.GetProperty("texture_references")), result.Metadata.TextureReferences.ToArray(), goldenPath);

            var goldenSkin = root.GetProperty("skin_table");
            Assert.AreEqual(goldenSkin.GetProperty("material_slot_count").GetInt32(), result.Metadata.SkinTable!.MaterialSlotCount, goldenPath);
            Assert.AreEqual(goldenSkin.GetProperty("skin_family_count").GetInt32(), result.Metadata.SkinTable.SkinFamilyCount, goldenPath);
            var expectedMatrix = ReadShortMatrix(goldenSkin.GetProperty("remap_matrix"));
            Assert.AreEqual(expectedMatrix.Length, result.Metadata.SkinTable.RemapMatrix.Count, goldenPath);
            for (var i = 0; i < expectedMatrix.Length; i++)
            {
                CollectionAssert.AreEqual(expectedMatrix[i], result.Metadata.SkinTable.RemapMatrix[i].ToArray(), goldenPath);
            }

            compared++;
        }

        if (compared == 0)
        {
            Assert.Inconclusive("Golden JSON files were available, but the matching corpus MDL files were not present.");
        }
    }

    private static string[] ReadStringArray(JsonElement element) =>
        element.EnumerateArray().Select(item => item.GetString()!).ToArray();

    private static short[][] ReadShortMatrix(JsonElement element) =>
        element.EnumerateArray()
            .Select(row => row.EnumerateArray().Select(item => checked((short)item.GetInt32())).ToArray())
            .ToArray();

    private static string? FindDirectory(params string[] segments)
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            var candidate = segments.Aggregate(current.FullName, Path.Combine);
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return null;
    }
}
