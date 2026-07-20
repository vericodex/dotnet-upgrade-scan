using UpgradeScan.Core.Model;
using Xunit;

namespace UpgradeScan.Analysis.Tests;

public class BuildalyzerTierAnalyzerTests
{
    private readonly BuildalyzerTierAnalyzer _analyzer = new();

    [Fact]
    public void SdkStyleProjectBuildsAndYieldsSemanticTier()
    {
        using var tmp = new TempDir();
        var proj = tmp.Write("Modern/Modern.csproj",
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);
        tmp.Write("Modern/Class1.cs", "namespace Modern; public class Class1;");
        Restore(proj);
        var diags = new List<ScanDiagnostic>();

        var result = _analyzer.TryAnalyze(proj, diags);

        if (result is null)
            return;

        Assert.Equal(AnalysisTier.Semantic, result.Tier);
        Assert.Equal(["net8.0"], result.TargetFrameworks);
        Assert.Equal(ProjectStyle.SdkStyle, result.Style);
    }

    [Fact]
    public void UnbuildableProjectDegradesWithDiagnosticInsteadOfThrowing()
    {
        using var tmp = new TempDir();
        var proj = tmp.Write("Broken/Broken.csproj",
            """
            <Project Sdk="Sdk.That.Does.Not.Exist/9.9.9">
              <PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup>
            </Project>
            """);
        var diags = new List<ScanDiagnostic>();

        var result = _analyzer.TryAnalyze(proj, diags);

        Assert.Null(result);
        Assert.Contains(diags, d => d.Code is DiagnosticCodes.BuildFailed or DiagnosticCodes.AnalyzerError);
    }

    private static void Restore(string projectPath)
    {
        var psi = new System.Diagnostics.ProcessStartInfo("dotnet", $"restore \"{projectPath}\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var process = System.Diagnostics.Process.Start(psi)!;
        process.WaitForExit();
        Assert.Equal(0, process.ExitCode);
    }
}
