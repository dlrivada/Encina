# Implementation Plan: Feature Flag Pipeline Integration — the Single-Flag Gate on `Microsoft.FeatureManagement`

> **Issue**: [#300](https://github.com/dlrivada/Encina/issues/300)
> **Type**: Feature
> **Complexity**: Low (5 phases, no database provider)
> **Estimated Scope**: ~600 lines of production code + ~1,000 lines of tests, in about 5 pull requests

---

## Summary

Enable or disable a request by feature flag through a pipeline behavior, using `Microsoft.FeatureManagement` for configuration-driven flags, percentage rollouts and targeting. Nothing of it exists in `src/` today: no feature-flag attribute or behavior, and `Microsoft.FeatureManagement` is not in `Directory.Packages.props`.

This plan is the **first user-visible slice** of the feature-flag family. Its neighbours: #604 owns the package and the `IFeatureFlagProvider` contract (this plan starts after #604 Phase 1), #345 is the umbrella (multi-flag, variants, redirection, filters, the family ADR), #451 adds OpenFeature and ConfigCat. SaaS providers are #2694 (outside 1.0).

| Piece | Owner |
|---|---|
| Package, contract, errors, EventId ranges | #604 Phase 1 |
| **`[FeatureFlag(name)]`, `FeatureFlagMetadata<TRequest>`, `FeatureFlagPipelineBehavior<,>`, `FeatureFlagContext.From(IRequestContext)`, `MicrosoftFeatureManagementProvider`, gate metrics (EventIds 7306-7309)** | **#300 (this plan)** |
| `[FeatureFlags]`, `[FeatureVariant]`, `Fallback`, tenant and contextual filters, family ADR | #345 |

**Affected packages**: `Encina.FeatureFlags` (created by #604). **Provider category**: none.

---

## Design Choices

<details>
<summary><strong>1. Relation to #345, #451 and #604 — behavior over a provider abstraction, library as one provider</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The behavior depends on `IFeatureFlagProvider` (#604); `Microsoft.FeatureManagement` is the first provider (`MicrosoftFeatureManagementProvider`)** | Provider-agnostic: the same gate works with the database, Redis, Unleash and OpenFeature providers | The library's filters are reached through an adapter; targeting context must be mapped |
| **B) The behavior depends on `IFeatureManager` directly; Encina adds only the gate** | Least code; no mapping | Couples the gate to the library's filter model; #604, #451 and #345 providers could not back it |

### Chosen Option: **A — Gate over `IFeatureFlagProvider` (recommended, pending the maintainer)**

### Rationale

- A wins when several backends must be interchangeable, which is the stated purpose of #604 and #451. B wins if the library is accepted as the single engine and the other provider issues are dropped.
- Under B, Phase 2 of this plan disappears and the behavior takes `IFeatureManager`; Phases 1, 3, 4 and 5 stay.
- The maintainer's foundation decision (comment of 2026-03-18 on #345) is respected in both options: the library stays the engine of the default provider.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. What <code>Fallback</code> does</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A disabled feature may redirect to an alternative handler named in the attribute (`Fallback = typeof(...)`), implemented by #345** | Old and new handlers coexist during a rollout; the caller sees a result | The redirected handler skips the behaviors after the gate |
| **B) No fallback: the `Left` goes to the caller** | Simple; nothing skipped | Callers degrade themselves; the fallback handlers carried from #361 are dropped |

### Chosen Option: **A — Alternative handler (recommended, pending the maintainer)**

### Rationale

- This is the same decision as #345 Design Choice 4; the answer is given once and applies to both plans.
- This plan ships only the `Left` outcome and leaves `FeatureFlagAttribute` open for the `Fallback` property that #345 adds. The enum `FeatureGateBehavior` of the issue sketch is dropped: with a `Fallback` type, the outcome is implied (none: `Left`; named: redirect).

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Placement of the <code>Microsoft.FeatureManagement</code> provider</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Inside `Encina.FeatureFlags` (the package references `Microsoft.FeatureManagement`)** | The zero-infrastructure default works after one `AddEncinaFeatureFlags`; matches "foundation" | Applications using only the database provider also reference the library |
| **B) Its own package `Encina.FeatureFlags.FeatureManagement`** | Core stays free of the library | One more package; the default needs two registrations |

### Chosen Option: **A — In the core package (recommended, pending the maintainer)**

### Rationale

- A wins when most users start from `appsettings` flags; B wins when many users bring only a database or SaaS provider and the dependency weight matters.
- The library depends on `Microsoft.Extensions.*` packages Encina already references, so the weight of A is small.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Attribute name and the disabled outcome</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `[FeatureFlag(name)]` on the request type; disabled returns `Left` `encina.featureflags.disabled`; no `ReturnDefault`, no `ThrowException`** | One attribute name for the family; Railway Oriented Programming | Renames the issue's `[FeatureGate]` |
| **B) Keep `[FeatureGate]` and `FeatureGateBehavior` (ReturnError, ReturnDefault, Throw)** | Matches the original text | Two attribute names for one concept (#345 uses `[FeatureFlag]`); a default value reports success for work not done; exceptions for business logic |

### Chosen Option: **A — `[FeatureFlag]` with a typed Left**

### Rationale

- The maintainer confirmed `[FeatureFlag]` and the pipeline behavior as the design (comment of 2026-03-18 on #345).
- The issue already dropped `ReturnDefault` and `ThrowException`.

**Class:** Settled
**Settled by:** Maintainer comment of 2026-03-18 on #345; AGENTS.md section 3 (Railway Oriented Programming; errors never swallowed); ADR-001

</details>

<details>
<summary><strong>5. Contextual evaluation — no <code>IFeatureContextProvider</code> seam</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `FeatureFlagContext.From(IRequestContext)`: user from the identity, tenant from `TenantId`, correlation id as an attribute; mapped to the library's targeting context inside the provider** | `Handle` already receives the context; no ambient accessor, no extra interface | Not extensible by an application with a different user source |
| **B) Keep `IFeatureContextProvider` and `RequestContextFeatureProvider`** | Custom context sources | An interface for a case no one has; `IRequestContext.Identity` already is the extension point |

### Chosen Option: **A — Build the context from `IRequestContext`**

### Rationale

- `IPipelineBehavior<,>.Handle(request, context, nextStep, ct)` receives `IRequestContext` (`src/Encina/Abstractions/IPipelineBehavior.cs`); `UserId` is the identity's projection and `TenantId` is a property (`src/Encina/Abstractions/IRequestContext.cs`).
- The issue itself says the interface is kept only if the context alone is not enough; it is.

**Class:** Settled
**Settled by:** `src/Encina/Abstractions/IPipelineBehavior.cs` and `IRequestContext.cs` (the context is a `Handle` parameter); issue #300 body ("Contextual evaluation")

</details>

<details>
<summary><strong>6. Caching, position in the pipeline and library API use</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No separate `CacheEvaluations` option: the cache decorator of #604 is the only cache; `[PipelineStage(Authorization, Order = 100)]`; contextual calls use the library's overloads taking a context** | One cache for all providers; rejection before work; no custom hashing | The cache design (#604 Design Choice 6) is open |
| **B) A cache inside the gate (the issue's `CacheEvaluations`, `CacheDuration`)** | Works without #604 | A second cache with different keys and expiry; tenants could share an entry |

### Chosen Option: **A — One cache (#604), Authorization stage, library context overloads**

### Rationale

- Issue #604 lists "Caching layer with change notification" as its own acceptance criterion.
- Stage: the same reasoning and citation as #345 Design Choice 6 (`PipelineStage.cs`: identity first, work later).
- At the start of Phase 2, confirm against the pinned library version that `IFeatureManager.IsEnabledAsync` has the overload taking an application context (targeting needs it); the provider adapter isolates any difference.

**Class:** Trivial
**Settled by:** Issue #604 body (acceptance criterion: caching layer); `src/Encina/Pipeline/Ordering/PipelineStage.cs` (stage documentation)

</details>

---

## Implementation Phases

### Phase 1: Attribute, Metadata Cache and Context Mapping

> **Goal**: The declarative part and the context, without any library dependency.

<details>
<summary><strong>Tasks</strong></summary>

1. `Attributes/FeatureFlagAttribute.cs` (`[AttributeUsage(AttributeTargets.Class)]`, `string Name`).
2. `Gate/FeatureFlagMetadata.cs`: `static class FeatureFlagMetadata<TRequest>` with `Attribute` read once (static field).
3. `Model/FeatureFlagContextExtensions.cs`: `FeatureFlagContext.From(IRequestContext)` (user, tenant, `correlation` attribute).
4. `PublicAPI.Unshipped.txt`, manifest entries; unit and guard tests.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
- Issue #300, Phase 1. #604 Phase 1 (package and contract) is merged.
TASK:
- Add FeatureFlagAttribute, FeatureFlagMetadata<TRequest> and FeatureFlagContext.From(IRequestContext).
KEY RULES:
- Metadata is read once per request type (static field), never per request.
- No user id or message text in logs; XML docs on every public member.
REFERENCE FILES:
- src/Encina.Compliance.Consent/ConsentRequiredPipelineBehavior.cs
- src/Encina/Abstractions/IRequestContext.cs
```

</details>

---

### Phase 2: `Microsoft.FeatureManagement` Provider

> **Goal**: The default provider reading flags from configuration (Design Choices 1 and 3).

<details>
<summary><strong>Tasks</strong></summary>

1. `Directory.Packages.props`: pin `Microsoft.FeatureManagement` (latest stable at implementation time, one source); reference it from `Encina.FeatureFlags.csproj`.
2. `Providers/MicrosoftFeatureManagementProvider.cs` implementing `IFeatureFlagProvider` over `IFeatureManager`: `IsEnabledAsync` maps `FeatureFlagContext` to the library's targeting context; a library exception becomes `Left` `provider_failed` (the exception type, not its message, is recorded).
3. `ServiceCollectionExtensions.AddEncinaFeatureFlags(...)`: registers `AddFeatureManagement()` against the `FeatureManagement` configuration section and the provider with `TryAdd*`.
4. Tests with the real manager and in-memory configuration (no mock of the library).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
- Issue #300, Phase 2. Skip this phase if the maintainer chooses Design Choice 1 option B (the behavior then takes IFeatureManager).
TASK:
- Add the package reference, MicrosoftFeatureManagementProvider and the registration.
KEY RULES:
- Either results; library exceptions become provider_failed with the exception type only.
- Tests use the real IFeatureManager over in-memory configuration.
- Package version only in Directory.Packages.props.
REFERENCE FILES:
- src/Encina.FeatureFlags/Abstractions/IFeatureFlagProvider.cs
- Directory.Packages.props
```

</details>

---

### Phase 3: Pipeline Behavior, Stage and Gate Observability

> **Goal**: `FeatureFlagPipelineBehavior<,>` and its diagnostics.

<details>
<summary><strong>Tasks</strong></summary>

1. `Gate/FeatureFlagPipelineBehavior.cs`: `[PipelineStage(PipelineStage.Authorization, Order = 100)]`; reads `FeatureFlagMetadata<TRequest>`; no attribute: `nextStep()`; attribute: `IFeatureFlagProvider.IsEnabledAsync(name, FeatureFlagContext.From(context), ct)`; enabled: `nextStep()`; disabled: `Left` `encina.featureflags.disabled` with the flag name in `details`; provider `Left`: handled by `FeatureFlagsOptions.DefaultWhenUnavailable` (#604 Design Choice 3), otherwise returned.
2. Registration: `TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(FeatureFlagPipelineBehavior<,>)))`.
3. `Diagnostics/FeatureFlagGateMetrics.cs`: counter `encina.featureflags.gate_evaluations` (tags `feature`, `outcome`), span `encina.featureflags.gate`; `[LoggerMessage]` EventIds 7306-7309 (gate disabled, provider failed, evaluation skipped, default applied).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
- Issue #300, Phase 3. The EventId range 7300-7319 is registered by #604; this plan uses 7306-7309.
TASK:
- Implement the behavior, its registration and the gate diagnostics.
KEY RULES:
- Behavior is a pipeline behavior with the real signature Handle(request, context, nextStep, cancellationToken).
- A Left is never turned into success unless DefaultWhenUnavailable says so, and that case is logged by code.
- No EncinaError.Message in logs or tags.
REFERENCE FILES:
- src/Encina/Abstractions/IPipelineBehavior.cs
- src/Encina.Compliance.Consent/ServiceCollectionExtensions.cs
```

</details>

---

### Phase 4: Testing

> **Goal**: Every applicable test type for the gate.

<details>
<summary><strong>Tasks</strong></summary>

1. Unit: enabled, disabled, no attribute, provider failure with and without a default, tenant targeting from the request context.
2. Guard: every public method with parameters.
3. Contract: the behavior's contract over `InMemory`-style provider and the library provider.
4. Integration: not required (configuration-backed library needs no service); noted in the pull request. Property and load: justified (binary decision, stateless).
5. Benchmark: gate overhead per request, through `BenchmarkSwitcher`, cached and uncached.
6. DI test: provider built with `ValidateOnBuild` and `ValidateScopes`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
- Issue #300, Phase 4. Per-flag targets are in .github/coverage-manifest/Encina.FeatureFlags.json.
TASK:
- Write the tests listed in Phase 4 Tasks.
KEY RULES:
- Tests execute real package code and the real library manager.
- Assertions through Encina.Testing.Shouldly; CRAP <= 10 on changed methods.
REFERENCE FILES:
- docs/en/guides/TESTING.md
- tests/Encina.UnitTests (Consent behavior tests)
```

</details>

---

### Phase 5: Documentation and Finalization

> **Goal**: Document the gate.

<details>
<summary><strong>Tasks</strong></summary>

1. README of `Encina.FeatureFlags`: the gate, an `appsettings` example, tenant targeting.
2. `changelog.d/300-feature-flag-gate.added.md`; `docs/INVENTORY.md`.
3. The ADR for D1 and D2 of the issue is written once by #345 (ADR number assigned when the ADR pull request opens).
4. `dotnet build --configuration Release` with 0 warnings; tests pass; per-flag coverage at target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
- Issue #300, Phase 5; documentation goes through docs-writer.
TASK:
- Write the README section, the fragment and the INVENTORY update.
KEY RULES:
- Never edit CHANGELOG.md; coverage figures only through covref markers.
REFERENCE FILES:
- changelog.d/README.md
- .claude/skills/encina-docs/SKILL.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | What it asks of this feature |
|--------|-----------|------------------------------|
| Maintainer comment 2026-03-18 on #345 | Library as foundation; `[FeatureFlag]`; cached metadata | Design Choices 4 and 6 |
| AGENTS.md section 3 | ROP; time from `TimeProvider`; fail closed | Typed `Left`, no default value |
| Microsoft.FeatureManagement documentation | `IFeatureManager`, targeting context, configuration section | Provider adapter |
| ADR-018 | 12 cross-cutting functions | Matrix below |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `IPipelineBehavior<,>`, `RequestHandlerCallback<TResponse>` | `src/Encina/Abstractions/IPipelineBehavior.cs`, `RequestHandlerDelegate.cs:9` | Behavior contract |
| `PipelineStage`, `PipelineStageAttribute` | `src/Encina/Pipeline/Ordering/` | Stage declaration |
| `IRequestContext` | `src/Encina/Abstractions/IRequestContext.cs` | Context mapping |
| `EncinaErrors.Create` | `src/Encina/Results/EncinaErrors.cs:25` | Error factory |
| Attribute cache pattern, `TryAddEnumerable` registration | `src/Encina.Compliance.Consent/ConsentRequiredPipelineBehavior.cs:57`, `ServiceCollectionExtensions.cs:98` | Same shape |
| `IFeatureFlagProvider`, cache decorator | `Encina.FeatureFlags` (#604) | Evaluation and caching |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.FeatureFlags` | 7300-7319 (registered by #604) | #604 7300-7305, **#300 7306-7309**, #345 7310-7315, #451 7316-7319 |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production (Phases 1-3) | ~12 | Attribute, metadata, context, provider, behavior, diagnostics, registration |
| Tests (Phase 4) | ~12 | Unit, guard, contract, benchmark, DI |
| Documentation (Phase 5) | ~3 | README, fragment, INVENTORY |
| **Total** | **~27** | Five pull requests |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #300: the single-flag feature gate on Microsoft.FeatureManagement.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library with Railway Oriented Programming; pre-1.0, no compatibility code.
- #604 Phase 1 (package, IFeatureFlagProvider, errors, EventId ranges) is merged before this work.
- Provider category: none.

IMPLEMENTATION OVERVIEW:
Phase 1: [FeatureFlag], FeatureFlagMetadata<TRequest>, FeatureFlagContext.From.
Phase 2: MicrosoftFeatureManagementProvider and registration.
Phase 3: FeatureFlagPipelineBehavior, stage, gate metrics, EventIds 7306-7309.
Phase 4: tests. Phase 5: documentation.

KEY PATTERNS:
- Attribute on the request type, metadata cached per type.
- Disabled is a Left (encina.featureflags.disabled); never a default value or an exception.
- Context from IRequestContext; one cache (the decorator of #604).
- Stage Authorization, order 100.

REFERENCE FILES:
- docs/plans/feature-flag-gate-implementation-plan-300.md (this plan)
- src/Encina.Compliance.Consent/ConsentRequiredPipelineBehavior.cs
- src/Encina/Abstractions/IPipelineBehavior.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Phase 3 | Uses the cache decorator of #604 (no second cache, Design Choice 6) |
| 2 | OpenTelemetry | ✅ Phase 3 | Counter `encina.featureflags.gate_evaluations` and span `encina.featureflags.gate` |
| 3 | Structured Logging | ✅ Phase 3 | EventIds 7306-7309 |
| 4 | Health Checks | ⏭️ Deferred | In-process for the library provider; remote providers: issue file `feature-flag-provider-health-checks` in `artifacts/issues/` |
| 5 | Validation | ❌ N/A | The only input is the attribute's flag name, validated at metadata creation |
| 6 | Resilience | ❌ N/A | In-process library; remote-provider resilience is #604, #451 and #2694 |
| 7 | Distributed Locks | ❌ N/A | Stateless evaluation |
| 8 | Transactions | ❌ N/A | Read-only; the gate runs before the transaction stage |
| 9 | Idempotency | ❌ N/A | No side effects |
| 10 | Multi-Tenancy | ✅ Phase 1 | Tenant from `IRequestContext.TenantId` in `FeatureFlagContext`; tenant as a metric tag |
| 11 | Module Isolation | ⏭️ Deferred | Module-scoped flags: issue file `feature-flag-module-scope` in `artifacts/issues/` |
| 12 | Audit Trail | ⏭️ Deferred | Flag change audit belongs to the provider issues: issue file `feature-flag-change-audit` in `artifacts/issues/`; gate decisions are metered and logged here |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| `Encina.FeatureFlags` package and contract | #604 Phase 1, open | Must merge first |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Umbrella: multi-flag, variants, redirection, family ADR | #345 | Starts after this plan |
| OpenFeature and ConfigCat | #451 | Same contract |
| SaaS providers | #2694 | Outside 1.0 |

---

## Next Steps

1. Review this plan; the Dilemmas (1, 2 and 3) are asked when work on the issue starts.
2. Post the plan link on #300.
3. Start after #604 Phase 1 merges.
4. Each phase is one pull request of at most 15 files and 1500 lines; the last carries `Fixes #300`.
