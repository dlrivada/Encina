# Implementation Plan: Cache auto-recovery and self-healing

> **Issue**: [#278](https://github.com/dlrivada/Encina/issues/278)
> **Type**: Feature
> **Complexity**: High (5 pull requests of 11, 15, 8, 12 and 11 files)
> **Estimated Scope**: about 900 lines of production code, about 1,800 lines of tests
> **Milestone**: v0.18.0 — Integration & Platform Capabilities

---

## Summary

Issue #278 asks for retry with exponential backoff, a circuit breaker, automatic reconnection (`ICacheConnectionManager`), fallback strategies (skip the cache, local memory, a secondary provider), Polly integration and health reporting for cache operations. Re-checked against `origin/main` (ce352001f):

| Premise of the issue | State in the code |
|----------------------|-------------------|
| "`ThrowOnCacheErrors = false` degrades gracefully" | True and it is the whole mechanism today: `QueryCachingPipelineBehavior` catches each failing call (`TryGetFromCacheAsync`, `TryCacheResultAsync`), logs it and continues. Nothing short-circuits: while the backend is down, every request still pays the provider's timeout on the read and again on the write before the handler result is returned. |
| Retry types such as `RedisTimeoutException` | `Encina.Caching` has no StackExchange.Redis reference; those types are known only to `Encina.Caching.Redis`. A provider-agnostic decorator needs a classifier supplied by the provider package. |
| Auto-reconnect is a feature to build | The Redis family shares one `IConnectionMultiplexer` singleton (`ConnectionMultiplexer.Connect(...)` in `src/Encina.Caching.Redis/ServiceCollectionExtensions.cs`); the client library maintains its own connections. What Encina can add is detection and an explicit reconfigure, which is exactly what `ICacheConnectionManager` would expose. Memory has nothing to reconnect; `HybridCacheProvider` delegates L2 to `HybridCache`, which reports no state; Memcached (#277) will bring its own client. |
| The factory of `GetOrSetAsync` is cache work | It is user code (`Func<CancellationToken, Task<T>>`). A decorator that counted its exceptions as cache failures would open the breaker because a handler threw. |
| Polly integration | `Encina.Polly` exists (`src/Encina.Polly`) but is a pipeline-behavior package for requests; `Encina.Caching` has no Polly reference (`Encina.Caching.csproj`), and `Polly` 8.6.6 is pinned centrally. |
| Health: "see #754" | #754 (open, plan on main) adds `MemoryCacheHealthCheck` and `HybridCacheHealthCheck`; the recovery check here reports breaker state, not provider reachability. |

The plan puts recovery in a decorator over `ICacheProvider` (`ResilientCacheProvider`), so the 8 cache providers (Memory, Hybrid, Redis, Valkey, Dragonfly, Garnet, KeyDB and the planned Memcached of #277) share one implementation and one behavior, for the `[Cache]` pipeline and for direct `ICacheProvider` callers alike. Everything is opt-in through `AddEncinaCacheAutoRecovery()`; no hosted service starts and no decorator exists without it.

**Shared pieces and ownership (family cache-patterns-1 and -2):** #266 owns `CachePatternMetrics` (a partial file is added in PR 4, after #266 PR 4 merges). #268 (fail-safe, stale data when the handler fails) and this plan address different failures (the handler versus the cache backend) and do not share code. EventIds: this plan's range `CachingRecovery` 30000-30011, the slot 30000-30049 of the Caching block 30000-30999 of the 10000+ zone (AGENTS.md section 7, `docs/engineering/eventid-block-registry.md`).

**Affected packages:** `Encina.Caching` (core), `Encina.Caching.Redis`, `Encina` (EventId registry). **Provider category:** Caching (8): Redis, Valkey, Dragonfly, Garnet, KeyDB share the Redis connection manager; Memory and Hybrid use the always-connected default; Memcached through #277.

---

## Design Choices

<details>
<summary><strong>1. Where recovery lives (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A decorator `ResilientCacheProvider : ICacheProvider` in `Encina.Caching`, registered by `AddEncinaCacheAutoRecovery()` around the registered provider** | One implementation for all 8 providers; covers direct `ICacheProvider` use and every behavior that uses it (query caching, invalidation, idempotency) | Wraps only `ICacheProvider`: pub/sub and distributed locks are other interfaces and are not covered |
| **B) Inside `QueryCachingPipelineBehavior`** | The behavior already catches per call | Direct users and the invalidation and idempotency behaviors get nothing |
| **C) A new package on Polly pipelines (`Encina.Caching.Resilience`)** | Polly's mature breaker and telemetry | A new package and dependency; Polly pipelines do not give the cache-aware states the issue lists |

### Chosen Option: **A - provider decorator** (recommended, pending the maintainer)

### Rationale

- Recovery is about the backend, and the decorator sits at the backend's interface. Option C wins for a team that already standardizes on Polly pipelines (see Design Choice 3).
- Registration order: the decorator replaces the registered `ICacheProvider` descriptor, so `AddEncinaCacheAutoRecovery()` must be called after the provider (`AddEncinaRedisCache`, ...); calling it before throws `InvalidOperationException` at registration naming the missing provider. Providers use `TryAdd`, so a decorator registered first would silently make the provider's own registration a no-op: that is why the check is explicit and tested in both orders.
- Class: Dilemma

</details>

<details>
<summary><strong>2. Operations, cancellation and the user factory</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Every `ICacheProvider` call is guarded; a cancelled caller token rethrows `OperationCanceledException` and never counts as a failure or a retry; in `GetOrSetAsync` an exception from the factory propagates untouched and never counts, and when the circuit is open the factory runs directly** | The breaker measures the backend only; cancellation is never a `Left` or a trip | A flag per call to know where an exception came from |
| **B) Count every exception from any call** | Simplest | A throwing handler opens the breaker; a caller cancelling requests degrades the cache for everyone |

### Chosen Option: **A - backend failures only**

### Rationale

- **Class:** Settled
- **Settled by:** AGENTS.md section 3 (cancellation is not an error and propagates; `Left` and exceptions of user code are not swallowed) and the signature `Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory, ...)` in `ICacheProvider.cs` (the factory is the caller's code).
- The interaction with `ThrowOnCacheErrors = true` is a separate decision (Design Choice 9).

</details>

<details>
<summary><strong>3. The breaker implementation and Polly (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An internal `CacheCircuitBreaker` (closed, open, half-open; failure ratio over a sampling window with `MinimumThroughput`, break duration, one probe call in half-open; `TimeProvider`)** | No dependency; cache-aware; fully unit testable with `FakeTimeProvider`; no static or generic-class state | One more breaker in the repository |
| **B) Polly 8 `ResiliencePipeline` inside `Encina.Caching`** | Mature, the issue's "integration with Polly" | A Polly dependency for every caching user (AGENTS.md section 1, pay-for-what-you-use); the breaker state is hidden from the health check |
| **C) A separate `Encina.Caching.Polly` adapter package** | Opt-in Polly | A second package and a second set of tests for what A already provides |

### Chosen Option: **A - internal breaker** (recommended, pending the maintainer)

### Rationale

- The issue lists "Integration with Polly" as a task; option A answers it with "no dependency" and option C is the path if the maintainer wants one. Option C wins for a team whose Polly pipelines already emit telemetry they want to reuse.
- Options: `FailureRatio` (default 0.5), `SamplingDuration` (30 s), `MinimumThroughput` (10), `BreakDuration` (30 s) as in the issue's example (which uses a count; a ratio with a throughput floor avoids tripping on three failures out of ten thousand).
- Class: Dilemma

</details>

<details>
<summary><strong>4. Which failures count and what is retried (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An `ICacheFailureClassifier` (public) decides transient failures: the default accepts `TimeoutException`, `IOException` and `SocketException`; `Encina.Caching.Redis` adds the StackExchange.Redis types through its own classifier; retries are off by default and, when on, apply to writes and removals only (a retried read delays the response while the handler could already be running)** | Provider-specific knowledge stays in the provider package; reads fail fast | The issue's `RetryableExceptions` list becomes an interface |
| **B) A `Type[] RetryableExceptions` option, as in the issue, applied to every operation** | Matches the issue text | Applications must know each provider's exception types; a read retried up to `MaxRetries` times adds up to hundreds of milliseconds to a request that could run the handler |

### Chosen Option: **A - classifier, retries for writes only** (recommended, pending the maintainer)

### Rationale

- Option B wins when reads must be retried, for example a cache whose miss would overload the database. The retry delay is `RetryDelay` times `RetryBackoffMultiplier` to the attempt number (defaults 100 ms and 2.0, three attempts, off); all cache operations are idempotent, so repeating a write is safe.
- Class: Dilemma

</details>

<details>
<summary><strong>5. Fallback strategies (Dilemma D5, the issue's acceptance criterion "fallback to local cache")</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `SkipCache` only** | Smallest; always correct | The acceptance criterion "fallback to local cache option" is not met |
| **B) `SkipCache` and `UseLocalMemory`: while the circuit is open, reads and writes go to a small in-process store (bounded by `LocalFallbackMaxEntries`, entries live at most `LocalFallbackMaxDuration`, default 30 s), cleared when the circuit closes; removals also remove locally; `UseSecondaryProvider` becomes an issue file** | Meets the criterion; the outage does not send all load to the database; entries are short lived and dropped on recovery | A local copy can be stale if another node changed the value during the outage; the short lifetime bounds that |
| **C) All three, with a secondary provider** | Matches the issue | Needs a second `ICacheProvider` registration (keyed or named), a policy for writes to two backends and two sets of health states |

### Chosen Option: **B - skip and local memory** (recommended, pending the maintainer)

### Rationale

- Option A wins if the maintainer judges staleness during an outage unacceptable; option C wins for an application that runs a standby cache. `artifacts/issues/cache-recovery-secondary-provider-fallback.md` records the secondary provider.
- Pattern removal: `CacheInvalidationPipelineBehavior` removes through `RemoveByPatternAsync`; while the circuit is open the decorator clears every local entry whose key matches the pattern (the glob rules of the provider contract: `*`, `?`), and when it cannot evaluate the pattern it clears the whole local store, so an invalidation is never outlived by a local copy. `SetWithSlidingExpirationAsync` in fallback stores the entry with the shorter of the sliding window and `LocalFallbackMaxDuration`; `RefreshAsync` extends nothing.
- The local store keeps object references (no serialization), keyed by the same tenant-qualified key as the backend, so tenants cannot read each other's local entries; its keys are never logged unredacted.
- Class: Dilemma

</details>

<details>
<summary><strong>6. Connection manager and automatic reconnection (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A public `ICacheConnectionManager` (`IsConnectedAsync`, `ReconnectAsync`, `ConnectionStateChanged`) with an always-connected default in core, a Redis implementation (connection state and `ReconfigureAsync`) in `Encina.Caching.Redis` for the five Redis-protocol products, and an opt-in `CacheRecoveryHostedService` that calls `ReconnectAsync` every `ReconnectInterval` while the circuit is open, at most `MaxReconnectAttempts` per outage** | The issue's interface; honest about what each provider can do | Hybrid and Memory report always connected; the hosted service is one more moving part |
| **B) No manager: the breaker's half-open probe is the only recovery** | Smaller | `ReconnectAsync` is the only way to force the client to re-establish a stuck connection; the issue's interface disappears |

### Chosen Option: **A - manager with a Redis implementation** (recommended, pending the maintainer)

### Rationale

- Option B wins if the client's own reconnection is judged sufficient. The loop catches per iteration and never lets an exception escape (AGENTS.md section 3, background loops; EventId 30010), a `ConnectionStateChanged` handler that throws is caught and logged (EventId 30011), a cancelled stopping token ends the loop without an error, and after `MaxReconnectAttempts` the loop stops trying for that outage (EventId 30009) while the breaker's half-open probes continue.
- The hosted service is registered only when `EnableAutoReconnect` is on (default off).
- Class: Dilemma

</details>

<details>
<summary><strong>7. What the health check reports (Dilemma D7)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Closed is `Healthy`, half-open and open are `Degraded`** | A cache outage that the application survives does not remove instances from rotation | A readiness probe never fails on a cache outage |
| **B) Open is `Unhealthy`** | The outage is loud | Orchestrators restart or drain every instance for a failure the application handles |

### Chosen Option: **A - degraded** (recommended, pending the maintainer)

### Rationale

- The check follows `EncinaHealthCheck` (a `DefaultName` const, static tags) and registers only with the recovery feature. Option B wins when the cache is a hard dependency of correctness. The result carries the state and the time since it opened, never a key or an exception message (AGENTS.md section 3).
- Class: Dilemma

</details>

<details>
<summary><strong>8. Scope of the breaker, defaults and telemetry</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One breaker per cache backend (provider instance), everything off by default; the metric has no tenant tag** | A backend outage is not a tenant event; no cost when off | A noisy tenant's failing keys cannot be isolated (they are not the cause of a backend failure) |
| **B) A breaker per tenant or module** | Isolation | One shared-backend failure would be counted separately per tenant, delaying the trip for each |

### Chosen Option: **A - one breaker per provider, opt-in, no tenant tag**

### Rationale

- **Class:** Trivial
- **Settled by:** AGENTS.md section 1 (pay-for-what-you-use) and ADR-054 D4 (the tenant tag is for tenant-scoped facts; the failure domain here is the shared cache server, which no tenant owns). This holds wherever the recovery code sits (Design Choice 1): the failure being measured is the backend's.
- Time comes from `TimeProvider`; no static field of a generic class holds options; no scoped service is injected into a singleton.

</details>

<details>
<summary><strong>9. Combining recovery with `ThrowOnCacheErrors` (Dilemma D9)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reject the combination: an `IValidateOptions<CacheAutoRecoveryOptions>` that reads `IOptions<CachingOptions>` fails the host at start (`ValidateOnStart`) when `ThrowOnCacheErrors` is true** | One meaning per option; the check runs after every `Configure` has been applied, not when `AddEncinaCacheAutoRecovery` is called (a lazy `Configure<CachingOptions>` from `AddEncinaCaching` is not known then) | The two options cannot be mixed |
| **B) The decorator honors the option: backend failures are rethrown after the breaker records them, and an open circuit throws instead of skipping** | Applications that want errors still get the breaker's metrics and health | An open circuit then fails requests, which is the opposite of the feature's goal |

### Chosen Option: **A - reject at start** (recommended, pending the maintainer)

### Rationale

- The existing contract of the option is "errors propagate" (`CachingOptions.cs`), recovery's is "degrade". Option B wins for an application that wants the breaker's observability while still failing fast. Option validation (ranges, ratio, tiers) also moves to `IValidateOptions` with `ValidateOnStart`, so it runs once at host start with the final option values.
- Class: Dilemma

</details>

---

## Implementation Phases

### Phase 1: Options, classifier and the breaker (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

PR 1 holds exactly these 11 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Recovery/CacheAutoRecoveryOptions.cs` | new (public: `Retry`, `CircuitBreaker`, `Fallback`, `EnableAutoReconnect`, `ReconnectInterval`, `MaxReconnectAttempts`, `Validate()`, and the public enum `CacheFallbackStrategy` declared here with `SkipCache`; `UseLocalMemory` is added by PR 3 only if Design Choice 5 is answered B or C) |
| 2 | `src/Encina.Caching/Recovery/ICacheFailureClassifier.cs` | new (public interface) |
| 3 | `src/Encina.Caching/Recovery/DefaultCacheFailureClassifier.cs` | new (internal sealed, plus the internal `CacheFailureClassification` (the composite, a distinct type from `ICacheFailureClassifier`, which is the member type): transient or backend failure when any registered classifier says so; the decorator takes the composite, so the provider packages' classifiers add to the defaults instead of replacing them) |
| 4 | `src/Encina.Caching/Recovery/CacheCircuitBreaker.cs` | new (internal sealed state machine) |
| 5 | `src/Encina.Caching/Recovery/CacheCircuitState.cs` | new (public enum: `Closed`, `Open`, `HalfOpen`) |
| 6 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 7 | `tests/Encina.UnitTests/Caching/CacheCircuitBreakerTests.cs` | new (`FakeTimeProvider`) |
| 8 | `tests/Encina.UnitTests/Caching/CacheAutoRecoveryOptionsTests.cs` | new (includes the classifier and composite tests) |
| 9 | `tests/Encina.GuardTests/Caching/CacheAutoRecoveryOptionsGuardTests.cs` | new |
| 10 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 11 | `changelog.d/278-cache-recovery-options.added.md` | new (`mechanical-fixer`) |
Tasks:

1. **Create** the options with the defaults of Design Choices 3, 4, 5 and 6 (everything off; ranges validated: ratio between 0 and 1 exclusive, positive durations, `MaxRetries` 1 to 10, backoff multiplier at least 1) and `Validate()` (throws `InvalidOperationException` naming the member, never a value).
2. **Create** `ICacheFailureClassifier` (`bool IsTransient(Exception exception)`, `bool IsBackendFailure(Exception exception)`) and the default classifier (cancellation is neither).
3. **Create** `CacheCircuitBreaker`: `TryAcquire()` (allowed or not, one probe in half-open), `RecordSuccess()`, `RecordFailure()`, `State`, `OpenedAtUtc`, a state-change callback; thread-safe with a lock (a lock-free compare of two fields would let two probes through); the sampling window is a ring of buckets, not a list of timestamps.
4. **Tests (unit):** closed to open at the ratio with the throughput floor, open to half-open after `BreakDuration`, one probe only, probe success closes, probe failure re-opens, window expiry, concurrency (many threads, one probe), option validation, classifier cases. Guard tests: the options' setters and the registration are public: setters reject out-of-range values (the guard file covers them); the breaker and default classifier are internal (no `InternalsVisibleTo` for `Encina.GuardTests` in `Encina.Caching.csproj`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #278 adds recovery around ICacheProvider. Encina.Caching has no Polly or
StackExchange.Redis reference. Time must come from TimeProvider.

TASK:
1. Create CacheAutoRecoveryOptions (+ Validate), ICacheFailureClassifier, the default classifier, CacheCircuitState and CacheCircuitBreaker.
2. Write the unit and guard tests; ask mechanical-fixer for PublicAPI.Unshipped.txt, the manifest rows and the changelog fragment.

KEY RULES:
- Cancellation is neither a failure nor transient. Everything is off by default.
- The breaker is thread-safe (one lock), allows exactly one probe in half-open and keeps no static state.
- Follow the maintainer's answers to Design Choices 3 and 4, given in your brief.

REFERENCE FILES:
- src/Encina.Caching/Configuration/CachingOptions.cs
- src/Encina.Caching/Abstractions/ICacheProvider.cs
- tests/Encina.UnitTests/Caching/CachingOptionsTests.cs
```

</details>

### Phase 2: The decorator, retry, skip-cache and registration (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

PR 2 holds exactly these 15 files (PR 2 needs #266 PR 4, because it creates a metrics partial of `CachePatternMetrics`):

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Recovery/ResilientCacheProvider.cs` | new (internal sealed partial) |
| 2 | `src/Encina.Caching/Recovery/CacheRecoveryLog.cs` | new (EventIds 30000-30004) |
| 3 | `src/Encina.Caching/CacheAutoRecoveryServiceCollectionExtensions.cs` | new (public `AddEncinaCacheAutoRecovery`, and the validator `IValidateOptions<CacheAutoRecoveryOptions>` registered with `ValidateOnStart`, which runs `Validate()` and the Design Choice 9 rule against the final `CachingOptions`) |
| 4 | `src/Encina/Diagnostics/EventIdRanges.cs` | modify (`CachingRecovery = (30000, 30011)`) |
| 5 | `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` | modify (map `Encina.Caching`) |
| 6 | `src/Encina/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`: one line, `static readonly Encina.Diagnostics.EventIdRanges.CachingRecovery -> (int Min, int Max)`, for the new field) |
| 7 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 8 | `tests/Encina.UnitTests/Caching/ResilientCacheProviderTests.cs` | new |
| 9 | `tests/Encina.GuardTests/Caching/CacheAutoRecoveryServiceCollectionExtensionsGuardTests.cs` | new |
| 10 | `tests/Encina.ContractTests/Caching/ResilientCacheProviderContractTests.cs` | new (real decorator over a real `MemoryCacheProvider`) |
| 11 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 12 | `changelog.d/278-cache-circuit-breaker.added.md` | new (`mechanical-fixer`) |
| 13 | `tests/Encina.ContractTests/Encina.ContractTests.csproj` | modify (`ProjectReference` to `Encina.Caching.Memory`, which the contract project does not reference today) |
| 14 | `src/Encina.Caching/Diagnostics/CachePatternMetrics.Recovery.cs` | new (partial of #266's class: the `encina.cache.circuit_state` gauge and the `encina.cache.circuit_transitions` and `encina.cache.fallback_operations` counters, emitted by the decorator and the breaker callback of this PR) |
| 15 | `docs/engineering/eventid-block-registry.md` | modify (a row `CachingRecovery \| 30000-30011 \| Caching \| zone` in the "Registered ranges" table, and, in the "Blocks" table, the `reserved` row of the slot is replaced by a `registered` row with the exact bounds of the range, `30000-30011 \| Caching \| #278 cache auto-recovery \| registered` (rule 2 of `EventIdRegistryAgreementTests` requires a `registered` block to match a range of `EventIdRanges.cs` with the same bounds, and rule 3 forbids a `reserved` row overlapping a registered range); the rest of the slot, 30012-30049, is left unlisted or as a `free` row (not checked); read the test and the document first) |

The registration tests (both orders, `ValidateOnBuild`, `ValidateScopes`, the validator) and the metrics `MeterListener` tests are inside `ResilientCacheProviderTests.cs` (row 8). `src/Encina.Caching/Configuration/CachingOptions.cs` is not touched: the options live in `AddEncinaCacheAutoRecovery`'s own `IOptions<CacheAutoRecoveryOptions>`, so the hand-written copy list of `AddEncinaCaching` is unaffected and no `CachingOptions` member is added.

Tasks:

1. **Create** `ResilientCacheProvider(ICacheProvider inner, CacheFailureClassification classification, CacheCircuitBreaker breaker, IOptions<CacheAutoRecoveryOptions> options, ILogger<...>, TimeProvider? timeProvider = null)`; each method follows: breaker not acquirable then the fallback of the chosen strategy (`SkipCache`: `GetAsync` returns default, `SetAsync` and `RemoveAsync` complete, `ExistsAsync` false, `RefreshAsync` false, `GetOrSetAsync` runs the factory); acquirable then call the inner, `RecordSuccess` on return, `RecordFailure` and log when the classifier says backend failure and the token is not cancelled, then swallow (reads) or retry (writes, Design Choice 4) and finally swallow, because `ThrowOnCacheErrors` is rejected with this feature (Design Choice 9, as answered). `OperationCanceledException` from the caller's token rethrows. In `GetOrSetAsync` the factory is wrapped so an exception from it is captured, rethrown unchanged and not counted.
2. **EventIds:** 30000 circuit opened, 30001 circuit half-open, 30002 circuit closed, 30003 retry attempt, 30004 retries exhausted; logs carry the operation name and the exception type only, never a key, a message or a value.
3. **Create** `AddEncinaCacheAutoRecovery(this IServiceCollection, Action<CacheAutoRecoveryOptions>? = null)` (public; null services throws; a null delegate is valid): registers the options with the `IValidateOptions` validator and `ValidateOnStart` (validation runs at host start with the final values, not at registration), requires an existing `ICacheProvider` descriptor (otherwise `InvalidOperationException` naming the missing provider) whose lifetime is `Singleton` (a scoped or transient original, such as a custom provider, would put a short-lived service inside the singleton decorator, so the call throws `InvalidOperationException` naming the lifetime; tested), replaces it with a singleton factory that resolves the original registration and wraps it. Classifiers: every classifier (the default and each provider package's) is registered as `ICacheFailureClassifier` with `TryAddEnumerable`, and the decorator takes a distinct internal type `CacheFailureClassification` (the composite) that is registered with `TryAddSingleton` and receives `IEnumerable<ICacheFailureClassifier>`, so the composite is never a member of the sequence it consumes and `ValidateOnBuild` finds no cycle; the Redis classifier is added with `TryAddEnumerable` too and never replaces the default (a test builds the provider with the default plus the Redis classifier). The `CacheCircuitBreaker` is registered here as an internal singleton (`TryAddSingleton`, built from `IOptions<CacheAutoRecoveryOptions>` and `TimeProvider`), so the decorator and the PR 4 hosted service and health check resolve the same instance.
4. **Tests:** open after failures and skip afterwards, half-open probe closes, a thrown factory never trips the breaker, cancellation never counts, retries with `FakeTimeProvider` for writes only, registration before and after the provider (the first throws), `ThrowOnCacheErrors` rejected, the contract test drives the real decorator over the real in-memory provider and asserts the provider contract still holds with the circuit closed. Targets in Research.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
Phase 1 is merged: options, classifier and CacheCircuitBreaker exist. Providers register ICacheProvider with TryAddSingleton.

TASK:
1. Create ResilientCacheProvider, CacheRecoveryLog (30000-30004) and AddEncinaCacheAutoRecovery; register CachingRecovery = (30000, 30011) in
   EventIdRanges.cs and the assembly map.
2. Write the unit, registration, guard and contract tests; ask mechanical-fixer for the PublicAPI files, the manifest rows and the fragment.

KEY RULES:
- Backend failures only count; cancellation rethrows; a factory exception propagates unchanged and never counts.
- The decorator must be registered after the provider and throws a clear InvalidOperationException otherwise.
- Never log a key, an exception message or a value; time from TimeProvider; the decorator is a singleton over a singleton.
- Follow the maintainer's answers to Design Choices 1, 3, 4 and 5, given in your brief.

REFERENCE FILES:
- src/Encina.Caching/Abstractions/ICacheProvider.cs
- src/Encina.Caching.Memory/ServiceCollectionExtensions.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.ContractTests/Database/Caching/QueryCachingContractTests.cs
```

</details>

### Phase 3: Local memory fallback (pull request 3)

<details>
<summary><strong>Tasks</strong></summary>

PR 3 holds exactly these 8 files: `src/Encina.Caching/Recovery/LocalFallbackCache.cs` (new, internal sealed, bounded, `TimeProvider`), `src/Encina.Caching/Recovery/ResilientCacheProvider.cs` (modify), `src/Encina.Caching/Recovery/CacheRecoveryLog.cs` (modify: 30005 local fallback active, 30006 local fallback evicted at the bound), `src/Encina.Caching/PublicAPI.Unshipped.txt` (`mechanical-fixer`, the enum value), `tests/Encina.UnitTests/Caching/LocalFallbackCacheTests.cs` (new), `tests/Encina.UnitTests/Caching/ResilientCacheProviderTests.cs` (modify), `.github/coverage-manifest/Encina.Caching.json` (`mechanical-fixer`), `changelog.d/278-cache-local-fallback.added.md` (`mechanical-fixer`).

Tasks: `LocalFallbackCache` is a bounded `ConcurrentDictionary` of key to (object, expiry) with oldest-first eviction at `LocalFallbackMaxEntries` and a per-entry lifetime of at most `LocalFallbackMaxDuration`; it is cleared when the circuit closes (a state-change callback); a removal while open also removes the local entry; `GetOrSetAsync` in fallback uses the local store around the factory. Tests: tenant-qualified keys never cross, expiry with `FakeTimeProvider`, clear on close, the bound, concurrent writers. The value types are the caller's: a type mismatch on read returns a miss, never an exception.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
Phase 2 is merged: the decorator skips the cache while the circuit is open.

TASK:
Create LocalFallbackCache and wire the UseLocalMemory strategy into the decorator; add EventIds 30005-30006; write the tests; ask
mechanical-fixer for PublicAPI, the manifest rows and the fragment.

KEY RULES:
- Bounded, short lived, cleared on recovery; keys never logged unredacted.
- Only the maintainer's answer to Design Choice 5 decides whether this phase exists.

REFERENCE FILES:
- src/Encina.Caching/Recovery/ResilientCacheProvider.cs
- src/Encina.Caching.Memory/MemoryCacheProvider.cs
```

</details>

### Phase 4: Connection manager, reconnect service and health check (pull request 4)

<details>
<summary><strong>Tasks</strong></summary>

PR 4 holds exactly these 12 files (no file of PR 2 or PR 3 is modified: the metrics and transitions are emitted in PR 2, and `ConnectionStateChanged` is raised by each manager from its own connection state, not from breaker transitions):

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Recovery/ICacheConnectionManager.cs` | new (public interface and `CacheConnectionEventArgs`) |
| 2 | `src/Encina.Caching/Recovery/AlwaysConnectedCacheConnectionManager.cs` | new (internal default) |
| 3 | `src/Encina.Caching/Recovery/CacheRecoveryHostedService.cs` | new (internal sealed `BackgroundService`) |
| 4 | `src/Encina.Caching/Health/CacheRecoveryHealthCheck.cs` | new (`EncinaHealthCheck`) |
| 5 | `src/Encina.Caching/Recovery/CacheRecoveryLog.cs` | modify (EventIds 30007-30011, emitted by the hosted service and the health check path) |
| 6 | `src/Encina.Caching/CacheAutoRecoveryServiceCollectionExtensions.cs` | modify (manager, hosted service when on, health check) |
| 7 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 8 | `tests/Encina.UnitTests/Caching/CacheRecoveryHostedServiceTests.cs` | new (`FakeTimeProvider`; includes the registration assertions of this PR) |
| 9 | `tests/Encina.UnitTests/Caching/CacheRecoveryHealthCheckTests.cs` | new |
| 10 | `tests/Encina.GuardTests/Caching/CacheRecoveryGuardTests.cs` | new (the public `CacheConnectionEventArgs`) |
| 11 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 12 | `changelog.d/278-cache-reconnect-health.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Create** `ICacheConnectionManager` (the issue's three members; `IsConnectedAsync` and `ReconnectAsync` take a `CancellationToken` and propagate cancellation) and the always-connected default for Memory, Hybrid and any provider without a manager (`TryAdd`).
2. **Create** `CacheRecoveryHostedService`: registered only when `EnableAutoReconnect` is on; a `PeriodicTimer` loop with `TimeProvider`; while the breaker is open it calls `ReconnectAsync` (EventId 30007 attempt, 30008 failed) up to `MaxReconnectAttempts` per outage (EventId 30009), each iteration catches its failures and never lets an exception escape (EventId 30010), `OperationCanceledException` from the stopping token ends the loop quietly; it resolves nothing scoped. `ConnectionStateChanged` is raised by each manager from its own connection state (the Redis manager from the multiplexer's connection-lost and connection-restored events; the default never raises it) and each handler runs in a try/catch (EventId 30011).
3. **Create** `CacheRecoveryHealthCheck` (Design Choice 7; result data holds the state name and seconds open only); the metrics were created in PR 2.
4. **Tests:** reconnect only while open, bounded attempts per outage and reset after recovery, a throwing handler does not stop the loop, loop survives a throwing manager, health thresholds, DI with `ValidateOnBuild` and `ValidateScopes`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
Phases 1-3 are merged. #266 PR 4 created CachePatternMetrics.cs.

TASK:
Create ICacheConnectionManager and its default, CacheRecoveryHostedService and CacheRecoveryHealthCheck; add EventIds
30007-30011; write the tests; ask mechanical-fixer for PublicAPI, the manifest rows and the fragment.

KEY RULES:
- The loop catches per iteration and never lets an exception escape; cancellation of the stopping token ends it quietly.
- Reconnect is off by default; no scoped service is injected into a singleton; time from TimeProvider.
- Logs carry the operation name and exception type only.

REFERENCE FILES:
- src/Encina.Caching/Sharding/Services/TopologyRefreshHostedService.cs
- src/Encina.Messaging/Health/EncinaHealthCheck.cs
- src/Encina.Caching/Health/QueryCacheHealthCheck.cs
```

</details>

### Phase 5: The Redis-family manager and classifier (pull request 5)

<details>
<summary><strong>Tasks</strong></summary>

PR 5 holds exactly these 11 files (the eleventh is `.github/coverage-manifest/Encina.Caching.json`, which carries the integration target of 40 for `Encina.Caching/Recovery/ResilientCacheProvider.cs`; the Redis rows go in `Encina.Caching.Redis.json`; targets only rise, so the ratchet is not hit): `src/Encina.Caching.Redis/RedisCacheConnectionManager.cs` (new, public sealed, over `IConnectionMultiplexer`), `src/Encina.Caching.Redis/RedisCacheFailureClassifier.cs` (new, public sealed: `RedisConnectionException`, `RedisTimeoutException`, `RedisServerException` of a loading or busy state), `src/Encina.Caching.Redis/ServiceCollectionExtensions.cs` (modify: register both with `TryAddEnumerable` and `TryAddSingleton`), `src/Encina.Caching.Redis/PublicAPI.Unshipped.txt` (`mechanical-fixer`), `tests/Encina.UnitTests/Caching/Redis/RedisCacheConnectionManagerTests.cs` (new), `tests/Encina.UnitTests/Caching/Redis/RedisCacheFailureClassifierTests.cs` (new), `tests/Encina.GuardTests/Caching/Redis/RedisCacheConnectionManagerGuardTests.cs` (new), `tests/Encina.IntegrationTests/Infrastructure/Caching/RedisAutoRecoveryIntegrationTests.cs` (new, shared Redis fixture: the container is paused and unpaused), `.github/coverage-manifest/Encina.Caching.Redis.json` (`mechanical-fixer`), `changelog.d/278-cache-redis-recovery.added.md` (`mechanical-fixer`).

Tasks: the manager reports `IConnectionMultiplexer.IsConnected` and `ReconnectAsync` calls the multiplexer's `ConfigureAsync`, which accepts no cancellation token, so cancellation cannot interrupt it: the method checks the token before the call and again after it, throws `OperationCanceledException` when it was cancelled, and the hosted loop treats that exception from its stopping token as a quiet end; Valkey, Dragonfly, Garnet and KeyDB inherit both through `AddEncinaRedisCache` (their registration tests assert it, in the existing wrapper test files only if they fail). The integration test pauses the shared container and asserts the breaker opens, requests succeed through the handler, and the circuit closes after unpausing; it never creates its own container and never calls the fixture's `DisposeAsync`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
Phases 1-4 are merged. The Redis family shares RedisCacheProvider and one IConnectionMultiplexer singleton.

TASK:
Create RedisCacheConnectionManager and RedisCacheFailureClassifier, register them in AddEncinaRedisCache, write the unit, guard and
integration tests; ask mechanical-fixer for PublicAPI, the manifest rows and the fragment.

KEY RULES:
- Integration tests use the shared Redis collection fixture; never a per-class container.
- A cancelled token is never a failure; exception messages and keys are never logged.

REFERENCE FILES:
- src/Encina.Caching.Redis/ServiceCollectionExtensions.cs
- tests/Encina.IntegrationTests/Infrastructure/Caching/Fixtures/RedisFixture.cs
```

</details>

### Phase 6: Cross-cutting integration, observability and coverage targets

<details>
<summary><strong>Tasks</strong></summary>

1. **EventIds:** `CachingRecovery` 30000-30011 only (30000 opened, 30001 half-open, 30002 closed, 30003 retry, 30004 retries exhausted, 30005 local fallback active, 30006 local fallback evicted, 30007 reconnect attempt, 30008 reconnect failed, 30009 reconnect attempts exhausted, 30010 recovery loop iteration faulted, 30011 state handler threw); verified free by a repo-wide grep of `src/Encina.Caching` (used 3500-3513) and by the registry (no row in 30000-30999 yet); emitted only by `Encina.Caching`; the Redis package emits none.
2. **Error codes:** none are created: a backend failure becomes a miss or a skipped write inside the decorator, and the handler's own `Left` values flow through untouched; so no ADR-062 category applies.
3. **Coverage targets (per flag, set in the PR that adds each file):**

| File | Unit | Guard | Contract | Integration | Justification |
|------|:----:|:-----:|:--------:|:-----------:|---------------|
| `Encina.Caching/Recovery/CacheCircuitBreaker.cs` | 100 | 0 | 0 | 0 | Pure state machine with `FakeTimeProvider`; internal, no `InternalsVisibleTo` for the guard project |
| `Encina.Caching/Recovery/ResilientCacheProvider.cs` | 95 | 0 | 70 | 0 now, 40 in PR 5 | PR 2 sets unit 95 and contract 70 (its unit tests drive each operation and failure; its contract test runs the real decorator over the real in-memory provider); the integration target of 40 is set in PR 5, the PR that adds the Redis pause and unpause test |
| `Encina.Caching/Recovery/LocalFallbackCache.cs` | 100 | 0 | 0 | 0 | Bounded store driven by unit tests; internal |
| `Encina.Caching/Recovery/CacheRecoveryHostedService.cs` | 90 | 0 | 0 | 0 | Driven with `FakeTimeProvider`; internal |
| `Encina.Caching/Recovery/CacheAutoRecoveryOptions.cs`, `CacheAutoRecoveryServiceCollectionExtensions.cs` | 100 | 85 | 0 | 0 | Setter range guards and the registration null guard are covered by guard and registration tests |
| `Encina.Caching/Health/CacheRecoveryHealthCheck.cs`, `Recovery/DefaultCacheFailureClassifier.cs`, `AlwaysConnectedCacheConnectionManager.cs` | 100 | 0 | 0 | 0 | Small classes covered by unit tests; internal or without validated parameters |
| `Encina.Caching/Diagnostics/CachePatternMetrics.Recovery.cs`, `Recovery/CacheRecoveryLog.cs` | 90 | 0 | 0 | 0 | Instrument calls and generated bodies run in the tests above |
| `Encina.Caching/Recovery/ICacheFailureClassifier.cs`, `ICacheConnectionManager.cs`, `CacheCircuitState.cs` | 0 | 0 | 0 | 0 | Interfaces and an enum: no executable lines |
| `Encina.Caching.Redis/RedisCacheConnectionManager.cs`, `RedisCacheFailureClassifier.cs` | 95 | 70 | 0 | 50 | Unit tests with a mocked multiplexer; the integration flag runs the pause and unpause path |

   The worker measures every flag per PR with `coverage-report.cs`; no target is lowered (`target-ratchet`).
4. **Property, load, benchmark:** no property test beyond the breaker's unit grid; no load test (the breaker is a single lock; a contention benchmark is a follow-up in `Encina.Caching.Benchmarks`); no benchmark in these PRs.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
The five code PRs are merged. Verification remains.

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

### Phase 7: Documentation and finalization

<details>
<summary><strong>Tasks</strong></summary>

1. XML documentation with `<example>` on every public type; the remarks state registration after the provider, that pub/sub and distributed locks are not decorated, that a `Right` from `Send` never confirms a cache write and the staleness bound of the local fallback.
2. `docs-writer`: a "Resilience patterns" section in `docs/features/handler-caching.md` (the handler-caching page owned by the #266 family; `query-caching.md` is the EF Core page and is not touched) (the issue's documentation criterion), citing tests by name and no typed figure.
3. `PublicAPI.Unshipped.txt` lines are written by `mechanical-fixer` per PR; no ROADMAP, INVENTORY or ADR change (ADR number assigned when the ADR pull request opens, if the maintainer asks for one).
4. Verification per PR: `dotnet build --configuration Release` with zero warnings; `dotnet test` of `Encina.UnitTests` (Caching), `Encina.GuardTests`, `Encina.ContractTests` and, for PR 5, `Encina.IntegrationTests` (Redis); per-flag coverage and the CRAP table of changed methods.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
CONTEXT:
All code PRs are merged. Documentation remains.

TASK:
Confirm the XML docs and the five changelog fragments; ask docs-writer for the handler-caching.md section.

KEY RULES:
- No hand-typed figures; cite tests by name.
- Fragment section is added; never edit CHANGELOG.md.

REFERENCE FILES:
- docs/features/handler-caching.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| AGENTS.md section 3 | Cancellation propagates; background loops never throw; time from `TimeProvider`; no message in logs |
| AGENTS.md section 5 | A caching feature is consistent across the 8 providers: one decorator |
| ADR-054 D4 | The state of a shared backend carries no tenant tag |
| ADR-021 | EventId range registered before use |
| Circuit Breaker pattern (Nygard, Release It!; Microsoft Azure Architecture Center) | States and the single probe of Design Choice 3 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `ICacheProvider` and its registrations | `src/Encina.Caching/Abstractions/ICacheProvider.cs` | Decorated interface |
| `QueryCachingPipelineBehavior` | `src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs` | Existing per-call catch that the decorator makes cheap |
| `TopologyRefreshHostedService` | `src/Encina.Caching/Sharding/Services/` | Precedent for a `PeriodicTimer` loop |
| `EncinaHealthCheck`, `QueryCacheHealthCheck` | `src/Encina.Messaging/Health/`, `src/Encina.Caching/Health/` | Health check base and style |
| `CachePatternMetrics` (#266) | `src/Encina.Caching/Diagnostics/` | Extended by a partial file |
| `RedisFixture` | `tests/Encina.IntegrationTests/Infrastructure/Caching/Fixtures/RedisFixture.cs` | Shared container |
| `EventIdRanges`, `EncinaEventIdAllocationTests` | `src/Encina/Diagnostics/EventIdRanges.cs` | New range and assembly map |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Caching` (new range `CachingRecovery`, slot 30000-30049 of the Caching block 30000-30999) | 30000-30011 | This plan: 30000 circuit opened, 30001 half-open, 30002 closed, 30003 retry attempt, 30004 retries exhausted, 30005 local fallback active, 30006 local fallback evicted, 30007 reconnect attempt, 30008 reconnect failed, 30009 reconnect attempts exhausted, 30010 recovery loop iteration faulted, 30011 state handler threw. 30012-30049 stay free in the slot; #276 takes the next slot 30050-30099. The existing `Caching` range 3500-3549 is untouched |

### Estimated File Count

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | 15 | 5 | New: options, classifier and default, breaker, state enum, decorator, local store, manager and default, hosted service, health check, metrics partial, log, registration, two Redis types; modified: registration, log, Redis DI, `EventIdRanges.cs` |
| Tests | 16 | 4 | Unit (10), guard (3), contract (1), integration (1) and Redis unit tests; modified: allocation test, options tests, registration tests |
| Documentation | 0 | 1 | `docs/features/handler-caching.md` |
| Configuration | 5 | 6 | Five changelog fragments; PublicAPI files and manifests |
| **Total** | **36** | **16** | 5 pull requests of 11, 15, 8, 12 and 11 files, each at most 15 files and 1500 lines |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0, no backward compatibility. Issue #278 adds recovery around ICacheProvider: a decorator with
retry for writes, a circuit breaker, a local-memory fallback, a connection manager with an opt-in reconnect service and a health check.
All 8 cache providers behave the same because the decorator sits at the interface. Today a down backend costs every request its timeout.

IMPLEMENTATION OVERVIEW:
1. PR 1: options, classifier, CacheCircuitBreaker. PR 2: ResilientCacheProvider, AddEncinaCacheAutoRecovery, EventIds 30000-30004.
2. PR 3: LocalFallbackCache (EventIds 30005-30006). PR 4: connection manager, reconnect service, health check (EventIds 30007-30011).
3. PR 5: Redis connection manager and classifier with the Redis integration test.

KEY PATTERNS:
- Only backend failures count; cancellation rethrows; a GetOrSetAsync factory exception propagates untouched and never counts.
- Everything is opt-in; the decorator must be registered after the provider; ThrowOnCacheErrors with recovery is rejected at startup.
- Background loops catch per iteration; time from TimeProvider; logs carry operation names and exception types only.
- Every new public symbol has PublicAPI lines and guard tests; internal types are used only inside their assembly.
- Follow the maintainer's answers to Design Choices 1, 3, 4, 5, 6, 7 and 9, given in your brief.

REFERENCE FILES:
- src/Encina.Caching/Abstractions/ICacheProvider.cs
- src/Encina.Caching/Behaviors/QueryCachingPipelineBehavior.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- .github/coverage-manifest/Encina.Caching.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Include | Decorates `ICacheProvider`; identical on all 8 providers. |
| 2 | OpenTelemetry | ✅ Include | `encina.cache.circuit_state` gauge and transition and fallback counters in a `CachePatternMetrics` partial; spans belong to #723 (open). |
| 3 | Structured Logging | ✅ Include | `CacheRecoveryLog`, EventIds 30000-30011 in `CachingRecovery`. |
| 4 | Health Checks | ✅ Include | `CacheRecoveryHealthCheck` reports breaker state; provider reachability checks are #754 (open). |
| 5 | Validation | ✅ Include | `CacheAutoRecoveryOptions.Validate()` at registration. |
| 6 | Resilience | ✅ Include | The feature itself: retry for writes, breaker, fallback, reconnect. |
| 7 | Distributed Locks | ❌ N/A | Breaker state is per node and `IDistributedLockProvider` is not decorated. |
| 8 | Transactions | ❌ N/A | Cache operations are independent of units of work. |
| 9 | Idempotency | ❌ N/A | Cache operations are idempotent, which is what makes write retries safe; no deduplication is involved. |
| 10 | Multi-Tenancy | ❌ N/A | A backend outage is not tenant scoped and the decorator sees keys, not tenants; local fallback keys are the tenant-qualified keys of the generator and are never logged unredacted. |
| 11 | Module Isolation | ❌ N/A | Same reason; module-scoped keys are #746 (open). |
| 12 | Audit Trail | ❌ N/A | Infrastructure state, not a compliance event. |

**Deferred items:** secondary provider fallback (`artifacts/issues/cache-recovery-secondary-provider-fallback.md`), tracing of recovery (#723, open), provider reachability checks (#754, open).

---

## Prerequisites & Dependencies

Registry: the EventIds are in the 10000+ zone (AGENTS.md section 7): no range of this plan is below 10000, so `EventIdRegistryAgreementTests` rule 5 holds; the Caching slots are packed in order (#278 30000-30049, then #276 30050-30099).

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| `ICacheProvider`, the eight provider registrations, `EncinaHealthCheck` | ✅ Available | On `origin/main` |
| #266 PR 4 (`CachePatternMetrics.cs`) | Open | PR 2 of this plan needs it (it creates the metrics partial) |
| Slot 30000-30049 of the Caching block of the EventId registry | Reserved by this plan | A row `30000-30049 \| Caching \| #278 cache auto-recovery \| reserved` in the "Blocks" table, added when the plan merges or by the first code PR; the code PR in PR 2 replaces it by `30000-30011 ... registered` (exact bounds) |
| Maintainer answers to Design Choices 1, 3, 4, 5, 6, 7 and 9 | Pending | Asked when work starts |

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas (Design Choices 1, 3, 4, 5, 6, 7 and 9) when work starts, records the answers under `## Maintainer Decisions`, then briefs five workers in PR order.
2. Open the follow-up issue file for the secondary provider fallback.
