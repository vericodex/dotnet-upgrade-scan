using UpgradeScan.Core.Model;

namespace UpgradeScan.Reporting.Tests;

internal static class SampleModel
{
    internal static AssessmentModel Build(DateTimeOffset? scanDate = null)
    {
        var lib = new ProjectAnalysis
        {
            Name = "Lib",
            FullPath = "/s/Lib/Lib.csproj",
            Tier = AnalysisTier.Syntactic,
            Style = ProjectStyle.Legacy,
            Language = "C#",
            Type = ProjectType.Library,
            TargetFrameworks = ["net472"],
            Packages = [new PackageRef("Newtonsoft.Json", "13.0.1")],
            ProjectReferences = [],
        };
        var app = new ProjectAnalysis
        {
            Name = "App",
            FullPath = "/s/App/App.csproj",
            Tier = AnalysisTier.Syntactic,
            Style = ProjectStyle.Legacy,
            Language = "C#",
            Type = ProjectType.AspNetMvc,
            TargetFrameworks = ["net472"],
            Packages = [new PackageRef("EntityFramework", "6.4.4"), new PackageRef("System.Data.SqlClient", "4.8.5")],
            ProjectReferences = ["/s/Lib/Lib.csproj"],
            Diagnostics = [new ScanDiagnostic(DiagnosticCodes.BuildFailed, DiagnosticSeverity.Warning,
                "App: design-time build failed; degrading to manifest analysis.")],
        };
        return new AssessmentModel
        {
            SolutionPath = "/s/All.sln",
            TargetFramework = "net10.0",
            ToolVersion = "1.2.3-test",
            RulesHash = "abcdef123456",
            ScanDate = scanDate,
            Projects =
            [
                new ProjectAssessment
                {
                    Analysis = lib,
                    PackageFindings = [new PackageFinding("PKG0001", "Newtonsoft.Json", "13.0.1",
                        PackageVerdict.Compatible, FindingSeverity.Info, null, null)],
                    ApiFindings = [],
                    Effort = new EffortScore { Score = 0, Band = EffortBand.S },
                },
                new ProjectAssessment
                {
                    Analysis = app,
                    PackageFindings =
                    [
                        new PackageFinding("PKG0002", "System.Data.SqlClient", "4.8.5", PackageVerdict.Replace,
                            FindingSeverity.Warning, "Microsoft.Data.SqlClient", "Namespace changes."),
                        new PackageFinding("PKG0003", "EntityFramework", "6.4.4", PackageVerdict.Partial,
                            FindingSeverity.Warning, "Microsoft.EntityFrameworkCore", null),
                    ],
                    ApiFindings =
                    [
                        new ApiFinding("API0101", "web", FindingSeverity.Blocker, "System.Web",
                            "Controllers/HomeController.cs", 2, true, "No modern equivalent; requires migration to ASP.NET Core."),
                        new ApiFinding("API0102", "web", FindingSeverity.Blocker, "System.Web.HttpContext.Current",
                            "Controllers/HomeController.cs", 12, true, null),
                    ],
                    Effort = new EffortScore { Score = 26, Band = EffortBand.M },
                },
            ],
            UpgradeOrder = ["Lib", "App"],
            Cycles = [],
            Diagnostics = [],
        };
    }
}
