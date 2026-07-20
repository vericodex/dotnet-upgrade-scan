using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using UpgradeScan.Core.Model;
using UpgradeScan.Core.Pipeline;
using Xunit;

namespace UpgradeScan.Analysis.Tests;

public class SemanticApiUsageCollectorTests
{
    private static (Compilation Compilation, string Dir) Compile(TempDir dir, string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source, path: Path.Combine(dir.Path, "C.cs"));
        var compilation = CSharpCompilation.Create("App", [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        return (compilation, dir.Path);
    }

    [Fact]
    public void RecordsExternalMemberWithExactFullyQualifiedName()
    {
        using var dir = new TempDir();
        var (compilation, root) = Compile(dir, """
            using System.Text;

            namespace App
            {
                public class C
                {
                    public string M() => Encoding.UTF8.WebName;
                }
            }
            """);

        var usages = SemanticApiUsageCollector.Collect(compilation, root);

        Assert.Contains(usages, u => u.Symbol == "System.Text.Encoding.WebName" && !u.Approximate);
        Assert.All(usages, u => Assert.Equal("C.cs", u.File));
        Assert.All(usages, u => Assert.False(u.Approximate));
    }

    [Fact]
    public void OwnAssemblySymbolsAreNotRecorded()
    {
        using var dir = new TempDir();
        var (compilation, root) = Compile(dir, """
            namespace App
            {
                public class D { public void X() { } }
                public class C
                {
                    public void M() { var d = new D(); d.X(); }
                }
            }
            """);

        Assert.DoesNotContain(SemanticApiUsageCollector.Collect(compilation, root),
            u => u.Symbol.StartsWith("App.", StringComparison.Ordinal));
    }

    [Fact]
    public void FixtureAppRecordsExternalUsagesWhenSemanticTierSucceeds()
    {
        var analyzer = new TieredProjectAnalyzer(
            [new BuildalyzerTierAnalyzer(), new SyntacticTierAnalyzer(), new ManifestAnalyzer()]);
        var result = analyzer.Analyze(Fixtures.Path("net472-two-proj", "App", "App.csproj"));

        if (result.Tier != AnalysisTier.Semantic)
            return;

        Assert.Contains(result.ApiUsages, u => u.Symbol == "System.Console.WriteLine" && !u.Approximate);
        Assert.All(result.ApiUsages, u => Assert.DoesNotContain('\\', u.File));
    }
}
