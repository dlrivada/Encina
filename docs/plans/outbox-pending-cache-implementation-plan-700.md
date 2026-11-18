# Implementation Plan: Short-TTL cache in front of the outbox pending-batch query

> **Issue**: [#700](https://github.com/dlrivada/Encina/issues/700)
> **Type**: Feature
> **Complexity**: Medium (4 phases, 3 pull requests of 9, 13 and 2 files)
> **Estimated Scope**: about 350 lines of production code, about 900 lines of tests
> **Milestone**: v0.18.0 - Integration & Platform Capabilities (caching, messaging)

---

## Summary

The outbox processor of every provider derives from `OutboxProcessorBase` (`src/Encina.Messaging/Outbox/OutboxProcessorBase.cs`). Each cycle creates a scope, resolves `IOutboxStore`, and `OutboxBatchProcessor.ProcessAsync` calls `GetPendingMessagesAsync(BatchSize, MaxRetries)` once (the only production caller; the other sites are `InstrumentedOutboxStore` and the fake). The loop then waits `OutboxOptions.ProcessingInterval` (default 30 seconds, `OutboxOptions.cs` line 16; the wait is the `Task.Delay` at `OutboxProcessorBase.cs` line 109). The issue asks for a cache decorator with a short TTL in front of that read.

Re-checked against `origin/main` (d523eda92), the issue text does not fit the code in five places, and the plan resolves each one:

1. **The sketch is not the interface.** Every method returns `Task<Either<EncinaError, ...>>` and the batch is `IEnumerable<IOutboxMessage>`; the sketch returns `IReadOnlyList<IOutboxMessage>` and uses `GetOrSetAsync`, which cannot return a `Left`.
2. **`Encina.Messaging` cannot host the decorator.** `Encina.Caching` references `Encina.Messaging` (`Encina.Caching.csproj`), so the decorator and its options live in `Encina.Caching` (the placement of the family, as in #695). There is no per-provider registration to write: `RegisterOutbox` (`MessagingServiceCollectionExtensions.cs` line 281) registers `IOutboxStore` once for all providers, so the acceptance criterion "Opt-in registration across all 13 providers" is met by that single registration, and the "13" is a leftover count: the matrix of AGENTS.md section 5 is the 10 database providers, all covered by the one call.
3. **A cached non-empty batch makes duplicate delivery systematic.** The stores have no claim: no `UPDLOCK`, no `SKIP LOCKED`, no lease (the SQL Server query is `SELECT TOP (@BatchSize) ... WHERE ProcessedAtUtc IS NULL ... ORDER BY CreatedAtUtc`), and the sibling plan #713 states that two replicas polling at once already publish the same rows twice. A distributed cache that hands the same batch to every replica inside the TTL turns that race into the normal case.
4. **The issue's invalidation is expensive and early.** `RemoveByPatternAsync($"{prefix}:pending:*")` on every `AddAsync` runs `server.KeysAsync` over the keyspace on Redis (`RedisCacheProvider.cs` lines 104-113), on the write path of every business transaction; and for EF Core `MarkAsProcessedAsync` and `AddAsync` only record changes on the context, which become visible at `SaveChangesAsync` (`OutboxProcessorBase.cs` line 157, `OutboxWriter.cs` lines 71-78), so an eviction at the write is followed by a refill from the not yet updated database.
5. **The default TTL does not fit the default interval.** With `ProcessingInterval` at 30 seconds and the issue's 10-second TTL, a single processor never hits its own cache; the gain exists only with several replicas on a shared cache, or with an interval below the TTL. The plan ships the benchmark that measures it and documents the condition.

**Affected packages:** `Encina.Caching` (decorator, options, registration). No provider package and no `Encina.Messaging` change. The shared foundation (`StoreCacheKey`, `StoreCachingRegistration`, `StoreCacheTelemetry`, `StoreCacheLog` ids 350-353, `StoreCacheErrors`, the file `StoreCachingServiceCollectionExtensions.cs`) is owned by #695; this plan builds on it and adds no EventId and no error code.

**Provider category**: Database (10) for the decorated stores (the decorator is provider-independent; the integration scenario runs against all ten); Caching (8) for the cache behind it, consistent because the decorator uses only `ICacheProvider` (Get/Set/Remove, TTL, serialization abstraction). Memory (L1) runs the scenario against the ten real stores; Redis runs one integration class with the existing `RedisCollection` fixture; Valkey, Dragonfly, Garnet and KeyDB share the Redis wrapper and are covered by that run; Hybrid has no run of its own (the same follow-up as the inbox plan, #695); Memcached (planned, #277) needs no change.

**Sibling plans (family)**: #695 (foundation), #696 (saga), #699 (choreography), #697, #698 (merged or merging, read `docs/plans`), #713 (outbox processing lock: with one replica polling at a time the cache saves little; the two features are independent and both opt-in), #2408 (post-commit invalidation) and #2407 (DSR cache purge, `ICachePurgePatternSource`).

---

## Design Choices

<details>
<summary><strong>1. Where the decorator lives and how it is applied</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Encina.Caching`, folder `MessagingStores/`, applied by a public `AddEncinaOutboxCaching(Action<OutboxCachingOptions>?)` that replaces the `IOutboxStore` descriptor through `StoreCachingRegistration.Decorate<IOutboxStore>` and throws `InvalidOperationException` at registration when no outbox store is registered** | The only buildable placement; one call covers all ten providers; a missing store is a loud error; the same call-order rule as #695 | Must be called after the provider registration |
| **B) A seam in `Encina.Messaging`** (`RegisterOutbox` resolves decorators from the container; `OutboxOptions.EnableCaching` turns it on, as the issue's configuration shows) | Order independent; matches the issue's configuration | Changes the core registration used by every provider and test; the flag would do nothing without `Encina.Caching` |
| **C) A new `Encina.Messaging.Caching` package** | `Encina.Caching` stays free of messaging stores | A package (csproj, README, manifest, PublicAPI) for about 350 lines; it references both packages anyway |

### Chosen Option: **A - `Encina.Caching` with a call-order extension that fails fast** (recommended, pending the maintainer)

### Rationale

- The placement (A over B and C) follows the circular reference between the two packages; the call-order rule is the question #695 puts to the maintainer (its Design Choice 2), and this plan follows whatever the maintainer answers there. B wins when applications register caching in a module that runs before the messaging provider and the maintainer accepts a seam in core `Encina.Messaging`.
- The decorator is scoped like the store it wraps (`RegisterOutbox` registers `IOutboxStore` as scoped; the processor resolves it per cycle from a scope); it receives only singletons besides the inner store (`ICacheProvider`, options, `TimeProvider`, logger, `StoreCacheTelemetry`), so no scoped service reaches a singleton.
- Class: Dilemma

</details>

<details>
<summary><strong>2. What is cached</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The pending batch, as the issue asks: key `{prefix}:x-store:outbox:pending:{maxRetries}:{batchSize}`, value an entry list that a hit returns as copies of the messages** | The largest reduction of reads when the outbox is busy; the issue's design | Every replica that hits receives the same rows and publishes them again (finding 3); the entry holds `Content`, the serialized notification, in plaintext in a shared cache; every write needs a pattern eviction (finding 4); a hit returns copies, never the provider entity |
| **B) Only the empty result: key `{prefix}:x-store:outbox:empty:{maxRetries}` and a marker value; a hit returns `Right` with no messages; a non-empty result is never cached** | The common cost of an idle outbox (a poll that finds nothing) is removed; no message content or personal data in the cache; no duplicate delivery can be caused by the cache; eviction is one exact-key removal, never a pattern | No gain while the outbox is busy; a message added by another replica is not seen until the marker expires or is evicted (at most the TTL) |
| **C) Do not cache; reduce idle polling in the processor** (a back-off of `ProcessingInterval` while polls are empty) | No staleness at all | Changes the processor loop and its options in `Encina.Messaging`; not the feature the issue asks for |

### Chosen Option: **B - cache only the empty result** (recommended, pending the maintainer)

### Rationale

- A wins when the application runs one processor on a distributed lock (#713), tolerates at-least-once duplicates and wants the largest read reduction under load, and accepts a pattern eviction per write; B wins in every deployment that cannot afford the systematic duplicates, which is why it is the recommendation. C wins if the maintainer prefers the saving without any cache (a smaller, separate change). The acceptance criterion "no increase in message processing latency greater than the TTL" holds for B and A alike: a marker or entry never lives longer than `PendingBatchCacheDuration`.
- The deviation from the issue is stated for the maintainer; if A is chosen, PR 1 adds `CachedOutboxEntry`, `CachedOutboxMessage : IOutboxMessage` (a copy that ignores setter writes, as in #695) and a pattern eviction through `StoreCacheKey.PatternFor`, and the integration scenario adds the duplicate-delivery case; the other phases are unchanged.
- Class: Dilemma

</details>

<details>
<summary><strong>3. When the cache is evicted</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Evict on every state-changing call that returns `Right`, including `MarkAsProcessedAsync` (so `AddAsync`, `MarkAsProcessedAsync`, `MarkAsFailedAsync`, `MarkAsExhaustedAsync`, `RequeueExhaustedAsync` with a positive result), and again after `SaveChangesAsync` returns `Right` when this decorator instance saw any such call since the last save; never evict on `Left`; never cache a `Left`. It is one removal of an exact key under Design Choice 2 option B and a pattern removal under option A** | Meets the issue's criterion literally ("Invalidate on AddAsync, MarkAsProcessedAsync, MarkAsFailedAsync"); correct for every option of Design Choice 2, because under the full-batch option a processed message must leave the cached batch; correct for the providers that apply a write at once (ADO, Dapper, MongoDB) and for EF Core, whose changes land at `SaveChangesAsync` (`OutboxWriter.cs` lines 71-78, `OutboxProcessorBase.cs` line 157) | Two eviction points and a per-instance dirty flag; a removal per processed message that is a no-op when only the empty marker is cached |
| **B) The same eviction set without `MarkAsProcessedAsync`** | Saves one removal per processed message | Valid only if Design Choice 2 keeps the empty marker (a processed message only removes pending rows); the issue's criterion is dropped, and the set must be reopened if the full batch is chosen |
| **C) Evict at the write only (the issue)** | Simplest | For EF Core a concurrent poll refills the cache before the row changes in the database |

### Chosen Option: **A - evict at every write and after the save** (recommended, pending the maintainer)

### Rationale

- B wins when the maintainer keeps the empty marker and wants to avoid one cheap removal per processed message; A wins because it holds for every option of Design Choice 2, meets the acceptance criterion as written, and costs a no-op removal. C leaves a concrete stale sequence on EF Core. A `Left` is never swallowed: it is returned untouched (AGENTS.md section 3).
- Class: Dilemma
- A message added inside a business transaction that commits later (the `TransactionalOutboxPipelineBehavior` path commits with the unit of work, not through `IOutboxStore.SaveChangesAsync`) can still be hidden for at most the TTL; the documentation says so and names the TTL as the latency bound.

</details>

<details>
<summary><strong>4. Defaults and limits of `OutboxCachingOptions`</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `PendingBatchCacheDuration` 10 seconds (the issue's default), setter accepts 1 to 300 seconds; no `EnableCaching` flag, the extension call is the opt-in** | The issue's default; the upper bound keeps the latency bound meaningful; pay-for-what-you-use (AGENTS.md section 1) | At the default interval (`OutboxOptions.cs` line 16, 30 seconds) a single processor never hits; the range is the plan's own |
| **B) Range 5 to 30 seconds (the issue's summary says "5-30 seconds") and the `EnableCaching` flag of the issue's configuration** | Follows the issue's text | A flag that does nothing without `Encina.Caching`; a narrower range than some deployments need |
| **C) Default equal to `OutboxOptions.ProcessingInterval`** | Hits for a single processor | Doubles the worst-case delivery latency of a message added while a marker is cached; couples two options |

### Chosen Option: **A - 10 seconds, 1 to 300, no flag** (recommended, pending the maintainer)

### Rationale

- B wins when the maintainer wants the text of the issue followed exactly; C wins for a deployment that runs one processor and accepts a larger delay; A wins by default. The default of 10 seconds is the issue's acceptance criterion; the range and the missing flag are decisions of this plan, so they are asked here.
- The key carries `maxRetries` and, under Design Choice 2 option B, no `batchSize` (an empty result is empty for every batch size); under option A of that choice the key also carries `batchSize`, as the issue's key does.
- Class: Dilemma

</details>

<details>
<summary><strong>5. Cache failure semantics</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A failing cache call is caught, counted and logged by exception type (`ForLogging()`), and the call falls through to the inner store; with `CachingOptions.ThrowOnCacheErrors` the decorator returns `Left` with `store_cache.failed` (`ErrorCategory.Internal`, ADR-062 rule R4)** | A cache outage never stops delivery; one existing switch; no new error code | The reuse of a global flag |
| **B) Always return `Left` on a cache error** | Loud | A Redis outage stops the outbox processor |

### Chosen Option: **A - fall through, strict switch returns `Left`** (recommended, pending the maintainer)

### Rationale

- The same question as Design Choice 6 of #695; the answer given there applies to every decorator of the family. A wins for availability; B wins in deployments that prefer a loud failure. A `Left` from the inner store is never swallowed and never cached under either option (AGENTS.md section 3). The error code `store_cache.failed` belongs to #695; this plan adds none.
- Class: Dilemma

</details>

<details>
<summary><strong>6. Keys, tenant and telemetry</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `StoreCacheKey.Build(prefix, null, "outbox", "empty", maxRetries)` (no tenant segment); the shared `StoreCacheTelemetry` with `store=outbox` and outcomes `hit`, `miss`, `skipped`, `failure`, plus the evictions counter; the shared log ids 350-353 of #695** | Injective by construction; nothing new to register; no key, id or content in a tag or log | The log lines cannot tell the outbox from the inbox beyond the `store` argument |
| **B) New EventIds and a meter of its own** | More precise | Ids that duplicate the shared meaning; a second meter for the same concept |

### Chosen Option: **A - shared builder, shared telemetry, shared ids**

### Rationale

- **Class:** Trivial
- **Settled by:** AGENTS.md section 7 (new EventIds only from a registered range; the family reuses 350-353) and ADR-054 D4 and D6 (a tenant tag only when a tenant scope exists; no keys or data in telemetry). `IOutboxMessage` has no tenant member (`IOutboxMessage.cs`), and `GetPendingMessagesAsync` takes none, so there is no tenant segment and no tenant tag; when a tenant column is added to the outbox, the key gains the tenant segment of `StoreCacheKey.Tenant` (injective, with the reserved `default` tenant distinct from "no tenant") in the same change.

</details>

---

## Implementation Phases

### Phase 1: Outbox decorator, options and registration (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

Prerequisite: PR 1 and PR 2 of #695 are merged (`StoreCacheKey`, `StoreCachingRegistration`, `StoreCacheTelemetry`, `StoreCacheLog`, `StoreCacheErrors`, `StoreCachingServiceCollectionExtensions`). PR 1 holds exactly these 9 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/MessagingStores/CachingOutboxStoreDecorator.cs` | new (internal sealed) |
| 2 | `src/Encina.Caching/MessagingStores/OutboxCachingOptions.cs` | new (public) |
| 3 | `src/Encina.Caching/MessagingStores/StoreCachingServiceCollectionExtensions.cs` | modify (adds `AddEncinaOutboxCaching`) |
| 4 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 5 | `tests/Encina.UnitTests/Caching/MessagingStores/CachingOutboxStoreDecoratorTests.cs` | new |
| 6 | `tests/Encina.GuardTests/Caching/OutboxCachingGuardTests.cs` | new |
| 7 | `tests/Encina.UnitTests/Caching/MessagingStores/OutboxCachingRegistrationTests.cs` | new (DI test with `ValidateOnBuild` and `ValidateScopes`, both orders with the `Encina.OpenTelemetry` store instrumentation) |
| 8 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 9 | `changelog.d/700-outbox-pending-cache.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Create** `internal sealed class CachingOutboxStoreDecorator : IOutboxStore` with the constructor `(IOutboxStore inner, ICacheProvider cache, IOptions<OutboxCachingOptions> options, IOptions<CachingOptions> cachingOptions, TimeProvider timeProvider, StoreCacheTelemetry telemetry, ILogger<CachingOutboxStoreDecorator> logger)`. `GetPendingMessagesAsync`: build the key from `maxRetries`; a marker hit returns `Right` with an empty sequence (counted `hit`); a miss calls the inner store, returns a `Left` untouched, writes the marker only when the batch is empty, and returns the inner result (a non-empty batch counts `skipped`, reason `not_empty`, event 353). `AddAsync`, `MarkAsFailedAsync`, `MarkAsExhaustedAsync` and `RequeueExhaustedAsync` (positive result) call the inner store and, on `Right`, remove the key and set the dirty flag; `SaveChangesAsync` calls the inner store and, on `Right` with the flag set, removes the key and clears the flag. `GetPendingCountAsync` and `GetExhaustedCountAsync` pass through; `MarkAsProcessedAsync` evicts like the other writes (Design Choice 3). Every cache call is wrapped (Design Choice 5).
2. **Create** `public sealed class OutboxCachingOptions { TimeSpan PendingBatchCacheDuration (default 10 seconds, 1 to 300 seconds, setter throws ArgumentOutOfRangeException) }`.
3. **Add** `public static IServiceCollection AddEncinaOutboxCaching(this IServiceCollection services, Action<OutboxCachingOptions>? configure = null)` to `StoreCachingServiceCollectionExtensions`: `ArgumentNullException.ThrowIfNull(services)`; `Configure<OutboxCachingOptions>`; `TryAddSingleton<StoreCacheTelemetry>` and `TryAddSingleton(TimeProvider.System)` (already present from #695, `TryAdd` keeps it idempotent); then `StoreCachingRegistration.Decorate<IOutboxStore>`. The decorator is built by a factory descriptor, which `ValidateOnBuild` cannot analyze, so a missing `ICacheProvider` fails at the first resolution of `IOutboxStore` (the first processor cycle); the DI test builds the provider with `ValidateOnBuild` and `ValidateScopes` with a cache provider registered and asserts the missing-provider exception on first resolution. A cache provider is a documented prerequisite.
4. **Tests (unit)**: an empty result writes the marker and the next call is a hit without calling the inner store; a non-empty result is never cached; a `Left` is returned untouched and never cached; `AddAsync`, `MarkAsProcessedAsync`, `MarkAsFailedAsync`, `MarkAsExhaustedAsync` and a positive `RequeueExhaustedAsync` evict on `Right` and not on `Left` (Design Choice 3 option A); `SaveChangesAsync` evicts only when a write happened in this instance and not after a `Left`; the two counts do not touch the cache; the marker TTL is the configured duration (`FakeTimeProvider` and a fake cache); keys differ per `maxRetries`; a throwing cache falls through, counts and logs by type; with `ThrowOnCacheErrors` it returns `Left` with `store_cache.failed` and category `Internal`; every other member delegates exactly once.
5. **Tests (guard)**: `AddEncinaOutboxCaching(null)` throws; the options setter rejects 0, negative and above-300-second values. The decorator constructor is internal and `Encina.Caching` has no `InternalsVisibleTo` for `Encina.GuardTests`, so its null checks are asserted by the unit tests (guard target 0 with that reason).
6. **Manifest, PublicAPI and changelog** through `mechanical-fixer` (fragment text: "Added opt-in `AddEncinaOutboxCaching`: a short-TTL cache of the empty outbox poll for any of the ten database providers").

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #700: a short-TTL cache in front of IOutboxStore.GetPendingMessagesAsync, in
Encina.Caching. #695 already added StoreCacheKey, StoreCachingRegistration, StoreCacheTelemetry, StoreCacheLog (350-353) and
StoreCacheErrors. IOutboxStore methods return Task<Either<EncinaError, ...>>.

TASK:
1. Create CachingOutboxStoreDecorator, OutboxCachingOptions and AddEncinaOutboxCaching as described in the plan.
2. Cache only the EMPTY pending result (a marker); never a non-empty batch; evict at every state-changing Right and after SaveChangesAsync.
3. Write the unit, guard and DI tests; ask mechanical-fixer for the PublicAPI line, the manifest and the changelog fragment.

KEY RULES:
- A Left from the inner store is returned untouched and never cached; a cache failure falls through (ThrowOnCacheErrors returns Left).
- The key is StoreCacheKey.Build(...); no id, key or content in a log or tag; exceptions through ForLogging().
- Never inject a scoped service into a singleton: the decorator is scoped, the telemetry is a singleton.
- No EventId and no error code are added; reuse the shared ones. Time comes from TimeProvider. Zero warnings; CRAP of every method at most 10.

REFERENCE FILES:
- src/Encina.Messaging/Outbox/IOutboxStore.cs, OutboxBatchProcessor.cs, OutboxProcessorBase.cs, OutboxWriter.cs
- src/Encina.OpenTelemetry/MessagingStores/InstrumentedOutboxStore.cs
- src/Encina.Testing.Fakes/Stores/FakeOutboxStore.cs
- src/Encina.Caching/MessagingStores (from #695)
```

</details>

### Phase 2: Integration scenario over the ten providers and a Redis run (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

PR 2 holds exactly these 13 files:

| # | File | Change |
|---|------|--------|
| 1 | `tests/Encina.IntegrationTests/Messaging/Outbox/OutboxCachingScenario.cs` | new (shared scenario) |
| 2-4 | `tests/Encina.IntegrationTests/ADO/{SqlServer,PostgreSQL,MySQL}/Outbox/OutboxStoreADOCachingTests.cs` | new (3) |
| 5-7 | `tests/Encina.IntegrationTests/Dapper/{SqlServer,PostgreSQL,MySQL}/Outbox/OutboxStoreDapperCachingTests.cs` | new (3) |
| 8-10 | `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/{SqlServer,PostgreSQL,MySQL}/Outbox/OutboxStoreEF<Db>CachingTests.cs` | new (3) |
| 11 | `tests/Encina.IntegrationTests/Infrastructure/MongoDB/Stores/OutboxStoreMongoDBCachingTests.cs` | new |
| 12 | `tests/Encina.IntegrationTests/Infrastructure/Caching/OutboxCachingRedisTests.cs` | new (`[Collection(RedisCollection.Name)]`, an in-test inner store, `RedisCacheProvider`: marker round trip, TTL and eviction on a real server) |
| 13 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`: the integration targets of the decorator) |

The folders are the ones that hold today's `OutboxStore*Tests.cs` (verified on `origin/main` for ADO, Dapper, MongoDB and the three EF folders); the new classes use the same `[Collection("<Family>-<Database>")]` fixtures and the harness of `OutboxRetryScenarios`, with `MemoryCacheProvider`, so no container is added. `Encina.IntegrationTests` already references `Encina.Caching`, `Encina.Caching.Memory` and `Encina.Caching.Redis`, so no csproj edit is needed. Valkey, Dragonfly, Garnet and KeyDB are covered by the Redis run (shared wrapper); Hybrid has no run (follow-up of #695).

Scenario list: (1) an empty outbox: the second poll is a hit (inner call count 0); (2) a message added after the marker was written is returned as soon as `AddAsync` and `SaveChangesAsync` returned (eviction), proven for EF Core where the row exists only after the save; (3) a failed message with a next retry is fetched again no later than the TTL after it becomes due (the pending query filters `NextRetryAtUtc <= now`, so a live empty marker can hide it until the marker expires or is evicted); (4) `RequeueExhaustedAsync` makes the exhausted message pending at once; (5) a message committed by another instance after the marker was written is returned no later than the TTL (`FakeTimeProvider`); (6) a throwing cache falls through and the batch is still returned; (7) a non-empty batch is returned from the store on every poll (never cached).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
PR 1 of issue #700 is merged: AddEncinaOutboxCaching decorates IOutboxStore with an empty-result marker. This PR proves it against the ten
database providers and one Redis server.

TASK:
1. Write OutboxCachingScenario (static asserts like OutboxRetryScenarios) and one thin class per provider using its shared collection fixture.
2. Cover the seven scenarios of the plan, including the EF Core eviction after SaveChangesAsync and the bound by the TTL.
3. Write OutboxCachingRedisTests with the existing RedisCollection fixture; ask mechanical-fixer for the integration targets.

KEY RULES:
- [Collection("<Family>-<Database>")] shared fixtures only; ClearAllDataAsync in InitializeAsync; never dispose the shared fixture.
- Integration tests execute the real stores and the real decorator; no mocks of either.
- No Thread.Sleep: FakeTimeProvider for the TTL.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Messaging/Outbox/OutboxRetryScenarios.cs
- tests/Encina.IntegrationTests/ADO/PostgreSQL/Outbox/OutboxStoreADOTests.cs
- tests/Encina.IntegrationTests/Infrastructure/Caching/Fixtures/RedisFixture.cs
```

</details>

### Phase 3: Observability, logging and coverage targets

<details>
<summary><strong>Tasks</strong></summary>

1. **Telemetry**: the shared `StoreCacheTelemetry` of #695 with `store=outbox`: `encina.store_cache.requests` (outcomes `hit`, `miss`, `skipped`, `failure`) and `encina.store_cache.evictions`. No key, message id or content in a tag or span (ADR-054 D6). No tenant tag: the outbox has no tenant scope (ADR-054 D4).
2. **Logs**: the shared ids 350-353 (read failed, write failed, eviction failed, entry not cached with a reason code); exceptions through `ForLogging()`. No new EventId, so nothing is registered in `EventIdRanges.cs` and the family block 400-449 is not used by this plan.
3. **Error code**: the shared `store_cache.failed`, `ErrorCategory.Internal` (ADR-062 rule R4, provider failures); no new code.
4. **Coverage targets** (per file, per flag; baseline: the new files do not exist, the manifest `targets` of `Encina.Caching` are unit 60, guard 30, contract 10; measured values replace the estimates before each pull request opens, measured with the commands of AGENTS.md section 9):

| File | PR | unit | guard | property | integration | Justification |
|------|----|------|-------|----------|-------------|---------------|
| `MessagingStores/CachingOutboxStoreDecorator.cs` | 1 (integration in 2) | 95 | 0 | 0 | 70 | Every branch unit tested with a fake store and cache; internal type without `InternalsVisibleTo` for `Encina.GuardTests`; the PR 2 scenario reaches hit, miss, eviction and fall-through against real stores |
| `MessagingStores/OutboxCachingOptions.cs` | 1 | 100 | 100 | 0 | 0 | Setter validation fully reachable from both flags; no integration surface |
| `MessagingStores/StoreCachingServiceCollectionExtensions.cs` (added method) | 1 | 95 | 90 | 0 | 0 | DI test and guard tests reach every line; integration uses it indirectly |

5. **Contract and load**: no contract test (no new public interface; the decorated interface keeps its contract, asserted by unit tests with the real `FakeOutboxStore` behind it); no load test (the decorator holds no state beyond one dirty flag per scope).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
The outbox cache decorator of issue #700 exists. Telemetry, logs and the error code are the shared ones of #695; this phase measures coverage
per flag and records the targets.

TASK:
Run each flag's test project with coverage, compare with the targets of the plan, and ask mechanical-fixer to record the targets and the
one-sentence justifications in .github/coverage-manifest/Encina.Caching.json (the PR 1 rows in PR 1, the integration columns in PR 2).

KEY RULES:
- No new EventId and no new error code; use the shared ones; no key, id or content in a log or tag.
- A target never goes down; documentation never types coverage figures.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- docs/architecture/adr/054-observability-conventions-and-trace-propagation.md
- docs/architecture/adr/062-error-category-contract.md
- .github/coverage-manifest/Encina.Caching.json
```

</details>

### Phase 4: Benchmark and documentation (pull request 3)

<details>
<summary><strong>Tasks</strong></summary>

PR 3 holds exactly these 2 files:

| # | File | Change |
|---|------|--------|
| 1 | `tests/Encina.BenchmarkTests/Encina.Caching.Benchmarks/OutboxPendingCacheBenchmarks.cs` | new (cached versus uncached idle poll over an in-benchmark inner store with a fixed latency and `MemoryCacheProvider`, built through the public `AddEncinaOutboxCaching`; `BenchmarkSwitcher` already runs the project; verify the filter with `--list flat`) |
| 2 | `docs/messaging/outbox.md` | new (`docs-writer`: the opt-in cache, the call-order rule, the empty-result rule, the latency bound by the TTL, the condition under which a single processor hits, and the relation with the processor lock of #713; no hand-typed figures) |

1. XML documentation (`<summary>`, `<remarks>`, `<param>`, `<example>`) on `AddEncinaOutboxCaching` and `OutboxCachingOptions` is part of PR 1; the call-order rule and the empty-result rule are stated there.
2. The benchmark is the evidence for the statement in finding 5 of the Summary: it reports the idle poll with and without the cache; the maintainer reads it before deciding whether the feature stays enabled by default in examples.
3. Build with `dotnet build --configuration Release` (0 warnings), `dotnet test` of each flag, `dotnet run --file .github/scripts/coverage-report.cs -- --check-justifications`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
Issue #700 is implemented in PR 1 and PR 2. PR 3 adds the benchmark and the outbox documentation page.

TASK:
Write the benchmark in tests/Encina.BenchmarkTests/Encina.Caching.Benchmarks (public AddEncinaOutboxCaching only), verify the filter with
--list flat, ask docs-writer for docs/messaging/outbox.md, and run the build and the coverage checks.

KEY RULES:
- State the call-order rule, the empty-result rule and the TTL latency bound in the page; never promise a gain for a busy outbox or a
  single processor on the default interval.
- Documentation cites coverage with cov: markers, never typed numbers.

REFERENCE FILES:
- docs/messaging/inbox.md
- changelog.d/README.md
- tests/Encina.BenchmarkTests/Encina.Caching.Benchmarks/CacheInvalidationBenchmarks.cs
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| AGENTS.md section 5 (database, caching) | The decorator is consistent across the 10 database providers and the 8 cache providers (Get/Set/Remove, TTL) |
| AGENTS.md section 3 | ROP, `TimeProvider`, errors never swallowed, exceptions through `ForLogging()`, registration completeness proven by a DI test |
| Outbox pattern (at-least-once publishing; the stores have no claim) | The reason a non-empty batch is not cached |
| ADR-054 D4, D6 | No tenant tag without a tenant scope; no keys or data in telemetry |
| ADR-062 | The shared error code states its `ErrorCategory` (`Internal`) |
| #712 | Cache Integration epic that this issue belongs to |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| ICacheProvider | src/Encina.Caching/Abstractions/ICacheProvider.cs | the cache the decorator reads and writes |
| StoreCacheKey, StoreCachingRegistration, StoreCacheTelemetry, StoreCacheLog, StoreCacheErrors | src/Encina.Caching/MessagingStores (#695) | the shared foundation |
| CachingOptions | src/Encina.Caching/Configuration/CachingOptions.cs | `KeyPrefix` and `ThrowOnCacheErrors` |
| IOutboxStore | src/Encina.Messaging/Outbox/IOutboxStore.cs | the decorated abstraction |
| OutboxBatchProcessor, OutboxProcessorBase, OutboxWriter | src/Encina.Messaging/Outbox | the callers of the pending query and of the save |
| RegisterOutbox | src/Encina.Messaging/MessagingServiceCollectionExtensions.cs | registers the store once for all providers |
| InstrumentedOutboxStore, DecorateService | src/Encina.OpenTelemetry | existing store decorator layered with the cache decorator |
| FakeOutboxStore | src/Encina.Testing.Fakes/Stores/FakeOutboxStore.cs | unit-test double of the inner store |
| OutboxRetryScenarios | tests/Encina.IntegrationTests/Messaging/Outbox/OutboxRetryScenarios.cs | reusable per-provider outbox harness |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Caching` (range `CachingStores` of #695) | 350-353 reused | No new ids; the family block 400-449 is used by #704 and #705, not by this plan |

### Estimated File Count

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | 2 | 1 | New: decorator, options; modified: `StoreCachingServiceCollectionExtensions.cs` |
| Tests | 16 | 0 | New: decorator unit, guard, DI (PR 1); scenario, 10 provider classes, Redis class (PR 2); benchmark (PR 3) |
| Documentation | 1 | 0 | outbox page |
| Configuration | 1 | 2 | New: changelog fragment; modified: `Encina.Caching.json` (PR 1 and PR 2), `PublicAPI.Unshipped.txt` |
| **Total** | **20** | **3** | 3 pull requests: 9, 13 and 2 files (24 file touches: the manifest is touched in PR 1 and PR 2, so 23 distinct files), each at most 15 files and about 800 lines |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0, no backward compatibility. Issue #700: an opt-in cache decorator for
IOutboxStore.GetPendingMessagesAsync in Encina.Caching, on the foundation of #695 (StoreCacheKey, StoreCachingRegistration,
StoreCacheTelemetry, StoreCacheLog 350-353, StoreCacheErrors).

IMPLEMENTATION OVERVIEW:
1. PR 1: CachingOutboxStoreDecorator (caches only the empty pending result as a marker; evicts at each state-changing Right and after
   SaveChangesAsync), OutboxCachingOptions (10 seconds, 1 to 300), AddEncinaOutboxCaching, unit, guard and DI tests, manifest, changelog.
2. PR 2: integration scenario over the 10 database providers with a Memory cache and one Redis-backed class; integration targets.
3. PR 3: the benchmark (public extension only) and docs/messaging/outbox.md.

KEY PATTERNS:
- A non-empty batch is never cached (the stores have no claim; a shared batch means duplicate delivery).
- A Left is returned untouched and never cached; a cache failure falls through (ThrowOnCacheErrors returns Left store_cache.failed, Internal).
- Keys go through StoreCacheKey; no key, id or content in logs or tags; exceptions through ForLogging(); no tenant tag (no tenant scope).
- The extension fails at registration when IOutboxStore is not registered yet (call it after the provider).
- No scoped service in a singleton; DI test with ValidateOnBuild and ValidateScopes in both orders with Encina.OpenTelemetry.
- Apply Design Choices 1 to 5 as the maintainer answers them; until then build the recommended option of each.

REFERENCE FILES:
- src/Encina.Messaging/Outbox/IOutboxStore.cs, OutboxBatchProcessor.cs, OutboxProcessorBase.cs, OutboxWriter.cs
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs, MessagingStores/InstrumentedOutboxStore.cs
- src/Encina.Caching/MessagingStores
- .github/coverage-manifest/Encina.Caching.json
- tests/Encina.IntegrationTests/Messaging/Outbox/OutboxRetryScenarios.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Include | The feature is a short-TTL cache of the empty outbox poll; opt-in, cache-aside, bounded by the TTL. |
| 2 | OpenTelemetry | ✅ Include | The shared counters with `store=outbox`; no keys, ids or content; no tenant tag because the outbox operations have no tenant scope (ADR-054 D4, D6). |
| 3 | Structured Logging | ✅ Include | The shared `[LoggerMessage]` ids 350-353 of #695; exceptions through `ForLogging()`. |
| 4 | Health Checks | ❌ N/A | The cache is optional and its failure falls through; the cache providers have their own health checks. |
| 5 | Validation | ✅ Include | The options setter rejects values outside 1 to 300 seconds; the key builder rejects an empty segment. |
| 6 | Resilience | ✅ Include | A cache outage never stops delivery (fall through); `ThrowOnCacheErrors` is the strict switch; no retry (the next poll retries naturally). |
| 7 | Distributed Locks | ⏭️ Defer | The processing lock is #713 (open, planned); the cache and the lock are independent and both opt-in; with the lock the cache saves little, which the page states. |
| 8 | Transactions | ✅ Include | Nothing is written to the cache before a commit: only an empty read is cached, and eviction follows the write and the save; the decorator never commits (`SaveChangesAsync` is the inner store's), so the plan claims bounded staleness (the TTL), not atomicity. |
| 9 | Idempotency | ✅ Include | Not caching a non-empty batch is what keeps the cache from adding duplicate deliveries; dispatch idempotency itself is #1220. |
| 10 | Multi-Tenancy | ✅ Include | The outbox row has no tenant today, so the key has none; `StoreCacheKey.Tenant` is injective and ready for a tenant column; stated, not invented. |
| 11 | Module Isolation | ❌ N/A | The outbox is not module scoped. |
| 12 | Audit Trail | ❌ N/A | A cache lookup is not an audit event; the outbox row is the record. |

**Deferred items:** none needs a new issue: the processing lock is #713 (open); the Hybrid-cache run of the store scenarios is the follow-up of #695; if the maintainer chooses Design Choice 2 option C, the processor back-off is a separate change in `Encina.Messaging` and would be opened then.

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| `ICacheProvider` and the Caching provider packages | ✅ Available | Memory, Redis, Valkey, Dragonfly, Garnet, KeyDB, Hybrid exist; Memcached is planned (#277) |
| Outbox stores of the 10 database providers | ✅ Available | ADO x3, Dapper x3, EF Core x3, MongoDB |
| #695 PR 1 and PR 2 (foundation) | ⏭️ Sibling | `StoreCacheKey`, `StoreCachingRegistration`, `StoreCacheTelemetry`, `StoreCacheLog`, `StoreCacheErrors`, `StoreCachingServiceCollectionExtensions` must be merged first; PR 1 of this plan builds on them |
| #713 (outbox processing lock) | ⏭️ Sibling | Not required |
| Maintainer answers to Design Choices 1 to 5 | Pending | Asked when work starts |

One real prerequisite: #695 PR 1 and PR 2.

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas (Design Choices 1 to 5) when work starts, records the answers in a `## Maintainer Decisions` section, then briefs three workers in the phase order, after #695 PR 2 is merged.
2. After PR 3, comment on #700 with the measured benchmark so the maintainer can judge the gain of the empty-poll cache against the processing interval in use.
