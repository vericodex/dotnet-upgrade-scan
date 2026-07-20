using UpgradeScan.Core.Model;
using UpgradeScan.Rules;

namespace UpgradeScan.Rules.Tests;

public class ApiRuleMatcherTests
{
    private static RuleSet Rules() => new()
    {
        Packages = [],
        ApiGroups =
        [
            new ApiRuleGroup
            {
                GroupId = "API0100",
                Technology = "ASP.NET (System.Web)",
                Category = "web",
                Patterns =
                [
                    new ApiPattern { Id = "API0101", Kind = ApiPatternKind.Namespace, Match = "System.Web", Severity = FindingSeverity.Blocker },
                    new ApiPattern { Id = "API0102", Kind = ApiPatternKind.Type, Match = "System.Web.HttpContext", Severity = FindingSeverity.Blocker },
                ],
            },
        ],
        Scoring = TestScoring.Default,
        Hash = "000000000000",
    };

    private static ApiUsage Use(string symbol, int line = 1) => new(symbol, "File.cs", line, false);

    [Fact]
    public void NamespaceMatchesItselfAndDescendants()
    {
        Assert.Equal("API0101", Assert.Single(ApiRuleMatcher.Match([Use("System.Web")], Rules())).RuleId);
        Assert.Equal("API0101", Assert.Single(ApiRuleMatcher.Match([Use("System.Web.UI.Page")], Rules())).RuleId);
        Assert.Empty(ApiRuleMatcher.Match([Use("System.WebSockets.Thing")], Rules()));
    }

    [Fact]
    public void MostSpecificPatternWins()
    {
        var finding = Assert.Single(ApiRuleMatcher.Match([Use("System.Web.HttpContext.Current")], Rules()));
        Assert.Equal("API0102", finding.RuleId);
        Assert.Equal("web", finding.Category);
    }

    [Fact]
    public void ApproximateFlagAndLocationFlowThrough()
    {
        var finding = Assert.Single(ApiRuleMatcher.Match([new ApiUsage("System.Web.HttpUtility", "Legacy.cs", 42, true)], Rules()));
        Assert.True(finding.Approximate);
        Assert.Equal("Legacy.cs", finding.File);
        Assert.Equal(42, finding.Line);
    }

    [Fact]
    public void FindingsAreOrderedByRuleIdFileLine()
    {
        var findings = ApiRuleMatcher.Match(
            [Use("System.Web.HttpContext", 9), Use("System.Web.UI.Page", 5), Use("System.Web.UI.Page", 2)],
            Rules());
        Assert.Equal([("API0101", 2), ("API0101", 5), ("API0102", 9)],
            findings.Select(f => (f.RuleId, f.Line)));
    }
}
