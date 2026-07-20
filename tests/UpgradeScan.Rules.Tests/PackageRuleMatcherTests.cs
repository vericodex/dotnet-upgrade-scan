using UpgradeScan.Core.Model;
using UpgradeScan.Rules;

namespace UpgradeScan.Rules.Tests;

public class PackageRuleMatcherTests
{
    private static RuleSet Rules(params PackageRule[] packages) => new()
    {
        Packages = packages,
        ApiGroups = [],
        Scoring = TestScoring.Default,
        Hash = "000000000000",
    };

    private static PackageRule SqlClient(PackageVerdict verdict = PackageVerdict.Replace) => new()
    {
        Id = "PKG0002",
        Package = "System.Data.SqlClient",
        Verdict = verdict,
        Severity = FindingSeverity.Warning,
        Replacement = new PackageReplacement("Microsoft.Data.SqlClient", "Namespace changes."),
    };

    [Fact]
    public void MatchIsCaseInsensitiveOnPackageId()
    {
        var findings = PackageRuleMatcher.Match(
            [new PackageRef("system.data.sqlclient", "4.8.5")], "net10.0", Rules(SqlClient()));
        var f = Assert.Single(findings);
        Assert.Equal("PKG0002", f.RuleId);
        Assert.Equal(PackageVerdict.Replace, f.Verdict);
        Assert.Equal("Microsoft.Data.SqlClient", f.ReplacementPackage);
        Assert.Equal("4.8.5", f.PackageVersion);
    }

    [Fact]
    public void TargetOverrideWinsOverFileVerdict()
    {
        var rule = SqlClient() with
        {
            TargetOverrides = new Dictionary<string, PackageVerdict> { ["net8.0"] = PackageVerdict.Incompatible },
        };
        Assert.Equal(PackageVerdict.Incompatible,
            Assert.Single(PackageRuleMatcher.Match([new PackageRef("System.Data.SqlClient", null)], "net8.0", Rules(rule))).Verdict);
        Assert.Equal(PackageVerdict.Replace,
            Assert.Single(PackageRuleMatcher.Match([new PackageRef("System.Data.SqlClient", null)], "net10.0", Rules(rule))).Verdict);
    }

    [Fact]
    public void UnknownPackagesProduceNoFindings()
    {
        Assert.Empty(PackageRuleMatcher.Match([new PackageRef("Totally.Unknown", "1.0")], "net10.0", Rules(SqlClient())));
    }

    [Fact]
    public void FindingsAreOrderedByRuleIdThenPackage()
    {
        var other = SqlClient() with { Id = "PKG0001", Package = "Newtonsoft.Json", Verdict = PackageVerdict.Compatible, Replacement = null };
        var findings = PackageRuleMatcher.Match(
            [new PackageRef("System.Data.SqlClient", null), new PackageRef("Newtonsoft.Json", null)],
            "net10.0", Rules(SqlClient(), other));
        Assert.Equal(["PKG0001", "PKG0002"], findings.Select(f => f.RuleId));
    }
}

internal static class TestScoring
{
    public static ScoringConfig Default => new()
    {
        CategoryWeights = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["web"] = 10, ["wcf"] = 8, ["default"] = 3 },
        NoReplacementWeight = 15,
        WithReplacementWeight = 3,
        SmallMax = 20,
        MediumMax = 60,
        LargeMax = 150,
        XlFloorTypes = [ProjectType.AspNetWebForms, ProjectType.WcfService],
        VbOneSizeUp = true,
    };
}
