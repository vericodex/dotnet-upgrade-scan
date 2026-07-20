using System.Text.RegularExpressions;
using System.Xml.Linq;
using UpgradeScan.Core.Abstractions;

namespace UpgradeScan.Analysis;

public sealed class SolutionLoader : ISolutionLoader
{
    private static readonly Regex SlnProjectLine = new(
        @"^Project\(""\{[A-F0-9\-]+\}""\)\s*=\s*""[^""]*"",\s*""(?<path>[^""]+)""",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

    public IReadOnlyList<string> FindProjects(string path)
    {
        var fullPath = Path.GetFullPath(path);
        IEnumerable<string> projects;
        if (Directory.Exists(fullPath))
            projects = FindInDirectory(fullPath);
        else if (fullPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            projects = ParseSln(fullPath);
        else if (fullPath.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase))
            projects = ParseSlnx(fullPath);
        else if (IsProjectFile(fullPath))
            projects = [fullPath];
        else
            throw new ArgumentException($"Not a solution (.sln/.slnx), project (.csproj/.vbproj), or directory: {path}");

        return projects
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsProjectFile(string path) =>
        path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<string> ParseSln(string slnPath)
    {
        var dir = Path.GetDirectoryName(slnPath)!;
        foreach (Match m in SlnProjectLine.Matches(File.ReadAllText(slnPath)))
        {
            var rel = Normalize(m.Groups["path"].Value);
            if (IsProjectFile(rel))
                yield return Path.Combine(dir, rel);
        }
    }

    private static IEnumerable<string> ParseSlnx(string slnxPath)
    {
        var dir = Path.GetDirectoryName(slnxPath)!;
        foreach (var project in XDocument.Load(slnxPath).Descendants("Project"))
        {
            var rel = project.Attribute("Path")?.Value;
            if (rel is not null && IsProjectFile(rel))
                yield return Path.Combine(dir, Normalize(rel));
        }
    }

    private static IEnumerable<string> FindInDirectory(string dir) =>
        Directory.EnumerateFiles(dir, "*.*proj", SearchOption.AllDirectories)
            .Where(IsProjectFile)
            .Where(p => !p.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(seg => seg.Equals("bin", StringComparison.OrdinalIgnoreCase)
                         || seg.Equals("obj", StringComparison.OrdinalIgnoreCase)
                         || seg == ".git"));

    private static string Normalize(string relPath) =>
        relPath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
}
