# Implementation Plan: W3C Trace Context Propagation API (`IContextPropagator`)

> **Issue**: [#132](https://github.com/dlrivada/Encina/issues/132)
> **Type**: Feature (observability core)
> **Related**: ADR-054 (D2 parent or link rule, D3 wire format, D4 no baggage, D5 release scope), #186 (owns `RequestTraceContext`, which this API carries), #176 (store-side use), #725 (the 10 transports adopt this API), #184 (request tracing behavior, sibling plan, not duplicated here), #265 (overlapping issue the ADR reconciles with #132 and #725), #1164 (envelope), ADR-062
> **Complexity**: Low-Medium (a small, transport-agnostic API and one rule, no persistence; the work is making it correct for 10 different header models)
> **Estimated Scope**: ~330 lines of production code + ~650 lines of tests + 2 documentation files, in three pull requests of at most 15 files and 900 lines each

---

## Summary

### What exists today (read on 2026-11-18, main `f484cdf4f`)

- No code propagates a trace context: a search of `src` for `traceparent`, `TraceParent`, `TextMapPropagator`, `Propagators.` and `Baggage` finds nothing. ADR-054 (Context) records the same finding: no persisted message carries a trace context and none of the 10 transports injects or extracts W3C headers.
- Core has `EncinaDiagnostics.ActivitySource` (`"Encina"`, `src/Encina/Diagnostics/EncinaDiagnostics.cs:10`) and the command and query activity behaviors (`CommandActivityPipelineBehavior`, `QueryActivityPipelineBehavior`). The messaging stores each own an `ActivitySource` (`OutboxActivitySource`, `InboxActivitySource`, `SchedulingActivitySource`, `SagaActivitySource` in `src/Encina.Messaging/Diagnostics/`). No consumer-side span is parented or linked to a producer.
- Core references no OpenTelemetry package (`src/Encina/Encina.csproj`); `Encina.OpenTelemetry` does. `System.Diagnostics` already offers `ActivityContext.TryParse`, `ActivityLink` and `ActivitySource.StartActivity(name, kind, parentContext, tags, links)`.
- `IRequestContext` carries no trace context today; #186 adds `RequestTraceContext` (W3C `traceparent` and `tracestate`).
- The core EventId range `EventIdRanges.Core = (100, 199)` has 100-176 used on main; 177-183 are claimed by the plan of #2382, so this plan uses 184-185.

### What the issue proposes and what changed since

The issue (2026-era text) proposes `IDistributedContext`, `IDistributedContextAccessor`, `IBaggageManager`, an options class with baggage limits, `ActivityPipelineBehavior<,>` and trace fields on `OutboxMessage`. ADR-054 (2026-10-22) decided otherwise for most of it: D4 says Encina never propagates or stores baggage ("#132 is re-scoped against D4"), D1 puts the persisted trace in the #1164 envelope instead of message columns, D2 fixes the parent or link rule per operation with no switch, and D5 keeps the context-propagation API of #132 in release 0.14.0.

### What this plan delivers

The API every transport and store uses to carry the context, in core and with no new package:

- `IContextPropagator` (inject and extract `traceparent` and `tracestate` through a caller-supplied setter and getter, so each transport uses its own header type) and the header-name constants.
- `TraceContextConsumer`, the single implementation of the ADR-054 D2 rule: a `Send` consumer span is a child of the producer span; `Publish`, scheduled and batch consumer spans start a new trace with an `ActivityLink`.
- Registration in `AddEncina` and two log events.

- Shared pieces and owner: `RequestTraceContext` is #186's; the envelope fields and the store-side calls are #176's; the per-transport adoption is #725's; the attribute constants are #177's.
- Affected packages: `Encina` (core). `Encina.OpenTelemetry`, `Encina.Messaging` and the transports consume it in other issues.
- Provider category: Messaging transport (10 existing, 6 planned, `AGENTS.md` section 5). The API is transport-agnostic; the matrix in Research lists how each transport carries the two headers.
- Not delivered, with the reason: `IBaggageManager`, baggage options, `IDistributedContext` and `IDistributedContextAccessor` (ADR-054 D4 and D5; trace fields are on `IRequestContext` through #186, and a second context interface is rejected by #186's own Alternatives); `ActivityPipelineBehavior<,>` (the open issue #184 owns request span creation, and the command and query behaviors exist); trace columns on `OutboxMessage` (ADR-054 D1, #1164 and #176).

---

## Design Choices

<details>
<summary><strong>1. How a carrier is accessed: a dictionary or a caller-supplied setter and getter</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Inject(context, IDictionary<string, string> carrier)` and `Extract(IReadOnlyDictionary<string, string> carrier)` (the issue's text)** | Simple; matches a JSON headers column | Every transport must copy its native headers to and from a string dictionary (Kafka `Headers` hold `byte[]`, RabbitMQ properties hold `object`, gRPC `Metadata` is a collection, SQS and Service Bus use typed attribute maps), an allocation per message on a hot path |
| **B) `Inject<TCarrier>(RequestTraceContext, TCarrier, Action<TCarrier, string, string>)` and `Extract<TCarrier>(TCarrier, Func<TCarrier, string, string?>)`** | No copy; one API for all 10 transports and the stores; the same shape as the OpenTelemetry propagator | A generic method with two delegates is slightly less obvious than a dictionary |

### Chosen Option: **B — Setter and getter delegates** (recommended, pending the maintainer)

### Rationale

- B wins on the 10 transports of ADR-054 D5 ("all 10 transports are coherent"): each passes a `static` lambda over its own header type. A wins if the only carriers were string maps (the stores of #176 and a JSON column).
- `InjectCurrent` (an extension) covers the common case of injecting the span current at the write.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Consumer span relation: one rule in one place</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `TraceContextConsumer.StartConsumer(source, name, incoming, TraceDispatchKind)` applying ADR-054 D2: `Send` is a child, `Publish`, `Scheduled` and `Batch` start a new trace with an `ActivityLink`** | One implementation for all transports and stores; testable once | A new public static class |
| **B) Each transport and store implements the rule** | No new public API | Ten diverging copies; the rule drifts (the failure ADR-054 was written to prevent) |
| **C) A configuration switch per message or endpoint** | Flexible | Rejected by ADR-054 D2: "No configuration option is added" |

### Chosen Option: **A — One helper**

### Rationale

- ADR-054 D2 fixes the table and says the rule is identical on all 10 transports; only a single implementation can guarantee that.
- The helper returns `null` when the source has no listener (`HasListeners()`), so it costs nothing without a listener (ADR-054 settled point 7).

**Class:** Settled
**Settled by:** ADR-054 D2 (the per-operation table, "identical on all 10 transports", "no configuration option") and settled point 7; only A satisfies all three

</details>

<details>
<summary><strong>3. Whether propagation can be switched off</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Always on: inject whenever a valid trace context exists, no option** | Nothing to configure or test; the trace is never broken by a forgotten setting | A deployment that must not let trace ids leave the process boundary cannot stop it |
| **B) `PropagateTraceContext` option (the issue's `config.Context.PropagateTraceContext`)** | The deployment above can opt out | An option and a code path on every transport; ADR-054 D2 already refuses a switch for the relation rule |

### Chosen Option: **A — Always on** (recommended, pending the maintainer)

### Rationale

- A wins in the normal case: a trace id is not personal data and ADR-054 D4 removed the only data-minimisation concern (baggage). B wins for a deployment whose policy forbids sending any correlation identifier to a third-party broker; it would be one boolean read in `Inject` (a registration-time option, not a per-message one).
- The recommendation follows ADR-054 D2's refusal of switches and `AGENTS.md` section 1 (every feature opt-in is about features, and this one only transports metadata the host already created).

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Log correlation of trace ids</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No Encina option; the application enables the platform setting (`ActivityTrackingOptions` with `TraceId` and `SpanId`) of `Microsoft.Extensions.Logging`** | Standard, works for every log source, no code | The application must enable it |
| **B) An `EnrichLogsWithTraceContext` option that adds a log scope per request (the issue's text)** | One switch in Encina | An allocation per request for a feature the logging stack already provides |

### Chosen Option: **A — The platform setting**

### Rationale

- The `ILogger` scope carrying `TraceId`, `SpanId` and `ParentId` is a documented feature of `LoggerFactoryOptions.ActivityTrackingOptions`; Encina's logs are written inside the request activity, so they already benefit.
- The documentation page of Phase 3 says how to enable it.

**Class:** Trivial
**Settled by:** `Microsoft.Extensions.Logging` `LoggerFactoryOptions.ActivityTrackingOptions` (the documented activity-tracking log scope)

</details>

<details>
<summary><strong>5. Items of the issue that are not built</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Do not build `IBaggageManager`, baggage options, `IDistributedContext`, `IDistributedContextAccessor.CreateChild` and trace columns on messages** | Follows ADR-054 D1, D4 and D5 and #186 | The issue's acceptance criteria that name them are met by the ADR decision instead (the maintainer amends the issue) |
| **B) Build them as the issue states** | Literal | Contradicts D4 (baggage is never propagated or stored) and D1 (no message columns) |

### Chosen Option: **A — Follow the ADR**

### Rationale

- ADR-054 D5: "#132 is re-scoped against D4: Encina neither propagates nor persists baggage". An application's own baggage is left untouched (D4).
- Maintainer action: amend the acceptance criteria of #132 to the ADR text (the plan never comments on an issue).

**Class:** Settled
**Settled by:** ADR-054 D4 and D5 ("#132 is re-scoped against D4") and D1; option B contradicts all three

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | `Extract` never throws: a missing, malformed or all-zero `traceparent` returns `default(RequestTraceContext)` and logs event 184 with a reason code (never the value); an invalid `tracestate` alone keeps the trace and drops the state, event 185 | Settled | `AGENTS.md` section 3 (logs carry codes, never values; a processor must not fail on foreign input) and #186 Design Choice 4 |
| C2 | No new `EncinaError` code: parsing foreign input is not a business operation that returns `Either`, so there is no ADR-062 `ErrorCategory` to assign | Settled | ADR-062 (a category is chosen where an `EncinaError` is created; none is created) |
| C3 | `Inject` writes nothing for an invalid or absent context, and never writes `baggage` | Settled | ADR-054 D4 |
| C4 | No tenant tag and no tenant-qualified id: the API carries only `traceparent` and `tracestate`; tenant telemetry tags are emitted where spans are created (ADR-054 D4); the reserved `default` tenant segment does not apply | Settled | ADR-054 D4 |
| C5 | `ContextPropagator` is a singleton with an `ILogger`; it holds no per-request state, so no scoped service is injected into a singleton | Settled | `AGENTS.md` section 3 (Registration completeness) |
| C6 | Public names: `IContextPropagator`, `TraceContextHeaders`, `TraceContextConsumer`, `TraceDispatchKind { Send, Publish, Scheduled, Batch }` in namespace `Encina.Tracing` | Dilemma (recommended, pending the maintainer) | public surface; alternatives are the namespace `Encina` or splitting `Batch` into a flag |

---

## Implementation Phases

### Phase 1: Propagator, Headers and Registration (pull request P1)

> **Goal**: Carry `traceparent` and `tracestate` through any carrier.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina/Tracing/IContextPropagator.cs` (`void Inject<TCarrier>(RequestTraceContext context, TCarrier carrier, Action<TCarrier, string, string> setter)`; `RequestTraceContext Extract<TCarrier>(TCarrier carrier, Func<TCarrier, string, string?> getter)`) and the constants `TraceContextHeaders.TraceParent = "traceparent"` and `TraceState = "tracestate"` in the same file.
2. **Create** `src/Encina/Tracing/ContextPropagator.cs` (`internal sealed`, constructor `(ILogger<ContextPropagator> logger)`; `Extract` reads both headers through the getter and validates with `RequestTraceContext.TryParse`; `Inject` skips an invalid context and never writes `baggage`), `src/Encina/Tracing/ContextPropagatorExtensions.cs` (`InjectCurrent<TCarrier>(this IContextPropagator, TCarrier, setter)` = `Inject(RequestTraceContext.FromActivity(Activity.Current), ...)`) and `src/Encina/Tracing/TraceContextLog.cs` (`[LoggerMessage]` EventIds 184 invalid `traceparent` ignored with a reason code, 185 `tracestate` dropped; XML text "Event IDs: 184-185 (see EventIdRanges.Core)").
3. **Modify** `src/Encina/Core/ServiceCollectionExtensions.cs` (`AddEncina`, near `:33,43`): `services.TryAddSingleton<IContextPropagator, ContextPropagator>()`.
4. **Modify** `src/Encina/PublicAPI.Unshipped.txt`, `.github/coverage-manifest/Encina.json` and add `changelog.d/132-trace-context-propagator.added.md` through `mechanical-fixer`.
5. Tests (new files): `tests/Encina.UnitTests/Tracing/ContextPropagatorTests.cs` (round trip through a dictionary and through a `List<KeyValuePair<string, byte[]>>`-style carrier; an invalid, all-zero, version `ff` and oversize `traceparent`; a null header; invalid `tracestate` alone; no `baggage` header is ever written even when `Baggage.Current`-style data exists on the activity); `tests/Encina.GuardTests/Tracing/ContextPropagatorGuardTests.cs` (null carrier, null setter, null getter, null logger; an invalid context is a valid no-op, not an exception); `tests/Encina.PropertyTests/Tracing/ContextPropagatorProperties.cs` (FsCheck: `Extract(Inject(x)) == x` for every valid context); `tests/Encina.ContractTests/Tracing/ContextPropagatorContractTests.cs` (the real implementation keeps the `IContextPropagator` contract: `Extract` of an empty carrier is `default`, `Inject` of `default` writes no header); `tests/Encina.UnitTests/Tracing/ContextPropagatorRegistrationTests.cs` (a provider built with `ValidateOnBuild` and `ValidateScopes` resolves `IContextPropagator` after `AddEncina`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #132, plan docs/plans/trace-context-propagation-implementation-plan-132.md,
phase 1. RequestTraceContext (readonly record struct with TraceParent and TraceState) comes from the plan of #186 and
must be merged first. ADR-054 D3: traceparent and tracestate are the only propagation fields; D4: never baggage.

TASK:
1. Create IContextPropagator (generic setter/getter), TraceContextHeaders, ContextPropagator (internal sealed, logging
   184-185), ContextPropagatorExtensions.InjectCurrent and TraceContextLog under src/Encina/Tracing/.
2. Register IContextPropagator with TryAddSingleton in AddEncina.
3. Tests: unit, guard, property (round trip), contract and the DI test; PublicAPI, manifest and changelog through
   mechanical-fixer.

KEY RULES:
- Extract never throws; log a reason code, never the header value (AGENTS.md section 3).
- EventIds 184-185 of EventIdRanges.Core, packed without gaps; register nothing new.
- No baggage, no option, no new package. A scoped service is never injected into the singleton.

REFERENCE FILES:
- src/Encina/Core/ServiceCollectionExtensions.cs, src/Encina/Diagnostics/EventIdRanges.cs
- src/Encina/Core/RequestTraceContext.cs (from #186)
```

</details>

---

### Phase 2: Consumer Span Rule (pull request P2)

> **Goal**: One implementation of the ADR-054 D2 parent or link rule.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina/Tracing/TraceContextConsumer.cs` with the enum `TraceDispatchKind { Send, Publish, Scheduled, Batch }` and `public static Activity? StartConsumer(ActivitySource source, string name, RequestTraceContext incoming, TraceDispatchKind kind)`: returns `null` when `source.HasListeners()` is false; for `Send` with a valid `incoming` it starts `ActivityKind.Consumer` with the incoming context as parent; for the other kinds it starts a consumer span with `parentContext: default` (a new trace) and an `ActivityLink` to the incoming context; an invalid `incoming` starts a plain consumer span with no parent and no link.
2. **Modify** `PublicAPI.Unshipped.txt`, `.github/coverage-manifest/Encina.json` and add `changelog.d/132-trace-context-consumer.added.md` through `mechanical-fixer`.
3. Tests: `tests/Encina.UnitTests/Tracing/TraceContextConsumerTests.cs` (an `ActivityListener` records: `Send` shares the trace id and has the producer span as parent; `Publish`, `Scheduled` and `Batch` have a different trace id and one link to the producer; an invalid incoming context gives no parent and no link; no listener returns `null`); `tests/Encina.GuardTests/Tracing/TraceContextConsumerGuardTests.cs` (null source, null or blank name; an out-of-range `kind` throws `ArgumentOutOfRangeException`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #132 plan docs/plans/trace-context-propagation-implementation-plan-132.md, phase 2. Phase 1 merged.

TASK:
1. Add TraceContextConsumer.StartConsumer and TraceDispatchKind implementing ADR-054 D2 exactly: Send = child of the
   producer span, same trace; Publish, Scheduled and Batch = new trace plus an ActivityLink to the producer.
2. Tests with a real ActivityListener asserting trace ids, parents and links; PublicAPI, manifest, changelog through
   mechanical-fixer.

KEY RULES:
- Return null without a listener; never throw for an invalid incoming context.
- No configuration option (ADR-054 D2). Identical behavior for every transport and store.

REFERENCE FILES:
- docs/architecture/adr/054-observability-conventions-and-trace-propagation.md (D2)
- src/Encina/Diagnostics/EncinaDiagnostics.cs, src/Encina.Messaging/Diagnostics/OutboxActivitySource.cs
```

</details>

---

### Phase 3: Documentation (pull request D1)

> **Goal**: Say how a transport or a store uses the API, and how to enable log correlation.

<details>
<summary><strong>Tasks</strong></summary>

1. `docs/features/trace-context-propagation.md` through `docs-writer`: how to inject and extract with a transport's own header type, the D2 table, why there is no baggage, how to enable `ActivityTrackingOptions`.
2. The `Encina.OpenTelemetry` README is not edited here (the observability guide is #906's).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #132 plan docs/plans/trace-context-propagation-implementation-plan-132.md, phase 3, documentation only.

TASK:
1. Write docs/features/trace-context-propagation.md (one Diataxis quadrant: how-to). Verify identifiers exist in src/.

KEY RULES:
- Use the encina-docs skill; no emojis; no hand-typed coverage figures.

REFERENCE FILES:
- docs/architecture/adr/054-observability-conventions-and-trace-propagation.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| W3C Trace Context | `traceparent` and `tracestate` headers; a receiver that finds an invalid `traceparent` starts a new trace | `Extract` behavior; header names |
| W3C Baggage | `baggage` header | Explicitly not used (ADR-054 D4) |
| OpenTelemetry messaging semantic conventions | Consumer spans are children for a single message and link to the producer for batches and fan-out | The D2 table |
| ADR-054 | D1 envelope, D2 relation rule, D3 wire format, D4 no baggage, D5 scope | Scope and behavior |
| ADR-062 | Error category contract | No error is created (register row C2) |
| `AGENTS.md` section 3 | Logs carry codes, never values | Events 184-185 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `RequestTraceContext` | `src/Encina/Core/RequestTraceContext.cs` (plan #186) | The value injected and extracted |
| `EncinaDiagnostics.ActivitySource` | `src/Encina/Diagnostics/EncinaDiagnostics.cs:10` | Example `ActivitySource` for the helper |
| `OutboxActivitySource` and siblings | `src/Encina.Messaging/Diagnostics/` | The first callers of `StartConsumer` (through #176) |
| `AddEncina` | `src/Encina/Core/ServiceCollectionExtensions.cs` | Registration point |
| `RedactedException.ForLogging` | `src/Encina/Diagnostics/RedactedException.cs:150` | Not needed: no exception is logged |
| `EventIdRanges.Core` | `src/Encina/Diagnostics/EventIdRanges.cs` | Ids 184-185 |

Transport adoption (owned by #725, listed so the API is checked against every carrier): RabbitMQ `IBasicProperties.Headers` (`IDictionary<string, object>`), Kafka `Headers` (`byte[]` values), AzureServiceBus `ApplicationProperties`, AmazonSQS `MessageAttributes`, NATS message headers, MQTT v5 user properties (v3.1.1 has no headers), InMemory (an in-process dictionary), gRPC `Metadata`, GraphQL (HTTP headers), Redis.PubSub (no headers: the envelope of the message). The six planned transports (GoogleCloudPubSub, AmazonEventBridge, Pulsar, Redis.Streams, ActiveMQ, Dapr) adopt the same API. Whether Redis.PubSub and MQTT v3.1.1 carry the trace in the message body is a decision of #725, recorded there.

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina` (core) | 184-185 (inside `EventIdRanges.Core` 100-199) | 184 invalid `traceparent` ignored (reason code), 185 `tracestate` dropped; 177-183 belong to the plan of #2382; no new range, and no id of the block 5970-5999 is used |

### Estimated File Count

Every count includes tests, the coverage manifest, PublicAPI and the changelog fragment; each pull request builds and passes alone.

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| P1 | Propagator, headers, log, registration | 4 | 2 (`ServiceCollectionExtensions.cs`, PublicAPI) | 5 | 0 | 1 | 1 (changelog) | 13 | ~750 |
| P2 | Consumer span rule | 1 | 1 (PublicAPI) | 2 | 0 | 1 | 1 | 6 | ~400 |
| D1 | Documentation | 0 | 0 | 0 | 0 | 0 | 1 (feature page) | 1 | ~120 |
| **Total** | | **5** | **3** | **7** | **0** | **2** | **3** | **20** | **~1,270** |

The columns sum to the total (5 + 3 + 7 + 0 + 2 + 3 = 20; 13 + 6 + 1 = 20). The pull request of this plan itself is one file.

Per-file coverage targets (per flag, one sentence each), written to `.github/coverage-manifest/Encina.json`:

| File | Unit | Guard | Contract | Property | Justification |
|------|------|-------|----------|----------|---------------|
| `Tracing/ContextPropagator.cs` | 95 | 75 | 80 | 80 | Every branch (valid, invalid, missing, tracestate only) is reachable with plain carriers; guard reaches the constructor and parameter checks; the contract test executes the real implementation; the round-trip property drives random valid contexts |
| `Tracing/ContextPropagatorExtensions.cs` | 95 | 70 | 0 | 0 | One expression body, covered with and without an activity; the guard run covers the null arguments; no contract or property applies to an extension |
| `Tracing/TraceContextConsumer.cs` | 95 | 70 | 0 | 0 | Every kind and the invalid-incoming and no-listener paths are driven with a real listener; guard covers the null and out-of-range arguments |
| `Tracing/TraceContextLog.cs`, `IContextPropagator.cs` | 0 | 0 | 0 | 0 | Generated log code and an interface with constants have no coverable lines of their own; both are executed through `ContextPropagator.cs` |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #132 - the W3C trace context propagation API of Encina - in three small pull requests.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution always; edit source only with the
  Edit/Write tools; PublicAPI, manifests and changelog fragments through mechanical-fixer; docs through docs-writer.
- ADR-054: W3C traceparent and tracestate are the only propagation fields; never baggage; the consumer span is a child for
  Send and a new trace with a link for Publish, scheduled and batch, identical on all 10 transports, no option.

IMPLEMENTATION OVERVIEW:
1. P1: IContextPropagator (setter and getter delegates), TraceContextHeaders, ContextPropagator, InjectCurrent, log events
   184-185 of EventIdRanges.Core, TryAddSingleton in AddEncina.
2. P2: TraceContextConsumer.StartConsumer and TraceDispatchKind (the ADR-054 D2 rule in one place).
3. D1: docs/features/trace-context-propagation.md.

KEY PATTERNS:
- Extract never throws and logs reason codes only; an invalid context is default; no scoped service in a singleton.
- Tests execute real code: ActivityListener for spans, a dictionary and a byte-valued carrier for propagation.
- Guard tests for every new public method with parameters.

REFERENCE FILES:
- docs/architecture/adr/054-observability-conventions-and-trace-propagation.md
- src/Encina/Core/RequestTraceContext.cs (from #186), src/Encina/Core/ServiceCollectionExtensions.cs
- docs/plans/trace-context-propagation-implementation-plan-132.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Stateless header handling |
| 2 | OpenTelemetry | ✅ Phases 1-2 | This is trace-context propagation; the consumer span helper applies D2 |
| 3 | Structured Logging | ✅ Phase 1 | Events 184-185, reason codes only |
| 4 | Health Checks | ❌ N/A | No checkable dependency |
| 5 | Validation | ✅ Phase 1 | Foreign headers are validated by `TryParse`; invalid input is ignored, never an error |
| 6 | Resilience | ❌ N/A | No external call |
| 7 | Distributed Locks | ❌ N/A | No shared state |
| 8 | Transactions | ⏭️ #176 | Writing the trace with the message in the same write is #176 on the #1164 envelope |
| 9 | Idempotency | ❌ N/A | Trace ids are not part of any idempotency key |
| 10 | Multi-Tenancy | ❌ N/A | No tenant data is carried; tenant tags on spans follow ADR-054 D4 where spans are created |
| 11 | Module Isolation | ❌ N/A | Cross-module by design |
| 12 | Audit Trail | ❌ N/A | Infrastructure metadata, not a security-relevant state |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| `RequestTraceContext` | Plan #186 (T1) | P1 cannot compile before it merges; this plan names it by issue number |
| ADR-054 accepted | ✅ On `main` | D1 to D5 |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Per-transport adoption | #725 | Calls `Inject` and `Extract` in the 10 transports |
| Store-side capture and restore | #176 | Calls the propagator or `RequestTraceContext` and `StartConsumer` in the Messaging processors |
| Request span behavior | #184 | Owned by the observability-core-2 plan; not duplicated here |
| Overlapping issue | #265 | ADR-054 D5 reconciles it with #132 and #725: maintainer action, close it as covered once both plans are implemented |

---

## Next Steps

1. Link this plan from #132 and ask the maintainer to answer Design Choices 1 and 3 and register row C6 when work starts.
2. Implement #186 T1 first, then P1, P2 and D1; amend the acceptance criteria of #132 to the ADR text (Design Choice 5) when it is picked up.
