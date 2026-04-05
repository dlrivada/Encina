---
title: "ADR-023: Coverage Strategy — Codecov Components + SonarCloud Static Analysis"
layout: default
parent: ADRs
grand_parent: Architecture
---

# ADR-023: Coverage Strategy — Codecov Components + SonarCloud Static Analysis

## Status

Accepted (March 2026)

## Context

Encina has 108 source packages with 170K+ NCLOC and 13,000+ tests across 7 types (Unit, Guard, Contract, Property, Integration, Load, Benchmark). Code coverage measurement faced three problems:

1. **Single global threshold**: SonarCloud supports only one coverage threshold for the entire project. Encina's modules have very different expected coverage levels — core business logic can achieve 85%+, but database provider stores (ADO/Dapper/EF Core/MongoDB) that execute raw SQL are covered by integration tests, not unit tests, and legitimately have lower unit test coverage.

2. **Unit tests only**: SonarCloud measured coverage only from the `Encina.UnitTests` project (67.9%), missing the 2,251 integration tests that cover the database providers. This gave an artificially low picture.

3. **Redundant test execution**: The SonarCloud workflow ran all unit tests (~45 min) redundantly with the CI workflow, which already runs them.

### Industry Context

Research into major .NET open-source projects showed:
- **dotnet/runtime, ASP.NET Core, MassTransit, Orleans, Wolverine**: No SonarCloud, no external coverage tool
- **Dapr .NET SDK**: Uses Codecov with `target: auto, threshold: 0%`
- **ABP Framework**: Uses Codecov with `threshold: 1%`

No major .NET project uses SonarCloud for coverage measurement.

## Decision

### 1. Use Codecov for coverage measurement with per-module thresholds

Codecov's [Components](https://docs.codecov.com/docs/components) feature allows defining different coverage targets per module category. Coverage is collected from 5 test types via [Flags](https://docs.codecov.com/docs/flags): Unit, Guard, Contract, Property, and Integration.

### 2. Keep SonarCloud for static analysis only

SonarCloud continues to analyze all source code for bugs, vulnerabilities, code smells, security hotspots, and duplication. It excels at this and should not be replaced.

### 3. Custom SonarCloud Quality Gate without coverage

Create a custom Quality Gate that keeps all static analysis conditions but removes the coverage threshold. Coverage enforcement is handled by Codecov status checks.

### 4. Upload coverage from CI workflow, not SonarCloud workflow

The CI workflow already runs all 5 test types with Coverlet. Codecov upload steps are added to each test job, tagged with the appropriate flag. The SonarCloud workflow is simplified to build + static analysis only, reducing its runtime from ~45 min to ~15-20 min.

### 5. The obligations model — a particular formula

Neither Codecov nor SonarCloud offers the exact measurement Encina needs. Both compute coverage as "covered lines / total lines", which loses information when a file should be exercised from multiple angles (unit + guard + integration) but is only hit by one. Encina therefore computes coverage using an **obligations model**:

- For each source file, the manifest declares which test types apply to it
- Each (flag, line) pair becomes one **obligation**
- A line covered by a test of that flag marks the corresponding obligation as met
- File, package, and overall coverage are all computed as `met obligations / total obligations × 100`

Exclusions (interfaces, generated code, metadata files) produce **zero obligations** and therefore do not affect the percentage in either direction. Files are classified automatically by `generate-coverage-manifest.cs` using glob, regex, and path rules; per-file overrides preserve manual corrections across regenerations.

The obligations model produces a lower but more informative number than naive line coverage. A file reached by unit tests alone when the manifest declares it also needs integration tests is visibly under-covered even if unit tests reach 100% of its lines. This is a deliberate choice: measurement should reward depth of testing, not just breadth.

Full details, formulas, flag semantics, exclusion patterns, dashboard coloring thresholds, and the recalculation mechanism are documented in [`docs/testing/coverage-measurement-methodology.md`](../../testing/coverage-measurement-methodology.md), which is versioned alongside the scripts that implement the model and is the canonical reference for both users and contributors.

## Module Categories and Targets

| Component | Target | Rationale |
|-----------|:------:|-----------|
| Core Logic | 85% | Critical framework code with testable business logic |
| Security | 80% | Security-critical, mostly testable with mocks |
| Compliance | 75% | Complex domain rules, some external dependencies |
| Caching | 80% | Core infrastructure, mockable interfaces |
| Database Providers | 50% | SQL-based stores, covered by integration tests |
| Message Transports | 75% | Transport abstractions, partially mockable |
| Cloud/Serverless | 60% | External cloud dependencies |
| Observability | 60% | Instrumentation wrappers, OpenTelemetry internals |
| Infrastructure | 70% | Supporting infrastructure (scheduling, locking, tenancy) |
| Validation | 80% | Input validation, fully testable |
| CDC | 60% | Streaming connectors, require real databases |
| Secrets | 60% | Cloud KMS integration, partially mockable |
| Encryption | 70% | Encrypt/decrypt logic, partially testable |
| Event Sourcing | 50% | Marten/PostgreSQL specific, integration-tested |

## Consequences

### Positive

- **True coverage picture**: Aggregates coverage from 5 test types instead of 1, giving a realistic view
- **Per-module thresholds**: Core logic held to 85%, database providers to 50% — each module judged by its nature
- **SonarCloud workflow simplified**: Reduced from ~45 min to ~15-20 min (no test execution)
- **No redundant test execution**: Tests run once in CI, not twice
- **Coverage badges and PR comments**: Codecov provides component-level breakdown in every PR

### Negative

- **Two services**: SonarCloud for quality + Codecov for coverage (instead of one)
- **Additional secret**: `CODECOV_TOKEN` to manage
- **Initial calibration**: Thresholds may need adjustment after first week of data
- **SonarCloud Quality Gate shows FAIL**: The free plan does not allow custom Quality Gates, so "Sonar way" (which includes an 80% new code coverage condition) remains active. Since coverage is no longer reported to SonarCloud, the QG will always fail on that condition. This is cosmetic — the real coverage enforcement is done by Codecov status checks.

### Neutral

- `coverlet.runsettings` becomes unused (was only for SonarCloud's OpenCover format)
- SonarCloud dashboard shows 0% coverage (expected — coverage is measured by Codecov)
- SonarCloud QG FAIL badge is expected and should be ignored for coverage; check Codecov badge instead

## Configuration

- **Codecov config**: `codecov.yml` (repository root)
- **CI upload**: `codecov/codecov-action@v5` in `.github/workflows/ci.yml`
- **SonarCloud**: `.github/workflows/sonarcloud.yml` (static analysis only)

## Related

- Methodology (canonical): [`docs/testing/coverage-measurement-methodology.md`](../../testing/coverage-measurement-methodology.md)
- Sibling ADR: [ADR-025 — Performance Measurement Infrastructure](025-performance-measurement-infrastructure.md)
- Sibling methodology: [`docs/testing/performance-measurement-methodology.md`](../../testing/performance-measurement-methodology.md)
- Issue: [#911](https://github.com/dlrivada/Encina/issues/911)
- EPIC: [#76](https://github.com/dlrivada/Encina/issues/76)
- Codecov Components: https://docs.codecov.com/docs/components
- Codecov Flags: https://docs.codecov.com/docs/flags
- Scripts: `.github/scripts/coverage-report.cs`, `coverage-history.cs`, `coverage-recalculate.cs`, `generate-coverage-manifest.cs`
- Dashboard: <https://dlrivada.github.io/Encina/coverage/>
