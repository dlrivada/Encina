# Delta scope of issue #25 (set rules-2026-10)

Reused from the original audit; do not re-derive it. Rules in this delta: see tools/ai/audit/pipeline-delta.json.

**Scope source:** the published record and the published stage files of the original audit (docs/knowledge/audits/25/stages/).

## Knowledge record (docs/knowledge/issues/25.md)

```yaml
schema: 1
nav_exclude: true
issue: 25
title: "[FEATURE] Snapshotting for large aggregates"
closed: 2025-12-24
state_reason: completed
outcome: duplicate
duplicate_of: 52
type: feature
area: eventsourcing
review: verified
packages:
  - Encina.Marten
prs:
linked_prs:
knowledge:
  - kind: decision
    statement: "Issue #25 was closed as created in error and its snapshotting feature was delivered under #52, an issue with the identical title opened about two hours later; #25 itself delivered nothing."
    current: yes
    sources:
      - "quote: \"Reverted - issue created in error\" (comment by dlrivada on #25, 2025-12-24)"
      - "paraphrase: #52 has the same title, was created 2025-12-24 and closed 2025-12-26 by commit c1e4a50c (Fixes #52), which added src/Encina.Marten/Snapshots/ (issue #52, 2025-12-26)"
    destinations:
      - kind: docs
        status: done
        target: "CHANGELOG.md"
audit:
  checklist: 1
  date: 2026-10-03
  verdict: not-audited
  record: "docs/knowledge/audits/issue-25.md"
remediation:
```

## From the original archivist.md (docs/knowledge/audits/25/stages/archivist.md)

Issue #25 delivered no code itself (comment "Reverted - issue created in error", closed 2025-12-24T11:52:29Z, 20 min 18 s after creation at 11:32:11Z; no linked PR; its only timeline commit `2b50a1ec` changed `.claude/CLAUDE.md`, `ROADMAP.md`, `docs/history/2025-12.md`). Packages in the record come from the shipped code (via #52), not the issue's unticked checklist: `Encina.Marten`. `Encina.EventStoreDB` named in the issue does not exist under `src/`.

Code scope belongs to its duplicate #52 (commit `c1e4a50c`), all present today under `src/Encina.Marten/Snapshots/`: `ISnapshot.cs`, `ISnapshotable.cs`, `ISnapshotStore.cs`, `MartenSnapshotStore.cs`, `SnapshotAwareAggregateRepository.cs`, `SnapshotEnvelope.cs`, `SnapshotErrorCodes.cs`, `SnapshotLog.cs`, `SnapshotOptions.cs`, plus registration in `src/Encina.Marten/ServiceCollectionExtensions.cs` and `EncinaMartenOptions.cs`. Tests from that commit: `MartenSnapshotStoreTests.cs`, `MartenSnapshotStoreIntegrationTests.cs` (plus an ExtraIntegration file added later). The `[Snapshot(every: 100)]` attribute proposed in #25 does not exist (grep of `src/` for `SnapshotAttribute` finds nothing). No removed-on-purpose code involved.

## From the original code.md (docs/knowledge/audits/25/stages/code.md)

Issue #25 delivered no code. It has no linked PR, and its only timeline commit, 2b50a1ec ("docs: restructure documentation"), touches only .claude/CLAUDE.md, ROADMAP.md and docs/history/2025-12.md (git show --stat, 3 files). The issue's proposals are absent from the current tree:
- src\Encina.EventStoreDB does not exist (Test-Path returned False).
- A grep of src\ for SnapshotAttribute and [Snapshot( finds 0 matches.
The archivist's scope list names src\Encina.Marten\Snapshots\ (nine files, all present: ISnapshot, ISnapshotable, ISnapshotStore, MartenSnapshotStore, SnapshotAwareAggregateRepository, SnapshotEnvelope, SnapshotErrorCodes, SnapshotLog, SnapshotOptions). Those files come from commit c1e4a50c (Fixes #52), not from #25, so I did not review them as #25's pull request. Scope correction: the code scope for #25 is empty.


