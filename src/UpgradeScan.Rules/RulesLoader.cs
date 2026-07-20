using System.Security.Cryptography;
using System.Text;
using UpgradeScan.Core.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace UpgradeScan.Rules;

public sealed class RulesLoadException(string message) : Exception(message);

public static class RulesLoader
{
    public static RuleSet Load(string rulesDirectory, string? overrideDirectory = null)
    {
        if (!Directory.Exists(rulesDirectory))
            throw new RulesLoadException($"Rules directory not found: {rulesDirectory}");

        var scoringPath = Path.Combine(rulesDirectory, "scoring.yaml");
        if (!File.Exists(scoringPath))
            throw new RulesLoadException($"Missing required file: {scoringPath} (scoring.yaml)");

        var packages = LoadPackageDir(Path.Combine(rulesDirectory, "packages"))
            .ToDictionary(p => p.Id, StringComparer.Ordinal);
        var apiGroups = LoadApiDir(Path.Combine(rulesDirectory, "apis"));
        var hashFiles = EnumerateYaml(rulesDirectory).ToList();

        if (overrideDirectory is not null)
        {
            if (!Directory.Exists(overrideDirectory))
                throw new RulesLoadException($"Rules override directory not found: {overrideDirectory}");
            foreach (var rule in LoadPackageDir(Path.Combine(overrideDirectory, "packages")))
                packages[rule.Id] = rule;
            var overrideGroups = LoadApiDir(Path.Combine(overrideDirectory, "apis"));
            var byGroup = apiGroups.ToDictionary(g => g.GroupId, StringComparer.Ordinal);
            foreach (var group in overrideGroups)
                byGroup[group.GroupId] = group;
            apiGroups = [.. byGroup.Values];
            hashFiles.AddRange(EnumerateYaml(overrideDirectory));
        }

        return new RuleSet
        {
            Packages = [.. packages.Values.OrderBy(p => p.Id, StringComparer.Ordinal)],
            ApiGroups = [.. apiGroups.OrderBy(g => g.GroupId, StringComparer.Ordinal)],
            Scoring = ParseScoringFile(scoringPath),
            Hash = ComputeHash(hashFiles),
        };
    }

    private static IEnumerable<string> EnumerateYaml(string root) =>
        Directory.EnumerateFiles(root, "*.yaml", SearchOption.AllDirectories)
            .OrderBy(f => Path.GetRelativePath(root, f).Replace('\\', '/'), StringComparer.Ordinal);

    private static List<PackageRule> LoadPackageDir(string dir) =>
        Directory.Exists(dir)
            ? [.. EnumerateYaml(dir).Select(ParsePackageFile)]
            : [];

    private static List<ApiRuleGroup> LoadApiDir(string dir) =>
        Directory.Exists(dir)
            ? [.. EnumerateYaml(dir).Select(ParseApiFile)]
            : [];

    internal static PackageRule ParsePackageFile(string path)
    {
        var dto = Deserialize<PackageRuleDto>(path);
        return new PackageRule
        {
            Id = Require(dto.Id, "id", path),
            Package = Require(dto.Package, "package", path),
            Verdict = ParseVerdict(Require(dto.Verdict, "verdict", path), path),
            Severity = ParseSeverity(Require(dto.Severity, "severity", path), path),
            Replacement = dto.Replacement is null
                ? null
                : new PackageReplacement(Require(dto.Replacement.Package, "replacement.package", path), dto.Replacement.Notes),
            TargetOverrides = (dto.Targets ?? []).ToDictionary(
                kvp => kvp.Key, kvp => ParseVerdict(kvp.Value, path), StringComparer.OrdinalIgnoreCase),
            Source = dto.Source,
            Links = dto.Links ?? [],
            HasFix = dto.Fix is not null,
        };
    }

    internal static ApiRuleGroup ParseApiFile(string path)
    {
        var dto = Deserialize<ApiGroupDto>(path);
        if (dto.Patterns is null || dto.Patterns.Count == 0)
            throw new RulesLoadException($"{path}: an API group needs at least one pattern.");
        return new ApiRuleGroup
        {
            GroupId = Require(dto.Group, "group", path),
            Technology = Require(dto.Technology, "technology", path),
            Category = Require(dto.Category, "category", path),
            Patterns = [.. dto.Patterns.Select(p => new ApiPattern
            {
                Id = Require(p.Id, "patterns[].id", path),
                Kind = ParseKind(Require(p.Kind, "patterns[].kind", path), path),
                Match = Require(p.Match, "patterns[].match", path),
                Severity = ParseSeverity(Require(p.Severity, "patterns[].severity", path), path),
                Note = p.Note,
            })],
        };
    }

    internal static ScoringConfig ParseScoringFile(string path)
    {
        var dto = Deserialize<ScoringDto>(path);
        var weights = dto.CategoryWeights
            ?? throw new RulesLoadException($"{path}: categoryWeights is required.");
        if (!weights.ContainsKey("default"))
            throw new RulesLoadException($"{path}: categoryWeights must contain a 'default' key.");
        var packageWeights = dto.PackageWeights
            ?? throw new RulesLoadException($"{path}: packageWeights is required.");
        var bands = dto.Bands
            ?? throw new RulesLoadException($"{path}: bands is required.");
        var floors = dto.Floors
            ?? throw new RulesLoadException($"{path}: floors is required.");
        return new ScoringConfig
        {
            CategoryWeights = new Dictionary<string, int>(weights, StringComparer.OrdinalIgnoreCase),
            NoReplacementWeight = packageWeights.NoReplacement,
            WithReplacementWeight = packageWeights.WithReplacement,
            SmallMax = bands.S,
            MediumMax = bands.M,
            LargeMax = bands.L,
            XlFloorTypes = [.. (floors.XlProjectTypes ?? []).Select(t =>
                Enum.TryParse<ProjectType>(t, ignoreCase: true, out var parsed)
                    ? parsed
                    : throw new RulesLoadException($"{path}: unknown project type '{t}' in floors.xlProjectTypes."))],
            VbOneSizeUp = floors.VbOneSizeUp,
        };
    }

    private static T Deserialize<T>(string path)
    {
        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            return deserializer.Deserialize<T>(File.ReadAllText(path))
                ?? throw new RulesLoadException($"{path}: file is empty.");
        }
        catch (RulesLoadException) { throw; }
        catch (Exception ex)
        {
            throw new RulesLoadException($"{path}: {ex.Message}");
        }
    }

    private static string Require(string? value, string field, string path) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new RulesLoadException($"{path}: '{field}' is required.")
            : value;

    private static PackageVerdict ParseVerdict(string value, string path) => value.ToLowerInvariant() switch
    {
        "compatible" => PackageVerdict.Compatible,
        "replace" => PackageVerdict.Replace,
        "partial" => PackageVerdict.Partial,
        "incompatible" => PackageVerdict.Incompatible,
        "deprecated" => PackageVerdict.Deprecated,
        _ => throw new RulesLoadException($"{path}: unknown verdict '{value}'."),
    };

    private static FindingSeverity ParseSeverity(string value, string path) => value.ToLowerInvariant() switch
    {
        "info" => FindingSeverity.Info,
        "warning" => FindingSeverity.Warning,
        "blocker" => FindingSeverity.Blocker,
        _ => throw new RulesLoadException($"{path}: unknown severity '{value}'."),
    };

    private static ApiPatternKind ParseKind(string value, string path) => value.ToLowerInvariant() switch
    {
        "namespace" => ApiPatternKind.Namespace,
        "type" => ApiPatternKind.Type,
        "member" => ApiPatternKind.Member,
        _ => throw new RulesLoadException($"{path}: unknown pattern kind '{value}'."),
    };

    private static string ComputeHash(IReadOnlyList<string> files)
    {
        using var sha = SHA256.Create();
        foreach (var file in files)
        {
            var nameBytes = Encoding.UTF8.GetBytes(Path.GetFileName(file));
            sha.TransformBlock(nameBytes, 0, nameBytes.Length, null, 0);
            var content = File.ReadAllBytes(file);
            sha.TransformBlock(content, 0, content.Length, null, 0);
        }
        sha.TransformFinalBlock([], 0, 0);
        return Convert.ToHexString(sha.Hash!)[..12].ToLowerInvariant();
    }

    private sealed class PackageRuleDto
    {
        public string? Id { get; set; }
        public string? Package { get; set; }
        public string? Verdict { get; set; }
        public string? Severity { get; set; }
        public ReplacementDto? Replacement { get; set; }
        public Dictionary<string, string>? Targets { get; set; }
        public string? Source { get; set; }
        public FixDto? Fix { get; set; }
        public List<string>? Links { get; set; }
    }

    private sealed class ReplacementDto
    {
        public string? Package { get; set; }
        public string? Notes { get; set; }
    }

    private sealed class FixDto
    {
        public string? Transform { get; set; }
    }

    private sealed class ApiGroupDto
    {
        public string? Group { get; set; }
        public string? Technology { get; set; }
        public string? Category { get; set; }
        public List<ApiPatternDto>? Patterns { get; set; }
    }

    private sealed class ApiPatternDto
    {
        public string? Id { get; set; }
        public string? Kind { get; set; }
        public string? Match { get; set; }
        public string? Severity { get; set; }
        public string? Note { get; set; }
    }

    private sealed class ScoringDto
    {
        public Dictionary<string, int>? CategoryWeights { get; set; }
        public PackageWeightsDto? PackageWeights { get; set; }
        public BandsDto? Bands { get; set; }
        public FloorsDto? Floors { get; set; }
    }

    private sealed class PackageWeightsDto
    {
        public int NoReplacement { get; set; }
        public int WithReplacement { get; set; }
    }

    private sealed class BandsDto
    {
        public int S { get; set; }
        public int M { get; set; }
        public int L { get; set; }
    }

    private sealed class FloorsDto
    {
        public List<string>? XlProjectTypes { get; set; }
        public bool VbOneSizeUp { get; set; }
    }
}
