# Mutation Measurement Methodology

> **Status**: Living document — updated as the methodology evolves.
> **Tracking issues**: [#957](https://github.com/dlrivada/Encina/issues/957) (workflow stabilization), [#962](https://github.com/dlrivada/Encina/issues/962) (scope widening)
> **Implementation**: `.github/scripts/run-stryker.cs`, `mutation-history.cs`, `update-mutation-summary.cs`, `mut-docs-render.cs`
> **Dashboard**: <https://dlrivada.github.io/Encina/mutations/>

## Purpose

This document explains **how** Encina measures mutation testing — the rules, formulas, and conventions adopted to produce honest, reproducible mutation data given the practical constraints of the test infrastructure (xUnit v3 + Stryker.NET 4.14 + GitHub-hosted runners).

It is the contract between the mutation infrastructure and the people who consume its output. Anyone reading a mutation score in a doc, a PR comment, or the dashboard should be able to come here, understand the formula that produced it, and be able to reproduce the result.

## Why a methodology document exists

Off-the-shelf mutation testing assumes that every commit can re-run the full mutant set against the full test suite. That is not feasible for Encina:

- The Stryker `--mutate` glob produces ~6,181 mutants for `src/Encina/Encina.csproj` alone.
- xUnit v3 + Stryker's `coverage-analysis: perTest` mode is broken upstream ([stryker-mutator/stryker-net#3117](https://github.com/stryker-mutator/stryker-net/issues/3117)). Without per-test coverage, every mutant runs the full ~23,000 test suite (`AllTests` mode).
- A full run at `~3 min/mutant` exceeds GitHub's 6-hour job limit and the 7 GB runner OOMs under Stryker's 4-way concurrency.

The result is that Encina runs **partial** mutation analysis on a rotating subset of folders, and the dashboard **accumulates** results across runs. The formulas below define exactly what that means and how to interpret the numbers.

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

Stryker's report is per-run. Encina's dashboard is per-file across runs. The `mutation-history.cs --merge-from` flag bridges the two:

1. Each weekly run mutates **one folder** (rotation of 17 entries — see [Folder rotation](#folder-rotation)).
2. `mutation-history.cs` reads the new Stryker report and the previous `latest.json`.
3. For every file in the previous snapshot **not** touched by the new run, the per-file counts are carried forward unchanged.
4. For every file the new run mutated, fresh data replaces the previous values.
5. The overall score is **recomputed** across the merged file set.

After 17 successful weekly runs, every folder has been measured at least once, and the dashboard reflects the union. A folder is allowed to "go stale" — its data shows the last time that folder was actually mutated, with no extrapolation.

This model deliberately picks **completeness over freshness**. The alternative — overwriting the dashboard with each run's narrow snapshot — would make the score swing wildly between weeks (5.08% one week, 0% the next) and lose the cumulative work.

### Live snapshot of folders measured so far

The two folders measured during the workflow stabilization (smoke test + Health):

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

## Folder rotation

`.github/workflows/mutation-tests.yml` uses ISO week number modulo 17 to pick a folder:

```bash
WEEK=$(date -u +%V)
IDX=$(( 10#$WEEK % 17 ))
SCOPE="${FOLDERS[$IDX]}"
```

The 17 entries are sized to fit the runner's 5h 50m timeout (350-minute job timeout) at `~3 min/mutant × ~80 mutants ≈ 4h with safety margin`. Each entry has at most 8 `.cs` files after exclusions.

Currently rotated folders:

- `**/Core/*.cs`
- `**/Pipeline/Behaviors/*.cs`
- `**/Pipeline/*.cs`
- `**/Dispatchers/Strategies/*.cs`
- `**/Validation/*.cs`
- `**/Sharding/Migrations/Strategies/*.cs`
- `**/Sharding/Routing/*.cs`
- `**/Sharding/ReplicaSelection/*.cs`
- `**/Sharding/Execution/*.cs`
- `**/Sharding/Shadow/**/*.cs`
- `**/Sharding/Colocation/*.cs`
- `**/Sharding/TimeBased/*.cs`
- `**/Sharding/Resharding/Phases/*.cs`
- `**/Sharding/Diagnostics/*.cs`
- `**/Modules/Isolation/*.cs`
- `**/Results/*.cs`
- `**/Sharding/Health/*.cs`

Operators can override via the `workflow_dispatch` inputs:

| Input | Effect |
|-------|--------|
| (default) | Weekly rotation. |
| `diff_mode: true` | `--since:main` — mutate only files changed vs main. Useful for PR-style validation. |
| `full_mode: true` | No `--mutate` override — mutate the entire `**/*.cs` glob. Will exceed the runner timeout in current configuration. |

## Mutate filter

The `mutate` array in `.github/stryker-config.json` is the global allow/deny list. Patterns are interpreted **relative to the target project** (`src/Encina/Encina.csproj`), not the solution root — Stryker's behavior was unintuitive on this point and produced the original 0-mutants bug ([#957](https://github.com/dlrivada/Encina/issues/957)).

Standard exclusions (carried into the rotation step's CLI overrides):

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

`stryker-config.json` sets `coverage-analysis: "off"` despite `perTest` being the recommended Stryker mode. The reason is the upstream xUnit v3 incompatibility documented in [stryker-mutator/stryker-net#3117](https://github.com/stryker-mutator/stryker-net/issues/3117). When the bug is fixed, switching back to `perTest` would reduce per-mutant test execution from ~23,000 tests to a small fraction (the tests covering the mutated line), which would in turn make full-scope runs feasible and remove the need for folder rotation.

Until then, `AllTests` mode is the only working configuration.

## DocRef convention

Mutation results are cited from documentation via stable identifiers:

```
mut:<package>/<relative-file-path>
```

Examples:

- `mut:Encina/Sharding/Migrations/Strategies/CanaryFirstStrategy.cs`
- `mut:Encina/Pipeline/Behaviors/CommandActivityPipelineBehavior.cs`

Files in the rotation snapshot are exposed as DocRef entries by `mutation-history.cs` (it emits `docs/mutations/data/docref-index.json` alongside `latest.json`). Each entry contains:

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

`mut-docs-render.cs` builds a reverse index at `docs/mutations/data/cited-by.json` mapping each DocRef ID to the list of `path:lineNumber` locations where it is cited (markers + free-form prose mentions). The mutation dashboard surfaces this in the "Cited in" column.

This makes documentation drift visible:

- An **orphan mutation result** is a file that has mutation data but is not cited from any doc. The dashboard does not flag this as an error — many files do not need to be documented — but the `cited-by.json` is the source of truth for "what's actually consumed".
- A **dangling citation** is a mutref in a document whose DocRef is not in the index. `mut-docs-render.cs` emits a warning row in the table and logs it.

## Recalculation

If a formula in this document changes, `mutation-history.cs` can be re-run against any historical artifact (raw `mutation-report.json` files are uploaded by `publish-mutations.yml` as `stryker-logs` artifacts and as snapshot copies under `docs/mutations/data/{timestamp}.json`).

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
