# Implementation Plan: `TenantStoreRedis` — `ITenantStore` on Redis with change notifications

> **Issue**: [#597](https://github.com/dlrivada/Encina/issues/597)
> **Type**: Feature
> **Complexity**: Medium (5 phases, 1 new package, 4 pull requests)
> **Estimated Scope**: ~700 lines of production code + ~1,300 lines of tests

---

## Summary

Implement the tenant catalog on Redis: `TenantStoreRedis` implements `ITenantStore` and the `ITenantStoreWriter` of #596 with one Redis hash per tenant and a set as the index, and publishes a change notification on every write so other instances learn that a tenant changed. The issue's sample is replaced where it is wrong: its key layout lets the tenant id `index` collide with the index key (`encina:tenants:index`), its create overwrites an existing tenant, its multi-key transaction fails on Redis Cluster, and it reads `GetAllAsync` one tenant at a time.

What the code looks like today (`origin/main` `915db384e`):

- `ITenantStore` is read-only, `InMemoryTenantStore` is the only implementation, and `TenantInfo` carries a connection string that is a credential (`src/Encina.Tenancy/Abstractions/`). The contract that a persistent store implements (`ITenantStoreWriter`, `ITenantSecretProtector`, `TenantIdRules`, `TenantStoreRegistration.Replace`, the `InstrumentedTenantStore` decorator and the shared behavior suite `TenantStoreBehaviorSuite`) is **owned by the #596 plan**; this plan consumes it and does not redefine it.
- `Encina.Caching.Redis` (`src/Encina.Caching.Redis/`) references `Encina.Caching`, `Encina.DistributedLock`, `Encina.Messaging` and `StackExchange.Redis`, registers an `IConnectionMultiplexer` through `AddEncinaRedisCache`, and has `RedisPubSubProvider` (channel publish with `RedisChannel.Literal`). `Encina.Caching.Valkey` and `Encina.Caching.Garnet` are thin packages that reference it. No Redis package references `Encina.Tenancy`.
- `tests/Encina.TestInfrastructure/Fixtures/RedisFixture.cs` (image `redis:7-alpine`) and the collection in `tests/Encina.IntegrationTests/Infrastructure/Caching/Fixtures/RedisFixture.cs` exist; there is no Valkey, Dragonfly, Garnet or KeyDB fixture.
- #711 (open, plan on `main`) plans `CachedTenantStore` in `Encina.Tenancy` with `InvalidateAsync`; its cache is per process, so a change made on one instance needs a signal to reach the others: this plan supplies that signal.

**Provider category**: caching/Redis-protocol servers, outside the 10-provider database matrix; the store speaks only the RESP commands that Redis, Valkey, Dragonfly, Garnet and KeyDB share through `StackExchange.Redis`. **Affected packages**: `Encina.Tenancy.Redis` (new), `Encina` (EventId registry), test projects, `Encina.slnx`.

---

## Design Choices

<details>
<summary><strong>1. Package placement (Dilemma D1)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A `Tenancy/` folder in `Encina.Caching.Redis`** (the issue's "or integrated") | No new package; the connection registration is already there | Every Redis cache user takes a ProjectReference to `Encina.Tenancy`; the package would hold a catalog that is not a cache |
| **B) A new `Encina.Tenancy.Redis` package** referencing `Encina.Tenancy` and `StackExchange.Redis`, reusing the `IConnectionMultiplexer` that `AddEncinaRedisCache` (or the application) registers | Pay-for-what-you-use; named like `Encina.Tenancy.AspNetCore`; works unchanged with the Valkey and Garnet packages | One more package and manifest |
| **C) `Encina.MultiTenancy.Redis`** (the issue's name) | None | The tenancy packages are `Encina.Tenancy.*`; a second prefix would split them |

### Chosen Option: **B — `Encina.Tenancy.Redis`** (recommended, pending the maintainer)

### Rationale

- B wins when applications use Redis for caching without tenancy (the common case); A wins only if the maintainer wants a single Redis package for everything. The package takes the multiplexer from DI and does not create one, so it follows whichever package registered it.

</details>

<details>
<summary><strong>2. Redis as the system of record or as a cache in front of another store (Dilemma D2)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A primary store: the hash is the tenant record, no TTL, the operator runs the server with persistence (`appendonly yes`) and `maxmemory-policy noeviction`** | Matches the issue's title and acceptance criteria; one store for small deployments | Losing the data (flush, eviction, no persistence) makes every tenant unknown, so every request is denied: an outage, never a cross-tenant read |
| **B) A cache that mirrors another store, with a TTL** | Durability comes from a database store of #596 | A second store to keep consistent; belongs to the cache decorator of #711, which already caches any store; the issue's own alternatives list it as "valid hybrid" |
| **C) Both: the primary store and an optional TTL on the records** | Flexible | A TTL on a system of record deletes tenants silently |

### Chosen Option: **A — a primary store without TTL** (recommended, pending the maintainer)

### Rationale

- A wins for deployments that keep tenants in Redis on purpose (small, or already operating Redis with persistence); B is what the maintainer should choose when the tenant catalog must survive a Redis loss, and it needs no code here: a database store of #596 plus the `CachedTenantStore` of #711 with the change notifications of this plan. The "optional TTL" of the issue belongs to that decorator, not to the catalog. The documentation states the persistence and eviction requirements, and the health check reports the server's `maxmemory-policy` as data when the server allows `CONFIG GET` and otherwise says it could not verify.

</details>

<details>
<summary><strong>3. Change notifications: what is published and who consumes it (Dilemma D3)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Types in `Encina.Tenancy.Redis`: `TenantChange(TenantChangeKind Kind, string TenantId)`, published as JSON on `{prefix}:{tenants}:changes` after the transaction commits; `RedisTenantChangeSubscriber.SubscribeAsync(handler, ct)` for consumers** | Pay-for-what-you-use; no neutral abstraction before a second producer exists | Another backend that wants the same feed repeats the types |
| **B) A provider-neutral `ITenantChangeFeed` in `Encina.Tenancy`** | One consumer contract for #711 | Designs a port for one producer; changes `Encina.Tenancy` in this issue |
| **C) No notifications** | Smallest | Misses an acceptance criterion; per-process caches (#711) stay stale until their TTL |

### Chosen Option: **A — Redis-local types** (recommended, pending the maintainer)

### Rationale

- A wins until #711 or a second store needs the feed; B wins if the maintainer wants the cache invalidation of #711 specified against an interface from the start. Pub/sub is at-most-once: a missed message is possible, so the notification is an optimization and every consumer keeps its TTL as the correctness bound; the documentation says so. The payload carries the operation and the tenant id (not a secret, not the connection string); the subscriber logs only the code of a malformed payload.

</details>

<details>
<summary><strong>4. Servers that the tests run against (Dilemma D4)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Redis and Valkey in CI; Dragonfly, Garnet and KeyDB documented as protocol-compatible and untested** | The two servers that have a first-party Encina package or the reference implementation; one extra fixture | Three servers carry a compatibility claim with no test |
| **B) Redis only** | No new fixture | Valkey users have only a claim |
| **C) All five servers** | Complete evidence | Three more fixtures and images; `AGENTS.md` section 5 lists eight caches but the tenant store is not a cache feature |

### Chosen Option: **A — Redis and Valkey** (recommended, pending the maintainer)

### Rationale

- A wins on cost against evidence; C wins if the maintainer wants the claim "works on all RESP servers" without qualification. The commands used are `HSET`, `HGETALL`, `SADD`, `SREM`, `SMEMBERS`, `EXISTS`, `DEL`, `PUBLISH`, `SUBSCRIBE` and `MULTI` with `WATCH`-free conditions, all in the common subset; the Valkey image is the vendor LTS (`AGENTS.md`, LTS default images).

</details>

<details>
<summary><strong>5. Key layout, atomicity and Redis Cluster</strong></summary>

**Class:** Settled
**Settled by:** the Redis Cluster specification (https://redis.io/docs/latest/operate/oss_and_stack/reference/cluster-spec/: multi-key transactions need one hash slot, forced with a hash tag), and `AGENTS.md` section 3 (tenant-qualified ids must be injective)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `{prefix}:{tenants}:t:{tenantId}` for records, `{prefix}:{tenants}:idx` for the index set: the `t:` namespace makes the key injective (the id is the whole suffix, so no id can equal the index key), the hash tag `{tenants}` puts every key in one slot** | Injective; `MULTI` works on Cluster; a catalog is small, so one slot is not a hot spot | All tenants live on one shard |
| **B) The issue's `encina:tenants:{id}` and `encina:tenants:index`** | As written | The id `index` collides with the index key (a wrong-type error or a corrupted index); no hash tag, so `MULTI` fails with CROSSSLOT on Cluster |

### Chosen Option: **A — namespaced keys with a hash tag**

### Rationale

- Only A is injective and cluster-safe. Create uses a transaction with `Condition.KeyNotExists(record)`, so a second create fails with `Left(tenancy.tenant_exists)` instead of overwriting; update uses `KeyExists`; delete uses `KeyExists` and removes the record and the index entry together. `GetAllTenantsAsync` reads the index and then the records in one pipelined batch; an index entry whose record is gone (an eviction) is skipped and logged, and never reported as a tenant.

</details>

<details>
<summary><strong>6. Cancellation, time and telemetry</strong></summary>

**Class:** Trivial
**Settled by:** `AGENTS.md` sections 3 (`TimeProvider`, errors never swallowed) and 6 (the telemetry decorator of #596 is reused: provider tag `redis`)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `.WaitAsync(cancellationToken)` on every `StackExchange.Redis` task, `TimeProvider` for `createdAtUtc`/`updatedAtUtc` (ISO 8601 `O`), spans and counters from `InstrumentedTenantStore`** | The client has no token parameter, so this is the available cancellation; no telemetry code in the store | A cancelled wait does not cancel the command already sent |
| **B) Ignore the token** | Simplest | Breaks callers' cancellation |

### Chosen Option: **A — wait with the token, reuse the decorator**

### Rationale

- The store itself has no logging except the ids of its own range (Event ID Allocation); the connection string is protected through `ITenantSecretProtector` before it is written to the hash field.

</details>

---

## Implementation Phases

### Phase 1: Package skeleton, keys and mapping (pull request 1)

<details>
<summary>Tasks</summary>

1. `src/Encina.Tenancy.Redis/Encina.Tenancy.Redis.csproj` (new): net10.0, nullable, references `Encina.Tenancy`, `StackExchange.Redis`, PublicAPI analyzers; `InternalsVisibleTo` for `Encina.UnitTests` only (the internal keys and mapper are guard-unreachable, so their guard target is 0 with that reason); `Encina.slnx` (edit).
2. `TenantStoreRedisOptions.cs` (new): `KeyPrefix` (default `encina`), `ChangeChannelName` derived from it; validated (non-empty, no `{`, `}` or whitespace).
3. `Internal/TenantStoreRedisKeys.cs` and `Internal/TenantHashMapper.cs` (new): key builders of Design Choice 5; hash fields `name`, `strategy`, `connectionString` (already protected), `schemaName`, `createdAtUtc`, `updatedAtUtc`; an unknown `strategy` value throws `InvalidOperationException` (fail closed).
4. Tests: `TenantStoreRedisKeysTests` (colliding-id test for `index`, `idx`, ids with `:` and braces), `TenantHashMapperTests` (round trip, corrupt field), `TenantStoreRedisOptionsGuardTests`; `PublicAPI.Shipped.txt` (empty) and `PublicAPI.Unshipped.txt`; `.github/coverage-manifest/Encina.Tenancy.Redis.json`; ProjectReference edits in `tests/Encina.UnitTests`, `tests/Encina.GuardTests` and `tests/Encina.IntegrationTests` csproj files (3).

Files: 14.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
- #596 delivers ITenantStoreWriter, ITenantSecretProtector, TenantIdRules and the behavior suite.
- The key layout of Design Choice 5 is injective and cluster-safe.

TASK:
Create the Encina.Tenancy.Redis package skeleton with options, key builders and the hash mapper, and their tests.

KEY RULES:
- Keys are built only through TenantStoreRedisKeys; the tenant id is always the whole suffix after "t:".
- Dates as ISO 8601 "O"; an unknown strategy or malformed date throws; nothing is returned half-parsed.
- XML docs on public members; PublicAPI files; coverage manifest with numeric targets and one-sentence justifications.

REFERENCE FILES:
- src/Encina.Caching.Redis/Encina.Caching.Redis.csproj
- src/Encina.Caching.Redis/ServiceCollectionExtensions.cs
- src/Encina.Tenancy/Abstractions/TenantInfo.cs
```

</details>

---

### Phase 2: The store and its registration (pull request 2)

<details>
<summary>Tasks</summary>

1. `TenantStoreRedis.cs` (new): `sealed class TenantStoreRedis : ITenantStore, ITenantStoreWriter`; constructor `(IConnectionMultiplexer multiplexer, IOptions<TenantStoreRedisOptions> options, ITenantSecretProtector? protector = null, TimeProvider? timeProvider = null)`; transactions with conditions per Design Choice 5; ids validated with `TenantIdRules` before any command; `ExistsAsync` is `KeyExistsAsync` on the record; `GetAllTenantsAsync` pipelines the reads in one `IBatch`.
2. `ServiceCollectionExtensions.cs` (new): `AddEncinaTenantStoreRedis(this IServiceCollection, Action<TenantStoreRedisOptions>? configure = null)`; registers the options, the store as a singleton and, through `TenantStoreRegistration.Replace` of #596, makes it the `ITenantStore` (and `ITenantStoreWriter`) wrapped by `InstrumentedTenantStore` with provider tag `redis`; it requires an `IConnectionMultiplexer` in DI (`AddEncinaRedisCache` or the application's own) and fails at `ValidateOnBuild` when it is missing.
3. Tests: `TenantStoreRedisTests` (substituted `IDatabase`, `ITransaction`, `IBatch`: every `Left` path, orphan index entry, corrupt hash), `TenantStoreRedisRegistrationTests` (provider built with `ValidateOnBuild` and `ValidateScopes`, both registration orders, application registration kept), `TenantStoreRedisGuardTests`; `PublicAPI.Unshipped.txt`, manifest.

Files: 7.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
- Phase 1 shipped the package, keys and mapper.
- A persistent store must win over the in-memory default in any registration order without replacing the application's registration (TenantStoreRegistration.Replace, #596).

TASK:
Implement TenantStoreRedis and AddEncinaTenantStoreRedis with their tests.

KEY RULES:
- Create uses KeyNotExists, update and delete use KeyExists; a failed condition is a Left (AlreadyExists or NotFound, ADR-062), never an overwrite.
- WaitAsync(cancellationToken) on every task; TimeProvider for timestamps; connection string protected before the write.
- Never inject a scoped service into the singleton; no EncinaError.Message in logs or tags.

REFERENCE FILES:
- src/Encina.Caching.Redis/RedisCacheProvider.cs
- src/Encina.Tenancy/Stores/InMemoryTenantStore.cs
- src/Encina.Tenancy/ServiceCollectionExtensions.cs
```

</details>

---

### Phase 3: Change notifications, health check and EventIds (pull request 3)

<details>
<summary>Tasks</summary>

1. `TenantChange.cs`, `TenantChangeKind.cs` (new, public) and `RedisTenantChangeSubscriber.cs` (new): `SubscribeAsync(Func<TenantChange, CancellationToken, ValueTask> handler, CancellationToken)`; a handler exception is caught per message, logged by type, and never ends the subscription (a background loop never lets an exception escape).
2. `TenantStoreRedis.cs` (edit): publishes after the transaction commits; a publish failure is logged and does not undo the committed write (the write is the truth, the notification an optimization).
3. `Health/TenantStoreRedisHealthCheck.cs` (new): `PingAsync` plus `CONFIG GET maxmemory-policy` as data when permitted; `DefaultName`, static `Tags`.
4. `Diagnostics/TenancyRedisLog.cs` (new, `[LoggerMessage]`) with the ids of the Event ID Allocation table; `src/Encina/Diagnostics/EventIdRanges.cs` (edit: `TenancyRedis = (12400, 12449)`), `src/Encina/PublicAPI.Unshipped.txt` (edit), `docs/engineering/eventid-block-registry.md` (edit: registered-ranges row and block row `registered`), `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` (edit: `AssemblyRanges`).
5. Tests: subscriber, health check and publish-failure unit tests; guard tests; package `PublicAPI.Unshipped.txt`; manifest.

Files: 15 (4 new source files, 1 store edit, 5 registry/API edits, 3 test files, package PublicAPI, manifest).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
- The store commits writes atomically; notifications are at-most-once and never the source of truth.

TASK:
Add the publisher, the subscriber, the health check, the log class and register the EventId range first.

KEY RULES:
- Register TenancyRedis (12400, 12449) in EventIdRanges.cs before using any id; pack ids without gaps; map the assembly in AssemblyRanges.
- A subscriber loop and its handlers never throw out of the loop; log by exception type and code only.
- No tenant id and no EncinaError.Message in tags or logs.

REFERENCE FILES:
- src/Encina.Caching.Redis/RedisPubSubProvider.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- docs/engineering/eventid-block-registry.md
- src/Encina.Caching.Redis/Health/RedisHealthCheck.cs
```

</details>

---

### Phase 4: Integration tests and the Valkey fixture (pull request 4)

<details>
<summary>Tasks</summary>

1. `tests/Encina.TestInfrastructure/Fixtures/ValkeyFixture.cs` (new) and its collection (new): vendor LTS image, started once per collection.
2. `tests/Encina.IntegrationTests/Tenancy/Redis/TenantStoreRedisIntegrationTests.cs` (new): derives from `TenantStoreBehaviorSuite` (#596), once per fixture (`[Collection("Redis")]` and the Valkey collection), `ClearAllDataAsync`-style flush in `InitializeAsync`; plus notification and orphan-entry tests, `[Trait("Category", "Integration")]`.
3. `changelog.d/597-tenant-store-redis.added.md`.

Files: 5.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
- The store, subscriber and health check exist and pass their unit tests.

TASK:
Add the Valkey fixture and the integration tests that run the shared behavior suite on Redis and Valkey.

KEY RULES:
- Shared collection fixtures only; never a per-class container; never dispose the fixture from a test.
- Flush the database in InitializeAsync; deterministic, no sleeps (await the subscription ready signal).

REFERENCE FILES:
- tests/Encina.TestInfrastructure/Fixtures/RedisFixture.cs
- tests/Encina.IntegrationTests/Infrastructure/Caching/RedisPubSubProviderIntegrationTests.cs
```

</details>

---

### Phase 5: Documentation and finalization (pull request 5)

<details>
<summary>Tasks</summary>

1. By `docs-writer`: `src/Encina.Tenancy.Redis/README.md` (new), the Redis section of the tenancy feature page, `docs/INVENTORY.md` (edit); the page states the persistence and eviction requirements, the at-most-once notification and the tested servers.
2. After #711 merges, a one-file bridge `TenantCacheInvalidationBridge` that calls `CachedTenantStore.InvalidateAsync` from the subscriber is an optional follow-up inside this package; it needs no separate plan because it holds no decision beyond the interface of #711.
3. Build and test verification against the Research targets.

Files: 3.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
- All code of #597 is merged.

TASK:
Write the package README and the tenancy page section.

KEY RULES:
- Coverage cited with covref markers, never typed; English only; the reference application is never named.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- src/Encina.Caching.Redis/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| Redis Cluster specification | Hash tags and the single-slot rule for `MULTI` |
| SPEC-002 REQ-061 | Multi-tenancy in 1.0; reserved `default` tenant (`TenantIdRules`, #596) |
| ADR-062 | Categories: `tenancy.tenant_exists` AlreadyExists, `tenancy.tenant_not_found` NotFound, `tenancy.tenant_id_invalid`/`tenant_id_reserved` InvalidRequest, `tenancy.protector_required` FailedPrecondition (all defined by #596; this plan adds no error code) |
| ADR-054 D4 | Tenant tag only through #2676; no record id as a tag |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| `ITenantStoreWriter`, `ITenantSecretProtector`, `TenantIdRules`, `TenantStoreRegistration`, `InstrumentedTenantStore`, `TenantStoreBehaviorSuite` | `Encina.Tenancy` and `tests/Encina.TestInfrastructure` (#596) | The contract, registration and tests |
| `IConnectionMultiplexer` registration | `src/Encina.Caching.Redis/ServiceCollectionExtensions.cs` | The connection the store uses |
| `RedisPubSubProvider` | `src/Encina.Caching.Redis/RedisPubSubProvider.cs` | Pattern for channel publish and subscribe |
| `RedisFixture` and collection | `tests/Encina.TestInfrastructure/Fixtures/RedisFixture.cs` | Integration tests on Redis |
| `CachedTenantStore` | #711 (plan) | Consumer of the notifications |

Per-file coverage targets (numeric, per flag):

| File | unit | guard | integration | Justification |
|------|-----:|------:|------------:|---------------|
| `TenantStoreRedis.cs` | 90 | 10 | 85 | Substituted `IDatabase` and `ITransaction` reach every `Left`; the real servers cover transaction conditions and cluster-safe keys |
| `Internal/TenantStoreRedisKeys.cs`, `Internal/TenantHashMapper.cs` | 100 | 0 | 0 | Internal pure functions; guard tests cannot reach internals (`InternalsVisibleTo` excludes `Encina.GuardTests`); fully covered by unit tests |
| `RedisTenantChangeSubscriber.cs` | 90 | 10 | 80 | Handler and payload paths by unit tests; a real subscription by integration |
| `Health/TenantStoreRedisHealthCheck.cs` | 100 | 10 | 70 | Two outcomes plus the `CONFIG` permission branch; the real server covers the success path |
| `TenantStoreRedisOptions.cs`, `ServiceCollectionExtensions.cs` | 100 | 40 | 0 | Validation and DI, proven with `ValidateOnBuild` |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Tenancy.Redis` | `TenancyRedis = (12400, 12449)` | New block in the Core and messaging zone: 12200-12399 are claimed by #340, the process manager plans, #3978 and #3981, so 12400 is the first free slot (checked on `origin/main` and the open `docs/plan-*` heads). Ids: 12400 notification publish failed, 12401 subscription started, 12402 subscription failed, 12403 handler failed (exception type), 12404 orphan index entry skipped, 12405 malformed payload ignored; 12406-12449 free. Registry row and `EventIdRanges.cs` field in pull request 3 |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Package skeleton, keys, mapper (PR 1) | 14 | Includes `Encina.slnx`, manifest and 3 test-project references |
| Store and registration (PR 2) | 7 | |
| Notifications, health, EventIds (PR 3) | 15 | Registry, `EventIdRanges.cs`, `AssemblyRanges`, PublicAPI x2 counted |
| Integration tests and fixture (PR 4) | 5 | Includes the changelog fragment |
| Documentation (PR 5) | 3 | `docs-writer` |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is pre-1.0, .NET 10, C# 14; ROP with Either<EncinaError, T>.
- #596 owns the tenant store contract; this plan adds the Redis store in a new package, Encina.Tenancy.Redis.

IMPLEMENTATION OVERVIEW:
1. Phase 1: package skeleton, options, injective cluster-safe keys, hash mapper.
2. Phase 2: TenantStoreRedis (conditional transactions) and AddEncinaTenantStoreRedis through TenantStoreRegistration.Replace.
3. Phase 3: change notifications, subscriber, health check, TenancyRedis EventIds (12400-12449).
4. Phase 4: Valkey fixture and integration tests running the shared behavior suite.
5. Phase 5: README and documentation.

KEY PATTERNS:
- Keys {prefix}:{tenants}:t:{id} and {prefix}:{tenants}:idx; the hash tag keeps MULTI valid on Cluster.
- Create uses KeyNotExists, update and delete KeyExists; never overwrite on create.
- WaitAsync(cancellationToken), TimeProvider, connection string protected before the write.
- A subscriber loop never throws; no tenant id and no EncinaError.Message in logs or tags.

REFERENCE FILES:
- src/Encina.Caching.Redis/ServiceCollectionExtensions.cs
- src/Encina.Caching.Redis/RedisPubSubProvider.cs
- src/Encina.Tenancy/Abstractions/ITenantStore.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ⏭️ | Deferred to #711 (open): `CachedTenantStore` consumes the notifications of this plan; Redis is the store here, not a cache of it |
| 2 | OpenTelemetry | ✅ | Spans and counters from `InstrumentedTenantStore` (provider tag `redis`); the record's tenant id is never a tag |
| 3 | Structured Logging | ✅ | `TenancyRedisLog` 12400-12405, exception type and code only |
| 4 | Health Checks | ✅ | `TenantStoreRedisHealthCheck`: ping, and the eviction policy as data when `CONFIG GET` is allowed |
| 5 | Validation | ✅ | `TenantIdRules` on every write; options validated at startup |
| 6 | Resilience | ❌ | A failure throws and the request fails closed; reconnect policy is the client's `ConfigurationOptions`; a Polly retry here would hide an outage from the health check |
| 7 | Distributed Locks | ❌ | Conditional transactions give atomic create, update and delete |
| 8 | Transactions | ✅ | `MULTI` with conditions keeps the record and the index consistent |
| 9 | Idempotency | ✅ | A second create is `Left(tenancy.tenant_exists)`; no silent overwrite |
| 10 | Multi-Tenancy | ✅ | This is the tenant catalog: global by design; `default` reserved; keys injective |
| 11 | Module Isolation | ❌ | The catalog is cross-module by design |
| 12 | Audit Trail | ⏭️ | Deferred to #752 (open), which audits lifecycle changes through the write port |

---

## Prerequisites & Dependencies

- **Hard prerequisite**: the contract pull requests of #596 (PR 1 and PR 2 of its plan): `ITenantStoreWriter`, `ITenantSecretProtector`, `TenantIdRules`, `TenantStoreRegistration`, `InstrumentedTenantStore`, `TenantStoreBehaviorSuite`. Phase 1 here can start in parallel (it uses none of them), Phase 2 cannot.
- #711 consumes the notifications later; nothing here depends on it.
- The Valkey fixture is new test infrastructure; no production package depends on it.

---

## Next Steps

1. The maintainer answers Dilemmas D1-D4 when work on the issue starts.
2. After #596's contract merges, cut PR 2 and run the unit, guard and integration flags; report per-file measurements against the targets above.
3. Register `TenancyRedis` in `EventIdRanges.cs` and the registry in the same pull request that first uses an id (PR 3).
