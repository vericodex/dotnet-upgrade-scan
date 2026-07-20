using System.CommandLine;
using System.Reflection;
using System.Xml;
using Spectre.Console;
using UpgradeScan.Analysis;
using UpgradeScan.Core.Abstractions;
using UpgradeScan.Core.Pipeline;
using UpgradeScan.Reporting;
using UpgradeScan.Rules;

namespace UpgradeScan.Cli;

public static class RootCommandFactory
{
    public static RootCommand Create()
    {
        var pathArgument = new Argument<string>("path")
        {
            Description = "Solution (.sln/.slnx), project file, or directory to scan.",
        };
        var formatOption = new Option<string>("--format")
        {
            Description = "Report format.",
            DefaultValueFactory = _ => "markdown",
        };
        formatOption.AcceptOnlyFromAmong("markdown", "json", "console");
        var outputOption = new Option<string?>("--output")
        {
            Description = "Write the report to this file instead of stdout (markdown/json only).",
        };
        var targetOption = new Option<string>("--target")
        {
            Description = "Modern target framework to assess against.",
            DefaultValueFactory = _ => "net10.0",
        };
        var noBuildOption = new Option<bool>("--no-build")
        {
            Description = "Skip design-time builds (Tier 1); syntax + manifest analysis only.",
        };
        var rulesOption = new Option<string?>("--rules")
        {
            Description = "Extra/override rules directory (same layout as the shipped rules/).",
        };
        var verbosityOption = new Option<string>("--verbosity")
        {
            Description = "diagnostic adds the full diagnostics list on stderr.",
            DefaultValueFactory = _ => "normal",
        };
        verbosityOption.AcceptOnlyFromAmong("quiet", "normal", "diagnostic");
        var deterministicOption = new Option<bool>("--deterministic")
        {
            Description = "Omit the scan date so the report is byte-stable.",
        };

        var root = new RootCommand("Deterministic, read-only .NET upgrade assessment scanner.")
        {
            pathArgument, formatOption, outputOption, targetOption,
            noBuildOption, rulesOption, verbosityOption, deterministicOption,
        };
        root.SetAction(parseResult => Scan(
            parseResult.GetValue(pathArgument)!,
            parseResult.GetValue(formatOption)!,
            parseResult.GetValue(outputOption),
            parseResult.GetValue(targetOption)!,
            parseResult.GetValue(noBuildOption),
            parseResult.GetValue(rulesOption),
            parseResult.GetValue(verbosityOption)!,
            parseResult.GetValue(deterministicOption)));

        var rulesCommand = new Command("rules", "Work with the rules database.");
        var listCommand = new Command("list", "Enumerate loaded rules with IDs.");
        listCommand.SetAction(_ => RulesList());
        var validateDirArgument = new Argument<string?>("dir")
        {
            Description = "Rules directory to validate (default: the shipped rules).",
            Arity = ArgumentArity.ZeroOrOne,
        };
        var validateCommand = new Command("validate", "Validate rule files; non-zero exit on any error.")
        {
            validateDirArgument,
        };
        validateCommand.SetAction(parseResult => RulesValidate(parseResult.GetValue(validateDirArgument)));
        rulesCommand.Subcommands.Add(listCommand);
        rulesCommand.Subcommands.Add(validateCommand);
        root.Subcommands.Add(rulesCommand);

        return root;
    }

    private static string ShippedRulesDirectory() => Path.Combine(AppContext.BaseDirectory, "rules");

    private static string ToolVersion() =>
        (typeof(RootCommandFactory).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0")
        .Split('+')[0];

    private static int Scan(string path, string format, string? output, string target,
        bool noBuild, string? rulesDir, string verbosity, bool deterministic)
    {
        try
        {
            var ruleSet = RulesLoader.Load(ShippedRulesDirectory(), rulesDir);
            List<ITierAnalyzer> tiers = noBuild
                ? [new SyntacticTierAnalyzer(), new ManifestAnalyzer()]
                : [new BuildalyzerTierAnalyzer(), new SyntacticTierAnalyzer(), new ManifestAnalyzer()];
            var pipeline = new AssessmentPipeline(
                new SolutionLoader(), new TieredProjectAnalyzer(tiers), new RulesAssessor(ruleSet, target));
            var context = new AssessmentContext(target, ToolVersion(), ruleSet.Hash,
                deterministic ? null : DateTimeOffset.UtcNow);
            var model = pipeline.Run(path, context);

            if (verbosity == "diagnostic")
                foreach (var d in model.Diagnostics.Concat(model.Projects.SelectMany(p => p.Analysis.Diagnostics)))
                    Console.Error.WriteLine($"{d.Code} {d.Message}");

            if (format == "console")
            {
                new ConsoleSummaryRenderer().Render(model, AnsiConsole.Console);
            }
            else
            {
                var report = format == "json"
                    ? new JsonReportRenderer().Render(model)
                    : new MarkdownReportRenderer().Render(model);
                if (output is not null)
                    File.WriteAllText(output, report);
                else
                    Console.Out.Write(report);
            }
            return 0;
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException
            or XmlException or RulesLoadException)
        {
            AnsiConsole.MarkupLine($"[red]error:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }

    private static int RulesList()
    {
        try
        {
            var rules = RulesLoader.Load(ShippedRulesDirectory());
            foreach (var pkg in rules.Packages)
                Console.WriteLine($"{pkg.Id}  package    {pkg.Package}  ({pkg.Verdict.ToString().ToLowerInvariant()})");
            foreach (var group in rules.ApiGroups)
            {
                Console.WriteLine($"{group.GroupId}  api-group  {group.Technology}");
                foreach (var pattern in group.Patterns)
                    Console.WriteLine($"{pattern.Id}  api        {pattern.Match}  ({pattern.Severity.ToString().ToLowerInvariant()})");
            }
            return 0;
        }
        catch (RulesLoadException ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }
    }

    private static int RulesValidate(string? dir)
    {
        var target = dir ?? ShippedRulesDirectory();
        var errors = RulesValidator.Validate(target);
        if (errors.Count == 0)
        {
            Console.WriteLine($"rules OK: {target}");
            return 0;
        }
        foreach (var error in errors)
            Console.Error.WriteLine(error);
        return 1;
    }
}
