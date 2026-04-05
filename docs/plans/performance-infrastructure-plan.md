# Performance Measurement Infrastructure — Implementation Plan

> **Status**: 🟢 Phase 3.1 implemented — awaiting validation run (Phase 2 validated)
> **ADR**: [025 — Performance Measurement Infrastructure](../architecture/adr/025-performance-measurement-infrastructure.md)
> **Methodology**: [`performance-measurement-methodology.md`](../testing/performance-measurement-methodology.md)
> **Date**: 2026-04-05

## Implementation log

| Date | Milestone |
|---|---|
| 2026-04-05 | Phase 0.3: 3 `Program.cs` files migrated to `BenchmarkSwitcher` (AzureFunctions, Polly, Refit) |
| 2026-04-05 | Phase 1.1: `perf-report.cs`, `perf-history.cs`, `perf-recalculate.cs`, `generate-perf-manifest.cs` created and smoke-tested locally |
| 2026-04-05 | Phase 0.4: 20 per-project manifests generated under `.github/perf-manifest/` (210+ benchmark methods catalogued) |
| 2026-04-05 | Phase 1.6: `benchmarks.yml` extended to all 20 projects, migrated to `ubuntu-latest`, NuGet cache, `DOTNET_TieredCompilation=0`, JSON exporter enabled, `aggregate` job added |
| 2026-04-05 | Phase 1.5: `publish-benchmarks.yml` created with `workflow_run` trigger, automatic `perf-raw` orphan branch bootstrap, Pages deploy |
| 2026-04-05 | Phase 1.2: Minimal dashboards created at `docs/benchmarks/dashboard/` and `docs/load-tests/dashboard/` with placeholder data |
| 2026-04-05 | Phase 1.3: `perf-raw` branch creation deferred to first publish run (auto-bootstrap in `publish-benchmarks.yml`) |
| 2026-04-05 | Phase 1 validated end-to-end: run 24008176459 produced 17 modules, 825 methods, 140 stable. Dashboard live, perf-raw branch populated, history.json committed |
| 2026-04-05 | Phase 2.1: `perf-fingerprint.cs` created — project-graph SHA-256 per benchmark project (transitive csproj closure + Directory.Packages.props). Deterministic, dependency-free |
| 2026-04-05 | Phase 2.1: `perf-diff-fingerprints.cs` created — compares current fingerprints to previous snapshot, emits dynamic matrix + skipped list |
| 2026-04-05 | Phase 2.1: `perf-report.cs` extended with `--fingerprints-file` (attaches fingerprint to each module) and `--carry-forward-from` (copies unchanged modules from previous snapshot with `carriedForward: true`) |
| 2026-04-05 | Phase 2.2: `benchmarks.yml` restructured with `determine-matrix` → `run-benchmarks` → `aggregate`. Dynamic matrix. Schedule and `force_full=true` bypass cache. Aggregate runs even when no projects execute |
| 2026-04-05 | Phase 2.3: `on: pull_request` trigger added with path filter for `src/**/*.cs`, `tests/Encina.BenchmarkTests/**`, `Directory.Packages.props`, workflow YAML, and perf-*.cs scripts |
| 2026-04-05 | `publish-benchmarks.yml` updated to commit `fingerprints.json` alongside `latest.json` to Pages, closing the Phase 2 loop |
| 2026-04-05 | Phase 2 validated end-to-end: run 24011643747 produced `freshModules: 0 / carriedForwardModules: 17` — true no-op no-change run. Fingerprints persistent, carry-forward flag visible in dashboard, coverage dashboard preserved via cross-domain Pages overlay fix |
| 2026-04-05 | Issue #923 opened and fixed: BenchmarkDotNet spawns child processes with different CWD, so `--artifacts` relative path broke AspNetCore upload (silently failing since run 24007640847). Fix: pass `${{ github.workspace }}/artifacts/performance/<name>` as absolute path |
| 2026-04-06 | Phase 3.1 implemented: `perf-class-matrix.cs` fans out each project into one matrix entry per `[Benchmark]` class (20 projects → ~75 entries) using the committed `.github/perf-manifest/*.json` files. `run-benchmarks` now dispatches class-level jobs (matrix capped to 30 concurrent by `max-parallel`), upload-artifact names include the class, `aggregate` downloads with `merge-multiple: true` to reassemble full per-project trees. Wall-clock for full runs expected to drop from ~25 min (bounded by slowest project, typically Core at 15+ min) to ~5-8 min (bounded by slowest single class) |

## Goal

Build a continuous, honest, reproducible performance-measurement system for Encina that:

1. Runs **all 20 benchmark projects and all NBomber scenarios** on a schedule and on relevant path changes.
2. Stores **all raw measurement data forever** in an orphan git branch (`perf-raw`), bypassing the 90-day GitHub Actions artifact retention.
3. Publishes **two live dashboards** (`/benchmarks/` and `/load-tests/`) mirroring the coverage dashboard pattern.
4. Quantifies and displays **measurement uncertainty** instead of pretending cloud runners are deterministic.
5. Keeps **documentation automatically synchronized** with measurements via `DocRef` identifiers and generated Markdown fragments.
6. Enables **full historical recalculation** when measurement methodology evolves.

## Guiding principles (from ADR-025)

- **Zero euros.** Only free-tier GitHub services (Actions, Pages, repository storage on public repo).
- **Cloud-only execution.** No self-hosted runners. Variance is a signal, not noise to hide.
- **All benchmarks, not a curated subset.** The 12 orphan projects become part of the rotation.
- **Mirror the coverage pattern.** Scripts, workflows, dashboard, manifests — all directly ported from ADR-023's working implementation.
- **Documentation is downstream of measurements.** Nothing in `docs/` is hand-typed from a benchmark output.

## Phase 0 — Governance and cleanup (unblocks everything)

**Objective:** know what we are measuring and why before building the infrastructure.

### 0.1 Documentation audit

- [ ] Finalize the inventory of the 95 quantitative mentions identified during the exploration (67 backed, 24 numeric-without-trace, 4 qualitative)
- [ ] Classify each entry as: *keep*, *refine*, *delete*, *needs new benchmark*
- [ ] Open GitHub issues for each *needs new benchmark* entry with label `[TEST]` and link to the doc citation
- [ ] Open a GitHub issue for each of the 4 qualitative statements with label `[DEBT]` and the recommended replacement

### 0.2 DocRef convention

- [ ] Decide the hierarchical format: `bench:<area>/<name>` and `load:<area>/<name>`
- [ ] Document the convention in `docs/testing/performance-measurement-methodology.md` (already scaffolded)
- [ ] Create a naming sheet listing the intended DocRefs for all benchmarks and scenarios that will be cited in docs on day 1
- [ ] Add `[BenchmarkCategory("DocRef:<id>")]` attributes to existing benchmarks that are already cited
- [ ] Add scenario constants to NBomber scenarios that are already cited

### 0.3 Purge and refactor

- [ ] Audit `[Params]` annotations across all benchmarks; remove redundant parameter combinations (tracked per-project as sub-issues)
- [ ] Identify benchmarks that do not back any documentation claim; move them to an `Unreferenced` category for review (do not delete yet — their DocRef audit may later show a citation)
- [ ] Verify every `Program.cs` in benchmark projects uses `BenchmarkSwitcher.FromAssembly(...).Run(args, config)` and not `BenchmarkRunner.Run<T>()` (CLAUDE.md enforcement)
- [ ] Verify no benchmark method returns `IQueryable<T>` (CLAUDE.md enforcement)

### 0.4 Threshold consolidation

- [ ] Import the jerarchical structure from `nbomber-*-thresholds.json` into a new `benchmark-thresholds.json` covering all existing benchmarks
- [ ] Document initial target values as `TBD` where no historical data exists; they will be calibrated from the first successful full run in Phase 1

### Phase 0 exit criteria

- DocRef convention documented and applied to the benchmarks that Phase 1 will consume
- GitHub issues open for every audit outcome
- All `Program.cs` files verified CLAUDE.md-compliant
- `benchmark-thresholds.json` exists and covers every benchmark method (values may be TBD)

---

## Phase 1 — Publication infrastructure (maximum value, low risk)

**Objective:** bring `/benchmarks/` and `/load-tests/` live on GitHub Pages with whatever data is currently producible. This is the phase that makes the dashboard-driven model real.

### 1.1 Port the coverage scripts

- [ ] Copy `.github/scripts/coverage-report.cs` → `.github/scripts/perf-report.cs` and adapt parsers:
  - BenchmarkDotNet CSV (`*-report.csv`) → per-method `{Mean, Median, StdDev, Min, Max, Allocated, Gen0/1/2, N}`
  - BenchmarkDotNet JSON (`*-report-full.json`) → confidence intervals
  - NBomber summary (`nbomber-summary.json`) → per-scenario `{RequestCount, Ok, Fail, Mean, Median, P50, P95, P99, StdDev, Min, Max, RPS}`
  - Output JSON structure matches the coverage `latest.json` shape: overall + per-module + per-benchmark, with DocRef index and metadata block
- [ ] Copy `.github/scripts/coverage-history.cs` → `.github/scripts/perf-history.cs`:
  - Append snapshot to `docs/{benchmarks,load-tests}/data/history.json`
  - **No 100-entry cap**; unlimited retention (supersedes coverage limit)
  - Lightweight entries keyed by `{timestamp, runId, sha}` + top-level metrics
- [ ] Copy `.github/scripts/coverage-recalculate.cs` → `.github/scripts/perf-recalculate.cs`:
  - Source: `perf-raw` git branch, not GitHub Actions artifacts
  - Iterate runs under `perf-raw/benchmarks/**/runId/` and `perf-raw/load-tests/**/runId/`
  - Rerun `perf-report.cs` against each and rewrite history
- [ ] Create `.github/scripts/generate-perf-manifest.cs`:
  - Scan `tests/Encina.BenchmarkTests/**/*Benchmarks.cs` classes
  - Emit one JSON manifest per project under `.github/perf-manifest/<Project>.json`
  - Preserve existing overrides (mirror coverage-manifest behavior)
- [ ] Create `.github/scripts/perf-manifest-check.cs`:
  - Validates that every benchmark declared in a manifest exists in source
  - Validates every DocRef declared in a manifest is actually cited in at least one `docs/**/*.md`
  - Fails workflow if drift detected

### 1.2 Port the dashboard

- [ ] Copy `docs/coverage/index.html` → `docs/benchmarks/dashboard/index.html`
- [ ] Copy `docs/coverage/app.js` → `docs/benchmarks/dashboard/app.js`, adapt for benchmark metrics:
  - Columns: Method / Mean / Median / StdDev / CI99 / Allocated / Gen0
  - Coloring: green if within target and stable, amber if unstable or drift ≤ threshold, red if regression outside CI99 band
  - Trend chart: switchable between Mean / Median / Throughput / Allocated
  - Filter: by module, by category, by DocRef presence, by stability flag
  - Export: TSV copy-to-clipboard (same as coverage)
- [ ] Copy `docs/coverage/app.css` → `docs/benchmarks/dashboard/app.css`
- [ ] Repeat 1.2 steps for `docs/load-tests/dashboard/` with load-test-specific columns (P50/P95/P99/ErrorRate/RPS)

### 1.3 Create the `perf-raw` orphan branch

- [ ] Initial commit: empty orphan branch `perf-raw` with a `README.md` explaining the branch purpose and directory layout
- [ ] Document the directory tree in the README exactly as specified in ADR-025 §2
- [ ] Verify the branch is pushable with `git push origin perf-raw` under standard branch protection rules (may need exception)

### 1.4 Backfill from retained artifacts

- [ ] One-shot script `.github/scripts/perf-backfill.cs` that:
  - Enumerates recent successful runs of `benchmarks.yml` and `load-tests.yml` using `gh run list`
  - Downloads artifacts from each with `gh run download`
  - Structures them under `perf-raw/benchmarks/<YYYY-MM-DD>/<runId>/` and `perf-raw/load-tests/<YYYY-MM-DD>/<runId>/`
  - Commits in batches (one commit per run to preserve individual metadata)
- [ ] Run the backfill once and push to `perf-raw`
- [ ] Run `perf-recalculate.cs` across the backfilled data to populate `docs/{benchmarks,load-tests}/data/history.json`

### 1.5 Publish workflows

- [ ] Create `.github/workflows/publish-benchmarks.yml` triggered by `workflow_run` on `benchmarks.yml` completion:
  - Download `benchmarks` artifact
  - Run `perf-report.cs` → produces `latest.json` + archive snapshot
  - Run `perf-history.cs` → appends to `history.json`
  - Archive raw data to `perf-raw` branch
  - Build `_site` with `docs/benchmarks/dashboard/*` + `docs/benchmarks/data/*`
  - Deploy to Pages
- [ ] Create `.github/workflows/publish-load-tests.yml` analogously
- [ ] Verify both publish workflows coexist peacefully with `publish-coverage.yml` (no Pages deploy conflicts)

### 1.6 Extend the benchmark workflow to all 20 projects

- [ ] Add the 12 orphan projects to the `benchmarks.yml` matrix: AspNetCore, DistributedLock, Extensions.Resilience, Audit.Marten, Marten.GDPR, Messaging.Encryption, Security.Encryption, Security.PII, Security.Sanitization, Refit, AwsLambda, AzureFunctions
- [ ] For each, verify the project builds and `BenchmarkSwitcher` discovers classes
- [ ] Migrate runs to `ubuntu-latest` by default (keep `windows-latest` only for benchmarks with justified OS sensitivity)
- [ ] Add `DOTNET_TieredCompilation=0` env var at workflow level
- [ ] Add NuGet cache: `actions/cache` keyed by hash of `Directory.Packages.props` and `*.csproj`
- [ ] Introduce job-type tiers: `dry` on PR, `short` nightly, `medium` weekly, `default` only on release tags

### 1.7 Template filling

- [ ] Add `perf-docs-render.cs` that regenerates Markdown tables under `<!-- docref:bench:xxx -->...<!-- /docref -->` markers from the latest snapshot
- [ ] Add marker blocks to `docs/benchmarks/dapper-vs-ado-comparison.md` and `docs/benchmarks/provider-sql-dialect-comparison.md` so they auto-fill from Phase 1 runs
- [ ] Add marker blocks to `docs/testing/benchmarks/benchmark-results.md` wrapping each cited table
- [ ] Hook `perf-docs-render.cs` into the publish workflows so docs are regenerated and committed on every run

### Phase 1 exit criteria

- `https://dlrivada.github.io/Encina/benchmarks/` is live with at least one full snapshot from a fresh run
- `https://dlrivada.github.io/Encina/load-tests/` is live with at least one full snapshot
- `perf-raw` branch contains the backfilled historical runs + every new run since Phase 1 started
- `docs/benchmarks/*` templates are auto-generated (no more manual editing)
- All 20 benchmark projects execute in the `benchmarks.yml` matrix

---

## Phase 2 — Incremental filtering

**Objective:** avoid running benchmarks when nothing that affects them has changed. Dashboards remain complete because the previous snapshot is carried forward.

### 2.1 Source fingerprinting

- [ ] `.github/scripts/perf-fingerprint.cs`: compute the project-graph hash for a benchmark project
  - Uses `dotnet msbuild /t:ResolveProjectReferences` to enumerate transitive project dependencies
  - Hashes all `.cs` files reachable through the dependency graph
  - Emits `{project, fingerprint}` pairs
- [ ] Add the fingerprint to the `metadata.json` of every `perf-raw` run
- [ ] Add the fingerprint to `docs/{benchmarks,load-tests}/data/latest.json` under each project entry

### 2.2 Skip-and-carry-forward logic

- [ ] Pre-run job in `benchmarks.yml`: compute current fingerprints, compare against the last `perf-raw` run, emit a matrix output with only the projects that changed
- [ ] Post-run consolidation step: for skipped projects, copy their previous entries from `history.json` into the new snapshot so the dashboard never shows gaps
- [ ] Replicate the same logic in `load-tests.yml`

### 2.3 Paths filter on triggers

- [ ] Define per-category paths filters on the workflow `on: push` trigger so that, for PRs that touch only one area, only the affected benchmarks run in `short` mode

### Phase 2 exit criteria

- A no-op commit to docs triggers no benchmark execution but still produces a valid dashboard update
- A commit that touches `src/Encina.Caching/**` runs only caching benchmarks on PR
- `perf-raw` grows only when benchmarks actually re-run

---

## Phase 3 — Parallelization and speed

**Objective:** when benchmarks do run, they finish in under 30 minutes wall-clock.

### 3.1 Matrix explosion to benchmark-class level

- [ ] Generate the matrix dynamically: a pre-job enumerates benchmark classes per project using `BenchmarkSwitcher --list flat` and emits them as matrix entries
- [ ] Each matrix entry runs a single class with `--filter "*ClassName*"`
- [ ] Fan-out: ~8 projects → ~80 matrix jobs

### 3.2 Shared build artifact

- [ ] `build` job compiles each benchmark project once, uploads `bin/Release` as artifact
- [ ] `run` jobs download the build artifact and skip `dotnet build`
- [ ] Saves ~30 seconds per job × 80 jobs = ~40 minutes

### 3.3 Quality tuning per class

- [ ] Add `[SimpleJob]` with tuned `warmupCount` and `iterationCount` to benchmark classes that have demonstrated stability in Phase 1-2 data
- [ ] Add `[BenchmarkCategory("Unstable")]` to classes whose coefficient of variation exceeds the threshold from the methodology doc
- [ ] Dashboard treats `Unstable` flagged classes differently: still shown, but excluded from headline trends and DocRef citations

### Phase 3 exit criteria

- Full clean run of all 20 projects finishes in <30 minutes on `ubuntu-latest` matrix
- Per-class fan-out produces ~80 parallel jobs
- Unstable benchmarks flagged and handled

---

## Phase 4 — Documentation integration and UX polish

**Objective:** docs, dashboards, and the developer workflow become indistinguishable from a product.

### 4.1 DocRef reverse index and cross-linking

- [ ] `perf-report.cs` emits `docs/{benchmarks,load-tests}/data/docref-index.json` mapping every DocRef to `{dashboard-url, latest-value, stability-flag, cited-by-docs[]}`
- [ ] Dashboard adds a "Cited in" column with links to the documents that reference each DocRef
- [ ] `perf-docs-render.cs` adds deep-link anchors on every generated table row so citations in docs jump to the exact dashboard entry

### 4.2 PR feedback

- [ ] Workflow step that comments on PRs with a delta table for `short` runs triggered by path changes
- [ ] Badge in README reflecting regression state of `main`:
  - 🟢 No regressions outside CI99 bands
  - 🟡 One or more regressions within tolerance
  - 🔴 Regressions exceeding tolerance

### 4.3 Unified dashboard

- [ ] Consolidate `/benchmarks/` and `/load-tests/` under `/performance/` with tabbed navigation
- [ ] Shared navigation, shared filter state, single search box
- [ ] Correlated trend view: latency (benchmarks) vs throughput (loads) over time
- [ ] Preserve `/benchmarks/` and `/load-tests/` as redirects to the new tabs for backward compatibility

### 4.4 Workflow dispatch inputs

- [ ] Extend `workflow_dispatch` inputs on `benchmarks.yml`: `project` (dropdown), `class_filter` (regex), `job_type` (existing)
- [ ] Same for `load-tests.yml`

### 4.5 ChatOps

- [ ] `/benchmark <project-or-class>` slash command on PRs via `peter-evans/slash-command-dispatch`
- [ ] Posts the result as a PR comment when complete

### 4.6 Apply everything to load tests explicitly

- [ ] Verify every script handles load tests symmetrically (most work is automatic because the architecture is unified)
- [ ] Document any load-test-specific divergences in the methodology doc

### Phase 4 exit criteria

- `/performance/` unified dashboard replaces the two separate ones
- Every citation in `docs/**` has a working deep-link to the dashboard
- PRs touching performance-sensitive code receive automated delta comments
- README regression badge is accurate

---

## Deliverables inventory

| Artifact | Path | Phase |
|---|---|---|
| ADR | `docs/architecture/adr/025-performance-measurement-infrastructure.md` | Done (with this plan) |
| Methodology | `docs/testing/performance-measurement-methodology.md` | 0 → ongoing |
| Plan | `docs/plans/performance-infrastructure-plan.md` (this file) | Done |
| `perf-report.cs` | `.github/scripts/perf-report.cs` | 1 |
| `perf-history.cs` | `.github/scripts/perf-history.cs` | 1 |
| `perf-recalculate.cs` | `.github/scripts/perf-recalculate.cs` | 1 |
| `perf-backfill.cs` | `.github/scripts/perf-backfill.cs` | 1 |
| `perf-docs-render.cs` | `.github/scripts/perf-docs-render.cs` | 1 |
| `perf-fingerprint.cs` | `.github/scripts/perf-fingerprint.cs` | 2 |
| `perf-manifest-check.cs` | `.github/scripts/perf-manifest-check.cs` | 1 |
| `generate-perf-manifest.cs` | `.github/scripts/generate-perf-manifest.cs` | 1 |
| Benchmark dashboard | `docs/benchmarks/dashboard/{index.html,app.js,app.css}` | 1 |
| Load-test dashboard | `docs/load-tests/dashboard/{index.html,app.js,app.css}` | 1 |
| Benchmark data | `docs/benchmarks/data/{latest.json,history.json,docref-index.json,<snapshot>.json}` | 1 |
| Load-test data | `docs/load-tests/data/{latest.json,history.json,docref-index.json,<snapshot>.json}` | 1 |
| Unified dashboard | `docs/performance/...` | 4 |
| Per-module manifests | `.github/perf-manifest/<Module>.json` | 1 |
| Consolidated thresholds | `.github/ci/benchmark-thresholds.json` (hierarchical) | 0 |
| Publish workflows | `.github/workflows/publish-benchmarks.yml`, `publish-load-tests.yml` | 1 |
| Extended benchmarks workflow | `.github/workflows/benchmarks.yml` (20 projects, ubuntu, cache, tiers) | 1 |
| Raw-data branch | orphan `perf-raw` with README | 1 |
| Fingerprint column in data | metadata section of every snapshot | 2 |
| DocRef reverse index | `data/docref-index.json` | 4 |
| Regression badge | shields.io endpoint backed by `latest.json` | 4 |

## Risks and mitigations

| Risk | Mitigation |
|---|---|
| `perf-raw` branch becomes huge and clones are slow | Branch is orphan; developers working on Encina do not need it locally. Only CI and recalculation script fetch it with `--single-branch`. |
| Dynamic matrix fan-out hits GitHub's 256-job matrix limit | Expected ~80 jobs in Phase 3, well below the cap. Guard the generation with an assertion. |
| Path filter misses a transitive dep and skips a benchmark that should have run | Project-graph fingerprint (Phase 2) is transitive via MSBuild — path filter is only a first-pass optimization. |
| Variance so high that no benchmark passes stability threshold | Methodology doc specifies the threshold starting permissive (CoV ≤ 10%) and tightening as data accumulates. Unstable classes remain displayed, just not cited. |
| Documentation references a DocRef that was renamed | `perf-manifest-check.cs` detects the mismatch and fails the workflow, making it fixable before merge. |
| Dashboard JavaScript grows unmaintainable | Start as a direct port of the coverage dashboard (already working). Any growth is reviewed against the same simplicity standard as the coverage app. |

## Open questions to resolve during implementation

These are deferred to the phase where they become actionable; all have a default answer in the ADR or methodology doc that can be changed if data suggests otherwise.

1. **Regression threshold default.** ADR defaults to "median outside previous CI99 band" plus a 10% delta on mean for comparison. To be validated from Phase 1 data and possibly per-category.
2. **Unstable flag threshold.** ADR defaults to CoV ≤ 10% as stable. Adjust in methodology doc once ~1 month of data exists.
3. **`perf-raw` compression.** Start uncompressed for simplicity; introduce gzip or zstd if branch grows beyond ~1 GB.
4. **Pruning policy for `perf-raw`.** Not before year 2. Revisit if branch exceeds 5 GB.
5. **Whether load-test runs commit to `perf-raw` every time** or only on main. Default: every run, including PR runs, because historical noise data is also informative.

## Working conventions during this plan

- Each phase is tracked as a `[EPIC]` issue with child issues per checklist section.
- Every new script is accompanied by at least one unit test in `tests/Encina.UnitTests/Infrastructure/Performance/`.
- Every PR that touches a script under `.github/scripts/perf-*.cs` runs a smoke test that executes the script against a fixture tree.
- Methodology changes trigger a full `perf-recalculate.cs` run on CI to ensure historical consistency.
