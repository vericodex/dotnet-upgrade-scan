using UpgradeScan.Core.Model;
using UpgradeScan.Rules;

namespace UpgradeScan.Rules.Tests;

public class RulesLoaderTests
{
    private static string WriteRulesDir(TempDir dir)
    {
        Directory.CreateDirectory(Path.Combine(dir.Path, "packages"));
        Directory.CreateDirectory(Path.Combine(dir.Path, "apis"));
        File.WriteAllText(Path.Combine(dir.Path, "packages", "system.data.sqlclient.yaml"), """
            id: PKG0002
            package: System.Data.SqlClient
            verdict: replace
            severity: warning
            replacement:
              package: Microsoft.Data.SqlClient
              notes: Namespace changes.
            targets:
              net8.0: replace
            source: manual
            fix:
              transform: PKG0002-swap-package
            links:
              - https://example.test/sqlclient
            """);
        File.WriteAllText(Path.Combine(dir.Path, "apis", "system-web.yaml"), """
            group: API0100
            technology: ASP.NET (System.Web)
            category: web
            patterns:
              - id: API0101
                kind: namespace
                match: System.Web
                severity: blocker
                note: Requires migration to ASP.NET Core.
            """);
        File.WriteAllText(Path.Combine(dir.Path, "scoring.yaml"), """
            categoryWeights:
              web: 10
              default: 3
            packageWeights:
              noReplacement: 15
              withReplacement: 3
            bands:
              s: 20
              m: 60
              l: 150
            floors:
              xlProjectTypes:
                - AspNetWebForms
                - WcfService
              vbOneSizeUp: true
            """);
        return dir.Path;
    }

    [Fact]
    public void LoadsAndMapsEverything()
    {
        using var dir = new TempDir();
        var rules = RulesLoader.Load(WriteRulesDir(dir));

        var pkg = Assert.Single(rules.Packages);
        Assert.Equal("PKG0002", pkg.Id);
        Assert.Equal(PackageVerdict.Replace, pkg.Verdict);
        Assert.Equal(FindingSeverity.Warning, pkg.Severity);
        Assert.Equal("Microsoft.Data.SqlClient", pkg.Replacement!.Package);
        Assert.Equal(PackageVerdict.Replace, pkg.TargetOverrides["net8.0"]);
        Assert.True(pkg.HasFix);

        var group = Assert.Single(rules.ApiGroups);
        Assert.Equal("API0100", group.GroupId);
        Assert.Equal("web", group.Category);
        var pattern = Assert.Single(group.Patterns);
        Assert.Equal(ApiPatternKind.Namespace, pattern.Kind);
        Assert.Equal(FindingSeverity.Blocker, pattern.Severity);

        Assert.Equal(10, rules.Scoring.CategoryWeights["web"]);
        Assert.Equal(15, rules.Scoring.NoReplacementWeight);
        Assert.Equal(3, rules.Scoring.WithReplacementWeight);
        Assert.Equal(20, rules.Scoring.SmallMax);
        Assert.Contains(ProjectType.WcfService, rules.Scoring.XlFloorTypes);
        Assert.True(rules.Scoring.VbOneSizeUp);
        Assert.Equal(12, rules.Hash.Length);
    }

    [Fact]
    public void HashIsStableAcrossLoadsAndChangesWithContent()
    {
        using var dir = new TempDir();
        WriteRulesDir(dir);
        var first = RulesLoader.Load(dir.Path).Hash;
        var second = RulesLoader.Load(dir.Path).Hash;
        Assert.Equal(first, second);

        File.AppendAllText(Path.Combine(dir.Path, "scoring.yaml"), "\n# comment\n");
        Assert.NotEqual(first, RulesLoader.Load(dir.Path).Hash);
    }

    [Fact]
    public void MissingScoringFileIsAHardError()
    {
        using var dir = new TempDir();
        WriteRulesDir(dir);
        File.Delete(Path.Combine(dir.Path, "scoring.yaml"));
        var ex = Assert.Throws<RulesLoadException>(() => RulesLoader.Load(dir.Path));
        Assert.Contains("scoring.yaml", ex.Message);
    }

    [Fact]
    public void InvalidVerdictNamesTheFile()
    {
        using var dir = new TempDir();
        WriteRulesDir(dir);
        File.WriteAllText(Path.Combine(dir.Path, "packages", "bad.yaml"), """
            id: PKG0099
            package: Bad.Package
            verdict: sideways
            severity: warning
            """);
        var ex = Assert.Throws<RulesLoadException>(() => RulesLoader.Load(dir.Path));
        Assert.Contains("bad.yaml", ex.Message);
        Assert.Contains("sideways", ex.Message);
    }

    [Fact]
    public void OverrideDirectoryReplacesById()
    {
        using var dir = new TempDir();
        using var extra = new TempDir();
        WriteRulesDir(dir);
        Directory.CreateDirectory(Path.Combine(extra.Path, "packages"));
        File.WriteAllText(Path.Combine(extra.Path, "packages", "system.data.sqlclient.yaml"), """
            id: PKG0002
            package: System.Data.SqlClient
            verdict: incompatible
            severity: blocker
            """);
        var rules = RulesLoader.Load(dir.Path, extra.Path);
        var pkg = Assert.Single(rules.Packages);
        Assert.Equal(PackageVerdict.Incompatible, pkg.Verdict);
    }
}
