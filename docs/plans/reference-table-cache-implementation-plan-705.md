# Implementation Plan: Distributed cache for reference table entity lists with replication-driven invalidation

> **Issue**: [#705](https://github.com/dlrivada/Encina/issues/705)
> **Type**: Feature
> **Complexity**: Medium-High (5 phases, 5 pull requests of 5, 12, 14, 14 and 3 files)
> **Estimated Scope**: about 600 lines of production code, about 1300 lines of tests
> **Milestone**: v0.18.0 - Integration & Platform Capabilities (caching, sharding)

---

## Summary

Reference tables (ADR-013, `src/Encina/Sharding/ReferenceTables`) are replicated from one primary shard to every other shard. The issue asks for a `CachedReferenceTableStore` decorator over `IReferenceTableStore.GetAllAsync` with a long TTL, invalidated through the CDC cache handler and broadcast through pub/sub.

Re-checked against `origin/main` (ec129cae5), the issue text does not fit the code in five places, and the plan resolves each one:

1. **The sketch is not the interface.** `IReferenceTableStore.GetAllAsync<TEntity>(CancellationToken)` takes no `shardId`, returns `Task<Either<EncinaError, IReadOnlyList<TEntity>>>` and operates on one shard connection; a store is created per shard by `IReferenceTableStoreFactory.CreateForShard(connectionString)`.
2. **Decorating the store would serve stale data to the replicator.** The only production callers of the store reads are the replication path: `ReferenceTableReplicator.ReplicateEntityAsync` reads the primary with `GetAllAsync` (line 158) and `PollingRefreshDetector` compares `GetHashAsync` (line 82). A cached `GetAllAsync` would replicate stale rows to every shard, and a cached `GetHashAsync` can never detect a change inside the TTL. No other component reads reference data through `IReferenceTableStore`; applications read their local replica with ordinary repositories. The cache that has value is a new read API for application reads (Design Choice 1).
3. **The CDC handler of the issue does not match.** `QueryCacheInvalidationCdcHandler` is a `JsonElement` handler that removes `{CacheKeyPrefix}:*:{entityType}:*` keys of the query cache (default prefix `sm:qc`) by a table-name mapping. Reference tables with `RefreshStrategy.CdcDriven` are already handled by `CdcDrivenRefreshHandler<TEntity>`, which calls `IReferenceTableReplicator.ReplicateAsync<TEntity>`; the polling and manual strategies call the same replicator. The replicator is the one seam shared by all three strategies, so invalidation hooks there (Design Choice 2).
4. **Two of the three cache keys of the issue are not cacheable.** The content hash (`hash:...`) is what `PollingRefreshDetector` compares to detect a change, and `metadata:...` is the in-process reflection metadata of `EntityMetadataCache` (a `ConcurrentDictionary`, not serializable state). Neither is cached (Design Choice 5).
5. **The Memory provider hands out the same instances.** `MemoryCacheProvider` stores object references (`_cache.Set`, line 90), while Redis serializes; a caller that mutates a cached entity would change it for every caller on the Memory cache only (Design Choice 4).

**Affected packages:** `Encina` (one small helper so the cache and the replicator resolve the primary shard identically, PR 1), `Encina.Caching` (cache, replicator decorator, subscriber, options, logs). `Encina.Cdc` is **not** changed. No provider package changes.

**Provider category**: Database (10) for the stores read through the cache (the cache is provider-independent; the integration scenario runs against all ten); Caching (8) for the cache behind it, consistent because it uses only `ICacheProvider` (Get/Set/Remove, TTL, serialization abstraction) and `IPubSubProvider` where a broadcast is wanted (Memcached has no pub/sub and needs none: its removal is global). Memory (L1) runs the scenario against the ten real stores; Redis runs one integration class with the existing `RedisCollection` fixture; Valkey, Dragonfly, Garnet and KeyDB share the Redis wrapper; Hybrid has no run of its own (the follow-up of #695); Memcached (planned, #277) needs no change.

**Sibling plans (family)**: #695 owns the foundation this plan builds on (`StoreCacheKey`, `StoreCachingRegistration`, `StoreCacheTelemetry`, `StoreCacheLog` ids 350-353, `StoreCacheErrors`); #696, #697, #698, #699 are its siblings; #2407 owns the DSR cache purge and the tenant namespace `{prefix}:t:{escaped tenant}:` (reference data has no tenant, so the purge never touches these keys); #2408 owns post-commit invalidation (not needed here: invalidation follows a replication, which is itself after the commit).

---

## Design Choices

<details>
<summary><strong>1. What is cached and through which API</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `CachedReferenceTableStore` as the issue sketches, produced by decorating `IReferenceTableStoreFactory`** | The issue's design; one place | The replicator and the polling detector go through the same factory: replication would copy stale rows and change detection would be blind for the TTL (finding 2) |
| **B) A new read API `IReferenceTableCache.GetAllAsync<TEntity>(ct)` in `Encina.Caching`: cache-aside over the store of the primary shard (resolved from `IShardTopologyProvider` and the registered configuration); the replicator and the detector keep using the raw factory** | Replication keeps reading the source of truth; one cache entry per entity type (replicas hold identical data, so a per-shard key would multiply memory by the shard count); the API is what an application needs | A new public interface instead of a decorator; applications must call it to benefit |
| **C) Decorate the store, and add a "bypass" argument to `IReferenceTableStoreFactory.CreateForShard` for the replicator** | Keeps the decorator shape | Changes an interface implemented by ten providers and every test double, for a decorator nobody in the repository would call without bypass |

### Chosen Option: **B - a read API over the primary shard** (recommended, pending the maintainer)

### Rationale

- C wins when applications already call `IReferenceTableStore.GetAllAsync` directly and the maintainer accepts an interface change across the ten providers; B wins now because it breaks nothing in the replication path and the repository has no such caller. A is a concrete failure (replicating stale rows) and stays in the table only to record why it is rejected.
- Key: `StoreCacheKey.Build(prefix, null, "ref-table", entityTypeId)` where `entityTypeId` is `typeof(TEntity).FullName` (never the simple name: two entity types with the same name in different namespaces would share an entry, the defect recorded in #2275 for rate limit buckets). No tenant segment: the replicator copies the whole table to every shard, so a table that holds tenant-owned rows is not a reference table. The value is a `TEntity[]`; a hit returns it as `IReadOnlyList<TEntity>`.
- An unregistered entity type returns `Left` with the existing `encina.reference_table.entity_not_registered`; an unresolvable primary the existing `encina.reference_table.primary_shard_not_found`; a store `Left` is returned as it is and never cached. No new error code is added by this feature.
- Class: Dilemma

</details>

<details>
<summary><strong>2. What invalidates an entry</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A decorator of `IReferenceTableReplicator` (`ReplicateAsync<TEntity>` and `ReplicateAllAsync`) that removes the entry after the inner call returns, whether `Right` (including a partial result) or `Left`** | One seam for the three refresh strategies (CDC-driven, polling, manual); needs no CDC package; no table-name mapping; a failed or partial replication may have changed rows, so a `Left` also evicts | The entry is invalidated after replication finishes, not at the moment of the commit on the primary |
| **B) Extend `QueryCacheInvalidationCdcHandler` (the issue)** | Immediate invalidation when CDC sees the change; reuses the existing pub/sub flow | Only for CDC deployments; the handler works on table names and the query-cache prefix `sm:qc`, so a mapping from table to entity type and a second key family must be kept in step; polling and manual tables are never invalidated |
| **C) A TTL only** | No hook | Up to the TTL of stale data after every change |

### Chosen Option: **A - invalidate from the replicator decorator** (recommended, pending the maintainer)

### Rationale

- B wins when an application needs the cache to drop an entry the instant the primary commits, before a large replication finishes, and runs CDC for the table; A wins because it covers all three strategies with one seam and the CDC path reaches it anyway (`CdcDrivenRefreshHandler<TEntity>` calls `ReplicateAsync<TEntity>`). The two are compatible: B can be added later without changing A.
- `ReplicateAllAsync` removes the whole kind with `StoreCacheKey.PatternFor(prefix, "ref-table")` (all keys are injective and glob-safe, so the pattern cannot widen), `ReplicateAsync<TEntity>` removes the exact key.
- The decorator is a singleton like the replicator it wraps (`TryAddSingleton<IReferenceTableReplicator, ReferenceTableReplicator>`, `ShardingServiceCollectionExtensions.cs` line 205), and receives only singletons (`ICacheProvider` is registered singleton by every cache package). The extension throws at registration when no replicator is registered (reference tables not configured), as #695 does for stores.
- Class: Dilemma

</details>

<details>
<summary><strong>3. Cross-instance invalidation</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The decorator publishes the removed key (or pattern) on `ReferenceTableCachingOptions.InvalidationChannel` through `IPubSubProvider`, and a hosted subscriber of this package removes it from the local cache; the subscriber accepts only messages that start with `{prefix}:x-store:ref-table:` and counts and logs any other message by reason code** | Complete by itself (publisher and subscriber ship together, proven by a registration test); closes the Memory and Hybrid L1 case where each instance has its own cache; a message cannot be used to flush other keys (fail closed) | A hosted service and a channel; the Redis family already removes globally, so the message is redundant there |
| **B) Publish on the existing `CachingOptions.InvalidationChannel` and rely on `CacheInvalidationSubscriberService` of `Encina.Cdc`** | No new subscriber | That subscriber exists only when CDC cache invalidation is registered, so a publisher with no subscriber is a registration gap (AGENTS.md section 3) |
| **C) No broadcast: shared caches (Redis family, Memcached) need none; Memory and Hybrid L1 expire by TTL** | Nothing to run | The issue's criterion "PubSub broadcast for cross-instance invalidation" is not met; per-instance caches stay stale up to the TTL (1 hour by default) |

### Chosen Option: **A - an own publisher and subscriber, with a prefix guard** (recommended, pending the maintainer)

### Rationale

- C wins for a deployment that uses only a shared cache and wants no hosted service; B wins only where CDC cache invalidation is always registered with it. A is the option that satisfies the issue without a hidden dependency. Publishing is optional per `ReferenceTableCachingOptions.EnableBroadcast` (default true when an `IPubSubProvider` is registered, otherwise the decorator publishes nothing and logs once at startup).
- A broadcast failure never fails the replication: it is counted and logged (event 420) and the local removal has already happened. The message carries a key or a pattern of this feature only: no entity data, no tenant, no personal data.
- Class: Dilemma

</details>

<details>
<summary><strong>4. What a hit returns: shared instances or copies</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Return the cached list as it is, typed `IReadOnlyList<TEntity>`, and document that the entities are read-only** | No copy; the cost of a hit is a dictionary lookup | On the Memory provider every caller shares the same entity instances; a mutation leaks to all callers (Redis returns fresh instances, so the behavior differs by provider) |
| **B) Return a copy made by a serializer round trip (`System.Text.Json`) on every hit** | Identical behavior on every provider; no leak | The cost of a deserialization per read, which defeats part of the purpose for a large list |
| **C) Require immutable entities (records with init-only members) and validate at registration** | The strongest guarantee | Cannot be checked reliably for arbitrary classes; the replicator upserts classes with setters today |

### Chosen Option: **A - shared, documented read-only** (recommended, pending the maintainer)

### Rationale

- B wins when callers are known to mutate returned entities (for example to fill a view model in place); A wins for the intended use, a lookup list that is read to render or validate. The documentation and the XML comment state the rule; a unit test pins the behavior (the same instance on a Memory hit) so a change is deliberate.
- Class: Dilemma

</details>

<details>
<summary><strong>5. The issue's `hash` and `metadata` keys, TTL, serialization and failure semantics</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Do not cache the content hash or the reflection metadata; the entity list entry has `Duration` 1 hour by default (valid 1 minute to 7 days, the issue's "long default TTL, default 1 hour"); a failing cache call is caught, counted and logged by exception type and the call falls through to the store, and with `CachingOptions.ThrowOnCacheErrors` it returns `Left` with the shared `store_cache.failed` (`ErrorCategory.Internal`, ADR-062 rule R4)** | Change detection stays correct (finding 4); the cache is an optimization whose outage never stops a read; one existing switch; no new error code | A reuse of the global flag (the answer given to Design Choice 6 of #695 applies) |
| **B) Cache all three keys of the issue** | The issue's text | A cached hash blinds `PollingRefreshDetector`; serializing reflection metadata is useless |

### Chosen Option: **A - entity list only, with the shared failure rule**

### Rationale

- **Class:** Trivial
- **Settled by:** `PollingRefreshDetector.cs` (line 82 reads `GetHashAsync` from the primary store to compare with the stored state, so the hash must be fresh), `EntityMetadataCache.cs` (a `ConcurrentDictionary` of reflection metadata), #705 acceptance criterion "Long default TTL (configurable, default 1 hour)", AGENTS.md section 3 (errors never swallowed: the inner `Left` is returned as it is and never cached), and #695 Design Choice 6 for the failure rule (its answer applies to the whole family).
- The TTL bound: `Duration` is the worst-case staleness for an instance that misses both the replicator hook and the broadcast (a direct database change that no strategy sees, or a manual table); the documentation says so.

</details>

<details>
<summary><strong>6. EventIds, telemetry and tenant</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The shared `StoreCacheLog` ids 350-353 for read, write and eviction failures with `store=ref_table`; a new range `CachingReferenceTables = (420, 434)` from the family block 400-449 with ids 420-423 for the broadcast and the subscriber; the shared `StoreCacheTelemetry` counters with `store=ref_table` (local) and `store=ref_table_broadcast` (received)** | No duplicated ids; the new range holds only what is new | One registry entry and an allocation-test mapping |
| **B) A separate range and meter for everything** | Self-contained | Duplicates the shared failure ids and a second meter for the same concept |

### Chosen Option: **A - shared ids plus a small new range**

### Rationale

- **Class:** Trivial
- **Settled by:** AGENTS.md section 7 (register the range in `EventIdRanges.cs` first; ids packed sequentially) and the family block 400-449 of the plan batch registry (#704 uses 400-419, this plan 420-434). ADR-054 D4 and D6: reference data has no tenant scope, so there is no tenant tag and no tenant segment; no key, entity or list content in a log, tag or span.

</details>

---

## Implementation Phases

### Phase 1: One primary-shard rule for the replicator and the cache (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

PR 1 holds exactly these 5 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina/Sharding/ReferenceTables/ReferenceTableConfiguration.cs` | modify (adds `ResolvePrimaryShard`) |
| 2 | `src/Encina/Sharding/ReferenceTables/ReferenceTableReplicator.cs` | modify (uses it; behavior unchanged) |
| 3 | `src/Encina/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 4 | `tests/Encina.UnitTests/Sharding/ReferenceTables/ReferenceTableConfigurationTests.cs` | modify (new cases) |
| 5 | `.github/coverage-manifest/Encina.json` | modify (`mechanical-fixer`) |

Tasks:

1. **Add** `public Either<EncinaError, ShardInfo> ResolvePrimaryShard(ShardTopology topology)` to the record: the primary is `Options.PrimaryShardId` or the first shard id of the topology; an absent id or a shard missing from the topology returns `Left` with `ReferenceTableErrorCodes.PrimaryShardNotFound` (the error the replicator returns today at the same two points). `ArgumentNullException.ThrowIfNull(topology)`.
2. **Modify** `ReferenceTableReplicator.ReplicateEntityAsync` to call it instead of its inline resolution (lines 119-143), with the same errors; the existing replicator tests and `ReferenceTableReplicatorGuardTests` stay green.
3. **Tests**: configured primary found; default to the first shard; empty topology; configured id absent from the topology; null topology throws; the replicator tests assert the same error code in the same two cases (no behavior change).
4. **Manifest and PublicAPI** through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #705 adds a reference table cache that must read the same primary shard as the
replicator. The replicator resolves it inline in ReferenceTableReplicator.ReplicateEntityAsync.

TASK:
1. Add ReferenceTableConfiguration.ResolvePrimaryShard(ShardTopology) returning Either<EncinaError, ShardInfo>.
2. Make the replicator use it with identical behavior and identical error codes.
3. Add unit tests; ask mechanical-fixer for the PublicAPI line and the manifest.

KEY RULES:
- No behavior change in the replicator; the existing tests must pass untouched.
- ROP: Left with the existing PrimaryShardNotFound code, never an exception for a missing shard.
- Zero warnings; CRAP of every method at most 10; tests execute the real record.

REFERENCE FILES:
- src/Encina/Sharding/ReferenceTables/ReferenceTableReplicator.cs
- src/Encina/Sharding/ReferenceTables/ReferenceTableConfiguration.cs
- tests/Encina.GuardTests/Core/Sharding/ReferenceTableReplicatorGuardTests.cs
```

</details>

### Phase 2: Read API, replicator decorator and registration (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

Prerequisite: PR 1 of this plan and PR 1 and PR 2 of #695 are merged. PR 2 holds exactly these 12 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/ReferenceTables/IReferenceTableCache.cs` | new (public) |
| 2 | `src/Encina.Caching/ReferenceTables/ReferenceTableCache.cs` | new (internal sealed) |
| 3 | `src/Encina.Caching/ReferenceTables/CachingReferenceTableReplicator.cs` | new (internal sealed) |
| 4 | `src/Encina.Caching/ReferenceTables/ReferenceTableCachingOptions.cs` | new (public) |
| 5 | `src/Encina.Caching/ReferenceTables/ReferenceTableCachingServiceCollectionExtensions.cs` | new (public `AddEncinaReferenceTableCaching`) |
| 6 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 7 | `tests/Encina.UnitTests/Caching/ReferenceTables/ReferenceTableCacheTests.cs` | new |
| 8 | `tests/Encina.UnitTests/Caching/ReferenceTables/CachingReferenceTableReplicatorTests.cs` | new |
| 9 | `tests/Encina.UnitTests/Caching/ReferenceTables/ReferenceTableCachingRegistrationTests.cs` | new (DI test with `ValidateOnBuild` and `ValidateScopes`) |
| 10 | `tests/Encina.GuardTests/Caching/ReferenceTableCachingGuardTests.cs` | new |
| 11 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 12 | `changelog.d/705-reference-table-cache.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Create** `public interface IReferenceTableCache { Task<Either<EncinaError, IReadOnlyList<TEntity>>> GetAllAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class; }` with XML docs stating the read-only rule (Design Choice 4) and the staleness bound.
2. **Create** `internal sealed class ReferenceTableCache : IReferenceTableCache` with `(IReferenceTableRegistry registry, IShardTopologyProvider topology, IReferenceTableStoreFactory storeFactory, ICacheProvider cache, IOptions<ReferenceTableCachingOptions> options, IOptions<CachingOptions> cachingOptions, StoreCacheTelemetry telemetry, ILogger<ReferenceTableCache> logger)`. Flow: not registered returns `Left` `entity_not_registered`; key from `typeof(TEntity).FullName`; a hit returns the array; a miss resolves the primary with `ResolvePrimaryShard`, reads `storeFactory.CreateForShard(connectionString).GetAllAsync<TEntity>`, returns a `Left` untouched, caches a `Right` as `TEntity[]` with `Duration`; every cache call is wrapped (Design Choice 5).
3. **Create** `internal sealed class CachingReferenceTableReplicator : IReferenceTableReplicator`: delegates, then removes the exact key (`ReplicateAsync<TEntity>`) or the kind pattern (`ReplicateAllAsync`) in a `finally`-like continuation on both `Right` and `Left`; an eviction failure is counted and logged (shared event 352) and never replaces the inner result.
4. **Create** `public sealed class ReferenceTableCachingOptions { TimeSpan Duration (1 hour, valid 1 minute to 7 days) }`; setters throw `ArgumentOutOfRangeException`.
5. **Create** `AddEncinaReferenceTableCaching(this IServiceCollection services, Action<ReferenceTableCachingOptions>? configure = null)`: guards `services`; `Configure<ReferenceTableCachingOptions>`; `TryAddSingleton<StoreCacheTelemetry>`; `TryAddSingleton<IReferenceTableCache, ReferenceTableCache>`; `StoreCachingRegistration.Decorate<IReferenceTableReplicator>` (throws at registration when no replicator is registered, naming the call order: sharding with reference tables first). The decorator is a factory descriptor, so a missing `ICacheProvider` fails at first resolution; the DI test asserts that and builds the provider with a cache provider registered.
6. **Tests (unit)**: miss reads the primary and caches; hit does not call the store; a `Left` from the store is returned and not cached; an unregistered entity returns `entity_not_registered`; the primary comes from `ResolvePrimaryShard` (same shard as the replicator in a shared test case); two entity types with the same simple name in different namespaces get different keys; the TTL is `Duration` (fake time provider and fake cache); a Memory hit returns the same instance (pinned); a throwing cache falls through, counts and logs by type, and with `ThrowOnCacheErrors` returns `Left` `store_cache.failed` category `Internal`; the replicator decorator evicts after `Right`, after a partial `Right` and after `Left`, evicts the kind after `ReplicateAllAsync`, and returns the inner result unchanged even when the eviction throws.
7. **Tests (guard)**: `AddEncinaReferenceTableCaching(null)` throws; the options setter rejects values outside 1 minute to 7 days; `ReferenceTableCache` and the decorator are internal, so their null checks are asserted by the unit tests (guard target 0 with that reason).
8. **Manifest, PublicAPI and changelog** through `mechanical-fixer` (fragment: "Added opt-in `AddEncinaReferenceTableCaching`: `IReferenceTableCache` serves reference table lists from the cache and replication evicts them").

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #705: a cache for reference table lists in Encina.Caching. PR 1 added
ReferenceTableConfiguration.ResolvePrimaryShard; #695 added StoreCacheKey, StoreCachingRegistration, StoreCacheTelemetry, StoreCacheLog
(350-353) and StoreCacheErrors. IReferenceTableStore is created per shard connection by IReferenceTableStoreFactory.CreateForShard.

TASK:
1. Create IReferenceTableCache, ReferenceTableCache, CachingReferenceTableReplicator, ReferenceTableCachingOptions and
   AddEncinaReferenceTableCaching as described in the plan.
2. The cache reads ONLY the primary shard through the raw factory; the replicator and PollingRefreshDetector are never given cached data.
3. Write the unit, guard and DI tests; ask mechanical-fixer for PublicAPI lines, the manifest and the changelog fragment.

KEY RULES:
- Key = StoreCacheKey.Build(prefix, null, "ref-table", typeof(TEntity).FullName); never the simple name; never concatenate by hand.
- A Left from the store is returned untouched and never cached; a cache failure falls through (ThrowOnCacheErrors returns Left).
- No entity data, key or list content in a log or tag; exceptions through ForLogging(); reuse the existing reference_table error codes.
- Singletons only: no scoped service captured. Zero warnings; CRAP of every method at most 10.

REFERENCE FILES:
- src/Encina/Sharding/ReferenceTables/ReferenceTableReplicator.cs, IReferenceTableStoreFactory.cs, IReferenceTableRegistry.cs
- src/Encina/Sharding/ShardingServiceCollectionExtensions.cs (lines 195-210)
- src/Encina.Caching/Sharding/ShardingCacheServiceCollectionExtensions.cs
- src/Encina.Caching/MessagingStores (from #695)
```

</details>

### Phase 3: Cross-instance invalidation and EventIds (pull request 3)

<details>
<summary><strong>Tasks</strong></summary>

PR 3 holds exactly these 14 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/ReferenceTables/ReferenceTableCacheInvalidationSubscriber.cs` | new (internal hosted service) |
| 2 | `src/Encina.Caching/ReferenceTables/ReferenceTableCacheLog.cs` | new (internal; ids 420-423) |
| 3 | `src/Encina.Caching/ReferenceTables/CachingReferenceTableReplicator.cs` | modify (publishes after the local eviction) |
| 4 | `src/Encina.Caching/ReferenceTables/ReferenceTableCachingOptions.cs` | modify (`EnableBroadcast`, `InvalidationChannel`) |
| 5 | `src/Encina.Caching/ReferenceTables/ReferenceTableCachingServiceCollectionExtensions.cs` | modify (registers the subscriber) |
| 6 | `src/Encina/Diagnostics/EventIdRanges.cs` | modify (`CachingReferenceTables = (420, 434)`) |
| 7 | `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` | modify (maps `Encina.Caching` to the new range) |
| 8 | `src/Encina/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`: the new `EventIdRanges` field) |
| 9 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 10 | `tests/Encina.UnitTests/Caching/ReferenceTables/ReferenceTableCacheInvalidationSubscriberTests.cs` | new |
| 11 | `tests/Encina.UnitTests/Caching/ReferenceTables/CachingReferenceTableReplicatorTests.cs` | modify |
| 12 | `tests/Encina.UnitTests/Caching/ReferenceTables/ReferenceTableCachingRegistrationTests.cs` | modify (subscriber registered once, with and without an `IPubSubProvider`) |
| 13 | `tests/Encina.GuardTests/Caching/ReferenceTableCachingGuardTests.cs` | modify (`InvalidationChannel` rejects null or whitespace) |
| 14 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |

Tasks:

1. **Create** the subscriber (`IHostedService`): subscribes to `InvalidationChannel` at start (a failure to subscribe is logged and counted, and the host still starts: the broadcast is an optimization over the TTL), and for each message accepts only a string that starts with the `{prefix}:x-store:ref-table:` prefix of `StoreCacheKey`, then calls `RemoveByPatternAsync` (an exact key has no glob characters, so it removes exactly that key); any other message is dropped with a reason code (event 421, never the message text). It disposes the subscription at stop.
2. **Modify** the decorator: after the local removal, when `EnableBroadcast` is true and an `IPubSubProvider` exists, publish the key or pattern; a publish failure is counted and logged with event 420 and never changes the result.
3. **EventIds** (register first, ADR-021): `CachingReferenceTables = (420, 434)` in `EventIdRanges.cs`, mapped in `EncinaEventIdAllocationTests`; ids packed: 420 broadcast publish failed, 421 invalidation message rejected, 422 subscriber subscription failed, 423 subscriber removal failed. Exceptions through `ForLogging()`; no key, entity or message text in a log.
4. **Tests**: a message with the guard prefix removes the key locally; a message without it is rejected and counted; a subscription failure does not stop the host; the publisher publishes the key after the local removal and a throwing publisher does not change the replication result; with no `IPubSubProvider` nothing is published and registration succeeds; the subscriber is registered exactly once when `AddEncinaReferenceTableCaching` is called twice.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
PR 1 and PR 2 of issue #705 are merged: IReferenceTableCache and the replicator decorator exist. This PR adds the cross-instance broadcast
and registers the EventId range.

TASK:
1. Register EventIdRanges.CachingReferenceTables = (420, 434) first, map it in EncinaEventIdAllocationTests, then write
   ReferenceTableCacheLog (420-423) and ReferenceTableCacheInvalidationSubscriber.
2. Make the decorator publish after the local removal; a publish failure never fails the replication.
3. The subscriber accepts only messages with the ref-table prefix of StoreCacheKey (fail closed) and never logs the message text.
4. Write the unit and guard tests; ask mechanical-fixer for the PublicAPI lines and the manifest.

KEY RULES:
- Ids 420-423 only, packed, inside the registered range; ADR-021.
- A background subscriber never lets an exception escape; a subscription failure is logged by exception type through ForLogging().
- No key, entity data or message text in a log or tag. Zero warnings; CRAP of every method at most 10.

REFERENCE FILES:
- src/Encina.Cdc/Caching/CacheInvalidationSubscriberService.cs
- src/Encina.Caching/Sharding/CachedShardDirectoryStore.cs (SubscribeToInvalidationsAsync)
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

### Phase 4: Integration over the ten providers and a Redis run (pull request 4)

<details>
<summary><strong>Tasks</strong></summary>

PR 4 holds exactly these 14 files:

| # | File | Change |
|---|------|--------|
| 1 | `tests/Encina.IntegrationTests/Sharding/ReferenceTables/ReferenceTableCacheScenario.cs` | new (shared scenario) |
| 2-4 | `tests/Encina.IntegrationTests/Sharding/ReferenceTables/ADO/{SqlServer,PostgreSQL,MySQL}/ReferenceTableCacheAdo<Db>Tests.cs` | new (3) |
| 5-7 | `tests/Encina.IntegrationTests/Sharding/ReferenceTables/Dapper/{SqlServer,PostgreSQL,MySQL}/ReferenceTableCacheDapper<Db>Tests.cs` | new (3) |
| 8-10 | `tests/Encina.IntegrationTests/Sharding/ReferenceTables/EFCore/{SqlServer,PostgreSQL,MySQL}/ReferenceTableCacheEF<Db>Tests.cs` | new (3) |
| 11 | `tests/Encina.IntegrationTests/Infrastructure/MongoDB/Sharding/ReferenceTableCacheMongoDBTests.cs` | new |
| 12 | `tests/Encina.IntegrationTests/Infrastructure/Caching/ReferenceTableCacheRedisTests.cs` | new (`[Collection(RedisCollection.Name)]`, an in-test store, `RedisCacheProvider` and `RedisPubSubProvider`: `TEntity[]` round trip, TTL, eviction and the broadcast message on a real server) |
| 13 | `tests/Encina.IntegrationTests/Sharding/ReferenceTables/EFCore/ReferenceTableEfTestContext.cs` | new (the `DbContext` the three EF classes share) |
| 14 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`: integration targets) |

The ADO, Dapper and MongoDB classes use the same collection fixtures and the entity `ReferenceTableTestEntity` of the existing reference-table store tests of that provider (verified on `origin/main`: `Sharding/ReferenceTables/ADO/*`, `Sharding/ReferenceTables/Dapper/*`, `Infrastructure/MongoDB/Sharding`); the three EF classes use the EF Core shared fixtures (`EFCore-SqlServer`, `EFCore-PostgreSQL`, `EFCore-MySQL`) and `ReferenceTableStoreFactoryEF`, because no EF reference-table integration test exists today; `Encina.IntegrationTests` already references `Encina.Caching`, `Encina.Caching.Memory` and `Encina.Caching.Redis`, so no csproj edit is needed.

Scenario: (1) first read reaches the real store and fills the cache, second read is a hit (store call count 0); (2) after the primary rows change and `IReferenceTableReplicator.ReplicateAsync` (through the decorator, with a topology of the primary and one target shard on the same server) the next read returns the new rows; (3) a failing replication still evicts; (4) a row removed directly on the primary is served until `Duration` (fake time provider) and then read again, which documents the TTL bound; (5) a throwing cache falls through to the store; (6) the CDC-driven path is exercised through `IReferenceTableReplicator`, the seam it shares with the other strategies (the CDC connectors have their own tests in `Encina.Cdc`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
PR 1 to PR 3 of issue #705 are merged. This PR proves the cache against the ten database providers and one Redis server.

TASK:
1. Write ReferenceTableCacheScenario (static asserts) and one thin class per provider using its shared collection fixture; add the shared EF
   test DbContext for the three EF classes.
2. Cover the six scenarios of the plan, including eviction after replication and the TTL bound.
3. Write ReferenceTableCacheRedisTests with the existing RedisCollection fixture; ask mechanical-fixer for the integration targets.

KEY RULES:
- [Collection] shared fixtures only; ClearAllDataAsync in InitializeAsync; never dispose the shared fixture.
- Real stores and the real cache and decorator; no mocks of either (the Redis class uses an in-test store).
- FakeTimeProvider for the TTL; no Thread.Sleep.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Sharding/ReferenceTables/ADO/PostgreSQL/ReferenceTableStoreAdoPostgreSqlTests.cs
- tests/Encina.IntegrationTests/Sharding/ReferenceTables/ReferenceTableTestEntity.cs
- tests/Encina.IntegrationTests/Infrastructure/Caching/Fixtures/RedisFixture.cs
```

</details>

### Phase 5: Observability, coverage targets, benchmark and documentation (pull request 5)

<details>
<summary><strong>Tasks</strong></summary>

1. **Telemetry** (the shared `StoreCacheTelemetry`, no file added): `encina.store_cache.requests` with `store=ref_table` and outcomes `hit`, `miss`, `skipped`, `failure`; `encina.store_cache.evictions` with `store=ref_table` (local, after a replication) and `store=ref_table_broadcast` (received). No key, entity type name from user data beyond the fixed `store` value, or list content in a tag or span; no tenant tag (ADR-054 D4: reference data has no tenant scope).
2. **Logs and errors**: ids 350-353 (shared) and 420-423 (this plan); the only error code that can surface from the decorator is the shared `store_cache.failed` (`ErrorCategory.Internal`, ADR-062 rule R4); the existing `entity_not_registered` and `primary_shard_not_found` codes keep the categories they have today. No new error code.
3. **Coverage targets** (per file, per flag; baseline: the new files do not exist; the manifest `targets` of `Encina.Caching` are unit 60, guard 30, contract 10 and of `Encina` unit 70, guard 20, contract 15; measured values replace the estimates before each pull request opens):

| File | PR | unit | guard | property | integration | Justification |
|------|----|------|-------|----------|-------------|---------------|
| `ReferenceTables/ReferenceTableCache.cs` | 2 (integration in 4) | 95 | 0 | 0 | 75 | Every branch unit tested with a fake store and cache; internal type without `InternalsVisibleTo` for `Encina.GuardTests`; the PR 4 scenario reaches hit, miss, eviction and fall-through against real stores |
| `ReferenceTables/CachingReferenceTableReplicator.cs` | 2 and 3 (integration in 4) | 95 | 0 | 0 | 60 | Success, partial, failure and publish paths unit tested; internal; the scenario reaches eviction after a real replication |
| `ReferenceTables/ReferenceTableCacheInvalidationSubscriber.cs` | 3 | 90 | 0 | 0 | 40 | Accept, reject and subscription-failure paths unit tested; internal; the Redis class reaches a real subscription |
| `ReferenceTables/ReferenceTableCachingOptions.cs` | 2 and 3 | 100 | 100 | 0 | 0 | Setter validation fully reachable from both flags |
| `ReferenceTables/ReferenceTableCachingServiceCollectionExtensions.cs` | 2 and 3 | 95 | 90 | 0 | 0 | DI test and guard tests reach every line |
| `ReferenceTables/ReferenceTableCacheLog.cs` | 3 | 90 | 0 | 0 | 0 | Log methods asserted through the decorator and subscriber tests with a fake logger; internal |
| `Sharding/ReferenceTables/ReferenceTableConfiguration.cs` (`Encina`) | 1 | 95 | 20 | 0 | 0 | The new method and its null check are unit tested; the record has other members |

4. **Contract and load**: `IReferenceTableCache` is a new public interface with one implementation, so no contract test beyond the unit tests (justification in the manifest `reason`); no load test (the cache holds no state beyond the cache provider).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
Issue #705 is implemented in PR 1 to PR 4. PR 5 adds the benchmark, the documentation and the final verification.

TASK:
Write the benchmark in tests/Encina.BenchmarkTests/Encina.Caching.Benchmarks (cached versus uncached list read over an in-benchmark store with a
fixed latency, public AddEncinaReferenceTableCaching only), verify the filter with --list flat, ask docs-writer for the section of
docs/configuration/reference-tables.md and the INVENTORY entry, and run the build, the tests and the coverage checks.

KEY RULES:
- Say the read-only rule, the TTL bound and the three invalidation paths in the public docs; never promise freshness inside the TTL for
  a database change that no refresh strategy sees.
- Documentation cites coverage with cov: markers, never typed numbers.

REFERENCE FILES:
- docs/configuration/reference-tables.md
- docs/architecture/adr/013-reference-tables.md
- tests/Encina.BenchmarkTests/Encina.Caching.Benchmarks/CacheInvalidationBenchmarks.cs
```

</details>

PR 5 holds exactly these 3 files: `tests/Encina.BenchmarkTests/Encina.Caching.Benchmarks/ReferenceTableCacheBenchmarks.cs` (new), `docs/configuration/reference-tables.md` (modify, `docs-writer`), `docs/INVENTORY.md` (modify, `docs-writer`: the `ReferenceTables` folder of `Encina.Caching`).

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| ADR-013 (reference tables) | The replication model: primary shard to every shard, three refresh strategies |
| AGENTS.md section 5 (database, caching, sharding) | The cache is consistent across the 10 database providers and the cache providers; Memcached needs no pub/sub here |
| AGENTS.md section 3 | ROP, errors never swallowed, exceptions through `ForLogging()`, registration completeness (publisher and subscriber ship together), background loops never let an exception escape |
| ADR-054 D4, D6 | No tenant tag without a tenant scope; no keys or data in telemetry |
| ADR-062 | The shared error code states its `ErrorCategory` (`Internal`); existing reference-table codes unchanged |
| #712 | Cache Integration epic that this issue belongs to |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| IReferenceTableStore, IReferenceTableStoreFactory | src/Encina/Sharding/ReferenceTables | the stores read through the cache |
| ReferenceTableReplicator, CdcDrivenRefreshHandler, PollingRefreshDetector | src/Encina/Sharding/ReferenceTables, src/Encina.Cdc/Sharding | the seam all refresh strategies share and the readers that must stay uncached |
| IReferenceTableRegistry, IShardTopologyProvider | src/Encina/Sharding | registered configuration and topology |
| ICacheProvider, IPubSubProvider | src/Encina.Caching/Abstractions | the cache and the broadcast |
| StoreCacheKey, StoreCachingRegistration, StoreCacheTelemetry, StoreCacheLog, StoreCacheErrors | src/Encina.Caching/MessagingStores (#695) | the shared foundation |
| CachedShardDirectoryStore | src/Encina.Caching/Sharding | precedent for a cache decorator with a pub/sub subscriber inside this package |
| QueryCacheInvalidationCdcHandler, CacheInvalidationSubscriberService | src/Encina.Cdc/Caching | the query-cache invalidation flow that this plan deliberately does not extend |
| EventIdRanges | src/Encina/Diagnostics/EventIdRanges.cs | EventId registry |
| ReferenceTableTestEntity and the store integration tests | tests/Encina.IntegrationTests/Sharding/ReferenceTables | reusable per-provider harness |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Caching` (new range `CachingReferenceTables`) | 420-434 (from the family block 400-449) | 420-423 used by this plan; 424-434 free; 400-419 are #704 |
| `Encina.Caching` (shared range `CachingStores` of #695) | 350-353 reused | read, write and eviction failures with `store=ref_table` |

### Estimated File Count

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | 7 | 3 | New: interface, cache, replicator decorator, options, extension, subscriber, log; modified: `ReferenceTableConfiguration.cs`, `ReferenceTableReplicator.cs`, `EventIdRanges.cs` (the PR 3 edits of PR 2 files are counted once, as new) |
| Tests | 19 | 2 | New: 3 unit and 1 guard in PR 2, 1 subscriber unit in PR 3, scenario, 9 per-provider classes for ADO, Dapper and EF Core, the MongoDB class, the Redis class and the EF context in PR 4, benchmark in PR 5; modified: `ReferenceTableConfigurationTests.cs`, `EncinaEventIdAllocationTests.cs` |
| Documentation | 0 | 2 | `reference-tables.md`, `INVENTORY.md` |
| Configuration | 1 | 4 | Changelog fragment; `Encina.Caching.json` (PR 2, 3 and 4), `Encina.json`, PublicAPI (`Encina`, `Encina.Caching`) |
| **Total** | **27** | **11** | 5 pull requests: 5, 12, 14, 14 and 3 files (48 file touches: 38 distinct files, because the shared files are touched in several pull requests), each at most 15 files and about 800 lines |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0, no backward compatibility. Issue #705: an opt-in cache for reference table entity lists in
Encina.Caching, invalidated by replication and broadcast across instances, on the foundation of #695 (StoreCacheKey,
StoreCachingRegistration, StoreCacheTelemetry, StoreCacheLog 350-353, StoreCacheErrors).

IMPLEMENTATION OVERVIEW:
1. PR 1: ReferenceTableConfiguration.ResolvePrimaryShard shared by the replicator and the cache (no behavior change).
2. PR 2: IReferenceTableCache (cache-aside over the primary shard, key by FullName), CachingReferenceTableReplicator (evicts after every
   replication, Right or Left), options, AddEncinaReferenceTableCaching, unit, guard and DI tests.
3. PR 3: the EventId range 420-434, the broadcast publisher and the prefix-guarded hosted subscriber.
4. PR 4: integration scenario over the 10 database providers with a Memory cache and a Redis class.
5. PR 5: benchmark, documentation, inventory.

KEY PATTERNS:
- The replicator and PollingRefreshDetector keep the raw store: never serve them cached data; the content hash and the reflection metadata
  are never cached.
- A Left is returned untouched and never cached; a cache failure falls through (ThrowOnCacheErrors returns Left store_cache.failed, Internal).
- Keys go through StoreCacheKey; no key, entity data or message text in logs or tags; no tenant (reference data has none).
- The subscriber accepts only messages with the ref-table key prefix (fail closed); a background loop never lets an exception escape.
- Singletons only; DI test with ValidateOnBuild and ValidateScopes; the extension fails at registration when no replicator is registered.
- Apply Design Choices 1 to 4 as the maintainer answers them; until then build the recommended option of each.

REFERENCE FILES:
- src/Encina/Sharding/ReferenceTables/ReferenceTableReplicator.cs, PollingRefreshDetector.cs, IReferenceTableStoreFactory.cs
- src/Encina.Cdc/Sharding/CdcDrivenRefreshHandler.cs, src/Encina.Cdc/Caching
- src/Encina.Caching/Sharding/CachedShardDirectoryStore.cs, src/Encina.Caching/MessagingStores
- src/Encina/Diagnostics/EventIdRanges.cs
- .github/coverage-manifest/Encina.Caching.json, Encina.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Include | The feature: cache-aside of reference lists over `ICacheProvider`, opt-in, with a TTL bound and replication-driven eviction. |
| 2 | OpenTelemetry | ✅ Include | The shared counters with `store=ref_table` and `ref_table_broadcast`; no keys or content; no tenant tag (reference data has no tenant scope, ADR-054 D4, D6). |
| 3 | Structured Logging | ✅ Include | Shared ids 350-353 and the new range 420-434 (ids 420-423); exceptions through `ForLogging()`. |
| 4 | Health Checks | ❌ N/A | The cache is optional and falls through; the cache providers have their own checks; the reference-table health check reads replication state, not the cache. |
| 5 | Validation | ✅ Include | Options setters reject out-of-range values; the subscriber rejects messages without the key prefix; the key builder rejects an empty segment. |
| 6 | Resilience | ✅ Include | A cache or broadcast outage never fails a read or a replication (fall through, count, log); `ThrowOnCacheErrors` is the strict switch. |
| 7 | Distributed Locks | ❌ N/A | Entries are replaced whole and eviction is idempotent; two instances refilling at once store equal lists. |
| 8 | Transactions | ✅ Include | The cache is filled only by reads of the primary and evicted after a replication has returned; the decorator never commits, so the plan claims bounded staleness, not atomicity. |
| 9 | Idempotency | ✅ Include | Eviction and refill are idempotent; a duplicate invalidation message removes an absent key. |
| 10 | Multi-Tenancy | ✅ Include | Reference data is tenant-agnostic (the whole table is copied to every shard), so there is no tenant segment; stated, not invented; the DSR purge of #2407 does not touch these keys because they carry no personal data. |
| 11 | Module Isolation | ❌ N/A | Reference tables are not module scoped. |
| 12 | Audit Trail | ❌ N/A | A cache read is not an audit event; the replication itself is observed by its own metrics. |

**Deferred items:** none needs a new issue. The immediate CDC-commit invalidation (Design Choice 2, option B) is recorded as an extension that can be added without changing this design; the Hybrid-cache run is the follow-up of #695.

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| Reference tables, replicator, stores for the 10 providers | ✅ Available | ADR-013; ADO x3, Dapper x3, EF Core x3, MongoDB |
| `ICacheProvider`, `IPubSubProvider` and the cache provider packages | ✅ Available | Memory, Redis, Valkey, Dragonfly, Garnet, KeyDB, Hybrid; Memcached is planned (#277) |
| #695 PR 1 and PR 2 (foundation) | ⏭️ Sibling | `StoreCacheKey`, `StoreCachingRegistration`, `StoreCacheTelemetry`, `StoreCacheLog`, `StoreCacheErrors` must be merged before PR 2 of this plan |
| Maintainer answers to Design Choices 1 to 4 | Pending | Asked when work starts |

One real prerequisite: #695 PR 1 and PR 2 before PR 2 of this plan (PR 1 of this plan has none).

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas (Design Choices 1 to 4) when work starts, records the answers in a `## Maintainer Decisions` section, then briefs five workers in the phase order (PR 1 may start at once).
2. After PR 5, comment on #705 with the measured benchmark so the maintainer can judge the gain of the list cache against the TTL in use.
