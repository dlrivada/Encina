# Implementation Plan: Feature Flag Pipeline Integration — the Single-Flag Gate on `Microsoft.FeatureManagement`

> **Issue**: [#300](https://github.com/dlrivada/Encina/issues/300)
> **Type**: Feature
> **Complexity**: Low (4 phases, no database provider)
> **Estimated Scope**: about 2,250 changed lines in 4 pull requests, tests, manifests and PublicAPI files included (table "Pull request split" under Research; each pull request is at most 15 files and 1,500 lines)

---

## Summary

Enable or disable a request by feature flag through a pipeline behavior, using `Microsoft.FeatureManagement` for configuration-driven flags, percentage rollouts and targeting. Nothing of it exists in `src/` today: no feature-flag attribute or behavior, and `Microsoft.FeatureManagement` is not in `Directory.Packages.props`.

This plan is the **first user-visible slice** of the feature-flag family. Its neighbours: #604 owns the package, the `IFeatureFlagProvider` contract, the options and the core EventId range (this plan starts after #604 Phases 1-3), #345 is the umbrella (multi-flag, variants, redirection, filters, the family ADR), #451 adds OpenFeature and ConfigCat. SaaS providers are #2694 (outside 1.0).

| Piece | Owner |
|---|---|
| Package, contract, errors (each with an `ErrorCategory`, ADR 062), options, registration skeleton, core EventId range 7300-7319 | #604 Phases 1-3 |
| **`[FeatureFlag(name)]`, `FeatureFlagMetadata<TRequest>`, `FeatureFlagPipelineBehavior<,>`, `FeatureFlagContext.From(IRequestContext)`, `MicrosoftFeatureManagementProvider`, gate metrics and logs (EventIds 7306-7309)** | **#300 (this plan)** |
| `[FeatureFlags]`, `[FeatureVariant]`, `Fallback`, tenant and contextual filters, family ADR | #345 |

**Acceptance criterion not delivered here**: the issue lists "Fallback implemented". This plan ships only the `Left` outcome; `Fallback` is delivered by #345 Phase 2 when its Design Choice 4 is decided for option A (Design Choice 2 below).

**Error contract**: ADR 062 (error category contract, merged in PR #3599) is on `main`. This plan adds no error code of its own: the gate uses `encina.featureflags.disabled` (category `FailedPrecondition`) and `provider_failed` (category `UpstreamFailure`) from the `FeatureFlagErrors` of #604, which are created with the `EncinaErrors.Create(code, category, message, ...)` overload (`src/Encina/Results/EncinaErrors.cs`). The original sketch of the issue used `encina.feature_disabled`; the rename is part of Design Choice 4.

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
- Under B, Phase 2 of this plan disappears and the behavior takes `IFeatureManager`; Phases 1, 3 and 4 stay.
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
| **A) `[FeatureFlag(name)]` on the request type; disabled returns `Left` `encina.featureflags.disabled` (renamed from the sketch's `encina.feature_disabled`, so the code follows the family prefix); no `ReturnDefault`, no `ThrowException`** | One attribute name and one code prefix for the family; Railway Oriented Programming | Renames the issue's `[FeatureGate]` and its error code |
| **B) Keep `[FeatureGate]` and `FeatureGateBehavior` (ReturnError, ReturnDefault, Throw)** | Matches the original text | Two attribute names for one concept (#345 uses `[FeatureFlag]`); a default value reports success for work not done; exceptions for business logic |

### Chosen Option: **A — `[FeatureFlag]` with a typed Left**

### Rationale

- The maintainer confirmed `[FeatureFlag]` and the pipeline behavior as the design (comment of 2026-03-18 on #345).
- The issue already dropped `ReturnDefault` and `ThrowException`; the error carries the flag name in `details` only (#345 Design Choice 3).

**Class:** Settled
**Settled by:** Maintainer comment of 2026-03-18 on #345; AGENTS.md section 3 (Railway Oriented Programming; errors never swallowed); ADR-001

</details>

<details>
<summary><strong>5. Contextual evaluation — no <code>IFeatureContextProvider</code> seam</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `FeatureFlagContext.From(IRequestContext)`: user from the identity, tenant from `TenantId`, correlation id as an attribute; mapped to the library's targeting context inside the provider** | `Handle` already receives the context; no ambient accessor, no extra interface; wins when the identity is the only user source | Not extensible by an application with a different user source |
| **B) Keep `IFeatureContextProvider` and `RequestContextFeatureProvider`** | Custom context sources; wins when the user or tenant comes from somewhere other than `IRequestContext` | An extra public interface; `IRequestContext.Identity` is already an extension point |

### Chosen Option: **A — Build the context from `IRequestContext` (recommended, pending the maintainer)**

### Rationale

- `IPipelineBehavior<,>.Handle(request, context, nextStep, ct)` receives `IRequestContext` (`src/Encina/Abstractions/IPipelineBehavior.cs`); `UserId` is the identity's projection and `TenantId` is a property (`src/Encina/Abstractions/IRequestContext.cs`).
- The issue itself says the interface is kept only if the context alone is not enough; the plan's reading is that it is enough, which the maintainer may overrule.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Caching — one cache or a cache inside the gate</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No separate `CacheEvaluations` option: the cache decorator of #604 is the only cache** | One cache for all providers; no custom hashing; tenants never share an entry | The cache design (#604 Design Choice 6) is open |
| **B) A cache inside the gate (the issue's `CacheEvaluations`, `CacheDuration`)** | Works without the #604 decorator | A second cache with different keys and expiry; a tenant or user could share an entry if the key is wrong |

### Chosen Option: **A — One cache (#604) (recommended, pending the maintainer)**

### Rationale

- A wins when one invalidation and one key design is preferred; B wins when the gate must be usable before the #604 cache exists. Issue #604 lists "Caching layer with change notification" as its own acceptance criterion.
- Caching stays opt-in (`FeatureFlagsOptions.CacheEnabled`, default `false`, #604 Design Choice 13); the gate does not enable it.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Position in the pipeline</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `[PipelineStage(PipelineStage.Authorization, Order = 100)]`: after identity and after the authorization steps that keep the default order 0, before compliance, validation and the transaction** | A disabled feature is rejected before any work; targeting has the identity; an unauthorized caller never learns a flag state | The stage is documented for authorization, a feature gate is a neighbour of it |
| **B) A new `PipelineStage` member** | Exact name | Extends a public contract for one consumer |
| **C) Default `Application` stage** | Nothing to declare | Runs inside the transaction and idempotency steps |

### Chosen Option: **A — Authorization stage, order 100 (recommended, pending the maintainer)**

### Rationale

- The same decision, reasoning and citation as #345 Design Choice 6 (`src/Encina/Pipeline/Ordering/PipelineStage.cs`: lower is further out; `Authorization = 500`, `Application = 1500`). Because the stage vocabulary is not yet applied by the pipeline builders (phase note of #2184 in that file), until it is the position follows registration order; Phase 3 states it in the XML docs.

**Class:** Dilemma

</details>

<details>
<summary><strong>8. Library API for contextual calls</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Contextual calls use the library's overloads that take a context (targeting context)** | The library's targeting does the bucketing | The overload must exist in the pinned version |
| **B) Evaluate targeting in Encina** | Independent of the library | Duplicates the library, against the maintainer's decision of 2026-03-18 |

### Chosen Option: **A — The library's context overloads**

### Rationale

- At the start of Phase 2, confirm against the pinned library version that `IFeatureManager.IsEnabledAsync` has the overload taking an application context (targeting needs it); the provider adapter isolates any difference.

**Class:** Trivial
**Settled by:** Maintainer comment of 2026-03-18 on #345 (library as engine); `Microsoft.FeatureManagement` documentation (targeting with a supplied context)

</details>

<details>
<summary><strong>9. Identifiers in telemetry</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The gate counter and span carry the flag name and the outcome only; no tenant id, no user id** | Bounded metric cardinality; no tenant identifier in telemetry | An operator cannot filter gate outcomes by tenant in metrics |
| **B) The tenant id as a span attribute only** | Trace search by tenant | A tenant identifier in telemetry storage |
| **C) The tenant id as a metric tag** | Per-tenant dashboards | Unbounded cardinality in a multi-tenant system |

### Chosen Option: **A — No tenant or user id in tags (recommended, pending the maintainer)**

### Rationale

- The same decision as #345 Design Choice 11; the answer is given once. A wins when telemetry must stay bounded; B when per-tenant diagnosis matters.
- Logs and tags never carry `EncinaError.Message` or personal data (AGENTS.md section 3).

**Class:** Dilemma

</details>

<details>
<summary><strong>10. Behavior registration and cost for requests without the attribute</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Register `FeatureFlagPipelineBehavior<,>` open-generic with `TryAddEnumerable`, as `ConsentRequiredPipelineBehavior<,>` is; the metadata is a static field per request type, so a request without the attribute costs one static read and `nextStep()`** | The established convention; no scan | The behavior is in the chain of every request |
| **B) Register only for request types that carry the attribute** | No cost for other requests | An assembly scan at registration; closed generics per request |

### Chosen Option: **A — Open-generic registration**

### Rationale

- `src/Encina.Compliance.Consent/ServiceCollectionExtensions.cs:98` registers its behavior this way.

**Class:** Trivial
**Settled by:** `src/Encina.Compliance.Consent/ServiceCollectionExtensions.cs:98` and `ConsentRequiredPipelineBehavior.cs:57` (convention)

</details>

<details>
<summary><strong>11. What <code>FeatureFlagContext.From</code> carries</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) User id, tenant id and a `correlation` attribute are passed to the provider; none is logged or tagged** | Targeting by user and tenant works, as the issue asks | The provider (and a remote one such as Unleash) receives the user id |
| **B) No user id** | Less personal data leaves the process | Percentage and user targeting cannot work |

### Chosen Option: **A — User, tenant and correlation id to the provider**

### Rationale

- The issue's "Contextual evaluation" criterion is targeting by user and tenant; B cannot meet it. Remote providers receive only what the application enabled them for.

**Class:** Settled
**Settled by:** Issue #300 body ("Contextual evaluation" acceptance criterion); AGENTS.md section 3 (no personal data in logs or tags)

</details>

---

## Implementation Phases

Each phase is one pull request: it carries its own unit, guard and (where it applies) contract and benchmark tests, its coverage-manifest entries (`.github/coverage-manifest/Encina.FeatureFlags.json`), its `PublicAPI.Unshipped.txt` lines and the diagnostics of the code it adds. Every new file under `src/` gets per-flag targets in the manifest, demanding and realistic, each with a one-sentence justification; a target of 0 needs a reason (for example the property flag of a binary, stateless decision). The table "Pull request split" under Research lists the files and lines of each phase.

### Phase 1: Attribute, Metadata Cache and Context Mapping

> **Goal**: The declarative part and the context, without any library dependency.

<details>
<summary><strong>Tasks</strong></summary>

1. `Attributes/FeatureFlagAttribute.cs` (`[AttributeUsage(AttributeTargets.Class)]`, `string Name`; the constructor rejects a null, empty or whitespace name with `ArgumentException`, a guard on code; the format rule of the name is checked by the providers and returns `invalid_flag_name`).
2. `Gate/FeatureFlagMetadata.cs`: `static class FeatureFlagMetadata<TRequest>` with `Attribute` read once (static field).
3. `Model/FeatureFlagContextExtensions.cs`: `FeatureFlagContext.From(IRequestContext)` (user, tenant, `correlation` attribute; Design Choice 11).
4. Tests: `FeatureFlagAttributeTests`, `FeatureFlagMetadataTests`, `FeatureFlagContextExtensionsTests`, guard tests; manifest entries and `PublicAPI.Unshipped.txt`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
- Issue #300, Phase 1. #604 Phases 1-3 (package, contract, options, EventId range) are merged.
TASK:
- Add FeatureFlagAttribute, FeatureFlagMetadata<TRequest> and FeatureFlagContext.From(IRequestContext) with tests, manifest entries and PublicAPI lines.
KEY RULES:
- Metadata is read once per request type (static field), never per request.
- No user id or message text in logs; XML docs on every public member.
- At most 15 files and 1500 lines.
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
2. `Providers/MicrosoftFeatureManagementProvider.cs` implementing `IFeatureFlagProvider` over `IFeatureManager`: `IsEnabledAsync` maps `FeatureFlagContext` to the library's targeting context (Design Choice 8); a library exception becomes `Left` `provider_failed` created with the category overload and the exception attached (the gate logs the exception through `ForLogging()`: type and stack trace, never the message).
3. `ServiceCollectionExtensions.AddEncinaFeatureFlags(...)` (the method of #604 Phase 2) registers `AddFeatureManagement()` against the `FeatureManagement` configuration section and the provider with `TryAdd*`; a provider registered by the application or by another package is never overridden.
4. Tests with the real manager and in-memory configuration (no mock of the library): `MicrosoftFeatureManagementProviderTests`, a registration test (`ValidateOnBuild`, `ValidateScopes`), guard tests, the provider contract suite of #604 run against this provider; manifest entries and `PublicAPI.Unshipped.txt`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
- Issue #300, Phase 2. Skip this phase if the maintainer chooses Design Choice 1 option B (the behavior then takes IFeatureManager).
TASK:
- Add the package reference, MicrosoftFeatureManagementProvider, the registration and their tests.
KEY RULES:
- Either results; library exceptions become provider_failed (category UpstreamFailure) with the exception attached.
- Tests use the real IFeatureManager over in-memory configuration.
- Package version only in Directory.Packages.props.
- At most 15 files and 1500 lines.
REFERENCE FILES:
- src/Encina.FeatureFlags/Abstractions/IFeatureFlagProvider.cs
- src/Encina.FeatureFlags/ServiceCollectionExtensions.cs
- Directory.Packages.props
```

</details>

---

### Phase 3: Pipeline Behavior, Stage and Gate Observability

> **Goal**: `FeatureFlagPipelineBehavior<,>` and its diagnostics, created together so that nothing refers to a later pull request.

<details>
<summary><strong>Tasks</strong></summary>

1. `Gate/FeatureFlagPipelineBehavior.cs`: `[PipelineStage(PipelineStage.Authorization, Order = 100)]`; reads `FeatureFlagMetadata<TRequest>`; no attribute: `nextStep()`; attribute: `IFeatureFlagProvider.IsEnabledAsync(name, FeatureFlagContext.From(context), ct)`; enabled: `nextStep()`; disabled: `Left` `encina.featureflags.disabled` with the flag name in `details`; provider `Left`: the policy of #345 Design Choice 8 (fail closed: return `provider_failed`; `FeatureFlagsOptions.DefaultWhenUnavailable = true` is the explicit, logged opt-out). The XML docs state the position dependency of Design Choice 7.
2. Registration: `TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(FeatureFlagPipelineBehavior<,>)))` in `AddEncinaFeatureFlags` (Design Choice 10).
3. `Diagnostics/FeatureFlagGateMetrics.cs`: counter `encina.featureflags.gate_evaluations` (tags `feature`, `outcome`), span `encina.featureflags.gate`; no tenant or user id (Design Choice 9). `Diagnostics/FeatureFlagGateLogMessages.cs`: `[LoggerMessage]` EventIds in 7306-7309 (inside the range registered by #604 Phase 3): 7306 gate rejected a disabled feature, 7307 provider failed (error code and exception type), 7308 provider rejected the flag name (`invalid_flag_name`, a configuration defect), 7309 default applied (explicit opt-out). Requests without the attribute are not logged.
4. Tests: `FeatureFlagPipelineBehaviorTests` (enabled, disabled, no attribute, provider failure with and without the opt-out, tenant targeting from the request context), metrics tests, a registration test (`ValidateOnBuild`, `ValidateScopes`), guard tests, `FeatureFlagGateContractTests` (the behavior over the in-memory-store provider of #604 and the library provider), the benchmark class `FeatureFlagGateBenchmarks` (gate overhead per request, cached and uncached, through `BenchmarkSwitcher`) and the `BenchmarkTests` project reference; manifest entries and `PublicAPI.Unshipped.txt`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
- Issue #300, Phase 3. The EventId range 7300-7319 is registered by #604 Phase 3; this plan uses 7306-7309.
TASK:
- Implement the behavior, its registration, the gate diagnostics and log messages, and their tests and benchmark.
KEY RULES:
- Behavior is a pipeline behavior with the real signature Handle(request, context, nextStep, cancellationToken).
- A Left is never turned into success unless DefaultWhenUnavailable says so, and that case is logged by code.
- No EncinaError.Message, tenant id or user id in logs or tags.
- EventIds 7306-7309 only, sequential.
- At most 15 files and 1500 lines.
REFERENCE FILES:
- src/Encina/Abstractions/IPipelineBehavior.cs
- src/Encina.Compliance.Consent/ServiceCollectionExtensions.cs
- src/Encina.FeatureFlags/Diagnostics/FeatureFlagsLogMessages.cs
```

</details>

---

### Phase 4: Documentation, Justifications and Finalization

> **Goal**: Document the gate and justify the test types that are not applicable.

<details>
<summary><strong>Tasks</strong></summary>

1. README of `Encina.FeatureFlags`: the gate, an `appsettings` example, tenant targeting.
2. `changelog.d/300-feature-flag-gate.added.md`; `docs/INVENTORY.md`.
3. Justification files: `tests/Encina.IntegrationTests/FeatureFlags/FeatureFlagGate.md` (configuration-backed library, no external service), `tests/Encina.LoadTests/FeatureFlags/FeatureFlagGate.md` and `tests/Encina.PropertyTests/FeatureFlags/FeatureFlagGate.md` (stateless binary decision, no concurrent shared state, no invariant beyond the unit tests).
4. The ADR for D1 and D2 of the issue is written once by #345 (ADR number assigned when the ADR pull request opens).
5. `dotnet build --configuration Release` with 0 warnings; tests pass; per-flag coverage of every new file at its target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
- Issue #300, Phase 4; documentation goes through docs-writer, the fragment through mechanical-fixer.
TASK:
- Write the README section, the fragment, the INVENTORY update and the three justification files.
KEY RULES:
- Never edit CHANGELOG.md; coverage figures only through covref markers.
- This pull request carries Fixes #300.
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
| Maintainer comment 2026-03-18 on #345 | Library as foundation; `[FeatureFlag]`; cached metadata | Design Choices 4 and 8 |
| AGENTS.md section 3 | ROP; time from `TimeProvider`; fail closed; no personal data in logs | Typed `Left`, no default value |
| ADR 062 | Error category contract (merged in PR #3599) | Error codes of #604 carry their category |
| Microsoft.FeatureManagement documentation | `IFeatureManager`, targeting context, configuration section | Provider adapter |
| ADR-018 | 12 cross-cutting functions | Matrix below |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `IPipelineBehavior<,>`, `RequestHandlerCallback<TResponse>` | `src/Encina/Abstractions/IPipelineBehavior.cs`, `RequestHandlerDelegate.cs:9` | Behavior contract |
| `PipelineStage`, `PipelineStageAttribute` | `src/Encina/Pipeline/Ordering/` | Stage declaration |
| `IRequestContext` | `src/Encina/Abstractions/IRequestContext.cs` | Context mapping |
| `EncinaErrors.Create` (with `ErrorCategory`) | `src/Encina/Results/EncinaErrors.cs` | Error factory, through `FeatureFlagErrors` of #604 |
| Attribute cache pattern, `TryAddEnumerable` registration | `src/Encina.Compliance.Consent/ConsentRequiredPipelineBehavior.cs:57`, `ServiceCollectionExtensions.cs:98` | Same shape |
| `IFeatureFlagProvider`, cache decorator, log class | `Encina.FeatureFlags` (#604) | Evaluation, caching, EventId range |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.FeatureFlags` | 7300-7319 (registered by #604 Phase 3) | #604 7300-7305, **#300 7306-7309** (all in Phase 3), #345 7310-7315; 7316-7319 unallocated; #451 owns 7330-7339 |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Attribute, metadata, context (Phase 1) | 9 | With tests, manifest, PublicAPI |
| Library provider (Phase 2) | 10 | Package pin, provider, registration, tests |
| Behavior and diagnostics (Phase 3) | 13 | Behavior, metrics, logs, registration, tests, benchmark |
| Documentation and justifications (Phase 4) | 6 | README, fragment, INVENTORY, 3 justification files |
| **Total** | **38** | Four pull requests |

#### Pull Request Split

Files include tests, manifests, `PublicAPI` files, project references and the changelog fragment; lines are estimates re-measured when each pull request opens. Every pull request is at most 15 files and 1500 lines; no pull request uses a type or log class that a later one creates.

| Phase | Content | Files | Lines |
|-------|---------|------:|------:|
| 1 | Attribute, metadata, `FeatureFlagContext.From`, manifest, PublicAPI, 3 unit test files, 1 guard test file | 9 | 450 |
| 2 | `Directory.Packages.props`, csproj, provider, registration edit, manifest, PublicAPI, provider, registration, guard and contract tests | 10 | 700 |
| 3 | Behavior, metrics, log messages, registration edit, manifest, PublicAPI, behavior, metrics, registration, guard and contract tests, benchmark class and project reference | 13 | 800 |
| 4 | README, fragment, INVENTORY, 3 justification files | 6 | 300 |
| **Total** | 4 pull requests | **38** | **2,250** |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #300: the single-flag feature gate on Microsoft.FeatureManagement.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library with Railway Oriented Programming; pre-1.0, no compatibility code. ADR 062 (error categories) is merged.
- #604 Phases 1-3 (package, IFeatureFlagProvider, errors, options, EventId range) are merged before this work.
- Provider category: none.

IMPLEMENTATION OVERVIEW:
Phase 1: [FeatureFlag], FeatureFlagMetadata<TRequest>, FeatureFlagContext.From.
Phase 2: MicrosoftFeatureManagementProvider and registration.
Phase 3: FeatureFlagPipelineBehavior, stage, gate metrics and logs, EventIds 7306-7309, benchmark.
Phase 4: documentation and justification files.

KEY PATTERNS:
- Attribute on the request type, metadata cached per type.
- Disabled is a Left (encina.featureflags.disabled); never a default value or an exception.
- Context from IRequestContext; one cache (the decorator of #604).
- Stage Authorization, order 100.
- Each phase is one pull request of at most 15 files and 1500 lines with its own tests, manifest entries and diagnostics.

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
| 2 | OpenTelemetry | ✅ Phase 3 | Counter `encina.featureflags.gate_evaluations` and span `encina.featureflags.gate`, flag name and outcome only (Design Choice 9) |
| 3 | Structured Logging | ✅ Phase 3 | EventIds 7306-7309 |
| 4 | Health Checks | ⏭️ Deferred | In-process for the library provider; remote providers: follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/feature-flag-provider-health-checks.md) |
| 5 | Validation | ✅ Phases 1, 3 | The only input is the flag name: the attribute constructor rejects an empty name, the providers reject a malformed one with `invalid_flag_name` |
| 6 | Resilience | ✅ Phase 3 | The gate fails closed when the provider returns a `Left` (#345 Design Choice 8); in-process library, remote-provider resilience is #604, #451 and #2694 |
| 7 | Distributed Locks | ❌ N/A | Stateless evaluation |
| 8 | Transactions | ❌ N/A | Read-only; the gate runs before the transaction stage |
| 9 | Idempotency | ❌ N/A | No side effects |
| 10 | Multi-Tenancy | ✅ Phase 1 | Tenant from `IRequestContext.TenantId` in `FeatureFlagContext`; the tenant is not a metric tag (Design Choice 9) |
| 11 | Module Isolation | ❌ N/A | Flags are named by the application and shared across modules; the request's module name can already be passed as a context attribute by a custom provider |
| 12 | Audit Trail | ⏭️ Deferred | Flag change audit belongs to the provider issues: follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/feature-flag-change-audit.md); gate decisions are metered and logged here |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| `Encina.FeatureFlags` package, contract, options and core EventId range | #604 Phases 1-3, open | Must merge first |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Umbrella: multi-flag, variants, redirection, family ADR | #345 | Starts after this plan |
| OpenFeature and ConfigCat | #451 | Same contract |
| SaaS providers | #2694 | Outside 1.0 |

---

## Next Steps

1. Review this plan; the Dilemmas (1, 2, 3, 5, 6, 7 and 9) are asked when work on the issue starts.
2. Post the plan link on #300.
3. Start after #604 Phases 1-3 merge.
4. Each phase is one pull request of at most 15 files and 1500 lines; the last carries `Fixes #300`.
