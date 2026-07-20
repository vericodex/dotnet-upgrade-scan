using UpgradeScan.Core.Abstractions;
using UpgradeScan.Core.Model;

namespace UpgradeScan.Core.Pipeline;

public sealed class TieredProjectAnalyzer(IReadOnlyList<ITierAnalyzer> tiers)
{
    public ProjectAnalysis Analyze(string projectPath)
    {
        var diagnostics = new List<ScanDiagnostic>();
        foreach (var tier in tiers)
        {
            var result = tier.TryAnalyze(projectPath, diagnostics);
            if (result is null)
                continue;
            if (result.Language == "VB")
                diagnostics.Add(new ScanDiagnostic(DiagnosticCodes.VbReducedAnalysis, DiagnosticSeverity.Warning,
                    $"{result.Name}: VB project found — reduced analysis."));
            return result with { Diagnostics = [.. result.Diagnostics, .. diagnostics] };
        }

        var name = Path.GetFileNameWithoutExtension(projectPath);
        diagnostics.Add(new ScanDiagnostic(DiagnosticCodes.ProjectUnreadable, DiagnosticSeverity.Error,
            $"{name}: no analysis tier could read this project."));
        return new ProjectAnalysis
        {
            Name = name,
            FullPath = Path.GetFullPath(projectPath),
            Tier = AnalysisTier.Manifest,
            Style = ProjectStyle.Unknown,
            Language = "Unknown",
            TargetFrameworks = [],
            Packages = [],
            ProjectReferences = [],
            Diagnostics = diagnostics,
        };
    }
}
