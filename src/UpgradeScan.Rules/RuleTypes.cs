using UpgradeScan.Core.Model;

namespace UpgradeScan.Rules;

public sealed record PackageReplacement(string Package, string? Notes);

public sealed record PackageRule
{
    public required string Id { get; init; }
    public required string Package { get; init; }
    public required PackageVerdict Verdict { get; init; }
    public required FindingSeverity Severity { get; init; }
    public PackageReplacement? Replacement { get; init; }
    public IReadOnlyDictionary<string, PackageVerdict> TargetOverrides { get; init; } =
        new Dictionary<string, PackageVerdict>();
    public string? Source { get; init; }
    public IReadOnlyList<string> Links { get; init; } = [];
    public bool HasFix { get; init; }
}

public enum ApiPatternKind { Namespace, Type, Member }

public sealed record ApiPattern
{
    public required string Id { get; init; }
    public required ApiPatternKind Kind { get; init; }
    public required string Match { get; init; }
    public required FindingSeverity Severity { get; init; }
    public string? Note { get; init; }
}

public sealed record ApiRuleGroup
{
    public required string GroupId { get; init; }
    public required string Technology { get; init; }
    public required string Category { get; init; }
    public required IReadOnlyList<ApiPattern> Patterns { get; init; }
}

public sealed record ScoringConfig
{
    public required IReadOnlyDictionary<string, int> CategoryWeights { get; init; }
    public required int NoReplacementWeight { get; init; }
    public required int WithReplacementWeight { get; init; }
    public required int SmallMax { get; init; }
    public required int MediumMax { get; init; }
    public required int LargeMax { get; init; }
    public required IReadOnlyList<ProjectType> XlFloorTypes { get; init; }
    public required bool VbOneSizeUp { get; init; }
}
