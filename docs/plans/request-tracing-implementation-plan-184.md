# Implementation Plan: Request Tracing, Behavior and Handler Spans

> **Issue**: [#184](https://github.com/dlrivada/Encina/issues/184)
> **Type**: Feature (observability core)
> **Related**: ADR-054 (settled point 2 no messages in telemetry, settled point 7 zero cost, D3 names, D4 tenant), #186 (trace context on `IRequestContext`), #132 (propagation API), #183 (sampling, reads the creation attribute), #185 (error recording, sibling), #1789 (command and query behaviors leave spans Unset on `Left`), #1788 (messages in satellite spans), #2676 (tenant attribute helper), #1794 (backlog triage), ADR-062
> **Complexity**: Medium (the request span exists; the new work is two optional span layers inside the pipeline and their opt-in)
> **Estimated Scope**: ~260 lines of production code + ~600 lines of tests + 1 documentation file, in two code pull requests and one documentation pull request of at most 15 files and 900 lines each

---

## Summary

### What exists today (read on 2026-11-20, main `098caa920`)

- Request spans already exist. `Encina.RequestDispatcher.ExecuteAsync` (`src/Encina/Dispatchers/Encina.RequestDispatcher.cs:58`) starts `Encina.Send` through `EncinaDiagnostics.SendStarted` for every request (tags: request type and name, response type, request kind, handler, handler count, identity kind), and `SendCompleted` sets status Ok or Error with the error code only (`EncinaDiagnostics.cs:55-70`). `Encina.Publish` and `Encina.Stream` exist too. The issue's `TracingBehavior<,>` would duplicate the dispatcher span.
- Opt-in `CommandActivityPipelineBehavior` and `QueryActivityPipelineBehavior` add `Encina.Command.<Name>` and `Encina.Query.<Name>` spans with functional-failure tags. They return before recording a `Left` outcome (`CommandActivityPipelineBehavior.cs:53-56` makes `RecordErrorOutcome` unreachable): bug #1789, open, not part of this plan.
- Behaviors run in one place, `PipelineBuilder<,>.ExecuteBehaviorAsync` (`src/Encina/Pipeline/PipelineBuilder.cs`), and the handler in `ExecuteHandlerAsync`; neither starts a span, so a slow behavior is invisible in a trace.
- `IRequestContext` carries `CorrelationId` and `TenantId` but the dispatcher span carries neither. Core has no options type for tracing; `EncinaOpenTelemetryOptions` has no request-tracing flags.
- The issue's `error.Message` as status description and `IncludeUserId` conflict with ADR-054 settled point 2 and with `EncinaDiagnostics` (identity kind only).

### What this plan delivers

- `EncinaTracingOptions` (core, public, defaults off): `TraceBehaviors`, `TraceHandler`, `IncludeCorrelationId`.
- A separate core source `Encina.Pipeline` with spans `Encina.Behavior <Name>` per pipeline step and `Encina.Handler <Name>`, started in `PipelineBuilder` only when the option is on and the source has a listener.
- The correlation id as an attribute of the `Encina.Send` span (opt-in).
- `EncinaOpenTelemetryOptions.EnableBehaviorTracing`, `EnableHandlerTracing` that set the core options, and `WithEncina()` subscribing the source.
- Shared pieces and owner: this plan owns `EncinaTracingOptions`; #185 adds one member to it; #183 reads the creation attribute; #1789 fixes the existing behaviors; #2676 adds the tenant attribute.
- Affected packages: `Encina`, `Encina.OpenTelemetry`. Provider category: observability.
- Not delivered: `TracingBehavior<,>` (the dispatcher span exists), `IncludeUserId` (ADR-054 settled point 2 and the identity-kind rule), tenant attribute (#2676), module attribute (follow-up issue file `artifacts/issues/module-id-on-request-telemetry.md`, written by the #174 plan).

---

## Design Choices

<details>
<summary><strong>1. New TracingBehavior or the spans that exist</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep `Encina.Send` as the request span and add only behavior and handler layers** | No duplicate span per request | The issue's class name is not built |
| **B) Add `TracingBehavior<,>` as the issue states** | Literal | A second span for the same request next to the dispatcher span (and a third with the activity behaviors) |

### Chosen Option: **A — Build on the dispatcher span** (recommended, pending the maintainer)

### Rationale

- A wins in the normal case. B wins only for a host that disables the dispatcher span, which has no switch today.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Where behavior and handler spans are started</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) In `PipelineBuilder.ExecuteBehaviorAsync` and `ExecuteHandlerAsync`** | One place covers every behavior and processor step, including third-party ones | Touches the hot path (guarded by a boolean read) |
| **B) A wrapper behavior around each registered behavior** | No hot-path edit | Doubles the step count, breaks `PipelineOrderResolver` ordering and `IPipelineStepIdentity` |

### Chosen Option: **A — Inside `PipelineBuilder`**

### Rationale

- The builder is the only code that sees every step in order; a wrapper would change the order the resolver validates.

**Class:** Settled
**Settled by:** `PipelineBuilder<,>` composes all steps in `ExecuteBehaviorAsync`; wrapping changes the ordering that `PipelineOrderResolver` computes (`src/Encina/Pipeline/Ordering/PipelineOrderResolver.cs`); option B contradicts the pipeline ordering design

</details>

<details>
<summary><strong>3. How the layers are switched on</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A core `EncinaTracingOptions` (defaults off) plus the separate source `Encina.Pipeline`; the OpenTelemetry options set it** | Opt-in even when `WithEncina()` subscribes by wildcard (#2677); spans cost nothing when off | One more core options type |
| **B) Only the source: on when a host subscribes to `Encina.Pipeline`** | No option | The #2677 wildcard `Encina*` would turn the layers on for everyone |

### Chosen Option: **A — Options plus a separate source** (recommended, pending the maintainer)

### Rationale

- A wins because `AGENTS.md` section 1 makes every feature opt-in and #2677 will subscribe by wildcard. B wins for a host that wants the toggle to be the subscription alone.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Attributes and PII</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Request name, step type name, response type, status as code or exception type; correlation id opt-in; no user id, no messages** | Follows the existing rules | The issue's `IncludeUserId` is not built |
| **B) The issue's set (user id option, error message as status)** | Literal | Violates ADR-054 settled point 2 |

### Chosen Option: **A — Code and type names only**

### Rationale

- ADR-054 settled point 2 allows only the code or exception type; `EncinaDiagnostics` already records identity kind only.

**Class:** Settled
**Settled by:** ADR-054 Decision, settled point 2, and `AGENTS.md` section 3 (`EncinaError.Message` never reaches telemetry); option B contradicts both

</details>

### Register of defaults and semantics

| Id | Choice | Class | Basis |
|----|--------|-------|-------|
| C1 | No new `EncinaError` code (spans never create errors), so no ADR-062 `ErrorCategory` | Settled | ADR-062 |
| C2 | Behavior span status: Error with the error code when the step returns `Left`, Ok otherwise; a cancelled step gets the cancellation code; failure recording goes through the helper of #185 when it lands | Settled | ADR-054 settled point 2 |
| C3 | Span names `Encina.Behavior <TypeName>` and `Encina.Handler <TypeName>` use the simple type name (bounded by registered types); `ActivityKind.Internal` | Trivial | Existing `Encina.Command.<Name>` naming |
| C4 | Correlation id attribute `Encina.correlation_id` is a client-supplied bounded string (128 characters, `InboundRequestInfo.MaxIdLength`); off by default | Trivial | `IRequestContext.CorrelationId` documentation |
| C5 | No tenant attribute and no cardinality here: ADR-054 D4 requires it only with multi-tenancy on and through the helper of #2676; no tenant-qualified id is created | Settled | ADR-054 D4, #2676 |
| C6 | `EncinaTracingOptions` is a singleton read once per request (no scoped service injected into a singleton); defaults off | Trivial | `AGENTS.md` section 3 (registration completeness) |
| C7 | Public names: `EncinaTracingOptions`, `EncinaOpenTelemetryOptions.EnableBehaviorTracing`, `EnableHandlerTracing` | Dilemma (recommended, pending the maintainer) | public surface; alternatives are `EnablePipelineTracing` as one flag |

---

## Implementation Phases

### Phase 1: Core Options and Pipeline Spans (pull request T1)

> **Goal**: Optional behavior and handler spans in the pipeline.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina/Diagnostics/EncinaTracingOptions.cs` (public sealed: `bool TraceBehaviors`, `bool TraceHandler`, `bool IncludeCorrelationId`, all false) and `src/Encina/Diagnostics/PipelineDiagnostics.cs` (internal static: `ActivitySource("Encina.Pipeline", "1.0")`, `StartBehavior(Type)`, `StartHandler(Type)`, `Complete(Activity?, EncinaError?)` using `ActivityStatusDescriptions.ForError`).
2. **Modify** `src/Encina/Core/ServiceCollectionExtensions.cs` (`services.TryAddSingleton<EncinaTracingOptions>()` next to line 61); `src/Encina/Pipeline/PipelineBuilder.cs` (`ExecuteBehaviorAsync` and `ExecuteHandlerAsync` start and complete a span when the option is on and `HasListeners()`; the options reach the builder through its constructor, whose call site is the request wrapper); `src/Encina/Dispatchers/Encina.RequestDispatcher.cs` (correlation id attribute when `IncludeCorrelationId`). Grep `new PipelineBuilder<` on origin/main for every site before changing the constructor.
3. **Modify** `src/Encina/PublicAPI.Unshipped.txt`, `.github/coverage-manifest/Encina.json`; add `changelog.d/184-pipeline-spans.added.md` through `mechanical-fixer`.
4. Tests (new): `tests/Encina.UnitTests/Pipeline/PipelineSpanTests.cs` (a real `Encina` with two behaviors and a handler and an `ActivityListener`: with the options on, one span per behavior in order and one handler span, nested under `Encina.Send`; a `Left` gives Error status with the code and never the message; off gives no span; no listener gives none), `tests/Encina.UnitTests/Diagnostics/EncinaTracingOptionsRegistrationTests.cs` (DI test with `ValidateOnBuild` and `ValidateScopes` after `AddEncina`), `tests/Encina.GuardTests/Diagnostics/PipelineDiagnosticsGuardTests.cs` (null type).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0). Issue #184, plan docs/plans/request-tracing-implementation-plan-184.md, phase 1.
Encina.Send, Encina.Publish and Encina.Stream spans exist; behaviors and handler have no span.

TASK:
1. Add EncinaTracingOptions (defaults off) and PipelineDiagnostics (source "Encina.Pipeline").
2. Start behavior and handler spans in PipelineBuilder only when the option is on and the source has listeners; add the opt-in correlation id attribute to Encina.Send.
3. Register the options with TryAddSingleton; tests with a real ActivityListener and a DI test; PublicAPI, manifest, changelog through mechanical-fixer.

KEY RULES:
- Status carries the error code or exception type only, never a message (ADR-054 settled point 2).
- Zero cost when off; no scoped service in a singleton; no tenant or user attribute.

REFERENCE FILES:
- src/Encina/Pipeline/PipelineBuilder.cs, src/Encina/Diagnostics/EncinaDiagnostics.cs, src/Encina/Core/ServiceCollectionExtensions.cs
```

</details>

---

### Phase 2: OpenTelemetry Options and Subscription (pull request T2)

> **Goal**: The host switches the layers on from `Encina.OpenTelemetry`.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina.OpenTelemetry/EncinaOpenTelemetryOptions.cs` (`EnableBehaviorTracing`, `EnableHandlerTracing`, `IncludeCorrelationId`, default false), `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs` (`AddEncinaOpenTelemetry` copies the flags to `EncinaTracingOptions`; `WithEncina` adds `tracing.AddSource("Encina.Pipeline")`), the OpenTelemetry `PublicAPI.Unshipped.txt`, `.github/coverage-manifest/Encina.OpenTelemetry.json`; changelog `184-pipeline-tracing-options.added.md` through `mechanical-fixer`.
2. Tests (new): `tests/Encina.UnitTests/OpenTelemetry/PipelineTracingOptionsTests.cs` (flags reach the core options; the source is subscribed; defaults off), `tests/Encina.IntegrationTests/Infrastructure/OpenTelemetry/PipelineTraceTests.cs` is not required: the unit test uses a real SDK builder and a test processor (no external system).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Issue #184 plan docs/plans/request-tracing-implementation-plan-184.md, phase 2. Phase 1 merged.

TASK:
1. Add the three flags to EncinaOpenTelemetryOptions, copy them to EncinaTracingOptions in AddEncinaOpenTelemetry, subscribe Encina.Pipeline in WithEncina.
2. Unit tests with a real SDK builder and a test BaseProcessor<Activity>; PublicAPI, manifest, changelog through mechanical-fixer.

KEY RULES:
- Defaults off. Do not replace a host's own EncinaTracingOptions registration (TryAdd semantics).

REFERENCE FILES:
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs
```

</details>

---

### Phase 3: Documentation (pull request D1)

<details>
<summary><strong>Tasks</strong></summary>

1. `docs/features/request-tracing.md` through `docs-writer` (how-to): the span tree, the two flags, naming, what is never recorded.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Issue #184 plan docs/plans/request-tracing-implementation-plan-184.md, phase 3, documentation only.

TASK:
1. Write docs/features/request-tracing.md (how-to). Verify identifiers in src/.

KEY RULES:
- encina-docs skill; no emojis; no hand-typed coverage figures.

REFERENCE FILES:
- docs/architecture/adr/054-observability-conventions-and-trace-propagation.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| OpenTelemetry semantic conventions | Internal span names are low cardinality; status Error with `error.type` | Span names, status |
| ADR-054 | Settled points 2 and 7, D3, D4 | Scope and rules |
| ADR-062 | Error category contract | None created (row C1) |
| `AGENTS.md` section 3 | Messages never reach telemetry | Attributes |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `EncinaDiagnostics` | `src/Encina/Diagnostics/EncinaDiagnostics.cs` | Existing request span |
| `PipelineBuilder<,>` | `src/Encina/Pipeline/PipelineBuilder.cs` | Span start points |
| `ActivityStatusDescriptions` | `src/Encina/Diagnostics/ActivityStatusDescriptions.cs` | Status text |
| `WithEncina` | `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs` | Subscription |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| none | none | Spans only; no log event is added (the dispatcher already logs request start and outcome); nothing taken from 5970-5999 |

### Estimated File Count

| PR | Content | New src | Changed src | New tests | Edited tests | Manifest | Other | Files | Lines (est.) |
|----|---------|---------|-------------|-----------|--------------|----------|-------|-------|--------------|
| T1 | Core options and pipeline spans | 2 | 4 (`ServiceCollectionExtensions`, `PipelineBuilder`, dispatcher, PublicAPI) | 3 | 0 | 1 | 1 (changelog) | 11 | ~620 |
| T2 | OpenTelemetry options | 0 | 3 (options, `ServiceCollectionExtensions`, PublicAPI) | 1 | 0 | 1 | 1 | 6 | ~230 |
| D1 | Documentation | 0 | 0 | 0 | 0 | 0 | 1 | 1 | ~110 |
| **Total** | | **2** | **7** | **4** | **0** | **2** | **3** | **18** | **~960** |

The columns sum (2 + 7 + 4 + 0 + 2 + 3 = 18; 11 + 6 + 1 = 18). The pull request of this plan is one file.

| File | Unit | Guard | Contract | Property | Justification |
|------|------|-------|----------|----------|---------------|
| `Diagnostics/PipelineDiagnostics.cs` | 95 | 70 | 0 | 0 | Start, complete, error and off paths run with a listener; guard covers the null type; internal static, no contract |
| `Diagnostics/EncinaTracingOptions.cs` | 90 | 0 | 0 | 0 | Property defaults and registration; no parameters to guard |
| `Pipeline/PipelineBuilder.cs` | current target kept | current target kept | 0 | 0 | The changed methods run in `PipelineSpanTests` |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #184 - optional behavior and handler spans - in two code pull requests and one documentation pull request.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0; edit source only with Edit/Write; PublicAPI, manifests, changelog through mechanical-fixer; docs through docs-writer.
- ADR-054: status carries the code or exception type only; zero cost without a listener; tenant tag is #2676's.

IMPLEMENTATION OVERVIEW:
1. T1: EncinaTracingOptions, PipelineDiagnostics (source Encina.Pipeline), spans in PipelineBuilder, correlation id on Encina.Send (opt-in).
2. T2: OpenTelemetry flags copied to the core options; WithEncina subscribes the source.
3. D1: docs/features/request-tracing.md.

KEY PATTERNS:
- Real ActivityListener tests; DI test with ValidateOnBuild and ValidateScopes; guard tests for new public members.
- Do not fix #1789 here.

REFERENCE FILES:
- src/Encina/Pipeline/PipelineBuilder.cs, docs/plans/request-tracing-implementation-plan-184.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Spans are real-time |
| 2 | OpenTelemetry | ✅ Phases 1-2 | This is the tracing work |
| 3 | Structured Logging | ❌ N/A | The dispatcher already logs request start and outcome; no new event |
| 4 | Health Checks | ❌ N/A | No dependency |
| 5 | Validation | ❌ N/A | Booleans only |
| 6 | Resilience | ❌ N/A | No external call |
| 7 | Distributed Locks | ❌ N/A | No shared state |
| 8 | Transactions | ❌ N/A | Span lifecycle is independent of transactions |
| 9 | Idempotency | ❌ N/A | Each execution is a span |
| 10 | Multi-Tenancy | ⏭️ #2676 | Tenant attribute through the shared helper when multi-tenancy is on |
| 11 | Module Isolation | ⏭️ Follow-up issue file | `module-id-on-request-telemetry.md` (written by the #174 plan) |
| 12 | Audit Trail | ❌ N/A | Telemetry is not an audit record |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| ADR-054 accepted | ✅ On `main` | |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Left outcome bug in activity behaviors | #1789 | Open; fix first or alongside |
| Trace context on the request | #186, #132 | Parenting from a received `traceparent` |
| Sampling | #183 | Same creation attribute |
| Error recording helper | #185 | Used for status of behavior spans |

---

## Next Steps

1. Link this plan from #184 and ask the maintainer to answer Design Choices 1 and 3 and register row C7 when work starts.
2. Implement T1, T2, D1; when #184 is picked up ask that its acceptance criteria be amended (Design Choices 1 and 4).
