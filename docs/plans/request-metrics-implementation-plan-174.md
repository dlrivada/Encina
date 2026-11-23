# Implementation Plan: Real Request and Messaging Metrics (`EncinaMetrics`)

> **Issue**: [#174](https://github.com/dlrivada/Encina/issues/174)
> **Type**: Feature (observability core)
> **Related**: ADR-054 (settled point 4 instrument rules, D3 names, D4 tenant tag, D6 Encina-level metrics), #2678 (renames every existing instrument to the ADR-054 rules, so this plan renames nothing), #2677 (renames the 27 plain `"Encina"` meters), #2676 (the one tenant tag helper), #1794 (backlog triage that lists #174), #1937 (instruments declared and never emitted, the same defect in the data-subject-rights package), #187 (dashboards read the names this plan settles), #906 (observability guide), #184 and #185 (sibling plans of this family, not duplicated), ADR-062
> **Complexity**: Medium (the instruments mostly exist; the work is finding the one recorder per fact, wiring what is declared and never emitted, and keeping the gauge off the request path)
> **Estimated Scope**: ~360 lines of production code + ~950 lines of tests + 3 documentation files, in five code pull requests (M1-M5) and one documentation pull request (D1) of at most 15 files and 900 lines each

---

## Summary

### What exists today (read on 2026-11-20, main `0336d31c7`)

- The issue says there is "no metrics collected". That is out of date. `src/Encina/Diagnostics/IEncinaMetrics.cs` holds `IEncinaMetrics` and the sealed class `EncinaMetrics` (an instance registered by `AddEncina` with `TryAddSingleton`, `src/Encina/Core/ServiceCollectionExtensions.cs:61`, not the static class of the issue). It creates, on the meter `Encina`, `Encina.request.success` and `Encina.request.failure` (counters) and `Encina.request.duration` (histogram in `ms`), tagged `request.kind`, `request.name` and, for a failure, `failure.reason` (the error code).
- The dispatcher feeds them for every `Send`: `src/Encina/Dispatchers/Encina.RequestDispatcher.cs:53` resolves `IEncinaMetrics`, and lines 83, 95, 128, 132 and 159 call `TrackFailure` or `TrackSuccess`, with the error code as the reason (never the message).
- `CommandMetricsPipelineBehavior` and `QueryMetricsPipelineBehavior` (`src/Encina/Pipeline/Behaviors/`) call the same two methods again, with `IFunctionalFailureDetector`. A host that registers one of them counts each request twice, and the dispatcher counts a `Right` that carries a functional failure as a success while the behaviors count it as a failure.
- `src/Encina.Messaging/Diagnostics/MessagingStoreMetrics.cs` declares 8 counters, 1 histogram and 2 observable gauges (`encina.outbox.pending_count`, `encina.saga.active_count`). **No `Record*` method of it is called anywhere in `src`, and no code registers `MessagingStoreMetricsCallbacks`** (a search of `src` finds only the declaration and `MessagingStoreMetricsInitializer`, which builds the class with callbacks that DI never holds). The only test that registers callbacks does so by hand (`tests/Encina.UnitTests/OpenTelemetry/MessagingStores/MessagingStoreMetricsInitializerTests.cs:26`). So nothing of it reaches a listener today.
- Processor-level instruments do exist and are fed: `OutboxProcessorMetrics` (`encina.outbox.processor.messages_total`, `encina.outbox.messages_requeued_total`, `encina.outbox.enqueued`, `encina.outbox.messages_exhausted`, `encina.transaction.rollback_only`) and `SchedulingProcessorMetrics` (`encina.scheduling.processor.messages_total`, `encina.scheduling.processor.cycle_duration_seconds`). The issue's "outbox processed" and "scheduling processed" are these.
- There is no `request.active` instrument, no outbox retry distribution, no saga duration and no inbox duplicate signal that reaches a listener. `ISagaStore` and `IScheduledMessageStore` have no count query (`IOutboxStore.GetPendingCountAsync(int maxRetries, CancellationToken)` exists), so an active-saga or pending-schedule gauge cannot be read from a store today.
- `Encina.OpenTelemetry` subscribes with `metrics.AddMeter("Encina")` (`src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs:213`) and adds no request instrument of its own; the `MetricsCollectorPipelineBehavior` of the issue is not needed.

### What the issue proposes and what changed since

The issue (2026-era text) proposes a static `EncinaMetrics`, the instruments `encina.request.total`, `encina.request.errors`, `encina.request.active` and seven messaging instruments, and a `MetricsCollectorPipelineBehavior`. ADR-054 settled point 4 (2026-10-22) bans the `_total` suffix and fixes seconds with unit `s`, so `encina.request.total` cannot be created; #2678 owns renaming what exists. The meter naming is #2677.

### What this plan delivers

- One recorder per request fact: the dispatcher records success, failure and the new active count, and learns functional failures from `IFunctionalFailureDetector`; the two duplicate behaviors are removed (Design Choice 1, recommended).
- `encina.request.active` (an `UpDownCounter<long>`, unit `{request}`), the one request instrument of the issue that does not exist.
- The messaging instruments that are declared and never emitted become real: inbox duplicates, saga transitions and a saga duration histogram, scheduling scheduled and executed, an outbox retry histogram, and the outbox pending gauge fed by a cached refresher (never a database call inside the gauge callback).
- Shared pieces and owner: this plan owns `IEncinaMetrics` and `MessagingStoreMetrics` content; #2678 renames the instrument names inside them and #2677 their meter; #2676 owns the tenant tag helper that these instruments call when it lands; #187 reads the names.
- Affected packages: `Encina` (core), `Encina.Messaging`, `Encina.OpenTelemetry`. Provider category: observability; the messaging wiring sits above the stores (orchestrators), so it covers all 10 database providers without store code.
- Not delivered, with the reason: `encina.request.total` (ADR-054 settled point 4); `MetricsCollectorPipelineBehavior` (the dispatcher already sees every request); `encina.saga.active` and `encina.scheduling.pending` as gauges (no store count query exists on 10 providers: a follow-up issue file); the tenant and module attribute on these instruments (#2676, and a follow-up issue file for the module).

---

## Design Choices

<details>
<summary><strong>1. Which component records request metrics: the dispatcher or the pipeline behaviors</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep both: the dispatcher records every request and the two opt-in behaviors record again** | No removal | A host that registers a metrics behavior counts every request twice and disagrees with the dispatcher on functional failures; the XML example of the behavior names a type (`MeterEncinaMetrics`) that does not exist |
| **B) The dispatcher is the only recorder; it resolves `IFunctionalFailureDetector` and counts a functional failure as a failure; remove `CommandMetricsPipelineBehavior` and `QueryMetricsPipelineBehavior`** | One number per request; metrics exist for every request kind (command, query, plain request) without registering anything; no legacy | Removes two public types and their tests (pre-1.0, `AGENTS.md` section 1) |
| **C) The behaviors are the only recorder; remove the dispatcher calls** | Opt-in, as the issue's "Alignment with Encina Philosophy" says | A host that forgets to register them has no request metrics at all, and plain `IRequest` types have no behavior |

### Chosen Option: **B — The dispatcher is the single recorder** (recommended, pending the maintainer)

### Rationale

- B wins in the normal case: the instruments cost nothing without a listener (ADR-054 settled point 7), so "opt-in" is already given by the host subscribing to the meter. C wins for a host that wants a request kind excluded from metrics by not registering the behavior; the dispatcher can cover it later with an option, which no one has asked for.
- `AGENTS.md` section 1 rules out keeping a redundant public type for compatibility.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Counter shape: success and failure counters, or one counter with an outcome attribute</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep the two counters (`success` and `failure`); #2678 renames them** | No shape change in this plan; the error rate is `failure / (success + failure)`; the issue's `request.errors` is `failure` | Two series to add in a dashboard |
| **B) One counter `encina.request.count` with an `encina.request.outcome` attribute (`success` or `failure`)** | The OpenTelemetry idiom; one series family | A shape change on top of #2678's rename, and every consumer of the old names changes twice |

### Chosen Option: **A — Keep two counters** (recommended, pending the maintainer)

### Rationale

- A wins while #2678 renames the instruments anyway: changing the shape in a second step is churn for dashboards (#187). B wins if the maintainer wants one error-rate expression in alert rules; the change then belongs in #2678's pull request, not here.
- The issue's `encina.request.total` is not created in either option: it breaks ADR-054 settled point 4 (no `_total`); the Prometheus exporter appends `_total` to a counter at export time, which is a property of the exposed series, not of the instrument name.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. How the active-request count is recorded</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Two members on `IEncinaMetrics`: `TrackStarted(kind, name)` and `TrackFinished(kind, name)`, called by the dispatcher around the pipeline** | Same shape as `TrackSuccess` and `TrackFailure`; one place; a custom `IEncinaMetrics` can forward it to another system | A third-party implementer must add two members |
| **B) A separate `IActiveRequestTracker` service** | No interface change | A second service for one instrument; the dispatcher resolves two services per request |

### Chosen Option: **A — Two members on the existing interface**

### Rationale

- The interface is the documented extension point of the request metrics (its remarks say it "can be customized to integrate with other observability systems"); an active gauge is one more fact of the same lifecycle. Only four test doubles implement it: `EncinaMetricsSpy` (`tests/Encina.UnitTests/Core/EncinaTests.cs:1936`) and `NoopMetrics` (`tests/Encina.UnitTests/Core/PipelineErrorCategoryTests.cs:70`), which gain the two members, and the two `SpyMetrics` of the behavior test files, which Design Choice 1 deletes with the behaviors.
- The decrement runs in a `finally`, so an exception that escapes the dispatcher (the pure-ROP fail-fast path) still balances the counter.

**Class:** Trivial
**Settled by:** the existing `TrackSuccess`/`TrackFailure` members of `IEncinaMetrics` (`src/Encina/Diagnostics/IEncinaMetrics.cs:12-28`) and `AGENTS.md` section 1 (pre-1.0, breaking changes acceptable)

</details>

<details>
<summary><strong>4. Where the messaging instruments are recorded</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) In the `Instrumented*Store` decorators of `Encina.OpenTelemetry`** | One place per store; covers all providers | Cannot see the facts that are orchestrator semantics (an inbox duplicate, a saga reaching a terminal status with its duration); decorators get no service provider today (`DecorateService` passes only the inner store), and the metrics class would have to be a DI singleton there as well |
| **B) In the orchestrators of `Encina.Messaging` (`InboxOrchestrator`, `SagaOrchestrator`, `SchedulerOrchestrator`, `OutboxOrchestrator` and the internal `OutboxBatchProcessor`), through an optional `MessagingStoreMetrics` constructor parameter** | The orchestrators already know the facts and already feed `OutboxProcessorMetrics`; all 10 providers sit below them; one instance registered once | Four DI-built constructors gain a last optional parameter that DI fills (the 72 `new X(` sites of those four classes in `src` and `tests` keep compiling); `OutboxBatchProcessor` is built by hand with `new` at `OutboxOrchestrator.cs:151` and `OutboxProcessorBase.cs:132`, so DI never fills it: `OutboxOrchestrator` passes its own parameter and `OutboxProcessorBase` resolves `services.GetService<MessagingStoreMetrics>()` in the cycle scope, as it does for `DeadLetterSourceCapture` |

### Chosen Option: **B — The orchestrators record** (recommended, pending the maintainer)

### Rationale

- B wins because the duplicate, the terminal saga status and the retry count are not visible at a store call. A wins for a host with a custom processor that bypasses the orchestrators; such a processor can call the public `MessagingStoreMetrics` itself.
- `MessagingStoreMetrics` becomes one DI singleton (`TryAddSingleton` in `AddEncinaMessaging`), and `MessagingStoreMetricsInitializer` resolves it instead of constructing a second one: two instances would create each instrument twice on the shared meter and double every measurement.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. How the outbox pending gauge gets its value</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A hosted refresher polls `IOutboxStore.GetPendingCountAsync` on a `PeriodicTimer` and caches the value that the gauge callback returns** | The gauge callback is synchronous and costs nothing; the database call is asynchronous with a token | A value up to one interval old; one query per interval per host instance |
| **B) The gauge callback calls the store and waits for the result** | Always fresh | A synchronous wait on an asynchronous database call from the metrics reader thread: forbidden |

### Chosen Option: **A — A cached refresher**

### Rationale

- `AGENTS.md` section 3 requires database calls to be asynchronous with a `CancellationToken` and never the synchronous overloads (thread-pool starvation); a gauge callback is synchronous.
- The loop catches per iteration (an `OperationCanceledException` of the stopping token ends it), logs the failure by error code and keeps running with the last value, as `AGENTS.md` section 3 says for a timer with no operation to retry.

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (database calls asynchronous with a `CancellationToken`; background loops catch per iteration and log by error code); only A complies

</details>

<details>
<summary><strong>6. Gauges for active sagas and pending scheduled messages</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An in-process `UpDownCounter` that the orchestrators increment on start and decrement on a terminal status** | No store change | Wrong after a restart or crash (the count restarts at zero) and across instances; a saga recovered from the store is never counted |
| **B) Add a count query to `ISagaStore` and `IScheduledMessageStore` on the 10 providers and feed the same refresher** | Correct, global | 10 providers x 2 stores plus the fakes: far beyond one pull request; its own issue |

### Chosen Option: **B — A follow-up issue; this plan delivers neither gauge** (recommended, pending the maintainer)

### Rationale

- B wins because a wrong gauge is worse than none for an alert on stuck sagas; A wins only for a single-process host that accepts the drift. The issue file `artifacts/issues/store-count-queries-saga-scheduled.md` carries the provider matrix.
- The saga duration histogram does not need the count: the orchestrator reads `StartedAtUtc` and `CompletedAtUtc` of the state it moves to a terminal status, so it is delivered here.

**Class:** Dilemma

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | New instruments follow ADR-054 settled point 4 from the start: lowercase, dot-separated, no `_total`, durations in seconds with unit `s`, counts with a braced singular unit: `encina.request.active` (`{request}`), `encina.outbox.message.retries` (histogram, `{retry}`), `encina.saga.duration` (histogram, `s`) | Settled | ADR-054 Decision, settled point 4; #2678 renames the existing ones |
| C2 | Attributes: `request.kind`, `request.name` (the request type name, bounded by the number of request types), `failure.reason` (an error code, bounded by the code set), `saga_type`, `outcome`; never `EncinaError.Message`, a request value or an id | Settled | `AGENTS.md` section 3 (messages never reach telemetry); ADR-054 settled point 2 |
| C3 | No tenant attribute here: ADR-054 D4 requires it only when multi-tenancy is on and through one helper; #2676 owns the helper and adds one call to `EncinaMetrics` and `MessagingStoreMetrics`; this plan adds no tenant tag and no cardinality | Settled | ADR-054 D4; #2676 |
| C4 | No new `EncinaError` code: a store failure in the refresher is logged by the existing error code and never returned, so there is no ADR-062 `ErrorCategory` to assign | Settled | ADR-062 (a category is chosen where an `EncinaError` is created; none is created) |
| C5 | Metrics are recorded when the orchestrator applies the state change through the store API; the commit belongs to the caller (`SagaOrchestrator.CommitAsync` of the #720 plan) and the orchestrator never calls `SaveChangesAsync`, so a counter can exceed the committed count when a later commit fails; the plan does not claim "after commit" | Settled | `SagaOrchestrator` (it mutates the state and calls the store, no save); #720 plan |
| C6 | `OutboxPendingRefresher` reads `OutboxOptions.MaxRetries` for `GetPendingCountAsync(maxRetries, ct)`, runs every `EncinaOpenTelemetryOptions.MessagingMetricsRefreshInterval` (default 30 seconds, validated greater than 1 second at registration), creates a scope per tick (`IServiceScopeFactory`; `IOutboxStore` is scoped, so nothing scoped is injected into the singleton) and idles with one log when no `IOutboxStore` is registered | Trivial | `OutboxProcessorBase.ProcessPendingMessagesAsync` (scope per iteration); AGENTS.md section 3 (registration completeness) |
| C7 | The pending value is reported by every host instance (one cached value each), so a dashboard uses `max by (service)` and not `sum`; #187 writes it that way | Settled | A global database count read by N instances is N equal series |
| C8 | New public names: `IEncinaMetrics.TrackStarted`, `TrackFinished`; `MessagingStoreMetrics.RecordOutboxRetry`, `RecordSagaCompleted`; option `EncinaOpenTelemetryOptions.MessagingMetricsRefreshInterval` | Dilemma (recommended, pending the maintainer) | public surface; alternatives are `TrackRequestStarted` or a nested options class |

---

## Implementation Phases

### Phase 1: The Dispatcher Is the Single Recorder (pull request M1)

> **Goal**: One number per request, functional failures included, with the two duplicate behaviors removed (Design Choice 1, option B; if the maintainer picks A, this phase is dropped and M2 adds its members next to the behaviors).

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina/Dispatchers/Encina.RequestDispatcher.cs`: resolve `IFunctionalFailureDetector` from the request scope (`serviceProvider.GetService<IFunctionalFailureDetector>() ?? NullFunctionalFailureDetector.Instance`; it is registered by `AddEncina` as a singleton) and, on a `Right`, call `TrackFailure(kind, name, elapsed, failureReason)` when `TryExtractFailure` reports a functional failure and `TrackSuccess` otherwise, the rule `CommandMetricsPipelineBehavior` applies today.
2. **Delete** `src/Encina/Pipeline/Behaviors/CommandMetricsPipelineBehavior.cs` and `QueryMetricsPipelineBehavior.cs`; remove their lines from `src/Encina/PublicAPI.Unshipped.txt` and their entries from `.github/coverage-manifest/Encina.json` (the source files no longer exist, so the ratchet does not apply).
3. **Delete** their tests: `tests/Encina.UnitTests/Pipeline/Behaviors/CommandMetricsPipelineBehaviorTests.cs`, `QueryMetricsPipelineBehaviorTests.cs`, `tests/Encina.GuardTests/Core/Pipeline/CommandMetricsPipelineBehaviorGuardTests.cs`, `QueryMetricsPipelineBehaviorGuardTests.cs`, `tests/Encina.ContractTests/Core/Pipeline/MetricsPipelineBehaviorContractTests.cs`; **edit** the three other test files that name them: the two type lists `tests/Encina.ContractTests/PipelineBehaviorContracts.cs:37-39` and `tests/Encina.ContractTests/ServiceRegistrationContracts.cs:15-17`, and `tests/Encina.UnitTests/Core/PipelineErrorCategoryTests.cs`, whose four tests `CommandMetricsBehavior_Cancelled_IsCancelled`, `CommandMetricsBehavior_Exception_IsInternal`, `QueryMetricsBehavior_Cancelled_IsCancelled` and `QueryMetricsBehavior_Exception_IsInternal` (lines 286-343) construct the deleted behaviors and are removed (the same cancelled and exception categories are asserted through the dispatcher in the new `EncinaRequestMetricsTests`, task 4); its `NoopMetrics` stays. Before deleting, grep test aliases (`using X = ...`) and every `typeof(CommandMetricsPipelineBehavior<,>)` site; `git grep -E "(Command|Query)MetricsPipelineBehavior"` on `0336d31c7` outside `docs` and `changelog.d` finds exactly 12 files: the two behavior sources, `PublicAPI.Unshipped.txt`, `.github/coverage-manifest/Encina.json`, the five test files above, the two contract lists and `PipelineErrorCategoryTests.cs`.
4. **Create** `tests/Encina.UnitTests/Core/EncinaRequestMetricsTests.cs`: a real `Encina` with an `EncinaMetrics` and a `MeterListener` (not a spy): a `Right` response records one success and one duration; a `Left` records one failure with `failure.reason` equal to the error code; a `Right` for which a stub `IFunctionalFailureDetector` reports a failure records a failure with the detector's reason; a missing handler records `encina.request.handler_missing`; a cancelled request records the cancellation code and the `Cancelled` error category; a throwing behavior yields the `Internal` category (the assertions of the four removed tests); the error message never appears in any tag value.
5. Add `changelog.d/174-single-request-metrics-recorder.changed.md` through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0, no compatibility layers). Issue #174, plan docs/plans/request-metrics-implementation-plan-174.md,
phase 1. The dispatcher (src/Encina/Dispatchers/Encina.RequestDispatcher.cs) already records every Send through IEncinaMetrics;
CommandMetricsPipelineBehavior and QueryMetricsPipelineBehavior record again and are removed.

TASK:
1. In RequestDispatcher.ExecuteAsync resolve IFunctionalFailureDetector from the scope and count a Right with a functional failure
   as TrackFailure (reason = the detector's reason), otherwise TrackSuccess.
2. Delete the two behaviors, their PublicAPI lines, manifest entries and the 5 test files named in the plan; edit the two contract
   type lists, and remove the four metrics-behavior tests of tests/Encina.UnitTests/Core/PipelineErrorCategoryTests.cs
   (lines 286-343). Grep test aliases and typeof sites on origin/main before deleting.
3. Add EncinaRequestMetricsTests with a real MeterListener, including the cancelled and exception error categories.
4. PublicAPI, manifest and the changelog fragment go through mechanical-fixer.

KEY RULES:
- Reasons are error codes or the detector reason, never EncinaError.Message (AGENTS.md section 3).
- Edit source only with Edit/Write; no [Obsolete], no alias.
- The build must pass with the behaviors gone: no leftover reference.

REFERENCE FILES:
- src/Encina/Pipeline/Behaviors/CommandMetricsPipelineBehavior.cs (the failure-detector rule to move)
- src/Encina/Diagnostics/IEncinaMetrics.cs, src/Encina/Dispatchers/Encina.RequestDispatcher.cs
```

</details>

---

### Phase 2: Active Requests (pull request M2)

> **Goal**: `encina.request.active`, balanced on every path.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina/Diagnostics/IEncinaMetrics.cs`: add `void TrackStarted(string requestKind, string requestName)` and `void TrackFinished(string requestKind, string requestName)` to the interface; `EncinaMetrics` creates `_activeCounter = Meter.CreateUpDownCounter<long>("encina.request.active", unit: "{request}", description: ...)` and adds +1 and -1 with the tags `request.kind` and `request.name`; both methods call `ArgumentNullException.ThrowIfNull` on each argument.
2. **Modify** `src/Encina/Dispatchers/Encina.RequestDispatcher.cs`: call `TrackStarted` after the kind is known and `TrackFinished` in a `finally` that wraps handler resolution, validation and the pipeline, so the early returns (missing handler, type mismatch) and a fail-fast exception are balanced.
3. **Modify** `src/Encina/PublicAPI.Unshipped.txt` and `.github/coverage-manifest/Encina.json`; add `changelog.d/174-active-requests-metric.added.md` through `mechanical-fixer`.
4. Tests: **edit** `tests/Encina.UnitTests/Core/EncinaTests.cs` (the spy at line 1936 implements the two members) and `tests/Encina.UnitTests/Core/PipelineErrorCategoryTests.cs` (`NoopMetrics`, line 70, implements them); **create** `tests/Encina.UnitTests/Diagnostics/EncinaMetricsActiveRequestsTests.cs` (a `MeterListener` sees +1 then -1 per request; an `Encina` whose handler throws still decrements; a missing handler decrements; two concurrent requests peak at 2) and `tests/Encina.GuardTests/Diagnostics/EncinaMetricsGuardTests.cs` (null kind, null name on both new members).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #174 plan docs/plans/request-metrics-implementation-plan-174.md, phase 2. Phase 1 merged.

TASK:
1. Add TrackStarted and TrackFinished to IEncinaMetrics and EncinaMetrics (UpDownCounter<long> "encina.request.active", unit "{request}").
2. Call them from the dispatcher: started after the request kind is known, finished in a finally.
3. Update the EncinaTests spy and the PipelineErrorCategoryTests NoopMetrics; add unit and guard tests; PublicAPI, manifest, changelog through mechanical-fixer.

KEY RULES:
- The name follows ADR-054 settled point 4 (no _total); the unit lives in the metadata.
- Every path decrements, including the early returns and a thrown exception.

REFERENCE FILES:
- src/Encina/Diagnostics/IEncinaMetrics.cs, src/Encina/Dispatchers/Encina.RequestDispatcher.cs
- tests/Encina.UnitTests/Core/EncinaTests.cs (the spy)
```

</details>

---

### Phase 3: Wire the Inbox and Saga Instruments (pull request M3)

> **Goal**: `MessagingStoreMetrics` becomes one DI singleton and the inbox and saga instruments reach a listener, from the orchestrators.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina.Messaging/Diagnostics/MessagingStoreMetrics.cs`: add `RecordSagaCompleted(string sagaType, string outcome, double durationSeconds)` (a histogram `encina.saga.duration`, unit `s`, tags `saga_type` and `outcome`; validate the strings with `ArgumentException.ThrowIfNullOrWhiteSpace`); remove `RecordOutboxProcessed` (it duplicates `encina.outbox.processor.messages_total` of `OutboxProcessorMetrics`) and `RecordOutboxAdded` (`OutboxProcessorMetrics.RecordEnqueued` covers it); `git grep -E "RecordOutboxProcessed|RecordOutboxAdded"` on `0336d31c7` finds only the declarations and their `PublicAPI.Unshipped.txt` lines (728, 729), no test.
2. **Modify** `src/Encina.Messaging/MessagingServiceCollectionExtensions.cs` (`AddEncinaMessaging`): `services.TryAddSingleton(static provider => new MessagingStoreMetrics(provider.GetService<MessagingStoreMetricsCallbacks>()))`, so one instance exists and a callbacks registration made by `Encina.OpenTelemetry` is honored.
3. **Modify** `InboxOrchestrator` and `SagaOrchestrator`, each with a last optional parameter `MessagingStoreMetrics? metrics = null` (after the existing optional parameters, so CS1737 does not apply; DI fills it): `InboxOrchestrator` (`RecordInboxDuplicate()` where an existing processed message is returned, `InboxOrchestrator.cs:214`; `RecordInboxReceived` after the new message is added, `InboxOrchestrator.cs:160`); `SagaOrchestrator` (`RecordSagaTransition` on each step advance and `RecordSagaCompleted` where the status becomes `Completed`, `Compensated`, `Failed` or `TimedOut`, lines 243, 345, 389 and 511, with duration `CompletedAtUtc - StartedAtUtc` from the injected `TimeProvider`).
4. **Modify** `src/Encina.Messaging/PublicAPI.Unshipped.txt` (remove lines 728-729, add `RecordSagaCompleted`), `.github/coverage-manifest/Encina.Messaging.json`; add `changelog.d/174-messaging-store-metrics-wired.fixed.md` through `mechanical-fixer`.
5. Tests: **create** `tests/Encina.UnitTests/Messaging/Diagnostics/MessagingStoreMetricsRecordingTests.cs` (a `MeterListener` sees each instrument with its tags), `tests/Encina.UnitTests/Messaging/Inbox/InboxOrchestratorMetricsTests.cs` (a duplicate message records one duplicate and no received), `tests/Encina.UnitTests/Messaging/Sagas/SagaOrchestratorMetricsTests.cs` (with a `FakeTimeProvider`: a saga completed after 90 seconds records 90 and outcome `completed`; each terminal status records once) and `tests/Encina.UnitTests/Messaging/Diagnostics/MessagingStoreMetricsRegistrationTests.cs` (the DI test: a provider built with `ValidateOnBuild` and `ValidateScopes` after `AddEncinaMessaging` resolves one `MessagingStoreMetrics`); **edit** the existing `tests/Encina.GuardTests/Messaging/Diagnostics/MessagingStoreMetricsGuardTests.cs` (null or blank `sagaType`, `outcome` of `RecordSagaCompleted`; a negative duration is a valid record of zero).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #174 plan docs/plans/request-metrics-implementation-plan-174.md, phase 3. MessagingStoreMetrics declares instruments
that no code ever records and no code registers the callbacks; OutboxProcessorMetrics and SchedulingProcessorMetrics are fed.

TASK:
1. Register MessagingStoreMetrics as one singleton in AddEncinaMessaging (TryAddSingleton with the optional callbacks from DI).
2. Add the last optional parameter MessagingStoreMetrics? metrics = null to InboxOrchestrator and SagaOrchestrator;
   record duplicate, received, transition and saga duration at the sites named in the plan.
3. Remove RecordOutboxProcessed and RecordOutboxAdded (duplicated by OutboxProcessorMetrics); add RecordSagaCompleted.
4. Tests with a real MeterListener and FakeTimeProvider, a DI test with ValidateOnBuild and ValidateScopes, edit the existing guard test file;
   PublicAPI, manifest, changelog through mechanical-fixer.

KEY RULES:
- The orchestrators never call SaveChangesAsync: do not describe a metric as recorded after the commit.
- Optional parameter last (CS1737); grep every new X( site on origin/main before changing a constructor.
- Tags carry saga type, outcome and codes only, never messages or ids.

REFERENCE FILES:
- src/Encina.Messaging/Diagnostics/MessagingStoreMetrics.cs, OutboxProcessorMetrics.cs
- src/Encina.Messaging/Inbox/InboxOrchestrator.cs, Sagas/SagaOrchestrator.cs
- tests/Encina.GuardTests/Messaging/Diagnostics/MessagingStoreMetricsGuardTests.cs
```

</details>

---

### Phase 4: Wire the Outbox Retry and Scheduling Instruments (pull request M4)

> **Goal**: The outbox retry histogram and the scheduling counters reach a listener, including through the two places that build `OutboxBatchProcessor` by hand.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `MessagingStoreMetrics.cs`: add `RecordOutboxRetry(int retryCount)` (a histogram `encina.outbox.message.retries`, unit `{retry}`).
2. **Modify** `src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs` (internal; constructor gains a last optional `MessagingStoreMetrics? metrics = null` after `deadLetterCapture`, and `RecordOutboxRetry(message.RetryCount)` runs when a message is marked failed); **modify** its two production callers, which DI never reaches because they use `new`: `src/Encina.Messaging/Outbox/OutboxOrchestrator.cs:151` (the constructor gains a last optional `MessagingStoreMetrics? metrics = null` that DI fills and the orchestrator passes on) and `src/Encina.Messaging/Outbox/OutboxProcessorBase.cs:132` (the cycle scope resolves `services.GetService<MessagingStoreMetrics>()` and passes it, like `DeadLetterSourceCapture`; the protected constructor does not change). The other two `new OutboxBatchProcessor(` sites (`tests/Encina.UnitTests/Messaging/Outbox/OutboxRetryAfterCeilingTests.cs:182` and `tests/Encina.PropertyTests/Messaging/RetryAfterCeilingProperties.cs:75`) keep compiling because the parameter is optional.
3. **Modify** `SchedulerOrchestrator` with the same last optional parameter (DI fills it): `RecordScheduled` when a message is scheduled and `RecordExecuted` when one is processed.
4. **Modify** `src/Encina.Messaging/PublicAPI.Unshipped.txt`, `.github/coverage-manifest/Encina.Messaging.json`; add `changelog.d/174-outbox-retry-scheduling-metrics.fixed.md` through `mechanical-fixer`.
5. Tests: **create** `tests/Encina.UnitTests/Messaging/Outbox/OutboxRetryMetricsTests.cs` (a failed message records its `RetryCount`; through `OutboxOrchestrator` and through a processor derived from `OutboxProcessorBase` with a registered `MessagingStoreMetrics`, so both hand-built sites are covered) and `tests/Encina.UnitTests/Messaging/Scheduling/SchedulerOrchestratorMetricsTests.cs` (scheduling records one scheduled, execution records one executed with its outcome); **edit** `MessagingStoreMetricsRecordingTests.cs` (the retry histogram).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Issue #174 plan docs/plans/request-metrics-implementation-plan-174.md, phase 4. Phase 3 merged: MessagingStoreMetrics is a DI singleton.

TASK:
1. Add RecordOutboxRetry to MessagingStoreMetrics.
2. Add the last optional parameter MessagingStoreMetrics? metrics = null to OutboxBatchProcessor, OutboxOrchestrator and SchedulerOrchestrator;
   OutboxOrchestrator.cs:151 passes its parameter, OutboxProcessorBase.cs:132 resolves GetService<MessagingStoreMetrics>() in the cycle scope.
3. Record retry, scheduled and executed at the sites named in the plan.
4. Tests with a real MeterListener; PublicAPI, manifest, changelog through mechanical-fixer.

KEY RULES:
- DI never fills a constructor that is called with new: pass the instance explicitly at every new OutboxBatchProcessor( site.
- Optional parameter last (CS1737); grep every new X( site on origin/main before changing a constructor.

REFERENCE FILES:
- src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs, OutboxOrchestrator.cs, OutboxProcessorBase.cs
- src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs
```

</details>

---

### Phase 5: The Outbox Pending Gauge (pull request M5)

> **Goal**: `encina.outbox.pending_count` reports the store's pending count without a database call in the callback.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.OpenTelemetry/MessagingStores/OutboxPendingRefresher.cs` (`internal sealed class : BackgroundService`; constructor `(IServiceScopeFactory scopeFactory, EncinaOpenTelemetryOptions options, TimeProvider timeProvider, ILogger<OutboxPendingRefresher> logger)`; a `PeriodicTimer(options.MessagingMetricsRefreshInterval, timeProvider)`; each tick creates a scope, resolves `IOutboxStore` and `OutboxOptions`, calls `GetPendingCountAsync(maxRetries, stoppingToken)` and stores the result with `Interlocked.Exchange`; a `Left` is logged by error code through event 7011 and keeps the last value; an exception is caught per tick (event 7012 with `ForLogging()`), an `OperationCanceledException` of the stopping token ends the loop) and `src/Encina.OpenTelemetry/MessagingStores/MessagingStoreMetricsLog.cs` (`[LoggerMessage]`, EventIds 7010 no outbox store registered, 7011 count query failed, 7012 refresh iteration failed; "Event IDs: 7010-7012 (see EventIdRanges.OpenTelemetry)"; no new range).
2. **Modify** `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs`: register `OutboxPendingRefresher` as a hosted service and a `MessagingStoreMetricsCallbacks` singleton whose `getOutboxPendingCount` reads the refresher's cache (`getActiveSagaCount` returns the sentinel `0` until the follow-up issue lands; the gauge for active sagas is therefore created only when the callbacks object reports it, see task 3); **modify** `MessagingStoreMetricsInitializer.cs` to resolve the singleton `MessagingStoreMetrics` instead of constructing one.
3. **Modify** `src/Encina.Messaging/Diagnostics/MessagingStoreMetrics.cs` so the `encina.saga.active_count` gauge is created only when the callbacks carry a saga source (`Func<long>? getActiveSagaCount`), removing the always-zero series; the constructor of `MessagingStoreMetricsCallbacks` keeps `getOutboxPendingCount` first and takes the saga function as a nullable second parameter (the Messaging `PublicAPI.Unshipped.txt` line 736 changes to `System.Func<long>? getActiveSagaCount`).
4. **Modify** `src/Encina.OpenTelemetry/EncinaOpenTelemetryOptions.cs` (`TimeSpan MessagingMetricsRefreshInterval { get; set; } = TimeSpan.FromSeconds(30)`), the OpenTelemetry and Messaging `PublicAPI.Unshipped.txt`, `.github/coverage-manifest/Encina.OpenTelemetry.json`; add `changelog.d/174-outbox-pending-gauge.added.md` through `mechanical-fixer`.
5. Tests (new): `tests/Encina.UnitTests/OpenTelemetry/MessagingStores/OutboxPendingRefresherTests.cs` (a `FakeTimeProvider` advances one interval: the cached value equals the store's count; a `Left` keeps the last value and logs event 7011 with the code only; a thrown exception does not end the loop; cancellation ends it; no store registered logs 7010 once), `tests/Encina.UnitTests/OpenTelemetry/MessagingStores/MessagingStoreMetricsWiringTests.cs` (the DI test: `AddEncinaMessaging` plus `AddEncinaOpenTelemetry` built with `ValidateOnBuild` and `ValidateScopes` resolves one `MessagingStoreMetrics` and the gauge reports the refreshed value through a `MeterListener`), `tests/Encina.GuardTests/OpenTelemetry/MessagingStores/OutboxPendingRefresherGuardTests.cs` (null scope factory, options, time provider, logger); **edit** `MessagingStoreMetricsInitializerTests.cs` for the singleton and the existing `tests/Encina.GuardTests/Messaging/Diagnostics/MessagingStoreMetricsGuardTests.cs`: its test at line 24 asserts that a null `getActiveSagaCount` throws `ArgumentNullException`, which no longer holds once the parameter is nullable, so it becomes a test that a null saga source is valid and creates no `encina.saga.active_count` gauge, while the null check of `getOutboxPendingCount` (line 16) stays.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
Issue #174 plan docs/plans/request-metrics-implementation-plan-174.md, phase 5. Phase 3 merged: MessagingStoreMetrics is a DI singleton.

TASK:
1. Create OutboxPendingRefresher (BackgroundService, PeriodicTimer with TimeProvider, scope per tick, cached value, per-iteration catch)
   and MessagingStoreMetricsLog with EventIds 7010-7012 of EventIdRanges.OpenTelemetry (7000-7009 are used by resharding).
2. Register the refresher and a MessagingStoreMetricsCallbacks singleton in AddEncinaOpenTelemetry; make the initializer resolve the singleton.
3. Create the saga gauge only when a saga source exists (getActiveSagaCount becomes nullable); add EncinaOpenTelemetryOptions.MessagingMetricsRefreshInterval (default 30 s).
4. Tests with FakeTimeProvider, a DI test with ValidateOnBuild and ValidateScopes, guard tests (and the edit of the existing
   MessagingStoreMetricsGuardTests null check of getActiveSagaCount); PublicAPI, manifest, changelog through mechanical-fixer.

KEY RULES:
- Never call the store from the gauge callback; async with the stopping token only (AGENTS.md section 3).
- Log by error code or ForLogging(); never EncinaError.Message. No scoped service in the singleton: use IServiceScopeFactory.
- EventIds packed without gaps inside 7010-7012; register nothing new.

REFERENCE FILES:
- src/Encina.Messaging/Outbox/OutboxProcessorBase.cs (scope per iteration), src/Encina.OpenTelemetry/MessagingStores/MessagingStoreMetricsInitializer.cs
- src/Encina.OpenTelemetry/Resharding/ReshardingLogMessages.cs (log style), src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

### Phase 6: Documentation (pull request D1)

> **Goal**: Say which instruments exist, which are gone and how to read the pending gauge.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.OpenTelemetry/README.md` (through `docs-writer`): the request and messaging instruments with their units, the recorder (the dispatcher), the 30-second refresh interval, the `max by` rule for the pending gauge.
2. `docs/features/pipeline-behaviors.md` and `docs/INVENTORY.md` (through `docs-writer`): remove the two deleted behaviors (lines 19-21 and 349-350 on 2026-11-20).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
Issue #174 plan docs/plans/request-metrics-implementation-plan-174.md, phase 6, documentation only. Phases 1 to 5 merged.

TASK:
1. Update the Encina.OpenTelemetry README, docs/features/pipeline-behaviors.md and docs/INVENTORY.md as the plan lists.

KEY RULES:
- Use the encina-docs skill; no emojis; no hand-typed coverage figures; every identifier must exist in src/.

REFERENCE FILES:
- src/Encina/Diagnostics/IEncinaMetrics.cs, src/Encina.Messaging/Diagnostics/MessagingStoreMetrics.cs
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| OpenTelemetry metrics API | `UpDownCounter` for a value that rises and falls; `Gauge` for a measured level; instrument names are case-insensitive; the unit is metadata | `request.active`, the pending gauge |
| OpenTelemetry Prometheus exporter | A counter gets `_total`, a unit `s` becomes `_seconds`, dots become underscores | Why the instrument has no `_total` yet the series does (#187 reads the series names) |
| ADR-054 | Settled point 4 instrument rules, settled point 7 zero cost without a listener, D4 tenant tag, D6 Encina-level metrics | Names, units, scope |
| ADR-062 | Error category contract | No error is created (register row C4) |
| `AGENTS.md` section 3 | Database calls async with a token; background loops catch per iteration; messages never reach telemetry | Refresher, tags |
| `AGENTS.md` section 7 | EventId ranges registered first | Ids 7010-7012 inside `EventIdRanges.OpenTelemetry` |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IEncinaMetrics`, `EncinaMetrics` | `src/Encina/Diagnostics/IEncinaMetrics.cs` | Gains the active-request members |
| `RequestDispatcher.ExecuteAsync` | `src/Encina/Dispatchers/Encina.RequestDispatcher.cs` | The single recorder |
| `IFunctionalFailureDetector`, `NullFunctionalFailureDetector` | registered in `AddEncina` | The functional-failure rule moved from the behaviors |
| `MessagingStoreMetrics`, `MessagingStoreMetricsCallbacks` | `src/Encina.Messaging/Diagnostics/MessagingStoreMetrics.cs` | Declared instruments that get wired |
| `OutboxProcessorMetrics`, `SchedulingProcessorMetrics` | `src/Encina.Messaging/Diagnostics/` | Already count processed messages; not duplicated |
| `IOutboxStore.GetPendingCountAsync` | `src/Encina.Messaging/Outbox/IOutboxStore.cs` | The refresher's source |
| `OutboxProcessorBase` | `src/Encina.Messaging/Outbox/OutboxProcessorBase.cs` | Pattern: scope per iteration |
| `MessagingStoreMetricsInitializer` | `src/Encina.OpenTelemetry/MessagingStores/` | Resolves the singleton |
| `EventIdRanges.OpenTelemetry` | `src/Encina/Diagnostics/EventIdRanges.cs:303` | Ids 7010-7012 |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.OpenTelemetry` | 7010-7012 (inside `EventIdRanges.OpenTelemetry` 7000-7099; 7000-7009 are resharding) | 7010 no outbox store registered, 7011 count query failed (code only), 7012 refresh iteration failed (`ForLogging()`); no new range and no id of the block 5970-5999; #183 takes 7013-7015 of the same range |

### Estimated File Count

Every count includes tests, the coverage manifest, PublicAPI and the changelog fragment; each pull request builds and passes alone.

| PR | Content | Deleted src | New src | Changed src | New tests | Deleted tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|-------------|---------|-------------|-----------|---------------|--------------|----------|-------|-------|--------------|
| M1 | Dispatcher is the single recorder | 2 | 0 | 2 (dispatcher, PublicAPI) | 1 | 5 | 3 (2 contract lists, `PipelineErrorCategoryTests`) | 1 | 1 (changelog) | 15 | ~700 |
| M2 | Active requests | 0 | 0 | 3 (interface, dispatcher, PublicAPI) | 2 | 0 | 2 (`EncinaTests`, `PipelineErrorCategoryTests`) | 1 | 1 | 9 | ~440 |
| M3 | Inbox and saga instruments, singleton | 0 | 0 | 5 (metrics class, `AddEncinaMessaging`, `InboxOrchestrator`, `SagaOrchestrator`, PublicAPI) | 4 | 0 | 1 (`MessagingStoreMetricsGuardTests`) | 1 | 1 | 12 | ~700 |
| M4 | Outbox retry and scheduling instruments | 0 | 0 | 6 (metrics class, `OutboxBatchProcessor`, `OutboxOrchestrator`, `OutboxProcessorBase`, `SchedulerOrchestrator`, PublicAPI) | 2 | 0 | 1 (`MessagingStoreMetricsRecordingTests`) | 1 | 1 | 11 | ~450 |
| M5 | Outbox pending gauge | 0 | 2 | 6 (`ServiceCollectionExtensions`, initializer, options, `MessagingStoreMetrics`, 2 PublicAPI) | 3 | 0 | 2 (`MessagingStoreMetricsInitializerTests`, `MessagingStoreMetricsGuardTests`) | 1 | 1 | 15 | ~800 |
| D1 | Documentation | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 3 (README, 2 pages) | 3 | ~120 |
| **Total** | | **2** | **2** | **22** | **12** | **5** | **9** | **5** | **8** | **65** | **~3,210** |

The columns sum to the total (2 + 2 + 22 + 12 + 5 + 9 + 5 + 8 = 65; 15 + 9 + 12 + 11 + 15 + 3 = 65). M1 and M5 sit at the limit of 15 files: M1 cannot shed a file (the four `PipelineErrorCategoryTests` tests construct the deleted behaviors, so the edit must land with the deletion); if M5 grows, it splits the options class and the log class into their own pull request. The pull request of this plan itself is one file.

Per-file coverage targets (per flag, one sentence each), written to the package manifests; a touched file keeps its current target and the changed methods reach full line coverage (CRAP gate):

| File | Unit | Guard | Contract | Property | Justification |
|------|------|-------|----------|----------|---------------|
| `Encina/Diagnostics/IEncinaMetrics.cs` (the `EncinaMetrics` class; the manifest lists it as an interface with no tests today) | 95 | 80 | 0 | 0 | Every instrument is driven through a real `MeterListener`; guard reaches the null checks of the new members; no contract or property applies to a counter wrapper |
| `Encina/Dispatchers/Encina.RequestDispatcher.cs` | 85 | 0 | 0 | 0 | The functional-failure, missing-handler, cancellation and fail-fast paths are driven with a real `Encina`; the dispatcher is a private nested class with no guardable public parameter |
| `Encina.Messaging/Diagnostics/MessagingStoreMetrics.cs` | 95 | 85 | 0 | 0 | Each `Record*` method is driven with a listener and the guard run covers the string checks; a metrics holder has no contract or property |
| `Encina.Messaging/Inbox/InboxOrchestrator.cs`, `Sagas/SagaOrchestrator.cs`, `Outbox/OutboxBatchProcessor.cs`, `Outbox/OutboxOrchestrator.cs`, `Outbox/OutboxProcessorBase.cs`, `Scheduling/SchedulerOrchestrator.cs` | current target kept | current target kept | 0 | 0 | The new lines are covered by the named recording tests (`SchedulerOrchestratorMetricsTests` for the scheduler); an optional constructor parameter adds no guard (a null `metrics` is the valid default) |
| `Encina.OpenTelemetry/MessagingStores/OutboxPendingRefresher.cs` | 90 | 75 | 0 | 0 | The loop is driven tick by tick with `FakeTimeProvider` and a stub store; guard covers the four constructor arguments |
| `Encina.OpenTelemetry/MessagingStores/MessagingStoreMetricsLog.cs` | 0 | 0 | 0 | 0 | Generated `[LoggerMessage]` code with no parameters of its own; executed through the refresher |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #174 - real request and messaging metrics of Encina - in six small pull requests (M1-M5 and D1).

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution always; edit source only with the
  Edit/Write tools; PublicAPI, manifests and changelog fragments through mechanical-fixer; docs through docs-writer.
- ADR-054 settled point 4: instrument names lowercase and dot-separated, durations in seconds with unit s, no _total suffix;
  #2678 renames the existing instruments, this work renames nothing. D4: the tenant tag is #2676's, add none.

IMPLEMENTATION OVERVIEW:
1. M1: the dispatcher is the only request recorder (it learns functional failures); delete CommandMetricsPipelineBehavior and QueryMetricsPipelineBehavior with their tests.
2. M2: IEncinaMetrics.TrackStarted/TrackFinished and the UpDownCounter encina.request.active, balanced in a finally.
3. M3: MessagingStoreMetrics as one DI singleton; the inbox and saga orchestrators record duplicate, transition and saga duration.
4. M4: the outbox batch processor (built by hand at two sites) records retries; the scheduler records scheduled and executed.
5. M5: OutboxPendingRefresher (PeriodicTimer, cached value) feeds the pending gauge; EventIds 7010-7012; initializer resolves the singleton.
6. D1: README and two pages.

KEY PATTERNS:
- Real MeterListener tests, never reflection-only; FakeTimeProvider for time; DI tests with ValidateOnBuild and ValidateScopes.
- Tags: request kind and name, error code, saga type, outcome; never EncinaError.Message.
- Guard tests for every new public method with parameters.

REFERENCE FILES:
- src/Encina/Diagnostics/IEncinaMetrics.cs, src/Encina/Dispatchers/Encina.RequestDispatcher.cs
- src/Encina.Messaging/Diagnostics/MessagingStoreMetrics.cs, src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs
- docs/plans/request-metrics-implementation-plan-174.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Counters and gauges are real-time; the pending value is cached for one interval inside the refresher, which is not a cache provider read |
| 2 | OpenTelemetry | ✅ Phases 1-5 | This is the metrics work: one recorder, the active instrument, the wired messaging instruments |
| 3 | Structured Logging | ✅ Phase 5 | Events 7010-7012 for the refresher, error codes only |
| 4 | Health Checks | ❌ N/A | A threshold on these metrics is an alert rule (#187), not a health check; the outbox and saga health checks exist |
| 5 | Validation | ✅ Phase 5 | The refresh interval is validated at registration (greater than 1 second); instrument inputs are internal |
| 6 | Resilience | ❌ N/A | The refresher's store call needs no retry: the next tick is the retry |
| 7 | Distributed Locks | ❌ N/A | Each instance reads the count independently (register row C7) |
| 8 | Transactions | ❌ N/A | Metrics are non-transactional; register row C5 states what "recorded" means |
| 9 | Idempotency | ❌ N/A | A counter increment is not idempotent by design; the duplicate counter counts what the inbox already detected |
| 10 | Multi-Tenancy | ⏭️ #2676 | The tenant attribute is added by the shared helper of #2676 when multi-tenancy is on (ADR-054 D4); no tag and no cardinality here |
| 11 | Module Isolation | ⏭️ Follow-up issue file | `artifacts/issues/module-id-on-request-telemetry.md`: the module attribute needs `IRequestContext.ModuleId` from #2442 |
| 12 | Audit Trail | ❌ N/A | Infrastructure metrics, not a security-relevant state |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| ADR-054 accepted | ✅ On `main` | Settled point 4, D3, D4, D6 |
| #2678 (instrument rename) | Open | Not blocking: this plan creates only conforming names and renames nothing; whichever lands second adjusts the other's tests that name an instrument |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Meter rename and wildcard subscription | #2677 | The new instruments live on the meters that exist; the rename moves them |
| Tenant attribute helper | #2676 | Adds one call to `EncinaMetrics` and `MessagingStoreMetrics` |
| Dashboards | #187 | Reads the series; its plan lists the names |
| Count queries for sagas and scheduled messages | `artifacts/issues/store-count-queries-saga-scheduled.md` | The two gauges this plan does not deliver |
| Observability guide | #906 | Documents the gauge rule and the tag cardinality |

---

## Next Steps

1. Link this plan from #174 and ask the maintainer to answer Design Choices 1, 2, 4 and 6 and register row C8 when work starts.
2. Implement M1 and M2 first (core only), then M3, M4 and M5; the follow-up issue files are opened by the orchestrator.
3. When #174 is picked up, ask that its acceptance criteria be amended: `encina.request.total` and the static `EncinaMetrics` are replaced by the instruments above (Design Choices 1 and 2).
