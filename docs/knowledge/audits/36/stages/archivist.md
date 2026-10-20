## Scope

Issue #36 (created 2025-12-24T13:23:34Z, closed 2025-12-26T19:47:21Z, no comments, no PR) was closed by commit 584a712b (`gh api .../timeline` `closed` event). Its 28 files, mapped to today. No `src/Encina.Projections` and no `src/Encina.EventStoreDB` exist (Test-Path false; ROADMAP.md:804 lists EventStoreDB as deprecated).

All source files are in `src/Encina.Marten/Projections/` (per-file `git log --follow`):
- `IProjection.cs`: 584a712b, e998f6cc, 19c9051a (Sonar refactors, 2026-01-13).
- `IProjectionManager.cs`, `IReadModel.cs`, `IReadModelRepository.cs`: only 584a712b.
- `InlineProjectionDispatcher.cs`: 584a712b, 9dde5e0a, e998f6cc, 6796fcb1 (#1095), da52fcd6 (#1557).
- `MartenProjectionManager.cs`: 584a712b ... dbb42740 (#321), 06c9e316 (#543 TimeProvider), 6796fcb1, da52fcd6.
- `MartenReadModelRepository.cs`: 584a712b, 9dde5e0a, e998f6cc, da52fcd6.
- `ProjectionRegistry.cs`: 584a712b, 8916282a. `ProjectionLog.cs`: 584a712b, 6796fcb1, d30b5b24 (#1121), a806ab65 (#1328).
- `ProjectionContext.cs`, `ProjectionErrorCodes.cs`, `ProjectionOptions.cs`, `ProjectionStatus.cs`: present. New since: `InlineProjectionRelay.cs`, `ProjectionContextFactory.cs`.
- `src/Encina.Marten/EncinaMartenOptions.cs` (Projections option) and `ServiceCollectionExtensions.cs` (`AddProjections`, `AddProjectionInfrastructure`, registered only when `Projections.Enabled`).
- Manifest: `.github/coverage-manifest/Encina.Marten.json` has `Projections/*` entries.
- `src/Encina.Marten` has no README and no PublicAPI files today (listing checked).

Tests (original 10 files in 584a712b were consolidated): `tests/Encina.UnitTests/Marten/Projections/*` (Context, ErrorCodes, Options, Registration, Registry, Status, ManagerTests, ReadModelRepository, InlineProjectionDispatcher*, Relay), `tests/Encina.GuardTests/Marten/Projections/*`, `tests/Encina.ContractTests/Marten/Core/{ProjectionRegistry,MartenReadModelRepository}ContractTests.cs`, `tests/Encina.IntegrationTests/Infrastructure/Marten/{Core/MartenReadModelRepositoryIntegrationTests,Projections/MartenInlineProjectionIntegrationTests}.cs`. No projection tests found in `tests/Encina.PropertyTests` (the original ProjectionPropertyTests is gone; the issue marked property tests "Maybe").

Shipped vs. proposed (compared one by one):
- Package `Encina.Projections`: NOT created; code is in Encina.Marten.
- `IProjection<TEvent, TReadModel>`: shipped as `IProjection<TReadModel>` plus `IProjectionHandler<in TEvent,TReadModel>`, `IProjectionCreator`, `IProjectionDeleter`.
- `IReadModelRepository<T>`: shipped (Get/GetByIds/Query/Store/StoreMany/Delete/DeleteAll/Exists/Count, all `Either`).
- Rebuild: shipped as `IProjectionManager.RebuildAsync<TReadModel>` (two overloads) plus status/start/stop/pause/resume.
- EventStoreDB integration: not built.
- 85%+ coverage acceptance criterion: not measurable from the issue; per-flag targets live in the manifest (code stage to judge).

## Destinations

- Abstractions (IProjection/IReadModel): present in code; contract tests present (`ProjectionRegistryContractTests`). Docs: only release notes (`docs/releases/v0.11.0/CHANGELOG-DETAILS.md:3733-3738`, `docs/releases/pre-v0.10.0/README.md:1488-1489,1542-1545` mention `IReadModelRepository<T>` and `IProjectionManager`, found by grep). No current guide under `docs/` or `docs/en` documents projections (grep for RebuildAsync/IProjectionHandler/IReadModelRepository outside plans, knowledge and releases found nothing). The issue's "Add documentation" task is thus only met by changelog/release notes: docs gap for the docs stage.
- Read model repository: present, contract and integration tests present.
- Rebuild: `MartenProjectionManagerTests` present; the issue's "Rebuild capabilities tested" criterion is covered only by unit tests (no rebuild integration test seen; check in tests stage).
- Inline projections opt-in: `MartenInlineProjectionIntegrationTests` present.
- ADR: no ADR was written for this issue. `grep -i "IReadModelRepository|IProjectionManager|projection"` over docs/architecture/adr matched ADR-019/020/027/034/036, which use projections but do not record these abstractions as a decision (ADR-019 mentions the repository only in compliance context). Not claimed as a gap beyond that.
- AGENTS.md:70 lists "projections" as applying to event-sourcing providers (Marten primary, EventStoreDB future) and §3 requires Marten projections to take dependencies via `IDocumentOperations`, not `IServiceProvider`: consistent with the shipped design.
- Separate package / EventStoreDB: not carried anywhere live; ROADMAP.md:804 lists EventStoreDB as deprecated. Recorded as `current: no`.

## Successor and duplicate issues

- #21 (identical title), verified with `gh issue view 21`: CLOSED/COMPLETED, closed 2025-12-24T11:52:21Z, 1h31m before #36 was created. Its record (docs/knowledge/issues/21.md) is `rejected-unexplained`; #36 is the same-scope re-creation that delivered. #36 itself is a delivered issue, no successor.
- No other duplicate or successor found in the timeline (only a rename and milestone/project events).

## Lessons for the pipeline

- The pre-draft listed `Encina.Projections` and `Encina.EventStoreDB` as packages straight from the issue's "Affected Packages"; neither exists. Records take packages from shipped code (Encina.Marten only).
- The pre-draft had `closed_at: 12/26/2025 19:47:21` and `linked_prs: []`, and invented "Candidate destinations" (spec-invariant) and a "Rules and lessons" claim about 85% coverage taken from acceptance criteria; both were dropped as not being decisions.
- The audit tooling's first `gh issue view --comments` call printed only JSON in this session when chained with another gh call; re-query body and comments with `--json body,comments --jq` to be sure nothing is missed.
- The record validator requires a link in every `paraphrase:` source (URL or `#<issue>`), including for commit-based paraphrases; cite `issue #<n>` or a URL even when the fact comes from a commit or a repo file.
