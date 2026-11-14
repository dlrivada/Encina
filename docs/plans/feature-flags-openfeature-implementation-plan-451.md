# Implementation Plan: `Encina.FeatureFlags.OpenFeature` and `.ConfigCat` — Feature Flag Providers

> **Issue**: [#451](https://github.com/dlrivada/Encina/issues/451)
> **Type**: Feature
> **Complexity**: Medium (7 pull requests with ConfigCat, 5 without, no database provider)
> **Estimated Scope**: about 4,050 changed lines in 7 pull requests with ConfigCat (2,750 in 5 without it), tests, manifests and PublicAPI files included (table "Pull request split" under Research; each pull request is at most 15 files and 1,500 lines)

---

## Summary

Add two provider packages to the feature-flag family: `Encina.FeatureFlags.OpenFeature` (the CNCF OpenFeature SDK, which reaches flagd and many vendors) and `Encina.FeatureFlags.ConfigCat` (the ConfigCat .NET SDK, subject to a Dilemma). Neither package exists in `src/` today, and `Directory.Packages.props` has no OpenFeature or ConfigCat reference. The part of the original issue that needs a paid cloud account moved to #2694 on 2026-10-23.

This plan does **not** define the abstraction. Ownership across the family (same table in #300, #345 and #604):

| Piece | Owner |
|---|---|
| Package `Encina.FeatureFlags`, `IFeatureFlagProvider`, `FeatureFlagContext(UserId, TenantId, Attributes)`, `FeatureFlagErrors` (with `ErrorCategory`, ADR 062), cache decorator, core EventId range 7300-7319 | #604 |
| Gate behavior, `[FeatureFlag]`, `FeatureFlagContext.From(IRequestContext)`, library provider | #300 |
| Umbrella: multi-flag, variants, redirection, filters, family ADR | #345 |
| **OpenFeature adapter (EventIds 7330-7334), ConfigCat adapter (7335-7339, only if its Dilemma is decided for inclusion), handler-side helper; this plan registers both ranges itself** | **#451 (this plan)** |

The `FeatureFlagContext(UserId, TenantId, Environment)` of the issue sketch is replaced by #604's record; `Environment` becomes the attribute `environment`.

**Error contract**: ADR 062 (error category contract, merged in PR #3599) is on `main`. The two codes this plan adds to `FeatureFlagErrors` (a file owned by #604; the edits are listed in the Phases that need them and carry a PublicAPI line) are created with the category overload of `EncinaErrors.Create`:

| Code | `ErrorCategory` | Added in |
|---|---|---|
| `encina.featureflags.not_supported` | `InvalidRequest` | Phase 2 (listing methods and unsupported variation types) |
| `encina.featureflags.context_missing` | `FailedPrecondition` | Phase 5 (handler helper called without a request context) |

**Affected packages**: new `Encina.FeatureFlags.OpenFeature`, `Encina.FeatureFlags.ConfigCat` (conditional), `Encina.FeatureFlags` (one extension class and two error codes), `tests/Encina.TestInfrastructure` (flagd fixture). **Provider category**: none (feature flags are not a database, cache or transport category).

---

## Design Choices

<details>
<summary><strong>1. How a handler gets its flag value</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The handler injects `IFeatureFlagProvider` and evaluates; a helper builds the context from `IRequestContext` (ambient through `IRequestContextAccessor`)** | Works with today's handler contract; explicit; no new mechanism | The handler code calls the provider |
| **B) A `[Feature("key")] bool useNewFlow` parameter bound by a `FeatureFlagInjectionBehavior`** | Declarative | `ICommandHandler<TCommand,TResponse>` (`src/Encina/Abstractions/CqrsContracts.cs:79`) and `IRequestHandler` (`src/Encina/Abstractions/IRequestHandler.cs`, which declares `Handle(request, cancellationToken)`) have no parameter-binding mechanism; it needs a new dispatcher feature or source generator |

### Chosen Option: **A — Inject the provider (recommended, pending the maintainer)**

### Rationale

- A wins when only a few handlers branch on a flag, and costs no new mechanism. B wins when many handlers branch and a binding mechanism is accepted as new core work (it would be its own issue, which takes its own EventIds from the registry).
- `IRequestContextAccessor` (`src/Encina/Abstractions/IRequestContextAccessor.cs`) already gives a handler the ambient context. Phase 5 adds `FeatureFlagProviderExtensions.IsEnabledAsync(provider, name, IRequestContext, ct)`.
- The acceptance criterion "flag value reaches handlers" is met by A.

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
- Under B, Phases 4a and 4b are dropped (the pull requests are 5 and the EventId block 7335-7339 stays unused) and the acceptance criterion "at least 2 providers" is met by #604's providers plus OpenFeature; under A, Phases 4a and 4b are built as written.

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

- Deterministic evaluation is the family rule (#604 Design Choice 9, the maintainer's bucketing decision); B is not deterministic, so it cannot comply.
- The anonymous case is documented; a missing key returns the flag's default through the SDK.

**Class:** Settled
**Settled by:** OpenFeature specification (targeting key, evaluation context attributes); #604 Design Choice 9 (deterministic bucketing, maintainer comment of 2026-03-18 on #345)

</details>

<details>
<summary><strong>6. Test infrastructure — flagd in Docker or an in-memory provider only</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `FlagdFixture` (Testcontainers) in `Encina.TestInfrastructure`, flag file mounted, `[Collection("FeatureFlags-Flagd")]`; the image tag is a single constant in the fixture (the vendor's stable tag; `RedisFixture` today hard-codes `redis:7-alpine` inline, so this plan does not claim an existing shared source)** | Real SDK to real flagd; no cloud account; follows existing fixtures | Needs Docker in CI integration |
| **B) An in-memory OpenFeature provider only** | No Docker; wins when CI has no Docker for this suite | Does not exercise the network provider the package is for; AGENTS.md section 9 asks a justification for skipped non-database integration |

### Chosen Option: **A — flagd container (recommended, pending the maintainer)**

### Rationale

- A wins when the package must be proven against a real flag service that runs locally (SPEC-000 section 6 allows local emulators); B wins when the Docker cost outweighs the extra assurance.
- AGENTS.md section 9 requires `[Collection]` fixtures for shared containers; the in-memory provider is still used for the property tests of Phase 3.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Ownership of the OpenFeature <code>Api</code> and the SDK provider lifecycle</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The application owns the OpenFeature `Api` singleton and sets its SDK provider (flagd or another); the Encina adapter gets a client for the configured domain (`OpenFeatureFeatureFlagsOptions.ClientDomain`, default: the default domain) and never calls `SetProviderAsync` or shutdown** | `Api` is process-global, so no conflict with other users of OpenFeature; the application chooses the vendor | An application that registers the adapter without setting a provider evaluates against the SDK's no-op provider (the flag default is returned with an error type that maps to `provider_failed`) |
| **B) The adapter owns it: options accept an SDK provider factory; a hosted service sets the provider at start and shuts down at stop** | One registration call is enough | Two components fighting over the global `Api`; hides the vendor choice inside Encina options |

### Chosen Option: **A — The application owns the `Api` and the SDK provider (recommended, pending the maintainer)**

### Rationale

- A wins for applications that already use OpenFeature elsewhere; B wins for applications that want Encina to be the only place configuring flags. Exact API names (`Api.Instance`, `GetClient`, provider set and shutdown calls) are confirmed against the pinned SDK version at the start of Phase 1.
- Initialization and shutdown failures of the SDK provider, when the application reports them through the options' callbacks, are logged by error type (EventIds 7333 and 7334).

**Class:** Dilemma

</details>

<details>
<summary><strong>8. Variation types supported by <code>GetVariationAsync&lt;T&gt;</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `T` is `bool`, `string`, `int` or `double`; any other `T` returns `Left` `not_supported`** | The four value kinds the SDK evaluates natively; no invented serialization | A structured flag value is not reachable through this contract |
| **B) Also structured values: a JSON object evaluated as the SDK's structure value and deserialized to `T`** | Structured flags work | A serialization contract (naming, nulls) that no other provider in the family has |

### Chosen Option: **A — Four scalar types (recommended, pending the maintainer)**

### Rationale

- A wins when flags are scalars (the usual case); B wins when applications keep configuration objects in flags.
- A missing flag returns the supplied `defaultValue` as `Right`, as #604 Design Choice 12 recommends (only when the SDK reports the flag as not found and that choice is decided for A).

**Class:** Dilemma

</details>

<details>
<summary><strong>9. Reserved attribute names and collisions</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The attributes `tenantId` and `environment` are set by the adapter: `tenantId` only from `FeatureFlagContext.TenantId`, never from `Attributes`; an `Attributes` entry with a reserved name is dropped and logged (EventId 7332, name only); `environment` may come from `Attributes` (the issue's `Environment` field)** | A caller cannot spoof the tenant used in targeting through a free attribute | A legitimate attribute named `tenantId` is dropped |
| **B) A matching `Attributes` entry overrides the explicit value** | Fewest surprises for callers who set it | Tenant spoofing through free attributes |

### Chosen Option: **A — The explicit tenant wins; reserved attributes are dropped and logged**

### Rationale

- Multi-tenancy must not be bypassable by input data, and a gate fails closed on missing or untrusted context.
- When `TenantId` is null, no `tenantId` attribute is sent.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (compliance and security gates fail closed; no personal or identifying data in logs: only the attribute name is logged); AGENTS.md section 6 function 10 (multi-tenancy)

</details>

<details>
<summary><strong>10. The handler helper without a request context</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IsEnabledAsync(provider, name, context, ct)` with a null context, or the accessor overload with no ambient context, returns `Left` `encina.featureflags.context_missing` (category `FailedPrecondition`)** | Fails closed; never reports a flag as enabled without context | The caller handles one more error |
| **B) Evaluate with an empty `FeatureFlagContext`** | Global flags still work | Targeting rules silently see no user or tenant |

### Chosen Option: **A — A missing context is a Left**

### Rationale

- Context missing means the gate denies; the only opt-out is explicit and logged.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (gates fail closed when context is missing)

</details>

---

## Implementation Phases

Each phase is one pull request: it carries its own unit, guard and (where it applies) contract, property and integration tests, its coverage-manifest entries, its `PublicAPI.Unshipped.txt` lines and the diagnostics of the code it adds. Every new file under `src/` gets per-flag targets in the package manifest, demanding and realistic, each with a one-sentence justification (the integration and property targets enter in Phase 3, which holds those tests); a target of 0 needs a reason. The table "Pull request split" under Research lists the files and lines of each phase.

### Phase 1: OpenFeature Package Skeleton and Options

> **Goal**: The `Encina.FeatureFlags.OpenFeature` project, its pinned package and its options.

<details>
<summary><strong>Tasks</strong></summary>

1. `Directory.Packages.props`: pin `OpenFeature` at the latest stable, one source.
2. `src/Encina.FeatureFlags.OpenFeature/Encina.FeatureFlags.OpenFeature.csproj` referencing `Encina.FeatureFlags` and `OpenFeature`, `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`, the entry in `Encina.slnx`.
3. `OpenFeatureFeatureFlagsOptions.cs` (`ClientDomain`, Design Choice 7; validated on start; no secret).
4. Tests: project references in the unit, guard, contract and integration test projects; options tests; a guard test; `.github/coverage-manifest/Encina.FeatureFlags.OpenFeature.json`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
- Issue #451, Phase 1. #604 Phases 1-3 (package, contract, options, core EventId range) are merged.
TASK:
- Create the OpenFeature project, the pinned package, the options, the manifest and the test project references.
KEY RULES:
- Packages only in Directory.Packages.props; XML docs on every public member.
- Options carry no secret; if an endpoint token is added later it is [JsonIgnore] with a ToString() override.
- At most 15 files and 1500 lines.
REFERENCE FILES:
- src/Encina.FeatureFlags/Abstractions/IFeatureFlagProvider.cs
- src/Encina.Caching.Redis/ (package and options layout)
```

</details>

---

### Phase 2: OpenFeature Adapter, Error Mapping and Registration

> **Goal**: `OpenFeatureFeatureFlagProvider` over the SDK client (Design Choices 4, 5, 7, 8, 9), with its diagnostics.

<details>
<summary><strong>Tasks</strong></summary>

1. `OpenFeatureFeatureFlagProvider.cs`: `IsEnabledAsync` (boolean details), `GetVariationAsync<T>` (Design Choice 8), context mapping (Design Choices 5 and 9); SDK error types: flag not found to `not_found` (or the default per #604 Design Choice 12), everything else to `provider_failed`; the error type name is the only thing recorded. `GetAllFlagsAsync` and `GetFlagAsync` return `Left` `not_supported`.
2. `Encina.FeatureFlags/FeatureFlagErrors.cs`: the code `encina.featureflags.not_supported` with category `InvalidRequest` (a small edit of the #604 file, with its `PublicAPI.Unshipped.txt` line and its unit test in the existing errors tests).
3. `ServiceCollectionExtensions.AddEncinaFeatureFlagsOpenFeature` (`TryAdd*`; never overrides the application's own `IFeatureFlagProvider`).
4. `src/Encina/Diagnostics/EventIdRanges.cs`: `FeatureFlagsOpenFeature = (7330, 7334)`, the `AssemblyRanges` entry and the `Encina` `PublicAPI.Unshipped.txt` line; `Diagnostics/OpenFeatureLogMessages.cs` with `[LoggerMessage]` EventIds 7330 evaluation error mapped (error type name), 7331 unsupported operation called, 7332 reserved attribute dropped, 7333 SDK provider initialization failed, 7334 SDK provider shutdown failed.
5. Telemetry: the adapter records on the `Encina.FeatureFlags` meter of #604 (`encina.featureflags.evaluations`, tag `provider` = `openfeature`) and wraps each evaluation in the span `encina.featureflags.evaluate` (tags `feature.name`, `provider`).
6. Tests: provider unit tests over the SDK's in-memory provider, a registration test (`ValidateOnBuild`, `ValidateScopes`), guard tests, the provider contract suite of #604 run against this provider; manifest entries.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
- Issue #451, Phase 2. Phase 1 is merged. #604 owns FeatureFlagErrors; this is a small, listed edit.
TASK:
- Implement the adapter, the error mapping, not_supported, the registration, the EventId range, the log messages, the telemetry and their tests.
KEY RULES:
- Never log or tag EncinaError.Message or the SDK message; use the error type name.
- Register FeatureFlagsOpenFeature in EventIdRanges.cs first; EventIds only inside 7330-7334, sequential.
- Registration never overrides the application's own IFeatureFlagProvider.
- No tenant id or user id in tags or logs; only reserved attribute names are logged.
- At most 15 files and 1500 lines.
REFERENCE FILES:
- src/Encina.FeatureFlags/FeatureFlagErrors.cs
- src/Encina.Compliance.Consent/ServiceCollectionExtensions.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

### Phase 3: flagd Fixture, Integration and Property Tests

> **Goal**: Prove the adapter against a real flagd and over the SDK's in-memory provider (Design Choice 6).

<details>
<summary><strong>Tasks</strong></summary>

1. `Directory.Packages.props`: pin the flagd provider package used by the tests; `tests/Encina.TestInfrastructure/Fixtures/FlagdFixture.cs` (the image tag is one constant in the class) and its `[Collection("FeatureFlags-Flagd")]` definition; a flag file with a boolean flag, a targeting rule and a string flag.
2. Integration test `OpenFeatureProviderIntegrationTests` (`ClearAllDataAsync`-style reset of the flag file), property test of percentage rules over the SDK's in-memory provider (deterministic per targeting key), the property and integration project references, and the integration and property per-file targets in the manifest.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
- Issue #451, Phase 3. Phases 1-2 are merged. Design Choice 6 is pending the maintainer: under option B only the property test and a justification file for integration are written.
TASK:
- Add the flagd fixture, the integration test, the property test and their manifest targets.
KEY RULES:
- The fixture follows the existing Testcontainers fixtures; the image tag is defined once.
- Shared [Collection] fixture, never IClassFixture for containers; never dispose the fixture from a test.
REFERENCE FILES:
- tests/Encina.TestInfrastructure/Fixtures/RedisFixture.cs
- docs/testing/integration-tests.md
```

</details>

---

### Phase 4a: ConfigCat Package Skeleton and Options (only if Design Choice 2 is decided for inclusion)

> **Goal**: The `Encina.FeatureFlags.ConfigCat` project and its options.

<details>
<summary><strong>Tasks</strong></summary>

1. `Directory.Packages.props`: pin `ConfigCat.Client`; project `src/Encina.FeatureFlags.ConfigCat/` with `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt` and the entry in `Encina.slnx`.
2. `ConfigCatFeatureFlagsOptions.cs` (SDK key `[JsonIgnore]`, `ToString()` override, poll interval from options, `TimeProvider` not needed by the SDK; validated on start).
3. Tests: project references in the unit, guard, contract and integration test projects; options tests (including the redacted `ToString()`); a guard test; `.github/coverage-manifest/Encina.FeatureFlags.ConfigCat.json`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4a</strong></summary>

```text
CONTEXT:
- Issue #451, Phase 4a; build only after the maintainer decides Design Choice 2 for inclusion.
TASK:
- Create the ConfigCat project, its options, the pinned package, the manifest and the test project references.
KEY RULES:
- The SDK key is a secret: [JsonIgnore] and a ToString() override.
- At most 15 files and 1500 lines.
REFERENCE FILES:
- src/Encina.FeatureFlags.OpenFeature/ (sibling package)
- src/Encina.Caching.Redis/ (options layout)
```

</details>

---

### Phase 4b: ConfigCat Provider (only if Design Choice 2 is decided for inclusion)

> **Goal**: `ConfigCatFeatureFlagProvider` over the ConfigCat client.

<details>
<summary><strong>Tasks</strong></summary>

1. `ConfigCatFeatureFlagProvider.cs` (user object from `FeatureFlagContext`: identifier, custom attributes, tenant under the same reserved-name rule as Design Choice 9; errors become `Left`, no default reported as success), `ServiceCollectionExtensions.AddEncinaFeatureFlagsConfigCat` (`TryAdd*`).
2. `EventIdRanges.cs`: `FeatureFlagsConfigCat = (7335, 7339)`, the `AssemblyRanges` entry, the `Encina` PublicAPI line; `Diagnostics/ConfigCatLogMessages.cs` (7335-7339, packed from 7335).
3. Tests with the SDK's offline local-dictionary mode (the real service has no emulator): provider tests, a registration test (`ValidateOnBuild`, `ValidateScopes`), guard tests, the provider contract suite; manifest entries.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4b</strong></summary>

```text
CONTEXT:
- Issue #451, Phase 4b; Phase 4a is merged.
TASK:
- Create the ConfigCat provider, registration, log messages and offline-mode tests.
KEY RULES:
- Errors become Left; no default value reported as success.
- Register FeatureFlagsConfigCat in EventIdRanges.cs first; EventIds only inside 7335-7339.
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

1. `Encina.FeatureFlags/FeatureFlagProviderExtensions.cs`: `IsEnabledAsync(this IFeatureFlagProvider, string name, IRequestContext context, CancellationToken)` using `FeatureFlagContext.From(context)` (#300), and an overload that reads `IRequestContextAccessor`; a missing context returns `Left` `context_missing` (Design Choice 10).
2. `FeatureFlagErrors.cs`: the code `encina.featureflags.context_missing` with category `FailedPrecondition` (listed edit of the #604 file) and its `PublicAPI.Unshipped.txt` line.
3. Unit and guard tests, the manifest entry; usage example in the package README is written in Phase 6.
4. If the maintainer chooses Design Choice 1 option B: this phase is replaced by a separate issue for a parameter-binding mechanism and the injection behavior.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
- Issue #451, Phase 5. #300 Phase 1 (FeatureFlagContext.From) is merged.
TASK:
- Add the extension methods, the context_missing error and their tests.
KEY RULES:
- A missing request context returns Left context_missing, never "enabled" (fail closed, AGENTS.md section 3).
- Use the category overload of EncinaErrors.Create.
REFERENCE FILES:
- src/Encina/Abstractions/IRequestContextAccessor.cs
- src/Encina.FeatureFlags/Model/FeatureFlagContextExtensions.cs
```

</details>

---

### Phase 6: Documentation, Justifications and Finalization

> **Goal**: Document the providers and justify the benchmark and load test types.

<details>
<summary><strong>Tasks</strong></summary>

1. Package README for each new package; section in the feature-flag guide (written by #345) on choosing a provider and the handler helper example.
2. `changelog.d/451-feature-flags-openfeature.added.md`, `docs/INVENTORY.md`.
3. Justification files: `tests/Encina.LoadTests/FeatureFlags/OpenFeature.md` (stateless reads, concurrency handled by the SDK) and `tests/Encina.BenchmarkTests/FeatureFlags/OpenFeature.md` (the issue's Test Matrix marks the benchmark Recommended; the measurable hot path is the cached evaluation of the gate, benchmarked by #300 and #345, and the adapter adds one SDK call that is not Encina code).
4. The ADR of the family is written by #345 (ADR number assigned when the ADR pull request opens).
5. `dotnet build --configuration Release` with 0 warnings; tests pass; per-flag coverage of every new file at its target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
- Issue #451, Phase 6; documentation goes through docs-writer, the fragment through mechanical-fixer.
TASK:
- Write READMEs, the guide section, the fragment, the INVENTORY update and the two justification files.
KEY RULES:
- Never edit CHANGELOG.md; state that SaaS providers are #2694, outside 1.0.
- This pull request carries Fixes #451.
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
| OpenFeature specification (CNCF) | Evaluation context, targeting key, evaluation details with error codes | Adapter semantics (Design Choices 4, 5 and 9) |
| SPEC-000 section 6, DEC-009 | Cloud services without a free local emulator are outside 1.0; the cloud half of #451 is #2694 | Design Choices 2 and 6 |
| AGENTS.md section 3 | Provider coherence; errors never swallowed; secrets in options; gates fail closed | Design Choices 3, 4, 9 and 10 |
| ADR 062 | Error category contract (merged in PR #3599) | New error codes carry a category |
| ADR-018 | 12 cross-cutting functions | Matrix below |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `IFeatureFlagProvider`, `FeatureFlagContext`, `FeatureFlagErrors` | `Encina.FeatureFlags` (#604) | Contract the adapters implement |
| `CachedFeatureFlagProvider`, `FeatureFlagsDiagnostics` | `Encina.FeatureFlags` (#604) | The only cache; meter and span |
| `IRequestContext`, `IRequestContextAccessor` | `src/Encina/Abstractions/` | Handler-side helper |
| `EventIdRanges` | `src/Encina/Diagnostics/EventIdRanges.cs` | This plan registers 7330-7334 and 7335-7339 |
| Provider fixtures | `tests/Encina.TestInfrastructure/Fixtures/` | Pattern for `FlagdFixture` |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| **`Encina.FeatureFlags.OpenFeature`** | **7330-7334** | New, registered by this plan in Phase 2 (block 7330-7339 of the registry) |
| **`Encina.FeatureFlags.ConfigCat`** | **7335-7339** | Registered in Phase 4b, only if Design Choice 2 is decided for inclusion |
| `Encina.FeatureFlags` | 7300-7319 | Owned by #604, #300 and #345; this plan uses none of it |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| OpenFeature package (Phases 1-3) | 37 | Skeleton, adapter, diagnostics, fixture, tests |
| ConfigCat package (Phases 4a-4b) | 25 | Conditional on Design Choice 2 |
| Helper (Phase 5) | 6 | Extension, error code, tests |
| Documentation (Phase 6) | 7 | 2 READMEs, guide edit, fragment, INVENTORY, 2 justification files (6 files without ConfigCat) |
| **Total** | **75** | Seven pull requests (49 files in five without ConfigCat) |

#### Pull Request Split

Files include tests, manifests, `PublicAPI` files, `Directory.Packages.props`, the solution entry, test project references and the changelog fragment; lines are estimates re-measured when each pull request opens. Every pull request is at most 15 files and 1500 lines; no pull request uses a type or log class that a later one creates.

| Phase | Content | Files | Lines |
|-------|---------|------:|------:|
| 1 | Package pin, project, PublicAPI (2), solution entry, options, manifest, 4 test project references, options and guard tests | 13 | 500 |
| 2 | Adapter, `FeatureFlagErrors` edit, registration, EventId range and allocation test, log messages, PublicAPI (3), manifest, provider, DI, guard and contract tests | 14 | 900 |
| 3 | Package pin, flagd fixture and collection, flag file, `TestInfrastructure` package reference, 2 test project references, integration and property tests, manifest | 10 | 650 |
| 4a | ConfigCat pin, project, PublicAPI (2), solution entry, options, manifest, 4 test project references, options and guard tests | 13 | 500 |
| 4b | Provider, registration, EventId range and allocation test, log messages, PublicAPI (2), manifest, provider, DI, guard and contract tests | 12 | 800 |
| 5 | Extension class, `FeatureFlagErrors` edit, PublicAPI, manifest, unit and guard tests | 6 | 350 |
| 6 | 2 READMEs, guide edit, fragment, INVENTORY, 2 justification files | 7 | 350 |
| **Total** | 7 pull requests (5 without 4a and 4b: 49 files, 2,750 lines) | **75** | **4,050** |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #451: OpenFeature (and, if decided, ConfigCat) providers for Encina feature flags.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library with Railway Oriented Programming; pre-1.0, no compatibility code. ADR 062 (error categories) is merged.
- #604 owns the package, IFeatureFlagProvider and the core EventId range; #300 and #345 own the gate. This issue only adds providers and one helper, and registers its own ranges 7330-7339.
- Provider category: none.

IMPLEMENTATION OVERVIEW:
Phase 1: OpenFeature package and options. Phase 2: adapter, error mapping, not_supported, registration, diagnostics 7330-7334.
Phase 3: flagd fixture, integration and property tests. Phases 4a-4b: ConfigCat (conditional).
Phase 5: handler-side helper. Phase 6: documentation.

KEY PATTERNS:
- Provider by registration (AddEncinaFeatureFlagsOpenFeature), TryAdd*.
- SDK errors become Left; never a default reported as success; every new error code has an ErrorCategory.
- Targeting key from the user; tenant from the explicit TenantId only; secrets with [JsonIgnore] and ToString().
- Each phase is one pull request of at most 15 files and 1500 lines with its own tests and manifest entries.

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
| 1 | Caching | ✅ Phase 2 | Uses the cache decorator of #604 (`CacheDuration`); no cache of its own |
| 2 | OpenTelemetry | ✅ Phase 2 | Family counter and span `encina.featureflags.evaluate` with `provider` = `openfeature` |
| 3 | Structured Logging | ✅ Phases 2, 4b | EventIds 7330-7334 (and 7335-7339 for ConfigCat) |
| 4 | Health Checks | ⏭️ Deferred | Flag service connectivity: follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/feature-flag-provider-health-checks.md); the issue's table says Integrate, the plan defers it so one health-check issue covers every provider of the family |
| 5 | Validation | ❌ N/A | Flag keys come from code and configuration; options validated on start |
| 6 | Resilience | ✅ Phase 2 | SDK failures become `provider_failed`; the gate's `DefaultWhenUnavailable` (#345) decides the fallback; retries and reconnects belong to the SDK provider, so no Polly pipeline here (the Polly layer of #604 is for Redis) |
| 7 | Distributed Locks | ❌ N/A | Stateless reads |
| 8 | Transactions | ❌ N/A | Reads only |
| 9 | Idempotency | ❌ N/A | Evaluation is a pure read |
| 10 | Multi-Tenancy | ✅ Phase 2 | Tenant passed as an evaluation attribute from the explicit `TenantId` only, and part of the cache key |
| 11 | Module Isolation | ❌ N/A | Flags are cross-cutting and shared across modules (same answer as #604, #345 and #300) |
| 12 | Audit Trail | ⏭️ Deferred | Flag change audit: follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/feature-flag-change-audit.md); the issue's table says Integrate, the plan defers it because flag state lives in the external service and the audit decorator of that follow-up covers the stores |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| `Encina.FeatureFlags`, `IFeatureFlagProvider`, errors, core EventId range | #604 Phases 1-3, open | Must merge first |
| `FeatureFlagContext.From(IRequestContext)` | #300 Phase 1, open | Used by the helper (Phase 5) |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Umbrella and family ADR | #345 | |
| SaaS providers (Azure App Configuration, LaunchDarkly, Split.io) | #2694 | Outside 1.0 |

---

## Next Steps

1. Review this plan; the Dilemmas (1, 2, 6, 7 and 8) are asked when work on the issue starts.
2. Post the plan link on #451.
3. Start after #604 Phases 1-3 merge; Phase 5 also needs #300 Phase 1.
4. Each phase is one pull request of at most 15 files and 1500 lines; the last carries `Fixes #451`.
