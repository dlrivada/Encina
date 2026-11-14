# Implementation Plan: Partitioned Sequential Messaging (ordered outbox partitions with pacing) (`Encina.Messaging` + 10 database providers)

> **Issue**: [#469](https://github.com/dlrivada/Encina/issues/469)
> **Type**: Feature (SPEC-002 REQ-042, AC-039 scenarios S3 and S9, AC-043; tracked by #1186)
> **Complexity**: High (9 phases, 10 database providers, 101 files, delivered in 9 pull requests that each compile on their own)
> **Estimated Scope**: about 1,500 lines of production code + about 2,600 lines of tests, documentation and manifests; no pull request over 1,000 changed lines
> **Family**: message priority and partitioning (#123, #251, #301, #469, #1254). This plan builds on `IOutboxMessage.PartitionKey` and `IPartitionedMessage` (owned by #1254) and on `OutboxFetchCriteria` (owned by #123). It merges after both.

---

## Summary

Messages that carry the same partition key are delivered **one after another in creation order**; messages of different keys run in parallel; a failed message **holds** the later messages of its key until it succeeds or an operator intervenes; and a per-key **pacing** interval limits how fast one key is dispatched. This is the ordered-outbox-partition capability of SPEC-002 REQ-042, accepted by scenario S3 (book, reschedule and cancel delivered in order per appointment) and S9 (pacing; rejection of record N holds later records; a server-directed wait is honoured), per tenant (REQ-061, AC-043).

The issue as written proposes in-process `System.Threading.Channels` queues (`IPartitionedQueueManager`, `PartitionedMessagingOptions`, `PartitionedMessageBehavior`, optional `IPartitionStore`). None of these exists in `src/`, and the maintainer comment and SPEC-002 build the guarantee on the outbox `PartitionKey` column instead. This plan therefore turns the feature into an ordered fetch and a smarter batch processor on the outbox, and drops the in-process artifacts unless the maintainer picks a different mechanism (choice 1).

Facts verified on `origin/main` (2026-11-13) that shape the design:

- `OutboxBatchProcessor.ProcessAsync` fetches `GetPendingMessagesAsync(batchSize, maxRetries, cancellationToken)` and processes the returned messages in a serial `foreach`; a failed message gets `RetryCount` and `NextRetryAtUtc` and the loop moves on to the next one, so a failure today lets later messages of the same entity overtake it. `OutboxFetchCriteria` does not exist yet: it is a #123 deliverable and a prerequisite of this plan.
- `OutboxBatchProcessor` holds one `IOutboxStore` resolved from a scope created per cycle by `OutboxProcessorBase`; outcomes are committed afterwards with `IOutboxStore.SaveChangesAsync`. `OutboxStoreEF` holds one `DbContext` (not thread-safe) and the ADO.NET and Dapper stores share one scoped connection, so concurrent store calls on one instance are unsafe (choice 10).
- `IOutboxStore` has 10 implementers on main: `OutboxStoreADO` x3, `OutboxStoreDapper` x3, `OutboxStoreEF`, `OutboxStoreMongoDB`, `InstrumentedOutboxStore` (`Encina.OpenTelemetry`, internal) and `FakeOutboxStore` (`Encina.Testing.Fakes`). No test class implements it (`DeadLetterSourcesEndToEndScenario` only resolves it).
- Stores order by `CreatedAtUtc` only. `OutboxWriter` creates one message per notification with `_timeProvider.GetUtcNow().UtcDateTime`, so messages written in one transaction can carry equal timestamps (always, under `FakeTimeProvider`), and `Id` is `Guid.NewGuid()` (random). Creation order is therefore not a total order today (choice 2).
- A server-directed wait already exists: `OutboxOptions.MaxRetryAfter` and `OutboxRetryBackoff.ComputeDelay` produce the `NextRetryAtUtc` after a failure. `OutboxOptions.BatchSize` defaults to 100.
- `RequeueExhaustedAsync` resets exhausted messages (`RetryCount >= MaxRetries`, not processed); it is the only operator tool today for releasing a held partition (there is no discard operation; see choice 3).
- `OutboxHealthCheck` exists in `src/Encina.Messaging/Health/`; the outbox has no `TenantId` yet (#737). The metric prefix `encina.outbox.*` is the one `OutboxProcessorMetrics` uses (`encina.outbox.enqueued`, `encina.outbox.messages_exhausted`, ...).
- The test containers default to `mysql:8.4` (`src/Encina.Testing.Testcontainers/ContainerDefaultImages.cs`).
- ADR 062 (error category contract) is merged (PR #3599): any new error code is created with `EncinaErrors.Create(code, ErrorCategory, message)`. This plan adds none.

- **Affected packages**: `Encina.Messaging`, `Encina`, `Encina.ADO.*` (3), `Encina.Dapper.*` (3), `Encina.EntityFrameworkCore`, `Encina.MongoDB`, `Encina.OpenTelemetry`, `Encina.Testing.Fakes`.
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
| **B) The write path makes `CreatedAtUtc` strictly increasing per process (`max(now, last + 1 tick)`), order by `(CreatedAtUtc, Id)`** | No schema change; a single internal helper used by the two write paths (`OutboxWriter`, `OutboxOrchestrator.AddAsync`); deterministic in tests | Two instances writing the same key concurrently can still interleave by clock skew; the guarantee is "creation order as seen by one writer" |
| **C) A database sequence column (`IDENTITY`, `BIGSERIAL`, `AUTO_INCREMENT`, a counter collection on MongoDB)** | True total order, also across instances | A different mechanism on each provider and one more schema change after #1254; Mongo needs a counter collection |

### Chosen Option: **B — strictly increasing creation time per writer process** (recommended, pending the maintainer)

### Rationale

- Option B wins when one logical producer writes a key (an appointment is handled by one request at a time), which matches S3.
- Option C wins when several instances append to the same key concurrently and their relative order matters; it is the right follow-up if that case is real.
- The helper (`OutboxTimestamp.NextUtc(TimeProvider)`) stays in `Encina.Messaging`; time still comes from `TimeProvider` (AGENTS.md section 3). `TransactionalOutboxPipelineBehavior` builds an `OutboxWriter`, so it is covered through the writer.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. A message that exhausts its retries at the head of a partition (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) Hold: the key stays blocked until an operator requeues the exhausted message (`RequeueExhaustedAsync`, the only release path that exists today)** | Preserves order strictly, as AC-039 requires ("rejection of record N holds later records"); needs the stalled-partition health signal | One poison message stops its key (not other keys) until handled; an operator cannot drop it, only requeue it (a discard operation is a follow-up, below) |
| **B) Skip: dead-letter the exhausted message and continue with the next** | Progress never stops | Later messages overtake a failed one; order is lost silently |
| **C) Configurable per key or message type (`Hold` or `Skip`)** | Fits both kinds of data | Two behaviors to test on 10 providers; a setting nobody asked for |

### Chosen Option: **A — hold until an operator requeues, and report stalled partitions** (recommended, pending the maintainer)

### Rationale

- Option A wins when order is part of correctness (a cancel delivered before its booking is wrong), which is what the reference scenarios are.
- Option B wins for feeds where the latest message supersedes earlier ones; choose C only if that need appears.
- Never reporting success for a failed message is also the background-infrastructure rule of AGENTS.md section 3.
- A held partition stays held whether or not the health check is registered: the health check only reports it (choice 11). An operator discard of a held head, and the audit of hold and release actions, are not built here: follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/partition-operator-discard-and-audit.md).

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
- Options: `OutboxPartitionOptions.MinDispatchInterval` (default zero, no pacing; choice 11).

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Shape of the ordered fetch — a window-function prefix per key or a head-only fetch with chained re-fetch (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) One query returns, for every key whose first unprocessed message is eligible, its first `MaxPerPartition` unprocessed messages (`ROW_NUMBER() OVER (PARTITION BY PartitionKey ORDER BY CreatedAtUtc, Id)`)** | A long run of one key is delivered in one cycle; few round trips | Window functions are not expressible in EF Core LINQ, so the EF store uses provider-specific `FromSql` (3 strings, composed with `Take`); MySQL needs 8.0 or later (the test fixtures run `mysql:8.4`); MongoDB uses `$setWindowFields` or `$group` with `$push` and `$slice` |
| **B) A head-only query (`NOT EXISTS` earlier unprocessed message of the same key) and a re-fetch of the next message of a key after each success** | Simple SQL, translatable in EF Core; no MySQL 8.0 floor | One query per message in a run; a key of 100 messages costs 100 round trips |
| **C) Head only, one message per key per cycle** | Simplest | With the 30-second default `ProcessingInterval`, 100 messages for one key take 50 minutes |

### Chosen Option: **A — window-function prefix per key** (recommended, pending the maintainer)

### Rationale

- Option A wins when keys have runs of messages (a booking, three reschedules and a cancel), which is the S3 shape.
- Option B wins when the team wants no raw SQL in the EF store or must support MySQL 5.7; it is acceptable only with a short `ProcessingInterval`.
- The eligibility rule is the same in all options: the key's first unprocessed message must be pending, not exhausted (`RetryCount < MaxRetries`) and past its `NextRetryAtUtc`; later messages of the key are returned behind it. A head whose `NextRetryAtUtc` is in the future therefore blocks its key until that time (this is the server-directed wait of S9). Messages without a key keep today's query and ordering.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Order dominates priority inside a key</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) Inside a key the order is creation order and ignores priority; across keys the #123 priority orders the keys (by their head message)** | Satisfies S3 (order per appointment) and keeps priority meaningful between appointments | Priority cannot overtake inside a key by design |
| **B) Priority wins inside a key too** | Critical messages jump the line | Breaks per-key order, which is the whole feature |

### Chosen Option: **A — order dominates priority inside a key**

### Rationale

- Order is the stated requirement (REQ-042, S3); a priority that reorders a key defeats it, so option B does not satisfy the cited scenario.

**Class:** Settled
**Settled by:** SPEC-002 REQ-042 and AC-039 (scenario S3: per-key order).

</details>

<details>
<summary><strong>7. Partitioning is opt-in</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) `OutboxPartitionOptions.Enabled = false` by default; with it off, messages that have a key are processed exactly as today** | Every messaging pattern is optional | The producer sets the key but gets no ordering until it enables the option |
| **B) On by default** | Ordering without configuration | Changes the processing of every outbox with keyed messages, and adds window-function SQL to every host |

### Chosen Option: **A — off by default**

### Rationale

- Every messaging pattern is optional and disabled by default; option B contradicts the cited rule.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (every messaging pattern is optional and disabled by default; ADR-050).

</details>

<details>
<summary><strong>8. Thin key interfaces</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) `ISagaPartitionedMessage`, `ITenantPartitionedMessage` and `IAggregatePartitionedMessage` over `IPartitionedMessage` (owned by #1254)** | The three keys the issue names are one interface each | Three small public types |
| **B) Only `IPartitionedMessage`** | Smallest API | The issue's acceptance list names the three interfaces |

### Chosen Option: **A — the three interfaces**

### Rationale

- The acceptance list of issue #469 names the three interfaces, so option B does not satisfy it.

**Class:** Settled
**Settled by:** issue #469 acceptance list (`ISagaPartitionedMessage`, `ITenantPartitionedMessage`, `IAggregatePartitionedMessage`).

</details>

<details>
<summary><strong>9. Composing tenant and key before #737 (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) `PartitionKeys.ForTenant(tenantId, key)` returns `Left` for a tenant id containing `:` and otherwise `tenantId:key`** | Readable; two tenants never share a partition; no hashing | The public key format is `tenant:key`; a tenant id with `:` is rejected |
| **B) Length-prefixed form `<len(tenantId)>:<tenantId><key>`** | Any tenant id is allowed; unambiguous | Less readable in tables and logs |
| **C) A hash of the pair** | Fixed width, unambiguous | Opaque; a collision is possible in principle; harder to debug |

### Chosen Option: **A — `tenantId:key`, `:` rejected in the tenant id** (recommended, pending the maintainer)

### Rationale

- Option A wins for operators reading the column; option B wins if tenant ids may contain `:`.
- It is a stopgap: #737 adds a real `TenantId` column and replaces the composition, so the format is short-lived.
- REQ-061 and AC-043 only require that tenants never share a partition, which all three options satisfy.

**Class:** Dilemma

</details>

<details>
<summary><strong>10. Parallelism across keys and thread safety of the store (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) The processor groups the fetched messages by key and runs the groups with `Parallel.ForEachAsync` (`MaxDegreeOfParallelism = MaxConcurrentPartitions`); publishing runs in parallel, every store call goes through one internal `SerializedOutboxStore` (a `SemaphoreSlim(1,1)` gate around the scoped store)** | Different keys publish in parallel, which is where the time goes; correct on all 10 providers because the single `DbContext`, connection or Mongo session is never used concurrently; no change to the stores | Store marks are serialized, so a fast handler workload is bounded by the store round trip |
| **B) One DI scope (one store, one `DbContext` or connection) per key group** | Store calls also run in parallel | `OutboxProcessorBase` and the transaction/commit model (`SaveChangesAsync` per scope) change; more connections per cycle |
| **C) Keep the serial loop** | No concurrency risk | Misses "different partitions run in parallel" |

### Chosen Option: **A — grouped parallel publishing, serialized store calls** (recommended, pending the maintainer)

### Rationale

- The requirement is parallelism across keys (issue #469: "Different partitions process in parallel"); the default `MaxConcurrentPartitions` follows the issue (`Environment.ProcessorCount`).
- Each group (a key's run) processes sequentially and stops at its first failure: the failed message is marked failed with its `NextRetryAtUtc`, the rest of the run stays unprocessed and is blocked by the head rule on the next fetch. Keyless messages stay in one serial group.
- Option B wins when the store round trip dominates; it is a larger change to the processor and is not needed for correctness.
- A test on the EF Core store with two concurrent groups proves the gate (Phase 8); the gate is unit-tested in Phase 7.

**Class:** Dilemma

</details>

<details>
<summary><strong>11. Defaults and smaller choices with no wrong answer</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) `MaxPerPartition = 50` (half of the default `BatchSize` of 100, so one key cannot fill a whole batch), `MinDispatchInterval = TimeSpan.Zero` (no pacing until configured), `MaxConcurrentPartitions = Environment.ProcessorCount`; the health check only reports stalled partitions and never changes their state; metrics are named `encina.outbox.partition.*` (the `encina.outbox.*` prefix of `OutboxProcessorMetrics`), not the issue's `encina.partition.*`; the minimum MySQL is 8.0 (tests run `mysql:8.4`)** | Safe defaults; consistent naming | The values are not tuned by measurement |
| **B) Other values or names** | None identified | Arbitrary |

### Chosen Option: **A**

### Rationale

- No defaults are observable by a caller who has not enabled the feature; the load test of Phase 9 measures them and they can change before 1.0.

**Class:** Trivial

</details>

---

## Implementation Phases

Nine pull requests, each of at most 15 files and 1500 lines (the largest is about 900 lines), ordered so that `main` compiles after every merge and an enabled processor never meets a store that cannot order: the interface member is added with a temporary default body first (expand), every store overrides it, and the default is removed in the pull request that adds the last two implementers (contract). The processor that uses the new fetch merges after every store. Requires #1254 and #123 merged; #1251 is needed for several instances (single-instance behavior is complete without it).

| PR | Phase | Content | Files | Lines (est.) |
| --- | --- | --- | ---: | ---: |
| PR-A | 1 | Core model, options, criteria, interface member with a default body | 14 | 450 |
| PR-B | 2 | ADO.NET x3: ordered fetch and stalled count, integration tests | 12 | 800 |
| PR-C | 3 | Dapper x3 | 12 | 800 |
| PR-D | 4 | EF Core and MongoDB | 10 | 900 |
| PR-E | 5 | `FakeOutboxStore`, `InstrumentedOutboxStore`, remove the default body | 11 | 450 |
| PR-F | 6 | Strictly increasing creation time on the two write paths | 7 | 300 |
| PR-G | 7 | Processor, serialized store, metrics, log class, EventIds | 14 | 900 |
| PR-H | 8 | Scenarios S3, S9, AC-043 on 10 providers | 11 | 700 |
| PR-I | 9 | Health check, load test, benchmark, documentation, ADR, changelog | 10 | 600 |
| **Total** | | | **101** | **about 5,900** |

Production files 28, test files 42, PublicAPI files 11, manifests 15, documentation, ADR and changelog 5: 28 + 42 + 11 + 15 + 5 = 101. The header, this table and the Estimated File Count table all state 101.

### Phase 1: Core Model, Options and Contracts (PR-A, additive)

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Messaging/Outbox/ISagaPartitionedMessage.cs`, `ITenantPartitionedMessage.cs`, `IAggregatePartitionedMessage.cs` (default interface members over `IPartitionedMessage.PartitionKey` from #1254); `PartitionKeys.cs` with `ForTenant` returning `Either<EncinaError, string>` (choice 9).
2. `OutboxPartitionOptions.cs` on `OutboxOptions.Partitioning`: `Enabled` (false), `MaxConcurrentPartitions`, `MaxPerPartition` (default 50), `MinDispatchInterval` (zero); validated in `OutboxOptions.Validate`.
3. `OutboxFetchCriteria` (from #123): add `PartitionOrdered`, `MaxPerPartition`, `PacingCutoffUtc` (a record: adding properties is additive).
4. `IOutboxStore.GetStalledPartitionCountAsync(int maxRetries, CancellationToken)`: number of keys whose first unprocessed message is exhausted. It is declared with a temporary default body returning `Left` (`EncinaErrors.Create` with an `ErrorCategory.FailedPrecondition` code `outbox.partition_stalled_count_not_supported`) so the 10 implementers still compile; PR-E removes the default. The default fails closed: it never reports "0 stalled".
5. Tests (same PR): `PartitionKeysTests`, `OutboxPartitionOptionsTests` (unit), a guard test for `PartitionKeys.ForTenant` and the options validation, and a property test (`ForTenant` is injective: equal results only for equal pairs).
6. `PublicAPI.Unshipped.txt` and the manifest of `Encina.Messaging` through `mechanical-fixer`.

Count: 8 src + 4 tests + 1 PublicAPI + 1 manifest = 14.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Plan docs/plans/partitioned-sequential-messaging-implementation-plan-469.md. #1254 added IOutboxMessage.PartitionKey and IPartitionedMessage; #123 added OutboxFetchCriteria and MessagePriority. This pull request is additive: nothing consumes the new members yet.
TASK: Implement Phase 1 Tasks 1 to 6.
KEY RULES: Either<EncinaError, T> and Left for invalid input (ADR-001, ADR-006); error codes created with ErrorCategory (ADR 062); options validated in OutboxOptions.Validate; no [Obsolete]; the default interface body is temporary and returns Left, never 0; XML docs on every public member; the key value is never logged or used as a metric dimension.
REFERENCE FILES: src/Encina.Messaging/Outbox/OutboxOptions.cs, IOutboxStore.cs, src/Encina/Errors/ErrorCategory.cs.
```

</details>

### Phase 2: ADO.NET Providers (PR-B)

<details>
<summary><strong>Tasks</strong></summary>

For each ADO.NET package (SqlServer, PostgreSQL, MySQL), in `Outbox/OutboxStoreADO.cs`:

1. The partition-ordered branch of `GetPendingMessagesAsync(criteria)`: a CTE with `ROW_NUMBER() OVER (PARTITION BY PartitionKey ORDER BY CreatedAtUtc, Id)` and `FIRST_VALUE` of the head-eligibility flag over unprocessed rows of keyed messages, `WHERE rn <= @MaxPerPartition AND head_ok = 1`, the pacing predicate (`NOT EXISTS` a message of the key with `ProcessedAtUtc > @PacingCutoffUtc` when pacing is on), unioned with the unchanged keyless query, limited to `@BatchSize` (`TOP (@n)` on SQL Server, `LIMIT @n` on PostgreSQL and MySQL 8.0 or later).
2. `GetStalledPartitionCountAsync`: count of keys whose first unprocessed message has `RetryCount >= @MaxRetries` (overrides the default of PR-A).
3. Index review: `(PartitionKey, CreatedAtUtc)` from #1254 serves the window; add `(PartitionKey, ProcessedAtUtc)` only if the benchmark of Phase 9 requires it.
4. Tests in the same PR: per provider an integration class (`OutboxStoreADOPartitionTests`, shared `[Collection("ADO-<Database>")]` fixture) with ordered prefix, blocked head (retry delay, exhaustion), keyless unchanged, prefix size, pacing, stalled count.
5. `PublicAPI.Unshipped.txt` and manifest entries per package through `mechanical-fixer`.

Count: 3 stores + 3 integration tests + 3 PublicAPI + 3 manifests = 12.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: ADO stores write SQL by hand. Window functions need SQL Server 2012+, PostgreSQL 8.4+, MySQL 8.0+ (the test fixtures run mysql:8.4). The existing keyless query is the one in OutboxStoreADO.GetPendingMessagesAsync.
TASK: Implement Phase 2 Tasks 1 to 5 for the three ADO.NET packages, one package per commit, with identical semantics.
KEY RULES: async with CancellationToken only; parameters, never concatenated values; the criteria record carries the clock values, stores never read the clock; keep the keyless query equivalent in behavior; identifiers follow each provider's quoting (brackets, lower-case, backticks); shared Collection fixtures.
REFERENCE FILES: src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs, src/Encina.ADO.PostgreSQL/Outbox/OutboxStoreADO.cs, src/Encina.ADO.MySQL/Outbox/OutboxStoreADO.cs, tests/Encina.IntegrationTests/ADO/SqlServer/Outbox/OutboxAtomicityTests.cs.
```

</details>

### Phase 3: Dapper Providers (PR-C)

<details>
<summary><strong>Tasks</strong></summary>

For each Dapper package, in `Outbox/OutboxStoreDapper.cs`: the same partition-ordered query and stalled count as Phase 2, using `CommandDefinition` with the `CancellationToken` and the `SELECT *`-style mapping to the provider `OutboxMessage`; the integration class `OutboxStoreDapperPartitionTests` per provider (`[Collection("Dapper-<Database>")]`); PublicAPI and manifest entries through `mechanical-fixer`.

Count: 3 stores + 3 integration tests + 3 PublicAPI + 3 manifests = 12.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: Dapper stores use CommandDefinition and map rows to the provider OutboxMessage class.
TASK: Apply Phase 3 to the three Dapper packages, mirroring Phase 2 so both families return identical sequences for identical data.
KEY RULES: same as Phase 2; DbLease and transaction accessor usage unchanged.
REFERENCE FILES: src/Encina.Dapper.SqlServer/Outbox/OutboxStoreDapper.cs, src/Encina.Dapper.PostgreSQL/Outbox/OutboxStoreDapper.cs, src/Encina.Dapper.MySQL/Outbox/OutboxStoreDapper.cs.
```

</details>

### Phase 4: EF Core (3 providers) and MongoDB (PR-D)

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.EntityFrameworkCore/Outbox/OutboxStoreEF.cs`: the partition-ordered branch uses `FromSql` with one SQL string per `Database.ProviderName` (SqlServer, PostgreSQL, MySQL), composed with `OrderBy` and `Take(BatchSize)`; the keyless branch stays LINQ. `GetStalledPartitionCountAsync` the same way.
2. `src/Encina.MongoDB/Outbox/OutboxStoreMongoDB.cs`: an aggregation (`$match` unprocessed keyed, `$sort` by `createdAtUtc` and `_id`, `$group` by `partitionKey` with `$push` and `$slice` to `MaxPerPartition`, head-eligibility filter, pacing `$lookup` or a second query) merged with the keyless query; `GetStalledPartitionCountAsync`.
3. Tests in the same PR: integration classes on EF Core SqlServer, PostgreSQL, MySQL and MongoDB with the same cases as Phase 2 (the result sequence equals the ADO.NET and Dapper result for the same data).
4. PublicAPI (EF Core, MongoDB) and the two manifests through `mechanical-fixer`.

Count: 2 stores + 4 integration tests + 2 PublicAPI + 2 manifests = 10.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT: One EF Core store serves three providers, so raw SQL needs a string per provider; MongoDB expresses the per-key prefix with an aggregation pipeline.
TASK: Implement Phase 4 Tasks 1 to 4.
KEY RULES: async with CancellationToken; no provider-specific code in abstractions; parameters, never concatenation; shared Collection fixtures with ClearAllDataAsync in InitializeAsync.
REFERENCE FILES: src/Encina.EntityFrameworkCore/Outbox/OutboxStoreEF.cs, src/Encina.MongoDB/Outbox/OutboxStoreMongoDB.cs, tests/Encina.IntegrationTests/Infrastructure/MongoDB/Stores/OutboxStoreMongoDBIntegrationTests.cs.
```

</details>

### Phase 5: Fake and Instrumented Stores, Remove the Default Body (PR-E)

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Testing.Fakes/Stores/FakeOutboxStore.cs`: the ordered fetch and stalled count in memory (same semantics as Phase 2).
2. `src/Encina.OpenTelemetry/MessagingStores/InstrumentedOutboxStore.cs`: forward `GetStalledPartitionCountAsync` with the existing activity and metric pattern.
3. `IOutboxStore.cs`: remove the default body (the 10 implementers now override it; the solution compiles).
4. Tests in the same PR: `FakeOutboxStore` unit tests, the ordered-fetch contract test run against the fake store (`tests/Encina.ContractTests/Messaging/Outbox/OutboxOrderedFetchContractTests.cs`, a real implementation, no `.md`), `InstrumentedOutboxStoreTests` and the two guard files `InstrumentedOutboxStoreGuardTests` and `InstrumentedStoreMethodGuardTests`.
5. `PublicAPI.Unshipped.txt` of `Encina.Testing.Fakes`; manifests of Fakes and OpenTelemetry through `mechanical-fixer`.

Count: 3 src + 5 tests + 1 PublicAPI + 2 manifests = 11.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT: After PR-B to PR-D every provider store overrides GetStalledPartitionCountAsync; the interface still carries the temporary default body.
TASK: Implement Phase 5 Tasks 1 to 5; the whole solution must compile after the default body is removed.
KEY RULES: the fake follows the same ordering and eligibility rules as the SQL stores; tests execute real code; Shouldly through Encina.Testing.Shouldly; no reflection-only tests.
REFERENCE FILES: src/Encina.Testing.Fakes/Stores/FakeOutboxStore.cs, src/Encina.OpenTelemetry/MessagingStores/InstrumentedOutboxStore.cs, tests/Encina.UnitTests/OpenTelemetry/MessagingStores/InstrumentedOutboxStoreTests.cs.
```

</details>

### Phase 6: Strictly Increasing Creation Time (PR-F)

<details>
<summary><strong>Tasks</strong></summary>

1. New `src/Encina.Messaging/Outbox/OutboxTimestamp.cs` (internal): strictly increasing `NextUtc(TimeProvider)` (choice 2).
2. `OutboxWriter.cs` and `OutboxOrchestrator.AddAsync` use it for `CreatedAtUtc` (`TransactionalOutboxPipelineBehavior` goes through the writer).
3. Tests in the same PR: `OutboxTimestampTests` (monotonic under `FakeTimeProvider`), `OutboxWriterTests` and `OutboxOrchestratorTests` (messages written in one call get increasing timestamps).
4. Manifest entry of `Encina.Messaging` through `mechanical-fixer` (the type is internal; no PublicAPI).

Count: 3 src + 3 tests + 1 manifest = 7.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT: OutboxWriter and OutboxOrchestrator.AddAsync stamp CreatedAtUtc from TimeProvider, which can repeat inside one transaction.
TASK: Implement Phase 6 Tasks 1 to 4.
KEY RULES: time only from TimeProvider (AGENTS.md section 3); the helper is thread-safe; no Thread.Sleep in tests.
REFERENCE FILES: src/Encina.Messaging/Outbox/OutboxWriter.cs, src/Encina.Messaging/Outbox/OutboxOrchestrator.cs.
```

</details>

### Phase 7: Batch Processor, Serialized Store, Metrics and Logs (PR-G)

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina/Diagnostics/EventIdRanges.cs`: register `MessagingPartitioning = (6735, 6739)` from the family block 6700-6749 (the block map is owned by the family registry in the plan-batch registry); add it to the `Encina.Messaging` entry in `EncinaEventIdAllocationTests`.
2. New `src/Encina.Messaging/Diagnostics/PartitioningLogMessages.cs` (`[LoggerMessage]`, 6735 to 6739): partitioning enabled at start-up; partition held after a failure (code and count, never the key); stalled partition detected (used by the health check of Phase 9); pacing delayed a partition; cycle summary (partitions processed, held). The log class is created here because this is the first pull request that logs.
3. `OutboxProcessorMetrics.cs`: `encina.outbox.partition.active` (gauge, partitions processed in the last cycle), `encina.outbox.partition.held` (counter), `encina.outbox.partition.paced` (counter), `encina.outbox.partition.wait_time` (histogram, seconds from creation to dispatch).
4. New `src/Encina.Messaging/Outbox/SerializedOutboxStore.cs` (internal): an `IOutboxStore` decorator that runs every call under `SemaphoreSlim(1,1)` (choice 10); the processor wraps its scoped store with it only when partitioning is enabled.
5. `src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs`: build the partition criteria (cutoff for pacing = now minus `MinDispatchInterval`); group the fetched messages by `PartitionKey` (keyless messages in one serial group); `Parallel.ForEachAsync` over key groups bounded by `MaxConcurrentPartitions`; inside a group stop at the first message whose outcome is not `Success` (failed, exhausted, store error, cancelled); `OutboxBatchResult` gains `HeldPartitions`. A cancelled group stops cleanly and no exception escapes the loop (AGENTS.md section 3). Split `ProcessAsync` into small methods (CRAP at most 10).
6. Tests in the same PR: `OutboxBatchProcessorPartitionTests` (grouping, N holds N+1, other keys continue, pacing eligibility, cancellation, with `FakeTimeProvider`), `SerializedOutboxStoreTests` (no two store calls overlap), a metrics exporter test, a property test (for any interleaving of keys and failures the delivered sequence of each key is a prefix of its creation order), the existing `OutboxBatchProcessorTests` updated for the result type, and the architecture test entry.
7. `PublicAPI.Unshipped.txt` of `Encina` (the new `EventIdRanges` field); manifests of `Encina` and `Encina.Messaging` through `mechanical-fixer`.

Count: 5 src + 6 tests + 1 PublicAPI + 2 manifests = 14.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT: ADR-021: register the range first (6735-6739 is assigned to this plan inside the family block). OutboxBatchProcessor.ProcessAsync fetches then runs a serial foreach and continues after a failed message (src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs). All stores now support the ordered fetch (PR-B to PR-E).
TASK: Implement Phase 7 Tasks 1 to 7. Messages without a key keep today's serial behavior and ordering exactly.
KEY RULES: a Left from a store or from publishing fails the operation and stops that key's run, never reports success; per-key sequential, cross-key parallel, store calls serialized; no exception escapes a background loop; time from TimeProvider; [LoggerMessage] source generator; the partition key is never a log argument, tag or metric dimension; EncinaError.Message never reaches logs; CRAP at most 10 on changed methods.
REFERENCE FILES: src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs, OutboxProcessorBase.cs, OutboxRetryBackoff.cs, src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs.
```

</details>

### Phase 8: Scenarios on the 10 Providers (PR-H)

<details>
<summary><strong>Tasks</strong></summary>

1. `tests/Encina.IntegrationTests/Messaging/Outbox/OutboxPartitionScenarios.cs` (shared scenario, like `OutboxAtomicityScenario`): S3 (book, reschedule, cancel delivered in order per appointment), S9 (rejection of record N holds later records; a server-directed wait is honoured; pacing interval respected), two tenants with the same key value never share a partition (AC-043, through `PartitionKeys.ForTenant`), and two concurrent key groups through the serialized store (the thread-safety proof for the EF Core `DbContext`).
2. One derived class per provider in the shared collections (ADO x3, Dapper x3, EF Core x3, MongoDB): 10 files.

Count: 1 scenario + 10 provider classes = 11.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```text
CONTEXT: AGENTS.md section 9: tests execute real code, shared [Collection("<Family>-<Database>")] fixtures, ClearAllDataAsync in InitializeAsync. The scenario pattern is tests/Encina.IntegrationTests/Messaging/Outbox/OutboxAtomicityScenario.cs with per-provider classes.
TASK: Write the scenarios of Phase 8 and report per-flag coverage against the manifest.
KEY RULES: deterministic (FakeTimeProvider, no Thread.Sleep); all 10 providers; never a .md for database integration tests; SQLite excluded.
REFERENCE FILES: tests/Encina.IntegrationTests/Messaging/Outbox/OutboxRetryScenarios.cs, tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Outbox/OutboxAtomicityEFTestsBase.cs.
```

</details>

### Phase 9: Health Check, Load, Benchmark and Documentation (PR-I)

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Messaging/Health/OutboxHealthCheck.cs`: `Degraded` when `GetStalledPartitionCountAsync` is greater than zero (key count only, never the keys); a `Left` from the store makes the check `Unhealthy`, never `Healthy`. Unit test `OutboxHealthCheckPartitionTests`; manifest entry of `Encina.Messaging`.
2. Load test (a concurrent feature): many keys with a bounded `MaxConcurrentPartitions` (`tests/Encina.LoadTests/Messaging/OutboxPartitionLoadTests.cs`); benchmark of the ordered fetch against the plain fetch (`tests/Encina.BenchmarkTests/Messaging/OutboxPartitionFetchBenchmarks.cs`), which decides the `(PartitionKey, ProcessedAtUtc)` index of choice 4 and measures the defaults of choice 11.
3. `docs-writer`: guide "Ordered delivery per key" (ordering guarantee and its limits: one writer per key, one instance until #1251, hold semantics, operator requeue, pacing, per-tenant keys), `src/Encina.Messaging/README.md`, `docs/INVENTORY.md`.
4. ADR for the ordering mechanism (choice 1, with choices 2 to 5); ADR number assigned when the ADR pull request opens. `changelog.d/469-partitioned-outbox.added.md` (`mechanical-fixer`).
5. `dotnet build --configuration Release` with zero warnings; `dotnet test`; every flag at its target.

Count: 1 health check + 1 health test + 1 manifest + 2 load and benchmark + 3 docs + 1 ADR + 1 changelog = 10.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```text
CONTEXT: documentation, changelog fragments and PublicAPI lines have path owners (docs-writer, mechanical-fixer). The health check follows the IEncinaHealthCheck pattern (DefaultName const, Tags, scoped resolution). Load tests run locally with DOTNET_JitObjectStackAllocationConditionalEscape=0.
TASK: Finish Phase 9 Tasks 1 to 5 after Phases 1 to 8 are merged.
KEY RULES: English only; coverage figures only through covref markers; write "the reference application", never a private client name; state the guarantee's limits plainly; EncinaError.Message never reaches health results; BenchmarkSwitcher, never BenchmarkRunner.Run.
REFERENCE FILES: src/Encina.Messaging/Health/OutboxHealthCheck.cs, changelog.d/README.md, docs/architecture/adr/index.md, docs/specifications/SPEC-002 section 5.11, docs/plans/dsr-implementation-plan-404.md.
```

</details>

### Per-file coverage targets (entered in the PR that carries the tests)

| File | PR | Flag and target | Justification |
| --- | --- | --- | --- |
| `Outbox/PartitionKeys.cs` | PR-A | unit 100, guard 80, property 90 | Every branch of `ForTenant` is a unit test; guard tests reach the null and empty arguments; the property test covers the success path. |
| `Outbox/OutboxPartitionOptions.cs` | PR-A | unit 95, guard 60 | Defaults and every rejected value are unit-tested; guard covers the setters that throw. |
| `Outbox/ISagaPartitionedMessage.cs`, `ITenantPartitionedMessage.cs`, `IAggregatePartitionedMessage.cs` | PR-A | unit: default-member tests, 0 where no body | The default members are called through test messages; an interface without a body has no executable lines. |
| Each provider `OutboxStore*.cs` | PR-B to PR-D | integration: the current manifest target, never lowered | The ordered fetch and stalled count are exercised by the integration classes of the same pull request; the worker reads the current value and only raises it. |
| `Stores/FakeOutboxStore.cs` | PR-E | unit 95, contract 70 | Unit tests cover ordering, eligibility and pacing; the contract test runs the same ordered-fetch contract on the fake. |
| `MessagingStores/InstrumentedOutboxStore.cs` | PR-E | unit: current target, guard: current target | The forwarded method follows the existing pattern covered by the instrumented-store tests. |
| `Outbox/OutboxTimestamp.cs` | PR-F | unit 100 | The helper is small and fully unit-tested under `FakeTimeProvider`. |
| `Outbox/OutboxBatchProcessor.cs` | PR-G | unit 98, integration 80 (current targets, not lowered) | Unit tests drive grouping, stop-on-failure, pacing and cancellation; the scenarios of PR-H add the integration flag. |
| `Outbox/SerializedOutboxStore.cs` | PR-G | unit 100, guard 0 | Every method is a delegated call under the gate, unit-tested for non-overlap; the type is internal without `InternalsVisibleTo` for `Encina.GuardTests`. |
| `Diagnostics/PartitioningLogMessages.cs` | PR-G | unit 0 | `[LoggerMessage]` declarations; the generated bodies are not in the file. |
| `Diagnostics/OutboxProcessorMetrics.cs` | PR-G | unit: current target | The exporter test drives the new instruments. |
| `Health/OutboxHealthCheck.cs` | PR-I | unit: current target | The health test covers healthy, degraded and store-error states. |

Guard tests for the new public API (#3798): `PartitionKeys.ForTenant` (null and whitespace tenant id and key, `:` in the tenant id) and `OutboxPartitionOptions` setters get guard tests in PR-A; the three key interfaces have default members only; `IOutboxStore.GetStalledPartitionCountAsync` implementers are covered by the provider guard tests that already exist for their other methods.

Test flags by feature: unit and guard in every pull request; property in PR-A and PR-G; contract on the fake (PR-E) and Integration on the 10 real stores (PR-B to PR-D, PR-H), since a contract test with real databases is an integration test; load and benchmark in PR-I. No test type uses a `.md` justification.

---

## Research

### Relevant Standards & Specifications

| Standard or source | Relevance |
| --- | --- |
| SPEC-002 REQ-042, AC-039 (S3, S9), REQ-061, AC-043, DEC-005 (a) | The requirement, its acceptance scenarios and the per-tenant rule |
| Enterprise Integration Patterns: Resequencer, Message Sequence | Per-key ordered delivery |
| Wolverine 5.0 partitioned sequential messaging | Inspiration cited by the issue (in-process); this plan persists the guarantee in the outbox |
| AGENTS.md sections 3, 5, 6, 7, 9 | Background-error rule, 10 providers, cross-cutting check, EventIds, load tests |
| ADR-001, ADR-006, ADR-018, ADR-021, ADR-050, ADR 062 | ROP, cross-cutting check, EventIds, module dependencies, error categories |

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
| `Encina.Messaging` (`EventIdRanges.MessagingPartitioning`, new) | 6735-6739 | Five ids from the family block 6700-6749 (owned by the family registry); siblings hold their own sub-ranges (#123 6700-6714, #301 6715-6724, #251 6725-6734, #1254 6748). `Encina.Messaging` already has an `AssemblyRanges` entry; the new field is added to it in PR-G |

### Estimated File Count

| Category | Files | Notes |
| --- | --- | --- |
| Production | 28 | PR-A 8, PR-B 3, PR-C 3, PR-D 2, PR-E 3, PR-F 3, PR-G 5, PR-I 1 |
| Tests | 42 | PR-A 4, PR-B 3, PR-C 3, PR-D 4, PR-E 5, PR-F 3, PR-G 6, PR-H 11, PR-I 3 |
| PublicAPI | 11 | PR-A 1, PR-B 3, PR-C 3, PR-D 2, PR-E 1, PR-G 1 |
| Coverage manifests | 15 | PR-A 1, PR-B 3, PR-C 3, PR-D 2, PR-E 2, PR-F 1, PR-G 2, PR-I 1 |
| Documentation, ADR, changelog | 5 | PR-I: guide, README, INVENTORY, ADR, changelog |
| **Total** | **101** | 28 + 42 + 11 + 15 + 5 |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined Prompt — All Phases</strong></summary>

```text
PROJECT CONTEXT: Encina, pre-1.0, .NET 10 / C# 14. Issue #469 (SPEC-002 REQ-042, AC-039 S3 and S9), plan docs/plans/partitioned-sequential-messaging-implementation-plan-469.md. It needs #1254 (PartitionKey column, IPartitionedMessage) and #123 (OutboxFetchCriteria) merged; row claiming for several instances is #1251. Read AGENTS.md first.

IMPLEMENTATION OVERVIEW: (1) Key interfaces, OutboxPartitionOptions, criteria extension, stalled-count store method with a temporary default. (2) ADO.NET x3, (3) Dapper x3, (4) EF Core and MongoDB ordered fetch. (5) Fake and instrumented stores, remove the default. (6) Strictly increasing creation time. (7) OutboxBatchProcessor: group by key, parallel across keys, stop at the first failure, serialized store, EventIds 6735-6739, metrics. (8) Scenarios S3, S9, AC-043 on 10 providers. (9) Health, load, benchmark, docs, ADR. Nine pull requests of at most 15 files and 1500 lines, each compiling on its own.

KEY PATTERNS: Either<EncinaError, T>; a failed message holds the later messages of its key and is never reported as success; order dominates priority inside a key; time from TimeProvider, stores never read the clock; opt-in via OutboxPartitionOptions.Enabled = false; keyless messages behave exactly as today; the partition key is never logged or a metric dimension; [LoggerMessage] with a registered range; async database calls with CancellationToken; shared [Collection] fixtures.

REFERENCE FILES: src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs, OutboxOptions.cs, OutboxWriter.cs, src/Encina.Dapper.SqlServer/Outbox/OutboxStoreDapper.cs, src/Encina.EntityFrameworkCore/Outbox/OutboxStoreEF.cs, src/Encina.MongoDB/Outbox/OutboxStoreMongoDB.cs, src/Encina.Messaging/Health/OutboxHealthCheck.cs, src/Encina/Diagnostics/EventIdRanges.cs, docs/specifications/SPEC-002 section 5.11.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Message sequencing, not data reads |
| 2 | OpenTelemetry | ✅ | `encina.outbox.partition.*` gauge, counters and histogram on the shared meter (Phase 7); never the key value |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` 6735-6739 in `EventIdRanges.MessagingPartitioning` (Phase 7) |
| 4 | Health Checks | ✅ | `OutboxHealthCheck` reports `Degraded` for stalled partitions (Phase 9) |
| 5 | Validation | ✅ | `OutboxPartitionOptions` validated in `OutboxOptions.Validate`; tenant id with `:` rejected in `PartitionKeys.ForTenant` (Phase 1) |
| 6 | Resilience | ✅ | Retry, backoff and dead-letter of a held partition follow the existing outbox policy (`OutboxRetryBackoff`, `MaxRetryAfter`); no new retry mechanism |
| 7 | Distributed Locks | ⏭️ | Multi-instance ordering needs row claiming, issue #1251; the lock-per-key option (choice 1, C) is not chosen |
| 8 | Transactions | ✅ | The key is written in the outbox transaction (#1254); the processor marks outcomes through the existing store methods |
| 9 | Idempotency | ⏭️ | Duplicate handling inside a partition is #735, #148 and #1220 |
| 10 | Multi-Tenancy | ✅ | Partitions and pacing are per tenant via `PartitionKeys.ForTenant` now (REQ-061); a real `TenantId` column is #737 |
| 11 | Module Isolation | ⏭️ | Module-scoped partitions follow `ModuleId` on messaging entities, issue #747 |
| 12 | Audit Trail | ⏭️ | An operator releasing or discarding a held partition is an operational action on live flows: the requeue path is the CLI of #2610; the audit event of hold and release is a follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/partition-operator-discard-and-audit.md) |

---

## Prerequisites & Dependencies

- #1254 (column and `IPartitionedMessage`) and #123 (`OutboxFetchCriteria`) merged before PR-A.
- #1251 (row claiming) for ordering guarantees across several instances; the plan is complete and correct for one instance without it.
- #737 (`TenantId` on `OutboxMessage`) replaces the composed tenant key; window functions need MySQL 8.0 or later (the test fixtures run `mysql:8.4`).
- Operators need the existing requeue path (`RequeueExhaustedAsync`, `Encina.Cli` messaging commands, issue #2610) to release a held partition.
- Merge order: PR-A, then PR-B, PR-C, PR-D (any order), PR-E (after all three), PR-F, PR-G (after PR-E and PR-F), PR-H and PR-I (after PR-G).

## Next Steps

1. Maintainer answers the Dilemmas, design choices 1 to 5, 9 and 10 (the issue's D1, D2 and D3 collapse into choice 1), when work starts; answers go to `## Maintainer Decisions`.
2. Worker briefs per pull request (PR-A to PR-I) after #1254 and #123 merge.
3. Orchestrator updates #469 to the scope above and notes that `IPartitionedQueueManager` and `IPartitionStore` are dropped under choice 1, option B.
