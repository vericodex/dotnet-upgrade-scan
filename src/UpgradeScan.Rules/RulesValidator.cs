using System.Text.RegularExpressions;

namespace UpgradeScan.Rules;

public static partial class RulesValidator
{
    [GeneratedRegex(@"^PKG\d{4}$")]
    private static partial Regex PackageIdFormat();

    [GeneratedRegex(@"^API\d{4}$")]
    private static partial Regex ApiIdFormat();

    public static IReadOnlyList<string> Validate(string rulesDirectory)
    {
        var errors = new List<string>();
        if (!Directory.Exists(rulesDirectory))
            return [$"Rules directory not found: {rulesDirectory}"];
        if (!File.Exists(Path.Combine(rulesDirectory, "scoring.yaml")))
            errors.Add($"Missing required file: {Path.Combine(rulesDirectory, "scoring.yaml")}");
        else
            Try(errors, () => RulesLoader.ParseScoringFile(Path.Combine(rulesDirectory, "scoring.yaml")));

        var seenIds = new Dictionary<string, string>(StringComparer.Ordinal);
        void CheckId(string id, Regex format, string file)
        {
            if (!format.IsMatch(id))
                errors.Add($"{file}: id '{id}' does not match the required format ({format}).");
            if (!seenIds.TryAdd(id, file))
                errors.Add($"{file}: duplicate id '{id}' (first used in {seenIds[id]}).");
        }

        foreach (var file in SortedYaml(Path.Combine(rulesDirectory, "packages")))
        {
            var rule = Try(errors, () => RulesLoader.ParsePackageFile(file));
            if (rule is null)
                continue;
            CheckId(rule.Id, PackageIdFormat(), file);
            if (rule.Verdict == UpgradeScan.Core.Model.PackageVerdict.Replace && rule.Replacement is null)
                errors.Add($"{file}: verdict 'replace' requires a replacement block.");
            var expected = rule.Package.ToLowerInvariant() + ".yaml";
            if (!Path.GetFileName(file).Equals(expected, StringComparison.Ordinal))
                errors.Add($"{file}: filename must be '{expected}' (lowercase package id).");
        }

        foreach (var file in SortedYaml(Path.Combine(rulesDirectory, "apis")))
        {
            var group = Try(errors, () => RulesLoader.ParseApiFile(file));
            if (group is null)
                continue;
            CheckId(group.GroupId, ApiIdFormat(), file);
            foreach (var pattern in group.Patterns)
                CheckId(pattern.Id, ApiIdFormat(), file);
        }

        return errors;
    }

    private static IEnumerable<string> SortedYaml(string dir) =>
        Directory.Exists(dir)
            ? Directory.EnumerateFiles(dir, "*.yaml", SearchOption.AllDirectories)
                .OrderBy(f => f, StringComparer.Ordinal)
            : [];

    private static T? Try<T>(List<string> errors, Func<T> parse) where T : class
    {
        try
        {
            return parse();
        }
        catch (RulesLoadException ex)
        {
            errors.Add(ex.Message);
            return null;
        }
    }
}
