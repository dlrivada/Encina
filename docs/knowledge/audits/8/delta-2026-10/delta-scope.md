# Delta scope of issue #8 (set rules-2026-10)

Reused from the original audit; do not re-derive it. Rules in this delta: see tools/ai/audit/pipeline-delta.json.

**Scope source:** the published record and the published audit result (docs/knowledge/audits/issue-8.md) only: the original audit has no published stage files.

## Knowledge record (docs/knowledge/issues/8.md)

```yaml
schema: 2
nav_exclude: true
issue: 8
title: "[DEBT] Fix Dapper.Sqlite PropertyTests - missing database tables"
closed: 2025-12-23
state_reason: completed
outcome: delivered
type: debt
area: data
packages: []
prs: []
linked_prs: []
duplicate_of:
superseded_by:
knowledge:
  - kind: gotcha
    statement: "SQLite's shared in-memory connection loses all data when closed, so a property test that closes then reopens the connection to verify transaction behavior across connection states cannot pass on SQLite; the test case was skipped rather than fixed."
    current: "no"
    sources:
      - "https://github.com/dlrivada/Encina/commit/062b705e1d163202a6dcf59d29a86f7ad13cec49, 2025-12-23, quote: \"Skip ConnectionState.Closed test (SQLite in-memory loses data on close)\""
    destinations:
      - kind: none
        status: done
        reason: "SQLite provider removed pre-1.0 (ADR-024, accepted March 2026); the fixture (SqliteFixture/Encina.TestInfrastructure) and the test project no longer exist. The same disposal/shared-connection hazard is already the rationale ADR-024 records for removing SQLite, so no new destination is needed."
  - kind: gotcha
    statement: "SQLite's text-based DateTime storage is format-incompatible with the ISO 8601 comparisons GetDueMessagesAsync relies on, so a reschedule-then-check-retry-fields property test could not be made to pass reliably; it was skipped with [Theory(Skip=...)] citing issue #7."
    current: "no"
    sources:
      - "https://github.com/dlrivada/Encina/commit/062b705e1d163202a6dcf59d29a86f7ad13cec49, 2025-12-23, quote: \"Skip Reschedule_AlwaysResetsRetryFields (datetime format incompatibility)\""
    destinations:
      - kind: none
        status: done
        reason: "SQLite provider removed pre-1.0 (ADR-024); ADR-024's 'SQLite-Specific Technical Challenges' table lists this exact DateTime format incompatibility as part of the removal rationale."
  - kind: rule
    statement: "SQLite in-memory property/integration test collections must disable xUnit test parallelization, because the shared in-memory database has single-writer semantics."
    current: "no"
    sources:
      - "https://github.com/dlrivada/Encina/commit/062b705e1d163202a6dcf59d29a86f7ad13cec49, 2025-12-23, quote: \"Add XunitConfiguration to disable test parallelization for SQLite\""
    destinations:
      - kind: none
        status: done
        reason: "Not current: the rule applied only to the now-removed SQLite provider (ADR-024 'Testing Infrastructure Impact': 'Parallelization disabled ... due to single-writer constraint')."
  - kind: decision
    statement: "RescheduleRecurringMessageAsync rejects a past nextScheduledAtUtc with an ArgumentException; the issue's fix corrected a property test that wrongly assumed a past reschedule would succeed and make the message appear as due."
    current: "yes"
    sources:
      - "https://github.com/dlrivada/Encina/commit/062b705e1d163202a6dcf59d29a86f7ad13cec49, 2025-12-23, quote: \"Note: Rescheduling to past is not allowed by the API (throws ArgumentException).\""
    destinations:
      - kind: none
        status: done
        reason: "Already enforced identically in the three surviving Dapper providers today (src/Encina.Dapper.SqlServer/Scheduling/ScheduledMessageStoreDapper.cs:153-154 and the PostgreSQL/MySQL siblings) and covered by their guard tests (tests/Encina.GuardTests/Dapper/*/ScheduledMessageStoreDapper*GuardTests.cs); no gap to fill."
remediation: []
review: verified
audit:
  checklist: 1
  date: 2026-09-25
  verdict: code-removed
  record: docs/knowledge/audits/issue-8.md
```

## Audit result (docs/knowledge/audits/issue-8.md)

# Audit result — issue #8

Checklist version: 1 (with the §15.3 amendments). Date: 2026-09-25. Verdict: **code-removed**.

## Scope

Files changed by the closing commit `062b705e1d163202a6dcf59d29a86f7ad13cec49` (2025-12-23, no pull request; `Fixes #8`):

| File at close | Status today | Evidence |
|---|---|---|
| `tests/Encina.Dapper.Sqlite.PropertyTests/Scheduling/ScheduledMessageStoreDapperPropertyTests.cs` | Deleted | `git log --diff-filter=D -- tests/Encina.Dapper.Sqlite.PropertyTests` → commit `22494a97` |
| `tests/Encina.Dapper.Sqlite.PropertyTests/TransactionPipelineBehaviorPropertyTests.cs` | Deleted | same commit |
| `tests/Encina.Dapper.Sqlite.PropertyTests/XunitConfiguration.cs` | Deleted | same commit |
| `tests/Encina.TestInfrastructure/Schemas/SqliteSchema.cs` | Deleted | `git log --diff-filter=D -- tests/Encina.TestInfrastructure/Schemas/SqliteSchema.cs` → commit `22494a97` |

Commit `22494a97` ("feat: remove SQLite provider — move to .backup, clean all references (ADR-024)") deleted the whole `Encina.Dapper.Sqlite.PropertyTests` project and every SQLite-specific file under `Encina.TestInfrastructure`, implementing [ADR-024](../../../../architecture/adr/024-remove-sqlite-provider-pre-1.0.md) (accepted March 2026, decision: "Remove SQLite provider support from the pre-1.0 release scope"). `.backup/sqlite/` is gitignored (commit `87b124b7`, "fix: restore .gitignore for .backup/ — must not be tracked in git") and is not present in this worktree.

Per SPEC-003 §5.1: "Files deleted since are recorded as `code-removed` and are not audited." Every file this issue touched falls in that category; the checklist items below are therefore all `n/a — code removed`, per §15.4's carve-out for ADR-009/ADR-024 removals.

## Checklist

| AUD | Outcome | Evidence |
|---|---|---|
| AUD-01 | n/a — code removed | Decision (defensive schema creation, skip two tests, fix the past-reschedule test data) was implemented as decided; no drift to record because the surrounding provider was removed by ADR-024, a later, recorded change of direction. |
| AUD-02 | n/a — code removed | The unit was a test project only; it created no entity, store, pipeline behavior or background service of its own. |
| AUD-03 | n/a — code removed | `Encina.Dapper.Sqlite.PropertyTests` and `Encina.Dapper.Sqlite` are absent from `.github/coverage-manifest/` today (searched: no `Encina.Dapper.Sqlite*.json` manifest file exists). |
| AUD-04 | n/a — code removed | No surviving test to inspect. |
| AUD-05 | n/a — code removed | No surviving test folder or justification file to inspect. |
| AUD-06 | n/a — issue type is `debt`, not `bug` | §15.3 amendment: AUD-06 applies only to `bug`/`delivered`/`partial` records with a closed bug. |
| AUD-07 | n/a — provider removed | SQLite was one of 10→9 Dapper-adjacent providers pre-ADR-024; the provider itself, not just a feature on it, was removed. |
| AUD-08 | n/a — code removed | The unit logged nothing (test project). |
| AUD-09 | n/a — code removed | No public API surface (test project, `PublicAPI.*.txt` not applicable to test projects). |
| AUD-10 | n/a — code removed | Same as AUD-09. |
| AUD-11 | n/a — code removed | `Encina.Dapper.Sqlite` had no shipped package/README; it was removed before 1.0. |
| AUD-12 | n/a | Not a security/compliance/audit/personal-data unit. |
| AUD-13 | n/a — code removed | No surviving error/log path to inspect. |
| AUD-14 | n/a — code removed | No surviving production code (test project only). |
| AUD-15 | n/a | No options classes in scope. |
| AUD-16 | n/a — code removed | No surviving database calls to inspect. |
| AUD-17 | n/a | No `AddEncina*` extension in scope (test project only). |
| AUD-18 | n/a — code removed | No surviving public surface. |

## Specialist passes

- `adversarial-reviewer`: **skipped**. There is no code left to review as "today's PR" — the entire unit (`Encina.Dapper.Sqlite.PropertyTests`, `SqliteSchema.cs`) was deleted by commit `22494a97` implementing ADR-024. Reviewing deleted files against today's standards would produce no verifiable finding.
- `docs-reviewer`: **skipped**. The feature had no README or docs page (it was internal test infrastructure for a provider removed before 1.0).
- Test review: **done by the coordinating worker** (this audit). No surviving test to measure coverage on; the manifest for `Encina.Dapper.Sqlite` no longer exists. The one durable fact the original property test got wrong (rescheduling to a past timestamp does not make a message due — the API rejects it) is verified as still correctly enforced and guard-tested on the three surviving Dapper providers: `src/Encina.Dapper.SqlServer/Scheduling/ScheduledMessageStoreDapper.cs:153-154`, `src/Encina.Dapper.PostgreSQL/Scheduling/ScheduledMessageStoreDapper.cs`, `src/Encina.Dapper.MySQL/Scheduling/ScheduledMessageStoreDapper.cs`, each throwing `ArgumentException` via `StoreValidationMessages.NextScheduledDateCannotBeInPast`, covered by `tests/Encina.GuardTests/Dapper/SqlServer/ScheduledMessageStoreDapperGuardTests.cs` and its PostgreSQL/MySQL siblings.

## Remediation

None. No finding was produced because there is no surviving code, test or doc to fail a checklist item against, and the one cross-provider invariant the original tests exercised is already correctly implemented and tested on every surviving provider.

## Deduplication search

Not applicable — no remediation issue drafted.


