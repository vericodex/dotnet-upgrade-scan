using UpgradeScan.Core.Model;
using Xunit;

namespace UpgradeScan.Analysis.Tests;

public class ProjectTypeDetectorTests
{
    private static IReadOnlyList<PackageRef> Pkgs(params string[] ids) =>
        [.. ids.Select(i => new PackageRef(i, "1.0.0"))];

    [Fact]
    public void MvcPackageWinsOverPlainWeb()
    {
        using var dir = new TempDir();
        Assert.Equal(ProjectType.AspNetMvc,
            ProjectTypeDetector.Detect(dir.Path, "Library", Pkgs("Microsoft.AspNet.Mvc")));
    }

    [Fact]
    public void AspxFilesMeanWebForms()
    {
        using var dir = new TempDir();
        File.WriteAllText(Path.Combine(dir.Path, "Default.aspx"), "<%@ Page %>");
        Assert.Equal(ProjectType.AspNetWebForms, ProjectTypeDetector.Detect(dir.Path, "Library", []));
    }

    [Fact]
    public void SvcFilesMeanWcfService()
    {
        using var dir = new TempDir();
        File.WriteAllText(Path.Combine(dir.Path, "Service1.svc"), "<%@ ServiceHost %>");
        Assert.Equal(ProjectType.WcfService, ProjectTypeDetector.Detect(dir.Path, "Library", []));
    }

    [Fact]
    public void ClientConfigWithServiceModelMeansWcfClient()
    {
        using var dir = new TempDir();
        File.WriteAllText(Path.Combine(dir.Path, "app.config"),
            "<configuration><system.serviceModel><client /></system.serviceModel></configuration>");
        Assert.Equal(ProjectType.WcfClient, ProjectTypeDetector.Detect(dir.Path, "Library", []));
    }

    [Fact]
    public void ExeOutputMeansConsoleWhenNothingElseMatches()
    {
        using var dir = new TempDir();
        Assert.Equal(ProjectType.Console, ProjectTypeDetector.Detect(dir.Path, "Exe", []));
        Assert.Equal(ProjectType.Library, ProjectTypeDetector.Detect(dir.Path, "Library", []));
        Assert.Equal(ProjectType.Unknown, ProjectTypeDetector.Detect(dir.Path, "", []));
    }
}
