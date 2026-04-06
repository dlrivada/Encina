---
title: "ADR-025: Performance Measurement Infrastructure — Benchmarks + Load Tests with Historical Raw Data and Recalculation"
layout: default
parent: ADRs
grand_parent: Architecture
---

# ADR-025: Performance Measurement Infrastructure — Benchmarks + Load Tests with Historical Raw Data and Recalculation

## Status

Accepted (April 2026)

## Context

Encina's documentation makes many quantitative claims about performance — throughput, latency, memory allocation, comparative ratios — that users rely on to make informed adoption decisions. An exhaustive audit of the documentation tree identified **95 quantitative mentions** across `docs/`, ADRs, feature guides, and package READMEs. Of those:

- **67 (71%)** are backed by traceable benchmark or load-test output
- **24 (25%)** contain concrete numbers without explicit traceability to a measurement
- **4 (4%)** are qualitative ("nearly native performance", "negligible overhead") with no number at all

The existing infrastructure has grown organically and exhibits several structural problems:

### Problem 1 — Partial coverage, orphan projects

There are **20 benchmark projects** under `tests/Encina.BenchmarkTests/`, but the `.github/workflows/benchmarks.yml` matrix only executes **8 of them** (Core, ADO, Dapper, EFCore, Caching, Security.AntiTampering, IdGeneration, Polly). The remaining 12 projects — AspNetCore, DistributedLock, Extensions.Resilience, Audit.Marten, Marten.GDPR, Messaging.Encryption, Security.Encryption, Security.PII, Security.Sanitization, Refit, AwsLambda, AzureFunctions — exist as code but produce no data, leaving entire features without measurement even though the benchmarks are implemented.

### Problem 2 — No historical storage, no trend analysis, no dashboard

Benchmark results are uploaded as GitHub Actions artifacts and disappear from view after each run. Load-test results follow the same pattern. There is no equivalent of the coverage dashboard at `/coverage/`, no historical graph, no regression detection, no way for a user to see "how has the outbox pattern throughput evolved since v0.9". `aggregate-performance-history.cs` generates `docs/data/{benchmark-history,load-history}.md` as flat Markdown tables but these are not rendered, filtered, or plotted.

### Problem 3 — 90-day artifact retention caps historical recalculation

GitHub Actions retains workflow artifacts for 90 days by default. Once an artifact expires, the raw measurement data is permanently lost. This prevents retroactive recalculation when measurement methodology evolves (formula changes, new metrics added, outlier detection refined). The coverage system already faces this limitation — it solves it only for runs within the retention window via `coverage-recalculate.cs`, which calls `gh run download` against still-retained runs.

### Problem 4 — Documentation drift

Quantitative numbers in the documentation are hand-copied from benchmark output. There is no automation that regenerates tables when new data arrives. Two benchmark documents exist as templates with empty result tables (`docs/benchmarks/dapper-vs-ado-comparison.md`, `docs/benchmarks/provider-sql-dialect-comparison.md`). Other feature docs cite numbers without any link to the benchmark that produced them, making it impossible to verify whether the number is still current or to regenerate the document when the source data changes.

### Problem 5 — Threshold asymmetry

`.github/ci/` contains 6 well-structured load-test threshold files with global/feature/provider/scenario hierarchies (`nbomber-{database,messaging,caching,locking,brokers}-thresholds.json` plus `load-thresholds.json`). Benchmarks have only **2 methods** defined in `benchmark-thresholds.json` out of ~60 benchmark methods. Regression detection on the benchmark side is essentially absent.

### Problem 6 — Variance on shared cloud runners

GitHub-hosted runners are VMs on shared hardware with noisy-neighbor CPU variance. Self-hosted runners are not available (budget: zero euros, no dedicated hardware). This means measurement noise is a permanent environmental constraint and the methodology must embrace it rather than try to eliminate it. Measurements must quantify uncertainty (StdDev, confidence intervals) and the dashboard must communicate it to the reader, because a number shown without its uncertainty is a lie in a noisy environment.

### Industry Context

Research into how major .NET projects handle performance measurement shows a wide spread of maturity:

- **dotnet/runtime**: dedicated performance team, internal `crank` infrastructure, custom dashboard (`perf.dot.net`), dedicated hardware
- **ASP.NET Core**: dedicated BenchmarkDotNet projects, results published on their blog manually, no live dashboard
- **MassTransit, Orleans, Wolverine**: benchmarks in repo, no public dashboard, documentation cites point-in-time measurements
- **Npgsql**: results hand-published to docs page, no automation

None of the public-facing .NET OSS projects surveyed offers continuous measurement with historical recalculation, dashboard visualization, and verifiable cross-references from documentation to raw data. **This is the gap Encina fills.**

Encina already solved a structurally similar problem for code coverage in ADR-023, producing the dashboard at `https://dlrivada.github.io/Encina/coverage/` powered by `.github/scripts/coverage-{report,history,recalculate}.cs` and `docs/coverage/data/`. This ADR extends that pattern to performance measurement.

## Decision

We will establish a **Performance Measurement Infrastructure** that mirrors the coverage system architecturally and extends it to handle the specific requirements of benchmark and load-test data. The infrastructure is built exclusively on free-tier GitHub services (Actions, Pages, repository storage) with zero external paid dependencies.

### 1. Port the coverage pattern to benchmarks and load tests

Duplicate the four coverage scripts and their workflow topology, adapting them to BenchmarkDotNet and NBomber output formats:

| Coverage artifact | Performance counterpart | Purpose |
|---|---|---|
| `coverage-report.cs` | `perf-report.cs` | Parse BenchmarkDotNet CSV/JSON + NBomber reports, apply manifests, generate unified JSON |
| `coverage-history.cs` | `perf-history.cs` | Append snapshot to `history.json`, unlimited retention |
| `coverage-recalculate.cs` | `perf-recalculate.cs` | Reprocess archived raw data when methodology changes |
| `generate-coverage-manifest.cs` | `generate-perf-manifest.cs` | Auto-discover benchmark classes and scenarios, emit per-module manifest |
| `docs/coverage/{index.html,app.js,app.css}` | `docs/benchmarks/dashboard/{index.html,app.js,app.css}` and `docs/load-tests/dashboard/{...}` | Dashboards |
| `docs/coverage/data/*` | `docs/benchmarks/data/*` and `docs/load-tests/data/*` | Snapshots and history |
| `publish-coverage.yml` | `publish-benchmarks.yml` and `publish-load-tests.yml` | Deploy workflows |

The dashboards start as **two separate pages** (`/benchmarks/` and `/load-tests/`), with a later consolidation into `/performance/` as tabbed views once both are operationally stable. This sequencing reduces coupling risk during initial build.

### 2. Archive raw measurement data in a git branch (`perf-raw`)

The 90-day GitHub Actions artifact retention is bypassed by committing a compressed copy of every run's raw output to an orphan git branch called `perf-raw`. Each commit on this branch corresponds to one workflow run, with a directory tree organized as:

```
perf-raw/
├── benchmarks/
│   └── {YYYY-MM-DD}/{runId}/
│       ├── metadata.json        # SHA, runner, OS, CPU info, .NET version, workflow input
│       ├── {project}/
│       │   └── BenchmarkDotNet.Artifacts/  # raw CSV + JSON output per project
└── load-tests/
    └── {YYYY-MM-DD}/{runId}/
        ├── metadata.json
        ├── metrics-{timestamp}.csv
        ├── nbomber-{timestamp}/
        │   ├── nbomber-summary.json
        │   └── nbomber-report.{csv,html,md,txt}
        └── harness-{timestamp}.log
```

The branch is orphan (no parent to `main`) so it does not grow the working-tree history of development branches. Size grows unbounded but GitHub imposes no storage cap on public repositories, which is an accepted and explicit premise of this decision. Recalculation (`perf-recalculate.cs`) operates against `perf-raw`, not against Actions artifacts, eliminating the 90-day window entirely.

**Implication**: every formula change, every outlier-detection tweak, every new metric we introduce can be applied retroactively to every historical run, producing fully corrected history without loss of data.

### 3. Treat variance as a first-class signal, not noise to hide

Shared cloud runners produce measurement variance that cannot be engineered away under a zero-euro budget. Instead of pretending measurements are deterministic, the system explicitly records and displays uncertainty:

- Every benchmark snapshot stores `{Mean, Median, StdDev, CI99Lower, CI99Upper, Min, Max, N}` for each method
- Every load-test snapshot stores `{Mean, P50, P95, P99, StdDev, ErrorRate, N}` per scenario/provider
- The dashboard renders error bars or shaded CI ranges on every trend graph
- Benchmarks whose coefficient of variation (StdDev / Mean) exceeds a configured threshold are flagged as **Unstable** and excluded from headline numbers in documentation, even when still executed and displayed
- Regression detection operates on whether the new Median falls outside the previous run's CI99 band, not on raw delta thresholds that ignore noise

This is one of the "particular formulas" Encina adopts because off-the-shelf tools do not. The methodology is documented in full in `docs/testing/performance-measurement-methodology.md` and versioned alongside the code.

### 4. Traceability via DocRef identifiers

Every benchmark method and load-test scenario that is cited in documentation carries a hierarchical identifier of the form `bench:<area>/<name>` or `load:<area>/<name>`. Example: `bench:mediator/send-command`, `load:caching/redis`. The identifier is expressed as a `[BenchmarkCategory("DocRef:<id>")]` attribute on the method (for BenchmarkDotNet) or as a scenario constant (for NBomber).

Documentation cites a measurement by its identifier. `perf-report.cs` emits a `docref-index.json` that maps each DocRef to the current value, standard deviation, and dashboard deep-link. The Markdown-generation step (`perf-docs-render.cs`) reads this index and regenerates citation fragments in place, keeping documentation automatically synchronized with the latest data.

A benchmark whose DocRef is deleted from every document in the repository is flagged as orphan and becomes a candidate for removal. A DocRef cited in a document with no matching benchmark fails the workflow. This closes the loop: the documentation and the benchmarks are mutually verifiable.

### 5. Per-module manifests with explicit targets

Mirroring `.github/coverage-manifest/`, a new `.github/perf-manifest/` contains one JSON file per module, defining the benchmark methods expected to exist, their DocRefs, their performance targets (e.g. `send_command_mean_us: <= 2.0`, `send_command_allocated_kb: <= 6.0`), and their stability expectations. Missing benchmarks, failing targets, and drift between code and manifest are detected by `perf-manifest-check.cs`, analogous to how coverage manifests are validated today.

### 6. All execution stays on GitHub-hosted cloud runners

A self-hosted runner on developer hardware was evaluated and rejected. The developer machine is not reliably available for multi-hour runs without interfering with ongoing work, and security concerns around PRs from external contributors add operational overhead. The system is designed from the start to live with cloud-runner variance (per decision 3) rather than assume determinism.

Where a benchmark is dependent on OS-specific APIs (Windows registry, kernel primitives, IOCP behavior) a multi-OS matrix is used. For all other benchmarks, `ubuntu-latest` is the default because it is faster, more available, and less susceptible to Windows runner queueing delays.

### 7. Filtering by source fingerprint

To avoid running benchmarks when nothing has changed, the workflow computes a fingerprint of each benchmark project's transitive source dependencies (MSBuild project graph + hash of all `.cs` files reachable through `ProjectReference` chains). The fingerprint is compared against the last successfully benchmarked fingerprint stored in `perf-raw`. A match means the previous run's results are copied forward into the new snapshot unchanged. A mismatch triggers a fresh run.

This decouples wall-clock cost from calendar cadence: runs sit idle until something actually changes. Combined with per-class matrix expansion (fan-out at the benchmark-class level rather than the project level) and shared build artifacts, wall-clock time for runs that do have changes is targeted at <30 minutes even when the full 20-project scope is active.

### 8. Pull-request feedback without blocking CI

PRs never fail because of performance regressions. Instead:

- A `dry` BenchmarkDotNet run validates that benchmarks still compile and start
- For changed paths (via `paths:` trigger), a `short` run produces deltas commented on the PR by a workflow action
- A README badge reflects the regression state of `main` (green / amber / red based on `docs/{benchmarks,load-tests}/data/latest.json`)

Regression is visible but never blocks merge. Developers and reviewers decide case-by-case whether a regression is acceptable, informed by the measured uncertainty.

## Module Categories and Baseline Targets

The performance manifests are organized around functional categories, parallel to (but independent of) the coverage categories in ADR-023. Initial targets are established empirically during Phase 1 rollout from the first full clean run and adjusted as data accumulates.

| Category | Primary metrics | Rationale |
|---|---|---|
| Core Mediator | Mean latency (µs), allocation (KB), throughput (ops/s) | Critical hot path; single-digit microseconds expected |
| Stores (Outbox/Inbox/Saga/Scheduled) | Per-op latency (µs), batch latency (µs) | I/O-bound; per-provider targets |
| Bulk Operations | Per-op throughput (ops/s) vs batch size | Comparative ratios across providers |
| Resilience (Polly, Extensions) | Overhead (ns) vs no-op baseline | Must be measurable in nanoseconds |
| Caching | Read/write latency (ns), throughput (ops/s), hit-rate impact | L1/L2 separation |
| Security (Encryption, PII, Sanitization, AntiTampering) | Per-op latency (ns), throughput per field type | Sub-microsecond targets |
| ID Generation | ns/id, allocations per id, uniqueness under contention | Hot path in event-heavy systems |
| Messaging (Encryption, Transports) | End-to-end latency (µs), throughput (ops/s) | Provider-specific |
| AspNetCore (Authorization, Pipeline) | Request overhead (µs) | Per middleware hop |
| Cloud (AwsLambda, AzureFunctions) | Cold-start impact (ms), warm latency (µs) | Serverless-specific |
| Audit/Compliance | Per-event latency (µs), encrypted-vs-plain ratio | GDPR overhead measurement |

The load-test categories already exist in `.github/ci/nbomber-*-thresholds.json` and are preserved; only their consumption by the new publishing pipeline is added.

## Consequences

### Positive

- **Every quantitative claim in documentation is traceable** via DocRef to a specific benchmark run stored in `perf-raw`, with the exact SHA, runner, OS, .NET version, and CPU model that produced it.
- **Historical data is unbounded and recalculable.** Methodology evolution does not invalidate history; it retroactively corrects it.
- **Users see both the number and its uncertainty.** No headline figure is shown without its confidence interval, eliminating the "invented-or-estimated number" problem that plagues other projects.
- **Documentation cannot drift.** Automated regeneration from the live data makes stale numbers structurally impossible.
- **The 12 orphan projects stop being blind spots.** Publication infrastructure creates the incentive to wire them up, which is then cheap (matrix row + one manifest).
- **Load tests and benchmarks share infrastructure.** Thresholds, manifests, dashboards, and recalculation work uniformly.

### Negative

- **`perf-raw` branch grows unbounded.** This is accepted per the zero-budget, public-repo premise. Compression and directory organization keep growth rate manageable but the branch will eventually be large. A pruning policy can be introduced later without disturbing consumers because recalculation is idempotent across whatever data happens to exist.
- **Cloud-runner variance is a permanent constraint.** Some benchmarks will be marked Unstable and omitted from headline citations. The user sees fewer "clean" numbers but the ones shown are honest.
- **Initial implementation cost.** Five phases of work, several scripts, two dashboards, a methodology document, and a full audit of existing documentation citations. The tradeoff is that this is paid once and amortized across every future measurement.
- **Methodology becomes a subject in its own right.** Formulas for variance handling, regression detection, and recalculation need to be documented, reviewed, and updated. This is codified in `docs/testing/performance-measurement-methodology.md`.

### Neutral

- Two dashboards initially, one combined later. The migration path is a straightforward DOM restructure of the existing pages; data formats do not change.
- The existing `.github/scripts/aggregate-performance-history.cs` and `docs/data/{benchmark,load}-history.md` flat Markdown tables remain in place during transition and are deprecated once the new dashboards carry their functionality.
- ADR-023 (Coverage) and this ADR share an architectural lineage but are independent. Changes to one do not require changes to the other.

## Related

- Methodology: [`docs/testing/performance-measurement-methodology.md`](../../testing/performance-measurement-methodology.md)
- Plan: [`docs/plans/performance-infrastructure-plan.md`](../../plans/performance-infrastructure-plan.md)
- ADR-023: Coverage Strategy (architectural parent)
- ADR-018: Cross-Cutting Integration Principle
- Coverage dashboard: <https://dlrivada.github.io/Encina/coverage/>
- Live benchmarks dashboard (target): <https://dlrivada.github.io/Encina/benchmarks/>
- Live load-tests dashboard (target): <https://dlrivada.github.io/Encina/load-tests/>
