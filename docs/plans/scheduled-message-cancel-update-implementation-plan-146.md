# Implementation Plan: Scheduled message cancellation and update by id

> **Issue**: [#146](https://github.com/dlrivada/Encina/issues/146)
> **Type**: Feature (rest of the issue after the 1.0 slice taken by #1222)
> **Complexity**: Medium (7 phases, all 10 database providers, one new store method, concurrency semantics)
> **Estimated Scope**: ~450 lines of production code + ~900 lines of tests, ~30 files in 3 sub-pull requests
> **Family**: scheduling-1 (builds on #1222; shares columns and the orchestrator surface with #133, #147 and #150)

---

## Summary

SPEC-002 DEC-005 (a) takes the schedule-key slice of this issue into 1.0 as #1222 (upsert, cancel and reschedule **by key**). This plan delivers what remains for messages **addressed by id**, checked against today's code:

- `IScheduledMessageStore.CancelAsync(Guid, ct)` and `SchedulerOrchestrator.CancelAsync(Guid, ct)` already exist and return `Either<EncinaError, Unit>`; the stores delete the row (`DELETE` in Dapper and ADO, `Remove` in `ScheduledMessageStoreEF.cs`, `DeleteOneInSessionAsync` in MongoDB). Cancel needs no new method; this plan fixes its documented semantics under concurrency.
- Missing: an id-based **update of the scheduled time** (`UpdateScheduledAtAsync`) and the orchestrator method that uses it (`RescheduleAsync`). `RescheduleRecurringMessageAsync(messageId, nextScheduledAtUtc, ct)` exists but serves recurring runs only and throws on a past time.
- Missing: defined behavior when cancel or update races the processor, and the decision on what cancel leaves behind.

The proposal of the issue (`Task<bool>` methods, an `ISchedulingOrchestrator` and extension methods) is reconciled with the real shapes: the orchestrator is the sealed class `SchedulerOrchestrator`, `ScheduleAsync` returns `Either<EncinaError, Guid>`, and no `bool` result is introduced (AGENTS.md section 3, ROP).

**Provider category**: Database (10): ADO.NET x3, Dapper x3, EF Core x3 (SqlServer, PostgreSQL, MySQL), MongoDB.

**Affected packages**: `Encina.Messaging`, the nine SQL provider packages through their store classes, `Encina.EntityFrameworkCore`, `Encina.MongoDB`, `Encina.OpenTelemetry` (decorator), `Encina.Testing.Fakes`.

**Split**: implementations first, interface last (as in #1222, so every pull request compiles). S1 ADO x3 and Dapper x3 stores with integration tests (~12 files); S2 EF Core and MongoDB (~6 files); S3 core: interface member, orchestrator methods, error code, log events, fake and decorator, docs (~9 files).

**Coordination (family scheduling-1)**: #1222 owns `ScheduledMessageDraft`, the key methods, the counters `encina.scheduling.rescheduled` and `encina.scheduling.cancelled`, and EventIds 2326-2339. This plan reuses the counters with an `operation` tag (`update`, `cancel`) and takes EventIds 2340-2349.

---

## Design Choices

<details>
<summary><strong>1. What cancel leaves behind</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Delete the row, as the stores do today** | No clutter; due queries unchanged; consistent with cancel-by-key freeing the key in #1222 | No record in the table that the message was cancelled |
| **B) Keep the row with a cancelled state and timestamp** | History and diagnostics; proof of cancellation for regulated flows | A state column on 10 providers, every due query filters it, retention to define, and a cancelled row blocks the schedule key |

### Chosen Option: **A — delete the row** (recommended, pending the maintainer)

### Rationale

- Option A wins when nothing needs the history. Option B wins for regulated or user-facing flows that must prove a cancellation.
- Proof of cancellation belongs to the audit trail (cancel and reschedule as audit events, tracked with #1778), which keeps the scheduling table small and avoids a state column on every provider.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Cancel or update racing the processor</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One conditional statement; the affected-row count decides** (`UPDATE ... WHERE Id = @id AND ProcessedAtUtc IS NULL`) | Atomic, no new column; the loser gets a defined result (`scheduling.message_not_pending`) | EF Core and MongoDB need the equivalent atomic form |
| **B) Optimistic locking with a row version** | Explicit conflict result | A new column on all providers and a retry path for callers |

### Chosen Option: **A — conditional statement** (recommended, pending the maintainer)

### Rationale

- Option A wins for simplicity; option B wins if callers must know that they lost a race and retry with fresh state.
- Semantics under A: an update of a processed or missing message returns `Left(scheduling.message_not_pending)`. A cancel that arrives while the processor already holds the row in memory cannot stop that dispatch: the in-flight run completes, and its outcome update then matches no row (Dapper `UPDATE`, MongoDB not-found log), so the message does not run again and a recurring one ends.
- EF Core uses `ExecuteUpdateAsync` with the same `Where(ProcessedAtUtc == null)` filter; MongoDB uses `UpdateOneAsync` with the `ProcessedAtUtc` null filter inside the session.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Scope of reschedule: time only or time and payload</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Time only** (`UpdateScheduledAtAsync`) | Smaller; no serialization or request-type checks; a keyed schedule already replaces its payload through the upsert of #1222 | A caller without a key cancels and schedules again to change a payload |
| **B) Time and optional payload** (`RescheduleAsync(id, time, newContent)`) | Complete | The new content must be serialized through `IMessageSerializer`, the request type must match, and validation is added on 10 providers |

### Chosen Option: **A — time only** (recommended, pending the maintainer)

### Rationale

- Option A wins for the 1.0 scope (P-20 asks for time moves); option B wins post-1.0 if unkeyed payload edits become a requirement.
- Under A the store has one new method, `UpdateScheduledAtAsync(Guid messageId, DateTime newScheduledAtUtc, CancellationToken)`, and the orchestrator gets `RescheduleAsync(Guid, DateTimeOffset, ct)` plus a `DateTime` overload.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Result type and idempotency of cancel</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep `Either<EncinaError, Unit>`; cancel of a missing or processed message stays `Right(Unit)`** | Matches the existing contract and the key-based cancel of #1222; safe to repeat | The caller cannot tell that nothing was cancelled |
| **B) Return `Either<EncinaError, bool>` or a Left when nothing was cancelled** | The caller learns the outcome | Changes the contract of ten stores for an outcome the audit trail records |

### Chosen Option: **A — keep the idempotent Unit result**

### Rationale

- Operations return `Either<EncinaError, T>` and never `bool` as in the issue text (AGENTS.md section 3, ADR-001, ADR-006); the XML doc of `CancelAsync` is extended to state that repeating it is safe and that an in-flight dispatch is not interrupted.
- `UpdateScheduledAtAsync` differs on purpose: moving a time that cannot be moved is an error the caller must see.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (Either, no bool), ADR-001, ADR-006, the existing `IScheduledMessageStore.CancelAsync` contract

</details>

<details>
<summary><strong>5. Past times and validation placement</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The orchestrator returns `scheduling.invalid_schedule_time` for a past time; the store keeps the `ArgumentException` convention for programming errors** | Same split as `ScheduleAsync` and `RescheduleRecurringMessageAsync` today | Two places check the time |
| **B) Only the store validates** | One place | The business error becomes an exception |

### Chosen Option: **A — orchestrator validates, store guards**

### Rationale

- `ScheduleAsync` already returns `InvalidScheduleTime` and the stores throw with `StoreValidationMessages.NextScheduledDateCannotBeInPast`; the new method follows both.
- Time comes from `TimeProvider` (AGENTS.md section 3).

**Class:** Trivial
**Settled by:** AGENTS.md section 3 (time from `TimeProvider`, no exceptions for business logic), existing convention in `ScheduledMessageStoreDapper.RescheduleRecurringMessageAsync`

</details>

---

## Implementation Phases

### Phase 1: Core contracts (S3 for the interface, written first)

> **Goal**: The error code, the store member and the orchestrator surface.

<details>
<summary><strong>Tasks</strong></summary>

1. `SchedulingErrorCodes.MessageNotPending = "scheduling.message_not_pending"` (in `src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs`).
2. `IScheduledMessageStore.UpdateScheduledAtAsync(Guid messageId, DateTime newScheduledAtUtc, CancellationToken)` returning `Task<Either<EncinaError, Unit>>`; it resets `ErrorMessage`, `RetryCount` and `NextRetryAtUtc` so the message is due at the new time.
3. `SchedulerOrchestrator.RescheduleAsync(Guid messageId, DateTimeOffset newExecuteAt, CancellationToken)` and a `DateTime` overload; the orchestrator returns `scheduling.invalid_schedule_time` for a past time, calls the store, logs and counts.
4. XML docs of `CancelAsync` (store and orchestrator): idempotent, deletes the row, does not interrupt a dispatch already in flight.
5. `PublicAPI.Unshipped.txt` lines (a `mechanical-fixer` writes them).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
- Issue #146, plan scheduled-message-cancel-update. IScheduledMessageStore already has CancelAsync(Guid, ct) returning Either<EncinaError, Unit>. The orchestrator is the sealed class SchedulerOrchestrator.
- #1222 (schedule key) lands first; reuse its counters and ScheduledMessageDraft.

TASK:
Add MessageNotPending, UpdateScheduledAtAsync and RescheduleAsync as described, and extend the XML docs of CancelAsync. Add the interface member only in the sub-issue where all providers already implement it.

KEY RULES:
- Either<EncinaError, T>; no bool results; time from TimeProvider.
- Log the message id and error code only; never the payload.

REFERENCE FILES:
- src/Encina.Messaging/Scheduling/IScheduledMessageStore.cs
- src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs
```

</details>

### Phase 2: ADO.NET providers x3 (S1)

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.ADO.<Db>/Scheduling/ScheduledMessageStoreADO.cs` (SqlServer, PostgreSQL, MySQL): public `UpdateScheduledAtAsync` — `UPDATE ... SET ScheduledAtUtc = @NewScheduledAtUtc, ErrorMessage = NULL, RetryCount = 0, NextRetryAtUtc = NULL WHERE Id = @MessageId AND ProcessedAtUtc IS NULL`; `ExecuteNonQueryAsync` returning 0 maps to `Left(scheduling.message_not_pending)`.
2. `OpenAsync` and `ExecuteNonQueryAsync` with the `CancellationToken`, joined to the scope transaction like the neighbouring methods.
3. Integration tests in `tests/Encina.IntegrationTests/ADO/<Db>/Scheduling/ScheduledMessageStoreADOTests.cs`: moved time is due at the new time; a processed message returns the Left; a repeated cancel stays `Right`; a concurrent update and `MarkAsProcessedAsync` end with exactly one winner.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
- Existing ADO stores have the pattern of RescheduleRecurringMessageAsync; SQL differs only in identifiers and booleans (SQL Server bit, PostgreSQL lower-case identifiers, MySQL 0/1).

TASK:
Implement UpdateScheduledAtAsync as a public member of each concrete ADO store with the conditional UPDATE and the affected-row check.

KEY RULES:
- Async with CancellationToken; never Open().
- Integration tests use [Collection("ADO-SqlServer")] and siblings, ClearAllDataAsync in InitializeAsync.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/Scheduling/ScheduledMessageStoreADO.cs
- tests/Encina.IntegrationTests/ADO/SqlServer/Scheduling/ScheduledMessageStoreADOTests.cs
```

</details>

### Phase 3: Dapper providers x3 (S1)

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Dapper.<Db>/Scheduling/ScheduledMessageStoreDapper.cs`: `UpdateScheduledAtAsync` inside `EitherHelpers.TryAsync` with `DbLease.Enlisted`; `ExecuteAsync` returns the affected rows; 0 returns the Left (error code of a failed statement stays `scheduling.update_scheduled_at_failed`).
2. Integration tests in `tests/Encina.IntegrationTests/Dapper/<Db>/Scheduling/` with the same four cases as phase 2.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
- Dapper stores take IDbConnection, a table name validated by SqlIdentifierValidator, TimeProvider and an optional IDbTransactionAccessor.

TASK:
Add UpdateScheduledAtAsync to the three Dapper stores.

KEY RULES:
- A statement failure returns a Left with the store error code; "no pending row" returns scheduling.message_not_pending.
- Collections Dapper-SqlServer, Dapper-PostgreSQL, Dapper-MySQL.

REFERENCE FILES:
- src/Encina.Dapper.SqlServer/Scheduling/ScheduledMessageStoreDapper.cs
- tests/Encina.IntegrationTests/Dapper/PostgreSQL/Scheduling/ScheduledMessageStoreDapperTests.cs
```

</details>

### Phase 4: EF Core and MongoDB (S2)

<details>
<summary><strong>Tasks</strong></summary>

1. `ScheduledMessageStoreEF.cs`: `UpdateScheduledAtAsync` with `ExecuteUpdateAsync` (atomic, filter `ProcessedAtUtc == null`), result 0 returns the Left; works for the SqlServer, PostgreSQL and MySQL providers through the one store. Because `ExecuteUpdateAsync` bypasses the tracker, document that a tracked instance of the same row in the same context is stale; the orchestrator does not load the row first.
2. `ScheduledMessageStoreMongoDB.cs`: `UpdateOneAsync` in the session with the null-`processedAtUtc` filter and `$set`/`$unset` of the retry fields; `MatchedCount == 0` returns the Left.
3. Shared integration bodies in `tests/Encina.TestInfrastructure/EFCore/ScheduledMessageStoreEFTestsBase.cs` and `tests/Encina.IntegrationTests/Infrastructure/MongoDB/Stores/ScheduledMessageStoreMongoDBIntegrationTests.cs`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
- EF Core: one store serves three providers. MongoDB: commands run through the session helpers (FindInSession, DeleteOneInSessionAsync).

TASK:
Implement UpdateScheduledAtAsync on both stores with an atomic conditional write.

KEY RULES:
- Never read-then-write for the race case.
- Reuse the integration test bodies for the three EF providers.

REFERENCE FILES:
- src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessageStoreEF.cs
- src/Encina.MongoDB/Scheduling/ScheduledMessageStoreMongoDB.cs
```

</details>

### Phase 5: Cross-cutting integration and observability (S3)

<details>
<summary><strong>Tasks</strong></summary>

1. `SchedulingScheduleLog` (created by #1222): events 2340-2342 inside `EventIdRanges.MessagingScheduling`: `MessageRescheduled`, `RescheduleRejectedNotPending`, `CancelRequested`. Arguments: message id and error code.
2. Counters from #1222: `encina.scheduling.rescheduled` and `encina.scheduling.cancelled` gain the tag `operation`; spans `encina.scheduling.reschedule` and `encina.scheduling.cancel` carry the message id.
3. `InstrumentedScheduledMessageStore` and `FakeScheduledMessageStore` implement `UpdateScheduledAtAsync` (the fake applies the same pending rule).
4. Test that logs, tags and metrics contain no payload or key.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
- EventIds 2340-2349 are reserved for #146 inside MessagingScheduling (2300-2399); #1222 owns 2326-2339.

TASK:
Add the log events, extend the shared counters and spans, and update the decorator and the fake.

KEY RULES:
- EncinaError.Message never reaches logs or tags; use the error code.
- Pack EventIds sequentially from 2340.

REFERENCE FILES:
- src/Encina.Messaging/Diagnostics/SchedulingStoreLog.cs
- src/Encina.OpenTelemetry/MessagingStores/InstrumentedScheduledMessageStore.cs
- src/Encina.Testing.Fakes/Stores/FakeScheduledMessageStore.cs
```

</details>

### Phase 6: Testing

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit**: `SchedulerOrchestratorTests` cases for `RescheduleAsync` (past time, store Left propagates, success logs and counts) with `FakeTimeProvider`; fake store pending rule.
2. **Guard**: `SchedulerOrchestratorGuardTests` and the store guard classes for the nine SQL providers and EF Core (`Guid.Empty` throws `ArgumentException` as the neighbouring methods do).
3. **Contract**: the pending-only rule of `UpdateScheduledAtAsync` and the idempotent cancel run against `FakeScheduledMessageStore`; the 10 providers are covered by the integration bodies.
4. **Property**: justified as not required (a single conditional update; ordering is covered by integration); recorded in the PR.
5. **Integration**: update, race against `MarkAsProcessedAsync` and repeated cancel on all 10 providers (real databases, shared collections).
6. **Load and benchmark**: `.md` justifications under `tests/Encina.LoadTests/Messaging/Scheduling/` and `tests/Encina.BenchmarkTests/Encina.Benchmarks/Messaging/Scheduling/`.
7. **Obligations**: each touched store file keeps its manifest targets and adds integration coverage of the new method; `Scheduling/SchedulerOrchestrator.cs` keeps unit 90 and guard 30 (targets never go down). Measure per flag with `dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage" --results-directory artifacts\coverage\<Flag>Tests` and `dotnet run --file .github/scripts/coverage-report.cs -- --input artifacts/coverage --output artifacts/coverage-report`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
- Tests execute real package code; Shouldly via Encina.Testing.Shouldly; FakeTimeProvider; shared [Collection] fixtures for databases.

TASK:
Write the tests listed in the plan for the sub-issue you are in.

KEY RULES:
- No Thread.Sleep; the race test uses two awaited operations ordered by the test, then asserts the single winner.
- Integration tests are never replaced by a .md justification.

REFERENCE FILES:
- tests/Encina.UnitTests/Messaging/Scheduling/SchedulerOrchestratorTests.cs
- tests/Encina.TestInfrastructure/EFCore/ScheduledMessageStoreEFTestsBase.cs
```

</details>

### Phase 7: Documentation and finalization

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs with an example on `RescheduleAsync`.
2. `changelog.d/146-scheduled-message-reschedule.added.md` (a `mechanical-fixer` writes it).
3. `docs/features/scheduling.md`: cancel semantics, reschedule, the race outcome (through the `docs-writer`).
4. `PublicAPI.Unshipped.txt`; `docs/INVENTORY.md` unchanged (no new files beyond tests).
5. `dotnet build --configuration Release` with 0 warnings; all flags at target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
CONTEXT:
- Documentation pages go to the docs-writer; changelog and PublicAPI lines to the mechanical-fixer.

TASK:
Finish XML docs and delegate the rest with the exact decided text.

KEY RULES:
- Never edit CHANGELOG.md.

REFERENCE FILES:
- docs/features/scheduling.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Requirement | Key Details |
|----------|-------------|-------------|
| SPEC-002 REQ-033, AC-033 | Cancel and reschedule by key | Delivered by #1222; this plan covers the id-based remainder |
| ADR-001, ADR-006 | Railway Oriented Programming | `Either<EncinaError, T>`, no `bool` |
| ADR-018 | Cross-cutting integration | Matrix below |
| ADR-021 | EventIds | Ids 2340-2349 inside `MessagingScheduling` |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `IScheduledMessageStore.CancelAsync`, `RescheduleRecurringMessageAsync` | `src/Encina.Messaging/Scheduling/IScheduledMessageStore.cs` | Existing cancel; pattern for the new update |
| `SchedulerOrchestrator.CancelAsync` | `src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs` | Gains `RescheduleAsync` next to it |
| `StoreValidationMessages` | provider packages | Argument-check messages |
| `SchedulingScheduleLog`, shared counters | created by #1222 | Reused |
| `ScheduledMessageStoreEFTestsBase` | `tests/Encina.TestInfrastructure/EFCore/` | Shared EF integration bodies |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` (`MessagingScheduling`, existing 2300-2399) | **2340-2349** for #146 | Used: 2300-2306, 2320-2325; reserved: #1222 2326-2339, #150 2350-2359, #133 2360-2379, #147 2380-2389 |
| New range | none | The block 6750-6799 is not needed |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Core `Encina.Messaging` | ~3 | store interface, orchestrator, log |
| SQL provider stores (ADO x3, Dapper x3) | 6 | one method each |
| EF Core and MongoDB stores | 2 | |
| Decorator and fake | 2 | |
| Tests | ~14 | unit, guard, contract, integration |
| Documentation and changelog | ~3 | |
| **Total** | **~30** | in 3 pull requests |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no compatibility layers; time from TimeProvider.
- Issue #146: id-based reschedule and cancel semantics. #1222 (schedule key) lands first and owns ScheduledMessageDraft, the counters and EventIds 2326-2339.

IMPLEMENTATION OVERVIEW:
S1: UpdateScheduledAtAsync as a concrete member of the ADO and Dapper stores, with integration tests.
S2: the same on EF Core (ExecuteUpdateAsync) and MongoDB (UpdateOneAsync), shared test bodies.
S3: interface member, MessageNotPending, SchedulerOrchestrator.RescheduleAsync, log events 2340-2342, decorator, fake, docs.
Apply the Maintainer Decisions for cancel residue, race handling and reschedule scope.

KEY PATTERNS:
- One conditional statement decides the race; zero affected rows returns scheduling.message_not_pending.
- Cancel stays idempotent and does not interrupt an in-flight dispatch.
- Implementations first, interface last.

REFERENCE FILES:
- src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs
- src/Encina.Dapper.SqlServer/Scheduling/ScheduledMessageStoreDapper.cs
- src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessageStoreEF.cs
- src/Encina.MongoDB/Scheduling/ScheduledMessageStoreMongoDB.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Writes to scheduled rows; nothing cacheable |
| 2 | OpenTelemetry | ✅ | Phase 5: shared counters with an `operation` tag and reschedule and cancel spans |
| 3 | Structured Logging | ✅ | Phase 5: events 2340-2342 inside the registered range |
| 4 | Health Checks | ❌ | No checkable dependency is added |
| 5 | Validation | ✅ | Phase 1: empty id and past time are rejected; a non-pending message returns `scheduling.message_not_pending` |
| 6 | Resilience | ❌ | No external call |
| 7 | Distributed Locks | ⏭️ | The race is resolved by the conditional statement; multi-instance claiming is #1251 |
| 8 | Transactions | ✅ | Phases 2 to 4: the update joins the scope transaction like the existing methods |
| 9 | Idempotency | ✅ | Cancel is idempotent; a repeated reschedule to the same time is a stable no-op result |
| 10 | Multi-Tenancy | ⏭️ | A tenant must not move another tenant's message; the filter is added with the tenant column of #739 |
| 11 | Module Isolation | ❌ | SPEC-002 asks no module scoping; `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ⏭️ | Cancel and reschedule as audit events depend on the audit stores (#1778); proof of cancellation is why option B of choice 1 was not chosen |

---

## Prerequisites & Dependencies

| Item | Relation | Note |
|------|----------|------|
| #1222 | Must merge first | Draft record, key methods, counters, log class, EventIds 2326-2339 |
| #739 | Advisable first | Tenant filter on id-based operations |
| #1251 | Coordinate | Claiming changes the completion methods; `UpdateScheduledAtAsync` does not take a claim token |
| #133 | Sibling | Pause and resume of recurring messages build on the same pending rule |

---

## Next Steps

1. Review this plan; the three Dilemmas (cancel residue, race handling, reschedule scope) are answered by the maintainer when work starts and recorded in `## Maintainer Decisions`.
2. Decompose into S1 to S3 after #1222 S4 merges.
3. Link the plan from #146.
