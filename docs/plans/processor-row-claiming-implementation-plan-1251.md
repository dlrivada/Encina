# Implementation Plan: Row Claiming in the Outbox and Scheduler Processors for Multi-Instance Hosts

> **Issue**: [#1251](https://github.com/dlrivada/Encina/issues/1251) (SPEC-002 REQ-056, AC-039, tracking id P-47, priority P0; part of #1186)
> **Type**: Feature
> **Complexity**: High (12 phases, all 10 database providers, breaking change of `IOutboxStore` and `IScheduledMessageStore`)
> **Estimated Scope**: ~1,800-2,400 lines of production code + ~3,000-4,000 lines of tests

---

## Summary

Make the outbox processor and the scheduled-message processor safe to run on several hosts at once. Each processing cycle **claims** a batch of rows with a lease before it dispatches them. The claim uses the provider's non-blocking lock hint: `UPDLOCK, READPAST` on SQL Server, `FOR UPDATE SKIP LOCKED` on PostgreSQL and MySQL, and an atomic guarded update on MongoDB. A row claimed by one instance is skipped by the others until its lease is completed, released or expired. Completing a message (processed, failed, rescheduled) is fenced by the claim token, so an instance that lost its lease cannot overwrite the work of the instance that reclaimed the row.

### What the code does today (2026-10-06, `main` at `5b485b12`)

Nothing of #1251 is implemented. There is no `SKIP LOCKED`, `READPAST`, `UPDLOCK` or `FindOneAndUpdate` anywhere in `src/`, and no lease column on any outbox or scheduled-message table.

1. **Every instance reads the same rows.** The outbox fetch is a plain read on all 10 providers:
   - `IOutboxStore.GetPendingMessagesAsync(batchSize, maxRetries)` (`src/Encina.Messaging/Outbox/IOutboxStore.cs:40-43`).
   - SQL Server: `SELECT TOP (@BatchSize) * ... ORDER BY CreatedAtUtc` with no hint (`src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs:57-63`).
   - PostgreSQL: `... ORDER BY createdatutc LIMIT @BatchSize` (`src/Encina.Dapper.PostgreSQL/Outbox/OutboxStoreDapper.cs:67-74`).
   - EF Core: a LINQ `Where/OrderBy/Take` (`src/Encina.EntityFrameworkCore/Outbox/OutboxStoreEF.cs:72-79`).
   - MongoDB: `Find(filter).SortBy(...).Limit(batchSize)` (`src/Encina.MongoDB/Outbox/OutboxStoreMongoDB.cs:85-90`).
   - The scheduled-message stores do the same through `IScheduledMessageStore.GetDueMessagesAsync` (`src/Encina.Messaging/Scheduling/IScheduledMessageStore.cs:40-43`; for example `src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessageStoreEF.cs:61-69`).
2. **The processors dispatch whatever they read.**
   - `OutboxBatchProcessor.ProcessAsync` fetches and publishes each message (`src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs:79-137`).
   - `SchedulerOrchestrator.ProcessDueMessagesAsync` does the same for scheduled messages (`src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs:288-317`).
   - `ScheduledMessageProcessor` itself records the gap: "Without a lock, multi-replica deployments may process the same message concurrently. See Issue #716." (`src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs:40`).
   - Two hosts therefore publish the same outbox message and execute the same scheduled command, which is exactly what REQ-056 forbids.
3. **Completion is not fenced.** `MarkAsProcessedAsync` and `MarkAsFailedAsync` update by `Id` only (`OutboxStoreADO.cs:143-147`, `:174-179`). An instance cannot tell whether another instance already took the row over.
4. **Defects found while researching, which this plan touches:**
   - `SchedulerOrchestrator.ProcessDueMessagesAsync` never calls `IScheduledMessageStore.SaveChangesAsync`. `ScheduledMessageStoreEF` records outcomes only on the tracked context (`ScheduledMessageStoreEF.cs:76-114`) and persists them only in `SaveChangesAsync` (`:154-160`). On EF Core every due scheduled message is therefore executed again on every cycle. This plan writes an issue file for it (see Prerequisites); Phase 7 removes the cause for the processor path.
   - `SchedulerOrchestrator.GetPendingCountAsync` loads every due message with `int.MaxValue` to count them (`SchedulerOrchestrator.cs:360-368`). Once the fetch becomes a claim, counting through it would claim rows, so Phase 1 adds a read-only count.
   - `ScheduledMessageProcessor` always records `failureCount: 0` (`ScheduledMessageProcessor.cs:163`), because `ProcessDueMessagesAsync` returns only a success count. Phase 3 returns per-outcome counts.
   - The ADO.NET stores' `OpenConnectionAsync` never opens the connection (`OutboxStoreADO.cs:340-345`). This is #1170 and #1868, fixed for the messaging stores by Phase 5 of the #718 plan.

### Scope

- **Affected packages**:
  - `Encina.Messaging`: claim contract, processors, orchestrators, options, telemetry, DI.
  - `Encina.ADO.SqlServer`, `Encina.ADO.PostgreSQL`, `Encina.ADO.MySQL`.
  - `Encina.Dapper.SqlServer`, `Encina.Dapper.PostgreSQL`, `Encina.Dapper.MySQL`.
  - `Encina.EntityFrameworkCore` (SqlServer, PostgreSQL, MySQL).
  - `Encina.MongoDB`.
  - `Encina.OpenTelemetry` (`InstrumentedOutboxStore`, `InstrumentedScheduledMessageStore`), `Encina.Testing.Fakes` (`FakeOutboxStore`, `FakeScheduledMessageStore`), `Encina.Testing` (outbox and scheduling helpers), `Encina.Testing.Bogus` (fakers).
- **Provider category**: Database, all 10 providers (AGENTS.md §5). SQLite is out of the matrix (ADR-024).
- **Out of scope**:
  - The inbox: it has no polling processor; inbox messages are claimed by their unique message id on receipt.
  - The saga store: saga concurrency is #715.
  - CDC-driven outbox dispatch (`src/Encina.Cdc/Messaging/OutboxCdcHandler.cs`): it reacts to change events, not to a polling query.
- **Estimated files**: ~45 production files touched or created, ~55 test files, ~15 documentation files.

### Ordering with the parallel plans

The #718 plan (`docs/plans/outbox-atomicity-ado-implementation-plan-718.md`, maintainer decisions of 2026-10-06) and the #1200 plan (purge, written in parallel) change the same store classes. The plan recommends this order:

1. **#718 first.** It changes every method of the 20 outbox and scheduled-message stores: `command.Transaction` enlistment, real `OpenAsync`, a new optional `IDbTransactionAccessor` constructor parameter. It also removes `OutboxPostProcessor`. The claim statements of this plan reuse its enlistment helper. The MySQL claim transaction joins or begins through `IDbTransactionAccessor.BeginOrJoinAsync` when one is registered.
2. **#1251 next.** It rewrites the fetch and completion statements, adds lease columns to the DDL and changes the processor cycle.
3. **#1200 before or after #1251: either can land first.** #1200 adds a purge statement, an index on the processed timestamp, retention options and a purge step in the same processor loops. The purge touches only processed rows, and row claims touch only unprocessed rows. The claim is also cleared at completion, so a purge never meets an active claim. The analysis of this plan agrees with the #1200 plan: there is no semantic conflict, only textual ones, and the second PR rebases on three points:
   - the order of the DDL edits in the `000_CreateAllTables.sql`, `001_*.sql` and `004_*.sql` scripts;
   - EventIds, already agreed in the `Messaging` range (2800-2999): #718 reserves 2963-2969, #1200 takes 2970-2976, and this plan takes 2977-2988;
   - where the purge step sits in `OutboxProcessorBase` and `ScheduledMessageProcessor`.

If #1251 lands before #718, Phase 5 of this plan must also replace the no-op `OpenConnectionAsync` in the four messaging stores of each ADO.NET package, because a claim needs an open connection. #718 then rebases mechanically.

---

## Design Choices

<details>
<summary><strong>1. Claim contract — replace the fetch with a claim and fence every completion</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Replace `GetPendingMessagesAsync`/`GetDueMessagesAsync` with `ClaimPendingMessagesAsync`/`ClaimDueMessagesAsync(MessageClaimRequest)`; completion methods take the claim token** | One atomic statement per batch; no unclaimed read path that a processor could use by mistake; the fencing is visible in the signature; pre-1.0 allows the break (AGENTS.md §1) | Breaking change for every store, the two decorators in `Encina.OpenTelemetry`, the fakes and every test that calls the old methods |
| **B) Keep the fetch and add `TryClaimAsync(IReadOnlyCollection<Guid> ids)` as a second step** | Smaller diff; the fetch stays usable for diagnostics | Two round trips; a race window between read and claim; losers must be filtered and the batch refilled; easy to call the fetch alone and lose the guarantee |
| **C) A claiming decorator (`ClaimingOutboxStore`) over the existing stores** | No store change | A decorator cannot add a lock hint to a query it does not build; it would need an extra table or a distributed lock, which is what the issue rejects |

### Chosen Option: **A — claim methods replace the fetch; completion is fenced by the claim token** (recommended, pending the maintainer)

### Rationale

- We recommend A because the guarantee must be impossible to bypass. With B, every caller has to remember the second step, and the race window is inherent.
- The claim token in `MarkAsProcessedAsync(messageId, claimToken)` makes the fencing a compile-time requirement on all 10 providers. A provider cannot forget it.
- A read-only count stays available: `GetPendingCountAsync` already exists on the outbox (`IOutboxStore.cs:75-77`), and Phase 1 adds `GetDueCountAsync` to the scheduler store for `SchedulerOrchestrator.GetPendingCountAsync`.
- `RequeueExhaustedAsync` keeps its signature and also clears the claim columns.

</details>

<details>
<summary><strong>2. Lease model and a lost lease — fixed lease, fenced completion, stop before expiry, release on stop</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Fixed lease (`ClaimedUntilUtc = now + LeaseDuration`); fenced completion; the batch stops and releases its remaining claims when less than `LeaseSafetyMargin` is left; release on cancellation** | One statement per batch; a crashed instance's rows come back after `LeaseDuration`; a slow batch never runs past its lease; works the same on the 10 providers | A single dispatch longer than `LeaseDuration - LeaseSafetyMargin` can still be duplicated after reclaim; host clock skew shortens the effective lease |
| **B) Lease with a heartbeat that extends `ClaimedUntilUtc` while the batch runs** | Long dispatches keep their lease; shorter leases possible | A background renewal timer per batch; renewal failures need their own handling; more statements per batch; harder to test deterministically |
| **C) Row locks held in an open transaction for the whole batch (no lease columns)** | No schema change; a crash releases the locks immediately | One long transaction per instance across every publish; holds a connection and locks for the whole batch; conflicts with #718's transaction ownership; MongoDB cannot do it without a replica set and a session per batch |

### Chosen Option: **A — fixed lease with fenced completion, stop-before-expiry and release** (recommended, pending the maintainer)

### Rationale

- We recommend A because it gives AC-039 ("no message is processed twice" with two processors) on every provider with the fewest moving parts. The guard before each dispatch, `now + LeaseSafetyMargin < ClaimedUntilUtc`, keeps an instance from dispatching a message whose lease could expire mid-dispatch.
- The defaults are `LeaseDuration = 5 minutes` and `LeaseSafetyMargin = 30 seconds`. They are far above a normal batch (100 messages by default) and far above realistic clock skew.
- A lost lease is still possible after a stall such as a GC pause or a network partition. The fenced completion then returns `Left(outbox.claim_lost)` or `Left(scheduling.claim_lost)`. The outcome is logged and counted, never reported as success.
- Delivery stays at-least-once, as it is today. A crash after the publish and before the mark redelivers. Consumer-side deduplication is #1220 (outbox) and #735 (scheduled).
- Release on cancellation (the host stopping) and on the safety stop hands the remaining rows to another instance at once, instead of after `LeaseDuration`.
- Times come from `TimeProvider` (AGENTS.md §3). The database clock would remove skew, but it would break the `FakeTimeProvider` tests that every store already has (`OutboxStoreEFTimeProviderTests`, `ScheduledMessageStoreEFTimeProviderTests`).

</details>

<details>
<summary><strong>3. Lease columns — <code>ClaimedBy</code>, <code>ClaimToken</code> and <code>ClaimedUntilUtc</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ClaimedBy` (instance id), `ClaimToken` (one GUID per claimed batch), `ClaimedUntilUtc`** | Fencing per batch, so two overlapping cycles in one process (the background processor plus a manual `OutboxOrchestrator.ProcessPendingMessagesAsync`) cannot complete each other's rows; operators see which instance holds a row | Three columns on the outbox and scheduled-message tables of every provider (12 ADO.NET/Dapper scripts, the three EF Core models and two MongoDB collections) |
| **B) `ClaimedBy` (unique per process start) and `ClaimedUntilUtc`** | Two columns; matches the issue's wording literally | Two cycles in the same process share the identity, so fencing does not separate them; reclaiming its own expired row is indistinguishable from still holding it |
| **C) `ClaimToken` and `ClaimedUntilUtc` only** | Minimal; fencing is correct | No operational visibility of which instance holds a stuck lease; the issue asks for claimed-by |

### Chosen Option: **A — `ClaimedBy`, `ClaimToken`, `ClaimedUntilUtc`** (recommended, pending the maintainer)

### Rationale

- We recommend A because the fencing must be per batch to be correct inside one process, and the issue asks for a claimed-by column for operations.
- `ClaimedBy` comes from a singleton `MessageProcessorIdentity`. Its default is `{MachineName}:{ProcessId}:{8 random hex}`, and `OutboxOptions.ProcessorInstanceId` and `SchedulingOptions.ProcessorInstanceId` can override it (at most 200 characters).
- `ClaimedBy` is not an identifier of a data subject. It is never a metric tag (cardinality); it appears only in debug logs.
- Column names follow AGENTS.md §4 (`AtUtc` suffix).
- Types per database:

  | Column | SQL Server | PostgreSQL | MySQL | MongoDB |
  |--------|------------|------------|-------|---------|
  | `ClaimedBy` | `NVARCHAR(200) NULL` | `VARCHAR(200) NULL` | `VARCHAR(200) NULL` | `claimedBy` string |
  | `ClaimToken` | `UNIQUEIDENTIFIER NULL` | `UUID NULL` | `CHAR(36) NULL` (like `Id`) | `claimToken` string |
  | `ClaimedUntilUtc` | `DATETIME2(7) NULL` | `TIMESTAMP NULL` | `DATETIME(6) NULL` | `claimedUntilUtc` UTC date |

</details>

<details>
<summary><strong>4. Relational claim statements — one statement on SQL Server and PostgreSQL, a short transaction on MySQL</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Per dialect: SQL Server CTE `UPDATE ... OUTPUT` with `UPDLOCK, READPAST, ROWLOCK`; PostgreSQL `UPDATE ... FROM (SELECT ... FOR UPDATE SKIP LOCKED) ... RETURNING`; MySQL short transaction: `SELECT ... FOR UPDATE SKIP LOCKED`, `UPDATE ... WHERE Id IN (...)`, `SELECT ... WHERE ClaimToken = @ClaimToken`** | Non-blocking on all three: concurrent claimers skip locked rows instead of waiting; one round trip on SQL Server and PostgreSQL; the hints the issue names | Three SQL texts per store, each written twice (ADO.NET and Dapper), plus once in EF Core; MySQL needs three statements in a transaction |
| **B) Uniform two-step: `UPDATE ... SET ClaimToken = @t` over `TOP`/`LIMIT` with no lock hint, then `SELECT` by token** | One shape for every dialect | Concurrent claimers block on each other's rows instead of skipping them; on SQL Server the subquery predicate is not re-evaluated after the wait, so two claimers can overwrite each other; not what REQ-056 names |
| **C) Optimistic: `SELECT` candidates without locks, then `UPDATE ... WHERE Id IN (...) AND (ClaimedUntilUtc IS NULL OR ClaimedUntilUtc <= @Now)`, then re-read** | No lock hints; portable | Under contention most candidates are lost and the batch comes back short; needs retry loops; more round trips |

### Chosen Option: **A — native skip-locked claim per dialect** (recommended, pending the maintainer)

### Rationale

- We recommend A because it is the standard queue-table pattern on each database, it is what REQ-056 and the issue name, and it scales with the number of instances without blocking.
- MySQL rejects an `UPDATE` whose `WHERE` subquery reads the same table (error 1093). A locking read inside a derived table is not reliably supported either. The transaction form is therefore the safe default. The Research section records a check of a single-statement variant against the `mysql:9.1` image used by the fixtures (`tests/Encina.TestInfrastructure/Fixtures/`).
- `RETURNING` and `OUTPUT` do not guarantee row order, so the store sorts the claimed rows by `CreatedAtUtc` (outbox) or `ScheduledAtUtc` (scheduler) before returning them.
- The claim also returns the previous `ClaimedBy` of each row. A non-null value means an expired lease was reclaimed, which feeds the `reclaimed` counter.

</details>

<details>
<summary><strong>5. EF Core — per-dialect claim SQL, load by token, fenced <code>ExecuteUpdateAsync</code> for completion</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Claim with the dialect's raw SQL (`Database.ExecuteSqlRawAsync`, dialect chosen from the connection type as `BulkOperationsEF` does), load the batch `AsNoTracking()` by `ClaimToken`, complete with `Where(Id == id && ClaimToken == token).ExecuteUpdateAsync(...)`** | Same SQL and semantics as ADO.NET and Dapper; each outcome is persisted at once, like the other 9 providers; a lost lease shows up as 0 rows affected; removes the cause of the scheduler `SaveChangesAsync` defect for the processor path | Two round trips for the claim; completion no longer goes through the caller's `SaveChangesAsync` (it never did for the scheduler, see Summary item 4) |
| **B) `FromSqlRaw` with `OUTPUT`/`RETURNING`, tracked entities, `ClaimToken` as a concurrency token, completion through `SaveChangesAsync`** | One round trip on SQL Server and PostgreSQL; keeps EF's unit-of-work style | One lost row throws `DbUpdateConcurrencyException` and loses every outcome of the batch; MySQL still needs the transaction form; the scheduler defect remains |
| **C) LINQ only: `Where(...).OrderBy(...).Take(n).ExecuteUpdateAsync(set token)`, then load by token** | No raw SQL | EF emits no lock hint, so claimers block and, on SQL Server, can overwrite each other (same as option B of Decision 4) |

### Chosen Option: **A — raw claim SQL, no-tracking load, fenced `ExecuteUpdateAsync`** (recommended, pending the maintainer)

### Rationale

- We recommend A because it makes EF Core behave exactly like the other nine providers: atomic skip-locked claim, per-message persisted outcome, fencing by token. A lost lease costs one message, not the whole batch.
- The dialect is chosen once per store from `DbContext.Database.GetDbConnection()` (`SqlConnection`, `NpgsqlConnection`, `MySqlConnection`), following `src/Encina.EntityFrameworkCore/BulkOperations/BulkOperationsEF.cs:98-99`. Table and column names come from the EF model (`IEntityType.GetTableName()`, `GetColumnName()`), so custom mappings keep working.
- `AddAsync`, `RequeueExhaustedAsync` and `SaveChangesAsync` keep their unit-of-work behavior. The application's outbox write stays inside its `DbContext` transaction, which #718 relies on.
- `OutboxProcessorBase` still calls `SaveChangesAsync` after the batch (`OutboxProcessorBase.cs:150`). With immediate completion it becomes a no-op on EF, as it already is on ADO.NET, Dapper and MongoDB.

</details>

<details>
<summary><strong>6. MongoDB claim — candidate ids, guarded <code>UpdateMany</code>, read back by token</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Find` candidate ids (sorted, limited, projected), `UpdateMany` with filter `Id in ids AND eligible AND (claimedUntilUtc null or <= now)` setting the claim, then `Find` by `claimToken`** | Each document is claimed atomically by the guarded filter; three round trips per batch whatever the batch size; works on a standalone server | Under contention part of the candidates are taken by another instance, so a batch can come back shorter than `BatchSize` |
| **B) Loop `FindOneAndUpdate` (filter eligible, sort, set claim) up to `BatchSize` times** | Each call atomic; a full batch under contention | `BatchSize` round trips per cycle (100 by default); slow on a remote cluster |
| **C) Multi-document transaction with a session** | All-or-nothing batch claim | Needs a replica set; MongoDB has no skip-locked, so concurrent transactions abort with write conflicts and retry |

### Chosen Option: **A — candidates, guarded `UpdateMany`, read back by token** (recommended, pending the maintainer)

### Rationale

- We recommend A because it keeps the round trips constant and needs no replica set. A short batch under contention is harmless: the next cycle picks up the rest.
- The guard repeats the full eligibility filter, so a document that another instance claimed or completed between the `Find` and the `UpdateMany` is never taken.
- Completion uses `UpdateOneAsync` with the filter `{ _id: id, claimToken: token }`. `ModifiedCount == 0` means `claim_lost`. This replaces the current "not found" logging (`OutboxStoreMongoDB.cs:106-115`).
- `MongoDbIndexCreator` gains a compound index on (`processedAtUtc`, `claimedUntilUtc`, `createdAtUtc`) for the outbox, (`processedAtUtc`, `claimedUntilUtc`, `scheduledAtUtc`) for the scheduler, and a sparse index on `claimToken`.

</details>

<details>
<summary><strong>7. Activation — always on, no switch</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Always on: the claim is the only way a processor reads work** | One code path to test on 10 providers; correct by default for one host or several (SPEC-002 §11.1 says row claiming is in 1.0 either way); also protects one host from overlapping manual and background cycles | Every deployment pays one `UPDATE` per batch and three nullable columns |
| **B) Opt-in `UseRowClaiming = false` by default** | Pay-for-what-you-use (AGENTS.md §1) in its strictest reading | Two read paths per store (claimed and unclaimed) on 10 providers; a multi-host deployment that forgets the flag duplicates messages silently |
| **C) On by default with an opt-out** | Correct by default | Still two read paths; the opt-out only saves one `UPDATE` per batch |

### Chosen Option: **A — always on** (recommended, pending the maintainer)

### Rationale

- We recommend A because the outbox and the scheduler are already opt-in patterns (`UseOutbox`, `UseScheduling`). Claiming is part of how they read work, not a separate feature. Pay-for-what-you-use applies at the pattern level.
- A second read path would double the provider matrix for little saving. The extra statement is the `UPDATE` that sets three columns on rows the batch is about to update anyway.
- The issue's alternatives stay available on top of claiming: a distributed lock around the processor (#716) and leader election (#717) are still useful to reduce polling load, but they are no longer needed for correctness.

</details>

<details>
<summary><strong>8. Tenancy — the claim request carries the tenant filter; the tenant columns come from #737 and #739</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `MessageClaimRequest.TenantId` filters the claim when set; the `TenantId` columns land with #737 (outbox) and #739 (scheduler); per-tenant cycles come from #1257** | Each issue keeps its scope; the claim contract is tenant-aware from day one; no schema change is done twice | #737 and #739 become prerequisites of Phase 9; the two-tenant test of AC-043 waits for them |
| **B) This plan adds the `TenantId` columns and the per-tenant cycle (absorbs #737 and #739)** | One PR delivers REQ-056 and the messaging part of REQ-061 | Doubles the scope; #737 and #739 also cover the write path, request-context restore (#1164) and query filters, which are unrelated to claiming |
| **C) Tenant-agnostic claims now; tenancy later** | Smallest change | Breaks REQ-061 ("row claiming restores the tenant ... never mixes tenants in one unit of work") and AC-043; the claim contract would change again |

### Chosen Option: **A — tenant filter in the claim request, columns from #737/#739** (recommended, pending the maintainer)

### Rationale

- We recommend A because the claim contract must not change twice, while the tenant column, its write path and the restored request context belong to #737, #739 and #1164.
- With `TenantId = null` the claim covers every tenant. That is the behavior with tenancy off (the fixed default tenant of #1257) and the behavior before #737 and #739 land.
- When the columns exist, the stores add `AND TenantId = @TenantId` to the claim, and the processors run one claim per tenant when #1257 provides the tenant enumeration. One batch never mixes tenants (REQ-061).
- The two-tenant claim test of AC-043 is part of Phase 11, and it runs once #737 and #739 are merged.

</details>

---

## Implementation Phases

Phases 1-8 change a public interface that every store implements, so the solution builds again only at the end of Phase 8. The recommended PR cut is Phases 1-8 plus their tests (one PR, `Refs #1251`), then Phases 9-12 (`Fixes #1251`).

### Phase 1: Core claim contract and models

> **Goal**: Define the claim request and result, the fenced store methods and the error codes in `Encina.Messaging`.

<details>
<summary><strong>Tasks</strong></summary>

1. **New folder `src/Encina.Messaging/Claiming/`**:
   - `MessageClaimRequest` (sealed record, namespace `Encina.Messaging.Claiming`):
     - `int BatchSize` (> 0), `int MaxRetries` (>= 0), `string ClaimedBy` (1-200 characters), `Guid ClaimToken` (not empty), `TimeSpan LeaseDuration` (> 0), `string? TenantId`.
     - Validation in the primary constructor throws `ArgumentOutOfRangeException`/`ArgumentException`; the processors build it once per cycle.
   - `MessageClaim<TMessage>` (sealed record): `Guid ClaimToken`, `DateTime ClaimedUntilUtc`, `IReadOnlyList<TMessage> Messages`, `int ReclaimedCount`; static `Empty(Guid token, DateTime claimedUntilUtc)`.
   - `MessageProcessorIdentity` (sealed class): `string InstanceId`; constructor `(string? configuredInstanceId)`; default `{Environment.MachineName}:{Environment.ProcessId}:{8 random hex}`; truncation to 200 characters.
2. **`src/Encina.Messaging/Outbox/IOutboxStore.cs`**:
   - Remove `GetPendingMessagesAsync` (`:40-43`).
   - Add `Task<Either<EncinaError, MessageClaim<IOutboxMessage>>> ClaimPendingMessagesAsync(MessageClaimRequest request, CancellationToken cancellationToken = default)`.
   - Change `MarkAsProcessedAsync(Guid messageId, Guid claimToken, CancellationToken)` and `MarkAsFailedAsync(Guid messageId, Guid claimToken, string errorMessage, DateTime? nextRetryAtUtc, CancellationToken)`: they update only the row with that token, clear the three claim columns, and return `Left(OutboxErrorCodes.ClaimLost)` when no row matched.
   - Add `Task<Either<EncinaError, int>> ReleaseClaimsAsync(Guid claimToken, IReadOnlyCollection<Guid>? messageIds, CancellationToken)`: clears the claim of the given (or all) rows that still carry the token.
   - XML docs of `RequeueExhaustedAsync`: it also clears the claim columns.
3. **`src/Encina.Messaging/Scheduling/IScheduledMessageStore.cs`**:
   - Replace `GetDueMessagesAsync` (`:40-43`) with `ClaimDueMessagesAsync(MessageClaimRequest, CancellationToken)` returning `MessageClaim<IScheduledMessage>`.
   - Fence `MarkAsProcessedAsync`, `MarkAsFailedAsync` and `RescheduleRecurringMessageAsync` with `Guid claimToken`; all clear the claim.
   - Add `ReleaseClaimsAsync(Guid claimToken, IReadOnlyCollection<Guid>? messageIds, CancellationToken)`.
   - Add `Task<Either<EncinaError, int>> GetDueCountAsync(int maxRetries, CancellationToken)` (read-only).
   - `CancelAsync` stays unfenced (an application operation); its XML docs say that cancelling a claimed message makes the holder's completion return `claim_lost`.
4. **`IOutboxMessage` and `IScheduledMessage`**: read-only `string? ClaimedBy`, `Guid? ClaimToken`, `DateTime? ClaimedUntilUtc`.
5. **Error codes**:
   - `OutboxErrorCodes.ClaimLost = "outbox.claim_lost"`, `OutboxErrorCodes.ClaimFailed = "outbox.claim_failed"` (`src/Encina.Messaging/Outbox/OutboxOrchestrator.cs:269-295`).
   - `SchedulingErrorCodes.ClaimLost = "scheduling.claim_lost"`, `SchedulingErrorCodes.ClaimFailed = "scheduling.claim_failed"` (`SchedulerOrchestrator.cs:521-572`).
6. **`PublicAPI.Unshipped.txt`** of `Encina.Messaging`: new types and members; remove the lines of the removed methods (RS0017).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #1251 (row claiming in the outbox and scheduler processors) in Encina.

CONTEXT:
- Encina is a .NET 10 / C# 14 library, pre-1.0: breaking changes are expected and done completely, no [Obsolete].
- Railway Oriented Programming: store methods return Either<EncinaError, T>; infrastructure failures are Left.
- Today IOutboxStore.GetPendingMessagesAsync (src/Encina.Messaging/Outbox/IOutboxStore.cs:40-43) and
  IScheduledMessageStore.GetDueMessagesAsync (src/Encina.Messaging/Scheduling/IScheduledMessageStore.cs:40-43)
  are plain reads, so two hosts process the same rows. Completion updates by Id only.
- Plan: docs/plans/processor-row-claiming-implementation-plan-1251.md (Design Choices 1-3).

TASK:
Create src/Encina.Messaging/Claiming/ with MessageClaimRequest, MessageClaim<TMessage> and MessageProcessorIdentity.
Replace the fetch methods with ClaimPendingMessagesAsync / ClaimDueMessagesAsync, fence MarkAsProcessedAsync,
MarkAsFailedAsync and RescheduleRecurringMessageAsync with a Guid claimToken, add ReleaseClaimsAsync to both stores
and GetDueCountAsync to the scheduler store, add the claim properties to IOutboxMessage and IScheduledMessage, and
add the claim_lost and claim_failed error codes.

KEY RULES:
- MessageClaimRequest validates its arguments; ClaimedBy is at most 200 characters; ClaimToken is never Guid.Empty.
- A fenced completion that matches no row returns Left(<pattern>.claim_lost); it is not an exception.
- XML docs on every public member, with the fencing and lease semantics explained.
- Update PublicAPI.Unshipped.txt (RS0016/RS0017). The solution builds again only after Phase 8.

REFERENCE FILES:
- src/Encina.Messaging/Outbox/IOutboxStore.cs, IOutboxMessage.cs, OutboxOrchestrator.cs (OutboxErrorCodes)
- src/Encina.Messaging/Scheduling/IScheduledMessageStore.cs, IScheduledMessage.cs, SchedulerOrchestrator.cs
- docs/plans/processor-row-claiming-implementation-plan-1251.md
```

</details>

---

### Phase 2: Configuration, DI and options validation

> **Goal**: Lease options, the processor identity singleton and start-up validation for both patterns.

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Messaging/Outbox/OutboxOptions.cs`**:
   - `TimeSpan LeaseDuration` (default 5 minutes, setter rejects <= 0).
   - `TimeSpan LeaseSafetyMargin` (default 30 seconds, setter rejects < 0).
   - `string? ProcessorInstanceId` (null means the default identity; at most 200 characters).
   - `Validate` (`:139-147`) also rejects `LeaseSafetyMargin >= LeaseDuration`.
2. **`src/Encina.Messaging/Scheduling/SchedulingOptions.cs`**:
   - The same three properties.
   - A new `internal void Validate(string paramName)` (today there is none, `SchedulingOptions.cs:10-52`), called from the `ScheduledMessageProcessor` and `SchedulerOrchestrator` constructors.
3. **`src/Encina.Messaging/MessagingServiceCollectionExtensions.cs`**:
   - `RegisterOutbox` (`:253-267`) and `RegisterScheduling` (`:313-330`) register `MessageProcessorIdentity` with `TryAddSingleton`.
   - When both patterns are on and only one sets `ProcessorInstanceId`, that value wins; when both set different values, start-up fails with `ArgumentException` (one host has one identity).
4. **DI completeness**: `OutboxProcessorBase`, `SchedulerOrchestrator` and `ScheduledMessageProcessor` resolve `MessageProcessorIdentity`; tests build the provider with `ValidateOnBuild` and `ValidateScopes` on all 10 providers (AGENTS.md §3, registration completeness).
5. **`PublicAPI.Unshipped.txt`**: the new options members.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #1251 in Encina: lease options, the processor identity and DI.

CONTEXT:
- Phase 1 added src/Encina.Messaging/Claiming/ (MessageClaimRequest, MessageClaim<T>, MessageProcessorIdentity).
- OutboxOptions has an internal Validate (src/Encina.Messaging/Outbox/OutboxOptions.cs:139-147) called by
  OutboxProcessorBase; SchedulingOptions has no Validate.
- The shared registration of all 10 providers is AddOutboxInboxSagaSchedulingServices
  (src/Encina.Messaging/MessagingServiceCollectionExtensions.cs:203), with RegisterOutbox (:253) and
  RegisterScheduling (:313).

TASK:
Add LeaseDuration, LeaseSafetyMargin and ProcessorInstanceId to OutboxOptions and SchedulingOptions, add
SchedulingOptions.Validate, register MessageProcessorIdentity as a singleton from both registrations, and add DI
tests that build the provider with ValidateOnBuild and ValidateScopes.

KEY RULES:
- Setters reject invalid single values; Validate checks cross-property rules (LeaseSafetyMargin < LeaseDuration).
- TryAdd registrations; one identity per host; conflicting configured instance ids fail at start-up.
- No DateTime.UtcNow; durations only. XML docs explain how to size LeaseDuration (longest batch plus clock skew).

REFERENCE FILES:
- src/Encina.Messaging/Outbox/OutboxOptions.cs, src/Encina.Messaging/Scheduling/SchedulingOptions.cs
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs
- tests/Encina.UnitTests/Messaging/ (existing DI tests)
```

</details>

---

### Phase 3: Processors and orchestrators

> **Goal**: Both processors claim, guard the lease, complete with the token, release on stop and report per-outcome counts.

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs`**:
   - Constructor gains `MessageProcessorIdentity identity` and `Func<Guid>? claimTokenSource = null` (deterministic tests).
   - `ProcessAsync` builds a `MessageClaimRequest` and calls `ClaimPendingMessagesAsync` instead of `GetPendingMessagesAsync` (`:79-82`). A `Left` is returned as today.
   - Before each message: if `now + LeaseSafetyMargin >= claim.ClaimedUntilUtc`, stop, release the rest with `ReleaseClaimsAsync(token, remainingIds)` and log 2010.
   - On cancellation (`:107-117`): release the undelivered rest.
   - Completion passes `claim.ClaimToken` (`:197`, `:230`, `:257`). `Left(outbox.claim_lost)` becomes a new `MessageOutcome.ClaimLost`, logged with 2009, never counted as delivered.
   - `OutboxBatchResult` gains `ClaimLost`, `Released` and `Reclaimed`; `Total` and `NotDelivered` include `ClaimLost`.
2. **`src/Encina.Messaging/Outbox/OutboxProcessorBase.cs`** (`:118-161`): resolves `MessageProcessorIdentity` from the root provider once; passes it to `OutboxBatchProcessor`; records the new counts.
3. **`src/Encina.Messaging/Outbox/OutboxOrchestrator.cs`**: `ProcessPendingMessagesAsync` (`:133-150`) takes the identity from its constructor.
4. **`src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs`**:
   - `ProcessDueMessagesAsync` (`:288-317`) claims, applies the same guard and release, and passes the token to `MarkAsProcessedAsync`, `MarkAsFailedAsync` and `RescheduleRecurringMessageAsync` (`:420`, `:431`, `:442`, `:451`, `:463`).
   - It returns `Either<EncinaError, ScheduledBatchResult>` (new `readonly record struct`: `Succeeded`, `Failed`, `ClaimLost`, `StoreErrors`, `Released`, `Reclaimed`) instead of an `int`.
   - It calls `_store.SaveChangesAsync(CancellationToken.None)` after the batch, like `OutboxProcessorBase.cs:150`. This is a no-op with immediate completion, kept for custom stores.
   - `GetPendingCountAsync` (`:360-368`) calls `GetDueCountAsync` instead of loading every due message.
5. **`src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs`**:
   - Records the real outcome counts (today `failureCount: 0`, `:163`).
   - Removes the #716 note (`:40`) and replaces it with the claiming description.
6. **Decorators and test doubles** (same change of signatures):
   - `src/Encina.OpenTelemetry/MessagingStores/InstrumentedOutboxStore.cs` and `InstrumentedScheduledMessageStore.cs`.
   - `src/Encina.Testing.Fakes/Stores/FakeOutboxStore.cs` and `FakeScheduledMessageStore.cs`: claim with a `lock` over the in-memory list, honor leases, `TimeProvider`-based expiry.
   - `src/Encina.Testing/Messaging/OutboxTestHelper.cs`, `SchedulingTestHelper.cs`; `src/Encina.Testing.Bogus/OutboxMessageFaker.cs`, `ScheduledMessageFaker.cs`; `src/Encina.Testing.Fakes/Models/FakeOutboxMessage.cs`, `FakeScheduledMessage.cs`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #1251 in Encina: the processors claim, guard the lease and fence completion.

CONTEXT:
- Phases 1-2 added the claim contract, lease options and MessageProcessorIdentity.
- OutboxBatchProcessor (src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs) is the single outbox cycle used by
  OutboxProcessorBase and OutboxOrchestrator. SchedulerOrchestrator.ProcessDueMessagesAsync
  (src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs:288) is the scheduler cycle; it never calls
  SaveChangesAsync, and ScheduledMessageProcessor records failureCount: 0 (ScheduledMessageProcessor.cs:163).

TASK:
Switch both cycles to ClaimPendingMessagesAsync / ClaimDueMessagesAsync; before each dispatch stop when
now + LeaseSafetyMargin >= ClaimedUntilUtc and release the remaining claims; release on cancellation; pass the
claim token to every completion; treat Left(claim_lost) as its own outcome; return per-outcome counts
(OutboxBatchResult, new ScheduledBatchResult); make GetPendingCountAsync use GetDueCountAsync; update the
OpenTelemetry decorators, the fakes, the testing helpers and the Bogus fakers.

KEY RULES:
- A lost claim is never reported as success; it is logged by code only (never EncinaError.Message, AGENTS.md §3).
- Release and save run with CancellationToken.None when the host is stopping (they record work already done).
- Time from TimeProvider; claim token from an injectable Func<Guid> for deterministic tests.
- Methods you add or change keep CRAP <= 10: extract the guard, the release and the outcome mapping into helpers.

REFERENCE FILES:
- src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs, OutboxProcessorBase.cs, OutboxOrchestrator.cs
- src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs, ScheduledMessageProcessor.cs
- src/Encina.OpenTelemetry/MessagingStores/InstrumentedOutboxStore.cs, InstrumentedScheduledMessageStore.cs
- src/Encina.Testing.Fakes/Stores/FakeOutboxStore.cs, FakeScheduledMessageStore.cs
```

</details>

---

### Phase 4: Schema — lease columns and claim indexes

> **Goal**: The three claim columns and a claim-friendly index on every outbox and scheduled-message table and collection.

<details>
<summary><strong>Tasks</strong></summary>

1. **SQL scripts** for each of `src/Encina.{ADO,Dapper}.{SqlServer,PostgreSQL,MySQL}/Scripts/`: `000_CreateAllTables.sql`, `001_CreateOutboxMessagesTable.sql`, `004_CreateScheduledMessagesTable.sql`:
   - Add `ClaimedBy`, `ClaimToken`, `ClaimedUntilUtc` with the types of Design Choice 3.
   - Extend the processing index with `ClaimedUntilUtc`. For example, SQL Server `IX_OutboxMessages_ProcessedAt_RetryCount` (`src/Encina.ADO.SqlServer/Scripts/001_CreateOutboxMessagesTable.sql:16-18`) becomes `([ProcessedAtUtc], [RetryCount], [NextRetryAtUtc], [ClaimedUntilUtc]) INCLUDE ([CreatedAtUtc])`.
   - Add an index on `ClaimToken` (used by the MySQL and EF Core read-back and by `ReleaseClaimsAsync`).
   - No migration scripts (pre-1.0, AGENTS.md §1).
2. **Entity classes**: `Outbox/OutboxMessage.cs` and `Scheduling/ScheduledMessage.cs` in the 6 ADO.NET/Dapper packages, `src/Encina.EntityFrameworkCore/Outbox/OutboxMessage.cs`, `Scheduling/ScheduledMessage.cs`, `src/Encina.MongoDB/Outbox/OutboxMessage.cs`, `Scheduling/ScheduledMessage.cs` (BSON names `claimedBy`, `claimToken`, `claimedUntilUtc`, `ClaimedUntilUtc` with `DateTimeKind.Utc`).
3. **EF Core configurations**: `OutboxMessageConfiguration.cs` and `ScheduledMessageConfiguration.cs`: `HasMaxLength(200)` for `ClaimedBy`; extend `IX_OutboxMessages_Processing` with `ClaimedUntilUtc`; add `IX_OutboxMessages_ClaimToken` and the scheduler equivalents.
4. **MongoDB**: `src/Encina.MongoDB/MongoDbIndexCreator.cs` creates the indexes of Design Choice 6.
5. **Test schemas**: `tests/Encina.TestInfrastructure/Schemas/SqlServerSchema.cs`, `PostgreSqlSchema.cs`, `MySqlSchema.cs`, `TenancySchema.cs`, `ModuleIsolationSchema.cs`; benchmark schema builders `tests/Encina.BenchmarkTests/Encina.ADO.Benchmarks/Infrastructure/AdoSchemaBuilder.cs`, `Encina.Dapper.Benchmarks/Infrastructure/DapperSchemaBuilder.cs`.
6. **`PublicAPI.Unshipped.txt`** of each provider package: the new entity properties.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #1251 in Encina: lease columns and claim indexes on all 10 providers.

CONTEXT:
- Phase 1 added ClaimedBy, ClaimToken and ClaimedUntilUtc to IOutboxMessage and IScheduledMessage.
- DDL lives in src/Encina.{ADO,Dapper}.{SqlServer,PostgreSQL,MySQL}/Scripts/000_CreateAllTables.sql,
  001_CreateOutboxMessagesTable.sql and 004_CreateScheduledMessagesTable.sql; EF Core in
  OutboxMessageConfiguration.cs / ScheduledMessageConfiguration.cs; MongoDB indexes in MongoDbIndexCreator.cs.
- Test schemas: tests/Encina.TestInfrastructure/Schemas/*.cs.

TASK:
Add the three columns (SQL Server NVARCHAR(200)/UNIQUEIDENTIFIER/DATETIME2(7); PostgreSQL VARCHAR(200)/UUID/
TIMESTAMP; MySQL VARCHAR(200)/CHAR(36)/DATETIME(6); MongoDB claimedBy/claimToken/claimedUntilUtc) to every outbox
and scheduled-message table, entity, EF configuration and test schema; extend the processing index with
ClaimedUntilUtc and add a ClaimToken index.

KEY RULES:
- All 10 providers and every schema copy in the same change; identifier casing per dialect (PostgreSQL lower-case,
  MySQL back-ticks, SQL Server brackets).
- No migration scripts and no compatibility columns (pre-1.0).
- Coordinate the DDL edits with #1200 (purge index on the processed timestamp) if it is in flight.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/Scripts/001_CreateOutboxMessagesTable.sql
- src/Encina.ADO.PostgreSQL/Scripts/004_CreateScheduledMessagesTable.sql
- src/Encina.ADO.MySQL/Scripts/001_CreateOutboxMessagesTable.sql
- src/Encina.EntityFrameworkCore/Outbox/OutboxMessageConfiguration.cs
- src/Encina.MongoDB/MongoDbIndexCreator.cs
```

</details>

---

### Phase 5: ADO.NET providers (SqlServer, PostgreSQL, MySQL)

> **Goal**: `OutboxStoreADO` and `ScheduledMessageStoreADO` claim with the native skip-locked statement and fence completion.

<details>
<summary><strong>Tasks</strong></summary>

For each of `src/Encina.ADO.{SqlServer,PostgreSQL,MySQL}/`:

1. **`Outbox/OutboxStoreADO.cs`**: replace `GetPendingMessagesAsync` (SqlServer `:45-101`, PostgreSQL `:38`, MySQL `:45`) with `ClaimPendingMessagesAsync`:
   - SQL Server:

     ```sql
     WITH candidates AS (
         SELECT TOP (@BatchSize) *
         FROM {table} WITH (UPDLOCK, READPAST, ROWLOCK)
         WHERE ProcessedAtUtc IS NULL AND RetryCount < @MaxRetries
           AND (NextRetryAtUtc IS NULL OR NextRetryAtUtc <= @NowUtc)
           AND (ClaimedUntilUtc IS NULL OR ClaimedUntilUtc <= @NowUtc)
         ORDER BY CreatedAtUtc)
     UPDATE candidates
     SET ClaimedBy = @ClaimedBy, ClaimToken = @ClaimToken, ClaimedUntilUtc = @ClaimedUntilUtc
     OUTPUT inserted.*, deleted.ClaimedBy AS PreviousClaimedBy;
     ```

   - PostgreSQL:

     ```sql
     UPDATE outboxmessages AS o
     SET claimedby = @ClaimedBy, claimtoken = @ClaimToken, claimeduntilutc = @ClaimedUntilUtc
     FROM (SELECT id, claimedby AS previousclaimedby FROM outboxmessages
           WHERE processedatutc IS NULL AND retrycount < @MaxRetries
             AND (nextretryatutc IS NULL OR nextretryatutc <= @NowUtc)
             AND (claimeduntilutc IS NULL OR claimeduntilutc <= @NowUtc)
           ORDER BY createdatutc LIMIT @BatchSize
           FOR UPDATE SKIP LOCKED) AS c
     WHERE o.id = c.id
     RETURNING o.*, c.previousclaimedby;
     ```

   - MySQL, in one transaction: `SELECT Id, ClaimedBy ... ORDER BY CreatedAtUtc LIMIT @BatchSize FOR UPDATE SKIP LOCKED`; `UPDATE ... SET ... WHERE Id IN (@Id0, ...)`; `SELECT * ... WHERE ClaimToken = @ClaimToken`; commit. With #718 merged, the transaction comes from `IDbTransactionAccessor.BeginOrJoinAsync`; otherwise from `DbConnection.BeginTransactionAsync`.
   - Rows are sorted by `CreatedAtUtc` in memory; `ReclaimedCount` counts non-null previous `ClaimedBy`.
2. **Fenced completion**: `MarkAsProcessedAsync` (`:135-159`) and `MarkAsFailedAsync` (`:162-192`) add `AND ClaimToken = @ClaimToken`, set the three claim columns to `NULL`, and return `Left(outbox.claim_lost)` when `ExecuteNonQueryAsync` returns 0.
3. **`ReleaseClaimsAsync`**: `UPDATE ... SET ClaimedBy = NULL, ClaimToken = NULL, ClaimedUntilUtc = NULL WHERE ClaimToken = @ClaimToken [AND Id IN (...)]`, chunked like `RequeueIdBatchSize` (`:21`).
4. **`RequeueExhaustedAsync`** (`:247-302`) also clears the claim columns.
5. **`Scheduling/ScheduledMessageStoreADO.cs`**: the same for `ClaimDueMessagesAsync` (eligibility adds `ScheduledAtUtc <= @NowUtc`, order by `ScheduledAtUtc`), `RescheduleRecurringMessageAsync`, `ReleaseClaimsAsync` and `GetDueCountAsync`.
6. **Connection opening**: if #718 has not landed, replace the no-op `OpenConnectionAsync` (`OutboxStoreADO.cs:340-345`) of the two stores with `DbConnection.OpenAsync(cancellationToken)` (#1170, #1868).
7. One internal SQL builder per package (`Messaging/ClaimSql.cs`) shared by the two stores, instead of two copies.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #1251 in Encina: native row claiming in the three ADO.NET packages.

CONTEXT:
- Phases 1-4 changed IOutboxStore/IScheduledMessageStore to claim + fenced completion and added the ClaimedBy,
  ClaimToken and ClaimedUntilUtc columns.
- OutboxStoreADO today reads with SELECT TOP/LIMIT and no lock hint
  (src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs:57-63) and updates by Id only (:143-147, :174-179).
- If issue #718 is merged, every command carries the accessor's transaction (command.Transaction) and connections
  are opened with OpenAsync; reuse its enlistment helper. Otherwise also fix the no-op OpenConnectionAsync (:340).

TASK:
For SqlServer, PostgreSQL and MySQL implement ClaimPendingMessagesAsync / ClaimDueMessagesAsync with the native
skip-locked statement (SQL Server CTE UPDATE ... OUTPUT with UPDLOCK, READPAST, ROWLOCK; PostgreSQL UPDATE ... FROM
(SELECT ... FOR UPDATE SKIP LOCKED) RETURNING; MySQL a short transaction SELECT ... FOR UPDATE SKIP LOCKED, UPDATE by
ids, SELECT by token), fence MarkAsProcessed/MarkAsFailed/RescheduleRecurring with ClaimToken, add ReleaseClaimsAsync
and GetDueCountAsync, and clear the claim in RequeueExhaustedAsync.

KEY RULES:
- Async only, with CancellationToken (AGENTS.md §3); parameters always, never string-concatenated values.
- All three providers in the same change; one internal SQL builder per package for both stores.
- 0 rows affected on a fenced completion => Left(<pattern>.claim_lost); never throw for it.
- Sort claimed rows in memory (RETURNING/OUTPUT order is not guaranteed); count reclaimed rows.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs, Scheduling/ScheduledMessageStoreADO.cs
- src/Encina.ADO.PostgreSQL/Outbox/OutboxStoreADO.cs, src/Encina.ADO.MySQL/Outbox/OutboxStoreADO.cs
- docs/plans/outbox-atomicity-ado-implementation-plan-718.md (Phase 5, enlistment)
```

</details>

---

### Phase 6: Dapper providers (SqlServer, PostgreSQL, MySQL)

> **Goal**: The same claim and fencing in `OutboxStoreDapper` and `ScheduledMessageStoreDapper`.

<details>
<summary><strong>Tasks</strong></summary>

For each of `src/Encina.Dapper.{SqlServer,PostgreSQL,MySQL}/`:

1. **`Outbox/OutboxStoreDapper.cs`**: replace `GetPendingMessagesAsync` (SqlServer `:61`, PostgreSQL `:56-84`, MySQL `:61`) with `ClaimPendingMessagesAsync` using the SQL of Phase 5 through `QueryAsync<OutboxMessageClaimRow>` (entity columns plus `PreviousClaimedBy`), with `CommandDefinition(..., transaction: ..., cancellationToken: ...)`.
2. **Fenced completion, `ReleaseClaimsAsync`, `RequeueExhaustedAsync`** as in Phase 5 tasks 2-4, through `ExecuteAsync` returning the affected rows.
3. **`Scheduling/ScheduledMessageStoreDapper.cs`** (`:59` in each package): `ClaimDueMessagesAsync`, fenced `MarkAsProcessedAsync`, `MarkAsFailedAsync`, `RescheduleRecurringMessageAsync`, `ReleaseClaimsAsync`, `GetDueCountAsync`.
4. **MySQL**: the transaction form of Design Choice 4, passing the transaction to the three Dapper calls (the #719 rule that every Dapper call passes the transaction explicitly, adopted by the #718 plan).
5. One internal SQL builder per package, as in Phase 5.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of issue #1251 in Encina: native row claiming in the three Dapper packages.

CONTEXT:
- Phase 5 implemented the claim statements in the ADO.NET packages; reuse the same SQL per dialect.
- OutboxStoreDapper reads with LIMIT/TOP and no lock hint
  (src/Encina.Dapper.PostgreSQL/Outbox/OutboxStoreDapper.cs:67-74).

TASK:
For SqlServer, PostgreSQL and MySQL implement ClaimPendingMessagesAsync / ClaimDueMessagesAsync, fenced completion,
ReleaseClaimsAsync, GetDueCountAsync and claim clearing in RequeueExhaustedAsync with Dapper's
QueryAsync/ExecuteAsync and CommandDefinition.

KEY RULES:
- Every Dapper call passes cancellationToken and, when a transaction is active, transaction: explicitly.
- Map the extra PreviousClaimedBy column through a private row type; never expose it publicly.
- Same error codes and the same in-memory ordering as Phase 5.

REFERENCE FILES:
- src/Encina.Dapper.SqlServer/Outbox/OutboxStoreDapper.cs, Scheduling/ScheduledMessageStoreDapper.cs
- src/Encina.Dapper.PostgreSQL/Outbox/OutboxStoreDapper.cs, src/Encina.Dapper.MySQL/Outbox/OutboxStoreDapper.cs
- src/Encina.ADO.SqlServer/Messaging/ClaimSql.cs (Phase 5)
```

</details>

---

### Phase 7: EF Core providers (SqlServer, PostgreSQL, MySQL)

> **Goal**: `OutboxStoreEF` and `ScheduledMessageStoreEF` claim with raw dialect SQL and complete with fenced `ExecuteUpdateAsync`.

<details>
<summary><strong>Tasks</strong></summary>

1. **New `src/Encina.EntityFrameworkCore/Messaging/EfClaimDialect.cs`** (internal): detects the dialect from `DbContext.Database.GetDbConnection()` (`SqlConnection`, `NpgsqlConnection`, `MySqlConnection`, as `BulkOperations/BulkOperationsEF.cs:98-99`); builds the claim SQL with the table and column names read from the EF model (`IEntityType.GetTableName()`, `GetSchema()`, `IProperty.GetColumnName()`). Any other provider returns `Left(outbox.claim_failed)` with a clear code.
2. **`Outbox/OutboxStoreEF.cs`**:
   - `ClaimPendingMessagesAsync` replaces `GetPendingMessagesAsync` (`:63-83`). SQL Server and PostgreSQL: `ExecuteSqlRawAsync` with the claim `UPDATE` (no `OUTPUT`/`RETURNING`). MySQL: the transaction form, joining `Database.CurrentTransaction` or beginning one.
   - Then `Set<OutboxMessage>().AsNoTracking().Where(m => m.ClaimToken == token).OrderBy(m => m.CreatedAtUtc).ToListAsync()`.
   - `ReclaimedCount` comes from a scalar count of the eligible rows with a non-null, expired `ClaimedUntilUtc`, taken inside the claim statement or transaction.
   - `MarkAsProcessedAsync` and `MarkAsFailedAsync` (`:86-122`): `Where(m => m.Id == id && m.ClaimToken == token).ExecuteUpdateAsync(...)`; 0 rows means `Left(outbox.claim_lost)`.
   - `ReleaseClaimsAsync` with `ExecuteUpdateAsync`. `RequeueExhaustedAsync` (`:155-195`) also clears the claim on the tracked entities.
3. **`Scheduling/ScheduledMessageStoreEF.cs`**:
   - The same for `ClaimDueMessagesAsync`, the fenced `MarkAsProcessedAsync`, `MarkAsFailedAsync` and `RescheduleRecurringMessageAsync` (`:76-136`), `ReleaseClaimsAsync` and `GetDueCountAsync`.
   - Outcomes are then persisted immediately, which removes the cause of the scheduler `SaveChangesAsync` defect (Summary item 4) for the processor path.
4. **`Outbox/OutboxProcessor.cs`** (`:42-43`): unchanged except the store constructor; confirm that `SaveChangesAsync` after the batch is harmless.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of issue #1251 in Encina: row claiming in the EF Core outbox and scheduler stores.

CONTEXT:
- One package (src/Encina.EntityFrameworkCore) serves SQL Server, PostgreSQL and MySQL.
- OutboxStoreEF reads with LINQ Take and no lock hint (Outbox/OutboxStoreEF.cs:72-79) and records outcomes on
  tracked entities (:86-122) persisted only by SaveChangesAsync. ScheduledMessageStoreEF works the same, and
  SchedulerOrchestrator never calls SaveChangesAsync, so scheduled outcomes are lost on EF Core today.
- BulkOperationsEF picks the dialect from the connection type (BulkOperations/BulkOperationsEF.cs:98-99).

TASK:
Add an internal EfClaimDialect that builds the claim SQL from the EF model per dialect; implement
ClaimPendingMessagesAsync / ClaimDueMessagesAsync (raw claim UPDATE, MySQL in a transaction, then an AsNoTracking
load by ClaimToken); implement fenced completion, ReleaseClaimsAsync and GetDueCountAsync with ExecuteUpdateAsync;
clear the claim in RequeueExhaustedAsync.

KEY RULES:
- Table, schema and column names come from the EF model, never hard-coded; values are always parameters.
- Completion is immediate (ExecuteUpdateAsync), so a lost claim costs one message, not the batch.
- AddAsync, RequeueExhaustedAsync and SaveChangesAsync keep their unit-of-work behavior (#718 depends on AddAsync).
- Integration tests on EFCore-SqlServer, EFCore-PostgreSQL and EFCore-MySQL collections (Phase 11).

REFERENCE FILES:
- src/Encina.EntityFrameworkCore/Outbox/OutboxStoreEF.cs, OutboxMessageConfiguration.cs, OutboxProcessor.cs
- src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessageStoreEF.cs, ScheduledMessageConfiguration.cs
- src/Encina.EntityFrameworkCore/BulkOperations/BulkOperationsEF.cs
```

</details>

---

### Phase 8: MongoDB provider

> **Goal**: Atomic per-document claim with a guarded `UpdateMany` and fenced completion.

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.MongoDB/Outbox/OutboxStoreMongoDB.cs`**:
   - `ClaimPendingMessagesAsync` replaces `GetPendingMessagesAsync` (`:67-95`):
     1. `Find(eligible).SortBy(CreatedAtUtc).Limit(batchSize).Project(Id, ClaimedBy)`.
     2. `UpdateManyAsync(And(In(Id, ids), eligible, Or(Eq(ClaimedUntilUtc, null), Lte(ClaimedUntilUtc, now))), Set(claim))`.
     3. `Find(Eq(ClaimToken, token)).SortBy(CreatedAtUtc)`.
   - `ReclaimedCount` counts the claimed documents whose candidate projection had a non-null `ClaimedBy`.
   - Fenced `MarkAsProcessedAsync` and `MarkAsFailedAsync` (`:98-145`) filter on `Id` and `ClaimToken` and unset the claim; `ModifiedCount == 0` means `Left(outbox.claim_lost)`.
   - `ReleaseClaimsAsync` with `UpdateManyAsync`. `RequeueExhaustedAsync` (`:180-208`) also unsets the claim.
2. **`src/Encina.MongoDB/Scheduling/ScheduledMessageStoreMongoDB.cs`** (`:71`): the same for the scheduler, plus `GetDueCountAsync` with `CountDocumentsAsync`.
3. **`src/Encina.MongoDB/MongoDbIndexCreator.cs`**: the indexes of Design Choice 6.
4. **Logging**: replace `OutboxMessageNotFoundForProcessed`/`OutboxMessageNotFoundForFailed` uses with the shared claim-lost log of Phase 10 (keep the `Log.cs` entries only if still used; remove dead ones).
5. When #718 is merged, every call passes the `IMongoSessionAccessor` session as the #718 plan's Phase 8 requires.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 of issue #1251 in Encina: row claiming in the MongoDB outbox and scheduler stores.

CONTEXT:
- MongoDB has no SKIP LOCKED. OutboxStoreMongoDB reads with Find/SortBy/Limit (Outbox/OutboxStoreMongoDB.cs:85-90)
  and completes with UpdateOneAsync by Id (:98-145).
- Design Choice 6: candidate ids, then a guarded UpdateMany that repeats the eligibility filter, then a read back
  by claimToken. Works on a standalone server.

TASK:
Implement ClaimPendingMessagesAsync / ClaimDueMessagesAsync, fenced completion (filter Id + ClaimToken, unset the
claim, ModifiedCount 0 => claim_lost), ReleaseClaimsAsync, GetDueCountAsync and claim clearing in
RequeueExhaustedAsync; add the claim indexes to MongoDbIndexCreator.

KEY RULES:
- The UpdateMany filter must repeat the full eligibility predicate plus the lease predicate, so a document taken
  or completed by another instance in between is never claimed.
- UTC dates with BsonDateTimeOptions(Kind = DateTimeKind.Utc); the claim token is stored as a string like Id.
- Pass the session from IMongoSessionAccessor when issue #718 is merged.

REFERENCE FILES:
- src/Encina.MongoDB/Outbox/OutboxStoreMongoDB.cs, Outbox/OutboxMessage.cs
- src/Encina.MongoDB/Scheduling/ScheduledMessageStoreMongoDB.cs, Scheduling/ScheduledMessage.cs
- src/Encina.MongoDB/MongoDbIndexCreator.cs, src/Encina.MongoDB/Log.cs
```

</details>

---

### Phase 9: Cross-Cutting Integration

> **Goal**: Tenancy, transactions and the relation with the distributed-lock and leader-election issues.

<details>
<summary><strong>Tasks</strong></summary>

1. **Multi-tenancy** (Design Choice 8):
   - When #737 and #739 have added `TenantId` to the outbox and scheduled-message tables, every claim statement on the 10 providers adds `AND TenantId = @TenantId` when `MessageClaimRequest.TenantId` is set.
   - With #1257's tenant enumeration, `OutboxProcessorBase` and `ScheduledMessageProcessor` run one claim per tenant per cycle; a batch never mixes tenants (REQ-061).
   - With tenancy off, `TenantId` is null and one claim covers all rows.
2. **Transactions**:
   - The claim is atomic per batch: one statement on SQL Server and PostgreSQL, one transaction on MySQL, one guarded `UpdateMany` on MongoDB.
   - Completion is atomic per message.
   - With #718 merged, the MySQL claim uses `IDbTransactionAccessor.BeginOrJoinAsync`. When `OutboxOrchestrator.ProcessPendingMessagesAsync` is called inside a request transaction, the claimed rows stay locked until that transaction commits, and other instances skip them. XML docs on the orchestrator say this.
3. **Distributed locks**: no lock is taken. XML docs and the feature page explain that row claims replace a global lock, and that #716 and #717 remain optional ways to reduce polling load.
4. **Idempotency**: document that a duplicate is possible only after a lost lease or a crash between the dispatch and the mark, and link #1220 and #735.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
You are implementing Phase 9 of issue #1251 in Encina: cross-cutting integration of row claiming.

CONTEXT:
- Phases 1-8 implemented claiming on all 10 providers. MessageClaimRequest.TenantId exists but no outbox or
  scheduled-message table has a TenantId column until issues #737 and #739 are merged; #1257 provides tenant-aware
  background cycles; #718 provides IDbTransactionAccessor.
- SPEC-002 REQ-061: background processing, including row claiming, never mixes tenants in one unit of work.

TASK:
If #737/#739 are merged, add the TenantId filter to every claim statement on the 10 providers and run one claim per
tenant per cycle when #1257's tenant enumeration is available; otherwise leave the null-tenant path and note the
dependency in the PR. Make the MySQL claim join or begin through IDbTransactionAccessor when #718 is merged.
Document the transaction, lock and idempotency semantics in XML docs.

KEY RULES:
- With tenancy off, no tenant configuration is needed (pay-for-what-you-use).
- Never mix tenants in one batch; never take a global distributed lock.
- Record the ADR-018 evaluation of the 12 functions in the PR.

REFERENCE FILES:
- src/Encina.Messaging/Outbox/OutboxProcessorBase.cs, src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs
- src/Encina.Tenancy/Abstractions/ITenantProvider.cs
- docs/plans/outbox-atomicity-ado-implementation-plan-718.md
```

</details>

---

### Phase 10: Observability

> **Goal**: Spans, counters and `[LoggerMessage]` logs for claim, reclaim, release and lost claims.

<details>
<summary><strong>Tasks</strong></summary>

1. **Activities**:
   - `OutboxActivitySource` (`src/Encina.Messaging/Diagnostics/OutboxActivitySource.cs:21`, source `Encina.Messaging.Outbox`) gains `StartClaim(int batchSize, string? tenantId)` → span `encina.outbox.claim`.
   - Tags: `encina.tenant_id`, `encina.claim.batch_size`, `encina.claim.claimed_count`, `encina.claim.reclaimed_count`; status `Error` with `error.type` set to the error code on `Left`.
   - `SchedulingActivitySource` (`:21`, source `Encina.Messaging.Scheduling`) gains `encina.scheduling.claim` with the same tags.
   - Never `ClaimedBy`, payloads or message content.
2. **Metrics**:
   - `OutboxProcessorMetrics` (`:58-69`) adds `encina.outbox.processor.claimed_total`, `encina.outbox.processor.reclaimed_total` and `encina.outbox.processor.claims_released_total` (`Counter<long>`, tag `encina.tenant_id`).
   - `encina.outbox.processor.messages_total` gains the `outcome="claim_lost"` value.
   - `SchedulingProcessorMetrics` (`:37-43`) adds `encina.scheduling.processor.claimed_total`, `...reclaimed_total` and `...claims_released_total`.
   - `encina.scheduling.processor.messages_total` records real outcomes, including `claim_lost`.
3. **Logs** (`[LoggerMessage]`, new `src/Encina.Messaging/Diagnostics/OutboxClaimLog.cs` and `SchedulingClaimLog.cs`), all in the range `EventIdRanges.Messaging` (2800-2999). The highest used today is 2962; #718 reserves 2963-2969 and #1200 takes 2970-2976 (coordinator agreement of 2026-10-06).
   - Outbox, **2977-2982**:
     - 2977 `OutboxBatchClaimed` (Debug: count, reclaimed count, claimed-by);
     - 2978 `OutboxExpiredLeasesReclaimed` (Information: count);
     - 2979 `OutboxClaimLost` (Warning: message id, operation);
     - 2980 `OutboxBatchStoppedBeforeLeaseExpiry` (Warning: remaining count, lease end);
     - 2981 `OutboxClaimsReleased` (Debug: count);
     - 2982 `OutboxClaimReleaseFailed` (Warning: error code).
   - Scheduling, **2983-2988**, with the same six messages (`Scheduling*` names).
   - No new range is registered: `Messaging` already maps to `Encina.Messaging` in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs:91`.
4. **Health checks**: none added (Matrix row 4).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 10</strong></summary>

```
You are implementing Phase 10 of issue #1251 in Encina: observability of row claiming.

CONTEXT:
- Outbox telemetry: OutboxActivitySource (source "Encina.Messaging.Outbox"), OutboxProcessorMetrics
  (encina.outbox.processor.messages_total). Scheduling: SchedulingActivitySource, SchedulingProcessorMetrics.
- EventId range (src/Encina/Diagnostics/EventIdRanges.cs): Messaging 2800-2999, highest used 2962; 2963-2969 are
  reserved by the #718 plan and 2970-2976 by the #1200 plan; this issue takes 2977-2988.

TASK:
Add the encina.outbox.claim and encina.scheduling.claim spans, the claimed/reclaimed/released counters and the
claim_lost outcome, and [LoggerMessage] logs with EventIds 2977-2982 (outbox) and 2983-2988 (scheduling) in new
OutboxClaimLog.cs and SchedulingClaimLog.cs.

KEY RULES:
- Tenant id is an attribute; ClaimedBy is never a metric tag; no payload, content or EncinaError.Message anywhere.
- EventIds packed sequentially inside the registered range; XML docs name the range (EventIdRanges.Messaging).
- Before writing, re-check that 2977-2988 are still free; if not, take the next free block and update the plan.

REFERENCE FILES:
- src/Encina.Messaging/Diagnostics/OutboxActivitySource.cs, OutboxProcessorMetrics.cs, OutboxStoreLog.cs
- src/Encina.Messaging/Diagnostics/SchedulingActivitySource.cs, SchedulingProcessorMetrics.cs, SchedulingProcessorLog.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

### Phase 11: Testing

> **Goal**: The AC-039 two-processor test on all 10 providers, plus every test type the issue requires.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit tests** (`tests/Encina.UnitTests/Messaging/Claiming/`, `Messaging/Outbox/`, `Messaging/Scheduling/`):
   - `MessageClaimRequest` validation; `MessageProcessorIdentity` default and override.
   - `OutboxBatchProcessor`: claim path, the stop-before-expiry guard with `FakeTimeProvider`, release on cancellation, `claim_lost` outcome, `Left` from the claim, release failure.
   - `SchedulerOrchestrator`: the same, plus the recurring path with the token, `ScheduledBatchResult` counts, `GetPendingCountAsync` through `GetDueCountAsync`, and `SaveChangesAsync` after the batch.
   - The fakes' in-memory claim; the OpenTelemetry decorators.
2. **Guard tests** (`tests/Encina.GuardTests/`): new public constructors and methods (`MessageClaimRequest`, stores' `ClaimPendingMessagesAsync`/`ClaimDueMessagesAsync`/`ReleaseClaimsAsync`, options setters).
3. **Contract tests** (`tests/Encina.ContractTests/Messaging/Claiming/`), on real implementations (the fakes and an in-memory EF context):
   - two claims never return the same row;
   - an expired lease is reclaimable;
   - completion with a foreign token returns `claim_lost`;
   - release makes the rows claimable at once;
   - `RequeueExhaustedAsync` clears the claim.
4. **Property tests**: a `.md` justification `tests/Encina.PropertyTests/Messaging/Claiming/ProcessorRowClaiming.md` (behavior depends on the database, as the issue's Test Matrix says), plus one FsCheck property on the fake store: for any interleaving of claim, complete and release, no message is completed under two tokens.
5. **Integration tests** (real databases, shared collections `ADO-<Db>`, `Dapper-<Db>`, `EFCore-<Db>`, MongoDB; `[Trait("Category", "Integration")]`; `ClearAllDataAsync` in `InitializeAsync`):
   - **AC-039**: two `OutboxProcessor` instances (two scopes, two connections or contexts) over 500 messages with a counting handler; every message is published exactly once.
   - The same with two `ScheduledMessageProcessor` instances.
   - Concurrent claims return disjoint sets; an expired lease is reclaimed with `ReclaimedCount`; fenced completion with a stale token returns `claim_lost`; release; `GetDueCountAsync` does not claim.
   - On EF Core, a scheduled message's outcome is persisted (regression of the `SaveChangesAsync` defect).
   - Two-tenant claim test (AC-043) once #737 and #739 are merged.
   - Locations next to the existing store tests, for example `tests/Encina.IntegrationTests/ADO/SqlServer/Outbox/OutboxStoreADOClaimTests.cs` and `tests/Encina.IntegrationTests/Infrastructure/MongoDB/Stores/OutboxStoreMongoDBClaimTests.cs`.
6. **Instrumentation test** (AC-044): in-memory exporter asserts the claim spans and counters carry `encina.tenant_id`, and that no tag or log carries `ClaimedBy`, content or an error message.
7. **Load tests**: `tests/Encina.LoadTests/Messaging/Claiming/ProcessorClaimingLoadTests.cs`, shaped like `tests/Encina.LoadTests/Cdc/CdcProcessorLoadTests.cs`:
   - 2, 4 and 8 processors on PostgreSQL and SQL Server;
   - throughput, claim latency, duplicate count (must be 0) and reclaim count;
   - remove the obsolete justification `tests/Encina.LoadTests/Messaging/Scheduling/ScheduledMessageProcessorLoadTests.md`, whose reason 3 ("requires #716") no longer holds.
8. **Benchmarks**: `.md` justification `tests/Encina.BenchmarkTests/Encina.Benchmarks/Messaging/Claiming/ProcessorRowClaiming.md` (one statement per batch, not a hot path), plus a claim variant in the existing `OutboxStoreBenchmarks.cs` of each ADO.NET and Dapper package so the change of the fetch is measured.
9. **Coverage obligations**: per-file targets with one-sentence justifications in `.github/coverage-manifest/Encina.Messaging.json`, `Encina.ADO.*.json`, `Encina.Dapper.*.json`, `Encina.EntityFrameworkCore.json`, `Encina.MongoDB.json` for every file touched; local measurement and `--check-justifications`; local CRAP table of the changed methods (CRAP <= 10).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 11</strong></summary>

```
You are implementing Phase 11 of issue #1251 in Encina: tests for row claiming.

CONTEXT:
- Phases 1-10 implemented claiming on all 10 providers. AC-039: "a two-processor test on each of the 10 providers
  in which no message is processed twice". Integration tests use shared collections ADO-<Db>, Dapper-<Db>,
  EFCore-<Db> and the MongoDB collection; MySQL runs on the mysql:9.1 image.
- The scheduler on EF Core lost outcomes before this change (SchedulerOrchestrator never saved); add a regression test.

TASK:
Write unit, guard, contract, property (justification + one FsCheck property on the fake), integration (the
two-processor tests for outbox and scheduler on all 10 providers, concurrent claim, reclaim, fencing, release,
read-only count, EF scheduler persistence), instrumentation, load and benchmark tests; add per-file coverage targets
with justifications to the coverage manifests and measure them locally.

KEY RULES:
- Tests execute real code (no reflection-only tests); Shouldly through Encina.Testing.Shouldly; no Thread.Sleep —
  use FakeTimeProvider for lease expiry and task coordination for concurrency.
- [Collection("<Family>-<Db>")] shared fixtures only; never IClassFixture or new fixtures; ClearAllDataAsync in
  InitializeAsync.
- Two processors means two scopes with their own connection or DbContext against the same database.
- Measure each flag (UnitTests, GuardTests, ContractTests, PropertyTests, IntegrationTests) with coverage-report.cs.

REFERENCE FILES:
- tests/Encina.IntegrationTests/ADO/SqlServer/Outbox/OutboxStoreADOTests.cs
- tests/Encina.IntegrationTests/Infrastructure/MongoDB/Outbox/OutboxProcessorMongoDBIntegrationTests.cs
- tests/Encina.UnitTests/Messaging/Outbox/OutboxProcessorBaseTests.cs
- tests/Encina.LoadTests/Cdc/CdcProcessorLoadTests.cs
- docs/testing/coverage-measurement-methodology.md
```

</details>

---

### Phase 12: Documentation & Finalization

> **Goal**: Every mandatory document of the prompt, the ADR and a clean build and test run.

<details>
<summary><strong>Tasks</strong></summary>

1. **XML docs** on every new or changed public API (`<summary>`, `<remarks>` with lease and fencing semantics, `<param>`, `<returns>`, `<example>` for options).
2. **Changelog fragments**:
   - `changelog.d/1251-processor-row-claiming.added.md`: row claiming with leases on the 10 providers.
   - `changelog.d/1251-processor-row-claiming.changed.md`: `IOutboxStore` and `IScheduledMessageStore` signatures, new columns.
   - `changelog.d/1251-scheduler-ef-outcomes.fixed.md`: only if the scheduler `SaveChangesAsync` defect is not fixed separately first.
3. **ADR**: `docs/architecture/adr/043-processor-row-claiming.md` (reserved in `docs/architecture/adr/index.md`). It records lease versus lock versus leader election, fencing, and the per-dialect statements.
4. **Feature page**: new `docs/features/processor-row-claiming.md` (explanation: why and how claims work, sizing `LeaseDuration`, multi-instance guidance); update `docs/features/scheduling.md`. Follow the encina-docs skill and cite figures with covref markers, never by hand.
5. **READMEs**: `src/Encina.Messaging/README.md` and the READMEs of the 8 provider packages (multi-instance section, new columns).
6. **`docs/INVENTORY.md`**: new files (`Claiming/`, claim logs, SQL builders, load test).
7. **`ROADMAP.md`**: mark REQ-056 / P-47 delivered in the v0.18.0 milestone.
8. **`PublicAPI.Unshipped.txt`**: `Encina.Messaging`, the 8 provider packages, `Encina.OpenTelemetry`, `Encina.Testing`, `Encina.Testing.Fakes`, `Encina.Testing.Bogus`.
9. **`docs/releases/`**: update the release notes of the version that ships it, if the folder exists by then.
10. **Verification**:
    - `dotnet build Encina.slnx --configuration Release`: 0 errors, 0 warnings.
    - `dotnet test`: all pass, and every coverage flag reaches its own target in `.github/coverage-manifest/{Package}.json`.
    - `crap-gate` local table: no changed method above 10.
11. **Follow-up**: after merge, the orchestrator proposes closing #716 (its acceptance "same scheduled message never fires more than once" is met) or narrowing it to an optional lock.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 12</strong></summary>

```
You are implementing Phase 12 of issue #1251 in Encina: documentation and finalization of row claiming.

CONTEXT:
- Phases 1-11 delivered claiming, telemetry and tests on all 10 providers.
- Documentation rules: .claude/skills/encina-docs/SKILL.md (one Diataxis quadrant per page, identifiers verified in
  src/, figures cited with covref/mutref markers). Changelog: changelog.d fragments only, never CHANGELOG.md.

TASK:
Write XML docs, the changelog fragments, ADR 043, docs/features/processor-row-claiming.md, the
update of docs/features/scheduling.md, the READMEs of Encina.Messaging and the 8 provider packages, INVENTORY.md,
ROADMAP.md and PublicAPI files; then build with zero warnings, run the tests, measure every coverage flag and the
CRAP table.

KEY RULES:
- English only; no hand-typed coverage numbers; no AI attribution.
- Explain sizing of LeaseDuration (longest batch plus clock skew) and the at-least-once caveat with links to #1220
  and #735.
- Do not close or edit issues; report #716 for the orchestrator.

REFERENCE FILES:
- docs/features/scheduling.md, src/Encina.Messaging/README.md, changelog.d/README.md
- docs/architecture/adr/036-three-audit-stores.md (ADR format)
- .github/scripts/coverage-report.cs
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Reference | Relevance |
|--------|-----------|-----------|
| SPEC-002 | REQ-056, AC-039, P-47 | Row claiming (`SKIP LOCKED`, `READPAST`) in outbox and scheduler processors; verified by a two-processor test on each of the 10 providers |
| SPEC-002 | REQ-061, AC-043, DEC-009 (b) | Background processing, row claiming included, restores the tenant and never mixes tenants in one unit of work |
| SPEC-002 | REQ-062, AC-044, DEC-010 | `ActivitySource`, `Meter`, `[LoggerMessage]` in registered ranges; tenant as attribute; no payloads |
| SPEC-002 | §11.1 | "Single or multiple instances: None: row claiming (REQ-056, P-47) is in 1.0 either way" |
| SQL Server | Table hints `UPDLOCK`, `READPAST`, `ROWLOCK`; `OUTPUT` clause | Non-blocking queue-table claim in one statement |
| PostgreSQL | `SELECT ... FOR UPDATE SKIP LOCKED`; `UPDATE ... RETURNING` | Non-blocking claim; `RETURNING` row order is unspecified |
| MySQL 8.0+ (fixtures use 9.1) | `FOR UPDATE SKIP LOCKED`; error 1093 (update of a table read in its own subquery) | Why the MySQL claim is a short transaction |
| MongoDB | Single-document atomicity of `updateMany` per document | Why the guarded filter is enough without a transaction |
| ADR-018, ADR-021 | Cross-cutting integration; EventId ranges | Matrix below; EventIds 2977-2988 |
| AGENTS.md §3 | `TimeProvider`, async DB calls, errors never swallowed | Lease times, `OpenAsync`, `claim_lost` as `Left` |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `OutboxBatchProcessor` | `src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs` | Single outbox cycle; claim, guard, release, fenced completion (Phase 3) |
| `OutboxProcessorBase` | `src/Encina.Messaging/Outbox/OutboxProcessorBase.cs` | Base of every provider's `OutboxProcessor`; one change covers 10 providers |
| `SchedulerOrchestrator.ProcessDueMessagesAsync` | `src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs:288` | Scheduler cycle; claim and per-outcome result |
| `OutboxOptions.Validate` | `src/Encina.Messaging/Outbox/OutboxOptions.cs:139` | Cross-property rule `LeaseSafetyMargin < LeaseDuration` |
| `AddOutboxInboxSagaSchedulingServices` | `src/Encina.Messaging/MessagingServiceCollectionExtensions.cs:203` | Shared DI of all 10 providers; registers `MessageProcessorIdentity` |
| `RequeueIdBatchSize` chunking | `src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs:21,282` | Template for chunked `ReleaseClaimsAsync` |
| Connection-type dialect detection | `src/Encina.EntityFrameworkCore/BulkOperations/BulkOperationsEF.cs:98-99` | `EfClaimDialect` |
| `MongoDbIndexCreator` | `src/Encina.MongoDB/MongoDbIndexCreator.cs` | Claim indexes |
| `OutboxActivitySource`, `OutboxProcessorMetrics`, `SchedulingActivitySource`, `SchedulingProcessorMetrics` | `src/Encina.Messaging/Diagnostics/` | Claim spans and counters |
| `InstrumentedOutboxStore`, `InstrumentedScheduledMessageStore` | `src/Encina.OpenTelemetry/MessagingStores/` | Decorators updated to the new signatures |
| `FakeOutboxStore`, `FakeScheduledMessageStore` | `src/Encina.Testing.Fakes/Stores/` | In-memory claim for unit and contract tests |
| `IDbTransactionAccessor` (planned) | #718 plan, Phases 1 and 5 | MySQL claim transaction joins or begins through it |
| Shared fixtures | `tests/Encina.IntegrationTests/**/Collections.cs`, `tests/Encina.TestInfrastructure/Schemas/` | Two-processor integration tests |
| `CdcProcessorLoadTests` | `tests/Encina.LoadTests/Cdc/CdcProcessorLoadTests.cs` | Harness shape for the claiming load test |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` (outbox claim) | `Messaging` 2800-2999 | **2977-2982** new |
| `Encina.Messaging` (scheduler claim) | `Messaging` 2800-2999 | **2983-2988** new |
| `Encina.Messaging` (other plans) | `Messaging` 2800-2999 | Highest used today 2962; 2963-2969 reserved by the #718 plan; 2970-2976 by the #1200 plan |
| `Encina.Messaging` (pattern ranges) | `MessagingOutbox` 2000-2099, `MessagingScheduling` 2300-2399 | Not used by this plan (coordinator agreement keeps the three plans in one range) |
| `Encina.MongoDB` | 3100-3199 | No new IDs; the not-found logs of `OutboxStoreMongoDB` are replaced by the shared claim-lost log |
| `Encina.ADO.*`, `Encina.Dapper.*`, `Encina.EntityFrameworkCore` | 3000-3499 | No new log messages |

No range is registered: `Messaging` already maps to `Encina.Messaging` (`EncinaEventIdAllocationTests.cs:91`). The 2963-2988 split between #718, #1200 and #1251 was agreed by the coordinator on 2026-10-06, so the EventIds do not constrain the order (the recommended order stays #718 first). 2989-2999 then remain free in the range.

### Open Questions to Verify During Implementation

| Question | How to settle it | Fallback |
|----------|------------------|----------|
| Does MySQL 9.1 accept a single-statement claim (`UPDATE t JOIN (SELECT id ... LIMIT n FOR UPDATE SKIP LOCKED) c ON ...`)? | Integration test on the `MySQL` fixture in Phase 5 | The transaction form of Design Choice 4 (default) |
| Does Npgsql return `RETURNING o.*, c.previousclaimedby` in a shape Dapper maps without aliases? | Phase 6 integration test | Explicit column list |
| Does `ExecuteUpdateAsync` on Pomelo MySQL honor a `Where` on a `CHAR(36)` `Guid` token? | Phase 7 integration test on `EFCore-MySQL` | Raw `ExecuteSqlRawAsync` for completion on MySQL only |

### File Count Estimate

| Category | Files | Notes |
|----------|-------|-------|
| `Encina.Messaging` (Phases 1-3, 10) | ~16 | 3 new in `Claiming/`, 2 claim logs, 2 interfaces, 2 message interfaces, options ×2, processors, orchestrators, metrics, activity sources, DI |
| ADO.NET ×3 (Phases 4-5) | ~21 | 2 stores, 2 entities, 3 scripts, 1 SQL builder per package |
| Dapper ×3 (Phases 4, 6) | ~21 | Same |
| EF Core (Phases 4, 7) | ~7 | 2 stores, 2 entities, 2 configurations, dialect |
| MongoDB (Phases 4, 8) | ~6 | 2 stores, 2 entities, index creator, `Log.cs` |
| OpenTelemetry, Testing, Fakes, Bogus | ~10 | Decorators, fakes, helpers, fakers |
| Test schemas | ~7 | `TestInfrastructure/Schemas`, benchmark schema builders |
| Tests (Phase 11) | ~55 | ~20 integration (2 stores × 10 providers), ~12 unit, ~8 guard, 2 contract, 1 property + `.md`, 1 load, benchmark variants, DI tests |
| Documentation (Phase 12) | ~16 | ADR, feature page, scheduling page, 9 READMEs, INVENTORY, ROADMAP, changelog fragments |
| **Total** | **~160** | |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing issue #1251 in Encina: row claiming in the outbox and scheduler processors so that several
hosts never process the same message twice (SPEC-002 REQ-056, AC-039).

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 CQRS library, pre-1.0: breaking changes are done completely, no [Obsolete].
- Railway Oriented Programming: Either<EncinaError, T>; errors never swallowed; EncinaError.Message never logged.
- Database providers: ADO.NET x3, Dapper x3, EF Core x3 (SqlServer, PostgreSQL, MySQL) and MongoDB; SQLite is out.
- Today every processor reads pending/due rows with a plain query (IOutboxStore.GetPendingMessagesAsync,
  IScheduledMessageStore.GetDueMessagesAsync) and completes by Id only, so two hosts dispatch the same rows.
- Ordering: issue #718 (transaction enlistment in the same stores) is recommended first; #1200 (purge) can land
  before or after (purge touches only processed rows, claims only unprocessed rows).

IMPLEMENTATION OVERVIEW:
Phase 1: Claiming/ (MessageClaimRequest, MessageClaim<T>, MessageProcessorIdentity); claim methods replace the
         fetch; completion fenced by Guid claimToken; ReleaseClaimsAsync; GetDueCountAsync; claim_lost codes
Phase 2: LeaseDuration (5 min), LeaseSafetyMargin (30 s), ProcessorInstanceId on both options; SchedulingOptions.Validate;
         MessageProcessorIdentity singleton; ValidateOnBuild DI tests
Phase 3: OutboxBatchProcessor and SchedulerOrchestrator claim, stop before lease expiry, release on stop, fenced
         completion, per-outcome results; decorators, fakes, helpers, fakers
Phase 4: ClaimedBy/ClaimToken/ClaimedUntilUtc columns and claim indexes on the outbox and scheduled-message tables of every provider (12 ADO.NET/Dapper scripts, the three EF Core models and two MongoDB collections); test schemas
Phase 5: ADO.NET x3: SQL Server CTE UPDATE ... OUTPUT (UPDLOCK, READPAST, ROWLOCK); PostgreSQL UPDATE ... FROM
         (SELECT ... FOR UPDATE SKIP LOCKED) RETURNING; MySQL transaction SELECT FOR UPDATE SKIP LOCKED + UPDATE + SELECT
Phase 6: Dapper x3: same SQL through CommandDefinition with transaction and cancellation token
Phase 7: EF Core: EfClaimDialect from the EF model; raw claim, AsNoTracking load by token, fenced ExecuteUpdateAsync
Phase 8: MongoDB: candidate ids, guarded UpdateMany, read back by token; fenced UpdateOneAsync; indexes
Phase 9: cross-cutting: TenantId filter when #737/#739 merged, per-tenant cycles with #1257, #718 accessor for MySQL
Phase 10: observability: encina.outbox.claim / encina.scheduling.claim spans, claimed/reclaimed/released counters,
          claim_lost outcome, EventIds 2977-2988 (Messaging range)
Phase 11: tests: two-processor AC-039 test for outbox and scheduler on 10 providers, contract, unit, guard, property,
          instrumentation, load (2/4/8 processors), benchmark variants, coverage manifests
Phase 12: docs: XML, changelog.d fragments, ADR, docs/features/processor-row-claiming.md, READMEs, INVENTORY, PublicAPI

KEY PATTERNS:
- Claim = one atomic statement (or one short transaction on MySQL, one guarded UpdateMany on MongoDB) per batch.
- Completion = WHERE Id = @Id AND ClaimToken = @ClaimToken, clears the claim; 0 rows => Left(<pattern>.claim_lost).
- Lease times from TimeProvider; claim token from an injectable Func<Guid>; release with CancellationToken.None.
- Store naming unchanged (OutboxStoreADO, OutboxStoreDapper, OutboxStoreEF, OutboxStoreMongoDB, ScheduledMessageStore*).
- Integration tests: [Collection("<Family>-<Db>")] shared fixtures, ClearAllDataAsync in InitializeAsync.
- Telemetry: tenant id attribute; never ClaimedBy as a metric tag; no payloads.

REFERENCE FILES:
- src/Encina.Messaging/Outbox/IOutboxStore.cs, OutboxBatchProcessor.cs, OutboxProcessorBase.cs, OutboxOptions.cs
- src/Encina.Messaging/Scheduling/IScheduledMessageStore.cs, SchedulerOrchestrator.cs, ScheduledMessageProcessor.cs
- src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs, src/Encina.Dapper.PostgreSQL/Outbox/OutboxStoreDapper.cs
- src/Encina.EntityFrameworkCore/Outbox/OutboxStoreEF.cs, src/Encina.MongoDB/Outbox/OutboxStoreMongoDB.cs
- docs/plans/processor-row-claiming-implementation-plan-1251.md
- docs/plans/outbox-atomicity-ado-implementation-plan-718.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | No read path that benefits from caching; the claim must always hit the database |
| 2 | OpenTelemetry | ✅ | `encina.outbox.claim` and `encina.scheduling.claim` spans; claimed, reclaimed and released counters; `claim_lost` outcome; tenant id as attribute (Phase 10) |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` with EventIds 2977-2988 in the already registered `Messaging` range, after the blocks of #718 and #1200 (Phase 10) |
| 4 | Health Checks | ❌ | No new checkable dependency; expired leases are reclaimed automatically, and the existing `OutboxHealthCheck`/`SchedulingHealthCheck` keep covering the store |
| 5 | Validation | ✅ | `OutboxOptions.Validate` and a new `SchedulingOptions.Validate` check the lease options at start-up; `MessageClaimRequest` validates its arguments (Phases 1-2) |
| 6 | Resilience | ❌ | No call to an external system; a failed claim returns `Left` and the next cycle retries |
| 7 | Distributed Locks | ✅ | Row-level claims replace a global lock (Design Choice 7); #716 and #717 stay optional for polling load only (Phase 9) |
| 8 | Transactions | ✅ | Claim atomic per batch, completion atomic per message; MySQL claim joins or begins through the #718 accessor (Phase 9) |
| 9 | Idempotency | ⏭️ | Duplicates remain possible only after a lost lease or a crash between dispatch and mark; consumer-side deduplication is #1220 (outbox) and #735 (scheduled), both open |
| 10 | Multi-Tenancy | ✅ | `MessageClaimRequest.TenantId` filters the claim; columns from #737 and #739, per-tenant cycles from #1257; two-tenant test once they merge (Phases 9, 11) |
| 11 | Module Isolation | ❌ | SPEC-002 requires no module scoping; `ModuleId` on messaging entities stays with #747 |
| 12 | Audit Trail | ❌ | Claiming is infrastructure coordination, not a compliance-relevant operation; processing audit stays with #749 |

---

## Prerequisites & Dependencies

### Advisable before or with this work

| Issue | State | Relation |
|-------|-------|----------|
| #718 (plan decided 2026-10-06) | open | Same 20 store files: enlistment, `OpenAsync`, accessor constructor parameter, removal of `OutboxPostProcessor`. Recommended first; the MySQL claim uses its `IDbTransactionAccessor` |
| Scheduler EF outcomes defect (issue file `artifacts/issues/plan-1251-scheduler-ef-outcomes-not-saved.md`) | to open | `SchedulerOrchestrator.ProcessDueMessagesAsync` never calls `SaveChangesAsync`, so EF Core re-executes scheduled messages every cycle. A small separate fix now is recommended; Phases 3 and 7 also remove the cause |
| #1170 / #1868 | open | No-op `OpenConnectionAsync` in the ADO.NET stores; fixed for the messaging stores by #718 Phase 5, or by Phase 5 here if #1251 lands first |
| #737, #739 | open | `TenantId` on outbox and scheduled messages; required for the tenant filter and the two-tenant test (Phase 9, Phase 11) |
| #1257, #1164 | open | Tenant enumeration for per-tenant cycles; persisted request context restored on dispatch |

### Parallel and follow-up work

| Issue | Relation |
|-------|----------|
| #1200 | Purge of processed messages in the same stores, scripts, options and processor loops. Either can land first: purge touches only processed rows and claims only unprocessed rows, so the conflicts are textual (DDL edits, position of the purge step). EventIds are agreed: #1200 2970-2976, this plan 2977-2988 |
| #716 | Distributed locks for scheduled dispatch; its acceptance "the same scheduled message never fires more than once" is met by this plan. Recommend closing it after merge or narrowing it to an optional polling lock |
| #717 | Leader election; stays an optional complement that reduces polling load |
| #1220, #735 | Consumer-side idempotency for the remaining at-least-once duplicates |
| #1221 | Stable persisted type names; touches the same entities, no functional overlap |

---

## Next Steps

1. The maintainer reviews the eight Design Choices and records the decisions; the orchestrator then adds the Maintainer Decisions section.
2. Open the issue drafted in `artifacts/issues/plan-1251-scheduler-ef-outcomes-not-saved.md` and decide whether it is fixed before this plan starts.
3. Confirm the order: #718 first, then #1251 and #1200 in either order. The second of the two rebases its DDL edits and the position of the purge step; EventIds are already agreed (2963-2969, 2970-2976, 2977-2988).
4. Link this plan from #1251 before implementation starts.
5. Implement Phases 1-8 as one PR (`Refs #1251`), then Phases 9-12 (`Fixes #1251`), one self-contained commit per phase where the build allows it.
