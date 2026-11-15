# Implementation Plan: Scheduled message cancellation and update by id

> **Issue**: [#146](https://github.com/dlrivada/Encina/issues/146)
> **Type**: Feature (rest of the issue after the 1.0 slice taken by #1222)
> **Complexity**: Medium (7 phases, all 10 database providers, one new store method, concurrency semantics)
> **Estimated Scope**: ~450 lines of production code + ~1,400 lines of tests and manifests; 71 file edits in 7 pull requests that each compile on their own, none over 500 changed lines
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

Facts verified on `origin/main` (2026-11-13): `IScheduledMessageStore` has 10 implementers (8 provider stores, `InstrumentedScheduledMessageStore`, `FakeScheduledMessageStore`) and no test class implements it; `SchedulingErrorCodes` is declared in `SchedulerOrchestrator.cs`; `ScheduledMessageStoreEF` defers writes through the tracker and `SaveChangesAsync`; `EventIdRanges.MessagingScheduling` is 2300-2399; #1222, #739, #747, #1251, #1778, #133, #150 and #147 are open. ADR 062 is merged (PR #3599): `scheduling.message_not_pending` is created with `EncinaErrors.Create(code, ErrorCategory.Conflict, message)` (invalid transition).

**Split**: additive first, interface last, so every pull request compiles on its own (the counts include tests, guard classes, manifests, PublicAPI files and the existing callers of a changed signature; no schema changes here):

| PR | Content | Files | Lines (est.) |
| --- | --- | ---: | ---: |
| S0 | `SchedulingErrorCodes.MessageNotPending` and the `CancelAsync` XML docs | 4 | 60 |
| S1 | ADO x3: concrete `UpdateScheduledAtAsync`, integration and guard tests | 15 | 400 |
| S2 | Dapper x3: same | 15 | 400 |
| S3 | EF Core and MongoDB: same | 10 | 300 |
| S4 | Interface member, `InstrumentedScheduledMessageStore`, `FakeScheduledMessageStore` | 12 | 300 |
| S5 | `RescheduleAsync`, log class, metric tag, spans | 11 | 450 |
| S6 | Documentation, changelog, load and benchmark justifications | 4 | 100 |
| **Total** | | **71** | **about 1,700** |

S0 4 (`SchedulerOrchestrator.cs`, `IScheduledMessageStore.cs` XML docs, `PublicAPI.Unshipped.txt`, manifest); S1 and S2 4 per provider (store, PublicAPI, manifest, integration test) plus 3 guard classes = 15; S3 EF 4 + Mongo 4 + 2 guard classes = 10; S4 3 src + 5 tests + 2 PublicAPI + 2 manifests = 12; S5 4 src + 5 tests + PublicAPI + manifest = 11; S6 = 4. The header, this table and the Estimated File Count table all state 71.

**Coordination (family scheduling-1)**: #1222 owns the key methods, `SchedulingScheduleMetrics` (counters `encina.scheduling.rescheduled` and `encina.scheduling.cancelled`), the span helpers and EventIds 2326-2339; this plan reuses the counters and spans with an `operation` tag (`update`, `cancel`) and creates its own log class `SchedulingRescheduleLog` with EventIds 2340-2342 (inside the 2326-2389 span the batch registry holds for #1222, #146, #150, #133 and #147). `SchedulingErrorCodes.MessageNotPending` is created by this plan in S0 (the first pull request) and used by #133 for pause and resume. #1222 P12 merges before S5.

---

## Design Choices

<details>
<summary><strong>1. What cancel leaves behind (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Delete the row, as the stores do today** | No clutter; due queries unchanged; consistent with cancel-by-key freeing the key in #1222 | No record in the table that the message was cancelled |
| **B) Keep the row with a cancelled state and timestamp** | History and diagnostics; proof of cancellation for regulated flows | A state column on 10 providers, every due query filters it, retention to define, and a cancelled row blocks the schedule key |

### Chosen Option: **A — delete the row** (recommended, pending the maintainer)

### Rationale

- Option A wins when nothing needs the history. Option B wins for regulated or user-facing flows that must prove a cancellation.
- Proof of cancellation belongs to the audit trail (cancel and reschedule as operation-audit events), which keeps the scheduling table small and avoids a state column on every provider.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Cancel or update racing the processor (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One conditional statement; the affected-row count decides** (`UPDATE ... WHERE Id = @id AND ProcessedAtUtc IS NULL`) | Atomic, no new column; the loser gets a defined result (`scheduling.message_not_pending`) | EF Core and MongoDB need the equivalent atomic form; does not cover an update while the processor holds the row (choice 3) |
| **B) Optimistic locking with a row version** | Explicit conflict result | A new column on all providers and a retry path for callers |

### Chosen Option: **A — conditional statement** (recommended, pending the maintainer)

### Rationale

- Option A wins for simplicity; option B wins if callers must know that they lost a race and retry with fresh state.
- Semantics under A: an update of a processed or missing message returns `Left(scheduling.message_not_pending)`. A cancel that arrives while the processor already holds the row in memory cannot stop that dispatch: the in-flight run completes, and its outcome update then matches no row (Dapper `UPDATE`, MongoDB not-found log), so the message does not run again and a recurring one ends.
- EF Core uses `ExecuteUpdateAsync` with the same `Where(ProcessedAtUtc == null)` filter; MongoDB uses `UpdateOneAsync` with the `ProcessedAtUtc` null filter inside the session.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. An update while the processor is dispatching the row (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Accept and document until row claiming (#1251): `ProcessedAtUtc IS NULL` is still true during dispatch, so the update returns `Right`, the dispatch runs at the old time, and `MarkAsProcessedAsync` then ends the message: the reschedule is lost silently** | No new state; #1251 closes the window by claiming rows | A caller can believe a moved message will run again |
| **B) Require a claim marker now** | The update can return `Left` while claimed | A claim column on 10 providers is what #1251 builds; duplicating it here |

### Chosen Option: **A — document the window, close it with #1251** (recommended, pending the maintainer)

### Rationale

- Option A wins for 1.0 scope; option B wins only if #1251 is dropped. The integration test of S1 to S3 includes the interleaving "due fetched, update applied, `MarkAsProcessedAsync`" and asserts that the message ends processed with the update lost, so the documented behavior is pinned. The same applies to the key-based reschedule of #1222.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Scope of reschedule: time only or time and payload (Dilemma)</strong></summary>

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
<summary><strong>5. Result type and idempotency of cancel (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep `Either<EncinaError, Unit>`; cancel of a missing or processed message stays `Right(Unit)`** | Matches the existing contract and the key-based cancel of #1222; safe to repeat | The caller cannot tell that nothing was cancelled; asymmetric with update, which returns `Left` for a message that cannot be moved |
| **B) `Either<EncinaError, bool>`, or a `Left` when nothing was cancelled** | The caller learns the outcome | Changes the contract of ten stores for an outcome the audit trail records; a `bool` inside an `Either` is not the issue's `bool` result but is still an outcome flag |

### Chosen Option: **A — keep the idempotent Unit result** (recommended, pending the maintainer)

### Rationale

- Both options are `Either` and satisfy ADR-001, ADR-006 and AGENTS.md section 3 (no exceptions for business logic), so the choice is open. Option A wins when callers retry cancels freely; option B wins when a caller must distinguish "cancelled" from "nothing to cancel".
- The XML doc of `CancelAsync` is extended to state that repeating it is safe and that an in-flight dispatch is not interrupted. The answer also applies to `CancelByKeyAsync` of #1222.
- `UpdateScheduledAtAsync` differs on purpose: moving a time that cannot be moved is an error the caller must see.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. What an update resets and what it does to a recurring message (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `UpdateScheduledAtAsync` sets `ScheduledAtUtc` and clears `ErrorMessage`, `RetryCount` and `NextRetryAtUtc`, so the message is due at the new time; on a recurring message only the next run moves, and later runs are computed from the cron expression after that run (the existing recurrence path)** | The moved message is due exactly at the new time | A failed message loses its retry history |
| **B) Keep the retry state; the message is due at the later of the new time and its `NextRetryAtUtc`** | History preserved | The new time can be ignored, which contradicts the caller's intent |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

- Option A wins for predictability; option B wins when retry counts must survive a reschedule for diagnostics (they are also kept by the audit trail).

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Past times, empty ids and validation placement</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The orchestrator returns `scheduling.invalid_schedule_time` for a time before `TimeProvider.GetUtcNow()` (no skew tolerance; the same rule as `ScheduleAsync`); the store does not check the time and only guards `Guid.Empty` with `ArgumentException` as the neighbouring methods do (a programming error)** | Business errors are `Left`, one check; no exception on a rule path | The store accepts a past time if called directly (the message is then simply due) |
| **B) Only the store validates, throwing for a past time** | One place | The business error becomes an exception |

### Chosen Option: **A — orchestrator validates the time, store guards the id**

### Rationale

- Time comes from `TimeProvider` and business errors are `Left` (AGENTS.md section 3); the existing `RescheduleRecurringMessageAsync` throws for a past time, and this plan does not copy that. A repeated `RescheduleAsync` to the same time is a stable `Right` (the statement rewrites the same value). A `DateTimeOffset` converts with `UtcDateTime`, as in #1222.

**Class:** Trivial
**Settled by:** AGENTS.md section 3 (time from `TimeProvider`, no exceptions for business logic), AGENTS.md sections 3 and 4 (UTC with the `AtUtc` suffix)

</details>

<details>
<summary><strong>8. Tenant isolation of id-based operations before #739 (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) #739 (`TenantId` on `ScheduledMessage`) is a hard prerequisite of S5, the pull request that exposes `RescheduleAsync`; S0 to S4 only add store members that nothing calls** | Fails closed: no public API can move another tenant's message | S5 waits for #739 |
| **B) Ship S5 first and state that the API is not for multi-tenant hosts until #739** | No wait | Until #739, a caller who knows an id can move another tenant's message (the existing `CancelAsync` has the same gap today) |

### Chosen Option: **A — #739 before S5** (recommended, pending the maintainer)

### Rationale

- Gates fail closed (AGENTS.md section 3). The same question is answered for the key methods in #1222.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Error code and cancel documentation (S0)

> **Goal**: The symbols the later pull requests return.

<details>
<summary><strong>Tasks</strong></summary>

1. `SchedulingErrorCodes.MessageNotPending = "scheduling.message_not_pending"` in `src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs`, created with `EncinaErrors.Create(code, ErrorCategory.Conflict, ...)` where it is produced (ADR 062).
2. XML docs of `CancelAsync` (`IScheduledMessageStore.cs` and the orchestrator): idempotent, deletes the row, does not interrupt a dispatch already in flight.
3. `PublicAPI.Unshipped.txt` and the manifest entry of `Encina.Messaging` through `mechanical-fixer`.

Count: 4 (the constant is a field with no executable lines; no test is added in this pull request, its first use is tested in S1).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
- Issue #146, plan scheduled-message-cancel-update. IScheduledMessageStore already has CancelAsync(Guid, ct) returning Either<EncinaError, Unit>. SchedulingErrorCodes is declared in src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs.

TASK:
Add MessageNotPending and the cancel XML docs. Do not touch IScheduledMessageStore members.

KEY RULES:
- Either<EncinaError, T>; no bool results; error codes created with an ErrorCategory (ADR 062).

REFERENCE FILES:
- src/Encina.Messaging/Scheduling/IScheduledMessageStore.cs
- src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs
```

</details>

### Phase 2: ADO.NET providers x3 (S1)

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.ADO.<Db>/Scheduling/ScheduledMessageStoreADO.cs` (SqlServer, PostgreSQL, MySQL): public `UpdateScheduledAtAsync` — `UPDATE ... SET ScheduledAtUtc = @NewScheduledAtUtc, ErrorMessage = NULL, RetryCount = 0, NextRetryAtUtc = NULL WHERE Id = @MessageId AND ProcessedAtUtc IS NULL`; `ExecuteNonQueryAsync` returning 0 maps to `Left(scheduling.message_not_pending)`; `Guid.Empty` throws `ArgumentException`.
2. `OpenAsync` and `ExecuteNonQueryAsync` with the `CancellationToken`, joined to the scope transaction like the neighbouring methods.
3. Tests in the same PR: integration tests in `tests/Encina.IntegrationTests/ADO/<Db>/Scheduling/ScheduledMessageStoreADOTests.cs`: moved time is due at the new time; a processed message returns the Left; a repeated cancel stays `Right`; a concurrent update and `MarkAsProcessedAsync` end with exactly one winner; the interleaving of choice 3 (due fetched, update applied, `MarkAsProcessedAsync`) ends processed with the update lost. The existing guard class of each store gets the `Guid.Empty` case.
4. `PublicAPI.Unshipped.txt` and manifest entry per package through `mechanical-fixer`.

Count: 3 stores + 3 integration tests + 3 guard classes + 3 PublicAPI + 3 manifests = 15.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
- Existing ADO stores have the pattern of RescheduleRecurringMessageAsync; SQL differs only in identifiers and booleans (SQL Server bit, PostgreSQL lower-case identifiers, MySQL 0/1).

TASK:
Implement UpdateScheduledAtAsync as a public member of each concrete ADO store with the conditional UPDATE and the affected-row check (the interface gains it in S4).

KEY RULES:
- Async with CancellationToken; never Open(); the Left for no pending row is created with ErrorCategory.Conflict (ADR 062).
- Integration tests use [Collection("ADO-SqlServer")] and siblings, ClearAllDataAsync in InitializeAsync.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/Scheduling/ScheduledMessageStoreADO.cs
- tests/Encina.IntegrationTests/ADO/SqlServer/Scheduling/ScheduledMessageStoreADOTests.cs
```

</details>

### Phase 3: Dapper providers x3 (S2)

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Dapper.<Db>/Scheduling/ScheduledMessageStoreDapper.cs`: `UpdateScheduledAtAsync` inside `EitherHelpers.TryAsync` with `DbLease.Enlisted`; `ExecuteAsync` returns the affected rows; 0 returns the Left (a failed statement stays `scheduling.update_scheduled_at_failed`, created with `ErrorCategory.Internal`).
2. Tests in the same PR: integration tests in `tests/Encina.IntegrationTests/Dapper/<Db>/Scheduling/` with the same cases as phase 2; the store guard classes gain `Guid.Empty`.
3. `PublicAPI.Unshipped.txt` and manifest per package through `mechanical-fixer`.

Count: 3 stores + 3 integration tests + 3 guard classes + 3 PublicAPI + 3 manifests = 15.

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

### Phase 4: EF Core and MongoDB (S3)

<details>
<summary><strong>Tasks</strong></summary>

1. `ScheduledMessageStoreEF.cs`: `UpdateScheduledAtAsync` with `ExecuteUpdateAsync` (atomic, filter `ProcessedAtUtc == null`), result 0 returns the Left; one store serves SqlServer, PostgreSQL and MySQL. `ExecuteUpdateAsync` runs immediately, unlike the existing methods that record on the tracker and wait for `SaveChangesAsync`: it joins the current database transaction only when the context has one (`Database.CurrentTransaction`), it does not see changes that are tracked but not yet saved, and a tracked instance of the same row in the same context is stale afterwards. The orchestrator does not load the row first; the XML doc states this, and an integration case covers an update after an unsaved `AddAsync` in the same unit of work.
2. `ScheduledMessageStoreMongoDB.cs`: `UpdateOneAsync` in the session with the null-`processedAtUtc` filter and `$set`/`$unset` of the retry fields; `MatchedCount == 0` returns the Left.
3. Tests in the same PR: shared integration bodies in `tests/Encina.TestInfrastructure/EFCore/ScheduledMessageStoreEFTestsBase.cs` and `tests/Encina.IntegrationTests/Infrastructure/MongoDB/Stores/ScheduledMessageStoreMongoDBIntegrationTests.cs`; the EF and MongoDB guard classes gain `Guid.Empty`.
4. PublicAPI (EF Core, MongoDB) and the two manifests through `mechanical-fixer`.

Count: 2 stores + 2 integration test files + 2 guard classes + 2 PublicAPI + 2 manifests = 10.

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

### Phase 5: Interface member, decorator and fake (S4)

<details>
<summary><strong>Tasks</strong></summary>

1. `IScheduledMessageStore.UpdateScheduledAtAsync(Guid messageId, DateTime newScheduledAtUtc, CancellationToken)` returning `Task<Either<EncinaError, Unit>>` (the 8 provider stores already have it).
2. `InstrumentedScheduledMessageStore` wraps it with the existing activity and metric pattern; `FakeScheduledMessageStore` implements it with the same pending rule.
3. Tests in the same PR: `InstrumentedScheduledMessageStoreTests`, `InstrumentedScheduledMessageStoreGuardTests`, `InstrumentedStoreMethodGuardTests`, unit tests of the fake pending rule, and a contract test of the pending-only rule and the idempotent cancel against `FakeScheduledMessageStore` (real code, no `.md`).
4. PublicAPI of Messaging and Fakes; manifests of Fakes and OpenTelemetry through `mechanical-fixer`.

Count: 3 src + 5 tests + 2 PublicAPI + 2 manifests = 12.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
- Every provider store already has UpdateScheduledAtAsync as a concrete member (S1 to S3); only the decorator and the fake lack it.

TASK:
Add the interface member, the decorator member and the fake member so the whole solution compiles.

KEY RULES:
- Either<EncinaError, T>; the decorator logs no payload and no EncinaError.Message; tests execute real code.

REFERENCE FILES:
- src/Encina.OpenTelemetry/MessagingStores/InstrumentedScheduledMessageStore.cs
- src/Encina.Testing.Fakes/Stores/FakeScheduledMessageStore.cs
```

</details>

### Phase 6: Orchestrator, logs and observability (S5, after #1222 P12 and #739)

<details>
<summary><strong>Tasks</strong></summary>

1. `SchedulerOrchestrator.RescheduleAsync(Guid messageId, DateTimeOffset newExecuteAt, CancellationToken)` and a `DateTime` overload: returns `scheduling.invalid_schedule_time` for a past time (choice 7), calls the store, logs and counts; a Left from the store propagates.
2. New `src/Encina.Messaging/Diagnostics/SchedulingRescheduleLog.cs` (`[LoggerMessage]`, EventIds 2340-2342 inside `EventIdRanges.MessagingScheduling`, already mapped to `Encina.Messaging` in `EncinaEventIdAllocationTests`): `MessageRescheduled`, `RescheduleRejectedNotPending`, `CancelRequested`; arguments are the message id and the error code. The log class is created here because this is the first pull request of this plan that logs. Ids 2343-2349 stay reserved and idle.
3. `SchedulingScheduleMetrics` and the span helpers of #1222: the counters `encina.scheduling.rescheduled` and `encina.scheduling.cancelled` gain the tag `operation`; spans `encina.scheduling.reschedule` and `encina.scheduling.cancel` carry the message id.
4. Tests in the same PR: `SchedulerOrchestratorTests` and `SchedulerOrchestratorGuardTests` (existing) extended, new `SchedulerOrchestratorRescheduleTests` (past time, store Left propagates, success logs and counts, with `FakeTimeProvider`), a test that no log argument, activity tag or metric tag carries a payload or key, and an exporter test of the counters.
5. `PublicAPI.Unshipped.txt` and the manifest of `Encina.Messaging` through `mechanical-fixer`.

Count: 4 src (`SchedulerOrchestrator.cs`, log class, metrics, activity source) + 5 tests + PublicAPI + manifest = 11.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
- EventIds 2340-2342 are used by this plan inside MessagingScheduling (2300-2399); #1222 owns 2326-2339. The shared metrics and spans are created by #1222.

TASK:
Add RescheduleAsync, the log class, the operation tag and the spans, with the tests of the phase.

KEY RULES:
- EncinaError.Message never reaches logs or tags; use the error code.
- Pack EventIds sequentially from 2340; XML docs name the range.

REFERENCE FILES:
- src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs
- src/Encina.Messaging/Diagnostics/SchedulingStoreLog.cs (pattern for the log class)
- tests/Encina.UnitTests/Messaging/Scheduling/SchedulerOrchestratorTests.cs
```

</details>

### Phase 7: Documentation and finalization (S6)

<details>
<summary><strong>Tasks</strong></summary>

1. `changelog.d/146-scheduled-message-reschedule.added.md` (a `mechanical-fixer` writes it).
2. `docs/features/scheduling.md`: cancel semantics, reschedule, the race outcome and the in-flight window of choice 3 (through the `docs-writer`).
3. Load and benchmark `.md` justifications (a single conditional update, not a concurrent hot path): `tests/Encina.LoadTests/Messaging/Scheduling/ScheduledMessageUpdate.md` and `tests/Encina.BenchmarkTests/Encina.Benchmarks/Messaging/Scheduling/ScheduledMessageUpdate.md`. Property: not required (a single conditional update; ordering is covered by integration), recorded in the PR; unit, guard, contract and integration are real tests.
4. XML docs with an example on `RescheduleAsync`; `dotnet build --configuration Release` with 0 warnings; all flags at target.

Count: 1 changelog + 1 page + 2 justifications = 4.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
CONTEXT:
- Documentation pages go to the docs-writer; changelog fragments to the mechanical-fixer.

TASK:
Finish the documentation and the justification files with the exact decided text.

KEY RULES:
- Never edit CHANGELOG.md; justification files follow the AGENTS.md section 9 template and are never used for unit, guard, contract or integration tests.

REFERENCE FILES:
- docs/features/scheduling.md
- changelog.d/README.md
```

</details>

### Per-file coverage targets (entered in the PR that carries the tests)

| File | PR | Flag and target | Justification |
| --- | --- | --- | --- |
| Each ADO and Dapper `ScheduledMessageStore*.cs` | S1, S2 | integration: the current manifest target, never lowered; guard: current target | The new method is exercised by the four integration cases of the same pull request; the worker reads the current value and only raises it. |
| `ScheduledMessageStoreEF.cs` | S3 | integration: current target, guard: current target | The shared EF bodies run the update, the race and the unsaved-add case on three providers. |
| `ScheduledMessageStoreMongoDB.cs` | S3 | integration: current target | The Mongo integration class drives the conditional `UpdateOneAsync` and the not-matched branch. |
| `Stores/FakeScheduledMessageStore.cs` | S4 | unit 95, contract 70 | The pending rule is a few branches, all unit-tested; the contract test runs it against the fake. |
| `MessagingStores/InstrumentedScheduledMessageStore.cs` | S4 | unit: current target, guard: current target | The forwarded method follows the pattern covered by the existing decorator tests. |
| `Scheduling/SchedulerOrchestrator.cs` | S0, S5 | unit and guard: the current manifest targets (the worker confirms them before editing) | `RescheduleAsync` is unit-tested with `FakeTimeProvider`; targets never go down. |
| `Diagnostics/SchedulingRescheduleLog.cs` | S5 | unit 0 | `[LoggerMessage]` declarations; the generated bodies are not in the file. |
| `Diagnostics/SchedulingScheduleMetrics.cs` | S5 | unit: current target | The exporter test drives the added tag. |

Guard tests for the new public API (#3798): `RescheduleAsync` (orchestrator) and `UpdateScheduledAtAsync` (each store, `Guid.Empty`) get guard tests in the pull request that adds them.

---

## Research

### Relevant Standards & Specifications

| Standard | Requirement | Key Details |
|----------|-------------|-------------|
| SPEC-002 REQ-033, AC-033 | Cancel and reschedule by key | Delivered by #1222; this plan covers the id-based remainder |
| ADR-001, ADR-006 | Railway Oriented Programming | `Either<EncinaError, T>`, no exceptions for business logic |
| ADR 062 | Error categories | `scheduling.message_not_pending` is `Conflict` |
| ADR-018 | Cross-cutting integration | Matrix below |
| ADR-021 | EventIds | Ids 2340-2342 inside `MessagingScheduling` |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `IScheduledMessageStore.CancelAsync`, `RescheduleRecurringMessageAsync` | `src/Encina.Messaging/Scheduling/IScheduledMessageStore.cs` | Existing cancel; pattern for the new update |
| `SchedulerOrchestrator.CancelAsync` | `src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs` | Gains `RescheduleAsync` next to it |
| `StoreValidationMessages` | provider packages | Argument-check messages |
| `SchedulingScheduleMetrics`, span helpers | created by #1222 | Reused with an `operation` tag |
| `SchedulingStoreLog` | `src/Encina.Messaging/Diagnostics/` | Pattern for the new log class |
| `ScheduledMessageStoreEFTestsBase` | `tests/Encina.TestInfrastructure/EFCore/` | Shared EF integration bodies |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` (`MessagingScheduling`, existing 2300-2399) | **2340-2342** used by #146, 2343-2349 reserved and idle | The batch registry holds 2326-2389 for #1222, #146, #150, #133 and #147; the sub-ranges of the siblings are theirs to state. `Encina.Messaging` already has an `AssemblyRanges` entry |
| New range | none | The block 6750-6799 is not needed |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production | 17 | S0 2, S1 3, S2 3, S3 2, S4 3, S5 4 |
| Tests | 28 | S1 6 (3 integration, 3 guard), S2 6, S3 4, S4 5, S5 5, S6 2 justifications |
| PublicAPI | 12 | S0 1, S1 3, S2 3, S3 2, S4 2, S5 1 |
| Coverage manifests | 12 | S0 1, S1 3, S2 3, S3 2, S4 2, S5 1 |
| Documentation, changelog | 2 | S6: changelog fragment and the feature page |
| **Total** | **71** | 17 + 28 + 12 + 12 + 2, the same 71 file edits as the per-PR table of the Summary |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no compatibility layers; time from TimeProvider.
- Issue #146: id-based reschedule and cancel semantics. #1222 (schedule key) lands first and owns the key methods, the shared metrics and EventIds 2326-2339.

IMPLEMENTATION OVERVIEW:
S0: MessageNotPending and the cancel docs. S1/S2/S3: UpdateScheduledAtAsync as a concrete member of the ADO, Dapper, EF Core (ExecuteUpdateAsync) and MongoDB (UpdateOneAsync) stores, with integration and guard tests. S4: interface member, decorator, fake. S5: SchedulerOrchestrator.RescheduleAsync, log events 2340-2342, operation tag (after #739). S6: docs.
Apply the Maintainer Decisions for cancel residue, race handling, reschedule scope and the tenant prerequisite.

KEY PATTERNS:
- One conditional statement decides the race; zero affected rows returns scheduling.message_not_pending.
- Cancel stays idempotent and does not interrupt an in-flight dispatch; an update during dispatch is lost until #1251.
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
| 2 | OpenTelemetry | ✅ | Phase 6: shared counters with an `operation` tag and reschedule and cancel spans |
| 3 | Structured Logging | ✅ | Phase 6: events 2340-2342 inside the registered range |
| 4 | Health Checks | ❌ | No checkable dependency is added |
| 5 | Validation | ✅ | Phase 6: a past time is rejected by the orchestrator; `Guid.Empty` is a programming error (ArgumentException from the store); a non-pending message returns `scheduling.message_not_pending` |
| 6 | Resilience | ❌ | No external call |
| 7 | Distributed Locks | ⏭️ | The race is resolved by the conditional statement; multi-instance claiming and the in-flight window are #1251 |
| 8 | Transactions | ✅ | Phases 2 to 4: the ADO.NET, Dapper and MongoDB updates join the scope transaction like the existing methods; the EF Core update runs immediately and joins only the context's current transaction (phase 4) |
| 9 | Idempotency | ✅ | Cancel is idempotent; a repeated reschedule to the same time is a stable `Right` |
| 10 | Multi-Tenancy | ⏭️ | Defer: #739 (open) is a hard prerequisite of S5 (choice 8) |
| 11 | Module Isolation | ❌ | SPEC-002 asks no module scoping; `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ⏭️ | Cancel and reschedule are operation-audit events (`IOperationAuditStore`): follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/scheduling-cancel-reschedule-audit.md); #1778 is the entity-change store |

---

## Prerequisites & Dependencies

| Item | Relation | Note |
|------|----------|------|
| #1222 | Must merge first (at least P12) | Key methods, shared counters and spans, EventIds 2326-2339 |
| #739 | Hard prerequisite of S5 | Tenant filter on id-based operations (choice 8) |
| #1251 | Coordinate | Claiming changes the completion methods and closes the in-flight window; `UpdateScheduledAtAsync` does not take a claim token |
| #133 | Sibling | Pause and resume of recurring messages use `scheduling.message_not_pending` created in S0 |

---

## Next Steps

1. Review this plan; the Dilemmas (choices 1 to 6 and 8: cancel residue, race handling, in-flight update, reschedule scope, cancel result, update semantics, tenant prerequisite) are answered by the maintainer when work starts and recorded in `## Maintainer Decisions`.
2. Decompose into S0 to S6 after #1222 P12 merges.
3. Link the plan from #146.
