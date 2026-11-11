# Implementation Plan: Message Priority for the Outbox (`Encina.Messaging` + 10 database providers)

> **Issue**: [#123](https://github.com/dlrivada/Encina/issues/123)
> **Type**: Feature
> **Complexity**: Medium (9 phases, 10 database providers, about 100 files, delivered in 7 pull requests)
> **Estimated Scope**: about 1,400 lines of production code + about 1,800 lines of tests
> **Family**: message priority and partitioning (#123, #251, #301, #469, #1254). This plan OWNS the shared priority design (type, attribute, interface, fetch contract, starvation); #301 and #251 reuse it and must not decide it twice. #147 is duplicate scope of #301 and is closed against it.

---

## Summary

Add opt-in **message priority** to the outbox: a message carries a `MessagePriority`, the outbox stores persist it, and the fetch that feeds the processors returns higher-priority messages first, with an age-based promotion so low-priority messages cannot wait forever. Priority comes from an attribute on the message type, from an interface property on the instance, or both.

Today nothing of this exists in `src/`: `MessagePriority`, `IPrioritizedRequest` and `[Priority]` are not in the tree. Every outbox store fetches through `IOutboxStore.GetPendingMessagesAsync(batchSize, maxRetries, ct)` and orders by `CreatedAtUtc` (for example `src/Encina.Dapper.SqlServer/Outbox/OutboxStoreDapper.cs`: `ORDER BY CreatedAtUtc`). `OutboxBatchProcessor.ProcessAsync` runs the returned messages one at a time. The change is therefore a new column, a new fetch ordering and a new fetch contract on all 10 database providers, plus a small amount of core code.

- **Standards**: Azure Architecture Center, Priority Queue pattern; Enterprise Integration Patterns (Resequencer is the opposite concern and is #469).
- **Scope**: outbox only. The scheduled store is #301 (it reuses everything defined here); transports are #251; per-key ordering is #469; the nullable `PartitionKey` column is #1254.
- **Affected packages**: `Encina.Messaging`, `Encina.ADO.SqlServer|PostgreSQL|MySQL`, `Encina.Dapper.SqlServer|PostgreSQL|MySQL`, `Encina.EntityFrameworkCore` (the three EF Core providers share it), `Encina.MongoDB`, `Encina.OpenTelemetry` (instrumented store), `Encina.Testing.Fakes`, `Encina.Testing.Bogus`, `Encina.Testing`.
- **Provider category**: Database (10): ADO.NET x3, Dapper x3, EF Core x3, MongoDB (AGENTS.md section 5).

**Family contract** (which plan owns which shared piece, so the five plans stay consistent):

| Shared piece | Owner | Used by |
| --- | --- | --- |
| `MessagePriority`, `MessagePriorityAttribute`, `IPrioritizedMessage`, `MessagePriorityResolver` | #123 | #301, #251 |
| `OutboxFetchCriteria` (the fetch contract) | #123 | #469 (adds partition ordering), #1251 (adds claiming) |
| Starvation rule (aged messages first) | #123 | #301 (same rule on `ScheduledAtUtc`) |
| `IOutboxMessage.PartitionKey`, `IPartitionedMessage` | #1254 | #469 |
| Merge order | #1254, then #123, then #301, then #469; #251 is independent after #123 | |

---

## Design Choices

<details>
<summary><strong>1. Shared priority type — one `MessagePriority` for outbox, scheduling and transports (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) One `MessagePriority` in `Encina.Messaging`, five levels `Background=0, Low=1, Normal=2, High=3, Critical=4`, higher is more urgent** | One concept, one attribute, one column type; a priority set on a command survives outbox, scheduler and broker; covers every level name used by #123, #251 and #301 | Every pattern must accept the same five levels; `Encina.InMemory` references only `Encina`, so it needs a new project reference to `Encina.Messaging` |
| **B) Separate enums per pattern (or separate queues per priority, as the issue's alternatives listed)** | Each pattern evolves alone | Two attributes and a mapping at every boundary; separate queues add tables or collections on 10 providers |

The three open issues disagree today: #123 uses `Low=0..Critical=3`, #251 uses `Low=0..Critical=4` with `Urgent`, #301 uses `Critical=0..Background=4` (ascending order by urgency). Option A also fixes direction: with "higher is more urgent" the fetch reads `ORDER BY Priority DESC`, matching RabbitMQ `x-max-priority` where a larger number is delivered first.

### Chosen Option: **A — one shared `MessagePriority`, five levels, higher is more urgent** (recommended, pending the maintainer)

### Rationale

- Option A wins when the same message type can be both outbox and scheduled, which is the normal case for domain notifications.
- Option B wins only if scheduling needs levels the outbox does not, and nothing in the three issues says so.
- The column default is `Normal` (2) written explicitly by the factory, so the numeric value of the first member does not decide what an unset row means; this removes the "zero is the harmless default" argument for `Low=0`.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Where a message gets its priority — attribute, interface, or both (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) Class attribute only** | Static and cheap, cached per type | Cannot depend on data such as a VIP flag |
| **B) Interface property only (`IPrioritizedMessage`)** | Dynamic per instance | Every type implements it, even when the priority is constant |
| **C) Both; the interface wins over the attribute, then the default `Normal`** | Covers static and dynamic use | Two ways to say one thing |

Names: the issues propose `IPrioritizedRequest` and `[Priority]` (#123), `IPrioritizedMessage` and `PriorityAttribute` (#251), `[MessagePriority]` (#301). The plan uses `IPrioritizedMessage` and `MessagePriorityAttribute` (two of three issues, and the object that carries the priority is the message).

### Chosen Option: **C — attribute and interface, interface wins** (recommended, pending the maintainer)

### Rationale

- Option C wins when static and dynamic use are mixed, which the issue's own examples show (`[Priority(High)] UrgentOrderCommand` and the VIP `OrderCommand`).
- Resolution is a static `ConcurrentDictionary<Type, MessagePriority?>` for the attribute (the project's "static per-type attribute caching" rule for behaviors); the interface is read per instance.
- An undefined numeric value returns a `Left` (`outbox.invalid_priority`) instead of throwing (ADR-001, ADR-006).

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Starvation prevention — aged messages first (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) Age-based promotion: a message older than `StarvationThreshold` sorts before the priority order** | Bounded wait for every message; portable as one `CASE` in `ORDER BY` (two queries on MongoDB); no extra state | Needs the current time in the query; promoted messages of low priority can crowd a batch after a long outage |
| **B) Weighted fair queuing, N high for every M low** | No time comparison | Slows low priority even when no high-priority work waits unless weights are adaptive; needs per-priority state or several queries |
| **C) Strict priority, no prevention** | Simplest | Low priority can wait forever under sustained load |

### Chosen Option: **A — age-based promotion, threshold default 5 minutes when priority is enabled** (recommended, pending the maintainer)

### Rationale

- Option A wins when a hard maximum wait is an SLA, which is the stated motivation ("SLA compliance").
- The ordering becomes: aged first (oldest first), then `Priority DESC`, then `CreatedAtUtc`. In SQL: `ORDER BY CASE WHEN CreatedAtUtc <= @StarvationCutoffUtc THEN 1 ELSE 0 END DESC, Priority DESC, CreatedAtUtc`. The cutoff is `TimeProvider.GetUtcNow() - StarvationThreshold`, computed once per fetch by `OutboxBatchProcessor`, so stores never read the clock (AGENTS.md section 3).
- Option C wins where low-priority work is genuinely optional; the option `StarvationThreshold = null` selects it.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Shape of the fetch contract — one criteria record for every fetch concern (Dilemma D4, answers #301 D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) Replace `GetPendingMessagesAsync(batchSize, maxRetries, ct)` by `GetPendingMessagesAsync(OutboxFetchCriteria, ct)`** | One fetch path on 10 providers; #469 (partition ordering) and #1251 (claiming) extend the record instead of changing the signature a third and fourth time | One signature change now on the interface, 10 stores, the fakes and about 30 tests |
| **B) Add a second method `GetPendingByPriorityAsync(...)` as #301 proposes** | Explicit; supports per-priority loops | Two fetch paths with duplicated SQL on 10 providers; the next feature adds a third |
| **C) Keep the signature; stores read `OutboxOptions`** | No signature change | Stores take options today as arguments, not from DI; hidden coupling; cannot be unit tested without options |

### Chosen Option: **A — one `OutboxFetchCriteria` record** (recommended, pending the maintainer)

### Rationale

- Pre-1.0 there is no compatibility layer to keep (AGENTS.md section 1), so changing the existing method is cheaper than a parallel one (#301 Alternatives, item 4).
- `OutboxFetchCriteria(int BatchSize, int MaxRetries, bool OrderByPriority, DateTime? StarvationCutoffUtc)`; `NowUtc` is passed by the processor so `NextRetryAtUtc <= @NowUtc` keeps working as today.
- Option B wins only if #301 chooses per-priority loops (its D2-B); this plan recommends against that.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Ordering design — a priority column on the existing table, one queue</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) One `Priority` column plus index, ordered fetch** | Same table, same transaction as the message, one design on all 10 providers | Ordering query is a little costlier than `ORDER BY CreatedAtUtc` |
| **B) A physical queue (table or collection) per priority** | Isolation per level | Five tables or collections times 10 providers; atomic enqueue across levels is harder; breaks the provider-coherence rule's single schema |
| **C) External priority service (Redis sorted set or similar)** | Fast | New infrastructure dependency; the outbox exists to be in the application database |

### Chosen Option: **A — one column, one queue**

### Rationale

- Provider coherence: the same schema change on every provider (AGENTS.md section 5); pay-for-what-you-use means no new infrastructure (AGENTS.md section 1).
- Index per provider: `(ProcessedAtUtc, RetryCount, Priority, CreatedAtUtc)` replaces the leading-column pattern of `IX_OutboxMessages_ProcessedAt_RetryCount`.

**Class:** Settled
**Settled by:** AGENTS.md section 5 (provider coherence: same shared schema on every provider) and section 1 (pay-for-what-you-use, no external service); issue #123 Alternatives Considered, items 1 and 2.

</details>

<details>
<summary><strong>6. Opt-in and default behavior</strong></summary>

### Options Considered

| Option | Pros | Cons |
| --- | --- | --- |
| **A) `OutboxOptions.Priority` (`OutboxPriorityOptions`): `Enabled = false`, `StarvationThreshold = 5 min`; column always present** | Disabled by default as required; stores keep `ORDER BY CreatedAtUtc` when off; schema is one shape | The column exists even when unused |
| **B) Column only created when enabled** | No unused column | Schema differs per configuration on 10 providers; scripts become conditional |

### Chosen Option: **A — option object, disabled by default, column always present**

### Rationale

- Every messaging pattern is optional and disabled by default (AGENTS.md section 3, ADR-050); the issue's `ProcessingStrategy.PriorityBased` and `PriorityQueues` flags are replaced by one `Enabled` flag because there is only one strategy after choice 5.
- Message priority is still stored when disabled, so switching it on later orders already queued rows.
- Options are validated in `OutboxOptions.Validate` (existing method for cross-property rules); threshold must be positive.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (every messaging pattern optional and disabled by default; ADR-050).

</details>

---

## Implementation Phases

Delivery is split into seven pull requests (15 files and 1500 lines each at most): PR-A phases 1 and 2, PR-B phase 3, PR-C ADO.NET (3 providers), PR-D Dapper (3), PR-E EF Core and MongoDB, PR-F observability and cross-cutting, PR-G integration tests for the 10 providers and docs. Merge order inside the family is in the Summary.

### Phase 1: Core Model

<details>
<summary><strong>Tasks</strong></summary>

New folder `src/Encina.Messaging/Priority/`:

1. `MessagePriority.cs`: `public enum MessagePriority { Background = 0, Low = 1, Normal = 2, High = 3, Critical = 4 }` (namespace `Encina.Messaging`).
2. `IPrioritizedMessage.cs`: `MessagePriority Priority { get; }`.
3. `MessagePriorityAttribute.cs`: `[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)] public sealed class MessagePriorityAttribute(MessagePriority priority) : Attribute`.
4. `MessagePriorityResolver.cs`: `public static class MessagePriorityResolver` with `Either<EncinaError, MessagePriority> Resolve(object message)`; interface first, then cached attribute, then `Normal`; undefined enum value returns `Left` with code `outbox.invalid_priority` (added to `OutboxErrorCodes`).
5. Update `src/Encina.Messaging/PublicAPI.Unshipped.txt` through `mechanical-fixer`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```text
CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library. Plan docs/plans/message-priority-implementation-plan-123.md (issue #123). Nothing named MessagePriority exists in src/.
TASK: Create the four types listed in Phase 1 Tasks under src/Encina.Messaging/Priority/. The resolver returns Either<EncinaError, MessagePriority>; use a static ConcurrentDictionary<Type, MessagePriority?> for attributes.
KEY RULES: ROP, never throw for business input (ADR-001, ADR-006); nullable enabled; XML docs on every public member; no [Obsolete]; error code constant in OutboxErrorCodes (src/Encina.Messaging/Outbox/OutboxOrchestrator.cs); PublicAPI.Unshipped lines are written by mechanical-fixer.
REFERENCE FILES: src/Encina.Messaging/Outbox/OutboxOrchestrator.cs (OutboxErrorCodes), src/Encina.Messaging/Outbox/IOutboxMessage.cs, docs/plans/dsr-implementation-plan-404.md (style).
```

</details>

### Phase 2: Core Contracts, Options and Processor

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Messaging/Outbox/IOutboxMessage.cs`: add `MessagePriority Priority { get; set; }`.
2. `src/Encina.Messaging/Outbox/IOutboxStore.cs`: replace `GetPendingMessagesAsync(int batchSize, int maxRetries, CancellationToken)` by `GetPendingMessagesAsync(OutboxFetchCriteria criteria, CancellationToken)`; new `OutboxFetchCriteria.cs` record (`BatchSize`, `MaxRetries`, `OrderByPriority`, `StarvationCutoffUtc`).
3. `src/Encina.Messaging/Outbox/OutboxOrchestrator.cs`: `IOutboxMessageFactory.Create(Guid id, string notificationType, string content, DateTime createdAtUtc, MessagePriority priority)`; `AddAsync<TNotification>` resolves the priority through `MessagePriorityResolver` and returns `Left` on an invalid value.
4. `src/Encina.Messaging/Outbox/OutboxWriter.cs` and `TransactionalOutboxPipelineBehavior.cs`: same resolution before `_messageFactory.Create`.
5. `src/Encina.Messaging/Outbox/OutboxOptions.cs`: `OutboxPriorityOptions Priority { get; }` with `Enabled` (false) and `StarvationThreshold` (`TimeSpan?`, default 5 minutes); validation in `Validate`.
6. `src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs`: build `OutboxFetchCriteria` from options and `TimeProvider`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```text
CONTEXT: Phase 1 types exist. The outbox fetch today is IOutboxStore.GetPendingMessagesAsync(batchSize, maxRetries, ct) and OutboxBatchProcessor.ProcessAsync calls it with _options.BatchSize and _options.MaxRetries.
TASK: Implement Phase 2 Tasks 1 to 6. The processor computes StarvationCutoffUtc = _timeProvider.GetUtcNow().UtcDateTime - threshold only when Priority.Enabled; otherwise OrderByPriority is false and the cutoff null.
KEY RULES: time only from TimeProvider (AGENTS.md section 3); stores never read the clock; Left for invalid priority; options validated in OutboxOptions.Validate; no compatibility overload of the old fetch method; update every caller and the in-repo fakes in the same change.
REFERENCE FILES: src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs, OutboxOptions.cs, OutboxWriter.cs, TransactionalOutboxPipelineBehavior.cs, src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs (similar factory shape).
```

</details>

### Phase 3: Testing Packages and Builders

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Testing.Fakes/Models/FakeOutboxMessage.cs` and `Stores/FakeOutboxStore.cs`: Priority and the new fetch criteria, with the same aged-first ordering as the SQL.
2. `src/Encina.Testing.Bogus/OutboxMessageFaker.cs`, `tests/Encina.TestInfrastructure/Builders/OutboxMessageBuilder.cs`, `src/Encina.Testing/Messaging/OutboxTestHelper.cs`, `src/Encina.Testing.FsCheck/ArbitraryMessages.cs`: a `WithPriority` option and a generator.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```text
CONTEXT: IOutboxMessage now has Priority; the fetch takes OutboxFetchCriteria.
TASK: Update the fake store, fake message, faker, builder, test helper and FsCheck arbitrary so existing tests compile and new tests can set priority.
KEY RULES: the fake store must implement the same ordering as the SQL (aged first, Priority DESC, CreatedAtUtc) so contract tests can compare; time from TimeProvider; no reflection-only tests.
REFERENCE FILES: src/Encina.Testing.Fakes/Stores/FakeOutboxStore.cs, src/Encina.Testing.Bogus/OutboxMessageFaker.cs, tests/Encina.TestInfrastructure/Builders/OutboxMessageBuilder.cs.
```

</details>

### Phase 4: ADO.NET Providers (SqlServer, PostgreSQL, MySQL)

<details>
<summary><strong>Tasks</strong></summary>

For each of `Encina.ADO.SqlServer`, `Encina.ADO.PostgreSQL`, `Encina.ADO.MySQL`:

1. `Outbox/OutboxMessage.cs`: `Priority` property.
2. `Outbox/OutboxMessageFactory.cs`: new `Create` signature.
3. `Outbox/OutboxStoreADO.cs`: `Priority` in the insert and the reader; fetch with `OutboxFetchCriteria`: `TOP (@BatchSize)` on SQL Server, `LIMIT @BatchSize` on PostgreSQL and MySQL; ordering expression of choice 3 when `OrderByPriority`, else `ORDER BY CreatedAtUtc`.
4. `Scripts/001_CreateOutboxMessagesTable.sql` and `Scripts/000_CreateAllTables.sql`: `Priority TINYINT NOT NULL DEFAULT 2` (SQL Server, MySQL), `SMALLINT NOT NULL DEFAULT 2` (PostgreSQL, lower-case `priority`), and the new index.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```text
CONTEXT: Core contracts from Phases 1 and 2 exist. ADO stores build SQL by hand; SQL Server uses TOP (@n) and bracketed identifiers, PostgreSQL uses lower-case unquoted columns and LIMIT, MySQL uses LIMIT and backticks.
TASK: Implement Phase 4 Tasks for the three ADO.NET packages, one package per commit.
KEY RULES: async with CancellationToken only (AGENTS.md section 3); parameters, never string-concatenated values; keep scripts idempotent (CREATE ... IF NOT EXISTS where the script already does); no migration helper; same ordering semantics on all three.
REFERENCE FILES: src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs, src/Encina.ADO.PostgreSQL/Scripts/001_CreateOutboxMessagesTable.sql, src/Encina.ADO.MySQL/Outbox/OutboxStoreADO.cs.
```

</details>

### Phase 5: Dapper Providers (SqlServer, PostgreSQL, MySQL)

<details>
<summary><strong>Tasks</strong></summary>

For each of `Encina.Dapper.SqlServer`, `Encina.Dapper.PostgreSQL`, `Encina.Dapper.MySQL`: the same five files as Phase 4 (`OutboxMessage.cs`, `OutboxMessageFactory.cs`, `OutboxStoreDapper.cs`, the two scripts). The Dapper stores use `SELECT *` mapped to `OutboxMessage`, so the property must match the column name (PostgreSQL maps lower-case columns through the existing mapping).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```text
CONTEXT: Dapper stores use CommandDefinition with CancellationToken and a SELECT * mapped to the provider OutboxMessage class (ORDER BY CreatedAtUtc today, src/Encina.Dapper.SqlServer/Outbox/OutboxStoreDapper.cs).
TASK: Apply Phase 5 to the three Dapper packages, mirroring the ADO.NET change so both families return identical orderings for identical data.
KEY RULES: same SQL semantics as Phase 4; async only; parameterised; DbLease/transaction accessor usage unchanged.
REFERENCE FILES: src/Encina.Dapper.SqlServer/Outbox/OutboxStoreDapper.cs, src/Encina.Dapper.PostgreSQL/Outbox/OutboxStoreDapper.cs, src/Encina.Dapper.MySQL/Outbox/OutboxStoreDapper.cs.
```

</details>

### Phase 6: EF Core (3 providers) and MongoDB

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.EntityFrameworkCore/Outbox/OutboxMessage.cs`, `OutboxMessageFactory.cs`, `OutboxMessageConfiguration.cs` (property with default 2, index `IX_OutboxMessages_Processing` extended), `OutboxStoreEF.cs` (`OrderByDescending` on the aged expression, then `Priority`, then `CreatedAtUtc`; `Take(BatchSize)`). One implementation serves SqlServer, PostgreSQL and MySQL.
2. `src/Encina.MongoDB/Outbox/OutboxMessage.cs` (`[BsonElement("priority")]`), `OutboxMessageFactory.cs`, `OutboxStoreMongoDB.cs` (aged query first, then the priority-sorted remainder, merged up to `BatchSize`), `MongoDbIndexCreator.cs` (compound index).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```text
CONTEXT: EF Core has one store for three database providers; MongoDB cannot express the aged-first CASE in a single sort, so it runs two ordered queries and concatenates them.
TASK: Implement Phase 6 Tasks 1 and 2.
KEY RULES: the EF query must translate on all three providers (test it on all three, not only one); MongoDB indexes are created by MongoDbIndexCreator, never ad hoc; async with CancellationToken.
REFERENCE FILES: src/Encina.EntityFrameworkCore/Outbox/OutboxStoreEF.cs, OutboxMessageConfiguration.cs, src/Encina.MongoDB/Outbox/OutboxStoreMongoDB.cs, src/Encina.MongoDB/MongoDbIndexCreator.cs.
```

</details>

### Phase 7: Cross-Cutting Integration and Observability

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina/Diagnostics/EventIdRanges.cs`: register `MessagingPriority = (6700, 6714)`; add `nameof(EventIdRanges.MessagingPriority)` to the `Encina.Messaging` entry of `AssemblyRanges` in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`.
2. `src/Encina.Messaging/Diagnostics/PriorityLogMessages.cs` (`[LoggerMessage]`, 6700 to 6707): priority resolved from attribute or interface (type name only), invalid priority rejected (code only), priority fetch enabled at start-up, aged promotion applied (count only).
3. `OutboxProcessorMetrics.cs` on the shared `"Encina"` meter: `encina.outbox.priority.processed` (counter, tag `priority`), `encina.outbox.priority.wait_time` (histogram in seconds, tag `priority`), `encina.outbox.priority.promoted` (counter). `OutboxActivitySource.StartAdd` gains the tag `encina.outbox.priority`.
4. `src/Encina.OpenTelemetry/MessagingStores/InstrumentedOutboxStore.cs`: new fetch signature.

Never log message content or an error message; only codes and counts (AGENTS.md section 3).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```text
CONTEXT: ADR-021 requires the EventId range to be registered first in src/Encina/Diagnostics/EventIdRanges.cs (6700-6714 is assigned to this family by the plan-batch registry); the shared meter is "Encina" (OutboxProcessorMetrics).
TASK: Implement Phase 7 Tasks 1 to 4. Pack EventIds 6700-6707 sequentially.
KEY RULES: [LoggerMessage] source generator, no LoggerMessage.Define; no payload, no EncinaError.Message in logs, tags or metrics; run the architecture tests (EventId allocation) before committing.
REFERENCE FILES: src/Encina/Diagnostics/EventIdRanges.cs, tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs, src/Encina.Messaging/Diagnostics/OutboxProcessorMetrics.cs, src/Encina.Messaging/Diagnostics/OutboxStoreLog.cs.
```

</details>

### Phase 8: Testing

<details>
<summary><strong>Tasks</strong></summary>

1. Unit: resolver (attribute, interface wins, default, invalid value), options validation, `OutboxBatchProcessor` criteria and cutoff with `FakeTimeProvider`, factory output per provider.
2. Guard: resolver, options, criteria record, `MessagePriorityAttribute`.
3. Contract: `IOutboxStore` ordering contract run against the fake and every real store (aged first, then priority, then age; disabled mode equals `CreatedAtUtc`).
4. Property (FsCheck): no message waits longer than the threshold plus one batch; the result is a permutation of the pending set restricted to `BatchSize`.
5. Integration: priority-ordered fetch on all 10 providers in the existing shared collections (`ADO-*`, `Dapper-*`, `EFCore-*`, MongoDB), `ClearAllDataAsync` in `InitializeAsync`; never a `.md` justification.
6. Load and benchmark: `.md` justification for the load test (the change is one query) and a benchmark of the ordered fetch (hot path) under `tests/Encina.BenchmarkTests`.
7. Coverage: per-file targets in `.github/coverage-manifest/Encina.Messaging.json` and the provider manifests, written by `mechanical-fixer`, reported by flag from `coverage-report.cs`; CRAP at most 10 on changed methods (`OutboxBatchProcessor.ProcessAsync` is the one to watch).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```text
CONTEXT: AGENTS.md section 9: per-flag coverage, tests must execute real package code, Shouldly through Encina.Testing.Shouldly, integration tests use shared [Collection("<Family>-<Database>")] fixtures.
TASK: Write the tests of Phase 8 Tasks 1 to 6 and report the per-flag coverage of every touched src/ file against its manifest target.
KEY RULES: AAA, one behavior per test, FakeTimeProvider (no Thread.Sleep); never IClassFixture for database fixtures; all 10 providers; SQLite is out of the matrix.
REFERENCE FILES: tests/Encina.IntegrationTests/Dapper/SqlServer/Outbox/OutboxStoreDapperTests.cs, tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Outbox/OutboxStoreEFIntegrationTests.cs, tests/Encina.IntegrationTests/Infrastructure/MongoDB/Stores/OutboxStoreMongoDBIntegrationTests.cs, .github/coverage-manifest/Encina.Messaging.json.
```

</details>

### Phase 9: Documentation and Finalization

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs on every new public member; `PublicAPI.Unshipped.txt` in every touched package (`mechanical-fixer`).
2. `changelog.d/123-message-priority.added.md` (`mechanical-fixer`).
3. `docs/architecture/adr/`: ADR for the shared priority design (D1 to D4); ADR number assigned when the ADR pull request opens. `docs-writer` writes it and `docs/features/message-priority.md`.
4. Package README of `Encina.Messaging`; `docs/INVENTORY.md`; `ROADMAP.md` only if the milestone changes.
5. `dotnet build --configuration Release` with zero warnings; `dotnet test`; every flag at its target.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```text
CONTEXT: Documentation is written by docs-writer, changelog fragments and PublicAPI lines by mechanical-fixer (path ownership hooks).
TASK: Finish Phase 9 Tasks 1 to 5 after Phases 1 to 8 are merged; link #251, #301 and #1254 from the ADR.
KEY RULES: documentation never types coverage figures by hand (covref markers); English only; no reference to a private client application (write "the reference application").
REFERENCE FILES: changelog.d/README.md, docs/architecture/adr/index.md, docs/plans/dsr-implementation-plan-404.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard or source | Relevance |
| --- | --- |
| Azure Architecture Center, Priority Queue pattern | Defines the pattern; notes that priority belongs to the queue consumer and that starvation needs a policy |
| Enterprise Integration Patterns (Hohpe and Woolf) | Message priority is a channel-level concern; a Resequencer is the opposite (per-key order, #469) |
| RabbitMQ priority queues | Larger number is delivered first, `x-max-priority`; basis for "higher is more urgent" (vendor documentation to confirm at implementation) |
| AGENTS.md sections 3, 5, 6, 7 | Opt-in patterns, 10-provider matrix, 12-function check, EventId rules |
| ADR-001, ADR-006, ADR-018, ADR-021, ADR-050 | ROP, cross-cutting check, EventIds, module dependencies |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
| --- | --- | --- |
| `IOutboxMessage`, `IOutboxStore`, `IOutboxMessageFactory` | `src/Encina.Messaging/Outbox/` | Gain `Priority` and the criteria-based fetch |
| `OutboxBatchProcessor` | `src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs` | Builds the criteria with `TimeProvider`; processes the batch |
| `OutboxOptions.Validate` | `src/Encina.Messaging/Outbox/OutboxOptions.cs` | Validates `OutboxPriorityOptions` |
| `OutboxProcessorMetrics`, `OutboxActivitySource` | `src/Encina.Messaging/Diagnostics/` | Shared `"Encina"` meter and `Encina.Messaging.Outbox` source |
| `OutboxHealthCheck` | `src/Encina.Messaging/Health/OutboxHealthCheck.cs` | Pending depth per priority is deferred to a new issue |
| Provider stores and scripts | `src/Encina.ADO.*`, `src/Encina.Dapper.*`, `src/Encina.EntityFrameworkCore`, `src/Encina.MongoDB` | Column, index, ordered fetch |
| Fakes, builders | `src/Encina.Testing.Fakes`, `Encina.Testing.Bogus`, `tests/Encina.TestInfrastructure` | Updated for the new contract |

### Event ID Allocation

| Package | Range | Notes |
| --- | --- | --- |
| `Encina.Messaging` (`EventIdRanges.MessagingPriority`, new) | 6700-6714 | From the batch block 6700-6749 assigned to this family; this plan uses 6700-6707, 6708-6714 stay reserved for #301 follow-ups. Siblings: #301 6715-6724, #251 6725-6734, #469 6735-6747, #1254 6748-6749 |

### Estimated File Count

| Category | Files | Notes |
| --- | --- | --- |
| Core new types and log file | 6 | Phase 1 and 7 |
| Core modified | 10 | Contracts, options, processor, diagnostics, `EventIdRanges.cs` |
| ADO.NET (3 providers) | 15 | Message, factory, store, 2 scripts each |
| Dapper (3 providers) | 15 | Same |
| EF Core and MongoDB | 8 | EF 4 files serve 3 providers; Mongo 4 files |
| Testing packages and OpenTelemetry | 6 | Fakes, faker, builder, helper, arbitrary, instrumented store |
| Tests | 24 | Unit, guard, contract, property, 10 integration, 2 justification and benchmark files |
| Docs, changelog, PublicAPI | 16 | ADR, feature page, README, changelog fragment, PublicAPI per package |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined Prompt — All Phases</strong></summary>

```text
PROJECT CONTEXT: Encina, pre-1.0, .NET 10 / C# 14. Issue #123, plan docs/plans/message-priority-implementation-plan-123.md. The plan owns the shared priority design of the family (#123, #251, #301, #469, #1254). Merge order: #1254, #123, #301, #469; #251 after #123. Read AGENTS.md first.

IMPLEMENTATION OVERVIEW: (1) Priority types and resolver in Encina.Messaging. (2) IOutboxMessage.Priority, OutboxFetchCriteria, options, processor. (3) Fakes and builders. (4) ADO.NET x3. (5) Dapper x3. (6) EF Core and MongoDB. (7) EventIds 6700-6707, metrics, activity tag. (8) Tests on 10 providers. (9) Docs. Seven pull requests of at most 15 files and 1500 lines.

KEY PATTERNS: Either<EncinaError, T> everywhere; time from TimeProvider (the processor computes the starvation cutoff, stores never read the clock); opt-in via OutboxOptions.Priority.Enabled = false; ORDER BY aged-first, Priority DESC, CreatedAtUtc when enabled and CreatedAtUtc otherwise; static per-type attribute cache; [LoggerMessage] with registered range; shared "Encina" meter; async database calls with CancellationToken; shared [Collection] integration fixtures.

REFERENCE FILES: src/Encina.Messaging/Outbox/*.cs, src/Encina.Dapper.SqlServer/Outbox/OutboxStoreDapper.cs, src/Encina.EntityFrameworkCore/Outbox/OutboxStoreEF.cs, src/Encina.MongoDB/Outbox/OutboxStoreMongoDB.cs, src/Encina/Diagnostics/EventIdRanges.cs, .github/coverage-manifest/Encina.Messaging.json, docs/plans/dsr-implementation-plan-404.md.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Writes and an ordered fetch of pending work; nothing here benefits from a cache |
| 2 | OpenTelemetry | ✅ | Priority counter, wait-time histogram, promotion counter on the shared meter; `encina.outbox.priority` activity tag (Phase 7) |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` 6700-6707 in `EventIdRanges.MessagingPriority` (Phase 7) |
| 4 | Health Checks | ⏭️ | Pending depth per priority needs a count-by-priority store method on 10 providers; issue file `artifacts/issues/outbox-priority-pending-depth.md` |
| 5 | Validation | ✅ | `OutboxPriorityOptions` in `OutboxOptions.Validate`; undefined priority value returns `Left` |
| 6 | Resilience | ❌ | No external call is added; retry and dead-letter handling are unchanged |
| 7 | Distributed Locks | ⏭️ | Multi-instance claiming is #1251; ordering must not assume one instance |
| 8 | Transactions | ✅ | The priority is written in the same `AddAsync` and transaction as the outbox row (Phase 2) |
| 9 | Idempotency | ⏭️ | Duplicate handling is #735, #148 and #1220; unchanged here |
| 10 | Multi-Tenancy | ⏭️ | The outbox has no `TenantId` yet; #737 adds it, and priority ordering is global until then |
| 11 | Module Isolation | ⏭️ | `ModuleId` on messaging entities is #747 |
| 12 | Audit Trail | ❌ | No compliance-relevant data is created; priority is operational metadata |

---

## Prerequisites & Dependencies

- #1254 (nullable `PartitionKey`) merges first because it changes the same provider files and `IOutboxMessageFactory.Create`; this plan then adds the `priority` parameter.
- #301 and #251 wait for choices 1 to 4 here; their plans record the same answers.
- #1251 (row claiming) extends `OutboxFetchCriteria` later; nothing here blocks it.

## Next Steps

1. Maintainer answers the Dilemmas, design choices 1 to 4 (the issue's D1, D3, D2 and the new fetch-contract question), when work starts; answers go to `## Maintainer Decisions`.
2. Orchestrator opens the follow-up issue file for pending depth per priority.
3. Worker briefs are written per pull request (PR-A to PR-G) with the 15-file limit.
4. After merge: close #147 as duplicate of #301 and narrow #251 to the transports.
