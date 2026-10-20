## Coverage measured
Issue #26 has no diff in `src/` or `tests/`, so there is nothing of #26 to measure: not measured: no src/ scope for #26.
- `git show --stat 2b50a1ec` (the only commit that mentions #26): `.claude/CLAUDE.md`, `ROADMAP.md`, `docs/history/2025-12.md`; no `src/` or `tests/` file.
- `d5c60cab` (the #42 delivery) is an ancestor of `audit/26` HEAD (`git merge-base --is-ancestor d5c60cab HEAD` exit 0), so the successor's code and tests are present in this worktree.

Narrow check of the successor's tests, only to confirm the destination named in the knowledge record exists, passes and is not vacuous. Command shape: `dotnet test tests\Encina.<Flag>Tests\Encina.<Flag>Tests.csproj -c Release --filter "FullyQualifiedName~DeadLetter" --collect "XPlat Code Coverage" --results-directory artifacts\audit\coverage\<flag>`. Build configuration Release (as CI). Docker 29.8.1 was up.

| Flag | Result of the `~DeadLetter` filter | Lines of `src\Encina.Messaging\DeadLetter\*` and `Health\DeadLetterHealthCheck.cs` executed |
| --- | --- | --- |
| unit | 176 passed, 0 failed | `DeadLetterCleanupProcessor.cs` 41/41, `DeadLetterFilter.cs` 22/22, `DeadLetterManager.cs` 124/140, `DeadLetterOptions.cs` 9/9, `DeadLetterOrchestrator.cs` 194/207, `DeadLetterServiceCollectionExtensions.cs` 13/17, `IDeadLetterManager.cs` 29/29, `IDeadLetterMessageFactory.cs` 14/14, `DeadLetterHealthCheck.cs` 54/54 |
| guard | 35 passed, 0 failed | `DeadLetterCleanupProcessor.cs` 8/41, `DeadLetterFilter.cs` 0/22, `DeadLetterManager.cs` 19/140, `DeadLetterOptions.cs` 8/9, `DeadLetterOrchestrator.cs` 27/207, `DeadLetterServiceCollectionExtensions.cs` 0/17, `DeadLetterHealthCheck.cs` 2/54 |
| contract | 11 passed, 0 failed; the matching tests are the CDC and scheduling files (`Cdc\ICdcDeadLetterStoreContractTests.cs`, `Messaging\Scheduling\ExponentialBackoffRetryPolicyContractTests.cs`), not the Messaging dead-letter feature | 0 lines of every file above |
| property | 5 passed, 0 failed; the match is `Cdc\InMemoryCdcDeadLetterStorePropertyTests.cs` | 0 lines of every file above |
| integration | no test matches the filter (Docker up, the run executed) | 0 lines of every file above |

The manifest `.github/coverage-manifest/Encina.Messaging.json` has package-wide targets (unit 70, guard 20, contract 15, property 15) and per-file `defaultTests`: the dead-letter files list unit (and guard for most); no dead-letter file lists contract, property or integration. A per-file figure from a filtered run cannot be compared with the package aggregate, so no pass/fail is stated. Gaps in the successor's coverage belong to the audit of #42.

## Findings
- none

## Informational (not findings)
- The destination in `artifacts\knowledge\issues\26.md`, `tests/Encina.UnitTests/Messaging/DeadLetter/DeadLetterManagerTests.cs`, exists and its class runs green under the unit filter (part of the 176 passed). Other dead-letter test files located with a `Select-String -List` over `tests\`: `Encina.UnitTests\Messaging\DeadLetter\DeadLetterFilterTests.cs`, `DeadLetterOrchestratorTests.cs`, `ReplayResultTests.cs`, `Messaging\Processors\DeadLetterCleanupProcessorTests.cs`, `Encina.GuardTests\Messaging\DeadLetter\DeadLetterGuardTests.cs`.
- Guard coverage of the successor is low for the files that list guard in the manifest (for example `DeadLetterFilter.cs` 0/22, `DeadLetterServiceCollectionExtensions.cs` 0/17 and `DeadLetterManager.cs` 19/140 in the guard flag). These are not findings of #26; they go to the audit of #42. The filtered run measures only tests whose names contain `DeadLetter`, so tests that exercise these files under another name are not counted.
- `IDeadLetterHandler`, the type #26 proposed, does not exist in `src/`, so no test of it is expected.
- Open work already tracked elsewhere: #149 (dead letter for failed scheduled messages) and #1609 (transports publish-only, no dead-letter support).

## CRAP
Pending #1346. Not computed here; #26 changed no method.

## Lessons for the pipeline
- A `FullyQualifiedName~DeadLetter` filter matches the CDC dead-letter contract and property tests too (`Cdc\ICdcDeadLetterStoreContractTests.cs`, `Cdc\InMemoryCdcDeadLetterStorePropertyTests.cs`): a green contract or property count from a name filter does not mean the feature has contract or property tests. State which test files the passing tests belong to, and read the per-file lines executed.
