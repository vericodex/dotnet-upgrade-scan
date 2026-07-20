namespace UpgradeScan.Core.Model;

public sealed record PackageFinding(
    string RuleId,
    string PackageId,
    string? PackageVersion,
    PackageVerdict Verdict,
    FindingSeverity Severity,
    string? ReplacementPackage,
    string? Notes);

public sealed record ApiFinding(
    string RuleId,
    string Category,
    FindingSeverity Severity,
    string Symbol,
    string File,
    int Line,
    bool Approximate,
    string? Note);

public sealed record EffortScore
{
    public required int Score { get; init; }
    public required EffortBand Band { get; init; }
    public IReadOnlyList<string> FloorsApplied { get; init; } = [];
}
