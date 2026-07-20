# Upgrade assessment — mvc5-web

- Tool: dotnet-upgrade-scan 0.0.0-test
- Rules: constant00000
- Target: net10.0

## Summary

| Project | Target framework | Style | Type | Tier | Blockers | Packages | Effort |
|---|---|---|---|---|---:|---:|---|
| Mvc5Web | net472 | Legacy | AspNetMvc | Syntactic | 2 | 2 | M |

## Upgrade order

1. Mvc5Web

```mermaid
graph TD
  Mvc5Web["Mvc5Web"]
```

## Mvc5Web

Effort: M (score 20)

### API findings

**web**

- [API0101] ~ System.Web — Controllers/HomeController.cs:1 — No modern equivalent; requires migration to ASP.NET Core.
- [API0101] ~ System.Web.Mvc — Controllers/HomeController.cs:2 — No modern equivalent; requires migration to ASP.NET Core.

### Packages

| Package | Version | Verdict | Replacement | Rule |
|---|---|---|---|---|
| Newtonsoft.Json | 12.0.2 | Compatible | — | PKG0001 |

## Effort formula

```
score = Σ (blocker findings per category × category weight)
      + incompatible packages with no known replacement × noReplacement weight
      + incompatible packages with a known replacement × withReplacement weight
floors: configured project types → XL; VB → one size up
```

Weights and bands live in rules/scoring.yaml; the rules hash above pins them.

## Diagnostics

None.
