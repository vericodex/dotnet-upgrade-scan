using System.Xml.Linq;
using UpgradeScan.Core.Abstractions;
using UpgradeScan.Core.Model;

namespace UpgradeScan.Analysis;

public sealed class ManifestAnalyzer : ITierAnalyzer
{
    public ProjectAnalysis? TryAnalyze(string projectPath, ICollection<ScanDiagnostic> diagnostics)
    {
        var fullPath = Path.GetFullPath(projectPath);
        var name = Path.GetFileNameWithoutExtension(fullPath);

        XDocument doc;
        try
        {
            doc = XDocument.Load(fullPath);
        }
        catch (Exception ex) when (ex is System.Xml.XmlException or IOException or UnauthorizedAccessException)
        {
            diagnostics.Add(new ScanDiagnostic(DiagnosticCodes.ProjectUnreadable, DiagnosticSeverity.Error,
                $"{name}: could not read project file ({ex.Message})"));
            return null;
        }

        var root = doc.Root;
        if (root is null)
        {
            diagnostics.Add(new ScanDiagnostic(DiagnosticCodes.ProjectUnreadable, DiagnosticSeverity.Error,
                $"{name}: empty project file"));
            return null;
        }

        var dir = Path.GetDirectoryName(fullPath)!;
        var packages = ProjectFileFacts.MergePackages(
            ElementsByLocalName(root, "PackageReference")
                .Select(e => new PackageRef(
                    e.Attribute("Include")?.Value ?? "",
                    e.Attribute("Version")?.Value ?? ValueByLocalName(e, "Version")))
                .Where(p => p.Id.Length > 0),
            dir);

        var projectRefs = ProjectFileFacts.NormalizeProjectRefs(
            ElementsByLocalName(root, "ProjectReference")
                .Select(e => e.Attribute("Include")?.Value)
                .Where(v => v is not null)
                .Select(v => v!),
            dir);

        return new ProjectAnalysis
        {
            Name = name,
            FullPath = fullPath,
            Tier = AnalysisTier.Manifest,
            Style = root.Attribute("Sdk") is not null ? ProjectStyle.SdkStyle : ProjectStyle.Legacy,
            Language = ProjectFileFacts.LanguageFromPath(fullPath),
            Type = ProjectTypeDetector.Detect(dir, ValueByLocalName(root, "OutputType") ?? "", packages),
            TargetFrameworks = ReadTargetFrameworks(root),
            Packages = packages,
            ProjectReferences = projectRefs,
        };
    }

    private static IReadOnlyList<string> ReadTargetFrameworks(XElement root)
    {
        var multi = ValueByLocalName(root, "TargetFrameworks");
        if (!string.IsNullOrWhiteSpace(multi))
            return multi.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var single = ValueByLocalName(root, "TargetFramework");
        if (!string.IsNullOrWhiteSpace(single))
            return [single];

        var legacy = ValueByLocalName(root, "TargetFrameworkVersion");
        if (!string.IsNullOrWhiteSpace(legacy))
            return [TargetFrameworkMoniker.FromLegacyVersion(legacy)];

        return [];
    }

    private static IEnumerable<XElement> ElementsByLocalName(XElement root, string localName) =>
        root.Descendants().Where(e => e.Name.LocalName == localName);

    private static string? ValueByLocalName(XElement scope, string localName) =>
        scope.Descendants().FirstOrDefault(e => e.Name.LocalName == localName)?.Value;
}
