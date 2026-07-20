using UpgradeScan.Rules;

namespace UpgradeScan.Rules.Tests;

public class RulesValidatorTests
{
    private static void WriteValid(TempDir dir)
    {
        Directory.CreateDirectory(Path.Combine(dir.Path, "packages"));
        Directory.CreateDirectory(Path.Combine(dir.Path, "apis"));
        File.WriteAllText(Path.Combine(dir.Path, "packages", "newtonsoft.json.yaml"), """
            id: PKG0001
            package: Newtonsoft.Json
            verdict: compatible
            severity: info
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
            """);
        File.WriteAllText(Path.Combine(dir.Path, "scoring.yaml"), """
            categoryWeights:
              default: 3
            packageWeights:
              noReplacement: 15
              withReplacement: 3
            bands:
              s: 20
              m: 60
              l: 150
            floors:
              xlProjectTypes: []
              vbOneSizeUp: true
            """);
    }

    [Fact]
    public void ValidDirectoryProducesNoErrors()
    {
        using var dir = new TempDir();
        WriteValid(dir);
        Assert.Empty(RulesValidator.Validate(dir.Path));
    }

    [Fact]
    public void ShippedRulesDirectoryIsValid()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !Directory.Exists(Path.Combine(dir, "rules", "packages")))
            dir = Path.GetDirectoryName(dir);
        Assert.NotNull(dir);
        Assert.Empty(RulesValidator.Validate(Path.Combine(dir!, "rules")));
    }

    [Fact]
    public void DuplicateIdIsAnError()
    {
        using var dir = new TempDir();
        WriteValid(dir);
        File.WriteAllText(Path.Combine(dir.Path, "packages", "other.package.yaml"), """
            id: PKG0001
            package: Other.Package
            verdict: compatible
            severity: info
            """);
        Assert.Contains(RulesValidator.Validate(dir.Path), e => e.Contains("duplicate", StringComparison.OrdinalIgnoreCase) && e.Contains("PKG0001"));
    }

    [Fact]
    public void ReplaceVerdictWithoutReplacementIsAnError()
    {
        using var dir = new TempDir();
        WriteValid(dir);
        File.WriteAllText(Path.Combine(dir.Path, "packages", "system.data.sqlclient.yaml"), """
            id: PKG0002
            package: System.Data.SqlClient
            verdict: replace
            severity: warning
            """);
        Assert.Contains(RulesValidator.Validate(dir.Path), e => e.Contains("replacement", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void WrongFilenameAndBadIdFormatAreErrors()
    {
        using var dir = new TempDir();
        WriteValid(dir);
        File.WriteAllText(Path.Combine(dir.Path, "packages", "misnamed.yaml"), """
            id: PKG12
            package: Some.Package
            verdict: compatible
            severity: info
            """);
        var errors = RulesValidator.Validate(dir.Path);
        Assert.Contains(errors, e => e.Contains("PKG12") && e.Contains("format", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, e => e.Contains("misnamed.yaml") && e.Contains("some.package.yaml"));
    }

    [Fact]
    public void UnparseableFileBecomesAnErrorNotAThrow()
    {
        using var dir = new TempDir();
        WriteValid(dir);
        File.WriteAllText(Path.Combine(dir.Path, "packages", "broken.yaml"), "id: [unclosed");
        Assert.Contains(RulesValidator.Validate(dir.Path), e => e.Contains("broken.yaml"));
    }
}
