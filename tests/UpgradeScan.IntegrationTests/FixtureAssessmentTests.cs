using UpgradeScan.Core.Model;

namespace UpgradeScan.IntegrationTests;

public class FixtureAssessmentTests
{
    [Fact]
    public void CyclePairIsAReportedGroupNotACrash()
    {
        var model = PipelineHarness.Run(Fixtures.Path("cycle-pair", "Cycle.sln"));

        var cycle = Assert.Single(model.Cycles);
        Assert.Equal(["A", "B"], cycle);
        var diagnostic = Assert.Single(model.Diagnostics);
        Assert.Equal(DiagnosticCodes.DependencyCycle, diagnostic.Code);
        Assert.Equal(["A", "B"], model.UpgradeOrder);
    }

    [Fact]
    public void VbLibIsDetectedWithReducedAnalysisAndFloor()
    {
        var model = PipelineHarness.Run(Fixtures.Path("vb-lib"));

        var p = Assert.Single(model.Projects);
        Assert.Equal("VB", p.Analysis.Language);
        Assert.Equal(AnalysisTier.Manifest, p.Analysis.Tier);
        Assert.Contains(p.Analysis.Diagnostics, d => d.Code == DiagnosticCodes.VbReducedAnalysis);
        Assert.Equal(EffortBand.M, p.Effort.Band);
        Assert.Contains(p.Effort.FloorsApplied, f => f.Contains("VB"));
    }

    [Fact]
    public void WcfClientGetsTypeAndBlockerFindings()
    {
        var model = PipelineHarness.Run(Fixtures.Path("wcf-client"));

        var p = Assert.Single(model.Projects);
        Assert.Equal(ProjectType.WcfClient, p.Analysis.Type);
        Assert.Equal(AnalysisTier.Syntactic, p.Analysis.Tier);
        Assert.Contains(p.ApiFindings, f => f.RuleId == "API0201" && f.Approximate);
        Assert.Contains(p.ApiFindings, f => f.RuleId == "API0202");
    }

    [Fact]
    public void Mvc5WebGetsTypePackagesAndWebBlockers()
    {
        var model = PipelineHarness.Run(Fixtures.Path("mvc5-web"));

        var p = Assert.Single(model.Projects);
        Assert.Equal(ProjectType.AspNetMvc, p.Analysis.Type);
        Assert.Contains(p.PackageFindings, f => f.RuleId == "PKG0001");
        Assert.All(p.ApiFindings, f => Assert.True(f.Approximate));
        Assert.Equal(2, p.ApiFindings.Count(f => f.RuleId == "API0101"));
        Assert.Equal(20, p.Effort.Score);
        Assert.Equal(EffortBand.M, p.Effort.Band);
    }
}
