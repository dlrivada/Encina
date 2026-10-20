# Delta scope of issue #2 (set rules-2026-10)

Reused from the original audit; do not re-derive it. Rules in this delta: see tools/ai/audit/pipeline-delta.json.

**Scope source:** the published record and the published audit result (docs/knowledge/audits/issue-2.md) only: the original audit has no published stage files.

## Knowledge record (docs/knowledge/issues/2.md)

```yaml
schema: 2
nav_exclude: true
issue: 2
title: "[DEBT] ADO.Oracle and ADO.Sqlite SQL scripts use incorrect SQL Server syntax"
closed: 2025-12-22
state_reason: completed
outcome: delivered
type: debt
area: data
packages: [Encina.ADO.Oracle, Encina.ADO.Sqlite]
prs: []
linked_prs: []
knowledge:
  - kind: decision
    statement: "Each ADO provider's schema/DDL scripts must use that provider's native SQL dialect and types (Oracle PL/SQL with VARCHAR2/CLOB/NUMBER/TIMESTAMP, SQLite TEXT/INTEGER with IF NOT EXISTS), never a script copy-pasted from SQL Server."
    current: no
    sources:
      - "https://github.com/dlrivada/Encina/issues/2, closed 2025-12-22, paraphrase: issue body lists SQL Server-only syntax (sys.objects, GO, PRINT, NVARCHAR(MAX), dbo prefix) found in the Oracle and SQLite scripts and asks for provider-native equivalents"
      - "commit cf4d3287a6ac58287530a778e34d2b4ebdb575c9, 2025-12-23, quote: \"Replace SQL Server types with Oracle equivalents... Use PL/SQL anonymous blocks with EXECUTE IMMEDIATE for idempotent table creation\" / \"Replace SQL Server types with SQLite equivalents... Use CREATE TABLE IF NOT EXISTS for idempotent creation\""
    destinations:
      - kind: none
        target: "no destination: Encina.ADO.Oracle was deleted outright (ADR-009, commit 6121713c) and Encina.ADO.Sqlite was moved to .backup/ (ADR-024, commit 22494a97); the decision has no current subject to bind"
        status: done
  - kind: rule
    statement: "Provider-specific SQL differences (parameter markers, LIMIT/TOP, boolean representation, native types) must be checked per provider before shipping a script, not assumed from the reference implementation."
    current: yes
    sources:
      - "https://github.com/dlrivada/Encina/issues/2, closed 2025-12-22, paraphrase: root cause recorded as scripts 'likely copy-pasted from SqlServer implementation without proper adaptation'"
    destinations:
      - kind: rule
        target: "CLAUDE.md, Multi-Provider Implementation Rule, 'Provider-specific SQL differences to consider' table"
        status: done
remediation: []
review: verified
```

## Where the knowledge lives (record)

- The general rule (check each provider's native SQL dialect) already lives in `CLAUDE.md`'s Multi-Provider Implementation Rule, "Provider-specific SQL differences to consider" table.
- The specific decision (Oracle/SQLite script dialects) has no current destination: both providers were later removed from the 1.0 provider matrix (ADR-009 for Oracle, commit `6121713c`, 2026-01-26; ADR-024 for SQLite, commit `22494a97`, 2026-03-28), which supersedes the need for their scripts to be correct at all.

## Audit result (docs/knowledge/audits/issue-2.md)

# Audit — issue #2 (ADO.Oracle / ADO.Sqlite SQL scripts)

Checklist version: 1 (SPEC-003 §5.2). Date: 2026-09-24. Verdict: **code-removed**.

## Scope mapping

The commit that closed #2 (`cf4d3287a6ac58287530a778e34d2b4ebdb575c9`, 2025-12-23) touched:

- `src/Encina.ADO.Oracle/Scripts/000_CreateAllTables.sql`, `001_CreateOutboxMessagesTable.sql`, `002_CreateInboxMessagesTable.sql`, `003_CreateSagaStatesTable.sql`, `004_CreateScheduledMessagesTable.sql`
- `src/Encina.ADO.Sqlite/Scripts/000_CreateAllTables.sql`, `001_CreateOutboxMessagesTable.sql`, `002_CreateInboxMessagesTable.sql`, `003_CreateSagaStatesTable.sql`, `004_CreateScheduledMessagesTable.sql`

Following the files with `git log --follow --diff-filter=D`:

- `src/Encina.ADO.Oracle/**` was deleted entirely by commit `6121713c1763240478b152b705c50331c6e1d806` ("Remove Oracle provider support from pre-1.0 release scope", 2026-01-26), implementing [ADR-009](../../../../architecture/adr/009-remove-oracle-provider-pre-1.0.md). No backup copy exists in the working tree (`.backup/oracle/` is not present; CLAUDE.md's claim that Oracle code is preserved there does not match what `git log` shows for this path — the commit message says "Removed", not "moved to backup").
- `src/Encina.ADO.Sqlite/**` was moved to `.backup/sqlite/src/Encina.ADO.Sqlite` by commit `22494a97c89e4262e41800f0f43e8168529e5511` ("remove SQLite provider — move to .backup, clean all references (ADR-024)", 2026-03-28). `.backup/` is gitignored (commit `87b124b7`, "fix: restore .gitignore for .backup/") and the folder does not exist in this checkout (confirmed: `.backup` is absent from the working tree).

Both removals are on-purpose scope decisions recorded in ADRs. Per SPEC-003 §2.1 ("When the code was removed on purpose ... record that and audit nothing more for it"), no AUD item is run against source that no longer exists.

## Checklist

| AUD | Outcome | Evidence |
|---|---|---|
| AUD-01 | n/a — code-removed | The decision ("scripts must use each provider's native dialect") was correct when made; its subject (Oracle/SQLite scripts) was later removed from scope by ADR-009 and ADR-024, which is itself the recorded change of direction. No drift to flag. |
| AUD-02 – AUD-18 | n/a — code-removed | `Encina.ADO.Oracle` does not exist under `src/`; `Encina.ADO.Sqlite` exists only under the gitignored `.backup/sqlite/` (not present in this checkout, not built, not tested, not shipped). No checklist item has a subject to audit. |
| AUD-06 (regression test) | n/a | `outcome: delivered`, `type: debt` (not `bug`), so AUD-06 does not apply by its own "applies when" clause. |

## Coverage per flag

Not applicable: no source files exist in either package's location today, so there is nothing in `.github/coverage-manifest/` to check and no Cobertura data to collect.

## Findings

None. No remediation issue opened.

## Notes for a future audit pass

- CLAUDE.md's "Multi-Provider Implementation Rule" note ("Oracle code is preserved in `.backup/oracle/` for potential future restoration") does not match the git history for the files this issue touched: the Oracle removal commit (`6121713c`) deletes the scripts outright with no corresponding `.backup/oracle/` add in the same commit. This is a documentation-accuracy question about CLAUDE.md itself, out of scope for a per-issue audit of #2, but worth a follow-up check if a future audit unit covers ADR-009 or the removal commit directly. No remediation issue opened for this observation because it doesn't trace to code #2 touched and duplicating unit-level findings from a knowledge-migration pass is out of scope (SPEC-003 §2.2, units are audited once per checklist version, not from per-issue passes).


