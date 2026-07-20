using UpgradeScan.Core.Abstractions;
using UpgradeScan.Core.Model;

namespace UpgradeScan.Core.Pipeline;

public sealed record AssessmentContext(
    string TargetFramework, string ToolVersion, string RulesHash, DateTimeOffset? ScanDate);

public sealed class AssessmentPipeline(
    ISolutionLoader loader, TieredProjectAnalyzer analyzer, IProjectAssessor assessor)
{
    private readonly ScanPipeline _scan = new(loader, analyzer);

    public AssessmentModel Run(string path, AssessmentContext context)
    {
        var solution = _scan.Run(path);
        var order = UpgradeOrderer.Order(solution);
        var rank = order.Order
            .Select((name, index) => (name, index))
            .ToDictionary(x => x.name, x => x.index, StringComparer.OrdinalIgnoreCase);

        var projects = solution.Projects
            .OrderBy(p => rank.TryGetValue(p.Name, out var r) ? r : int.MaxValue)
            .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .Select(assessor.Assess)
            .ToList();

        return new AssessmentModel
        {
            SolutionPath = solution.FullPath,
            TargetFramework = context.TargetFramework,
            ToolVersion = context.ToolVersion,
            RulesHash = context.RulesHash,
            ScanDate = context.ScanDate,
            Projects = projects,
            UpgradeOrder = order.Order,
            Cycles = order.Cycles,
            Diagnostics = order.Diagnostics,
        };
    }
}
