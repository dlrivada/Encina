# Docs stage: delta rules-2026-10 (rule (a) only), issue #25

## Pages reviewed

Issue #25 was closed in error and delivered nothing; the snapshot feature (`src/Encina.Marten/Snapshots/`: `ISnapshotStore<T>`, `ISnapshotable<T>`, `SnapshotAwareAggregateRepository<T>`, `SnapshotOptions`) was delivered by #52. Rule (a) point 5 is run on the delivered feature. Paths checked with `Test-Path` or `Get-ChildItem` in the worktree.

| Page | Result |
| --- | --- |
| `src/Encina.Marten/README.md` | does not exist (`Get-ChildItem src\Encina.Marten -Filter *.md` returns nothing) |
| `docs/features/` (`docs/features/index.md` has 86 physical lines by `(Get-Content).Count`, 69 of them non-blank) | no event-sourcing or snapshot page; `docs/features/index.md` has 0 hits for `Marten` and `event sourc`; only a link to `event-metadata-tracking.md` at :85 |
| `docs/features/crypto-shredding.md:602` | only feature page that names `SnapshotEnvelope<T>`; it is about encrypting snapshot state, not about adopting snapshots (:9, :43, :99, :439 say "snapshot" in passing) |
| `docs/tutorials/index.md`, `docs/tutorials/quickstart.md` | `Select-String -Pattern 'event sourc|Marten|snapshot'` over `docs/tutorials/*.md`, `docs/index.md`, `docs/introduction.md`: only `docs/index.md:69`; no tutorial or learning-path entry |
| `docs/index.md:69` | one table row, "Marten event store with projections, snapshots, GDPR crypto-shredding", with no link and `2` as a typed package count |
| `docs/architecture/adr/019-compliance-event-sourcing-marten.md:51,124` | ADR table row and decision text name `ISnapshotStore<T>`, `SnapshotAwareAggregateRepository<T>` and `ISnapshotable<T>`; names exist in `src/Encina.Marten/Snapshots/`; this is decision text, not a guide |
| `docs/INVENTORY.md:1388` | Spanish inventory page, writes `ISnapshotStore` (non-generic; the type is `ISnapshotStore<T>`) |

Search used for "no page teaches snapshots": `Get-ChildItem docs -Recurse -Filter *.md` excluding `knowledge|history|plans|releases`, pattern `ISnapshotStore|ISnapshotable|SnapshotOptions|SnapshotEnvelope|EnableSnapshots|SnapshotEvery`. Hits: `docs/INVENTORY.md:1388,1449`, `docs/architecture/adr/019-...:51,124`, `docs/features/crypto-shredding.md:602`. None shows `EncinaMartenOptions.Snapshots` (`Enabled`, `SnapshotEvery` = 100, `KeepSnapshots` = 3, `AsyncSnapshotCreation` = true, `src/Encina.Marten/Snapshots/SnapshotOptions.cs`) or how an aggregate opts in.

## Findings

1. **Major** — Rule (a) point 5, no concept or guide page and no reference for aggregate snapshots. `docs/features/` has no page explaining `ISnapshotable<T>`, `SnapshotAwareAggregateRepository<T>` or the `Snapshots` options (`SnapshotEvery`, `KeepSnapshots`, `AsyncSnapshotCreation`); the only feature-page mention is the encryption note at `docs/features/crypto-shredding.md:602`; `docs/index.md:69` is an unlinked table row. Open umbrella #1894 already asks for an event-sourcing feature page on `Encina.Marten` that lists snapshots in its proposal (body searched for `napshot`: hits in the page proposal and in the coverage items), so this is an extension of #1894, not a new issue; ask that the page carry a snapshots section with the options table and a Mermaid sequence of load-from-snapshot plus following events.
2. **Major** — Rule (a) point 5, `src/Encina.Marten/` has no README, so the snapshot feature has no package-level reference (0 `.md` files in the folder). Covered by #1894 ("no package README"); extend it with the snapshot registration (`options.Snapshots.Enabled`, registered by the `internal` `AddSnapshots` at `src/Encina.Marten/ServiceCollectionExtensions.cs:223`, called at :70 only when enabled) so the README names the public entry point rather than the helper.
3. **Major** — Rule (a) point 5, the feature appears in no tutorial or learning path. `docs/tutorials/` holds `index.md` and `quickstart.md`; both have 0 hits for `Marten`, `event sourc` and `snapshot`. #1894 names the missing tutorial-index entry; extend it with a "snapshot a long-lived aggregate" step in the event-sourcing tutorial.
4. **Minor** — Rule (a) point 4, `docs/index.md:69` lists snapshots in an unlinked row, and `docs/features/index.md` has no event-sourcing entry, so no neighbouring page links to the feature. #1894 asks for the row to become a link; extend it to the features index.
5. **Minor** — Rule (a) point 2, `docs/INVENTORY.md:1388` names `ISnapshotStore` without its type argument; `src/Encina.Marten/Snapshots/ISnapshotStore.cs` declares the generic `ISnapshotStore<T>` (as `docs/architecture/adr/019-compliance-event-sourcing-marten.md:51` writes it). The page is Spanish and carries emoji status marks, already outside the English-only rule; fold into the item that rewrites or retires `docs/INVENTORY.md`.

## Informational (not findings)

- `docs/index.md:69` types the package count `2` for Event Sourcing as a literal; site-wide figure literals are the same class as the one noted in the #23 stage, not specific to this feature.
- `docs/INVENTORY.md:1449` says "Backward compatible con `SnapshotEvery` existente"; not reviewed further (Spanish, history text).
- ADR-019 lines 51 and 124 and the code agree on `ISnapshotStore<T>` and `ISnapshotable<T>`; the `[Snapshot(every: 100)]` attribute proposed in #25 appears on no page (it does not exist in `src/`, per the original code stage).
- `crypto-shredding.md:602` is accurate against `SnapshotEnvelope<T>` and was not scoped further.

## Lessons for the pipeline

- For a closed-in-error issue whose delivered sub-feature (snapshots) sits inside a package that an open umbrella issue (#1894) already covers, the delta docs stage should map each rule (a) point 5 gap to that umbrella and say what to add to it, instead of opening a new issue.
- A feature-level search for the type names (`ISnapshotStore`, `SnapshotOptions`) quickly separates decision text (ADR) and passing mentions from pages that teach the feature; list the hits by file:line in the stage.
- State line counts as physical lines by `(Get-Content).Count` and say how many are non-blank; `Measure-Object -Line` counts only non-blank lines (audit #25 verifier FAIL: 69 was the non-blank count of an 86-line file).
