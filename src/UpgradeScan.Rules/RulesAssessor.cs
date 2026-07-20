using UpgradeScan.Core.Abstractions;
using UpgradeScan.Core.Model;

namespace UpgradeScan.Rules;

public sealed class RulesAssessor(RuleSet rules, string targetTfm) : IProjectAssessor
{
    public ProjectAssessment Assess(ProjectAnalysis project)
    {
        var packageFindings = PackageRuleMatcher.Match(project.Packages, targetTfm, rules);
        var apiFindings = ApiRuleMatcher.Match(project.ApiUsages, rules);
        return new ProjectAssessment
        {
            Analysis = project,
            PackageFindings = packageFindings,
            ApiFindings = apiFindings,
            Effort = EffortScorer.Score(project, packageFindings, apiFindings, rules.Scoring),
        };
    }
}
