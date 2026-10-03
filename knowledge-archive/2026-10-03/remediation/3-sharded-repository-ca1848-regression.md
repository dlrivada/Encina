<!-- issue
title: [DEBT] FunctionalShardedRepository (ADO x3, Dapper x3, EF Core, MongoDB) logs via raw ILogger extension methods instead of Log.cs
labels: technical-debt, area-performance
milestone: v0.14.0 — Hardening
-->

## Type

- [x] Code quality (warnings, analyzers)
- [ ] Failing tests
- [ ] Missing tests
- [x] Performance optimization
- [ ] Refactoring needed
- [ ] Documentation gap
- [ ] Incorrect implementation
- [ ] Other

## Description

Found by the SPEC-003 audit of #3. Issue #3 (closed 2025-12-23) migrated every messaging-transport and data-access package from `#pragma warning disable CA1848` to `[LoggerMessage]` source-generator `Log.cs` partial classes. `FunctionalShardedRepository*` was added later (feature #289, 2026-02-10) in eight packages and calls `ILogger` extension methods (`LogDebug`/`LogWarning`/`LogInformation`) directly, with no `Log.cs` entries and no EventIds — reintroducing the exact pattern #3 removed, in packages #3 itself had already fixed.

## Location

- **File(s)**:
  - `src/Encina.ADO.SqlServer/Sharding/FunctionalShardedRepositoryADO.cs` (lines 261, 296, 302, 323, 361, 367, and others — ~22 raw calls)
  - `src/Encina.ADO.PostgreSQL/Sharding/FunctionalShardedRepositoryADO.cs` (same pattern, ~22 calls)
  - `src/Encina.ADO.MySQL/Sharding/FunctionalShardedRepositoryADO.cs` (same pattern, ~22 calls)
  - `src/Encina.Dapper.SqlServer/Sharding/FunctionalShardedRepositoryDapper.cs` (same pattern, ~22 calls)
  - `src/Encina.Dapper.PostgreSQL/Sharding/FunctionalShardedRepositoryDapper.cs` (same pattern, ~22 calls)
  - `src/Encina.Dapper.MySQL/Sharding/FunctionalShardedRepositoryDapper.cs` (same pattern, ~22 calls)
  - `src/Encina.EntityFrameworkCore/Sharding/FunctionalShardedRepositoryEF.cs` (~22 calls)
  - `src/Encina.MongoDB/Sharding/FunctionalShardedRepositoryMongoDB.cs` (~32 calls)
- **Package(s)**: Encina.ADO.SqlServer, Encina.ADO.PostgreSQL, Encina.ADO.MySQL, Encina.Dapper.SqlServer, Encina.Dapper.PostgreSQL, Encina.Dapper.MySQL, Encina.EntityFrameworkCore, Encina.MongoDB

## Current Behavior

`FunctionalShardedRepository*` classes call `_logger.LogDebug(...)`, `_logger.LogWarning(...)`, `_logger.LogInformation(...)` directly. No `[LoggerMessage]` source generator, no `EventId`, and the calls are not routed through each package's existing `Log.cs`. CA1848 is not enforced as an error (`.editorconfig` sets `dotnet_diagnostic.CA1848.severity = suggestion`), and no architecture test asserts that all production logging in a package goes through its `Log.cs`, so this was not caught in CI.

## Expected Behavior

Every log call in `FunctionalShardedRepository*` is a call to a `[LoggerMessage]`-generated static partial method declared in the package's existing `Log.cs`, with an `EventId` inside that package's registered range in `src/Encina/Diagnostics/EventIdRanges.cs`, consistent with every other class in the same package.

## Root Cause

`FunctionalShardedRepository*` was added by a later sharding feature (#289) without following the CA1848/`Log.cs` convention that issue #3 established and that `CLAUDE.md` documents as mandatory; `CA1848` is a suggestion-level analyzer diagnostic, not an error, so it did not block the PR.

## Proposed Fix

1. Add the missing `[LoggerMessage]` methods to each package's existing `Log.cs` (new EventIds packed at the end of that package's registered range).
2. Replace every raw `_logger.LogX(...)` call in the eight `FunctionalShardedRepository*` files with the corresponding `Log.X(_logger, ...)` call.
3. Consider adding an architecture test (or extending `EncinaEventIdAllocationTests`) that flags direct `ILogger.LogX` extension-method calls in production code outside a package's `Log.cs`, so this class of regression cannot recur silently.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [x] **Medium** - Should be fixed before 1.0 release
- [ ] **Low** - Nice to have, can be deferred

## Effort Estimate

- [ ] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [x] Large (> 4 hours)

## Related Issues

- #3 (the original CA1848 → LoggerMessage migration this regresses)
- #289 (the sharding feature that introduced `FunctionalShardedRepository*` without following the convention)
