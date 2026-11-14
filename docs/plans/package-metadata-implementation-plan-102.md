# Implementation Plan: Package Metadata Review — description, tags, license, readme and icon on every shipped package

> **Issue**: [#102](https://github.com/dlrivada/Encina/issues/102)
> **Type**: Feature
> **Complexity**: Low per change, wide in reach (107 packable projects, about 14 pull requests)
> **Estimated Scope**: ~150 lines of shared MSBuild metadata and per-project edits, ~350 lines for the validation script, 39 short package READMEs, 2 to 3 icon-related lines

---

## Summary

Make the NuGet metadata of every package shipped from `src/` complete and verified by a command, as SPEC-000 REQ-018 requires ("license, readme and icon metadata"; AC-018: "`Directory.Build.props` carries the metadata; #92, #93, #102 closed"). Today (measured on `origin/main` 7a0049c07) `Directory.Build.props` sets only `Authors`, `RepositoryUrl` and `RepositoryType`, and the 107 projects under `src/` (all packable by the path rule that SPEC-000 REQ-001 introduced) are uneven:

| Metadata | Set today | Gap |
|----------|-----------|-----|
| `Description` | 96 of 107 | 11: `Encina.ADO.{SqlServer,PostgreSQL,MySQL}`, `Encina.Dapper.{SqlServer,PostgreSQL,MySQL}`, `Encina.Extensions.Resilience`, `Encina.Hangfire`, `Encina.Polly`, `Encina.Quartz`, `Encina.Refit` |
| `PackageTags` | 70 of 107 | 37 projects |
| `PackageReadmeFile` | 26 of 107 (11 pack the repository root `README.md`, 15 pack the README beside the project) | 81 projects; 67 projects have a README beside the project file, 40 have none |
| License | 23 projects use `PackageLicenseFile` (the `LICENSE` file is packed with a `None` item); none uses `PackageLicenseExpression` | 84 projects declare no license at all |
| `PackageIcon` | 0 of 107 | all; the file is produced by [#103](https://github.com/dlrivada/Encina/issues/103) |
| `Copyright`, `Company`, `Product`, `NeutralLanguage`, `PackageProjectUrl` | none | shared values |

What this plan delivers:

- The shared values (`Copyright`, `Company`, `Product`, `NeutralLanguage`, `PackageProjectUrl`, `PackageLicenseExpression` = `MIT`) and the shared readme and icon wiring in `Directory.Build.props`.
- A per-package `Description`, `PackageTags` and README where one is missing.
- A C# file-based script, `.github/scripts/check-package-metadata.cs`, that reads the packed `.nuspec` of every packable project and fails when a required field is absent, wired into CI at the end.

**Scope**: no code under `src/` changes; the changes are MSBuild properties in `Directory.Build.props` and the project files, package READMEs and one script. No public API changes, so no `PublicAPI.*.txt` entry.

**Affected packages**: all 107 packable projects under `src/`; the substantive edits are in the 11 without description, the 37 without tags, the 23 with `PackageLicenseFile`, the 26 that reference a README explicitly and the 40 without a README.

**Provider category**: none. The issue changes build properties and package metadata only; no database, caching, transport, lock or validation provider code is touched (`AGENTS.md` section 5).

**Size and split.** One pull request cannot carry this (107 projects, `AGENTS.md` section 11: at most 15 files and 1500 changed lines per pull request). The split below is the proposal; each row is one branch and one pull request that carries `Refs #102`, the last one `Fixes #102`:

| PR | Phase | Content | Files | Depends on |
|----|-------|---------|-------|------------|
| 1 | 1 | Validation script in report-only mode, with `--self-test` | 1 | none |
| 2 | 2 | Remove `PackageLicenseFile` and the packed `LICENSE` item from the first 12 of the 23 projects | 12 | PR 1 (baseline) |
| 3 | 2 | Remove them from the remaining 11 projects; add the shared properties and `PackageLicenseExpression` to `Directory.Build.props` | 12 | PR 2 |
| 4 | 3 | `Description` for the 11 projects that have none | 11 | none |
| 5 to 7 | 3 | `PackageTags` for the 37 projects that have none (13, 12 and 12 projects) | 13 / 12 / 12 | none |
| 8 | 4 | README convention in `Directory.Build.props` and removal of the explicit README items from the first 13 projects (if Design Choice 3 stays option A) | 14 | PR 3 |
| 9 | 4 | Removal of the explicit README items from the other 13 projects of the 26 | 13 | PR 8 |
| 10 to 12 | 4 | The 39 missing package READMEs (the core `Encina` packs the repository README), written by `docs-writer` (13 files each) | 13 / 13 / 13 | PR 8 |
| 13 | 5 | Icon packing rule in `Directory.Build.props`, `assets/icons` consumed, icon check enabled in the script | 1 to 2 | PR 3 and #103 merged |
| 14 | 6 | Script switched to enforcing, wired into `ci.yml` (orchestrator-owned), changelog fragment, contributor-guide note | 3 | PRs 1 to 13 |

Between PR 2 and PR 3 the 12 projects of PR 2 temporarily declare no license; nothing is published from `main` in that window (publishing is tag-driven, and the full 107-package pipeline belongs to #92 and #93), but PRs 2 and 3 should merge back to back. PR 13 waits for #103; if #103 is late, PRs 1 to 12 and 14 can land first and the icon check is enabled with PR 13.

---

## Design Choices

<details>
<summary><strong>1. Where the common metadata lives — <code>Directory.Build.props</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Directory.Build.props`** | One place; already holds `Authors`, `RepositoryUrl` and the path rule that makes `src/` projects packable; SPEC-000 AC-018 names it | A change touches every project's build (cache invalidation) |
| **B) A separate `Packaging.props` imported by `Directory.Build.props`** | Isolates the metadata block | A second file for ten properties; nothing else in the repository does this |
| **C) Repeat the properties in each `.csproj`** | Each package is self-contained | 107 copies to keep in step; this is the current state and the cause of the unevenness |

### Chosen Option: **A — `Directory.Build.props`**

**Class:** Settled
**Settled by:** SPEC-000 AC-018 (`Directory.Build.props` carries the metadata) and REQ-001 (the path rule already lives there)

### Rationale

- SPEC-000 AC-018 says `Directory.Build.props` carries the metadata.
- The file already derives packability from the path (REQ-001) and sets `Authors` (`David Lozano Rivada`), `RepositoryUrl` and `RepositoryType` under the same `.csproj` condition; the new properties join that group.
- What is specific to one package (description, tags, README) stays in its own `.csproj` or beside it.

</details>

<details>
<summary><strong>2. License — <code>PackageLicenseExpression</code> <code>MIT</code> for every package</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `PackageLicenseExpression` `MIT` in `Directory.Build.props`** | One SPDX id, no file to pack, NuGet.org shows it directly, matches `LICENSE` ("MIT License", "Copyright (c) 2025 David Lozano Rivada") | The 23 projects that pack the file must drop `PackageLicenseFile` (a package cannot declare both) |
| **B) `PackageLicenseFile` `LICENSE` everywhere** | Same text in every package | A `None` item with a relative path in 107 projects; NuGet.org cannot read the license from a file as it does from an expression |
| **C) Keep the mix** | No edits | 84 packages have no license; fails REQ-018 |

### Chosen Option: **A — `PackageLicenseExpression` `MIT`**

**Class:** Settled
**Settled by:** SPEC-000 REQ-018 (license metadata required on every package) and the repository `LICENSE` file (MIT)

### Rationale

- `LICENSE` is the MIT license; the SPDX expression is the form NuGet recommends for a standard license.
- Setting the expression in `Directory.Build.props` while a project still sets `PackageLicenseFile` makes that project fail to pack, so the 23 projects are cleaned first (PRs 2 and 3) and the shared property lands with the last of them.
- Phase 2 confirms the exact NuGet error with `dotnet pack` on one of the 23 before the property lands.

</details>

<details>
<summary><strong>3. README wiring — convention in <code>Directory.Build.props</code> or an explicit item in each project</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Convention in `Directory.Build.props`**: when `README.md` exists beside the project, set `PackageReadmeFile` and pack that file | 0 per-project edits for the 67 projects that already have a README beside them; a new package cannot forget it; same spirit as the path rule for `IsPackable` | The 26 explicit entries become duplicates and must be removed; a README stored elsewhere needs an explicit override; two items packing the same path may fail the pack as a warning-as-error (`NU*`), to be confirmed in Phase 4 |
| **B) Explicit `PackageReadmeFile` and `None` item in each project** | Visible in the project file; this is what 26 projects do today | 81 more near-identical edits; the 11 projects that pack the root README show the wrong page on NuGet.org |
| **C) One shared README for all packages** | Trivial | A satellite package page that says nothing about the satellite; does not let users tell packages apart |

### Chosen Option: **A — convention in `Directory.Build.props`** (recommended, pending the maintainer)

**Class:** Dilemma

### Rationale

- Option A touches the fewest files and makes the result checkable: the validation script asserts that the `.nuspec` carries a readme for every package, whichever option wires it.
- Option B wins for a maintainer who wants every package's identity readable in its own project file; the cost is 81 edits in about six pull requests.
- The core `Encina` package keeps its explicit item because it ships the repository `README.md` (there is no README beside `src/Encina/Encina.csproj`).

</details>

<details>
<summary><strong>4. The 40 packages without a README — write one each, or fall back to a shared page</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A short per-package README for each of the 40, written by `docs-writer`** | The NuGet.org page explains what the package is, how to install and register it; consistent with the 67 existing ones | 40 files in three pull requests |
| **B) Fall back to the repository root README for packages without one** | No new files; 11 packages do this today | Every satellite shows the same page; fails the intent of REQ-018 for users choosing between packages |
| **C) Generate a README from `Description` and `PackageTags` at pack time** | No files to maintain | A generated page has no install or usage section; a build step that invents content |

### Chosen Option: **A — a short per-package README for each of the 40** (recommended, pending the maintainer)

**Class:** Dilemma

### Rationale

- Option A is the only one that lets a user choose between, for example, `Encina.Caching.Redis` and `Encina.Caching.Valkey` on the package page.
- Option B wins if the maintainer wants the metadata gate closed before the READMEs are written; the cost is the 11 packages that already show the wrong page, plus the 40.
- The 39 to write (40 projects have no README beside them; the core `Encina` packs the repository README): `Encina.AmazonSQS`, `Encina.AzureServiceBus`, `Encina.Caching`, `Encina.Caching.{Dragonfly,Garnet,Hybrid,KeyDB,Memory,Redis,Valkey}`, `Encina.Cdc.{Debezium,MongoDb,MySql,PostgreSql,SqlServer}`, `Encina.Cli`, `Encina.Compliance.{Anonymization,CrossBorderTransfer,PrivacyByDesign}`, `Encina.DistributedLock.{InMemory,Redis,SqlServer}`, `Encina.gRPC`, `Encina.InMemory`, `Encina.Kafka`, `Encina.Marten`, `Encina.MQTT`, `Encina.NATS`, `Encina.RabbitMQ`, `Encina.Redis.PubSub`, `Encina.Security.{Audit,BreakTheGlass}`, `Encina.SignalR`, `Encina.Tenancy`, `Encina.Tenancy.AspNetCore` and `Encina.Testing{,.Fakes,.Shouldly,.TUnit}`.

</details>

<details>
<summary><strong>5. How the validation script reads the metadata — pack and read the nuspec, evaluate MSBuild properties, or both</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `dotnet pack` every packable project and read each `.nuspec` inside the `.nupkg`** | Verifies what NuGet.org will show, including the packed README and icon files, and catches pack-time errors | Slowest; packs 107 projects (the time is not measured yet; Phase 1 measures it) |
| **B) `dotnet msbuild -getProperty:...` for each project** | No pack, fast, no build output | Does not prove the README or icon file reaches the package |
| **C) B on every pull request, A in the release pipeline** | Fast feedback, full proof where it matters | Two code paths in one script |

### Chosen Option: **A — pack and read the nuspec** (recommended, pending the maintainer)

**Class:** Dilemma

### Rationale

- The issue asks for a script that "packs the packages and reads each `.nuspec`", so the result is verified by command and not by eye; option A is that script.
- Option C wins if the measured pack time makes a per-pull-request run unaffordable; Phase 1 reports the time before the script is wired into `ci.yml`, and `ci-full.yml` already runs `dotnet pack` for the core package only (`src/Encina/Encina.csproj`).
- The script follows `.github/scripts/check-src-projects.cs`: a file-based app with `--self-test` that `ci.yml` runs.

</details>

<details>
<summary><strong>6. Author and copyright holder — as in <code>LICENSE</code> and <code>Directory.Build.props</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Copyright` = `Copyright (c) 2025 David Lozano Rivada`, `Company` = `David Lozano Rivada`** | Identical to `LICENSE` and to the existing `Authors`; static, so builds stay deterministic | Needs a manual edit if the year range is to grow |
| **B) A year range computed from the clock at build time** | Always current | Makes the nuspec change from build to build, against REQ-018's deterministic build settings |
| **C) The issue's original text (`2025-2026`, a different author name)** | None | Disagrees with `LICENSE` and `Authors` |
| **D) `Copyright (c) 2025-2026 David Lozano Rivada`, a static string** | Deterministic, and honest for a package first published in 2026 | Differs from the year in `LICENSE` until `LICENSE` is updated too |

### Chosen Option: **A — the values of `LICENSE`** (the year is recommended, pending the maintainer: "2025" on a package first published in 2026 is user-visible, D is the alternative)

**Class:** Dilemma

### Rationale

- The author in `LICENSE` and `Directory.Build.props` is David Lozano Rivada; the rewritten issue keeps that value.
- A static string keeps the package contents reproducible (A and D both are).
- The holder name is Settled by the repository `LICENSE` file and the existing `Authors` property in `Directory.Build.props`; only the year is open.

</details>

<details>
<summary><strong>7. <code>Title</code> — not set per package</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Leave `Title` unset (NuGet uses the package id)** | No 107 edits; the id (`Encina.Caching.Redis`) is already the clearest name | The issue's sample shows a `Title` |
| **B) Set a `Title` in each project** | Friendly name on NuGet.org | 107 values to keep in step with `PackageId`; REQ-018 does not ask for it |

### Chosen Option: **A — leave `Title` unset**

### Rationale

- The required fields are the ones REQ-018 lists; `Title` is optional and defaults to the id.
- A project that later wants a title sets it itself; the validation script does not require it.

**Class:** Trivial
**Settled by:** SPEC-000 REQ-018 (required fields); NuGet nuspec reference (`title` is optional) — <https://learn.microsoft.com/en-us/nuget/reference/nuspec>

</details>

<details>
<summary><strong>8. Icon — one 128 px PNG from <code>assets/icons</code>, packed as <code>icon.png</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Pack `assets/icons/package-icon-128.png` as `icon.png` through one item in `Directory.Build.props`, `PackageIcon` = `icon.png`** | One item for 107 packages; the 128 px size is the NuGet recommendation; file names in the repository can differ from the package path | Waits for #103 to commit the files |
| **B) Copy the icon next to each project** | Self-contained projects | 107 copies of one binary |
| **C) A placeholder icon until #103 lands** | Unblocks the property | A throwaway binary in the repository; the property would ship a wrong image |

### Chosen Option: **A — one packed item from `assets/icons`**

### Rationale

- #103 states the layout (`assets/icons/package-icon.svg`, `package-icon-128.png`, `package-icon-256.png`) and that #102 declares `PackageIcon`; the issue text of #102 calls the file `icon.png`, which is its path inside the package.
- NuGet accepts PNG or JPG up to 1 MB, 128x128 recommended; the item uses `Pack="true"` and `PackagePath=""` and applies only to packable projects.

**Class:** Settled
**Settled by:** #103 (icon deliverables and who wires them); NuGet nuspec `icon` reference — <https://learn.microsoft.com/en-us/nuget/reference/nuspec#icon>

</details>

---

## Implementation Phases

### Phase 1: Baseline and Validation Script

<details>
<summary>Tasks</summary>

1. Create `.github/scripts/check-package-metadata.cs`, a C# 14 file-based app modelled on `.github/scripts/check-src-projects.cs` (header comment with usage and exit codes, `--root`, `--self-test`).
2. The script enumerates the packable projects (every `.csproj` under `src/`), runs `dotnet pack` for each into `artifacts/package-metadata/` (or evaluates properties, per Design Choice 5), opens the `.nupkg` and reads the `.nuspec`.
3. Required fields: `authors`, `copyright`, `license` (an expression), `projectUrl`, `description`, `tags`, `readme` (and the file must exist in the package), `icon` (and the file must exist in the package; checked only once Phase 5 lands, behind a flag).
4. `--report` prints the gaps per project and exits 0; the default mode exits 1 when a gap exists; exit 2 is a usage error.
5. `--self-test` builds small nuspec fixtures in memory (complete, missing license, missing readme file in the package) and asserts the verdict of each.
6. Record the baseline (the counts in the Summary) and the measured pack time of all projects in the pull request.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina is pre-1.0, .NET 10 / C# 14. SPEC-000 REQ-018 requires license, readme and icon metadata on every shipped package. Every project under src/ is packable (Directory.Build.props). No script checks the packed metadata yet. Issue #102.

TASK: Create .github/scripts/check-package-metadata.cs as a file-based app (no .csproj) that packs each src project and reads its .nuspec, reporting every missing required field per project. Modes: --report (exit 0), default (exit 1 on a gap), --self-test, --root <dir>. Do not touch any project file in this phase.

KEY RULES: PowerShell or C# file-based apps only (AGENTS.md section 2). Output goes under artifacts/, never the repository root (section 9). Fail closed: a project that cannot be packed is a failure, never a skip. No product code changes, no warnings.

REFERENCE FILES: .github/scripts/check-src-projects.cs (structure, --self-test), Directory.Build.props (the path rule), ci.yml lines around the check-src-projects step (how a script is wired; leave ci.yml to the orchestrator).
```

</details>

### Phase 2: Common Metadata and License Unification

<details>
<summary>Tasks</summary>

1. PR 2: in the first 12 of the 23 projects that set `PackageLicenseFile` (list them with the script's baseline), remove `<PackageLicenseFile>LICENSE</PackageLicenseFile>` and the `<None Include="..\..\LICENSE" Pack="true" ... />` item.
2. PR 3: do the same for the other 11, and add to the `.csproj`-conditioned `PropertyGroup` of `Directory.Build.props`: `Copyright`, `Company`, `Product` (`Encina`), `NeutralLanguage` (`en`), `PackageProjectUrl` (`https://github.com/dlrivada/Encina`) and `PackageLicenseExpression` (`MIT`).
3. Confirm with `dotnet pack` on one project of each kind (the core, one satellite, `Encina.Cli`, which is a tool package with `PackAsTool`) that the nuspec carries the license and no `NU*` warning appears (`TreatWarningsAsErrors` makes `NU*` an error).
4. `Encina.Cli` packs `LICENSE` and the root `README.md` as `None` items without declaring them; the cleanup covers it.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: 23 projects under src/ set PackageLicenseFile and pack LICENSE; the other 84 declare no license. Directory.Build.props sets Authors, RepositoryUrl, RepositoryType only. The repository LICENSE is MIT, "Copyright (c) 2025 David Lozano Rivada". Issue #102, plan Design Choices 1, 2 and 6.

TASK: (PR 2) remove PackageLicenseFile and the packed LICENSE item from 12 of the 23 projects. (PR 3) remove them from the other 11 and add Copyright, Company, Product, NeutralLanguage, PackageProjectUrl and PackageLicenseExpression=MIT to the existing Package Metadata group of Directory.Build.props. Verify with dotnet pack that a package carries <license type="expression">MIT</license>.

KEY RULES: Edit with the Edit tool only. Static strings; no property computed from the clock. At most 15 files per pull request. Zero warnings, including NU* warnings.

REFERENCE FILES: Directory.Build.props, src/Encina/Encina.csproj, src/Encina.Cli/Encina.Cli.csproj, LICENSE, the baseline output of .github/scripts/check-package-metadata.cs --report.
```

</details>

### Phase 3: Descriptions and Tags

<details>
<summary>Tasks</summary>

1. PR 4: add `<Description>` to `Encina.ADO.SqlServer`, `Encina.ADO.PostgreSQL`, `Encina.ADO.MySQL`, `Encina.Dapper.SqlServer`, `Encina.Dapper.PostgreSQL`, `Encina.Dapper.MySQL`, `Encina.Extensions.Resilience`, `Encina.Hangfire`, `Encina.Polly`, `Encina.Quartz` and `Encina.Refit`. Each states what the package adds and for which technology, in one or two sentences, and does not repeat the package id.
2. PRs 5 to 7: add `<PackageTags>` (5 to 10 tags, `;` separated, lower case) to the 37 projects without them: `Encina.ADO.{MySQL,PostgreSQL,SqlServer}`, `Encina.AmazonSQS`, `Encina.AzureServiceBus`, `Encina.Caching`, `Encina.Cdc` and `Encina.Cdc.{Debezium,MongoDb,MySql,PostgreSql,SqlServer}`, `Encina.Cli`, `Encina.Dapper.{MySQL,PostgreSQL,SqlServer}`, `Encina.EntityFrameworkCore`, `Encina.Extensions.Resilience`, `Encina.GraphQL`, `Encina.gRPC`, `Encina.Hangfire`, `Encina.InMemory`, `Encina.Kafka`, `Encina.Marten`, `Encina.Messaging`, `Encina.MongoDB`, `Encina.MQTT`, `Encina.NATS`, `Encina.OpenTelemetry`, `Encina.Polly`, `Encina.Quartz`, `Encina.RabbitMQ`, `Encina.Redis.PubSub`, `Encina.Refit`, `Encina.SignalR`, `Encina.Tenancy` and `Encina.Tenancy.AspNetCore`.
3. Tag families: core `cqrs`, `mediator`, `rop`, `dotnet`; EF Core `efcore`, `repository`, `unitofwork`, `outbox`; Messaging `messaging`, `outbox`, `inbox`, `saga`, `scheduling`; Dapper `dapper`, `micro-orm`, `sql`; ADO `ado-net`, `sql`; each provider adds its product name (`sqlserver`, `postgresql`, `mysql`, `rabbitmq`, `kafka`, ...). The existing 70 projects keep their tags.
4. Descriptions and tags read the package's `README.md` or its public registration method (`AddEncina*`) so they are accurate.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: 11 packable projects have no Description and 37 have no PackageTags (list them with check-package-metadata.cs --report). The other projects already have both and are not changed. Issue #102, plan Phase 3.

TASK: Add a Description to the 11 projects in PR 4, and PackageTags to the 37 in PRs 5 to 7 (13, 12 and 12 projects). Read each project's source and README first; describe what it really does and for which technology. Tags: 5 to 10, lower case, ';' separated, most specific first.

KEY RULES: Edit tool only. English only. No marketing claims the code does not back; do not copy another package's text. Match the existing PropertyGroup style of the project file. At most 15 files per pull request.

REFERENCE FILES: src/Encina/Encina.csproj (style), src/Encina.Caching.Redis/Encina.Caching.Redis.csproj, the README.md beside each project, AGENTS.md section 5 (provider families).
```

</details>

### Phase 4: README Wiring and Package READMEs

<details>
<summary>Tasks</summary>

1. PR 8 and 9 (if Design Choice 3 stays A): in `Directory.Build.props`, for a packable project with a `README.md` beside it and no `PackageReadmeFile` of its own, set `<PackageReadmeFile>README.md</PackageReadmeFile>` under `Condition="'$(PackageReadmeFile)' == ''"` and add `<None Include="$(MSBuildProjectDirectory)\README.md" Pack="true" PackagePath="" />` under the same condition, so a project that still has the explicit entries packs the README once and PR 8 and PR 9 are independent; remove the explicit `PackageReadmeFile` and README `None` items from the 26 projects that have them, except the core `Encina` (its README is the repository root file).
2. The 11 projects that pack the root README switch to their own README (they have one beside them, or get one in PRs 10 to 12).
3. Confirm with `dotnet pack` that no `NU*` duplicate-file warning remains.
4. PRs 10 to 12: `docs-writer` writes the missing package READMEs (Design Choice 4): purpose, install, minimal registration with the real `AddEncina*` call, link to the docs page. Each README is reviewed by `docs-reviewer` and checks that every identifier exists in `src/`.
5. `Encina.Cli` is a tool package (`PackAsTool`): its README describes `dotnet tool install`.
6. `check-package-metadata.cs` also checks that `PackageProjectUrl` and `RepositoryUrl` answer with a success status (an HTTP HEAD request, run in the release pipeline where network is available, not on every pull request), and that the packed README is non-empty and starts with a heading; "READMEs render properly in package preview" is otherwise confirmed on the pre-release listing of #100.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: 67 projects have a README.md beside the project file, 26 reference a README (11 the repository root file, 15 their own), 40 have none. NuGet needs PackageReadmeFile plus the file packed at that path. Issue #102, plan Design Choices 3 and 4 (both pending the maintainer: follow the chosen option recorded in the Maintainer Decisions section, if it exists).

TASK: (PR 8, 9) add the README convention to Directory.Build.props and remove the 26 explicit entries (the core Encina keeps its own). (PRs 10-12, docs-writer) write the 39 missing package READMEs, 13 per pull request.

KEY RULES: Edit tool for props and project files; READMEs only through docs-writer (the encina-docs skill: junior-readable English, real identifiers verified in src/, no hand-typed coverage figures, no emojis). One pull request, at most 15 files. Run dotnet pack on one project before and after to prove the nuspec readme entry.

REFERENCE FILES: Directory.Build.props, src/Encina.Caching.Redis/README.md (existing satellite README), .claude/skills/encina-docs/SKILL.md, the nuspec readme reference https://learn.microsoft.com/en-us/nuget/reference/msbuild-targets#packagereadmefile.
```

</details>

### Phase 5: Package Icon

<details>
<summary>Tasks</summary>

1. Precondition: [#103](https://github.com/dlrivada/Encina/issues/103) is merged and `assets/icons/package-icon-128.png` exists.
2. In `Directory.Build.props`, for packable projects, set `<PackageIcon>icon.png</PackageIcon>` and add `<None Include="$(MSBuildThisFileDirectory)assets\icons\package-icon-128.png" Pack="true" PackagePath="icon.png" Visible="false" />`.
3. Enable the icon requirement in `check-package-metadata.cs` (the field and the file in the package).
4. Check once on a test publish that the icon displays on NuGet.org (the acceptance criterion), recording the pre-release version used.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: SPEC-000 REQ-018 needs icon metadata on every package. No src project sets PackageIcon. Issue #103 produces assets/icons/package-icon-128.png; this issue wires it. Plan Design Choice 8.

TASK: Add the PackageIcon property and the packed icon item to Directory.Build.props for packable projects, then turn on the icon check in .github/scripts/check-package-metadata.cs. Prove with dotnet pack that the nupkg contains icon.png and the nuspec has <icon>icon.png</icon>.

KEY RULES: Do not start before #103 is merged; never commit a placeholder image. Edit tool only. PNG, at most 1 MB. Zero warnings.

REFERENCE FILES: Directory.Build.props, assets/icons/ (from #103), https://learn.microsoft.com/en-us/nuget/reference/nuspec#icon.
```

</details>

### Phase 6: Testing, CI Enforcement, Documentation and Finalization

<details>
<summary>Tasks</summary>

1. Switch `check-package-metadata.cs` to enforcing and add the CI step (`dotnet run --file .github/scripts/check-package-metadata.cs -- --self-test`, then the check) to `ci.yml` next to the `check-src-projects.cs` step; `.github/workflows/*` is owned by the orchestrator, so this plan hands over the exact step rather than editing it.
2. Tests: none of the Encina test projects apply (no code under `src/`); the script's `--self-test` is the test, and `check-package-metadata.cs` itself is the integration check over the real packed packages (see the Estimated File Count and the matrix for why unit, guard, contract, property, load and benchmark tests are not applicable).
3. Documentation: a short note in `CONTRIBUTING.md` or the packaging guide on the metadata conventions (where the shared values live, that a new package needs a README and a description), written by `docs-writer`; no ADR (no architectural decision).
4. Changelog fragment `changelog.d/102-package-metadata.changed.md`, written by `mechanical-fixer`.
5. Verification: `dotnet build Encina.slnx --configuration Release` with zero warnings, and the script exits 0 over all 107 projects.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Phases 1 to 5 landed: the script reports no gap on the 107 packable projects. Issue #102 closes with this pull request.

TASK: Make the script enforcing by default, hand the orchestrator the ci.yml step, add the contributor note (docs-writer) and the changelog fragment (mechanical-fixer, section changed).

KEY RULES: Never edit CHANGELOG.md or .github/workflows/* from a worker. Per-job permissions in workflows (AGENTS.md section 10). Report the script's final output and the build output verbatim.

REFERENCE FILES: .github/scripts/check-package-metadata.cs, ci.yml (check-src-projects step), changelog.d/README.md, CONTRIBUTING.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | What it requires | Use here |
|--------|------------------|----------|
| SPEC-000 REQ-018, AC-018 | License, readme and icon metadata on every package; `Directory.Build.props` carries the metadata; #92, #93, #102 closed | The acceptance of the whole feature |
| SPEC-000 REQ-001 | One authoritative list of shipping packages; every project under `src/` is packable by path | The script iterates the same set |
| SPEC-000 REQ-019 | Package ids reserved on NuGet.org, publish exercised end to end on a pre-release | The icon display check reuses that pre-release (#100) |
| NuGet nuspec reference | `license` as an SPDX expression or file; `readme` a packed `.md`; `icon` a packed PNG or JPG up to 1 MB, 128x128 recommended; `title` optional | Field rules of the script |
| NuGet pack MSBuild targets | `PackageLicenseExpression`, `PackageReadmeFile`, `PackageIcon`, `PackageProjectUrl`, `Copyright` | The properties set in `Directory.Build.props` |
| `AGENTS.md` section 8 | Zero warnings; `NU*` is an error in this repository (`WarningsAsErrors`) | Duplicate packed files fail the build |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| Common package properties and the packability path rule | `Directory.Build.props` | Receives the shared values and the readme and icon rules |
| Script pattern with `--self-test` | `.github/scripts/check-src-projects.cs` | Template for `check-package-metadata.cs` |
| Script wiring in CI | `.github/workflows/ci.yml` (the `check-src-projects.cs` step) | Where the new step goes (orchestrator-owned) |
| Existing core pack step | `.github/workflows/ci-full.yml` (`dotnet pack src/Encina/Encina.csproj`) | Pack baseline; the full pipeline is #92, #93 |
| Existing READMEs | `src/*/README.md` (67) | Packed by the convention |
| Repository license | `LICENSE` (MIT) | Source of the license expression, author and copyright |
| Release checklist registry | `.github/scripts/release-readiness.cs` | May cite the script as a release-gate item; not changed here |
| Brand image today | `docs/assets/Encina Framework Identidad Visual.png` | Not used; the icon comes from #103 |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| none | none | No runtime code and no `[LoggerMessage]`; nothing is registered in `src/Encina/Diagnostics/EventIdRanges.cs` |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| `Directory.Build.props` | 1 | Shared values, license, readme and icon rules (edited in PRs 3, 8 and 13) |
| Project files (`.csproj`) | up to 107 | 23 drop `PackageLicenseFile`, 11 gain `Description`, 37 gain `PackageTags`, 26 drop explicit README items; some projects appear in more than one list |
| Package READMEs | 39 | Missing READMEs, written by `docs-writer` (the core `Encina` packs the root README) |
| Validation script | 1 | `.github/scripts/check-package-metadata.cs` |
| CI wiring | 1 | `ci.yml` step, orchestrator-owned |
| Tests | 0 | `--self-test` of the script; no `src/` code changes |
| Changelog fragment | 1 | `changelog.d/102-package-metadata.changed.md` |
| Documentation | 1 | Contributor note on the metadata conventions |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library. Every project under src/ (107) is packable by the path rule in Directory.Build.props. SPEC-000 REQ-018 requires license, readme and icon metadata on every package. Today 96 projects set Description, 70 PackageTags, 26 PackageReadmeFile, 23 PackageLicenseFile, none PackageIcon or the shared values. Issue #102; the icon file comes from #103.

IMPLEMENTATION OVERVIEW: Phase 1 adds .github/scripts/check-package-metadata.cs (report mode). Phase 2 removes PackageLicenseFile from the 23 projects and adds Copyright, Company, Product, NeutralLanguage, PackageProjectUrl and PackageLicenseExpression=MIT to Directory.Build.props. Phase 3 adds 11 Descriptions and 37 PackageTags. Phase 4 wires READMEs by convention and writes the missing package READMEs through docs-writer. Phase 5 packs assets/icons/package-icon-128.png as icon.png once #103 is merged. Phase 6 makes the script enforcing, wires it into ci.yml (orchestrator), adds the contributor note and the changelog fragment. Each row of the split table in the Summary is one pull request of at most 15 files.

KEY PATTERNS: Shared values in Directory.Build.props, package-specific values in the project; the script follows check-src-projects.cs (file-based app, --self-test, exit codes 0/1/2); static strings only, so builds stay deterministic; NU* warnings are errors; fail closed when a project cannot be packed. Design Choices 3, 4 and 5 are pending the maintainer: follow the Maintainer Decisions section if it exists, otherwise the recommended option.

REFERENCE FILES: Directory.Build.props, Directory.Build.targets, src/Encina/Encina.csproj, src/Encina.Cli/Encina.Cli.csproj, .github/scripts/check-src-projects.cs, .github/workflows/ci.yml, LICENSE, docs/specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md (REQ-018, AC-018), AGENTS.md sections 2, 8, 9, 10 and 11.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | MSBuild properties and packed assets have nothing to do with `ICacheProvider`, decorators or `[Cache]`. |
| 2 | OpenTelemetry | ❌ | The validation script is a build-time tool, not a runtime operation, so there is no span or metric to add. |
| 3 | Structured Logging | ❌ | No `[LoggerMessage]` is added and no EventId range is needed; the script writes to the console. |
| 4 | Health Checks | ❌ | There is no runtime dependency to check; the script is a CI tool, not a service. |
| 5 | Validation | ❌ | The script validates package metadata at build time; no request is received and no `IValidationProvider` is involved. |
| 6 | Resilience | ❌ | No external system is called at run time; `dotnet pack` runs locally and fails closed. |
| 7 | Distributed Locks | ❌ | No shared state is accessed concurrently. |
| 8 | Transactions | ❌ | No multi-operation atomicity is needed; each pull request is independent. |
| 9 | Idempotency | ❌ | Re-running the script or a pack gives the same result; no duplicates can arrive. |
| 10 | Multi-Tenancy | ❌ | No tenant data is stored or queried. |
| 11 | Module Isolation | ❌ | No module scoping applies to package metadata. |
| 12 | Audit Trail | ❌ | No compliance or security effect beyond the license field. |

---

## Prerequisites & Dependencies

| Item | State | Effect |
|------|-------|--------|
| [#103](https://github.com/dlrivada/Encina/issues/103) design logo and visual identity | open | Phase 5 (the icon) cannot start before its files are merged; Phases 1 to 4 and 6 do not wait |
| [#92](https://github.com/dlrivada/Encina/issues/92) and [#93](https://github.com/dlrivada/Encina/issues/93) release pipeline (provenance, signing) | open | They own the full 107-package pack and publish pipeline; this plan only adds the metadata they pack |
| [#1362](https://github.com/dlrivada/Encina/issues/1362) committed package manifest (SPEC-000 AC-001) | open | When it lands, the script can read its list instead of enumerating `src/`; not required here |
| SourceLink and deterministic build settings (also named by REQ-018) | not in this issue | Not part of #102's acceptance criteria; the plan does not add them |

---

## Next Steps

1. When work on #102 starts, present Design Choices 3, 4 and 5 (README wiring, the 40 missing READMEs, how the script reads the metadata) to the maintainer and record the answers in a last section, `Maintainer Decisions`.
2. Write the worker briefs per pull request from the split table: PR 1 first (its baseline fixes the exact lists of projects per pull request), PRs 4 to 7 can run in parallel with it, PRs 10 to 12 go to `docs-writer`.
3. Hand the `ci.yml` step of Phase 6 to the orchestrator, and open the icon pull request (PR 13) only after #103 is merged.
4. Comment on #102 with the link to this plan.
