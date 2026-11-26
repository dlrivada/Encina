# Implementation Plan: Durable write-behind queue for cache strategies

> **Issue**: [#4411](https://github.com/dlrivada/Encina/issues/4411)
> **Type**: Feature
> **Complexity**: High (12 pull requests: 8, 12, 15, eight provider pull requests of 11 or fewer files, and a closing one of 6 files)
> **Estimated Scope**: about 1,500 lines of production code, about 2,800 lines of tests
> **Milestone**: v0.18.0 — Integration & Platform Capabilities

---

## Summary

Issue #4411 adds an opt-in durable queue under the write-behind strategy of #272, so queued writes survive a process crash, with an `IWriteBehindQueue` abstraction extracted from the internal in-memory queue and a durable implementation per provider of the 10-provider database matrix. Re-checked against `origin/main` (19b761ffa):

| Premise of the issue | State in the code |
|----------------------|-------------------|
| "Extract `IWriteBehindQueue` from the internal `WriteBehindQueue` of #272" | Nothing to extract yet: `src/Encina.Caching` has no `Patterns/`, `Services/` or write-behind file on main (#272 is a merged plan, not code). This plan starts after #272 PR 3 (queue, behavior) and PR 4 (flush service, `AddEncinaWriteBehind`). |
| A durable queue can reuse the outbox or the scheduled-message store of `Encina.Messaging` (issue D1) | Not as they are. `IOutboxStore.GetPendingMessagesAsync(batchSize, maxRetries)` (`IOutboxStore.cs:40-43`) and `IScheduledMessageStore.GetDueMessagesAsync(batchSize, maxRetries)` (`IScheduledMessageStore.cs:40-43`) return every due item with no request-type filter, so their own processors would consume a write-behind item as a notification (`OutboxProcessorBase.cs:179` fails on a type that is not an `INotification`) or dispatch it as a scheduled request. Neither `IOutboxMessage` nor `IScheduledMessage` carries a tenant (`ScheduledMessageProcessor.cs:42`: "does not carry TenantId yet. See Issue #739"). |
| "10 providers" | Eight packages: ADO.NET SqlServer, PostgreSQL, MySQL; Dapper SqlServer, PostgreSQL, MySQL; `Encina.EntityFrameworkCore` (one package, three databases, tested per database); `Encina.MongoDB`. Their stores live in the provider packages, which reference `Encina.Messaging` and not `Encina.Caching` (`Encina.ADO.SqlServer.csproj`), so a store contract must sit in `Encina.Messaging`, as `IScheduledMessageStore` does. |
| "Enqueue with the unit of work" (cross-cutting row 8) | The existing stores enlist in the scope's shared transaction (`ScheduledMessageStoreADO.cs:19,61`: `IDbTransactionAccessor`, `DbLease.Enlisted`), so a store added in the request's scope stages its row with the application's own commit. The orchestrator never calls `SaveChangesAsync`; the plan claims nothing beyond "staged on the same scope transaction, committed by whoever owns the commit". |
| Cache providers are not involved | True: the queue is behavior-level and independent of the 8 cache providers. |

**Shared pieces and ownership (family cache-patterns-2):** #272 owns the write-behind types this plan extracts and changes (`WriteBehindQueue`, `WriteBehindPipelineBehavior`, `WriteBehindFlushService`, `AddEncinaWriteBehind`, `CacheStrategyLog` 500-507, the `WriteBehind` sub-options of `CachingOptions`). This plan owns `IWriteBehindStore` and `WriteBehindItem` (`Encina.Messaging`), `IWriteBehindQueue`, the durable queue and its EventIds 520-523 (range `CachingWriteBehindDurable`, free inside block 500-549: the merged plans use 500-519 there, and #276 and #278 took the 30350 and 30400 slots). #4412 uses 524-525 and #4413 uses 526-527; 528-549 stay free.

**Affected packages:** `Encina.Messaging`, `Encina.Caching`, `Encina` (EventId registry), the eight provider packages above. **Provider category:** database (10) for the store; the 8 cache providers are not involved.

---

## Design Choices

<details>
<summary><strong>1. Where the durable queue lives (Dilemma D1, the issue's D1 to D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A dedicated `IWriteBehindStore` in `Encina.Messaging`, implemented by the eight provider packages; `AddEncinaWriteBehind` selects it with `Queue = Durable`** | Purpose-built: tenant, request type, lease, stable batch id; no interference with the outbox and scheduling processors; the in-memory queue stays the default | Ten implementations in eight packages |
| **B) Reuse `IScheduledMessageStore` (or the outbox), adding a request-type filter and a tenant to their contracts** | One store family; scheduling already exists in the 10 providers | The contract change touches every store and processor of two patterns to serve a third; the scheduler would still need to ignore the items; write-behind batches are not scheduled messages |
| **C) Keep the in-memory queue only** | No work | A crash loses queued writes; the issue is closed without code |

### Chosen Option: **A - dedicated store** (recommended, pending the maintainer)

### Rationale

- The evidence in the Summary shows reuse (B) needs the same provider-wide work as A plus a risk to two shipped patterns. Option B wins when the application already runs the scheduler and accepts a tenant and type filter on it; option C wins when a documented crash window is acceptable (the #272 default).
- The rest of this plan describes A in detail; under C the plan closes with no code, and the follow-up work of the other two plans of this batch is unaffected.
- Class: Dilemma

</details>

<details>
<summary><strong>2. Claim, lease and redelivery (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The store claims atomically: `ClaimBatchAsync(owner, batchSize, leaseDuration)` sets `LeaseOwner`, `LeasedUntilUtc` and a stable `BatchId` on the claimed rows; an expired lease makes the whole batch claimable again with the same `BatchId`** | The store is the only coordinator; a crashed node's batch is redelivered whole with the same `BatchId`, so the handler's deduplication of #272 works; no extra package | Each provider needs an atomic claim (SQL Server `UPDATE ... OUTPUT` with `UPDLOCK, READPAST`, PostgreSQL `FOR UPDATE SKIP LOCKED`, MySQL `SKIP LOCKED` in a transaction, MongoDB `findOneAndUpdate`) |
| **B) An `IDistributedLockProvider` lock around a read-only fetch** | The store is a plain read and write | A lock round trip per batch; with the in-memory lock provider nothing is coordinated; redelivery after a crash needs a processed marker anyway |

### Chosen Option: **A - lease in the store** (recommended, pending the maintainer)

### Rationale

- Option B wins when the application already runs a database-backed or Redis lock provider and wants the simplest store.
- A claim selects the oldest claimable item, then up to `batchSize - 1` more with the same `RequestType` and the same `TenantId`: a batch never mixes tenants (the #272 rule).
- A completed batch is deleted (the rows are transient work items, not a record); a failed attempt releases the lease with `RetryCount + 1` and `NextRetryAtUtc`; at `FlushRetryCount` the batch goes to the `IWriteBehindDeadLetterSink` of #272 and is then deleted, so the backlog of an offboarded tenant drains instead of growing.
- Class: Dilemma

</details>

<details>
<summary><strong>3. Ordering (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Oldest first inside a claimed batch; no order across batches** | Parallel replay on several nodes; simple claim | A retried batch can run after a newer one |
| **B) Group-exclusive: a (request type, tenant) group is claimed by one node at a time, strictly oldest first** | Order holds for last-write-wins updates | One node per group; a slow batch blocks its group; a harder claim statement in ten implementations |

### Chosen Option: **A - best-effort order** (recommended, pending the maintainer)

### Rationale

- The issue's use cases are counters and audit-light updates, which commute. Option B wins when queued commands overwrite each other (set a status), at the cost of throughput.
- The documentation states the rule and tells handlers to use the `BatchId` and the item order inside the batch.
- Class: Dilemma

</details>

<details>
<summary><strong>4. Payload and request type resolution (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The payload is the request serialized with `System.Text.Json`; the type is stored as a stable name from an allow-list built at startup from the registered `[Cache(Strategy = WriteBehind)]` request types; an unknown name or an undecodable payload is dead-lettered (EventId 521), never instantiated** | No type is created from stored text; a renamed type fails visibly; ties to the types `IWriteBehindBatchHandler<TRequest>` is registered for | A request type needs a stable name (default: the type's full name; an attribute-free override is not offered) |
| **B) The assembly-qualified type name and `Type.GetType`, as the outbox does (`OutboxOrchestrator.cs:97`)** | Same as existing stores; no registry | A tampered row can name any loadable type |

### Chosen Option: **A - allow-list** (recommended, pending the maintainer)

### Rationale

- Option B wins when consistency with the outbox matters more than the closed set. The stored request may hold personal data: it lives in the application's own database like the outbox `Content`, which the documentation states; `EncinaError.Message` is never stored (the row keeps `LastErrorCode` only, AGENTS.md section 3).
- Class: Dilemma

</details>

<details>
<summary><strong>5. The durable store is unavailable at enqueue (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The enqueue returns `Left` `cache.write_behind_store_unavailable` (ADR-062 `ErrorCategory.Unavailable`, constant message "The write-behind store is unavailable."), the handler does not run, EventId 520 logs the code** | The durability promise is never silently dropped (fail closed) | A database outage turns write-behind requests into errors |
| **B) Fall back to the in-memory queue for that item** | Requests keep working | The item loses durability exactly when the store is failing; the caller sees success |

### Chosen Option: **A - reject** (recommended, pending the maintainer)

### Rationale

- Option B wins for counters where availability beats durability. The same code is used by the replay path when the store cannot be read (logged by EventId 523, never returned to a caller: it is a background `Left`, retried on the next tick, AGENTS.md section 3).
- Class: Dilemma

</details>

<details>
<summary><strong>6. Every provider of the matrix implements the store</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) All ten providers (eight packages), each with the same `IWriteBehindStore` contract tests** | Switching provider means changing the DI registration | Nine provider pull requests |
| **B) A subset** | Fewer pull requests | Breaks provider coherence |

### Chosen Option: **A - all providers**

### Rationale

- **Class:** Settled
- **Settled by:** AGENTS.md section 5 ("every provider-dependent feature MUST be implemented for all 10 providers"; "Tests of a provider-dependent feature MUST cover all 10 database providers"). It applies to any durable store, whichever option D1 chooses; under D1 option C there is no store and nothing to implement.
- Registration follows the existing store flags (`config.UseAuditLogStore`, `config.UseReadAuditStore`, `src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs:93-120`): a new flag `MessagingConfiguration.UseWriteBehindStore` (default false) makes each provider register `IWriteBehindStore` with `TryAdd`.

</details>

---

## Implementation Phases

### Phase 1: The store contract in `Encina.Messaging` (pull request 1)

<details>
<summary><strong>Tasks</strong></summary>

PR 1 holds exactly these 8 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Messaging/WriteBehind/WriteBehindItem.cs` | new (public sealed record: `Id`, `RequestType`, `TenantId`, `Content`, `CreatedAtUtc`, `RetryCount`, `NextRetryAtUtc`, `BatchId`, `LeaseOwner`, `LeasedUntilUtc`, `LastErrorCode`) |
| 2 | `src/Encina.Messaging/WriteBehind/IWriteBehindStore.cs` | new (public interface, ROP) |
| 3 | `src/Encina.Messaging/MessagingConfiguration.cs` | modify (`UseWriteBehindStore`) |
| 4 | `src/Encina.Messaging/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 5 | `tests/Encina.UnitTests/Messaging/MessagingConfigurationPatternFlagsTests.cs` | modify (default false) |
| 6 | `tests/Encina.UnitTests/Messaging/WriteBehindItemTests.cs` | new |
| 7 | `tests/Encina.IntegrationTests/Infrastructure/Messaging/WriteBehindStoreContractTestsBase.cs` | new (abstract contract tests every provider test class derives from; it runs from PR 4) |
| 8 | `.github/coverage-manifest/Encina.Messaging.json` | modify (`mechanical-fixer`) |

Tasks:

1. **Create** `IWriteBehindStore`: `Task<Either<EncinaError, Unit>> AddAsync(WriteBehindItem item, CancellationToken)`; `Task<Either<EncinaError, IReadOnlyList<WriteBehindItem>>> ClaimBatchAsync(string leaseOwner, int batchSize, TimeSpan leaseDuration, CancellationToken)` (Design Choice 2: expired-lease batches first, whole and with their `BatchId`; otherwise the oldest group, tenant never mixed); `CompleteAsync(IReadOnlyCollection<Guid> ids, CancellationToken)` deletes; `ReleaseAsync(IReadOnlyCollection<Guid> ids, string errorCode, DateTime nextRetryAtUtc, CancellationToken)` clears the lease and adds one retry; `GetBacklogAsync(CancellationToken)` returns the count and the oldest `CreatedAtUtc` (no tenant breakdown, so no per-tenant metric series). Every method is asynchronous with a token and returns `Either`; time is passed in or read from `TimeProvider`, never `DateTime.UtcNow`.
2. **Create** `WriteBehindItem` (`TenantId` is stored verbatim in its own column; the implicit single-tenant mode stores the reserved segment `default`, a convention of #275 and #272; grouping compares `(RequestType, TenantId)` as two columns, so no two pairs can collide, and under multi-tenancy a request with no tenant never reaches the store, #272).
3. **Modify** `MessagingConfiguration` (`public bool UseWriteBehindStore { get; set; }` with XML docs).
4. **Tests:** record equality and `with`; the abstract contract base (claim never mixes tenants, an expired lease redelivers the same `BatchId`, complete deletes, release adds a retry, concurrent claims never return the same row twice) is exercised by the provider pull requests; the flag default is asserted in the existing test class.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #4411 adds a durable queue for the write-behind cache strategy of #272. Provider packages reference
Encina.Messaging and not Encina.Caching, so the store contract lives in Encina.Messaging (pattern: IScheduledMessageStore).

TASK:
1. Create WriteBehindItem and IWriteBehindStore under src/Encina.Messaging/WriteBehind/ and the UseWriteBehindStore flag.
2. Write the unit tests and the abstract contract test base; ask mechanical-fixer for PublicAPI.Unshipped.txt and the manifest rows.

KEY RULES:
- Either<EncinaError, T>, async with CancellationToken, no DateTime.UtcNow.
- A claim never mixes tenants; redelivery keeps the BatchId; no EncinaError.Message is stored.
- Follow the maintainer's answers to Design Choices 1 to 3 given in your brief.

REFERENCE FILES:
- src/Encina.Messaging/Scheduling/IScheduledMessageStore.cs
- src/Encina.Messaging/MessagingConfiguration.cs
- tests/Encina.UnitTests/Messaging/MessagingConfigurationPatternFlagsTests.cs
```

</details>

### Phase 2: Extract `IWriteBehindQueue` from the in-memory queue (pull request 2)

<details>
<summary><strong>Tasks</strong></summary>

PR 2 starts after #272 PR 4 and holds exactly these 12 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Abstractions/IWriteBehindQueue.cs` | new (public: `EnqueueAsync`, `ClaimAsync`, `CompleteAsync`, `ReleaseAsync`, `GetBacklogAsync`; result types `WriteBehindClaim`) |
| 2 | `src/Encina.Caching/Patterns/WriteBehindQueue.cs` | modify (implements the interface; complete and release are no-ops over the channel, redelivery is the flush retry of #272) |
| 3 | `src/Encina.Caching/Behaviors/WriteBehindPipelineBehavior.cs` | modify (enqueue through the interface) |
| 4 | `src/Encina.Caching/Services/WriteBehindFlushService.cs` | modify (resolve the queue from an `IServiceScopeFactory` scope per tick and use claim, complete, release) |
| 5 | `src/Encina.Caching/WriteBehindServiceCollectionExtensions.cs` | modify (register the in-memory queue as `IWriteBehindQueue`) |
| 6 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 7 | `tests/Encina.UnitTests/Caching/WriteBehindQueueTests.cs` | modify |
| 8 | `tests/Encina.UnitTests/Caching/WriteBehindPipelineBehaviorTests.cs` | modify |
| 9 | `tests/Encina.UnitTests/Caching/WriteBehindFlushServiceTests.cs` | modify |
| 10 | `tests/Encina.UnitTests/Caching/WriteBehindRegistrationTests.cs` | modify (`ValidateOnBuild`, `ValidateScopes`) |
| 11 | `tests/Encina.ContractTests/Caching/WriteBehindContractTests.cs` | modify (contract of the interface over the in-memory queue) |
| 12 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |

Tasks:

1. **Create** `IWriteBehindQueue` with the semantics of Phase 1 (claim returns a `WriteBehindClaim`: batch id, tenant id, request type, items). The in-memory queue keeps its behavior: it is the default and registered with `TryAdd`.
2. **Modify** the flush service: the singleton never holds a scoped service (the durable queue of PR 3 is scoped because its store is); each tick opens a scope, resolves the queue and the batch handler there, claims, runs the handler, then completes or releases. Cancellation propagates and is never a release or a dead letter.
3. **Tests:** every existing #272 test keeps passing through the interface; the contract test asserts claim, complete and release on the in-memory queue; the DI test proves the flush service resolves its queue per scope.
4. No changelog fragment (a refactor with no user-visible change; PR 3 carries the fragment).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
#272 PR 3 and PR 4 are merged: WriteBehindQueue (internal), WriteBehindPipelineBehavior, WriteBehindFlushService and AddEncinaWriteBehind exist.

TASK:
1. Extract the public IWriteBehindQueue (enqueue, claim, complete, release, backlog) from WriteBehindQueue; the in-memory queue implements it.
2. Make the behavior and the flush service use the interface; the flush service resolves it per scope.
3. Update the tests; ask mechanical-fixer for PublicAPI.Unshipped.txt and the manifest rows.

KEY RULES:
- Behavior does not change for existing users: the in-memory queue stays the default.
- A singleton never injects a scoped service; cancellation is never a Left, a release or a dead letter.

REFERENCE FILES:
- src/Encina.Caching/Patterns/WriteBehindQueue.cs
- src/Encina.Caching/Services/WriteBehindFlushService.cs
- src/Encina.Caching/WriteBehindServiceCollectionExtensions.cs
```

</details>

### Phase 3: The durable queue, options and registration (pull request 3)

<details>
<summary><strong>Tasks</strong></summary>

PR 3 holds exactly these 15 files:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.Caching/Patterns/DurableWriteBehindQueue.cs` | new (internal sealed, scoped, over `IWriteBehindStore`) |
| 2 | `src/Encina.Caching/Patterns/WriteBehindRequestRegistry.cs` | new (internal sealed: stable name to request type, built at startup) |
| 3 | `src/Encina.Caching/Configuration/WriteBehindQueueKind.cs` | new (public enum `InMemory`, `Durable`) |
| 4 | `src/Encina.Caching/Configuration/CachingOptions.cs` | modify (`WriteBehind.Queue`, `LeaseDuration`) |
| 5 | `src/Encina.Caching/WriteBehindServiceCollectionExtensions.cs` | modify (durable registration and the startup check that an `IWriteBehindStore` is registered) |
| 6 | `src/Encina.Caching/Diagnostics/CacheWriteBehindDurableLog.cs` | new (EventIds 520-523) |
| 7 | `src/Encina.Caching/Diagnostics/CachePatternMetrics.Strategies.cs` | modify (gauge `encina.cache.write_behind.durable_backlog`, replay span) |
| 8 | `src/Encina/Diagnostics/EventIdRanges.cs` | modify (`CachingWriteBehindDurable = (520, 523)`) |
| 9 | `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs` | modify (add the range name to the `Encina.Caching` list) |
| 10 | `docs/engineering/eventid-block-registry.md` | modify (row `CachingWriteBehindDurable \| 520-523 \| Caching \| historical` in "Registered ranges" and the block row split; read `EventIdRegistryAgreementTests` first) |
| 11 | `tests/Encina.UnitTests/Caching/DurableWriteBehindQueueTests.cs` | new (includes an in-test fake `IWriteBehindStore`, the registry and the metrics listener) |
| 12 | `tests/Encina.UnitTests/Caching/WriteBehindRegistrationTests.cs` | modify (durable registration, missing store fails at start) |
| 13 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`) |
| 14 | `src/Encina.Caching/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 15 | `src/Encina/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`: the new `EventIdRanges` field) |

The health check change and the changelog fragment are in PR 12, which has room, so this pull request stays at 15 files.

Tasks:

1. **Create** `DurableWriteBehindQueue`: `EnqueueAsync` serializes the request with the registry's stable name and adds a `WriteBehindItem` (tenant verbatim, `default` in single-tenant mode); a store `Left` returns `cache.write_behind_store_unavailable` (Design Choice 5) and logs EventId 520 with the code only; `ClaimAsync` maps rows back through the registry, dead-letters an unknown name or undecodable payload (EventId 521, request type name only), and logs EventId 522 when a claim carries an already-used `BatchId` (a reclaimed batch).
2. **Create** `WriteBehindRequestRegistry` from the request types that have a registered `IWriteBehindBatchHandler<TRequest>`.
3. **Modify** `AddEncinaWriteBehind`: with `Queue = Durable` it registers the durable queue as scoped, replaces the in-memory registration of PR 2 (`Replace`, because the application chose it explicitly), and fails at host start when no `IWriteBehindStore` is registered (the startup check uses `IServiceProviderIsService`; DI test built with `ValidateOnBuild` and `ValidateScopes`).
4. **Observability (ADR-054 D4):** the gauge `encina.cache.write_behind.durable_backlog` reports the total, with no tenant tag; the replay span `encina.cache.write_behind.replay` carries `encina.tenant_id` only when multi-tenancy is on; EventId 523 logs a store failure during replay (retried on the next tick, never swallowed as success).
5. **Tests:** enqueue maps tenant and type, a failing store returns the `Left` with the constant message, unknown types are dead-lettered, a batch is redelivered whole with the same `BatchId`, a tenant-less request never reaches the queue (the #272 rule), the startup check, the gauge and tag rules.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
Phases 1 and 2 are merged: IWriteBehindStore and IWriteBehindQueue exist, the flush service claims, completes and releases.

TASK:
1. Create DurableWriteBehindQueue, WriteBehindRequestRegistry, WriteBehindQueueKind and the Queue and LeaseDuration options; register the durable queue from AddEncinaWriteBehind.
2. Create CacheWriteBehindDurableLog (EventIds 520-523), register CachingWriteBehindDurable = (520, 523) in EventIdRanges.cs, the allocation test and the registry document.
3. Add the backlog gauge and the replay span; write the unit and DI tests; ask mechanical-fixer for PublicAPI and manifest rows.

KEY RULES:
- Store failure on enqueue returns Left cache.write_behind_store_unavailable (ErrorCategory.Unavailable, constant message); never fall back silently.
- No type is created from stored text outside the registry; the singleton flush service never holds the scoped queue.
- Metrics carry the tenant tag only with multi-tenancy on; logs carry codes and request type names only.
- Follow the maintainer's answers to Design Choices 4 and 5 given in your brief.

REFERENCE FILES:
- src/Encina.Caching/WriteBehindServiceCollectionExtensions.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- docs/engineering/eventid-block-registry.md
```

</details>

### Phase 4: Provider stores (pull requests 4 to 11)

<details>
<summary><strong>Tasks</strong></summary>

One pull request per provider package, in this order: ADO.SqlServer (4), ADO.PostgreSQL (5), ADO.MySQL (6), Dapper.SqlServer (7), Dapper.PostgreSQL (8), Dapper.MySQL (9), EntityFrameworkCore (10, three databases), MongoDB (11). Each starts after PR 1 and is independent of PR 2 and PR 3. Each pull request holds 11 files or fewer; the ADO.SqlServer one:

| # | File | Change |
|---|------|--------|
| 1 | `src/Encina.ADO.SqlServer/WriteBehind/WriteBehindStoreADO.cs` | new (public sealed, ctor like `ScheduledMessageStoreADO`: connection, `TimeProvider`, optional `IDbTransactionAccessor`) |
| 2 | `src/Encina.ADO.SqlServer/Scripts/031_CreateWriteBehindMessagesTable.sql` | new (the next free number of that package's `Scripts/`) |
| 3 | `src/Encina.ADO.SqlServer/Scripts/000_CreateAllTables.sql` | modify |
| 4 | `src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs` | modify (`RegisterWriteBehindStore` when `UseWriteBehindStore`, `TryAdd`) |
| 5 | `src/Encina.ADO.SqlServer/PublicAPI.Unshipped.txt` | modify (`mechanical-fixer`) |
| 6 | `tests/Encina.TestInfrastructure/Schemas/SqlServerSchema.cs` | modify (table and `ClearAllDataAsync`; shared with the Dapper SqlServer pull request, so only the first of the two edits it) |
| 7 | `tests/Encina.IntegrationTests/ADO/SqlServer/WriteBehind/WriteBehindStoreADOTests.cs` | new (`[Collection("ADO-SqlServer")]`, derives the contract base) |
| 8 | `tests/Encina.GuardTests/ADO/SqlServer/WriteBehindStoreADOGuardTests.cs` | new (constructor and method arguments) |
| 9 | `tests/Encina.UnitTests/ADO/SqlServer/ServiceCollectionExtensionsTests.cs` | modify (flag registers the store; flag off does not) |
| 10 | `.github/coverage-manifest/Encina.ADO.SqlServer.json` | modify (`mechanical-fixer`) |
| 11 | `changelog.d/4411-cache-write-behind-store-ado-sqlserver.added.md` | new (`mechanical-fixer`) |

Provider differences (every one proven by the contract base against the real database):

| Provider | Atomic claim | Notes |
|----------|--------------|-------|
| ADO.NET and Dapper SqlServer | `UPDATE TOP (@n) ... WITH (UPDLOCK, READPAST) OUTPUT inserted.*` | `@param`, `bit`, native `DateTime` and `GUID` |
| ADO.NET and Dapper PostgreSQL | `UPDATE ... WHERE id IN (SELECT ... FOR UPDATE SKIP LOCKED LIMIT @n) RETURNING *` | case-sensitive identifiers |
| ADO.NET and Dapper MySQL | transaction with `SELECT ... FOR UPDATE SKIP LOCKED LIMIT @n`, then `UPDATE` | backtick identifiers, `0/1` booleans |
| EF Core (SqlServer, PostgreSQL, MySQL) | the three statements above through `FromSql` inside one transaction | one entity, configuration and store in `src/Encina.EntityFrameworkCore/WriteBehind/`; schema through `EFCoreSchema.cs`; three integration test classes |
| MongoDB | `findOneAndUpdate` claim per candidate with a unique claim token, then a find by token | no script; collection and indexes created like the other Mongo stores |

Common rules: every database call is asynchronous with a `CancellationToken` (no `Open()`); a `DbException` becomes a `Left` created by `EncinaErrors.FromException` with `ErrorCategory.Unavailable` and a constant message (the existing `ScheduledMessageStoreADO.cs` has no `Left` or `catch`, so the new stores deliberately follow the interface documentation, not that file); cancellation propagates as `OperationCanceledException`; time from `TimeProvider`; the `Content` column is never logged.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
PR 1 is merged: IWriteBehindStore, WriteBehindItem and the abstract contract test base exist.

TASK:
For ONE provider package (named in your brief): create the store, the schema script (and the aggregate script), the DI registration behind UseWriteBehindStore, the
test schema and cleanup, the integration test deriving the contract base, the guard test and the DI unit test; ask mechanical-fixer for PublicAPI, manifest rows and the fragment.

KEY RULES:
- The claim is atomic in the database (table in the plan); a batch never mixes tenants; a redelivered batch keeps its BatchId.
- Async calls with CancellationToken only; failures become Left (ErrorCategory.Unavailable, constant message); cancellation propagates.
- Integration tests use the shared [Collection] fixture of the family; never a per-class container.
- One package per pull request, 15 files or fewer.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/Scheduling/ScheduledMessageStoreADO.cs
- src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs
- tests/Encina.IntegrationTests/ADO/SqlServer/Scheduling/ScheduledMessageStoreADOTests.cs
- tests/Encina.TestInfrastructure/Schemas/SqlServerSchema.cs
```

</details>

### Phase 5: Restart integration test, documentation and finalization (pull request 12)

<details>
<summary><strong>Tasks</strong></summary>

PR 12 holds exactly these 6 files (plus the two moved from PR 3 if the worker moved them):

| # | File | Change |
|---|------|--------|
| 1 | `tests/Encina.IntegrationTests/Infrastructure/Caching/DurableWriteBehindRestartTests.cs` | new (PostgreSQL shared fixture: enqueue, dispose the service provider, build a new one, the flush service replays; the handler sees the same `BatchId`) |
| 2 | `docs/features/handler-caching.md` | modify (`docs-writer`: "Durable write-behind" section) |
| 3 | `changelog.d/4411-cache-durable-write-behind.added.md` | new (`mechanical-fixer`) |
| 4 | `.github/coverage-manifest/Encina.Caching.json` | modify (`mechanical-fixer`: the integration targets below) |
| 5 | `src/Encina.Caching/Health/WriteBehindHealthCheck.cs` | modify (backlog age: Degraded when the oldest claimable item is older than a configured age; moved from PR 3) |
| 6 | `tests/Encina.UnitTests/Caching/WriteBehindHealthCheckTests.cs` | modify |

Tasks: the restart test proves the acceptance criterion "queued writes survive a process restart"; the documentation states the at-least-once rule, the best-effort order, the stored personal data and that `BatchId` is the deduplication key (cites tests by name, no typed figure). XML docs with `<example>` on `IWriteBehindStore`, `IWriteBehindQueue`, `WriteBehindQueueKind` and the option members.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
All provider stores and the durable queue are merged.

TASK:
Write the restart integration test on the shared PostgreSQL fixture, move the health check backlog-age change here if PR 3 left it, ask docs-writer for the handler-caching.md section, and ask mechanical-fixer for the fragment and manifest rows.

KEY RULES:
- The test never creates its own container; it never sleeps (FakeTimeProvider drives the lease).
- No hand-typed figures in docs.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Infrastructure/Caching/CacheStrategiesIntegrationTests.cs
- docs/features/handler-caching.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Relevance |
|----------|-----------|
| AGENTS.md section 3 | ROP, async database calls, background errors never swallowed, loops never throw, time from `TimeProvider`, no message stored in plaintext |
| AGENTS.md section 5 | The store is implemented for all 10 database providers; the 8 cache providers are not involved |
| ADR-062 | `cache.write_behind_store_unavailable` is `Unavailable`; the completeness scan needs one category per code and a constant message |
| ADR-054 D4 | Tenant tag on telemetry only with multi-tenancy on |
| ADR-021 | Range registered before use |
| Hazelcast MapStore write-behind and NCache write-behind (named in the issue) | Comparable products that persist the queue; at-least-once with replay |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `IScheduledMessageStore` and its stores in the eight provider packages | `src/Encina.Messaging/Scheduling/`, `src/Encina.ADO.SqlServer/Scheduling/ScheduledMessageStoreADO.cs` | Pattern for contract placement, constructor, scope-transaction enlistment, scripts and tests |
| `IDbTransactionAccessor`, `DbLease.Enlisted` | `src/Encina.Messaging/Transactions/` | The enqueue is staged on the scope transaction |
| Store flags and `Register*Stores` | `src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs:93-120` | `UseWriteBehindStore` registration |
| Shared fixtures and schemas | `tests/Encina.TestInfrastructure/Fixtures/`, `tests/Encina.TestInfrastructure/Schemas/` | Collections and table clean-up |
| Write-behind types of #272 | `src/Encina.Caching` (after #272 PR 4) | Extracted and extended |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Caching` (new range `CachingWriteBehindDurable`, block 500-549 of the plan batch registry, family cache-patterns-2) | 520-523 | 520 durable enqueue failed (code only), 521 stored item not decodable or type not registered (dead-lettered), 522 batch redelivered after an expired lease, 523 store unavailable during replay. Verified free: the merged plans use 500-519 in this block and a grep of `docs/plans` finds no id 520-549 claimed (the 520-536 lines in the #274 and #275 plans are superseded by the 30350 and 30400 slots of #276 and #278). #4412 takes 524-525 and #4413 takes 526-527; the batch B plans of this family start at 528 |

### Estimated File Count

| Category | New Files | Modified Files | Notes |
|----------|-----------|----------------|-------|
| Production code | about 26 | about 22 | New: item, store contract, queue interface, durable queue, registry, enum, log, eight stores plus the EF entity and configuration, six SQL scripts; modified: options, eight provider DI files, flush service, behavior, metrics, health check, `EventIdRanges.cs`, six aggregate scripts, PublicAPI files |
| Tests | about 28 | about 16 | New: contract base, integration classes (EF has three), guard classes, unit tests, restart test; modified: DI tests, schemas |
| Documentation | 0 | 2 | `docs/features/handler-caching.md`, the registry document |
| Configuration | 10 | 12 | Ten changelog fragments at most; manifests edited per pull request |
| **Total** | **about 64** | **about 52** | 12 pull requests, each at most 15 files and 1500 lines (the count of each is in its phase table) |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt - All Phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library, pre-1.0. Issue #4411 adds a durable queue under the write-behind cache strategy of #272. The store contract lives in
Encina.Messaging; each provider package implements it; Encina.Caching extracts IWriteBehindQueue from its in-memory queue and adds a durable implementation.

IMPLEMENTATION OVERVIEW:
1. PR 1: WriteBehindItem, IWriteBehindStore, UseWriteBehindStore, the abstract contract tests.
2. PR 2: IWriteBehindQueue extraction; the flush service resolves the queue per scope and uses claim, complete, release.
3. PR 3: DurableWriteBehindQueue, the request registry, options, registration, EventIds 520-523, the backlog gauge.
4. PRs 4 to 11: one provider package each (ADO x3, Dapper x3, EF Core, MongoDB) with its atomic claim, script, DI flag, tests.
5. PR 12: restart integration test, health backlog age, documentation.

KEY PATTERNS:
- Fail closed: a store failure on enqueue returns Left (Unavailable, constant message); no silent fallback; no tenant means no enqueue (#272).
- Claims are atomic in the database; a batch never mixes tenants; a redelivered batch keeps its BatchId (at-least-once).
- No type is created from stored text outside the allow-list; no EncinaError.Message is stored or logged.
- Singletons never inject scoped services; time from TimeProvider; async database calls with tokens.
- Follow the maintainer's answers to Design Choices 1 to 5 given in your brief.

REFERENCE FILES:
- src/Encina.Messaging/Scheduling/IScheduledMessageStore.cs
- src/Encina.ADO.SqlServer/Scheduling/ScheduledMessageStoreADO.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- .github/coverage-manifest/Encina.Caching.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Include | Extends the write-behind strategy of #272; behavior-level, the 8 cache providers are untouched. |
| 2 | OpenTelemetry | ✅ Include | Gauge `encina.cache.write_behind.durable_backlog` (no tenant series) and the replay span with `encina.tenant_id` only when multi-tenancy is on (ADR-054 D4). |
| 3 | Structured Logging | ✅ Include | `CacheWriteBehindDurableLog`, EventIds 520-523 in `CachingWriteBehindDurable`. |
| 4 | Health Checks | ✅ Include | Backlog age in `WriteBehindHealthCheck` (PR 12). |
| 5 | Validation | ✅ Include | `LeaseDuration` positive, a registered store is required for `Queue = Durable`, checked at host start. |
| 6 | Resilience | ✅ Include | Bounded retry with `NextRetryAtUtc`, then the dead-letter sink of #272; no Polly dependency is added. |
| 7 | Distributed Locks | ❌ N/A | Design Choice 2 coordinates replay with a lease in the store, so no lock is needed (the issue's row assumed a lock; the lease replaces it). |
| 8 | Transactions | ✅ Include | The stores enlist in the scope transaction (`IDbTransactionAccessor`); no atomicity claim beyond that, the commit owner is the transaction behavior or `SagaOrchestrator.CommitAsync` (#720). |
| 9 | Idempotency | ✅ Include | At-least-once delivery with a stable `BatchId` per batch, also after redelivery. |
| 10 | Multi-Tenancy | ✅ Include | Tenant stored verbatim in its own column, reserved `default` for single-tenant mode, no tenant means no enqueue, batches never mix tenants. |
| 11 | Module Isolation | ⏭️ Defer | Follows the module dimension of #746 (open). |
| 12 | Audit Trail | ❌ N/A | Operational queue, not a compliance event; a dead-lettered batch is logged by code. |

**Deferred items:** module-scoped items (#746, open). Load test of concurrent claims and a benchmark of the enqueue path are justified skips in the first release: the claim is covered by the contract base against real databases; a concurrent-claim load test is a follow-up inside the existing load tests once a provider pull request lands.

---

## Prerequisites & Dependencies

| Prerequisite | Status | Notes |
|--------------|--------|-------|
| #272 PR 3 (queue, behavior, abstractions) and PR 4 (flush service, `AddEncinaWriteBehind`) | Open | PR 2 and PR 3 of this plan need them; PR 1 and the provider pull requests (4 to 11) need only PR 1 of this plan |
| `IScheduledMessageStore` pattern, `IDbTransactionAccessor`, shared fixtures | ✅ Available | On `origin/main` |
| Block 500-549 of the EventId registry | ✅ Assigned | Plan batch registry; 520-523 free |
| Maintainer answers to Design Choices 1 to 5 | Pending | Asked when work starts |

Coverage targets (per flag, set in the pull request that adds each file; every row's test file is in the same pull request):

| File | Unit | Guard | Contract | Integration | Justification |
|------|:----:|:-----:|:--------:|:-----------:|---------------|
| `Encina.Messaging/WriteBehind/WriteBehindItem.cs` | 90 | 0 | 0 | 0 | A data record with no validated parameter: no guard surface; equality and `with` are unit tested |
| `Encina.Messaging/WriteBehind/IWriteBehindStore.cs` | 0 | 0 | 0 | 0 | An interface has no executable lines |
| `Encina.Messaging/MessagingConfiguration.cs` | keep | keep | 0 | 0 | The existing targets are never lowered (`target-ratchet`); one property added and its default tested |
| `Encina.Caching/Patterns/DurableWriteBehindQueue.cs` | 95 | 0 | 0 | 40 | Driven by a fake store; internal, no `InternalsVisibleTo` for the guard project; integration 40 is set in PR 12, the pull request that adds the restart test |
| `Encina.Caching/Patterns/WriteBehindRequestRegistry.cs` | 95 | 0 | 0 | 0 | Pure mapping with unit tests; internal |
| `Encina.Caching/Patterns/WriteBehindQueue.cs`, `Services/WriteBehindFlushService.cs` | keep | 0 | keep | 0 | The targets #272 sets are kept; the changed lines are covered by the updated tests |
| `Encina.Caching/Diagnostics/CacheWriteBehindDurableLog.cs` | 90 | 0 | 0 | 0 | Generated bodies run in the queue tests |
| `Encina.Caching/Health/WriteBehindHealthCheck.cs` | keep | 0 | 0 | 0 | The target #272 sets (100) is kept; the new branch is tested in PR 12 |
| Each store, for example `Encina.ADO.SqlServer/WriteBehind/WriteBehindStoreADO.cs` | 0 | 25 | 0 | 85 | SQL runs only against a real database (the integration flag drives every statement); the guard tests reach the constructor and argument checks that precede the SQL (about a quarter of the file, in line with the 15 to 40 guard shares of other stores); unit 0 because a mocked connection executes none of the SQL |
| `Encina.EntityFrameworkCore/WriteBehind/WriteBehindStoreEF.cs`, `Encina.MongoDB/WriteBehind/WriteBehindStoreMongoDB.cs` | 0 | 25 | 0 | 85 | Same reasoning: real database tests, argument guards only |
| Provider `ServiceCollectionExtensions.cs` files | keep | keep | 0 | 0 | Existing targets kept; the flag branch is tested by the DI unit test of the same pull request |

The worker measures every flag with `coverage-report.cs` per pull request and reports the numbers; no target is lowered.

---

## Next Steps

1. The orchestrator asks the maintainer the Dilemmas (Design Choices 1 to 5) when work starts, records the answers under `## Maintainer Decisions`, then briefs PR 1; PR 2 and PR 3 wait for #272 PR 4, the eight provider pull requests can run in parallel after PR 1 (one lane each, ADO and Dapper of the same database serialize on the shared test schema file).
2. No issue file is written for this plan: the module dimension is #746 (open) and the load test belongs to the pull request that lands the first provider.
