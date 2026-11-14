# Implementation Plan: `IDelayedRetryStore` on Redis with Sorted Sets

> **Issue**: [#595](https://github.com/dlrivada/Encina/issues/595)
> **Type**: Feature
> **Parent**: [#594](https://github.com/dlrivada/Encina/issues/594) (persistent `IDelayedRetryStore` for the 10 database providers); EPIC [#875](https://github.com/dlrivada/Encina/issues/875)
> **Related**: #2254 (recoverability OpenTelemetry and health check, backlog members), #2248 (default recoverability registration, closed), SPEC-000 DEC-009
> **Complexity**: Medium (one new package, Lua scripts, Redis in CI)
> **Estimated Scope**: ~600-800 lines of production code + ~1,600 lines of tests; exceeds one worker pull request, split proposed in Phase 1

---

## Summary

### What exists today (re-verified on 2026-11-14, main `c871956bf`)

- `IDelayedRetryStore` has five `Task`-returning members (`AddAsync`, `GetPendingMessagesAsync(batchSize)`, `MarkAsProcessedAsync`, `MarkAsFailedAsync(id, errorMessage)`, `DeleteByContextIdAsync`) in `src/Encina.Messaging/Recoverability/IDelayedRetryScheduler.cs`, plus `IDelayedRetryMessage` and `IDelayedRetryMessageFactory`. The issue's snippet (`DelayedRetry`, `ScheduleRetryAsync`, `GetDueRetriesAsync`, `IncrementRetryCountAsync`, `RemoveAsync`, and `DateTime.ToUnixTimeSeconds()`, which is not a .NET method) does not match `src`.
- No store exists outside tests. #594 plans the ten database stores and changes this contract (`Either<EncinaError, T>` returns, a claim-aware `GetPendingMessagesAsync`, `GetBacklogAsync`, `TenantId` and `ModuleId`, the terminal and cancel semantics, the idempotency key), subject to the maintainer's answers to its Design Choices 1-4 and 6-9. This plan **cannot compile against main today** and is blocked until: (a) the maintainer has answered #594 D1 to D4, (b) #594's contract PRs **P1 and P2** and its decorator PR **P4** have merged (P1: `Either` interface, P2: claim-aware pending, backlog, tenant/module, fake and the shared contract suite, P4: `InstrumentedDelayedRetryStore`, which this plan's telemetry test needs). No #595 pull request opens before them.
- Fallback per answer to #594 (the plan is rewritten, not patched, if an answer is not the recommended one): D1 B (delete on processed) means `MarkAsProcessedAsync` is `DEL` and no retention TTL; D2 B (distributed lock) replaces the claim script with a lock around `ZRANGEBYSCORE`; D2 C (accept duplicates) drops the `processing` set; D3 B (keep `Task`) changes the mapping to exceptions; D4 B (no typed tenant and module columns) drops those hash fields.
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

- The three mutations of the store are partial updates, and a hash keeps them cheap and script-light. The key layout is `{encina:delayedretry}:msg:{id}` (hash), `{...}:schedule` (sorted set scored by `ExecuteAtUtc` as Unix milliseconds), `{...}:processing` (sorted set scored by the claim expiry), `{...}:att:{contextId}:{attempt}` (the idempotency key, `SET NX`, holding the `id`) and `{...}:ctxp:{contextId}` (the id of the context's **pending** entry, removed when that entry is processed, failed or cancelled). The common `{...}` hash tag keeps every key of a multi-key script in one slot, so the store works on Redis Cluster; the tag is not optional (see the key prefix rule in Phase 2).
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
- Errors: a `RedisException` or a script error is `Left(EncinaError)` with an error code, never swallowed (`AGENTS.md` section 3, errors not swallowed in background infrastructure); the result shape is the one #594 Design Choice 3 settles, the codes are the shared `DelayedRetryErrorCodes` of #594 (table below). `MarkAsFailedAsync` stores the error code or exception type only, never raw text (`EncinaError.Message` never reaches plaintext storage).
- Backlog members (contract note of 2026-10-16 on #2254; shape per #594 Design Choice 9): pending count is `ZCARD schedule + ZCARD processing`; the oldest pending entry is `ZRANGE schedule 0 0 WITHSCORES`.
- Secrets: connection strings in options are `[JsonIgnore]` with a redacting `ToString()` (`AGENTS.md` section 3, #851).
- Why sorted sets: the structure is the issue's own requirement (the title of #595, "with Sorted Sets"); option B (Streams, an external scheduler) is excluded because Streams cannot order future entries and Hangfire/Quartz adapters are future work (`AGENTS.md` section 3, Scheduling). "Redis as an additional provider" holds only while #594's ten database stores stay the default.

**Class:** Settled
**Settled by:** the text of issue #595 (Sorted Sets), AGENTS.md section 1 (pay-for-what-you-use) and section 3 (TimeProvider, errors not swallowed, secrets in options, Scheduling), AGENTS.md section 5 (the ten database stores remain the durable default), and the contract note of 2026-10-16 on #2254

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

### Trivial decisions

| Decision | Value | Citation |
|----------|-------|----------|
| Date representation in scores and hash fields | Unix milliseconds (`long`) | The lowest precision among the stores is MongoDB's BSON date (milliseconds), so the shared contract suite of #594 compares `ExecuteAtUtc` truncated to milliseconds for every provider, and a `double` score cannot hold 100 ns ticks (about 6e17 exceeds the 2^53 integer range) |
| Pending order | `(ExecuteAtUtc, Id)`; equal scores order by the member, the `Id` in `"D"` format | Same order as #594 (its Trivial table) |

### Error codes and categories (ADR-062)

The codes are the `DelayedRetryErrorCodes` of #594 (P1), created with `EncinaErrors.Create(code, category, ...)`; each row has a `GetCategory` test. Only the code reaches logs, storage and tags.

| Outcome | Code (category) |
|---------|-----------------|
| `RedisTimeoutException` | `delayed_retry.store_timeout` (`Timeout`) |
| Connection failure, `RedisConnectionException`, `LOADING`, `READONLY` | `delayed_retry.store_unavailable` (`Unavailable`) |
| Script error, other `RedisServerException` | `delayed_retry.store_failed` (`Internal`) |
| `MarkAs*` for an absent id | `delayed_retry.not_found` (`NotFound`) |

---

## Implementation Phases

### Phase 1: Foundations — Package, EventIds, Split

> **Goal**: Scaffolding and the order of work relative to #594.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.Messaging.Redis/Encina.Messaging.Redis.csproj` (name per Design Choice 1; layout of `src/Encina.DistributedLock.Redis`), `PublicAPI.Shipped.txt`, `PublicAPI.Unshipped.txt`, README, and a row in `Encina.slnx`.
2. **Modify** `src/Encina/Diagnostics/EventIdRanges.cs`: add `MessagingDelayedRetryRedis = (6830, 6839)` (family block 6800-6849) and the entry of the `AssemblyRanges` map in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`.
3. **Create** `.github/coverage-manifest/Encina.Messaging.Redis.json` with the per-file, per-flag targets of the table "Per-file coverage targets" (Phase 4), each with its one-sentence justification.
4. **Order**: blocked until the maintainer answers #594 D1-D4 and #594's P1 (contract), P2 (claim, backlog, tenant and module, fake, shared contract suite) and P4 (`InstrumentedDelayedRetryStore`) have merged (see "What exists today"); no #595 pull request opens before.
5. **Split** into four pull requests, each with its own tests and manifest rows so the blocking `crap-gate` can pass, each at most 15 files and 1,500 lines **counting tests, manifests, PublicAPI, `ProjectReference` and DI/contract tests**:

   | PR | Content | Files (approx.) |
   |----|---------|-----------------|
   | R1 | Package foundations: csproj, two PublicAPI files, message, factory, mapper, options (with the key prefix validation), `Encina.slnx`, `EventIdRanges.cs`, the allocation-test map, manifest; unit tests (mapper, factory, options) and guard tests | 14 |
   | R2 | Scripts, store, `ServiceCollectionExtensions`, `Log.cs`, health check, PublicAPI update; unit tests with a mocked `IDatabase` (fault mapping, script arguments), DI test (`ValidateOnBuild`, `ValidateScopes`, with and without a pre-registered multiplexer, both orders), decorator-wraps test, guard tests | 12 |
   | R3 | Derived contract test class over the shared suite of #594 P2, property test, integration tests on the Redis collection fixture, two load/benchmark justification `.md` files, manifest update | 6 |
   | R4 | README, configuration guide section, `docs/INVENTORY.md`, changelog fragment; carries `Fixes #595` | 4 |

   Line counts are re-measured when each PR is briefed.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0) adds a Redis-backed IDelayedRetryStore (issue #595). The contract in
src/Encina.Messaging/Recoverability/IDelayedRetryScheduler.cs is changed by #594 (its PRs P1, P2 and P4 and the maintainer's answers to D1-D4); wait for them.
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
2. **Create** `DelayedRetry/DelayedRetryMessageMapper.cs` (hash fields to and from the message; dates as Unix milliseconds, see the Trivial table) and `DelayedRetry/DelayedRetryScripts.cs` (the Lua text as constants, loaded with `LuaScript.Prepare`).
3. **Create** `DelayedRetry/DelayedRetryStoreRedis.cs` with the key layout of Design Choice 3, **aligned with #594's semantics** (Design Choices 6 and 8 there):
   - `AddAsync`: one script: `SET att:{contextId}:{attempt} {id} NX`; if it already exists the script returns the duplicate outcome and `AddAsync` returns `Right(false)` (a rescheduled attempt with a new `Id` creates no second entry); otherwise `HSET` the message, `ZADD schedule`, `SET ctxp:{contextId}`.
   - `GetPendingMessagesAsync`: the claim script of Design Choice 2 (`ZRANGEBYSCORE schedule -inf now LIMIT 0 n`, move to `processing` with score `now + visibility timeout`, return the ids); the **same script first reclaims** expired `processing` entries, returning an id to `schedule` only if its hash still exists (`EXISTS`), else `ZREM`. After the claim the store reads each hash (`HGETALL`); an empty hash (expired by the retention TTL or deleted by cancel in between) removes the id from `processing` (`ZREM`) and is skipped, so no dangling id is reclaimed forever. A test with the fake clock covers both.
   - `MarkAsProcessedAsync`: `ZREM` from both sets, `HSET ProcessedAtUtc`, `DEL ctxp:{contextId}`, `EXPIRE` of the hash and of `att:` with the retention option so a terminal hash never lives forever (the default follows #594 Design Choice 1 A, 7 days; with option B of #594 it is a `DEL`).
   - `MarkAsFailedAsync`: terminal like processed (#594 Design Choice 6 A): `ZREM`, `HSET ProcessedAtUtc` and the error code, `DEL ctxp:`, the same `EXPIRE`; the failed entry is excluded from pending and expires with the retention TTL.
   - `DeleteByContextIdAsync`: acts on **pending entries only**: resolve the id through `ctxp:{contextId}`; if the entry is in `schedule` or has an expired claim, `ZREM`, `DEL` the hash and `ctxp:` and return `true`; a processed, failed or live-claimed entry returns `false` and is not touched.
   - `GetBacklogAsync`: `ZCARD` and `ZRANGE ... WITHSCORES` as in Design Choice 4.
   The shared contract suite of #594 (idempotency, terminal states, cancel) therefore passes unchanged on this store.
4. **Create** `RedisDelayedRetryOptions.cs` (key prefix, visibility timeout, retention TTL, database index, connection string `[JsonIgnore]` with a redacting `ToString()`). **Key prefix rule**: the prefix must contain a hash tag `{...}` (a prefix without it breaks every multi-key script on Redis Cluster with `CROSSSLOT`); the options are validated with `ValidateOnStart` and a startup test covers a prefix with and without the tag. Then `ServiceCollectionExtensions.cs` (`AddEncinaDelayedRetryRedis(...)` overloads taking an `IConnectionMultiplexer`, a factory or a connection string, `TryAddSingleton` of the multiplexer, then `AddEncinaDelayedRetryStore<DelayedRetryStoreRedis, DelayedRetryMessageFactory>`).
5. **Create** `Diagnostics/Log.cs` (ids 6830-6839) and `Health/RedisDelayedRetryHealthCheck.cs` (`PING` plus backlog numbers; `DefaultName` const, static `Tags`, scoped resolution).
6. **Update** `PublicAPI.Unshipped.txt`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Phase 1 created Encina.Messaging.Redis and registered EventIds 6830-6839. #594 (P1, P2) changed
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
- Adding the same (contextId, attempt) twice leaves one schedule entry and returns Right(false); cancel
  acts on pending entries only; processed and failed hashes get a retention TTL.
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
2. **Verify** by test that `InstrumentedDelayedRetryStore` (added by #594's decorator PR P4 in `Encina.OpenTelemetry`) wraps this store; no store-level `Meter` here and no tenant tag of this package (ADR-054 D4: the decorator adds one only when multi-tenancy is on).
3. **Store** `TenantId` and `ModuleId` as hash fields from the message (the interface carries them after #594 P2); keys are not tenant-scoped (the idempotency and pending keys use the context id and attempt, which are globally unique Guid/int pairs, so no tenant segment exists to collide and no reserved `default` segment arises). The store returns every tenant's entries, as #594's contract says, and the processor's fail-closed restore of tenant context (#594 Design Choice 10) is the gate; the store itself never decides.
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
7. **Semantic tests** (the shared suite of #594 plus Redis specifics): duplicate `(contextId, attempt)` with a new `Id` is `Right(false)` and one entry; cancel deletes a pending entry and not a processed, failed or claimed one; a processed or failed hash has a TTL (`KeyTimeToLive` set) and `att:` expires with it; a dangling id in `processing` is removed; keys contain the hash tag and a prefix without it fails startup; every row of the error table maps to its code and category.
8. **README security notes**: the hash holds `RequestContent` and `ContextContent` in clear, so the server needs TLS and persistence at rest is the operator's; payload encryption, when configured, is applied by the same Messaging payload encryption path that serves the database stores (exercised in `StorePayloadEncryptionTests`), not by this package; the worker names the component after verifying it in `src`.

#### Per-file coverage targets (percent, per flag)

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `DelayedRetry/DelayedRetryMessage.cs` | 90 | 0 | 70 | 0 | 80 | Value type with no validated parameters |
| `DelayedRetry/DelayedRetryMessageFactory.cs` | 95 | 90 | 80 | 0 | 80 | Copies values; the null guard is the only branch |
| `DelayedRetry/DelayedRetryMessageMapper.cs` | 95 | 90 | 60 | 60 | 80 | Pure mapping; millisecond truncation round trips are a property |
| `DelayedRetry/DelayedRetryScripts.cs` | 0 | 0 | 0 | 0 | 90 | Lua text constants: no executable C# to unit test, exercised only by running them on a real Redis |
| `DelayedRetry/DelayedRetryStoreRedis.cs` | 85 | 90 | 85 | 60 | 85 | Fault mapping through a mocked `IDatabase`; contract and Redis cover claim, cancel and terminal states; property covers due-ordering |
| `RedisDelayedRetryOptions.cs` | 90 | 90 | 0 | 0 | 50 | Validation incl. the hash-tag rule is unit and guard work; contract tests do not drive options |
| `ServiceCollectionExtensions.cs` | 90 | 95 | 0 | 0 | 70 | DI tests are unit; contract tests do not register services |
| `Diagnostics/Log.cs` | 60 | 0 | 0 | 0 | 40 | Source-generated logging |
| `Health/RedisDelayedRetryHealthCheck.cs` | 90 | 90 | 0 | 0 | 80 | `PING` plus backlog; no contract surface |

Each PR reports the measured per-flag value of every touched file against these targets (`coverage-report.cs` "Per-file targets" table).

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
| `IDelayedRetryStore`, `IDelayedRetryMessage`, `IDelayedRetryMessageFactory` | `src/Encina.Messaging/Recoverability/IDelayedRetryScheduler.cs` | The contract (changed by #594 P1 and P2) |
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
| **Total** | **~30** | Four pull requests R1-R4 (Phase 1 item 5), each at most 15 files and 1,500 lines counting tests, manifests, PublicAPI and fragments |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #595 - IDelayedRetryStore on Redis Sorted Sets.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution always.
- Railway Oriented Programming; the contract is changed by #594 (implement after its P1, P2, P4 and the maintainer's answers).
- Redis is an additional optional provider; the ten database stores of #594 stay the durable default.

IMPLEMENTATION OVERVIEW:
1. Foundations: package Encina.Messaging.Redis, EventId range 6830-6839, manifest, four-PR split (each PR with its own tests).
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
| 2 | OpenTelemetry | ✅ Phase 3 | The `InstrumentedDelayedRetryStore` decorator (added by #594 P4) wraps this store; a test proves it; backlog metric names are owned by #2254; this package adds no tenant tag (ADR-054 D4) |
| 3 | Structured Logging | ✅ Phase 2 | `[LoggerMessage]` ids 6830-6839, registered first (ADR-021) |
| 4 | Health Checks | ✅ Phase 2 | `PING` plus backlog numbers from `GetBacklogAsync`, shared with #2254 |
| 5 | Validation | ✅ Phase 2 | Guard tests for `batchSize`, `Guid.Empty` and null messages |
| 6 | Resilience | ✅ Phase 3 | StackExchange.Redis connect/sync timeouts and reconnect policy through options; no extra layer |
| 7 | Distributed Locks | ❌ N/A | The Lua claim replaces a lock; it becomes ✅ only if Design Choice 2 is B |
| 8 | Transactions | ❌ N/A | Multi-key operations are single Lua scripts; there is no ambient unit of work in Redis |
| 9 | Idempotency | ✅ Phase 2 | `SET NX` on `att:{contextId}:{attempt}` inside the add script, duplicate returns `Right(false)` as #594 Design Choice 8 A defines; processing is guarded by the claim |
| 10 | Multi-Tenancy | ✅ Phase 3 | `TenantId` stored as a hash field from the message (interface per #594); keys are not tenant-scoped |
| 11 | Module Isolation | ✅ Phase 3 | `ModuleId` stored as a hash field from the message (interface per #594) |
| 12 | Audit Trail | ❌ N/A | Infrastructure retry operations are not auditable events; the dead letter capture audits the terminal outcome |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| Maintainer answers to #594 D1-D4 | Open | A different answer rewrites this plan (fallbacks in "What exists today") |
| #594 P1 (`Either` contract), P2 (claim, backlog, tenant and module, fake, shared contract suite) and P4 (`InstrumentedDelayedRetryStore`) | Open | This plan implements the changed contract; no #595 PR opens before they merge |
| `StackExchange.Redis`, `Testcontainers.Redis` | ✅ Available | In `Directory.Packages.props` |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Recoverability OpenTelemetry and health check | #2254 | Owns metric and health check names |
| Redis-family caching providers | `AGENTS.md` section 5 | Relevant to Design Choice 5 only |

---

## Next Steps

1. Link this plan from #595 and ask the maintainer to answer Design Choices 1, 2 (together with #594 Design Choice 2), 3 and 5 when work starts.
2. Implement Phases 1-5 after #594's P1, P2 and P4 as the four pull requests R1-R4 of Phase 1 item 5, `Fixes #595` on the last (R4).
