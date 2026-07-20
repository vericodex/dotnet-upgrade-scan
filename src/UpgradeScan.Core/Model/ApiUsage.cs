namespace UpgradeScan.Core.Model;

public sealed record ApiUsage(string Symbol, string File, int Line, bool Approximate);
