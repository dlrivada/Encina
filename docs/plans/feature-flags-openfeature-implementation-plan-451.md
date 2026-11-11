# Implementation Plan: `Encina.FeatureFlags.OpenFeature` and `.ConfigCat` — Feature Flag Providers

> **Issue**: [#451](https://github.com/dlrivada/Encina/issues/451)
> **Type**: Feature
> **Complexity**: Medium (7 phases, no database provider)
> **Estimated Scope**: ~800 lines of production code + ~1,400 lines of tests, in about 6 pull requests

---

## Summary

Add two provider packages to the feature-flag family: `Encina.FeatureFlags.OpenFeature` (the CNCF OpenFeature SDK, which reaches flagd and many vendors) and `Encina.FeatureFlags.ConfigCat` (the ConfigCat .NET SDK, subject to a Dilemma). Neither package exists in `src/` today, and `Directory.Packages.props` has no OpenFeature or ConfigCat reference. The part of the original issue that needs a paid cloud account moved to #2694 on 2026-10-23.

This plan does **not** define the abstraction. Ownership across the family (same table in #300, #345 and #604):

| Piece | Owner |
|---|---|
| Package `Encina.FeatureFlags`, `IFeatureFlagProvider`, `FeatureFlagContext(UserId, TenantId, Attributes)`, errors, cache decorator, EventId ranges 7300-7339 | #604 |
| Gate behavior, `[FeatureFlag]`, `FeatureFlagContext.From(IRequestContext)`, library provider | #300 |
| Umbrella: multi-flag, variants, redirection, filters, family ADR | #345 |
| **OpenFeature adapter (EventIds 7330-7334), ConfigCat adapter (7335-7339, only if its Dilemma is decided for inclusion), handler-side helper** | **#451 (this plan)** |

The `FeatureFlagContext(UserId, TenantId, Environment)` of the issue sketch is replaced by #604's record; `Environment` becomes the attribute `environment`.

**Affected packages**: new `Encina.FeatureFlags.OpenFeature`, `Encina.FeatureFlags.ConfigCat` (conditional), `Encina.FeatureFlags` (one extension class), `tests/Encina.TestInfrastructure` (flagd fixture). **Provider category**: none (feature flags are not a database, cache or transport category).

---

## Design Choices

<details>
<summary><strong>1. How a handler gets its flag value</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The handler injects `IFeatureFlagProvider` and evaluates; a helper builds the context from `IRequestContext` (ambient through `IRequestContextAccessor`)** | Works with today's handler contract; explicit; no new mechanism | The handler code calls the provider |
| **B) A `[Feature("key")] bool useNewFlow` parameter bound by a `FeatureFlagInjectionBehavior`** | Declarative | `ICommandHandler<TCommand,TResponse>.Handle(request, cancellationToken)` (`src/Encina/Abstractions/IRequestHandler.cs`) has no parameter-binding mechanism; it needs a new dispatcher feature or source generator |

### Chosen Option: **A — Inject the provider (recommended, pending the maintainer)**

### Rationale

- A wins when only a few handlers branch on a flag, and costs no new mechanism. B wins when many handlers branch and a binding mechanism is accepted as new core work (it would be its own issue).
- `IRequestContextAccessor` (`src/Encina/Abstractions/IRequestContextAccessor.cs`) already gives a handler the ambient context. Phase 5 adds `FeatureFlagProviderExtensions.IsEnabledAsync(provider, name, IRequestContext, ct)`.
- The acceptance criterion "flag value reaches handlers" is met by A; under B the injection behavior would use EventIds 7316-7319 (reserved in #604's table for that case only).

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Is ConfigCat a 1.0 provider?</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Include `Encina.FeatureFlags.ConfigCat`** | A native provider for a cost-effective service; the issue's original table | SaaS without a free local emulator; tests would use the SDK's offline mode, not the service |
| **B) OpenFeature only; ConfigCat users reach the service through its OpenFeature provider** | Matches SPEC-000 section 6 (no cloud service without a free local emulator); one adapter to maintain | One native provider fewer |

### Chosen Option: **B — OpenFeature only (recommended, pending the maintainer)**

### Rationale

- A wins when users ask for ConfigCat specifically and the offline-mode tests are accepted. B wins when local-only testing is required, which is the 1.0 rule for services.
- Under B, Phase 4 is dropped, the EventId block 7335-7339 stays unused and the acceptance criterion "at least 2 providers" is met by #604's providers plus OpenFeature; under A, Phase 4 is built as written.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Provider selection — by registration, not by an enum</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AddEncinaFeatureFlagsOpenFeature(options)` (and `...ConfigCat`) next to `AddEncinaFeatureFlags`; switching provider means changing the registration** | Provider coherence; core knows no provider | Two calls |
| **B) `config.Provider = FeatureFlagProvider.ConfigCat` (the issue sketch)** | One call | The core package would reference every provider |

### Chosen Option: **A — Registration per provider package**

### Rationale

- AGENTS.md section 3: providers share the interface and differ only in the registration; satellite DI is `AddEncina{Feature}{Provider}` with `TryAdd*`.
- `CacheExpiration` of the sketch becomes `FeatureFlagsOptions.CacheDuration` (#604), the only cache.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (provider coherence; registration completeness); #604 plan ownership of the contract

</details>

<details>
<summary><strong>4. Adapter semantics — errors become Left, listing is not supported</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Use the SDK's detailed evaluation; any error type other than none becomes `Left` `provider_failed` (or `not_found` for a missing flag); `GetAllFlagsAsync` and `GetFlagAsync` return `Left` `encina.featureflags.not_supported`** | Honest about failures; matches #604 Design Choice 3 | Callers of the listing methods must handle `not_supported` |
| **B) Use the SDK's value API, which returns the default on any error** | Simplest | Hides provider failure; the caller cannot tell a default from an answer |

### Chosen Option: **A — Detailed evaluation, errors as Left**

### Rationale

- The OpenFeature specification returns the supplied default plus an error code on failure and defines no operation to enumerate flags, so listing cannot be implemented without inventing semantics.
- AGENTS.md section 3: errors are never swallowed; the error code, not its message, is recorded (OpenFeature error type name as a tag).

**Class:** Settled
**Settled by:** AGENTS.md section 3 (errors never swallowed; `EncinaError.Message` never logged); OpenFeature specification (evaluation details with error code; no flag enumeration); #604 Design Choice 3 for the shared failure semantics

</details>

<details>
<summary><strong>5. Context mapping to the SDK evaluation context</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `TargetingKey` = `UserId`; `tenantId` and every `Attributes` entry as context attributes; the tenant also added to the cache key by #604** | Standard OpenFeature targeting; tenant-aware rules work in flagd | An anonymous request has no targeting key (percentage rules then fall back to the default) |
| **B) A random targeting key when the user is missing** | Percentage rules always evaluate | Non-deterministic: the same caller flips between requests |

### Chosen Option: **A — Targeting key from the user, tenant as an attribute**

### Rationale

- Deterministic evaluation is the family rule (#604 Design Choice 8).
- The anonymous case is documented; a missing key returns the flag's default through the SDK.

**Class:** Trivial
**Settled by:** OpenFeature specification (targeting key, evaluation context attributes); #604 Design Choice 8 (deterministic bucketing)

</details>

<details>
<summary><strong>6. Test infrastructure — flagd in Docker with a vendor-LTS tag defined once</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `FlagdFixture` (Testcontainers) in `Encina.TestInfrastructure`, flag file mounted, `[Collection("FeatureFlags-Flagd")]`** | Real SDK to real flagd; no cloud account; follows existing fixtures | Needs Docker in CI integration |
| **B) An in-memory OpenFeature provider only** | No Docker | Does not exercise the network provider the package is for |

### Chosen Option: **A — flagd container, one image source**

### Rationale

- Integration tests of the family run against local services only (SPEC-000 section 6); AGENTS.md section 9 requires `[Collection]` fixtures for shared containers.
- The image tag is the vendor's LTS or stable tag defined in one place, as for the other provider fixtures (`tests/Encina.TestInfrastructure/Fixtures/RedisFixture.cs`).

**Class:** Settled
**Settled by:** SPEC-000 section 6 (local emulators only); AGENTS.md section 9 (integration fixtures); `tests/Encina.TestInfrastructure/Fixtures/` (existing fixture pattern)

</details>

---

## Implementation Phases

### Phase 1: OpenFeature Package and Adapter

> **Goal**: `OpenFeatureFeatureFlagProvider` over the SDK client.

<details>
<summary><strong>Tasks</strong></summary>

1. `Directory.Packages.props`: pin `OpenFeature` (and, for tests, the flagd provider package) at the latest stable, one source.
2. `src/Encina.FeatureFlags.OpenFeature/Encina.FeatureFlags.OpenFeature.csproj` referencing `Encina.FeatureFlags` and `OpenFeature`.
3. `OpenFeatureFeatureFlagProvider.cs`: `IsEnabledAsync` (boolean details), `GetVariationAsync<T>` (boolean, string, integer, double or structure details by `T`), context mapping (Design Choice 5).
4. `OpenFeatureFeatureFlagsOptions.cs` (client name, domain), `PublicAPI.Unshipped.txt`, manifest entry.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
- Issue #451, Phase 1. #604 Phase 1 (package, contract) is merged.
TASK:
- Create Encina.FeatureFlags.OpenFeature with the provider, the context mapping and the options.
KEY RULES:
- Either results; no exception for flag errors.
- Packages only in Directory.Packages.props; XML docs on every public member.
- Options carry no secret; if an endpoint token is added later it is [JsonIgnore] with a ToString() override.
REFERENCE FILES:
- src/Encina.FeatureFlags/Abstractions/IFeatureFlagProvider.cs
- src/Encina.Caching.Redis/ (package and options layout)
```

</details>

---

### Phase 2: Error Mapping, Unsupported Operations and Registration

> **Goal**: Design Choice 4 and the DI surface.

<details>
<summary><strong>Tasks</strong></summary>

1. Map the SDK error types: flag not found to `not_found`; everything else to `provider_failed`; record the error type name only.
2. `GetAllFlagsAsync` and `GetFlagAsync` return `Left` `not_supported` (new code in `FeatureFlagErrors`, added by this plan: one small edit in `Encina.FeatureFlags`).
3. `ServiceCollectionExtensions.AddEncinaFeatureFlagsOpenFeature` (`TryAdd*`); DI test with `ValidateOnBuild` and `ValidateScopes`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
- Issue #451, Phase 2.
TASK:
- Implement the error mapping, the not_supported operations and the registration.
KEY RULES:
- Never log or tag EncinaError.Message or the SDK message; use the error type name.
- Registration never overrides the application's own IFeatureFlagProvider.
REFERENCE FILES:
- src/Encina.FeatureFlags/FeatureFlagErrors.cs
- src/Encina.Compliance.Consent/ServiceCollectionExtensions.cs
```

</details>

---

### Phase 3: Observability and the flagd Fixture

> **Goal**: Diagnostics for the OpenFeature package and the container fixture.

<details>
<summary><strong>Tasks</strong></summary>

1. `Diagnostics/OpenFeatureLogMessages.cs`: `[LoggerMessage]` EventIds 7330-7334 (evaluation error mapped, unsupported operation, context attribute skipped, client domain missing, hook failure); counters on the `Encina.FeatureFlags` meter from #604 (`encina.featureflags.evaluations`, tag `provider` = `openfeature`).
2. `tests/Encina.TestInfrastructure/Fixtures/FlagdFixture.cs` and its `[Collection("FeatureFlags-Flagd")]` definition; a flag file with a boolean flag, a targeting rule and a string flag.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
- Issue #451, Phase 3. The range 7330-7334 is registered by #604 Phase 1.
TASK:
- Add the log messages, the metric tags and the flagd fixture.
KEY RULES:
- EventIds packed sequentially inside 7330-7334; XML docs name the range.
- The fixture follows the existing Testcontainers fixtures; the image tag is defined in one place.
REFERENCE FILES:
- tests/Encina.TestInfrastructure/Fixtures/RedisFixture.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

### Phase 4: ConfigCat Provider (only if Design Choice 2 is decided for inclusion)

> **Goal**: `Encina.FeatureFlags.ConfigCat`.

<details>
<summary><strong>Tasks</strong></summary>

1. `Directory.Packages.props`: pin `ConfigCat.Client`; project `src/Encina.FeatureFlags.ConfigCat/`.
2. `ConfigCatFeatureFlagProvider.cs` (user object from `FeatureFlagContext`: identifier, custom attributes, tenant), `ConfigCatFeatureFlagsOptions.cs` (SDK key `[JsonIgnore]`, `ToString()` override, poll interval from options, `TimeProvider` not needed by the SDK), registration, EventIds 7335-7339.
3. Tests with the SDK's offline local-dictionary mode (the real service has no emulator).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
- Issue #451, Phase 4; build only after the maintainer decides Design Choice 2 for inclusion.
TASK:
- Create Encina.FeatureFlags.ConfigCat with provider, options, registration and offline-mode tests.
KEY RULES:
- The SDK key is a secret: [JsonIgnore] and a ToString() override.
- Errors become Left; no default value reported as success.
REFERENCE FILES:
- src/Encina.FeatureFlags.OpenFeature/ (sibling adapter)
- src/Encina.FeatureFlags/FeatureFlagErrors.cs
```

</details>

---

### Phase 5: Handler-Side Helper

> **Goal**: Design Choice 1 option A for handlers.

<details>
<summary><strong>Tasks</strong></summary>

1. `Encina.FeatureFlags/FeatureFlagProviderExtensions.cs`: `IsEnabledAsync(this IFeatureFlagProvider, string name, IRequestContext context, CancellationToken)` using `FeatureFlagContext.From(context)` (#300), and an overload that reads `IRequestContextAccessor`.
2. Unit and guard tests; usage example in the package README (a handler branching on a flag).
3. If the maintainer chooses option B: this phase is replaced by a separate issue for a parameter-binding mechanism and the injection behavior (EventIds 7316-7319).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
- Issue #451, Phase 5.
TASK:
- Add the extension methods and their tests.
KEY RULES:
- A missing request context returns Left, never "enabled" (fail closed, AGENTS.md section 3).
REFERENCE FILES:
- src/Encina/Abstractions/IRequestContextAccessor.cs
- src/Encina.FeatureFlags/Model/FeatureFlagContextExtensions.cs
```

</details>

---

### Phase 6: Testing

> **Goal**: Every applicable test type.

<details>
<summary><strong>Tasks</strong></summary>

1. Unit: mapping, error conversion, unsupported operations; DI registration.
2. Guard: every public method with parameters.
3. Contract: the shared `IFeatureFlagProvider` contract suite from #604 runs against the OpenFeature provider (and ConfigCat if built).
4. Property: percentage rules over the SDK's in-memory provider (deterministic per targeting key).
5. Integration: flagd container, `[Collection("FeatureFlags-Flagd")]`, `ClearAllDataAsync`-style reset of the flag file.
6. Benchmark: cached evaluation is #345's; load test justified (stateless reads).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
- Issue #451, Phase 6; targets in .github/coverage-manifest/Encina.FeatureFlags.OpenFeature.json.
TASK:
- Write the tests of the Phase 6 Tasks.
KEY RULES:
- Real adapter code, real SDK; shared Collection fixture, never IClassFixture for containers.
- CRAP <= 10 on changed methods.
REFERENCE FILES:
- docs/testing/integration-tests.md
- docs/en/guides/TESTING.md
```

</details>

---

### Phase 7: Documentation and Finalization

> **Goal**: Document the providers.

<details>
<summary><strong>Tasks</strong></summary>

1. Package README for each new package; section in the feature-flag guide (written by #345) on choosing a provider.
2. `changelog.d/451-feature-flags-openfeature.added.md`, `docs/INVENTORY.md`.
3. The ADR of the family is written by #345 (ADR number assigned when the ADR pull request opens).
4. `dotnet build --configuration Release` with 0 warnings; tests pass; per-flag coverage at target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT:
- Issue #451, Phase 7; documentation goes through docs-writer.
TASK:
- Write READMEs, the fragment and the INVENTORY update.
KEY RULES:
- Never edit CHANGELOG.md; state that SaaS providers are #2694, outside 1.0.
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
| OpenFeature specification (CNCF) | Evaluation context, targeting key, evaluation details with error codes | Adapter semantics (Design Choices 4 and 5) |
| SPEC-000 section 6, DEC-009 | Cloud services without a free local emulator are outside 1.0; the cloud half of #451 is #2694 | Design Choice 2 |
| AGENTS.md section 3 | Provider coherence; errors never swallowed; secrets in options | Design Choices 3 and 4 |
| ADR-018 | 12 cross-cutting functions | Matrix below |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `IFeatureFlagProvider`, `FeatureFlagContext`, `FeatureFlagErrors` | `Encina.FeatureFlags` (#604) | Contract the adapters implement |
| `CachedFeatureFlagProvider` | `Encina.FeatureFlags` (#604) | The only cache |
| `IRequestContext`, `IRequestContextAccessor` | `src/Encina/Abstractions/` | Handler-side helper |
| `EventIdRanges` | `src/Encina/Diagnostics/EventIdRanges.cs` | Ranges 7330-7339 registered by #604 |
| Provider fixtures | `tests/Encina.TestInfrastructure/Fixtures/` | Pattern for `FlagdFixture` |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| **`Encina.FeatureFlags.OpenFeature`** | **7330-7334** | New (registered by #604 Phase 1) |
| **`Encina.FeatureFlags.ConfigCat`** | **7335-7339** | Only if Design Choice 2 is decided for inclusion |
| `Encina.FeatureFlags` | 7316-7319 | Reserved for #451 only if Design Choice 1 is decided for option B |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| OpenFeature package (Phases 1-3) | ~14 | Project, provider, options, registration, diagnostics, PublicAPI, manifest |
| ConfigCat package (Phase 4) | ~9 | Conditional |
| Helper (Phase 5) | ~4 | |
| Tests (Phase 6) | ~16 | Includes the flagd fixture |
| Documentation (Phase 7) | ~4 | |
| **Total** | **~47** | Six pull requests (five without ConfigCat) |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #451: OpenFeature (and, if decided, ConfigCat) providers for Encina feature flags.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library with Railway Oriented Programming; pre-1.0, no compatibility code.
- #604 owns the package, IFeatureFlagProvider and the EventId ranges; #300 and #345 own the gate. This issue only adds providers and one helper.
- Provider category: none.

IMPLEMENTATION OVERVIEW:
Phase 1: OpenFeature adapter. Phase 2: error mapping, not_supported, registration.
Phase 3: diagnostics 7330-7334 and the flagd fixture. Phase 4: ConfigCat (conditional).
Phase 5: handler-side helper. Phase 6: tests. Phase 7: documentation.

KEY PATTERNS:
- Provider by registration (AddEncinaFeatureFlagsOpenFeature), TryAdd*.
- SDK errors become Left; never a default reported as success.
- Targeting key from the user; tenant as an attribute; secrets with [JsonIgnore] and ToString().
- Each phase is one pull request of at most 15 files and 1500 lines.

REFERENCE FILES:
- docs/plans/feature-flags-openfeature-implementation-plan-451.md (this plan)
- src/Encina.FeatureFlags/Abstractions/IFeatureFlagProvider.cs
- tests/Encina.TestInfrastructure/Fixtures/RedisFixture.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Phase 1 | Uses the cache decorator of #604 (`CacheDuration`); no cache of its own |
| 2 | OpenTelemetry | ✅ Phase 3 | Family counter and span with `provider` = `openfeature` |
| 3 | Structured Logging | ✅ Phase 3 | EventIds 7330-7334 |
| 4 | Health Checks | ⏭️ Deferred | Flag service connectivity: issue file `feature-flag-provider-health-checks` in `artifacts/issues/` |
| 5 | Validation | ❌ N/A | Flag keys come from code and configuration; options validated on start |
| 6 | Resilience | ✅ Phase 2 | SDK failures become `provider_failed`; the gate's `DefaultWhenUnavailable` (#345) decides the fallback; provider-side retries belong to the SDK provider |
| 7 | Distributed Locks | ❌ N/A | Stateless reads |
| 8 | Transactions | ❌ N/A | Reads only |
| 9 | Idempotency | ❌ N/A | Evaluation is a pure read |
| 10 | Multi-Tenancy | ✅ Phase 1 | Tenant passed as an evaluation attribute and part of the cache key |
| 11 | Module Isolation | ❌ N/A | Flags are cross-cutting; module-scoped flags are the item deferred by #604 and #345 |
| 12 | Audit Trail | ⏭️ Deferred | Flag change audit: issue file `feature-flag-change-audit` in `artifacts/issues/` (flag state lives in the external service) |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| `Encina.FeatureFlags` and `IFeatureFlagProvider` | #604 Phase 1, open | Must merge first |
| `FeatureFlagContext.From(IRequestContext)` | #300 Phase 1, open | Used by the helper (Phase 5) |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Umbrella and family ADR | #345 | |
| SaaS providers (Azure App Configuration, LaunchDarkly, Split.io) | #2694 | Outside 1.0 |

---

## Next Steps

1. Review this plan; the Dilemmas (1 and 2) are asked when work on the issue starts.
2. Post the plan link on #451.
3. Start after #604 Phase 1 and #300 Phase 1 merge.
4. Each phase is one pull request of at most 15 files and 1500 lines; the last carries `Fixes #451`.
