namespace UpgradeScan.IntegrationTests;

public static class Fixtures
{
    public static string Root => Path();

    public static string Path(params string[] parts)
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !Directory.Exists(System.IO.Path.Combine(dir, "tests", "fixtures")))
            dir = System.IO.Path.GetDirectoryName(dir);
        if (dir is null)
            throw new InvalidOperationException("Could not locate tests/fixtures above " + AppContext.BaseDirectory);
        return System.IO.Path.Combine([dir, "tests", "fixtures", .. parts]);
    }
}
