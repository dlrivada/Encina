# Implementation Plan: Tenant-Scoped Rate Limiting

> **Issue**: [#740](https://github.com/dlrivada/Encina/issues/740)
> **Type**: Feature (in the 1.0 scope by the maintainer decision of 2026-10-23 on #2414; SPEC-002 REQ-061 and the fail-closed rule DEC-006)
> **Complexity**: Medium (5 phases, no database provider, 5 pull requests, about 36 file touches)
> **Estimated Scope**: ~450 lines of production code + ~1,700 lines of tests, manifests and documentation
> **Related plans and decisions** (cited by issue number, not by link): #2414 (decisions D1 to D3 of the rate limiter spike, 2026-10-23), ADR-055 (resilience family: the rate limiting core is planned in `Encina.Resilience`), #3448 (the open spike on the partition key), #704 (rate limiter state store), #728 (resilience telemetry), #1233 (`Encina.Http` per-client and per-tenant limiter), #1257 (shared tenant-scope primitive, tenant cache key), #346 (tenant gate and `[TenantAgnostic]`), #2676 (tenant telemetry helper)

---

## Summary

Make the mediator-level rate limiter fair across tenants: a `[RateLimit]` request type gets one bucket **per tenant** instead of one bucket for every caller, the per-tenant limits can differ by tier through an `ITenantRateLimitPolicy`, and a multi-tenant application that dispatches a rate-limited request with no tenant is denied (fail closed) unless the request type opts out explicitly.

**State of `origin/main` (`f484cdf4f`), verified by reading and grep (corrects the issue text).**

- The limiter lives in `Encina.Polly`, not in `Encina` or `Encina.AspNetCore`. `RateLimitingPipelineBehavior<TRequest, TResponse>` (`src/Encina.Polly/Behaviors/RateLimitingPipelineBehavior.cs`) reads `[RateLimit]` from the request type and keys the bucket by `typeof(TRequest).Name` alone (`var key = requestType;`, lines 57 and 58) for `AcquireAsync`, `RecordSuccess`, `RecordFailure` and `GetState`. `IRateLimiter` (`src/Encina.Polly/RateLimiting/IRateLimiter.cs`) already takes a `string key`, so the partition is a key change, not an interface change. `AdaptiveRateLimiter` keeps `ConcurrentDictionary<string, RateLimitBucket> _buckets`; a bucket takes its `RateLimitAttribute` configuration when it is created and is never removed except by `Reset(key)`, so one bucket per tenant and request type would grow without bound.
- `RateLimitAttribute` carries `MaxRequestsPerWindow` (100), `WindowSizeSeconds` (60), `ErrorThresholdPercent`, `CooldownSeconds`, `RampUpFactor`, `EnableAdaptiveThrottling` and `MinimumThroughputForThrottling`; the denial is `EncinaError.New(errorMessage)` with a message that holds the counts and no error code, although `EncinaErrorCodes.RateLimitExceeded = "encina.ratelimit.exceeded"` exists (`src/Encina/Errors/EncinaErrorCodes.cs` line 93, `ErrorCategory.RateLimited`).
- There is no ASP.NET Core rate limiter code in `Encina.AspNetCore` (grep finds none), no `ITenantRateLimitPolicy` and no tier concept; `TenantInfo` (`src/Encina.Tenancy/Abstractions/TenantInfo.cs`) has an id, name, isolation strategy, connection string and schema, no tier.
- The maintainer decided on #2414 (2026-10-23): D1 = 1a one partitioned rate limiting core over `System.Threading.RateLimiting.PartitionedRateLimiter`; D2 = 2a adaptive throttling stays an opt-in decorator that counts transient errors only; D3 = 3a "request type x tenant by default when tenancy is registered; a request without tenant is denied unless it opts out explicitly and the opt-out is logged (SPEC-002 DEC-006 fail-closed: the maintainer treats rate limiting as a security control)"; and #740 moved into 1.0. ADR-055 R1 puts that core in `Encina.Resilience`, which **does not exist in `src/` and has no plan or issue of its own** (the plan of #1233 states the same and builds a thin limiter in `Encina.Http` now behind an internal interface). #3448 (open, `needs-decision`, filed 2026-11-09 from a code reading) asks the partition question again for the current code; its option B is this issue. Design Choice 2 records it as this plan's own Dilemma.
- Telemetry: the plan of #728 adds `encina.resilience.partitions` (an observable gauge fed by the limiter's key count), `encina.resilience.rate_limiter.state_changes` and `encina.resilience.rate_limiter.state` under `Meter("Encina.Resilience")`, with the tenant as a tag only when multi-tenancy is on (ADR-054 D4). That is the "per-tenant consumption visibility" of the issue (Design Choice 6).
- `EventIdRanges.Polly = (3900, 3949)`; ids 3900-3921 are in use (grep), #728 plans no new log message there, and the plan-batch registry holds no claim on 3922 or above.

**Not in scope**: the Encina.Resilience partitioned core itself and the replacement of `IRateLimiter` (ADR-055 R1, #2414 D1; no issue exists yet, a follow-up issue file is written with this plan); the adaptive accounting bugs #2274 (business `Left` and cancellation counted as outage failures) and #2275 (simple-name collisions across namespaces: the request-type part of the key is passed in so #2275 changes one call site); the distributed state store (#704); `Encina.Http` (#1233); an ASP.NET Core edge middleware (Design Choice 6).

**Affected packages**: `Encina` (the policy abstraction), `Encina.Polly` (key, options, default policy, behavior, limiter eviction, registration).

**Provider category**: none (no database; the resilience category applies).

---

## Design Choices

<details>
<summary><strong>1. Where it is built while `Encina.Resilience` does not exist (Dilemma D1)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Now: the neutral abstraction `ITenantRateLimitPolicy` in core `Encina`, the default policy, options, key helper and the behavior and limiter changes in `Encina.Polly`** | The feature works in 1.0 before the core exists; the abstraction, the key rule and the options survive the move (only the wiring moves with `Encina.Polly`, which ADR-055 R1 removes) | The behavior and limiter edits are throwaway when `Encina.Resilience` replaces them |
| **B) Wait for the `Encina.Resilience` rate limiting core and build the tenant partition there** | One limiter, no throwaway wiring | The core has no issue or plan; #740 is a 1.0 issue and would block on unplanned work |
| **C) Build the partitioned core here** | Closes #2414 D1 | A feature of its own (instruments, `Retry-After`, `Encina.Http`, bulkhead partitions), out of this issue's size and scope |

### Chosen Option: **A — the abstraction in core, the wiring in `Encina.Polly` now** (recommended, pending the maintainer)

### Rationale

- A wins when the tenant guarantee is needed before the core exists (the issue is approved for 1.0) and the abstraction is written once; B wins if the maintainer schedules the core first; C wins only if #740 is merged into the core's work. The same stance as #1233 (a thin limiter now behind an interface the core can replace). The follow-up issue file `artifacts/issues/resilience-partitioned-rate-limit-core.md` records the missing core issue.

</details>

<details>
<summary><strong>2. The partition key and the missing tenant (Dilemma D2, the question of #3448)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Request type only (today)** | Protects a shared downstream quota by total volume; no code | A noisy tenant throttles every tenant (the issue's motivation) |
| **B) Request type x tenant when multi-tenancy is registered (`MultiTenancyMarker`), key `{TenantCacheKey.Segment(tenant)}:{requestType}`; a rate-limited request with no tenant is denied with `Left(encina.tenancy.tenant_required)` (ADR-062 category `InvalidRequest`) unless its type has `[TenantAgnostic]`, which uses the global segment (`g`) and is logged once per type** | The maintainer's recorded decision D3 = 3a of #2414; the issue's own acceptance; injective (the tenant segment is percent-encoded and the first `:` always separates, so no two tenant and type pairs share a key); fail closed | One bucket per tenant and type (memory, Design Choice 5); does not protect a shared downstream quota by total volume |
| **C) Per principal** | Finest fairness | Highest memory; principal resolution; out of the issue |
| **D) Leave per-client limiting to the ASP.NET Core rate limiter at the edge** | No Encina code | Mediator-level and non-HTTP entry points (background, gRPC bridges) are unprotected; #3448's own option |

### Chosen Option: **B — type x tenant, fail closed** (recommended, pending the maintainer)

### Rationale

- B wins in multi-tenant SaaS where fairness between tenants is the point (SPEC-002 S11's noisy neighbour); A wins when one downstream resource with a global quota is what the limiter protects (a per-type total can still be had by a second `[RateLimit]` on an outer request type or by the edge limiter); D wins for HTTP-only applications. Answering this Dilemma answers #3448 (its option B) and the answer should be recorded on both.
- The behavior reads the tenant with `ITenantScopeResolver.ResolveRead(context.TenantId)` of #1257 (the one fail-closed rule) and the segment with `TenantCacheKey.Segment(scope.TenantId)`: under the default-tenant rule of #1257 (Design Choice 2) a single-tenant application gets the `t.default` segment, under a null default the `g` segment; the behavior hard-codes neither. The behavior does not rely on the gate of #346: ADR-055 puts the limiter at the `Admission` stage, earlier than the gate's `Authorization` stage (today the behavior has no stage attribute), so in either order it denies by itself.
- When tenancy is not registered the key is the request type alone, exactly as today (pay-for-what-you-use): no behavior change for single-tenant applications.

</details>

<details>
<summary><strong>3. Per-tenant limits and tiers (Dilemma D3)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ITenantRateLimitPolicy` in core: `ValueTask<Either<EncinaError, TenantRateLimit?>> GetLimitAsync(string tenantId, string requestType, CancellationToken)`, `TenantRateLimit(Tier, MaxRequestsPerWindow, WindowSizeSeconds)`; a default `OptionsTenantRateLimitPolicy` reads `TenantRateLimitOptions` (named tiers, tenant to tier map); registered by `AddEncinaTenantRateLimiting(...)` (opt-in); `null` means the attribute's own limits** | Tiers without a new store; an application can register its own policy (a database of plans); no change to `TenantInfo` | One abstraction and one options class |
| **B) Tier values on the attribute (`[RateLimit(PremiumMax = ...)]`)** | No abstraction | Tiers are compiled into request types; a new tier is a code change |
| **C) A `Tier` on `TenantInfo`/`ITenantStore`** | One tenant record | Changes the tenancy package and its stores for a rate limiting concern; `Encina.Polly` would reference `Encina.Tenancy` |

### Chosen Option: **A — a policy abstraction with an options-based default** (recommended, pending the maintainer)

### Rationale

- A wins when quotas follow contracts that live outside code and one application needs more than the options (a database of plans); B wins for a fixed two-tier product. Limits apply when a bucket is created: a changed tier takes effect when the bucket is next created (idle eviction, Design Choice 5, or `IRateLimiter.Reset(key)`), and a unit test documents it. A policy `Left` denies the request with `encina.ratelimit.policy_unavailable` (ADR-062 category `Internal`: a server-side configuration lookup is not a request error, the rule #1257 applies to `missing_tenant_config`), logged by code; it never falls back to another tenant's limits.
- `TenantRateLimitOptions` has no secret, so it needs no `ToString` override; a validator (`IValidateOptions`, run on start) rejects non-positive limits, a tenant mapped to an unknown tier and a tenant id over 128 characters or with edge white space.

</details>

<details>
<summary><strong>4. A tenant the policy does not know (Dilemma D4)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The policy returns null and the request type's `[RateLimit]` limits apply (a default tier)** | The limiter still protects capacity; no second existence check (#338 owns "tenant exists") | An unconfigured tenant gets the default limit |
| **B) Deny with a new `encina.ratelimit.tenant_unknown` code** | A paid quota cannot be exceeded by an unconfigured tenant | A second tenant-existence rule next to #338; a new code to map |

### Chosen Option: **A — the attribute's limits are the default tier** (recommended, pending the maintainer)

### Rationale

- A wins when the limiter is a capacity guard and tenant validity is another gate's job (#338); B wins when tiers are contractual quotas and an unconfigured tenant must not consume capacity. Under A no new error code exists beyond `encina.ratelimit.policy_unavailable` (Design Choice 3).

</details>

<details>
<summary><strong>5. Bounding the number of buckets (Dilemma D5)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Lazy idle eviction in `AdaptiveRateLimiter`: every 256th `AcquireAsync` (a counter, no timer) removes buckets whose request window is empty, whose state is `Normal` and whose last use is older than one window, using the injected `TimeProvider`** | Bounded by active tenants; no background timer (a timer callback must never throw, `AGENTS.md` section 3); testable with `FakeTimeProvider` | A sweep costs O(buckets) every 256 calls under no lock beyond the dictionary's |
| **B) A hard cap on buckets with denial beyond it** | A fixed memory bound | An attacker who can vary the tenant header could deny every new legitimate tenant |
| **C) No eviction (today)** | No change | One bucket per tenant and type never freed |
| **D) Replace the dictionary with `PartitionedRateLimiter`, which evicts idle limiters itself** | The framework's own eviction | That is the #2414 core (Design Choice 1 option C) |

### Chosen Option: **A — lazy idle eviction** (recommended, pending the maintainer)

### Rationale

- A wins as an interim until D arrives with the core; B wins if tenant ids reaching the limiter are unvalidated (the tenant exists check of #338 runs later than the limiter stage, so a hostile id would create a bucket: the validator of the resolver bounds its length at 128 characters, which bounds each key, not their number). An evicted bucket forgets its window, which can only admit a request earlier than a strict limiter would after a full idle window, so it never over-denies.

</details>

<details>
<summary><strong>6. The ASP.NET Core middleware and the dashboard of the issue (Dilemma D6)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Neither is built: the visibility is the tenant tag on the instruments of #728 (ADR-054 D4, cardinality documented in #906) and a documentation sample shows an edge `PartitionedRateLimiter` partitioned by the same policy; a follow-up issue file records an `Encina.AspNetCore` helper** | ASP.NET Core ships the edge limiter (`RateLimitPartition.GetSlidingWindowLimiter`); no UI exists in Encina to host a dashboard | The issue's "per-tenant rate limit middleware" and "dashboard" bullets are met by metrics and a sample |
| **B) An `Encina.AspNetCore` middleware over the policy now** | HTTP entry points get tenant limits before the pipeline | A second limiter implementation (#2414 wants one core); HTTP-only |

### Chosen Option: **A — metrics and a documented sample** (recommended, pending the maintainer)

### Rationale

- A wins while there is one core to come; B wins if endpoints outside the Encina pipeline (health, minimal endpoints) need tenant limits in 1.0. The follow-up issue file is `artifacts/issues/aspnetcore-tenant-rate-limit-helper.md`.

</details>

---

## Implementation Phases

### Phase 1: Core Abstraction

> **Goal**: The neutral contract in `Encina` (PR 1).

<details>
<summary>Tasks</summary>

1. **Create `src/Encina/RateLimiting/TenantRateLimit.cs`**: `public sealed record TenantRateLimit` with `string Tier`, `int MaxRequestsPerWindow`, `int WindowSizeSeconds`, validated in the constructor (non-blank tier, both numbers at least 1; `ArgumentException` or `ArgumentOutOfRangeException`).
2. **Create `src/Encina/RateLimiting/ITenantRateLimitPolicy.cs`**: `ValueTask<Either<EncinaError, TenantRateLimit?>> GetLimitAsync(string tenantId, string requestType, CancellationToken cancellationToken = default)`; XML remarks: null means "use the request type's own limits" (Design Choice 4), a `Left` denies (Design Choice 3), the policy is never asked for a missing tenant.
3. `PublicAPI.Unshipped.txt` of `Encina`, the `Encina.json` manifest entries, a unit test and a guard test (null and blank tier, non-positive limits, null and blank tenant and request type arguments of the contract's documented checks).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```
CONTEXT:
- Issue #740; plan docs/plans/tenant-rate-limiting-implementation-plan-740.md. Dilemmas D1-D6 are recommended, pending the maintainer: follow the Maintainer Decisions section if it exists.
- Encina is pre-1.0: no [Obsolete], no compatibility layers. Namespace and folder follow the neighbouring core abstractions.

TASK:
Create TenantRateLimit and ITenantRateLimitPolicy in core Encina with XML documentation, PublicAPI lines, manifest entries and their tests.

KEY RULES:
- Either<EncinaError, T> for the policy result; never throw for business outcomes.
- The record validates in its constructor; guard tests cover every check.
- Zero warnings; per-file coverage targets in .github/coverage-manifest/Encina.json with one-sentence justifications.

REFERENCE FILES:
- src/Encina/Errors/EncinaErrorCodes.cs
- src/Encina/Results/EncinaErrors.cs
- src/Encina/Abstractions/MultiTenancyMarker.cs
- .github/coverage-manifest/Encina.json
```

</details>

---

### Phase 2: Default Policy, Options, Key Helper and Registration

> **Goal**: The opt-in registration and the pure pieces in `Encina.Polly` (PR 2).

<details>
<summary>Tasks</summary>

1. **Create `src/Encina.Polly/RateLimiting/RateLimitKey.cs`**: `public static string Create(string? tenantId, string requestType)` returns `{TenantCacheKey.Segment(tenantId)}:{requestType}` (the segment of #1257's PR A1: `t.{percent-encoded tenant}` or `g`); the request type is a parameter so #2275 changes one call site.
2. **Create `src/Encina.Polly/RateLimiting/TenantRateLimitOptions.cs`** (`Tiers`: name to `TenantRateLimit`-shaped limits, `Tenants`: tenant id to tier name) and `TenantRateLimitOptionsValidator.cs` (`IValidateOptions<TenantRateLimitOptions>`, rules of Design Choice 3).
3. **Create `src/Encina.Polly/RateLimiting/OptionsTenantRateLimitPolicy.cs`**: the default `ITenantRateLimitPolicy` (singleton, reads `IOptions<TenantRateLimitOptions>`; no I/O; `GetLimitAsync` returns `Right(null)` for an unmapped tenant).
4. **Modify `src/Encina.Polly/ServiceCollectionExtensions.cs`**: `AddEncinaTenantRateLimiting(this IServiceCollection services, Action<TenantRateLimitOptions>? configure = null)` registers the options with `ValidateOnStart`, the validator and `TryAddSingleton<ITenantRateLimitPolicy, OptionsTenantRateLimitPolicy>()` (a policy the application registered first wins; registration order does not matter, `AGENTS.md` section 3). `AddEncinaPolly` is unchanged: the tenant partition itself needs only `MultiTenancyMarker`.
5. `PublicAPI.Unshipped.txt` of `Encina.Polly`, the manifest `Encina.Polly.json`, unit tests (key, policy, validator), the guard test (null services, null configure is valid) and the DI test that builds the provider with `ValidateOnBuild` and `ValidateScopes` and resolves the policy.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```
CONTEXT:
- Issue #740. Phase 1 is merged: ITenantRateLimitPolicy and TenantRateLimit exist in core. #1257's TenantCacheKey is merged.

TASK:
Implement the key helper, options, validator, default policy and the opt-in registration in Encina.Polly with their tests.

KEY RULES:
- The key is injective: tenant segment from TenantCacheKey.Segment, request type as a parameter.
- Registration completeness: AddEncinaTenantRateLimiting registers the options, validator and policy; a DI test with ValidateOnBuild and ValidateScopes proves it; a policy registered by the application is not overridden.
- No tenant value in any log or error message.

REFERENCE FILES:
- src/Encina.Polly/ServiceCollectionExtensions.cs
- src/Encina.Polly/RateLimiting/IRateLimiter.cs
- src/Encina.Polly/Attributes/RateLimitAttribute.cs
```

</details>

---

### Phase 3: Pipeline Behavior and Limiter

> **Goal**: The tenant partition, the fail-closed rule, the per-tenant limits and the idle eviction (PR 3).

<details>
<summary>Tasks</summary>

1. **Modify `RateLimitingPipelineBehavior<TRequest, TResponse>`**: the constructor gains `ITenantScopeResolver? tenantScopeResolver = null, ITenantRateLimitPolicy? tenantRateLimitPolicy = null, MultiTenancyMarker? tenancy = null` after the existing required parameters (optional parameters stay last, CS1737); the registration is transient, so nothing scoped is captured by a singleton. When `tenancy` is null the key stays the request type (no behavior change). When it is set: resolve the scope (Design Choice 2: `Left` returned as is for a missing tenant; `[TenantAgnostic]` request types use `RateLimitKey.Create(null, requestType)`, logged once per type through a static cache of closed generic types), ask the policy (Design Choice 3: a `Left` becomes `encina.ratelimit.policy_unavailable`; a non-null result builds the effective limits as a copy of the attribute with `MaxRequestsPerWindow` and `WindowSizeSeconds` overridden), and use `RateLimitKey.Create(scope.TenantId, requestType)` for `AcquireAsync`, `RecordSuccess`, `RecordFailure` and `GetState`.
2. The denial uses `EncinaErrors.Create(EncinaErrorCodes.RateLimitExceeded, ErrorCategory.RateLimited, ...)` instead of `EncinaError.New(...)`, with the counts in the message and no tenant value; the adaptive accounting rules stay as they are (#2274).
3. **Modify `AdaptiveRateLimiter`**: lazy idle eviction (Design Choice 5) with a per-instance counter and the injected `TimeProvider`; `Reset(key)` keeps working.
4. EventIds inside the registered `Polly` range (3900-3949), packed from 3922 as private `[LoggerMessage]` methods of the behavior, like 3915 to 3917: 3922 tenant required, request denied (Warning, error code and request type only), 3923 tenant-agnostic request type limited globally (Information, once per type), 3924 tenant policy lookup failed (Warning, error code only). The architecture test already maps `Encina.Polly` to `Polly`.
5. Tests: the decision matrix below (one row per line), the key never equals across two tenants, a per-tenant tier is applied at bucket creation, an idle bucket is evicted after one window (`FakeTimeProvider`), a tier change applies after eviction, the single-tenant path is unchanged, the guard edits for the new constructor parameters (null resolver, policy and marker are valid: tests of the valid result), the property test (any two distinct tenant ids give distinct keys, `:` and `%` included), and a benchmark scenario in `RateLimitingBenchmarks.cs`.

   | Tenancy registered | Tenant | `[TenantAgnostic]` | Result |
   |---|---|---|---|
   | yes | absent | no | `Left(encina.tenancy.tenant_required)`, no bucket touched |
   | yes | T | no | bucket `{segment(T)}:{type}` with the policy's limits (or the attribute's when the policy returns null) |
   | yes | absent | yes | bucket `g:{type}`, logged once per type |
   | yes | T | yes | bucket for T (the attribute only lifts the requirement) |
   | no | any | any | bucket `{type}` exactly as today |

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```
CONTEXT:
- Issue #740. Phases 1 and 2 are merged. #1257's ITenantScopeResolver and #346's [TenantAgnostic] are merged.

TASK:
Change RateLimitingPipelineBehavior and AdaptiveRateLimiter per the Phase 3 tasks, with the full decision matrix as unit tests.

KEY RULES:
- Fail closed: a multi-tenant application with no tenant is denied before any bucket is touched; [TenantAgnostic] is the only logged opt-out.
- Optional constructor parameters last; the pipeline behavior stays transient; no scoped service is injected into a singleton.
- Time from TimeProvider; no timer for eviction; EncinaError.Message and tenant ids never logged; EventIds 3922-3924 only, packed.
- Update PublicAPI.Unshipped.txt (remove the old behavior constructor line, add the new one); zero warnings.

REFERENCE FILES:
- src/Encina.Polly/Behaviors/RateLimitingPipelineBehavior.cs
- src/Encina.Polly/RateLimiting/AdaptiveRateLimiter.cs
- tests/Encina.UnitTests/Polly/RateLimitingPipelineBehaviorTests.cs
- tests/Encina.GuardTests/Infrastructure/Polly/RateLimitingPipelineBehaviorGuardsTests.cs
```

</details>

---

### Phase 4: Observability and Cross-Cutting Integration

> **Goal**: The tenant dimension on the resilience instruments and the visibility of the issue (PR 4, after #728 and #2676).

<details>
<summary>Tasks</summary>

1. Tag the instruments `encina.resilience.rate_limiter.state_changes`, `encina.resilience.rate_limiter.state` and the rejection counter of #728 with the tenant through #2676's helper (`WithTenantTag`), only when multi-tenancy is on (ADR-054 D4); `encina.resilience.partitions` counts the limiter's buckets, now one per tenant and type, so its value is the number of live partitions. The cardinality risk is documented in the observability guide (#906).
2. This pull request is not opened until #728's instruments and #2676's helper are on `main`; with neither merged the tag is not emitted (no tenant tag without the helper).
3. Tests: a listener test asserts the tag is present with tenancy on and absent with tenancy off.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```
CONTEXT:
- Issue #740. #728's instruments and #2676's SetTenantTag/WithTenantTag helper are on main.

TASK:
Add the tenant tag to the rate limiter instruments through the helper and test it with a MeterListener.

KEY RULES:
- ADR-054 D4: the tag only when multi-tenancy is on; no baggage; no tenant id in a log.
- Do not create instruments of your own beyond those #728 defines.

REFERENCE FILES:
- docs/architecture/adr/054-observability-conventions-and-trace-propagation.md
- docs/plans/resilience-telemetry-implementation-plan-728.md
- src/Encina.Polly/Behaviors/RateLimitingPipelineBehavior.cs
```

</details>

---

### Phase 5: Testing, Documentation & Finalization

> **Goal**: Per-flag coverage at the manifest targets, documentation and the changelog (PR 5 holds the documentation).

<details>
<summary>Tasks</summary>

Tests travel with the pull request that adds the code they cover (see the split).

1. **Unit, Guard, Property, Benchmark**: in PRs 1 to 3 as listed. **Contract**: no provider family exists; justified. **Integration**: no database or external service; the behavior is exercised with the real `AdaptiveRateLimiter` through a real pipeline in the unit test project; justified, not a `.md` for a database feature. **Load**: concurrent acquires from two tenants against one limiter instance in PR 3 (a rate limiter is concurrent code, `AGENTS.md` section 9).
2. **Per-file targets** (in each manifest, with one-sentence justifications; measured with `dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage"` and `.github/scripts/coverage-report.cs`; the touched files have no numeric target today, `defaultTests` only):

   | File | Unit | Guard | Contract | Property | Integration | Justification |
   |---|---|---|---|---|---|---|
   | `RateLimitingPipelineBehavior.cs` | 95 | 5 | 0 | 0 | 0 | Every row of the matrix runs against a fake limiter and policy; guard reaches the constructor checks; contract and integration have nothing reachable |
   | `AdaptiveRateLimiter.cs` | 95 | 90 | 0 | 0 | 0 | Eviction and acquire paths run with `FakeTimeProvider`; the guard tests reach every `ThrowIfNull` |
   | `RateLimitKey.cs` | 100 | 0 | 0 | 70 | 0 | A pure function with no argument checks; the property proves injectivity; guard 0 because nothing is validated |
   | `OptionsTenantRateLimitPolicy.cs`, `TenantRateLimitOptionsValidator.cs`, `TenantRateLimitOptions.cs` | 95 | 85 | 0 | 0 | 0 | Pure lookups and validation; guard reaches the constructor checks |
   | `ServiceCollectionExtensions.cs` (Polly) | 55 | 70 | 0 | 0 | 30 | Current unit 55 and integration 30 targets are held; guard covers `AddEncinaTenantRateLimiting` |
   | `TenantRateLimit.cs`, `ITenantRateLimitPolicy.cs` (`Encina`) | 100 | 90 | 0 | 0 | 0 | The record's checks are all reachable; the interface has no executable code |

3. XML docs on every new public member; `PublicAPI.Unshipped.txt`.
4. Documentation (PR 5, through `docs-writer`): the resilience guide section "Rate limiting per tenant" with a Mermaid diagram of the decision matrix, the options example, the tier-change rule, the edge `PartitionedRateLimiter` sample of Design Choice 6 and the cardinality note; `changelog.d/740-tenant-rate-limiting.added.md` (through `mechanical-fixer`); an ADR only if the maintainer's answer to D2 is architectural (ADR number assigned when the ADR pull request opens).
5. `dotnet build --configuration Release` with 0 warnings; `dotnet test` with every flag at its target.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```
CONTEXT:
- Issue #740 is implemented; this phase closes tests, targets and documentation.

TASK:
Fill the manifest targets from the measured coverage, then write the guide section and the changelog fragment through the specialists.

KEY RULES:
- Targets never go down; coverage figures in documentation are cited with covref markers, never typed.
- Public text never names a private client application.

REFERENCE FILES:
- .github/coverage-manifest/Encina.Polly.json
- docs/testing/coverage-measurement-methodology.md
- changelog.d/README.md
```

</details>

---

### Proposed split into pull requests (limit 15 files and 1500 lines each)

| PR | Content | Approx. files | Approx. lines |
|----|---------|---------------|---------------|
| 1 | `Encina`: `TenantRateLimit`, `ITenantRateLimitPolicy`, PublicAPI, `Encina.json`, unit test, guard test | 6 | 260 |
| 2 | `Encina.Polly`: `RateLimitKey`, options, validator, default policy, registration, PublicAPI, manifest, 3 unit tests, guard test, DI test | 12 | 780 |
| 3 | `Encina.Polly`: behavior, `AdaptiveRateLimiter`, PublicAPI, manifest, behavior unit tests (edit and new), eviction unit test, 2 guard edits, property test, load test, benchmark edit | 12 | 1,100 |
| 4 | Tenant tag on the #728 instruments: behavior edit, listener test, manifest (needs #728 and #2676) | 3 | 200 |
| 5 | Documentation: resilience guide section, changelog fragment, inventory line | 3 | 250 |

A pull request that measures over 15 files or 1,500 lines when it is briefed is split again. PR 2 needs PR 1 and #1257's PR A1; PR 3 needs PR 2 and #346's PR A.

---

## Research

### Relevant Standards & Specifications

| Reference | What it defines | Use in this feature |
|-----------|-----------------|---------------------|
| #2414 decisions D1 to D3 (2026-10-23), ADR-055 R1 | One partitioned core; type x tenant by default; fail closed without tenant | Design Choices 1 and 2 |
| SPEC-002 REQ-061, DEC-006, scenario S11 | Tenant-aware capabilities; gates fail closed; the noisy neighbour | Acceptance |
| ADR-054 D4 | Tenant as attribute and metric tag only when multi-tenancy is on | Phase 4 |
| ADR-062 | Error category contract | `encina.ratelimit.exceeded` (`RateLimited`), `encina.tenancy.tenant_required` (`InvalidRequest`), new `encina.ratelimit.policy_unavailable` (`Internal`) |
| `System.Threading.RateLimiting` `PartitionedRateLimiter` | The framework partitioning and idle eviction | The direction of the core (D5 option D), not used here |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `RateLimitingPipelineBehavior`, `AdaptiveRateLimiter`, `IRateLimiter`, `RateLimitAttribute` | `src/Encina.Polly/` | The key change, eviction and limits override |
| `EncinaErrorCodes.RateLimitExceeded`, `ErrorCategory.RateLimited`, `EncinaErrors.Create` | `src/Encina/Errors/`, `src/Encina/Results/` | The coded denial |
| `MultiTenancyMarker`, `IRequestContext.TenantId` | `src/Encina/Abstractions/` | The "tenancy is on" signal and the tenant |
| `ITenantScopeResolver`, `TenantCacheKey` | #1257 PR A1 (core) | Fail-closed resolution and the injective segment |
| `[TenantAgnostic]` | #346 PR A (core) | The explicit opt-out |
| `encina.resilience.*` instruments, `WithTenantTag` | #728 PR-2, #2676 | The visibility of the issue |
| `RateLimitingBenchmarks`, `AdaptiveRateLimiterTests`, `RateLimitingPipelineBehaviorTests` | `tests/Encina.BenchmarkTests/Encina.Polly.Benchmarks/`, `tests/Encina.UnitTests/Polly/` | Test bases |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Polly` | `Polly` 3900-3949 | Existing range; 3900-3921 in use; **this plan 3922-3924** (private `[LoggerMessage]` methods of the behavior); #728 plans no new log message there; absent from the plan-batch registry; no new range |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Core production | 2 | `TenantRateLimit`, `ITenantRateLimitPolicy` |
| `Encina.Polly` production | 8 | Key, options, validator, default policy, registration, behavior and limiter edits, and the second touch of the behavior in PR 4 |
| Public API and manifests | 7 | `PublicAPI.Unshipped.txt` x2, `Encina.json`, `Encina.Polly.json` (touched in PRs 2, 3 and 4) |
| Tests | 16 | Unit, guard, property, DI, load, benchmark edit, listener test |
| Documentation | 3 | Guide section, fragment, inventory |
| **Total** | **about 36 across 5 pull requests** | The sum of the split rows is the authority |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no [Obsolete], no compatibility layers.
- Issue #740: tenant-scoped rate limiting (decisions D1-D3 of #2414; ADR-055). Plan: docs/plans/tenant-rate-limiting-implementation-plan-740.md; Dilemmas D1-D6 are recommended, pending the maintainer.

IMPLEMENTATION OVERVIEW (5 pull requests; every one builds):
1. ITenantRateLimitPolicy and TenantRateLimit in core Encina (Phase 1).
2. Key helper, options, validator, default policy, AddEncinaTenantRateLimiting in Encina.Polly (Phase 2).
3. The behavior (tenant key, fail closed, per-tenant limits, coded denial) and the limiter's idle eviction (Phase 3).
4. The tenant tag on the #728 instruments (Phase 4); documentation and targets (Phase 5).

KEY PATTERNS:
- Key {TenantCacheKey.Segment(tenant)}:{requestType} when MultiTenancyMarker is registered; the request type alone otherwise.
- Fail closed without tenant; [TenantAgnostic] is the only logged opt-out; a policy Left denies with encina.ratelimit.policy_unavailable (Internal).
- Limits apply when a bucket is created; eviction by counter and TimeProvider, never a timer; EventIds 3922-3924.
- Tests: Shouldly, FakeTimeProvider, per-flag coverage from the manifests, guard tests for new public API.

REFERENCE FILES:
- src/Encina.Polly/Behaviors/RateLimitingPipelineBehavior.cs
- src/Encina.Polly/RateLimiting/AdaptiveRateLimiter.cs
- src/Encina.Polly/ServiceCollectionExtensions.cs
- docs/architecture/adr/055-resilience-family.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: limiter state needs read-your-writes; the distributed state store is #704 |
| 2 | OpenTelemetry | ✅ | Phase 4: tenant tag on the #728 rate limiter instruments through #2676's helper, only when multi-tenancy is on (ADR-054 D4); `encina.resilience.partitions` shows the partition count |
| 3 | Structured Logging | ✅ | Phase 3: EventIds 3922-3924 in `Polly`; error codes and request type only, never a tenant id |
| 4 | Health Checks | ⏭️ | Deferred: the throttled-state health check is part of #728's plan (PR-4); it reads `IRateLimiter` and sees the per-tenant buckets without change |
| 5 | Validation | ✅ | Phase 2: `TenantRateLimitOptionsValidator` on start; the tenant is bounded by the resolver |
| 6 | Resilience | ✅ | This is a resilience feature; the circuit breaker stays independent (#2278 decision A) |
| 7 | Distributed Locks | ❌ | Not applicable: per-instance limits for 1.0 (#2414 scope); cross-instance state is #704 |
| 8 | Transactions | ❌ | Not applicable: no store write |
| 9 | Idempotency | ❌ | Not applicable: a denied request is not recorded; no deduplication key is touched |
| 10 | Multi-Tenancy | ✅ | This is the feature: the partition, the fail-closed rule and the tiers |
| 11 | Module Isolation | ❌ | Not applicable: the module is not a rate limit dimension in the issue; a module partition would be a separate request |
| 12 | Audit Trail | ❌ | Not applicable: rate limit denials are operational telemetry, not audited operations; the denial counter carries the tenant tag |

---

## Prerequisites & Dependencies

- #1257 PR A1 (`ITenantScopeResolver`, `TenantCacheKey`) before PR 2; #346 PR A (`[TenantAgnostic]`) before PR 3; #728's instruments and #2676's helper before PR 4.
- The answer to D2 is also the answer of #3448; record it on both issues.
- The missing `Encina.Resilience` core issue (Design Choice 1) and the ASP.NET Core helper (Design Choice 6) are follow-up issue files written with this plan.

---

## Next Steps

1. The maintainer answers D1 to D6 when work on #740 starts (D2 together with #3448); the answers go into `## Maintainer Decisions` and the markers are removed.
2. Brief pull request 1 first; the others follow in order.
