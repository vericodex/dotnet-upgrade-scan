using System.Xml.Linq;
using UpgradeScan.Core.Model;

namespace UpgradeScan.Analysis;

public static class PackagesConfigReader
{
    public static IReadOnlyList<PackageRef> Read(string projectDirectory)
    {
        var file = Path.Combine(projectDirectory, "packages.config");
        if (!File.Exists(file))
            return [];
        return XDocument.Load(file)
            .Descendants("package")
            .Select(p => new PackageRef(p.Attribute("id")?.Value ?? "", p.Attribute("version")?.Value))
            .Where(p => p.Id.Length > 0)
            .OrderBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
