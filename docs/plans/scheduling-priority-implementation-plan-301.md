# Implementation Plan: Priority for Scheduled Messages (and per-priority batching question) (`Encina.Messaging` + 10 database providers)

> **Issue**: [#301](https://github.com/dlrivada/Encina/issues/301)
> **Type**: Feature
> **Complexity**: Medium (8 phases, 10 database providers, about 65 files, delivered in 5 pull requests)
> **Estimated Scope**: about 800 lines of production code + about 1,000 lines of tests
> **Family**: message priority and partitioning (#123, #251, #301, #469, #1254). The shared priority design is OWNED by #123; this plan applies it to the scheduled-message store and decides only what is specific to scheduling and to per-priority batching. #147 (priority for scheduled messages) is duplicate scope of this issue and is closed against it.

---

## Summary

#301 asks for priority in both the outbox and scheduling, with per-priority batch sizes and processing intervals. After the family analysis its scope is:

- **Outbox side**: delivered by #123 (type, attribute, column, ordered fetch, starvation). This plan adds nothing there.
- **Scheduling side (this plan)**: `IScheduledMessage.Priority`, a priority column on the scheduled-message table of all 10 providers, a priority-aware due fetch, and priority resolution when a request is scheduled.
- **Per-priority batching and intervals (this plan, Dilemma)**: whether to run one loop per priority level or one ordered query.

Facts verified in today's code:

- `IScheduledMessageStore.GetDueMessagesAsync(int batchSize, int maxRetries, CancellationToken)` returns `Either<EncinaError, IEnumerable<IScheduledMessage>>`; stores order by `ScheduledAtUtc` (for example `src/Encina.Dapper.SqlServer/Scheduling/ScheduledMessageStoreDapper.cs`: `SELECT TOP (@BatchSize) * ... ORDER BY ScheduledAtUtc`).
- `SchedulerOrchestrator` calls it in `ProcessDueMessagesAsync` with `_options.BatchSize` and again in `GetPendingCountAsync` with `int.MaxValue` (it loads every due row only to count them; that path must keep working unchanged and is noted for the health-check follow-up).
- `IScheduledMessageFactory.Create(id, requestType, content, scheduledAtUtc, createdAtUtc, isRecurring, cronExpression)` is implemented by one factory per provider package.
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
- The aged-first rule is applied to the overdue time: a due message whose `ScheduledAtUtc` is older than the starvation cutoff sorts first (oldest first), then `Priority DESC`, then `ScheduledAtUtc`. A message that is not yet due is never returned, so priority cannot make a future message run early.
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
<summary><strong>3. Scheduling-specific semantics: priority never changes when a message is due, and recurring messages keep it</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) Priority only orders messages that are already due; recurring occurrences inherit the priority of the series** | Matches the meaning of "priority queue" and of `IScheduledMessage.IsDue()`; `RescheduleRecurringMessageAsync` keeps the row, so the column persists with no extra code | A high-priority message is still late if the processor is late |
| **B) Priority shifts the due time (high priority runs earlier than scheduled)** | Appears to speed critical work | Changes the contract of `ScheduledAtUtc`; surprising and unsafe for time-bound jobs |
| **C) A different priority per occurrence of a recurring series** | Flexible | Needs a priority source per occurrence; no use case in the issues |

### Chosen Option: **A — order only the due set; the series keeps one priority**

### Rationale

- `IsDue()` and the `ScheduledAtUtc <= now` filter stay exactly as they are; the new `ORDER BY` only changes the order of rows that pass the existing filter.
- A scheduled message is a request, not a notification: the priority is resolved from the request type or instance by `MessagePriorityResolver` (#123 choice 2) in `SchedulerOrchestrator.ScheduleAsync` and `ScheduleRecurringAsync` before `IScheduledMessageFactory.Create`.

**Class:** Settled
**Settled by:** `IScheduledMessage.IsDue()` and the existing due filter in `src/Encina.Messaging/Scheduling/IScheduledMessage.cs` and the provider stores (the contract this plan must not change); issue #301 Alternatives, item 3 (time-based prioritization is not priority).

</details>

<details>
<summary><strong>4. Opt-in and defaults</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) `SchedulingOptions.Priority` (`SchedulingPriorityOptions`): `Enabled = false`, `StarvationThreshold = 5 minutes`; column always present** | Disabled by default; one schema; switching it on later orders rows already stored | Unused column when disabled |
| **B) Reuse the outbox's `OutboxPriorityOptions` instance** | One setting | Outbox and scheduler are separate patterns that users enable separately |

### Chosen Option: **A — separate options object per pattern, same shape**

### Rationale

- Every messaging pattern is optional and disabled by default (AGENTS.md section 3, ADR-050); the issue's `EnablePriorityProcessing` flag becomes `SchedulingOptions.Priority.Enabled`.
- Validation lives with the other cross-property rules of `SchedulingOptions`; the threshold must be positive.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (every messaging pattern optional and disabled by default; ADR-050).

</details>

<details>
<summary><strong>5. Fetch contract: change `GetDueMessagesAsync` to take a criteria record</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) `GetDueMessagesAsync(ScheduledFetchCriteria criteria, ct)` (`BatchSize`, `MaxRetries`, `OrderByPriority`, `StarvationCutoffUtc`)** | Mirrors the outbox change of #123; the next feature extends the record (row claiming, #1251) instead of the signature | One signature change on the interface, 10 stores, the fake and tests |
| **B) Add `GetDueByPriorityAsync`** | Existing callers untouched | Two fetch paths with duplicated SQL on 10 providers; pre-1.0 has no compatibility to protect |

### Chosen Option: **A — one criteria record** (this follows #123 choice 4; the answer is recorded there)

### Rationale

- Pre-1.0 the existing method is changed completely rather than duplicated (AGENTS.md section 1).
- `SchedulerOrchestrator.GetPendingCountAsync` passes `BatchSize = int.MaxValue` and `OrderByPriority = false`.

**Class:** Trivial
**Settled by:** AGENTS.md section 1 (pre-1.0: no compatibility layer, change completely) and the same choice recorded as choice 4 of the #123 plan.

</details>

---

## Implementation Phases

Five pull requests of at most 15 files and 1500 lines: PR-A phases 1 and 2 (core and fakes), PR-B phase 3 (ADO.NET), PR-C phase 4 (Dapper), PR-D phase 5 (EF Core and MongoDB), PR-E phases 6 to 8. All of it starts after #123 merges, because it reuses `MessagePriority`, the resolver and the options pattern.

### Phase 1: Core Contracts, Options and Orchestrator

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Messaging/Scheduling/IScheduledMessage.cs`: `MessagePriority Priority { get; set; }`.
2. `src/Encina.Messaging/Scheduling/IScheduledMessageStore.cs`: `GetDueMessagesAsync(ScheduledFetchCriteria criteria, CancellationToken)`; new record `ScheduledFetchCriteria`.
3. `src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs`: `IScheduledMessageFactory.Create(..., MessagePriority priority)`; resolve the priority in `ScheduleAsync` and `ScheduleRecurringAsync` (a `Left` for an undefined value, code `scheduling.invalid_priority` in `SchedulingErrorCodes`); build the criteria with `TimeProvider` (cutoff = now minus threshold, only when enabled); `GetPendingCountAsync` uses the unordered criteria.
4. `src/Encina.Messaging/Scheduling/SchedulingOptions.cs`: `SchedulingPriorityOptions Priority { get; }` and validation.
5. `PublicAPI.Unshipped.txt` through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Plan docs/plans/scheduling-priority-implementation-plan-301.md. #123 already merged MessagePriority, MessagePriorityResolver and OutboxPriorityOptions in src/Encina.Messaging/Priority and Outbox. The scheduler fetches due rows through IScheduledMessageStore.GetDueMessagesAsync(batchSize, maxRetries, ct).
TASK: Implement Phase 1 Tasks 1 to 4. Do not alter IsDue() or the due filter.
KEY RULES: Either<EncinaError, T>, Left for an invalid priority; time from TimeProvider, stores never read the clock (AGENTS.md section 3); no compatibility overload of the old fetch; options validated in SchedulingOptions; XML docs.
REFERENCE FILES: src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs, IScheduledMessageStore.cs, SchedulingOptions.cs, src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs (the #123 pattern).
```

</details>

### Phase 2: Testing Packages

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Testing.Fakes/Models/FakeScheduledMessage.cs` and `Stores/FakeScheduledMessageStore.cs`: priority and the criteria fetch with the same ordering as the SQL.
2. `src/Encina.Testing.Bogus/ScheduledMessageFaker.cs`, `src/Encina.Testing/Messaging/SchedulingTestHelper.cs`, `src/Encina.Testing.FsCheck/ArbitraryMessages.cs`: `WithPriority` and a generator.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: IScheduledMessage now has Priority; the due fetch takes ScheduledFetchCriteria.
TASK: Update the fake store, fake message, faker, helper and arbitrary so existing tests compile and new tests can set priority.
KEY RULES: the fake orders exactly like the SQL (aged first, Priority DESC, ScheduledAtUtc) so the contract test compares them; FakeTimeProvider; no reflection-only tests.
REFERENCE FILES: src/Encina.Testing.Fakes/Stores/FakeScheduledMessageStore.cs, src/Encina.Testing.Bogus/ScheduledMessageFaker.cs.
```

</details>

### Phase 3: ADO.NET Providers (SqlServer, PostgreSQL, MySQL)

<details>
<summary><strong>Tasks</strong></summary>

For each ADO.NET package: `Scheduling/ScheduledMessage.cs` (property), `Scheduling/ScheduledMessageFactory.cs` (signature), `Scheduling/ScheduledMessageStoreADO.cs` (insert, reader, due fetch with the criteria: `TOP (@BatchSize)` or `LIMIT @BatchSize`, ordering of choice 1 when `OrderByPriority`), `Scripts/004_CreateScheduledMessagesTable.sql` and `Scripts/000_CreateAllTables.sql` (`Priority TINYINT NOT NULL DEFAULT 2` on SQL Server and MySQL, `SMALLINT NOT NULL DEFAULT 2` on PostgreSQL, index `(ProcessedAtUtc, ScheduledAtUtc, Priority)`).

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

For each Dapper package: the same five files as Phase 3 with `ScheduledMessageStoreDapper.cs` (`SELECT TOP (@BatchSize) * ... ORDER BY ScheduledAtUtc` becomes the criteria-driven query).

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

1. `src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessage.cs`, `ScheduledMessageFactory.cs`, `ScheduledMessageConfiguration.cs` (default 2 and the index), `ScheduledMessageStoreEF.cs` (aged expression, `Priority`, `ScheduledAtUtc`).
2. `src/Encina.MongoDB/Scheduling/ScheduledMessage.cs` (`[BsonElement("priority")]`), `ScheduledMessageFactory.cs`, `ScheduledMessageStoreMongoDB.cs` (aged query, then the priority-sorted remainder), and the compound index in `MongoDbIndexCreator.cs`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT: One EF Core store serves SqlServer, PostgreSQL and MySQL; MongoDB runs two ordered queries and concatenates (no single-sort equivalent of the aged CASE).
TASK: Implement Phase 5 Tasks 1 and 2.
KEY RULES: the EF query must translate on all three providers (test all three); indexes in MongoDbIndexCreator only; async with CancellationToken.
REFERENCE FILES: src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessageStoreEF.cs, ScheduledMessageConfiguration.cs, src/Encina.MongoDB/Scheduling/ScheduledMessageStoreMongoDB.cs.
```

</details>

### Phase 6: Cross-Cutting Integration and Observability

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina/Diagnostics/EventIdRanges.cs`: register `MessagingPriorityScheduling = (6715, 6724)` from the family block; add it to the `Encina.Messaging` entry in `EncinaEventIdAllocationTests`.
2. `src/Encina.Messaging/Diagnostics/SchedulingPriorityLogMessages.cs` (`[LoggerMessage]`, 6715 to 6718): priority resolved (type name only), invalid priority rejected (code only), priority fetch enabled at start-up, aged promotion applied (count only).
3. `SchedulingProcessorMetrics.cs` on the shared `"Encina"` meter: `encina.scheduling.priority.processed` (counter, tag `priority`), `encina.scheduling.priority.wait_time` (histogram, seconds from due time to execution, tag `priority`), `encina.scheduling.priority.promoted` (counter); `SchedulingActivitySource` schedule activity gains the tag `encina.scheduling.priority`.
4. `src/Encina.OpenTelemetry/MessagingStores/InstrumentedScheduledMessageStore.cs`: new fetch signature.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT: ADR-021: register the range first (6715-6724 is assigned to this plan by the batch registry). Metric names follow the encina.scheduling.* prefix of SchedulingProcessorMetrics.
TASK: Implement Phase 6 Tasks 1 to 4, packing EventIds 6715-6718 sequentially.
KEY RULES: [LoggerMessage] source generator; no payload, no EncinaError.Message in logs, tags or metrics; run the EventId architecture tests.
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs, src/Encina.Messaging/Diagnostics/SchedulingProcessorMetrics.cs, SchedulingActivitySource.cs.
```

</details>

### Phase 7: Testing

<details>
<summary><strong>Tasks</strong></summary>

1. Unit: priority resolution at `ScheduleAsync` and `ScheduleRecurringAsync` (attribute, interface wins, default, invalid), criteria with `FakeTimeProvider`, `GetPendingCountAsync` unordered path, options validation.
2. Guard: new public members.
3. Contract: `IScheduledMessageStore` ordering contract on the fake and the 10 real stores (future messages never returned, aged first, priority, due time; disabled mode equals `ScheduledAtUtc`).
4. Property: the due set returned is independent of priority (priority only reorders); no starvation beyond the threshold plus one batch.
5. Integration: both ordering modes on all 10 providers in the shared collections; a recurring message keeps its priority after `RescheduleRecurringMessageAsync`; never a `.md` for database integration tests.
6. Load and benchmark: `.md` justifications (single ordered query); a benchmark of the due fetch is optional and shares the outbox harness of #123.
7. Coverage: per-file targets in `Encina.Messaging.json` and provider manifests (`mechanical-fixer`); CRAP at most 10 on changed methods (`SchedulerOrchestrator.ProcessDueMessagesAsync`).

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

1. XML docs; `PublicAPI.Unshipped.txt` per package; `changelog.d/301-scheduling-priority.added.md` (`mechanical-fixer`).
2. `docs-writer`: add the scheduling section to the priority guide created by #123; package README; `docs/INVENTORY.md` if needed. The ADR is the one of #123.
3. Close #147 as duplicate (orchestrator).
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
| `Encina.Messaging` (`EventIdRanges.MessagingPriorityScheduling`, new) | 6715-6724 | From the family block 6700-6749; this plan uses 6715-6718. Siblings: #123 6700-6714, #251 6725-6734, #469 6735-6747, #1254 6748-6749 |

### Estimated File Count

| Category | Files | Notes |
| --- | --- | --- |
| Core new and modified | 8 | Interface, store contract and criteria record, orchestrator, options, error code, diagnostics, `EventIdRanges.cs` |
| ADO.NET (3 providers) | 15 | Message, factory, store, 2 scripts each |
| Dapper (3 providers) | 15 | Same |
| EF Core and MongoDB | 9 | EF 4 files; Mongo 4 files plus index creator |
| Testing packages and OpenTelemetry | 6 | Fakes, faker, helper, arbitrary, instrumented store |
| Tests | 22 | Unit, guard, contract, property, 10 integration, justification files |
| Docs, changelog, PublicAPI | 12 | Per package |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined Prompt — All Phases</strong></summary>

```text
PROJECT CONTEXT: Encina, pre-1.0, .NET 10 / C# 14. Issue #301, plan docs/plans/scheduling-priority-implementation-plan-301.md. It applies the shared priority design of #123 to scheduled messages; #123 must be merged first (order: #1254, #123, #301, #469). Read AGENTS.md first.

IMPLEMENTATION OVERVIEW: (1) IScheduledMessage.Priority, ScheduledFetchCriteria, SchedulingPriorityOptions, orchestrator resolution. (2) Fakes. (3) ADO.NET x3. (4) Dapper x3. (5) EF Core and MongoDB. (6) EventIds 6715-6718, metrics, activity tag. (7) Tests on 10 providers. (8) Docs; #147 closed as duplicate. Five pull requests of at most 15 files and 1500 lines.

KEY PATTERNS: Either<EncinaError, T>; time from TimeProvider and the cutoff computed in the orchestrator; priority orders only rows that are already due; recurring series keep their priority; ORDER BY aged-first, Priority DESC, ScheduledAtUtc when enabled and ScheduledAtUtc otherwise; opt-in via SchedulingOptions.Priority.Enabled = false; [LoggerMessage] with a registered range; shared "Encina" meter; async database calls; shared [Collection] fixtures.

REFERENCE FILES: src/Encina.Messaging/Scheduling/*.cs, src/Encina.Dapper.SqlServer/Scheduling/ScheduledMessageStoreDapper.cs, src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessageStoreEF.cs, src/Encina.MongoDB/Scheduling/ScheduledMessageStoreMongoDB.cs, src/Encina/Diagnostics/EventIdRanges.cs, docs/plans/dsr-implementation-plan-404.md.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Ordered fetch of pending work; nothing to cache |
| 2 | OpenTelemetry | ✅ | `encina.scheduling.priority.*` counters and histogram, activity tag (Phase 6) |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` 6715-6718 in `EventIdRanges.MessagingPriorityScheduling` (Phase 6) |
| 4 | Health Checks | ⏭️ | Backlog per priority shares the follow-up issue file of #123 (`artifacts/issues/outbox-priority-pending-depth.md`, extended to scheduling); `GetPendingCountAsync` loading every due row is noted there |
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

- #123 merged (shared type, resolver, options pattern, fetch-record pattern). Which merges first among #123 and the scheduling slices (#146, #148, #150) is for the orchestrator to order, because both change `IScheduledMessageFactory.Create` and the scheduled tables.
- #1251 only for per-priority loops, which this plan does not build (choice 2).

## Next Steps

1. Maintainer answers choices 1 and 2 when work starts (choice 1 repeats the answer of #123); answers go to `## Maintainer Decisions`.
2. Orchestrator closes #147 as duplicate of this issue and narrows #301 to the scheduled side plus the answer on loops.
3. Worker briefs per pull request (PR-A to PR-E).
