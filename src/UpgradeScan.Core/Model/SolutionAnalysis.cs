namespace UpgradeScan.Core.Model;

public sealed record SolutionAnalysis
{
    public required string FullPath { get; init; }
    public required IReadOnlyList<ProjectAnalysis> Projects { get; init; }
    public IReadOnlyList<ScanDiagnostic> Diagnostics { get; init; } = [];
}
