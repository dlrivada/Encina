# Implementation Plan: Transactional Safety for Saga Compensation

> **Issue**: [#720](https://github.com/dlrivada/Encina/issues/720)
> **Type**: Feature
> **Complexity**: Very high (9 phases, 10 database providers, about 70 files in 7 pull requests)
> **Estimated Scope**: ~1,400 lines of production code + ~3,200 lines of tests

---

## Summary

Make saga compensation recoverable: each compensation step and the saga state update that records it are written in one transaction, a failing compensation leaves a persisted failure state (not only a dead letter), and a configurable retry policy re-runs it, automatically through a background processor or manually. A `SagaCompensationFailed` notification lets monitoring react. The mode is opt-in (`SagaOptions`), off by default.

**Today's code** (main 7a0049c07): `SagaRunner.HandleStepFailedAsync` persists `Compensating` through `SagaOrchestrator.StartCompensationAsync`, then `SagaRunner.CompensateAsync` runs every compensation delegate in process (a failing one does not stop the others and only the first failure code is kept), and only afterwards `CompleteCompensationAsync` records all steps with `SagaOrchestrator.CompensateStepAsync`. When a delegate failed, `SagaOrchestrator.FailAsync` moves the saga to `Failed` and dead-letters it (`DeadLetterOptions.IntegrateWithSagas`). The per-step progress is not persisted while delegates run, there is no attempt count, no next-retry time and no retry.

**The issue's `IUnitOfWork` is not reachable from Messaging**: `Encina.Messaging` references only `Encina` core (`src/Encina.Messaging/Encina.Messaging.csproj`). The transaction mechanism in this package is `IDbTransactionAccessor` with leases (`src/Encina.Messaging/Transactions/`), the one `TransactionPipelineBehavior` uses and in which the stores of a scope enlist (ADO/Dapper through the accessor, MongoDB through `IMongoSessionAccessor`, EF Core through the `DbContext` and `SaveChangesAsync`). The plan builds on it.

**Not in scope**: replay idempotency of a compensation step (#736), saga audit (#750), tenant column (#738), external effects through the outbox (an option of Dilemma D1, a later extension), concurrent retry across instances (deferred issue, see the matrix).

**Affected packages**: `Encina.Messaging`, `Encina.ADO.SqlServer|PostgreSQL|MySQL`, `Encina.Dapper.SqlServer|PostgreSQL|MySQL`, `Encina.EntityFrameworkCore`, `Encina.MongoDB`, `Encina.Testing.Fakes`.

**Provider category**: Database (10); `SagaStoreEF` serves three databases.

**Shared with the saga family**: #738 (tenant column), #1237 (correlation columns) and #736 (replay idempotency) also add columns and store methods to the same scripts and stores. This plan owns `RetryCount`, `NextRetryAtUtc` and `GetCompensationRetryDueAsync`; the plans rebase onto each other in merge order. EventId slice 2206-2229 inside `MessagingSaga` (2200-2299).

---

## Design Choices

<details>
<summary><strong>1. Scope of the atomicity guarantee (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The transaction covers the saga state and database-local compensation effects only; external effects are at least once and rely on the idempotency key of #736** | Honest guarantee; no new component | The failure window of external effects remains |
| **B) Record the compensation intent in the saga state and publish the external effect through the outbox in the same transaction** | No lost compensation even for external effects | Requires the outbox module (ADR-050) and turns a compensation delegate into a message handler |

### Chosen Option: **A — Database-local atomicity, external effects at least once** (recommended, pending the maintainer)

### Rationale

- Option A wins when compensations are database writes or call idempotent APIs. Option B wins when compensations must never be lost and the application already uses the outbox; it is a later extension on top of A.
- **Class:** Dilemma

</details>

<details>
<summary><strong>2. Where the retry state is stored (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Two typed columns `RetryCount` and `NextRetryAtUtc` on `ISagaState` and the 10 providers, with a store query `GetCompensationRetryDueAsync`; the failing step stays in `CurrentStep` (the compensation cursor) and the error code in `ErrorMessage`; the status stays `Compensating` while retries remain** | Queryable and indexable by the retry processor; names follow `AGENTS.md` section 4 | Schema change in 10 providers, shared with #738 and #1237 |
| **B) Serialize attempts and next retry into the existing `Metadata` text column (it exists in the SQL scripts and in EF Core's `SagaState`, not in `ISagaState`)** | No new column | Not queryable without scanning; `Metadata` is not part of the provider-coherent interface |
| **C) A new status `CompensationFailed` plus A's columns** | The failure is visible in the status | A new status value touches every consumer of `SagaStatus`, the stuck-saga query and the health check |

### Chosen Option: **A — Typed columns, status stays `Compensating`** (recommended, pending the maintainer)

### Rationale

- Option A wins when the retry processor must find due sagas with an indexed query. Option C wins if operators filter dashboards by status and accept the wider change. Option B wins only if schema changes must be avoided.
- **Class:** Dilemma

</details>

<details>
<summary><strong>3. How the retry runs: built-in policy or Polly (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A built-in `SagaCompensationRetryProcessor` (a `BackgroundService`, like `DelayedRetryProcessor`) with `MaxRetries`, `InitialRetryDelay` and exponential `BackoffFactor` in `SagaOptions`** | No dependency; state is persisted, so a retry survives a restart; same shape as the recoverability processor | Encina maintains a small policy |
| **B) Delegate to Polly (`Encina.Polly`)** | Rich policies | Polly retries in memory: a restart loses the retry, which is the problem this issue solves; `Encina.Messaging` does not reference Polly |

### Chosen Option: **A — Built-in persisted retry** (recommended, pending the maintainer)

### Rationale

- Option A wins because the retry must be durable. Option B wins if the maintainer accepts in-memory retries only, as an add-on for transient failures inside one step.
- **Class:** Dilemma

</details>

<details>
<summary><strong>4. Order and stopping on a failed compensation (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) With the transaction mode on: stop at the first failed step, persist the failure, and retry from that step (reverse order is preserved)** | Compensations that depend on each other's order stay correct; the cursor is exact | The later (earlier-in-forward-order) compensations wait for the retry |
| **B) Keep today's behavior: continue with the remaining compensations and report the first failure** | Maximum progress | Re-running only the failed step later may violate the order; the per-step cursor is ambiguous |

### Chosen Option: **A — Stop and retry from the failed step** (recommended, pending the maintainer)

### Rationale

- Option A wins when compensations are ordered (refund before cancel). Option B wins when compensations are independent and the fastest cleanup matters more.
- With the mode off, today's behavior is unchanged (choice 5).
- **Class:** Dilemma

</details>

<details>
<summary><strong>5. The transaction mode is opt-in</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `SagaOptions.CompensationTransaction` (default `false`); with it off nothing changes** | Pay-for-what-you-use | A mode flag to document |
| **B) Always on** | One behavior | Breaks applications whose compensation delegates cannot run inside a transaction |

### Chosen Option: **A — Opt-in**

### Rationale

- **Class:** Settled
- **Settled by:** `AGENTS.md` section 1 (every feature opt-in) and section 3 (every messaging pattern optional and disabled by default).

</details>

<details>
<summary><strong>6. A failed compensation is persisted and surfaced, never reported as success</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Persist the failure state, retry per policy, dead-letter when exhausted (`FailAsync`)** | Nothing reports success; the dead letter stays the last resort | More states to test |
| **B) Log and continue** | Simple | Swallows an error in background infrastructure |

### Chosen Option: **A — Persist, retry, then dead-letter**

### Rationale

- **Class:** Settled
- **Settled by:** `AGENTS.md` section 3 (errors are never swallowed in background infrastructure) and the existing `SagaOrchestrator.FailAsync` dead-letter behavior.

</details>

<details>
<summary><strong>7. The transaction mechanism</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A lease from `IDbTransactionAccessor` around each compensation step and its `CompensateStepAsync` write, committed or rolled back as `TransactionPipelineBehavior` does** | Reachable from Messaging; every store of the scope enlists | A step that calls a different database is not covered (D1) |
| **B) `IUnitOfWork` from `Encina.DomainModeling`** | The wording of the issue | `Encina.Messaging` does not reference DomainModeling; adding the reference widens the package |

### Chosen Option: **A — `IDbTransactionAccessor` leases**

### Rationale

- **Class:** Settled
- **Settled by:** the precedent in the same package, `src/Encina.Messaging/TransactionPipelineBehavior.cs` (lease begin, commit on `Right`, rollback on `Left` or exception, commit and rollback with `CancellationToken.None`), and `AGENTS.md` section 3 (provider coherence: abstractions live in `Encina.Messaging`).
- `ISagaStore.SaveChangesAsync` remains the commit point for EF Core.

</details>

<details>
<summary><strong>8. Events carry the error code only; names</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `SagaCompensationFailed : INotification` with saga id, step index, attempt, error code, `OccurredAtUtc`; published through `IEncina.Publish`; a `Left` from publish fails the operation** | Pattern of `AISystemReclassifiedNotification`; monitoring subscribes | One notification type |
| **B) A log line only** | None | The issue asks for an event |

### Chosen Option: **A — A notification with the error code**

### Rationale

- **Class:** Settled
- **Settled by:** `AGENTS.md` section 3 (`EncinaError.Message` never reaches logs or plaintext storage; a `Left` from `Publish` fails that operation); names follow section 4 (`RetryCount`, `NextRetryAtUtc`, `AtUtc` suffix) and the `saga.*` family in `SagaErrorCodes`.

</details>

---

## Implementation Phases

### Phase 1: Core Models, Options and Contract

> **Goal**: The contract in `Encina.Messaging`.

<details>
<summary>Tasks</summary>

1. `SagaOptions` (`src/Encina.Messaging/Sagas/SagaOrchestrator.cs`, line 615): `CompensationTransaction` (bool, false), `MaxCompensationRetries` (int, 3), `CompensationRetryDelay` (TimeSpan, 30 s), `CompensationRetryBackoffFactor` (double, 2.0), `CompensationRetryBatchSize` (int, 100), `CompensationRetryCheckInterval` (TimeSpan, 30 s); a validator class (`IValidateOptions<SagaOptions>`) rejecting non-positive values.
2. `ISagaState`: add `int RetryCount { get; set; }` and `DateTime? NextRetryAtUtc { get; set; }`.
3. `ISagaStateFactory.Create(...)`: no change (defaults zero and null).
4. `ISagaStore`: add `GetCompensationRetryDueAsync(int batchSize, CancellationToken)` returning sagas with `Status = Compensating` and `NextRetryAtUtc <= now`.
5. `SagaCompensationFailed` (sealed record, `INotification`): `SagaId`, `SagaType`, `StepIndex`, `Attempt`, `ErrorCode`, `WillRetry`, `OccurredAtUtc`.
6. `SagaErrorCodes`: `saga.compensation_retry_exhausted`.
7. `PublicAPI.Unshipped.txt` of `Encina.Messaging`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```
CONTEXT:
- Issue #720; plan docs/plans/saga-compensation-transaction-implementation-plan-720.md. Dilemmas D1-D4 are recommended, pending the maintainer: follow the Maintainer Decisions section if it exists.
- Encina.Messaging does not reference Encina.DomainModeling; transactions use IDbTransactionAccessor leases.

TASK:
Implement the Phase 1 tasks.

KEY RULES:
- Options validated by an IValidateOptions validator registered with the saga options; timestamps use the AtUtc suffix.
- The notification carries the error code only, never EncinaError.Message.
- Update PublicAPI.Unshipped.txt; zero warnings; no [Obsolete].

REFERENCE FILES:
- src/Encina.Messaging/Sagas/SagaOrchestrator.cs (SagaOptions, line 615)
- src/Encina.Messaging/Sagas/ISagaState.cs
- src/Encina.Compliance.AIAct/Notifications/AISystemReclassifiedNotification.cs
```

</details>

---

### Phase 2: Orchestrator and Runner

> **Goal**: Per-step transactional compensation with persisted progress.

<details>
<summary>Tasks</summary>

1. `SagaRunner.CompensateAsync`: when `CompensationTransaction` is on, for each step in reverse order: take a lease from `IDbTransactionAccessor`, run the delegate, call `SagaOrchestrator.CompensateStepAsync`, `SaveChangesAsync`, commit; on a delegate failure roll back, call a new `SagaOrchestrator.RecordCompensationFailureAsync(sagaId, errorCode, ct)` (increments `RetryCount`, sets `NextRetryAtUtc = now + delay * factor^RetryCount`, keeps `Compensating`) in a fresh write, publish `SagaCompensationFailed`, and stop (D4). With the mode off the current behavior is kept.
2. `SagaOrchestrator.RecordCompensationFailureAsync`: when `RetryCount` reaches `MaxCompensationRetries`, call `FailAsync` (dead letter) with `saga.compensation_retry_exhausted`.
3. `SagaOrchestrator.RetryCompensationAsync(sagaId, ct)`: for the processor and for manual use; re-enters at the persisted cursor.
4. Handle the existing swallow: the runner must honour the `Left` of these calls (#1581 is the same defect for the forward path; both can be fixed together or the plan's calls are written correctly from the start).
5. `[LoggerMessage]` entries in the `MessagingSaga` range, ids 2206-2229.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```
CONTEXT:
- Phase 1 added the options, the state fields, the store method and the notification (issue #720).

TASK:
Implement the Phase 2 tasks in SagaRunner and SagaOrchestrator.

KEY RULES:
- Commit and rollback with CancellationToken.None, as TransactionPipelineBehavior does.
- A Left from any store or publish call is returned, never discarded.
- Only error codes are persisted and logged; exceptions via ForLogging(); time from TimeProvider.
- EventIds 2206-2229, sequential.

REFERENCE FILES:
- src/Encina.Messaging/Sagas/LowCeremony/SagaRunner.cs (CompensateAsync, line 299)
- src/Encina.Messaging/TransactionPipelineBehavior.cs
- src/Encina.Messaging/Transactions/IDbTransactionAccessor.cs
- src/Encina.Messaging/Sagas/SagaOrchestrator.cs
```

</details>

---

### Phase 3: Retry Processor and DI

> **Goal**: Durable automatic retry, opt-in and complete by `ValidateOnBuild`.

<details>
<summary>Tasks</summary>

1. `SagaCompensationRetryProcessor` (`BackgroundService`, `src/Encina.Messaging/Sagas/`): a `PeriodicTimer` loop; per iteration it catches failures and keeps running (an `OperationCanceledException` from the stopping token ends the loop); loads due sagas with `GetCompensationRetryDueAsync`, calls `RetryCompensationAsync` for each; a failing retry is recorded by the orchestrator, not swallowed. Time from `TimeProvider`.
2. Registration in `RegisterSagas` (`src/Encina.Messaging/MessagingServiceCollectionExtensions.cs`, line 355) only when `CompensationTransaction` is on: the processor, the options validator and the notification publisher dependency, with a DI test using `ValidateOnBuild` and `ValidateScopes`.
3. Startup check: the mode needs a store that implements `GetCompensationRetryDueAsync` (all 10 do after Phases 4-5).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```
CONTEXT:
- Issue #720. The retry processor follows DelayedRetryProcessor.

TASK:
Implement SagaCompensationRetryProcessor and its registration.

KEY RULES:
- Background loops catch per iteration and never let an exception escape (AGENTS.md section 3); cancellation from the stopping token ends the loop.
- TryAdd registrations; ValidateOnBuild and ValidateScopes DI test with the mode on and off.

REFERENCE FILES:
- src/Encina.Messaging/Recoverability/DelayedRetryProcessor.cs
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs (RegisterSagas)
```

</details>

---

### Phase 4: ADO.NET and Dapper Providers (6)

<details>
<summary>Tasks</summary>

1. SQL: `RetryCount INT NOT NULL DEFAULT 0` and `NextRetryAtUtc` (`DATETIME2(7)` / `TIMESTAMP` / `DATETIME(6)` as the neighbouring `TimeoutAtUtc`) plus an index `(Status, NextRetryAtUtc)` in `Scripts/003_CreateSagaStatesTable.sql` and `Scripts/000_CreateAllTables.sql` of the six packages (12 files).
2. `SagaState` (6) and `SagaStateFactory` (6): the two properties.
3. `SagaStoreADO` (3) and `SagaStoreDapper` (3): include the columns in `INSERT`, `UPDATE`, `SELECT`; `GetCompensationRetryDueAsync` with `TOP (@n)` on SQL Server and `LIMIT @n` on PostgreSQL and MySQL; commands enlist in the scope's transaction through the accessor as the other methods do; asynchronous calls with a `CancellationToken`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```
CONTEXT:
- Phases 1-3 done (issue #720). The six implementations behave identically and differ only in dialect.

TASK:
Apply the Phase 4 tasks to every ADO.NET and Dapper package.

KEY RULES:
- SQL Server TOP (@n); PostgreSQL LIMIT @n and case-sensitive identifiers; MySQL LIMIT @n and backticks.
- Never IDbConnection.Open(); always the async overloads with a CancellationToken.
- Do not drop columns; no migration helper.

REFERENCE FILES:
- src/Encina.ADO.PostgreSQL/Sagas/SagaStoreADO.cs
- src/Encina.Dapper.MySQL/Sagas/SagaStoreDapper.cs
- src/Encina.ADO.SqlServer/Scripts/003_CreateSagaStatesTable.sql
```

</details>

---

### Phase 5: EF Core and MongoDB Providers

<details>
<summary>Tasks</summary>

1. EF Core: `SagaState` properties, `SagaStateConfiguration` (`HasIndex(x => new { x.Status, x.NextRetryAtUtc })`), `SagaStateFactory`, `SagaStoreEF.GetCompensationRetryDueAsync`; one implementation for the three databases. No EF migration (pre-1.0).
2. MongoDB: `SagaState` fields, `SagaStoreMongoDB.GetCompensationRetryDueAsync`, the compound index in `MongoDbIndexCreator.CreateSagaIndexesAsync` (line 163). Multi-document transactions need a replica set; the integration fixture states it.
3. `Encina.Testing.Fakes`: `FakeSagaState`/`FakeSagaStore` support the fields and the query.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```
CONTEXT:
- Phases 1-4 done (issue #720).

TASK:
Implement the Phase 5 tasks.

KEY RULES:
- The behavior is identical to the SQL stores; the contract tests run against every store and the fake.
- MongoDB uses IMongoSessionAccessor for the transaction when one is active.

REFERENCE FILES:
- src/Encina.EntityFrameworkCore/Sagas/SagaStoreEF.cs
- src/Encina.MongoDB/Sagas/SagaStoreMongoDB.cs
- src/Encina.Testing.Fakes/Stores/FakeSagaStore.cs
```

</details>

---

### Phase 6: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. **Validation**: the options validator (Phase 1).
2. **Resilience**: the persisted retry policy of Phase 2-3.
3. **Transactions**: the feature itself.
4. **Deferred**: health check for sagas stuck in failed compensation (extend `SagaHealthCheck`, `src/Encina.Messaging/Health/SagaHealthCheck.cs`); concurrent retry across instances (a claim or lock; ADR-050 lists the lock for timeouts on several instances; issue file `artifacts/issues/saga-compensation-retry-concurrency.md`, related #2213); replay idempotency (#736); audit (#750); tenant (#738).

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```
CONTEXT:
- Cross-cutting matrix of docs/plans/saga-compensation-transaction-implementation-plan-720.md.

TASK:
Confirm the included functions are wired and the deferred ones are out of the diff.

KEY RULES:
- No lock, tenant or audit code in this plan.

REFERENCE FILES:
- AGENTS.md section 6
```

</details>

---

### Phase 7: Observability

<details>
<summary>Tasks</summary>

1. Counter `encina.saga.compensation_failed` (`Counter<long>`, tags `saga.type`, `will_retry`) and activity tags `saga.compensation.step`, `saga.compensation.attempt` on `SagaActivitySource` (no new span).
2. Log messages (ids 2206-2229); run `EncinaEventIdAllocationTests`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```
CONTEXT:
- Issue #720; slice 2206-2229 of MessagingSaga.

TASK:
Add the counter, tags and log messages.

KEY RULES:
- [LoggerMessage] source generator; sequential ids; error code only; the saga id is a span tag only.

REFERENCE FILES:
- src/Encina.Messaging/Diagnostics/SagaActivitySource.cs
- src/Encina.Messaging/Diagnostics/MessagingStoreMetrics.cs
```

</details>

---

### Phase 8: Testing

<details>
<summary>Tasks</summary>

1. **Unit**: each failure point against a fake store (step fails, commit fails, publish fails, retry exhausted, mode off keeps today's behavior), options validator, processor loop (fake time), notification content.
2. **Guard**: new public members.
3. **Contract**: `ISagaStore` retry state and query identical across all stores.
4. **Property (FsCheck)**: after a failure at any point the persisted state and the compensation outcome do not diverge.
5. **Integration**: all 10 providers through the shared collections with real transactions (rollback leaves the step uncompensated and the failure recorded); never a `.md`.
6. **Load / Benchmark**: justification files `tests/Encina.LoadTests/Sagas/CompensationTransaction.md` and `tests/Encina.BenchmarkTests/Sagas/CompensationTransaction.md` (failure path, not a hot path).
7. **Per-file targets** with one-sentence justifications in the manifests of every touched package.

</details>

<details>
<summary>Prompt for AI Agents — Phase 8</summary>

```
CONTEXT:
- Phases 1-7 implemented (issue #720).

TASK:
Write the tests of Phase 8 Tasks and fill the manifest targets.

KEY RULES:
- Real databases for all 10 providers; shared [Collection] fixtures; ClearAllDataAsync in InitializeAsync.
- Shouldly via Encina.Testing.Shouldly; a test never swallows an exception; FakeTimeProvider for the processor.

REFERENCE FILES:
- tests/Encina.UnitTests/Messaging/Sagas/SagaRunnerTests.cs
- docs/testing/integration-tests.md
```

</details>

---

### Phase 9: Documentation & Finalization

<details>
<summary>Tasks</summary>

1. XML docs; `PublicAPI.Unshipped.txt` per package; `changelog.d/720-saga-compensation-transaction.added.md`; the saga guide `docs/messaging/sagas.md` (through docs-writer); an ADR recording D1-D4 (read `docs/architecture/adr/index.md` for the next free number).
2. `dotnet build --configuration Release` with 0 warnings; `dotnet test` with every flag at its target.

</details>

<details>
<summary>Prompt for AI Agents — Phase 9</summary>

```
CONTEXT:
- Issue #720 implemented and tested.

TASK:
Write the documentation set.

KEY RULES:
- Pages go through the docs-writer agent; coverage figures only with covref markers.

REFERENCE FILES:
- changelog.d/README.md
```

</details>

---

### Proposed split into pull requests (limit 15 files and 1500 lines each)

| PR | Content | Approx. files | Approx. lines |
|----|---------|---------------|---------------|
| 1 | Phase 1: options, validator, state fields, store method, notification, errors | 9 | 500 |
| 2 | Phase 2: orchestrator and runner | 6 | 1,000 |
| 3 | Phase 3: processor and DI | 5 | 600 |
| 4 | Phase 4a: ADO.NET (3) | 15 | 700 |
| 5 | Phase 4b: Dapper (3) | 15 | 700 |
| 6 | Phase 5 and 7: EF Core, MongoDB, fakes, observability | 12 | 700 |
| 7 | Phase 8 and 9: contract, property, integration, docs | 14 | 1,400 |

---

## Research

### Relevant Standards & Specifications

| Reference | What it defines | Use in this feature |
|-----------|-----------------|---------------------|
| Saga pattern (Garcia-Molina and Salem, 1987) | Compensating transactions, semantic undo | Reverse-order compensation |
| Temporal, Durable Functions, MassTransit | Durable compensation progress and retry | Comparable products named in the issue |
| `AGENTS.md` section 3 | Errors never swallowed; `EncinaError.Message` never stored; timer loops never throw | Choices 6 and 8, processor |
| ADR-050 | Module requirements, lock for timeouts on several instances | Deferred concurrency |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `SagaRunner`, `SagaOrchestrator`, `SagaOptions` | `src/Encina.Messaging/Sagas/` | Compensation flow and options |
| `IDbTransactionAccessor` and leases | `src/Encina.Messaging/Transactions/` | Per-step transaction |
| `TransactionPipelineBehavior` | `src/Encina.Messaging/TransactionPipelineBehavior.cs` | Commit/rollback semantics |
| `DelayedRetryProcessor` | `src/Encina.Messaging/Recoverability/DelayedRetryProcessor.cs` | Retry processor pattern |
| `DeadLetterOptions.IntegrateWithSagas`, `FailAsync` | `src/Encina.Messaging/DeadLetter/`, `SagaOrchestrator.cs` | Last resort when retries are exhausted |
| `AISystemReclassifiedNotification` | `src/Encina.Compliance.AIAct/Notifications/` | Notification pattern |
| Saga stores | `src/Encina.ADO.*/Sagas/`, `Encina.Dapper.*/Sagas/`, `Encina.EntityFrameworkCore/Sagas/`, `Encina.MongoDB/Sagas/` | Columns and query |
| `FakeSagaStore` | `src/Encina.Testing.Fakes/Stores/FakeSagaStore.cs` | Test double |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` (saga) | 2200-2299 | Existing `MessagingSaga`; `SagaStoreLog` uses 2200-2205 |
| **This plan (#720)** | **2206-2229** | **Compensation transaction, failure, retry; slice within the saga family: #720 2206-2229, #738 2230-2249, #750 2250-2274, #1237 2275-2299** |

No new range; `Encina.Messaging` already maps to `MessagingSaga` in `EncinaEventIdAllocationTests`.

### Provider Matrix

| Provider | Columns + state | Retry query | Transaction enlistment | Integration test |
|----------|-----------------|-------------|------------------------|------------------|
| ADO-SqlServer, ADO-PostgreSQL, ADO-MySQL | Required | Required | `IDbTransactionAccessor` | `[Collection("ADO-<Db>")]` |
| Dapper-SqlServer, Dapper-PostgreSQL, Dapper-MySQL | Required | Required | `IDbTransactionAccessor` | `[Collection("Dapper-<Db>")]` |
| EFCore-SqlServer, EFCore-PostgreSQL, EFCore-MySQL | one `SagaStoreEF` | Required | `DbContext` + `SaveChangesAsync` | `[Collection("EFCore-<Db>")]` x3 |
| MongoDB | Required | Required | `IMongoSessionAccessor` (replica set) | MongoDB fixture |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Messaging core | ~12 | Options, validator, state fields, store contract, notification, runner, orchestrator, processor, DI, logs |
| ADO.NET x3 and Dapper x3 | ~36 | State, factory, store, 2 scripts per package |
| EF Core, MongoDB, fakes | ~10 | |
| Tests | ~16 | Unit, guard, contract, property, 10 integration (grouped), two justifications |
| Documentation | ~3 | Fragment, guide, ADR |
| **Total** | **~77 across 7 pull requests** | |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no [Obsolete], no migrations; XML docs; zero warnings.
- Issue #720: transactional, persisted, retryable saga compensation across the 10 database providers. Plan: docs/plans/saga-compensation-transaction-implementation-plan-720.md; Dilemmas D1-D4 are recommended, pending the maintainer.

IMPLEMENTATION OVERVIEW:
1. Options, validator, state fields RetryCount and NextRetryAtUtc, store query, notification (Phase 1).
2. Per-step transactional compensation in SagaRunner and SagaOrchestrator (Phase 2).
3. Durable retry processor and DI (Phase 3).
4. Providers: ADO/Dapper (Phase 4), EF Core, MongoDB, fakes (Phase 5).
5. Cross-cutting, observability, tests, docs (Phases 6-9).

KEY PATTERNS:
- IDbTransactionAccessor leases as in TransactionPipelineBehavior; commit/rollback with CancellationToken.None.
- Every Left (store, publish) is returned; only error codes are stored and logged; exceptions via ForLogging().
- Background loop catches per iteration; TimeProvider for time; TryAdd registrations with a ValidateOnBuild DI test.
- SQL: SQL Server TOP (@n); PostgreSQL and MySQL LIMIT @n; asynchronous calls with a CancellationToken.
- EventIds 2206-2229 inside the registered MessagingSaga range.

REFERENCE FILES:
- src/Encina.Messaging/Sagas/LowCeremony/SagaRunner.cs
- src/Encina.Messaging/Sagas/SagaOrchestrator.cs
- src/Encina.Messaging/TransactionPipelineBehavior.cs
- src/Encina.Messaging/Recoverability/DelayedRetryProcessor.cs
- src/Encina.EntityFrameworkCore/Sagas/SagaStoreEF.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: saga state needs read-your-writes; a cache would break it |
| 2 | OpenTelemetry | ✅ | Phase 7: `encina.saga.compensation_failed` counter and activity tags on `SagaActivitySource` |
| 3 | Structured Logging | ✅ | Phase 2 and 7: messages in 2206-2229, error code only |
| 4 | Health Checks | ⏭️ | Deferred: sagas stuck in failed compensation are checkable; extend `SagaHealthCheck`; issue to be opened with the processor work |
| 5 | Validation | ✅ | Phase 1: the retry options validator |
| 6 | Resilience | ✅ | Phase 2-3: the persisted retry policy (built-in, D3) |
| 7 | Distributed Locks | ⏭️ | Deferred: concurrent retry of one saga across instances; issue file `artifacts/issues/saga-compensation-retry-concurrency.md`, related #2213 |
| 8 | Transactions | ✅ | This is the transaction feature |
| 9 | Idempotency | ⏭️ | Deferred: replay idempotency of a compensation step is #736; the schema is coordinated |
| 10 | Multi-Tenancy | ⏭️ | Deferred: `TenantId` on the saga row is #738 |
| 11 | Module Isolation | ❌ | Not applicable: saga rows have no module scope (#747 is separate) |
| 12 | Audit Trail | ⏭️ | Deferred: saga transition audit is #750 |

---

## Prerequisites & Dependencies

- #1581 (SagaRunner discards the `Left` of the orchestrator's persisting calls) touches the same lines; fix it first or with Phase 2.
- #736, #738 and #1237 change the same scripts and stores: agree the merge order; this plan's columns are independent of theirs.
- #2208 (timeouts never enforced) concerns `TimeoutAsync`; independent, but the compensation retry loop follows its processor design.

---

## Next Steps

1. The maintainer answers D1 to D4 when work on #720 starts; the answers go into `## Maintainer Decisions` and the markers are removed.
2. Open the deferred issues (health check, retry concurrency) and link them from #720.
3. Brief pull request 1 first.
