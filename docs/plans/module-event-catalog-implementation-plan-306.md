# Implementation Plan: Modular Monolith Integration Events (the module event catalog, the delta over #358)

> **Issue**: [#306](https://github.com/dlrivada/Encina/issues/306)
> **Type**: Feature
> **Complexity**: Low (4 phases, 2 pull requests of 11 and 3 files)
> **Estimated Scope**: ~250 lines of production code + ~600 lines of tests
> **Family**: modular monolith (#358 owns the integration event contract, the module event bus and the contract validator; #360 the module graph and exports; #352 the boundary rule); this plan owns only what #306 adds on top: the event catalog

---

## Summary

#306 asks for integration events between modules: `IIntegrationEvent`, `IntegrationEventBase`, `IModuleEventBus`, an in-process bus with optional outbox, `IIntegrationEventHandler<T>`, `[DependsOnEvents]`, `IModuleEventCatalog` and startup validation. Almost all of it is the scope of #358 ("Inter-Module Communication"), which is planned. State of `origin/main` (`96c8803bd`) and of the sibling plans, verified by reading:

| #306 item | Today | Owner |
|-----------|-------|-------|
| `IIntegrationEvent` with `EventId`, `OccurredAtUtc`, `SourceModule` | `Encina.DomainModeling.IIntegrationEvent` (`src/Encina.DomainModeling/IIntegrationEvent.cs`) has `EventId`, `OccurredAtUtc`, `EventVersion`, `CorrelationId` and is not an `INotification`; no `SourceModule` | #358 pull request 1 (`IIntegrationEvent : INotification`, `SourceModule`) |
| `IntegrationEventBase` | The abstract record `IntegrationEvent` already exists in the same file | #358 pull request 1 adds `SourceModule` to it; no second base record |
| `IModuleEventBus` (in-process and outbox) | Missing | #358 (`IIntegrationEventBus`, in-memory bus pull requests 2-3, outbox bus pull request 5): #352 Design Choice 8 keeps one bus |
| `IIntegrationEventHandler<T>` | Missing | #358 Design Choice 3: handlers are ordinary `INotificationHandler<TEvent>` |
| `[DependsOnEvents]` | Missing | #358 and #360: the subscriber's module dependency is `[DependsOn]` (#360) and the publisher exports the event type (`Exports`) |
| Startup dependency validation | Missing | #358 pull request 4 (`ModuleContractValidator`, `ModuleContractStartupValidator`) |
| `IModuleEventCatalog` (published and consumed events per module, `ValidateDependencies`) | Missing | **This plan** |

So #306 is a duplicate of #358 except for the catalog. This plan plans the delta only and records the duplicate relation as a Dilemma for the maintainer (Design Choice 1); it does not decide it.

**Delivered**: `IModuleEventCatalog` (read-only view of which module publishes and which modules consume each integration event, a Mermaid event-flow diagram and a `ValidateDependencies` that reuses #358's validator), its internal implementation and an opt-in registration extension.

**Not in scope**: the bus, the contract, the handler model and the startup validator (#358); module exports and the dependency graph (#360); audit of cross-module events and idempotent consumers (follow-up issue files of the #358 plan).

**Affected packages**: `Encina.DomainModeling` (placement follows the bus, Design Choice 2).

**Provider category**: none. The catalog is in-process introspection; no store, transport or lock is involved.

---

## Design Choices

<details>
<summary><strong>1. What to do with #306, which overlaps #358 (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep #306 open for the delta only: the event catalog, rewritten title and body to say so; #358 owns everything else** | The one useful part of the issue (catalog) ships; no duplicate bus | An issue with a narrowed scope needs its title and body rewritten |
| **B) Close #306 as a duplicate of #358 and move the catalog idea into a comment on #358** | One issue per feature; nothing left to plan here | The catalog is not tracked as work unless #358's plan adopts it |
| **C) Implement #306 as written, with its own `IModuleEventBus` beside #358's bus** | Matches the issue text | Two buses for one concept, contradicting #352 Design Choice 8 and #358 |

### Chosen Option: **A — keep the delta, plan the catalog** (recommended, pending the maintainer)

### Rationale

A wins when the catalog is wanted for tooling, the CLI and the documentation generator (a module-to-event map is the first thing an architect asks for). B wins if the maintainer judges the catalog not worth a pull request of its own. C is listed for completeness: it loses in every scenario because it duplicates the bus. The issue number stays the owner of the catalog either way.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Where the catalog lives (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) In `Encina.DomainModeling`, next to the integration event contract and the bus (`IIntegrationEvent` is declared there and core `Encina` cannot reference it)** | The catalog sees `IIntegrationEvent` directly; no new reference | `Encina.DomainModeling` owns one more module-aware type |
| **B) In core `Encina.Modules`, typed by `Type` and a runtime check of the interface by name** | Core owns the whole module story | Stringly-typed check; moves with #358's D1 only if the contract moves to core |
| **C) In `Encina.Testing` as an architecture helper** | Out of production code | Not available at runtime (health, CLI, diagnostics endpoints) |

### Chosen Option: **A — `Encina.DomainModeling`** (recommended, pending the maintainer)

### Rationale

A wins because it follows the contract. If the maintainer answers #358's D1 with option B (the contract moves into core), the catalog moves with it and this choice becomes option B without further change. C wins only if the catalog is meant for tests alone.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. How a module's consumed events are known (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Derived: the closed `INotificationHandler<TEvent>` types with `TEvent : IIntegrationEvent` found by the same discovery as #358's contract validator, attributed to a module through `IModuleHandlerRegistry.GetModuleName(handlerType)`** | Nothing to declare; the catalog cannot drift from the handlers that exist | A subscriber that is not a handler (a future broker adapter) is invisible |
| **B) Declared: `[DependsOnEvents(typeof(OrderPlacedIntegrationEvent))]` on the module, as the issue shows** | Explicit, readable, visible to analyzers | A second declaration that can disagree with the real handlers; needs a validator rule to compare them |
| **C) Both: derived by default, `[DependsOnEvents]` adds subscriptions that have no handler** | Covers non-handler subscribers | Two sources and a merge rule for a case that does not exist yet |

### Chosen Option: **A — derived from the handlers** (recommended, pending the maintainer)

### Rationale

A wins while every consumer is an `INotificationHandler` (#358 Design Choice 3). B wins when an architecture review wants the declared dependency to be checked against reality; C wins once a non-handler subscriber exists. The published side is the module's exports (#360, `IModuleGraph.GetExports`) filtered to `IIntegrationEvent` types, so it needs no declaration either.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. `ValidateDependencies` reuses #358's validator</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ValidateDependencies()` calls #358's pure `ModuleContractValidator` over the catalog's own snapshot of inputs and wraps the violations in one `Left`** | One set of rules; the catalog and the startup validator can never disagree | The catalog depends on #358 pull request 4 |
| **B) The catalog implements its own checks** | Independent | Two validators for one contract (`AGENTS.md` section 3: no parallel layers) |

### Chosen Option: **A — reuse the validator**

### Rationale

The error is `encina.modules.event_contract_invalid` with `ErrorCategory.FailedPrecondition` (ADR-062); the message lists module names and event type names only, never a payload.

**Class:** Trivial
**Settled by:** the #358 plan, Phase 3 (`ModuleContractValidator` is a pure function over the graph, the registry and the handler discovery) and `AGENTS.md` section 3 (no parallel layers).

</details>

<details>
<summary><strong>5. A module name the catalog does not know (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `GetPublishedEvents` and `GetConsumedEvents` throw `ArgumentException` for a name that is not a registered module (case-insensitive like `ModuleRegistry`)** | A typo cannot look like "this module has no events" | The caller must know the module exists |
| **B) Return an empty list** | No exception path | A typo silently answers "nothing", which tooling reads as "no dependency" |

### Chosen Option: **A — reject unknown names** (recommended, pending the maintainer)

### Rationale

A wins when the catalog feeds checks that treat an empty answer as "safe". B wins for a diagnostics endpoint that must never throw. A blank name is an `ArgumentException` in both cases.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Opt-in registration</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Its own `AddEncinaModuleEventCatalog(this IServiceCollection)`, `TryAddSingleton`, requiring #358's `AddEncinaIntegrationEvents` and `AddEncinaModules`** | Pay for what you use; no edit to #358's registration | One more call |
| **B) Registered by `AddEncinaIntegrationEvents`** | No extra call | Every bus user pays for a catalog and the file belongs to #358 |

### Chosen Option: **A — a separate opt-in extension**

### Rationale

Every feature is opt-in (`AGENTS.md` section 1). The extension throws `InvalidOperationException` at registration when `IModuleRegistry` is missing (a registration error, like #358's bus).

**Class:** Trivial
**Settled by:** `AGENTS.md` section 1 (pay-for-what-you-use, every feature opt-in) and section 3 (registration completeness).

</details>

---

## Implementation Phases

### Phase 1: The catalog contract and implementation (pull request 1)

> **Goal**: The read-only catalog over #358's discovery and #360's exports.

<details>
<summary>Tasks</summary>

1. `src/Encina.DomainModeling/IModuleEventCatalog.cs` (public): `IReadOnlyList<Type> GetPublishedEvents(string moduleName)`, `IReadOnlyList<Type> GetConsumedEvents(string moduleName)`, `IReadOnlyList<string> GetPublishers(Type eventType)`, `IReadOnlyList<string> GetConsumers(Type eventType)`, `string ToMermaid()` (a `flowchart LR` with one edge per module-to-module event), `Either<EncinaError, Unit> ValidateDependencies()`. XML docs with an example; module names compare case-insensitively.
2. `src/Encina.DomainModeling/ModuleEventCatalog.cs` (internal sealed): built once, lazily and thread safely, from `IModuleRegistry`, `IModuleGraph` (exports) and the handler discovery of #358 PR4; a published event is an exported type that implements `IIntegrationEvent` (D3); a consumed event is the `TEvent` of a handler attributed to the module; `ValidateDependencies()` returns `Right(Unit)` or one `Left` listing every violation of #358's validator (D4).
3. `src/Encina.DomainModeling/ModuleEventCatalogErrors.cs` (internal static): `encina.modules.event_contract_invalid` (`ErrorCategory.FailedPrecondition`).
4. `src/Encina.DomainModeling/ModuleEventCatalogServiceCollectionExtensions.cs`: `AddEncinaModuleEventCatalog(this IServiceCollection)` (D6).
5. Tests: unit `ModuleEventCatalogTests` (published and consumed lists, publishers and consumers, case-insensitive module names, Mermaid text, valid and invalid contract, the same input twice gives the same answer), guard `ModuleEventCatalogGuardTests` (null and blank module name, null event type, null services), DI `ModuleEventCatalogRegistrationTests` (`ValidateOnBuild` and `ValidateScopes`; missing registry throws), contract `ModuleEventCatalogContractTests` (the catalog agrees with `ModuleContractValidator` on the same inputs), property `ModuleEventCatalogPropertyTests` (for generated acyclic module and event sets every consumed event of a module is published by at least one module when the validator reports no violation); `PublicAPI.Unshipped.txt` and the `Encina.DomainModeling.json` manifest through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT:
- Issue #306; plan docs/plans/module-event-catalog-implementation-plan-306.md. #358 pull requests 1 to 4 (contract, bus, ModuleContractValidator) and #360 (IModuleGraph, exports) are merged. D1, D2, D3 and D5 are recommended, pending the maintainer: follow the Maintainer Decisions section if it exists.

TASK:
Add IModuleEventCatalog, its internal implementation, the error factory, the registration extension and every test listed.

KEY RULES:
- Reuse ModuleContractValidator; no second set of rules (D4). An unknown module name is an ArgumentException (D5).
- The catalog is read-only and holds no scoped service (register as singleton, build lazily and thread safely).
- Messages list module and event type names only, never a payload; errors carry ErrorCategory.FailedPrecondition.
- XML docs with examples; PublicAPI.Unshipped.txt; zero warnings; CRAP <= 10 per method.

REFERENCE FILES:
- src/Encina.DomainModeling/IIntegrationEvent.cs
- src/Encina/Modules/IModuleHandlerRegistry.cs
- src/Encina/Modules/IModuleRegistry.cs
- tests/Encina.UnitTests/DomainModeling/BoundedContextTests.cs
```

</details>

---

### Phase 2: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. Module isolation (function 11): the catalog is the read model of the event half of module isolation; it grants and denies nothing.
2. Validation (function 5): `ValidateDependencies` (D4).
3. No tenant, cache, lock or transaction code is added (see the matrix). The catalog creates no identifier, so the reserved `default` tenant segment and the injectivity rule do not apply; it is not a gate, so the fail-closed rule applies only to `ValidateDependencies`, which returns `Left` on any violation.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT:
- Issue #306; the cross-cutting matrix of the plan is the contract.

TASK:
Verify that the Include rows of the matrix exist in code and tests.

KEY RULES:
- ValidateDependencies never reports success when a violation exists.

REFERENCE FILES:
- AGENTS.md sections 3 and 6
```

</details>

---

### Phase 3: Testing

<details>
<summary>Tasks</summary>

1. **Unit, Guard, DI, Contract, Property** as listed in Phase 1. **Integration**: justification file `tests/Encina.IntegrationTests/DomainModeling/ModuleEventCatalog.md` (in-process introspection, no external dependency; the bus's outbox integration test of #358 covers the database path). **Load**: justification file `tests/Encina.LoadTests/DomainModeling/ModuleEventCatalog.md` (built once, read-only). **Benchmark**: justification file `tests/Encina.BenchmarkTests/DomainModeling/ModuleEventCatalog.md` (not a hot path).
2. **Per-file targets** for `.github/coverage-manifest/Encina.DomainModeling.json` (each flag with a one-sentence justification written in the manifest; the baseline of any edited file is measured at work start with `dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage"` and `.github/scripts/coverage-report.cs`, and a target is the measured value or higher):

   | File | Unit | Guard | Contract | Property | Integration | Justification |
   |------|:---:|:---:|:---:|:---:|:---:|---------------|
   | `ModuleEventCatalog.cs` | 95 | 80 | 90 | 80 | 0 | Every lookup and the Mermaid output run with real registries and handlers; guard tests reach the argument checks; integration 0 because the type has no external dependency |
   | `ModuleEventCatalogErrors.cs` | 100 | 0 | 100 | 0 | 0 | One factory asserted by the contract test on the real error; no parameterized public member, so guard 0 |
   | `ModuleEventCatalogServiceCollectionExtensions.cs` | 100 | 90 | 0 | 0 | 0 | The registration and its missing-registry failure are unit and guard tests; the DI test proves the provider builds |
   | `IModuleEventCatalog.cs` | 0 | 0 | 90 | 0 | 0 | An interface has no executable code for unit or guard flags; the contract test exercises it through the real implementation |

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT:
- Issue #306. Per-flag coverage model of AGENTS.md section 9: targets live in .github/coverage-manifest/Encina.DomainModeling.json.

TASK:
Write the tests and the manifest entries; the three justification files cover only load, benchmark and integration.

KEY RULES:
- Shouldly through Encina.Testing.Shouldly; tests execute real code and never swallow an exception.
- A nullable parameter that accepts null gets a valid-result test, not an exception test.
- Targets never go down.

REFERENCE FILES:
- tests/Encina.UnitTests/DomainModeling/BoundedContextTests.cs
- .github/coverage-manifest/Encina.DomainModeling.json
```

</details>

---

### Phase 4: Documentation & Finalization (pull request 2)

<details>
<summary>Tasks</summary>

1. A "Event catalog" section in the integration events guide that #358 pull request 6 creates (`docs/features/integration-events.md`), through `docs-writer`; `docs/INVENTORY.md` line.
2. `changelog.d/306-module-event-catalog.added.md` through `mechanical-fixer`.
3. `dotnet build --configuration Release` with 0 warnings; `dotnet test` with every flag at its target.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT:
- Issue #306 is implemented and tested.

TASK:
Write the documentation set of the Phase 4 tasks through the specialists that own them.

KEY RULES:
- Pages through docs-writer; changelog fragment through mechanical-fixer; never edit CHANGELOG.md; coverage figures are cited with covref markers.

REFERENCE FILES:
- changelog.d/README.md
- .claude/skills/encina-docs/SKILL.md
```

</details>

---

### Proposed split into pull requests (limit 15 files and 1500 lines each)

| PR | Content | Approx. files | Approx. lines |
|----|---------|---------------|---------------|
| 1 | Interface, implementation, errors, registration extension, PublicAPI, manifest, 5 test files, 3 justification files (`Encina.DomainModeling`) | 14 | 850 |
| 2 | Guide section, inventory line, changelog fragment | 3 | 120 |

Pull request 1 needs #358 pull requests 1 to 4 and #360 pull request 3 merged. It adds no change to an interface, so no implementer is touched.

---

## Research

### Relevant Standards & Specifications

| Reference | What it defines | Use in this feature |
|-----------|-----------------|---------------------|
| `AGENTS.md` sections 1, 3 and 6 | Opt-in features, no parallel layers, 12-function check | Registration and reuse of the validator |
| ADR-062 | `ErrorCategory` on every error code | `encina.modules.event_contract_invalid` is `FailedPrecondition` |
| Spring Modulith application module model and event publication registry | A catalog of the events a module publishes and listens to | Concept source named by the issue |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IIntegrationEvent`, `IntegrationEvent` | `src/Encina.DomainModeling/IIntegrationEvent.cs` | The event type the catalog lists |
| `IModuleRegistry`, `IModuleHandlerRegistry` | `src/Encina/Modules/` | Registered modules; handler to module |
| `IModuleGraph` (`GetExports`) | #360 | Published events |
| `ModuleContractValidator` | #358 pull request 4 | `ValidateDependencies` |
| `BoundedContextValidator` | `src/Encina.DomainModeling/BoundedContext.cs` | Parallel concept, unchanged (unification is a #358 follow-up issue file) |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| none | none | The catalog is read-only introspection and logs nothing; the validator it reuses logs under #358's `ModulesIntegrationEvents` range. Ids 7696-7699 of the assigned block 7650-7699 stay unused |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 4 | Interface, implementation, errors, registration extension |
| Public API and manifest | 2 | `Encina.DomainModeling` |
| Tests | 5 | Unit, guard, DI, contract, property |
| Justification files | 3 | Integration, load, benchmark |
| Documentation | 3 | Guide section, inventory line, changelog fragment |
| **Total** | **17 across 2 pull requests** | See the split table |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no [Obsolete], no compatibility layers.
- Issue #306 overlaps #358; this plan implements only the module event catalog. Plan: docs/plans/module-event-catalog-implementation-plan-306.md; D1, D2, D3 and D5 are recommended, pending the maintainer.

IMPLEMENTATION OVERVIEW:
1. IModuleEventCatalog, ModuleEventCatalog, the error factory and AddEncinaModuleEventCatalog in Encina.DomainModeling, with tests and the three justification files (Phases 1-3).
2. Documentation section and changelog fragment (Phase 4).

KEY PATTERNS:
- Published events are the module's exports that implement IIntegrationEvent; consumed events are the TEvent of the module's INotificationHandler<TEvent> handlers.
- ValidateDependencies reuses #358's ModuleContractValidator; an unknown module name is an ArgumentException.
- Read-only singleton, built lazily and thread safely; no scoped service in a singleton; no payload in messages.

REFERENCE FILES:
- src/Encina.DomainModeling/IIntegrationEvent.cs
- src/Encina/Modules/IModuleHandlerRegistry.cs
- .github/coverage-manifest/Encina.DomainModeling.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: the catalog is built once in memory |
| 2 | OpenTelemetry | ❌ | Not applicable: read-only introspection, no operation worth tracing; the bus traces event flow (#358) |
| 3 | Structured Logging | ❌ | Not applicable: nothing to log; the startup validator it reuses logs (#358) |
| 4 | Health Checks | ❌ | Not applicable: no dependency to check; per-module health is #364 |
| 5 | Validation | ✅ | `ValidateDependencies` (Design Choice 4) |
| 6 | Resilience | ❌ | Not applicable: no external call |
| 7 | Distributed Locks | ❌ | Not applicable: in-process read model |
| 8 | Transactions | ❌ | Not applicable: no data operation |
| 9 | Idempotency | ❌ | Not applicable: deterministic read model; consumer-side deduplication is a #358 follow-up |
| 10 | Multi-Tenancy | ❌ | Not applicable: the catalog describes code structure, not tenant data |
| 11 | Module Isolation | ✅ | Read model of publishers and consumers per module |
| 12 | Audit Trail | ⏭️ | Deferred: audit of cross-module events is the #358 follow-up issue file `module-cross-module-audit` |

---

## Prerequisites & Dependencies

- **#358 pull requests 1 to 4** (contract, bus, `ModuleContractValidator`) and **#360 pull request 3** (`IModuleGraph.GetExports`) must be merged before pull request 1 of this plan.
- #352's boundary rule and #364's per-module health are independent consumers of the same module facts.
- The relation of #306 to #358 is Design Choice 1, which only the maintainer answers.

---

## Next Steps

1. The maintainer answers D1, D2, D3 and D5 when work on #306 starts; the answers go into `## Maintainer Decisions` and the markers are removed. If D1 is answered B, this plan is closed with #306.
2. Pull request 1 after the prerequisites; pull request 2 afterwards.
