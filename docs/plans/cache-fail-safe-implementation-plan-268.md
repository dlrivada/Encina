# Implementation Plan: Fail-safe cache (serve stale on failure, soft and hard timeouts, throttle)

> **Issue**: [#268](https://github.com/dlrivada/Encina/issues/268)
> **Type**: Feature
> **Complexity**: Medium (3 code phases and a prerequisite PR 0, pull requests of 4, 15, 13 and 7 files)
> **Estimated Scope**: about 500 lines of production code, about 1,100 lines of tests
> **Milestone**: v0.18.0 — Integration & Platform Capabilities

---

## Summary

Issue #268 asks that a `[Cache]` request serves a stale entry when the handler fails or is slow: `FailSafeDurationSeconds` (how long a stale entry may be served), `SoftTimeoutMs` (serve stale if the handler is slower), `HardTimeoutMs` (stop waiting for the handler) and `FailSafeThrottleDurationSeconds` (do not call the handler again for a while after a failure), with metrics. Re-checked against `origin/main` (8727c5cc3):

| Premise of the issue | State in the code |
|----------------------|-------------------|
| `CachePipelineBehavior` catches the factory exception and serves `entry.Value` | The class is `QueryCachingPipelineBehavior` (`src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs`). It does not catch handler exceptions, and a handler failure is normally a `Left` (ROP), not an exception: the issue's `catch (Exception)` sketch covers only half of the failure shapes. |
| Stale data is served for any failure | Serving stale data for a `Left` of category `Forbidden`, `Unauthenticated` or `NotFound` would hide a definitive answer (Design Choice 1). `EncinaErrors.GetCategory` exists (`src/Encina/Results/EncinaErrors.cs`, line 211; ADR-062). |
| The behavior can cancel the factory at `HardTimeoutMs` | `RequestHandlerCallback<TResponse>` takes no token (`src/Encina/Abstractions/RequestHandlerDelegate.cs`, line 9) and is bound to the request scope, so through `nextStep()` the behavior can stop waiting but cannot cancel the handler or keep it running after the response; real cancellation needs an owned, awaited re-dispatch (Design Choice 3). |
| `CacheEntry<T>` can get `ExpiresAtUtc`, `FailSafeExpiresAtUtc`, `LastFailureAtUtc` | The entry is internal with `Value` and `CachedAtUtc` only (line 353); `ExpiresAtUtc` comes from #266 PR 1; this plan adds `FailSafeExpiresAtUtc`; `LastFailureAtUtc` becomes an in-process tracker under the recommended Design Choice 4 (a rewrite of the entry races with invalidation). The provider TTL is the physical lifetime, so a stale entry survives only if the store TTL is the fail-safe lifetime. |
| Replacing an entry is safe on every provider | `MemoryCacheProvider.SetAsync` adds the key to its tracker and registers a post-eviction callback; when a key is replaced, the old entry's callback removes the key from the tracker, so the new entry stays readable but invisible to `RemoveByPatternAsync` (`MemoryCacheProvider.cs`, lines 79-90). Fail-safe makes replacing a live entry the normal case, so a PR 0 fixes the tracker first (Phase 1). |
| The issue lists no option class members | Only attribute properties are listed; this plan adds no `CachingOptions` member. |

The plan stores entries with a physical TTL equal to the fail-safe lifetime and a logical expiry (`ExpiresAtUtc`); on a stale hit it runs the miss path and falls back to the stale value for eligible failures, applies the throttle through `LastFailureAtUtc`, and adds the two timeouts. All 8 providers (Memory, Hybrid, Redis, Valkey, Dragonfly, Garnet, KeyDB, and the planned Memcached of #277) are served by the behavior through `SetAsync` with a longer TTL; none changes.

**Shared pieces (family cache-patterns-1):** `ExpiresAtUtc`, `CachePatternSettings`, `CachePatternLog`, `CachePatternMetrics`, the `CachingPatterns` range (450-499) and the request-flow contract are owned by #266. This plan owns `FailSafeExpiresAtUtc`, `LastFailureAtUtc`, `FailSafePolicy` and EventIds 462-468. The soft timeout reuses `IBackgroundCacheRefresher` of #267.

**Affected packages:** `Encina.Caching`. **Provider category:** Caching (8), behavior-level.

---

## Design Choices

<details>
<summary><strong>1. Which failures serve stale data (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Stale data only for explicitly identified transient failures: a `Left` of category `Unavailable`, `Timeout`, `UpstreamFailure` or `RateLimited`, or a thrown `TimeoutException`, `HttpRequestException`, `IOException`, `SocketException` or `System.Data.Common.DbException`; everything else fails closed: any other `Left` (including `Internal`, which is the category an uncategorized error such as `dsr.restriction_active` or `abac.indeterminate` maps to), any other exception (including `UnauthorizedAccessException`) and any cancellation is returned or rethrown unchanged; a non-transient `Left` also stops the stale entry from being served on a later failure, by the mechanism Design Choice 4 chooses (a mark in the in-process tracker under its option A, a removal of the entry under its option B)** | An access denial, a deletion or a permission change is never masked by old data; an unknown failure never serves stale | A handler that reports an outage as an uncategorized error gets no fail-safe (it must use the right category) |
| **B) Any `Left` or exception serves stale data (the issue's sketch)** | Simple | A permission change or a deletion is hidden for up to the fail-safe lifetime, which is a security and correctness defect |

### Chosen Option: **A - infrastructure categories only** (recommended, pending the maintainer)

### Rationale

- ADR-062 defines the category contract; `GetCategory()` returns `Internal` for an uncategorized error, so `Internal` cannot mean "transient" and is excluded. Cancellation is checked first: `PipelineBuilder.ExecuteHandlerAsync` turns a caller cancellation into an uncategorized `encina.handler.cancelled` `Left`, so before looking at any `Left` the behavior tests the caller's token (`cancellationToken.IsCancellationRequested`) and that code, and returns the `Left` unchanged; a test goes through the real pipeline wrapper, and further tests use the real denial factories (`dsr.restriction_active`, `abac.indeterminate`, `Forbidden`) and assert stale is never served.
- Option B wins when every handler in the application reports all failures uniformly and the maintainer wants no category dependence.
- Class: Dilemma

</details>

<details>
<summary><strong>2. What `FailSafeDurationSeconds` measures (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Total age: a stale entry may be served until `CachedAtUtc + FailSafeDurationSeconds` (the issue's example: "serve stale up to 3600s old"); it must be at least `DurationSeconds`** | Matches the issue text; one number to reason about | A fail-safe value below the duration is an error |
| **B) Extra time after expiry: stale until `ExpiresAtUtc + FailSafeDurationSeconds`** | The name reads as "extra" | Contradicts the issue's example ("after 3600s no stale data is available") |

### Chosen Option: **A - total age** (recommended, pending the maintainer)

### Rationale

- The provider TTL becomes `max(FailSafeDurationSeconds, jittered Duration)` (jitter from #266 applies to the logical expiry).
- Option B wins if the maintainer reads "fail-safe duration" as an additional window.
- Class: Dilemma

</details>

<details>
<summary><strong>3. Soft and hard timeouts (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Race `nextStep()` against the timers and abandon the slow run; at the soft timeout with a stale entry serve it and hand a background refresh to `IBackgroundCacheRefresher` (#267); at the hard timeout serve stale if present else return `Left` `cache.hard_timeout`** | No re-entry of the pipeline; reuses the #267 worker | The hard timeout stops waiting but does not cancel the handler (the issue requires cancelling the factory); the abandoned `nextStep()` keeps running after the request ends and its scope is disposed, which is the very hazard #267 Design Choice 1 uses to reject capturing `nextStep()`; observing its fault (468) neither cancels database work nor prevents side effects |
| **B) Run the handler through a re-dispatch of the request via `IEncina` in the same scope with a token owned by the behavior (a linked source cancelled at the hard timeout); the behavior always awaits that run, cancelled or complete, before returning, so the scope stays valid until the handler has finished; the soft timeout cancels and awaits the run, serves the stale entry and hands the refresh to #267's refresher, which re-dispatches in its own new scope** | The hard timeout really cancels the factory; no run outlives its scope; no abandoned run | The pipeline behaviors outside the cache behavior run once and the inner dispatch runs the inner behaviors once more (a marker of #267 makes the cache behavior run its miss path without scheduling); a soft timeout restarts the work in the refresher; `IEncina` is resolved lazily from the request-scope `IServiceProvider` to avoid a constructor cycle |
| **C) Ship the failure fallback and the throttle now; timeouts later in their own issue** | Smallest first release | Two acceptance criteria of the issue ("Soft and hard timeouts work correctly") are not met |

### Chosen Option: **B - owned, awaited re-dispatch** (recommended, pending the maintainer)

### Rationale

- The behavior cannot cancel `nextStep()` or let it outlive its scope (`RequestHandlerCallback` has no token), so only B meets "cancel factory execution at the hard timeout" with a scope that stays valid; A only stops waiting and leaves an abandoned run on a scope that is being disposed.
- Soft timeout is only taken when a safe refresh exists: for a `VaryByUser` request (not eligible for #267's refresh) or with no refresher registered, the soft timeout is inert (the behavior waits for the handler up to the hard timeout), because serving stale and discarding a slow success would never refresh the entry; a test repeats slow successes with and without a refresher.
- The new error is created as `EncinaErrors.Create("cache.hard_timeout", category: ErrorCategory.Timeout, message: ...)` with a constant message: ADR-062 category `Timeout`. No exception message is copied.
- Option A wins where pipeline re-entry is unacceptable and a stop-waiting timeout is enough (the maintainer then accepts the abandoned run and records it); option C wins to deliver the failure fallback first, with no timeouts and no abandoned run.
- Whole-entry access: the stale path needs `ExpiresAtUtc` and `FailSafeExpiresAtUtc` of the entry read, so it relies on #266 PR 1 (`TryGetForCallerAsync` returns `(Found, Entry)`); this plan does not change that method.
- Class: Dilemma

</details>

<details>
<summary><strong>4. Where the throttle state lives (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) In-process tracker (internal `FailSafeTracker`: a bounded dictionary of cache key to last failure time and "answered definitively" marks, each expiring with `TimeProvider` at the entry's `FailSafeExpiresAtUtc`; capacity 10,000 keys, the oldest dropped when full); the cache entry is never rewritten or removed by the fail-safe path** | No read-modify-write: no restoring of an invalidated value, no overwriting of a concurrent refresh, no deleting of a newer entry; nothing to coordinate with `InvalidatesCache` | Each node throttles and suppresses stale on its own; lost on restart; a node that has not seen the definitive answer may serve stale once |
| **B) In the entry (`LastFailureAtUtc`, rewritten on a failure with the remaining physical TTL; stale entry removed on a definitive answer)** | Shared by every node through the cache; the issue's own design | Unconditional writes and removals race with invalidation and with refreshes (`ICacheProvider` has no compare-and-set), so they can restore an invalidated value or delete a newer entry |

### Chosen Option: **A - in-process tracker** (recommended, pending the maintainer)

### Rationale

- `ICacheProvider` offers no version-checked write, so any rewrite of the stale snapshot is racy; the tracker removes the need. Option B wins when cross-node throttling matters more than the race, and the maintainer then also chooses the coordination (for example a lock per key).
- Only a successful store ever writes the entry (a fresh value replaces the old one), so the Memory provider's replacement-safe key tracking (PR 0) is the one provider prerequisite.
- Class: Dilemma

</details>

<details>
<summary><strong>5. Invalid or unsupported combinations (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Throw `InvalidOperationException` at resolution (as #266 Design Choice 7 recommends) for: `FailSafeDurationSeconds` below `DurationSeconds`, a negative value, a soft timeout not below the hard timeout, fail-safe with `SlidingExpiration = true`; a soft or hard timeout set without `FailSafeDurationSeconds` is valid, as in the issue's own second example: the hard timeout then returns `cache.hard_timeout` (no stale entry exists) and the soft timeout is inert because there is never a stale entry to serve (documented)** | Deterministic, one place | The first request of the type fails |
| **B) Ignore the unsupported setting and log a Warning once** | The request works | Silent loss of protection |

### Chosen Option: **A - throw at resolution** (recommended, pending the maintainer)

### Rationale

- A sliding window has no fixed age to measure staleness against, so the combination has no defined meaning. Option B wins when a package must never fail a request over a setting.
- Class: Dilemma

</details>

<details>
<summary><strong>6. Reading stale entries is no new access path</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A stale entry is read and served only after the existing bypass checks (caching disabled, `VaryByUser` without a user, multi-tenancy on with no tenant) and only through the same tenant- and user-qualified key** | No new isolation risk; stale data is never visible to another tenant or user | None |
| **B) A separate stale key space** | Independent TTL | A second key to qualify, escape and invalidate (`InvalidatesCache` would miss it) |

### Chosen Option: **A - same key, same checks**

### Rationale

- **Class:** Trivial
- **Settled by:** the existing order in `QueryCachingPipelineBehavior.Handle` (`ShouldUseCache` at line 103 precedes key generation and every read) and AGENTS.md section 3 (gates fail closed when context is missing). This holds under every option of Design Choices 1 to 5, which change what is served, not whether the checks run. Stale serving adds no access path beyond the existing cache hit, which already returns a cached value without running the behaviors inside `nextStep()`; therefore a security or compliance gate (authorization, `ProcessingRestrictionPipelineBehavior`) must be registered OUTSIDE (before) the cache behavior for hits and stale hits alike. `Encina.Caching` declares no pipeline stage today, so this ordering is a documented prerequisite of the docs section of PR 1 and is tested with a gate registered before the cache behavior that denies while a stale entry exists (the denial is returned and the cache is never read); declaring a stage that enforces it is the follow-up issue file `artifacts/issues/cache-gate-before-cache-pipeline-stage.md`.

</details>

---

## Implementation Phases

### Phase 1: Retention, failure fallback and throttle (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

**PR 0 (prerequisite, 4 files, merged before PR 1):** `src/Encina.Caching.Memory/MemoryCacheProvider.cs` (the post-eviction callback removes the key from `_keyTracker` only when the eviction reason is not `Replaced`, or when the evicted entry is the tracked one, in `SetAsync`, `GetOrSetAsync` and `SetWithSlidingExpirationAsync`), `tests/Encina.UnitTests/Caching/Memory/MemoryCacheProviderTests.cs` (overwrite a key, then `RemoveByPatternAsync` removes the new entry; overwrite then expire the old one still tracks the new one), `.github/coverage-manifest/Encina.Caching.Memory.json` (`mechanical-fixer`, existing unit 95 kept) and `changelog.d/268-cache-memory-replace-tracking.fixed.md` (`mechanical-fixer`). No public API changes and no new EventId.

PR 1 holds exactly these 15 files (the metrics, the docs page and the Redis retention test move to PR 3; the tracker is public so it can be a parameter of the public behavior constructor, and is wired by DI):

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Attributes/CacheAttribute.cs` | modify (`FailSafeDurationSeconds`, `FailSafeThrottleDurationSeconds`) |
| 2 | `src/Encina.Caching/Patterns/FailSafeTracker.cs` | new (public sealed; the optional trailing constructor parameter `FailSafeTracker? failSafeTracker = null` of the behavior; null means no throttle and no definitive-answer marks, documented) |
| 3 | `src/Encina.Caching/Patterns/CachePatternSettings.cs` | modify (fail-safe fields and validation; owned by #266) |
| 4 | `src/Encina.Caching/Patterns/FailSafePolicy.cs` | new (internal static) |
| 5 | `src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs` | modify (envelope field, physical TTL, stale path, new optional parameter) |
| 6 | `src/Encina.Caching/ServiceCollectionExtensions.cs` | modify (`TryAddSingleton<FailSafeTracker>()`) |
| 7 | `src/Encina.Caching/Diagnostics/CachePatternLog.FailSafe.cs` | new (ids 462, 465, 466) |
| 8 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`: the tracker and the changed constructor line) |
| 9 | `tests/Encina.UnitTests/Caching/FailSafeTests.cs` | new (policy and tracker) |
| 10 | `tests/Encina.UnitTests/Caching/QueryCachingFailSafeTests.cs` | new |
| 11 | `tests/Encina.UnitTests/Caching/CachePatternSettingsTests.cs` | modify |
| 12 | `tests/Encina.UnitTests/Caching/ServiceCollectionExtensionsTests.cs` | modify (`ValidateOnBuild` and `ValidateScopes`; one tracker instance) |
| 13 | `tests/Encina.GuardTests/Infrastructure/Caching/QueryCachingPipelineBehaviorGuardTests.cs` | modify (valid-result test for a null `failSafeTracker`) |
| 14 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 15 | `changelog.d/268-cache-fail-safe.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Create** `internal static class FailSafePolicy`: `bool IsStale(DateTime nowUtc, DateTime expiresAtUtc)`; `bool CanServeStale(DateTime nowUtc, DateTime? failSafeExpiresAtUtc)`; `bool IsTransientFailure(EncinaError error)` (Design Choice 1: `error.GetCategory()` in `Unavailable`, `Timeout`, `UpstreamFailure`, `RateLimited`, and never the `encina.handler.cancelled` code) and `bool IsTransientException(Exception exception)` (the listed exception types only); `bool IsThrottled(DateTime nowUtc, DateTime? lastFailureAtUtc, TimeSpan throttle)`; `FailSafeTracker` (file 2, public sealed, a singleton) holds the per-key last failure and "answered definitively" marks of Design Choice 4; `TimeSpan PhysicalTtl(TimeSpan failSafe, TimeSpan logicalDuration)`.
2. **Modify** `CacheAttribute` (`FailSafeDurationSeconds` 0 disables, `FailSafeThrottleDurationSeconds` 0 disables) and `CachePatternSettings.Resolve` (validation of Design Choice 5).
3. **Modify** the behavior. Store: when fail-safe is on, `SetAsync` receives `PhysicalTtl`, and the entry holds `ExpiresAtUtc` (logical) and `FailSafeExpiresAtUtc = CachedAtUtc + FailSafeDurationSeconds`. Read: this PR uses the entry that `TryGetFromCacheAsync` and `TryGetForCallerAsync` return since #266 PR 1 (it does not change those methods). Fresh (`now < ExpiresAtUtc`): served as today. Stale and fail-safe on: if throttled, serve it (log 465); else run the miss path (single-flight of #266 when its mode is on, so the stale decision is made once per flight and shared): on `Right`, store and return; before looking at any outcome the behavior checks the caller's token and the `encina.handler.cancelled` code and returns the cancellation unchanged; on a transient failure and `CanServeStale`, and only when the key is not marked "answered definitively", serve the stale value, log 462 with the error code or `ex.ForLogging()`, and record the failure time in the `FailSafeTracker` (no cache write); on any other `Left` or exception, mark the key "answered definitively" in the tracker until the entry's `FailSafeExpiresAtUtc` (no cache removal) and return the `Left` unchanged (the exception is rethrown); when stale data is no longer allowed, log 466 and return the failure unchanged. For a `VaryByUser` request the identity check `IsSameCaller` runs again immediately before every delayed stale return (after the handler or timer wait), and a failed check runs no stale serve and no tracker write (a test ends the identity scope during the wait). An entry without `ExpiresAtUtc` (a sliding entry) is fresh. Only a successful store writes the entry.
4. **EventIds (packed, from #266's block):** 462 stale served on failure, 463 stale served on soft timeout, 464 hard timeout elapsed, 465 throttle active, 466 fail-safe exhausted, 467 abandoned run completed, 468 abandoned run faulted. PR 1 emits 462, 465, 466; PR 2 emits 463 and 464; 467 and 468 exist only if the maintainer answers Design Choice 3 with option A (abandoned runs are observed), and are released to the family otherwise. In PR 3, counter `encina.cache.failsafe` with tag `outcome` (`stale_on_failure`, `stale_on_soft_timeout`, `hard_timeout`, `throttled`, `exhausted`) and the tag `encina.tenant_id` only with multi-tenancy on and a tenant present (ADR-054 D4). Logs carry the error code or exception type and the redacted key, never a message.
5. **Tests (unit):** fresh hit unchanged; with fail-safe off every existing behavior test passes; stale entry plus `Left` of each of the four transient categories serves stale, `Forbidden` / `Unauthenticated` / `NotFound` / `Validation` / `Internal` and the real denial factories (`dsr.restriction_active`, `abac.indeterminate`) return the `Left` and the key is marked, so a later failure does not serve the old entry; a listed transient exception serves stale and `UnauthorizedAccessException` is rethrown; a caller cancellation, through the real pipeline wrapper that turns it into `encina.handler.cancelled`, propagates; a gate registered before the cache behavior that denies while a stale entry exists never reaches the cache; beyond `FailSafeExpiresAtUtc` the failure is returned; the throttle skips the handler for the throttle period and resumes after it (`FakeTimeProvider`); the provider receives the physical TTL; a bypassed request (no tenant under multi-tenancy, anonymous with `VaryByUser`) never reads a stale entry; two tenants never see each other's stale entry; settings validation per Design Choice 5; the DI test builds the provider with `ValidateOnBuild` and `ValidateScopes` and resolves one `FailSafeTracker` shared by two behaviors. **Guard tests:** the new optional constructor parameter is nullable, so `QueryCachingPipelineBehaviorGuardTests.cs` gets a valid-result test with a null `failSafeTracker` (stale serving works, no throttle). The Redis retention integration test is in PR 3.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #268 adds fail-safe caching to QueryCachingPipelineBehavior. #266 added
CacheEntry<T>.ExpiresAtUtc, CachePatternSettings, CachePatternLog/Metrics partial classes and the CachingPatterns range 450-499.

TASK:
Add FailSafeDurationSeconds and FailSafeThrottleDurationSeconds, FailSafePolicy, the entry field FailSafeExpiresAtUtc, the public in-process
FailSafeTracker (no entry rewrite) with its DI registration and the optional constructor parameter, the physical-TTL store, the stale path
(failure fallback, throttle), CachePatternLog.FailSafe.cs (462, 465, 466); write the unit, DI and guard tests; ask mechanical-fixer for
PublicAPI, manifest and changelog. PR 0 (Memory tracker fix) is merged before PR 1 and, of the PRs that edit MemoryCacheProvider eviction
callbacks, goes before #271 PR 1.

KEY RULES:
- Stale data only for the explicit transient failures (Left categories Unavailable, Timeout, UpstreamFailure, RateLimited and the listed
  exception types), as the maintainer answered Design Choice 1 (given in your brief); Internal and every other Left or exception fails closed:
  it is returned unchanged and the key is marked answered-definitively in the tracker (no cache write or removal).
- Never serve stale data past FailSafeExpiresAtUtc; the bypass checks run before any read.
- Never log EncinaError.Message; exceptions through ForLogging(); time from TimeProvider.
- Keep Handle at CRAP 10 or below: put the stale path in private methods.

REFERENCE FILES:
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs
- src/Encina/Results/EncinaErrors.cs (GetCategory)
- src/Encina/Errors/ErrorCategory.cs
- tests/Encina.IntegrationTests/Infrastructure/Caching/RedisCacheProviderIntegrationTests.cs
```

</details>

### Phase 2: Soft and hard timeouts (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

PR 2 holds exactly these 13 files and, under the recommended Design Choice 3 option B, depends on #267 PR 2 and PR 3 (the refresher, the refresh marker and the optional behavior parameter; under option A it needs only the refresher, under option C nothing); it adds the optional trailing constructor parameter `IServiceProvider? services = null` (used to resolve `IEncina` lazily), so `QueryCachingPipelineBehaviorGuardTests.cs` (file 11) gets a valid-result test for a null `services` (the timeouts are then NOT applied, with no abandoned run, documented) and `PublicAPI.Unshipped.txt` changes the constructor line:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Attributes/CacheAttribute.cs` | modify (`SoftTimeoutMs`, `HardTimeoutMs`) |
| 2 | `src/Encina.Caching/Patterns/CachePatternSettings.cs` | modify (timeouts and validation) |
| 3 | `src/Encina.Caching/Patterns/FailSafeErrors.cs` | new (internal static: `HardTimeout()`) |
| 4 | `src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs` | modify (owned re-dispatch with cancellation) |
| 5 | `src/Encina.Caching/Diagnostics/CachePatternLog.FailSafe.cs` | modify (ids 463 and 464; 467 and 468 only under option A) |
| 6 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 7 | `tests/Encina.UnitTests/Caching/QueryCachingTimeoutTests.cs` | new |
| 8 | `tests/Encina.UnitTests/Caching/FailSafeErrorsTests.cs` | new |
| 9 | `tests/Encina.UnitTests/Caching/CachePatternSettingsTests.cs` | modify |
| 10 | `tests/Encina.UnitTests/Caching/QueryCachingFailSafeTests.cs` | modify (timeouts with a stale entry) |
| 11 | `tests/Encina.GuardTests/Infrastructure/Caching/QueryCachingPipelineBehaviorGuardTests.cs` | modify (valid-result test for a null `services`) |
| 12 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 13 | `changelog.d/268-cache-fail-safe-timeouts.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Create** `FailSafeErrors.HardTimeout()` returning `EncinaErrors.Create(code: "cache.hard_timeout", category: ErrorCategory.Timeout, message: "The cache hard timeout elapsed before the handler completed.")` (named `category:`, constant message, no exception copied; ADR-062 category `Timeout`).
2. **Behavior** (Design Choice 3, recommended option B): when timeouts are configured, the behavior runs the handler through `IEncina.Send(request, innerContext, linked.Token)` in the same scope (`IEncina` resolved lazily from the request-scope `IServiceProvider`), where `innerContext` is `context.WithMetadata(CacheRefreshMarker.Key, true).WithIdempotencyKey(null)`: the marker is #267's, the identity and tenant are kept because it is the same live request in the same scope, and the idempotency key is cleared so an idempotency behavior does not treat the inner call as a duplicate; a request implementing `IDistributedIdempotentRequest` is not eligible for timeouts. The inner dispatch re-runs the whole pipeline, including the behaviors outside the cache behavior (validation, authorization, logging), once more, which the docs state. `linked` is a `CancellationTokenSource` cancelled at the hard timeout and the behavior awaits the run in every case, so the scope stays valid until the handler has finished or been cancelled. Hard elapsed: cancel, await, then serve stale if allowed (log 464), else return `FailSafeErrors.HardTimeout()`. Soft elapsed with a stale entry and a safe refresh available (not `VaryByUser`, a refresher registered): cancel and await the run, serve the stale entry (log 463) after the `IsSameCaller` check, and call `_refresher.TrySchedule(...)` with #267's re-dispatch delegate so the refresh runs in its own new scope; when no safe refresh exists the soft timeout is inert. Logs 467 (abandoned run completed) and 468 (abandoned run faulted, `ex.ForLogging()`) exist only if the maintainer answers option A, where the abandoned run is observed instead; with a null `services` no timeout is applied. Without a stale entry the soft timeout does nothing and only the hard timeout applies.
3. **Tests (unit, `FakeTimeProvider`, `TaskCompletionSource` gates):** soft timeout serves stale and schedules one refresh; repeated slow successes with a refresher refresh the entry, and with a `VaryByUser` request or a null refresher the soft timeout is inert and the success is stored; hard timeout cancels the handler (its token is cancelled and the run is awaited before the response) with and without a stale entry (the `Left` code and category `Timeout`); a handler finishing before the soft timeout is returned normally; a `VaryByUser` identity scope that ends during the wait gets no stale serve; a delayed denial from a gate registered before the cache behavior is returned; the constant error message equals the expected text and contains no request data; validation errors of Design Choice 5. **Guard tests:** internal types only: none.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
Phase 1 is merged. #267 PR 2 and PR 3 are merged: IBackgroundCacheRefresher and the refresh marker exist and the behavior takes the refresher as an optional parameter.

TASK:
Add SoftTimeoutMs and HardTimeoutMs, FailSafeErrors.HardTimeout (ADR-062 category Timeout, named category: argument, constant message), the
owned, awaited re-dispatch of the handler with a cancellable token (Design Choice 3 as the maintainer answered it, given in your brief), ids 463
and 464 (467 and 468 only if the answer is option A), and the unit tests; ask mechanical-fixer for PublicAPI, manifest and changelog.

KEY RULES:
- RequestHandlerCallback has no token: under option B the behavior runs the handler through IEncina.Send in the same scope with a marker context,
  a cleared idempotency key and a token it owns, and always awaits it, so no run outlives the scope; null services means no timeout.
- Under option A only: observe every abandoned run (no unobserved exception). Never log EncinaError.Message.
- Soft timeout without a stale entry does nothing; time from TimeProvider; no Thread.Sleep.

REFERENCE FILES:
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs
- src/Encina/Results/EncinaErrors.cs
- src/Encina.Caching/Patterns/BackgroundCacheRefresher.cs (from #267)
```

</details>

### Phase 3: Metrics, the Redis retention test and the docs page (pull request 3)

<details>
<summary><strong>Tasks</strong></summary>

PR 3 holds exactly these 7 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Diagnostics/CachePatternMetrics.FailSafe.cs` | new (partial class of #266 PR 4) |
| 2 | `src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs` | modify (the counter calls) |
| 3 | `tests/Encina.UnitTests/Caching/CachePatternMetricsFailSafeTests.cs` | new (`MeterListener`) |
| 4 | `tests/Encina.IntegrationTests/Infrastructure/Caching/FailSafeIntegrationTests.cs` | new (shared `RedisCollection`: an entry survives its logical expiry for the fail-safe lifetime and disappears after the physical TTL) |
| 5 | `docs/features/handler-caching.md` | new or modified (`docs-writer`: the existing `query-caching.md` documents the EF Core `QueryCacheInterceptor`, not the `[Cache]` request behavior; the name is coordinated with the #84 documentation plan and the first family PR that needs it creates it; the ordering prerequisite for gates, the transient-failure rule and the timeouts are documented here) |
| 6 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 7 | `changelog.d/268-cache-fail-safe-metrics.added.md` | new (`mechanical-fixer`) |

Tasks: the counter `encina.cache.failsafe` (tag `outcome`: `stale_on_failure`, `stale_on_soft_timeout`, `hard_timeout`, `throttled`, `exhausted`; the tag `encina.tenant_id` only with multi-tenancy on and a tenant present, ADR-054 D4) is incremented at the log sites of PR 1 and PR 2; the unit test asserts tags and the absence of keys; the integration test and the docs page are as listed. Depends on #266 PR 4 (`CachePatternMetrics.cs`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
Phases 1 and 2 are merged; #266 PR 4 created CachePatternMetrics.

TASK:
Add CachePatternMetrics.FailSafe.cs and the counter calls at the existing log sites, the MeterListener test, the Redis retention integration
test and the handler-caching docs page; ask mechanical-fixer for the manifest and the changelog and docs-writer for the page.

KEY RULES:
- No key, tenant value (other than the allowed tag), or message in any tag; counters only at sites that already log.

REFERENCE FILES:
- src/Encina.Caching/Diagnostics/CachePatternMetrics.cs (from #266)
- tests/Encina.IntegrationTests/Infrastructure/Caching/RedisCacheProviderIntegrationTests.cs
```

</details>

### Phase 4: Coverage targets and finalization

<details>
<summary><strong>Tasks</strong></summary>

1. **Error code:** `cache.hard_timeout`, ADR-062 category `Timeout`; the only new code. **EventIds** 462-468.
2. **Coverage targets (per flag).** Each row is entered in the manifest of the PR that adds the file and its tests, never later (#1762): PR 1 sets `FailSafePolicy.cs`, `FailSafeTracker.cs`, the log file and the behavior row; PR 2 sets `FailSafeErrors.cs` and raises the behavior row for the timeout paths; PR 3 sets the metrics file and the integration value:

| File | Unit | Guard | Contract | Integration | Justification |
|------|:----:|:-----:|:--------:|:-----------:|---------------|
| `Attributes/CacheAttribute.cs` | keeps 100 | 0 | 0 | 0 | #266 sets the first target (the manifest has none today); the four new properties are initializers executed by the behavior and settings tests, no validated parameter |
| `Patterns/CachePatternSettings.cs` | keeps 100 | 0 | 0 | 0 | Owned by #266; the fail-safe and timeout validation branches are covered by the settings tests of PR 1 and PR 2 |
| `Patterns/FailSafePolicy.cs` | 100 | 0 | 0 | 0 | Pure functions fully unit tested; internal without `InternalsVisibleTo` for the guard project |
| `Patterns/FailSafeTracker.cs` | 95 | 0 | 0 | 0 | Expiry, capacity eviction and the definitive-answer marks are unit tested with `FakeTimeProvider`; a public type whose members are internal (no public method with a validated parameter), so there is nothing for the guard flag to reach |
| `ServiceCollectionExtensions.cs` | 90 | 0 | 0 | 0 | The first target is set by #266 (kept); PR 1's DI test runs the tracker registration; no guard test calls `AddEncinaCaching` |
| `Encina.Caching.Memory/MemoryCacheProvider.cs` | keeps 95 | keeps 20 | 0 | 0 | PR 0: the replacement-safe tracking is covered by the new overwrite tests; existing targets are not lowered |
| `Patterns/FailSafeErrors.cs` | 100 | 0 | 0 | 0 | One factory asserted in a unit test; internal |
| `Diagnostics/CachePatternLog.FailSafe.cs`, `CachePatternMetrics.FailSafe.cs` | 90 | 0 | 0 | 0 | Generated log bodies and counter calls run in the behavior tests |
| `Behaviors/QueryCachingPipelineBehavior.cs` | keeps 98 | keeps 20 | 0 | 40 | Never lowered; the stale and timeout paths are unit tested; the integration flag runs the Redis retention test |

   The contract flag stays 0 for the new files: they implement no public contract (the contract flag of `QueryCachingPipelineBehavior.cs` is 0 in the manifest); a contract justification file is not allowed, so the manifest reason states that the observable rules are asserted by unit tests. The worker records measured numbers in each PR description and never lowers a target.
3. Verification per PR: `dotnet build --configuration Release` with zero warnings; `dotnet test` of `Encina.UnitTests` (Caching), `Encina.GuardTests`, and for PR 1 `Encina.IntegrationTests` (Redis collection); per-flag coverage and the CRAP table of changed methods.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
All code PRs are merged.

TASK:
Verify each PR carried its own per-flag targets (table above, measured with coverage-report.cs) and fix any gap through mechanical-fixer;
run --check-justifications.

KEY RULES:
- Never lower an existing target (target-ratchet). No log, metric or error code beyond the plan.

REFERENCE FILES:
- .github/coverage-manifest/Encina.Caching.json
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| "stale-if-error" and "stale-while-revalidate" (RFC 5861) | The semantics of serving stale on error and revalidating in the background |
| ADR-062 | Error category contract: the eligibility rule and the `Timeout` category of `cache.hard_timeout` |
| AGENTS.md section 3 | Fail closed on missing context; no message in logs; time from `TimeProvider`; ROP |
| AGENTS.md section 5 (caching) | Consistent across all 8 providers |
| ADR-054 D4 | Tenant tag on metrics only with multi-tenancy on |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `QueryCachingPipelineBehavior`, `CacheEntry<T>` | `src/Encina.Caching/Behaviors/` | Store and read paths |
| `EncinaErrors.GetCategory`, `EncinaErrors.Create(code, category, message)` | `src/Encina/Results/EncinaErrors.cs` | Failure classification and the new error |
| `CachePatternSettings`, `CachePatternLog`, `CachePatternMetrics` | `src/Encina.Caching` (from #266) | Shared settings, partial log and metrics |
| `IBackgroundCacheRefresher` | `src/Encina.Caching` (from #267) | Background refresh at the soft timeout |
| `FakeTimeProvider` | `Microsoft.Extensions.Time.Testing` | Deterministic timeout tests |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Caching` (`CachingPatterns`, registered by #266) | 462-468 | 462 stale on failure, 463 stale on soft timeout, 464 hard timeout, 465 throttle, 466 exhausted, 467 abandoned run completed, 468 abandoned run faulted. If Design Choice 3 is answered with option C, 463, 464, 467 and 468 are released to the family |

### Estimated File Count

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | 5 | 5 | New: policy, tracker, errors, two partial files; modified: attribute, settings, behavior, DI, `PublicAPI.Unshipped.txt` |
| Tests | 6 | 4 | New: fail-safe (policy and tracker), behavior, timeout, errors, metrics tests plus the integration test; modified: settings tests, DI tests, guard tests, the fail-safe tests (PR 2) |
| Documentation | 1 | 0 | `docs/features/handler-caching.md` (new or shared with the family) |
| Configuration | 2 | 1 | Two changelog fragments; the manifest |
| **Total** | **10** | **9** | pull requests: PR 0 4 files, PR 1 15, PR 2 13 and PR 3 7 files, each at most 15 files and 1500 lines |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #268 serves stale cache entries when the handler fails or is slow. #266 owns
ExpiresAtUtc, CachePatternSettings, the Log/Metrics partial classes and EventIds 450-499; #267 owns IBackgroundCacheRefresher; this plan
uses ids 462-468.

IMPLEMENTATION OVERVIEW:
0. PR 0: Memory key tracking safe under replacement (before PR 1, and before #271 PR 1; both edit the eviction callbacks).
1. PR 1: attribute (FailSafeDurationSeconds, FailSafeThrottleDurationSeconds), FailSafePolicy, public FailSafeTracker and its DI
   registration, physical TTL, the stale path with transient-failure fallback and throttle, logs 462/465/466.
2. PR 2: SoftTimeoutMs / HardTimeoutMs, the owned awaited re-dispatch with a cancellable token, cache.hard_timeout
   (ErrorCategory.Timeout), logs 463/464 (467/468 only under option A).
3. PR 3: metrics, Redis retention integration test, handler-caching docs page.

KEY PATTERNS:
- Stale only for exceptions and infrastructure categories; other Left removes the stale entry. Same key, same bypass checks.
- Constant error messages; no EncinaError.Message in logs; under the recommended option B the behavior owns, cancels and awaits the handler run.
- Follow the maintainer's answers to Design Choices 1 to 5, given in your brief.

REFERENCE FILES:
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs
- src/Encina/Results/EncinaErrors.cs
- .github/coverage-manifest/Encina.Caching.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Include | Extends `QueryCachingPipelineBehavior`; physical TTL longer than the logical one, all 8 providers through `SetAsync`. |
| 2 | OpenTelemetry | ✅ Include | Counter `encina.cache.failsafe` on the `Encina.Caching` meter of #266; tenant tag per ADR-054 D4; meter registration is #723. |
| 3 | Structured Logging | ✅ Include | EventIds 462-468 in `CachingPatterns`. |
| 4 | Health Checks | ❌ N/A | A behavior-level pattern with no backend of its own. |
| 5 | Validation | ✅ Include | Attribute combinations validated at resolution (Design Choice 5). |
| 6 | Resilience | ✅ Include | Fail-safe is the resilience pattern; it does not replace Polly retry or circuit breaker, which run in their own behaviors. |
| 7 | Distributed Locks | ❌ N/A | No lock; coalescing of the failing call is #266's single-flight when enabled. |
| 8 | Transactions | ❌ N/A | Cache operations are independent of units of work. |
| 9 | Idempotency | ❌ N/A | Re-serving a cached value is not message deduplication. |
| 10 | Multi-Tenancy | ✅ Include | Stale entries use the tenant-qualified key and the existing bypass checks (Design Choice 6); the issue's deferral to #746 covers only module scoping. |
| 11 | Module Isolation | ⏭️ Defer | Module-scoped fail-safe keys follow #746 (open). |
| 12 | Audit Trail | ❌ N/A | Infrastructure with no compliance event. |

**Deferred items:** module dimension (#746, open).

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| #266 PR 1 (`ExpiresAtUtc`, settings, whole-entry hit path), PR 3 (log class, EventId range) and PR 4 (`CachePatternMetrics.cs`, created there) | Pending | PR 1 needs #266 PR 1 and PR 3; PR 3 of this plan needs #266 PR 4 (its file 1 is a partial of the metrics class) |
| #267 PR 2 and PR 3 (`IBackgroundCacheRefresher`) | Pending | Needed by PR 2 of this plan under the recommended Design Choice 3 option B (refresher and marker) and under option A (refresher only); not needed under option C |
| ADR-062 error categories (`GetCategory`, `Create` with category) | ✅ Available | On `origin/main` |
| Maintainer answers to Design Choices 1 to 5 | Pending | Asked when work starts |

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas (Design Choices 1 to 5) when work starts, records the answers under `## Maintainer Decisions`, then briefs the workers in PR order (PR 0, 1, 2, 3).
2. After PR 1 merges, comment on #278 (auto-recovery, next family) that the stale path and `FailSafePolicy` exist.
