using UpgradeScan.Core.Model;
using Xunit;

namespace UpgradeScan.Core.Tests;

public class ModelDefaultsTests
{
    private static ProjectAnalysis MinimalProject() => new()
    {
        Name = "P",
        FullPath = "/p/P.csproj",
        Tier = AnalysisTier.Manifest,
        Style = ProjectStyle.SdkStyle,
        Language = "C#",
        TargetFrameworks = ["net8.0"],
        Packages = [],
        ProjectReferences = [],
    };

    [Fact]
    public void NewProjectAnalysisPropertiesDefaultSafely()
    {
        var p = MinimalProject();
        Assert.Equal(ProjectType.Unknown, p.Type);
        Assert.Empty(p.ApiUsages);
    }

    [Fact]
    public void SolutionAnalysisDiagnosticsDefaultEmpty()
    {
        var s = new SolutionAnalysis { FullPath = "/p", Projects = [] };
        Assert.Empty(s.Diagnostics);
    }

    [Fact]
    public void AssessmentModelHoldsSchemaVersion1()
    {
        Assert.Equal(1, AssessmentModel.SchemaVersion);
        Assert.Equal("UPS0005", DiagnosticCodes.DependencyCycle);
    }
}
