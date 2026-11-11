# Implementation Plan: Partitioned Sequential Messaging (ordered outbox partitions with pacing) (`Encina.Messaging` + 10 database providers)

> **Issue**: [#469](https://github.com/dlrivada/Encina/issues/469)
> **Type**: Feature (SPEC-002 REQ-042, AC-039 scenarios S3 and S9, AC-043; tracked by #1186)
> **Complexity**: High (9 phases, 10 database providers, about 75 files, delivered in 7 pull requests)
> **Estimated Scope**: about 1,300 lines of production code + about 2,000 lines of tests
> **Family**: message priority and partitioning (#123, #251, #301, #469, #1254). This plan builds on `IOutboxMessage.PartitionKey` and `IPartitionedMessage` (owned by #1254) and on `OutboxFetchCriteria` (owned by #123). It merges after both.

---

## Summary

Messages that carry the same partition key are delivered **one after another in creation order**; messages of different keys run in parallel; a failed message **holds** the later messages of its key until it succeeds or an operator intervenes; and a per-key **pacing** interval limits how fast one key is dispatched. This is the ordered-outbox-partition capability of SPEC-002 REQ-042, accepted by scenario S3 (book, reschedule and cancel delivered in order per appointment) and S9 (pacing; rejection of record N holds later records; a server-directed wait is honoured), per tenant (REQ-061, AC-043).

The issue as written proposes in-process `System.Threading.Channels` queues (`IPartitionedQueueManager`, `PartitionedMessagingOptions`, `PartitionedMessageBehavior`, optional `IPartitionStore`). None of these exists in `src/`, and the maintainer comment and SPEC-002 build the guarantee on the outbox `PartitionKey` column instead. This plan therefore turns the feature into an ordered fetch and a smarter batch processor on the outbox, and drops the in-process artifacts unless the maintainer picks a different mechanism (choice 1).

Facts verified in today's code that shape the design:

- `OutboxBatchProcessor.ProcessAsync` fetches `GetPendingMessagesAsync(...)` and processes the returned messages in a serial `foreach`; a failed message gets `RetryCount` and `NextRetryAtUtc` and the loop moves on to the next one, so a failure today lets later messages of the same entity overtake it.
- Stores order by `CreatedAtUtc` only. `OutboxWriter` creates one message per notification with `_timeProvider.GetUtcNow().UtcDateTime`, so messages written in one transaction can carry equal timestamps (always, under `FakeTimeProvider`), and `Id` is `Guid.NewGuid()` (random). Creation order is therefore not a total order today (choice 2).
- A server-directed wait already exists: `OutboxOptions.MaxRetryAfter` and `OutboxRetryBackoff.ComputeDelay` produce the `NextRetryAtUtc` after a failure.
- `RequeueExhaustedAsync` resets exhausted messages (`RetryCount >= MaxRetries`, not processed); this is the operator tool that releases a held partition.
- `OutboxHealthCheck` exists in `src/Encina.Messaging/Health/`; the outbox has no `TenantId` yet (#737).

- **Affected packages**: `Encina.Messaging`, `Encina.ADO.*` (3), `Encina.Dapper.*` (3), `Encina.EntityFrameworkCore`, `Encina.MongoDB`, `Encina.OpenTelemetry`, `Encina.Testing.Fakes`, `Encina.Testing.Bogus`, `Encina.Testing`.
- **Provider category**: Database (10), all required (AGENTS.md section 5). Broker partitioning (Kafka partitions, Service Bus sessions) is #255 and post-1.0.

---

## Design Choices

<details>
<summary><strong>1. What carries the ordering guarantee — the outbox with the key column, in-process channels, or locks (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) In-process `Channel<T>` per key, as the issue proposes** | Simple and fast; no database contention | The guarantee holds only inside one process; two instances can reorder a key; not durable across restart unless `IPartitionStore` is added (a second abstraction that AGENTS.md section 5 then requires on all 10 providers) |
| **B) Order in the outbox processor: an ordered fetch on the `PartitionKey` column plus a stop-on-failure batch loop (row claiming of #1251 for several instances)** | Correct across restarts; matches REQ-042 and AC-039; the outbox row is the durable record, so no `IPartitionStore` and no queue manager | More database work per cycle; multi-instance correctness needs #1251 |
| **C) A distributed lock per key (`IDistributedLockProvider`) around in-process channels** | Reuses existing locks | One lock operation per key; needs a lock provider; still not durable |

### Chosen Option: **B — ordering in the outbox processor** (recommended, pending the maintainer)

### Rationale

- Option B wins for multi-instance or durable needs, which SPEC-002 states (the outbox is the durable record, ordering survives restarts).
- Option A wins for a single-instance host that wants no database cost; Option C wins when row claiming is not available yet. With B, the guarantee is already correct for one instance, and complete for several once #1251 lands.
- Consequences if B is chosen: the issue's own Dilemma D2 (back-pressure at `MaxMessagesPerPartition`) and D3 (`IPartitionStore`) disappear, because no in-memory queue exists; `IPartitionedQueueManager`, `PartitionedMessagingOptions.MaxMessagesPerPartition`, `PartitionIdleTimeout`, `EnableDurability` and `PartitionedMessageBehavior` are not built. If the maintainer picks A or C instead, this plan is reopened for those phases.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. A total order inside a key — timestamps are not unique (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) Order by `(CreatedAtUtc, Id)`** | No change to the write path | `Id` is a random Guid, so rows with equal timestamps (same transaction, fast loop, `FakeTimeProvider`) are ordered arbitrarily, which breaks S3 |
| **B) The write path makes `CreatedAtUtc` strictly increasing per process (`max(now, last + 1 tick)`), order by `(CreatedAtUtc, Id)`** | No schema change; a single internal helper used by the three write paths; deterministic in tests | Two instances writing the same key concurrently can still interleave by clock skew; the guarantee is "creation order as seen by one writer" |
| **C) A database sequence column (`IDENTITY`, `BIGSERIAL`, `AUTO_INCREMENT`, a counter collection on MongoDB)** | True total order, also across instances | A different mechanism on each provider and one more schema change after #1254; Mongo needs a counter collection |

### Chosen Option: **B — strictly increasing creation time per writer process** (recommended, pending the maintainer)

### Rationale

- Option B wins when one logical producer writes a key (an appointment is handled by one request at a time), which matches S3.
- Option C wins when several instances append to the same key concurrently and their relative order matters; it is the right follow-up if that case is real.
- The helper (`OutboxTimestamp.NextUtc(TimeProvider)`) stays in `Encina.Messaging`; time still comes from `TimeProvider` (AGENTS.md section 3).

**Class:** Dilemma

</details>

<details>
<summary><strong>3. A message that exhausts its retries at the head of a partition (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) Hold: the key stays blocked until an operator requeues or discards the exhausted message** | Preserves order strictly, as AC-039 requires ("rejection of record N holds later records"); `RequeueExhaustedAsync` already exists | One poison message stops its key (not other keys) until handled; needs the stalled-partition health signal |
| **B) Skip: dead-letter the exhausted message and continue with the next** | Progress never stops | Later messages overtake a failed one; order is lost silently |
| **C) Configurable per key or message type (`Hold` or `Skip`)** | Fits both kinds of data | Two behaviors to test on 10 providers; a setting nobody asked for |

### Chosen Option: **A — hold until an operator acts, and report stalled partitions** (recommended, pending the maintainer)

### Rationale

- Option A wins when order is part of correctness (a cancel delivered before its booking is wrong), which is what the reference scenarios are.
- Option B wins for feeds where the latest message supersedes earlier ones; choose C only if that need appears.
- Never reporting success for a failed message is also the background-infrastructure rule of AGENTS.md section 3.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Where pacing state lives — in the store or in the process (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) In-process `last dispatched` time per key** | No query cost | Lost at restart: a restart can burst past a limit that an external service enforces (the S9 case); per instance with several instances |
| **B) Derived from the store: a key is eligible only if no message of that key has `ProcessedAtUtc` newer than `now - MinDispatchInterval`** | Exact across restarts and instances; no new state | One extra predicate in the ordered fetch; may want an index on `(PartitionKey, ProcessedAtUtc)` if the benchmark shows a cost |
| **C) A per-partition state table or collection** | Fast, explicit | A new table on 10 providers (AGENTS.md section 5), which the other options avoid |

### Chosen Option: **B — pacing derived from the store** (recommended, pending the maintainer)

### Rationale

- Option B wins when the external limit must hold across restarts (rate limits are hard limits in S9).
- Option A wins when pacing is only a courtesy; Option C wins when the predicate becomes too slow, and can be added later behind the same options.
- A server-directed wait needs no new state: the failure path already stores it as `NextRetryAtUtc`, and the head-of-line rule blocks the key until then.
- Options: `OutboxPartitionOptions.MinDispatchInterval` (default zero, no pacing).

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Shape of the ordered fetch — a window-function prefix per key or a head-only fetch with chained re-fetch (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) One query returns, for every key whose first unprocessed message is eligible, its first `MaxPerPartition` unprocessed messages (`ROW_NUMBER() OVER (PARTITION BY PartitionKey ORDER BY CreatedAtUtc, Id)`)** | A long run of one key is delivered in one cycle; few round trips | Window functions are not expressible in EF Core LINQ, so the EF store uses provider-specific `FromSql` (3 strings, composed with `Take`); MySQL needs 8.0 or later; MongoDB uses `$setWindowFields` or `$group` with `$push` and `$slice` |
| **B) A head-only query (`NOT EXISTS` earlier unprocessed message of the same key) and a re-fetch of the next message of a key after each success** | Simple SQL, translatable in EF Core | One query per message in a run; a key of 100 messages costs 100 round trips |
| **C) Head only, one message per key per cycle** | Simplest | With the 30-second default `ProcessingInterval`, 100 messages for one key take 50 minutes |

### Chosen Option: **A — window-function prefix per key** (recommended, pending the maintainer)

### Rationale

- Option A wins when keys have runs of messages (a booking, three reschedules and a cancel), which is the S3 shape.
- Option B wins when the team wants no raw SQL in the EF store; it is acceptable only with a short `ProcessingInterval`.
- The eligibility rule is the same in all options: the key's first unprocessed message must be pending, not exhausted (`RetryCount < MaxRetries`) and past its `NextRetryAtUtc`; later messages of the key are returned behind it. Messages without a key keep today's query and ordering.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Priority and order together; opt-in default; interfaces for the common keys</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) Inside a key the order is creation order and ignores priority; across keys #123 priority orders the keys (by their head message); partitioning is off by default (`OutboxPartitionOptions.Enabled = false`); `ISagaPartitionedMessage`, `ITenantPartitionedMessage` and `IAggregatePartitionedMessage` are thin interfaces over `IPartitionedMessage` (owned by #1254)** | Satisfies S3 (order per appointment) and keeps priority meaningful between appointments; matches the issue's acceptance list | Priority cannot overtake inside a key by design |
| **B) Priority wins inside a key too** | Critical messages jump the line | Breaks per-key order, which is the whole feature |

### Chosen Option: **A — order dominates priority inside a key; opt-in; thin key interfaces**

### Rationale

- Order is the stated requirement (REQ-042, S3); a priority that reorders a key defeats it.
- Every messaging pattern is optional and disabled by default (AGENTS.md section 3, ADR-050). With the option off, messages that have a key are processed exactly as today.
- Per tenant (REQ-061, AC-043): until `TenantId` exists on the outbox (#737), a helper `PartitionKeys.ForTenant(tenantId, key)` composes `tenantId:key` and rejects a tenant id containing `:`, so two tenants never share a partition; `ITenantPartitionedMessage` uses it.

**Class:** Settled
**Settled by:** SPEC-002 REQ-042 and AC-039 (scenario S3: per-key order), REQ-061 and AC-043 (per tenant); AGENTS.md section 3 (every messaging pattern optional and disabled by default; ADR-050).

</details>

<details>
<summary><strong>7. Parallelism across keys</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) The processor groups the fetched messages by key and runs the groups with `Parallel.ForEachAsync` (`MaxDegreeOfParallelism = OutboxPartitionOptions.MaxConcurrentPartitions`, default `Environment.ProcessorCount`); messages without a key stay in one serial group** | Different keys run in parallel as required; today's serial behavior is preserved for keyless messages; each group stops at its first failure | Store calls from several tasks need a store that is safe for concurrent use per call (scoped connection per call) |
| **B) Keep the serial loop and rely on short cycles** | No concurrency | Misses "different partitions run in parallel" |

### Chosen Option: **A — grouped, bounded parallelism**

### Rationale

- The requirement is parallelism across keys; the default follows the issue's `MaxConcurrentPartitions = Environment.ProcessorCount`.
- Each group (a key's run) processes sequentially and stops at its first failure: the failed message is marked failed with its `NextRetryAtUtc`, the rest of the run stays unprocessed and is blocked by the head rule on the next fetch.

**Class:** Trivial
**Settled by:** issue #469 ("Messages with same PartitionKey process sequentially within partition; Different partitions process in parallel") and `OutboxOptions.BatchSize` / `MaxRetries` semantics in `src/Encina.Messaging/Outbox/OutboxOptions.cs`.

</details>

---

## Implementation Phases

Seven pull requests of at most 15 files and 1500 lines: PR-A phases 1 and 2 (core, processor, fakes), PR-B phase 3 (ADO.NET), PR-C phase 4 (Dapper), PR-D phase 5 (EF Core and MongoDB), PR-E phase 6 (observability and health), PR-F phase 7 (tests and scenarios), PR-G phase 8 (docs and ADR). Requires #1254 and #123 merged; #1251 is needed for several instances (single-instance behavior is complete without it).

### Phase 1: Core Model, Options and Contracts

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Messaging/Outbox/ISagaPartitionedMessage.cs`, `ITenantPartitionedMessage.cs`, `IAggregatePartitionedMessage.cs` (default interface members over `IPartitionedMessage.PartitionKey` from #1254); `PartitionKeys.cs` with `ForTenant`.
2. `OutboxPartitionOptions.cs` on `OutboxOptions.Partitioning`: `Enabled` (false), `MaxConcurrentPartitions`, `MaxPerPartition` (batch prefix, default 50), `MinDispatchInterval` (zero); validated in `OutboxOptions.Validate`.
3. `OutboxFetchCriteria` (from #123): add `PartitionOrdered`, `MaxPerPartition`, `PacingCutoffUtc`.
4. `IOutboxStore.GetStalledPartitionCountAsync(int maxRetries, CancellationToken)`: number of keys whose first unprocessed message is exhausted.
5. `OutboxTimestamp` (internal): strictly increasing `NextUtc(TimeProvider)`; used by `OutboxWriter`, `OutboxOrchestrator.AddAsync`, `TransactionalOutboxPipelineBehavior` (choice 2).
6. `PublicAPI.Unshipped.txt` through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Plan docs/plans/partitioned-sequential-messaging-implementation-plan-469.md. #1254 added IOutboxMessage.PartitionKey and IPartitionedMessage; #123 added OutboxFetchCriteria and MessagePriority. The outbox write paths stamp CreatedAtUtc from TimeProvider, which can repeat.
TASK: Implement Phase 1 Tasks 1 to 5.
KEY RULES: Either<EncinaError, T> and Left for invalid input (ADR-001, ADR-006); time only from TimeProvider (AGENTS.md section 3); options validated in OutboxOptions.Validate; no [Obsolete]; XML docs on every public member; the key value is never logged or used as a metric dimension.
REFERENCE FILES: src/Encina.Messaging/Outbox/OutboxOptions.cs, OutboxWriter.cs, OutboxOrchestrator.cs, TransactionalOutboxPipelineBehavior.cs, IOutboxStore.cs.
```

</details>

### Phase 2: Batch Processor — Grouping, Stop-on-Failure, Parallel Keys

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs`: build the partition criteria (cutoff for pacing = now minus `MinDispatchInterval`); group the fetched messages by `PartitionKey` (keyless messages in one serial group); `Parallel.ForEachAsync` over key groups bounded by `MaxConcurrentPartitions`; inside a group stop at the first message whose outcome is not `Success` (failed, exhausted, store error, cancelled).
2. Keep `OutboxBatchResult` counters; add `HeldPartitions` (keys stopped by a failure in this cycle).
3. Cancellation: a cancelled group stops cleanly; no exception escapes the loop (AGENTS.md section 3).
4. `src/Encina.Messaging/Outbox/OutboxProcessorBase.cs` is unchanged except for the result type if needed.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: OutboxBatchProcessor.ProcessAsync fetches then runs a serial foreach and continues after a failed message (src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs). A failed message is marked with RetryCount/NextRetryAtUtc by FailAsync.
TASK: Implement Phase 2 Tasks 1 to 3. Messages without a key keep today's serial behavior and ordering exactly.
KEY RULES: a Left from a store or from publishing fails the operation and stops that key's run, never reports success; per-key sequential, cross-key parallel; no exception escapes a background loop; time from TimeProvider; CRAP at most 10 on changed methods, so split ProcessAsync into small methods.
REFERENCE FILES: src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs, OutboxProcessorBase.cs, OutboxRetryBackoff.cs.
```

</details>

### Phase 3: ADO.NET Providers (SqlServer, PostgreSQL, MySQL)

<details>
<summary><strong>Tasks</strong></summary>

For each ADO.NET package, in `Outbox/OutboxStoreADO.cs`:

1. The partition-ordered branch of `GetPendingMessagesAsync(criteria)`: a CTE with `ROW_NUMBER() OVER (PARTITION BY PartitionKey ORDER BY CreatedAtUtc, Id)` and `FIRST_VALUE` of the head-eligibility flag over unprocessed rows of keyed messages, `WHERE rn <= @MaxPerPartition AND head_ok = 1`, the pacing predicate (`NOT EXISTS` a message of the key with `ProcessedAtUtc > @PacingCutoffUtc` when pacing is on), unioned with the unchanged keyless query, limited to `@BatchSize` (`TOP (@n)` on SQL Server, `LIMIT @n` on PostgreSQL and MySQL 8.0 or later).
2. `GetStalledPartitionCountAsync`: count of keys whose first unprocessed message has `RetryCount >= @MaxRetries`.
3. Index review: `(PartitionKey, CreatedAtUtc)` from #1254 serves the window; add `(PartitionKey, ProcessedAtUtc)` only if the benchmark requires it.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: ADO stores write SQL by hand. Window functions need SQL Server 2012+, PostgreSQL 8.4+, MySQL 8.0+. The existing keyless query is the one in OutboxStoreADO.GetPendingMessagesAsync.
TASK: Implement Phase 3 Tasks 1 and 2 for the three ADO.NET packages, one package per commit, with identical semantics.
KEY RULES: async with CancellationToken only; parameters, never concatenated values; the criteria record carries the clock values, stores never read the clock; keep the keyless query byte-for-byte equivalent in behavior; identifiers follow each provider's quoting (brackets, lower-case, backticks).
REFERENCE FILES: src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs, src/Encina.ADO.PostgreSQL/Outbox/OutboxStoreADO.cs, src/Encina.ADO.MySQL/Outbox/OutboxStoreADO.cs.
```

</details>

### Phase 4: Dapper Providers (SqlServer, PostgreSQL, MySQL)

<details>
<summary><strong>Tasks</strong></summary>

For each Dapper package, in `Outbox/OutboxStoreDapper.cs`: the same partition-ordered query and stalled count as Phase 3, using `CommandDefinition` with the `CancellationToken` and the `SELECT *`-style mapping to the provider `OutboxMessage`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT: Dapper stores use CommandDefinition and map rows to the provider OutboxMessage class.
TASK: Apply Phase 4 to the three Dapper packages, mirroring Phase 3 so both families return identical sequences for identical data.
KEY RULES: same as Phase 3; DbLease and transaction accessor usage unchanged.
REFERENCE FILES: src/Encina.Dapper.SqlServer/Outbox/OutboxStoreDapper.cs, src/Encina.Dapper.PostgreSQL/Outbox/OutboxStoreDapper.cs, src/Encina.Dapper.MySQL/Outbox/OutboxStoreDapper.cs.
```

</details>

### Phase 5: EF Core (3 providers) and MongoDB

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.EntityFrameworkCore/Outbox/OutboxStoreEF.cs`: the partition-ordered branch uses `FromSql` with one SQL string per `Database.ProviderName` (SqlServer, PostgreSQL, MySQL), composed with `OrderBy` and `Take(BatchSize)`; the keyless branch stays LINQ. `GetStalledPartitionCountAsync` the same way.
2. `src/Encina.MongoDB/Outbox/OutboxStoreMongoDB.cs`: an aggregation (`$match` unprocessed keyed, `$sort` by `createdAtUtc` and `_id`, `$group` by `partitionKey` with `$push` and `$slice` to `MaxPerPartition`, head-eligibility filter, pacing `$lookup` or a second query) merged with the keyless query; `GetStalledPartitionCountAsync`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT: One EF Core store serves three providers, so raw SQL needs a string per provider; MongoDB expresses the per-key prefix with an aggregation pipeline.
TASK: Implement Phase 5 Tasks 1 and 2 and prove both on every provider with the shared contract test of Phase 7.
KEY RULES: async with CancellationToken; no provider-specific code in abstractions; parameters, never concatenation; the result sequence must equal the ADO.NET and Dapper result for the same data.
REFERENCE FILES: src/Encina.EntityFrameworkCore/Outbox/OutboxStoreEF.cs, src/Encina.MongoDB/Outbox/OutboxStoreMongoDB.cs.
```

</details>

### Phase 6: Observability, Health and Cross-Cutting Integration

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina/Diagnostics/EventIdRanges.cs`: register `MessagingPartitioning = (6735, 6747)` from the family block; add it to the `Encina.Messaging` entry in `EncinaEventIdAllocationTests`.
2. `src/Encina.Messaging/Diagnostics/PartitioningLogMessages.cs` (`[LoggerMessage]`, 6735 to 6742): partitioning enabled at start-up; partition held after a failure (code and count, never the key); stalled partition detected; pacing delayed a partition; cycle summary (partitions processed, held); options rejected.
3. `OutboxProcessorMetrics.cs`: `encina.outbox.partition.active` (gauge, partitions processed in the last cycle), `encina.outbox.partition.held` (counter), `encina.outbox.partition.paced` (counter), `encina.outbox.partition.wait_time` (histogram, seconds from creation to dispatch).
4. `src/Encina.Messaging/Health/OutboxHealthCheck.cs`: `Degraded` when `GetStalledPartitionCountAsync` is greater than zero (key count only, never the keys).
5. `src/Encina.OpenTelemetry/MessagingStores/InstrumentedOutboxStore.cs`: the new store method.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT: ADR-021: register the range first (6735-6747 is assigned to this plan by the batch registry). The health check follows the IEncinaHealthCheck pattern (DefaultName const, Tags, scoped resolution).
TASK: Implement Phase 6 Tasks 1 to 5, packing EventIds 6735-6742 sequentially.
KEY RULES: [LoggerMessage] source generator; the partition key can identify a data subject so it is never a log argument, tag or metric dimension; EncinaError.Message never reaches logs or health results; run the EventId architecture tests.
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs, src/Encina.Messaging/Health/OutboxHealthCheck.cs, src/Encina.Messaging/Diagnostics/OutboxProcessorMetrics.cs.
```

</details>

### Phase 7: Testing

<details>
<summary><strong>Tasks</strong></summary>

1. Unit: grouping and stop-on-failure with `FakeTimeProvider` (failed N holds N+1; other keys continue), pacing eligibility, `OutboxTimestamp` monotonicity, `PartitionKeys.ForTenant`, options validation, health check states.
2. Guard: new public members.
3. Contract: one ordered-fetch contract run against the fake store and all 10 real stores: same sequence for the same data, head blocked by retry delay or exhaustion, keyless messages unchanged, prefix size, pacing.
4. Property (FsCheck): for any interleaving of keys and failures, the delivered sequence of each key is a prefix of its creation order and different keys interleave freely.
5. Scenarios as integration tests on all 10 providers in the shared collections: S3 (book, reschedule, cancel delivered in order per appointment) and S9 (rejection of record N holds later records; a server-directed wait is honoured; pacing interval respected), two tenants with the same key value never share a partition (AC-043).
6. Load test (required, concurrent feature): many keys with a bounded `MaxConcurrentPartitions`; benchmark of the ordered fetch against the plain fetch (decides the `(PartitionKey, ProcessedAtUtc)` index of choice 4).
7. Coverage: per-file targets in `Encina.Messaging.json` and the provider manifests (`mechanical-fixer`); CRAP at most 10 on changed methods.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT: AGENTS.md section 9: per-flag coverage, tests execute real code, Shouldly through Encina.Testing.Shouldly, shared [Collection("<Family>-<Database>")] fixtures, ClearAllDataAsync in InitializeAsync, load tests run locally with DOTNET_JitObjectStackAllocationConditionalEscape=0.
TASK: Write the tests of Phase 7 Tasks 1 to 6 and report per-flag coverage against the manifest.
KEY RULES: deterministic (FakeTimeProvider, no Thread.Sleep); all 10 providers; never a .md for database integration tests; SQLite excluded.
REFERENCE FILES: tests/Encina.IntegrationTests/Dapper/SqlServer/Outbox/OutboxStoreDapperTests.cs, tests/Encina.IntegrationTests/Messaging/Outbox/OutboxRetryScenarios.cs, tests/Encina.IntegrationTests/Infrastructure/MongoDB/Stores/OutboxStoreMongoDBIntegrationTests.cs.
```

</details>

### Phase 8: Documentation and Finalization

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs; `PublicAPI.Unshipped.txt` per package; `changelog.d/469-partitioned-outbox.added.md` (`mechanical-fixer`).
2. `docs-writer`: guide "Ordered delivery per key" (ordering guarantee and its limits: one writer per key, one instance until #1251, hold semantics, operator requeue, pacing, per-tenant keys); package README; `docs/INVENTORY.md`.
3. ADR for the ordering mechanism (choice 1, with choices 2 to 5); ADR number assigned when the ADR pull request opens.
4. `dotnet build --configuration Release` with zero warnings; `dotnet test`; every flag at its target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```text
CONTEXT: documentation, changelog fragments and PublicAPI lines have path owners (docs-writer, mechanical-fixer).
TASK: Finish Phase 8 Tasks 1 to 4 after Phases 1 to 7 are merged.
KEY RULES: English only; coverage figures only through covref markers; write "the reference application", never a private client name; state the guarantee's limits plainly.
REFERENCE FILES: changelog.d/README.md, docs/architecture/adr/index.md, docs/specifications/SPEC-002 section 5.11, docs/plans/dsr-implementation-plan-404.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard or source | Relevance |
| --- | --- |
| SPEC-002 REQ-042, AC-039 (S3, S9), REQ-061, AC-043, DEC-005 (a) | The requirement, its acceptance scenarios and the per-tenant rule |
| Enterprise Integration Patterns: Resequencer, Message Sequence | Per-key ordered delivery |
| Wolverine 5.0 partitioned sequential messaging | Inspiration cited by the issue (in-process); this plan persists the guarantee in the outbox |
| AGENTS.md sections 3, 5, 6, 7, 9 | Background-error rule, 10 providers, cross-cutting check, EventIds, load tests |
| ADR-001, ADR-006, ADR-018, ADR-021, ADR-050 | ROP, cross-cutting check, EventIds, module dependencies |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
| --- | --- | --- |
| `OutboxBatchProcessor`, `OutboxProcessorBase` | `src/Encina.Messaging/Outbox/` | Grouping, stop-on-failure, parallel keys |
| `OutboxOptions.MaxRetryAfter`, `OutboxRetryBackoff` | `src/Encina.Messaging/Outbox/` | Server-directed wait already stored as `NextRetryAtUtc` |
| `IOutboxStore.RequeueExhaustedAsync` | `src/Encina.Messaging/Outbox/IOutboxStore.cs` | Operator release of a held partition |
| `PartitionKey`, `IPartitionedMessage` (#1254), `OutboxFetchCriteria` (#123) | `src/Encina.Messaging/Outbox/` | The column and the fetch contract |
| `OutboxHealthCheck` | `src/Encina.Messaging/Health/OutboxHealthCheck.cs` | Stalled partitions |
| `OutboxProcessorMetrics`, `OutboxActivitySource` | `src/Encina.Messaging/Diagnostics/` | Shared `"Encina"` meter and source |
| Provider outbox stores | `src/Encina.ADO.*`, `Encina.Dapper.*`, `Encina.EntityFrameworkCore`, `Encina.MongoDB` | Ordered fetch and stalled count |
| `DeadLetterSourceCapture` | `src/Encina.Messaging/DeadLetter/` | Existing capture of exhausted messages, unchanged |

### Event ID Allocation

| Package | Range | Notes |
| --- | --- | --- |
| `Encina.Messaging` (`EventIdRanges.MessagingPartitioning`, new) | 6735-6747 | From the family block 6700-6749; this plan uses 6735-6742. Siblings: #123 6700-6714, #301 6715-6724, #251 6725-6734, #1254 6748-6749 |

### Estimated File Count

| Category | Files | Notes |
| --- | --- | --- |
| Core new and modified | 14 | Interfaces, options, helpers, criteria, store contract, processor, writer paths, diagnostics, health, `EventIdRanges.cs` |
| ADO.NET (3 providers) | 3 | Store only; schema comes from #1254 |
| Dapper (3 providers) | 3 | Store only |
| EF Core and MongoDB | 2 | Store only |
| Testing packages and OpenTelemetry | 4 | Fake store, builder, helper, instrumented store |
| Tests | 28 | Unit, guard, contract, property, 10 integration, load, benchmark |
| Docs, changelog, PublicAPI | 12 | ADR, guide, README, fragment, PublicAPI per package |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined Prompt — All Phases</strong></summary>

```text
PROJECT CONTEXT: Encina, pre-1.0, .NET 10 / C# 14. Issue #469 (SPEC-002 REQ-042, AC-039 S3 and S9), plan docs/plans/partitioned-sequential-messaging-implementation-plan-469.md. It needs #1254 (PartitionKey column, IPartitionedMessage) and #123 (OutboxFetchCriteria) merged; row claiming for several instances is #1251. Read AGENTS.md first.

IMPLEMENTATION OVERVIEW: (1) Key interfaces, OutboxPartitionOptions, criteria extension, stalled-count store method, strictly increasing creation time. (2) OutboxBatchProcessor: group by key, parallel across keys, stop at the first failure of a key. (3) ADO.NET x3 window-function fetch. (4) Dapper x3. (5) EF Core per-provider FromSql and MongoDB aggregation. (6) EventIds 6735-6742, metrics, health. (7) Tests including S3, S9, per-tenant, load. (8) Docs and ADR. Seven pull requests of at most 15 files and 1500 lines.

KEY PATTERNS: Either<EncinaError, T>; a failed message holds the later messages of its key and is never reported as success; order dominates priority inside a key; time from TimeProvider, stores never read the clock; opt-in via OutboxPartitionOptions.Enabled = false; keyless messages behave exactly as today; the partition key is never logged or a metric dimension; [LoggerMessage] with a registered range; async database calls with CancellationToken; shared [Collection] fixtures.

REFERENCE FILES: src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs, OutboxOptions.cs, OutboxWriter.cs, src/Encina.Dapper.SqlServer/Outbox/OutboxStoreDapper.cs, src/Encina.EntityFrameworkCore/Outbox/OutboxStoreEF.cs, src/Encina.MongoDB/Outbox/OutboxStoreMongoDB.cs, src/Encina.Messaging/Health/OutboxHealthCheck.cs, src/Encina/Diagnostics/EventIdRanges.cs, docs/specifications/SPEC-002 section 5.11.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Message sequencing, not data reads |
| 2 | OpenTelemetry | ✅ | `encina.outbox.partition.*` gauge, counters and histogram on the shared meter (Phase 6); never the key value |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` 6735-6742 in `EventIdRanges.MessagingPartitioning` (Phase 6) |
| 4 | Health Checks | ✅ | `OutboxHealthCheck` reports `Degraded` for stalled partitions (Phase 6) |
| 5 | Validation | ✅ | `OutboxPartitionOptions` validated in `OutboxOptions.Validate`; tenant id with `:` rejected in `PartitionKeys.ForTenant` |
| 6 | Resilience | ✅ | Retry, backoff and dead-letter of a held partition follow the existing outbox policy (`OutboxRetryBackoff`, `MaxRetryAfter`); no new retry mechanism |
| 7 | Distributed Locks | ⏭️ | Multi-instance ordering needs row claiming, issue #1251; the lock-per-key option (choice 1, C) is not chosen |
| 8 | Transactions | ✅ | The key is written in the outbox transaction (#1254); the processor marks outcomes through the existing store methods |
| 9 | Idempotency | ⏭️ | Duplicate handling inside a partition is #735, #148 and #1220 |
| 10 | Multi-Tenancy | ✅ | Partitions and pacing are per tenant via `PartitionKeys.ForTenant` now (REQ-061); a real `TenantId` column is #737 |
| 11 | Module Isolation | ⏭️ | Module-scoped partitions follow `ModuleId` on messaging entities, issue #747 |
| 12 | Audit Trail | ❌ | Ordering decisions are operational; held and released partitions are logged by code and count, and no personal data is created |

---

## Prerequisites & Dependencies

- #1254 (column and `IPartitionedMessage`) and #123 (`OutboxFetchCriteria`) merged before PR-A.
- #1251 (row claiming) for ordering guarantees across several instances; the plan is complete and correct for one instance without it.
- #737 (`TenantId` on `OutboxMessage`) replaces the composed tenant key; window functions need MySQL 8.0 or later (confirm the MySQL image tag used by the test fixtures at implementation).
- Operators need the existing requeue path (`RequeueExhaustedAsync`, `Encina.Cli` messaging commands, issue #2610) to release a held partition.

## Next Steps

1. Maintainer answers the Dilemmas, design choices 1 to 5 (the issue's D1, D2 and D3 collapse into choice 1), when work starts; answers go to `## Maintainer Decisions`.
2. Worker briefs per pull request (PR-A to PR-G) after #1254 and #123 merge.
3. Orchestrator updates #469 to the scope above and notes that `IPartitionedQueueManager` and `IPartitionStore` are dropped under choice 1, option B.
