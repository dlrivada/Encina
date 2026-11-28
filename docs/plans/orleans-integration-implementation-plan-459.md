# Implementation Plan: Encina.Orleans - Handlers as Grains, Saga Grains and Reminder-Driven Scheduling

> **Issue**: [#459](https://github.com/dlrivada/Encina/issues/459)
> **Type**: Feature (extends the `Encina.Orleans` package created by #388)
> **Related**: #388 (owns the `Encina.Orleans` skeleton, `EncinaGrain<TState>`, `ActorGrainKey`, the identity and tenant-verification filters and the `Orleans` EventId range), #330 (event-sourced actors), #416 and #440 (Aspire hosting and umbrella), #720 and #297 (saga commit owner and state machine), #1257 (default-tenant rule), ADR-054, ADR-062
> **Complexity**: High (6 phases, 6 pull requests, one package)
> **Estimated Scope**: ~1,500 lines of production code + ~2,900 lines of tests and documentation, in six pull requests of at most 15 files

---

## Summary

### What exists today (read on 2026-11-27, `origin/main` `64aec507f`)

- `Encina.Orleans` does **not** exist in `src/`, and `Directory.Packages.props` has no `Microsoft.Orleans.*` pin. The plan of #388 (on main) creates `Encina.Actors` and the `Encina.Orleans` skeleton in its pull requests G1 to G4 (`ISiloBuilder.AddEncinaOrleans`, `EncinaOrleansOptions`, `EncinaGrain<TState>` with `SendAsync`, `ActorGrainKey`, `IActorTenantVerifier`, the incoming and outgoing call filters, `OrleansDiagnostics`, `OrleansLog`) and reserves the EventId range `Orleans` 80500-80549 (ids 80500-80503 used). **This plan starts after #388's G4 has merged** and never creates a project.
- Request handling: `IEncina.Send` resolves an `IRequestHandler<TRequest, TResponse>` from DI and wraps it in `IPipelineBehavior<TRequest, TResponse>` (`Handle(request, context, nextStep, ct)`, `src/Encina/Abstractions/IPipelineBehavior.cs`). A handler is a DI service, not an Orleans grain: Orleans creates grain instances itself, so the issue's `class OrderHandler : Grain, ICommandHandler<...>` cannot be one object that DI and Orleans both construct.
- Sagas: `SagaOrchestrator` (`src/Encina.Messaging/Sagas/SagaOrchestrator.cs`: `StartAsync`, `AdvanceAsync`, `CompleteAsync`, `StartCompensationAsync`, `CompensateStepAsync`, `FailAsync`, `GetAsync`) over `ISagaStore` (10 provider stores). `SagaState` carries `TimeoutAtUtc`; **no processor acts on an expired saga today**: `GetExpiredSagasAsync` is called only by `SagaHealthCheck` and passed through by the orchestrator.
- Scheduling: `ScheduledMessageProcessor` is a `BackgroundService` polling every `SchedulingOptions.ProcessingInterval` (30 s) and runs only when `SchedulingOptions.EnableProcessor` (default `true`) is on; `SchedulerOrchestrator.ProcessDueMessagesAsync` does one cycle.
- `Encina.OpenTelemetry` registers its sources by hand in `ServiceCollectionExtensions.cs` (`tracing.AddSource("Encina")` and a list of named sources); `Encina.Orleans` is not among them. The Aspire family is `Encina.Aspire.Hosting` (#416, `AddEncina` and `WithEncina`); `Directory.Packages.props` pins only `Aspire.Hosting.Testing` and `Aspire.Hosting.PostgreSQL`. There is no `samples/` directory.

### What this plan delivers

- **Request routing to grains**: the attribute `[GrainRoute("orders/{OrderId}")]` on a **request type** and a pipeline behavior that forwards such requests to `IRequestGrain<TRequest, TResponse>`, a generic grain keyed by the template result; inside the grain the request runs through `IEncina.Send` (with a bypass marker so it is not forwarded again). One request at a time per key, no change to handler classes.
- **Saga grains**: `ISagaGrain<TSagaData>` / `SagaGrain<TSagaData>` serialize the calls of one saga through `SagaOrchestrator`; the saga state stays in `ISagaStore`; an optional reminder at `TimeoutAtUtc` starts compensation.
- **Reminder-driven scheduling**: a cluster-singleton `SchedulerDriverGrain` runs `SchedulerOrchestrator.ProcessDueMessagesAsync` from a reminder plus a grain timer and switches the polling processor off.
- **Health and telemetry**: a cluster health check, the `Encina.Orleans` source and meter registered in `Encina.OpenTelemetry`, `[LoggerMessage]` ids 80504-80511 inside #388's range.
- **Not delivered**: Orleans streams for outbox publishing and observers for notification broadcasting (not in the acceptance criteria; recorded in Next Steps), Aspire resource code (Design Choice 6), distributed `IUnitOfWork` over Orleans transactions (the issue's "Orleans ACID transactions"; Next Steps).
- Affected packages: `Encina.Orleans` (code, manifest), `Encina.OpenTelemetry` (one registration edit, manifest). Provider category: none of the Encina categories; saga and scheduling state stay in the 10-provider stores that already exist, so the database rule is unaffected.

---

## Design Choices

<details>
<summary><strong>1. How a handler runs inside a grain</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Route by request type: `[GrainRoute(template)]` on the request, a pipeline behavior (`GrainRoutingBehavior<TRequest, TResponse>`) forwards to `IRequestGrain<TRequest, TResponse>` keyed by the template result; the grain executes the request through `IEncina.Send` with a bypass marker** | Existing handlers unchanged; per-key single-threaded execution; opt-in per request type and by `EncinaOrleansOptions.RouteRequestsToGrains` (default `false`) | Request and response types must be serializable by Orleans (Design Choice 5); one extra hop |
| **B) Replace `IEncina` with a grain-aware decorator that routes every request** | Fully automatic | Every request pays the hop; hides routing decisions from the code reading it |
| **C) An explicit call: `IGrainEncina.SendToGrainAsync(request)`** | No pipeline magic; the caller decides | The issue's "automatic placement" and `MapHandlersToGrains` are not met |

### Chosen Option: **A - Attribute on the request, pipeline behavior** (recommended, pending the maintainer)

### Rationale

- A wins when a few aggregates-like request families need serialization per key (the issue's per-instrument, per-device and per-player cases); B wins when the whole application should be grain-routed; C wins when the maintainer wants no pipeline behavior. The issue's literal form (a handler that derives from `Grain`) is not an option because DI and Orleans would each build their own instance. The routing behavior is registered only when the option is on; the attribute alone does nothing.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Who owns saga state</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The grain is a single-writer gate in front of `SagaOrchestrator`; the state stays in `ISagaStore` (all 10 providers)** | `SagaHealthCheck`, stuck and expired queries, the 10-provider stores and every existing saga keep working; the grain adds only per-saga serialization | Two layers (grain activation and store row); the saga calls still commit through the orchestrator and the store |
| **B) Saga state lives in Orleans grain storage (`IPersistentState`)** | One layer, the issue's "Handler/Saga state storage" | The saga disappears from `ISagaStore`: health checks and stuck-saga queries stop seeing it; two saga implementations to keep equal |

### Chosen Option: **A - Grain as single-writer gate** (recommended, pending the maintainer)

### Rationale

- A wins in every deployment that already runs sagas on a database store; B wins only for a new application that wants no relational saga table and accepts losing the store-based queries. The plan never describes the grain call and the store commit as atomic: `SagaOrchestrator.CommitAsync` of the #720 plan owns the commit, and the grain only guarantees that two calls for one saga do not interleave.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. What drives scheduled messages in a cluster</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One cluster-singleton `SchedulerDriverGrain` (fixed key) with a reminder (durable, wakes the grain after a silo loss) and a grain timer at `SchedulingOptions.ProcessingInterval` (sub-minute polling); each tick calls `SchedulerOrchestrator.ProcessDueMessagesAsync`; the polling `ScheduledMessageProcessor` is switched off by `PostConfigure<SchedulingOptions>(EnableProcessor = false)`** | Exactly one active driver in the cluster, so no duplicate processing; survives silo failure; sub-minute precision | A singleton grain is a hot spot at very high rates (the cycle processes `BatchSize` messages) |
| **B) One reminder per scheduled message** | Native delayed delivery | Orleans reminders are coarse (minute scale) and each one is a storage row: millions of messages mean millions of reminders |
| **C) Keep the polling processor on every silo and rely on the store's claim of due messages** | No new grain | Every silo polls; relies on the stores' claim semantics for duplicate safety, which the issue's reminder goal does not use |

### Chosen Option: **A - Singleton driver grain with reminder and timer** (recommended, pending the maintainer)

### Rationale

- A wins for clusters of several silos, where it also acts as leader election for scheduling; C wins for a single silo or when a lock-based leader already exists; B wins only for a small number of long-delay messages. The timer callback and the reminder handler catch every failure, log by code or `ForLogging()` and keep running (`AGENTS.md` section 3); a `Left` from the cycle fails that cycle visibly and the next tick retries. The clock is `TimeProvider`.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. What a saga timeout reminder does</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Start compensation: when the reminder at `TimeoutAtUtc` fires and the saga is still running, call `StartCompensationAsync` and compensate its steps** | A timed-out saga undoes its work, the usual meaning of a saga timeout; closes the gap that nothing acts on expired sagas today | Changes the effective behavior of timeouts for applications that adopt saga grains; compensation steps must be idempotent |
| **B) Mark the saga failed (`FailAsync`) with no compensation** | No side effects beyond the status | Leaves partial work in place |
| **C) Observe only: log and raise the health check (as today)** | No behavior change | The issue's "Reminders: saga timeout handling" is not met |

### Chosen Option: **A - Compensate on timeout** (recommended, pending the maintainer)

### Rationale

- A wins for sagas whose steps have compensations (the pattern's purpose); B wins when compensation is dangerous without human review; C wins when the maintainer wants the semantics decided together with #297's state machine first. Opt-in: only saga grains with `EncinaOrleansOptions.SagaTimeoutReminders` (default `false`) register the reminder; the reminder is unregistered when the saga completes.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Serialization of routed requests and responses</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Orleans' own code generation: routed request and response types carry `[GenerateSerializer]` and `[Id]` members** | Fastest; compile-time checked | Every routed type needs Orleans attributes in the application's domain code |
| **B) A System.Text.Json serializer registered for the types that carry `[GrainRoute]` (and their response types)** | No Orleans attributes on application types; the wire form is the JSON the application already uses | Slower; polymorphism and records need the same JSON options on both sides; the implementing PR confirms the Orleans JSON serializer API against the pinned version |

### Chosen Option: **B - JSON for routed types** (recommended, pending the maintainer)

### Rationale

- B wins for applications that keep their requests free of infrastructure attributes (the usual Encina style); A wins on hot paths that need the compact binary form, and A is always allowed in addition because a type with `[GenerateSerializer]` is already handled by Orleans. `Either<EncinaError, T>` crosses the wire through #388's codec and `ActorFailure`, never as a raw exception.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Aspire integration</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Guide only: the AppHost uses Aspire's own Orleans resource (`AddOrleans`, clustering and storage references) and the service project calls `AddEncinaOrleans`; no Encina code in the AppHost** | No duplicated resource model; nothing to keep in step with `Aspire.Hosting.Orleans`; the issue's `WithEncinaOrleans()` is a documented two-liner | The issue's literal `WithEncinaOrleans()` and "dashboard integration" are not shipped code |
| **B) `WithEncinaOrleans()` in `Encina.Aspire.Hosting` (#416) that sets Encina-specific environment variables for the service** | The one-call experience of the issue | A new Orleans-specific surface in the hosting package owned by #416; the environment contract of #416 and #417 must be extended and kept in step |

### Chosen Option: **A - Guide only** (recommended, pending the maintainer)

### Rationale

- A wins while Encina has no Orleans-specific AppHost setting to pass; B wins as soon as one exists (for example a storage name `AddEncinaOrleans` should read from the environment), and then belongs to #416's family. The dashboard integration is Orleans' own dashboard, enabled in the AppHost; the Encina telemetry reaches the Aspire dashboard through OTLP like every other Encina source.

**Class:** Dilemma

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | The key template (`orders/{OrderId}`) names public readable properties of the request; it is parsed once per request type at startup (`ValidateOnStart`), compiled accessors are cached per generic type, and a template naming no property is a startup error; at run time a null or empty property value is `encina.orleans.route_key_unresolved` (`Validation`) | Settled | `AGENTS.md` section 3 (pipeline behaviors: static per-generic-type caching; validation returns `Either`) |
| C2 | The grain key is built by #388's `ActorGrainKey` from (tenant, grain type, template result), each component percent-encoded, so two (tenant, type, key) triples never map to one key; with multi-tenancy on and no tenant the call is `tenant_required` (`InvalidRequest`); the reserved tenant literal `default` is used only when multi-tenancy is off; every routed call passes #388's tenant-verification filter | Settled | Plan #388 register rows C4 and C5 and Design Choice 6; plan #1257 (reserved `default` segment) |
| C3 | The bypass marker is a request-context item set by the grain around its inner `IEncina.Send`; the routing behavior forwards only when the marker is absent, so a routed request is never forwarded twice | Trivial | `src/Encina/Abstractions/IPipelineBehavior.cs` (behaviors receive `IRequestContext`) |
| C4 | Error codes (prefix `encina.orleans.`; #388 owns `encina.actors.*`): `route_key_unresolved` (`Validation`), `route_template_invalid` (`Internal`, a configuration entry, raised at startup), `saga_grain_failed` (`Internal`, wraps a store failure by code only), `scheduler_cycle_failed` (`Internal`) | Settled | ADR-062 rules R1 to R4 |
| C5 | A `Left` from the inner `Send`, the orchestrator or the scheduling cycle is returned to the caller or fails that cycle; nothing is swallowed; `EncinaError.Message` never reaches a log, tag or health result | Settled | `AGENTS.md` section 3 |
| C6 | Time comes from `TimeProvider` (reminder due times, timer periods); a reminder is registered with `RegisterOrUpdateReminder` using a `TimeSpan` from that clock | Settled | `AGENTS.md` section 3 |
| C7 | All new behavior is opt-in and off by default: `RouteRequestsToGrains`, `SagaGrains`, `SagaTimeoutReminders`, `SchedulingDriver` on `EncinaOrleansOptions`; enabling the scheduling driver is the only option that changes another module's registration (it sets `SchedulingOptions.EnableProcessor = false` and logs this once) | Settled | `AGENTS.md` section 3 (every feature opt-in, pay-for-what-you-use) |
| C8 | Telemetry: the `Encina.Orleans` source and meter of #388; this plan adds the span `encina.orleans.route` and `encina.orleans.saga.step`, counters `encina.orleans.routed.requests` and `encina.orleans.scheduler.cycles` (no `_total`) with tags for the request type name and outcome; tenant tag through #2676 when merged; never grain keys, tenant ids, user ids or messages | Settled | ADR-054 decision 4 and D4 |
| C9 | The examples the issue asks for ("Sample project") are the test grains of the integration tests and the guide, as in #388; no `samples/` directory is created | Trivial | Plan #388 (its examples are test grains and the guide) |

---

## Implementation Phases

### Delivery order and size

Six pull requests; each builds and passes alone with its own tests. All start after #388's G4 has merged. Counts include PublicAPI, manifests, DI and the fragment.

| PR | Phase | Content | Files | Lines (est.) |
|----|-------|---------|-------|--------------|
| H1 | 1 | Options, `[GrainRoute]`, route template parser and resolver, error codes | 9 | ~700 |
| H2 | 2 | `IRequestGrain`, `RequestGrain`, `GrainRoutingBehavior`, serializer, registration, TestCluster tests | 10 | ~1,300 |
| H3 | 3 | `ISagaGrain`, `SagaGrain`, timeout reminders | 8 | ~1,100 |
| H4 | 4 | `ISchedulerDriverGrain`, `SchedulerDriverGrain`, processor switch-off | 8 | ~900 |
| H5 | 5 | Cluster health check, EventIds 80504-80511, `Encina.OpenTelemetry` source registration | 13 | ~800 |
| H6 | 6 | Guide with Aspire section, README, ADR draft, INVENTORY, ROADMAP, changelog | 6 | ~500 |

### Phase 1: Options, Route Attribute and Template (pull request H1)

<details>
<summary>Tasks</summary>

1. **Create** `src/Encina.Orleans/Routing/GrainRouteAttribute.cs` (`[AttributeUsage(Class)]`, `Template`), `Routing/RouteTemplate.cs` (internal: parse `orders/{OrderId}`, compiled getters, `Either<EncinaError, string> Resolve(object request)`), `OrleansErrorCodes.cs` (row C4).
2. **Modify** `EncinaOrleansOptions.cs` (`RouteRequestsToGrains`, `SagaGrains`, `SagaTimeoutReminders`, `SchedulingDriver`, all `false`), `PublicAPI.Unshipped.txt`, `.github/coverage-manifest/Encina.Orleans.json` (through `mechanical-fixer`).
3. Tests: new `tests/Encina.UnitTests/Orleans/RouteTemplateTests.cs` (valid, unknown property, nested braces, null and empty values), `tests/Encina.GuardTests/Orleans/GrainRouteAttributeGuardTests.cs` (null and whitespace template), `tests/Encina.PropertyTests/Orleans/RouteKeyPropertyTests.cs` (two distinct (tenant, type, key) triples never encode to one grain key; resolution is deterministic). The `InternalsVisibleTo` entries of #388's csproj already include UnitTests and PropertyTests.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #459, plan docs/plans/orleans-integration-implementation-plan-459.md, phase 1.
Prerequisite: #388 G1-G4 merged (Encina.Actors, Encina.Orleans with ActorGrainKey and diagnostics). If not, stop and report.

TASK:
1. [GrainRoute], RouteTemplate and OrleansErrorCodes; four opt-in options (default false).
2. PublicAPI and manifest through mechanical-fixer; unit, guard and property tests.

KEY RULES:
- Template parsed once per request type at startup; invalid template is a startup error; run-time failures are Either (route_key_unresolved, Validation).
- Error codes carry ErrorCategory per ADR-062 (never FailedPrecondition); every option defaults to off.
- XML docs with examples; zero warnings.

REFERENCE FILES:
- docs/plans/virtual-actors-implementation-plan-388.md (ActorGrainKey, register rows C4 and C5), src/Encina/Abstractions/IPipelineBehavior.cs
```

</details>

### Phase 2: Request Routing (pull request H2)

<details>
<summary>Tasks</summary>

1. **Create** `Routing/IRequestGrain.cs` (`IGrainWithStringKey`; `Task<Either<EncinaError, TResponse>> HandleAsync(TRequest request)` over the codec of #388), `Routing/RequestGrain.cs` (derives from `EncinaGrain<RequestGrainState>` stateless in practice; sets the bypass marker and calls `SendAsync`), `Routing/GrainRoutingBehavior.cs` (`IPipelineBehavior<TRequest, TResponse>`; forwards when the request type carries the attribute, the option is on and the marker is absent; builds the key with `ActorGrainKey`), `Routing/GrainRoutingBypass.cs` (the marker).
2. **Modify** `SiloBuilderExtensions.cs` (when `RouteRequestsToGrains`: register the behavior, scan requests with `[GrainRoute]`, validate templates with `ValidateOnStart`, add the JSON serializer for those types per Design Choice 5), `PublicAPI.Unshipped.txt`, manifest.
3. Tests: new `tests/Encina.IntegrationTests/Orleans/RequestRoutingIntegrationTests.cs` in #388's shared `[Collection("Orleans-TestCluster")]` (test requests and handlers inside the file: two requests with one key run one at a time; different keys run in parallel; a handler `Left` reaches the caller; a missing tenant in a multi-tenant host is `tenant_required`; a forged tenant in the key is denied by the #388 filter), `tests/Encina.UnitTests/Orleans/GrainRoutingBehaviorTests.cs` (not forwarded without attribute, with the bypass marker or with the option off), guard tests.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT:
Issue #459 plan docs/plans/orleans-integration-implementation-plan-459.md, phase 2. H1 merged.

TASK:
1. IRequestGrain, RequestGrain, GrainRoutingBehavior, the bypass marker; wire them in AddEncinaOrleans when RouteRequestsToGrains is on.
2. TestCluster integration tests in the shared collection (never a new cluster per class), unit and guard tests.

KEY RULES:
- Handlers stay DI services; the grain calls IEncina.Send with the bypass marker so a request is never forwarded twice.
- The grain key comes from ActorGrainKey (tenant-qualified, injective); fail closed without a tenant when multi-tenancy is on.
- A Left is returned, never swallowed; do not call the grain call and the handler's own commit atomic.
- Never inject a scoped service into a singleton; Orleans grains resolve scoped services through the request scope of #388's EncinaGrain.

REFERENCE FILES:
- src/Encina/Abstractions/IPipelineBehavior.cs, docs/plans/virtual-actors-implementation-plan-388.md (phase 3)
```

</details>

### Phase 3: Saga Grains (pull request H3)

<details>
<summary>Tasks</summary>

1. **Create** `Sagas/ISagaGrain.cs` (`ISagaGrain<TSagaData> : IGrainWithGuidKey`: `AdvanceAsync`, `CompleteAsync`, `StartCompensationAsync`, `CompensateStepAsync`, `FailAsync`, `GetAsync`, mirroring `SagaOrchestrator` and returning `Either`), `Sagas/SagaGrain.cs` (delegates each call to `SagaOrchestrator`; with `SagaTimeoutReminders` registers a reminder at `TimeoutAtUtc` on the first call that sees it and, when it fires and the saga is still running, calls `StartCompensationAsync` and compensates; unregisters the reminder on completion).
2. **Modify** `SiloBuilderExtensions.cs` (registration when `SagaGrains`), `PublicAPI.Unshipped.txt`, manifest.
3. Tests: new `tests/Encina.IntegrationTests/Orleans/SagaGrainIntegrationTests.cs` (shared TestCluster collection; saga state in the fake store of `Encina.Testing.Fakes`: calls for one saga serialize, a failing step compensates, a timeout with a fake clock compensates once and unregisters), `tests/Encina.UnitTests/Orleans/SagaTimeoutDecisionTests.cs`, guard tests.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT:
Issue #459 plan docs/plans/orleans-integration-implementation-plan-459.md, phase 3. H1 and H2 merged.

TASK:
1. ISagaGrain and SagaGrain over SagaOrchestrator; optional timeout reminder starting compensation.
2. Integration, unit and guard tests.

KEY RULES:
- The state stays in ISagaStore; the grain only serializes calls. Never describe the grain call plus the store commit as atomic; SagaOrchestrator.CommitAsync of plan #720 owns the commit.
- Timeout handling is opt-in (SagaTimeoutReminders, default false); reminder due times come from TimeProvider.
- Errors never swallowed; the reminder handler catches its own failures, logs by code or ForLogging() and keeps running.

REFERENCE FILES:
- src/Encina.Messaging/Sagas/SagaOrchestrator.cs, src/Encina.Messaging/Sagas/ISagaStore.cs, src/Encina.Testing.Fakes/Stores/FakeSagaStore.cs
```

</details>

### Phase 4: Scheduling Driver (pull request H4)

<details>
<summary>Tasks</summary>

1. **Create** `Scheduling/ISchedulerDriverGrain.cs` (`IGrainWithStringKey`, fixed key `scheduler`; `StartAsync`, `TickAsync`), `Scheduling/SchedulerDriverGrain.cs` (reminder `encina.scheduler` plus a grain timer at `SchedulingOptions.ProcessingInterval` created with `RegisterGrainTimer`; each tick runs `SchedulerOrchestrator.ProcessDueMessagesAsync` in a scope and catches every failure).
2. **Modify** `SiloBuilderExtensions.cs` (when `SchedulingDriver`: register the grain, `PostConfigure<SchedulingOptions>` sets `EnableProcessor = false`, a startup activation of the driver), `PublicAPI.Unshipped.txt`, manifest.
3. Tests: new `tests/Encina.IntegrationTests/Orleans/SchedulerDriverIntegrationTests.cs` (shared TestCluster collection: due messages are processed once with two silos; a failing cycle does not stop the driver), `tests/Encina.UnitTests/Orleans/SchedulerDriverOptionsTests.cs` (the processor is switched off and the switch is logged once), guard tests.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT:
Issue #459 plan docs/plans/orleans-integration-implementation-plan-459.md, phase 4. H1-H3 merged.

TASK:
1. ISchedulerDriverGrain and SchedulerDriverGrain; switch the polling processor off when the driver is enabled.
2. Integration, unit and guard tests.

KEY RULES:
- Reminder and timer callbacks catch every failure and never throw; a Left from the cycle fails that cycle visibly and the next tick retries.
- Resolve SchedulerOrchestrator in a scope per tick; never inject a scoped service into the grain's constructor.
- Time from TimeProvider; the singleton grain key is a constant, never user input.

REFERENCE FILES:
- src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs, src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs, src/Encina.Messaging/Scheduling/SchedulingOptions.cs
```

</details>

### Phase 5: Health, EventIds and OpenTelemetry (pull request H5)

<details>
<summary>Tasks</summary>

1. **Create** `Health/OrleansClusterHealthCheck.cs` (`DefaultName` const, `Tags` static array; reads `IClusterMembershipService`; healthy when the local silo is active and no silo is dead for longer than a configured period; scoped resolution through a scope).
2. **Modify** `OrleansLog.cs` (#388's file; ids 80504-80511 packed: 80504 request routed (Debug), 80505 route key unresolved (Warning, code), 80506 saga grain step failed (Warning, code), 80507 saga timeout reminder fired (Information), 80508 saga timeout action failed (`ForLogging()`), 80509 scheduler cycle completed (Debug, count), 80510 scheduler cycle failed (`ForLogging()`), 80511 scheduling driver enabled and polling processor switched off (Information)), `OrleansDiagnostics.cs` (row C8), `GrainRoutingBehavior.cs`, `SagaGrain.cs`, `SchedulerDriverGrain.cs`, `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs` (`tracing.AddSource("Encina.Orleans")` and the meter), `PublicAPI.Unshipped.txt`, `Encina.Orleans.json`, `Encina.OpenTelemetry.json`. No new `EventIdRanges` field: the range `Orleans` 80500-80549 is #388's.
3. Tests: new `OrleansClusterHealthCheckTests`, `OrleansTelemetryRegistrationTests` (the source and meter are subscribed), edit `OrleansDiagnosticsTests` (no grain key, tenant id or message in any tag); run the EventId architecture tests.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT:
Issue #459 plan docs/plans/orleans-integration-implementation-plan-459.md, phase 5. H1-H4 merged.

TASK:
1. The cluster health check, the new log ids inside #388's range, the new instruments, the OpenTelemetry source registration.

KEY RULES:
- ADR-021 and ADR-054: ids packed 80504-80511 inside the existing Orleans range; no _total suffix; no grain key, tenant id, user id or EncinaError.Message in any tag or log; exceptions through ForLogging().
- The health check resolves scoped services through a scope.

REFERENCE FILES:
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs, src/Encina.Messaging/Health/SagaHealthCheck.cs, docs/engineering/eventid-block-registry.md
```

</details>

### Phase 6: Documentation and Finalization (pull request H6)

<details>
<summary>Tasks</summary>

1. Through `docs-writer`: the `src/Encina.Orleans/README.md` section for handlers, sagas and scheduling with cluster setup examples, `docs/features/orleans-integration.md` (the issue's use cases, the opt-in options, an Aspire section with the AppHost snippet of Design Choice 6 option A), the ADR draft (number assigned when the ADR pull request opens), `docs/INVENTORY.md`, `ROADMAP.md`.
2. `changelog.d/459-orleans-integration.added.md` through `mechanical-fixer`.
3. Build with zero warnings; every flag reaches its targets; the PR reports measured per-file coverage.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT:
Issue #459 plan docs/plans/orleans-integration-implementation-plan-459.md, phase 6. All code merged.

TASK:
1. Ask docs-writer for the README section, guide, ADR draft, INVENTORY and ROADMAP lines; the fragment through mechanical-fixer.

KEY RULES:
- Documentation never types coverage figures by hand (covref markers); public text never names a private client application.
- The guide states that routed handlers run one request at a time per key and that nothing here is atomic with the handler's own commit.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md, docs/features/
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| Microsoft Orleans documentation (grains, reminders, timers, serialization, TestingHost) | Virtual actors, durable reminders, grain timers | Phases 2 to 4 |
| `AGENTS.md` section 3 | ROP, timer callbacks never throw, `TimeProvider`, opt-in, fail-closed tenants | Register rows C1 to C7 |
| ADR-054, ADR-062, ADR-021 | Telemetry, error categories, EventIds | Phase 5, row C4 |
| Plans #388, #1257, #720 | `ActorGrainKey` and tenant verification, reserved `default` segment, saga commit owner | Rows C2, Design Choice 2 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IPipelineBehavior<TRequest, TResponse>` | `src/Encina/Abstractions/IPipelineBehavior.cs` | Routing behavior |
| `SagaOrchestrator`, `ISagaStore`, `FakeSagaStore` | `src/Encina.Messaging/Sagas/`, `src/Encina.Testing.Fakes/Stores/FakeSagaStore.cs` | Saga grain delegate and test store |
| `SchedulerOrchestrator`, `SchedulingOptions`, `ScheduledMessageProcessor` | `src/Encina.Messaging/Scheduling/` | Scheduling driver |
| `EncinaGrain<TState>`, `ActorGrainKey`, filters, `OrleansDiagnostics`, `OrleansLog` | `Encina.Orleans` (created by #388) | Base grain, key, tenant verification, diagnostics |
| `Encina.OpenTelemetry` source registration | `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs` | One `AddSource` edit |
| `[Collection("Orleans-TestCluster")]` | `tests/Encina.IntegrationTests/Orleans/` (created by #388) | Shared in-process cluster |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Orleans` (existing field `Orleans` of #388) | ids `80504-80511` inside `80500-80549`, no new range | #388 reserves and registers the range (80500-80503 used); this plan packs its eight ids directly after them; no `EventIdRanges.cs`, registry or allocation-map edit is needed |

### Estimated File Count

| PR | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| H1 | 3 (`GrainRouteAttribute`, `RouteTemplate`, `OrleansErrorCodes`) | 2 (`EncinaOrleansOptions.cs`, `PublicAPI.Unshipped.txt`) | 3 | 0 | 1 (`Encina.Orleans.json`) | 0 | 9 | ~700 |
| H2 | 4 (`IRequestGrain`, `RequestGrain`, `GrainRoutingBehavior`, `GrainRoutingBypass`) | 2 (`SiloBuilderExtensions.cs`, `PublicAPI.Unshipped.txt`) | 3 (integration, behavior unit, guard) | 0 | 1 | 0 | 10 | ~1,300 |
| H3 | 2 (`ISagaGrain`, `SagaGrain`) | 2 (`SiloBuilderExtensions.cs`, `PublicAPI.Unshipped.txt`) | 3 | 0 | 1 | 0 | 8 | ~1,100 |
| H4 | 2 (`ISchedulerDriverGrain`, `SchedulerDriverGrain`) | 2 (`SiloBuilderExtensions.cs`, `PublicAPI.Unshipped.txt`) | 3 | 0 | 1 | 0 | 8 | ~900 |
| H5 | 1 (`OrleansClusterHealthCheck`) | 7 (`OrleansLog.cs`, `OrleansDiagnostics.cs`, `GrainRoutingBehavior.cs`, `SagaGrain.cs`, `SchedulerDriverGrain.cs`, `Encina.OpenTelemetry` `ServiceCollectionExtensions.cs`, `PublicAPI.Unshipped.txt`) | 2 | 1 (`OrleansDiagnosticsTests.cs`) | 2 (`Encina.Orleans.json`, `Encina.OpenTelemetry.json`) | 0 | 13 | ~800 |
| H6 | 0 | 0 | 0 | 0 | 0 | 6 (README section, guide, ADR draft, `INVENTORY.md`, `ROADMAP.md`, fragment) | 6 | ~500 |
| **Total** | **12** | **15** | **14** | **1** | **6** | **6** | **54** | **~5,300** |

Column check: 12 + 15 + 14 + 1 + 6 + 6 = 54 = 9 + 10 + 8 + 8 + 13 + 6. No existing interface changes: the plan consumes `IPipelineBehavior`, `SagaOrchestrator` and `SchedulerOrchestrator` and implements `IRequestGrain`, `ISagaGrain` and `ISchedulerDriverGrain`, all new, each with one implementer in this plan, so there are no existing implementers or fakes to update. New public constructors with parameters: `GrainRouteAttribute`, `SagaGrain`, `SchedulerDriverGrain`, `RequestGrain` and `GrainRoutingBehavior` (guard tests counted in "New tests"). The `InternalsVisibleTo` entries needed for the unit, guard and property tests of internal types (`RouteTemplate`) are already written by #388 in `Encina.Orleans.csproj` (UnitTests, GuardTests, ContractTests, PropertyTests, IntegrationTests), so this plan edits no csproj and `Directory.Packages.props` gains no pin.

Per-file coverage targets (the manifest `Encina.Orleans.json` is created by #388; rows below are added by the pull request that creates each file and never lower an existing value):

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `Routing/RouteTemplate.cs` | 95 | 0 | 0 | 80 | 0 | Parse and resolve branches are table-driven; the property test proves determinism and injectivity |
| `Routing/GrainRouteAttribute.cs` | 80 | 90 | 0 | 0 | 0 | One validated constructor argument |
| `Routing/GrainRoutingBehavior.cs` | 85 | 60 | 0 | 0 | 60 | Forward and skip branches are unit tested; the cluster proves the real hop |
| `Routing/RequestGrain.cs` | 30 | 50 | 0 | 0 | 80 | Behavior needs a running grain; guard covers the constructor |
| `Sagas/SagaGrain.cs` | 40 | 50 | 0 | 0 | 80 | Timeout decision is unit tested; serialization and reminders need the cluster |
| `Scheduling/SchedulerDriverGrain.cs` | 40 | 50 | 0 | 0 | 80 | Tick logic unit tested; reminder and timer behavior need the cluster |
| `Health/OrleansClusterHealthCheck.cs` | 85 | 60 | 0 | 0 | 0 | Healthy, dead-silo and inactive-silo branches over a substituted membership service |
| `Routing/GrainRoutingBypass.cs`, `OrleansErrorCodes.cs`, `IRequestGrain.cs`, `ISagaGrain.cs`, `ISchedulerDriverGrain.cs` | 0 | 0 | 0 | 0 | 0 | Constants, a marker and interfaces: no executable code of their own |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #459 - handlers as grains, saga grains and reminder-driven scheduling in Encina.Orleans - in six small pull
requests (H1 to H6), in order. Plan: docs/plans/orleans-integration-implementation-plan-459.md. Prerequisite: #388 G1-G4 merged.

PROJECT CONTEXT:
- Encina .NET 10 / C# 14, pre-1.0, no [Obsolete], no compatibility layers. Edit with Edit/Write only; PublicAPI lines, manifests and
  changelog through mechanical-fixer; README, the guide and the ADR through docs-writer.
- Encina.Orleans is created by #388; this plan adds files to it and never creates the project. Do not implement Orleans streams, observers,
  Orleans transactions or Aspire resource code.

IMPLEMENTATION OVERVIEW:
1. H1: [GrainRoute], route template, error codes, four opt-in options. 2. H2: IRequestGrain, RequestGrain, GrainRoutingBehavior.
3. H3: SagaGrain with optional timeout reminder. 4. H4: SchedulerDriverGrain, polling processor off. 5. H5: health check, ids 80504-80511, OTel source.
6. H6: documentation.

KEY PATTERNS:
- ROP: Either everywhere; a Left is returned or fails the cycle, never swallowed; EncinaError.Message never logged or tagged.
- Handlers stay DI services; a grain key is tenant-qualified and injective via ActorGrainKey; fail closed without a tenant; every call passes #388's tenant verification.
- Timer and reminder callbacks never throw; time through TimeProvider; never inject a scoped service into a grain or singleton.
- Everything is opt-in (default false); never call a grain call plus a store commit atomic.
- Guard tests for every public constructor and method parameter; integration tests share the Orleans-TestCluster collection.

REFERENCE FILES:
- docs/plans/virtual-actors-implementation-plan-388.md, src/Encina.Messaging/Sagas/SagaOrchestrator.cs, src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs
- src/Encina/Abstractions/IPipelineBehavior.cs, src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Grain activations keep their state in memory; the issue's "grain state caching" has no consumer, and #388 reached the same conclusion |
| 2 | OpenTelemetry | ✅ Phase 5 | `Encina.Orleans` spans and counters of row C8; the source and meter are registered in `Encina.OpenTelemetry` |
| 3 | Structured Logging | ✅ Phase 5 | Ids 80504-80511 inside #388's `Orleans` range; code and exception type only |
| 4 | Health Checks | ✅ Phase 5 | `OrleansClusterHealthCheck` (the item #388 deferred to this plan) |
| 5 | Validation | ❌ N/A | The routed request still runs the validation pipeline inside the grain's `IEncina.Send`; route templates are validated at startup (row C1); no separate validation of grain method inputs is added |
| 6 | Resilience | ❌ N/A | Orleans owns retries and reactivation; the inner `Send` keeps the application's own resilience behaviors; no second layer |
| 7 | Distributed Locks | ❌ N/A | One grain per key serializes work; the singleton scheduling driver replaces lock-based leader election |
| 8 | Transactions | ❌ N/A | The issue's "Orleans ACID transactions" are not used (Next Steps); the inner `Send` keeps `TransactionPipelineBehavior`; nothing here is atomic across the grain call and a commit |
| 9 | Idempotency | ❌ N/A | A duplicate-safe command uses the inbox behavior inside `Send`; the issue deferred grain-call deduplication with no owner, so no issue is opened |
| 10 | Multi-Tenancy | ✅ Phase 2 | Tenant-qualified injective keys, fail closed, tenant verification of #388 on every call (rows C1, C2); the issue deferred it, the plan integrates it because routed calls carry tenant data |
| 11 | Module Isolation | ❌ N/A | Grains are isolated units, as the issue says |
| 12 | Audit Trail | ⏭️ | Gated handlers inside `Send` audit through the existing behaviors; a grain-invocation audit is not added; #388 records the same deferral and no consumer is named |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| `Encina.Actors` and `Encina.Orleans` skeleton, `EncinaGrain<TState>`, `ActorGrainKey`, tenant filters, `OrleansDiagnostics`, `OrleansLog`, the TestCluster collection | Planned in #388 (G1 to G4) | Not on main yet; H1 starts after G4 merges |
| `Microsoft.Orleans.*` pins | Planned in #388 (G2, G3) | #459 adds none |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Saga commit owner | #720 | Defines how a saga step commits; the grain only serializes |
| Saga state machine | #297 | Timeout semantics of Design Choice 4 may follow it |
| Tenant tag helper | #2676 | H5 adds the tenant tag when it has merged |
| Aspire hosting | #416, #440 | Design Choice 6 option B would live there |
| Event-sourced actors | #330 | A grain host for deciders reuses `EncinaGrain` |

---

## Next Steps

1. Link this plan from #459 and ask the maintainer to answer Design Choices 1 to 6 when work starts; tell #388's author that this plan adds files to `Encina.Orleans` after G4 and registers its telemetry source in `Encina.OpenTelemetry`.
2. Implement H1 to H6 in order (H3 and H4 are independent after H2).
3. Follow-ups recorded here: Orleans streams for outbox publishing and observers for notification broadcasting (outside the issue's acceptance criteria), and an Orleans-transaction-backed unit of work. Maintainer action: amend the acceptance criteria of #459 that name `IGrainHandler<,>`, `[GrainHandler]` on the handler class, `config.MapHandlersToGrains`, saga grain timeout reminders as a default, `WithEncinaOrleans()`, a sample project and "Aspire dashboard integration" to the outcome of this plan (this plan never comments on an issue).
