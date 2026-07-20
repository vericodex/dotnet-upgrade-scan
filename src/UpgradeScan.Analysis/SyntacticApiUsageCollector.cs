using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UpgradeScan.Core.Model;

namespace UpgradeScan.Analysis;

public static class SyntacticApiUsageCollector
{
    public static IReadOnlyList<ApiUsage> Collect(string projectDir)
    {
        var usages = new List<ApiUsage>();
        foreach (var relative in ProjectFileFacts.EnumerateCSharpFiles(projectDir))
        {
            var text = File.ReadAllText(Path.Combine(projectDir, relative));
            var root = CSharpSyntaxTree.ParseText(text).GetRoot();

            foreach (var directive in root.DescendantNodes().OfType<UsingDirectiveSyntax>())
            {
                if (directive.Name is not null)
                    usages.Add(new ApiUsage(directive.Name.ToString(), relative, Line(directive), Approximate: true));
            }

            foreach (var node in root.DescendantNodes()
                         .Where(n => n is QualifiedNameSyntax or MemberAccessExpressionSyntax))
            {
                if (ShouldSkip(node))
                    continue;
                var chain = DottedChain(node);
                if (chain is not null)
                    usages.Add(new ApiUsage(chain, relative, Line(node), Approximate: true));
            }
        }
        return [.. usages
            .DistinctBy(u => (u.Symbol, u.File, u.Line))
            .OrderBy(u => u.File, StringComparer.Ordinal)
            .ThenBy(u => u.Line)
            .ThenBy(u => u.Symbol, StringComparer.Ordinal)];
    }

    private static int Line(SyntaxNode node) =>
        node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

    private static bool ShouldSkip(SyntaxNode node) =>
        (node.Parent is QualifiedNameSyntax q && q.Left == node)
        || (node.Parent is MemberAccessExpressionSyntax m && m.Expression == node)
        || (node.Parent is BaseNamespaceDeclarationSyntax ns && ns.Name == node)
        || node.Ancestors().OfType<UsingDirectiveSyntax>().Any();

    private static string? DottedChain(SyntaxNode node) => node switch
    {
        IdentifierNameSyntax id => id.Identifier.ValueText,
        QualifiedNameSyntax qn => DottedChain(qn.Left) is { } left && qn.Right is IdentifierNameSyntax r
            ? $"{left}.{r.Identifier.ValueText}"
            : null,
        MemberAccessExpressionSyntax ma when ma.IsKind(SyntaxKind.SimpleMemberAccessExpression) =>
            DottedChain(ma.Expression) is { } target && ma.Name is IdentifierNameSyntax name
                ? $"{target}.{name.Identifier.ValueText}"
                : null,
        _ => null,
    };
}
