namespace UpgradeScan.Core.Model;

public sealed record ProjectAnalysis
{
    public required string Name { get; init; }
    public required string FullPath { get; init; }
    public required AnalysisTier Tier { get; init; }
    public required ProjectStyle Style { get; init; }
    public required string Language { get; init; }
    public required IReadOnlyList<string> TargetFrameworks { get; init; }
    public required IReadOnlyList<PackageRef> Packages { get; init; }
    public required IReadOnlyList<string> ProjectReferences { get; init; }
    public ProjectType Type { get; init; } = ProjectType.Unknown;
    public IReadOnlyList<ApiUsage> ApiUsages { get; init; } = [];
    public IReadOnlyList<ScanDiagnostic> Diagnostics { get; init; } = [];
}
