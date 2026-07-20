namespace UpgradeScan.Analysis;

public static class TargetFrameworkMoniker
{
    public static string FromLegacyVersion(string version) =>
        "net" + version.TrimStart('v', 'V').Replace(".", "", StringComparison.Ordinal);
}
