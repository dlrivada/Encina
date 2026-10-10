## Scope

Closed by commit 957093c2 (2025-12-26, `Fixes #37`; no PR, no linked PR in the timeline). All paths below exist today unless marked.

Source (`src/Encina.Marten/`):
- `Versioning/IEventUpcaster.cs`, `EventUpcasterBase.cs`, `LambdaEventUpcaster.cs`, `EventUpcasterRegistry.cs`, `EventVersioningOptions.cs`, `ConfigureMartenEventVersioning.cs`, `EventVersioningErrorCodes.cs`, `VersioningLog.cs`
- `EncinaMartenOptions.cs`, `ServiceCollectionExtensions.cs` (versioning hooks)
- Not in core `Encina`, and `src/Encina.EventStoreDB` does not exist.

Tests today (the original `tests/Encina.Marten.Tests` and `Encina.Marten.IntegrationTests` projects were removed by the consolidation commit 65826302):
- `tests/Encina.UnitTests/Marten/Versioning/` (ConfigureMartenEventVersioningTests, EventUpcasterRegistryTests, EventVersioningErrorCodesTests, EventVersioningOptionsTests, LambdaEventUpcasterTests)
- `tests/Encina.GuardTests/Marten/Versioning/` (VersioningGuardTests, EventUpcasterRegistryGuardTests)
- `tests/Encina.ContractTests/Marten/Core/EventUpcasterRegistryContractTests.cs`
- `tests/Encina.IntegrationTests/Infrastructure/Marten/Versioning/` (EventVersioningIntegrationTests, TestProductUpcasters)
- Property tests: the commit added `VersioningPropertyTests.cs`; no file under `tests/Encina.PropertyTests` mentions `Upcast` today. Gone, not relocated (verified by grep).
- `EventUpcasterBaseTests` has no successor file by that name (grep found no `EventUpcasterBase` test file).

Docs/other: `CHANGELOG.md` (Issue #37 entry, line 9071), `.github/coverage-manifest/Encina.Marten.json` (Versioning entries). `docs/history/2025-12.md` was deleted by commit d098e542 (broad refactor). There is no `src/Encina.Marten/README.md` and no PublicAPI*.txt in Encina.Marten.

## Destinations

- Upcaster design (Marten-wrapping interfaces, registry): ADR-019 table row "Event Versioning ... Production" and rule 6 "Upcasters for evolution" (`docs/architecture/adr/019-compliance-event-sourcing-marten.md` lines 53, 125, 273): present. Searched `upcast` (case-insensitive) in `docs/architecture/adr/*.md`: only ADR-019 and ADR-034 match; no dedicated ADR for versioning.
- User-facing guide for schema evolution / migration tooling (acceptance criterion "Schema evolution documented"): missing. Only CHANGELOG, INVENTORY.md (line 1395, 4215), ADR-019 and PROJECT-HISTORY mention it; no page under `docs/features` or guides (grep `upcast` across docs/).
- Core abstractions in `Encina` and `Encina.EventStoreDB` support: missing; not tracked by any issue I found (`gh issue list --search "upcasting event versioning"` returned only #134, #307, #309, #1221 which are message/request versioning, not event-store upcasting).
- Migration tooling: no file in `Versioning/` implements it (grep `migrat` only hits a doc comment in `EventVersioningOptions.cs`): missing.
- Property tests for upcasting chains and 85%+ coverage: property tests missing; coverage manifest entries present.
- Failure policy: `ThrowOnUpcastFailure` etc. are dead code per open bug #1914.

## Successor and duplicate issues

- None (outcome delivered). Related: #1914 (OPEN, verified 2026-10-10: "[BUG] EventVersioningOptions.ThrowOnUpcastFailure is never read ...") records a defect in the delivered code, so the failure policy is pending, not implemented.
- #134, #307 (OPEN) are message/request versioning, not successors.
- #1894 appears in the timeline only as a cross-reference (OPEN delta re-audit of #17); unrelated.

## Lessons for the pipeline

- [issue-archivist] The pre-draft's single "code touched: unknown" for a commit-only closure is resolved by `git show --stat <oid>` from the timeline `closed` event commit_id; then check every listed test path with `git log --diff-filter=D` because tests were consolidated into `tests/Encina.*Tests` and the original project paths no longer exist.
- [issue-archivist] The record schema's `area` takes `eventsourcing` (no hyphen), and the destination kind for an open issue is `backlog`; both were rejected by the checker on the first pass.
- [issue-archivist] A source marked `paraphrase:` needs a `#<number>` or URL; a bare commit hash does not count as a link, so include the issue number.
