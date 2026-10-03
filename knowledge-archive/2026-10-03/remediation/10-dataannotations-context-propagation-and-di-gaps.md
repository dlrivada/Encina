<!-- issue
title: [TEST] Encina.DataAnnotations: no regression test for context propagation, DI ValidateOnBuild, or cancellation-token behavior
labels: area-testing, area-validation, ai:claude-required
milestone: v0.19.0 — Providers & Testing
-->

## Test Category

- [x] Unit Tests
- [ ] Integration Tests (Docker/Testcontainers)
- [x] Property-Based Tests (FsCheck)
- [ ] Contract Tests
- [ ] Guard Clause Tests
- [ ] Load Tests (NBomber)
- [ ] Benchmark Tests (BenchmarkDotNet)
- [ ] Coverage Gap (below 85% target)

## Description

Found by the SPEC-003 audit of #10 (which fixed a `CustomValidationAttribute` reflection failure in this package's property tests) and verified by an `adversarial-reviewer` pass on 2026-09-25.

Three gaps in `Encina.DataAnnotations`:

1. **Context propagation has no regression test.** `DataAnnotationsValidationProvider.ValidateAsync` (`src/Encina.DataAnnotations/DataAnnotationsValidationProvider.cs:30-42`) sets `ValidationContext.Items["CorrelationId"|"UserId"|"TenantId"]` from the `IRequestContext` so that a custom `ValidationAttribute` can read them — the exact mechanism issue #10's `ContextAwareCommand` scenario exercised. Today's property tests (`tests/Encina.PropertyTests/Validation/DataAnnotations/ValidationInvariantProperties.cs:403-456`, `ValidateAsync_ContextWith{UserId,TenantId,UserIdAndTenantId}_ValidationStillSucceeds`) only assert `result.IsValid`; they would pass identically even if the provider never touched `context` at all. The package's `README.md` advertises "🔄 Context Enrichment" and a "Context-Aware Validation" example reading `validationContext.Items["UserId"]` as a feature, so this is a documented, user-facing behavior with zero regression coverage — the SPEC-003 AUD-01 lesson from issue #10 (types must stay public and reachable for reflection-based custom validators) has no test trace left after the test-consolidation rewrite (commit d7b7b8ac) dropped the `CustomValidationAttribute` scenario.
2. **No DI registration-completeness test.** `ServiceCollectionExtensionsTests.CreateProvider()` (`tests/Encina.UnitTests/DataAnnotations/ServiceCollectionExtensionsTests.cs:19-24`) calls plain `services.BuildServiceProvider()`, never `new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }`, so `AddDataAnnotationsValidation()` has no test proving CLAUDE.md's registration-completeness rule (every registered service's dependencies resolve).
3. **`CancellationToken` behavior is undocumented by any test.** `ValidateAsync`'s `cancellationToken` parameter (`DataAnnotationsValidationProvider.cs:25`) is accepted but never consulted — `Validator.TryValidateObject` is synchronous and CPU-bound — and no test exercises an already-cancelled token or documents that this provider ignores cancellation by design.

## Packages / Providers Affected

- **Package(s)**: Encina.DataAnnotations
- **Provider(s)**: DataAnnotationsValidationProvider

## Current Coverage

> Not a coverage-gap issue; `Encina.DataAnnotations` unit (90.9%/100%) and property (100%/100%) flags already exceed their manifest targets for the two source files. The gap is in test *scope* (what is asserted), not line coverage.

## Infrastructure Required

- [ ] Docker / Testcontainers
- [ ] Real database (specify: SQL Server / PostgreSQL / MySQL / MongoDB)
- [ ] Message broker (specify: RabbitMQ / Kafka / NATS / MQTT)
- [ ] NBomber load testing framework
- [ ] BenchmarkDotNet
- [x] None (pure unit tests)

## Test Plan

### Tests to Implement

- [ ] A property or unit test that registers a custom `ValidationAttribute` reading `ValidationContext.Items["CorrelationId"]`/`["UserId"]`/`["TenantId"]` and asserts the values match the `IRequestContext` passed to `ValidateAsync`.
- [ ] Change `ServiceCollectionExtensionsTests.CreateProvider()` to build with `new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }`.
- [ ] A unit test asserting that calling `ValidateAsync` with an already-cancelled `CancellationToken` does not throw (documents the synchronous, cancellation-agnostic behavior by design).

### Success Criteria

- [x] All new tests pass
- [x] Coverage meets manifest target per flag
- [x] No flaky tests introduced
- [x] Tests run within acceptable time limits

## Collection Fixture (Integration Tests Only)

- **Collection**: N/A
- **Fixture**: N/A

## Related Issues

- #10 - the original `CustomValidationAttribute` visibility fix this issue's finding traces back to.

None found tracking this specific gap: searched `gh issue list --repo dlrivada/Encina --state open --search "DataAnnotations context"` and related terms on 2026-09-25.
