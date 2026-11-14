# Implementation Plan: Recurring messages — end conditions, pause and resume, missed occurrences and `[RecurringSchedule]`

> **Issue**: [#133](https://github.com/dlrivada/Encina/issues/133)
> **Type**: Feature
> **Complexity**: High (7 phases, all 10 database providers, six new columns, orchestrator lifecycle)
> **Estimated Scope**: ~1,000 lines of production code + ~2,300 lines of tests, documentation and manifests; 154 file edits in 16 pull requests that each compile on their own, none over 700 changed lines
> **Family**: scheduling-1 (builds on #1222 and #146; shares the row with #147)

---

## Summary

Recurrence already exists in a basic form, and this plan extends it (all facts re-checked against `origin/main`, 2026-11-13):

- `IScheduledMessage` has `IsRecurring`, `CronExpression` and `LastExecutedAtUtc`; the next run time is stored in `ScheduledAtUtc`; `SchedulerOrchestrator.ScheduleRecurringAsync(request, cronExpression, ct)` and the private `HandleRecurringMessageAsync` reschedule after each run through `IScheduledMessageStore.RescheduleRecurringMessageAsync`; `CancelAsync(Guid, ct)` removes the schedule.
- `ICronParser` has no implementation today. #1222 ships `CronosCronParser` with the zone-aware signature and the stored `TimeZoneId`, so time zones, 5 and 6 field expressions and the cron library are **not** part of this plan.
- Missing, and delivered here: end conditions (never, after N occurrences, on a date), pause and resume, missed-occurrence handling, the `[RecurringSchedule]` attribute with a startup registrar, fluent options, and metrics.

**A latent bug found while reading the stores.** The six ADO.NET and Dapper stores select due messages with `WHERE (ProcessedAtUtc IS NULL OR IsRecurring = 1) ...` (10 matching lines, for example in `src/Encina.Dapper.SqlServer/Scheduling/ScheduledMessageStoreDapper.cs` and `src/Encina.ADO.PostgreSQL/Scheduling/ScheduledMessageStoreADO.cs`), while `ScheduledMessageStoreEF.cs` (line 63) and `ScheduledMessageStoreMongoDB.cs` require `ProcessedAtUtc == null`. When a recurring schedule ends, `HandleRecurringMessageAsync` calls `MarkAsProcessedAsync`; on the six SQL providers that row stays due and runs again at every cycle. End conditions make this reachable on purpose, so the first two pull requests (R0a, R0b) align the six predicates with EF Core and MongoDB and prove it on all 10 providers; the changelog fragment `133-scheduling-due-predicate.fixed.md` ships in R0a, the pull request that fixes the bug. A bug issue file is written for it so it can ship before this plan.

**Provider category**: Database (10): ADO.NET x3, Dapper x3, EF Core x3 (SqlServer, PostgreSQL, MySQL), MongoDB.

**Affected packages**: `Encina.Messaging`, the nine SQL provider packages, `Encina.EntityFrameworkCore`, `Encina.MongoDB`, `Encina.OpenTelemetry` (decorator), `Encina.Testing.Fakes`, `Encina.Testing.Bogus`, `Encina.Testing.FsCheck`, plus `tests/Encina.TestInfrastructure` and the benchmark projects.

Other facts verified on `origin/main`: `IScheduledMessage` has 13 implementers (8 providers, `FakeScheduledMessage`, the nested `FakeScheduledMessage` of the Bogus faker, `ArbitraryScheduledMessage`, two `BenchmarkScheduledMessage`); `IScheduledMessageStore` has 10 implementers (8 providers, `InstrumentedScheduledMessageStore`, `FakeScheduledMessageStore`); `SchedulingErrorCodes` is declared in `src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs` (line 630, with `NoCronParser` and `UnknownRequestShape`) and receives `InvalidRecurrenceOptions`; ADR 062 is merged (PR #3599): each new error code is created with `EncinaErrors.Create(code, ErrorCategory, message)` (`scheduling.invalid_recurrence_options` is `Validation`).

**Split** (additive first, interface last, so every pull request compiles on its own; the counts include tests, PublicAPI files, manifests and existing callers of a changed signature):

| PR | Content | Files | Lines (est.) |
| --- | --- | ---: | ---: |
| R0a | Due-predicate fix on ADO x3 with regression tests and the `fixed` changelog | 7 | 200 |
| R0b | Due-predicate fix on Dapper x3, regression tests on Dapper, EF Core and MongoDB | 8 | 250 |
| R1 | Enums, `RecurringOptions` properties, builder, `SchedulingOptions`, `InvalidRecurrenceOptions` | 12 | 450 |
| R2 | ADO.SqlServer: six columns, due filter, increment, pause and resume (concrete) | 9 | 300 |
| R3 | Dapper.SqlServer | 8 | 250 |
| R4 | ADO.PostgreSQL | 9 | 300 |
| R5 | Dapper.PostgreSQL | 8 | 250 |
| R6 | ADO.MySQL | 9 | 300 |
| R7 | Dapper.MySQL | 8 | 250 |
| R8 | EF Core and MongoDB | 11 | 450 |
| R9 | Six properties on `IScheduledMessage` and the 5 non-provider implementers | 14 | 300 |
| R10 | Pause and resume on `IScheduledMessageStore`, decorator and fake | 12 | 350 |
| R11 | Orchestrator lifecycle, log class, metrics, spans | 13 | 700 |
| R12 | Recurring scenarios on the 10 providers | 11 | 500 |
| R13 | `[RecurringSchedule]` attribute, registrar, DI | 8 | 350 |
| R14 | Documentation, ADR section, changelog, justifications | 7 | 150 |
| **Total** | | **154** | **about 5,000** |

By category: production 55, tests and test support 61, PublicAPI 16, manifests 16, documentation, changelog and fragments 6: 55 + 61 + 16 + 16 + 6 = 154. The header, this table and the Estimated File Count table all state 154. Order: R0a and R0b first (a bug fix, independent), then R1, then R2 to R8 (an ADO.NET pull request before the Dapper one of the same database because the shared test schema changes there), R9, R10, R11 (after #739 if #1222 gates it, see choice 12), R12, R13, R14. All of them start after #1222 P12 and #146 S3 have merged because they edit the same store files.

**Coordination (family scheduling-1)**: #1222 owns `RecurringOptions` (this plan adds properties), the parser, the key methods and the settable-property approach to new columns (its choice 5; if the maintainer picks the draft record there, the recurrence properties travel in that record instead). #146 owns `scheduling.message_not_pending` (created in its first pull request), which pause and resume reuse. This plan creates `SchedulingErrorCodes.InvalidRecurrenceOptions` (R1), `SchedulingRecurrenceLog` and `SchedulingRecurrenceMetrics` (R11) and takes EventIds 2360-2369 inside the 2326-2389 span the batch registry holds for #1222, #146, #150, #133 and #147.

---

## Design Choices

<details>
<summary><strong>1. Which cron library backs the default parser (decided in #1222)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Cronos** | Tested; 5 and 6 fields; zones and DST native | A new dependency for `Encina.Messaging` consumers |
| **B) NCrontab** | Small | Weaker zone and DST handling |
| **C) In-tree parser** | No dependency | Encina owns every cron edge case |

### Chosen Option: **A — Cronos** (recommended, pending the maintainer)

### Rationale

- This is the cron-library Dilemma of #1222 (its choice 2); the issue's own D1 asks the same question and one answer covers both. This plan adds no parser code: `Describe`, `IsValid` and `GetNextOccurrences` of the original proposal are not shipped because no feature here uses them (a validity check is the `Left` of `GetNextOccurrence`, as today).

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Default handling of missed occurrences</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Run once, then continue from now** (`RunOnce`) | Catches up without a flood; this is what `HandleRecurringMessageAsync` does today because it computes the next run from the current time | The count of missed runs is lost |
| **B) Skip every missed run** (`Skip`) | Never runs late | Silent gaps |
| **C) Run every missed occurrence** (`RunAll`) | Nothing lost | A burst after long downtime |

### Chosen Option: **A — `RunOnce` as the default** (recommended, pending the maintainer)

### Rationale

- A wins for idempotent "do the latest" jobs such as reports; B for time-critical jobs that are useless late; C for accounting-style jobs where each occurrence matters.
- All three modes ship as `MissedOccurrenceHandling` on `RecurringOptions` (stored per schedule); the default only decides what a schedule without the option does.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Where recurrence state lives</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Columns on the scheduled-message row** | No new table or store on 10 providers; fits the existing reschedule-after-run model | A wider row; pause and end state sit next to delivery state |
| **B) A separate schedule entity with its own store; each run is a message** | Clean model; run history; one schedule produces many messages | A new store, factory, scripts and tests on all 10 providers |

### Chosen Option: **A — columns on the row** (recommended, pending the maintainer)

### Rationale

- A wins for the minimal slice and for the rest of the family (#1222, #147 add columns to the same row); B wins if executions need history (a run log), which the dead letter queue and the audit stores already serve for failures.
- New columns, all nullable or defaulted so the `CREATE TABLE` scripts are edited in place (AGENTS.md section 1, no migrations): `RecurrenceEndMode` (int, default 0), `MaxOccurrences` (int null), `OccurrenceCount` (int, default 0), `RecurrenceEndsAtUtc` (datetime null), `PausedAtUtc` (datetime null), `MissedOccurrenceHandling` (int, default 0).

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Which end conditions ship</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Never`, `AfterCount`, `OnDate`** | All are plain data, storable and testable on every provider | No custom condition |
| **B) Add `OnCondition`** | Flexible | A delegate or expression cannot be persisted portably across 10 providers |

### Chosen Option: **A — three modes** (recommended, pending the maintainer)

### Rationale

- A wins for the first slice; B wins only with a persisted-condition design (a named condition resolved at run time), which is a feature of its own.
- Rules: `AfterCount` needs `MaxOccurrences >= 1`; `OnDate` needs `RecurrenceEndsAtUtc` in the future (a past date is a validation error, not an immediate end); otherwise `Left(scheduling.invalid_recurrence_options)`. The last run ends the schedule through `MarkAsProcessedAsync` (the row is then excluded from due queries by the R0 predicate).

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Shape of `[RecurringSchedule]` and its registrar</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Attribute on the request type; an opt-in registrar creates the request with its public parameterless constructor and upserts it with `ScheduleKey` = the type's full name; a removed attribute never deletes the schedule (documented); startup fails when an annotated type lacks the constructor, has an invalid expression or the upsert returns `Left`** | Declarative; the key makes registration idempotent across restarts and instances; fails closed | Only requests constructible without data; the key format is public |
| **B) Attribute plus an `IRecurringRequestFactory<T>` the application registers** | Requests that need data or services | More ceremony for the simple case |
| **C) No attribute; the application calls `ScheduleRecurringAsync` with a key at startup** | Nothing new to maintain | The acceptance criterion of the issue asks for the attribute |

### Chosen Option: **A — attribute with a parameterless request** (recommended, pending the maintainer)

### Rationale

- A wins for health-check-style commands (the sample of the issue); B wins when the request carries data from configuration; C when the team prefers explicit startup code.
- The registrar is opt-in (`AddEncinaRecurringSchedules(params Assembly[])`, pay-for-what-you-use). Failing startup on a failed upsert follows the fail-closed rule for background infrastructure (AGENTS.md section 3).

**Class:** Dilemma

</details>

<details>
<summary><strong>6. `SkipIfPreviousRunning` is not shipped (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No option; one instance never overlaps two runs of the same schedule because the next run is scheduled only after the dispatch returns** | Every line serves a current purpose | Across instances nothing prevents two instances from fetching the same due row until row claiming (#1251) lands; the interim behavior is the duplicate dispatch every scheduled message has today |
| **B) Add the flag from the original proposal** | Matches the sketch | Dead configuration inside one instance; cross-instance prevention still needs claiming |

### Chosen Option: **A — not shipped** (recommended, pending the maintainer)

### Rationale

- Option A wins while the single-instance guarantee is what the flag would express; option B wins if the maintainer wants the issue's API surface kept. The interim cross-instance behavior is stated in the documentation (R14) and closed by #1251 (open); it is not "impossible by construction".

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Cancel reuse; pause and resume by id (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reuse `CancelAsync(Guid)` and `CancelByKeyAsync` (#1222); add `PauseRecurringAsync(Guid)` and `ResumeRecurringAsync(Guid)`** | One cancel path; the id of a keyed schedule is returned by the upsert | A caller holding only a key looks the id up first |
| **B) Reuse cancel and add key variants `PauseRecurringByKeyAsync` and `ResumeRecurringByKeyAsync`** | A keyed schedule is controlled without a lookup; key variants of pause and resume are not aliases of cancel | Twice the store members on 10 providers |
| **C) Add `CancelRecurringAsync`** | Symmetric name | A second name for the same operation, which AGENTS.md section 3 forbids (compatibility alias) |

### Chosen Option: **A — reuse cancel, id-based pause and resume** (recommended, pending the maintainer)

### Rationale

- Option C is excluded by the cited rule; options A and B both comply, so the choice between them is open. A wins for the smallest surface; B when keyed schedules are the normal case (the `[RecurringSchedule]` registrar creates only keyed ones). Resume recomputes the next run from the current time so it never floods.

**Class:** Dilemma

</details>

<details>
<summary><strong>8. Thresholds and small rules</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `SchedulingOptions.MissedOccurrenceTolerance` 5 minutes (a run is "missed" when `now - ScheduledAtUtc` exceeds it on its first attempt); `MaxCatchUpOccurrences` 100 per cycle so one schedule cannot monopolize a batch; `RunAll` stops at its first failure and that failure follows the normal retry path (a `Left` is never reported as success); resume recomputes from now** | Safe defaults; fail-closed on failure | Values are not tuned by measurement |
| **B) Other values** | None identified | Arbitrary |

### Chosen Option: **A**

### Rationale

- No observable default for a caller that does not use recurring options; the defaults can change before 1.0. The failure rule is the background-infrastructure rule.

**Class:** Trivial

</details>

<details>
<summary><strong>9. What counts as an occurrence (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `OccurrenceCount` increases with each dispatched run, so a `RunAll` catch-up of 3 missed runs counts 3 and `AfterCount` N means N dispatches** | `AfterCount` is exactly "N runs happened" | A catch-up burst can end an `AfterCount` schedule within one cycle |
| **B) `OccurrenceCount` increases once per reschedule (once per cycle)** | A catch-up counts as one | `AfterCount` N can mean more than N dispatches |

### Chosen Option: **A — per dispatch** (recommended, pending the maintainer)

### Rationale

- Option A wins for accounting-style jobs; option B for jobs where a catch-up is a single logical run. The increment is atomic in the store (never read-modify-write).

**Class:** Dilemma

</details>

<details>
<summary><strong>10. Names of the new properties</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `OccurrenceCount`, `RecurrenceEndsAtUtc` and the other names of choice 3** | Follow the naming rules (`AtUtc` suffix, `...Count`) | Differ from the issue text (`CurrentOccurrence`, `EndsAtUtc`) |
| **B) The issue's names** | Match the sketch | Break the rules |

### Chosen Option: **A**

### Rationale

- UTC timestamps carry the `AtUtc` suffix and counters end in `Count`; option B violates the cited rule.

**Class:** Settled
**Settled by:** AGENTS.md section 4 (UTC timestamps with the `AtUtc` suffix, `RetryCount` style counters)

</details>

<details>
<summary><strong>11. Pause and resume on a message in the wrong state (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One code: pause of a non-recurring, processed or already-paused message, and resume of a message that is not paused, all return `Left(scheduling.message_not_pending)` (created by #146)** | No new code; one rule | The caller cannot tell the reasons apart |
| **B) Distinct codes (`scheduling.not_recurring`, `scheduling.not_paused`)** | Precise errors | New codes and tests on 10 providers |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

- Option A wins for the smallest surface; option B when callers branch on the reason. Both are `Either`.

**Class:** Dilemma

</details>

<details>
<summary><strong>12. Tenant isolation before #739 (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) #739 (`TenantId` on `ScheduledMessage`) is a hard prerequisite of R11 (the pull request that exposes pause, resume and the lifecycle on the orchestrator) and R13; R0 to R10 add only concrete members and interface members that nothing calls** | Fails closed: no public API can pause another tenant's schedule or register an unscoped one | R11 waits for #739 |
| **B) Ship first and document the limit** | No wait | Until #739, id-based pause and resume reach any tenant's schedule |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

- Gates fail closed (AGENTS.md section 3); the same question is answered for #1222 and #146. The tenant policy of the attribute registrar is decided with #739.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Due-predicate bug fix (R0a, R0b)

<details>
<summary><strong>Tasks</strong></summary>

1. In the six SQL stores replace `(ProcessedAtUtc IS NULL OR IsRecurring = 1)` by `ProcessedAtUtc IS NULL` (`ScheduledMessageStoreADO.cs` and `ScheduledMessageStoreDapper.cs` of SqlServer, PostgreSQL, MySQL).
2. Regression test on all 10 providers: a recurring message ended through `MarkAsProcessedAsync` is not returned by `GetDueMessagesAsync` again (existing store test classes of the six SQL providers, the shared EF base `ScheduledMessageStoreEFTestsBase` and `ScheduledMessageStoreMongoDBIntegrationTests`, which prove the already correct behavior).
3. `changelog.d/133-scheduling-due-predicate.fixed.md` in R0a (a `mechanical-fixer` writes it). No manifest edit: the stores keep their current targets.

Count: R0a 3 stores + 3 integration tests + 1 changelog = 7; R0b 3 stores + 3 integration tests + 2 (EF base, MongoDB class) = 8.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
- Issue #133, bug found while planning. SQL stores select due rows with "OR IsRecurring = 1"; EF Core and MongoDB require ProcessedAtUtc == null.

TASK:
Align the six predicates and add the regression integration test on every provider.

KEY RULES:
- Async with CancellationToken; shared [Collection("<Family>-<Db>")] fixtures; never a .md for integration tests.

REFERENCE FILES:
- src/Encina.Dapper.SqlServer/Scheduling/ScheduledMessageStoreDapper.cs
- src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessageStoreEF.cs
- tests/Encina.IntegrationTests/Dapper/SqlServer/Scheduling/ScheduledMessageStoreDapperTests.cs
```

</details>

### Phase 2: Core model, options and error code (R1)

> **Goal**: The shared types of the recurrence feature (nothing consumes them yet).

<details>
<summary><strong>Tasks</strong></summary>

1. `RecurrenceEndMode.cs` (`Never`, `AfterCount`, `OnDate`) and `MissedOccurrenceHandling.cs` (`RunOnce`, `Skip`, `RunAll`) in `src/Encina.Messaging/Scheduling/`.
2. `RecurringOptions` (created by #1222: `ScheduleKey`, `TimeZoneId`) gains `EndMode`, `MaxOccurrences`, `EndsAtUtc`, `MissedHandling`; `RecurringOptionsBuilder.cs` with `InTimeZone`, `WithKey`, `EndAfterOccurrences`, `EndOnDate`, `OnMissedOccurrence`; validation of option combinations in one method returning `Either`.
3. `SchedulingOptions`: `MissedOccurrenceTolerance` (TimeSpan, 5 minutes) and `MaxCatchUpOccurrences` (int, 100).
4. `SchedulingErrorCodes.InvalidRecurrenceOptions = "scheduling.invalid_recurrence_options"` in `SchedulerOrchestrator.cs` (the class at line 630), created with `ErrorCategory.Validation`.
5. Tests: builder unit tests, options validation unit tests, `SchedulingOptions` unit test, a guard test of the builder.
6. `PublicAPI.Unshipped.txt` and manifest of `Encina.Messaging` through `mechanical-fixer`.

Count: 6 src (two enums, `RecurringOptions`, builder, `SchedulingOptions`, `SchedulerOrchestrator.cs`) + 4 tests + PublicAPI + manifest = 12.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
- Issue #133. #1222 and #146 have merged: RecurringOptions, CronosCronParser and scheduling.message_not_pending exist.

TASK:
Add the enums, the options properties, the builder, the SchedulingOptions properties and the error code.

KEY RULES:
- Either<EncinaError, T>; error codes created with an ErrorCategory (ADR 062); XML docs on every public member; UTC timestamps carry the AtUtc suffix.
- No interface edits here.

REFERENCE FILES:
- src/Encina.Messaging/Scheduling/SchedulingOptions.cs
- src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs
```

</details>

### Phase 3: Providers — columns, due filter, pause and resume (R2 to R8)

> **Goal**: The six columns and the store behavior on all 10 providers, as concrete members.

<details>
<summary><strong>Tasks</strong></summary>

1. One pull request per SQL provider (ADO.NET first, then Dapper, per database): the six columns in `Scripts/004_CreateScheduledMessagesTable.sql` and the table in `Scripts/000_CreateAllTables.sql`, the six properties on the provider `ScheduledMessage` (the enum types from R1), the `AddAsync` column lists and readers, the due filter `AND PausedAtUtc IS NULL`, and the shared test schema (`tests/Encina.TestInfrastructure/Schemas`) and benchmark schema builder section of the database.
2. `RescheduleRecurringMessageAsync` also sets `LastExecutedAtUtc = now` and `OccurrenceCount = OccurrenceCount + 1` in the same statement (an atomic increment, never a read-modify-write; choice 9).
3. New concrete store members: `PauseRecurringAsync(Guid messageId, DateTime pausedAtUtc, ct)` (`WHERE Id = @id AND IsRecurring AND ProcessedAtUtc IS NULL AND PausedAtUtc IS NULL`) and `ResumeRecurringAsync(Guid messageId, DateTime nextScheduledAtUtc, ct)` (clears `PausedAtUtc`, sets `ScheduledAtUtc`, resets retry fields, `WHERE PausedAtUtc IS NOT NULL`); zero affected rows returns `Left(scheduling.message_not_pending)` (choice 11).
4. R8: EF Core (`ScheduledMessage.cs`, `ScheduledMessageConfiguration.cs` with enum conversions, `ScheduledMessageStoreEF.cs` with `ExecuteUpdateAsync`) and MongoDB (`ScheduledMessage.cs` with `[BsonElement]`, `ScheduledMessageStoreMongoDB.cs` with `UpdateOneAsync` and `$inc` for the count).
5. Tests in each pull request: the existing store integration class gains pause hides and resume restores a message, the atomic increment under two concurrent reschedules and the paused filter; R8 uses the shared EF base and the MongoDB class.
6. PublicAPI and manifest entries per package through `mechanical-fixer`.

Count: ADO pull request 4 package files + PublicAPI + manifest + test schema + benchmark builder + integration test = 9 (R2, R4, R6); Dapper pull request 4 + PublicAPI + manifest + benchmark builder + integration test = 8 (R3, R5, R7); R8 5 src + 2 tests + 2 PublicAPI + 2 manifests = 11.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
- Stores: src/Encina.ADO.<Db>/Scheduling/ScheduledMessageStoreADO.cs, src/Encina.Dapper.<Db>/Scheduling/ScheduledMessageStoreDapper.cs, src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessageStoreEF.cs, src/Encina.MongoDB/Scheduling/ScheduledMessageStoreMongoDB.cs.
- SQL differs per database: SQL Server bit and TOP, PostgreSQL lower-case identifiers and LIMIT, MySQL 0/1. Integration tests build the table from tests/Encina.TestInfrastructure/Schemas.

TASK:
Implement ONE provider (or EF Core plus MongoDB) per pull request.

KEY RULES:
- Never Open() or other synchronous database calls.
- The same observable behavior on all 10 providers; shared [Collection("<Family>-<Db>")] fixtures.

REFERENCE FILES:
- src/Encina.Dapper.SqlServer/Scheduling/ScheduledMessageStoreDapper.cs
- src/Encina.ADO.PostgreSQL/Scripts/004_CreateScheduledMessagesTable.sql
- tests/Encina.IntegrationTests/Dapper/SqlServer/Scheduling/ScheduledMessageStoreDapperTests.cs
```

</details>

### Phase 4: Interface members, decorator and fake (R9, R10)

<details>
<summary><strong>Tasks</strong></summary>

1. R9: `IScheduledMessage` gains `RecurrenceEndMode`, `MaxOccurrences`, `OccurrenceCount`, `RecurrenceEndsAtUtc`, `PausedAtUtc`, `MissedOccurrenceHandling` (the 8 providers already have them); the 5 other implementers (`FakeScheduledMessage`, the Bogus faker, `ArbitraryScheduledMessage`, the two `BenchmarkScheduledMessage` classes) and `ScheduledMessageBuilder` take them; a contract test (real code) asserts the 8 provider factories and the fake create messages with the defaults; PublicAPI (Messaging, Fakes, Bogus) and manifests (Fakes, Bogus, FsCheck). Count: 4 src + builder + 2 benchmark implementers + contract test + 3 PublicAPI + 3 manifests = 14.
2. R10: `IScheduledMessageStore.PauseRecurringAsync` and `ResumeRecurringAsync` (the 8 providers already have them); `InstrumentedScheduledMessageStore` wraps them; `FakeScheduledMessageStore` implements them; tests `InstrumentedScheduledMessageStoreTests`, two guard classes, the fake unit tests and a pause, resume and increment contract test against the fake; PublicAPI (Messaging, Fakes) and manifests (Fakes, OpenTelemetry). Count: 3 src + 5 tests + 2 PublicAPI + 2 manifests = 12.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
- R2 to R8 gave every provider the properties and the two store members as concrete members; only non-provider implementers lack them.

TASK:
Add the interface members and update the other implementers so the whole solution compiles (R9 for IScheduledMessage, R10 for IScheduledMessageStore).

KEY RULES:
- Either<EncinaError, T>; no reflection-only tests; a nullable parameter that accepts null gets a valid-result guard test; XML docs on every public member.

REFERENCE FILES:
- src/Encina.Messaging/Scheduling/IScheduledMessage.cs
- src/Encina.OpenTelemetry/MessagingStores/InstrumentedScheduledMessageStore.cs
- src/Encina.Testing.Fakes/Stores/FakeScheduledMessageStore.cs
```

</details>

### Phase 5: Orchestrator lifecycle and observability (R11, after #739 per choice 12)

<details>
<summary><strong>Tasks</strong></summary>

1. `ScheduleRecurringAsync` with `RecurringOptions`: validates the options (Phase 2), computes the first run through `ICronParser` in the stored zone, assigns the recurrence properties to the created message (the settable-property approach of #1222).
2. `HandleRecurringMessageAsync` (private) is split into small methods: `EndRecurringAsync` (ends when the count or date limit is reached or the parser returns no next run), `RescheduleRecurringAsync` (stores the next run, increments the count). Each new method stays under cyclomatic complexity 10 (CRAP gate).
3. Missed occurrences: a pre-dispatch check on the first attempt applies the stored `MissedOccurrenceHandling`: `RunOnce` dispatches; `Skip` reschedules without dispatching; `RunAll` dispatches once per missed occurrence up to `MaxCatchUpOccurrences`, stopping at the first failure, which follows the normal retry path.
4. `PauseRecurringAsync(Guid, ct)` and `ResumeRecurringAsync(Guid, ct)` (resume computes the next run from the current time in the stored zone and calls the store). `CompiledExpressionScheduledMessageDispatcher` and `ScheduledMessageProcessor` need no change.
5. New `SchedulingRecurrenceLog.cs` in `src/Encina.Messaging/Diagnostics/` (`[LoggerMessage]`, 2360-2369 inside `EventIdRanges.MessagingScheduling`, already mapped to `Encina.Messaging`): `RecurringPaused` (2360), `RecurringResumed`, `MissedOccurrenceRunOnce`, `MissedOccurrenceSkipped`, `MissedOccurrencesCaughtUp`, `CatchUpCapped`, `RecurringEndedByCount`, `RecurringEndedByDate`, `RecurringScheduleRegistered`, `RecurringScheduleRegistrationFailed` (2369); arguments are message ids, counts and error codes. The last two are used by the registrar of R13; they are created here, the first pull request that logs, and R13 only calls them.
6. `SchedulingRecurrenceMetrics.cs` on the `Encina` meter: `encina.scheduling.recurring.runs`, `encina.scheduling.recurring.missed` (tag `handling`), `encina.scheduling.recurring.ended` (tag `reason`: `count`, `date`, `no_next`); spans `encina.scheduling.pause` and `encina.scheduling.resume` in `SchedulingActivitySource`. The existing `SchedulingHealthCheck` already reports overdue messages and paused rows are not due, so no new health check is needed.
7. Tests in the same PR: the three existing orchestrator test files extended, new `SchedulerOrchestratorLifecycleTests` (each end mode and each missed mode with `FakeTimeProvider`), a property test (next-occurrence monotonicity over repeated reschedules; the count never exceeds `MaxOccurrences`; an `OnDate` schedule never runs after its end date), a metrics exporter test and a test that no log argument, tag or metric carries the key, payload or `EncinaError.Message`.
8. `PublicAPI.Unshipped.txt` and manifest of `Encina.Messaging` through `mechanical-fixer`.

Count: 4 src (`SchedulerOrchestrator.cs`, log class, metrics, activity source) + 7 tests + PublicAPI + manifest = 13.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
- SchedulerOrchestrator is a sealed class. Time comes from the injected TimeProvider; the cron parser is the optional ICronParser dependency. EventIds 2360-2379 are held for #133 inside MessagingScheduling (the batch registry holds 2326-2389 for #1222, #146, #150, #133 and #147).
- Errors from the store must never be swallowed: a Left fails the operation (logged by code) and the message keeps its state.

TASK:
Implement the recurrence lifecycle of the plan with small private methods, plus the log class, metrics and spans.

KEY RULES:
- Log message ids and error codes only; never the payload, the schedule key or EncinaError.Message.
- Unit tests with FakeTimeProvider for each end mode and each missed-handling mode; pack ids sequentially from 2360.

REFERENCE FILES:
- src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs
- src/Encina.Messaging/Diagnostics/SchedulingStoreLog.cs
- tests/Encina.UnitTests/Messaging/Scheduling/SchedulerOrchestratorScheduleRecurringTests.cs
```

</details>

### Phase 6: Scenarios on the 10 providers (R12)

<details>
<summary><strong>Tasks</strong></summary>

1. `tests/Encina.IntegrationTests/Messaging/Scheduling/RecurringScheduleScenarios.cs` (shared scenario, like `SchedulerCycleScenarios`): `AfterCount` ends after N runs through `SchedulerOrchestrator`, `OnDate` ends, `Skip` and `RunOnce` after a simulated downtime, pause hides and resume restores.
2. One derived class per provider in the shared collections (ADO x3, Dapper x3, EF Core x3, MongoDB): 10 files.

Count: 1 scenario + 10 provider classes = 11.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
- Tests execute real package code; Shouldly via Encina.Testing.Shouldly; FakeTimeProvider; shared database collections.

TASK:
Write the scenario and its 10 provider classes; compare per-file coverage with the manifest targets.

KEY RULES:
- No Thread.Sleep; no test swallows an exception thrown by the code it drives.
- Integration tests are never replaced by a .md justification.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Scheduling/SchedulerCycleScenarios.cs
- tests/Encina.TestInfrastructure/EFCore/ScheduledMessageStoreEFTestsBase.cs
```

</details>

### Phase 7: Attribute, registrar and documentation (R13, R14)

<details>
<summary><strong>Tasks</strong></summary>

1. R13: `RecurringScheduleAttribute.cs` (`CronExpression`, `TimeZone`, optional end-mode data); `RecurringScheduleRegistrar.cs` (internal `IHostedService`) scans the assemblies given to `AddEncinaRecurringSchedules(params Assembly[])`, creates each annotated request with its parameterless constructor and upserts it with `ScheduleKey = type full name`; an annotated type without that constructor, an invalid expression or a failed upsert fails startup with the error code (fail closed); the registration method in `MessagingServiceCollectionExtensions.cs`. Tests: registrar unit test with a fake orchestrator and real attribute scanning, a DI test with `ValidateOnBuild` and `ValidateScopes` (the registrar is absent unless the method is called and resolves `SchedulerOrchestrator` through a created scope), a guard test. PublicAPI and manifest. Count: 3 src + 3 tests + PublicAPI + manifest = 8.
2. R14: load and benchmark `.md` justifications (recurring runs are sequential per row; the parser is #1222's) at `tests/Encina.LoadTests/Messaging/Scheduling/RecurringSchedules.md` and `tests/Encina.BenchmarkTests/Encina.Benchmarks/Messaging/Scheduling/RecurringSchedules.md`; `changelog.d/133-scheduling-recurring-messages.added.md`; `docs-writer`: `docs/features/scheduling.md` recurring section (end modes, pause and resume, missed handling, the attribute, the interim cross-instance duplicate dispatch of choice 6), `src/Encina.Messaging/README.md`, the ADR section on missed-occurrence semantics added to the cron ADR of #1222 (ADR number assigned when the ADR pull request opens), `docs/INVENTORY.md`. Count: 2 justifications + changelog + 4 docs = 7.
3. `dotnet build --configuration Release` with 0 warnings; all flags at target; XML docs with examples on `ScheduleRecurringAsync` with options, pause, resume and the attribute.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
CONTEXT:
- The upsert by schedule key (#1222) makes registration idempotent across restarts and across instances starting together (the unique index resolves the race). Documentation pages go to the docs-writer; changelog fragments, PublicAPI lines and manifests to the mechanical-fixer.

TASK:
Add the attribute, the opt-in registrar and the registration method (R13); finish the documentation with the exact decided text (R14).

KEY RULES:
- Opt-in only; nothing is scanned unless AddEncinaRecurringSchedules is called; startup failure is explicit, a hosted service never lets an exception escape silently.
- Never edit CHANGELOG.md; never type coverage figures by hand; justification files only for load and benchmark.

REFERENCE FILES:
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs
- src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs
- docs/features/scheduling.md
- changelog.d/README.md
```

</details>

### Per-file coverage targets (entered in the PR that carries the tests)

| File | PR | Flag and target | Justification |
| --- | --- | --- | --- |
| `Scheduling/RecurringOptionsBuilder.cs` | R1 | unit 95, guard 70 | Every fluent method and the validation combinations are unit-tested; guard covers null and out-of-range arguments. |
| `Scheduling/RecurrenceEndMode.cs`, `MissedOccurrenceHandling.cs` | R1 | unit 0 | Enum declarations have no executable lines. |
| `Scheduling/RecurringOptions.cs`, `SchedulingOptions.cs` | R1 | unit 95, guard 40 | Defaults and validation of the new properties; guard covers the setters that throw. |
| Each provider `ScheduledMessageStore*.cs` | R0a to R8 | integration: the current manifest target, never lowered | The fixed predicate, pause, resume, increment and paused filter are exercised by the integration cases of the same pull request; the worker reads the current value and only raises it. |
| Each provider `ScheduledMessage.cs` | R2 to R8 | integration: current target | The six properties are written and read by the same cases. |
| `Stores/FakeScheduledMessageStore.cs` | R10 | unit 95, contract 70 | Pause, resume and increment are small branches, all unit-tested and run through the contract test. |
| `Scheduling/SchedulerOrchestrator.cs` | R1, R11 | unit and guard: the current manifest targets (the worker confirms them), CRAP at most 10 on every changed method | The lifecycle is unit-tested per end mode and per missed mode; targets never go down. |
| `Diagnostics/SchedulingRecurrenceMetrics.cs` | R11 | unit 90 | The exporter test drives the three instruments. |
| `Diagnostics/SchedulingRecurrenceLog.cs` | R11 | unit 0 | `[LoggerMessage]` declarations; the generated bodies are not in the file. |
| `Scheduling/RecurringScheduleRegistrar.cs` | R13 | unit 85, guard 60 | The scan, the idempotent upsert and every startup failure are unit-tested with a fake orchestrator; guard covers the constructor arguments. |
| `Scheduling/RecurringScheduleAttribute.cs` | R13 | unit 80 | The scanning tests read real attributes; the rest are property getters. |

Guard tests for the new public API (#3798): the builder methods, `PauseRecurringAsync` and `ResumeRecurringAsync` (orchestrator and each store), `AddEncinaRecurringSchedules` (null assemblies) get guard tests in the pull request that adds them. Test flags: unit and guard in every pull request that changes `src/`; contract on the fake (R9, R10); property in R11; integration on the 10 providers in R0 to R8 and R12; load and benchmark by `.md` (R14) only.

---

## Research

### Relevant Standards & Specifications

| Standard | Requirement | Key Details |
|----------|-------------|-------------|
| SPEC-002 REQ-033 | Recurring cron with stored zone, shipped parser | Delivered by #1222; this plan builds on it |
| ADR-050 | Scheduling dependencies | Default `ICronParser` (#1222) when recurring is on; claiming (#1251); dispatch idempotency (#735) |
| ADR 062 | Error categories | Category of `scheduling.invalid_recurrence_options` |
| ADR-018 | Cross-cutting integration | Matrix below |
| ADR-021 | EventIds | Ids 2360-2369 inside `MessagingScheduling` |
| MassTransit recurring jobs, Quartz.NET triggers, Hangfire recurring jobs | Comparable products | Misfire handling modes (fire once, skip, fire all) are the same set as the three missed-occurrence modes |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `SchedulerOrchestrator.HandleRecurringMessageAsync`, `ScheduleRecurringAsync` | `src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs` | Extended and split |
| `IScheduledMessageStore.RescheduleRecurringMessageAsync`, `MarkAsProcessedAsync`, `CancelAsync` | `src/Encina.Messaging/Scheduling/IScheduledMessageStore.cs` | Increment, end and cancel |
| `SchedulingOptions` | `src/Encina.Messaging/Scheduling/SchedulingOptions.cs` | Two new options |
| `SchedulingHealthCheck` | `src/Encina.Messaging/Health/SchedulingHealthCheck.cs` | Overdue recurring messages |
| `RecurringOptions`, `CronosCronParser`, key methods | created by #1222 | Reused |
| `scheduling.message_not_pending` | created by #146 | Reused by pause and resume |
| `SchedulerCycleScenarios` | `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Scheduling/` | End-to-end cycles |

Provider files touched:

| Provider | Message class | Store | Schema or mapping |
|----------|---------------|-------|-------------------|
| ADO-SqlServer, -PostgreSQL, -MySQL | `src/Encina.ADO.<Db>/Scheduling/ScheduledMessage.cs` | `.../ScheduledMessageStoreADO.cs` | `src/Encina.ADO.<Db>/Scripts/004_CreateScheduledMessagesTable.sql`, `000_CreateAllTables.sql` |
| Dapper-SqlServer, -PostgreSQL, -MySQL | `src/Encina.Dapper.<Db>/Scheduling/ScheduledMessage.cs` | `.../ScheduledMessageStoreDapper.cs` | `src/Encina.Dapper.<Db>/Scripts/004_CreateScheduledMessagesTable.sql`, `000_CreateAllTables.sql` |
| EFCore-SqlServer, -PostgreSQL, -MySQL | `src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessage.cs` | `.../ScheduledMessageStoreEF.cs` | `.../ScheduledMessageConfiguration.cs` (shared) |
| MongoDB | `src/Encina.MongoDB/Scheduling/ScheduledMessage.cs` | `.../ScheduledMessageStoreMongoDB.cs` | BsonElement attributes |

The provider factories are unchanged (the orchestrator assigns the properties after `Create`).

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` (`MessagingScheduling`, existing 2300-2399) | **2360-2369** used by #133 (ids 2370-2379 held) | The batch registry holds 2326-2389 for #1222, #146, #150, #133 and #147; the sub-ranges of the siblings are theirs to state. `Encina.Messaging` already has an `AssemblyRanges` entry |
| New range | none | The block 6750-6799 is not needed |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production | 55 | R0 6, R1 6, R2 to R7 24, R8 5, R9 4, R10 3, R11 4, R13 3 |
| Tests and test support | 61 | R0 8, R1 4, R2 to R7 15, R8 2, R9 4, R10 5, R11 7, R12 11, R13 3, R14 2 justifications |
| PublicAPI | 16 | R1 1, R2 to R7 6, R8 2, R9 3, R10 2, R11 1, R13 1 |
| Coverage manifests | 16 | the same pull requests |
| Documentation, changelog | 6 | R0a changelog, R14 changelog, page, README, ADR section, INVENTORY |
| **Total** | **154** | 55 + 61 + 16 + 16 + 6, the same 154 file edits as the per-PR table of the Summary |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no compatibility layers; time from TimeProvider.
- Issue #133 after #1222 (RecurringOptions, parser, key methods) and #146 (scheduling.message_not_pending).

IMPLEMENTATION OVERVIEW:
R0a/R0b: align the due predicate of the six SQL stores with EF Core and MongoDB (ended recurring rows must not run again). R1: enums, RecurringOptions properties, builder, options, error code. R2-R8: six recurrence columns, pause/resume store members, atomic count increment on reschedule, paused rows excluded from due queries, one provider per pull request. R9/R10: interface members, decorator, fake. R11: orchestrator lifecycle (end modes, missed handling, pause/resume), logs 2360-2369, metrics. R12: scenarios. R13: [RecurringSchedule] attribute, opt-in registrar, DI. R14: docs.
Apply the Maintainer Decisions for the cron library, missed-occurrence default, row versus entity, end conditions, attribute shape and the tenant prerequisite.

KEY PATTERNS:
- Implementations first, interface last, so each pull request compiles.
- Reschedule-after-run means one instance never overlaps; no SkipIfPreviousRunning; cross-instance overlap waits for #1251.
- Message ids and error codes only in logs; no key, payload or EncinaError.Message.

REFERENCE FILES:
- src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs
- src/Encina.Dapper.SqlServer/Scheduling/ScheduledMessageStoreDapper.cs
- src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessageConfiguration.cs
- src/Encina.Messaging/Health/SchedulingHealthCheck.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Schedules are written rarely and read by the processor; nothing to cache |
| 2 | OpenTelemetry | ✅ | Phase 5: counters `encina.scheduling.recurring.runs`, `.missed`, `.ended` and pause/resume spans |
| 3 | Structured Logging | ✅ | Phase 5: events 2360-2369 inside the registered range |
| 4 | Health Checks | ❌ | The existing `SchedulingHealthCheck` already reports overdue recurring messages; paused rows are not due |
| 5 | Validation | ✅ | Phases 2 and 7: cron, zone, end options and annotated types validated; invalid input returns Left or fails startup |
| 6 | Resilience | ❌ | No external call; retry and dead letter are the existing scheduler policy |
| 7 | Distributed Locks | ⏭️ | Multi-instance claiming of recurring rows is #1251 (choice 6) |
| 8 | Transactions | ✅ | Phase 3: pause, resume and the increment join the scope transaction like the existing methods (the EF Core `ExecuteUpdateAsync` joins only the context's current transaction, see #146) |
| 9 | Idempotency | ✅ | The attribute registrar upserts by schedule key, so a restart or a second instance changes nothing; re-execution idempotency of dispatch is #735 |
| 10 | Multi-Tenancy | ⏭️ | Defer: #739 (open) is a hard prerequisite of R11 and R13 (choice 12) |
| 11 | Module Isolation | ❌ | SPEC-002 asks no module scoping; `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ⏭️ | Create, pause, resume and cancel are operation-audit events (`IOperationAuditStore`): follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/scheduling-cancel-reschedule-audit.md); #1778 is the entity-change store |

---

## Prerequisites & Dependencies

| Item | Relation | Note |
|------|----------|------|
| #1222 | Must merge first (at least P12) | `RecurringOptions`, parser, key methods, EventIds 2326-2339 |
| #146 | Must merge first (at least S3) | `scheduling.message_not_pending`, the update pattern on the same store files |
| #739 | Hard prerequisite of R11 and R13 | Tenant column (choice 12) |
| #1251 and #718 | Coordinate | Claiming replaces `GetDueMessagesAsync`; the due predicate and the paused filter move into the claim query; it closes the cross-instance overlap |
| #147 | Sibling | Adds a priority column to the same row; each pull request changes the same store files, so merge order is decided at decomposition |

---

## Next Steps

1. Review this plan; the Dilemmas (choices 1 to 7, 9, 11 and 12) are answered by the maintainer when work starts and recorded in `## Maintainer Decisions`.
2. Open the bug issue for the six-store due predicate and ship R0a and R0b first; decompose R1 to R14 after #1222 and #146 merge.
3. Link the plan from #133.
