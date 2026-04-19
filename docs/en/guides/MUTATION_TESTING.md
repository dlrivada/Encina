# Encina Mutation Testing Guide

> **Methodology**: [`docs/testing/mutation-measurement-methodology.md`](../../testing/mutation-measurement-methodology.md) is the contract that defines the formulas, the accumulation model, the matrix execution model, and the citation system. This guide is the practical companion: how to run, interpret, and contribute.
>
> **Dashboard**: <https://dlrivada.github.io/Encina/mutations/>
> **Tracking**: [#957](https://github.com/dlrivada/Encina/issues/957) (workflow stabilization, closed), [#962](https://github.com/dlrivada/Encina/issues/962) (scope widening + cited-by, closed)

## What mutation testing measures

Mutation testing answers: **"are the tests that cover this line strong enough to detect a real defect there?"**. Stryker injects small semantic changes (mutants) — flipping `>` to `>=`, removing a `null` check, replacing a string literal — and reports which mutants the test suite catches (`Killed`) and which it does not (`Survived`).

A killed mutant is evidence the test would fail under that defect. A surviving mutant is evidence the test would pass even with the bug. **Surviving mutants are the actionable signal**.

This complements coverage: coverage tells you *which* lines are tested, mutation tells you *how meaningfully*. A 100% covered line whose tests miss every mutation is a 0% mutation score — pure coverage padding. See [`coverage-measurement-methodology.md`](../../testing/coverage-measurement-methodology.md) for the coverage side.

## Current state (April 2026)

The Mutation Tests workflow runs weekly via GitHub Actions on the `main` branch. It fans out into 17 parallel shards (one per folder of `src/Encina/`) and an `aggregate` job merges their reports into a single dataset. Results accumulate per-file across runs into a single dashboard ([accumulation model](../../testing/mutation-measurement-methodology.md#the-accumulation-model)).

The current accumulated snapshot:

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
- `dotnet tool restore` from the repository root (Stryker is pinned to v4.14.0 in `.config/dotnet-tools.json`).

### Quick run

The C# helper script wraps Stryker with the repo's configuration:

```bash
dotnet run --file .github/scripts/run-stryker.cs
```

Use the same `--mutate` glob one of the CI matrix shards uses to mutate just one folder:

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

The full `**/*.cs` scope works locally given enough time (~hours) but is not feasible in CI's 350-minute runner timeout under AllTests mode. See [methodology — coverage analysis: why off](../../testing/mutation-measurement-methodology.md#coverage-analysis-why-off) for the underlying constraint.

### Reports

After Stryker finishes:

- `artifacts/mutation/reports/mutation-report.json` — raw per-mutant data
- `artifacts/mutation/reports/mutation-report.html` — interactive HTML report (open in a browser)
- `artifacts/mutation/logs/log-*.txt` — Stryker's trace log (when `--log-to-file` is used; CI always uses it)

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
| `schedule` (weekly) | Matrix | All 17 folders run as parallel shards; the `aggregate` job merges their reports |
| `workflow_dispatch` (default) | Matrix | Same as schedule |
| `workflow_dispatch` `custom_scope: "<glob>"` | Custom (1 shard) | Override `--mutate` glob; matrix collapses to one shard |
| `workflow_dispatch` `diff_mode: true` | Diff (1 shard) | `--since:main` — only files changed vs main (PR-style) |
| `workflow_dispatch` `full_mode: true` | Full (1 shard) | No `--mutate` override — entire `**/*.cs`. Will exceed the timeout in current configuration. |

After Stryker completes, `.github/workflows/publish-mutations.yml` is triggered automatically. It:

1. Downloads the aggregated `mutation-report` artifact from the Mutation Tests run (produced by the `aggregate` job, which merges every shard's `mutation-report-shard-*`).
2. Fetches the previous `latest.json` from GitHub Pages and merges it (carry-forward for files this run did not touch — see [accumulation model](../../testing/mutation-measurement-methodology.md#the-accumulation-model)).
3. Runs `mut-docs-render.cs` to expand `<!-- mutref-table -->` and `<!-- mutref -->` markers in `docs/` and `src/`, and to build `cited-by.json`.
4. Commits any rendered doc changes back to `main`.
5. Deploys the dashboard to <https://dlrivada.github.io/Encina/mutations/>.

The publish guard refuses to overwrite the dashboard with a 0-mutant report (e.g. when `--since` filtered everything out).

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
