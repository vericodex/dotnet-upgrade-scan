using Xunit;

namespace UpgradeScan.Analysis.Tests;

public class PackagesConfigReaderTests
{
    [Fact]
    public void ReadsAndSortsPackages()
    {
        using var tmp = new TempDir();
        tmp.Write("packages.config",
            """
            <?xml version="1.0" encoding="utf-8"?>
            <packages>
              <package id="Newtonsoft.Json" version="13.0.1" targetFramework="net472" />
              <package id="EntityFramework" version="6.4.4" targetFramework="net472" />
            </packages>
            """);

        var packages = PackagesConfigReader.Read(tmp.Path);

        Assert.Equal(2, packages.Count);
        Assert.Equal("EntityFramework", packages[0].Id);
        Assert.Equal("6.4.4", packages[0].Version);
        Assert.Equal("Newtonsoft.Json", packages[1].Id);
    }

    [Fact]
    public void MissingFileYieldsEmptyList()
    {
        using var tmp = new TempDir();
        Assert.Empty(PackagesConfigReader.Read(tmp.Path));
    }
}
