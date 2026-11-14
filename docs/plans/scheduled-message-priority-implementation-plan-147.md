# Implementation Plan: Scheduled messages priority queue support

> **Issue**: [#147](https://github.com/dlrivada/Encina/issues/147)
> **Type**: Feature
> **Complexity**: Medium (7 phases, all 10 database providers, one column, one index, one ordered query, 92 file changes in 11 pull requests on the recommended route (109 in 12 on the conditional route), each of at most 15 files, see "Delivery order and size")
> **Estimated Scope**: ~500 lines of production code + ~1,000 lines of tests
> **Family**: scheduling-1 (adds a column to the row #1222 reshapes). The shared priority type `MessagePriority`, its resolver and the error code `messaging.invalid_priority` are OWNED by #123 and created by PR-A of #123; this plan never creates them. #301 plans the same scheduled-store work with a different design; which of the two issues delivers it is Dilemma 6, and only one of the two plans is ever implemented.

---

## Summary

When several scheduled messages are due at the same time, the higher-priority ones should be dispatched first. Checked against the tree:

- Nothing exists: no `Priority` on `IScheduledMessage`, no priority enum anywhere in `src/` (the only priority enum is `CachePriority` in `Encina.Caching`).
- `IScheduledMessageStore.GetDueMessagesAsync(int batchSize, int maxRetries, ct)` returns `Either<EncinaError, IEnumerable<IScheduledMessage>>`. The stores order by `ScheduledAtUtc` only: `ORDER BY ScheduledAtUtc` in `ScheduledMessageStoreDapper.cs` and the ADO stores, `OrderBy(m => m.ScheduledAtUtc)` in `ScheduledMessageStoreEF.cs`, `SortBy(m => m.ScheduledAtUtc)` in `ScheduledMessageStoreMongoDB.cs`.
- `SchedulerOrchestrator.ScheduleAsync` has overloads `(request, DateTime, ct)` and `(request, TimeSpan, ct)` and no priority argument; #1222 adds `ScheduleOptions` and `RecurringOptions` records to carry per-call settings.
- The existing index `IX_ScheduledMessages_ScheduledAt_Processed` (SQL Server script `004_CreateScheduledMessagesTable.sql`) is `(ScheduledAtUtc, ProcessedAtUtc, RetryCount)` with included columns, which cannot serve `ORDER BY Priority DESC, ScheduledAtUtc`.

This plan adds a `Priority` column and property, orders the due fetch by `Priority DESC, ScheduledAtUtc ASC` among messages that are already due (the due filter stays first), replaces the index, lets callers set the priority through the options records of #1222, and records processed counts and wait time per priority. The outbox and transport priorities (#123, #301, #251) are separate features; the type shared with them is Dilemma 1.

**Provider category**: Database (10): ADO.NET x3, Dapper x3, EF Core x3 (SqlServer, PostgreSQL, MySQL), MongoDB.

**Affected packages**: `Encina.Messaging`, the six ADO.NET and Dapper provider packages, `Encina.EntityFrameworkCore`, `Encina.MongoDB`, `Encina.Testing.Fakes`, `Encina.Testing.Bogus`, `Encina.Testing.FsCheck`. `Encina.OpenTelemetry` is not affected: `InstrumentedScheduledMessageStore` needs no new member because the fetch signature does not change.

**Split** (implementations first, interface last, as in #1222): the priority is added as a plain property of each provider message class and as an `ORDER BY` in each store before `IScheduledMessage` gets the member, so every pull request leaves main building. The type comes from PR-A of #123, which merges first. The delivery table ("Delivery order and size", under Implementation Phases) counts files including tests, manifests and PublicAPI.

**Dependency on #1222 choice 5 (how new row values reach `IScheduledMessageFactory.Create`)**: the plan of #1222 recommends route C, settable properties on `IScheduledMessage` assigned by the orchestrator after `Create`, with no `ScheduledMessageDraft` and no change to the provider factories; the draft exists only if the maintainer picks its option A (the `Create(ScheduledMessageDraft)` signature). That choice is not yet answered, so this plan describes both and treats the first as the recommended path (register entry U6): under route C the orchestrator assigns `Priority` after `Create` and the provider factories are untouched; under route A the draft gains `Priority` in its own first pull request (A0) and each provider factory overload copies it. The `ScheduleOptions` and `RecurringOptions` records are the same under both routes.

**Coordination (family scheduling-1)**: #1222 owns the options records (and the draft, if its option A is chosen); this plan adds `Priority` to the options (and to the draft under route A); #133 adds `PausedAtUtc` to the same due filter; #1251 turns the due fetch into a claim, and the ordering moves into the claim query. This plan takes EventIds 2380-2389.

---

## Design Choices

<details>
<summary><strong>1. One priority type shared with the outbox and transports, or one for scheduling</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Share the `MessagePriority` enum of #123 (in `Encina.Messaging`, created by its PR-A)** | One concept; a priority set on a command flows through the scheduler and the outbox; one mapping to learn | The levels must fit all patterns; this plan depends on #123 choosing the same |
| **B) A scheduling-only `ScheduledMessagePriority`** | Independent of #123 | Two enums and a mapping when a scheduled message ends up in the outbox |

### Chosen Option: **A — shared `MessagePriority`** (recommended, pending the maintainer)

### Rationale

- A wins when scheduled messages end up in the outbox (they are dispatched through `IEncina`, and outbox publication has its own priority in #123); B wins if scheduled work needs different levels than the transports.
- The two issues must not choose differently (#147 and #123 both say so). The levels are those of #123 choice 1: `Background = 0`, `Low = 1`, `Normal = 2`, `High = 3`, `Critical = 4`; the first draft of this plan used four levels and is superseded. If the maintainer picks B, only the type name in Phase 1 changes. This plan never creates the enum, whatever the merge order: it starts after PR-A of #123.

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
- #301 recommends B for the scheduled store (aged rows first, with a criteria fetch). If Dilemma 6 is answered A (#301 delivers), this plan is withdrawn and #301's answer applies; if it is answered B (#147 delivers) and the maintainer picks B here, the fetch needs the criteria record of #123 choice 4 and the delivery table changes (the contract chain of #123 R9), so the answers must be given together.

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

- The due filter (`ProcessedAtUtc IS NULL`, retry window, `ScheduledAtUtc <= now`) comes first in every provider, so option B (sort across all pending messages) would make a message run before it is due, which `IScheduledMessage.IsDue()` forbids.

**Class:** Settled
**Settled by:** `IScheduledMessage.IsDue()` and the existing due filter in `src/Encina.Messaging/Scheduling/IScheduledMessage.cs` and the provider stores (the contract this plan must not change; option B contradicts it); #147 issue text (corrections section)

</details>

<details>
<summary><strong>5. Stored form, default and index</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Column `Priority NOT NULL DEFAULT 2` (`TINYINT` on SQL Server and MySQL, `SMALLINT` on PostgreSQL, the types of #123 register entry R2); the old scheduled-at index replaced by `(ProcessedAtUtc, Priority DESC, ScheduledAtUtc)` with the retry columns included** | Serves the ordered due query; existing rows read as `Normal`; scripts edited in place | A benchmark is needed to confirm the replaced index |
| **B) Keep the old index and add a second one** | The old access path stays | Two indexes written on every insert and update |

### Chosen Option: **A — one replaced index** (recommended, pending the maintainer)

### Rationale

- Option A wins when write cost matters (one index); option B wins when a query path that the old index serves exists (none is known). Both satisfy AGENTS.md section 5 (all 10 providers consistent), so the rule does not decide.
- No migration support pre-1.0 (AGENTS.md section 1), so the `CREATE TABLE` scripts are edited in place; EF Core uses `HasIndex(...).IsDescending(false, true, false)`; MongoDB gets the compound index `{ processedAtUtc: 1, priority: -1, scheduledAtUtc: 1 }`. The EF Core benchmark `ScheduledMessageStoreEFBenchmarks` gains an ordered-fetch case.
- The column type and default follow #123 (the first draft used `INT` and default 1); the default is the property initializer (`Normal`) plus the column default (the provider factories are untouched under the recommended route, register entry U6).

**Class:** Dilemma

</details>

<details>
<summary><strong>6. Which issue delivers the scheduled-store priority: #147 or #301 (Dilemma)</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) #301 delivers it; #147 is closed as duplicate after the merge and this plan is withdrawn** | One design with the family (aged-first rule, resolver, criteria record, one chain of pull requests) | The options-record route of choice 2 and the no-starvation answer of choice 3 are not built |
| **B) #147 delivers it with this plan; #301 is narrowed to the per-priority batching question** | Builds on the options records of #1222; no fetch-signature change under choice 3 option A | Two plans must stay reconciled; the scheduled side would not get the starvation rule unless choice 3 answers B |

### Chosen Option: **A — #301 delivers it** (recommended, pending the maintainer; the same question is Dilemma D6 of #301)

### Rationale

- Option A wins when one fetch contract and one ordering rule must serve outbox and scheduler; option B wins when #1222 merges first and priority should be set through its options records.
- Both plans use the type, resolver, error code, levels, column type and default of #123. Only one of the two is implemented.

**Class:** Dilemma

</details>

### Decision register (decisions not covered by choices 1 to 6)

Class is Trivial, Settled, Dilemma (the maintainer decides; recommendation only) or Defer.

| Id | Decision | Class | Text |
| --- | --- | --- | --- |
| U1 | Default priority `Normal` (value 2, stored default 2), levels `Background` to `Critical` (0 to 4) | Trivial | Follows #123 choice 1 and register entry R2; the enum is not defined here |
| U2 | Undefined enum value: `Left` `messaging.invalid_priority` with `ErrorCategory.Validation` (the shared code of #123 PR-A), checked with `Enum.IsDefined` on the explicit option value; the orchestrator returns it from `ScheduleAsync` and `ScheduleRecurringAsync` | Dilemma (answered with #123 R4; recommended: fail, pending the maintainer) | The other option is defaulting to `Normal` |
| U3 | Metric names `encina.scheduling.priority.processed` (counter) and `encina.scheduling.priority.wait` (histogram, seconds from `ScheduledAtUtc` to dispatch, recorded in `TryProcessMessageAsync`); the tag `priority` has five bounded values | Trivial | Follow the `encina.scheduling.*` names of `SchedulingProcessorMetrics` |
| U4 | Log event 2380 `InvalidPriorityRejected` (error code only) in the existing `MessagingScheduling` range 2300-2399, ids 2380-2389 reserved for this plan (registry: 2326-2389 claimed by #1222, #146, #150, #133, #147) | Trivial | Existing range; no new range is registered |
| U6 | How the priority reaches a new row: route C of #1222 (settable `Priority` assigned by the orchestrator after `Create`, no draft, factories untouched) or route A (the `ScheduledMessageDraft` carries it, each factory overload copies it) | Dilemma (answered with #1222 choice 5; recommended: route C, pending the maintainer) | Route C keeps every pull request independent of the factory signature and is the recommendation of #1222; route A needs the draft to gain `Priority` in its own pull request A0 first. Both delivery tables are in "Delivery order and size" |
| U5 | Backlog per priority for health checks | Defer | Follow-up issue to be opened (draft: `artifacts/decisions/plan-batch/followups/outbox-priority-pending-depth.md`, which covers scheduling) |

---

## Implementation Phases

### Delivery order and size

Implementations first, interface last. Pull requests A to H add the `Priority` property to each provider message class and the ordered query to each store; none of them uses a type created later (the enum is created by PR-A of #123, which merges first). Pull request I adds the `IScheduledMessage.Priority` member together with every remaining implementer (the fake message, the faker's message class, the FsCheck arbitrary's message class and the two benchmark entity factories), so no implementer is left behind; the 8 provider message classes already have the property. Pull request J adds the orchestrator logic, the options `Priority` and the fake-store ordering; pull request K is documentation. Counts include tests, coverage manifests and PublicAPI files; every pull request has at most 15 files and 1500 lines.

**Recommended route (#1222 route C: settable properties, no draft, provider factories untouched).** The orchestrator assigns `Priority` after `IScheduledMessageFactory.Create`; a new row gets `Normal` from the property initializer and the column default (register entry U1). Nothing in A to H reads a draft.

| PR | Content (phases) | src | Tests | PublicAPI | Manifests | Docs | Total |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| A | ADO SQL Server: message, store, two scripts; integration ordering cases (Phase 2) | 4 | 1 | 1 | 1 | 0 | 7 |
| B | ADO PostgreSQL: same; plus the PostgreSQL schema-script integration test (Phase 2) | 4 | 2 | 1 | 1 | 0 | 8 |
| C | ADO MySQL: same, with the MySQL schema-script test (Phase 2) | 4 | 2 | 1 | 1 | 0 | 8 |
| D, E, F | Dapper SQL Server, PostgreSQL, MySQL, one per database: message, store, two scripts; unit store test and integration cases (Phase 3) | 4 each | 2 each | 1 each | 1 each | 0 | 8 each |
| G | EF Core: message, configuration, store; shared integration bodies in `ScheduledMessageStoreEFTestsBase`, ordered-fetch benchmark case (Phase 4 task 1, 3) | 3 | 2 | 1 | 1 | 0 | 7 |
| H | MongoDB: message, store, index creator; integration cases (Phase 4 task 2) | 3 | 1 | 1 | 1 | 0 | 6 |
| I | `IScheduledMessage.Priority` plus the remaining implementers: `FakeScheduledMessage`, `ScheduledMessageFaker`, `ArbitraryMessages`, the two `BenchmarkEntityFactory` classes; faker test (Phase 1 task 2) | 6 | 1 | 4 | 4 | 0 | 15 |
| J | Options `Priority`, orchestrator validation and assignment, `FakeScheduledMessageStore` ordering, metrics, log event 2380; unit, metrics, fake-store and contract tests (Phase 1 task 3, Phase 5) | 6 | 4 | 2 | 2 | 0 | 14 |
| K | Docs: `docs/features/scheduling.md` section, `docs/INVENTORY.md`, changelog fragment (`docs-writer`, `mechanical-fixer`) | 0 | 0 | 0 | 0 | 3 | 3 |
| **Total** | 11 pull requests | **42** | **19** | **14** | **14** | **3** | **92** |

The total adds up by rows (7 + 8 + 8 + 24 + 7 + 6 + 15 + 14 + 3 = 92) and by columns (42 + 19 + 14 + 14 + 3 = 92).

**Conditional route (#1222 option A: `ScheduledMessageDraft` and the `Create(draft)` factory signature exist).** A new pull request A0 comes first: `ScheduledMessageDraft` gains `Priority` (default `Normal`) with its test, PublicAPI line and manifest entry (src 1, tests 1, PublicAPI 1, manifests 1 = 4 files). Each provider pull request A to H then also changes its factory overload to copy `draft.Priority` (+1 source file) and its factory test (+1 test file), so A 9, B 10, C 10, D to F 10 each, G 8, H 7, as in the first version of this plan; the draft is no longer touched in J (J has 13 files: 5 source). The orchestrator still validates the options value and builds the draft with it. Total: 12 pull requests, 92 + 4 + 14 (factory files and tests of A to H) - 1 = 109 file changes. Under this route A to H never read a `Priority` that the draft only gains later: A0 merges first.

Existing test callers need no migration under either route: the fetch signature does not change, so only the files above are touched; the brief of each pull request recounts them with `git grep` on the message and store types.

**Tests and coverage targets ship with the code.** Each pull request enters the per-file targets below (one-sentence justification each, written by `mechanical-fixer` into its package manifest) and reports measured per-flag coverage. Targets of existing files never go down; a "keeps" row means the manifest value is unchanged and the pull request must not lower it.

| File (pull request) | Unit | Guard | Integration | Justification |
| --- | ---: | ---: | ---: | --- |
| ADO and Dapper `ScheduledMessageStore*.cs` x6 (A to F) | keeps | keeps | keeps | The ordered due query is covered by the integration cases added in the same pull request; the signature does not change, so existing guard tests are unchanged |
| ADO and Dapper `ScheduledMessage.cs` x6 (A to F) | 0 | 0 | 80 | A plain data class with one new auto-property; only the integration round trip of the store reads and writes it |
| `EntityFrameworkCore/Scheduling/ScheduledMessageStoreEF.cs` and `ScheduledMessageConfiguration.cs` (G) | keeps | keeps | keeps | The shared EF integration bodies run the ordering on SqlServer, PostgreSQL and MySQL |
| `MongoDB/Scheduling/ScheduledMessageStoreMongoDB.cs` and `MongoDbIndexCreator.cs` (H) | keeps | keeps | keeps | The Mongo integration class covers the sort and the index creation |
| `Testing.Fakes/Stores/FakeScheduledMessageStore.cs` (J) | 95 | 0 | 0 | The ordering is exercised by unit and contract tests; no new parameter checks |
| `Testing.Fakes/Models/FakeScheduledMessage.cs` (I) | 90 | 0 | 0 | One auto-property read by the fake-store tests |
| `Scheduling/SchedulerOrchestrator.cs` (J) | 90 | 30 | 85 | Keeps the existing unit 90 and guard 30; the new validation and assignment branches are covered by unit tests and by the scheduler end-to-end suites |
| Options records with `Priority` (J) | 90 | 0 | 0 | Records without checks, exercised through the orchestrator |
| Log member of event 2380 (J) | 70 | 0 | 0 | Source-generated, asserted through a fake logger |

Guard tests (#3798): every new public constructor or method with a validated parameter gets its argument tests in the pull request that adds it (the orchestrator and the options in J; the nine SQL stores and EF Core keep their existing guard classes, which are unchanged because no signature changes).

> **Goal**: The priority on the shared contracts.

<details>
<summary><strong>Tasks</strong></summary>

1. The priority enum is `MessagePriority` of #123, created by its PR-A; this plan never creates it and does not start before PR-A has merged.
2. Pull request I: `IScheduledMessage.Priority` (`MessagePriority Priority { get; set; }`, XML docs), added last, together with every remaining implementer (`FakeScheduledMessage`, the message class inside `ScheduledMessageFaker`, the one inside `ArbitraryMessages`, and the two `BenchmarkEntityFactory` classes); the 8 provider message classes already have the property from pull requests A to H.
3. Pull request J: `ScheduleOptions` and `RecurringOptions` (created by #1222) gain `Priority` (default `Normal`). Only under route A (U6): `ScheduledMessageDraft` gains `Priority` in the earlier pull request A0, not here.
4. No new error code: an undefined enum value returns the shared `messaging.invalid_priority` (`ErrorCategory.Validation`) of #123 PR-A, checked with `Enum.IsDefined` (register entry U2).
5. `PublicAPI.Unshipped.txt` and manifest entries (a `mechanical-fixer` writes the lines) in each pull request.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
- Issue #147. #1222 has merged (ScheduleOptions, RecurringOptions, and ScheduledMessageDraft only if its choice 5 picked route A; read the answer in Maintainer Decisions) and PR-A of #123 has merged (MessagePriority, the shared error code). The priority type is Dilemma 1 of the plan; use the answer recorded in Maintainer Decisions.

TASK:
Add Priority to the options records and IScheduledMessage (and to the draft under route A) in the pull request the brief names.

KEY RULES:
- Either<EncinaError, T>; undefined enum values are a Left (messaging.invalid_priority, ErrorCategory.Validation, ADR 062), not an exception.
- Never create MessagePriority or a scheduling-specific error code here.
- XML docs on every public member.

REFERENCE FILES:
- src/Encina.Messaging/Scheduling/IScheduledMessage.cs
- src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs
```

</details>

### Phase 2: ADO.NET providers x3 (pull requests A, B, C, one per database)

<details>
<summary><strong>Tasks</strong></summary>

1. `Scripts/004_CreateScheduledMessagesTable.sql` and `Scripts/000_CreateAllTables.sql` (SqlServer, PostgreSQL, MySQL): `Priority TINYINT NOT NULL DEFAULT 2` (SQL Server, MySQL) or `SMALLINT NOT NULL DEFAULT 2` (PostgreSQL), per #123 R2; the index `IX_ScheduledMessages_ScheduledAt_Processed` is replaced by `(ProcessedAtUtc, Priority DESC, ScheduledAtUtc)` including `RetryCount`, `NextRetryAtUtc`.
2. `ScheduledMessage.cs`: the `Priority` property (initializer `Normal`). Only under route A (U6): `ScheduledMessageFactory.cs` copies `draft.Priority` in its draft overload.
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

### Phase 3: Dapper providers x3 (pull requests D, E, F, one per database)

<details>
<summary><strong>Tasks</strong></summary>

1. Same script and SQL changes (types and default of #123 R2) in `src/Encina.Dapper.<Db>/Scripts/004_CreateScheduledMessagesTable.sql`, `000_CreateAllTables.sql` and `Scheduling/ScheduledMessageStoreDapper.cs` (`ORDER BY Priority DESC, ScheduledAtUtc` replaces `ORDER BY ScheduledAtUtc`); `AddAsync` passes the message object to Dapper, so the new property needs the column in the `INSERT` list.
2. Message change (and the factory only under route A, U6); integration tests in `tests/Encina.IntegrationTests/Dapper/<Db>/Scheduling/` with the cases of phase 2.

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

### Phase 4: EF Core and MongoDB (pull requests G and H)

<details>
<summary><strong>Tasks</strong></summary>

1. `ScheduledMessageConfiguration.cs`: `Priority` property with default value 2 and `HasIndex(x => new { x.ProcessedAtUtc, x.Priority, x.ScheduledAtUtc }).IsDescending(false, true, false)` (one configuration for the SqlServer, PostgreSQL and MySQL providers); `ScheduledMessageStoreEF.cs`: `.OrderByDescending(m => m.Priority).ThenBy(m => m.ScheduledAtUtc)`.
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

### Phase 5: Orchestrator, cross-cutting integration and observability (pull request J)

<details>
<summary><strong>Tasks</strong></summary>

1. The orchestrator validates `options.Priority` with `Enum.IsDefined` (register entry U2) and assigns it to the new message after `Create` (route C) or copies it into the draft (route A); the processor path needs no change because the store already returns messages in priority order.
2. A member of the log class #1222 creates (this is the first pull request of this plan that logs): event 2380 `InvalidPriorityRejected` inside `EventIdRanges.MessagingScheduling` (2300-2399; #147 reserves 2380-2389); arguments are the error code only.
3. Metrics on the `Encina` meter, tag `priority` with the five enum names (bounded cardinality): counter `encina.scheduling.priority.processed` and histogram `encina.scheduling.priority.wait` (seconds between `ScheduledAtUtc` and dispatch), recorded in `TryProcessMessageAsync` (register entry U3).
4. `InstrumentedScheduledMessageStore` needs no new member (the fetch signature does not change); `FakeScheduledMessageStore` orders by priority then due time (this pull request); `ScheduledMessageFaker` sets the property (pull request I).
5. Deferred: the per-priority backlog health check is a follow-up issue to be opened (draft: `artifacts/decisions/plan-batch/followups/outbox-priority-pending-depth.md`), see the matrix.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
- EventIds 2380-2389 are reserved for #147 inside MessagingScheduling; other family reservations: #1222 2326-2339, #146 2340-2349, #150 2350-2359, #133 2360-2379.

TASK:
Wire the priority through the orchestrator, add the log event and the two instruments, and update the fake store ordering.

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

Tests ship with the pull request that owns the code (see the delivery table).

1. **Unit** (J): orchestrator priority validation and assignment (`FakeTimeProvider`); fake store ordering; metrics tags; under route A also the factory output per provider in A to H.
2. **Guard** (AGENTS.md section 9, #3798): every new public constructor or method with a validated parameter gets its argument tests in the pull request that adds it (J for the orchestrator and options records); the existing store guard classes for the nine SQL providers and EF Core are unchanged because no signature changes.
3. **Contract** (J): the due-fetch ordering contract against `FakeScheduledMessageStore`; under route A the factory contract across the 10 factories (extended by A to H as each factory changes); the 10 providers are covered by the integration bodies.
4. **Property**: justified as not required (the ordering is a sort expressed in the database); the integration cases carry it. Recorded in the pull request of J.
5. **Integration (10 providers, real databases)**: priority order among due messages, tie by due time, a future critical message excluded; each in its provider pull request (A to H), in the shared collections, never a `.md`.
6. **Benchmark** (G): extend `tests/Encina.BenchmarkTests/Encina.EntityFrameworkCore.Benchmarks/ScheduledMessageStoreEFBenchmarks.cs` with an ordered due fetch over a large table with the new index (run with the `BenchmarkSwitcher` pattern); **load**: `.md` justification (the processor reads sequentially).
7. **Obligations** (entered in the same pull request as the tests that meet them): `Scheduling/SchedulerOrchestrator.cs` keeps unit 90 and guard 30 (targets never go down); each provider store keeps its targets and the ordered query is covered by the integration flag; per-file targets with justification are in "Delivery order and size". Measure per flag with `dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage" --results-directory artifacts\coverage\<Flag>Tests` and `dotnet run --file .github/scripts/coverage-report.cs -- --input artifacts/coverage --output artifacts/coverage-report`.

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

Pull request K; XML docs and PublicAPI lines ship with each code pull request.

1. XML docs with an example of scheduling with a priority (in the code pull requests).
2. `changelog.d/147-scheduled-message-priority.added.md` (a `mechanical-fixer` writes it).
3. `docs/features/scheduling.md` priority section and the scaling note on starvation (through the `docs-writer`); the ADR for the shared priority design is the one #123 opens (ADR number assigned when the ADR pull request opens).
4. `docs/INVENTORY.md` for new files.
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
| `ScheduleOptions`, `RecurringOptions` (and `ScheduledMessageDraft` under route A) | created by #1222 | Carry the priority |
| `SchedulingProcessorMetrics` | `src/Encina.Messaging/Diagnostics/` | Pattern for the instruments |
| `ScheduledMessageStoreEFBenchmarks` | `tests/Encina.BenchmarkTests/Encina.EntityFrameworkCore.Benchmarks/` | Ordered fetch benchmark |

Provider files touched (the Factory column is touched only under route A, U6):

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

The authoritative count is the "Delivery order and size" table.

| Category | File changes | Notes |
|----------|------:|-------|
| Source | 42 | Recommended route C: 6 SQL providers x 4 = 24, EF Core 3, MongoDB 3 (30); pull request I 6 (interface, fake message, faker, arbitrary, two benchmark entity factories); pull request J 6 (two options files, orchestrator, metrics, log, `FakeScheduledMessageStore`) |
| Tests | 19 | Integration, unit store, schema-script, faker, metrics, contract, fake store |
| PublicAPI | 14 | One per package per pull request that adds a public member |
| Manifests | 14 | Same |
| Documentation and changelog | 3 | Pull request K |
| **Total** | **92** | 11 pull requests, equals the route C delivery table (route A: 109 in 12 pull requests) |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no compatibility layers; time from TimeProvider.
- Issue #147 after #1222 (options records; the draft record only under #1222 route A, register entry U6). The shared priority type is #123's; apply the Maintainer Decisions.

IMPLEMENTATION OVERVIEW:
Pull requests A to H (one per database for ADO and Dapper, then EF Core, MongoDB): Priority column (TINYINT or SMALLINT NOT NULL DEFAULT 2), replaced index (ProcessedAtUtc, Priority DESC, ScheduledAtUtc), ORDER BY Priority DESC, ScheduledAtUtc among due messages.
Pull request I: IScheduledMessage.Priority plus every remaining implementer. Pull request J: options Priority, validation (messaging.invalid_priority), orchestrator assignment (route C) or draft copy (route A), log 2380, metrics per priority, fake store ordering. Pull request K: docs.
The type MessagePriority comes from PR-A of #123 and is never created here; only one of this plan and the #301 plan is implemented (Dilemma 6).

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
| 4 | Health Checks | ⏭️ | Due backlog per priority; follow-up issue to be opened (draft: `artifacts/decisions/plan-batch/followups/outbox-priority-pending-depth.md`; the existing `SchedulingHealthCheck` counts overdue messages without priority) |
| 5 | Validation | ✅ | Phase 1 and 5: undefined enum values return Left `messaging.invalid_priority` (`ErrorCategory.Validation`) |
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
| #1222 | Must merge first | Options records, EventIds 2326-2339; the draft record only if its choice 5 picks route A (this plan states both routes, U6) |
| #123 | Must merge first (PR-A) | Owns and creates the shared `MessagePriority`, the resolver, the error code and the anti-starvation mechanism; Dilemma 1 must be answered the same way in both plans |
| #301 | Same scope | Plans the same scheduled-store work (Dilemma 6); only one of the two plans is implemented |
| #133 | Sibling | Adds `PausedAtUtc` to the same due filter and row |
| #1251 and #718 | Coordinate | The ordered fetch becomes the ordered claim; claiming changes the completion methods |
| #251 | Related | Transport priority shares the type |

---

## Next Steps

1. Review this plan; the Dilemmas (shared type, how the priority is set, starvation, index, which issue delivers) are answered by the maintainer when work starts, together with the answers of #123 for the shared type and of #301 for Dilemma 6, and recorded in `## Maintainer Decisions`.
2. If Dilemma 6 is answered B, decompose into pull requests A to K after #1222 merges and PR-A of #123 merges; if A, withdraw this plan.
3. Link the plan from #147.

## Maintainer Decisions

None yet. Pending: Dilemmas 1, 2, 3, 5 and 6 and register entries U2 and U6 (U6 follows the answer to choice 5 of #1222).
