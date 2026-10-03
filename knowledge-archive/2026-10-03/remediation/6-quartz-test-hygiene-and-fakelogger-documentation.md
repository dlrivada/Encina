<!-- issue
title: [DEBT] Document the FakeLogger<T> testing pattern and tidy Quartz log-assertion/guard-test duplication
labels: technical-debt, area-testing, area-scheduling
milestone: v0.21.0 — Documentation
-->

## Type

- [ ] Failing tests
- [x] Missing tests
- [ ] Code quality (warnings, analyzers)
- [ ] Performance optimization
- [x] Refactoring needed
- [x] Documentation gap
- [ ] Incorrect implementation
- [ ] Other

## Description

Found by the SPEC-003 audit of #6. Three small, non-blocking gaps surfaced while auditing today's code against the closed issue #6 ("Refactor Quartz logging tests to work with LoggerMessage delegates"):

1. `CLAUDE.md`'s Testing Standards section documents the Shouldly/Bogus/FsCheck/etc. assertion-library conventions, but never documents the `Microsoft.Extensions.Diagnostics.Testing.FakeLogger<T>` pattern that #6 introduced to test `[LoggerMessage]` source-generator output — even though the pattern is now used in 14+ test files across the codebase (Quartz, Hangfire, SignalR, Refit, Compliance.Retention, AzureFunctions, RedisPubSub, and more). A contributor writing a new test against `[LoggerMessage]` output has no written guidance and no example to copy, and could re-invent the (broken) NSubstitute approach #6 fixed.
2. `tests/Encina.UnitTests/Quartz/QuartzRequestJobTests.cs` locates the log entry it asserts on with `_logger.Collector.GetSnapshot().FirstOrDefault(r => r.Message.Contains("failed"))` (and similar substring lookups at lines 92-99, 137-141, 156-160, 176-180), while the sibling `QuartzNotificationJobTests.cs:157` pins the exact `EventId` (`r.Id.Id == 4059`). No functional risk today, but the two files are inconsistent in rigor and the substring approach is more fragile to future message-text edits.
3. Guard-clause tests for `QuartzRequestJob<,>`/`QuartzNotificationJob<>` (null constructor arguments, null `Execute` context) are duplicated almost verbatim between `tests/Encina.UnitTests/Quartz/Guards/QuartzRequestJobGuardsTests.cs` + `QuartzNotificationJobGuardsTests.cs` and `tests/Encina.GuardTests/Quartz/QuartzGuardTests.cs`. Per `.github/coverage-manifest/Encina.Quartz.json`, the `guard` flag is meant to be satisfied by `Encina.GuardTests`; the copies under `Encina.UnitTests/Quartz/Guards` only count toward the `unit` flag and add maintenance duplication with no comment explaining why both exist.

## Location

- **File(s)**:
  - `CLAUDE.md` (Testing Standards > Assertion and helper libraries)
  - `tests/Encina.UnitTests/Quartz/QuartzRequestJobTests.cs`
  - `tests/Encina.UnitTests/Quartz/Guards/QuartzRequestJobGuardsTests.cs`
  - `tests/Encina.UnitTests/Quartz/Guards/QuartzNotificationJobGuardsTests.cs`
  - `tests/Encina.GuardTests/Quartz/QuartzGuardTests.cs`
- **Package(s)**: Encina.Quartz (items 2–3); repository-wide convention (item 1)

## Current Behavior

1. No written rule for testing `[LoggerMessage]` output; the pattern exists only as tribal knowledge spread across 14+ files.
2. `QuartzRequestJobTests.cs` finds log entries by message substring; `QuartzNotificationJobTests.cs` finds one by exact `EventId`. Inconsistent rigor between two sibling test files for the same package.
3. Guard-clause tests for the two Quartz jobs exist in both `Encina.UnitTests/Quartz/Guards/` and `Encina.GuardTests/Quartz/`, exercising the same constructors and the same null-`context` path.

## Expected Behavior

1. `CLAUDE.md`'s Testing Standards documents the `FakeLogger<T>` pattern (asserting on `FakeLogger<T>.Collector.GetSnapshot()`, matching by `EventId` where practical) as the required approach for testing `[LoggerMessage]` delegate output, next to the existing Shouldly/Bogus convention, citing #6 as project history.
2. `QuartzRequestJobTests.cs`'s log-lookup assertions match `QuartzNotificationJobTests.cs`'s style (exact `EventId`, or a documented reason for the substring approach).
3. Either the `Encina.UnitTests/Quartz/Guards/*GuardsTests.cs` files are removed in favor of `Encina.GuardTests/Quartz/QuartzGuardTests.cs` (the flag the manifest expects them to satisfy), or a one-line comment states why both exist.

## Root Cause

1. #6 fixed the tests but the resulting pattern was never promoted from "working test code" to a written CLAUDE.md rule, unlike other testing conventions in the same section.
2. The `EventId`-pinning refinement (`QuartzNotificationJobTests.cs:157`) was added later (for #1173's PII-redaction tests) but not backported to the sibling request-job test file.
3. Both guard-test locations likely trace to different points in the test-consolidation history (`docs/plans/test-consolidation-plan.md`) and were never deduplicated.

## Proposed Fix

1. Add a short rule to `CLAUDE.md`'s "Assertion and helper libraries" subsection: use `FakeLogger<T>` (from `Microsoft.Extensions.Diagnostics.Testing`) to test `[LoggerMessage]` output; assert by `EventId` when more than one log call could match a message substring. Cite #6.
2. Update `QuartzRequestJobTests.cs`'s four substring-based lookups to match by `EventId` (4051, 4052, 4053, 4054 per `src/Encina.Quartz/Log.cs`), mirroring `QuartzNotificationJobTests.cs:157`.
3. Remove the duplicated guard tests from `tests/Encina.UnitTests/Quartz/Guards/` (keeping `Encina.GuardTests/Quartz/QuartzGuardTests.cs` as the flag-of-record), or add a one-line comment explaining the intentional duplication if the maintainer prefers to keep both.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [ ] **Medium** - Should be fixed before 1.0 release
- [x] **Low** - Nice to have, can be deferred

## Effort Estimate

- [x] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

- #6 — the closed issue this was found auditing (SPEC-003 per-issue deep audit)
- #1173 — added the `EventId`-pinning pattern to `QuartzNotificationJobTests.cs` that item 2 asks to backport
