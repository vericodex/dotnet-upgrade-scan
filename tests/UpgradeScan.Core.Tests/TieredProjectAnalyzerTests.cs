using UpgradeScan.Core.Abstractions;
using UpgradeScan.Core.Model;
using UpgradeScan.Core.Pipeline;
using Xunit;

namespace UpgradeScan.Core.Tests;

public class TieredProjectAnalyzerTests
{
    private sealed class StubTier(ProjectAnalysis? result, ScanDiagnostic? diagnostic = null) : ITierAnalyzer
    {
        public ProjectAnalysis? TryAnalyze(string projectPath, ICollection<ScanDiagnostic> diagnostics)
        {
            if (diagnostic is not null)
                diagnostics.Add(diagnostic);
            return result;
        }
    }

    private static ProjectAnalysis Sample(string language = "C#") => new()
    {
        Name = "App",
        FullPath = "/x/App.csproj",
        Tier = AnalysisTier.Manifest,
        Style = ProjectStyle.Legacy,
        Language = language,
        TargetFrameworks = ["net472"],
        Packages = [],
        ProjectReferences = [],
    };

    [Fact]
    public void FirstSuccessfulTierWinsAndDiagnosticsAreMerged()
    {
        var failDiag = new ScanDiagnostic(DiagnosticCodes.BuildFailed, DiagnosticSeverity.Warning, "build failed");
        var analyzer = new TieredProjectAnalyzer([
            new StubTier(null, failDiag),
            new StubTier(Sample()),
        ]);

        var result = analyzer.Analyze("/x/App.csproj");

        Assert.Equal("App", result.Name);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.BuildFailed);
    }

    [Fact]
    public void VbResultGetsReducedAnalysisDiagnostic()
    {
        var analyzer = new TieredProjectAnalyzer([new StubTier(Sample(language: "VB"))]);

        var result = analyzer.Analyze("/x/App.vbproj");

        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.VbReducedAnalysis);
    }

    [Fact]
    public void AllTiersDecliningYieldsUnknownProjectWithError()
    {
        var analyzer = new TieredProjectAnalyzer([new StubTier(null)]);

        var result = analyzer.Analyze("/x/Mystery.csproj");

        Assert.Equal("Mystery", result.Name);
        Assert.Equal(ProjectStyle.Unknown, result.Style);
        Assert.Equal("Unknown", result.Language);
        Assert.Contains(result.Diagnostics,
            d => d.Code == DiagnosticCodes.ProjectUnreadable && d.Severity == DiagnosticSeverity.Error);
    }
}
