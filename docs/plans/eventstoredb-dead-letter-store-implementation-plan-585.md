# Implementation Plan: `DeadLetterStoreEventStoreDB` — EventStoreDB backend for the persistent dead letter queue

> **Issue**: [#585](https://github.com/dlrivada/Encina/issues/585)
> **Type**: Feature
> **Complexity**: Medium-High (6 phases, 1 provider, a new package shared with #576, ~18 source files + tests)
> **Estimated Scope**: ~1,100-1,500 lines of production code + ~1,800-2,400 lines of tests

---

## Summary

Implement `Encina.Messaging.DeadLetter.IDeadLetterStore` on EventStoreDB as `DeadLetterStoreEventStoreDB`, for applications that keep their events in EventStoreDB (named in `AGENTS.md` section 5 as the future event-sourcing provider next to Marten). Each dead letter lives in its own append-only stream; the lifecycle (captured, replay claimed, replayed, deleted) is the stream itself. The store implements all 11 methods of the ADR-046 contract. EventStoreDB has no secondary index, so the plan has to say how filtered reads, exact counts, the atomic claim, the unique `(SourcePattern, SourceMessageId)` key and deletes are satisfied (Design Choices 1 to 3).

What the code looks like today (main `7a0049c07`):

- There is **no** `Encina.EventStoreDB` package and no EventStoreDB client use in `src/`. `Directory.Packages.props` pins `EventStore.Client.Grpc.Streams` 23.3.9 and no `Testcontainers.EventStoreDb`. `docker-compose.yml` has no EventStoreDB service. The package is created by the first of this issue and #576 (EventStoreDB audit store) to merge; the other rebases onto it.
- The contract is `IDeadLetterStore` (`src/Encina.Messaging/DeadLetter/IDeadLetterStore.cs`), 11 methods returning `Either<EncinaError, T>`. There is no `DeadLetterMessage` type (it is `IDeadLetterMessage`), no `message.Source` (it is `SourcePattern`, values in `DeadLetterSourcePatterns`: Recoverability, Outbox, Inbox, Scheduling, Saga, Choreography) and no `GetBySourceAsync`. The issue's sample (`StreamState.NoStream` keyed by `{Source}-{Id}`, three methods) is replaced.
- The existing backends are `DeadLetterStoreADO`/`Dapper` (x3 each), `DeadLetterStoreEF`, `DeadLetterStoreMongoDB`, plus `InstrumentedDeadLetterStore` and `FakeDeadLetterStore`; the shared behavior suite is `tests/Encina.ContractTests/Messaging/DeadLetter/DeadLetterStoreContract.cs`.

**Provider category**: event sourcing, outside the 10-provider database matrix (`AGENTS.md` section 5). **Affected packages**: `Encina.EventStoreDB` (new, shared with #576), `Encina.Messaging` (no change), test projects, `Directory.Packages.props`.

---

## Design Choices

<details>
<summary><strong>1. Reads, counts and claims on a store with no secondary index (Dilemma D1)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Subscription or catch-up projection into a read model in another store** (Marten/PostgreSQL or an in-process index) | Fast filtered pages and counts for a large queue | A second storage system, which the issue's "unified immutable store" argument rejects; counts are eventually consistent; the claim still needs the primary store |
| **B) Stream reads with client-side filtering** (read the dead letter streams, fold each into its current state, filter, order, page) | One store; exact and consistent; no projection to run or rebuild | O(n) reads for every filtered page and count; fine for the small queues dead letters are, poor for hundreds of thousands |
| **C) A documented reduced capability** (writes plus reads by id and category; the rest returns a typed `Left`) | Smallest to build | Breaks provider coherence (`AGENTS.md` section 5: providers differ only in implementation) and every manager feature that filters, counts or claims |

### Chosen Option: **B — client-side folding of the dead letter streams, with a bounded Id-to-stream cache** (recommended, pending the maintainer)

### Rationale

- A dead letter queue is an exception path: queues are small, operators page through them, and exact consistent answers matter more than throughput. B keeps one store and passes the same contract suite as every other backend.
- The scan reads `$all` with a server-side `StreamFilter.Prefix("dlq.")`, so it needs no system projection (category streams `$ce-*` need `$by_category` running, an operational dependency the store would otherwise have to verify).
- A wins when the queue grows large and operators filter often: it can be added later behind the same store because the contract does not change. C wins only as a stop-gap and is rejected for the coherence rule.

</details>

<details>
<summary><strong>2. What delete means on an immutable store (Dilemma D2)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Append a `DeadLetterDeleted` tombstone, then soft-delete the stream** (scavenge reclaims space later) | Covers `DeleteAsync`, `DeleteManyAsync` and `DeleteExpiredAsync` uniformly, so the contract holds; the dead letter disappears from every read at once | Payload bytes stay on disk until scavenge runs; weaker history after deletion |
| **B) Stream metadata (`$maxAge`, `$tb`) set at capture to express `ExpiresAtUtc`** | Time-based expiry without a scan | Explicit deletes (by id, by filter) are not covered; `DeleteExpiredAsync` would not return a count |
| **C) A + B** | Expiry handled by the server, explicit deletes by tombstone | Two mechanisms to test; metadata must be set before the first read of an expired message |

### Chosen Option: **A — tombstone then soft delete** (recommended, pending the maintainer)

### Rationale

- The contract returns `Right(true)` when a message was deleted and a count for set-based deletes, so every delete path must be an explicit act; B alone cannot meet that.
- The payload (`RequestContent`) already passes through `IMessageSerializer`, so an application that must not leave readable personal data on disk before scavenge uses message encryption (`Encina.Messaging.Encryption`) for dead-lettered content; the plan documents this and does not add its own crypto.
- C wins when a very large expired backlog makes the scan of `DeleteExpiredAsync` expensive; it can be added without changing the contract.

</details>

<details>
<summary><strong>3. Stream identity and the unique source key (Dilemma D3)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Stream named from the source key** (`dlq.{SourcePattern}-{hash(SourceMessageId)}`), appended with `StreamState.NoStream`; an in-process, bounded Id-to-stream cache for `GetAsync(Guid)` | Uniqueness is one atomic append: a second capture fails the expected-state check and returns `Right(false)`; the pattern is the category | `GetAsync(Guid)` on a cold cache is a scan; a hash collision needs a guard (the first event stores the full key and is compared) |
| **B) Stream named from the message `Id`, plus a gate stream per source key appended first** | Direct lookup by id | Two appends are not atomic: a crash between them leaves a gate with no dead letter and every later capture of that source message wrongly returns `Right(false)` |
| **C) The store derives the message `Id` deterministically from the source key** (a name-based GUID) | Both lookups are direct | The store would override the `Id` the orchestrator assigned; callers that hold the original `Id` after `AddAsync` would diverge |

### Chosen Option: **A — stream from the source key with a bounded Id cache** (recommended, pending the maintainer)

### Rationale

- A is the only option whose uniqueness is atomic without overriding the caller's `Id`. A cache entry never goes stale (an `Id` never changes its stream); a miss falls back to the scan of Design Choice 1.
- B wins if `GetAsync` by id is on a hot path with a cold cache every time (it is not: the manager reads, then claims, then marks, all by id in one request). C wins if the maintainer accepts the store assigning identity.
- Whether `StreamState.NoStream` succeeds again after a soft delete (the source key may be captured again later) is verified in the integration tests of Phase 5 and documented; if it does not, a deleted source key could never be captured again and the delete design returns to the maintainer as a new question (an open verification item, not a decision made here).

</details>

<details>
<summary><strong>4. Contract and semantics — ADR-046</strong></summary>

**Class:** Settled
**Settled by:** ADR-046 (decisions 1, 5, 6, 7), ADR-001, ADR-006, AGENTS.md section 3 (TimeProvider; Left is never swallowed), SPEC-002 REQ-061

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Full ADR-046 contract: oldest first by `DeadLetteredAtUtc` then `Id`, `Right(false)` for a repeated source key, replay result as a code, "now" and expiry from `TimeProvider`, `TenantId` stored and filtered only when the filter names it** | Provider coherence; passes the shared contract suite | None |
| **B) A subset** | Less work | Breaks coherence |

### Chosen Option: **A — the full contract**

### Rationale

- The contract and its argument rules (`skip >= 0`, `1 <= take <= DeadLetterStoreLimits.MaxPageSize`, non-empty id, non-blank result) are fixed by ADR-046 and enforced by `DeadLetterStoreContract`, which this store must pass unchanged.

</details>

<details>
<summary><strong>5. Package, naming, registration and tests</strong></summary>

**Class:** Settled
**Settled by:** AGENTS.md section 3 (event-sourced stores: no InMemory variant; registration completeness), AGENTS.md section 4 (`{Pattern}Store{Provider}`), AGENTS.md section 5 (EventStoreDB is the event sourcing provider next to Marten), #576

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New package `Encina.EventStoreDB` (shared with #576); store `DeadLetterStoreEventStoreDB`; `AddEncinaDeadLetterEventStoreDB()` over the shared `AddDeadLetterQueueServices`; real EventStoreDB container for integration tests, NSubstitute for unit tests** | Matches `Encina.Marten`; one package for both EventStoreDB issues; no InMemory event-sourced store | The first of #576 and #585 to merge creates the package and the client registration |
| **B) A package per feature (`Encina.EventStoreDB.DeadLetter`)** | Independent versions | Two packages to configure one client |

### Chosen Option: **A — one `Encina.EventStoreDB` package**

### Rationale

- The issue itself says the package is shared. The client (`EventStoreClient`) is registered once (`TryAddSingleton`) by `AddEncinaEventStoreDB`; this feature adds only its own registration.
- The test container is `Testcontainers.EventStoreDb` (to be added to `Directory.Packages.props`; the exact image tag is the vendor LTS, per the default-images rule of the project, and is confirmed in Phase 5).

</details>

---

## Implementation Phases

### Phase 1: Package, client registration and event model

> **Goal**: the package skeleton (skipped if #576 already created it) and the persisted shapes.

<details>
<summary>Tasks</summary>

1. `src/Encina.EventStoreDB/Encina.EventStoreDB.csproj` (net10.0, nullable, `EventStore.Client.Grpc.Streams`, `Encina.Messaging`, PublicAPI analyzers, XML docs) and `Directory.Packages.props` (`Testcontainers.EventStoreDb` for tests); add the project to `Encina.slnx`.
2. `src/Encina.EventStoreDB/ServiceCollectionExtensions.cs` and `EncinaEventStoreDBOptions.cs` (connection string, never printed: `[JsonIgnore]` and a `ToString()` override, `AGENTS.md` section 3): `AddEncinaEventStoreDB` registers the options and one `EventStoreClient` with `TryAddSingleton` (only if #576 has not).
3. `src/Encina.EventStoreDB/DeadLetter/DeadLetterStreamNames.cs`: `dlq.{SourcePattern}-{hash}`; hash is SHA-256 of the UTF-8 source id, hex, truncated to 32 characters; the first event stores the full key for collision checks.
4. `src/Encina.EventStoreDB/DeadLetter/DeadLetterEvents.cs`: `DeadLetterCaptured` (the full message: `Id`, `RequestType`, `RequestContent`, `ErrorCode`, `ExceptionType`, `ExceptionStackTrace`, `CorrelationId`, `SourcePattern`, `SourceMessageId`, `TenantId`, `TotalRetryAttempts`, the instants), `DeadLetterReplayClaimed`, `DeadLetterReplayed(ReplayedAtUtc, ReplayResult)`, `DeadLetterDeleted`; serialized with System.Text.Json source generation.
5. `src/Encina.EventStoreDB/DeadLetter/DeadLetterState.cs` (public sealed class implementing `IDeadLetterMessage`, with `Apply(event)` folding) and `DeadLetterMessageFactory.cs`.
6. `PublicAPI.Unshipped.txt` and `PublicAPI.Shipped.txt` for the new project.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
- Encina is a .NET 10 / C# 14 library (Railway Oriented Programming, Either<EncinaError, T>).
- Issue #585: DeadLetterStoreEventStoreDB implements IDeadLetterStore (ADR-046) in the new package src/Encina.EventStoreDB, shared with issue #576.

TASK:
Create the package skeleton, the options, the stream-name helper, the events, the folded state and the factory as listed in the Phase 1 Tasks. Skip the skeleton if the package already exists on main.

KEY RULES:
- An options property with a connection string or key is [JsonIgnore] and ToString() never prints it.
- Events are immutable records; the state is rebuilt only by folding events; dates are UTC instants.
- No [Obsolete], no compatibility layer; XML docs on every public type; PublicAPI files complete.

REFERENCE FILES:
- src/Encina.Messaging/DeadLetter/IDeadLetterMessage.cs
- src/Encina.MongoDB/DeadLetter/DeadLetterMessage.cs and DeadLetterMessageFactory.cs
- src/Encina.Marten/Encina.Marten.csproj (package layout)
```

</details>

---

### Phase 2: `DeadLetterStoreEventStoreDB`

> **Goal**: the 11 contract methods.

<details>
<summary>Tasks</summary>

1. `src/Encina.EventStoreDB/DeadLetter/DeadLetterStoreEventStoreDB.cs`: `sealed class DeadLetterStoreEventStoreDB : IDeadLetterStore`; constructor `(EventStoreClient client, TimeProvider timeProvider, ILogger<DeadLetterStoreEventStoreDB> logger, IOptions<DeadLetterEventStoreDBOptions>? options = null)`.
2. `AddAsync`: `AppendToStreamAsync(streamName, StreamState.NoStream, [captured])`; `WrongExpectedVersionException` maps to `Right(false)`; other failures to `Left` (`dlq.store_failed`, exception type only).
3. A private `ReadAllStatesAsync(DeadLetterFilter?, CancellationToken)` scanning `$all` with `StreamFilter.Prefix("dlq.")` (narrowed to `dlq.{SourcePattern}-` when the filter names a pattern), folding events per stream, skipping streams whose last event is `DeadLetterDeleted`; every filter field applied in one `Matches(state, filter)` method split into small predicates.
4. `GetAsync` (cache, then direct stream read, then the scan on a miss), `GetMessagesAsync` (order `DeadLetteredAtUtc`, `Id`; `newestFirst`; `skip`/`take`), `GetCountAsync`.
5. `TryClaimForReplayAsync`: read the stream, test the claim conditions with the live-claim window, append `DeadLetterReplayClaimed` with the expected revision read; `WrongExpectedVersionException` is a lost claim, `Right(false)`. `MarkAsReplayedAsync` is the same pattern, only while `ReplayedAtUtc` is null.
6. `DeleteAsync` (tombstone, then `SoftDeleteAsync`, expected revision), `DeleteManyAsync` and `DeleteExpiredAsync` (scan, then delete each match, returning the count of those that succeeded; a failure of one is a `Left` and stops, per ADR-046 decision 7), `SaveChangesAsync` returns `Right(Unit)`.
7. The scan must honor the `CancellationToken` and stop reading after the page when ordering allows (the append order of `$all` is the capture order, which is `DeadLetteredAtUtc` order for in-process captures; the final order is still sorted explicitly).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
- Phase 1 produced the events, DeadLetterState and DeadLetterStreamNames. The contract is src/Encina.Messaging/DeadLetter/IDeadLetterStore.cs.

TASK:
Implement DeadLetterStoreEventStoreDB as described in the Phase 2 Tasks.

KEY RULES:
- Either<EncinaError, T> everywhere; a client failure is a Left, never success; log the error code or exception type, never the message.
- Time comes from TimeProvider; no DateTime.UtcNow.
- Optimistic concurrency (expected revision) is the claim; no locks.
- Keep each method at CRAP <= 10; split the filter into small predicates.

REFERENCE FILES:
- src/Encina.MongoDB/DeadLetter/DeadLetterStoreMongoDB.cs
- src/Encina.Messaging/DeadLetter/DeadLetterFilter.cs
- tests/Encina.ContractTests/Messaging/DeadLetter/DeadLetterStoreContract.cs
```

</details>

---

### Phase 3: Configuration, DI and registration

> **Goal**: `AddEncinaDeadLetterEventStoreDB()` proven by a DI test.

<details>
<summary>Tasks</summary>

1. `DeadLetterEventStoreDBOptions` (`IdCacheSize` default 10,000; `ScanPageSize` default 500).
2. `AddEncinaDeadLetterEventStoreDB(this IServiceCollection, Action<DeadLetterEventStoreDBOptions>? = null, Action<DeadLetterOptions>? = null)`: registers the options, `TryAddSingleton(TimeProvider.System)` and delegates to `AddDeadLetterQueueServices<DeadLetterStoreEventStoreDB, DeadLetterMessageFactory>(true, options)` so the store wins in any order and never overrides the application's own registration.
3. Health: `EventStoreDBHealthCheck : IEncinaHealthCheck` (const `DefaultName`, static `Tags`, scoped resolution) reading the server's gossip or a `ReadStreamAsync` of `$all` with `maxCount: 1`; registered when `ProviderHealthCheck.Enabled`.
4. DI test with `ValidateOnBuild` and `ValidateScopes`, both registration orders.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
- Registration of the queue is shared (src/Encina.Messaging/DeadLetter/DeadLetterServiceCollectionExtensions.cs, AddDeadLetterQueueServices, first registration wins).

TASK:
Create the options, the registration extension, the health check and the DI test.

KEY RULES:
- Registration completeness: every option and dependency the store resolves is registered; proven with ValidateOnBuild and ValidateScopes.
- Health check: DefaultName const, Tags static array, scoped resolution via IServiceProvider.CreateScope().
- Pay-for-what-you-use: nothing is registered unless the method is called.

REFERENCE FILES:
- src/Encina.Kafka/Health/KafkaHealthCheck.cs
- src/Encina.MongoDB/ServiceCollectionExtensions.cs
- src/Encina.Audit.Marten/ServiceCollectionExtensions.cs
```

</details>

---

### Phase 4: Cross-cutting integration and observability

> **Goal**: logging and the EventIds; tracing through the existing decorator.

<details>
<summary>Tasks</summary>

1. Register `EventStoreDBDeadLetter = (6925, 6934)` in `src/Encina/Diagnostics/EventIdRanges.cs` first (from this family's block 6900-6949), add the assembly to `AssemblyRanges` in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`, and add the field to `src/Encina/PublicAPI.Unshipped.txt`. The package-level ids of the audit store (#576) are that plan's, not this range.
2. `Diagnostics/DeadLetterEventStoreDBLog.cs`: `[LoggerMessage]` ids 6925-6934 packed (store failure by operation, claim lost, duplicate capture, scan truncated by the cancellation token, delete count).
3. Tracing and metrics: `InstrumentedDeadLetterStore` decorates the store; add a test that it does.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
- ADR-021: every EventId range is registered in src/Encina/Diagnostics/EventIdRanges.cs before use.

TASK:
Register the range, add the log class and the architecture-test entry as listed in the Phase 4 Tasks.

KEY RULES:
- Ids are packed sequentially inside 6925-6934; never outside the range.
- Never log EncinaError.Message, exception messages or the tenant id; log error codes and exception types.
- Run the architecture tests (EncinaEventIdAllocationTests).

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- src/Encina.Kafka/Log.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 5: Testing

> **Goal**: every applicable flag reaches its target in `.github/coverage-manifest/Encina.EventStoreDB.json` (new, with one-sentence justifications, unless #576 created it).

<details>
<summary>Tasks</summary>

1. **Unit**: stream-name helper (stable, collision guard), event folding, filter predicates, error mapping, claim outcomes, with the client mocked (NSubstitute) and a `FakeTimeProvider`.
2. **Guard**: every public method and the registration extension.
3. **Contract/integration**: `DeadLetterStoreContract` derived against a real EventStoreDB container (a new `EventStoreDbFixture` in `Encina.TestInfrastructure` with a shared `[Collection]`); plus: concurrent claim from two stores, repeated capture after a soft delete (the open verification of Design Choice 3), a scan with 1,000 streams.
4. **Property**: stream names are unique per `(pattern, source id)` and stable; folding is order-preserving.
5. **Load / Benchmark**: `.md` justifications (queue scan is an exception-path operation), per `AGENTS.md` section 9.
6. PR reports per-flag coverage of each new file and the CRAP table.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
- Event-sourced stores have no InMemory variant: unit tests mock the client; integration tests use a real EventStoreDB container (AGENTS.md section 3).

TASK:
Write the tests of Phase 5 and the manifest entries.

KEY RULES:
- Shouldly through Encina.Testing.Shouldly; no FluentAssertions.
- Shared [Collection] fixture; never IClassFixture or a per-class container; ClearAllDataAsync in InitializeAsync.
- No Thread.Sleep; deterministic time.

REFERENCE FILES:
- tests/Encina.TestInfrastructure/Fixtures/KafkaFixture.cs
- tests/Encina.ContractTests/Messaging/DeadLetter/DeadLetterStoreContract.cs
- tests/Encina.IntegrationTests/Infrastructure/MongoDB/Stores/DeadLetterStoreMongoDBIntegrationTests.cs
```

</details>

---

### Phase 6: Documentation and finalization

<details>
<summary>Tasks</summary>

1. XML docs on every public API; package README (`src/Encina.EventStoreDB/README.md`); a section in `docs/features/dead-letter-queue.md`; `docs/INVENTORY.md`.
2. `changelog.d/585-eventstoredb-dead-letter-store.added.md`.
3. ADR: the stream and delete semantics (Design Choices 1 to 3) are a lasting architectural decision; ADR number assigned when the ADR pull request opens.
4. Build 0 warnings; tests per flag; coverage and CRAP reported.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
- The documentation of this feature: package README, docs/features/dead-letter-queue.md, the changelog fragment, the ADR.

TASK:
Complete the documentation tasks of Phase 6.

KEY RULES:
- Never edit CHANGELOG.md; use changelog.d/.
- No hand-typed coverage figures (covref markers); English only; the reference application is never named.

REFERENCE FILES:
- docs/features/dead-letter-queue.md
- src/Encina.Audit.Marten/README.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Topic | Requirement |
|--------|-------|-------------|
| SPEC-000 DEC-008, REQ-030 | Dead letter queue in 1.0 | #584-#589 are additional backends (milestone v0.19.0) |
| ADR-046 | Persistent dead letter queue | Oldest first, `Right(false)` on a repeated source key, atomic claim, set-based deletes, `TimeProvider`, `TenantId` |
| ADR-019, ADR-027 | Event sourcing | Event-sourced stores have no InMemory variant |
| AGENTS.md section 3 | Options secrets | A connection string is `[JsonIgnore]` and never printed |
| EventStoreDB docs (vendor) | Expected-state append, soft delete, `$all` filters | Verified by the Phase 5 integration tests before the ADR is written |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in a new IDeadLetterStore backend |
|-----------|----------|------------------------------------------|
| `IDeadLetterStore` | `src/Encina.Messaging/DeadLetter/IDeadLetterStore.cs` | The contract this backend implements |
| `IDeadLetterMessage`, `DeadLetterData`, `IDeadLetterMessageFactory` | `src/Encina.Messaging/DeadLetter/` | The record and its factory |
| `DeadLetterFilter`, `DeadLetterErrorCodes`, `DeadLetterStoreLimits`, `DeadLetterSourcePatterns` | `src/Encina.Messaging/DeadLetter/` | Filter translation, error codes, limits, the six source patterns |
| `AddDeadLetterQueueServices` | `src/Encina.Messaging/DeadLetter/DeadLetterServiceCollectionExtensions.cs` | First-wins registration shared by all providers |
| `InstrumentedDeadLetterStore` | `src/Encina.OpenTelemetry/MessagingStores/InstrumentedDeadLetterStore.cs` | Tracing and metrics around any store |
| `DeadLetterStoreContract` | `tests/Encina.ContractTests/Messaging/DeadLetter/DeadLetterStoreContract.cs` | Shared behavior suite |
| `DeadLetterStoreMongoDB` | `src/Encina.MongoDB/DeadLetter/DeadLetterStoreMongoDB.cs` | Closest implementation reference |
| `KafkaHealthCheck`, `KafkaFixture` | `src/Encina.Kafka/Health/`, `tests/Encina.TestInfrastructure/Fixtures/` | Patterns for the health check and the container fixture |
| `EventStore.Client.Grpc.Streams` 23.3.9 | `Directory.Packages.props` | The client, already pinned |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.EventStoreDB` (dead letter) | 6925-6934 (`EventStoreDBDeadLetter`) | From this family's block 6900-6949; register before use |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Package skeleton (Phase 1, skipped if #576 landed first) | ~6 | csproj, options, registration, PublicAPI x2, README |
| Dead letter production code (Phases 1-4) | ~9 | Stream names, events, state, factory, store, options, registration, health check, log |
| Tests (Phase 5) | ~9 | Unit, guard, contract/integration, property, fixture |
| Manifest, feature page, changelog, ADR | ~4 | Documentation phase |
| **Total** | **~28** | Exceeds 15 files. Proposed split: PR 1 = package skeleton, events, state, store (Phases 1-2); PR 2 = registration, health, logging (Phases 3-4); PR 3 = tests and documentation (Phases 5-6) |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution first.
- Railway Oriented Programming: Either<EncinaError, T>; errors never swallowed; TimeProvider for time; async with CancellationToken.
- Issue #585: DeadLetterStoreEventStoreDB implements IDeadLetterStore (ADR-046) in the new package Encina.EventStoreDB, shared with #576.

IMPLEMENTATION OVERVIEW:
Phase 1: package skeleton (if absent), options, stream names, events, folded state, factory.
Phase 2: the 11 contract methods; uniqueness by StreamState.NoStream on a source-key stream; claim by expected revision; delete by tombstone plus soft delete.
Phase 3: AddEncinaDeadLetterEventStoreDB over AddDeadLetterQueueServices; health check; DI test.
Phase 4: EventIdRanges.EventStoreDBDeadLetter 6925-6934, Log class, architecture test entry.
Phase 5: unit, guard, contract/integration (container), property tests; manifest.
Phase 6: docs, changelog fragment, ADR (number assigned when its pull request opens).

KEY PATTERNS:
- Store DeadLetterStoreEventStoreDB; DI AddEncinaDeadLetterEventStoreDB() first-wins.
- Dilemmas D1-D3 are recommended, pending the maintainer: do not start Phase 2 before they are answered.
- Unverified server behavior (NoStream after a soft delete) is checked by an integration test before the design is frozen.

REFERENCE FILES:
- src/Encina.Messaging/DeadLetter/ (contract, registration)
- src/Encina.MongoDB/DeadLetter/ (closest implementation)
- tests/Encina.ContractTests/Messaging/DeadLetter/DeadLetterStoreContract.cs
- docs/architecture/adr/046-persistent-dead-letter-queue.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | The queue is an operator tool; only the immutable Id-to-stream map is cached inside the store. |
| 2 | OpenTelemetry | ✅ | `InstrumentedDeadLetterStore` decorates the store; Phase 4 tests it. |
| 3 | Structured logging | ✅ | Phase 4: range 6925-6934 registered first, error codes and exception types only. |
| 4 | Health checks | ✅ | Phase 3: `EventStoreDBHealthCheck` (`IEncinaHealthCheck`). |
| 5 | Validation | ✅ | Contract argument rules throw before I/O; input rules live once in the orchestrator and manager (ADR-046 decision 6). |
| 6 | Resilience | ✅ | Connection failures map to `Left`; the client's own retry settings apply; no extra policy in the store. |
| 7 | Distributed locks | ❌ | The claim is optimistic concurrency on the stream, not a lock. |
| 8 | Transactions | ❌ | EventStoreDB offers optimistic concurrency per stream; each operation touches one stream. |
| 9 | Idempotency | ✅ | `StreamState.NoStream` on the source-key stream; a repeated capture returns `Right(false)`. |
| 10 | Multi-tenancy | ✅ | `TenantId` stored in the event and filtered only when the filter names it (ADR-046 Choice 7). |
| 11 | Module isolation | ❌ | `IDeadLetterMessage` has no `ModuleId` today. |
| 12 | Audit trail | ⏭️ | The stream is the store's own history; operator replay and delete audit is #2013 (issue exists). |

---

## Prerequisites & Dependencies

- #583 (closed) fixed the contract. #576 (EventStoreDB audit store, open) shares the package: coordinate which pull request creates the skeleton; the second rebases. #2013 (open) later adds `ActorId` and `CausationId` to the record.
- `Testcontainers.EventStoreDb` must be added to `Directory.Packages.props`; `docker-compose.yml` gets an optional EventStoreDB service in the `core` or a new profile only if the maintainer wants local runs beyond Testcontainers.
- No decision of another issue blocks this plan; D1-D3 block Phase 2.

---

## Next Steps

1. The maintainer answers D1, D2 and D3 when work on #585 starts; the answers go into `## Maintainer Decisions`.
2. Re-check the Design Choices against main and the state of #576; `adversarial-reviewer` verifies the Settled ones.
3. Brief a worker per proposed pull request (3 pull requests, see the file count).
4. Final commit references `Fixes #585`.
