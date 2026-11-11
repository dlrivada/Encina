# Implementation Plan: `IDelayedRetryStore` on Redis with Sorted Sets

> **Issue**: [#595](https://github.com/dlrivada/Encina/issues/595)
> **Type**: Feature
> **Parent**: [#594](https://github.com/dlrivada/Encina/issues/594) (persistent `IDelayedRetryStore` for the 10 database providers); EPIC [#875](https://github.com/dlrivada/Encina/issues/875)
> **Related**: #2254 (recoverability OpenTelemetry and health check, backlog members), #2248 (default recoverability registration, closed), SPEC-000 DEC-009
> **Complexity**: Medium (one new package, Lua scripts, Redis in CI)
> **Estimated Scope**: ~600-800 lines of production code + ~1,600 lines of tests; exceeds one worker pull request, split proposed in Phase 1

---

## Summary

### What exists today (read on 2026-11-11, main `942c0f124`)

- `IDelayedRetryStore` has five `Task`-returning members (`AddAsync`, `GetPendingMessagesAsync(batchSize)`, `MarkAsProcessedAsync`, `MarkAsFailedAsync(id, errorMessage)`, `DeleteByContextIdAsync`) in `src/Encina.Messaging/Recoverability/IDelayedRetryScheduler.cs`, plus `IDelayedRetryMessage` and `IDelayedRetryMessageFactory`. The issue's snippet (`DelayedRetry`, `ScheduleRetryAsync`, `GetDueRetriesAsync`, `IncrementRetryCountAsync`, `RemoveAsync`, and `DateTime.ToUnixTimeSeconds()`, which is not a .NET method) does not match `src`.
- No store exists outside tests. #594 plans the ten database stores and changes this contract (Phase 1 there: `Either<EncinaError, T>` returns, a claim-aware `GetPendingMessagesAsync`, `GetBacklogAsync`, `TenantId` and `ModuleId`, subject to the maintainer's answers). This plan implements the contract **after** #594 Phase 1 and follows its answers.
- `AddEncinaDelayedRetryStore<TStore, TFactory>` (`RecoverabilityServiceCollectionExtensions.cs:256`) registers the store and factory as scoped; `DelayedRetryStartupCheck` fails startup when `EnableDelayedRetries` is on without them.
- Redis packages in `src`: `Encina.Caching.Redis`, `Encina.DistributedLock.Redis` and `Encina.Redis.PubSub`; there is no `Encina.Messaging.Redis`. `Encina.DistributedLock.Redis` shows the registration shape: overloads taking an `IConnectionMultiplexer` or a factory, registered with `TryAddSingleton`. `StackExchange.Redis` 2.12.14 and `Testcontainers.Redis` are in `Directory.Packages.props`.

### What this plan delivers

`DelayedRetryStoreRedis` and `DelayedRetryMessageFactory` in a new package, using a Sorted Set scored by `ExecuteAtUtc`, with Lua scripts for the multi-key operations, options, a registration extension, a health check and tests against a real Redis. Redis is an additional, optional provider: the ten database stores of #594 stay the durable default (`AGENTS.md` section 5), and Redis durability depends on the server's AOF/RDB persistence, which the README must state.

- Affected packages: a new Redis messaging package (name per Design Choice 1), `Encina` (EventId registry), `Directory.Packages.props` (none new: StackExchange.Redis and the Redis container exist); `Encina.Messaging` is consumed.
- Provider category: none of the database matrix; Redis-compatible servers (Valkey, Dragonfly, Garnet, KeyDB) are caching providers and are dealt with in Design Choice 5.

---

## Design Choices

<details>
<summary><strong>1. Where the store lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A new package `Encina.Messaging.Redis`** (the issue's proposal) | Pay-for-what-you-use (`AGENTS.md` section 1); a clear name; messaging users do not take the caching package | One more package to document and release |
| **B) Inside an existing Redis package** (`Encina.Caching.Redis` or `Encina.DistributedLock.Redis`) | Fewer packages; a connection already registered there | Couples a messaging store to caching or lock concerns and their options; a user who wants only the store takes unrelated code |

### Chosen Option: **A — A new package** (recommended, pending the maintainer)

### Rationale

- The existing Redis packages are each named for one concern; a store for the recoverability pipeline belongs to the messaging family, the same reasoning as the per-vendor packages (`Encina.Messaging.Encryption.AwsKms`). The package registers `IConnectionMultiplexer` with `TryAddSingleton`, so an application that already registered it for caching shares the connection.
- B wins when the team wants the smallest possible package count and already references the lock package.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Claiming a due entry so two processors never take the same one</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A Lua script that moves due ids from the schedule set into a processing set with a visibility timeout** | Atomic and self-contained; a crashed processor's entries return after the timeout; the same shape as the claim-with-timeout of #594 (Design Choice 2, option A) | Needs a script and a reclaim rule; scripts must run on every Redis-compatible server tested |
| **B) `IDistributedLockProvider` (`Encina.DistributedLock.Redis`) around each batch** | No script | Requires a lock registration (an extra module in ADR-050 terms) and serializes processors on a lock |
| **C) `ZPOPMIN`-style removal on read** | Simplest | An entry is lost if the processor crashes after the pop |

### Chosen Option: **A — A Lua claim with a visibility timeout** (recommended, pending the maintainer)

### Rationale

- Redis executes a script atomically, so A needs no extra module and gives the same at-least-once-with-expiry guarantee that the database stores get from their claim column. C violates `AGENTS.md` section 3 (background infrastructure never reports success after losing work).
- The answer follows #594 Design Choice 2: if the maintainer picks the lock (B) there, B here is the consistent choice; if the claim, A. B also wins when the application already runs `Encina.DistributedLock.Redis` and wants no scripts.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Message representation — one JSON string or a hash</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One JSON string per message** (the issue's proposal) | One round trip to read; easy schema evolution | Every partial update (`ProcessedAtUtc`, error code, claim) rewrites the payload or needs `cjson` in Lua, which differs between Redis-compatible servers and re-encodes large `RequestContent` strings |
| **B) A Redis hash with one field per property** | `MarkAsProcessedAsync` and `MarkAsFailedAsync` are `HSET` of one or two fields; the large `RequestContent`/`ContextContent` fields are never rewritten; no `cjson` in scripts | A read is `HGETALL` and a mapper; more fields to keep in step with `IDelayedRetryMessage` |

### Chosen Option: **B — A hash per message** (recommended, pending the maintainer)

### Rationale

- The three mutations of the store are partial updates, and a hash keeps them cheap and script-light. The key layout is `{encina:delayedretry}:msg:{id}` (hash), `{...}:schedule` (sorted set scored by `ExecuteAtUtc` as Unix milliseconds), `{...}:processing` (sorted set scored by the claim expiry) and `{...}:ctx:{contextId}` (string holding the `id`). The common `{...}` hash tag keeps every key of a multi-key script in one slot, so the store works on Redis Cluster.
- A wins when updates are rare compared with reads and the simplest read path matters most.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Sorted Sets, TimeProvider, error text and contract conventions</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Sorted Sets scored by `ExecuteAtUtc`, `TimeProvider`, error code only, `Either` per #594, Redis as an additional provider** | Native time ordering and range queries (`ZRANGEBYSCORE`, O(log N + M)); same contract and conventions as the database stores | Durability depends on the server persistence settings |
| **B) Redis Streams or an external scheduler (Hangfire/Quartz) as the store** | Streams have consumer groups | Streams lack ordering of future entries; Hangfire and Quartz run infrastructure jobs and adapters to use them as scheduling backends are future work (`AGENTS.md` section 3, Scheduling) |

### Chosen Option: **A — Sorted Sets and the shared conventions**

### Rationale

- Time: the due test uses the injected `TimeProvider` (`now` passed to the script as an argument), never the system clock (`AGENTS.md` section 3). Redis `TIME` is not used, so the fake clock drives the tests.
- Errors: a `RedisException` or a script error is `Left(EncinaError)` with an error code, never swallowed (`AGENTS.md` section 3, errors not swallowed in background infrastructure); the result shape is the one #594 Design Choice 3 settles. `MarkAsFailedAsync` stores the error code or exception type only, never raw text (`EncinaError.Message` never reaches plaintext storage).
- Backlog members (decision on #2254, 2026-10-16): pending count is `ZCARD schedule + ZCARD processing`; the oldest pending entry is `ZRANGE schedule 0 0 WITHSCORES`.
- Secrets: connection strings in options are `[JsonIgnore]` with a redacting `ToString()` (`AGENTS.md` section 3, #851).

**Class:** Settled
**Settled by:** AGENTS.md section 1 (pay-for-what-you-use) and section 3 (TimeProvider, errors not swallowed, secrets in options, Scheduling), AGENTS.md section 5 (the ten database stores remain the durable default), and the maintainer decision of 2026-10-16 on #2254

</details>

<details>
<summary><strong>5. Which Redis-compatible servers the tests cover</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Redis only in CI; Valkey, Dragonfly, Garnet and KeyDB documented as protocol-compatible but untested** | One container, fast CI; honest claims | A user on Garnet (partial Lua support) finds a gap alone |
| **B) The five servers in a matrix** | Verified compatibility, parity with the caching category | Five fixtures, slower CI, and Lua gaps become the maintainer's burden |

### Chosen Option: **A — Redis only** (recommended, pending the maintainer)

### Rationale

- This is a messaging store, not a caching provider, so the "8 caching providers" rule of `AGENTS.md` section 5 does not apply; the scripts use only core commands (`ZADD`, `ZREM`, `ZRANGEBYSCORE`, `HSET`, `EXPIRE`), which keeps compatibility likely. The README states the tested server.
- B wins if the 1.0 promise is that every Redis family server is a first-class store.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Foundations — Package, EventIds, Split

> **Goal**: Scaffolding and the order of work relative to #594.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Messaging.Redis/Encina.Messaging.Redis.csproj` (name per Design Choice 1; layout of `src/Encina.DistributedLock.Redis`), `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`, README, and a row in `Encina.slnx`.
2. **Modify** `src/Encina/Diagnostics/EventIdRanges.cs`: add `MessagingDelayedRetryRedis = (6830, 6839)` (family block 6800-6849) and the entry of the `AssemblyRanges` map in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`.
3. **Create** `.github/coverage-manifest/Encina.Messaging.Redis.json` with per-flag targets and one-sentence justifications (no relational flag; integration with a real Redis).
4. **Order**: implement after #594 Phase 1 merges (contract, claim semantics, backlog member, tenant and module fields).
5. **Split**: S1 package production code (about 12 files), S2 tests (about 10 files), S3 documentation.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0) adds a Redis-backed IDelayedRetryStore (issue #595). The contract in
src/Encina.Messaging/Recoverability/IDelayedRetryScheduler.cs is changed by #594 Phase 1; wait for it.
src/Encina.DistributedLock.Redis is the layout and registration reference.

TASK:
1. Create the project (name per the answer to Design Choice 1), PublicAPI files, README, slnx entry.
2. Register EventId range 6830-6839 in EventIdRanges.cs and the AssemblyRanges map of
   EncinaEventIdAllocationTests.
3. Create the coverage manifest.

KEY RULES:
- ADR-021: register the range before any [LoggerMessage].
- Zero warnings; RS0016/RS0017 in PublicAPI.Unshipped.txt.
- No store code in this phase.

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
- src/Encina.DistributedLock.Redis/ and .github/coverage-manifest/Encina.DistributedLock.Redis.json
```

</details>

---

### Phase 2: `DelayedRetryStoreRedis`

> **Goal**: The store passes the shared contract against a real Redis.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `DelayedRetry/DelayedRetryMessage.cs` (implements `IDelayedRetryMessage`) and `DelayedRetryMessageFactory.cs` (`Create(DelayedRetryMessageData)`).
2. **Create** `DelayedRetry/DelayedRetryMessageMapper.cs` (hash fields to and from the message, dates as Unix milliseconds or ISO strings, one choice for the package) and `DelayedRetry/DelayedRetryScripts.cs` (the Lua text as constants, loaded with `LuaScript.Prepare`).
3. **Create** `DelayedRetry/DelayedRetryStoreRedis.cs` with the key layout of Design Choice 3:
   - `AddAsync`: one script, `HSET` the message, `ZADD schedule`, `SET ctx:{contextId}`; adding the same `Id` twice leaves one entry.
   - `GetPendingMessagesAsync`: the claim script of Design Choice 2 (`ZRANGEBYSCORE schedule -inf now LIMIT 0 n`, move to `processing` with score `now + visibility timeout`, return the ids), then `HGETALL` for each; expired `processing` entries are returned to `schedule` first.
   - `MarkAsProcessedAsync`: `ZREM` from both sets, `HSET ProcessedAtUtc`, optional `EXPIRE` from the retention option (follows #594 Design Choice 1).
   - `MarkAsFailedAsync`: `ZREM`, store the error code.
   - `DeleteByContextIdAsync`: resolve the id through `ctx:{contextId}`, `ZREM`, `DEL` both keys; returns whether something was deleted.
   - `GetBacklogAsync`: `ZCARD` and `ZRANGE ... WITHSCORES` as in Design Choice 4.
4. **Create** `RedisDelayedRetryOptions.cs` (key prefix, visibility timeout, retention TTL, database index, connection string `[JsonIgnore]` with a redacting `ToString()`) and `ServiceCollectionExtensions.cs` (`AddEncinaDelayedRetryRedis(...)` overloads taking an `IConnectionMultiplexer`, a factory or a connection string, `TryAddSingleton` of the multiplexer, then `AddEncinaDelayedRetryStore<DelayedRetryStoreRedis, DelayedRetryMessageFactory>`).
5. **Create** `Diagnostics/Log.cs` (ids 6830-6839) and `Health/RedisDelayedRetryHealthCheck.cs` (`PING` plus backlog numbers; `DefaultName` const, static `Tags`, scoped resolution).
6. **Update** `PublicAPI.Unshipped.txt`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Phase 1 created Encina.Messaging.Redis and registered EventIds 6830-6839. #594 Phase 1 changed
IDelayedRetryStore (Either returns, claim-aware GetPendingMessagesAsync, GetBacklogAsync,
TenantId/ModuleId). The database stores of #594 are the behavioral reference.

TASK:
1. Implement the message, factory, mapper, scripts and DelayedRetryStoreRedis per the plan's
   Design Choices 2-4: hash per message, schedule and processing sorted sets, ctx key, hash-tag
   {encina:delayedretry} so scripts are valid on Redis Cluster.
2. Pass "now" from the injected TimeProvider into every script (never Redis TIME, never
   DateTime.UtcNow). Visibility timeout and retention come from the options.
3. RedisException / script errors -> Left(EncinaError) with an error code and no exception message;
   MarkAsFailedAsync stores a code or exception type only.
4. Options with [JsonIgnore] on the connection string and a redacting ToString(); registration with
   TryAdd; Log.cs ids 6830-6839; health check.

KEY RULES:
- All Redis calls async with a CancellationToken where the API accepts one (CommandFlags otherwise).
- Adding the same Id twice leaves one schedule entry.
- Scripts use core commands only (ZADD, ZREM, ZRANGEBYSCORE, HSET, HGETALL, EXPIRE, DEL).

REFERENCE FILES:
- src/Encina.Messaging/Recoverability/IDelayedRetryScheduler.cs
- src/Encina.DistributedLock.Redis/ServiceCollectionExtensions.cs and RedisDistributedLockProvider.cs
- the #594 database stores (for example src/Encina.ADO.SqlServer/DelayedRetry/)
```

</details>

---

### Phase 3: Cross-Cutting Integration and Observability

> **Goal**: Registration completeness and telemetry reuse.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** DI tests with `ValidateOnBuild` and `ValidateScopes`: the application starts with `EnableDelayedRetries` on and only this registration, with and without a pre-registered `IConnectionMultiplexer`, and with an application-registered store winning in either order.
2. **Verify** by test that `InstrumentedDelayedRetryStore` (added by #594 Phase 6 in `Encina.OpenTelemetry`) wraps this store; no store-level `Meter` here.
3. **Store** `TenantId` and `ModuleId` as hash fields from the message (the interface carries them after #594 Phase 1); keys are not tenant-scoped.
4. **Expose** StackExchange.Redis connect/sync timeouts and reconnect policy through the options.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
The store exists (Phase 2). AGENTS.md section 3 requires proven registration completeness; section 6
the 12-function evaluation recorded in the plan matrix.

TASK:
1. Write the DI tests for AddEncinaDelayedRetryRedis in the configurations of the plan.
2. Write the test that the telemetry decorator wraps the store.
3. Map TenantId and ModuleId into the hash and back.

KEY RULES:
- No new Meter or ActivitySource in this package.
- Tests execute real package code.

REFERENCE FILES:
- src/Encina.OpenTelemetry/MessagingStores/ (the decorators)
- src/Encina.Messaging/Recoverability/RecoverabilityServiceCollectionExtensions.cs (line 256)
```

</details>

---

### Phase 4: Testing

> **Goal**: Every applicable flag reaches its manifest target; Redis runs in CI.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit tests**: key layout, mapper round trips, `Left` on `RedisException`, script argument building, fake `TimeProvider`.
2. **Guard tests**: every public method with parameters (`batchSize`, `Guid.Empty`, null messages).
3. **Contract tests**: the `IDelayedRetryStore` contract shared with #594, with the real store.
4. **Property tests**: due ordering by score and "a processed entry is never returned again", only if the claim logic stays non-trivial.
5. **Integration tests**: `Testcontainers.Redis` through a shared `[Collection("Redis")]` fixture (reuse `tests/Encina.IntegrationTests/Infrastructure/Caching/Fixtures/RedisFixture.cs` if its collection fits, else a messaging collection); concurrent claim (no entry returned twice), claim timeout reclaim, `DeleteByContextIdAsync`, cluster-safe keys; `ClearAllDataAsync` in `InitializeAsync`; never a `.md` justification.
6. **Justification files**: `.md` for load and benchmark tests (throughput is Redis's; concurrency is covered by the integration test).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Per-flag coverage (AGENTS.md section 9): unit, guard, contract, property and integration each reach
their own target in .github/coverage-manifest/Encina.Messaging.Redis.json.

TASK:
1. Write the unit, guard, contract and (if kept) property tests of the plan.
2. Write the integration tests on a shared Redis collection fixture, including concurrent claim and
   the timeout reclaim with a fake TimeProvider driving "now".
3. Add the load and benchmark justification .md files.
4. Measure per-flag coverage and CRAP with coverage-report.cs and report it against the manifest.

KEY RULES:
- Shouldly through Encina.Testing.Shouldly; shared collection fixtures only, never IClassFixture.
- CRAP <= 10 on changed methods.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Infrastructure/Caching/Fixtures/RedisFixture.cs
- .github/scripts/coverage-report.cs
- docs/testing/integration-tests.md
```

</details>

---

### Phase 5: Documentation and Finalization

> **Goal**: Users can configure the store and know its durability limits.

<details>
<summary><strong>Tasks</strong></summary>

1. XML doc comments on every public API.
2. `changelog.d/595-redis-delayed-retry-store.added.md`.
3. Package README (key layout, claim and visibility timeout, Redis persistence requirement AOF/RDB, tested server), a configuration guide section, `docs/INVENTORY.md` row.
4. ADR only if Design Choice 1 sets a package-layout policy (ADR number assigned when the ADR pull request opens).
5. `dotnet build --configuration Release` with zero warnings; `dotnet test` with every flag at its target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
The store is implemented and tested. Documentation is written by the docs-writer agent following
the encina-docs skill; figures are cited with covref markers, never typed.

TASK:
1. Complete XML docs and the PublicAPI files.
2. Write the changelog fragment and the README, including the persistence requirement.
3. Update INVENTORY.md.

KEY RULES:
- English only; never edit CHANGELOG.md directly; no hand-typed coverage numbers.

REFERENCE FILES:
- changelog.d/README.md
- .claude/skills/encina-docs/SKILL.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | Use in this feature |
|--------|-----------|---------------------|
| Redis Sorted Sets | `ZADD`, `ZREM`, `ZRANGEBYSCORE` ordered by score, O(log N + M) range | The schedule and processing sets |
| Redis scripting | A script runs atomically; cluster scripts need keys in one slot (hash tag) | The claim script and key layout |
| SPEC-000 DEC-009 | Free, locally testable items are inside 1.0 | Redis in CI through Testcontainers |
| ADR-001, ADR-006 | Railway Oriented Programming | Result shape per #594 Design Choice 3 |
| ADR-018, ADR-021 | Cross-cutting, EventIds | Matrix below, range 6830-6839 |
| Decision on #2254 (2026-10-16) | Pending count and oldest pending entry on every persistent store | `GetBacklogAsync` |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IDelayedRetryStore`, `IDelayedRetryMessage`, `IDelayedRetryMessageFactory` | `src/Encina.Messaging/Recoverability/IDelayedRetryScheduler.cs` | The contract (changed by #594 Phase 1) |
| `AddEncinaDelayedRetryStore<TStore, TFactory>` | `RecoverabilityServiceCollectionExtensions.cs:256` | Registration |
| `AddEncinaDistributedLockRedis` overloads | `src/Encina.DistributedLock.Redis/ServiceCollectionExtensions.cs` | Registration shape with `IConnectionMultiplexer` |
| `RedisFixture` | `tests/Encina.IntegrationTests/Infrastructure/Caching/Fixtures/RedisFixture.cs` | Integration tests |
| Telemetry decorators | `src/Encina.OpenTelemetry/MessagingStores/` | Store telemetry (decorator added by #594) |
| `StackExchange.Redis`, `Testcontainers.Redis` | `Directory.Packages.props` | Client and container |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging.Redis` | 6830-6839 (new: `MessagingDelayedRetryRedis`) | From the family block 6800-6849; registered in `EventIdRanges.cs` and the `AssemblyRanges` map first |
| #588, #589 | 6800-6829 | Sibling plans, same block |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| New production files | ~12 | Message, factory, mapper, scripts, store, options, DI extension, Log, health check, csproj, README, 2 PublicAPI |
| Changed production files | 3 | `EventIdRanges.cs`, `Encina.slnx`, the allocation test map |
| Coverage manifest | 1 | One per package |
| Tests | ~10 | Unit, guard, contract, property, integration, DI, 2 justification files |
| Documentation | ~4 | README, guide section, INVENTORY, changelog fragment |
| **Total** | **~30** | Three pull requests (Phase 1 item 5) |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #595 - IDelayedRetryStore on Redis Sorted Sets.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution always.
- Railway Oriented Programming; the contract is changed by #594 Phase 1 (implement after it).
- Redis is an additional optional provider; the ten database stores of #594 stay the durable default.

IMPLEMENTATION OVERVIEW:
1. Foundations: package Encina.Messaging.Redis, EventId range 6830-6839, manifest, three-PR split.
2. Store: hash per message, sorted sets schedule and processing, ctx key, hash tag for cluster
   safety, Lua claim with a visibility timeout, backlog via ZCARD and ZRANGE WITHSCORES.
3. Cross-cutting: DI tests with ValidateOnBuild/ValidateScopes, decorator-based telemetry, health
   check, tenant/module fields, connection options.
4. Tests per flag against a real Redis; docs and changelog, including the persistence requirement.

KEY PATTERNS:
- "now" comes from the injected TimeProvider and is passed into scripts.
- Error code or exception type only in MarkAsFailedAsync; EncinaError.Message never reaches logs.
- Secrets in options: [JsonIgnore] + redacting ToString().
- TryAdd registration so the application's store and multiplexer win in any order.
- Shared collection fixtures for integration tests; Shouldly only.

REFERENCE FILES:
- src/Encina.Messaging/Recoverability/IDelayedRetryScheduler.cs
- src/Encina.DistributedLock.Redis/ServiceCollectionExtensions.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- docs/plans/delayed-retry-redis-implementation-plan-595.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Redis is the store itself; a cache in front would return claimed entries |
| 2 | OpenTelemetry | ✅ Phase 3 | The `InstrumentedDelayedRetryStore` decorator (added by #594 Phase 6) wraps this store; a test proves it; backlog metric names are owned by #2254 |
| 3 | Structured Logging | ✅ Phase 2 | `[LoggerMessage]` ids 6830-6839, registered first (ADR-021) |
| 4 | Health Checks | ✅ Phase 2 | `PING` plus backlog numbers from `GetBacklogAsync`, shared with #2254 |
| 5 | Validation | ✅ Phase 2 | Guard tests for `batchSize`, `Guid.Empty` and null messages |
| 6 | Resilience | ✅ Phase 3 | StackExchange.Redis connect/sync timeouts and reconnect policy through options; no extra layer |
| 7 | Distributed Locks | ❌ N/A | The Lua claim replaces a lock; it becomes ✅ only if Design Choice 2 is B |
| 8 | Transactions | ❌ N/A | Multi-key operations are single Lua scripts; there is no ambient unit of work in Redis |
| 9 | Idempotency | ✅ Phase 2 | Adding the same `Id` twice leaves one schedule entry; processing is guarded by the claim |
| 10 | Multi-Tenancy | ✅ Phase 3 | `TenantId` stored as a hash field from the message (interface per #594); keys are not tenant-scoped |
| 11 | Module Isolation | ✅ Phase 3 | `ModuleId` stored as a hash field from the message (interface per #594) |
| 12 | Audit Trail | ❌ N/A | Infrastructure retry operations are not auditable events; the dead letter capture audits the terminal outcome |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| #594 Phase 1 (contract, claim semantics, backlog member, tenant and module fields) | Open | This plan implements the changed contract; implement after it merges |
| `StackExchange.Redis`, `Testcontainers.Redis` | ✅ Available | In `Directory.Packages.props` |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Recoverability OpenTelemetry and health check | #2254 | Owns metric and health check names |
| Redis-family caching providers | `AGENTS.md` section 5 | Relevant to Design Choice 5 only |

---

## Next Steps

1. Link this plan from #595 and ask the maintainer to answer Design Choices 1, 2 (together with #594 Design Choice 2), 3 and 5 when work starts.
2. Implement Phases 1-5 after #594 Phase 1 as the three pull requests of Phase 1 item 5, `Fixes #595` on the last.
