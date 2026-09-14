using SfmPackageBuilder.Core.Planning;

namespace SfmPackageBuilder.Core.Staging;

internal sealed class StagingPlanEntry
{
    public StagingPlanEntry(PackagePlanEntry entry, StagingPath path, StagingWriteKind writeKind, byte[]? contentBytes)
    {
        Entry = entry;
        Path = path;
        WriteKind = writeKind;
        ContentBytes = contentBytes?.ToArray();
    }

    public PackagePlanEntry Entry { get; }

    public StagingPath Path { get; }

    public StagingWriteKind WriteKind { get; }

    public byte[]? ContentBytes { get; }

    public bool IsSourceBacked => Entry.SourcePath is not null;

    public string SourceIdentity =>
        Entry.SourcePath is null
            ? $"generated:{Entry.Id}"
            : System.IO.Path.GetFullPath(Entry.SourcePath).TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
}
