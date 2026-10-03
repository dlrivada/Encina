<!-- issue
title: [TEST] Encina.Compliance.Consent: contract tests are reflection-only, no Marten integration test, no DI ValidateOnBuild test
labels: area-testing, area-compliance, ai:claude-required
milestone: v0.14.0 — Hardening
-->

## Test Category

- [ ] Unit Tests
- [x] Integration Tests (Docker/Testcontainers)
- [ ] Property-Based Tests (FsCheck)
- [x] Contract Tests
- [ ] Guard Clause Tests
- [ ] Load Tests (NBomber)
- [ ] Benchmark Tests (BenchmarkDotNet)
- [x] Coverage Gap (below manifest target)

## Description

Three related test gaps in `Encina.Compliance.Consent` (Marten/PostgreSQL provider), found during the SPEC-003 deep quality audit of the package (2026-09-24):

1. **Reflection-only contract tests**: `tests/Encina.ContractTests/Compliance/Consent/IConsentServiceContractTests.cs` has 48 passing tests, but every one only calls `typeof(IConsentService).GetMethod(...)`/`.GetParameters()`/`.ReturnType` — no real implementation of `IConsentService` is ever instantiated or invoked. Measured contract-flag coverage of the package is 0% against the manifest's 15% target (`.github/coverage-manifest/Encina.Compliance.Consent.json`), which is exactly the "reflection-only tests cover zero lines" pattern `CLAUDE.md` calls out under "Tests must execute real package code".
2. **Missing Marten integration test**: `tests/Encina.IntegrationTests/Compliance/Consent/ConsentPipelineIntegrationTests.cs` has 8 tests, all covering only DI registration and options validation. The file's own doc comment states that the full consent flow (grant/withdraw/validate) "require[s] a real PostgreSQL + Marten backend and [is] covered by Marten-specific integration tests" — but no such test file exists anywhere in the repository. `CLAUDE.md`'s event-sourced-module rule ("Rules for event-sourced modules") requires integration tests against Marten on PostgreSQL through Testcontainers for compliance modules; `Encina.Compliance.Consent` has none.
3. **Missing DI validation test**: no test under `tests/Encina.UnitTests` or `tests/Encina.IntegrationTests` builds a `ServiceCollection` with `ValidateOnBuild=true` and `ValidateScopes=true` after `AddEncinaConsent()`/`AddConsentAggregates()` and resolves every service they register. This is exactly the defect class of #1273 (inbox orchestrator that could not resolve its options on MongoDB) and #522.

## Packages / Providers Affected

- **Package(s)**: Encina.Compliance.Consent
- **Provider(s)**: Marten/PostgreSQL (the only provider this event-sourced module ships on, per ADR-019)

## Current Coverage

| Package | Flag | Measured (proxy) | Target | Gap |
|---------|------|:-----------------:|:------:|:---:|
| Encina.Compliance.Consent | contract | 0% | 15% | -15pp |
| Encina.Compliance.Consent | guard | 11.3% | 20% | -8.7pp |
| Encina.Compliance.Consent | property | 10.9% | 15% | -4.1pp |

Measured locally on 2026-09-24 with `dotnet test <flag-project> --filter FullyQualifiedName~Compliance.Consent --collect:"XPlat Code Coverage"` and a one-off aggregation script; this is a raw-line proxy, not the authoritative `coverage-report.cs` obligations model, but the contract-flag gap in particular is unambiguous: the only contract-flagged file (`ConsentRequiredPipelineBehavior.cs`) measures 0%.

## Infrastructure Required

- [x] Docker / Testcontainers
- [x] Real database (specify: PostgreSQL, via `Encina.Testing.Testcontainers`, for Marten)
- [ ] Message broker (specify: RabbitMQ / Kafka / NATS / MQTT)
- [ ] NBomber load testing framework
- [ ] BenchmarkDotNet
- [ ] None (pure unit tests)

## Test Plan

### Tests to Implement

- [ ] Rewrite `IConsentServiceContractTests.cs` (and/or add a sibling file) to instantiate `DefaultConsentService`/`DefaultConsentValidator`/`ConsentRequiredPipelineBehavior<,>` against fakes/mocks (`IAggregateRepository<ConsentAggregate>`, `IReadModelRepository<ConsentReadModel>`, `ICacheProvider`) and assert on real behaviour, not just reflected metadata.
- [ ] Add a Testcontainers-backed Marten/PostgreSQL integration test that grants, withdraws, renews and queries consent end to end through `ConsentAggregate` + `ConsentProjection` + `ConsentReadModel`, following the pattern `CLAUDE.md`'s "Rules for event-sourced modules" describes (real store, no InMemory substitute).
- [ ] Add a unit or integration test that builds the DI container with `ValidateOnBuild=true` and `ValidateScopes=true` after `AddEncinaConsent()` (and `AddConsentAggregates()` where Marten is registered) and resolves every service they register, per AUD-17 and the #1273/#522 defect class.

### Success Criteria

- [ ] All new tests pass
- [ ] Contract, guard and property flags reach their manifest targets (15%, 20%, 15% respectively) for `Encina.Compliance.Consent`
- [ ] No flaky tests introduced
- [ ] Tests run within acceptable time limits

## Collection Fixture (Integration Tests Only)

> Per CLAUDE.md: Integration tests MUST use shared `[Collection]` fixtures.

- **Collection**: none exists yet for Marten; a shared Marten/PostgreSQL fixture (e.g. `Marten-PostgreSQL`) would need to be introduced, consistent with the existing `ADO-*`/`Dapper-*`/`EFCore-*` collection pattern, or the Consent tests should join an existing Marten collection if one is added by a sibling compliance-module test effort.
- **Fixture**: to be created (e.g. `MartenFixture`/`ConsentMartenFixture`), following `docs/testing/integration-tests.md#collection-fixture-strategy`.

## Related Issues

- #1273 — DI resolution defect class (MongoDB inbox orchestrator)
- #522 — DI resolution defect class
- #777 — Marten migration that left the module without a real Marten-backed integration test
- Found during the SPEC-003 deep quality audit of `Encina.Compliance.Consent` (2026-09-24), checklist items AUD-04, AUD-05 and AUD-17; findings F3, F5 and F9 in `artifacts/audit/findings.csv` (audit worktree)
