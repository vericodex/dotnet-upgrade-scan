# dotnet-upgrade-scan

[![CI](https://github.com/Prot0x/dotnet-upgrade-scan/actions/workflows/ci.yml/badge.svg)](https://github.com/Prot0x/dotnet-upgrade-scan/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/dotnet-upgrade-scan.svg)](https://www.nuget.org/packages/dotnet-upgrade-scan)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%2010.0-512BD4)

A free, open-source, **deterministic** assessment CLI for planning .NET Framework → modern .NET upgrades. Point it at a legacy solution; get a dependency-ordered upgrade plan with per-project effort estimates — without modifying a single file.

> **Why this exists.** Microsoft deprecated the free .NET Upgrade Assistant in favor of a paid GitHub Copilot modernization agent. AWS closed its free Porting Assistant to new customers. The community lost every free, deterministic option for upgrade assessment. This project is the community continuation.

## What it tells you

Scanning a solution produces a report that answers four questions:

1. **Where are you?** Target framework, project style (SDK vs. legacy), and language for every project — even ones that no longer build.
2. **In what order?** The dependency-ordered upgrade sequence (leaves first), with circular references reported as groups instead of crashes.
3. **What will block you?** NuGet packages incompatible with modern .NET (with known replacements), and blocker API usages per project — WCF, WebForms, `System.Web`, AppDomain, Remoting, COM interop, and more.
4. **How much work is it?** A rough S / M / L / XL effort score per project, computed from a documented, tunable heuristic.

## Principles

- **Deterministic.** Same input + same tool version → byte-identical report. No AI in the core, no heuristic drift.
- **Read-only.** The scanner never writes into your source tree — enforced by an automated test, not a promise.
- **No network, no telemetry. Ever.** Scans run fully offline. Nothing is collected, phoned home, or updated behind your back.
- **Every claim is traceable.** Each finding cites a rule ID (`PKG0042`, `API0117`) backed by a YAML rule file you can read, dispute, and improve.
- **Never dies mid-scan.** Analysis degrades gracefully per project: full design-time build → syntax-only → raw manifest parsing. A broken project on disk still yields useful facts.
- **Cross-platform.** Windows, Linux, macOS. No Visual Studio or full MSBuild install required.

## Status

The full v1 pipeline works end to end: load → tiered analysis → upgrade order → rules matching → effort scoring → report. The main open work is growing the rules database beyond its seed set.

| Capability | Status |
|---|---|
| Scan `.sln` / `.slnx` / project / directory | ✅ |
| Tiered analysis with graceful degradation | ✅ (all three tiers) |
| Package & API rules engine (YAML) | ✅ (seed database — contributions welcome) |
| Dependency-ordered upgrade sequence | ✅ (cycles reported as groups) |
| Effort scoring (S/M/L/XL) | ✅ |
| Markdown, JSON & console reports | ✅ |
| `dotnet tool install` packaging | ✅ |
| SARIF output, GitHub Action, codemods | planned (v2) |

## Quick start

```bash
dotnet tool install -g dotnet-upgrade-scan
upgrade-scan path/to/YourSolution.sln --output report.md
```

Requires .NET 8 or later. To build from source instead:

```bash
git clone https://github.com/Prot0x/dotnet-upgrade-scan.git
cd dotnet-upgrade-scan
dotnet run --project src/UpgradeScan.Cli -- path/to/YourSolution.sln --output report.md
```

Building from source requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

The default report is Markdown: a summary table, the dependency-ordered upgrade sequence (with a Mermaid graph), and per-project findings with effort scores. An excerpt from scanning an MVC 5 fixture:

```markdown
## Summary

| Project | Target framework | Style | Type | Tier | Blockers | Packages | Effort |
|---|---|---|---|---|---:|---:|---|
| Mvc5Web | net472 | Legacy | AspNetMvc | Syntactic | 2 | 2 | M |

## Mvc5Web

Effort: M (score 20)

### API findings

**web**

- [API0101] ~ System.Web — Controllers/HomeController.cs:1 — No modern equivalent; requires migration to ASP.NET Core.
```

For a quick look in the terminal, use `--format console`:

```text
upgrade-scan — tests/fixtures/net472-two-proj
target net10.0 · rules 04f243e34a9b · 2 project(s)
╭─────────┬──────────────────┬────────┬─────────┬───────────┬──────────┬──────────┬────────╮
│ Project │ Target framework │ Style  │ Type    │ Tier      │ Blockers │ Packages │ Effort │
├─────────┼──────────────────┼────────┼─────────┼───────────┼──────────┼──────────┼────────┤
│ Lib     │ net472           │ Legacy │ Library │ Syntactic │ 0        │ 0        │ S      │
│ App     │ net472           │ Legacy │ Console │ Syntactic │ 0        │ 1        │ S      │
╰─────────┴──────────────────┴────────┴─────────┴───────────┴──────────┴──────────┴────────╯
Effort: 2×S · 0 blocker finding(s)
```

### Options

| Option | Effect |
|---|---|
| `--format <markdown\|json\|console>` | Report format. Default: `markdown`. |
| `--output <file>` | Write the report to a file instead of stdout (markdown/json). |
| `--target <tfm>` | Modern target framework to assess against. Default: `net10.0`. |
| `--no-build` | Skip design-time builds (Tier 1); syntax + manifest analysis only. Fastest, useful in CI. |
| `--rules <dir>` | Extra/override rules directory (same layout as the shipped `rules/`). |
| `--verbosity <quiet\|normal\|diagnostic>` | `diagnostic` adds the full diagnostics list on stderr. |
| `--deterministic` | Omit the scan date so the report is byte-stable. |

There are also two rules subcommands: `rules list` enumerates every loaded rule with its ID, and `rules validate [dir]` checks a rules directory and exits non-zero on any error.

Exit code `0` means the scan completed (findings never affect the exit code — this is an assessment, not a gate); `1` means the scan could not run.

## How it works

Each project is analyzed at the highest tier that succeeds, and the report tells you which tier produced each result:

| Tier | Method | You get |
|---|---|---|
| **Semantic** | Buildalyzer design-time build → Roslyn compilation | Exact API usage via resolved symbols, transitive package closure |
| **Syntactic** | Roslyn syntax trees, no compilation | API usage from using-directives + qualified names, flagged `~approximate` |
| **Manifest** | Raw XML of `.csproj`, `packages.config`, `*.config` | Target framework, project style & type, direct packages |

Legacy solutions often fail design-time builds — that's the norm, not the exception, and it's why the tiered approach exists. A scan of 20 projects never dies on project 3.

VB.NET projects are detected and reported at manifest level with a "reduced analysis" note; C# gets the full tiered analysis.

## Contributing

Contributions are welcome — especially to the **rules database**: each package/API rule is a small, self-contained YAML file in [`rules/`](rules/), reviewable without knowing the analyzer internals. The authoring guide is [`docs/rules-authoring.md`](docs/rules-authoring.md), and `upgrade-scan rules validate` checks your rule files before you open a PR.

Also valuable:

- **Try it on your solution** and open an issue for anything that crashes, misdetects, or surprises you — broken/exotic legacy projects are exactly the test cases this tool needs.
- Run `dotnet test` from the repo root; the suite must stay green and builds are warning-clean (`TreatWarningsAsErrors`).

## Prior art & credits

This project continues ideas from tools the community lost — the .NET Upgrade Assistant (Microsoft, MIT, deprecated) and Porting Assistant for .NET (AWS, Apache-2.0, closed to new customers). See [`NOTICE`](NOTICE) for lineage and license provenance.

## License

[MIT](LICENSE) © 2026 Nitin Mukherjee
