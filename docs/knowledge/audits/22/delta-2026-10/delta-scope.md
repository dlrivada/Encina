# Delta scope of issue #22 (set rules-2026-10)

Reused from the original audit; do not re-derive it. Rules in this delta: see tools/ai/audit/pipeline-delta.json.

**Scope source:** the published record and the published stage files of the original audit (docs/knowledge/audits/22/stages/).

## Knowledge record (docs/knowledge/issues/22.md)

```yaml
schema: 1
nav_exclude: true
issue: 22
title: "[FEATURE] Event Versioning - upcasting and schema evolution"
closed: 2025-12-24
state_reason: completed
outcome: duplicate
duplicate_of: 37
type: feature
area: eventsourcing
review: verified
packages:
  - Encina.Marten
prs:
linked_prs:
knowledge:
  - kind: direction-change
    statement: "Issue #22 was closed without work as 'created in error'; the event versioning feature was tracked and delivered under the identically titled issue #37 (closed 2025-12-26 by commit 957093c2, Encina.Marten/Versioning)."
    current: no
    sources:
      - "quote: \"Reverted - issue created in error\" (comment by dlrivada on #22, 2025-12-24)"
      - "paraphrase: #37 has the same title, was created 2025-12-24 13:23Z (about two hours after #22) and was closed by commit 957093c2 'feat(marten): add Event Versioning/Upcasters for schema evolution (Fixes #37)'"
    destinations:
      - kind: none
        status: done
        target: "src/Encina.Marten/Versioning/"
audit:
  checklist: 1
  date: 2026-10-03
  verdict: not-audited
  record: "docs/knowledge/audits/issue-22.md"
remediation:
```

## From the original archivist.md (docs/knowledge/audits/22/stages/archivist.md)

- Issue #22 was closed on 2025-12-24 (about 21 minutes after creation: created 2025-12-24T11:31:36Z, closed 11:52:23Z) by its author with the single comment "Reverted - issue created in error". It has no linked PR and no closing commit. Its only timeline commit reference is 2b50a1ec (documentation restructure), which created it together with #21 and #23-#28. That commit touched only `.claude/CLAUDE.md`, `ROADMAP.md` and `docs/history/2025-12.md`.
- The issue itself changed no code, so there is nothing to scope for it directly.
- The feature it described (event upcasting and schema versioning for Marten) exists today in `src/Encina.Marten/Versioning/` (IEventUpcaster, EventUpcasterBase, LambdaEventUpcaster, EventUpcasterRegistry, EventVersioningOptions, ConfigureMartenEventVersioning, EventVersioningErrorCodes, VersioningLog). It was delivered under #37 by commit 957093c2 (`git log --follow` on IEventUpcaster.cs shows that commit only). Tests exist in tests/Encina.UnitTests, GuardTests and IntegrationTests under `Marten/Versioning`. If the auditor wants the code reviewed, it belongs to #37's audit, not this one.
- `Encina.EventStoreDB` (named in the issue as "Other") does not exist in `src/`; it was never created (EventStoreDB is listed as "future" in AGENTS.md).

## From the original code.md (docs/knowledge/audits/22/stages/code.md)

Issue #22 was closed in error ("Reverted - issue created in error") with no PR and no closing commit. The only commit referencing it, 2b50a1ec, touches `.claude/CLAUDE.md`, `ROADMAP.md` and `docs/history/2025-12.md` (verified with `git show --stat`), so the issue's diff contains no code. No code scope exists for #22. The feature it described lives in `src/Encina.Marten/Versioning/` (8 files, confirmed present) and was delivered under #37 (commit 957093c2); its code review belongs to #37's audit. `src/Encina.EventStoreDB` does not exist (confirmed with `Test-Path`), consistent with AGENTS.md listing EventStoreDB as future.


