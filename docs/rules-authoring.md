# Add a rule in 10 minutes

Every claim `upgrade-scan` makes traces to a rule file with a stable ID. No rule, no finding.
That means the rules database *is* the product — and adding one file is a complete, shippable
contribution.

## Layout

```
rules/
  packages/<package-id-lowercase>.yaml   # one file per NuGet package
  apis/<group>.yaml                      # one file per API technology group
  scoring.yaml                           # effort weights and bands (PR-able like any rule)
```

## Package rule

```yaml
# rules/packages/system.data.sqlclient.yaml
id: PKG0002                 # ^PKG\d{4}$ — unique across ALL files, never reused
package: System.Data.SqlClient
verdict: replace            # compatible | replace | partial | incompatible | deprecated
severity: warning           # info | warning | blocker
replacement:                # REQUIRED when verdict is replace
  package: Microsoft.Data.SqlClient
  notes: Namespace changes from System.Data.SqlClient to Microsoft.Data.SqlClient.
targets:                    # optional per-TFM verdict overrides
  net8.0: replace
source: manual              # provenance: manual | aws-porting-assistant-datastore
fix:                        # optional, v2 codemods — validated, unused by v1
  transform: PKG0002-swap-package
links:
  - https://learn.microsoft.com/sql/connect/ado-net/introduction-microsoft-data-sqlclient-namespace
```

The filename must be the package id lowercased + `.yaml` — the validator enforces it.

## API rule group

```yaml
# rules/apis/system-web.yaml
group: API0100              # ^API\d{4}$
technology: ASP.NET (System.Web)
category: web               # its weight lives in scoring.yaml (falls back to "default")
patterns:
  - id: API0101             # ^API\d{4}$ — every pattern has its own ID
    kind: namespace         # namespace | type | member
    match: System.Web
    severity: blocker
    note: No modern equivalent; requires migration to ASP.NET Core.
```

Pattern kinds: `namespace` matches the namespace and everything inside it; `type` matches the
type and its members; `member` matches exactly. The most specific (longest) match wins.

## Picking an ID

Search the rules tree for the highest existing number and take the next one:

```
grep -rh "^id: PKG" rules/packages | sort | tail -1
grep -rhE "^\s*-? ?id: API|^group: API" rules/apis | sort | tail -1
```

IDs are permanent. If a rule is ever deleted, its ID is retired with it — never recycled.

## Validate before you push

```
dotnet run --project src/UpgradeScan.Cli --framework net10.0 -- rules validate rules
```

CI runs exactly this command; a red `rules-validate` job means one of: bad ID format, duplicate
ID, wrong filename, `replace` without `replacement:`, or unparseable YAML. The validator prints
every problem at once.

## PR checklist

- [ ] `rules validate` passes locally
- [ ] One rule (or one tight group) per PR
- [ ] `links:` carries at least one piece of evidence (vendor doc, migration guide, repo notice)
- [ ] `source:` says where the knowledge came from
- [ ] Severity honest: `blocker` = cannot run on modern .NET without rework, not "annoying"
