## Scope
Issue #26 shipped no code: no linked PR; the only timeline commit, 2b50a1ec (2025-12-24, "docs: restructure documentation"), touched `.claude/CLAUDE.md`, `ROADMAP.md` and `docs/history/2025-12.md` and only lists #26 among new issues (verified with `git show --stat`). Outcome: `duplicate` of #42. The code delivered for the same feature lives in `src/Encina.Messaging/DeadLetter/` (commit d5c60cab, Fixes #42: `IDeadLetterStore`, `IDeadLetterManager`, `IDeadLetterMessage`, `IDeadLetterMessageFactory`, `DeadLetterManager`, `DeadLetterOrchestrator`, `DeadLetterOptions`, `DeadLetterCleanupProcessor`, `DeadLetterServiceCollectionExtensions`, `Health/DeadLetterHealthCheck.cs`) with tests under `tests/Encina.UnitTests/Messaging/DeadLetter/` and `tests/Encina.GuardTests/Messaging/DeadLetter/`. That code belongs to the audit of #42, not #26. `IDeadLetterHandler` (the name in #26) does not exist in `src/`: scope difference is naming only (#42's API is manager/store with an `OnDeadLetter` callback).

## Destinations
- Decision "#26 withdrawn, feature delivered under #42": destination is the delivered tests, `tests/Encina.UnitTests/Messaging/DeadLetter/DeadLetterManagerTests.cs` (present, listed by Get-ChildItem). No ADR/AGENTS.md destination: no design decision was taken in #26.

## Successor and duplicate issues
- #42 "[FEATURE] Dead Letter Queue - enhanced DLQ handling": created 2025-12-24T13:25:23Z, state CLOSED, stateReason COMPLETED (closed 2025-12-26 via d5c60cab). Verified today with gh. Same feature, so #26 is a duplicate of #42; its scope was compared body to body (both: unified DLQ abstraction, monitoring, `Encina.Messaging` core, all transports).
- #149 "Dead Letter Queue for Failed Scheduled Messages": OPEN. Related, NOT implemented.
- #1609 "transports are publish-only ... no dead-letter support": OPEN, pending.
- #631 (CDC DLQ): CLOSED COMPLETED 2026-02-15; unrelated to the generic scope.

## Lessons for the pipeline
- An issue closed "created in error" is a duplicate of its same-scope re-creation (#42 here, #52 for audit #25): compare bodies, then record `outcome: duplicate` with `duplicate_of`, rather than `rejected-unexplained`. I first chose rejected-unexplained; state_reason COMPLETED and a different title ("enhancements" vs "enhanced DLQ handling") hid the match.
