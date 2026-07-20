using UpgradeScan.Core.Pipeline;
using Xunit;

namespace UpgradeScan.Analysis.Tests;

public class ReadOnlyGuaranteeTests
{
    [Fact]
    public void FullModeAnalysisWritesNothingIntoTheScannedTree()
    {
        using var dir = new TempDir();
        CopyTree(Path.Combine(Fixtures.Root, "net472-two-proj"), dir.Path);
        var before = Snapshot(dir.Path);

        var analyzer = new TieredProjectAnalyzer([new BuildalyzerTierAnalyzer(), new ManifestAnalyzer()]);
        foreach (var csproj in Directory.EnumerateFiles(dir.Path, "*.csproj", SearchOption.AllDirectories))
            analyzer.Analyze(csproj);

        Assert.Equal(before, Snapshot(dir.Path));
    }

    private static string[] Snapshot(string root) =>
        [.. Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Select(f => $"{Path.GetRelativePath(root, f)}|{new FileInfo(f).Length}")
            .OrderBy(x => x, StringComparer.Ordinal)];

    private static void CopyTree(string source, string target)
    {
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var dest = Path.Combine(target, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(file, dest);
        }
    }
}
