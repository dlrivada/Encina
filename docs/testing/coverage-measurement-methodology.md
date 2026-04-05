# Coverage Measurement Methodology

> **Status**: Living document — updated as the methodology evolves.
> **ADR**: [023 — Coverage Strategy (Codecov + SonarCloud)](../architecture/adr/023-coverage-strategy-codecov-sonarcloud.md)
> **Implementation**: `.github/scripts/coverage-report.cs`, `coverage-history.cs`, `coverage-recalculate.cs`, `generate-coverage-manifest.cs`

## Purpose

This document explains **how** Encina measures code coverage — the formulas, rules, and conventions the team adopted to produce honest, per-module, reproducible coverage data. It is written for two audiences:

1. **Users of Encina** who need to verify that the coverage numbers shown in the dashboard and referenced in documentation are trustworthy and understand what they actually mean.
2. **Contributors to Encina** who need to add or maintain tests, interpret coverage warnings, or change the formulas and be confident that historical data will remain consistent.

The content of this document is the contract between the coverage infrastructure and everyone who consumes its output. Changes to formulas require a `coverage-recalculate.cs` run against archived artifact data so that the dashboard and documentation reflect the new rules uniformly.

## Why a methodology document exists

Off-the-shelf coverage tools (Codecov, SonarCloud) produce a single percentage per file or per project. Encina needed something different because its reality is different:

- **108 source packages** with very different testability profiles — core business logic that can realistically reach 85%, database provider stores that run SQL and are only exercised by integration tests, cloud adapters that depend on AWS/Azure/GCP SDKs, cryptographic primitives that need property-based tests, etc.
- **5 test types** that each carry distinct guarantees (unit, guard, contract, property, integration). A package "covered" only by unit tests is less covered than the same package with all 5 types, even if the line-hit percentage is identical.
- **Measurement should distinguish "not applicable" from "uncovered"**. An interface file has no implementation to cover; a logging source-generator file has no logic to cover. Tools that count these files drag the project percentage down artificially and push teams toward writing pointless tests.
- **Per-package targets are not optional**. Holding database provider packages to an 85% bar the way core logic is held is unrealistic; holding core logic to a 50% bar the way providers are held is a failure of rigor. A single project-wide threshold is wrong in both directions simultaneously.

ADR-023 documents *what* we use (Codecov Components + Flags) and *why* (no single-threshold tool fits). This document explains the **particular formulas** Encina applies on top of that tooling, because the public tools do not provide them out of the box. The formulas are implemented in `.github/scripts/coverage-report.cs` and live on the `gh-pages` dashboard at <https://dlrivada.github.io/Encina/coverage/>.

## The obligations model

The central formula is the **obligations model**. It replaces the standard "covered lines / total lines" definition with a more accurate one that respects the per-file, per-flag nature of Encina's testing.

### Definition

An **obligation** is a (flag, line) pair that the test suite is expected to cover.

For each source file:

1. The file manifest declares which test types (flags) apply to that file — e.g. an `*Store.cs` file might have `["unit", "guard", "integration"]` applicable.
2. For each applicable flag, every line of the file becomes one obligation.
3. If a line is covered by a test of that flag, the obligation is **met**. If not, it remains **unmet**.
4. The file's coverage is `met obligations / total obligations` across all its applicable flags.

**Example.** A file with 100 lines whose manifest declares `["unit", "guard", "integration"]`:

- Total obligations = 100 lines × 3 flags = **300 obligations**
- If unit tests cover 80 lines, guard tests cover 40 lines, integration tests cover 60 lines:
  - Met = 80 + 40 + 60 = **180 obligations**
  - File coverage = 180 / 300 = **60.0%**

Contrast with the naive model:

- Lines covered = set union of {80, 40, 60} = ~85 distinct lines (depending on overlap)
- Naive coverage = 85 / 100 = **85%**

The obligations model gives a lower but more honest number: it says "your tests cover the file, but not from every angle you said they should".

### Aggregation

**Package coverage:**

```
package.coverage = Σ(file.met_obligations) / Σ(file.total_obligations) × 100
```

**Overall coverage:**

```
overall.coverage = Σ(package.met_obligations) / Σ(package.total_obligations) × 100
```

Both are rounded to two decimal places. This is exactly what `coverage-report.cs` computes — the package calculation at the point where `perFlag` totals are summed, and the overall at the final aggregation across packages.

### What the obligations model catches that naive coverage misses

- A file nominally "covered" by unit tests alone, when it has a `Store.cs` suffix that implies it also needs integration tests, shows a **low obligations percentage** even if unit tests reach 100% of its lines. The naive model would report 100% and miss the gap.
- An entire package with unit tests but no contract tests, where the manifest declares contract tests applicable, is visibly under-covered. Naive tools give it full marks for the lines that unit tests hit.
- Files excluded from testing (interfaces, generated code, metadata files) have **zero obligations** and therefore **no effect on the percentage**. Naive tools either count them (dragging the number down) or require manual exclusion config (brittle, error-prone).

This is why the dashboard coverage percentage for Encina is lower than what a default SonarCloud or Codecov report would produce: it is measuring something more demanding and more informative.

## The 5 test types (flags)

Encina distinguishes five categories of tests. Each category makes a different guarantee about the code it covers, and each one is counted as a separate flag in the obligations model. The enum definition in `coverage-report.cs` is authoritative:

```csharp
[Flags]
enum TestType
{
    None = 0,
    Unit = 1,
    Guard = 2,
    Contract = 4,
    Property = 8,
    Integration = 16,
    All = Unit | Guard | Contract | Property | Integration
}
```

| Flag | Purpose | Project location | Directory pattern in artifacts |
|---|---|---|---|
| **Unit** | Isolated behavior tests, mocked dependencies | `tests/Encina.UnitTests/` | `UnitTests-*` |
| **Guard** | Parameter validation (ArgumentNullException, ArgumentException on public methods) | `tests/Encina.GuardTests/` | `GuardTests-*` |
| **Contract** | API surface and interface contracts, inheritance rules, exported types | `tests/Encina.ContractTests/` | `ContractTests-*` |
| **Property** | Property-based tests via FsCheck, invariants over random inputs | `tests/Encina.PropertyTests/` | `PropertyTests-*` |
| **Integration** | Tests against real infrastructure (Testcontainers databases, brokers, etc.) | `tests/Encina.IntegrationTests/` | `IntegrationTests-*` or `EFCore-*` |

When CI uploads coverage, each test job tags its Cobertura XML output with the corresponding flag. `coverage-report.cs` classifies incoming artifact directories by matching these patterns (case-insensitive) and assigns the coverage data to the appropriate flag bucket.

A flag is **applicable to a file** only if the file's manifest entry lists it under `defaultTests` (or an explicit `override`). Non-applicable flags do not generate obligations for that file and do not affect its coverage.

## Per-file manifests and automatic classification

Every source file in `src/**/*.cs` is classified into the test types that should cover it. This classification is stored in per-package manifests under `.github/coverage-manifest/<Package>.json`.

### Automatic generation

`generate-coverage-manifest.cs` scans every `.cs` file in `src/` (excluding `obj/` and `bin/`) and applies the rules from `.github/coverage-manifest/defaults.json` in order. The **first matching rule wins**. If no rule matches, the file falls back to `["unit", "guard"]`.

Rule types supported:

| Match type | Semantics | Example |
|---|---|---|
| `exact` | Exact filename match | `Log.cs` → no tests |
| `glob` | Glob pattern on filename | `*Store.cs` → unit+guard+integration |
| `regex` | Regex on filename | `^I[A-Z].*\.cs$` → no tests (if also an interface) |
| `path-glob` | Glob pattern on relative path | `Compliance/**/*.cs` → specific compliance rules |

A rule can carry a **condition** like `contains_interface`, which requires additional file inspection. For interface detection, `generate-coverage-manifest.cs` reads the first 40 lines of files whose name begins with `I<Upper>` and checks for the `interface` keyword. Only files that pass both the name pattern and the content check are treated as interfaces (and therefore excluded).

### Overrides

Manifests can contain per-file overrides that replace the automatically computed `defaultTests`. The override structure is preserved across regenerations: running `generate-coverage-manifest.cs` again does not destroy existing overrides. This allows targeted corrections when the automatic rules misclassify a file — for example, a store-like file that genuinely does not need integration tests because it holds only pure helpers.

### Per-package targets

Each manifest declares per-flag coverage targets in its `targets` section:

```json
{
  "package": "Encina.Caching",
  "targets": {
    "unit": 60,
    "guard": 20,
    "contract": 10
  },
  "files": { ... }
}
```

These targets are **not** used as the overall percentage threshold. They are per-flag floors used by the dashboard to color individual cells (see §Dashboard coloring). The overall package percentage is still computed by the obligations formula across all applicable flags.

Categories are **not** defined in `coverage-weights.json` anymore. That file contains 12 legacy categories (Full, Logic, Provider, Transport, Cloud, CDC, Validation, DistributedLock, Caching, TestingLibrary, Tooling, Excluded) with aggregate targets (ranging from 0% for Excluded to 85% for Full), but it is no longer referenced by the report generator. It is kept in the repo as historical documentation of the original category thinking; targets are now per-package in the manifests.

## Exclusions

Some files have **zero obligations** regardless of how many lines they contain. These are files whose inclusion in coverage metrics would be misleading. The exclusion rules are declared in `defaults.json` with `"tests": []`:

| Pattern | Match type | Reason |
|---|---|---|
| `Log.cs` | exact | Source-generated `[LoggerMessage]` delegates — no logic |
| `GlobalSuppressions.cs` | exact | Only `[SuppressMessage]` attributes |
| `AssemblyInfo.cs` | exact | Assembly metadata |
| `*.g.cs` | glob | Auto-generated code (MSBuild, protobuf, analyzers) |
| `PublicAPI.Shipped.txt` | exact | Public API tracking file |
| `PublicAPI.Unshipped.txt` | exact | Public API tracking file |
| `*LogMessages.cs` | glob | Source-generated logging delegates |
| `^I[A-Z].*\.cs$` + contains interface | regex | Interface declarations — no implementation to test |

A file matched by these rules contributes **nothing** to obligations. It is not "uncovered"; it is "not applicable". The distinction matters because an uncovered file hurts the percentage while a not-applicable file leaves it untouched — which is what honesty requires.

## Dashboard coloring

The dashboard at <https://dlrivada.github.io/Encina/coverage/> renders per-package and per-flag coverage with a three-state coloring. The thresholds are computed in the client-side JavaScript (`docs/coverage/app.js`) against the targets supplied by the manifests.

### Per-flag cells and per-package headline

For a measured percentage `pct` against a target `target`:

- **🟢 PASS** — `pct >= target`
- **🟡 WARN** — `target × 0.8 ≤ pct < target`
- **🔴 FAIL** — `pct < target × 0.8`

This means a package falling between 80% and 100% of its target is visibly at risk (amber) but not yet failing (red). The 80% threshold is deliberately generous: a single test PR regression should not immediately turn the dashboard red, but it should warn before the situation is critical.

### Overall project percentage (no explicit target)

The headline overall number on the dashboard has no manifest target, so a fixed scale is used (defined in `coverage-report.cs`):

- **🟢 Green** — `overall ≥ 70%`
- **🟡 Amber** — `50% ≤ overall < 70%`
- **🔴 Red** — `overall < 50%`

This is a historical choice, calibrated to where Encina's obligations-based coverage was expected to land. It is not a target in the sense that packages have targets; it is a visual cue. The real enforcement happens at the per-package level where manifests carry explicit numbers.

## Data flow and the publishing pipeline

The coverage pipeline has two distinct workflows with a specific coupling:

1. **CI Full** runs the test suites, generates Cobertura XML outputs per test type, and uploads them as artifacts (`coverage-report` consolidated plus per-type `test-results-*` artifacts).
2. **Publish Coverage** is triggered by `workflow_run` when CI Full completes successfully on `main`, or manually via `workflow_dispatch` with an optional `run_id` input.

### Publish Coverage steps (from `.github/workflows/publish-coverage.yml`)

1. **Resolve the source run ID.** Uses the manual input if provided, the triggering workflow_run ID if automatic, or falls back to the latest successful CI Full run via `gh run list`.
2. **Download the `coverage-report` artifact** from that run with `actions/download-artifact@v8` + `run-id:`.
3. **Build the Pages site** by copying the entire `docs/` tree to `_site` and overwriting `_site/coverage/data/latest.json` with the fresh artifact, and `_site/coverage/badge.{svg,json}` with the fresh badges.
4. **Inject the `runId`** into `latest.json` using `jq` so that future recalculations know which GitHub Actions run produced this snapshot.
5. **Archive the snapshot** with a timestamped filename `{YYYY-MM-DDTHHMMSSZ}.json` under `_site/coverage/data/`, preserving the full snapshot for future recalculation and audit.
6. **Reconcile the history file.** Fetch `history.json` from the live Pages URL and compare the entry count with the local repo's version. The more complete of the two wins. This detours around a problem where branch protection can prevent the repo version from being updated promptly — Pages is the canonical live source.
7. **Append the new entry** to `history.json` via `coverage-history.cs`.
8. **Persist `history.json` back to the repo** with a bot commit (`continue-on-error: true` to tolerate branch protection rejections).
9. **Upload the Pages artifact** and deploy to GitHub Pages.

### The history file

`history.json` lives at `docs/coverage/data/history.json` and contains at most **100 entries**. When the 101st entry is appended, the oldest entry is removed. Each entry is intentionally lightweight (the full snapshot is kept separately in the archived `{timestamp}.json` file) and contains:

| Field | Description |
|---|---|
| `timestamp` | ISO 8601 UTC |
| `file` | Name of the archived full snapshot |
| `coverage` | Overall percentage, integer |
| `obligations` | Total obligations in this snapshot |
| `met` | Met obligations in this snapshot |
| `runId` *(optional)* | GitHub Actions run ID — enables retroactive recalculation |
| `perFlag` *(optional)* | Per-flag overall coverage for trend filtering (unit, guard, contract, property, integration) |

The 100-entry cap is historically motivated — at one snapshot per successful CI Full run on `main`, it covers several months of history. For unbounded retention, the archived snapshot files and the raw CI artifacts are the sources of truth. The lightweight `history.json` exists for fast dashboard loading, not as the primary data store.

## Recalculation: how methodology changes propagate to history

`coverage-recalculate.cs` is the mechanism that keeps historical data consistent with the **current** formulas. When the obligations model changes, when a new flag is added, when exclusion rules are refined, or when per-flag targets are updated, recalculation rebuilds the derived data from the raw CI artifacts of every historical run.

### Process

1. Read `docs/coverage/data/history.json`.
2. For each entry that has a `runId`, attempt to download the raw artifacts from that GitHub Actions run via `gh run download`:
   - First try the consolidated `coverage-raw-{runId}` artifact.
   - If that fails, fall back to downloading the individual `test-results-*` artifacts with `--pattern`.
3. Re-execute `coverage-report.cs` against the freshly downloaded Cobertura XMLs.
4. Read the regenerated `encina-coverage-summary.json`.
5. Update the corresponding entry in `history.json` with the new `coverage`, `lines`, `covered`, and `perFlag` values.

### Limitation — the 90-day artifact retention

GitHub Actions retains workflow artifacts for **90 days by default**. Once the artifacts for a given run expire, `gh run download` fails and the recalculation script prints `FAILED: Could not download artifacts for run {runId}` and skips that entry. The history entry remains in place with its old values, untouched.

This is a hard constraint of the platform on the free tier. The performance measurement infrastructure (see ADR-025) addresses this for benchmarks and load tests by archiving raw data in an orphan git branch (`perf-raw`), which has no retention limit. For coverage, the equivalent solution would be to commit the Cobertura XMLs to a `coverage-raw` branch. This is a possible future improvement; it is not currently implemented because the coverage methodology has been stable enough that 90 days of recalculation window has sufficed.

**What this means in practice:** if you change an obligations-related formula today and run `coverage-recalculate.cs`, you will update history entries for runs up to ~90 days old. Older entries keep their pre-change values. The dashboard's historical trend graph therefore shows a discontinuity at the 90-day boundary whenever a formula changes — and that discontinuity is visible precisely because the methodology is honest about what could and could not be reprocessed.

## Known limitations and honest disclosures

The methodology is deliberate and imperfect. These are the trade-offs we know about.

1. **Obligations double-count identical lines across flags.** A line covered by both unit and integration tests contributes two met obligations. This is intentional — being covered from two angles is better than being covered from one — but it means the absolute numbers (e.g., "224,430 total obligations") are larger than the NCLOC of the project. Readers should interpret the **percentage**, not the raw obligation counts, when comparing to NCLOC.

2. **Per-file manifests drift from source code.** New files added to `src/` do not automatically appear in the manifest until `generate-coverage-manifest.cs` runs. Until then, their coverage is not counted. This is mitigated by running the generator on every coverage update.

3. **Manifest-source drift is not enforced in CI.** Nothing currently fails the build if a file exists in `src/` but not in any manifest, or vice versa. A warning is logged but the workflow continues. Tightening this is a possible improvement.

4. **Automatic test-type classification is heuristic.** Rules in `defaults.json` match on filenames. A file named `FooStore.cs` is assumed to need integration tests, but if it is actually a pure in-memory helper with no SQL, the classification is wrong and the obligations percentage is artificially low. The override mechanism exists for exactly this case, but using it requires a human to notice.

5. **The 100-entry history cap discards old trend data.** The trend chart on the dashboard shows at most the last 100 snapshots. Longer-term analysis requires reading the archived `{timestamp}.json` files or going to the raw CI artifacts.

6. **Recalculation cannot reach beyond 90 days.** Described above. The dashboard's historical trend is most accurate for recent data.

7. **There is no source-code deep linking.** The dashboard knows which files belong to which package but does not generate links back to the GitHub source. The data to do so is present; the UI is not wired for it. This is a future improvement.

8. **The overall thresholds 70/50 for green/amber/red are historical, not computed.** Unlike per-package targets (which come from manifests), the overall dashboard headline uses fixed cutoffs. They reflect where Encina was when the dashboard was built and may need revision as the project matures.

## How to read the dashboard

Visit <https://dlrivada.github.io/Encina/coverage/>. Key elements:

- **Headline percentage** at the top with its obligations breakdown and link to the triggering CI run.
- **Coverage Over Time** chart with toggle between combined and per-flag views.
- **Coverage Distribution** (sunburst) showing which packages contribute which portions.
- **By Package table** with sortable columns for each flag's coverage and target. Package names can be filtered by search or by prefix. A "Show Lines" toggle switches from percentages to raw line counts.
- **File Detail** expansions under each package row.
- **Copy to Excel** button exports the current table as TSV.

Every cell is colored by the pass/warn/fail rules above. Cells with no data (flag not applicable, or no test artifact arrived) are shown as grey with an explanatory label.

## Relationship to other measurements

- **Code coverage** (this document) measures *which lines are tested*.
- **Performance measurement** (see [`performance-measurement-methodology.md`](performance-measurement-methodology.md)) measures *how fast* those lines run and *how much memory* they use.

The two systems share an architectural lineage — same scripts layout, same dashboard pattern, same history + recalculation model — but are independent. A code change can improve one without affecting the other, and a methodology change in one does not propagate to the other.

## Working with this document

Changes to the formulas in this document require:

1. A corresponding change in `.github/scripts/coverage-report.cs` (or the relevant script).
2. A `coverage-recalculate.cs` run to propagate the change across recent history.
3. An update to this document explaining what changed and why.

The document is living because the methodology is living. Honesty about what we measure — and what we cannot — is more important than hiding imperfection behind a clean percentage.

## Related

- [ADR-023 — Coverage Strategy (Codecov + SonarCloud)](../architecture/adr/023-coverage-strategy-codecov-sonarcloud.md)
- [ADR-025 — Performance Measurement Infrastructure](../architecture/adr/025-performance-measurement-infrastructure.md) (architectural sibling)
- [`performance-measurement-methodology.md`](performance-measurement-methodology.md) (sibling methodology)
- Scripts: `.github/scripts/coverage-report.cs`, `.github/scripts/coverage-history.cs`, `.github/scripts/coverage-recalculate.cs`, `.github/scripts/generate-coverage-manifest.cs`
- Dashboard: <https://dlrivada.github.io/Encina/coverage/>
