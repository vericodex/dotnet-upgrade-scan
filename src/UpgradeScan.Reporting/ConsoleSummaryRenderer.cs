using System.Globalization;
using Spectre.Console;
using UpgradeScan.Core.Model;

namespace UpgradeScan.Reporting;

public sealed class ConsoleSummaryRenderer
{
    public void Render(AssessmentModel model, IAnsiConsole console)
    {
        console.MarkupLine($"[bold]upgrade-scan[/] — {Markup.Escape(model.SolutionPath)}");
        console.MarkupLine(
            $"target {Markup.Escape(model.TargetFramework)} · rules {Markup.Escape(model.RulesHash)} · "
            + $"{model.Projects.Count.ToString(CultureInfo.InvariantCulture)} project(s)");

        if (model.Projects.Count > 0)
        {
            var table = new Table().Border(TableBorder.Rounded);
            table.AddColumns("Project", "Target framework", "Style", "Type", "Tier", "Blockers", "Packages", "Effort");
            foreach (var p in model.Projects)
            {
                var a = p.Analysis;
                table.AddRow(
                    Markup.Escape(a.Name),
                    Markup.Escape(a.TargetFrameworks.Count == 0 ? "?" : string.Join(";", a.TargetFrameworks)),
                    Markup.Escape(a.Style.ToString()),
                    Markup.Escape(a.Type.ToString()),
                    Markup.Escape(a.Tier.ToString()),
                    ReportFacts.Blockers(p).ToString(CultureInfo.InvariantCulture),
                    a.Packages.Count.ToString(CultureInfo.InvariantCulture),
                    Markup.Escape(p.Effort.Band.ToString()));
            }
            console.Write(table);

            var bands = model.Projects
                .GroupBy(p => p.Effort.Band)
                .OrderBy(g => g.Key)
                .Select(g => $"{g.Count().ToString(CultureInfo.InvariantCulture)}×{g.Key}");
            var totalBlockers = model.Projects.Sum(ReportFacts.Blockers);
            console.MarkupLine(
                $"[bold]Effort:[/] {Markup.Escape(string.Join("  ", bands))} · "
                + $"{totalBlockers.ToString(CultureInfo.InvariantCulture)} blocker finding(s)");
        }

        foreach (var cycle in model.Cycles)
            console.MarkupLine($"[yellow]cycle:[/] {Markup.Escape(string.Join(" <-> ", cycle))} — upgrade together");

        foreach (var diagnostic in model.Diagnostics
                     .Concat(model.Projects.SelectMany(p => p.Analysis.Diagnostics)))
        {
            var color = diagnostic.Severity == DiagnosticSeverity.Error ? "red" : "yellow";
            console.MarkupLine($"[{color}]{diagnostic.Code}[/] {Markup.Escape(diagnostic.Message)}");
        }
    }
}
