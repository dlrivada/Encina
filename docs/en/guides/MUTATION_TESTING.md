# Encina Mutation Testing Guide

> **Methodology**: [`docs/testing/mutation-measurement-methodology.md`](../../testing/mutation-measurement-methodology.md) is the contract that defines the formulas, the accumulation model, the matrix execution model, and the citation system. This guide is the practical companion: how to run, interpret, and contribute.
>
> **Dashboard**: <https://dlrivada.github.io/Encina/mutations/>
> **Tracking**: [#957](https://github.com/dlrivada/Encina/issues/957) (workflow stabilization, closed), [#962](https://github.com/dlrivada/Encina/issues/962) (scope widening + cited-by, closed)

## What mutation testing measures

Mutation testing answers: **"are the tests that cover this line strong enough to detect a real defect there?"**. Stryker injects small semantic changes (mutants) — flipping `>` to `>=`, removing a `null` check, replacing a string literal — and reports which mutants the test suite catches (`Killed`) and which it does not (`Survived`).

A killed mutant is evidence the test would fail under that defect. A surviving mutant is evidence the test would pass even with the bug. **Surviving mutants are the actionable signal**.

This complements coverage: coverage tells you *which* lines are tested, mutation tells you *how meaningfully*. A 100% covered line whose tests miss every mutation is a 0% mutation score — pure coverage padding. See [`coverage-measurement-methodology.md`](../../testing/coverage-measurement-methodology.md) for the coverage side.

## Current state

The Mutation Tests workflow runs weekly via GitHub Actions on the `main` branch. It fans out into 20 parallel shards (slices of `src/Encina/` sized by mutant count) and an `aggregate` job merges their reports into a single dataset. Results accumulate per-file across runs into a single dashboard ([accumulation model](../../testing/mutation-measurement-methodology.md#the-accumulation-model)).

The accumulated snapshot below still holds rows from 2026-04-14, produced before the migration to the MTP runner; they are not a measure of test quality, and the first run of the new runner replaces them:

<!-- mutref-table: mut:Encina/Sharding/Migrations/Strategies/* -->

| File | Score | Killed | Survived | NoCov | Total | Last run |
|------|------:|-------:|---------:|------:|------:|----------|
| <a id="mutref-mut-Encina-Sharding-Migrations-Strategies-CanaryFirstStrategy-cs"></a>[`Sharding/Migrations/Strategies/CanaryFirstStrategy.cs`](https://dlrivada.github.io/Encina/mutations/#pkg-Encina) | 0.00% | 0 | 9 | 0 | 9 | 2026-04-14 |
| <a id="mutref-mut-Encina-Sharding-Migrations-Strategies-ParallelMigrationStrategy-cs"></a>[`Sharding/Migrations/Strategies/ParallelMigrationStrategy.cs`](https://dlrivada.github.io/Encina/mutations/#pkg-Encina) | 0.00% | 0 | 21 | 0 | 21 | 2026-04-14 |
| <a id="mutref-mut-Encina-Sharding-Migrations-Strategies-RollingUpdateStrategy-cs"></a>[`Sharding/Migrations/Strategies/RollingUpdateStrategy.cs`](https://dlrivada.github.io/Encina/mutations/#pkg-Encina) | 0.00% | 0 | 15 | 0 | 15 | 2026-04-14 |
| <a id="mutref-mut-Encina-Sharding-Migrations-Strategies-SequentialMigrationStrategy-cs"></a>[`Sharding/Migrations/Strategies/SequentialMigrationStrategy.cs`](https://dlrivada.github.io/Encina/mutations/#pkg-Encina) | 21.43% | 3 | 11 | 0 | 14 | 2026-04-14 |

*4 file(s) matched `mut:Encina/Sharding/Migrations/Strategies/*`. Data from [mutations dashboard](https://dlrivada.github.io/Encina/mutations/). See [mutation-measurement-methodology.md](../../testing/mutation-measurement-methodology.md).*

<!-- /mutref-table -->

<!-- mutref-table: mut:Encina/Sharding/Health/* -->

| File | Score | Killed | Survived | NoCov | Total | Last run |
|------|------:|-------:|---------:|------:|------:|----------|
| <a id="mutref-mut-Encina-Sharding-Health-ShardHealthResult-cs"></a>[`Sharding/Health/ShardHealthResult.cs`](https://dlrivada.github.io/Encina/mutations/#pkg-Encina) | 0.00% | 0 | 12 | 0 | 12 | 2026-04-14 |
| <a id="mutref-mut-Encina-Sharding-Health-ShardReplicaHealthCheck-cs"></a>[`Sharding/Health/ShardReplicaHealthCheck.cs`](https://dlrivada.github.io/Encina/mutations/#pkg-Encina) | 0.00% | 0 | 29 | 0 | 29 | 2026-04-14 |
| <a id="mutref-mut-Encina-Sharding-Health-ShardedHealthSummary-cs"></a>[`Sharding/Health/ShardedHealthSummary.cs`](https://dlrivada.github.io/Encina/mutations/#pkg-Encina) | 0.00% | 0 | 12 | 0 | 12 | 2026-04-14 |

*3 file(s) matched `mut:Encina/Sharding/Health/*`. Data from [mutations dashboard](https://dlrivada.github.io/Encina/mutations/). See [mutation-measurement-methodology.md](../../testing/mutation-measurement-methodology.md).*

<!-- /mutref-table -->

The full per-package and per-file picture lives on the [dashboard](https://dlrivada.github.io/Encina/mutations/).

## Running locally

### Prerequisites

- .NET 10 SDK installed.
- `dotnet tool restore` from the repository root (Stryker.NET is pinned to v5.0.0 in `.config/dotnet-tools.json`). It runs with the Microsoft Testing Platform runner, which Stryker still labels a preview.

### Quick run

The C# helper script wraps Stryker with the repo's configuration (`.github/stryker-config.json`: project mode on `Encina.csproj`, MTP runner, coverage analysis off). It runs Stryker from `tests/Encina.UnitTests`, so run the command below from the repository root. Extra arguments passed after `--` reach Stryker unchanged, so relative paths in them resolve against `tests/Encina.UnitTests`; use absolute paths.

```bash
dotnet run --file .github/scripts/run-stryker.cs
```

Pass `--mutate` globs to mutate just one slice, as the CI matrix shards do. Every mutant runs the whole `Encina.UnitTests` project (the MTP runner ignores `test-case-filter`), so a run is slow even for a few mutants: the initial test run alone took about 4 min 40 s in a smoke check on 2026-10-03 (Windows). Start with one file:

```bash
dotnet run --file .github/scripts/run-stryker.cs -- --mutate:**/Dispatchers/Strategies/SequentialDispatchStrategy.cs
```

That run (plus the standard exclusions) tested 4 mutants, killed 4 and took 8 min 41 s. The script forwards `-c`/`--configuration` to Stryker only when you pass it; otherwise Stryker builds the project's default configuration (Debug), which the CI Build step pre-builds (a Release build was not materially faster in a local comparison, so Debug stays). To mimic a CI shard, pass its globs and the standard exclusions; the shard definitions are the `SHARDS` array in `.github/workflows/mutation-tests.yml`. A glob there may end with a span suffix `{start..end}` that limits it to a character range of the file (character offsets, not line numbers; see [methodology — span splits](../../testing/mutation-measurement-methodology.md#span-splits-of-one-file)); no shard uses one yet:

```bash
dotnet run --file .github/scripts/run-stryker.cs -- \
  "--mutate:**/Sharding/Migrations/Strategies/*.cs" \
  "--mutate:!**/Log.cs" \
  "--mutate:!**/LogMessages.cs" \
  "--mutate:!**/Diagnostics/*ActivitySource.cs" \
  "--mutate:!**/Diagnostics/*Metrics.cs" \
  "--mutate:!**/*Errors.cs" \
  "--mutate:!**/*Constants.cs"
```

The full `**/*.cs` scope is not feasible in one CI shard in AllTests mode: a `full_mode` dispatch collapses the matrix to a single shard and exceeds its 340-minute timeout. See [methodology — coverage analysis: why off](../../testing/mutation-measurement-methodology.md#coverage-analysis-why-off) and [methodology — runner and project mode](../../testing/mutation-measurement-methodology.md#runner-and-project-mode) for the underlying constraints.

### Reports

After Stryker finishes:

- `artifacts/mutation/reports/mutation-report.json` — raw per-mutant data
- `artifacts/mutation/reports/mutation-report.html` — interactive HTML report (open in a browser)
- `artifacts/mutation/logs/log-*.txt` — Stryker's trace log, only when you pass `--log-to-file` (`dotnet run --file .github/scripts/run-stryker.cs -- --log-to-file --mutate:...`). The script does not pass it by default, and CI does not use it: under the MTP runner it also writes a JSON-RPC log per test server that grows about 40 MB per run of the whole test project, so use it for short diagnostic runs only.
- `artifacts/mutation/logs/stryker-console.log` and the `runner-resources*.log` files — written by the CI shards only (uploaded with the `stryker-logs-shard-<idx>` artifact): Stryker's console output, and the runner readings taken every minute (`runner-resources.log` holds every full reading, `runner-resources-latest.log` the latest, `runner-resources-history.log` one compact line per minute). Every fifth reading is also printed to the step log with the prefix `[runner-resources]`. What the readings contain and why: [methodology — runner protection and telemetry](../../testing/mutation-measurement-methodology.md#runner-protection-and-telemetry)

A local run uses the configuration's concurrency of 1 and no memory cap. The CI step sets `DOTNET_GCHeapHardLimit` to 4 GiB; to reproduce a CI out-of-memory failure (the CI step fails a shard whose Stryker console shows an `OutOfMemoryException` and does not upload its report), set that variable (`0x100000000`, hexadecimal bytes) in your shell before running `run-stryker.cs`. Do not pass `-c 1` to lower the concurrency: the script reads `-c` as `--configuration`, so it would select a build configuration named "1".

The C# script `.github/scripts/update-mutation-summary.cs` parses the JSON and writes a concise text summary to stdout.

## Interpreting results

| Status | Meaning | Action |
|--------|---------|--------|
| `Killed` | A test failed when the mutant was injected — good. | Nothing. |
| `Survived` | All tests passed even with the mutant. | Add or strengthen a test that detects the change. |
| `NoCoverage` | No test covers the mutated line at all. | Add coverage first; mutation comes after. |
| `Timeout` | A test did not finish (often an infinite loop induced by the mutant). | Counts as detected, no action needed unless the timeout is suspicious. |
| `CompileError` | The mutant produced uncompilable code — Stryker's fault, not the test suite's. | Excluded from the score; ignore. |
| `Ignored` | Excluded by the `mutate` filter or the `since` filter. | Excluded from the score; ignore. |

The score formula is:

```
mutation_score = 100 × (Killed + Timeout + RuntimeError) / (Killed + Survived + NoCoverage + Timeout + RuntimeError)
```

Compile errors and ignored mutants are not in the denominator. The full reasoning is in [`mutation-measurement-methodology.md`](../../testing/mutation-measurement-methodology.md#the-mutation-score-formula).

## Test quality patterns

Encina ships two attributes in `Encina.Testing.Mutations` to document mutation-related decisions in test code.

### `[NeedsMutationCoverage]`

Mark a test that has a known surviving mutant whose detection requires a stronger assertion. The reason and (optionally) the mutant ID, source file, and line.

```csharp
using Encina.Testing.Mutations;

[Fact]
[NeedsMutationCoverage("Boundary not verified — survived arithmetic mutation on line 45",
    MutantId = "280", SourceFile = "src/Calculator.cs", Line = 45)]
public void Calculate_BoundaryValue_ShouldReturnExpectedResult()
{
    // ...
}
```

The attribute is a TODO, not a forever marker — once the mutant is killed, remove it.

### `[MutationKiller]`

Mark a test that was specifically written to kill a particular mutation type. Useful for documenting *why* a test exists when its purpose isn't obvious from its assertions.

```csharp
[Fact]
[MutationKiller("EqualityMutation", Description = "Verifies >= is not mutated to >")]
public void IsAdult_ExactlyEighteen_ShouldReturnTrue()
{
    var person = new Person { Age = 18 };
    person.IsAdult().ShouldBeTrue(); // catches >= -> > mutation
}
```

### Common mutation types worth targeting

- **EqualityMutation**: `==`↔`!=`, `<`↔`<=`, `>`↔`>=` — kill with exact-boundary tests
- **ArithmeticMutation**: `+`↔`-`, `*`↔`/`, `%`↔`*` — kill with non-zero, non-identity inputs
- **BooleanMutation**: `true`↔`false`, `&&`↔`||` — kill with cases where each branch matters
- **UnaryMutation**: `-x`↔`x`, `!x`↔`x`, `++`↔`--` — kill with sign/parity-sensitive assertions
- **NullCheckMutation**: `x == null` ↔ `x != null` — kill with both-null-and-non-null cases
- **StringMutation**: `""` ↔ `"Stryker was here!"` — kill by asserting on actual content, not just length
- **LinqMutation**: `First()`↔`Last()`, `Any()`↔`All()` — kill with multi-element collections where the difference matters
- **BlockRemoval**: removing entire statements — kill by asserting on side-effects of the removed code

## Workflow

The Mutation Tests workflow (`.github/workflows/mutation-tests.yml`) runs Friday at 03:00 UTC plus on `workflow_dispatch`. Execution modes:

| Trigger | Mode | Scope |
|---------|------|-------|
| `schedule` (weekly) | Matrix | All 20 shards run in parallel; the `aggregate` job merges their reports |
| `workflow_dispatch` (default) | Matrix | Same as schedule |
| `workflow_dispatch` `custom_scope: "<glob>"` | Custom (1 shard) | Override the scope: one glob, or several separated by `;` with `!` for exclusions (same form as a `SHARDS` entry); matrix collapses to one shard |
| `workflow_dispatch` `diff_mode: true` | Diff (1 shard) | `--since:main` — only files changed vs main (PR-style) |
| `workflow_dispatch` `full_mode: true` | Full (1 shard) | Explicit `--mutate:**/*.cs` plus the workflow exclusion list (mirrors the configured full-project mutate scope). Will exceed the timeout in current configuration. |

After Stryker completes, `.github/workflows/publish-mutations.yml` is triggered automatically. It:

1. Downloads the aggregated `mutation-report` artifact from the Mutation Tests run (produced by the `aggregate` job, which merges every shard's `mutation-report-shard-*`).
2. Fetches the previous `latest.json` from GitHub Pages and merges it (carry-forward for files this run did not touch — see [accumulation model](../../testing/mutation-measurement-methodology.md#the-accumulation-model)).
3. Runs `mut-docs-render.cs` to expand `<!-- mutref-table -->` and `<!-- mutref -->` markers in `docs/` and `src/`, and to build `cited-by.json`.
4. Commits any rendered doc changes back to `main`.
5. Deploys the dashboard to <https://dlrivada.github.io/Encina/mutations/>.

The publish guard refuses to overwrite the dashboard with a 0-mutant report (e.g. when `--since` filtered everything out).

When a run looks wrong, check the Mutation Tests run summary first:

- In the default matrix mode, a shard that tested mutants and killed none fails (step "Fail a shard that killed no mutant"), and the `aggregate` job refuses to publish a report of that shape, so one such shard blocks the whole run; it means the runner did not activate the mutants. `custom_scope`, `diff_mode` and `full_mode` dispatches are not guarded, because a small scope can legitimately kill none.
- A "Missing shard reports" block and a "Shards missing a report" row in the aggregate summary name every shard that uploaded no report (lost runner, timeout or earlier failure). The files of a missing shard keep their previous dashboard data; a file split between two shards is published with the half that ran.
- For a lost runner, GitHub keeps no job log, but each shard publishes a check run named "mutation telemetry (shard N)" on the commit (N is the shard index, or `custom`, `full` or `diff`). Open the commit's Checks list (or the run's checks): its summary holds the last compact readings (load, memory, swap, top process, `dotnet` and `Encina.UnitTests` process counts) and its text the latest full reading. The check run ends with the conclusion `neutral` because it is a record, not a verdict. For a shard that finished, the `stryker-logs-shard-<idx>` artifact has the same readings in full.
- A failure that names a file and a mutant key means two shard reports hold the same mutant: two `SHARDS` entries overlap, or the two halves of a span-split file do not pair.
- A shard that hits its timeout: the `TIMEOUTS` are provisional, see [methodology — timeouts](../../testing/mutation-measurement-methodology.md#timeouts).

Why each guard exists: [methodology — guards](../../testing/mutation-measurement-methodology.md#guards).

## Citing mutation data in documentation

Reference mutation results from any Markdown file using HTML comment markers. The `mut-docs-render.cs` step in the publish workflow expands them automatically.

**Tables** (rendered in-place):

```html
<!-- mutref-table: mut:Encina/Pipeline/Behaviors/* -->
(generated table — do not edit)
<!-- /mutref-table -->
```

The pattern after `mutref-table:` is a glob matched against DocRef IDs in `docs/mutations/data/docref-index.json`. `*` alone matches everything.

**Inline values** (single metric in prose):

```html
The current mutation score for the canary-first migration strategy is
<!-- mutref: mut:Encina/Sharding/Migrations/Strategies/CanaryFirstStrategy.cs:score -->0.00%<!-- /mutref -->.
```

**Free-form prose mentions** are also captured by the cited-by index. Writing `mut:Encina/Pipeline/Behaviors/CommandActivityPipelineBehavior.cs` anywhere in a markdown file outside a code fence will register that doc as a citing location for that file (provided it's in the index).

The dashboard's "Cited In" column shows the inverse view: for any file with mutation data, which docs cite it.

See [methodology — DocRef convention](../../testing/mutation-measurement-methodology.md#docref-convention) for the full specification.

## When to write a mutation-killing test

Not every survivor is worth chasing. Apply judgment:

- **Always kill** survivors in `Sharding`, `Pipeline`, `Validation`, `Errors`, and any code path with security or financial impact.
- **Usually kill** survivors in `Modules`, `Dispatchers`, `Core` — these are framework hot paths used by every consumer.
- **Equivalent mutants** (mutations that produce semantically identical code) are unkillable. Document with `[NeedsMutationCoverage("equivalent mutant: <reason>")]` if the noise is recurring.
- **Trivial code** (logging, diagnostic plumbing, error-code tables) is excluded by the `mutate` filter — see the exclusion list in [methodology — mutate filter](../../testing/mutation-measurement-methodology.md#mutate-filter).

The dashboard's per-file score is the reliable signal: anything in the `mut:Encina/...` index with a low score and a non-trivial mutant count is fair game.

## Related references

- [Mutation measurement methodology](../../testing/mutation-measurement-methodology.md) — formulas, accumulation, citation system
- [Coverage measurement methodology](../../testing/coverage-measurement-methodology.md) — the obligations model and how coverage and mutation differ
- [Performance measurement methodology](../../testing/performance-measurement-methodology.md) — the docref system this one is modelled after
- [Stryker.NET docs](https://stryker-mutator.io/docs/stryker-net/introduction/)
- [Issue #957](https://github.com/dlrivada/Encina/issues/957) — workflow stabilization (closed)
- [Issue #962](https://github.com/dlrivada/Encina/issues/962) — scope widening + cited-by (closed)
