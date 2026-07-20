using UpgradeScan.Core.Model;
using Xunit;

namespace UpgradeScan.Analysis.Tests;

public class ManifestAnalyzerTests
{
    private readonly ManifestAnalyzer _analyzer = new();

    private const string LegacyCsproj =
        """
        <?xml version="1.0" encoding="utf-8"?>
        <Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
          <PropertyGroup>
            <OutputType>Exe</OutputType>
            <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
          </PropertyGroup>
          <ItemGroup>
            <ProjectReference Include="..\Lib\Lib.csproj">
              <Name>Lib</Name>
            </ProjectReference>
          </ItemGroup>
        </Project>
        """;

    [Fact]
    public void LegacyProjectYieldsMappedTfmStyleAndReferences()
    {
        using var tmp = new TempDir();
        var proj = tmp.Write("App/App.csproj", LegacyCsproj);
        tmp.Write("App/packages.config",
            """
            <packages>
              <package id="Newtonsoft.Json" version="13.0.1" />
            </packages>
            """);
        var diags = new List<ScanDiagnostic>();

        var result = _analyzer.TryAnalyze(proj, diags);

        Assert.NotNull(result);
        Assert.Equal("App", result.Name);
        Assert.Equal(AnalysisTier.Manifest, result.Tier);
        Assert.Equal(ProjectStyle.Legacy, result.Style);
        Assert.Equal("C#", result.Language);
        Assert.Equal(["net472"], result.TargetFrameworks);
        Assert.Equal("Newtonsoft.Json", Assert.Single(result.Packages).Id);
        Assert.EndsWith("Lib.csproj", Assert.Single(result.ProjectReferences));
        Assert.True(Path.IsPathRooted(result.ProjectReferences[0]));
        Assert.Empty(diags);
    }

    [Fact]
    public void SdkStyleProjectYieldsTfmAndPackageReferences()
    {
        using var tmp = new TempDir();
        var proj = tmp.Write("Lib/Lib.csproj",
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFrameworks>net472;netstandard2.0</TargetFrameworks>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Serilog" Version="2.12.0" />
              </ItemGroup>
            </Project>
            """);
        var diags = new List<ScanDiagnostic>();

        var result = _analyzer.TryAnalyze(proj, diags);

        Assert.NotNull(result);
        Assert.Equal(ProjectStyle.SdkStyle, result.Style);
        Assert.Equal(["net472", "netstandard2.0"], result.TargetFrameworks);
        var pkg = Assert.Single(result.Packages);
        Assert.Equal("Serilog", pkg.Id);
        Assert.Equal("2.12.0", pkg.Version);
    }

    [Fact]
    public void VbProjectIsDetectedByExtension()
    {
        using var tmp = new TempDir();
        var proj = tmp.Write("VbLib/VbLib.vbproj",
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net48</TargetFramework></PropertyGroup>
            </Project>
            """);

        var result = _analyzer.TryAnalyze(proj, []);

        Assert.NotNull(result);
        Assert.Equal("VB", result.Language);
    }

    [Fact]
    public void UnparsableProjectDegradesWithDiagnostic()
    {
        using var tmp = new TempDir();
        var proj = tmp.Write("Bad/Bad.csproj", "not xml at all <<<");
        var diags = new List<ScanDiagnostic>();

        var result = _analyzer.TryAnalyze(proj, diags);

        Assert.Null(result);
        var d = Assert.Single(diags);
        Assert.Equal(DiagnosticCodes.ProjectUnreadable, d.Code);
        Assert.Equal(DiagnosticSeverity.Error, d.Severity);
    }
}
