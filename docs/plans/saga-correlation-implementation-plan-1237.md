# Implementation Plan: Saga Correlation by External Key and Wait-for-Event Step

> **Issue**: [#1237](https://github.com/dlrivada/Encina/issues/1237)
> **Type**: Feature (SPEC-002 P-35, REQ-044, AC-039; part of #1186)
> **Complexity**: Very high (10 phases, 10 database providers, about 185 files in 16 pull requests)
> **Estimated Scope**: ~2,200 lines of production code + ~4,500 lines of tests

---

## Summary

Let a saga be found by an external key (a payment or refund id from a gateway) and wait for an event with a timeout and a compensation. Three parts: correlation keys (name and value) stored with the saga and indexed, with `FindByCorrelationAsync` (on `ISagaCorrelationStore`, Design Choice 8); a wait-for-event step in the low-ceremony definition (`SagaStepBuilder`) that suspends the saga until a matching event arrives or the wait times out and compensates; and an early-event buffer, so an event that arrives before the saga reaches the step (or before the saga exists) is kept and matched later. Payment webhooks arrive out of order and refer to the gateway's ids, not the saga id (SPEC-002 note H, H-R27); reference scenarios S6 and S7 (a refund that waits for the gateway's confirmation and compensates on timeout) verify it (P-25).

**Today's code** (main c49b987e9): `SagaRunner.RunAsync` runs all steps of a `BuiltSagaDefinition<TData>` in one call and returns a `SagaResult<TData>` (`src/Encina.Messaging/Sagas/LowCeremony/`); the runner has no store of its own: it persists through `SagaOrchestrator`, which calls `ISagaStore.AddAsync`/`UpdateAsync` and never `SaveChangesAsync`, so this plan states who saves (Design Choice 9); `SagaStepDefinition` has `Execute` and `Compensate` delegates only; `ISagaStore` finds a saga by id only; `ISagaNotFoundDispatcher` and `IHandleSagaNotFound<TMessage>` handle a message whose saga does not exist (`src/Encina.Messaging/Sagas/`). The saga row has no correlation: the SQL scripts and the EF Core `SagaState` carry an unindexed free-text `CorrelationId` column (EF Core indexes it; `src/Encina.EntityFrameworkCore/Sagas/SagaStateConfiguration.cs`), it is not part of `ISagaState`, and the MongoDB `SagaState` has none, so the providers are not coherent. The timeout fields exist (`TimeoutAtUtc`, `GetExpiredSagasAsync`, `TimeoutAsync`) but nothing calls `TimeoutAsync` (bug #2208).

**Maintainer note recorded on the issue** (decision on #2308, 2026-10-16): the routing of messages by correlation that the original low-ceremony saga request (#41) asked for is delivered here; the attribute, step builder and inline state syntax were superseded by the fluent API. The issue comment of 2026-10-17 records "settled by elimination": correlation keys stored and indexed, `FindByCorrelationAsync`, a wait step with timeout and compensation, early events kept; application-side lookup tables and gateway polling are rejected.

**Not in scope**: the state-machine coordinator (#297 uses `FireAsync(sagaId, trigger)` and can call this plan's lookup), the tenant column of the saga row (#738; the new tables here carry their own tenant), compensation retry (#720), audit (#750).

**Affected packages**: `Encina.Messaging`, `Encina.ADO.SqlServer|PostgreSQL|MySQL`, `Encina.Dapper.SqlServer|PostgreSQL|MySQL`, `Encina.EntityFrameworkCore`, `Encina.MongoDB`, `Encina.Testing.Fakes`.

**Provider category**: Database (10); `SagaStoreEF` and the new buffer store serve three databases each.

**Shared with the saga family**: #738, #720 and #750 also change the saga stores and scripts. This plan owns the correlation key and pending event tables, `ISagaCorrelationStore` (`FindByCorrelationAsync`), `ISagaEventBufferStore`, `ISagaEventRouter` and the wait step. EventId slice 2275-2299 inside `MessagingSaga` (2200-2299), no new range.

---

## Design Choices

<details>
<summary><strong>1. Where correlation keys are stored (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A separate table/collection `SagaCorrelationKeys` (`SagaId`, `SagaType`, `KeyName`, `KeyValue`, `TenantId`, `CreatedAtUtc`) with a composite index on `(TenantId, SagaType, KeyName, KeyValue)`** | Many keys per saga (payment id, refund id); indexed lookup; keys can be added later in the saga's life | A new table in 10 providers; keys and the saga row are written in the order of Design Choice 9 (no cross-store transaction is assumed) |
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
| **A) A buffer store `ISagaEventBufferStore` (table `SagaPendingEvents`: `Id`, `TenantId`, `KeyName`, `KeyValue`, `EventType`, serialized payload, `ReceivedAtUtc`, `ExpiresAtUtc`, `ClaimedUntilUtc`) implemented for the 10 providers** | Works whether the saga exists or not; matched by key, not saga id; purgeable by expiry | Another store and table in 10 providers |
| **B) Reuse the inbox store** | Existing infrastructure | The inbox is for idempotent exactly-once consumption, not for holding a message until a saga reaches a step; its processing model does not fit |
| **C) In memory only** | Simple | Lost on restart, which loses a payment confirmation |

### Chosen Option: **A — A dedicated buffer store** (recommended, pending the maintainer)

### Rationale

- Option A wins because a webhook may arrive before the saga reaches the step or even before the saga exists, and must survive a restart. Option B wins only if the maintainer wants a single message store and accepts reshaping the inbox contract.
- Payloads are serialized with `IMessageSerializer`, as `SagaOrchestrator` does for saga data, so serializer decorators such as encryption apply (see choice 7). `KeyName`, `KeyValue` and `SagaType` stay plaintext columns (they are indexed gateway identifiers), so an encryption decorator does not cover them; the docs say so and the plan never logs or tags a key value.
- The defaults of the buffer (`BufferRetention`, `ExpiryBatchSize`, `ExpiryCheckInterval`, `ClaimLease`) and what happens to an unmatched event that the not-found dispatcher keeps rejecting are Design Choice 10.
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
<summary><strong>7. Serialization of the buffered payload</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Payload through `IMessageSerializer`** | Encryption decorators apply, as they do to saga state | `KeyName`/`KeyValue` stay plaintext (Design Choice 4) |
| **B) Raw JSON** | Simple | Bypasses encryption decorators |

### Chosen Option: **A — Serializer decorators**

### Rationale

- **Class:** Settled
- **Settled by:** the precedent in `SagaOrchestrator`, which uses `IMessageSerializer` "so that decorators such as `EncryptingMessageSerializer` apply to saga state too" (`src/Encina.Messaging/Sagas/SagaOrchestrator.cs`), and `AGENTS.md` section 3 (provider coherence: one serialization path).

</details>

<details>
<summary><strong>7b. Tenant of the new rows and the "tenancy off" value</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Every new row carries `TenantId` from the ambient `IRequestContext`, read as `DeadLetterManager.AmbientTenantId` does (line 100); "tenancy is on" is the `MultiTenancyMarker` registration, the trigger `DeadLetterManager` uses (`_tenancy is not null`, line 155), so #738 and #845 use the same rule; lookups filter by tenant and fail closed when tenancy is on and no tenant resolves; with tenancy off the default-tenant constant that #738/#1164 decides applies, and if that constant is `default` a tenant id `default` is refused under tenancy (the reserved-`default` rule) so it cannot clash with the no-tenant value** | Complies with SPEC-002 REQ-061 and AC-043; one trigger across the saga stores | The default-tenant constant is decided elsewhere (#738/#1164), so the "tenancy off" half waits for it |
| **B) No tenant column** | Simple | Violates REQ-061 |

### Chosen Option: **A — tenant on every row, following the #738 decision** (recommended, pending the maintainer)

### Rationale

- The fail-closed half is fixed by `AGENTS.md` section 3 (compliance and security gates fail closed) and SPEC-002 REQ-061/AC-043; the value used with tenancy off is not decided by this plan: it follows whatever #738/#1164 decide.

**Class:** Dilemma

</details>

<details>
<summary><strong>8. Interface placement and names</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A new `ISagaCorrelationStore` (`AddCorrelationKeyAsync`, `FindByCorrelationAsync`, `TryAdvanceAsync`) and a new `ISagaEventBufferStore`, both opt-in and additive; routing in `ISagaEventRouter`; classes `SagaCorrelationStoreADO|Dapper|EF|MongoDB` and `SagaEventBufferStoreADO|Dapper|EF|MongoDB`, `{Pattern}Store{Provider}` per `AGENTS.md` section 4** | Nothing existing breaks, so each pull request builds alone and each provider lands in its own pull request; a deployment that never uses a wait step implements nothing; third-party `ISagaStore` implementations are untouched | One more interface and a second store instance per provider; keys and the saga row do not share a store object (ordering in Design Choice 9 makes this safe) |
| **B) Add the three members to `ISagaStore`** | One interface; keys written through the same store instance as the saga row | Every `ISagaStore` implementation (nine stores, `FakeSagaStore`, any third-party store) must implement keys even if wait steps are unused, so the interface change and all implementers must land together: far over 15 files, which needs the `size-exception-approved` label |
| **C) Everything on `ISagaStore`, buffer included** | One interface | Every saga store must implement the buffer too |

### Chosen Option: **A — separate additive interfaces** (recommended, pending the maintainer)

### Rationale

- A wins on the size rule and on pay-for-what-you-use; B wins if the maintainer wants keys inside the saga store's own unit of work and accepts one very large, labelled pull request. Both A and B satisfy the cited store-naming and provider-coherence rules, so this is a choice, not a settled point.

**Class:** Dilemma

</details>

<details>
<summary><strong>9. Concurrency and persistence protocol: who writes, who reads, and in what order</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A self-contained protocol: the buffer is claimed, not destructively taken (`ClaimMatchingAsync` sets `ClaimedUntilUtc` atomically; the row is deleted only after the resume advanced the saga), the saga cursor advances with a conditional update (`ISagaCorrelationStore.TryAdvanceAsync(sagaId, expectedStep, ...)` succeeds only while the saga is still at the awaited wait step), and writes are ordered write-before-read on both sides (below). The runner calls `ISagaStore.SaveChangesAsync` after each persistence group** | Safe on every provider, including MongoDB with no scope transaction; no event is lost by a crash after the claim; two resumes, or a resume racing the scheduled timeout, are decided by one compare-and-set; independent of #2213 | A claim column and a lease; handlers are at-least-once (cursor advance is exactly once) |
| **B) Make #2213 (concurrency control on the saga row) an explicit prerequisite and rely on its conditional update; keep a destructive `TakeMatchingAsync` in the resume's transaction on SQL providers** | No second mechanism if #2213 lands first | #2213 is an open debt issue about `TimeoutAsync` doing an unconditional read-modify-write, not a delivered mechanism; MongoDB and EF (where the stores only track and `SaveChangesAsync` is separate) have no scope transaction to put the take in; a crash after the take and before the save loses the event |

**The protocol of A**, which the property test of Phase 9 exercises over every interleaving:

1. Runner at a wait step: (1) persist the cursor at the wait step and call `ISagaStore.SaveChangesAsync`; (2) `AddCorrelationKeyAsync`; (3) schedule the timeout message (Phase 3); (4) `ClaimMatchingAsync` for a buffered event; if one is claimed, resume with it, otherwise return `Waiting`.
2. Router on an incoming event: (1) `AddAsync` the event to the buffer (committed); (2) `FindByCorrelationAsync`; if a key exists, `ClaimMatchingAsync` and resume; if not, return `Buffered`. Each side writes its own record before reading the other's, so at least one of them sees the other: an event buffered between the runner's claim and the router's insert is found by the router's re-check, and an event buffered before the key is found by the runner's claim. The claim is atomic, so exactly one of them wins; the other gets an empty claim and does nothing.
3. Resume: load the saga and check it waits for `TEvent` at the cursor (else `saga.event_type_mismatch`); run the handler; `TryAdvanceAsync`; if it returns `Right(false)` another resume or the timeout won, the claimed event goes to the not-found handler path (the saga no longer waits) and the row is deleted; on `Right(true)` delete the claimed row and continue with the remaining steps. A crash after the advance and before the delete leaves a claim that expires; the re-claim finds the saga already advanced and deletes the row.
4. Timeout (Phase 3): the handler calls `TryAdvanceAsync` to the compensating state; losing the compare-and-set means the event arrived first and nothing is compensated.
5. Who saves: the runner (per persistence group) calls `ISagaStore.SaveChangesAsync` (EF Core flushes its change tracker there; ADO.NET and Dapper commit each statement unless the caller supplies a scope transaction through `IDbTransactionAccessor`; MongoDB writes immediately). No cross-store transaction is assumed anywhere.
6. EF Core and the change tracker: `ExecuteUpdateAsync` (the claim and `TryAdvanceAsync` of the EF stores) bypasses the change tracker, while the runner persists the cursor through the tracked `SagaState` and `SaveChangesAsync` in the same scope; a tracked entity with a stale `CurrentStep` could be flushed over the compare-and-set result. The EF stores therefore detach (or reload) the tracked `SagaState` after `TryAdvanceAsync` returns, and an EF-store test proves that a later `SaveChangesAsync` does not overwrite the advanced cursor.

### Chosen Option: **A — the claim and compare-and-set protocol** (recommended, pending the maintainer)

### Rationale

- A wins because it is correct on every provider without a transaction that MongoDB does not have; B wins if #2213 lands first and the maintainer prefers one concurrency mechanism for all saga transitions (the compare-and-set of A is then replaced by #2213's, nothing else changes).

**Class:** Dilemma

</details>

<details>
<summary><strong>10. Defaults and the fate of an event nobody can accept</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `BufferRetention` 7 days, `ExpiryBatchSize` 100, `ExpiryCheckInterval` 1 minute, `ClaimLease` 5 minutes, `MaxWaitTimeout` per step required (no default wait without a timeout); an expired event goes to `ISagaNotFoundDispatcher`; when the dispatcher keeps returning `Left` the row is **kept** (never silently dropped), counted with outcome `undeliverable` and surfaced by the deferred backlog health check** | Nothing is lost silently; bounded work per iteration | A permanently rejected event stays until an operator acts |
| **B) The same defaults, but after `MaxDispatchAttempts` (for example 5) the row moves to the dead letter store** | The backlog cannot grow without bound | Needs a dead letter integration and a decision on who owns the redrive |

### Chosen Option: **A — keep and surface** (recommended, pending the maintainer)

### Rationale

- The values are user-visible and no rule fixes them. A wins on simplicity and on "errors are never swallowed"; B wins when an unbounded backlog is a bigger risk than a manual redrive.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Core Models and Contracts

> **Goal**: The contract in `Encina.Messaging`.

<details>
<summary>Tasks</summary>

1. `SagaCorrelationKey` (sealed record): `SagaId`, `SagaType`, `KeyName`, `KeyValue`, `TenantId`, `CreatedAtUtc`.
2. `ISagaCorrelationStore` (new, additive; Design Choice 8 A): `AddCorrelationKeyAsync(SagaCorrelationKey key, CancellationToken)`, `FindByCorrelationAsync(string sagaType, string keyName, string keyValue, CancellationToken)` returning `Either<EncinaError, Option<ISagaState>>`, and `TryAdvanceAsync(Guid sagaId, int expectedStep, ISagaState next, CancellationToken)` returning `Either<EncinaError, bool>` (the conditional cursor advance of Design Choice 9; `Right(false)` means the saga is no longer at the expected step).
3. `PendingSagaEvent` (sealed record) and `ISagaEventBufferStore`: `AddAsync`, `ClaimMatchingAsync(keyName, keyValue, eventType, claimUntilUtc, ct)` (atomically marks and returns the unclaimed or lease-expired matching rows; it does not delete), `GetExpiredAsync(batchSize, ct)` (unclaimed, past `ExpiresAtUtc`), `DeleteAsync(ids, ct)`.
4. `ISagaEventRouter`: `ValueTask<Either<EncinaError, SagaEventRoutingResult>> RouteAsync<TEvent>(string sagaType, string keyName, string keyValue, TEvent @event, CancellationToken)`; `SagaEventRoutingResult` (enum-like record: Delivered, Buffered, NotFound); the protocol is Design Choice 9.
5. `SagaErrorCodes`: `saga.correlation_key_conflict`, `saga.wait_timeout`, `saga.event_type_mismatch`, each created with an `ErrorCategory` where the `EncinaError` is built (ADR-062, `EncinaErrors.Create(code, ErrorCategory, ...)`; hosts never parse the code text): a duplicate key is `AlreadyExists` (R2), an event for a saga that no longer waits for that type is `Conflict` (R2: an invalid transition), a wait timeout is `Timeout`. `saga.event_type_mismatch` is not `InvalidRequest` because the event itself is well formed: it is the saga state that rejects it. A unit test per code asserts `GetCategory`.
6. `PublicAPI.Unshipped.txt` of `Encina.Messaging`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```
CONTEXT:
- Issue #1237; plan docs/plans/saga-correlation-implementation-plan-1237.md. Dilemmas D1-D5, D7b and D8-D10 are recommended, pending the maintainer: follow the Maintainer Decisions section if it exists.
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
3. `SagaRunner`: at a wait step it follows the runner protocol of Design Choice 9: persist the cursor and `SaveChangesAsync`, `AddCorrelationKeyAsync`, schedule the timeout (the `SagaWaitTimeout` message record is declared in PR 2, its handler and the scheduling check arrive in PR 3b, so PR 2 is not enabled for timed waits on its own), `ClaimMatchingAsync`; a claimed event is applied at once, otherwise the runner returns `SagaResult` with `IsWaiting = true`. The runner takes `ISagaCorrelationStore` and `ISagaEventBufferStore` as constructor dependencies only for sagas that contain a wait step (a wait definition without them is a registration error at startup, ADR-050 rule 3).
4. `ISagaRunner.ResumeAsync<TData, TEvent>(BuiltSagaDefinition<TData> definition, Guid sagaId, TEvent @event, CancellationToken)`: loads the saga, checks it waits for `TEvent` (else `saga.event_type_mismatch`), runs the handler, `TryAdvanceAsync` (a `Right(false)` loses the race and is a no-op, Design Choice 9) and continues with the remaining steps; a failure compensates in reverse order as today. The handler may run twice after a crash (at-least-once); the cursor advances once.
5. Honour every `Left` of the orchestrator calls on this path (same defect as #1581).
6. `[LoggerMessage]` entries for the runner in `Diagnostics/SagaCorrelationLogMessages.cs` (created here, ids 2275 upward, sequential; later pull requests append to the same file, each with the messages its own code emits), and `Diagnostics/SagaCorrelationDiagnostics.cs` with the span `encina.saga.correlate` and the counters of Phase 8, so the runner can emit them in this pull request (Phase 8 only adds the tenant tag wiring and the exporter test).

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```
CONTEXT:
- Phase 1 added the contract (issue #1237). The runner today executes all steps in one call.

TASK:
Implement the wait step in SagaStepBuilder, SagaStepDefinition, SagaRunner and ISagaRunner.ResumeAsync.

KEY RULES:
- A Left from any store call is returned; a persisted cursor survives a restart; follow the claim and compare-and-set protocol of Design Choice 9 exactly.
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

Tasks 1, 2 and 5 (without the scheduling check) ship in PR 3a; tasks 3, 4, the scheduling check of task 5 and task 6 ship in PR 3b, so the ADR-050 amendment lands in the same pull request as the first code that enforces the Saga to Scheduling requirement (ADR-050 rule 3).

1. `SagaDefinitionRegistry` (new, singleton): maps (saga type, event type name) to the `BuiltSagaDefinition<TData>` and a type-erased resume delegate (`Func<Guid sagaId, string payload, CancellationToken, ValueTask<Either<EncinaError, Unit>>>`) built when a definition with a wait step is registered (`AddEncinaSagaWait<TData, TEvent>(definition)`); it owns the `TData`/`TEvent` knowledge and the deserialization of the payload through `IMessageSerializer`, because the router, the expiry processor and the timeout handler hold only strings and a serialized payload (`ISagaNotFoundDispatcher.DispatchAsync<TMessage>` is generic too).
2. `SagaEventRouter` (`ISagaEventRouter`): the router protocol of Design Choice 9 (`AddAsync` to the buffer, then `FindByCorrelationAsync`, then `ClaimMatchingAsync` and the registry's resume delegate); no key found: return `Buffered`.
3. `SagaEventBufferExpiryProcessor` (`BackgroundService`, `PeriodicTimer` loop that catches per iteration): expired buffered events go to `ISagaNotFoundDispatcher` (choice 6); a dispatcher `Left` keeps the row for the next iteration (Design Choice 10: never silently dropped).
4. Wait timeout (D5 recommendation): a `SagaWaitTimeout` scheduled message handled by a handler that calls `TryAdvanceAsync` to the compensating state; if it wins (the saga still waits) it runs the compensation path of Phase 2 and records `saga.wait_timeout`, if it loses the event arrived first and nothing is compensated.
5. Registration `AddEncinaSagaCorrelation()` (TryAdd): router, registry, processor, options with a validator (`BufferRetention`, `ExpiryBatchSize`, `ExpiryCheckInterval`, `ClaimLease`; defaults per Design Choice 10), timeout handler; startup validation when a definition has a wait and no `ISagaCorrelationStore` or `ISagaEventBufferStore` is registered, or a wait has a timeout and scheduling is off; a DI test with `ValidateOnBuild` and `ValidateScopes`.
6. ADR-050 amendment (an edit of the existing ADR-050, named separately from the new ADR of Phase 10) for the Saga to Scheduling requirement (D5 A).

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

1. Scripts: `Scripts/0NN_CreateSagaCorrelationKeysTable.sql` and `Scripts/0NN_CreateSagaPendingEventsTable.sql` (next free numbers in each package) and the same tables in `000_CreateAllTables.sql`, in `Encina.ADO.SqlServer|PostgreSQL|MySQL`; remove the free-text `CorrelationId` column from `003_CreateSagaStatesTable.sql` and `000_CreateAllTables.sql`. Unique index `(TenantId, SagaType, KeyName, KeyValue)` (D2 A); index on `(TenantId, KeyName, KeyValue, EventType)` and `ExpiresAtUtc` for the buffer. `TenantId VARCHAR(128)` as the dead letter tables (MySQL `utf8mb4_bin`); the buffer table has `ClaimedUntilUtc`.
2. `Sagas/SagaCorrelationStoreADO.cs` (3, one per package: the two key methods and `TryAdvanceAsync` as `UPDATE ... WHERE SagaId = @id AND CurrentStep = @expected`, affected rows 1 means `Right(true)`); `Sagas/SagaEventBufferStoreADO.cs` (3, `ClaimMatchingAsync` is one atomic statement: SQL Server `UPDATE TOP (1) ... OUTPUT inserted.*`, PostgreSQL `UPDATE ... WHERE Id = (SELECT Id ... FOR UPDATE SKIP LOCKED LIMIT 1) RETURNING *`, MySQL a short `SELECT ... FOR UPDATE SKIP LOCKED` plus `UPDATE` inside its own transaction because MySQL has no `RETURNING`).
3. `ServiceCollectionExtensions` of each package registers both stores (`TryAdd`, store wins over any in-memory default in any registration order).
4. One pull request per package (PRs 4 to 6), each carrying its own tests for every flag: a unit test, a guard test, a thin subclass of the contract base classes of PR 1 (keys, claim and `TryAdvanceAsync` on the real database through the package's shared collection) and one integration class (S6, S7 and the two-tenant test).

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

The same as Phase 4 in `Encina.Dapper.SqlServer|PostgreSQL|MySQL`: scripts, `Sagas/SagaCorrelationStoreDapper.cs` (3), `Sagas/SagaEventBufferStoreDapper.cs` (3), registration; one pull request per package (PRs 7 to 9), each with the same four test files as Phase 4 task 4 (unit, guard, contract subclass, integration).

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

1. EF Core (PRs 10a and 10b; 10a is the production code with its unit and guard tests, 10b the contract subclass and the integration class per database, three databases): entities `SagaCorrelationKeyEntity` and `SagaPendingEventEntity`, their configurations (unique index per D2 A), `SagaCorrelationStoreEF` (`TryAdvanceAsync` through `ExecuteUpdateAsync` with the step condition, then the tracked `SagaState` is detached or reloaded so a later `SaveChangesAsync` cannot overwrite the advanced cursor, Design Choice 9 point 6; a unit test on the store proves it), `SagaEventBufferStoreEF` (claim through `ExecuteUpdateAsync`); remove `CorrelationId` from `SagaState` and its configuration; one implementation serving SQL Server, PostgreSQL and MySQL, with integration tests for the three databases. No migration (pre-1.0).
2. MongoDB (PR 11, with its unit, guard, contract-subclass and integration tests): documents and collections (`Collections.SagaCorrelationKeys`, `SagaPendingEvents` in the options), `SagaCorrelationStoreMongoDB` (`TryAdvanceAsync` as a filtered `UpdateOne`), `SagaEventBufferStoreMongoDB` (claim as `FindOneAndUpdate`), unique and lookup indexes in `MongoDbIndexCreator`. No scope transaction is needed.
3. `Encina.Testing.Fakes` (PR 12): `FakeSagaCorrelationStore` and `FakeSagaEventBufferStore` (new classes; `FakeSagaStore` is not touched), with their contract subclasses (the abstract contract base classes ship in PR 1 and each store PR subclasses them), the property tests and their guard and unit tests.

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

1. **Validation**: options for the buffer (`BufferRetention`, `ExpiryBatchSize`, `ExpiryCheckInterval`, `ClaimLease`) validated at start-up with an `IValidateOptions` validator (registered in Phase 3; this task adds only its remaining tests).
2. **Distributed locks**: not used. Concurrent events for one saga, two resumes, or a resume racing the scheduled timeout are decided by the atomic claim and the compare-and-set `TryAdvanceAsync` (Design Choice 9); the unique key serializes key creation only. #2213 is not a mechanism this plan relies on.
3. **Transactions**: no cross-store transaction is assumed (MongoDB has none, and `SagaOrchestrator` never saves). The runner calls `ISagaStore.SaveChangesAsync` per persistence group and the steps are ordered and idempotent (Design Choice 9); the saga state and any outbox write of the resumed step share a scope only where the host already provides one.
4. **Idempotency**: a duplicate event is matched once: the claim is atomic and the cursor advance is a compare-and-set, so a duplicate finds the saga already advanced and its row is deleted; the handler is at-least-once.
5. **Multi-tenancy**: choices 7 and 7b.
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

1. Span `encina.saga.correlate` (attributes `encina.tenant_id`, `encina.saga.type`) on `SagaActivitySource`; counters `encina.saga.wait_timeouts` and `encina.saga.early_events` (`Counter<long>`, no `_total` suffix, on the package-named `Encina.Messaging` meter per ADR-054 D3; tags `encina.saga.type`, `outcome` and `encina.tenant_id` when the `MultiTenancyMarker` is registered, none when it is not, per ADR-054 D4); no payload, response body, key value or data-subject identifier in any attribute, tag or log (AC-044).
2. Log messages in 2275-2299 ship with the pull request whose code emits them (PR 2 runner, PR 3a router, PR 3b timeout handler and expiry processor), sequential ids in `SagaCorrelationLogMessages.cs`; the span and counters are defined in PR 2 (Phase 2 task 6) and this phase adds the tenant tag wiring; run `EncinaEventIdAllocationTests` in each of those pull requests.
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
3. **Contract**: `ISagaCorrelationStore` and `ISagaEventBufferStore` identical across all stores and fakes (claim atomicity, `TryAdvanceAsync` `Right(false)` on a moved cursor). The abstract base classes ship in PR 1; each store and fakes pull request adds its thin subclass, so each pull request carries its own contract coverage.
4. **Property (FsCheck)**: any arrival order of the event relative to the wait step ends in the same saga state; and every interleaving of the router's two steps (`AddAsync`, `FindByCorrelationAsync`) with the runner's two steps (`AddCorrelationKeyAsync`, `ClaimMatchingAsync`), and of a resume with the timeout, ends with exactly one resume and no lost event.
5. **Integration**: all 10 providers through the shared collections, including S6 and S7 end to end and the two-tenant test; `ClearAllDataAsync` in `InitializeAsync`; never `IClassFixture`. Each provider's tests ship in that provider's pull request.
6. **Load / Benchmark**: justification files `tests/Encina.LoadTests/Sagas/SagaCorrelation.md` and `tests/Encina.BenchmarkTests/Sagas/SagaCorrelation.md` (not a concurrent hot path).
7. **Per-file targets** with one-sentence justifications in the manifest of every touched package (`mechanical-fixer`, in the PR that adds the file; targets never go down). Proposed:

   | File | Unit | Guard | Contract | Property | Integration | Justification |
   |------|-----:|------:|---------:|---------:|------------:|---------------|
   | `SagaRunner.cs` (wait paths), `SagaStepBuilder.cs` | 95 | 100 | 0 | 90 | 80 | Suspend, resume, timeout and compensation are pure over fakes; the interleaving property and S6/S7 integration cover the rest |
   | `SagaEventRouter.cs`, `SagaDefinitionRegistry.cs` | 95 | 100 | 0 | 90 | 80 | Delivered, buffered, not found, lost race; the property proves no lost event |
   | `SagaEventBufferExpiryProcessor.cs`, timeout handler | 95 | 100 | 0 | 0 | 0 | Loop catches per iteration on `FakeTimeProvider`; a dispatcher `Left` keeps the row |
   | Options and validator | 100 | 100 | 0 | 0 | 0 | Four rules, no I/O |
   | `SagaCorrelationStore*`, `SagaEventBufferStore*` (10 providers) | 70 | 100 | 90 | 0 | 85 | SQL and driver code is proven by the contract suite and real databases, not by mocks; guard on parameters |
   | `FakeSagaCorrelationStore`, `FakeSagaEventBufferStore` | 95 | 100 | 90 | 90 | 0 | In-memory logic exercised by the contract and property suites |

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

1. XML docs; `PublicAPI.Unshipped.txt` per package; `changelog.d/1237-saga-correlation.added.md`; package README note; configuration guide `docs/messaging/sagas.md`; an ADR recording D1-D5 and D8-D10 (ADR number assigned when the ADR pull request opens) and, named separately, the amendment of the existing ADR-050 (Phase 3 task 6, which ships in PR 3b, not here).
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

Counts include tests, `PublicAPI.*.txt` and `.github/coverage-manifest/*.json`. With the additive interfaces of Design Choice 8 A nothing existing breaks, so every pull request builds alone, and each provider ships its own tests (AGENTS.md section 9: the PR that adds a file meets its flags). If the maintainer picks Design Choice 8 B, the interface change and all its implementers cannot be split and need the `size-exception-approved` label.

| PR | Content | Files (production + PublicAPI + manifest + tests) | Approx. lines |
|----|---------|---------------|---------------|
| 1 | Phase 1: contract (interfaces, records, `SagaErrorCodes` with categories) and the abstract contract base classes | 13 (6 production, `PublicAPI.Unshipped.txt`, manifest, unit, guard, category test, 2 contract base classes) | 800 |
| 2 | Phase 2: wait step and runner, with its log messages, span and counters | 13 (builder, step definition, definition, runner, `ISagaRunner`, `SagaCorrelationLogMessages.cs`, `SagaCorrelationDiagnostics.cs`, `PublicAPI.Unshipped.txt`, manifest, unit (2), guard, property: interleavings) | 1,400 |
| 3a | Phase 3: registry, router, options and validator, DI, router log messages | 12 (5 production, log messages edit, `PublicAPI.Unshipped.txt`, manifest, unit (2), DI and validator test, guard) | 1,000 |
| 3b | Phase 3: expiry processor, scheduled timeout (message and handler in one file), scheduling startup check, ADR-050 amendment, their log messages | 11 (processor, timeout message and handler, DI edit, log messages edit, `PublicAPI.Unshipped.txt`, manifest, ADR-050 edit, unit (2), guard edit) | 800 |
| 4 | Phase 4: ADO.NET SqlServer | 14 (4 scripts, 2 stores, registration, manifest, `PublicAPI.Unshipped.txt`, unit, guard, contract subclass, 1 integration) | 900 |
| 5 | Phase 4: ADO.NET PostgreSQL | 14 | 900 |
| 6 | Phase 4: ADO.NET MySQL | 14 | 950 |
| 7 | Phase 5: Dapper SqlServer | 14 | 900 |
| 8 | Phase 5: Dapper PostgreSQL | 14 | 900 |
| 9 | Phase 5: Dapper MySQL | 14 | 950 |
| 10a | Phase 6a: EF Core production | 12 (2 entities, 2 configurations, 2 stores, registration, `SagaState` mod, manifest, `PublicAPI.Unshipped.txt`, unit, guard) | 900 |
| 10b | Phase 6a: EF Core tests on the three databases | 6 (3 contract subclasses, 3 integration classes, one per `EFCore-<Db>` collection) | 600 |
| 11 | Phase 6b: MongoDB | 14 (2 documents, 2 stores, index creator, options, registration, manifest, `PublicAPI.Unshipped.txt`, unit, guard, contract subclass, 1 integration) | 1,000 |
| 12 | Phase 6c: fakes, contract subclasses, property suite | 9 (2 fakes, 2 contract subclasses, property, unit, guard, manifest, `PublicAPI.Unshipped.txt` of the fakes package) | 900 |
| 13 | Phases 7-8: tenant tag wiring, two-tenant tests, exporter test | 5 (diagnostics edit, exporter test, two-tenant test, manifest, `PublicAPI.Unshipped.txt`) | 400 |
| 14 | Phase 10: documentation, ADR, fragment, justification files | 7 | 500 |

---

## Research

### Relevant Standards & Specifications

| Reference | What it defines | Use in this feature |
|-----------|-----------------|---------------------|
| SPEC-002 REQ-044, AC-039, scenarios S6 and S7 | Saga correlation and wait-for-event | Acceptance |
| SPEC-002 REQ-061, AC-043 | Tenant-aware capabilities | Choices 7 and 7b |
| SPEC-002 REQ-062, AC-044; ADR-054 D3 and D4 | Instrumentation with the tenant as attribute and, when tenancy is on, as a metric tag; package-named meter; no payloads | Phase 8 |
| ADR-062 | Error category set where the error is created | `SagaErrorCodes` (Phase 1) |
| Enterprise Integration Patterns: Correlation Identifier, Process Manager | Message correlation to a process instance | Design vocabulary |
| MassTransit sagas (correlation ids), NServiceBus sagas (correlation property) | Comparable products | Key model |
| ADR-050 | Module requirements | Scheduling requirement (D5 A) |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `SagaRunner`, `SagaStepBuilder`, `SagaDefinition` | `src/Encina.Messaging/Sagas/LowCeremony/` | Wait step and resume |
| `SagaOrchestrator`, `ISagaStore` and nine store files | `src/Encina.Messaging/Sagas/`, `src/Encina.*/Sagas/` | Persistence of the saga row (untouched interface); `SaveChangesAsync` called by the runner |
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
| ADO-SqlServer, ADO-PostgreSQL, ADO-MySQL | Required (`SagaCorrelationStoreADO`) | `SagaEventBufferStoreADO` | `[Collection("ADO-<Db>")]` |
| Dapper-SqlServer, Dapper-PostgreSQL, Dapper-MySQL | Required (`SagaCorrelationStoreDapper`) | `SagaEventBufferStoreDapper` | `[Collection("Dapper-<Db>")]` |
| EFCore-SqlServer, EFCore-PostgreSQL, EFCore-MySQL | one implementation (`SagaCorrelationStoreEF`) | `SagaEventBufferStoreEF` | `[Collection("EFCore-<Db>")]` x3 |
| MongoDB | Required (`SagaCorrelationStoreMongoDB`) | `SagaEventBufferStoreMongoDB` | MongoDB fixture |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Messaging core | ~25 | Contract, builder, runner, registry, router, processor, handler, options, DI, logs, diagnostics, manifests, PublicAPI |
| ADO.NET x3 and Dapper x3 | ~60 | Scripts (4 per package), two new stores, registration, manifest, PublicAPI |
| EF Core, MongoDB, fakes | ~30 | Entities, configurations, stores, indexes, fakes, manifests, PublicAPI |
| Tests | ~64 | Unit, guard, contract bases and subclasses, property, integration per provider, DI, two justifications |
| Documentation | ~6 | Fragment, guide, ADR, README note, ADR-050 amendment (in PR 3b) |
| **Total** | **~185 across 16 pull requests** (the sum of the split table, which includes manifests and PublicAPI files) | |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no [Obsolete], no migrations, no compatibility aliases; XML docs; zero warnings.
- Issue #1237 (SPEC-002 P-35, REQ-044, AC-039): saga correlation by external key, a wait-for-event step with timeout and compensation, early events kept. Plan: docs/plans/saga-correlation-implementation-plan-1237.md; Dilemmas D1-D5, D7b and D8-D10 are recommended, pending the maintainer.

IMPLEMENTATION OVERVIEW:
1. Contract: keys, buffer store, router (Phase 1).
2. Wait step, suspend and resume in SagaRunner (Phase 2).
3. Router, expiry processor, scheduled timeout, DI (Phase 3).
4. Providers: ADO.NET, Dapper (Phases 4-5); EF Core, MongoDB, fakes (Phase 6).
5. Cross-cutting, observability, tests, documentation (Phases 7-10).

KEY PATTERNS:
- Tenant on every new row from the ambient IRequestContext (trigger: MultiTenancyMarker, as DeadLetterManager), fail closed; payloads through IMessageSerializer.
- Follow the claim and compare-and-set protocol of Design Choice 9 (no destructive take, no cross-store transaction); new stores are additive interfaces (ISagaCorrelationStore, ISagaEventBufferStore).
- Error codes are created with an ErrorCategory (ADR-062); sixteen pull requests of at most 15 files, each with its own tests.
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
| 2 | OpenTelemetry | ✅ | Phase 8: `encina.saga.correlate` span, `encina.saga.wait_timeouts` and `encina.saga.early_events` counters, tenant as span attribute and metric tag when tenancy is on (REQ-062, ADR-054 D4) |
| 3 | Structured Logging | ✅ | Phase 2 and 8: messages in 2275-2299, error code only |
| 4 | Health Checks | ⏭️ | Deferred: a buffered-event backlog check extends `SagaHealthCheck`; issue file `artifacts/issues/saga-event-buffer-health-check.md` |
| 5 | Validation | ✅ | Phase 7: options validated at start-up |
| 6 | Resilience | ❌ | Not applicable: no call to an external system; the timeout is durable infrastructure |
| 7 | Distributed Locks | ❌ | Not applicable: concurrent events, two resumes and a resume racing the timeout are decided by the atomic claim and the compare-and-set `TryAdvanceAsync` (Design Choice 9), which also work on MongoDB; no lock provider is added |
| 8 | Transactions | ✅ | Phase 7: no cross-store transaction is assumed; the runner saves per persistence group in the order of Design Choice 9 and the steps are idempotent |
| 9 | Idempotency | ✅ | Phase 7: a duplicate event is matched once (atomic claim plus compare-and-set; the handler is at-least-once) |
| 10 | Multi-Tenancy | ✅ | Choices 7b: tenant on every new row (REQ-061, AC-043), `MultiTenancyMarker` trigger, reserved-`default` rule |
| 11 | Module Isolation | ❌ | Not applicable: SPEC-002 requires no module scoping; `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ⏭️ | Deferred: saga transition audit is #750; resumed and timed-out waits become events there |

---

## Prerequisites & Dependencies

- #738 (tenant on the saga row) and the default-tenant decision of #1164: the new tables carry their own tenant column and follow the same rule.
- #2213 (an open debt issue about `TimeoutAsync` doing an unconditional read-modify-write) is not a mechanism this plan relies on: the compare-and-set `TryAdvanceAsync` of Design Choice 9 is part of this plan; if #2213 delivers optimistic concurrency first, Design Choice 9 B applies.
- No prerequisite issue blocks pull request 1.
- #2208 (timeouts never enforced) only if D5 is answered B.
- #1581 (SagaRunner discards `Left` results) touches the same lines as Phase 2.
- #720 and #750 change the same stores and scripts: agree the merge order.

---

## Next Steps

1. The maintainer answers D1 to D5, D7b and D8 to D10 when work on #1237 starts; the answers go into `## Maintainer Decisions` and the markers are removed.
2. Open the deferred health-check issue and link it from #1237.
3. Brief pull request 1 first.
