using Microsoft.VisualStudio.TestTools.UnitTesting;
using SfmPackageBuilder.Core.Domain;
using SfmPackageBuilder.Core.SharedFiles;

namespace SfmPackageBuilder.Core.Tests;

[TestClass]
public sealed class SharedFileRegistryTests
{
    [TestMethod]
    public void LowercaseControlGroupsFilenameProducesNotice()
    {
        var notices = Analyze(Candidate("a", @"Misc\sfm_defaultanimationgroups.txt"));

        Assert.AreEqual(1, notices.Count);
        Assert.AreEqual(SharedFileNoticeKind.Information, notices[0].Kind);
        Assert.AreEqual(KnownSharedFiles.ControlGroupsRuleKey, notices[0].RuleKey);
    }

    [TestMethod]
    public void MixedCaseControlGroupsFilenameProducesNotice()
    {
        var notices = Analyze(Candidate("a", @"Misc\SFM_DefaultAnimationGroups.TXT"));

        Assert.AreEqual(1, notices.Count);
        Assert.AreEqual("a", notices[0].CandidateId);
    }

    [TestMethod]
    public void UnrelatedFilenameProducesNoNotice()
    {
        var notices = Analyze(Candidate("a", @"Misc\README.txt"));

        Assert.AreEqual(0, notices.Count);
    }

    [TestMethod]
    public void FilenameDetectionRequiresExactFilenameNotSubstring()
    {
        var notices = Analyze(Candidate("a", @"Misc\my_sfm_defaultanimationgroups.txt"));

        Assert.AreEqual(0, notices.Count);
    }

    [TestMethod]
    public void IndividualExtraOriginCanBeRecognized()
    {
        var notices = Analyze(new SharedFileCandidate(
            "extra-file",
            @"Misc\sfm_defaultanimationgroups.txt",
            @"E:\Release\sfm_defaultanimationgroups.txt",
            SharedFileCandidateOrigin.ExtraFile));

        Assert.AreEqual(1, notices.Count);
        Assert.AreEqual("extra-file", notices[0].CandidateId);
        Assert.AreEqual(@"E:\Release\sfm_defaultanimationgroups.txt", notices[0].SourcePath);
    }

    [TestMethod]
    public void ExpandedFolderOriginCanBeRecognized()
    {
        var notices = Analyze(new SharedFileCandidate(
            "expanded-folder-file",
            @"docs\sfm_defaultanimationgroups.txt",
            @"E:\Release\docs\sfm_defaultanimationgroups.txt",
            SharedFileCandidateOrigin.ExpandedFolderFile));

        Assert.AreEqual(1, notices.Count);
        Assert.AreEqual("expanded-folder-file", notices[0].CandidateId);
    }

    [TestMethod]
    public void SourceMechanismDoesNotAffectRecognition()
    {
        var candidates = new[]
        {
            new SharedFileCandidate("extra", @"A\sfm_defaultanimationgroups.txt", origin: SharedFileCandidateOrigin.ExtraFile),
            new SharedFileCandidate("folder", @"B\sfm_defaultanimationgroups.txt", origin: SharedFileCandidateOrigin.ExpandedFolderFile),
            new SharedFileCandidate("unknown", @"C\sfm_defaultanimationgroups.txt", origin: SharedFileCandidateOrigin.Unknown)
        };

        var notices = new SharedFileRegistry().Analyze(candidates);

        CollectionAssert.AreEquivalent(new[] { "extra", "folder", "unknown" }, notices.Select(notice => notice.CandidateId).ToArray());
    }

    [TestMethod]
    public void FilenameDetectionProducesInformationalMetadata()
    {
        var notice = Analyze(Candidate("a", @"Misc\sfm_defaultanimationgroups.txt")).Single();

        Assert.AreEqual(SharedFileNoticeKind.Information, notice.Kind);
        Assert.AreEqual(KnownSharedFiles.ControlGroupsInformationTitle, notice.Title);
        StringAssert.Contains(notice.Body, "one shared control-groups");
        Assert.IsFalse(notice.IsBlocking);
    }

    [TestMethod]
    public void FilenameDetectionProducesControlGroupsSharedFileSignal()
    {
        var notices = Analyze(Candidate("a", @"Misc\sfm_defaultanimationgroups.txt"));

        Assert.IsTrue(SharedFileRegistry.HasControlGroupsReadmeContext(notices));
    }

    [TestMethod]
    public void ExplicitRegisteredLiveDestinationProducesWarningMetadata()
    {
        var registry = RegistryWithLiveDestination(@"cfg\sfm_defaultanimationgroups.txt");

        var notices = registry.Analyze(new[] { Candidate("a", @"cfg\sfm_defaultanimationgroups.txt") });

        Assert.AreEqual(2, notices.Count);
        Assert.IsTrue(notices.Any(notice => notice.Kind == SharedFileNoticeKind.Information));
        Assert.IsTrue(notices.Any(notice => notice.Kind == SharedFileNoticeKind.LiveDestinationWarning));
        Assert.IsTrue(notices.All(notice => !notice.IsBlocking));
    }

    [TestMethod]
    public void SameFilenameAtNonLiveDestinationDoesNotProduceDestinationWarning()
    {
        var registry = RegistryWithLiveDestination(@"cfg\sfm_defaultanimationgroups.txt");

        var notices = registry.Analyze(new[] { Candidate("a", @"Misc\sfm_defaultanimationgroups.txt") });

        Assert.AreEqual(1, notices.Count);
        Assert.AreEqual(SharedFileNoticeKind.Information, notices[0].Kind);
    }

    [TestMethod]
    public void LiveDestinationMatchingIsCaseInsensitive()
    {
        var registry = RegistryWithLiveDestination(@"cfg\sfm_defaultanimationgroups.txt");

        var notices = registry.Analyze(new[] { Candidate("a", @"CFG\SFM_DEFAULTANIMATIONGROUPS.TXT") });

        Assert.IsTrue(notices.Any(notice => notice.Kind == SharedFileNoticeKind.LiveDestinationWarning));
    }

    [TestMethod]
    public void SlashNormalizedEquivalentDestinationMatches()
    {
        var registry = RegistryWithLiveDestination(@"cfg\sfm_defaultanimationgroups.txt");

        var notices = registry.Analyze(new[] { Candidate("a", "cfg/sfm_defaultanimationgroups.txt") });

        Assert.IsTrue(notices.Any(notice => notice.Kind == SharedFileNoticeKind.LiveDestinationWarning));
        Assert.IsTrue(notices.All(notice => notice.DestinationRelativePath == @"cfg\sfm_defaultanimationgroups.txt"));
    }

    [TestMethod]
    public void MisleadingSubpathDestinationDoesNotMatchLiveDestination()
    {
        var registry = RegistryWithLiveDestination(@"cfg\sfm_defaultanimationgroups.txt");

        var notices = registry.Analyze(new[] { Candidate("a", @"cfg\sfm_defaultanimationgroups.txt\other.txt") });

        Assert.AreEqual(0, notices.Count);
    }

    [TestMethod]
    public void MultipleSharedFileCandidatesRemainIndependentlyIdentifiable()
    {
        var notices = Analyze(
            Candidate("a", @"A\sfm_defaultanimationgroups.txt"),
            Candidate("b", @"B\sfm_defaultanimationgroups.txt"));

        Assert.AreEqual(2, notices.Count);
        CollectionAssert.AreEqual(new[] { "a", "b" }, notices.Select(notice => notice.CandidateId).ToArray());
    }

    [TestMethod]
    public void RegistryIsExtensibleWithAdditionalRule()
    {
        var registry = new SharedFileRegistry(new[]
        {
            new SharedFileRule(
                "test_rule",
                "shared_test.txt",
                Array.Empty<string>(),
                "shared_test.txt included",
                "Test-only shared file.")
        });

        var notices = registry.Analyze(new[] { Candidate("test", @"Misc\shared_test.txt") });

        Assert.AreEqual(1, notices.Count);
        Assert.AreEqual("test_rule", notices[0].RuleKey);
    }

    [TestMethod]
    public void RegistryDoesNotModifyInputObjects()
    {
        var candidate = Candidate("a", @"Misc/sfm_defaultanimationgroups.txt", @"E:\Source\sfm_defaultanimationgroups.txt");
        var originalDestination = candidate.DestinationRelativePath;
        var originalSource = candidate.SourcePath;

        _ = Analyze(candidate);

        Assert.AreEqual(originalDestination, candidate.DestinationRelativePath);
        Assert.AreEqual(originalSource, candidate.SourcePath);
    }

    private static IReadOnlyList<SharedFileNotice> Analyze(params SharedFileCandidate[] candidates)
    {
        return new SharedFileRegistry().Analyze(candidates);
    }

    private static SharedFileCandidate Candidate(string id, string destination, string? source = null)
    {
        return new SharedFileCandidate(id, destination, source);
    }

    private static SharedFileRegistry RegistryWithLiveDestination(string destination)
    {
        return new SharedFileRegistry(new[]
        {
            new SharedFileRule(
                KnownSharedFiles.ControlGroupsRuleKey,
                KnownSharedFiles.ControlGroupsFilename,
                new[] { destination },
                KnownSharedFiles.ControlGroupsInformationTitle,
                KnownSharedFiles.ControlGroupsInformationText,
                providesControlGroupsReadmeContext: true)
        });
    }
}
