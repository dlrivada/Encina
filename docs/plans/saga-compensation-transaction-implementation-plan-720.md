# Implementation Plan: Transactional Safety for Saga Compensation

> **Issue**: [#720](https://github.com/dlrivada/Encina/issues/720)
> **Type**: Feature
> **Complexity**: Very high (9 phases, 10 database providers, about 100 files in 15 pull requests)
> **Estimated Scope**: ~1,400 lines of production code + ~3,200 lines of tests

---

## Summary

Make saga compensation recoverable: each compensation step and the saga state update that records it are written in one transaction, a failing compensation leaves a persisted failure state (not only a dead letter), and a configurable retry policy re-runs it, automatically through a background processor or manually. A `SagaCompensationFailed` notification lets monitoring react. The mode is opt-in (`SagaOptions`), off by default.

**Today's code** (main 7a0049c07): `SagaRunner.HandleStepFailedAsync` persists `Compensating` through `SagaOrchestrator.StartCompensationAsync`, then `SagaRunner.CompensateAsync` runs every compensation delegate in process (a failing one does not stop the others and only the first failure code is kept), and only afterwards `CompleteCompensationAsync` records all steps with `SagaOrchestrator.CompensateStepAsync`. When a delegate failed, `SagaOrchestrator.FailAsync` moves the saga to `Failed` and dead-letters it (`DeadLetterOptions.IntegrateWithSagas`). The per-step progress is not persisted while delegates run, there is no attempt count, no next-retry time and no retry.

**The issue's `IUnitOfWork` is not reachable from Messaging**: `Encina.Messaging` references only `Encina` core (`src/Encina.Messaging/Encina.Messaging.csproj`). The transaction mechanism in this package is `IDbTransactionAccessor` with leases (`src/Encina.Messaging/Transactions/`), the one `TransactionPipelineBehavior` uses and in which the stores of a scope enlist (ADO/Dapper through the accessor, MongoDB through `IMongoSessionAccessor`, EF Core through the `DbContext` and `SaveChangesAsync`). The plan builds on it.

**Not in scope**: replay idempotency of a compensation step (#736), saga audit (#750), tenant column (#738), external effects through the outbox (an option of Dilemma D1, a later extension), concurrent retry across instances (deferred issue, see the matrix).

**Affected packages**: `Encina.Messaging`, `Encina.ADO.SqlServer|PostgreSQL|MySQL`, `Encina.Dapper.SqlServer|PostgreSQL|MySQL`, `Encina.EntityFrameworkCore`, `Encina.MongoDB`, `Encina.Testing.Fakes`, `Encina.Testing.FsCheck` (`ArbitrarySagaState`), plus the test doubles listed in Design Choice 10. The automatic retry needs a registry of saga definitions by saga type (Design Choice 9, Dilemma D5).

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
- **Who owns the commit and the dependencies** (verified on origin/main: `SagaRunner` holds only the orchestrator, `IRequestContextAccessor` and a logger, `SagaRunner.cs` lines 22-24, and `SagaOrchestrator` never calls `SaveChangesAsync`): the runner takes the lease and owns the transaction boundary; the orchestrator owns the store, so it gets one new method, `SagaOrchestrator.CommitAsync(CancellationToken)`, that calls `ISagaStore.SaveChangesAsync` and returns its `Either`. The runner never touches `ISagaStore`. The constructor changes, all listed in `PublicAPI.Unshipped.txt` (remove the old line, add the new one): `SagaRunner` gains `IDbTransactionAccessor? transactionAccessor`, `IEncina encina` (publishes `SagaCompensationFailed`, Design Choice 8) and `IOptions<SagaOptions> options`, and an optional `TimeProvider? timeProvider = null`; `SagaOrchestrator` gains no dependency (it already holds `SagaOptions` and a `TimeProvider`). With `CompensationTransaction` on and no `IDbTransactionAccessor` registered, startup validation fails with a clear message.

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
- **Publish failure after the failure state is stored**: the order is persist first (the failure state with the incremented `RetryCount` and `NextRetryAtUtc`), publish second. A `Left` from `IEncina.Publish` is returned by the failure-recording operation (nothing reports success) but the state is already stored, so the retry still happens: the processor finds the persisted row. A unit test covers "persist succeeded, publish failed": the operation returns the `Left` and a later `GetCompensationRetryDueAsync` still returns the saga.

</details>

<details>
<summary><strong>9. How the retry finds the compensation delegates after a restart (Dilemma D5)</strong></summary>

Compensation delegates are closures of the application's `BuiltSagaDefinition<TData>` that the application passes to `SagaRunner.RunAsync`. `SagaOrchestrator` and a background processor have only the persisted row (the saga type name and the serialized data). Verified on origin/main: `BuiltSagaDefinition<TData>` has `SagaType` and `Steps` (`src/Encina.Messaging/Sagas/LowCeremony/SagaDefinition.cs`, lines 143-154) and `SagaRunner.CompensateAsync(definition, data, lastStepIndex, requestContext, ct)` takes the definition and the deserialized data in memory; there is no registry of definitions by saga type anywhere in `src/` (grep `SagaDefinitionRegistry`: none). So a persisted `RetryCount` and `NextRetryAtUtc` alone cannot make a retry survive a restart: after the restart nobody can find the delegates or knows `TData` to deserialize.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A definition registry: the application registers each definition at startup (`services.AddSagaDefinition(builtDefinition)`, keyed by `SagaType`, duplicates rejected at startup); the registry entry erases `TData` and exposes `RetryAsync(runner, sagaId, ct)` which loads the row, deserializes `TData` with the orchestrator's serializer and calls `SagaRunner.RetryCompensationAsync<TData>(definition, sagaId, ct)`; the processor looks the entry up by the row's `SagaType`** | The retry is durable across restarts, which is the value of the issue; fails closed per row (below) | New public API (registry, extension, entry), a registration the application must not forget; an unknown saga type at processing time needs a defined outcome |
| B) Restrict to manual retry: `SagaRunner.RetryCompensationAsync(definition, sagaId, ct)` called by the application (its own hosted service, a scheduler, an operator), with no registry and no processor | Smaller: no registry, no processor, no DI beyond the options; the persisted state is still queryable through `GetCompensationRetryDueAsync` | No built-in automatic retry: each application writes its loop; the "configurable retry policy" of the issue is only the persisted delay arithmetic |

### Chosen Option: **A — Registry and automatic retry** (recommended, pending the maintainer)

### Rationale

- Option A wins because a retry that does not survive a restart does not solve the issue. Option B wins if the maintainer prefers the smallest public surface and accepts application-owned loops; it removes the registry, the processor and PR 13 of the split.
- **Fail closed per row (option A)**: a due row whose `SagaType` has no registered definition, or whose data cannot be deserialized, is recorded as a compensation failure with the error code `saga.definition_not_registered` or `saga.deserialization_failed` through the same path as any failed attempt (the count increments, the retry backs off, and after `MaxCompensationRetries` the saga is failed and dead-lettered), never skipped and never reported as success. Startup validation: with `CompensationTransaction` on and the processor registered, the registry must hold at least one definition; the application cannot be checked for saga types it has not registered, so the per-row rule is the guard for the rest.
- **Class:** Dilemma

</details>

<details>
<summary><strong>10. Build order of the pull request split (Dilemma D6)</strong></summary>

`ISagaState` gains `RetryCount` and `NextRetryAtUtc` and `ISagaStore` gains `GetCompensationRetryDueAsync`. Their implementers: 10 state classes, 9 stores, `FakeSagaState`, `FakeSagaStore`, `ArbitrarySagaState` (`src/Encina.Testing.FsCheck/ArbitraryMessages.cs`), the two benchmark `BenchmarkSagaState` classes (`tests/Encina.BenchmarkTests/Encina.ADO.Benchmarks|Encina.Dapper.Benchmarks/Infrastructure/BenchmarkEntityFactory.cs`), `TestSagaState` (`SagaOrchestratorTests.cs`) and the store doubles `RecordingSagaStore` (`tests/Encina.IntegrationTests/Infrastructure/Sagas/SagaRunnerOutcomeScenarios.cs`) and `FailingUpdateStore` (`SagaRunnerTests.cs`). An interface change before its implementers does not compile (`AGENTS.md` section 8), and a pull request is one package, at most 15 files and 1,500 lines counting tests and manifests (section 11).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Implementers first, interface last (the same rule as the #738 plan, D5): each provider pull request adds the two properties and `GetCompensationRetryDueAsync` to its concrete classes (they compile against the old interfaces), with SQL, store-level integration tests, PublicAPI and manifest entries; a later pull request adds the members to the interfaces** | Every pull request builds and carries its own tests; no scaffold merges | The new members exist on concrete classes for a few pull requests before the interface has them; 15 pull requests |
| B) One pull request with the whole change (about 77 files) | One review | Over the limits; needs the `size-exception-approved` label |
| C) Default interface members as a scaffold, removed later | Interface first | A compatibility layer merges for a while (`AGENTS.md` section 1) |

### Chosen Option: **A — Implementers first, interface last** (recommended, pending the maintainer)

### Rationale

- Option A wins because every step is green and reviewable and no transitional code merges. Option B wins if the maintainer prefers one large review. The same decision as in the #738 plan; the maintainer's answer should be one for the saga family, and the PRs that touch the same test doubles (#738 PR 11, this plan PR 10) merge in either order, the second adding only its members.
- **Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Core Models, Options and Contract

> **Goal**: The contract in `Encina.Messaging`.

<details>
<summary>Tasks</summary>

Delivered in PR 11 of the split, after every implementer already has the members (D6 option A).

1. `SagaOptions` (`src/Encina.Messaging/Sagas/SagaOrchestrator.cs`, line 615): `CompensationTransaction` (bool, false), `MaxCompensationRetries` (int, 3), `CompensationRetryDelay` (TimeSpan, 30 s), `CompensationRetryBackoffFactor` (double, 2.0), `CompensationRetryBatchSize` (int, 100), `CompensationRetryCheckInterval` (TimeSpan, 30 s); a validator class (`IValidateOptions<SagaOptions>`) rejecting non-positive values.
2. `ISagaState`: add `int RetryCount { get; set; }` and `DateTime? NextRetryAtUtc { get; set; }`.
3. `ISagaStateFactory.Create(...)`: no change (defaults zero and null).
4. `ISagaStore`: add `GetCompensationRetryDueAsync(int batchSize, CancellationToken)` returning sagas with `Status = Compensating` and `NextRetryAtUtc <= now`.
5. `SagaCompensationFailed` (sealed record, `INotification`): `SagaId`, `SagaType`, `StepIndex`, `Attempt`, `ErrorCode`, `WillRetry`, `OccurredAtUtc`.
6. `SagaErrorCodes`: new `saga.compensation_retry_exhausted` and `saga.definition_not_registered`; the existing `saga.deserialization_failed` (`SagaErrorCodes.DeserializationFailed`) is reused.
7. `PublicAPI.Unshipped.txt` of `Encina.Messaging`.
8. Unit tests of the validator, a contract test that the members exist with the documented semantics on the fake and on the real stores, and the manifest entries.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```
CONTEXT:
- Issue #720; plan docs/plans/saga-compensation-transaction-implementation-plan-720.md. Dilemmas D1-D6 are recommended, pending the maintainer: follow the Maintainer Decisions section if it exists.
- Encina.Messaging does not reference Encina.DomainModeling; transactions use IDbTransactionAccessor leases.
- All implementers already have the new members (earlier pull requests of the split, D6); this pull request adds them to the interfaces.

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

Constructor and ownership changes (Design Choice 7), all in `PublicAPI.Unshipped.txt`: `SagaRunner` gains `IDbTransactionAccessor?`, `IEncina`, `IOptions<SagaOptions>` and an optional `TimeProvider`; `SagaOrchestrator` gains the methods below and no dependency.

1. `SagaRunner.CompensateAsync`: when `CompensationTransaction` is on, for each step in reverse order: take a lease from `IDbTransactionAccessor`, run the delegate, call `SagaOrchestrator.CompensateStepAsync`, then the new `SagaOrchestrator.CommitAsync(ct)` (`ISagaStore.SaveChangesAsync`), then commit the lease; on a delegate failure roll back the lease, call the new `SagaOrchestrator.RecordCompensationFailureAsync(sagaId, errorCode, ct)` (increments `RetryCount`, sets `NextRetryAtUtc = now + delay * factor^RetryCount`, keeps `Compensating`) in a fresh write that is committed on its own, then publish `SagaCompensationFailed` through `IEncina.Publish`, and stop (D4). With the mode off the current behavior is kept.
2. `SagaOrchestrator.RecordCompensationFailureAsync`: when `RetryCount` reaches `MaxCompensationRetries`, call `FailAsync` (dead letter) with `saga.compensation_retry_exhausted`.
3. `SagaRunner.RetryCompensationAsync<TData>(BuiltSagaDefinition<TData> definition, Guid sagaId, CancellationToken ct)`: for the processor (through the registry entry of D5) and for manual use; loads the row with `SagaOrchestrator.GetAsync<TData>`, re-enters `CompensateAsync` at the persisted cursor (`CurrentStep`) with the deserialized data and the definition's delegates. It lives on the runner because only the runner can run the delegates.
4. Handle the existing swallow: the runner must honour the `Left` of these calls (#1581 is the same defect for the forward path; both can be fixed together or the plan's calls are written correctly from the start).
5. `[LoggerMessage]` entries in the `MessagingSaga` range, ids 2206-2229, packed sequentially from 2206.
6. Tests: the order "persist the failure state, then publish" with "persist succeeded, publish failed" returning the `Left` and leaving the saga due for retry (Design Choice 8).

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```
CONTEXT:
- Phase 1 added the options, the state fields, the store method and the notification (issue #720).

TASK:
Implement the Phase 2 tasks in SagaRunner and SagaOrchestrator.

KEY RULES:
- The runner never touches ISagaStore: commits go through SagaOrchestrator.CommitAsync. List every changed constructor in PublicAPI.Unshipped.txt.
- Persist the failure state first, publish second; a publish Left is returned but the retry still happens.
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

1. **Definition registry (D5 option A)**: `ISagaDefinitionRegistry` / `SagaDefinitionRegistry` (singleton, `src/Encina.Messaging/Sagas/`), `services.AddSagaDefinition(BuiltSagaDefinition<TData>)` (`TryAdd`-style: a second definition with the same `SagaType` fails at startup with a clear message), and the registry entry with `RetryAsync(runner, sagaId, ct)` that erases `TData` (Design Choice 9). Public API in `PublicAPI.Unshipped.txt`.
2. `SagaCompensationRetryProcessor` (`BackgroundService`, `src/Encina.Messaging/Sagas/`): the loop is a `PeriodicTimer(CompensationRetryCheckInterval, timeProvider)` loop, so `FakeTimeProvider` drives it in tests. It follows `DelayedRetryProcessor` only for the exception handling (`src/Encina.Messaging/Recoverability/DelayedRetryProcessor.cs`, lines 97-121: a `while (!stoppingToken.IsCancellationRequested)` loop, a try/catch per iteration that logs `ex.ForLogging()` and continues, and `catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)` ending the loop); `DelayedRetryProcessor` itself waits with `Task.Delay`, which is not the model here. Per iteration: load due sagas with `GetCompensationRetryDueAsync`; for each, look the definition up by `SagaType` in the registry and call its `RetryAsync`; an unknown type or a failing retry is recorded as a compensation failure (D5), never skipped. Each retry is its own lease and unit of work.
3. **Tenants (REQ-061, AC-043)**: the batch holds sagas of all tenants. Each saga is retried in its own unit of work (task 2), and until #738 puts a `TenantId` on the saga row there is no tenant to restore, so the processor runs without a tenant context; this risk is recorded in matrix row 10. When #738 lands, the processor sets the ambient tenant from `row.TenantId` for the duration of that saga's retry (a one-line change in the processor plus a two-tenant test), so it never mixes tenants in one unit of work.
4. Registration in `RegisterSagas` (`src/Encina.Messaging/MessagingServiceCollectionExtensions.cs`, line 355) only when `CompensationTransaction` is on: the processor, the registry, the options validator (the publisher dependency is the runner's `IEncina`, Design Choice 7), with a DI test using `ValidateOnBuild` and `ValidateScopes`.
5. Startup checks: the mode needs a store that implements `GetCompensationRetryDueAsync` (all 10 do after Phases 4-5), an `IDbTransactionAccessor`, and a non-empty registry when the processor is registered (Design Choice 9).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```
CONTEXT:
- Issue #720. D5 option A (registry) is recommended, pending the maintainer. The retry processor takes its per-iteration exception handling from DelayedRetryProcessor but waits with a PeriodicTimer(interval, TimeProvider), not Task.Delay.

TASK:
Implement the definition registry, SagaCompensationRetryProcessor and their registration.

KEY RULES:
- Background loops catch per iteration and never let an exception escape (AGENTS.md section 3); cancellation from the stopping token ends the loop.
- An unknown saga type or a failed deserialization is recorded as a compensation failure with its error code, never skipped.
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

One pull request per package (PRs 1 to 6; each carries its integration test, `PublicAPI.Unshipped.txt` and manifest entries; the interfaces are unchanged until PR 11, D6):

1. SQL: `RetryCount INT NOT NULL DEFAULT 0` and `NextRetryAtUtc` (`DATETIME2(7)` / `TIMESTAMP` / `DATETIME(6)` as the neighbouring `TimeoutAtUtc`) plus an index `(Status, NextRetryAtUtc)` in `Scripts/003_CreateSagaStatesTable.sql` and `Scripts/000_CreateAllTables.sql` of the package (2 files).
2. `SagaState`: the two properties. `SagaStateFactory` is not changed (`ISagaStateFactory.Create(...)` keeps its signature; defaults are zero and null).
3. `SagaStoreADO` or `SagaStoreDapper`: include the columns in `INSERT`, `UPDATE`, `SELECT`; the public method `GetCompensationRetryDueAsync` with `TOP (@n)` on SQL Server and `LIMIT @n` on PostgreSQL and MySQL; commands enlist in the scope's transaction through the accessor as the other methods do; asynchronous calls with a `CancellationToken`.
4. Store-level integration test of the package (`[Collection("ADO-<Db>")]` or `[Collection("Dapper-<Db>")]`): round trip of both fields, the due query (ordering, batch limit, only `Compensating` rows with `NextRetryAtUtc <= now`), and that a write inside a rolled-back lease is not persisted.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```
CONTEXT:
- Issue #720. The interfaces are not changed yet (D6): add the properties and the method to ONE package's concrete classes so that it builds against the current interfaces. The six implementations behave identically and differ only in dialect.

TASK:
Apply the Phase 4 tasks to the one ADO.NET or Dapper package of this pull request.

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

PRs 7 to 10 of the split, each building against the current interfaces (D6):

1. PR 7, EF Core: `SagaState` properties, `SagaStateConfiguration` (`HasIndex(x => new { x.Status, x.NextRetryAtUtc })`), `SagaStoreEF.GetCompensationRetryDueAsync`; one implementation for the three databases, with one integration test class per database. No EF migration (pre-1.0).
2. PR 8, MongoDB: `SagaState` fields, `SagaStoreMongoDB.GetCompensationRetryDueAsync`, the compound index in `MongoDbIndexCreator.CreateSagaIndexesAsync` (line 163), one integration test class. Multi-document transactions need a replica set; the integration fixture states it.
3. PR 9, `Encina.Testing.Fakes`: `FakeSagaState`/`FakeSagaStore` support the fields and the query, with unit tests.
4. PR 10, test-side implementers: `ArbitrarySagaState` (`src/Encina.Testing.FsCheck/ArbitraryMessages.cs`), the two benchmark `BenchmarkSagaState` classes, `TestSagaState`, `RecordingSagaStore` and `FailingUpdateStore` get the properties and the method (named in Design Choice 10).

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```
CONTEXT:
- Issue #720; the Phase 4 pull requests are merged and the interfaces are still unchanged (D6).

TASK:
Implement the Phase 5 tasks, one pull request per package or group as listed.

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

Delivered in PR 12 (with the orchestrator and runner) and PR 13 (processor and registry messages).

1. Counter `encina.saga.compensation_failed` (`Counter<long>`, tags `saga.type`, `will_retry`) and activity tags `saga.compensation.step`, `saga.compensation.attempt` on `SagaActivitySource` (no new span).
2. Log messages (ids 2206-2229, packed sequentially from 2206 across PRs 12 and 13); run `EncinaEventIdAllocationTests`.

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

Tests travel with the pull request that adds the code they cover; none waits for the end.

1. **Unit**: each failure point against a fake store (step fails, commit fails, publish fails after the state is stored, retry exhausted, unknown saga type, deserialization failure, mode off keeps today's behavior), options validator, registry (duplicate type), processor loop (`FakeTimeProvider`, a throwing store: the loop survives and logs the error code), notification content.
2. **Guard**: new public members, in the pull request that adds them.
3. **Contract**: `ISagaStore` retry state and query identical across all stores (PR 11).
4. **Property (FsCheck)** (PR 14): after a failure at any point the persisted state and the compensation outcome do not diverge.
5. **Integration**: the store-level tests in PRs 1 to 8; the orchestrator-level test on all 10 providers in PR 14 through the shared collections with real transactions (rollback leaves the step uncompensated and the failure recorded; a registered definition is retried after a simulated restart: a new provider, the same registry); never a `.md`.
6. **Load / Benchmark**: justification files `tests/Encina.LoadTests/Sagas/CompensationTransaction.md` and `tests/Encina.BenchmarkTests/Sagas/CompensationTransaction.md` (failure path, not a hot path), in PR 14.
7. **Per-file targets** in the manifests of every touched package, each flag with a one-sentence justification written in the manifest. Starting values:

   | File group | Unit | Guard | Contract | Property | Integration | Justification |
   |---|---|---|---|---|---|---|
   | `SagaOrchestrator` new methods, `SagaRunner` compensation path | 95 | 90 | 0 | 60 | 0 | Every failure point is testable with fake stores and a recording transaction accessor; the property flag checks state/outcome consistency; the real stores are covered by their integration flag |
   | Registry, processor, DI extension, options validator | 95 | 90 | 0 | 0 | 0 | Loop, per-row failure rules and validation are deterministic with `FakeTimeProvider` |
   | Provider stores and state classes (ADO.NET x3, Dapper x3, EF Core, MongoDB) | 0 | 90 | 80 | 0 | 80 | The SQL, LINQ and Mongo queries and transaction enlistment can only be proved against real databases (`AGENTS.md` section 5) |
   | `FakeSagaStore`, `FakeSagaState`, `ArbitrarySagaState` | 90 | 90 | 80 | 0 | 0 | Test doubles that must behave like the real stores; the contract test runs against them |
   | `SagaCompensationFailed`, `SagaOptions` additions | 0 | 90 | 0 | 0 | 0 | Declarations; guard covers the constructors |

</details>

<details>
<summary>Prompt for AI Agents — Phase 8</summary>

```
CONTEXT:
- Issue #720. Tests travel with the pull request that adds the code they cover (see the split); the orchestrator-level integration test and the property test are PR 14.

TASK:
Write the tests of Phase 8 Tasks for the pull request you are briefed on and fill its manifest targets.

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

1. XML docs; `PublicAPI.Unshipped.txt` per package; `changelog.d/720-saga-compensation-transaction.added.md`; the saga guide `docs/messaging/sagas.md` (through docs-writer); an ADR recording D1-D6 (ADR number assigned when the ADR pull request opens).
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

Order rule (D6 option A): implementers first, interfaces last. Every pull request builds, carries its own tests, `PublicAPI.Unshipped.txt` and manifest entries, and is one package (or one test-infrastructure group). Counts include tests and manifests.

| PR | Content | Approx. files | Approx. lines |
|----|---------|---------------|---------------|
| 1 | `Encina.ADO.SqlServer`: `SagaState`, `SagaStoreADO`, 2 scripts, PublicAPI, manifest, store integration test | 7 | 500 |
| 2 | `Encina.ADO.PostgreSQL`: same set | 7 | 500 |
| 3 | `Encina.ADO.MySQL`: same set | 7 | 500 |
| 4 | `Encina.Dapper.SqlServer`: same set | 7 | 500 |
| 5 | `Encina.Dapper.PostgreSQL`: same set | 7 | 500 |
| 6 | `Encina.Dapper.MySQL`: same set | 7 | 500 |
| 7 | `Encina.EntityFrameworkCore`: state, configuration, store, PublicAPI, manifest, 3 integration test classes | 8 | 600 |
| 8 | `Encina.MongoDB`: state, store, index creator, PublicAPI, manifest, integration test | 6 | 450 |
| 9 | `Encina.Testing.Fakes`: `FakeSagaState`, `FakeSagaStore`, PublicAPI, manifest, unit tests | 6 | 400 |
| 10 | Test-side implementers: `ArbitrarySagaState` (+ PublicAPI, manifest), 2 benchmark states, `TestSagaState`, `RecordingSagaStore`, `FailingUpdateStore` | 8 | 250 |
| 11 | Phase 1: `ISagaState`, `ISagaStore`, `SagaOptions` + validator, `SagaCompensationFailed`, error codes, PublicAPI, manifest, validator unit and contract tests | 11 | 800 |
| 12 | Phase 2 and 7: orchestrator (`RecordCompensationFailureAsync`, `CommitAsync`), runner (`RetryCompensationAsync`, constructor changes), logs, counter, unit tests | 9 | 1,300 |
| 13 | Phase 3: registry, `AddSagaDefinition`, `SagaCompensationRetryProcessor`, `RegisterSagas`, PublicAPI, manifest, unit and DI tests | 10 | 1,200 |
| 14 | Orchestrator-level integration test on 10 providers, property test, load and benchmark justifications | 14 | 1,200 |
| 15 | Documentation: fragment, saga guide, ADR (through docs-writer) | 3 | 300 |

A pull request that measures over 15 files or 1,500 lines when it is briefed is split again. If the maintainer picks D5 option B, PR 13 shrinks to the options and the manual retry and the registry, processor and its tests are dropped.

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
| `DelayedRetryProcessor` | `src/Encina.Messaging/Recoverability/DelayedRetryProcessor.cs` | Per-iteration exception handling and shutdown (lines 97-121); its wait is `Task.Delay`, the new loop uses `PeriodicTimer` |
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
| Messaging core | ~16 | Options, validator, state fields, store contract, notification, runner, orchestrator, registry, processor, DI, logs, PublicAPI, manifest (no factory change) |
| ADO.NET x3 and Dapper x3 | ~24 | State, store, 2 scripts per package (PublicAPI, manifest and the store integration test are counted per PR row) |
| EF Core, MongoDB, fakes, test-side implementers | ~26 | With PublicAPI, manifests and the store-level integration classes |
| Tests (orchestrator level) | ~16 | Unit, contract, property, 10 integration classes, two justifications |
| Documentation | ~3 | Fragment, guide, ADR |
| **Total** | **~100 across 15 pull requests** | The sum of the split table rows is the authority |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no [Obsolete], no migrations; XML docs; zero warnings.
- Issue #720: transactional, persisted, retryable saga compensation across the 10 database providers. Plan: docs/plans/saga-compensation-transaction-implementation-plan-720.md; Dilemmas D1-D6 are recommended, pending the maintainer.

IMPLEMENTATION OVERVIEW (the order of the 15 pull requests; every one builds):
1. Providers first, one package per pull request: ADO/Dapper (Phase 4), EF Core, MongoDB, fakes, test-side implementers (Phase 5).
2. Options, validator, notification and the interface members (Phase 1).
3. Per-step transactional compensation in SagaRunner and SagaOrchestrator, with observability (Phases 2 and 7).
4. Definition registry, durable retry processor and DI (Phase 3).
5. Orchestrator-level tests on 10 providers, then docs (Phases 8-9).

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
| 6 | Resilience | ✅ | Phase 2-3: the persisted retry policy (built-in, D3); it survives a restart only with the definition registry (D5) |
| 7 | Distributed Locks | ⏭️ | Deferred: concurrent retry of one saga across instances; issue file `artifacts/issues/saga-compensation-retry-concurrency.md`, related #2213 |
| 8 | Transactions | ✅ | This is the transaction feature |
| 9 | Idempotency | ⏭️ | Deferred: replay idempotency of a compensation step is #736; the schema is coordinated |
| 10 | Multi-Tenancy | ⏭️ | Deferred: `TenantId` on the saga row is #738. Risk until then: the retry processor loads sagas of all tenants in one batch and has no tenant to restore (REQ-061, AC-043); each saga is retried in its own unit of work, and once #738 lands the processor sets the ambient tenant per saga (Phase 3 task 3) |
| 11 | Module Isolation | ❌ | Not applicable: saga rows have no module scope (#747 is separate) |
| 12 | Audit Trail | ⏭️ | Deferred: saga transition audit is #750 |

---

## Prerequisites & Dependencies

- #1581 (SagaRunner discards the `Left` of the orchestrator's persisting calls) touches the same lines; fix it first or with Phase 2.
- #736, #738 and #1237 change the same scripts and stores: agree the merge order; this plan's columns are independent of theirs.
- #2208 (timeouts never enforced) concerns `TimeoutAsync`; independent, but the compensation retry loop follows its processor design.

---

## Next Steps

1. The maintainer answers D1 to D6 when work on #720 starts; the answers go into `## Maintainer Decisions` and the markers are removed.
2. Open the deferred issues (health check, retry concurrency) and link them from #720.
3. Brief pull request 1 first.
