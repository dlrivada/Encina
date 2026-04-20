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
- A full single-job run at `~3 min/mutant` exceeds GitHub's 6-hour job limit and the 7 GB runner OOMs under Stryker's 4-way concurrency.

Encina instead runs **17 parallel shards per weekly run**, each mutating one folder of `src/Encina/` in ~10–15 min. The per-shard filter (see [#1027](https://github.com/dlrivada/Encina/issues/1027)) drops per-mutant test execution from ~23,000 tests to ~20–500 without touching Stryker's broken `perTest` path. The `aggregate` job merges the 17 shard reports into a single `mutation-report.json` that the downstream publish workflow treats as one normal Stryker run. The formulas below define exactly what the merged dataset means and how to interpret the numbers.

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

1. **Per-run aggregation** (`.github/workflows/mutation-tests.yml`): the weekly workflow fans out into a 17-shard matrix (see [Matrix execution](#matrix-execution)). An `aggregate` job then merges the 17 shard reports into a single `mutation-report.json` by taking the union of their `files` maps.
2. **Cross-run carry-forward** (`mutation-history.cs --merge-from`): `publish-mutations.yml` reads the aggregated report and the previous `latest.json`. For every file in the previous snapshot **not** touched by this run, the per-file counts are carried forward unchanged. Files the new run mutated get fresh data. The overall score is **recomputed** across the merged file set.

With matrix execution every folder gets a fresh measurement every week, so the carry-forward layer mostly handles files outside the rotation scope (or occasional intermittent shard failures). If a shard fails, the previous week's data for that folder survives until the next successful run — the dashboard never silently regresses to zero for transient infrastructure issues.

This model deliberately picks **completeness over freshness**. The alternative — overwriting the dashboard with each run's narrow snapshot — would make the score swing wildly when shards fail and lose the cumulative work.

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

## Matrix execution

`.github/workflows/mutation-tests.yml` fans the weekly run out into 17 parallel shards — one per folder in the list below — via a GitHub Actions matrix. Before [#1028](https://github.com/dlrivada/Encina/issues/1028) the workflow rotated through the same 17 entries one per week (ISO-week modulo 17), which meant each folder was measured roughly once every 4 months. With per-folder runs down to ~5–10 min after [#1027](https://github.com/dlrivada/Encina/issues/1027), running all 17 in parallel every week became feasible.

Pipeline shape:

| Job | Purpose |
|-----|---------|
| `select-matrix` | Emits the shard list as a JSON array (17 entries by default; 1 entry when a dispatch override collapses the matrix). |
| `test-baseline` | Runs once in parallel with the matrix (not per shard). Diagnostic aid that exposes which Stryker test project owns any "unable to test" failures. |
| `run-mutation-tests` | Matrix job (`fail-fast: false`, job-level `continue-on-error: true`). Each shard uploads `mutation-report-shard-<idx>`. A shard failure does **not** fail the workflow. |
| `aggregate` | Downloads every `mutation-report-shard-*` artifact, merges their `files` maps with `jq -s '(.[0]) + {files: (map(.files) | add)}'`, and uploads the result as a single `mutation-report` artifact (the shape `publish-mutations.yml` expects). |

The 17 shard scopes:

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

Each entry is sized for ≤ 8 `.cs` files after exclusions, which keeps per-shard mutant count at ~30–150 and the per-shard wall-clock time around 10–15 min (build + Stryker). The matrix runs at ~15 min end-to-end; serial runner-time is ~2 h per weekly run, well within the weekly CI budget.

Operators can override via the `workflow_dispatch` inputs; any of them collapses the matrix to a single shard:

| Input | Effect |
|-------|--------|
| (default) | Full 17-shard matrix — every folder measured fresh. |
| `custom_scope: "<glob>"` | Single shard with the given `--mutate` override. If the glob matches a rotation entry, its paired test-case-filter is reused; otherwise the config default applies. |
| `diff_mode: true` | Single shard with `--since:main` — mutate only files changed vs main. Useful for PR-style validation. |
| `full_mode: true` | Single shard mutating the entire `**/*.cs` glob. Will exceed the 60-minute per-shard timeout in the current configuration. |

## Mutate filter

The `mutate` array in `.github/stryker-config.json` is the global allow/deny list. Patterns are interpreted **relative to the target project** (`src/Encina/Encina.csproj`), not the solution root — Stryker's behavior was unintuitive on this point and produced the original 0-mutants bug ([#957](https://github.com/dlrivada/Encina/issues/957)).

Standard exclusions (carried into each shard's CLI overrides):

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

## Per-folder test filter

`AllTests` mode is Stryker's contract, but `test-case-filter` is VSTest's. Encina exploits the latter to bypass the former: the workflow pairs each rotation folder with a test-namespace substring and patches `stryker-config.json` (the `test-case-filter` property) to `FullyQualifiedName~<substring>` before invoking Stryker. Each mutant then runs only the ~20–500 tests whose FQN matches, instead of the full ~23,000. See [#1027](https://github.com/dlrivada/Encina/issues/1027) for the rationale.

> **Why patch the config file instead of passing `--test-case-filter` on the CLI?** Stryker.NET 4.14 exposes this setting only via the config file, not the command line (upstream [stryker-mutator/stryker-net#3091](https://github.com/stryker-mutator/stryker-net/issues/3091)). The workflow uses `jq` to rewrite `.github/stryker-config.json` in place before the run — CI runners are ephemeral, so the edit does not leak into tracked state.

The mapping lives in the `FILTERS` bash array in `.github/workflows/mutation-tests.yml`, parallel to `FOLDERS`. Each entry is a pipe-separated list of substrings; at runtime each substring is wrapped in `FullyQualifiedName~<substring>` and joined with `|` (OR). An empty entry means "no override — use the default filter from `stryker-config.json`" (currently all three Stryker test projects). The substrings are derived from the convention that test folders mirror source folders:

| Source folder | Test FQN segment | Filter substring |
|---|---|---|
| `src/Encina/Sharding/Migrations/Strategies/` | `Encina.UnitTests.Core.Sharding.Migrations.Strategies.*` | `Sharding.Migrations.Strategies` |
| `src/Encina/Dispatchers/Strategies/` | `Encina.UnitTests.Dispatchers.Strategies.*` | `Dispatchers.Strategies` |
| `src/Encina/Modules/Isolation/` | `Encina.UnitTests.Core.Modules.Isolation.*` | `Modules.Isolation` |

### Edge cases

- **Root-namespace folders** (`**/Core/*.cs`, `**/Pipeline/*.cs`): source files declare `namespace Encina;` with no folder-derived segment, so the filter falls back to the explicit test-folder path (e.g. `Encina.UnitTests.Core|Encina.ContractTests.Core`). This is broader than ideal — it also runs tests for sibling source folders rooted at the same test path — but it is never *narrower* than reality, which is the safety property that matters.
- **`**/Results/*.cs`**: same root-namespace situation, but there is no corresponding `*.Results.*` test namespace — tests for `EitherHelpers`, `EncinaErrors`, and `NullFunctionalFailureDetector` live in scattered domain folders. The FILTERS entry is intentionally empty so the run falls back to the config default (all three Stryker test projects). This sacrifices speed for correctness on the Results rotation only.
- **Non-recursive globs**: `**/Pipeline/*.cs` matches `src/Encina/Pipeline/*.cs` only, but the filter `Pipeline` also matches `Pipeline.Behaviors` tests. Accepted — prefer false positives (extra tests) over false negatives (missed kills).
- **Tests in unexpected locations**: `ContractTests/Database/Sharding/*` contains contract tests for routing/replica/colocation code that lives in `src/Encina/Sharding/*`. The substring `Sharding.Routing` (etc.) still matches because it is a substring of the full FQN — the mirror convention is looser than "exact namespace match".

### Custom-scope dispatch

When `workflow_dispatch` is invoked with `custom_scope`, the `select-matrix` job emits a single-shard matrix instead of the usual 17, and looks up the scope in the `FOLDERS` array to reuse its paired filter. If the custom scope doesn't match any rotation entry, the filter is empty and Stryker falls back to the default `test-case-filter` from `stryker-config.json` (which selects all tests in the three Stryker test projects).

### What this unlocks

- Per-mutant test execution drops from ~23,000 to ~20–500 → per-mutant time drops from ~3 min to ~5–10 s.
- A single folder shard completes in ~5–10 min instead of ~3 h, making the 17-shard matrix (see [Matrix execution](#matrix-execution)) cheap enough to run every week.
- Kill counts become deterministic: a targeted test added to the matching folder shows up in the next run's kill delta, instead of being drowned in the ~23,000-test noise.

### Risk: namespace drift

If a test namespace is renamed (e.g. `Encina.UnitTests.Core.Sharding.Migrations.Strategies` → something else), tests silently stop matching the filter and mutants stop being killed. The dashboard would show a regression but without an obvious cause. Mitigation: the `FILTERS` array is a concrete artifact reviewers must update alongside namespace changes. If the methodology is ever revised, prefer explicit, reviewable mappings over clever derivations.

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
