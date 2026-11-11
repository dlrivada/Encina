# Implementation Plan: Scheduling minimal slices — schedule key, time-zone-aware cron, shipped `ICronParser`, orphan `IMessageScheduler`

> **Issue**: [#1222](https://github.com/dlrivada/Encina/issues/1222)
> **Type**: Feature (SPEC-002 P-20, REQ-033, AC-033, scenario S4; priority P0)
> **Complexity**: High (10 phases, all 10 database providers, shape change of `IScheduledMessage`, `IScheduledMessageStore`, `IScheduledMessageFactory` and `ICronParser`)
> **Estimated Scope**: ~900 lines of production code + ~1,600 lines of tests, ~65 files in 5 sub-pull requests
> **Family**: scheduling-1 (#1222 owns the shared pieces; #133, #146, #147 and #150 build on them)

---

## Summary

Scheduling gains the minimal slice SPEC-002 DEC-005 (a) puts into 1.0:

1. A **schedule key** (`ScheduleKey`, an application-defined string) with upsert, cancel and reschedule by key on all 10 database providers.
2. A **time zone stored with a recurring schedule**, so the cron expression is evaluated in the IANA zone (a 09:00 Europe/Madrid job stays at 09:00 local time across both DST changes, AC-033).
3. A **shipped `ICronParser`**: today the interface is declared in `src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs` and no implementation exists, so `ScheduleRecurringAsync` always returns `scheduling.no_cron_parser` unless the application writes its own parser. ADR-050 lists a default `ICronParser` (#1222) as a required dependency of scheduling when recurring messages are on.
4. The orphan `Encina.EntityFrameworkCore.Scheduling.IMessageScheduler` is **removed** (the maintainer settled this on the issue on 2026-10-17: nothing in `src/` uses it, and abstractions belong in `Encina.Messaging`).
5. `DateTimeOffset` is accepted wherever the orchestrator takes a `DateTime` execution time.

**Provider category**: Database (10): ADO.NET x3, Dapper x3, EF Core x3 (SqlServer, PostgreSQL, MySQL) and MongoDB. Hangfire and Quartz run infrastructure jobs and are not touched (AGENTS.md section 3, Scheduling bullet): they expose request scheduling helpers and use their own cron engines, not `ICronParser`.

**Affected packages**: `Encina.Messaging`, `Encina.ADO.SqlServer`, `Encina.ADO.PostgreSQL`, `Encina.ADO.MySQL`, `Encina.Dapper.SqlServer`, `Encina.Dapper.PostgreSQL`, `Encina.Dapper.MySQL`, `Encina.EntityFrameworkCore`, `Encina.MongoDB`, `Encina.OpenTelemetry` (the `InstrumentedScheduledMessageStore` decorator), `Encina.Testing.Fakes`, `Encina.Testing.Bogus`.

**Why it does not fit one pull request, and the split.** Adding members to `IScheduledMessage`, `IScheduledMessageStore` and `IScheduledMessageFactory` breaks all 10 implementers at once. The sub-issues therefore put the implementations first and the interface last, so every pull request compiles on its own and stays within 15 files and 1,500 lines (AGENTS.md section 11):

| Sub-issue | Content | Files (est.) |
|-----------|---------|:-:|
| S0 | New core types (`ScheduledMessageDraft`, `ScheduleOptions`, `RecurringOptions`, `ScheduleKeyValidator`, `SchedulingTimeZones`), the new error codes, `CronosCronParser` with the zone-aware `ICronParser` signature, package reference, registration, unit and property tests | 14 |
| S1 | ADO x3: `ScheduledMessage` properties, factory overload `Create(ScheduledMessageDraft)`, the SQL script columns and unique index, the three store methods as public methods of the concrete store, integration tests | 15 |
| S2 | Dapper x3: same content as S1 | 15 |
| S3 | EF Core (message, configuration, factory, store) and MongoDB (message, factory, store, index), shared integration test bodies | 14 |
| S4 | Wiring: the interface members, the orchestrator methods, DI, `FakeScheduledMessageStore`, `InstrumentedScheduledMessageStore`, removal of `IMessageScheduler` and its contract test, factories drop the seven-argument `Create`, docs, changelog | 15 |

Order: S0, then S1 to S3 in any order, then S4. #146, #150, #133 and #147 follow the same implementations-first order and start after S4 of this plan.

---

## Design Choices

<details>
<summary><strong>1. Where the default cron parser lives — `Encina.Messaging`</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `CronosCronParser` in `Encina.Messaging`, registered with `TryAdd` by the scheduling registration when `EnableRecurringMessages` is on** | Works out of the box; one default, as ADR-050 requires; the application can still register its own parser first | `Encina.Messaging` takes the cron library as a dependency |
| **B) A new satellite package the application adds explicitly** | `Encina.Messaging` stays free of the dependency | Recurring messages stay broken until the package is added; ADR-050 says the default is registered once it exists |

### Chosen Option: **A — default parser in `Encina.Messaging`**

### Rationale

- ADR-050 (scheduling row) lists "a default `ICronParser` (#1222)" as a required dependency, conditional on `EnableRecurringMessages`, registered with `TryAdd` "once the single default exists".
- `RegisterScheduling` in `src/Encina.Messaging/MessagingServiceCollectionExtensions.cs` already registers the retry policy and dispatcher with `TryAdd`; the parser joins them inside the same `if (schedulingOptions.EnableRecurringMessages)` branch.

**Class:** Settled
**Settled by:** ADR-050 (scheduling row, `ICronParser` TryAdd), SPEC-002 REQ-033

</details>

<details>
<summary><strong>2. Which cron library backs the default parser</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Cronos (HangfireIO, MIT)** | Tested; parses 5 and 6 fields (seconds) through a format flag; evaluates the next occurrence in a `TimeZoneInfo` and handles DST gaps and overlaps natively | A new package dependency for every `Encina.Messaging` consumer |
| **B) NCrontab** | Small | Weaker time zone and DST handling; Encina would own the zone logic and its DST bugs |
| **C) In-tree parser** | No dependency | Encina owns every cron edge case and every DST bug; the largest implementation |

### Chosen Option: **A — Cronos** (recommended, pending the maintainer)

### Rationale

- Option A wins for correctness, which is the reason the issue exists (silent DST drift is the stated motivation). Option B wins only when the dependency footprint of `Encina.Messaging` matters more than DST correctness; option C only if no library is acceptable.
- This is the same question as the cron-library Dilemma of #133 (D1); one answer covers both. Cronos is not in `Directory.Packages.props` today, so option A adds a `PackageVersion` pinned at implementation time. The ADR that records the answer is also the home of the DST semantics of #150 (ADR number assigned when the ADR pull request opens).

**Class:** Dilemma

</details>

<details>
<summary><strong>3. How the time zone reaches the parser</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ICronParser.GetNextOccurrence(string cronExpression, DateTime afterUtc, string? timeZoneId)` replaces the two-argument method** | The parser that understands zones does the work natively; one place for DST | Every implementation, including test fakes, takes the third parameter |
| **B) The orchestrator converts to the zone and back around a UTC-only parser** | The interface stays as it is | DST gap and overlap are re-implemented outside the parser; two places for time logic |

### Chosen Option: **A — zone parameter on the parser** (recommended, pending the maintainer)

### Rationale

- The interface has one method and one production caller (`SchedulerOrchestrator`, two call sites) plus test doubles, so the change is small; AGENTS.md section 1 (pre-1.0) rules out keeping the old signature next to the new one.
- A null `timeZoneId` means UTC, today's behavior. An unknown id returns `Left(scheduling.invalid_time_zone)`; a bad expression keeps `scheduling.invalid_cron_expression`.
- This is the parser-contract Dilemma of #150 (D3). Option B wins only when the smallest interface change matters more than keeping DST logic in one place.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Schedule key uniqueness and what an upsert replaces</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The key is unique over all rows; an upsert on an existing key reuses that row (same `Id`, `CreatedAtUtc`) and resets its processing state** | One plain unique index works on every provider, including MySQL, which has no partial index; native upserts (`ON CONFLICT`, `ON DUPLICATE KEY`, `MERGE`, Mongo `upsert`) apply directly | The processed state of the previous run for that key is overwritten |
| **B) The key is unique only among unprocessed rows (partial index)** | Keeps the history of earlier runs of a key | MySQL has no partial index (needs a generated column), the upsert has two shapes, and a processed row for key K and a pending one coexist |

### Chosen Option: **A — unique over all rows, upsert reuses the row** (recommended, pending the maintainer)

### Rationale

- `CancelAsync` deletes the row on every provider today (`DELETE FROM ... WHERE Id = @MessageId` in `ScheduledMessageStoreDapper.cs`, `Remove` in `ScheduledMessageStoreEF.cs`, `DeleteOneInSessionAsync` in the MongoDB store), so cancelling by key frees the key, and option A matches that model.
- On an update the statement sets `RequestType`, `Content`, `ScheduledAtUtc`, `IsRecurring`, `CronExpression`, `TimeZoneId` and clears `ProcessedAtUtc`, `ErrorMessage`, `RetryCount`, `NextRetryAtUtc`; it keeps `Id` and `CreatedAtUtc`. The store returns the stored `Id` as `Either<EncinaError, Guid>`.
- Option B wins when an application must keep one row per run of the same key (audit of earlier runs); that need is served by the dead letter and audit stores, not by this table.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Factory shape — a draft record instead of a longer argument list</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IScheduledMessageFactory.Create(ScheduledMessageDraft draft)`** | One signature for this slice and for the columns #133, #147 and the tenant id (#739) add later; the ten factories change once | The ten factories and their ten unit-test classes change now |
| **B) Keep positional parameters and append `scheduleKey` and `timeZoneId`** | Smaller diff now | The signature grows with every later column (priority, recurrence state, tenant); each growth touches ten factories again |

### Chosen Option: **A — draft record** (recommended, pending the maintainer)

### Rationale

- Option A wins because four open features add columns to the same row; it makes the later changes additive. Option B wins when the diff of this slice must be as small as possible.
- Pre-1.0: the seven-argument `Create` is removed, not kept beside the new one (AGENTS.md section 1).

**Class:** Dilemma

</details>

<details>
<summary><strong>6. `IMessageScheduler` is removed, not implemented</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Delete `src/Encina.EntityFrameworkCore/Scheduling/IMessageScheduler.cs` and its contract test** | No unused public API; the orchestrator is the single scheduling surface | The XML example and two README snippets must be rewritten |
| **B) Implement it over `SchedulerOrchestrator`** | Keeps the name | A second API with a `bool`-returning `CancelAsync` and a `Task<Guid>` that contradicts the Either convention; lives in one provider package against the provider-coherence rule |

### Chosen Option: **A — remove**

### Rationale

- Nothing in `src/` references the interface (only `tests/Encina.ContractTests/EntityFrameworkCore/IMessageSchedulerContractTests.cs`, which tests the interface by reflection); AGENTS.md section 3 forbids unused code and keeps abstractions in `Encina.Messaging`.
- Maintainer comment on #1222, 2026-10-17: "Settled by elimination ... remove the orphan".

**Class:** Settled
**Settled by:** AGENTS.md section 3 (no legacy or unused code; provider coherence), #1222 maintainer comment of 2026-10-17

</details>

<details>
<summary><strong>7. `DateTimeOffset` at the boundary</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Add `DateTimeOffset` overloads that convert with `UtcDateTime`; stores and columns stay UTC `DateTime`** | No schema change; one conversion point | Two overloads per method |
| **B) Change the stores to `DateTimeOffset`** | Offset preserved | Schema and interface change on 10 providers for no behavior gain |

### Chosen Option: **A — overloads converting at the orchestrator**

### Rationale

- UTC timestamps carry the `AtUtc` suffix and time comes from `TimeProvider` (AGENTS.md sections 3 and 4); REQ-033 only asks that `DateTimeOffset` is accepted.
- The `DateTime` overloads keep their documented UTC contract.

**Class:** Trivial
**Settled by:** AGENTS.md sections 3 and 4 (UTC with the `AtUtc` suffix), SPEC-002 REQ-033

</details>

---

## Implementation Phases

### Phase 1: Core types, contracts and error codes (S0 and S4)

> **Goal**: The new shapes in `Encina.Messaging` that every provider implements.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Messaging/Scheduling/ScheduledMessageDraft.cs` — sealed record: `Id`, `RequestType`, `Content`, `ScheduledAtUtc`, `CreatedAtUtc`, `IsRecurring`, `CronExpression`, `TimeZoneId`, `ScheduleKey` (the shared row-creation input, extended by #133, #147 and #739).
2. `ScheduleOptions.cs` (`ScheduleKey`) and `RecurringOptions.cs` (`ScheduleKey`, `TimeZoneId`) — sealed records; #133 adds the end and missed-occurrence properties to `RecurringOptions`.
3. `ScheduleKeyValidator.cs` — static; a key is 1 to 256 characters, not whitespace-only, no control characters; returns `scheduling.invalid_schedule_key`.
4. `SchedulingErrorCodes` (in `SchedulerOrchestrator.cs`): `InvalidTimeZone = "scheduling.invalid_time_zone"`, `InvalidScheduleKey`, `ScheduleKeyNotFound = "scheduling.schedule_key_not_found"`.
5. `IScheduledMessage`: `string? ScheduleKey { get; set; }` and `string? TimeZoneId { get; set; }` with XML docs (key unique; zone is an IANA id, null means UTC).
6. `IScheduledMessageStore` (S4, after the providers): `Task<Either<EncinaError, Guid>> UpsertByKeyAsync(IScheduledMessage message, CancellationToken)`, `Task<Either<EncinaError, Unit>> CancelByKeyAsync(string scheduleKey, CancellationToken)` (idempotent, like `CancelAsync`), `Task<Either<EncinaError, Unit>> RescheduleByKeyAsync(string scheduleKey, DateTime nextScheduledAtUtc, CancellationToken)` (Left `scheduling.schedule_key_not_found` when no pending row has the key: the statement carries `ProcessedAtUtc IS NULL`, so a processed one-time row is never revived by a reschedule, only by an upsert that replaces its payload; a recurring row is pending after every reschedule).
7. `IScheduledMessageFactory.Create(ScheduledMessageDraft draft)` replaces the seven-argument method (S4).
8. `PublicAPI.Unshipped.txt` of `Encina.Messaging` (a `mechanical-fixer` writes the lines).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
- Issue #1222 (SPEC-002 REQ-033). Encina is pre-1.0 .NET 10 / C# 14; no [Obsolete], no compatibility layers.
- src/Encina.Messaging/Scheduling/ holds IScheduledMessage, IScheduledMessageStore and SchedulerOrchestrator.cs (which declares IScheduledMessageFactory, ICronParser, SchedulingErrorCodes).

TASK:
Add ScheduledMessageDraft, ScheduleOptions, RecurringOptions, ScheduleKeyValidator and the new error codes. Add ScheduleKey and TimeZoneId to IScheduledMessage. Add the three key methods to IScheduledMessageStore and change IScheduledMessageFactory.Create to take the draft. Do the interface edits only in the sub-issue where every provider already implements them.

KEY RULES:
- Either<EncinaError, T> on every store method; XML docs on every public member.
- UTC timestamps carry the AtUtc suffix; the schedule key is never logged or used as a metric tag (it can identify a data subject).

REFERENCE FILES:
- src/Encina.Messaging/Scheduling/IScheduledMessage.cs
- src/Encina.Messaging/Scheduling/IScheduledMessageStore.cs
- src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs
```

</details>

### Phase 2: Shipped cron parser with time zone (S0)

> **Goal**: A DST-correct default `ICronParser`.

<details>
<summary><strong>Tasks</strong></summary>

1. Add `Cronos` to `Directory.Packages.props` (version pinned at implementation) and a `PackageReference` in `src/Encina.Messaging/Encina.Messaging.csproj`.
2. `ICronParser`: `Either<EncinaError, DateTime> GetNextOccurrence(string cronExpression, DateTime afterUtc, string? timeZoneId)`.
3. `src/Encina.Messaging/Scheduling/CronosCronParser.cs` — public sealed class: 5 fields use the standard format and 6 fields the seconds format; resolves the zone through `SchedulingTimeZones.TryResolve` (null means UTC); returns the next instant after `afterUtc` as UTC; `Left(scheduling.invalid_cron_expression)` or `Left(scheduling.invalid_time_zone)` otherwise.
4. `SchedulingTimeZones.cs` — internal static `TryResolve(string? id, out TimeZoneInfo zone)` over `TimeZoneInfo.FindSystemTimeZoneById`. Risk to verify at implementation: minimal container images may lack tz data; document the requirement.
5. `MessagingServiceCollectionExtensions.RegisterScheduling`: when `schedulingOptions.EnableRecurringMessages`, `services.TryAddSingleton<ICronParser, CronosCronParser>()`.
6. Update the two call sites in `SchedulerOrchestrator` to pass the new argument (null in S0).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
- ICronParser has one method today and no implementation ships. The decision on the library is Dilemma 2 of the plan; implement the answer recorded in the plan (Cronos by default).

TASK:
Implement CronosCronParser and SchedulingTimeZones, change ICronParser to the zone-aware signature, register the parser with TryAdd under EnableRecurringMessages, and update the orchestrator call sites.

KEY RULES:
- No exceptions for business input: an invalid expression or zone is a Left with the scheduling.* code.
- The parser never reads the clock; the caller passes afterUtc from TimeProvider.
- Tests: the 09:00 Europe/Madrid job on both sides of the last-Sunday-of-October and last-Sunday-of-March changes; a property test that the result is after the input and its local time is stable.

REFERENCE FILES:
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs (RegisterScheduling)
- src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs (HandleRecurringMessageAsync, ScheduleRecurringAsync)
```

</details>

### Phase 3: Orchestrator API (S4)

> **Goal**: Key and zone aware scheduling on the single scheduling surface.

<details>
<summary><strong>Tasks</strong></summary>

1. `ScheduleAsync<TRequest>(TRequest, DateTimeOffset executeAt, ScheduleOptions? options = null, CancellationToken)`; the existing `DateTime` and `TimeSpan` overloads gain a `ScheduleOptions` overload. A set `ScheduleKey` selects `UpsertByKeyAsync`, otherwise `AddAsync`.
2. `ScheduleRecurringAsync<TRequest>(TRequest, string cronExpression, RecurringOptions options, CancellationToken)`: validates the zone and key, computes the first occurrence in the zone, persists `TimeZoneId`.
3. `CancelByKeyAsync(string scheduleKey, ct)` and `RescheduleByKeyAsync(string scheduleKey, DateTimeOffset newExecuteAt, ct)`.
4. `HandleRecurringMessageAsync` passes `message.TimeZoneId` to the parser, so each recurrence is computed in the stored zone.
5. All message creation goes through `ScheduledMessageDraft`; keep every new method under cyclomatic complexity 10 (CRAP gate).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
- SchedulerOrchestrator is a sealed class (there is no ISchedulerOrchestrator). ScheduleAsync returns Either<EncinaError, Guid>; time comes from the injected TimeProvider.

TASK:
Add the key, DateTimeOffset and time zone members listed in the plan, reusing one private method that builds the draft and one that validates key and zone.

KEY RULES:
- A past execution time returns scheduling.invalid_schedule_time, as today.
- A Left from the store propagates; nothing is swallowed.
- Log the message id, never the key or the request content.

REFERENCE FILES:
- src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs
- tests/Encina.UnitTests/Messaging/Scheduling/SchedulerOrchestratorTests.cs
- tests/Encina.UnitTests/Messaging/Scheduling/SchedulerOrchestratorScheduleRecurringTests.cs
```

</details>

### Phase 4: Configuration, DI and removal of `IMessageScheduler` (S0 and S4)

<details>
<summary><strong>Tasks</strong></summary>

1. The parser registration of phase 2; no new options class (the zone is per schedule, see #150).
2. Delete `src/Encina.EntityFrameworkCore/Scheduling/IMessageScheduler.cs` and `tests/Encina.ContractTests/EntityFrameworkCore/IMessageSchedulerContractTests.cs`; remove stale lines from the `Encina.EntityFrameworkCore` PublicAPI files if present.
3. Rewrite the `IMessageScheduler` snippets in `src/Encina.Messaging/README.md` (around lines 192 and 513) to use `SchedulerOrchestrator`.
4. DI test: build the provider with `ValidateOnBuild` and `ValidateScopes` for scheduling on, recurring on and off, and assert `ICronParser` resolves only when recurring is on and that an application-registered parser wins.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
- AGENTS.md section 3: an AddEncina* method must register everything it resolves, proven by a DI test with ValidateOnBuild and ValidateScopes.

TASK:
Register the default parser, remove IMessageScheduler and its contract test, update the README snippets, and add the DI test.

KEY RULES:
- TryAdd so an application parser registered first wins.
- No compatibility alias for IMessageScheduler.

REFERENCE FILES:
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs
- src/Encina.EntityFrameworkCore/Scheduling/IMessageScheduler.cs
- src/Encina.Messaging/README.md
```

</details>

### Phase 5: ADO.NET providers x3 (S1)

> **Goal**: Columns, index and the three store methods on SqlServer, PostgreSQL and MySQL.

<details>
<summary><strong>Tasks</strong></summary>

1. `Scripts/004_CreateScheduledMessagesTable.sql` of each ADO package (edited in place, no migration support, AGENTS.md section 1): `ScheduleKey` NVARCHAR(256) or VARCHAR(256) NULL, `TimeZoneId` VARCHAR(100) NULL; unique index on the key (SQL Server filtered `WHERE ScheduleKey IS NOT NULL`, PostgreSQL and MySQL allow many NULLs in a unique index).
2. `ScheduledMessage.cs`: the two properties. `ScheduledMessageFactory.cs`: `Create(ScheduledMessageDraft)` next to the old method until S4.
3. `ScheduledMessageStoreADO.cs`: add the key columns to `AddAsync`'s column list; `UpsertByKeyAsync` (SQL Server `MERGE ... WITH (HOLDLOCK)` with `OUTPUT inserted.Id`; PostgreSQL `INSERT ... ON CONFLICT (schedulekey) DO UPDATE ... RETURNING id`; MySQL `INSERT ... ON DUPLICATE KEY UPDATE` followed by a `SELECT Id` in the same command scope); `CancelByKeyAsync` (`DELETE`); `RescheduleByKeyAsync` (`UPDATE ... SET ScheduledAtUtc ... WHERE ScheduleKey = @Key AND ProcessedAtUtc IS NULL`, reset retry state, affected rows 0 returns the not-found Left).
4. All commands use `OpenAsync`, `ExecuteNonQueryAsync`/`ExecuteScalarAsync` with the `CancellationToken` and join the scope transaction through the existing transaction accessor (AGENTS.md section 3, async database calls).
5. Extend `tests/Encina.IntegrationTests/ADO/<Db>/Scheduling/ScheduledMessageStoreADOTests.cs` (existing `[Collection("ADO-<Db>")]`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
- Existing stores: src/Encina.ADO.<Db>/Scheduling/ScheduledMessageStoreADO.cs. Differences: SQL Server uses TOP and bit, PostgreSQL lower-case identifiers and LIMIT, MySQL backtick-safe identifiers and 0/1.

TASK:
Implement the schema columns, the draft factory overload and the three key methods for the three ADO providers as public methods of the concrete store (the interface gains them in S4).

KEY RULES:
- Async with CancellationToken only; never Open().
- The unique index, not application code, resolves two concurrent upserts; a duplicate-key error on insert is retried once as an update.
- Integration tests: shared [Collection] fixtures, ClearAllDataAsync in InitializeAsync, [Trait("Category","Integration")].

REFERENCE FILES:
- src/Encina.ADO.SqlServer/Scheduling/ScheduledMessageStoreADO.cs
- src/Encina.ADO.PostgreSQL/Scripts/004_CreateScheduledMessagesTable.sql
- tests/Encina.IntegrationTests/ADO/SqlServer/Scheduling/ScheduledMessageStoreADOTests.cs
```

</details>

### Phase 6: Dapper providers x3 (S2)

<details>
<summary><strong>Tasks</strong></summary>

1. Same columns and SQL as phase 5 in `src/Encina.Dapper.<Db>/Scripts/004_CreateScheduledMessagesTable.sql` and `Scheduling/ScheduledMessageStoreDapper.cs`.
2. `ScheduledMessageStoreDapper` uses `EitherHelpers.TryAsync`, `DbLease.Enlisted(_connection, _transactionAccessor)` and `CommandDefinition` with the token; the new methods follow that shape with error codes `scheduling.upsert_failed`, `scheduling.cancel_by_key_failed`, `scheduling.reschedule_by_key_failed`.
3. Message class and `Create(ScheduledMessageDraft)` per package; integration tests in `tests/Encina.IntegrationTests/Dapper/<Db>/Scheduling/`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
- Phase 5 defines the SQL per database; the Dapper stores wrap it in EitherHelpers.TryAsync and DbLease.Enlisted.

TASK:
Implement the Dapper side for SqlServer, PostgreSQL and MySQL.

KEY RULES:
- Validation of an empty key or a past time follows the store convention (argument checks throw ArgumentException today); the orchestrator validates first, so the stores keep their convention.
- Collections Dapper-SqlServer, Dapper-PostgreSQL, Dapper-MySQL.

REFERENCE FILES:
- src/Encina.Dapper.SqlServer/Scheduling/ScheduledMessageStoreDapper.cs
- tests/Encina.IntegrationTests/Dapper/SqlServer/Scheduling/ScheduledMessageStoreDapperTests.cs
```

</details>

### Phase 7: EF Core and MongoDB (S3)

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessage.cs` and `ScheduledMessageConfiguration.cs`: `ScheduleKey` (max 256) with `HasIndex(...).IsUnique()` and `TimeZoneId` (max 100). One configuration serves SqlServer, PostgreSQL and MySQL; on PostgreSQL and MySQL a nullable unique index allows many NULLs and the SQL Server provider filters it.
2. `ScheduledMessageStoreEF.cs`: `UpsertByKeyAsync` reads the row by key in the current unit of work, updates or adds it, saves, and on a unique-violation `DbUpdateException` re-reads once; `CancelByKeyAsync` removes by key; `RescheduleByKeyAsync` updates.
3. `src/Encina.MongoDB/Scheduling/ScheduledMessage.cs` (`[BsonElement("scheduleKey")]`, `[BsonElement("timeZoneId")]`), factory, and `ScheduledMessageStoreMongoDB.cs`: upsert with `FindOneAndUpdate` and `IsUpsert = true` using `$set` for the mutable fields and `$setOnInsert` for `_id` and `createdAtUtc`; a unique partial index on `scheduleKey` where it is a string.
4. Shared integration test bodies in `tests/Encina.TestInfrastructure/EFCore/ScheduledMessageStoreEFTestsBase.cs` (run by the three EF provider classes) and `tests/Encina.IntegrationTests/Infrastructure/MongoDB/Stores/ScheduledMessageStoreMongoDBIntegrationTests.cs`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
CONTEXT:
- EF Core: one store and one configuration for the three providers; the provider-specific part is only the DbContext provider. MongoDB: BsonElement attributes on the message class, indexes created with the store.

TASK:
Implement the EF Core and MongoDB parts of phases 5 and 6.

KEY RULES:
- The unit of work (SaveChangesAsync) stays the caller's; the store methods record on the context like the existing ones and the orchestrator batch saves.
- A Mongo upsert is one atomic FindOneAndUpdate, never read-then-write.

REFERENCE FILES:
- src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessageStoreEF.cs
- src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessageConfiguration.cs
- src/Encina.MongoDB/Scheduling/ScheduledMessageStoreMongoDB.cs
```

</details>

### Phase 8: Cross-cutting integration and observability (S4)

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Messaging/Diagnostics/SchedulingScheduleLog.cs` — `[LoggerMessage]` events 2326-2331 inside `EventIdRanges.MessagingScheduling` (2300-2399, already mapped to `Encina.Messaging` in `EncinaEventIdAllocationTests`): `ScheduleUpserted`, `ScheduleRescheduledByKey`, `ScheduleCancelledByKey`, `ScheduleKeyNotFound`, `InvalidTimeZone`, `InvalidScheduleKey`. Arguments are the message id and the error code only.
2. `SchedulingScheduleMetrics.cs` on the shared `Encina` meter (as `SchedulingProcessorMetrics`): counters `encina.scheduling.rescheduled` (tag `recurring`) and `encina.scheduling.cancelled`; no key, payload or zone as a metric tag. Spans `encina.scheduling.upsert`, `...reschedule`, `...cancel` in `SchedulingActivitySource` with the recurring flag; the tenant id attribute is added when #739 lands.
3. `InstrumentedScheduledMessageStore` (Encina.OpenTelemetry) wraps the three methods; `FakeScheduledMessageStore` implements them; `ScheduledMessageFaker` (Bogus) and `ScheduledMessageBuilder` (TestInfrastructure) set the two new properties.
4. Test that no activity tag, metric tag or log argument carries the key or content.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
CONTEXT:
- Logging uses the [LoggerMessage] source generator; ids 2326-2339 are reserved for this issue inside MessagingScheduling (2300-2399); #146 uses 2340-2349, #150 2350-2359, #133 2360-2379 and #147 2380-2389.

TASK:
Add the log class, the metrics class, the spans, the decorator members and the fake members.

KEY RULES:
- EncinaError.Message never reaches logs, tags or health results; log the error code.
- Pack ids sequentially from 2326; XML docs name the range.

REFERENCE FILES:
- src/Encina.Messaging/Diagnostics/SchedulingStoreLog.cs
- src/Encina.Messaging/Diagnostics/SchedulingProcessorMetrics.cs
- src/Encina.OpenTelemetry/MessagingStores/InstrumentedScheduledMessageStore.cs
```

</details>

### Phase 9: Testing

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit** (`tests/Encina.UnitTests/Messaging/Scheduling/`): orchestrator key paths, zone validation, DateTimeOffset conversion with `FakeTimeProvider`; `CronosCronParserTests`; one factory test class per provider updated for `Create(ScheduledMessageDraft)`.
2. **Guard** (`tests/Encina.GuardTests/Messaging/Scheduling/`): every new public method with parameters throws `ArgumentNullException`; the store guard classes of the 9 SQL providers and EF Core.
3. **Contract**: `ICronParser` contract against `CronosCronParser`; factory contract across the 10 factories (pure, no database); key-method contract against `FakeScheduledMessageStore`.
4. **Property** (`tests/Encina.PropertyTests/Messaging/Scheduling/`): for any valid 5-field expression and zone, the next occurrence is after the input and its local time of day is stable across the two yearly DST changes.
5. **Integration**: the key lifecycle on all 10 providers (upsert twice returns the same `Id`, cancel then reschedule returns the not-found code, two concurrent upserts end with one row) plus the scenario S4 case through `SchedulerOrchestrator` (P-25). A two-tenant case is added with #739.
6. **Load and benchmark**: `.md` justifications under `tests/Encina.LoadTests/Messaging/Scheduling/` and `tests/Encina.BenchmarkTests/Encina.Benchmarks/Messaging/Scheduling/` (not a concurrent hot path).
7. **Obligations** (baseline measured per flag with `dotnet test tests\Encina.UnitTests --collect "XPlat Code Coverage" --results-directory artifacts\coverage\UnitTests` and the other flag projects, then `dotnet run --file .github/scripts/coverage-report.cs -- --input artifacts/coverage --output artifacts/coverage-report`): `Scheduling/CronosCronParser.cs` unit 95, guard 60, contract 70, property 80 (pure logic, a 0 is not justified); `Scheduling/SchedulerOrchestrator.cs` keeps unit 90 and guard 30 and raises neither down (targets never go down); each provider store file: unit and guard at its current target, integration for the new methods. A `mechanical-fixer` writes the manifest entries.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
CONTEXT:
- Tests must execute real package code; reflection-only tests cover nothing. Shouldly through Encina.Testing.Shouldly; no FluentAssertions. FakeTimeProvider for time.

TASK:
Write the tests of the plan for the sub-issue you are in; run the flag projects and compare the per-file coverage with the manifest targets.

KEY RULES:
- Integration tests use shared [Collection("<Family>-<Db>")] fixtures and never dispose the fixture.
- No Thread.Sleep; deterministic times.
- A test never swallows an exception thrown by the code it drives.

REFERENCE FILES:
- tests/Encina.UnitTests/Messaging/Scheduling/SchedulerOrchestratorTests.cs
- tests/Encina.TestInfrastructure/EFCore/ScheduledMessageStoreEFTestsBase.cs
- tests/Encina.IntegrationTests/Dapper/SqlServer/Scheduling/ScheduledMessageStoreDapperTests.cs
```

</details>

### Phase 10: Documentation and finalization

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs on every new public API with an example for `ScheduleAsync` with a key and `ScheduleRecurringAsync` with a zone.
2. `changelog.d/1222-scheduling-schedule-key.added.md` and `changelog.d/1222-scheduling-imessagescheduler.removed.md` (a `mechanical-fixer` writes them).
3. `PublicAPI.Unshipped.txt` lines for `Encina.Messaging` (and the removal for `Encina.EntityFrameworkCore` if the interface was tracked).
4. `docs/features/scheduling.md` (key, zone, parser, DST note); `src/Encina.Messaging/README.md`; the ADR on the cron library and time-zone semantics (ADR number assigned when the ADR pull request opens); `docs/INVENTORY.md` for the new files.
5. `dotnet build --configuration Release` with 0 warnings; `dotnet test` green; every coverage flag at its target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 10</strong></summary>

```
CONTEXT:
- Documentation pages under docs/ go to the docs-writer; changelog fragments and PublicAPI lines go to the mechanical-fixer.

TASK:
Finish the XML docs and hand the page, README, ADR, changelog and PublicAPI work to the specialists with the exact text decided in the plan.

KEY RULES:
- Never edit CHANGELOG.md; coverage figures are cited with covref markers, never typed.

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
| SPEC-002 REQ-033 | Scheduling minimal slice | Schedule key, time-zone-aware cron storing the zone, shipped `ICronParser`, orphan interface resolved, `DateTimeOffset` accepted |
| SPEC-002 AC-033 and S4 | Acceptance | Upsert and cancel by key on 10 providers; 09:00 Europe/Madrid fires at 09:00 local on both sides of a DST change |
| SPEC-002 DEC-005 (a) | Why now | Schema and signature changes across 10 providers are free before 1.0 |
| ADR-050 | Scheduling dependencies | A default `ICronParser`, registered with `TryAdd` only when `EnableRecurringMessages` is on |
| ADR-018 | Cross-cutting integration | The 12-function matrix below |
| ADR-021 | EventIds | Ids inside the registered `MessagingScheduling` range |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `SchedulerOrchestrator`, `SchedulingErrorCodes`, `ICronParser`, `IScheduledMessageFactory` | `src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs` | Extended; the factory and parser contracts change |
| `IScheduledMessage`, `IScheduledMessageStore` | `src/Encina.Messaging/Scheduling/` | Two properties and three methods added |
| `RegisterScheduling` | `src/Encina.Messaging/MessagingServiceCollectionExtensions.cs` | Registers the default parser with `TryAdd` |
| `SchedulingProcessorMetrics`, `SchedulingActivitySource`, `SchedulingStoreLog` | `src/Encina.Messaging/Diagnostics/` | Pattern for the new metrics, spans and log class |
| `EitherHelpers.TryAsync`, `DbLease.Enlisted` | Dapper and ADO packages | Store method shape and transaction enlistment |
| `InstrumentedScheduledMessageStore` | `src/Encina.OpenTelemetry/MessagingStores/` | Decorator members |
| `FakeScheduledMessageStore`, `ScheduledMessageFaker`, `ScheduledMessageBuilder` | `Encina.Testing.Fakes`, `Encina.Testing.Bogus`, `Encina.TestInfrastructure` | Test doubles and builders |

Provider files touched (first draft by the local model, reviewed against the tree):

| Provider | Message class | Factory | Store | Schema or mapping |
|----------|---------------|---------|-------|-------------------|
| ADO-SqlServer, -PostgreSQL, -MySQL | `src/Encina.ADO.<Db>/Scheduling/ScheduledMessage.cs` | `.../ScheduledMessageFactory.cs` | `.../ScheduledMessageStoreADO.cs` | `src/Encina.ADO.<Db>/Scripts/004_CreateScheduledMessagesTable.sql` |
| Dapper-SqlServer, -PostgreSQL, -MySQL | `src/Encina.Dapper.<Db>/Scheduling/ScheduledMessage.cs` | `.../ScheduledMessageFactory.cs` | `.../ScheduledMessageStoreDapper.cs` | `src/Encina.Dapper.<Db>/Scripts/004_CreateScheduledMessagesTable.sql` |
| EFCore-SqlServer, -PostgreSQL, -MySQL | `src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessage.cs` | `.../ScheduledMessageFactory.cs` | `.../ScheduledMessageStoreEF.cs` | `.../ScheduledMessageConfiguration.cs` (shared) |
| MongoDB | `src/Encina.MongoDB/Scheduling/ScheduledMessage.cs` | `.../ScheduledMessageFactory.cs` | `.../ScheduledMessageStoreMongoDB.cs` | BsonElement attributes and an index |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` (`MessagingScheduling`, existing) | **2326-2339** reserved for #1222 | `SchedulingScheduleLog`; 2300-2306 are `SchedulingStoreLog` and 2320-2325 `SchedulingProcessorLog`. Family reservations inside the same range: #146 2340-2349, #150 2350-2359, #133 2360-2379, #147 2380-2389 |
| New range | none | The block 6750-6799 assigned to this family is not needed |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Core `Encina.Messaging` (types, parser, orchestrator, DI, diagnostics, csproj, PublicAPI) | ~16 | S0 and S4 |
| ADO.NET x3 | ~9 | message, factory, store, script per provider less shared edits |
| Dapper x3 | ~9 | same |
| EF Core and MongoDB | ~7 | |
| OpenTelemetry, Fakes, Bogus, TestInfrastructure | ~5 | |
| Removal of `IMessageScheduler` and its contract test | 2 | deleted |
| Tests | ~35 | unit, guard, contract, property, integration |
| Documentation and changelog | ~6 | |
| **Total** | **~89 touched, ~65 net new or changed in 5 pull requests** | |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library. Either<EncinaError, T> everywhere; no [Obsolete], no compatibility layers; time from TimeProvider.
- Issue #1222 (SPEC-002 REQ-033): schedule key, zone-aware cron, shipped ICronParser, remove IMessageScheduler, DateTimeOffset.

IMPLEMENTATION OVERVIEW:
S0: core types, error codes, CronosCronParser with the zone-aware ICronParser, registration.
S1/S2/S3: ADO x3, Dapper x3, EF Core, MongoDB: columns, unique index, three store methods as concrete members, integration tests.
S4: interface members, orchestrator methods, DI, fakes, decorator, removal of IMessageScheduler, docs.
Apply the answers recorded in Maintainer Decisions for the cron library, the parser contract, the key scope and the factory shape.

KEY PATTERNS:
- Implementations first, interface last, so each pull request compiles.
- Unique index resolves concurrent upserts; schedule keys and payloads never reach logs, tags or metrics.
- EventIds 2326-2339 inside EventIdRanges.MessagingScheduling.
- Integration tests: [Collection("ADO-SqlServer")] style shared fixtures, ClearAllDataAsync.

REFERENCE FILES:
- src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs
- src/Encina.Dapper.SqlServer/Scheduling/ScheduledMessageStoreDapper.cs
- docs/architecture/adr/050-modules-may-require-modules.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | No read path benefits from a cache; resolved `TimeZoneInfo` instances are cached by the runtime |
| 2 | OpenTelemetry | ✅ | Phase 8: counters `encina.scheduling.rescheduled` and `encina.scheduling.cancelled`, upsert/reschedule/cancel spans, no key or payload in tags |
| 3 | Structured Logging | ✅ | Phase 8: events 2326-2331 in `SchedulingScheduleLog` inside the registered range |
| 4 | Health Checks | ❌ | No new checkable dependency; the existing `SchedulingHealthCheck` already reports overdue messages |
| 5 | Validation | ✅ | Phase 1 and 3: key, cron expression and zone id validated at schedule time, invalid input returns Left |
| 6 | Resilience | ❌ | No call to an external system |
| 7 | Distributed Locks | ⏭️ | Multi-instance claiming is #1251; upsert atomicity here comes from the unique index |
| 8 | Transactions | ✅ | Phases 5 to 7: the new store methods join the scope transaction like the existing ones |
| 9 | Idempotency | ✅ | The schedule key is the deduplication key: scheduling the same key again updates one row |
| 10 | Multi-Tenancy | ⏭️ | The tenant column and filters are #739 (open); the unique index becomes (tenant, key) there; AC-043 is met when both land |
| 11 | Module Isolation | ❌ | SPEC-002 asks no module scoping; `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ❌ | No compliance-relevant data is created; cancel and reschedule audit events are tracked with #146 and #1778 |

---

## Prerequisites & Dependencies

| Item | Relation | Note |
|------|----------|------|
| #739 | Advisable first | `TenantId` on `ScheduledMessage`; the key's unique index and the metric attribute depend on it |
| #718 and #1251 | Coordinate | #1251 replaces `GetDueMessagesAsync` with claiming and changes every completion method of `IScheduledMessageStore`; the new key methods are independent of the claim token. Merge order is decided at decomposition |
| #146, #150, #133, #147 | Follow this plan | Each reuses `ScheduledMessageDraft`, the key methods, the parser and the EventId reservations listed above |
| #1153 | Related | `ScheduleRecurringAsync` ignoring a failed insert; the orchestrator here returns the store's Left |

---

## Next Steps

1. Review this plan; the four Dilemmas (cron library, parser contract, key scope, factory shape) are answered by the maintainer when work on the issue starts and recorded in `## Maintainer Decisions`.
2. Decompose into the sub-issues S0 to S4 of the Summary, each with its exact file list and baseline.
3. Link the plan from #1222 and start with S0 in its own worktree.
