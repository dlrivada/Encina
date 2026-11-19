# Implementation Plan: Distributed Trace Context on `IRequestContext`

> **Issue**: [#186](https://github.com/dlrivada/Encina/issues/186)
> **Type**: Feature (observability core)
> **Related**: ADR-054 (settled point 3 W3C wire format, D1 envelope, D5 release scope), #132 (the propagation API that consumes the type of this plan), #176 (store-side capture and restore), #175 (correlation and causation), #177 (semantic-convention keys), #1164 (the persisted envelope), #2442 (also adds a member to `IRequestContext`), #725 (transports), ADR-035 (request identity model), ADR-062
> **Complexity**: Low-Medium (one small value type and one interface member, but the interface has 8 implementers that change in the same pull request)
> **Estimated Scope**: ~160 lines of production code + ~420 lines of tests, in three pull requests of at most 15 files and 900 lines each (the third only if Design Choice 3 picks option B)

---

## Summary

### What exists today (read on 2026-11-18, main `89303f1e8`)

- `IRequestContext` (`src/Encina/Abstractions/IRequestContext.cs`) has `CorrelationId` (a `string`), `CausationId` (`string?`), `Identity`, `IdempotencyKey`, `TenantId`, `Timestamp` and `Metadata`. It has no trace members. The issue's snippet shows `Guid CorrelationId`; the real contract is a bounded string (`InboundRequestNormalizer.Id`, `InboundRequestInfo.MaxIdLength = 128`).
- A context is built in one place: `RequestContext.Build(...)` (`src/Encina/Core/RequestContext.cs:156`), reached by `Create`, `CreateAnonymousAt` and the internal `CreateAt`; copies go through the private copy constructor, `CopyOf` and `ForNestedDispatch`. A context created without an id takes `Activity.Current?.Id ?? Guid.NewGuid().ToString("N")` as its correlation id (`NewCorrelationId`, `RequestContext.cs:194`); the HTTP entry does the same through `HttpContextInboundRequestExtensions.cs:98` (`Header(headers, CorrelationIdHeader) ?? Activity.Current?.Id`). With W3C ids, that `Activity.Id` is the whole `traceparent` string.
- `IRequestContext` has 8 implementers: 2 in `src` (`RequestContext`, `IdentityPinnedRequestContext` in `src/Encina.Caching/Behaviors/`) and 6 in tests (`tests/Encina.TestInfrastructure/PropertyTests/TestRequestContext.cs`, `tests/Encina.PropertyTests/Infrastructure/Caching/CacheKeyGeneratorPropertyTests.cs:171`, `tests/Encina.GuardTests/Messaging/SoftDelete/SoftDeleteQueryFilterBehaviorGuardTests.cs:121`, `tests/Encina.IntegrationTests/Infrastructure/MongoDB/ReadWriteSeparation/RoutingPipelineMongoDBIntegrationTests.cs:666`, `tests/Encina.UnitTests/Marten/EventMetadataEnrichmentServiceTests.cs:407`, `tests/Encina.BenchmarkTests/Encina.Caching.Benchmarks/CacheKeyGeneratorBenchmarks.cs:85`); 437 test sites use `Substitute.For<IRequestContext>()`, which return defaults for a new member without edits.
- The persisted form of a request identity, `PersistedRequestIdentity(Kind, ActorId, TenantId, CorrelationId, CausationId)` (`src/Encina/Identity/PersistedRequestIdentity.cs`), has no trace fields; #1164 persists it and ADR-054 D1 requires the trace context to live in that envelope.
- Not in `src`: any `traceparent`, `TraceState` or W3C parsing code; no OpenTelemetry package is referenced by core (`src/Encina/Encina.csproj`), so everything here uses `System.Diagnostics` only.

### What this plan delivers

`RequestTraceContext`, a `readonly record struct` holding the W3C `traceparent` and `tracestate` of the activity that was current when the context was created, with `TraceId`, `SpanId` and `IsSampled` derived from it, and one member `TraceContext` on `IRequestContext`. The snapshot is taken in `RequestContext.Build` so every factory, copy and restored context carries it. Convenience extension properties `context.TraceId` and `context.SpanId` follow the precedent of `context.UserId` (`RequestContextIdentityExtensions`).

- Shared pieces and owner: this plan owns `RequestTraceContext` and `IRequestContext.TraceContext`; #132 owns `IContextPropagator` and the consumer-span rule built on it; #176 owns writing and reading the trace in the #1164 envelope; #175 owns correlation and causation; #177 owns the attribute-key constants. #2442 (`ModuleId`) also adds an interface member: whichever pull request lands second adds its property to the same 8 implementers.
- Affected packages: `Encina` (core) and, when Design Choice 3 picks B, `Encina.AspNetCore`.
- Provider category: none.
- Deviations from the issue, each in a Design Choice: five flat properties become one value type (1); `ParentSpanId` is not carried (1); the context is a snapshot, not a live read (2).

---

## Design Choices

<details>
<summary><strong>1. Shape on the interface: five flat properties or one value type</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Five flat members `TraceId`, `SpanId`, `ParentSpanId`, `TraceFlags`, `TraceState` (the issue's text)** | Matches the issue and the acceptance criteria literally; `context.TraceId` reads directly | Five members for 8 implementers and 437 mock sites to reason about; `ParentSpanId` and `TraceFlags` are not wire fields, so a restored message could not rebuild them; ADR-054 settled point 3 says `traceparent` and `tracestate` are the only propagation fields |
| **B) One member `RequestTraceContext TraceContext` (`TraceParent`, `TraceState`; `TraceId`, `SpanId`, `IsSampled` derived), plus extension properties `TraceId` and `SpanId` on `IRequestContext`** | One member to implement; the value is exactly the two wire fields, so persisting and restoring it is lossless; `ParentSpanId` stays available as `Activity.ParentSpanId` for the code that needs it | `ParentSpanId` and `TraceFlags` as named in the issue are not on the context (the sampled bit is `IsSampled`) |
| **C) B plus a captured `ParentSpanId`** | Satisfies the issue's `ParentSpanId` item | The parent is a property of the span, not of the request: it changes on every nested span and cannot be rebuilt from `traceparent`, so the value would be wrong after a restore |

### Chosen Option: **B — One value type, extension properties for convenience** (recommended, pending the maintainer)

### Rationale

- B wins when the persisted envelope (#1164, ADR-054 D1) must restore the context exactly: only the two wire fields round-trip. A wins for a team whose handlers log `context.ParentSpanId` today; C wins when the maintainer wants the issue's list verbatim and accepts that the value is lost on restore.
- If the maintainer picks A or C, Phase 1 adds the members to the same 8 implementers and the type holds the extra fields; the file count of the first pull request does not change.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Snapshot at creation or live read of `Activity.Current`</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Snapshot in `RequestContext.Build` from `Activity.Current`; copies and nested dispatches keep the parent's snapshot** | The context stays immutable; the value is stable across threads and `await`; one seam covers every factory | After a handler starts its own span, `context.TraceContext` still names the entry span |
| **B) Compute on every read from `Activity.Current`** | Always the current span | A context kept beyond its scope or read on another flow returns another request's span; contradicts the immutability the interface documents |

### Chosen Option: **A — Snapshot in `Build`**

### Rationale

- `IRequestContext` states "Immutable design: use `With*` methods to create modified copies" and `Timestamp` is "captured when the context is created"; B breaks both.
- A message written by a handler must carry the span current at the write, not the entry span: #176 reads `Activity.Current` at the write and falls back to this snapshot only when no activity exists, so the snapshot never has to be live.

**Class:** Settled
**Settled by:** `src/Encina/Abstractions/IRequestContext.cs` remarks ("Immutable design") and `Timestamp` ("Captured when the context is created"); only A complies

</details>

<details>
<summary><strong>3. Where the inbound HTTP trace context comes from</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Activity.Current` only (ASP.NET Core hosting already turns a valid `traceparent` header into the request activity)** | No second parse; the sampling decision of the host is respected; no cost without a listener (ADR-054 settled point 7) | A service with no `ActivitySource` listener has no activity, so it forwards no inbound trace context |
| **B) `Activity.Current`, and when it is null, the `traceparent` and `tracestate` headers read by `EncinaContextMiddleware` through `InboundRequestInfo`** | Pass-through of the caller's trace in a service that has no tracing SDK | A header parse per request on the hot path; two members added to `InboundRequestInfo` and the middleware; the value is client-controlled and needs the validation of Design Choice 4 |

### Chosen Option: **A — `Activity.Current` only** (recommended, pending the maintainer)

### Rationale

- A wins when every Encina service runs an OpenTelemetry SDK (the ADR-054 assumption); B wins for a relay service without a tracing SDK that must still keep the chain intact for the next hop.
- Pay-for-what-you-use (`AGENTS.md` section 1) and ADR-054 settled point 7 favor A; B adds a third pull request (T3).

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Validation of a trace context that came from outside</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `RequestTraceContext.TryParse` uses `ActivityContext.TryParse(traceparent, tracestate, ...)` and keeps the strings only when it succeeds; `tracestate` is also bounded to 512 characters and stripped of control characters** | The W3C rules (version, lengths, hex, all-zero ids, tracestate grammar) come from the platform; an invalid value becomes `default` and is never an error | Reads the string twice (parse, then keep) |
| **B) A hand-written parser** | No dependency on the BCL parser | A second W3C implementation to keep correct |

### Chosen Option: **A — The BCL parser plus a length bound**

### Rationale

- The value is client-controlled (`traceparent` and `tracestate` headers, or a persisted row): a log line or a response header that echoed it unchecked would be an injection vector, the same reason `InboundRequestNormalizer` bounds ids and strips control characters.
- A failed parse never throws and is not an `EncinaError`: the context simply has no trace (register row C1).

**Class:** Trivial
**Settled by:** `InboundRequestNormalizer.cs` (bounded ids, control characters stripped) and the .NET API `ActivityContext.TryParse`

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | No new `EncinaError` code and no exception: an invalid or absent trace context is `default(RequestTraceContext)`, so there is no ADR-062 `ErrorCategory` to assign | Settled | ADR-062 (a category is chosen where an `EncinaError` is created; none is created) |
| C2 | The type carries no tenant, user or secret; the tenant tag on telemetry is the concern of the packages that emit it (ADR-054 D4), so there is no tenant-qualified id here and the reserved `default` tenant segment does not apply | Settled | ADR-054 D4 |
| C3 | The interface change lands together with all 8 implementers in one pull request; no default interface member | Settled | Plan #2442 ("pre-1.0: no default interface members. An interface change lands together with every implementer in one pull request") and `AGENTS.md` section 3 (no compatibility layers) |
| C4 | `FromActivity` returns `default` when the activity uses the hierarchical id format (`Activity.IdFormat != W3C`) | Trivial | .NET `Activity.DefaultIdFormat` is W3C; the hierarchical id is not a `traceparent` |
| C5 | Public names: `RequestTraceContext`, `IRequestContext.TraceContext`, extension properties `TraceId` and `SpanId` in namespace `Encina` | Dilemma (recommended, pending the maintainer) | public surface; alternatives are `TraceContext` as the type name (clashes with the member name) or a namespace `Encina.Tracing` |

---

## Implementation Phases

### Phase 1: Value Type, Interface Member and Implementers (pull request T1)

> **Goal**: Every context carries a validated snapshot of the current W3C trace context.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina/Core/RequestTraceContext.cs`: `public readonly record struct RequestTraceContext(string? TraceParent, string? TraceState)`; `bool IsValid`; `string? TraceId` and `string? SpanId` (substrings of a valid `traceparent`); `bool IsSampled` (flag `01`); `static RequestTraceContext FromActivity(Activity? activity)` (W3C ids only, register row C4); `static bool TryParse(string? traceParent, string? traceState, out RequestTraceContext context)` (Design Choice 4); `static RequestTraceContext None => default`.
2. **Modify** `src/Encina/Abstractions/IRequestContext.cs`: `RequestTraceContext TraceContext { get; }` with XML docs (a snapshot; `default` when no activity).
3. **Modify** `src/Encina/Core/RequestContext.cs`: `public RequestTraceContext TraceContext { get; init; }`; set in `Build` from `RequestTraceContext.FromActivity(Activity.Current)`; copied by the private copy constructor and `CopyOf`; kept by `ForNestedDispatch`. **Modify** `src/Encina.Caching/Behaviors/IdentityPinnedRequestContext.cs`: `public RequestTraceContext TraceContext => _inner.TraceContext;`.
4. **Modify** the 6 test implementers listed in Summary (one property each; the property returns `default` or a fixed value).
5. **Modify** `src/Encina/PublicAPI.Unshipped.txt`, `.github/coverage-manifest/Encina.json` and add `changelog.d/186-request-context-trace-context.added.md`, all through `mechanical-fixer`.
6. Tests (new files): `tests/Encina.UnitTests/Core/RequestTraceContextTests.cs` (valid and invalid `traceparent`, all-zero ids, version `ff`, over-long `tracestate`, control characters, W3C and hierarchical activities, copies and nested dispatch keep the snapshot, a context created inside an activity carries its ids, a context created outside carries none; and a case that wraps a context carrying a trace in `IdentityPinnedRequestContext` (internal, reachable through the existing InternalsVisibleTo) and reads `TraceContext`, so the forwarding member keeps that file at its manifest target of 100 without a sixteenth file); `tests/Encina.GuardTests/Core/RequestTraceContextGuardTests.cs` (`TryParse(null, null)` is a valid `false` result, not an exception; `FromActivity(null)` returns `default`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #186, plan docs/plans/request-context-trace-context-implementation-plan-186.md,
phase 1. IRequestContext has 8 implementers (2 in src, 6 in tests); RequestContext.Build is the one creation seam.

TASK:
1. Create RequestTraceContext (readonly record struct: TraceParent, TraceState, derived TraceId, SpanId, IsSampled,
   FromActivity, TryParse using ActivityContext.TryParse, tracestate bounded to 512 chars without control characters).
2. Add TraceContext to IRequestContext and implement it in RequestContext (captured in Build from Activity.Current) and
   IdentityPinnedRequestContext; add the property to every test implementer so the whole solution builds.
3. Unit and guard tests that execute the real type; PublicAPI, manifest and changelog through mechanical-fixer.

KEY RULES:
- Never throw or return an error for an invalid trace context: it becomes default.
- No default interface member, no [Obsolete]. Re-grep implementers (class|record|struct ... : IRequestContext) at work
  start; if #2442 landed first, keep its ModuleId property in every implementer.
- Core references no OpenTelemetry package: System.Diagnostics only.

REFERENCE FILES:
- src/Encina/Abstractions/IRequestContext.cs, src/Encina/Core/RequestContext.cs (Build at :156)
- src/Encina.Caching/Behaviors/IdentityPinnedRequestContext.cs
- src/Encina/Identity/RequestContextIdentityExtensions.cs (the extension-property precedent)
```

</details>

---

### Phase 2: Extension Properties (pull request T2)

> **Goal**: `context.TraceId` and `context.SpanId` read like `context.UserId`.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina/Core/RequestContextTraceExtensions.cs` with C# 14 extension members `string? TraceId` and `string? SpanId` on `IRequestContext` (they use `context?.`, so a `null` context returns `null`; this is a new decision of the plan, not the `UserId` precedent, which is null-tolerant only of `Identity` and throws on a null context).
2. **Modify** `PublicAPI.Unshipped.txt`, `.github/coverage-manifest/Encina.json` and the changelog fragment `changelog.d/186-request-context-trace-extensions.added.md` through `mechanical-fixer`.
3. Tests: `tests/Encina.UnitTests/Core/RequestContextTraceExtensionsTests.cs`; `tests/Encina.GuardTests/Core/RequestContextTraceExtensionsGuardTests.cs` (a `null` context is a valid `null` result).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #186 plan docs/plans/request-context-trace-context-implementation-plan-186.md, phase 2. Phase 1 merged:
IRequestContext.TraceContext exists.

TASK:
1. Add C# 14 extension properties TraceId and SpanId on IRequestContext, reading context.TraceContext.
2. Tests; PublicAPI, manifest and changelog through mechanical-fixer.

KEY RULES:
- Follow RequestContextIdentityExtensions for shape and null handling. XML docs with an example.

REFERENCE FILES:
- src/Encina/Identity/RequestContextIdentityExtensions.cs, src/Encina/Core/RequestTraceContext.cs
```

</details>

---

### Phase 3: HTTP Pass-Through (pull request T3, only if Design Choice 3 picks B)

> **Goal**: A service without a tracing listener still carries the caller's `traceparent`.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina/Identity/InboundRequestInfo.cs` (two optional trailing members `TraceParent` and `TraceState`), `src/Encina.AspNetCore/HttpContextInboundRequestExtensions.cs` (read both headers only when `Activity.Current` is null), `src/Encina/Identity/RequestContextScopeFactory.cs` (apply the validated value to the new context) and the matching tests; edit `PublicAPI.Unshipped.txt` files, the two package manifests and the changelog through `mechanical-fixer`.
2. Every consumer of `new InboundRequestInfo(` is grepped at work start (optional trailing parameters keep existing call sites compiling).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #186 plan docs/plans/request-context-trace-context-implementation-plan-186.md, phase 3, only when the maintainer
chose option B of Design Choice 3.

TASK:
1. Carry traceparent and tracestate through InboundRequestInfo when no Activity exists; validate with
   RequestTraceContext.TryParse; an invalid header is ignored, never an error.

KEY RULES:
- Client-controlled input: bounded, control characters stripped, never logged raw.
- Optional trailing record parameters (CS1737 order). Manifests, PublicAPI and changelog through mechanical-fixer.

REFERENCE FILES:
- src/Encina.AspNetCore/HttpContextInboundRequestExtensions.cs (:98), src/Encina/Identity/InboundRequestInfo.cs
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| W3C Trace Context (Recommendation) | `traceparent` version-trace-id-parent-id-flags; `tracestate` list grammar and size limits | Format and validation of `RequestTraceContext` |
| ADR-054 | Settled point 3 (W3C wire format, only `traceparent` and `tracestate`), D1 envelope, D4 tenant tag, D5 scope | The type holds only the two wire fields |
| ADR-035 | Request identity model (immutable context) | Snapshot semantics (Design Choice 2) |
| ADR-062 | Error category contract | No error is created (register row C1) |
| `AGENTS.md` section 3 | Gates fail closed; no compatibility layers | Invalid input becomes `default`; no default interface member |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `RequestContext.Build` and `NewCorrelationId` | `src/Encina/Core/RequestContext.cs:156,194` | The single capture seam |
| `PersistedRequestIdentity` | `src/Encina/Identity/PersistedRequestIdentity.cs` | The envelope #1164 persists and #176 extends with the trace |
| `RequestContextIdentityExtensions` | `src/Encina/Identity/` | Precedent for extension properties |
| `InboundRequestNormalizer` | `src/Encina/Identity/InboundRequestNormalizer.cs` | Bounding and control-character rules for client values |
| `IdentityPinnedRequestContext` | `src/Encina.Caching/Behaviors/` | Second `src` implementer |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina` (core) | none | No logging code is added; an invalid trace context is silent by design (no operational event on a hot path). The id block 5970-5999 assigned to this family is not used by this plan |

### Estimated File Count

Every count includes tests, the coverage manifest, PublicAPI and the changelog fragment; each pull request builds and passes alone.

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| T1 | Type, interface member, 8 implementers | 1 | 4 (interface, `RequestContext.cs`, `IdentityPinnedRequestContext.cs`, PublicAPI) | 2 | 6 (the test implementers) | 1 | 1 (changelog) | 15 | ~520 |
| T2 | Extension properties | 1 | 1 (PublicAPI) | 2 | 0 | 1 | 1 | 6 | ~200 |
| T3 | HTTP pass-through (conditional) | 0 | 5 (`InboundRequestInfo.cs`, `HttpContextInboundRequestExtensions.cs`, `RequestContextScopeFactory.cs`, 2 PublicAPI) | 2 | 1 | 2 | 1 | 11 | ~450 |
| **Total** | | **2** | **10** | **6** | **7** | **4** | **3** | **32** | **~1,170** |

The columns sum to the total (2 + 10 + 6 + 7 + 4 + 3 = 32; 15 + 6 + 11 = 32). T1 is exactly 15 files; if #2442 lands first, its property is already in the 8 implementers and T1 changes the same lines, so the count does not grow.

Per-file coverage targets (per flag, one sentence each):

| File | Unit | Guard | Contract | Property | Justification |
|------|------|-------|----------|----------|---------------|
| `Core/RequestTraceContext.cs` | 95 | 70 | 0 | 0 | Every parse and derivation branch is reachable with plain strings and real activities; the guard run reaches the null-tolerant `TryParse` and `FromActivity(null)`; no interface contract applies to a struct |
| `Core/RequestContext.cs` (existing 95 unit, 60 guard) | 95 | 60 | 0 | 0 | The new property, copy and capture lines are covered by the unit tests of T1; targets do not go down |
| `Core/RequestContextTraceExtensions.cs` | 95 | 80 | 0 | 0 | Two expression-bodied members, both driven with real contexts; the guard run covers the null context |
| `Caching/Behaviors/IdentityPinnedRequestContext.cs` (existing 100 unit) | 100 | 0 | 0 | 0 | The forwarding `TraceContext` member is read by the wrapped-context case of `RequestTraceContextTests`; the target of 100 does not go down |

Documentation: this plan has no docs task; the new public API carries XML docs only, and the propagation page belongs to #132.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #186 - the distributed trace context of IRequestContext - in at most three pull requests.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution always; edit source only with the
  Edit/Write tools; PublicAPI, coverage manifests and changelog fragments through mechanical-fixer.
- ADR-054: W3C traceparent and tracestate are the only propagation fields; no baggage; the tenant is a tag only when
  multi-tenancy is on.

IMPLEMENTATION OVERVIEW:
1. T1: RequestTraceContext (readonly record struct) and IRequestContext.TraceContext, captured in RequestContext.Build from
   Activity.Current, carried by every copy; the 8 implementers (2 src, 6 tests) change in the same pull request.
2. T2: extension properties TraceId and SpanId.
3. T3 (only if the maintainer chose option B of Design Choice 3): carry the inbound headers when no Activity exists.

KEY PATTERNS:
- Invalid or absent trace context is default, never an error; client values are bounded and control characters stripped.
- No default interface members; grep every implementer at work start (#2442 adds ModuleId to the same implementers).
- Guard tests: a nullable parameter that accepts null gets a valid-result test.

REFERENCE FILES:
- src/Encina/Abstractions/IRequestContext.cs, src/Encina/Core/RequestContext.cs
- docs/architecture/adr/054-observability-conventions-and-trace-propagation.md
- docs/plans/request-context-trace-context-implementation-plan-186.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Per-request value; `IdentityPinnedRequestContext` only forwards it |
| 2 | OpenTelemetry | ✅ Phase 1 | The snapshot of the current activity; the spans and keys are #132, #175 and #177 |
| 3 | Structured Logging | ❌ N/A | No event; log correlation uses the platform `ActivityTrackingOptions` (see #132) |
| 4 | Health Checks | ❌ N/A | No dependency to check |
| 5 | Validation | ✅ Phase 1 | Client-controlled values are validated by `TryParse`; an invalid value becomes `default` |
| 6 | Resilience | ❌ N/A | No external call |
| 7 | Distributed Locks | ❌ N/A | No shared state |
| 8 | Transactions | ⏭️ #176 | Persisting the trace with the message in the same write is #176 on top of #1164 |
| 9 | Idempotency | ❌ N/A | Read-only metadata; not part of any key |
| 10 | Multi-Tenancy | ❌ N/A | The type carries no tenant; telemetry tenant tags are emitted where spans are created (ADR-054 D4) |
| 11 | Module Isolation | ❌ N/A | Cross-module by design; #2442 adds `ModuleId` separately |
| 12 | Audit Trail | ❌ N/A | Infrastructure metadata, not a security-relevant state |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| ADR-054 accepted | ✅ On `main` | Settled point 3, D1 and D5 define the scope |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| `ModuleId` on `IRequestContext` | #2442 | Same interface and implementers; the second pull request to land adds its property to them |
| Propagation API | #132 | Consumes `RequestTraceContext`; opens after T1 |
| Persisted envelope | #1164 | #176 adds the trace fields to it; this plan does not wait for it |

---

## Next Steps

1. Link this plan from #186 and ask the maintainer to answer Design Choices 1 and 3 and register row C5 when work starts.
2. Implement T1, then T2; T3 only if Design Choice 3 picks option B. Open #132 after T1 lands (it names `RequestTraceContext` by issue number).
