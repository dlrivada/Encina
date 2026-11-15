# Implementation Plan: `Encina.FeatureFlags` — Feature Flags / Feature Toggles Integration (umbrella)

> **Issue**: [#345](https://github.com/dlrivada/Encina/issues/345)
> **Type**: Feature
> **Complexity**: Medium (4 phases, no database provider)
> **Estimated Scope**: about 2,850 changed lines in 4 pull requests, tests, manifests and PublicAPI files included (table "Pull request split" under Research; each pull request is at most 15 files and 1,500 lines)

---

## Summary

Add feature flags to Encina's CQRS pipeline: attributes on requests (`[FeatureFlag]`, `[FeatureFlags]`, `[FeatureVariant]`), a pipeline behavior that gates or redirects a request by flag, tenant-aware and contextual filters on top of `Microsoft.FeatureManagement`, and the family documentation (ADR). Nothing of it exists in `src/` today.

The maintainer fixed the foundation on 2026-03-18 (comment on #345): `Microsoft.FeatureManagement` is the engine and Encina adds the CQRS layer (attributes, pipeline behavior, multi-tenancy context). Issue #361 was closed as a duplicate on 2026-10-06 and its fallback handlers and per-tenant filters were carried here.

**This plan is the umbrella of the family.** The delivery order and the owner of each shared piece (same table in #300, #451 and #604):

| Order | Piece | Owner |
|---|---|---|
| 1 | Package `Encina.FeatureFlags`, `IFeatureFlagProvider`, errors (each with an `ErrorCategory`, ADR 062), core EventId range 7300-7319, rule evaluator, cache decorator, stores, Redis, Unleash | #604 |
| 2 | `[FeatureFlag]` (single flag), `FeatureFlagPipelineBehavior<,>`, metadata cache, `FeatureFlagContext.From(IRequestContext)`, the `Microsoft.FeatureManagement` provider, `AddEncinaFeatureFlags` extension for it | #300 |
| 3 | `[FeatureFlags]` (AND/OR), `[FeatureVariant]`, handler redirection (fallback and variants), `TenantFilter`, `ContextualFilter`, `UseCustomProvider<T>()`, evaluation metrics, the family ADR | **#345 (this plan)** |
| 4 | `Encina.FeatureFlags.OpenFeature` and `.ConfigCat` | #451 |

**Affected packages**: `Encina.FeatureFlags` (extended; created by #604), `Encina.AspNetCore` (documentation example only: no change needed, the user comes from `IRequestContext.Identity`).

**Provider category**: none (the providers are #604, #451 and #2694; the built-in provider here is `Microsoft.FeatureManagement` reading `appsettings`).

**Error contract**: ADR 062 (error category contract, merged in PR #3599) is on `main`. This plan adds no error code of its own: it uses `encina.featureflags.disabled` and `provider_failed` from the `FeatureFlagErrors` of #604 (created with their `ErrorCategory`).

**Observability names**: the issue lists the counter `encina.featureflags.evaluations` and the span `encina.featureflags.evaluate`; they are the provider-level instruments created by #604. The gate adds `encina.featureflags.gate_evaluations` and `encina.featureflags.gate`, named apart so that a gate decision is not confused with a provider evaluation (this is the recorded rename).

---

## Design Choices

<details>
<summary><strong>1. Attribute model — request-type attributes with metadata cached per request type</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `[FeatureFlag]`, `[FeatureFlags]`, `[FeatureVariant]` on the request type; read once per `TRequest` into a static generic cache** | The behavior is generic over `TRequest` and needs no handler lookup; no reflection per request; same shape as `[RequireConsent]` | The issue sketch put the attributes on handler classes |
| **B) Attributes on handler classes (as in the issue sketch)** | Reads naturally next to the handler | The behavior would have to find the handler type; the community sketch scanned assemblies on every request, which the maintainer rejected |
| **C) Fluent registration only** | No reflection | Easy to forget; far from the request |

### Chosen Option: **A — Attributes on the request type, cached per type**

### Rationale

- The maintainer's comment of 2026-03-18 asks for cached attribute metadata instead of reflection per request, and confirms `[FeatureFlag]` and the pipeline behavior as part of the design.
- `ConsentRequiredPipelineBehavior<,>` already does it this way: `AttributeCache` keyed by request type (`src/Encina.Compliance.Consent/ConsentRequiredPipelineBehavior.cs:57`).
- A request has exactly one handler, so nothing is lost by putting the attribute on the request.

**Class:** Settled
**Settled by:** Maintainer comment of 2026-03-18 on #345 (attribute names, cached metadata); `src/Encina.Compliance.Consent/ConsentRequiredPipelineBehavior.cs:57,106` (convention)

</details>

<details>
<summary><strong>2. Filters — reuse the Microsoft.FeatureManagement built-ins, add only the Encina-specific ones</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Use the library's percentage, time-window and targeting filters; add `TenantFilter` (enable per tenant, carried from #361) and `ContextualFilter` (custom predicate over `IRequestContext`)** | No reinvention; the library's targeting already buckets deterministically | Encina's filters depend on the library's filter interfaces |
| **B) Write `PercentageFilter`, `TimeWindowFilter` and `TargetingFilter` of the issue sketch** | Full control, same behavior for all providers | Duplicates the library (same filter names); the maintainer chose the library to avoid exactly this |

### Chosen Option: **A — Built-ins from the library plus `TenantFilter` and `ContextualFilter`**

### Rationale

- "That way we get the mature filter infrastructure without reinventing it" (maintainer, 2026-03-18).
- Providers without an engine of their own (database, Redis) use `FeatureFlagRuleEvaluator` from #604, which carries the SHA256 bucketing and the `TimeProvider` rule the maintainer asked for.
- The filter evaluation context is `FeatureFlagContext` (built by `FeatureFlagContext.From(IRequestContext)` in #300), so tenant and user reach the filters.

**Class:** Settled
**Settled by:** Maintainer comment of 2026-03-18 on #345 (library as foundation, SHA256 bucketing, `TimeProvider`); #361 carry-over comment (per-tenant filters)

</details>

<details>
<summary><strong>3. Disabled feature — a typed Left, never a default and never an exception</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Left` with `encina.featureflags.disabled`** | Explicit; Railway Oriented Programming; the caller sees why | Callers handle one more error |
| **B) `Skip`: return a default response (the issue's third outcome)** | Handler quietly not run | Reports success for a request that did nothing |
| **C) Throw `FeatureDisabledException`** | Familiar | Exceptions for business logic |

### Chosen Option: **A — Typed Left**

### Rationale

- AGENTS.md section 3 forbids exceptions for business logic and forbids reporting success when work was not done.
- The error carries the flag name in `details` only; the code is stable (`FeatureFlagErrors.Disabled`, #604 Phase 1).

**Class:** Settled
**Settled by:** AGENTS.md section 3 (Railway Oriented Programming; errors never swallowed); ADR-001

</details>

<details>
<summary><strong>4. Handler redirection — what happens for a fallback or a variant</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The behavior resolves an alternative `IRequestHandler<TRequest,TResponse>` named in the attribute (`Fallback = typeof(...)`, `[FeatureVariant("exp", "B", typeof(...))]`) and calls it instead of `nextStep`** | One mechanism for fallback and A/B variants; the caller sees a result; no change to the dispatcher | The redirected handler skips every step after the gate: compliance gates, validation, transaction and idempotency of the stages after `Authorization` do not run for the redirected path (a security consequence, below) |
| **B) No redirection: the `Left` goes to the caller; variants are evaluated by the handler through `IFeatureFlagProvider`** | Simplest; no hidden skipping of behaviors | The carried fallback handlers of #361 are not delivered; callers must degrade themselves |
| **C) The gate records the chosen alternative handler and the dispatcher invokes it at the end of the pipeline** | Every later behavior still runs | Needs a hook in the core dispatcher that no plan of the family builds; a wider change than the issue asks for |

### Chosen Option: **A — Alternative handler named in the attribute (recommended, pending the maintainer)**

### Rationale

- A wins when old and new handlers coexist during a rollout (the #361 scenario the maintainer approved to carry). B wins when callers can degrade themselves and the simplicity matters more; C wins when the alternative path must keep every later guarantee and a core change is acceptable.
- **Security consequence of A, stated as a rule of this plan**: the alternative handler is a second primary handler for the same request and carries its own validation and compliance checks, because the steps after the gate do not run for it. The redirection tests (Phase 2) assert which stages are skipped, and the guide (Phase 4) says it in the same words.
- The fallback type must implement `IRequestHandler<TRequest,TResponse>` for the same request; the registration validator fails at startup otherwise.
- The same decision applies to #300's `Fallback` outcome; #300 follows this answer.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. How the four feature-flag issues fit together</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) #345 is the umbrella; #300 is the first slice (single-flag gate), #604 owns the provider foundation, #451 adds two providers** | Each piece stays within the 15-file pull request limit; ordered by dependency | Overlapping descriptions need the ownership table |
| **B) Merge #300 into #345** | One gate proposal | One larger issue; the single-flag gate would wait for variants and redirection |

### Chosen Option: **A — Umbrella with ordered slices (recommended, pending the maintainer)**

### Rationale

- A wins when the pieces are built in separate pull requests over time, which the size rule requires anyway; B wins if one worker builds the whole gate in one go.
- Under B this plan's phases 1-3 still apply; only #300's plan would be folded in.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Pipeline position — declared stage, no new enum member</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `[PipelineStage(PipelineStage.Authorization, Order = 100)]`: after identity and after the authorization steps that keep the default order 0, before compliance, validation and the transaction** | A disabled feature is rejected before any work; targeting has the identity; an unauthorized caller never learns a flag state; no change to the public enum | The stage is documented for authorization, a feature gate is a neighbour of it |
| **B) A new `PipelineStage.FeatureGate` member** | Exact name | Extends a public contract for one consumer |
| **C) Default `Application` stage** | Nothing to declare | Runs inside the transaction and idempotency steps, so a disabled feature opens a transaction first |

### Chosen Option: **A — Authorization stage, order 100 (recommended, pending the maintainer)**

### Rationale

- `PipelineStage.cs` documents `Authorization = 500` as the stage after identity, and `Application = 1500` as "inside every gate, transaction and idempotency step". Steps run in ascending (stage, order) and "lower is further out", with the default `Order` of 0 (`PipelineStageAttribute.cs`): `Order = 100` therefore runs after the authorization behaviors with a lower or default order, and leaves room for ones that must run later in the stage.
- A wins when the gate must come after authorization and before the work; B wins if a feature gate deserves its own place in the closed stage vocabulary; C is out for the reason in its Cons.
- The remark in `PipelineStage.cs` (phase note of #2184) says the stage vocabulary is declared but not yet applied by the pipeline builders: until it is, the position follows registration order. Phase 1 states that dependency in the XML docs and the guide.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Registration — one <code>AddEncinaFeatureFlags</code>, providers by DI registration</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AddEncinaFeatureFlags(options)` plus `UseCustomProvider<T>()`; each provider package adds its own `AddEncinaFeatureFlags{Provider}`; no `UseAzureAppConfiguration`** | Switching provider means changing the registration; same shape as `AddEncinaConsent` | Two calls for a non-default provider |
| **B) A provider enum in the options (`config.Provider = ...`)** | One call | Core would know every provider; AGENTS.md provider coherence |

### Chosen Option: **A — Provider by registration**

### Rationale

- AGENTS.md section 3 (provider coherence): providers share the interface and differ only in the registration.
- `UseAzureAppConfiguration` of the original sketch moved to #2694 (paid cloud account).

**Class:** Settled
**Settled by:** AGENTS.md section 3 (provider coherence; registration completeness); issue #345 body (Azure App Configuration moved to #2694)

</details>

<details>
<summary><strong>8. The gate when the provider fails</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Fail closed: a `Left` from the provider is returned as `provider_failed` (the request does not run); `FeatureFlagsOptions.DefaultWhenUnavailable = true` is the explicit opt-out: the flag is then treated as enabled, and every such decision is logged by error code** | A flag outage never enables a feature by accident (gates fail closed, AGENTS.md section 3); the opt-out is explicit and logged | An outage of the flag backend fails the flagged requests unless the application opts out |
| **B) Fail open by default: treat the flag as enabled when the provider fails** | The application keeps working during an outage | A kill switch or a not-yet-released feature turns on during the outage |

### Chosen Option: **A — Fail closed, explicit logged opt-out (recommended, pending the maintainer)**

### Rationale

- A wins for kill switches, compliance and unreleased features; B wins when flags only tune behavior and availability matters more than the flag value. The default of `DefaultWhenUnavailable` is `false` (#604 Design Choice 13); this gate is where it takes effect.
- The log carries the error code (never the message): EventId 7307 and 7309 of #300 (provider failed, default applied).

**Class:** Dilemma

</details>

<details>
<summary><strong>9. Attribute combinations and variant fall-through</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) When a request carries several attributes, all gates must pass: `[FeatureFlag]` first, then `[FeatureFlags]` (`RequireAll` defaults to `true`; names evaluated sequentially in declared order, short-circuiting on the first decisive result), then `[FeatureVariant]` selection; a variant name that matches no `[FeatureVariant]` continues to `nextStep` (the default handler); a provider `Left` during variant selection follows Design Choice 8** | Predictable: the default path is the control; an experiment never blocks a request | A mistyped variant name silently runs the default handler (logged at 7313) |
| **B) An unknown variant is a `Left` `not_found`** | A mistyped variant fails loudly | A provider that adds a new variant name breaks existing requests |

### Chosen Option: **A — All gates pass; unknown variant runs the default handler (recommended, pending the maintainer)**

### Rationale

- A wins for gradual experiments where the default handler is the safe path; B wins when every variant name is controlled by the application and a mismatch is a defect.
- An empty or whitespace flag name, or an empty `Names` array, is a programmer error: the attribute constructor throws `ArgumentException` (a guard on code, not a business rule), and the startup validator (Phase 2) instantiates the attributes of every request type it finds among the registered `IRequestHandler<,>` descriptors so the failure happens at start.

**Class:** Dilemma

</details>

<details>
<summary><strong>10. Missing context in the filters</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A missing tenant, a missing context or a failing predicate denies the feature, and the denial is logged by error code** | The only behavior that complies with "gates fail closed" | A single-tenant application without a tenant id must list no tenant filter |
| **B) A missing tenant allows the feature** | Fewer surprises for single-tenant applications | Fails open on missing context |

### Chosen Option: **A — Deny on missing context**

### Rationale

- Compliance and security gates fail closed: when context is missing or a lookup fails, the gate denies; the opt-out is explicit and logged (AGENTS.md section 3). The signature of the predicate contract is Design Choice 12.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (gates fail closed); SPEC-002 DEC-006

</details>

<details>
<summary><strong>11. Identifiers in telemetry</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The gate metrics and span carry the flag name and the outcome only; no tenant id, no user id** | Bounded metric cardinality; no tenant identifier in telemetry | An operator cannot filter gate outcomes by tenant in metrics |
| **B) The tenant id as a span attribute only** | Trace search by tenant | A tenant identifier in telemetry storage; depends on sampling and retention |
| **C) The tenant id as a metric tag** | Per-tenant dashboards | Unbounded cardinality in a multi-tenant system |

### Chosen Option: **A — No tenant or user id in tags (recommended, pending the maintainer)**

### Rationale

- A wins when telemetry must stay bounded and free of identifiers; B wins when per-tenant diagnosis matters and the telemetry store is treated as tenant data.
- Logs and activity tags never carry `EncinaError.Message` or personal data (AGENTS.md section 3).

**Class:** Dilemma

</details>

<details>
<summary><strong>12. Signature of the <code>IFeatureFlagPredicate</code> contract</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ValueTask<Either<EncinaError, bool>> EvaluateAsync(FeatureFlagContext, CancellationToken)`; a `Left` denies and is logged by error code** | Failure is explicit and never swallowed; matches the operations of the family | A predicate author returns an `Either` |
| **B) `ValueTask<bool>`; the filter catches an exception, denies and logs the exception type** | Simpler for authors; wins for trivial predicates | Failure is signalled by an exception; business failures cannot carry a code |

### Chosen Option: **A — `Either` result (recommended, pending the maintainer)**

### Rationale

- A wins when predicates call stores or services that can fail; B wins when predicates are pure and trivial. Both deny on failure, so the gate stays closed either way (AGENTS.md section 3).
- The contract has a `Name` and `EvaluateAsync`; it is a new public interface (Phase 3).

**Class:** Dilemma

</details>

---

## Implementation Phases

Each phase is one pull request: it carries its own unit, guard and (where it applies) contract and property tests, its coverage-manifest entries (`.github/coverage-manifest/Encina.FeatureFlags.json`), its `PublicAPI.Unshipped.txt` lines, and the diagnostics (metrics outcomes, EventIds) of the code it adds, so no phase uses a log message or metric that a later one creates. Every new file under `src/` gets per-flag targets in the manifest, demanding and realistic, each with a one-sentence justification; a target of 0 needs a reason (for example the property flag of a file with no invariant worth a property test). The table "Pull request split" under Research lists the files and lines of each phase.

### Phase 1: Multi-Flag Attribute

> **Goal**: `[FeatureFlags("A", "B", RequireAll = false)]` evaluated by the behavior from #300.

<details>
<summary><strong>Tasks</strong></summary>

1. `Attributes/FeatureFlagsAttribute.cs` (`params string[] Names`, `RequireAll`, default `true`; the constructor rejects an empty array and empty or whitespace names with `ArgumentException`, Design Choice 9).
2. Extend `Gate/FeatureFlagMetadata.cs` (from #300) to hold the multi-flag set.
3. Extend `Gate/FeatureFlagPipelineBehavior.cs`: evaluate each name through `IFeatureFlagProvider` in declared order, short-circuiting on the first decisive result; a provider `Left` follows Design Choice 8 (the helper of #300); the XML docs state the position dependency of Design Choice 6.
4. Tests: `FeatureFlagsAttributeTests` (AND, OR, defaults, rejected names), `FeatureFlagBehaviorMultiFlagTests` (one `Left`, short-circuit order, both attributes on one request), a guard test class, the #300 behavior and metadata tests updated for the changed metadata type; manifest entries and `PublicAPI.Unshipped.txt`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
- Issue #345, Phase 1. #604 (package, contract) and #300 (single-flag behavior) are merged.
TASK:
- Add [FeatureFlags] and extend the metadata and the behavior as in the Phase 1 Tasks, with tests, manifest entries and PublicAPI lines.
KEY RULES:
- Metadata read once per request type; no reflection per request.
- A Left from the provider is never swallowed (AGENTS.md section 3); fail closed unless DefaultWhenUnavailable says otherwise (Design Choice 8).
- XML docs and PublicAPI.Unshipped.txt for every public member.
- At most 15 files and 1500 lines.
REFERENCE FILES:
- src/Encina.FeatureFlags/Gate/FeatureFlagPipelineBehavior.cs (behavior from #300)
- src/Encina.Compliance.Consent/ConsentRequiredPipelineBehavior.cs
```

</details>

---

### Phase 2: Handler Redirection — Fallback and Variants

> **Goal**: `Fallback = typeof(...)` and `[FeatureVariant(...)]` (Design Choice 4; if the maintainer chooses B, this phase becomes a documentation-only phase and `[FeatureVariant]` is evaluated by handlers).

<details>
<summary><strong>Tasks</strong></summary>

1. `Attributes/FeatureVariantAttribute.cs` (repeatable: feature name, variant name, handler type).
2. `Attributes/FeatureFlagAttribute.cs`: the `Fallback` property (type; added to the attribute of #300).
3. `Redirection/FeatureFlagHandlerRedirector.cs`: resolves the named handler from `IServiceProvider`, calls `Handle(request, cancellationToken)`, returns its `Either`; the gate behavior and the metadata call it.
4. Variant selection through `IFeatureFlagProvider.GetVariationAsync<string>`; an unknown variant continues to `nextStep` (Design Choice 9).
5. `Redirection/FeatureFlagRedirectionValidator.cs`: startup validation (`ValidateOnStart`) over the request types found in the registered `IRequestHandler<,>` descriptors: every named type implements `IRequestHandler<TRequest,TResponse>` for its request and is registered; otherwise start fails.
6. Diagnostics of this phase: the outcomes `redirected` and `variant_selected` on `encina.featureflags.gate_evaluations` and on the span `encina.featureflags.gate` (`Diagnostics/FeatureFlagGateMetrics.cs` of #300); `[LoggerMessage]` EventIds in the gate log class of #300 (`Diagnostics/FeatureFlagGateLogMessages.cs`, inside 7310-7315 of #345): 7310 request redirected to the fallback handler, 7311 variant selected, 7312 redirect handler missing at run time, 7313 unknown variant fell through to the default handler. Tags carry the flag name and the outcome only (Design Choice 11).
7. Tests: redirector, validator (with the DI test: `ValidateOnBuild`, `ValidateScopes`), behavior redirection tests that assert the skipped stages of the redirected path (Design Choice 4), guard tests; manifest entries and PublicAPI.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
- Issue #345, Phase 2; the redirection answer is Design Choice 4. Phase 1 is merged.
TASK:
- Implement FeatureVariantAttribute, the Fallback property, the redirector, the startup validator, the diagnostics of this phase and their tests.
KEY RULES:
- The redirected handler runs instead of nextStep; behaviors after the gate are skipped, documented and asserted by tests; the alternative handler carries its own validation.
- A Left from the alternative handler is returned unchanged.
- EventIds 7310-7313 only, sequential, in the gate log class of #300; log error codes and flag names, never messages, tenant or user ids.
- At most 15 files and 1500 lines.
REFERENCE FILES:
- src/Encina/Abstractions/IRequestHandler.cs
- src/Encina.FeatureFlags/Gate/FeatureFlagPipelineBehavior.cs
- src/Encina/Pipeline/Ordering/PipelineStage.cs
```

</details>

---

### Phase 3: Tenant and Contextual Filters, Builder and `UseCustomProvider<T>()`

> **Goal**: Per-tenant enablement and request-context predicates for the `Microsoft.FeatureManagement` provider, and the complete registration surface.

<details>
<summary><strong>Tasks</strong></summary>

1. `Filters/TenantFilter.cs`: contextual filter over `FeatureFlagContext`; parameters from configuration (`Tenants`: list of allowed tenant ids); a missing tenant denies and is logged (Design Choice 10).
2. `Abstractions/IFeatureFlagPredicate.cs` (new public contract of Design Choice 10) and `Filters/ContextualFilter.cs`: runs registered predicates by name over `FeatureFlagContext`; an unknown predicate name or a `Left` denies and is logged by error code.
3. `FeatureFlagsBuilder.cs` with `AddFilter<T>()` and `UseCustomProvider<T>()` (replaces the default `Microsoft.FeatureManagement` provider that `AddEncinaFeatureFlags` adds with `TryAdd*`, in any registration order: the default is added only when no provider is registered, and the custom registration removes a default added earlier; a provider the application registered itself is never replaced); `AddEncinaFeatureFlags` returns it and registers both filters.
4. Diagnostics of this phase: EventIds 7314 contextual predicate failed, 7315 tenant filter denied (no tenant) in the gate log class; no tenant or user id in the message arguments.
5. Registration rule: a provider registered through `UseCustomProvider<T>()` wins over the `Microsoft.FeatureManagement` provider that `AddEncinaFeatureFlags` registers by default, in any registration order, and a provider the application registered itself is never overridden. The caching decorator of #604 stays opt-in through `FeatureFlagsOptions.CacheEnabled` (default `false`); this plan does not enable it.
6. Tests: `TenantFilterTests`, `ContextualFilterTests`, `FeatureFlagsBuilderRegistrationTests` (DI with `ValidateOnBuild` and `ValidateScopes`, both registration orders), guard tests, property tests of `TenantFilter` (never enables an unlisted tenant); manifest entries and PublicAPI.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
- Issue #345, Phase 3. The library's built-in filters are used as they are (Design Choice 2). Phases 1-2 are merged.
TASK:
- Implement TenantFilter, IFeatureFlagPredicate, ContextualFilter, FeatureFlagsBuilder (AddFilter, UseCustomProvider) and their registration, diagnostics and tests.
KEY RULES:
- Missing tenant or context denies the feature; a predicate Left denies; never swallowed silently (logged by code).
- No tenant id or user id in logs or tags (Design Choice 11).
- EventIds 7314-7315 only; registration completeness proven by a DI test with ValidateOnBuild and ValidateScopes.
- At most 15 files and 1500 lines.
REFERENCE FILES:
- src/Encina.FeatureFlags/Model/FeatureFlagContext.cs
- src/Encina/Abstractions/IRequestContext.cs
- src/Encina.Compliance.Consent/ServiceCollectionExtensions.cs
```

</details>

---

### Phase 4: Contract Tests, Benchmarks, Justifications and the Family ADR

> **Goal**: The remaining test types, and the documentation and ADR of the whole family.

<details>
<summary><strong>Tasks</strong></summary>

0. Manifest: the contract-flag targets (with one-sentence justifications) of the Phase 1-3 files are entered in this phase's edit of `.github/coverage-manifest/Encina.FeatureFlags.json`, the pull request that carries the contract tests that meet them.
1. Contract: extend `FeatureFlagGateContractTests` (created by #300 Phase 3) with the multi-flag and redirection scenarios, run over every `IFeatureFlagProvider` reachable without Docker (the library provider of #300 and the in-memory-store provider of #604).
2. Benchmark: add the redirection and variant scenarios to the gate benchmark class of #300, through `BenchmarkSwitcher` (cached provider, hot path).
3. Justification files: edit the load and integration files created by #300 Phase 4 (`tests/Encina.LoadTests/FeatureFlags/FeatureFlagGate.md`, `tests/Encina.IntegrationTests/FeatureFlags/FeatureFlagGate.md`) so their reasons also cover redirection and the filters (stateless decision, no concurrent shared state; configuration-backed, no external service; the stores and providers have their integration tests in #604). Property tests exist here (Phase 3), so the property file of #300 is edited to point to them.
4. ADR for the feature-flag architecture (provider abstraction, relation to `Microsoft.FeatureManagement`, redirection, ownership of #300/#345/#451/#604; it also delivers the ADR acceptance criterion of #604, D1 to D3 of that issue); ADR number assigned when the ADR pull request opens.
5. Usage guide under `docs/features/` (attributes, filters, appsettings example, tenant targeting, the redirected-path rule of Design Choice 4); `changelog.d/345-feature-flags.added.md`, `docs/INVENTORY.md`, `ROADMAP.md` (#345 entry), package README.
6. `dotnet build --configuration Release` with 0 warnings; tests pass; per-flag coverage of every file of Phases 1-3 at its target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
- Issue #345, Phase 4; documentation goes through docs-writer, changelog through mechanical-fixer. Phases 1-3 are merged.
TASK:
- Write the contract tests, the benchmark scenarios, the edits of the justification files of #300, the ADR, the guide, the fragment and the INVENTORY/ROADMAP updates.
KEY RULES:
- Never edit CHANGELOG.md; coverage figures only through covref markers.
- State the provider coverage honestly: SaaS providers are #2694, outside 1.0.
- This pull request carries Fixes #345.
REFERENCE FILES:
- docs/architecture/adr/index.md
- changelog.d/README.md
- docs/en/guides/TESTING.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | What it asks of this feature |
|--------|-----------|------------------------------|
| Maintainer comment 2026-03-18 on #345 | `Microsoft.FeatureManagement` as foundation; SHA256 bucketing; `TimeProvider`; cached metadata | Design Choices 1 and 2 |
| #361 carry-over comment (2026-10-06) | Fallback handlers and per-tenant filters | Design Choices 2 and 4 |
| AGENTS.md section 3 | ROP; fail closed; provider coherence; no personal data in logs | Design Choices 3, 7, 8, 10 and 11 |
| ADR 062 | Error category contract (merged in PR #3599) | Error codes come from #604 with their category |
| ADR-018 | 12 cross-cutting functions | Matrix below |
| Martin Fowler, Feature Toggles | Release, experiment, ops and permission toggles | Scenarios the attributes serve |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `IPipelineBehavior<,>` | `src/Encina/Abstractions/IPipelineBehavior.cs` | Behavior contract (`Handle(request, context, nextStep, ct)`) |
| `PipelineStage`, `PipelineStageAttribute` | `src/Encina/Pipeline/Ordering/` | Stage declaration (lower is further out) |
| `IRequestContext` (`TenantId`, `UserId`), `IRequestContextAccessor` | `src/Encina/Abstractions/` | Filter context |
| Attribute-cache behavior pattern | `src/Encina.Compliance.Consent/ConsentRequiredPipelineBehavior.cs:57` | Metadata cache |
| `IFeatureFlagProvider`, rule evaluator, cache | `Encina.FeatureFlags` (#604) | Evaluation |
| `FeatureFlagContext.From`, `[FeatureFlag]`, behavior, gate diagnostics | `Encina.FeatureFlags` (#300) | Extended here |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.FeatureFlags` | 7300-7319 (registered by #604 Phase 3) | #604 7300-7305, #300 7306-7309, **#345 7310-7315** (7310-7313 in Phase 2, 7314-7315 in Phase 3); 7316-7319 unallocated; #451 owns 7330-7339 |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Multi-flag (Phase 1) | 9 | Attribute, metadata, behavior, tests, manifest, PublicAPI |
| Redirection (Phase 2) | 15 | Attributes, redirector, validator, diagnostics, tests |
| Filters and builder (Phase 3) | 13 | Filters, predicate contract, builder, diagnostics, tests |
| Contract tests, benchmark, ADR and docs (Phase 4) | 12 | Manifest edit (contract targets of Phases 1-3), contract test edit, benchmark edit, 3 justification file edits, ADR, guide, README, fragment, INVENTORY, ROADMAP |
| **Total** | **49** | Four pull requests |

#### Pull Request Split

Files include tests, manifests, `PublicAPI` files, existing tests edited for a changed type and the changelog fragment; lines are estimates re-measured when each pull request opens. Every pull request is at most 15 files and 1500 lines.

| Phase | Content | Files | Lines |
|-------|---------|------:|------:|
| 1 | `FeatureFlagsAttribute`, metadata and behavior edits, manifest, PublicAPI, unit and guard tests, one existing #300 test file edited | 9 | 450 |
| 2 | `FeatureVariantAttribute`, `Fallback`, redirector, validator, metadata, behavior, service registration, gate metrics and log edits (7310-7313), manifest, PublicAPI, 4 test files | 15 | 1,000 |
| 3 | `TenantFilter`, `ContextualFilter`, `IFeatureFlagPredicate`, `FeatureFlagsBuilder`, registration and log edits (7314-7315), manifest, PublicAPI, unit, DI, guard and property tests | 13 | 800 |
| 4 | Manifest edit (contract targets), contract test edit, benchmark edit, 3 justification file edits, ADR, guide, README, fragment, INVENTORY, ROADMAP | 12 | 600 |
| **Total** | 4 pull requests | **49** | **2,850** |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #345: the umbrella of Encina feature flags (multi-flag, variants, redirection, filters).

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library with Railway Oriented Programming; pre-1.0, no compatibility code. ADR 062 (error categories) is merged.
- #604 (package and provider contract) and #300 (single-flag gate) are merged before this work.
- Provider category: none for this plan.

IMPLEMENTATION OVERVIEW:
Phase 1: [FeatureFlags] multi-flag. Phase 2: fallback and variant redirection with its diagnostics (EventIds 7310-7313).
Phase 3: TenantFilter, ContextualFilter, builder and UseCustomProvider (EventIds 7314-7315).
Phase 4: contract tests, benchmarks, justification files, ADR, guide, fragment.

KEY PATTERNS:
- Attributes on the request type, metadata cached per type.
- Disabled feature returns a Left (encina.featureflags.disabled); a provider failure fails closed unless the explicit logged opt-out is set.
- Microsoft.FeatureManagement built-in filters are used as they are.
- Fail closed on missing tenant or context.
- Each phase is one pull request of at most 15 files and 1500 lines with its own tests, manifest entries and diagnostics.

REFERENCE FILES:
- docs/plans/feature-flags-implementation-plan-345.md (this plan)
- src/Encina.Compliance.Consent/ConsentRequiredPipelineBehavior.cs
- src/Encina/Pipeline/Ordering/PipelineStage.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Phase 3 | Uses the cache decorator of #604, opt-in through `FeatureFlagsOptions.CacheEnabled` (default `false`); the issue marked it Defer, the plan integrates it because the decorator exists in #604 |
| 2 | OpenTelemetry | ✅ Phase 2 | Gate counter and span on the family meter, flag name and outcome only (Design Choice 11) |
| 3 | Structured Logging | ✅ Phases 2, 3 | EventIds 7310-7315 |
| 4 | Health Checks | ⏭️ Deferred | Provider health check: follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/feature-flag-provider-health-checks.md; same item as #604) |
| 5 | Validation | ✅ Phases 1, 2, 3 | Attribute names validated by the constructor; startup validation of fallback and variant handler types; options validated on start |
| 6 | Resilience | ✅ Phase 1 | The gate fails closed when the provider returns a `Left`, with the logged opt-out of Design Choice 8; provider-side resilience is in #604; the issue marked it Defer, the plan integrates the gate side |
| 7 | Distributed Locks | ❌ N/A | Stateless read |
| 8 | Transactions | ❌ N/A | The gate runs before the transaction stage; no mutation |
| 9 | Idempotency | ❌ N/A | Evaluation is deterministic |
| 10 | Multi-Tenancy | ✅ Phase 3 | `TenantFilter`; tenant in `FeatureFlagContext` (not a tag, Design Choice 11) |
| 11 | Module Isolation | ❌ N/A | Flags are named by the application and shared across modules; the request's module name can be passed as a context attribute to a custom filter or provider (the issue marked it Defer; the family settled on not applicable) |
| 12 | Audit Trail | ⏭️ Deferred | Flag change and evaluation audit: follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/feature-flag-change-audit.md; same item as #604) |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| Package and `IFeatureFlagProvider` | #604 Phases 1-3 | Open |
| Single-flag gate and library provider | #300 | Open |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| OpenFeature and ConfigCat providers | #451 | Build on the same contract |
| SaaS providers | #2694 | Outside 1.0 (SPEC-000 section 6) |
| Multi-tenancy support | #346 | Tenant context source |

---

## Next Steps

1. Review this plan; the Dilemmas (4, 5, 6, 8, 9, 11 and 12) are asked when work on the issue starts.
2. Post the plan link on #345 and cross-reference #300, #451 and #604.
3. Start after #604 Phases 1-3 and #300 are merged.
4. Each phase is one pull request of at most 15 files and 1500 lines; the last carries `Fixes #345`.
