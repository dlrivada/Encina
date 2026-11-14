# Implementation Plan: Persistent `IDelayedRetryStore` for the 10 Database Providers

> **Issue**: [#594](https://github.com/dlrivada/Encina/issues/594)
> **Type**: Feature
> **Parent**: [#875](https://github.com/dlrivada/Encina/issues/875) (EPIC)
> **Related**: #595 (Redis implementation, child), #2254 (recoverability OpenTelemetry and health check, shares the backlog members), #2248 (default recoverability registration, closed), #583 (persistent dead letter stores, closed), #590 (routing slip stores, same `Either` question), ADR-050 (module dependencies)
> **Complexity**: High (10 providers, an interface change, a claim protocol)
> **Estimated Scope**: ~2,600-3,400 lines of production code + ~6,000 lines of tests; far above one worker pull request, split proposed in Phase 1

---

## Summary

### What exists today (re-verified on 2026-11-14, main `c871956bf`)

- `IDelayedRetryStore` (`src/Encina.Messaging/Recoverability/IDelayedRetryScheduler.cs`, from line 153) has five members, all returning `Task`: `AddAsync(IDelayedRetryMessage)`, `GetPendingMessagesAsync(batchSize)`, `MarkAsProcessedAsync(id)`, `MarkAsFailedAsync(id, errorMessage)` and `DeleteByContextIdAsync(recoverabilityContextId)` (returns `Task<bool>`). The issue's original listing (`ScheduleRetryAsync`, `GetDueRetriesAsync`, `IncrementRetryCountAsync`, `RemoveAsync`, `GetByMessageIdAsync`, a `DelayedRetry` type) does not exist in `src`.
- `IDelayedRetryMessage` carries `Id`, `RecoverabilityContextId`, `RequestType`, `RequestContent`, `ContextContent`, `DelayedRetryAttempt`, `ScheduledAtUtc`, `ExecuteAtUtc`, `CorrelationId`, `ProcessedAtUtc` and `ErrorMessage`. `IDelayedRetryMessageFactory.Create(DelayedRetryMessageData)` is taken by `DelayedRetryScheduler`.
- No implementation of the store or the factory exists outside tests. `AddEncinaDelayedRetryStore<TStore, TFactory>` (`RecoverabilityServiceCollectionExtensions.cs:256`) registers both as scoped. `RecoverabilityOptions.EnableDelayedRetries` defaults to `false` (`RecoverabilityOptions.cs:119`); with it on and no store or factory, `DelayedRetryStartupCheck` fails host start (ADR-050 amendment of 2026-10-26, #2248).
- `DelayedRetryProcessor` (a hosted service) reads `GetPendingMessagesAsync(BatchSize)` from a scoped store (`DelayedRetryProcessor.cs:131-138`), then calls `MarkAsProcessedAsync`/`MarkAsFailedAsync`. The error argument is already a code: `ex.GetType().FullName` (line 176) or a `FailureInfo` text built from a code (lines 236-251, 596); Phase 1 verifies the last one.
- Dead letter stores of #583 are the structural reference: per provider a `DeadLetter/` folder with `DeadLetterMessage`, `DeadLetterMessageFactory` and `DeadLetterStore{ADO|Dapper|EF|MongoDB}`, a `029_CreateDeadLetterMessagesTable.sql` script, and `AddDeadLetterQueueServices<TStore, TFactory>(config.UseDeadLetterQueue, ...)` in each provider's `ServiceCollectionExtensions` (e.g. `Encina.ADO.SqlServer/ServiceCollectionExtensions.cs:82`).
- Maintainer decision on #594 (2026-10-16): in scope for 1.0, priority `p0-mandatory`, the 10 providers (not 13; SQLite is out, ADR-024). Contract note from #2254: the store exposes the pending count and the oldest pending entry, implemented by every persistent store.
- The only implementers and mocks of the interface today are test classes: nine files under `tests/Encina.UnitTests/Messaging` name `IDelayedRetryStore` (for example `DelayedRetryProcessorTests`, `DelayedRetrySchedulerTests`, `StorePayloadEncryptionTests.cs:351`, `ErrorMessageLeakTests`). The private test store in `StorePayloadEncryptionTests.cs:351-356` sets only `ErrorMessage` on failure and leaves `ProcessedAtUtc` null, so on its semantics a "failed" row stays pending: the processor relies on the opposite (`DelayedRetryProcessor.cs:240-241`, "so a row that keeps throwing cannot loop"). No `IDelayedRetryStore` fake exists in `src/Encina.Testing*`.
- The processor dispatches with `tenantId: null` in a background scope (`DelayedRetryProcessor.cs:384`), and the scheduler's "cancel" is `DeleteByContextIdAsync` (`DelayedRetryScheduler.cs:114`).

### What this plan delivers

`DelayedRetryStore{ADO,Dapper,EF,MongoDB}` with `DelayedRetryMessage` and `DelayedRetryMessageFactory` for each of the 10 providers, the migration scripts, the registration from each provider's `ServiceCollectionExtensions` (gated by `UseRecoverability`), the backlog members, the decisions below applied to the core interface, the processor and the fakes, and tests against real databases.

- Affected packages: `Encina.Messaging` (interface, scheduler, processor, health), `Encina.ADO.{SqlServer,PostgreSQL,MySQL}`, `Encina.Dapper.{SqlServer,PostgreSQL,MySQL}`, `Encina.EntityFrameworkCore`, `Encina.MongoDB`, `Encina.OpenTelemetry` (decorator), `Encina.Testing.Fakes`.
- Provider category: Database (10), `AGENTS.md` section 5.

---

## Design Choices

<details>
<summary><strong>1. What happens to a processed row</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep it with `ProcessedAtUtc` set; a cleanup processor purges after a retention period** | Full history for diagnosis; the backlog count and oldest-pending queries can use the same table without extra bookkeeping; mirrors `DeadLetterCleanupProcessor` | A new processor and a retention option; table growth until the purge |
| **B) Delete the row when it is marked processed** | Smallest table; no cleanup job | No history of past retries; `MarkAsProcessedAsync` becomes a delete, which also changes what `ProcessedAtUtc` on `IDelayedRetryMessage` means (it is then never non-null on a stored row) |

### Chosen Option: **A — Keep and purge after a retention period** (recommended, pending the maintainer)

### Rationale

- The interface already models a processed state (`ProcessedAtUtc`, `MarkAsProcessedAsync`), and the repo's convention for terminal rows is keep then purge (`DeadLetterCleanupProcessor`, outbox cleanup).
- B wins for very high retry volume, where the table size matters more than diagnosis.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Preventing duplicate pickup when several processors run</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Atomic claim in the store** (a claim column such as `ClaimedAtUtc`; `GetPendingMessagesAsync` claims the rows it returns in one statement, with a claim timeout like `DeadLetterOptions.ReplayClaimTimeout`) | Correct without extra infrastructure; the same mechanism as `IDeadLetterStore.TryClaimForReplayAsync` (`AGENTS.md` section 3 background-processing rules); ADR-050 names row claiming for the outbox (#1251) | Changes the store contract semantics and the processor (a crashed claim must expire) |
| **B) `IDistributedLockProvider` around each batch** | No interface change | Needs a lock provider registered (an extra required module in ADR-050 terms); one lock per batch serializes processors |
| **C) Accept duplicates and require idempotent handlers** | Simplest | Every retried handler must be idempotent; violates the "exactly-once processing" intent of the inbox pattern for retried work |

### Chosen Option: **A — Atomic claim in the store** (recommended, pending the maintainer)

### Rationale

- A gives correct multi-instance behavior with the database that is already required. SQL Server uses `UPDATE ... OUTPUT` with `READPAST, UPDLOCK`, PostgreSQL and MySQL use `FOR UPDATE SKIP LOCKED` inside one statement or transaction, EF Core uses the same through raw SQL, MongoDB uses `FindOneAndUpdate` per document.
- B wins when the application already runs a lock provider for other modules and wants the store unchanged; C wins only when every retried handler is idempotent.
- #595 (Redis) follows this answer for its own claim.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Should the store return `Either<EncinaError, T>`</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Change the interface to `Either`** like `IDeadLetterStore` | Consistent with ROP (ADR-001, ADR-006); the processor handles an infrastructure `Left` explicitly (fail the operation, retry later) instead of relying on a catch-all | Changes the interface, the scheduler, the processor and the fakes (all in the same pull request series; pre-1.0 so no compatibility layer) |
| **B) Keep `Task` and exceptions** | No change to the core | Failures surface as exceptions; two error conventions side by side in `Encina.Messaging`; the processor's catch-all hides the difference between a handler failure and a store failure |

### Chosen Option: **A — Change the interface to `Either`** (recommended, pending the maintainer)

### Rationale

- `AGENTS.md` section 3: operations return `Either<EncinaError, T>` and a `Left` from a store inside a processor fails that operation, never reports success. Pre-1.0 means the interface is changed completely (`AGENTS.md` section 1).
- B wins only if the maintainer wants this issue to stay out of the core. The answer is shared with #590 (D2 there), so both stores end with one convention.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Tenant and module columns on the retry row</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Typed `TenantId` and `ModuleId` columns** on `IDelayedRetryMessage` and `DelayedRetryMessageData`, filled by the scheduler from the request context | Queryable per tenant/module (backlog health per tenant, tenant purge); consistent with `IDeadLetterMessage.TenantId` | Interface and data record grow; the scheduler must read the ambient context |
| **B) Only the serialized `ContextContent`** | No interface change | Tenant/module are invisible to queries and to the backlog health check; a tenant purge scans payloads |

### Chosen Option: **A — Typed columns** (recommended, pending the maintainer)

### Rationale

- `AGENTS.md` section 6, functions 10 and 11, and SPEC-000 section 2 (tenancy in 1.0): a new store that holds tenant data carries `TenantId` and `ModuleId` as columns. The dead letter row already carries `TenantId`.
- B wins if retries are never queried by tenant and the interface must stay minimal.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Storage separation, time, error text, scope and registration</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Dedicated table per provider, `TimeProvider`, error code only, 10-provider scope, `AddEncinaDelayedRetryStore<TStore, TFactory>` per provider** | Pending retries and terminal dead letters keep separate contracts; no raw error text in storage; the application's own registration wins | One more table and script per provider |
| **B) Reuse the dead letter table or a broker's delay feature** | Fewer tables | Retries are pending and dead letters terminal (`IDeadLetterStore` rows are final until replayed); broker delay (RabbitMQ delayed messages, SQS visibility timeout) is broker-specific while the store contract is portable |

### Chosen Option: **A — Dedicated table per provider and the shared conventions**

### Rationale

- Time: `TimeProvider` is injected into every store; "due" means `ExecuteAtUtc <= now AND ProcessedAtUtc IS NULL` (and no live claim). Database calls are asynchronous with a `CancellationToken` (`AGENTS.md` section 3).
- Error text: the `errorMessage` argument of `MarkAsFailedAsync` is stored as an error code or exception type; the **column** is `error_code` while the interface member stays `ErrorMessage` (it exists on `IDelayedRetryMessage`; each store maps it explicitly) (`EncinaError.Message` never reaches plaintext storage, `AGENTS.md` section 3). Phase 1 confirms the three call sites.
- Scope: the 10 providers of `AGENTS.md` section 5; SQLite is out (ADR-024). Registration: `AddEncinaDelayedRetryStore<TStore, TFactory>` as decided on #2248, via `TryAdd` so a store registered by the application wins in any order, gated by `UseRecoverability`, with the `Tenancy` variant of each provider.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (TimeProvider, async database calls, EncinaError.Message never in plaintext storage, registration completeness), AGENTS.md section 5 (10 providers), ADR-024, ADR-050, and the maintainer decision of 2026-10-16 on #594

</details>

<details>
<summary><strong>6. What a failed row and a cancelled retry mean</strong></summary>

### Options Considered

`MarkAsFailedAsync` is called when a retry cannot be dispatched or keeps failing; `DeleteByContextIdAsync` is the scheduler's cancel of a pending retry (`DelayedRetryScheduler.cs:114`). Today the interface does not say what either leaves in the store.

| Option | Pros | Cons |
|--------|------|------|
| **A) A failed row becomes terminal by setting `ProcessedAtUtc` and `error_code`** (pending is still `ProcessedAtUtc IS NULL`; retention purges processed and failed rows alike) | No new column or interface member; the pending selection, the backlog and the purge use one predicate; a row that keeps throwing cannot loop (`DelayedRetryProcessor.cs:240-241`) | `ProcessedAtUtc` also marks failures, so "succeeded versus failed" is told apart only by `error_code` being set |
| **B) A separate `FailedAtUtc` column and member** | Success and failure are distinct states | Interface and data record grow; every store, index and the purge need a second predicate |

### Chosen Option: **A — A failed row is terminal through `ProcessedAtUtc` plus `error_code`** (recommended, pending the maintainer)

### Rationale

- A keeps the interface as it is and gives all ten stores one definition: `MarkAsFailedAsync` sets `ProcessedAtUtc` (from `TimeProvider`) and `error_code`, the row is excluded from pending and counted by the retention purge. B is the choice when operators must query "failed retries" separately from "completed retries".
- Cancel (both options): `DeleteByContextIdAsync` deletes only rows that are pending (`ProcessedAtUtc IS NULL`) and not under a live claim (claim unset or expired), and returns `true` only when it deleted one; processed or failed rows are kept for history (Design Choice 1 A), and a retry that is in flight is not cancelled. #595 follows the same rule (its plan removes the context key when an entry is processed or failed).

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Where the add runs relative to the caller's transaction (and the EF mechanism)</strong></summary>

### Options Considered

The retry is scheduled by `RecoverabilityPipelineBehavior` **after** the handler failed. A row joined to the request's transaction would be lost when that transaction rolls back, which is the usual outcome of a failed handler. The interface also has no `SaveChangesAsync` (unlike `IDeadLetterStore`) and the processor never calls one, so an EF store must say when it writes.

| Option | Pros | Cons |
|--------|------|------|
| **A) The add runs on its own connection and statement, outside the request transaction, on all providers** (ADO/Dapper open a new connection, never `IDbTransactionAccessor`; EF uses a fresh `DbContext`/connection created from the same options, with `ExecuteInsert`-style immediate execution; MongoDB has no transaction involved) | The retry survives the handler's rollback, which is its purpose; no flush of the caller's tracked EF changes | One extra connection per scheduled retry; the add is not atomic with the handler's other writes (they are rolled back anyway) |
| **B) The add joins the caller's unit of work when one exists** | One transaction | The retry row disappears with the rollback of a failed handler: the retry is never executed |

### Chosen Option: **A — Own connection, outside the request transaction** (recommended, pending the maintainer)

### Rationale

- Only A delivers the issue's behaviour (a failed handler is retried later). The matrix row 8 is corrected accordingly. The EF store never uses the scoped `DbContext` change tracker: a save on the shared context would also flush the caller's pending changes. A test starts a transaction, schedules a retry, rolls the transaction back and asserts the retry row survives, on every provider.
- B wins only for a scheduler that runs before any handler failure, which `RecoverabilityPipelineBehavior` does not.

**Class:** Dilemma

</details>

<details>
<summary><strong>8. Idempotency key of a scheduled retry</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Unique `(RecoverabilityContextId, DelayedRetryAttempt)`; a second add of the same attempt returns `Right(false)`** | A repeated schedule of one attempt is a no-op even with a new `Id`; the same outcome shape as `IDeadLetterStore.AddAsync` | A unique index per database and a `Right(false)` the scheduler must handle (log by code, no error) |
| **B) Deduplicate on `Id` only** | Primary key, no extra index | A rescheduled attempt with a new `Id` creates a second pending row, so the handler runs twice |

### Chosen Option: **A — Unique `(RecoverabilityContextId, DelayedRetryAttempt)`** (recommended, pending the maintainer)

### Rationale

- A matches the "no duplicates" intent of the pattern (inbox idempotency, function 9) and makes the cross-provider contract test unambiguous: adding the same attempt twice leaves one row and the second call is `Right(false)`. #595 (Redis) follows this answer (`SET NX` on `ctx:{contextId}:{attempt}` inside the add script). B wins if attempts are never re-added with a new `Id`.

**Class:** Dilemma

</details>

<details>
<summary><strong>9. Shape of the backlog member</strong></summary>

### Options Considered

The contract note of 2026-10-16 on #2254 says the store exposes the pending count and the oldest pending entry; #2254 owns the health check and metric names, not the member shape.

| Option | Pros | Cons |
|--------|------|------|
| **A) One member `GetBacklogAsync(ct)` returning `Either<EncinaError, DelayedRetryBacklog>` (`PendingCount`, `OldestPendingExecuteAtUtc`)** | One round trip, one consistent snapshot for the health check | One new record type |
| **B) Two members, `GetPendingCountAsync` and `GetOldestPendingAsync`** | No new type | Two queries that can disagree; two members to implement ten times |

### Chosen Option: **A — One member** (recommended, pending the maintainer)

### Rationale

- A gives one snapshot per health probe and the smallest surface for ten implementations. The worker re-reads #2254 before coding and follows its comment if it fixed another shape. B wins if #2254 asks for independent members.

**Class:** Dilemma

</details>

<details>
<summary><strong>10. Tenant and module context when a retry runs</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The processor restores tenant and module context from the row before `Send`; with multi-tenancy on and no tenant on the row it does not dispatch, fails the row visibly (`MarkAsFailedAsync` with `delayed_retry.tenant_missing`) and logs by code** | Fail closed (`AGENTS.md` section 3); the retry runs as the caller that failed | The processor needs the tenant and module context setters (named by the worker from `ITenantContext`/`IModuleContext` registrations) |
| **B) Dispatch with `tenantId: null`** (today, `DelayedRetryProcessor.cs:384`) | No change | A retry runs as an unscoped caller while tenancy is on: a fail-open path |

### Chosen Option: **A — Restore the context, fail closed**

### Rationale

- Only A satisfies the fail-closed rule: B runs tenant data without its tenant. `GetPendingMessagesAsync` reads every tenant (stated for all ten stores and asserted by the contract test); the new decorator adds a tenant tag only when multi-tenancy is on (ADR-054 D4).

**Class:** Settled
**Settled by:** AGENTS.md section 3 (compliance and security gates fail closed), SPEC-000 section 2 (tenancy in 1.0), ADR-054 D4

</details>

### Trivial decisions

| Decision | Value | Citation |
|----------|-------|----------|
| Pending order | `(ExecuteAtUtc, Id)` ascending | Deterministic batches; same ordering key as the dead letter stores |
| Timestamp precision | `datetime2(7)` (SQL Server), `timestamptz` (PostgreSQL), `DATETIME(6)` (MySQL; the default `DATETIME` rounds to the second and would move due times) | The MySQL dead letter script uses `DATETIME(6)`; the SQL Server one `datetime2` |

Defaults that exist only if the maintainer's answers are A (the worker copies them when briefed, they are not registered as decisions here): with Design Choice 1 A, `RetentionPeriod` 7 days and `CleanupInterval` 1 hour as in `DeadLetterOptions` (`DeadLetterOptions.cs:29`, `:44`); with Design Choice 2 A, a claim timeout of 5 minutes as `ReplayClaimTimeout` (`DeadLetterOptions.cs:63`). All three are options on the recoverability options, validated like the dead letter ones.

### Error codes and categories (ADR-062)

A new `DelayedRetryErrorCodes` class in `Encina.Messaging.Recoverability` owns the codes; every `Left` is created with `EncinaErrors.Create(code, category, ...)` and a `GetCategory` unit test asserts each row. Only the code, never the provider message, reaches storage, logs and tags.

| Code | Category | Used for |
|------|----------|----------|
| `delayed_retry.store_failed` | `Internal` | Non-transient database or driver fault |
| `delayed_retry.store_unavailable` | `Unavailable` | Connection failure where the provider distinguishes it |
| `delayed_retry.store_timeout` | `Timeout` | Command timeout not caused by the caller's token |
| `delayed_retry.not_found` | `NotFound` | `MarkAsProcessedAsync`/`MarkAsFailedAsync` for an `Id` the request names and the store does not hold |
| `delayed_retry.tenant_missing` | `FailedPrecondition` | The processor refuses a row without a tenant while tenancy is on (Design Choice 10) |

---

## Implementation Phases

### Phase 1: Core Contract, Backlog Members and Split

> **Goal**: Apply the answers to Design Choices 2 to 4 and 6 to 10 in `Encina.Messaging` before any provider work.

<details>
<summary><strong>Tasks</strong></summary>

1. **Modify** `src/Encina.Messaging/Recoverability/IDelayedRetryScheduler.cs`. Exact signatures (Design Choices 3, 2, 8, 9; all take a trailing `CancellationToken cancellationToken = default`):
   - `Task<Either<EncinaError, bool>> AddAsync(IDelayedRetryMessage message, ...)`: `Right(true)` stored, `Right(false)` the `(RecoverabilityContextId, DelayedRetryAttempt)` pair already exists (Design Choice 8 A).
   - `Task<Either<EncinaError, IReadOnlyList<IDelayedRetryMessage>>> GetPendingMessagesAsync(int batchSize, ...)`: claims and returns due, unclaimed-or-expired-claim rows ordered `(ExecuteAtUtc, Id)`, every tenant.
   - `Task<Either<EncinaError, Unit>> MarkAsProcessedAsync(Guid id, ...)` and `Task<Either<EncinaError, Unit>> MarkAsFailedAsync(Guid id, string errorMessage, ...)`: terminal (Design Choice 6 A); `Left(delayed_retry.not_found)` when the row is absent; repeating on a terminal row is `Right`.
   - `Task<Either<EncinaError, bool>> DeleteByContextIdAsync(Guid recoverabilityContextId, ...)`: deletes pending, unclaimed-or-expired rows only; `Right(true)` only then.
   - `Task<Either<EncinaError, DelayedRetryBacklog>> GetBacklogAsync(...)` (Design Choice 9 A; `DelayedRetryBacklog(long PendingCount, DateTime? OldestPendingExecuteAtUtc)`).
   `IDelayedRetryMessage`/`DelayedRetryMessageData` gain `TenantId` and `ModuleId` (Design Choice 10).
2. **Modify** `DelayedRetryScheduler.cs` and `DelayedRetryProcessor.cs` to the new contract: an infrastructure `Left` fails the operation visibly; the three `MarkAsFailedAsync` call sites keep passing a code (Phase 1 verifies the `failure.Text` at line 596 is a code, and keeps this check); the scheduler fills `TenantId`/`ModuleId` from the ambient context. **Restore step** (Design Choice 10 A): before `Send` the processor sets tenant and module context from the row (no `tenantId: null`); with multi-tenancy on and no tenant on the row it does not dispatch, calls `MarkAsFailedAsync` with `delayed_retry.tenant_missing` and logs the code; processor tests cover both. Add `DelayedRetryCleanupProcessor` and the retention options if Design Choice 1 is A, and `DelayedRetryErrorCodes` with the table above.
3. **Add** `FakeDelayedRetryStore` to `src/Encina.Testing.Fakes` (none exists) implementing the full contract in memory, and **update** the nine existing test files under `tests/Encina.UnitTests/Messaging` that name `IDelayedRetryStore` (mocks and the private test stores), including `StorePayloadEncryptionTests` whose store must set `ProcessedAtUtc` on failure.
4. **Add** `[LoggerMessage]` ids 5459-5464 in `MessagingRecoverability` (5450-5499, 5450-5458 used) for the cleanup processor and the backlog report; update `PublicAPI.Unshipped.txt` of `Encina.Messaging` and `Encina.Testing.Fakes`.
5. **Split** into thirteen pull requests, each carrying its own tests and manifest rows (a production-only PR cannot reach its per-flag targets and fails the blocking `crap-gate`), each at most 15 files and 1,500 lines **counting tests, scripts, `PublicAPI`, manifests and fragments**; the existing test files that must change with the interface travel in P1:

   | PR | Content | Files (approx.) |
   |----|---------|-----------------|
   | P1 | Interface to `Either`, `AddAsync` bool, `DelayedRetryErrorCodes`, scheduler and processor adapted, the nine existing test files adapted, `PublicAPI` | 15 |
   | P2 | Claim-aware pending, `GetBacklogAsync`/`DelayedRetryBacklog`, `TenantId`/`ModuleId`, the processor restore step, `FakeDelayedRetryStore`, the abstract `DelayedRetryStoreContractTests` run against the fake, processor tests, manifests, `PublicAPI` | 14 |
   | P3 | `DelayedRetryCleanupProcessor`, retention options, EventIds 5459-5464, tests, manifest (if Design Choice 1 is A) | 8 |
   | P4 | `InstrumentedDelayedRetryStore` and its decoration line in `Encina.OpenTelemetry`, tests, manifest | 5 |
   | P5-P7 | One per ADO.NET provider (SqlServer, PostgreSQL, MySQL): message, factory, store, script, registration change, `Log.cs`/EventIds, `PublicAPI`, manifest; unit, guard, DI (`ValidateOnBuild`, `ValidateScopes`) and integration tests, one derived contract test class | 14 each |
   | P8-P10 | The same for Dapper (SqlServer, PostgreSQL, MySQL) | 14 each |
   | P11 | EF Core (one package, three providers): entity, configuration, factory, store, registration, `Log.cs`, `PublicAPI`, manifest; unit, guard, DI, three integration classes, derived contract classes | 15 |
   | P12 | MongoDB: document, factory, store, options, index creator, registration, `Log.cs`, `PublicAPI`, manifest; tests | 14 |
   | P13 | Documentation: READMEs, configuration guide, `docs/INVENTORY.md`, two changelog fragments; carries `Fixes #594` | 12 |

   The contract suite is introduced in P2 against the fake and reused by each provider PR through one derived class; line counts are re-measured when each PR is briefed.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT:
Encina (.NET 10, C# 14, pre-1.0, no backward compatibility) has IDelayedRetryStore in
src/Encina.Messaging/Recoverability/IDelayedRetryScheduler.cs with no implementation outside tests.
Issue #594 adds persistent stores for the 10 database providers. The maintainer answered Design
Choices 1-4 and 6-9 of docs/plans/delayed-retry-stores-implementation-plan-594.md (apply the recorded
answers; build the pull requests P1-P13 of Phase 1 item 5 one at a time, each with its tests).

TASK:
1. Change IDelayedRetryStore to Either<EncinaError, T> returns with the exact signatures of Phase 1
   item 1, make GetPendingMessagesAsync claim the rows it returns (claim column + timeout), add
   GetBacklogAsync (shape per Design Choice 9, re-checked on #2254), and add TenantId/ModuleId to
   IDelayedRetryMessage and DelayedRetryMessageData.
2. Update DelayedRetryScheduler and DelayedRetryProcessor: a Left from the store fails the operation
   (no success reported), the error argument of MarkAsFailedAsync stays a code or exception type,
   the processor restores tenant/module context from the row and fails closed (Design Choice 10).
3. If the answer to Design Choice 1 is "keep", add DelayedRetryCleanupProcessor + options.
4. Update fakes, mocks and tests that implement or mock the store; add EventIds in 5459-5464.

KEY RULES:
- No [Obsolete], no compatibility overload: change the interface completely.
- TimeProvider injected; EncinaError.Message never reaches logs or storage.
- Background loops never let an exception escape (AGENTS.md section 3).

REFERENCE FILES:
- src/Encina.Messaging/Recoverability/IDelayedRetryScheduler.cs, DelayedRetryScheduler.cs, DelayedRetryProcessor.cs
- src/Encina.Messaging/DeadLetter/IDeadLetterStore.cs, DeadLetterCleanupProcessor.cs
- src/Encina.Messaging/Recoverability/RecoverabilityServiceCollectionExtensions.cs
- docs/architecture/adr/050-*.md
```

</details>

---

### Phase 2: ADO.NET Stores (SQL Server, PostgreSQL, MySQL)

> **Goal**: Three `DelayedRetryStoreADO` classes with their scripts and registration.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** per package `DelayedRetry/DelayedRetryMessage.cs`, `DelayedRetryMessageFactory.cs`, `DelayedRetryStoreADO.cs`, following `DeadLetter/` in the same package.
2. **Create** per package `Scripts/0NN_CreateDelayedRetryMessagesTable.sql` (next free number; `030` is the highest on ADO.SqlServer today) with indexes on `(processed_at_utc, execute_at_utc)`, `recoverability_context_id` and `(tenant_id, execute_at_utc)`, a **unique** index on `(recoverability_context_id, delayed_retry_attempt)` (Design Choice 8 A), time columns `datetime2(7)` / `timestamptz` / `DATETIME(6)` (a default MySQL `DATETIME` rounds to the second), `error_code` for the `ErrorMessage` member, and on SQL Server `COLLATE Latin1_General_100_BIN2` on `tenant_id` as in the dead letter script.
3. **Implement** the claim per database: pending is `processed_at_utc IS NULL AND execute_at_utc <= @now AND (claimed_at_utc IS NULL OR claimed_at_utc <= @claimExpiredBefore)` ordered `(execute_at_utc, id)`. SQL Server: `UPDATE TOP (n)` has no `ORDER BY`, so the claim is an ordered CTE (`SELECT TOP (@n) ... WITH (UPDLOCK, READPAST) ORDER BY execute_at_utc, id`) updated through `OUTPUT`; PostgreSQL: `UPDATE ... WHERE id IN (SELECT ... ORDER BY ... LIMIT @n FOR UPDATE SKIP LOCKED) RETURNING`; MySQL: **a short transaction, not one statement**: `SELECT ... ORDER BY ... LIMIT @n FOR UPDATE SKIP LOCKED` then an `UPDATE` of the selected ids. `MarkAsFailedAsync` sets `processed_at_utc` and `error_code` (Design Choice 6 A); `DeleteByContextIdAsync` deletes only pending, unclaimed-or-expired rows. The add uses its **own connection**, never `IDbTransactionAccessor` (Design Choice 7 A). Backlog count and oldest entry; every `DbException` becomes a `Left` with the code and category of the table in Design Choices.
4. **Modify** each package's `ServiceCollectionExtensions` to register `AddEncinaDelayedRetryStore<DelayedRetryStoreADO, DelayedRetryMessageFactory>` when `UseRecoverability` is on, including the tenancy variant; add EventIds inside the package range (`ADOSqlServer` 3200-3249, `ADOPostgreSQL` 3250-3299, `ADOMySQL` 3300-3349).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT:
Phase 1 changed the core contract. ADO.NET providers share one folder structure per database
package (DeadLetter/, Scripts/). SQL differences (AGENTS.md section 5): SQL Server uses TOP (@n) and
bit; PostgreSQL and MySQL use LIMIT @n; PostgreSQL identifiers are case-sensitive; MySQL uses backticks.

TASK:
1. For Encina.ADO.SqlServer, Encina.ADO.PostgreSQL and Encina.ADO.MySQL create DelayedRetryMessage,
   DelayedRetryMessageFactory, DelayedRetryStoreADO and the SQL script of the plan.
2. Implement every IDelayedRetryStore member with OpenAsync/ExecuteReaderAsync/ExecuteNonQueryAsync and
   a CancellationToken (never Open()), UTC DateTime values from TimeProvider, parameterized SQL.
3. Register through AddEncinaDelayedRetryStore with TryAdd semantics when UseRecoverability is on.

KEY RULES:
- Left(EncinaError) on DbException with an error code, never the exception message.
- Pending selection: execute_at_utc <= @now AND processed_at_utc IS NULL AND no live claim.
- Same behavior in all three databases; the contract tests run them side by side.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/DeadLetter/DeadLetterStoreADO.cs and Scripts/029_CreateDeadLetterMessagesTable.sql
- src/Encina.ADO.PostgreSQL/DeadLetter/, src/Encina.ADO.MySQL/DeadLetter/
- src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs (line 82)
```

</details>

---

### Phase 3: Dapper Stores (SQL Server, PostgreSQL, MySQL)

> **Goal**: Three `DelayedRetryStoreDapper` classes with scripts and registration.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** per package `DelayedRetry/DelayedRetryMessage.cs`, `DelayedRetryMessageFactory.cs`, `DelayedRetryStoreDapper.cs`, mirroring `DeadLetter/` of the same package.
2. **Create** the same SQL scripts as Phase 2 (identical schema per database).
3. **Implement** the members with `QueryAsync`/`ExecuteAsync` and `CommandDefinition` carrying the `CancellationToken`; the claim statement is the one of Phase 2.
4. **Modify** each `ServiceCollectionExtensions` (registration, tenancy variant) and add EventIds inside `DapperSqlServer` 3350-3399, `DapperPostgreSQL` 3400-3449, `DapperMySQL` 3450-3499.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT:
Phase 2 fixed the schema and claim statements per database. Dapper packages mirror the ADO ones.

TASK:
1. For Encina.Dapper.SqlServer, Encina.Dapper.PostgreSQL and Encina.Dapper.MySQL create the message,
   factory and DelayedRetryStoreDapper, reusing the Phase 2 SQL text per database.
2. Use CommandDefinition with the CancellationToken for every call; map DbException to Left.
3. Register through AddEncinaDelayedRetryStore when UseRecoverability is on.

KEY RULES:
- Identical observable behavior to the ADO store of the same database.
- TimeProvider injected; no synchronous database calls.

REFERENCE FILES:
- src/Encina.Dapper.SqlServer/DeadLetter/DeadLetterStoreDapper.cs
- src/Encina.Dapper.PostgreSQL/ServiceCollectionExtensions.cs (line 65)
- the Phase 2 ADO delayed retry stores
```

</details>

---

### Phase 4: EF Core Store (SQL Server, PostgreSQL, MySQL)

> **Goal**: One `DelayedRetryStoreEF` for the three EF providers.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.EntityFrameworkCore/DelayedRetry/DelayedRetryMessage.cs`, `DelayedRetryMessageConfiguration.cs` (indexes of Phase 2), `DelayedRetryMessageFactory.cs`, `DelayedRetryStoreEF.cs`.
2. **Implement** the claim with provider-aware raw SQL (the same three statements) behind one method that selects by `DbContext.Database.ProviderName`, or by an `ExecuteUpdateAsync` pattern where one statement is portable; the rest with LINQ. **Who saves** (Design Choice 7 A): the interface has no `SaveChangesAsync`, so every store call executes immediately through `ExecuteSql`/`ExecuteUpdateAsync` on a **fresh `DbContext` (own connection)**, never through the scoped context's change tracker, so the caller's tracked changes are not flushed and the add survives the request transaction's rollback; a test asserts both.
3. **Modify** `ServiceCollectionExtensions.cs` (registration next to line 212, tenancy variant) and add EventIds inside `EntityFrameworkCore` 3000-3099.
4. **Add** the migration notes (the entity is part of the model of an application that uses `UseRecoverability`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT:
Encina.EntityFrameworkCore is one package for SQL Server, PostgreSQL and MySQL. The dead letter store
is DeadLetterStoreEF with DeadLetterMessageConfiguration.

TASK:
1. Create the entity, IEntityTypeConfiguration, factory and DelayedRetryStoreEF.
2. Implement the claim so the three providers behave the same (raw SQL per provider name or one
   portable ExecuteUpdateAsync); SaveChangesAsync/ExecuteUpdateAsync with the CancellationToken.
3. Register through AddEncinaDelayedRetryStore when UseRecoverability is on.

KEY RULES:
- Left(EncinaError) on DbUpdateException/DbException with an error code only.
- Tenant filter: no global query filter leaks into the processor; the processor reads every tenant.

REFERENCE FILES:
- src/Encina.EntityFrameworkCore/DeadLetter/
- src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs (lines 212-214)
```

</details>

---

### Phase 5: MongoDB Store

> **Goal**: `DelayedRetryStoreMongoDB`.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.MongoDB/DelayedRetry/DelayedRetryMessage.cs`, `DelayedRetryMessageFactory.cs`, `DelayedRetryStoreMongoDB.cs`.
2. **Modify** `EncinaMongoDbOptions` (collection name next to `Collections.DeadLetterMessages`), `MongoDbIndexCreator.cs` (indexes `(ProcessedAtUtc, ExecuteAtUtc)`, `RecoverabilityContextId`, `(TenantId, ExecuteAtUtc)`), `ServiceCollectionExtensions` and `Log.cs` (ids after 3166 inside 3100-3199).
3. **Implement** the claim with `FindOneAndUpdateAsync` per document up to `batchSize`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT:
MongoDB has no SQL; the dead letter store is the reference (DeadLetterStoreMongoDB, index creation in
MongoDbIndexCreator, collection names in EncinaMongoDbOptions.Collections).

TASK:
1. Create the document class, factory and store; claim each pending document with FindOneAndUpdate.
2. Add the collection option, indexes and registration (UseRecoverability).
3. Map MongoException to Left with an error code.

KEY RULES:
- UTC DateTime from TimeProvider; CancellationToken on every driver call.

REFERENCE FILES:
- src/Encina.MongoDB/DeadLetter/DeadLetterStoreMongoDB.cs
- src/Encina.MongoDB/MongoDbIndexCreator.cs
- src/Encina.MongoDB/EncinaMongoDbOptions.cs
```

</details>

---

### Phase 6: Cross-Cutting Integration and Observability

> **Goal**: Telemetry, health and registration completeness.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create** `src/Encina.OpenTelemetry/MessagingStores/InstrumentedDelayedRetryStore.cs` (decorator, like `InstrumentedDeadLetterStore`) and its `DecorateService<IDelayedRetryStore>` line in `ServiceCollectionExtensions.cs` (next to line 152), so all 10 providers are instrumented once.
2. **Create** the backlog health check input from `GetBacklogAsync`, coordinated with #2254 (which owns the check and metric names).
3. **Create** DI tests with `ValidateOnBuild` and `ValidateScopes` per provider: the application starts with `EnableDelayedRetries` on and only the provider's registration. The decorator and its test travel in P4, each provider's DI test in that provider's PR (P5-P12). The decorator adds a tenant tag only when multi-tenancy is on (ADR-054 D4) and carries only codes, never `EncinaError.Message`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT:
Stores exist for all 10 providers. Telemetry for stores is done by decorators in
src/Encina.OpenTelemetry/MessagingStores/.

TASK:
1. Add InstrumentedDelayedRetryStore and register the decoration.
2. Feed the backlog numbers to the health check designed in #2254 (do not invent metric names).
3. Write per-provider DI tests (ValidateOnBuild, ValidateScopes; with and without UseRecoverability).

KEY RULES:
- No per-provider Meter; one decorator.
- EncinaError.Message never in tags.

REFERENCE FILES:
- src/Encina.OpenTelemetry/MessagingStores/InstrumentedDeadLetterStore.cs
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs (line 152)
- src/Encina.Messaging/Recoverability/DelayedRetryStartupCheck.cs
```

</details>

---

### Phase 7: Testing

> **Goal**: Every applicable flag reaches its manifest target for every touched package.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit tests** for all 10 stores and factories (mapping, due selection, error-code storage, `Left` on database failure, fake `TimeProvider`).
2. **Guard tests** for every public method with parameters (`batchSize`, `Guid.Empty`, null messages).
3. **Contract tests**: one `IDelayedRetryStore` contract run over all 10 stores (real implementations).
4. **Property tests** (FsCheck): due-ordering invariant and "a processed row is never returned again".
5. **Integration tests** on the shared collection fixtures (`ADO-`/`Dapper-`/`EFCore-` x `SqlServer`/`PostgreSQL`/`MySQL`, MongoDB): concurrent pickup (no row returned to two claimants), claim timeout, purge; `ClearAllDataAsync` in `InitializeAsync`; never a `.md` justification.
6. **Load tests** for concurrent pickup if Design Choice 2 is A or B, else a justification file; a benchmark justification file. With D2 A the load flag expectation for the claim code of each store is 60 percent of its claim method (concurrent pickup is exactly what the load flag exercises).
7. **Semantic tests** (contract suite, every provider): `MarkAsFailedAsync` row is not returned by pending again and is purged; `DeleteByContextIdAsync` deletes a pending row (`true`), not a claimed, processed or failed one (`false`); a duplicate `(contextId, attempt)` add returns `Right(false)` and leaves one row; a retry row survives the rollback of a surrounding transaction (Design Choice 7); pending returns every tenant.

#### Per-file coverage targets (percent, per flag)

Manifests are `Encina.Messaging.json`, `Encina.Testing.Fakes.json`, `Encina.OpenTelemetry.json` and one per provider package. Provider rows apply to each of the nine provider stores (ADO x3, Dapper x3, EF, MongoDB; the EF row counts three providers through one file).

| File | Unit | Guard | Contract | Property | Integration | Justification |
|------|------|-------|----------|----------|-------------|---------------|
| `DelayedRetryProcessor.cs` (modified) | baseline + 5 | 90 | 0 | 0 | 0 | Existing unit tests grow with the restore and fail-closed branches; the PR reports the measured value against the manifest baseline; no store contract in this class |
| `DelayedRetryScheduler.cs` (modified) | baseline + 5 | 90 | 0 | 0 | 0 | Same: `Left` handling and tenant filling |
| `DelayedRetryErrorCodes.cs`, `DelayedRetryBacklog.cs` | 95 | 0 | 80 | 0 | 0 | Constants and a record; no validated parameters |
| `FakeDelayedRetryStore.cs` | 90 | 90 | 90 | 60 | 0 | The contract suite runs on it; no database, so no integration flag |
| `DelayedRetryCleanupProcessor.cs` | 90 | 90 | 0 | 0 | 70 | Pure loop with a fake clock; integration proves the purge on a real database |
| `InstrumentedDelayedRetryStore.cs` | 90 | 90 | 80 | 0 | 0 | Decorator over the contract; no real database needed |
| `DelayedRetryStore{ADO,Dapper,EF,MongoDB}.cs` (each) | 85 | 90 | 85 | 60 | 85 | Fault paths through unit tests; contract and integration cover the claim, cancel and terminal states; property covers due-ordering; load 60 for the claim method |
| `DelayedRetryMessage.cs`, `DelayedRetryMessageFactory.cs` (each) | 90 | 90 | 70 | 0 | 80 | Value mapping; guard on the factory's null data |
| Provider `ServiceCollectionExtensions.cs` (modified) | baseline + 5 | 95 | 0 | 0 | 60 | DI tests per provider; baseline from the manifest |
| `MongoDbIndexCreator.cs` (modified) | baseline | 0 | 0 | 0 | 80 | Index creation is reachable only against a real MongoDB; no validated parameters added |
| Provider `Log.cs` (modified) | 60 | 0 | 0 | 0 | 40 | Source-generated logging |

Each PR reports the measured per-flag value of every touched file against these targets (`coverage-report.cs` "Per-file targets" table) and the local `crap-gate` table.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT:
Per-flag coverage (AGENTS.md section 9) for Encina.Messaging, the 8 provider packages and
Encina.OpenTelemetry; targets in .github/coverage-manifest/{Package}.json.

TASK:
1. Write the unit, guard, contract and property tests of the plan for all 10 providers.
2. Write the integration tests on the existing shared collection fixtures.
3. Add the load test (per Design Choice 2) or its justification, and the benchmark justification.
4. Measure per-flag coverage and CRAP with coverage-report.cs and report it against the manifests.

KEY RULES:
- Shouldly through Encina.Testing.Shouldly; shared collection fixtures only, never IClassFixture.
- Tests of a provider-dependent feature cover all 10 providers.

REFERENCE FILES:
- tests/Encina.IntegrationTests (Collections.cs files)
- tests/Encina.ContractTests (dead letter store contract tests)
- .github/scripts/coverage-report.cs
```

</details>

---

### Phase 8: Documentation and Finalization

> **Goal**: Records complete; users can enable persistent delayed retries.

<details>
<summary><strong>Tasks</strong></summary>

1. XML doc comments on every new or changed public API.
2. `changelog.d/594-delayed-retry-stores.added.md` (and a `changed` fragment for the `Either` interface change).
3. README updates for the 8 provider packages, the recoverability configuration guide, `docs/INVENTORY.md`.
4. ADR if Design Choice 2 or 3 changes the interface as a policy (ADR number assigned when the ADR pull request opens).
5. `dotnet build --configuration Release` with zero warnings; `dotnet test` with every flag at its target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```text
CONTEXT:
All stores are implemented and tested. Documentation is written by the docs-writer agent following
the encina-docs skill; figures are cited with covref markers, never typed.

TASK:
1. Complete XML docs and PublicAPI files for every package touched.
2. Write the changelog fragments, the README updates and the configuration guide section.

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
| ADR-050 and its 2026-10-26 amendment | Delayed retries need a store and a factory when the flag is on | Startup check stays; this plan supplies the stores |
| ADR-001, ADR-006 | Railway Oriented Programming | Design Choice 3 |
| ADR-024 | SQLite out of the provider matrix | 10 providers, not 13 |
| ADR-018, ADR-021 | Cross-cutting, EventIds | Matrix below, ids in existing ranges |
| `AGENTS.md` section 3 | Time, async DB calls, errors not swallowed, error text not stored, gates fail closed | Design Choices 5 and 10 |
| ADR-062, ADR-054 D4 | Error categories; tenant tags on telemetry | Error table; decorator tag |
| #2254 contract note (2026-10-16) | Pending count and oldest pending entry on every persistent store | `GetBacklogAsync` |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IDelayedRetryStore`, `IDelayedRetryMessage`, `IDelayedRetryMessageFactory` | `src/Encina.Messaging/Recoverability/IDelayedRetryScheduler.cs` | The contract being implemented and changed |
| `DelayedRetryScheduler`, `DelayedRetryProcessor`, `DelayedRetryStartupCheck` | `src/Encina.Messaging/Recoverability/` | Consumers of the store |
| `AddEncinaDelayedRetryStore<TStore, TFactory>` | `RecoverabilityServiceCollectionExtensions.cs:256` | Registration |
| Dead letter stores and scripts | `src/Encina.{ADO,Dapper}.*/DeadLetter/`, `Encina.EntityFrameworkCore/DeadLetter/`, `Encina.MongoDB/DeadLetter/` | Structural reference per provider |
| `DeadLetterCleanupProcessor` | `src/Encina.Messaging/DeadLetter/` | Pattern for the purge processor |
| `InstrumentedDeadLetterStore` | `src/Encina.OpenTelemetry/MessagingStores/` | Pattern for the decorator |
| Shared integration collections | `tests/Encina.IntegrationTests` | `ADO-`, `Dapper-`, `EFCore-` x 3 and MongoDB |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` | 5459-5464 inside `MessagingRecoverability` (5450-5499, 5450-5458 used) | Cleanup processor and backlog report; no new range |
| `Encina.ADO.SqlServer`, `.PostgreSQL`, `.MySQL` | next free ids inside 3200-3249, 3250-3299, 3300-3349 | A handful per package; the allocation test verifies |
| `Encina.Dapper.SqlServer`, `.PostgreSQL`, `.MySQL` | next free ids inside 3350-3399, 3400-3449, 3450-3499 | Same |
| `Encina.EntityFrameworkCore` | next free ids inside 3000-3099 | Same |
| `Encina.MongoDB` | 3167+ inside 3100-3199 (3166 is the last used) | Same |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| New production files | ~45 | Per provider: message, factory, store; 6 SQL scripts; decorator; cleanup processor |
| Changed production files | ~20 | Interface, scheduler, processor, 8 `ServiceCollectionExtensions`, options, index creator, `Log.cs` files, fakes, PublicAPI |
| Coverage manifests | ~10 | One edit per package |
| Tests | ~40 | Unit, guard, contract, property, integration, DI, load or justification |
| Documentation | ~12 | READMEs, guide, INVENTORY, 2 changelog fragments |
| **Total** | **~127** | Thirteen pull requests P1-P13 (Phase 1 item 5), each at most 15 files and 1,500 lines counting tests, scripts, manifests, `PublicAPI` and fragments |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```text
You are implementing issue #594 - persistent IDelayedRetryStore for the 10 database providers.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library; pre-1.0, no backward compatibility, best solution always.
- Railway Oriented Programming: Either<EncinaError, T>; a Left from a store inside a processor fails
  the operation, never reports success.
- Database category: ADO.NET x3, Dapper x3, EF Core x3 (SQL Server, PostgreSQL, MySQL), MongoDB.
  SQLite is out of the matrix (ADR-024).

IMPLEMENTATION OVERVIEW:
1. Core: IDelayedRetryStore to Either, claim-aware GetPendingMessagesAsync, GetBacklogAsync (shape
   from #2254), TenantId/ModuleId, optional cleanup processor, fakes (answers to Design Choices 1-4 and 6-9; Design Choice 10 is settled).
2. ADO.NET, Dapper, EF Core, MongoDB stores with scripts, factories, registrations (UseRecoverability).
3. Decorator-based telemetry, backlog health input, per-provider DI tests.
4. Tests per flag across all 10 providers; docs and changelog.

KEY PATTERNS:
- TimeProvider injected; due = ExecuteAtUtc <= now AND ProcessedAtUtc IS NULL AND no live claim.
- MarkAsFailedAsync stores an error code or exception type, never raw text.
- Async database calls with CancellationToken; parameterized SQL.
- TryAdd registration so the application's store wins in any order.
- Shared collection fixtures for integration tests; Shouldly only.

REFERENCE FILES:
- src/Encina.Messaging/Recoverability/IDelayedRetryScheduler.cs
- src/Encina.Messaging/Recoverability/DelayedRetryProcessor.cs
- src/Encina.ADO.SqlServer/DeadLetter/DeadLetterStoreADO.cs
- src/Encina.OpenTelemetry/MessagingStores/InstrumentedDeadLetterStore.cs
- docs/plans/delayed-retry-stores-implementation-plan-594.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | Write-heavy, time-sensitive reads; a cached pending list would return claimed rows |
| 2 | OpenTelemetry | ✅ Phase 6 | `InstrumentedDelayedRetryStore` decorator covers all 10 providers; metric and span names for the backlog are owned by #2254; a tenant tag only when multi-tenancy is on (ADR-054 D4) |
| 3 | Structured Logging | ✅ Phases 1-5 | `[LoggerMessage]` ids inside existing ranges (5459-5464 and the provider ranges), packed sequentially |
| 4 | Health Checks | ✅ Phase 6 | Backlog depth and oldest pending entry from `GetBacklogAsync`, shared with #2254 |
| 5 | Validation | ✅ Phases 2-5 | Guard tests for `batchSize`, `Guid.Empty` and null messages; the payload is validated upstream |
| 6 | Resilience | ❌ N/A | Transient database retry belongs to the existing database resilience options (`src/Encina/Database/DatabaseResilienceOptions.cs`), shared by all stores; the store adds no second layer. A gap found by the integration tests is opened as a debt issue then |
| 7 | Distributed Locks | ❌ N/A | Only if Design Choice 2 is B, in which case the processor takes an `IDistributedLockProvider` lock and this row becomes ✅ |
| 8 | Transactions | ✅ Phases 2-5 | The add runs on its own connection and statement **outside** the request transaction (Design Choice 7 A) because the retry is scheduled after the handler failed and a joined row would roll back with it; the claim is its own statement; the EF store never uses the scoped change tracker |
| 9 | Idempotency | ✅ Phases 2-5 | Unique `(RecoverabilityContextId, DelayedRetryAttempt)`; a duplicate add returns `Right(false)` (Design Choice 8 A, pending) |
| 10 | Multi-Tenancy | ✅ Phase 1 | Typed `TenantId` column (Design Choice 4) with a `(tenant, execute_at)` index |
| 11 | Module Isolation | ✅ Phase 1 | Typed `ModuleId` column (Design Choice 4) |
| 12 | Audit Trail | ❌ N/A | Infrastructure retries are not auditable events; the dead letter capture already audits the terminal outcome |

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| Default recoverability registration (#2248) | ✅ Closed | `DelayedRetryStartupCheck` and `AddEncinaDelayedRetryStore` exist |
| Persistent dead letter stores (#583) | ✅ Closed | Structural reference |
| Backlog member shape (#2254) | Open | Design Choice 9 recommends `GetBacklogAsync`; re-check before P2 |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Redis implementation | #595 | Follows Design Choices 2, 6 and 8 (claim, terminal and cancel semantics, idempotency key); depends on P1, P2 and P4 |
| Routing slip stores | #590 | Same `Either` question; answers must match |

---

## Next Steps

1. Link this plan from #594 and ask the maintainer to answer Design Choices 1 to 4 and 6 to 9 when work starts (3 together with #590). #595 (Redis) is blocked until the answers to D1 to D4 and the merge of P1, P2 and P4 (the contract PRs and the decorator PR).
2. Coordinate the `GetBacklogAsync` shape with #2254 before P2.
3. Implement Phases 1 to 8 as the pull requests P1-P13 of Phase 1 item 5, `Fixes #594` on the last (P13).
