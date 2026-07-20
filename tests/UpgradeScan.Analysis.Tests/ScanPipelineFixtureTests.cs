using UpgradeScan.Core.Model;
using UpgradeScan.Core.Pipeline;
using Xunit;

namespace UpgradeScan.Analysis.Tests;

public class ScanPipelineFixtureTests
{
    [Fact]
    public void ManifestOnlyPipelineScansFixtureEndToEnd()
    {
        var pipeline = new ScanPipeline(
            new SolutionLoader(),
            new TieredProjectAnalyzer([new ManifestAnalyzer()]));

        var result = pipeline.Run(Fixtures.Path("net472-two-proj", "All.sln"));

        Assert.Equal(2, result.Projects.Count);
        Assert.Equal("App", result.Projects[0].Name);
        Assert.Equal("Lib", result.Projects[1].Name);
        Assert.All(result.Projects, p => Assert.Equal(AnalysisTier.Manifest, p.Tier));
        Assert.All(result.Projects, p => Assert.Equal(["net472"], p.TargetFrameworks));
    }
}
