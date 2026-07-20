using UpgradeScan.Core.Model;
using Xunit;

namespace UpgradeScan.Analysis.Tests;

public class FixtureScanTests
{
    [Fact]
    public void ManifestScanOfNet472FixtureFindsBothProjects()
    {
        var sln = Fixtures.Path("net472-two-proj", "All.sln");
        var loader = new SolutionLoader();
        var analyzer = new ManifestAnalyzer();
        var diags = new List<ScanDiagnostic>();

        var results = loader.FindProjects(sln)
            .Select(p => analyzer.TryAnalyze(p, diags))
            .ToList();

        Assert.Equal(2, results.Count);
        Assert.All(results, Assert.NotNull);
        var app = results.Single(r => r!.Name == "App")!;
        Assert.Equal(["net472"], app.TargetFrameworks);
        Assert.Equal(ProjectStyle.Legacy, app.Style);
        Assert.Equal("Newtonsoft.Json", Assert.Single(app.Packages).Id);
        Assert.EndsWith("Lib.csproj", Assert.Single(app.ProjectReferences));
        Assert.Empty(diags);
    }
}
