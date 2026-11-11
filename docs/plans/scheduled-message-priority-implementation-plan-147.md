# Implementation Plan: Scheduled messages priority queue support

> **Issue**: [#147](https://github.com/dlrivada/Encina/issues/147)
> **Type**: Feature
> **Complexity**: Medium (7 phases, all 10 database providers, one column, one index, one ordered query)
> **Estimated Scope**: ~500 lines of production code + ~1,000 lines of tests, ~45 files in 4 sub-pull requests
> **Family**: scheduling-1 (adds a column to the row #1222 reshapes; the shared priority type is the subject of #123)

---

## Summary

When several scheduled messages are due at the same time, the higher-priority ones should be dispatched first. Checked against the tree:

- Nothing exists: no `Priority` on `IScheduledMessage`, no priority enum anywhere in `src/` (the only priority enum is `CachePriority` in `Encina.Caching`).
- `IScheduledMessageStore.GetDueMessagesAsync(int batchSize, int maxRetries, ct)` returns `Either<EncinaError, IEnumerable<IScheduledMessage>>`. The stores order by `ScheduledAtUtc` only: `ORDER BY ScheduledAtUtc` in `ScheduledMessageStoreDapper.cs` and the ADO stores, `OrderBy(m => m.ScheduledAtUtc)` in `ScheduledMessageStoreEF.cs`, `SortBy(m => m.ScheduledAtUtc)` in `ScheduledMessageStoreMongoDB.cs`.
- `SchedulerOrchestrator.ScheduleAsync` has overloads `(request, DateTime, ct)` and `(request, TimeSpan, ct)` and no priority argument; #1222 adds `ScheduleOptions` and `RecurringOptions` records to carry per-call settings.
- The existing index `IX_ScheduledMessages_ScheduledAt_Processed` (SQL Server script `004_CreateScheduledMessagesTable.sql`) is `(ScheduledAtUtc, ProcessedAtUtc, RetryCount)` with included columns, which cannot serve `ORDER BY Priority DESC, ScheduledAtUtc`.

This plan adds a `Priority` column and property, orders the due fetch by `Priority DESC, ScheduledAtUtc ASC` among messages that are already due (the due filter stays first), replaces the index, lets callers set the priority through the options records of #1222, and records processed counts and wait time per priority. The outbox and transport priorities (#123, #301, #251) are separate features; the type shared with them is Dilemma 1.

**Provider category**: Database (10): ADO.NET x3, Dapper x3, EF Core x3 (SqlServer, PostgreSQL, MySQL), MongoDB.

**Affected packages**: `Encina.Messaging`, the nine SQL provider packages, `Encina.EntityFrameworkCore`, `Encina.MongoDB`, `Encina.OpenTelemetry` (decorator), `Encina.Testing.Fakes`, `Encina.Testing.Bogus`.

**Split** (implementations first, interface last, as in #1222):

| Sub-issue | Content | Files (est.) |
|-----------|---------|:-:|
| S1 | ADO x3: column, replaced index, ordered due query, message and factory, integration tests | 15 |
| S2 | Dapper x3: same | 15 |
| S3 | EF Core (message, configuration with `HasIndex`, factory, store) and MongoDB (message, factory, store, compound index) | 14 |
| S4 | Core: enum use, interface member, draft and options properties, orchestrator validation, metrics and log, fakes, decorator, docs | 14 |

**Coordination (family scheduling-1)**: #1222 owns `ScheduledMessageDraft` and the options records (this plan adds `Priority` to them); #133 adds `PausedAtUtc` to the same due filter; #1251 turns the due fetch into a claim, and the ordering moves into the claim query. This plan takes EventIds 2380-2389.

---

## Design Choices

<details>
<summary><strong>1. One priority type shared with the outbox and transports, or one for scheduling</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Share the `MessagePriority` enum proposed in #123 (in `Encina.Messaging`)** | One concept; a priority set on a command flows through the scheduler and the outbox; one mapping to learn | The levels must fit all patterns; this plan depends on #123 choosing the same |
| **B) A scheduling-only `ScheduledMessagePriority`** | Independent of #123 | Two enums and a mapping when a scheduled message ends up in the outbox |

### Chosen Option: **A — shared `MessagePriority`** (recommended, pending the maintainer)

### Rationale

- A wins when scheduled messages end up in the outbox (they are dispatched through `IEncina`, and outbox publication has its own priority in #123); B wins if scheduled work needs different levels than the transports.
- The two issues must not choose differently (#147 and #123 both say so). The levels are `Low = 0`, `Normal = 1`, `High = 2`, `Critical = 3`; if the maintainer picks B, only the type name in Phase 1 changes.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. How a caller sets the priority</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A `Priority` property on the `ScheduleOptions` and `RecurringOptions` records of #1222** | Explicit per call; no new overload per method | Not derived from the message type |
| **B) The shared attribute or interface of #123 on the request type** | Consistent across patterns; priority belongs to the message type | Not per call; depends on #123 shipping it |
| **C) Both, an explicit option overriding the attribute** | Most flexible | Two sources to document and test |

### Chosen Option: **A — options records** (recommended, pending the maintainer)

### Rationale

- A wins for direct callers and needs nothing from #123; B wins when priority is a property of the type (a payment command is always critical). C can be added later without rework: the resolution order would be explicit option, then attribute, then `Normal`.
- The original sample (`priority:` as a parameter of each `ScheduleAsync` overload) is replaced by the options records, which avoids overload growth (`DateTime`, `TimeSpan`, `DateTimeOffset`, recurring).

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Starvation of low-priority messages</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) None; the due filter already bounds waiting and low priority waits only while higher-priority work is due** | Simplest; no extra column or query | A sustained overload of high-priority work delays low priority without bound |
| **B) The anti-starvation mechanism of #123** | One behavior across patterns | More logic in the due query on 10 providers |

### Chosen Option: **A — no starvation prevention** (recommended, pending the maintainer)

### Rationale

- A wins when due volume stays below batch capacity (`SchedulingOptions.BatchSize`, default 100, every `ProcessingInterval`, default 30 seconds); B wins when the scheduler runs in sustained overload. The per-priority wait histogram of Phase 5 shows whether B is ever needed.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Ordering rule</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `Priority` descending, then `ScheduledAtUtc` ascending, among messages already due** | A higher priority never runs before it is due; ties keep first-due-first-served | The due predicate stays in the query |
| **B) Sort by priority across all pending messages** | Simple SQL | A critical message due tomorrow would run before a normal one due now |

### Chosen Option: **A — priority then due time, among due messages**

### Rationale

- Stated in the issue and in #123; the due filter (`ProcessedAtUtc IS NULL`, retry window, `ScheduledAtUtc <= now`) comes first in every provider.

**Class:** Settled
**Settled by:** #147 issue text (corrections section), #123

</details>

<details>
<summary><strong>5. Stored form, default and index</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Integer column `Priority NOT NULL DEFAULT 1`; the old scheduled-at index replaced by `(ProcessedAtUtc, Priority DESC, ScheduledAtUtc)` with the retry columns included** | Serves the ordered due query; existing rows read as `Normal`; scripts edited in place | A benchmark is needed to confirm the replaced index |
| **B) Keep the old index and add a second one** | The old access path stays | Two indexes written on every insert and update |

### Chosen Option: **A — one replaced index**

### Rationale

- No migration support pre-1.0, so the `CREATE TABLE` scripts are edited in place; EF Core uses `HasIndex(...).IsDescending(false, true, false)`; MongoDB gets the compound index `{ processedAtUtc: 1, priority: -1, scheduledAtUtc: 1 }`. The EF Core benchmark `ScheduledMessageStoreEFBenchmarks` gains an ordered-fetch case.

**Class:** Trivial
**Settled by:** AGENTS.md section 1 (no migration support pre-1.0), AGENTS.md section 5 (all 10 providers consistent)

</details>

---

## Implementation Phases

### Phase 1: Core model (S4)

> **Goal**: The priority on the shared contracts.

<details>
<summary><strong>Tasks</strong></summary>

1. The priority enum: `MessagePriority` as defined by #123 (recommended); if #123 has not merged when this starts, the enum is created here in `src/Encina.Messaging/` and #123 reuses it.
2. `IScheduledMessage.Priority` (`MessagePriority Priority { get; set; }`, XML docs); interface edit last.
3. `ScheduledMessageDraft`, `ScheduleOptions` and `RecurringOptions` (created by #1222) gain `Priority` (default `Normal`).
4. `SchedulingErrorCodes.InvalidPriority = "scheduling.invalid_priority"` for undefined enum values (`Enum.IsDefined`).
5. `PublicAPI.Unshipped.txt` (a `mechanical-fixer` writes the lines).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
- Issue #147. #1222 has merged (ScheduledMessageDraft, ScheduleOptions, RecurringOptions). The priority type is Dilemma 1 of the plan; use the answer recorded in Maintainer Decisions.

TASK:
Add Priority to the draft, the options records and IScheduledMessage, and the InvalidPriority error code.

KEY RULES:
- Either<EncinaError, T>; undefined enum values are a Left, not an exception.
- XML docs on every public member.

REFERENCE FILES:
- src/Encina.Messaging/Scheduling/IScheduledMessage.cs
- src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs
```

</details>

### Phase 2: ADO.NET providers x3 (S1)

<details>
<summary><strong>Tasks</strong></summary>

1. `Scripts/004_CreateScheduledMessagesTable.sql` (SqlServer, PostgreSQL, MySQL): `Priority INT NOT NULL DEFAULT 1`; the index `IX_ScheduledMessages_ScheduledAt_Processed` is replaced by `(ProcessedAtUtc, Priority DESC, ScheduledAtUtc)` including `RetryCount`, `NextRetryAtUtc`.
2. `ScheduledMessage.cs` and `ScheduledMessageFactory.cs` (draft overload): the `Priority` property.
3. `ScheduledMessageStoreADO.cs`: `Priority` in `AddAsync`, the reader, and `ORDER BY Priority DESC, ScheduledAtUtc` in `GetDueMessagesAsync` (`TOP (@BatchSize)` on SQL Server, `LIMIT @BatchSize` on PostgreSQL and MySQL).
4. Integration tests: three messages with different priorities and times, all due, come back in priority order; equal priority keeps due order; a not-yet-due critical message is not returned.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
- ADO stores: src/Encina.ADO.<Db>/Scheduling/ScheduledMessageStoreADO.cs. SQL Server uses TOP and bit, PostgreSQL lower-case identifiers and LIMIT, MySQL 0/1.

TASK:
Implement the column, index and ordered due query on the three ADO providers.

KEY RULES:
- Async database calls with CancellationToken only.
- Integration tests use [Collection("ADO-SqlServer")] and siblings with ClearAllDataAsync.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/Scheduling/ScheduledMessageStoreADO.cs
- src/Encina.ADO.PostgreSQL/Scripts/004_CreateScheduledMessagesTable.sql
- tests/Encina.IntegrationTests/ADO/SqlServer/Scheduling/ScheduledMessageStoreADOTests.cs
```

</details>

### Phase 3: Dapper providers x3 (S2)

<details>
<summary><strong>Tasks</strong></summary>

1. Same script and SQL changes in `src/Encina.Dapper.<Db>/Scripts/004_CreateScheduledMessagesTable.sql` and `Scheduling/ScheduledMessageStoreDapper.cs` (`ORDER BY Priority DESC, ScheduledAtUtc` replaces `ORDER BY ScheduledAtUtc`); `AddAsync` passes the message object to Dapper, so the new property needs the column in the `INSERT` list.
2. Message and factory changes; integration tests in `tests/Encina.IntegrationTests/Dapper/<Db>/Scheduling/` with the cases of phase 2.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
- Dapper stores use EitherHelpers.TryAsync, DbLease.Enlisted and CommandDefinition with the token.

TASK:
Apply the ordered due query and the column to the three Dapper providers.

KEY RULES:
- The due filter stays first; only the ORDER BY changes.
- Collections Dapper-SqlServer, Dapper-PostgreSQL, Dapper-MySQL.

REFERENCE FILES:
- src/Encina.Dapper.SqlServer/Scheduling/ScheduledMessageStoreDapper.cs
- tests/Encina.IntegrationTests/Dapper/PostgreSQL/Scheduling/ScheduledMessageStoreDapperTests.cs
```

</details>

### Phase 4: EF Core and MongoDB (S3)

<details>
<summary><strong>Tasks</strong></summary>

1. `ScheduledMessageConfiguration.cs`: `Priority` property with default value 1 and `HasIndex(x => new { x.ProcessedAtUtc, x.Priority, x.ScheduledAtUtc }).IsDescending(false, true, false)` (one configuration for the SqlServer, PostgreSQL and MySQL providers); `ScheduledMessageStoreEF.cs`: `.OrderByDescending(m => m.Priority).ThenBy(m => m.ScheduledAtUtc)`.
2. MongoDB: `[BsonElement("priority")]` on the message class, `Sort.Descending(priority).Ascending(scheduledAtUtc)` in `GetDueMessagesAsync`, the compound index created with the store.
3. Shared integration bodies in `ScheduledMessageStoreEFTestsBase` and the MongoDB integration class.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
- EF Core: one store and configuration for three providers. MongoDB: BsonElement attributes and indexes created with the store.

TASK:
Add the column, index and ordering on EF Core and MongoDB.

KEY RULES:
- Keep the existing due filters; change only the ordering.
- Reuse the integration test bodies for the three EF providers.

REFERENCE FILES:
- src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessageConfiguration.cs
- src/Encina.MongoDB/Scheduling/ScheduledMessageStoreMongoDB.cs
```

</details>

### Phase 5: Orchestrator, cross-cutting integration and observability (S4)

<details>
<summary><strong>Tasks</strong></summary>

1. The orchestrator copies `options.Priority` into the draft after `Enum.IsDefined` validation; the processor path needs no change because the store already returns messages in priority order.
2. `SchedulingPriorityLog.cs` (or a member of the log class #1222 creates): event 2380 `InvalidPriorityRejected` inside `EventIdRanges.MessagingScheduling` (2300-2399; #147 reserves 2380-2389); arguments are the error code only.
3. Metrics on the `Encina` meter, tag `priority` with the four enum names (bounded cardinality): counter `encina.scheduling.priority.processed` and histogram `encina.scheduling.priority.wait` (seconds between `ScheduledAtUtc` and dispatch), recorded in `TryProcessMessageAsync`.
4. `InstrumentedScheduledMessageStore` needs no new member; `FakeScheduledMessageStore` orders by priority then due time; `ScheduledMessageFaker` and `ScheduledMessageBuilder` set the property.
5. Deferred: the per-priority backlog health check is written as an issue file (`artifacts/issues/`), see the matrix.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
- EventIds 2380-2389 are reserved for #147 inside MessagingScheduling; other family reservations: #1222 2326-2339, #146 2340-2349, #150 2350-2359, #133 2360-2379.

TASK:
Wire the priority through the orchestrator, add the log event and the two instruments, and update the fake and builders.

KEY RULES:
- EncinaError.Message never reaches logs or tags; priority names are the only tag values.
- Pack ids sequentially from 2380.

REFERENCE FILES:
- src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs
- src/Encina.Messaging/Diagnostics/SchedulingProcessorMetrics.cs
- src/Encina.Testing.Fakes/Stores/FakeScheduledMessageStore.cs
```

</details>

### Phase 6: Testing

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit**: orchestrator priority validation and draft copy (`FakeTimeProvider`); fake store ordering; metrics tags.
2. **Guard**: new public methods with parameters throw `ArgumentNullException`; store guard classes for the nine SQL providers and EF Core.
3. **Contract**: the due-fetch ordering contract against `FakeScheduledMessageStore`; factory contract across the 10 factories; the 10 providers are covered by the integration bodies.
4. **Property**: justified as not required (the ordering is a sort expressed in the database); the integration cases carry it. Recorded in the PR.
5. **Integration (10 providers, real databases)**: priority order among due messages, tie by due time, a future critical message excluded.
6. **Benchmark**: extend `tests/Encina.BenchmarkTests/Encina.EntityFrameworkCore.Benchmarks/ScheduledMessageStoreEFBenchmarks.cs` with an ordered due fetch over a large table with the new index (run with the `BenchmarkSwitcher` pattern); **load**: `.md` justification (the processor reads sequentially).
7. **Obligations**: `Scheduling/SchedulerOrchestrator.cs` keeps unit 90 and guard 30 (targets never go down); each provider store keeps its targets and the ordered query is covered by the integration flag. Measure per flag with `dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage" --results-directory artifacts\coverage\<Flag>Tests` and `dotnet run --file .github/scripts/coverage-report.cs -- --input artifacts/coverage --output artifacts/coverage-report`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
- Tests execute real package code; Shouldly via Encina.Testing.Shouldly; shared [Collection] database fixtures; BenchmarkSwitcher.FromAssembly, never BenchmarkRunner.Run<T>().

TASK:
Write the tests listed in the plan for the sub-issue you are in.

KEY RULES:
- No Thread.Sleep; a benchmark materializes with ToList(), never returns IQueryable.
- Integration tests are never replaced by a .md justification.

REFERENCE FILES:
- tests/Encina.UnitTests/Messaging/Scheduling/SchedulerOrchestratorTests.cs
- tests/Encina.TestInfrastructure/EFCore/ScheduledMessageStoreEFTestsBase.cs
- tests/Encina.BenchmarkTests/Encina.EntityFrameworkCore.Benchmarks/ScheduledMessageStoreEFBenchmarks.cs
```

</details>

### Phase 7: Documentation and finalization

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs with an example of scheduling with a priority.
2. `changelog.d/147-scheduled-message-priority.added.md` (a `mechanical-fixer` writes it).
3. `docs/features/scheduling.md` priority section and the scaling note on starvation (through the `docs-writer`); the ADR for the shared priority design is the one #123 opens (ADR number assigned when the ADR pull request opens).
4. `PublicAPI.Unshipped.txt`; `docs/INVENTORY.md` for new files.
5. `dotnet build --configuration Release` with 0 warnings; all flags at target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
CONTEXT:
- Documentation pages go to the docs-writer; changelog and PublicAPI lines to the mechanical-fixer.

TASK:
Finish the XML docs and delegate the rest with the exact decided text.

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
| #123, #301, #251 | Priority for outbox, scheduling and transports | Same enum, attribute and ordering proposed; #123 owns the shared type |
| Hangfire queues, RabbitMQ priority queues, Azure Service Bus | Comparable products | Higher priority is served first (Hangfire serves queues in the configured order); the starvation trade-off of choice 3 applies to all of them |
| ADR-018 | Cross-cutting integration | Matrix below |
| ADR-021 | EventIds | Ids 2380-2389 inside `MessagingScheduling` |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `IScheduledMessageStore.GetDueMessagesAsync` | `src/Encina.Messaging/Scheduling/IScheduledMessageStore.cs` | Gains the ordering (signature unchanged) |
| `SchedulerOrchestrator.TryProcessMessageAsync` | `src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs` | Records processed count and wait per priority |
| `ScheduledMessageDraft`, `ScheduleOptions`, `RecurringOptions` | created by #1222 | Carry the priority |
| `SchedulingProcessorMetrics` | `src/Encina.Messaging/Diagnostics/` | Pattern for the instruments |
| `ScheduledMessageStoreEFBenchmarks` | `tests/Encina.BenchmarkTests/Encina.EntityFrameworkCore.Benchmarks/` | Ordered fetch benchmark |

Provider files touched (first draft by the local model, reviewed against the tree):

| Provider | Message class | Factory | Store | Schema or mapping |
|----------|---------------|---------|-------|-------------------|
| ADO-SqlServer, -PostgreSQL, -MySQL | `src/Encina.ADO.<Db>/Scheduling/ScheduledMessage.cs` | `.../ScheduledMessageFactory.cs` | `.../ScheduledMessageStoreADO.cs` | `src/Encina.ADO.<Db>/Scripts/004_CreateScheduledMessagesTable.sql` |
| Dapper-SqlServer, -PostgreSQL, -MySQL | `src/Encina.Dapper.<Db>/Scheduling/ScheduledMessage.cs` | `.../ScheduledMessageFactory.cs` | `.../ScheduledMessageStoreDapper.cs` | `src/Encina.Dapper.<Db>/Scripts/004_CreateScheduledMessagesTable.sql` |
| EFCore-SqlServer, -PostgreSQL, -MySQL | `src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessage.cs` | `.../ScheduledMessageFactory.cs` | `.../ScheduledMessageStoreEF.cs` | `.../ScheduledMessageConfiguration.cs` (shared) |
| MongoDB | `src/Encina.MongoDB/Scheduling/ScheduledMessage.cs` | `.../ScheduledMessageFactory.cs` | `.../ScheduledMessageStoreMongoDB.cs` | BsonElement attributes and a compound index |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` (`MessagingScheduling`, existing 2300-2399) | **2380-2389** for #147 (uses 2380) | Used: 2300-2306, 2320-2325; reserved: #1222 2326-2339, #146 2340-2349, #150 2350-2359, #133 2360-2379 |
| New range | none | The block 6750-6799 is not needed |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Core `Encina.Messaging` | ~6 | enum, interface, draft/options, orchestrator, log, metrics |
| SQL provider stores, scripts, messages, factories (9 providers) | ~27 | |
| EF Core and MongoDB | ~7 | |
| Fakes, Bogus, builders | ~3 | |
| Tests | ~26 | unit, guard, contract, integration, benchmark |
| Documentation and changelog | ~3 | |
| **Total** | **~72 touched, ~45 net in 4 pull requests** | |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no compatibility layers; time from TimeProvider.
- Issue #147 after #1222 (draft record and options records). The shared priority type is #123's; apply the Maintainer Decisions.

IMPLEMENTATION OVERVIEW:
S1/S2/S3: Priority column (INT NOT NULL DEFAULT 1), replaced index (ProcessedAtUtc, Priority DESC, ScheduledAtUtc), ORDER BY Priority DESC, ScheduledAtUtc among due messages, on ADO x3, Dapper x3, EF Core, MongoDB.
S4: enum, interface member, draft and options Priority, validation, orchestrator copy, log 2380, metrics per priority, fakes, docs.

KEY PATTERNS:
- The due filter stays first; only the ordering changes. #1251 later moves the ordering into the claim query.
- Implementations first, interface last.
- Priority names are the only metric tag values; no key, payload or EncinaError.Message in telemetry.

REFERENCE FILES:
- src/Encina.Dapper.SqlServer/Scheduling/ScheduledMessageStoreDapper.cs
- src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessageStoreEF.cs
- src/Encina.MongoDB/Scheduling/ScheduledMessageStoreMongoDB.cs
- src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Ordered fetch of due work; nothing cacheable |
| 2 | OpenTelemetry | ✅ | Phase 5: `encina.scheduling.priority.processed` and `encina.scheduling.priority.wait`, tagged with the priority name |
| 3 | Structured Logging | ✅ | Phase 5: event 2380 inside the registered range |
| 4 | Health Checks | ⏭️ | Due backlog per priority; issue file written in `artifacts/issues/` (the existing `SchedulingHealthCheck` counts overdue messages without priority) |
| 5 | Validation | ✅ | Phase 1 and 5: undefined enum values return Left `scheduling.invalid_priority` |
| 6 | Resilience | ❌ | No external call is added |
| 7 | Distributed Locks | ⏭️ | Multi-instance claiming is #1251; the ordering moves into its claim query |
| 8 | Transactions | ✅ | Phases 2 to 4: the priority is written with the scheduled row in the caller's transaction |
| 9 | Idempotency | ❌ | Ordering does not create duplicates; dispatch idempotency is #735 and independent of this feature |
| 10 | Multi-Tenancy | ⏭️ | Per-tenant fairness and the tenant column are #739 |
| 11 | Module Isolation | ❌ | SPEC-002 asks no module scoping; `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ❌ | No compliance-relevant data is created |

---

## Prerequisites & Dependencies

| Item | Relation | Note |
|------|----------|------|
| #1222 | Must merge first | Draft record, options records, EventIds 2326-2339 |
| #123 | Coordinate | Owns the shared `MessagePriority`, the attribute and the anti-starvation mechanism; Dilemma 1 must be answered the same way in both plans |
| #133 | Sibling | Adds `PausedAtUtc` to the same due filter and row |
| #1251 and #718 | Coordinate | The ordered fetch becomes the ordered claim; claiming changes the completion methods |
| #301 and #251 | Related | Outbox and transport priority share the type |

---

## Next Steps

1. Review this plan; the three Dilemmas (shared type, how the priority is set, starvation) are answered by the maintainer when work starts, together with the answer of #123 for the shared type, and recorded in `## Maintainer Decisions`.
2. Decompose into S1 to S4 after #1222 S4 merges.
3. Link the plan from #147.
