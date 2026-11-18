# Implementation Plan: Eager (background) refresh of cache entries

> **Issue**: [#267](https://github.com/dlrivada/Encina/issues/267)
> **Type**: Feature
> **Complexity**: Medium (3 code phases, 3 pull requests of 13, 12 and 10 files)
> **Estimated Scope**: about 450 lines of production code, about 1,000 lines of tests
> **Milestone**: v0.18.0 — Integration & Platform Capabilities

---

## Summary

Issue #267 asks that a `[Cache]` entry past a fraction of its TTL (`EagerRefreshThreshold`, for example 0.8) is served immediately while the value is recomputed in the background, with at most one refresh per key, configuration per handler and globally, and metrics. Re-checked against `origin/main` (f484cdf4f):

| Premise of the issue | State in the code |
|----------------------|-------------------|
| The behavior is `CachePipelineBehavior` and can call `RefreshInBackgroundAsync(request, key, ct)` with the request token | The class is `QueryCachingPipelineBehavior` (`src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs`). The delegate `RequestHandlerCallback<TResponse>` takes no token and is bound to the request's DI scope (`src/Encina/Abstractions/RequestHandlerDelegate.cs`, line 9), so a fire-and-forget call of `nextStep()` after the response ends runs on a disposed scope with a cancelled token. |
| `CacheEntry<T>` can get `ExpiresAtUtc` and `IsRefreshing` | `CacheEntry<T>` is internal with `Value` and `CachedAtUtc` only (line 353). The expiry field `ExpiresAtUtc` is added by #266 (PR 1 of that plan, which this plan depends on). |
| `DateTime.UtcNow` computes the age | Production code takes `TimeProvider` (AGENTS.md section 3); the behavior already holds one. |
| `EagerRefreshOnlyOnAccess` selects "vs timer-based" | No timer refresh exists; see Design Choice 4. |
| Tenant-scoped refresh keys are deferred to #746 | The key already carries the tenant (`DefaultCacheKeyGenerator`); the refresh must also run with that tenant (Design Choice 2). |

The plan adds `EagerRefreshThreshold` (attribute and option), a pure age decision, `IBackgroundCacheRefresher` (a bounded queue worked by a hosted service, which creates a new DI scope per refresh and re-dispatches the same request through `IEncina` with a refresh marker in the request context), and the trigger on the cache-hit path. All 8 providers (Memory, Hybrid, Redis, Valkey, Dragonfly, Garnet, KeyDB, and the planned Memcached of #277) are served by the behavior with no provider change.

**Shared pieces (family cache-patterns-1):** the entry envelope field `ExpiresAtUtc`, `CachePatternSettings`, `CachePatternMetrics`, `CachePatternLog`, the `CachingPatterns` EventId range (450-499) and the request-flow contract belong to #266. This plan owns `IBackgroundCacheRefresher`, `CacheRefreshMarker` and EventIds 456-461; #268 reuses the refresher for its soft-timeout background completion. The siblings #272, #274, #275, #276 and #278 reuse these types.

**Affected packages:** `Encina.Caching`. **Provider category:** Caching (8), behavior-level.

---

## Design Choices

<details>
<summary><strong>1. How a background refresh executes (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Re-dispatch the request through `IEncina.Send(request, context, token)` in a new DI scope created by `IServiceScopeFactory`, with a refresh marker in the context metadata so the cache behavior skips the read and does not trigger again** | Runs on a live scope with its own token and the full pipeline (validation, authorization, tenancy behaviors); no captured scope | Pipeline behaviors run a second time; the refresh needs the context replay of Design Choice 2 |
| **B) Call the captured `nextStep()` in a background task (the issue's sketch)** | Simplest | Runs on a scope that is disposed when the request ends and a token that is cancelled; scoped services (a `DbContext`) throw `ObjectDisposedException` |
| **C) No background work: the first request past the threshold recomputes inline (the caller pays the latency)** | No scope or context problem | Not what the issue asks for ("current request returns immediately") |

### Chosen Option: **A - re-dispatch in a new scope** (recommended, pending the maintainer)

### Rationale

- B is unsafe on the evidence of the delegate shape; C wins if the maintainer prefers no background execution at all, in which case #266's early expiration already provides the inline form and this issue reduces to documentation.
- The marker is a metadata entry on the `IRequestContext` of the refresh call (`WithMetadata`); a caller able to set context metadata can only force a recompute, never read or write another key.
- Class: Dilemma

</details>

<details>
<summary><strong>2. Context and identity the refresh runs under (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Replay tenant id, correlation id, causation id and metadata of the triggering request; a request with `VaryByUser = true` is not eligible for eager refresh; a request with no tenant under multi-tenancy never reaches the hit path (the existing bypass), so no tenant-less refresh exists** | The refreshed value is stored under the key of the tenant that triggered it; no identity is invented | User-specific entries are not refreshed in the background; a handler that reads the tenant from an ambient provider instead of the context is the application's risk (stated in the XML docs and tested with a context-reading handler) |
| **B) Also replay the user identity** | `VaryByUser` entries refresh too | `RequestIdentity` reads `Anonymous` once its scope ends (#1892), so a detached identity snapshot is not available in the code today |
| **C) Disable eager refresh whenever `MultiTenancyMarker` is registered** | No cross-tenant risk at all | Multi-tenant applications get no feature |

### Chosen Option: **A - replay the tenant, exclude `VaryByUser`** (recommended, pending the maintainer)

### Rationale

- The failure to avoid is a value computed for one tenant being stored under another tenant's key; the key and the replayed tenant come from the same request, and the integration test of Phase 3 uses two tenants and a handler that returns its context tenant.
- Option C wins when handlers resolve the tenant from a scoped ambient provider (for example an HTTP-based `ITenantProvider`), which a background scope cannot reproduce.
- Class: Dilemma

</details>

<details>
<summary><strong>3. Preventing concurrent refreshes of one key (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) In-process set of keys being refreshed (a `ConcurrentDictionary`), checked and set by `TrySchedule`** | No write to the cache; nothing can get stuck; N nodes refresh a key at most N times per cycle | Not cross-node |
| **B) `IsRefreshing` flag inside the cached entry (the issue)** | Cross-node in principle | A read-modify-write race; a node that dies leaves the flag set until the TTL ends, blocking refresh; a write per trigger |
| **C) An `IDistributedLockProvider` lock per key** | Exactly one refresh across nodes | A lock round trip per trigger; no lock provider with the Hybrid package |

### Chosen Option: **A - in-process set** (recommended, pending the maintainer)

### Rationale

- A refresh costs one handler call; N parallel refreshes (one per node) are cheap compared with the stuck-flag failure of B. The issue's matrix marks distributed locks "N/A: IsRefreshing flag prevents concurrency"; this plan replaces that mechanism and says so.
- Option C wins for a very expensive handler on a large cluster; it composes with #266's lock mode.
- Class: Dilemma

</details>

<details>
<summary><strong>4. `EagerRefreshOnlyOnAccess` and timer-based refresh (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Ship on-access refresh only and no `EagerRefreshOnlyOnAccess` option** | No dead option; the issue's own "Alternatives Considered" rejects timer refresh as wasteful | The listed option is omitted |
| **B) Ship the option with a timer mode** | Matches the configuration block | A timer must remember which requests to re-run (a request registry per type and key), which the issue does not design |

### Chosen Option: **A - on-access only** (recommended, pending the maintainer)

### Rationale

- Every line serves a current purpose (AGENTS.md section 1); a mode with no design is dead code. Option B wins if hot keys must stay warm without traffic, which is cache warming and belongs to #269.
- Class: Dilemma

</details>

<details>
<summary><strong>5. Shape of the background worker (Dilemma D5, follows D1 option A or B)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A bounded `Channel<RefreshWork>` (capacity `EagerRefreshQueueCapacity`, default 256; full means the trigger is dropped and counted) read by `EagerRefreshMaxConcurrency` workers (default 4) in a `BackgroundService`, registered by `AddEncinaCaching` and stopping with the host** | Bounded memory and concurrency; graceful stop; precedent in the package | Two options and a hosted service |
| **B) `Task.Run` per refresh, tracked in a set** | Less code | Unbounded under a burst; shutdown must cancel and await by hand |

### Chosen Option: **A - bounded channel and hosted workers** (recommended, pending the maintainer)

### Rationale

- The package already hosts a refresh loop (`src/Encina.Caching/Sharding/Services/TopologyRefreshHostedService.cs`); every worker iteration catches its failure (AGENTS.md section 3: background loops never let an exception escape) and an `OperationCanceledException` from the stopping token ends the worker.
- A dropped trigger is not an error: the next hit past the threshold triggers again.
- Option B wins where a host forbids hosted services.
- Class: Dilemma

</details>

<details>
<summary><strong>6. A failed refresh (Dilemma D6, follows D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep the existing entry, log the error code (or `ex.ForLogging()`), count the failure, never fail the triggering request; `ThrowOnCacheErrors` does not apply (no caller to throw to); no backoff in this plan** | The cache never makes a healthy request fail; failure is visible in logs and metrics; the precedent is `QueryCachingPipelineBehavior.TryCacheResultAsync` (lines 255-263) | A permanently failing refresh retries on every hit past the threshold, bounded by the queue and the in-flight set |
| **B) Remove the entry on failure** | The next request recomputes | Turns a refresh failure into a miss storm |

### Chosen Option: **A - keep the entry, fail visibly** (recommended, pending the maintainer)

### Rationale

- AGENTS.md section 3: a `Left` from `IEncina.Send` fails that operation and is never reported as success; an `EncinaError.Message` never reaches a log. Under Design Choice 1 option C there is no background refresh and this choice does not apply (the inline recompute follows #266's rule that a failed early refresh serves the cached value).
- Option B wins when a failing handler means the cached value is wrong and must not be served again.
- Class: Dilemma

</details>

---

## Implementation Phases

### Phase 1: Threshold, options and the age decision (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

PR 1 holds exactly these 13 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Patterns/EagerRefreshPolicy.cs` | new (internal static) |
| 2 | `src/Encina.Caching/Patterns/CachePatternSettings.cs` | modify (`EagerRefreshThreshold`; owned by #266) |
| 3 | `src/Encina.Caching/Attributes/CacheAttribute.cs` | modify (`EagerRefreshThreshold`) |
| 4 | `src/Encina.Caching/Configuration/CachingOptions.cs` | modify (`DefaultEagerRefreshThreshold`, `EnableEagerRefresh`, `EagerRefreshMaxConcurrency`, `EagerRefreshQueueCapacity`) |
| 5 | `src/Encina.Caching/ServiceCollectionExtensions.cs` | modify (copy the four options) |
| 6 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 7 | `tests/Encina.UnitTests/Caching/EagerRefreshPolicyTests.cs` | new |
| 8 | `tests/Encina.UnitTests/Caching/CachePatternSettingsTests.cs` | modify |
| 9 | `tests/Encina.UnitTests/Caching/CachingOptionsTests.cs` | modify |
| 10 | `tests/Encina.UnitTests/Caching/ServiceCollectionExtensionsTests.cs` | modify |
| 11 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 12 | `changelog.d/267-cache-eager-refresh-options.added.md` | new (`mechanical-fixer`) |
| 13 | `tests/Encina.GuardTests/Infrastructure/Caching/CachingOptionsEagerRefreshGuardTests.cs` | new (the two validated setters) |

Tasks:

1. **Create** `internal static class EagerRefreshPolicy` with `static bool ShouldRefresh(DateTime nowUtc, DateTime cachedAtUtc, DateTime expiresAtUtc, double threshold)`: false when `threshold <= 0`, when the lifetime `expiresAtUtc - cachedAtUtc` is not positive, or when `nowUtc >= expiresAtUtc` (an expired entry is #268's stale path or a miss); otherwise `(nowUtc - cachedAtUtc) / lifetime >= threshold`.
2. **Modify** `CacheAttribute` (`public double EagerRefreshThreshold { get; init; }`, 0 disables, valid range 0 to under 1) and `CachingOptions` (`DefaultEagerRefreshThreshold` 0, `EnableEagerRefresh` false (opt-in: no hosted service, queue or workers unless asked, AGENTS.md section 1), `EagerRefreshMaxConcurrency` 4, `EagerRefreshQueueCapacity` 256); the attribute value wins when greater than 0. `EagerRefreshMaxConcurrency` and `EagerRefreshQueueCapacity` have setters that throw `ArgumentOutOfRangeException` below 1 (a capacity below 1 would throw from `Channel.CreateBounded` at start and a concurrency of 0 would start no worker, so refreshes would silently never run; the convention is that of AGENTS.md section 9 guard tests, and PR 1 adds `CachingOptionsEagerRefreshGuardTests.cs`). Extend `CachePatternSettings.Resolve` with the same range validation and failure mode as the maintainer's answer to #266 Design Choice 7. Copy every new option in `AddEncinaCaching` (the copy list is hand-written, lines 53-72).
3. **Tests:** the policy at 0, below, at and above the threshold, at and after expiry, with a zero or negative lifetime; resolution takes the attribute over the option, rejects 1, -0.1 and NaN; DI proves the four options reach `IOptions<CachingOptions>`. **Guard tests:** `CachingOptionsEagerRefreshGuardTests` (file 13): `EagerRefreshMaxConcurrency` and `EagerRefreshQueueCapacity` of 0 and -1 throw `ArgumentOutOfRangeException`, valid values are stored; the policy is internal without `InternalsVisibleTo` for `Encina.GuardTests`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #267 adds eager background refresh to QueryCachingPipelineBehavior. #266 (merged before
this phase) added CacheEntry<T>.ExpiresAtUtc and CachePatternSettings (src/Encina.Caching/Patterns).

TASK:
Add EagerRefreshPolicy.ShouldRefresh, EagerRefreshThreshold on CacheAttribute, the four CachingOptions members, their validation in
CachePatternSettings.Resolve and the copy lines in AddEncinaCaching. Write the unit tests; ask mechanical-fixer for PublicAPI.Unshipped.txt,
the manifest entry and the changelog fragment.

KEY RULES:
- Pure functions take times as arguments; the caller passes TimeProvider time. Never DateTime.UtcNow.
- Threshold 0 disables; defaults change nothing (pay for what you use).
- Validation behaves as the maintainer answered #266 Design Choice 7 (given in your brief).

REFERENCE FILES:
- src/Encina.Caching/Patterns/CachePatternSettings.cs
- src/Encina.Caching/ServiceCollectionExtensions.cs
- tests/Encina.UnitTests/Caching/CachingOptionsTests.cs
```

</details>

### Phase 2: The background refresher (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

PR 2 holds exactly these 12 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Abstractions/IBackgroundCacheRefresher.cs` | new (public) |
| 2 | `src/Encina.Caching/Patterns/BackgroundCacheRefresher.cs` | new (internal sealed `BackgroundService` implementing the interface) |
| 3 | `src/Encina.Caching/Diagnostics/CachePatternLog.EagerRefresh.cs` | new (ids 456-461) |
| 4 | `src/Encina.Caching/Diagnostics/CachePatternMetrics.EagerRefresh.cs` | new (partial class of #266 PR 4) |
| 5 | `src/Encina.Caching/ServiceCollectionExtensions.cs` | modify (single-instance registration, see task 5) |
| 6 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 7 | `tests/Encina.UnitTests/Caching/BackgroundCacheRefresherTests.cs` | new |
| 8 | `tests/Encina.UnitTests/Caching/ServiceCollectionExtensionsTests.cs` | modify (`ValidateOnBuild` and `ValidateScopes`) |
| 9 | `tests/Encina.ContractTests/Caching/BackgroundCacheRefresherContractTests.cs` | new (instantiates the real refresher; asserts the `IBackgroundCacheRefresher` contract and its argument checks) |
| 10 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`: targets of this PR's files) |
| 11 | `changelog.d/267-cache-eager-refresh-worker.added.md` | new (`mechanical-fixer`) |
| 12 | `tests/Encina.GuardTests/Infrastructure/Caching/BackgroundCacheRefresherGuardTests.cs` | new (through DI, see task 6) |

Tasks:

1. **Create** `public interface IBackgroundCacheRefresher { bool TrySchedule(string cacheKey, string? tenantId, Func<IServiceProvider, CancellationToken, ValueTask<Either<EncinaError, Unit>>> refresh); }`: `false` when the key is already in flight or the queue is full (counted by reason). The delegate receives the new scope's provider; the refresher never sees the request type.
2. **Create** `BackgroundCacheRefresher`: `Channel.CreateBounded<Work>(new BoundedChannelOptions(capacity) { FullMode = Wait, SingleReader = false })` used with `TryWrite`; `MaxConcurrency` workers started in `ExecuteAsync`; per item: `using var scope = scopeFactory.CreateScope()`, run the delegate, remove the key from the in-flight set in a `finally`. Every item is wrapped in try/catch: a `Left` logs 458 with the error code only; an exception logs 458 with `ex.ForLogging()`; `OperationCanceledException` of the stopping token ends the worker and logs 461 once with the number of dropped items. It takes `IServiceScopeFactory`, `IOptions<CachingOptions>`, `ILogger` and `TimeProvider` (constructor, DI-resolved; the singleton never holds a scoped service).
3. **Log and metrics (EventIds 456-461, packed, registered by #266):** 456 scheduled, 457 completed (duration), 458 failed, 459 skipped because in flight, 460 rejected because the queue is full, 461 stopped with N dropped. Counter `encina.cache.eager_refresh` with tag `outcome` (`scheduled`, `completed`, `failed`, `skipped_in_flight`, `rejected_full`) and the tag `encina.tenant_id` only when `tenantId` is present and multi-tenancy is registered (ADR-054 D4); never a key or message. If the maintainer answers Design Choice 1 with option C, 456, 459, 460 and 461 are not emitted and are released back to the family.
4. **Tests (unit):** a scheduled item runs once in a new scope (a counting scope factory); the same key twice runs once; a full queue returns false and counts; a `Left` and an exception are logged and counted and the worker keeps running; the stopping token ends the workers and logs 461; `MaxConcurrency` bounds parallelism (gate); the in-flight key is released after failure; DI builds with `ValidateOnBuild` and `ValidateScopes`, resolves `IBackgroundCacheRefresher` and the hosted service and asserts they are the same instance (`ReferenceEquals`), and `EnableEagerRefresh = false` (the default) registers neither.
5. **Registration (opt-in):** only when `EnableQueryCaching` and `EnableEagerRefresh` are both true: `TryAddSingleton<BackgroundCacheRefresher>()`, `TryAddSingleton<IBackgroundCacheRefresher>(sp => sp.GetRequiredService<BackgroundCacheRefresher>())` and `AddHostedService(sp => sp.GetRequiredService<BackgroundCacheRefresher>())`, so there is one instance, one queue and one set of workers. Without the registration the behavior receives a null refresher and never schedules.
6. **Contract and guard tests:** the contract test instantiates the real `BackgroundCacheRefresher`, starts it (`StartAsync` with a real `IServiceScopeFactory`) and asserts: a first schedule returns true and the delegate runs once in a new scope, the same key while in flight returns false, a full queue returns false, a failing delegate does not stop later items, null `cacheKey` and null `refresh` throw `ArgumentNullException`; it runs the workers, which is why the contract target below is reachable. The guard test file `BackgroundCacheRefresherGuardTests.cs` (file 12) resolves `IBackgroundCacheRefresher` from a provider built with `AddEncinaCaching` and `EnableQueryCaching` plus `EnableEagerRefresh` (the interface is public, so no `InternalsVisibleTo` is needed) and asserts `TrySchedule` null `cacheKey` and null `refresh` throw `ArgumentNullException`, with a valid call returning true.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
Phase 1 is merged. #266 PR 3 registered the CachingPatterns range (450-499) and CachePatternLog, and #266 PR 4 created CachePatternMetrics; #267 owns ids 456-461.

TASK:
Add IBackgroundCacheRefresher and BackgroundCacheRefresher (bounded Channel, MaxConcurrency workers, IServiceScopeFactory per item),
CachePatternLog.EagerRefresh.cs, CachePatternMetrics.EagerRefresh.cs, the registration, and the unit tests; ask mechanical-fixer for
PublicAPI.Unshipped.txt, the manifest and the changelog fragment.

KEY RULES:
- A worker iteration never lets an exception escape; OperationCanceledException from the stopping token ends the worker.
- Log only error codes, ex.ForLogging(), redacted keys; never EncinaError.Message.
- The singleton resolves no scoped service: a new scope per item through IServiceScopeFactory.
- Tag encina.tenant_id only with multi-tenancy on and a tenant present. Time from TimeProvider. No Thread.Sleep in tests.

REFERENCE FILES:
- src/Encina.Caching/Sharding/Services/TopologyRefreshHostedService.cs
- src/Encina.Caching/Diagnostics/CachePatternLog.cs (from #266)
- src/Encina/Recoverability/RetryMetrics.cs
```

</details>

### Phase 3: Trigger on the hit path (pull request 3)

<details>
<summary><strong>Tasks</strong></summary>

PR 3 holds exactly these 10 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs` | modify (trigger, marker, optional `IBackgroundCacheRefresher? refresher = null`) |
| 2 | `src/Encina.Caching/Patterns/CacheRefreshMarker.cs` | new (internal static: metadata key and helpers) |
| 3 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`: the constructor line changes) |
| 4 | `tests/Encina.UnitTests/Caching/QueryCachingEagerRefreshTests.cs` | new |
| 5 | `tests/Encina.GuardTests/Infrastructure/Caching/QueryCachingPipelineBehaviorGuardTests.cs` | modify |
| 6 | `tests/Encina.IntegrationTests/Infrastructure/Caching/EagerRefreshIntegrationTests.cs` | new |
| 7 | `docs/features/query-caching.md` | modify (`docs-writer`) |
| 8 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 9 | `changelog.d/267-cache-eager-refresh.added.md` | new (`mechanical-fixer`) |
| 10 | `tests/Encina.UnitTests/Caching/CachePatternSettingsTests.cs` | modify (the eligibility rule for `VaryByUser`) |

Tasks:

1. **Behavior:** in the hit path, after the identity and tenant checks and after the entry is served as `found`, when the settings give a threshold above 0, the request is not `VaryByUser` (Design Choice 2) and `EagerRefreshPolicy.ShouldRefresh(_timeProvider.GetUtcNow().UtcDateTime, entry.CachedAtUtc, entry.ExpiresAtUtc, threshold)` is true, call `_refresher.TrySchedule(cacheKey, context.TenantId, refresh)`; the delegate resolves `IEncina` from the new scope's provider and calls `Send(request, refreshContext, token)` where `refreshContext = context.WithMetadata(CacheRefreshMarker.Key, true)` (the tenant, correlation and metadata are replayed by the copy; no identity). The served response is never delayed. A request that carries the marker skips the cache read, runs the miss path (handler, store with jitter) and never schedules. The entry's `ExpiresAtUtc` absent (an older build, or a sliding entry, which has no fixed lifetime) means not eligible. A null refresher means no eager refresh (direct construction, or `EnableEagerRefresh` false). **Ownership and order:** the hit path returns the whole entry since #266 PR 1 (`TryGetForCallerAsync` returns `(Found, Entry)`); this PR builds on it. Merge order of the family PRs that edit `QueryCachingPipelineBehavior`: #266 PR 1, PR 3, PR 4, then this PR 3, then #268 PR 1 and PR 2, then #271 PR 5 (the later PR rebases; the request-flow contract of #266 fixes the order of the checks: threshold, early draw, stale path).
2. **Precedence (request-flow contract of #266):** the threshold is checked before #266's early-expiration draw; when it triggers, no early draw is made.
3. **Tests (unit, real `MemoryCacheProvider` and a `HybridCacheProvider` built through DI):** below threshold nothing is scheduled; at the threshold the old value is served at once and one refresh runs; ten hits at the threshold schedule one refresh; the refresh stores a new entry with a reset TTL; a failing handler leaves the old entry and logs 458; `VaryByUser` never schedules; a request with the marker never schedules and always runs the handler; a missing tenant under multi-tenancy never reaches the trigger; two tenants refresh into their own keys. **Guard tests:** a null `refresher` is valid (a valid-result test, no refresh scheduled). **Integration (shared `RedisCollection`):** a real `RedisCacheProvider`, a real `IEncina` with the caching behavior, two tenants and a handler that returns its context tenant: after the threshold, each tenant's refreshed entry holds its own tenant's value. The Redis family (Valkey, Dragonfly, Garnet, KeyDB) shares `RedisCacheProvider`, so this is their coverage; Memcached follows #277.
4. **Docs:** `docs-writer` adds the "Eager refresh" section to `docs/features/query-caching.md` (threshold, what the refresh runs under, the `VaryByUser` exclusion, the ambient-tenant caveat), citing tests by name.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
Phases 1 and 2 are merged: EagerRefreshPolicy, the options, IBackgroundCacheRefresher and its hosted service exist.

TASK:
Wire the trigger into QueryCachingPipelineBehavior (optional trailing parameter IBackgroundCacheRefresher? refresher = null), add
CacheRefreshMarker, the refresh delegate (IEncina.Send in the new scope with the marker, tenant replayed, no identity), the unit, guard
and Redis integration tests; ask mechanical-fixer for PublicAPI, manifest and changelog and docs-writer for the docs section.

KEY RULES:
- Never delay the served response; never call nextStep() from the background.
- VaryByUser requests are not eligible; a marker request never schedules.
- A failed refresh keeps the entry and logs the error code only.
- Keep Handle at CRAP 10 or below: extract the trigger into a private method.

REFERENCE FILES:
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs
- src/Encina.Caching/Behaviors/IdentityPinnedRequestContext.cs
- src/Encina/Abstractions/IRequestContext.cs
- tests/Encina.IntegrationTests/Infrastructure/Caching/RedisCacheProviderIntegrationTests.cs
```

</details>

### Phase 4: Coverage targets and finalization

<details>
<summary><strong>Tasks</strong></summary>

1. **No new error code** (no ADR-062 category to name): a refresh `Left` is the handler's own. **EventIds** 456-461 only.
2. **Coverage targets (per flag, new or touched files).** Each row is entered in the manifest of the PR that adds the file and its tests, never later (#1762): PR 1 sets `EagerRefreshPolicy.cs`; PR 2 sets `BackgroundCacheRefresher.cs`, `IBackgroundCacheRefresher.cs` and the two partial files; PR 3 sets `CacheRefreshMarker.cs` and the behavior row.

| File | Unit | Guard | Contract | Integration | Justification |
|------|:----:|:-----:|:--------:|:-----------:|---------------|
| `Patterns/EagerRefreshPolicy.cs` | 100 | 0 | 0 | 0 | One pure function fully unit tested; internal without `InternalsVisibleTo` for the guard project |
| `Patterns/CachePatternSettings.cs` | keeps 100 | 0 | 0 | 0 | Owned by #266 (target 100 set there); the new threshold branches are covered by the settings tests of PR 1 |
| `Attributes/CacheAttribute.cs` | keeps 100 | 0 | 0 | 0 | #266 sets the first target (the manifest has none today); `EagerRefreshThreshold` is an initializer executed by the behavior and settings tests |
| `Configuration/CachingOptions.cs` | keeps | keeps 85, raise to 90 | 0 | 0 | The two validated setters are exercised by `CachingOptionsEagerRefreshGuardTests` in PR 1 |
| `ServiceCollectionExtensions.cs` | keeps 90 | keeps 30 | 0 | 0 | #266 sets the first target; the four copy lines and the registration branch are executed by the DI tests of PR 1 and PR 2 |
| `Patterns/BackgroundCacheRefresher.cs` | 95 | 0 | 70 | 0 | Workers, queue and failure paths are driven with gates and a counting scope factory; the contract test of PR 2 runs the schedule, in-flight, full-queue and argument-check paths; no `InternalsVisibleTo` for the guard project |
| `Patterns/CacheRefreshMarker.cs` | 100 | 0 | 0 | 0 | Two trivial members covered by the behavior tests |
| `Abstractions/IBackgroundCacheRefresher.cs` | 0 | 0 | 0 | 0 | An interface with no executable lines |
| `Diagnostics/CachePatternLog.EagerRefresh.cs`, `CachePatternMetrics.EagerRefresh.cs` | 90 | 0 | 0 | 0 | Generated log bodies and counter calls run in the refresher tests |
| `Behaviors/QueryCachingPipelineBehavior.cs` | keeps 98 | keeps 20 | 0 | 40 | Existing targets are never lowered; the trigger is unit tested; the integration flag runs only the Redis hit path with two tenants |

   The contract test of PR 2 is a real test of the public `IBackgroundCacheRefresher`; no justification file is written for a contract test. No load or benchmark file (the bounded channel is covered by the gated unit tests).
3. Verification per PR: `dotnet build --configuration Release` with zero warnings; `dotnet test` of `Encina.UnitTests` (Caching), `Encina.GuardTests`, and for PR 3 `Encina.IntegrationTests` (Redis collection); per-flag coverage and the CRAP table of changed methods.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
The three code PRs are merged; each carried its own per-flag targets.

TASK:
Verify, with coverage-report.cs, that each PR's measured numbers reach the targets above; fix any gap through mechanical-fixer in a small PR; run
--check-justifications.

KEY RULES:
- Never lower an existing target (target-ratchet); no new log, metric or error code beyond the plan.

REFERENCE FILES:
- .github/coverage-manifest/Encina.Caching.json
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| "stale-while-revalidate" (RFC 5861, HTTP cache extension) | The behavior pattern: serve now, revalidate in the background |
| AGENTS.md section 3 | `TimeProvider`; background loops never let an exception escape; errors are not swallowed; no message in logs |
| AGENTS.md section 5 (caching) | Consistent across all 8 providers |
| ADR-054 D4 | Tenant tag on metrics only with multi-tenancy on |
| ADR-021 | EventId range registered before use (done by #266) |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `QueryCachingPipelineBehavior`, `CacheEntry<T>` | `src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs` | Hit path and trigger |
| `CachePatternSettings`, `CachePatternLog`, `CachePatternMetrics` | `src/Encina.Caching` (from #266) | Settings, partial log and metrics files |
| `TopologyRefreshHostedService` | `src/Encina.Caching/Sharding/Services/` | Precedent for a hosted refresh loop |
| `IEncina.Send(request, context, token)` | `src/Encina/Abstractions` | Re-dispatch in the new scope |
| `IRequestContext.WithMetadata` / `WithTenantId` | `src/Encina/Abstractions/IRequestContext.cs` | Marker and tenant replay |
| `RedisCollection`, `RedisFixture` | `tests/Encina.IntegrationTests/Infrastructure/Caching/Fixtures/` | Integration test |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Caching` (`CachingPatterns`, registered by #266) | 456-461 | 456 scheduled, 457 completed, 458 failed, 459 skipped in flight, 460 rejected queue full, 461 stopped with dropped items |

### Estimated File Count

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | 6 | 4 | New: policy, refresher interface, refresher, marker, two partial files; modified: settings, attribute, options, DI, behavior |
| Tests | 4 | 7 | New: policy, refresher, eager-refresh and integration tests; modified: settings, options, DI, guard behavior tests |
| Documentation | 0 | 1 | `docs/features/query-caching.md` |
| Configuration | 3 | 2 | Three changelog fragments; the manifest and `PublicAPI.Unshipped.txt` |
| **Total** | **13** | **14** | 3 pull requests: 13, 12 and 10 files, each at most 15 files and 1500 lines |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #267 serves a cache entry past a TTL fraction immediately and refreshes it in the
background. #266 (merged first) owns CacheEntry.ExpiresAtUtc, CachePatternSettings, CachePatternLog, CachePatternMetrics and the
CachingPatterns EventId range 450-499; this plan uses ids 456-461.

IMPLEMENTATION OVERVIEW:
1. PR 1: EagerRefreshThreshold (attribute, options, settings validation), EagerRefreshPolicy.
2. PR 2: IBackgroundCacheRefresher and its hosted workers (bounded channel, new scope per item), logs, metrics.
3. PR 3: the trigger in QueryCachingPipelineBehavior, re-dispatch through IEncina with a marker, tenant replay, docs.

KEY PATTERNS:
- Never run nextStep() in the background; re-dispatch in a new scope. VaryByUser is not eligible. A marker request never schedules.
- Failed refresh keeps the entry; logs carry codes and redacted keys only.
- Follow the maintainer's answers to Design Choices 1 to 6, given in your brief.

REFERENCE FILES:
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs
- src/Encina.Caching/Sharding/Services/TopologyRefreshHostedService.cs
- .github/coverage-manifest/Encina.Caching.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Include | Extends `QueryCachingPipelineBehavior` for all 8 providers. |
| 2 | OpenTelemetry | ✅ Include | Counter `encina.cache.eager_refresh` on the `Encina.Caching` meter of #266; tenant tag per ADR-054 D4; meter registration in the OTel package is #723. |
| 3 | Structured Logging | ✅ Include | EventIds 456-461 in `CachingPatterns`. |
| 4 | Health Checks | ❌ N/A | A behavior-level pattern with no backend of its own. |
| 5 | Validation | ✅ Include | Threshold range validated at resolution (follows #266 Design Choice 7). |
| 6 | Resilience | ✅ Include | The issue deferred refresh-failure handling; it is included here: bounded queue, keep-entry-on-failure, visible failure. |
| 7 | Distributed Locks | ❌ N/A | With Design Choice 3 option A the in-flight set replaces the flag; option C would add the lock mode of #266. |
| 8 | Transactions | ❌ N/A | A refresh is a read; the dispatch never calls `SaveChangesAsync` itself. |
| 9 | Idempotency | ❌ N/A | A refresh recomputes and overwrites an idempotent cache value. |
| 10 | Multi-Tenancy | ✅ Include | The refresh replays the tenant and writes under that tenant's key; no tenant-less refresh exists (existing bypass); the issue's deferral to #746 covers the module dimension only. |
| 11 | Module Isolation | ⏭️ Defer | Module-scoped refresh keys follow the module dimension of #746 (open). |
| 12 | Audit Trail | ❌ N/A | Infrastructure with no compliance event. |

**Deferred items:** module dimension (#746, open).

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| #266 PR 1 (`ExpiresAtUtc`, `CachePatternSettings`, whole-entry hit path), PR 3 (`CachePatternLog`, range 450-499) and PR 4 (`CachePatternMetrics.cs`, created there) | Pending | PR 1 needs #266 PR 1; PR 2 needs #266 PR 3 and PR 4 (it adds a partial of the metrics class) |
| `IServiceScopeFactory`, `IEncina.Send(request, context, token)` | ✅ Available | On `origin/main` |
| Maintainer answers to Design Choices 1 to 6 | Pending | Asked when work starts |

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas (Design Choices 1 to 6) when work starts, records the answers under `## Maintainer Decisions`, then briefs three workers in PR order.
2. After PR 2 merges, comment on #268 that `IBackgroundCacheRefresher` is available for its soft-timeout completion.
