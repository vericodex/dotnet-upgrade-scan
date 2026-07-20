using UpgradeScan.Core.Abstractions;
using UpgradeScan.Core.Model;
using UpgradeScan.Core.Pipeline;
using Xunit;

namespace UpgradeScan.Analysis.Tests;

public class TierDegradationTests
{
    [Fact]
    public void UnbuildableProjectDegradesToManifestWithDiagnostic()
    {
        using var dir = new TempDir();
        var csproj = Path.Combine(dir.Path, "Broken.csproj");
        File.WriteAllText(csproj, """
            <Project Sdk="Microsoft.NET.Sdk">
              <Import Project="does-not-exist.targets" />
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        var analyzer = new TieredProjectAnalyzer([new BuildalyzerTierAnalyzer(), new ManifestAnalyzer()]);
        var result = analyzer.Analyze(csproj);

        Assert.Equal(AnalysisTier.Manifest, result.Tier);
        Assert.Equal(["net8.0"], result.TargetFrameworks);
        Assert.Contains(result.Diagnostics, d =>
            d.Code is DiagnosticCodes.BuildFailed or DiagnosticCodes.AnalyzerError);
    }
}
