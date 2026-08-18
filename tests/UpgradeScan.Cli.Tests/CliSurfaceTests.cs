using System.Text.Json;
using UpgradeScan.Cli;

namespace UpgradeScan.Cli.Tests;

public class CliSurfaceTests
{
    [Fact]
    public void DefaultsAreMarkdownNet10Normal()
    {
        var parse = RootCommandFactory.Create().Parse(["some.sln"]);
        Assert.Empty(parse.Errors);
        Assert.Equal("markdown", parse.GetValue<string>("--format"));
        Assert.Equal("net10.0", parse.GetValue<string>("--target"));
        Assert.Equal("normal", parse.GetValue<string>("--verbosity"));
        Assert.False(parse.GetValue<bool>("--deterministic"));
    }

    [Fact]
    public void InvalidEnumValuesAreParseErrors()
    {
        Assert.NotEmpty(RootCommandFactory.Create().Parse(["p", "--format", "xml"]).Errors);
        Assert.NotEmpty(RootCommandFactory.Create().Parse(["p", "--verbosity", "loud"]).Errors);
        Assert.NotEmpty(RootCommandFactory.Create().Parse([]).Errors);
    }

    [Fact]
    public void RulesValidateShippedRulesExitsZero()
    {
        Assert.Equal(0, RootCommandFactory.Create().Parse(["rules", "validate"]).Invoke());
    }

    [Fact]
    public void RulesListExitsZero()
    {
        Assert.Equal(0, RootCommandFactory.Create().Parse(["rules", "list"]).Invoke());
    }

    [Fact]
    public void DeterministicJsonScanIsByteStableAndSchemaVersioned()
    {
        var sln = Fixtures.Path("net472-two-proj", "All.sln");
        var out1 = Path.Combine(Path.GetTempPath(), $"upscan-{Guid.NewGuid():N}-1.json");
        var out2 = Path.Combine(Path.GetTempPath(), $"upscan-{Guid.NewGuid():N}-2.json");
        try
        {
            string[] BaseArgs(string output) =>
                [sln, "--no-build", "--deterministic", "--format", "json", "--output", output];

            Assert.Equal(0, RootCommandFactory.Create().Parse(BaseArgs(out1)).Invoke());
            Assert.Equal(0, RootCommandFactory.Create().Parse(BaseArgs(out2)).Invoke());

            var json = File.ReadAllText(out1);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal(1, doc.RootElement.GetProperty("schemaVersion").GetInt32());
            Assert.False(doc.RootElement.TryGetProperty("scanDate", out _));
            Assert.Equal(File.ReadAllBytes(out1), File.ReadAllBytes(out2));
        }
        finally
        {
            File.Delete(out1);
            File.Delete(out2);
        }
    }

    [Fact]
    public void WritingTheReportToAFileConfirmsOnStdout()
    {
        var output = Path.Combine(Path.GetTempPath(), $"upscan-{Guid.NewGuid():N}.md");
        var stdout = CaptureStdout([Fixtures.Path("net472-two-proj", "All.sln"),
            "--no-build", "--deterministic", "--format", "markdown", "--output", output], output);

        Assert.Contains(output, stdout);
        Assert.Contains("github.com/vericodex/dotnet-upgrade-scan", stdout);
    }

    [Fact]
    public void ReportPipedToStdoutIsNotPollutedByTheConfirmation()
    {
        var stdout = CaptureStdout([Fixtures.Path("net472-two-proj", "All.sln"),
            "--no-build", "--deterministic", "--format", "markdown"], null);

        Assert.StartsWith("# Upgrade assessment", stdout);
        Assert.EndsWith("scan@vericodex.com\n", stdout.Replace("\r\n", "\n"));
    }

    private static string CaptureStdout(string[] args, string? outputToDelete)
    {
        var original = Console.Out;
        var captured = new StringWriter();
        try
        {
            Console.SetOut(captured);
            Assert.Equal(0, RootCommandFactory.Create().Parse(args).Invoke());
        }
        finally
        {
            Console.SetOut(original);
            if (outputToDelete is not null)
                File.Delete(outputToDelete);
        }
        return captured.ToString();
    }
}
