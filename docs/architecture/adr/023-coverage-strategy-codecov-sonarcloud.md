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

### Neutral

- `coverlet.runsettings` becomes unused (was only for SonarCloud's OpenCover format)
- Coverage data in SonarCloud dashboard will show 0% (expected, documented)

## Configuration

- **Codecov config**: `codecov.yml` (repository root)
- **CI upload**: `codecov/codecov-action@v5` in `.github/workflows/ci.yml`
- **SonarCloud**: `.github/workflows/sonarcloud.yml` (static analysis only)

## Related

- Issue: [#911](https://github.com/dlrivada/Encina/issues/911)
- EPIC: [#76](https://github.com/dlrivada/Encina/issues/76)
- Codecov Components: https://docs.codecov.com/docs/components
- Codecov Flags: https://docs.codecov.com/docs/flags
