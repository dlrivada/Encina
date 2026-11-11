# Implementation Plan: `DeadLetterStoreKafka` — Apache Kafka backend for the persistent dead letter queue

> **Issue**: [#587](https://github.com/dlrivada/Encina/issues/587)
> **Type**: Feature
> **Complexity**: High (6 phases, 1 provider, a log-backed index, ~16 source files + tests)
> **Estimated Scope**: ~1,200-1,600 lines of production code + ~1,800-2,400 lines of tests

---

## Summary

Implement `Encina.Messaging.DeadLetter.IDeadLetterStore` on Apache Kafka as `DeadLetterStoreKafka`, in the existing `Encina.Kafka` package, using dedicated dead letter topics per source pattern (the standard dead letter topic pattern of Kafka ecosystems, so external consumers can read them). A Kafka topic is an append-only log; the ADR-046 contract also needs reads by id and filter with paging, exact counts, an atomic replay claim, a unique `(SourcePattern, SourceMessageId)` key (`AddAsync` returns `Right(false)` for a repeat) and deletes. The plan resolves that gap with a **log-backed index**: the topics stay the single source of truth, each instance folds them into an in-process index, and the order of the log arbitrates uniqueness and claims (Design Choices 1 and 2).

What the code looks like today (main `7a0049c07`):

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

- The queue is an exception path, so it is small enough to hold in memory (a configurable cap makes the store fail closed with a `Left` and an unhealthy health check when exceeded, instead of silently degrading).
- A wins if the maintainer decides Kafka dead letters are consumed only by Kafka tooling and the manager is not used with this backend; it needs a core abstraction, which is why it is not recommended for a backend issue.
- Multi-instance use: every instance consumes all partitions of all dead letter topics (manual assignment from the beginning, no committed group offsets), so each instance holds the same index.

</details>

<details>
<summary><strong>2. Uniqueness of the source key and the atomic claim (Dilemma D2)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Key the record by the source key and rely on log compaction** | Simplest; duplicates collapse eventually | `AddAsync` always returns `Right(true)`: the contract's `Right(false)` does not hold; compaction keeps the last record, not the first, so the surviving `Id` can be the loser's |
| **B) Arbitration by log order: produce, wait until the local index has seen the record's offset, and decide from the folded state** | The contract holds across instances: the first capture of a source key in its partition wins; a claim event is effective only if, at its position in the key's order, the message is unreplayed and holds no live claim | A write waits for the local consumer to catch up (bounded timeout, then `Left`); needs deterministic folding (events carry the instants they were decided with) |

### Chosen Option: **B — arbitration by log order** (recommended, pending the maintainer)

### Rationale

- Capture records are keyed by `SourceMessageId` within the topic of their `SourcePattern`, so all captures of one source key land in one partition and have one total order; lifecycle events (claim, replay, delete) are keyed by the message `Id` in the lifecycle topic, again one partition per message. The first capture wins; a repeat is detected by the fast path (the index already holds the key) or by arbitration when two instances race.
- A claim event carries a claim id, the claim instant and `ClaimExpiredBeforeUtc` as given by the caller, so every instance folds it to the same result without reading a clock. `TryClaimForReplayAsync` returns `Right(true)` only when the folded current claim id equals its own.
- A wins only where `Right(false)` is not needed and a stale duplicate is tolerable, which ADR-046 Choice 6 rejects (a duplicate dead letter makes a replay dispatch twice).

</details>

<details>
<summary><strong>3. What delete means on a log and how physical removal happens (Dilemma D3)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Logical delete by a `Deleted` lifecycle event; bytes leave the broker when topic retention expires** (`cleanup.policy=delete`, `retention.ms` configurable) | Keeps history; no compaction subtleties; uniqueness arbitration stays correct | Erasure on demand is not possible: personal data in the payload stays readable until retention ends |
| **B) Compacted capture topics with a tombstone on delete** (`cleanup.policy=compact,delete`) | Physical removal of the payload after compaction | Compaction keeps the last record per key, which interacts with the arbitration of Design Choice 2 (duplicates of a source key are never produced when the fast path hits, but a race can leave a loser as the survivor); more operational tuning |

### Chosen Option: **A — logical delete, retention for physical removal** (recommended, pending the maintainer)

### Rationale

- The default topic retention is infinite (`retention.ms=-1`): a dead letter must not vanish before an operator replays it. An application with an erasure obligation sets a finite retention that matches its policy, and encrypts dead-lettered content through `IMessageSerializer` (message encryption), which already applies to `RequestContent`.
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
**Settled by:** AGENTS.md section 5 (transport category: same client configuration so switching transports stays a registration change), AGENTS.md section 4 (`{Pattern}Store{Provider}`), AGENTS.md section 3 (registration completeness), AGENTS.md section 9 (shared `[Collection]` fixtures), #587

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `DeadLetterStoreKafka` in `Encina.Kafka`; reuses the registered `IProducer<string, byte[]>` and `EncinaKafkaOptions` (bootstrap servers, acks, idempotence); `AddEncinaDeadLetterKafka()` registers the producer with `TryAdd` through the same internal helper as `AddEncinaKafka`, so the DI test builds without calling `AddEncinaKafka` first** | One Kafka configuration; the store wins in any order | The producer registration is extracted into a shared internal method |
| **B) The store builds its own producer from its own options** | Independent | A second client configuration; switching transports is no longer one registration |

### Chosen Option: **A — reuse the transport client**

### Rationale

- The topic names are `{TopicPrefix}{pattern}` with the prefix `dlq-` and the pattern in lower invariant case (`dlq-recoverability`, `dlq-outbox`, `dlq-inbox`, `dlq-scheduling`, `dlq-saga`, `dlq-choreography`); characters Kafka does not allow in topic names are escaped as `_xx`; the name `lifecycle` is reserved for the events topic `dlq-lifecycle`. Topic creation is opt-in (`CreateTopicsIfMissing`, default `false`).
- Integration tests use the shared Kafka container fixture and a unique topic prefix per test run, since topics cannot be truncated cheaply.

</details>

---

## Implementation Phases

### Phase 1: Records, serialization and topic naming

> **Goal**: the persisted shapes and names.

<details>
<summary>Tasks</summary>

1. `src/Encina.Kafka/DeadLetter/DeadLetterKafkaMessage.cs`: public sealed class implementing `IDeadLetterMessage` and `DeadLetterKafkaMessageFactory.cs`.
2. `DeadLetterKafkaRecords.cs` (internal): `CaptureRecord` (the full message), and lifecycle records `ClaimRecord(Id, ClaimId, ClaimedAtUtc, ClaimExpiredBeforeUtc)`, `ReplayedRecord(Id, ReplayedAtUtc, ReplayResult)`, `DeletedRecord(Id, DeletedAtUtc)`; System.Text.Json source generation.
3. `DeadLetterTopicNames.cs`: topic per pattern with the escaping and the reserved `lifecycle` name; headers: `encina-dlq-id`, `encina-dlq-source-pattern`, `encina-dlq-exception-type`, `encina-dlq-retry-attempts`, `encina-dlq-tenant-id`; never exception text.
4. `PublicAPI.Unshipped.txt` lines.

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

1. `DeadLetterIndex.cs` (internal, thread-safe): by `Id`, by `(SourcePattern, SourceMessageId)`, and a sorted view by `DeadLetteredAtUtc` then `Id`; `Apply(CaptureRecord, position)`, `Apply(ClaimRecord, position)`, `Apply(ReplayedRecord, position)`, `Apply(DeletedRecord, position)` folding with the rules of Design Choice 2 (first capture of a key wins; a claim is effective only while unreplayed and without a live claim; replay only while unreplayed; delete removes); `WaitUntilAsync(topic, partition, offset, timeout, ct)` for read-your-writes.
2. `DeadLetterIndexService.cs` (`BackgroundService`): one consumer with manual partition assignment from the beginning for every dead letter topic and the lifecycle topic (no group, no committed offsets); a per-iteration `try`/`catch` that logs and keeps running (an `OperationCanceledException` from the stopping token ends the loop, per `AGENTS.md` section 3); topic discovery refreshed on an interval so a pattern topic created later is picked up; a readiness flag set when the end offsets observed at start are reached.
3. A cap `MaxIndexedMessages` (default 100,000): when exceeded the index refuses new captures with `Left` and the health check turns unhealthy (fail closed).
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

> **Goal**: the 11 contract methods.

<details>
<summary>Tasks</summary>

1. `DeadLetterStoreKafka : IDeadLetterStore`; constructor `(IProducer<string, byte[]> producer, DeadLetterIndex index, TimeProvider timeProvider, ILogger<DeadLetterStoreKafka> logger, IOptions<DeadLetterKafkaOptions>? options = null)`.
2. `AddAsync`: fast path (the index holds the key: `Right(false)`); else produce the capture record (key `SourceMessageId`, value JSON, headers), await the delivery report, `WaitUntilAsync` its offset, then `Right(true)` iff the index's winner for the key has this `Id`, else `Right(false)`; timeout or produce failure is `Left` (`dlq.store_failed`, error type only).
3. `GetAsync`, `GetMessagesAsync`, `GetCountAsync` read the index (filter predicates as small methods; order `DeadLetteredAtUtc`, `Id`; `newestFirst` reverses).
4. `TryClaimForReplayAsync`, `MarkAsReplayedAsync`: produce the lifecycle record keyed by `Id`, wait for its offset, decide from the folded state (Design Choice 2).
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

1. `DeadLetterKafkaOptions`: `TopicPrefix` (default `dlq-`), `CreateTopicsIfMissing` (false), `Partitions` (default 3), `ReplicationFactor` (default 1), `RetentionMs` (default -1), `MaxIndexedMessages` (100,000), `IndexWaitTimeout` (default 10 s), `AddHealthCheck`.
2. `AddEncinaDeadLetterKafka(this IServiceCollection, Action<DeadLetterKafkaOptions>? = null, Action<EncinaKafkaOptions>? = null, Action<DeadLetterOptions>? = null)`: shared producer registration helper (`TryAdd`), the index and its hosted service, `TryAddSingleton(TimeProvider.System)`, `AddDeadLetterQueueServices<DeadLetterStoreKafka, DeadLetterKafkaMessageFactory>(true, options)`.
3. Health: `DeadLetterKafkaHealthCheck : IEncinaHealthCheck`: unhealthy while the index is not ready or over its cap; otherwise delegates to the existing `KafkaHealthCheck`.
4. DI test with `ValidateOnBuild` and `ValidateScopes`, both registration orders (with and without `AddEncinaKafka`).
5. EventIds 4156-4175 inside the existing `Kafka` range (4150-4199): `Log.cs` additions for index catch-up, capture duplicate, claim lost, produce failure, index over cap; update the range comment in `EventIdRanges.cs`. `InstrumentedDeadLetterStore` decorates the store; add a test. No tenant id in logs or tags.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
- Registration of the queue is shared (AddDeadLetterQueueServices, first wins). AddEncinaKafka registers the producer today; the dead letter registration must not require it to be called first.

TASK:
Create the options, the registration, the health check, the log additions and the DI test of Phase 4.

KEY RULES:
- Registration completeness proven with ValidateOnBuild and ValidateScopes.
- EventIds packed sequentially inside 4156-4175, in the already registered Kafka range.
- Never log EncinaError.Message, exception messages or tenant ids.

REFERENCE FILES:
- src/Encina.Kafka/ServiceCollectionExtensions.cs
- src/Encina.Kafka/Health/KafkaHealthCheck.cs
- src/Encina.Kafka/Log.cs
```

</details>

---

### Phase 5: Testing

> **Goal**: every applicable flag reaches its target in `.github/coverage-manifest/Encina.Kafka.json` (new entries per new file with justifications; the package has `unit` and `guard` targets today).

<details>
<summary>Tasks</summary>

1. **Unit**: topic names (escaping, reserved name), index folding (first capture wins, effective and ineffective claims, replay only once, delete), filter predicates, error mapping, with a fake producer and a `FakeTimeProvider`.
2. **Guard**: every public method and the registration.
3. **Contract/integration**: `DeadLetterStoreContract` against the Kafka container (`KafkaFixture`, shared `[Collection]`, unique prefix per run), plus: two stores racing on one source key (exactly one `Right(true)`), two stores racing on one claim, restart of the index (a new store over the same topics sees the same state), the cap and not-ready paths.
4. **Property**: folding is independent of the interleaving of partitions; topic-name mapping is injective.
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
| `AddDeadLetterQueueServices` | `src/Encina.Messaging/DeadLetter/DeadLetterServiceCollectionExtensions.cs` | First-wins registration |
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
| Production code (Phases 1-4) | ~12 | Message, factory, records, topic names, index, index service, store, options, registration, health check, log additions, `PublicAPI` |
| Tests (Phase 5) | ~8 | Unit, guard, contract/integration, property |
| Manifest, README, feature page, changelog, ADR | ~5 | Documentation phase |
| **Total** | **~25** | Exceeds 15 files. Proposed split: PR 1 = Phases 1-2 (records, names, index, index service, their tests); PR 2 = Phases 3-4 (store, registration, health, logging, tests); PR 3 = integration tests, documentation, ADR |

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
Phase 3: DeadLetterStoreKafka, 11 methods, produce then wait for the local index to reach the offset.
Phase 4: options, AddEncinaDeadLetterKafka reusing the transport producer, health check, EventIds 4156-4175.
Phase 5: unit, guard, contract/integration (Kafka container), property tests; manifest.
Phase 6: docs, changelog fragment, ADR.

KEY PATTERNS:
- Same Kafka client configuration as the publisher (AGENTS.md section 5); store naming DeadLetterStoreKafka.
- Dilemmas D1-D3 are recommended, pending the maintainer: do not start Phase 2 before they are answered.
- Erasure on demand is not possible with logical delete; document retention and message encryption.

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
| 12 | Audit trail | ⏭️ | The lifecycle topic is the store's own history; operator replay and delete audit is #2013 (issue exists). |

---

## Prerequisites & Dependencies

- #583 (closed) fixed the contract; #580 (Kafka audit store, open) is independent. #2013 (open) later adds `ActorId` and `CausationId` to the record.
- No new package versions are needed (`Confluent.Kafka`, `Testcontainers.Kafka` are pinned).
- No decision of another issue blocks this plan; D1-D3 block Phase 2.

---

## Next Steps

1. The maintainer answers D1, D2 and D3 when work on #587 starts; the answers go into `## Maintainer Decisions`.
2. Re-check the Design Choices against main; `adversarial-reviewer` verifies the Settled ones.
3. Brief a worker per proposed pull request (3 pull requests).
4. Final commit references `Fixes #587`.
