using Spectre.Console.Testing;
using UpgradeScan.Core.Model;
using UpgradeScan.Reporting;
using Xunit;

namespace UpgradeScan.Reporting.Tests;

public class ConsoleSummaryRendererTests
{
    [Fact]
    public void AssessmentRenderShowsTypeBlockersEffortAndBanner()
    {
        var console = new TestConsole { Profile = { Width = 200 } };
        new ConsoleSummaryRenderer().Render(SampleModel.Build(), console);

        Assert.Contains("AspNetMvc", console.Output);
        Assert.Contains("Effort:", console.Output);
        Assert.Contains("2 blocker finding(s)", console.Output);
        Assert.Contains("UPS0001", console.Output);
    }

    [Fact]
    public void CycleWarningIsRendered()
    {
        var console = new TestConsole();
        var model = SampleModel.Build() with { Cycles = [["A", "B"]] };
        new ConsoleSummaryRenderer().Render(model, console);

        Assert.Contains("A <-> B", console.Output);
        Assert.Contains("upgrade together", console.Output);
    }
}
