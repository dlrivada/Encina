# Performance Measurement Methodology

> **Status**: Living document — updated as the methodology evolves with accumulated data.
> **ADR**: [025 — Performance Measurement Infrastructure](../architecture/adr/025-performance-measurement-infrastructure.md)
> **Plan**: [`performance-infrastructure-plan.md`](../plans/performance-infrastructure-plan.md)

## Purpose

This document explains **how** Encina measures performance — the rules, formulas, and conventions the team has adopted to produce honest, reproducible, traceable data. It is written for two audiences:

1. **Users of Encina** who need to verify that the numbers cited in documentation are trustworthy, understand their uncertainty, and reason about whether a benchmark's conditions match their production environment.
2. **Contributors to Encina** who need to add new benchmarks, interpret regressions, extend the measurement infrastructure, or change the formulas and be confident that history will remain consistent.

The content of this document is the contract between the measurement infrastructure and everyone who consumes its output. Changes to formulas here require a `perf-recalculate.cs` run across historical raw data so that dashboards and documentation reflect the new rules uniformly.

## Why a methodology document exists

Off-the-shelf benchmark tooling (BenchmarkDotNet, NBomber) produces raw numbers. Turning those numbers into *decisions a user can trust* requires answering questions that no tool answers by itself:

- What does the reported mean latency actually mean when the CI runner was a shared cloud VM that day?
- Is a 7% increase week-over-week a regression, or is it within the noise envelope of the measurement environment?
- How do we compare a measurement from a Windows Server 2022 runner to one from Ubuntu 24.04?
- When we change the way we compute confidence intervals, does the history become inconsistent — or can we rerun the new formula against the old data?
- When the documentation says "X is 3× faster than Y", what is the statistical weight behind that claim and can the reader inspect it?

These questions have answers, and the answers are not always the same across the industry. Encina picks a specific set, documents them here, versions them in git alongside the scripts that implement them, and retroactively applies them to history via `perf-recalculate.cs`. This is the same approach Encina used for code coverage (see ADR-023): the "obligations model" for coverage is a particular formula not offered by SonarCloud or Codecov out of the box, but it is the one that matches Encina's reality of 108 packages with very different testability profiles. Performance measurement faces analogous tradeoffs and needs the same kind of explicit, versioned treatment.

## Measurement environment

All performance measurements published to the dashboards are produced on **GitHub-hosted runners**. No self-hosted runners are used (see ADR-025 §6 for the rationale). This has consequences that the methodology treats as first-class:

- **Hardware is nondeterministic.** GitHub runners are VMs on shared infrastructure. CPU frequency scaling, noisy-neighbor contention, background maintenance tasks, and thermal throttling all contribute to run-to-run variance that cannot be eliminated.
- **Runner generations change.** Microsoft updates the underlying hardware periodically. A benchmark that ran on `ubuntu-22.04` with one CPU may run on a different CPU after a platform refresh, producing a step change in numbers that is not a regression in Encina's code.
- **Queue times are nonzero.** Windows runners tend to queue longer than Linux, which is one reason the workflow preferentially targets `ubuntu-latest`.

The methodology addresses these by (a) **quantifying uncertainty explicitly**, (b) **recording runner metadata with every measurement**, and (c) **using change-aware detection** that subtracts infrastructure shifts from genuine code regressions.

## Metrics collected

### Benchmarks (BenchmarkDotNet)

For every benchmark method, each run records:

| Metric | Unit | Source |
|---|---|---|
| `Mean` | ns/µs/ms (autoscale) | BenchmarkDotNet report |
| `Median` | same as Mean | BenchmarkDotNet report |
| `StdDev` | same as Mean | BenchmarkDotNet report |
| `Min` | same as Mean | BenchmarkDotNet report |
| `Max` | same as Mean | BenchmarkDotNet report |
| `CI99Lower`, `CI99Upper` | same as Mean | computed from `StdDev` and `N` |
| `N` (iteration count) | count | BenchmarkDotNet report |
| `AllocatedBytesPerOp` | bytes | `[MemoryDiagnoser]` |
| `Gen0`, `Gen1`, `Gen2` | count per 1k ops | `[MemoryDiagnoser]` |
| `CoefficientOfVariation` | dimensionless | computed as `StdDev / Mean` |

**Why Median alongside Mean.** Mean is sensitive to outliers introduced by runner variance (one slow iteration pulls the mean up). Median is robust. The dashboard shows both; headline citations in documentation use Median by default because it represents the *typical* measurement under the observed noise better than Mean.

**Why CI99 and not CI95.** Cloud-runner variance is wider than lab variance. A 95% interval is too narrow to capture typical noise swings; regression detection based on it fires too often on non-regressions. Encina uses 99% intervals for decisions and shows them on graphs. This is a deliberate, conservative choice that trades sensitivity for trustworthiness.

### Load tests (NBomber)

For every scenario and provider combination, each run records:

| Metric | Unit | Source |
|---|---|---|
| `RequestCount`, `Ok`, `Fail` | count | NBomber summary |
| `Mean`, `Median` | ms | NBomber summary |
| `P50`, `P95`, `P99` | ms | NBomber summary |
| `StdDev` | ms | computed from raw samples in NBomber CSV |
| `Min`, `Max` | ms | NBomber summary |
| `RPS` (requests per second) | ops/s | NBomber summary |
| `ErrorRate` | fraction | `Fail / RequestCount` |
| `MeanCpuPercent` (runner) | % | `collect-load-metrics.cs` |
| `PeakWorkingSetMB` (runner) | MB | `collect-load-metrics.cs` |

**Why percentiles for loads but not benchmarks.** Benchmarks measure a single operation in isolation; a single-value distribution collapses to mean/median/stddev. Load tests drive sustained concurrent traffic where tail latency matters far more than average — a system with 1 ms mean and 500 ms P99 behaves very differently in production than a system with 5 ms mean and 10 ms P99. Headline load-test citations in documentation include at least `{Mean, P95, P99}` and the ErrorRate.

### Metadata recorded with every run

Every snapshot stored in `perf-raw` includes a `metadata.json` with:

- `runId` (GitHub Actions run ID)
- `sha` (git commit)
- `timestamp` (ISO-8601, UTC)
- `workflow` (name of the workflow that produced the snapshot)
- `eventType` (schedule, workflow_dispatch, push, pull_request)
- `runner.os` (`ubuntu-latest`, `windows-latest`, etc.)
- `runner.osVersion` (actual kernel/OS version at run time)
- `runner.arch` (`x64`, `arm64`)
- `runner.cpuModel` (read from `/proc/cpuinfo` or `wmic` at run time)
- `runner.cpuCount`
- `runner.memoryMB`
- `dotnetVersion`
- `benchmarkDotNetVersion` (for benchmarks)
- `nbomberVersion` (for loads)
- `jobType` (`dry`, `short`, `medium`, `default`)
- `fingerprint` (project-graph hash, see §Fingerprinting)
- `stabilityFlags` (list of benchmarks marked Unstable for this run)

Metadata is never discarded. `perf-recalculate.cs` can filter history by any of these fields — for example, "show me the mean latency of `bench:mediator/send-command` excluding any run where the runner CPU model was X".

## Uncertainty and stability

### Coefficient of variation (CoV)

For every benchmark method, the system computes `CoV = StdDev / Mean`. This is a unitless measure of relative noise. It drives a two-tier stability rule that accounts for BenchmarkDotNet's habit of auto-shortening very fast benchmarks to N < 10 iterations:

- **N < 3**: always **Unstable**. A single-digit sample carries no statistical weight.
- **3 ≤ N < 10**: **Stable** iff `CoV ≤ 0.03` (3%). The stricter bar compensates for the smaller sample size — a benchmark with N=9 and CoV=0.5% is actually more trustworthy than one with N=30 and CoV=8%, and the old flat `N < 10 → unstable` rule rejected dozens of genuinely tight sub-microsecond benchmarks (`IdGeneration.Ulid`, `MessagingEncryption.Decrypt_Short`, `DistributedLock.AcquireAndRelease`, etc.) as false positives.
- **N ≥ 10**: **Stable** iff `CoV ≤ 0.10` (10%).

Stable methods are suitable for citation in documentation; trend regressions on stable methods are treated as signal. Unstable methods are still executed, still displayed on the dashboard, but excluded from `DocRef` citations and not considered for regression alerts. The dashboard marks these with an amber indicator and a tooltip explaining why.

The 3% and 10% thresholds are starting points. After one month of data the team reviews the distribution of CoV values across all benchmarks and adjusts if warranted. Any adjustment triggers a full `perf-recalculate.cs` run so history remains internally consistent.

### Stability overrides (expected-unstable)

Some benchmarks measure operations whose variance is a **property of what they measure** rather than a flaw in the benchmark design:

- **Lock / semaphore contention**: `Polly.BulkheadBenchmarks.GetMetrics` repeatedly reads a `SemaphoreSlim` counter under concurrent updates — the CoV is inherently high and no amount of extra iterations will settle it.
- **Multi-key concurrent writes**: `Caching.CacheInvalidationBenchmarks.Invalidation_MultipleKeys(keyCount=25)` races on a shared `ConcurrentDictionary`; each run sees different collision patterns.
- **Sub-nanosecond jitter**: `Encina.Benchmarks.CacheOptimizationBenchmarks.TypeCheck_Direct` measures a single `is` pattern at < 1 ns, where normal CPU jitter dominates the measurement.
- **Async-scheduler variance**: `Refit.RestApiRequestHandlerBenchmarks.*` go through the task scheduler, which on a shared runner has real variance unrelated to Refit itself.

Marking these methods as Unstable would pollute the headline stability percentage and mask real regressions in other benchmarks. Instead, per-project `.github/perf-manifest/<Project>.Benchmarks.json` files expose a `stabilityOverrides` map that lists known-noisy entries and the reason:

```json
"stabilityOverrides": {
  "$comment": "Keys match 'ClassName.Method' or 'ClassName.Method(Params)'.",
  "CacheOptimizationBenchmarks.TypeCheck_Direct": "Sub-nanosecond type check dominated by CPU jitter; CoV ~93% even at N=26.",
  "CacheInvalidationBenchmarks.Invalidation_MultipleKeys(keyCount=25)": "Multi-key cache invalidation races on the underlying ConcurrentDictionary; CoV 28-41% at N=30."
}
```

`perf-report.cs` reads these at report time and marks matching benchmarks with `expectedUnstable: true` and `expectedUnstableReason: "..."`. The headline overall counts on the dashboard track three buckets — `stableMethods`, `unstableMethods`, `expectedUnstableMethods` — and the stability ratio is computed as `stable / (stable + unstable)`, ignoring expected-unstable so the percentage reflects benchmarks whose stability is actually a signal.

**When to add an override**:
- The CoV is driven by contention / scheduling / sub-ns jitter, not by the code path under test.
- You have at least one run with N ≥ 15 to confirm the noise does not collapse with more samples.
- You can explain in one sentence why the variance is inherent.

**When NOT to add an override**:
- The CoV is borderline (3–8%) and the benchmark is a single-threaded CPU operation — it probably just needs more iterations via a class-level `[SimpleJob(iterationCount: 15)]`. This is the "Palanca 2" lever: tune at the source, not at the manifest.
- The benchmark is genuinely broken (warm-up missing, stateful between iterations). Fix the benchmark.
- You want to silence an unexpected regression. Overrides are not a way to hide failing benchmarks.

### Confidence intervals

Each metric is accompanied by a 99% confidence interval computed assuming a Student's *t* distribution over the sample iterations. The formula:

```
CI99 = Mean ± t(0.005, N-1) × (StdDev / √N)
```

Where `t(0.005, N-1)` is the critical *t* value for a two-tailed 99% interval with `N-1` degrees of freedom. The dashboard shows `[CI99Lower, CI99Upper]` as a shaded band on every trend graph.

This is computed from BenchmarkDotNet's `N` (number of iterations per workload, which is determined by `[SimpleJob]` or inferred from target time) and the reported `StdDev`. When `N` is below 10 the CI is considered unreliable and the method is treated as Unstable irrespective of CoV.

### Which `job_type` publishes to the dashboard

The `Benchmarks` workflow exposes a `job_type` input with four values that map to BDN run strategies of increasing cost:

| `job_type` | BDN iterations (`N`) | Stable results possible? | Publishes to dashboard? |
|---|:---:|:---:|:---:|
| `dry` | 1 | No | No |
| `short` | 3 | No | **No** |
| `medium` | 15 | Yes | Yes |
| `default` | ~20–35 (auto) | Yes | Yes |

Because the stability rule forces `N < 10` into the Unstable bucket regardless of CoV, running the workflow with `job_type=short` produces ~100 % Unstable results by design. **Publishing those to the dashboard would overwrite the last good snapshot with a fake regression.** To prevent that, `publish-benchmarks.yml` reads `metadata.jobType` from `latest.json` and early-exits the publish job when the value is `short` or `dry`.

Practical consequences:

- A manual dispatch with `job_type=short` still runs (fast feedback, < 15 min for the full matrix), still archives raw BDN outputs to the `perf-raw` branch, and still uploads artifacts to the workflow run page — but **does not touch `docs/benchmarks/data/latest.json`, `history.json`, or the Pages dashboard**.
- A manual dispatch with `job_type=medium` or `default` (the workflow_dispatch default is now `medium`) publishes normally.
- Scheduled Sunday runs use `medium` and publish normally.
- Pull requests use `short` (fast validation) and are filtered out of `publish-benchmarks.yml` anyway (which only triggers on `branches: [main]`).

If you run a `short` dispatch and later decide the measurements were valuable after all, re-run the same commit with `job_type=medium` — the fingerprint diff ensures only changed modules actually execute, so the recovery is cheap.

### Regression detection

A regression is declared when **all** of the following hold:

1. The new Median for a method falls **outside** the previous run's `[CI99Lower, CI99Upper]` band
2. The direction of the change is worse (higher latency or allocation, lower throughput)
3. The method is classified as Stable in the current run
4. The same shift persists across **at least two consecutive runs** — a single-run excursion is treated as noise and surfaced as a warning but not a regression

This four-condition rule is deliberately conservative. It is designed to avoid the two typical failure modes of naive regression detection:

- **False positives from noise**: runner variance alone can cause 5-15% swings on some methods. A simple "mean increased by X%" check fires constantly on such methods, erodes trust, and causes people to ignore real regressions.
- **False negatives from gradual drift**: a method that slowly degrades 2% per run for 10 runs shows no single alarming delta but ends up 22% slower. The two-consecutive-runs rule plus the absolute CI99 band eventually catches this as the band from N-2 no longer contains the current Median.

The current implementation plans to surface three signal classes on the dashboard: **Regression** (all four conditions met), **Drift warning** (condition 1 met once), **Stable** (no signal).

### Infrastructure shifts

When metadata indicates a runner platform change (new CPU model, new OS version) that affects many benchmarks simultaneously, the system detects it as a **synchronized shift** rather than per-method regressions. The detection heuristic: if more than 30% of stable benchmarks shift their Median in the same direction within one run, and the shift size is within typical platform-change magnitudes (±10-20%), the dashboard labels that run as a platform-shift point and the regression detector is suppressed for it. Subsequent runs establish a new baseline.

This is one of the "particular formulas" that off-the-shelf tooling does not provide. It exists because Encina's measurement environment (shared cloud runners) genuinely experiences these shifts and pretending otherwise produces misleading alerts.

## Normalization across environments

Documentation citations need to compare numbers obtained under different conditions. The methodology **does not normalize across runners** — it does the opposite: every citation is explicit about its environment.

Every generated documentation table includes a footer row with:

- Runner OS and architecture
- CPU model
- .NET version
- BenchmarkDotNet/NBomber version
- Date of the run
- Link to the dashboard entry and to the raw data in `perf-raw`

If a reader wants to compare two numbers from different environments, they see the difference of environments plainly and can decide for themselves whether the comparison is meaningful. The methodology refuses to silently "normalize" one number to match another's conditions because doing so would be exactly the kind of invented data this infrastructure exists to prevent.

## Fingerprinting: when to re-run a benchmark

A benchmark is re-executed when its **project-graph fingerprint** has changed. The fingerprint is computed as follows:

1. `dotnet msbuild /t:ResolveProjectReferences` enumerates all transitive `ProjectReference` dependencies of the benchmark project
2. For each project in the closure, all `.cs` files under its root are enumerated
3. The files are sorted lexicographically and their contents are hashed together (SHA-256)
4. The final hash is the fingerprint

If the current fingerprint matches the last successful run's fingerprint (stored in `perf-raw/.../metadata.json`), the run is **skipped** and the previous measurement is carried forward into the new snapshot. The dashboard does not distinguish skipped runs from executed ones visually because the carried-forward data is just as valid as a re-execution — the source is provably unchanged.

When `NuGet` package versions change (via `Directory.Packages.props`), the fingerprint does not automatically change because it only hashes `.cs` files. To detect package updates, the `Directory.Packages.props` hash is included as a secondary input to the fingerprint. This is documented here rather than buried in script comments because it affects the reader's understanding of when "no change" is truly no change.

## DocRef convention

Every benchmark method or load-test scenario that is cited in documentation carries a hierarchical identifier:

```
bench:<area>/<name>
load:<area>/<name>
```

Examples:

- `bench:mediator/send-command` — Mediator.Send measured in Encina.Benchmarks
- `bench:mediator/publish-notification`
- `bench:cache/command-hit`
- `bench:mediator/send-command`
- `bench:security/encrypt-string-short`
- `load:caching/redis`
- `load:messaging/inmemorybus`
- `load:database/uow`

The identifier is attached to benchmarks via `[BenchmarkCategory("DocRef:bench:mediator/send-command")]` and to NBomber scenarios via a constant on the scenario class. The `perf-report.cs` generator reads these annotations and builds a reverse index mapping every DocRef to its current value, uncertainty, dashboard URL, and list of citing documents.

### Citing in documentation

A documentation table that cites benchmark data is wrapped in generator markers:

```markdown
<!-- docref-table: bench:mediator/* -->
| DocRef | Method | Median | CI99 | Allocated |
|--------|--------|--------|------|-----------|
| *generated content* | | | | |
<!-- /docref-table -->
```

On every publish, `perf-docs-render.cs` scans for marker pairs and regenerates the enclosed table from the latest data. Hand-editing inside the markers is overwritten on the next run. This is the enforcement mechanism that keeps documentation from drifting.

### Orphan and dangling DocRefs

- An **orphan benchmark** is one whose DocRef is not cited in any `docs/**/*.md`. `perf-manifest-check.cs` flags it as a candidate for removal but does not fail the build — benchmarks can exist for development purposes without being consumer-facing data.
- A **dangling citation** is a DocRef mentioned in a document that has no matching benchmark or scenario. This **fails** the publish workflow. It indicates either a typo, a renamed benchmark, or a deletion that left docs inconsistent.

## Recalculation

`perf-recalculate.cs` is the piece of this infrastructure that most directly addresses the "invented data" problem. When any of the formulas in this document change — CoV threshold, CI level, regression rule, metric set, normalization approach — the script:

1. Reads every commit from the `perf-raw` branch
2. Descends into `benchmarks/<YYYY-MM-DD>/<runId>/` and `load-tests/<YYYY-MM-DD>/<runId>/`
3. Parses the raw BenchmarkDotNet CSVs and NBomber JSONs found there
4. Applies the **current** formulas to produce updated `{snapshot, history, docref-index}`
5. Commits the regenerated `docs/{benchmarks,load-tests}/data/*` back to `main`

This means a reader can trust that the history displayed on the dashboard always reflects the latest, most refined methodology — not a frozen snapshot of whatever formulas were current at the time a given data point was captured. The raw data is immutable; the derived data is rebuildable.

Recalculation is not run on every commit (it would be wasteful). It is run:

- When a file under `.github/scripts/perf-*.cs` changes
- When this methodology document changes in a way that affects formulas (noted in the commit message with `methodology: recalculate`)
- On explicit workflow dispatch with input `recalculate: true`

## Quality rules for benchmarks

This section captures the rules benchmark authors must follow. Violations are detected by static checks before a benchmark is merged.

1. **Use `BenchmarkSwitcher.FromAssembly(assembly).Run(args, config)`**, never `BenchmarkRunner.Run<T>()`. The switcher honors `--filter` and is required for per-class matrix fan-out.
2. **Return materialized results from benchmark methods.** Returning `IQueryable<T>` causes BenchmarkDotNet's validator to reject the result. Use `.ToList()` or similar.
3. **Use `[MemoryDiagnoser]` on every benchmark class.** Allocation data is a first-class metric, not a diagnostic extra.
4. **Add `[BenchmarkCategory("DocRef:<id>")]` to every method cited in documentation.** Benchmarks not cited can omit it.
5. **Do not use `[Params]` combinations that multiply iteration counts beyond what the workflow timeout can absorb.** If you need many parameters, split into multiple benchmark classes.
6. **Do not use `Thread.Sleep` inside a benchmark.** It measures nothing and destroys variance statistics.
7. **Use `[GlobalSetup]` for expensive initialization and `[IterationSetup]` only for per-iteration state that must be reset.**
8. **If your benchmark allocates inside a tight loop, document whether the allocation is intentional (measuring allocation cost) or accidental (a defect to fix).**
9. **Benchmarks that interact with real I/O (DB, HTTP, filesystem) belong in integration benchmarks, not microbenchmarks.** They have very different variance profiles and should be categorized accordingly.

## Quality rules for load tests

1. **Every scenario has a constant `DocRef` identifier** registered with the scenario class.
2. **Warmup is mandatory** and subtracted from measured data (NBomber handles this via `WarmUp`).
3. **Error rate above 1% invalidates the measurement** and flags the scenario as requiring investigation before data is cited.
4. **Each scenario runs on Testcontainers-managed databases/brokers**, never on shared infrastructure that could contaminate runs.
5. **P99 is always reported alongside P95.** A load test without tail-latency data is incomplete.

## How to read the dashboard

(This section will be populated in detail once the dashboard is live in Phase 1. Until then, the structural description in ADR-025 §1 and the coverage dashboard at <https://dlrivada.github.io/Encina/coverage/> serve as the reference.)

High-level principles:

- The **headline number** of any benchmark is its **Median**, accompanied by its **CI99 band**.
- Colors: green = stable + no regression; amber = stable + drift warning; red = stable + regression; grey = Unstable (excluded from headlines).
- Filters: by module, by category, by stability, by DocRef presence, by time range.
- Trend graphs: default to Median with CI99 shaded; toggle to Mean, Throughput, or Allocated.
- Hovering a data point shows the full `metadata.json` for that run.

## Working with this document

This file is **living**. Changes to formulas are reviewed like code, committed to `main`, and trigger a `methodology: recalculate` run that rebuilds all derived data under the new rules. The raw data in `perf-raw` is never rewritten — only the interpretations in `docs/{benchmarks,load-tests}/data/*` change.

When you add a new section, a new metric, or a new rule, include a rationale. The goal of this document is not to be complete; it is to be **honest**. If the team does not know how to handle some situation (e.g. a benchmark that cannot be stabilized), say so explicitly rather than inventing a rule that sounds plausible.

## Related

- [ADR-025 — Performance Measurement Infrastructure](../architecture/adr/025-performance-measurement-infrastructure.md)
- [Performance Infrastructure Plan](../plans/performance-infrastructure-plan.md)
- [ADR-023 — Coverage Strategy](../architecture/adr/023-coverage-strategy-codecov-sonarcloud.md) (architectural parent)
- [`coverage-measurement-methodology.md`](coverage-measurement-methodology.md) (sibling methodology — same architectural pattern)
- Coverage dashboard: <https://dlrivada.github.io/Encina/coverage/>
- Benchmarks dashboard (target): <https://dlrivada.github.io/Encina/benchmarks/>
- Load-tests dashboard (target): <https://dlrivada.github.io/Encina/load-tests/>
