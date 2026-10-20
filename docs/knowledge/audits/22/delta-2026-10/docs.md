## Pages reviewed

Issue #22 was closed in error and shipped no page (delta-scope.md). Rule (a) point 5 was run on the feature a successor (#37) delivered, event versioning and upcasting in `src/Encina.Marten/Versioning/` (8 files, all present: `Get-ChildItem src\Encina.Marten\Versioning`). Searches were run from the worktree root with `Select-String` over `docs\`, `README.md` and `src\Encina.Marten` `*.md`, excluding `docs\knowledge`, `docs\plans`, `docs\releases` and `docs\history` (records, plans and release notes, not reader pages).

| Destination checked | Result (command output) |
| --- | --- |
| Package README `src/Encina.Marten/README.md` | `Test-Path` false; `Get-ChildItem src\Encina.Marten -Filter README.md` returns nothing |
| Concept or guide page for event versioning | no file named `event-versioning*` (`Test-Path docs\features\event-versioning.md` false); the only `docs\features\*.md` hit for `upcast` is `crypto-shredding.md:797`, a limit ("Raw `JsonDocument` upcasters are not supported"), not a description of the feature |
| Reader pages that name `IEventUpcaster`, `EventUpcasterRegistry` or `AddEventUpcaster` | `docs/architecture/adr/019-compliance-event-sourcing-marten.md:53` (table row, Production) and `:125` (one-line principle), `:273`; `docs/INVENTORY.md:1389`, `:4209`; `docs/engineering/PROJECT-HISTORY.md:277` (history). None shows registration or a sample |
| Tutorials | `docs/tutorials/index.md` has 12 lines and one lesson (`quickstart.md`, line 12); no event-sourcing or versioning entry |
| Learning path or index | `docs/index.md:69` has one row "Event Sourcing ... Marten event store with projections, snapshots, GDPR crypto-shredding" with no versioning; `docs/features/index.md` (86 lines) has no Marten or versioning link (only `event-metadata-tracking.md` at :85); `docs/guides/` has no event or Marten page |

Registration surface a page would document (verified in `src/`): `EncinaMartenOptions.EventVersioning` (`src/Encina.Marten/EncinaMartenOptions.cs:94`), `EventVersioningOptions.Enabled`, `ThrowOnUpcastFailure`, `AddUpcaster<TUpcaster>()`, `AddUpcaster<TFrom,TTo>(...)`, `ScanAssembly` (`src/Encina.Marten/Versioning/EventVersioningOptions.cs:42, 48, 60, 122, 143`) and the public `AddEventUpcaster<TUpcaster>` (`src/Encina.Marten/ServiceCollectionExtensions.cs:296`; the `AddEventVersioning` at :239 is `internal`).

## Findings

1. **Major** — Event versioning and upcasting (delivered under #37 in `src/Encina.Marten/Versioning/`, the feature #22 asked for) has no concept or guide page, no package README and no tutorial or learning-path entry (rule (a) point 5). Evidence: `src/Encina.Marten/README.md` does not exist; no `docs/features/` or `docs/guides/` page describes upcasters (the only `docs/features` hit for `upcast` is the limit at `docs/features/crypto-shredding.md:797`); `docs/tutorials/index.md` lists only the quickstart (:12); `docs/index.md:69` and `docs/features/index.md` do not mention versioning. The only reader-facing mentions are one table row and two sentences in `docs/architecture/adr/019-compliance-event-sourcing-marten.md:53, :125, :273`, which carry no registration steps (`options.EventVersioning.Enabled`, `AddUpcaster`, `AddEventUpcaster<T>`) and no sample. A reader cannot learn how to evolve an event schema. Related issue: #1894 (finding "No concept or how-to page, package README or tutorial entry for event sourcing on Encina.Marten") covers a feature page, a guide, a tutorial and `src/Encina.Marten/README.md` for aggregates, repository, snapshots and projections; its proposal does not list versioning or upcasters (0 hits for "upcast" and "Versioning" in its body), so the fix is an extension of #1894 (add event versioning to the page, the README and the tutorial), not a separate issue; ask the orchestrator to retarget or extend #1894.

2. **Minor** — `docs/index.md:69` ("Event Sourcing", 2 packages) describes the capability list without event versioning, and `docs/features/index.md` (86 lines, last entry :85) has no event-sourcing entry, so the feature table does not reach upcasting even once finding 1 is fixed. Evidence: `Select-String docs\features\index.md -Pattern 'marten|event'` returns only `:85 Event Metadata Tracking`. Related issue: #1894 (`docs/index.md:69` named in its File(s) line; the features index link is part of its fix 1).

## Informational (not findings)

- Point 1 (visual): no page was delivered, so there is nothing to count. ADR-019 is a decision record, not the destination of #22.
- Point 2 (samples): `docs/architecture/adr/019-compliance-event-sourcing-marten.md:53` names `IEventUpcaster<TFrom, TTo>` and `EventUpcasterRegistry`, both of which exist (`src/Encina.Marten/Versioning/IEventUpcaster.cs`, `EventUpcasterRegistry.cs`). No csharp block describes the feature.
- Point 3 (figures): no figure on a versioning page; none exists.
- Point 4 (placement): not applicable until a page exists; `docs/features/` is the quadrant (concept and reference) the `encina-docs` skill uses for a feature.
- `docs/INVENTORY.md:838-842`, `:394`, `:747` are Spanish ("Versionado y upcasting") and describe other, unbuilt items (#134, #307); they are outside rule (a) and #22, noted for the language check of another audit.
- `src/Encina.EventStoreDB` does not exist (original archivist); no page documents it as built.

## Lessons for the pipeline

- For a closed-in-error issue whose feature a successor delivered, check whether the umbrella issue that names the surrounding feature (here #1894 for Encina.Marten) lists the delivered sub-feature in its proposal; a general "event sourcing page" issue can miss the sub-feature, and the finding is then an extension of that issue.
- A public registration helper next to an `internal` twin (`AddEventUpcaster` public at `ServiceCollectionExtensions.cs:296`, `AddEventVersioning` internal at :239) should be checked for visibility before a page cites either.
