using SfmPackageBuilder.Core.Domain;

namespace SfmPackageBuilder.Core.Tests;

internal static class TestProjectFactory
{
    public static PackageProject CreateRepresentativeProject()
    {
        return new PackageProject
        {
            AssetName = "Chair Prop",
            CurrentVersion = "1.1",
            ArchiveName = "Chair_Prop_v1.1.zip",
            Models = new List<ModelEntry>
            {
                new()
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Role = ModelRole.Primary,
                    SourceMdlPath = @"E:\SFM\game\usermod\models\Creator\props\chair\chair_dev.mdl",
                    SourceStem = "chair_dev",
                    ReleaseStem = "chair_release",
                    Companions = new List<ModelCompanionSelection>
                    {
                        new()
                        {
                            SourcePath = @"E:\SFM\game\usermod\models\Creator\props\chair\chair_dev.vvd",
                            RuntimeSuffix = ".vvd",
                            UserSelection = CompanionUserSelection.Include
                        },
                        new()
                        {
                            SourcePath = @"E:\SFM\game\usermod\models\Creator\props\chair\chair_dev.dx90.vtx",
                            RuntimeSuffix = ".dx90.vtx",
                            UserSelection = CompanionUserSelection.Include
                        }
                    }
                },
                new()
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Role = ModelRole.Additional,
                    SourceMdlPath = @"E:\SFM\game\usermod\models\Creator\shared\hinge.mdl",
                    SourceStem = "hinge",
                    ReleaseStem = "hinge",
                    Companions = new List<ModelCompanionSelection>
                    {
                        new()
                        {
                            SourcePath = @"E:\SFM\game\usermod\models\Creator\shared\hinge.vvd",
                            RuntimeSuffix = ".vvd",
                            UserSelection = CompanionUserSelection.Exclude
                        }
                    }
                }
            },
            MaterialSources = new List<SourceEntry>
            {
                new()
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Kind = SourceEntryKind.Material,
                    SourcePath = @"E:\SFM\game\usermod\materials\models\Creator\props\chair",
                    IsFolder = true,
                    IncludeRecursively = true,
                    NaturalDestination = @"materials\models\Creator\props\chair"
                },
                new()
                {
                    Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    Kind = SourceEntryKind.Material,
                    SourcePath = @"D:\Exports\body.vtf",
                    IsFolder = false,
                    IncludeRecursively = false,
                    DestinationOverride = new DestinationOverride
                    {
                        Kind = DestinationOverrideKind.Custom,
                        RelativePath = @"materials\models\Creator\shared",
                        Reason = "Orphaned source file without a materials anchor."
                    }
                }
            },
            Extras = new List<SourceEntry>
            {
                new()
                {
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    Kind = SourceEntryKind.Extra,
                    SourcePath = @"E:\ReleaseDocs\LICENSE.txt",
                    IsFolder = false,
                    IncludeRecursively = false,
                    DestinationOverride = new DestinationOverride
                    {
                        Kind = DestinationOverrideKind.Root,
                        RelativePath = string.Empty
                    }
                },
                new()
                {
                    Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                    Kind = SourceEntryKind.Extra,
                    SourcePath = @"E:\ReleaseDocs\screenshots",
                    IsFolder = true,
                    IncludeRecursively = true,
                    DestinationOverride = new DestinationOverride
                    {
                        Kind = DestinationOverrideKind.Misc,
                        RelativePath = "Misc"
                    }
                }
            },
            Readme = new ReadmeConfig
            {
                Mode = ReadmeMode.Custom,
                CustomSource = ReadmeCustomSource.ProjectText,
                Description = "A release-ready chair prop.",
                Author = "Creator",
                Website = "https://example.test",
                License = "Custom",
                AdditionalResources = "Includes screenshots.",
                IncludeInstallInstructions = true,
                IncludeModelPath = true,
                IncludeVersionChanges = true,
                ImportedReadmePath = @"E:\ReleaseDocs\README-source.txt",
                CustomReadmeText = "Custom readme stored in the project."
            },
            ReleaseHistory = new List<ReleaseRecord>
            {
                new()
                {
                    Version = "1.0",
                    Changes = new List<string> { "Initial release." }
                },
                new()
                {
                    Version = "1.1",
                    Changes = new List<string>
                    {
                        "Added opening doors.",
                        "Improved collision model."
                    }
                }
            },
            BuildHistory = new List<BuildRecord>
            {
                new()
                {
                    Version = "1.1",
                    ArchiveName = "Chair_Prop_v1.1.zip",
                    BuildTimestamp = DateTimeOffset.Parse("2026-02-01T10:00:00-05:00")
                },
                new()
                {
                    Version = "1.0",
                    ArchiveName = "Chair_Prop_v1.0.zip",
                    BuildTimestamp = DateTimeOffset.Parse("2026-01-01T10:00:00-05:00")
                }
            },
            InformationalState = new List<ProjectNoticeState>
            {
                new()
                {
                    Key = "sfm_defaultanimationgroups",
                    IsAcknowledged = true
                }
            }
        };
    }
}
