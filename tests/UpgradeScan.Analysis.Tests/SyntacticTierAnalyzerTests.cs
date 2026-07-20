using UpgradeScan.Core.Model;
using Xunit;

namespace UpgradeScan.Analysis.Tests;

public class SyntacticTierAnalyzerTests
{
    private const string Csproj = """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net48</TargetFramework>
          </PropertyGroup>
        </Project>
        """;

    private static readonly string LegacySource = """
        using System.Web;

        namespace Web.Handlers
        {
            public class Legacy
            {
                public object? Who() => System.Web.HttpContext.Current.User;
            }
        }
        """;

    [Fact]
    public void CollectsUsingsAndDottedChainsAsApproximate()
    {
        using var dir = new TempDir();
        var csproj = dir.Write("Web.csproj", Csproj);
        dir.Write("Handlers/Legacy.cs", LegacySource);

        var diagnostics = new List<ScanDiagnostic>();
        var result = new SyntacticTierAnalyzer().TryAnalyze(csproj, diagnostics);

        Assert.NotNull(result);
        Assert.Equal(AnalysisTier.Syntactic, result!.Tier);
        Assert.All(result.ApiUsages, u => Assert.True(u.Approximate));
        Assert.Contains(result.ApiUsages, u => u.Symbol == "System.Web" && u.Line == 1);
        Assert.Contains(result.ApiUsages, u => u.Symbol == "System.Web.HttpContext.Current.User");
        Assert.All(result.ApiUsages, u => Assert.Equal("Handlers/Legacy.cs", u.File));
    }

    [Fact]
    public void VbProjectReturnsNull()
    {
        using var dir = new TempDir();
        var vbproj = dir.Write("Old.vbproj", "<Project></Project>");
        dir.Write("Class1.vb", "Class Class1\nEnd Class");
        Assert.Null(new SyntacticTierAnalyzer().TryAnalyze(vbproj, []));
    }

    [Fact]
    public void NoCSharpSourcesReturnsNull()
    {
        using var dir = new TempDir();
        var csproj = dir.Write("Empty.csproj", Csproj);
        Assert.Null(new SyntacticTierAnalyzer().TryAnalyze(csproj, []));
    }

    [Fact]
    public void BinAndObjSourcesAreIgnored()
    {
        using var dir = new TempDir();
        var csproj = dir.Write("Web.csproj", Csproj);
        dir.Write("Handlers/Legacy.cs", LegacySource);
        dir.Write("obj/Generated.cs", "using System.Runtime.Remoting;");

        var result = new SyntacticTierAnalyzer().TryAnalyze(csproj, []);

        Assert.DoesNotContain(result!.ApiUsages, u => u.Symbol.Contains("Remoting"));
    }
}
