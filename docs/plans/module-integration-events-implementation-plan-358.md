# Implementation Plan: Inter-Module Communication (integration events, `IIntegrationEventBus`, module public API, contract validation)

> **Issue**: [#358](https://github.com/dlrivada/Encina/issues/358)
> **Type**: Feature
> **Complexity**: High (6 phases, 6 pull requests of 5, 10, 13, 10, 12 and 4 files)
> **Estimated Scope**: about 900 lines of production code and about 2,000 lines of tests, split over six pull requests
> **Family**: modular monolith (#352 umbrella, #360 graph and exports, #359, #364); this plan owns the integration event contract and the module event bus

---

## Summary

The issue asks for a distinction between domain events and integration events, an in-memory `IIntegrationEventBus`, a module public API pattern, domain and integration dispatchers, outbox integration and module dependency validation. State of `origin/main` (`db1e7f688`), verified by reading and grep:

| Issue item | Today | Owner of what remains |
|------------|-------|-----------------------|
| `IDomainEvent` | `Encina.DomainModeling.IDomainEvent` (`EventId`, `OccurredAtUtc`), `DomainEvent` and `RichDomainEvent` records; not an `INotification` | Unchanged here; delivery is #1236 (Design Choice 4) |
| `IIntegrationEvent` | `Encina.DomainModeling.IIntegrationEvent` (`EventId`, `OccurredAtUtc`, `EventVersion`, `CorrelationId`) and the `IntegrationEvent` record; **not an `INotification`**, so `IEncina.Publish` cannot carry it; no `SourceModule` | This plan, Phase 1 |
| `DomainEventDispatcher` (same transaction) | `DomainEventDispatcherInterceptor` (EF Core only, publishes after `SaveChanges`); `DomainEventDispatchHelper` for manual dispatch | #1236 (domain events to the outbox on all 10 providers) |
| Domain to integration mapping | `IDomainEventToIntegrationEventMapper`, async and fallible variants, `IntegrationEventExtensions` | Reused as is |
| Dispatch strategy (Sequential or Parallel) | `NotificationDispatchOptions` and `SequentialDispatchStrategy`, `ParallelDispatchStrategy`, `ParallelWhenAllDispatchStrategy` in core `Encina` | Reused (Design Choice 8) |
| `IIntegrationEventBus`, in-memory | Missing | This plan, Phase 2 |
| `IModulePublicApi` | Missing; `Encina.Testing` already assumes the naming (`ModuleArchitectureRules.ModuleApiInterfacesShouldFollowNamingConvention` requires names ending with `ModuleApi`; `MockModuleApi<T>`) | This plan, Phase 3 |
| Outbox integration | `IOutboxStore` on 10 providers, `OutboxWriter` (internal), `TransactionalOutboxPipelineBehavior` for `IHasNotifications` | This plan, Phase 4 |
| Module dependency validation | `BoundedContextValidator.ValidateEventContracts` in `Encina.DomainModeling` works on a parallel `IBoundedContextModule` type, not on `IModule` | This plan, Phase 3; unification of the two is a follow-up issue file |
| `IModuleEventBus` of #352 | Missing | Not created: #352's Design Choice 8 keeps one bus |

Facts that shape the design:

1. **`IIntegrationEvent` is not publishable today.** `IEncina.Publish<TNotification>` requires `TNotification : INotification` (`src/Encina/Abstractions/IEncina.cs:89`). Making the contract an `INotification` is the first change.
2. **Transactions are not this plan's to claim.** `OutboxWriter.WriteAsync` adds rows and calls `SaveChangesAsync` itself (`src/Encina.Messaging/Outbox/OutboxWriter.cs`); whether a row commits with a business change depends on the transaction lease of the enclosing request, which #1236 and #718 work on, and the saga commit owner is `SagaOrchestrator.CommitAsync` of #720. This plan makes no atomicity claim: outbox mode writes through the shared writer and gives at-least-once delivery.
3. **Persisted outbox rows carry no tenant** (#737 is open). An outbox bus in a multi-tenant application would lose the tenant of the event, so it fails closed (Design Choice 6).
4. **Domain events and integration events live in `Encina.DomainModeling`, the module registry in core `Encina`**, and `Encina.Messaging` references only `Encina` (its project file has one `ProjectReference`). The bus therefore sits in `Encina.DomainModeling` and the outbox variant in `Encina.Messaging` (Design Choice 1).

Delivered: `IIntegrationEvent : INotification` with `SourceModule`; `IIntegrationEventBus` and its in-memory implementation with source and export checks; `IModulePublicApi` and a startup contract validator; an optional outbox bus; telemetry, logging (EventIds 7630-7639) and a guide page.

**Provider category**: none for the bus. Outbox mode uses `IOutboxStore`, which every one of the 10 database providers implements; provider coherence is inherited from the store contract and is not re-implemented. Message brokers, caching and locks are not involved.

**Terminology**: ADR-050 calls an Encina feature (Outbox, Inbox) a "module"; this plan says "application module" for `IModule`.

---

## Design Choices

<details>
<summary><strong>1. Where the bus and the contracts live (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The bus abstraction, the in-memory bus and the contract validator in `Encina.DomainModeling` (next to `IIntegrationEvent`); the outbox bus in `Encina.Messaging`, which gains a `ProjectReference` to `Encina.DomainModeling`** | No type moves; module-aware code reads core `IModuleExecutionContext` and `IModuleGraph`; opt-in outbox | `Encina.Messaging` depends on `Encina.DomainModeling` (light: LanguageExt, DI and logging abstractions) |
| **B) Move `IIntegrationEvent` and `IntegrationEvent` into core `Encina.Modules` and put the bus in core** | Core holds the whole module story; `Encina.Messaging` needs no new reference | Moves two shipped public types (namespace change) and puts a DDD concept in core |
| **C) A new `Encina.Modules.Messaging` package for the bus and the outbox variant** | Keeps both packages unchanged | A new package for about six types |

### Chosen Option: **A — bus in `DomainModeling`, outbox variant in `Messaging`** (recommended, pending the maintainer)

### Rationale

A wins when the existing DDD contracts should stay where users already import them. B wins if the maintainer wants the module system self-contained in core. C wins when the dependency from Messaging to DomainModeling is unacceptable. The issue's `Encina.Modules` package does not exist; the module code is core `Encina`.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. How an integration event names its source module (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `string SourceModule { get; }` is declared by the event (abstract on the `IntegrationEvent` base); the bus rejects an event whose `SourceModule` is not the ambient module, or not a registered module when there is no ambient module** | Explicit in the contract and visible to analyzers; an event cannot claim another module as its origin | Every event declares a string (a constant in practice) |
| **B) The bus stamps the module** | Nothing to declare | Events are records with `init` properties and the interface is read-only; stamping needs a wrapper type and hides the origin from the contract |
| **C) An envelope carries the module, the event stays plain** | Plain events | Handlers receive the envelope, changing every handler signature |

### Chosen Option: **A — the event declares it, the bus verifies it** (recommended, pending the maintainer)

### Rationale

A wins when the contract itself must tell a consumer, an analyzer or a future broker who published the event; the check closes the spoofing hole that a declared string would otherwise open (a module could publish events in another module's name). C wins when events must stay free of module names (generic shared contracts). Errors: `encina.modules.event_source_mismatch` (`ErrorCategory.Forbidden`) and `encina.modules.event_source_unknown` (`ErrorCategory.InvalidRequest`), both ADR-062.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. `IIntegrationEvent` becomes an `INotification` (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IIntegrationEvent : INotification`; handlers are ordinary `INotificationHandler<TEvent>`; `IntegrationEventCollector` and `Encina.Testing` already collect `INotification`** | One handler model; the pipeline, the outbox (`IOutboxStore` stores notifications) and tests work unchanged | Integration events reference `Encina` core |
| **B) Keep the contract free of `INotification` and publish an `IntegrationEventEnvelope<T> : INotification`** | Events have no dependency on Encina | Handlers take the envelope; two shapes for one concept |

### Chosen Option: **A — `IIntegrationEvent : INotification`** (recommended, pending the maintainer)

### Rationale

A wins because `Encina.DomainModeling` already references core `Encina` and every consumer in the framework (outbox serialization by runtime type, test collectors, the dispatch strategies) is built on `INotification`. B wins only for contract assemblies that must not depend on Encina, which the issue's "same event contracts for microservices" goal does not require.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Domain events are not changed here</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Leave `IDomainEvent` and its delivery to #1236; this plan only converts domain events to integration events through the existing mappers** | No overlap with the open outbox work; no duplicated dispatcher | The issue's `DomainEventDispatcher` and `IntegrationEventDispatcher` are not new types |
| **B) Add a second dispatcher pair in this plan** | Matches the issue text | Two dispatch paths for domain events, one of them unfinished |

### Chosen Option: **A — reuse and leave to #1236**

### Rationale

The EF Core `DomainEventDispatcherInterceptor` and `DomainEventDispatchHelper` exist and #1236 plans the delivery of domain events to the outbox on 10 providers; a second dispatcher would collide with it (`AGENTS.md` section 3: no parallel layers).

**Class:** Settled
**Settled by:** #1236 (open, owns domain event delivery) and `AGENTS.md` section 3 (no compatibility layers).

</details>

<details>
<summary><strong>5. Publishing several events (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `PublishAsync(IEnumerable<IIntegrationEvent>)` publishes in order and stops at the first `Left`, returning it; earlier events stay published** | Deterministic order; the caller sees the first error and which events went out | Later events are not attempted |
| **B) Attempt every event and return an aggregate error** | No event is skipped | An aggregate error type that the project does not have; order of side effects continues after a failure |

### Chosen Option: **A — ordered, stop at the first failure** (recommended, pending the maintainer)

### Rationale

A wins when events of one batch depend on order (a state change event before a follow-up event). B wins for independent notifications where partial delivery is preferable to an early stop. The result never reports success for a failed event, and no atomicity is claimed for the batch.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Outbox mode and multi-tenancy (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AddEncinaIntegrationEventOutbox()` (in `Encina.Messaging`) replaces the bus with one that writes outbox rows; when multi-tenancy is on (`MultiTenancyMarker` registered) and `OutboxMessage` carries no tenant, registration throws `InvalidOperationException`; the check disappears when #737 lands** | Fail-closed; no cross-tenant delivery; opt-in | Outbox mode is unavailable to multi-tenant applications until #737 |
| **B) Serialize the tenant into the event payload and restore it in the consumer** | Works before #737 | A second, unreviewed tenant carrier; a consumer that forgets to restore it runs under the wrong tenant |
| **C) Defer the whole outbox mode to a later pull request after #737** | No guard code | The acceptance criterion "optional outbox integration" is not met now |

### Chosen Option: **A — fail closed until the outbox row carries the tenant** (recommended, pending the maintainer)

### Rationale

A wins when tenant isolation must not depend on every consumer being careful, which is the project's compliance rule (`AGENTS.md` section 3, gates fail closed when context is missing). B wins if a multi-tenant application needs outbox events before #737. C wins if the maintainer prefers no temporary guard. The in-memory bus is unaffected: the ambient tenant flows through the in-process call.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Contract validation: when it fails the application (Dilemma D7)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A startup validator checks that each integration event handled by a module is exported by exactly one other module, each registered `IModulePublicApi` interface is exported by its own module, and the subscriber declared a dependency on the publisher (#360); with `EnforceModuleBoundaries` on (#352) a violation throws `InvalidOperationException` listing every violation, otherwise each is logged as a warning** | Tied to the same switch as the request boundary; no surprises for applications that do not enforce | Violations are only warnings until enforcement is on |
| **B) Always throw** | Strict | Breaks an application that adopted events before exports |
| **C) Always warn** | Never blocks | A broken contract only shows in logs |

### Chosen Option: **A — enforced with the module boundary switch** (recommended, pending the maintainer)

### Rationale

A wins when one opt-in controls all module architecture rules. B wins for teams that want contract errors at the first start. The validator replaces nothing: `BoundedContextValidator` keeps working for `IBoundedContextModule` (unification is a follow-up issue file).

**Class:** Dilemma

</details>

<details>
<summary><strong>8. Dispatch strategy: reuse the global option</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The bus calls `IEncina.Publish`, so `NotificationDispatchOptions` (Sequential, Parallel, ParallelWhenAll with `MaxDegreeOfParallelism`) decides handler dispatch** | One strategy for all notifications; already tested | Cannot differ between domain and integration events |
| **B) A separate `IntegrationEventDispatchStrategy` on the bus options** | Per-bus control | A second source of truth for the same concept |

### Chosen Option: **A — reuse `NotificationDispatchOptions`**

### Rationale

The issue's `DispatchStrategy.Sequential` or `Parallel` is exactly `NotificationDispatchStrategy` (`src/Encina/Core/Encina.cs:53-58`); duplicating it would create two configuration surfaces.

**Class:** Trivial
**Settled by:** existing option `NotificationDispatchOptions` (`src/Encina/Core/Encina.cs:39-58`).

</details>

<details>
<summary><strong>9. The module public API marker</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IModulePublicApi` (marker, in core `Encina.Modules`); the implementation class is internal to the module; the interface is listed in the module's `Exports` (#360); names end with `ModuleApi` as the existing architecture rule requires** | Matches the issue and the existing rule and `MockModuleApi<T>` | A marker adds no behavior by itself |
| **B) No marker; exports alone identify a public API** | Fewer types | The validator cannot tell an API interface from any exported type |

### Chosen Option: **A — marker plus exports**

### Rationale

The marker is what lets the validator and the architecture rules find API interfaces. The convention is already enforced by `ModuleArchitectureRules.ModuleApiInterfacesShouldFollowNamingConvention` (`HaveNameEndingWith("ModuleApi")`).

**Class:** Trivial
**Settled by:** `src/Encina.Testing/Modules/ModuleArchitectureRules.cs` (`ModuleApiInterfacesShouldFollowNamingConvention`) and `src/Encina.Testing/Modules/MockModuleApi.cs`.

</details>

---

## Implementation Phases

### Phase 1: The integration event contract (pull request 1)

<details>
<summary>Tasks</summary>

1. `src/Encina.DomainModeling/IIntegrationEvent.cs`: `IIntegrationEvent : INotification`, new member `string SourceModule { get; }` (XML docs: the name of the application module that publishes the event, case-insensitive); the abstract record `IntegrationEvent` gains `public abstract string SourceModule { get; }` (D2, D3). The existing `DateTime OccurredAtUtc`, `EventVersion` and `CorrelationId` stay.
2. `IntegrationEventExtensions.cs` needs no change (it maps through the mapper interfaces); verify the 21 references still compile.
3. Tests: unit `IntegrationEventTests` (new, `tests/Encina.UnitTests/DomainModeling/`: the record defaults, the abstract member, `INotification` assignability and a publish through a real `IEncina` to an `INotificationHandler<TEvent>`), contract `IntegrationEventContractTests` (a real subclass keeps the interface contract), guard not needed (no parameterized public member added).
4. PublicAPI (`Encina.DomainModeling`), manifest `Encina.DomainModeling.json` through `mechanical-fixer`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina.DomainModeling.IIntegrationEvent is a plain marker with EventId, OccurredAtUtc, EventVersion and CorrelationId and is not an INotification, so IEncina.Publish cannot carry it. D2 (declared SourceModule) and D3 (INotification) are implemented as the maintainer answered, the recommended options until then.
TASK: Make IIntegrationEvent an INotification, add SourceModule to the interface and the abstract base record, and add the tests listed.
KEY RULES: No [Obsolete] and no default implementation that hides the member; keep OccurredAtUtc as the existing UTC DateTime from TimeProvider; XML docs on every member; a test publishes a real event through IEncina.
REFERENCE FILES: src/Encina.DomainModeling/IIntegrationEvent.cs, src/Encina.DomainModeling/IntegrationEventExtensions.cs, src/Encina/Abstractions/INotification.cs, tests/Encina.UnitTests/DomainModeling/BoundedContextTests.cs.
```

</details>

### Phase 2: The in-memory bus (pull requests 2 and 3)

Pull request 2 carries items 1, 2, 3, 6 and the tests of the checks; pull request 3 carries the observability items 4 and 5 and the telemetry registration, so each stays within 15 files.

<details>
<summary>Tasks</summary>

1. `src/Encina.DomainModeling/IIntegrationEventBus.cs`: `ValueTask<Either<EncinaError, Unit>> PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : IIntegrationEvent` and `ValueTask<Either<EncinaError, Unit>> PublishAsync(IEnumerable<IIntegrationEvent> events, CancellationToken ct = default)` (ROP, not `Task` as in the issue).
2. `src/Encina.DomainModeling/InMemoryIntegrationEventBus.cs` (internal sealed): constructor `(IEncina encina, IModuleExecutionContext moduleContext, IModuleRegistry registry, IModuleGraph graph, ModuleBoundaryOptions boundaries, ILogger<...>)`; checks (D2): non-blank `SourceModule`, ambient module equals `SourceModule` (OrdinalIgnoreCase) or, with no ambient module, `SourceModule` is registered; when boundaries are enforced the event type must be in `graph.GetExports(SourceModule)`; then `_encina.Publish(@event, ct)` and returns its result unchanged (a `Left` from a handler is returned, never swallowed); batch per D5. The bus is registered as scoped because `IEncina`'s scope handling and `IModuleExecutionContext` are scoped.
3. `src/Encina.DomainModeling/IntegrationEventErrors.cs`: factories for `encina.modules.event_source_mismatch` (`ErrorCategory.Forbidden`), `encina.modules.event_source_unknown` (`InvalidRequest`), `encina.modules.event_not_exported` (`Forbidden`) through `EncinaErrors.Create(code, category, message)`; messages name modules and the event type name only.
4. `src/Encina.DomainModeling/Diagnostics/IntegrationEventLog.cs` (`[LoggerMessage]`, EventIds 7630-7635 for published, failed, mismatch, unknown source, not exported, batch stopped) and `IntegrationEventTelemetry.cs` (an `ActivitySource` and a `Meter` named `Encina.DomainModeling.IntegrationEvents`; counter `encina.integration_events.published_total` with tags `event_type`, `source_module`, `outcome` and, when `MultiTenancyMarker` is registered, the tenant tag required by ADR-054 D4); the OpenTelemetry package registers the source and the meter (`src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs`).
5. `EventIdRanges.ModulesIntegrationEvents = (7630, 7644)` and the `Encina.DomainModeling` entry of the allocation test map.
6. `src/Encina.DomainModeling/IntegrationEventServiceCollectionExtensions.cs`: `AddEncinaIntegrationEvents(this IServiceCollection)`, `TryAddScoped<IIntegrationEventBus, InMemoryIntegrationEventBus>()`; throws when `IModuleRegistry` is not registered (the bus needs `AddEncinaModules`, a registration error rather than a bus that skips its checks).
7. Tests: unit (every check, a handler `Left` returned, order and stop of a batch, the export check on and off), guard, DI registration test with `ValidateOnBuild` and `ValidateScopes`, property (any event whose source equals the ambient module and is exported publishes; any other source is denied), contract (`IIntegrationEventBus` behavior with the real bus), allocation map edit; PublicAPI, manifests of `Encina.DomainModeling` and `Encina.OpenTelemetry`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Pull request 1 made IIntegrationEvent an INotification with SourceModule; #360's IModuleGraph and #352's ModuleBoundaryOptions and context producer are on main. D1 (bus in DomainModeling), D5 (ordered batch, stop at first failure) and D8 (global dispatch strategy) apply.
TASK: Add the bus interface, the in-memory bus, the errors, the logs and telemetry, the EventId range, the registration and every test listed.
KEY RULES: A check failure is Left(EncinaError) with the category above, never an exception; a handler failure is returned, never swallowed and never logged as success; logs and tags carry codes, module names and the event type name only (never EncinaError.Message or event content); the tenant tag only with multi-tenancy on; no scoped service injected into a singleton; no atomicity claim about batches.
REFERENCE FILES: src/Encina.DomainModeling/DomainEventDispatchHelper.cs, src/Encina/Abstractions/IEncina.cs, src/Encina/Core/Encina.cs, src/Encina/Results/EncinaErrors.cs, src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs.
```

</details>

### Phase 3: `IModulePublicApi` and the contract validator (pull request 4)

<details>
<summary>Tasks</summary>

1. `src/Encina/Modules/IModulePublicApi.cs` (core): marker interface (D9).
2. `src/Encina.DomainModeling/ModuleContractValidator.cs` (internal): given `IModuleGraph`, `IModuleRegistry`, the registered `INotificationHandler<>` service descriptors for `IIntegrationEvent` types (the handler's module comes from `IModuleHandlerRegistry`) and the registered `IModulePublicApi` services, it computes the violations of D7: an event handled in module B that no other module exports, an event exported by two modules, an API interface not exported by the module that owns its implementation, a subscriber with no declared dependency on the publisher.
3. `src/Encina.DomainModeling/ModuleContractStartupValidator.cs` (internal hosted service): runs once at start; with `EnforceModuleBoundaries` on throws `InvalidOperationException` listing all violations, otherwise logs each (EventIds 7636 failed, 7637 validated).
4. Tests: unit (each violation kind, no violations, enforcement on and off), guard, property (generated module and event sets: no violation is reported when every handled event is exported by exactly one other module that the subscriber depends on); PublicAPI (core and DomainModeling), manifests.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: The bus enforces source and exports at publish time; this phase checks the contracts once at startup. D7 (enforced with the boundary switch) applies; #360 supplies IModuleGraph and exports and #352 the options.
TASK: Add the marker, the pure validator, the startup hosted service and the tests.
KEY RULES: The validator is a pure function over its inputs (every branch testable); one InvalidOperationException lists every violation (no first-error-only); logs carry module names and type names only; the hosted service fails the start rather than reporting success; CRAP <= 10 per method.
REFERENCE FILES: src/Encina.DomainModeling/BoundedContext.cs (BoundedContextValidator.ValidateEventContracts), src/Encina/Modules/ModuleHandlerRegistry.cs, src/Encina.Testing/Modules/ModuleArchitectureRules.cs, tests/Encina.UnitTests/DomainModeling/BoundedContextTests.cs.
```

</details>

### Phase 4: Outbox mode (pull request 5)

<details>
<summary>Tasks</summary>

1. `src/Encina.Messaging/Encina.Messaging.csproj`: `ProjectReference` to `Encina.DomainModeling` (D1).
2. `src/Encina.Messaging/Outbox/OutboxIntegrationEventBus.cs` (internal sealed, scoped): performs the same checks as the in-memory bus (shared through an internal checker in `Encina.DomainModeling` exposed with `InternalsVisibleTo` for `Encina.Messaging`, or duplicated in a small public static `IntegrationEventChecks`, decided at implementation by the smaller surface), then writes the event through the shared writer used by `TransactionalOutboxPipelineBehavior` (`OutboxWriter.WriteAsync`), returning its `Either`; a failed write is returned, never reported as published. No atomicity claim: the row commits with the business change only when the enclosing request holds the transaction lease that #1236 and #718 provide.
3. `src/Encina.Messaging/Outbox/IntegrationEventOutboxServiceCollectionExtensions.cs`: `AddEncinaIntegrationEventOutbox(this IServiceCollection)`: decorates (or replaces) the bus registration regardless of registration order, requires `IOutboxStore` to be registered and, with `MultiTenancyMarker` present, throws `InvalidOperationException` until the outbox row carries the tenant (D6).
4. Logs 7638 (stored, Debug) and 7639 (store failed, Error by error code); the allocation test map gains the new range for `Encina.Messaging`.
5. Tests: unit with `FakeOutboxStore` (a published event produces one row typed by its runtime type; a store `Left` is returned; the multi-tenant guard throws; registration order does not matter), guard, DI test (`ValidateOnBuild`, `ValidateScopes`), integration `IntegrationEventOutboxIntegrationTests` on PostgreSQL through ADO.NET (`[Collection("ADO-PostgreSQL")]`: publish, process with the real outbox processor, handler receives the event), contract (the outbox bus satisfies `IIntegrationEventBus`); the outbox store itself keeps its existing 10-provider contract and integration coverage.
6. PublicAPI, manifest `Encina.Messaging.json`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT: Outbox mode is opt-in. OutboxWriter is internal to Encina.Messaging and calls SaveChangesAsync itself; the tenant is not stored on outbox rows until #737. D6 (fail closed with multi-tenancy) applies.
TASK: Add the project reference, the outbox bus, the registration extension, logs and tests including one real-database integration test.
KEY RULES: Never claim atomic, same-transaction or after-save behavior (the orchestrators own commit: #1236, #718, #720); a Left from the store is returned; a database store or outbox registration must win over the in-memory default in any registration order without overriding an application registration; shared [Collection] fixtures only, ClearAllDataAsync in InitializeAsync.
REFERENCE FILES: src/Encina.Messaging/Outbox/OutboxWriter.cs, src/Encina.Messaging/Outbox/TransactionalOutboxPipelineBehavior.cs, src/Encina.Messaging/Outbox/IOutboxStore.cs, src/Encina.Testing.Fakes (FakeOutboxStore), tests/Encina.IntegrationTests/ADO/PostgreSQL.
```

</details>

### Phase 5: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. OpenTelemetry (function 2): activity and counter of Phase 2, tenant tag only with multi-tenancy on (ADR-054 D4).
2. Logging (function 3): `IntegrationEventLog`, EventIds 7630-7639 in the registered range.
3. Multi-tenancy (function 10): the in-memory bus keeps the ambient tenant of the call; outbox mode fails closed until #737 (D6); the bus resolves no tenant itself, so it calls neither `ResolveRead` nor `ResolveWrite` of #1257 and #798. The feature creates no tenant-qualified identifier: the event id is a `Guid`, so the reserved `default` tenant segment and injectivity rules do not apply here; the follow-up issue file for idempotent consumers states that any dedupe key must be the injective pair (tenant, `EventId`).
4. Error codes and categories (ADR-062): `encina.modules.event_source_mismatch` Forbidden, `encina.modules.event_source_unknown` InvalidRequest, `encina.modules.event_not_exported` Forbidden.
5. Module isolation (function 11): source verification, exports and declared dependencies.
6. Deferred, as issue files in this plan's worktree: idempotent consumption (`artifacts/issues/module-integration-event-idempotent-consumers.md`), audit of cross-module events (`artifacts/issues/module-cross-module-audit.md`), unification of `IBoundedContextModule` with the module system (`artifacts/issues/module-bounded-context-unification.md`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT: The bus is the in-process (and optional outbox) channel between application modules; deferred items are tracked in issue files.
TASK: Verify the integrations above exist in code and tests, and that the three issue files are written.
KEY RULES: Never log EncinaError.Message or event content; the tenant tag only with multi-tenancy; every new code has its ErrorCategory.
REFERENCE FILES: AGENTS.md sections 3 and 6, docs/architecture/adr/054-observability-conventions-and-trace-propagation.md, docs/architecture/adr/062-error-category-contract.md.
```

</details>

### Phase 6: Testing, documentation and finalization

<details>
<summary>Tasks</summary>

Per-flag targets for the files this work adds or changes, entered in the manifest of each package in the pull request that adds their tests. Baselines of the edited files (`IIntegrationEvent.cs`, `EventIdRanges.cs`) are measured at work start with `dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage" --results-directory artifacts\coverage\<Flag>Tests` per flag and `dotnet run --file .github/scripts/coverage-report.cs -- --input artifacts/coverage --output artifacts/coverage-report`; each target is the measured value or higher, never below it.

| File | unit | guard | contract | property | integration |
|------|:---:|:---:|:---:|:---:|:---:|
| `Encina.DomainModeling/IIntegrationEvent.cs` | 95 | 0 | 100 | 0 | 0 |
| `Encina.DomainModeling/InMemoryIntegrationEventBus.cs` | 95 | 70 | 90 | 80 | 0 |
| `Encina.DomainModeling/IntegrationEventErrors.cs` | 100 | 80 | 100 | 0 | 0 |
| `Encina.DomainModeling/ModuleContractValidator.cs` | 100 | 0 | 0 | 90 | 0 |
| `Encina.DomainModeling/ModuleContractStartupValidator.cs` | 90 | 60 | 0 | 0 | 0 |
| `Encina.Messaging/Outbox/OutboxIntegrationEventBus.cs` | 90 | 60 | 90 | 0 | 80 |

| File and flag | Justification |
|---------------|---------------|
| `IIntegrationEvent.cs` unit 95, contract 100 | The record defaults and the interface contract run from unit and contract tests with a real subclass; the base record's property initializers are the only lines unit tests do not distinguish. |
| `InMemoryIntegrationEventBus.cs` unit 95, guard 70, contract 90, property 80 | Every check and the batch loop are unit-testable with real `IEncina`; guard tests reach the constructor and null event checks; the property test covers generated source and export combinations; integration 0 because the bus has no external dependency. |
| `IntegrationEventErrors.cs` unit 100, guard 80, contract 100 | One-line factories; the contract test asserts each code and its `ErrorCategory` on the real error. |
| `ModuleContractValidator.cs` unit 100, property 90 | A pure function whose every violation kind is a unit test; the property test generates module and event sets. |
| `ModuleContractStartupValidator.cs` unit 90, guard 60 | The enforce and warn paths run with fakes; guard tests reach the constructor checks only. |
| `OutboxIntegrationEventBus.cs` unit 90, guard 60, contract 90, integration 80 | The write, the failure and the guard run with `FakeOutboxStore`; integration runs the end-to-end flow on PostgreSQL and misses only store failure paths. |

1. **Unit, Guard, Contract, Property, Integration** as listed in the phases; **Load**: justification file `tests/Encina.LoadTests/Modules/IntegrationEventBus.md` (the in-memory bus adds checks and delegates to `IEncina.Publish`, whose load tests exist); **Benchmark**: justification file `tests/Encina.BenchmarkTests/Modules/IntegrationEventBus.md` (the publish path is the existing notification benchmark plus a constant-time check). Both ship in pull request 3.
2. **Documentation (pull request 6)**: `docs/features/integration-events.md` through `docs-writer` (domain versus integration events, the bus, source and export rules, the module API pattern, outbox mode and its limits), XML docs with examples, `docs/INVENTORY.md`, `ROADMAP.md`, `changelog.d/358-module-integration-events.added.md` through `mechanical-fixer`; no ADR of its own: the shared module visibility ADR belongs to #352 (ADR number assigned when that ADR pull request opens).
3. `dotnet build --configuration Release` with zero warnings; all tests green; every flag at its target; CRAP table of every changed method below 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT: Per-flag coverage targets live in .github/coverage-manifest/*.json; targets never go down; tests execute real code; integration for a database-backed mode uses a real database.
TASK: Write the tests, the two justification files, the manifests with one-sentence justifications in the same pull request as their tests, the guide, the inventory and roadmap lines and the changelog fragment through the specialists that own them.
KEY RULES: Shouldly through Encina.Testing.Shouldly; no Thread.Sleep; no hand-typed coverage figures in docs; never edit CHANGELOG.md; justification files only for load and benchmark.
REFERENCE FILES: .github/coverage-manifest/Encina.DomainModeling.json, .github/coverage-manifest/Encina.Messaging.json, tests/Encina.UnitTests/DomainModeling/BoundedContextTests.cs, .claude/skills/encina-docs/SKILL.md, changelog.d/README.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard / Spec | Relevance | Notes |
|-----------------|-----------|-------|
| `AGENTS.md` sections 3 and 6 | ROP, errors never swallowed, fail-closed gates, 12 functions | Bus returns `Either`; outbox guard |
| ADR-021 | EventId ranges registered first | `ModulesIntegrationEvents` 7630-7644 |
| ADR-054 D4 | Tenant tag only with multi-tenancy | Published counter |
| ADR-062 | `ErrorCategory` on every code | Three codes above |
| Domain versus integration events (Microsoft architecture guide, Milan Jovanovic) | Named by the issue | Concept source |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IIntegrationEvent`, `IntegrationEvent`, mapper interfaces | `src/Encina.DomainModeling/IIntegrationEvent.cs`, `IntegrationEventExtensions.cs` | The contract, extended |
| `IDomainEvent`, `DomainEventDispatchHelper` | `src/Encina.DomainModeling/` | Unchanged (#1236) |
| `IEncina.Publish`, `NotificationDispatchOptions` | `src/Encina/Abstractions/IEncina.cs`, `src/Encina/Core/Encina.cs` | Dispatch and strategy |
| `IModuleExecutionContext`, `IModuleRegistry`, `IModuleHandlerRegistry` | `src/Encina/Modules/` | Ambient module, registered modules, handler to module |
| `IModuleGraph` (exports, dependencies), `ModuleBoundaryOptions` | #360, #352 | Export and dependency checks |
| `OutboxWriter`, `IOutboxStore`, `IHasNotifications`, `TransactionalOutboxPipelineBehavior`, `OutboxProcessorBase` | `src/Encina.Messaging/Outbox/` | Outbox mode and its processing |
| `FakeOutboxStore` | `src/Encina.Testing.Fakes` | Unit tests |
| `IntegrationEventCollector`, `ModuleTestFixture`, `MockModuleApi<T>`, `ModuleArchitectureRules` | `src/Encina.Testing/Modules/` | Test and architecture companions |
| `BoundedContextValidator`, `IBoundedContextModule` | `src/Encina.DomainModeling/BoundedContext.cs` | Parallel concept; follow-up unification |
| `OutboxHealthCheck` | `src/Encina.Messaging/Health/` | Health of outbox mode |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.DomainModeling` and `Encina.Messaging` (`ModulesIntegrationEvents`, new) | 7630-7644, ids 7630-7639 used | From the assigned block 7600-7649 (registry `artifacts/decisions/plan-batch/eventid-blocks.md`); the assembly map lists the range under both assemblies; #360 7600-7619, #352 7620-7629, #364 7645-7649 |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production code | 17 | `IIntegrationEvent` edit; `IIntegrationEventBus`, `InMemoryIntegrationEventBus`, `IntegrationEventErrors`, `IntegrationEventLog`, `IntegrationEventTelemetry`, `IntegrationEventServiceCollectionExtensions`, `IModulePublicApi`, `ModuleContractValidator`, `ModuleContractStartupValidator`, `OutboxIntegrationEventBus`, `IntegrationEventOutboxServiceCollectionExtensions` new; `EventIdRanges`, `Encina.Messaging.csproj`, OpenTelemetry `ServiceCollectionExtensions` edited |
| Public API and manifests | 2 to 4 per pull request | `Encina.DomainModeling`, `Encina`, `Encina.Messaging`, `Encina.OpenTelemetry` |
| Tests | about 20 | unit, guard, contract, property, DI, integration (one), allocation map edit |
| Justification files | 2 | load, benchmark (pull request 3) |
| Documentation | 4 | guide, inventory, roadmap, changelog fragment |
| **Pull request 1** | **5** | `IIntegrationEvent`, PublicAPI, manifest, 2 tests (`IntegrationEventTests`, `IntegrationEventContractTests`); about 200 lines |
| **Pull request 2** | **10** | 4 production (`IIntegrationEventBus`, `InMemoryIntegrationEventBus`, `IntegrationEventErrors`, `IntegrationEventServiceCollectionExtensions`), PublicAPI, manifest, 4 tests (bus unit, guard, DI registration, contract); about 700 lines |
| **Pull request 3** | **13** | 5 production (`IntegrationEventLog`, `IntegrationEventTelemetry`, `InMemoryIntegrationEventBus` edit, `EventIdRanges`, OpenTelemetry `ServiceCollectionExtensions`), 4 tests (allocation map edit, telemetry unit, bus unit edit, property), 2 manifests (`Encina.DomainModeling`, `Encina.OpenTelemetry`), 2 justification files; about 700 lines |
| **Pull request 4** | **10** | `IModulePublicApi`, `ModuleContractValidator`, `ModuleContractStartupValidator`, `Encina` PublicAPI, 2 manifests, 4 tests (validator unit, startup unit, guard, property); about 700 lines |
| **Pull request 5** | **12** | `OutboxIntegrationEventBus`, `IntegrationEventOutboxServiceCollectionExtensions`, `IntegrationEventOutboxLog`, `Encina.Messaging.csproj`, PublicAPI, manifest, 5 tests (unit with `FakeOutboxStore`, guard, DI, contract, PostgreSQL integration), allocation map edit; about 800 lines |
| **Pull request 6** | **4** | guide, inventory and roadmap lines in one edit pass, changelog fragment, XML doc touch-ups |

Callers and implementers, by grep on `origin/main`: `IIntegrationEvent` is referenced in `Encina.DomainModeling` (`IIntegrationEvent.cs`, `IntegrationEventExtensions.cs`, `IDomainEvent.cs` comment) and in `Encina.Testing` only through `INotification` collectors; no test type implements it today (the module test fixtures publish plain `INotification` types), so adding `SourceModule` leaves no broken implementer and the interface change and its implementers land in one pull request. `ModuleTestFixtureTests` and `IntegrationEventCollectorTests` use their own event types and are re-checked in pull request 1.

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library; no backward compatibility, zero warnings, ROP. Application modules (IModule) live in core; Encina.DomainModeling has IIntegrationEvent (not an INotification), mappers and BoundedContext types; Encina.Messaging has the outbox. IEncina.Publish needs INotification.

IMPLEMENTATION OVERVIEW: PR1 IIntegrationEvent becomes an INotification with SourceModule; PR2 IIntegrationEventBus and the in-memory bus with source and export checks and errors; PR3 logs 7630-7635, telemetry, the ModulesIntegrationEvents range 7630-7644 and the justification files; PR4 IModulePublicApi and the startup contract validator (logs 7636-7637); PR5 the optional outbox bus in Encina.Messaging (logs 7638-7639) that fails closed with multi-tenancy until #737; PR6 documentation. Dilemmas D1, D2, D3, D5, D6 and D7 are implemented as the maintainer answers, the recommended options until then.

KEY PATTERNS: Check failures and handler failures are Left(EncinaError) with an ErrorCategory, never exceptions and never swallowed; no atomicity claim about outbox rows (commit owners are #1236, #718, #720); the bus delegates dispatch to IEncina.Publish and the global NotificationDispatchOptions; telemetry and logs carry codes, module names and type names only; tenant tag only with multi-tenancy; singletons take no scoped service; each pull request stands alone with tests and manifest entries; CRAP <= 10; targets never go down.

REFERENCE FILES: src/Encina.DomainModeling/*.cs, src/Encina/Modules/*.cs, src/Encina/Abstractions/IEncina.cs, src/Encina/Core/Encina.cs, src/Encina.Messaging/Outbox/*.cs, src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs, src/Encina.Testing/Modules/*.cs, .github/coverage-manifest/Encina.DomainModeling.json.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | The bus has no read path to cache; handler resolution caching belongs to the existing dispatcher. |
| 2 | OpenTelemetry | ✅ | Activity and `encina.integration_events.published_total` with event, source module and outcome tags; tenant tag only with multi-tenancy on (ADR-054 D4). |
| 3 | Structured Logging | ✅ | `IntegrationEventLog`, EventIds 7630-7639 in the registered `ModulesIntegrationEvents` range. |
| 4 | Health Checks | ❌ | The in-memory bus has no dependency to check; outbox mode is covered by the existing `OutboxHealthCheck`; per-module health is #364. |
| 5 | Validation | ✅ | Source module and export checks at publish time, and the startup contract validator (D7). |
| 6 | Resilience | ✅ | Retry and dead-letter of outbox mode come from the existing outbox processor and its backoff; in-memory failures are returned to the publisher. |
| 7 | Distributed Locks | ❌ | The in-memory bus is in-process; outbox mode reuses the processor's own row claiming. |
| 8 | Transactions | ⏭️ | Atomic delivery with the business write is #1236 (domain events to the outbox on 10 providers) and the commit owner for orchestrated work is #720; this plan writes through the shared writer and claims nothing more. |
| 9 | Idempotency | ⏭️ | Consumer-side dedupe by event id is the follow-up issue file `artifacts/issues/module-integration-event-idempotent-consumers.md` (this plan's worktree); the existing `EventId` is the key material. |
| 10 | Multi-Tenancy | ✅ | Ambient tenant flows through the in-memory bus; outbox mode fails closed until #737 puts the tenant on the row (D6); no tenant resolution here, so no call to `ResolveRead` or `ResolveWrite` (#1257, #798). |
| 11 | Module Isolation | ✅ | Source module verification, exports and declared dependencies are the event-level half of module isolation (#352 request level, #359 data level). |
| 12 | Audit Trail | ⏭️ | Module attribution of audit rows is #1636 (open); audit of cross-module events and denied calls is the follow-up issue file `artifacts/issues/module-cross-module-audit.md` (this plan's worktree). |

---

## Prerequisites & Dependencies

- **#360, pull requests 1 and 3** (`IModuleGraph`, `Exports`, `EncinaModule`) and **#352, pull requests 1 and 3** (module context producer in every package, `ModuleBoundaryOptions`) must be merged before pull requests 2 to 4 of this plan; pull request 1 of this plan has no prerequisite.
- **Outbox mode (pull request 5)** needs nothing open to ship, but it is unavailable to multi-tenant applications until #737 (tenant on `OutboxMessage`) is merged; the guard disappears with it.
- **Related, not blocking**: #1236 (domain events to the outbox) and #718 and #720 (transaction lease and saga commit owner) define when an outbox row commits; #306 is the overlapping earlier issue "Modular Monolith Integration Events" (same bus, an `IModuleEventBus` sketch); it belongs to a sibling family plan and is closed or merged into this one by the maintainer's decision.

---

## Next Steps

1. Ask the maintainer D1, D2, D3, D5, D6 and D7 when work on #358 starts; record the answers in `## Maintainer Decisions`.
2. Pull request 1: the contract (5 files).
3. Pull request 2: the in-memory bus and its checks (10 files), after #360 and #352 pull requests 1 and 3.
4. Pull request 3: logging, telemetry, EventId range and the two justification files (13 files).
5. Pull request 4: `IModulePublicApi` and the contract validator (10 files).
6. Pull request 5: outbox mode (12 files).
7. Pull request 6: documentation and changelog (4 files).
