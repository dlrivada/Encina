Verdict: PASS
## Verified claims
Branch check first: `audit/21`, HEAD `90e09428`; `git merge-base --is-ancestor` returned 0 for both `584a712b` (#36) and `2b50a1ec`, so every search and run below was done inside the wia-21 checkout.

Archivist and knowledge record
- `gh issue view 21 --comments`: created 2025-12-24T11:31:26Z, closed 11:52:21Z (21 minutes), stateReason COMPLETED, single OWNER comment "Reverted - issue created in error". Matches the record.
- `gh issue view 36` (re-run): CLOSED, completed, created 2025-12-24T13:23:34Z (about two hours after #21), closed 2025-12-26T19:47:21Z, same title as #21. #22 and #25 re-run: both CLOSED. The commit that closed #36, `584a712b` "(Fixes #36)", added `src/Encina.Marten/Projections/*`.
- `git show --stat 2b50a1ec`: touches only `.claude/CLAUDE.md`, `ROADMAP.md`, `docs/history/2025-12.md`. Its history entry lists #21 to #28. A time-window `gh issue list` returns exactly eight issues, #21 to #28, created 11:31 to 11:32Z. The "one of eight issues" claim holds. The wording "opened by commit 2b50a1ec" is loose: the commit documents them rather than creating them. That is not a correction.
- `src/Encina.Marten/Projections/` holds 15 files, exactly the archivist's list (`ProjectionContext*.cs` covers ProjectionContext.cs and ProjectionContextFactory.cs).
- The delivered symbols exist: `IProjection<TReadModel>` (IProjection.cs:56), `IReadModel` and `IReadModel<TId>` (IReadModel.cs:30,42), `IReadModelRepository<TReadModel>` (IReadModelRepository.cs:43), `IProjectionManager` (IProjectionManager.cs:34).
- `IProjectionStore` has zero matches in `src/` (Grep), and zero in `*.md` under the worktree.
- No `*EventStore*` directory under `src/`.
- `ROADMAP.md:326` lists projections/read models as done via #36.
- The outcome `rejected-unexplained` is consistent with the sources: #21 never names #36 and never says duplicate.
- The record's front matter and its three sections are consistent with the archivist and code stages.

Code stage
- All three claims (15 files, no `IProjectionStore`, no EventStore package) reproduced. "none" is correct for an empty diff.

Tests stage
- `git show --stat 2b50a1ec` and the ancestry of `584a712b` reproduced. The manifest `targets` are unit 38, guard 7, contract 8, with no integration or property target (re-read). The integration test file `tests\Encina.IntegrationTests\Infrastructure\Marten\Projections\MartenInlineProjectionIntegrationTests.cs` exists. The stage states it was not run, which is accurate.
- Measured coverage re-run with my own subdirectories (`artifacts\audit\coverage\verify-unit`, `verify-guard`, `verify-contract`, `-c Release`, same filters). The test counts match: 135 unit, 35 guard and 18 contract tests passed. The per-file union of hits matches every number in tests.md:
  - Unit: InlineProjectionDispatcher 97/101, InlineProjectionRelay 44/45, IProjectionManager 6/6, MartenProjectionManager 107/283, MartenReadModelRepository 121/150, ProjectionContext 21/21, ProjectionContextFactory 14/17, ProjectionOptions 5/5, ProjectionRegistry 84/85, ProjectionStatus 10/10.
  - Guard: 17/101, 0/45, 2/6, 63/283, 13/150, 4/21, 0/17, 0/5, 12/85, 0/10.
  - Contract: only ProjectionRegistry is covered, 78/85; every other file is 0.
- The claim that `MartenProjectionManager` is referenced by no test outside `Marten\Projections\` except `IAggregateRepositoryContractTests.cs` is confirmed by Grep. The stage reports measured figures, not estimates, and states they are informational.
- CRAP is reported as "pending #1346", the expected placeholder.

Docs stage
- No `.md` anywhere in the worktree mentions `IProjectionStore`. `src/Encina.Marten/README.md` does not exist. No `docs/features` page is about projections.

Remediation stage
- `artifacts\knowledge\remediation\` does not exist, so there are no drafts to check for duplicates, prefix, headers or milestone. Code, tests and docs each report "- none", so there are no findings to account for. The remediation stage's "no drafts" statement is complete.

## Corrections
(none)

## Lessons for the pipeline
- Docs stage: it said `cursor-pagination.md`, `crypto-shredding.md` and `audit-marten.md` "only mention the word" projection. `data-retention.md` also does. This did not change any outcome. Docs stages should list matches from the search output, not from memory.
- An empty-diff issue ("created in error", delivered under a successor) went through all stages cleanly. The verifier can finish by re-running the successor's filtered unit, guard and contract tests and comparing the per-file union of hits to tests.md, which takes about 3 minutes once built.
