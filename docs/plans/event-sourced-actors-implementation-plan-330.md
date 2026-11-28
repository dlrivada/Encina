# Implementation Plan: Actor-Based Event Sourcing (`Encina.Actors.EventSourcing` and `Encina.Actors.Marten`)

> **Issue**: [#330](https://github.com/dlrivada/Encina/issues/330)
> **Type**: Feature (new packages)
> **Related**: #388 (owns `Encina.Actors` contracts and `Encina.Orleans`), #459 (Orleans handlers, sagas and reminders), #320 (decider pattern, owns `IDecider`), #323 (snapshot strategies), #326 (Marten multi-tenancy), #1257 (default-tenant rule), ADR-054, ADR-062
> **Complexity**: High (6 phases, 6 pull requests, two new packages)
> **Estimated Scope**: ~2,100 lines of production code + ~3,500 lines of tests and documentation, in six pull requests of at most 15 files

---

## Summary

### What exists today (read on 2026-11-27, `origin/main` `4f8e52f42`)

- There is no actor code in `src/`: no `Encina.Actors`, no `Encina.Orleans`. Two plans on main own the first pieces: #388 creates `Encina.Actors` (`ActorId`, the `IEncinaActor` marker, `IActorSystem`, `ActorErrorCodes`) and the `Encina.Orleans` skeleton (it records an in-process actor runtime as a follow-up it does not build), and #320 plans `IDecider<TCommand, TEvent, TState>` (`Decide(command, state)` returning `Either<EncinaError, IReadOnlyList<TEvent>>`, `Evolve(state, event)`, `InitialState`, `IsTerminal`) with a Marten repository that folds, decides and appends. Neither is implemented yet; this plan cites them by issue number.
- Event-sourced aggregates today are class based: `IAggregateRepository<TAggregate>` and `MartenAggregateRepository<TAggregate>` (`src/Encina.Marten/`), which append with the expected stream version (`_session.Events.Append(aggregate.Id, aggregate.Version, events)`, `MartenAggregateRepository.cs`) and surface a conflict as `marten.concurrency_conflict`. Concurrency is optimistic: two writers race and one retries.
- Marten 9.38.0 is pinned and every Encina aggregate stream uses a `Guid` identity; `Encina.Marten` has no `PublicAPI.Unshipped.txt`. `ISnapshotStore<TAggregate>` is typed to `IAggregate`/`ISnapshotable`, so it cannot hold the state of a decider.
- The issue's sketch `IEventSourcedActor<TState, TCommand, TEvent>` (`Handle`, pure `Apply`, `OnRecoveredAsync`) is the same shape as #320's decider (`Decide`, `Evolve`); `Task OnRecoveredAsync` and `IActorSystem.GetActor<OrderActor>(orderId).SendAsync(cmd)` have no equivalent yet. Orleans, Proto.Actor and Akka are not referenced anywhere in `Directory.Packages.props`.

### What this plan delivers

- **`Encina.Actors.EventSourcing`** (new package): an **in-process event-sourced actor runtime** that hosts a #320 decider as an actor: `IEventSourcedActorSystem` (`GetActor` by registered name and `ActorId`), `IActorRef<TCommand, TEvent, TState>` (`SendAsync`, `GetStateAsync`), one mailbox per actor so commands run one at a time, activation by loading the stream (snapshot first, then events), passivation after an idle timeout, snapshots every N events, registration `AddEncinaEventSourcedActors(o => o.Register<OrderDecider>("order"))`.
- **`Encina.Actors.Marten`** (new package): `MartenActorEventStore` (append with the expected version, tenant, correlation and causation headers) and the snapshot store, on a **secondary Marten store with string stream identity** so the tenant-qualified key is exact (Design Choice 3).
- **Fail-closed tenancy**, observability, a health check and a new EventId range.
- **Not delivered**: Orleans hosting of these actors (a grain that runs the same decider is a follow-up of #459 and recorded in Next Steps), Proto.Actor or Akka.NET adapters (post-1.0, recorded in the plan only), actor clustering beyond what the expected-version append already guarantees, an EventStoreDB store (its package does not exist).
- Affected packages: `Encina.Actors.EventSourcing` (new), `Encina.Actors.Marten` (new), `Encina` (EventIds). Provider category: event sourcing (Marten primary, `AGENTS.md` section 5); the 10-database rule does not apply. Event-sourced modules follow `AGENTS.md` section 3: unit tests mock the store, integration tests run Marten on PostgreSQL through Testcontainers; the in-memory event store of the runtime exists only for runtime unit tests and development, not for any compliance module.

---

## Design Choices

<details>
<summary><strong>1. Where the actors run</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A lightweight in-process runtime owned by Encina (mailbox per actor, activation, passivation), storing events through an `IActorEventStore`** | Meets "actor pattern works for a basic use case" with no cluster; testable with a fake clock; closes the in-process follow-up of #388; no new third-party dependency | Single process only: two processes hosting one actor are protected by the expected-version append, not by placement |
| **B) Orleans-hosted: a grain base in `Encina.Orleans` runs the decider, state in the grain, events in Marten** | Placement, clustering and activation by a mature runtime | Needs #388's `Encina.Orleans` merged first; Orleans TestCluster in every test; ties the pattern to one runtime |
| **C) Proto.Actor or Akka.NET adapters** | Mature actor semantics, supervision | A new dependency and package per runtime, with no demand recorded; the issue says "if community interest" |

### Chosen Option: **A - In-process runtime, deciders as the contract** (recommended, pending the maintainer)

### Rationale

- A wins for a first release and for tests; B wins when the application already runs Orleans and needs distribution, and it reuses this plan's decider and store contracts (the grain replaces only the mailbox and activation); C wins only with proven demand. A and B share the `IActorEventStore` seam, so B costs one extra class later.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. The actor's contract: the issue's interface or the decider</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reuse #320's `IDecider<TCommand, TEvent, TState>`: `Decide` is the issue's `Handle`, `Evolve` is `Apply`; the runtime adds only hosting** | One contract for event-sourced logic, whether run by a repository or by an actor; tests with the decider's Given/When/Then helper | Depends on #320 (open, no code); the issue's `OnRecoveredAsync` hook becomes an optional `IActorRecoveryHook<TState>` |
| **B) The issue's own `IEventSourcedActor<TState, TCommand, TEvent>` and `EventSourcedActor` base class** | No dependency on #320; matches the issue text | A second pure-function contract next to the decider; the issue's `Handle` returns `IEnumerable<TEvent>` where #320 returns an immutable list, so two near-identical contracts drift |

### Chosen Option: **A - Hosting for #320's decider** (recommended, pending the maintainer)

### Rationale

- A wins unless #320 is dropped or reshaped by the maintainer; B wins if the actor feature must ship before #320. The runtime depends only on the three members the plan names (`Decide`, `Evolve`, `InitialState`, `IsTerminal`), so a change of #320's location (its own Dilemma D1) is a namespace edit here.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Stream identity and the tenant-qualified key</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A secondary Marten store (`AddMartenStore<IActorMartenStore>`) with `StreamIdentity.AsString`; the stream key is `{tenant}/{actor name}/{actor key}` with each component percent-encoded (`Uri.EscapeDataString`), so `/` cannot occur inside a component and two (tenant, name, key) triples never map to one key; the tenant segment is the reserved literal `default` only when multi-tenancy is off** | Exact, injective keys; no effect on the aggregate store, which stays on `Guid` identity | A second Marten store configuration and schema (a connection string or the same database with its own schema) |
| **B) A deterministic `Guid` derived from the triple (name-based UUID) in the existing store** | One store | A 128-bit hash is not injective; a collision would merge two actors' streams (the loader would have to verify a stored key header and fail closed) |
| **C) Switch the whole Marten store to string identity** | One store, exact keys | Changes the identity of every existing aggregate stream |

### Chosen Option: **A - Secondary string-identity store** (recommended, pending the maintainer)

### Rationale

- A wins because injectivity is a stated requirement for tenant-qualified keys (the rule of #388's register row C4 and #1257's `default` segment) and A achieves it without touching existing streams; B wins if a second store configuration is unacceptable and the key-header verification is accepted; C is rejected for pre-1.0 only if the maintainer wants a single identity style. With multi-tenancy on and no tenant in the context, `GetActor` returns `tenant_required` (`InvalidRequest`) and never invents `default`; a tenant literally named `default` is `reserved_tenant` (`Validation`).

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Actor lifecycle: mailbox, passivation and snapshots</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A bounded `Channel<T>` per active actor with a single reader task; passivation by a `TimeProvider` timer after `PassivationTimeout` of idleness; a snapshot of the state every `SnapshotEvery` events (default 100) in a state-typed snapshot store** | Exactly one command at a time per actor; memory bounded by the active set; the snapshot makes recovery bounded | A snapshot store of its own (the aggregate `ISnapshotStore` cannot hold decider state) |
| **B) A lock (`SemaphoreSlim`) per actor id, no mailbox** | Smaller | No ordering guarantee between waiting callers; no backpressure; a lock per id leaks in a dictionary |

### Chosen Option: **A - Mailbox, timer passivation, state snapshots**

### Rationale

- A gives the single-threaded-per-actor guarantee the issue is about and a bound on pending commands (`MailboxCapacity`, default 1,000; a full mailbox is `mailbox_full`, `Unavailable`). The passivation timer callback catches every failure, logs by code and never throws; a passivating actor drains its mailbox first; the next call re-activates it. Snapshot cadence per aggregate strategy belongs to #323's types for aggregates; this plan keeps only the event-count cadence for actors.

**Class:** Trivial
**Settled by:** `AGENTS.md` section 3 (timer callbacks never throw; time from `TimeProvider`); issue text "One message at a time per actor", "Automatic lifecycle: activation/passivation"

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | Every command appends with the expected stream version; an `append_conflict` (`Conflict`, another process wrote) drops the cached state, the actor re-activates from the store on the next command, and the failed command is not retried automatically: the caller sees the `Left` | Trivial | `MartenAggregateRepository.SaveAsync` (expected-version append, `marten.concurrency_conflict`) |
| C2 | A `Left` from `Decide` is returned to the caller unchanged and nothing is appended; a command to an actor whose `IsTerminal` is true is `actor_terminal` (`Conflict`, an invalid transition) | Settled | `AGENTS.md` section 3 (ROP); ADR-062 R2 |
| C3 | Error codes (prefix `encina.actors.es.`): `actor_not_registered` (`Internal`, a configuration entry), `tenant_required` (`InvalidRequest`), `reserved_tenant` (`Validation`), `mailbox_full` (`Unavailable`), `actor_stopped` (`Unavailable`), `append_conflict` (`Conflict`), `actor_terminal` (`Conflict`), `recovery_failed` (`Internal`), `store_failed` (`Internal`) | Settled | ADR-062 rules R1 to R4; no `FailedPrecondition` before #3420 |
| C4 | The ids `ActorId` come from #388's `ActorId.Create` (`Validation` on bad input); this plan adds no id type | Settled | Plan #388 (owner of `Encina.Actors` contracts) |
| C5 | The store appends the headers `TenantId`, correlation and causation from `IRequestContext`; the stored events are the audit trail of the actor | Trivial | `src/Encina.Marten/EventMetadataEnrichmentService.cs` (the same headers) |
| C6 | Telemetry: `ActivitySource` and `Meter` named `Encina.Actors.EventSourcing`; counter `encina.actors.es.commands` (no `_total`) with tags `encina.actors.es.actor` (the registered name) and `encina.actors.es.outcome` (`ok`, `rejected`, `conflict`, `failed`); gauge `encina.actors.es.active` (observable); histogram `encina.actors.es.command.duration` in seconds; tenant tag through #2676 when merged; never the actor key, tenant id, user id or command content | Settled | ADR-054 decision 4 and D4 |
| C7 | Time comes from `TimeProvider` (passivation, snapshot timestamps); the runtime never reads `DateTime.UtcNow` | Settled | `AGENTS.md` section 3 |
| C8 | `EncinaError.Message` never reaches a log, tag or health-check result; exceptions go through `ForLogging()` | Settled | `AGENTS.md` section 3 |
| C9 | The in-memory `IActorEventStore` is for runtime unit tests and local development; registering it is explicit (`UseInMemoryStore()`), and `AddEncinaActorsMarten()` replaces it in any registration order without overriding an application's own `IActorEventStore` | Settled | `AGENTS.md` section 3 (a database store MUST win over an in-memory default in any registration order) |

---

## Implementation Phases

### Delivery order and size

Six pull requests; each builds and passes alone with its own tests (counts include csproj, `Encina.slnx`, PublicAPI, manifests, test-project references and the fragment).

| PR | Phase | Content | Files | Lines (est.) |
|----|-------|---------|-------|--------------|
| A1 | 1 | `Encina.Actors.EventSourcing` skeleton: contracts, options, error codes, `IActorEventStore` | 15 | ~700 |
| A2 | 2 | Runtime: key, mailbox, activation, passivation, in-memory store, registration | 11 | ~1,400 |
| A3 | 3 | `Encina.Actors.Marten`: event store, snapshot store, secondary store registration, integration tests | 14 | ~1,300 |
| A4 | 4 | Observability, health check, EventIds | 14 | ~900 |
| A5 | 5 | Store contract base, property tests | 7 | ~800 |
| A6 | 6 | Guides, ADR, READMEs, INVENTORY, ROADMAP, changelog | 7 | ~500 |

### Phase 1: Contracts (pull request A1)

<details>
<summary>Tasks</summary>

1. **Create** `src/Encina.Actors.EventSourcing/Encina.Actors.EventSourcing.csproj` (references `Encina`, `Encina.Actors` from #388 and the decider contract of #320; `InternalsVisibleTo` for UnitTests, GuardTests, ContractTests, PropertyTests, IntegrationTests), `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`; **modify** `Encina.slnx`, `tests/Encina.UnitTests/Encina.UnitTests.csproj` and `tests/Encina.GuardTests/Encina.GuardTests.csproj` (references).
2. **Create** `IEventSourcedActorSystem.cs` (`Either<EncinaError, IActorRef<TCommand, TEvent, TState>> GetActor<TCommand, TEvent, TState>(string actorName, ActorId id)`), `IActorRef.cs` (`ValueTask<Either<EncinaError, ActorCommandResult<TEvent, TState>>> SendAsync(TCommand, CancellationToken)`, `ValueTask<Either<EncinaError, TState>> GetStateAsync(CancellationToken)`), `ActorCommandResult.cs` (sealed class: the events appended, the new state, the new version; constructor null checks), `EventSourcedActorOptions.cs` (`PassivationTimeout` 5 minutes, `SnapshotEvery` 100, `MailboxCapacity` 1,000, `RequireTenantContext` as `bool?` (null follows `MultiTenancyMarker`), `Register<TDecider>(string name)`), `EventSourcedActorErrorCodes.cs` (row C3), `IActorEventStore.cs` (public: `LoadAsync` returning snapshot plus events after it, `AppendAsync(key, expectedVersion, events, ct)`, `SaveSnapshotAsync`; the optional `IActorRecoveryHook<TState>` with `OnRecoveredAsync(TState, ct)` is declared in the same file).
3. Manifest `Encina.Actors.EventSourcing.json` and PublicAPI lines through `mechanical-fixer`; unit tests `EventSourcedActorOptionsTests` (defaults, duplicate name registration is `Validation`), guard tests `ActorCommandResultGuardTests`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #330, plan docs/plans/event-sourced-actors-implementation-plan-330.md, phase 1.
Prerequisites: #388 (Encina.Actors: ActorId) and #320 (IDecider) have merged; if either has not, stop and report.

TASK:
1. Create the Encina.Actors.EventSourcing project, the contracts, options and error codes listed in the plan.
2. PublicAPI files and the manifest through mechanical-fixer; unit and guard tests.

KEY RULES:
- Every method returns Either<EncinaError, T>; error codes carry ErrorCategory per ADR-062 (never FailedPrecondition).
- RequireTenantContext is bool?: null follows whether MultiTenancyMarker is registered.
- XML docs with examples on every public member; zero warnings.

REFERENCE FILES:
- docs/plans/virtual-actors-implementation-plan-388.md, docs/plans/decider-pattern-implementation-plan-320.md
- src/Encina.Compliance.Consent/ConsentOptions.cs (RequireTenantContext precedent)
```

</details>

### Phase 2: Runtime (pull request A2)

<details>
<summary>Tasks</summary>

1. **Create** `Runtime/ActorKey.cs` (internal: builds `{tenant}/{name}/{key}`, percent-encoded, reserved `default`), `Runtime/ActorInstance.cs` (internal: bounded `Channel<T>`, single reader loop, state, version, passivation timer through `TimeProvider.CreateTimer` with a catching callback), `Runtime/EventSourcedActorSystem.cs` (implements `IEventSourcedActorSystem`; activation loads the snapshot and events through `IActorEventStore`, folds with `Evolve`, calls the recovery hook), `Runtime/InMemoryActorEventStore.cs`, `ServiceCollectionExtensions.cs` (`AddEncinaEventSourcedActors(Action<EventSourcedActorOptions>)`, `UseInMemoryStore()`; `TryAdd*` so a database store wins in any order; the system is a singleton that creates a scope per command through `IServiceScopeFactory` to reach the scoped `IRequestContext`).
2. **Modify** `PublicAPI.Unshipped.txt`, the manifest.
3. Tests: new `tests/Encina.UnitTests/ActorsEventSourcing/ActorKeyTests.cs` (round trip, `/` and `%` in components, default segment rule), `ActorInstanceTests.cs` (commands run one at a time, a full mailbox, passivation with a `FakeTimeProvider`, re-activation after a conflict), `EventSourcedActorSystemTests.cs` (tenant gate, unknown name, `Left` from `Decide` appends nothing, terminal actor), a DI test with `ValidateOnBuild` and `ValidateScopes`, guard tests for constructors.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT:
Issue #330 plan docs/plans/event-sourced-actors-implementation-plan-330.md, phase 2. A1 merged.

TASK:
1. ActorKey, ActorInstance (mailbox), EventSourcedActorSystem, the in-memory store and the registration.
2. Unit, guard and DI tests with a FakeTimeProvider.

KEY RULES:
- One command at a time per actor; bounded mailbox; passivation drains the mailbox first.
- A timer callback never throws (catch, log by code, keep running); an exception from the stopping token ends the loop quietly.
- The system is a singleton: create a scope per command with IServiceScopeFactory; never inject a scoped service into it.
- With multi-tenancy on and no tenant the call is tenant_required; never invent "default"; the key is injective.
- Never swallow a Left; EncinaError.Message never reaches a log or tag.

REFERENCE FILES:
- src/Encina.Marten/MartenAggregateRepository.cs, src/Encina/Abstractions/MultiTenancyMarker.cs
```

</details>

### Phase 3: Marten Store (pull request A3)

<details>
<summary>Tasks</summary>

1. **Create** `src/Encina.Actors.Marten/Encina.Actors.Marten.csproj` (references `Encina.Actors.EventSourcing`, `Encina.Marten`, `Marten`; `InternalsVisibleTo`), two PublicAPI files, `MartenActorEventStore.cs` (reads and appends through the secondary store's session, `StreamIdentity.AsString`, expected-version append, a concurrency exception becomes `append_conflict`, headers of row C5), `MartenActorSnapshotStore.cs` (a Marten document keyed by the same injective key), `IActorMartenStore.cs` (marker for `AddMartenStore`), `ServiceCollectionExtensions.cs` (`AddEncinaActorsMarten(Action<StoreOptions>)`).
2. **Modify** `Encina.slnx`, `tests/Encina.IntegrationTests/Encina.IntegrationTests.csproj` and `tests/Encina.GuardTests/Encina.GuardTests.csproj` (references).
3. Tests: new `tests/Encina.IntegrationTests/Actors/MartenActorEventStoreIntegrationTests.cs` and its fixture inside the shared Marten PostgreSQL collection (a second schema for the string-identity store): append and load, snapshot then events, a stale expected version is `append_conflict`, two tenants never read each other's stream, a command sequence through the real runtime produces the same state as `Evolve` over the stored events; `tests/Encina.GuardTests/Actors/MartenActorEventStoreGuardTests.cs`; manifest `Encina.Actors.Marten.json`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT:
Issue #330 plan docs/plans/event-sourced-actors-implementation-plan-330.md, phase 3. A1 and A2 merged.

TASK:
1. The Encina.Actors.Marten project: event store, snapshot store, secondary string-identity Marten store, registration.
2. Integration tests on Marten PostgreSQL through Testcontainers (shared collection), guard tests, manifest.

KEY RULES:
- String stream identity only on the secondary store; the aggregate store keeps Guid identity.
- Database calls asynchronous with the CancellationToken; the store registers so that it wins over the in-memory store in any order.
- Never describe an append as part of another component's transaction; it is one Marten commit per command.

REFERENCE FILES:
- src/Encina.Marten/ServiceCollectionExtensions.cs, tests/Encina.IntegrationTests/Infrastructure/Marten/Fixtures/MartenFixture.cs
```

</details>

### Phase 4: Observability and EventIds (pull request A4)

<details>
<summary>Tasks</summary>

1. **Create** `Diagnostics/ActorDiagnostics.cs` (row C6), `Diagnostics/ActorLog.cs` (`[LoggerMessage]`, ids 12550-12557: 12550 actor activated, 12551 actor passivated, 12552 command failed (code), 12553 append conflict, 12554 recovery failed (`ForLogging()`), 12555 passivation timer failed (`ForLogging()`), 12556 mailbox full, 12557 snapshot write failed (code)), `Health/ActorSystemHealthCheck.cs` (`DefaultName` const, `Tags` static array, active-actor count and store reachability through a scope).
2. **Modify** `src/Encina/Diagnostics/EventIdRanges.cs` (`EventSourcedActors = (12550, 12599)`), `PublicAPI.Unshipped.txt` of `Encina` and of the package, `docs/engineering/eventid-block-registry.md` (one row in each parsed table with identical bounds), `EncinaEventIdAllocationTests.cs` (both assemblies map to the range), `ActorInstance.cs`, `EventSourcedActorSystem.cs`, `Encina.json`, `Encina.Actors.EventSourcing.json`.
3. Tests: `ActorDiagnosticsTests` (no key, tenant or user id in any tag), `ActorSystemHealthCheckTests`; run `EventIdRegistryAgreementTests` and `EncinaEventIdAllocationTests`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT:
Issue #330 plan docs/plans/event-sourced-actors-implementation-plan-330.md, phase 4. A1-A3 merged.

TASK:
1. Diagnostics, log and health check; register EventIdRanges.EventSourcedActors (12550, 12599) FIRST; registry rows; allocation map.

KEY RULES:
- ADR-021 and ADR-054: ids packed 12550-12557 without gaps; no _total suffix; seconds with unit s; no actor key, tenant id, user id, command content or EncinaError.Message in any tag or log.
- The health check resolves scoped services through a scope.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs, docs/engineering/eventid-block-registry.md, src/Encina.Messaging/Health/SagaHealthCheck.cs
```

</details>

### Phase 5: Contract Base and Property Tests (pull request A5)

<details>
<summary>Tasks</summary>

1. **Create** `tests/Encina.TestInfrastructure/ActorsEventSourcing/ActorEventStoreContractBase.cs` (abstract: append and load, expected-version conflict, snapshot then events, tenant isolation), `tests/Encina.ContractTests/ActorsEventSourcing/InMemoryActorEventStoreContractTests.cs`, `tests/Encina.PropertyTests/ActorsEventSourcing/ActorPropertyTests.cs` (FsCheck: any command sequence through the runtime leaves a state equal to `Evolve` folded over the stored events; two distinct (tenant, name, key) triples never produce one stream key; commands sent concurrently never interleave).
2. **Modify** the TestInfrastructure, ContractTests and PropertyTests csproj references; the manifest. The Marten run of the contract base is a derived class added to the A3 integration file.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT:
Issue #330 plan docs/plans/event-sourced-actors-implementation-plan-330.md, phase 5. A1-A4 merged.

TASK:
1. The abstract store contract base, its in-memory run and the FsCheck properties.

KEY RULES:
- Contract and property tests instantiate real implementations; Shouldly through Encina.Testing.Shouldly; deterministic with a FakeTimeProvider.

REFERENCE FILES:
- tests/Encina.ContractTests/Marten/Core/IAggregateRepositoryContractTests.cs
```

</details>

### Phase 6: Documentation and Finalization (pull request A6)

<details>
<summary>Tasks</summary>

1. Through `docs-writer`: the two package READMEs, `docs/features/event-sourced-actors.md` (when to use actors or repositories with the issue's comparison table, a runnable example, tenancy, passivation and snapshots, running more than one process), the ADR draft (number assigned when the ADR pull request opens), `docs/INVENTORY.md`, `ROADMAP.md`.
2. `changelog.d/330-event-sourced-actors.added.md` through `mechanical-fixer`.
3. Build with zero warnings; every flag reaches its targets; the PR reports measured per-file coverage.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT:
Issue #330 plan docs/plans/event-sourced-actors-implementation-plan-330.md, phase 6. All code merged.

TASK:
1. Ask docs-writer for the READMEs, guide, ADR draft, INVENTORY and ROADMAP lines; the fragment through mechanical-fixer.

KEY RULES:
- Documentation never types coverage figures by hand (covref markers); public text never names a private client application.
- The guide states that two processes hosting one actor are protected by the expected-version append, not by placement.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md, docs/features/
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| Akka Persistence, Orleans grains, Proto.Actor persistence, Akkatecture (issue references) | Event-sourced actor model: mailbox, recovery, passivation, snapshots | Runtime semantics |
| `AGENTS.md` section 3 | ROP, timer callbacks never throw, `TimeProvider`, store wins over in-memory, event-sourced modules on Marten | Register rows, Design Choice 1 |
| ADR-054, ADR-062, ADR-021 | Telemetry, error categories, EventIds | Phase 4, row C3 |
| #388 and #1257 plans | Injective tenant-qualified keys, reserved `default` segment | Design Choice 3 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| Expected-version append and conflict mapping | `src/Encina.Marten/MartenAggregateRepository.cs`, `src/Encina.Marten/MartenErrorCodes.cs` | Behavior copied by the actor store |
| Event headers (`TenantId`, correlation, causation) | `src/Encina.Marten/EventMetadataEnrichmentService.cs` | Audit headers of row C5 |
| `ISnapshotStore<T>` and `SnapshotEnvelope` | `src/Encina.Marten/Snapshots/` | Pattern for the state-typed snapshot document |
| `MultiTenancyMarker`, `IRequestContext` | `src/Encina/Abstractions/` | Multi-tenancy detection and tenant id |
| `MartenFixture` and the Marten collection | `tests/Encina.IntegrationTests/Infrastructure/Marten/Fixtures/MartenFixture.cs` | Real Marten on PostgreSQL |
| `SagaHealthCheck` | `src/Encina.Messaging/Health/SagaHealthCheck.cs` | Health check pattern |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Actors.EventSourcing` and `Encina.Actors.Marten` (new field `EventSourcedActors`) | `12550-12599`, ids 12550-12557 used | Area Core and messaging, first free 50-id slot after the siblings of this family (`MartenTemporal` 12450-12499 of #314, `DurableExecution` 12500-12549 of #315) and the claims on main through 12449; both assemblies map to the one range; A4 registers it with one row in each parsed table of the registry |

### Estimated File Count

| PR | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| A1 | 9 (csproj, 2 PublicAPI, `IEventSourcedActorSystem`, `IActorRef`, `ActorCommandResult`, `EventSourcedActorOptions`, `EventSourcedActorErrorCodes`, `IActorEventStore` with the recovery hook declared in the same file) | 1 (`Encina.slnx`) | 2 | 2 (UnitTests and GuardTests csproj) | 1 | 0 | 15 | ~700 |
| A2 | 5 (`ActorKey`, `ActorInstance`, `EventSourcedActorSystem`, `InMemoryActorEventStore`, `ServiceCollectionExtensions`) | 1 (`PublicAPI.Unshipped.txt`) | 4 (`ActorKeyTests`, `ActorInstanceTests`, `EventSourcedActorSystemTests`, `EventSourcedActorsRegistrationTests`) | 0 | 1 | 0 | 11 | ~1,400 |
| A3 | 7 (csproj, 2 PublicAPI, `MartenActorEventStore`, `MartenActorSnapshotStore`, `IActorMartenStore`, `ServiceCollectionExtensions`) | 1 (`Encina.slnx`) | 3 (integration test, its fixture, guard test) | 2 (IntegrationTests and GuardTests csproj) | 1 | 0 | 14 | ~1,300 |
| A4 | 3 (`ActorDiagnostics`, `ActorLog`, `ActorSystemHealthCheck`) | 5 (`EventIdRanges.cs`, two `PublicAPI.Unshipped.txt` of `Encina` and the package, `ActorInstance.cs`, `EventSourcedActorSystem.cs`) | 2 | 1 (`EncinaEventIdAllocationTests.cs`) | 2 (`Encina.json`, `Encina.Actors.EventSourcing.json`) | 1 (registry) | 14 | ~900 |
| A5 | 0 | 0 | 3 | 3 (TestInfrastructure, ContractTests, PropertyTests csproj) | 1 | 0 | 7 | ~800 |
| A6 | 0 | 0 | 0 | 0 | 0 | 7 (two READMEs, guide, ADR draft, `INVENTORY.md`, `ROADMAP.md`, fragment) | 7 | ~500 |
| **Total** | **24** | **8** | **14** | **8** | **6** | **8** | **68** | **~5,600** |

Column check: 24 + 8 + 14 + 8 + 6 + 8 = 68 = 15 + 11 + 14 + 14 + 7 + 7. In A1 the nine new source files are the csproj, the two PublicAPI files and six type files. No existing interface changes (`IDecider` and `ActorId` are consumed, not changed), so there are no existing implementers or fakes to update; `IActorEventStore` has two implementers, both in this plan (`InMemoryActorEventStore`, `MartenActorEventStore`), verified on the branch with the declaration-form grep `class \w+\s*(\([^)]*\))?\s*:\s*[^{]*IActorEventStore`. `Encina.Actors.EventSourcing` and `Encina.Actors.Marten` are new packages, so their `PublicAPI.Unshipped.txt` files list every public symbol.

Per-file coverage targets (new packages; the manifests are created by A1 and A3 with these targets):

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `Runtime/ActorKey.cs` | 95 | 0 | 0 | 80 | 0 | Encode branches are table-driven unit tests; the property test proves injectivity |
| `Runtime/ActorInstance.cs` | 85 | 40 | 0 | 60 | 0 | Mailbox, passivation and conflict paths run with a fake clock; the property test proves ordering |
| `Runtime/EventSourcedActorSystem.cs` | 85 | 60 | 0 | 60 | 0 | Activation, tenant gate, unknown name and rejection paths over the in-memory store |
| `Runtime/InMemoryActorEventStore.cs` | 90 | 40 | 80 | 0 | 0 | The contract base runs against it |
| `EventSourcedActorOptions.cs` | 90 | 60 | 0 | 0 | 0 | Defaults and registration validation |
| `ActorCommandResult.cs` | 80 | 90 | 0 | 0 | 0 | Constructor null checks by guard tests |
| `ServiceCollectionExtensions.cs` (both packages) | 85 | 60 | 0 | 0 | 40 | Registration and order tests; the Marten path runs in the integration collection |
| `Marten/MartenActorEventStore.cs`, `MartenActorSnapshotStore.cs` | 0 | 60 | 60 | 0 | 85 | Behavior needs a real Marten store; guard covers arguments; the contract base runs on it |
| `Health/ActorSystemHealthCheck.cs` | 85 | 60 | 0 | 0 | 0 | Healthy, store-down and over-capacity branches |
| `Diagnostics/ActorDiagnostics.cs` | 80 | 0 | 0 | 0 | 0 | Listener test; internal static, no validated parameter |
| `Diagnostics/ActorLog.cs`, `EventSourcedActorErrorCodes.cs`, `IEventSourcedActorSystem.cs`, `IActorRef.cs`, `IActorEventStore.cs`, `IActorMartenStore.cs` | 0 | 0 | 0 | 0 | 0 | Source-generated log class, constants and interfaces: no executable code of their own |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #330 - actor-based event sourcing - in six small pull requests (A1 to A6), in order.
Plan: docs/plans/event-sourced-actors-implementation-plan-330.md. Prerequisites: #388 (Encina.Actors, ActorId) and #320 (IDecider) merged.

PROJECT CONTEXT:
- Encina .NET 10 / C# 14, pre-1.0, no [Obsolete], no compatibility layers. Edit with Edit/Write only; PublicAPI lines, manifests and
  changelog through mechanical-fixer; READMEs, the guide and the ADR through docs-writer.
- The runtime hosts a decider as an in-process actor; events are stored through IActorEventStore (in-memory for tests, Marten for real use).
- Do not implement Orleans hosting, Proto.Actor or Akka adapters.

IMPLEMENTATION OVERVIEW:
1. A1: Encina.Actors.EventSourcing contracts and options. 2. A2: runtime (key, mailbox, activation, passivation, in-memory store, DI).
3. A3: Encina.Actors.Marten (secondary string-identity store, event and snapshot stores). 4. A4: diagnostics, health check, EventIds 12550-12557.
5. A5: contract base and property tests. 6. A6: documentation.

KEY PATTERNS:
- ROP: Either everywhere; a Left from Decide is returned unchanged and nothing is appended; errors never swallowed; EncinaError.Message never logged.
- One command at a time per actor; expected-version append; a conflict drops the cached state; no automatic retry.
- Fail closed on tenants; injective percent-encoded key; reserved "default" segment never invented; time through TimeProvider.
- Timer callbacks and loops never throw; the singleton system creates a scope per command; a database store wins over the in-memory one in any order.
- Guard tests for every public constructor and method parameter.

REFERENCE FILES:
- src/Encina.Marten/MartenAggregateRepository.cs, src/Encina.Marten/EventMetadataEnrichmentService.cs, src/Encina/Diagnostics/EventIdRanges.cs
- docs/plans/virtual-actors-implementation-plan-388.md, docs/plans/decider-pattern-implementation-plan-320.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | An active actor holds its state in memory; that is its cache. The issue deferred state-snapshot caching; snapshots are stored (Design Choice 4), so nothing is left to defer |
| 2 | OpenTelemetry | ✅ Phase 4 | `Encina.Actors.EventSourcing` source and meter, register row C6; never keys or ids |
| 3 | Structured Logging | ✅ Phase 4 | `ActorLog` ids 12550-12557 of `EventSourcedActors`; code and exception type only |
| 4 | Health Checks | ✅ Phase 4 | `ActorSystemHealthCheck`; the issue deferred it, the plan integrates it because a runtime that holds state needs a probe |
| 5 | Validation | ❌ N/A | `Decide` returns a `Left` for an invalid command; request validation runs in the pipeline of the handler that calls the actor |
| 6 | Resilience | ❌ N/A | A failed activation drops the instance and the next call re-activates it from the store; no second retry layer; automatic command retry after a conflict is deliberately absent (register row C1) |
| 7 | Distributed Locks | ❌ N/A | One mailbox per actor and the expected-version append replace a lock, as the issue says |
| 8 | Transactions | ❌ N/A | Each command's events are one Marten commit of the actor store; the actor system is not part of `IUnitOfWork` and never claims atomicity with a caller's work |
| 9 | Idempotency | ❌ N/A | Duplicate commands are decided from state by the decider; delivery deduplication belongs to the caller's inbox; the issue deferred it with no owner, so no issue is opened |
| 10 | Multi-Tenancy | ✅ Phase 2 | Tenant-qualified injective key, fail closed (Design Choice 3); the issue deferred it, the plan integrates it because the key format is hard to change later |
| 11 | Module Isolation | ❌ N/A | Streams carry no module identifier in `Encina.Marten`; module-scoped actor systems are a deployment concern |
| 12 | Audit Trail | ✅ Phase 3 | The event stream is the audit trail; the store writes tenant, correlation and causation headers (row C5) |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| `Encina.Actors` (`ActorId`) | Planned in #388 (G1) | Not on main yet; A1 starts after it merges |
| `IDecider` | Planned in #320 | Not on main yet; A1 starts after it merges |
| Marten fixture | Merged | `MartenFixture` |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Orleans hosting of the same deciders | #459, #388 | A grain-based host reusing `IActorEventStore` |
| Tenant tag helper | #2676 | A4 adds the tenant tag when it has merged |
| Marten multi-tenancy | #326 | Per-tenant sessions for the aggregate store, independent of this plan's secondary store |
| Snapshot strategies | #323 | Aggregate snapshots; actor snapshots here are count-based only |

---

## Next Steps

1. Link this plan from #330 and ask the maintainer to answer Design Choices 1, 2 and 3 when work starts; tell #388 and #320 that this plan consumes `ActorId` and `IDecider`.
2. Implement A1 to A6 in order (A4 and A5 are independent after A3).
3. Follow-ups recorded here: a grain host for the same deciders (with #459), and Proto.Actor or Akka.NET adapters if demand appears. Maintainer action: amend the acceptance criteria of #330 that name `IEventSourcedActor`, `EventSourcedActorBase`, `AddEncinaActors` with `ActorPersistenceProvider`, the Orleans and Proto.Actor packages and "Example application" to the outcome of this plan (this plan never comments on an issue).
