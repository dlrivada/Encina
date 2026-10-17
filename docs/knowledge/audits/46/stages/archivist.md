## Scope

Closing evidence: timeline `closed` event 2025-12-26T13:49:59Z (manual, no PR); commit 9a0ebf9cb (2025-12-26 14:51 +0100) is only a later `referenced` event. No linked PR.

Code (today):
- `src/Encina.Testing/EventSourcing/AggregateTestBase.cs` (added by 9a0ebf9cb per `--diff-filter=A`; later commits 0954ff15f, e0576aa0c, 19c9051a8 = Sonar fixes / IAggregate move). Types: `AggregateTestBase<TAggregate, TId>`, `AggregateTestBase<TAggregate>` (Guid). Members verified present: Given, GivenEmpty, When, WhenAsync, Then<T> (2 overloads), ThenEvents, ThenNoEvents, ThenState, ThenThrows<T> (2 overloads), GetUncommittedEvents (2 overloads), virtual CreateAggregate. `ThenError` does not exist.
- Package csproj `src/Encina.Testing/Encina.Testing.csproj` (touched by the commit).

Tests: original `tests/Encina.Testing.Tests/EventSourcing/*` were deleted by 658263027 (2026-01-16, test consolidation). Today:
- `tests/Encina.UnitTests/Testing/EventSourcing/AggregateTestBase{,Contract,GuardClause,Property}Tests.cs` (30/15/18/12 test attributes).
- A second, identical-count copy under `tests/Encina.UnitTests/Testing/Base/EventSourcing/` (same class names, namespace `...Testing.Base.EventSourcing`): apparent duplication, for test-auditor.
- `tests/Encina.GuardTests/Testing/EventSourcing/AggregateTestBaseGuardTests.cs` (14).

Docs: `CHANGELOG.md` (Issue #46 entry, ~line 9466), `docs/INVENTORY.md:3161`, `docs/engineering/PROJECT-HISTORY.md:569`, `docs/plans/testing-dogfooding-plan.md`, `docs/releases/pre-v0.10.0/README.md`. `docs/history/2025-12.md` (added by the commit) was deleted by d098e5425. `src/Encina.Testing` has no README (open #1689).

## Destinations

- API decision (base class + member set): present in code, CHANGELOG.md, docs/INVENTORY.md; regression tests present.
- ThenError -> ThenThrows rename and WhenAsync: present in code; the issue body still shows ThenError, the close comment lists ThenThrows. Present in tests.
- 85%+ coverage criterion: no measured evidence in the issue; missing (auditor to measure against the coverage manifest).
- "Add documentation" task: only CHANGELOG/INVENTORY/release notes; no package README (#1689 OPEN) and no guide page found under docs/ outside those files.
- Pre-draft items dropped: "Rules and lessons" (guard tests required for test infrastructure, sync+async support) and candidate `reviewer-checklist` had no quotable source; "unit/property/contract tests required" is the issue's own Test Matrix, not a decision.

## Successor and duplicate issues

None. Related, all checked: #1689 OPEN ("Encina.Testing has no package README"); #44 CLOSED (Encina.Testing fixtures; superseding #23); #169 and #434 CLOSED (later Given/When/Then helpers). #1923 and #2373 are OPEN Encina.Testing items, unrelated to this class.

## Lessons for the pipeline

- (issue-archivist) The pre-draft had `closed_at: 12/26/2025`, `linked_prs: []`, listed an unticked "Affected Packages" entry as is, and invented a `reviewer-checklist` destination plus a "Rules and lessons" section; all normalised or dropped. (normalise pre-draft dates and drop destinations/rules with no quotable source)
- (issue-archivist) The close was manual at 13:49:59Z; the commit appears as a later `referenced` event, so the duration is computed from createdAt/closedAt (2 d 0 h 23 min), not from the commit. (never derive closing time from the commit)
- (issue-archivist) Consolidated tests can exist twice (here `Testing/EventSourcing` and `Testing/Base/EventSourcing`); list both paths in scope when grepping tests by class name. (when scoping tests, glob by class name across tests/ and report duplicates)
