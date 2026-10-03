## Scope reviewed
Read `artifacts\knowledge\stages\archivist.md` and `artifacts\knowledge\issues\21.md`. Issue #21 was closed 20 minutes after creation ("Reverted - issue created in error") with no PR and no code change; the only commit referencing it (2b50a1ec) touched `.claude/CLAUDE.md`, `ROADMAP.md` and `docs/history/2025-12.md`. Reviewed as a PR submitted today, #21's diff is empty, so there is no code to review. No scope correction. I confirmed the archivist's claims in the worktree:
- `src/Encina.Marten/Projections/` holds the 15 files the archivist listed, delivered under #36.
- `IProjectionStore` appears in no `.cs` file under `src/`.
- No `*EventStore*` package directory exists under `src/`.
## Findings
- none
## Siblings audited
- The feature #21 asked for is `src/Encina.Marten/Projections/` (`IProjection`, `IReadModel`, `IReadModelRepository`, `IProjectionManager`, `MartenProjectionManager`, `MartenReadModelRepository`, `InlineProjectionDispatcher`, `InlineProjectionRelay` and the rest). It was delivered by #36, not #21, so its code audit belongs to #36's audit. It is not reviewed here, which keeps #21's framing and avoids duplicating that stage.
- The proposed `IProjectionStore` does not exist. The delivered design uses `IReadModelRepository<T>`. This is a design difference, not a defect.
- The "Other: Encina.EventStoreDB" package #21 mentioned is not in the repo. AGENTS.md §5 lists EventStoreDB as "future", so there is no coherence gap to flag.
- The eight issues opened by commit 2b50a1ec were not examined. Each is audited under its own number.
## Lessons for the pipeline
- When the archivist reports that an issue was closed in error and delivered no code, the code stage can end at confirming the empty diff and the existence or absence of the proposed types, with "- none" findings. Walking out to the successor's code would duplicate that successor's audit.
