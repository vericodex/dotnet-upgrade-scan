using UpgradeScan.Reporting;

namespace UpgradeScan.IntegrationTests;

public class DeterminismTests
{
    [Fact]
    public void DoubleRunRendersByteIdenticalReports()
    {
        var sln = Fixtures.Path("net472-two-proj", "All.sln");
        var first = PipelineHarness.Run(sln);
        var second = PipelineHarness.Run(sln);

        Assert.Equal(new MarkdownReportRenderer().Render(first), new MarkdownReportRenderer().Render(second));
        Assert.Equal(new JsonReportRenderer().Render(first), new JsonReportRenderer().Render(second));
    }

    [Fact]
    public void Mvc5WebMarkdownMatchesGolden() =>
        AssertGolden("mvc5-web.md", dir => new MarkdownReportRenderer().Render(
            PipelineHarness.Run(dir, rulesHashOverride: "constant00000")));

    [Fact]
    public void Mvc5WebJsonMatchesGolden() =>
        AssertGolden("mvc5-web.json", dir => new JsonReportRenderer().Render(
            PipelineHarness.Run(dir, rulesHashOverride: "constant00000")));

    private static void AssertGolden(string fileName, Func<string, string> render)
    {
        var fixtureDir = Fixtures.Path("mvc5-web");
        var rendered = Sanitize(render(fixtureDir), fixtureDir);
        var golden = Path.Combine(RepoRoot(), "tests", "UpgradeScan.IntegrationTests", "expected", fileName);
        if (!File.Exists(golden))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(golden)!);
            File.WriteAllText(golden, rendered);
            Assert.Fail("Golden file created on first run — inspect it, then re-run.");
        }
        Assert.Equal(File.ReadAllText(golden).Replace("\r\n", "\n"), rendered);
    }

    private static string Sanitize(string report, string fixtureDir) =>
        report
            .Replace(fixtureDir.Replace('\\', '/'), "<fixture>")
            .Replace(fixtureDir.Replace("\\", "\\\\"), "<fixture>")
            .Replace(fixtureDir, "<fixture>")
            .Replace("<fixture>\\\\", "<fixture>/")
            .Replace("<fixture>\\", "<fixture>/");

    private static string RepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !Directory.Exists(Path.Combine(dir, "tests", "fixtures")))
            dir = Path.GetDirectoryName(dir);
        return dir ?? throw new InvalidOperationException("repo root not found");
    }
}
