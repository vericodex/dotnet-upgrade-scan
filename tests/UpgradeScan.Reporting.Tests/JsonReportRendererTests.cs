using System.Text.Json;
using UpgradeScan.Reporting;
using Xunit;

namespace UpgradeScan.Reporting.Tests;

public class JsonReportRendererTests
{
    [Fact]
    public void SchemaVersionCamelCaseEnumsAndOrdering()
    {
        var json = new JsonReportRenderer().Render(SampleModel.Build());
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.False(root.TryGetProperty("scanDate", out _));
        var projects = root.GetProperty("projects");
        Assert.Equal("Lib", projects[0].GetProperty("name").GetString());
        Assert.Equal("App", projects[1].GetProperty("name").GetString());
        Assert.Equal("legacy", projects[0].GetProperty("style").GetString());
        Assert.Equal("aspNetMvc", projects[1].GetProperty("type").GetString());
        Assert.Equal("replace",
            projects[1].GetProperty("packageFindings")[0].GetProperty("verdict").GetString());
        Assert.Equal("blocker",
            projects[1].GetProperty("apiFindings")[0].GetProperty("severity").GetString());
        Assert.DoesNotContain("apiUsages", json);
        Assert.DoesNotContain('\r', json);
    }

    [Fact]
    public void ScanDateAppearsOnlyWhenSet()
    {
        var json = new JsonReportRenderer().Render(
            SampleModel.Build(new DateTimeOffset(2026, 7, 18, 12, 0, 0, TimeSpan.Zero)));
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("scanDate", out _));
    }

    [Fact]
    public void MatchesCommittedGoldenFile()
    {
        var rendered = new JsonReportRenderer().Render(SampleModel.Build());
        var golden = Path.Combine(MarkdownReportRendererTests.RepoRoot(),
            "tests", "UpgradeScan.Reporting.Tests", "expected", "sample-report.json");
        if (!File.Exists(golden))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(golden)!);
            File.WriteAllText(golden, rendered);
            Assert.Fail("Golden file created on first run — inspect the schema, then re-run.");
        }
        Assert.Equal(File.ReadAllText(golden).Replace("\r\n", "\n"), rendered);
    }
}
