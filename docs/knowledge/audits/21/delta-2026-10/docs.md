## Pages reviewed

Issue #21 delivered nothing (closed "created in error", no PR, no code or docs change; the feature it asked for was delivered under #36 and lives in `src/Encina.Marten/Projections/`, 15 files, counted with `(Get-ChildItem src\Encina.Marten\Projections -File).Count`). There is no page of #21 to review, so rule (a) is checked on the places that describe what the issue asked for (projections and read models on `Encina.Marten`):

- `docs/index.md` (line 69, the Event Sourcing row)
- `docs/tutorials/index.md` (12 lines, 9 non-blank) and `docs/tutorials/quickstart.md`
- `docs/features/index.md` (the only event-sourcing-adjacent entry is `Event Metadata Tracking`, line 85)
- the package folder `src/Encina.Marten/` (README check)
- reader-facing pages that name the projection types: `docs/features/crypto-shredding.md` (816 lines; line 604 names `MartenProjectionManager`), `docs/INVENTORY.md:1387`.

Search used: `Get-ChildItem docs,README.md,src -Recurse -Filter *.md -File` (excluding `docs\knowledge\`), pattern `IReadModelRepository|IProjectionManager|MartenProjectionManager|InlineProjectionDispatcher|AddProjection\b|IReadModel\b`. 224 hits; outside plans (`docs/plans/`), release notes (`docs/releases/`) and ADRs (`docs/architecture/adr/`) they are `docs/INVENTORY.md` (lines 1387, 5928, 6001, 6040, 6093, 6159, inventory bullets), `docs/features/crypto-shredding.md:604` and the compliance package READMEs (`src/Encina.Compliance.Consent`, `.DataSubjectRights`, `.LawfulBasis`, which describe their own read models). `IProjectionStore` (the type #21 proposed) matches no page.

## Findings

1. **Major** — Rule (a) point 5 (adequate docs for a feature in scope): the projections and read-models feature that #21 asked for (and #36 delivered) has no concept or guide page, no reference, no package README and no tutorial or learning-path entry. Evidence: `src/Encina.Marten/` has no `.md` file (`Get-ChildItem src\Encina.Marten -File` lists only `.cs`, `.csproj`); `docs/tutorials/` has 2 pages (`index.md`, `quickstart.md`) with 0 hits for `projection`, `read model`, `event sourc` or `Marten`; `docs/features/index.md` has 0 hits for `Marten`/`projection`; `docs/index.md:69` is a one-line row ("Marten event store with projections, snapshots, GDPR crypto-shredding") with no link; no page documents `IProjection<TReadModel>` (`src/Encina.Marten/Projections/IProjection.cs:56`) or `IReadModelRepository<TReadModel>` (`src/Encina.Marten/Projections/IReadModelRepository.cs:43`); the only reader-facing mentions are `docs/features/crypto-shredding.md:604` (a failure mode of `MartenProjectionManager`) and `docs/INVENTORY.md:1387` (one bullet naming three types). Related issue: duplicate of #1894, item "No concept or how-to page, package README or tutorial entry for event sourcing on Encina.Marten" (its Proposed Fix item 1 already names projections, items 3 and 4 the tutorial links and `src/Encina.Marten/README.md`); #1847 lists `Marten` among the 11 packages with no README. No new issue needed; ask the orchestrator to confirm #1894's fix covers a projections section (read models, `IProjectionManager`, inline dispatch, `ProjectionOptions`) and not only aggregates and snapshots.

## Informational (not findings)

- Points 1, 2 and 3 of rule (a) have no page to apply to: the issue shipped no page, so there is no diagram, sample or figure of #21's own. The original audit's empty-diff conclusion stands.
- `docs/INVENTORY.md` is mostly Spanish; it is an inventory outside the reviewed scope, tracked by earlier delta issues, and is not judged here.
- Point 4 (placement): nothing to place; the future page location is already in #1894's Proposed Fix.

## Lessons for the pipeline

- For a closed-in-error issue whose feature a successor delivered, the delta docs stage should still run rule (a) point 5 on the feature's destination (concept page, package README, tutorial entry) and map it to the open umbrella issue that already names the feature, instead of repeating "no page, no finding".
- State line counts as physical lines by command ((Get-Content f).Count), and say how many are non-blank when that matters.
