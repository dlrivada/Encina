# Implementation Plan: `EntityChangeAuditStoreRedisStreams` — Redis Streams for the entity-change audit store

> **Issue**: [#581](https://github.com/dlrivada/Encina/issues/581)
> **Type**: Feature
> **Complexity**: Medium (6 phases, a decorator store over a queryable store, a stream reader with consumer groups, ~10 source files + tests)
> **Estimated Scope**: ~650-950 lines of production code + ~1,200-1,700 lines of tests

---

## Summary

Publish every entity-change audit entry (today `IAuditLogStore`, becoming `IEntityChangeAuditStore` under ADR-036) to a Redis Stream, so real-time dashboards and consumers read the recent trail with sub-millisecond latency. The issue itself rates Redis Streams "not recommended" for long-term compliance and recommends it as a hot buffer in front of a persistent store; the ADR-036 contract needs paged history, a 2555-day default retention and `PurgeEntriesAsync`, and a Redis server keeps data only as durably as its persistence settings allow. This plan therefore recommends a composite (Design Choice 1): `EntityChangeAuditStoreRedisStreams` writes to a queryable inner store, then appends to a stream, and delegates history and purge to the inner store. The samples of the issue (`Task` returns, a per-entity stream key that is not injective, count-based `MaxLength` trimming that silently drops audit entries, the user id as a plain field, `DateTime` timestamps) are replaced by the ADR-036 shapes.

What the code looks like today (origin/main `1dee4ba61`, re-checked for this revision; the plan branch was cut from `d01de2d77`):

- `IAuditLogStore` lives in `src/Encina.DomainModeling/Auditing/IAuditLogStore.cs`; `AuditLogEntry` and `InMemoryAuditLogStore` sit next to it. ADR-036 renames and reshapes all of it; that work is **#1778, open**.
- Redis packages: `Encina.Caching.Redis` (range `CachingRedis` 3600-3649), `Encina.DistributedLock.Redis` (3750-3799) and `Encina.Redis.PubSub` (4400-4449); none uses streams. `Encina.DistributedLock.Redis` shows the registration shape: overloads that take an `IConnectionMultiplexer` or a factory, registered with `TryAddSingleton`. #240 (open) plans an `Encina.Redis.Streams` transport; #595 (open) plans `Encina.Messaging.Redis`.
- `StackExchange.Redis` 2.12.14 (MIT) and `Testcontainers.Redis` 4.15.0 are pinned; `tests/Encina.TestInfrastructure/Fixtures/RedisFixture.cs` (image `redis:7-alpine`, collection `Redis`) exists. StackExchange.Redis commands take no `CancellationToken`.

**Provider category**: Redis belongs to the caching category (`AGENTS.md` section 5), but this is not a cache feature (no `ICacheProvider` change), so the eight-provider caching rule does not apply; it is an audit sink outside the 10-provider database matrix. Valkey, Dragonfly, Garnet and KeyDB speak the same protocol; which of them support streams is listed in the README after the Phase 0 check. **Affected packages**: a new audit Redis package (Design Choice 2), `Encina.Security.Audit` (the contract, from #1778), `Encina` (EventId registry), test projects.

---

## Design Choices

<details>
<summary><strong>1. Role of Redis Streams in the store (Dilemma D1)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Composite decorator: wraps the registered queryable `IEntityChangeAuditStore`; write = inner write, then `XADD`; history delegates to the inner store; `PurgeEntriesAsync` for a tenant purges the inner store and then trims that tenant's stream copies with `XTRIM MINID` at the same cutoff** | The full contract and the 2555-day retention hold in a durable store; Redis adds the hot stream only; matches the issue's own architecture recommendation | Two systems per write |
| **B) Full store on Redis: stream per entity, history with `XREVRANGE`, purge with `XTRIM MINID` over streams found through a per-tenant index** | Redis is the only system | Audit durability depends on AOF settings; years of entries held in memory; the issue itself does not recommend it for compliance retention |

### Chosen Option: **A — composite decorator over a queryable store** (recommended, pending the maintainer)

### Rationale

- Ordering (rule): inner write first; if `XADD` fails, the call returns `Left` (`Unavailable`), the fail-closed writer fails the unit of work, and its retry repeats both steps (the inner write is idempotent; the stream may hold the entry twice and consumers deduplicate by the `entryId` field). B wins for deployments with short legal retention and Redis configured with `appendfsync always`.
- Retention under both options: the ADR-036 `EntityChangeAuditRetentionService` runs in a tenant-less scope, enumerates `ITenantStore.GetAllTenantsAsync` and calls the purge once per tenant with that tenant's cutoff (#4182); the store never reads the ambient tenant for a purge and has no cross-tenant purge opt-in. The stream trim of the purge removes the copies of that tenant older than its cutoff from the streams Design Choice 3 assigns to it (with a shared stream, the trim removes older copies of every tenant, which only shortens how long a copy stays; the record of truth is the inner store).

</details>

<details>
<summary><strong>2. Package placement (Dilemma D2)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New package `Encina.Security.Audit.Redis`** | Cache users do not take the audit package (pay-for-what-you-use); named like the audit satellites | One more package |
| **B) Inside `Encina.Caching.Redis`** (the issue) | No new package; a multiplexer already registered there | Couples audit to the cache package and its dependencies |
| **C) Inside the planned `Encina.Redis.Streams` transport (#240)** | One streams package | Waits for #240; couples audit to a transport, as option B of the #580 plan would for Kafka |

### Chosen Option: **A — `Encina.Security.Audit.Redis`** (recommended, pending the maintainer)

### Rationale

- The same reasoning as Design Choice 2 of the #580 plan: a transport or cache package should not depend on `Encina.Security.Audit`. The package registers `IConnectionMultiplexer` with `TryAddSingleton`, so an application that already registered it for caching shares one connection. B wins when the maintainer wants no new package; C wins if #240 lands first and the maintainer wants one streams package.

</details>

<details>
<summary><strong>3. Stream key layout and tenant isolation (Dilemma D3)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One stream per tenant: `{prefix}{base64url(tenant)}`; fields `entryId`, `entityType`, `entityId`, `action`, `changedAtUtc`, `moduleId`, `payload` (JSON of the entry), `payloadHash`, `schema`** | Consumers read one tenant's stream; Redis ACL key patterns can restrict a consumer to its tenant; few keys | Per-entity reading needs a filter |
| **B) One stream per entity** (the issue) | Per-entity reading is a range | One key per audited entity (millions of keys), no ACL pattern per tenant, trimming must visit every key |
| **C) One global stream** | Simplest | No isolation at all |

### Chosen Option: **A — one stream per tenant** (recommended, pending the maintainer)

### Rationale

- The key is injective because the tenant segment is base64url-encoded (the prefix is validated not to end inside an encoded segment). Tenant rules: reserved segment `default` only when tenancy is off; a real tenant `default` rejected (`InvalidRequest`, ADR-062 R3); tenancy on and no tenant resolved denies before the inner write (`InvalidRequest`). The reader is bound to the ambient tenant's stream; reading another tenant needs an explicit, logged opt-in (`Forbidden` otherwise, R6). B wins when consumers mostly follow single entities; C wins for single-tenant applications only.

</details>

<details>
<summary><strong>4. Stream trimming (Dilemma D4)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Time-based trimming on every append: `XADD ... MINID ~ {now - StreamRetention}` (Redis 6.2+), `now` from the store's `TimeProvider`; `StreamRetention` defaults to 7 days** | The stream holds a short, bounded time window in memory | A consumer offline longer than the window misses entries (it reads them from the inner store) |
| **B) Count-based trimming `MAXLEN ~ n`** (the issue) | Predictable memory | The window shrinks under load, so a consumer may miss entries written seconds ago |
| **C) No append-time trimming** | Nothing leaves the stream before the retention purge | Memory grows with every entry until the per-tenant purge trims it |

### Chosen Option: **A — time-based trimming on append** (recommended, pending the maintainer)

### Rationale

- This choice sizes the hot window only. Under every option the retention bound comes from the per-tenant purge of Design Choice 1, which trims each tenant's copies at that tenant's cutoff, so no option keeps a copy past the retention (ADR-036 decision 1, GDPR Art. 5(1)(e)). B wins when memory is the hard limit and consumers keep up; it can be offered as an additional cap (`MaxLength`, default unset). C wins when consumers must be able to replay the whole retention period from Redis.

</details>

<details>
<summary><strong>5. Consumer groups (the issue's acceptance criterion) (Dilemma D5)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IEntityChangeAuditStream` with `ReadFromAsync(DateTimeOffset fromUtc, CancellationToken)` (replay then follow with `XREAD BLOCK`) and thin group methods: `CreateConsumerGroupAsync`, `ReadGroupAsync`, `AcknowledgeAsync`, all tenant-bound and returning `Either`** | Covers the criterion; groups are Redis's own mechanism, Encina only wraps them | Pending-entry handling (`XPENDING`, `XCLAIM`) is the consumer's design |
| **B) Replay only, no groups** | Smaller | Drops an acceptance criterion |
| **C) A hosted consumer with automatic claiming of stale pending entries** | Turnkey | A background loop, lock and failure handling inside the library |

### Chosen Option: **A — replay plus thin consumer-group methods** (recommended, pending the maintainer)

### Rationale

- A meets the criterion without background machinery. B wins if the maintainer wants the composite first; C wins when many applications need the same claiming logic.

</details>

<details>
<summary><strong>6. Persistence check of the Redis server (Dilemma D6)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Start-up check through `INFO persistence`: when `aof_enabled:0`, log a warning by event id and continue** | Works on managed services that block `CONFIG`; the inner store is the record, so a lost stream copy loses no audit data | A misconfigured server is only a warning |
| **B) Fail start-up when AOF is off, with an explicit logged opt-out** | Stream consumers never silently depend on a volatile server | Breaks managed services where `INFO persistence` reports AOF off but replication protects data |

### Chosen Option: **A — warn on start-up** (recommended, pending the maintainer)

### Rationale

- Under Design Choice 1 option A the stream is a copy, so durability of Redis affects consumers, not the audit record; with option B of Design Choice 1 this choice should switch to B.

</details>

<details>
<summary><strong>7. Tamper evidence (Dilemma D7)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The store only appends (`XADD`; `XDEL` is never used), a `payloadHash` field travels with each entry, and a documented Redis ACL (writer: `+xadd`; readers: `+xread +xreadgroup +xack +xrange`; no `+xdel +xtrim +del` except the store's own trimming user); the record of truth is the inner store; a hash chain is left to #1240** | No new mechanism | A user with `XDEL` or `DEL` can still remove stream entries |
| **B) A per-stream hash chain now** | Gaps detectable by consumers | Serialises appends per tenant; duplicates #1240 |

### Chosen Option: **A — append-only use, payload hash, ACLs; chain through #1240** (recommended, pending the maintainer)

### Rationale

- Redis has no write-once mode. #1240 (open) owns the cross-provider chain. B wins if consumers must prove completeness before #1240 lands.

</details>

<details>
<summary><strong>8. Contract, naming, cancellation and failure semantics</strong></summary>

**Class:** Settled
**Settled by:** ADR-036 (decisions 1 and 7), ADR-001, ADR-006, ADR-062 (R3-R6), AGENTS.md section 3 (fail closed; errors never swallowed; TimeProvider), AGENTS.md section 4

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `EntityChangeAuditStoreRedisStreams : IEntityChangeAuditStore` on the ADR-036 contract; `Either` results; an awaited `StreamAddAsync` whose `RedisConnectionException`, `RedisTimeoutException` or `RedisServerException` is a `Left`; the token is checked before each command (StackExchange.Redis commands cannot be cancelled once sent) and `OperationCanceledException` propagates** | Fail-closed auditing; honest cancellation | Depends on #1778 |
| **B) `CommandFlags.FireAndForget` appends** | Lowest latency | Swallows a failed audit write, forbidden by section 3 |

### Chosen Option: **A — awaited append on the ADR-036 contract**

### Rationale

- B reports success for an entry that may never reach the stream. Only the exception type and the Redis error prefix (for example `WRONGTYPE`) reach errors and logs, never the server message; every `EncinaErrors.Create` passes a named `category:` and a constant message. A connection string set in options is `[JsonIgnore]` with a `ToString()` that prints no password.

</details>

<details>
<summary><strong>9. Erasure requests and the stream copies</strong></summary>

**Class:** Settled
**Settled by:** maintainer decision of 2026-10-26 on #2847 (path C is the default for audit data: GDPR Art. 17(3)(b) and 17(3)(e)), ADR-036 decision 1, #4182 (per-tenant retention purge)

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No per-subject delete: the stored entries follow path C, and stream copies leave at the latest when the per-tenant retention purge trims them at the tenant's cutoff (under both options of Design Choice 1; append-time trimming of Design Choice 4 may remove them earlier, never later); minimisation by the writer's redaction hook** | Matches the audit erasure decision | Copies already read by consumers are outside Encina's reach |
| **B) `XDEL` per subject** | Possible on Redis | Contradicts path C as the default |

### Chosen Option: **A — path C, trimming bounds the copies**

### Rationale

- Path C records the request and keeps audit data until retention; the README states that consumers which persist the stream inherit the retention duty.

</details>

---

## Implementation Phases

### Phase 0: Server compatibility check (no pull request)

<details>
<summary>Tasks</summary>

1. With locally started containers (Redis 7 at the image the fixture pins, and the current LTS images of Valkey, Dragonfly, Garnet and KeyDB), run with a throwaway script that is never committed: `XADD` with `MINID ~`, `XREVRANGE`, `XREAD BLOCK`, `XGROUP CREATE MKSTREAM`, `XREADGROUP`, `XACK`, `INFO persistence`.
2. The evidence is a pass/fail table per server in the worker's report; the orchestrator posts it on #581, and the README lists the servers that pass. No server outside Redis is promised before this table exists.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 0</strong></summary>

```
CONTEXT:
- Issue #581: Redis Streams backend of the entity-change audit store. No stream code exists on main.

TASK:
Run the commands of the Phase 0 Tasks against the five servers and report a pass/fail table.

KEY RULES:
- No production code, no pins and no pull request; the script is never committed.

REFERENCE FILES:
- tests/Encina.TestInfrastructure/Fixtures/RedisFixture.cs
```

</details>

---

### Phase 1: Package, options and entry mapping

<details>
<summary>Tasks</summary>

1. `src/Encina.Security.Audit.Redis/Encina.Security.Audit.Redis.csproj` (name per Design Choice 2; `StackExchange.Redis`, `Encina.Security.Audit`; PublicAPI analyzers; XML docs; `InternalsVisibleTo` for `Encina.UnitTests`, `Encina.PropertyTests`, `Encina.IntegrationTests`, `Encina.ContractTests`, not `Encina.GuardTests`, so the guard target of 0 for internal types stays valid); `Encina.slnx` row; PublicAPI x2.
2. `EntityChangeAuditRedisStreamsOptions` (public, sealed): `KeyPrefix` (default `encina:audit:entity-change:`), `StreamRetention` (the append-time hot window of Design Choice 4, 7 days; the retention bound itself comes from the per-tenant purge), `MaxLength` (unset), `Database`, `ConnectionString` (`[JsonIgnore]`), `AllowCrossTenantReads` (`false`), `ToString()` safe.
3. `EntityChangeAuditStreamMapper` (internal): entry to and from `NameValueEntry[]`, key builder (Design Choice 3), payload hash.
4. Unit tests (key injectivity, field set, round trip), options guard tests, manifest `.github/coverage-manifest/Encina.Security.Audit.Redis.json`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
- Encina is a .NET 10 / C# 14 library (ROP). The ADR-036 contract is delivered by #1778.

TASK:
Create the package, options and mapper of the Phase 1 Tasks with tests and manifest.

KEY RULES:
- One stream per tenant, base64url tenant segment, injective key.
- StreamRetention validated to be positive; it sizes the hot window, while the per-tenant purge enforces the retention.
- Secrets [JsonIgnore]; ToString prints none; XML docs; PublicAPI complete.

REFERENCE FILES:
- src/Encina.DistributedLock.Redis/ServiceCollectionExtensions.cs
- docs/architecture/adr/036-three-audit-stores.md
```

</details>

---

### Phase 2: The composite store and its registration

<details>
<summary>Tasks</summary>

1. `EntityChangeAuditStoreRedisStreams` (public, sealed) `: IEntityChangeAuditStore`; constructor `(IEntityChangeAuditStore inner, IConnectionMultiplexer redis, ITenantContext? tenantContext, IOptions<EntityChangeAuditRedisStreamsOptions> options, TimeProvider timeProvider, ILogger<EntityChangeAuditStoreRedisStreams> logger)`; write per Design Choice 1 with the trimming of Design Choice 4; history delegates; `PurgeEntriesAsync` with the tenant and cutoff it receives (signature from #4182 and #1778) purges the inner store with them, then `XTRIM MINID` at that cutoff on the tenant's streams (ids derived from the cutoff's milliseconds); a failure of either step is a `Left` and no success is reported; never reads the ambient tenant; the composite must be constructible in the tenant-less scope of the retention service.
2. `EntityChangeAuditRedisStreamsErrors`; `EntityChangeAuditRedisPersistenceCheck` (internal `IHostedService`, Design Choice 6).
3. `ServiceCollectionExtensions.AddEncinaEntityChangeAuditRedisStreams` with three overloads like `AddEncinaDistributedLockRedis` (options with connection string; an `IConnectionMultiplexer`; a factory): `TryAddSingleton<IConnectionMultiplexer>`, options with `ValidateOnStart`, `TryAddSingleton(TimeProvider.System)`, the decorator over the registered inner store (inside the ADR-036 `InstrumentedEntityChangeAuditStore`; the mechanism follows #1778's registration; the decorator keeps the inner store's lifetime), the check through `TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, EntityChangeAuditRedisPersistenceCheck>())`. Fails at build with a clear message when no inner store is registered.
4. EventId range and log class in this PR. DI test with `ValidateOnBuild` and `ValidateScopes` (singleton and scoped inner stores). CRAP <= 10 per method.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
- Phase 1 created the package, options and mapper.

TASK:
Implement the composite store, the persistence check, the errors, the registrations, the EventId range and the log class, with unit, guard and DI tests.

KEY RULES:
- Inner write first, then awaited XADD with MINID trimming from TimeProvider; failures are Left values.
- Check the CancellationToken before each command; OperationCanceledException propagates.
- The decorator keeps the inner store's lifetime; hosted services through TryAddEnumerable.

REFERENCE FILES:
- src/Encina.DistributedLock.Redis/ServiceCollectionExtensions.cs
- docs/architecture/adr/062-error-category-contract.md
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

### Phase 3: Stream reader, consumer groups and health check

<details>
<summary>Tasks</summary>

1. `IEntityChangeAuditStream` (public) and `EntityChangeAuditStreamRedis` (Design Choice 5): `ReadFromAsync` (stream ids derived from `fromUtc` milliseconds), `CreateConsumerGroupAsync`, `ReadGroupAsync`, `AcknowledgeAsync`; tenant-bound; a corrupt entry yields one `Left` (`Internal`) and the read continues; registered by `AddEncinaEntityChangeAuditRedisStreams`.
2. `EntityChangeAuditRedisStreamsHealthCheck : IEncinaHealthCheck` (`PING` and `XINFO STREAM` tolerant of a missing stream); const `DefaultName`, static `Tags`; registered by the same method.
3. Unit and guard tests; manifest edit; `PublicAPI.Unshipped.txt`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
- Phase 2 shipped the composite store and its registration.

TASK:
Add IEntityChangeAuditStream with the Redis reader and group methods, and the health check, all registered.

KEY RULES:
- Tenant-bound reads; cross-tenant only with the explicit, logged opt-in.
- Results are Either; no server message in a Left.

REFERENCE FILES:
- src/Encina.Caching.Redis/Health/RedisHealthCheck.cs
- tests/Encina.TestInfrastructure/Fixtures/RedisFixture.cs
```

</details>

---

### Phase 4: Cross-cutting integration and observability

<details>
<summary>Tasks</summary>

1. EventId range `RedisStreamsEntityChangeAudit = (52550, 52599)` in `src/Encina/Diagnostics/EventIdRanges.cs`, registered in PR 2 with its line in `src/Encina/PublicAPI.Unshipped.txt` (one line for the new field), the `["Encina.Security.Audit.Redis"]` entry in `AssemblyRanges` of `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`, and in `docs/engineering/eventid-block-registry.md` a "Registered ranges" row `RedisStreamsEntityChangeAudit | 52550-52599` (Zone `zone`) and a "Blocks" row `52550-52599` with status `registered`.
2. `Diagnostics/RedisStreamsEntityChangeAuditLog.cs`, ids packed from 52550: 52550 append failed (exception type), 52551 server persistence disabled, 52552 consumer group created, 52553 corrupt entry skipped, 52554 cross-tenant read opt-in used, 52555 read failed (exception type), 52556 tenant stream trim failed (exception type). 52557-52599 stay free.
3. The ADR-036 `InstrumentedEntityChangeAuditStore` wraps the composite (DI test); ADR-054 D4 tenant attribute and tag only when multi-tenancy is on; logs never carry tenant, user or entity ids, values or messages.
4. **ErrorCategory (ADR-062)**: `entity_change_audit.redis_streams.append_failed` (connection, timeout) `Unavailable`; `.server_error` `Internal` (R4); `.tenant_required` and `.reserved_tenant` `InvalidRequest` (R3); `.cross_tenant_denied` `Forbidden` (R6); `.corrupt_entry`, `.read_failed` and `.trim_failed` `Internal`; `.invalid_read` `Validation`. Errors of the inner store pass through. Cancellation propagates.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
- AGENTS.md section 7: new ranges only in the 10000+ zone, registered before use.

TASK:
Register RedisStreamsEntityChangeAudit 52550-52599 (in PR 2), write the log class, wire the categories and the decorator test.

KEY RULES:
- Ids packed from 52550; nothing outside the range.
- Never log EncinaError.Message, server messages, values or ids.

REFERENCE FILES:
- docs/engineering/eventid-block-registry.md
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 5: Testing

> **Goal**: every applicable flag reaches its target in `.github/coverage-manifest/Encina.Security.Audit.Redis.json`, each target and its justification set in the pull request whose tests reach it.

<details>
<summary>Tasks</summary>

1. **Unit**: mapper, composite ordering (inner `Left` stops before `XADD`; append failure after inner success is `Left`), trimming id from `FakeTimeProvider`, tenant rules, purge of the tenant passed in (inner purge then stream trim at the same cutoff; never the ambient tenant; a trim failure is `Left`), persistence check, reader filter and corrupt entries; `IConnectionMultiplexer` and `IDatabase` substituted (interfaces; `StreamEntry` and `NameValueEntry` are public structs), a fake inner store.
2. **Guard** (`tests/Encina.GuardTests/Security/Audit/Redis/`): constructors, methods, the three registration overloads, options.
3. **Contract** (PR 4): the shared entity-change contract base run against the composite over the in-memory inner store and the Redis container.
4. **Integration** (PR 4, `[Collection("Redis")]`, `[Trait("Category", "Integration")]`): entry appended with its fields, time-based trimming, replay from a time and follow, consumer group create, read and acknowledge, tenant isolation, per-tenant purge from a scope with no ambient tenant (tenant A's cutoff trims only tenant A's stream; tenant B keeps its copies), append failure with the container paused, persistence warning on the default image, health check. Keys use a per-class prefix; `ClearAllDataAsync` where the fixture offers it, otherwise the class deletes its keys in `InitializeAsync`. The `Redis` collection is defined next to `RedisFixture` in `Encina.TestInfrastructure`; the PR verifies that `Encina.ContractTests` can use it and adds a collection definition there if it cannot. `Testcontainers.Redis` 4.15.0 (MIT) is pinned; the fixture image `redis:7-alpine` runs only in tests.

**Manifest rule**: the manifest is new (no existing floor). PR 1 creates it with package-level `targets` for unit and guard; PR 4 adds the package-level contract, property and integration targets. Each PR adds the per-file `targets` and `justifications` below for the flags its own tests reach, so `coverage-report.cs --check-justifications` passes on every PR.
5. **Property** (PR 4): key injectivity and mapper round trip.
6. **Load/Benchmark**: `.md` justifications (the append is one command; not a hot path of Encina).

**Per-file targets** (`-` = not applicable; "PR" adds the target, its tests and its justification):

| File | Unit | Guard | Contract | Property | Integration | PR | Justification |
|------|:----:|:-----:|:--------:|:--------:|:-----------:|:--:|---------------|
| `EntityChangeAuditRedisStreamsOptions.cs` | 90 | 60 | - | - | 60 | 1, 4 | Properties, window and prefix validation and `ToString()`. |
| `EntityChangeAuditStreamMapper.cs` | 95 | 0 | - | 90 | 85 | 1, 4 | Pure mapping and key building; internal, so guard 0. |
| `EntityChangeAuditStoreRedisStreams.cs` | 95 | 85 | 85 | - | 85 | 2, 4 | Ordering and failure paths with substitutes; the contract and the container cover appends. |
| `EntityChangeAuditRedisPersistenceCheck.cs` | 90 | 0 | - | - | 70 | 2, 4 | Both `INFO` answers with a substitute; internal; the container proves the real reply. |
| `EntityChangeAuditRedisStreamsErrors.cs` | 100 | 0 | - | - | - | 2 | Factory methods called by the tests; internal. |
| `ServiceCollectionExtensions.cs` | 95 | 90 | - | - | - | 2 | DI wiring for the three overloads with ValidateOnBuild, singleton and scoped inner stores. |
| `EntityChangeAuditStreamRedis.cs` | 85 | 80 | - | - | 85 | 3, 4 | Filter and group paths with substitutes; the container proves blocking reads and groups. |
| `EntityChangeAuditRedisStreamsHealthCheck.cs` | 90 | 80 | - | - | 70 | 3, 4 | Two commands and a status mapping. |
| `Diagnostics/RedisStreamsEntityChangeAuditLog.cs` | 0 | 0 | - | - | - | 2 | Source-generated `[LoggerMessage]` partial methods with no hand-written executable code. |

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
- Tests execute real package code; Redis is shared through the Redis collection.

TASK:
Write the tests of Phase 5 and the manifest targets with justifications in the pull requests that reach them.

KEY RULES:
- Shouldly via Encina.Testing.Shouldly; no FluentAssertions, per-class containers or Thread.Sleep.
- Per-class key prefixes; FakeTimeProvider for trimming ids.
- Targets never go down; run coverage-report.cs --check-justifications.

REFERENCE FILES:
- tests/Encina.TestInfrastructure/Fixtures/RedisFixture.cs
- tests/Encina.IntegrationTests/Infrastructure/Caching/RedisPubSubProviderIntegrationTests.cs
- tests/Encina.ContractTests/Messaging/DeadLetter/DeadLetterStoreContract.cs
```

</details>

---

### Phase 6: Documentation and finalization

<details>
<summary>Tasks</summary>

1. XML docs; `src/Encina.Security.Audit.Redis/README.md` (composite model, the limits of Redis for long-term retention required by the issue, trimming window, consumer groups, ACLs, persistence warning, servers verified in Phase 0); a feature page under `docs/features/`; `docs/INVENTORY.md`; ROADMAP if affected (through `docs-writer`).
2. `changelog.d/581-redis-streams-entity-change-audit-store.added.md` (through `mechanical-fixer`).
3. ADR only if a Design Choice changes a rule of ADR-036; ADR number assigned when the ADR pull request opens.
4. Build 0 warnings; per-flag coverage and CRAP reported.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
- Documentation: README with the long-term retention limits, feature page, inventory, changelog fragment.

TASK:
Complete the documentation tasks of Phase 6.

KEY RULES:
- Never edit CHANGELOG.md; use changelog.d/.
- No hand-typed coverage figures; English only; never name the reference application.

REFERENCE FILES:
- docs/features/read-auditing.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Topic | Requirement |
|--------|-------|-------------|
| ADR-036 | Three audit stores | Entity-change contract, 2555-day retention, values never kept indefinitely, fail closed |
| ADR-062 | Error categories | Every code categorised (R3-R6) |
| ADR-054 D4 | Telemetry | Tenant attribute and tag only when multi-tenancy is on |
| SPEC-002 REQ-007 | Retention | Per-category retention, enforced by the inner store and by the per-tenant stream trim |
| #4182 (open, `[DEBT]`) | Per-tenant retention | Retention iterates `ITenantStore` with each tenant's policy and passes the tenant to the purge; never the ambient tenant or a cross-tenant opt-in |
| GDPR Art. 5(1)(e), 17(3)(b) | Storage limitation, erasure exception | Per-tenant trim at the tenant's cutoff; recorded erasure (decision on #2847) |
| Redis Streams documentation (vendor) | `XADD MINID`, `XREAD BLOCK`, consumer groups, persistence | Verified per server in Phase 0 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in the Redis Streams entity-change audit store |
|-----------|----------|------------------------------------------------------|
| `IAuditLogStore` (becomes `IEntityChangeAuditStore`) | `src/Encina.DomainModeling/Auditing/IAuditLogStore.cs` | The contract and the inner store |
| `AddEncinaDistributedLockRedis` overloads | `src/Encina.DistributedLock.Redis/ServiceCollectionExtensions.cs` | Registration shape for the multiplexer |
| `RedisHealthCheck` | `src/Encina.Caching.Redis/Health/RedisHealthCheck.cs` | Health check pattern |
| `RedisFixture`, `Redis` collection | `tests/Encina.TestInfrastructure/Fixtures/RedisFixture.cs` | Shared server |
| `InstrumentedEntityChangeAuditStore`, `EntityChangeAuditRetentionService` | delivered by #1778 | Tracing; per-tenant retention of the inner store and the streams (#4182) |
| `ITenantStore.GetAllTenantsAsync` | `src/Encina.Tenancy/Abstractions/ITenantStore.cs` | Tenant enumeration of the retention service (#4182) |
| `StackExchange.Redis`, `Testcontainers.Redis` | `Directory.Packages.props` | Already pinned; no new package versions |
| `EventIdRanges` | `src/Encina/Diagnostics/EventIdRanges.cs` | Registration of the new range |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Security.Audit.Redis` | 52550-52599 (`RedisStreamsEntityChangeAudit`) | Fifth of the five audit-stores-1 slots 52350-52599, the first free Security and compliance space after the #4007 reservations 50000-52349 (registry on origin/main `1dee4ba61`); ids 52550-52556 used, packed; registered by PR 2 with its registry rows |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production | ~10 | csproj, options, mapper, composite store, persistence check, errors, registration, stream interface and reader, health check, log class |
| Tests | ~11 | unit x4, guard x2, DI, contract, integration, property, load/benchmark `.md` x2 |
| Metadata | ~9 | `Encina.slnx`, PublicAPI x2, manifest, `EventIdRanges.cs`, `src/Encina/PublicAPI.Unshipped.txt`, registry page, allocation test, test-project references |
| **Total** | **~36 with edits** | Every PR starts after #1778, builds and passes alone, at most 15 files and 1500 lines. **PR 1** (11 files): csproj, `Encina.slnx`, PublicAPI x2, options, mapper, `Encina.UnitTests` and `Encina.GuardTests` project references, mapper unit tests, options guard tests, manifest (no service introduced). **PR 2** (15 files): composite store, persistence check, errors, `ServiceCollectionExtensions`, log class, `EventIdRanges.cs`, `src/Encina/PublicAPI.Unshipped.txt`, registry page, `EncinaEventIdAllocationTests.cs`, package `PublicAPI.Unshipped.txt`, store unit tests, persistence check unit tests, DI test, guard tests, manifest. **PR 3** (9 files): stream interface, reader, health check, registration edit, `PublicAPI.Unshipped.txt`, unit tests x2, guard tests, manifest. **PR 4** (<= 11 files): three test-project references, a `Redis` collection definition in `Encina.ContractTests` if it has none, contract class, integration tests, property tests, two `.md` justifications, manifest. **PR 5** (4 files): README, feature page, `docs/INVENTORY.md`, changelog fragment |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution first.
- ROP: Either<EncinaError, T>; errors never swallowed; TimeProvider; cancellation checked before each Redis command and propagated.
- Issue #581: EntityChangeAuditStoreRedisStreams is a composite IEntityChangeAuditStore (ADR-036, delivered by #1778): inner queryable store first, then awaited XADD with time-based trimming; reads delegate; the per-tenant purge purges the inner store and trims that tenant's streams at the same cutoff.

IMPLEMENTATION OVERVIEW:
Phase 0: server compatibility check (Redis, Valkey, Dragonfly, Garnet, KeyDB); report only.
Phase 1: package (recommended Encina.Security.Audit.Redis), options, mapper.
Phase 2: composite store (per-tenant purge with the tenant passed by the retention service), persistence check, registrations, EventId range 52550-52599, log class.
Phase 3: IEntityChangeAuditStream reader with consumer-group methods, health check.
Phase 4: error categories, instrumentation, telemetry tags.
Phase 5: unit, guard, contract, integration (Redis collection), property tests; manifest targets with justifications.
Phase 6: docs (long-term retention limits) and changelog fragment.

KEY PATTERNS:
- Dilemmas D1-D7 are recommended, pending the maintainer: follow the answers recorded in the plan.
- One stream per tenant; reserved default; deny without tenant; explicit logged cross-tenant reads; the purge receives its tenant from the retention service iterating ITenantStore (#4182), never the ambient tenant.
- Append-time MINID window for the hot stream; the per-tenant purge trims at each tenant's cutoff; never count-only trimming by default; never XDEL.
- Never log values, ids or server messages.

REFERENCE FILES:
- docs/architecture/adr/036-three-audit-stores.md
- src/Encina.DistributedLock.Redis/ServiceCollectionExtensions.cs
- tests/Encina.TestInfrastructure/Fixtures/RedisFixture.cs
- the plan of issue #580 (Kafka composite, same structure)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | A write path and a stream reader; the stream is not a cache of the inner store. |
| 2 | OpenTelemetry | ✅ | The ADR-036 decorator wraps the composite; tenant tag per ADR-054 D4. |
| 3 | Structured logging | ✅ | Range 52550-52599 registered first; exception types only. |
| 4 | Health checks | ✅ | Phase 3: `EntityChangeAuditRedisStreamsHealthCheck` (related: #754 for the caching providers). |
| 5 | Validation | ✅ | Argument checks before I/O; options validated (hot window, prefix). |
| 6 | Resilience | ✅ | StackExchange.Redis reconnects but does not retry commands; a failed append is `Unavailable`, and the fail-closed writer's retry repeats the idempotent inner write and the append (supersedes the issue's "deferred"). |
| 7 | Distributed locks | ❌ | `XADD` is atomic per stream; nothing to coordinate. |
| 8 | Transactions | ⏭️ | Atomic "database write plus stream" would come from the outbox carrying the entry; tracked by an issue to be opened (draft entity-change-audit-outbox-streaming, shared with #580); its number replaces this text when it exists. |
| 9 | Idempotency | ✅ | Inner store idempotent; the `entryId` field lets consumers deduplicate a retried append. |
| 10 | Multi-tenancy | ✅ | One stream per tenant; reserved `default`; deny without tenant; tenant-bound reader (Design Choice 3); per-tenant retention purge and stream trim with the tenant passed explicitly (Design Choice 1, #4182). |
| 11 | Module isolation | ✅ | `moduleId` field on every stream entry. |
| 12 | Audit trail | ✅ | This store publishes the entity-change audit trail. |

---

## Prerequisites & Dependencies

- **#1778 (open, `[DEBT]`, no `needs-decision`, no blocking dependency)** delivers the contract, the decorator, the retention service and the shared behavioral contract base. If it merges without a reusable base, the first of #577-#582 to reach its integration pull request adds it (the same fallback as the #582 plan).
- A queryable inner store must be registered (any of the 10 database providers from #1778, or #577-#579, #582).
- #240 (Redis Streams transport) and #595 (Redis delayed retry) are related; Design Choice 2 keeps this package independent of both.
- #1240 (open) owns the hash chain (Design Choice 7).
- **#4182 (open, `[DEBT]`)** decides the per-tenant retention mechanism (tenant enumeration through `ITenantStore`, each tenant's policy, the tenant-explicit purge signature) before #1778 implements `EntityChangeAuditRetentionService`; the purge of Design Choice 1 follows it.
- The EventId slot 52550-52599 lies after the #4007 reservations (50000-52349, #4174 merged).

---

## Next Steps

1. The maintainer answers D1 to D7 when work on #581 starts (D1 and D2 together with the same choices of #580); the answers go into `## Maintainer Decisions`.
2. Wait for #4182 and #1778; re-check each Design Choice against main; `adversarial-reviewer` verifies the Settled ones.
3. Brief one worker per proposed pull request (5 pull requests), after the Phase 0 table.
4. The final pull request references `Fixes #581`.
