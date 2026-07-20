namespace UpgradeScan.Core.Abstractions;

public interface ISolutionLoader
{
    IReadOnlyList<string> FindProjects(string path);
}
