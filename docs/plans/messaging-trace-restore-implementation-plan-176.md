# Implementation Plan: Capture and Restore of the Trace Context in the Messaging Stores

> **Issue**: [#176](https://github.com/dlrivada/Encina/issues/176)
> **Type**: Feature (observability, messaging glue)
> **Related**: ADR-054 (D1 the trace lives in the #1164 envelope, D2 child or link rule, D4 no baggage, D5 scope), #1164 (the persisted envelope, owns the schema on 10 providers), #132 (propagation API and `TraceContextConsumer`), #186 (`RequestTraceContext` on `IRequestContext`), #725 (transports), #175 (correlation and causation), #177 (attribute constants), #1790 (dead messaging activity sources), ADR-062
> **Complexity**: Low-Medium (all schema work belongs to #1164 and all API work to #132; what is left is two small call sites and the rule for which span is captured)
> **Estimated Scope**: ~140 lines of production code + ~420 lines of tests, in two pull requests of at most 15 files and 900 lines each

---

## Summary

### What exists today (read on 2026-11-20, main `098caa920`)

- The issue's premise holds: no persisted message carries a trace context. `IOutboxMessage` (`src/Encina.Messaging/Outbox/IOutboxMessage.cs`) has `Id`, `NotificationType`, `Content`, timestamps, retry fields and `ErrorMessage`, no headers or envelope; `IInboxMessage`, `IScheduledMessage` and `ISagaState` have none either. A search of `src` for `traceparent`, `TextMapPropagator` and `Baggage` finds nothing.
- The classes the issue names do not exist: there is no `OutboxPostProcessor`. The outbox is written by `TransactionalOutboxPipelineBehavior` (which starts `OutboxActivitySource.StartEnqueue`, `src/Encina.Messaging/Outbox/TransactionalOutboxPipelineBehavior.cs:173`), read and published by `OutboxProcessorBase.PublishAsync` (`src/Encina.Messaging/Outbox/OutboxProcessorBase.cs:171-185`, `encina.Publish(typedNotification, cancellationToken)`), and scheduled messages are dispatched by `ScheduledMessageProcessor` (line 151: `(msg, type, req, ct) => dispatcher.DispatchAsync(type, req, ct)`). The inbox is `InboxPipelineBehavior` with `InboxOrchestrator`, and it never re-dispatches a stored message (it caches the response).
- #1164 has no plan yet and is open; ADR-054 D1 assigns it the envelope that "carries `traceparent` and `tracestate`; the dispatcher rebuilds the trace from it". The plans of #186 (`RequestTraceContext`, `IRequestContext.TraceContext`) and #132 (`IContextPropagator`, `TraceContextConsumer.StartConsumer`, `TraceDispatchKind`) are written but not on `main`; this plan names them by issue number.
- Messaging EventIds: the registry reserves Messaging 2977-2979 for #176 (`artifacts/decisions/plan-batch/eventid-blocks.md`); 2971-2976 belong to other plans and 2989-2998 are used.

### What the issue proposes and what changed since

The issue proposes `BaggagePropagator` (inject and extract with `Baggage.Current`), a `Headers` string on three message interfaces, injection in `OutboxPostProcessor` and extraction in `InboxPipelineBehavior`. ADR-054 (2026-10-22) changed it: D4 says Encina never propagates or stores baggage, D1 puts the trace in the #1164 envelope instead of a `Headers` column, and #132 owns the propagation API.

### What this plan delivers

Only the glue that neither #1164 nor #132 owns:

- `MessageTraceCapture.Capture(IRequestContext)` (internal, `Encina.Messaging`): the value #1164's envelope builder stores: the trace context of `Activity.Current` at the moment the row is written (the span that wrote it) when valid, otherwise the request's snapshot `context.TraceContext`.
- `MessagingTraceDispatch.StartConsumer(source, name, restoredContext, kind)` (internal): calls `TraceContextConsumer.StartConsumer` with the right `TraceDispatchKind` and is used by `OutboxProcessorBase` (`Publish`) and `ScheduledMessageProcessor` (`Scheduled`), so a processor span starts a new trace linked to the producer span (ADR-054 D2).
- Two log events for a stored trace that is missing or invalid.
- Shared pieces and owner: #1164 owns the envelope fields and their persistence on the 10 providers; #132 owns the propagator and the consumer rule; #186 owns `RequestTraceContext`; this plan owns the capture choice and the two processor call sites.
- Affected packages: `Encina.Messaging`. Provider category: database (10) for the persisted data, which this plan does not touch; the call sites sit above the stores, so no provider code changes here.
- Not delivered, with the reason: `BaggagePropagator`, baggage and `Headers` on message interfaces (ADR-054 D1 and D4); trace columns or store changes (#1164); transport injection and extraction (#725 and #132); extraction in `InboxPipelineBehavior` (the inbox never re-dispatches, so a restore has no call site; the transport extraction that feeds the inbox row is #725); saga steps and delayed retry and dead-letter replay restore (#1164 rebuilds their context, and the call sites are added by the issue file `artifacts/issues/trace-restore-saga-retry-replay.md`).

---

## Design Choices

<details>
<summary><strong>1. Which span is captured at the write</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The current activity at the write (the enqueue or schedule span), falling back to the request snapshot** | The consumer span links to the span that wrote the row: precise in a trace view | One more rule than "copy the request's trace" |
| **B) Always `context.TraceContext` (the request's snapshot, #186)** | No rule; identical to what #1164 persists for other fields | The link points at the request root, not at the write |

### Chosen Option: **A — Current activity first** (recommended, pending the maintainer)

### Rationale

- A wins when the write happens inside a dedicated span (the outbox enqueue has one); B wins when no listener exists at the write (then `Activity.Current` is null and A falls back to B anyway). Both give the same trace id; only the span id of the link differs.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Consumer span relation per store</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Outbox notifications use `Publish` and scheduled messages use `Scheduled`: a new trace with an `ActivityLink` to the captured span** | Exactly the ADR-054 D2 table | A reader follows two traces |
| **B) A child of the producer for everything** | One trace | Contradicts D2 for fan-out and delayed messages |

### Chosen Option: **A — The D2 table**

### Rationale

- The outbox publishes `INotification` (fan-out) and scheduled messages are delayed: both are in the "new trace with a link" row of D2; no option is allowed to switch it.

**Class:** Settled
**Settled by:** ADR-054 D2 (`Publish`, scheduled or delayed messages and batches start a new trace with an `ActivityLink`; "no configuration option is added"); option B contradicts it

</details>

<details>
<summary><strong>3. Baggage and the issue's `Headers` column</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) None: the trace is two fields of the #1164 envelope; baggage is never propagated or stored** | Complies; no schema change here | The issue's acceptance criteria that name `BaggagePropagator` and `Headers` are replaced by the ADR text |
| **B) A `Headers` column and `BaggagePropagator` as the issue states** | Literal | Contradicts D1 and D4 (baggage reaches brokers and stores in plaintext) |

### Chosen Option: **A — Follow the ADR**

### Rationale

- ADR-054 D4 and D5: "#132 is re-scoped against D4"; D1: no dedicated columns.

**Class:** Settled
**Settled by:** ADR-054 D1 and D4; option B contradicts both

</details>

<details>
<summary><strong>4. Behavior when the stored trace is absent or invalid</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Start a plain consumer span with no parent and no link, log a reason code, process the message** | Telemetry never blocks delivery | A broken trace for that message |
| **B) Fail the message** | Strict | Telemetry data deciding delivery of a business message |

### Chosen Option: **A — Process and log**

### Rationale

- Trace data is metadata; `TraceContextConsumer` of #132 already returns a plain span for an invalid context, and `AGENTS.md` section 3 forbids swallowing a `Left` but this is not an error return: nothing failed.

**Class:** Settled
**Settled by:** #132 plan, Design Choice 2 and register row C1 (an invalid context gives a plain span); `AGENTS.md` section 3 (logs carry codes only)

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | No new `EncinaError` code, so no ADR-062 `ErrorCategory`: capture and restore never return an error | Settled | ADR-062 |
| C2 | Events 2977 (stored trace context missing, Debug) and 2978 (stored trace context invalid, Warning, reason code only, never the value); 2979 stays unused and is returned to the registry | Settled | `AGENTS.md` section 7 (packed, registered range); registry reservation |
| C3 | The tenant telemetry tag required by ADR-054 D4 on the consumer span is added by the processors through the helper of #2676 when multi-tenancy is on; this plan adds none and creates no tenant-qualified id, so the reserved `default` tenant segment does not apply | Settled | ADR-054 D4, #2676 |
| C4 | Fail-closed gates are not involved: restoring a trace grants nothing and denies nothing | Settled | `AGENTS.md` section 3 (gates fail closed apply to compliance and security gates) |
| C5 | `Activity.Current` after `StartConsumer` is the consumer span for the dispatch; `Encina.Send` spans started by `Publish` become its children; the span is disposed after the dispatch completes, in a `using` | Trivial | `OutboxProcessorBase.PublishAsync` structure |
| C6 | Public names: none (both helpers internal) | Trivial | Smallest surface |

---

## Implementation Phases

### Phase 1: Capture (pull request R1, after #1164 defines the envelope)

> **Goal**: The envelope builder has one function that chooses the span to capture.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Messaging/Diagnostics/MessageTraceCapture.cs` (`internal static RequestTraceContext Capture(IRequestContext context)`: `RequestTraceContext.FromActivity(Activity.Current)` when valid, else `context.TraceContext`; null context throws) and `src/Encina.Messaging/Diagnostics/TraceRestoreLog.cs` (`[LoggerMessage]` 2977 missing and 2978 invalid; "Event IDs: 2977-2978 (see EventIdRanges.Messaging)"; no new range).
2. **Modify** `.github/coverage-manifest/Encina.Messaging.json`; add `changelog.d/176-message-trace-capture.added.md` through `mechanical-fixer`; the call from #1164's envelope builder is part of #1164's pull request, which names this function.
3. Tests (new): `tests/Encina.UnitTests/Messaging/Diagnostics/MessageTraceCaptureTests.cs` (with an `ActivityListener`: inside an activity the capture equals that activity's `traceparent`; with no activity it equals the context snapshot; with neither it is `default`; the `tracestate` is carried), `tests/Encina.UnitTests/Messaging/Diagnostics/TraceRestoreLogTests.cs` (the two events log the reason code and never a header value).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #176, plan docs/plans/messaging-trace-restore-implementation-plan-176.md, phase 1.
#186 (RequestTraceContext) and #132 (propagation API) are merged; the #1164 envelope exists. ADR-054 D1: the trace lives in the envelope, D4: no baggage.

TASK:
1. Create MessageTraceCapture.Capture(IRequestContext) and TraceRestoreLog (EventIds 2977-2978, Messaging range).
2. Tests with a real ActivityListener; manifest and changelog through mechanical-fixer.

KEY RULES:
- No baggage, no Headers column. Log reason codes only. No EncinaError is created.

REFERENCE FILES:
- src/Encina.Messaging/Outbox/TransactionalOutboxPipelineBehavior.cs, src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

### Phase 2: Restore in the Processors (pull request R2)

> **Goal**: Outbox and scheduled processor spans link to the producer.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Messaging/Diagnostics/MessagingTraceDispatch.cs` (`internal static Activity? StartConsumer(ActivitySource source, string name, RequestTraceContext stored, TraceDispatchKind kind, ILogger logger)`: logs 2977 or 2978 for a missing or invalid value and delegates to `TraceContextConsumer.StartConsumer`).
2. **Modify** `src/Encina.Messaging/Outbox/OutboxProcessorBase.cs` (`PublishAsync`: `using var span = MessagingTraceDispatch.StartConsumer(OutboxActivitySource.ActivitySource, "encina.outbox.publish", restoredContext, TraceDispatchKind.Publish, logger)` around `encina.Publish(...)`, where `restoredContext.TraceContext` is the value #1164 restores) and `src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs` (the dispatch lambda at line 151, kind `Scheduled`, the span name `encina.scheduling.dispatch`). Grep every override and `new` site of the changed private methods on origin/main (the call sites are private, so none outside the two files).
3. **Modify** `.github/coverage-manifest/Encina.Messaging.json`; changelog `176-message-trace-restore.added.md` through `mechanical-fixer`.
4. Tests (new): `tests/Encina.UnitTests/Messaging/Outbox/OutboxTraceRestoreTests.cs` (a stored valid context gives a consumer span in a new trace with one link to the captured span id; an absent or invalid value gives a plain span and the log event with the code only; the notification is still published), `tests/Encina.UnitTests/Messaging/Scheduling/ScheduledTraceRestoreTests.cs` (same for `Scheduled`), `tests/Encina.UnitTests/Messaging/Diagnostics/MessagingTraceDispatchTests.cs`. Guard target: `Encina.Messaging` grants `InternalsVisibleTo` to the unit, integration, contract and property projects; confirm whether `Encina.GuardTests` is listed before planning guard tests of these internal types (if not, the guard target is 0 with that reason).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #176 plan docs/plans/messaging-trace-restore-implementation-plan-176.md, phase 2. Phase 1, #132 and #1164 merged.

TASK:
1. Create MessagingTraceDispatch (delegates to TraceContextConsumer.StartConsumer, logs missing or invalid values).
2. Use it in OutboxProcessorBase.PublishAsync (Publish) and in the ScheduledMessageProcessor dispatch (Scheduled), in a using around the dispatch.
3. Tests with a real ActivityListener asserting a new trace id and one link; manifest and changelog through mechanical-fixer.

KEY RULES:
- ADR-054 D2: new trace plus ActivityLink for fan-out and delayed messages; no option. Never throw for a bad stored value; never fail the message because of telemetry.
- Log codes only, never the stored header.

REFERENCE FILES:
- src/Encina.Messaging/Outbox/OutboxProcessorBase.cs, src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs
- docs/plans/trace-context-propagation-implementation-plan-132.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| W3C Trace Context | `traceparent`, `tracestate` | The stored values |
| OpenTelemetry messaging conventions | Consumer spans link to the producer for fan-out and batches | Design Choice 2 |
| ADR-054 | D1 envelope, D2 rule, D4 no baggage, D5 scope | Scope and behavior |
| ADR-062 | Error category contract | None created (row C1) |
| `AGENTS.md` section 7 | EventIds packed in a registered range | 2977-2978 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `OutboxActivitySource` | `src/Encina.Messaging/Diagnostics/OutboxActivitySource.cs` | Source for the consumer span |
| `SchedulingActivitySource` | `src/Encina.Messaging/Diagnostics/SchedulingActivitySource.cs` | Same |
| `OutboxProcessorBase` | `src/Encina.Messaging/Outbox/OutboxProcessorBase.cs` | Outbox call site |
| `ScheduledMessageProcessor` | `src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs` | Scheduled call site |
| `TraceContextConsumer`, `RequestTraceContext` | plans of #132 and #186 | The rule and the value |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` | 2977-2978 (inside `EventIdRanges.Messaging` 2800-2999) | 2977 stored trace missing, 2978 invalid (reason code); 2979 returned to the registry; no new range and no id of the block 5970-5999 |

### Estimated File Count

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| R1 | Capture | 2 | 0 | 2 | 0 | 1 | 1 (changelog) | 6 | ~330 |
| R2 | Restore in processors | 1 | 2 (the two processors) | 3 | 0 | 1 | 1 | 8 | ~560 |
| **Total** | | **3** | **2** | **5** | **0** | **2** | **2** | **14** | **~890** |

The columns sum (3 + 2 + 5 + 0 + 2 + 2 = 14; 6 + 8 = 14). The pull request of this plan is one file.

| File | Unit | Guard | Contract | Property | Justification |
|------|------|-------|----------|----------|---------------|
| `Diagnostics/MessageTraceCapture.cs` | 100 | 0 | 0 | 0 | Three branches reachable with a listener; internal, and guard tests need `InternalsVisibleTo` for `Encina.GuardTests` (to be confirmed, Phase 2 task 4) |
| `Diagnostics/MessagingTraceDispatch.cs` | 95 | 0 | 0 | 0 | Valid, missing and invalid paths run through both processors; same guard reason |
| `Diagnostics/TraceRestoreLog.cs` | 0 | 0 | 0 | 0 | Generated `[LoggerMessage]` code, executed through the dispatch tests |
| `Outbox/OutboxProcessorBase.cs`, `Scheduling/ScheduledMessageProcessor.cs` | current target kept | current target kept | 0 | 0 | The changed lines run in the restore tests |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #176 - trace context capture and restore for the messaging stores - in two small pull requests.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0; edit source only with Edit/Write; manifests and changelog through mechanical-fixer.
- ADR-054: the trace (traceparent, tracestate) lives in the #1164 envelope; never baggage; Publish and scheduled consumer spans are a new trace with a link.

IMPLEMENTATION OVERVIEW:
1. R1: MessageTraceCapture (current activity first, else the request snapshot) and TraceRestoreLog (2977-2978).
2. R2: MessagingTraceDispatch used by OutboxProcessorBase (Publish) and ScheduledMessageProcessor (Scheduled).

KEY PATTERNS:
- Real ActivityListener tests; never fail a message for a bad stored trace; log reason codes only.

REFERENCE FILES:
- docs/plans/messaging-trace-restore-implementation-plan-176.md, docs/plans/trace-context-propagation-implementation-plan-132.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Per-message data |
| 2 | OpenTelemetry | ✅ Phases 1-2 | This is trace restore for messaging |
| 3 | Structured Logging | ✅ Phases 1-2 | Events 2977-2978 |
| 4 | Health Checks | ❌ N/A | No dependency |
| 5 | Validation | ✅ Phase 2 | Stored trace is validated by `TryParse` of #186; invalid is ignored |
| 6 | Resilience | ❌ N/A | No external call |
| 7 | Distributed Locks | ❌ N/A | No shared state |
| 8 | Transactions | ⏭️ #1164 | The trace is persisted with the row inside the envelope write, which is #1164's transaction; this plan persists nothing |
| 9 | Idempotency | ❌ N/A | Trace ids are not part of any key |
| 10 | Multi-Tenancy | ⏭️ #2676 | Tenant tag on the consumer span through the helper when multi-tenancy is on (row C3) |
| 11 | Module Isolation | ❌ N/A | Crosses modules by design |
| 12 | Audit Trail | ❌ N/A | Infrastructure metadata |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| #1164 envelope (carries `traceparent` and `tracestate`) | Open, no plan | R1 and R2 cannot restore before it; this plan names it by issue number |
| #132 (`TraceContextConsumer`) and #186 (`RequestTraceContext`) | Plans written | R2 compiles only after both merge |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Transports inject and extract | #725 | Feeds inbox rows |
| Dead messaging sources | #1790 | Settles which activity sources survive |
| Tenant attribute | #2676 | Row C3 |

---

## Next Steps

1. Link this plan from #176 and ask the maintainer to answer Design Choice 1 when work starts.
2. Implement R1 and R2 after #1164, #186 and #132; amend the acceptance criteria of #176 to the ADR text (Design Choice 3) when it is picked up.
