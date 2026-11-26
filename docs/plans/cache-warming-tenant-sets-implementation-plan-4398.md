# Implementation Plan: Cache warming tenant-scoped warming sets

> **Issue**: [#4398](https://github.com/dlrivada/Encina/issues/4398)
> **Type**: Feature
> **Complexity**: Medium (2 code phases, 2 pull requests of 10 and 12 files)
> **Estimated Scope**: about 400 lines of production code, about 900 lines of tests
> **Milestone**: Encina 1.0 (caching category, SPEC-000 REQ-027)

---

## Summary

Issue #4398 lets cache warming (#269) warm tenant-varying entries. #269 skips such a request under multi-tenancy because a request without a tenant bypasses the cache; a multi-tenant application can warm only `VaryByTenant = false` requests or write a custom warmer. Re-checked against `origin/main` (37b2edd32); #269 is open and its plan is merged, so the engine, `RequestCacheWarmer` and `IWarmingRequestSource<TRequest>` do not exist yet and this plan builds on the types that plan defines.

| Premise of the issue | State in the code |
|----------------------|-------------------|
| A tenant-less warm-up must never write an entry | True and already enforced: with `MultiTenancyMarker` registered, `QueryCachingPipelineBehavior.TenantAllowsCache` (`src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs:143-152`) bypasses the cache (no read, no write) for a tenant-varying request with no tenant (EventId 3513). The plan keeps that rule and adds tenants, it never relaxes it. |
| `ITenantStore` can list the tenants to warm (issue D2) | `ITenantStore.GetAllTenantsAsync` exists (`src/Encina.Tenancy/Abstractions/ITenantStore.cs`), but `Encina.Caching` references only `Encina`, `Encina.DistributedLock` and `Encina.Messaging` (`Encina.Caching.csproj`), and `Encina.Tenancy` does not reference `Encina.Caching`: using the store from the engine needs a new project reference or a satellite adapter (Design Choice 1). |
| The warmer runs "with a context that carries that tenant" | `RequestContext.CreateAnonymousAt(now, correlationId, tenantId)` (`src/Encina/Core/RequestContext.cs:136`) builds an anonymous context with a tenant; the key then contains `t:{escaped tenant}` (`DefaultCacheKeyGenerator.cs`), the segment a real request of that tenant reads. |
| Entries are isolated per tenant | The key segment is escaped with `CacheKeyRedactor.EscapeSegment` (`src/Encina.Caching/KeyGeneration/CacheKeyRedactor.cs:64`: `%` becomes `%25`, `:` becomes `%3A`), so two different tenant ids can never produce the same segment: the key is injective in the tenant. |
| "Per-(warmer, tenant) timeout" | The #269 engine applies `WarmingTimeout` and the failure isolation to a whole warmer; per-pair limits need the engine to know the pairs (Design Choice 2). |
| `.WithTenants<ActiveTenantsSource>()` after `AddEncinaRequestCacheWarmer<...>()` | The #269 plan leaves the return type of `AddEncinaRequestCacheWarmer` unspecified (a service collection); a fluent `WithTenants` needs a builder type (Design Choice 3). |

The plan adds `ITenantWarmingSource<TRequest>` (the tenants of one warmed request), `ITenantCacheWarmer` (a warmer that declares tenants, so the engine runs (warmer, tenant) pairs), the option `WarmingMaxTenants` and the registration `AddEncinaWarmingTenantSource<TRequest, TSource>()`. Dispatch stays `IEncina.Send`, so the 8 cache providers (Memory, Hybrid, Redis, Valkey, Dragonfly, Garnet, KeyDB and the planned Memcached of #277) behave identically and no provider changes.

**Family cache-warming-followups (#4396, #4397, #4398) - ownership and order.** #4396 (health check and lock) is implemented first and owns `ICacheWarmingStatus`, `WarmingLockResource`, the option `WarmingUseDistributedLock` and EventIds 488-489. This plan is second and owns `ITenantWarmingSource<TRequest>`, `ITenantCacheWarmer`, `WarmingMaxTenants` and EventIds 490-493; it extends `WarmingLockResource` with a tenant parameter (PR 2) when #4396 PR 3 has merged. #4397 (strategies) is last and independent of the tenant dimension. All three edit `CacheWarmingService.cs`, `CachingOptions.cs`, `ServiceCollectionExtensions.cs` and `CachePatternLog.Warming.cs`; the later plan rebases on the earlier one.

**Affected packages:** `Encina.Caching`. **Provider category:** Caching (8), behavior-level; multi-tenancy is ◐ for caching in the AGENTS.md section 5 matrix.

---

## Design Choices

<details>
<summary><strong>1. Where the tenants of a warmer come from (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An explicit `ITenantWarmingSource<TRequest>` per warmed request: the application lists the tenants (`IAsyncEnumerable<string> GetTenantsAsync(CancellationToken)`)** | Bounded and predictable; no new project reference; an application that wants "all active tenants" writes a source over `ITenantStore.GetAllTenantsAsync` in a few lines | The application writes one class |
| **B) All tenants from `ITenantStore`** | No application code | Startup reads every tenant's data and cost grows with the tenant count; needs `Encina.Caching` to reference `Encina.Tenancy` (or a new satellite package) |
| **C) Warm a tenant on its first request** | No startup cost | That is lazy loading, not warming |

### Chosen Option: **A - explicit source** (recommended, pending the maintainer)

### Rationale

- The source decides which tenants are hot, which is application knowledge (a store lists every tenant, including idle ones). Option B wins for a small, bounded tenant list where a full warm-up is a requirement and the maintainer accepts the project reference. Option C is not warming; it is what already happens today.
- Class: Dilemma

</details>

<details>
<summary><strong>2. Where (warmer, tenant) pairs are expanded (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Inside `RequestCacheWarmer`: it loops over the tenants itself; the engine of #269 stays tenant-agnostic** | Smallest public surface (one interface, the source) | One `WarmingTimeout` and one outcome for the whole warmer: with 100 tenants the same 5 minutes cover all of them, one tenant's failure is a single failed warmer, and custom `ICacheWarmer` implementations cannot warm tenants the same way |
| **B) In the engine: a public `ITenantCacheWarmer : ICacheWarmer` declares `bool WarmsTenants`, `IAsyncEnumerable<string> GetTenantsAsync(CancellationToken)` and `WarmAsync(string tenantId, CancellationToken)`; the engine runs each pair in its own scope, under its own `WarmingTimeout` token, isolated from the other pairs, counted in the report, bounded by `WarmingConcurrency` in `Parallel`; `RequestCacheWarmer` implements it, and a custom warmer may implement it too** | Per-pair timeout and isolation as the issue's Resilience row asks; the health check (#4396) and the report count pairs; a custom warmer can warm tenants | One more public interface; the engine of #269 grows |

### Chosen Option: **B - pairs in the engine** (recommended, pending the maintainer)

### Rationale

- The issue lists "Per-(warmer, tenant) timeout" under Resilience; only the engine can apply it. Option A wins when the maintainer wants the engine untouched and accepts a shared timeout.
- A tenant warmer's inherited tenant-less `WarmAsync(CancellationToken)` is never called by the engine; `RequestCacheWarmer` implements it as "skipped" (a tenant-less run never writes an entry).
- Class: Dilemma

</details>

<details>
<summary><strong>3. Registration shape (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A separate method `AddEncinaWarmingTenantSource<TRequest, TSource>(this IServiceCollection)` that registers the source (scoped) and validates the request** | No change to the return type of `AddEncinaRequestCacheWarmer` (#269); order of the two calls is free | Differs from the issue's fluent sample |
| **B) `AddEncinaRequestCacheWarmer<...>()` returns a builder with `.WithTenants<TSource>()` as in the issue** | The issue's syntax; one statement per warmed request | Changes the #269 method's return type (a builder instead of the service collection) before it ships |

### Chosen Option: **A - separate method** (recommended, pending the maintainer)

### Rationale

- The method throws `InvalidOperationException` when `TRequest` has no `[CacheWarmer]` or when its `[Cache]` has `VaryByTenant = false` (there is nothing tenant-specific to warm). Option B wins when the issue's sample syntax is a requirement and #269 PR 3 is still unmerged.
- Class: Dilemma

</details>

<details>
<summary><strong>4. A source that exceeds the bound (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `WarmingMaxTenants` (default 100, at least 1) caps the pairs of one warmer: the first N tenants in source order run, the rest are dropped, the engine logs EventId 491 with the bound and counts one skipped** | Startup is bounded whatever the source returns; the application orders the source by importance | A subset is warmed silently apart from the log and the count |
| **B) Exceeding the bound fails the warmer before any tenant is dispatched (counted failed, EventId 491)** | A mis-registered source (all tenants of a large fleet) is visible at once and warms nothing by accident | Nothing is warmed for that request until the source or the bound is fixed |

### Chosen Option: **A - cap and report** (recommended, pending the maintainer)

### Rationale

- The engine reads at most `WarmingMaxTenants + 1` ids from the source so an unbounded enumerable is never materialized. Option B wins when a silent subset would mislead operators more than a visible failure.
- Class: Dilemma

</details>

<details>
<summary><strong>5. A tenant-less run never writes an entry</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep the #269 rule: under `MultiTenancyMarker`, a tenant-varying request is warmed only through a tenant pair; a tenant-less dispatch is skipped before dispatch (EventId 476) and `QueryCachingPipelineBehavior` still bypasses the cache for it** | Fails closed, no entry is ever stored under a tenant-less key | Without a source the request is not warmed |
| **B) Warm tenant-varying requests without a tenant when no source exists** | Something is warmed | Dispatching runs the handler and the behavior discards the result (`TenantAllowsCache`), or, if the marker is absent, stores an entry every tenant would share |

### Chosen Option: **A - fail closed**

### Rationale

- **Class:** Settled
- **Settled by:** AGENTS.md section 3 ("Compliance and security gates fail closed: when context is missing ... the gate denies") and `QueryCachingPipelineBehavior.cs:143-152`, which already bypasses the cache for exactly this case; only option A complies. A tenant source implies tenant-varying requests only: the registration rejects `VaryByTenant = false` (Design Choice 3), and a tenant-independent request keeps its single tenant-less dispatch. This holds under every option of Design Choices 1 to 4.

</details>

<details>
<summary><strong>6. Isolation of entries per tenant</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reuse the existing key generator: each pair dispatches with a context whose `TenantId` is the pair's tenant, so the key is `{prefix}:t:{EscapeSegment(tenant)}:...` exactly as for a real request** | No new key code; the entry is the one a real request of that tenant reads; injective in the tenant | None |
| **B) A warming-specific key prefix** | Separates warming entries | The real request would never hit them: the issue's purpose fails |

### Chosen Option: **A - the existing generator**

### Rationale

- **Class:** Trivial
- **Settled by:** `DefaultCacheKeyGenerator.cs` (the `t:` segment) and `CacheKeyRedactor.EscapeSegment` (`CacheKeyRedactor.cs:64`, `%` and `:` escaped, so two tenant ids never share a segment); the #269 plan's finding that a hand-written key never hits (its Summary table). Tests: tenants `a:b` and `a%3Ab` produce two different keys, and a request of tenant `b` never reads the entry warmed for tenant `a`. Holds under every option of Design Choices 1 to 4.

</details>

<details>
<summary><strong>7. Tenant ids from a source: blank, duplicate, logging and telemetry</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A null, empty or whitespace id is skipped and counted (EventId 492, count only), never replaced by a placeholder tenant; duplicates (ordinal comparison) run once; no log line and no exception text carries a tenant id; the metric `encina.cache.warming` gets the tag `encina.tenant_id` only when `MultiTenancyMarker` is registered** | Fails closed with a missing tenant; no tenant data in logs; follows ADR-054 D4 | A cardinality risk on the metric, documented in the observability guide (#906) |
| **B) Map a blank id to a reserved `default` tenant** | Warms something | Two meanings for one segment; the entry would be readable by a request whose tenant is literally `default` |

### Chosen Option: **A - skip blanks, no tenant in logs**

### Rationale

- **Class:** Settled
- **Settled by:** AGENTS.md section 3 (fail closed with missing tenant context) and ADR-054 D4 (tenant tag only with multi-tenancy on, an attribute and a metric tag); the reserved `default` tenant segment is a convention of sibling plans (for example #241 and #238) that applies where a tenant-less value must be encoded; here no tenant-less value is ever created, so no `default` segment exists. Logs carry the warmer type name and counts only (the `CacheKeyRedactor.Redact` precedent masks `t:` segments in keys). Holds under every option of Design Choices 1 to 4.

</details>

---

## Implementation Phases

### Phase 1: Abstractions, option and registration (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

PR 1 holds exactly these 10 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Abstractions/ITenantWarmingSource.cs` | new (`ITenantWarmingSource<TRequest>`) |
| 2 | `src/Encina.Caching/Abstractions/ITenantCacheWarmer.cs` | new |
| 3 | `src/Encina.Caching/Configuration/CachingOptions.cs` | modify (`WarmingMaxTenants`) |
| 4 | `src/Encina.Caching/ServiceCollectionExtensions.cs` | modify (copy the option; `AddEncinaWarmingTenantSource<TRequest, TSource>`) |
| 5 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 6 | `tests/Encina.UnitTests/Caching/CachingOptionsTests.cs` | modify (default 100, copy by DI) |
| 7 | `tests/Encina.UnitTests/Caching/ServiceCollectionExtensionsTests.cs` | modify (`ValidateOnBuild` and `ValidateScopes`: the source resolves scoped; rejections) |
| 8 | `tests/Encina.GuardTests/Infrastructure/Caching/CacheWarmingTenantGuardTests.cs` | new |
| 9 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 10 | `changelog.d/4398-cache-warming-tenant-abstractions.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Create** `interface ITenantWarmingSource<TRequest> { IAsyncEnumerable<string> GetTenantsAsync(CancellationToken cancellationToken); }` (the issue names it `ITenantWarmingSource`; the generic form associates a source with one warmed request, as `IWarmingRequestSource<TRequest>` does) and `interface ITenantCacheWarmer : ICacheWarmer { bool WarmsTenants { get; } IAsyncEnumerable<string> GetTenantsAsync(CancellationToken cancellationToken); ValueTask<Either<EncinaError, CacheWarmerResult>> WarmAsync(string tenantId, CancellationToken cancellationToken); }` with XML docs that a tenant-less `WarmAsync` is never called by the engine when `WarmsTenants` is true. A source is an `IAsyncEnumerable` because it enumerates data, like the #269 request source; a source failure is an exception handled by the engine (PR 2), not a `Left`.
2. **Modify** `CachingOptions`: `WarmingMaxTenants` (int, default 100, the setter throws `ArgumentOutOfRangeException` below 1) and the copy line in `AddEncinaCaching` (the list at lines 53-72 is hand-written).
3. **Create** `AddEncinaWarmingTenantSource<TRequest, TSource>(this IServiceCollection services)` (`where TSource : class, ITenantWarmingSource<TRequest>`): null services throws `ArgumentNullException`; `TRequest` without `[CacheWarmer]` or with `[Cache(VaryByTenant = false)]` or without `[Cache]` throws `InvalidOperationException` with a constant message; it registers `TryAddScoped<ITenantWarmingSource<TRequest>, TSource>()` (a second registration for the same request is kept as the first, `TryAdd`).
4. **Tests:** the option default and setter limits (guard), the DI copy, the registration (resolves under `ValidateOnBuild` and `ValidateScopes`, two requests get two sources, null services and the three rejected request shapes throw).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #4398 adds tenant-scoped warming sets to cache warming (#269, merged: ICacheWarmer,
ICacheWarmingService, CacheWarmerAttribute, IWarmingRequestSource<TRequest>, AddEncinaRequestCacheWarmer). AddEncinaCaching copies every
CachingOptions property by hand.

TASK:
Add ITenantWarmingSource<TRequest>, ITenantCacheWarmer, the option WarmingMaxTenants with its copy line and AddEncinaWarmingTenantSource; write the
unit and guard tests; ask mechanical-fixer for PublicAPI.Unshipped.txt, the manifest and the changelog fragment.

KEY RULES:
- Follow the maintainer's answers to Design Choices 1 to 4, given in your brief.
- Registration rejects a request that cannot be tenant-specific; a second registration never replaces the first.
- XML docs with an example on every public member; zero warnings.

REFERENCE FILES:
- src/Encina.Caching/ServiceCollectionExtensions.cs
- src/Encina.Caching/Configuration/CachingOptions.cs
- src/Encina.Caching/Abstractions/IWarmingRequestSource.cs (from #269)
- tests/Encina.UnitTests/Caching/ServiceCollectionExtensionsTests.cs
```

</details>

### Phase 2: Pairs in the engine, tenant context and integration (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

PR 2 holds exactly these 12 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Warming/CacheWarmingService.cs` | modify (expand tenant warmers into pairs) |
| 2 | `src/Encina.Caching/Warming/RequestCacheWarmer.cs` | modify (implement `ITenantCacheWarmer`) |
| 3 | `src/Encina.Caching/Warming/WarmingLockResource.cs` | modify (tenant parameter; only when #4396 PR 3 has merged, otherwise this row and row 8 are dropped) |
| 4 | `src/Encina.Caching/Diagnostics/CachePatternLog.Warming.cs` | modify (ids 490-493) |
| 5 | `src/Encina.Caching/Diagnostics/CachePatternMetrics.Warming.cs` | modify (tenant tag) |
| 6 | `tests/Encina.UnitTests/Caching/CacheWarmingServiceTenantTests.cs` | new |
| 7 | `tests/Encina.UnitTests/Caching/RequestCacheWarmerTenantTests.cs` | new |
| 8 | `tests/Encina.UnitTests/Caching/WarmingLockResourceTests.cs` | modify (tenant parameter) |
| 9 | `tests/Encina.IntegrationTests/Infrastructure/Caching/CacheWarmingTenantIntegrationTests.cs` | new (`RedisCollection`) |
| 10 | `docs/features/handler-caching.md` | modify (`docs-writer`: tenant-scoped warming) |
| 11 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 12 | `changelog.d/4398-cache-warming-tenant-sets.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Engine** (Design Choice 2 option B): per priority group, for each warmer with `warmer is ITenantCacheWarmer { WarmsTenants: true }`, open a planning scope for that warmer, read at most `WarmingMaxTenants + 1` ids from `GetTenantsAsync` under the caller's token (a source exception is caught, logged as 490 with `ex.ForLogging()`, and counts the warmer failed with no pair dispatched; cancellation is not an error), drop blank ids and duplicates (EventId 492, count only, counted skipped), apply the bound (EventId 491, Design Choice 4), dispose the scope; each remaining pair then runs like any #269 warmer unit: its own scope (the warmer re-resolved by concrete type), a linked token with `WarmingTimeout` from `TimeProvider`, caller token checked first, then the timeout token, a `Left` after either token fired is never a failure, the pair's outcome counted in the report; `Parallel` bounds pairs by `WarmingConcurrency` (flattened inside the group), `Sequential` runs them one by one. EventId 493 summarizes the warmer (pairs, succeeded, failed, timed out, skipped). A warmer without tenants is unchanged.
2. **`RequestCacheWarmer<TRequest, TResponse>`** implements `ITenantCacheWarmer`: `WarmsTenants` is `IServiceProviderIsService.IsService(typeof(ITenantWarmingSource<TRequest>))` (no resolution); `GetTenantsAsync` streams the source resolved from the warmer's own scope; `WarmAsync(string tenantId, ct)` enumerates `IWarmingRequestSource<TRequest>` up to `WarmingInstances` and dispatches each request with `RequestContext.CreateAnonymousAt(timeProvider.GetUtcNow(), RequestContext.NewCorrelationId(), tenantId)`, anonymous apart from the tenant (the pair's tenant always replaces any ambient tenant, whichever option the maintainer chose for #269 Design Choice 9); the #269 pre-dispatch skips stay (`EnableQueryCaching` false, `VaryByUser`); the tenant-less `WarmAsync(ct)` returns a skipped result without dispatch.
3. **Telemetry:** the counter `encina.cache.warming` adds the tag `encina.tenant_id` only when `MultiTenancyMarker` is registered and the unit is a pair (ADR-054 D4; the tenant-less units keep no tenant tag); logs never carry a tenant id. **Lock:** when #4396 has merged, the pair's lock resource is `WarmingLockResource.Create(prefix, warmer, tenantId)`, the same length-prefixed hash with a third field, so a pair never shares a resource with another tenant or with the tenant-less form.
4. **Tests (unit, `FakeTimeProvider`):** a tenant warmer expands to one pair per listed tenant in source order; blank and duplicate ids are skipped and counted; the bound caps at N and logs 491 (and, if the maintainer chose option B, fails the warmer); a source that throws fails the warmer and dispatches nothing; one pair's `Left`, exception or timeout never stops the others; the caller's cancellation ends the run; `Parallel` never exceeds `WarmingConcurrency` across pairs; the dispatched context carries the pair's tenant and no user, even when the call happens inside an ambient other tenant; a tenant-less `WarmAsync` dispatches nothing; the tag is present only with the marker. **Integration (shared `RedisCollection`):** a real `RedisCacheProvider`, a real `IEncina`, a counting handler and a source listing tenants `a` and `b`: after `RunAsync` a real request of `a` and of `b` is served from the cache (the handler ran once per tenant, during warming), a request of unlisted tenant `c` runs the handler, and a request of `a` never reads `b`'s entry. The Redis family (Valkey, Dragonfly, Garnet, KeyDB) shares `RedisCacheProvider`; Memory is covered by the unit tests with `MemoryCacheProvider`; Hybrid by a unit test with a `HybridCacheProvider` built through DI; Memcached follows #277.
5. **Docs:** `docs-writer` adds "Tenant-scoped warming" to `docs/features/handler-caching.md`: the source, the bound, the fail-closed rule and the cardinality warning, citing tests by name.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
Phase 1 is merged: ITenantWarmingSource<TRequest>, ITenantCacheWarmer, WarmingMaxTenants and AddEncinaWarmingTenantSource. #269 is merged:
CacheWarmingService runs warmers by priority group, each in its own scope under a linked timeout token. EventIds 490-493 of CachingPatterns
(450-499, registered by #266) are reserved for this issue.

TASK:
Make the engine expand tenant warmers into (warmer, tenant) pairs, implement ITenantCacheWarmer in RequestCacheWarmer, add ids 490-493, the tenant
tag, the tests and the Redis integration test; ask mechanical-fixer for the manifest and the changelog and docs-writer for the docs section.

KEY RULES:
- A tenant-less run never writes an entry; a blank tenant id is skipped, never replaced; a source failure fails the warmer.
- Tenant ids never appear in logs; the tenant tag only with MultiTenancyMarker registered (ADR-054 D4).
- Every singleton resolves scoped services through IServiceScopeFactory; time from TimeProvider; no Thread.Sleep in tests.
- EventIds 490-493 only, packed.

REFERENCE FILES:
- src/Encina.Caching/Warming/CacheWarmingService.cs (from #269)
- src/Encina.Caching/Warming/RequestCacheWarmer.cs (from #269)
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs
- src/Encina.Caching/KeyGeneration/CacheKeyRedactor.cs
- tests/Encina.IntegrationTests/Infrastructure/Caching/CacheWarmingIntegrationTests.cs (from #269)
```

</details>

### Phase 3: Coverage targets and finalization

<details>
<summary><strong>Tasks</strong></summary>

1. **No new `EncinaError`**: a source failure, a bound breach and a blank id are counts and log lines, never created errors, so no ADR-062 `ErrorCategory` applies; under Design Choice 4 option B the breach is a counted failure with the constant log text `cache.warming.tenant_bound_exceeded`, still no `EncinaError`. **EventIds** 490-493 only: 490 tenant source failed, 491 tenant bound exceeded, 492 blank or duplicate tenant ids skipped (count), 493 tenant warming summary.
2. **Coverage targets (per flag).** Each row is entered in the manifest of the PR that adds the file and its tests (#1762). Numbers quoted from `origin/main`: `Configuration/CachingOptions.cs` guard 85 and no other flag; `ServiceCollectionExtensions.cs` has no `targets` row in `Encina.Caching.json` on `main` (#269 PR 1 creates one; the worker reads the value on `main` when the PR merges); the #269 plan sets `CacheWarmingService.cs` unit 95, `RequestCacheWarmer.cs` unit 95 and integration 40, `CachePatternLog.Warming.cs` and `CachePatternMetrics.Warming.cs` unit 90, and #4396 raises the engine integration target to 35.

| File | Unit | Guard | Contract | Integration | Justification |
|------|:----:|:-----:|:--------:|:-----------:|---------------|
| `Abstractions/ITenantWarmingSource.cs`, `ITenantCacheWarmer.cs` (PR 1) | 0 | 0 | 0 | 0 | Interfaces without executable lines |
| `Configuration/CachingOptions.cs` (PR 1) | no row | keeps 85 | 0 | 0 | One more validating setter, run by `CacheWarmingTenantGuardTests` |
| `ServiceCollectionExtensions.cs` (PR 1) | raise only: at least the value on `main` when the PR merges | the value on `main` | 0 | 0 | The new method's null-collection and three rejection branches are a handful of lines of a large file; never lowered (`target-ratchet`) |
| `Warming/CacheWarmingService.cs` (PR 2) | keeps 95 | 0 | 0 | keeps (35 after #4396, else the #269 value) | Pair expansion, bound, blanks, duplicates and every outcome are unit tested with gates and a fake clock; internal without `InternalsVisibleTo` for the guard project (#269 Phase 4) |
| `Warming/RequestCacheWarmer.cs` (PR 2) | keeps 95 | 0 | 0 | keeps 40 | The tenant path runs in the unit tests and the real fill in the Redis test |
| `Warming/WarmingLockResource.cs` (PR 2, when #4396 PR 3 is merged) | keeps 100 | 0 | 0 | 0 | The tenant parameter is a pure function branch |
| `Diagnostics/CachePatternLog.Warming.cs`, `CachePatternMetrics.Warming.cs` (PR 2) | keeps 90 | 0 | 0 | 0 | Ids 490-493 and the tag run in the engine tests |

   No contract test is written: the shipped warmer implements `ITenantCacheWarmer` with one engine and the rules are asserted by unit tests; a contract justification file is not allowed, so the manifest reason says so. The worker records measured numbers in each PR and never lowers a target.
3. Verification per PR: `dotnet build --configuration Release` with zero warnings; `dotnet test` of `Encina.UnitTests` (Caching), `Encina.GuardTests` and, for PR 2, `Encina.IntegrationTests` (Redis collection); per-flag coverage and the CRAP table of changed methods (keep the engine's pair expansion in small helpers so each method stays at CRAP 10 or below).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
The two code PRs are merged; each carried its own per-flag targets.

TASK:
Verify with coverage-report.cs that each PR's measured numbers reach the targets above; fix any gap through mechanical-fixer in a small PR; run
--check-justifications and the crap-gate table of changed methods.

KEY RULES:
- Never lower an existing target (target-ratchet); no log, metric or error code beyond the plan.

REFERENCE FILES:
- .github/coverage-manifest/Encina.Caching.json
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| AGENTS.md section 3 | Compliance and security gates fail closed with missing context; errors never swallowed in background infrastructure |
| AGENTS.md section 5 (caching, multi-tenancy ◐) | Behavior-level, identical on all 8 providers |
| ADR-054 D4 | The tenant is a metric tag only when multi-tenancy is on; cardinality risk documented in #906 |
| SPEC-002 AC-044 | The rule ADR-054 D4 applies across Encina |
| ADR-062 | No new `EncinaError`, so no category to assign |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `QueryCachingPipelineBehavior.TenantAllowsCache` | `src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs:143-152` | The fail-closed rule the plan keeps |
| `DefaultCacheKeyGenerator`, `CacheKeyRedactor.EscapeSegment` | `src/Encina.Caching/KeyGeneration/` | Tenant-qualified, injective keys |
| `RequestContext.CreateAnonymousAt`, `IRequestContext.WithTenantId` | `src/Encina/Core/RequestContext.cs:136`, `src/Encina/Abstractions/IRequestContext.cs:185` | Context of each pair |
| `MultiTenancyMarker` | `src/Encina/Abstractions/MultiTenancyMarker.cs` | Metric tag condition |
| `ITenantStore` | `src/Encina.Tenancy/Abstractions/ITenantStore.cs` | Basis of an application's own source (Design Choice 1) |
| `CacheWarmingService`, `RequestCacheWarmer`, `CachePatternLog`, `CachePatternMetrics` | `src/Encina.Caching` (from #269 and #266) | Engine and logging |
| `RedisCollection`, `RedisFixture` | `tests/Encina.IntegrationTests/Infrastructure/Caching/Fixtures/` | Two-tenant test |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Caching` (`CachingPatterns` 450-499, registered by #266 PR 3, plan batch registry `cache-patterns-1`) | 490-493 | 490 tenant source failed; 491 tenant bound exceeded; 492 blank or duplicate ids skipped; 493 tenant warming summary. Family: #266 450-455, #267 456-461, #268 462-468, #269 469-477, #271 478-487, #4396 488-489, #4397 494-495; 496-499 stay free. No new range |

### Estimated File Count

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | 2 | 8 | New: the two interfaces; modified (once per PR): options, DI, `PublicAPI.Unshipped.txt` (PR 1), engine, request warmer, lock resource, log file, metrics file (PR 2; the lock resource only after #4396 PR 3) |
| Tests | 4 | 3 | New: guard, engine-tenant, request-warmer-tenant, integration tests; modified: options, DI, lock-resource tests |
| Documentation | 0 | 1 | `docs/features/handler-caching.md` |
| Configuration | 2 | 2 | Two changelog fragments; the manifest in each PR |
| **Total** | **8** | **14** | Counted per pull request entry: 22 entries in 2 pull requests of 10 and 12 files, each at most 15 files and 1500 lines |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #4398 adds tenant-scoped warming sets to cache warming (#269). #266 owns CachePatternLog and the
CachingPatterns range 450-499; this plan uses ids 490-493. #4396 (merged first) owns WarmingLockResource and the status.

IMPLEMENTATION OVERVIEW:
1. PR 1: ITenantWarmingSource<TRequest>, ITenantCacheWarmer, WarmingMaxTenants, AddEncinaWarmingTenantSource.
2. PR 2: the engine runs (warmer, tenant) pairs under per-pair timeout and isolation; RequestCacheWarmer dispatches with the pair's tenant; tests, Redis
   integration, docs.

KEY PATTERNS:
- A tenant-less run never writes an entry; blank ids are skipped; tenant ids are never logged; the tenant tag only with multi-tenancy on.
- The key comes from the existing generator, so it is the one a real request reads and is injective in the tenant.
- Follow the maintainer's answers to Design Choices 1 to 4, given in your brief; Design Choices 5 to 7 are decided.

REFERENCE FILES:
- src/Encina.Caching/Warming/CacheWarmingService.cs (from #269)
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs
- src/Encina.Caching/KeyGeneration/CacheKeyRedactor.cs
- .github/coverage-manifest/Encina.Caching.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Include | Extends warming through the existing behavior for all 8 providers; entries are the tenant-qualified keys real requests read. |
| 2 | OpenTelemetry | ✅ Include | `encina.cache.warming` gains the tenant tag only with multi-tenancy on (ADR-054 D4); meter registration is #723. |
| 3 | Structured Logging | ✅ Include | EventIds 490-493 in `CachingPatterns`; no tenant id in any log line. |
| 4 | Health Checks | ❌ N/A | Pairs are counted in the report the health check of #4396 reads; no check is added here. |
| 5 | Validation | ✅ Include | `WarmingMaxTenants` setter, registration rejects a request that cannot be tenant-specific, blank and duplicate ids are skipped. |
| 6 | Resilience | ✅ Include | Per-pair timeout and failure isolation (Design Choice 2), bounded concurrency. |
| 7 | Distributed Locks | ⏭️ Defer | The lock is #4396 (open); the pair resource is the tenant parameter this plan adds to its helper. |
| 8 | Transactions | ❌ N/A | Warming reads data and never calls `SaveChangesAsync`. |
| 9 | Idempotency | ❌ N/A | Warming is idempotent: a second dispatch is served from the cache. |
| 10 | Multi-Tenancy | ✅ Include | The point of the issue: tenants isolated per key, fail closed without a tenant. |
| 11 | Module Isolation | ⏭️ Defer | Follows the module dimension of #746 (open). |
| 12 | Audit Trail | ❌ N/A | Infrastructure; warming returns data the cache stores for its own tenant, no compliance event. |

**Deferred items:** the lock resource per pair rides on #4396 (open); module-scoped warming follows #746 (open).

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| #269 PR 1 to PR 3 (engine, `RequestCacheWarmer`, `IWarmingRequestSource<TRequest>`, registration, `docs/features/handler-caching.md`) | Pending | PR 1 needs the abstractions and registration; PR 2 needs the engine |
| #266 PR 3 (`CachePatternLog`, range 450-499) | Pending | Needed for the ids of PR 2 |
| #4396 PR 3 (`WarmingLockResource`) | Pending, optional | Only the lock-resource row of PR 2 depends on it |
| Maintainer answers to Design Choices 1 to 4 | Pending | Asked when work starts |

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas (Design Choices 1 to 4) when work starts, records the answers under `## Maintainer Decisions`, then briefs two workers in PR order after #269 is merged (and after #4396 when its lock is wanted).
2. Implement this plan after #4396 and before or independently of #4397 (see "Family cache-warming-followups" in the Summary).
