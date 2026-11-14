# Implementation Plan: Priority for Scheduled Messages (and per-priority batching question) (`Encina.Messaging` + 10 database providers)

> **Issue**: [#301](https://github.com/dlrivada/Encina/issues/301)
> **Type**: Feature
> **Complexity**: Medium (8 phases, 10 database providers, 163 file changes, delivered in 15 pull requests of at most 15 files each, see "Delivery order and size")
> **Estimated Scope**: about 800 lines of production code + about 1,000 lines of tests
> **Family**: message priority and partitioning (#123, #251, #301, #469, #1254). The shared priority design (and the type `MessagePriority`, created by PR-A of #123) is OWNED by #123; this plan applies it to the scheduled-message store and decides only what is specific to scheduling and to per-priority batching. #147 (priority for scheduled messages) plans the same store work; which of the two issues delivers it is Dilemma D6 below, and only one of the two plans is ever implemented.

---

## Summary

#301 asks for priority in both the outbox and scheduling, with per-priority batch sizes and processing intervals. After the family analysis its scope is:

- **Outbox side**: delivered by #123 (type, attribute, column, ordered fetch, starvation). This plan adds nothing there.
- **Scheduling side (this plan)**: `IScheduledMessage.Priority`, a priority column on the scheduled-message table of all 10 providers, a priority-aware due fetch, and priority resolution when a request is scheduled.
- **Per-priority batching and intervals (this plan, Dilemma)**: whether to run one loop per priority level or one ordered query.

Facts verified in today's code:

- `IScheduledMessageStore.GetDueMessagesAsync(int batchSize, int maxRetries, CancellationToken)` returns `Either<EncinaError, IEnumerable<IScheduledMessage>>`; stores order by `ScheduledAtUtc` (for example `src/Encina.Dapper.SqlServer/Scheduling/ScheduledMessageStoreDapper.cs`: `SELECT TOP (@BatchSize) * ... ORDER BY ScheduledAtUtc`).
- `SchedulerOrchestrator` calls it in `ProcessDueMessagesAsync` with `_options.BatchSize` and again in `GetPendingCountAsync` with `int.MaxValue` (it loads every due row only to count them; that path must keep working unchanged and is noted for the health-check follow-up).
- `IScheduledMessageFactory.Create(id, requestType, content, scheduledAtUtc, createdAtUtc, isRecurring, cronExpression)` is implemented by one factory per provider package; this plan does not change its signature (register entry S4, same answer as #123 R10).
- `SchedulingErrorCodes` exists today inside `src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs` (not in a file of its own); the invalid-priority error is not added there: the resolver of #123 returns the shared code `messaging.invalid_priority` with `ErrorCategory.Validation` (ADR 062, merged).
- `SchedulingOptions` has `ProcessingInterval`, `BatchSize`, `MaxRetries`, `BaseRetryDelay`, `EnableProcessor`, `EnableRecurringMessages`. `GetPendingByPriorityAsync`, `PriorityBatchingOptions` and `IOutboxMessageFactory` as drawn in the issue do not exist; the issue's "13 providers" is stale (10 database providers, SQLite is out of the matrix).

- **Affected packages**: `Encina.Messaging`, `Encina.ADO.*` (3), `Encina.Dapper.*` (3), `Encina.EntityFrameworkCore`, `Encina.MongoDB`, `Encina.OpenTelemetry` (instrumented store), `Encina.Testing.Fakes`, `Encina.Testing.Bogus`, `Encina.Testing`.
- **Provider category**: Database (10) for the store changes (AGENTS.md section 5). Hangfire and Quartz run infrastructure jobs and are not affected (AGENTS.md section 3, Scheduling).
- **Related scheduling plans**: the scheduling slices of SPEC-002 REQ-033 (#146, #148, #150) change the same factory and tables; the merge order is decided by the orchestrator.

---

## Design Choices

<details>
<summary><strong>1. Adopt the shared priority design of #123 (type, source, starvation, fetch contract) (Dilemma, answered once in #123)</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) Reuse `MessagePriority` (`Background..Critical`, higher is more urgent), `IPrioritizedMessage`, `[MessagePriority]`, the aged-first starvation rule and a criteria record for the fetch (`ScheduledFetchCriteria`), exactly as #123 recommends** | One concept for outbox, scheduler and transports; a command that is both published and scheduled keeps its priority; no second ADR | Inherits the five levels and the direction decided in #123 |
| **B) A scheduling-specific enum and attribute** | Scheduling could use other levels | Two attributes and a mapping; contradicts the "one feature seen from four sides" analysis of the issues |

This issue's own enum is `Critical = 0` ascending to `Background = 4`; #123 recommends `Background = 0` ascending to `Critical = 4`. The two directions cannot both ship, so this choice is the same question as #123 choice 1 and takes the same answer.

### Chosen Option: **A — reuse the #123 design** (recommended, pending the maintainer)

### Rationale

- Option B wins only if scheduling needs levels the outbox does not; nothing in #301 or #147 says so.
- The aged-first rule is applied to the overdue time and uses exactly the ordering of #123 (its register entry R7): a due message whose `ScheduledAtUtc <= StarvationCutoffUtc` is aged and sorts before the others; inside each of the two groups the order is `Priority DESC`, then `ScheduledAtUtc` ascending. A message that is not yet due is never returned, so priority cannot make a future message run early.
- The type is created by PR-A of #123 and this plan starts after it has merged; this plan never creates `MessagePriority`.
- Stores keep their old ordering (`ScheduledAtUtc`) when `SchedulingOptions.Priority.Enabled` is false.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. One ordered fetch or one processing loop per priority (Dilemma D2 of the issue)</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) One due-fetch ordered by priority (with the starvation rule); one batch size and one interval** | One store path changed on 10 providers; no extra database load; nothing to coordinate between loops | No per-priority pacing: a critical message waits for the next cycle (at most `ProcessingInterval`) |
| **B) Per-priority batch sizes and intervals as proposed (`PriorityBatching`, `PriorityIntervals`)** | A fast lane: critical messages polled every 100 ms while background ones are batched every 30 s | Five loops per store, a query per level per cycle, a bigger option surface; several loops contend for the same rows unless row claiming (#1251) exists |
| **C) A: now; B later as a separate issue if measured latency needs it** | Ships the ordering value now; keeps B possible because the fetch contract is a record | Latency-sensitive users wait |

### Chosen Option: **C — ship the ordered fetch now, open B only on evidence** (recommended, pending the maintainer)

### Rationale

- Option B wins when critical messages need sub-second latency while background ones are batched, which no acceptance criterion states; the issue says only that priorities are "processed before" others.
- The processing interval default is 30 seconds (`SchedulingOptions.ProcessingInterval`); anyone needing a fast lane can lower it for the whole scheduler.
- B depends on #1251 (row claiming) to be safe with several loops or instances; recorded as a prerequisite instead of built on sand.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Scheduling-specific semantics: priority never changes when a message is due (Settled), and recurring messages keep it (Dilemma)</strong></summary>

### Options Considered

**3a. Does priority change when a message is due**

| Option | Pros | Cons |
| --- | --- | --- |
| **A) Priority only orders messages that are already due** | Matches the meaning of "priority queue" and of `IScheduledMessage.IsDue()` | A high-priority message is still late if the processor is late |
| **B) Priority shifts the due time (high priority runs earlier than scheduled)** | Appears to speed critical work | Changes the contract of `ScheduledAtUtc`; surprising and unsafe for time-bound jobs |

### Chosen Option: **A — order only the due set**

### Rationale

- `IsDue()` and the `ScheduledAtUtc <= now` filter stay exactly as they are; the new `ORDER BY` only changes the order of rows that pass the existing filter.

**Class:** Settled
**Settled by:** `IScheduledMessage.IsDue()` and the existing due filter in `src/Encina.Messaging/Scheduling/IScheduledMessage.cs` and the provider stores (the contract this plan must not change; option B contradicts it); issue #301 Alternatives, item 3 (time-based prioritization is not priority).

**3b. Priority of a recurring series**

| Option | Pros | Cons |
| --- | --- | --- |
| **A) Occurrences inherit the priority of the series** | `RescheduleRecurringMessageAsync` keeps the row, so the column persists with no extra code | Cannot raise one occurrence |
| **C) A different priority per occurrence** | Flexible | Needs a priority source per occurrence; no use case in the issues |

### Chosen Option: **A — the series keeps one priority** (recommended, pending the maintainer)

### Rationale

- Option A wins for the use case the issues describe (a priority is a property of the request type). Option C wins if an occurrence must be escalated, for example after repeated failures.
- A scheduled message is a request, not a notification: the priority is resolved from the request type or instance by `MessagePriorityResolver` (#123 choice 2) in `SchedulerOrchestrator.ScheduleAsync` and `ScheduleRecurringAsync` and set on the message after `IScheduledMessageFactory.Create` (register entry S4).

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Opt-in and defaults</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) `SchedulingOptions.Priority` (`SchedulingPriorityOptions`): `Enabled = false`, `StarvationThreshold = 5 minutes`; column always present** | Disabled by default; one schema; switching it on later orders rows already stored | Unused column when disabled |
| **B) Reuse the outbox's `OutboxPriorityOptions` instance** | One setting | Outbox and scheduler are separate patterns that users enable separately |

### Chosen Option: **A — separate options object per pattern, same shape** (recommended, pending the maintainer)

### Rationale

- Both options keep the feature off by default (AGENTS.md section 3, ADR-050), so the rule does not decide the shape. Option A wins when outbox and scheduler are enabled independently (they are separate patterns); option B wins when one switch for both is wanted.
- The issue's `EnablePriorityProcessing` flag becomes `SchedulingOptions.Priority.Enabled`; the default threshold is 5 minutes, the same answer as #123 choice 3.
- Validation lives with the other cross-property rules of `SchedulingOptions`; the threshold must be positive.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Fetch contract: change `GetDueMessagesAsync` to take a criteria record</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) `GetDueMessagesAsync(ScheduledFetchCriteria criteria, ct)` (`BatchSize`, `MaxRetries`, `OrderByPriority`, `StarvationCutoffUtc`)** | Mirrors the outbox change of #123; the next feature extends the record (row claiming, #1251) instead of the signature | One signature change on the interface, 10 stores, the fake and tests |
| **B) Add `GetDueByPriorityAsync`** | Existing callers untouched | Two fetch paths with duplicated SQL on 10 providers; pre-1.0 has no compatibility to protect |

### Chosen Option: **A — one criteria record** (recommended, pending the maintainer; the same question as #123 choice 4, answered once there)

### Rationale

- Option A wins pre-1.0 where the existing method is changed completely rather than duplicated (AGENTS.md section 1); option B wins if the maintainer picks B for the outbox.
- `SchedulerOrchestrator.GetPendingCountAsync` passes `BatchSize = int.MaxValue` and `OrderByPriority = false`.
- The compile-safe delivery (transitional default members, removed last) is the one of #123 register entry R9 and the answer is shared.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Which issue delivers the scheduled-store priority: #301 or #147 (Dilemma D6)</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) #301 delivers it with this plan; #147 is closed as duplicate after the merge** | One design with the family (aged-first, resolver, criteria record); the analysis of #123 and #301 treats #147 as duplicate scope | #147's own plan (options records of #1222, no starvation rule) is withdrawn |
| **B) #147 delivers it with its plan; #301 is narrowed to the per-priority batching question (choice 2)** | #147 builds on the draft and options records of #1222 | The scheduled side would carry no starvation rule unless #147's choice 3 answers B; two plans must be reconciled |

### Chosen Option: **A — #301 delivers it** (recommended, pending the maintainer)

### Rationale

- Option A wins when one fetch contract and one ordering rule must serve outbox and scheduler. Option B wins when #1222 merges first and the maintainer wants priority set through its options records.
- Only one of the two plans is ever implemented. Whatever the answer, both use the type from PR-A of #123, the shared error code, the column types and default of #123 register entry R2, and never create `MessagePriority` themselves.

**Class:** Dilemma

</details>

### Decision register (decisions not covered by choices 1 to 6)

Class is Trivial, Settled (verifiable citation excludes every other option) or Dilemma (the maintainer decides; recommendation only).

| Id | Decision | Class | Text |
| --- | --- | --- | --- |
| S1 | `StarvationThreshold` default 5 minutes | Dilemma (answered with #123 choice 3) | Same default as the outbox; a different default would split one rule in two |
| S2 | Column `Priority`: `TINYINT NOT NULL DEFAULT 2` on SQL Server and MySQL, `SMALLINT NOT NULL DEFAULT 2` on PostgreSQL, EF Core `HasConversion<byte>()`, MongoDB `int32` | Trivial | Same as #123 R2; the enum has five members |
| S3 | Index `(ProcessedAtUtc, ScheduledAtUtc, Priority)` | Trivial | The leading columns serve the existing due predicate and the sort is applied on the filtered set; any index serving the predicate is equivalent in behavior; the existing EF benchmark class (migrated in K) can compare it |
| S4 | The priority reaches the row through the mutable `IScheduledMessage.Priority` set by the orchestrator after `IScheduledMessageFactory.Create`; the factory signature does not change | Dilemma (recommended: A, pending the maintainer; answered with #123 R10) | B is a new `priority` parameter on `Create` (10 factories and their test callers change) |
| S5 | MongoDB runs two ordered find queries (aged group, then the rest up to the remaining batch size) with the sort of #123 R7 and concatenates; the aggregation `$addFields` with `$cond` then `$sort` could express the aged expression in one query, but it cannot use the compound index for both groups | Trivial | Both return the same order; performance only |
| S6 | `GetPendingCountAsync` stays unordered (`OrderByPriority = false`, `BatchSize = int.MaxValue`) | Trivial | It only counts; the health-check follow-up replaces the load-all-rows implementation |
| S7 | Metric names `encina.scheduling.priority.processed`, `encina.scheduling.priority.wait_time`, `encina.scheduling.priority.aged`, tag `priority` with five bounded values | Trivial | Follow the `encina.scheduling.*` names of `SchedulingProcessorMetrics` |
| S8 | Undefined priority: `Left` `messaging.invalid_priority` (`ErrorCategory.Validation`) fails `ScheduleAsync` and `ScheduleRecurringAsync` | Dilemma (answered with #123 R4) | Defaulting to `Normal` is the other option |
| S9 | Backlog per priority for health checks | Defer | Follow-up issue to be opened (draft: `artifacts/decisions/plan-batch/followups/outbox-priority-pending-depth.md`, which covers scheduling too) |

---

## Implementation Phases

### Delivery order and size

All of it starts after PR-A of #123 has merged (it creates `MessagePriority`, the resolver and the shared error code) and uses the answers of #123 to choices 1 to 4 and entries R9 and R10. The contract change (`IScheduledMessageStore.GetDueMessagesAsync(ScheduledFetchCriteria, ct)`, `IScheduledMessage.Priority`) is delivered with the same compile-safe chain as #123 (transitional default members first, one provider per pull request, callers migrated, old members removed last). Counts include tests, coverage manifests and PublicAPI files; every pull request has at most 15 files and 1500 lines. Existing callers measured on origin/main: 16 `src/` files and 40 files under `tests/` mention `GetDueMessagesAsync` (a few are `.md` docs).

| PR | Content (phases) | src and builders | Tests | PublicAPI | Manifests | Total |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| A | `ScheduledFetchCriteria`, `SchedulingPriorityOptions`, `SchedulingOptions`, `IScheduledMessage.Priority` and the criteria fetch as default members (Phase 1 tasks 1, 2, 4) | 5 | 3 | 1 | 1 | 10 |
| B | Fake message and store, `InstrumentedScheduledMessageStore`, faker, builder (Phase 2, without the helper and arbitrary) | 5 | 4 | 2 | 3 | 14 |
| C | `SchedulerOrchestrator` resolves the priority, sets it, builds the criteria only when enabled (old fetch otherwise); log class, EventIds 6708-6711, metrics, activity tag (Phase 1 task 3, Phase 6) | 5 | 3 | 2 | 2 | 12 |
| D, E, F | ADO.NET SQL Server, PostgreSQL, MySQL, one per database: message, factory, store, two scripts; guard, integration and factory tests (Phase 3) | 5 each | 3 each | 1 each | 1 each | 10 each |
| G, H, I | Dapper SQL Server, PostgreSQL, MySQL, one per database (Phase 4) | 5 each | 4, 4, 3 | 1 each | 1 each | 11, 11, 10 |
| J | EF Core (one implementation for three providers): message, factory, configuration, store; three unit files, four integration files, `ScheduledMessageStoreEFTestsBase` (Phase 5 task 1) | 4 | 8 | 1 | 1 | 14 |
| K | MongoDB (message, factory, store, `MongoDbIndexCreator`) with integration and session tests, plus the EF guard, contract and benchmark callers (Phase 5 task 2) | 4 | 5 | 1 | 1 | 11 |
| L | `SchedulerOrchestrator`, `SchedulingHealthCheck` and `SchedulingTestHelper` always use the criteria fetch; 7 mock-based test files migrate (`SchedulerOrchestratorTests`, `SchedulerOrchestratorGuardTests`, `SchedulingHealthCheckTests`, `HealthChecksTests`, `HealthCheckBuilderExtensionsTests`, `SchedulingTestHelperTests`, `ErrorMessageLeakTests`) | 3 | 7 | 0 | 2 | 12 |
| M | Remove the `IScheduledMessage.Priority` default; `ArbitraryMessages` generator; the two instrumented-store test files; property test; load-test justification `.md` update | 2 | 4 | 2 | 1 | 9 |
| N | Remove the old fetch from `IScheduledMessageStore`, the 10 stores, the fake and the decorator | 13 | 0 | 2 | 0 | 15 |
| O | Documentation and changelog fragment (`docs-writer`, `mechanical-fixer`) | - | - | - | - | 4 |
| **Total** | 15 pull requests | | | | | **163** |

Sum: A 10 + B 14 + C 12 = 36; D to F 30; G to I 32; J 14; K 11; L 12; M 9; N 15; O 4; 36 + 30 + 32 + 14 + 11 + 12 + 9 + 15 + 4 = 163. By category: 71 source files, 54 test files, 17 PublicAPI files, 17 manifests, 4 documentation files (the sum of the table columns).

**Tests and targets ship with the code.** Each pull request carries its unit, guard and integration tests (priority ordering and the recurring-series case in the existing integration classes, shared collections), enters per-file targets with a one-sentence justification in the package manifests (`mechanical-fixer`), and reports measured per-flag coverage. New files: `ScheduledFetchCriteria.cs` (unit 90, guard 0: positional record, no parameter checks), `SchedulingPriorityOptions.cs` (unit 95, guard 60: unit tests assert defaults and rejected values, guard tests reject a non-positive threshold), `SchedulingPriorityLogMessages.cs` (unit 70, guard 0: source-generated, asserted through orchestrator tests with a fake logger). Existing targets never go down; `Encina.Testing` changes (`SchedulingTestHelper`) enter their targets in L.

### Phase 1: Core Contracts, Options and Orchestrator

<details>
<summary><strong>Tasks</strong></summary>

Pull request A (tasks 1, 2, 4) and C (task 3). The transitional members are default interface members, removed by pull requests M and N.

1. `src/Encina.Messaging/Scheduling/IScheduledMessage.cs`: `MessagePriority Priority { get; set; }` with a default implementation (getter `Normal`, setter ignored) until M.
2. `src/Encina.Messaging/Scheduling/IScheduledMessageStore.cs`: add `GetDueMessagesAsync(ScheduledFetchCriteria criteria, CancellationToken)` as a default member that calls the old `GetDueMessagesAsync(batchSize, maxRetries, ct)`; new record `ScheduledFetchCriteria`. N removes the old method and the default.
3. `src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs` (pull request C): resolve the priority in `ScheduleAsync` and `ScheduleRecurringAsync` through `MessagePriorityResolver` (a `Left` with `messaging.invalid_priority`, `ErrorCategory.Validation`, for an undefined value, register entry S8) and set it on the message after `IScheduledMessageFactory.Create` (the factory signature does not change, S4); when `Priority.Enabled`, build the criteria with `TimeProvider` (cutoff = now minus threshold) and call the criteria fetch, otherwise keep the old fetch so existing mock-based tests pass (L makes it unconditional); `GetPendingCountAsync` keeps the unordered path (S6).
4. `src/Encina.Messaging/Scheduling/SchedulingOptions.cs`: `SchedulingPriorityOptions Priority { get; }` and validation.
5. `PublicAPI.Unshipped.txt` and manifest entries through `mechanical-fixer`, in each pull request.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Plan docs/plans/scheduling-priority-implementation-plan-301.md. PR-A of #123 already merged MessagePriority, MessagePriorityResolver and MessagePriorityErrorCodes in src/Encina.Messaging/Priority; never create them here. The scheduler fetches due rows through IScheduledMessageStore.GetDueMessagesAsync(batchSize, maxRetries, ct).
TASK: Implement Phase 1 Tasks 1 to 4 in the pull request the brief names (A or C). Do not alter IsDue() or the due filter.
KEY RULES: Either<EncinaError, T>, Left for an invalid priority (ErrorCategory.Validation, ADR 062); time from TimeProvider, stores never read the clock (AGENTS.md section 3); the default members exist only until pull requests M and N; options validated in SchedulingOptions; XML docs.
REFERENCE FILES: src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs, IScheduledMessageStore.cs, SchedulingOptions.cs, src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs (the #123 pattern).
```

</details>

### Phase 2: Testing Packages

<details>
<summary><strong>Tasks</strong></summary>

Pull request B (tasks 1 and 2a), L (`SchedulingTestHelper`) and M (`ArbitraryMessages`).

1. `src/Encina.Testing.Fakes/Models/FakeScheduledMessage.cs` and `Stores/FakeScheduledMessageStore.cs`: priority and an override of the criteria fetch with the same ordering as the SQL; `src/Encina.OpenTelemetry/MessagingStores/InstrumentedScheduledMessageStore.cs` forwards the criteria fetch (a decorator that inherited the default member would drop the priority).
2. `src/Encina.Testing.Bogus/ScheduledMessageFaker.cs` and the `ScheduledMessageBuilder` of `tests/Encina.TestInfrastructure`: `WithPriority`. `SchedulingTestHelper` (calls the due fetch) moves in L, `ArbitraryMessages` generator in M.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: IScheduledMessage now has Priority; the due fetch takes ScheduledFetchCriteria.
TASK: Update the fake store, fake message, faker, helper and arbitrary so existing tests compile and new tests can set priority.
KEY RULES: the fake orders exactly like the SQL (aged rows first, then Priority DESC, then ScheduledAtUtc ascending) so the ordering contract test and the integration cases compare like with like; FakeTimeProvider; no reflection-only tests.
REFERENCE FILES: src/Encina.Testing.Fakes/Stores/FakeScheduledMessageStore.cs, src/Encina.Testing.Bogus/ScheduledMessageFaker.cs.
```

</details>

### Phase 3: ADO.NET Providers (SqlServer, PostgreSQL, MySQL)

<details>
<summary><strong>Tasks</strong></summary>

One pull request per database (D SQL Server, E PostgreSQL, F MySQL). Each: `Scheduling/ScheduledMessage.cs` (property), `Scheduling/ScheduledMessageFactory.cs` (default priority `Normal`, signature unchanged), `Scheduling/ScheduledMessageStoreADO.cs` (insert, reader, override of the criteria fetch: `TOP (@BatchSize)` or `LIMIT @BatchSize`, ordering of choice 1 when `OrderByPriority`), `Scripts/004_CreateScheduledMessagesTable.sql` and `Scripts/000_CreateAllTables.sql` (register entries S2 and S3), plus that database's guard and integration tests (the integration class gains the ordering and recurring-series cases) and a factory test.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: ADO scheduled stores write SQL by hand; SQL Server uses TOP (@n) and brackets, PostgreSQL lower-case columns and LIMIT, MySQL LIMIT and backticks.
TASK: Apply Phase 3 Tasks to the three ADO.NET packages, one package per commit.
KEY RULES: async with CancellationToken only; parameters; keep the existing due filter (ScheduledAtUtc <= @NowUtc, retries) untouched; same ordering semantics on all three; no migration helper.
REFERENCE FILES: src/Encina.ADO.SqlServer/Scheduling/ScheduledMessageStoreADO.cs, src/Encina.ADO.PostgreSQL/Scripts/004_CreateScheduledMessagesTable.sql, src/Encina.ADO.MySQL/Scheduling/ScheduledMessageStoreADO.cs.
```

</details>

### Phase 4: Dapper Providers (SqlServer, PostgreSQL, MySQL)

<details>
<summary><strong>Tasks</strong></summary>

One pull request per database (G SQL Server, H PostgreSQL, I MySQL): the same five source files as Phase 3 with `ScheduledMessageStoreDapper.cs` (`SELECT TOP (@BatchSize) * ... ORDER BY ScheduledAtUtc` becomes the criteria-driven query) and the existing unit, guard and integration test files of that database that call the due fetch.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT: Dapper scheduled stores select * mapped to the provider ScheduledMessage class.
TASK: Apply Phase 4 to the three Dapper packages, mirroring Phase 3.
KEY RULES: same as Phase 3; the property name must match the column (PostgreSQL lower-case convention).
REFERENCE FILES: src/Encina.Dapper.SqlServer/Scheduling/ScheduledMessageStoreDapper.cs, src/Encina.Dapper.PostgreSQL/Scheduling/ScheduledMessageStoreDapper.cs, src/Encina.Dapper.MySQL/Scheduling/ScheduledMessageStoreDapper.cs.
```

</details>

### Phase 5: EF Core (3 providers) and MongoDB

<details>
<summary><strong>Tasks</strong></summary>

1. Pull request J: `src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessage.cs`, `ScheduledMessageFactory.cs`, `ScheduledMessageConfiguration.cs` (default 2 and the index), `ScheduledMessageStoreEF.cs` (aged expression, `Priority`, `ScheduledAtUtc`); the 3 unit, 4 integration and base test files are migrated and the three EF providers each run the ordering cases (the translation of the aged expression is tested on SqlServer, PostgreSQL and MySQL).
2. Pull request K: `src/Encina.MongoDB/Scheduling/ScheduledMessage.cs` (`[BsonElement("priority")]`), `ScheduledMessageFactory.cs`, `ScheduledMessageStoreMongoDB.cs` (register entry S5), and the compound index in `MongoDbIndexCreator.cs`; also the EF guard, contract and benchmark callers.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT: One EF Core store serves SqlServer, PostgreSQL and MySQL; MongoDB runs two ordered find queries and concatenates so each uses the compound index (register entry S5; an aggregation with $cond could express the aged expression in one query but not use the index for both groups).
TASK: Implement Phase 5 Tasks 1 and 2.
KEY RULES: the EF query must translate on all three providers (test all three); indexes in MongoDbIndexCreator only; async with CancellationToken.
REFERENCE FILES: src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessageStoreEF.cs, ScheduledMessageConfiguration.cs, src/Encina.MongoDB/Scheduling/ScheduledMessageStoreMongoDB.cs.
```

</details>

### Phase 6: Cross-Cutting Integration and Observability

<details>
<summary><strong>Tasks</strong></summary>

All in pull request C (the first that logs, so it creates the log class and registers the range).

1. `src/Encina/Diagnostics/EventIdRanges.cs`: register `MessagingPriorityScheduling = (6708, 6711)`, the 4 ids used, the first free ids of the family block 6700-6749 after the 6700-6707 of #123; add it to the `Encina.Messaging` entry in `EncinaEventIdAllocationTests`.
2. `src/Encina.Messaging/Diagnostics/SchedulingPriorityLogMessages.cs` (`[LoggerMessage]`, 6708 to 6711): priority resolved (type name only), invalid priority rejected (code only), priority fetch enabled at start-up, aged messages fetched (count only; the orchestrator counts the returned messages with `ScheduledAtUtc <= cutoff`).
3. `SchedulingProcessorMetrics.cs` on the shared `"Encina"` meter: the three instruments of register entry S7; `SchedulingActivitySource` schedule activity gains the tag `encina.scheduling.priority`.
4. `InstrumentedScheduledMessageStore` forwards the criteria fetch in pull request B, not here.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT: ADR-021: register the range first (the family block is 6700-6749 in the batch registry; #123 registers 6700-6707 and this plan 6708-6711). Metric names follow the encina.scheduling.* prefix of SchedulingProcessorMetrics.
TASK: Implement Phase 6 Tasks 1 to 3, packing EventIds 6708-6711 sequentially.
KEY RULES: [LoggerMessage] source generator; no payload, no EncinaError.Message in logs, tags or metrics; run the EventId architecture tests.
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs, src/Encina.Messaging/Diagnostics/SchedulingProcessorMetrics.cs, SchedulingActivitySource.cs.
```

</details>

### Phase 7: Testing

<details>
<summary><strong>Tasks</strong></summary>

Tests ship with the pull request that owns the code (see the delivery table).

1. Unit: priority resolution at `ScheduleAsync` and `ScheduleRecurringAsync` (attribute, interface wins, default, invalid), criteria with `FakeTimeProvider`, `GetPendingCountAsync` unordered path, options validation (pull requests A and C); factory output per provider (D to K).
2. Guard (AGENTS.md section 9, #3798): every new public constructor or method with a validated parameter gets its argument tests in the pull request that adds it (options threshold in A; existing store guard classes migrate in their provider pull request, L for the orchestrator).
3. Contract: the `IScheduledMessageStore` ordering contract (future messages never returned, aged first, priority, due time; disabled mode equals `ScheduledAtUtc`) runs against the fake in B; the real stores are covered by item 5.
4. Property (pull request M): the due set returned is independent of priority (priority only reorders); no starvation beyond the threshold plus one batch.
5. Integration: both ordering modes on all 10 providers in the shared collections, each in its provider pull request (D to K); a recurring message keeps its priority after `RescheduleRecurringMessageAsync`; never a `.md` for database integration tests.
6. Load and benchmark: the load-test `.md` justification (existing `ScheduledMessageProcessorLoadTests.md`) is updated in M (single ordered query); the EF benchmark class migrates in K; no new benchmark class is added (the load and benchmark obligations are justified: one ordered query).
7. Coverage: per-file targets in `Encina.Messaging.json` and provider manifests (`mechanical-fixer`) in the same pull request as the tests that meet them; CRAP at most 10 on changed methods (`SchedulerOrchestrator.ProcessDueMessagesAsync`, pull requests C and L).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT: AGENTS.md section 9: per-flag coverage, real code, Shouldly through Encina.Testing.Shouldly, shared Collection fixtures.
TASK: Write the tests of Phase 7 Tasks 1 to 5 and the justification files of Task 6; report per-flag coverage against the manifest.
KEY RULES: AAA, FakeTimeProvider, no Thread.Sleep; all 10 providers; SQLite excluded.
REFERENCE FILES: tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Scheduling/ScheduledMessageStoreEFIntegrationTests.cs, tests/Encina.IntegrationTests/Infrastructure/MongoDB/Stores/ScheduledMessageStoreMongoDBIntegrationTests.cs, tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Scheduling/SchedulerCycleScenarios.cs.
```

</details>

### Phase 8: Documentation and Finalization

<details>
<summary><strong>Tasks</strong></summary>

Pull request O. XML docs and PublicAPI lines ship with each code pull request.

1. `changelog.d/301-scheduling-priority.added.md` (`mechanical-fixer`).
2. `docs-writer`: add the scheduling section to the priority guide created by #123; package README; `docs/INVENTORY.md` if needed. The ADR is the one of #123.
3. Orchestrator: act on the answer to Dilemma D6 (close #147 as duplicate if option A).
4. `dotnet build --configuration Release` with zero warnings; `dotnet test`; every flag at its target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```text
CONTEXT: documentation, changelog fragments and PublicAPI lines have path owners (docs-writer, mechanical-fixer).
TASK: Finish Phase 8 Tasks 1 to 4.
KEY RULES: English only; coverage figures only through covref markers; "the reference application", never a private client name.
REFERENCE FILES: changelog.d/README.md, docs/architecture/adr/index.md, docs/plans/dsr-implementation-plan-404.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard or source | Relevance |
| --- | --- |
| Azure Architecture Center, Priority Queue pattern | Order work by priority; starvation needs a policy |
| SPEC-002 REQ-033 | Scheduling slices (schedule key, time zone) touch the same factory and tables |
| AGENTS.md section 3 (Scheduling), sections 5, 6, 7 | Encina scheduling carries domain messages; Hangfire and Quartz are separate; 10 providers; EventIds |
| ADR-001, ADR-006, ADR-018, ADR-021, ADR-050 | ROP, cross-cutting check, EventIds, module dependencies |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
| --- | --- | --- |
| `IScheduledMessage`, `IScheduledMessageStore`, `IScheduledMessageFactory` | `src/Encina.Messaging/Scheduling/` | Gain `Priority` and the criteria fetch |
| `SchedulerOrchestrator` | `src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs` | Resolves priority, builds the criteria |
| `SchedulingOptions` | `src/Encina.Messaging/Scheduling/SchedulingOptions.cs` | Hosts `SchedulingPriorityOptions` |
| `MessagePriority`, `MessagePriorityResolver` (from #123) | `src/Encina.Messaging/Priority/` | Shared type and resolution |
| `SchedulingProcessorMetrics`, `SchedulingActivitySource` | `src/Encina.Messaging/Diagnostics/` | Metrics and tag |
| Provider scheduled stores, scripts, configuration | `src/Encina.ADO.*`, `Encina.Dapper.*`, `Encina.EntityFrameworkCore`, `Encina.MongoDB` | Column, index, ordered due fetch |
| `SchedulingHealthCheck` | `src/Encina.Messaging/Health/SchedulingHealthCheck.cs` | Backlog per priority is deferred |

### Event ID Allocation

| Package | Range | Notes |
| --- | --- | --- |
| `Encina.Messaging` (`EventIdRanges.MessagingPriorityScheduling`, new) | 6708-6711 | From the family block 6700-6749; exactly the 4 ids used, packed after the 6700-6707 of #123 (registered by that plan). #251 takes the ids after these (see its plan); #469 and #1254 use the rest of the block |

### Estimated File Count

The authoritative count is the "Delivery order and size" table (163 file changes in 15 pull requests).

| Category | File changes | Notes |
| --- | ---: | --- |
| Source and builders | 71 | Core contracts, orchestrator, 9 SQL providers, EF Core, MongoDB, fakes, decorator, removals (N 13) |
| Tests | 54 | Unit, guard, integration, contract, property, load justification |
| PublicAPI | 17 | One per package per pull request that adds or removes a public member |
| Manifests | 17 | Per package per pull request |
| Documentation and changelog | 4 | Pull request O |
| Total | 163 | Equals the delivery table |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined Prompt — All Phases</strong></summary>

```text
PROJECT CONTEXT: Encina, pre-1.0, .NET 10 / C# 14. Issue #301, plan docs/plans/scheduling-priority-implementation-plan-301.md. It applies the shared priority design of #123 to scheduled messages; PR-A of #123 (the type) must be merged first (order: #1254, #123, #301, #469). Read AGENTS.md first.

IMPLEMENTATION OVERVIEW: (1) Contracts as transitional default members, options (A). (2) Fakes, decorator, builders (B). (3) Orchestrator resolution, EventIds 6708-6711, metrics (C). (4) ADO.NET x3 (D-F), Dapper x3 (G-I), EF Core (J), MongoDB (K), one pull request each. (5) Orchestrator unconditional and caller migration (L), default removal and property test (M), old fetch removal (N). (6) Docs (O). Fifteen pull requests of at most 15 files and 1500 lines, each leaving main building with its own tests and manifest targets. Only one of this plan and the #147 plan is implemented (Dilemma D6).

KEY PATTERNS: Either<EncinaError, T> (ErrorCategory per ADR 062); time from TimeProvider and the cutoff computed in the orchestrator; priority orders only rows that are already due; recurring series keep their priority; ORDER BY aged rows first, then Priority DESC, then ScheduledAtUtc when enabled and ScheduledAtUtc otherwise; opt-in via SchedulingOptions.Priority.Enabled = false; [LoggerMessage] with a registered range; shared "Encina" meter; async database calls; shared [Collection] fixtures.

REFERENCE FILES: src/Encina.Messaging/Scheduling/*.cs, src/Encina.Dapper.SqlServer/Scheduling/ScheduledMessageStoreDapper.cs, src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessageStoreEF.cs, src/Encina.MongoDB/Scheduling/ScheduledMessageStoreMongoDB.cs, src/Encina/Diagnostics/EventIdRanges.cs, docs/plans/dsr-implementation-plan-404.md.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Ordered fetch of pending work; nothing to cache |
| 2 | OpenTelemetry | ✅ | `encina.scheduling.priority.*` counters and histogram, activity tag (Phase 6) |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` 6708-6711 in `EventIdRanges.MessagingPriorityScheduling` (Phase 6) |
| 4 | Health Checks | ⏭️ | Backlog per priority shares the follow-up issue to be opened (draft: `artifacts/decisions/plan-batch/followups/outbox-priority-pending-depth.md`, which covers scheduling); `GetPendingCountAsync` loading every due row is noted there |
| 5 | Validation | ✅ | `SchedulingPriorityOptions` validated; undefined priority returns `Left` (Phase 1) |
| 6 | Resilience | ❌ | No external call is added; retry policy (`ExponentialBackoffRetryPolicy`) is unchanged |
| 7 | Distributed Locks | ⏭️ | Row claiming for several instances is #1251; it is also the prerequisite of per-priority loops (choice 2, option B) |
| 8 | Transactions | ✅ | The priority is stored in the same `AddAsync` and transaction as the scheduled row (Phase 1) |
| 9 | Idempotency | ⏭️ | Re-execution idempotency is #735 and #148; unchanged here |
| 10 | Multi-Tenancy | ⏭️ | `TenantId` on `ScheduledMessage` is #739; ordering is global until then |
| 11 | Module Isolation | ⏭️ | `ModuleId` on messaging entities is #747 |
| 12 | Audit Trail | ❌ | No compliance-relevant data is created |

---

## Prerequisites & Dependencies

- PR-A of #123 merged (shared type, resolver, error code). The options pattern and fetch-record pattern are those of #123. Which merges first among #123 and the scheduling slices (#146, #148, #150) is for the orchestrator to order, because they change the scheduled tables (this plan does not change `IScheduledMessageFactory.Create`, entry S4).
- #1251 only for per-priority loops, which this plan does not build (choice 2).

## Next Steps

1. Maintainer answers choices 1 to 6 and entries S1, S4, S8 when work starts (choices 1, 4 and 5 and entries S1, S4, S8 repeat the answers of #123); answers go to `## Maintainer Decisions`.
2. Orchestrator acts on D6 (close #147 as duplicate of this issue if A).
3. Worker briefs per pull request (PR-A to PR-O) with a recount of existing callers.

## Maintainer Decisions

None yet. Pending: every Dilemma above (choices 1 to 6, entries S1, S4, S8).
