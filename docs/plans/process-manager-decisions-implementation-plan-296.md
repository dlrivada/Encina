# Implementation Plan: Decision-Driven Process Manager Definitions (`ProcessManagerDefinition<TData>`)

> **Issue**: [#296](https://github.com/dlrivada/Encina/issues/296)
> **Type**: Feature
> **Complexity**: Medium (7 phases on top of the #392 core, no new package, about 39 files in 5 pull requests)
> **Estimated Scope**: ~700 lines of production code + ~1,600 lines of tests
> **Family**: this plan is the decision layer on the Process Manager core owned by #392 (package `Encina.ProcessManager`, `ProcessId`, `ProcessStep` audit record, `ProcessManagerBase<TState>`, `IProcessManagerStore`, routing handler); #317 adds commands and the Marten store. Siblings are referred to by issue number only.

---

## Summary

#296 describes a Process Manager that **decides the next step at run time** from the process data and its history (the EIP definition), as opposed to a saga whose steps are a fixed sequence. After the #392 core exists, what #296 adds is exactly the decision loop: an author writes `DecideAsync(data, history)`, the engine executes the chosen action, records it and asks again until the definition says wait, complete or fail.

**What exists today** (`origin/main` at `6e73e575d`, 2026-11-24):

- No process manager code (`git grep -il ProcessManager origin/main -- src` is empty). The fixed-sequence alternative is `SagaDefinition<TData>`/`BuiltSagaDefinition<TData>` and `SagaRunner` (`src/Encina.Messaging/Sagas/LowCeremony/`), which runs steps in order and compensates in reverse. `src/Encina.Messaging/RoutingSlip/` carries predefined dynamic routes.
- Commands are sent with `IEncina.Send<TResponse>(IRequest<TResponse>, CancellationToken)` (`src/Encina/Abstractions/IEncina.cs`).

**Corrections to the issue text** (the sketch cannot be built as written):

1. The issue's `IProcessManager<TData>` (`ProcessId`, `Data`, `History`, `EvaluateNextStepAsync`, `ExecuteStepAsync`) would be a second manager contract beside the `ProcessManagerBase<TState>` of #392; here the decision layer is a subclass of `ProcessManagerBase<TData>`, so there is one manager type in the package (Design Choice 1).
2. `ProcessStep` (what to execute) collides with the #392 `ProcessStep` (an audit record of what happened); the executable unit is named `ProcessAction<TData>`, and `CompletedStep` is the #392 `ProcessStep`.
3. `Step<THandler>() where THandler : IRequestHandler<...>` is incomplete (the type arguments are elided) and ties a step to a handler type; an action is built from a request factory (`Send(name, data => command)`), which is what `IEncina.Send` needs.
4. `ProcessDecision.Reason` is free text; it would reach logs and the step table. It becomes a validated `ReasonCode` (Design Choice 6).
5. `Task<ProcessDecision>` and `Task<Either<...>>` signatures mix styles; decisions return `ValueTask<Either<EncinaError, ProcessDecision>>` so a failing decision is a `Left`.
6. `ProcessDecisionType` has six values; `Branch` and `Compensate` are open Dilemmas (Design Choices 2 and 3), not silently implemented.

**Standards**: Enterprise Integration Patterns, Process Manager (the issue's reference). No law applies.

**Affected packages**: `Encina.ProcessManager` only (the issue lists `Encina` core and `Encina.Messaging`; neither changes: the saga integration is composition, Design Choice 7).

**Provider category**: none. The layer is in-memory logic over the #392 store contract, so the 10-provider matrix does not apply to this plan.

**Shared pieces and who owns them**: #392 owns the contract, `ProcessStep`, the error-code class and the EventId range `ProcessManager` (12270-12289). This plan adds to `ProcessManagerErrorCodes` and `ProcessManagerLog` by addition and uses 12284-12289 of that range; #317 uses 12278-12283 and #392 12270-12277, so the three claims are contiguous and disjoint. This plan takes the last id of the `ProcessManager` field (12270-12289), which is then full: the next consumer of the family needs a new field in the spare 12290-12299 of the #392 table.

---

## Design Choices

<details>
<summary><strong>1. Where the decision layer sits (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ProcessManagerDefinition<TData> : ProcessManagerBase<TData>` and the engine inside `Encina.ProcessManager`; the routing handler asks the engine after each event when the manager is decision-driven** | One manager type, one store, one router; a manager can mix event handlers and a decision loop | The core package carries the engine even for applications that never decide |
| **B) A separate package `Encina.ProcessManager.Definitions` over the core** | The core stays minimal | One more package for about 400 lines; the routing handler needs an extension point it does not have |
| **C) The issue's standalone `IProcessManager<TData>` with its own store and history** | Matches the sketch | Two incompatible managers and two stores in one family |

### Chosen Option: **A — A subclass and an engine inside the core package** (recommended, pending the maintainer)

### Rationale

- A wins when decisions are one authoring style of the same pattern, as #296 itself says ("Alternative" to #317). B wins when the maintainer wants the core as small as possible; C only when #296 ships without #392.
- The engine is about 150 lines and loads nothing unless a decision-driven manager is registered (pay-for-what-you-use through registration, not through a package).
- **Class:** Dilemma

</details>

<details>
<summary><strong>2. What `Branch` means (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No `Branch`: the definition chooses a path by returning a different `Continue(action)`; the enum has `Continue`, `Wait`, `Complete`, `Fail`** | The decision function already is the branch; nothing unspecified ships | The issue's `Branch(params ProcessStep[] options)` is dropped |
| **B) `Branch` is a parallel fork: all options run, the engine joins before the next decision** | Real fan-out | Needs per-branch results, failure policy (all or any), and concurrency control on one process row |
| **C) `Branch` is an ordered list run one after the other** | Easy | Identical to several `Continue` decisions; adds nothing |

### Chosen Option: **A — No `Branch`** (recommended, pending the maintainer)

### Rationale

- The issue says only "Multiple possible paths", which describes the choice a definition makes, not a mechanism. A wins until a fan-out use case exists; B wins when independent actions (payment and inventory) must run at the same time and the maintainer accepts join semantics, which would be a follow-up with its own plan.
- **Class:** Dilemma

</details>

<details>
<summary><strong>3. What `Compensate` means (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No `Compensate` decision: rollback is an ordinary `Continue(refund action)` chosen from the history; transactional rollback is a saga run as one action (Design Choice 7)** | No second compensation engine beside `SagaRunner`; #720 is changing saga compensation and this does not collide | The issue's `Compensate` ("start rollback") is not a decision type |
| **B) `Compensate` runs the optional compensation of every completed action in reverse order** | The saga-like experience | Duplicates `SagaRunner.CompensateAsync`, whose retry and failure rules #720 rewrites; two behaviors to keep equal |

### Chosen Option: **A — Compensation is an ordinary action** (recommended, pending the maintainer)

### Rationale

- A wins because the table in the issue itself says a saga has "Linear compensation" and a process manager "Multiple routes"; rollback by routes is a decision. B wins when the same process needs automatic reverse-order rollback without a saga.
- **Class:** Dilemma

</details>

<details>
<summary><strong>4. What an action failure does (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Record the failed action as a step (outcome `action_failed`, error code), then ask `DecideAsync` again; after `MaxActionAttempts` consecutive failures of the same action (default 3) the process becomes `Failed` and the engine returns `processmanager.action_attempts_exceeded` so the publisher dead-letters** | The definition owns retry, alternative routes and giving up; the failure is visible in the history and never reported as success | The engine tracks attempts; the definition must read the history |
| **B) Return the `Left` of the action to the publisher immediately** | Simplest; the publisher's retry policy applies | The definition is never consulted, so no alternative route; a permanent failure retries forever |

### Chosen Option: **A — Record, decide again, then fail visibly** (recommended, pending the maintainer)

### Rationale

- A wins because dynamic decisions are the point of the pattern (#296: "Supports runtime decisions based on message content and history"); the bounded attempts and the terminal `Left` keep `AGENTS.md` section 3 ("errors are never swallowed in background infrastructure"). B wins when every failure must go straight to operators.
- **Class:** Dilemma

</details>

<details>
<summary><strong>5. `Wait` is a recorded marker, not a persisted subscription (Trivial)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Wait(eventTypeName)` records a step (event type, outcome `waiting`) and stops the loop; the next event reaches the manager through the #392 router and `DecideAsync` runs again** | No new storage; the dashboard and stuck detection see the waiting step | The definition checks the history for the awaited event |
| **B) A subscription table that wakes a process when a specific event arrives** | Explicit wake-up | A new table on ten providers; the #392 router already routes every correlated event |

### Chosen Option: **A — A recorded marker**

### Rationale

Every correlated event already reaches the process through the router, so waiting needs no subscription; the marker gives the dashboard and `ListStuckAsync` something to show. A timeout while waiting is a scheduling concern (deferred issue, matrix item 6).

- **Class:** Trivial
- **Settled by:** issue #296 (acceptance: "External event waiting works" with "Wait: Wait for external event") and the #392 routing design (every correlated event is delivered to the manager).

</details>

<details>
<summary><strong>6. A reason is a code, not text (Trivial)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ReasonCode` validated as lower-case dotted text (`^[a-z0-9._-]{1,64}$`), stored on the step** | No personal data or error message can reach storage or logs | A short code instead of a sentence |
| **B) The issue's free-text `Reason`** | Readable | Free text reaches the step table, logs and traces |

### Chosen Option: **A — A validated code**

### Rationale

The step table is an audit trail; free text is how an `EncinaError.Message` or a name ends up in plaintext storage.

- **Class:** Trivial
- **Settled by:** `AGENTS.md` section 3 (`EncinaError.Message` never reaches logs, activity tags, health-check results or plaintext storage; error codes only).

</details>

<details>
<summary><strong>7. Saga integration by composition (Trivial)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An action sends a command whose handler runs a saga through `ISagaRunner`; no code ties the packages** | `Encina.ProcessManager` does not reference `Encina.Messaging`; the issue's "Integration with Saga pattern" is a tested recipe | The recipe lives in the guide and one integration test |
| **B) A `SagaAction` type in `Encina.ProcessManager` calling `ISagaRunner` directly** | One-liner for authors | The core package references `Encina.Messaging` and its stores |

### Chosen Option: **A — Composition through a command**

### Rationale

`ISagaRunner.RunAsync` already takes a built definition and returns `Either`; any handler can call it.

- **Class:** Trivial
- **Settled by:** `AGENTS.md` section 1 (every feature opt-in; pay-for-what-you-use) and `src/Encina.Messaging/Sagas/LowCeremony/ISagaRunner.cs`.

</details>

<details>
<summary><strong>8. Redelivery of an event that was fully handled (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The handler saves the event's own step (ordinal 0) before the engine runs, and the engine always starts from the STORED snapshot (the handler reloads it with `GetAsync` after the save). The store returns `Right(Unit)` for a duplicate with no flag (#392), so the handler never tries to tell a duplicate from a stored step: a redelivery re-applies the event handler to a throw-away in-memory copy, the ordinal-0 save is a silent no-op, and the engine resumes from the persisted state, which already holds every action saved before the crash** | A redelivery after a crash between actions resumes at the first unsaved action; a fully handled redelivery computes `Wait` or `Complete` again and sends no action (the definition is a deterministic function of data and history, a documented rule); no change to the #392 store contract | One extra `GetAsync` per event; a redelivery runs `DecideAsync` once more even when nothing is left to do |
| **B) The handler saves the event's own step last, after the engine** | A crash anywhere resumes through the same event's redelivery with no reload | A fully handled redelivery re-runs decisions from the pre-event state and re-sends actions until its last save is reached; the actions must be idempotent in every case |

### Chosen Option: **A — Event step first, engine resumes from stored state** (recommended, pending the maintainer)

### Rationale

- A wins because a crash resumes through redelivery (as B) and a fully handled event sends nothing again (unlike B), with no contract change. The "already handled" check does not live in the handler: it is the engine's own state-driven decision plus the derived idempotency keys of Phase 2 task 4. B wins when a redelivery must never read the stored state again. Under both options `Dispatch` commands of #317 are sent before the ordinal-0 save and are re-sent on any redelivery (their handlers must be idempotent, #317 Design Choice 2). The choice sets the handler order of the #392/#317 routing handler, so the maintainer's answer is applied to #392 pull request 4 and #317 pull request 1 as well.
- **Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Core Models & Abstractions

> **Goal**: The decision contract (additions to `Encina.ProcessManager`).

<details>
<summary>Tasks</summary>

1. **`ProcessDecisionType`**: `Continue = 0, Wait = 1, Complete = 2, Fail = 3` (Design Choices 2 and 3; additive if the maintainer later answers B).
2. **`ProcessDecision`**: sealed record `(ProcessDecisionType Type, ProcessAction<TData>? Next, string? EventTypeName, string ReasonCode)` as a generic `ProcessDecision<TData>`; factory methods `Continue(action, reasonCode)`, `WaitFor<TEvent>(reasonCode)`, `Complete(reasonCode)`, `Fail(reasonCode)`; a `Continue` with no action or a `ReasonCode` outside `^[a-z0-9._-]{1,64}$` returns `processmanager.decision_invalid`.
3. **`ProcessAction<TData>`**: sealed class `(string Name, Func<TData, CancellationToken, ...> Execute)` built by `ProcessActionBuilder`: `Send<TResponse>(string name, Func<TData, IRequest<TResponse>> command, Func<TData, TResponse, TData>? apply = null)`; `ExecuteAsync(IEncina, TData, CancellationToken)` returns `ValueTask<Either<EncinaError, TData>>` and is the only place that calls `IEncina.Send`.
4. **`ProcessManagerDefinition<TData> : ProcessManagerBase<TData>`** (`where TData : class, new()`): `protected abstract ValueTask<Either<EncinaError, ProcessDecision<TData>>> DecideAsync(TData data, IReadOnlyList<ProcessStep> history, CancellationToken)` and helpers `Action(name)`, `Continue`, `WaitFor<TEvent>`, `Complete`, `Fail`.
5. **Error codes** (added to `ProcessManagerErrorCodes`, ADR-062 categories): `processmanager.decision_invalid` = `Internal` (a programmer error in a definition); `processmanager.decision_loop_exceeded` = `Internal`; `processmanager.action_attempts_exceeded` = `UpstreamFailure`. Messages carry the process type and action name only.
6. **`PublicAPI.Unshipped.txt`** lines and the manifest entries of the new files.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```
CONTEXT:
- Issue #296; plan docs/plans/process-manager-decisions-implementation-plan-296.md. The #392 core exists. Dilemmas D1-D5 are recommended, pending the maintainer: read the Maintainer Decisions section if it exists and follow the answers.

TASK:
Add the decision contract of Phase 1 Tasks with unit and guard tests.

KEY RULES:
- The executable unit is ProcessAction<TData>; ProcessStep stays the #392 audit record.
- ReasonCode is validated; no free text anywhere in a decision.
- Every new error code has an ADR-062 ErrorCategory set at creation; messages never embed EncinaError.Message.
- Sealed records, XML docs, nullable enabled, PublicAPI.Unshipped.txt updated.

REFERENCE FILES:
- src/Encina/Abstractions/IEncina.cs
- src/Encina.Messaging/Sagas/LowCeremony/SagaDefinition.cs (builder style)
- src/Encina.ProcessManager/ProcessManagerErrors.cs (from #392; it holds the error codes and their factory methods)
```

</details>

---

### Phase 2: The Decision Engine

> **Goal**: The loop that runs actions and asks again.

<details>
<summary>Tasks</summary>

1. **`ProcessDecisionEngine`** (internal, scoped; takes `IEncina`, `IProcessManagerStore`, `ProcessDecisionOptions`, `TimeProvider`, a logger): `AdvanceAsync(manager, snapshot, CancellationToken)`: loop { load history (`GetStepsAsync`); `DecideAsync`; `Continue` runs `ExecuteAsync`, applies the result to the data, appends a step; `Wait` records a `waiting` step and stops; `Complete` calls `Complete()`; `Fail` calls `Fail(reasonCode)` }.
2. **Bounds**: at most `MaxStepsPerAdvance` actions per call (default 32); beyond it the engine stops, saves what was done and returns `processmanager.decision_loop_exceeded`. After `MaxActionAttempts` consecutive failures of the same action name (default 3) the engine sets the process `Failed` and returns `processmanager.action_attempts_exceeded` (Design Choice 4).
3. **Failed action**: a `Left` from `ExecuteAsync` is recorded as a step with outcome `action_failed` and the error code (never the message), the state is saved, and `DecideAsync` is asked again.
4. **Hook and order**: for a `ProcessManagerDefinition<TData>` the routing handler of #392/#317 runs, in this order: the manager's event handler; the pending `Dispatch` commands of #317 (sent first); the save of the event's own step (ordinal 0, by the handler, as in #392/#317); a reload of the stored snapshot by the handler (`GetAsync`; this plan's addition to the hook, needed because `SaveAsync` returns `Right(Unit)` for a duplicate with no flag, #392); the engine; there is no `IsFinished` check, because a definition ends through `Decision.Complete` or `Decision.Fail`, which the engine saves itself. Every other manager keeps the #317 order (dispatch, `IsFinished`, save) unchanged, so each manager kind has one editor of the handler's method in one order.
   **Crash and redelivery (Dilemma D5-A)**: the engine always works on the STORED snapshot and its history, never on the in-memory copy the event handler mutated. A redelivery therefore re-applies the event handler to a copy that is discarded, finds its ordinal-0 save a silent no-op, reloads the stored state and lets `DecideAsync` choose the next action from it: after a crash that saved actions 1 and 2 of 4, the redelivery decides action 3 and then 4, and never repeats 1 or 2, because the decision function sees them in the data and history. Nothing in the handler or the engine needs to know whether a save was a duplicate. A redelivery of a fully handled event decides `Wait` or `Complete` again (the `Wait` step has the fixed key `"{eventKey}/wait"`, so the second save of it is dropped as a duplicate: the history does not grow and the stuck-process timestamp is not refreshed; a terminal process is left alone) and sends no action; this relies on the rule that a definition is a deterministic function of data and history, stated in the XML docs and the guide.
   **Idempotency keys of engine steps**: the engine saves each step with `ordinal = 0`. An action step takes the derived key `"{eventKey}/a{position}"`, where `position` is the process-wide step count at that save (the history length loaded for `DecideAsync` plus one); `position` only grows, so a key is never reused by a later action. A `Wait` step does NOT use `position`: its key is the fixed `"{eventKey}/wait"` (an advance ends at its first `Wait`, so an event has at most one), independent of the history length, which is what makes a redelivered `Wait` a duplicate instead of a new step. The unique `(process, key, ordinal)` guard of #392 drops only a retried identical save of the same step (for example a lost response); concurrent writers are stopped by `expectedVersion` (`processmanager.concurrency_conflict`). A null event key gives a null derived key (no guard, as #392). This stays inside the #392 contract (any recorded `(key, ordinal)` pair of a process is a no-op, any other is persisted), so #392 needs no change; the `Ordinal` of #392 stays "the position within one event" and the engine does not use values above 0.
5. **`ProcessDecisionOptions`** (`MaxStepsPerAdvance`, `MaxActionAttempts`) validated at startup; registration `AddProcessManagerDefinition<TDefinition, TData>()` with `TryAdd*`.
6. Every `Left` of the store, the engine or an action is returned or recorded, never swallowed; saving uses `SaveAsync(snapshot, expectedVersion, step, idempotencyKey)` of #392, so a concurrent writer gets `processmanager.concurrency_conflict`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```
CONTEXT:
- Issue #296, Phase 2. Phase 1 types exist; IProcessManagerStore has GetStepsAsync and SaveAsync(snapshot, expectedVersion, step, idempotencyKey).

TASK:
Implement the engine, its options, the registration and the hook, with unit tests (in-memory store) and guard tests.

KEY RULES:
- The loop is bounded; both bounds return a Left with an ADR-062 category.
- The handler reloads the stored snapshot after the ordinal-0 save and the engine decides from it; engine steps use ordinal 0, action steps the derived key "{eventKey}/a{position}" and a `Wait` step the fixed key "{eventKey}/wait" (task 4); SaveAsync gives no duplicate flag, so no code tests for one.
- A failed action is recorded with its error code only, then the definition decides again.
- No scoped service is injected into a singleton; the engine is scoped and created per event.
- TimeProvider for timestamps; tests use FakeTimeProvider, no Thread.Sleep.

REFERENCE FILES:
- src/Encina.Messaging/Sagas/LowCeremony/SagaRunner.cs
- src/Encina.ProcessManager/ProcessManagerRoutingHandler.cs (from #392 and #317)
```

</details>

---

### Phase 3: Configuration & DI

> **Goal**: Registration that the DI proof can validate.

<details>
<summary>Tasks</summary>

1. `AddProcessManagerDefinition<TDefinition, TData>()` registers the definition as a manager (as `AddProcessManager<TManager, TState>()` of #392) and the engine once with `TryAddScoped`.
2. `ProcessDecisionOptions` through `AddOptions<T>().ValidateOnStart()`.
3. DI test: the provider builds with `ValidateOnBuild` and `ValidateScopes`; the engine resolves `IEncina` and the store from the event's scope.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```
CONTEXT:
- Issue #296, Phase 3.

TASK:
Add the registration extension and the DI proof test.

KEY RULES:
- Every option type the registration resolves is registered and validated.
- The proof test builds the provider with ValidateOnBuild and ValidateScopes.

REFERENCE FILES:
- src/Encina.ProcessManager/ServiceCollectionExtensions.cs (from #392)
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs
```

</details>

---

### Phase 4: Cross-Cutting Integration

> **Goal**: Apply the matrix items marked integrate.

<details>
<summary>Tasks</summary>

1. **Validation**: decisions and reason codes are validated (Phase 1); options validated at startup.
2. **Transactions**: each action is followed by a `SaveAsync` of the new data and its step, so a crash between two actions resumes from the last saved step; an action is sent before its save (at-least-once, the action's command handler must be idempotent), the same order as #317 Design Choice 2.
3. **Idempotency**: the event's own step is guarded by `(process, eventKey, 0)` of #392; each engine action step by its derived key `"{eventKey}/a{position}"` and a `Wait` step by `"{eventKey}/wait"` (Phase 2 task 4). Neither stops a redelivery from resuming: the engine decides from the stored state. An action is guarded by the command handler's own inbox (documented), and the `Dispatch` commands of #317 are re-sent on a redelivery.
4. **Audit trail**: the history is the step list of #392; every decision appends a step.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```
CONTEXT:
- Issue #296, Phase 4.

TASK:
Add the crash-resume test (a definition of four actions, the store fails after the second save, the redelivery of the same event with the same idempotency key completes actions 3 and 4, and the history holds exactly four action steps, none duplicated), the fully-handled-redelivery test (no action is sent again), and the redelivered-`Wait` test (an event that ended in `Wait` is redelivered: no new step is persisted, the history length is unchanged and the stuck-process timestamp, read through the `FakeTimeProvider`, is unchanged).

KEY RULES:
- Actions are at-least-once; the test proves a crash after an action re-runs only the unsaved ones (D5-A: the engine resumes from the stored snapshot).
- Do not assert that the ordinal-0 save is "a duplicate" anywhere: the store gives no such signal.
- No EncinaError.Message in any step, log or tag.

REFERENCE FILES:
- src/Encina.Messaging/Inbox/ (idempotency precedent)
```

</details>

---

### Phase 5: Observability

> **Goal**: Logs, a counter and a span inside the family range.

<details>
<summary>Tasks</summary>

1. **EventIds** inside the range `ProcessManager` (12270-12289) registered by #392: 12284 decision made, 12285 action executed, 12286 action failed, 12287 action attempts exceeded, 12288 decision loop exceeded, 12289 process failed by decision; packed, no gaps. Added to `ProcessManagerLog` of #392.
2. **Metrics**: `Counter<long> encina.process_manager.decisions` (on the `Encina.ProcessManager` meter and `ProcessManagerLog` landed by #392 pull request 2; any duration histogram uses unit seconds like #392's) (tags process type, decision type) and `Counter<long> encina.process_manager.action_failures` (tags process type, error code); no `_total` suffix; the tenant tag only when multi-tenancy is on (ADR-054 D4); never the correlation key, the process id or the reason text.
3. **Span**: `encina.process_manager.decide` per `DecideAsync` with the decision type.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```
CONTEXT:
- Issue #296, Phase 5. The range ProcessManager 12270-12289 is registered by #392; this plan owns 12284-12289.

TASK:
Add the six log messages, the two counters and the span, and unit tests with a listener.

KEY RULES:
- No id outside 12284-12289; run EncinaEventIdAllocationTests and EventIdRegistryAgreementTests.
- Tags are bounded; instruments have no _total suffix.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
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
| Unit | engine loop, bounds, failed action, wait, complete, fail, validation of decisions | `ProcessDecisionEngine.cs` 90, `ProcessDecision.cs` 95, `ProcessAction.cs` 90 (pure logic against the in-memory store) |
| Guard | public constructors, factories and the extension | `ProcessManagerDefinition.cs` 85, `ServiceCollectionExtensions.cs` 90 (null and empty arguments are the only branches) |
| Contract | none new (the store contract is #392's) | justification file |
| Unit (crash and redelivery) | one event with four actions saves four steps with distinct derived keys and all four persist; a crash after action 2 (the store fails the third save) and a redelivery of the same event: actions 3 and 4 are completed, 1 and 2 are not repeated, exactly four action steps exist; a redelivery of a fully handled event sends no action and adds no step | `ProcessDecisionEngine.cs` (same file target as above, in the engine unit test file) |
| Property | a bounded engine always terminates: for any decision sequence the number of actions never exceeds `MaxStepsPerAdvance` (FsCheck) | `ProcessDecisionEngine.cs` 70 (one invariant over generated sequences) |
| Integration | the saga recipe: an action whose handler runs `ISagaRunner` on a real database collection, plus a durable store crash-resume | `ProcessDecisionEngine.cs` 60 (the real paths across a real store) |

Load: a justification `.md` (one event drives at most `MaxStepsPerAdvance` actions; concurrency is the store version check). Benchmark: a justification `.md` (the loop is dominated by command handlers).

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```
CONTEXT:
- Issue #296, Phase 6.

TASK:
Write the tests of the table and the three justification files (contract, load, benchmark).

KEY RULES:
- Tests execute real code; Shouldly through Encina.Testing.Shouldly; FakeTimeProvider; no Thread.Sleep.
- Integration tests use the shared [Collection] fixtures and ClearAllDataAsync.

REFERENCE FILES:
- tests/Encina.UnitTests/Messaging/Sagas/SagaRunnerTests.cs
- .github/coverage-manifest/Encina.Messaging.json
```

</details>

---

### Phase 7: Documentation & Finalization

> **Goal**: The mandatory documentation set.

<details>
<summary>Tasks</summary>

1. XML docs on every public member; `changelog.d/296-process-manager-decisions.added.md`.
2. A section in the #392 feature guide: decisions versus sagas, the saga recipe, the failure and bounds rules; the package README; `docs/INVENTORY.md`; `ROADMAP.md`.
3. `dotnet build --configuration Release` with zero warnings; per-flag coverage measured against the manifest.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```
CONTEXT:
- Issue #296, Phase 7. Delegated to the docs-writer agent for pages under docs/ and READMEs.

TASK:
Write the guide section, the README additions, the changelog fragment and the INVENTORY and ROADMAP lines.

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
| 1 | Phase 1: `ProcessDecisionType`, `ProcessDecision`, `ProcessAction`, `ProcessActionBuilder`, `ProcessManagerDefinition` (5), `ProcessManagerErrors.cs` (1 modified file, #392 task 7), PublicAPI, manifest, 2 unit tests, 1 guard test | 11 | 900 |
| 2 | Phases 2-3: engine, options, registration (3), routing-handler hook (modified), PublicAPI, manifest, 3 unit tests (engine loop, failure and bounds, hook), DI proof, guard | 11 | 1,200 |
| 3 | Phase 5: log messages and counters (modified `ProcessManagerLog`, new metrics file), PublicAPI, 2 unit tests | 5 | 400 |
| 4 | Phases 4 and 6: crash-resume test, property test (`PropertyTests` project reference exists after #392), saga-recipe integration test, ordinal integration test (several steps of one event on a durable store), 3 justification files | 7 | 800 |
| 5 | Phase 7 documentation: guide section, README, INVENTORY, ROADMAP, changelog fragment | 5 | 350 |

Total: 5 pull requests, 39 files (upper estimate); no pull request exceeds 15 files or 1,500 lines. Pull request 1 needs #392 pull request 1 (contract with `ProcessStep.Ordinal`) and edits no `ProcessManagerLog`; pull request 2 edits the routing handler and so needs #392 pull request 4 and #317 pull request 1 (its dispatch step); pull request 3 needs #392 pull request 2 (the `ProcessManager` range registration, `ProcessManagerLog` and the meter) and #296 pull request 2 (the engine the counters and logs instrument); pull request 4 needs a durable store: #317 pull request 2 (Marten) or one of the #392 provider pull requests 6 to 13 (the in-memory store of #392 pull request 3 is enough only for its non-integration tests).

---

## Research

### Relevant Standards & Specifications

| Reference | What it defines | Use in this feature |
|-----------|-----------------|---------------------|
| Enterprise Integration Patterns, Process Manager | A central unit that decides the next step from message content and state | The decision loop |
| Saga versus Process Manager (the issue's table) | Predefined steps and linear compensation versus dynamic decisions and multiple routes | Positioning against `SagaRunner` |
| NServiceBus and Rebus sagas (the issue's motivation) | Event-driven process state | Named comparables |
| ADR-001, ADR-006 | Railway Oriented Programming | A decision and an action return `Either` |
| ADR-018, ADR-021, ADR-062 | Cross-cutting check, EventIds, error categories | Matrix, range slice, codes |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IEncina.Send<TResponse>` | `src/Encina/Abstractions/IEncina.cs` | The only way an action sends its command |
| `SagaDefinition<TData>`, `SagaStepBuilder<TData>` | `src/Encina.Messaging/Sagas/LowCeremony/` | Builder and immutable-definition style |
| `ISagaRunner` | `src/Encina.Messaging/Sagas/LowCeremony/ISagaRunner.cs` | The saga recipe: an action's handler runs a saga |
| `RoutingSlip` | `src/Encina.Messaging/RoutingSlip/` | Predefined dynamic routes; the contrast the issue draws |
| `ProcessManagerBase`, `IProcessManagerStore`, routing handler, `ProcessStep` | `src/Encina.ProcessManager/` (after #392) | The shared core this plan extends |
| `EncinaErrors.Create`, `ErrorCategory` | `src/Encina/Errors/ErrorCategory.cs` | Error codes with a protocol-neutral category (ADR-062) |
| `EventIdRanges` | `src/Encina/Diagnostics/EventIdRanges.cs` | Registry; the range is registered by #392 |
| `TimeProvider`, `FakeTimeProvider` | BCL, `Microsoft.Extensions.TimeProvider.Testing` | Injected clock and test clock |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.ProcessManager` (#392) | 12270-12277 | Registered by #392 as `ProcessManager = (12270, 12289)` |
| `Encina.ProcessManager.Marten` (#317) | 12278-12283 | Commands and the Marten store |
| **`Encina.ProcessManager` (this plan)** | **12284-12289** | **Decision made, action executed, action failed, attempts exceeded, loop exceeded, process failed by decision; no new field** |

The block 12250-12299 is the first free 50-id slot of the Core and messaging area after the #4007 reservations and the #340 claim; no range is registered by this plan.

### Provider Matrix

| Provider category | Applies | Reason |
|-------------------|---------|--------|
| Database (10) | No | The layer uses the #392 store contract; the ten stores are #392's Phase 4 |
| Event sourcing (Marten) | No | #317 |
| Caching, Transport, Locks, Validation, Cloud | No | None |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Source (new or modified) | ~13 | Decision types, action builder, definition, engine, options, registration, log, metrics |
| PublicAPI and manifests | ~5 | Unshipped lines and manifest entries per pull request |
| Tests | ~16 | Unit, guard, property, DI, crash-resume, saga recipe, justification files |
| Documentation | ~5 | Guide section, README, INVENTORY, ROADMAP, changelog fragment |
| **Total** | **~39 in 5 pull requests** | Equals the sum of the split table |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Railway Oriented Programming (Either<EncinaError, T>); no [Obsolete]; XML docs on every public API; zero warnings.
- Issue #296: decision-driven process managers on the #392 core. Plan: docs/plans/process-manager-decisions-implementation-plan-296.md. Dilemmas D1-D5 are recommended, pending the maintainer; read the Maintainer Decisions section first if it exists.

IMPLEMENTATION OVERVIEW:
1. Phase 1: ProcessDecision, ProcessAction, ProcessActionBuilder, ProcessManagerDefinition<TData>, three error codes.
2. Phase 2: the bounded decision engine and the routing-handler hook. Phase 3: registration and DI proof.
3. Phase 4: crash-resume and idempotency. Phase 5: EventIds 12284-12289, counters, span.
4. Phase 6: tests. Phase 7: documentation.

KEY PATTERNS:
- The executable unit is ProcessAction; ProcessStep is the #392 audit record.
- A failed action is recorded with its error code, the definition decides again, bounded attempts end in a visible Failed process and a Left.
- ReasonCode is a validated code; no free text, EncinaError.Message, correlation key or process id in logs, tags, spans or storage.
- Actions are at-least-once; sent before the save; their handlers are idempotent.
- TryAdd* registrations; a DI test with ValidateOnBuild and ValidateScopes; no scoped service in a singleton.

REFERENCE FILES:
- src/Encina/Abstractions/IEncina.cs
- src/Encina.Messaging/Sagas/LowCeremony/SagaRunner.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- docs/plans/dsr-implementation-plan-404.md (style)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: a decision reads the data and history of the event being processed; a cached read would serve stale state to a writer (the issue defers it) |
| 2 | OpenTelemetry | ✅ | Phase 5: counters `decisions` and `action_failures`, span `encina.process_manager.decide`; tenant tag only when multi-tenancy is on (ADR-054 D4) |
| 3 | Structured Logging | ✅ | Phase 5: ids 12284-12289 of the registered range |
| 4 | Health Checks | ⏭️ | Deferred to #392 Phase 5 (stuck processes; a `waiting` step older than the threshold is stuck) |
| 5 | Validation | ✅ | Phase 1: decisions and reason codes validated; options validated at startup (the issue defers it) |
| 6 | Resilience | ⏭️ | Deferred: a retry delay between failed attempts and a timeout while waiting need the scheduler; the bounded attempts are integrated; issue file `artifacts/issues/process-manager-wait-timeout-and-retry-delay.md` |
| 7 | Distributed Locks | ⏭️ | Deferred with #392 (optimistic concurrency is integrated) |
| 8 | Transactions | ✅ | Phase 4: each action's data and step are saved by one `SaveAsync`; actions are sent before the save; no atomicity with the sent commands is claimed |
| 9 | Idempotency | ✅ | Phase 4: the event's idempotency key guards the event; action handlers must be idempotent (documented) |
| 10 | Multi-Tenancy | ✅ | Inherited from the #392 `ProcessId` and store; the engine takes no tenant of its own and fails closed through the store |
| 11 | Module Isolation | ⏭️ | Deferred: `ModuleId` on messaging entities is #747 |
| 12 | Audit Trail | ✅ | Every decision appends a step to the #392 history; the operation audit store is the deferred issue of #392 |

---

## Prerequisites & Dependencies

- **#392** (the core): this plan edits its routing handler, error codes and log class; merge order #392, #317, #296.
- #317 changes the same routing handler; this plan's hook follows its dispatch step (pull request 2 of this plan after #317 pull request 1).
- **Difference from #392 text**: the #392 plan still describes the #296 engine as appending steps with ordinals 1, 2, ...; this plan supersedes that: engine steps use ordinal 0 with derived keys (`"{eventKey}/a{position}"` for actions, `"{eventKey}/wait"` for `Wait`). Both stay valid under the #392 guard rule (any recorded `(key, ordinal)` pair is a no-op); #392 will be aligned by a later edit.
- #720 is changing saga compensation; this plan stays independent of it (Design Choice 3).

---

## Next Steps

1. The maintainer answers Dilemmas D1 to D5 when work on #296 starts; the answers go into `## Maintainer Decisions` at the end of this plan.
2. Open the deferred issue named in the matrix and link it from #296.
3. Brief pull request 1 after the #392 and #317 pull requests it needs.
