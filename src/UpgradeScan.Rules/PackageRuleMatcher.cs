using UpgradeScan.Core.Model;

namespace UpgradeScan.Rules;

public static class PackageRuleMatcher
{
    public static IReadOnlyList<PackageFinding> Match(
        IReadOnlyList<PackageRef> packages, string targetTfm, RuleSet rules)
    {
        var byPackageId = rules.Packages.ToDictionary(r => r.Package, StringComparer.OrdinalIgnoreCase);
        var findings = new List<PackageFinding>();
        foreach (var package in packages)
        {
            if (!byPackageId.TryGetValue(package.Id, out var rule))
                continue;
            var verdict = rule.TargetOverrides.TryGetValue(targetTfm, out var overridden)
                ? overridden
                : rule.Verdict;
            findings.Add(new PackageFinding(
                rule.Id, package.Id, package.Version, verdict, rule.Severity,
                rule.Replacement?.Package, rule.Replacement?.Notes));
        }
        return [.. findings
            .OrderBy(f => f.RuleId, StringComparer.Ordinal)
            .ThenBy(f => f.PackageId, StringComparer.OrdinalIgnoreCase)];
    }
}
