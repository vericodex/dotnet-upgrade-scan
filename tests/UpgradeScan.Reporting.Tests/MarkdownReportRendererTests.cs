using UpgradeScan.Reporting;
using Xunit;

namespace UpgradeScan.Reporting.Tests;

public class MarkdownReportRendererTests
{
    internal static string RepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !Directory.Exists(Path.Combine(dir, "tests", "fixtures")))
            dir = Path.GetDirectoryName(dir);
        return dir ?? throw new InvalidOperationException("repo root not found");
    }

    [Fact]
    public void StructureAndDeterministicDateHandling()
    {
        var withoutDate = new MarkdownReportRenderer().Render(SampleModel.Build());
        Assert.StartsWith("# Upgrade assessment — All.sln", withoutDate);
        Assert.DoesNotContain("- Date:", withoutDate);
        Assert.DoesNotContain("\r", withoutDate);
        Assert.Contains("1. Lib", withoutDate);
        Assert.Contains("2. App", withoutDate);
        Assert.Contains("```mermaid", withoutDate);
        Assert.Contains("~ System.Web", withoutDate);
        Assert.Contains("Microsoft.Data.SqlClient", withoutDate);
        Assert.Contains("UPS0001", withoutDate);

        var withDate = new MarkdownReportRenderer().Render(
            SampleModel.Build(new DateTimeOffset(2026, 7, 18, 12, 0, 0, TimeSpan.Zero)));
        Assert.Contains("- Date: 2026-07-18", withDate);
    }

    [Fact]
    public void MatchesCommittedGoldenFile()
    {
        var rendered = new MarkdownReportRenderer().Render(SampleModel.Build());
        var golden = Path.Combine(RepoRoot(), "tests", "UpgradeScan.Reporting.Tests", "expected", "sample-report.md");
        if (!File.Exists(golden))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(golden)!);
            File.WriteAllText(golden, rendered);
            Assert.Fail("Golden file created on first run — inspect the layout, then re-run.");
        }
        Assert.Equal(File.ReadAllText(golden).Replace("\r\n", "\n"), rendered);
    }
}
