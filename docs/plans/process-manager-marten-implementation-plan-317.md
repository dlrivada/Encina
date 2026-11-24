# Implementation Plan: Process Manager Commands and the Marten Store (`Encina.ProcessManager.Marten`)

> **Issue**: [#317](https://github.com/dlrivada/Encina/issues/317)
> **Type**: Feature
> **Complexity**: Medium (8 phases on top of the #392 core, one new satellite package, about 38 files in 5 pull requests)
> **Estimated Scope**: ~900 lines of production code + ~1,800 lines of tests
> **Family**: this plan builds on the Process Manager core owned by #392 (package `Encina.ProcessManager`, `ProcessId`, `ProcessManagerBase<TState>`, `IProcessManagerStore`, routing handler) and is read together with it; #296 is the third plan of the family. Siblings are referred to by issue number only.

---

## Summary

#317 asks for a Process Manager that reacts to events from several aggregates, keeps its own state, **dispatches commands to drive the process forward**, detects completion and persists in Marten. After the #392 core, three pieces are missing, and they are this plan:

1. **Commands after an event**: a manager records the commands it wants sent (`Dispatch(command)`); the routing handler sends them through `IEncina.Send` in a defined order relative to the save.
2. **Completion detection**: the manager says when the process is finished; the handler completes it.
3. **A Marten store**: `MartenProcessManagerStore` in a new satellite package `Encina.ProcessManager.Marten`, with optimistic concurrency, tenant and correlation lookup.

**What exists today** (`origin/main` at `ff9a579c1`, 2026-11-24):

- No process manager code (`git grep -il ProcessManager origin/main -- src` is empty). `IEncina.Send<TResponse>(IRequest<TResponse>, CancellationToken)` returns `ValueTask<Either<EncinaError, TResponse>>` (`src/Encina/Abstractions/IEncina.cs`).
- `Encina.Marten` provides `IAggregateRepository`, `MartenSnapshotStore<TAggregate>` (a store over `IDocumentSession`, `src/Encina.Marten/Snapshots/MartenSnapshotStore.cs`), `EncinaMartenOptions.UseOptimisticConcurrency` (default `true`) and registers Marten configuration through `IConfigureMarten` (`EncinaStoreOptionsConfigurator`, `src/Encina.Marten/ServiceCollectionExtensions.cs` lines 47-127).
- A `MartenFixture` exists for integration tests (`tests/Encina.IntegrationTests/Compliance/BreachNotification/`, collection `Marten-PostgreSQL`).

**Corrections to the issue text** (the sketch cannot be built as written):

1. `IProcessManagerEventHandler<TProcessManager, TEvent> where TProcessManager : IProcessManager` uses a non-generic `IProcessManager` that the issue never defines; the contract is the one #392 owns.
2. `IProcessManagerStore` with `LoadAsync<TProcessManager, TState>` and `where TProcessManager : IProcessManager<TState>` repeats the type parameters on every call and cannot be called without naming both; the shared store of #392 is non-generic over the manager (`ProcessSnapshot` carries the state as serialized text).
3. `List<object> CommandsToDispatch` cannot be sent: `IEncina.Send<TResponse>` is generic over the response, so an `object` needs reflection. The manager records a typed closure instead (Design Choice 3).
4. `ProcessContext(Guid CorrelationId, IEncina Mediator, CancellationToken)` puts a mediator and a token in a record; both are handler parameters or injected services.
5. `ValueTask HandleAsync` returns nothing, so a failure would be lost; the handler returns `Either<EncinaError, Unit>` like every handler (`AGENTS.md` section 3).
6. `Guid ProcessId` is replaced by the structured, tenant-qualified `ProcessId` of #392.

**Standards**: Enterprise Integration Patterns, Process Manager. No law applies.

**Affected packages**: `Encina.ProcessManager` (additions to the #392 files, order #392 then #317) and a new `Encina.ProcessManager.Marten` (references `Encina.ProcessManager` and `Marten`).

**Provider category**: Event sourcing (Marten). It is outside the 10-database rule (`AGENTS.md` section 5, "Message brokers, caching and event sourcing (Marten) are excluded from the database rule"); the relational stores are #392's Phase 4. Per the event-sourcing rule of `AGENTS.md` section 3, unit tests substitute the store with NSubstitute and the integration tests run Marten on PostgreSQL through Testcontainers.

**Shared pieces and who owns them**: #392 owns the contract and the EventId range `ProcessManager` (12270-12289); this plan uses 12278-12283 inside it and changes `ProcessManagerBase<TState>`, the routing handler and `ProcessManagerLog` only by addition. It adds no member to `IProcessManagerStore`.

---

## Design Choices

<details>
<summary><strong>1. Where the Marten store lives (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New satellite `Encina.ProcessManager.Marten`** | The core package stays free of Marten; the Marten package stays free of the process manager; naming follows `{Feature}.{Provider}` | One more package, one more csproj and README |
| **B) Inside `Encina.Marten`** | The issue's wording ("Integration with Marten", `Encina.Marten` under "Other") | `Encina.Marten` would reference `Encina.ProcessManager`, so every Marten user takes the process manager |
| **C) Inside `Encina.ProcessManager`** | Fewest packages | The core takes a `Marten` package reference; a relational application carries it |

### Chosen Option: **A — Satellite package** (recommended, pending the maintainer)

### Rationale

- A wins on pay-for-what-you-use in both directions. B wins when the maintainer wants one Marten package for everything event-sourced. C wins only when Marten is the single supported store.
- Under B the files move to `src/Encina.Marten/ProcessManager/` and the project reference points the other way; the phases do not change.
- **Class:** Dilemma

</details>

<details>
<summary><strong>2. Commands relative to the save (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Dispatch first, then save; any `Left` from `Send` or from the save is returned to the publisher** | At-least-once with no new storage: a crash after a dispatch re-delivers the event and the commands are sent again, never lost; the state advances only after every command was accepted | Commands can be sent twice, so their handlers must be idempotent (inbox) |
| **B) Save the state with the pending commands on the process row, dispatch, then clear them; a sweep re-dispatches leftovers** | Commands are durable before any send; no duplicate on a clean path | A new hosted service, a column on every store, and a sweep that needs locks and leader election |
| **C) Write the commands to the outbox in the same unit of work as the state** | The existing reliability mechanism, atomic per provider | Needs the outbox on the Marten session (#2383 is still open), a reference to `Encina.Messaging`, and commands in an outbox that carries notifications |

### Chosen Option: **A — Dispatch, then save** (recommended, pending the maintainer)

### Rationale

- A is the smallest correct design: nothing is lost, and the duplicate-delivery guard of #392 (idempotency key on the step) stops the state from advancing twice. B wins when a command must never repeat, C when #2383 lands and exact atomicity is wanted.
- A `Left` from `Send` is returned unchanged (its category is preserved), the state is not saved and the event is retried by its publisher; no error is swallowed (`AGENTS.md` section 3).
- **Class:** Dilemma

</details>

<details>
<summary><strong>3. How a manager records a command (Trivial)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `protected void Dispatch<TResponse>(IRequest<TResponse> command)` stores a typed closure `Func<IEncina, CancellationToken, ValueTask<Either<EncinaError, Unit>>>` over `IEncina.Send`** | Type-safe; no reflection; the response is discarded explicitly | A command whose response matters is not supported (use a handler) |
| **B) `List<object>` and dispatch by reflection (the issue)** | Matches the sketch | Reflection per command; breaks trimming; a non-request object fails at run time |

### Chosen Option: **A — Typed closure**

### Rationale

`IEncina.Send` is generic over the response, so only a typed capture compiles without reflection.

- **Class:** Trivial
- **Settled by:** `src/Encina/Abstractions/IEncina.cs` (`Send<TResponse>(IRequest<TResponse>, CancellationToken)`) and `AGENTS.md` section 8 (zero warnings; the trimming and static-caching conventions of the pipeline code).

</details>

<details>
<summary><strong>4. Correlation lookup — one key, or aliases (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The correlation key is part of `ProcessId` (#392); the router of each event type names the property that holds it; "find by correlation" is `GetAsync`** | No contract change; every store already supports it; the issue's `OrderFulfillment` example carries the order id in every event | An event that only carries another id (a payment id) needs the author to put the order id on it or to write a custom router |
| **B) Secondary correlation aliases: a store method `FindByCorrelationAsync(tenant, type, key, value)` and an alias index per process** | An event with only a `PaymentId` reaches the process | A contract change after #392's eight stores exist; an alias table on every provider |

### Chosen Option: **A — Primary key only** (recommended, pending the maintainer)

### Rationale

- A wins while events carry the business key, which is the usual case for events published by the same system. B wins when a process consumes third-party events that carry only their own ids. Choosing B means the contract change is made in the #392 plan before its store pull requests.
- The issue's `FindByCorrelationAsync(string correlationKey, object correlationValue)` is satisfied for listing: `ListActiveAsync(tenant, processType, ...)` of #392 plus a Marten query on the document.
- **Class:** Dilemma

</details>

<details>
<summary><strong>5. Marten document or event-sourced process (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A Marten document per process (`MartenProcessInstance`), steps as a second document type, optimistic concurrency through `EncinaMartenOptions.UseOptimisticConcurrency`** | One read per event; matches the shared `ProcessSnapshot` contract; simple retention purge | The history is the step list, not a replayable event stream |
| **B) An event stream per process through `IAggregateRepository`; the steps are the events** | Replayable, fits compliance-style audit | The base class would need `Apply(object)` and aggregate semantics that #392's observer shape does not have; a stream per process and a projection for the dashboard |

### Chosen Option: **A — Document per process** (recommended, pending the maintainer)

### Rationale

- A wins for state that is read on every event and has no replay requirement. B wins when the maintainer needs to rebuild a process from events (for example an audit of regulated processes) and accepts a second base class.
- `MartenSnapshotStore<TAggregate>` already stores documents through `IDocumentSession`, so the pattern exists.
- **Class:** Dilemma

</details>

<details>
<summary><strong>6. Completion detection (Trivial)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `protected virtual bool IsFinished(TState state) => false;` evaluated by the routing handler after each event; when true the handler calls `Complete()` and records a `Completed` step** | The issue's `IsComplete` rule in one place; the status is stored, so a listing query needs no deserialization | One virtual method |
| **B) The author calls `Complete()` inside every handler** | Explicit | Easy to forget; two handlers can disagree |

### Chosen Option: **A — A predicate evaluated by the handler**

### Rationale

#317's acceptance criterion "process completion detection" and its sample `IsComplete => State.PaymentReceived && State.InventoryReserved && State.Shipped` are a predicate over the state; the handler is the one place that runs after every event.

- **Class:** Trivial
- **Settled by:** issue #317 (Implementation Tasks: "Add process completion detection"; usage example `IsComplete`).

</details>

---

## Implementation Phases

### Phase 1: Core Additions — Commands and Completion

> **Goal**: The manager can record commands and say it is finished (in `Encina.ProcessManager`, additions only).

<details>
<summary>Tasks</summary>

1. **`PendingCommand`** (internal sealed): wraps the closure of Design Choice 3 and the command type name (for logs; never the command content).
2. **`ProcessManagerBase<TState>`** (modified): `protected void Dispatch<TResponse>(IRequest<TResponse> command)` (null check), `internal IReadOnlyList<PendingCommand> TakePendingCommands()`, `protected virtual bool IsFinished(TState state) => false`.
3. **`ProcessManagerRoutingHandler<TEvent>`** (modified): after the manager's handler returns `Right`, take the pending commands, send each in order through `IEncina`; the first `Left` stops the loop and is returned unchanged; then evaluate `IsFinished`, call `Complete()` and `SaveAsync` (order of Design Choice 2, option A).
4. **`ProcessManagerLog`** (modified): add `[LoggerMessage]` ids 12278 (command dispatched, process type and command type name) and 12279 (command dispatch failed, error code), 12280 (process completed) inside the registered `ProcessManager` range.
5. **Tests, `PublicAPI.Unshipped.txt` and the manifest** of the package.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```
CONTEXT:
- Issue #317; plan docs/plans/process-manager-marten-implementation-plan-317.md. The #392 core exists in src/Encina.ProcessManager/. Dilemmas D1-D4 are recommended, pending the maintainer: read the Maintainer Decisions section if it exists and follow the answers.

TASK:
Implement Phase 1 Tasks: PendingCommand, Dispatch, IsFinished, the dispatch step of the routing handler, the three log messages, and their tests.

KEY RULES:
- Commands are sent before the save; the first Left from Send is returned unchanged and the state is not saved.
- No reflection: the command is captured as a typed closure over IEncina.Send.
- Log the command TYPE NAME and the error code only; never the command content or EncinaError.Message.
- EventIds 12278-12280 are inside the already registered ProcessManager range; do not add a range.

REFERENCE FILES:
- src/Encina/Abstractions/IEncina.cs
- src/Encina.ProcessManager/ProcessManagerBase.cs and ProcessManagerRoutingHandler.cs (from #392)
```

</details>

---

### Phase 2: Marten Store

> **Goal**: `IProcessManagerStore` over Marten.

<details>
<summary>Tasks</summary>

#### New project: `src/Encina.ProcessManager.Marten/` (new)

1. **Project file**: `net10.0`, references `Encina.ProcessManager` and the `Marten` package (central version), `InternalsVisibleTo` for `Encina.UnitTests`, `Encina.GuardTests`, `Encina.IntegrationTests`, `Encina.ContractTests` (as `src/Encina.Marten/Encina.Marten.csproj` lines 14-18); add to `Encina.slnx`.
2. **`MartenProcessInstance`** (internal document): `Id` (the injective string form of `ProcessId`), `TenantId`, `ProcessType`, `CorrelationKey`, `StateJson`, `Status`, `Version` (long), timestamps; **`MartenProcessStepDocument`** with a unique key `(process id, idempotency key)`.
3. **`ProcessManagerMartenConfigurator : IConfigureMarten`**: registers both documents, a unique index on `Id`, an index on `(TenantId, ProcessType, Status)` and the optimistic-concurrency setting; added with `TryAddEnumerable` like `EncinaStoreOptionsConfigurator`.
4. **`MartenProcessManagerStore : IProcessManagerStore`** (scoped; takes `IDocumentSession` and `TimeProvider`): `GetAsync`, `SaveAsync` (state and step stored in one `SaveChangesAsync` of the session, so the store's own two writes are one Marten transaction; a stale `expectedVersion` returns `processmanager.concurrency_conflict`; a duplicate idempotency key returns `Right(Unit)`), `GetStepsAsync`, `ListActiveAsync`, `ListStuckAsync`, `PurgeCompletedAsync`. Every Marten exception becomes `processmanager.store_failed` (`Unavailable`) with the exception type only.
5. **`ServiceCollectionExtensions.AddEncinaProcessManagerMarten()`**: registers the configurator and the store with `TryAdd*`; a database or in-memory store registered earlier or later does not override the application's own registration.
6. **Per-file hygiene**: `PublicAPI.Shipped.txt` (empty), `PublicAPI.Unshipped.txt`, manifest `Encina.ProcessManager.Marten.json`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```
CONTEXT:
- Issue #317, Phase 2. IProcessManagerStore (#392) has GetAsync, SaveAsync(snapshot, expectedVersion, step, idempotencyKey), GetStepsAsync, ListActiveAsync, ListStuckAsync, PurgeCompletedAsync.

TASK:
Create the satellite package, the two documents, the configurator, the store and the DI extension.

KEY RULES:
- Async with CancellationToken only; the session is injected, never IServiceProvider inside a projection.
- Tenant is a field of every query; a missing tenant with RequireTenant returns processmanager.tenant_required.
- Errors are Left; the exception type, never its message, is logged (ForLogging()).
- Unit tests substitute IDocumentSession with NSubstitute; integration tests use the Marten-PostgreSQL fixture.

REFERENCE FILES:
- src/Encina.Marten/Snapshots/MartenSnapshotStore.cs
- src/Encina.Marten/EncinaStoreOptionsConfigurator.cs
- src/Encina.Marten/ServiceCollectionExtensions.cs
- tests/Encina.IntegrationTests/Compliance/BreachNotification/BreachNotificationAggregateIntegrationTests.cs
```

</details>

---

### Phase 3: Configuration & DI

> **Goal**: One extension wires the store; registration order cannot lose the application's own store.

<details>
<summary>Tasks</summary>

1. `AddEncinaProcessManagerMarten()` is called before or after `AddEncinaProcessManager()`; the Marten store wins over the in-memory default through `TryAdd*` ordering rules of `AGENTS.md` section 3 (a database store wins in any order without overriding the application's registration).
2. A DI test builds the provider with `ValidateOnBuild` and `ValidateScopes`: the scoped store depends on the scoped `IDocumentSession`; no scoped service is injected into a singleton; the routing handler resolves the store from its own scope.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```
CONTEXT:
- Issue #317, Phase 3. The store of Phase 2 exists.

TASK:
Finish the registration and add the DI proof test for both registration orders and for an application-supplied store.

KEY RULES:
- TryAdd* only; the test fails if the application's own IProcessManagerStore is replaced.
- ValidateOnBuild and ValidateScopes are on in the test.

REFERENCE FILES:
- src/Encina.Marten/ServiceCollectionExtensions.cs
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs
```

</details>

---

### Phase 4: Cross-Cutting Integration

> **Goal**: Apply the matrix items marked integrate.

<details>
<summary>Tasks</summary>

1. **Idempotency**: the step document key `(process id, idempotency key)` is the duplicate guard of #392; a duplicate returns `Right(Unit)`.
2. **Validation**: a command that is `null` is rejected by `Dispatch`; the options of #392 are validated at startup.
3. **Multi-tenancy**: `TenantId` is a document field and in every Marten query; fail closed with `RequireTenant`.
4. **Audit trail**: the step list; the operation audit is the deferred issue of #392.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```
CONTEXT:
- Issue #317, Phase 4.

TASK:
Add the duplicate-delivery integration test, the tenant isolation test (two tenants, same process type and key never see each other) and the Dispatch argument checks.

KEY RULES:
- Two tenants with the same (type, key) are two different processes; the test proves it on Marten.
- Missing tenant with RequireTenant returns processmanager.tenant_required.

REFERENCE FILES:
- src/Encina.Tenancy/TenancyErrors.cs
```

</details>

---

### Phase 5: Observability

> **Goal**: Log, trace and measure the new paths inside the family range.

<details>
<summary>Tasks</summary>

1. **EventIds** (inside the range registered by #392, `ProcessManager` 12270-12289; this plan 12278-12283, packed): 12278 command dispatched, 12279 command dispatch failed, 12280 process completed (Phase 1), 12281 Marten store failure, 12282 Marten concurrency conflict, 12283 Marten configured. Map `Encina.ProcessManager.Marten` to the range in `AssemblyRanges` of `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`.
2. **Metrics** (the `Encina.ProcessManager` meter of #392): `Counter<long> encina.process_manager.commands_dispatched` (tags process type, outcome); no `_total` suffix; the tenant tag only when multi-tenancy is on (ADR-054 D4); never the correlation key.
3. **Spans**: a child span `encina.process_manager.dispatch_command` with the command type name.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```
CONTEXT:
- Issue #317, Phase 5. The range ProcessManager 12270-12289 is registered by #392; this plan owns 12278-12283.

TASK:
Add the Marten log class (ids 12281-12283), the command counter and span, and the AssemblyRanges entry; run the EventId architecture tests.

KEY RULES:
- Ids packed without gaps inside 12278-12283; no id outside the registered range.
- No EncinaError.Message, correlation key or process id in a log, tag or span attribute.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- docs/engineering/eventid-block-registry.md
- src/Encina.Messaging/Diagnostics/MessagingStoreMetrics.cs
```

</details>

---

### Phase 6: Testing

> **Goal**: Every flag at its target.

<details>
<summary>Tasks</summary>

| Flag | Scope | Per-file target (justification) |
|------|-------|---------------------------------|
| Unit | `Dispatch`, `PendingCommand`, the handler's dispatch step, the Marten store with a substituted session | `ProcessManagerBase.cs` 90, `MartenProcessManagerStore.cs` 75 (the session is substituted; real paths are integration), `ProcessManagerMartenConfigurator.cs` 85 |
| Guard | `Dispatch(null)`, store and extension arguments | `MartenProcessManagerStore.cs` 85, `ServiceCollectionExtensions.cs` 90 (null and empty arguments are the only branches) |
| Contract | `IProcessManagerStore` contract run against the Marten store | `MartenProcessManagerStore.cs` 60 (the contract exercises save, conflict, list, purge) |
| Property | none beyond #392 | no new logic with an invariant of its own; justification file |
| Integration | Marten on PostgreSQL through the shared `Marten-PostgreSQL` fixture: save, conflict, tenant isolation, duplicate, purge | `MartenProcessManagerStore.cs` 70 (real session and database) |

Load and benchmark: a justification `.md` each (one load and one save per event; concurrency is the version check, covered by integration).

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```
CONTEXT:
- Issue #317, Phase 6.

TASK:
Write the tests of the table, the two justification files and add the project references the new tests need.

KEY RULES:
- Tests execute real code; Shouldly through Encina.Testing.Shouldly; no Thread.Sleep.
- Integration tests use the shared Marten-PostgreSQL collection and clear data in InitializeAsync; never IClassFixture.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Compliance/BreachNotification/BreachNotificationAggregateIntegrationTests.cs
- .github/coverage-manifest/Encina.Marten.json
```

</details>

---

### Phase 7: Documentation & Finalization

> **Goal**: The mandatory documentation set.

<details>
<summary>Tasks</summary>

1. XML docs on every public member; `changelog.d/317-process-manager-commands-marten.added.md`.
2. `src/Encina.ProcessManager.Marten/README.md`; a section in the #392 feature guide on commands and completion; `docs/INVENTORY.md`; `ROADMAP.md`.
3. `dotnet build --configuration Release` with zero warnings; per-flag coverage measured against the manifest.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```
CONTEXT:
- Issue #317, Phase 7. Delegated to the docs-writer agent for pages under docs/ and READMEs.

TASK:
Write the README, the guide section, the changelog fragment and the INVENTORY and ROADMAP lines.

KEY RULES:
- No hand-typed coverage figures; cite cov: references.
- Public text never names a private client application.

REFERENCE FILES:
- docs/plans/dsr-implementation-plan-404.md
- changelog.d/README.md
```

</details>

---

### Proposed split into pull requests (limit 15 files and 1500 lines each)

| PR | Content | Files | Lines |
|----|---------|-------|-------|
| 1 | Phase 1: `PendingCommand`, `ProcessManagerBase`, routing handler, `ProcessManagerLog` (4 modified or new), PublicAPI, manifest, 2 unit tests, 1 guard test | 9 | 700 |
| 2 | Phase 2 package and store: csproj, `Encina.slnx`, 2 documents, configurator, store, DI extension (6), 2 PublicAPI, manifest, `UnitTests` and `GuardTests` references (already present from #392 PR 1), unit and guard tests (2) | 12 | 1,100 |
| 3 | Phases 3-5: registration order test, Marten log class, `AssemblyRanges` entry, command counter and span (2 source), 2 unit tests, PublicAPI | 8 | 600 |
| 4 | Phase 6 integration: 4 integration classes on `Marten-PostgreSQL` (save and conflict, tenant isolation, duplicate, purge), contract registration, `ContractTests` reference if absent, justification files (2) | 9 | 900 |
| 5 | Phase 7 documentation: README, guide section, INVENTORY, ROADMAP, changelog fragment | 5 | 400 |

Total: 5 pull requests, 43 files (upper estimate); no pull request exceeds 15 files or 1,500 lines. Pull request 1 needs the #392 pull requests 1 and 3 merged; pull request 2 needs #392 pull request 1.

---

## Research

### Relevant Standards & Specifications

| Reference | What it defines | Use in this feature |
|-----------|-----------------|---------------------|
| Enterprise Integration Patterns, Process Manager | State plus routing of messages through a process | Pattern semantics |
| Saga versus Process Manager (the issue's comparison table) | Duration, compensation and trigger differences | Positioning in the guide |
| ADR-001, ADR-006 | Railway Oriented Programming | `Left` for every failure |
| ADR-018, ADR-021, ADR-052 | Cross-cutting check, EventIds, lock contract | Matrix, range, and why locks are deferred |
| ADR-054 D4, ADR-062 | Tenant tag, `ErrorCategory` | Telemetry, error categories |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IEncina.Send<TResponse>` | `src/Encina/Abstractions/IEncina.cs` | The only way to send a command; captured as a typed closure |
| `MartenSnapshotStore<TAggregate>` | `src/Encina.Marten/Snapshots/MartenSnapshotStore.cs` | Pattern for a Marten document store over `IDocumentSession` |
| `EncinaStoreOptionsConfigurator`, `IConfigureMarten` | `src/Encina.Marten/EncinaStoreOptionsConfigurator.cs` | Pattern for registering documents and indexes |
| `EncinaMartenOptions.UseOptimisticConcurrency` | `src/Encina.Marten/EncinaMartenOptions.cs` | Optimistic concurrency switch (default true) |
| `MartenFixture` | `tests/Encina.IntegrationTests/Compliance/BreachNotification/` | Shared PostgreSQL Marten fixture for integration tests |
| `TenancyErrors` | `src/Encina.Tenancy/TenancyErrors.cs` | `tenancy.tenant_required` is `InvalidRequest`; mirrored by #392 |
| `ProcessManagerBase`, `IProcessManagerStore`, routing handler | `src/Encina.ProcessManager/` (after #392) | The shared core this plan extends |
| `EventIdRanges` | `src/Encina/Diagnostics/EventIdRanges.cs` | Registry; the range is registered by #392 |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Marten` | 2600-2799 | Existing; not used by this plan |
| `Encina.ProcessManager` (#392) | 12270-12277 | Registered by #392 as `ProcessManager = (12270, 12289)` |
| **`Encina.ProcessManager` and `Encina.ProcessManager.Marten` (this plan)** | **12278-12283** | **Inside the same registered field; 12278-12280 commands and completion, 12281-12283 Marten store** |
| #296 | 12284-12289 | Decision-driven definitions |

The block 12250-12299 is the first free 50-id slot of the Core and messaging area after the #4007 reservations and the #340 claim (12200-12249); 12250-12269 belongs to #128. No new field is registered by this plan.

### Provider Matrix

| Provider category | Applies | Reason |
|-------------------|---------|--------|
| Event sourcing (Marten) | Yes | The store of this plan |
| Database (10) | No (#392) | The ten-provider stores are #392's Phase 4 |
| Caching, Transport, Locks, Validation, Cloud | No | None |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Core additions | ~5 | Modified or new files in `Encina.ProcessManager` |
| Marten package source | ~12 | csproj, documents, configurator, store, DI, log, PublicAPI x2, manifest |
| Tests | ~19 | Unit, guard, contract, DI, integration x4, justification x2 |
| Registry and registrations | ~2 | `Encina.slnx`, `AssemblyRanges` test |
| Documentation | ~5 | README, guide section, INVENTORY, ROADMAP, changelog fragment |
| **Total** | **~43 in 5 pull requests** | Equals the sum of the split table |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Railway Oriented Programming (Either<EncinaError, T>); no [Obsolete]; XML docs on every public API; zero warnings.
- Issue #317: commands after events, completion detection and a Marten store for the Process Manager core of #392. Plan: docs/plans/process-manager-marten-implementation-plan-317.md. Dilemmas D1-D4 are recommended, pending the maintainer; read the Maintainer Decisions section first if it exists.

IMPLEMENTATION OVERVIEW:
1. Phase 1: Dispatch (typed closure over IEncina.Send), IsFinished, dispatch step in the routing handler.
2. Phase 2: new package Encina.ProcessManager.Marten with the documents, configurator and store. Phase 3: DI and its proof.
3. Phase 4: idempotency, tenant isolation. Phase 5: EventIds 12278-12283, counter, span.
4. Phase 6: tests. Phase 7: documentation.

KEY PATTERNS:
- Commands are sent before the save; the first Left is returned unchanged; the state is not saved on a Left.
- No reflection for dispatch; no EncinaError.Message, correlation key, process id or command content in logs, tags or spans.
- Marten unit tests substitute the session (NSubstitute); integration tests run Marten on PostgreSQL through Testcontainers and the shared fixture.
- TryAdd* registrations; DI test with ValidateOnBuild and ValidateScopes.

REFERENCE FILES:
- src/Encina/Abstractions/IEncina.cs
- src/Encina.Marten/Snapshots/MartenSnapshotStore.cs
- src/Encina.Marten/ServiceCollectionExtensions.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- docs/plans/dsr-implementation-plan-404.md (style)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: process state is written on every event; a cached snapshot would serve stale state to the next writer (the issue marks it not applicable) |
| 2 | OpenTelemetry | ✅ | Phase 5: command counter and span in the `Encina.ProcessManager` source and meter; tenant tag only when multi-tenancy is on (ADR-054 D4) |
| 3 | Structured Logging | ✅ | Phase 5: ids 12278-12283 of the registered range; type names and error codes only |
| 4 | Health Checks | ⏭️ | Deferred to #392 Phase 5 (`ProcessManagerHealthCheck` over `ListStuckAsync`, which the Marten store implements) |
| 5 | Validation | ✅ | Phase 4: `Dispatch` rejects `null`; incoming events are validated by #392's correlation check |
| 6 | Resilience | ❌ | Not applicable: the retry of a failed command dispatch belongs to the publisher, which receives the `Left` (Design Choice 2); a Polly policy belongs to the command's own handler |
| 7 | Distributed Locks | ⏭️ | Deferred with #392 (optimistic concurrency is integrated; an opt-in lock is the follow-up issue of #392) |
| 8 | Transactions | ✅ | Phase 2: the state document and the step document are stored by one Marten `SaveChangesAsync` of the session; no atomicity with the sent commands is claimed (Design Choice 2) |
| 9 | Idempotency | ✅ | Phase 4: unique `(process id, idempotency key)` on the step document; commands may repeat, so their handlers must be idempotent (documented) |
| 10 | Multi-Tenancy | ✅ | Phase 4: `TenantId` on the document and in every query; fail closed with `RequireTenant`; the issue deferred it, but the shared `ProcessId` is tenant-qualified |
| 11 | Module Isolation | ⏭️ | Deferred: `ModuleId` on messaging entities is #747; the document inherits it when #747 lands |
| 12 | Audit Trail | ✅ | The step documents are the process audit trail (issue: "Audit process state transitions"); the operation audit store is the deferred issue of #392 |

---

## Prerequisites & Dependencies

- **#392** (the core): this plan edits its types; pull request 1 of this plan follows #392 pull requests 1 and 3, and its Marten store follows the store contract of #392 pull request 1.
- #2383 (outbox in the Marten session) is the prerequisite of the alternative in Design Choice 2, option C; it does not block this plan.
- No other blocking issue.

---

## Next Steps

1. The maintainer answers Dilemmas D1 to D4 when work on #317 starts; the answers go into `## Maintainer Decisions` at the end of this plan.
2. Merge the #392 plan first; then brief pull request 1 of this plan.
3. If Design Choice 4 chooses aliases, change the #392 contract before its store pull requests.
