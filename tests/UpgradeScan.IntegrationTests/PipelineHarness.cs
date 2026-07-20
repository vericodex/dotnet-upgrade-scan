using UpgradeScan.Analysis;
using UpgradeScan.Core.Model;
using UpgradeScan.Core.Pipeline;
using UpgradeScan.Rules;

namespace UpgradeScan.IntegrationTests;

internal static class PipelineHarness
{
    internal static string RulesRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !Directory.Exists(Path.Combine(dir, "rules", "packages")))
            dir = Path.GetDirectoryName(dir);
        return Path.Combine(dir ?? throw new InvalidOperationException("rules/ not found"), "rules");
    }

    internal static AssessmentModel Run(string path, string? rulesHashOverride = null)
    {
        var rules = RulesLoader.Load(RulesRoot());
        var pipeline = new AssessmentPipeline(
            new SolutionLoader(),
            new TieredProjectAnalyzer([new SyntacticTierAnalyzer(), new ManifestAnalyzer()]),
            new RulesAssessor(rules, "net10.0"));
        return pipeline.Run(path,
            new AssessmentContext("net10.0", "0.0.0-test", rulesHashOverride ?? rules.Hash, ScanDate: null));
    }
}
