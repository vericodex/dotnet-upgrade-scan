using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UpgradeScan.Core.Model;

namespace UpgradeScan.Analysis;

public static class SemanticApiUsageCollector
{
    private static readonly SymbolDisplayFormat Fqn = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        memberOptions: SymbolDisplayMemberOptions.IncludeContainingType);

    public static IReadOnlyList<ApiUsage> Collect(Compilation compilation, string projectDir)
    {
        var usages = new List<ApiUsage>();
        foreach (var tree in compilation.SyntaxTrees)
        {
            if (string.IsNullOrEmpty(tree.FilePath))
                continue;
            var fullPath = Path.GetFullPath(tree.FilePath);
            var relative = Path.GetRelativePath(projectDir, fullPath);
            if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
                continue;
            if (ProjectFileFacts.IsInExcludedDir(projectDir, fullPath))
                continue;
            var file = relative.Replace('\\', '/');

            var model = compilation.GetSemanticModel(tree);
            foreach (var node in tree.GetRoot().DescendantNodes()
                         .Where(n => n is MemberAccessExpressionSyntax or QualifiedNameSyntax or SimpleNameSyntax))
            {
                if (node.Parent is MemberAccessExpressionSyntax or QualifiedNameSyntax)
                    continue;
                var info = model.GetSymbolInfo(node);
                var symbol = info.Symbol
                    ?? (info.CandidateReason == CandidateReason.OverloadResolutionFailure
                        ? info.CandidateSymbols.FirstOrDefault()
                        : null);
                if (symbol is null || symbol.ContainingAssembly is null)
                    continue;
                if (SymbolEqualityComparer.Default.Equals(symbol.ContainingAssembly, compilation.Assembly))
                    continue;
                if (symbol.Kind is not (SymbolKind.NamedType or SymbolKind.Method
                    or SymbolKind.Property or SymbolKind.Field or SymbolKind.Event))
                    continue;
                var line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                usages.Add(new ApiUsage(symbol.ToDisplayString(Fqn), file, line, Approximate: false));
            }
        }
        return [.. usages
            .DistinctBy(u => (u.Symbol, u.File, u.Line))
            .OrderBy(u => u.File, StringComparer.Ordinal)
            .ThenBy(u => u.Line)
            .ThenBy(u => u.Symbol, StringComparer.Ordinal)];
    }
}
