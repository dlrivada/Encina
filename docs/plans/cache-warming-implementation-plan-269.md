# Implementation Plan: Cache warming (pre-warming system)

> **Issue**: [#269](https://github.com/dlrivada/Encina/issues/269)
> **Type**: Feature
> **Complexity**: Medium (3 code phases, 3 pull requests of 13, 6 and 14 files)
> **Estimated Scope**: about 550 lines of production code, about 1,000 lines of tests
> **Milestone**: v0.18.0 — Integration & Platform Capabilities

---

## Summary

Issue #269 asks for a cache warming system: `ICacheWarmer` implementations and a `[CacheWarmer]` attribute on query requests, priorities, sequential and parallel strategies, a timeout per slow warmer, a hosted service that runs on startup, progress logging, metrics and options. Re-checked against `origin/main` (f484cdf4f):

| Premise of the issue | State in the code |
|----------------------|-------------------|
| Encina has no cache warming | True: no warmer, attribute or hosted service exists in `src/Encina.Caching` (the only hosted service there is `Sharding/Services/TopologyRefreshHostedService.cs`). |
| `[CacheWarmer(Priority, WarmOnStartup, WarmingInstances)]` on a query record warms it | A request with parameters (`GetProductQuery(Guid Id)`) cannot be instantiated from an attribute, and `WarmingInstances = 10` has no defined source of the ten instances (Design Choice 1). |
| `Task WarmAsync(CancellationToken)` | Operations return `Either<EncinaError, T>` (AGENTS.md section 3, ROP); a failure by exception cannot be counted or classified (Design Choice 2). |
| The warmer example writes `_cache.SetAsync($"product:{id}", ...)` | A hand-written key does not match the key `DefaultCacheKeyGenerator` builds for the same query (`{prefix}:t:{tenant}:{type}:{hash}`), so the request never hits it; warming through `IEncina` fills the key the behavior reads. |
| Affected packages include `Encina.AspNetCore` | Nothing in this plan needs it: the generic host runs `IHostedService`, so only `Encina.Caching` changes. |
| `WarmingStrategy` has `TopHeavy` and `AccessPattern` | Neither has a data source (no access statistics exist); see Design Choice 3. |
| Tenant-scoped warming sets, see #746 | #746 is the module dimension of cache keys; tenant-scoped warming needs its own design (Design Choice 4). |

The plan adds `ICacheWarmer` (priority, name, `ValueTask<Either<EncinaError, CacheWarmerResult>> WarmAsync`), `ICacheWarmingService` (the engine, also a manual trigger), `[CacheWarmer]` plus `IWarmingRequestSource<TRequest>` (the source of the request instances), a hosted service, options, a log class and a meter. The cache fill goes through `IEncina.Send`, so all 8 providers (Memory, Hybrid, Redis, Valkey, Dragonfly, Garnet, KeyDB, and the planned Memcached of #277) are warmed identically by the existing `QueryCachingPipelineBehavior`; no provider changes.

**Shared pieces (family cache-patterns-1):** `CachePatternLog`, `CachePatternMetrics` (partial classes) and the `CachingPatterns` range (450-499) belong to #266; this plan owns EventIds 469-477. It does not use `CacheEntry<T>` envelope fields.

**Affected packages:** `Encina.Caching`. **Provider category:** Caching (8), behavior-level.

---

## Design Choices

<details>
<summary><strong>1. Warmer model and the source of request instances (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ICacheWarmer` for custom code, plus `[CacheWarmer(Priority)]` on a request type (the issue's per-attribute `WarmOnStartup` is dropped: `ICacheWarmer` has no per-warmer startup switch and the engine runs every registered warmer; the global `WarmOnStartup` option decides whether the hosted service runs at all) paired with an `IWarmingRequestSource<TRequest>` (`IAsyncEnumerable<TRequest> GetRequestsAsync(CancellationToken)`) registered by `AddEncinaRequestCacheWarmer<TRequest, TSource>()`; `WarmingInstances` becomes a cap on how many requests the source yields** | The attribute works for requests with parameters; the issue's `WarmingInstances` keeps a meaning; the fill goes through the real pipeline | A second type per warmed request |
| **B) `ICacheWarmer` only, no attribute** | Smallest surface | The issue's attribute task is dropped |
| **C) The attribute only on a parameterless request, instantiated with `Activator`; `WarmingInstances` dropped** | No source type | Does not cover `GetProductQuery(Guid)`, the issue's second example |

### Chosen Option: **A - warmers plus attribute with a request source** (recommended, pending the maintainer)

### Rationale

- A request warmer dispatches each request through `IEncina.Send` in a new scope (`IServiceScopeFactory`), so the key and the stored `CacheEntry` are the ones a real caller reads.
- Option B wins for the smallest first release; option C wins when every warmed request is parameterless.
- Class: Dilemma

</details>

<details>
<summary><strong>2. Return type of `ICacheWarmer.WarmAsync` (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ValueTask<Either<EncinaError, CacheWarmerResult>>` with `CacheWarmerResult(int Warmed, int Skipped)`** | ROP (AGENTS.md section 3); a failure carries a code; the report can count entries | Differs from the issue's `Task WarmAsync` |
| **B) `Task WarmAsync(CancellationToken)` as in the issue; an exception means failure** | Matches the issue | No code, no counts; failures are exceptions the engine must catch by type |

### Chosen Option: **A - Either with a result** (recommended, pending the maintainer)

### Rationale

- Option B wins when custom warmers are written by people unfamiliar with `Either`; the engine still catches an exception from either shape, logs `ex.ForLogging()` and reports the warmer failed.
- Class: Dilemma

</details>

<details>
<summary><strong>3. Which strategies ship (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Sequential` and `Parallel` only; `TopHeavy` and `AccessPattern` go to a follow-up issue** | Both shipped modes are fully defined; no invented data source | Two enum values of the issue are not delivered |
| **B) All four values** | Matches the issue | `AccessPattern` needs access statistics Encina does not collect; `TopHeavy` needs a percentage option the issue does not define |

### Chosen Option: **A - Sequential and Parallel** (recommended, pending the maintainer)

### Rationale

- The acceptance criteria name only priority order, parallel warming with configurable concurrency, timeouts and progress. The follow-up issue file for the two strategies is written with this plan (`artifacts/issues/cache-warming-topheavy-accesspattern-strategies.md`).
- Option B wins if the maintainer wants the enum final now, with the two extra values implemented as documented fallbacks.
- The default strategy is `Sequential` (recommended; the issue's example sets `Parallel` explicitly and states no default): it has no concurrency to configure; `Parallel` as default wins for applications with many independent warmers.
- Priority is respected in both strategies: warmers of one priority value run together (up to `WarmingConcurrency` in `Parallel`, one at a time in `Sequential`), and a priority group starts only after the previous group finished.
- Class: Dilemma

</details>

<details>
<summary><strong>4. Warming under multi-tenancy (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A request warmer for a request that varies by tenant (the default) is skipped, logged (EventId 476) and counted when `MultiTenancyMarker` is registered, because a request with no tenant bypasses the cache; tenants are warmed by a custom `ICacheWarmer` that supplies its own tenant context; tenant-scoped warming sets get their own issue** | Fails closed and never writes a tenant-less entry; no startup scan of every tenant's data | Multi-tenant applications warm only tenant-independent requests (`VaryByTenant = false`) or write a custom warmer |
| **B) Warm every tenant from `ITenantStore` automatically** | Complete | Startup reads every tenant's data; cost and exposure grow with the tenant count |

### Chosen Option: **A - skip and report, tenants by custom warmer** (recommended, pending the maintainer)

### Rationale

- Dispatching such a request would run the handler and then not cache it (`QueryCachingPipelineBehavior.TenantAllowsCache`, lines 143-152): wasted work, so the engine decides before dispatch.
- Option B wins for a small, bounded tenant list where full warm-up is a requirement.
- Class: Dilemma

</details>

<details>
<summary><strong>5. Startup behavior (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A `BackgroundService` that starts warming after the host has started; the application serves traffic while it runs** | Never delays or fails startup | The first requests may still miss |
| **B) `StartAsync` blocks until warming finishes or a separate `WarmingStartupTimeout` (an option that exists only under this choice, not `WarmingTimeout`, which stays the per-warmer timeout) elapses** | Traffic arrives on a warm cache | Slow startup; a slow warmer delays readiness probes |

### Chosen Option: **A - background after start** (recommended, pending the maintainer)

### Rationale

- Option B wins in deployments that gate traffic on readiness and can afford the startup time. Readiness reporting belongs to the follow-up health-check issue.
- Class: Dilemma

</details>

<details>
<summary><strong>6. Failure, timeout and cancellation of one warmer</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Each warmer runs under its own linked token with `WarmingTimeout` (default 5 minutes, as the issue's example); a `Left`, an exception (logged with `ForLogging()`), or a timeout marks that warmer failed or timed out and never stops the other warmers; host shutdown cancels the run and is not an error** | One slow or failing warmer cannot block the rest; the report counts every outcome | None |
| **B) The first failure stops the run** | Fail fast | A single bad warmer prevents all warming |

### Chosen Option: **A - isolate each warmer**

### Rationale

- **Class:** Trivial
- **Settled by:** the issue's acceptance criteria ("Timeout handling for slow warmers", "Warming progress logged"), AGENTS.md section 3 (a `Left` or failure is never reported as success; background loops never let an exception escape) and the precedent `Sharding/Services/TopologyRefreshHostedService.cs`. This holds under every option of Design Choices 1 to 5, which change what is warmed and when, not how a failure is isolated.

</details>

<details>
<summary><strong>7. Opt-in and options</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `EnableCacheWarming` false by default; `WarmOnStartup` true (the `WarmingStrategy` option and its default belong to Design Choice 3, not to this entry); `WarmingConcurrency` 4 (at least 1) and `WarmingTimeout` 5 minutes (positive), the values of the configuration example in the issue; property setters throw `ArgumentOutOfRangeException`** | Nothing runs unless enabled; invalid values fail at configuration | None |
| **B) Enabled by default** | Fewer lines in the application | Starts work nobody asked for |

### Chosen Option: **A - disabled by default**

### Rationale

- **Class:** Trivial
- **Settled by:** AGENTS.md section 1 and section 3 for the disabled default (every feature opt-in, patterns optional and disabled by default); the issue's configuration example (section 4, `WarmingConcurrency = 4`, `WarmingTimeout = TimeSpan.FromMinutes(5)`) for those two values; `ArgumentOutOfRangeException` setters as the guard-test convention of AGENTS.md section 9. The default strategy is not decided here (Design Choice 3).

</details>

---

## Implementation Phases

### Phase 1: Abstractions, attribute and options (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

PR 1 holds exactly these 13 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Abstractions/ICacheWarmer.cs` | new (also `CacheWarmerResult`) |
| 2 | `src/Encina.Caching/Abstractions/ICacheWarmingService.cs` | new (also `CacheWarmingReport`) |
| 3 | `src/Encina.Caching/Attributes/CacheWarmerAttribute.cs` | new |
| 4 | `src/Encina.Caching/Configuration/WarmingStrategy.cs` | new (public enum: `Sequential`, `Parallel`) |
| 5 | `src/Encina.Caching/Configuration/CachingOptions.cs` | modify (five options) |
| 6 | `src/Encina.Caching/ServiceCollectionExtensions.cs` | modify (copy the five options) |
| 7 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 8 | `tests/Encina.UnitTests/Caching/CachingOptionsTests.cs` | modify |
| 9 | `tests/Encina.UnitTests/Caching/ServiceCollectionExtensionsTests.cs` | modify |
| 10 | `tests/Encina.GuardTests/Infrastructure/Caching/CachingOptionsWarmingGuardTests.cs` | new (options setters and the attribute's `WarmingInstances`) |
| 11 | `tests/Encina.UnitTests/Caching/CacheWarmerAttributeTests.cs` | new (defaults, `Priority`, valid `WarmingInstances`) |
| 12 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 13 | `changelog.d/269-cache-warming-abstractions.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Create** `ICacheWarmer { int Priority { get; } string Name => GetType().Name; ValueTask<Either<EncinaError, CacheWarmerResult>> WarmAsync(CancellationToken cancellationToken); }`, `readonly record struct CacheWarmerResult(int Warmed, int Skipped)`, `ICacheWarmingService { Task<CacheWarmingReport> RunAsync(CancellationToken cancellationToken); }` and `sealed record CacheWarmingReport(int Succeeded, int Failed, int TimedOut, int Skipped, TimeSpan Duration)`.
2. **Create** `[CacheWarmer]` (`Priority` int, `WarmingInstances` int default 10, positive; `AttributeUsage` class, not inherited) and the enum.
3. **Modify** `CachingOptions`: `EnableCacheWarming` (false), `WarmOnStartup` (true), `WarmingStrategy` (`Sequential`), `WarmingConcurrency` (4, setter rejects below 1), `WarmingTimeout` (5 minutes, setter rejects zero or negative); copy all five in `AddEncinaCaching` (the copy list at lines 53-72 is hand-written).
4. **Tests:** the defaults; the DI copy; **guard tests:** `WarmingConcurrency = 0` and `-1`, `WarmingTimeout = TimeSpan.Zero` and negative throw `ArgumentOutOfRangeException`, valid values are stored; the attribute's `WarmingInstances` below 1 throws.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #269 adds cache warming to Encina.Caching. AddEncinaCaching
(src/Encina.Caching/ServiceCollectionExtensions.cs) copies every CachingOptions property by hand.

TASK:
Add ICacheWarmer, ICacheWarmingService with their result records, CacheWarmerAttribute, WarmingStrategy (Sequential, Parallel), the five
CachingOptions members with validating setters and the copy lines; write the unit and guard tests; ask mechanical-fixer for
PublicAPI.Unshipped.txt, the manifest and the changelog fragment.

KEY RULES:
- Disabled by default; ROP return type; setters throw ArgumentOutOfRangeException for invalid values.
- Follow the maintainer's answers to Design Choices 1 to 3 (model, return type, strategies and default), given in your brief.
- XML docs with an example on every public member.

REFERENCE FILES:
- src/Encina.Caching/Configuration/CachingOptions.cs
- src/Encina.Caching/ServiceCollectionExtensions.cs
- tests/Encina.UnitTests/Caching/CachingOptionsTests.cs
```

</details>

### Phase 2: The warming engine (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

PR 2 holds exactly these 6 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Warming/CacheWarmingService.cs` | new (internal sealed, implements `ICacheWarmingService`) |
| 2 | `src/Encina.Caching/Diagnostics/CachePatternLog.Warming.cs` | new (ids 469-475; PR 3 adds 476-477) |
| 3 | `src/Encina.Caching/Diagnostics/CachePatternMetrics.Warming.cs` | new (partial class of #266) |
| 4 | `tests/Encina.UnitTests/Caching/CacheWarmingServiceTests.cs` | new |
| 5 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 6 | `changelog.d/269-cache-warming-engine.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Create** `CacheWarmingService(IEnumerable<ICacheWarmer> warmers, IOptions<CachingOptions> options, ILogger<CacheWarmingService> logger, TimeProvider? timeProvider = null)`. `RunAsync`: logs 474 and returns an empty report when warming is disabled or there are no warmers; groups warmers by `Priority` ascending; per group, `Sequential` runs them one by one and `Parallel` runs them with `Parallel.ForEachAsync` bounded by `WarmingConcurrency`; each warmer runs under a linked token: `using var timeout = new CancellationTokenSource(WarmingTimeout, timeProvider)` linked to the caller's token with `CancellationTokenSource.CreateLinkedTokenSource` (`CancelAfter` has no `TimeProvider` overload, the constructor does); outcomes: `Right` (logs 470 with counts, which is the per-warmer progress line), `Left` (471, error code only), exception other than cancellation (471 with `ex.ForLogging()`), the warmer's own timeout (472), the caller's cancellation (475 once, the run ends). Logs 469 and 473 bracket the run with the warmer count, strategy and duration. Never an `EncinaError.Message` in a log.
2. **Metrics:** `Counter<long> encina.cache.warming` (tag `outcome`: `succeeded`, `failed`, `timed_out`, `skipped`; no warmer name, key or tenant is a tag) on the `Encina.Caching` meter of #266; no tenant tag, because the engine runs without a request context (the request warmer of Phase 3 counts skips by reason only). Histogram `encina.cache.warming.duration` in seconds.
3. **Tests (unit, `FakeTimeProvider`, gates):** priority order (a lower number starts first and a group waits for the previous one); `Parallel` never exceeds `WarmingConcurrency` (gate counter); `Sequential` runs one at a time; a `Left`, an exception and a timeout are counted without stopping the others; the caller's cancellation ends the run and logs 475; the per-warmer timeout fires with `FakeTimeProvider.Advance` and logs 472; disabled and empty cases; the report counts equal the outcomes. **Guard tests:** the class is internal and `Encina.Caching` has no `InternalsVisibleTo` for `Encina.GuardTests`; the null checks of the constructor are asserted in the unit tests and the manifest reason says so.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
Phase 1 is merged: ICacheWarmer, ICacheWarmingService, the options. #266 PR 3 registered CachingPatterns (450-499) and CachePatternLog;
this plan owns ids 469-477.

TASK:
Add CacheWarmingService (priority groups, Sequential and Parallel, per-warmer timeout), CachePatternLog.Warming.cs, the metrics file and the
unit tests; ask mechanical-fixer for the manifest and the changelog fragment.

KEY RULES:
- One warmer's failure, timeout or exception never stops the others; shutdown cancellation is not an error.
- Never log an EncinaError message; exceptions through ForLogging(); time from TimeProvider; no Thread.Sleep.
- EventIds 469-477 only, packed.

REFERENCE FILES:
- src/Encina.Caching/Sharding/Services/TopologyRefreshHostedService.cs
- src/Encina.Caching/Diagnostics/CachePatternLog.cs (from #266)
```

</details>

### Phase 3: Hosted service, request warmer, registration and integration (pull request 3)

<details>
<summary><strong>Tasks</strong></summary>

PR 3 holds exactly these 14 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Warming/CacheWarmingHostedService.cs` | new (internal sealed `BackgroundService`) |
| 2 | `src/Encina.Caching/Warming/RequestCacheWarmer.cs` | new (internal sealed generic `ICacheWarmer`) |
| 2a | `src/Encina.Caching/Diagnostics/CachePatternLog.Warming.cs` | modify (add 476 and 477) |
| 3 | `src/Encina.Caching/Abstractions/IWarmingRequestSource.cs` | new |
| 4 | `src/Encina.Caching/ServiceCollectionExtensions.cs` | modify (`AddEncinaCacheWarmer<TWarmer>`, `AddEncinaRequestCacheWarmer<TRequest, TSource>`, engine and hosted service registration) |
| 5 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 6 | `tests/Encina.UnitTests/Caching/CacheWarmingHostedServiceTests.cs` | new |
| 7 | `tests/Encina.UnitTests/Caching/RequestCacheWarmerTests.cs` | new |
| 8 | `tests/Encina.UnitTests/Caching/ServiceCollectionExtensionsTests.cs` | modify (`ValidateOnBuild` and `ValidateScopes`) |
| 9 | `tests/Encina.GuardTests/Infrastructure/Caching/CacheWarmingRegistrationGuardTests.cs` | new |
| 10 | `tests/Encina.IntegrationTests/Infrastructure/Caching/CacheWarmingIntegrationTests.cs` | new |
| 11 | `docs/features/query-caching.md` | modify (`docs-writer`) |
| 12 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 13 | `changelog.d/269-cache-warming.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Hosted service** (Design Choice 5 option A): `ExecuteAsync` waits for `ApplicationStarted` (through `IHostApplicationLifetime`), then calls `ICacheWarmingService.RunAsync(stoppingToken)` once when `EnableCacheWarming` and `WarmOnStartup`; any exception is caught and logged with EventId 477 and `ex.ForLogging()` (a background loop never lets an exception escape); `OperationCanceledException` of the stopping token ends it. PR 3 therefore also modifies `CachePatternLog.Warming.cs` to add 476 and 477 (PR 2 declares 469-475).
2. **`RequestCacheWarmer<TRequest>`:** `Priority` from the `[CacheWarmer]` attribute; `WarmAsync` enumerates `IWarmingRequestSource<TRequest>` up to `WarmingInstances`, and for each request creates a scope with `IServiceScopeFactory` and calls `IEncina.Send(request, ct)`. A request that would not be cached is skipped before dispatch (log 476, counted skipped, no handler run): when `EnableQueryCaching` is false, when `VaryByUser` is true (there is no user at startup, so the behavior would bypass the cache), and when the request varies by tenant (the `[Cache]` default) while `MultiTenancyMarker` is registered (Design Choice 4); a `Left` counts as failed for that request and is returned as the warmer's `Left` after the loop (the first error code); entries warmed are the `Right` results. A `[CacheWarmer]` request without `[Cache]` is rejected at registration with `InvalidOperationException` (nothing would be cached).
3. **Registration:** `AddEncinaCacheWarmer<TWarmer>()` adds `ICacheWarmer` (singleton, `TryAddEnumerable`); `AddEncinaRequestCacheWarmer<TRequest, TSource>()` registers the source and the generic warmer; `AddEncinaCaching` registers `ICacheWarmingService` (singleton) and the hosted service only when `EnableCacheWarming` is true. The DI test builds the provider with `ValidateOnBuild` and `ValidateScopes`, resolves the engine and the hosted service, and proves a registered warmer is found and that with warming disabled no hosted service exists.
4. **Tests:** the hosted service runs once after start, never throws out of `ExecuteAsync`, stops on cancellation; the request warmer caps at `WarmingInstances`, dispatches through a fake `IEncina`, skips under multi-tenancy for a tenant-varying request and warms a `VaryByTenant = false` request, rejects a request without `[Cache]`. **Guard tests:** both extension methods throw `ArgumentNullException` for a null `IServiceCollection`; `AddEncinaRequestCacheWarmer` for a type without the attribute throws. **Integration (shared `RedisCollection`):** a real `RedisCacheProvider`, a real `IEncina` and a counting handler: after `RunAsync` the first real request is served from the cache (the handler ran once, during warming). The Redis family (Valkey, Dragonfly, Garnet, KeyDB) shares `RedisCacheProvider`; Memory is covered by unit tests with `MemoryCacheProvider`; Hybrid by a unit test with a `HybridCacheProvider` built through DI; Memcached follows #277.
5. **Docs:** `docs-writer` adds a "Cache warming" section to `docs/features/query-caching.md`: the model, the strategies, the tenant rule and the readiness limit, citing tests by name.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
Phases 1 and 2 are merged: abstractions, options, CacheWarmingService.

TASK:
Add CacheWarmingHostedService, RequestCacheWarmer<TRequest>, IWarmingRequestSource<TRequest>, the registration methods and the tests of the
plan (unit, guard, Redis integration); ask mechanical-fixer for PublicAPI, manifest and changelog and docs-writer for the docs section.

KEY RULES:
- Warming runs after the host started; no exception escapes the hosted service; shutdown cancellation is not an error.
- Warm through IEncina.Send in a new scope, never by writing hand-built keys; skip tenant-varying requests under multi-tenancy (fail closed).
- The singleton hosted service resolves no scoped service: use IServiceScopeFactory.
- Registration completeness: a DI test with ValidateOnBuild and ValidateScopes.

REFERENCE FILES:
- src/Encina.Caching/Sharding/Services/TopologyRefreshHostedService.cs
- src/Encina.Caching/ServiceCollectionExtensions.cs
- tests/Encina.IntegrationTests/Infrastructure/Caching/RedisCacheProviderIntegrationTests.cs
```

</details>

### Phase 4: Coverage targets and finalization

<details>
<summary><strong>Tasks</strong></summary>

1. **No new error code** (warm failures are `Left` values of the warmers or handlers, never created here; no ADR-062 category to name). **EventIds** 469-477.
2. **Coverage targets (per flag).** Each row is entered in the manifest of the PR that adds the file and its tests (#1762): PR 1 sets the options, attribute and `ServiceCollectionExtensions.cs` copy lines; PR 2 sets the engine, the log and the metrics files; PR 3 sets the hosted service, the request warmer and `ServiceCollectionExtensions.cs`:

| File | Unit | Guard | Contract | Integration | Justification |
|------|:----:|:-----:|:--------:|:-----------:|---------------|
| `Warming/CacheWarmingService.cs` | 95 | 0 | 0 | 0 | Ordering, bounds and every outcome are unit tested with gates; internal without `InternalsVisibleTo` for the guard project |
| `Warming/CacheWarmingHostedService.cs` | 90 | 0 | 0 | 0 | Start, failure and cancellation paths are unit tested; internal |
| `Warming/RequestCacheWarmer.cs` | 95 | 0 | 0 | 40 | Cap, skip and dispatch unit tested; the integration flag runs the real fill |
| `Configuration/CachingOptions.cs` | keeps | keeps 85, raises to 90 | 0 | 0 | The new setters are exercised by `CachingOptionsWarmingGuardTests` in PR 1 |
| `ServiceCollectionExtensions.cs` | 90 | 40 | 0 | 0 | Set by PR 3 (no per-file target exists today): the DI test builds the provider with both validations and the registration guard tests cover the null-collection and the missing-attribute branches of the two new methods |
| `Attributes/CacheWarmerAttribute.cs` | 100 | 100 | 0 | 0 | Set by PR 1: `CacheWarmerAttributeTests` (unit) and the attribute cases of `CachingOptionsWarmingGuardTests` (guard) run the positive-value setter, the only logic |
| `Diagnostics/CachePatternLog.Warming.cs`, `CachePatternMetrics.Warming.cs` | 90 | 0 | 0 | 0 | Generated log bodies and counter calls run in the engine tests |
| `Abstractions/ICacheWarmer.cs`, `ICacheWarmingService.cs`, `IWarmingRequestSource.cs`, `Configuration/WarmingStrategy.cs` | 0 | 0 | 0 | 0 | Interfaces, records and an enum without executable lines of their own |

   No contract test file is written: the shipped warmers have one engine and the contract flag of the package stays at its current targets; a contract justification file is not allowed, so the manifest reason says the rules are asserted by unit tests. The worker records measured numbers in each PR and never lowers a target.
3. Verification per PR: `dotnet build --configuration Release` with zero warnings; `dotnet test` of `Encina.UnitTests` (Caching), `Encina.GuardTests` and, for PR 3, `Encina.IntegrationTests` (Redis collection); per-flag coverage and the CRAP table of changed methods.

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
| Cache warming (cold-start mitigation), the pattern named in the issue | Priorities, bounded concurrency, timeouts |
| AGENTS.md section 3 | ROP; background loops never let an exception escape; compliance gates fail closed |
| AGENTS.md section 5 (caching) | Consistent across all 8 providers |
| ADR-021 | EventId range registered before use (#266) |
| ADR-054 D4 | Tenant tag only with multi-tenancy on; the engine has no request context, so it emits none |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `QueryCachingPipelineBehavior` | `src/Encina.Caching/Behaviors/` | Fills the cache when a warmed request is dispatched |
| `TopologyRefreshHostedService` | `src/Encina.Caching/Sharding/Services/` | Hosted-service precedent |
| `MultiTenancyMarker` | `src/Encina/Abstractions/MultiTenancyMarker.cs` | The tenant rule of Design Choice 4 |
| `CachePatternLog`, `CachePatternMetrics` | `src/Encina.Caching` (from #266) | Partial log and metrics files |
| `IEncina.Send`, `IServiceScopeFactory` | core and DI | Dispatch in a new scope |
| `RedisCollection`, `RedisFixture` | `tests/Encina.IntegrationTests/Infrastructure/Caching/Fixtures/` | Integration test |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Caching` (`CachingPatterns`, registered by #266) | 469-477 | 469 warming started, 470 warmer completed (per-warmer progress), 471 warmer failed (`Left` or exception), 472 warmer timed out, 473 warming completed, 474 warming skipped (disabled or no warmers), 475 warming cancelled at shutdown (PR 2); 476 request warmer skipped (request would not be cached), 477 hosted service run failed (PR 3) |

### Estimated File Count

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | 11 | 3 | New: two interfaces and a source interface, attribute, enum, engine, hosted service, request warmer, two partial files; modified: options, DI, `PublicAPI.Unshipped.txt` |
| Tests | 6 | 3 | New: engine, hosted-service, request-warmer, options-guard, registration-guard and integration tests; modified: options, DI tests |
| Documentation | 0 | 1 | `docs/features/query-caching.md` |
| Configuration | 3 | 1 | Three changelog fragments; the manifest |
| **Total** | **20** | **8** | 3 pull requests: 13, 6 and 14 files, each at most 15 files and 1500 lines |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #269 adds cache warming to Encina.Caching. #266 owns CachePatternLog/Metrics partial
classes and the CachingPatterns EventId range 450-499; this plan uses ids 469-477.

IMPLEMENTATION OVERVIEW:
1. PR 1: ICacheWarmer, ICacheWarmingService, [CacheWarmer], WarmingStrategy, the five options.
2. PR 2: CacheWarmingService (priority groups, Sequential/Parallel, per-warmer timeout), logs, metrics.
3. PR 3: CacheWarmingHostedService, RequestCacheWarmer, IWarmingRequestSource, registration, Redis integration test, docs.

KEY PATTERNS:
- Warm through IEncina.Send in a new scope; skip tenant-varying requests under multi-tenancy; disabled by default.
- One warmer's failure never stops the others; no exception escapes the hosted service.
- Follow the maintainer's answers to Design Choices 1 to 5, given in your brief.

REFERENCE FILES:
- src/Encina.Caching/ServiceCollectionExtensions.cs
- src/Encina.Caching/Sharding/Services/TopologyRefreshHostedService.cs
- .github/coverage-manifest/Encina.Caching.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Include | Fills the cache through the existing behavior for all 8 providers. |
| 2 | OpenTelemetry | ✅ Include | Counter and duration histogram on the `Encina.Caching` meter of #266; no tenant tag (no request context in the engine). Meter registration is #723. |
| 3 | Structured Logging | ✅ Include | EventIds 469-477 in `CachingPatterns`. |
| 4 | Health Checks | ⏭️ Defer | The issue defers warming-completion reporting; follow-up issue file `artifacts/issues/cache-warming-health-and-multi-node.md`. |
| 5 | Validation | ✅ Include | Option setters and the attribute validate their values; a `[CacheWarmer]` request without `[Cache]` is rejected at registration. |
| 6 | Resilience | ✅ Include | Per-warmer timeout and isolation of failures. |
| 7 | Distributed Locks | ⏭️ Defer | Preventing duplicate warming across nodes is deferred by the issue; same follow-up issue file as health checks. |
| 8 | Transactions | ❌ N/A | Warming reads data; it never calls `SaveChangesAsync`. |
| 9 | Idempotency | ❌ N/A | Warming is idempotent by nature: it rewrites the same cache values. |
| 10 | Multi-Tenancy | ⏭️ Defer | Fail closed now (Design Choice 4); tenant-scoped warming sets are the follow-up issue file `artifacts/issues/cache-warming-tenant-scoped-sets.md`. |
| 11 | Module Isolation | ⏭️ Defer | Module-scoped warming follows the module dimension of #746 (open). |
| 12 | Audit Trail | ❌ N/A | Infrastructure with no compliance event. |

**Deferred items:** three issue files (health and multi-node, TopHeavy and AccessPattern strategies, tenant-scoped sets) and #746.

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| #266 PR 3 (`CachePatternLog`, range 450-499) and PR 4 (`CachePatternMetrics`, created there) | Pending | Both needed by PR 2, which uses the log class and the meter |
| `IEncina.Send`, `IServiceScopeFactory`, `IHostApplicationLifetime` | ✅ Available | On `origin/main` |
| Maintainer answers to Design Choices 1 to 5 | Pending | Asked when work starts |

PR 1 has no dependency on #266.

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas (Design Choices 1 to 5) when work starts, records the answers under `## Maintainer Decisions`, then briefs three workers in PR order.
2. Open the three follow-up issues of the deferred items (files under `artifacts/issues/` of the planning worktree).
