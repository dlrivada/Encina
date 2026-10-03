<!-- issue
title: [TEST] Encina.ADO.PostgreSQL: BulkOperations guard tests, DI ValidateOnBuild test, and unit-flag coverage re-check
labels: area-testing, area-database
milestone: v0.14.0 — Hardening
-->

## Test Category

- [x] Unit Tests
- [ ] Integration Tests (Docker/Testcontainers)
- [ ] Property-Based Tests (FsCheck)
- [ ] Contract Tests
- [x] Guard Clause Tests
- [ ] Load Tests (NBomber)
- [ ] Benchmark Tests (BenchmarkDotNet)
- [x] Coverage Gap (below 85% target)

## Description

Three test gaps found during the SPEC-003 pilot-2 deep quality audit of `Encina.ADO.PostgreSQL`:

1. **AUD-05**: `Encina.ADO.SqlServer` has
   `tests/Encina.GuardTests/ADO/SqlServer/BulkOperationsADOGuardTests.cs`; PostgreSQL and MySQL
   have neither an equivalent guard-test file nor a justification `.md` for
   `BulkOperations/BulkOperationsPostgreSQL.cs`. CLAUDE.md never allows a justification for
   guard tests, so this is a genuine gap, not a documented exception.
2. **AUD-17**: no test in `tests/` builds the `Encina.ADO.PostgreSQL` DI graph with
   `ValidateOnBuild`+`ValidateScopes` and resolves every registered service. Manual review of
   `ServiceCollectionExtensions.cs` found no live resolution-failure bug (unlike #1273's MongoDB
   case), but the safety net that would have caught a future regression of that kind does not
   exist for this package.
3. **AUD-03**: a file-level proxy of the coverage obligations model measured the `unit` flag at
   29.6% against the manifest's 30% target (guard 26.0%/10%, contract 92.9%/5% and integration
   39.2%/25% all clear their targets by the same method). The proxy was not run through
   `.github/scripts/coverage-report.cs` itself, so this needs a CI Full run (or a local run of
   that script) to confirm whether the unit flag actually passes or needs a small addition.

## Packages / Providers Affected

- **Package(s)**: Encina.ADO.PostgreSQL
- **Provider(s)**: ADO-PostgreSQL

## Current Coverage

| Package | Flag | Line Coverage (proxy) | Target | Gap |
|---------|------|:---------------------:|:------:|:---:|
| Encina.ADO.PostgreSQL | unit | 29.6% | 30% | -0.4pt (needs official re-check) |
| Encina.ADO.PostgreSQL | guard | 26.0% | 10% | pass |
| Encina.ADO.PostgreSQL | contract | 92.9% | 5% | pass |
| Encina.ADO.PostgreSQL | integration | 39.2% | 25% | pass |

## Infrastructure Required

- [ ] Docker / Testcontainers
- [ ] Real database (specify: SQL Server / PostgreSQL / MySQL / MongoDB)
- [ ] Message broker (specify: RabbitMQ / Kafka / NATS / MQTT)
- [ ] NBomber load testing framework
- [ ] BenchmarkDotNet
- [x] None (pure unit tests) — for items 1 and 2; item 3 is a measurement re-run only

## Test Plan

### Tests to Implement

- [ ] `BulkOperationsPostgreSQLGuardTests.cs` mirroring `BulkOperationsADOGuardTests.cs` (SqlServer), covering null/invalid-argument guard clauses on `BulkOperationsPostgreSQL`'s public methods.
- [ ] A DI-graph test that calls `AddEncinaADO(...)` (and the tenancy/sharding/migration extension variants) on a real `ServiceCollection`, then `BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true })`, and resolves every service the extension registers.
- [ ] Run `.github/scripts/coverage-report.cs` (or the CI Full pipeline) against fresh per-flag Cobertura output for this package and confirm the unit flag's official percentage; add a small number of unit tests if it is genuinely short of 30%.

### Success Criteria

- [ ] All new tests pass
- [ ] Coverage meets the manifest's per-flag targets (unit 30 / guard 10 / contract 5 / integration 25), confirmed by the official script
- [ ] No flaky tests introduced
- [ ] Tests run within acceptable time limits

## Collection Fixture (Integration Tests Only)

N/A — these are guard/DI-graph tests, not integration tests.

## Related Issues

- #1273 - MongoDB inbox orchestrator resolution failure (the pattern the DI-graph test guards against)
- #667 - TimeProvider decision this package's ServiceCollectionExtensions already conforms to for its stores' constructors
