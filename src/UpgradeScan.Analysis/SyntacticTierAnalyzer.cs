using UpgradeScan.Core.Abstractions;
using UpgradeScan.Core.Model;

namespace UpgradeScan.Analysis;

public sealed class SyntacticTierAnalyzer : ITierAnalyzer
{
    private readonly ManifestAnalyzer _manifest = new();

    public ProjectAnalysis? TryAnalyze(string projectPath, ICollection<ScanDiagnostic> diagnostics)
    {
        var fullPath = Path.GetFullPath(projectPath);
        if (ProjectFileFacts.LanguageFromPath(fullPath) != "C#")
            return null;

        var manifest = _manifest.TryAnalyze(fullPath, diagnostics);
        if (manifest is null)
            return null;

        var dir = Path.GetDirectoryName(fullPath)!;
        if (ProjectFileFacts.EnumerateCSharpFiles(dir).Count == 0)
            return null;

        try
        {
            return manifest with
            {
                Tier = AnalysisTier.Syntactic,
                ApiUsages = SyntacticApiUsageCollector.Collect(dir),
            };
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            diagnostics.Add(new ScanDiagnostic(DiagnosticCodes.AnalyzerError, DiagnosticSeverity.Warning,
                $"{manifest.Name}: {ex.GetType().Name} during syntax analysis; degrading. ({ex.Message})"));
            return null;
        }
    }
}
