using System.Text.Json;
using System.Text.Json.Serialization;
using UpgradeScan.Core.Model;

namespace UpgradeScan.Reporting;

public sealed class JsonReportRenderer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public string Render(AssessmentModel model) =>
        JsonSerializer.Serialize(new ReportDto(model), Options).Replace("\r\n", "\n") + "\n";

    private sealed class ReportDto(AssessmentModel model)
    {
        public int SchemaVersion { get; } = AssessmentModel.SchemaVersion;
        public string ToolVersion { get; } = model.ToolVersion;
        public string RulesHash { get; } = model.RulesHash;
        public string TargetFramework { get; } = model.TargetFramework;
        public string SolutionPath { get; } = model.SolutionPath;
        public DateTimeOffset? ScanDate { get; } = model.ScanDate;
        public IReadOnlyList<string> UpgradeOrder { get; } = model.UpgradeOrder;
        public IReadOnlyList<IReadOnlyList<string>> Cycles { get; } = model.Cycles;
        public IReadOnlyList<ProjectDto> Projects { get; } = [.. model.Projects.Select(p => new ProjectDto(p))];
        public IReadOnlyList<DiagnosticDto> Diagnostics { get; } = [.. model.Diagnostics.Select(d => new DiagnosticDto(d))];
    }

    private sealed class ProjectDto(ProjectAssessment p)
    {
        public string Name { get; } = p.Analysis.Name;
        public string Path { get; } = p.Analysis.FullPath;
        public string Language { get; } = p.Analysis.Language;
        public ProjectStyle Style { get; } = p.Analysis.Style;
        public ProjectType Type { get; } = p.Analysis.Type;
        public AnalysisTier Tier { get; } = p.Analysis.Tier;
        public IReadOnlyList<string> TargetFrameworks { get; } = p.Analysis.TargetFrameworks;
        public IReadOnlyList<PackageDto> Packages { get; } = [.. p.Analysis.Packages.Select(x => new PackageDto(x))];
        public IReadOnlyList<PackageFindingDto> PackageFindings { get; } = [.. p.PackageFindings.Select(f => new PackageFindingDto(f))];
        public IReadOnlyList<ApiFindingDto> ApiFindings { get; } = [.. p.ApiFindings.Select(f => new ApiFindingDto(f))];
        public EffortDto Effort { get; } = new(p.Effort);
        public IReadOnlyList<DiagnosticDto> Diagnostics { get; } = [.. p.Analysis.Diagnostics.Select(d => new DiagnosticDto(d))];
    }

    private sealed class PackageDto(PackageRef r)
    {
        public string Id { get; } = r.Id;
        public string? Version { get; } = r.Version;
    }

    private sealed class PackageFindingDto(PackageFinding f)
    {
        public string RuleId { get; } = f.RuleId;
        public string PackageId { get; } = f.PackageId;
        public string? PackageVersion { get; } = f.PackageVersion;
        public PackageVerdict Verdict { get; } = f.Verdict;
        public FindingSeverity Severity { get; } = f.Severity;
        public string? ReplacementPackage { get; } = f.ReplacementPackage;
        public string? Notes { get; } = f.Notes;
    }

    private sealed class ApiFindingDto(ApiFinding f)
    {
        public string RuleId { get; } = f.RuleId;
        public string Category { get; } = f.Category;
        public FindingSeverity Severity { get; } = f.Severity;
        public string Symbol { get; } = f.Symbol;
        public string File { get; } = f.File;
        public int Line { get; } = f.Line;
        public bool Approximate { get; } = f.Approximate;
        public string? Note { get; } = f.Note;
    }

    private sealed class EffortDto(EffortScore e)
    {
        public int Score { get; } = e.Score;
        public EffortBand Band { get; } = e.Band;
        public IReadOnlyList<string> FloorsApplied { get; } = e.FloorsApplied;
    }

    private sealed class DiagnosticDto(ScanDiagnostic d)
    {
        public string Code { get; } = d.Code;
        public DiagnosticSeverity Severity { get; } = d.Severity;
        public string Message { get; } = d.Message;
    }
}
