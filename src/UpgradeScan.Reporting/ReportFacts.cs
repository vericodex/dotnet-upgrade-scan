using UpgradeScan.Core.Model;

namespace UpgradeScan.Reporting;

internal static class ReportFacts
{
    internal static int Blockers(ProjectAssessment p) =>
        p.ApiFindings.Count(f => f.Severity == FindingSeverity.Blocker)
        + p.PackageFindings.Count(f => f.Severity == FindingSeverity.Blocker);
}
