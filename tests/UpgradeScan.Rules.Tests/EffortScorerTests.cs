using UpgradeScan.Core.Model;
using UpgradeScan.Rules;

namespace UpgradeScan.Rules.Tests;

public class EffortScorerTests
{
    private static ProjectAnalysis Project(ProjectType type = ProjectType.Library, string language = "C#") => new()
    {
        Name = "P",
        FullPath = "/p/P.csproj",
        Tier = AnalysisTier.Semantic,
        Style = ProjectStyle.Legacy,
        Language = language,
        Type = type,
        TargetFrameworks = ["net472"],
        Packages = [],
        ProjectReferences = [],
    };

    private static ApiFinding Blocker(string category, int line) =>
        new("API0101", category, FindingSeverity.Blocker, "S.W.X", "F.cs", line, false, null);

    private static PackageFinding Pkg(PackageVerdict verdict, string? replacement) =>
        new("PKG0002", "X", null, verdict, FindingSeverity.Warning, replacement, null);

    [Fact]
    public void FormulaMatchesSpec()
    {
        var score = EffortScorer.Score(Project(),
            [Pkg(PackageVerdict.Incompatible, null), Pkg(PackageVerdict.Replace, "New.Pkg")],
            [Blocker("web", 1), Blocker("web", 2), Blocker("odd-category", 3)],
            TestScoring.Default);
        Assert.Equal(41, score.Score);
        Assert.Equal(EffortBand.M, score.Band);
        Assert.Empty(score.FloorsApplied);
    }

    [Fact]
    public void CompatiblePackagesAndNonBlockerApisDoNotScore()
    {
        var score = EffortScorer.Score(Project(),
            [Pkg(PackageVerdict.Compatible, null)],
            [new ApiFinding("API0101", "web", FindingSeverity.Warning, "S", "F.cs", 1, false, null)],
            TestScoring.Default);
        Assert.Equal(0, score.Score);
        Assert.Equal(EffortBand.S, score.Band);
    }

    [Fact]
    public void WebFormsFloorForcesXl()
    {
        var score = EffortScorer.Score(Project(ProjectType.AspNetWebForms), [], [], TestScoring.Default);
        Assert.Equal(EffortBand.XL, score.Band);
        Assert.Contains(score.FloorsApplied, f => f.Contains("AspNetWebForms"));
    }

    [Fact]
    public void VbGoesOneSizeUp()
    {
        var score = EffortScorer.Score(Project(language: "VB"), [], [], TestScoring.Default);
        Assert.Equal(EffortBand.M, score.Band);
        Assert.Contains(score.FloorsApplied, f => f.Contains("VB"));
    }

    [Fact]
    public void BandBoundariesAreExact()
    {
        static EffortScore At(int weight) => EffortScorer.Score(
            new ProjectAnalysis
            {
                Name = "P", FullPath = "/p", Tier = AnalysisTier.Manifest, Style = ProjectStyle.Legacy,
                Language = "C#", TargetFrameworks = [], Packages = [], ProjectReferences = [],
            },
            [],
            [.. Enumerable.Range(1, weight).Select(i =>
                new ApiFinding("API0101", "unit", FindingSeverity.Blocker, "S", "F.cs", i, false, null))],
            TestScoring.Default with
            {
                CategoryWeights = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["unit"] = 1, ["default"] = 1 },
            });
        Assert.Equal(EffortBand.S, At(19).Band);
        Assert.Equal(EffortBand.M, At(20).Band);
        Assert.Equal(EffortBand.L, At(60).Band);
        Assert.Equal(EffortBand.XL, At(150).Band);
    }
}
