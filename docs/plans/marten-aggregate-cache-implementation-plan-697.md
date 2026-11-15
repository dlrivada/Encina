# Implementation Plan: Distributed cache for hydrated Marten aggregates

> **Issue**: [#697](https://github.com/dlrivada/Encina/issues/697)
> **Type**: Feature
> **Complexity**: High (5 phases, 3 pull requests of 12, 13 and 6 files)
> **Estimated Scope**: about 600 lines of production code, about 1200 lines of tests
> **Milestone**: v0.18.0 - Integration & Platform Capabilities (caching, event sourcing)

---

## Summary

`SnapshotAwareAggregateRepository<TAggregate>` and `MartenAggregateRepository<TAggregate>` rebuild an aggregate on every `LoadAsync(id)`: the snapshot (if any) plus the events after it (`LoadWithSnapshotAsync`, `FetchStreamAsync`, `LoadFromHistory`). The issue asks for a decorator that caches the hydrated state and invalidates it on `SaveAsync`.

Re-checked against `origin/main` (db1e7f688), the plan adapts the sketch to the real contract and adds the hazards the sketch does not see:

1. **The interface is `IAggregateRepository<TAggregate>` (one type parameter, `Guid` ids, `Task<Either<EncinaError, ...>>`)** in `Encina.Marten`, not `IAggregateRepository<TAggregate, TId>`; the decorator keeps the four members (`LoadAsync(id)`, `LoadAsync(id, version)`, `SaveAsync`, `CreateAsync`).
2. **An aggregate is mutable.** A loaded aggregate is changed by `RaiseEvent` and emptied by `ClearUncommittedEvents` before and after `SaveAsync`. A cached instance handed to two callers would be corrupted by the first; every hit must return a fresh object.
3. **A cached state goes stale across instances.** `SaveAsync` appends with the expected stream version and `SaveChangesOrEjectAsync` commits inside the repository (the events are durable when it returns, `SnapshotAwareAggregateRepository.cs`), so evicting after `SaveAsync` is exact on one instance; another instance keeps serving the old state until its entry expires, unless the hit is validated against the stream version (Design Choice 3, the issue's "version-aware" criterion).
4. **The cached payload is the decrypted aggregate.** `Encina.Marten.GDPR` crypto-shreds personal fields (`[CryptoShredded]`); a shared cache holding the decrypted state would keep a forgotten subject readable until the TTL. The plan refuses to cache such aggregates (Design Choice 4).
5. **The package cannot take the dependency for free.** `Encina.Marten` references only `Encina`, `Encina.DomainModeling` and `Encina.Messaging`; `Encina.Caching` brings Entity Framework Core, the distributed lock package and the messaging package. A Marten application that does not use caching must not pull them (pay-for-what-you-use, AGENTS.md section 1), so the decorator ships in a new satellite package (Design Choice 1).

This plan reuses the foundation of #695 (`StoreCacheKey`, `StoreCachingRegistration` pattern, the shared logs and telemetry shape) through an `InternalsVisibleTo` entry for the new package, and owns the Marten range `MartenCaching` 390-399 from the family block 350-399.

**Affected packages:** `Encina.Marten.Caching` (new: options, decorator, entry, key, registration, telemetry, logs), `Encina.Caching` (one `InternalsVisibleTo` line), `Encina` (EventId range registration only), `Encina.slnx`. `Encina.Marten` is not changed.

**Provider category**: Event sourcing (Marten, the primary and only provider; AGENTS.md section 5 "other categories"); Caching (8) for the cache behind it: Memory and Redis tested, Valkey, Dragonfly, Garnet and KeyDB share the Redis wrapper, Hybrid by the `ICacheProvider` contract, Memcached planned (#277). The database category does not apply (Marten runs on PostgreSQL only).

**Sibling plans (caching family)**: #695 owns the key builder, the registration pattern and the shared telemetry names; #326 (Marten multi-tenancy) decides how a tenant reaches the session; this plan keys by the session's tenant and names no behavior of that plan. #2407 purges the tenant namespace `{prefix}:t:{escaped tenant}:` for an erasure; the aggregate keys sit inside it.

---

## Design Choices

<details>
<summary><strong>1. Where the decorator lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A new satellite package `Encina.Marten.Caching` referencing `Encina.Marten` and `Encina.Caching`** | A Marten application without caching pulls nothing new; follows the satellite convention (`Encina.Marten.GDPR`, `Encina.Messaging.Encryption`); the new `InternalsVisibleTo` entry in `Encina.Caching.csproj` follows its existing list of satellites | A new project: csproj, solution entry, README, two PublicAPI files, a manifest and test project references |
| **B) Inside `Encina.Marten`, with a project reference to `Encina.Caching`** | No new package | Every Marten application gets Entity Framework Core, the lock package and messaging transitively |
| **C) Inside `Encina.Caching` with a reference to `Encina.Marten`** | One package for caches | Inverts the dependency: the cache package would depend on an event store |

### Chosen Option: **A - satellite package** (recommended, pending the maintainer)

### Rationale

- B wins if the maintainer accepts the transitive dependencies to avoid a package and wants the decorator discoverable next to the repositories; C is not considered because it makes a general package depend on a provider.
- Aggregate caching is event-sourcing specific: it needs `IAggregate`, `IAggregateRepository<T>` and Marten's serializer, none of which exist in `Encina.Caching`.
- Class: Dilemma

</details>

<details>
<summary><strong>2. What is stored and what a hit returns</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A plain entry `{ Version, Payload }` where `Payload` is the aggregate serialized to a string with Marten's configured serializer (the one that persists `SnapshotEnvelope<TAggregate>.State` today); a hit deserializes into a fresh `TAggregate` with an empty uncommitted-event list** | Isolation: no shared instance; the aggregate round-trips exactly as Marten already round-trips it; the cache serializer only sees strings and a number, so System.Text.Json and MessagePack both work | A deserialization per hit (cheaper than event replay, not free) |
| **B) The aggregate object through the `ICacheProvider` serializer** | No Marten serializer dependency | The cache serializer (System.Text.Json by default) has to round-trip aggregates that Marten serializes with its own settings (private setters, `Apply` methods); the Memory provider may hand out the same instance |
| **C) Cache the snapshot plus the events after it** | Reuses the replay code | Larger payloads and the same replay CPU on every hit |

### Chosen Option: **A - versioned string payload** (recommended, pending the maintainer)

### Rationale

- B wins for a cache of small aggregates designed for System.Text.Json when the Marten serializer is not available to the decorator; the plan checks at implementation time that the serializer is reachable from the session (`IDocumentStore` options) and reports back if it is not.
- `LoadAsync(id, version)` (a historical load) and `CreateAsync` pass through uncached; only the head state of a stream is cached.
- Class: Dilemma

</details>

<details>
<summary><strong>3. Version awareness and invalidation</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) On a hit, read the stream version with one lightweight query (`session.Events.FetchStreamStateAsync(id)`); serve the cached state only when its version equals the stream version, otherwise reload, replace the entry and return the fresh state; `SaveAsync` evicts the entry after the inner call returned `Right`** | Never serves a stale version, on any instance (the issue's "version-aware" criterion); the expensive part (snapshot read, event fetch and replay) is skipped | One small round trip per load remains |
| **B) TTL plus eviction on the saving instance only** | No query on a hit | Another instance serves a state older than its own commit for up to the TTL; a command built on it fails with a concurrency conflict at `SaveAsync` instead of being rejected up front |
| **C) B plus a cross-instance invalidation message over `IPubSubProvider`** | No query on a hit and near-immediate convergence | Needs a backplane (Memory is in-process, Hybrid has none, Memcached none) and a publisher in the commit path; a lost message leaves the stale window |

### Chosen Option: **A - validate the version on every hit** (recommended, pending the maintainer)

### Rationale

- B wins when a one-row query per load is too expensive and stale reads up to the TTL are tolerable (read models fed from other sources); C wins when an application already runs a Redis backplane and needs zero extra queries.
- `SaveAsync` also evicts on `Left` (a concurrency conflict means the cached state is already stale).
- The TTL is `AggregateCachingOptions.Duration` (default 10 minutes) with an optional per-type override (`Cache<TAggregate>(TimeSpan duration)`), the issue's "configurable TTL per aggregate type".
- Class: Dilemma

</details>

<details>
<summary><strong>4. Which aggregates may be cached</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Opt in per aggregate type (`Cache<TAggregate>()`); registration throws `InvalidOperationException` for a type with a member marked `[CryptoShredded]` (detected by the attribute's full name `Encina.Marten.GDPR.CryptoShreddedAttribute`, so no package reference is needed)** | Fails closed: decrypted personal data never reaches a shared cache; no hidden default | A shredded aggregate cannot be cached |
| **B) Allow shredded aggregates and evict on a `SubjectForgotten` event** | Caches every aggregate | Needs a subscription to forget events in every instance, and a window in which a forgotten subject is still readable; a second mechanism to keep correct |
| **C) Cache every aggregate type of the application by default** | Zero configuration | Violates opt-in and caches personal data unasked |

### Chosen Option: **A - per-type opt-in, shredded types refused** (recommended, pending the maintainer)

### Rationale

- B wins when a team must cache a shredded aggregate and accepts the engineering of forget-time eviction (a later feature); A is the compliant default: AGENTS.md section 3 requires compliance gates to fail closed and opt-in for every feature.
- Class: Dilemma

</details>

<details>
<summary><strong>5. Key, tenant and failure semantics</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Key `{CachingOptions.KeyPrefix}:t:{encoded session tenant}:x-store:agg:{encoded aggregate type name}:{id:N}` through `StoreCacheKey` (#695); the tenant is the tenant of the repository's `IDocumentSession` (`IQuerySession.TenantId`), because that is the tenant Marten partitions the stream by; an empty tenant bypasses the cache; a cache failure falls through, with `ThrowOnCacheErrors` it returns `Left store_cache.failed` (`Internal`, ADR-062)** | Injective (two tenants, two types or two ids never share a key); inside the tenant namespace that the DSR purge of #2407 clears; fails closed on a missing tenant; the cache never fails a load | Depends on the foundation of #695 |
| **B) Key by `IRequestContext.TenantId`** | Same source as the event metadata | The request tenant can differ from the session tenant of a system job; a hit could cross tenants |
| **C) No tenant in the key** | Shorter keys | Two tenants with the same aggregate id share an entry |

### Chosen Option: **A - session tenant, injective key**

### Rationale

- **Class:** Settled
- **Settled by:** AGENTS.md section 3 (compliance and security gates fail closed when context is missing; errors are not swallowed; Railway Oriented Programming), ADR-054 D4 (the tenant is a tag when multi-tenancy is on), ADR-062 (the code names its category) and the injectivity requirement stated in #695 Design Choice 5. B and C allow a cross-tenant hit, which no rule permits.

</details>

<details>
<summary><strong>6. EventIds</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A new range `MartenCaching = (390, 399)` from the family block 350-399, ids 390-394** | No collision with the Marten range 2600-2799, whose free ids other plans claim | One registry entry and an allocation-test mapping for the new assembly |
| **B) Ids 2747-2751 inside the `Marten` range** | No registry change | The assembly is not `Encina.Marten`; the allocation test maps ranges to assemblies |

### Chosen Option: **A - new range 390-399**

### Rationale

- **Class:** Trivial
- **Settled by:** the assignment of block 350-399 to this family in the plan batch registry (`artifacts/decisions/plan-batch/eventid-blocks.md`) and AGENTS.md section 7 (register the range first; the allocation test maps `Encina.Marten.Caching` to it). Ids: 390 read failed, 391 write failed, 392 evict failed, 393 version check failed (falls through to a full load), 394 aggregate not cached (reason `not_opted_in`, `no_tenant`, `too_large`).

</details>

---

## Implementation Phases

### Phase 1: The package skeleton and the key (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

PR 1 holds exactly these 12 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Marten.Caching/Encina.Marten.Caching.csproj` | new (references `Encina.Marten` and `Encina.Caching`; `InternalsVisibleTo` for `Encina.UnitTests` and `Encina.IntegrationTests`; PublicAPI analyzer) |
| 2 | `Encina.slnx` | modify |
| 3 | `src/Encina.Marten.Caching/PublicAPI.Shipped.txt` | new (empty) |
| 4 | `src/Encina.Marten.Caching/PublicAPI.Unshipped.txt` | new (`mechanical-fixer`) |
| 5 | `src/Encina.Caching/Encina.Caching.csproj` | modify (`InternalsVisibleTo` `Encina.Marten.Caching`, next to the existing satellites) |
| 6 | `src/Encina/Diagnostics/EventIdRanges.cs` | modify (`MartenCaching = (390, 399)`) |
| 7 | `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` | modify (map `Encina.Marten.Caching`) |
| 8 | `src/Encina/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 9 | `src/Encina.Marten.Caching/AggregateCacheKey.cs` | new (internal; builds the key through `StoreCacheKey`) |
| 10 | `tests/Encina.UnitTests/Marten/Caching/AggregateCacheKeyTests.cs` | new |
| 11 | `tests/Encina.UnitTests/Encina.UnitTests.csproj` | modify (`ProjectReference` to `Encina.Marten.Caching`) |
| 12 | `.github/coverage-manifest/Encina.Marten.Caching.json` | new (`mechanical-fixer`) |

Tasks:

1. **Create** the csproj, modeled on `Encina.Marten.GDPR.csproj` (package id, description, tags, `GenerateDocumentationFile`, `LanguageExt.Core`, logging and options abstractions, the PublicAPI analyzer and files); a new `ProjectGuid`.
2. **Create** `internal static class AggregateCacheKey` with `Build(string keyPrefix, string tenantId, Type aggregateType, Guid id)` returning `StoreCacheKey.Build(keyPrefix, tenantId, "agg", aggregateType.FullName!, id.ToString("N"))`; a null or empty tenant throws `ArgumentException` (the decorator checks first and bypasses).
3. **Register** the range and the allocation-test mapping.
4. **Tests**: key shape for two tenants, two types and two ids never collide; hostile tenant and type names are encoded; the tenant `default` differs from every other tenant; `ArgumentException` for an empty tenant.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #697 caches hydrated Marten aggregates in a new satellite package
Encina.Marten.Caching. This PR creates the package skeleton and the key builder; #695 already added StoreCacheKey to Encina.Caching.

TASK:
1. Create the project (model it on Encina.Marten.GDPR), add it to Encina.slnx and to the UnitTests project references.
2. Add the InternalsVisibleTo entry in Encina.Caching.csproj, register EventIdRanges.MartenCaching = (390, 399) and map the assembly.
3. Create AggregateCacheKey and its tests; ask mechanical-fixer for PublicAPI lines and the manifest.

KEY RULES:
- Every segment goes through StoreCacheKey; no hand-built keys.
- The new package references Encina.Marten and Encina.Caching only; Encina.Marten is not changed.
- Zero warnings; CRAP of every method at most 10.

REFERENCE FILES:
- src/Encina.Marten.GDPR/Encina.Marten.GDPR.csproj
- src/Encina.Caching/Encina.Caching.csproj, MessagingStores/StoreCacheKey.cs (#695)
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

### Phase 2: The caching repository decorator (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

PR 2 holds exactly these 13 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Marten.Caching/AggregateCachingOptions.cs` | new (public) |
| 2 | `src/Encina.Marten.Caching/CachedAggregateEntry.cs` | new (internal) |
| 3 | `src/Encina.Marten.Caching/CachingAggregateRepositoryDecorator.cs` | new (internal sealed) |
| 4 | `src/Encina.Marten.Caching/AggregateCachingServiceCollectionExtensions.cs` | new (public `AddEncinaMartenAggregateCaching`) |
| 5 | `src/Encina.Marten.Caching/AggregateCachingLog.cs` | new (ids 390-394) |
| 6 | `src/Encina.Marten.Caching/AggregateCachingTelemetry.cs` | new (meter, tenant tag) |
| 7 | `src/Encina.Marten.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 8 | `tests/Encina.UnitTests/Marten/Caching/CachingAggregateRepositoryDecoratorTests.cs` | new |
| 9 | `tests/Encina.UnitTests/Marten/Caching/AggregateCachingRegistrationTests.cs` | new (DI test with `ValidateOnBuild` and `ValidateScopes`) |
| 10 | `tests/Encina.GuardTests/Marten/Caching/AggregateCachingGuardTests.cs` | new |
| 11 | `tests/Encina.GuardTests/Encina.GuardTests.csproj` | modify (`ProjectReference`) |
| 12 | `.github/coverage-manifest/Encina.Marten.Caching.json` | modify (`mechanical-fixer`) |
| 13 | `changelog.d/697-marten-aggregate-cache.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Create** `public sealed class AggregateCachingOptions` with `Duration` (10 minutes), `MaxPayloadLength` (262144 characters) and `Cache<TAggregate>(TimeSpan? duration = null)` recording opted-in types with an optional TTL; setters reject non-positive values.
2. **Create** `internal sealed class CachingAggregateRepositoryDecorator<TAggregate> : IAggregateRepository<TAggregate>` with `(IAggregateRepository<TAggregate> inner, IDocumentSession session, ICacheProvider cache, AggregateCachingOptions options, IOptions<CachingOptions> cachingOptions, TimeProvider timeProvider, AggregateCachingTelemetry telemetry, ILogger<...> logger)`. `LoadAsync(id)`: bypass when the type is not opted in or the session tenant is empty; read the entry; on a hit call `session.Events.FetchStreamStateAsync(id)` and serve only when the versions match (an absent stream or a failed version check reloads); deserialize into a fresh aggregate; on a miss call the inner repository, return a `Left` untouched, and cache a `Right` whose payload is within `MaxPayloadLength`. `SaveAsync` calls the inner repository then evicts the key (after `Right` and after `Left`); `CreateAsync` and `LoadAsync(id, version)` delegate.
3. **Create** `AddEncinaMartenAggregateCaching(this IServiceCollection services, Action<AggregateCachingOptions> configure)`: `ArgumentNullException.ThrowIfNull` on both; for each opted-in type, throws at registration when the type has a `[CryptoShredded]` member (design choice 4); replaces the scoped `IAggregateRepository<TAggregate>` descriptor, or wraps the open-generic `MartenAggregateRepository<>` when only the open generic is registered, and fails when `ICacheProvider` is not registered at the first resolution (DI test).
4. **Tests (unit)**: hit with the same version serves without the inner call; hit with a newer stream version reloads and replaces the entry; miss caches; `Left` and oversized payloads are never cached; two hits return distinct instances with empty uncommitted events; `SaveAsync` evicts after `Right` and after `Left`; `CreateAsync` and the versioned load delegate; an empty tenant bypasses; a failing cache or version check falls through and, with `ThrowOnCacheErrors`, returns `Left`; the opt-in check and the shredded-type refusal.
5. **Tests (guard)**: the extension (`services`, `configure` null), options setters.
6. **Manifest and changelog** through `mechanical-fixer` (fragment: "Added `AddEncinaMartenAggregateCaching`: an opt-in, per-aggregate-type, version-validated cache for Marten aggregate loads (new package `Encina.Marten.Caching`)").

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
Issue #697. PR 1 created Encina.Marten.Caching and AggregateCacheKey. IAggregateRepository<TAggregate> (Encina.Marten) has LoadAsync(id),
LoadAsync(id, version), SaveAsync and CreateAsync, all returning Task<Either<EncinaError, ...>>; aggregates are mutable.

TASK:
1. Create the options, the entry, the decorator, the extension, the logs (390-394) and the telemetry as described in the plan.
2. A hit is served only after the stream version matches; every hit deserializes a fresh aggregate.
3. Write the unit, guard and DI tests; ask mechanical-fixer for PublicAPI lines, the manifest and the changelog fragment.

KEY RULES:
- Only opted-in aggregate types are cached; a type with a [CryptoShredded] member is refused at registration.
- A Left from the inner repository is returned untouched and never cached; SaveAsync evicts after Right and Left.
- Never hand out a cached instance; no key, id or payload in a log or tag; exceptions through ForLogging().
- The telemetry carries the tenant tag only when multi-tenancy is on (ADR-054 D4).
- Never inject a scoped service into a singleton; the decorator is scoped like the repository. Zero warnings; CRAP of every method at most 10.

REFERENCE FILES:
- src/Encina.Marten/IAggregateRepository.cs, Snapshots/SnapshotAwareAggregateRepository.cs, MartenAggregateRepository.cs
- src/Encina.Marten/ServiceCollectionExtensions.cs
- src/Encina.Marten.GDPR/Attributes/CryptoShreddedAttribute.cs
- src/Encina.Caching/MessagingStores (#695)
```

</details>

### Phase 3: Integration against Marten and a cache (pull request 3)

<details>
<summary><strong>Tasks</strong></summary>

PR 3 holds exactly these 6 files:

| # | File | Change |
|---|------|--------|
| 1 | `tests/Encina.IntegrationTests/Infrastructure/Marten/Caching/CachingAggregateRepositoryIntegrationTests.cs` | new (shared Marten/PostgreSQL collection fixture, Memory cache) |
| 2 | `tests/Encina.IntegrationTests/Encina.IntegrationTests.csproj` | modify (`ProjectReference`) |
| 3 | `tests/Encina.BenchmarkTests/Encina.Marten.Caching/AggregateCache.md` | new (justification: the gain depends on database latency; the integration test counts the avoided stream reads) |
| 4 | `src/Encina.Marten.Caching/README.md` | new (`docs-writer`) |
| 5 | `docs/features/marten-aggregate-cache.md` | new (`docs-writer`) |
| 6 | `docs/INVENTORY.md` | modify (`docs-writer`) |

The integration test uses the existing Marten fixture of `Infrastructure/Marten` with two repositories over one store to simulate two instances: instance A loads (miss), instance B loads (hit, no event fetch), instance A saves, instance B loads and receives the new version; two tenants with the same aggregate id never share an entry; a `[CryptoShredded]` aggregate is refused at registration; a Redis-backed run is added to the existing Redis collection when the maintainer asks for it.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
PR 1 and PR 2 of issue #697 are merged. This PR proves the decorator against a real Marten store and ships the documentation.

TASK:
1. Write the integration test with the shared Marten fixture: two repositories over one store, version validation, tenant isolation.
2. Add the project reference to the integration test project; write the benchmark justification file.
3. Ask docs-writer for the package README, the feature page and the INVENTORY entry.

KEY RULES:
- Shared collection fixtures only; never dispose the shared fixture; real Marten and the real decorator, no mocks of either.
- Documentation cites coverage with cov: markers and types no figures.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Infrastructure/Marten (fixtures)
- src/Encina.Marten.Caching
```

</details>

### Phase 4: Observability, logging and coverage targets

<details>
<summary><strong>Tasks</strong></summary>

1. **Telemetry** (`AggregateCachingTelemetry`, PR 2): meter `Encina.Marten.Caching`, counter `encina.aggregate_cache.requests` with tags `aggregate_type` (the type name, never an id) and `outcome` (`hit`, `stale`, `miss`, `bypass`, `failure`); the tag `tenant.id` is added only when multi-tenancy is enabled (ADR-054 D4); no key, aggregate id or payload (ADR-054 D6). Spans of the cache calls come from `InstrumentedCacheProvider`.
2. **Logs** (`AggregateCachingLog`, `[LoggerMessage]`, range 390-399, packed from 390): 390 cache read failed, 391 cache write failed, 392 cache evict failed, 393 stream version check failed, 394 aggregate not cached (reason code). Exceptions through `ForLogging()`; never a key, id or payload.
3. **Error code**: the shared `store_cache.failed`, `ErrorCategory.Internal` (ADR-062 R4); no new code.
4. **Coverage targets** (per file, per flag; measured values replace the estimates before PR 2 and PR 3 open):

| File | unit | guard | integration | Justification |
|------|------|-------|-------------|---------------|
| `AggregateCacheKey.cs` | 100 | 0 | 50 | Pure and fully unit tested; internal type without `InternalsVisibleTo` for `Encina.GuardTests`; the integration test reaches it on every load |
| `CachingAggregateRepositoryDecorator.cs` | 95 | 0 | 70 | Every branch unit tested with fakes; internal; the integration test reaches hit, stale, miss and eviction with a real store |
| `CachedAggregateEntry.cs` | 100 | 0 | 50 | Mapping fully unit tested; reached on every hit |
| `AggregateCachingOptions.cs` | 100 | 100 | 0 | Setter and `Cache<T>` validation reachable from both flags |
| `AggregateCachingServiceCollectionExtensions.cs` | 95 | 90 | 30 | DI test and guard tests reach every line; the integration test registers it |
| `AggregateCachingTelemetry.cs`, `AggregateCachingLog.cs` | 90 | 0 | 0 | Counters and log methods asserted through the decorator tests; internal |

5. **Contract, property, load**: no contract test (the decorated interface keeps its contract; the unit tests drive a fake repository behind it); no property test (key injectivity is the property of #695); no load test (the decorator holds no shared state beyond the cache provider); the manifest `reason` says so. The benchmark is justified in the `.md` of PR 3.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
The Marten aggregate cache of issue #697 exists. Telemetry, logs and targets are added with the PR that introduces each caller.

TASK:
Add AggregateCachingTelemetry and AggregateCachingLog (390-394) in PR 2, measure each flag with coverage, and ask mechanical-fixer to
record per-file targets with one-sentence justifications in Encina.Marten.Caching.json.

KEY RULES:
- EventIds only 390-394, packed from 390; the range is registered first and mapped in the allocation test.
- Tenant tag only when multi-tenancy is on; no key, id or payload anywhere.
- Documentation never types coverage figures.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- docs/architecture/adr/054-observability-conventions-and-trace-propagation.md
- .github/coverage-manifest/Encina.Marten.Caching.json
```

</details>

### Phase 5: Documentation and finalization

<details>
<summary><strong>Tasks</strong></summary>

1. XML documentation on every public member (options, extension) with the opt-in rule, the version validation, the refusal of shredded aggregates and an example.
2. Package README, `docs/features/marten-aggregate-cache.md` and `docs/INVENTORY.md` through `docs-writer` (PR 3); `changelog.d/697-marten-aggregate-cache.added.md` (PR 2); PublicAPI files of the new package and `Encina`.
3. Build with `dotnet build --configuration Release` (0 warnings), `dotnet test` of each flag, `dotnet run --file .github/scripts/coverage-report.cs -- --check-justifications`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
Issue #697 is implemented. Finish documentation and verification.

TASK:
Write the XML docs, ask docs-writer for the README, feature page and INVENTORY entry, ask mechanical-fixer for PublicAPI lines and the
changelog fragment, and run the build, tests and coverage checks.

KEY RULES:
- The docs state the opt-in, the version validation and that shredded aggregates are refused; they type no coverage figures.

REFERENCE FILES:
- docs/features/audit-marten.md (style of a Marten feature page)
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| AGENTS.md section 1 and section 3 | Pay-for-what-you-use (new satellite), opt-in, compliance fails closed, ROP, `TimeProvider`, registration completeness with a DI test |
| AGENTS.md section 5 (event sourcing, caching) | Marten is the event-sourcing provider; the cache side is consistent across the cache providers |
| ADR-019, ADR-027 | Marten as the event-sourcing provider and the compliance modules built on it |
| ADR-062 | The shared error code states its `ErrorCategory` (`Internal`) |
| ADR-054 D4, D6 | Tenant tag when multi-tenancy is on; no keys or data in telemetry |
| #712 | Cache Integration epic |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| IAggregateRepository, MartenAggregateRepository | src/Encina.Marten | the decorated abstraction and its default implementation |
| SnapshotAwareAggregateRepository, ISnapshotStore | src/Encina.Marten/Snapshots | the repository the issue names; snapshots stay as they are |
| AddAggregateRepository, AddSnapshotableAggregate | src/Encina.Marten/ServiceCollectionExtensions.cs | the registrations the decorator wraps |
| CryptoShreddedAttribute | src/Encina.Marten.GDPR/Attributes | detected by name to refuse shredded aggregates |
| StoreCacheKey and the shared telemetry names | src/Encina.Caching/MessagingStores (#695) | injective keys; metric shape |
| ICacheProvider, CachingOptions | src/Encina.Caching | the cache and `ThrowOnCacheErrors` |
| Marten test fixtures | tests/Encina.IntegrationTests/Infrastructure/Marten | the real store for the integration test |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Marten.Caching` (new range `MartenCaching`) | 390-399 (from block 350-399) | 390-394 used by this plan; 395-399 free; the range 350-389 belongs to `Encina.Caching` (#695, #698) |

### Estimated File Count

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | 8 | 3 | New: csproj, key, options, entry, decorator, extension, log, telemetry; modified: `Encina.slnx`, `Encina.Caching.csproj`, `EventIdRanges.cs` |
| Tests | 5 | 4 | New: key unit, decorator unit, DI, guard, integration; modified: allocation test and the three test project files (`UnitTests`, `GuardTests`, `IntegrationTests`) |
| Documentation | 3 | 1 | New: package README, feature page, benchmark justification; modified: `INVENTORY.md` |
| Configuration | 4 | 1 | New: two PublicAPI files of the new package, manifest, changelog fragment; modified: PublicAPI of `Encina` |
| **Total** | **20** | **9** | 3 pull requests: 12, 13 and 6 files (31 file touches; the manifest and the new package's Unshipped file are touched twice), each at most 15 files and about 700 lines |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0, no backward compatibility. Issue #697: an opt-in cache for hydrated Marten aggregates in a
new satellite package Encina.Marten.Caching. Aggregates are mutable, can hold crypto-shredded personal data and go stale across instances.

IMPLEMENTATION OVERVIEW:
1. PR 1: package skeleton, AggregateCacheKey (through StoreCacheKey of #695), IVT entry, EventId range 390-399.
2. PR 2: options, entry, CachingAggregateRepositoryDecorator, AddEncinaMartenAggregateCaching, logs 390-394, telemetry, tests, manifest, changelog.
3. PR 3: integration test with a real Marten store and two repositories, benchmark justification, README, feature page, INVENTORY.

KEY PATTERNS:
- Cache only opted-in aggregate types; refuse a type with a [CryptoShredded] member at registration.
- Payload = Marten-serialized aggregate string plus version; a hit is served only if the stream version matches; every hit deserializes a fresh object.
- SaveAsync evicts after Right and Left; CreateAsync and the versioned load delegate; a Left is never cached.
- Key = StoreCacheKey with the session tenant; an empty tenant bypasses; a cache failure falls through (ThrowOnCacheErrors returns Left store_cache.failed, Internal).
- No key, id or payload in logs or tags; tenant tag only when multi-tenancy is on; exceptions through ForLogging().
- Apply Design Choices 1 to 4 as the Maintainer Decisions section says.

REFERENCE FILES:
- src/Encina.Marten/IAggregateRepository.cs, Snapshots/SnapshotAwareAggregateRepository.cs, ServiceCollectionExtensions.cs
- src/Encina.Marten.GDPR/Encina.Marten.GDPR.csproj, Attributes/CryptoShreddedAttribute.cs
- src/Encina.Caching/MessagingStores
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.IntegrationTests/Infrastructure/Marten
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Include | The feature: a version-validated, per-type, opt-in cache of hydrated aggregates. |
| 2 | OpenTelemetry | ✅ Include | Counter with `aggregate_type` and `outcome`; the tenant tag only when multi-tenancy is on (ADR-054 D4); no keys, ids or payloads (D6). |
| 3 | Structured Logging | ✅ Include | `[LoggerMessage]` ids 390-394 in the new range `MartenCaching`; exceptions through `ForLogging()`. |
| 4 | Health Checks | ❌ N/A | The cache is optional and falls through; the cache providers and Marten have their own checks. |
| 5 | Validation | ✅ Include | Options setters reject non-positive values; registration refuses a shredded aggregate type (fails at startup). |
| 6 | Resilience | ✅ Include | A cache or version-check failure never fails a load (fall through to the full load); `ThrowOnCacheErrors` is the strict switch. |
| 7 | Distributed Locks | ❌ N/A | Optimistic concurrency by stream version already guards writes; the cache holds no mutable shared entry. |
| 8 | Transactions | ✅ Include | The entry is written only from a completed load; eviction follows `SaveAsync`, whose `SaveChangesOrEjectAsync` has committed the events when it returns; no cache write happens before a commit. |
| 9 | Idempotency | ❌ N/A | Loads are idempotent; command idempotency is the inbox's concern. |
| 10 | Multi-Tenancy | ✅ Include | Key and bypass by the session tenant; injective keys inside the tenant namespace of the DSR purge (#2407); a missing tenant bypasses (fails closed). |
| 11 | Module Isolation | ❌ N/A | Aggregates are not module scoped by this feature. |
| 12 | Audit Trail | ❌ N/A | A cache hit is not an audit event; the event stream is the audit record. |

**Deferred items:** forget-time eviction so that shredded aggregates can be cached (Design Choice 4, option B) is not planned until the maintainer asks for it; no issue is opened for it now. Cross-instance invalidation over a backplane (Design Choice 3, option C) is likewise an alternative, not a deferred item.

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| #695 foundation (`StoreCacheKey`, shared telemetry and error code) | ⏭️ Sibling plan | PR 1 of this plan needs #695 PR 1 merged; PR 2 needs #695 PR 2 for `StoreCacheErrors` |
| `Encina.Marten` repositories and snapshots | ✅ Available | No change needed |
| #326 (Marten multi-tenancy) | ⏭️ Sibling plan | The decorator reads the tenant of the session; it works with whatever that plan decides |
| `Encina.Caching` `InternalsVisibleTo` for the new package | ✅ Planned in PR 1 | One line, next to the existing satellites |
| Maintainer answers to Design Choices 1 to 4 | Pending | Asked when work starts |

Blocking prerequisite: #695 PR 1 and PR 2.

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas (Design Choices 1, 2, 3 and 4) when work starts, records the answers under `## Maintainer Decisions`, then briefs three workers after #695 PR 2.
2. If the maintainer answers option B of Design Choice 1, the package skeleton of PR 1 disappears and the files move into `Encina.Marten`; the plan is re-split then.
3. After PR 3, comment on #697 with the integration result.
