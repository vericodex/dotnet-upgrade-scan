using Buildalyzer;
using Buildalyzer.Environment;
using Buildalyzer.Workspaces;
using UpgradeScan.Core.Abstractions;
using UpgradeScan.Core.Model;

namespace UpgradeScan.Analysis;

public sealed class BuildalyzerTierAnalyzer : ITierAnalyzer
{
    private readonly AnalyzerManager _manager = new();

    public ProjectAnalysis? TryAnalyze(string projectPath, ICollection<ScanDiagnostic> diagnostics)
    {
        var fullPath = Path.GetFullPath(projectPath);
        var name = Path.GetFileNameWithoutExtension(fullPath);
        try
        {
            var options = new EnvironmentOptions { Restore = false };
            options.GlobalProperties["TargetFrameworkMonikerAssemblyAttributesPath"] =
                Path.Combine(Path.GetTempPath(), "upgrade-scan.TFM.AssemblyAttributes.cs");
            var pathTag = Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(fullPath.ToUpperInvariant())))[..8];
            options.GlobalProperties["IntermediateOutputPath"] =
                Path.Combine(Path.GetTempPath(), "upgrade-scan", "obj", $"{name}-{pathTag}")
                + Path.DirectorySeparatorChar;
            var results = _manager.GetProject(fullPath).Build(options);
            var succeeded = results.Where(r => r.Succeeded).ToList();
            if (succeeded.Count == 0)
            {
                diagnostics.Add(new ScanDiagnostic(DiagnosticCodes.BuildFailed, DiagnosticSeverity.Warning,
                    $"{name}: design-time build failed; degrading to manifest analysis."));
                return null;
            }

            var first = succeeded[0];
            var tfms = succeeded
                .Select(r => r.TargetFramework)
                .Where(t => !string.IsNullOrEmpty(t))
                .Select(t => t!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (tfms.Count == 0)
            {
                var legacyVersion = first.GetProperty("TargetFrameworkVersion");
                if (!string.IsNullOrEmpty(legacyVersion))
                    tfms = [TargetFrameworkMoniker.FromLegacyVersion(legacyVersion)];
            }

            var dir = Path.GetDirectoryName(fullPath)!;
            var packages = ProjectFileFacts.MergePackages(
                succeeded
                    .SelectMany(r => r.PackageReferences)
                    .Select(kvp => new PackageRef(kvp.Key,
                        kvp.Value.TryGetValue("Version", out var version) ? version : null)),
                dir);

            var projectRefs = ProjectFileFacts.NormalizeProjectRefs(
                succeeded.SelectMany(r => r.ProjectReferences), dir);

            IReadOnlyList<ApiUsage> apiUsages = [];
            if (ProjectFileFacts.LanguageFromPath(fullPath) == "C#")
            {
                using var workspace = first.GetWorkspace(addProjectReferences: false);
                var compilation = workspace.CurrentSolution.Projects.Single()
                    .GetCompilationAsync().GetAwaiter().GetResult();
                if (compilation is not null)
                    apiUsages = SemanticApiUsageCollector.Collect(compilation, dir);
            }

            return new ProjectAnalysis
            {
                Name = name,
                FullPath = fullPath,
                Tier = AnalysisTier.Semantic,
                Style = first.GetProperty("UsingMicrosoftNETSdk") == "true" ? ProjectStyle.SdkStyle : ProjectStyle.Legacy,
                Language = ProjectFileFacts.LanguageFromPath(fullPath),
                Type = ProjectTypeDetector.Detect(dir, first.GetProperty("OutputType") ?? "", packages),
                ApiUsages = apiUsages,
                TargetFrameworks = tfms,
                Packages = packages,
                ProjectReferences = projectRefs,
            };
        }
        catch (Exception ex)
        {
            diagnostics.Add(new ScanDiagnostic(DiagnosticCodes.AnalyzerError, DiagnosticSeverity.Warning,
                $"{name}: {ex.GetType().Name} during design-time build; degrading. ({ex.Message})"));
            return null;
        }
    }
}
