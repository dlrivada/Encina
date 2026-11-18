# Implementation Plan: Multi-tenant cache policies and quotas

> **Issue**: [#275](https://github.com/dlrivada/Encina/issues/275)
> **Type**: Feature
> **Complexity**: Medium (3 code phases, 3 pull requests of 15, 15 and 11 files)
> **Estimated Scope**: about 800 lines of production code, about 1,700 lines of tests
> **Milestone**: v0.18.0 — Integration & Platform Capabilities

---

## Summary

Issue #275 asks for per-tenant cache policies (`MaxEntries`, `MaxMemoryMb`, `DefaultDuration`, `AllowedCacheLevels`, `DisableEagerRefresh`), a dynamic `ITenantCachePolicyProvider`, quota enforcement, per-tenant rate limiting of cache operations and per-tenant usage metrics. Re-checked against `origin/main` (f484cdf4f):

| Premise of the issue | State in the code |
|----------------------|-------------------|
| "`VaryByTenant` isolates keys but there is no per-tenant resource control" | True. The tenant is a key segment (`DefaultCacheKeyGenerator`, `sm:t:{tenant}:...`) and the behavior bypasses the cache when multi-tenancy is on and the context has no tenant (`QueryCachingPipelineBehavior`, EventId 3513). Nothing counts or limits entries per tenant. |
| `ITenantCachePolicyProvider.GetPolicyAsync(string tenantId, ...)` maps a tenant to its tier policy | `TenantInfo` (`src/Encina.Tenancy/Abstractions/TenantInfo.cs`) has `TenantId`, `Name`, `Strategy`, `ConnectionString` and `SchemaName`: no tier or plan. The mapping tenant to policy is the application's. `Encina.Caching` does not reference `Encina.Tenancy`; it reads `IRequestContext.TenantId` and `MultiTenancyMarker` from core `Encina`. |
| The cache can enforce `MaxEntries` and `MaxMemoryMb` per tenant | `ICacheProvider` has no count, size or per-prefix enumeration (`ICacheProvider.cs`); `RedisCacheProvider` would need a `SCAN` per request, `MemoryCacheProvider` tracks keys per process (`_keyTracker`), `HybridCacheProvider` tracks only its own keys (#2402) and Memcached (#277) has no key enumeration. Enforcement has to be accounting done by Encina, not a provider feature. |
| `AllowedCacheLevels` (L1, L2) applies to every provider | Only `HybridCacheProvider` has two levels; the other seven providers have one. |
| `DisableEagerRefresh` is a policy member | Eager refresh is #267 (open, plan in review): nothing to disable yet. |
| Rate limiting can reuse existing code | `IRateLimiter` and `AdaptiveRateLimiter` live in `Encina.Polly` (`src/Encina.Polly/RateLimiting/`), which `Encina.Caching` does not reference. |
| Metrics "see #273 and #723" | Both open; no `Meter` exists in `Encina.Caching` today (#266 PR 4 creates `CachePatternMetrics`). |

The plan implements the part that every provider can honor with the same behavior: policy resolution (configured or dynamic), policy duration, per-tenant entry quota with node-local accounting, per-tenant rate limiting of cache operations, and tenant metrics, all inside `QueryCachingPipelineBehavior` through an internal `TenantCacheGovernor`, so the 8 cache providers (Memory, Hybrid, Redis, Valkey, Dragonfly, Garnet, KeyDB and the planned Memcached of #277) behave the same with no provider change. Memory quotas, cache levels and eager-refresh opt-out are Dilemma 3.

**Shared pieces and ownership (family cache-patterns-1 and -2):** #266 owns `CachePatternSettings`, the request-flow contract and `CachePatternMetrics` (a partial file is added here, so PR 2 starts after #266 PR 4). The EventIds are this plan's range `CachingTenantPolicies` 514-519 (block 500-549 of the plan batch registry).

**Affected packages:** `Encina.Caching` (core), `Encina` (EventId registry). **Provider category:** Caching (8), behavior-level and identical on all 8. Multi-tenancy is ◐ for caching in the AGENTS.md section 5 matrix.

---

## Design Choices

<details>
<summary><strong>1. Where quotas and limits are enforced (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) In `QueryCachingPipelineBehavior`, through an internal `TenantCacheGovernor` (optional trailing constructor parameter)** | The tenant is known once per request from `IRequestContext.TenantId` after the existing fail-closed bypass; identical on all 8 providers; no key parsing | Direct `ICacheProvider` calls (not `[Cache]` requests) are not governed |
| **B) An `ICacheProvider` decorator that resolves the tenant from `ITenantProvider` (async) or parses it from the key** | Governs direct `ICacheProvider` use too | Parsing `sm:t:{escaped tenant}:` is brittle (a custom `ICacheKeyGenerator` changes the shape); a per-call async tenant lookup; `Encina.Caching` has no `Encina.Tenancy` reference |

### Chosen Option: **A - behavior level** (recommended, pending the maintainer)

### Rationale

- The behavior already owns the tenant decision (EventId 3513) and the key; governing the same path keeps one fail-closed rule. Option B wins if the maintainer wants direct `ICacheProvider` users governed too, at the price of a key-shape contract.
- Class: Dilemma

</details>

<details>
<summary><strong>2. What the configured dictionary is keyed by and the provider contract (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `CacheTenantPolicyOptions.Policies` is keyed by tenant id with a `Default` policy; a tier concept is the application's, implemented by a custom `ITenantCachePolicyProvider`** | Matches what the framework knows (a tenant id); the issue's database example is exactly a custom provider | The issue's `["premium"]`, `["standard"]`, `["free"]` literals become tenant ids in the configured form |
| **B) Keyed by policy (tier) name, with a required `Func<string, CancellationToken, ValueTask<string?>>` that maps a tenant id to its policy name** | The issue's dictionary reads as written | One more delegate to configure; the delegate is what `ITenantCachePolicyProvider` already is |

### Chosen Option: **A - tenant-keyed configuration, custom provider for tiers** (recommended, pending the maintainer)

### Rationale

- `ITenantCachePolicyProvider.GetPolicyAsync(string tenantId, CancellationToken)` returns `ValueTask<Either<EncinaError, CacheTenantPolicy?>>` (a `Left` for a failed lookup, `Right(null)` for "no policy for this tenant"): ROP, AGENTS.md section 3. The resolver caches each tenant's answer for `PolicyCacheDuration` (default 1 minute, `TimeProvider`) in an instance-level bounded dictionary, so the provider is not called on every request. Option B wins when a team has a handful of tiers and thousands of tenants.
- Class: Dilemma

</details>

<details>
<summary><strong>3. Which policy members ship (Dilemma D3: the omitted acceptance items)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `MaxEntries`, `Duration` and a rate limit ship; `MaxMemoryMb`, `AllowedCacheLevels` and `DisableEagerRefresh` each become an issue file for when its prerequisite exists** | Each shipped member works identically on all 8 providers and is testable; the deferred three have no honest implementation today (no size reporting by providers, one provider with levels, no eager refresh) | The acceptance criterion "quota enforcement (entries, memory)" is met for entries only |
| **B) All members now: memory estimated by serializing the entry in the behavior, levels honored by Hybrid only (a policy with levels on another provider fails validation), `DisableEagerRefresh` stored and read by #267 later** | Matches the issue's type | Double serialization for every governed write; a member that is inert or provider-specific |

### Chosen Option: **A - the three members every provider can honor** (recommended, pending the maintainer)

### Rationale

- Option B wins if the maintainer wants the public record of the issue complete from the first release; the cost is an approximate memory figure and members that do nothing on seven of eight providers (AGENTS.md section 5 asks features to be consistent across the category).
- Follow-up issue files: `artifacts/issues/cache-tenant-memory-quota.md` (needs a provider-reported size, which #274's codec can supply) and `artifacts/issues/cache-tenant-levels-and-eager-refresh.md` (needs #267 and a level concept on `ICacheProvider`).
- Class: Dilemma

</details>

<details>
<summary><strong>4. What the policy duration means (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Override: the policy's `Duration` replaces `[Cache(DurationSeconds)]` for that tenant when set; a request type can opt out with `[Cache(AllowTenantPolicyDuration = false)]`** | Delivers the issue's use case (a premium tenant caches longer than a free one) | The tenant's tier can lengthen a staleness bound the handler author chose, unless the type opts out |
| **B) Cap: the effective duration is the smaller of the attribute and the policy** | A billing tier never makes data staler than the handler allows | A premium tenant never gets more than a free one for the same request type: the differentiation of the issue works only downward |

### Chosen Option: **A - override with an opt-out** (recommended, pending the maintainer)

### Rationale

- The issue says "policy-based cache duration override". Option B wins when staleness is a correctness bound for most queries (money, stock, permissions). The attribute default of 300 seconds cannot say whether a value was set explicitly, which is why the opt-out is a separate property.
- With `SlidingExpiration` the policy duration is the sliding window; the absolute maximum is unchanged.
- Class: Dilemma

</details>

<details>
<summary><strong>5. Quota state and what happens at the limit (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Node-local accounting: per tenant a bounded map of key to expiry, expired entries purged lazily on admission; at `MaxEntries` the new entry is not cached (the handler result is still returned)** | No I/O on the hot path; works identically on all providers; predictable | With N nodes a tenant can hold up to N times its limit in a shared distributed cache; entries removed by another node's invalidation stay counted until their expiry |
| **B) Node-local accounting, and at the limit evict the tenant's oldest key with `ICacheProvider.RemoveAsync` before admitting** | The cache keeps fresh entries | A remove on the write path; with a failing provider the eviction fails too |
| **C) Distributed counters in the cache provider itself** | Exact across nodes | Not atomic through `ICacheProvider` (get then set races); Memcached and Memory have no shared counters |

### Chosen Option: **A - node-local, deny admission** (recommended, pending the maintainer)

### Rationale

- The limit is documented as per node. Option C wins for a single shared Redis where the exact figure matters; it needs an `ICacheProvider` member for atomic increments, outside this plan.
- The accounting table is keyed by the tenant id verbatim; the implicit single-tenant case (no multi-tenancy) uses the reserved segment `default`, and because a missing tenant under multi-tenancy bypasses before accounting, the two modes never share a table (injective). The number of tracked tenants is bounded by `MaxTrackedTenants` (default 10,000): at the bound a new tenant is not governed by a quota and the event is logged (EventId 518), never an unbounded dictionary.
- Class: Dilemma

</details>

<details>
<summary><strong>6. Reaction to the rate limit (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Bypass: an operation over the limit skips the cache (no read, no write) and the handler runs; the request never fails because of the limit** | The cache is an optimization, so a cache limit degrades speed, not correctness | A throttled tenant sends more load to the database, which is the resource the cache protects |
| **B) Reject: the request returns `Left` with `ErrorCategory.RateLimited`** | The tenant is held to its budget | A business request fails for a cache concern; AGENTS.md section 3 reserves failing visibly for background operations that would otherwise report success |

### Chosen Option: **A - bypass** (recommended, pending the maintainer)

### Rationale

- Option B wins for a multi-tenant platform whose database must be protected from a noisy tenant. A request-level limit on handlers is the job of `RateLimitAttribute` in `Encina.Polly`, not of the cache.
- The limiter is an internal token bucket (`TenantCacheRateLimiter`: `MaxOperationsPerSecond` refill, `MaxBurstSize` capacity, `TimeProvider`, idle buckets purged lazily, bounded by `MaxTrackedTenants`); its state is per node.
- Class: Dilemma

</details>

<details>
<summary><strong>7. No dependency on `Encina.Polly` for the bucket</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A small internal token bucket in `Encina.Caching`** | No new package reference; about 60 lines | Two rate limiters exist in the repository |
| **B) Reference `Encina.Polly` and reuse `IRateLimiter`** | One implementation | `Encina.Caching` would take a Polly dependency for everyone; `IRateLimiter` is keyed by `RateLimitAttribute` configuration, not by a policy record |

### Chosen Option: **A - internal bucket**

### Rationale

- **Class:** Trivial
- **Settled by:** AGENTS.md section 1 (pay-for-what-you-use); `Encina.Caching.csproj` references `Encina`, `Encina.DistributedLock` and `Encina.Messaging` and no Polly package.

</details>

<details>
<summary><strong>8. Resolution failures fail closed, features are opt-in and telemetry follows ADR-054</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A failed or empty policy lookup (a `Left`, an exception, `Right(null)` with no default) bypasses the cache for that request and logs the code; tenant policies are off unless `CacheTenantPolicyOptions.Enabled` is set; the tenant tag is added to metrics only when multi-tenancy is on** | Missing context never grants unlimited cache; no cost when off; one telemetry convention | A lookup outage disables caching for the affected tenants until it recovers |
| **B) Fall back to unlimited caching on a failed lookup** | The cache keeps working | A tenant's missing policy becomes an unbounded quota: the gate opens when its context is missing |

### Chosen Option: **A - fail closed, opt-in, ADR-054 D4**

### Rationale

- **Class:** Settled
- **Settled by:** AGENTS.md section 3 ("Compliance and security gates fail closed: when context is missing ... or a lookup fails, the gate denies"), AGENTS.md section 1 (pay-for-what-you-use) and ADR-054 D4 (the tenant is a tag when multi-tenancy is on).
- Error codes: none are returned to callers (a lookup `Left` is logged by its code and the request continues without the cache); the provider's own `Left` codes are the application's.

</details>

---

## Implementation Phases

### Phase 1: Policy models, provider contract and resolver (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

PR 1 holds exactly these 15 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Tenancy/CacheTenantPolicy.cs` | new (public sealed record `CacheTenantPolicy` and `CacheRateLimitPolicy`, with `Validate()`) |
| 2 | `src/Encina.Caching/Tenancy/ITenantCachePolicyProvider.cs` | new (public) |
| 3 | `src/Encina.Caching/Tenancy/CacheTenantPolicyOptions.cs` | new (public: `Enabled`, `Policies`, `Default`, `PolicyCacheDuration`, `MaxTrackedTenants`, `EnableRateLimiting`) |
| 4 | `src/Encina.Caching/Tenancy/ConfiguredTenantCachePolicyProvider.cs` | new (internal, default provider) |
| 5 | `src/Encina.Caching/Tenancy/TenantCachePolicyResolver.cs` | new (internal sealed: caching, bound, `TimeProvider`) |
| 6 | `src/Encina.Caching/Configuration/CachingOptions.cs` | modify (`TenantPolicies`) |
| 7 | `src/Encina.Caching/ServiceCollectionExtensions.cs` | modify (copy the option, validate, register resolver and default provider with `TryAdd`) |
| 8 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 9 | `tests/Encina.UnitTests/Caching/CacheTenantPolicyTests.cs` | new |
| 10 | `tests/Encina.UnitTests/Caching/TenantCachePolicyResolverTests.cs` | new |
| 11 | `tests/Encina.UnitTests/Caching/CachingOptionsTests.cs` | modify |
| 12 | `tests/Encina.UnitTests/Caching/ServiceCollectionExtensionsTests.cs` | modify (DI built with `ValidateOnBuild` and `ValidateScopes`) |
| 13 | `tests/Encina.GuardTests/Caching/CacheTenantPolicyGuardTests.cs` | new |
| 14 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 15 | `changelog.d/275-cache-tenant-policy-model.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Create** `CacheTenantPolicy` (`MaxEntries` int?, `Duration` TimeSpan?, `RateLimit` `CacheRateLimitPolicy?`) with `Validate()` (positive values, `MaxBurstSize` at least 1, a finite duration; the message names the member, never a tenant id) and `CacheRateLimitPolicy` (`MaxOperationsPerSecond`, `MaxBurstSize`).
2. **Create** `ITenantCachePolicyProvider` (Design Choice 2) and the default `ConfiguredTenantCachePolicyProvider` (looks up `Policies` by tenant id, `Right(Default)` otherwise, `Right(null)` when no default).
3. **Create** `TenantCachePolicyResolver`: `ValueTask<TenantPolicyDecision> ResolveAsync(string tenantKey, CancellationToken)` where the decision is `Allow(policy)` or `Deny(reason)` with reason `LookupFailed` (a `Left` or an exception from a custom provider), `NoPolicy` (`Right(null)` with no default) or `InvalidPolicy` (`Validate()` failed): Design Choice 8; the resolver logs nothing, the governor of PR 2 logs the reason; caches per tenant for `PolicyCacheDuration`; bounded by `MaxTrackedTenants`; instance-level state only (never a static field of a generic class).
4. **Modify** `CachingOptions` (`CacheTenantPolicyOptions TenantPolicies { get; set; } = new();`, `Enabled` false) and `AddEncinaCaching`: copy the option (the list is hand-written), run each policy's `Validate()` at registration, register the resolver and the default provider with `TryAddSingleton` (a custom provider registered before or after wins: `TryAdd`).
5. **Tests (unit):** validation rules, tenant-keyed lookup, default fallback, a `Left`, an exception and `Right(null)` each deny, cache expiry with `FakeTimeProvider`, bound at `MaxTrackedTenants`, DI test with `ValidateOnBuild` and `ValidateScopes`. Guard tests: `CacheTenantPolicyOptions` setters reject null collections and `ConfiguredTenantCachePolicyProvider` is internal (no `InternalsVisibleTo` for `Encina.GuardTests` in `Encina.Caching.csproj`), so the guard file covers the public members only.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #275 adds per-tenant cache policies. Encina.Caching reads IRequestContext.TenantId and
MultiTenancyMarker from core Encina; it does not reference Encina.Tenancy. AddEncinaCaching copies every CachingOptions property by hand.

TASK:
1. Create CacheTenantPolicy (+ CacheRateLimitPolicy), ITenantCachePolicyProvider, CacheTenantPolicyOptions, the default configured provider
   and TenantCachePolicyResolver.
2. Add CachingOptions.TenantPolicies (off by default), copy and validate it in AddEncinaCaching, register with TryAdd.
3. Write unit, DI and guard tests; ask mechanical-fixer for PublicAPI.Unshipped.txt, the manifest rows and the changelog fragment.

KEY RULES:
- A failed or empty lookup denies (fail closed); never log a tenant id verbatim in an error message or EncinaError.Message.
- Provider contract returns Either<EncinaError, CacheTenantPolicy?>; time from TimeProvider; no static field of a generic class holds options.
- Follow the maintainer's answers to Design Choices 2 and 3, given in your brief.

REFERENCE FILES:
- src/Encina.Caching/Configuration/CachingOptions.cs
- src/Encina.Caching/ServiceCollectionExtensions.cs
- src/Encina/Abstractions/MultiTenancyMarker.cs
- src/Encina/Results/EncinaErrors.cs
```

</details>

### Phase 2: Duration, quota and tenant metrics (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

PR 2 holds exactly these 15 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Tenancy/TenantCacheUsageTracker.cs` | new (internal sealed, Design Choice 5) |
| 2 | `src/Encina.Caching/Tenancy/TenantCacheGovernor.cs` | new (internal sealed: resolve, duration, admission) |
| 3 | `src/Encina.Caching/Diagnostics/CacheTenantPolicyLog.cs` | new (EventIds 514-516, 518 and 519) |
| 4 | `src/Encina.Caching/Diagnostics/CachePatternMetrics.TenantPolicies.cs` | new (partial of #266's class) |
| 5 | `src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs` | modify (optional `TenantCacheGovernor?` parameter, hooks at resolution, duration and admission) |
| 6 | `src/Encina.Caching/Attributes/CacheAttribute.cs` | modify (`AllowTenantPolicyDuration`) |
| 7 | `src/Encina/Diagnostics/EventIdRanges.cs` | modify (`CachingTenantPolicies = (514, 519)`) |
| 8 | `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` | modify (map `Encina.Caching`) |
| 9 | `src/Encina/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`: the new `EventIdRanges` field) |
| 10 | `tests/Encina.UnitTests/Caching/TenantCacheUsageTrackerTests.cs` | new |
| 11 | `tests/Encina.UnitTests/Caching/TenantCacheGovernorTests.cs` | new (includes the metrics listener tests) |
| 12 | `tests/Encina.UnitTests/Caching/QueryCachingTenantPolicyTests.cs` | new |
| 13 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 14 | `changelog.d/275-cache-tenant-quota.added.md` | new (`mechanical-fixer`) |

Row 15 is `src/Encina.Caching/PublicAPI.Unshipped.txt` (`mechanical-fixer`: the `CacheAttribute` property and the changed behavior constructor line, RS0017), so PR 2 holds 15 files. The metrics listener tests live in `TenantCacheGovernorTests.cs`.

Tasks:

1. **Create** `TenantCacheUsageTracker`: per tenant key a bounded `Dictionary<string, DateTime>` (key to expiry, `TimeProvider`), `TryAdmit(tenantKey, cacheKey, expiry, maxEntries)` purges expired keys lazily, never exceeds `MaxEntries`, counts a re-write of an existing key as no growth; `Forget(tenantKey, cacheKey)` for a removal. The cache key stored is the generated key (tenant-qualified, so unique per tenant by construction); it is never logged (use `CacheKeyRedactor`).
2. **Create** `TenantCacheGovernor`: `ValueTask<TenantDecision> BeginAsync(string? tenantId, CancellationToken)` resolves the policy (deny means bypass: the behavior treats it like the EventId 3513 path and logs 515 in the same Debug style, once per request); `TimeSpan EffectiveDuration(...)` applies Design Choice 4; `bool TryAdmit(...)` applies the quota (EventId 516 with the redacted key, counter `encina.cache.tenant.rejected` with reason `quota`).
3. **Modify** `QueryCachingPipelineBehavior`: an optional trailing `TenantCacheGovernor? governor = null` (the existing call sites compile unchanged; DI supplies it only when `TenantPolicies.Enabled`); the three hook points are small private methods so `Handle` stays at CRAP 10 or below. A request with `[Cache(VaryByTenant = false)]` is a global entry and is not governed (it belongs to no tenant).
4. **Metrics (ADR-054 D4):** an observable gauge `encina.cache.tenant.entries` and the counter `encina.cache.tenant.rejected`; the tenant tag `encina.tenant_id` is added only when `MultiTenancyMarker` is registered, and the gauge reports at most `MaxTrackedTenants` series. EventIds: 514 policy resolution failed (by error code or exception type), 515 no policy, cache bypassed, 516 quota reached, 518 tracked-tenant bound reached, 519 invalid policy (the reason carries the member name only).
5. **Tests:** two tenants with different limits never see each other's counts, a re-write does not grow the count, expiry frees room with `FakeTimeProvider`, a quota hit returns the handler result and writes nothing, a policy duration reaches the provider (override and cap per the maintainer's answer), the opt-out attribute, deny on a failed lookup, `VaryByTenant = false` is ungoverned, the metrics tag is absent without multi-tenancy.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
Phase 1 is merged: policies, the provider contract and TenantCachePolicyResolver exist. #266 PR 4 created CachePatternMetrics.cs.

TASK:
1. Create TenantCacheUsageTracker and TenantCacheGovernor; hook them into QueryCachingPipelineBehavior through an optional trailing parameter.
2. Create CacheTenantPolicyLog (EventIds 514-516, 518, 519) and register CachingTenantPolicies = (514, 519) in EventIdRanges.cs and the assembly map.
3. Add the CachePatternMetrics partial; write the tests; ask mechanical-fixer for the PublicAPI files, manifest rows and changelog fragment.

KEY RULES:
- Fail closed: a missing tenant under multi-tenancy or a failed policy lookup bypasses the cache; never grant unlimited caching.
- Quota state is node-local, bounded by MaxTrackedTenants; the implicit single-tenant case uses the reserved segment default.
- Logs carry redacted keys and error codes only; the tenant tag appears only with multi-tenancy on (ADR-054 D4).
- Keep QueryCachingPipelineBehavior.Handle at CRAP 10 or below: extract the hooks into private methods.
- Follow the maintainer's answers to Design Choices 1, 4 and 5, given in your brief.

REFERENCE FILES:
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- src/Encina.Tenancy/Diagnostics/TenancyMetrics.cs
- tests/Encina.UnitTests/Caching/CacheTenantFailClosedTests.cs
```

</details>

### Phase 3: Rate limiting and the Redis integration test (pull request 3)

<details>
<summary><strong>Tasks</strong></summary>

PR 3 holds exactly these 11 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Tenancy/TenantCacheRateLimiter.cs` | new (internal sealed token bucket) |
| 2 | `src/Encina.Caching/Tenancy/TenantCacheGovernor.cs` | modify (`TryAcquireOperation`) |
| 3 | `src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs` | modify (one acquire before the read and one before the write) |
| 4 | `src/Encina.Caching/Diagnostics/CacheTenantPolicyLog.cs` | modify (EventId 517) |
| 5 | `src/Encina.Caching/Diagnostics/CachePatternMetrics.TenantPolicies.cs` | modify (reason `rate_limit`) |
| 6 | `tests/Encina.UnitTests/Caching/TenantCacheRateLimiterTests.cs` | new (`FakeTimeProvider`) |
| 7 | `tests/Encina.UnitTests/Caching/QueryCachingTenantPolicyTests.cs` | modify |
| 8 | `tests/Encina.IntegrationTests/Infrastructure/Caching/TenantCachePolicyIntegrationTests.cs` | new (shared Redis fixture, two tenants) |
| 9 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 10 | `changelog.d/275-cache-tenant-rate-limit.added.md` | new (`mechanical-fixer`) |
| 11 | `tests/Encina.UnitTests/Caching/TenantCacheGovernorTests.cs` | modify |

Tasks:

1. **Create** `TenantCacheRateLimiter`: per tenant key a token bucket (`MaxOperationsPerSecond` refill with `TimeProvider`, `MaxBurstSize` capacity); `TryAcquire(tenantKey, policy)`; idle buckets are purged lazily when the table reaches `MaxTrackedTenants` (EventId 518 is shared); no timer, no thread.
2. **Modify** the behavior: when `EnableRateLimiting` is on and the tenant's policy has a `RateLimit`, a read or a write over the limit is skipped (Design Choice 6: bypass), logged once per request with EventId 517 (request type only) and counted with reason `rate_limit`.
3. **Integration test** (`[Collection("Redis")]`-style shared fixture, `[Trait("Category", "Integration")]`): tenant A at the quota stops caching while tenant B keeps caching; a bypassed request still returns the handler's value. The test never creates its own container.
4. **Tests (unit):** burst then refill, tenants isolated, an exhausted bucket bypasses without failing the request, no limiter state without a `RateLimit`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
Phases 1-2 are merged: policies, resolver, governor, quota and metrics exist.

TASK:
Create TenantCacheRateLimiter, wire it into the governor and the behavior, add EventId 517 and the rate_limit metric reason, and write the
unit tests and the Redis integration test; ask mechanical-fixer for the manifest rows and the changelog fragment.

KEY RULES:
- An exhausted bucket bypasses the cache; it never fails the request (Design Choice 6 as answered in your brief).
- No timer or thread: refill is computed from TimeProvider on access; state is bounded by MaxTrackedTenants.
- Integration tests use the shared Redis collection fixture.

REFERENCE FILES:
- src/Encina.Caching/Tenancy/TenantCacheGovernor.cs
- tests/Encina.IntegrationTests/Infrastructure/Caching/Fixtures/RedisFixture.cs
- tests/Encina.IntegrationTests/Infrastructure/Caching/RedisCacheProviderIntegrationTests.cs
```

</details>

### Phase 4: Cross-cutting integration, observability and coverage targets

<details>
<summary><strong>Tasks</strong></summary>

1. **EventIds:** `CachingTenantPolicies` 514-519 (514 policy resolution failed, 515 no policy and cache bypassed, 516 quota reached, 517 rate limit bypass, 518 tracked-tenant bound reached, 519 policy provider returned an invalid policy); verified free by a repo-wide grep of `src/Encina.Caching` (used 3500-3513); emitted only by `Encina.Caching`.
2. **Error codes:** none are created; the lookup `Left` of a custom provider is logged by its code and never returned to the caller.
3. **Coverage targets (per flag, set in the PR that adds each file):**

| File | Unit | Guard | Contract | Integration | Justification |
|------|:----:|:-----:|:--------:|:-----------:|---------------|
| `Encina.Caching/Tenancy/CacheTenantPolicy.cs` | 100 | 80 | 0 | 0 | Validation branches are unit tested; the guard file covers the public argument checks |
| `Encina.Caching/Tenancy/CacheTenantPolicyOptions.cs` | 100 | 85 | 0 | 0 | Property setters with null guards, covered by unit and guard tests |
| `Encina.Caching/Tenancy/TenantCachePolicyResolver.cs`, `ConfiguredTenantCachePolicyProvider.cs` | 95 | 0 | 0 | 0 | Internal types: no `InternalsVisibleTo` for the guard project |
| `Encina.Caching/Tenancy/TenantCacheUsageTracker.cs`, `TenantCacheRateLimiter.cs` | 100 | 0 | 0 | 0 | Pure logic with `FakeTimeProvider`; internal |
| `Encina.Caching/Tenancy/TenantCacheGovernor.cs` | 95 | 0 | 0 | 40 | Unit tests drive every decision; the integration flag runs it against a real Redis |
| `Encina.Caching/Tenancy/ITenantCachePolicyProvider.cs` | 0 | 0 | 0 | 0 | An interface has no executable lines |
| `Encina.Caching/Diagnostics/CacheTenantPolicyLog.cs`, `CachePatternMetrics.TenantPolicies.cs` | 90 | 0 | 0 | 0 | Generated bodies and instrument calls run in the behavior tests |
| `Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs`, `ServiceCollectionExtensions.cs`, `Configuration/CachingOptions.cs`, `Attributes/CacheAttribute.cs` | keep | keep | 0 | 0 | Existing targets are never lowered; new branches are tested in the same PRs |

   The worker measures every flag per PR with `coverage-report.cs`; no target is lowered (`target-ratchet`).
4. **Property, load, benchmark:** no property test (the token bucket and the tracker are covered by a unit grid); no load test (state is node-local and lock-free per tenant; a load test of the governor is a follow-up inside `Encina.LoadTests`); no benchmark in these PRs (the governor adds one dictionary lookup per governed request).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
The three code PRs are merged. Verification remains.

TASK:
Verify each PR carried its per-flag targets and the EventId registration; fix omissions in a small PR through mechanical-fixer; run
--check-justifications.

KEY RULES:
- Never lower an existing target (target-ratchet).
- Add no log, metric or error code beyond the plan.

REFERENCE FILES:
- .github/coverage-manifest/Encina.Caching.json
- docs/testing/coverage-measurement-methodology.md
```

</details>

### Phase 5: Documentation and finalization

<details>
<summary><strong>Tasks</strong></summary>

1. XML documentation with `<example>` on every public type; the remarks state that limits are per node, that direct `ICacheProvider` use is not governed and that `[Cache(VaryByTenant = false)]` entries are global.
2. `docs-writer`: a "Multi-tenant policies" section in `docs/features/query-caching.md` with SaaS tier patterns (the issue's documentation criterion), citing tests by name and no typed figure.
3. `PublicAPI.Unshipped.txt` lines are written by `mechanical-fixer` per PR; no ROADMAP, INVENTORY or ADR change (ADR number assigned when the ADR pull request opens, if the maintainer asks for one).
4. Verification per PR: `dotnet build --configuration Release` with zero warnings; `dotnet test` of `Encina.UnitTests` (Caching), `Encina.GuardTests` and, for PR 3, `Encina.IntegrationTests` (Redis); per-flag coverage and the CRAP table of changed methods.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
All code PRs are merged. Documentation remains.

TASK:
Confirm the XML docs and the three changelog fragments; ask docs-writer for the query-caching.md section.

KEY RULES:
- No hand-typed figures; cite tests by name.
- Fragment section is added; never edit CHANGELOG.md.

REFERENCE FILES:
- docs/features/query-caching.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| AGENTS.md section 3 | Fail-closed gates; ROP on the provider contract; time from `TimeProvider` |
| AGENTS.md section 5 | Multi-tenancy is ◐ for caching; behavior-level feature identical on all 8 providers |
| ADR-054 D4 | Tenant tag on telemetry only with multi-tenancy on |
| ADR-021 | EventId range registered before use |
| Token bucket algorithm (Turner, ATM traffic shaping; RFC 2697 single rate policing) | The limiter of Design Choice 6 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `QueryCachingPipelineBehavior` and its tenant bypass | `src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs` | Host of the governor hooks |
| `DefaultCacheKeyGenerator`, `CacheKeyRedactor` | `src/Encina.Caching/KeyGeneration/` | Tenant-qualified keys; redacted logs |
| `MultiTenancyMarker` | `src/Encina/Abstractions/MultiTenancyMarker.cs` | Multi-tenancy detection and the metric tag condition |
| `TenancyMetrics` | `src/Encina.Tenancy/Diagnostics/TenancyMetrics.cs` | Precedent for tenant-tagged instruments |
| `CachePatternMetrics` (#266) | `src/Encina.Caching/Diagnostics/` | Extended by a partial file |
| `RedisFixture` | `tests/Encina.IntegrationTests/Infrastructure/Caching/Fixtures/RedisFixture.cs` | Shared container for the integration test |
| `EventIdRanges`, `EncinaEventIdAllocationTests` | `src/Encina/Diagnostics/EventIdRanges.cs` | New range and assembly map |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Caching` (new range `CachingTenantPolicies`, block 500-549 of the plan batch registry) | 514-519 | This plan: 514 policy resolution failed, 515 no policy and cache bypassed, 516 quota reached, 517 rate limit bypass, 518 tracked-tenant bound reached, 519 invalid policy returned by a provider. Family: #272 500-507, #274 508-513, #278 520-531, #276 532-536; 537-549 free. The existing `Caching` range 3500-3549 is untouched |

### Estimated File Count

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | 10 | 6 | New: policy types, provider, options, resolver, tracker, governor, limiter, log, metrics partial; modified: options, DI, behavior, attribute, `EventIdRanges.cs`, PublicAPI |
| Tests | 8 | 5 | Unit (6), guard (1), integration (1); modified: options, DI, allocation test, behavior tests |
| Documentation | 0 | 1 | `docs/features/query-caching.md` |
| Configuration | 3 | 3 | Three changelog fragments; manifest edited in each PR |
| **Total** | **21** | **15** | 3 pull requests of 15, 15 and 11 files, each at most 15 files and 1500 lines |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #275 adds per-tenant cache policies, quotas and rate limiting to
QueryCachingPipelineBehavior (src/Encina.Caching). The behavior already bypasses the cache when multi-tenancy is on and the request has
no tenant. All 8 cache providers behave the same because everything lives at the behavior level.

IMPLEMENTATION OVERVIEW:
1. PR 1: CacheTenantPolicy, ITenantCachePolicyProvider, options, resolver, DI (off by default).
2. PR 2: usage tracker, governor, policy duration, entry quota, metrics, EventIds 514-519 range.
3. PR 3: TenantCacheRateLimiter and the Redis integration test.

KEY PATTERNS:
- Fail closed: a failed or empty policy lookup bypasses the cache; the implicit single tenant uses the reserved segment default.
- State is node-local and bounded by MaxTrackedTenants; no timer or thread; time from TimeProvider.
- Telemetry tags the tenant only with multi-tenancy on (ADR-054 D4); logs carry redacted keys and codes only.
- Optional trailing constructor parameter on the behavior; no scoped service injected into a singleton.
- Follow the maintainer's answers to Design Choices 1 to 6, given in your brief.

REFERENCE FILES:
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs
- src/Encina.Caching/ServiceCollectionExtensions.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- .github/coverage-manifest/Encina.Caching.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Include | Extends `QueryCachingPipelineBehavior` for all 8 providers; no provider change. |
| 2 | OpenTelemetry | ✅ Include | `encina.cache.tenant.entries` and `.rejected` in a `CachePatternMetrics` partial; tenant tag only with multi-tenancy on (ADR-054 D4); spans belong to #723 (open). |
| 3 | Structured Logging | ✅ Include | `CacheTenantPolicyLog`, EventIds 514-519 in `CachingTenantPolicies`. |
| 4 | Health Checks | ⏭️ Defer | Per-tenant cache health belongs to the provider health work (#754, open); this feature has no backend of its own. |
| 5 | Validation | ✅ Include | `CacheTenantPolicy.Validate()` at registration and at provider answer time. |
| 6 | Resilience | ❌ N/A | Policies are configuration and an in-memory lookup; a failing custom provider denies the cache (fail closed) rather than retrying. |
| 7 | Distributed Locks | ❌ N/A | State is node-local; no shared resource is coordinated. |
| 8 | Transactions | ❌ N/A | Cache operations are independent of units of work. |
| 9 | Idempotency | ❌ N/A | Applying a policy is idempotent. |
| 10 | Multi-Tenancy | ✅ Include | The feature itself: tenant-qualified keys unchanged, fail-closed bypass, reserved `default` segment for the implicit tenant, tenant tag per ADR-054 D4. |
| 11 | Module Isolation | ⏭️ Defer | Module-scoped policies follow the module dimension of #746 (open). |
| 12 | Audit Trail | ⏭️ Defer | Auditing quota enforcement events would flood `IOperationAuditStore` (a rejection is per request); an aggregate audit of policy changes is `artifacts/issues/cache-tenant-policy-audit.md`, to be opened by the orchestrator. |

**Deferred items:** memory quota and levels/eager refresh (`artifacts/issues/cache-tenant-memory-quota.md`, `artifacts/issues/cache-tenant-levels-and-eager-refresh.md`), policy-change audit (`artifacts/issues/cache-tenant-policy-audit.md`), module-scoped policies (#746, open), per-tenant health (#754, open).

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| `QueryCachingPipelineBehavior`, `MultiTenancyMarker`, `CachingOptions` | ✅ Available | On `origin/main` |
| #266 PR 4 (`CachePatternMetrics.cs`) | Open | PR 2 of this plan needs it |
| Block 500-549 of the EventId registry | ✅ Assigned | Plan batch registry |
| Maintainer answers to Design Choices 1 to 6 | Pending | Asked when work starts |

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas (Design Choices 1 to 6) when work starts, records the answers under `## Maintainer Decisions`, then briefs three workers in PR order.
2. Open the three follow-up issue files.
