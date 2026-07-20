using UpgradeScan.Core.Model;

namespace UpgradeScan.Core.Pipeline;

public sealed record UpgradeOrderResult(
    IReadOnlyList<string> Order,
    IReadOnlyList<IReadOnlyList<string>> Cycles,
    IReadOnlyList<ScanDiagnostic> Diagnostics);

public static class UpgradeOrderer
{
    public static UpgradeOrderResult Order(SolutionAnalysis solution)
    {
        var byPath = solution.Projects.ToDictionary(p => p.FullPath, StringComparer.OrdinalIgnoreCase);
        var dependencies = solution.Projects.ToDictionary(
            p => p.Name,
            p => p.ProjectReferences
                .Where(byPath.ContainsKey)
                .Select(r => byPath[r].Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToHashSet(StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);

        var order = new List<string>();
        var remaining = new HashSet<string>(dependencies.Keys, StringComparer.OrdinalIgnoreCase);
        while (remaining.Count > 0)
        {
            var ready = remaining
                .Where(name => dependencies[name].All(dep => !remaining.Contains(dep)))
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (ready.Count == 0)
                break;
            foreach (var name in ready)
            {
                order.Add(name);
                remaining.Remove(name);
            }
        }

        var cycles = FindCycles(remaining, dependencies);
        var diagnostics = new List<ScanDiagnostic>();
        foreach (var cycle in cycles)
        {
            foreach (var name in cycle)
                order.Add(name);
            diagnostics.Add(new ScanDiagnostic(DiagnosticCodes.DependencyCycle, DiagnosticSeverity.Warning,
                $"Dependency cycle: {string.Join(" -> ", cycle)} -> {cycle[0]}. These projects must be upgraded together."));
        }
        foreach (var name in remaining
                     .Where(n => !cycles.Any(c => c.Contains(n, StringComparer.OrdinalIgnoreCase)))
                     .OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
            order.Add(name);

        return new UpgradeOrderResult(order, cycles, diagnostics);
    }

    private static IReadOnlyList<IReadOnlyList<string>> FindCycles(
        IReadOnlySet<string> nodes, IReadOnlyDictionary<string, HashSet<string>> dependencies)
    {
        var cycles = new List<IReadOnlyList<string>>();
        var assigned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var start in nodes.OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
        {
            if (assigned.Contains(start))
                continue;
            var reachable = Reachable(start, nodes, dependencies);
            if (!reachable.Contains(start))
                continue;
            var group = reachable
                .Where(n => Reachable(n, nodes, dependencies).Contains(start) || n.Equals(start, StringComparison.OrdinalIgnoreCase))
                .Where(n => Reachable(start, nodes, dependencies).Contains(n))
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
            foreach (var member in group)
                assigned.Add(member);
            if (group.Count > 0)
                cycles.Add(group);
        }
        return cycles;
    }

    private static HashSet<string> Reachable(
        string from, IReadOnlySet<string> nodes, IReadOnlyDictionary<string, HashSet<string>> dependencies)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var stack = new Stack<string>(dependencies[from].Where(nodes.Contains));
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!seen.Add(current))
                continue;
            foreach (var next in dependencies[current].Where(nodes.Contains))
                stack.Push(next);
        }
        return seen;
    }
}
