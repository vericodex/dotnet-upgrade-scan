using UpgradeScan.Core.Model;

namespace UpgradeScan.Analysis;

public static class ProjectFileFacts
{
    public static string LanguageFromPath(string projectPath) =>
        projectPath.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase) ? "VB" : "C#";

    public static IReadOnlyList<PackageRef> MergePackages(IEnumerable<PackageRef> declared, string projectDir) =>
        [.. declared
            .Concat(PackagesConfigReader.Read(projectDir))
            .DistinctBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p.Id, StringComparer.OrdinalIgnoreCase)];

    public static IReadOnlyList<string> NormalizeProjectRefs(IEnumerable<string> refs, string projectDir) =>
        [.. refs
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar))
            .Select(r => Path.IsPathRooted(r) ? Path.GetFullPath(r) : Path.GetFullPath(Path.Combine(projectDir, r)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)];

    public static IReadOnlyList<string> EnumerateCSharpFiles(string projectDir) =>
        Directory.Exists(projectDir)
            ? [.. Directory.EnumerateFiles(projectDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !IsInExcludedDir(projectDir, f))
                .Select(f => Path.GetRelativePath(projectDir, f).Replace('\\', '/'))
                .OrderBy(f => f, StringComparer.Ordinal)]
            : [];

    public static bool IsInExcludedDir(string root, string file)
    {
        var relative = Path.GetRelativePath(root, file);
        var segments = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Any(s => s is "bin" or "obj" or ".git");
    }
}
