using UpgradeScan.Core.Model;

namespace UpgradeScan.Rules;

public static class ApiRuleMatcher
{
    public static IReadOnlyList<ApiFinding> Match(IReadOnlyList<ApiUsage> usages, RuleSet rules)
    {
        var patterns = rules.ApiGroups
            .SelectMany(g => g.Patterns.Select(p => (Group: g, Pattern: p)))
            .OrderByDescending(x => x.Pattern.Match.Length)
            .ThenBy(x => x.Pattern.Id, StringComparer.Ordinal)
            .ToList();

        var findings = new List<ApiFinding>();
        foreach (var usage in usages)
        {
            var hit = patterns.FirstOrDefault(x => Matches(x.Pattern, usage.Symbol));
            if (hit.Pattern is null)
                continue;
            findings.Add(new ApiFinding(
                hit.Pattern.Id, hit.Group.Category, hit.Pattern.Severity,
                usage.Symbol, usage.File, usage.Line, usage.Approximate, hit.Pattern.Note));
        }
        return [.. findings
            .OrderBy(f => f.RuleId, StringComparer.Ordinal)
            .ThenBy(f => f.File, StringComparer.OrdinalIgnoreCase)
            .ThenBy(f => f.Line)];
    }

    private static bool Matches(ApiPattern pattern, string symbol) => pattern.Kind switch
    {
        ApiPatternKind.Namespace => symbol.Equals(pattern.Match, StringComparison.Ordinal)
            || symbol.StartsWith(pattern.Match + ".", StringComparison.Ordinal),
        ApiPatternKind.Type => symbol.Equals(pattern.Match, StringComparison.Ordinal)
            || symbol.StartsWith(pattern.Match + ".", StringComparison.Ordinal),
        ApiPatternKind.Member => symbol.Equals(pattern.Match, StringComparison.Ordinal),
        _ => false,
    };
}
