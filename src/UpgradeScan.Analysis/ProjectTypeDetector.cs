using UpgradeScan.Core.Model;

namespace UpgradeScan.Analysis;

public static class ProjectTypeDetector
{
    public static ProjectType Detect(string projectDir, string outputTypeOrEmpty, IReadOnlyList<PackageRef> packages)
    {
        bool HasPackage(string prefix) =>
            packages.Any(p => p.Id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        bool HasFile(string pattern) =>
            Directory.Exists(projectDir) &&
            Directory.EnumerateFiles(projectDir, pattern, SearchOption.AllDirectories)
                .Any(f => !ProjectFileFacts.IsInExcludedDir(projectDir, f));

        if (HasFile("*.svc"))
            return ProjectType.WcfService;
        if (HasFile("*.aspx"))
            return ProjectType.AspNetWebForms;
        if (HasPackage("Microsoft.AspNet.Mvc"))
            return ProjectType.AspNetMvc;
        if (HasPackage("Microsoft.AspNet.WebPages") || HasPackage("Microsoft.AspNet.WebForms"))
            return ProjectType.AspNetWebForms;
        if (ConfigDeclaresWcfClient(projectDir))
            return ProjectType.WcfClient;
        if (HasFile("*.xaml"))
            return ProjectType.Wpf;
        if (HasFile("*.Designer.cs") && HasFile("*.resx"))
            return ProjectType.WinForms;

        return outputTypeOrEmpty.Equals("Exe", StringComparison.OrdinalIgnoreCase) ? ProjectType.Console
            : outputTypeOrEmpty.Equals("Library", StringComparison.OrdinalIgnoreCase) ? ProjectType.Library
            : ProjectType.Unknown;
    }

    private static bool ConfigDeclaresWcfClient(string projectDir)
    {
        foreach (var config in new[] { "app.config", "App.config", "web.config", "Web.config" })
        {
            var path = Path.Combine(projectDir, config);
            if (!File.Exists(path))
                continue;
            var text = File.ReadAllText(path);
            if (text.Contains("<system.serviceModel", StringComparison.OrdinalIgnoreCase) &&
                text.Contains("<client", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
