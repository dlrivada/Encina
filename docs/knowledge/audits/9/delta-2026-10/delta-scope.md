# Delta scope of issue #9 (set rules-2026-10)

Reused from the original audit; do not re-derive it. Rules in this delta: see tools/ai/audit/pipeline-delta.json.

**Scope source:** the published record and the published audit result (docs/knowledge/audits/issue-9.md) only: the original audit has no published stage files.

## Knowledge record (docs/knowledge/issues/9.md)

```yaml
schema: 2
nav_exclude: true
issue: 9
title: "[DEBT] Fix Dapper.Sqlite ContractTests - IScheduledMessage interface issues"
closed: 2025-12-23
state_reason: completed
outcome: delivered
type: debt
area: testing-quality
packages: [Encina.Dapper.Sqlite]
prs: []
linked_prs: []
knowledge:
  - kind: decision
    statement: "IScheduledMessage_Contract_AllPropertiesAccessible must build its ScheduledMessage with a past ScheduledAtUtc/CreatedAtUtc and RetryCount below maxRetries, so the message actually surfaces from GetDueMessagesAsync instead of relying on a follow-up RescheduleRecurringMessageAsync call."
    current: unknown
    sources:
      - "https://github.com/dlrivada/Encina/issues/9#issuecomment-3684610518 (2025-12-23), quote: \"IScheduledMessage_Contract_AllPropertiesAccessible now creates message in past.\""
      - "commit 3f20fec8f4ebbe98317be9ec02e8b9755ed19dc2 (2025-12-23), paraphrase: diff shows scheduledAt/createdAt moved to the past and RetryCount set to 0."
    destinations:
      - kind: none
        status: "n/a — the file this decision lived in (tests/Encina.Dapper.Sqlite.ContractTests/Scheduling/ScheduledMessageStoreDapperContractTests.cs) was deleted when ADR-024 removed the SQLite provider; the decision has no current target to check against."
  - kind: gotcha
    statement: "SQLite's ISO-8601 text datetime storage is format-sensitive enough that RescheduleRecurringMessageAsync_Contract_ResetsRetryFields could not be made reliable, so it was permanently skipped with only a one-line justification in the closing GitHub comment, not a .md justification file (that convention did not exist in Encina in December 2025)."
    current: yes
    sources:
      - "https://github.com/dlrivada/Encina/issues/9#issuecomment-3684610518 (2025-12-23), quote: \"RescheduleRecurringMessageAsync_Contract_ResetsRetryFields skipped due to SQLite datetime format incompatibility.\""
      - "commit 3f20fec8f4ebbe98317be9ec02e8b9755ed19dc2 (2025-12-23), quote: \"[Fact(Skip = \\\"SQLite datetime format incompatibility with ISO8601 makes this test unreliable - see GitHub issue #7\\\")]\""
    destinations:
      - kind: adr
        target: "docs/architecture/adr/024-remove-sqlite-provider-pre-1.0.md"
        status: present
  - kind: pending-work
    statement: "Issue #9 was a sub-item of #7 ('[DEBT] Fix 57 failing tests across multiple packages'); its fix landed directly on main via commit 3f20fec (message says 'Fixes #7'), with #9 itself closed manually by comment, not by an auto-closing PR — so this record has no linked pull request."
    current: yes
    sources:
      - "https://github.com/dlrivada/Encina/issues/9 (REST), paraphrase: 'closed' event at 2025-12-23T00:13:15Z has no commit_id, immediately after the closing comment referencing commit 3f20fec."
    destinations:
      - kind: none
        status: "n/a — a provenance fact about how the issue was closed, not a rule or decision to relocate."
audit:
  checklist: 1
  date: 2026-09-25
  verdict: code-removed
  record: "docs/knowledge/audits/issue-9.md"
remediation: []
review: verified
```

## Audit result (docs/knowledge/audits/issue-9.md)

---
issue: 9
checklist: 1
date: 2026-09-25
verdict: code-removed
remediation: []
---

# Audit — Issue #9

## Scope

Issue #9 touched a single file: `tests/Encina.Dapper.Sqlite.ContractTests/Scheduling/ScheduledMessageStoreDapperContractTests.cs`, via commit `3f20fec8f4ebbe98317be9ec02e8b9755ed19dc2` (2025-12-23, "Fixes #7"). No pull request; the commit landed directly on `main` and the maintainer closed #9 manually by comment.

Following the file with `git log --follow` confirms it was deleted, along with the rest of `src/Encina.Dapper.Sqlite` and `tests/Encina.Dapper.Sqlite.*`, by commit `22494a97` ("feat: remove SQLite provider — move to .backup, clean all references (ADR-024)"). `Encina.slnx` has zero references to `Sqlite` today, and `.backup/` (git-ignored, per `.gitignore:16`) is not present in this checkout. Per SPEC-003 §5.1, "Files deleted since are recorded as code-removed and are not audited"; per §5.3 the verdict `code-removed` applies to the whole issue.

## Checklist (version 1)

| ID | Outcome | Evidence / reason |
|---|---|---|
| AUD-01 | pass | The code no longer does what the issue decided, but the drift is recorded: [ADR-024](../../../../architecture/adr/024-remove-sqlite-provider-pre-1.0.md) removed the entire SQLite provider in March 2026, and its "SQLite-Specific Technical Challenges" table names the exact failure mode issue #9 hit ("`datetime('now')` Incompatible format with ISO 8601"). No unrecorded drift. |
| AUD-02 | n/a | Code removed by ADR-024; the unit no longer exists in `src/`/`tests/` to check for cross-cutting integrations. |
| AUD-03 | n/a | `.github/coverage-manifest/Encina.Dapper.Sqlite.json` does not exist (checked: no file of that name under `.github/coverage-manifest/`); the package was deleted, so there is no manifest or coverage to measure. |
| AUD-04 | n/a | Code removed; no test to read. |
| AUD-05 | n/a | Code removed; no test folder to list. |
| AUD-06 | n/a | Issue #9's `type` is `debt`, not `bug`; AUD-06 applies only to `type: bug` records (SPEC-003 §15.3 amendment). |
| AUD-07 | n/a | SQLite was removed from the provider matrix by ADR-024; it is no longer one of the 10 required database providers, so provider-parity is moot for this issue. |
| AUD-08 | n/a | Code removed; no `[LoggerMessage]`/`LoggerMessage.Define` calls to check (the file was a test class with no logging). |
| AUD-09 | n/a | Code removed; the file was a test class with no public API surface tracked in `PublicAPI.*.txt`. |
| AUD-10 | n/a | Code removed; test classes carry no XML-doc obligation. |
| AUD-11 | n/a | `Encina.Dapper.Sqlite` has no README under `src/Encina.Dapper.Sqlite` today (the directory holds only `bin/`, `obj/`, and a stray `.csproj.user`); the package and its docs were removed by ADR-024. |
| AUD-12 | n/a | Not a security/compliance/audit/personal-data unit. |
| AUD-13 | n/a | Code removed; no error factories or log messages to read. |
| AUD-14 | n/a | Code removed; and even before removal this was test code, not production code, so AUD-14 ("production code... never reads `DateTime.UtcNow`") would not have applied to the test builder's `DateTime.UtcNow.AddHours(-1)` calls. |
| AUD-15 | n/a | The unit had no options classes. |
| AUD-16 | n/a | Code removed; no database calls to read. |
| AUD-17 | n/a | The unit had no `AddEncina*` extension (it was a test project). |
| AUD-18 | n/a | Code removed; no public surface to check for `[Obsolete]` members. |

Verified with the local model's first-draft table (`artifacts/local-ai/out/audit9-table.md`, ledger line 2026-09-25T08:52:30Z, task `audit9-table`, 356 prompt + 591 completion tokens) as a starting point, then corrected and completed by hand: added the specific manifest/README/provider-matrix/logging/options/AddEncina* evidence AUD-03/AUD-07/AUD-08/AUD-09/AUD-11/AUD-14/AUD-15/AUD-17 need (the local draft only repeated the generic "code removed" reason for all of them, which understates AUD-14's nuance about test vs. production code and AUD-06's own "type: bug only" exemption).

## Specialist passes

- **`adversarial-reviewer`**: skipped. There is no code or pull request in scope to review — `src/Encina.Dapper.Sqlite` and the ContractTests project no longer exist, and the fix commit predates this audit by nine months with no open PR.
- **`docs-reviewer`**: skipped. The package had no README or docs page; ADR-024 (which documents the removal and supersedes any Dapper.Sqlite-specific docs) is itself out of scope for a per-file docs review — it is not "docs describing what issue #9 delivered."
- **Test review**: no test project exists to measure coverage against; not applicable for the same reason as AUD-03/AUD-04/AUD-05.

## Deduplication check

`gh issue list --repo dlrivada/Encina --state open --search "Dapper.Sqlite datetime"` and `--search "SQLite scheduled message"` were run; no open issue tracks a live defect in this now-deleted code. No remediation issue is warranted: the one finding pilot 1 raised (missing `.md` justification for the skipped test) is moot because the file and the package it lived in are gone, and the underlying lesson (SQLite datetime incompatibility) is already recorded in ADR-024.

## Remediation

None. Verdict `code-removed`.


