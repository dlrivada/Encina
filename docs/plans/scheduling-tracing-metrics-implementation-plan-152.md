# Implementation Plan: Scheduled Message Tracing and Metrics (per-message spans, delay and duration instruments)

> **Issue**: [#152](https://github.com/dlrivada/Encina/issues/152)
> **Type**: Feature (observability; the issue is `status:proposal`, part of the scheduling family)
> **Complexity**: Low-Medium (observability only, no schema and no provider code; 4 pull requests of 6 to 8 files)
> **Estimated Scope**: ~330 lines of production code + ~700 lines of tests, documentation and manifests; 29 file touches (21 distinct files) in 4 pull requests
> **Family**: scheduling-3. Siblings are cited by issue number only: #176 (restores the producer trace at dispatch), #132 and #1164 (propagation API and persisted envelope), #177 (messaging attribute constants), #721 (store instrumentation and the `Encina.Messaging` meter), #1222, #146, #150, #133, #147 (scheduling features that add counters and spans of their own), #735 (dispatch context), #739 (tenant on the row), #159 (analytics, owns the pending count)

---

## Summary

### What exists today (read on 2026-11-25, `origin/main` `1c067b45b`)

- The issue's premise is half true. Scheduling already has telemetry, but most of it is not wired:
  - `SchedulingActivitySource` (`src/Encina.Messaging/Diagnostics/SchedulingActivitySource.cs`, internal, source `Encina.Messaging.Scheduling`) has `StartSchedule`, `StartProcessDue`, `StartExecute`, `StartCancel` and the processor-cycle helpers. Only `StartProcessingCycle`, `CompleteProcessingCycle` and `Failed` are called, by `ScheduledMessageProcessor` (`ScheduledMessageProcessor.cs` lines 147, 166, 172). `SchedulerOrchestrator` calls none of the others.
  - `InstrumentedScheduledMessageStore` (`src/Encina.OpenTelemetry/MessagingStores/InstrumentedScheduledMessageStore.cs`, internal) creates its own second `ActivitySource` with the same name and the same span names (`encina.scheduling.schedule`, `process_due`, `execute`, `cancel`). Its `execute` span wraps only the `MarkAsProcessedAsync` store call, not the dispatch of the request, so a trace reader sees an "execute" that took a database round trip.
  - `MessagingStoreMetrics` (meter `Encina`) declares `encina.scheduling.messages_scheduled_total` and `encina.scheduling.messages_executed_total` with `RecordScheduled` and `RecordExecuted`; a search of `src` finds no caller of either method (only the declarations, the two public API lines `src/Encina.Messaging/PublicAPI.Unshipped.txt:733-734` and the initializer that creates the class; no test calls either method).
  - `SchedulingProcessorMetrics` (`encina.scheduling.processor.messages_total` and `cycle_duration_seconds`) is fed by `ScheduledMessageProcessor`, which always passes `failureCount: 0` (`RecordBatch(successCount: count, failureCount: 0)`): the orchestrator returns only the number of dispatched messages, so the `failure` outcome is never recorded.
- No trace context is persisted or propagated: a search of `src` for `TraceContext`, `traceparent` and `TextMapPropagator` finds nothing. ADR-054 D1 puts the persisted trace in the #1164 envelope and not in a column of each message type, and the plans of #132 and #176 own the propagation API and the two processor call sites (`encina.scheduling.dispatch`, kind `Scheduled`, a new trace linked to the producer span).
- Today the structured logs the issue asks for exist: `SchedulerOrchestrator`'s `Log` class (EventIds 2931-2941 and 2989) logs scheduled, executed, failed, dispatch-failed and dead-letter paths; `SchedulingProcessorLog` and `SchedulingStoreLog` use 2300-2306 and 2320-2325.
- `SchedulingHealthCheck` exists (`src/Encina.Messaging/Health/SchedulingHealthCheck.cs`) and reports overdue messages; the "health" item of the issue is not new work here.
- The wiring point for the OpenTelemetry package is `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs`: it subscribes `tracing.AddSource("Encina.Messaging.Scheduling")` (line 207) and `metrics.AddMeter("Encina")` (lines 213 and 301). Implementers: `IScheduledMessageStore` has 10 (8 provider stores, `InstrumentedScheduledMessageStore`, `FakeScheduledMessageStore`); this plan does not change the interface.

### What the issue proposes and what changes

The issue proposes a public `SchedulingActivitySource` and `SchedulingMetrics`, a `TraceContext` property on `IScheduledMessage`, `EnableTracing` and `EnableMetrics` flags and a pending gauge. This plan keeps the useful part and corrects the rest: the trace column is superseded by ADR-054 D1 (Design Choice 1); the pending gauge needs a store count that #159 delivers (Design Choice 6); the flags are a Dilemma (Design Choice 2); and the instruments that exist but are dead or always zero are replaced, not duplicated (Design Choice 3).

### What this plan delivers

- Per-message spans in `SchedulerOrchestrator`: `encina.scheduling.schedule` (also for recurring), `encina.scheduling.execute` around the dispatch of one due message (the span the issue calls "process"), `encina.scheduling.cancel`, with the issue's semantic attributes under the `encina.scheduling.*` prefix.
- Three instruments on the `Encina.Messaging` meter (ADR-054 D3): counter `encina.scheduling.messages` (tag `encina.scheduling.outcome`: `scheduled`, `executed`, `failed`, `dead_lettered`), histogram `encina.scheduling.delay` (seconds from the due time to the start of the dispatch) and histogram `encina.scheduling.duration` (seconds of the dispatch). No `_total` suffix.
- Removal of the three instruments those replace, and the rename of the decorator's store-level spans so the two layers stop sharing names.
- The tenant dimension (`encina.tenant_id`, ADR-054 D4) in the last pull request, after #739.
- Shared pieces and owner: #176 owns the producer link and the `encina.scheduling.dispatch` span; #177 owns the `messaging.*` constants; #1222 owns the `rescheduled` and `cancelled` counters and the upsert spans; #721 creates the `Encina.Messaging` meter registration (`AddMeter("Encina.Messaging")`) when it lands first (this plan adds it if it does not); #159 owns the pending count and gauge; this plan owns the three instruments, the execute span and `SchedulingTelemetryNames`.
- Affected packages: `Encina.Messaging`, `Encina.OpenTelemetry`. Provider category: Observability (OpenTelemetry) over Scheduling; no database provider code changes, so the 10-provider matrix is satisfied by the existing stores and the integration scenario of Phase 6.
- Not delivered, with the reason: `IScheduledMessage.TraceContext` (ADR-054 D1, #1164, #176); a pending gauge (needs #159's count); webhook and analytics (#160 and #159).

---

## Design Choices

<details>
<summary><strong>1. Persist the trace context on the message (the issue's `TraceContext` property)</strong></summary>

**Class:** Settled
**Settled by:** ADR-054 D1 (the trace context is stored inside the request-context envelope of #1164, not in dedicated columns), ADR-054 D4 (no baggage) and the plan of #176 (the processor restores it at dispatch).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Do not add a property: the envelope of #1164 carries `traceparent` and `tracestate`, the call sites are #176's** | One schema change for all message types; the 13 `IScheduledMessage` implementers and 10 stores stay untouched | The producer link appears only when #1164 and #176 land |
| **B) `string? TraceContext { get; set; }` on `IScheduledMessage` as the issue states** | Self-contained | Contradicts ADR-054 D1 (rejected as option 1a); adds a column on 10 providers next to the #1164 change and a second persisted copy |

### Chosen Option: **A — no property**

### Rationale

- Only A complies with ADR-054 D1. This plan therefore adds no member to `IScheduledMessage` and touches no provider.

</details>

<details>
<summary><strong>2. `EnableTracing` and `EnableMetrics` flags (Dilemma)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No flags: spans are created only when an `ActivityListener` is attached (`ActivitySource.HasListeners()`, already the rule of `SchedulingActivitySource`) and instruments are recorded only when a `MeterListener` or exporter enables them (`Instrument.Enabled`); the opt-in is subscribing, which `WithEncina()` does** | Zero cost when unobserved; no option class growth, no second code path; same model as every other Encina source | A host cannot silence scheduling telemetry while keeping the subscription |
| **B) `SchedulingOptions.EnableTracing` and `EnableMetrics`, default `false`, as the issue states** | Per-feature switch independent of the OpenTelemetry subscription | Two options and a branch on every call; telemetry is off even when the host subscribed, which surprises; no other messaging pattern has such flags |

### Chosen Option: **A — no flags** (recommended, pending the maintainer)

### Rationale

- A wins when pay-for-what-you-use is read as "unobserved means free"; B wins if a host must keep the `Encina.Messaging` meter on for the outbox while silencing scheduling. Under B the flags default to `false` and gate the two helper classes, and `SchedulingOptions` gains two properties (one `PublicAPI.Unshipped.txt` line each, and a guard-free options test).

</details>

<details>
<summary><strong>3. The existing instruments that are dead or always zero (Dilemma)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Replace: delete `encina.scheduling.messages_scheduled_total` and `messages_executed_total` with `RecordScheduled` and `RecordExecuted` (no caller), and delete `encina.scheduling.processor.messages_total` (its `failure` outcome is always zero); keep `cycle_duration_seconds` (it stays on the plain `Encina` meter, which ADR-054 D3 retires: its move to the `Encina.Messaging` meter belongs to the ADR-054 D3 retirement of the plain meter, not to this plan; `WithEncina()` keeps `AddMeter("Encina")` until then, and #1787 checks that every meter is subscribed); the new counter carries all four outcomes** | One counter, no `_total`, no duplicate series; follows "no legacy code" (`AGENTS.md` section 1) | A dashboard built on the old names breaks; there is no released user (pre-1.0) |
| **B) Keep them and wire them: call `RecordScheduled` and `RecordExecuted` from the orchestrator and fix the processor's failure count** | No rename | Two series for the same fact; the `_total` names stay; the processor cannot know the failure count without changing `ProcessDueMessagesAsync`'s return type (32 call sites in 9 files) |

### Chosen Option: **A — replace** (recommended, pending the maintainer)

### Rationale

- A wins because nothing consumes the old names and the pre-1.0 rule asks for the complete change; B wins if a published dashboard already depends on them. The new counter's `failed` and `dead_lettered` outcomes are recorded where the failure is decided (`MarkAsFailedAsync` of the orchestrator), which fixes the always-zero defect without a signature change.

</details>

<details>
<summary><strong>4. Span layers: the orchestrator and the decorator (Dilemma)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The orchestrator owns the operation spans (`encina.scheduling.schedule`, `.execute`, `.cancel`); the decorator's spans are renamed `encina.scheduling.store.add`, `.get_due`, `.mark_processed`, `.mark_failed`, `.reschedule`, `.cancel` and become their children** | One name per layer; `execute` finally covers the dispatch; ADR-054 D6 (Encina-level spans on every store) is kept | The decorator and its unit test change (about 2 files) |
| **B) Keep both layers with the same names** | No edit | Two `encina.scheduling.schedule` spans per call, one of them a database call; the `execute` meaning stays wrong |
| **C) Remove the decorator's scheduling spans** | One layer | Loses the store-level latency that ADR-054 D6 asks for on every provider |

### Chosen Option: **A — rename the store spans** (recommended, pending the maintainer)

### Rationale

- A wins for readable traces; C wins if the store-level spans prove redundant in the observability guide of #906. The names follow the `encina.<pattern>.store.<operation>` shape that #721 proposes for the other three stores (the worker aligns the exact strings with #721 when it lands first).

</details>

<details>
<summary><strong>5. Meter name, instrument names and tags</strong></summary>

**Class:** Settled
**Settled by:** ADR-054 D3 (a meter per package, `Encina.Messaging`; the plain `Encina` name is being retired), ADR-054 D4 (tenant tag only when multi-tenancy is on) and the OpenTelemetry metric naming guidance applied the same way in the plan of #3994 (a counter name carries no `_total`; the exporter adds it).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `new Meter("Encina.Messaging", "1.0")`; `encina.scheduling.messages` (counter, `{messages}`), `encina.scheduling.delay` and `encina.scheduling.duration` (histograms, `s`); tags `encina.scheduling.outcome` (four fixed values) and `encina.scheduling.request_type` (the resolved type's `FullName`, `unknown` when it cannot be resolved); `AddMeter("Encina.Messaging")` in `WithEncina`** | Complies with D3; bounded tags (outcomes fixed, request types are the application's types); the message id is never a tag | One more `AddMeter` line in `Encina.OpenTelemetry` |
| **B) The shared `Encina` meter like `SchedulingProcessorMetrics`** | Already subscribed | Contradicts D3, which names the plain meter as the defect |

### Chosen Option: **A — the `Encina.Messaging` meter**

### Rationale

- Only A complies with D3. The message id, the error text and the content are span attributes at most (the id), never metric tags; failures record the error code only (`AGENTS.md` section 3: `EncinaError.Message` never reaches tags).

</details>

<details>
<summary><strong>6. The pending-message gauge (Dilemma)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Here, through `SchedulerOrchestrator.GetPendingCountAsync`** | Delivered with the other instruments | It calls `GetDueMessagesAsync(int.MaxValue, ...)` and materializes every due row at each metric collection; it counts only due rows, not pending ones |
| **B) In #159, through the count member that plan adds to the store (a `COUNT` query on the 10 providers); this plan leaves the gauge out** | Cheap, correct count; one owner | The gauge arrives with #159, not here |

### Chosen Option: **B — #159 delivers the gauge** (recommended, pending the maintainer)

### Rationale

- B wins on cost: a callback that loads every due row each collection is a performance defect, and A wins only if #159 is not accepted. The acceptance item "pending gauge" of the issue is therefore recorded against #159.

</details>

<details>
<summary><strong>7. Tenant dimension</strong></summary>

**Class:** Settled
**Settled by:** ADR-054 D4 and SPEC-002 AC-044 (the tenant id is a span attribute and a metric tag when multi-tenancy is on), and the helper of #2676 (`TenantTelemetry.ValueFor`).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Add the tag in the last pull request, from `IScheduledMessage.TenantId` (#739) through `TenantTelemetry.ValueFor(tenantId, multiTenancyEnabled)`; no tag when multi-tenancy is off** | Complies with D4; no raw id when tenancy is off | Waits for #739 |
| **B) Never tag the tenant** | No cardinality risk | Breaks AC-044 for metrics |

### Chosen Option: **A — T4, after #739**

### Rationale

- Only A complies. The cardinality risk is documented in the observability guide of #906. Before #739 the row carries no tenant, so the cycle emits no tenant tag.

</details>

---

## Implementation Phases

### Phase 1: Telemetry names and instruments (PR T1)

> **Goal**: One constants class and one instrument class, with no behavior change yet.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Messaging/Diagnostics/SchedulingTelemetryNames.cs` (`internal static class`): the span names, the attribute keys (`encina.scheduling.message_id`, `.request_type`, `.delay_ms`, `.retry_count`, `.outcome`) and the four outcome values. The existing keys `scheduling.message_id`, `scheduling.message_type`, `scheduling.scheduled_at` become `encina.scheduling.*` (the issue's convention); `messaging.*` keys come from the #177 constants class when it has merged.
2. **Create** `src/Encina.Messaging/Diagnostics/SchedulingExecutionMetrics.cs` (`internal sealed class`, a static `Meter("Encina.Messaging", "1.0")`): `RecordScheduled(string requestType)`, `RecordExecuted(string requestType, TimeSpan delay, TimeSpan duration)`, `RecordFailed(string requestType, TimeSpan delay, TimeSpan duration, bool deadLettered)`. `requestType` is validated with `ArgumentException.ThrowIfNullOrWhiteSpace`.
3. **Modify** `src/Encina.Messaging/Diagnostics/SchedulingActivitySource.cs`: use the new names; `StartExecute(Guid messageId, string requestType, int retryCount, TimeSpan delay)` sets the issue's attributes; `Failed` keeps recording the error code only; add `SetOutcome(Activity?, string outcome)`.
4. Tests (new): `tests/Encina.UnitTests/Messaging/Diagnostics/SchedulingExecutionMetricsTests.cs` (a `MeterListener` asserts the counter's four outcomes, the delay and duration measurements, the tag set, and that no message id or error text is ever a tag), `tests/Encina.GuardTests/Messaging/Diagnostics/SchedulingExecutionMetricsGuardTests.cs` (null, empty and whitespace `requestType` on each method; `Encina.Messaging` grants `InternalsVisibleTo` to `Encina.GuardTests`), and the span-attribute assertions in the orchestrator telemetry test of Phase 2.
5. `.github/coverage-manifest/Encina.Messaging.json` entries through `mechanical-fixer`. No `PublicAPI` line: every type is internal.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
- Issue #152, plan docs/plans/scheduling-tracing-metrics-implementation-plan-152.md. Dilemmas 2, 3, 4 and 6 are recommended, pending the maintainer: follow the Maintainer Decisions section when it exists.
- Encina is pre-1.0: no compatibility shims. SchedulingActivitySource and the metrics classes are internal.

TASK:
Create SchedulingTelemetryNames and SchedulingExecutionMetrics and extend SchedulingActivitySource as listed in Phase 1.

KEY RULES:
- Meter name Encina.Messaging; no "_total" suffix; counter tags are outcome and request type only.
- EncinaError.Message never reaches a tag or a status description: record the error code only.
- Guard every string parameter; HasListeners and Instrument.Enabled keep the unobserved path free.

REFERENCE FILES:
- src/Encina.Messaging/Diagnostics/SchedulingActivitySource.cs
- src/Encina.Messaging/Diagnostics/SchedulingProcessorMetrics.cs
- tests/Encina.GuardTests/Messaging/Diagnostics/MessagingStoreMetricsGuardTests.cs
```

</details>

### Phase 2: Orchestrator wiring (PR T1)

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs`:
   - `ScheduleAsync` and `ScheduleRecurringAsync`: `SchedulingActivitySource.StartSchedule(...)` before the store call, `Complete` or `Failed(errorCode)` after, and `RecordScheduled` on `Right`.
   - `DispatchMessageAsync`: start `StartExecute` with the delay (`now - (NextRetryAtUtc ?? ScheduledAtUtc)`, never negative, from `_timeProvider`), time the `executeCallback` with `_timeProvider.GetTimestamp()` and `GetElapsedTime`, then `RecordExecuted` on success (`CompleteDispatchedMessageAsync`) or `RecordFailed` in `MarkAsFailedAsync` (`deadLettered` from `decision.IsDeadLettered`). The request type tag is the resolved `Type.FullName`, `unknown` for the two pre-dispatch failures.
   - `CancelAsync`: `StartCancel`.
   - The constructor is unchanged (the metrics class is a static-meter instance created inside); `new SchedulerOrchestrator(` has 28 sites in 8 files, none changes.
2. The orchestrator never calls `SaveChangesAsync` from these paths and the telemetry makes no atomicity claim: the span ends when the store call returns, whatever the caller commits later.
3. Tests (new): `tests/Encina.UnitTests/Messaging/Scheduling/SchedulerOrchestratorTelemetryTests.cs`: an `ActivityListener` and a `MeterListener` around a mocked store: schedule creates one span and one `scheduled` measurement; a successful dispatch gives an `execute` span with the issue's attributes, one `executed` measurement and a delay of the exact `FakeTimeProvider` difference; a `Left` from the callback gives `failed` and an error code on the span; the last retry gives `dead_lettered`; an unknown request type gives `request_type = unknown`; no span or tag contains the content or an error message.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
- Phase 1 merged: SchedulingTelemetryNames, SchedulingExecutionMetrics and the extended SchedulingActivitySource exist.

TASK:
Wire spans and measurements into SchedulerOrchestrator exactly where the outcome is decided (see the Phase 2 tasks) and add SchedulerOrchestratorTelemetryTests.

KEY RULES:
- Time comes from the orchestrator's TimeProvider; production code never reads DateTime.UtcNow.
- The orchestrator never calls SaveChangesAsync; no claim of atomicity.
- A Left is recorded as an outcome and an error code, never swallowed and never turned into an exception.
- Tags: outcome and request type only; the message id is a span attribute.

REFERENCE FILES:
- src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs
- tests/Encina.UnitTests/Messaging/Scheduling/ (existing orchestrator tests for the mocked-store setup)
```

</details>

### Phase 3: Superseded instruments (PR T2)

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Messaging/Diagnostics/MessagingStoreMetrics.cs`: delete the two scheduling counters, `RecordScheduled` and `RecordExecuted` (Design Choice 3, option A).
2. `src/Encina.Messaging/Diagnostics/SchedulingProcessorMetrics.cs`: delete `messages_total` and `RecordBatch`; keep `cycle_duration_seconds`. `src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs`: delete the `RecordBatch` call and keep `RecordCycleDuration` and the log.
3. `src/Encina.Messaging/PublicAPI.Unshipped.txt`: remove lines 733 and 734 (`MessagingStoreMetrics.RecordScheduled` and `RecordExecuted` are public API; leaving them is RS0017 and fails the build), through `mechanical-fixer`.
4. Tests: a grep of `src` and `tests` on `origin/main` (`RecordScheduled`, `RecordExecuted`, `messages_scheduled_total`, `messages_executed_total`, `RecordBatch(`) finds no test that calls the removed members: `MessagingStoreMetricsGuardTests.cs` has no scheduling case, and the only `RecordBatch` caller besides the processor is the outbox's own `OutboxProcessorMetrics.RecordBatch`, which is not touched. The pull request adds `tests/Encina.UnitTests/Messaging/Diagnostics/SchedulingProcessorMetricsTests.cs` (a `MeterListener` asserts that `cycle_duration_seconds` is still recorded and that no `messages_total` instrument is created), and the `.github/coverage-manifest/Encina.Messaging.json` entries through `mechanical-fixer`. T2 is 6 files: the three source files, the new test, `PublicAPI.Unshipped.txt` and the manifest. If the maintainer answers Design Choice 3 with B, this phase becomes "wire `RecordScheduled` and `RecordExecuted` and fix the failure count" instead.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
- The new counter encina.scheduling.messages (Phase 1-2) carries every outcome; the three old instruments are replaced.

TASK:
Delete the superseded instruments and their callers; keep cycle_duration_seconds.

KEY RULES:
- No compatibility alias and no [Obsolete]; grep every caller first and list them in the pull request.
- Build must stay at zero warnings; remove now-unused fields and using directives.

REFERENCE FILES:
- src/Encina.Messaging/Diagnostics/MessagingStoreMetrics.cs
- src/Encina.Messaging/Diagnostics/SchedulingProcessorMetrics.cs
- src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs
```

</details>

### Phase 4: OpenTelemetry package (PR T3)

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.OpenTelemetry/MessagingStores/InstrumentedScheduledMessageStore.cs`: rename the six span names to `encina.scheduling.store.*` (Design Choice 4, option A) and read the source name from one constant; keep `HasListeners` guards.
2. `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs`: add `metrics.AddMeter("Encina.Messaging")` next to line 213 and `builder.AddMeter("Encina.Messaging")` next to line 301 (a no-op if #721 added it).
3. Tests: edit `tests/Encina.UnitTests/OpenTelemetry/MessagingStores/InstrumentedScheduledMessageStoreTests.cs` (new names); extend the shared `SchedulerCycleScenarios` (`tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Scheduling/`) with a cycle that schedules, dispatches and fails one message under an `ActivityListener` and `MeterListener` and asserts the span tree (`process cycle > execute > store.*`) and the measurements, so the scenario runs on the three EF Core databases and the same scenario file is the base of the ADO.NET, Dapper and MongoDB runs where it exists.
4. Documentation through `docs-writer`: the scheduling section of the observability guide (spans, instruments, tags, cardinality note) and `changelog.d/152-scheduling-tracing-metrics.added.md` through `mechanical-fixer`. `Encina.OpenTelemetry.json` manifest entry.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
- Phases 1-3 merged. The orchestrator now owns the operation spans; the decorator spans are store-level children.

TASK:
Rename the decorator spans, subscribe the Encina.Messaging meter and add the integration scenario.

KEY RULES:
- The decorator never swallows a Left; it records the error code on the span and returns the inner result unchanged.
- The scenario asserts real spans and measurements from a real EF Core cycle; no reflection-only tests.

REFERENCE FILES:
- src/Encina.OpenTelemetry/MessagingStores/InstrumentedScheduledMessageStore.cs
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs
- tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Scheduling/SchedulerCycleScenarios.cs
```

</details>

### Phase 5: Cross-cutting integration, the tenant dimension (PR T4, after #739)

<details>
<summary><strong>Tasks</strong></summary>

1. `SchedulingExecutionMetrics` methods gain `string? tenantId` and add the tag only when `TenantTelemetry.ValueFor(tenantId, multiTenancyEnabled)` returns a value; the spans set `encina.tenant_id` through the same helper (`TenantTelemetry`, #2676). `SchedulerOrchestrator` takes `MultiTenancyMarker? multiTenancy = null` as its last optional constructor parameter, after the optional parameters #739 and #735 add (all optional: no existing call site breaks, and the order rule CS1737 is kept by appending last). The public constructor line `src/Encina.Messaging/PublicAPI.Unshipped.txt:870` is edited in this pull request (RS0016/RS0017); #739 (12b) and #749 (PR 6) edit the same line, so whichever merges later rebases it and appends its parameter last. A guard test edit (`SchedulerOrchestratorGuardTests.cs`: a null `multiTenancy` is accepted and valid) goes in the same pull request.
4. T4 is 8 files: `SchedulingExecutionMetrics.cs`, `SchedulingActivitySource.cs`, `SchedulerOrchestrator.cs`, `SchedulerOrchestratorTelemetryTests.cs` (edit), `SchedulingExecutionMetricsTests.cs` (edit), `SchedulerOrchestratorGuardTests.cs` (edit), `PublicAPI.Unshipped.txt` and `Encina.Messaging.json`.
2. The tenant comes from `message.TenantId` on the dispatch path and from the stamped value on the schedule path (#739), never from the ambient context of the background processor.
3. Tests: the telemetry tests add a two-tenant case (tag present and equal to the row's tenant when the marker is registered; no tag when it is not) and a test that an application tenant literally named `default` is tagged as `default` (the reserved id is a value, not a missing tenant).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
- #739 has merged: IScheduledMessage.TenantId exists. #2676 has merged: TenantTelemetry exists in core.

TASK:
Add the tenant dimension to the scheduling spans and instruments.

KEY RULES:
- Tag only when multi-tenancy is on (MultiTenancyMarker registered); otherwise no tenant tag.
- Never read the ambient tenant in the processor; use the row's tenant.
- The new constructor parameter is optional and last.

REFERENCE FILES:
- src/Encina/Diagnostics/TenantTelemetry.cs
- src/Encina.Messaging/Diagnostics/SchedulingExecutionMetrics.cs
- src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs
```

</details>

### Phase 6: Per-file coverage targets (entered in the PR that carries the tests)

<details>
<summary><strong>Tasks</strong></summary>

| File | PR | Flag and target | Justification |
| --- | --- | --- | --- |
| `Diagnostics/SchedulingExecutionMetrics.cs` | T1 | unit 95, guard 40 | A `MeterListener` drives every method and outcome; guard tests reach the string validation through `InternalsVisibleTo`. |
| `Diagnostics/SchedulingTelemetryNames.cs` | T1 | unit 0 | Constants only, no executable code. |
| `Diagnostics/SchedulingActivitySource.cs` | T1, T4 | unit 70, guard 20 (the package defaults, not lowered; the file has no per-file target) | The telemetry tests read every attribute the helper sets. |
| `Scheduling/SchedulerOrchestrator.cs` | T1, T4 | unit 90, guard 30, integration 65 (current, never lowered) | The telemetry paths are unit-tested with a mocked store; the EF Core scenario drives a real cycle. |
| `Diagnostics/MessagingStoreMetrics.cs`, `Diagnostics/SchedulingProcessorMetrics.cs`, `Scheduling/ScheduledMessageProcessor.cs` | T2 | unit 70, guard 20 (the package defaults, not lowered) | Smaller files; remaining members keep their tests and the new `SchedulingProcessorMetricsTests`. |
| `MessagingStores/InstrumentedScheduledMessageStore.cs` | T3 | unit 76, guard 71 (current, not lowered) | The renamed spans are asserted per method. |

Measure with `dotnet test tests\Encina.UnitTests --collect "XPlat Code Coverage" --results-directory artifacts\coverage\UnitTests` (and the guard and integration projects) and `dotnet run --file .github/scripts/coverage-report.cs -- --input artifacts/coverage --output artifacts/coverage-report`. CRAP: the changed orchestrator methods stay at comp 10 or less by extracting the span and metric calls into small private helpers; the worker runs the local crap-gate table before the PR.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
- Targets live in .github/coverage-manifest/Encina.Messaging.json and Encina.OpenTelemetry.json; they never go down.

TASK:
Add the per-file targets of the table above, with one-sentence justifications, in the pull request that carries the tests.

KEY RULES:
- Numbers, not "current": read the manifest and write the number; a target is never lowered.
- Tests execute real code; no reflection-only tests.

REFERENCE FILES:
- .github/coverage-manifest/Encina.Messaging.json
- docs/testing/coverage-measurement-methodology.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Requirement | Key Details |
|----------|-------------|-------------|
| ADR-054 D1, D4 | Trace in the #1164 envelope; no baggage; tenant tag when tenancy is on | No `TraceContext` property; `encina.tenant_id` in T4 |
| ADR-054 D3 | One meter per package; keys in one constants class | `Encina.Messaging` meter; `SchedulingTelemetryNames` |
| ADR-054 D6 | Encina-level spans on every store, no query text | Store spans renamed, never carry SQL or content |
| SPEC-002 AC-044 | Tenant is a span attribute and metric tag with tenancy on | T4 |
| OpenTelemetry metrics guidance | No `_total` in counter names; bounded tag cardinality | Three instruments, fixed outcomes |
| `AGENTS.md` section 3 | `EncinaError.Message` never reaches tags, logs or status | Error code only |
| ADR-062 | `ErrorCategory` of new error codes | No new error code is created |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `SchedulingActivitySource` | `src/Encina.Messaging/Diagnostics/` | Extended; its unused helpers are finally called |
| `SchedulerOrchestrator` | `src/Encina.Messaging/Scheduling/` | The one place where outcome, delay and duration are known |
| `InstrumentedScheduledMessageStore` | `src/Encina.OpenTelemetry/MessagingStores/` | Store-level spans, renamed |
| `SchedulerCycleScenarios` | `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Scheduling/` | End-to-end telemetry scenario |
| `TenantTelemetry` | `src/Encina/Diagnostics/` (created by #2676) | Tenant tag in T4 |
| `SchedulingHealthCheck` | `src/Encina.Messaging/Health/` | Exists; not changed |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` (`MessagingScheduling`, existing 2300-2399) | none | The schedule, execute and failure logs already exist (orchestrator ids 2931-2941 and 2989; processor and store 2300-2306 and 2320-2325); telemetry adds no log |
| New range | none | No block is needed |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production | 9 | 2 new (`SchedulingTelemetryNames`, `SchedulingExecutionMetrics`), 7 modified; 12 touches over the four PRs |
| Tests | 9 | 4 new (metrics unit, metrics guard, orchestrator telemetry, processor metrics), 5 edited (including the two guard and unit edits of T4); 12 touches |
| Coverage manifests | 2 | `Encina.Messaging.json`, `Encina.OpenTelemetry.json`; 4 touches |
| PublicAPI | 1 | `Encina.Messaging` `PublicAPI.Unshipped.txt`, in T2 (two removed lines) and T4 (the orchestrator constructor line); 2 touches |
| Documentation and changelog | 2 | Guide section and fragment, in T3 |
| **Total** | **21** | 29 touches in four pull requests of 8, 6, 7 and 8 files |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no compatibility layers; time from TimeProvider; Shouldly for assertions.
- Issue #152: scheduling already has dead or unwired telemetry; this plan wires it and replaces what is dead.

IMPLEMENTATION OVERVIEW:
T1: SchedulingTelemetryNames, SchedulingExecutionMetrics (meter Encina.Messaging; counter encina.scheduling.messages with outcome; histograms delay and duration), orchestrator spans and measurements.
T2: delete the three superseded instruments. T3: rename the decorator spans, AddMeter, EF Core scenario, docs. T4: tenant dimension after #739.

KEY PATTERNS:
- No TraceContext property (ADR-054 D1); the producer link is #176.
- No _total suffix; tags are outcome and request type; the message id is a span attribute only.
- Error code only in any tag, log or status; the orchestrator never calls SaveChangesAsync.

REFERENCE FILES:
- src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs
- src/Encina.Messaging/Diagnostics/SchedulingActivitySource.cs
- src/Encina.OpenTelemetry/MessagingStores/InstrumentedScheduledMessageStore.cs
- tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Scheduling/SchedulerCycleScenarios.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Telemetry is real time and not cacheable |
| 2 | OpenTelemetry | ✅ | This is the feature: Phases 1 to 4 |
| 3 | Structured Logging | ❌ | The schedule, execute and failure logs already exist (ids 2931-2941, 2989); no new log is added |
| 4 | Health Checks | ❌ | `SchedulingHealthCheck` exists; lag-based health from the new histograms is not planned |
| 5 | Validation | ✅ | Phase 1: guard tests validate the string inputs of the new internal recorder |
| 6 | Resilience | ❌ | Observational, no external call |
| 7 | Distributed Locks | ❌ | No shared state |
| 8 | Transactions | ❌ | No store operation is added; the telemetry makes no atomicity claim |
| 9 | Idempotency | ❌ | Telemetry is append-only |
| 10 | Multi-Tenancy | ✅ | Phase 5 (T4): the tenant tag from the row, after #739 |
| 11 | Module Isolation | ⏭️ | A `ModuleId` tag belongs with #747's column; no issue needed beyond it |
| 12 | Audit Trail | ⏭️ | Audit of background processing is #749 |

---

## Prerequisites & Dependencies

| Item | Relation | Note |
|------|----------|------|
| #176, #132, #1164 | Siblings | The producer link and the persisted trace; independent of this plan, ordered by their own PRs |
| #177 | Soft | The `messaging.*` constants class; `SchedulingTelemetryNames` uses it once merged |
| #721 | Soft | Creates the `Encina.Messaging` meter registration and the store-span naming; T3 aligns |
| #739 | Hard for T4 | `TenantId` on the row |
| #2676 | Hard for T4 | `TenantTelemetry` |
| #159 | Sibling | Owns the pending count and gauge (Design Choice 6) |
| #906 | Sibling | The observability guide documents the tenant-tag cardinality risk |

---

## Next Steps

1. Review this plan; the Dilemmas (choices 2, 3, 4 and 6: flags, the fate of the old instruments, the span layers, the pending gauge) are answered by the maintainer when work starts; the answers are then recorded in a `## Maintainer Decisions` section added at that time.
2. Implement T1 to T3 in order; T4 after #739.
3. Link the plan from #152.
