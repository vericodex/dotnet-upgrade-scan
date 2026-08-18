using System.Globalization;
using System.Text;
using UpgradeScan.Core.Model;

namespace UpgradeScan.Reporting;

public sealed class MarkdownReportRenderer
{
    public string Render(AssessmentModel model)
    {
        var sb = new StringBuilder();
        Header(sb, model);
        Summary(sb, model);
        UpgradeOrder(sb, model);
        foreach (var project in model.Projects)
            ProjectDetail(sb, project);
        EffortAppendix(sb);
        DiagnosticsAppendix(sb, model);
        Footer(sb, model);
        return sb.ToString();
    }

    private static void Header(StringBuilder sb, AssessmentModel model)
    {
        var solutionName = model.SolutionPath.Replace('\\', '/').Split('/')[^1];
        sb.Append("# Upgrade assessment — ").Append(solutionName).Append("\n\n");
        sb.Append("- Tool: [").Append(ReportBranding.ToolName).Append("](").Append(ReportBranding.ProjectUrl)
          .Append(") ").Append(model.ToolVersion).Append('\n');
        sb.Append("- Rules: ").Append(model.RulesHash).Append('\n');
        sb.Append("- Target: ").Append(model.TargetFramework).Append('\n');
        if (model.ScanDate is { } date)
            sb.Append("- Date: ").Append(date.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append('\n');
        sb.Append('\n');
    }

    private static void Summary(StringBuilder sb, AssessmentModel model)
    {
        sb.Append("## Summary\n\n");
        sb.Append("| Project | Target framework | Style | Type | Tier | Blockers | Packages | Effort |\n");
        sb.Append("|---|---|---|---|---|---:|---:|---|\n");
        foreach (var p in model.Projects)
        {
            var a = p.Analysis;
            sb.Append("| ").Append(Cell(a.Name))
              .Append(" | ").Append(Cell(a.TargetFrameworks.Count == 0 ? "?" : string.Join(";", a.TargetFrameworks)))
              .Append(" | ").Append(a.Style)
              .Append(" | ").Append(a.Type)
              .Append(" | ").Append(a.Tier)
              .Append(" | ").Append(ReportFacts.Blockers(p).ToString(CultureInfo.InvariantCulture))
              .Append(" | ").Append(a.Packages.Count.ToString(CultureInfo.InvariantCulture))
              .Append(" | ").Append(p.Effort.Band)
              .Append(" |\n");
        }
        sb.Append('\n');
    }

    private static void UpgradeOrder(StringBuilder sb, AssessmentModel model)
    {
        sb.Append("## Upgrade order\n\n");
        var position = 1;
        foreach (var name in model.UpgradeOrder)
            sb.Append((position++).ToString(CultureInfo.InvariantCulture)).Append(". ").Append(name).Append('\n');
        foreach (var cycle in model.Cycles)
            sb.Append("\n**Dependency cycle — upgrade together:** ").Append(string.Join(", ", cycle)).Append('\n');
        sb.Append('\n');

        var byPath = model.Projects.ToDictionary(
            p => p.Analysis.FullPath, p => p.Analysis.Name, StringComparer.OrdinalIgnoreCase);
        var edges = model.Projects
            .SelectMany(p => p.Analysis.ProjectReferences
                .Where(byPath.ContainsKey)
                .Select(r => (From: p.Analysis.Name, To: byPath[r])))
            .Distinct()
            .OrderBy(e => e.From, StringComparer.OrdinalIgnoreCase)
            .ThenBy(e => e.To, StringComparer.OrdinalIgnoreCase)
            .ToList();
        sb.Append("```mermaid\ngraph TD\n");
        foreach (var name in model.Projects.Select(p => p.Analysis.Name)
                     .Where(n => !edges.Any(e => e.From == n || e.To == n)))
            sb.Append("  ").Append(NodeId(name)).Append("[\"").Append(name).Append("\"]\n");
        foreach (var (from, to) in edges)
            sb.Append("  ").Append(NodeId(from)).Append("[\"").Append(from).Append("\"] --> ")
              .Append(NodeId(to)).Append("[\"").Append(to).Append("\"]\n");
        sb.Append("```\n\n");
    }

    private static void ProjectDetail(StringBuilder sb, ProjectAssessment p)
    {
        var a = p.Analysis;
        sb.Append("## ").Append(a.Name).Append("\n\n");
        sb.Append("Effort: ").Append(p.Effort.Band)
          .Append(" (score ").Append(p.Effort.Score.ToString(CultureInfo.InvariantCulture)).Append(')');
        if (p.Effort.FloorsApplied.Count > 0)
            sb.Append(" — floors: ").Append(string.Join("; ", p.Effort.FloorsApplied));
        sb.Append("\n\n");

        if (p.ApiFindings.Count > 0)
        {
            sb.Append("### API findings\n\n");
            foreach (var group in p.ApiFindings.GroupBy(f => f.Category, StringComparer.Ordinal)
                         .OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                sb.Append("**").Append(group.Key).Append("**\n\n");
                foreach (var f in group)
                {
                    sb.Append("- [").Append(f.RuleId).Append("] ")
                      .Append(f.Approximate ? "~ " : "").Append(f.Symbol)
                      .Append(" — ").Append(f.File).Append(':')
                      .Append(f.Line.ToString(CultureInfo.InvariantCulture));
                    if (f.Note is not null)
                        sb.Append(" — ").Append(f.Note);
                    sb.Append('\n');
                }
                sb.Append('\n');
            }
        }

        if (p.PackageFindings.Count > 0)
        {
            sb.Append("### Packages\n\n");
            sb.Append("| Package | Version | Verdict | Replacement | Rule |\n|---|---|---|---|---|\n");
            foreach (var f in p.PackageFindings)
            {
                sb.Append("| ").Append(Cell(f.PackageId))
                  .Append(" | ").Append(Cell(f.PackageVersion ?? "?"))
                  .Append(" | ").Append(f.Verdict)
                  .Append(" | ").Append(Cell(f.ReplacementPackage ?? "—"))
                  .Append(" | ").Append(f.RuleId)
                  .Append(" |\n");
            }
            sb.Append('\n');
        }

        if (p.ApiFindings.Count == 0 && p.PackageFindings.Count == 0)
            sb.Append("No findings.\n\n");
    }

    private static void EffortAppendix(StringBuilder sb) =>
        sb.Append("## Effort formula\n\n")
          .Append("```\nscore = Σ (blocker findings per category × category weight)\n")
          .Append("      + incompatible packages with no known replacement × noReplacement weight\n")
          .Append("      + incompatible packages with a known replacement × withReplacement weight\n")
          .Append("floors: configured project types → XL; VB → one size up\n```\n\n")
          .Append("Weights and bands live in rules/scoring.yaml; the rules hash above pins them.\n\n");

    private static void DiagnosticsAppendix(StringBuilder sb, AssessmentModel model)
    {
        sb.Append("## Diagnostics\n\n");
        var any = false;
        foreach (var d in model.Diagnostics)
        {
            sb.Append("- ").Append(d.Code).Append(' ').Append(d.Message).Append('\n');
            any = true;
        }
        foreach (var p in model.Projects)
            foreach (var d in p.Analysis.Diagnostics)
            {
                sb.Append("- ").Append(d.Code).Append(' ').Append(d.Message).Append('\n');
                any = true;
            }
        sb.Append(any ? "\n" : "None.\n\n");
    }

    private static void Footer(StringBuilder sb, AssessmentModel model)
    {
        sb.Append("---\n\n");
        sb.Append("Generated by [").Append(ReportBranding.ToolName).Append("](").Append(ReportBranding.ProjectUrl)
          .Append(") ").Append(model.ToolVersion)
          .Append(" — free, open source, and fully offline: no telemetry, no network calls, ")
          .Append("nothing leaves the machine it runs on.\n\n");
        sb.Append("Effort bands are heuristic. They describe the shape of the problem, not the cost of solving it — ")
          .Append("a blocker count is not a schedule, and this report does not estimate duration, team size, ")
          .Append("or budget.\n\n");
        sb.Append("For a costed, sequenced migration plan — blockers triaged, risks named, and a person accountable ")
          .Append("for the number — talk to ").Append(ReportBranding.Company).Append(": ")
          .Append(ReportBranding.ContactEmail).Append('\n');
    }

    private static string Cell(string value) => value.Replace("|", "\\|");

    private static string NodeId(string name) =>
        new([.. name.Select(c => char.IsLetterOrDigit(c) ? c : '_')]);
}
