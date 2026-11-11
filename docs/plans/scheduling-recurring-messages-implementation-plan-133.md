# Implementation Plan: Recurring messages — end conditions, pause and resume, missed occurrences and `[RecurringSchedule]`

> **Issue**: [#133](https://github.com/dlrivada/Encina/issues/133)
> **Type**: Feature
> **Complexity**: High (7 phases, all 10 database providers, six new columns, orchestrator lifecycle)
> **Estimated Scope**: ~1,000 lines of production code + ~1,800 lines of tests, ~70 files in 6 sub-pull requests
> **Family**: scheduling-1 (builds on #1222 and #146; shares the row and the factory draft with #147)

---

## Summary

Recurrence already exists in a basic form, and this plan extends it (all facts re-checked against the tree):

- `IScheduledMessage` has `IsRecurring`, `CronExpression` and `LastExecutedAtUtc`; the next run time is stored in `ScheduledAtUtc`; `SchedulerOrchestrator.ScheduleRecurringAsync(request, cronExpression, ct)` and the private `HandleRecurringMessageAsync` reschedule after each run through `IScheduledMessageStore.RescheduleRecurringMessageAsync`; `CancelAsync(Guid, ct)` removes the schedule.
- `ICronParser` has no implementation today. #1222 ships `CronosCronParser` with the zone-aware signature and the stored `TimeZoneId`, so time zones, 5 and 6 field expressions and the cron library are **not** part of this plan.
- Missing, and delivered here: end conditions (never, after N occurrences, on a date), pause and resume, missed-occurrence handling, the `[RecurringSchedule]` attribute with a startup registrar, fluent options, and metrics.

**A latent bug found while reading the stores.** The six ADO.NET and Dapper stores select due messages with `WHERE (ProcessedAtUtc IS NULL OR IsRecurring = 1) ...` (for example `src/Encina.Dapper.SqlServer/Scheduling/ScheduledMessageStoreDapper.cs`, `src/Encina.ADO.PostgreSQL/Scheduling/ScheduledMessageStoreADO.cs`), while `ScheduledMessageStoreEF.cs` and `ScheduledMessageStoreMongoDB.cs` require `ProcessedAtUtc == null`. When a recurring schedule ends, `HandleRecurringMessageAsync` calls `MarkAsProcessedAsync`; on the six SQL providers that row stays due and runs again at every cycle. End conditions make this reachable on purpose, so the first sub-issue (S0) aligns the six predicates with EF Core and MongoDB and proves it on all 10 providers. A bug issue file is written for it so it can ship before this plan.

**Provider category**: Database (10): ADO.NET x3, Dapper x3, EF Core x3 (SqlServer, PostgreSQL, MySQL), MongoDB.

**Affected packages**: `Encina.Messaging`, the nine SQL provider packages, `Encina.EntityFrameworkCore`, `Encina.MongoDB`, `Encina.OpenTelemetry` (decorator), `Encina.Testing.Fakes`, `Encina.Testing.Bogus`.

**Split** (implementations first, interface last, as in #1222):

| Sub-issue | Content | Files (est.) |
|-----------|---------|:-:|
| S0 | Due-predicate alignment on the 6 SQL stores with regression tests (also the bug issue) | 12 |
| S1 | ADO x3: six columns in the SQL scripts, message and factory, store pause/resume, increment on reschedule, due filter on `PausedAtUtc`, integration tests | 15 |
| S2 | Dapper x3: same | 15 |
| S3 | EF Core (message, configuration, factory, store) and MongoDB (message, factory, store) | 14 |
| S4 | Core: enum, options, builder, interface members, orchestrator lifecycle, `SchedulingOptions`, log and metrics, fakes, decorator | 15 |
| S5 | `[RecurringSchedule]` attribute, registrar hosted service, DI, docs, changelog | 9 |

**Coordination (family scheduling-1)**: #1222 owns `ScheduledMessageDraft`, `RecurringOptions` (this plan adds properties), the parser and EventIds 2326-2339. #146 owns `scheduling.message_not_pending`, which pause and resume reuse. This plan takes EventIds 2360-2379.

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

- This is the cron-library Dilemma of #1222 (Dilemma 2); the issue's own D1 asks the same question and one answer covers both. This plan adds no parser code: `Describe`, `IsValid` and `GetNextOccurrences` of the original proposal are not shipped because no feature here uses them (a validity check is the `Left` of `GetNextOccurrence`, as today).

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
- All three modes ship as `MissedOccurrenceHandling` on `RecurringOptions` (stored per schedule); the default only decides what a schedule without the option does. A run is "missed" when `now - ScheduledAtUtc` exceeds `SchedulingOptions.MissedOccurrenceTolerance` (new, default 5 minutes) on its first attempt. `RunAll` is capped by `SchedulingOptions.MaxCatchUpOccurrences` (new, default 100) per cycle so one schedule cannot monopolize a batch.

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
- New columns, all nullable or defaulted so the `CREATE TABLE` scripts are edited in place (AGENTS.md section 1, no migrations): `RecurrenceEndMode` (int, default 0), `MaxOccurrences` (int null), `OccurrenceCount` (int, default 0), `RecurrenceEndsAtUtc` (datetime null), `PausedAtUtc` (datetime null), `MissedOccurrenceHandling` (int, default 0). The original names `CurrentOccurrence` and `EndsAtUtc` follow the naming rules instead (`AtUtc`, `...Count`).

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
- Rules: `AfterCount` needs `MaxOccurrences >= 1`; `OnDate` needs `RecurrenceEndsAtUtc` in the future; otherwise `Left(scheduling.invalid_recurrence_options)`. The last run ends the schedule through `MarkAsProcessedAsync` (the row is then excluded from due queries by the S0 predicate).

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Shape of `[RecurringSchedule]`</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Attribute on the request type; an opt-in registrar creates the request with its public parameterless constructor and upserts it** | Declarative; the schedule key (the type's full name) makes registration idempotent across restarts and instances | Only requests constructible without data |
| **B) Attribute plus an `IRecurringRequestFactory<T>` the application registers** | Requests that need data or services | More ceremony for the simple case |
| **C) No attribute; the application calls `ScheduleRecurringAsync` with a key at startup** | Nothing new to maintain | The acceptance criterion of the issue asks for the attribute |

### Chosen Option: **A — attribute with a parameterless request** (recommended, pending the maintainer)

### Rationale

- A wins for health-check-style commands (the sample of the issue); B wins when the request carries data from configuration; C when the team prefers explicit startup code.
- The registrar is opt-in (`AddEncinaRecurringSchedules(params Assembly[])`, pay-for-what-you-use), fails closed at startup when an annotated type lacks a parameterless constructor or has an invalid expression, and never deletes a schedule whose attribute was removed (documented).

**Class:** Dilemma

</details>

<details>
<summary><strong>6. `SkipIfPreviousRunning` is not shipped</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) No option; overlap cannot happen by construction** | Every line serves a current purpose | None |
| **B) Add the flag from the original proposal** | Matches the sketch | Dead configuration: the next run is scheduled only after the dispatch completes |

### Chosen Option: **A — not shipped**

### Rationale

- `HandleRecurringMessageAsync` reschedules after the dispatch returns, so one instance never overlaps two runs of the same schedule; across instances the row claiming of #1251 prevents it.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (no unused code), #1251 (row claiming)

</details>

<details>
<summary><strong>7. No `CancelRecurringAsync` alias; pause and resume are by id</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reuse `CancelAsync(Guid)` and `CancelByKeyAsync` (#1222); add `PauseRecurringAsync(Guid)` and `ResumeRecurringAsync(Guid)`** | One cancel path; the id of a keyed schedule is returned by the upsert | A caller holding only a key looks the id up first |
| **B) Add `CancelRecurringAsync` and key variants of pause and resume** | Symmetric API | Duplicate cancel; twice the store methods on 10 providers |

### Chosen Option: **A — reuse cancel, id-based pause and resume**

### Rationale

- A second name for the same operation is a compatibility alias, which AGENTS.md section 3 forbids; resume recomputes the next run from the current time so it never floods.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (no aliases), #1222 (cancel by key)

</details>

---

## Implementation Phases

### Phase 1: Core model, options and error codes (S4)

> **Goal**: The shared types of the recurrence feature.

<details>
<summary><strong>Tasks</strong></summary>

1. `RecurrenceEndMode.cs` (`Never`, `AfterCount`, `OnDate`) and `MissedOccurrenceHandling.cs` (`RunOnce`, `Skip`, `RunAll`) in `src/Encina.Messaging/Scheduling/`.
2. `RecurringOptions` (created by #1222: `ScheduleKey`, `TimeZoneId`) gains `EndMode`, `MaxOccurrences`, `EndsAtUtc`, `MissedHandling`; `RecurringOptionsBuilder.cs` with `InTimeZone`, `WithKey`, `EndAfterOccurrences`, `EndOnDate`, `OnMissedOccurrence`; `ScheduleRecurringAsync(request, cron, Action<RecurringOptionsBuilder>, ct)` overload.
3. `ScheduledMessageDraft` (#1222) gains the recurrence properties; `IScheduledMessage` gains `RecurrenceEndMode`, `MaxOccurrences`, `OccurrenceCount`, `RecurrenceEndsAtUtc`, `PausedAtUtc`, `MissedOccurrenceHandling` (interface edit last).
4. `SchedulingOptions`: `MissedOccurrenceTolerance` (TimeSpan, 5 minutes) and `MaxCatchUpOccurrences` (int, 100).
5. `SchedulingErrorCodes.InvalidRecurrenceOptions = "scheduling.invalid_recurrence_options"`; `scheduling.message_not_pending` comes from #146.
6. `PublicAPI.Unshipped.txt` (a `mechanical-fixer` writes the lines).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
- Issue #133. #1222 and #146 have merged: ScheduledMessageDraft, RecurringOptions, CronosCronParser and scheduling.message_not_pending exist.

TASK:
Add the enums, the options properties, the builder, the SchedulingOptions properties and the error code; validate option combinations in one method returning Either.

KEY RULES:
- Either<EncinaError, T>; XML docs on every public member; UTC timestamps carry the AtUtc suffix.
- Interface members are added only in the sub-issue where all providers already implement them.

REFERENCE FILES:
- src/Encina.Messaging/Scheduling/SchedulingOptions.cs
- src/Encina.Messaging/Scheduling/IScheduledMessage.cs
- src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs
```

</details>

### Phase 2: Providers — columns, due predicate, pause and resume (S0 to S3)

> **Goal**: The six columns and the store behavior on all 10 providers.

<details>
<summary><strong>Tasks</strong></summary>

1. **S0**: in the six SQL stores, replace `(ProcessedAtUtc IS NULL OR IsRecurring = 1)` by `ProcessedAtUtc IS NULL` (`ScheduledMessageStoreADO.cs` and `ScheduledMessageStoreDapper.cs` of SqlServer, PostgreSQL, MySQL). Integration test on all 10 providers: a recurring message ended through `MarkAsProcessedAsync` is not returned by `GetDueMessagesAsync` again.
2. **S1 to S3**: add the six columns to `Scripts/004_CreateScheduledMessagesTable.sql` (ADO and Dapper, three databases each), `ScheduledMessageConfiguration.cs` (EF Core, enum conversions) and the MongoDB message class (`[BsonElement]`); extend the `AddAsync` column lists and the readers/mappers; the due filter adds `PausedAtUtc IS NULL`.
3. `RescheduleRecurringMessageAsync` also sets `LastExecutedAtUtc = now` and `OccurrenceCount = OccurrenceCount + 1` in the same statement (an atomic increment, never a read-modify-write).
4. New concrete store members: `PauseRecurringAsync(Guid messageId, DateTime pausedAtUtc, ct)` (`WHERE Id = @id AND IsRecurring AND ProcessedAtUtc IS NULL AND PausedAtUtc IS NULL`) and `ResumeRecurringAsync(Guid messageId, DateTime nextScheduledAtUtc, ct)` (clears `PausedAtUtc`, sets `ScheduledAtUtc`, resets retry fields, `WHERE PausedAtUtc IS NOT NULL`); zero affected rows returns `Left(scheduling.message_not_pending)`.
5. All SQL calls are asynchronous with a `CancellationToken`; EF Core uses `ExecuteUpdateAsync`, MongoDB `UpdateOneAsync` with `$inc` for the count.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
- Stores: src/Encina.ADO.<Db>/Scheduling/ScheduledMessageStoreADO.cs, src/Encina.Dapper.<Db>/Scheduling/ScheduledMessageStoreDapper.cs, src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessageStoreEF.cs, src/Encina.MongoDB/Scheduling/ScheduledMessageStoreMongoDB.cs.
- SQL differs per database: SQL Server bit and TOP, PostgreSQL lower-case identifiers and LIMIT, MySQL 0/1.

TASK:
Implement the sub-issue you are in (S0, S1, S2 or S3) for its providers.

KEY RULES:
- Never Open() or other synchronous database calls.
- The same observable behavior on all 10 providers; integration tests for each new behavior, shared [Collection("<Family>-<Db>")] fixtures.

REFERENCE FILES:
- src/Encina.Dapper.SqlServer/Scheduling/ScheduledMessageStoreDapper.cs
- src/Encina.ADO.PostgreSQL/Scripts/004_CreateScheduledMessagesTable.sql
- tests/Encina.IntegrationTests/Dapper/SqlServer/Scheduling/ScheduledMessageStoreDapperTests.cs
```

</details>

### Phase 3: Orchestrator lifecycle (S4)

<details>
<summary><strong>Tasks</strong></summary>

1. `ScheduleRecurringAsync` with `RecurringOptions`: validates the options (Phase 1), computes the first run through `ICronParser` in the stored zone, writes the recurrence columns through the draft.
2. `HandleRecurringMessageAsync` (private) is split into small methods: `EndRecurringAsync` (ends when the count or date limit is reached or the parser returns no next run), `RescheduleRecurringAsync` (stores the next run, increments the count). Each new method stays under cyclomatic complexity 10 (CRAP gate).
3. Missed occurrences: a pre-dispatch check on the first attempt applies the stored `MissedOccurrenceHandling`: `RunOnce` dispatches; `Skip` reschedules without dispatching; `RunAll` dispatches once per missed occurrence up to `MaxCatchUpOccurrences`, stopping at the first failure, which follows the normal retry path.
4. `PauseRecurringAsync(Guid, ct)` and `ResumeRecurringAsync(Guid, ct)` (resume computes the next run from the current time in the stored zone and calls the store).
5. `CompiledExpressionScheduledMessageDispatcher` and `ScheduledMessageProcessor` need no change.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
- SchedulerOrchestrator is a sealed class. Time comes from the injected TimeProvider; the cron parser is the optional ICronParser dependency.
- Errors from the store must never be swallowed: a Left fails the operation (logged by code) and the message keeps its state.

TASK:
Implement the recurrence lifecycle of the plan with small private methods.

KEY RULES:
- Log message ids and error codes only; never the payload or the schedule key.
- Unit tests with FakeTimeProvider for each end mode and each missed-handling mode.

REFERENCE FILES:
- src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs
- tests/Encina.UnitTests/Messaging/Scheduling/SchedulerOrchestratorScheduleRecurringTests.cs
```

</details>

### Phase 4: Attribute, registrar and DI (S5)

<details>
<summary><strong>Tasks</strong></summary>

1. `RecurringScheduleAttribute.cs` (`CronExpression`, `TimeZone`, optional `EndMode` data) in `src/Encina.Messaging/Scheduling/`.
2. `RecurringScheduleRegistrar.cs` (internal `IHostedService`): scans the assemblies given to `AddEncinaRecurringSchedules(params Assembly[])`, creates each annotated request with its parameterless constructor, upserts it with `ScheduleKey = type full name`; an annotated type without such a constructor, an invalid expression or a failed upsert fails startup with the error code (fail closed).
3. DI test with `ValidateOnBuild` and `ValidateScopes`: the registrar is absent unless the method is called and resolves `SchedulerOrchestrator` through a created scope.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
- The upsert by schedule key (#1222) makes registration idempotent across restarts and across instances starting together (the unique index resolves the race).

TASK:
Add the attribute, the opt-in registrar and the registration method.

KEY RULES:
- Opt-in only; nothing is scanned unless AddEncinaRecurringSchedules is called.
- A hosted service never lets an exception escape silently: startup failure is explicit.

REFERENCE FILES:
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs
- src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs
```

</details>

### Phase 5: Cross-cutting integration and observability (S4)

<details>
<summary><strong>Tasks</strong></summary>

1. `SchedulingRecurrenceLog.cs` in `src/Encina.Messaging/Diagnostics/`: events 2360-2369 inside `EventIdRanges.MessagingScheduling`: `RecurringPaused` (2360), `RecurringResumed`, `MissedOccurrenceRunOnce`, `MissedOccurrenceSkipped`, `MissedOccurrencesCaughtUp`, `CatchUpCapped`, `RecurringEndedByCount`, `RecurringEndedByDate`, `RecurringScheduleRegistered`, `RecurringScheduleRegistrationFailed` (2369). Arguments are message ids, counts and error codes.
2. `SchedulingRecurrenceMetrics.cs` on the `Encina` meter: `encina.scheduling.recurring.runs`, `encina.scheduling.recurring.missed` (tag `handling`), `encina.scheduling.recurring.ended` (tag `reason`: `count`, `date`, `no_next`).
3. Spans `encina.scheduling.pause` and `encina.scheduling.resume`; `InstrumentedScheduledMessageStore` wraps the two store members; `FakeScheduledMessageStore` implements them; Bogus faker and `ScheduledMessageBuilder` set the new properties.
4. The existing `SchedulingHealthCheck` already reports overdue messages, recurring ones included, and paused rows are not due, so no new health check is needed.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
- EventIds 2360-2379 are reserved for #133 inside MessagingScheduling (2300-2399); other family reservations: #1222 2326-2339, #146 2340-2349, #150 2350-2359, #147 2380-2389.

TASK:
Add the log class, the metrics class, the spans and the decorator and fake members.

KEY RULES:
- EncinaError.Message never reaches logs or tags; use the error code.
- Pack ids sequentially from 2360; XML docs name the range.

REFERENCE FILES:
- src/Encina.Messaging/Diagnostics/SchedulingStoreLog.cs
- src/Encina.Messaging/Diagnostics/SchedulingProcessorMetrics.cs
- src/Encina.OpenTelemetry/MessagingStores/InstrumentedScheduledMessageStore.cs
```

</details>

### Phase 6: Testing

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit**: each end mode, each missed mode, pause/resume, option validation, the builder; the registrar with a fake orchestrator and real attribute scanning.
2. **Guard**: `ArgumentNullException` on every new public method; store guard classes for the nine SQL providers and EF Core.
3. **Contract**: `IScheduledMessageStore` pause/resume/increment contract against `FakeScheduledMessageStore`; factory contract across the 10 factories.
4. **Property**: next-occurrence monotonicity over repeated reschedules; the count never exceeds `MaxOccurrences`; an `OnDate` schedule never runs after its end date (FsCheck with `FakeTimeProvider`).
5. **Integration (10 providers, real databases)**: ended recurring message not due again (S0), pause hides and resume restores a message, `AfterCount` ends after N runs through `SchedulerOrchestrator`, `Skip` and `RunOnce` after a simulated downtime, increment atomicity under two concurrent reschedules.
6. **Load**: `.md` justification (recurring runs are sequential per row); **benchmark**: `.md` justification for cron next-occurrence unless the maintainer wants it measured (the parser is #1222's).
7. **Obligations**: `Scheduling/SchedulerOrchestrator.cs` keeps unit 90 and guard 30 (targets never go down) and the new methods must measure CRAP at most 10; new files `RecurringOptionsBuilder.cs` unit 95 and guard 70, `RecurringScheduleRegistrar.cs` unit 85 and guard 60, `RecurringScheduleAttribute.cs` unit 80; each provider store keeps its targets. Measure per flag with `dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage" --results-directory artifacts\coverage\<Flag>Tests` and `dotnet run --file .github/scripts/coverage-report.cs -- --input artifacts/coverage --output artifacts/coverage-report`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
- Tests execute real package code; Shouldly via Encina.Testing.Shouldly; FakeTimeProvider; shared database collections.

TASK:
Write the tests listed in the plan for the sub-issue you are in and compare per-file coverage with the manifest targets.

KEY RULES:
- No Thread.Sleep; no test swallows an exception thrown by the code it drives.
- Integration tests are never replaced by a .md justification.

REFERENCE FILES:
- tests/Encina.UnitTests/Messaging/Scheduling/SchedulerOrchestratorTests.cs
- tests/Encina.TestInfrastructure/EFCore/ScheduledMessageStoreEFTestsBase.cs
- tests/Encina.PropertyTests/Messaging/Scheduling/
```

</details>

### Phase 7: Documentation and finalization

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs with examples on `ScheduleRecurringAsync` with options, `PauseRecurringAsync`, `ResumeRecurringAsync` and the attribute.
2. `changelog.d/133-scheduling-recurring-messages.added.md` and `changelog.d/133-scheduling-due-predicate.fixed.md` (a `mechanical-fixer` writes them).
3. `docs/features/scheduling.md` recurring section (end modes, pause/resume, missed handling, the attribute), `src/Encina.Messaging/README.md`, and the ADR section on missed-occurrence semantics added to the cron ADR of #1222 (ADR number assigned when the ADR pull request opens), through the `docs-writer`.
4. `PublicAPI.Unshipped.txt`; `docs/INVENTORY.md` for the new files.
5. `dotnet build --configuration Release` with 0 warnings; all flags at target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
CONTEXT:
- Documentation pages go to the docs-writer; changelog and PublicAPI lines to the mechanical-fixer.

TASK:
Finish the XML docs and delegate pages, changelog and PublicAPI with the exact decided text.

KEY RULES:
- Never edit CHANGELOG.md; never type coverage figures by hand.

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
| SPEC-002 REQ-033 | Recurring cron with stored zone, shipped parser | Delivered by #1222; this plan builds on it |
| ADR-050 | Scheduling dependencies | Default `ICronParser` (#1222) when recurring is on; claiming (#1251); dispatch idempotency (#735) |
| ADR-018 | Cross-cutting integration | Matrix below |
| ADR-021 | EventIds | Ids 2360-2379 inside `MessagingScheduling` |
| MassTransit recurring jobs, Quartz.NET triggers, Hangfire recurring jobs | Comparable products | Misfire handling modes (fire once, skip, fire all) are the same set as the three missed-occurrence modes |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `SchedulerOrchestrator.HandleRecurringMessageAsync`, `ScheduleRecurringAsync` | `src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs` | Extended and split |
| `IScheduledMessageStore.RescheduleRecurringMessageAsync`, `MarkAsProcessedAsync`, `CancelAsync` | `src/Encina.Messaging/Scheduling/IScheduledMessageStore.cs` | Increment, end and cancel |
| `SchedulingOptions` | `src/Encina.Messaging/Scheduling/SchedulingOptions.cs` | Two new options |
| `SchedulingHealthCheck` | `src/Encina.Messaging/Health/SchedulingHealthCheck.cs` | Overdue recurring messages |
| `ScheduledMessageDraft`, `RecurringOptions`, `CronosCronParser`, key methods | created by #1222 | Reused |
| `scheduling.message_not_pending` | created by #146 | Reused by pause and resume |
| `SchedulerCycleScenarios` | `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Scheduling/` | End-to-end cycles |

Provider files touched (first draft by the local model, reviewed against the tree):

| Provider | Message class | Factory | Store | Schema or mapping |
|----------|---------------|---------|-------|-------------------|
| ADO-SqlServer, -PostgreSQL, -MySQL | `src/Encina.ADO.<Db>/Scheduling/ScheduledMessage.cs` | `.../ScheduledMessageFactory.cs` | `.../ScheduledMessageStoreADO.cs` | `src/Encina.ADO.<Db>/Scripts/004_CreateScheduledMessagesTable.sql` |
| Dapper-SqlServer, -PostgreSQL, -MySQL | `src/Encina.Dapper.<Db>/Scheduling/ScheduledMessage.cs` | `.../ScheduledMessageFactory.cs` | `.../ScheduledMessageStoreDapper.cs` | `src/Encina.Dapper.<Db>/Scripts/004_CreateScheduledMessagesTable.sql` |
| EFCore-SqlServer, -PostgreSQL, -MySQL | `src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessage.cs` | `.../ScheduledMessageFactory.cs` | `.../ScheduledMessageStoreEF.cs` | `.../ScheduledMessageConfiguration.cs` (shared) |
| MongoDB | `src/Encina.MongoDB/Scheduling/ScheduledMessage.cs` | `.../ScheduledMessageFactory.cs` | `.../ScheduledMessageStoreMongoDB.cs` | BsonElement attributes |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` (`MessagingScheduling`, existing 2300-2399) | **2360-2379** for #133 (uses 2360-2369) | Used: 2300-2306, 2320-2325; reserved: #1222 2326-2339, #146 2340-2349, #150 2350-2359, #147 2380-2389 |
| New range | none | The block 6750-6799 is not needed |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Core `Encina.Messaging` | ~14 | enums, options, builder, attribute, registrar, orchestrator, logs, metrics |
| SQL provider stores, scripts, messages, factories (9 providers) | ~27 | |
| EF Core and MongoDB | ~7 | |
| Decorator, fakes, builders | ~4 | |
| Tests | ~38 | unit, guard, contract, property, integration |
| Documentation and changelog | ~6 | |
| **Total** | **~96 touched, ~70 net in 6 pull requests** | |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no compatibility layers; time from TimeProvider.
- Issue #133 after #1222 (draft record, key methods, parser, RecurringOptions) and #146 (scheduling.message_not_pending).

IMPLEMENTATION OVERVIEW:
S0: align the due predicate of the six SQL stores with EF Core and MongoDB (ended recurring rows must not run again).
S1/S2/S3: six recurrence columns, pause/resume store members, atomic count increment on reschedule, paused rows excluded from due queries.
S4: enums, RecurringOptions properties, builder, interface members, orchestrator lifecycle (end modes, missed handling, pause/resume), options, logs 2360-2369, metrics.
S5: [RecurringSchedule] attribute, opt-in registrar, DI, docs.
Apply the Maintainer Decisions for the cron library, missed-occurrence default, row versus entity, end conditions and attribute shape.

KEY PATTERNS:
- Implementations first, interface last.
- Reschedule-after-run means overlap cannot happen; no SkipIfPreviousRunning.
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
| 5 | Validation | ✅ | Phases 1 and 4: cron, zone, end options and annotated types validated; invalid input returns Left or fails startup |
| 6 | Resilience | ❌ | No external call; retry and dead letter are the existing scheduler policy |
| 7 | Distributed Locks | ⏭️ | Multi-instance claiming of recurring rows is #1251 |
| 8 | Transactions | ✅ | Phase 2: pause, resume and the increment join the scope transaction like the existing methods |
| 9 | Idempotency | ✅ | The attribute registrar upserts by schedule key, so a restart or a second instance changes nothing; re-execution idempotency of dispatch is #735 |
| 10 | Multi-Tenancy | ⏭️ | The tenant column and filters are #739; the attribute registrar then needs a tenant policy decided there |
| 11 | Module Isolation | ❌ | SPEC-002 asks no module scoping; `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ⏭️ | Create, pause and cancel as audit events depend on the audit stores (#1778) |

---

## Prerequisites & Dependencies

| Item | Relation | Note |
|------|----------|------|
| #1222 | Must merge first | Draft record, `RecurringOptions`, parser, key methods, EventIds 2326-2339 |
| #146 | Must merge first | `scheduling.message_not_pending` |
| #1251 and #718 | Coordinate | Claiming replaces `GetDueMessagesAsync`; the due predicate and the paused filter move into the claim query |
| #147 | Sibling | Adds a priority column to the same row; the draft and factories change once each |
| #739 | Later | Tenant column |

---

## Next Steps

1. Review this plan; the five Dilemmas (cron library, missed-occurrence default, row versus entity, end conditions, attribute shape) are answered by the maintainer when work starts and recorded in `## Maintainer Decisions`.
2. Open the bug issue for the six-store due predicate and ship S0 first; decompose S1 to S5 after #1222 and #146 merge.
3. Link the plan from #133.
