using Xunit;

namespace UpgradeScan.Analysis.Tests;

public class SolutionLoaderTests
{
    private readonly SolutionLoader _loader = new();

    [Fact]
    public void ParsesClassicSlnAndResolvesRelativePaths()
    {
        using var tmp = new TempDir();
        tmp.Write("App/App.csproj", "<Project/>");
        tmp.Write("Lib/Lib.csproj", "<Project/>");
        var sln = tmp.Write("All.sln",
            """
            Microsoft Visual Studio Solution File, Format Version 12.00
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "App", "App\App.csproj", "{11111111-1111-1111-1111-111111111111}"
            EndProject
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Lib", "Lib\Lib.csproj", "{22222222-2222-2222-2222-222222222222}"
            EndProject
            Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "SolutionItems", "SolutionItems", "{33333333-3333-3333-3333-333333333333}"
            EndProject
            """);

        var projects = _loader.FindProjects(sln);

        Assert.Equal(2, projects.Count);
        Assert.All(projects, p => Assert.True(Path.IsPathRooted(p)));
        Assert.EndsWith("App.csproj", projects[0]);
        Assert.EndsWith("Lib.csproj", projects[1]);
    }

    [Fact]
    public void ParsesSlnx()
    {
        using var tmp = new TempDir();
        tmp.Write("App/App.csproj", "<Project/>");
        var slnx = tmp.Write("All.slnx",
            """
            <Solution>
              <Project Path="App/App.csproj" />
              <Folder Name="docs" />
            </Solution>
            """);

        var projects = _loader.FindProjects(slnx);

        Assert.Single(projects);
        Assert.EndsWith("App.csproj", projects[0]);
    }

    [Fact]
    public void DirectoryScanSkipsBinObjAndSorts()
    {
        using var tmp = new TempDir();
        tmp.Write("B/B.csproj", "<Project/>");
        tmp.Write("A/A.vbproj", "<Project/>");
        tmp.Write("A/bin/Bad.csproj", "<Project/>");
        tmp.Write("A/obj/Bad2.csproj", "<Project/>");

        var projects = _loader.FindProjects(tmp.Path);

        Assert.Equal(2, projects.Count);
        Assert.EndsWith("A.vbproj", projects[0]);
        Assert.EndsWith("B.csproj", projects[1]);
    }

    [Fact]
    public void SingleProjectFileIsReturnedAsIs()
    {
        using var tmp = new TempDir();
        var proj = tmp.Write("App/App.csproj", "<Project/>");
        var projects = _loader.FindProjects(proj);
        Assert.Equal([Path.GetFullPath(proj)], projects);
    }

    [Fact]
    public void UnsupportedInputThrows()
    {
        using var tmp = new TempDir();
        var txt = tmp.Write("readme.txt", "hi");
        Assert.Throws<ArgumentException>(() => _loader.FindProjects(txt));
    }
}
