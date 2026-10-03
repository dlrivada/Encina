<!-- issue
title: [DEBT] Encina.Marten: InlineProjectionRelay logs EncinaError.Message, and AddEncinaMarten/AddSnapshotableAggregate have no ValidateOnBuild DI test
labels: technical-debt, area-event-sourcing
milestone: v0.14.0 — Hardening
-->

## Type

- [ ] Failing tests
- [x] Missing tests
- [ ] Code quality (warnings, analyzers)
- [ ] Performance optimization
- [ ] Refactoring needed
- [ ] Documentation gap
- [x] Incorrect implementation
- [ ] Other

## Description

Found by the SPEC-003 audit of #15 (adversarial-reviewer pass on `Encina.Marten`). Two independent, small gaps in the same package:

1. `Projections/InlineProjectionRelay.cs:134-137` passes `error.Message` (from a failed `IInlineProjectionDispatcher.DispatchManyAsync` call) into `ProjectionLog.InlineProjectionFailedAfterSave`, violating CLAUDE.md's rule that `EncinaError.Message` never reaches logs — only the error code or exception type may be recorded (project history #1168, #1173, #1259, #1274). This is the same defect class already tracked for `EventPublishingPipelineBehavior.cs:87` in the same package by open issue #1328, but at a different call site not covered by that issue.
2. No test in the repository builds the `Encina.Marten` `IServiceProvider` with `ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }` (grep for that string under `tests/**/*Marten*` returns zero matches), so `AddEncinaMarten` and `AddSnapshotableAggregate<TAggregate>` are not proven correct by the CLAUDE.md-mandated registration-completeness DI test (project history #1260, #1273, #1285, #1289).

## Location

- **File(s)**:
  - `src/Encina.Marten/Projections/InlineProjectionRelay.cs:134-137`
  - `src/Encina.Marten/ServiceCollectionExtensions.cs` (`AddEncinaMarten`, `AddSnapshotableAggregate<TAggregate>`)
  - `tests/Encina.UnitTests/Marten/ServiceCollectionExtensionsTests.cs` (and siblings) — missing DI-validation test
- **Package(s)**: Encina.Marten

## Current Behavior

- `InlineProjectionRelay.cs:134-137` logs the raw `error.Message` text from a projection-dispatch failure.
- `ServiceCollectionExtensionsTests.cs` calls `BuildServiceProvider()` without `ValidateOnBuild`/`ValidateScopes` (lines 20, 42, 223), so a missing dependency in `AddEncinaMarten`/`AddSnapshotableAggregate` would not be caught by any existing test.

## Expected Behavior

- `InlineProjectionRelay` logs `error.GetEncinaCode()` (and the exception type where available), never the free-text `Message`, matching the pattern already used by `CommandMetricsPipelineBehavior`/`QueryMetricsPipelineBehavior` and the fix applied for #1168/#1173/#1259/#1274.
- A test builds the `Encina.Marten` service collection with `ValidateOnBuild: true, ValidateScopes: true` for both `AddEncinaMarten` (with projections/snapshots/versioning/metadata enabled) and `AddSnapshotableAggregate<TAggregate>`, proving every dependency the repositories need actually resolves.

## Root Cause

`InlineProjectionRelay` was written before the no-message-leak rule was established project-wide, and its only existing test coverage does not exercise the logging call site with an asserting spy. The DI-validation test convention (#1260, #1273, #1285, #1289) postdates the original `Encina.Marten` registration code and was never retrofitted to this package's test suite.

## Proposed Fix

1. Change `InlineProjectionRelay.cs:134-137` to log `error.GetEncinaCode()` instead of `error.Message`; update the `ProjectionLog.InlineProjectionFailedAfterSave` template placeholder accordingly; add a regression test asserting the log never receives the literal message text.
2. Add a `ValidateOnBuild`/`ValidateScopes` DI test to `tests/Encina.UnitTests/Marten/ServiceCollectionExtensionsTests.cs` covering `AddEncinaMarten` with every optional feature flag enabled, and a second one covering `AddSnapshotableAggregate<TAggregate>`.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [x] **Medium** - Should be fixed before 1.0 release
- [ ] **Low** - Nice to have, can be deferred

## Effort Estimate

- [x] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

Found by the SPEC-003 audit of #15. Related: #1328 (same message-leak defect class, different call site in the same package), #1260/#1273/#1285/#1289 (registration-completeness DI test convention).
