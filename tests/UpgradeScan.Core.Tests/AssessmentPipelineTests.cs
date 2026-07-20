using UpgradeScan.Core.Abstractions;
using UpgradeScan.Core.Model;
using UpgradeScan.Core.Pipeline;
using Xunit;

namespace UpgradeScan.Core.Tests;

public class AssessmentPipelineTests
{
    private sealed class StubLoader(IReadOnlyList<string> paths) : ISolutionLoader
    {
        public IReadOnlyList<string> FindProjects(string path) => paths;
    }

    private sealed class StubTier : ITierAnalyzer
    {
        public ProjectAnalysis? TryAnalyze(string projectPath, ICollection<ScanDiagnostic> diagnostics)
        {
            var name = Path.GetFileNameWithoutExtension(projectPath);
            return new ProjectAnalysis
            {
                Name = name,
                FullPath = Path.GetFullPath(projectPath),
                Tier = AnalysisTier.Manifest,
                Style = ProjectStyle.Legacy,
                Language = "C#",
                TargetFrameworks = ["net472"],
                Packages = [],
                ProjectReferences = name == "App" ? [Path.GetFullPath("/s/Lib/Lib.csproj")] : [],
            };
        }
    }

    private sealed class StubAssessor : IProjectAssessor
    {
        public ProjectAssessment Assess(ProjectAnalysis project) => new()
        {
            Analysis = project,
            PackageFindings = [],
            ApiFindings = [],
            Effort = new EffortScore { Score = 0, Band = EffortBand.S },
        };
    }

    private static AssessmentPipeline Pipeline() => new(
        new StubLoader(["/s/App/App.csproj", "/s/Lib/Lib.csproj"]),
        new TieredProjectAnalyzer([new StubTier()]),
        new StubAssessor());

    private static AssessmentContext Context(DateTimeOffset? scanDate = null) =>
        new("net10.0", "1.2.3-test", "abcdef123456", scanDate);

    [Fact]
    public void ProjectsAreOrderedTopologicallyThenAlphabetically()
    {
        var model = Pipeline().Run("/s", Context());

        Assert.Equal(["Lib", "App"], model.UpgradeOrder);
        Assert.Equal(["Lib", "App"], model.Projects.Select(p => p.Analysis.Name));
        Assert.Empty(model.Cycles);
        Assert.Empty(model.Diagnostics);
    }

    [Fact]
    public void ContextFlowsThroughUnchanged()
    {
        var date = new DateTimeOffset(2026, 7, 18, 0, 0, 0, TimeSpan.Zero);

        var deterministic = Pipeline().Run("/s", Context());
        Assert.Equal("net10.0", deterministic.TargetFramework);
        Assert.Equal("1.2.3-test", deterministic.ToolVersion);
        Assert.Equal("abcdef123456", deterministic.RulesHash);
        Assert.Null(deterministic.ScanDate);

        Assert.Equal(date, Pipeline().Run("/s", Context(date)).ScanDate);
    }
}
