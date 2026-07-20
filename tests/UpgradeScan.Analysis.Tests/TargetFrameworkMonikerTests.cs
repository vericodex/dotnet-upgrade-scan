using Xunit;

namespace UpgradeScan.Analysis.Tests;

public class TargetFrameworkMonikerTests
{
    [Theory]
    [InlineData("v4.7.2", "net472")]
    [InlineData("v4.8", "net48")]
    [InlineData("v4.0", "net40")]
    [InlineData("v3.5", "net35")]
    [InlineData("V4.6.1", "net461")]
    public void MapsLegacyVersionToMoniker(string legacy, string expected) =>
        Assert.Equal(expected, TargetFrameworkMoniker.FromLegacyVersion(legacy));
}
