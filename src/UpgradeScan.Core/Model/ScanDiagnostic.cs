namespace UpgradeScan.Core.Model;

public sealed record ScanDiagnostic(string Code, DiagnosticSeverity Severity, string Message);
