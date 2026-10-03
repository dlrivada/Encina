<!-- issue
title: [DEBT] Encina.Compliance.Consent: coverage manifest gap and dead GetConsentHistoryAsync stub
labels: technical-debt, area-compliance, ai:local-candidate
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

Two related debt items in `Encina.Compliance.Consent`:

1. **Coverage manifest gap**: `.github/coverage-manifest/Encina.Compliance.Consent.json` declares `totalFiles: 24`, but `src/Encina.Compliance.Consent` contains 25 `.cs` files. `SubjectIdConversion.cs` (an internal class, 191 lines) has no manifest entry, so it is excluded from every coverage flag's obligations and never gates a build.
2. **Dead stub**: `GetConsentHistoryAsync` in `Services/DefaultConsentService.cs` (lines 414-425) always returns `ConsentErrors.EventHistoryUnavailable`. This is a stub left over from the #777 Marten migration; its own code comment says "will be implemented when Marten-specific integration is configured (Phase 4+)". It has shipped in the public `IConsentService` contract with no tracking issue recording the deferred work.

## Location

- **File(s)**:
  - `.github/coverage-manifest/Encina.Compliance.Consent.json`
  - `src/Encina.Compliance.Consent/SubjectIdConversion.cs`
  - `src/Encina.Compliance.Consent/Services/DefaultConsentService.cs:414-425`
- **Package(s)**: Encina.Compliance.Consent

## Current Behavior

- `SubjectIdConversion.cs` has no coverage-manifest entry, so it is not subject to any per-flag coverage obligation.
- `GetConsentHistoryAsync` always returns a failure (`ConsentErrors.EventHistoryUnavailable`) through the normal `Either.Left` success-shaped return, with no exception and no issue tracking when it will be implemented.

## Expected Behavior

- `SubjectIdConversion.cs` has a manifest entry with the test-type flags appropriate to an internal, reflection-based static converter (at least `unit`; `guard` and `property` where the invariants documented in its XML comments justify them).
- `GetConsentHistoryAsync` either is implemented against Marten's event stream, or the method (and its README/XML-doc claim) is changed to make the permanence of the gap explicit, with this issue as the tracking reference.

## Root Cause

- The coverage manifest was not updated when `SubjectIdConversion.cs` was added for #1149.
- `GetConsentHistoryAsync` was never finished after the #777 Marten migration, and no follow-up issue was opened at the time.

## Proposed Fix

1. Add a manifest entry for `SubjectIdConversion.cs` with the applicable test-type flags (mechanical — a `mechanical-fixer` change).
2. Decide whether to implement `GetConsentHistoryAsync` against Marten's `IEventStore`/stream APIs now, or keep it deferred with a clear, documented boundary (README + XML doc) referencing this issue instead of an undated "Phase 4+" comment.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [x] **Medium** - Should be fixed before 1.0 release
- [ ] **Low** - Nice to have, can be deferred

## Effort Estimate

- [x] Small (< 1 hour) — item 1, the manifest entry
- [x] Medium (1-4 hours) — item 2, the `GetConsentHistoryAsync` decision and its documentation

## Related Issues

- #1149 — added `SubjectIdConversion.cs` without updating the coverage manifest
- #777 — Marten migration that left `GetConsentHistoryAsync` unfinished
- Found during the SPEC-003 deep quality audit of `Encina.Compliance.Consent` (2026-09-24), checklist items AUD-01 and AUD-03; findings F4 and F6 in `artifacts/audit/findings.csv` (audit worktree)
