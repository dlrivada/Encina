## Scope

Issue #24 shipped no code. It was closed 2025-12-24T11:52Z with the comment "Reverted - issue created in error". Its only timeline reference is commit 2b50a1ec (docs restructuring: `.claude/CLAUDE.md`, `ROADMAP.md`, `docs/history/2025-12.md`), verified with `git show --stat`; it lists #24 as pending work and changes no `src/` file. No linked PRs.

The real surface is the duplicate #35's work (commit 924ca2e5), mapped to today (all verified present):
- `src/Encina.Messaging/Health/` : `IEncinaHealthCheck.cs`, `EncinaHealthCheck.cs`, `HealthCheckResult.cs`, `OutboxHealthCheck.cs`, `InboxHealthCheck.cs`, `SagaHealthCheck.cs`, `SchedulingHealthCheck.cs` (also `DatabaseHealthCheck.cs`, `ProviderHealthCheckOptions.cs` from the commit, not re-checked).
- `src/Encina.AspNetCore/Health/` : `EncinaHealthCheckAdapter.cs`, `CompositeEncinaHealthCheck.cs`, `HealthCheckBuilderExtensions.cs`.
- Tests: the original `tests/Encina.Tests/Health/` no longer exists (the folder is gone); `tests/Encina.UnitTests/AspNetCore/Health/CompositeEncinaHealthCheckTests.cs` and `EncinaHealthCheckAdapterTests.cs` exist; I did not locate Messaging-side tests (OutboxHealthCheckTests etc.) by file name, so the test stage should search for them.
The code stage should audit this under #35, not #24.

## Destinations

The issue made no decision, so there is no destination to check. Its proposed solution (IEncinaHealthCheck, ASP.NET Core integration) is present today in the files above, and cross-cutting function 4 "Health checks / `IEncinaHealthCheck`" is in AGENTS.md section 6. The three checks it listed for caches, queues and databases were not part of #35's delivery and are tracked by open issues (#142, #263, #622, #754, #755), seen in `gh issue list`, not analysed further.

## Successor and duplicate issues

- #35, "[FEATURE] Health Check Abstractions - IHealthCheck integration" (identical title): verified CLOSED, state reason COMPLETED, closed 2025-12-25T10:31Z, created 2025-12-24T13:23Z (after #24 closed). Its comment lists the shipped classes and follow-ups #113, #114, #115 (all CLOSED per `gh issue list`). Record uses `outcome: duplicate`, `duplicate_of: 35`.
- The pre-draft's `rejected-unexplained` was corrected: the identical-title issue explains it.

## Lessons for the pipeline

- [issue-archivist] The pre-draft marked "created in error" as rejected-unexplained and "unrelated commit"; searching the issue list for the identical title (`gh issue list --search "<title words> in:title"`) found #35 immediately. The pre-draft's "Open questions" (was IEncinaHealthCheck implemented?) were answered by grepping `src/`.
- [issue-archivist] Compute every duration from the gh timestamps and write both timestamps in the record: the record said "eleven minutes" but created_at 2025-12-24T11:32:02Z and closed_at 11:52:27Z are 20 minutes 25 seconds apart (third audit in a row with a wrong duration).
- [issue-archivist] A pre-draft lists packages straight from an unticked template checklist (here `Encina.Caching.*`, `Encina.AspNetCore` etc. were all unchecked); take packages from the code that shipped, not from the template.
