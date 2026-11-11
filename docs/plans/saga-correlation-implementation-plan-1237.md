# Implementation Plan: Saga Correlation by External Key and Wait-for-Event Step

> **Issue**: [#1237](https://github.com/dlrivada/Encina/issues/1237)
> **Type**: Feature (SPEC-002 P-35, REQ-044, AC-039; part of #1186)
> **Complexity**: Very high (10 phases, 10 database providers, about 95 files in 10 pull requests)
> **Estimated Scope**: ~2,200 lines of production code + ~4,500 lines of tests

---

## Summary

Let a saga be found by an external key (a payment or refund id from a gateway) and wait for an event with a timeout and a compensation. Three parts: correlation keys (name and value) stored with the saga and indexed, with `ISagaStore.FindByCorrelationAsync`; a wait-for-event step in the low-ceremony definition (`SagaStepBuilder`) that suspends the saga until a matching event arrives or the wait times out and compensates; and an early-event buffer, so an event that arrives before the saga reaches the step (or before the saga exists) is kept and matched later. Payment webhooks arrive out of order and refer to the gateway's ids, not the saga id (SPEC-002 note H, H-R27); reference scenarios S6 and S7 (a refund that waits for the gateway's confirmation and compensates on timeout) verify it (P-25).

**Today's code** (main 7a0049c07): `SagaRunner.RunAsync` runs all steps of a `BuiltSagaDefinition<TData>` in one call and returns a `SagaResult<TData>` (`src/Encina.Messaging/Sagas/LowCeremony/`); `SagaStepDefinition` has `Execute` and `Compensate` delegates only; `ISagaStore` finds a saga by id only; `ISagaNotFoundDispatcher` and `IHandleSagaNotFound<TMessage>` handle a message whose saga does not exist (`src/Encina.Messaging/Sagas/`). The saga row has no correlation: the SQL scripts and the EF Core `SagaState` carry an unindexed free-text `CorrelationId` column (EF Core indexes it; `src/Encina.EntityFrameworkCore/Sagas/SagaStateConfiguration.cs`), it is not part of `ISagaState`, and the MongoDB `SagaState` has none, so the providers are not coherent. The timeout fields exist (`TimeoutAtUtc`, `GetExpiredSagasAsync`, `TimeoutAsync`) but nothing calls `TimeoutAsync` (bug #2208).

**Maintainer note recorded on the issue** (decision on #2308, 2026-10-16): the routing of messages by correlation that the original low-ceremony saga request (#41) asked for is delivered here; the attribute, step builder and inline state syntax were superseded by the fluent API. The issue comment of 2026-10-17 records "settled by elimination": correlation keys stored and indexed, `FindByCorrelationAsync`, a wait step with timeout and compensation, early events kept; application-side lookup tables and gateway polling are rejected.

**Not in scope**: the state-machine coordinator (#297 uses `FireAsync(sagaId, trigger)` and can call this plan's lookup), the tenant column of the saga row (#738; the new tables here carry their own tenant), compensation retry (#720), audit (#750).

**Affected packages**: `Encina.Messaging`, `Encina.ADO.SqlServer|PostgreSQL|MySQL`, `Encina.Dapper.SqlServer|PostgreSQL|MySQL`, `Encina.EntityFrameworkCore`, `Encina.MongoDB`, `Encina.Testing.Fakes`.

**Provider category**: Database (10); `SagaStoreEF` and the new buffer store serve three databases each.

**Shared with the saga family**: #738, #720 and #750 also change the saga stores and scripts. This plan owns the correlation key and pending event tables, `FindByCorrelationAsync`, `ISagaEventBufferStore`, `ISagaEventRouter` and the wait step. EventId slice 2275-2299 inside `MessagingSaga` (2200-2299), no new range.

---

## Design Choices

<details>
<summary><strong>1. Where correlation keys are stored (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A separate table/collection `SagaCorrelationKeys` (`SagaId`, `SagaType`, `KeyName`, `KeyValue`, `TenantId`, `CreatedAtUtc`) with a composite index on `(TenantId, SagaType, KeyName, KeyValue)`** | Many keys per saga (payment id, refund id); indexed lookup; keys can be added later in the saga's life | A new table in 10 providers; keys and saga row are written together (same transaction) |
| **B) Two columns `CorrelationKeyName`, `CorrelationKeyValue` on the saga row** | No new table | One key per saga; the issue's example needs several |
| **C) Serialize the keys into the existing `Metadata` text column** | No schema change | Not indexable; `Metadata` is not part of `ISagaState` and missing in MongoDB |

### Chosen Option: **A — A separate keys table with a composite index** (recommended, pending the maintainer)

### Rationale

- Option A wins when a saga is reachable by several external ids, as in the issue's refund scenario (a payment id and a refund id). Option B wins for deployments where every saga has exactly one key and a smaller schema matters more.
- The existing unindexed `CorrelationId` column of the scripts and EF Core is replaced by this model; it is removed from the scripts and from EF Core `SagaState` (no compatibility alias, pre-1.0).
- **Class:** Dilemma

</details>

<details>
<summary><strong>2. Uniqueness of a key (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Unique per `(TenantId, SagaType, KeyName, KeyValue)`; a duplicate returns `Left` `saga.correlation_key_conflict`** | A webhook always maps to at most one saga; no ambiguity | A gateway id reused by two sagas of the same type cannot coexist |
| **B) Not unique: `FindByCorrelationAsync` returns every match** | Flexible | The caller must decide which saga receives the event; an ambiguous routing is a security and consistency risk |

### Chosen Option: **A — Unique** (recommended, pending the maintainer)

### Rationale

- Option A wins when each external id identifies one business process, as with payment and refund ids. Option B wins when one external event must fan out to several sagas (a broadcast), which the issue does not ask for.
- **Class:** Dilemma

</details>

<details>
<summary><strong>3. How a wait step suspends and resumes the runner (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `SagaStepBuilder.WaitForEvent<TEvent>(keyName, keySelector, timeout)` creates a wait step; `RunAsync` persists the saga as `Running`, records the awaited event and returns `SagaResult` with a `Waiting` flag; `ISagaRunner.ResumeAsync<TData, TEvent>(definition, sagaId, event)` continues from the next step** | Same fluent API and runner; durable across restarts; the steps before and after share the compensation chain | `SagaResult` and `ISagaRunner` grow; the definition must be supplied again on resume |
| **B) Keep the runner linear; waiting is modelled only by the state-machine coordinator of #297** | No runner change | The issue explicitly asks for a wait step in the saga; two models for one need |

### Chosen Option: **A — Suspend and resume in the runner** (recommended, pending the maintainer)

### Rationale

- Option A wins because the issue's acceptance scenarios (S6, S7) are low-ceremony sagas. Option B wins if the maintainer prefers that event-driven sagas are always state machines (#297) and the low-ceremony runner stays a straight sequence.
- **Class:** Dilemma

</details>

<details>
<summary><strong>4. Storage of early events (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A buffer store `ISagaEventBufferStore` (table `SagaPendingEvents`: `Id`, `TenantId`, `KeyName`, `KeyValue`, `EventType`, serialized payload, `ReceivedAtUtc`, `ExpiresAtUtc`) implemented for the 10 providers** | Works whether the saga exists or not; matched by key, not saga id; purgeable by expiry | Another store and table in 10 providers |
| **B) Reuse the inbox store** | Existing infrastructure | The inbox is for idempotent exactly-once consumption, not for holding a message until a saga reaches a step; its processing model does not fit |
| **C) In memory only** | Simple | Lost on restart, which loses a payment confirmation |

### Chosen Option: **A — A dedicated buffer store** (recommended, pending the maintainer)

### Rationale

- Option A wins because a webhook may arrive before the saga reaches the step or even before the saga exists, and must survive a restart. Option B wins only if the maintainer wants a single message store and accepts reshaping the inbox contract.
- Payloads are serialized with `IMessageSerializer`, as `SagaOrchestrator` does for saga data, so serializer decorators such as encryption apply (see choice 7).
- **Class:** Dilemma

</details>

<details>
<summary><strong>5. The wait timeout mechanism (Dilemma D5)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A scheduled message (`Encina.Messaging` scheduling) per wait that triggers the compensation if the saga still waits, as the issue says "a timeout (scheduled message)"** | Durable and per wait step, independent of the whole-saga timeout; no polling loop of our own | The saga module needs the scheduling module when a wait has a timeout; the closed list of ADR-050 does not list that requirement, so the ADR is amended in the same pull request |
| **B) Reuse the saga timeout (`TimeoutAtUtc`, `GetExpiredSagasAsync`, `TimeoutAsync`)** | Existing fields | Whole-saga timeout, not per wait; nothing enforces it today (#2208, open bug), so the feature would depend on that fix and share its processor |

### Chosen Option: **A — A scheduled timeout message** (recommended, pending the maintainer)

### Rationale

- Option A wins when a saga has several waits with different deadlines, as refund flows do. Option B wins if #2208's timeout processor lands first and one deadline per saga is enough.
- With either option, the timeout is enforced by durable infrastructure, never by an in-memory timer.
- **Class:** Dilemma

</details>

<details>
<summary><strong>6. An event for a saga that does not exist</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Buffer it (choice 4); if a buffer expires unmatched, dispatch it to the existing `ISagaNotFoundDispatcher` so `IHandleSagaNotFound<TMessage>` handlers decide (ignore or dead letter)** | Reuses the existing not-found contract and its dead letter integration | Delay before the not-found handler runs |
| **B) Call the not-found handler immediately** | Simple | Loses the out-of-order case the issue exists for |

### Chosen Option: **A — Buffer, then the not-found handler on expiry**

### Rationale

- **Class:** Settled
- **Settled by:** the issue ("an event arriving before the saga reaches the step is kept and matched when it does") and the existing contract in `src/Encina.Messaging/Sagas/ISagaNotFoundDispatcher.cs` (a handler that fails to move or ignore returns an error so the caller does not acknowledge the message).

</details>

<details>
<summary><strong>7. Serialization and tenant of the new rows</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Payload through `IMessageSerializer`; every new row carries `TenantId` from the ambient `IRequestContext`; lookups filter by it and fail closed when the context has no tenant for a tenant-owned key; with tenancy off a fixed default tenant applies** | Complies with SPEC-002 REQ-061 and AC-043; encryption decorators apply | The default-tenant constant is defined by #738/#1164 |
| **B) Raw JSON and no tenant** | Simple | Violates REQ-061 and bypasses encryption |

### Chosen Option: **A — Serializer decorators and tenant on every row**

### Rationale

- **Class:** Settled
- **Settled by:** SPEC-002 REQ-061 and AC-043 (named in the issue), `AGENTS.md` section 3 (compliance gates fail closed), and the precedent `SagaOrchestrator` uses `IMessageSerializer` "so that decorators such as `EncryptingMessageSerializer` apply to saga state too" (`src/Encina.Messaging/Sagas/SagaOrchestrator.cs`).

</details>

<details>
<summary><strong>8. Interface placement and names</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Key methods on `ISagaStore` (`AddCorrelationKeyAsync`, `FindByCorrelationAsync`); a separate `ISagaEventBufferStore`; routing in `ISagaEventRouter`; store classes `SagaEventBufferStoreADO`, `SagaEventBufferStoreDapper`, `SagaEventBufferStoreEF`, `SagaEventBufferStoreMongoDB`** | Keys are written with the saga row; the buffer is opt-in; names follow the store convention | Many new classes |
| **B) Everything on `ISagaStore`** | One interface | Every saga store must implement the buffer even if wait steps are unused |

### Chosen Option: **A — Keys on `ISagaStore`, buffer in its own store**

### Rationale

- **Class:** Trivial
- **Settled by:** `AGENTS.md` section 4 (`{Pattern}Store{Provider}`; never a bare `Store`) and section 3 (provider coherence: abstractions live in `Encina.Messaging`; every messaging pattern optional).

</details>

---

## Implementation Phases

### Phase 1: Core Models and Contracts

> **Goal**: The contract in `Encina.Messaging`.

<details>
<summary>Tasks</summary>

1. `SagaCorrelationKey` (sealed record): `SagaId`, `SagaType`, `KeyName`, `KeyValue`, `TenantId`, `CreatedAtUtc`.
2. `ISagaStore`: `AddCorrelationKeyAsync(SagaCorrelationKey key, CancellationToken)` and `FindByCorrelationAsync(string sagaType, string keyName, string keyValue, CancellationToken)` returning `Either<EncinaError, Option<ISagaState>>`.
3. `PendingSagaEvent` (sealed record) and `ISagaEventBufferStore`: `AddAsync`, `TakeMatchingAsync(keyName, keyValue, eventType, ct)` (removes and returns), `GetExpiredAsync(batchSize, ct)`, `DeleteAsync(ids, ct)`.
4. `ISagaEventRouter`: `ValueTask<Either<EncinaError, SagaEventRoutingResult>> RouteAsync<TEvent>(string sagaType, string keyName, string keyValue, TEvent @event, CancellationToken)`; `SagaEventRoutingResult` (enum-like record: Delivered, Buffered, NotFound).
5. `SagaErrorCodes`: `saga.correlation_key_conflict`, `saga.wait_timeout`, `saga.event_type_mismatch`.
6. `PublicAPI.Unshipped.txt` of `Encina.Messaging`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```
CONTEXT:
- Issue #1237; plan docs/plans/saga-correlation-implementation-plan-1237.md. Dilemmas D1-D5 are recommended, pending the maintainer: follow the Maintainer Decisions section if it exists.
- Encina is pre-1.0: no compatibility aliases; the old free-text CorrelationId column is replaced.

TASK:
Implement the Phase 1 contract types.

KEY RULES:
- Sealed records, XML docs with examples, timestamps with the AtUtc suffix, error codes under saga.*.
- Names follow AGENTS.md section 4; update PublicAPI.Unshipped.txt; zero warnings.

REFERENCE FILES:
- src/Encina.Messaging/Sagas/ISagaStore.cs
- src/Encina.Messaging/Sagas/ISagaNotFoundDispatcher.cs
- src/Encina.Messaging/Sagas/SagaErrorCodes.cs
```

</details>

---

### Phase 2: Wait Step in the Low-Ceremony Runner

> **Goal**: Suspend at a wait step and resume on the event (D3 Option A).

<details>
<summary>Tasks</summary>

1. `SagaStepBuilder<TData>.WaitForEvent<TEvent>(string keyName, Func<TData, string> keySelector, TimeSpan timeout)` and `.OnEvent(Func<TData, TEvent, IRequestContext, CancellationToken, ValueTask<Either<EncinaError, TData>>>)`; the step keeps `Compensate`.
2. `SagaStepDefinition<TData>`: a nullable `SagaWaitDefinition` (event type name, key name, key selector, timeout, handler).
3. `SagaRunner`: when the next step is a wait step it adds the correlation key (`AddCorrelationKeyAsync`), looks for a buffered event (`TakeMatchingAsync`); if found it applies it at once; otherwise it schedules the timeout (Phase 3), persists the cursor and returns `SagaResult` with `IsWaiting = true`.
4. `ISagaRunner.ResumeAsync<TData, TEvent>(BuiltSagaDefinition<TData> definition, Guid sagaId, TEvent @event, CancellationToken)`: loads the saga, checks it waits for `TEvent` (else `saga.event_type_mismatch`), runs the handler, advances and continues with the remaining steps; a failure compensates in reverse order as today.
5. Honour every `Left` of the orchestrator calls on this path (same defect as #1581).
6. `[LoggerMessage]` entries, ids 2275-2299.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```
CONTEXT:
- Phase 1 added the contract (issue #1237). The runner today executes all steps in one call.

TASK:
Implement the wait step in SagaStepBuilder, SagaStepDefinition, SagaRunner and ISagaRunner.ResumeAsync.

KEY RULES:
- A Left from any store call is returned; a persisted cursor survives a restart.
- Compensation of the steps before the wait still runs in reverse order on a timeout.
- Only error codes are persisted and logged; time from TimeProvider; EventIds 2275-2299, sequential.

REFERENCE FILES:
- src/Encina.Messaging/Sagas/LowCeremony/SagaRunner.cs
- src/Encina.Messaging/Sagas/LowCeremony/SagaStepBuilder.cs
- src/Encina.Messaging/Sagas/LowCeremony/SagaDefinition.cs
```

</details>

---

### Phase 3: Router, Buffer Expiry, Timeout and DI

> **Goal**: Events find their saga, wait on timeout compensates, registration is complete.

<details>
<summary>Tasks</summary>

1. `SagaEventRouter` (`ISagaEventRouter`): `FindByCorrelationAsync` then deliver by calling the registered resume delegate; not found: `ISagaEventBufferStore.AddAsync` and return `Buffered`.
2. `SagaEventBufferExpiryProcessor` (`BackgroundService`, `PeriodicTimer` loop that catches per iteration): expired buffered events go to `ISagaNotFoundDispatcher` (choice 6); a dispatcher `Left` keeps the row for the next iteration.
3. Wait timeout (D5 recommendation): a `SagaWaitTimeout` scheduled message handled by a handler that, if the saga still waits for that event, calls the compensation path of Phase 2 and records `saga.wait_timeout`.
4. Registration `AddEncinaSagaCorrelation()` (TryAdd): router, processor, buffer store requirement, timeout handler; startup validation when a definition has a wait with a timeout and scheduling is off; a DI test with `ValidateOnBuild` and `ValidateScopes`.
5. ADR-050 amendment for the Saga to Scheduling requirement (D5 A).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```
CONTEXT:
- Phases 1-2 done (issue #1237). D5 is recommended as a scheduled timeout message, pending the maintainer.

TASK:
Implement the router, the expiry processor, the timeout handler and the registration.

KEY RULES:
- Background loops catch per iteration and never let an exception escape; cancellation from the stopping token ends the loop.
- An undeliverable event is never reported as delivered; a Left keeps it buffered.
- TryAdd registrations; ValidateOnBuild and ValidateScopes DI test; time from TimeProvider.

REFERENCE FILES:
- src/Encina.Messaging/Recoverability/DelayedRetryProcessor.cs
- src/Encina.Messaging/Sagas/SagaNotFoundDispatcher.cs
- src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs (RegisterSagas)
- docs/architecture/adr/050-modules-may-require-modules.md
```

</details>

---

### Phase 4: ADO.NET Providers (3)

<details>
<summary>Tasks</summary>

1. Scripts: `Scripts/0NN_CreateSagaCorrelationKeysTable.sql` and `Scripts/0NN_CreateSagaPendingEventsTable.sql` (next free numbers in each package) and the same tables in `000_CreateAllTables.sql`, in `Encina.ADO.SqlServer|PostgreSQL|MySQL`; remove the free-text `CorrelationId` column from `003_CreateSagaStatesTable.sql` and `000_CreateAllTables.sql`. Unique index `(TenantId, SagaType, KeyName, KeyValue)` (D2 A); index on `(TenantId, KeyName, KeyValue, EventType)` and `ExpiresAtUtc` for the buffer. `TenantId VARCHAR(128)` as the dead letter tables (MySQL `utf8mb4_bin`).
2. `SagaStoreADO` (3): the two key methods; `Sagas/SagaEventBufferStoreADO.cs` (3).
3. `ServiceCollectionExtensions` of each package registers the buffer store (`TryAdd`, store wins over any in-memory default in any registration order).

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```
CONTEXT:
- Phases 1-3 done (issue #1237).

TASK:
Apply the Phase 4 tasks in the three ADO.NET packages; the implementations differ only in SQL dialect.

KEY RULES:
- SQL Server @param and TOP (@n); PostgreSQL and MySQL LIMIT @n; MySQL tenant column utf8mb4_bin.
- Asynchronous calls with a CancellationToken; commands enlist in the scope's transaction through the accessor; a unique violation maps to saga.correlation_key_conflict.

REFERENCE FILES:
- src/Encina.ADO.PostgreSQL/Sagas/SagaStoreADO.cs
- src/Encina.ADO.PostgreSQL/Scripts/003_CreateSagaStatesTable.sql
- src/Encina.ADO.PostgreSQL/ServiceCollectionExtensions.cs
```

</details>

---

### Phase 5: Dapper Providers (3)

<details>
<summary>Tasks</summary>

The same as Phase 4 in `Encina.Dapper.SqlServer|PostgreSQL|MySQL`: scripts, `SagaStoreDapper` (3), `Sagas/SagaEventBufferStoreDapper.cs` (3), registration.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```
CONTEXT:
- Phase 4 is done for ADO.NET (issue #1237); Dapper must behave identically.

TASK:
Apply the Phase 5 tasks to the three Dapper packages.

KEY RULES:
- Same dialect rules and error mapping as Phase 4; asynchronous Dapper calls with a CancellationToken.

REFERENCE FILES:
- src/Encina.Dapper.SqlServer/Sagas/SagaStoreDapper.cs
- src/Encina.Dapper.MySQL/Scripts/000_CreateAllTables.sql
```

</details>

---

### Phase 6: EF Core, MongoDB and Fakes

<details>
<summary>Tasks</summary>

1. EF Core: entities `SagaCorrelationKeyEntity` and `SagaPendingEventEntity`, their configurations (unique index per D2 A), `SagaStoreEF` key methods, `SagaEventBufferStoreEF`; remove `CorrelationId` from `SagaState` and its configuration; one implementation serving SQL Server, PostgreSQL and MySQL. No migration (pre-1.0).
2. MongoDB: documents and collections (`Collections.SagaCorrelationKeys`, `SagaPendingEvents` in the options), `SagaStoreMongoDB` key methods, `SagaEventBufferStoreMongoDB`, unique and lookup indexes in `MongoDbIndexCreator`.
3. `Encina.Testing.Fakes`: `FakeSagaStore` key methods, `FakeSagaEventBufferStore`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```
CONTEXT:
- Phases 1-5 done (issue #1237).

TASK:
Implement the Phase 6 tasks.

KEY RULES:
- Behavior identical to the SQL stores; contract tests run against every store and the fakes.
- MongoDB indexes only through MongoDbIndexCreator and guarded by UseSagas.

REFERENCE FILES:
- src/Encina.EntityFrameworkCore/Sagas/SagaStoreEF.cs
- src/Encina.MongoDB/MongoDbIndexCreator.cs
- src/Encina.Testing.Fakes/Stores/FakeSagaStore.cs
```

</details>

---

### Phase 7: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. **Validation**: options for the buffer (`BufferRetention`, `ExpiryBatchSize`, `ExpiryCheckInterval`) validated at start-up with an `IValidateOptions` validator.
2. **Distributed locks**: concurrent events for one saga are serialized through the saga store's concurrency control (#2213) and the unique key; no lock provider is added here.
3. **Transactions**: the key and the saga row are written in the same scope transaction; the saga state and any outbox write of the resumed step share the scope.
4. **Idempotency**: a duplicate event is matched once (the buffered event is removed by `TakeMatchingAsync` in the same transaction as the resume).
5. **Multi-tenancy**: choice 7.
6. **Deferred**: audit (#750), module scoping (#747), health check of the buffer (a count above a threshold; issue file).

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```
CONTEXT:
- Cross-cutting matrix of docs/plans/saga-correlation-implementation-plan-1237.md; SPEC-002 REQ-061 and REQ-062.

TASK:
Wire the included functions and keep the deferred out of the diff.

KEY RULES:
- Two-tenant test: tenant A never reads, acts on or exports tenant B's keys or buffered events.

REFERENCE FILES:
- AGENTS.md section 6
- src/Encina.Messaging/DeadLetter/DeadLetterManager.cs (AmbientTenantId)
```

</details>

---

### Phase 8: Observability

<details>
<summary>Tasks</summary>

1. Span `encina.saga.correlate` (attributes `encina.tenant_id`, `encina.saga.type`) on `SagaActivitySource`; counters `encina.saga.wait_timeouts` and `encina.saga.early_events` (`Counter<long>`, tenant and saga type as attributes per SPEC-002 REQ-062); no payload, response body or data-subject identifier in any attribute, tag or log (AC-044).
2. Log messages in 2275-2299; run `EncinaEventIdAllocationTests`.
3. A test with an in-memory exporter shows the main operations emit their activity and metric with the tenant.

</details>

<details>
<summary>Prompt for AI Agents — Phase 8</summary>

```
CONTEXT:
- Issue #1237; slice 2275-2299 of MessagingSaga.

TASK:
Add the span, counters and log messages and the exporter test.

KEY RULES:
- [LoggerMessage] source generator; sequential ids; error code only; correlation key values are never logged or tagged.

REFERENCE FILES:
- src/Encina.Messaging/Diagnostics/SagaActivitySource.cs
- src/Encina.Messaging/Diagnostics/MessagingStoreMetrics.cs
```

</details>

---

### Phase 9: Testing

<details>
<summary>Tasks</summary>

1. **Unit**: builder, runner suspend and resume, router (delivered, buffered, not found), expiry, timeout handler, options validator, fakes.
2. **Guard**: every public member with parameters.
3. **Contract**: `ISagaStore` correlation methods and `ISagaEventBufferStore` identical across all stores and fakes.
4. **Property (FsCheck)**: any arrival order of the event relative to the wait step ends in the same saga state.
5. **Integration**: all 10 providers through the shared collections, including S6 and S7 end to end and the two-tenant test; `ClearAllDataAsync` in `InitializeAsync`; never `IClassFixture`.
6. **Load / Benchmark**: justification files `tests/Encina.LoadTests/Sagas/SagaCorrelation.md` and `tests/Encina.BenchmarkTests/Sagas/SagaCorrelation.md` (not a concurrent hot path).
7. **Per-file targets** with one-sentence justifications in the manifest of every touched package.

</details>

<details>
<summary>Prompt for AI Agents — Phase 9</summary>

```
CONTEXT:
- Phases 1-8 implemented (issue #1237).

TASK:
Write the tests of Phase 9 Tasks and fill the manifest targets.

KEY RULES:
- Real databases for all 10 providers; never a .md for integration; Shouldly via Encina.Testing.Shouldly; FakeTimeProvider for processors.

REFERENCE FILES:
- tests/Encina.UnitTests/Messaging/Sagas/SagaRunnerTests.cs
- docs/testing/integration-tests.md
```

</details>

---

### Phase 10: Documentation & Finalization

<details>
<summary>Tasks</summary>

1. XML docs; `PublicAPI.Unshipped.txt` per package; `changelog.d/1237-saga-correlation.added.md`; package README note; configuration guide `docs/messaging/sagas.md`; an ADR recording D1-D5 and the ADR-050 amendment (read `docs/architecture/adr/index.md` for the next free number).
2. `dotnet build --configuration Release` with 0 warnings; `dotnet test` with every flag at its target; the pull request records the ADR-018 evaluation of the 12 functions.

</details>

<details>
<summary>Prompt for AI Agents — Phase 10</summary>

```
CONTEXT:
- Issue #1237 implemented and tested.

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
| 1 | Phase 1: contract | 8 | 450 |
| 2 | Phase 2: wait step and runner | 7 | 1,100 |
| 3 | Phase 3: router, processor, timeout, DI | 9 | 1,200 |
| 4 | Phase 4: ADO.NET (3) | 15 | 1,200 |
| 5 | Phase 5: Dapper (3) | 15 | 1,200 |
| 6 | Phase 6a: EF Core | 10 | 700 |
| 7 | Phase 6b: MongoDB and fakes | 10 | 700 |
| 8 | Phases 7-8: cross-cutting and observability | 8 | 600 |
| 9 | Phase 9: tests (contract, property, integration x10) | 14 | 1,400 |
| 10 | Phase 10: documentation and ADR | 6 | 450 |

---

## Research

### Relevant Standards & Specifications

| Reference | What it defines | Use in this feature |
|-----------|-----------------|---------------------|
| SPEC-002 REQ-044, AC-039, scenarios S6 and S7 | Saga correlation and wait-for-event | Acceptance |
| SPEC-002 REQ-061, AC-043 | Tenant-aware capabilities | Choice 7 |
| SPEC-002 REQ-062, AC-044 | Instrumentation with the tenant as attribute, no payloads | Phase 8 |
| Enterprise Integration Patterns: Correlation Identifier, Process Manager | Message correlation to a process instance | Design vocabulary |
| MassTransit sagas (correlation ids), NServiceBus sagas (correlation property) | Comparable products | Key model |
| ADR-050 | Module requirements | Scheduling requirement (D5 A) |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `SagaRunner`, `SagaStepBuilder`, `SagaDefinition` | `src/Encina.Messaging/Sagas/LowCeremony/` | Wait step and resume |
| `SagaOrchestrator`, `ISagaStore` and nine store files | `src/Encina.Messaging/Sagas/`, `src/Encina.*/Sagas/` | Persistence |
| `ISagaNotFoundDispatcher`, `IHandleSagaNotFound<T>`, `SagaNotFoundContext` | `src/Encina.Messaging/Sagas/` | Unmatched events |
| Scheduling (`IScheduledMessageStore`, `ScheduledMessageProcessor`) | `src/Encina.Messaging/Scheduling/` | Wait timeout |
| `IMessageSerializer` | `src/Encina.Messaging` | Payload serialization with encryption decorators |
| `DeadLetterManager.AmbientTenantId` | `src/Encina.Messaging/DeadLetter/DeadLetterManager.cs` | Tenant resolution |
| `MongoDbIndexCreator` | `src/Encina.MongoDB/MongoDbIndexCreator.cs` | Indexes |
| `FakeSagaStore` | `src/Encina.Testing.Fakes/Stores/FakeSagaStore.cs` | Test double |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` (saga) | 2200-2299 | Existing `MessagingSaga`; `SagaStoreLog` uses 2200-2205 |
| **This plan (#1237)** | **2275-2299** | **Correlation, wait, buffer, expiry; slice within the saga family: #720 2206-2229, #738 2230-2249, #750 2250-2274, #1237 2275-2299** |

No new range; `Encina.Messaging` already maps to `MessagingSaga`.

### Provider Matrix

| Provider | Keys table | Buffer store | Integration test |
|----------|------------|--------------|------------------|
| ADO-SqlServer, ADO-PostgreSQL, ADO-MySQL | Required | `SagaEventBufferStoreADO` | `[Collection("ADO-<Db>")]` |
| Dapper-SqlServer, Dapper-PostgreSQL, Dapper-MySQL | Required | `SagaEventBufferStoreDapper` | `[Collection("Dapper-<Db>")]` |
| EFCore-SqlServer, EFCore-PostgreSQL, EFCore-MySQL | one implementation | `SagaEventBufferStoreEF` | `[Collection("EFCore-<Db>")]` x3 |
| MongoDB | Required | `SagaEventBufferStoreMongoDB` | MongoDB fixture |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Messaging core | ~18 | Contract, builder, runner, router, processor, handler, DI, logs |
| ADO.NET x3 and Dapper x3 | ~42 | Scripts (4 per package), store edits, buffer store, registration |
| EF Core, MongoDB, fakes | ~16 | Entities, configurations, stores, indexes, fakes |
| Tests | ~24 | Unit, guard, contract, property, 10 integration (grouped), two justifications, DI |
| Documentation | ~4 | Fragment, guide, ADR, README note |
| **Total** | **~104 across 10 pull requests** | |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no [Obsolete], no migrations, no compatibility aliases; XML docs; zero warnings.
- Issue #1237 (SPEC-002 P-35, REQ-044, AC-039): saga correlation by external key, a wait-for-event step with timeout and compensation, early events kept. Plan: docs/plans/saga-correlation-implementation-plan-1237.md; Dilemmas D1-D5 are recommended, pending the maintainer.

IMPLEMENTATION OVERVIEW:
1. Contract: keys, buffer store, router (Phase 1).
2. Wait step, suspend and resume in SagaRunner (Phase 2).
3. Router, expiry processor, scheduled timeout, DI (Phase 3).
4. Providers: ADO.NET, Dapper (Phases 4-5); EF Core, MongoDB, fakes (Phase 6).
5. Cross-cutting, observability, tests, documentation (Phases 7-10).

KEY PATTERNS:
- Tenant on every new row from the ambient IRequestContext, fail closed; payloads through IMessageSerializer.
- Every Left is returned; error codes only in storage and logs; background loops catch per iteration; TimeProvider for time.
- SQL: SQL Server TOP (@n); PostgreSQL and MySQL LIMIT @n; asynchronous calls with a CancellationToken; MySQL tenant column utf8mb4_bin.
- TryAdd registrations, a store wins over any in-memory default in any order; ValidateOnBuild DI test.
- EventIds 2275-2299 inside the registered MessagingSaga range.

REFERENCE FILES:
- src/Encina.Messaging/Sagas/LowCeremony/SagaRunner.cs
- src/Encina.Messaging/Sagas/ISagaNotFoundDispatcher.cs
- src/Encina.Messaging/Sagas/SagaOrchestrator.cs
- src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs
- src/Encina.EntityFrameworkCore/Sagas/SagaStoreEF.cs
- src/Encina.MongoDB/MongoDbIndexCreator.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: no read path that benefits from caching; saga state needs read-your-writes |
| 2 | OpenTelemetry | ✅ | Phase 8: `encina.saga.correlate` span, `encina.saga.wait_timeouts` and `encina.saga.early_events` counters, tenant as attribute (REQ-062) |
| 3 | Structured Logging | ✅ | Phase 2 and 8: messages in 2275-2299, error code only |
| 4 | Health Checks | ⏭️ | Deferred: a buffered-event backlog check extends `SagaHealthCheck`; issue file `artifacts/issues/saga-event-buffer-health-check.md` |
| 5 | Validation | ✅ | Phase 7: options validated at start-up |
| 6 | Resilience | ❌ | Not applicable: no call to an external system; the timeout is durable infrastructure |
| 7 | Distributed Locks | ✅ | Phase 7: concurrent events for one saga are serialized through the saga store's concurrency control (#2213) and the unique key; no lock provider is added |
| 8 | Transactions | ✅ | Phase 7: keys, saga state and outbox writes share the scope transaction |
| 9 | Idempotency | ✅ | Phase 7: a duplicate event is matched once |
| 10 | Multi-Tenancy | ✅ | Choice 7: tenant on every new row (REQ-061, AC-043) |
| 11 | Module Isolation | ❌ | Not applicable: SPEC-002 requires no module scoping; `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ⏭️ | Deferred: saga transition audit is #750; resumed and timed-out waits become events there |

---

## Prerequisites & Dependencies

- #738 (tenant on the saga row) and the default-tenant decision of #1164: the new tables carry their own tenant column and follow the same rule.
- #2213 (concurrency control on the saga row) for the serialization claim of Phase 7.
- #2208 (timeouts never enforced) only if D5 is answered B.
- #1581 (SagaRunner discards `Left` results) touches the same lines as Phase 2.
- #720 and #750 change the same stores and scripts: agree the merge order.

---

## Next Steps

1. The maintainer answers D1 to D5 when work on #1237 starts; the answers go into `## Maintainer Decisions` and the markers are removed.
2. Open the deferred health-check issue and link it from #1237.
3. Brief pull request 1 first.
