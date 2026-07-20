using UpgradeScan.Core.Model;

namespace UpgradeScan.Core.Abstractions;

public interface IProjectAssessor
{
    ProjectAssessment Assess(ProjectAnalysis project);
}
