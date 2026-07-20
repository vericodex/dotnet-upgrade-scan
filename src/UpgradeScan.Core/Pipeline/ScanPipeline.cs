using UpgradeScan.Core.Abstractions;
using UpgradeScan.Core.Model;

namespace UpgradeScan.Core.Pipeline;

public sealed class ScanPipeline(ISolutionLoader loader, TieredProjectAnalyzer analyzer)
{
    public SolutionAnalysis Run(string path)
    {
        var projects = loader.FindProjects(path)
            .Select(analyzer.Analyze)
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(p => p.FullPath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new SolutionAnalysis
        {
            FullPath = Path.GetFullPath(path),
            Projects = projects,
        };
    }
}
