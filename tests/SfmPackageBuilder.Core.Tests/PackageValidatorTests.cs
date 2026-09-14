using Microsoft.VisualStudio.TestTools.UnitTesting;
using SfmPackageBuilder.Core.Domain;
using SfmPackageBuilder.Core.FileSystem;
using SfmPackageBuilder.Core.Planning;
using SfmPackageBuilder.Core.Readme;
using SfmPackageBuilder.Core.SharedFiles;
using SfmPackageBuilder.Core.Validation;

namespace SfmPackageBuilder.Core.Tests;

[TestClass]
public sealed class PackageValidatorTests
{
    [TestMethod]
    public void ValidPackageCanBuild()
    {
        var fixture = CreateValidFixture();

        var result = fixture.Validate();

        Assert.IsTrue(result.CanBuild, Codes(result));
        Assert.IsFalse(result.HasErrors);
        Assert.IsFalse(result.HasRequiredDecisions);
    }

    [TestMethod]
    public void MissingPrimaryMdlIsError()
    {
        var fixture = CreateValidFixture(addModel: false);

        var result = fixture.Validate();

        AssertHasError(result, "planned-source-missing");
    }

    [TestMethod]
    public void MissingIncludedCompanionIsError()
    {
        var fixture = CreateValidFixture();
        fixture.Project.Models[0].Companions.Add(new ModelCompanionSelection
        {
            RuntimeSuffix = ".vvd",
            SourcePath = @"E:\SFM\game\usermod\models\Creator\chair\chair.vvd",
            UserSelection = CompanionUserSelection.Include
        });

        var result = fixture.Validate();

        AssertHasError(result, "planned-source-missing");
    }

    [TestMethod]
    public void MissingMaterialIsError()
    {
        var fixture = CreateValidFixture(addMaterial: false);

        var result = fixture.Validate();

        AssertHasError(result, "planned-source-missing");
    }

    [TestMethod]
    public void MissingExtraIsError()
    {
        var fixture = CreateValidFixture();
        fixture.Project.Extras.Add(ExtraFile(@"E:\Missing\LICENSE.txt", DestinationOverrideKind.Root, string.Empty));

        var result = fixture.Validate();

        AssertHasError(result, "planned-source-missing");
    }

    [TestMethod]
    public void UnreadableSourceIsError()
    {
        var fixture = CreateValidFixture();
        var plan = fixture.Plan();
        fixture.FileSystem.FailRead(@"E:\SFM\game\usermod\materials\models\Creator\chair\chair.vmt");

        var result = fixture.Validate(plan);

        AssertHasError(result, "source-unreadable");
    }

    [TestMethod]
    public void ImportedReadmeDisappearedAfterPlanningIsError()
    {
        var fixture = CreateValidFixture();
        fixture.FileSystem.AddFile(@"E:\Docs\README.txt", Array.Empty<byte>());
        fixture.Project.Readme.Mode = ReadmeMode.Custom;
        fixture.Project.Readme.CustomSource = ReadmeCustomSource.ImportedFile;
        fixture.Project.Readme.ImportedReadmePath = @"E:\Docs\README.txt";
        var plan = fixture.Plan();
        fixture.FileSystem.RemoveFile(@"E:\Docs\README.txt");

        var result = fixture.Validate(plan);

        AssertHasError(result, "source-missing");
    }

    [TestMethod]
    public void ValidModelAndMaterialDestinationsPassDestinationSafety()
    {
        var result = CreateValidFixture().Validate();

        AssertNoError(result, "unsafe-destination");
    }

    [TestMethod]
    public void MissingAnchorIsError()
    {
        var fixture = CreateValidFixture(addMaterial: false);
        fixture.FileSystem.AddFile(@"D:\RandomExports\body.vtf", Array.Empty<byte>());
        fixture.Project.MaterialSources.Clear();
        fixture.Project.MaterialSources.Add(MaterialFile(@"D:\RandomExports\body.vtf"));

        var result = fixture.Validate();

        AssertHasError(result, "planned-anchor-missing");
    }

    [TestMethod]
    public void InvalidOverrideIsError()
    {
        var fixture = CreateValidFixture();
        fixture.FileSystem.AddFile(@"E:\Release\doc.txt", Array.Empty<byte>());
        fixture.Project.Extras.Add(ExtraFile(@"E:\Release\doc.txt", DestinationOverrideKind.Custom, @"..\outside"));

        var result = fixture.Validate();

        AssertHasError(result, "planned-invalid-destination-override");
    }

    [TestMethod]
    public void TraversalDestinationInImpossiblePlanIsError()
    {
        var result = CreateValidFixture().Validate(new PackagePlan(new[]
        {
            new PackagePlanEntry(
                "bad",
                PackagePlanEntryType.Extra,
                PackagePlanEntryStatus.Resolved,
                @"..\outside.txt",
                @"E:\Release\source.txt",
                Guid.NewGuid(),
                null,
                isGenerated: false)
        }, Array.Empty<SharedFileNotice>()));

        AssertHasError(result, "unsafe-destination");
    }

    [TestMethod]
    [DataRow(@"..\evil.txt")]
    [DataRow(@"C:\absolute.txt")]
    [DataRow(@"\rooted.txt")]
    [DataRow(@"\\server\share\file")]
    [DataRow("CON")]
    [DataRow("CON.txt")]
    [DataRow("AUX")]
    [DataRow("NUL")]
    [DataRow("COM1")]
    [DataRow("LPT9")]
    [DataRow("name.")]
    [DataRow("name<bad>.txt")]
    public void UnsafePackageDestinationsAreBlockingErrors(string destination)
    {
        var fixture = CreateValidFixture();
        fixture.FileSystem.AddFile(@"E:\Release\source.txt", Array.Empty<byte>());
        var result = fixture.Validate(new PackagePlan(new[]
        {
            Entry("bad", @"E:\Release\source.txt", destination)
        }, Array.Empty<SharedFileNotice>()));

        AssertHasError(result, "unsafe-destination");
    }

    [TestMethod]
    [DataRow(@"materials\\models//Creator\chair\body.vtf")]
    [DataRow(@"README.txt")]
    [DataRow(@"scripts\sfm\animset\rig.py")]
    public void NormalPackageDestinationsRemainValid(string destination)
    {
        var fixture = CreateValidFixture();
        fixture.FileSystem.AddFile(@"E:\Release\source.txt", Array.Empty<byte>());
        var result = fixture.Validate(new PackagePlan(new[]
        {
            Entry("ok", @"E:\Release\source.txt", destination)
        }, Array.Empty<SharedFileNotice>()));

        AssertNoError(result, "unsafe-destination");
    }

    [TestMethod]
    public void LongPackageDestinationIsNotRejectedForLegacy260CharacterLimit()
    {
        var fixture = CreateValidFixture();
        fixture.FileSystem.AddFile(@"E:\Release\source.txt", Array.Empty<byte>());
        var longDestination = @"materials\" +
            string.Join('\\', Enumerable.Range(0, 26).Select(index => "nested_segment_" + index.ToString("00"))) +
            @"\body.vtf";

        Assert.IsTrue(longDestination.Length > 260);
        var result = fixture.Validate(new PackagePlan(new[]
        {
            Entry("long", @"E:\Release\source.txt", longDestination)
        }, Array.Empty<SharedFileNotice>()));

        AssertNoError(result, "unsafe-destination");
    }

    [TestMethod]
    [DataRow("chair_release")]
    [DataRow("chair release")]
    [DataRow("椅子 release")]
    [DataRow("")]
    [DataRow("  ")]
    public void ValidReleaseStemsAreAllowed(string stem)
    {
        var fixture = CreateValidFixture();
        fixture.Project.Models[0].ReleaseStem = stem;

        var result = fixture.Validate();

        AssertNoError(result, "invalid-release-model-name");
    }

    [TestMethod]
    [DataRow("bad/name")]
    [DataRow("bad\\name")]
    [DataRow("bad:name")]
    [DataRow("CON")]
    [DataRow("name.")]
    [DataRow("name ")]
    public void InvalidReleaseStemsAreErrors(string stem)
    {
        var fixture = CreateValidFixture();
        fixture.Project.Models[0].ReleaseStem = stem;

        var result = fixture.Validate();

        var message = AssertHasError(result, "invalid-release-model-name");
        Assert.IsFalse(message.Message.Contains("not a valid Windows filename", StringComparison.Ordinal));
        Assert.AreEqual(0, message.SourcePaths.Count);
        Assert.IsTrue(message.Detail?.Contains("Name:", StringComparison.Ordinal) == true);
    }

    [TestMethod]
    public void InvalidReleaseStemShowsSpecificDestinationNameReason()
    {
        var fixture = CreateValidFixture();
        fixture.Project.Models[0].ReleaseStem = "chair:model";

        var result = fixture.Validate();

        var message = AssertHasError(result, "invalid-release-model-name");
        Assert.AreEqual("Model name in ZIP contains a character Windows does not allow.", message.Message);
        Assert.AreEqual("Name: chair:model" + Environment.NewLine + "Remove \":\" from the model name.", message.Detail);
        Assert.AreEqual(0, message.SourcePaths.Count);
        CollectionAssert.AreEqual(new[] { "chair:model" }, message.AffectedValues.ToArray());
    }

    [TestMethod]
    public void InvalidAdditionalModelReleaseStemUsesSameValidation()
    {
        var fixture = CreateValidFixture();
        fixture.FileSystem.AddFile(@"E:\SFM\game\usermod\models\Creator\chair\shirt_dev04.mdl", Array.Empty<byte>());
        fixture.Project.Models.Add(new ModelEntry
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Role = ModelRole.Additional,
            SourceMdlPath = @"E:\SFM\game\usermod\models\Creator\chair\shirt_dev04.mdl",
            SourceStem = "shirt_dev04",
            ReleaseStem = "shirt:name"
        });

        var result = fixture.Validate();

        var message = AssertHasError(result, "invalid-release-model-name");
        Assert.AreEqual("Model name in ZIP contains a character Windows does not allow.", message.Message);
        CollectionAssert.AreEqual(new[] { "shirt:name" }, message.AffectedValues.ToArray());
    }

    [TestMethod]
    public void RenamedAdditionalModelUsesOrdinaryDestinationCollisionDetection()
    {
        var fixture = CreateValidFixture();
        fixture.FileSystem.AddFile(@"E:\SFM\game\usermod\models\Creator\chair\shirt_dev04.mdl", Array.Empty<byte>());
        fixture.Project.Models.Add(new ModelEntry
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Role = ModelRole.Additional,
            SourceMdlPath = @"E:\SFM\game\usermod\models\Creator\chair\shirt_dev04.mdl",
            SourceStem = "shirt_dev04",
            ReleaseStem = "chair_release"
        });

        var result = fixture.Validate();

        AssertHasError(result, "destination-collision");
    }

    [TestMethod]
    public void DifferentSourcesSameDestinationIsCollisionError()
    {
        var fixture = CreateValidFixture(addMaterial: false);
        fixture.FileSystem.AddFile(@"E:\SFM\materials\models\foo\body.vtf", Array.Empty<byte>());
        fixture.FileSystem.AddFile(@"D:\backup\materials\models\foo\body.vtf", Array.Empty<byte>());
        fixture.Project.MaterialSources.Clear();
        fixture.Project.MaterialSources.Add(MaterialFile(@"E:\SFM\materials\models\foo\body.vtf"));
        fixture.Project.MaterialSources.Add(MaterialFile(@"D:\backup\materials\models\foo\body.vtf"));

        var result = fixture.Validate();

        var collision = AssertHasError(result, "destination-collision");
        Assert.AreEqual(2, collision.SourcePaths.Count);
        Assert.AreEqual(2, collision.RelatedEntryIds.Count);
    }

    [TestMethod]
    public void CaseOnlyDestinationDifferenceIsCollisionError()
    {
        var fixture = CreateValidFixture();
        var plan = new PackagePlan(new[]
        {
            Entry("a", @"E:\A\body.vtf", @"materials\models\Foo\body.vtf"),
            Entry("b", @"E:\B\body.vtf", @"materials\models\foo\BODY.vtf")
        }, Array.Empty<SharedFileNotice>());
        fixture.FileSystem.AddFile(@"E:\A\body.vtf", Array.Empty<byte>());
        fixture.FileSystem.AddFile(@"E:\B\body.vtf", Array.Empty<byte>());

        var result = fixture.Validate(plan);

        AssertHasError(result, "destination-collision");
    }

    [TestMethod]
    public void ExtraMaterialCollisionIsError()
    {
        var fixture = CreateValidFixture(addMaterial: false);
        fixture.FileSystem.AddFile(@"E:\SFM\materials\models\foo\body.vtf", Array.Empty<byte>());
        fixture.FileSystem.AddFile(@"E:\Release\body.vtf", Array.Empty<byte>());
        fixture.Project.MaterialSources.Clear();
        fixture.Project.MaterialSources.Add(MaterialFile(@"E:\SFM\materials\models\foo\body.vtf"));
        fixture.Project.Extras.Add(ExtraFile(@"E:\Release\body.vtf", DestinationOverrideKind.Custom, @"materials\models\foo"));

        var result = fixture.Validate();

        AssertHasError(result, "destination-collision");
    }

    [TestMethod]
    public void ModelExtraCollisionIsError()
    {
        var fixture = CreateValidFixture();
        fixture.FileSystem.AddFile(@"E:\Release\chair_release.mdl", Array.Empty<byte>());
        fixture.Project.Extras.Add(ExtraFile(@"E:\Release\chair_release.mdl", DestinationOverrideKind.Custom, @"models\Creator\chair"));

        var result = fixture.Validate();

        AssertHasError(result, "destination-collision");
    }

    [TestMethod]
    public void SamePhysicalSourceSameDestinationIsInformation()
    {
        var fixture = CreateValidFixture(addMaterial: false);
        fixture.FileSystem.AddFile(@"E:\SFM\materials\models\foo\body.vtf", Array.Empty<byte>());
        fixture.Project.MaterialSources.Clear();
        fixture.Project.MaterialSources.Add(MaterialFile(@"E:\SFM\materials\models\foo\body.vtf"));
        fixture.Project.MaterialSources.Add(MaterialFile(@"E:\SFM\materials\models\foo\body.vtf"));

        var result = fixture.Validate();

        AssertHasInformation(result, "duplicate-source-destination");
        Assert.AreEqual(2, result.Messages.Single(message => message.Code == "duplicate-source-destination").RelatedEntryIds.Count);
        AssertNoError(result, "destination-collision");
    }

    [TestMethod]
    public void GeneratedReadmeValidates()
    {
        var result = CreateValidFixture().Validate();

        AssertNoError(result, "readme-content-missing");
    }

    [TestMethod]
    public void CustomProjectTextReadmeValidates()
    {
        var fixture = CreateValidFixture();
        fixture.Project.Readme.Mode = ReadmeMode.Custom;
        fixture.Project.Readme.CustomSource = ReadmeCustomSource.ProjectText;
        fixture.Project.Readme.CustomReadmeText = "Custom";

        var result = fixture.Validate();

        AssertNoError(result, "readme-content-missing");
    }

    [TestMethod]
    public void ImportedReadmeValidates()
    {
        var fixture = CreateValidFixture();
        fixture.FileSystem.AddFile(@"E:\Docs\README.txt", Array.Empty<byte>());
        fixture.Project.Readme.Mode = ReadmeMode.Custom;
        fixture.Project.Readme.CustomSource = ReadmeCustomSource.ImportedFile;
        fixture.Project.Readme.ImportedReadmePath = @"E:\Docs\README.txt";

        var result = fixture.Validate();

        AssertNoError(result, "source-missing");
    }

    [TestMethod]
    public void ImportedReadmeMissingIsError()
    {
        var fixture = CreateValidFixture();
        fixture.Project.Readme.Mode = ReadmeMode.Custom;
        fixture.Project.Readme.CustomSource = ReadmeCustomSource.ImportedFile;
        fixture.Project.Readme.ImportedReadmePath = @"E:\Docs\README.txt";

        var result = fixture.Validate();

        AssertHasError(result, "planned-readme-resolution-failed");
    }

    [TestMethod]
    public void ImportedReadmeUnreadableIsError()
    {
        var fixture = CreateValidFixture();
        fixture.FileSystem.AddFile(@"E:\Docs\README.txt", Array.Empty<byte>());
        fixture.FileSystem.FailRead(@"E:\Docs\README.txt");
        fixture.Project.Readme.Mode = ReadmeMode.Custom;
        fixture.Project.Readme.CustomSource = ReadmeCustomSource.ImportedFile;
        fixture.Project.Readme.ImportedReadmePath = @"E:\Docs\README.txt";

        var result = fixture.Validate();

        AssertHasError(result, "planned-readme-resolution-failed");
    }

    [TestMethod]
    public void NoReadmeIsAllowed()
    {
        var fixture = CreateValidFixture();
        fixture.Project.Readme.Mode = ReadmeMode.None;

        var result = fixture.Validate();

        AssertHasInformation(result, "readme-none");
        Assert.IsFalse(result.HasErrors);
    }

    [TestMethod]
    public void MultipleReadmePlanEntriesAreRejected()
    {
        var fixture = CreateValidFixture();
        var entries = fixture.Plan().Entries.Concat(new[]
        {
            new PackagePlanEntry(
                "readme2",
                PackagePlanEntryType.Readme,
                PackagePlanEntryStatus.Resolved,
                "README.txt",
                null,
                null,
                null,
                isGenerated: true,
                readmeKind: ReadmePlanEntryKind.GeneratedText,
                textContent: "extra",
                contentBytes: Array.Empty<byte>())
        }).ToArray();

        var result = fixture.Validate(new PackagePlan(entries, Array.Empty<SharedFileNotice>()));

        AssertHasError(result, "multiple-readme-entries");
    }

    [TestMethod]
    public void SharedFileInformationIsSurfacedAndDoesNotMutatePreference()
    {
        var fixture = CreateValidFixture();
        fixture.FileSystem.AddFile(@"E:\Release\sfm_defaultanimationgroups.txt", Array.Empty<byte>());
        fixture.Project.Extras.Add(ExtraFile(@"E:\Release\sfm_defaultanimationgroups.txt", DestinationOverrideKind.Root, string.Empty));

        var result = fixture.Validate();

        var message = AssertHasInformation(result, "shared-file-information");
        Assert.AreEqual("SFM control-groups file included. It may overwrite the user's existing setup.", message.Message);
        StringAssert.Contains(message.Detail!, "one shared control-groups");
    }

    [TestMethod]
    public void KnownLiveDestinationNoticeBecomesWarningAndDoesNotBlock()
    {
        var fixture = CreateValidFixture();
        fixture.FileSystem.AddFile(@"E:\Release\sfm_defaultanimationgroups.txt", Array.Empty<byte>());
        fixture.Project.Extras.Add(ExtraFile(@"E:\Release\sfm_defaultanimationgroups.txt", DestinationOverrideKind.Custom, "cfg"));
        var registry = new SharedFileRegistry(new[]
        {
            new SharedFileRule(
                KnownSharedFiles.ControlGroupsRuleKey,
                KnownSharedFiles.ControlGroupsFilename,
                new[] { @"cfg\sfm_defaultanimationgroups.txt" },
                KnownSharedFiles.ControlGroupsInformationTitle,
                KnownSharedFiles.ControlGroupsInformationText,
                providesControlGroupsReadmeContext: true)
        });
        var plan = new PackagePlanner(
            new SfmPackageBuilder.Core.Expansion.SourceExpansionService(fixture.FileSystem),
            registry,
            new SfmPackageBuilder.Core.Readme.ReadmeResolver(fixture.FileSystem)).CreatePlan(fixture.Project);

        var result = fixture.Validate(plan);

        var message = AssertHasWarning(result, "shared-file-live-destination");
        Assert.AreEqual("SFM control-groups file included. It may overwrite the user's existing setup.", message.Message);
        StringAssert.Contains(message.Detail!, "one shared control-groups");
        Assert.IsFalse(result.HasErrors);
    }

    [TestMethod]
    public void NewVersionRequiresNoDecision()
    {
        var fixture = CreateValidFixture();
        fixture.Project.CurrentVersion = "2.0";
        fixture.Project.ReleaseHistory.Add(new ReleaseRecord { Version = "1.0" });
        fixture.Project.BuildHistory.Add(new BuildRecord { Version = "1.0", ArchiveName = "Chair_Prop_v1.0.zip", BuildTimestamp = DateTimeOffset.Now });

        Assert.IsFalse(fixture.Validate().RequiredDecisions.Any(decision => decision.Kind == RequiredDecisionKind.VersionReuse));
    }

    [TestMethod]
    public void CurrentVersionAppearingOnceInReadmeChangelogDoesNotRequireReuseDecision()
    {
        var fixture = CreateValidFixture();
        fixture.Project.CurrentVersion = "1.0.2";
        fixture.Project.ReleaseHistory.Add(new ReleaseRecord { Version = "1.0.2", Changes = new List<string> { "current release notes" } });
        fixture.Project.ReleaseHistory.Add(new ReleaseRecord { Version = "1.0.1", Changes = new List<string> { "old notes" } });

        var result = fixture.Validate();

        Assert.IsFalse(result.RequiredDecisions.Any(decision => decision.Kind == RequiredDecisionKind.VersionReuse));
        AssertNoError(result, "version-reuse");
        AssertNoWarning(result, "duplicate-readme-changelog-version");
        Assert.IsTrue(result.CanBuild, Codes(result));
    }

    [TestMethod]
    public void DuplicateReadmeChangelogEntriesAreNonblockingWarning()
    {
        var fixture = CreateValidFixture();
        fixture.Project.CurrentVersion = "1.0.2";
        fixture.Project.ReleaseHistory.Add(new ReleaseRecord { Version = "1.0.2", Changes = new List<string> { "current release notes" } });
        fixture.Project.ReleaseHistory.Add(new ReleaseRecord { Version = "1.0.2", Changes = new List<string> { "duplicate notes" } });

        var result = fixture.Validate();

        AssertHasWarning(result, "duplicate-readme-changelog-version");
        Assert.IsFalse(result.RequiredDecisions.Any(decision => decision.Kind == RequiredDecisionKind.VersionReuse));
        Assert.IsTrue(result.CanBuild, Codes(result));
    }

    [TestMethod]
    public void PreviouslyBuiltVersionRequiresDecisionButIsNotError()
    {
        var fixture = CreateValidFixture();
        fixture.Project.CurrentVersion = "1.0";
        fixture.Project.ReleaseHistory.Add(new ReleaseRecord { Version = "1.0", Changes = new List<string> { "Current notes." } });
        fixture.Project.BuildHistory.Add(new BuildRecord { Version = "1.0", ArchiveName = "Chair_Prop_v1.0.zip", BuildTimestamp = DateTimeOffset.Now });

        var result = fixture.Validate();

        Assert.IsTrue(result.RequiredDecisions.Any(decision => decision.Kind == RequiredDecisionKind.VersionReuse));
        AssertNoError(result, "version-reuse");
        Assert.IsFalse(result.CanBuild);
        Assert.AreEqual(1, fixture.Project.ReleaseHistory.Count);
        Assert.AreEqual(1, fixture.Project.BuildHistory.Count);
    }

    [TestMethod]
    public void CustomReadmeProvenanceWithoutPackageChangesDoesNotReportStale()
    {
        var fixture = CreateValidFixture();
        ConvertGeneratedReadmeToCustomWithCurrentFingerprint(fixture);

        var result = fixture.Validate();

        AssertNoInformation(result, "custom-readme-package-details-changed");
        Assert.IsTrue(result.CanBuild, Codes(result));
    }

    [TestMethod]
    public void CustomReadmeProvenancePackageChangeReportsInformationAndDoesNotBlock()
    {
        var fixture = CreateValidFixture();
        ConvertGeneratedReadmeToCustomWithCurrentFingerprint(fixture);
        var customText = fixture.Project.Readme.CustomReadmeText;
        fixture.Project.AssetName = "Changed Chair Prop";

        var result = fixture.Validate();

        var message = AssertHasInformation(result, "custom-readme-package-details-changed");
        Assert.AreEqual("Package details changed after you customized the README.", message.Message);
        Assert.AreEqual("Review your custom README if it mentions those details.", message.Detail);
        Assert.IsTrue(result.CanBuild, Codes(result));
        Assert.AreEqual(customText, fixture.Project.Readme.CustomReadmeText);
    }

    [TestMethod]
    public void LegacyCustomReadmeWithoutProvenanceDoesNotReportStale()
    {
        var fixture = CreateValidFixture();
        fixture.Project.Readme.Mode = ReadmeMode.Custom;
        fixture.Project.Readme.CustomSource = ReadmeCustomSource.ProjectText;
        fixture.Project.Readme.CustomReadmeText = "Legacy custom README.";
        fixture.Project.Readme.CustomReadmeGeneratedFromFingerprint = null;
        fixture.Project.AssetName = "Changed Chair Prop";

        var result = fixture.Validate();

        AssertNoInformation(result, "custom-readme-package-details-changed");
        Assert.IsTrue(result.CanBuild, Codes(result));
    }

    [TestMethod]
    public void ValidArchiveNameAndOutputLocationPass()
    {
        var result = CreateValidFixture().Validate();

        AssertNoError(result, "invalid-archive-name");
        AssertNoError(result, "output-directory-missing");
        AssertNoError(result, "output-directory-unwritable");
    }

    [TestMethod]
    [DataRow("bad/name.zip")]
    [DataRow("bad:name.zip")]
    [DataRow("archive.txt")]
    [DataRow("")]
    public void InvalidArchiveNameIsError(string archiveName)
    {
        var fixture = CreateValidFixture();
        fixture.Project.ArchiveName = archiveName;

        var result = fixture.Validate();

        AssertHasError(result, "invalid-archive-name");
    }

    [TestMethod]
    public void UnwritableOutputIsError()
    {
        var fixture = CreateValidFixture();
        fixture.Output.CanWrite = false;

        var result = fixture.Validate();

        AssertHasError(result, "output-directory-unwritable");
    }

    [TestMethod]
    public void MissingOutputDirectoryIsError()
    {
        var fixture = CreateValidFixture();
        var result = new PackageValidator(fixture.FileSystem, fixture.Output).Validate(
            fixture.Project,
            fixture.Plan(),
            new PackageValidationContext
            {
                OutputDirectory = @"O:\Missing"
            });

        AssertHasError(result, "output-directory-missing");
    }

    [TestMethod]
    public void ExistingArchiveRequiresDecisionAndIsNotDeleted()
    {
        var fixture = CreateValidFixture();
        fixture.Output.AddFile(@"O:\Output\Chair_Prop_v1.0.zip");

        var result = fixture.Validate();

        Assert.IsTrue(result.RequiredDecisions.Any(decision => decision.Kind == RequiredDecisionKind.ExistingOutputArchive));
        Assert.IsTrue(fixture.Output.FileExists(@"O:\Output\Chair_Prop_v1.0.zip"));
    }

    [TestMethod]
    public void ArchiveAbsentRequiresNoOutputDecision()
    {
        var result = CreateValidFixture().Validate();

        Assert.IsFalse(result.RequiredDecisions.Any(decision => decision.Kind == RequiredDecisionKind.ExistingOutputArchive));
    }

    [TestMethod]
    public void WarningAloneDoesNotBlockButDecisionDoes()
    {
        var result = new ValidationResult(
            new[] { new ValidationMessage(ValidationSeverity.Warning, "warning", "Warning.") },
            Array.Empty<RequiredDecision>());

        Assert.IsTrue(result.CanBuild);

        var withDecision = new ValidationResult(
            Array.Empty<ValidationMessage>(),
            new[] { new RequiredDecision(RequiredDecisionKind.VersionReuse, "version-reuse", "Version exists.", new[] { RequiredDecisionOption.Cancel }) });

        Assert.IsFalse(withDecision.CanBuild);
    }

    [TestMethod]
    public void InformationAloneDoesNotBlock()
    {
        var result = new ValidationResult(
            new[] { new ValidationMessage(ValidationSeverity.Information, "info", "Info.") },
            Array.Empty<RequiredDecision>());

        Assert.IsTrue(result.CanBuild);
    }

    [TestMethod]
    public void ErrorBlocksBuild()
    {
        var result = new ValidationResult(
            new[] { new ValidationMessage(ValidationSeverity.Error, "error", "Error.") },
            Array.Empty<RequiredDecision>());

        Assert.IsFalse(result.CanBuild);
    }

    [TestMethod]
    public void ValidationDoesNotMutatePhysicalSourceFixtures()
    {
        var root = Path.Combine(Path.GetTempPath(), "SfmPackageBuilderValidatorTests", Guid.NewGuid().ToString("N"));
        var modelDir = Path.Combine(root, "models", "Creator", "chair");
        var materialDir = Path.Combine(root, "materials", "models", "Creator", "chair");
        var outputDir = Path.Combine(root, "output");
        Directory.CreateDirectory(modelDir);
        Directory.CreateDirectory(materialDir);
        Directory.CreateDirectory(outputDir);
        var modelPath = Path.Combine(modelDir, "chair.mdl");
        var materialPath = Path.Combine(materialDir, "chair.vmt");
        File.WriteAllText(modelPath, "model");
        File.WriteAllText(materialPath, "material");
        File.SetAttributes(modelPath, FileAttributes.ReadOnly);

        try
        {
            var beforeNames = Directory.GetFileSystemEntries(modelDir).Concat(Directory.GetFileSystemEntries(materialDir)).Select(Path.GetFileName).Order().ToArray();
            var beforeBytes = new[] { File.ReadAllBytes(modelPath), File.ReadAllBytes(materialPath) };
            var beforeAttributes = new[] { File.GetAttributes(modelPath), File.GetAttributes(materialPath) };
            var project = BasicProject(modelPath, materialDir);
            var fileSystem = new PhysicalFileSystem();
            var plan = new PackagePlanner(fileSystem).CreatePlan(project);
            var validator = new PackageValidator(fileSystem, new PhysicalOutputEnvironment());

            _ = validator.Validate(project, plan, new PackageValidationContext
            {
                OutputDirectory = outputDir
            });

            var afterNames = Directory.GetFileSystemEntries(modelDir).Concat(Directory.GetFileSystemEntries(materialDir)).Select(Path.GetFileName).Order().ToArray();
            CollectionAssert.AreEqual(beforeNames, afterNames);
            CollectionAssert.AreEqual(beforeBytes.Select(Convert.ToBase64String).ToArray(), new[] { File.ReadAllBytes(modelPath), File.ReadAllBytes(materialPath) }.Select(Convert.ToBase64String).ToArray());
            CollectionAssert.AreEqual(beforeAttributes, new[] { File.GetAttributes(modelPath), File.GetAttributes(materialPath) });
        }
        finally
        {
            File.SetAttributes(modelPath, FileAttributes.Normal);
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public void ExistingOutputArchiveRemainsUntouchedWithPhysicalProbe()
    {
        var root = Path.Combine(Path.GetTempPath(), "SfmPackageBuilderOutputTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var archivePath = Path.Combine(root, "Chair_Prop_v1.0.zip");
        File.WriteAllText(archivePath, "existing archive");
        var beforeBytes = File.ReadAllBytes(archivePath);

        try
        {
            var output = new PhysicalOutputEnvironment();
            var result = new OutputValidator(output).Validate(root, "Chair_Prop_v1.0.zip");

            Assert.IsTrue(result.RequiredDecisions.Any(decision => decision.Kind == RequiredDecisionKind.ExistingOutputArchive));
            CollectionAssert.AreEqual(beforeBytes, File.ReadAllBytes(archivePath));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public void ValidationMessageOrderingIsDeterministic()
    {
        var fixture = CreateValidFixture(addMaterial: false);
        fixture.Project.Models[0].ReleaseStem = "CON";
        fixture.Project.MaterialSources.Clear();
        fixture.FileSystem.AddFile(@"E:\A\body.vtf", Array.Empty<byte>());
        fixture.FileSystem.AddFile(@"E:\B\body.vtf", Array.Empty<byte>());
        fixture.Project.MaterialSources.Add(MaterialFile(@"E:\A\body.vtf", Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), @"materials\same.vtf"));
        fixture.Project.MaterialSources.Add(MaterialFile(@"E:\B\body.vtf", Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), @"materials\same.vtf"));

        CollectionAssert.AreEqual(Codes(fixture.Validate()).Split('|'), Codes(fixture.Validate()).Split('|'));
    }

    private static ValidationFixture CreateValidFixture(bool addModel = true, bool addMaterial = true)
    {
        var fileSystem = new FakeFileSystem();
        if (addModel)
        {
            fileSystem.AddFile(@"E:\SFM\game\usermod\models\Creator\chair\chair.mdl", Array.Empty<byte>());
        }

        if (addMaterial)
        {
            fileSystem.AddFile(@"E:\SFM\game\usermod\materials\models\Creator\chair\chair.vmt", Array.Empty<byte>());
        }

        var output = new FakeOutputEnvironment();
        output.AddDirectory(@"O:\Output");
        return new ValidationFixture(fileSystem, output, BasicProject());
    }

    private static PackageProject BasicProject(string modelPath = @"E:\SFM\game\usermod\models\Creator\chair\chair.mdl", string materialPath = @"E:\SFM\game\usermod\materials\models\Creator\chair")
    {
        return new PackageProject
        {
            AssetName = "Chair Prop",
            CurrentVersion = "1.0",
            ArchiveName = "Chair_Prop_v1.0.zip",
            Models = new List<ModelEntry>
            {
                new()
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Role = ModelRole.Primary,
                    SourceMdlPath = modelPath,
                    SourceStem = "chair",
                    ReleaseStem = "chair_release"
                }
            },
            MaterialSources = new List<SourceEntry>
            {
                new()
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Kind = SourceEntryKind.Material,
                    SourcePath = materialPath,
                    IsFolder = true,
                    IncludeRecursively = true
                }
            },
            Readme = new ReadmeConfig { Mode = ReadmeMode.Generated }
        };
    }

    private static void ConvertGeneratedReadmeToCustomWithCurrentFingerprint(ValidationFixture fixture)
    {
        var plan = fixture.Plan();
        var context = new ReadmeContextBuilder().Create(
            fixture.Project,
            plan.Entries.Where(entry => entry.EntryType != PackagePlanEntryType.Readme).ToArray(),
            plan.SharedFileNotices);
        var generated = new ReadmeGenerator().Generate(fixture.Project, context);
        fixture.Project.Readme = new ReadmeConversionService().UseGeneratedTextAsCustomReadme(fixture.Project.Readme, generated);
    }

    private static SourceEntry MaterialFile(string path, Guid? id = null, string? overrideDestination = null)
    {
        return new SourceEntry
        {
            Id = id ?? Guid.NewGuid(),
            Kind = SourceEntryKind.Material,
            SourcePath = path,
            IsFolder = false,
            DestinationOverride = overrideDestination is null
                ? null
                : new DestinationOverride
                {
                    Kind = DestinationOverrideKind.Custom,
                    RelativePath = Path.GetDirectoryName(overrideDestination)?.Replace('/', '\\') ?? string.Empty
                }
        };
    }

    private static SourceEntry ExtraFile(string path, DestinationOverrideKind kind, string relativePath)
    {
        return new SourceEntry
        {
            Id = Guid.NewGuid(),
            Kind = SourceEntryKind.Extra,
            SourcePath = path,
            IsFolder = false,
            DestinationOverride = new DestinationOverride
            {
                Kind = kind,
                RelativePath = relativePath
            }
        };
    }

    private static PackagePlanEntry Entry(string id, string sourcePath, string destination)
    {
        return new PackagePlanEntry(
            id,
            PackagePlanEntryType.Material,
            PackagePlanEntryStatus.Resolved,
            destination,
            sourcePath,
            Guid.NewGuid(),
            null,
            isGenerated: false);
    }

    private static ValidationMessage AssertHasError(ValidationResult result, string code) =>
        AssertHas(result, ValidationSeverity.Error, code);

    private static ValidationMessage AssertHasWarning(ValidationResult result, string code) =>
        AssertHas(result, ValidationSeverity.Warning, code);

    private static ValidationMessage AssertHasInformation(ValidationResult result, string code) =>
        AssertHas(result, ValidationSeverity.Information, code);

    private static ValidationMessage AssertHas(ValidationResult result, ValidationSeverity severity, string code)
    {
        var message = result.Messages.FirstOrDefault(message => message.Severity == severity && message.Code == code);
        Assert.IsNotNull(message, $"Expected {severity} '{code}'. Actual: {Codes(result)}");
        return message;
    }

    private static void AssertNoError(ValidationResult result, string code)
    {
        Assert.IsFalse(
            result.Messages.Any(message => message.Severity == ValidationSeverity.Error && message.Code == code),
            $"Did not expect Error '{code}'. Actual: {Codes(result)}");
    }

    private static void AssertNoWarning(ValidationResult result, string code)
    {
        Assert.IsFalse(
            result.Messages.Any(message => message.Severity == ValidationSeverity.Warning && message.Code == code),
            $"Did not expect Warning '{code}'. Actual: {Codes(result)}");
    }

    private static void AssertNoInformation(ValidationResult result, string code)
    {
        Assert.IsFalse(
            result.Messages.Any(message => message.Severity == ValidationSeverity.Information && message.Code == code),
            $"Did not expect Information '{code}'. Actual: {Codes(result)}");
    }

    private static string Codes(ValidationResult result)
    {
        return string.Join("|", result.Messages.Select(message => $"{message.Severity}:{message.Code}"));
    }

    private sealed class ValidationFixture
    {
        public ValidationFixture(
            FakeFileSystem fileSystem,
            FakeOutputEnvironment output,
            PackageProject project)
        {
            FileSystem = fileSystem;
            Output = output;
            Project = project;
        }

        public FakeFileSystem FileSystem { get; }

        public FakeOutputEnvironment Output { get; }

        public PackageProject Project { get; }

        public PackagePlan Plan() => new PackagePlanner(FileSystem).CreatePlan(Project);

        public ValidationResult Validate() => Validate(Plan());

        public ValidationResult Validate(PackagePlan plan)
        {
            return new PackageValidator(FileSystem, Output).Validate(Project, plan, new PackageValidationContext
            {
                OutputDirectory = @"O:\Output"
            });
        }
    }
}
