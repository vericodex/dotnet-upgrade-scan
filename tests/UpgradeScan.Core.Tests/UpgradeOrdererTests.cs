using UpgradeScan.Core.Model;
using UpgradeScan.Core.Pipeline;
using Xunit;

namespace UpgradeScan.Core.Tests;

public class UpgradeOrdererTests
{
    private static ProjectAnalysis Proj(string name, params string[] refs) => new()
    {
        Name = name,
        FullPath = $"/s/{name}/{name}.csproj",
        Tier = AnalysisTier.Manifest,
        Style = ProjectStyle.SdkStyle,
        Language = "C#",
        TargetFrameworks = ["net472"],
        Packages = [],
        ProjectReferences = [.. refs.Select(r => $"/s/{r}/{r}.csproj")],
    };

    private static SolutionAnalysis Sln(params ProjectAnalysis[] projects) =>
        new() { FullPath = "/s/All.sln", Projects = projects };

    [Fact]
    public void LeavesComeFirstDependentsAfter()
    {
        var result = UpgradeOrderer.Order(Sln(Proj("App", "Lib"), Proj("Lib")));
        Assert.Equal(["Lib", "App"], result.Order);
        Assert.Empty(result.Cycles);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void TiesBreakAlphabetically()
    {
        var result = UpgradeOrderer.Order(Sln(Proj("Zeta"), Proj("Alpha"), Proj("Mid", "Alpha")));
        Assert.Equal(["Alpha", "Zeta", "Mid"], result.Order);
    }

    [Fact]
    public void CycleIsReportedNotFatal()
    {
        var result = UpgradeOrderer.Order(Sln(Proj("A", "B"), Proj("B", "A"), Proj("Solo")));
        Assert.Equal(["Solo", "A", "B"], result.Order);
        Assert.Single(result.Cycles);
        Assert.Equal(["A", "B"], result.Cycles[0]);
        var diag = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.DependencyCycle, diag.Code);
        Assert.Equal(DiagnosticSeverity.Warning, diag.Severity);
    }

    [Fact]
    public void ReferencesOutsideTheSolutionAreIgnored()
    {
        var result = UpgradeOrderer.Order(Sln(Proj("App", "NotInSolution")));
        Assert.Equal(["App"], result.Order);
        Assert.Empty(result.Cycles);
    }
}
