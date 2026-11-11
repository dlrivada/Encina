# Implementation Plan: `Encina.StateMachine` — Fluent State Machine with Saga Integration

> **Issue**: [#297](https://github.com/dlrivada/Encina/issues/297)
> **Type**: Feature
> **Complexity**: Medium (8 phases, no database provider, about 40 files in 6 pull requests)
> **Estimated Scope**: ~1,800 lines of production code + ~2,800 lines of tests

---

## Summary

Implement a declarative, fluent state machine for entities with a lifecycle (orders, approvals, documents): states and triggers are enums, transitions are `Permit`, `PermitIf`, `PermitReentry` and `Ignore`, states have entry and exit actions (sync and async) and can be nested (`SubstateOf`), and the current state can live in the caller's own entity through an accessor. Every failure (a trigger the state does not permit, a guard that rejects, an action that fails) is an `Either<EncinaError, T>` `Left`, never an exception. A `DOT` export draws the machine. The machine complements the existing saga pattern: a durable coordinator persists the machine state in the saga data through `SagaOrchestrator`, so a long-running process can receive triggers over time.

**Standards and references**: none in law; the model is the Harel statechart subset implemented by the Stateless library (6k+ stars) and by MassTransit's Automatonymous, both named in the issue.

**Not in scope** (Stateless features the issue does not list): parameterised triggers (a payload per trigger), dynamic destinations, internal transitions, timers and a UML/Mermaid export. The DOT export covers visualization.

**Affected packages**: a new `Encina.StateMachine` (the machine, no messaging reference) and `Encina.Messaging` (the saga coordinator, next to `SagaDefinition`). Both locations depend on Dilemmas D1 and D2 below.

**Provider category**: none. The machine is in-memory logic; the saga integration reuses `ISagaStore` unchanged, so the 10-database-provider matrix does not apply (see "Provider Matrix" in Research).

**Integration points that exist today** (the issue's sketch used an `ISaga` that does not exist): `ISagaRunner`, `SagaDefinition<TData>`/`BuiltSagaDefinition<TData>` (`src/Encina.Messaging/Sagas/LowCeremony/`), `SagaOrchestrator` and `ISagaState` (`src/Encina.Messaging/Sagas/`). The machine integrates with `SagaOrchestrator` (Dilemma D3), not with a base class.

**Shared pieces with the saga family** (separate plans, same code): `SagaOrchestrator` is read and written by this plan, #1237 (correlation by external key and wait-for-event, which owns the lookup of a saga by key), #720 (transactional compensation), #738 (`TenantId` on `SagaState`) and #750 (audit of saga transitions). This plan owns `Encina.StateMachine`, the `statemachine.*` error codes and EventId range 5850-5899; it adds no member to `ISagaState`, `ISagaStore` or `SagaOrchestrator`, so it does not collide with their changes.

---

## Design Choices

<details>
<summary><strong>1. Engine — own implementation or a wrapper over Stateless (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Own engine** | `Either` semantics, `CancellationToken` and a deterministic failure model designed in; no third-party dependency to track; the machine can be a first-class citizen of the pipeline and the saga | Encina owns hierarchical states, guards, the entry/exit ordering and the DOT export (about 600 lines to maintain) |
| **B) Wrap Stateless** | A mature engine does the state logic; fewer lines | An extra dependency; its API throws exceptions and its async actions are `Func<Task>` without cancellation, so Encina needs a translation layer to `Either`; Stateless types leak or need mirroring in the public API |

### Chosen Option: **A — Own engine** (recommended, pending the maintainer)

### Rationale

- Option A wins when the machine must obey the repository rules for failure and time (`AGENTS.md` section 3: `Either`, no exceptions for business logic) without a translation layer; the engine is small because the issue excludes parameterised triggers and dynamic destinations.
- Option B wins when time to market matters more than API fit, or when the maintainer wants Stateless features beyond the issue's list (parameterised triggers, dynamic destinations) on day one.
- **Class:** Dilemma

</details>

<details>
<summary><strong>2. Package location — one package, inside Encina.Messaging, or a pure package plus a coordinator in Messaging (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New package `Encina.StateMachine` holding both the machine and the saga coordinator** | One place for the feature; pay-for-what-you-use at the package level | The saga coordinator needs `Encina.Messaging`, so every consumer of the package takes the messaging stack, which defeats the issue's own argument for a separate package |
| **B) Inside `Encina.Messaging`** | The coordinator sits next to `SagaDefinition` with no extra reference | Every messaging user carries the machine; an entity lifecycle outside messaging needs the messaging package |
| **C) Pure `Encina.StateMachine` (references only `Encina` core) plus the coordinator in `Encina.Messaging` (namespace `Encina.Messaging.Sagas.StateMachine`, references `Encina.StateMachine`)** | The pure machine is usable without messaging; the coordinator sits next to the saga code; each assembly carries only what it needs | Two places to document; `Encina.Messaging` gains a small project reference; two EventId ranges |

### Chosen Option: **C — Pure package plus the coordinator in Messaging** (recommended, pending the maintainer)

### Rationale

- Option C wins when entity lifecycles are used without messaging (the issue's order, approval and document examples) and the saga integration must still feel native. Option A wins when the maintainer prefers one package and accepts the messaging reference; Option B wins when the machine is only ever used with sagas.
- The plan is written for C; under A the coordinator files move to `src/Encina.StateMachine/Saga/` and one EventId range covers the package; under B all files move to `src/Encina.Messaging/StateMachine/`. The phases do not change.
- **Class:** Dilemma

</details>

<details>
<summary><strong>3. Saga integration — a step adapter over ISagaRunner, or a durable coordinator over SagaOrchestrator (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Step adapter: build a `BuiltSagaDefinition<TData>` whose steps each fire a trigger, and run it with `ISagaRunner.RunAsync`** | Reuses the runner end to end, including compensation in reverse order and the timeout; no new persistence code | A runner executes a fixed list of steps in one call, so the machine degenerates to a linear flow: no waiting for a trigger that arrives later, no branching on guards |
| **B) Durable coordinator over `SagaOrchestrator`: the machine state is a field of the saga data, `StartAsync` persists it, each `FireAsync(sagaId, trigger)` loads the saga, fires the machine and persists the new state with `AdvanceAsync`; a final state completes the saga, a failure state fails it** | Event-driven and long-running, the way MassTransit's state-machine sagas work; uses the persisted saga row and its status values; any trigger can arrive at any time | New coordinator code (about 250 lines); compensation is expressed as ordinary transitions (for example `Refund` to `Cancelled`) rather than the reverse-order step replay of `SagaRunner`; a concurrent trigger window exists until the saga row has concurrency control (#2213) |

### Chosen Option: **B — Durable coordinator over `SagaOrchestrator`** (recommended, pending the maintainer)

### Rationale

- The point of a state machine is to react to triggers over time; Option B wins in the issue's own examples (an order waits for payment, shipment, delivery). Option A wins when the process is a straight sequence that only wants named states for logging.
- `SagaOrchestrator` already exposes everything B needs (`StartAsync`, `AdvanceAsync` with an `updateData` function, `CompleteAsync`, `FailAsync`, `GetAsync`) and the status values `Running`, `Completed`, `Failed` (`src/Encina.Messaging/Sagas/SagaOrchestrator.cs`), so no store, entity or migration changes.
- **Class:** Dilemma

</details>

<details>
<summary><strong>4. Reentrant Fire — a trigger fired from inside an entry or exit action (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reject: `Fire` called while a transition runs returns `Left` `statemachine.reentrant_fire`** | The operation fails visibly (`AGENTS.md` section 3); simple, no hidden ordering; the state is never observed half-way | A chain such as "on entering Paid, fire Ship" cannot be written inside the action |
| **B) Queue: the inner trigger runs after the current transition completes (Stateless's default mode)** | Chains like "Paid then Ship" read naturally | The caller of the outer `Fire` gets success while an inner trigger may still fail later; the failure of the queued trigger needs a channel to be reported |

### Chosen Option: **A — Reject with a `Left`** (recommended, pending the maintainer)

### Rationale

- Option A wins when predictability matters more than convenience: failures are returned to the caller that caused them. Option B wins when the application has many automatic follow-up transitions and prefers the Stateless convention.
- Concurrent `Fire` calls from two threads on one instance are rejected the same way (the instance is not thread-safe; a shared lifecycle belongs to the saga coordinator, whose concurrency is #2213).
- **Class:** Dilemma

</details>

<details>
<summary><strong>5. A failing action — does the state change? (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The state changes only after every exit and entry action has succeeded; a failing action returns `Left` and the accessor is not written** | The persisted state never claims an entry whose side effects did not complete; the caller can retry the same trigger | Actions that already ran are not undone (the plan documents it); a retry re-runs the earlier actions |
| **B) The state changes before the entry actions run; a failing entry action returns `Left` with the machine already in the new state** | The state reflects that the transition was taken; a retry does not repeat exit actions | A persisted state can name an entry whose action failed, which the next reader cannot tell apart from success |

### Chosen Option: **A — Commit the state after all actions succeed** (recommended, pending the maintainer)

### Rationale

- Option A wins when entry actions have important side effects (reserve inventory, send a notice) and the state must mean "done". Option B wins when entry actions are best-effort notifications and the machine must keep progressing.
- An action that throws is caught and returned as `Left` `statemachine.action_failed`; the error carries the exception type name only, never its message (`AGENTS.md` section 3, `EncinaError.Message` rule).
- **Class:** Dilemma

</details>

<details>
<summary><strong>6. Failures are Left values</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Either<EncinaError, T>` for every runtime failure of `Fire`, `FireAsync` and the coordinator** | Matches the repository rule; failures are values the caller must handle | Differs from Stateless, which throws `InvalidOperationException` for an unpermitted trigger |
| **B) Throw for an unpermitted trigger, as Stateless does** | Familiar to Stateless users | Forbidden for business logic; an unpermitted trigger is a normal runtime outcome |

### Chosen Option: **A — `Either` everywhere at runtime**

### Rationale

- **Class:** Settled
- **Settled by:** `AGENTS.md` section 3 (Railway Oriented Programming: operations return `Either<EncinaError, T>`, never exceptions for business logic), ADR-001, ADR-006.
- A programmer error while building the definition (no states configured, a trigger permitted twice from one state without guards) throws `InvalidOperationException` from `Build()`, the same boundary as `SagaDefinition<TData>.Build()` (`src/Encina.Messaging/Sagas/LowCeremony/SagaDefinition.cs`, line 115).

</details>

<details>
<summary><strong>7. Public shape: an immutable definition, instances per entity, and Fire returning TransitionInfo</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `StateMachineBuilder.Build()` returns an immutable `StateMachineDefinition<TState,TTrigger>`; `definition.Create(accessor)` returns an `IStateMachine<TState,TTrigger>` per entity; `Fire` returns `Either<EncinaError, TransitionInfo<TState,TTrigger>>`** | One definition is shared by every entity (built once, DI-friendly); the instance holds only the state; the coordinator reads from and to states from the result | Two types instead of the issue's single `StateMachine` |
| **B) `Build()` returns a mutable machine that owns its state, `Fire` returns `Unit`** | Exactly the issue's sketch | One machine per entity re-builds the graph; the saga coordinator cannot report the transition |

### Chosen Option: **A — Definition plus instance**

### Rationale

- **Class:** Trivial
- **Settled by:** the precedent in the repository: `SagaDefinition<TData>.Build()` returns the immutable `BuiltSagaDefinition<TData>` and `ISagaRunner.RunAsync` returns `SagaResult<TData>` rather than `Unit` (`src/Encina.Messaging/Sagas/LowCeremony/ISagaRunner.cs`).
- `IStateMachineStateAccessor<TState>` (`GetState`, `SetState`) from the issue stays; `StateMachineBuilder.CreateWithAccessor(...)` of the issue becomes `definition.Create(getState, setState)` and `definition.Create(initialState)` for an in-memory instance.

</details>

<details>
<summary><strong>8. Async signatures, guards and actions</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `FireAsync(trigger, CancellationToken)` returns `ValueTask<Either<...>>`; async actions take the transition context and a token; `Fire(trigger)` is a synchronous shortcut that returns `Left` `statemachine.async_action_requires_fire_async` when an async action is on the path** | Cancellation everywhere; matches `ISagaRunner`; sync users pay no async cost | Two entry points |
| **B) Only async, `Func<Task>` actions as in the issue sketch** | One entry point | No cancellation; blocks sync callers on `.GetAwaiter().GetResult()` |

### Chosen Option: **A — Token-aware async plus a sync shortcut**

### Rationale

- **Class:** Trivial
- **Settled by:** the repository convention that every async method takes a `CancellationToken` and returns `ValueTask`/`Task` of `Either` (`ISagaRunner.RunAsync`, `SagaOrchestrator.StartAsync`); the issue's sketch omits the token, and the plan adds it.
- Guards are `Func<bool>` as in the issue; an optional description string feeds the DOT export. A guard that throws returns `Left` `statemachine.guard_failed` with the exception type only.

</details>

<details>
<summary><strong>9. Registration of the saga coordinator</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AddEncinaStateMachineSagas()` registers the coordinator (scoped, `TryAdd`) and its metrics, requires `UseSagas`, and fails with a clear startup error when `SagaOrchestrator` is not registered** | Opt-in, complete, the pure machine needs no registration at all | One more extension method |
| **B) Register the coordinator inside `RegisterSagas` whenever sagas are on** | No call needed | Every saga user pays for a feature they did not ask for |

### Chosen Option: **A — Explicit opt-in registration**

### Rationale

- **Class:** Settled
- **Settled by:** `AGENTS.md` section 1 (pay-for-what-you-use: every feature opt-in) and section 3 (registration completeness: the `AddEncina*` method registers every dependency it resolves, proven by a DI test built with `ValidateOnBuild` and `ValidateScopes`); ADR-050 lists the module requirements, and the saga row requires the saga store.
- The pure machine (definition, instance) has no DI registration: it is a plain object built by the application.

</details>

<details>
<summary><strong>10. Telemetry names and cardinality</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) ActivitySource and Meter `Encina.StateMachine`; span `encina.statemachine.fire`; counter `encina.statemachine.transition_count`; tags `state.from`, `state.to`, `trigger`, `outcome`** | Matches the names in the issue; state and trigger are enum names, so cardinality is bounded by the definition | Applications with huge enums get many series |
| **B) No state or trigger tags, only `outcome`** | Minimal cardinality | Loses the signal that matters (which transition) |

### Chosen Option: **A — The issue's names with bounded tags**

### Rationale

- **Class:** Trivial
- **Settled by:** `SagaActivitySource` (`src/Encina.Messaging/Diagnostics/SagaActivitySource.cs`: dedicated source, `HasListeners()` guard, `encina.saga.transition` span) and `MessagingStoreMetrics` (`Counter<long>` fields); `AGENTS.md` section 7 for logging.
- The coordinator's spans and counters use the same names with the tag `saga.id` on the span only (never on the counter).

</details>

---

## Implementation Phases

### Phase 1: Core Models, Errors & Abstractions

> **Goal**: The public contract of the pure package, with no behavior yet.

<details>
<summary>Tasks</summary>

#### New project: `src/Encina.StateMachine/`

1. **Project file** `Encina.StateMachine.csproj`: `net10.0`, nullable, implicit usings, XML docs, `Microsoft.CodeAnalysis.PublicApiAnalyzers`, references `Encina` core, `LanguageExt.Core`, `Microsoft.Extensions.Logging.Abstractions`. Copy the layout of `src/Encina.Hangfire/Encina.Hangfire.csproj`. Add the project to `Encina.slnx`.
2. **`IStateMachine<TState, TTrigger>`** (`where TState : struct, Enum where TTrigger : struct, Enum`): `TState CurrentState { get; }`, `Either<EncinaError, TransitionInfo<TState, TTrigger>> Fire(TTrigger trigger)`, `ValueTask<Either<EncinaError, TransitionInfo<TState, TTrigger>>> FireAsync(TTrigger trigger, CancellationToken cancellationToken = default)`, `bool CanFire(TTrigger trigger)`, `IReadOnlyList<TTrigger> PermittedTriggers { get; }`.
3. **`IStateMachineStateAccessor<TState>`**: `TState GetState()`, `void SetState(TState state)`.
4. **`TransitionInfo<TState, TTrigger>`**: sealed record `(TState From, TState To, TTrigger Trigger, bool IsReentry, bool IsIgnored)`.
5. **`TransitionContext<TState, TTrigger>`**: sealed record handed to actions `(TState From, TState To, TTrigger Trigger)`.
6. **`StateMachineErrorCodes`** (public static class): `statemachine.trigger_not_permitted`, `statemachine.guard_failed`, `statemachine.ambiguous_transition`, `statemachine.reentrant_fire`, `statemachine.action_failed`, `statemachine.async_action_requires_fire_async`, `statemachine.saga_not_found`, `statemachine.saga_not_running` (the last two are used by the coordinator; the style follows `SagaErrorCodes`, `src/Encina.Messaging/Sagas/SagaErrorCodes.cs`).
7. **`StateMachineErrors`** (internal): factory methods over `EncinaErrors.Create(code, message)` (`src/Encina/Results/EncinaErrors.cs`) with messages that carry state and trigger enum names only.
8. **EventId range**: add `StateMachine = (5850, 5874)` and `MessagingStateMachineSagas = (5875, 5899)` to `src/Encina/Diagnostics/EventIdRanges.cs`, map `Encina.StateMachine` to the first and add the second to the `Encina.Messaging` entry of `AssemblyRanges` in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (line 91), and add both fields to `src/Encina/PublicAPI.Unshipped.txt`.
9. **`PublicAPI.Shipped.txt`** (empty) and **`PublicAPI.Unshipped.txt`** with all public symbols.
10. **Coverage manifest** `.github/coverage-manifest/Encina.StateMachine.json` with per-file targets and one-sentence justifications (see Testing).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```
CONTEXT:
- Encina is a .NET 10 / C# 14 library using Railway Oriented Programming (Either<EncinaError, T>).
- This phase creates the NEW project src/Encina.StateMachine/ with the public contract only (no engine).
- Issue #297; plan docs/plans/state-machine-implementation-plan-297.md. Dilemmas D1-D5 are recommended, pending the maintainer: read the Maintainer Decisions section if it exists and follow the answers.

TASK:
Create the project, the interfaces, records, error codes, the EventId range registration and the PublicAPI files listed in Phase 1 Tasks.

KEY RULES:
- Sealed records, XML docs on every public member, nullable enabled, no [Obsolete].
- Error codes are lower-case dotted strings prefixed statemachine.; error messages carry enum names only, never exception messages.
- Register the EventId range in src/Encina/Diagnostics/EventIdRanges.cs BEFORE any [LoggerMessage] uses it; update AssemblyRanges in EncinaEventIdAllocationTests.cs.
- Zero warnings; PublicAPI.Unshipped.txt lists every public symbol.

REFERENCE FILES:
- src/Encina.Messaging/Sagas/LowCeremony/ISagaRunner.cs (signature style, result record)
- src/Encina.Messaging/Sagas/SagaErrorCodes.cs (error code style)
- src/Encina/Results/EncinaErrors.cs (error factory)
- src/Encina.Hangfire/Encina.Hangfire.csproj (project layout)
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

### Phase 2: Builder, State Configuration & Definition

> **Goal**: The fluent API and the immutable compiled definition, including hierarchy, guards and DOT export.

<details>
<summary>Tasks</summary>

1. **`StateMachineBuilder<TState, TTrigger>`**: `static Create()`, `State(TState)` returning a `StateConfiguration`, `Build()` returning `StateMachineDefinition<TState, TTrigger>`. `Build()` throws `InvalidOperationException` for programmer errors: no state configured, a superstate cycle, an undeclared destination state reachable by `Permit`, or the same trigger permitted twice from one state without distinct guards.
2. **`StateConfiguration<TState, TTrigger>`** (fluent): `Permit`, `PermitIf(trigger, destination, Func<bool> guard, string? guardDescription = null)`, `PermitReentry`, `Ignore`, `OnEntry(Action<TransitionContext<...>>)`, `OnEntryAsync(Func<TransitionContext<...>, CancellationToken, Task>)`, `OnExit`, `OnExitAsync`, `SubstateOf(TState superstate)`. An overload per action form that returns `ValueTask<Either<EncinaError, Unit>>` lets an action return a typed failure instead of throwing.
3. **`StateMachineDefinition<TState, TTrigger>`** (immutable): the compiled graph (per-state permits, guards, actions, parent), `Create(TState initial)`, `Create(Func<TState> getState, Action<TState> setState)`, `Create(IStateMachineStateAccessor<TState>)`, `IReadOnlyCollection<TState> States`, `bool IsDescendantOf(TState state, TState ancestor)` and `string ToDotGraph()`.
4. **Resolution rules** (Stateless semantics, Trivial): a trigger is looked up in the current state, then in its superstates, nearest first; the first permit whose guard passes wins; two guards passing return `Left` `statemachine.ambiguous_transition`; `Ignore` returns a no-op result with `IsIgnored = true`. On a transition the exit actions run from the source up to, not including, the common ancestor, then the entry actions from just below the common ancestor down to the destination; `PermitReentry` exits and re-enters the same state.
5. **`ToDotGraph()`**: a deterministic `digraph` with one node per state, one edge per permit labelled with trigger and guard description, substates as `subgraph cluster_*`; golden-file tested.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```
CONTEXT:
- Phase 1 created src/Encina.StateMachine/ with IStateMachine, IStateMachineStateAccessor, TransitionInfo, TransitionContext and StateMachineErrorCodes.
- This phase adds the builder, the state configuration and the immutable definition (issue #297).

TASK:
Implement StateMachineBuilder, StateConfiguration and StateMachineDefinition as listed in Phase 2 Tasks, with the resolution rules of task 4 and the DOT export of task 5.

KEY RULES:
- Build() throws InvalidOperationException only for programmer errors in the definition (same boundary as SagaDefinition<TData>.Build()); everything at runtime is a Left.
- The definition is immutable and thread-safe; instances are not.
- Guards and actions that throw are converted by the machine (Phase 3) to Left with the exception type name only.
- No exceptions for flow control, no Thread.Sleep, TimeProvider is not needed here.
- XML docs with an <example> on the builder, PublicAPI.Unshipped.txt updated, zero warnings.

REFERENCE FILES:
- src/Encina.Messaging/Sagas/LowCeremony/SagaDefinition.cs (fluent builder, built definition)
- src/Encina.Messaging/Sagas/LowCeremony/SagaStepBuilder.cs
- docs/plans/state-machine-implementation-plan-297.md (Design Choices 6-8)
```

</details>

---

### Phase 3: The Machine Instance

> **Goal**: `Fire`, `FireAsync`, `CanFire` and `PermittedTriggers` with the failure semantics of D4 and D5.

<details>
<summary>Tasks</summary>

1. **`StateMachine<TState, TTrigger>`** (sealed, created by `StateMachineDefinition.Create`, implements `IStateMachine<,>`): reads the state through the accessor, resolves the transition (Phase 2 rules), runs exit then entry actions in order, and writes the destination with `SetState` only after every action succeeded (D5 recommendation).
2. **Reentrancy guard**: an `Interlocked` flag set for the duration of `Fire`/`FireAsync`; a second call returns `Left` `statemachine.reentrant_fire` (D4 recommendation).
3. **`Fire` (sync)** returns `Left` `statemachine.async_action_requires_fire_async` when the resolved path contains an async action; `FireAsync` runs sync and async actions in order and observes the token before each action (a cancelled token propagates `OperationCanceledException`, cancellation is not an error).
4. **`CanFire` / `PermittedTriggers`**: evaluate the guards without running actions; a throwing guard counts as not permitted.
5. **Exception handling**: an action or guard exception becomes `Left` `statemachine.action_failed` / `statemachine.guard_failed` with the exception type name only (`AGENTS.md` section 3).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```
CONTEXT:
- Phases 1 and 2 provide the contract and the compiled StateMachineDefinition (issue #297).
- Dilemmas D4 (reentrant Fire) and D5 (failing action) are recommended, pending the maintainer: follow the Maintainer Decisions section if it exists.

TASK:
Implement StateMachine<TState, TTrigger> as listed in Phase 3 Tasks.

KEY RULES:
- Every runtime failure is Either.Left; the Left carries a code from StateMachineErrorCodes and never an exception message.
- OperationCanceledException from the caller's token propagates (cancellation is not an error).
- The accessor is written exactly once per successful transition and never on failure.
- Instances are not thread-safe; the reentrancy flag makes misuse fail visibly.
- CRAP <= 10 on every method: split resolution, action running and telemetry into small methods.

REFERENCE FILES:
- src/Encina.Messaging/Sagas/LowCeremony/SagaRunner.cs (step execution, exception to Left conversion)
- src/Encina.Messaging/Sagas/SagaOrchestrator.cs
```

</details>

---

### Phase 4: Saga Integration — the Durable Coordinator

> **Goal**: A state machine whose state is persisted in the saga row (Dilemma D3, Option B; location per D2, Option C).

<details>
<summary>Tasks</summary>

1. **`StateMachineSagaDefinition<TState, TTrigger, TData>`** (`src/Encina.Messaging/Sagas/StateMachine/`, `where TData : class, new()`): `SagaType` (string), the `StateMachineDefinition<TState, TTrigger>`, `Func<TData, TState> GetState`, `Action<TData, TState> SetState`, `InitialState`, `CompleteIn(params TState[])` (the saga is completed when the machine enters one of them), `FailIn(string errorCode, params TState[])` (the saga is failed, the code is never free text), and an optional timeout. This replaces the issue's `StateMachineSaga` base class and its `GetState`/`SetState` overrides.
2. **`IStateMachineSagaCoordinator`** and its implementation `StateMachineSagaCoordinator`: constructor takes `SagaOrchestrator`, `ILogger`, and an optional `TimeProvider`. Methods: `Task<Either<EncinaError, Guid>> StartAsync<TState, TTrigger, TData>(definition, TData initialData, CancellationToken)` (sets the initial state, calls `SagaOrchestrator.StartAsync`) and `Task<Either<EncinaError, TransitionInfo<TState, TTrigger>>> FireAsync<TState, TTrigger, TData>(definition, Guid sagaId, TTrigger trigger, CancellationToken)`.
3. **`FireAsync` flow**: `SagaOrchestrator.GetAsync<TData>` (`None` becomes `Left` `statemachine.saga_not_found`; a status other than `Running` becomes `Left` `statemachine.saga_not_running`); create a machine instance over an accessor bound to the loaded data; fire; on `Left`, return it and persist nothing (the saga stays `Running`); on `Right`, persist with `SagaOrchestrator.AdvanceAsync(sagaId, updateData: d => { setState(d, to); return d; })` (one `UpdateAsync` on the saga row writes state and data together); then `CompleteAsync` when the new state is in `CompleteIn`, or `FailAsync(sagaId, code)` when it is in `FailIn`. A persistence `Left` is returned, never swallowed (`AGENTS.md` section 3).
4. **Compensation**: expressed as transitions of the machine (for example `Refund` from `Paid` to `Cancelled`); the reverse-order step replay of `SagaRunner` is not used. Transactional safety of compensation is #720.
5. **Correlation**: the coordinator is addressed by `sagaId`; lookup by an external key and waiting for an event are #1237's, which can call `FireAsync` with the saga it resolves.
6. **No change** to `ISagaState`, `ISagaStore`, `SagaOrchestrator` or any of the saga stores (the nine store files, the EF Core store serving three databases).

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```
CONTEXT:
- Phases 1-3 deliver the pure Encina.StateMachine package. This phase adds the saga coordinator in Encina.Messaging (Dilemma D2 Option C; under Option A or B only the folder changes).
- Dilemma D3 is recommended, pending the maintainer: a durable coordinator over SagaOrchestrator.
- Siblings: #1237 (correlation by key), #720 (transactional compensation), #738 (TenantId), #750 (audit), #2213 (concurrency on the saga row). Do not touch their code.

TASK:
Implement StateMachineSagaDefinition, IStateMachineSagaCoordinator and StateMachineSagaCoordinator as listed in Phase 4 Tasks.

KEY RULES:
- Use only the public members of SagaOrchestrator (StartAsync, AdvanceAsync, CompleteAsync, FailAsync, GetAsync); add nothing to it.
- A Left from the machine or from the orchestrator is returned to the caller; nothing reports success on failure.
- The error code passed to FailAsync is a constant, never exception text or user input.
- Time comes from TimeProvider if needed (injection, default TimeProvider.System); no DateTime.UtcNow.
- Log with [LoggerMessage] in range 5875-5899 (registered in Phase 1), error codes only, no EncinaError.Message.

REFERENCE FILES:
- src/Encina.Messaging/Sagas/SagaOrchestrator.cs
- src/Encina.Messaging/Sagas/LowCeremony/SagaRunner.cs
- src/Encina.Messaging/Sagas/ISagaState.cs
- tests/Encina.UnitTests/Messaging/Sagas/SagaOrchestratorTests.cs
```

</details>

---

### Phase 5: Configuration & DI

> **Goal**: Opt-in registration, complete by `ValidateOnBuild`.

<details>
<summary>Tasks</summary>

1. **`StateMachineServiceCollectionExtensions.AddEncinaStateMachineSagas(this IServiceCollection)`** in `src/Encina.Messaging/Sagas/StateMachine/`: `TryAddScoped<IStateMachineSagaCoordinator, StateMachineSagaCoordinator>()`; the metrics class is registered as a singleton with `TryAddSingleton`. It resolves `SagaOrchestrator`, which `RegisterSagas` registers when `UseSagas` is on (`src/Encina.Messaging/MessagingServiceCollectionExtensions.cs`, line 365).
2. **Startup check**: when `UseSagas` is off the coordinator cannot be built; the DI test proves the failure is a clear message at provider build or first resolve, not a silent default.
3. **No options class**: the coordinator has no tunables in this plan. Timeouts come from `SagaDefinition`/`SagaOptions.DefaultSagaTimeout`.
4. **DI test** building the provider with `ValidateOnBuild` and `ValidateScopes` (registration completeness rule).

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```
CONTEXT:
- The coordinator (Phase 4) is the only service the feature registers; the pure machine needs no DI.

TASK:
Implement AddEncinaStateMachineSagas and its DI test.

KEY RULES:
- TryAdd* registrations; the application's own registration always wins.
- The test builds the provider with ValidateOnBuild and ValidateScopes, with UseSagas on and with it off.
- No sync-over-async, no static state.

REFERENCE FILES:
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs (RegisterSagas)
- tests/Encina.UnitTests/Messaging/Sagas/AdoDapperMessagingDiRegistrationTests.cs
- docs/architecture/adr/050-modules-may-require-modules.md
```

</details>

---

### Phase 6: Cross-Cutting Integration

> **Goal**: Wire the functions marked Include in the matrix and record the deferrals.

<details>
<summary>Tasks</summary>

1. **Transactions (Include)**: state and data are written by one `ISagaStore.UpdateAsync` through `AdvanceAsync`, so the persisted transition is atomic; when the caller dispatches through `IEncina.Send` with the transaction behavior active, the write joins that unit of work. Atomic compensation is #720.
2. **OpenTelemetry and logging (Include)**: Phase 7.
3. **Deferred**: concurrent and duplicate triggers on one saga (a lock or a version check, and a deduplication key) go into the issue file `artifacts/issues/state-machine-saga-concurrent-triggers.md`, linked to #2213 (concurrency control on the saga row); multi-tenancy is #738, module isolation is #747, audit of transitions is #750 (`TransitionInfo` is the data it records).
4. **Not applicable**: caching, health checks, validation and resilience, with the reasons in the matrix.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```
CONTEXT:
- Cross-cutting matrix of docs/plans/state-machine-implementation-plan-297.md; AGENTS.md section 6.

TASK:
Verify that the coordinator persists state and data in a single AdvanceAsync call, that a Left never leaves a half-written state, and add the unit tests that prove it. Do not add locks, tenant or audit code in this plan.

KEY RULES:
- A test fails if a persistence Left is swallowed.
- Deferred items stay deferred: reference #2213, #738, #747, #750 in XML remarks where a reader would expect them.

REFERENCE FILES:
- src/Encina.Messaging/Sagas/SagaOrchestrator.cs (AdvanceAsync)
- src/Encina.Messaging/TransactionPipelineBehavior.cs
```

</details>

---

### Phase 7: Observability

> **Goal**: Traces, a counter and structured logs for the machine and the coordinator.

<details>
<summary>Tasks</summary>

1. **`StateMachineActivitySource`** (internal, `src/Encina.StateMachine/Diagnostics/`): source `Encina.StateMachine`, `HasListeners()` guard, span `encina.statemachine.fire` with tags `state.from`, `state.to`, `trigger`, `outcome`.
2. **`StateMachineMetrics`** (internal, `Meter` `Encina.StateMachine`): `Counter<long>` `encina.statemachine.transition_count` with tags `state.from`, `state.to`, `trigger`, `outcome`; the coordinator adds the span tag `saga.id` (span only).
3. **`StateMachineLogMessages`** (`[LoggerMessage]`, range 5850-5874): permitted transition, ignored trigger, unpermitted trigger, guard rejected, action failed, reentrant fire. Coordinator messages in `src/Encina.Messaging/Diagnostics/` use 5875-5899. EventIds are packed sequentially; the XML docs name the range. Errors are logged by code, exceptions through `ForLogging()`.
4. **Architecture test**: `EncinaEventIdAllocationTests` passes with both ranges mapped (Phase 1).
5. **Machine without DI**: a standalone machine takes an optional `ILogger` through `definition.Create(...)`; no logger means no logging, no listener means no span.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```
CONTEXT:
- Ranges 5850-5874 (Encina.StateMachine) and 5875-5899 (the coordinator in Encina.Messaging) were registered in Phase 1.

TASK:
Implement the ActivitySource, the metrics class and the [LoggerMessage] classes of Phase 7 Tasks and call them from Fire/FireAsync and the coordinator.

KEY RULES:
- Use the [LoggerMessage] source generator; no LoggerMessage.Define.
- EventIds sequential, inside the registered range; run the architecture tests.
- Never log EncinaError.Message or an exception message; log the error code and the exception type via ForLogging().
- Counter tags are state/trigger enum names and outcome only (bounded cardinality).

REFERENCE FILES:
- src/Encina.Messaging/Diagnostics/SagaActivitySource.cs
- src/Encina.Messaging/Diagnostics/MessagingStoreMetrics.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 8: Testing and Documentation & Finalization

> **Goal**: Per-flag coverage at the manifest targets and the documentation set.

<details>
<summary>Tasks</summary>

#### Testing (`tests/Encina.<Flag>Tests/StateMachine/` and `tests/Encina.UnitTests/Messaging/Sagas/StateMachine/`)

1. **Unit** (real implementations): every transition rule (permit, guard, reentry, ignore, ambiguity), hierarchy entry/exit order, sync and async actions, failing and throwing actions (D5), reentrant `Fire` (D4), accessor round trip, DOT golden file, the coordinator against a minimal in-test `ISagaStore` (start, fire, complete, fail, not found, not running, persistence `Left`), the DI test (Phase 5), telemetry through a listener.
2. **Guard**: `ArgumentNullException` for every public parameter of the builder, configuration, definition, instance and coordinator.
3. **Contract**: `IStateMachine<,>` and `IStateMachineStateAccessor<>` keep their shape, using the real implementation.
4. **Property (FsCheck)**: a machine never reaches a state that is not reachable by permitted triggers; `CanFire` agrees with `Fire`; a failed `Fire` never changes the state.
5. **Integration**: one test class on the existing ADO.NET PostgreSQL saga store (`[Collection("ADO-PostgreSQL")]`, `[Trait("Category", "Integration")]`) drives start, fire and complete end to end. The coordinator touches only `ISagaStore`, which the plan does not change, so repeating it on the other nine providers is not part of this feature; the provider stores already have their own integration tests.
6. **Load and Benchmark**: justification files `tests/Encina.LoadTests/StateMachine/StateMachine.md` and `tests/Encina.BenchmarkTests/StateMachine/StateMachine.md` (instances hold no shared state; the definition is immutable). A benchmark is added only if the measured cost of `Fire` shows it is a hot path.
7. **Per-file targets** in `.github/coverage-manifest/Encina.StateMachine.json` (and `Encina.Messaging.json` for the new coordinator files), each flag with a one-sentence justification: the engine files target unit 90 and guard 90, property 60 on the resolution file; the interfaces and records follow the manifest default rules (no coverable lines for interfaces). Measure with `dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage"` and `.github/scripts/coverage-report.cs`; run `-- --check-justifications`.

#### Documentation (mandatory set)

8. XML docs with `<example>` on all new public APIs; `PublicAPI.Unshipped.txt` for `Encina.StateMachine`, `Encina.Messaging` and `Encina` (EventIdRanges fields).
9. `changelog.d/297-state-machine.added.md`; `ROADMAP.md` (the state machine row); package README `src/Encina.StateMachine/README.md`; `docs/features/state-machine.md` (usage guide, order example, saga example, DOT export); `docs/INVENTORY.md` (new package and files); an ADR in `docs/architecture/adr/` recording the decided D1-D5 (next free number: read `docs/architecture/adr/index.md`, which reserves some numbers); `docs/releases/v0.18.0/` notes (the issue's milestone).
10. `dotnet build --configuration Release` with 0 errors and 0 warnings; `dotnet test` with every flag at its target.

</details>

<details>
<summary>Prompt for AI Agents — Phase 8</summary>

```
CONTEXT:
- Phases 1-7 are implemented. Per-flag coverage is the obligation model of AGENTS.md section 9; NEVER FluentAssertions; assertions use Shouldly through Encina.Testing.Shouldly.

TASK:
Write the tests of Phase 8 Tasks 1-6, fill the manifest targets with justifications, then write the documentation set of tasks 8-10 (documentation pages go through the docs-writer agent).

KEY RULES:
- Tests execute real package code; no reflection-only tests.
- Integration test uses the shared [Collection("ADO-PostgreSQL")] fixture and ClearAllDataAsync in InitializeAsync; never IClassFixture, never a per-class fixture.
- A test never swallows an exception thrown by the code it drives.
- The PR reports the measured per-flag coverage of the new files against the manifest targets.

REFERENCE FILES:
- tests/Encina.UnitTests/Messaging/Sagas/SagaRunnerTests.cs
- docs/testing/coverage-measurement-methodology.md
- .github/coverage-manifest/Encina.Messaging.json
```

</details>

---

### Proposed split into pull requests (limit 15 files and 1500 lines each)

| PR | Content | Approx. files | Approx. lines |
|----|---------|---------------|---------------|
| 1 | Phase 1: project, contract, errors, EventId ranges, PublicAPI, manifest, slnx | 13 | 450 |
| 2 | Phases 2-3: builder, configuration, definition, machine, DOT, unit and guard tests | 11 | 1,300 |
| 3 | Phase 8 tests for the engine: contract, property, load and benchmark justifications | 7 | 600 |
| 4 | Phase 7: observability, listener tests, EventId tests | 8 | 600 |
| 5 | Phases 4-6: coordinator, definition, DI, unit and integration tests, Messaging manifest | 14 | 1,300 |
| 6 | Documentation: README, guide, ADR, changelog, ROADMAP, INVENTORY, release notes (through docs-writer) | 7 | 500 |

---

## Research

### Relevant Standards & Specifications

| Reference | What it defines | Use in this feature |
|-----------|-----------------|---------------------|
| Harel statecharts (hierarchical states) | Superstates, entry/exit ordering | Semantics of `SubstateOf` and the entry/exit path |
| Stateless library (dotnet-state-machine/stateless) | Fluent `Permit`/`PermitIf`/`PermitReentry`/`Ignore`, guards | API vocabulary named in the issue |
| MassTransit Automatonymous state-machine sagas | Event-driven saga state machines persisted per instance | Model for the durable coordinator (D3) |
| ADR-001, ADR-006 | Railway Oriented Programming | Failures are `Left` |
| ADR-018, ADR-021, ADR-050 | Cross-cutting check, EventIds, module requirements | Matrix, ranges, registration |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `ISagaRunner` / `SagaResult<TData>` | `src/Encina.Messaging/Sagas/LowCeremony/ISagaRunner.cs` | Model for `ValueTask<Either<EncinaError, T>>` plus `CancellationToken` signatures |
| `SagaDefinition` / `BuiltSagaDefinition<TData>` | `src/Encina.Messaging/Sagas/LowCeremony/SagaDefinition.cs` | Precedent for an immutable built definition; `Build()` throws `InvalidOperationException` on programmer error |
| `SagaOrchestrator` | `src/Encina.Messaging/Sagas/SagaOrchestrator.cs` | Persistence of the durable integration: `StartAsync`, `AdvanceAsync`, `CompleteAsync`, `FailAsync`, `GetAsync` |
| `ISagaState`, `SagaStatus` | `src/Encina.Messaging/Sagas/ISagaState.cs`, `SagaOrchestrator.cs` | Persisted statuses `Running`, `Completed`, `Compensating`, `Compensated`, `Failed`, `TimedOut` |
| `SagaErrorCodes` | `src/Encina.Messaging/Sagas/SagaErrorCodes.cs` | Error code style; new codes use `statemachine.*` |
| `SagaActivitySource` | `src/Encina.Messaging/Diagnostics/SagaActivitySource.cs` | ActivitySource pattern with a `HasListeners()` guard |
| `MessagingStoreMetrics` | `src/Encina.Messaging/Diagnostics/MessagingStoreMetrics.cs` | `Counter<long>` pattern |
| `RegisterSagas` | `src/Encina.Messaging/MessagingServiceCollectionExtensions.cs` | Registers `SagaOrchestrator` and `ISagaRunner` when `UseSagas` is on |
| `EventIdRanges` | `src/Encina/Diagnostics/EventIdRanges.cs` | Registry for the two new ranges |
| `TimeProvider` | .NET BCL | Injected where a timestamp is needed |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` (saga store) | 2200-2299 | Existing `MessagingSaga`, unchanged |
| `Encina.Messaging` (orchestrators and runners) | 2800-2999 | Existing `Messaging`, unchanged |
| `Encina.Messaging` (recoverability) | 5450-5499 | Existing `MessagingRecoverability` |
| **`Encina.StateMachine`** | **5850-5874** | **New `StateMachine`: permitted, ignored, rejected, failed, reentrant** |
| **`Encina.Messaging` (state machine sagas)** | **5875-5899** | **New `MessagingStateMachineSagas`: coordinator start, fire, complete, fail** |

The block 5850-5899 is assigned to the saga state machine family in the plan batch registry (the free area 5500-6999 of `AGENTS.md` section 7 is shared with other families). The other plans of this family take ids inside their packages' existing ranges, and this plan does not use anything beyond 5850-5899.

### Provider Matrix

| Provider category | Applies | Reason |
|-------------------|---------|--------|
| Database (10) | No | The coordinator uses `ISagaStore` unchanged; no SQL, no connection type, no store |
| Caching, Transport, Locks, Validation, Cloud | No | The machine is in-memory logic |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| `Encina.StateMachine` source | ~16 | Contract, builder, configuration, definition, machine, DOT, diagnostics, project, PublicAPI x2, README |
| `Encina.Messaging` coordinator | ~5 | Definition, interface, implementation, DI extension, log messages |
| Registry and manifests | ~5 | `EventIdRanges.cs`, `AssemblyRanges` test, two manifests, `Encina.slnx` |
| Tests | ~18 | Unit, guard, contract, property, integration, DI, three justification files |
| Documentation | ~7 | Guide, ADR, changelog fragment, ROADMAP, INVENTORY, release notes |
| **Total** | **~51 in 6 pull requests** | |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Railway Oriented Programming (Either<EncinaError, T>); no [Obsolete], no compatibility layers; XML docs on every public API; zero warnings.
- Issue #297: a fluent state machine (Permit, PermitIf, PermitReentry, Ignore, OnEntry/OnExit sync and async, SubstateOf, external state accessor, DOT export) and a saga integration.
- The saga integration points are ISagaRunner, SagaDefinition and SagaOrchestrator (there is no ISaga). Plan: docs/plans/state-machine-implementation-plan-297.md. Dilemmas D1-D5 are recommended, pending the maintainer; read the Maintainer Decisions section first if it exists.

IMPLEMENTATION OVERVIEW:
1. New project src/Encina.StateMachine: contract, errors, EventId ranges 5850-5899 registered first (Phase 1).
2. Builder, configuration, immutable definition, DOT export (Phase 2); machine instance with Left-only failures (Phase 3).
3. In Encina.Messaging: StateMachineSagaDefinition and a coordinator over SagaOrchestrator that persists the state in the saga data (Phase 4); AddEncinaStateMachineSagas (Phase 5).
4. Cross-cutting (Phase 6), observability (Phase 7), tests and documentation (Phase 8).

KEY PATTERNS:
- Failures are Left; Build() throws only for programmer errors; exceptions in guards and actions become Left with the exception type name only.
- Definitions are immutable and shared; instances are not thread-safe; a reentrant or concurrent Fire returns Left.
- Time from TimeProvider; Message of EncinaError never reaches logs or tags; exceptions go through ForLogging().
- TryAdd* registrations; a DI test with ValidateOnBuild and ValidateScopes.
- Tests: Shouldly through Encina.Testing.Shouldly, real implementations, per-flag coverage from the manifest, CRAP <= 10 on changed methods.

REFERENCE FILES:
- src/Encina.Messaging/Sagas/SagaOrchestrator.cs
- src/Encina.Messaging/Sagas/LowCeremony/ISagaRunner.cs
- src/Encina.Messaging/Sagas/LowCeremony/SagaDefinition.cs
- src/Encina.Messaging/Diagnostics/SagaActivitySource.cs
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- docs/plans/dsr-implementation-plan-404.md (style)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: the machine reads nothing, and caching the state of a running saga would serve stale state to a writer. The issue listed it as deferred; this plan reclassifies it with that reason |
| 2 | OpenTelemetry | ✅ | Phase 7: source and meter `Encina.StateMachine`, span `encina.statemachine.fire`, counter `encina.statemachine.transition_count` |
| 3 | Structured Logging | ✅ | Phase 7: `[LoggerMessage]` in ranges 5850-5874 and 5875-5899, registered in Phase 1 |
| 4 | Health Checks | ❌ | Not applicable: the machine has no checkable dependency; saga store health is covered by the existing `SagaHealthCheck` (`src/Encina.Messaging/Health/SagaHealthCheck.cs`) |
| 5 | Validation | ❌ | Not applicable: triggers and states are enums, so there is no free input; the definition is validated once in `Build()` |
| 6 | Resilience | ❌ | Not applicable: entry and exit actions are the caller's code and may use Polly themselves; the machine makes no external call |
| 7 | Distributed Locks | ⏭️ | Deferred: concurrent triggers on one saga row need a lock or a version check, related to #2213; issue file `artifacts/issues/state-machine-saga-concurrent-triggers.md` |
| 8 | Transactions | ✅ | Phase 6: state and data are persisted by one `UpdateAsync`; atomic compensation is #720 |
| 9 | Idempotency | ⏭️ | Deferred with item 7: a deduplication key for a trigger delivered twice, same issue file |
| 10 | Multi-Tenancy | ⏭️ | Deferred: `TenantId` on the saga row is #738; the coordinator inherits it when #738 lands |
| 11 | Module Isolation | ⏭️ | Deferred: `ModuleId` on the saga row is #747 |
| 12 | Audit Trail | ⏭️ | Deferred: audit of saga transitions is #750; `TransitionInfo` carries the from, to and trigger it needs |

---

## Prerequisites & Dependencies

- No blocking issue. The saga code (`SagaOrchestrator`, `ISagaStore`) is stable and the plan changes none of it.
- Advisable before the coordinator is used by several workers: #2213 (concurrency control on the saga row) and the deferred issue for concurrent triggers.
- Coordinate with #1237 (saga correlation by external key): it decides how an incoming event finds its saga and then calls `FireAsync(definition, sagaId, trigger)`.

---

## Next Steps

1. The maintainer answers the Dilemmas D1 to D5 when work on #297 starts; the answers go into `## Maintainer Decisions` at the end of this plan, and the markers "(recommended, pending the maintainer)" are removed.
2. Open the deferred issue (`artifacts/issues/state-machine-saga-concurrent-triggers.md`) and link it from #297.
3. Brief pull request 1 (Phase 1) first; pull requests 2 to 6 follow in order.
