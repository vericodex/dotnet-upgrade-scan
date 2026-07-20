using UpgradeScan.Core.Model;

namespace UpgradeScan.Core.Abstractions;

public interface ITierAnalyzer
{
    ProjectAnalysis? TryAnalyze(string projectPath, ICollection<ScanDiagnostic> diagnostics);
}
