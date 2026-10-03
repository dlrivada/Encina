<!-- issue
title: [TEST] No ValidateOnBuild/ValidateScopes test proves Core.AddEncina() registers a resolvable service graph
labels: area-testing, area-core
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

`CLAUDE.md`'s "Registration completeness" rule (project history: #1260, #1273, #1285, #1289) requires that a registration method (`AddEncina*`) that adds a service, orchestrator or hosted service also registers every option type and dependency it resolves, proven by a DI test that builds the provider with `ValidateOnBuild` and `ValidateScopes` both `true`. `src/Encina/Core/ServiceCollectionExtensions.AddEncina()` — the mediator's own core registration, and the one every other package's registration builds on — has no such test.

Found during the SPEC-003 pilot 2 deep quality audit of the core request pipeline audit unit (checklist item AUD-17).

## Packages / Providers Affected

- **Package(s)**: `Encina` (core)
- **Provider(s)**: n/a (not a database/cache/transport provider)

## Current Coverage

Not a line-coverage gap: `AddEncina` itself is well covered by `tests/Encina.UnitTests/Core/RequestContextAccessorRegistrationTests.cs`, `EncinaTests.cs` and `NestedDispatchContextTests.cs` (94.1% of `Core/ServiceCollectionExtensions.cs` under the unit flag, measured 2026-09-24). The gap is a missing *assertion*, not missing execution: every `services.BuildServiceProvider()` call found in those files (7 call sites in `RequestContextAccessorRegistrationTests.cs`) uses the default options (`ValidateOnBuild = false`, `ValidateScopes = false`), so a registration that silently fails to resolve (as happened for `Encina.MongoDB`'s `InboxOrchestrator`/`InboxOptions` in #1273) would not be caught here.

## Infrastructure Required

- [ ] Docker / Testcontainers
- [ ] Real database
- [ ] Message broker
- [ ] NBomber load testing framework
- [ ] BenchmarkDotNet
- [x] None (pure unit tests)

## Test Plan

### Tests to Implement

- [ ] `AddEncina_WithValidateOnBuildAndScopes_ResolvesEveryRegisteredService`: call `services.AddEncina(typeof(...).Assembly)`, then `services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true })`, then resolve (in a scope) every service type `AddEncina` is documented to register: `IEncina`, `IRequestContextAccessor`, `IEncinaMetrics`, `IFunctionalFailureDetector`, `IModuleHandlerRegistry`, `IOptions<NotificationDispatchOptions>`.
- [ ] Repeat with `AddEncina(configure, assemblies)` (the overload with `Action<EncinaConfiguration>`) to cover configured pipeline behaviors/pre/post-processors added via `EncinaConfiguration.AddPipelineBehavior`/`AddRequestPreProcessor`/`AddRequestPostProcessor`.

### Success Criteria

- [ ] All new tests pass
- [ ] `ValidateOnBuild`/`ValidateScopes` both `true` in the new test(s)
- [ ] No flaky tests introduced
- [ ] Tests run within acceptable time limits

## Collection Fixture (Integration Tests Only)

N/A — pure unit test, no database.

## Related Issues

- #1260, #1273, #1285, #1289 — the registration-completeness defects that motivated the `CLAUDE.md` rule this test enforces.
- SPEC-003 audit unit: `encina-core-pipeline`.
