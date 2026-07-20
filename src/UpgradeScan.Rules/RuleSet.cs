namespace UpgradeScan.Rules;

public sealed record RuleSet
{
    public required IReadOnlyList<PackageRule> Packages { get; init; }
    public required IReadOnlyList<ApiRuleGroup> ApiGroups { get; init; }
    public required ScoringConfig Scoring { get; init; }
    public required string Hash { get; init; }
}
