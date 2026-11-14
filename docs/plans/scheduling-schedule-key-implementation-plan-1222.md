# Implementation Plan: Scheduling minimal slices — schedule key, time-zone-aware cron, shipped `ICronParser`, orphan `IMessageScheduler`

> **Issue**: [#1222](https://github.com/dlrivada/Encina/issues/1222)
> **Type**: Feature (SPEC-002 P-20, REQ-033, AC-033, scenario S4; priority P0)
> **Complexity**: High (10 phases, all 10 database providers, three new members on `IScheduledMessageStore`, a new signature of `ICronParser`)
> **Estimated Scope**: about 900 lines of production code + about 2,400 lines of tests, documentation and manifests; 147 file edits in 14 pull requests that each compile on their own, none over 700 changed lines
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

**Affected packages**: `Encina.Messaging`, `Encina.ADO.SqlServer`, `Encina.ADO.PostgreSQL`, `Encina.ADO.MySQL`, `Encina.Dapper.SqlServer`, `Encina.Dapper.PostgreSQL`, `Encina.Dapper.MySQL`, `Encina.EntityFrameworkCore`, `Encina.MongoDB`, `Encina.OpenTelemetry` (the `InstrumentedScheduledMessageStore` decorator), `Encina.Testing.Fakes`, `Encina.Testing.Bogus`, `Encina.Testing.FsCheck`, plus `tests/Encina.TestInfrastructure` and the benchmark projects.

Facts verified on `origin/main` (2026-11-13):

- `ICronParser` (line 616), `IScheduledMessageFactory` (line 587) and `SchedulingErrorCodes` (line 630) are declared in `SchedulerOrchestrator.cs`. Four test files use `ICronParser` and break with a new signature: `SchedulerOrchestratorGuardTests`, `SchedulerOrchestratorTests`, `SchedulerOrchestratorScheduleRecurringTests`, `SchedulerCycleScenarios`.
- `IScheduledMessage` has 13 implementers: the 8 provider `ScheduledMessage` classes, `FakeScheduledMessage` (Fakes), the nested `FakeScheduledMessage` of `ScheduledMessageFaker` (Bogus), `ArbitraryScheduledMessage` (FsCheck) and `BenchmarkScheduledMessage` in the ADO and Dapper benchmark projects. `IScheduledMessageStore` has 10 implementers (8 providers, `InstrumentedScheduledMessageStore`, `FakeScheduledMessageStore`). `IScheduledMessageFactory` has 8 implementers (the provider factories) and 8 factory unit-test classes.
- Cronos is not in `Directory.Packages.props`. `IMessageScheduler` is referenced only by its own file, its contract test, the `Encina.EntityFrameworkCore` coverage manifest, `.github/honesty-audit-baseline.json` (line 29) and two snippets of `src/Encina.Messaging/README.md` (lines 192 and 513, verified).
- The integration test schemas for the scheduled-message table live in `tests/Encina.TestInfrastructure/Schemas` (SqlServer and MySQL define it there) and in the two benchmark schema builders, not only in the `Scripts/004_*.sql` files.
- ADR 062 (error category contract) is merged (PR #3599): each new error code below is created with `EncinaErrors.Create(code, ErrorCategory, message)`: `scheduling.invalid_time_zone` and `scheduling.invalid_schedule_key` are `Validation` (R3), `scheduling.schedule_key_not_found` is `NotFound`.

**Why it does not fit one pull request, and the split.** Adding members to `IScheduledMessage` and `IScheduledMessageStore` breaks their 13 and 10 implementers at once, and a new `ICronParser` signature breaks four test files. The pull requests therefore add the provider-side members as concrete members first and the interface members last, so every pull request compiles on its own and stays within 15 files and 1,500 lines (AGENTS.md section 11). The counts include tests, manifests, PublicAPI files and the existing callers of a changed signature.

| PR | Content | Files | Lines (est.) |
| --- | --- | ---: | ---: |
| P1 | `CronosCronParser`, zone-aware `ICronParser`, `SchedulingTimeZones`, package reference, the four existing test callers | 14 | 450 |
| P2 | `ScheduleOptions`, `RecurringOptions`, `ScheduleKeyValidator`, error codes, DI registration of the parser | 9 | 300 |
| P3 | ADO.SqlServer: columns, unique index, three key methods (concrete) | 9 | 300 |
| P4 | Dapper.SqlServer | 8 | 250 |
| P5 | ADO.PostgreSQL | 9 | 300 |
| P6 | Dapper.PostgreSQL | 8 | 250 |
| P7 | ADO.MySQL | 9 | 300 |
| P8 | Dapper.MySQL | 8 | 250 |
| P9 | EF Core and MongoDB | 12 | 500 |
| P10 | `IScheduledMessage.ScheduleKey` and `TimeZoneId` and their 5 non-provider implementers | 14 | 300 |
| P11 | The three key methods on `IScheduledMessageStore`, decorator and fake | 12 | 400 |
| P12 | Orchestrator methods, log class, metrics, spans | 12 | 700 |
| P13 | Scenario S4 on the 10 providers | 11 | 500 |
| P14 | Removal of `IMessageScheduler`, documentation, ADR, changelog | 12 | 300 |
| **Total** | | **147** | **about 4,800** |

Order: P1, P2, then P3 to P9 (an ADO.NET pull request before the Dapper one of the same database, because the shared test schema changes there), then P10, P11, P12 (after #739, choice 11), P13, P14. #146, #150, #133 and #147 follow the same additive-first order and start after P12 of this plan.

**Story shared with the sibling plans** (who creates which type or code, in which pull request):

| Symbol | Created by | In |
| --- | --- | --- |
| `ICronParser` zone-aware signature, `CronosCronParser`, `SchedulingTimeZones` | #1222 | P1 |
| `SchedulingErrorCodes.InvalidTimeZone`, `InvalidScheduleKey`, `ScheduleKeyNotFound`; `ScheduleOptions`, `RecurringOptions` | #1222 | P1 (`InvalidTimeZone`), P2 (the rest) |
| `IScheduledMessage.ScheduleKey` and `TimeZoneId`, the three key methods on `IScheduledMessageStore` | #1222 | P10, P11 |
| `SchedulingErrorCodes.MessageNotPending` | #146 | its first pull request |
| `SchedulingErrorCodes.InvalidRecurrenceOptions` and the recurrence properties | #133 | its own pull requests (in `SchedulerOrchestrator.cs`, where the class is declared) |
| `SchedulingScheduleLog` (ids 2326-2331), `SchedulingScheduleMetrics` | #1222 | P12 |

Each sibling plan creates its own log class for its own ids in the first pull request that logs; #1222 does not create classes for them.

---

## Design Choices

<details>
<summary><strong>1. Where the default cron parser lives (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `CronosCronParser` in `Encina.Messaging`, registered with `TryAdd` by the scheduling registration when `EnableRecurringMessages` is on** | Works out of the box; one default, as ADR-050 requires; the application can still register its own parser first | `Encina.Messaging` takes the cron library as a dependency, for every consumer |
| **B) A satellite package (for example `Encina.Messaging.Cron`) that registers the default parser when the application adds it** | `Encina.Messaging` stays free of the dependency; ADR-050 only requires that a default is registered with `TryAdd`, not where it lives | Recurring messages stay broken until the package is added |

### Chosen Option: **A — default parser in `Encina.Messaging`** (recommended, pending the maintainer)

### Rationale

- ADR-050 (scheduling row) lists "a default `ICronParser` (#1222)" as a required dependency, conditional on `EnableRecurringMessages`, registered with `TryAdd`; both options satisfy it.
- Option A wins when out-of-the-box behavior matters (the reason the issue exists); option B wins when the dependency footprint of `Encina.Messaging` matters more. The answer is tied to choice 2 (which library).
- `RegisterScheduling` in `src/Encina.Messaging/MessagingServiceCollectionExtensions.cs` already registers the retry policy and dispatcher with `TryAdd`; the parser joins them inside the same `if (schedulingOptions.EnableRecurringMessages)` branch.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Which cron library backs the default parser (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Cronos (HangfireIO, MIT)** | Tested; parses 5 and 6 fields (seconds) through a format flag; evaluates the next occurrence in a `TimeZoneInfo` and handles DST gaps and overlaps natively | A new package dependency |
| **B) NCrontab** | Small | Weaker time zone and DST handling; Encina would own the zone logic and its DST bugs |
| **C) In-tree parser** | No dependency | Encina owns every cron edge case and every DST bug; the largest implementation |

### Chosen Option: **A — Cronos** (recommended, pending the maintainer)

### Rationale

- Option A wins for correctness, which is the reason the issue exists (silent DST drift is the stated motivation). Option B wins only when the dependency footprint matters more than DST correctness; option C only if no library is acceptable.
- This is the same question as the cron-library Dilemma of #133 (D1); one answer covers both. Cronos is not in `Directory.Packages.props` today, so option A adds a `PackageVersion` pinned at implementation time. The ADR that records the answer is also the home of the DST semantics of #150 (ADR number assigned when the ADR pull request opens).

**Class:** Dilemma

</details>

<details>
<summary><strong>3. How the time zone reaches the parser (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ICronParser.GetNextOccurrence(string cronExpression, DateTime afterUtc, string? timeZoneId)` replaces the two-argument method** | The parser that understands zones does the work natively; one place for DST | Every implementation, including test fakes, takes the third parameter (four existing test files change in P1) |
| **B) The orchestrator converts to the zone and back around a UTC-only parser** | The interface stays as it is | DST gap and overlap are re-implemented outside the parser; two places for time logic |

### Chosen Option: **A — zone parameter on the parser** (recommended, pending the maintainer)

### Rationale

- The interface has one method and one production caller (`SchedulerOrchestrator`, two call sites) plus test doubles; AGENTS.md section 1 (pre-1.0) rules out keeping the old signature next to the new one.
- A null `timeZoneId` means UTC, today's behavior (Trivial, no wrong answer). An unknown id returns `Left(scheduling.invalid_time_zone)`; a bad expression keeps `scheduling.invalid_cron_expression`.
- This is the parser-contract Dilemma of #150 (D3). Option B wins only when the smallest interface change matters more than keeping DST logic in one place.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Schedule key uniqueness and what an upsert replaces (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The key is unique over all rows; an upsert on an existing key reuses that row (same `Id`, `CreatedAtUtc`) and resets its processing state** | One plain unique index works on every provider, including MySQL, which has no partial index; native upserts (`ON CONFLICT`, `ON DUPLICATE KEY`, `MERGE`, Mongo `upsert`) apply directly | The processed state of the previous run for that key is overwritten |
| **B) The key is unique only among unprocessed rows (partial index)** | Keeps the history of earlier runs of a key | MySQL has no partial index (needs a generated column), the upsert has two shapes, and a processed row for key K and a pending one coexist |

### Chosen Option: **A — unique over all rows, upsert reuses the row** (recommended, pending the maintainer)

### Rationale

- `CancelAsync` deletes the row on every provider today (`DELETE FROM ... WHERE Id = @MessageId` in `ScheduledMessageStoreDapper.cs`, `Remove` in `ScheduledMessageStoreEF.cs`, `DeleteOneInSessionAsync` in the MongoDB store), so cancelling by key frees the key, and option A matches that model.
- On an update the statement sets `RequestType`, `Content`, `ScheduledAtUtc`, `IsRecurring`, `CronExpression`, `TimeZoneId` and clears `ProcessedAtUtc`, `ErrorMessage`, `RetryCount`, `NextRetryAtUtc`; it keeps `Id` and `CreatedAtUtc`. The store returns the stored `Id` as `Either<EncinaError, Guid>`.
- Row being processed: until row claiming exists (#1251) the processor does not claim rows. An upsert that resets a row while the processor is dispatching it is lost, because `MarkAsProcessed` then ends the message. This plan accepts that window and documents it; #1251 closes it. Option B does not close it either.
- Option B wins when an application must keep one row per run of the same key (audit of earlier runs); that need is served by the dead letter and audit stores, not by this table.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. How the key and zone reach a new message (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IScheduledMessageFactory.Create(ScheduledMessageDraft draft)` replaces the seven-argument method** | One signature for this slice and for the columns #133, #147 and #739 add later | The switch is atomic: the factory interface, 8 factories, 8 factory unit-test classes, the orchestrator and the test callers change in one pull request (about 30 files, over the 15-file limit; it would need the `size-exception-approved` label) |
| **B) Keep positional parameters and append `scheduleKey` and `timeZoneId`** | Smaller diff than A | The signature grows with every later column; the same atomic switch of 8 factories and their tests |
| **C) Settable `ScheduleKey` and `TimeZoneId` on `IScheduledMessage` (same style as `RetryCount`); the orchestrator assigns them after `Create`** | No factory change at all; every pull request compiles on its own; later columns of #133 and #147 are additive the same way | The factory returns a message whose key is assigned one line later |

### Chosen Option: **C — settable properties, assigned by the orchestrator** (recommended, pending the maintainer)

### Rationale

- The review of this plan found that A and B cannot be split so that each step compiles within 15 files; C needs no factory change.
- If the maintainer picks A, its atomic factory switch needs the size exception; the rest of the split is unchanged, and #133, #146 and #147 then build their rows through `ScheduledMessageDraft`.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. `IMessageScheduler` is removed, not implemented</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Delete `src/Encina.EntityFrameworkCore/Scheduling/IMessageScheduler.cs` and its contract test** | No unused public API; the orchestrator is the single scheduling surface | The two README snippets, the coverage manifest entry and the honesty baseline line must change |
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

<details>
<summary><strong>8. Key rules and column sizes (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) 1 to 256 characters (UTF-16 code units), not whitespace-only, no control characters, stored exactly as supplied; `ScheduleKey` column 256, `TimeZoneId` column 100 (the longest IANA id is far shorter)** | Fits unique index limits on all providers (SQL Server 900 bytes: 512 for `NVARCHAR(256)`; MySQL `utf8mb4` 1024 of 3072 bytes); one rule for every provider | A producer with a longer natural key must hash it |
| **B) 128 or 450 characters** | Other limits | Arbitrary |
| **C) Trim and normalise the key** | Tolerates stray spaces | The key is no longer returned unchanged; two producers differing by spaces silently share a schedule |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

- The rule returns `Left(scheduling.invalid_schedule_key)` (category `Validation`); validation never throws (ADR-001, ADR-006).

**Class:** Dilemma

</details>

<details>
<summary><strong>9. Key comparison across providers (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Ordinal (case-sensitive) on every provider: a binary collation on the `ScheduleKey` column (`Latin1_General_100_BIN2` on SQL Server, `utf8mb4_bin` on MySQL), the default on PostgreSQL and MongoDB** | "Daily-Report" and "daily-report" are two schedules on all 10 providers | Explicit collation clauses in the scripts, the EF configuration and the test schemas |
| **B) The database default collation** | No clause | MySQL (and usually SQL Server) treat the two keys as one row, PostgreSQL and MongoDB as two: the same call upserts differently per provider |
| **C) Normalise to one case before storing** | Case-insensitive everywhere | The key is not returned unchanged |

### Chosen Option: **A — ordinal comparison everywhere** (recommended, pending the maintainer)

### Rationale

- Provider coherence (AGENTS.md section 5) asks that switching provider changes only the registration. The integration lifecycle test adds a case-variant pair on every provider.

**Class:** Dilemma

</details>

<details>
<summary><strong>10. Semantics of cancel and reschedule by key (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `CancelByKeyAsync` is idempotent (`Right` when the key is missing, like `CancelAsync` by id); `RescheduleByKeyAsync` returns `Left(scheduling.schedule_key_not_found)` when no pending row has the key (the statement carries `ProcessedAtUtc IS NULL`); a duplicate-key error on an upsert insert is retried once as an update; a null `TimeZoneId` means UTC** | Consistent with the existing cancel; a reschedule of nothing is visible | Cancel and reschedule are asymmetric on a missing key |
| **B) Cancel also returns `Left` when the key is missing** | Symmetric and visible | Differs from `CancelAsync` by id |

### Chosen Option: **A** (recommended, pending the maintainer)

### Rationale

- The cancel-by-id semantics for a missing row is a Dilemma of its own in #146; the answer given there applies to `CancelByKeyAsync` too. A processed one-time row is never revived by a reschedule, only by an upsert that replaces its payload; a recurring row is pending after every reschedule.

**Class:** Dilemma

</details>

<details>
<summary><strong>11. Tenant isolation of the key before #739 (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) #739 (`TenantId` on `ScheduledMessage`) is a hard prerequisite of P12, the pull request that exposes the key methods on the orchestrator; P3 to P11 add only concrete store members and interface members that nothing calls** | No public API can overwrite another tenant's row; fails closed; the unique index becomes `(TenantId, ScheduleKey)` in #739 | P12 waits for #739 |
| **B) Ship P12 first and state that the API is not for multi-tenant hosts until #739** | No wait | Until #739, tenant A upserting key K overwrites tenant B's row in a multi-tenant host; contradicts AC-043 and the fail-closed rule |
| **C) Compose `tenantId:key` in the orchestrator now** | Isolation without a column | Needs `ITenantContext` in scheduling; replaced by #739 anyway |

### Chosen Option: **A — #739 before P12** (recommended, pending the maintainer)

### Rationale

- Gates fail closed (AGENTS.md section 3). The tenant span attribute (AC-044) and the two-tenant test (AC-043) land with #739; the plan's integration scenario (P13) includes the two-tenant case only after #739.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Cron parser with time zone (P1)

> **Goal**: A DST-correct default `ICronParser`.

<details>
<summary><strong>Tasks</strong></summary>

1. Add `Cronos` to `Directory.Packages.props` (version pinned at implementation) and a `PackageReference` in `src/Encina.Messaging/Encina.Messaging.csproj`.
2. `ICronParser` (in `SchedulerOrchestrator.cs`): `Either<EncinaError, DateTime> GetNextOccurrence(string cronExpression, DateTime afterUtc, string? timeZoneId)`; `SchedulingErrorCodes.InvalidTimeZone = "scheduling.invalid_time_zone"` created with `ErrorCategory.Validation`; the two call sites pass `null` for the zone for now.
3. `src/Encina.Messaging/Scheduling/CronosCronParser.cs` — public sealed class: 5 fields use the standard format and 6 fields the seconds format; resolves the zone through `SchedulingTimeZones.TryResolve` (null means UTC); returns the next instant after `afterUtc` as UTC; `Left(scheduling.invalid_cron_expression)` or `Left(scheduling.invalid_time_zone)` otherwise.
4. `SchedulingTimeZones.cs` — internal static `TryResolve(string? id, out TimeZoneInfo zone)` over `TimeZoneInfo.FindSystemTimeZoneById`. Risk to verify: minimal container images may lack tz data; document the requirement.
5. The four existing test files that use `ICronParser` (`SchedulerOrchestratorGuardTests`, `SchedulerOrchestratorTests`, `SchedulerOrchestratorScheduleRecurringTests`, `SchedulerCycleScenarios`) take the new signature.
6. Tests: `CronosCronParserTests` (the 09:00 Europe/Madrid job on both sides of the last-Sunday-of-October and last-Sunday-of-March changes), a property test (the result is after the input and its local time of day is stable), a contract test (`ICronParser` against `CronosCronParser`).
7. `PublicAPI.Unshipped.txt` and the manifest of `Encina.Messaging` through `mechanical-fixer`.

Count: 4 src (parser, time zones, `SchedulerOrchestrator.cs`, csproj) + `Directory.Packages.props` + 4 existing tests + 3 new tests + PublicAPI + manifest = 14.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
- ICronParser has one method today and no implementation ships. The decision on the library is Dilemma 2 of the plan; implement the answer recorded in Maintainer Decisions (Cronos by default).

TASK:
Implement CronosCronParser and SchedulingTimeZones, change ICronParser to the zone-aware signature, update the orchestrator call sites and the four existing test files.

KEY RULES:
- No exceptions for business input: an invalid expression or zone is a Left with the scheduling.* code, created with EncinaErrors.Create and an ErrorCategory (ADR 062).
- The parser never reads the clock; the caller passes afterUtc from TimeProvider.
- No registration here (P2).

REFERENCE FILES:
- src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs (HandleRecurringMessageAsync, ScheduleRecurringAsync)
- tests/Encina.UnitTests/Messaging/Scheduling/SchedulerOrchestratorScheduleRecurringTests.cs
```

</details>

### Phase 2: Key types, error codes and DI (P2)

<details>
<summary><strong>Tasks</strong></summary>

1. `ScheduleOptions.cs` (`ScheduleKey`) and `RecurringOptions.cs` (`ScheduleKey`, `TimeZoneId`) — sealed records; #133 adds the end and missed-occurrence properties to `RecurringOptions`.
2. `ScheduleKeyValidator.cs` — internal static; the rules of choice 8; returns `scheduling.invalid_schedule_key`.
3. `SchedulingErrorCodes` (in `SchedulerOrchestrator.cs`): `InvalidScheduleKey` (`Validation`), `ScheduleKeyNotFound = "scheduling.schedule_key_not_found"` (`NotFound`).
4. `MessagingServiceCollectionExtensions.RegisterScheduling`: when `schedulingOptions.EnableRecurringMessages`, `services.TryAddSingleton<ICronParser, CronosCronParser>()`.
5. Tests: `ScheduleKeyValidatorTests`; a DI test that builds the provider with `ValidateOnBuild` and `ValidateScopes` for scheduling on, recurring on and off, asserts `ICronParser` resolves only when recurring is on and that an application-registered parser wins.
6. `PublicAPI.Unshipped.txt` and manifest of `Encina.Messaging` through `mechanical-fixer`.

Count: 5 src (two records, validator, `SchedulerOrchestrator.cs`, `MessagingServiceCollectionExtensions.cs`) + 2 tests + PublicAPI + manifest = 9.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
- AGENTS.md section 3: an AddEncina* method must register everything it resolves, proven by a DI test with ValidateOnBuild and ValidateScopes. P1 merged the parser.

TASK:
Add the options records, the validator, the two error codes and the TryAdd registration, with the tests of Phase 2.

KEY RULES:
- TryAdd so an application parser registered first wins.
- The schedule key is never logged or used as a metric tag (it can identify a data subject).

REFERENCE FILES:
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs
- src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs
```

</details>

### Phase 3: ADO.NET providers (P3 SqlServer, P5 PostgreSQL, P7 MySQL)

> **Goal**: Columns, index and the three store methods as concrete members.

<details>
<summary><strong>Tasks</strong></summary>

One pull request per database. For each ADO.NET package:

1. `Scripts/004_CreateScheduledMessagesTable.sql` and the table in `Scripts/000_CreateAllTables.sql` (edited in place, no migration support, AGENTS.md section 1): `ScheduleKey` NVARCHAR(256) or VARCHAR(256) NULL with the binary collation of choice 9, `TimeZoneId` VARCHAR(100) NULL; unique index on the key (SQL Server filtered `WHERE ScheduleKey IS NOT NULL`, PostgreSQL and MySQL allow many NULLs in a unique index).
2. `ScheduledMessage.cs`: the two properties as concrete members (the interface gets them in P10).
3. `ScheduledMessageStoreADO.cs`: key columns in `AddAsync`; public methods `UpsertByKeyAsync` (SQL Server `MERGE ... WITH (HOLDLOCK)` with `OUTPUT inserted.Id`; PostgreSQL `INSERT ... ON CONFLICT (schedulekey) DO UPDATE ... RETURNING id`; MySQL `INSERT ... ON DUPLICATE KEY UPDATE` followed by a `SELECT Id`), `CancelByKeyAsync` (`DELETE`), `RescheduleByKeyAsync` (`UPDATE ... WHERE ScheduleKey = @Key AND ProcessedAtUtc IS NULL`, resets retry state, 0 rows returns the not-found Left).
4. All commands use `OpenAsync`, `ExecuteNonQueryAsync`/`ExecuteScalarAsync` with the `CancellationToken` and join the scope transaction through the transaction accessor (AGENTS.md section 3).
5. Tests in the same PR: the existing `tests/Encina.IntegrationTests/ADO/<Db>/Scheduling/ScheduledMessageStoreADOTests.cs` (`[Collection("ADO-<Db>")]`) gains the key lifecycle (upsert twice returns the same `Id`, cancel then reschedule returns the not-found code, two concurrent upserts end with one row, a case-variant pair stays two rows); the shared test schema (`tests/Encina.TestInfrastructure/Schemas`) and the benchmark `AdoSchemaBuilder.cs` section of that database get the columns, and any test with its own DDL for the table (found by grep at pick-up) is updated.
6. `PublicAPI.Unshipped.txt` and the manifest of the package through `mechanical-fixer`.

Count per pull request: 4 package files (message, store, 2 scripts) + PublicAPI + manifest + test schema + benchmark builder + integration test = 9.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
- Existing stores: src/Encina.ADO.<Db>/Scheduling/ScheduledMessageStoreADO.cs. Differences: SQL Server uses TOP and bit, PostgreSQL lower-case identifiers and LIMIT, MySQL backtick-safe identifiers and 0/1. Integration tests build the table from tests/Encina.TestInfrastructure/Schemas, not from the scripts.

TASK:
Implement the schema columns and the three key methods for ONE ADO provider per pull request as public methods of the concrete store (the interface gains them in P11).

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

### Phase 4: Dapper providers (P4 SqlServer, P6 PostgreSQL, P8 MySQL)

<details>
<summary><strong>Tasks</strong></summary>

1. Same columns and SQL as phase 3 in `src/Encina.Dapper.<Db>/Scripts/004_CreateScheduledMessagesTable.sql`, `000_CreateAllTables.sql` and `Scheduling/ScheduledMessageStoreDapper.cs`; message class with the two properties.
2. `ScheduledMessageStoreDapper` uses `EitherHelpers.TryAsync`, `DbLease.Enlisted(_connection, _transactionAccessor)` and `CommandDefinition` with the token; the new methods follow that shape with error codes `scheduling.upsert_failed`, `scheduling.cancel_by_key_failed`, `scheduling.reschedule_by_key_failed` (each created with `ErrorCategory.Internal`, ADR 062).
3. Tests in the same PR: the existing `tests/Encina.IntegrationTests/Dapper/<Db>/Scheduling/ScheduledMessageStoreDapperTests.cs`, the `DapperSchemaBuilder.cs` section of the database, `PublicAPI.Unshipped.txt` and manifest.

Count per pull request: 4 package files + PublicAPI + manifest + benchmark builder + integration test = 8 (the shared test schema changed in the ADO.NET pull request of the same database).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
- Phase 3 defines the SQL per database; the Dapper stores wrap it in EitherHelpers.TryAsync and DbLease.Enlisted.

TASK:
Implement the Dapper side for ONE database per pull request (SqlServer, PostgreSQL, MySQL).

KEY RULES:
- Argument checks follow the store convention (ArgumentException for an empty key); the orchestrator validates first.
- Collections Dapper-SqlServer, Dapper-PostgreSQL, Dapper-MySQL.

REFERENCE FILES:
- src/Encina.Dapper.SqlServer/Scheduling/ScheduledMessageStoreDapper.cs
- tests/Encina.IntegrationTests/Dapper/SqlServer/Scheduling/ScheduledMessageStoreDapperTests.cs
```

</details>

### Phase 5: EF Core and MongoDB (P9)

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessage.cs` and `ScheduledMessageConfiguration.cs`: `ScheduleKey` (max 256, binary collation per provider) with `HasIndex(...).IsUnique()` and `TimeZoneId` (max 100). One configuration serves SqlServer, PostgreSQL and MySQL; a nullable unique index allows many NULLs on PostgreSQL and MySQL and the SQL Server provider filters it.
2. `ScheduledMessageStoreEF.cs`: `UpsertByKeyAsync` reads the row by key in the current unit of work, updates or adds it, saves, and on a unique-violation `DbUpdateException` re-reads once; `CancelByKeyAsync` removes by key; `RescheduleByKeyAsync` updates the tracked row.
3. `src/Encina.MongoDB/Scheduling/ScheduledMessage.cs` (`[BsonElement("scheduleKey")]`, `[BsonElement("timeZoneId")]`), `ScheduledMessageStoreMongoDB.cs` (upsert with `FindOneAndUpdate` and `IsUpsert = true`, `$set` for the mutable fields and `$setOnInsert` for `_id` and `createdAtUtc`) and `MongoDbIndexCreator.cs` (a unique partial index on `scheduleKey` where it is a string).
4. Tests in the same PR: the shared bodies in `tests/Encina.TestInfrastructure/EFCore/ScheduledMessageStoreEFTestsBase.cs` (run by the three EF provider classes) and `tests/Encina.IntegrationTests/Infrastructure/MongoDB/Stores/ScheduledMessageStoreMongoDBIntegrationTests.cs`.
5. PublicAPI (EF Core, MongoDB) and the two manifests through `mechanical-fixer`.

Count: EF 3 (message, configuration, store) + Mongo 3 (message, store, index creator) = 6 src, 2 tests, 2 PublicAPI, 2 manifests = 12.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
- EF Core: one store and one configuration for the three providers. MongoDB: BsonElement attributes on the message class, indexes created in MongoDbIndexCreator.

TASK:
Implement the EF Core and MongoDB parts of phases 3 and 4.

KEY RULES:
- EF unit of work (SaveChangesAsync) stays the caller's; the store methods record on the context like the existing ones.
- A Mongo upsert is one atomic FindOneAndUpdate, never read-then-write.

REFERENCE FILES:
- src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessageStoreEF.cs
- src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessageConfiguration.cs
- src/Encina.MongoDB/Scheduling/ScheduledMessageStoreMongoDB.cs
```

</details>

### Phase 6: `IScheduledMessage` members and non-provider implementers (P10)

<details>
<summary><strong>Tasks</strong></summary>

1. `IScheduledMessage.cs`: `string? ScheduleKey { get; set; }` and `string? TimeZoneId { get; set; }` with XML docs (key unique; zone is an IANA id, null means UTC). The 8 provider classes already have them, so they compile unchanged.
2. The 5 other implementers in the same PR: `FakeScheduledMessage`, the nested `FakeScheduledMessage` of `ScheduledMessageFaker`, `ArbitraryScheduledMessage`, and the two `BenchmarkScheduledMessage` classes; `ScheduledMessageBuilder` (TestInfrastructure) sets the properties.
3. Contract test (real code, no `.md`): the 8 provider factories and the fake create messages whose `ScheduleKey` and `TimeZoneId` are null.
4. `PublicAPI.Unshipped.txt` of Messaging, Fakes and Bogus; manifests of Fakes, Bogus and FsCheck through `mechanical-fixer`.

Count: 4 src + builder + 2 benchmark implementers + 1 contract test + 3 PublicAPI + 3 manifests = 14.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
- P3 to P9 gave every provider ScheduledMessage the two properties. IScheduledMessage has 13 implementers (listed in the plan Summary).

TASK:
Add the interface members and update the 5 other implementers so the whole solution compiles.

KEY RULES:
- No reflection-only tests; dotnet build of Encina.slnx with zero warnings; XML docs on every public member.

REFERENCE FILES:
- src/Encina.Messaging/Scheduling/IScheduledMessage.cs
- src/Encina.Testing.Bogus/ScheduledMessageFaker.cs
- src/Encina.Testing.FsCheck/ArbitraryMessages.cs
```

</details>

### Phase 7: Store interface members, decorator and fake (P11)

<details>
<summary><strong>Tasks</strong></summary>

1. `IScheduledMessageStore`: `Task<Either<EncinaError, Guid>> UpsertByKeyAsync(IScheduledMessage message, CancellationToken)`, `Task<Either<EncinaError, Unit>> CancelByKeyAsync(string scheduleKey, CancellationToken)` and `Task<Either<EncinaError, Unit>> RescheduleByKeyAsync(string scheduleKey, DateTime nextScheduledAtUtc, CancellationToken)` (semantics of choice 10). The 8 provider stores already have them.
2. `InstrumentedScheduledMessageStore` (Encina.OpenTelemetry) wraps the three methods with the existing activity and metric pattern; `FakeScheduledMessageStore` implements them.
3. Tests: `InstrumentedScheduledMessageStoreTests`, `InstrumentedScheduledMessageStoreGuardTests`, `InstrumentedStoreMethodGuardTests`, unit tests of the fake, and a key-method contract test against `FakeScheduledMessageStore`.
4. PublicAPI of Messaging and Fakes; manifests of Fakes and OpenTelemetry through `mechanical-fixer`.

Count: 3 src + 5 tests + 2 PublicAPI + 2 manifests = 12.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
CONTEXT:
- Every provider store already has the three methods as concrete members; only the decorator and the fake lack them.

TASK:
Add the interface members, the decorator members and the fake members.

KEY RULES:
- Either<EncinaError, T> on every store method; the key is never a tag or log argument; tests execute real code.

REFERENCE FILES:
- src/Encina.Messaging/Scheduling/IScheduledMessageStore.cs
- src/Encina.OpenTelemetry/MessagingStores/InstrumentedScheduledMessageStore.cs
- src/Encina.Testing.Fakes/Stores/FakeScheduledMessageStore.cs
```

</details>

### Phase 8: Orchestrator API and observability (P12, after #739)

> **Goal**: Key and zone aware scheduling on the single scheduling surface.

<details>
<summary><strong>Tasks</strong></summary>

1. `ScheduleAsync<TRequest>(TRequest, DateTimeOffset executeAt, ScheduleOptions? options = null, CancellationToken)`; the existing `DateTime` and `TimeSpan` overloads gain a `ScheduleOptions` overload. A set `ScheduleKey` selects `UpsertByKeyAsync`, otherwise `AddAsync`. After `IScheduledMessageFactory.Create` the orchestrator assigns `ScheduleKey` and `TimeZoneId` (choice 5, option C).
2. `ScheduleRecurringAsync<TRequest>(TRequest, string cronExpression, RecurringOptions options, CancellationToken)`: validates the zone and key, computes the first occurrence in the zone, persists `TimeZoneId`.
3. `CancelByKeyAsync(string scheduleKey, ct)` and `RescheduleByKeyAsync(string scheduleKey, DateTimeOffset newExecuteAt, ct)`.
4. `HandleRecurringMessageAsync` passes `message.TimeZoneId` to the parser, so each recurrence is computed in the stored zone. Keep every new method under cyclomatic complexity 10 (CRAP gate).
5. New `src/Encina.Messaging/Diagnostics/SchedulingScheduleLog.cs` (`[LoggerMessage]`, 2326-2331 inside `EventIdRanges.MessagingScheduling` 2300-2399, already mapped to `Encina.Messaging` in `EncinaEventIdAllocationTests`): `ScheduleUpserted`, `ScheduleRescheduledByKey`, `ScheduleCancelledByKey`, `ScheduleKeyNotFound`, `InvalidTimeZone`, `InvalidScheduleKey`; arguments are the message id and the error code only. The log class is created here because this is the first pull request that logs.
6. `SchedulingScheduleMetrics.cs` on the shared `Encina` meter: counters `encina.scheduling.rescheduled` (tag `recurring`) and `encina.scheduling.cancelled`; spans `encina.scheduling.upsert`, `...reschedule`, `...cancel` in `SchedulingActivitySource` with the recurring flag; no key, payload or zone as a tag. The tenant id attribute is added with #739.
7. Tests: the three existing orchestrator test files updated and extended (a new `SchedulerOrchestratorKeyTests`), a test that no activity tag, metric tag or log argument carries the key or content, an exporter test of the counters.
8. `PublicAPI.Unshipped.txt` and manifest of `Encina.Messaging` through `mechanical-fixer`.

Count: 4 src (`SchedulerOrchestrator.cs`, log class, metrics, activity source) + 6 tests + PublicAPI + manifest = 12.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
CONTEXT:
- SchedulerOrchestrator is a sealed class (there is no ISchedulerOrchestrator). ScheduleAsync returns Either<EncinaError, Guid>; time comes from the injected TimeProvider. Ids 2326-2339 are reserved for this issue inside MessagingScheduling; #146 uses 2340-2349, #150 2350-2359, #133 2360-2379 and #147 2380-2389 (the batch registry holds 2326-2389 for the five plans).

TASK:
Add the key, DateTimeOffset and time zone members, the log class, the metrics class and the spans listed in the plan.

KEY RULES:
- A past execution time returns scheduling.invalid_schedule_time, as today; a Left from the store propagates, nothing is swallowed.
- EncinaError.Message never reaches logs, tags or health results; log the message id and error code, never the key or the request content; pack ids sequentially from 2326.

REFERENCE FILES:
- src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs
- src/Encina.Messaging/Diagnostics/SchedulingStoreLog.cs
- src/Encina.Messaging/Diagnostics/SchedulingProcessorMetrics.cs
- tests/Encina.UnitTests/Messaging/Scheduling/SchedulerOrchestratorTests.cs
```

</details>

### Phase 9: Scenario S4 on the 10 providers (P13)

<details>
<summary><strong>Tasks</strong></summary>

1. `tests/Encina.IntegrationTests/Messaging/Scheduling/ScheduleKeyScenarios.cs` (shared scenario, like `OutboxAtomicityScenario`): scenario S4 (P-25) through `SchedulerOrchestrator`: upsert, reschedule, cancel by key, the 09:00 Europe/Madrid recurrence across a DST change. The two-tenant case is added with #739.
2. One derived class per provider in the shared collections (ADO x3, Dapper x3, EF Core x3, MongoDB): 10 files.

Count: 1 scenario + 10 provider classes = 11.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
CONTEXT:
- Tests must execute real package code. Shouldly through Encina.Testing.Shouldly; FakeTimeProvider for time; shared [Collection("<Family>-<Db>")] fixtures, never dispose the fixture.

TASK:
Write the scenario and its 10 provider classes; report per-flag coverage against the manifest.

KEY RULES:
- No Thread.Sleep; deterministic times; a test never swallows an exception thrown by the code it drives; never a .md for database integration tests.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Scheduling/SchedulerCycleScenarios.cs
- tests/Encina.IntegrationTests/Dapper/SqlServer/Scheduling/ScheduledMessageStoreDapperTests.cs
```

</details>

### Phase 10: Removal of `IMessageScheduler`, documentation and finalization (P14)

<details>
<summary><strong>Tasks</strong></summary>

1. Delete `src/Encina.EntityFrameworkCore/Scheduling/IMessageScheduler.cs` and `tests/Encina.ContractTests/EntityFrameworkCore/IMessageSchedulerContractTests.cs`; remove its entry from `.github/coverage-manifest/Encina.EntityFrameworkCore.json` and the contract-test line from `.github/honesty-audit-baseline.json` (the file is not in the PublicAPI files). Rewrite the snippets of `src/Encina.Messaging/README.md` (lines 192 and 513) to use `SchedulerOrchestrator`.
2. Load and benchmark `.md` justifications (not a concurrent hot path): `tests/Encina.LoadTests/Messaging/Scheduling/ScheduleKey.md` and `tests/Encina.BenchmarkTests/Encina.Benchmarks/Messaging/Scheduling/ScheduleKey.md`. Unit, guard, contract, property and integration are real tests.
3. `changelog.d/1222-scheduling-schedule-key.added.md` and `changelog.d/1222-scheduling-imessagescheduler.removed.md` (`mechanical-fixer`).
4. `docs-writer`: `docs/features/scheduling.md` (key, zone, parser, DST note, the in-flight upsert window of choice 4), `src/Encina.Messaging/README.md`, the ADR on the cron library and time-zone semantics (ADR number assigned when the ADR pull request opens), `docs/INVENTORY.md`.
5. `dotnet build --configuration Release` with 0 warnings; `dotnet test` green; every coverage flag at its target.

Count: 2 deletions + 2 manifest and baseline edits + 2 justifications + 2 changelog + README + feature page + ADR + INVENTORY = 12.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 10</strong></summary>

```
CONTEXT:
- Documentation pages under docs/ go to the docs-writer; changelog fragments, PublicAPI lines and manifests go to the mechanical-fixer.

TASK:
Remove the orphan interface and its references, then finish the documentation and hand the page, README, ADR and changelog work to the specialists with the exact text decided in the plan.

KEY RULES:
- Never edit CHANGELOG.md; coverage figures are cited with covref markers, never typed; no compatibility alias for IMessageScheduler.

REFERENCE FILES:
- docs/features/scheduling.md
- changelog.d/README.md
- .github/honesty-audit-baseline.json
```

</details>

### Per-file coverage targets (entered in the PR that carries the tests)

| File | PR | Flag and target | Justification |
| --- | --- | --- | --- |
| `Scheduling/CronosCronParser.cs` | P1 | unit 95, guard 60, contract 70, property 80 | Pure logic: unit tests cover the 5 and 6 field formats, both DST changes and the invalid expression and zone; guard covers the null and empty arguments; the contract test runs the interface contract on the real parser; the property test covers the local-time stability. |
| `Scheduling/SchedulingTimeZones.cs` | P1 | unit 100, guard 0 | Two branches (null, resolved or not) are unit-tested; the type is internal without `InternalsVisibleTo` for `Encina.GuardTests` (null is covered through the parser guard tests). |
| `Scheduling/ScheduleKeyValidator.cs` | P2 | unit 100, guard 0 | Every rule has a unit test; the type is internal without `InternalsVisibleTo` for `Encina.GuardTests`. |
| `Scheduling/ScheduleOptions.cs`, `RecurringOptions.cs` | P2 | unit 0 | Positional records with no executable lines of their own. |
| Each provider `ScheduledMessageStore*.cs` | P3 to P9 | integration: the current manifest target, never lowered | The three key methods are exercised by the lifecycle test of the same pull request; the worker reads the current value from the manifest and only raises it. |
| Each provider `ScheduledMessage.cs` | P3 to P9 | integration: current target | The two properties are assigned and read by the same lifecycle test. |
| `Scheduling/SchedulerOrchestrator.cs` | P1, P2, P12 | unit and guard: the current manifest targets, integration 70 | The new methods are unit-tested with `FakeTimeProvider`; targets never go down, and the worker confirms the current unit and guard values in the manifest before editing. |
| `Diagnostics/SchedulingScheduleLog.cs` | P12 | unit 0 | `[LoggerMessage]` declarations; the generated bodies are not in the file. |
| `Diagnostics/SchedulingScheduleMetrics.cs` | P12 | unit 90 | The exporter test drives both counters; the remaining lines are disposal paths. |
| `Stores/FakeScheduledMessageStore.cs`, `MessagingStores/InstrumentedScheduledMessageStore.cs` | P11 | unit: current targets, contract 70 for the fake | The fake's key methods have unit and contract tests; the decorator follows the existing pattern. |

Guard tests for the new public API (#3798): `CronosCronParser.GetNextOccurrence` (null and empty expression), `ScheduleAsync` and `RescheduleByKeyAsync` overloads with a null request or key, and the three store methods (null message or key) get guard tests in the pull request that adds them; `ScheduleOptions? options = null` accepts null and has a valid-result test, not an exception test.

Departure from the issue matrix: the issue marks functions 7, 8 and 9 not applicable; this plan integrates 8 (the key methods join the scope transaction) and 9 (the key is the deduplication key) because they cost nothing here.

---

## Research

### Relevant Standards & Specifications

| Standard | Requirement | Key Details |
|----------|-------------|-------------|
| SPEC-002 REQ-033 | Scheduling minimal slice | Schedule key, time-zone-aware cron storing the zone, shipped `ICronParser`, orphan interface resolved, `DateTimeOffset` accepted |
| SPEC-002 AC-033 and S4 | Acceptance | Upsert and cancel by key on 10 providers; 09:00 Europe/Madrid fires at 09:00 local on both sides of a DST change |
| SPEC-002 DEC-005 (a) | Why now | Schema and signature changes across 10 providers are free before 1.0 |
| ADR-050 | Scheduling dependencies | A default `ICronParser`, registered with `TryAdd` only when `EnableRecurringMessages` is on |
| ADR 062 | Error categories | Category of each new error code |
| ADR-018 | Cross-cutting integration | The 12-function matrix below |
| ADR-021 | EventIds | Ids inside the registered `MessagingScheduling` range |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `SchedulerOrchestrator`, `SchedulingErrorCodes`, `ICronParser`, `IScheduledMessageFactory` | `src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs` | Extended; the parser contract changes |
| `IScheduledMessage`, `IScheduledMessageStore` | `src/Encina.Messaging/Scheduling/` | Two properties and three methods added |
| `RegisterScheduling` | `src/Encina.Messaging/MessagingServiceCollectionExtensions.cs` | Registers the default parser with `TryAdd` |
| `SchedulingProcessorMetrics`, `SchedulingActivitySource`, `SchedulingStoreLog` | `src/Encina.Messaging/Diagnostics/` | Pattern for the new metrics, spans and log class |
| `EitherHelpers.TryAsync`, `DbLease.Enlisted` | Dapper and ADO packages | Store method shape and transaction enlistment |
| `InstrumentedScheduledMessageStore` | `src/Encina.OpenTelemetry/MessagingStores/` | Decorator members |
| `FakeScheduledMessageStore`, `ScheduledMessageFaker`, `ScheduledMessageBuilder` | `Encina.Testing.Fakes`, `Encina.Testing.Bogus`, `Encina.TestInfrastructure` | Test doubles and builders |

Provider files touched:

| Provider | Message class | Store | Schema or mapping |
|----------|---------------|-------|-------------------|
| ADO-SqlServer, -PostgreSQL, -MySQL | `src/Encina.ADO.<Db>/Scheduling/ScheduledMessage.cs` | `.../ScheduledMessageStoreADO.cs` | `src/Encina.ADO.<Db>/Scripts/004_CreateScheduledMessagesTable.sql`, `000_CreateAllTables.sql` |
| Dapper-SqlServer, -PostgreSQL, -MySQL | `src/Encina.Dapper.<Db>/Scheduling/ScheduledMessage.cs` | `.../ScheduledMessageStoreDapper.cs` | `src/Encina.Dapper.<Db>/Scripts/004_CreateScheduledMessagesTable.sql`, `000_CreateAllTables.sql` |
| EFCore-SqlServer, -PostgreSQL, -MySQL | `src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessage.cs` | `.../ScheduledMessageStoreEF.cs` | `.../ScheduledMessageConfiguration.cs` (shared) |
| MongoDB | `src/Encina.MongoDB/Scheduling/ScheduledMessage.cs` | `.../ScheduledMessageStoreMongoDB.cs` | BsonElement attributes and `MongoDbIndexCreator.cs` |

The provider factories are unchanged under choice 5, option C.

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` (`MessagingScheduling`, existing) | **2326-2339** reserved for #1222, ids 2326-2331 used | `SchedulingScheduleLog`; 2300-2306 are `SchedulingStoreLog` and 2320-2325 `SchedulingProcessorLog`. The batch registry holds 2326-2389 for #1222, #146, #150, #133 and #147; the sub-ranges of the siblings are theirs to state |
| New range | none | The block 6750-6799 assigned to this family is not needed |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production and package files | 52 | P1 5 (with `Directory.Packages.props`), P2 5, P3 to P8 24, P9 6, P10 4, P11 3, P12 4, P14 1 deletion |
| Tests and test support | 55 | P1 7, P2 2, P3 to P8 15 (schema, benchmark builder and integration test per ADO pull request, the last two per Dapper one), P9 2, P10 4, P11 5, P12 6, P13 11, P14 3 (1 deleted contract test, 2 justifications) |
| PublicAPI | 16 | P1 1, P2 1, P3 to P8 6, P9 2, P10 3, P11 2, P12 1 |
| Coverage manifests | 17 | The same pull requests, plus P14 1 |
| Documentation, changelog, baseline | 7 | P14: README, feature page, ADR, INVENTORY, 2 changelog fragments, honesty baseline |
| **Total** | **147** | 52 + 55 + 16 + 17 + 7; the same 147 file edits as the per-PR table of the Summary |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library. Either<EncinaError, T> everywhere; no [Obsolete], no compatibility layers; time from TimeProvider.
- Issue #1222 (SPEC-002 REQ-033): schedule key, zone-aware cron, shipped ICronParser, remove IMessageScheduler, DateTimeOffset.

IMPLEMENTATION OVERVIEW:
P1/P2: zone-aware ICronParser and CronosCronParser, key types, error codes, TryAdd registration. P3-P9: ADO x3, Dapper x3, EF Core, MongoDB: columns, unique index, three store methods as concrete members, integration tests. P10/P11: interface members, decorator, fake. P12: orchestrator methods and diagnostics (after #739). P13: scenario S4. P14: removal of IMessageScheduler, docs.
Apply the answers recorded in Maintainer Decisions for the cron library, the parser contract, the key scope, how the key reaches a message and the tenant prerequisite.

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
| 5 | Validation | ✅ | Phases 1, 2 and 8: key, cron expression and zone id validated at schedule time, invalid input returns Left |
| 6 | Resilience | ❌ | No call to an external system |
| 7 | Distributed Locks | ⏭️ | Multi-instance claiming is #1251; upsert atomicity here comes from the unique index |
| 8 | Transactions | ✅ | Phases 3 to 5: the new store methods join the scope transaction like the existing ones (the EF Core store records on the context and the caller saves) |
| 9 | Idempotency | ✅ | The schedule key is the deduplication key: scheduling the same key again updates one row |
| 10 | Multi-Tenancy | ⏭️ | Defer: #739 (open) is a hard prerequisite of P12 (choice 11); the unique index becomes (tenant, key) there; AC-043 is met when both land |
| 11 | Module Isolation | ❌ | SPEC-002 asks no module scoping; `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ⏭️ | Cancel and reschedule are operation-audit events (`IOperationAuditStore`), tracked with #146 (open); #1778 is the entity-change store |

---

## Prerequisites & Dependencies

| Item | Relation | Note |
|------|----------|------|
| #739 | Hard prerequisite of P12 | `TenantId` on `ScheduledMessage`; without it the key methods could overwrite another tenant's row (choice 11) |
| #718 and #1251 | Coordinate | #1251 replaces `GetDueMessagesAsync` with claiming and changes every completion method of `IScheduledMessageStore`; the new key methods are independent of the claim token. It also closes the in-flight upsert window (choice 4). Merge order is decided at decomposition |
| #146, #150, #133, #147 | Follow this plan | Each reuses the key methods, the parser and the EventId reservations listed above |
| #1153 | Related | `ScheduleRecurringAsync` ignoring a failed insert; the orchestrator here returns the store's Left |

---

## Next Steps

1. Review this plan; the Dilemmas (choices 1 to 5 and 8 to 11) are answered by the maintainer when work on the issue starts and recorded in `## Maintainer Decisions`.
2. Decompose into the sub-issues P1 to P14 of the Summary, each with its exact file list and baseline.
3. Link the plan from #1222 and start with P1 in its own worktree.
