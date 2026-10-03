<!--
title: [DEBT] Encina.Testing has no package README
labels: technical-debt
milestone: 
kind: docs
-->

## Type

- [ ] Failing tests
- [ ] Missing tests
- [ ] Code quality (warnings, analyzers)
- [ ] Performance optimization
- [ ] Refactoring needed
- [x] Documentation gap
- [ ] Incorrect implementation
- [ ] Other

## Description

The `Encina.Testing` package has no README. `AGENTS.md` section 8 requires each satellite package to have its own README, and `Encina.Testing` is the package users reference to test Encina handlers, sagas, aggregates, messaging helpers and modules. It exposes a broad public surface (`EncinaFixture`, `EncinaTestFixture`, `HandlerSpecification<TRequest, TResponse>`, `SagaSpecification<TSaga, TSagaData>`, `AggregateTestBase<TAggregate, TId>`, `FakeTimeProvider`, `OutboxTestHelper`, `ModuleTestFixture<TModule>` and the `Either` assertions in `EitherAssertions`), yet there is no page that introduces it, shows how to install it or lists what it contains.

## Location

- **File(s)**: `src/Encina.Testing/README.md` (missing); the project file is `src/Encina.Testing/Encina.Testing.csproj`, which sets `PackageId`, `Description` and `PackageTags` but no `PackageReadmeFile` and no `None Include` item that packs a README (only `LICENSE` is packed, lines 40-42). No `*.md` file exists anywhere under `src/Encina.Testing/`.
- **Package(s)**: Encina.Testing

## Current Behavior

`src/Encina.Testing/` contains 30 `.cs` files and no Markdown file. The NuGet package is built from `Encina.Testing.csproj` without a README, so the package page shows only the one-line `Description` ("Testing utilities for Encina - fixtures, fluent assertions, and mock helpers for Railway Oriented Programming."). A reader cannot find, from the package, the entry points under `src/Encina.Testing/` (`EncinaFixture.cs:42`, `EncinaTestFixture.cs:56`, `EncinaTestContext.cs:49`, `Handlers/HandlerSpecification.cs:47`, `Sagas/SagaSpecification.cs:66`, `EventSourcing/AggregateTestBase.cs:44`, `Time/FakeTimeProvider.cs:30`, `Messaging/OutboxTestHelper.cs:33`, `Modules/ModuleTestFixture.cs:59`, `Assertions/EitherAssertions.cs`).

## Expected Behavior

`src/Encina.Testing/README.md` exists and is packed with the NuGet package (through `PackageReadmeFile`). It states what the package is for, how to install it, and shows a short example for each main area (fixtures, `Either` assertions such as `ShouldBeSuccess`, handler and saga specifications, aggregate tests, fake time, messaging helpers, module testing), and links to the related testing documentation and to `Encina.Testing.Fakes`, which the package references.

## Root Cause

The package grew across several issues after its first delivery, and none of them added a README for it.

## Proposed Fix

Write `src/Encina.Testing/README.md` following the structure of the other satellite package READMEs, verify every type name and example against `src/Encina.Testing/`, and add the `PackageReadmeFile` property and the `None Include ... Pack="true"` item for the README to `Encina.Testing.csproj`.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [ ] **Medium** - Should be fixed before 1.0 release
- [x] **Low** - Nice to have, can be deferred

## Effort Estimate

- [ ] Small (< 1 hour)
- [x] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

- #23 (This issue)
