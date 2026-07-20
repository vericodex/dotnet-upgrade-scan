using UpgradeScan.Core.Model;

namespace UpgradeScan.Rules;

public static class EffortScorer
{
    public static EffortScore Score(
        ProjectAnalysis project,
        IReadOnlyList<PackageFinding> packageFindings,
        IReadOnlyList<ApiFinding> apiFindings,
        ScoringConfig scoring)
    {
        var score = 0;
        foreach (var group in apiFindings.Where(f => f.Severity == FindingSeverity.Blocker).GroupBy(f => f.Category, StringComparer.OrdinalIgnoreCase))
        {
            var weight = scoring.CategoryWeights.TryGetValue(group.Key, out var w)
                ? w
                : scoring.CategoryWeights["default"];
            score += group.Count() * weight;
        }
        foreach (var finding in packageFindings.Where(f => f.Verdict != PackageVerdict.Compatible))
            score += finding.ReplacementPackage is null ? scoring.NoReplacementWeight : scoring.WithReplacementWeight;

        var band = score < scoring.SmallMax ? EffortBand.S
            : score < scoring.MediumMax ? EffortBand.M
            : score < scoring.LargeMax ? EffortBand.L
            : EffortBand.XL;

        var floors = new List<string>();
        if (scoring.XlFloorTypes.Contains(project.Type))
        {
            if (band != EffortBand.XL)
                band = EffortBand.XL;
            floors.Add($"{project.Type} project type → XL floor");
        }
        if (scoring.VbOneSizeUp && project.Language == "VB" && band != EffortBand.XL)
        {
            band = band switch { EffortBand.S => EffortBand.M, EffortBand.M => EffortBand.L, _ => EffortBand.XL };
            floors.Add("VB project → one size up");
        }

        return new EffortScore { Score = score, Band = band, FloorsApplied = floors };
    }
}
