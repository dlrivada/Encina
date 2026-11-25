# Implementation Plan: `Encina.ProcessManager` — Event-Reacting Process Manager with Routing, Audit Trail and Dashboard

> **Issue**: [#392](https://github.com/dlrivada/Encina/issues/392)
> **Type**: Feature
> **Complexity**: High (new package, ten database providers, 8 phases, about 162 files in 14 pull requests)
> **Estimated Scope**: ~1,500 lines of production code in the core package + ~1,000 lines per provider family + ~3,500 lines of tests
> **Family**: this plan OWNS the shared core of the Process Manager family (#392, #317, #296): the package, the identity, the store contract, the routing and the EventId range. #317 (Marten store, commands) and #296 (decision-driven definitions) build on it and are referred to by issue number only.

---

## Summary

A Process Manager keeps the state of one long-running business process (an order from placement to delivery) while it **reacts** to notifications published by other parts of the system. Unlike a saga, it does not command the participants or compensate; it observes, records every step in an audit trail and answers "where is process X?". #392 delivers the part that makes this possible for any handler author: a base class for the state, a correlation router that finds the right process for an event, a store contract, an automatic routing handler on the existing notification dispatch, an audit trail of steps, and a dashboard query service.

**What exists today** (checked on `origin/main` at `f2a0692ed`, 2026-11-24):

- There is no `Encina.ProcessManager` package and no `ProcessManager` type anywhere in `src/` (`git grep -il ProcessManager origin/main -- src` returns nothing).
- Notifications are dispatched by `Encina.NotificationDispatcher`, which resolves `IEnumerable<INotificationHandler<T>>` (`src/Encina/Dispatchers/Encina.NotificationDispatcher.cs`, line 63). **There is no notification pipeline behavior type**: `INotificationPipelineBehavior<T>`, used by the issue's `ProcessManagerRoutingBehavior`, does not exist anywhere in `src/` (`git grep INotificationPipelineBehavior` is empty). The routing must be an `INotificationHandler<>`; `src/Encina.SignalR/ServiceCollectionExtensions.cs` line 115 already registers an open generic `INotificationHandler<>` (`SignalRBroadcastHandler<>`), so the mechanism is proven.
- The closest existing pattern is choreography: `IChoreographySaga<TState>` with `IChoreographyStateStore` and an event history keyed by a `string` correlation id (`src/Encina.Messaging/Choreography/`). It has no router, no tenant, no audit trail type and no dashboard.
- `SagaOrchestrator` and `ISagaStore` (nine implementers: ADO.NET x3, Dapper x3, EF Core, MongoDB, in-memory) are the model for a persisted, provider-coherent store (`AGENTS.md` section 5).

**Corrections to the issue text** (the sketch cannot be built as written):

1. `INotificationPipelineBehavior` does not exist (above); a routing `INotificationHandler<>` replaces `ProcessManagerRoutingBehavior`.
2. `ProcessStep.Description` and `Metadata` (`Dictionary<string, object>`) are replaced by `EventType`, `Outcome` and `ErrorCode` (Design Choice 5).
3. `ProcessId.From($"order-{e.OrderId}")` is not injective across tenants; `ProcessId` is a structured key (Design Choice 4).
4. The `Handle` examples mix `async` and `Unit.ValueTask`; the real handler contract is `Task<Either<EncinaError, Unit>> Handle(TNotification, CancellationToken)` (`src/Encina/Abstractions/INotificationHandler.cs`).
5. The `[CorrelateBy(nameof(OrderId))]` attribute is read once per event type and cached, not reflected per event.

**Standards and references**: Enterprise Integration Patterns, Process Manager; the Oskar Dudycz comparison named in #128. No law applies.

**Affected packages**: a new `Encina.ProcessManager` (references only `Encina` core), plus one store class in each of `Encina.ADO.SqlServer|PostgreSQL|MySQL`, `Encina.Dapper.SqlServer|PostgreSQL|MySQL`, `Encina.EntityFrameworkCore` and `Encina.MongoDB` (Design Choice 3). #317 adds `Encina.ProcessManager.Marten`; #296 adds a definition layer inside `Encina.ProcessManager`.

**Provider category**: Database (10) for the durable store, as for the saga store. A process store holds state, so it follows `AGENTS.md` section 5; Marten is event sourcing and sits outside the database rule (#317).

**Shared pieces and who owns them**: this plan owns `ProcessId`, `ProcessStatus`, `ProcessStep`, `ProcessManagerBase<TState>`, `IProcessManagerStore`, `IProcessManagerRouter`, the routing handler, `ProcessManagerErrorCodes`, the `processmanager.*` codes and the EventId range `ProcessManager` (12270-12289), `ProcessManagerLog`, the activity source and the meter. #317 adds command dispatch (members on `ProcessManagerBase<TState>` and a post-save step in the routing handler) and takes 12278-12283; #296 adds `ProcessManagerDefinition<TData>` and takes 12284-12289. This plan uses 12270-12277. The three plans never edit the same method in the same pull request; they merge in the order #392, #317, #296.

**Which pull request lands what** (the sibling plans read this): pull request 1 lands the contract (`ProcessId`, `ProcessStatus`, `ProcessStep`, `ProcessManagerBase<TState>`, `IProcessManagerStore`, `ProcessManagerErrorCodes`); **pull request 2 lands the range registration `ProcessManager = (12270, 12289)`, `ProcessManagerLog`, `ProcessManagerActivitySource` and `ProcessManagerMetrics`**, so that #317 and #296 add their log entries by addition to a class that already exists, and pull request 3 onward can log; pull request 3 lands the router, the in-memory store and the options; pull request 4 lands the routing handler and the registration. A sibling pull request that needs `ProcessManagerLog` starts after pull request 2 merges.

---

## Design Choices

<details>
<summary><strong>1. Package placement — a new package (Settled)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New package `Encina.ProcessManager`** | Pay-for-what-you-use at the package level; no messaging stack needed by an application that only wants process state; matches the issue | One more package to document and version |
| **B) Inside `Encina.Messaging` next to `Choreography/`** | Reuses the messaging project reference | Every messaging consumer carries the process manager; the pattern needs no outbox, inbox or saga |

### Chosen Option: **A — New package `Encina.ProcessManager`**

### Rationale

The issue's "Affected Packages" names `Encina.ProcessManager (new package)` and #317 names the same package, so the placement is already specified; `AGENTS.md` section 1 ("every feature opt-in") agrees. The package references only `Encina` core (`INotificationHandler<>`, `IEncina`, `Either`).

- **Class:** Settled
- **Settled by:** issue #392 (Affected Packages: "Encina.ProcessManager (new package)"), issue #317 (Affected Packages), `AGENTS.md` section 1 (pay-for-what-you-use).

</details>

<details>
<summary><strong>2. Which issue sketch is the shared core (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) #392 owns the core (handler-driven, observes events, owns routing, audit trail and dashboard); #317 adds commands and the Marten store; #296 adds decision-driven definitions** | One `IProcessManager`, one `IProcessManagerStore`, one router; the three issues stop defining the same type names with incompatible shapes | #317 and #296 are written as additions, not as the standalone designs their issues sketch |
| **B) #317 owns the core (apply events, collect commands, Marten first)** | Command dispatch is part of the base from day one; fits event-sourced processes | The base class assumes `Apply(object)` and `CommandsToDispatch`, which a pure observer (#392) does not need; Marten becomes the reference store, not the ten providers |
| **C) Three independent designs, the maintainer implements one** | Each issue is plannable alone | Three incompatible `IProcessManagerStore` signatures in one package; two plans are wasted |

### Chosen Option: **A — #392 owns the core** (recommended, pending the maintainer)

### Rationale

- A wins when the three issues are one pattern with three views (observe, react-and-command, decide), which their texts say: #296 names #317 as "Alternative", #392 names "Saga Pattern (existing)" and Choreography as its comparison. B wins when the first customer is an event-sourced Marten application whose processes are driven by commands.
- The plans are written for A. Under B the file inventory of Phase 1 moves to the #317 plan and this plan becomes an addition (routing, audit trail, dashboard).
- **Class:** Dilemma

</details>

<details>
<summary><strong>3. Durable store scope — in-memory plus all ten providers, or fewer (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) In-memory (default, tests) plus a store in each of the eight provider packages (ADO x3, Dapper x3, EF Core, MongoDB)** | Complies with `AGENTS.md` section 5 for a feature that persists state; switching provider changes only the DI registration | Eight store pull requests, three schema scripts per ADO/Dapper family, integration tests on real databases |
| **B) In-memory plus Marten only (#317)** | One store; event-sourced applications are served | A relational application has no durable process state; violates the 10-provider rule unless the feature is declared event sourcing |
| **C) In-memory plus EF Core only, others deferred** | Smallest durable scope | Same rule violation for seven providers |

### Chosen Option: **A — In-memory plus eight provider stores** (recommended, pending the maintainer)

### Rationale

- A wins because the process row is state like the saga row, and `AGENTS.md` section 5 requires "every provider-dependent feature" on all ten providers. B or C win when the maintainer wants a first release quickly and accepts the gap with a recorded follow-up.
- The contract is written so a store is small: `GetAsync`, `SaveAsync` (state, expected version and the step in one call), `GetStepsAsync`, `ListActiveAsync`, `ListStuckAsync`, `PurgeCompletedAsync`.
- **Class:** Dilemma

</details>

<details>
<summary><strong>4. ProcessId — a structured, tenant-qualified key (Trivial)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ProcessId` is a `readonly record struct (string? TenantId, string ProcessType, string CorrelationKey)`; its string form length-prefixes every segment (`<len>:<tenant>\|<len>:<type>\|<len>:<key>`); `default` is the reserved tenant segment of a single-tenant host** | Injective: no two (tenant, type, key) triples give one string; stores keep the three columns separately | A little ceremony versus a plain string |
| **B) A plain string such as `order-{id}` (the issue's sketch)** | Simplest | `order-1` of tenant A and `order-1` of tenant B collide; a key containing the separator collides with another type |

### Chosen Option: **A — Structured key with a length-prefixed string form**

### Rationale

The store addresses rows by the three components, so the string form is only for dictionary keys in the in-memory store and never for logs. `default` is the reserved tenant segment of a single-tenant host: `ProcessId.Create` rejects a real tenant id equal to `default` (ordinal, case-insensitive) with `processmanager.invalid_correlation`, so a single-tenant row and a tenant row never share an id. A missing tenant returns `processmanager.tenant_required` unless the host has set the explicit opt-out `ProcessManagerOptions.AllowMissingTenant = true` (single-tenant host, default `false`, fail closed; each opted-out event is logged, Phase 7).

- **Class:** Trivial
- **Settled by:** `AGENTS.md` section 3 (compliance and security gates fail closed), ADR-054 D4 (tenant as an attribute when multi-tenancy is on), and the tenant-on-the-row precedent of #738.

</details>

<details>
<summary><strong>5. What a ProcessStep records (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ProcessStep(long Sequence, int Ordinal, string EventType, DateTime OccurredAtUtc, string Outcome, string? ErrorCode)`** | Nothing free-form reaches storage: no personal data, no `EncinaError.Message`; cheap to store on every provider | An operator cannot read a sentence such as "Payment 17 confirmed" |
| **B) The issue's `Description` and `Metadata` dictionary** | Human-readable timeline | The author's text and an `object` dictionary can hold personal data and reach plaintext storage; `Dictionary<string, object>` needs a serializer per provider |
| **C) A, plus an optional `string? Detail` the author sets, documented as non-personal** | Readable when the author is careful | One more field that can leak when misused |

### Chosen Option: **A — Event type, outcome and error code only** (recommended, pending the maintainer)

### Rationale

A wins because `AGENTS.md` section 3 forbids `EncinaError.Message` in plaintext storage and the step table is the audit trail of a possibly regulated process. B wins for internal tools with no personal data and a strong need for readable history; C is the compromise when authors are trusted. The dashboard shows the event type name and the outcome, which is enough to locate a stuck process.

- **Class:** Dilemma

</details>

<details>
<summary><strong>6. Concurrent events for one process (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Optimistic concurrency: `SaveAsync(snapshot, expectedVersion)` fails with `processmanager.concurrency_conflict`; the routing handler returns that `Left` so the publisher retries** | Correct on every provider with no lock infrastructure; the version is already a field of the base class | A hot process retries under contention |
| **B) A per-process distributed lock (`IDistributedLockProvider.TryAcquireAsync`) around load-handle-save** | No retries for hot processes | A new package reference; `TryAcquireAsync` returns `null` on contention (ADR-052), so a "busy" `Left` is still needed; no store-level protection if the lock is lost |
| **C) Both, the lock opt-in** | Best of both | Two mechanisms to test and document |

### Chosen Option: **A — Optimistic concurrency** (recommended, pending the maintainer)

### Rationale

A is required in every option (a lock can expire mid-handler, ADR-052); B and C add a lock on top. C wins when processes are hot and handlers are expensive. The lock stays an opt-in follow-up (`artifacts/issues/process-manager-distributed-lock.md`), so this plan integrates the version check only.

- **Class:** Dilemma

</details>

<details>
<summary><strong>7. Failures are Left values, and the routing handler never swallows one (Settled)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Every store, router and manager failure is a `Left`; the routing handler returns it to the dispatcher** | The publisher (outbox, inbox, retry) sees the failure | A failing process blocks nothing else because handlers are independent |
| **B) The handler logs and returns `Right` | The publisher is never retried | The event is silently lost: forbidden |

### Chosen Option: **A — `Left` all the way**

### Rationale

The handler contract returns `Either<EncinaError, Unit>`; a `Left` inside a handler is a failed operation, not a success.

- **Class:** Settled
- **Settled by:** `AGENTS.md` section 3 (errors are never swallowed in background infrastructure; Railway Oriented Programming), ADR-001, ADR-006.

</details>

<details>
<summary><strong>8. Who commits the write on each provider (Dilemma D5)</strong></summary>

### Options Considered

Evidence: the caller of the routing handler is `NotificationDispatcher`, which never calls `SaveChangesAsync` and has no unit-of-work behavior (`src/Encina/Dispatchers/Encina.NotificationDispatcher.cs`). The saga, outbox, inbox and dead-letter stores expose `SaveChangesAsync` on the contract (`ISagaStore.cs` line 76, `SagaStoreEF.cs` line 128, `OutboxStoreEF.cs` line 222, `IDeadLetterStore.cs` line 161) because an orchestrator (`DeadLetterOrchestrator.cs` line 352) stages several changes and commits once; for a request the commit is the transaction behavior (`TransactionPipelineBehavior.CommitAsync`), which a notification does not pass through.

| Option | Pros | Cons |
|--------|------|------|
| **A) `SaveAsync` commits by itself on every provider: ADO.NET and Dapper in one provider transaction, EF Core calls `SaveChangesAsync` inside `SaveAsync` (state and step in one call), MongoDB and Marten (#317) commit in their own call** | One behavior on all providers and Marten; no member added to the contract (#317 and #296 call `SaveAsync` unchanged); "state and step in one call" is true everywhere | An application that wants the process write in its own `DbContext` transaction cannot enlist it through this store; it enlists through an ambient transaction on the connection, which EF Core honors |
| **B) `IProcessManagerStore.SaveChangesAsync` as in `ISagaStore`; the routing handler calls it after `SaveAsync`** | Matches the saga family; the EF `DbContext` can be shared with application changes | A second call that every consumer must remember; ADO.NET, Dapper and MongoDB implement it as a no-op while Marten commits in `SaveAsync`, so the members mean different things per provider; a failure between the two calls loses the step |
| **C) The routing handler opens a unit of work (`IUnitOfWork`) around load-handle-save** | One commit owner for any store | Pulls the transaction module into the package (pay-for-what-you-use), and MongoDB and Marten do not take part in it |

### Chosen Option: **A — The store commits inside `SaveAsync`** (recommended, pending the maintainer)

### Rationale

A is the only option in which the contract means the same on all nine stores and the event is durable when the handler returns `Right`; the routing handler is the orchestrator and it stages one write, so the saga reason for a separate commit (several stages, one commit) does not apply. B wins when the maintainer wants application and process rows committed together through one shared `DbContext`; the plan then adds the member and the handler call in pull request 4 and the EF store stages only. Phase 4 and matrix item 8 are written for A.

- **Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Core Models, Errors & Contract

> **Goal**: The public contract of the new package, with no behavior beyond identity and the base class.

<details>
<summary>Tasks</summary>

#### New project: `src/Encina.ProcessManager/` (new)

1. **Project file** `Encina.ProcessManager.csproj`: `net10.0`, nullable, XML docs, `Microsoft.CodeAnalysis.PublicApiAnalyzers`; references `Encina` core and `LanguageExt.Core`; layout copied from `src/Encina.Hangfire/Encina.Hangfire.csproj`, plus `InternalsVisibleTo` items for `Encina.UnitTests`, `Encina.GuardTests`, `Encina.PropertyTests`, `Encina.ContractTests` and `Encina.IntegrationTests` (as `src/Encina.Messaging/Encina.Messaging.csproj` declares; `Encina.Hangfire.csproj` has none) so the internal `ProcessManagerErrors`, `ProcessManagerLog` and the later internals of #317 and #296 are testable. Add it to `Encina.slnx`.
2. **`ProcessId`**: `readonly record struct (string? TenantId, string ProcessType, string CorrelationKey)`; `static Either<EncinaError, ProcessId> Create(string? tenantId, string processType, string correlationKey)` rejecting empty or whitespace type and key and a tenant id equal to the reserved segment `default`; `override string ToString()` returns the length-prefixed form of Design Choice 4 and never the raw key in a log (callers log the process type only).
3. **`ProcessStatus`**: `NotStarted = 0, InProgress = 1, Completed = 2, Failed = 3, Cancelled = 4` (the issue's values).
4. **`ProcessStep`**: sealed record `(long Sequence, int Ordinal, string EventType, DateTime OccurredAtUtc, string Outcome, string? ErrorCode)` (Design Choice 5). `Sequence` is assigned by the store and orders the steps of a process; `Ordinal` is the position of the step within the handling of ONE event (0 for the event's own step; the decision engine of #296 and the commands of #317 append 1, 2, ... for the further steps they save for the same event). `Ordinal` is what lets several steps share one idempotency key (task 6, Phase 6).
5. **`ProcessManagerBase<TState>`** (`where TState : class, new()`): `ProcessId Id`, `TState State`, `ProcessStatus Status`, `long Version`; `protected void Start()`, `Complete()`, `Fail(string errorCode)`, `Cancel()`; a status change on a terminal process returns `processmanager.invalid_status` through `Either`.
6. **`IProcessManagerStore`**: `Task<Either<EncinaError, Option<ProcessSnapshot>>> GetAsync(ProcessId, CT)`; `Task<Either<EncinaError, Unit>> SaveAsync(ProcessSnapshot snapshot, long expectedVersion, ProcessStep step, string? idempotencyKey, CT)` (state and step in one call and committed by the call itself, Design Choice 8; an already recorded `(idempotencyKey, step.Ordinal)` pair of that process returns `Right(Unit)` without a second step and without applying the snapshot, while another ordinal under the same key is persisted); `GetStepsAsync(ProcessId, CT)`; `ListActiveAsync(string? tenantId, string? processType, int pageSize, string? after, CT)`; `ListStuckAsync(TimeSpan olderThan, int batchSize, CT)`; `PurgeCompletedAsync(DateTime cutoffUtc, int batchSize, CT)`. `ProcessSnapshot` is declared in the same file `IProcessManagerStore.cs` as a sealed record `(ProcessId Id, string StateJson, ProcessStatus Status, long Version, DateTime StartedAtUtc, DateTime LastUpdatedAtUtc, DateTime? CompletedAtUtc)` declared with the interface; state is serialized through `IMessageSerializer`-style injection in the stores, never by the interface.
7. **`ProcessManagerErrorCodes`** (public static) and internal **`ProcessManagerErrors`**, both in the one file `ProcessManagerErrors.cs`, over `EncinaErrors.Create(code, ErrorCategory, message)`. Codes and ADR-062 categories: `processmanager.not_found` = `NotFound` (a process addressed by a caller-supplied id, rule R1); `processmanager.concurrency_conflict` = `Conflict`; `processmanager.invalid_status` = `FailedPrecondition`; `processmanager.tenant_required` = `InvalidRequest` (mirrors `tenancy.tenant_required`); `processmanager.invalid_correlation` = `InvalidRequest`; `processmanager.store_failed` = `Unavailable`; `processmanager.state_deserialization_failed` = `Internal`. Messages name codes and types only.
8. **`PublicAPI.Shipped.txt`** (empty) and **`PublicAPI.Unshipped.txt`** with every public symbol; the manifest `.github/coverage-manifest/Encina.ProcessManager.json`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```
CONTEXT:
- Encina is a .NET 10 / C# 14 library using Railway Oriented Programming (Either<EncinaError, T>).
- This phase creates the NEW project src/Encina.ProcessManager/ with the public contract only.
- Issue #392; plan docs/plans/process-manager-implementation-plan-392.md. Dilemmas D1-D5 are recommended, pending the maintainer: read the Maintainer Decisions section if it exists and follow the answers.

TASK:
Create the project, ProcessId, ProcessStatus, ProcessStep, ProcessManagerBase<TState>, IProcessManagerStore with ProcessSnapshot, the error codes, the PublicAPI files and the coverage manifest listed in Phase 1 Tasks.

KEY RULES:
- Sealed records, XML docs on every public member, nullable enabled, no [Obsolete].
- Error codes are lower-case dotted strings prefixed processmanager.; every code has an ADR-062 ErrorCategory set at creation; messages carry codes and type names only, never EncinaError.Message of another error.
- ProcessId is injective across tenants (length-prefixed string form); a missing tenant returns processmanager.tenant_required unless the explicit option AllowMissingTenant (default false) is set.
- The package declares InternalsVisibleTo for the five test assemblies.
- Zero warnings; PublicAPI.Unshipped.txt lists every public symbol.

REFERENCE FILES:
- src/Encina.Messaging/Sagas/SagaErrorCodes.cs (code style)
- src/Encina.Messaging/Sagas/ISagaStore.cs (store contract style)
- src/Encina.Tenancy/TenancyErrors.cs (tenant_required category)
- src/Encina.Hangfire/Encina.Hangfire.csproj (project layout)
```

</details>

---

### Phase 2: Default Implementation — In-Memory Store, Correlation Router, Options

> **Goal**: A working process manager in memory with attribute and custom correlation.

<details>
<summary>Tasks</summary>

1. **`InMemoryProcessManagerStore`** (`src/Encina.ProcessManager/InMemory/`): `ConcurrentDictionary` keyed by the three components; `SaveAsync` compares `expectedVersion` and returns `processmanager.concurrency_conflict`; uses `TimeProvider` for timestamps.
2. **`CorrelateByAttribute`**: `[AttributeUsage(Class)] sealed class CorrelateByAttribute(string propertyName)`; **`AttributeProcessManagerRouter`** compiles one accessor per event type into a static per-type cache (the pipeline-behavior caching rule) and returns `Either<EncinaError, Option<ProcessId>>`; a property that is missing or not a string/Guid/int returns `processmanager.invalid_correlation` at registration time, not per event.
3. **`IProcessManagerRouter`**: `ValueTask<Either<EncinaError, Option<ProcessId>>> ResolveAsync<TEvent>(TEvent @event, CancellationToken)`; `None` means "not a process event"; the tenant comes from `IRequestContextAccessor`.
4. **`ProcessManagerOptions`**: `AllowMissingTenant` (default `false`: the gate fails closed, `AGENTS.md` section 3; `true` is the explicit single-tenant opt-out that stores the reserved `default` tenant segment and is logged per event, Phase 7), `StuckThreshold` (default 1 hour), `RetentionOfCompleted` (default 30 days), `Validate()` returning errors.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```
CONTEXT:
- Phase 1 of issue #392 exists (contract in src/Encina.ProcessManager/).
- This phase adds the in-memory store, the attribute router and the options.

TASK:
Implement Phase 2 Tasks with their unit, guard and property tests.

KEY RULES:
- TimeProvider is injected; production code never reads DateTime.UtcNow.
- The store compares expectedVersion and returns processmanager.concurrency_conflict on mismatch.
- The router caches one compiled accessor per event type in a static generic class; no reflection per event.
- Tests use Shouldly through Encina.Testing.Shouldly, real implementations, AAA, no Thread.Sleep.

REFERENCE FILES:
- src/Encina.Messaging/Sagas/SagaOrchestrator.cs
- src/Encina.Messaging/Choreography/ChoreographyOptions.cs
- docs/engineering/crap-gate-design.md
```

</details>

---

### Phase 3: Routing Handler, Registration & DI

> **Goal**: An event published through `IEncina.Publish` reaches the right manager and is saved.

<details>
<summary>Tasks</summary>

1. **`IProcessManagerEventHandler<TManager, TEvent>`**: `Task<Either<EncinaError, Unit>> HandleAsync(TManager manager, TEvent @event, CancellationToken)`; a manager declares the events it handles by implementing this interface once per event.
2. **`ProcessManagerRoutingHandler<TEvent> : INotificationHandler<TEvent> where TEvent : INotification`** (an open generic; the constraint repeats the interface's, without it the compiler reports CS0452/CS0314; precedent `SignalRBroadcastHandler<>`): resolve the router, load the snapshot (or create a `NotStarted` one when the event is a start event), call the matching `IProcessManagerEventHandler`, then `SaveAsync(snapshot, expectedVersion, step, idempotencyKey)` (the store commits, Design Choice 8; the handler calls no commit member); every `Left` is returned (Design Choice 7). An event with no correlation is ignored with `Right(Unit)` and no step.
3. **`ServiceCollectionExtensions`**: `AddEncinaProcessManager(Action<ProcessManagerOptions>? configure = null)` registers the options (validated at startup), the in-memory store with `TryAddSingleton`, the router and the open-generic routing handler; `AddProcessManager<TManager, TState>()` registers the manager and its event handlers. Registration is idempotent and a database store registered earlier or later wins without overriding an application registration (`AGENTS.md` section 3, registration completeness).
4. **DI proof**: a test that builds the provider with `ValidateOnBuild` and `ValidateScopes`; the routing handler is transient and resolves scoped services from its own scope (no scoped service injected into a singleton).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```
CONTEXT:
- Phases 1-2 of issue #392 exist. INotificationHandler<T> is Task<Either<EncinaError, Unit>> Handle(T, CancellationToken).
- There is no notification pipeline behavior type; the routing is an open-generic INotificationHandler.

TASK:
Implement Phase 3 Tasks with unit, guard, contract (IProcessManagerStore contract against the in-memory store) and DI tests.

KEY RULES:
- Never swallow a Left; return it.
- TryAdd* registrations; database stores win in any registration order.
- No scoped service in a singleton; create a scope per event.
- Tenant from IRequestContextAccessor; a missing tenant returns processmanager.tenant_required unless AllowMissingTenant is set.

REFERENCE FILES:
- src/Encina.SignalR/ServiceCollectionExtensions.cs (open generic handler)
- src/Encina/Dispatchers/Encina.NotificationDispatcher.cs
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs
```

</details>

---

### Phase 4: Multi-Provider Stores

> **Goal**: The durable store on the eight provider packages (Design Choice 3, option A).

<details>
<summary>Tasks</summary>

Per provider: a `ProjectReference` from the provider csproj to `Encina.ProcessManager`, an entity/row type, the store `ProcessManagerStore{ADO|Dapper|EF|MongoDB}`, the schema script (ADO and Dapper: `0NN_CreateProcessManagerTables.sql`, next free number when the pull request opens, creating `ProcessManagerInstances` and `ProcessManagerSteps`, and the same tables appended to the aggregate `000_CreateAllTables.sql` of the package as the other tables are; EF Core: `ProcessManagerInstanceConfiguration`; MongoDB: two collections with a unique index on the three id components), and `AddEncinaProcessManager{Provider}()` with `TryAdd*` called before the core registration.

| Package | Store | Notes |
|---------|-------|-------|
| `Encina.ADO.SqlServer` | `ProcessManagerStoreADO` | `@param`, `TOP (@n)`, `bit`; conditional `UPDATE ... WHERE Version = @expected` |
| `Encina.ADO.PostgreSQL` | `ProcessManagerStoreADO` | `LIMIT @n`, `true/false`, quoted identifiers |
| `Encina.ADO.MySQL` | `ProcessManagerStoreADO` | `LIMIT @n`, `0/1`, backticks |
| `Encina.Dapper.SqlServer` | `ProcessManagerStoreDapper` | same SQL as ADO.NET through Dapper |
| `Encina.Dapper.PostgreSQL` | `ProcessManagerStoreDapper` | same |
| `Encina.Dapper.MySQL` | `ProcessManagerStoreDapper` | same |
| `Encina.EntityFrameworkCore` | `ProcessManagerStoreEF` | one class for SqlServer, PostgreSQL and MySQL; `rowversion`-style check through the `Version` column; `SaveAsync` stages the instance and the step and calls `SaveChangesAsync` itself (Design Choice 8), a `DbUpdateConcurrencyException` or unique-index violation maps to `concurrency_conflict` or the duplicate result |
| `Encina.MongoDB` | `ProcessManagerStoreMongoDB` | `FindOneAndUpdate` with the version in the filter |

Every store: async with `CancellationToken` (`OpenAsync`, `ExecuteNonQueryAsync`), the state serialized through the same serializer path as the saga state (so encryption decorators apply), `Left` on infrastructure failure, `TimeProvider` for timestamps, the tenant in a column and in every query.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```
CONTEXT:
- Issue #392, Phase 4. The IProcessManagerStore contract and the in-memory store exist. This prompt is run once per provider package, in the pull-request order of the split table.

TASK:
Implement the store, the schema script or mapping, the DI extension and the tests of ONE provider package.

KEY RULES:
- Async only with CancellationToken; NEVER the synchronous overloads.
- Conditional update on Version; a mismatch returns processmanager.concurrency_conflict.
- The tenant is a column and part of every query; a missing tenant returns processmanager.tenant_required unless AllowMissingTenant is set.
- SaveAsync commits by itself (Design Choice 8): the EF Core store calls SaveChangesAsync inside SaveAsync; nobody else commits.
- Provider coherence: same behavior on all eight packages; a contract test instantiates each real store.
- Integration tests use [Collection("<Family>-<Database>")] shared fixtures and ClearAllDataAsync in InitializeAsync; never IClassFixture.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/Sagas/SagaStoreADO.cs
- src/Encina.Dapper.PostgreSQL/Sagas/SagaStoreDapper.cs
- src/Encina.EntityFrameworkCore/Sagas/SagaStoreEF.cs
- src/Encina.MongoDB/Sagas/SagaStoreMongoDB.cs
- src/Encina.ADO.SqlServer/Scripts/003_CreateSagaStatesTable.sql
```

</details>

---

### Phase 5: Dashboard & Health Check

> **Goal**: The issue's visibility queries and a stuck-process health check.

<details>
<summary>Tasks</summary>

1. **`IProcessManagerDashboard`**: `GetProcessDetailsAsync(ProcessId, CT)` returning `Either<EncinaError, ProcessDetails>` (`processmanager.not_found` as `Left`, rule R1), `ListActiveAsync(page query, CT)` and `ListStuckAsync(TimeSpan threshold, CT)`; the issue's `IAsyncEnumerable` becomes a page (`ProcessSummaryPage` with a keyset cursor) so a `Left` can be reported. `ProcessDetails` and `ProcessSummary` carry ids, status, version, step list and timestamps, never the state JSON.
2. **`ProcessManagerHealthCheck`** (`DefaultName` const, tags static array, scoped store resolution through `IServiceProvider.CreateScope()`): degraded when stuck processes exceed a threshold, unhealthy above a critical one; reuses the shape of `SagaHealthCheck`.
3. **Fail closed**: a dashboard call with no tenant returns `processmanager.tenant_required` unless `AllowMissingTenant` is set.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```
CONTEXT:
- Issue #392, Phase 5. The store contract has ListActiveAsync, ListStuckAsync and GetStepsAsync.

TASK:
Implement the dashboard service, its DTOs, the health check and their tests.

KEY RULES:
- Every method returns Either; a not-found id is processmanager.not_found (NotFound).
- DTOs never include the state JSON or any free text.
- Health check follows SagaHealthCheck (DefaultName, Tags, scoped resolution).

REFERENCE FILES:
- src/Encina.Messaging/Health/SagaHealthCheck.cs
- src/Encina.Messaging/Sagas/ISagaStore.cs
```

</details>

---

### Phase 6: Cross-Cutting Integration

> **Goal**: Apply the matrix items marked integrate.

<details>
<summary>Tasks</summary>

1. **Idempotency**: the routing handler passes `IRequestContext.IdempotencyKey` (`src/Encina/Abstractions/IRequestContext.cs`) to `SaveAsync`; a duplicate delivery of an already recorded `(process, idempotency key, step ordinal)` appends no step, applies no snapshot and returns `Right(Unit)`; a null key disables the guard for that event. The key is per step ordinal (`ProcessStep.Ordinal`, Phase 1) because one event may save several steps with the same key (the decision engine of #296 saves one per action, #317 one per command): `(key, 0)` is the event's own step, `(key, 1..n)` the further ones, each persisted exactly once. The step table has a unique index on `(tenant, process type, correlation key, idempotency key, ordinal)`. **Null-key behavior per provider**: the idempotency key column is nullable and a unique index treats NULL differently (SQL Server admits one NULL per index, PostgreSQL and MySQL admit many, MongoDB treats a missing field as one null value). To make all providers behave as "a null key never conflicts", the index is partial on non-null keys: SQL Server a filtered index `WHERE IdempotencyKey IS NOT NULL`, PostgreSQL a partial index with the same predicate, MySQL (no partial indexes) a stored generated column that is NULL when the key is NULL and a unique index on it plus the id columns, MongoDB a `partialFilterExpression` on `IdempotencyKey` existing. The tenant column of the index holds the reserved `default` segment for a single-tenant host, never NULL, so the tenant part of the index has no NULL case; a contract test per provider covers two null-key saves and a duplicate non-null key.
2. **Validation**: the options validator runs at startup (`ValidateOnStart`); the routing handler rejects an event whose correlation value is empty with `processmanager.invalid_correlation`.
3. **Audit trail**: the `ProcessStep` list is the audit trail of the process; the operation audit of ADR-036 is deferred (matrix item 12).
4. **Resilience**: no external call is made by the handler; retry of a failed event belongs to the publisher, which receives the `Left`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```
CONTEXT:
- Issue #392, Phase 6. Phases 1-5 exist.

TASK:
Add the duplicate-delivery guard, the options validation and the correlation validation, with tests.

KEY RULES:
- A duplicate delivery returns Right(Unit) and appends no step; a store failure is a Left.
- No EncinaError.Message in any log, tag or stored value.

REFERENCE FILES:
- src/Encina.Messaging/Inbox/ (idempotency precedent)
- src/Encina/Abstractions/IRequestContext.cs
```

</details>

---

### Phase 7: Observability

> **Goal**: Spans, metrics and logs under one registered range.

<details>
<summary>Tasks</summary>

1. **EventId range**: register `ProcessManager = (12270, 12289)` in `src/Encina/Diagnostics/EventIdRanges.cs` BEFORE any `[LoggerMessage]`; add the registry row (status `reserved` to `registered`), the field to `src/Encina/PublicAPI.Unshipped.txt` and the `Encina.ProcessManager` entry to `AssemblyRanges` in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`. This plan uses 12270-12277: event applied with its step appended (12270), event ignored (12271), duplicate (12272), concurrency conflict (12273), tenant denied (12274), store failure (12275), stuck detected (12276), event handled without a tenant under the `AllowMissingTenant` opt-out (12277); #317 takes 12278-12283 and #296 12284-12289.
2. **`ProcessManagerActivitySource`** named `Encina.ProcessManager`, span `encina.process_manager.handle_event` with `HasListeners()` guard; tags `process_type`, `outcome`, `error_code`; the tenant tag only when multi-tenancy is enabled (ADR-054 D4); never the correlation key or the process id.
3. **`ProcessManagerMetrics`** (Meter `Encina.ProcessManager`): `Counter<long> encina.process_manager.events_handled` (tags process type, outcome), `Histogram<double> encina.process_manager.event_duration` in **seconds**, unit `s` (ADR-054 decision 4; the code records `Stopwatch` ticks as seconds). No `_total` suffix.
4. **`ProcessManagerLog`** (`internal static partial`, in this pull request so that #317 and #296 add entries by addition): `[LoggerMessage]` entries carrying the process type and the error code, never the correlation key or `EncinaError.Message`; exceptions go through `ForLogging()`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```
CONTEXT:
- Issue #392, Phase 7, landing in pull request 2 (before the router, store and handler, which log through ProcessManagerLog). EventIds are registered first (ADR-021); the block is 12250-12299 of the Core and messaging area, split between the family plans. The histogram unit is seconds.

TASK:
Register the range, add the activity source, the meter and the log class, instrument the routing handler, and test them.

KEY RULES:
- Register in EventIdRanges.cs, the registry page, PublicAPI.Unshipped.txt and AssemblyRanges BEFORE using an id; ids packed from 12270 without gaps.
- Instruments have no _total suffix; tags are bounded (process type, outcome, error code) plus the tenant tag only when multi-tenancy is on.
- Run the architecture tests (EncinaEventIdAllocationTests, EventIdRegistryAgreementTests).

REFERENCE FILES:
- src/Encina.Messaging/Diagnostics/SagaActivitySource.cs
- src/Encina.Messaging/Diagnostics/MessagingStoreMetrics.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- docs/engineering/eventid-block-registry.md
```

</details>

---

### Phase 8: Testing and Documentation & Finalization

> **Goal**: Every flag at its target; the documentation set.

<details>
<summary>Tasks</summary>

#### Testing

| Flag | Scope | Per-file target (justification) |
|------|-------|---------------------------------|
| Unit | `ProcessId`, `ProcessManagerBase`, in-memory store, router, routing handler, dashboard, health check | `ProcessId.cs` 95, `ProcessManagerBase.cs` 90, `InMemoryProcessManagerStore.cs` 90, `AttributeProcessManagerRouter.cs` 90, `ProcessManagerRoutingHandler.cs` 90 (all pure logic reachable without infrastructure) |
| Guard | every public constructor and method with parameters | `ProcessId.cs` 90, the DI extension 90, the dashboard 85 (null and empty arguments are the only branches) |
| Contract | `IProcessManagerStore` against the in-memory store and each real provider store | `InMemoryProcessManagerStore.cs` 80 (the contract exercises save, load, conflict, list, purge) |
| Property | `ProcessId` injectivity and round trip (FsCheck) | `ProcessId.cs` 80 (one invariant covers the two encoders) |
| Integration | each provider store on a real database through shared `[Collection]` fixtures | each provider store 70 (save, conflict, list and purge paths against Docker databases) |

Load and benchmark: a justification `.md` each (the routing handler is one load, one save per event; concurrency is the store's version check, covered by integration tests).

#### Documentation (mandatory set)

1. XML docs on every public API; `changelog.d/392-process-manager.added.md`.
2. `src/Encina.ProcessManager/README.md`, `docs/features/process-manager.md` (saga versus process manager, usage), `docs/INVENTORY.md`, `ROADMAP.md`.
3. ADR (number assigned when the ADR pull request opens): one pattern, one core for the family (D1).
4. `PublicAPI` files; `dotnet build --configuration Release` with zero warnings; per-flag coverage measured against the manifest.

</details>

<details>
<summary>Prompt for AI Agents — Phase 8</summary>

```
CONTEXT:
- Issue #392, Phase 8. All production phases exist.

TASK:
Write the tests of the table in Phase 8, the justification files and the documentation set.

KEY RULES:
- Tests execute real code; reflection-only tests are forbidden; Shouldly through Encina.Testing.Shouldly.
- Integration tests use shared [Collection] fixtures and ClearAllDataAsync.
- Documentation never types coverage figures by hand (cov: references).

REFERENCE FILES:
- tests/Encina.UnitTests/Messaging/Sagas/SagaOrchestratorTests.cs
- docs/testing/coverage-measurement-methodology.md
- .github/coverage-manifest/Encina.Messaging.json
```

</details>

---

### Proposed split into pull requests (limit 15 files and 1500 lines each)

Every count includes the tests, the manifest, the PublicAPI lines and the `ProjectReference` edits of its own code; each pull request builds and passes alone.

| PR | Content | Files | Lines |
|----|---------|-------|-------|
| 1 | Phase 1: csproj (with the `InternalsVisibleTo` items), `ProcessId`, `ProcessStatus`, `ProcessStep`, `ProcessManagerBase`, `ProcessManagerErrors.cs` (codes and internal errors in one file), `IProcessManagerStore.cs` (with `ProcessSnapshot`) = 7; `Encina.slnx`, 2 PublicAPI, manifest, `UnitTests` and `GuardTests` project references, 1 unit and 1 guard test file = 8 | 15 | 900 |
| 2 | Phase 7: `ProcessManagerActivitySource`, `ProcessManagerMetrics`, `ProcessManagerLog` (3), `EventIdRanges.cs`, registry page, `AssemblyRanges` test, `Encina` PublicAPI, package PublicAPI, 2 manifests, 2 unit tests | 12 | 700 |
| 3 | Phase 2: in-memory store, `CorrelateByAttribute`, `AttributeProcessManagerRouter`, `IProcessManagerRouter`, options (5), PublicAPI, manifest, unit (store, router), guard, property test + `PropertyTests` project reference | 12 | 1,000 |
| 4 | Phase 3: routing handler, handler interface, DI extension (3), PublicAPI, manifest, unit, DI proof, guard, contract test + `ContractTests` project reference | 11 | 1,100 |
| 5 | Phases 5-6: dashboard, DTOs, health check (3), duplicate guard in the handler, PublicAPI, manifest, 3 tests | 10 | 800 |
| 6-8 | Phase 4 ADO.NET: one PR per provider (SqlServer, PostgreSQL, MySQL): store, entity, script, aggregate `000_CreateAllTables.sql`, DI extension, provider csproj `ProjectReference`, PublicAPI, manifest, unit, guard, integration test, contract registration | 12 each | 900 each |
| 9-11 | Phase 4 Dapper: one PR per provider, same shape | 12 each | 900 each |
| 12 | Phase 4 EF Core (one store for three databases): store, configuration, DI extension, csproj `ProjectReference`, PublicAPI, manifest, unit, guard, 3 integration classes, contract registration | 12 | 1,100 |
| 13 | Phase 4 MongoDB: store, indexes, DI extension, csproj `ProjectReference`, PublicAPI, manifest, unit, guard, integration, contract registration | 10 | 800 |
| 14 | Phase 8 documentation: README, feature guide, INVENTORY, ROADMAP, ADR, changelog fragment, load and benchmark justification files | 8 | 600 |

Total: 14 pull requests, about 162 files (15 + 12 + 12 + 11 + 10 + 6 x 12 + 12 + 10 + 8, the sum of the split table); no pull request exceeds 15 files or 1,500 lines. Pull requests 1 to 5 are sequential; pull requests 6 to 13 are independent of each other after 4; pull request 2 is the one #317 and #296 wait for when they need `ProcessManagerLog`.

---

## Research

### Relevant Standards & Specifications

| Reference | What it defines | Use in this feature |
|-----------|-----------------|---------------------|
| Enterprise Integration Patterns, Process Manager | A central unit that keeps process state and routes messages | The pattern name and the router role |
| Oskar Dudycz, "Saga and Process Manager" | Saga = transaction with compensation; Process Manager = long-lived state reacting to events | Positioning against `SagaOrchestrator` |
| ADR-001, ADR-006 | Railway Oriented Programming | Failures are `Left` |
| ADR-018, ADR-021 | Cross-cutting check, EventId ranges | Matrix and range 12270-12289 |
| ADR-054 D4, ADR-062 | Tenant tag, `ErrorCategory` | Telemetry and error codes |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `INotificationHandler<TNotification>` | `src/Encina/Abstractions/INotificationHandler.cs` | Handler contract returning `Task<Either<EncinaError, Unit>>`; the routing handler is an open generic |
| `Encina.NotificationDispatcher` | `src/Encina/Dispatchers/Encina.NotificationDispatcher.cs` | Resolves `IEnumerable<INotificationHandler<T>>`; there is no notification pipeline behavior type |
| `SignalRBroadcastHandler<>` | `src/Encina.SignalR/ServiceCollectionExtensions.cs` | Precedent for an open-generic `INotificationHandler` registration |
| `IChoreographySaga` and `IChoreographyStateStore` | `src/Encina.Messaging/Choreography/IChoreographyStateStore.cs` | Closest existing event-reaction pattern keyed by a string correlation id |
| `SagaOrchestrator` and `ISagaStore` | `src/Encina.Messaging/Sagas/SagaOrchestrator.cs` | Model for store naming, per-provider stores and the saga row |
| `EncinaErrors.Create` and `ErrorCategory` | `src/Encina/Errors/ErrorCategory.cs` | Error factory with a protocol-neutral category (ADR-062) |
| `TenancyErrors` | `src/Encina.Tenancy/TenancyErrors.cs` | `tenancy.tenant_required` is `InvalidRequest`; the process code mirrors it |
| `SagaHealthCheck` | `src/Encina.Messaging/Health/SagaHealthCheck.cs` | Shape of the stuck-process health check |
| `SagaActivitySource`, `MessagingStoreMetrics` | `src/Encina.Messaging/Diagnostics/` | ActivitySource and Meter patterns |
| `EventIdRanges` | `src/Encina/Diagnostics/EventIdRanges.cs` | Registry of the new range |
| `TimeProvider` | BCL | Injected clock |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` (saga) | 2200-2299 | Full: #720 2206-2229, #738 2230-2249, #750 2250-2274, #1237 2275-2299 |
| `Encina` area Core and messaging, reserved by #4007 | 10000-12199 | Migration targets of existing ranges; not allocatable |
| Claimed by #340 (open plan branch) | 12200-12249 | `RequestEnrichment` |
| **Block for this family (Process Manager 12250-12299)** | **12250-12299** | **First free 50-id slot of the registry after the #4007 reservations and #340; #128 takes 12250-12269 (`MessagingSagaVisibility`); the Process Manager family takes 12270-12289 (`ProcessManager`); 12290-12299 spare** |
| **`Encina.ProcessManager` (this plan)** | **12270-12277** | **New field `ProcessManager = (12270, 12289)`; this plan 12270-12277, #317 12278-12283, #296 12284-12289** |

The block row is added to the registry page when the code lands (status `reserved` to `registered`), with a `PublicAPI.Unshipped.txt` line per new field.

### Provider Matrix

| Provider category | Applies | Reason |
|-------------------|---------|--------|
| Database (10) | Yes (Design Choice 3) | The process row and its steps are state; ADO x3, Dapper x3, EF Core (3 databases), MongoDB |
| Marten | #317 | Event-sourced store, outside the database rule |
| Caching, Transport, Locks, Validation, Cloud | No | No cache, broker or lock of its own |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Core package source | ~25 | Contract, in-memory, router, handler, DI, dashboard, health, diagnostics, project, PublicAPI x2 |
| Provider stores | ~40 | Eight packages: store, entity or mapping, script, DI line each |
| Registry and manifests | ~14 | `EventIdRanges.cs`, registry page, `AssemblyRanges` test, nine manifests, `Encina.slnx`, `Encina` PublicAPI |
| Tests | ~55 | Unit, guard, contract, property, DI, integration (eight classes), justification files |
| Documentation | ~8 | README, guide, INVENTORY, ROADMAP, ADR, changelog fragment |
| **Total** | **~162 in 14 pull requests** | Equals the sum of the split table (categories overlap in the estimates above; the split table is the count) |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Railway Oriented Programming (Either<EncinaError, T>); no [Obsolete], no compatibility layers; XML docs on every public API; zero warnings.
- Issue #392: the Process Manager core. A new package Encina.ProcessManager keeps process state while reacting to notifications; #317 (commands, Marten) and #296 (decisions) extend it. Plan: docs/plans/process-manager-implementation-plan-392.md. Dilemmas D1-D5 are recommended, pending the maintainer; read the Maintainer Decisions section first if it exists.

IMPLEMENTATION OVERVIEW:
1. Phase 1: contract (ProcessId, ProcessStatus, ProcessStep, ProcessManagerBase, IProcessManagerStore, errors).
2. Phase 2: in-memory store, attribute router, options. Phase 3: open-generic routing INotificationHandler, registration, DI proof.
3. Phase 4: eight provider stores. Phase 5: dashboard and health check. Phase 6: duplicate guard and validation.
4. Phase 7: EventId range 12270-12289, activity source, meter, log. Phase 8: tests and documentation.

KEY PATTERNS:
- No INotificationPipelineBehavior exists; the routing is an INotificationHandler<> open generic.
- Every failure is a Left and is returned to the publisher; never swallowed.
- ProcessId is injective across tenants; the tenant is a column in every query; a missing tenant is processmanager.tenant_required unless the explicit option AllowMissingTenant (default false) is set.
- SaveAsync commits by itself on every provider (Design Choice 8); the idempotency guard is per (process, idempotency key, step ordinal).
- Steps carry the event type, outcome and error code only; EncinaError.Message never reaches logs, tags or storage.
- Time from TimeProvider; TryAdd* registrations; a DI test with ValidateOnBuild and ValidateScopes.
- Tests: Shouldly through Encina.Testing.Shouldly, real implementations, per-flag targets from the manifest, CRAP <= 10 on changed methods.

REFERENCE FILES:
- src/Encina/Abstractions/INotificationHandler.cs
- src/Encina.SignalR/ServiceCollectionExtensions.cs
- src/Encina.Messaging/Sagas/ISagaStore.cs
- src/Encina.Messaging/Health/SagaHealthCheck.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- docs/plans/dsr-implementation-plan-404.md (style)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ⏭️ | Deferred: reads of a hot process could be cached, but a cached snapshot would serve stale state to a writer; the issue lists it as deferred and the saga store cache is #696; issue file `artifacts/issues/process-manager-state-cache.md` |
| 2 | OpenTelemetry | ✅ | Phase 7: source and meter `Encina.ProcessManager`, span `encina.process_manager.handle_event`, counter `events_handled`, histogram `event_duration` (seconds); tenant tag only when multi-tenancy is on (ADR-054 D4) |
| 3 | Structured Logging | ✅ | Phase 7: `[LoggerMessage]` in 12270-12277, registered first; process type and error code only |
| 4 | Health Checks | ✅ | Phase 5: `ProcessManagerHealthCheck` for stuck processes (the issue deferred it; the dashboard already needs `ListStuckAsync`) |
| 5 | Validation | ✅ | Phase 6: options validated at startup, empty correlation rejected as `processmanager.invalid_correlation` |
| 6 | Resilience | ❌ | Not applicable: the handler makes no external call; retry of a failed event is the publisher's, which receives the `Left` |
| 7 | Distributed Locks | ⏭️ | Deferred (Dilemma D4): optimistic concurrency is integrated; an opt-in lock is `artifacts/issues/process-manager-distributed-lock.md` |
| 8 | Transactions | ✅ | Phase 4: `SaveAsync` writes the state and the step in one call; the store commits inside `SaveAsync` on every provider (Design Choice 8, Dilemma D5, recommended, pending the maintainer): ADO.NET and Dapper use one provider transaction inside the store (or the scope transaction when the caller supplies one), EF Core stages both on the `DbContext` and calls `SaveChangesAsync` itself, MongoDB writes the step first under a unique `(process, idempotency key)` key and then the state; no atomicity is claimed across stores, and a retry after a crash finds the step and finishes through the duplicate guard |
| 9 | Idempotency | ✅ | Phase 6: a duplicate `(process, idempotency key)` appends no step and returns `Right(Unit)` |
| 10 | Multi-Tenancy | ✅ | `TenantId` in `ProcessId`, in every column and query, fail closed (`AllowMissingTenant` is the explicit, logged opt-out); the issue deferred it, but a store of per-tenant state cannot ship without it |
| 11 | Module Isolation | ⏭️ | Deferred: `ModuleId` on messaging entities is #747; the process row inherits it when #747 lands |
| 12 | Audit Trail | ✅ | The step list is the process audit trail (issue acceptance criterion); the operation audit store of ADR-036 is deferred to `artifacts/issues/process-manager-operation-audit.md` |

---

## Prerequisites & Dependencies

- No blocking issue: `INotificationHandler<>`, the dispatcher and the provider patterns exist on `origin/main`.
- Advisable: #2213 (read-modify-write on the saga row) shows the same optimistic-concurrency theme; #738 (tenant on the saga row) is the precedent for the tenant columns.
- Sibling plans merge after this one: #317, then #296.

---

## Next Steps

1. The maintainer answers Dilemmas D1 to D5 when work on #392 starts; the answers go into `## Maintainer Decisions` at the end of this plan.
2. Open the three deferred issues named in the matrix and link them from #392.
3. Brief pull request 1 first; pull requests 2 to 5 follow in order, then the provider pull requests 6 to 13 in any order, then 14.
