# Implementation Plan: `Encina.FeatureFlags` — Feature Flags / Feature Toggles Integration (umbrella)

> **Issue**: [#345](https://github.com/dlrivada/Encina/issues/345)
> **Type**: Feature
> **Complexity**: Medium (7 phases, no database provider)
> **Estimated Scope**: ~1,100 lines of production code + ~1,800 lines of tests, in about 7 pull requests

---

## Summary

Add feature flags to Encina's CQRS pipeline: attributes on requests (`[FeatureFlag]`, `[FeatureFlags]`, `[FeatureVariant]`), a pipeline behavior that gates or redirects a request by flag, tenant-aware and contextual filters on top of `Microsoft.FeatureManagement`, and the family documentation (ADR). Nothing of it exists in `src/` today.

The maintainer fixed the foundation on 2026-03-18 (comment on #345): `Microsoft.FeatureManagement` is the engine and Encina adds the CQRS layer (attributes, pipeline behavior, multi-tenancy context). Issue #361 was closed as a duplicate on 2026-10-06 and its fallback handlers and per-tenant filters were carried here.

**This plan is the umbrella of the family.** The delivery order and the owner of each shared piece (same table in #300, #451 and #604):

| Order | Piece | Owner |
|---|---|---|
| 1 | Package `Encina.FeatureFlags`, `IFeatureFlagProvider`, errors, EventId ranges 7300-7339, rule evaluator, cache decorator, stores, Redis, Unleash | #604 |
| 2 | `[FeatureFlag]` (single flag), `FeatureFlagPipelineBehavior<,>`, metadata cache, `FeatureFlagContext.From(IRequestContext)`, the `Microsoft.FeatureManagement` provider, `AddEncinaFeatureFlags` extension for it | #300 |
| 3 | `[FeatureFlags]` (AND/OR), `[FeatureVariant]`, handler redirection (fallback and variants), `TenantFilter`, `ContextualFilter`, `UseCustomProvider<T>()`, evaluation metrics, the family ADR | **#345 (this plan)** |
| 4 | `Encina.FeatureFlags.OpenFeature` and `.ConfigCat` | #451 |

**Affected packages**: `Encina.FeatureFlags` (extended; created by #604), `Encina.AspNetCore` (documentation example only: no change needed, the user comes from `IRequestContext.Identity`).

**Provider category**: none (the providers are #604, #451 and #2694; the built-in provider here is `Microsoft.FeatureManagement` reading `appsettings`).

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
| **A) The behavior resolves an alternative `IRequestHandler<TRequest,TResponse>` named in the attribute (`Fallback = typeof(...)`, `[FeatureVariant("exp", "B", typeof(...))]`) and calls it instead of `nextStep`** | One mechanism for fallback and A/B variants; the caller sees a result; no change to the dispatcher | The redirected handler skips the behaviors that run after the gate (validation, transaction); this is documented and tested |
| **B) No redirection: the `Left` goes to the caller; variants are evaluated by the handler through `IFeatureFlagProvider`** | Simplest; no hidden skipping of behaviors | The carried fallback handlers of #361 are not delivered; callers must degrade themselves |

### Chosen Option: **A — Alternative handler named in the attribute (recommended, pending the maintainer)**

### Rationale

- A wins when old and new handlers coexist during a rollout (the #361 scenario the maintainer approved to carry). B wins when callers can degrade themselves and the simplicity matters more.
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
- Under B this plan's phases 1-4 still apply; only #300's plan would be folded in.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Pipeline position — declared stage, no new enum member</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `[PipelineStage(PipelineStage.Authorization, Order = 100)]`: after identity and authorization, before compliance, validation and the transaction** | A disabled feature is rejected before any work; targeting has the identity; no change to the public enum | The stage is documented for authorization, a feature gate is a neighbour of it |
| **B) A new `PipelineStage.FeatureGate` member** | Exact name | Extends a public contract for one consumer |
| **C) Default `Application` stage** | Nothing to declare | Runs inside the transaction and idempotency steps, so a disabled feature opens a transaction first |

### Chosen Option: **A — Authorization stage, order 100**

### Rationale

- `PipelineStage.cs` documents `Authorization = 500` as the stage after identity, and `Application = 1500` as "inside every gate, transaction and idempotency step".
- The `Order` value leaves room for authorization behaviors that must run first.

**Class:** Trivial
**Settled by:** `src/Encina/Pipeline/Ordering/PipelineStage.cs` (stage documentation), `PipelineStageAttribute.cs` (`Order`); example in `src/Encina/Core/EncinaConfiguration.cs:142`

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

---

## Implementation Phases

### Phase 1: Multi-Flag Attribute and Context Extensions

> **Goal**: `[FeatureFlags("A", "B", RequireAll = false)]` evaluated by the behavior from #300.

<details>
<summary><strong>Tasks</strong></summary>

1. `Attributes/FeatureFlagsAttribute.cs` (`params string[] Names`, `RequireAll`, default `true`).
2. Extend `FeatureFlagMetadata<TRequest>` (from #300) to hold the multi-flag set.
3. Extend `FeatureFlagPipelineBehavior<,>`: evaluate each name through `IFeatureFlagProvider` (sequentially, short-circuit on the first decisive result); any `Left` from the provider is returned or mapped by `FeatureFlagsOptions.DefaultWhenUnavailable` (#604, Design Choice 3).
4. Tests: AND, OR, one `Left`, empty list rejected at startup.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
- Issue #345, Phase 1. #604 (package, contract) and #300 (single-flag behavior) are merged.
TASK:
- Add [FeatureFlags] and extend the metadata and the behavior as in the Phase 1 Tasks.
KEY RULES:
- Metadata read once per request type; no reflection per request.
- A Left from the provider is never swallowed (AGENTS.md section 3).
- XML docs and PublicAPI.Unshipped.txt for every public member.
REFERENCE FILES:
- src/Encina.FeatureFlags/ (behavior from #300)
- src/Encina.Compliance.Consent/ConsentRequiredPipelineBehavior.cs
```

</details>

---

### Phase 2: Handler Redirection — Fallback and Variants

> **Goal**: `Fallback = typeof(...)` and `[FeatureVariant(...)]` (Design Choice 4; if the maintainer chooses B, this phase becomes a documentation-only phase and `[FeatureVariant]` is evaluated by handlers).

<details>
<summary><strong>Tasks</strong></summary>

1. `Attributes/FeatureVariantAttribute.cs` (repeatable: feature name, variant name, handler type).
2. `FeatureFlagAttribute.Fallback` (type; added to the attribute of #300).
3. `Redirection/FeatureFlagHandlerRedirector.cs`: resolves the named handler from `IServiceProvider`, calls `Handle(request, cancellationToken)`, returns its `Either`.
4. Variant selection through `IFeatureFlagProvider.GetVariationAsync<string>`; an unknown variant continues to `nextStep`.
5. Startup validation: every named type implements `IRequestHandler<TRequest,TResponse>` for its request and is registered; otherwise `ValidateOnStart` fails.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
- Issue #345, Phase 2; the redirection answer is Design Choice 4.
TASK:
- Implement FeatureVariantAttribute, the Fallback property, the redirector and the startup validation.
KEY RULES:
- The redirected handler runs instead of nextStep; behaviors after the gate are skipped and documented.
- A Left from the alternative handler is returned unchanged.
REFERENCE FILES:
- src/Encina/Abstractions/IRequestHandler.cs
- src/Encina.FeatureFlags/FeatureFlagPipelineBehavior.cs
```

</details>

---

### Phase 3: Tenant and Contextual Filters

> **Goal**: Per-tenant enablement and request-context predicates for the `Microsoft.FeatureManagement` provider.

<details>
<summary><strong>Tasks</strong></summary>

1. `Filters/TenantFilter.cs`: contextual filter over `FeatureFlagContext`; parameters from configuration (`Tenants`: list of allowed tenant ids); a missing tenant denies (fail closed).
2. `Filters/ContextualFilter.cs`: runs registered `IFeatureFlagPredicate` implementations by name over `FeatureFlagContext`.
3. `FeatureFlagsBuilder.AddFilter<T>()` registration; both filters are registered by `AddEncinaFeatureFlags`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
- Issue #345, Phase 3. The library's built-in filters are used as they are (Design Choice 2).
TASK:
- Implement TenantFilter, ContextualFilter and their registration.
KEY RULES:
- Missing tenant or context denies the feature.
- No tenant id or user id in logs or tags except the tenant id as a metric tag.
REFERENCE FILES:
- src/Encina.FeatureFlags/Model/FeatureFlagContext.cs
- src/Encina/Abstractions/IRequestContext.cs
```

</details>

---

### Phase 4: Options, Registration and `UseCustomProvider<T>()`

> **Goal**: Complete the registration surface.

<details>
<summary><strong>Tasks</strong></summary>

1. `FeatureFlagsBuilder.UseCustomProvider<T>()` (registers `T : IFeatureFlagProvider` with `TryAdd*`).
2. `FeatureFlagsOptions` additions: `DefaultBehavior` is not added; the disabled behavior is the attribute's (Design Choice 3).
3. DI test with `ValidateOnBuild` and `ValidateScopes`; the database store wins over the in-memory default in any registration order.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
- Issue #345, Phase 4.
TASK:
- Add UseCustomProvider<T>() and the registration tests.
KEY RULES:
- Registration completeness: AddEncinaFeatureFlags registers every option and dependency it resolves.
- Never override the application's own registration.
REFERENCE FILES:
- src/Encina.Compliance.Consent/ServiceCollectionExtensions.cs
- src/Encina.FeatureFlags/ServiceCollectionExtensions.cs
```

</details>

---

### Phase 5: Observability

> **Goal**: Evaluation metrics and logs for the gate and the redirection.

<details>
<summary><strong>Tasks</strong></summary>

1. `Diagnostics/FeatureFlagGateMetrics.cs`: counter `encina.featureflags.gate_evaluations` (tags `feature`, `outcome`: enabled, disabled, redirected, error) on the `Encina.FeatureFlags` meter from #604; span `encina.featureflags.gate`.
2. `[LoggerMessage]` EventIds 7310-7315 (gate disabled, redirected, variant selected, provider failed, redirect handler missing, evaluation skipped because no attribute).
3. No user id and no `EncinaError.Message` in logs or tags.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
- Issue #345, Phase 5. Range 7300-7319 is registered by #604; this plan uses 7310-7315.
TASK:
- Add the gate metrics, span and log messages.
KEY RULES:
- EventIds packed sequentially inside 7310-7315.
- Log error codes, never messages.
REFERENCE FILES:
- src/Encina.FeatureFlags/Diagnostics/FeatureFlagsDiagnostics.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

### Phase 6: Testing

> **Goal**: Every applicable test type.

<details>
<summary><strong>Tasks</strong></summary>

1. Unit: attribute metadata, AND/OR, redirection, filters; `FakeTimeProvider` for time-window rules.
2. Guard: every public method with parameters.
3. Contract: the behavior over every `IFeatureFlagProvider` implementation reachable without Docker.
4. Property: `TenantFilter` (never enables an unlisted tenant).
5. Integration: not required beyond #604's (local configuration needs no external service); justified in the pull request.
6. Benchmark: gate on the hot path (cached provider) with BenchmarkDotNet through `BenchmarkSwitcher`; load test justification `.md`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
- Issue #345, Phase 6; per-flag targets are in .github/coverage-manifest/Encina.FeatureFlags.json.
TASK:
- Write the tests listed in Phase 6 Tasks and reach every flag's target for the files this plan adds.
KEY RULES:
- Tests execute real behavior and filter code; shouldly via Encina.Testing.Shouldly.
- CRAP <= 10 on changed methods.
REFERENCE FILES:
- docs/en/guides/TESTING.md
- tests/Encina.UnitTests (Consent behavior tests)
```

</details>

---

### Phase 7: Documentation and the Family ADR

> **Goal**: Document the feature and record the decisions of the whole family.

<details>
<summary><strong>Tasks</strong></summary>

1. ADR for the feature-flag architecture (provider abstraction, relation to `Microsoft.FeatureManagement`, redirection, ownership of #300/#345/#451/#604); ADR number assigned when the ADR pull request opens.
2. Usage guide under `docs/features/` (attributes, filters, appsettings example, tenant targeting).
3. `changelog.d/345-feature-flags.added.md`, `docs/INVENTORY.md`, `ROADMAP.md` (#345 entry), package README.
4. `dotnet build --configuration Release` with 0 warnings; tests pass; per-flag coverage at target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT:
- Issue #345, Phase 7; documentation goes through docs-writer.
TASK:
- Write the ADR, the guide, the fragment and the INVENTORY/ROADMAP updates.
KEY RULES:
- Never edit CHANGELOG.md; coverage figures only through covref markers.
- State the provider coverage honestly: SaaS providers are #2694, outside 1.0.
REFERENCE FILES:
- docs/architecture/adr/index.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | What it asks of this feature |
|--------|-----------|------------------------------|
| Maintainer comment 2026-03-18 on #345 | `Microsoft.FeatureManagement` as foundation; SHA256 bucketing; `TimeProvider`; cached metadata | Design Choices 1 and 2 |
| #361 carry-over comment (2026-10-06) | Fallback handlers and per-tenant filters | Design Choices 2 and 4 |
| AGENTS.md section 3 | ROP; fail closed; provider coherence | Design Choices 3 and 7 |
| ADR-018 | 12 cross-cutting functions | Matrix below |
| Martin Fowler, Feature Toggles | Release, experiment, ops and permission toggles | Scenarios the attributes serve |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `IPipelineBehavior<,>` | `src/Encina/Abstractions/IPipelineBehavior.cs` | Behavior contract (`Handle(request, context, nextStep, ct)`) |
| `PipelineStage`, `PipelineStageAttribute` | `src/Encina/Pipeline/Ordering/` | Stage declaration |
| `IRequestContext` (`TenantId`, `UserId`), `IRequestContextAccessor` | `src/Encina/Abstractions/` | Filter context |
| Attribute-cache behavior pattern | `src/Encina.Compliance.Consent/ConsentRequiredPipelineBehavior.cs:57` | Metadata cache |
| `IFeatureFlagProvider`, rule evaluator, cache | `Encina.FeatureFlags` (#604) | Evaluation |
| `FeatureFlagContext.From`, `[FeatureFlag]`, behavior | `Encina.FeatureFlags` (#300) | Extended here |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.FeatureFlags` | 7300-7319 (registered by #604) | #604 7300-7305, #300 7306-7309, **#345 7310-7315**, #451 7316-7319 |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Attributes and behavior changes (Phases 1-2) | ~10 | |
| Filters and registration (Phases 3-4) | ~9 | |
| Observability (Phase 5) | ~3 | |
| Tests (Phase 6) | ~16 | Unit, guard, contract, property, benchmark |
| Documentation (Phase 7) | ~6 | ADR, guide, README, fragment, INVENTORY, ROADMAP |
| **Total** | **~44** | Seven pull requests |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #345: the umbrella of Encina feature flags (multi-flag, variants, redirection, filters).

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library with Railway Oriented Programming; pre-1.0, no compatibility code.
- #604 (package and provider contract) and #300 (single-flag gate) are merged before this work.
- Provider category: none for this plan.

IMPLEMENTATION OVERVIEW:
Phase 1: [FeatureFlags] multi-flag. Phase 2: fallback and variant redirection. Phase 3: TenantFilter, ContextualFilter.
Phase 4: UseCustomProvider and registration tests. Phase 5: metrics and EventIds 7310-7315.
Phase 6: tests. Phase 7: ADR, guide, fragment.

KEY PATTERNS:
- Attributes on the request type, metadata cached per type.
- Disabled feature returns a Left (encina.featureflags.disabled).
- Microsoft.FeatureManagement built-in filters are used as they are.
- Fail closed on missing tenant or context.

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
| 1 | Caching | ✅ Phase 4 | Uses the cache decorator of #604; enabled through `FeatureFlagsOptions.CacheEnabled` |
| 2 | OpenTelemetry | ✅ Phase 5 | Gate counter and span on the family meter |
| 3 | Structured Logging | ✅ Phase 5 | EventIds 7310-7315 |
| 4 | Health Checks | ⏭️ Deferred | Provider health check: issue file `feature-flag-provider-health-checks` in `artifacts/issues/` (same item as #604) |
| 5 | Validation | ✅ Phases 2, 4 | Startup validation of fallback and variant handler types; options validated on start |
| 6 | Resilience | ✅ Phase 1 | `DefaultWhenUnavailable` decides what the gate does when the provider returns a `Left`; provider-side Polly is in #604 |
| 7 | Distributed Locks | ❌ N/A | Stateless read |
| 8 | Transactions | ❌ N/A | The gate runs before the transaction stage; no mutation |
| 9 | Idempotency | ❌ N/A | Evaluation is deterministic |
| 10 | Multi-Tenancy | ✅ Phase 3 | `TenantFilter`; tenant in `FeatureFlagContext` and as a metric tag |
| 11 | Module Isolation | ⏭️ Deferred | Module-scoped flags: issue file `feature-flag-module-scope` in `artifacts/issues/` |
| 12 | Audit Trail | ⏭️ Deferred | Flag change and evaluation audit: issue file `feature-flag-change-audit` in `artifacts/issues/` |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| Package and `IFeatureFlagProvider` | #604 Phase 1 | Open |
| Single-flag gate and library provider | #300 | Open |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| OpenFeature and ConfigCat providers | #451 | Build on the same contract |
| SaaS providers | #2694 | Outside 1.0 (SPEC-000 section 6) |
| Multi-tenancy support | #346 | Tenant context source |

---

## Next Steps

1. Review this plan; the Dilemmas (4 and 5) are asked when work on the issue starts.
2. Post the plan link on #345 and cross-reference #300, #451 and #604.
3. Start after #604 Phase 1 and #300 are merged.
4. Each phase is one pull request of at most 15 files and 1500 lines; the last carries `Fixes #345`.
