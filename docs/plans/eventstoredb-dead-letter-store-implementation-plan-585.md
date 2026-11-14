# Implementation Plan: `DeadLetterStoreEventStoreDB` — EventStoreDB backend for the persistent dead letter queue

> **Issue**: [#585](https://github.com/dlrivada/Encina/issues/585)
> **Type**: Feature
> **Complexity**: Medium-High (6 phases, 1 provider, a new package shared with #576, ~18 source files + tests)
> **Estimated Scope**: ~1,100-1,500 lines of production code + ~1,800-2,400 lines of tests

---

## Summary

Implement `Encina.Messaging.DeadLetter.IDeadLetterStore` on EventStoreDB as `DeadLetterStoreEventStoreDB`, for applications that keep their events in EventStoreDB (named in `AGENTS.md` section 5 as the future event-sourcing provider next to Marten). Each dead letter lives in its own append-only stream; the lifecycle (captured, replay claimed, replayed, deleted) is the stream itself. The store implements all 10 methods of the ADR-046 contract. EventStoreDB has no secondary index, so the plan has to say how filtered reads, exact counts, the atomic claim, the unique `(SourcePattern, SourceMessageId)` key and deletes are satisfied (Design Choices 1 to 3).

What the code looks like today (main `18ec8dd19`):

- There is **no** `Encina.EventStoreDB` package and no EventStoreDB client use in `src/`. `Directory.Packages.props` pins `EventStore.Client.Grpc.Streams` 23.3.9 and no `Testcontainers.EventStoreDb`. `docker-compose.yml` has no EventStoreDB service. The package is shared with #576 (EventStoreDB audit store); who owns the skeleton is Dilemma D4.
- The contract is `IDeadLetterStore` (`src/Encina.Messaging/DeadLetter/IDeadLetterStore.cs`), 10 methods returning `Either<EncinaError, T>`. There is no `DeadLetterMessage` type (it is `IDeadLetterMessage`), no `message.Source` (it is `SourcePattern`, values in `DeadLetterSourcePatterns`: Recoverability, Outbox, Inbox, Scheduling, Saga, Choreography) and no `GetBySourceAsync`. The issue's sample (`StreamState.NoStream` keyed by `{Source}-{Id}`, three methods) is replaced.
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
| **B) Client-side folding over a `$all` prefix scan** (read `$all` with a server-side `StreamFilter.Prefix("dlq.")`, fold each stream into its current state, filter, order, page) | One store; exact and consistent; no projection to run or rebuild; no system projection needed | **The cost is O(all events of the database), not O(dead letters)**: the server applies the prefix filter while it still walks the whole transaction log, so one filtered page, one count, each timer run of `DeleteExpiredAsync` and each manager screen read the application's entire event history. In an event-sourced application (the audience of this store) that is the largest dataset the store holds |
| **C) A documented reduced capability** (writes plus reads by id and category; the rest returns a typed `Left`) | Smallest to build | Breaks provider coherence (`AGENTS.md` section 5: providers differ only in implementation) and every manager feature that filters, counts or claims |
| **D) Category streams** (`$ce-dlq.{SourcePattern}` from the `$by_category` system projection; stream names are `dlq.{SourcePattern}-{hash}`, so the category is `dlq.{SourcePattern}`) | A filtered read or count walks only the dead letters of one pattern (cost proportional to the queue, not to the database); exact; no read model of our own | Requires the `$by_category` system projection to be running on the server (an operational dependency, named in the README and checked at startup and by the health check: not running is fail closed, a `Left`); the category streams are eventually consistent with the source streams for a few milliseconds, so a read right after a capture can miss it (the contract suite verifies the exact behavior, Phase 0) |

Whatever option is chosen, the store takes a scan cap: `DeadLetterEventStoreDBOptions.MaxScanEvents` (default 100,000 events read by one call). A call that would read more returns a `Left` (`dlq.scan_limit_exceeded`, fail closed: never a truncated page or a wrong count reported as success) and `EventStoreDBHealthCheck` reports `Unhealthy` when the last scan hit the cap (the way #587 does for its topic scans).

### Chosen Option: **D — category streams with the scan cap, with B kept as the fallback when `$by_category` cannot be enabled** (recommended, pending the maintainer)

### Rationale

- A dead letter queue is an exception path, but the store lives next to the application's whole event history, so the cost of a read must follow the queue, not the log. D is the only option that does while keeping one store and the same contract suite as every other backend.
- B is correct and has no operational dependency; it wins on a dedicated EventStoreDB that holds only dead letters (the log is the queue), or when the maintainer will not accept the `$by_category` requirement. A wins when the queue is very large and operators filter often: it can be added later behind the same store because the contract does not change. C wins only as a stop-gap and is rejected for the coherence rule.

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
| **D) Hard delete: append `DeadLetterDeleted`, then tombstone the stream** (`TombstoneAsync`) | The stream is gone for good from every read and subscription; no readable state remains in the log's live view | A tombstoned stream id can **never be written again**, and the stream id here is derived from the source key, so the same `(SourcePattern, SourceMessageId)` could never be captured again. The name must carry a generation (`dlq.{SourcePattern}-{hash}-g{n}`): a capture reads the highest generation of the key, and a tombstoned latest generation opens generation n+1 (still one `StreamState.NoStream` append, so concurrent captures stay serialized). More code and one more read per capture |

**What none of the options does by itself**: the events of a soft-deleted or tombstoned stream (including `RequestContent`, which may hold personal data) stay on disk in their chunk files until the server scavenges them (data in EventStoreDB is erased only by scavenge). So with A, C and D alike, the default behavior is: after `DeleteAsync` or `DeleteExpiredAsync` the dead letter disappears from every read, and its bytes remain physically until the next scavenge. Operators must schedule scavenge (at least as often as the retention promise they make; stated in the package README and checked by the health check only as far as the server exposes the last scavenge time) and the plan states this as the default instead of leaving it to message encryption; an application may additionally encrypt dead-lettered content (`Encina.Messaging.Encryption`), which is its choice, not a substitute for scavenge.

### Chosen Option: **A — tombstone event then soft delete, with the scavenge duty documented** (recommended, pending the maintainer)

### Rationale

- The contract returns `Right(true)` when a message was deleted and a count for set-based deletes, so every delete path must be an explicit act; B alone cannot meet that.
- A keeps the source key re-capturable without a generation scheme (verified by the Phase 0 spike before any PR) and erases no earlier than D does, since both need scavenge. D wins when the maintainer needs the stream id permanently closed (an audit rule that a deleted id never reappears); then the generation suffix is part of Design Choice 3.
- C wins when a very large expired backlog makes the scan of `DeleteExpiredAsync` expensive; it can be added without changing the contract.

</details>

<details>
<summary><strong>3. Stream identity and the unique source key (Dilemma D3)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Stream named from the source key** (`dlq.{SourcePattern}-{hash(SourceMessageId)}`), appended with `StreamState.NoStream`; a bounded Id-to-stream cache held by a **singleton** service (not by the scoped store) for `GetAsync(Guid)` | Uniqueness is one atomic append: a second capture fails the expected-state check and returns `Right(false)` once the collision guard has passed; the pattern is the category | `GetAsync(Guid)` on a cold cache is a category read (D1 option D) or a scan (option B); a hash collision needs a guard, which runs before the `Right(false)` (see Phase 2 task 2) |
| **B) Stream named from the message `Id`, plus a gate stream per source key appended first** | Direct lookup by id | Two appends are not atomic: a crash between them leaves a gate with no dead letter and every later capture of that source message wrongly returns `Right(false)` |
| **C) The store derives the message `Id` deterministically from the source key** (a name-based GUID) | Both lookups are direct | The store would override the `Id` the orchestrator assigned; callers that hold the original `Id` after `AddAsync` would diverge |

### Chosen Option: **A — stream from the source key with a bounded Id cache** (recommended, pending the maintainer)

### Rationale

- A is the only option whose uniqueness is atomic without overriding the caller's `Id`. A cache entry never goes stale (an `Id` never changes its stream); a miss falls back to the scan of Design Choice 1.
- B wins if `GetAsync` by id is on a hot path with a cold cache every time (it is not: the manager reads, then claims, then marks, all by id in one request). C wins if the maintainer accepts the store assigning identity.
- The store is registered `TryAddScoped<IDeadLetterStore, TStore>` (`src/Encina.Messaging/MessagingServiceCollectionExtensions.cs:495`), so a cache held inside the store would live one request and never hit. The cache is a singleton service (`DeadLetterStreamIndex`, `IdCacheSize` bound) registered by `AddEncinaDeadLetterEventStoreDB` and injected into the store.
- Whether `StreamState.NoStream` succeeds again after a soft delete (the source key may be captured again later) is a **Phase 0 spike** run before any pull request (see Phase 0); if it does not, a deleted source key could never be captured again and the delete design returns to the maintainer (Design Choice 2 option D with a generation suffix) before the store is written. Re-capture semantics: a new `DeadLetterCaptured` on a stream whose previous life ended in `DeadLetterDeleted` resets the folded state, so the fold never relies on "skip streams whose last event is `DeadLetterDeleted`" alone.

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
<summary><strong>4b. Package placement and ownership of the shared skeleton (Dilemma D4)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One shared package `Encina.EventStoreDB`; the skeleton (csproj, `Directory.Packages.props`, `Encina.slnx` row, options, client registration, PublicAPI files, manifest, README) is its own first pull request owned by #585; #576 depends on it and rebases onto it (a hard prerequisite, stated in both issues)** | Matches `Encina.Marten`; one package and one client registration for both EventStoreDB issues; no two pull requests create the same csproj, props entries, slnx rows and manifest | #576 waits for the skeleton of #585 |
| **B) The same shared package, with #576 owning the skeleton and #585 rebasing** | #576 is the lower number and the older audit feature | #585 waits; the skeleton is then shaped by the audit store |
| **C) A package per feature (`Encina.EventStoreDB.DeadLetter`)** | Independent versions, no ordering between the issues | Two packages and two client registrations for one server |

### Chosen Option: **A — one `Encina.EventStoreDB` package, skeleton owned by #585 as its own first PR** (recommended, pending the maintainer)

### Rationale

- "The first to merge creates the package" is a coordination defect: both pull requests would create the same project, props entries and solution rows and conflict, and neither may depend on an unmerged package of the other. Naming one owner and making the skeleton a standalone first PR removes the race. The only rule that remains fixed here is that there is no InMemory event-sourced store (`AGENTS.md` section 3) and the name is `DeadLetterStoreEventStoreDB` (`AGENTS.md` section 4).
- B wins if #576 is implemented first by order of work; C wins if the maintainer wants every EventStoreDB capability to be an independent opt-in package.
- The client (`EventStoreClient`) is registered once (`TryAddSingleton`) by `AddEncinaEventStoreDB`; this feature adds only its own registration.
- The test container is `Testcontainers.EventStoreDb` (to be added to `Directory.Packages.props`; the exact image tag is the vendor LTS, per the default-images rule of the project, and is confirmed in Phase 5).

</details>

---

## Implementation Phases

### Phase 0: Server behavior spike (before any pull request)

> **Goal**: the EventStoreDB behaviors the design depends on are verified once, against a real container, and recorded in the issue (no production code, no pull request; it runs before Phase 1 and gates Phase 2).

<details>
<summary>Tasks</summary>

1. Against the vendor LTS image started locally (no committed test), verify and record: (a) `StreamState.NoStream` succeeds on a stream that was soft-deleted (re-capture of a deleted source key) and fails on a live stream; (b) the observable behavior of a tombstoned stream (Design Choice 2 option D) on append; (c) that `$by_category` is available and how it is enabled (Design Choice 1 option D) and its latency after an append; (d) that `StreamFilter.Prefix` on `$all` returns only `dlq.` streams (option B fallback) and what it costs on a database with 1,000,000 unrelated events; (e) that soft-deleted and tombstoned events remain readable on disk until scavenge (the retention duty of Design Choice 2).
2. The results go into the issue as a comment and into `## Maintainer Decisions` of this plan; a result that contradicts a Design Choice returns that choice to the maintainer before Phase 2 starts.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 0</strong></summary>

```
CONTEXT:
- Issue #585 depends on EventStoreDB server behaviors that are not verified yet (see Design Choices 1 to 3).

TASK:
Start the vendor LTS EventStoreDB image locally and verify the five points of the Phase 0 Tasks with a throwaway console script (not committed). Record the observed results and the commands used.

KEY RULES:
- No production code and no pull request in this phase; the output is a comment on the issue.
- A result that contradicts a Design Choice is reported, not worked around.

REFERENCE FILES:
- docs/plans/eventstoredb-dead-letter-store-implementation-plan-585.md (Design Choices 1 to 3)
```

</details>

---

### Phase 1: Package, client registration and event model

> **Goal**: the package skeleton (its own first pull request, owned by this issue per Design Choice 4b) and the persisted shapes (the second pull request).

<details>
<summary>Tasks</summary>

1. `src/Encina.EventStoreDB/Encina.EventStoreDB.csproj` (net10.0, nullable, `EventStore.Client.Grpc.Streams`, `Encina.Messaging`, PublicAPI analyzers, XML docs) and `Directory.Packages.props` (`Testcontainers.EventStoreDb` for tests); add the project to `Encina.slnx`.
2. `src/Encina.EventStoreDB/ServiceCollectionExtensions.cs` and `EncinaEventStoreDBOptions.cs` (connection string, never printed: `[JsonIgnore]` and a `ToString()` override, `AGENTS.md` section 3): `AddEncinaEventStoreDB` registers the options and one `EventStoreClient` with `TryAddSingleton`.
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
Create the package skeleton, the options, the stream-name helper, the events, the folded state and the factory as listed in the Phase 1 Tasks. The skeleton is its own pull request; if the package already exists on main, do not recreate it.

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

> **Goal**: the 10 contract methods (`AddAsync`, `GetAsync`, `GetMessagesAsync`, `GetCountAsync`, `TryClaimForReplayAsync`, `MarkAsReplayedAsync`, `DeleteAsync`, `DeleteManyAsync`, `DeleteExpiredAsync`, `SaveChangesAsync`).

<details>
<summary>Tasks</summary>

1. `src/Encina.EventStoreDB/DeadLetter/DeadLetterStoreEventStoreDB.cs`: `sealed class DeadLetterStoreEventStoreDB : IDeadLetterStore`; constructor `(EventStoreClient client, TimeProvider timeProvider, ILogger<DeadLetterStoreEventStoreDB> logger, IOptions<DeadLetterEventStoreDBOptions>? options = null)`.
2. `AddAsync`: `AppendToStreamAsync(streamName, StreamState.NoStream, [captured])`. On `WrongExpectedVersionException` the **collision guard runs first**: read the first event of the stream and compare its full `(SourcePattern, SourceMessageId)` with the one being captured; equal means a repeated capture, `Right(false)`; different means a hash collision, a `Left` (`dlq.stream_collision`), never `Right(false)`, so a dead letter is never silently dropped. Other failures map to `Left` (`dlq.store_failed`, exception type only); connection failures to `Left` (`dlq.store_unavailable`).
   - **ErrorCategory (ADR-062)**: `Encina.EventStoreDB` is a new package outside `ErrorCategoryPending.txt`, so `ErrorCategoryCompletenessStaticScanTests` fails on the first uncategorized `EncinaErrors.Create` in `src/Encina.EventStoreDB`; every code is created with its category: `dlq.store_failed` `Internal` (R4: store failures), `dlq.stream_collision` `Internal` (an invariant of the stream naming, never the caller's fault), `dlq.scan_limit_exceeded` `Internal` (the scan cap of Design Choice 1 is a server-side configuration limit, R4; the caller cannot fix it by retrying) and `dlq.category_projection_not_running` `Internal` (a missing server-side wiring, R1), `dlq.store_unavailable` `Unavailable` (connection failures, retryable). A lost claim and a repeated capture are `Right(false)`, not errors.
3. A private `ReadStatesAsync(DeadLetterFilter?, CancellationToken)`: under Design Choice 1 option D it reads `$ce-dlq.{SourcePattern}` (every pattern when the filter names none, bounded by `MaxScanEvents`); under the fallback option B it scans `$all` with `StreamFilter.Prefix("dlq.")` (narrowed to `dlq.{SourcePattern}-` when the filter names a pattern). It folds events per stream; a `DeadLetterCaptured` after a `DeadLetterDeleted` resets the state (re-capture), and a stream whose last event is `DeadLetterDeleted` is skipped; every filter field is applied in one `Matches(state, filter)` method split into small predicates. Exceeding `MaxScanEvents` is a `Left` (`dlq.scan_limit_exceeded`).
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

1. `DeadLetterEventStoreDBOptions` (`IdCacheSize` default 10,000; `ScanPageSize` default 500; `MaxScanEvents` default 100,000) and the singleton `DeadLetterStreamIndex` (the bounded Id-to-stream map, `TryAddSingleton`), injected into the scoped store, so the cache survives the request (the store is registered `TryAddScoped`).
2. `AddEncinaDeadLetterEventStoreDB(this IServiceCollection, Action<DeadLetterEventStoreDBOptions>? = null, Action<DeadLetterOptions>? = null)`: registers the options, `TryAddSingleton(TimeProvider.System)` and delegates to `AddDeadLetterQueueServices<DeadLetterStoreEventStoreDB, DeadLetterMessageFactory>(true, options)` so the store wins in any order and never overrides the application's own registration.
3. Health: `EventStoreDBHealthCheck : IEncinaHealthCheck` (const `DefaultName`, static `Tags`; the `EventStoreClient` is a singleton, so the check takes it by constructor injection and needs no scope, unlike the Kafka precedent that resolves lazily) reading the server's gossip or a `ReadStreamAsync` of `$all` with `maxCount: 1`; registered when `ProviderHealthCheck.Enabled`.
4. DI test with `ValidateOnBuild` and `ValidateScopes`, both registration orders.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
- Registration of the queue is shared (AddDeadLetterQueueServices in src/Encina.Messaging/MessagingServiceCollectionExtensions.cs:495, first registration wins; the store is registered TryAddScoped).

TASK:
Create the options, the registration extension, the health check and the DI test.

KEY RULES:
- Registration completeness: every option and dependency the store resolves is registered; proven with ValidateOnBuild and ValidateScopes.
- Health check: DefaultName const, Tags static array; the singleton EventStoreClient is injected directly (no scope); Unhealthy when the last scan hit MaxScanEvents or when $by_category is not running.
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
2. `Diagnostics/DeadLetterEventStoreDBLog.cs`: `[LoggerMessage]` ids 6925-6934 packed (store failure by operation, claim lost, duplicate capture, stream collision, scan cap exceeded, category projection not running, delete count; the log class lands with the store, PR 3 of the split, because the store uses it).
3. Tracing and metrics: `InstrumentedDeadLetterStore` decorates the store; add a test that it does.
4. Tenant (ADR-054 D4): the tenant is a span attribute and a metric tag when multi-tenancy is on. `InstrumentedDeadLetterStore` on main never carries it, so this plan **defers** the tenant tag on the decorator (a decorator-wide change for every store; issue to open at implementation time) and the store itself never writes the tenant id into logs. The tenant stays a stored event field and a filter value; stream names are derived from `(SourcePattern, SourceMessageId)` only, so two tenants capturing the same source key share a stream (the ADR-046 unique key is `(SourcePattern, SourceMessageId)` with no tenant part, on every provider), and no tenant-qualified id exists to collide.

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
- Never log EncinaError.Message or exception messages; log error codes and exception types (the tenant tag on telemetry is deferred, task 4).
- Run the architecture tests (EncinaEventIdAllocationTests).

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- src/Encina.Kafka/Log.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 5: Testing

> **Goal**: every applicable flag reaches its target in `.github/coverage-manifest/Encina.EventStoreDB.json` (new, created by the skeleton pull request of Design Choice 4b; each pull request adds the rows of the files it contains, targets never go down).

Per-file targets (percent of coverable lines per flag; contract is 0 for every file because the derived `DeadLetterStoreContract` class needs a real server and runs in `Encina.IntegrationTests`, where it counts for the integration flag):

| File | Unit | Guard | Integration | Property | Justification |
|------|-----:|------:|------------:|---------:|---------------|
| `DeadLetterStoreEventStoreDB.cs` | 80 | 90 | 85 | 0 | Mocked-client unit tests reach the branches and the container suite reaches the real append, read and delete calls; stream-name properties live in a different file. |
| `DeadLetterStreamNames.cs` | 95 | 90 | 60 | 90 | Pure functions with a null/blank guard; the property suite proves uniqueness and stability. |
| `DeadLetterEvents.cs` | 85 | 0 | 80 | 0 | Records with serialization attributes and no validated parameters; round-tripped by the integration suite. |
| `DeadLetterState.cs` | 95 | 0 | 80 | 80 | The fold is pure and branch-heavy, so unit and property tests carry it; no parameters to guard beyond the event type. |
| `DeadLetterMessageFactory.cs` | 95 | 90 | 70 | 0 | One `Create` method with a null check. |
| `DeadLetterEventStoreDBOptions.cs` | 90 | 0 | 0 | 0 | Properties with defaults; no parameters, no I/O. |
| `DeadLetterStreamIndex.cs` | 90 | 90 | 0 | 0 | A bounded concurrent map; unit tests cover eviction, guard tests cover the arguments. |
| `DeadLetterEventStoreDBErrorCodes.cs` | 90 | 0 | 0 | 0 | Constants and category mapping, no parameters. |
| `DeadLetterEventStoreDBLog.cs` | 70 | 0 | 0 | 0 | Source-generated `[LoggerMessage]` partials reached through the store. |
| `EventStoreDBHealthCheck.cs` | 85 | 90 | 70 | 0 | Unit tests mock the client for healthy, cap-hit and projection-missing outcomes; integration reaches the real probe. |
| `ServiceCollectionExtensions.cs` (package and dead letter) | 90 | 95 | 0 | 0 | Registration code reached by the DI test and by null guards. |
| `EncinaEventStoreDBOptions.cs` | 90 | 0 | 0 | 0 | Properties, `[JsonIgnore]` and `ToString()`; no parameters. |

<details>
<summary>Tasks</summary>

1. **Unit**: stream-name helper (stable, collision guard), event folding, filter predicates, error mapping, claim outcomes, with the client mocked (NSubstitute) and a `FakeTimeProvider`.
2. **Guard**: every public method and the registration extension.
3. **Contract/integration**: `DeadLetterStoreContract` derived against a real EventStoreDB container (a new `EventStoreDbFixture` in `Encina.TestInfrastructure` with a shared `[Collection]`); plus: concurrent claim from two stores, repeated capture after a soft delete (a regression test of the Phase 0 spike result), a hash-collision capture returns a `Left`, the scan cap returns `dlq.scan_limit_exceeded`, a read through the category streams with 1,000 dead letters.
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
| EventStoreDB docs (vendor) | Expected-state append, soft delete, `$all` filters | Verified by the Phase 0 spike and re-checked by the Phase 5 integration tests before the ADR is written |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in a new IDeadLetterStore backend |
|-----------|----------|------------------------------------------|
| `IDeadLetterStore` | `src/Encina.Messaging/DeadLetter/IDeadLetterStore.cs` | The contract this backend implements |
| `IDeadLetterMessage`, `DeadLetterData`, `IDeadLetterMessageFactory` | `src/Encina.Messaging/DeadLetter/` | The record and its factory |
| `DeadLetterFilter`, `DeadLetterErrorCodes`, `DeadLetterStoreLimits`, `DeadLetterSourcePatterns` | `src/Encina.Messaging/DeadLetter/` | Filter translation, error codes, limits, the six source patterns |
| `AddDeadLetterQueueServices` | `src/Encina.Messaging/MessagingServiceCollectionExtensions.cs:495` (wrappers in `src/Encina.Messaging/DeadLetter/DeadLetterServiceCollectionExtensions.cs`) | First-wins registration shared by all providers |
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
| Package skeleton (Phase 1) | ~11 | csproj, `Directory.Packages.props`, `Encina.slnx` row, options, registration, PublicAPI x2, README, manifest, unit and guard tests of the skeleton |
| Dead letter production code (Phases 1-4) | ~14 | Stream names, events, state, factory, store, options, stream index, error codes, registration, health check, log, `EventIdRanges.cs`, `src/Encina/PublicAPI.Unshipped.txt`, package `PublicAPI.Unshipped.txt` |
| Tests (Phases 1-5) | ~14 | Unit, guard, DI test, decorator test, contract/integration, property, `EventStoreDbFixture`, `AssemblyRanges` test entry, test-project `ProjectReference` edits |
| Manifest rows, feature page, changelog, ADR, INVENTORY, load/benchmark justification files | ~7 | Documentation phase |
| **Total** | **~46** | Exceeds 15 files. Tests and the log class travel with the code that needs them; each pull request builds and passes alone |

Proposed split (each at most 15 files and 1500 lines, counting tests, manifest rows, PublicAPI files, `EventIdRanges.cs`, slnx and props edits and test `ProjectReference` edits):

| PR | Content | Files | Estimated lines | Verifiable alone because |
|----|---------|-------|-----------------|--------------------------|
| 1 | Package skeleton: csproj, `Directory.Packages.props`, `Encina.slnx`, `EncinaEventStoreDBOptions`, `AddEncinaEventStoreDB`, PublicAPI x2, README, manifest, unit and guard tests (owned by #585; #576 rebases, Design Choice 4b) | ~11 | ~450 | The client registration is tested on its own |
| 2 | Stream names, events, state, factory with their unit and guard tests, PublicAPI lines, manifest rows | ~9 | ~700 | Pure types with their own tests |
| 3 | Store, error codes with categories, options, `DeadLetterStreamIndex`, log class, `EventIdRanges.cs`, `AssemblyRanges` test entry, `src/Encina/PublicAPI.Unshipped.txt`, package PublicAPI, manifest rows, unit and guard tests | ~14 | ~1,400 | The store builds with its log and options and is tested on a mocked client |
| 4 | `AddEncinaDeadLetterEventStoreDB`, health check, DI test (`ValidateOnBuild`, `ValidateScopes`), health and guard tests, decorator test, manifest rows, PublicAPI lines | ~8 | ~600 | DI test against the store of PR 3 |
| 5 | `EventStoreDbFixture` and the `Testcontainers.EventStoreDb` pin, test-project `ProjectReference` edits, derived contract class, integration cases, property tests, load and benchmark justification files, manifest rows | ~10 | ~1,200 | Integration and property flags reach their targets on a real server |
| 6 | Package README section, `docs/features/dead-letter-queue.md`, `docs/INVENTORY.md`, changelog fragment, ADR | ~5 | ~400 | Documentation only |

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
Phase 2: the 10 contract methods; uniqueness by StreamState.NoStream on a source-key stream; claim by expected revision; delete by tombstone plus soft delete.
Phase 3: AddEncinaDeadLetterEventStoreDB over AddDeadLetterQueueServices; health check; DI test.
Phase 4: EventIdRanges.EventStoreDBDeadLetter 6925-6934, Log class, architecture test entry.
Phase 5: unit, guard, contract/integration (container), property tests; manifest.
Phase 6: docs, changelog fragment, ADR (number assigned when its pull request opens).

KEY PATTERNS:
- Store DeadLetterStoreEventStoreDB; DI AddEncinaDeadLetterEventStoreDB() first-wins.
- Dilemmas D1-D4 are recommended, pending the maintainer: do not start Phase 2 before D1-D3 are answered, nor the first pull request before D4.
- Unverified server behavior (NoStream after a soft delete, $by_category, scavenge) is checked by the Phase 0 spike before the design is frozen.
- Every new error code carries its ADR-062 ErrorCategory (the package is not in ErrorCategoryPending.txt).

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

- #583 (closed) fixed the contract. #576 (EventStoreDB audit store, open) shares the package: the skeleton is the first pull request of this plan and #576 depends on it (Design Choice 4b, recommended, pending the maintainer). #2013 (open) later adds `ActorId` and `CausationId` to the record.
- `Testcontainers.EventStoreDb` must be added to `Directory.Packages.props`; `docker-compose.yml` gets an optional EventStoreDB service in the `core` or a new profile only if the maintainer wants local runs beyond Testcontainers.
- No decision of another issue blocks this plan; D1-D3 block Phase 2 and D4 blocks the first pull request.

---

## Next Steps

1. The maintainer answers D1, D2, D3 and D4 when work on #585 starts, after the Phase 0 spike; the answers go into `## Maintainer Decisions`.
2. Re-check the Design Choices against main and the state of #576; `adversarial-reviewer` verifies the Settled ones.
3. Brief a worker per proposed pull request (6 pull requests, see the file count).
4. Final commit references `Fixes #585`.
