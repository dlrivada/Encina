<!-- issue
title: [DEBT] Add an architecture test for DI registration completeness (ambient-context resolution and options-for-orchestrator pairs)
labels: technical-debt, area-architecture-testing, area-testing
milestone:
-->

## Type

- [x] Missing tests
- [x] Incorrect implementation

## Description

Three closed issues sampled by the knowledge-migration pilot (#1163, #1273, and #1260 seen in the broader issue list) are all instances of the same bug class: a `ServiceCollectionExtensions` registers a consumer type without registering something that consumer depends on, so DI resolution either throws at first use or — worse, as in #1163 — silently returns `null` because the missing type is ambient context rather than a required constructor parameter.

- #1163: ~28 call sites across ADO/Dapper/EFCore/MongoDB/ABAC/Secrets/NIS2/Messaging resolved `IRequestContext` directly from DI. `IRequestContext` is never registered (it is ambient per-request state meant to be read through `IRequestContextAccessor`), so every one of those sites always got `null`, with no exception and no log.
- #1273: `AddEncinaMongoDB` registered `InboxOrchestrator` without registering the `InboxOptions` it depends on.
- #1260 (not sampled in depth, but the pattern matches): `Encina.ADO.MySQL` registered no Unit of Work although `UnitOfWorkADO` exists.

Each was fixed individually, but nothing in the test suite or architecture rules would catch a fourth instance of the same class before it ships.

## Location

- **File(s)**: `src/Encina.Testing.Architecture/*.cs` (new rule class, alongside `EventIdUniquenessRule.cs`); `tests/Encina.UnitTests/Testing/Architecture/*.cs` (new test applying the rule to every provider assembly)
- **Package(s)**: Encina.Testing.Architecture, and every package with a `ServiceCollectionExtensions.cs` (ADO ×3, Dapper ×3, EntityFrameworkCore, MongoDB, Security.ABAC, Security.Secrets, Messaging, and satellite compliance packages)

## Current Behavior

Registration gaps are only caught when a consumer is actually resolved at runtime (or, for ambient-context types resolved directly instead of through their accessor, not caught at all — the resolution simply returns `null`). Coverage and guard tests do not exercise "build the full `IServiceCollection` for provider X with every `Use*` flag enabled and resolve every registered service" as a class of test.

## Expected Behavior

An architecture-level test (mirroring the existing `EventIdUniquenessRule` pattern in `Encina.Testing.Architecture`) that, for each provider package's `ServiceCollectionExtensions`, builds a `ServiceCollection` with the relevant `Use*` options enabled and asserts:

1. Every service registered by that extension method resolves without throwing.
2. No type in `src/` that is meant to be ambient (starting with `IRequestContext`, and any future accessor-pattern types) is ever resolved directly from `IServiceProvider` outside of its own `*Accessor` implementation — a static analysis rule similar to how `EventIdUniquenessRule` scans for literal `new EventId(...)` allocations.

## Root Cause

Registration completeness for provider packages is currently verified only by hand-written unit tests per package, which each new provider/orchestrator combination has to remember to write. There is no structural guarantee, so the same class of gap has recurred at least three times in one week (Sept 2026) across otherwise unrelated packages.

## Proposed Fix

1. Add a `DiRegistrationCompletenessRule` (or extend `EncinaArchitectureRulesBuilder`) in `Encina.Testing.Architecture` that takes a `ServiceCollectionExtensions` entry point plus the `Use*` options needed to enable every registered branch, and asserts every registered service type resolves.
2. Add a companion static-analysis check (source scan, similar to the `EventIdUniquenessRule`'s literal-`EventId` scan) that flags any `IServiceProvider.GetService<IRequestContext>()` / `GetRequiredService<IRequestContext>()` call outside `IRequestContextAccessor`'s own implementation.
3. Apply both to every package listed above in `tests/Encina.UnitTests/Testing/Architecture/`, following the same per-assembly registration pattern `EncinaEventIdAllocationTests.cs` uses for EventId ranges.
4. Register any new EventIds or PublicAPI entries the new rule class needs, per CLAUDE.md's existing EventId Allocation Workflow.

## Priority

- [x] **Medium** - Should be fixed before 1.0 release

## Effort Estimate

- [x] Large (> 4 hours)

## Related Issues

- #1163 — About 28 call sites resolve an unregistered IRequestContext from DI and always get null
- #1273 — AddEncinaMongoDB registers InboxOrchestrator without registering InboxOptions
- #1260 — Encina.ADO.MySQL registers no Unit of Work although UnitOfWorkADO exists
- Pilot source record: `artifacts/pilot/records/1163.md`, `artifacts/pilot/records/1273.md` (knowledge-migration pilot, 2026-09-24)
