# Implementation Plan: Audit Trail for Saga State Transitions

> **Issue**: [#750](https://github.com/dlrivada/Encina/issues/750)
> **Type**: Feature
> **Complexity**: Medium (7 phases, no database provider of its own, about 25 files in 4 pull requests)
> **Estimated Scope**: ~700 lines of production code + ~1,500 lines of tests

---

## Summary

Record every saga state transition as an operation audit entry, opt-in, so the lifecycle of a distributed transaction (start, step executed, step failed, compensating, compensated, completed, failed, timed out) can be traced from the audit store. The orchestrator (`src/Encina.Messaging/Sagas/SagaOrchestrator.cs`) today only writes `[LoggerMessage]` lines for its transitions; nothing reaches an audit store.

**The dependency problem**: the audit abstraction of `AGENTS.md` section 6, function 12 is `IOperationAuditStore` (`src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs`; the issue's `IAuditStore` does not exist), and `Encina.Security.Audit` references `Encina.Messaging` (`src/Encina.Security.Audit/Encina.Security.Audit.csproj`), so `Encina.Messaging` cannot reference it back. The plan introduces a small sink abstraction owned by `Encina.Messaging` and an adapter in `Encina.Security.Audit` (Dilemma D1).

**Prerequisite that affects correctness**: `SagaRunner` discards the `Left` results of the orchestrator's state-persisting calls (#1581, open). An audit write that fails with a `Left` inside `AdvanceAsync`/`CompleteAsync` would be lost by the runner. #1581 should land first or travel with the first pull request.

**Not in scope**: audit of the outbox and inbox (#749), the `TenantId` column on the saga row (#738), entity-change history (`IAuditLogStore`, #1778).

**Affected packages**: `Encina.Messaging` (sink abstraction, event record, orchestrator emission, opt-in flag) and `Encina.Security.Audit` (adapter and registration).

**Provider category**: none of its own. The audit store implementations keep their coverage: `OperationAuditStoreADO` (3), `OperationAuditStoreDapper` (3), `OperationAuditStoreEF`, `OperationAuditStoreMongoDB` and `InMemoryOperationAuditStore` already exist; the plan adds no store and no SQL.

**Shared with the saga family**: #738 adds the tenant to the saga row (the entry carries it once it lands), #720 adds compensation failure states the audit must cover, #1237 adds wait-for-event transitions. This plan owns the sink abstraction and the event set; EventId slice 2250-2274 inside `MessagingSaga` (2200-2299), no new range.

---

## Design Choices

<details>
<summary><strong>1. Dependency direction: how Messaging reaches the audit store (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A small sink interface `ISagaAuditSink` owned by `Encina.Messaging`, implemented in `Encina.Security.Audit` by an adapter over `IOperationAuditStore`** | No project cycle, no new dependency in Messaging; other sinks are possible | One more abstraction and one more registration |
| **B) Move the audit-entry abstraction (`OperationAuditEntry` and the store interface) into core `Encina`** | One abstraction for every package | Breaks the public API of `Encina.Security.Audit` and widens core |
| **C) The orchestrator publishes an `INotification` (for example `SagaTransitioned`) and `Encina.Security.Audit` handles it** | Reuses the notification mechanism; no new interface | The orchestrator needs a publisher; a failing handler is not returned to the orchestrator unless publish results are honoured, which weakens D3 |

### Chosen Option: **A — A sink owned by Messaging, adapter in Security.Audit** (recommended, pending the maintainer)

### Rationale

- Option A wins when applications use Messaging without Security.Audit (the sink is optional) and the failure of the audit write must come back to the orchestrator as a `Left`. Option B wins when several other packages (outbox, inbox: #749) will also be audited and a shared abstraction is worth the API break. Option C wins if the maintainer prefers to model audit as an event consumed by any number of handlers.
- **Class:** Dilemma

</details>

<details>
<summary><strong>2. Atomicity of the audit record with the state change (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Record after the saga state was saved; a failure leaves the transition applied but unaudited and returns `Left`** | Simple; no shared transaction | A gap is possible between state and audit |
| **B) Write both in the same unit of work** | No gap | The audit store must join the saga store's connection or transaction, possible only when both use the same database provider |

### Chosen Option: **A — Record after the save** (recommended, pending the maintainer)

### Rationale

- Option A wins when the audit store and the saga store do not share a connection (different providers, MongoDB, the in-memory store). Option B wins in regulated deployments where a gap is unacceptable; it is tracked for when `IOperationAuditStore` gains transaction support.
- The caller of the orchestrator receives the `Left` from the audit write, so the gap is never silent (see choice 5).
- **Class:** Dilemma

</details>

<details>
<summary><strong>3. Which audit abstraction records the event</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IOperationAuditStore` (operation audit entries)** | The abstraction `AGENTS.md` section 6 names for operation audit; stores exist for the 10 providers | Entries need a `CorrelationId`, `Action`, `EntityType` |
| **B) `IAuditLogStore` (entity change history)** | None for this purpose | It records entity changes, not operations; #1778 renames it |

### Chosen Option: **A — `IOperationAuditStore`**

### Rationale

- **Class:** Settled
- **Settled by:** `AGENTS.md` section 6, function 12 (`IOperationAuditStore`, `IEntityChangeAuditStore` (today `IAuditLogStore`, #1778), `IReadAuditStore` (ADR-036)); the issue itself.
- Mapping to `OperationAuditEntry` (`src/Encina.Security.Audit/OperationAuditEntry.cs`): `Action` = `Saga.<Event>`, `EntityType` = `Saga`, `EntityId` = saga id, `CorrelationId` = the request correlation id or the saga id when there is none, `Outcome` = `Success` (started, step executed, completed, compensated), `Failure` (step failed, compensating), `Error` (failed, timed out), `TenantId` from the ambient context, `Metadata` = saga type, step index, previous and new status, duration since the previous transition.

</details>

<details>
<summary><strong>4. The audit event set</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One event per transition of the real status set: Started, StepExecuted, StepFailed, Compensating, Compensated, Completed, Failed, TimedOut** | Complete lifecycle, as the acceptance criterion asks | Eight events, not six |
| **B) The six events named in the original text** | Smaller | `Compensated` and `TimedOut` are real `SagaStatus` values (`SagaOrchestrator.cs`) and would stay unaudited |

### Chosen Option: **A — The full status set**

### Rationale

- **Class:** Settled
- **Settled by:** the issue's acceptance criterion "All saga state transitions audited (including `Compensated` and `TimedOut`)" and `SagaStatus` in `src/Encina.Messaging/Sagas/SagaOrchestrator.cs`.
- Emission points: `StartAsync` (Started), `AdvanceAsync` (StepExecuted), `StartCompensationAsync` (StepFailed and Compensating, two entries with distinct ids, the failing step index and the error code), `CompensateStepAsync` when no step remains (Compensated), `CompleteAsync` (Completed), `FailAsync` (Failed), `TimeoutAsync` (TimedOut). The runner (`SagaRunner`) does not emit: the orchestrator is the single place that persists transitions.

</details>

<details>
<summary><strong>5. Failure of the audit write</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A `Left` from the sink fails the saga operation visibly** | Nothing reports success when the audit failed | The caller sees an error although the state is saved (D2 A) |
| **B) Log and continue** | The saga is never affected by the audit | Swallows an error in background infrastructure |

### Chosen Option: **A — Return the `Left`**

### Rationale

- **Class:** Settled
- **Settled by:** `AGENTS.md` section 3 (errors are never swallowed in background infrastructure: a `Left` from a store inside an orchestrator fails that operation).
- With #1581 not yet merged the runner would drop this `Left`; hence the prerequisite above.

</details>

<details>
<summary><strong>6. What the entry records about errors</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The error code only; never `EncinaError.Message`, never the saga `Data` payload** | Complies; no personal data in the audit trail | Less detail for the investigator |
| **B) The message and a payload hash** | More detail | `EncinaError.Message` can carry personal data (#1259, #1274) |

### Chosen Option: **A — Error code only**

### Rationale

- **Class:** Settled
- **Settled by:** `AGENTS.md` section 3 (`EncinaError.Message` never reaches logs, activity tags, health-check results or plaintext storage).
- `RequestPayload`, `ResponsePayload` and `RequestPayloadHash` of the entry stay null.

</details>

<details>
<summary><strong>7. Entry identity and idempotency</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A deterministic `Guid` derived from saga id, event name and step index (SHA-256, first 16 bytes)** | A retried transition cannot write two entries for one transition (if the store honours the id) | Needs a stable derivation helper |
| **B) `Guid.NewGuid()` per call** | Trivial | A retry duplicates the entry |

### Chosen Option: **A — Deterministic id**

### Rationale

- **Class:** Trivial
- **Settled by:** the idempotency question of `AGENTS.md` section 6, function 9 (can receive duplicates: deduplication key); `OperationAuditEntry.Id` is a required `Guid`.
- Timestamps come from `TimeProvider` (`AGENTS.md` section 3).

</details>

<details>
<summary><strong>8. Opt-in switch</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `MessagingConfiguration.UseSagaAudit` (default false), like its siblings** | Same naming as `UseSagas`, `UseOperationAuditStore`, `UseReadAuditStore` | Differs from the issue's `AuditSagas` |
| **B) `AuditSagas` as written in the issue** | Matches the issue text | Breaks the `Use*` naming of every sibling |

### Chosen Option: **A — `UseSagaAudit`**

### Rationale

- **Class:** Trivial
- **Settled by:** the convention of `src/Encina.Messaging/MessagingConfiguration.cs` (`UseSagas` line 106, `UseOperationAuditStore` line 497, `UseReadAuditStore` line 532) and `AGENTS.md` section 3 (every messaging pattern optional, disabled by default; ADR-050).
- With the flag on and no sink registered, startup validation fails with a clear message (registration completeness, `AGENTS.md` section 3); the sink is registered by `AddEncinaAudit()`.

</details>

---

## Implementation Phases

### Phase 1: Core Models and the Sink Abstraction

> **Goal**: The contract in `Encina.Messaging`.

<details>
<summary>Tasks</summary>

1. `SagaTransitionEvent` (sealed record, `src/Encina.Messaging/Sagas/Auditing/`): `SagaId`, `SagaType`, `Kind` (enum `SagaTransitionKind`: Started, StepExecuted, StepFailed, Compensating, Compensated, Completed, Failed, TimedOut), `StepIndex`, `PreviousStatus`, `NewStatus`, `ErrorCode?`, `OccurredAtUtc`, `TenantId?`, `CorrelationId?`.
2. `ISagaAuditSink`: `ValueTask<Either<EncinaError, Unit>> RecordAsync(SagaTransitionEvent transition, CancellationToken cancellationToken = default)`.
3. `MessagingConfiguration.UseSagaAudit` (default false) with XML docs.
4. `PublicAPI.Unshipped.txt` of `Encina.Messaging`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```
CONTEXT:
- Issue #750; plan docs/plans/saga-audit-implementation-plan-750.md. Dilemmas D1-D2 are recommended, pending the maintainer: follow the Maintainer Decisions section if it exists.
- Encina.Messaging cannot reference Encina.Security.Audit (the reverse reference exists).

TASK:
Create the event record, the sink interface and the opt-in flag.

KEY RULES:
- Sealed records, XML docs, nullable enabled; the event carries the error code only.
- Timestamps use the AtUtc suffix; no DateTime.UtcNow.
- Update PublicAPI.Unshipped.txt; zero warnings.

REFERENCE FILES:
- src/Encina.Messaging/Sagas/SagaOrchestrator.cs (SagaStatus)
- src/Encina.Messaging/MessagingConfiguration.cs (UseSagas, line 106)
- src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs
```

</details>

---

### Phase 2: Orchestrator Emission

> **Goal**: Every transition is recorded after its state is saved.

<details>
<summary>Tasks</summary>

1. `SagaOrchestrator`: optional constructor parameter `ISagaAuditSink? auditSink`; after each successful `ISagaStore.UpdateAsync`/`AddAsync` call the sink (emission points of choice 4); a `Left` from the sink is returned (choice 5). With no sink nothing is emitted and nothing is allocated.
2. Stable entry ids are derived by the adapter (Phase 3) from the event, not by the orchestrator.
3. `[LoggerMessage]` entries (audit recorded at Debug, audit failed at Error with the error code) in the `MessagingSaga` range, ids 2250-2274, sequential.
4. `RegisterSagas` (`src/Encina.Messaging/MessagingServiceCollectionExtensions.cs`, line 355) does not register the sink; it is resolved optionally.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```
CONTEXT:
- Phase 1 created ISagaAuditSink and SagaTransitionEvent (issue #750).

TASK:
Emit the events from SagaOrchestrator at the points listed in Phase 2 Tasks.

KEY RULES:
- Emit only after the state was saved; return the sink's Left to the caller, never swallow it.
- Time from the existing TimeProvider; log the error code only, exceptions via ForLogging().
- EventIds 2250-2274 inside the registered MessagingSaga range; run EncinaEventIdAllocationTests.

REFERENCE FILES:
- src/Encina.Messaging/Sagas/SagaOrchestrator.cs
- src/Encina.Messaging/Diagnostics/SagaStoreLog.cs (2200-2205)
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

### Phase 3: Adapter in Encina.Security.Audit and DI

> **Goal**: `IOperationAuditStore` records the events; registration is complete.

<details>
<summary>Tasks</summary>

1. `SagaAuditSink` (internal, `src/Encina.Security.Audit/Sagas/`): maps `SagaTransitionEvent` to `OperationAuditEntry` (choice 3), derives the deterministic id (choice 7), calls `IOperationAuditStore.RecordAsync`.
2. `AddEncinaAudit()` registers `ISagaAuditSink` with `TryAddScoped` (`src/Encina.Security.Audit/ServiceCollectionExtensions.cs`, line 79).
3. Startup validation: `UseSagaAudit` with no `ISagaAuditSink` registered fails with a clear message.
4. DI test with `ValidateOnBuild` and `ValidateScopes`: with and without `AddEncinaAudit()`.
5. `PublicAPI.Unshipped.txt` and the Security.Audit manifest.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```
CONTEXT:
- Phases 1-2 are done (issue #750). The audit stores for the 10 providers already exist; this phase adds none.

TASK:
Implement the adapter and its registration.

KEY RULES:
- TryAdd registrations so the application's own sink wins; ValidateOnBuild and ValidateScopes DI test.
- RequestPayload, ResponsePayload and RequestPayloadHash stay null; Metadata holds saga type, step index, statuses only.
- The mapping of event kind to AuditOutcome is in choice 3 of the plan.

REFERENCE FILES:
- src/Encina.Security.Audit/ServiceCollectionExtensions.cs
- src/Encina.Security.Audit/OperationAuditEntry.cs
- src/Encina.Security.Audit/AuditOutcome.cs
```

</details>

---

### Phase 4: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. **OpenTelemetry**: a counter `encina.saga.audit_events` (`Counter<long>`, tags `saga.type`, `kind`, `outcome`) on the saga meter source; failures counted with `outcome=failed`.
2. **Structured logging**: Phase 2 messages.
3. **Idempotency**: the deterministic entry id (choice 7).
4. **Multi-tenancy**: the entry's `TenantId` comes from the ambient `IRequestContext.TenantId` (`IRequestContextAccessor`); when #738 lands the row's tenant is used.
5. **Deferred**: transactions (D2 B), resilience of the audit write (owned by the store), module scoping, health.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```
CONTEXT:
- Cross-cutting matrix of docs/plans/saga-audit-implementation-plan-750.md.

TASK:
Add the counter and the tenant propagation; leave the deferred items out.

KEY RULES:
- Counter tags are bounded (saga type, kind, outcome); the tenant is never a metric label.

REFERENCE FILES:
- src/Encina.Messaging/Diagnostics/MessagingStoreMetrics.cs
- src/Encina.Messaging/DeadLetter/DeadLetterManager.cs (AmbientTenantId)
```

</details>

---

### Phase 5: Observability

<details>
<summary>Tasks</summary>

1. The Phase 4 counter and an `Activity` tag `saga.audit.kind` on the existing `Encina.Messaging.Saga` source (`SagaActivitySource`).
2. The log messages of Phase 2 and the architecture test mapping (no new range, `Encina.Messaging` and `Encina.Security.Audit` already map to theirs).

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```
CONTEXT:
- Issue #750.

TASK:
Add the activity tag and confirm the architecture tests pass.

KEY RULES:
- HasListeners() guard; no payload or message in any tag.

REFERENCE FILES:
- src/Encina.Messaging/Diagnostics/SagaActivitySource.cs
- tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs
```

</details>

---

### Phase 6: Testing

<details>
<summary>Tasks</summary>

1. **Unit**: each event kind is emitted at its point; opt-out emits nothing; a sink `Left` is returned; mapping to `OperationAuditEntry` (outcome, metadata, no payload); deterministic id.
2. **Guard**: new public members with parameters.
3. **Contract**: `ISagaAuditSink` implemented by the adapter and a fake.
4. **Property (FsCheck)**: any lifecycle sequence produces events in order with consistent previous and new status.
5. **Integration**: the adapter against `OperationAuditStoreADO` on one real database through `[Collection("ADO-PostgreSQL")]` (the adapter has no provider-specific code, so one provider proves the mapping; the stores have their own integration tests).
6. **Load and Benchmark**: justification files `tests/Encina.LoadTests/Sagas/SagaAudit.md` and `tests/Encina.BenchmarkTests/Sagas/SagaAudit.md` (thin write path).
7. **Per-file targets** with a one-sentence justification in `.github/coverage-manifest/Encina.Messaging.json` and `Encina.Security.Audit.json`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```
CONTEXT:
- Phases 1-5 are implemented (issue #750).

TASK:
Write the tests of Phase 6 Tasks and fill the manifest targets.

KEY RULES:
- Shouldly via Encina.Testing.Shouldly; real implementations; shared [Collection] fixtures, ClearAllDataAsync in InitializeAsync.
- A test never swallows an exception.

REFERENCE FILES:
- tests/Encina.UnitTests/Messaging/Sagas/SagaOrchestratorTests.cs
- .github/coverage-manifest/Encina.Security.Audit.json
```

</details>

---

### Phase 7: Documentation & Finalization

<details>
<summary>Tasks</summary>

1. XML docs; `PublicAPI.Unshipped.txt`; `changelog.d/750-saga-audit-trail.added.md`; the saga guide (through docs-writer); an ADR for D1 (read `docs/architecture/adr/index.md` for the next free number); package README note for `Encina.Security.Audit`.
2. `dotnet build --configuration Release` with 0 warnings; `dotnet test` with every flag at its target.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```
CONTEXT:
- Issue #750 is implemented and tested.

TASK:
Write the documentation set.

KEY RULES:
- Pages go through the docs-writer agent; coverage figures only with covref markers.

REFERENCE FILES:
- changelog.d/README.md
```

</details>

---

### Proposed split into pull requests

| PR | Content | Approx. files | Approx. lines |
|----|---------|---------------|---------------|
| 1 | Phase 1 and 2: contract, flag, emission, logs, unit tests | 9 | 800 |
| 2 | Phase 3: adapter, DI, DI test | 6 | 450 |
| 3 | Phase 4 and 5: counter, activity tag, tests | 5 | 300 |
| 4 | Phase 6 remainder and 7: property, integration, justifications, docs | 10 | 900 |

---

## Research

### Relevant Standards & Specifications

| Reference | What it defines | Use in this feature |
|-----------|-----------------|---------------------|
| `AGENTS.md` section 6, function 12 | Audit abstractions | `IOperationAuditStore` |
| `AGENTS.md` section 3 | Errors never swallowed; error message never stored | Choices 5 and 6 |
| ADR-036 | Read audit | Out of scope; context only |
| ADR-050 | Module requirements | Opt-in flag and registration |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `SagaOrchestrator` and `SagaStatus` | `src/Encina.Messaging/Sagas/SagaOrchestrator.cs` | Emission points and status set |
| `IOperationAuditStore`, `OperationAuditEntry`, `AuditOutcome` | `src/Encina.Security.Audit/` | Recording |
| `AddEncinaAudit` | `src/Encina.Security.Audit/ServiceCollectionExtensions.cs` | Adapter registration |
| Operation audit stores | `src/Encina.ADO.*/Auditing/`, `Encina.Dapper.*/Auditing/`, `Encina.EntityFrameworkCore/Auditing/`, `Encina.MongoDB/Auditing/` | Existing persistence for the 10 providers |
| `MessagingConfiguration` | `src/Encina.Messaging/MessagingConfiguration.cs` | Opt-in flag |
| `SagaActivitySource` | `src/Encina.Messaging/Diagnostics/SagaActivitySource.cs` | Activity tag |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` (saga) | 2200-2299 | Existing `MessagingSaga`; `SagaStoreLog` uses 2200-2205 |
| **This plan (#750)** | **2250-2274** | **Audit recorded and failed; slice within the saga family: #720 2206-2229, #738 2230-2249, #750 2250-2274, #1237 2275-2299** |
| `Encina.Security.Audit` | 5000-5099 | Existing; the adapter may log inside it (first unused id after scanning `Log.cs`) |

### Provider Matrix

| Provider | Applies | Reason |
|----------|---------|--------|
| Database (10) | No new code | No saga table or store changes; the existing operation audit stores keep their own coverage |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Messaging | ~6 | Event, kind, sink, flag, orchestrator, logs |
| Security.Audit | ~3 | Adapter, registration, startup check |
| Tests | ~9 | Unit, guard, contract, property, integration, DI, two justifications |
| Documentation | ~4 | Changelog fragment, saga guide, ADR, README note |
| **Total** | **~22 across 4 pull requests** | |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no [Obsolete]; XML docs; zero warnings.
- Issue #750: opt-in audit of saga transitions through IOperationAuditStore. Encina.Security.Audit references Encina.Messaging, so Messaging owns a sink interface. Plan: docs/plans/saga-audit-implementation-plan-750.md; Dilemmas D1-D2 are recommended, pending the maintainer.

IMPLEMENTATION OVERVIEW:
1. Event record, sink interface, UseSagaAudit flag (Phase 1).
2. Emission from SagaOrchestrator after each saved transition (Phase 2).
3. Adapter and registration in Encina.Security.Audit (Phase 3).
4. Cross-cutting, observability, tests, documentation (Phases 4-7).

KEY PATTERNS:
- A Left from the sink is returned to the caller, never swallowed; error code only in entries and logs.
- Deterministic entry ids; TimeProvider for time; TryAdd registrations with a ValidateOnBuild DI test.
- EventIds 2250-2274 inside the registered MessagingSaga range.

REFERENCE FILES:
- src/Encina.Messaging/Sagas/SagaOrchestrator.cs
- src/Encina.Security.Audit/ServiceCollectionExtensions.cs
- src/Encina.Security.Audit/OperationAuditEntry.cs
- src/Encina.Messaging/MessagingConfiguration.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: write-only path |
| 2 | OpenTelemetry | ✅ | Phase 4 and 5: `encina.saga.audit_events` counter and activity tag |
| 3 | Structured Logging | ✅ | Phase 2: messages in 2250-2274, error code only |
| 4 | Health Checks | ❌ | Not applicable: the audit store has its own `OperationAuditStoreHealthCheck` |
| 5 | Validation | ❌ | Not applicable: no external input; startup validation of the flag and sink is registration completeness |
| 6 | Resilience | ❌ | Not applicable: the audit store implementation owns retries and the orchestrator does not retry; a failed write is returned as a `Left` |
| 7 | Distributed Locks | ❌ | Not applicable: no shared state is locked |
| 8 | Transactions | ⏭️ | Deferred: depends on D2 option B, when `IOperationAuditStore` gains transaction support (to be opened as an issue if the maintainer chooses B) |
| 9 | Idempotency | ✅ | Phase 3: deterministic entry id |
| 10 | Multi-Tenancy | ✅ | Phase 4: ambient tenant; the row's tenant once #738 lands |
| 11 | Module Isolation | ❌ | Not applicable: saga rows have no module scope (#747 is separate) |
| 12 | Audit Trail | ✅ | This is the feature |

---

## Prerequisites & Dependencies

- #1581 (SagaRunner discards the `Left` of the orchestrator's persisting calls) should land first; otherwise audit failures are dropped by the runner.
- #738 (tenant on the saga row) is advisable; the plan works with the ambient tenant until then.
- #720 and #1237 add transitions; their plans extend the event kinds through the same sink.

---

## Next Steps

1. The maintainer answers D1 and D2 when work on #750 starts; the answers go into `## Maintainer Decisions` and the markers are removed.
2. Brief pull request 1 (with or after #1581).
