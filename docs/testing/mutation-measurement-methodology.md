# Mutation Measurement Methodology

> **Status**: Living document — updated as the methodology evolves.
> **Tracking issues**: [#957](https://github.com/dlrivada/Encina/issues/957) (workflow stabilization), [#962](https://github.com/dlrivada/Encina/issues/962) (scope widening), [#1440](https://github.com/dlrivada/Encina/issues/1440) and [#1441](https://github.com/dlrivada/Encina/issues/1441) (Stryker.NET 5 with the Microsoft Testing Platform runner), [#1682](https://github.com/dlrivada/Encina/issues/1682) (lost shard runners)
> **Implementation**: `.github/scripts/run-stryker.cs`, `mutation-history.cs`, `update-mutation-summary.cs`, `mut-docs-render.cs`
> **Dashboard**: <https://dlrivada.github.io/Encina/mutations/>

## Purpose

This document explains **how** Encina measures mutation testing — the rules, formulas, and conventions adopted to produce honest, reproducible mutation data given the practical constraints of the test infrastructure (xUnit v3 + Stryker.NET 5.0.0 with the Microsoft Testing Platform runner + GitHub-hosted runners).

It is the contract between the mutation infrastructure and the people who consume its output. Anyone reading a mutation score in a doc, a PR comment, or the dashboard should be able to come here, understand the formula that produced it, and be able to reproduce the result.

## Why a methodology document exists

Off-the-shelf mutation testing assumes that every commit can re-run the full mutant set against the full test suite. That is not feasible for Encina:

- The Stryker `--mutate` glob produces ~6,181 mutants for `src/Encina/Encina.csproj` alone.
- xUnit v3 + Stryker's `coverage-analysis: perTest` mode is broken upstream ([stryker-mutator/stryker-net#3117](https://github.com/stryker-mutator/stryker-net/issues/3117)). Without per-test coverage, every mutant runs the whole `Encina.UnitTests` project (`AllTests` mode).
- The Microsoft Testing Platform (MTP) runner ignores `test-case-filter` ([stryker-mutator/stryker-net#3757](https://github.com/stryker-mutator/stryker-net/issues/3757)), so a mutant cannot be narrowed to the tests of its folder.
- A full single-job run, at about 35.6 s per mutant measured in the [#1087 spike](../engineering/stryker-5-mtp-spike-1087.md), exceeds GitHub's 6-hour job limit. The repository is public, so `ubuntu-latest` has 4 vCPU and 16 GB; the cause of the lost runner in [#1682](https://github.com/dlrivada/Encina/issues/1682) is unknown, and the `runner-resources.log` of the validation run will show memory and disk use.

Encina instead runs **20 parallel shards per weekly run** (indexes 0 to 19), each mutating a slice of `src/Encina/` sized by its number of tested mutants, not by folder (see [Matrix execution](#matrix-execution)). The `aggregate` job merges the shard reports into a single `mutation-report.json` that the downstream publish workflow treats as one normal Stryker run. The formulas below define exactly what the merged dataset means and how to interpret the numbers.

## The mutation score formula

The score reported by the dashboard, the README badge, and `update-mutation-summary.cs` is:

```
mutation_score = 100 × detected / total_considered
```

where:

| Term | Definition |
|------|------------|
| `detected` | Mutants whose status is `Killed`, `Timeout`, or `RuntimeError` (any test reaction is detection). |
| `total_considered` | Mutants whose status is `Killed`, `Survived`, `NoCoverage`, `RuntimeError`, or `Timeout`. **Excludes** `CompileError` and `Ignored`. |

Compile errors are not the test suite's fault — they reflect Stryker producing a syntactically valid but semantically broken mutation that never makes it into the testable surface. Ignored mutants are filtered by the `mutate` glob or the `since` filter and never run. Excluding both keeps the score honest.

The formula lives in `.github/scripts/mutation-history.cs` (`MutationCounts.Detected` and `MutationCounts.TotalConsidered`).

## The accumulation model

Stryker's report is per-run. Encina's dashboard is per-file across runs. Two layers of merging produce the final dataset:

1. **Per-run aggregation** (`.github/workflows/mutation-tests.yml`): the weekly workflow fans out into a 20-shard matrix (see [Matrix execution](#matrix-execution)). An `aggregate` job then merges the shard reports into a single `mutation-report.json` by taking the union of their `files` maps.
2. **Cross-run carry-forward** (`mutation-history.cs --merge-from`): `publish-mutations.yml` reads the aggregated report and the previous `latest.json`. Before it builds either `history.json` or the previous `latest.json` used by `--merge-from`, the publisher runs `pages-dashboard-data.ps1 -Mode Read -Domain mutations -OutDir base/mutations` and builds its base files from the `dashboard-data` branch copy. For `history.json` it uses the branch copy when it is present and non-empty; otherwise it uses the larger, by entry count, of the live Pages copy and the copy committed in `docs/mutations/data/`. The previous `latest.json` used by `--merge-from` comes from the branch copy, else from the live Pages copy; the copy committed in `docs/mutations/data/` is **never** used for it, because it holds old shard results that would be merged back in. When the branch carries both base files the live read is skipped; otherwise the publisher runs `pages-dashboard-data.ps1 -Mode Live -Domain mutations -OutDir live/mutations`, which reads the live `latest.json` and `history.json`. A 404 writes nothing, and when neither the branch nor the live site has a `latest.json` (the first run) nothing is merged. Any other failure (HTTP error, timeout, empty or invalid JSON) fails the job, so an outage is never mistaken for a first run or an empty history. For every file in the previous snapshot **not** touched by this run, the per-file counts are carried forward unchanged. Files the new run mutated get fresh data. The overall score is **recomputed** across the merged file set.

With matrix execution every shard gets a fresh measurement every week, so the carry-forward layer mostly handles files outside the shard scopes (or occasional intermittent shard failures). If a shard fails or uploads no report, the previous week's data for its files survives until the next successful run — the dashboard never silently regresses to zero for transient infrastructure issues. The aggregate job names every shard that uploaded no report (see [Guards](#guards)).

This model deliberately picks **completeness over freshness**. The alternative — overwriting the dashboard with each run's narrow snapshot — would make the score swing wildly when shards fail and lose the cumulative work.

### Live snapshot of folders measured so far

The two folders measured during the workflow stabilization (smoke test + Health). These rows date from 2026-04-14, before the migration to the MTP runner, so they are not a measure of test quality; the first run of the new runner replaces them:

<!-- mutref-table: mut:Encina/Sharding/Migrations/Strategies/* -->

| File | Score | Killed | Survived | NoCov | Total | Last run |
|------|------:|-------:|---------:|------:|------:|----------|
| <a id="mutref-mut-Encina-Sharding-Migrations-Strategies-CanaryFirstStrategy-cs"></a>[`Sharding/Migrations/Strategies/CanaryFirstStrategy.cs`](https://dlrivada.github.io/Encina/mutations/#pkg-Encina) | 0.00% | 0 | 9 | 0 | 9 | 2026-04-14 |
| <a id="mutref-mut-Encina-Sharding-Migrations-Strategies-ParallelMigrationStrategy-cs"></a>[`Sharding/Migrations/Strategies/ParallelMigrationStrategy.cs`](https://dlrivada.github.io/Encina/mutations/#pkg-Encina) | 0.00% | 0 | 21 | 0 | 21 | 2026-04-14 |
| <a id="mutref-mut-Encina-Sharding-Migrations-Strategies-RollingUpdateStrategy-cs"></a>[`Sharding/Migrations/Strategies/RollingUpdateStrategy.cs`](https://dlrivada.github.io/Encina/mutations/#pkg-Encina) | 0.00% | 0 | 15 | 0 | 15 | 2026-04-14 |
| <a id="mutref-mut-Encina-Sharding-Migrations-Strategies-SequentialMigrationStrategy-cs"></a>[`Sharding/Migrations/Strategies/SequentialMigrationStrategy.cs`](https://dlrivada.github.io/Encina/mutations/#pkg-Encina) | 21.43% | 3 | 11 | 0 | 14 | 2026-04-14 |

*4 file(s) matched `mut:Encina/Sharding/Migrations/Strategies/*`. Data from [mutations dashboard](https://dlrivada.github.io/Encina/mutations/). See [mutation-measurement-methodology.md](mutation-measurement-methodology.md).*

<!-- /mutref-table -->

<!-- mutref-table: mut:Encina/Sharding/Health/* -->

| File | Score | Killed | Survived | NoCov | Total | Last run |
|------|------:|-------:|---------:|------:|------:|----------|
| <a id="mutref-mut-Encina-Sharding-Health-ShardHealthResult-cs"></a>[`Sharding/Health/ShardHealthResult.cs`](https://dlrivada.github.io/Encina/mutations/#pkg-Encina) | 0.00% | 0 | 12 | 0 | 12 | 2026-04-14 |
| <a id="mutref-mut-Encina-Sharding-Health-ShardReplicaHealthCheck-cs"></a>[`Sharding/Health/ShardReplicaHealthCheck.cs`](https://dlrivada.github.io/Encina/mutations/#pkg-Encina) | 0.00% | 0 | 29 | 0 | 29 | 2026-04-14 |
| <a id="mutref-mut-Encina-Sharding-Health-ShardedHealthSummary-cs"></a>[`Sharding/Health/ShardedHealthSummary.cs`](https://dlrivada.github.io/Encina/mutations/#pkg-Encina) | 0.00% | 0 | 12 | 0 | 12 | 2026-04-14 |

*3 file(s) matched `mut:Encina/Sharding/Health/*`. Data from [mutations dashboard](https://dlrivada.github.io/Encina/mutations/). See [mutation-measurement-methodology.md](mutation-measurement-methodology.md).*

<!-- /mutref-table -->

For the canonical view across all packages and history, go to the [mutations dashboard](https://dlrivada.github.io/Encina/mutations/).

## Matrix execution

`.github/workflows/mutation-tests.yml` fans the weekly run out into 20 parallel shards (indexes 0 to 19) via a GitHub Actions matrix. Before [#1028](https://github.com/dlrivada/Encina/issues/1028) the workflow rotated through a list of folders one per week; since then every shard runs every week.

### Why shards are sized by mutant count

The MTP runner ignores `test-case-filter` ([stryker-net#3757](https://github.com/stryker-mutator/stryker-net/issues/3757)), so every mutant runs the whole `Encina.UnitTests` project. A local smoke on 2026-10-03 found 22,069 tests in that project, and the [#1087 spike](../engineering/stryker-5-mtp-spike-1087.md) measured 35.6 s per mutant on its pilot shard. A shard's duration therefore grows with its number of tested mutants (statuses Killed, Survived or Timeout), not with the folder it mutates. The shards are sized to about 150 tested mutants or fewer. The per-folder test filter of [#1027](https://github.com/dlrivada/Encina/issues/1027) no longer exists: the `FILTERS` array and the `test-case-filter` patching were removed in [#1441](https://github.com/dlrivada/Encina/issues/1441).

### Pipeline shape

| Job | Purpose |
|-----|---------|
| `select-matrix` | Emits the shard list as a JSON array (20 entries by default; 1 entry when a dispatch override collapses the matrix). |
| `test-baseline` | Runs once in parallel with the matrix (not per shard) and builds and tests only `tests/Encina.UnitTests`. Diagnostic aid that shows which test fails when every shard's initial test run breaks. |
| `run-mutation-tests` | Matrix job (`fail-fast: false`, job-level `continue-on-error: true`). Each shard restores and builds only `tests/Encina.UnitTests` (Debug) and uploads `mutation-report-shard-<idx>` and `stryker-logs-shard-<idx>`. A shard failure does **not** fail the workflow. |
| `aggregate` | Downloads every `mutation-report-shard-*` artifact, applies the [guards](#guards), merges the reports (see [Merging the shard reports](#merging-the-shard-reports)) and uploads the result as a single `mutation-report` artifact (the shape `publish-mutations.yml` expects). |

### Shard definitions

The `SHARDS` array in the `select-matrix` job is the source of truth; the comments next to each entry hold the tested-mutant count that sized it. Each entry is one or more Stryker `mutate` globs separated by `;`, and a glob that starts with `!` excludes files. Rules:

- A folder with few mutants shares a shard with others (for example the shard that holds `Validation`, `Results` and `Sharding/Health`).
- A folder with too many mutants for one shard is split by file: one or more shards name its largest files, and the folder's last shard takes the folder glob minus those files. A file added to the folder later therefore always lands in some shard.
- A single file with more tested mutants than the target (`SqlServerPermissionScriptGenerator.cs` and `PostgreSqlPermissionScriptGenerator.cs` in `Modules/Isolation`) gets a shard of its own, because a file is the smallest shard unit: the aggregate merge keys reports by file.
- The counts come from CI run 36959365309 (2026-10-02), and from run 36304862790 for `Modules/Isolation`, whose shard lost its runner in the first run (#1682).

### Timeouts

Each shard's `timeout-minutes` comes from the `TIMEOUTS` array, same index as `SHARDS`. The values are provisional: they apply the [#1395](https://github.com/dlrivada/Encina/issues/1395) formula `max(60, ceil(round(minutes) x 1.5))` to an estimate of 20 minutes of setup plus the shard's tested mutants times 35.6 s. The first validation run of the new runner re-sizes them from observed shard durations. Read the per-shard values in the workflow; they are not repeated here.

### Dispatch overrides

Operators can override via the `workflow_dispatch` inputs; any of them collapses the matrix to a single shard:

| Input | Effect |
|-------|--------|
| (default) | Full 20-shard matrix. |
| `custom_scope: "<glob>"` | Single shard (index `custom`) mutating the given scope. The value takes the same form as a `SHARDS` entry: one glob, or several separated by `;` with `!` for exclusions, and it must contain at least one include glob. If it equals a `SHARDS` entry, that shard's timeout is reused; otherwise the largest configured timeout applies. |
| `diff_mode: true` | Single shard with `--since:main` — mutate only files changed vs main. Useful for PR-style validation. |
| `full_mode: true` | Single shard mutating the entire `**/*.cs` glob. Will exceed the 340-minute per-shard timeout in the current configuration. |

### Guards

- **A shard that killed no mutant fails (matrix mode only).** When a shard's report has more than 0 tested mutants (Killed, Survived or Timeout) and 0 Killed, the step "Fail a shard that killed no mutant" fails the shard. The `aggregate` job also fails without publishing when any report has that shape. In matrix mode one zero-kill shard therefore blocks the publishing of the whole run; this is by design, because nothing from a runner that cannot activate mutants is published ([#1440](https://github.com/dlrivada/Encina/issues/1440)). The guard does not apply to `custom_scope`, `diff_mode` or `full_mode` dispatches, because a small scope can legitimately kill no mutant. The VsTest runner reported 0 killed on every shard for weeks while every job stayed green ([#1440](https://github.com/dlrivada/Encina/issues/1440)).
- **Missing reports are named.** The report and summary upload steps run with `if: always()`, so a report Stryker wrote before failing is still uploaded. A shard that lost its runner, timed out before Stryker wrote the report, or failed earlier uploads nothing; the `aggregate` job then writes a `::warning` and a "Missing shard reports" block in the step summary naming every such shard, adds a "Shards missing a report" row to the aggregation table, and merges the other reports. The files of a missing shard keep their previous dashboard data ([#1682](https://github.com/dlrivada/Encina/issues/1682)). A missing report alone does not fail the merge; it fails with "No shard reports found" only when no shard uploaded one.
- **Runner resources are logged.** Each shard writes runner memory (`free -m`) and disk (`df -h /`) to `artifacts/mutation/logs/runner-resources.log` every 60 s, uploaded with the `stryker-logs-shard-<idx>` artifact. Every fifth reading is also printed to the step log with the prefix `[runner-resources]`, so the readings taken before a lost runner survive in the live job log ([#1682](https://github.com/dlrivada/Encina/issues/1682)).
- **The merge guards.** See the next section.

### Merging the shard reports

Stryker lists every file of the project it mutated in the shard's `files` map, including files that belong to other shards, with an empty `mutants` array ([#1681](https://github.com/dlrivada/Encina/issues/1681)). The `aggregate` job therefore:

1. Drops each report's entries with an empty `mutants` array before combining, so one shard's empty entry cannot overwrite another shard's entry that has the mutants.
2. Fails, naming the files, when a file has mutants in two reports (overlapping globs).
3. Combines the remaining `files` maps; the metadata (thresholds, project root and so on) comes from the first report.
4. Fails when the merged mutant count differs from the sum of the shard reports' mutant counts, so the merge neither loses nor invents mutants ([#1684](https://github.com/dlrivada/Encina/issues/1684)).

The script is the "Merge shard reports" step of the `aggregate` job in `.github/workflows/mutation-tests.yml`.

## Mutate filter

The `mutate` array in `.github/stryker-config.json` is the global allow/deny list. Patterns are interpreted **relative to the target project** (`src/Encina/Encina.csproj`, project mode), not the repository root — Stryker's behavior was unintuitive on this point and produced the original 0-mutants bug ([#957](https://github.com/dlrivada/Encina/issues/957)).

Standard exclusions (appended to every shard's `--mutate` arguments by the workflow):

```
!**/Log.cs
!**/LogMessages.cs
!**/Diagnostics/*ActivitySource.cs
!**/Diagnostics/*Metrics.cs
!**/*Errors.cs
!**/*Constants.cs
```

These exclude files where mutation testing has no value (logging surface, diagnostic plumbing, error-code/constant tables). They are not exhaustive — surviving mutants in these files are still uninteresting and should be filtered as they appear.

## Coverage analysis: why `off`

`stryker-config.json` sets `coverage-analysis: "off"` despite `perTest` being the recommended Stryker mode. The reason is the upstream xUnit v3 incompatibility documented in [stryker-mutator/stryker-net#3117](https://github.com/stryker-mutator/stryker-net/issues/3117). When the bug is fixed, switching back to `perTest` would reduce per-mutant test execution from the whole `Encina.UnitTests` project to the tests covering the mutated line, which could shrink the work per mutant, but the #1087 spike could not finish the MTP `perTest` coverage capture within its time box, so that is not measured. `perTest` coverage stays off for now; [#1026](https://github.com/dlrivada/Encina/issues/1026) stays open.

Until then, `AllTests` mode is the only working configuration.

## Runner and project mode

Since [#1441](https://github.com/dlrivada/Encina/issues/1441) Encina runs Stryker.NET 5.0.0 (pinned in `.config/dotnet-tools.json`) with the Microsoft Testing Platform runner. `.github/stryker-config.json` sets `"test-runner": "mtp"`, `coverage-analysis: "off"` and `project: Encina.csproj`, and has no `solution`, `test-projects` or `test-case-filter` key. `.github/scripts/run-stryker.cs` starts Stryker with `tests/Encina.UnitTests` as its working directory.

Why:

- **The VsTest runner kills 0 mutants under xUnit v3** ([#1440](https://github.com/dlrivada/Encina/issues/1440)). CI run 36304862790, the first complete run of the old 17-shard matrix on Stryker 4.14.0, reported 0 killed on every shard. The [#1087 spike](../engineering/stryker-5-mtp-spike-1087.md) reproduced it locally in Stryker 4.14.0 and 5.0.0. Scores the VsTest-based workflow published are not a measure of test quality, because no mutant was activated.
- **Solution mode breaks the MTP runner's initial run.** The MTP runner ignores `test-projects`, and run from the repository root it starts every test project of the solution, including `Encina.IntegrationTests` and the benchmark executable, so the initial test run fails. In project mode from `tests/Encina.UnitTests`, only that project runs.
- **`run-stryker.cs` does not pass `--log-to-file` by default.** File logging is trace level and, under MTP, writes a JSON-RPC log per test server that grows about 40 MB for each run of the whole project (58-88 MB for a 4-mutant run, measured locally). A 150-mutant shard would write gigabytes. Pass the flag through for a short diagnostic run: `dotnet run --file .github/scripts/run-stryker.cs -- --log-to-file --mutate:...`.

The MTP runner is in preview: Stryker logs "The Microsoft Test Platform testrunner is currently in preview".

### Smoke check

On 2026-10-03, on Windows with Stryker 5.0.0, the MTP runner, coverage off and project mode, this command:

```bash
dotnet run --file .github/scripts/run-stryker.cs -- --mutate:**/Dispatchers/Strategies/SequentialDispatchStrategy.cs
```

plus the standard exclusions tested 4 mutants, killed all 4 and left 0 survivors, in 8 min 41 s of wall time (the initial test run took about 4 min 40 s). It is a sanity check that mutants are activated, not a measurement.

### Test project baseline

Every mutant runs the whole `Encina.UnitTests` project, so a failing test there breaks the initial test run of every shard (`break-on-initial-test-failure`). The `test-baseline` job shows which test fails.

## History: the per-folder test filter (removed)

From [#1027](https://github.com/dlrivada/Encina/issues/1027) until [#1441](https://github.com/dlrivada/Encina/issues/1441), the workflow paired each shard folder with a test-namespace substring (a `FILTERS` array parallel to `FOLDERS`) and patched `test-case-filter` in `stryker-config.json` to `FullyQualifiedName~<substring>`, so a mutant ran about 20-500 tests instead of the whole suite. That worked only on the VsTest runner, which kills no mutants under xUnit v3. The MTP runner ignores `test-case-filter`, so the filter, the `FILTERS` and `FOLDERS` arrays and the 17 folder shards are gone; the [shard design](#matrix-execution) replaces them. The edge-case notes of that design (root-namespace folders, the empty `Results` filter of [#1401](https://github.com/dlrivada/Encina/issues/1401), the slow `Pipeline` shard) no longer apply and were removed.

## DocRef convention

Mutation results are cited from documentation via stable identifiers:

```
mut:<package>/<relative-file-path>
```

Examples:

- `mut:Encina/Sharding/Migrations/Strategies/CanaryFirstStrategy.cs`
- `mut:Encina/Pipeline/Behaviors/CommandActivityPipelineBehavior.cs`

Files in the rotation snapshot are exposed as DocRef entries by `mutation-history.cs` (it emits `docref-index.json` alongside `latest.json`). `publish-mutations.yml`'s `publish-data` job stages both under `overlay/mutations/data/` and persists them to the orphan `dashboard-data` branch (`.github/scripts/pages-dashboard-data.ps1 -Mode Persist -Domain mutations`), and its `deploy` job calls `docs.yml` — the only workflow that deploys GitHub Pages (#1381) — whose `deploy` job lays that branch over the site, so the live copy ends up at `mutations/data/docref-index.json` on Pages. The copy tracked at `docs/mutations/data/docref-index.json` is only a fallback seed and is not current. Each entry contains:

| Field | Meaning |
|-------|---------|
| `package` | Source NuGet package name (e.g. `Encina`) |
| `path` | File path relative to the package root |
| `score` | `100 × detected / total` for that file |
| `total`, `killed`, `survived`, `noCoverage`, `timeouts` | Raw counts |
| `lastRun` | ISO timestamp of the run that produced the data |
| `runId` | GitHub Actions run ID — links back to the raw artifact |
| `dashboardUrl` | Deep link into the dashboard for this file |

### Citation markers in documentation

Documentation references mutation data via two HTML comment marker forms, mirroring the performance dashboard's pattern:

**Tables** — expanded to a markdown table by `mut-docs-render.cs`:

```html
<!-- mutref-table: mut:Encina/Sharding/Migrations/Strategies/* -->
(generated table — do not edit)
<!-- /mutref-table -->
```

**Inline values** — expanded to a single metric value in prose:

```html
The current mutation score for canary-first migrations is
<!-- mutref: mut:Encina/Sharding/Migrations/Strategies/CanaryFirstStrategy.cs:score -->0.00%<!-- /mutref -->.
```

The pattern after `mutref-table:` is a glob matched against known DocRef IDs. `*` alone matches everything in the index.

### Cited-by index

`mut-docs-render.cs` builds a reverse index, `cited-by.json`, mapping each DocRef ID to the list of `path:lineNumber` locations where it is cited (markers + free-form prose mentions). It runs inside `docs.yml`'s `build-docs` job (`continue-on-error: true`, per INV-002: a rendering problem never blocks the deploy), against the `docref-index.json` staged in `_dashboards/mutations/data/`, so it writes `cited-by.json` there too; `build-docs` then copies it into the built site, which the `deploy` job publishes, so it is served live at `mutations/data/cited-by.json` on Pages. The mutation dashboard surfaces this in the "Cited in" column.

This makes documentation drift visible:

- An **orphan mutation result** is a file that has mutation data but is not cited from any doc. The dashboard does not flag this as an error — many files do not need to be documented — but the `cited-by.json` is the source of truth for "what's actually consumed".
- A **dangling citation** is a mutref in a document whose DocRef is not in the index. `mut-docs-render.cs` emits a warning row in the table and logs it.

## Recalculation

If a formula in this document changes, `mutation-history.cs` can be re-run against any historical artifact (raw `mutation-report.json` files are uploaded by `publish-mutations.yml` as `stryker-logs` artifacts; `publish-mutations.yml`'s `publish-data` job also stages a timestamped snapshot alongside `latest.json` before persisting it to the orphan `dashboard-data` branch, but `pages-dashboard-data.ps1 -Mode Persist` prunes earlier timestamped snapshots on every persist, so only the latest `mutations/data/{timestamp}.json` stays live on Pages at any time; nothing commits it to `docs/mutations/data/` in the repository).

The recalculation does NOT re-run Stryker. It re-applies the formulas to the same raw data so that historical numbers in the dashboard stay consistent with the current methodology.

## Relationship with coverage and performance

| Dashboard | What it measures | Where it lives |
|-----------|------------------|----------------|
| [Coverage](../coverage/) | Which lines are covered, by which test type ("obligations" model, see [coverage-measurement-methodology.md](coverage-measurement-methodology.md)) | `docs/coverage/` |
| [Performance](../performance/) | Stable median latency and stability under load (see [performance-measurement-methodology.md](performance-measurement-methodology.md)) | `docs/performance/` |
| [Mutations](../mutations/) | How well the existing tests detect injected defects (see this document) | `docs/mutations/` |

These three answer different questions:

- Coverage answers **"is this line tested?"**
- Mutation answers **"is the test that covers this line meaningful?"** (will it fail when the code changes?)
- Performance answers **"does this code path stay fast?"**

A high coverage score with a low mutation score indicates tests that touch the code without verifying its behavior — pure coverage padding. A low coverage score with a high mutation score is mathematically impossible (mutation requires coverage). The two are complementary, not redundant.
