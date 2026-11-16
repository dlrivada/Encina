# Implementation Plan: Distributed cache for hydrated Marten aggregates

> **Issue**: [#697](https://github.com/dlrivada/Encina/issues/697)
> **Type**: Feature
> **Complexity**: High (5 phases, 3 pull requests of 12, 14 and 10 files)
> **Estimated Scope**: about 650 lines of production code, about 1400 lines of tests
> **Milestone**: v0.18.0 - Integration & Platform Capabilities (caching, event sourcing)

---

## Summary

`SnapshotAwareAggregateRepository<TAggregate>` and `MartenAggregateRepository<TAggregate>` rebuild an aggregate on every `LoadAsync(id)`: the snapshot (if any) plus the events after it (`LoadWithSnapshotAsync`, `FetchStreamAsync`, `LoadFromHistory`). The issue asks for a decorator that caches the hydrated state and invalidates it on `SaveAsync`.

Re-checked against `origin/main` (80b232c90), the plan adapts the sketch to the real contract and adds the hazards the sketch does not see:

1. **The interface is `IAggregateRepository<TAggregate>` (one type parameter, `Guid` ids, `Task<Either<EncinaError, ...>>`)** in `Encina.Marten`, not `IAggregateRepository<TAggregate, TId>`; the decorator keeps the four members (`LoadAsync(id)`, `LoadAsync(id, version)`, `SaveAsync`, `CreateAsync`).
2. **An aggregate is mutable.** A loaded aggregate is changed by `RaiseEvent` and emptied by `ClearUncommittedEvents` before and after `SaveAsync`. A cached instance handed to two callers would be corrupted by the first; every hit must return a fresh object.
3. **A cached state goes stale across instances.** `SaveAsync` appends with the expected stream version and `SaveChangesOrEjectAsync` commits inside the repository (the events are durable when it returns, `SnapshotAwareAggregateRepository.cs`), so evicting after `SaveAsync` is exact on one instance; another instance keeps serving the old state until its entry expires, unless the hit is validated against the stream version (Design Choice 3, the issue's "version-aware" criterion).
4. **The cached payload is the decrypted aggregate, and forgetting is invisible to the version check.** `[CryptoShredded]` goes on **event** properties, not on the aggregate (`src/Encina.Marten.GDPR/Attributes/CryptoShreddedAttribute.cs` lines 9-18: "encrypted during event serialization", `SubjectIdProperty` names "a sibling property on the same event type", example `UserEmailChangedEvent`). The aggregate's `Apply(object)` (`AggregateBase.cs` line 128) copies the decrypted value into an unmarked property, so the aggregate's own members carry no attribute and the store serializer (`CryptoShredderSerializer`, installed by `CryptoShredderSerializerFactory.Apply`, line 84) writes that state in plaintext. Forgetting a subject (`ISubjectKeyProvider.DeleteSubjectKeysAsync`) deletes the subject's keys and publishes the notification `SubjectForgottenEvent(SubjectId, ...)` when `CryptoShreddingOptions.PublishEvents` is true; it appends no event to any aggregate stream. A cached state therefore passes the version check after a forget and stays readable, possibly in plaintext in Redis, until its TTL. Design Choice 4 decides how the cache fails closed.
5. **The package cannot take the dependency for free.** `Encina.Marten` references only `Encina`, `Encina.DomainModeling` and `Encina.Messaging`; `Encina.Caching` brings Entity Framework Core, the distributed lock package and the messaging package. A Marten application that does not use caching must not pull them (pay-for-what-you-use, AGENTS.md section 1), so the decorator ships in a new satellite package (Design Choice 1).
6. **Marten's session tenant is never empty.** `IQuerySession.TenantId` is `*DEFAULT*` without tenancy (`tests/Encina.GuardTests/Marten/GDPR/PostgreSqlSubjectKeyProviderGuardTests.cs` line 107), and in the per-tenant-store modes of #326 (one store per schema or database) every tenant's session reports it, so the session tenant cannot qualify a key. The key's tenant comes from the tenant #326 binds (Design Choice 5).

This plan reuses the foundation of #695 (`StoreCacheKey`, `StoreCachingRegistration` pattern, the shared logs and telemetry shape) through an `InternalsVisibleTo` entry for the new package, and owns the Marten range `MartenCaching` 390-399 from the family block 350-399.

**Affected packages:** `Encina.Marten.Caching` (new: options, decorator, entry, key, registration, telemetry, logs), `Encina.Caching` (one `InternalsVisibleTo` line), `Encina` (EventId range registration only), `Encina.slnx`. `Encina.Marten` is not changed.

**Provider category**: Event sourcing (Marten, the primary and only provider; AGENTS.md section 5 "other categories"); Caching (8) for the cache behind it: Memory and Redis tested, Valkey, Dragonfly, Garnet and KeyDB share the Redis wrapper, Hybrid by the `ICacheProvider` contract, Memcached planned (#277). The database category does not apply (Marten runs on PostgreSQL only).

**Sibling plans (caching family)**: #695 owns the key builder, the registration pattern, the shared telemetry names and the cache failure semantics (its Design Choice 6, pending); #326 (Marten multi-tenancy) owns `IMartenSessionAccessor`, `EncinaMartenOptions.MultiTenancy.Isolation` and the `marten.tenant_required` `Left` (its decisions 5 and 6); this plan's tenant source follows #326 (Design Choice 5). #2407 purges the tenant namespace `{prefix}:t:{escaped tenant}:` for an erasure; the aggregate keys sit inside it.

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
- The version check sees appended events only; a crypto-shredding forget appends none, which is why Design Choice 4 exists. The TTL and its defaults are Design Choice 8.
- Class: Dilemma

</details>

<details>
<summary><strong>4. Crypto-shredded personal data and forgetting</strong></summary>

Every option caches only aggregate types opted in one by one (`Cache<TAggregate>()`); caching every type by default is excluded because it violates opt-in (AGENTS.md section 1). The question is what happens when the store crypto-shreds. Facts on `origin/main`: the attribute sits on event properties (`CryptoShreddedAttribute.cs` lines 9-18), so an aggregate's own members never carry it; `AggregateBase.Apply(object)` is one abstract method (`AggregateBase.cs` line 128), so the event types an aggregate applies cannot be discovered by reflection; `CryptoShredderSerializerFactory.Apply` replaces the store serializer with `CryptoShredderSerializer` (line 84), which is how `CryptoShreddingHealthCheck` (line 79) and `CryptoShreddingStartupValidationHostedService` (line 89) detect crypto-shredding; `DeleteSubjectKeysAsync` appends no event and its `SubjectForgottenEvent` carries only the subject id, and only when `PublishEvents` is true.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Fail closed per store: when the store's serializer is the crypto shredder (`IDocumentStore.Options.Serializer()` whose type full name is `Encina.Marten.GDPR.CryptoShredderSerializer`, so no package reference is needed), the startup validator of the package (`AggregateCachingStartupValidator`, a hosted service) throws `InvalidOperationException` and the host does not start, whichever aggregate types are opted in** | Decrypted personal data never reaches a cache; the check reads the installed serializer after every registration ran, so it does not depend on registration order; one rule, easy to test; follows the precedent of `CryptoShreddingStartupValidationHostedService` | An application that crypto-shreds cannot cache any aggregate, even one whose events carry no personal data |
| **B) Fail closed per type, with a declared event list: with crypto-shredding detected as in A, `Cache<TAggregate>(eventTypes)` must list the event types the aggregate applies; the validator refuses when the list is missing or any listed type has a `[CryptoShredded]` property at any depth (the classification `Encina.Marten.GDPR` already does at startup)** | Caches the non-personal aggregates of a crypto-shredding application | The list is hand-maintained: an event type added to `Apply` later and not listed re-opens the leak silently, which is fail-open by omission; needs a reference to `Encina.Marten.GDPR` or a name-based walker that duplicates its classifier |
| **C) Allow caching and evict on forget: a handler of `SubjectForgottenEvent` removes every aggregate entry of the tenant (the event names a subject, not an aggregate, so the eviction is by pattern over the `agg` namespace); the validator refuses when `PublishEvents` is false** | Caches every opted-in aggregate | Between key deletion and the handler, and in the L1 memory of other instances (Hybrid), a forgotten subject stays readable; the payload is plaintext in Redis until evicted; a failed eviction must fail the forget, which couples the GDPR package to the cache |
| **D) Never cache decrypted state: encrypt the cached payload with the subject's key** | The cache holds ciphertext only | An aggregate has no subject binding (a subject id sits on events), so there is no key to choose; not implementable with today's model |

### Chosen Option: **A - refuse caching when the store crypto-shreds** (recommended, pending the maintainer)

### Rationale

- A wins for any application that holds personal data under crypto-shredding, such as the reference application (health data): a cache never holds what a forget must make unreadable, and AGENTS.md section 3 requires compliance gates to fail closed. B wins for a crypto-shredding application whose hot aggregates carry no personal data and whose team accepts maintaining the event list. C wins only when a team must cache shredded aggregates and accepts a bounded window after a forget, which GDPR Article 17 ("without undue delay") makes hard to defend. D is listed for completeness.
- The refusal is a startup failure, not a `Left`: it is a configuration error found before any request, as with the crypto-shredding startup validator.
- Class: Dilemma

</details>

<details>
<summary><strong>5. Key tenant and the session the decorator uses</strong></summary>

The key shape is fixed by #695 Design Choice 5 (Settled there): `StoreCacheKey.Build(KeyPrefix, tenant, "agg", aggregate type full name, id:N)`, every variable segment percent-encoded, the tenant segment `t:{encoded tenant}` for a tenant and absent for none, so no two tenant, type and id tuples share a key and `t:default` differs from "no tenant". This entry decides where the tenant comes from. The session tenant is excluded by the facts: it is `*DEFAULT*` without tenancy and in every per-tenant store of #326, so two tenants of a schema-per-tenant or database-per-tenant deployment would share a key for the same aggregate id (deterministic or natural-key ids make that realistic, and the version check only lowers the odds).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Follow #326: the decorator takes `IMartenSessionAccessor` (#326 decision 6) instead of `IDocumentSession` and calls it at the start of each load, returning its `Left` (`marten.tenant_required`, `InvalidRequest`) untouched with no cache access; the tenant segment is `IRequestContext.TenantId`, the value #326 binds the session and selects the store with, and only when `EncinaMartenOptions.MultiTenancy.Isolation` is configured; with no isolation configured the key has no tenant segment (one store, one stream per id); a cross-tenant scope of #326 decision 12 bypasses the cache** | Correct in the conjoined, schema-per-tenant and database-per-tenant modes; fails closed with #326's `Left` instead of an exception at resolution; one tenant source across the Marten packages; the multi-tenancy-on signal for the telemetry tag is the same `Isolation` setting | PR 2 waits for #326 PR 1 (options) and PR 2 (accessor); it rests on #326 decision 6, which is pending |
| **B) Ship before #326: the decorator takes `IDocumentSession`, keys have no tenant segment, and the startup validator refuses to start when the store is multi-tenant (`StoreOptions.Events.TenancyStyle` is `Conjoined` or the store's tenancy is not single-tenant); the move to A comes with #326** | Does not wait for #326; today `Encina.Marten` has no tenancy (the #326 summary), so the shared store already lets every tenant read every stream and the cache leaks nothing new | The per-tenant-store mode of #326 (one single-tenant store per schema) passes the refusal check, so the refusal must be rewritten when #326 lands; a missing session surfaces as an exception at resolution, not a `Left`; two code paths over the life of the feature |

### Chosen Option: **A - the tenant #326 binds, through its accessor** (recommended, pending the maintainer)

### Rationale

- A wins when #326 decision 6 is answered with its option A and #326 lands first, which keeps one tenant model for every Marten consumer. B wins when #697 must ship before #326 and the maintainer accepts a refusal check that #326 replaces.
- Both options keep the key injective per #695 Design Choice 5 and never key by `IQuerySession.TenantId`.
- Class: Dilemma

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
- **Settled by:** the assignment of block 350-399 to this family in the plan batch registry (`artifacts/decisions/plan-batch/eventid-blocks.md`) and AGENTS.md section 7 (register the range first; the allocation test maps `Encina.Marten.Caching` to it). Ids: 390 read failed, 391 write failed, 392 evict failed, 393 version check failed (falls through to a full load), 394 aggregate not cached (a reason code such as `not_opted_in` or `too_large`; the set follows the answers to Design Choices 5 and 8, the id does not). The startup refusals of Design Choices 4 and 9 throw and log nothing of their own, so they need no id.

</details>

<details>
<summary><strong>7. Cache failure semantics (follows #695 Design Choice 6, pending)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) As #695 Design Choice 6 option A: a failing cache read, write or evict, or a failing `FetchStreamStateAsync`, is counted, logged by exception type (`ForLogging()`, ids 390-393) and falls through to the inner repository; with `CachingOptions.ThrowOnCacheErrors` the load returns `Left` `store_cache.failed` (`ErrorCategory.Internal`, ADR-062 rule R4)** | A cache outage never fails a load; the same switch and code as the rest of the family | A permanently broken cache shows only in metrics and logs; one global flag changes every caching feature |
| **B) As #695 Design Choice 6 option B: every cache failure returns `Left` `store_cache.failed`** | Loud | A Redis outage stops every aggregate load |

### Chosen Option: **whatever the maintainer answers for #695 Design Choice 6; A is recommended there** (recommended, pending the maintainer)

### Rationale

- This entry has no answer of its own: the family shares one failure model (#695 Design Choice 6, last bullet), so the decorator applies the option chosen there. In both options a `Left` from the inner repository is returned untouched and never cached (AGENTS.md section 3, errors never swallowed), and an eviction failure after `SaveAsync` never turns the committed save into a `Left`: it is logged (392) and the entry expires by TTL; with A the version check of Design Choice 3 still refuses the stale entry on the next hit.
- Class: Dilemma

</details>

<details>
<summary><strong>8. Defaults of <code>AggregateCachingOptions</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Duration` 10 minutes, `MaxPayloadLength` 262144 characters, and a per-type `Cache<TAggregate>(TimeSpan? duration)` that replaces the global `Duration` for that type** | With the version check of Design Choice 3 a long TTL never serves a stale version, so the TTL bounds only memory and the reach of an outage; 256 Ki characters fit an aggregate of a few hundred events of state while staying below the 512 MB Redis value limit by far; a replacing override is the issue's "configurable TTL per aggregate type" in its simplest form | A 256 Ki payload deserialized on every hit can cost more than a short replay; a forgotten override keeps the global value |
| **B) `Duration` 2 minutes, `MaxPayloadLength` 32768 characters (the response limit of #695 Design Choice 8, option A)** | Smaller memory footprint and one limit across the family | More misses; larger aggregates are never cached, often the ones that benefit most |

### Chosen Option: **A - 10 minutes, 262144 characters, replacing override** (recommended, pending the maintainer)

### Rationale

- A wins for aggregates whose replay is expensive (long streams) and memory is not the constraint; B wins for deployments with tight cache memory or many small aggregates. Both are option defaults the application overrides in `AddEncinaMartenAggregateCaching(options => ...)`; setters reject non-positive values in both.
- Class: Dilemma

</details>

<details>
<summary><strong>9. Registration order with <code>AddSnapshotableAggregate</code></strong></summary>

`AddSnapshotableAggregate<TAggregate>` registers with a plain `AddScoped` (`src/Encina.Marten/ServiceCollectionExtensions.cs` line 372), not `TryAdd`, so a call after `AddEncinaMartenAggregateCaching` adds a later descriptor that wins and the cache is silently lost.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The startup validator of Design Choice 4 resolves `IAggregateRepository<TAggregate>` for every opted-in type in a scope it creates (`IServiceScopeFactory`, never a scoped service in the singleton) and throws `InvalidOperationException` naming the type when the result is not the decorator; the XML docs state the order** | A wrong order fails at startup, visibly, whatever the call order of the application; the DI test covers both orders | Resolving a repository at startup creates a session per opted-in type once |
| **B) Document the order and assert it in the DI test only** | No startup work | An application that registers in the other order loses the cache silently; only a drop in the hit metric shows it |

### Chosen Option: **A - startup check of the resolved repository** (recommended, pending the maintainer)

### Rationale

- A wins whenever the cache is part of a capacity plan (a silent loss of the cache becomes a database load spike); B wins when startup time must stay minimal and the team owns one registration site.
- Class: Dilemma

</details>

<details>
<summary><strong>10. Cache providers of the integration test</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Marten plus Redis, and Marten plus Memory, in one integration class that runs each scenario against both caches** | Meets the issue's matrix ("IntegrationTests: Required, Marten + Redis cache, Docker (PostgreSQL + Redis)"); Redis proves the payload round trip across a real serializer and a shared store | One combined collection definition (the `Marten` and `Redis` collections are separate, `MartenFixture.cs` line 110 and `RedisFixture.cs` line 68) |
| **B) Memory only** | No extra container | Does not meet the issue |

### Chosen Option: **A - Marten plus Redis and Memory**

### Rationale

- **Class:** Settled
- **Settled by:** the test matrix of #697 ("Marten + Redis cache, Docker (PostgreSQL + Redis)"); B does not satisfy it.

</details>

---

## Implementation Phases

### Phase 1: The package skeleton and the key (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

PR 1 holds exactly these 12 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Marten.Caching/Encina.Marten.Caching.csproj` | new (references `Encina.Marten` and `Encina.Caching`; `InternalsVisibleTo` for `Encina.UnitTests`, `Encina.ContractTests` and `Encina.IntegrationTests`; PublicAPI analyzer) |
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
| 12 | `.github/coverage-manifest/Encina.Marten.Caching.json` | new (`mechanical-fixer`; `AggregateCacheKey.cs` unit 100, integration 0 with the reason "the integration test lands in PR 3") |

Tasks:

1. **Create** the csproj, modeled on `Encina.Marten.GDPR.csproj` (package id, description, tags, `GenerateDocumentationFile`, `LanguageExt.Core`, logging, options and hosting abstractions, the PublicAPI analyzer and files); a new `ProjectGuid`.
2. **Create** `internal static class AggregateCacheKey` with `Build(string keyPrefix, string? tenantId, Type aggregateType, Guid id)` returning `StoreCacheKey.Build(keyPrefix, tenantId, "agg", aggregateType.FullName!, id.ToString("N"))`; a null tenant means "no tenant segment" (multi-tenancy off, Design Choice 5); an empty or whitespace tenant throws `ArgumentException`, because it is never a valid tenant.
3. **Register** the range and the allocation-test mapping.
4. **Tests**: key shape for two tenants, two types and two ids never collide; hostile tenant and type names are encoded; the tenant `default` and the string `*DEFAULT*` differ from "no tenant" and from each other; `ArgumentException` for an empty or whitespace tenant.

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

PR 2 holds exactly these 14 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Marten.Caching/AggregateCachingOptions.cs` | new (public) |
| 2 | `src/Encina.Marten.Caching/CachedAggregateEntry.cs` | new (internal) |
| 3 | `src/Encina.Marten.Caching/CachingAggregateRepositoryDecorator.cs` | new (internal sealed) |
| 4 | `src/Encina.Marten.Caching/AggregateCachingServiceCollectionExtensions.cs` | new (public `AddEncinaMartenAggregateCaching`) |
| 5 | `src/Encina.Marten.Caching/AggregateCachingStartupValidator.cs` | new (internal hosted service: Design Choices 4 and 9) |
| 6 | `src/Encina.Marten.Caching/AggregateCachingLog.cs` | new (ids 390-394) |
| 7 | `src/Encina.Marten.Caching/AggregateCachingTelemetry.cs` | new (meter, tenant tag) |
| 8 | `src/Encina.Marten.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 9 | `tests/Encina.UnitTests/Marten/Caching/CachingAggregateRepositoryDecoratorTests.cs` | new |
| 10 | `tests/Encina.UnitTests/Marten/Caching/AggregateCachingRegistrationTests.cs` | new (DI test with `ValidateOnBuild` and `ValidateScopes`, both registration orders, the startup validator) |
| 11 | `tests/Encina.GuardTests/Marten/Caching/AggregateCachingGuardTests.cs` | new |
| 12 | `tests/Encina.GuardTests/Encina.GuardTests.csproj` | modify (`ProjectReference`) |
| 13 | `.github/coverage-manifest/Encina.Marten.Caching.json` | modify (`mechanical-fixer`; unit and guard targets of the new files, integration and contract 0 with the reason "the integration and contract tests land in PR 3") |
| 14 | `changelog.d/697-marten-aggregate-cache.added.md` | new (`mechanical-fixer`) |

Under Design Choice 5 option A, PR 2 needs #326 PR 1 (`EncinaMartenOptions.MultiTenancy`) and #326 PR 2 (`IMartenSessionAccessor`) merged; under option B it does not, and the decorator takes `IDocumentSession`.

Tasks:

1. **Create** `public sealed class AggregateCachingOptions` with `Duration`, `MaxPayloadLength` and `Cache<TAggregate>(TimeSpan? duration = null)` recording opted-in types with an optional TTL that replaces `Duration` for that type (defaults per Design Choice 8); setters reject non-positive values.
2. **Create** `internal sealed class CachingAggregateRepositoryDecorator<TAggregate> : IAggregateRepository<TAggregate>` with `(IAggregateRepository<TAggregate> inner, IMartenSessionAccessor sessions, IRequestContext requestContext, IOptions<EncinaMartenOptions> martenOptions, ICacheProvider cache, AggregateCachingOptions options, IOptions<CachingOptions> cachingOptions, TimeProvider timeProvider, AggregateCachingTelemetry telemetry, ILogger<...> logger)` (Design Choice 5 option A; option B takes `IDocumentSession` and drops the request context and Marten options). `LoadAsync(id)`: bypass when the type is not opted in or a cross-tenant scope is active; get the session from the accessor and return its `Left` untouched; build the key with `IRequestContext.TenantId` when `MultiTenancy.Isolation` is configured, with no tenant otherwise; read the entry; on a hit call `session.Events.FetchStreamStateAsync(id)` and serve only when the versions match (an absent stream or a failed version check reloads); deserialize into a fresh aggregate; on a miss call the inner repository, return a `Left` untouched, and cache a `Right` whose payload is within `MaxPayloadLength`. `SaveAsync` and `CreateAsync` throw `ArgumentNullException` for a null aggregate (the contract of `SnapshotAwareAggregateRepositoryContractTests`); `SaveAsync` calls the inner repository then evicts the key (after `Right` and after `Left`); `CreateAsync` and `LoadAsync(id, version)` delegate. Cache failures follow Design Choice 7.
3. **Create** `AddEncinaMartenAggregateCaching(this IServiceCollection services, Action<AggregateCachingOptions> configure)`: `ArgumentNullException.ThrowIfNull` on both; replaces the scoped `IAggregateRepository<TAggregate>` descriptor of each opted-in type, or wraps the open-generic `MartenAggregateRepository<>` when only the open generic is registered; registers `AggregateCachingStartupValidator` with `TryAddEnumerable`; fails when `ICacheProvider` is not registered at the first resolution (DI test).
4. **Create** `internal sealed class AggregateCachingStartupValidator : IHostedService` (singleton, takes `IServiceScopeFactory` and `IDocumentStore`): in `StartAsync` it throws `InvalidOperationException` when the store's serializer is `Encina.Marten.GDPR.CryptoShredderSerializer` (Design Choice 4 option A) and, in a scope it creates, when the resolved `IAggregateRepository<TAggregate>` of an opted-in type is not the decorator (Design Choice 9 option A); the messages name the type, never data.
5. **Tests (unit)**: hit with the same version serves without the inner call; hit with a newer stream version reloads and replaces the entry; miss caches; `Left` and oversized payloads are never cached; two hits return distinct instances with empty uncommitted events; `SaveAsync` evicts after `Right` and after `Left`; `CreateAsync` and the versioned load delegate; the accessor's `Left` is returned with no cache call; isolation off gives a key with no tenant segment and isolation on the request tenant; a cross-tenant scope bypasses; a failing cache or version check follows Design Choice 7; the opt-in check; the validator refuses a crypto-shredding store and a repository registered after the caching registration (`AddSnapshotableAggregate` called last), and accepts the order caching-last.
6. **Tests (guard)**: the extension (`services`, `configure` null), options setters (zero and negative `Duration`, `MaxPayloadLength` and per-type TTL).
7. **Manifest and changelog** through `mechanical-fixer` (fragment: "Added `AddEncinaMartenAggregateCaching`: an opt-in, per-aggregate-type, version-validated cache for Marten aggregate loads (new package `Encina.Marten.Caching`)").

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
Issue #697. PR 1 created Encina.Marten.Caching and AggregateCacheKey. IAggregateRepository<TAggregate> (Encina.Marten) has LoadAsync(id),
LoadAsync(id, version), SaveAsync and CreateAsync, all returning Task<Either<EncinaError, ...>>; aggregates are mutable.

TASK:
1. Create the options, the entry, the decorator, the extension, the startup validator, the logs (390-394) and the telemetry as described in the plan.
2. A hit is served only after the stream version matches; every hit deserializes a fresh aggregate.
3. Write the unit, guard and DI tests; ask mechanical-fixer for PublicAPI lines, the manifest and the changelog fragment.

KEY RULES:
- Apply the maintainer's answers to Design Choices 4, 5, 7, 8 and 9 once recorded; the plan describes the recommended options.
- Only opted-in aggregate types are cached; the startup validator refuses a store whose serializer is the crypto shredder.
- The tenant segment is the tenant #326 binds (IRequestContext.TenantId) and only when EncinaMartenOptions.MultiTenancy.Isolation is
  configured; never IQuerySession.TenantId (it is "*DEFAULT*" in every per-tenant store); the accessor's Left is returned untouched.
- A Left from the inner repository is returned untouched and never cached; SaveAsync evicts after Right and Left.
- Never hand out a cached instance; no key, id or payload in a log or tag; exceptions through ForLogging().
- The telemetry carries the tenant tag only when MultiTenancy.Isolation is configured (ADR-054 D4).
- Never inject a scoped service into a singleton: the decorator is scoped like the repository; the validator uses IServiceScopeFactory.
  Zero warnings; CRAP of every method at most 10.

REFERENCE FILES:
- src/Encina.Marten/IAggregateRepository.cs, Snapshots/SnapshotAwareAggregateRepository.cs, MartenAggregateRepository.cs
- src/Encina.Marten/ServiceCollectionExtensions.cs (AddSnapshotableAggregate, plain AddScoped)
- src/Encina.Marten.GDPR/CryptoShreddingStartupValidationHostedService.cs, Health/CryptoShreddingHealthCheck.cs (serializer detection)
- src/Encina.Caching/MessagingStores (#695); the IMartenSessionAccessor of #326
```

</details>

### Phase 3: Integration against Marten and a cache (pull request 3)

<details>
<summary><strong>Tasks</strong></summary>

PR 3 holds exactly these 10 files:

| # | File | Change |
|---|------|--------|
| 1 | `tests/Encina.IntegrationTests/Infrastructure/Marten/Caching/MartenRedisCollection.cs` | new (`[CollectionDefinition("Marten-Redis")]` implementing `ICollectionFixture<MartenFixture>` and `ICollectionFixture<RedisFixture>`, the existing fixtures of `Infrastructure/Marten/Fixtures` and `Infrastructure/Caching/Fixtures`) |
| 2 | `tests/Encina.IntegrationTests/Infrastructure/Marten/Caching/CachingAggregateRepositoryIntegrationTests.cs` | new (collection `Marten-Redis`; every scenario against the Redis and the Memory cache) |
| 3 | `tests/Encina.IntegrationTests/Encina.IntegrationTests.csproj` | modify (`ProjectReference`; `Encina.Caching.Redis` is already referenced) |
| 4 | `tests/Encina.ContractTests/Marten/Caching/CachingAggregateRepositoryDecoratorContractTests.cs` | new (the assertions of `SnapshotAwareAggregateRepositoryContractTests` run against the decorator) |
| 5 | `tests/Encina.ContractTests/Encina.ContractTests.csproj` | modify (`ProjectReference`) |
| 6 | `.github/coverage-manifest/Encina.Marten.Caching.json` | modify (`mechanical-fixer`; raises the integration and contract targets from 0 to the measured values) |
| 7 | `tests/Encina.BenchmarkTests/Encina.Marten.Caching/AggregateCache.md` | new (justification: the gain depends on database latency; the integration test counts the avoided stream reads) |
| 8 | `src/Encina.Marten.Caching/README.md` | new (`docs-writer`) |
| 9 | `docs/features/marten-aggregate-cache.md` | new (`docs-writer`) |
| 10 | `docs/INVENTORY.md` | modify (`docs-writer`) |

The integration test (Design Choice 10) runs each scenario against Redis and against Memory, with two repositories over one store to simulate two instances: instance A loads (miss), instance B loads (hit, no event fetch), instance A saves, instance B loads and receives the new version; under Design Choice 5 option A, two tenants with the same aggregate id never share an entry in conjoined mode; a store with crypto-shredding configured makes the host fail at startup; registering `AddSnapshotableAggregate` after the caching registration fails at startup. The contract test asserts that the decorator implements `IAggregateRepository<>` and is sealed, that `SaveAsync` and `CreateAsync` throw `ArgumentNullException` for a null aggregate, that saving an aggregate with no uncommitted events returns `Right` and that creating an aggregate with no events returns `Left`, with the real decorator over the real `SnapshotAwareAggregateRepository` and substitutes of the session and cache.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
PR 1 and PR 2 of issue #697 are merged. This PR proves the decorator against a real Marten store and ships the documentation.

TASK:
1. Add the Marten-Redis collection definition over the existing MartenFixture and RedisFixture; write the integration test against
   Redis and Memory: two repositories over one store, version validation, tenant isolation, the startup refusals.
2. Write the contract test (the SnapshotAwareAggregateRepository contract against the decorator); add the project references.
3. Measure the integration and contract flags and ask mechanical-fixer to raise their targets; write the benchmark justification file.
4. Ask docs-writer for the package README, the feature page and the INVENTORY entry.

KEY RULES:
- Shared collection fixtures only; never dispose the shared fixture; real Marten, real Redis and the real decorator, no mocks of them.
- Documentation cites coverage with cov: markers and types no figures.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Infrastructure/Marten/Fixtures/MartenFixture.cs, Infrastructure/Caching/Fixtures/RedisFixture.cs
- tests/Encina.ContractTests/Marten/Core/SnapshotAwareAggregateRepositoryContractTests.cs
- src/Encina.Marten.Caching
```

</details>

### Phase 4: Observability, logging and coverage targets

<details>
<summary><strong>Tasks</strong></summary>

1. **Telemetry** (`AggregateCachingTelemetry`, PR 2): meter `Encina.Marten.Caching`, counter `encina.aggregate_cache.requests` with tags `aggregate_type` (the type name, never an id) and `outcome` (`hit`, `stale`, `miss`, `bypass`, `failure`); the tag `tenant.id` is added only when multi-tenancy is enabled (ADR-054 D4), and the signal is `EncinaMartenOptions.MultiTenancy.Isolation` being configured (#326; under Design Choice 5 option B there is no tenant and no tag); no key, aggregate id or payload (ADR-054 D6). Spans of the cache calls come from `InstrumentedCacheProvider`.
2. **Logs** (`AggregateCachingLog`, `[LoggerMessage]`, range 390-399, packed from 390): 390 cache read failed, 391 cache write failed, 392 cache evict failed, 393 stream version check failed, 394 aggregate not cached (reason code). Exceptions through `ForLogging()`; never a key, id or payload.
3. **Error code**: the shared `store_cache.failed`, `ErrorCategory.Internal` (ADR-062 R4); no new code.
4. **Coverage targets** (per file, per flag; measured values replace the estimates before each PR opens). Each target is entered in the PR that adds the tests meeting it: PR 1 enters the key's unit target; PR 2 enters the unit and guard targets of its files and enters integration and contract as 0 with the reason "the integration and contract tests land in PR 3"; PR 3 raises those to the values below (raising a target is allowed, lowering one is not).

| File | unit | guard | contract | integration | Entered in | Justification |
|------|------|-------|----------|-------------|------------|---------------|
| `AggregateCacheKey.cs` | 100 | 0 | 0 | 50 | unit PR 1, integration PR 3 | Pure and fully unit tested; internal type without `InternalsVisibleTo` for `Encina.GuardTests`; the contract test does not build keys; the integration test reaches it on every load |
| `CachingAggregateRepositoryDecorator.cs` | 95 | 0 | 40 | 70 | unit PR 2, contract and integration PR 3 | Every branch unit tested with fakes; internal; the contract test reaches the argument checks and the save and create paths; the integration test reaches hit, stale, miss and eviction with a real store |
| `CachedAggregateEntry.cs` | 100 | 0 | 0 | 50 | unit PR 2, integration PR 3 | Mapping fully unit tested; internal; the contract scenarios never hit the cache; reached on every integration hit |
| `AggregateCachingOptions.cs` | 100 | 100 | 0 | 30 | unit and guard PR 2, integration PR 3 | Setter and `Cache<T>` validation reachable from both unit and guard flags; the contract test builds no options; the integration test configures them |
| `AggregateCachingServiceCollectionExtensions.cs` | 95 | 90 | 0 | 30 | unit and guard PR 2, integration PR 3 | DI test and guard tests reach every line; the contract test builds the decorator directly; the integration test registers it |
| `AggregateCachingStartupValidator.cs` | 95 | 0 | 0 | 60 | unit PR 2, integration PR 3 | Both refusals and the accepted order are unit tested; internal without `InternalsVisibleTo` for `Encina.GuardTests`; the integration test starts a host with a real store |
| `AggregateCachingTelemetry.cs`, `AggregateCachingLog.cs` | 90 | 0 | 0 | 0 | unit PR 2 | Counters and log methods asserted through the decorator tests; internal; the integration test asserts behavior, not counters |

5. **Contract, property, load**: the contract test of PR 3 runs the `SnapshotAwareAggregateRepositoryContractTests` assertions against the decorator; no property test (key injectivity is the FsCheck property of #695, and `AggregateCacheKey` only forwards to `StoreCacheKey`); no load test (the decorator holds no shared state beyond the cache provider); the manifest `reason` says so. The benchmark is justified in the `.md` of PR 3.

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

1. XML documentation on every public member (options, extension) with the opt-in rule, the version validation, the startup refusals (a crypto-shredding store, a repository registered after the cache) and an example.
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
- The docs state the opt-in, the version validation, the registration order and that a crypto-shredding store refuses the cache
  (per the answer to Design Choice 4); they type no coverage figures.

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
| CryptoShredderSerializer, CryptoShreddingStartupValidationHostedService | src/Encina.Marten.GDPR/Serialization, src/Encina.Marten.GDPR | the installed store serializer is detected by type name to refuse caching (Design Choice 4); the hosted service is the precedent of a startup refusal |
| IMartenSessionAccessor, `EncinaMartenOptions.MultiTenancy` | #326 (planned) | the session and its `Left`; the multi-tenancy-on signal (Design Choice 5) |
| MartenFixture, RedisFixture, SnapshotAwareAggregateRepositoryContractTests | tests/Encina.IntegrationTests/Infrastructure, tests/Encina.ContractTests/Marten/Core | combined `Marten-Redis` collection; the contract reused for the decorator |
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
| Production code | 9 | 3 | New: csproj, key, options, entry, decorator, extension, startup validator, log, telemetry; modified: `Encina.slnx`, `Encina.Caching.csproj`, `EventIdRanges.cs` |
| Tests | 7 | 5 | New: key unit, decorator unit, DI, guard, integration, `Marten-Redis` collection definition, contract; modified: allocation test and the four test project files (`UnitTests`, `GuardTests`, `IntegrationTests`, `ContractTests`) |
| Documentation | 3 | 1 | New: package README, feature page, benchmark justification; modified: `INVENTORY.md` |
| Configuration | 4 | 1 | New: two PublicAPI files of the new package, manifest, changelog fragment; modified: PublicAPI of `Encina` |
| **Total** | **23** | **10** | 3 pull requests: 12, 14 and 10 files (36 file touches: the manifest is touched three times and the new package's Unshipped file twice), each at most 15 files and about 750 lines |

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
2. PR 2: options, entry, CachingAggregateRepositoryDecorator, AddEncinaMartenAggregateCaching, startup validator, logs 390-394,
   telemetry, unit/guard/DI tests, manifest (integration and contract 0 until PR 3), changelog.
3. PR 3: Marten-Redis collection, integration test against Redis and Memory, contract test, manifest raise, benchmark justification,
   README, feature page, INVENTORY.

KEY PATTERNS:
- Cache only opted-in aggregate types; the startup validator refuses a store whose serializer is the crypto shredder
  ([CryptoShredded] sits on events, and a forget appends no event, so the version check cannot see it).
- Payload = Marten-serialized aggregate string plus version; a hit is served only if the stream version matches; every hit deserializes a fresh object.
- SaveAsync evicts after Right and Left; CreateAsync and the versioned load delegate; a Left is never cached.
- Key = StoreCacheKey with IRequestContext.TenantId when EncinaMartenOptions.MultiTenancy.Isolation is configured (#326), no tenant
  segment otherwise; never IQuerySession.TenantId; the session comes from #326's IMartenSessionAccessor and its Left is returned as is.
- Cache failures follow #695 Design Choice 6 (pending); the startup validator also refuses a repository registered after the cache.
- No key, id or payload in logs or tags; tenant tag only when MultiTenancy.Isolation is configured; exceptions through ForLogging().
- Apply the answers to Design Choices 1, 2, 3, 4, 5, 7, 8 and 9 once recorded.

REFERENCE FILES:
- src/Encina.Marten/IAggregateRepository.cs, Snapshots/SnapshotAwareAggregateRepository.cs, ServiceCollectionExtensions.cs
- src/Encina.Marten.GDPR/Encina.Marten.GDPR.csproj, CryptoShreddingStartupValidationHostedService.cs, Attributes/CryptoShreddedAttribute.cs
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
| 5 | Validation | ✅ Include | Options setters reject non-positive values; the startup validator refuses a crypto-shredding store (Design Choice 4) and a repository registered after the cache (Design Choice 9). |
| 6 | Resilience | ✅ Include | Cache and version-check failures follow #695 Design Choice 6 (Design Choice 7 here, pending): recommended, fall through to the full load with `ThrowOnCacheErrors` as the strict switch. |
| 7 | Distributed Locks | ❌ N/A | Optimistic concurrency by stream version already guards writes; the cache holds no mutable shared entry. |
| 8 | Transactions | ✅ Include | The entry is written only from a completed load; eviction follows `SaveAsync`, whose `SaveChangesOrEjectAsync` has committed the events when it returns; no cache write happens before a commit. |
| 9 | Idempotency | ❌ N/A | Loads are idempotent; command idempotency is the inbox's concern. |
| 10 | Multi-Tenancy | ✅ Include | Key tenant from the tenant #326 binds (`IRequestContext.TenantId`) when `MultiTenancy.Isolation` is configured, never the session's `*DEFAULT*`; injective keys inside the tenant namespace of the DSR purge (#2407); a missing tenant returns #326's `Left` `marten.tenant_required` before any cache access (fails closed); Design Choice 5. |
| 11 | Module Isolation | ❌ N/A | Aggregates are not module scoped by this feature. |
| 12 | Audit Trail | ❌ N/A | A cache hit is not an audit event; the event stream is the audit record. |

**Deferred items:** none. Caching under crypto-shredding by a declared event list or by forget-time eviction (Design Choice 4, options B and C) is an alternative the maintainer may choose, not a deferred item. Cross-instance invalidation over a backplane (Design Choice 3, option C) is likewise an alternative, not a deferred item.

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| #695 foundation (`StoreCacheKey`, shared telemetry and error code) | ⏭️ Sibling plan | PR 1 of this plan needs #695 PR 1 merged; PR 2 needs #695 PR 2 for `StoreCacheErrors` |
| `Encina.Marten` repositories and snapshots | ✅ Available | No change needed |
| #326 (Marten multi-tenancy) | ⏭️ Sibling plan | Under Design Choice 5 option A, PR 2 needs #326 PR 1 (`MultiTenancy` options) and PR 2 (`IMartenSessionAccessor`), and the tenant-isolation integration scenario needs #326's conjoined mode; under option B nothing waits for #326 |
| `Encina.Caching` `InternalsVisibleTo` for the new package | ✅ Planned in PR 1 | One line, next to the existing satellites |
| Maintainer answers to Design Choices 1, 2, 3, 4, 5, 7, 8 and 9 | Pending | Asked when work starts; Design Choice 7 follows #695 Design Choice 6 |

Blocking prerequisite: #695 PR 1 and PR 2; with Design Choice 5 option A, also #326 PR 1 and PR 2.

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas (Design Choices 1, 2, 3, 4, 5, 7, 8 and 9) when work starts, records the answers on the issue, then briefs three workers after #695 PR 2 (and #326 PR 2 under Design Choice 5 option A).
2. If the maintainer answers option B of Design Choice 1, the package skeleton of PR 1 disappears and the files move into `Encina.Marten`; the plan is re-split then.
3. After PR 3, comment on #697 with the integration result.
