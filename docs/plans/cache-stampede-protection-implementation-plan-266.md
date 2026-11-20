# Implementation Plan: Cache stampede protection (single-flight, probabilistic early expiration, TTL jitter)

> **Issue**: [#266](https://github.com/dlrivada/Encina/issues/266)
> **Type**: Feature
> **Complexity**: Medium (4 code phases, 4 pull requests of 14, 9, 15 and 14 files)
> **Estimated Scope**: about 700 lines of production code, about 1,500 lines of tests
> **Milestone**: v0.18.0 — Integration & Platform Capabilities

---

## Summary

Issue #266 asks for three protections against a cache stampede on a `[Cache]` request: single-flight (concurrent identical misses share one handler execution), probabilistic early expiration (PER) and automatic TTL jitter, plus a distributed-lock mode, metrics and a configuration surface. Re-checked against `origin/main` (8727c5cc3):

| Premise of the issue | State in the code |
|----------------------|-------------------|
| "Encina has basic protection with distributed locks in Redis provider" | True only for `RedisCacheProvider.GetOrSetAsync` (`src/Encina.Caching.Redis/RedisCacheProvider.cs`, lines 149-196: a `SET NX` key, one 100 ms retry, then the factory runs regardless). `HybridCacheProvider.GetOrSetAsync` delegates to `HybridCache` (built-in coalescing) and `MemoryCacheProvider.GetOrSetAsync` to `IMemoryCache.GetOrCreateAsync`. |
| Stampede protection is reached by `[Cache]` requests | False. `QueryCachingPipelineBehavior` never calls `GetOrSetAsync`: it reads with `GetAsync<CacheEntry<TResponse>>` (line 212), runs `nextStep()` (line 119) and writes with `SetAsync` or `SetWithSlidingExpirationAsync` (lines 273-286). Every concurrent miss of a `[Cache]` request therefore reaches the handler today, on every provider. |
| `CacheEntry<T>` can carry expiry metadata | `CacheEntry<T>` is `internal sealed` in `QueryCachingPipelineBehavior.cs` (line 353) with `Value` and `CachedAtUtc` only; the provider TTL is the only expiry. |
| `StampedeProtectionMode` includes `ProbabilisticEarlyExpiration` and the attribute has `EarlyExpirationProbability` | Both are in the issue; they overlap (Design Choice 1). |
| Metrics, see #723 | #723 is open and instruments providers; no `Meter` exists in `Encina.Caching` today (`QueryCacheMetrics` lives in `Encina.EntityFrameworkCore.Diagnostics`). |

The plan puts all three protections in `QueryCachingPipelineBehavior`, so the 8 cache providers (Memory, Hybrid, Redis, Valkey, Dragonfly, Garnet, KeyDB and the planned Memcached of #277) behave the same without a provider change. It adds `ISingleFlightExecutor` (in-process, default), a `DistributedLock` mode on `IDistributedLockProvider` (already referenced by `Encina.Caching`), a `CacheEntry<T>.ExpiresAtUtc` field, `JitterPercent` and an XFetch-based early expiration, a `Meter` and a new EventId range.

**Shared pieces and ownership (family cache-patterns-1: #266, #267, #268, #269, #271):** this plan owns (a) the `CachingPatterns` EventId range 450-499 and its registration (PR 3), (b) the `CacheEntry<T>` envelope fields `ExpiresAtUtc` and `ComputeMilliseconds`, (c) `CachePatternSettings` (the per-request-type resolved settings that #267 and #268 extend), (d) `CachePatternMetrics` and `CachePatternLog` (the sibling plans add partial files), and (e) the request-flow contract below. The siblings #267, #268, #269 and #271 depend on this plan's PR 1 (envelope, settings, whole-entry hit path), PR 3 (log class, EventId range) and PR 4 (`CachePatternMetrics.cs` is created there, and every sibling adds a partial of it). The later family cache-patterns-2 (#272, #274, #275, #276, #278) reuses these types and does not redefine them.

**Request-flow contract (owned here, applied by #267 and #268).** After the existing bypass checks (caching disabled, `VaryByUser` without a user, multi-tenancy on with no tenant) and the key, the behavior does, in this order: (1) read the entry; (2) if it is fresh (`now < ExpiresAtUtc`): the deterministic background refresh of #267 first, else this plan's early-expiration decision, else serve it; (3) if it is stale but retained (#268): the stale path of #268; (4) if there is no entry: the miss path (this plan: single-flight or lock, handler, store with jitter). A request that bypasses the cache never reaches single-flight, so no response is shared across tenants or users.

**Affected packages:** `Encina.Caching` (core), `Encina` (EventId registry). No provider package changes. **Provider category:** Caching (8): behavior-level, identical on all 8.

---

## Design Choices

<details>
<summary><strong>1. Shape of the mode enum and where PER and jitter sit (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `StampedeProtectionMode { None, SingleFlight, DistributedLock }`; jitter (`JitterPercent`) and PER (`EarlyExpirationBeta`) are independent attribute properties** | The three protections combine (single-flight plus jitter plus PER); each has one meaning | Differs from the issue's enum, which lists `ProbabilisticEarlyExpiration` as a mode |
| **B) The issue's four-value enum (`ProbabilisticEarlyExpiration` as a mode) plus `JitterPercent`** | Matches the issue text | PER and single-flight are mutually exclusive, yet PER without single-flight makes the winner and many other requests refresh together |
| **C) `[Flags]` enum with all four values** | Combines | Invalid combinations (`None` with `SingleFlight`) need validation; the attribute reads worse than separate properties |

### Chosen Option: **A - three modes plus independent properties** (recommended, pending the maintainer)

### Rationale

- PER, jitter and coalescing address different causes (synchronized expiry, expiry-time misses, concurrent misses) and are meant to be used together.
- Option B wins if the maintainer wants the public vocabulary of the issue unchanged.
- Class: Dilemma

</details>

<details>
<summary><strong>2. Where single-flight lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) In `QueryCachingPipelineBehavior`, around the miss path, through `ISingleFlightExecutor`** | One implementation covers all 8 providers; the behavior is the only code that runs the handler on a miss | The provider-level `GetOrSetAsync` implementations keep their own, unrelated protection |
| **B) Inside each provider's `GetOrSetAsync`, and make the behavior call it** | Provider-native tricks (HybridCache) | Eight implementations; the behavior cannot hand a `Func` that returns `Either` and keep its identity and tenant checks around the read; Memcached has no lock primitive |

### Chosen Option: **A - behavior level**

### Rationale

- **Class:** Trivial
- **Settled by:** AGENTS.md section 1 (provider-agnostic abstractions) and section 5 (a caching feature must be consistent across all 8 providers); the code facts in the Summary: the behavior never calls `GetOrSetAsync` (`QueryCachingPipelineBehavior.cs`, lines 212 and 119).
- The existing `GetOrSetAsync` implementations are left as they are; issue-level changes to them belong to the read-through family (#272).

</details>

<details>
<summary><strong>3. What a coalesced follower receives and how cancellation behaves (Dilemma D3 and a settled part)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Followers share the leader's whole result, `Right` or `Left` (a `Left` is never cached); a follower whose leader was cancelled re-enters the flight and one of them becomes the new leader** | A failing backend sees one call per key and flight, which is when protection matters; no wrong cache write | A `Left` that depended on the leader's own identity (for a key that does not vary by user) reaches followers |
| **B) Followers share only a `Right`; on a `Left` each follower runs its own handler** | A leader-specific denial never reaches another caller | Under a backend outage every follower hits the backend: the stampede returns exactly when the backend is failing |

### Chosen Option: **A - share the whole result** (recommended, pending the maintainer)

### Rationale

- The key already encodes tenant and, with `VaryByUser`, the user; a follower gets what a cache hit on that key would give it. For a key that does not vary by user, the existing design already serves one caller's `Right` to all callers, so authorization has to run outside the cache behavior.
- Option B wins for a request type whose handler authorizes per caller although the attribute does not vary by user.
- Fixed part (not a choice): the shared execution always runs under the leader's cancellation token, because `RequestHandlerCallback<TResponse>` takes no token (`src/Encina/Abstractions/RequestHandlerDelegate.cs`, line 9), so the behavior cannot detach the handler from its caller. A follower never inherits a leader's `OperationCanceledException`: it re-enters the flight.
- Class: Dilemma

</details>

<details>
<summary><strong>4. How the distributed mode coordinates nodes (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) In-process single-flight first, then the leader takes an `IDistributedLockProvider` lock on `{LockKeyPrefix}:cache:{SHA-256 of the cache key, hex}` (the hash keeps tenant and user ids out of the lock providers' own logs), re-reads the cache after acquiring it, and runs the handler only on a miss** | Works with every lock provider (Memory, Redis, SqlServer), so with all 8 cache providers; one lock attempt per node and key | Waiting nodes poll through the lock provider's `wait` and `retry`, not through a push notification |
| **B) A Redis-specific executor in `Encina.Caching.Redis` (`SET NX` plus pub/sub completion message)** | Lower follower latency | Covers 5 of the 8 providers (the Redis family); Memory, Hybrid and Memcached would have no distributed mode, against AGENTS.md section 5 |
| **C) A, with B as a later optimization** | Compliant now, faster later | More code to maintain later |

### Chosen Option: **A - lock-provider based** (recommended, pending the maintainer)

### Rationale

- `Encina.Caching` already references `Encina.DistributedLock` and reads `CachingOptions.LockKeyPrefix`, `DefaultLockExpiry`, `DefaultLockWait` and `DefaultLockRetry`; the Memory, Redis, Valkey, Dragonfly, Garnet and KeyDB packages register an `IDistributedLockProvider`, the Hybrid package does not.
- Option C wins if measurements show follower latency matters.
- Outcomes inside the lock path: a lock wait that elapses (`null` handle) runs the handler uncoalesced and logs EventId 452; an exception from the lock provider follows `CachingOptions.ThrowOnCacheErrors` (true: rethrown; false: logged with `ForLogging()` as EventId 453, handler runs uncoalesced). The lock is never a security gate, so missing context does not apply to it; the key it protects is the tenant- and user-qualified cache key.
- Class: Dilemma

</details>

<details>
<summary><strong>5. Mode `DistributedLock` without an `IDistributedLockProvider` (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Throw `InvalidOperationException` naming the request type and the missing service, when the behavior of that type is constructed (every request of the type, deterministic from the first one)** | The attribute is an explicit requirement; a misconfiguration shows at the first test | A request fails for a protection that is an optimization |
| **B) Degrade to `SingleFlight` and log a Warning once per request type** | The request still works | A deployment believes it has cross-node protection and does not |

### Chosen Option: **A - throw** (recommended, pending the maintainer)

### Rationale

- With the Hybrid package no lock provider is registered, so this case is real; failing loudly follows the registration-completeness rule of AGENTS.md section 3 in spirit (a required dependency is never silently absent).
- Option B wins in a rolling deployment where the lock package is added after the attribute.
- Class: Dilemma

</details>

<details>
<summary><strong>6. Early-expiration algorithm (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) XFetch: refresh early when `now - computeTime * beta * ln(rand) >= expiry`; the entry stores `ComputeMilliseconds` (the handler duration of the write); property `EarlyExpirationBeta` (0 disables; default when set 1.0)** | The algorithm of the paper the issue cites; expensive handlers refresh earlier automatically | Renames the issue's `EarlyExpirationProbability`; needs the timing field |
| **B) Linear ramp: inside the last N percent of the TTL the chance of refresh grows from 0 to `EarlyExpirationProbability` per access** | Keeps the issue's property and meaning; no timing field | Not the cited algorithm; the ramp length is another knob |

### Chosen Option: **A - XFetch** (recommended, pending the maintainer)

### Rationale

- The refresh decision is pure and takes `TimeProvider` time and an injected random source (Phase 1 and 4), so tests are deterministic.
- Option B wins when the owners of a request type think in "10 percent of the time" rather than in a recompute-cost multiplier.
- The early refresh is executed inline by the one caller that wins the draw (the XFetch design), through the single-flight miss path, so the caller pays the handler latency; the deterministic background refresh is #267's, which takes precedence when both apply.
- Class: Dilemma

</details>

<details>
<summary><strong>7. Validation of attribute and option values (Dilemma D7)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `CachePatternSettings.Resolve` throws `InvalidOperationException` (request type, property, value) when the behavior of a `[Cache]` type is constructed (every request of that type, since the behavior is transient) and `JitterPercent` is outside 0 to under 100, `EarlyExpirationBeta` is negative or not finite, or a mode needs a missing service** | Fail-fast, one place, deterministic | The first request of that type fails; there is no startup scan of request types |
| **B) Clamp to the nearest valid value and log a Warning once** | The request works | A typo (`JitterPercent = 1000`) silently becomes 99 |

### Chosen Option: **A - throw at resolution** (recommended, pending the maintainer)

### Rationale

- A configuration error is not business logic, so AGENTS.md section 3 (no exceptions for business logic) is not touched; no new `EncinaError` code is created, so there is no ADR-062 category to name.
- The option defaults (`CachingOptions.DefaultJitterPercent`) are validated by the same `Resolve`, since they are read through it.
- Option B wins when a package must never fail a request for a performance setting. Siblings #267 and #268 follow the answer given here.
- Class: Dilemma

</details>

<details>
<summary><strong>8. Shape of the new `CacheEntry<T>` fields (Dilemma D8)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New members `DateTime? ExpiresAtUtc` and `long? ComputeMilliseconds`, nullable: a null `ExpiresAtUtc` means "no fixed lifetime", which is exactly a sliding entry (the sliding store branch writes none, because `RefreshAsync` keeps the entry alive); a null `ComputeMilliseconds` means the handler time was not measured (an entry stored before PR 4 enables early expiration, or a type without it)** | The type states the real semantics of sliding entries; no sentinel value | Two nullable members; every reader handles null |
| **B) `required` `ExpiresAtUtc` always set, with `DateTime.MaxValue` as the value for a sliding entry** | One rule for readers (always compare) | A sentinel in a persisted value; a sliding entry would look "fresh forever" to a reader that forgets the sentinel |

### Chosen Option: **A - nullable members** (recommended, pending the maintainer)

### Rationale

- `CacheEntry<T>` is serialized with `System.Text.Json` by `RedisCacheProvider` (camel case) and held by reference by `MemoryCacheProvider`; both handle nullable members.
- There is no support for entries written by an earlier build: AGENTS.md section 3 forbids compatibility layers and pre-1.0 has no existing user, so no fallback line reads an old-shape entry; the nullable members are justified by sliding-entry semantics alone.
- Option B wins when the maintainer prefers a type with no nullable expiry.
- Class: Dilemma

</details>

<details>
<summary><strong>9. Time, randomness and optional dependencies</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `TimeProvider` (already in the behavior) for every timestamp; randomness from `Random.Shared` through an internal `Func<double>` seam used by tests; new constructor dependencies are optional trailing parameters defaulting to a shared default** | Deterministic tests; 30 existing `new QueryCachingPipelineBehavior<` call sites keep compiling | A static default `InProcessSingleFlightExecutor.Shared` exists for direct construction |
| **B) Required constructor parameters** | Explicit | Breaks the 30 call sites (grep of `tests` and `src`) for no gain; DI always supplies them |

### Chosen Option: **A - `TimeProvider`, a random seam, optional parameters**

### Rationale

- **Class:** Trivial
- **Settled by:** AGENTS.md section 3 (time comes from `TimeProvider`) and the existing precedent in the same constructor (`TimeProvider? timeProvider = null` defaulting to `TimeProvider.System`, `QueryCachingPipelineBehavior.cs`, line 85; `MultiTenancyMarker? multiTenancy = null`).
- Guard tests: a null optional parameter is a valid input and gets a valid-result test, not an exception test.

</details>

<details>
<summary><strong>10. Return shape of `ISingleFlightExecutor.ExecuteAsync` (Dilemma D10)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Task<SingleFlightResult<TResult>>` with `WasCoalesced`** | The behavior can log and count a follower without a second channel | Differs from the issue's `Task<TResult> ExecuteAsync<TResult>(key, factory, ct)` |
| **B) The issue's `Task<TResult>`; coalescing is reported through an optional callback or not at all** | Matches the issue text | No clean way to count followers (an `out` is impossible in async) |

### Chosen Option: **A - result with a flag** (recommended, pending the maintainer)

### Rationale

- Metrics and logs of AC "Metrics track stampede prevention" need to know a follower from a leader. Option B wins if the public signature of the issue is a requirement.
- Class: Dilemma

</details>

<details>
<summary><strong>11. The default for early expiration (Dilemma D11)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `CachingOptions.DefaultEarlyExpirationBeta` (0 disables), attribute value wins when above 0, as for jitter** | Parity with the issue's `DefaultEarlyExpirationProbability` and `DefaultJitterPercent` | One more option and copy line, added in PR 1 together with `DefaultJitterPercent` (PR 1 already edits `CachingOptions.cs`, `ServiceCollectionExtensions.cs` and their tests, so it costs no file; PR 4 adds only the attribute property and the behavior use) |
| **B) Attribute only** | Smaller | The issue's third configuration member has no counterpart |

### Chosen Option: **A - an option with the same shape as jitter** (recommended, pending the maintainer)

### Rationale

- The name follows Design Choice 6 (XFetch, `Beta`); if option B of Design Choice 6 is chosen the option is `DefaultEarlyExpirationProbability`. Option B here wins when per-type tuning is enough.
- Class: Dilemma

</details>

---

## Implementation Phases

### Phase 1: TTL jitter and the entry envelope (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

PR 1 holds exactly these 14 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Patterns/CacheExpiration.cs` | new (internal static: `ApplyJitter`) |
| 2 | `src/Encina.Caching/Patterns/CachePatternSettings.cs` | new (internal sealed record and `Resolve`) |
| 3 | `src/Encina.Caching/Attributes/CacheAttribute.cs` | modify (`JitterPercent`) |
| 4 | `src/Encina.Caching/Configuration/CachingOptions.cs` | modify (`DefaultJitterPercent` and `DefaultEarlyExpirationBeta`) |
| 5 | `src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs` | modify (`CacheEntry<T>` members, jittered TTL at store) |
| 6 | `src/Encina.Caching/ServiceCollectionExtensions.cs` | modify (copy the new option; the option list at lines 53-72 is hand-written) |
| 7 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 8 | `tests/Encina.UnitTests/Caching/CacheExpirationTests.cs` | new |
| 9 | `tests/Encina.UnitTests/Caching/CachePatternSettingsTests.cs` | new |
| 10 | `tests/Encina.UnitTests/Caching/QueryCachingJitterTests.cs` | new |
| 11 | `tests/Encina.UnitTests/Caching/CachingOptionsTests.cs` | modify |
| 12 | `tests/Encina.UnitTests/Caching/ServiceCollectionExtensionsTests.cs` | modify |
| 13 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 14 | `changelog.d/266-cache-ttl-jitter.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Create** `internal static class CacheExpiration` with `static TimeSpan ApplyJitter(TimeSpan duration, double jitterPercent, Func<double> nextDouble)`: returns `duration * (1 + jitterPercent / 100 * (2 * nextDouble() - 1))`, never below one millisecond; `jitterPercent == 0` returns `duration` untouched without calling the random source.
2. **Create** `internal sealed record CachePatternSettings(double JitterPercent)` with `static CachePatternSettings Resolve(CacheAttribute attribute, CachingOptions options, Type requestType)`: the RAW attribute value and the RAW option value are each validated first (each must be 0 or finite and inside the range; a negative or NaN attribute value throws even when the option default is valid, because the comparison `attribute > 0` is false for both and would silently discard it), and only then the attribute value is used when it is greater than 0, else `options.DefaultJitterPercent` (the same two-step rule applies to `EarlyExpirationBeta` when PR 4 adds it); failures throw `InvalidOperationException` (Design Choice 7). The record is extended by PR 3 and PR 4 and by #267 and #268.
3. **Modify** `CacheAttribute`: `public double JitterPercent { get; init; }` (default 0, XML docs with the 0 to under 100 range and the rule that jitter applies to `Duration`, and to the sliding window when `SlidingExpiration` is on). **Modify** `CachingOptions`: `public double DefaultJitterPercent { get; set; }` (default 0; pay-for-what-you-use).
4. **Modify** `QueryCachingPipelineBehavior`: settings resolved in the constructor from the static attribute (read once per closed generic, as today) and the instance's `CachingOptions` (the behavior is registered transient, `ServiceCollectionExtensions.cs` line 80, and options differ per instance, so a static `Lazy` cannot hold them): an invalid value throws `InvalidOperationException` when the behavior is constructed, that is on every request of that type, which is deterministic and visible at the first request (Design Choice 7); `TryGetFromCacheAsync` and `TryGetForCallerAsync` now return `(bool Found, CacheEntry<TResponse>? Entry)` instead of `(Found, Value)`: PR 1 owns this change, because the hit path of #267 and #268 needs `CachedAtUtc`, `ExpiresAtUtc` and the later fields, and the served value is `Entry.Value`; an entry with no `ExpiresAtUtc` (sliding entries only; there is no fallback for entries of an earlier build, AGENTS.md section 3) is fresh and governed by the provider TTL exactly as today; the sliding branch of the store never writes `ExpiresAtUtc`, because `RefreshAsync` (`QueryCachingPipelineBehavior.cs`, lines 187-190) keeps a sliding entry alive past any fixed instant and a fixed expiry would make a live entry look stale (#267 treats an absent `ExpiresAtUtc` as not eligible and #268 rejects fail-safe with sliding, both consistent with this); the constructor resolves settings only when the type has `[Cache]` (the behavior is built for every request type, and `CacheAttribute` is null for most), so `Resolve` takes a non-null attribute and a request type without `[Cache]` resolves nothing and throws nothing (a unit test builds the behavior for a type without the attribute); `CacheEntry<T>` gets `public DateTime? ExpiresAtUtc { get; init; }` and `public long? ComputeMilliseconds { get; init; }` (Design Choice 8); `TryCacheResultAsync` computes the effective duration with `CacheExpiration.ApplyJitter`, sets `ExpiresAtUtc = CachedAtUtc + effectiveDuration` in the absolute branch only and passes `effectiveDuration` to `StoreCacheEntryAsync` (both branches: the jittered value is the sliding window in the sliding branch; the absolute maximum of the sliding branch is not jittered). `ComputeMilliseconds` is measured with `TimeProvider.GetTimestamp` around `nextStep()` in PR 4; PR 1 leaves it null.
5. **Modify** `AddEncinaCaching`: add `opt.DefaultJitterPercent = options.DefaultJitterPercent;` to the copy list (a missing line would silently drop the option).
6. **Tests (unit)**: `ApplyJitter` returns the duration for 0, stays inside the band for the seam values 0, 0.5 and 1, never goes below one millisecond; `Resolve` takes the attribute over the option, rejects -1, 100, NaN and infinity for the attribute with a VALID option default (the raw value is rejected, not hidden by the default) and for the option, with a message that names the request type and not the value of any request; the behavior stores a different TTL for two requests when the seam differs and sets `ExpiresAtUtc`; with `JitterPercent` 0 the provider receives exactly `Duration` (today's behavior) and a sliding entry (no `ExpiresAtUtc`) is served as fresh (Design Choice 8); the options test asserts the default 0 of `DefaultJitterPercent` and of `DefaultEarlyExpirationBeta`, and the DI test asserts `AddEncinaCaching(o => { o.DefaultJitterPercent = 10; o.DefaultEarlyExpirationBeta = 1.5; })` reaches `IOptions<CachingOptions>` for both. **Guard tests:** `CachePatternSettings` and `CacheExpiration` are internal and `Encina.Caching` has no `InternalsVisibleTo` for `Encina.GuardTests`, and the attribute and options have no constructor parameters, so no guard test is added; the manifest reason says so.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #266 adds stampede protection to QueryCachingPipelineBehavior
(src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs). The behavior reads CacheEntry<TResponse> with GetAsync, runs
nextStep() on a miss and stores with SetAsync; CacheEntry<T> is internal with Value and CachedAtUtc. AddEncinaCaching
(src/Encina.Caching/ServiceCollectionExtensions.cs) copies every CachingOptions property by hand.

TASK:
1. Add CacheExpiration.ApplyJitter and CachePatternSettings.Resolve (internal); add DefaultJitterPercent and DefaultEarlyExpirationBeta (0, never negative or non-finite) to CachingOptions with their copy lines (Design Choice 11: recommended option A, unless the maintainer answers otherwise in your brief).
2. Add JitterPercent to CacheAttribute and DefaultJitterPercent to CachingOptions; copy the option in AddEncinaCaching.
3. Give CacheEntry<T> optional ExpiresAtUtc and ComputeMilliseconds; store with the jittered TTL and set ExpiresAtUtc.
4. Write the unit tests of the plan; ask mechanical-fixer for PublicAPI.Unshipped.txt, the manifest entry and the changelog fragment.

KEY RULES:
- Time only from TimeProvider; randomness through an internal Func<double> seam, never in the pure functions.
- JitterPercent 0 changes nothing: the provider receives exactly Duration.
- A sliding entry has no ExpiresAtUtc (nullable by design) and is served as fresh; no fallback for entries of an earlier build (AGENTS.md section 3).
- Invalid values throw InvalidOperationException once per request type (Design Choice 7, as the maintainer answered it); no EncinaError code is created.
- Zero warnings; CRAP of every added or changed method at most 10; no log or telemetry in this phase.

REFERENCE FILES:
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs
- src/Encina.Caching/Attributes/CacheAttribute.cs
- src/Encina.Caching/Configuration/CachingOptions.cs
- src/Encina.Caching/ServiceCollectionExtensions.cs
- tests/Encina.UnitTests/Caching/QueryCachingPipelineBehaviorTests.cs
```

</details>

### Phase 2: The in-process single-flight executor (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

PR 2 holds exactly these 9 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Configuration/StampedeProtectionMode.cs` | new (public enum: `None`, `SingleFlight`, `DistributedLock`) |
| 2 | `src/Encina.Caching/Abstractions/ISingleFlightExecutor.cs` | new (also holds `SingleFlightResult<TResult>`) |
| 3 | `src/Encina.Caching/Patterns/InProcessSingleFlightExecutor.cs` | new (public sealed) |
| 4 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 5 | `tests/Encina.UnitTests/Caching/InProcessSingleFlightExecutorTests.cs` | new |
| 6 | `tests/Encina.GuardTests/Infrastructure/Caching/InProcessSingleFlightExecutorGuardTests.cs` | new |
| 7 | `tests/Encina.ContractTests/Caching/SingleFlightExecutorContractTests.cs` | new (instantiates `InProcessSingleFlightExecutor` and asserts the `ISingleFlightExecutor` contract: one factory run per key, results shared, nothing retained, cancelled leader re-elected; a justification file is never allowed for a contract test, AGENTS.md section 9) |
| 8 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 9 | `changelog.d/266-cache-single-flight-executor.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Create** `public interface ISingleFlightExecutor { Task<SingleFlightResult<TResult>> ExecuteAsync<TResult>(string key, Func<CancellationToken, Task<TResult>> factory, CancellationToken cancellationToken); }` and `public readonly record struct SingleFlightResult<TResult>(TResult Value, bool WasCoalesced)`. The generic `TResult` carries `Either<EncinaError, TResponse>` unchanged (ROP: nothing is thrown for a business outcome); `WasCoalesced` tells the behavior to log and count a follower. The issue's signature returns a bare `Task<TResult>`; the extra flag is the minimum the metrics and logs need.
2. **Create** `InProcessSingleFlightExecutor`: a `ConcurrentDictionary<string, Flight>` where a `Flight` holds one `Lazy<Task<TResult>>`-style shared task per key and type; the first caller creates it (the leader runs the factory with its own token), later callers with the same key and result type await it. The entry is removed when the task completes (success, fault or cancellation) in a `finally`, so nothing is retained. A follower whose awaited flight ended with `OperationCanceledException` of the leader's token loops and re-enters (Design Choice 3); a follower's own token cancels only its own wait. A factory exception is rethrown to every waiter (the behavior treats it like today: `ThrowOnCacheErrors` does not apply to handler exceptions). `public static InProcessSingleFlightExecutor Shared { get; }` is the default for direct construction (Design Choice 9). The key is used as given (it is already tenant- and user-qualified by the key generator); the dictionary uses `StringComparer.Ordinal`.
3. **Tests (unit)**: one factory run for 32 concurrent callers of the same key (a `TaskCompletionSource` gate, no `Thread.Sleep`); different keys run independently; followers receive the leader's value and `WasCoalesced = true`, the leader `false`; a faulting factory faults every waiter and the next call runs the factory again (nothing retained); a cancelled leader lets a follower become leader and succeed; a cancelled follower leaves the others unaffected; the dictionary is empty after completion; two result types for one key do not mix. **Guard tests:** null `key` and null `factory` throw `ArgumentNullException`; a pre-cancelled token throws before the factory runs; a whitespace key is rejected (`ArgumentException`) because an empty key would coalesce unrelated requests.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
Phase 1 is merged. QueryCachingPipelineBehavior runs the handler on every miss. This phase adds the executor that Phase 3 wires in.

TASK:
Add StampedeProtectionMode, ISingleFlightExecutor, SingleFlightResult<TResult> and InProcessSingleFlightExecutor (public sealed) with
the unit and guard tests of the plan; ask mechanical-fixer for PublicAPI.Unshipped.txt, the manifest and the changelog fragment.

KEY RULES:
- The shared task runs under the leader's token; a follower never inherits a leader's OperationCanceledException (it re-enters).
- Nothing is retained after completion; no static state other than the Shared default instance.
- Tests use TaskCompletionSource gates and FakeTimeProvider, never sleeps.
- ROP: TResult carries the Either unchanged; no exception for a business result.

REFERENCE FILES:
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs
- src/Encina/Abstractions/RequestHandlerDelegate.cs
- tests/Encina.GuardTests/Infrastructure/Caching/QueryCachingPipelineBehaviorGuardTests.cs
```

</details>

### Phase 3: Behavior integration and EventIds (pull request 3)

<details>
<summary><strong>Tasks</strong></summary>

PR 3 holds exactly these 15 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Attributes/CacheAttribute.cs` | modify (`StampedeProtection`) |
| 2 | `src/Encina.Caching/Configuration/CachingOptions.cs` | modify (`DefaultStampedeProtection`) |
| 3 | `src/Encina.Caching/Patterns/CachePatternSettings.cs` | modify (mode) |
| 4 | `src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs` | modify (optional `ISingleFlightExecutor` parameter and the flight) |
| 5 | `src/Encina.Caching/ServiceCollectionExtensions.cs` | modify (`TryAddSingleton<ISingleFlightExecutor, InProcessSingleFlightExecutor>`, option copy) |
| 6 | `src/Encina.Caching/Diagnostics/CachePatternLog.cs` | new (internal static partial class; ids 450 and 451) |
| 7 | `src/Encina/Diagnostics/EventIdRanges.cs` | modify (register `CachingPatterns = (450, 499)`) |
| 8 | `src/Encina/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 9 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`: the constructor line changes, so the old line is removed) |
| 10 | `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` | modify (`Encina.Caching` maps to `Caching` and `CachingPatterns`) |
| 11 | `tests/Encina.UnitTests/Caching/QueryCachingSingleFlightTests.cs` | new |
| 12 | `tests/Encina.UnitTests/Caching/ServiceCollectionExtensionsTests.cs` | modify |
| 13 | `tests/Encina.GuardTests/Infrastructure/Caching/QueryCachingPipelineBehaviorGuardTests.cs` | modify |
| 14 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 15 | `changelog.d/266-cache-stampede-single-flight.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Modify** `CacheAttribute` (`public StampedeProtectionMode StampedeProtection { get; init; }`, default `None`) and `CachingOptions` (`DefaultStampedeProtection`, default `None`; attribute `None` falls back to the option, so a per-request opt-out needs the attribute set to `None` and the option to `None`: the attribute cannot express "inherit" versus "off"; this is stated in the XML docs and Design Choice 1 is the place to revisit it if the maintainer wants a nullable property). Copy the option in `AddEncinaCaching`.
2. **Modify** `QueryCachingPipelineBehavior`: add the optional trailing parameter `ISingleFlightExecutor? singleFlight = null` (null uses `InProcessSingleFlightExecutor.Shared`). On the miss path only (never on a bypass, never on a hit): when the resolved mode is not `None`, wrap `nextStep()` and the store in `singleFlight.ExecuteAsync(cacheKey, ...)`, so the leader stores once and followers return the shared `Either` without a second write. The flight key is the cache key; a request with `VaryByUser` keeps its identity check (`IsSameCaller`) at three points: before joining the flight, before the leader's store, and before a follower is served the coalesced result (the existing cache-hit path rejects the same race, `TryGetForCallerAsync` and `AScopeThatEndsWhileTheCacheIsRead_DoesNotServeTheUsersEntry_NorWrite`); on a mismatch that caller runs its own handler uncached and shares nothing. A gated test ends a follower's identity scope while it awaits the flight and asserts it receives its own handler result, not the leader's. `WasCoalesced` logs EventId 450 (Debug, request type and redacted key through `CacheKeyRedactor.Redact`, never the key itself) and a leader cancelled while followers wait logs 451 (Debug).
3. **Create** `CachePatternLog` (`internal static partial class`, `[LoggerMessage]`, XML docs naming the range `CachingPatterns` 450-499): 450 `SingleFlightCoalesced`, 451 `SingleFlightLeaderCancelled`; the sibling plans add partial files (`CachePatternLog.Stampede.cs` is this plan's file name from PR 4 on, the rest are theirs).
4. **EventIds:** register `public static readonly (int Min, int Max) CachingPatterns = (450, 499);` in `EventIdRanges.cs` with its XML doc, list it in `src/Encina/PublicAPI.Unshipped.txt`, and change the `AssemblyRanges` entry of `Encina.Caching` in `EncinaEventIdAllocationTests` to `[nameof(EventIdRanges.Caching), nameof(EventIdRanges.CachingPatterns)]`. Run the architecture tests (`EventIdUniquenessRule`). The family allocation (packed): #266 450-455, #267 456-461, #268 462-468, #269 469-477, #271 478-484, 485-499 free for the family.
5. **Tests (unit)**: 32 concurrent misses of one request run the handler once and return equal results (the cache provider is a counting fake), the cache is written once; a `Left` from the leader reaches every follower and nothing is cached; different keys (two tenants, two users) never coalesce; a bypassed request (anonymous with `VaryByUser`, no tenant with multi-tenancy on) never reaches the executor (a spy executor sees no call); mode `None` calls the handler per miss exactly as today (all existing behavior tests stay green); a cancelled leader lets a follower run; the DI test builds the provider with `ValidateOnBuild` and `ValidateScopes` and resolves `QueryCachingPipelineBehavior<,>` with the executor registered once and a user-registered `ISingleFlightExecutor` winning (`TryAdd`). **Guard tests:** the new optional constructor parameter is nullable and accepts null, so `QueryCachingPipelineBehaviorGuardTests.cs` gets a valid-result test (a null `singleFlight` builds the behavior and a miss still returns the handler result through `InProcessSingleFlightExecutor.Shared`), not an exception test.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
Phases 1 and 2 are merged: jitter, CacheEntry.ExpiresAtUtc, CachePatternSettings, ISingleFlightExecutor exist. QueryCachingPipelineBehavior
still runs the handler on every miss.

TASK:
Add StampedeProtection to CacheAttribute and DefaultStampedeProtection to CachingOptions, wire the executor into the miss path of
QueryCachingPipelineBehavior (optional trailing constructor parameter, null means InProcessSingleFlightExecutor.Shared), add CachePatternLog
(ids 450 and 451), register the CachingPatterns range 450-499 in EventIdRanges.cs and the architecture test map, register the executor with
TryAddSingleton. Write the unit tests of the plan; ask mechanical-fixer for both PublicAPI files, the manifest and the changelog fragment.

KEY RULES:
- Single-flight only on the miss path, after every bypass check and after the key exists: a bypassed request never reaches it.
- The flight key is the cache key; log only CacheKeyRedactor.Redact(key); never an EncinaError message.
- Followers share the leader's whole Either; a Left is never cached.
- EventIds only from 450-455 for this plan, packed; 450 and 451 now.
- 30 existing constructor call sites must keep compiling (optional trailing parameter).

REFERENCE FILES:
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
- tests/Encina.UnitTests/Caching/CacheTenantFailClosedTests.cs
```

</details>

### Phase 4: Distributed lock mode, early expiration and metrics (pull request 4)

<details>
<summary><strong>Tasks</strong></summary>

PR 4 holds exactly these 14 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs` | modify (lock path, early expiration, `ComputeMilliseconds`, metrics) |
| 2 | `src/Encina.Caching/Patterns/CacheExpiration.cs` | modify (`ShouldRefreshEarly`) |
| 3 | `src/Encina.Caching/Patterns/CachePatternSettings.cs` | modify (`EarlyExpirationBeta`) |
| 4 | `src/Encina.Caching/Attributes/CacheAttribute.cs` | modify (`EarlyExpirationBeta`) |
| 5 | `src/Encina.Caching/Diagnostics/CachePatternLog.Stampede.cs` | new (ids 452 to 455) |
| 6 | `src/Encina.Caching/Diagnostics/CachePatternMetrics.cs` | new (internal static `Meter`) |
| 7 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 8 | `tests/Encina.UnitTests/Caching/QueryCachingDistributedLockTests.cs` | new |
| 9 | `tests/Encina.UnitTests/Caching/QueryCachingEarlyExpirationTests.cs` | new |
| 10 | `tests/Encina.UnitTests/Caching/CachePatternMetricsTests.cs` | new |
| 11 | `tests/Encina.IntegrationTests/Infrastructure/Caching/StampedeProtectionIntegrationTests.cs` | new |
| 12 | `tests/Encina.GuardTests/Infrastructure/Caching/QueryCachingPipelineBehaviorGuardTests.cs` | modify |
| 13 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 14 | `changelog.d/266-cache-distributed-lock-and-early-expiration.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Behavior, lock mode** (`DistributedLock`): after the in-process flight is entered, the leader calls `IDistributedLockProvider.TryAcquireAsync(resource, CachingOptions.DefaultLockExpiry, DefaultLockWait, DefaultLockRetry, ct)` with `resource = $"{LockKeyPrefix}:cache:{hash}"` where `hash` is the SHA-256 of the cache key in lowercase hex truncated to 32 characters: two cache keys (two tenants, two users) get distinct resources (a collision is negligible), and the lock providers, which log the raw resource (`RedisDistributedLockProvider` and `MemoryDistributedLockProvider` log it at acquisition, release and failure, some at Warning), never see a tenant or user id; no provider change is needed and no two callers' locks merge; on a handle it re-reads the cache: for a cold miss any entry found is a hit (EventId 454, Debug, returns the entry value without running the handler); for an early-expiration refresh (the caller observed a fresh entry and won the draw) it is a hit only when the re-read entry has a `CachedAtUtc` newer than the observed one (another caller refreshed it meanwhile), otherwise the same entry is found and the handler runs; else runs the handler, stores, and disposes the handle in a `finally`; a `null` handle logs 452 (Warning) and runs the handler uncoalesced; a provider exception follows `ThrowOnCacheErrors` and logs 453 through `ex.ForLogging()`. The optional constructor parameter is `IDistributedLockProvider? lockProvider = null`; a null provider with mode `DistributedLock` follows Design Choice 5. The lock resource is the hashed form above, so it carries no identifier and needs no redaction; a test records every resource passed to a fake lock provider and asserts none contains the tenant or user text, and a second test runs the real `MemoryDistributedLockProvider` with a capturing logger and asserts its logs contain neither.
2. **Early expiration:** `CacheExpiration.ShouldRefreshEarly(DateTime nowUtc, DateTime expiresAtUtc, long computeMilliseconds, double beta, Func<double> nextDouble)` implements Design Choice 6 (A); `nextDouble() == 0` is treated as the smallest positive value so `ln(0)` never occurs. In the hit path, a fresh entry whose draw says refresh runs the miss path (single-flight) as if it were a miss, logs 455 (Debug) and counts it; if that refresh yields a `Left`, the existing cached value is served (the request does not fail because of an early refresh). `ComputeMilliseconds` is measured with `TimeProvider.GetTimestamp` around the handler and stored.
3. **Metrics:** `internal static partial class CachePatternMetrics` (partial, so #267, #268, #269 and #271 add their instruments in their own files) with `Meter("Encina.Caching", "1.0")` and `Counter<long> encina.cache.stampede` (tag `outcome`: `coalesced`, `lock_acquired`, `lock_timeout`, `double_check_hit`, `early_refresh`; tag `encina.tenant_id` only when a tenant exists and multi-tenancy is registered, ADR-054 D4, the tag name used by `RetryMetrics`). The tags never carry a key, a user or an error message. Registering the meter name with the OpenTelemetry package belongs to #723.
4. **Tests (unit)**: lock acquired then handler runs once; a second node is simulated with its OWN behavior instance and its OWN `InProcessSingleFlightExecutor` (never `Shared`, so local coalescing cannot hide a broken lock), a shared fake cache and a fake lock provider that holds the first node's handler until the second node has attempted the lock, after which the second acquisition sees the first node's cache write (double-check hit, handler not run); a `DistributedLock` request that wins the early-expiration draw while its entry is fresh runs the handler (the re-read returns the same entry, so it is not a hit), and one whose entry was refreshed by another caller meanwhile does not; null handle runs the handler and logs 452; provider exception with `ThrowOnCacheErrors` true rethrows and false continues; missing provider with the mode set follows the maintainer's answer to Design Choice 5; `ShouldRefreshEarly` is false far from expiry, true at or after expiry, monotonic in `beta` and in `computeMilliseconds` (an FsCheck property, `tests/Encina.PropertyTests/Infrastructure/Caching/` is not touched: the invariant is asserted over a fixed grid in the unit test to hold the file count); `beta` 0 never refreshes; the early refresh `Left` serves the cached value; meter tests with `MeterListener` assert the tags, the absence of a tenant tag without multi-tenancy and the absence of any key. **Guard tests:** a valid-result test for the new optional `lockProvider` parameter in `QueryCachingPipelineBehaviorGuardTests.cs` (null accepted when the mode is not `DistributedLock`, and a request with that mode and no provider follows Design Choice 5).
5. **Integration (real Redis through `RedisCollection`/`RedisFixture`)**: two behavior instances, each with its own `InProcessSingleFlightExecutor`, sharing one real `RedisCacheProvider` and one Redis server (each with its own `RedisDistributedLockProvider`), with a slow counting handler that the first node blocks until the second node has attempted the Redis lock: 10 concurrent requests from the two instances run the handler once, which only the Redis lock can explain; with the lock holder cancelled, the next request recovers within the lock expiry. The Redis family (Valkey, Dragonfly, Garnet, KeyDB) shares `RedisCacheProvider` and `RedisDistributedLockProvider`, so this one fixture is their coverage; Memory is covered by the unit tests with `MemoryDistributedLockProvider`; Hybrid has no lock provider (Design Choice 5) and Memcached follows #277.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
Phases 1 to 3 are merged: jitter, ISingleFlightExecutor wired into the miss path, EventIds 450 and 451, the CachingPatterns range.

TASK:
Add the DistributedLock mode (in-process flight, then IDistributedLockProvider lock, double-check read, handler), XFetch early expiration
(CacheExpiration.ShouldRefreshEarly, CacheAttribute.EarlyExpirationBeta, CacheEntry.ComputeMilliseconds), CachePatternLog.Stampede.cs
(ids 452-455), CachePatternMetrics and the tests of the plan; ask mechanical-fixer for PublicAPI.Unshipped.txt, the manifest and the changelog.

KEY RULES:
- Lock timeout runs the handler uncoalesced; a lock-provider exception follows ThrowOnCacheErrors; never a security gate, never swallow silently.
- Log only redacted keys, error codes or ex.ForLogging(); the tag encina.tenant_id only with multi-tenancy on and a tenant present.
- An early refresh that fails serves the cached value; it never fails the request.
- No new EncinaError code; no Thread.Sleep; time from TimeProvider, randomness from the seam.
- EventIds 452-455 only (packed after 450-451).

REFERENCE FILES:
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs
- src/Encina.DistributedLock/IDistributedLockProvider.cs
- src/Encina.Caching.Redis/RedisDistributedLockProvider.cs
- src/Encina/Recoverability/RetryMetrics.cs (tenant tag precedent)
- tests/Encina.IntegrationTests/Infrastructure/Caching/RedisCacheProviderIntegrationTests.cs
```

</details>

### Phase 5: Cross-cutting integration, observability and coverage targets

<details>
<summary><strong>Tasks</strong></summary>

1. **EventIds** are those of Phases 3 and 4 only (450-455); no other id is used.
2. **Telemetry** is the `Meter` of Phase 4; no `ActivitySource` is added (a span per coalesced request would cost more than it explains; spans of the cache operations belong to #723).
3. **No new error code**: nothing returns a `Left` that the pattern creates, so there is no ADR-062 `ErrorCategory` to name; the `Left` values that flow through are the handler's own.
4. **Coverage targets (per flag, new or touched files)** are part of the PR that adds the file and its tests, never of a later PR (#1762): each of PR 1 to PR 4 lists the rows of the table below for its own files in its manifest edit (`mechanical-fixer`), measured with `coverage-report.cs` in that PR. Provisional EventIds: 452 to 455 depend on Design Choices 4 and 6 (lock timeout, lock error, double-check hit and early refresh exist only under the recommended options); a different answer releases the unused ids to the family:

| File | Unit | Guard | Contract | Integration | Justification |
|------|:----:|:-----:|:--------:|:-----------:|---------------|
| `Encina.Caching/Patterns/CacheExpiration.cs` | 100 | 0 | 0 | 0 | Two pure functions fully driven by unit tests; internal, no `InternalsVisibleTo` for the guard project |
| `Encina.Caching/Patterns/CachePatternSettings.cs` | 100 | 0 | 0 | 0 | Every validation branch and the attribute-over-option rule are unit tested; internal |
| `Encina.Caching/Patterns/InProcessSingleFlightExecutor.cs` | 100 | 25 | 80 | 0 | The concurrency paths are unit tested with gates and by the contract test; the guard tests throw before the flight starts (null key, null factory, whitespace key, pre-cancelled token), so they reach only the argument checks, a quarter of the file |
| `Encina.Caching/Abstractions/ISingleFlightExecutor.cs`, `Configuration/StampedeProtectionMode.cs` | 0 | 0 | 0 | 0 | An interface, a record struct and an enum: no executable lines of their own |
| `Encina.Caching/Diagnostics/CachePatternLog.cs`, `CachePatternLog.Stampede.cs` | 90 | 0 | 0 | 0 | Source-generated `[LoggerMessage]` bodies run when the unit tests of the behavior hit each path; internal |
| `Encina.Caching/Diagnostics/CachePatternMetrics.cs` | 95 | 0 | 0 | 0 | The counter calls are driven by `MeterListener` unit tests |
| `Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs` | keeps 98 | keeps 20 | 0 | 0 | The existing targets are never lowered; the new branches are covered by the unit tests of the same PR (guard tests only reach the constructor and argument checks, so the guard target is not raised) |
| `Encina.Caching/Attributes/CacheAttribute.cs` | 100 | 0 | 0 | 0 | The manifest has no target today (default rules only); the new properties are initializers executed by the attribute and behavior tests; no validated parameter, so no guard lines |
| `Encina.Caching/Configuration/CachingOptions.cs` | keeps | keeps 85 | 0 | 0 | Existing guard 85 is kept; the new properties are initializers executed by the same constructions |
| `Encina.Caching/ServiceCollectionExtensions.cs` | 90 | 0 | 0 | 0 | The manifest has no target today (set in PR 1 and kept by PR 3); the DI tests run the option copy and the executor registration; no test in `Encina.GuardTests` calls `AddEncinaCaching` and PR 1 adds no guard test, so the guard target is 0 for that reason |

   The Redis integration test (Phase 4) adds an integration-flag measurement for `QueryCachingPipelineBehavior.cs` of at least 40, because only the lock path with a real provider runs there. The worker measures every flag with `coverage-report.cs` and records the numbers in the PR description; no target is lowered (`target-ratchet`).
5. **Property, contract, load, benchmark**: the early-expiration monotonicity runs as a fixed grid in a unit test (Phase 4); a contract test of `ISingleFlightExecutor` runs against the real executor (PR 2, so the contract flag reaches its target for `InProcessSingleFlightExecutor.cs`: 80); no load test (the concurrency is covered by the gated unit tests and the Redis integration test); no benchmark in these PRs, because jitter and the miss path add one dictionary operation per miss (a follow-up benchmark of the behavior belongs to the existing `QueryCachingPipelineBenchmarks`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
The four code PRs are merged. Measurement and finalization remain.

TASK:
Verify that each PR carried its own per-flag targets (table above) and that nothing was deferred; if a PR missed one, add it in a small PR, with one-sentence
justifications through mechanical-fixer; run --check-justifications.

KEY RULES:
- Never lower an existing target (target-ratchet).
- Add no log, metric or error code beyond the plan.

REFERENCE FILES:
- .github/coverage-manifest/Encina.Caching.json
- docs/testing/coverage-measurement-methodology.md
```

</details>

### Phase 6: Documentation and finalization

<details>
<summary><strong>Tasks</strong></summary>

1. XML documentation with `<example>` on `StampedeProtectionMode`, `ISingleFlightExecutor`, `SingleFlightResult<TResult>`, `InProcessSingleFlightExecutor` and the new `CacheAttribute` and `CachingOptions` members; each remark states the tenant and user isolation of the flight key and that a bypassed request is never coalesced.
2. `docs-writer`: add a "Stampede protection" section to `docs/features/handler-caching.md` (new or shared with the family: `query-caching.md` documents the EF Core `QueryCacheInterceptor`, so the `[Cache]` handler page is a separate one named with the #84 documentation plan; the first family PR that needs it creates it) (modes, jitter, XFetch, the lock mode and the Hybrid limitation) citing the unit tests by name and no typed figure. The `Encina.Caching` package has no README today; creating it is outside this plan.
3. `PublicAPI.Unshipped.txt` lines are written by `mechanical-fixer` per PR (the old `QueryCachingPipelineBehavior` constructor line is removed in PR 3 and PR 4: RS0017).
4. No ROADMAP, INVENTORY or ADR change: no architectural decision beyond this plan; ADR number assigned when the ADR pull request opens, if the maintainer asks for one for the request-flow contract.
5. Verification per PR: `dotnet build --configuration Release` with zero warnings; `dotnet test` of `Encina.UnitTests` (Caching namespaces), `Encina.GuardTests` and, for PR 4, `Encina.IntegrationTests` (Redis collection); per-flag coverage and the CRAP table of the changed methods (`QueryCachingPipelineBehavior.Handle` must stay at CRAP 10 or below: extract the miss path into private methods in PR 3 before adding branches).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
All code PRs are merged. Documentation remains.

TASK:
Confirm the XML docs and the four changelog fragments; ask docs-writer for the handler-caching.md section.

KEY RULES:
- No hand-typed figures; cite tests by name.
- Fragment section is added; never edit CHANGELOG.md.

REFERENCE FILES:
- docs/features/handler-caching.md (created by the first family PR that needs it)
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| "Optimal Probabilistic Cache Stampede Prevention" (Vattani, Chierichetti, Lowenstein), the XFetch paper the issue cites | The early-expiration decision of Design Choice 6 |
| AGENTS.md section 5 (caching category) | A caching feature is consistent across all 8 providers |
| AGENTS.md section 3 | Time from `TimeProvider`; `ThrowOnCacheErrors` semantics for cache-side failures; no exceptions for business logic |
| ADR-054 D4 | Tenant tag on metrics only with multi-tenancy on |
| ADR-021 | EventId range registered before use |
| ADR-062 | No new error code here, so no new category |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `QueryCachingPipelineBehavior` and `CacheEntry<T>` | `src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs` | The only place the miss path runs; extended |
| `CacheAttribute`, `CachingOptions`, `AddEncinaCaching` | `src/Encina.Caching` | New properties and the hand-written option copy |
| `IDistributedLockProvider` | `src/Encina.DistributedLock/IDistributedLockProvider.cs` | The lock of the distributed mode; registered by Memory, Redis, Valkey, Dragonfly, Garnet and KeyDB |
| `CacheKeyRedactor` | `src/Encina.Caching/KeyGeneration/CacheKeyRedactor.cs` | Redaction of every logged key and lock resource |
| `MultiTenancyMarker` | `src/Encina/Abstractions/MultiTenancyMarker.cs` | Existing bypass and the tenant metric tag condition |
| `RetryMetrics` | `src/Encina/Recoverability/RetryMetrics.cs` | Precedent for the `encina.tenant_id` tag |
| `RedisCollection`, `RedisFixture` | `tests/Encina.IntegrationTests/Infrastructure/Caching/Fixtures/RedisFixture.cs` | Shared Redis container for the integration test |
| `EventIdRanges` and `EncinaEventIdAllocationTests` | `src/Encina/Diagnostics/EventIdRanges.cs`, `tests/Encina.UnitTests/Testing/Architecture/` | The new range and the assembly map |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Caching` (new range `CachingPatterns`, block 450-499 of the plan batch registry) | 450-455 | This plan: 450 coalesced, 451 leader cancelled, 452 lock timeout, 453 lock provider error, 454 double-check hit, 455 early refresh. Family: #267 456-461, #268 462-468, #269 469-477, #271 478-484; 485-499 free. The existing `Caching` range 3500-3549 is untouched (3500-3513 used) |

### Estimated File Count

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | 8 | 5 | New: executor, interface (with `SingleFlightResult`), enum, settings, expiration, two log files, metrics; modified: attribute, options, behavior, DI, `EventIdRanges.cs` |
| Tests | 11 | 4 | New: unit (8), guard (1), integration (1), contract (1); no property test; modified: options, DI, guard behavior tests, event-id architecture test |
| Documentation | 0 | 1 | `docs/features/handler-caching.md` (new or shared with the family) |
| Configuration | 4 | 3 | Four changelog fragments; the manifest (edited in each PR); two `PublicAPI.Unshipped.txt` files |
| **Total** | **23** | **13** | 4 pull requests: 14, 9, 15 and 14 files, each at most 15 files and 1500 lines |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0, no backward compatibility. Issue #266 adds stampede protection to
QueryCachingPipelineBehavior (src/Encina.Caching), which reads CacheEntry<TResponse> with GetAsync, runs nextStep() on a miss and
stores with SetAsync. The behavior never calls ICacheProvider.GetOrSetAsync. All 8 cache providers behave the same because the
protection lives in the behavior.

IMPLEMENTATION OVERVIEW:
1. PR 1: JitterPercent / DefaultJitterPercent, CacheExpiration.ApplyJitter, CachePatternSettings.Resolve, CacheEntry.ExpiresAtUtc.
2. PR 2: StampedeProtectionMode, ISingleFlightExecutor, SingleFlightResult, InProcessSingleFlightExecutor.
3. PR 3: StampedeProtection attribute/option, the flight in the miss path, CachePatternLog 450-451, CachingPatterns range 450-499.
4. PR 4: DistributedLock mode (IDistributedLockProvider, double-check read), XFetch early expiration, CachePatternLog.Stampede 452-455,
   CachePatternMetrics, Redis integration test.

KEY PATTERNS:
- Flight only after every bypass check and the key; a bypassed request is never coalesced.
- Followers share the leader's whole Either; a Left is never cached; the shared run uses the leader's token.
- Time from TimeProvider, randomness from a seam; logs carry redacted keys only; tag encina.tenant_id only with multi-tenancy on.
- No new EncinaError code; no new public symbol without PublicAPI.Unshipped.txt and guard tests.
- Follow the maintainer's answers to Design Choices 1, 3, 4, 5, 6, 7, 8, 10 and 11, given in your brief.

REFERENCE FILES:
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs
- src/Encina.Caching/ServiceCollectionExtensions.cs
- src/Encina.DistributedLock/IDistributedLockProvider.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- .github/coverage-manifest/Encina.Caching.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Include | Extends `QueryCachingPipelineBehavior` for all 8 providers; no provider change. |
| 2 | OpenTelemetry | ✅ Include | `Meter` `Encina.Caching` with the `encina.cache.stampede` counter; the tenant tag only with multi-tenancy on (ADR-054 D4); spans and meter registration with the OTel package belong to #723 (open). |
| 3 | Structured Logging | ✅ Include | `CachePatternLog` with EventIds 450-455 in the new `CachingPatterns` range. |
| 4 | Health Checks | ❌ N/A | A behavior-level pattern with no backend of its own; provider health is #754. |
| 5 | Validation | ✅ Include | Attribute and option values validated at resolution (Design Choice 7); no request input is added. |
| 6 | Resilience | ✅ Include | Single-flight is the resilience pattern; a lock timeout degrades to an uncoalesced run, never to a failure. |
| 7 | Distributed Locks | ✅ Include | The `DistributedLock` mode uses `IDistributedLockProvider`. |
| 8 | Transactions | ❌ N/A | Cache operations are independent of units of work; the behavior never calls `SaveChangesAsync`. |
| 9 | Idempotency | ❌ N/A | Coalescing concurrent identical reads is not message deduplication; a cache write is idempotent. |
| 10 | Multi-Tenancy | ✅ Include | The flight and lock keys are the existing tenant-qualified cache key; a request with no tenant under multi-tenancy bypasses the cache and so the flight (fail closed); the metric tag follows ADR-054 D4. Tenant quotas are #275. |
| 11 | Module Isolation | ⏭️ Defer | Module-scoped stampede keys follow the module dimension of the cache keys, tracked by #746 (open). |
| 12 | Audit Trail | ❌ N/A | A cache optimization with no compliance event. |

**Deferred items:** module-scoped keys (#746, open).

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| `QueryCachingPipelineBehavior`, `CachingOptions`, `IDistributedLockProvider` | ✅ Available | On `origin/main` |
| Block 450-499 of the EventId registry | ✅ Assigned | Plan batch registry |
| #3419 / ADR-062 error categories | ✅ Not needed | No error code is created |
| Maintainer answers to Design Choices 1, 3, 4, 5, 6, 7, 8, 10 and 11 | Pending | Asked when work starts |

#267, #268, #269 and #271 start after PR 1, PR 3 and PR 4 of this plan are merged (PR 4 creates `CachePatternMetrics.cs`). #277 (Memcached) needs nothing from this plan.

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas (Design Choices 1, 3, 4, 5, 6, 7, 8, 10 and 11) when work starts, records the answers under `## Maintainer Decisions`, then briefs four workers in the PR order (PR 1, PR 2, PR 3, PR 4).
2. After PR 3 merges, comment on #267, #268, #269 and #271 that the shared types and the EventId range are on `main`.
