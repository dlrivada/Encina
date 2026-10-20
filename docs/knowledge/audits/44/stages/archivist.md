## Scope

Closing commit 929046c5 (2025-12-24, no PR; found via the issue timeline) changed `Encina.slnx`, `src/Encina.Testing/{Encina.Testing.csproj,EncinaFixture.cs,Assertions/EitherAssertions.cs}`, `tests/Encina.Testing.Tests/{EncinaFixtureTests.cs,EitherAssertionsTests.cs,csproj}`. Today:
- `src/Encina.Testing/EncinaFixture.cs` and `Assertions/EitherAssertions.cs` (git log --follow shows 929046c5 as origin; EitherAssertions was later extended, e.g. ab423a83 AndConstraint/collection/streaming assertions, issue #170). The package now also holds EncinaTestFixture, Handlers/, Sagas/, Messaging/, Modules/, Time/, etc. (later work, not this issue).
- `tests/Encina.Testing.Tests` is gone (consolidation); its tests now exist in `tests/Encina.UnitTests/Testing/EncinaFixtureTests.cs`, `Testing/EitherAssertionsTests.cs` and duplicated in `Testing/Base/` (open #1693).
- `src/Encina.Testing.Fakes/` (FakeEncina, fake stores): added by 08dd6734 on 2025-12-30, NOT by the closing commit, though the issue lists it as affected.
- Coverage manifest: `.github/coverage-manifest/Encina.Testing.json`.
- In-memory transport: delivered elsewhere, as the separate package `src/Encina.InMemory` (`InMemoryMessageBus.cs`, `IInMemoryMessageBus.cs`, `ServiceCollectionExtensions.cs`), first added as `src/SimpleMediator.InMemory` by 9271f1998 on 2025-12-21 (`git log --diff-filter=A --follow` on InMemoryMessageBus.cs and the csproj), renamed to Encina.InMemory by cab5e04a7 on 2025-12-22; both before the issue (2025-12-24), the first three days before; `src/Encina.Testing` has none because it never needed one (Correction 1 of the verification).
- Handler mocking utilities: not found in `src/Encina.Testing`; the nearest surface is `Handlers/{HandlerSpecification,Scenario,ScenarioResult}.cs` and `Sagas/SagaSpecification.cs`, added by ada094223 (2026-01-02, issue #434), not by this issue. Pre-draft open question answered: Fakes not in the closing commit; 85% coverage not verified here (per-flag model has no project-wide figure).

## Destinations

- Package existence and description: `docs/INVENTORY.md` lines 3108-3109 list Encina.Testing and Encina.Testing.Fakes as complete: present.
- `docs/plans/testing-dogfooding-plan.md` and `docs/releases/pre-v0.10.0/README.md` mention EncinaFixture: present.
- Package README ("Add documentation" task): missing (`src/Encina.Testing/README.md` does not exist); tracked by open #1689.
- In-memory transport: present, in `src/Encina.InMemory`; `docs/INVENTORY.md:1372` lists `Encina.InMemory` as complete.
- Handler mocking utilities: not delivered as mocking helpers and no tracking issue was found. The record's pending-work item uses a docs destination, `docs/releases/pre-v0.10.0/README.md` (line 8928 documents `HandlerSpecification<TRequest, TResponse>`), the page that describes the nearest delivered surface (BDD specifications in `src/Encina.Testing/Handlers/` and `Sagas/`).
- Decision-level rules (ADR, AGENTS.md): none; the pre-draft's roadmap candidate was not verified against ROADMAP.md and is not recorded.

## Successor and duplicate issues

None. Related open follow-ups (verified with gh issue view): #1689 OPEN (no package README), #1693 OPEN (duplicated Testing/Base tests), #1922 OPEN (assertions expect error codes no error carries). #430 CLOSED (Encina.Testing.Verify) only cross-references.

## Lessons for the pipeline

- Pre-draft lives in the main checkout only (artifacts/ is gitignored); read it from D:\Proyectos\Encina\artifacts\knowledge\predraft\.
- A "not delivered" claim needs a search of the whole `src/` tree by concept before it becomes a pending-work item; searching only the issue's own package missed `src/Encina.InMemory` (verifier correction 1).
- A feature issue ticking many tasks can be closed by a commit that delivers only part of them; compare each task (here in-memory transport, mocking) with the commit file list and today's src before choosing delivered vs partial.
