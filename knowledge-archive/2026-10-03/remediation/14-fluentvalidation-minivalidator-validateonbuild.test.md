<!-- issue
title: [TEST] Add ValidateOnBuild DI proof tests for Encina.FluentValidation and Encina.MiniValidator
labels: area-testing, area-validation
milestone: v0.14.0 — Hardening
-->

## Test Category

- [x] Unit Tests
- [ ] Integration Tests (Docker/Testcontainers)
- [ ] Property-Based Tests (FsCheck)
- [ ] Contract Tests
- [ ] Guard Clause Tests
- [ ] Load Tests (NBomber)
- [ ] Benchmark Tests (BenchmarkDotNet)
- [ ] Coverage Gap (below 85% target)

## Description

Found by the SPEC-003 audit of #14 (Encina.Validation orchestrator refactor), verified by an `adversarial-reviewer` pass on 2026-09-25.

`tests/Encina.UnitTests/FluentValidation/ServiceCollectionExtensionsTests.cs` (e.g. the `BuildServiceProvider()` calls at lines 41, 160, 178, 198, 217, 233, 250) and `tests/Encina.UnitTests/MiniValidator/ServiceCollectionExtensionsTests.cs` (lines 26, 54, 64, 84, 87, 102, 118) all call plain `services.BuildServiceProvider()`, never `new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }`. CLAUDE.md's registration-completeness rule (project history #1260, #1273, #1285, #1289) requires that a registration method (`AddEncinaFluentValidation`, `AddMiniValidation`) be proven, by a DI test, to build cleanly with both options — not inferred by manual review.

Manual review of the current DI graphs did not find a captive-dependency defect: `Encina.FluentValidation`'s `AddEncinaFluentValidation` registers `IValidationProvider` and `ValidationOrchestrator` as `TryAddScoped` and `ValidationPipelineBehavior<,>` as `TryAddTransient` (`src/Encina.FluentValidation/ServiceCollectionExtensions.cs:62-64`), and `Encina.MiniValidator`'s `AddMiniValidation` registers everything as `TryAddSingleton`/`TryAddTransient` (`src/Encina.MiniValidator/ServiceCollectionExtensions.cs:65-67`) — both look safe. But "looks safe on review" is exactly what the registration-completeness rule exists to stop relying on.

This is the same class of gap as #1337, which already covers `Encina.DataAnnotations`'s missing `ValidateOnBuild` test (plus its own context-propagation and cancellation-token gaps, which do not apply here in the same way — see below). This issue covers only the remaining two packages, `Encina.FluentValidation` and `Encina.MiniValidator`, so as not to duplicate #1337.

## Packages / Providers Affected

- **Package(s)**: Encina.FluentValidation, Encina.MiniValidator
- **Provider(s)**: FluentValidationProvider, MiniValidationProvider

## Current Coverage

> Not a coverage-gap issue; both packages already meet their unit/guard manifest targets. The gap is a missing DI-validation assertion, not a line-coverage shortfall.

## Infrastructure Required

- [ ] Docker / Testcontainers
- [ ] Real database (specify: SQL Server / PostgreSQL / MySQL / MongoDB)
- [ ] Message broker (specify: RabbitMQ / Kafka / NATS / MQTT)
- [ ] NBomber load testing framework
- [ ] BenchmarkDotNet
- [x] None (pure unit tests)

## Test Plan

### Tests to Implement

- [ ] Add a test in `tests/Encina.UnitTests/FluentValidation/ServiceCollectionExtensionsTests.cs` that calls `services.AddEncinaFluentValidation(assembly)` then `services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true })` and asserts it does not throw.
- [ ] Add the equivalent test for the `ServiceLifetime` overload of `AddEncinaFluentValidation` (Scoped and Transient), since validator lifetime is caller-configurable and each combination should build cleanly.
- [ ] Add a test in `tests/Encina.UnitTests/MiniValidator/ServiceCollectionExtensionsTests.cs` that calls `services.AddMiniValidation()` then builds with the same `ServiceProviderOptions` and asserts it does not throw.

### Success Criteria

- [x] All new tests pass
- [x] Coverage meets manifest target per flag
- [x] No flaky tests introduced
- [x] Tests run within acceptable time limits

## Collection Fixture (Integration Tests Only)

- **Collection**: N/A
- **Fixture**: N/A

## Related Issues

- #1337 — same gap for Encina.DataAnnotations (ValidateOnBuild DI test, plus its own context-propagation and cancellation-token test gaps).
- #14 — the Encina.Validation orchestrator refactor this audit covers.

Searched `gh issue list --repo dlrivada/Encina --state open --search "ValidateOnBuild FluentValidation"` and `"MiniValidator DI"` on 2026-09-25; no open issue besides #1337 tracks this gap.
