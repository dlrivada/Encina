# Implementation Plan: Distributed cache layer for the Inbox store (and the shared store-cache foundation)

> **Issue**: [#695](https://github.com/dlrivada/Encina/issues/695)
> **Type**: Feature
> **Complexity**: Medium (5 phases, 3 pull requests of 9, 13 and 15 files)
> **Estimated Scope**: about 550 lines of production code, about 1100 lines of tests
> **Milestone**: v0.18.0 - Integration & Platform Capabilities (caching, messaging, idempotency)

---

## Summary

`InboxOrchestrator.ProcessCoreAsync` (`src/Encina.Messaging/Inbox/InboxOrchestrator.cs` line 131) calls `IInboxStore.GetMessageAsync(messageId)` once per received message; it is the only production caller (the other two sites are `InstrumentedInboxStore` and a test helper). The issue asks for a cache decorator in front of that read.

Re-checked against `origin/main` (db1e7f688), the issue text does not fit the code in four places, and the plan resolves each one:

1. **The sketch is not the interface.** Every store method returns `Task<Either<EncinaError, ...>>` and the lookup returns `Option<IInboxMessage>` (`IInboxStore.cs`); the sketch returns a bare `IInboxMessage?`.
2. **`Encina.Messaging` cannot host the decorator.** `Encina.Caching` references `Encina.Messaging` (`Encina.Caching.csproj`), so the decorator and its options live in `Encina.Caching`; a Messaging reference to Caching would be circular.
3. **There is no per-provider registration to write.** All ten database stores are registered by one method, `RegisterInbox` in `MessagingServiceCollectionExtensions.cs` (`services.AddScoped<IInboxStore, TInboxStore>()`), so one registration in `Encina.Caching` decorates every provider; "13 providers" is a leftover count (the real matrix is 10: ADO x3, Dapper x3, EF Core x3, MongoDB).
4. **Pre-populating on `AddAsync` and invalidating on `MarkAs*` is unsafe.** The orchestrator reads `RetryCount` and `IsProcessed` of an existing message; the inbox record is written outside the business transaction while `MarkAsProcessedAsync` joins it (ADR-048). A cached unprocessed entry goes stale on another instance and makes the handler run again or the retry count lie. Only the terminal, immutable state "processed, with its response" can be cached, so the cache helps the redelivery path (a duplicate of a processed message), not the first arrival of a new id: a message never seen cannot be cached as "absent", because another instance may add it a moment later. The plan therefore ships the cache opt-in and ships the benchmark that measures the gain on a duplicate-heavy workload.

This plan **owns the shared foundation of the cache-stores family**: the injective key builder `StoreCacheKey`, the descriptor-decoration helper `StoreCachingRegistration`, `StoreCacheErrors`, `StoreCacheTelemetry`, `StoreCacheLog` and the EventId range `CachingStores` (350-389, from the block 350-399 assigned to this family). Sibling plans reuse them: #696 (saga store), #699 (choreography state store), #698 (replica count sharing) and #697 (Marten aggregates, which reaches the key builder through an `InternalsVisibleTo` entry for its own satellite package).

**Affected packages:** `Encina.Caching` (decorator, options, key builder, registration, telemetry, logs, errors), `Encina` (EventId range registration only). No provider package changes.

**Provider category**: Database (10) for the decorated stores (the decorator is provider-independent, the integration scenario runs against all ten); Caching (8) for the cache behind it: tested with Memory (unit and integration) and Redis (integration); Valkey, Dragonfly, Garnet and KeyDB share the Redis wrapper; Hybrid is exercised by the contract of `ICacheProvider` only; Memcached (planned, #277) needs no change.

**Sibling plans (caching family)**: #2408 owns `CacheInvalidationPostCommitProcessor` and the range `CachingInvalidation` (300-319); #2407 owns the DSR cache purge (range 320-329) and the tenant namespace pattern `{prefix}:t:{escaped tenant}:`; #271 (tag invalidation, open) is not a prerequisite. The decorators of this family never use tags.

---

## Design Choices

<details>
<summary><strong>1. Where the decorator lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Encina.Caching`, folder `MessagingStores/`** | Already references `Encina.Messaging` and has the cache and pub/sub abstractions; precedent: `CachedShardDirectoryStore` and `CachedShardTopologyProvider` decorate core abstractions inside this package; no new package | `Encina.Caching` knows the messaging store interfaces |
| **B) `Encina.Messaging`** | The place the issue names | Circular project reference (`Encina.Caching` -> `Encina.Messaging`); not buildable |
| **C) A new `Encina.Messaging.Caching` package** | `Encina.Caching` stays free of messaging stores | A package with its own csproj, README, manifest and PublicAPI files for about 550 lines; it references both packages anyway, so it saves no dependency |

### Chosen Option: **A - `Encina.Caching`**

### Rationale

- **Class:** Trivial
- **Settled by:** AGENTS.md section 5 (caching category: features that use the cache abstractions are consistent across the cache providers, and `Encina.Caching` is the package that owns them) and the in-package precedent of the sharding decorators (`src/Encina.Caching/Sharding`). Option B cannot build; C adds a package without removing a dependency.

</details>

<details>
<summary><strong>2. How the decorator is applied to the registered store</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A public extension `AddEncinaInboxCaching(Action<InboxCachingOptions>?)` that replaces the existing `IInboxStore` descriptor with a factory that builds the inner store from the old descriptor (the technique of `DecorateService` in `Encina.OpenTelemetry`), and throws `InvalidOperationException` at registration when no `IInboxStore` is registered yet** | Same technique and call-order rule as the existing decorators; a missing store is a loud error, not a silent no-op (the OpenTelemetry helper returns silently, which this plan does not copy); one call covers all ten providers; no change to `Encina.Messaging` | Must be called after the provider registration; the documented order is "provider first, caching after" |
| **B) A seam in `Encina.Messaging`**: `RegisterInbox` resolves every `IInboxStoreDecorator` from the container and wraps the store, and `InboxOptions.EnableCaching` turns it on (the issue's configuration) | Order independent; matches the configuration in the issue | Changes the core registration used by all providers and every test that registers `IInboxStore` directly; the flag in Messaging would do nothing without Encina.Caching, so it needs its own startup validation |
| **C) Add the Scrutor package and call `Decorate<IInboxStore, ...>()`** | Familiar API | A new third-party dependency (it is not in `Directory.Packages.props`) for one call; same call-order rule as A |

### Chosen Option: **A - call-order extension that fails fast** (recommended, pending the maintainer)

### Rationale

- Option B wins when applications register caching in a module that runs before the messaging provider and the maintainer accepts a seam in core `Encina.Messaging`; A wins now because it changes no core registration and follows the decorators already in the repository.
- The decorator is scoped like the store it wraps (`RegisterInbox` registers `IInboxStore` as scoped because the EF store holds a `DbContext`); it receives only singletons besides the inner store (`ICacheProvider`, options, `TimeProvider`, logger, `StoreCacheTelemetry`), so no scoped service reaches a singleton.
- With `Encina.OpenTelemetry` also decorating `IInboxStore`, the order of the two `Add*` calls decides which wraps which; both orders are correct (the cache above the instrumented store spans only misses, below it spans every lookup) and the DI test builds both.
- Class: Dilemma

</details>

<details>
<summary><strong>3. What is cached and when it is written or removed</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Cache-aside on `GetMessageAsync` for a message that `IsProcessed` and has a `Response`, with TTL = min(`Duration`, remaining retention); never cache `None` or an unprocessed message; no write-through; evict the ids of `RemoveExpiredMessagesAsync` after the inner call returned `Right`** | The cached state is immutable until removal, so no instance can serve a stale value that changes the outcome; matches the only branch of `InboxOrchestrator` that uses a cached response (`IsProcessed && Response != null`) | The first arrival of a new id still hits the database; the gain is limited to redeliveries of processed messages |
| **B) The issue's behavior: pre-populate on `AddAsync`, evict on `MarkAsProcessedAsync` and `MarkAsFailedAsync`** | Fewer misses | Caches `RetryCount` and the unprocessed flag: another instance's `MarkAsFailedAsync` is invisible until the TTL, so `MaxRetries` is exceeded or the handler runs twice; eviction after `MarkAsProcessedAsync` runs before the business commit (ADR-048), so a concurrent reader re-caches the unprocessed row |
| **C) Also cache absence (`None`) with a short TTL** | Saves the lookup for new ids | Another instance adds the id inside the TTL: both instances run the handler (the exact failure the inbox prevents) |

### Chosen Option: **A - processed entries only, cache-aside**

### Rationale

- **Class:** Settled
- **Settled by:** ADR-048 (the inbox record is committed outside the business transaction and `MarkAsProcessedAsync` joins it, so only the processed state is stable) and `InboxOrchestrator.HandleExistingMessageAsync` (a message with `RetryCount` below `MaxRetries` and no response is re-processed; its fields must be read fresh). B and C each have a concrete sequence that processes a message twice or exceeds `MaxRetries`, so A is the only option that keeps the inbox guarantee. The issue's items "pre-populate on `AddAsync`" and "invalidate on `MarkAsProcessedAsync` and `MarkAsFailedAsync`" are replaced by this rule; the plan records the deviation here for the maintainer.
- The TTL never exceeds `ExpiresAtUtc - TimeProvider.GetUtcNow()`, so no cached entry outlives the retention of the row; entries over `MaxCachedResponseLength` (default 32768 characters) are skipped and counted, so a large response never fills the cache.

</details>

<details>
<summary><strong>4. The cached entry and what the decorator returns</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An internal immutable record `CachedInboxEntry` (ids, request type, response, error message, timestamps, retry count) written to the cache; a hit returns a fresh internal `CachedInboxMessage : IInboxMessage` built from it** | Plain strings and dates serialize with every `ICacheProvider` serializer (System.Text.Json, MessagePack); no provider entity (EF tracking, MongoDB attributes) enters the cache; the orchestrator never mutates the message it reads | One more internal type; `IInboxMessage` setters exist, so the copy ignores writes |
| **B) Cache the provider entity and return it** | No new type | EF entities carry tracking and navigation state and are not safe to serialize or to hand out as a shared instance; per-provider types differ |
| **C) Rebuild through `IInboxMessageFactory.Create` and set the properties** | Returns the provider's own type | Needs the scoped factory inside the decorator, one more construction per hit, and the factory's `Create` signature is built for new messages |

### Chosen Option: **A - dedicated entry and message copy** (recommended, pending the maintainer)

### Rationale

- C wins when a custom `IInboxStore` caller needs the provider's concrete message type on a hit; nothing in `src/` casts the result of `GetMessageAsync` (checked: the three call sites above).
- The key is `{CachingOptions.KeyPrefix}:x-store:inbox:{segment}` where the id passes through `StoreCacheKey.Segment` (design choice 5). The inbox table has no tenant column today (`IInboxMessage` has no `TenantId`; the id is the primary key), so the key mirrors the identity of the row and has no tenant segment. If a tenant column is added to the inbox, the key gains the tenant segment of `StoreCacheKey.Tenant` in the same change.
- Class: Dilemma

</details>

<details>
<summary><strong>5. Injective keys and the tenant segment (shared by the whole family)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `StoreCacheKey`: every variable segment is percent-encoded (`%`, `:`, and the glob characters `*`, `?`, `[`, `]`, `\`), kinds are fixed literals, the family namespace segment is `x-store` (not a valid CLR identifier, so it cannot equal the request-type segment of `DefaultCacheKeyGenerator` keys), and the tenant segment is `t:{encoded tenant}` for a tenant and absent for none** | Injective (two distinct tuples never give one key, FsCheck-proven); a hostile id cannot widen a `RemoveByPattern` glob; a key with no tenant and a key of the tenant `default` never collide; tenant keys sit under the `{prefix}:t:{tenant}:` namespace that the DSR purge of #2407 removes | One more key builder next to `DefaultCacheKeyGenerator` |
| **B) Reuse `CacheKeyRedactor.EscapeSegment` only (`%` and `:`)** | Existing code | Does not encode glob characters: an id such as `a*` widens `RemoveByPatternAsync`; no tenant helper |
| **C) Wait for the core `TenantCacheKey` of #1257** | One implementation across the repository | #1257 is not on `main` and proposes the segment form `t.{encoded}` / `g`; this family cannot wait |

### Chosen Option: **A - `StoreCacheKey` in `Encina.Caching`**

### Rationale

- **Class:** Settled
- **Settled by:** the injectivity requirement of the plan batch (a tenant-qualified id must map to exactly one key; the earlier finding on DSR keys: tenant `-` colliding with "no tenant" and an unescaped `:` colliding across tenants) and AGENTS.md section 3 (compliance gates fail closed, no widening of a tenant namespace). B fails the glob case; C is unavailable. The reserved `default` tenant of #1257 is a normal tenant id for this builder, so `t:default` and "no tenant" are different keys; when #1257 lands, `StoreCacheKey.Tenant` delegates to its `TenantCacheKey` segment in one commit and the family's keys keep their tenant prefix shape (the plans of #696 and #699 have no tenant, #697 and #698 state theirs).

</details>

<details>
<summary><strong>6. Cache failure semantics</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A failing cache call is caught, counted and logged by exception type (`ForLogging()`), and the call falls through to the inner store; with `CachingOptions.ThrowOnCacheErrors` the decorator returns `Left` with `store_cache.failed` (`ErrorCategory.Internal`, ADR-062 rule R4) instead** | The cache is an optimization: its outage never fails a message; one switch already exists for strict deployments; ROP, no exception | A permanently broken cache is only visible in metrics and logs |
| **B) Always fail the operation on a cache error** | Loud | A Redis outage stops the whole inbox |
| **C) Disable the cache after N failures** | Self-heals | Hidden state machine in a decorator |

### Chosen Option: **A - fall through, strict switch returns `Left`**

### Rationale

- **Class:** Settled
- **Settled by:** AGENTS.md section 3 (Railway Oriented Programming: no exception for business logic; an exception passed to a logger goes through `ForLogging()`) and ADR-062 (every new code states its category). A `Left` from the inner store is never swallowed: it is returned as it is and never cached.

</details>

<details>
<summary><strong>7. EventIds</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A new range `CachingStores = (350, 389)` from the family block 350-399, ids 350-353 for this plan** | No collision with the other families (300-349 is `CachingInvalidation` and `ComplianceDSRErasure`) | One registry entry and an allocation-test mapping |
| **B) Ids inside `Caching` (3500-3549)** | No registry change | Competes with the free ids that #2408 and other caching plans reserved |

### Chosen Option: **A - new range 350-389**

### Rationale

- **Class:** Trivial
- **Settled by:** the assignment of block 350-399 to this family in the plan batch registry (`artifacts/decisions/plan-batch/eventid-blocks.md`) and AGENTS.md section 7 (register the range in `EventIdRanges.cs` first; the allocation test maps `Encina.Caching` to it). Ids are packed: 350-353 here, 354-360 are #698, and #696 and #699 reuse 350-353.

</details>

---

## Implementation Phases

### Phase 1: Shared key builder and registration helper (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

PR 1 holds exactly these 9 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/MessagingStores/StoreCacheKey.cs` | new |
| 2 | `src/Encina.Caching/MessagingStores/StoreCachingRegistration.cs` | new |
| 3 | `src/Encina/Diagnostics/EventIdRanges.cs` | modify (`CachingStores = (350, 389)`) |
| 4 | `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` | modify (map `Encina.Caching` to the new range) |
| 5 | `src/Encina/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 6 | `tests/Encina.UnitTests/Caching/MessagingStores/StoreCacheKeyTests.cs` | new |
| 7 | `tests/Encina.PropertyTests/Caching/StoreCacheKeyInjectivityProperties.cs` | new |
| 8 | `tests/Encina.UnitTests/Caching/MessagingStores/StoreCachingRegistrationTests.cs` | new |
| 9 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |

Both new types are internal and `Encina.Caching` has no `InternalsVisibleTo` entry for `Encina.GuardTests`, so PR 1 has no guard test file; their argument checks are asserted by the unit tests and the guard targets in the manifest are 0 with that reason.

Tasks:

1. **Create** `internal static class StoreCacheKey` with `Segment(string value)` (percent-encodes `%`, `:`, `*`, `?`, `[`, `]`, `\`; throws `ArgumentException` for null or empty because an empty id would collide with an absent one), `Tenant(string? tenantId)` (`t:{Segment(tenantId)}` or `null`/empty for none) and `Build(string keyPrefix, string? tenantId, string kind, params string[] segments)` returning `{prefix}[:t:{tenant}]:x-store:{kind}:{seg}:...`, plus `PatternFor(string keyPrefix, string kind)` for the family-wide eviction pattern of one kind. Pure, no I/O.
2. **Create** `internal static class StoreCachingRegistration` with `Decorate<TService>(IServiceCollection services, Func<IServiceProvider, TService, TService> factory, string serviceName)`: finds the non-keyed `TService` descriptor, throws `InvalidOperationException` naming `serviceName` and the required order when there is none, replaces it with a descriptor of the same lifetime that builds the inner service from the old descriptor (instance, factory or `ActivatorUtilities`) and passes it to `factory`; a second call stacks a second decorator (the DI test proves the order).
3. **Register** `CachingStores = (350, 389)` and the allocation-test mapping (task 3 and 4 of the table).
4. **Tests**: key encoding of every special character; hostile ids (`a:b`, `a*`, `%3A`) never equal another tuple's key; tenant `default` differs from no tenant; `PatternFor` is a prefix of every key of its kind; FsCheck: for random `(tenant, kind, segments)` tuples two different tuples never give one key and `Segment` is reversible by percent-decoding; the helper replaces an instance, a factory and a type registration, keeps the lifetime, throws when the service is absent, and a stacked second decoration wraps the first.
5. **Manifest (same PR as the tests)**: targets with one-sentence justifications through `mechanical-fixer` (Phase 3 table).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #695 adds a cache decorator for IInboxStore; this PR adds only the shared
foundation of the cache-stores family (key builder, descriptor-decoration helper, EventId range) so the next PR builds on it.

TASK:
1. Create StoreCacheKey and StoreCachingRegistration in src/Encina.Caching/MessagingStores as described in the plan.
2. Register EventIdRanges.CachingStores = (350, 389) and map Encina.Caching to it in EncinaEventIdAllocationTests.
3. Write the unit tests and the FsCheck injectivity property; ask mechanical-fixer for the PublicAPI line and the manifest entries.

KEY RULES:
- Every variable segment goes through StoreCacheKey.Segment; no string concatenation of ids elsewhere.
- The helper fails loudly when the service is not registered; it never returns silently.
- No scoped service is captured by a singleton; the decorated lifetime is preserved.
- Zero warnings; CRAP of every method at most 10; tests execute the real classes.

REFERENCE FILES:
- src/Encina.Caching/KeyGeneration/CacheKeyRedactor.cs
- src/Encina.Caching/KeyGeneration/DefaultCacheKeyGenerator.cs
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs (DecorateService)
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

### Phase 2: Inbox cache decorator and registration (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

PR 2 holds exactly these 13 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/MessagingStores/CachingInboxStoreDecorator.cs` | new (internal sealed) |
| 2 | `src/Encina.Caching/MessagingStores/CachedInboxEntry.cs` | new (internal; entry record and the `CachedInboxMessage` copy) |
| 3 | `src/Encina.Caching/MessagingStores/InboxCachingOptions.cs` | new (public) |
| 4 | `src/Encina.Caching/MessagingStores/StoreCachingServiceCollectionExtensions.cs` | new (public `AddEncinaInboxCaching`) |
| 5 | `src/Encina.Caching/MessagingStores/StoreCacheErrors.cs` | new (`store_cache.failed`, category `Internal`) |
| 6 | `src/Encina.Caching/MessagingStores/StoreCacheTelemetry.cs` | new (meter `Encina.Caching.Stores`) |
| 7 | `src/Encina.Caching/MessagingStores/StoreCacheLog.cs` | new (ids 350-353, Phase 4) |
| 8 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 9 | `tests/Encina.UnitTests/Caching/MessagingStores/CachingInboxStoreDecoratorTests.cs` | new |
| 10 | `tests/Encina.GuardTests/Caching/StoreCachingGuardTests.cs` | new |
| 11 | `tests/Encina.UnitTests/Caching/MessagingStores/InboxCachingRegistrationTests.cs` | new (DI test with `ValidateOnBuild` and `ValidateScopes`, both orders with OpenTelemetry) |
| 12 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 13 | `changelog.d/695-inbox-store-cache.added.md` | new (`mechanical-fixer`) |

Tasks:

1. **Create** `internal sealed class CachingInboxStoreDecorator : IInboxStore` with the constructor `(IInboxStore inner, ICacheProvider cache, InboxCachingOptions options, IOptions<CachingOptions> cachingOptions, TimeProvider timeProvider, StoreCacheTelemetry telemetry, ILogger<CachingInboxStoreDecorator> logger)`. `GetMessageAsync`: build the key; `cache.GetAsync<CachedInboxEntry>`; a hit returns `Right(Some(new CachedInboxMessage(entry)))` after checking `ExpiresAtUtc` against the clock (an expired entry is removed and treated as a miss); a miss calls the inner store, returns a `Left` untouched, and caches the result only when `Some`, `IsProcessed`, `Response is not null`, the response is within `MaxCachedResponseLength` and the remaining retention is positive. Every cache call is wrapped (design choice 6). `AddAsync`, `MarkAsProcessedAsync`, `CacheHandlerErrorAsync`, `MarkAsFailedAsync`, `GetExpiredMessagesAsync` and `SaveChangesAsync` pass through unchanged. `RemoveExpiredMessagesAsync` calls the inner store and, on `Right`, removes the key of every id.
2. **Create** `public sealed class InboxCachingOptions { TimeSpan Duration (default 1 hour, must be positive); int MaxCachedResponseLength (default 32768, must be positive) }`; setters reject invalid values with `ArgumentOutOfRangeException`. The issue's `Enabled` and `CacheKeyPrefix` are not options: registering the extension is the opt-in, and the prefix is `CachingOptions.KeyPrefix` (one prefix per application, so a pattern flush of the prefix covers every cache of the family).
3. **Create** `public static IServiceCollection AddEncinaInboxCaching(this IServiceCollection services, Action<InboxCachingOptions>? configure = null)`: `ArgumentNullException.ThrowIfNull(services)`; registers `Configure<InboxCachingOptions>`, `TryAddSingleton<StoreCacheTelemetry>`, `TryAddSingleton(TimeProvider.System)`, then `StoreCachingRegistration.Decorate<IInboxStore>` (the decorator resolves `ICacheProvider` as required: a missing cache provider fails at the first resolution, and the DI test proves the message).
4. **Create** `StoreCacheErrors.Failed(Exception)` returning the categorized error (`EncinaErrors.FromException("store_cache.failed", ErrorCategory.Internal, ex, ...)`, category named explicitly with the `category:` argument so the completeness scan recognises it).
5. **Tests (unit)**: hit returns the copy without calling the inner store; miss caches only processed entries with a response; `None`, unprocessed, failed (`RetryCount > 0` and no response) and oversized entries are never cached; the TTL is the minimum of `Duration` and the remaining retention (`FakeTimeProvider`); an expired cached entry is a miss; a `Left` from the inner store is returned and never cached; `RemoveExpiredMessagesAsync` evicts after `Right` and not after `Left`; a throwing cache falls through, counts and logs by type; with `ThrowOnCacheErrors` it returns `Left` with the code and category; every other member delegates exactly once; the copy ignores setter writes; a hostile `messageId` yields an encoded key and never a pattern.
6. **Tests (guard)**: `AddEncinaInboxCaching(null)` throws; `InboxCachingOptions` setters reject zero and negative values. The decorator constructor is internal, so its null checks are covered by the unit tests (guard target 0 with that reason).
7. **Manifest and changelog** through `mechanical-fixer` (Phase 3 table; fragment text: "Added opt-in `AddEncinaInboxCaching`: a cache-aside decorator for the inbox store that serves processed messages from the cache for any of the ten database providers").

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #695: a cache decorator for IInboxStore in Encina.Caching. PR 1 added StoreCacheKey
and StoreCachingRegistration. IInboxStore methods return Task<Either<EncinaError, ...>>; the lookup returns Option<IInboxMessage>.

TASK:
1. Create CachingInboxStoreDecorator, CachedInboxEntry (+ CachedInboxMessage), InboxCachingOptions, AddEncinaInboxCaching,
   StoreCacheErrors, StoreCacheTelemetry and StoreCacheLog (ids 350-353) as described in the plan.
2. Cache only messages that are processed and have a response; never None, never unprocessed; TTL = min(Duration, remaining retention).
3. Write the unit, guard and DI tests; ask mechanical-fixer for PublicAPI lines, the manifest and the changelog fragment.

KEY RULES:
- A Left from the inner store is returned untouched and never cached; a cache failure falls through (ThrowOnCacheErrors returns Left).
- The key is StoreCacheKey.Build(...); the id is never concatenated by hand; no key, id or response in a log or tag.
- Exceptions go to loggers through ForLogging(); EncinaError.Message never reaches a log.
- Never inject a scoped service into a singleton; the decorator is scoped, the telemetry is a singleton.
- Time comes from TimeProvider. Zero warnings; CRAP of every method at most 10.

REFERENCE FILES:
- src/Encina.Messaging/Inbox/IInboxStore.cs, InboxOrchestrator.cs
- src/Encina.OpenTelemetry/MessagingStores/InstrumentedInboxStore.cs
- src/Encina.Caching/Sharding/CachedShardDirectoryStore.cs
- docs/architecture/adr/048-inbox-record-vs-business-transaction.md
- docs/architecture/adr/062-error-category-contract.md
```

</details>

### Phase 3: Per-provider integration scenario, benchmark and coverage targets (pull request 3)

<details>
<summary><strong>Tasks</strong></summary>

PR 3 holds exactly these 15 files (12 tests and the benchmark, then the three documentation files):

| # | File | Change |
|---|------|--------|
| 13 | `docs/messaging/inbox.md` | modify (`docs-writer`: the opt-in cache, the call-order rule, the processed-only rule) |
| 14 | `docs/features/query-caching.md` | modify (`docs-writer`: link to the store caches) |
| 15 | `docs/INVENTORY.md` | modify (`docs-writer`: the `MessagingStores` folder) |
| 1 | `tests/Encina.IntegrationTests/Messaging/InboxCachingScenario.cs` | new (shared scenario: processed message served from the cache, redelivery of a failed message re-runs the handler, removal evicts) |
| 2-4 | `tests/Encina.IntegrationTests/ADO/{SqlServer,PostgreSQL,MySQL}/Inbox/InboxStoreADOCachingTests.cs` | new (3) |
| 5-7 | `tests/Encina.IntegrationTests/Dapper/{SqlServer,PostgreSQL,MySQL}/Inbox/InboxStoreDapperCachingTests.cs` | new (3) |
| 8-10 | `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/{SqlServer,PostgreSQL,MySQL}/Inbox/InboxStoreEF<Db>CachingTests.cs` | new (3) |
| 11 | `tests/Encina.IntegrationTests/Infrastructure/MongoDB/Stores/InboxStoreMongoDBCachingTests.cs` | new |
| 12 | `tests/Encina.BenchmarkTests/Caching/InboxStoreCacheBenchmarks.cs` | new (cached versus uncached duplicate lookup over a fake store with a fixed latency; `BenchmarkSwitcher`) |

The folders are the ones that hold today's `InboxStore*Tests.cs` of each provider (verified on `origin/main` for the ten providers); the new classes use the same `[Collection("<Family>-<Database>")]` fixtures and the same harness as `InboxPipelineScenario`, with `MemoryCacheProvider` as the cache, so no new container is needed. A Redis-backed run of the shared scenario is added to the existing Redis integration collection when the maintainer asks for it; the 10 database providers are the required matrix here.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
PR 1 and PR 2 of issue #695 are merged: AddEncinaInboxCaching decorates IInboxStore. This PR proves it against the ten database providers.

TASK:
1. Write InboxCachingScenario (static asserts like InboxPipelineScenario) and one thin class per provider using its shared collection fixture.
2. Scenario: after a message is processed the next GetMessageAsync is a cache hit (inner call count 0); a failed message is never served
   from the cache and its handler re-runs until MaxRetries; RemoveExpiredMessagesAsync evicts the id.
3. Add the benchmark with BenchmarkSwitcher and verify the filter with --list flat.

KEY RULES:
- [Collection("<Family>-<Database>")] shared fixtures only; ClearAllDataAsync in InitializeAsync; never dispose the shared fixture.
- Integration tests execute the real stores and the real decorator; no mocks of either.
- Outputs go under artifacts/; no BenchmarkRunner.Run<T>().

REFERENCE FILES:
- tests/Encina.IntegrationTests/Messaging/InboxPipelineScenario.cs
- tests/Encina.IntegrationTests/ADO/PostgreSQL/Inbox/InboxStoreADOTests.cs
- tests/Encina.IntegrationTests/Collections.cs (per family)
```

</details>

### Phase 4: Observability, logging and coverage targets

<details>
<summary><strong>Tasks</strong></summary>

1. **Telemetry** (`StoreCacheTelemetry`, shared by the family): meter `Encina.Caching.Stores`, counter `encina.store_cache.requests` with tags `store` (`inbox`) and `outcome` (`hit`, `miss`, `skipped`, `failure`), counter `encina.store_cache.evictions`. No cache key, message id or response in a tag or span (ADR-054 D6). ADR-054 D4: the inbox operations carry no tenant scope (`IInboxStore` receives none), so there is no tenant tag; when a tenant column exists the tag is added in the same change. Spans of the cache calls already exist in `InstrumentedCacheProvider` (`Encina.OpenTelemetry`), so the decorator adds counters only.
2. **Logs** (`StoreCacheLog`, `[LoggerMessage]`, range 350-389): 350 cache read failed (store, exception type), 351 cache write failed, 352 cache eviction failed, 353 entry not cached (store, reason code: `too_large`, `no_retention_left`). Exceptions through `ForLogging()`; ids, keys and responses are never logged.
3. **Error code**: `store_cache.failed`, `ErrorCategory.Internal` (ADR-062, rule R4: provider failures are `Internal`).
4. **Coverage targets** (per file, per flag; measured values replace the estimates before PR 2 and PR 3 open):

| File | unit | guard | property | integration | Justification |
|------|------|-------|----------|-------------|---------------|
| `MessagingStores/StoreCacheKey.cs` | 100 | 0 | 90 | 0 | Pure encoding fully unit tested; internal type without `InternalsVisibleTo` for `Encina.GuardTests`; the injectivity property is the invariant; no I/O to integrate |
| `MessagingStores/StoreCachingRegistration.cs` | 95 | 0 | 0 | 0 | Registration paths unit tested; internal type; no property over registrations |
| `MessagingStores/CachingInboxStoreDecorator.cs` | 95 | 0 | 0 | 70 | Every branch unit tested with a fake store and cache; internal type; the integration scenario reaches hit, miss and eviction against real stores |
| `MessagingStores/CachedInboxEntry.cs` | 100 | 0 | 0 | 50 | Mapping fully unit tested; internal; reached by the scenario on every hit |
| `MessagingStores/InboxCachingOptions.cs` | 100 | 100 | 0 | 0 | Setter validation fully reachable from both flags; no integration surface |
| `MessagingStores/StoreCachingServiceCollectionExtensions.cs` | 95 | 90 | 0 | 0 | DI test and guard tests reach every line; integration uses it indirectly |
| `MessagingStores/StoreCacheErrors.cs`, `StoreCacheTelemetry.cs`, `StoreCacheLog.cs` | 90 | 0 | 0 | 0 | Counters and log methods asserted through the decorator tests; internal types |

5. **Contract and load**: no contract test (no new public interface; the decorated interface keeps its contract, asserted by the unit tests with the real `FakeInboxStore` behind it); no load test (the decorator holds no shared state beyond the cache provider, which the cache packages already load-test); the manifest `reason` says so.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
The inbox cache decorator of issue #695 exists. Telemetry, logs, error code and coverage targets are added with the PR that introduces
each caller (PR 2), and measured per flag.

TASK:
Add StoreCacheTelemetry, StoreCacheLog (350-353) and StoreCacheErrors in PR 2; run each flag's test project with coverage and compare with the
targets of the plan; record the targets and one-sentence justifications through mechanical-fixer.

KEY RULES:
- EventIds only 350-353 here; the range is registered first in EventIdRanges.cs and mapped in the allocation test.
- No cache key, message id, response or tenant in a log, tag or attribute.
- The new error code names its ErrorCategory (ADR-062) with the category: argument.
- Documentation never types coverage figures.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- docs/architecture/adr/054-observability-conventions-and-trace-propagation.md
- .github/coverage-manifest/Encina.Caching.json
```

</details>

### Phase 5: Documentation and finalization

<details>
<summary><strong>Tasks</strong></summary>

1. XML documentation (`<summary>`, `<remarks>`, `<param>`, `<example>`) on `AddEncinaInboxCaching` and `InboxCachingOptions`, including the call-order rule (provider first, caching after) and the statement that only processed messages are cached.
2. `docs/messaging/inbox.md` and `docs/features/query-caching.md` get a short section through `docs-writer` in PR 3 (no hand-typed figures; coverage cited with `cov:` markers); `Encina.Caching` has no README on `main`, so none is created by this plan.
3. `changelog.d/695-inbox-store-cache.added.md` (PR 2, `mechanical-fixer`); `PublicAPI.Unshipped.txt` of `Encina.Caching` and `Encina`.
4. `docs/INVENTORY.md`: the new `MessagingStores` folder (once, in PR 3).
5. Build with `dotnet build --configuration Release` (0 warnings), `dotnet test` of each flag, `dotnet run --file .github/scripts/coverage-report.cs -- --check-justifications`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
Issue #695 is implemented. Finish the documentation and verification of PR 2.

TASK:
Write the XML docs, ask docs-writer for the inbox page section, ask mechanical-fixer for PublicAPI lines, the changelog fragment and
the INVENTORY entry, and run the build, the tests and the coverage checks.

KEY RULES:
- Say the call-order rule and the processed-only rule in the public docs; never promise a gain for new message ids.
- Documentation cites coverage with cov: markers, never typed numbers.

REFERENCE FILES:
- docs/messaging/inbox.md
- docs/features/query-caching.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| AGENTS.md section 5 (database, caching) | The decorator is consistent across the 10 database providers and the cache providers; Memcached has no pub/sub and needs none here |
| AGENTS.md section 3 | ROP, `TimeProvider`, errors never swallowed, exceptions through `ForLogging()`, registration completeness proven by a DI test |
| ADR-048 | The inbox record is outside the business transaction; only the processed state is stable and cacheable |
| ADR-062 | The new error code states its `ErrorCategory` (`Internal`) |
| ADR-054 D4, D6 | No tenant tag without a tenant scope; no keys or data in telemetry |
| #712 | Cache Integration epic that this issue belongs to |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| ICacheProvider | src/Encina.Caching/Abstractions/ICacheProvider.cs | the cache the decorators read and write |
| IPubSubProvider | src/Encina.Caching/Abstractions/IPubSubProvider.cs | cross-instance messages (used by #698, not by this plan) |
| CachingOptions | src/Encina.Caching/Configuration/CachingOptions.cs | `KeyPrefix` and `ThrowOnCacheErrors` |
| CacheKeyRedactor | src/Encina.Caching/KeyGeneration/CacheKeyRedactor.cs | segment escaping precedent and log redaction |
| CachedShardDirectoryStore | src/Encina.Caching/Sharding/CachedShardDirectoryStore.cs | precedent for a cache decorator of a core store abstraction inside Encina.Caching |
| CachingPolicyStoreDecorator | src/Encina.Security.ABAC/Persistence/CachingPolicyStoreDecorator.cs | the decorator pattern named by the issues |
| IInboxStore | src/Encina.Messaging/Inbox/IInboxStore.cs | the decorated abstraction |
| InboxOrchestrator | src/Encina.Messaging/Inbox/InboxOrchestrator.cs | the only production reader of `GetMessageAsync` |
| InstrumentedInboxStore, DecorateService | src/Encina.OpenTelemetry | existing store decorator and the descriptor technique; layered with the cache decorator |
| EventIdRanges | src/Encina/Diagnostics/EventIdRanges.cs | EventId registry |
| InboxPipelineScenario | tests/Encina.IntegrationTests/Messaging/InboxPipelineScenario.cs | reusable per-provider inbox scenario |
| FakeInboxStore | src/Encina.Testing.Fakes/Stores/FakeInboxStore.cs | unit-test double of the inner store |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Caching` (new range `CachingStores`) | 350-389 (from block 350-399) | 350-353 used by this plan (shared failure logs and the skipped-entry log); 354-360 reserved by #698; #696 and #699 reuse 350-353; 361-389 free; 390-399 reserved for the Marten satellite of #697 |

### Estimated File Count

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | 9 | 1 | New: key builder, registration helper, decorator, entry, options, extension, errors, telemetry, log; modified: `EventIdRanges.cs` |
| Tests | 18 | 1 | New: key unit, injectivity property, registration unit (PR 1); decorator unit, guard, DI (PR 2); scenario, 10 provider classes, benchmark (PR 3); modified: the allocation test |
| Documentation | 0 | 3 | inbox page, query-caching page, `INVENTORY.md` |
| Configuration | 1 | 3 | Changelog fragment; `Encina.Caching.json` (touched in PR 1 and PR 2), PublicAPI (`Encina`, `Encina.Caching`) |
| **Total** | **28** | **8** | 3 pull requests: 9, 13 and 15 files (37 file touches: the manifest is touched twice), each at most 15 files and about 700 lines |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0, no backward compatibility. Issue #695: an opt-in cache decorator for IInboxStore in
Encina.Caching that serves processed inbox messages from ICacheProvider. This plan also creates the shared foundation of the
cache-stores family (StoreCacheKey, StoreCachingRegistration, StoreCache* telemetry/log/errors, EventId range 350-389).

IMPLEMENTATION OVERVIEW:
1. PR 1: StoreCacheKey (injective encoding of every segment, tenant segment, family namespace x-store), StoreCachingRegistration,
   the EventId range and its allocation test.
2. PR 2: CachingInboxStoreDecorator (processed entries only, TTL bounded by retention), options, AddEncinaInboxCaching, errors,
   telemetry, logs 350-353, tests, manifest and changelog.
3. PR 3: integration scenario over the 10 database providers with a Memory cache, and the benchmark.

KEY PATTERNS:
- Cache-aside on GetMessageAsync only for a message that IsProcessed and has a Response; never None, never unprocessed; no write-through.
- A Left is returned untouched and never cached; a cache failure falls through (ThrowOnCacheErrors returns Left store_cache.failed, Internal).
- Keys go through StoreCacheKey; no key, id, response or tenant in logs or tags; exceptions through ForLogging().
- The extension fails at registration when IInboxStore is not registered yet (call it after the provider).
- No scoped service in a singleton; DI test with ValidateOnBuild and ValidateScopes in both orders with Encina.OpenTelemetry.
- Apply Design Choices 2 and 4 as the Maintainer Decisions section says.

REFERENCE FILES:
- src/Encina.Messaging/Inbox/IInboxStore.cs, InboxOrchestrator.cs, MessagingServiceCollectionExtensions.cs (RegisterInbox)
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs, MessagingStores/InstrumentedInboxStore.cs
- src/Encina.Caching/Sharding/CachedShardDirectoryStore.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- .github/coverage-manifest/Encina.Caching.json
- tests/Encina.IntegrationTests/Messaging/InboxPipelineScenario.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Include | The feature is a cache of the inbox lookup; opt-in, cache-aside, processed entries only. |
| 2 | OpenTelemetry | ✅ Include | Counters `encina.store_cache.requests` and `evictions` with `store` and `outcome` tags; no keys or ids; no tenant tag because the inbox operations have no tenant scope (ADR-054 D4, D6); cache spans exist in `InstrumentedCacheProvider`. |
| 3 | Structured Logging | ✅ Include | `[LoggerMessage]` ids 350-353 in the new range `CachingStores`; exceptions through `ForLogging()`. |
| 4 | Health Checks | ❌ N/A | The cache is optional and its failure falls through; the cache providers have their own health checks. |
| 5 | Validation | ✅ Include | `InboxCachingOptions` setters reject non-positive values; the key builder rejects an empty segment. |
| 6 | Resilience | ✅ Include | A cache outage never fails a message (fall through); `ThrowOnCacheErrors` is the strict switch; no retry (the next lookup retries naturally). |
| 7 | Distributed Locks | ❌ N/A | Entries are immutable once cached; no concurrent mutation to lock. |
| 8 | Transactions | ✅ Include | Nothing is written to the cache before the business commit: the entry is created only from a read of a processed row (ADR-048), never from a write inside the transaction. |
| 9 | Idempotency | ✅ Include | The feature serves the idempotency check; the processed-only rule keeps exactly-once processing intact. |
| 10 | Multi-Tenancy | ✅ Include | The inbox row has no tenant today, so the key has none; `StoreCacheKey.Tenant` is injective and ready for a tenant column; stated, not invented. |
| 11 | Module Isolation | ❌ N/A | The inbox is not module scoped. |
| 12 | Audit Trail | ❌ N/A | A cache lookup is not an audit event; the inbox record is the audit of the receipt. |

**Deferred items:** none that need an issue from this plan. The response payload is cached in the same form the inbox row stores it, with a TTL bounded by the retention of that row; a subject-level erasure that must also clear the cache is the DSR purge of #2407 (tenant namespace) and does not reach inbox keys, which have no tenant: the follow-up is part of the inbox tenant-column work, not of this plan.

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| `ICacheProvider` and the Caching provider packages | ✅ Available | Memory, Redis, Valkey, Dragonfly, Garnet, KeyDB, Hybrid exist; Memcached is planned (#277) |
| Inbox stores of the 10 database providers | ✅ Available | ADO x3, Dapper x3, EF Core x3, MongoDB |
| #2408 (post-commit invalidation) | ⏭️ Sibling | Not required: this decorator never needs eviction timing |
| #1257 (`TenantCacheKey`) | ⏭️ Sibling | `StoreCacheKey.Tenant` delegates to it when it lands; not required |
| Maintainer answers to Design Choices 2 and 4 | Pending | Asked when work starts |

No blocking prerequisites.

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas (Design Choices 2 and 4) when work starts, records the answers under `## Maintainer Decisions`, then briefs three workers in the phase order.
2. After PR 2, #696 and #699 build on `StoreCachingRegistration`, `StoreCacheTelemetry` and `StoreCacheLog`; #698 and #697 build on `StoreCacheKey`.
3. After PR 3, comment on #695 with the measured benchmark so the maintainer can judge the gain on duplicate-heavy workloads.
