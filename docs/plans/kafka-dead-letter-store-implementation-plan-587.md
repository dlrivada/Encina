# Implementation Plan: `DeadLetterStoreKafka` — Apache Kafka backend for the persistent dead letter queue

> **Issue**: [#587](https://github.com/dlrivada/Encina/issues/587)
> **Type**: Feature
> **Complexity**: High (6 phases, 1 provider, a log-backed index, ~16 source files + tests)
> **Estimated Scope**: ~1,200-1,600 lines of production code + ~1,800-2,400 lines of tests

---

## Summary

Implement `Encina.Messaging.DeadLetter.IDeadLetterStore` on Apache Kafka as `DeadLetterStoreKafka`, in the existing `Encina.Kafka` package, using dedicated dead letter topics per source pattern (the standard dead letter topic pattern of Kafka ecosystems, so external consumers can read them). A Kafka topic is an append-only log; the ADR-046 contract also needs reads by id and filter with paging, exact counts, an atomic replay claim, a unique `(SourcePattern, SourceMessageId)` key (`AddAsync` returns `Right(false)` for a repeat) and deletes. The plan resolves that gap with a **log-backed index**: the topics stay the single source of truth, each instance folds them into an in-process index, and the order of the log arbitrates uniqueness and claims (Design Choices 1 and 2).

What the code looks like today (main `18ec8dd19`):

- `src/Encina.Kafka` has `KafkaMessagePublisher`, `KafkaHealthCheck`, `EncinaKafkaOptions`, `AddEncinaKafka`, and EventId range `Kafka` (4150-4199, ids used 4150-4155). It registers one singleton `IProducer<string, byte[]>` (idempotence on by default, acks `all`). It has no dead letter code. `Confluent.Kafka` 2.14.0 and `Testcontainers.Kafka` 4.15.0 are pinned; `docker-compose.yml` has a Kafka service (`apache/kafka:3.7.0`, KRaft); `tests/Encina.TestInfrastructure/Fixtures/KafkaFixture.cs` exists.
- The contract is `IDeadLetterStore` (`src/Encina.Messaging/DeadLetter/IDeadLetterStore.cs`). The issue's sample used `IProducer<string, string>`, `message.Source`, `message.RetryCount` and a `DeadLetterMessage` type; the real producer is `IProducer<string, byte[]>`, the fields are `SourcePattern` and `TotalRetryAttempts`, and the record is `IDeadLetterMessage`. The topic list of the issue (`dlq-inbox`, `dlq-outbox`, `dlq-saga`, `dlq-scheduled`) misses Recoverability and Choreography and misnames Scheduling; the real values are in `DeadLetterSourcePatterns`.
- Existing backends: `DeadLetterStoreADO`/`Dapper` (x3 each), `DeadLetterStoreEF`, `DeadLetterStoreMongoDB`, `InstrumentedDeadLetterStore`, `FakeDeadLetterStore`; the behavior suite is `tests/Encina.ContractTests/Messaging/DeadLetter/DeadLetterStoreContract.cs`.

**Provider category**: Kafka is a message transport, outside the 10-provider database matrix. As a transport it also belongs to the transport category (`AGENTS.md` section 5), so the store reuses the publisher's client configuration. **Affected packages**: `Encina.Kafka`, `Encina.Messaging` (no change), test projects.

---

## Design Choices

<details>
<summary><strong>1. Can a Kafka topic be an `IDeadLetterStore` at all (Dilemma D1)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A write-and-replay sink with its own narrower interface** (for example `IDeadLetterSink`) accepted by the orchestrator besides the store | Honest about what a topic is; Kafka consumers are the operators and replay by consuming | A second abstraction in `Encina.Messaging`; the manager, health check and cleanup processor do not work for this backend |
| **B) Implement `IDeadLetterStore` fully; the topics are the log and an in-process index folded from them answers queries** | Passes the shared contract suite; provider coherence kept (switching provider is a registration change); no state outside Kafka | State is rebuilt at startup (read of every dead letter topic); memory proportional to the queue; claims and uniqueness need log-order arbitration (Design Choice 2) |
| **C) Implement the interface and return a typed `Left` for what a topic cannot do** | Smallest to build | Breaks provider coherence (`AGENTS.md` section 5) and surprises callers; a stop-gap only |

### Chosen Option: **B — full store, log-backed index** (recommended, pending the maintainer)

### Rationale

- The queue is an exception path, so it is small enough to hold in memory (a configurable cap makes the store fail closed with a `Left` and an unhealthy health check when exceeded, instead of silently degrading). The cap counts messages and bytes (Design Choice 8).
- A wins if the maintainer decides Kafka dead letters are consumed only by Kafka tooling and the manager is not used with this backend; it needs a core abstraction, which is why it is not recommended for a backend issue.
- Multi-instance use: every instance consumes all partitions of all dead letter topics (manual assignment from the beginning, no committed group offsets), so each instance holds the same index.
- **Startup cost**: every instance reads the retained history of every dead letter topic from the beginning, including records of messages that were deleted since, so readiness time grows with the retained history, not with the live queue. With a finite `RetentionMs` (Design Choice 3) the history is bounded by the retention window; with infinite retention it is unbounded, which is one more reason infinite retention is opt-in. Periodic snapshots or compaction of the index would bound it further: deferred, issue to open at implementation time; the catch-up duration is logged and the health check is `Unhealthy` until the end offsets observed at start are reached.

</details>

<details>
<summary><strong>2. Uniqueness of the source key and the atomic claim (Dilemma D2)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Key the record by the source key and rely on log compaction** | Simplest; duplicates collapse eventually | `AddAsync` always returns `Right(true)`: the contract's `Right(false)` does not hold; compaction keeps the last record, not the first, so the surviving `Id` can be the loser's |
| **B) Arbitration by log order: produce, wait until the local index has seen the record's offset, and decide from the folded state, with every record that changes a source key's state in that key's one partition** (the ordering domain is the source key, Design Choice 6) | The contract holds across instances: the first capture of a source key in its partition wins; a claim record is effective only if, at its position in the key's order, the message is unreplayed and holds no live claim; every instance folds the same sequence per key, including after a delete and a re-capture of the same key | A write waits for the local consumer to catch up (bounded timeout, then `Left`); needs deterministic folding (records carry the instants they were decided with) |

### Chosen Option: **B — arbitration by log order inside the source key's partition** (recommended, pending the maintainer)

### Rationale

- Every record that can change the state of a source key (capture, claim, replayed, deleted) is keyed by `SourceMessageId` in the topic of its `SourcePattern`, so they share one partition and one total order; the fold is per key over that sequence. The first capture wins; a repeat is detected by the fast path (the index already holds the key) or by arbitration when two instances race; after a `Deleted` record in the same sequence a re-capture of the key is accepted by every instance alike. Two topics would be two orders: instance A could read `Deleted(Id1)` before `Capture(Id2)` and accept it while instance B reads the capture first and treats it as a duplicate, which is why no second topic carries state-changing records.
- A claim record carries a claim id, the claim instant and `ClaimExpiredBeforeUtc` as given by the caller, so every instance folds it to the same result without reading a clock. `TryClaimForReplayAsync` returns `Right(true)` only when the folded current claim id equals its own.
- A wins only where `Right(false)` is not needed and a stale duplicate is tolerable, which ADR-046 Choice 6 rejects (a duplicate dead letter makes a replay dispatch twice).

</details>

<details>
<summary><strong>3. What delete means on a log and how physical removal happens (Dilemma D3)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Logical delete by a `Deleted` record; bytes leave the broker when topic retention expires** (`cleanup.policy=delete`, `retention.ms` configurable) | Keeps history; no compaction subtleties; uniqueness arbitration stays correct | Erasure on demand is not possible: personal data in the payload stays readable until retention ends |
| **B) Compacted capture topics with a tombstone on delete** (`cleanup.policy=compact,delete`) | Physical removal of the payload after compaction | Compaction keeps the last record per key, which interacts with the arbitration of Design Choice 2 (duplicates of a source key are never produced when the fast path hits, but a race can leave a loser as the survivor); more operational tuning |

### Chosen Option: **A — logical delete, retention for physical removal** (recommended, pending the maintainer)

### Rationale

- With a logical delete, `DeleteAsync` and `DeleteExpiredAsync` remove a message from every read at once but not its bytes: `RequestContent` (which may hold personal data) stays on the broker until retention expires. Retention is therefore the erasure mechanism, and its default is Design Choice 7 (it is not decided here). Message encryption (`IMessageSerializer`) may be added by the application and is not a substitute for retention.
- B wins when physical erasure on demand is a legal requirement for this backend; it can be added later as a topic configuration plus a tombstone producer without changing the contract.

</details>

<details>
<summary><strong>4. Contract, semantics and content rules</strong></summary>

**Class:** Settled
**Settled by:** ADR-046 (decisions 1, 5, 6, 7), ADR-001, ADR-006, AGENTS.md section 3 (TimeProvider; `EncinaError.Message` and exception text never reach logs or storage), SPEC-002 REQ-061

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Full ADR-046 semantics: oldest first by `DeadLetteredAtUtc` then `Id`, replay result as a code, "now" and expiry from `TimeProvider`, `TenantId` in the record and as a header, headers carry `ExceptionType`, source pattern and `TotalRetryAttempts` (never exception text)** | Provider coherence; passes the contract suite | None |
| **B) Looser semantics** | Less code | Breaks coherence; leaks exception text |

### Chosen Option: **A — the full contract**

### Rationale

- Fixed by ADR-046 and enforced by `DeadLetterStoreContract`. Argument rules (`skip >= 0`, `1 <= take <= DeadLetterStoreLimits.MaxPageSize`, non-empty id, non-blank result) throw before any I/O.

</details>

<details>
<summary><strong>5. Client reuse, naming, registration and tests</strong></summary>

**Class:** Settled
**Settled by:** AGENTS.md section 3 "Provider coherence" ("providers share the same interfaces and configuration and differ only in implementation, so switching provider means changing the DI registration"), which excludes a store-owned second client configuration (option B); AGENTS.md section 4 (`{Pattern}Store{Provider}`), AGENTS.md section 3 (registration completeness), AGENTS.md section 9 (shared `[Collection]` fixtures), #587

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `DeadLetterStoreKafka` in `Encina.Kafka`; reuses the registered `IProducer<string, byte[]>` and `EncinaKafkaOptions` (bootstrap servers, acks, idempotence); `AddEncinaDeadLetterKafka()` registers the producer with `TryAdd` through the same internal helper as `AddEncinaKafka`, so the DI test builds without calling `AddEncinaKafka` first** | One Kafka configuration; the store wins in any order | The producer registration is extracted into a shared internal method |
| **B) The store builds its own producer from its own options** | Independent | A second client configuration; switching transports is no longer one registration |

### Chosen Option: **A — reuse the transport client**

### Rationale

- The topic names are `{TopicPrefix}{pattern}` with the prefix `dlq-` and the pattern in lower invariant case (`dlq-recoverability`, `dlq-outbox`, `dlq-inbox`, `dlq-scheduling`, `dlq-saga`, `dlq-choreography`); characters Kafka does not allow in topic names are escaped as `_xx`. There is no separate events topic under the recommended option of Design Choice 6 (the name `lifecycle` is reserved only if option B of that choice is picked). Topic creation is opt-in (`CreateTopicsIfMissing`, default `false`).
- Integration tests use the shared Kafka container fixture and a unique topic prefix per test run, since topics cannot be truncated cheaply.

</details>

<details>
<summary><strong>6. Ordering domain of the state-changing records (Dilemma D4)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Every record (capture, claim, replayed, deleted) goes to the pattern topic, keyed by the source key `SourceMessageId`** | One ordering domain per key: every instance folds the same sequence, including delete followed by re-capture; a claim can never be read before its capture; no second topic to create, discover or reserve; external consumers see the whole history of a message in one topic | Lifecycle records are produced to a topic whose name says "pattern"; `TryClaimForReplayAsync(messageId, ...)` first resolves the source key from the index by `Id` (a message absent from the index is `Right(false)`) |
| **B) A separate `dlq-lifecycle` topic keyed by the source key plus a capture sequence number** | Capture topics stay capture-only | Two topics are two orders: the fold must order a lifecycle record against the capture it belongs to through the sequence number, and a lifecycle record can arrive before its capture on another instance; more code and a harder property to test |

### Chosen Option: **A — everything in the pattern topic, keyed by the source key** (recommended, pending the maintainer)

### Rationale

- A removes the cross-topic ordering problem instead of compensating for it, and is the smallest correct design. B wins only if the maintainer wants capture topics to stay capture-only for external consumers.

</details>

<details>
<summary><strong>7. Default retention and the infinite-retention opt-in (Dilemma D5)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No default: `RetentionMs` is required; startup validation fails when it is infinite (`-1`) unless `AllowInfiniteRetention = true`, which is logged at startup** | Fail closed: a compliance control (physical removal of payload data) is never off by accident; the operator chooses a value that matches the policy | The first run needs one more setting |
| **B) A finite default (a number chosen by the maintainer, for example 30 or 90 days) with the same explicit opt-in for infinite retention** | Works out of the box and still erases | The number is a policy choice with legal weight; a default shorter than the replay window loses unreplayed dead letters silently |
| **C) Infinite default (`-1`)** | A dead letter never vanishes before an operator replays it | Personal data in `RequestContent` is never physically removed unless the operator remembers to set a retention: a compliance default that is off |

### Chosen Option: **A — no default, startup validation, explicit logged opt-in for infinite** (recommended, pending the maintainer)

### Rationale

- Compliance and security controls fail closed (`AGENTS.md` section 3): C is rejected; the choice between A and B is the maintainer's because B embeds a legal number. B wins when out-of-the-box operation matters more than a first-run setting. The opt-in is `AllowInfiniteRetention` (an options property, default `false`), and startup logs it once when it is on.

</details>

<details>
<summary><strong>8. Memory model of the index (Dilemma D6)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The index keeps every indexed message in memory, capped by count (`MaxIndexedMessages`, default 100,000) and by bytes (`MaxIndexedBytes`, default 256 MiB, counting `RequestContent` and `ExceptionStackTrace`)** | Reads never touch the broker; simple | `DeadLetterStoreLimits` sets no maximum length for the payload fields, so the count cap alone does not bound memory; the byte cap makes the failure mode explicit |
| **B) The index keeps metadata and the record position (topic, partition, offset) only and re-reads the payload from the broker on demand** | Memory proportional to the number of messages, not their size | A broker read per `GetAsync`; a consumer pool for point reads; more code |

### Chosen Option: **A — in memory, capped by count and bytes** (recommended, pending the maintainer)

### Rationale

- Exceeding either cap makes the index refuse new captures with a `Left` (`dlq.index_over_cap`) and turns the health check `Unhealthy` (fail closed). B wins when payloads are routinely large; it can replace the in-memory payload later without changing the contract.

</details>

---

## Implementation Phases

### Phase 1: Records, serialization and topic naming

> **Goal**: the persisted shapes and names.

<details>
<summary>Tasks</summary>

1. `src/Encina.Kafka/DeadLetter/DeadLetterKafkaMessage.cs`: public sealed class implementing `IDeadLetterMessage` and `DeadLetterKafkaMessageFactory.cs`.
2. `DeadLetterKafkaRecords.cs` (internal): `CaptureRecord` (the full message), and lifecycle records `ClaimRecord(Id, ClaimId, ClaimedAtUtc, ClaimExpiredBeforeUtc)`, `ReplayedRecord(Id, ReplayedAtUtc, ReplayResult)`, `DeletedRecord(Id, DeletedAtUtc)`; every record is produced to the pattern topic keyed by the source key (Design Choice 6); System.Text.Json source generation.
3. `DeadLetterTopicNames.cs`: topic per pattern with the escaping (and the reserved `lifecycle` name only under option B of Design Choice 6); headers: `encina-dlq-id`, `encina-dlq-source-pattern`, `encina-dlq-exception-type`, `encina-dlq-retry-attempts`, `encina-dlq-tenant-id`; never exception text. The tenant header is plaintext and readable by any consumer of the topic: the README and the feature page say so, and an application that must not expose the tenant to topic consumers uses topic ACLs (the tenant is also inside the record value).
4. Public API tracking: `src/Encina.Kafka` has no `PublicAPI.Shipped.txt` or `PublicAPI.Unshipped.txt` and its csproj has no `PublicApiAnalyzers` (verified on main), so there are no PublicAPI lines to write; adding the tracking to the package is a separate decision outside this plan.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
- Encina is a .NET 10 / C# 14 library (Railway Oriented Programming). Issue #587: DeadLetterStoreKafka implements IDeadLetterStore (ADR-046) in src/Encina.Kafka, backed by dead letter topics and a log-backed index.

TASK:
Create the message class, the factory, the record types, the topic-name helper and the header names as listed in the Phase 1 Tasks.

KEY RULES:
- Headers and records never contain exception text or EncinaError.Message.
- Instants are UTC; records carry the instants they were decided with (deterministic folding).
- No [Obsolete], no compatibility layer; XML docs on every public type.

REFERENCE FILES:
- src/Encina.MongoDB/DeadLetter/DeadLetterMessage.cs and DeadLetterMessageFactory.cs
- src/Encina.Messaging/DeadLetter/DeadLetterSourcePatterns.cs
- src/Encina.Kafka/KafkaMessagePublisher.cs
```

</details>

---

### Phase 2: The log-backed index

> **Goal**: fold the topics into an in-process index that every instance keeps.

<details>
<summary>Tasks</summary>

1. `DeadLetterIndex.cs` (internal, thread-safe): by `Id`, by `(SourcePattern, SourceMessageId)`, and a sorted view by `DeadLetteredAtUtc` then `Id`; `Apply(CaptureRecord, position)`, `Apply(ClaimRecord, position)`, `Apply(ReplayedRecord, position)`, `Apply(DeletedRecord, position)` folding per source key, in the order of the key's partition, with the rules of Design Choice 2 (first capture of a key wins; a capture after a delete of the same key is accepted; a claim is effective only while unreplayed and without a live claim; replay only while unreplayed; delete removes); `WaitUntilAsync(topic, partition, offset, timeout, ct)` for read-your-writes.
2. `DeadLetterIndexService.cs` (`BackgroundService`): one consumer built by a singleton factory (`DeadLetterConsumerFactory`, registered by `AddEncinaDeadLetterKafka`) from the same `EncinaKafkaOptions` as the producer: `BootstrapServers` is reused, `EnableAutoCommit = false`, `AutoOffsetReset = earliest`, and `GroupId` is `{GroupId}-dlq-index` (Confluent.Kafka requires a `group.id` to build a consumer, but the service uses manual `Assign` and never commits, so the group has no members and no offsets). `EncinaKafkaOptions` has no security fields today (verified), so there is nothing else to reuse; a security setting added to the options later must reach both the producer and the consumer factory (a DI test with `ValidateOnBuild` covers the graph). The consumer reads every dead letter topic from the beginning; a per-iteration `try`/`catch` that logs and keeps running (an `OperationCanceledException` from the stopping token ends the loop, per `AGENTS.md` section 3); topic discovery refreshed on an interval so a pattern topic created later is picked up; a readiness flag set when the end offsets observed at start are reached.
3. The caps of Design Choice 8: `MaxIndexedMessages` (default 100,000) and `MaxIndexedBytes` (default 256 MiB, summing the UTF-8 length of `RequestContent` and `ExceptionStackTrace` and a fixed overhead per message): when either is exceeded the index refuses new captures with `Left` (`dlq.index_over_cap`) and the health check turns unhealthy (fail closed).
4. The index applies the store's `TimeProvider` only for expiry views, never inside folding.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
- Phase 1 produced the records. The index is the only reader of the topics; the store reads the index and writes through the shared producer.

TASK:
Implement DeadLetterIndex and DeadLetterIndexService as described in the Phase 2 Tasks.

KEY RULES:
- Folding is deterministic: it uses only data inside the records, never the clock or the order of arrival across partitions.
- A background loop never lets an exception escape: catch per iteration, log by error code or exception type, keep running; only the stopping token ends the loop (AGENTS.md section 3).
- Thread safety: readers never see a half-applied record.
- Keep every method at CRAP <= 10.

REFERENCE FILES:
- src/Encina.Kafka/KafkaMessagePublisher.cs
- tests/Encina.TestInfrastructure/Fixtures/KafkaFixture.cs
- docs/en/guides/TESTING.md (timer and loop rules)
```

</details>

---

### Phase 3: `DeadLetterStoreKafka`

> **Goal**: the 10 contract methods (`AddAsync`, `GetAsync`, `GetMessagesAsync`, `GetCountAsync`, `TryClaimForReplayAsync`, `MarkAsReplayedAsync`, `DeleteAsync`, `DeleteManyAsync`, `DeleteExpiredAsync`, `SaveChangesAsync`).

<details>
<summary>Tasks</summary>

1. `DeadLetterStoreKafka : IDeadLetterStore`; constructor `(IProducer<string, byte[]> producer, DeadLetterIndex index, TimeProvider timeProvider, ILogger<DeadLetterStoreKafka> logger, IOptions<DeadLetterKafkaOptions>? options = null)`.
2. `AddAsync`: fast path (the index holds the key: `Right(false)`); else produce the capture record (key `SourceMessageId`, value JSON, headers), await the delivery report, `WaitUntilAsync` its offset, then `Right(true)` iff the index's winner for the key has this `Id`, else `Right(false)`; a produce failure or an index wait timeout is a `Left` (error type only). When the wait times out the record is already on the broker, so the capture is effective although the caller got a `Left`: a retry of the same capture finds the key in the index and returns `Right(false)` (idempotent); this is documented and tested.
   - **ErrorCategory (ADR-062)**: `src/Encina.Kafka/` is under the pending prefix `P-Kafka` of `ErrorCategoryPending.txt`, so the completeness scan does not fail today, but the plan states every category so the prefix can be removed without a new decision: `dlq.store_failed` `Internal` (R4: store and serialization failures), `dlq.store_unavailable` `Unavailable` (the broker is unreachable or the delivery report times out), `dlq.index_not_ready` `Unavailable` (catch-up not finished, fail closed), `dlq.index_over_cap` `Unavailable` (fail closed, clears when the queue shrinks), `dlq.index_wait_timeout` `Timeout`. A lost claim and a repeated capture are `Right(false)`, not errors.
3. `GetAsync`, `GetMessagesAsync`, `GetCountAsync` read the index (filter predicates as small methods; order `DeadLetteredAtUtc`, `Id`; `newestFirst` reverses).
4. `TryClaimForReplayAsync`, `MarkAsReplayedAsync`: resolve the source key from the index by `Id` (absent: `Right(false)`), produce the lifecycle record keyed by the source key, wait for its offset, decide from the folded state (Design Choice 2). If the wait times out the record is on the broker and may be effective: for a claim the live claim blocks any other replay until `ReplayClaimTimeout` passes (the claiming caller got a `Left` and does not replay), which is documented and tested; a replayed record is idempotent.
5. `DeleteAsync`, `DeleteManyAsync`, `DeleteExpiredAsync` (`ExpiresAtUtc <= timeProvider.GetUtcNow()`): produce a `DeletedRecord` per match and count those whose fold removed a message; a produce failure stops and returns `Left` (ADR-046 decision 7). `SaveChangesAsync` returns `Right(Unit)`.
6. Not-ready index (startup catch-up, or the cap exceeded) returns `Left` with a distinct code; it never answers from a partial index.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
- Phases 1 and 2 created the records, topic names, the index and its service. The contract is src/Encina.Messaging/DeadLetter/IDeadLetterStore.cs.

TASK:
Implement DeadLetterStoreKafka as described in the Phase 3 Tasks.

KEY RULES:
- Either<EncinaError, T> everywhere; produce failures and index timeouts are Left, never success.
- Time from TimeProvider; the replay result stored is a code, never error text.
- The idempotent producer (EnableIdempotence) prevents broker duplicates only; source-key uniqueness is the arbitration of Design Choice 2.
- Keep every method at CRAP <= 10.

REFERENCE FILES:
- src/Encina.MongoDB/DeadLetter/DeadLetterStoreMongoDB.cs
- src/Encina.Messaging/DeadLetter/DeadLetterFilter.cs
- tests/Encina.ContractTests/Messaging/DeadLetter/DeadLetterStoreContract.cs
```

</details>

---

### Phase 4: Configuration, DI, health and observability

<details>
<summary>Tasks</summary>

1. `DeadLetterKafkaOptions`: `TopicPrefix` (default `dlq-`), `CreateTopicsIfMissing` (false), `Partitions` (default 3), `ReplicationFactor` (default 1), `RetentionMs` (no default, Design Choice 7: startup validation fails when it is unset or `-1` unless `AllowInfiniteRetention` is `true`, which is logged once at startup), `AllowInfiniteRetention` (default `false`), `MaxIndexedMessages` (100,000), `MaxIndexedBytes` (256 MiB), `IndexWaitTimeout` (default 10 s), `AddHealthCheck`.
2. `AddEncinaDeadLetterKafka(this IServiceCollection, Action<DeadLetterKafkaOptions>? = null, Action<EncinaKafkaOptions>? = null, Action<DeadLetterOptions>? = null)`: shared producer registration helper (`TryAdd`), `TryAddSingleton` for the index, the consumer factory and the options, `TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, DeadLetterIndexService>())` so a second call never starts a second consumer, `TryAddSingleton(TimeProvider.System)`, `AddDeadLetterQueueServices<DeadLetterStoreKafka, DeadLetterKafkaMessageFactory>(true, options)` (the store is `TryAddScoped` and takes the singleton index and producer, which is a valid scoped-to-singleton dependency).
3. Health: `DeadLetterKafkaHealthCheck : IEncinaHealthCheck`: unhealthy while the index is not ready or over its cap; otherwise delegates to the existing `KafkaHealthCheck`.
4. DI test with `ValidateOnBuild` and `ValidateScopes`, both registration orders (with and without `AddEncinaKafka`).
5. EventIds 4156-4175 inside the existing `Kafka` range (4150-4199), `Log.cs` additions packed from 4156: 4156 index catch-up complete, 4157 capture duplicate, 4158 claim lost, 4159 produce failure, 4160 index over cap, 4161 index wait timeout, 4162 infinite retention opted in, 4163 index consumer loop error; 4164-4175 stay reserved and unused. Update the range comment in `EventIdRanges.cs`. The log additions land with the index service (PR 2 of the split), because it logs. `InstrumentedDeadLetterStore` decorates the store; add a test.
6. Tenant (ADR-054 D4): the tenant is a span attribute and a metric tag when multi-tenancy is on; `InstrumentedDeadLetterStore` on main never carries it, so this plan **defers** the tenant tag on the decorator (a decorator-wide change for every store; issue to open at implementation time), and the store writes no tenant id to logs. The tenant stays a record field and the plaintext header of Phase 1 task 3.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
- Registration of the queue is shared (AddDeadLetterQueueServices in src/Encina.Messaging/MessagingServiceCollectionExtensions.cs:495, first wins; the store is TryAddScoped and takes the singleton index and producer). AddEncinaKafka registers the producer today; the dead letter registration must not require it to be called first.

TASK:
Create the options, the registration, the health check, the log additions and the DI test of Phase 4.

KEY RULES:
- Registration completeness proven with ValidateOnBuild and ValidateScopes.
- EventIds packed sequentially from 4156 inside 4156-4175, in the already registered Kafka range.
- Never log EncinaError.Message or exception messages (the tenant tag on telemetry is deferred, Phase 4 task 6).
- RetentionMs has no default; infinite retention needs AllowInfiniteRetention and is logged.
- The hosted service is registered with TryAddEnumerable; the index and consumer factory with TryAddSingleton.

REFERENCE FILES:
- src/Encina.Kafka/ServiceCollectionExtensions.cs
- src/Encina.Kafka/Health/KafkaHealthCheck.cs
- src/Encina.Kafka/Log.cs
```

</details>

---

### Phase 5: Testing

> **Goal**: every applicable flag reaches its target in `.github/coverage-manifest/Encina.Kafka.json` (the package tracks `unit` and `guard` today; the new entries add the integration and property flags for the new files; each pull request adds the rows of the files it contains, targets never go down).

Per-file targets (percent of coverable lines per flag; contract is 0 for every file because the derived `DeadLetterStoreContract` class needs a real broker and runs in `Encina.IntegrationTests`, where it counts for the integration flag):

| File | Unit | Guard | Integration | Property | Justification |
|------|-----:|------:|------------:|---------:|---------------|
| `DeadLetterStoreKafka.cs` | 80 | 90 | 85 | 0 | Fake-producer unit tests reach the branches and the broker suite reaches produce, wait and race paths; the property suite targets the index. |
| `DeadLetterIndex.cs` | 95 | 0 | 80 | 90 | Internal type (no `InternalsVisibleTo` for the guard project); the fold is pure and branch-heavy, so unit and property tests carry it. |
| `DeadLetterIndexService.cs` | 75 | 0 | 80 | 0 | Internal `BackgroundService`; the loop is driven with a fake clock and a fake consumer, the real consumer is reached by the broker suite. |
| `DeadLetterConsumerFactory.cs` | 85 | 90 | 70 | 0 | Builds a consumer config from options with null checks; the broker suite proves the config works. |
| `DeadLetterKafkaRecords.cs` | 90 | 0 | 80 | 90 | Internal records with source-generated serialization; the property suite round-trips them. |
| `DeadLetterTopicNames.cs` | 95 | 90 | 60 | 90 | Pure mapping with a null/blank guard; the property suite proves injectivity. |
| `DeadLetterKafkaMessage.cs` | 90 | 0 | 80 | 0 | Data class with `IsReplayed`/`IsExpiredAt` logic and no validated parameters. |
| `DeadLetterKafkaMessageFactory.cs` | 95 | 90 | 70 | 0 | One `Create` method with a null check. |
| `DeadLetterKafkaOptions.cs` | 90 | 0 | 0 | 0 | Properties and the startup validation of retention; no I/O. |
| `DeadLetterKafkaHealthCheck.cs` | 85 | 90 | 70 | 0 | Unit tests cover ready, not ready and over-cap; the broker suite covers the real delegate to `KafkaHealthCheck`. |
| `ServiceCollectionExtensions.cs` (the Kafka file, touched) | 95 (unchanged) | 40 (from 38) | 0 | 0 | The shared producer helper and the new method are reached by the DI test and by the null guards of the new method; the existing targets never go down. |
| `Log.cs` (Kafka, touched) | 0 | 0 | 0 | 0 | Source-generated `[LoggerMessage]` delegates with no logic: the manifest rule of this file has no test flag (guard tests cannot reach generated partials). |

<details>
<summary>Tasks</summary>

1. **Unit**: topic names (escaping, reserved name), index folding (first capture wins, a capture after a delete is accepted, effective and ineffective claims, replay only once, delete), filter predicates, error mapping and its categories, retention startup validation, with a fake producer and a `FakeTimeProvider`.
2. **Guard**: every public method and the registration.
3. **Contract/integration**: `DeadLetterStoreContract` against the Kafka container (`KafkaFixture`, shared `[Collection]`, unique prefix per run), plus: two stores racing on one source key (exactly one `Right(true)`), two stores racing on one claim, delete followed by re-capture of the same key seen identically by two stores, restart of the index (a new store over the same topics sees the same state), the cap and not-ready paths, the wait-timeout paths of Phase 3 (a retried capture returns `Right(false)`; a timed-out claim blocks replay until `ReplayClaimTimeout`).
4. **Property**: folding the records of one source key gives the same state for every interleaving of the records of different keys (different partitions); a delete followed by a re-capture folds to the same state whatever instance reads first; topic-name mapping is injective.
5. **Load**: capture throughput as a load test only if the maintainer wants it; otherwise `.md` justification; **Benchmark**: `.md` justification, per `AGENTS.md` section 9.
6. Report per-flag coverage of the new files and the CRAP table.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
- Tests execute real package code; the broker is the shared Kafka container.

TASK:
Write the tests of Phase 5 and the manifest entries.

KEY RULES:
- Shouldly via Encina.Testing.Shouldly; shared [Collection] fixture, never IClassFixture for containers; no Thread.Sleep.
- A test never swallows an exception thrown by the code it drives; background loops are tested with the fake clock.
- Targets never go down; justify every target in one sentence.

REFERENCE FILES:
- tests/Encina.IntegrationTests/MessageBrokers/Kafka/KafkaHealthCheckIntegrationTests.cs
- tests/Encina.UnitTests/Kafka/Publishing/KafkaMessagePublisherTests.cs
- tests/Encina.ContractTests/Messaging/DeadLetter/DeadLetterStoreContract.cs
```

</details>

---

### Phase 6: Documentation and finalization

<details>
<summary>Tasks</summary>

1. XML docs on every public API; `src/Encina.Kafka` README section; a section in `docs/features/dead-letter-queue.md` (topic names, record format for external consumers, retention and erasure limits, memory and startup cost).
2. `changelog.d/587-kafka-dead-letter-store.added.md`.
3. ADR for the log-arbitrated index; number assigned when the ADR pull request opens.
4. Build 0 warnings; coverage and CRAP reported.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
- Documentation: Encina.Kafka README, docs/features/dead-letter-queue.md, changelog fragment, ADR.

TASK:
Complete the documentation tasks of Phase 6.

KEY RULES:
- Never edit CHANGELOG.md; use changelog.d/.
- No hand-typed coverage figures; English only; never name the reference application.

REFERENCE FILES:
- docs/features/dead-letter-queue.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Topic | Requirement |
|--------|-------|-------------|
| SPEC-000 DEC-008, REQ-030 | Dead letter queue in 1.0 | #584-#589 are additional backends (milestone v0.19.0) |
| ADR-046 | Persistent dead letter queue | Oldest first, `Right(false)` on a repeated source key, atomic claim, set-based deletes, `TimeProvider`, `TenantId`, no exception text |
| ADR-029 | Recoverability | Permanent failures reach the dead letter queue |
| AGENTS.md section 3 | Background loops | A loop catches per iteration and never lets an exception escape |
| Apache Kafka documentation (vendor) | Ordering, idempotent producer, retention, compaction | Order is per partition; idempotence prevents broker duplicates; retention and compaction are topic configuration |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in a new IDeadLetterStore backend |
|-----------|----------|------------------------------------------|
| `IDeadLetterStore` | `src/Encina.Messaging/DeadLetter/IDeadLetterStore.cs` | The contract |
| `IDeadLetterMessage`, `DeadLetterData`, `IDeadLetterMessageFactory` | `src/Encina.Messaging/DeadLetter/` | The record and its factory |
| `DeadLetterFilter`, `DeadLetterErrorCodes`, `DeadLetterStoreLimits`, `DeadLetterSourcePatterns` | `src/Encina.Messaging/DeadLetter/` | Filter translation, error codes, limits, the six topic suffixes |
| `AddDeadLetterQueueServices` | `src/Encina.Messaging/MessagingServiceCollectionExtensions.cs:495` (wrappers in `src/Encina.Messaging/DeadLetter/DeadLetterServiceCollectionExtensions.cs`) | First-wins registration; the store is `TryAddScoped` |
| `AddEncinaKafka`, `EncinaKafkaOptions`, `IProducer<string, byte[]>` | `src/Encina.Kafka/ServiceCollectionExtensions.cs`, `EncinaKafkaOptions.cs` | The shared client configuration |
| `KafkaHealthCheck` | `src/Encina.Kafka/Health/KafkaHealthCheck.cs` | Delegated connectivity probe |
| `InstrumentedDeadLetterStore` | `src/Encina.OpenTelemetry/MessagingStores/InstrumentedDeadLetterStore.cs` | Tracing and metrics |
| `DeadLetterStoreContract` | `tests/Encina.ContractTests/Messaging/DeadLetter/DeadLetterStoreContract.cs` | Shared behavior suite |
| `KafkaFixture` | `tests/Encina.TestInfrastructure/Fixtures/KafkaFixture.cs` | Container fixture (`Testcontainers.Kafka` 4.15.0) |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Kafka` | 4156-4175 (inside `Kafka` 4150-4199) | Ids 4150-4155 are used today; no new range is needed |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production code (Phases 1-4) | ~13 | Message, factory, records, topic names, index, index service, consumer factory, store, options, registration, health check, `Log.cs` additions, `EventIdRanges.cs` (no `PublicAPI` files: the package has none) |
| Tests (Phases 1-5) | ~14 | Unit, guard, DI test, decorator test, contract/integration, property |
| Manifest rows, README, feature page, changelog, ADR, load/benchmark justification files | ~8 | Documentation phase and per-PR manifest rows |
| **Total** | **~35** | Exceeds 15 files. Tests and log additions travel with the code that needs them; each pull request builds and passes alone |

Proposed split (each at most 15 files and 1500 lines, counting tests, manifest rows, `EventIdRanges.cs` and test `ProjectReference` edits):

| PR | Content | Files | Estimated lines | Verifiable alone because |
|----|---------|-------|-----------------|--------------------------|
| 1 | Message, factory, records, topic names, `DeadLetterKafkaOptions`, their unit and guard tests, manifest rows | ~10 | ~900 | Pure types and options with their own tests |
| 2 | Index, index service, consumer factory, `Log.cs` additions, `EventIdRanges.cs` range comment, unit and guard tests (fold, loop with a fake clock), manifest rows | ~10 | ~1,300 | The index builds with its log and is tested with a fake consumer |
| 3 | Store (10 contract methods) with its error codes and categories, unit and guard tests, manifest rows | ~6 | ~1,000 | The store is tested against a fake producer and the index of PR 2 |
| 4 | `AddEncinaDeadLetterKafka`, shared producer helper, health check, DI test (`ValidateOnBuild`, `ValidateScopes`), health and guard tests, decorator test, manifest rows | ~8 | ~700 | DI test against the store of PR 3 |
| 5 | Derived contract class and integration cases on the Kafka container, property tests, load and benchmark justification files, manifest rows, test-project `ProjectReference` edits if needed | ~8 | ~1,100 | Integration and property flags reach their targets on a real broker |
| 6 | Package README section, `docs/features/dead-letter-queue.md`, changelog fragment, ADR | ~4 | ~400 | Documentation only |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution first.
- Railway Oriented Programming: Either<EncinaError, T>; errors never swallowed; TimeProvider for time; async with CancellationToken.
- Issue #587: DeadLetterStoreKafka implements IDeadLetterStore (ADR-046) in Encina.Kafka using dead letter topics and a log-backed index.

IMPLEMENTATION OVERVIEW:
Phase 1: message, factory, capture and lifecycle records, topic names and headers.
Phase 2: DeadLetterIndex (deterministic folding, first capture wins, effective claims) and DeadLetterIndexService (manual assignment, per-iteration catch).
Phase 3: DeadLetterStoreKafka, 10 methods, produce then wait for the local index to reach the offset.
Phase 4: options, AddEncinaDeadLetterKafka reusing the transport producer, health check, EventIds 4156-4175.
Phase 5: unit, guard, contract/integration (Kafka container), property tests; manifest.
Phase 6: docs, changelog fragment, ADR.

KEY PATTERNS:
- Same Kafka client configuration as the publisher (AGENTS.md section 5); store naming DeadLetterStoreKafka.
- Dilemmas D1-D6 are recommended, pending the maintainer: do not start Phase 2 before they are answered.
- Every state-changing record of a source key goes to that key's partition (one ordering domain); folding is per key.
- Erasure on demand is not possible with logical delete; retention is the erasure mechanism and has no default (infinite is an explicit, logged opt-in); message encryption is an application choice.
- Every new error code carries its ADR-062 ErrorCategory; Encina.Kafka has no PublicAPI files.

REFERENCE FILES:
- src/Encina.Messaging/DeadLetter/ (contract, registration)
- src/Encina.Kafka/ (publisher, options, health check, Log.cs)
- tests/Encina.ContractTests/Messaging/DeadLetter/DeadLetterStoreContract.cs
- docs/architecture/adr/046-persistent-dead-letter-queue.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | The queue is write-and-inspect; the index is the store's own state, not a cache of another store. |
| 2 | OpenTelemetry | ✅ | `InstrumentedDeadLetterStore` decorates the store; the producer span is separate. |
| 3 | Structured logging | ✅ | Phase 4: ids 4156-4175 in the registered `Kafka` range, error codes and exception types only. |
| 4 | Health checks | ✅ | Phase 4: `DeadLetterKafkaHealthCheck` (index readiness and cap) over the existing `KafkaHealthCheck`. |
| 5 | Validation | ✅ | Contract argument rules throw before I/O; input rules live once in the orchestrator and manager (ADR-046 decision 6). |
| 6 | Resilience | ✅ | A failed produce or a lagging index maps to `Left`; the idempotent producer retries inside the client. |
| 7 | Distributed locks | ❌ | The claim is arbitrated by log order (Design Choice 2), not by a lock. |
| 8 | Transactions | ❌ | The capture runs in its own scope by design (ADR-046 decision 11), so there is no application transaction for a Kafka transaction to join. |
| 9 | Idempotency | ✅ | Idempotent producer for broker duplicates plus log-order arbitration for the source key. |
| 10 | Multi-tenancy | ✅ | `TenantId` in the record and as a header, filtered only when the filter names it (ADR-046 Choice 7); tenant-partitioned topics are not planned. |
| 11 | Module isolation | ❌ | `IDeadLetterMessage` has no `ModuleId` today. |
| 12 | Audit trail | ⏭️ | The pattern topics (capture plus lifecycle records) are the store's own history; operator replay and delete audit is #2013 (issue exists). |

---

## Prerequisites & Dependencies

- #583 (closed) fixed the contract; #580 (Kafka audit store, open) is independent. #2013 (open) later adds `ActorId` and `CausationId` to the record.
- No new package versions are needed (`Confluent.Kafka`, `Testcontainers.Kafka` are pinned).
- No decision of another issue blocks this plan; D1-D6 block Phase 2.

---

## Next Steps

1. The maintainer answers D1 to D6 when work on #587 starts; the answers go into `## Maintainer Decisions`.
2. Re-check the Design Choices against main; `adversarial-reviewer` verifies the Settled ones.
3. Brief a worker per proposed pull request (6 pull requests, see the file count).
4. Final commit references `Fixes #587`.
