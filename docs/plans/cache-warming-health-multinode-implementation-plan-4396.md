# Implementation Plan: Cache warming readiness health check and multi-node coordination

> **Issue**: [#4396](https://github.com/dlrivada/Encina/issues/4396)
> **Type**: Feature
> **Complexity**: Medium (3 code phases, 3 pull requests of 10, 11 and 13 files)
> **Estimated Scope**: about 350 lines of production code, about 800 lines of tests
> **Milestone**: Encina 1.0 (caching category, SPEC-000 REQ-027)

---

## Summary

Issue #4396 completes two items that #269 deferred: a health check that reports whether cache warming finished, and an optional distributed lock so several nodes do not warm the same warmer at the same time. Re-checked against `origin/main` (4047d9a8a); #269 is open and its plan is merged, so none of its code exists yet (`src/Encina.Caching` has no `Warming/` folder) and this plan builds on the types that plan defines.

| Premise of the issue | State in the code |
|----------------------|-------------------|
| The check implements `IEncinaHealthCheck` and has "`DefaultName` const and `Tags` static array as in the other checks" | The contract is the abstract class `EncinaHealthCheck` (`src/Encina.Messaging/Health/EncinaHealthCheck.cs:33`), which implements `IEncinaHealthCheck` and receives name and tags in its constructor; `QueryCacheHealthCheck` (`src/Encina.Caching/Health/QueryCacheHealthCheck.cs`) has a `DefaultName` const and passes `["cache", "query-cache", "ready"]` to `base(...)`, there is no static `Tags` array. The ASP.NET registration of that check is `AddEncinaQueryCache` in `src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs:751`. |
| `ICacheWarmingService` "keeps the `CacheWarmingReport` of its last run" | It does not: the #269 plan defines `ICacheWarmingService` with a single `RunAsync` returning the report and keeps no state, so nothing says whether warming has started, is running or finished. This plan adds a status object (Design Choice 1). |
| "Unhealthy never" | `EncinaHealthCheck.CheckHealthAsync` turns any exception thrown by the check body into `Unhealthy`; "never Unhealthy" therefore holds only if the body reads an in-memory snapshot and cannot throw (Design Choice 2). |
| The lock resource is `{LockKeyPrefix}:warming:{warmer name}` | `ICacheWarmer.Name` defaults to `GetType().Name` (#269 plan, Phase 1 task 1), which for the generic `RequestCacheWarmer<TRequest, TResponse>` is the same text for every warmed request type: with that resource, the first request warmer to take the lock would make a second node skip ALL request warmers. The resource is a hash of the warmer identity instead (Design Choice 6). |
| The lock works with the Memory provider (`MemoryDistributedLockProvider`) | That provider is documented "for single-instance scenarios" (`src/Encina.Caching.Memory/MemoryDistributedLockProvider.cs:6`): two nodes never see each other's lock. The Memory cache is also per process, so a node that skips a warmer because another node holds the lock would stay cold. The lock is meaningful only when the cache is shared (Redis family, and the L2 of Hybrid). |
| "With the lock on, two nodes warm each warmer once" | The lock excludes concurrent warming, it is not a once-per-cluster guarantee: a node that starts after the holder released takes the lock and dispatches again. That second dispatch is cheap because `QueryCachingPipelineBehavior` serves an already cached entry without running the handler. The acceptance test therefore uses two concurrent runs. |
| `CachingOptions.EnableDistributedLocks` is related | It is declared and copied (`CachingOptions.cs:30`, `ServiceCollectionExtensions.cs:58`) but nothing in `src/Encina.Caching` reads it today; the warming lock does not consult it. |

The plan adds `ICacheWarmingStatus` (read-only snapshot) written by the engine, `CacheWarmingHealthCheck` plus `AddEncinaCacheWarming` for `IHealthChecksBuilder`, the option `WarmingUseDistributedLock` and the lock inside the engine. It changes no cache provider: the check and the lock are behavior-level and work with the 8 providers (Memory, Hybrid, Redis, Valkey, Dragonfly, Garnet, KeyDB and the planned Memcached of #277); the lock needs an `IDistributedLockProvider`, which `AddEncinaMemoryCache` (`ServiceCollectionExtensions.cs:67`) and `AddEncinaRedisCache` (`ServiceCollectionExtensions.cs:160`) register and the Hybrid package does not.

**Family cache-warming-followups (#4396, #4397, #4398) - ownership and order.** This plan is merged and implemented FIRST. It owns `ICacheWarmingStatus`, `CacheWarmingStatusSnapshot`, `CacheWarmingHealthCheck`, `WarmingLockResource` (the lock identity helper), the option `WarmingUseDistributedLock`, the outcome `skipped_locked` and EventIds 488-489. #4398 (tenant-scoped sets) comes second and extends `WarmingLockResource` with a tenant parameter; #4397 (strategies) comes last and touches neither the status nor the lock. All three edit `CacheWarmingService.cs`, `CachingOptions.cs`, `ServiceCollectionExtensions.cs` and `CachePatternLog.Warming.cs`, so the later plan rebases on the earlier one. #269 (PR 2 and PR 3) must be merged before PR 1 of this plan.

**Affected packages:** `Encina.Caching`, `Encina.AspNetCore` (the registration extension only). **Provider category:** Caching (8), behavior-level.

---

## Design Choices

<details>
<summary><strong>1. Where the run state lives (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A new read-only `ICacheWarmingStatus { CacheWarmingStatusSnapshot Current { get; } }` (singleton), written only by the engine through an internal class; the snapshot is an immutable record `(State, LastReport, LastRunCancelled)` with `State` in `NotStarted`, `Running`, `Completed`** | `ICacheWarmingService` of #269 stays a one-method interface; the health check reads one consistent snapshot without tearing; a test double needs no engine | One more public interface |
| **B) Add `State` and `LastReport` to `ICacheWarmingService`** | Fewer types | Every implementer or test double of the engine interface must implement state; a custom engine would have to maintain it |

### Chosen Option: **A - separate status** (recommended, pending the maintainer)

### Rationale

- The status is written by the engine and read by the health check and by applications (for example a readiness endpoint of their own); keeping the engine interface small keeps #269 unchanged. Option B wins when the maintainer prefers a single entry point and accepts the larger interface.
- Class: Dilemma

</details>

<details>
<summary><strong>2. Health status mapping (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Healthy` when warming is disabled, when `WarmOnStartup` is false and no run has happened, and after a completed run with no failed, timed-out or cancelled warmer; `Degraded` while not started (startup warming expected), while running, and after a run with a failure, a timeout or a cancellation; never `Unhealthy`. Mapping `Degraded` to an HTTP status for a readiness probe is the application's choice through `HealthCheckOptions.ResultStatusCodes`** | Matches the issue ("Unhealthy never": warming is an optimization, a failed warmer must not take a node out of rotation); simple | A probe that wants to hold traffic until warm needs the status-code mapping |
| **B) Option A plus `CacheWarmingHealthCheckOptions.NotReadyStatus` (default `Degraded`) that an application can raise to `Unhealthy` while the node is not started or running** | A readiness probe can gate traffic on the warm cache without a custom mapping | A new options type; an application can take its own nodes out of rotation, the exact thing the issue avoids by default |

### Chosen Option: **A - never Unhealthy** (recommended, pending the maintainer)

### Rationale

- The data of the result carries counts only (state, succeeded, failed, timed out, skipped, duration in seconds): no warmer name, tenant or `EncinaError.Message` (AGENTS.md section 3). The body reads the in-memory snapshot and does no I/O, so the base class never converts it to `Unhealthy`; the internal status class has no throwing member, and a theory over every snapshot asserts the result is never `Unhealthy`.
- Option B wins for deployments that gate traffic on readiness (the case #269 Design Choice 5 option B serves).
- Class: Dilemma

</details>

<details>
<summary><strong>3. A node that does not get the lock (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Skip the warmer and count it skipped (outcome `skipped_locked`, EventId 488); the lock is requested with a zero wait (the wait value belongs to this option: under option B it is `WarmingTimeout`)** | As the issue states; the node finishes quickly; no extra load | The skipping node reports `Completed` while the holder may still be warming, so the readiness signal can be early for the skipped warmers |
| **B) Wait for the lock up to `WarmingTimeout`, then run the warmer** | The node reports `Completed` only after the holder released; the run dispatches cheap cache hits; readiness stays truthful | Startup of the waiting nodes lasts as long as the slowest holder; a holder that crashed holds the lock until its expiry |

### Chosen Option: **A - skip** (recommended, pending the maintainer)

### Rationale

- The health data exposes the skipped count, so a skipped warmer is visible. Option B wins when the health check gates traffic (Design Choice 2 option B) because with A a node can be declared warm while its warmers belong to another node.
- Class: Dilemma

</details>

<details>
<summary><strong>4. Health check shape and registration</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `CacheWarmingHealthCheck : EncinaHealthCheck` in `Encina.Caching.Health` (constant `DefaultName = "encina-cache-warming"`, tags `["cache", "cache-warming", "ready"]` passed to `base`), constructor `(IServiceProvider)` like `QueryCacheHealthCheck`, and an `AddEncinaCacheWarming(this IHealthChecksBuilder, ...)` registration in `Encina.AspNetCore` next to `AddEncinaQueryCache`** | The same shape as the existing cache check and its registration | None |
| **B) Implement `Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck` directly** | No dependency on `Encina.Messaging.Health` | Breaks the convention and the `EncinaHealthCheckAdapter` that every other Encina check uses |

### Chosen Option: **A - the existing shape**

### Rationale

- **Class:** Trivial
- **Settled by:** the precedent `QueryCacheHealthCheck` (`src/Encina.Caching/Health/QueryCacheHealthCheck.cs`) and `AddEncinaQueryCache` (`src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs:751`), and the health-check rule of the plan prompt (section 3, "Health checks": `DefaultName` const, resolution through `IServiceProvider`). This entry fixes only the SHAPE (base class, `DefaultName`, tags, `IServiceProvider` constructor, registration next to `AddEncinaQueryCache`), which holds under every option of Design Choices 1 to 3. What the check reads (`ICacheWarmingStatus` under Design Choice 1 option A, the engine interface under option B) and the parameters of the registration method (a `CacheWarmingHealthCheckOptions` under Design Choice 2 option B) follow the maintainer's answers; the tasks below are written for the recommended options. It resolves its dependencies with `GetService`, so a host without warming registered gets `Healthy` ("cache warming is disabled") instead of a resolution exception.

</details>

<details>
<summary><strong>5. The lock is opt-in</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `CachingOptions.WarmingUseDistributedLock` is `false` by default** | No lock traffic and no provider requirement unless asked | A cluster must turn it on |
| **B) On by default when an `IDistributedLockProvider` is registered** | Fewer lines in the application | With the Memory provider it silently does nothing across nodes and it changes behavior without being asked |

### Chosen Option: **A - off by default**

### Rationale

- **Class:** Trivial
- **Settled by:** AGENTS.md section 1 (every feature opt-in) and section 3 (patterns optional and disabled by default); the issue states "Lock off by default". Holds under every option of Design Choices 1 to 3.

</details>

<details>
<summary><strong>6. Lock resource identity and expiry</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `{LockKeyPrefix}:warming:{hash}`, with `hash` the first 32 lowercase hex characters of the SHA-256 of the length-prefixed fields `warmer.GetType().FullName` and `warmer.Name`; expiry `WarmingTimeout`** | Injective for distinct warmers (the length prefix makes two field pairs never concatenate to the same text, and the hash is collision-resistant); generic request warmers get distinct resources; the lock providers log the raw resource and see no identifier | Not human readable in a lock browser |
| **B) `{LockKeyPrefix}:warming:{warmer.Name}` as in the issue** | Readable | `Name` defaults to `GetType().Name`, identical for every `RequestCacheWarmer<,>` and for same-named types of different namespaces: warmers would block each other |

### Chosen Option: **A - hashed identity**

### Rationale

- **Class:** Trivial
- **Settled by:** the `ICacheWarmer.Name` default in the #269 plan (Phase 1 task 1: `Name => GetType().Name`) shows that B collides, and the lock-resource hashing precedent of the #266 plan (Phase 3 task 1: SHA-256 of the key, 32 hex characters, so providers that log the resource never see an identifier). The helper is `WarmingLockResource` (internal static) so #4398 can add a tenant parameter. Nodes running different builds in a rolling deployment compute different hashes only if the type name differs; the cost is a duplicate warm, never a missed one.

</details>

<details>
<summary><strong>7. Lock provider missing or failing</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) With `WarmingUseDistributedLock` on and no `IDistributedLockProvider` registered, or a provider call that fails (a thrown exception other than the caller's cancellation today; a `Left` once ADR-052 / #2667 changes the contract), the warmer is counted failed and EventId 489 is logged with the constant error-code text `cache.warming.lock_unavailable` (and `ex.ForLogging()` for an exception, the error code for a `Left`); no warmer runs unlocked** | Never swallows the error in background infrastructure; a misconfiguration is visible in the report and the health check | The warming of that node is lost until fixed |
| **B) Log and run the warmer without the lock** | Warming still happens | Hides the misconfiguration and defeats the purpose of the option; AGENTS.md section 3 forbids reporting success after a failure |

### Chosen Option: **A - fail the warmer**

### Rationale

- **Class:** Settled
- **Settled by:** AGENTS.md section 3, "Errors are NEVER swallowed in background infrastructure": a failure of a store or provider inside a background job fails that operation, never reports success; only option A complies. The caller's cancellation (host shutdown) is not an error and never a failure count (the cancellation rule of #269 Design Choice 6). No `EncinaError` is created here (the code text is a log constant), so no ADR-062 `ErrorCategory` is assigned; if a later change surfaces it as a `Left`, it uses `ErrorCategory.Internal` (ADR-062 R1: a missing configuration entry is `Internal`; R4: a provider failure is `Internal`; R2 forbids `FailedPrecondition` before #3420) and the constant message "Cache warming lock provider is not available."
- **Lock contract, today and after #2667.** `IDistributedLockProvider.TryAcquireAsync` returns `Task<IAsyncDisposable?>` today (null means contention). ADR-052 (accepted; implementation #2667, open) changes it to `Either<EncinaError, Option<ILockHandle>>`: `Right(None)` is contention (skip), a `Left` is an infrastructure fault and counts the warmer failed with EventId 489 and the error code, never a skip. PR 3 is written for the shape on `main` when it starts; if #2667 has merged it takes the `Either` form (a prerequisite, see below), and a unit test covers `Right(Some)`, `Right(None)` and `Left`.

</details>

<details>
<summary><strong>8. The lock and the existing lock options</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The warming lock is governed only by `WarmingUseDistributedLock`; `CachingOptions.EnableDistributedLocks` is not consulted; the retry interval, used only when a wait is non-zero (Design Choice 3 option B), is `CachingOptions.DefaultLockRetry` (200 ms)** | One switch per feature; no hidden coupling | A user who set `EnableDistributedLocks = false` and `WarmingUseDistributedLock = true` gets a lock |
| **B) Require both options** | Respects a global off switch | `EnableDistributedLocks` is read nowhere today, so the global switch has no defined meaning to honor |

### Chosen Option: **A - own switch, existing retry option**

### Rationale

- **Class:** Trivial
- **Settled by:** the code: `EnableDistributedLocks` is declared and copied only (`CachingOptions.cs:30`, `ServiceCollectionExtensions.cs:58`) and gates nothing, and `DefaultLockRetry` is the existing 200 ms retry option (`CachingOptions.cs`); the XML doc of the new option states the independence. Holds under every option of Design Choices 1 to 3 (the retry is unused under Design Choice 3 option A).

</details>

---

## Implementation Phases

### Phase 1: Run status (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

PR 1 holds exactly these 10 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Abstractions/ICacheWarmingStatus.cs` | new (interface, `CacheWarmingState`, `CacheWarmingStatusSnapshot`) |
| 2 | `src/Encina.Caching/Warming/CacheWarmingStatus.cs` | new (internal sealed) |
| 3 | `src/Encina.Caching/Warming/CacheWarmingService.cs` | modify (#269 PR 2: report run start and end) |
| 4 | `src/Encina.Caching/ServiceCollectionExtensions.cs` | modify (`TryAddSingleton<CacheWarmingStatus>` and `ICacheWarmingStatus` next to the engine registration of #269 PR 3) |
| 5 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 6 | `tests/Encina.UnitTests/Caching/CacheWarmingStatusTests.cs` | new |
| 7 | `tests/Encina.UnitTests/Caching/CacheWarmingServiceTests.cs` | modify (status transitions) |
| 8 | `tests/Encina.UnitTests/Caching/ServiceCollectionExtensionsTests.cs` | modify (`ValidateOnBuild` and `ValidateScopes`: the status resolves and is the same instance the engine writes) |
| 9 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 10 | `changelog.d/4396-cache-warming-status.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Create** `enum CacheWarmingState { NotStarted = 0, Running = 1, Completed = 2 }`, `sealed record CacheWarmingStatusSnapshot(CacheWarmingState State, CacheWarmingReport? LastReport, bool LastRunCancelled)` and `interface ICacheWarmingStatus { CacheWarmingStatusSnapshot Current { get; } }` (Design Choice 1 option A). `CacheWarmingStatus` keeps an active-run counter and publishes a new immutable snapshot under one lock: `RunStarted()` increments the counter and sets `Running`; `RunFinished(CacheWarmingReport? report, bool cancelled)` decrements it and, when the counter reaches zero, sets `Completed` with the report (a `null` report means the run ended by an unexpected exception). Overlapping runs (the hosted service and a manual `RunAsync`) keep `Running` until the last one ends.
2. **Modify** the engine: `RunAsync` returns the empty report and leaves the status untouched ONLY when `EnableCacheWarming` is false (a disabled feature stays `NotStarted`). When warming is enabled it calls `RunStarted()` first, BEFORE the planning scope that resolves the warmers, and `RunFinished` in a `finally` with the report or `null` and the caller's cancellation flag; an enabled run with zero registered warmers returns the empty report and ends `Completed` with it (so a valid configuration with no warmers is `Healthy`, never `Degraded` forever).
3. **Tests:** transitions (not started, running, completed), overlapping runs, a cancelled run sets `LastRunCancelled`, an enabled run with zero warmers ends `Completed` with an empty report, a disabled engine leaves `NotStarted`, an exception thrown while resolving the warmers in the planning scope (after `RunStarted()`) ends with `Completed` and a `null` report and still decrements; the DI test resolves both types with `ValidateOnBuild` and `ValidateScopes` and proves one shared instance.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #4396 adds a readiness health check and multi-node coordination to cache warming (#269).
#269 PR 2 (CacheWarmingService) and PR 3 (registration) are merged; ICacheWarmingService has only RunAsync and keeps no state.

TASK:
Add ICacheWarmingStatus, CacheWarmingState, CacheWarmingStatusSnapshot and the internal CacheWarmingStatus; make the engine report run start and
end in a finally; register the status as a singleton shared by the engine; write the unit and DI tests; ask mechanical-fixer for
PublicAPI.Unshipped.txt, the manifest and the changelog fragment.

KEY RULES:
- The snapshot is immutable and read without tearing; a disabled or empty run does not touch the status.
- Singleton state only; no scoped service injected into a singleton; time from TimeProvider if a timestamp is added.
- XML docs with an example on every public member; zero warnings.

REFERENCE FILES:
- src/Encina.Caching/Warming/CacheWarmingService.cs (from #269)
- src/Encina.Caching/ServiceCollectionExtensions.cs
- tests/Encina.UnitTests/Caching/CacheWarmingServiceTests.cs (from #269)
```

</details>

### Phase 2: Health check and ASP.NET registration (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

PR 2 holds exactly these 11 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Health/CacheWarmingHealthCheck.cs` | new (public sealed, `EncinaHealthCheck`) |
| 2 | `src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs` | modify (`AddEncinaCacheWarming`) |
| 3 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 4 | `src/Encina.AspNetCore/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 5 | `tests/Encina.UnitTests/Caching/CacheWarmingHealthCheckTests.cs` | new |
| 6 | `tests/Encina.UnitTests/AspNetCore/Health/HealthCheckBuilderExtensionsTests.cs` | modify (registration, name, tags, failure status) |
| 7 | `tests/Encina.GuardTests/AspNetCore/AspNetCoreGuardTests.cs` | modify (`AddEncinaCacheWarming_NullBuilder_Throws`) |
| 8 | `tests/Encina.GuardTests/Infrastructure/Caching/CacheWarmingHealthCheckGuardTests.cs` | new (constructor null case) |
| 9 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 10 | `.github/coverage-manifest/Encina.AspNetCore.json` | modify (`mechanical-fixer`) |
| 11 | `changelog.d/4396-cache-warming-health-check.added.md` | new (`mechanical-fixer`) |

The documentation section of Phase 3 is added to `docs/features/handler-caching.md` in PR 3, not here.

Tasks:

1. **Create** `CacheWarmingHealthCheck(IServiceProvider serviceProvider) : EncinaHealthCheck` with `DefaultName = "encina-cache-warming"` and tags `["cache", "cache-warming", "ready"]` (the `encina` tag is added by the base). `CheckHealthCoreAsync` resolves `IOptions<CachingOptions>` and `ICacheWarmingStatus` with `GetService`, reads `Current` once and maps it per Design Choice 2 option A: disabled or no status registered gives `Healthy` ("Cache warming is disabled."); `NotStarted` with `WarmOnStartup` false gives `Healthy`; `NotStarted` with `WarmOnStartup` true, `Running`, a null report after a run, any `Failed`, `TimedOut` or a cancelled last run give `Degraded`; otherwise `Healthy`. The `data` dictionary holds `state`, `succeeded`, `failed`, `timed_out`, `skipped` and `duration_seconds` only. The method returns `Task.FromResult` (no I/O).
2. **Modify** `HealthCheckBuilderExtensions` with `AddEncinaCacheWarming(this IHealthChecksBuilder builder, string name = CacheWarmingHealthCheck.DefaultName, IEnumerable<string>? tags = null, AspNetHealthStatus? failureStatus = null)` that mirrors `AddEncinaQueryCache` (null builder throws, `CombineTags(tags, "cache", "cache-warming")`, `EncinaHealthCheckAdapter`).
3. **Tests (unit):** every state of the mapping including the `WarmOnStartup` false and disabled cases; the data keys contain no warmer name, tenant or message (assert the key set); the registration produces a check named `encina-cache-warming` with the tags and the failure status; a full `HealthCheckService` run through `AddHealthChecks().AddEncinaCacheWarming()` returns the expected `HealthStatus`. **Guard tests:** null `IServiceProvider` in the constructor, null builder in the extension; the check never returns `Unhealthy` for any snapshot (a theory over all states).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
Phase 1 is merged: ICacheWarmingStatus and the engine write the run state. QueryCacheHealthCheck (src/Encina.Caching/Health) and
AddEncinaQueryCache (src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs) are the precedents.

TASK:
Add CacheWarmingHealthCheck and AddEncinaCacheWarming; write the unit and guard tests; ask mechanical-fixer for the two PublicAPI files, the
two manifests and the changelog fragment.

KEY RULES:
- Never Unhealthy: the body reads the snapshot and does no I/O (the base class maps any exception to Unhealthy).
- The result data carries counts only; never a warmer name, a tenant id or an EncinaError.Message.
- Registration mirrors AddEncinaQueryCache; GetService, not GetRequiredService, so a host without warming is Healthy.

REFERENCE FILES:
- src/Encina.Caching/Health/QueryCacheHealthCheck.cs
- src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs
- src/Encina.Messaging/Health/EncinaHealthCheck.cs
- tests/Encina.UnitTests/AspNetCore/Health/HealthCheckBuilderExtensionsTests.cs
```

</details>

### Phase 3: Distributed lock around each warmer (pull request 3)

<details>
<summary><strong>Tasks</strong></summary>

PR 3 holds exactly these 13 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Configuration/CachingOptions.cs` | modify (`WarmingUseDistributedLock`) |
| 2 | `src/Encina.Caching/ServiceCollectionExtensions.cs` | modify (copy the option; the list at lines 53-72 is hand-written) |
| 3 | `src/Encina.Caching/Warming/WarmingLockResource.cs` | new (internal static) |
| 4 | `src/Encina.Caching/Warming/CacheWarmingService.cs` | modify (the lock around each warmer) |
| 5 | `src/Encina.Caching/Diagnostics/CachePatternLog.Warming.cs` | modify (ids 488 and 489) |
| 6 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 7 | `tests/Encina.UnitTests/Caching/CachingOptionsTests.cs` | modify (default false, copy by DI) |
| 8 | `tests/Encina.UnitTests/Caching/WarmingLockResourceTests.cs` | new |
| 9 | `tests/Encina.UnitTests/Caching/CacheWarmingServiceLockTests.cs` | new |
| 10 | `tests/Encina.IntegrationTests/Infrastructure/Caching/CacheWarmingLockIntegrationTests.cs` | new (`RedisCollection`) |
| 11 | `docs/features/handler-caching.md` | modify (`docs-writer`: health check, lock, the limits of the Memory provider and of skip) |
| 12 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 13 | `changelog.d/4396-cache-warming-distributed-lock.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Option:** `CachingOptions.WarmingUseDistributedLock` (bool, default false, XML doc naming the lock providers and the single-instance limit of Memory). A bool has no validation, so no guard test is needed for it.
2. **`WarmingLockResource.Create(string lockKeyPrefix, ICacheWarmer warmer)`** returns `$"{prefix}:warming:{hash}"`; the hashed text is `"{len}:{FullName}|{len}:{Name}"` (length-prefixed), SHA-256, lowercase hex, 32 characters (Design Choice 6).
3. **Engine:** inside the scope created for each warmer (#269 Phase 2), when the option is on: resolve `IDistributedLockProvider` with `GetService`; a null provider logs 489 once per run with the constant `cache.warming.lock_unavailable` and counts the warmer failed; otherwise `TryAcquireAsync(resource, expiry: WarmingTimeout, wait: TimeSpan.Zero, retry: DefaultLockRetry, ct)` (Design Choice 8; a zero wait still tries once: `RedisDistributedLockProvider.TryAcquireAsync` and `MemoryDistributedLockProvider.TryAcquireAsync` always attempt at least once); a `null` handle (after #2667: `Right(None)`) logs 488 (Information), counts `skipped` and the outcome `skipped_locked`, and moves on (Design Choice 3 option A); a thrown exception other than the caller's cancellation (after #2667: a `Left`, logged by error code) logs 489 with `ex.ForLogging()` and counts failed, never a skip (Design Choice 7); the handle is disposed in a `finally` after the warmer completes or its timeout fires. The lock call runs inside the linked token of the warmer so shutdown cancels it, and a cancellation is never a failure.
4. **Tests (unit, `FakeTimeProvider`):** the resource is stable for one warmer, differs for two warmers of one generic type with different closed types, differs for two types with the same `Name`, never contains the warmer name or type name (the lock providers log the resource), and every length-prefixed pair is distinct; with a fake lock provider: a held lock skips the warmer and counts `skipped_locked`, a free lock runs and disposes the handle, a throwing provider fails the warmer and the others still run (after #2667 also a provider returning `Left`, counted failed and not skipped), a missing provider fails every warmer once-logged, cancellation is not a failure, the option off never touches the provider. **Integration (shared `RedisCollection`):** two engines with a real `RedisDistributedLockProvider` and one counting handler start concurrently; the warmer's handler runs once (the second engine reports skipped), and with the option off both dispatch (the second dispatch is a cache hit). Valkey, Dragonfly, Garnet and KeyDB share `RedisDistributedLockProvider` and are covered by it; Memory and Hybrid are covered by unit tests; Memcached follows #277.
5. **Docs:** the issue asks for a section of `docs/features/query-caching.md`, which documents the EF Core `QueryCacheInterceptor`; the `[Cache]` handler page is `docs/features/handler-caching.md` (#269 Phase 3 explains), so this plan deliberately deviates from the issue's file name. `docs-writer` adds the sections "Readiness" and "Running on several nodes" there, stating the limits found above (Memory lock is single-instance, the lock excludes concurrency and is not once per cluster, a skipped warmer can leave a node declared warm).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
Phases 1 and 2 are merged: the status and the health check exist. #269 PR 2 and PR 3 are merged: CacheWarmingService runs each warmer in its own
scope under a linked timeout token. EventIds 488-489 of CachingPatterns (450-499, registered by #266) are reserved for this issue.

TASK:
Add the option WarmingUseDistributedLock, WarmingLockResource and the lock around each warmer in the engine, ids 488 and 489 in CachePatternLog.Warming.cs,
the unit tests and the Redis integration test; ask mechanical-fixer for PublicAPI, manifest and changelog and docs-writer for the docs sections.

KEY RULES:
- A lock failure fails the warmer (never runs unlocked); a null handle skips it; the caller's cancellation is neither.
- The resource is a hash of the warmer identity, never the name; TryAcquireAsync with wait zero; the handle is disposed in a finally.
- Log by error code or ex.ForLogging(), never an EncinaError message; EventIds 488-489 only, packed.

REFERENCE FILES:
- src/Encina.Caching/Warming/CacheWarmingService.cs (from #269)
- src/Encina.DistributedLock/IDistributedLockProvider.cs
- src/Encina.Caching.Redis/RedisDistributedLockProvider.cs
- tests/Encina.IntegrationTests/Infrastructure/Caching/RedisCacheProviderIntegrationTests.cs
```

</details>

### Phase 4: Coverage targets and finalization

<details>
<summary><strong>Tasks</strong></summary>

1. **No new `EncinaError`** (the failures are counts and log constants): no ADR-062 category applies today; Design Choice 7 names the category a future `Left` must use. **EventIds** 488-489 only. **Telemetry:** the outcome `skipped_locked` on `encina.cache.warming` (#269); no tenant tag, because the engine runs without a request context (ADR-054 D4; #4398 adds the tag per pair).
2. **Coverage targets (per flag).** Each row is entered in the manifest of the PR that adds the file and its tests (#1762). Numbers quoted from `origin/main`: `Behaviors/QueryCachingPipelineBehavior.cs` unit 98, guard 20, contract 0 (not touched here); `Configuration/CachingOptions.cs` guard 85 and no other flag; `ServiceCollectionExtensions.cs` and `Health/QueryCacheHealthCheck.cs` have no `targets` row in `Encina.Caching.json` on `main`; `Health/HealthCheckBuilderExtensions.cs` has no `targets` row in `Encina.AspNetCore.json`.

| File | Unit | Guard | Contract | Integration | Justification |
|------|:----:|:-----:|:--------:|:-----------:|---------------|
| `Warming/CacheWarmingStatus.cs` (PR 1) | 95 | 0 | 0 | 0 | Counter and snapshot transitions are unit tested; internal without `InternalsVisibleTo` for `Encina.GuardTests` (#269 Phase 4) |
| `Abstractions/ICacheWarmingStatus.cs` (PR 1) | 0 | 0 | 0 | 0 | Interface, enum and record: no executable lines of their own |
| `Health/CacheWarmingHealthCheck.cs` (PR 2) | 95 | 15 | 0 | 0 | The mapping is a handful of branches, all unit tested; the guard flag reaches the one constructor null check (about 1 of 12 executable lines, rounded up to the share of argument checks) |
| `Health/HealthCheckBuilderExtensions.cs` (AspNetCore, PR 2) | 10 | 10 | 0 | 0 | No `targets` row exists today in a file of 16+ extension methods already run by `HealthCheckBuilderExtensionsTests` and `AspNetCoreGuardTests`; the worker first measures the baseline with `coverage-report.cs` and enters `max(10, measured whole-file value rounded down)` per flag, so the new method (about 4% of the executable lines) can never leave the target unmet |
| `Warming/WarmingLockResource.cs` (PR 3) | 100 | 0 | 0 | 0 | One pure function, fully unit tested; internal |
| `Warming/CacheWarmingService.cs` (PR 3) | keeps 95 (#269) | 0 | 0 | raise to 35 | The lock paths are unit tested with a fake provider; the integration flag adds the real Redis lock (the #269 integration test already runs the engine path) |
| `Diagnostics/CachePatternLog.Warming.cs` (PR 3) | keeps 90 | 0 | 0 | 0 | Ids 488-489 run in the lock tests |
| `Configuration/CachingOptions.cs` (PR 3) | no row | 90 (85 on `main` today, raised to 90 by #269 PR 1) | 0 | 0 | A bool property with no validation: no new guard surface; the target stays 90 |
| `ServiceCollectionExtensions.cs` (PR 1 and PR 3) | 90 (no row on `main` today; #269 Phase 4 sets unit 90 and guard 0 in its PR 1) | 0 | 0 | 0 | One more copy line and one more singleton, run by the DI tests with both validations; the numbers are those #269 sets and are never lowered (`target-ratchet`) |

   No contract test is written: there is one status implementation and one check, and the contract flag of the package stays at its targets (a justification file is not allowed for contract tests; the manifest reason says the rules are asserted by unit tests).
3. Verification per PR: `dotnet build --configuration Release` with zero warnings; `dotnet test` of `Encina.UnitTests` (Caching, AspNetCore health), `Encina.GuardTests` and, for PR 3, `Encina.IntegrationTests` (Redis collection); per-flag coverage and the CRAP table of changed methods (`CheckHealthCoreAsync` and the engine's lock block stay at or below 10: keep the mapping a small table or two helpers).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
The three code PRs are merged; each carried its own per-flag targets.

TASK:
Verify with coverage-report.cs that each PR's measured numbers reach the targets above; fix any gap through mechanical-fixer in a small PR; run
--check-justifications and the crap-gate table of changed methods.

KEY RULES:
- Never lower an existing target (target-ratchet); no log, metric or error code beyond the plan.

REFERENCE FILES:
- .github/coverage-manifest/Encina.Caching.json
- .github/coverage-manifest/Encina.AspNetCore.json
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| AGENTS.md section 3 | Errors never swallowed in background infrastructure (Design Choice 7); background loops never let an exception escape; opt-in features |
| AGENTS.md section 5 (caching, distributed locks) | The check and the lock behave the same on all 8 cache providers; the lock needs one of the 7 lock providers of SPEC-000 DEC-009 |
| ASP.NET Core health checks (`HealthStatus`, `HealthCheckOptions.ResultStatusCodes`) | `Degraded` maps to HTTP 200 unless the application maps it (Design Choice 2) |
| ADR-054 D4 | Tenant tag only with multi-tenancy on; none here (no request context) |
| ADR-062 | No new `EncinaError`; the category a future `Left` must use is recorded in Design Choice 7 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `EncinaHealthCheck`, `EncinaHealthCheckAdapter` | `src/Encina.Messaging/Health/EncinaHealthCheck.cs`, `src/Encina.AspNetCore/Health/EncinaHealthCheckAdapter.cs` | Base class and ASP.NET adapter of the new check |
| `QueryCacheHealthCheck`, `AddEncinaQueryCache` | `src/Encina.Caching/Health/`, `src/Encina.AspNetCore/Health/HealthCheckBuilderExtensions.cs:751` | Shape and registration precedent |
| `IDistributedLockProvider` | `src/Encina.DistributedLock/IDistributedLockProvider.cs` (`Encina.Caching` already references the project) | The lock |
| `RedisDistributedLockProvider`, `MemoryDistributedLockProvider` | `src/Encina.Caching.Redis/`, `src/Encina.Caching.Memory/` | Integration test and the single-instance limit |
| `CacheWarmingService`, `CachePatternLog`, `CachePatternMetrics` | `src/Encina.Caching` (from #269 and #266) | Engine, ids 488-489, outcome tag |
| `RedisCollection`, `RedisFixture` | `tests/Encina.IntegrationTests/Infrastructure/Caching/Fixtures/` | Two-node test |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Caching` (`CachingPatterns` 450-499, registered by #266 PR 3 and listed in the plan batch registry as `cache-patterns-1`) | 488-489 | 488 warmer skipped because another node holds the lock; 489 lock unavailable (missing provider or provider exception). The family uses #266 450-455, #267 456-461, #268 462-468, #269 469-477, #271 478-487; 488-499 were free when this plan was written. Within the follow-up family: #4396 488-489, #4398 490-493, #4397 494-495; 496-499 stay free. No new range is created |

### Estimated File Count

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | 4 | 11 | New: status interface file, status class, health check, `WarmingLockResource`; modified (counted once per PR that edits it): engine (PR 1, PR 3), DI (PR 1, PR 3), options, AspNetCore extensions, log file, `PublicAPI.Unshipped.txt` (Caching in PR 1, PR 2 and PR 3; AspNetCore in PR 2) |
| Tests | 6 | 5 | New: status, health, health guard, lock-resource, lock-engine and integration tests; modified: engine, DI, options, health-builder and AspNetCore guard tests |
| Documentation | 0 | 1 | `docs/features/handler-caching.md` (created by #269 PR 3) |
| Configuration | 3 | 4 | Three changelog fragments; manifest rows: Caching in PR 1, PR 2 and PR 3, AspNetCore in PR 2 |
| **Total** | **13** | **21** | Counted per pull request entry: 34 entries in 3 pull requests of 10, 11 and 13 files, each at most 15 files and 1500 lines |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #4396 adds a readiness health check and a distributed lock around each warmer to cache warming
(#269). #266 owns CachePatternLog and the CachingPatterns range 450-499; this plan uses ids 488-489.

IMPLEMENTATION OVERVIEW:
1. PR 1: ICacheWarmingStatus (immutable snapshot) written by the engine, registered as a singleton.
2. PR 2: CacheWarmingHealthCheck (never Unhealthy) and AddEncinaCacheWarming in Encina.AspNetCore.
3. PR 3: WarmingUseDistributedLock, WarmingLockResource (hashed identity) and the lock in the engine; Redis integration test; docs.

KEY PATTERNS:
- A lock failure fails the warmer; a held lock skips it; cancellation is neither; the lock resource never contains a name.
- Health data carries counts only; the check reads memory and never throws.
- Follow the maintainer's answers to Design Choices 1 to 3, given in your brief; Design Choices 4 to 7 are decided.

REFERENCE FILES:
- src/Encina.Caching/Health/QueryCacheHealthCheck.cs
- src/Encina.Caching/Warming/CacheWarmingService.cs (from #269)
- src/Encina.DistributedLock/IDistributedLockProvider.cs
- .github/coverage-manifest/Encina.Caching.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Include | Extends cache warming; behavior-level for all 8 providers; the lock is meaningful only for a shared cache (Redis family, Hybrid L2). |
| 2 | OpenTelemetry | ✅ Include | The outcome `skipped_locked` on `encina.cache.warming` (#269 meter); no tenant tag (no request context, ADR-054 D4); meter registration is #723. |
| 3 | Structured Logging | ✅ Include | EventIds 488-489 in `CachingPatterns`; the lock resource is hashed so the providers' own logs carry no identifier. |
| 4 | Health Checks | ✅ Include | The point of the issue: `CacheWarmingHealthCheck` and `AddEncinaCacheWarming`. The Memory and Hybrid provider checks are #754. |
| 5 | Validation | ❌ N/A | A bool option and a registration parameter: no input to validate. |
| 6 | Resilience | ✅ Include | Lock request without waiting (Design Choice 3 option A), the lock expiry equals `WarmingTimeout`, a provider failure fails only that warmer. |
| 7 | Distributed Locks | ✅ Include | The second half of the issue; limits stated for the Memory provider. |
| 8 | Transactions | ❌ N/A | Cache operations are independent; nothing here calls `SaveChangesAsync`. |
| 9 | Idempotency | ❌ N/A | Warming is idempotent: a second dispatch is served from the cache. |
| 10 | Multi-Tenancy | ⏭️ Defer | The lock and the status are tenant-independent; per-tenant warming sets are #4398 (open). |
| 11 | Module Isolation | ⏭️ Defer | Follows the module dimension of #746 (open). |
| 12 | Audit Trail | ❌ N/A | Infrastructure with no compliance event. |

**Deferred items:** tenant-scoped warming (#4398, open), module-scoped warming (#746, open).

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| #269 PR 2 (engine, `CacheWarmingReport`, `CachePatternLog.Warming.cs`) and PR 3 (registration, hosted service, `docs/features/handler-caching.md`) | Pending | PR 1 of this plan edits the engine and the registration; PR 3 appends to the docs page |
| #2667 (ADR-052: `TryAcquireAsync` returns `Either<EncinaError, Option<ILockHandle>>`, open) | Pending, prerequisite for PR 3 | PR 3 is written for the contract on `main` when it starts; if #2667 has not merged, the worker uses the null-handle shape and a follow-up on #2667 adds the `Left` branch; the plan's `Left` tests are mandatory once it has merged |
| #266 PR 3 (`CachePatternLog`, range 450-499) | Pending | Needed for the ids of PR 3 |
| `IDistributedLockProvider` with Redis and Memory implementations | ✅ Available | On `origin/main` |
| `EncinaHealthCheck`, `AddEncinaQueryCache` | ✅ Available | On `origin/main` |
| Maintainer answers to Design Choices 1 to 3 | Pending | Asked when work starts |

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas (Design Choices 1 to 3) when work starts, records the answers under `## Maintainer Decisions`, then briefs three workers in PR order after #269 is merged.
2. Implement this plan before #4398 and #4397 (see "Family cache-warming-followups" in the Summary).
