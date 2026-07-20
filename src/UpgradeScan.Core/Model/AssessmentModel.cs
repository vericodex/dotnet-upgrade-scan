namespace UpgradeScan.Core.Model;

public sealed record ProjectAssessment
{
    public required ProjectAnalysis Analysis { get; init; }
    public required IReadOnlyList<PackageFinding> PackageFindings { get; init; }
    public required IReadOnlyList<ApiFinding> ApiFindings { get; init; }
    public required EffortScore Effort { get; init; }
}

public sealed record AssessmentModel
{
    public const int SchemaVersion = 1;

    public required string SolutionPath { get; init; }
    public required string TargetFramework { get; init; }
    public required string ToolVersion { get; init; }
    public required string RulesHash { get; init; }
    public DateTimeOffset? ScanDate { get; init; }
    public required IReadOnlyList<ProjectAssessment> Projects { get; init; }
    public required IReadOnlyList<string> UpgradeOrder { get; init; }
    public required IReadOnlyList<IReadOnlyList<string>> Cycles { get; init; }
    public required IReadOnlyList<ScanDiagnostic> Diagnostics { get; init; }
}
