# ADR-054: Observability Conventions and Cross-Boundary Trace Propagation

## Status

**Accepted** - 2026-10-22, issue [#1791](https://github.com/dlrivada/Encina/issues/1791). The maintainer decided the six open points of the spike on 2026-10-22, in the comment thread of the issue: D1 option 1b, D2 option 2c per operation without a user switch, D3 options 3a and 3i, D4 option 4b without baggage, D5 option 5c, D6 option 6c extended to every category. The evidence is in the [spike report](../../engineering/observability-conventions-spike-1791.md).

## Context

A request that enters Encina is dispatched, may be written to the outbox, inbox, scheduling or saga stores, and is later published by a transport and consumed elsewhere. Today the trace stops at the first store: no persisted message carries a trace context, none of the 10 transports injects or extracts W3C headers, and 27 meters share the plain name `"Encina"` while 30 of the 49 `ActivitySource` names and 29 of the 30 meter names are not subscribed by `WithEncina()`. Instrument names mix lowercase and PascalCase, `ms` and `s`, with and without `_total`. The spike report lists the measurements with their file and line (sections 2 and 3).

[ADR-018](018-cross-cutting-integration-principle.md) requires every feature to be evaluated against OpenTelemetry, but no ADR defines the conventions themselves; this is the first. The points the research settled by an existing rule or citation are recorded below without reopening them; the six dilemmas are the decisions of the maintainer.

## Decision

### Settled points

These are not choices. Each has the citation in section 5 of the spike report. Points 3 and 4 were proposed as settled there (no project rule names them) and are confirmed by accepting this ADR.

1. **Exporters and dashboards stay out of `WithEncina()`.** A library depends on the OpenTelemetry API; the application chooses the SDK, configuration and exporters (OpenTelemetry library guidelines; pay-for-what-you-use, [AGENTS.md](../../../AGENTS.md) section 1). Exporter work is [#1043](https://github.com/dlrivada/Encina/issues/1043) and [#1048](https://github.com/dlrivada/Encina/issues/1048) and [#1049](https://github.com/dlrivada/Encina/issues/1049), dashboards [#187](https://github.com/dlrivada/Encina/issues/187). No telemetry is sent to the maintainer or the project ([SPEC-000](../../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md), [SPEC-002](../../specifications/SPEC-002-eu-regulatory-readiness.md) DEC-010).
2. **Error status carries the code or the type only.** A span marked as error, a tag or an event carries the `EncinaError` code or the exception type name, never `EncinaError.Message` or `Exception.Message` ([AGENTS.md](../../../AGENTS.md) section 3). A `Left` marks the span as error with the code. The remaining leaks are tracked by [#1788](https://github.com/dlrivada/Encina/issues/1788), [#1789](https://github.com/dlrivada/Encina/issues/1789), [#1468](https://github.com/dlrivada/Encina/issues/1468) and [#2474](https://github.com/dlrivada/Encina/issues/2474).
3. **W3C Trace Context is the wire format.** `traceparent` and `tracestate` are the only propagation fields.
4. **Instrument names follow the OpenTelemetry metric rules.** Lowercase, dot-separated names; durations in seconds with unit `s`; no `_total` suffix on counters; the unit lives in the metadata, not in the name. This applies to instrument names; the roots of source and meter names are decision D3.
5. **All 10 transports are coherent.** A partial subset is ruled out ([AGENTS.md](../../../AGENTS.md) section 5).
6. **No compatibility layers.** Renames and schema changes are done completely ([AGENTS.md](../../../AGENTS.md) sections 1 and 3).
7. **Instrumentation costs nothing without a listener** (`HasListeners()` guards, `StartActivity` returning `null`), and time is read from `TimeProvider`.
8. **One owner per `ActivitySource` name**, and an architecture test that every source and meter is subscribed or explicitly excluded ([#1787](https://github.com/dlrivada/Encina/issues/1787), [#1790](https://github.com/dlrivada/Encina/issues/1790), [#1637](https://github.com/dlrivada/Encina/issues/1637)).

### D1. The trace context is stored inside the request-context envelope (option 1b)

The persisted trace context (`traceparent`, `tracestate`) lives inside the request-context envelope of [#1164](https://github.com/dlrivada/Encina/issues/1164), for the outbox, inbox, scheduled and saga rows. Dedicated columns are not added. The same dispatcher code rebuilds the request context and the trace. The #1164 envelope must therefore land before or together with the trace-propagation work.

### D2. Per-operation rule, fixed, no user switch (option 2c)

| Operation | Consumer span relation to the producer |
| --- | --- |
| `Send` dispatched from the outbox (one message, one handler) | **Child** of the producer span, in the same trace |
| `Publish` (fan-out), scheduled or delayed messages, batches | **New trace** with an `ActivityLink` to the producer |

The rule is identical on all 10 transports. No configuration option is added. The model is NServiceBus ([spike report](../../engineering/observability-conventions-spike-1791.md) section 4.3).

### D3. Names and semantic conventions (options 3a and 3i)

- `ActivitySource` and `Meter` keep PascalCase `Encina.*` names, one per package. The 27 meters named plain `"Encina"` take their package name, like the sources.
- Subscription defects are fixed outside naming: `WithEncina()` subscribes by wildcard, and [#1787](https://github.com/dlrivada/Encina/issues/1787) asserts that no source is phantom or unsubscribed.
- Attributes follow the latest OpenTelemetry messaging semantic conventions. Every attribute key lives in one constants class. Support for `OTEL_SEMCONV_STABILITY_OPT_IN` is added once messaging is declared stable.

### D4. No baggage; the tenant is an attribute and a tag when multi-tenancy is on (option 4b)

- Encina propagates and persists only `traceparent` and `tracestate`. It never propagates or stores OpenTelemetry baggage, as a data-minimisation measure (GDPR Art. 5(1)(c) and Art. 25). An application's own baggage is left untouched.
- When multi-tenancy is enabled, the tenant id is a span attribute and a metric tag in every package. This is the rule of [SPEC-002](../../specifications/SPEC-002-eu-regulatory-readiness.md) AC-044, applied across all of Encina. When multi-tenancy is off, no tenant tag is emitted.
- The cardinality risk of the tenant metric tag is documented in the observability guide ([#906](https://github.com/dlrivada/Encina/issues/906)).

### D5. Release scope (option 5c)

Version 0.14.0 includes:

- these conventions;
- the persisted shape, which is the #1164 context envelope required by D1;
- transport instrumentation on all 10 transports ([#725](https://github.com/dlrivada/Encina/issues/725), [#177](https://github.com/dlrivada/Encina/issues/177));
- the context-propagation API of [#132](https://github.com/dlrivada/Encina/issues/132);
- the liveness and readiness health separation of [#454](https://github.com/dlrivada/Encina/issues/454).

[#1164](https://github.com/dlrivada/Encina/issues/1164), #132, #725, #177 and #454 are in milestone v0.14.0. #132 is re-scoped against D4: Encina neither propagates nor persists baggage. [#185](https://github.com/dlrivada/Encina/issues/185) stays with the [#1794](https://github.com/dlrivada/Encina/issues/1794) triage, because as written it conflicts with the error-message rule. [#265](https://github.com/dlrivada/Encina/issues/265) overlaps #132 and #725 and is reconciled with them.

### D6. Encina-level spans and metrics on every provider; no query text or data keys (option 6c extended)

1. Encina emits spans and metrics at the level of its own operations, on every provider of each category: the stores of the 10 database providers, the 8 caches (`ICacheProvider`) and the 5 lock providers (`IDistributedLockProvider`).
2. Driver-level instrumentation (SQL statements, Redis commands) is not duplicated. The documentation says which instrumentation package to enable for each driver.
3. **Encina never emits `db.query.text`, and never emits cache keys or values that carry data.** It emits only the system, the operation, the collection or table, and the outcome (hit or miss, acquired or timeout). The rule sits next to the `EncinaError.Message` rule and is enforced by the static scan of [#1788](https://github.com/dlrivada/Encina/issues/1788).

## Alternatives considered

Each option and the scenario where it wins is in section 6 of the [spike report](../../engineering/observability-conventions-spike-1791.md).

- **D1, dedicated `TraceParent` and `TraceState` columns (1a)**: rejected. It adds columns to four tables across 10 providers and a second schema change next to #1164; no precedent was found among the libraries read.
- **D2, link everywhere (2a), child everywhere (2b), a link default with a per-message or per-endpoint switch (2c as offered)**: rejected. 2a gives two traces to follow for single-message flows; 2b breaks for batches and fan-out; a switch adds two code paths and an option to document and test on every transport.
- **D3, lowercase `encina.*` roots (3b), one root `Encina` (3c)**: rejected. 3b matches no studied .NET library; 3c prevents toggling one package. Staying on the v1.24.0 messaging names (3ii) or emitting both (3iii): not chosen; the maintainer chose to pin the latest names behind one constants class.
- **D4, tenant as span attribute only (4a), propagating baggage as opt-in (4c)**: rejected. 4a would break AC-044 for metrics; 4c sends context to brokers and third parties.
- **D5, conventions and shape only (5a), plus transports only (5b)**: rejected. Traces would still break at the broker, and the propagation API would be designed late.
- **D6, metrics only by default with spans opt-in (6a), spans on every call (6b), decorators of today only (6c as written)**: not chosen. 6a gives no per-call trace; 6c as written leaves caches, locks and MongoDB dark; 6b duplicates driver instrumentation and risks leaking query text.
- **Issue options A (W3C context in the outbox only) and C (B plus exporters, dashboards and a test framework)**: rejected as settled points 5 and 1.

## Consequences

### Positive

- One trace follows a request across the dispatcher, the stores and the transports, with a fixed rule for parent against link.
- Every instrument can be found by one convention, and a host subscribes to all of Encina with one wildcard.
- The telemetry carries no baggage, no query text and no data-bearing cache keys, which follows the data-minimisation reading of GDPR Art. 5(1)(c) and Art. 25 (spike report section 4.4; not legal advice).

### Negative

- The #1164 envelope becomes a prerequisite of the trace work, so its delivery order matters.
- Trace context is not queryable by SQL without parsing the envelope column.
- The tenant metric tag grows cardinality with the number of tenants; the guide documents it.
- The work spans 10 transports, 10 database providers, 8 caches and 5 lock providers.

### What each point implies for the code

The owners are proposed follow-ups, to be confirmed when the issues are updated.

| Implication | Owner |
| --- | --- |
| The #1164 envelope carries `traceparent` and `tracestate`; the dispatcher rebuilds the trace from it | [#1164](https://github.com/dlrivada/Encina/issues/1164) |
| `Send` consumer spans are children and `Publish`, scheduled and batch consumer spans carry an `ActivityLink`, on all 10 transports | [#725](https://github.com/dlrivada/Encina/issues/725), [#132](https://github.com/dlrivada/Encina/issues/132) |
| The 27 plain `"Encina"` meters are renamed to their package name; `WithEncina()` subscribes by wildcard | Follow-up issue of #1791, with [#1787](https://github.com/dlrivada/Encina/issues/1787) (architecture test) and [#1790](https://github.com/dlrivada/Encina/issues/1790) (dead sources) |
| One semantic-conventions constants class holds every messaging and database attribute key | [#177](https://github.com/dlrivada/Encina/issues/177) |
| The tenant tag is emitted only when multi-tenancy is enabled; there is no baggage | Follow-up issues of #1791 |
| Spans and metrics at the Encina operation level for the 10 database stores, the 8 caches and the 5 lock providers | Follow-up issues of #1791 |
| `db.query.text` and data-bearing cache keys or values are never emitted; the static scan checks it | [#1788](https://github.com/dlrivada/Encina/issues/1788) |
| `AGENTS.md` section 3 states the `db.query.text` rule next to the `EncinaError.Message` rule | Follow-up issue of #1791 |

## Related

- [Spike report for #1791](../../engineering/observability-conventions-spike-1791.md)
- [ADR-018](018-cross-cutting-integration-principle.md), [ADR-021](021-eventid-uniqueness-enforcement.md), [SPEC-000](../../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md), [SPEC-002](../../specifications/SPEC-002-eu-regulatory-readiness.md)
- Issues: [#1791](https://github.com/dlrivada/Encina/issues/1791), [#1164](https://github.com/dlrivada/Encina/issues/1164), [#132](https://github.com/dlrivada/Encina/issues/132), [#725](https://github.com/dlrivada/Encina/issues/725), [#177](https://github.com/dlrivada/Encina/issues/177), [#454](https://github.com/dlrivada/Encina/issues/454), [#1787](https://github.com/dlrivada/Encina/issues/1787), [#1788](https://github.com/dlrivada/Encina/issues/1788), [#1789](https://github.com/dlrivada/Encina/issues/1789), [#1790](https://github.com/dlrivada/Encina/issues/1790), [#2474](https://github.com/dlrivada/Encina/issues/2474), [#187](https://github.com/dlrivada/Encina/issues/187), [#1043](https://github.com/dlrivada/Encina/issues/1043), [#1048](https://github.com/dlrivada/Encina/issues/1048), [#185](https://github.com/dlrivada/Encina/issues/185), [#265](https://github.com/dlrivada/Encina/issues/265), [#1794](https://github.com/dlrivada/Encina/issues/1794)

## Date

2026-10-22
