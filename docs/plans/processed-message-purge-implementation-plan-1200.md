# Implementation Plan: Purge of Processed Outbox and Executed Scheduled Messages on All 10 Database Providers

> **Issue**: [#1200](https://github.com/dlrivada/Encina/issues/1200) (SPEC-002 tracking id **P-12**, priority P0)
> **Type**: Feature
> **Complexity**: Medium-high (11 phases, all 10 database providers, one new hosted service in `Encina.Messaging`)
> **Estimated Scope**: ~900-1,300 lines of production code + ~2,000-2,800 lines of tests
> **Parent EPIC**: [#1186](https://github.com/dlrivada/Encina/issues/1186) — EU regulatory readiness (SPEC-002)
> **Specification**: [SPEC-002](../specifications/SPEC-002-eu-regulatory-readiness.md) REQ-018, AC-018, scenario S21; cross-cutting REQ-061 (AC-043) and REQ-062 (AC-044)
> **Related plans**: [#718](https://github.com/dlrivada/Encina/issues/718) outbox atomicity (`docs/plans/outbox-atomicity-ado-implementation-plan-718.md`, branch `docs/plans-backfill-01`), [#1251](https://github.com/dlrivada/Encina/issues/1251) row claiming (planned in parallel)

---

## Summary

Processed outbox messages and executed scheduled messages are deleted once they are older than a configurable retention period (30 days by default, automatic purge on), on all 10 database providers. Pending and dead-lettered messages are never purged automatically. The application can lengthen the period or switch the purge off. This implements GDPR Art. 5(1)(e) (storage limitation) for the two message tables that keep payloads, which can carry personal and health data, forever today.

### What the code does today (2026-10-06, `main` at `5b485b12`)

Nothing of #1200 is implemented. The research found four facts that shape the plan; two of them contradict the issue and SPEC-002 REQ-018.

1. **No purge on the outbox or scheduled stores.** `IOutboxStore` (`src/Encina.Messaging/Outbox/IOutboxStore.cs:23-128`) and `IScheduledMessageStore` (`src/Encina.Messaging/Scheduling/IScheduledMessageStore.cs:23-93`) have no delete-by-age operation. `OutboxOptions` (`src/Encina.Messaging/Outbox/OutboxOptions.cs:10-148`) and `SchedulingOptions` (`src/Encina.Messaging/Scheduling/SchedulingOptions.cs:10-52`) have no retention setting. The only delete is `CancelAsync` on a scheduled message (`src/Encina.ADO.SqlServer/Scheduling/ScheduledMessageStoreADO.cs:228`).
2. **The inbox is not purged either, on 9 of the 10 providers.** The issue and REQ-018 say the outbox should be purged "as the inbox already is". It is not:
   - `InboxOptions.EnableAutomaticPurge`, `PurgeInterval` and `PurgeBatchSize` (`src/Encina.Messaging/Inbox/InboxOptions.cs:33,39,45`) are read nowhere in `src/`.
   - `MessageRetentionPeriod` is read once, to stamp `ExpiresAtUtc` at receipt (`src/Encina.Messaging/Inbox/InboxOrchestrator.cs:118`).
   - `IInboxStore.RemoveExpiredMessagesAsync` (`src/Encina.Messaging/Inbox/IInboxStore.cs:88`) has no production caller; `GetExpiredMessagesAsync` is called only by `InboxHealthCheck` (`src/Encina.Messaging/Health/InboxHealthCheck.cs:38`).
   - Only MongoDB deletes expired inbox documents, through a TTL index (`src/Encina.MongoDB/MongoDbIndexCreator.cs:101-107`).
   - The package READMEs document an automatic purge that does not run (for example `src/Encina.EntityFrameworkCore/README.md:216-219`).
3. **The purge predicate needs no retry count.** Every store sets `ProcessedAtUtc` only on success. For the outbox this happens in `MarkAsProcessedAsync` (`src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs:143-147`). Pending and dead-lettered rows keep `ProcessedAtUtc IS NULL`, and dead letters are `ProcessedAtUtc IS NULL AND RetryCount >= @MaxRetries` (`OutboxStoreADO.cs:229-233`). An active recurring scheduled message has `ProcessedAtUtc` reset to `NULL` on every reschedule (`ScheduledMessageStoreADO.cs:198-205`; EF `src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessageStoreEF.cs:131`). `ProcessedAtUtc IS NOT NULL AND ProcessedAtUtc < @cutoff` therefore selects exactly the processed or executed rows, on every provider.
4. **Two defects next to this feature would make the purge wrong or silent.** Each one gets an issue file, listed under Prerequisites & Dependencies.
   - **Recurring messages are dispatched again after their recurrence ends, on ADO.NET and Dapper.** These six stores select due rows with `(ProcessedAtUtc IS NULL OR IsRecurring = 1)` (for example `ScheduledMessageStoreADO.cs:92` and `src/Encina.Dapper.PostgreSQL/Scheduling/ScheduledMessageStoreDapper.cs:76`). EF Core and MongoDB use `ProcessedAtUtc == null` (`ScheduledMessageStoreEF.cs:62-63`, `src/Encina.MongoDB/Scheduling/ScheduledMessageStoreMongoDB.cs:80-81`). When a recurrence ends, `SchedulerOrchestrator` marks the message processed (`src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs:431,451`). On the six SQL-text stores the row stays due, is dispatched on every cycle, and its `ProcessedAtUtc` is refreshed each time, so it never ages out.
   - **CDC-published outbox rows are never marked processed.** `OutboxCdcHandler` publishes on insert but never calls `IOutboxStore` (`src/Encina.Cdc/Messaging/OutboxCdcHandler.cs:67-106`). Under CDC alone, those rows keep `ProcessedAtUtc IS NULL` and are never purged. In the hybrid setup that `docs/examples/cdc-outbox-integration.md:65-71` recommends, the polling processor publishes them a second time, which contradicts that page.

**Indexes.** Every outbox index already leads with `ProcessedAtUtc`, so the purge range scan is served with no new outbox index:

- SQL scripts: `IX_OutboxMessages_ProcessedAt_RetryCount` in `src/Encina.{ADO,Dapper}.{SqlServer,PostgreSQL,MySQL}/Scripts/001_CreateOutboxMessagesTable.sql`.
- EF Core: `src/Encina.EntityFrameworkCore/Outbox/OutboxMessageConfiguration.cs:42`.
- MongoDB: `IX_Outbox_Pending` (`MongoDbIndexCreator.cs:73-82`).

The scheduled-message indexes of the six ADO.NET and Dapper scripts lead with `ScheduledAtUtc` (`IX_ScheduledMessages_ScheduledAt_Processed` in `004_CreateScheduledMessagesTable.sql`), so they need a new index. EF Core (`ScheduledMessageConfiguration.cs:63`) and MongoDB (`IX_Scheduled_Due`, `MongoDbIndexCreator.cs:155-163`) already lead with `ProcessedAtUtc`.

**Tenancy.** Neither table has a tenant column: #737 (outbox) and #739 (scheduled) are open, and `ScheduledMessageProcessor.cs:42` records the gap. A per-tenant purge cannot be expressed today. #1257 already owns the two-tenant purge test of AC-043 ("background cycle ... retention sweep, purge").

### Scope

- **Implements**: SPEC-002 REQ-018 / AC-018 / S21 on the outbox and scheduled-message stores. The inbox's dead purge options are covered under Design Choice 2.
- **Affected packages**:
  - `Encina.Messaging`: options, store contracts, purge service, DI, diagnostics.
  - `Encina.ADO.SqlServer`, `Encina.ADO.PostgreSQL`, `Encina.ADO.MySQL`.
  - `Encina.Dapper.SqlServer`, `Encina.Dapper.PostgreSQL`, `Encina.Dapper.MySQL`.
  - `Encina.EntityFrameworkCore` (one package for SqlServer, PostgreSQL and MySQL).
  - `Encina.MongoDB`.
  - `Encina.OpenTelemetry` (instrumented store decorators) and `Encina.Testing.Fakes` (fake stores), because both implement the two interfaces.
- **Provider category**: Database, all 10 providers. SQLite is out of the matrix (ADR-024).
- **Out of scope**: Hangfire and Quartz job stores (infrastructure schedulers, not `IScheduledMessageStore`; their persisted responses were #1173); the saga store; dead-letter purge (REQ-017 keeps dead letters until the application acts).
- **Estimated files**: ~45 production files touched or created (most of them one new method per store), ~45 test files.

---

## Design Choices

<details>
<summary><strong>1. Where the purge cycle runs — a dedicated hosted service</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Inside the existing processors** (`OutboxProcessorBase.ExecuteAsync`, `ScheduledMessageProcessor.ExecuteAsync`), on its own interval | Matches the issue wording; no new hosted service; reuses the processor's scope and logger | The purge stops whenever the processor is off. `EnableProcessor = false` returns immediately (`OutboxProcessorBase.cs:87-91`, `ScheduledMessageProcessor.cs:94-98`), and that is the CDC setup. Mixes two cadences (seconds and hours) in one loop. `OutboxProcessorBase` has 10 provider subclasses whose tests all change |
| **B) A new `MessagePurgeService : BackgroundService` in `Encina.Messaging`**, one loop per pattern store, registered when any pattern has purge on | Independent of `EnableProcessor`, so CDC or external dispatch still purges; one place for cadence, metrics and logs; one service can also run the inbox purge (Choice 2); follows `OperationAuditRetentionService` (`src/Encina.Security.Audit/OperationAuditRetentionService.cs:48,99,113-151`) | One more hosted service; differs from the issue's wording |
| **C) Store operation only**; the application schedules it (Hangfire, Quartz, cron) | Smallest change; full control for the application | The 30-day default and "purge on" of REQ-018 are not met out of the box; every application re-implements the loop (the issue rejects this as alternative 1) |

### Chosen Option: **B — dedicated `MessagePurgeService`** (recommended, pending the maintainer)

### Rationale

- I recommend B because retention is a data-protection obligation that must hold however messages are dispatched. With A, an application that publishes the outbox through CDC (`EnableProcessor = false`) would silently keep payloads forever.
- The purge cadence (default 24 h, like `InboxOptions.PurgeInterval`) has nothing in common with the dispatch cadence (default 30 s). Separate loops keep both simple and keep CRAP low on the changed methods.
- The store operation from Choice 3 stays public, so an application that prefers its own scheduler disables `EnableAutomaticPurge` and calls it. Option C remains available without being the default.

</details>

<details>
<summary><strong>2. Inbox scope — wire the existing inbox purge options too</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Outbox and scheduled messages only (as the issue says)**; the dead inbox options go to a separate `[BUG]` | Smallest scope; matches the issue title | REQ-018's premise ("as the inbox already is") stays false on 9 providers; the READMEs keep documenting a purge that does not run; a second PR later adds the same service wiring for the inbox |
| **B) The same service also purges the inbox** through the existing `GetExpiredMessagesAsync` + `RemoveExpiredMessagesAsync` (`IInboxStore.cs:78,88`), honouring `InboxOptions.EnableAutomaticPurge`, `PurgeInterval` and `PurgeBatchSize` | Makes the three options live and REQ-018's premise true; no inbox store change (the methods exist on all 10 providers); one service, one set of metrics | Widens the PR; the inbox keeps its two-step API (fetch ids, delete by ids), which differs from the outbox's single call |
| **C) B plus reshape the inbox to the single-call `PurgeExpiredAsync`** | One store API shape for the three patterns | Changes 10 more stores, their tests and the instrumented decorator for no functional gain; the MongoDB TTL index already purges that provider |

### Chosen Option: **B — the service also runs the inbox purge with the existing store methods** (recommended, pending the maintainer)

### Rationale

- I recommend B because the inbox options exist, are documented and do nothing today. The service built for Choice 1 can honour them for the cost of one more loop, without touching any inbox store.
- On MongoDB the TTL index already deletes expired inbox documents (`MongoDbIndexCreator.cs:101-107`); the service's inbox loop then finds nothing, which is harmless.
- If the maintainer chooses A, the issue file `plan-1200-inbox-automatic-purge-never-runs.md` records the inbox defect on its own.

</details>

<details>
<summary><strong>3. Store operation shape — one batched delete returning the count</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `PurgeProcessedAsync(DateTime processedBeforeUtc, int batchSize, CancellationToken)` → `Either<EncinaError, int>`** on `IOutboxStore` and `IScheduledMessageStore`; one bounded `DELETE` per call | One round trip per batch; each batch atomic by construction (one statement); the count feeds metrics; same shape as `IOperationAuditStore.PurgeEntriesAsync` (`src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs:168`) | 20 store implementations with per-dialect SQL (SQL Server `DELETE TOP`, MySQL `DELETE ... LIMIT`, PostgreSQL subquery) |
| **B) Inbox-style two steps**: `GetPurgeableMessagesAsync(cutoff, batchSize)` + `RemoveMessagesAsync(ids)` | Mirrors `IInboxStore`; ids visible to the caller | Two round trips; a race between the read and the delete needs the predicate repeated in the delete; payloads are read only to be deleted (personal data loaded for nothing) |
| **C) Database-native expiry**: a MongoDB TTL index on `ProcessedAtUtc`, SQL Agent / `pg_cron` / MySQL events for the relational providers | No application loop on MongoDB | Not portable: the relational engines have no TTL, and the jobs live outside Encina (the issue rejects this as alternative 1); a MongoDB TTL index fixes the period at index creation, so a changed `MessageRetentionPeriod` needs an index rebuild |

### Chosen Option: **A — `PurgeProcessedAsync` with one bounded `DELETE` per call** (recommended, pending the maintainer)

### Rationale

- I recommend A because the purge never needs the payload, and a single predicate-guarded statement cannot delete a row that changed state between a read and a delete.
- The method returns `Left` on any provider failure (wrapped in `EitherHelpers.TryAsync` like the existing store methods), so the service never reports a failed purge as success (AGENTS.md §3).
- The bounded batch keeps lock duration and transaction-log growth small on large backlogs. The service loops until a batch deletes fewer rows than `PurgeBatchSize`.

</details>

<details>
<summary><strong>4. Retention anchor — cutoff computed at purge time from <code>ProcessedAtUtc</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Cutoff at purge time**: `ProcessedAtUtc IS NOT NULL AND ProcessedAtUtc < now - MessageRetentionPeriod`, `now` from `TimeProvider` | No schema change; a changed period applies to every row at the next cycle; pending and dead-lettered rows are excluded by the same predicate (Summary, fact 3) | Differs from the inbox, which stamps `ExpiresAtUtc` at receipt |
| **B) Stamp `ExpiresAtUtc` when the message is marked processed** (the inbox model) | Same model as the inbox; per-row expiry possible later | New column on two tables × 10 providers, new migrations and test schemas; a changed period does not apply to rows already stamped |
| **C) Anchor on `CreatedAtUtc`** | Index on the creation date is simple | A message that was retried for days could be deleted soon after delivery, and a pending message could match unless the predicate also checks `ProcessedAtUtc`; the period no longer means "after processing or execution" as REQ-018 says |

### Chosen Option: **A — cutoff at purge time from `ProcessedAtUtc`** (recommended, pending the maintainer)

### Rationale

- I recommend A because REQ-018 defines the period as "30 days after processing or execution", which is exactly `ProcessedAtUtc` on both tables, and it needs no new column.
- A message that ends its recurrence is marked processed (`SchedulerOrchestrator.cs:431,451`) and becomes purgeable; an active recurring message keeps `ProcessedAtUtc` `NULL` between executions and is never purged.
- The cutoff is computed once per cycle in the service from the injected `TimeProvider` and passed to the store, so stores never read the clock for the purge (AGENTS.md §3).

</details>

<details>
<summary><strong>5. Multi-instance coordination — no lock; idempotent bounded deletes</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No lock**: each host runs the purge; every batch is a predicate-guarded `DELETE` (PostgreSQL with `FOR UPDATE SKIP LOCKED` in the subquery) | Correct under concurrency by construction (two hosts can only delete disjoint or already-deleted rows); no new package reference; no dependency on a lock provider being registered | Redundant work when many hosts run the cycle at the same moment (bounded by the batch size and the 24 h interval) |
| **B) Optional `IDistributedLockProvider`**: take a lock named `encina:purge:{pattern}` when a provider is registered, run without it otherwise | Matches the issue text ("under a distributed lock when several hosts run"); avoids redundant work | `Encina.Messaging` references only `Encina` today (`src/Encina.Messaging/Encina.Messaging.csproj:17`); it would need a reference to `Encina.DistributedLock` or a new abstraction; two code paths to test |
| **C) Leader election for background processors (#717)** | One mechanism for every messaging loop | #717 is open and unplanned; blocks REQ-018 on it |

### Chosen Option: **A — no lock; idempotent bounded deletes** (recommended, pending the maintainer)

### Rationale

- I recommend A because the lock in the issue protects nothing here: deleting rows that match a fixed cutoff is idempotent, and a lost race only means the other host deleted the row first.
- It keeps pay-for-what-you-use: an application with one host or without a lock provider gets a correct purge with no extra registration.
- If #717 (leader election) lands, the service can run only on the leader without changing the store contract. The Cross-Cutting Integration Matrix marks distributed locks ❌ with this reasoning and points to #717.

</details>

<details>
<summary><strong>6. Tenant scope — deployment-wide retention now, tenant filter with #737/#739</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Deployment-wide purge now**: one retention period per pattern for all rows; add a tenant filter to `PurgeProcessedAsync` when #737/#739 add `TenantId` | Ships REQ-018 without waiting; no data crosses tenants (the purge reads no row and returns only a count); #1257 already owns the two-tenant purge test | AC-043's "background cycles filter by tenant" is met only once the rows carry a tenant |
| **B) Block on #737 and #739** and purge per tenant from the start | AC-043 met in the same PR | Blocks a P0 storage-limitation requirement on two open features that each touch 10 providers |
| **C) Per-tenant retention overrides** (`Func<string?, TimeSpan>` or a dictionary on the options) | Lets a tenant keep messages longer | Needs the tenant column first (same blocker as B); no SPEC-002 requirement asks for different periods per tenant |

### Chosen Option: **A — deployment-wide purge now; tenant filter with #737/#739 (verified by #1257)** (recommended, pending the maintainer)

### Rationale

- I recommend A because a deployment-wide period is the controller's policy, and a delete that returns only a count cannot read, act on or export one tenant's data for another.
- The Multi-Tenancy row of the matrix is ⏭️ and names #737, #739 and #1257. No new issue is needed: #1257 lists the purge among the background cycles of its two-tenant test.
- The activity and metrics carry no `encina.tenant_id` until rows have one. Writing a placeholder tenant would be false telemetry.

</details>

<details>
<summary><strong>7. Configuration model — options on each pattern, mirroring <code>InboxOptions</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `MessageRetentionPeriod`, `EnableAutomaticPurge`, `PurgeInterval`, `PurgeBatchSize` on `OutboxOptions` and `SchedulingOptions`** (same names and defaults as `InboxOptions.cs:27-45`), validated in the setters like `OutboxOptions.BatchSize` (`OutboxOptions.cs:23-31`) | Same vocabulary across the three patterns; the issue proposes exactly this; no new configuration object | Four properties repeated on three classes |
| **B) One shared `MessagePurgeOptions` on `MessagingConfiguration`** with per-pattern sub-objects | One place to configure retention | Inbox options would move out of `InboxOptions` (a breaking rename, acceptable pre-1.0, but churn in 8 READMEs); the stores' pattern options no longer describe the whole pattern |
| **C) Reuse `Encina.Compliance.Retention` policies** | One retention model in the product | `Encina.Messaging` would depend on a compliance package (pay-for-what-you-use broken); that package is Marten-only (ADR-019) and models data categories, not technical message tables |

### Chosen Option: **A — the four inbox options on `OutboxOptions` and `SchedulingOptions`** (recommended, pending the maintainer)

### Rationale

- I recommend A because REQ-018 asks for parity with the inbox; identical names make the parity visible in configuration.
- Defaults: `MessageRetentionPeriod = 30 days`, `EnableAutomaticPurge = true`, `PurgeInterval = 24 h`, `PurgeBatchSize = 100`. Setters reject a non-positive period, interval or batch size with `ArgumentOutOfRangeException`. The same validation is added to `InboxOptions`, which validates nothing today.
- Switching the purge off is `EnableAutomaticPurge = false`; lengthening it is a larger `MessageRetentionPeriod`. Both are the knobs REQ-018 requires.

</details>

---

## Implementation Phases

### Phase 1: Core Options & Store Contracts

> **Goal**: Retention options on the outbox and scheduling patterns, validation on the inbox options, and the purge operation on the two store interfaces.

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Messaging/Outbox/OutboxOptions.cs`**: add, with XML docs and setter validation using the `field` keyword (pattern of `BatchSize`, lines 23-31):
   - `TimeSpan MessageRetentionPeriod` = 30 days (must be > `TimeSpan.Zero`);
   - `bool EnableAutomaticPurge` = `true`;
   - `TimeSpan PurgeInterval` = 24 h (must be > `TimeSpan.Zero`);
   - `int PurgeBatchSize` = 100 (must be ≥ 1).
2. **`src/Encina.Messaging/Scheduling/SchedulingOptions.cs`**: the same four properties and defaults.
3. **`src/Encina.Messaging/Inbox/InboxOptions.cs`**: add the same setter validation to the existing `MessageRetentionPeriod`, `PurgeInterval` and `PurgeBatchSize` (lines 27, 33, 45); defaults unchanged.
4. **`src/Encina.Messaging/Outbox/IOutboxStore.cs`**: add

   ```csharp
   Task<Either<EncinaError, int>> PurgeProcessedAsync(
       DateTime processedBeforeUtc,
       int batchSize,
       CancellationToken cancellationToken = default);
   ```

   The XML contract says: deletes at most `batchSize` messages whose `ProcessedAtUtc` is set and earlier than `processedBeforeUtc`, never a message with `ProcessedAtUtc` null (pending or dead-lettered), returns the number deleted, `batchSize < 1` throws `ArgumentOutOfRangeException`, and a non-UTC `processedBeforeUtc` (`Kind == Local`) throws `ArgumentException`.
5. **`src/Encina.Messaging/Scheduling/IScheduledMessageStore.cs`**: the same method, with the scheduled-message wording: executed one-shot messages and recurring messages whose recurrence ended; never an active recurring message (its `ProcessedAtUtc` is null between executions).
6. **`src/Encina.Messaging/PublicAPI.Unshipped.txt`**: the eight new option members and the two interface methods.
7. **Shared argument guard**: one `internal static` helper in `Encina.Messaging` (for example `PurgeArguments.Validate(DateTime, int)`) used by every store, exposed to the provider packages through the existing `InternalsVisibleTo` entries, or a public `StoreValidationMessages` constant pair if those packages are not covered. No per-store copy.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #1200 in Encina (.NET 10, C# 14): retention options and the purge contract.

CONTEXT:
- InboxOptions (src/Encina.Messaging/Inbox/InboxOptions.cs:27-45) has MessageRetentionPeriod (30 days),
  PurgeInterval (24 h), EnableAutomaticPurge (true), PurgeBatchSize (100), with no validation.
- OutboxOptions (src/Encina.Messaging/Outbox/OutboxOptions.cs) validates in setters with the field keyword
  (BatchSize, lines 23-31). SchedulingOptions has no retention settings.
- IOutboxStore and IScheduledMessageStore have no purge. Pending and dead-lettered rows have ProcessedAtUtc
  null; processed rows have it set; active recurring scheduled messages are reset to null on reschedule.

TASK:
1. Add MessageRetentionPeriod, EnableAutomaticPurge, PurgeInterval, PurgeBatchSize (same defaults as the inbox)
   to OutboxOptions and SchedulingOptions, validated in the setters (period and interval > 0, batch >= 1).
2. Add the same setter validation to InboxOptions without changing its defaults.
3. Add Task<Either<EncinaError, int>> PurgeProcessedAsync(DateTime processedBeforeUtc, int batchSize,
   CancellationToken cancellationToken = default) to IOutboxStore and IScheduledMessageStore, with a complete
   XML contract (what is deleted, what is never deleted, return value, argument exceptions).
4. Add one internal argument guard shared by all stores.
5. Update PublicAPI.Unshipped.txt.

KEY RULES:
- Pre-1.0: no [Obsolete], no compatibility overloads; the interfaces simply gain the method.
- XML docs on every new public member, with an <example> on the options.
- Do not implement any store yet; the solution will not build until Phases 3-7 add the method everywhere,
  so commit Phases 1 and 3-7 together or keep this phase on a branch until then.

REFERENCE FILES:
- src/Encina.Messaging/Inbox/InboxOptions.cs
- src/Encina.Messaging/Outbox/OutboxOptions.cs
- src/Encina.Messaging/Outbox/IOutboxStore.cs
- src/Encina.Messaging/Scheduling/IScheduledMessageStore.cs
- src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs (PurgeEntriesAsync, line 168)
```

</details>

---

### Phase 2: Purge Service & DI

> **Goal**: One hosted service that runs the outbox, scheduled-message and inbox purge loops, registered only when a pattern has purge on.

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Messaging/Purge/MessagePurgeService.cs`** (new): `public sealed class MessagePurgeService : BackgroundService`.
   - Constructor: `(IServiceScopeFactory scopeFactory, MessagePurgeSchedule schedule, ILogger<MessagePurgeService> logger, TimeProvider? timeProvider = null)`.
   - `ExecuteAsync` runs one independent loop per enabled pattern (`Task.WhenAll`). Each loop waits with `Task.Delay(interval, timeProvider, ct)` and catches every exception except cancellation, logging it through `ForLogging()` like `OperationAuditRetentionService.cs:143-150`.
   - Each loop also runs once at start-up. The first delay is a small fixed jitter, so hosts started together do not all purge in the same second.
2. **`src/Encina.Messaging/Purge/MessagePurgeSchedule.cs`** (new, `internal sealed record`). It is built at registration from the three option objects:
   - `OutboxPurge`, `SchedulingPurge` and `InboxPurge` entries;
   - each entry holds `(TimeSpan Retention, TimeSpan Interval, int BatchSize)`, or null when that pattern is disabled or `EnableAutomaticPurge` is false.
3. **`src/Encina.Messaging/Purge/MessagePurgeCycle.cs`** (new, `internal static`): small methods so that each stays under CRAP 10.
   - `PurgeOutboxAsync(IOutboxStore, DateTime cutoff, int batchSize, ct)` and `PurgeScheduledAsync(...)` loop `PurgeProcessedAsync` until a batch returns fewer than `batchSize` or `Left`. They return `Either<EncinaError, int>` (the total), and the first `Left` stops the cycle and is returned.
   - `PurgeInboxAsync(IInboxStore, int batchSize, ct)` loops `GetExpiredMessagesAsync` + `RemoveExpiredMessagesAsync` with the same rules.
   - The cutoff is `timeProvider.GetUtcNow().UtcDateTime - retention`, computed once per cycle in the service.
4. **`src/Encina.Messaging/MessagingServiceCollectionExtensions.cs`**:
   - `RegisterOutbox` (line 253), `RegisterInbox` (line 273) and `RegisterScheduling` (line 313) stay as they are.
   - A new `RegisterMessagePurge(services, useOutbox, outboxOptions, useInbox, inboxOptions, useScheduling, schedulingOptions)` builds the schedule. It adds `MessagePurgeService` with `TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, MessagePurgeService>())` only when at least one entry is non-null.
   - Call it from `AddOutboxInboxSagaSchedulingServices` (line 203) and from `AddMessagingServicesCore` (line 358), which has no scheduling. Pass `useScheduling: false` there.
5. **No change to `OutboxProcessorBase` or `ScheduledMessageProcessor`**: the purge is independent of `EnableProcessor` (Design Choice 1).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #1200 in Encina: the MessagePurgeService hosted service and its DI.

CONTEXT:
- Phase 1 added retention options to OutboxOptions and SchedulingOptions (MessageRetentionPeriod,
  EnableAutomaticPurge, PurgeInterval, PurgeBatchSize) and PurgeProcessedAsync(processedBeforeUtc, batchSize, ct)
  -> Either<EncinaError, int> to IOutboxStore and IScheduledMessageStore.
- IInboxStore already has GetExpiredMessagesAsync(batchSize) and RemoveExpiredMessagesAsync(ids); nothing calls
  them in production today, and InboxOptions.EnableAutomaticPurge/PurgeInterval/PurgeBatchSize are unused.
- Reference pattern: src/Encina.Security.Audit/OperationAuditRetentionService.cs (scope per run, TimeProvider
  delay, Left logged by error code, exceptions logged with ForLogging()).
- Stores are scoped; MessagingServiceCollectionExtensions registers them in RegisterOutbox/RegisterInbox/
  RegisterScheduling (lines 253, 273, 313).

TASK:
1. Create src/Encina.Messaging/Purge/MessagePurgeService.cs (BackgroundService) with one loop per enabled
   pattern, each creating an async scope per run, resolving the store, computing the cutoff once from
   TimeProvider and calling MessagePurgeCycle.
2. Create MessagePurgeCycle (internal static) with PurgeOutboxAsync, PurgeScheduledAsync, PurgeInboxAsync that
   loop batches until a short batch or the first Left, and return the total or the Left.
3. Create MessagePurgeSchedule (internal record) built from the three options at registration.
4. Add RegisterMessagePurge to MessagingServiceCollectionExtensions; call it from
   AddOutboxInboxSagaSchedulingServices and AddMessagingServicesCore; register the service only when at least
   one pattern is enabled with EnableAutomaticPurge = true.

KEY RULES:
- A Left from a store fails that cycle: log the error code (never EncinaError.Message), record the failure
  metric, do not report success (AGENTS.md section 3).
- Time only from the injected TimeProvider; Task.Delay(interval, timeProvider, ct).
- Every method under CRAP 10: keep the loop body, the batch loop and the logging in separate small methods.
- Prove the registration with a DI test that builds the provider with ValidateOnBuild and ValidateScopes.

REFERENCE FILES:
- src/Encina.Security.Audit/OperationAuditRetentionService.cs
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs
- src/Encina.Messaging/Outbox/OutboxProcessorBase.cs (scope creation, error-code logging)
- src/Encina.Messaging/Inbox/IInboxStore.cs
```

</details>

---

### Phase 3: ADO.NET Providers (SqlServer, PostgreSQL, MySQL)

> **Goal**: `PurgeProcessedAsync` on the three ADO.NET outbox and scheduled-message stores, plus the scheduled-message index.

<details>
<summary><strong>Tasks</strong></summary>

For each of `src/Encina.ADO.{SqlServer,PostgreSQL,MySQL}/`:

1. **`Outbox/OutboxStoreADO.cs`** and **`Scheduling/ScheduledMessageStoreADO.cs`**: implement `PurgeProcessedAsync` inside `EitherHelpers.TryAsync(..., "outbox.purge_failed")` / `"scheduling.purge_failed"`, with the dialect's bounded delete:
   - SQL Server: `DELETE TOP (@BatchSize) FROM {table} WHERE ProcessedAtUtc IS NOT NULL AND ProcessedAtUtc < @ProcessedBeforeUtc`
   - PostgreSQL: `DELETE FROM {table} WHERE id IN (SELECT id FROM {table} WHERE processedatutc IS NOT NULL AND processedatutc < @ProcessedBeforeUtc ORDER BY processedatutc LIMIT @BatchSize FOR UPDATE SKIP LOCKED)`
   - MySQL: ``DELETE FROM `{table}` WHERE ProcessedAtUtc IS NOT NULL AND ProcessedAtUtc < @ProcessedBeforeUtc ORDER BY ProcessedAtUtc LIMIT @BatchSize``
   - Return `ExecuteNonQueryAsync`'s affected-row count. Use the asynchronous command path with the `CancellationToken`.
2. **Connection handling**: if #718 has landed, use its enlistment helper and its real `OpenAsync`. Otherwise, open a closed connection with `DbConnection.OpenAsync(ct)` in the new method rather than copying the no-op `OpenConnectionAsync` (`OutboxStoreADO.cs:340-345`; #1170/#1868).
3. **`Scripts/004_CreateScheduledMessagesTable.sql`**: add `IX_ScheduledMessages_ProcessedAtUtc` on `(ProcessedAtUtc)`. On PostgreSQL make it a partial index, `WHERE processedatutc IS NOT NULL`. The outbox script already has `IX_OutboxMessages_ProcessedAt_RetryCount` leading with `ProcessedAtUtc`, so nothing changes there.
4. **`PublicAPI.Unshipped.txt`** of each package: the two new public methods.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #1200 in Encina: PurgeProcessedAsync on the three ADO.NET providers.

CONTEXT:
- Phase 1 added PurgeProcessedAsync(DateTime processedBeforeUtc, int batchSize, CancellationToken) ->
  Either<EncinaError, int> to IOutboxStore and IScheduledMessageStore.
- ADO stores build SQL text per dialect (src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs) and wrap each call in
  EitherHelpers.TryAsync with an error code. Their OpenConnectionAsync is a no-op (#1170/#1868); #718 may already
  have replaced it with an enlistment helper and OpenAsync.
- The outbox scripts already index ProcessedAtUtc first; the scheduled scripts index ScheduledAtUtc first.

TASK:
For SqlServer, PostgreSQL and MySQL: implement PurgeProcessedAsync on OutboxStoreADO and ScheduledMessageStoreADO
with one bounded DELETE (SQL Server DELETE TOP, PostgreSQL id IN (SELECT ... LIMIT ... FOR UPDATE SKIP LOCKED),
MySQL DELETE ... ORDER BY ... LIMIT); return the affected rows; add IX_ScheduledMessages_ProcessedAtUtc to the
three 004 scripts (partial on PostgreSQL); update PublicAPI.Unshipped.txt.

KEY RULES:
- The predicate is exactly "ProcessedAtUtc IS NOT NULL AND ProcessedAtUtc < @ProcessedBeforeUtc"; never filter on
  RetryCount or IsRecurring.
- Async only with CancellationToken; never IDbConnection.Open() or synchronous Execute*.
- Parameters only; the table name goes through SqlIdentifierValidator as in the existing constructor.
- All three providers in the same change.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs (RequeueExhaustedAsync for the command and transaction style)
- src/Encina.ADO.PostgreSQL/Scheduling/ScheduledMessageStoreADO.cs
- src/Encina.ADO.MySQL/Scripts/004_CreateScheduledMessagesTable.sql
```

</details>

---

### Phase 4: Dapper Providers (SqlServer, PostgreSQL, MySQL)

> **Goal**: The same operation and index on the three Dapper packages.

<details>
<summary><strong>Tasks</strong></summary>

For each of `src/Encina.Dapper.{SqlServer,PostgreSQL,MySQL}/`:

1. **`Outbox/OutboxStoreDapper.cs`** and **`Scheduling/ScheduledMessageStoreDapper.cs`**: `PurgeProcessedAsync` with the SQL of Phase 3. Run it through `ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: ct))` so the token reaches the driver, and return the affected rows.
2. **`Scripts/004_CreateScheduledMessagesTable.sql`**: the same `IX_ScheduledMessages_ProcessedAtUtc`.
3. **`PublicAPI.Unshipped.txt`**: the two methods.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #1200 in Encina: PurgeProcessedAsync on the three Dapper providers.

CONTEXT:
- Phase 3 implemented the same operation on ADO.NET with one bounded DELETE per dialect.
- Dapper stores call ExecuteAsync/QueryAsync on the scoped IDbConnection (src/Encina.Dapper.SqlServer/Outbox/
  OutboxStoreDapper.cs). #718 may already pass a transaction from its accessor.

TASK:
For SqlServer, PostgreSQL and MySQL: implement PurgeProcessedAsync on OutboxStoreDapper and
ScheduledMessageStoreDapper with the Phase 3 SQL through CommandDefinition (with the CancellationToken); add the
ProcessedAtUtc index to the three 004 scripts; update PublicAPI.Unshipped.txt.

KEY RULES:
- Same predicate and same dialect SQL as Phase 3; no RetryCount or IsRecurring filter.
- CancellationToken reaches the driver through CommandDefinition.
- Wrap in EitherHelpers.TryAsync with "outbox.purge_failed" / "scheduling.purge_failed".

REFERENCE FILES:
- src/Encina.Dapper.SqlServer/Outbox/OutboxStoreDapper.cs
- src/Encina.Dapper.PostgreSQL/Scheduling/ScheduledMessageStoreDapper.cs
- src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs (Phase 3 result)
```

</details>

---

### Phase 5: EF Core Providers (SqlServer, PostgreSQL, MySQL)

> **Goal**: The operation on `OutboxStoreEF` and `ScheduledMessageStoreEF`, translated by each EF provider.

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.EntityFrameworkCore/Outbox/OutboxStoreEF.cs`**: implement `PurgeProcessedAsync` as

   ```csharp
   _dbContext.Set<OutboxMessage>()
       .Where(m => m.ProcessedAtUtc != null && m.ProcessedAtUtc < processedBeforeUtc)
       .OrderBy(m => m.ProcessedAtUtc)
       .Take(batchSize)
       .ExecuteDeleteAsync(cancellationToken)
   ```

   - `ExecuteDeleteAsync` bypasses the change tracker and `SaveChangesAsync`, so the purge commits on its own. The service runs it in its own scope.
   - The XML remarks of the method say so.
2. **`src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessageStoreEF.cs`**: the same over `ScheduledMessage`.
3. **Translation check on the three EF providers** (Research, open question 1):
   - SQL Server translates `Take` + `ExecuteDelete` to `DELETE TOP`;
   - Npgsql translates it to a subquery;
   - Pomelo `9.0.0` (`Directory.Packages.props`) must be confirmed on .NET 10. If it refuses `Take` in `ExecuteDelete`, select the batch's ids and run `ExecuteDeleteAsync` on `Where(m => ids.Contains(m.Id) && m.ProcessedAtUtc < processedBeforeUtc)`. The predicate is repeated so that no row that changed state can be deleted.
4. **Indexes**: none. `OutboxMessageConfiguration.cs:42` and `ScheduledMessageConfiguration.cs:63` already lead with `ProcessedAtUtc`.
5. **`src/Encina.EntityFrameworkCore/PublicAPI.Unshipped.txt`**.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #1200 in Encina: PurgeProcessedAsync on the EF Core stores (SqlServer,
PostgreSQL, MySQL through one package).

CONTEXT:
- OutboxStoreEF and ScheduledMessageStoreEF use the scoped DbContext (src/Encina.EntityFrameworkCore/Outbox/
  OutboxStoreEF.cs:38). Indexes on ProcessedAtUtc already exist (OutboxMessageConfiguration.cs:42,
  ScheduledMessageConfiguration.cs:63).
- EF Core 10 ExecuteDeleteAsync with Take translates on SqlServer and Npgsql; Pomelo 9.0.0 on .NET 10 must be
  verified by an integration test.

TASK:
Implement PurgeProcessedAsync on both stores with Where(ProcessedAtUtc != null && ProcessedAtUtc < cutoff)
.OrderBy(ProcessedAtUtc).Take(batchSize).ExecuteDeleteAsync(ct); if Pomelo refuses the translation, fall back to
selecting ids then ExecuteDeleteAsync with the predicate repeated; document in XML remarks that the purge does not
go through SaveChangesAsync; update PublicAPI.Unshipped.txt.

KEY RULES:
- Wrap in EitherHelpers.TryAsync (or the store's existing Left pattern); provider exceptions become Left.
- No new index and no migration.
- The same integration test runs on the three EF collections (EFCore-SqlServer, EFCore-PostgreSQL, EFCore-MySQL).

REFERENCE FILES:
- src/Encina.EntityFrameworkCore/Outbox/OutboxStoreEF.cs (RequeueExhaustedAsync, lines 155-196)
- src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessageStoreEF.cs
- Directory.Packages.props (Pomelo.EntityFrameworkCore.MySql version)
```

</details>

---

### Phase 6: MongoDB Provider

> **Goal**: The operation on `OutboxStoreMongoDB` and `ScheduledMessageStoreMongoDB` with a bounded batch.

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.MongoDB/Outbox/OutboxStoreMongoDB.cs`**: `PurgeProcessedAsync`.
   - Find at most `batchSize` ids with `Filter.And(Filter.Ne(m => m.ProcessedAtUtc, null), Filter.Lt(m => m.ProcessedAtUtc, processedBeforeUtc))`, sorted by `ProcessedAtUtc` and projected to `Id` only, so no payload is read.
   - Then `DeleteManyAsync(Filter.And(Filter.In(m => m.Id, ids), <same predicate>), ct)` and return `DeletedCount`.
2. **`src/Encina.MongoDB/Scheduling/ScheduledMessageStoreMongoDB.cs`**: the same.
3. **Indexes**: none. `IX_Outbox_Pending` and `IX_Scheduled_Due` (`MongoDbIndexCreator.cs:73-82,155-163`) lead with `ProcessedAtUtc`. No TTL index (Design Choice 3).
4. **`src/Encina.MongoDB/PublicAPI.Unshipped.txt`**.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of issue #1200 in Encina: PurgeProcessedAsync on the MongoDB stores.

CONTEXT:
- OutboxStoreMongoDB and ScheduledMessageStoreMongoDB use typed Builders filters (src/Encina.MongoDB/Outbox/
  OutboxStoreMongoDB.cs:76-81). DeleteMany has no limit, so batching needs an id query first.
- Indexes leading with ProcessedAtUtc exist (MongoDbIndexCreator.cs:73-82, 155-163).

TASK:
Implement PurgeProcessedAsync on both stores: find up to batchSize ids (projection to Id, sort by ProcessedAtUtc)
matching ProcessedAtUtc != null && ProcessedAtUtc < cutoff, then DeleteManyAsync on In(ids) AND the same
predicate; return DeletedCount; update PublicAPI.Unshipped.txt.

KEY RULES:
- Repeat the predicate in the delete filter so a row that changed state is never deleted.
- Never read Content (payloads) during the purge.
- CancellationToken on every driver call; driver exceptions become Left.

REFERENCE FILES:
- src/Encina.MongoDB/Outbox/OutboxStoreMongoDB.cs (RequeueExhaustedAsync, lines 180-209)
- src/Encina.MongoDB/Scheduling/ScheduledMessageStoreMongoDB.cs
- src/Encina.MongoDB/MongoDbIndexCreator.cs
```

</details>

---

### Phase 7: Decorators & Fakes

> **Goal**: Every other implementation of the two interfaces gains the method, so the solution builds and tests can use it.

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.OpenTelemetry/MessagingStores/InstrumentedOutboxStore.cs`** (line 22) and **`InstrumentedScheduledMessageStore.cs`** (line 23): delegate `PurgeProcessedAsync` to the inner store inside the decorator's existing timing/activity wrapper. They record the operation name `purge_processed` and the outcome, never ids or payloads.
2. **`src/Encina.Testing.Fakes/Stores/FakeOutboxStore.cs`** (line 27) and **`FakeScheduledMessageStore.cs`** (line 19): in-memory implementation with the same predicate and batch bound, so applications can test their retention configuration.
3. **Test doubles in `tests/`**: `tests/Encina.UnitTests/Messaging/Pipeline/OutboxPostProcessorTests.cs:401` and `tests/Encina.UnitTests/Messaging/Scheduling/SchedulerOrchestratorTests.cs:751,763` implement the interfaces and gain the method. If #718 Phase 9 removed `OutboxPostProcessorTests` first, skip that file.
4. **`PublicAPI.Unshipped.txt`** of `Encina.OpenTelemetry` and `Encina.Testing.Fakes`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of issue #1200 in Encina: the decorators and fakes of IOutboxStore and
IScheduledMessageStore gain PurgeProcessedAsync.

CONTEXT:
- Encina.OpenTelemetry wraps the stores in InstrumentedOutboxStore and InstrumentedScheduledMessageStore
  (src/Encina.OpenTelemetry/MessagingStores/). Encina.Testing.Fakes ships FakeOutboxStore and
  FakeScheduledMessageStore (src/Encina.Testing.Fakes/Stores/).
- Two unit-test files implement the interfaces as hand-written doubles.

TASK:
Add PurgeProcessedAsync to the two decorators (delegate and instrument like the other methods), to the two fakes
(in-memory, same predicate and batch bound as the real stores) and to the test doubles; update both packages'
PublicAPI.Unshipped.txt.

KEY RULES:
- Decorators never tag ids, payloads or EncinaError.Message; error code only.
- The fakes honour the contract exactly: never delete a message with ProcessedAtUtc null; at most batchSize.

REFERENCE FILES:
- src/Encina.OpenTelemetry/MessagingStores/InstrumentedOutboxStore.cs
- src/Encina.OpenTelemetry/MessagingStores/InstrumentedInboxStore.cs (RemoveExpiredMessagesAsync, line 117)
- src/Encina.Testing.Fakes/Stores/FakeOutboxStore.cs
- src/Encina.Testing.Fakes/Stores/FakeInboxStore.cs (RemoveExpiredMessagesAsync, line 176)
```

</details>

---

### Phase 8: Cross-Cutting Integration

> **Goal**: Apply the matrix decisions: validation and transactions now, tenancy and leader election left to their issues.

<details>
<summary><strong>Tasks</strong></summary>

1. **Validation (✅)**: the setter validation of Phase 1, plus a start-up check in `RegisterMessagePurge`. If `PurgeInterval` is longer than `MessageRetentionPeriod`, messages can outlive the period by up to one interval. Log this once at start-up as a warning (an EventId of Phase 9); it is not an error.
2. **Transactions (✅)**: each batch is one statement and therefore atomic. The service runs in its own scope, outside any request transaction. Document in `MessagePurgeService`'s remarks that a purge never joins an ambient transaction. After #718, the accessor's `Current` is null in that scope.
3. **Multi-tenancy (⏭️)**: no code. Add a `// Tenant filter: #737 (outbox), #739 (scheduled); two-tenant test: #1257` remark on both `PurgeProcessedAsync` contracts, and mention it in the feature page.
4. **Distributed locks (❌)**: no code. Remark on `MessagePurgeService` that concurrent hosts are safe by construction, and that leader election, if wanted, is #717.
5. **Health checks (❌)**: no change. `OutboxHealthCheck`, `SchedulingHealthCheck` and `InboxHealthCheck` already probe the stores. The purge adds no new dependency.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 of issue #1200 in Encina: cross-cutting integration of the purge.

CONTEXT:
- Phases 1-7 added the options, the store operation on 10 providers, MessagePurgeService and its DI.
- Matrix decisions: Validation and Transactions included; Multi-tenancy deferred to #737/#739/#1257; Distributed
  locks not applicable (idempotent deletes; leader election is #717); Health checks not applicable.

TASK:
1. Add the start-up warning when PurgeInterval > MessageRetentionPeriod for any enabled pattern.
2. Document in XML remarks: purge runs outside any ambient transaction, each batch atomic; concurrent hosts are
   safe; the tenant filter arrives with #737/#739.
3. No lock, no tenant parameter, no new health check.

KEY RULES:
- Log through [LoggerMessage] with the EventIds of Phase 9 only.
- Never add a placeholder tenant id to logs, tags or metrics.

REFERENCE FILES:
- src/Encina.Messaging/Purge/MessagePurgeService.cs
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs
- src/Encina.Messaging/Health/OutboxHealthCheck.cs
```

</details>

---

### Phase 9: Observability

> **Goal**: An activity per purge cycle, counters for purged messages and failures, and `[LoggerMessage]` logs in a registered range.

<details>
<summary><strong>Tasks</strong></summary>

1. **Activities**: add `StartPurge(int batchSize)` and `CompletePurge(Activity?, int purgedCount)` to the existing sources:
   - `OutboxActivitySource` (`"Encina.Messaging.Outbox"`, `src/Encina.Messaging/Diagnostics/OutboxActivitySource.cs:21`) as `encina.outbox.purge`;
   - `SchedulingActivitySource` (`SchedulingActivitySource.cs:21`) as `encina.scheduling.purge`;
   - `InboxActivitySource` (`"Encina.Messaging.Inbox"`) as `encina.inbox.purge`.
   - Tags: `encina.purge.batch_size`, `encina.purge.purged_count`, and `encina.error_code` on failure. No tenant tag until #737/#739 (Design Choice 6).
2. **`src/Encina.Messaging/Diagnostics/MessagePurgeMetrics.cs`** (new) on the shared `Meter("Encina", "1.0")` used by `OutboxProcessorMetrics.cs:48` and `SchedulingProcessorMetrics.cs:26`:
   - `encina.outbox.messages_purged_total`, `encina.scheduling.messages_purged_total` and `encina.inbox.messages_purged_total` (`Counter<long>`);
   - `encina.messaging.purge_failures_total` (`Counter<long>`, tags `encina.messaging.pattern` and `encina.error_code`).
3. **`src/Encina.Messaging/Diagnostics/MessagePurgeLog.cs`** (new, `[LoggerMessage]`), EventIds **2970-2976** in `EventIdRanges.Messaging` (2800-2999):
   - 2970 `PurgeServiceStarted` (patterns, intervals);
   - 2971 `PurgeServiceDisabled`;
   - 2972 `PurgeCompleted` (pattern, count);
   - 2973 `PurgeFailed` (pattern, error code);
   - 2974 `PurgeCycleError` (pattern, exception through `ForLogging()`);
   - 2975 `PurgeIntervalExceedsRetention` (pattern);
   - 2976 `PurgeServiceStopped`.
   - XML doc: `/// Event IDs: 2970-2976 (see EventIdRanges.Messaging)`.
4. **EventId coordination**: the highest Messaging EventId on `main` is 2962. The #718 plan reserves 2963-2969, and this plan takes 2970-2976. The #1251 plan, written in parallel, must start after 2976 or use the pattern store ranges. Re-check the registry when implementation starts. `Encina.Messaging` is already mapped in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`, so no new assembly entry is needed.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
You are implementing Phase 9 of issue #1200 in Encina: observability of the purge.

CONTEXT:
- Activity sources exist per pattern: OutboxActivitySource ("Encina.Messaging.Outbox"), SchedulingActivitySource
  ("Encina.Messaging.Scheduling"), InboxActivitySource ("Encina.Messaging.Inbox") in src/Encina.Messaging/
  Diagnostics/. Processor metrics use Meter("Encina", "1.0").
- EventIdRanges.Messaging is 2800-2999; main uses up to 2962; the #718 plan reserves 2963-2969.

TASK:
1. Add StartPurge/CompletePurge (encina.outbox.purge, encina.scheduling.purge, encina.inbox.purge) to the three
   activity sources, with batch size, purged count and error code tags.
2. Create MessagePurgeMetrics with the three *_messages_purged_total counters and purge_failures_total.
3. Create MessagePurgeLog with [LoggerMessage] EventIds 2970-2976, packed, documented with the range.
4. Call them from MessagePurgeService and MessagePurgeCycle.
5. Run the architecture tests (EventIdUniquenessRule, EncinaEventIdAllocationTests).

KEY RULES:
- No payload, message id, EncinaError.Message or subject identifier in any tag, metric or log; error code only.
- No tenant attribute until rows carry a tenant (#737/#739).
- Re-read EventIdRanges.cs and grep Encina.Messaging for 2963-2976 before using the ids.

REFERENCE FILES:
- src/Encina.Messaging/Diagnostics/OutboxActivitySource.cs
- src/Encina.Messaging/Diagnostics/OutboxProcessorMetrics.cs
- src/Encina.Messaging/Diagnostics/SchedulingProcessorLog.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

### Phase 10: Testing

> **Goal**: Every flag of the touched files reaches its manifest target; AC-018 is proven on the 10 providers.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit tests** (`tests/Encina.UnitTests/Messaging/Purge/`):
   - `MessagePurgeCycle`: batch loop termination, total, first `Left` stops the cycle.
   - `MessagePurgeService` with `FakeTimeProvider`: the cutoff equals `now - retention`; a disabled pattern runs no loop; a `Left` logs only the error code and increments the failure counter; an exception logs through `ForLogging()` and the loop continues.
   - Options validation, and `RegisterMessagePurge` (service present or absent for each flag combination).
   - Unit tests for the fakes and decorators.
2. **Guard tests** (`tests/Encina.GuardTests/`): `batchSize < 1` and a `Local` cutoff on each of the 20 store implementations, both decorators and both fakes; null arguments of the `MessagePurgeService` constructor; the options setters.
3. **Contract tests** (`tests/Encina.ContractTests/Messaging/Purge/`): one abstract `PurgeProcessedContract<TStore>` run against the two fakes, plus the abstract base the integration tests reuse. Pending, dead-lettered and active recurring rows survive; processed rows older than the cutoff go; rows newer than the cutoff stay; at most `batchSize` per call.
4. **Property tests** (`tests/Encina.PropertyTests/Messaging/Purge/`): FsCheck over random mixes of pending, dead-lettered, processed (random ages) and recurring rows on the fake stores. Invariants: no row with `ProcessedAtUtc == null` is ever deleted; every deleted row has `ProcessedAtUtc < cutoff`; repeated purges converge to the same set (idempotency).
5. **Integration tests** (real databases, shared collections):
   - Outbox and scheduled purge tests in the existing files of the 10 providers, listed under "Existing Encina Infrastructure":
     - `tests/Encina.IntegrationTests/ADO/{SqlServer,PostgreSQL,MySQL}/{Outbox,Scheduling}/`;
     - `Dapper/...`;
     - `Infrastructure/EntityFrameworkCore/{SqlServer,PostgreSQL,MySQL}/...`;
     - `Infrastructure/MongoDB/Stores/`.
   - Each one seeds pending, dead-lettered, a 31-day-old processed row, a 29-day-old processed row and an active recurring row, purges with the 30-day default cutoff, and asserts AC-018. It also checks the batch bound with `batchSize = 2` over five old rows.
   - One `MessagePurgeService` end-to-end test per family (ADO SqlServer, Dapper PostgreSQL, EF MySQL, MongoDB) with a `FakeTimeProvider` advanced past the retention period.
   - `[Collection("<Family>-<Database>")]` fixtures, `ClearAllDataAsync()` in `InitializeAsync`. Add `IX_ScheduledMessages_ProcessedAtUtc` to `tests/Encina.TestInfrastructure/Schemas/{SqlServer,PostgreSql,MySql}Schema.cs`; the scheduled indexes there lead with `ScheduledAtUtc`, for example `SqlServerSchema.cs:118-119`.
   - Extend `tests/Encina.IntegrationTests/SchemaScripts/*SchemaScriptsIntegrationTests.cs` to cover the new index.
6. **Instrumentation test (AC-044)**: with an in-memory exporter, a purge cycle emits `encina.outbox.purge` and `encina.outbox.messages_purged_total`. A test asserts that no tag, metric dimension or log message carries a message id, payload or `EncinaError.Message`.
7. **DI test**: `ValidateOnBuild` and `ValidateScopes` with the outbox, inbox and scheduling enabled, for one provider per family.
8. **Load tests**: justification `tests/Encina.LoadTests/Messaging/Purge/MessagePurge.md` (no concurrent hot path; concurrency safety is covered by the PostgreSQL `SKIP LOCKED` integration test and the idempotency property).
9. **Benchmarks**: justification `tests/Encina.BenchmarkTests/Encina.Benchmarks/Messaging/Purge/MessagePurge.md` (one statement every 24 h; not a hot path).
10. **Manifests**: per-file targets with one-sentence justifications for every new or touched `src/` file:
    - in the existing `.github/coverage-manifest/Encina.Messaging.json`, `Encina.EntityFrameworkCore.json`, `Encina.MongoDB.json`, `Encina.ADO.SqlServer.json`, `Encina.OpenTelemetry.json` and `Encina.Testing.Fakes.json`;
    - in new manifests for the ADO.PostgreSQL, ADO.MySQL and three Dapper packages if they still have none when work starts.
    - Then run `coverage-report.cs --check-justifications` and the local CRAP table.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 10</strong></summary>

```
You are implementing Phase 10 of issue #1200 in Encina: tests for the processed-message purge.

CONTEXT:
- Phases 1-9 added PurgeProcessedAsync on IOutboxStore and IScheduledMessageStore (10 providers, 2 decorators,
  2 fakes), MessagePurgeService (outbox, scheduled, inbox loops) and its diagnostics.
- AC-018: on the 10 providers the purge deletes processed/executed messages older than the period (30-day
  default) and keeps pending, dead-lettered and active recurring messages.

TASK:
Write unit, guard, contract, property and integration tests as listed in the Phase 10 Tasks; add the scheduled
ProcessedAtUtc index to the three test schemas; write the load and benchmark justification files; add per-file
targets and justifications to the coverage manifests; measure each flag and the CRAP of changed methods.

KEY RULES:
- Tests execute real code (no reflection-only tests); Shouldly through Encina.Testing.Shouldly; FakeTimeProvider,
  never Thread.Sleep or wall-clock dates.
- Integration tests use the shared [Collection("<Family>-<Database>")] fixtures, never IClassFixture or new
  fixtures, and call ClearAllDataAsync in InitializeAsync.
- Every applicable flag reaches its target in .github/coverage-manifest/; CRAP <= 10 on every changed method.

REFERENCE FILES:
- tests/Encina.IntegrationTests/ADO/SqlServer/Outbox/OutboxStoreADOTests.cs
- tests/Encina.PropertyTests/Messaging/Outbox/OutboxExhaustionPropertyTests.cs
- tests/Encina.UnitTests/Messaging/Outbox/OutboxProcessorBaseTests.cs
- tests/Encina.TestInfrastructure/Schemas/SqlServerSchema.cs
- docs/testing/coverage-measurement-methodology.md
```

</details>

---

### Phase 11: Documentation & Finalization

> **Goal**: Documentation that matches the code, changelog fragment, zero warnings, all flags green.

<details>
<summary><strong>Tasks</strong></summary>

1. **XML documentation** on every new public member (options with `<example>`, the two interface methods with the full contract, `MessagePurgeService` remarks on transactions, concurrency and tenancy).
2. **`changelog.d/1200-processed-message-purge.added.md`**: outbox and scheduled-message purge with a 30-day default on the 10 providers, and the inbox purge options now honoured.
3. **Package READMEs**:
   - `src/Encina.Messaging/README.md`: the retention section for the three patterns.
   - The eight provider READMEs: replace the inbox purge snippets that described a non-running purge (for example `src/Encina.EntityFrameworkCore/README.md:216-219,783-792`) with the real behavior, and add the outbox and scheduling options.
4. **Feature documentation** (Diátaxis how-to, `encina-docs` skill): `docs/features/messaging-retention.md`, covering:
   - defaults, switching off, lengthening;
   - what is never purged;
   - the CDC caveat until the CDC issue is fixed;
   - a link to SPEC-002 REQ-018.
   - Add a cross-link from `docs/features/scheduling.md`.
5. **ADR**: not required. The decisions are local to the messaging patterns and recorded in this plan; the maintainer may ask for one if Design Choice 1 or 5 is contested.
6. **`docs/INVENTORY.md`**: the new `Purge/` folder and diagnostics files.
7. **`PublicAPI.Unshipped.txt`**: verify the entries of Phases 1, 3-7 (RS0016/RS0017 clean).
8. **`ROADMAP.md` / `docs/releases/`**: tick REQ-018 / P-12 where the SPEC-002 progress is tracked; no new milestone.
9. **Build verification**: `dotnet build Encina.slnx --configuration Release`: 0 errors, 0 warnings.
10. **Test verification**: `dotnet test` all pass. Every coverage flag (unit, guard, contract, property, integration) reaches its own target in `.github/coverage-manifest/{Package}.json`; record the per-file measurement and the CRAP table in the PR together with the ADR-018 matrix of this plan.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 11</strong></summary>

```
You are implementing Phase 11 of issue #1200 in Encina: documentation and finalization.

CONTEXT:
- The purge is implemented and tested on the 10 providers; the inbox purge options now run.
- Several provider READMEs document an inbox purge that never ran before this change.

TASK:
Complete XML docs; add changelog.d/1200-processed-message-purge.added.md; update the Messaging README and the
eight provider READMEs; write docs/features/messaging-retention.md (how-to) with the encina-docs skill and link it
from docs/features/scheduling.md; update docs/INVENTORY.md; verify PublicAPI files; build with zero warnings; run
every test flag and record per-file coverage and CRAP in the PR.

KEY RULES:
- English only; never edit the [Unreleased] section of CHANGELOG.md.
- No hand-typed coverage figures in docs: use covref markers (SPEC-001).
- The feature page names what is never purged (pending, dead-lettered, active recurring) and the CDC caveat.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- changelog.d/README.md
- src/Encina.EntityFrameworkCore/README.md
- docs/features/scheduling.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Requirement | Relevance |
|--------|-------------|-----------|
| GDPR Art. 5(1)(e) | Storage limitation: personal data kept no longer than necessary | Outbox and scheduled payloads can hold personal and health data; the purge bounds their lifetime |
| GDPR Art. 25 | Data protection by default | Purge on by default (30 days), the application opts out explicitly |
| SPEC-002 REQ-018 / AC-018 | Purge processed outbox and executed scheduled messages on the 10 providers; dead letters and pending kept | The requirement this plan implements |
| SPEC-002 S21 | Reference scenario: messages carrying personal data are purged after their period | Verified by #1227 (P-25) on top of this work |
| SPEC-002 REQ-017 | Dead letters stay in the outbox until the application acts | Why the predicate excludes `ProcessedAtUtc IS NULL` rows |
| SPEC-002 REQ-061 / AC-043, REQ-062 / AC-044 | Tenant-aware, instrumented | Tenancy deferred to #737/#739/#1257 (Design Choice 6); instrumentation in Phase 9 |

### Provider Delete Semantics

| Provider | Bounded delete | Notes |
|----------|----------------|-------|
| SQL Server (ADO, Dapper, EF) | `DELETE TOP (@n) ... WHERE ...` | No `ORDER BY` in `DELETE TOP`; order is irrelevant for correctness |
| PostgreSQL (ADO, Dapper, EF) | `DELETE ... WHERE id IN (SELECT id ... LIMIT @n FOR UPDATE SKIP LOCKED)` | `SKIP LOCKED` lets two hosts purge disjoint batches; EF (Npgsql) emits a subquery |
| MySQL (ADO, Dapper, EF) | `DELETE ... ORDER BY ProcessedAtUtc LIMIT @n` | Single-table syntax; Pomelo 9.0.0 translation of `Take` + `ExecuteDelete` on .NET 10 to verify |
| MongoDB | `find` ids (limit, projection) then `deleteMany` on ids + predicate | `deleteMany` has no limit |

### Open Questions to Verify at Implementation

1. Does Pomelo `9.0.0` (or the version current when work starts) translate `OrderBy().Take().ExecuteDeleteAsync()` on .NET 10? Phase 5 has the fallback.
2. Have #718 and #1251 landed? If so, the store constructors, enlistment helper, EventIds and test schemas have moved. Rebase the phase lists on that day's code.

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `InboxOptions` purge settings | `src/Encina.Messaging/Inbox/InboxOptions.cs:27-45` | Names and defaults mirrored on outbox and scheduling; validated and finally consumed |
| `IInboxStore.GetExpiredMessagesAsync` / `RemoveExpiredMessagesAsync` | `src/Encina.Messaging/Inbox/IInboxStore.cs:78,88` | Inbox loop of `MessagePurgeService` (Design Choice 2) |
| `OperationAuditRetentionService` | `src/Encina.Security.Audit/OperationAuditRetentionService.cs` | Reference for a retention `BackgroundService` (scope per run, `TimeProvider` delay, error-code logging) |
| `IOperationAuditStore.PurgeEntriesAsync` | `src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs:168` | Precedent for `Either<EncinaError, int>` purge shape |
| `EitherHelpers.TryAsync` | used by every store, e.g. `OutboxStoreADO.cs:55` | Provider exceptions become `Left` with an error code |
| Outbox / scheduling / inbox activity sources | `src/Encina.Messaging/Diagnostics/*ActivitySource.cs` | `encina.*.purge` activities |
| `Meter("Encina", "1.0")` | `OutboxProcessorMetrics.cs:48`, `SchedulingProcessorMetrics.cs:26` | Purge counters on the shared meter |
| `MessagingServiceCollectionExtensions` | `src/Encina.Messaging/MessagingServiceCollectionExtensions.cs:203,253,273,313,358` | `RegisterMessagePurge` beside the pattern registrations |
| Existing ProcessedAtUtc indexes | outbox scripts `001_*`, `OutboxMessageConfiguration.cs:42`, `ScheduledMessageConfiguration.cs:63`, `MongoDbIndexCreator.cs:73-82,155-163` | No new outbox index; only the six scheduled scripts gain one |
| Store integration test files | `tests/Encina.IntegrationTests/{ADO,Dapper}/*/{Outbox,Scheduling}/`, `Infrastructure/EntityFrameworkCore/*/`, `Infrastructure/MongoDB/Stores/` | Purge tests added to the existing classes and collections |
| `FakeOutboxStore`, `FakeScheduledMessageStore` | `src/Encina.Testing.Fakes/Stores/` | Contract and property tests run against them |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` | 2800-2999 (`EventIdRanges.Messaging`) | **2970-2976** new (`MessagePurgeLog`); highest used on `main` is 2962; #718 plan reserves 2963-2969; #1251 must start after 2976 |
| `Encina.ADO.*`, `Encina.Dapper.*`, `Encina.EntityFrameworkCore`, `Encina.MongoDB` | their own ranges | No new log messages; store failures surface as `Left` and are logged by the service |

### File Count Estimate

| Category | Files | Notes |
|----------|-------|-------|
| Core options and contracts | 5 | Three option classes, two interfaces |
| Purge service, schedule, cycle, guard | 4 | New `Purge/` folder |
| Diagnostics | 5 | Three activity sources (modified), metrics and log (new) |
| DI | 1 | `MessagingServiceCollectionExtensions.cs` |
| Provider stores | 20 | 10 outbox + 10 scheduled, one method each |
| SQL scripts | 6 | Scheduled index on ADO/Dapper |
| Decorators and fakes | 4 | `Encina.OpenTelemetry`, `Encina.Testing.Fakes` |
| PublicAPI files | 11 | Messaging, 8 provider packages, OpenTelemetry, Testing.Fakes |
| Tests | ~45 | Unit ~8, guard ~10, contract 2, property 1, integration ~20, schemas 3, justifications 2 |
| Documentation | ~13 | Messaging and 8 provider READMEs, feature page, scheduling link, INVENTORY, changelog fragment |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a pre-1.0 .NET 10 / C# 14 library (no backward compatibility). Messaging patterns live in
Encina.Messaging; providers (ADO.NET x3, Dapper x3, EF Core x3, MongoDB) implement IOutboxStore,
IScheduledMessageStore and IInboxStore. Operations return Either<EncinaError, T>; time comes from TimeProvider;
database calls are async with CancellationToken; EncinaError.Message never reaches logs or tags. Issue #1200
(SPEC-002 REQ-018, AC-018, S21): purge processed outbox and executed scheduled messages after a configurable
period (30 days default, purge on) on the 10 providers; never purge pending or dead-lettered messages.

IMPLEMENTATION OVERVIEW:
1. Options: MessageRetentionPeriod, EnableAutomaticPurge, PurgeInterval, PurgeBatchSize on OutboxOptions and
   SchedulingOptions (InboxOptions names and defaults), setter validation, also on InboxOptions.
2. Contracts: PurgeProcessedAsync(DateTime processedBeforeUtc, int batchSize, ct) -> Either<EncinaError, int> on
   IOutboxStore and IScheduledMessageStore; predicate ProcessedAtUtc IS NOT NULL AND ProcessedAtUtc < cutoff.
3. MessagePurgeService (BackgroundService) with outbox, scheduled and inbox loops (inbox through the existing
   GetExpiredMessagesAsync/RemoveExpiredMessagesAsync), independent of EnableProcessor, registered only when a
   pattern has purge on.
4. Providers: one bounded DELETE per dialect (SQL Server DELETE TOP, PostgreSQL subquery with SKIP LOCKED,
   MySQL DELETE ... LIMIT, EF ExecuteDeleteAsync with Take, MongoDB ids then DeleteMany); new ProcessedAtUtc
   index on the six ADO/Dapper scheduled scripts only.
5. Decorators (Encina.OpenTelemetry) and fakes (Encina.Testing.Fakes) gain the method.
6. Observability: encina.{outbox,scheduling,inbox}.purge activities, *_messages_purged_total counters,
   purge_failures_total, [LoggerMessage] EventIds 2970-2976 in EventIdRanges.Messaging.
7. Tests on all flags, AC-018 integration tests on the 10 providers, docs and changelog fragment.

KEY PATTERNS:
- No distributed lock (deletes are idempotent; leader election is #717); no tenant filter until #737/#739 add
  TenantId (two-tenant test owned by #1257); no placeholder tenant in telemetry.
- Each batch is one statement (atomic); the service runs in its own scope outside any ambient transaction.
- A Left stops the cycle and is logged by error code; exceptions are logged with ForLogging().
- Shared [Collection] fixtures for integration tests; Shouldly via Encina.Testing.Shouldly; FakeTimeProvider.
- Per-file coverage targets with justifications; CRAP <= 10 on changed methods; zero warnings; PublicAPI tracked.
- Order: after #718 (store constructors and enlistment), independent of #1251 (claims touch only unprocessed rows).

REFERENCE FILES:
- src/Encina.Messaging/Inbox/InboxOptions.cs, src/Encina.Messaging/Inbox/IInboxStore.cs
- src/Encina.Messaging/Outbox/IOutboxStore.cs, src/Encina.Messaging/Outbox/OutboxOptions.cs
- src/Encina.Messaging/Scheduling/IScheduledMessageStore.cs, src/Encina.Messaging/Scheduling/SchedulingOptions.cs
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs
- src/Encina.Security.Audit/OperationAuditRetentionService.cs
- src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs, src/Encina.EntityFrameworkCore/Outbox/OutboxStoreEF.cs,
  src/Encina.MongoDB/Outbox/OutboxStoreMongoDB.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- docs/plans/outbox-atomicity-ado-implementation-plan-718.md (ordering and EventIds)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | No read path: the purge deletes and returns a count |
| 2 | OpenTelemetry | ✅ | `encina.{outbox,scheduling,inbox}.purge` activities and purge counters on `Meter("Encina")` (Phase 9); no ids, payloads or tenant placeholder |
| 3 | Structured Logging | ✅ | `MessagePurgeLog` with EventIds 2970-2976 in `EventIdRanges.Messaging` (Phase 9) |
| 4 | Health Checks | ❌ | No new dependency; `OutboxHealthCheck`, `SchedulingHealthCheck` and `InboxHealthCheck` already probe the stores |
| 5 | Validation | ✅ | Setter validation of the retention options (also added to `InboxOptions`) and the start-up warning when the interval exceeds the period (Phases 1, 8) |
| 6 | Resilience | ❌ | Calls only the application's own database; a failed cycle is retried at the next interval |
| 7 | Distributed Locks | ❌ | Bounded predicate-guarded deletes are idempotent across hosts (Design Choice 5); optional leader election is #717 |
| 8 | Transactions | ✅ | Each batch is one atomic statement; the purge runs in its own scope and never joins a request transaction (Phase 8) |
| 9 | Idempotency | ❌ | Not a message or request entry point; repeated purges are idempotent by construction (property test, Phase 10) |
| 10 | Multi-Tenancy | ⏭️ | No tenant column on outbox or scheduled rows yet: #737 and #739 add it, #1257 owns the two-tenant purge test (Design Choice 6) |
| 11 | Module Isolation | ❌ | SPEC-002 requires no module scoping of messaging; `ModuleId` on messaging entities stays with #747 |
| 12 | Audit Trail | ❌ | Deleting technical delivery records is not a compliance event; evidence of sending belongs in the application's records or audit trail (issue Motivation); counts are in metrics and logs |

---

## Prerequisites & Dependencies

### Ordering with the plans that change the same stores

| Issue | State | Relation and recommended order |
|-------|-------|-------------------------------|
| #718 (+ #719, #1935) | open, plan decided 2026-10-06 | Rewrites the constructors of all outbox and scheduled stores (transaction accessor). It also enlists every command, replaces the no-op `OpenConnectionAsync`, removes `OutboxPostProcessor` and reserves Messaging EventIds 2963-2969. **Recommended: land #1200 after #718**, so the purge commands use #718's enlistment helper and real `OpenAsync`. If #1200 lands first, #718's Phases 5-8 must add `PurgeProcessedAsync` to their enlistment list |
| #1251 | open, planned in parallel | Adds lease columns and changes the pending/due queries of the same tables, scripts and test schemas. Functionally independent: claims touch only unprocessed rows, and the purge touches only processed rows. Either order works. The second PR rebases the shared scripts and `tests/Encina.TestInfrastructure/Schemas/*`. Its EventIds must start after 2976 |
| #737, #739 | open | Add `TenantId` to outbox and scheduled rows; then `PurgeProcessedAsync` gains a tenant filter (Design Choice 6) |
| #1257 | open | Owns the AC-043 two-tenant test that includes the purge |
| #717 | open | Leader election, if the maintainer later wants the purge on one host only |

### Advisable before or with this work (issue files written by this plan)

| Issue file | Relation |
|------------|----------|
| `artifacts/issues/plan-1200-recurring-scheduled-redispatch-ado-dapper.md` | ADO.NET and Dapper scheduled stores keep dispatching recurring messages after their recurrence ends (`ScheduledMessageStoreADO.cs:92`). Such rows never age out, so the purge cannot remove them on those six providers. Fix before or with Phase 3 |
| `artifacts/issues/plan-1200-cdc-outbox-rows-never-marked-processed.md` | `OutboxCdcHandler` never marks rows processed (`OutboxCdcHandler.cs:67-106`). Under CDC the purge never removes them, and in hybrid mode they are published twice, contrary to `docs/examples/cdc-outbox-integration.md:65-71` |
| `artifacts/issues/plan-1200-inbox-automatic-purge-never-runs.md` | `InboxOptions.EnableAutomaticPurge`/`PurgeInterval`/`PurgeBatchSize` are never read. Covered by this plan if the maintainer picks Design Choice 2 B; open it as a standalone bug otherwise |

### Related, not blocking

| Issue | State | Relation |
|-------|-------|----------|
| #1170 / #1868 | open | No-op `OpenConnectionAsync` in ADO stores; the new purge methods must not copy it (Phase 3) |
| #1418 | open | Synchronous ADO/Dapper calls; the new methods are async from the start |
| #1150 | closed | Dead-letter state that the purge must keep (REQ-017) |
| #1168 | closed | Outbox payloads now go through `IMessageSerializer`; encrypted payloads are purged the same way |
| #1227 | open | Reference scenario S21 runs on top of this work (P-25) |

---

## Next Steps

1. The maintainer decides Design Choices 1-7. The orchestrator records the answers in a final Maintainer Decisions section.
2. The orchestrator opens the three issue files under `artifacts/issues/` (two bugs, plus the inbox bug only if Design Choice 2 is not B) and links them here.
3. Link this plan from #1200, and cross-link it from the #718 and #1251 plans (ordering, EventIds 2963-2969 / 2970-2976).
4. When work starts, re-check every file and line cited here against that day's `main`. #718 and #1251 may have changed the stores, scripts and EventIds.
5. Implement Phases 1-11. Phases 1 and 3-7 must merge together, because the interface change does not build alone. Recommended PR: one PR `Fixes #1200`, after the #718 PR series.
