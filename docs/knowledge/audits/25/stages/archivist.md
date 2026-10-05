## Scope

Issue #25 delivered no code itself (comment "Reverted - issue created in error", closed 2025-12-24T11:52:29Z, 20 min 18 s after creation at 11:32:11Z; no linked PR; its only timeline commit `2b50a1ec` changed `.claude/CLAUDE.md`, `ROADMAP.md`, `docs/history/2025-12.md`). Packages in the record come from the shipped code (via #52), not the issue's unticked checklist: `Encina.Marten`. `Encina.EventStoreDB` named in the issue does not exist under `src/`.

Code scope belongs to its duplicate #52 (commit `c1e4a50c`), all present today under `src/Encina.Marten/Snapshots/`: `ISnapshot.cs`, `ISnapshotable.cs`, `ISnapshotStore.cs`, `MartenSnapshotStore.cs`, `SnapshotAwareAggregateRepository.cs`, `SnapshotEnvelope.cs`, `SnapshotErrorCodes.cs`, `SnapshotLog.cs`, `SnapshotOptions.cs`, plus registration in `src/Encina.Marten/ServiceCollectionExtensions.cs` and `EncinaMartenOptions.cs`. Tests from that commit: `MartenSnapshotStoreTests.cs`, `MartenSnapshotStoreIntegrationTests.cs` (plus an ExtraIntegration file added later). The `[Snapshot(every: 100)]` attribute proposed in #25 does not exist (grep of `src/` for `SnapshotAttribute` finds nothing). No removed-on-purpose code involved.

## Destinations

- Decision "closed as created in error; delivered as #52": destination CHANGELOG.md, present ("Snapshotting for large aggregates (Issue #52)" in CHANGELOG.md). No ADR, rule or test applies: a bookkeeping closure.
- Pre-draft open question (partial implementation of snapshotting?): answered, yes, full implementation exists but from #52.

## Successor and duplicate issues

- #52 "[FEATURE] Snapshotting for large aggregates": CLOSED (completed 2025-12-26T20:20:35Z, verified with gh); created 2025-12-24T13:28:46Z, identical title. It is the duplicate target (`duplicate_of: 52`). Its implementation is present in the tree.
- Related follow-ons, OPEN (work pending, not implemented): #323 advanced snapshot strategies, #697 distributed cache for Marten snapshots, #940 Marten benchmarks (append, project, snapshot).
- #21 and #22 (named in the issue's Related Issues) are not checked, they are unrelated cross-references.

## Lessons for the pipeline

- The pre-draft concluded "rejected-unexplained" and asked whether a partial implementation existed; searching `gh issue list --search "<title> in:title"` found the identical-title #52 which fully delivered the feature, so the outcome is `duplicate`, not rejected. (Already lesson #21; confirmed again: always search identical titles before settling on rejected-unexplained.)
- The record validator requires `audit`, block-list (not `[]`) fields, and a non-`none` destination for `current: yes`; a duplicate record can use `docs` -> CHANGELOG.md as the destination.
