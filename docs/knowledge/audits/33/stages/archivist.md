## Scope
- `src/Encina/Core/Encina.cs` (lines 73-82 `Publish`, 99-102 `PublishCore` calling `EncinaRequestGuards.TryValidateNotification`; line 87 `Send` calling `TryValidateRequest`).
- `src/Encina/Pipeline/EncinaRequestGuards.cs` (guards at lines 18, 37, 59, 91, 122); `src/Encina/Pipeline/EncinaNotificationGuards.cs` (handle-method guard, related, not named by the issue).
- Tests: the issue's commit `dbb984e5` added `tests/Encina.Tests/Guards/RequestGuardsTests.cs` and `NotificationGuardsTests.cs`; both were deleted by consolidation commit `65826302` (2026-01-16). Today `tests/Encina.GuardTests/Core/Pipeline/EncinaRequestGuardsTests.cs` (15 `[Fact]`/`[Theory]` test methods; 10 lines name `TryValidateNotification`, 50 lines contain `TryValidate`; counted by command) is the only file referencing `TryValidateNotification`.
- No PR; the only change is commit `dbb984e5` (tests only, `git show --stat` verified). Timeline: one `referenced` commit, one `closed` event.

## Destinations
- Decision "Publish and Send share the guard class": present in `Encina.cs` (verified by grep) and covered by the GuardTests file; no ADR or AGENTS.md entry (not needed).
- Cancellation-token guard proposed in the issue body: missing, no source delivered it (guards in `EncinaRequestGuards` only check null/handler types). Recorded as a `current: no` gotcha for the code auditor to judge.
- Pre-draft claim that tests are in `tests/Encina.UnitTests/Core/` was wrong: they were in `tests/Encina.Tests/Guards/` and are now merged into GuardTests.

## Successor and duplicate issues
None. The issue is not rejected, superseded or a duplicate.

## Lessons for the pipeline
- (issue-archivist) The pre-draft guessed the test location and listed `closed_at: 12/24/2025` and `linked_prs: []` in the wrong format; check the commit's `--stat` and `git log --follow` for the test path, then locate where the tests live after consolidation (here deleted and merged into another file).
- (issue-archivist) When a close comment says "already implemented", compare the issue body's proposals one by one with what shipped (here the cancellation-token guard was never mentioned again).
