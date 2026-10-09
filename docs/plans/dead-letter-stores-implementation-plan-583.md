# Implementation Plan: Persistent `IDeadLetterStore` on the 10 Database Providers

> **Issue**: [#583](https://github.com/dlrivada/Encina/issues/583) (priority P0, milestone v0.19.0 — Providers & Testing)
> **Type**: Feature
> **Complexity**: High (12 phases, all 10 database providers, a reshaped store contract in `Encina.Messaging`)
> **Estimated Scope**: ~2,200-2,900 lines of production code + ~3,000-3,800 lines of tests
> **Scope decision**: maintainer comment on #583, 2026-10-07: the dead letter queue (DLQ) enters 1.0 complete, on exactly the 10 providers of the database matrix. Oracle and SQLite are out of the matrix ([ADR-009](../architecture/adr/009-remove-oracle-provider-pre-1.0.md), [ADR-024](../architecture/adr/024-remove-sqlite-provider-pre-1.0.md)), so the issue title's "13 providers" no longer applies. SPEC-000 records the scope change as DEC-008 (merged as #2008).
> **Follow-up that relies on this plan**: [#1991](https://github.com/dlrivada/Encina/issues/1991) (wire the five `IntegrateWith*` sources to the DLQ, option (a), decided 2026-10-07). Post-1.0 store families: #584-#589.
> **Related plans**: [#718](outbox-atomicity-ado-implementation-plan-718.md) (transaction accessor), [#1200](processed-message-purge-implementation-plan-1200.md) (purge), [#1251](processor-row-claiming-implementation-plan-1251.md) (row claiming)

---

## Summary

Encina gets a database-backed dead letter queue on the 10 providers of the database matrix: ADO.NET, Dapper and EF Core on SQL Server, PostgreSQL and MySQL, plus MongoDB. A message that fails for good is stored in a `DeadLetterMessages` table (or collection). It survives restarts, can be listed, counted, replayed and deleted through `IDeadLetterManager`, and expires after `DeadLetterOptions.RetentionPeriod`. Today the queue exists only as a contract and an in-memory test fake, so a production application cannot use it.

### What the code does today (2026-10-07, `main` at `dd9b8ea5`)

1. **No persistent store.** `IDeadLetterStore` (`src/Encina.Messaging/DeadLetter/IDeadLetterStore.cs:23-98`) has one implementation, `FakeDeadLetterStore` (`src/Encina.Testing.Fakes/Stores/FakeDeadLetterStore.cs:19`). None of the 8 provider packages has a `DeadLetter/` folder or a dead letter table in its `Scripts/` (the highest script is `028_CreateOperationAuditEntriesTable.sql` in all six ADO.NET and Dapper packages).
2. **`UseDeadLetterQueue` registers nothing.** `MessagingConfiguration.UseDeadLetterQueue` (`src/Encina.Messaging/MessagingConfiguration.cs:170`) and `MessagingConfiguration.DeadLetterOptions` (`:739`) are read only by `PatternFlags()` (`:990`). `AddMessagingServices` (`src/Encina.Messaging/MessagingServiceCollectionExtensions.cs:44-74`) and `AddOutboxInboxSagaSchedulingServices` (`:203-247`) never look at them. The only registration path is `AddEncinaDeadLetterQueue<TStore, TFactory>` (`src/Encina.Messaging/DeadLetter/DeadLetterServiceCollectionExtensions.cs:20-54`), which no file under `src/` calls; one unit test does (`tests/Encina.UnitTests/Messaging/Serialization/MessageSerializerRegistrationTests.cs:104`).
3. **The record reads the wall clock.** `IDeadLetterMessage.IsExpired` (`IDeadLetterMessage.cs:108`) is a property, so an implementation can only read the system clock. The fake does exactly that (`src/Encina.Testing.Fakes/Models/FakeDeadLetterMessage.cs:59`, `TimeProvider.System`), and both `DeadLetterManager.IsNotReplayable` (`src/Encina.Messaging/DeadLetter/DeadLetterManager.cs:97`) and `FakeDeadLetterStore.DeleteExpiredAsync` (`FakeDeadLetterStore.cs:257-276`) depend on it. AGENTS.md §3 forbids production code that reads the clock directly.
4. **The record keeps a field that is always null and a name that lies.** The orchestrator never fills `ExceptionMessage`, because it can carry personal data (`IDeadLetterMessageFactory.cs:20-23`, `DeadLetterOrchestrator.cs:121,216`). `ErrorMessage` holds the `EncinaError` code, not a message (`IDeadLetterMessageFactory.cs:9-12`, `DeadLetterOrchestrator.cs:107,113`), while the filter calls the same value `ErrorCode` (`DeadLetterFilter.cs:24`), and the fake ignores that filter (`FakeDeadLetterStore.cs:176-222`).
5. **The order of results is undefined, and the code assumes two opposite orders.** `GetStatisticsAsync` treats the first row of `GetMessagesAsync` as the oldest and the row at `skip: pendingCount - 1` as the newest (`DeadLetterOrchestrator.cs:311-334`). The fake returns rows newest first (`FakeDeadLetterStore.cs:159`), so on the fake the statistics swap "oldest" and "newest".
6. **Bulk operations load the whole queue and swallow errors.** `GetStatisticsAsync` reads every pending row (`take: int.MaxValue`) to count expired ones (`DeadLetterOrchestrator.cs:342-350`). `DeleteAllAsync` does the same and then deletes row by row (`DeadLetterManager.cs:315-330`). It turns a `Left` from `DeleteAsync` into "not deleted" (`:325`) and ignores the result of `SaveChangesAsync` (`:334`). `RecordReplayOutcomeAsync` ignores both store results (`:107-111`). AGENTS.md §3 says errors are never swallowed.
7. **The health check reports a broken store as healthy.** `DeadLetterHealthCheck` maps a `Left` from `GetCountAsync` to a count of 0 (`src/Encina.Messaging/Health/DeadLetterHealthCheck.cs:51-53`), and a `Left` from the old-message query to "no old messages" (`:75-77`). With a persistent store, a database outage reads as "DLQ is empty". Its registration uses `TryAddScoped<IEncinaHealthCheck>` (`DeadLetterServiceCollectionExtensions.cs:45`). That call is skipped whenever any other `IEncinaHealthCheck` is already registered, for example the provider health check that `AddEncinaADO` adds (`src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs:135`).
8. **No tenant, no idempotency key.** The record has no `TenantId`, and nothing identifies the source message. SPEC-000 §2 states that "retrofitting a tenant key into persisted shapes after 1.0 would break the API and the schema" (`docs/specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md:31`). #1991 will dead-letter outbox rows that SPEC-002 REQ-017 keeps in the outbox table (`docs/specifications/SPEC-002-eu-regulatory-readiness.md:288`), so the same source message can reach the DLQ more than once.

### Scope

- **Implements**: #583 on the 10 providers, plus the contract defects above (items 2-7), because a persistent store makes each of them observable in production.
- **Affected packages**:
  - `Encina.Messaging`: record, data, context, filter, store contract, orchestrator, manager, health check, options, DI and diagnostics.
  - `Encina.ADO.SqlServer`, `Encina.ADO.PostgreSQL`, `Encina.ADO.MySQL`.
  - `Encina.Dapper.SqlServer`, `Encina.Dapper.PostgreSQL`, `Encina.Dapper.MySQL`.
  - `Encina.EntityFrameworkCore` (one package for SQL Server, PostgreSQL and MySQL).
  - `Encina.MongoDB`.
  - `Encina.OpenTelemetry` (new `InstrumentedDeadLetterStore`) and `Encina.Testing.Fakes` (fake store and message follow the new contract).
- **Provider category**: Database, all 10 providers (AGENTS.md §5). SQLite and Oracle are out of the matrix.
- **Out of scope**: wiring the five sources to the DLQ (#1991); Marten, EventStoreDB, Elasticsearch, Kafka, Azure and DynamoDB stores (#584-#589, post-1.0); moving `DeadLetterCleanupProcessor` to `Encina.Scheduling` (#771); the DLQ documentation gaps that #1990 owns, except the provider registration this plan adds.
- **Estimated files**: ~60 production files created or touched, ~45 test files.

---

## Design Choices

<details>
<summary><strong>1. Persisted record shape — reshape the record before it is persisted</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reshape, then persist**: rename `ErrorMessage` to `ErrorCode`; delete `ExceptionMessage`; add `SourceMessageId` (string, required) and `TenantId` (string, nullable) to `IDeadLetterMessage`, `DeadLetterData` and the columns | Column names say what they hold (the code, #1274); no column that is always null and could later receive personal data; the identity and tenant keys exist before 1.0 (SPEC-000 §2) | Touches the interface, the fake, the orchestrator and their tests; `DeadLetterFilter.ErrorCode` finally maps to a real column |
| **B) Persist the interface as it is, plus `TenantId`** | Smallest change to `Encina.Messaging` | 10 schemas get an `ErrorMessage` column that holds codes and an `ExceptionMessage` column that must stay empty forever; no key for #1991's idempotency (Choice 6) |
| **C) The issue's schema** (`id`, `message_id`, `payload`, `source`, `created_at_utc`, snake_case) | Matches the issue text | Does not match `IDeadLetterMessage` (no expiry, replay or first-failure columns); snake_case breaks the PascalCase convention of every Encina table; the record and the table drift apart |

### Chosen Option: **A — reshape the record, then persist it** (decided 2026-10-09)

### Rationale

- Decided A (2026-10-09) because the persisted shape is the part that is expensive to change after 1.0. Every rename done now is free; every rename done later is a migration on 10 providers.
- `ExceptionMessage` is never written (`DeadLetterOrchestrator.cs:121,216`) because an exception message can hold personal data. A column that must always be empty is an invitation to fill it.
- `SourceMessageId` and `TenantId` are the inputs of Choices 6 and 7. A only creates the columns; those choices decide how they are used.

</details>

<details>
<summary><strong>2. Expiry evaluation — computed from an explicit instant, never from the wall clock</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Replace the `IsExpired` property with `IsExpiredAt(DateTime utcNow)`**; keep `IsReplayed` (pure, from `ReplayedAtUtc`); the manager passes `TimeProvider.GetUtcNow()`; stores compute "expired" in SQL from their own `TimeProvider` | No clock read in a record; deterministic with `FakeTimeProvider`; the store predicate and the in-memory check use the same rule (`ExpiresAtUtc <= now`) | One interface member changes; callers of `IsExpired` move to the method |
| **B) Keep the property; implementations read `TimeProvider.System`** | No interface change | Violates AGENTS.md §3 in 10 new entity classes; time-dependent tests need real delays |
| **C) Remove both computed members**; the manager compares `ExpiresAtUtc` itself | Smallest interface | Every caller repeats the rule; `IsReplayed` is pure and useful, so removing it gains nothing |

### Chosen Option: **A — `IsExpiredAt(DateTime utcNow)`** (decided 2026-10-09)

### Rationale

- Decided A (2026-10-09) because it removes the only reason a persisted entity would read the clock, and it makes the expiry rule one expression shared by the 10 stores, the fake and the manager.
- The boundary is fixed as `ExpiresAtUtc <= utcNow` (expired at the instant of expiry), the form every SQL predicate and MongoDB filter uses. The fake today treats that instant as not expired (`now > ExpiresAtUtc`, `FakeDeadLetterMessage.cs:59`); it changes with the rest of the fake, and the contract test pins the boundary.

</details>

<details>
<summary><strong>3. Package placement — a <code>DeadLetter/</code> folder in each existing provider package</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `DeadLetter/` folder in the 8 existing provider packages** (`DeadLetterMessage`, `DeadLetterMessageFactory`, `DeadLetterStore{Provider}`), next to `Outbox/`, `Inbox/`, `Scheduling/` | Same shape as the four messaging patterns; reuses each package's connection registration, scripts folder, coverage manifest and integration collection; no new NuGet package | The 8 packages grow by ~3 files each |
| **B) 10 new satellite packages** (`Encina.ADO.SqlServer.DeadLetter`, ...) | Pay-for-what-you-use at the package level | 10 new packages, manifests, READMEs and release entries for ~300 lines each; the other messaging stores do not live in satellites, so the layout becomes inconsistent |
| **C) One `Encina.Messaging.DeadLetter.Sql` package with a dialect strategy** for the 6 ADO.NET/Dapper variants | Less SQL duplication | Breaks provider coherence (AGENTS.md §5: providers differ only in implementation, registered by their own package); the dialect strategy would need its own connection handling, duplicating each package's |

### Chosen Option: **A — `DeadLetter/` folder in the existing packages** (decided 2026-10-09)

### Rationale

- Decided A (2026-10-09) because the DLQ is a messaging pattern like the other four, and AGENTS.md §4 places feature stores in a folder named after the feature.
- The feature stays opt-in through `UseDeadLetterQueue` (Choice 4), so placing the code in the existing packages does not make anyone pay at run time.

</details>

<details>
<summary><strong>4. Registration — <code>UseDeadLetterQueue</code> drives the provider registration through one shared helper</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Flag-driven, like the other patterns**: `MessagingConfiguration.UseDeadLetterQueue` (and new `EncinaMongoDbOptions.UseDeadLetterQueue` + `DeadLetterOptions`) make each provider call a new public core helper `AddDeadLetterQueueServices<TStore, TFactory>(bool, DeadLetterOptions)`; `AddEncinaDeadLetterQueue<TStore, TFactory>` becomes a thin wrapper over the same helper; store and factory use `TryAdd` | The existing flag finally does something; one registration body for 8 packages (#1333's reason for the shared outbox/inbox helper); the application's own `IDeadLetterStore` (or the fake) still wins | MongoDB options gain two members |
| **B) Satellite methods `AddEncinaDeadLetterQueue{Provider}()`** called before the core registration (the prompt's satellite rule) | Explicit per provider; follows the generic satellite convention | A second way to enable a pattern that already has a flag; `UseDeadLetterQueue` stays dead or must be deleted; 8 new public methods |
| **C) Keep only the generic method**; applications pass the provider's store and factory types | No new API | Applications must know internal type names per provider; `UseDeadLetterQueue` stays dead; the reason no one uses the DLQ today remains |

### Chosen Option: **A — flag-driven registration through `AddDeadLetterQueueServices`** (decided 2026-10-09)

### Rationale

- Decided A (2026-10-09) because outbox, inbox, saga and scheduling are all enabled by a flag on the same configuration object. A flag that registers nothing is the defect class #1991 and #1969 already report.
- The helper also fixes the health-check registration (`TryAddEnumerable` instead of `TryAddScoped<IEncinaHealthCheck>`, which is skipped when another health check exists) and registers `DeadLetterHealthCheckOptions` with `TryAddSingleton`, so `ValidateOnBuild` passes with or without custom thresholds.
- No in-memory production default exists for `IDeadLetterStore`, so "the store wins over the in-memory default" reduces to `TryAdd`: a store or fake registered by the application before the provider is kept, and a provider registered first is not overridden by a later `AddEncinaDeadLetterQueue` call with other types (documented, tested).

</details>

<details>
<summary><strong>5. Store contract — one defined order and set-based operations</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Oldest first, set-based**: `GetMessagesAsync` orders by `DeadLetteredAtUtc` then `Id`, ascending (contract); new `DeleteManyAsync(DeadLetterFilter, ct) -> Either<EncinaError, int>` replaces the manager's per-row loop; `MarkAsReplayedAsync` returns `Either<EncinaError, bool>` and updates only when `ReplayedAtUtc IS NULL`; `DeadLetterFilter` gains `ExpiresAtOrBeforeUtc` so the orchestrator counts expired rows with `GetCountAsync`; newest pending comes from a `newestFirst` flag on `GetMessagesAsync` | Statistics become correct on every store; no operation loads the whole queue; no swallowed `Left`; one statement per bulk delete | 10 stores implement two more predicates and one more method; manager and orchestrator change |
| **B) Keep the contract; fix only the orchestrator** to read in the fake's order (newest first) | Fewer store changes | The order is still not part of the contract, so 10 stores may each pick one; `int.MaxValue` reads and the swallowed `Left` stay |
| **C) A plus a store-side `GetStatisticsAsync`** (one grouped query) | One round trip for statistics | A grouped query per dialect and per MongoDB aggregation for an operator screen; A already removes the full scans, and the remaining counts are indexed |

### Chosen Option: **A — oldest first, set-based operations** (decided 2026-10-09)

### Rationale

- Decided A (2026-10-09) because the persistent stores make the two defects real: on a large queue, `take: int.MaxValue` loads every payload into memory, and a failed delete is reported as "nothing to delete".
- Oldest first matches the operator's job (work the backlog in arrival order) and the orchestrator's existing reading of "first row".
- The conditional `MarkAsReplayedAsync` lets the manager detect that a concurrent replay already recorded an outcome. Making replay exclusive before dispatch is the atomic claim of B3, implemented in this PR with #2012.

</details>

<details>
<summary><strong>6. Idempotent capture — unique source key now, used by #1991</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Unique index on `(SourcePattern, SourceMessageId)`**; `AddAsync` returns `Either<EncinaError, bool>` (`false` when the source message is already dead-lettered); `DeadLetterContext` gains `SourceMessageId` (defaults to the new dead letter id when the caller has none) | A retried source (outbox row requeued under REQ-017, a recoverability retry after a crash) never produces two dead letters; #1991 relies on it without a schema change; `OnDeadLetter` runs once per source message | Insert-if-absent SQL per dialect; EF Core detects duplicates with a query before tracking, so a race between two hosts surfaces as a unique-violation `Left` |
| **B) Defer the key to #1991** | Smaller #583 | The 10 schemas change twice before 1.0; #1991 must add a column and a migration path to every provider |
| **C) No deduplication** | Simplest | Duplicate dead letters on every retry of a source that keeps its row (REQ-017); replaying both dispatches twice |

### Chosen Option: **A — unique `(SourcePattern, SourceMessageId)` with `AddAsync -> bool`** (decided 2026-10-09)

### Rationale

- Decided A (2026-10-09) because SPEC-002 REQ-017 keeps dead-lettered outbox messages in the outbox table and lets them be requeued. A message can therefore exhaust its retries twice, and #1991 will dead-letter it each time.
- `SourceMessageId` is a string because the sources use different identifiers: `Guid` for outbox, scheduled messages and sagas, `string` for inbox message ids.
- Without a source id, the orchestrator uses the new dead letter id, so the key never blocks unrelated messages.

</details>

<details>
<summary><strong>7. Tenancy — tenant column stamped at capture, explicit store filter, manager defaults to the ambient tenant</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `TenantId` column stamped from `IRequestContext.TenantId` at capture** (`DeadLetterContext` may override it, for sources that restore a persisted tenant), plus `DeadLetterFilter.TenantId`; the store contract is explicit (a store query returns every tenant unless the filter names one), while `DeadLetterManager` reads, replays and deletes default to the ambient `IRequestContext.TenantId` when present, with an explicit all-tenants option (`DeadLetterFilter.AllTenants`, B7) | The tenant is persisted from day one (SPEC-000 §2); a tenant admin screen cannot show another tenant's dead letters by forgetting the filter (SPEC-002 REQ-061); operator tooling opts in to every tenant; single-tenant applications need no configuration | The store itself does no implicit scoping, so direct store callers must set the filter |
| **B) Column plus implicit scoping** to the current `ITenantContext`, failing closed when the application is multi-tenant and no tenant is present | Tenant isolation by default on reads | The DLQ is an operator tool: background cleanup and platform operators have no tenant, so they would be denied or need an opt-out on every call; the cleanup processor would need per-tenant loops |
| **C) No column now; defer** as #737/#739 did for outbox and scheduled rows | No tenancy work in #583 | Exactly the post-1.0 retrofit SPEC-000 §2 warns against; #1991 could not carry the source's tenant |

### Chosen Option: **A — stamped column, explicit store filter, ambient-tenant default in the manager** (decided 2026-10-09; B7)

### Rationale

- Decided A because the persisted shape must carry the tenant before 1.0. The store contract stays explicit; the cleanup processor and the health check stay deployment-wide, and `DeadLetterManager` (and `GetStatisticsAsync`) default to the ambient tenant (B7).
- The orchestrator reads `IRequestContextAccessor` (already registered by `AddOutboxInboxSagaSchedulingServices`, `MessagingServiceCollectionExtensions.cs:235`). Telemetry carries no tenant id (SPEC-002 REQ-062: no identifiers in telemetry).
- Replaying under the persisted tenant, rather than the operator's context, is a separate gap: #2013.

</details>

<details>
<summary><strong>8. Expired-row cleanup on MongoDB — the same application loop as the other nine providers</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Application loop only**: `DeleteExpiredAsync` run by `DeadLetterCleanupProcessor` (`DeadLetterCleanupProcessor.cs:45-89`), plus an ordinary index on `ExpiresAtUtc` | Same behaviour on all 10 providers; `EnableAutomaticCleanup = false` and `RetentionPeriod = null` are honoured; metrics and logs count every deletion | One more periodic query on MongoDB |
| **B) TTL index on `ExpiresAtUtc`** (`expireAfterSeconds: 0`), as the inbox does | No loop needed on MongoDB | Ignores `EnableAutomaticCleanup = false`; deletes outside Encina's metrics and logs; the processor still runs and finds nothing, so MongoDB reports different numbers than the other 9 providers |
| **C) Both** | Belt and braces | The cons of B, plus two mechanisms to reason about |

### Chosen Option: **A — application loop only** (decided 2026-10-09)

### Rationale

- Decided A (2026-10-09) because the DLQ options promise a switch (`EnableAutomaticCleanup`, `DeadLetterOptions.cs:28`) that a TTL index cannot honour.
- #771 may later move the loop to `Encina.Scheduling`. It will still call the same `DeleteExpiredAsync`, so A stays valid after #771.

</details>

<details>
<summary><strong>9. PostgreSQL identifiers — quoted PascalCase shared by ADO.NET, Dapper and EF Core</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Quoted PascalCase** (`"DeadLetterMessages"`, `"SourcePattern"`), as the newest scripts do (`src/Encina.ADO.PostgreSQL/Scripts/028_CreateOperationAuditEntriesTable.sql:7,28`) | ADO.NET, Dapper and EF Core (which quotes `ToTable("DeadLetterMessages")`) read and write the same table; one integration schema serves all three families | Every PostgreSQL statement quotes its identifiers |
| **B) Unquoted lowercase** (`deadlettermessages`), as the older messaging tables do (`src/Encina.Dapper.PostgreSQL/Scheduling/ScheduledMessageStoreDapper.cs:30`) | Same as the other messaging tables | EF Core's table differs from ADO.NET/Dapper's; switching provider family means a different table |

### Chosen Option: **A — quoted PascalCase** (decided 2026-10-09)

### Rationale

- Decided A (2026-10-09) because provider coherence (AGENTS.md §3) means switching the DI registration, not the schema. With A, one PostgreSQL table works for all three families.
- The older messaging tables are not touched here. Aligning them is not needed for this issue.

</details>

---

## Implementation Phases

### Phase 1: Core Record & Store Contract

> **Goal**: The reshaped record, data, context, filter and store contract in `Encina.Messaging` (Choices 1, 2, 5, 6, 7).

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Messaging/DeadLetter/IDeadLetterMessage.cs`**:
   - rename `ErrorMessage` to `ErrorCode`;
   - delete `ExceptionMessage` and `IsExpired`;
   - add `string SourceMessageId { get; set; }`, `string? TenantId { get; set; }` and `DateTime? ReplayClaimedAtUtc { get; set; }` (B3);
   - `ReplayResult` now holds an outcome code only (`success`, `dlq.replay_failed` or the `EncinaError` code), never error text;
   - `ActorId` and `CausationId` are **not** added by this plan (B8): #2013 adds them;
   - add `bool IsExpiredAt(DateTime utcNow)` (`ExpiresAtUtc is { } e && e <= utcNow`, documented as the rule every store applies in SQL).
2. **`IDeadLetterMessageFactory.cs`** (`DeadLetterData` record): rename `ErrorMessage` to `ErrorCode`; delete `ExceptionMessage`; add `SourceMessageId` (required) and `TenantId` (optional).
3. **`DeadLetterOrchestrator.cs`** (`DeadLetterContext` record, `:17-23`): add `string? SourceMessageId = null` and `string? TenantId = null`.
4. **`DeadLetterFilter.cs`**: add `TenantId`, `ExpiresAtOrBeforeUtc`, and `SourceMessageId`; keep `ErrorCode` (now a real column). Add `bool AllTenants { get; init; }` (default `false`, B7): the explicit all-tenants option for operator tooling. The manager applies the ambient `IRequestContext.TenantId` when `TenantId` is null, `AllTenants` is false and an ambient tenant exists; stores ignore `AllTenants` (they filter only by `TenantId`). `PublicAPI.Unshipped.txt` lists `DeadLetterFilter.AllTenants`.
5. **`IDeadLetterStore.cs`**:
   - `Task<Either<EncinaError, bool>> AddAsync(IDeadLetterMessage message, CancellationToken)`: `false` when `(SourcePattern, SourceMessageId)` already exists.
   - `GetMessagesAsync(DeadLetterFilter? filter, int skip, int take, bool newestFirst = false, CancellationToken)`: order `DeadLetteredAtUtc, Id` ascending (descending when `newestFirst`), documented as the contract.
   - `Task<Either<EncinaError, bool>> MarkAsReplayedAsync(Guid, string, CancellationToken)`: updates only when `ReplayedAtUtc IS NULL`; `false` when not found or already replayed. The string is an **outcome code** (B3), never error text.
   - Atomic replay claim (B3, folded with #2012; the method name is fixed at implementation): a store method that claims one message before dispatch with a conditional update on `ReplayedAtUtc IS NULL AND (ReplayClaimedAtUtc IS NULL OR the claim has expired)`. The claim expiry is `DeadLetterOptions.ReplayClaimTimeout` (`TimeSpan`, default 5 minutes, validated > 0 at start, Phase 3 task 3); the manager receives `DeadLetterOptions` and passes the computed "claim expired before" instant to the store, returning `true` only to the caller that won the claim. The claim expiry comes from the manager's options and the store's `TimeProvider`.
   - `Task<Either<EncinaError, int>> DeleteManyAsync(DeadLetterFilter filter, CancellationToken)`: one set-based delete. `DeleteAllAsync(DeadLetterFilter.All)` keeps deleting the whole queue in one statement (B9): state it in the XML docs; no confirmation parameter.
   - `DeleteExpiredAsync` unchanged in shape; documented predicate `ExpiresAtUtc IS NOT NULL AND ExpiresAtUtc <= now` with `now` from the store's `TimeProvider`.
   - Argument rules documented on the interface: `skip >= 0`, `1 <= take <= DeadLetterStoreLimits.MaxPageSize` (1,000), `messageId != Guid.Empty`, `replayResult` not null or whitespace.
6. **New `src/Encina.Messaging/DeadLetter/DeadLetterStoreLimits.cs`** (public static class): `MaxPageSize = 1000`, `SourcePatternMaxLength = 64`, `SourceMessageIdMaxLength = 256`, `RequestTypeMaxLength = 1000`, `ErrorCodeMaxLength = 256`, `TenantIdMaxLength = 128`, `CorrelationIdMaxLength = 256`, `ReplayResultMaxLength = 256` (an outcome code, B3); every schema and EF configuration takes its lengths from here.
7. **`DeadLetterErrorCodes.cs`**: add `InvalidMessageType = "dlq.invalid_message_type"` (EF store given a foreign record type) and `InvalidArgument`-style codes only if a store needs one beyond `ArgumentException`.
8. **`PublicAPI.Unshipped.txt`** of `Encina.Messaging`: remove the stale lines (RS0017) and add the new members (RS0016).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #583 in Encina: the reshaped dead letter record and store contract.

CONTEXT:
- Encina is pre-1.0 (.NET 10, C# 14, nullable on). No compatibility layers, no [Obsolete].
- src/Encina.Messaging/DeadLetter/IDeadLetterMessage.cs has ErrorMessage (holds the EncinaError code),
  ExceptionMessage (never written, may hold personal data) and IsExpired (forces a wall-clock read).
- IDeadLetterStore has no defined result order, no set-based delete, and AddAsync/MarkAsReplayedAsync return Unit.
- Plan decisions (decided 2026-10-09, see the plan's Design Choices 1, 2, 5, 6, 7 and B3, B7).

TASK:
1. IDeadLetterMessage: ErrorMessage -> ErrorCode; delete ExceptionMessage and IsExpired; add SourceMessageId
   (string), TenantId (string?), bool IsExpiredAt(DateTime utcNow) (ExpiresAtUtc <= utcNow).
2. DeadLetterData: same renames and additions. DeadLetterContext: SourceMessageId?, TenantId?.
3. DeadLetterFilter: TenantId, ExpiresAtOrBeforeUtc, SourceMessageId, bool AllTenants (init, default false; manager
   only; add it to PublicAPI.Unshipped.txt). IDeadLetterMessage also gets DateTime? ReplayClaimedAtUtc.
4. IDeadLetterStore: AddAsync -> Either<EncinaError, bool>; GetMessagesAsync gains bool newestFirst = false and a
   documented order (DeadLetteredAtUtc, Id); MarkAsReplayedAsync -> Either<EncinaError, bool>, conditional on
   ReplayedAtUtc IS NULL (outcome code only); an atomic replay-claim method (ReplayClaimedAtUtc, with claim
   expiry); new DeleteManyAsync(DeadLetterFilter, ct) -> Either<EncinaError, int> (DeleteAllAsync(All) documented).
   Do NOT add ActorId or CausationId (#2013).
5. New DeadLetterStoreLimits with the column lengths (ReplayResult 256) and MaxPageSize = 1000.
6. Update PublicAPI.Unshipped.txt (RS0016/RS0017 clean).

KEY RULES:
- XML docs on every public member state the contract (order, predicates, return values, argument rules).
- Times are UTC DateTime with the AtUtc suffix; no member reads TimeProvider.System or DateTime.UtcNow.
- The build will break in the orchestrator, manager, fake and tests until Phase 2; land Phases 1-8 in one PR.

REFERENCE FILES:
- src/Encina.Messaging/DeadLetter/IDeadLetterMessage.cs
- src/Encina.Messaging/DeadLetter/IDeadLetterMessageFactory.cs
- src/Encina.Messaging/DeadLetter/IDeadLetterStore.cs
- src/Encina.Messaging/DeadLetter/DeadLetterFilter.cs
- src/Encina.Messaging/DeadLetter/DeadLetterOrchestrator.cs (DeadLetterContext at the top)
```

</details>

---

### Phase 2: Orchestrator, Manager, Health Check & Fakes

> **Goal**: The core components and the test fake follow the new contract, and no store result is swallowed.

<details>
<summary><strong>Tasks</strong></summary>

1. **`DeadLetterOrchestrator.cs`**:
   - New constructor dependency `IRequestContextAccessor` (`TimeProvider` stays optional). `AddAsync` and `AddFromFailedMessageAsync` stamp `TenantId` (context override, else `IRequestContext.TenantId`) and `SourceMessageId` (context value, `FailedMessage.Id` for the recoverability path, else the new dead letter id).
   - `PersistAsync` returns the store's `bool`. On `false`, log `DeadLetterDuplicateIgnored` (Phase 10) and return the existing row (`GetMessagesAsync` with `SourcePattern` + `SourceMessageId`) without invoking `OnDeadLetter`.
   - On a `Left` from the store, log `DeadLetterStoreWriteFailed` with the error code only, then return the `Left`.
   - `GetStatisticsAsync`: oldest pending from `GetMessagesAsync(take: 1)`, newest from `newestFirst: true, take: 1`, expired count from `GetCountAsync` with `ExcludeReplayed = true` and `ExpiresAtOrBeforeUtc = now`. No `int.MaxValue` read remains.
2. **`DeadLetterManager.cs`**:
   - inject `TimeProvider?` (default `TimeProvider.System`) and `DeadLetterOptions` (for `ReplayClaimTimeout`); `IsNotReplayable` uses `message.IsExpiredAt(now)`;
   - `RecordReplayOutcomeAsync` returns `Either<EncinaError, bool>`. A `Left` is logged as `ReplayOutcomeNotRecorded` (error code only) and, for `ReplayAsync`, returned. When the update finds the row already replayed, the result says so (`ReplayResult.Failed` with `dlq.already_replayed`);
   - **outcome code and claim (B3, with #2012)**: the manager stops writing message text (`"Failed: {errorMessage}"`, `DeadLetterManager.cs:83,118,133-138`); the stored `ReplayResult` is an outcome code only (max 256). `ReplayAsync` first calls the atomic claim (`ReplayClaimedAtUtc`, with claim expiry); a lost claim returns a failed result without dispatching;
   - **tenant default (B7)**: reads, replays and deletes default to `IRequestContext.TenantId` when it is present, with the explicit `DeadLetterFilter.AllTenants` option for operator tooling; `DeadLetterOrchestrator.GetStatisticsAsync` follows the same ambient-tenant default; with no ambient tenant the manager behaves as before. The store contract stays explicit (no implicit scoping in `IDeadLetterStore`), and the cleanup processor and the health check stay deployment-wide;
   - `DeleteAllAsync` calls `DeleteManyAsync(filter)` then `SaveChangesAsync`, propagating either `Left`; `DeleteAllAsync(DeadLetterFilter.All)` deletes the whole queue in one statement, documented in the XML docs and covered by one unit test (B9);
   - `ReplayAllAsync` no longer mutates the caller's filter (`DeadLetterManager.cs:220`): it copies it.
3. **`src/Encina.Messaging/Health/DeadLetterHealthCheck.cs`**: done by #2011, which lands first as its own PR (B2); this plan only rebases onto it. For reference, #2011 makes:
   - a `Left` from either query return `Unhealthy` with `data["error_code"]` and no message (fail closed);
   - `public const string DefaultName = "encina-deadletter"` and a static `Tags` array.
4. **`src/Encina.Testing.Fakes/Models/FakeDeadLetterMessage.cs`** and **`Stores/FakeDeadLetterStore.cs`**:
   - same members as the interface; ascending order with `newestFirst` support;
   - `ErrorCode`, `TenantId`, `SourceMessageId` and `ExpiresAtOrBeforeUtc` filters;
   - uniqueness on `(SourcePattern, SourceMessageId)`; conditional `MarkAsReplayedAsync`; the atomic replay claim; `DeleteManyAsync`;
   - order rules (B10): `AddFakeDeadLetterStore` keeps `TryAddSingleton` (the first registration wins); a test that registers the provider first uses `ReplaceWithFakes()` (`src/Encina.Testing.Fakes/ServiceCollectionExtensions.cs:140`); `FakeDeadLetterMessage.cs:41,44,59` also read `TimeProvider.System` as property initialisers and take their time from the fake's own `TimeProvider`;
   - `DeleteExpiredAsync` uses the injected `TimeProvider`, not `IsExpired`;
   - `PublicAPI.Unshipped.txt` of `Encina.Testing.Fakes` updated.
5. **`DeadLetterCleanupProcessor.cs`**: no behaviour change (already logs the code of a `Left`, `:75-77`). Its XML docs name #771 as the planned move to `Encina.Scheduling`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #583 in Encina: orchestrator, manager, health check and fakes on the new
dead letter contract.

CONTEXT:
- Phase 1 reshaped IDeadLetterMessage (ErrorCode, SourceMessageId, TenantId, IsExpiredAt) and IDeadLetterStore
  (AddAsync -> bool, ordered GetMessagesAsync with newestFirst, conditional MarkAsReplayedAsync -> bool,
  DeleteManyAsync).
- Today DeadLetterManager swallows store Lefts (RecordReplayOutcomeAsync, DeleteAllAsync), GetStatisticsAsync reads
  int.MaxValue rows, and DeadLetterHealthCheck maps a Left to "0 pending" (healthy).

TASK:
1. DeadLetterOrchestrator: inject IRequestContextAccessor; stamp TenantId and SourceMessageId; handle the duplicate
   (false) result without invoking OnDeadLetter; log Lefts by error code; rewrite GetStatisticsAsync with
   GetCountAsync + ordered take-1 reads.
2. DeadLetterManager: TimeProvider for IsExpiredAt; propagate every store Left; DeleteAllAsync via DeleteManyAsync;
   copy the filter in ReplayAllAsync instead of mutating it.
3. DeadLetterHealthCheck: already fixed by #2011 (Left -> Unhealthy with data["error_code"]; DefaultName const and static Tags), which lands first; do not redo it.
4. FakeDeadLetterMessage and FakeDeadLetterStore: the full new contract, TimeProvider-driven expiry (including the property initialisers at FakeDeadLetterMessage.cs:41,44,59), order rules and ReplaceWithFakes().
5. DeadLetterManager (with #2012): ReplayResult stores an outcome code only (max 256); atomic claim via ReplayClaimedAtUtc before dispatch; default to the ambient tenant with an explicit all-tenants option; DeleteAllAsync(All) documented and tested.

KEY RULES:
- EncinaError.Message never reaches logs, health data, activity tags or ReplayResult; use GetCode().
- Errors are never swallowed: every store Left fails the operation that received it.
- FakeTimeProvider in tests; no DateTime.UtcNow, no TimeProvider.System reads outside defaults.
- CRAP <= 10 on every changed method: split GetStatisticsAsync into small helpers.

REFERENCE FILES:
- src/Encina.Messaging/DeadLetter/DeadLetterOrchestrator.cs
- src/Encina.Messaging/DeadLetter/DeadLetterManager.cs
- src/Encina.Messaging/Health/DeadLetterHealthCheck.cs
- src/Encina.Testing.Fakes/Stores/FakeDeadLetterStore.cs
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs (IRequestContextAccessor registration)
```

</details>

---

### Phase 3: Configuration, DI & Registration

> **Goal**: `UseDeadLetterQueue` registers the provider's store on all 10 providers (Choice 4), with complete and verified registrations.

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Messaging/MessagingServiceCollectionExtensions.cs`**: new public `AddDeadLetterQueueServices<TStore, TFactory>(this IServiceCollection, bool useDeadLetterQueue, DeadLetterOptions options)`. When enabled it registers:
   - `options` as a singleton;
   - `TryAddSingleton(TimeProvider.System)`, `IRequestContextAccessor` and `TryAddDefaultMessageSerializer()`;
   - `TryAddScoped<IDeadLetterStore, TStore>` and `TryAddScoped<IDeadLetterMessageFactory, TFactory>`;
   - `TryAddScoped<DeadLetterOrchestrator>` and `TryAddScoped<IDeadLetterManager, DeadLetterManager>`;
   - `TryAddSingleton(new DeadLetterHealthCheckOptions())` and `TryAddEnumerable(ServiceDescriptor.Scoped<IEncinaHealthCheck, DeadLetterHealthCheck>())` (the `TryAddEnumerable` registration is #2011's change, which lands first; keep it);
   - `AddHostedService<DeadLetterCleanupProcessor>()` when cleanup is on.
2. **`DeadLetterServiceCollectionExtensions.cs`**: both `AddEncinaDeadLetterQueue` overloads delegate to the helper (the health-check overload replaces the `DeadLetterHealthCheckOptions` registration instead of adding a second one).
3. **`DeadLetterOptions.cs`**: add `TimeSpan ReplayClaimTimeout` (default 5 minutes, B3); setter validation (`RetentionPeriod` > 0 when set, `CleanupInterval` > 0, `ReplayClaimTimeout` > 0), `ArgumentOutOfRangeException` with the parameter name; invalid values fail at start. `PublicAPI.Unshipped.txt` lists it.
4. **Provider registrations** (each with a `DeadLetter/` using):
   - `src/Encina.ADO.{SqlServer,PostgreSQL,MySQL}/ServiceCollectionExtensions.cs`, private `AddEncinaADO(config)` (`ADO.SqlServer` `:62-87`): `services.AddDeadLetterQueueServices<DeadLetterStoreADO, DeadLetterMessageFactory>(config.UseDeadLetterQueue, config.DeadLetterOptions)`.
   - `src/Encina.ADO.{SqlServer,PostgreSQL,MySQL}/Tenancy/TenancyServiceCollectionExtensions.cs` (B5): `AddEncinaADOWithTenancy` (`:53`, which reaches `AddMessagingServicesCore`, `MessagingServiceCollectionExtensions.cs:358`, and wires only outbox and inbox) also calls `AddDeadLetterQueueServices` with the same store and factory.
   - `src/Encina.Dapper.{SqlServer,PostgreSQL,MySQL}/ServiceCollectionExtensions.cs`: the same with `DeadLetterStoreDapper`.
   - `src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs` (next to `:165`): the same with `DeadLetterStoreEF`.
   - `src/Encina.MongoDB/EncinaMongoDbOptions.cs`: `UseDeadLetterQueue`, `DeadLetterOptions` (get-only), and `MongoDbCollectionNames.DeadLetterMessages = "dead_letter_messages"`. `ServiceCollectionExtensions.RegisterCommonServices` (`:616`) calls the helper with `DeadLetterStoreMongoDB`.
5. **DI tests** (Phase 11 writes them; listed here as acceptance): for one provider of each family and for MongoDB, `BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true })` with `UseDeadLetterQueue = true` resolves `IDeadLetterManager`, `DeadLetterOrchestrator`, every `IEncinaHealthCheck` and the hosted services. A store registered by the application before the provider is kept, in both registration orders. Order rules (B10): for `TryAdd`-style registrations (including `AddFakeDeadLetterStore`) the first registration wins, for a plain `AddScoped<IDeadLetterStore, ...>` the last does; tests that registered the provider first use `ReplaceWithFakes()`. One extra DI test covers the `AddEncinaADOWithTenancy` path of each ADO package (B5, `ValidateOnBuild` + `ValidateScopes`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #583 in Encina: registration of the dead letter queue on the 10 providers.

CONTEXT:
- MessagingConfiguration.UseDeadLetterQueue (MessagingConfiguration.cs:170) and DeadLetterOptions (:739) are read
  nowhere except PatternFlags(); AddEncinaDeadLetterQueue<TStore, TFactory> is called by no production code.
- DeadLetterServiceCollectionExtensions registers the health check with TryAddScoped<IEncinaHealthCheck>, which
  is skipped when any other IEncinaHealthCheck exists (AddEncinaADO adds SqlServerHealthCheck).
- Provider packages register the other patterns through AddMessagingServices / AddOutboxInboxSagaSchedulingServices
  (MessagingServiceCollectionExtensions.cs:44, :203). MongoDB uses EncinaMongoDbOptions, not MessagingConfiguration.

TASK:
1. Add public AddDeadLetterQueueServices<TStore, TFactory>(bool useDeadLetterQueue, DeadLetterOptions options) in
   MessagingServiceCollectionExtensions; make both AddEncinaDeadLetterQueue overloads delegate to it.
2. Health check through TryAddEnumerable; DeadLetterHealthCheckOptions through TryAddSingleton.
3. Call the helper from AddEncinaADO (x3), AddEncinaADOWithTenancy (x3, with a DI test), AddEncinaDapper (x3), AddEncinaEntityFrameworkCore and AddEncinaMongoDB;
   add UseDeadLetterQueue, DeadLetterOptions and Collections.DeadLetterMessages to the MongoDB options.
4. Setter validation on DeadLetterOptions, including the new ReplayClaimTimeout (TimeSpan, default 5 minutes, > 0).

KEY RULES:
- Registration completeness (AGENTS.md section 3): every option type and dependency the store, orchestrator,
  manager, health check and cleanup processor resolve is registered; prove it with ValidateOnBuild and
  ValidateScopes.
- TryAdd for store and factory, so an application's own IDeadLetterStore (or the testing fake) is never replaced.
- Feature stays opt-in: nothing is registered when UseDeadLetterQueue is false.

REFERENCE FILES:
- src/Encina.Messaging/DeadLetter/DeadLetterServiceCollectionExtensions.cs
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs
- src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs
- src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs
- src/Encina.MongoDB/ServiceCollectionExtensions.cs, src/Encina.MongoDB/EncinaMongoDbOptions.cs
```

</details>

---

### Phase 4: Persistence Entities, Factories & Provider Scripts

> **Goal**: One table shape on the 9 relational providers (scripts, EF configuration) and the MongoDB collection with its indexes.

<details>
<summary><strong>Tasks</strong></summary>

1. **Entity and factory per provider package** (`DeadLetter/DeadLetterMessage.cs`, `DeadLetter/DeadLetterMessageFactory.cs`) in the 8 packages:
   - the entity is a `sealed class` implementing `IDeadLetterMessage` with settable properties and no clock read;
   - the factory copies `DeadLetterData` field by field (never serializes; `IDeadLetterMessageFactory.cs:54-60`).
   - MongoDB's entity carries `[BsonId]`/`[BsonElement]` attributes like `src/Encina.MongoDB/Scheduling/ScheduledMessage.cs`, with **camelCase element names** (`requestType`, `scheduledAtUtc` are the existing messaging style); only the collection name `dead_letter_messages` is snake_case.
2. **Scripts** `Scripts/029_CreateDeadLetterMessagesTable.sql` in the six ADO.NET and Dapper packages, also appended to each `000_CreateAllTables.sql`. Columns, lengths from `DeadLetterStoreLimits`:
   - `Id` (PK), `RequestType`, `RequestContent`, `ErrorCode`, `ExceptionType` (null), `ExceptionStackTrace` (null);
   - `CorrelationId` (null), `SourcePattern`, `SourceMessageId`, `TenantId` (null), `TotalRetryAttempts`;
   - `FirstFailedAtUtc`, `DeadLetteredAtUtc`, `ExpiresAtUtc` (null), `ReplayClaimedAtUtc` (null, B3), `ReplayedAtUtc` (null), `ReplayResult` (null, an outcome code, max 256).
   - No `ModuleId` (deferred to #747, B6) and no `ActorId` / `CausationId` (#2013, B8).
   - Types per dialect in the Research table "Provider SQL Matrix". On PostgreSQL every `*AtUtc` column is `TIMESTAMPTZ` (B4), as in `028_CreateOperationAuditEntriesTable.sql:13-15`.
3. **Indexes** (all relational providers and MongoDB):
   - `UX_DeadLetterMessages_Source` unique on `(SourcePattern, SourceMessageId)`;
   - `IX_DeadLetterMessages_DeadLetteredAt` on `(DeadLetteredAtUtc, Id)` (paging order);
   - `IX_DeadLetterMessages_Pending` on `(ReplayedAtUtc, SourcePattern, DeadLetteredAtUtc)` (health check, statistics, `FromSource`);
   - `IX_DeadLetterMessages_ExpiresAt` on `(ExpiresAtUtc)` (cleanup);
   - `IX_DeadLetterMessages_CorrelationId` on `(CorrelationId)`;
   - `IX_DeadLetterMessages_Tenant` on `(TenantId, DeadLetteredAtUtc)`.
   - No filtered indexes in the EF configuration: `HasFilter` takes dialect-specific SQL, and one EF configuration serves three databases (the existing `HasFilter("IsRecurring = 1")` in `src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessageConfiguration.cs:73` is SQL Server syntax; the other `HasFilter` is `:68`).
4. **EF Core**: `src/Encina.EntityFrameworkCore/DeadLetter/DeadLetterMessageConfiguration.cs` (`IEntityTypeConfiguration<DeadLetterMessage>`, `ToTable("DeadLetterMessages")`, lengths, required flags, the six indexes, `Ignore(x => x.IsReplayed)`). Applications apply it as they apply `ScheduledMessageConfiguration` (`src/Encina.EntityFrameworkCore/README.md:49-52`).
5. **MongoDB indexes**: `MongoDbIndexCreator` gains `(_options.UseDeadLetterQueue, CreateDeadLetterIndexesAsync)` in its table (`src/Encina.MongoDB/MongoDbIndexCreator.cs:61-66`). The unique index uses a plain unique key, because `SourceMessageId` is never null.
6. **Integration test schemas**: `CreateDeadLetterSchemaAsync` in `tests/Encina.TestInfrastructure/Schemas/{SqlServer,PostgreSql,MySql}Schema.cs`, called by the three fixtures next to `CreateSchedulingSchemaAsync` (`tests/Encina.TestInfrastructure/Fixtures/SqlServerFixture.cs:45`), and `DeadLetterMessages` in each `ClearAllDataAsync`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #583 in Encina: entities, factories, SQL scripts, EF configuration and
MongoDB indexes for the dead letter queue.

CONTEXT:
- Phase 1 fixed the record: Id, RequestType, RequestContent, ErrorCode, ExceptionType, ExceptionStackTrace,
  CorrelationId, SourcePattern, SourceMessageId, TenantId, TotalRetryAttempts, FirstFailedAtUtc,
  DeadLetteredAtUtc, ExpiresAtUtc, ReplayClaimedAtUtc, ReplayedAtUtc, ReplayResult (outcome code, 256); lengths in
  DeadLetterStoreLimits. No ModuleId, ActorId or CausationId.
- Each provider package has Scripts/000..028; the newest PostgreSQL scripts quote PascalCase identifiers
  (028_CreateOperationAuditEntriesTable.sql); EF configurations are applied by the application.

TASK:
1. DeadLetterMessage + DeadLetterMessageFactory in the 8 provider packages (DeadLetter/ folder).
2. 029_CreateDeadLetterMessagesTable.sql in the 6 ADO.NET/Dapper packages, appended to 000_CreateAllTables.sql,
   with the six indexes of the plan (unique source key, paging, pending, expiry, correlation, tenant).
3. DeadLetterMessageConfiguration in Encina.EntityFrameworkCore (no HasFilter).
4. CreateDeadLetterIndexesAsync in MongoDbIndexCreator, gated by UseDeadLetterQueue.
5. CreateDeadLetterSchemaAsync in the three test schema classes, called by the fixtures, cleared by
   ClearAllDataAsync.

KEY RULES:
- SQL Server: NVARCHAR, DATETIME2(7), UNIQUEIDENTIFIER. PostgreSQL: quoted PascalCase, VARCHAR/TEXT, TIMESTAMPTZ for every
  *AtUtc column, UUID. MySQL: backticks, VARCHAR/LONGTEXT, DATETIME(6), CHAR(36), InnoDB utf8mb4.
- Lengths only from DeadLetterStoreLimits; RequestType is 1000 (assembly-qualified names of generic types are long).
- No ExceptionMessage column anywhere.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/Scripts/028_CreateOperationAuditEntriesTable.sql
- src/Encina.ADO.PostgreSQL/Scripts/028_CreateOperationAuditEntriesTable.sql
- src/Encina.ADO.MySQL/Scripts/004_CreateScheduledMessagesTable.sql
- src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessageConfiguration.cs
- src/Encina.MongoDB/MongoDbIndexCreator.cs
- tests/Encina.TestInfrastructure/Schemas/SqlServerSchema.cs
```

</details>

---

### Phase 5: ADO.NET Providers (SqlServer, PostgreSQL, MySQL)

> **Goal**: `DeadLetterStoreADO` on the three ADO.NET packages.

<details>
<summary><strong>Tasks</strong></summary>

For each of `src/Encina.ADO.{SqlServer,PostgreSQL,MySQL}/DeadLetter/`:

1. **`DeadLetterStoreADO : IDeadLetterStore`** (`public sealed class`). Constructor:
   - `(IDbConnection connection, string tableName = "DeadLetterMessages", TimeProvider? timeProvider = null)`;
   - after #718 lands, also its transaction accessor, as the other ADO stores take it then;
   - table name through `SqlIdentifierValidator.ValidateTableName`, like `ScheduledMessageStoreADO.cs:34`.
2. Each method inside `EitherHelpers.TryAsync(..., "dlq.<operation>_failed")`, async command execution with the `CancellationToken`, real `DbConnection.OpenAsync(ct)` when closed. Never copy the no-op `OpenConnectionAsync` (`ScheduledMessageStoreADO.cs:286-290`, #1170).
3. SQL per dialect (Research, "Provider SQL Matrix"):
   - insert-if-absent returning rows affected (1 → `true`, 0 → `false`), with the unique-violation number (SQL Server 2627/2601, MySQL 1062) or SQLSTATE `23505` (PostgreSQL) mapped to `false`;
   - paging (`OFFSET/FETCH` on SQL Server, `LIMIT/OFFSET` on the other two);
   - atomic replay claim (`ReplayClaimedAtUtc`, conditional on `ReplayedAtUtc IS NULL` and no live claim) and conditional replay update (outcome code only);
   - set-based `DELETE` for `DeleteManyAsync` and `DeleteExpiredAsync`.
4. One private `BuildWhere(DeadLetterFilter?, IDbCommand)` per store, parameterized. `ExcludeReplayed` maps to `ReplayedAtUtc IS NULL` / `IS NOT NULL`; `ExpiresAtOrBeforeUtc` to `ExpiresAtUtc IS NOT NULL AND ExpiresAtUtc <= @p`.
5. `SaveChangesAsync` returns `Right(Unit)` (statements run immediately), as `ScheduledMessageStoreADO.cs:243-247`.
6. `PublicAPI.Unshipped.txt` of each package.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #583 in Encina: DeadLetterStoreADO for SqlServer, PostgreSQL and MySQL.

CONTEXT:
- Phases 1-4 defined the contract (ordered paging, AddAsync -> bool with unique (SourcePattern, SourceMessageId),
  conditional MarkAsReplayedAsync -> bool, DeleteManyAsync, DeleteExpiredAsync with the store's TimeProvider), the
  entity, factory and the 029 scripts.
- Existing ADO stores build SQL text per dialect and wrap calls in EitherHelpers.TryAsync; their
  OpenConnectionAsync is a no-op (#1170). #718 may have introduced IDbTransactionAccessor; if so, enlist every
  command as its plan says.

TASK:
Implement DeadLetterStoreADO in the three ADO packages with the dialect SQL of the plan's Provider SQL Matrix,
parameterized filters, real OpenAsync, async execution with the CancellationToken, and PublicAPI entries.

KEY RULES:
- Async only (OpenAsync, ExecuteNonQueryAsync, ExecuteReaderAsync, ReadAsync with the token); never Open() or a
  synchronous Execute*.
- Parameters only; identifiers through SqlIdentifierValidator.
- now = TimeProvider.GetUtcNow().UtcDateTime; never DateTime.UtcNow.
- Arguments validated before TryAsync (skip >= 0, 1 <= take <= DeadLetterStoreLimits.MaxPageSize,
  messageId != Guid.Empty).
- All three providers in the same change; CRAP <= 10 per method (one helper for the WHERE clause).

REFERENCE FILES:
- src/Encina.ADO.SqlServer/Scheduling/ScheduledMessageStoreADO.cs
- src/Encina.ADO.PostgreSQL/Scheduling/ScheduledMessageStoreADO.cs
- src/Encina.ADO.MySQL/Scheduling/ScheduledMessageStoreADO.cs
- docs/plans/outbox-atomicity-ado-implementation-plan-718.md (enlistment, if landed)
```

</details>

---

### Phase 6: Dapper Providers (SqlServer, PostgreSQL, MySQL)

> **Goal**: `DeadLetterStoreDapper` on the three Dapper packages.

<details>
<summary><strong>Tasks</strong></summary>

For each of `src/Encina.Dapper.{SqlServer,PostgreSQL,MySQL}/DeadLetter/`:

1. **`DeadLetterStoreDapper : IDeadLetterStore`**, same constructor shape and SQL as Phase 5. Calls go through `CommandDefinition(sql, parameters, transaction, cancellationToken: ct)`. The existing Dapper stores call `ExecuteAsync(sql, message)` without the token (`ScheduledMessageStoreDapper.cs:54`, #1418); the new store does not copy that.
2. Row mapping with `QueryAsync<DeadLetterMessage>`. On MySQL the `CHAR(36)` id needs the package's existing GUID type handler (`TypeHandlers/`).
3. Unique-violation mapping as Phase 5.
4. `PublicAPI.Unshipped.txt` of each package.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of issue #583 in Encina: DeadLetterStoreDapper for SqlServer, PostgreSQL and MySQL.

CONTEXT:
- Phase 5 implemented the same contract with ADO.NET; reuse its SQL text per dialect.
- Existing Dapper stores (src/Encina.Dapper.PostgreSQL/Scheduling/ScheduledMessageStoreDapper.cs) call Dapper
  without a CancellationToken; #1418 tracks that defect. Each Dapper package has TypeHandlers/ for GUIDs.

TASK:
Implement DeadLetterStoreDapper in the three Dapper packages with CommandDefinition (transaction and
cancellationToken), QueryAsync<DeadLetterMessage> mapping, unique-violation -> false, and PublicAPI entries.

KEY RULES:
- Every Dapper call passes the CancellationToken through CommandDefinition.
- Parameters only; identifiers through SqlIdentifierValidator; TimeProvider for "now".
- Same argument validation and error codes as DeadLetterStoreADO.
- Include the atomic replay claim (ReplayClaimedAtUtc, conditional update, claim expiry from
  DeadLetterOptions.ReplayClaimTimeout) with the same SQL as the ADO store.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/DeadLetter/DeadLetterStoreADO.cs (Phase 5)
- src/Encina.Dapper.PostgreSQL/Scheduling/ScheduledMessageStoreDapper.cs
- src/Encina.Dapper.MySQL/TypeHandlers/
```

</details>

---

### Phase 7: EF Core Provider (SqlServer, PostgreSQL, MySQL)

> **Goal**: `DeadLetterStoreEF` on the shared `DbContext`, verified on the three databases.

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.EntityFrameworkCore/DeadLetter/DeadLetterStoreEF.cs`**: `(DbContext dbContext, TimeProvider? timeProvider = null)`, like `ScheduledMessageStoreEF.cs:27-32`.
   - `AddAsync`: a foreign record type returns `Left(dlq.invalid_message_type)` (pattern of `ScheduledMessageStoreEF.cs:38-42`). Otherwise `AnyAsync` on the source key: `false` when found, else `Set<DeadLetterMessage>().AddAsync` and `true`.
   - Reads use `AsNoTracking()`, the filter as an `IQueryable` extension, `OrderBy(DeadLetteredAtUtc).ThenBy(Id)` (or descending), `Skip/Take`.
   - `MarkAsReplayedAsync`: `Where(Id == id && ReplayedAtUtc == null).ExecuteUpdateAsync(...)`, rows > 0. The atomic replay claim (B3) is the same shape on `ReplayClaimedAtUtc` (`ExecuteUpdateAsync`, rows > 0 wins the claim).
   - `DeleteManyAsync` and `DeleteExpiredAsync`: `ExecuteDeleteAsync`.
   - `SaveChangesAsync`: `DbContext.SaveChangesAsync(ct)`. A `DbUpdateException` caused by the unique index (a race between two hosts) becomes `Left(dlq.store_failed)` through `EitherHelpers.TryAsync`.
2. XML remarks on the class: the store shares the scoped `DbContext`, so `SaveChangesAsync` also saves any other tracked change in that context. A caller in a failure path must use a new scope; this is the rule #1991 must follow (Cross-Cutting Integration, Transactions).
3. Test DbContexts: add `DbSet<DeadLetterMessage>` and `ApplyConfiguration(new DeadLetterMessageConfiguration())` to `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/TestEFDbContext.cs` and the PostgreSQL/MySQL test contexts.
4. `PublicAPI.Unshipped.txt` of `Encina.EntityFrameworkCore`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of issue #583 in Encina: DeadLetterStoreEF.

CONTEXT:
- EF Core stores take the scoped DbContext and a TimeProvider (src/Encina.EntityFrameworkCore/Scheduling/
  ScheduledMessageStoreEF.cs); AddAsync tracks, SaveChangesAsync commits.
- Phase 4 added DeadLetterMessageConfiguration (table DeadLetterMessages, unique (SourcePattern, SourceMessageId)).

TASK:
Implement DeadLetterStoreEF: type check, AnyAsync duplicate check before tracking, AsNoTracking ordered paging,
ExecuteUpdateAsync for the conditional replay mark, ExecuteDeleteAsync for DeleteManyAsync/DeleteExpiredAsync,
SaveChangesAsync through EitherHelpers.TryAsync; register the entity in the three integration test DbContexts;
document the shared-DbContext rule for failure-path callers.

KEY RULES:
- No raw SQL and no HasFilter: one implementation serves SQL Server, PostgreSQL (Npgsql) and MySQL (Pomelo).
- Every EF call takes the CancellationToken; TimeProvider for "now".
- Verify ExecuteUpdateAsync/ExecuteDeleteAsync translation on Pomelo for .NET 10 (plan, Open Questions).

REFERENCE FILES:
- src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessageStoreEF.cs
- src/Encina.EntityFrameworkCore/Outbox/OutboxStoreEF.cs
- tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/TestEFDbContext.cs
```

</details>

---

### Phase 8: MongoDB Provider

> **Goal**: `DeadLetterStoreMongoDB` with the same contract.

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.MongoDB/DeadLetter/DeadLetterStoreMongoDB.cs`**: `(IMongoClient, IOptions<EncinaMongoDbOptions>, ILogger<DeadLetterStoreMongoDB>, TimeProvider? = null)`, like `ScheduledMessageStoreMongoDB.cs:25-40`. Collection `Collections.DeadLetterMessages`.
   - `AddAsync`: `InsertOneAsync`; a `MongoWriteException` with `ServerErrorCategory.DuplicateKey` → `false`; log `AddedDeadLetterMessage` (Debug).
   - Reads: a filter builder from `DeadLetterFilter`, `Sort` ascending (or descending) on `DeadLetteredAtUtc` then `_id`, `Skip/Limit`.
   - `MarkAsReplayedAsync`: `UpdateOneAsync` with `ReplayedAtUtc == null` in the filter; `ModifiedCount > 0`. The atomic replay claim (B3) is an `UpdateOneAsync` (or `FindOneAndUpdateAsync`) conditional on `replayedAtUtc == null` and no live `replayClaimedAtUtc`.
   - Documents use camelCase element names (`requestType`, `deadLetteredAtUtc`), as the existing messaging documents do.
   - `DeleteManyAsync` / `DeleteExpiredAsync`: `DeleteManyAsync`, `DeletedCount`.
   - `SaveChangesAsync`: `Right(Unit)`.
2. `Log.cs`: `AddedDeadLetterMessage` (3165) and `CreatedDeadLetterIndexes` (3166); see Phase 10 (3163-3164 stay reserved for #718, B1).
3. `PublicAPI.Unshipped.txt` of `Encina.MongoDB`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 of issue #583 in Encina: DeadLetterStoreMongoDB.

CONTEXT:
- MongoDB stores resolve their collection from IOptions<EncinaMongoDbOptions>.Collections and log through
  src/Encina.MongoDB/Log.cs (EventIdRanges.MongoDB 3100-3199, highest used 3162).
- Phase 4 added Collections.DeadLetterMessages and CreateDeadLetterIndexesAsync (unique source key and the other
  five indexes).

TASK:
Implement DeadLetterStoreMongoDB: InsertOne with duplicate-key -> false, ordered filtered paging, conditional
UpdateOne for the replay mark and the atomic replay claim, DeleteMany for the bulk and expiry deletes; camelCase element names;
add EventIds 3165-3166 to Log.cs (3163-3164 are reserved by #718).

KEY RULES:
- Every driver call takes the CancellationToken; TimeProvider for "now".
- No TTL index (Design Choice 8): expiry is DeleteExpiredAsync, driven by DeadLetterCleanupProcessor.
- Log only ids, source pattern and counts; never payloads or EncinaError.Message.

REFERENCE FILES:
- src/Encina.MongoDB/Scheduling/ScheduledMessageStoreMongoDB.cs
- src/Encina.MongoDB/MongoDbIndexCreator.cs
- src/Encina.MongoDB/Log.cs
```

</details>

---

### Phase 9: Cross-Cutting Integration

> **Goal**: The functions marked ✅ in the matrix are wired; the deferred ones are recorded.

<details>
<summary><strong>Tasks</strong></summary>

1. **Multi-tenancy (✅)**: `TenantId` stamped by the orchestrator (Phase 2), filterable (Phase 1), indexed (Phase 4), and never a telemetry dimension (REQ-062; Phase 10). The manager defaults to the ambient tenant with an explicit all-tenants option (B7). Replay restoring the persisted tenant is #2013.
2. **Transactions (✅)**:
   - each store write is one statement (atomic);
   - the ADO.NET and Dapper stores enlist through #718's `IDbTransactionAccessor` when it exists at implementation time, otherwise they use the connection as the other stores do;
   - the EF store documents the shared-`DbContext` rule (Phase 7). `DeadLetterOrchestrator` remarks state it for #1991.
3. **Idempotency (✅)**: unique source key, `AddAsync -> bool`, `OnDeadLetter` once per source message (Phases 1, 2, 4).
4. **Validation (✅)**: store argument rules, `DeadLetterOptions` setter validation, `SourcePattern` and `SourceMessageId` length checks in the orchestrator (`ArgumentException` before any I/O).
5. **Health checks (✅)**: `DeadLetterHealthCheck` fails closed and registers through `TryAddEnumerable` (#2011, which lands first; Phases 2, 3). The provider database health checks already cover connectivity.
6. **Deferred (⏭️)**: no new issue files are written (B2). Replay tenant context and operator-action audit are #2013; replay exclusivity is implemented here with #2012; module isolation is #747 (comment already posted).
7. Record the ADR-018 evaluation of the 12 functions in the PR (this plan's matrix).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
You are implementing Phase 9 of issue #583 in Encina: cross-cutting integration of the persistent dead letter
queue.

CONTEXT:
- Phases 1-8 delivered the contract, the core components and the 10 stores.
- The plan's Cross-Cutting Integration Matrix marks Observability, Logging, Health, Validation, Transactions,
  Idempotency, Distributed Locks (the replay claim) and Multi-Tenancy as included; Module Isolation (#747) and Audit
  Trail (#2013) as deferred.

TASK:
Verify each included function is wired as the matrix says (tenant stamping and filter, one-statement writes and
#718 enlistment if present, unique source key, atomic replay claim, argument and options validation, fail-closed
health check from #2011); put the matrix in the PR description. Do not write new issue files: #2011, #2012, #2013
and #747 already exist.

KEY RULES:
- Compliance and health gates fail closed; errors are never swallowed.
- Never add a placeholder tenant to telemetry when the row has none.
- Workers never open issues.

REFERENCE FILES:
- docs/plans/dead-letter-stores-implementation-plan-583.md (matrix and Next Steps)
- docs/plans/outbox-atomicity-ado-implementation-plan-718.md
- .github/ISSUE_TEMPLATE/
```

</details>

---

### Phase 10: Observability

> **Goal**: Tracing, metrics and logs for the DLQ without payloads, error messages or invented tenants.

<details>
<summary><strong>Tasks</strong></summary>

1. **Tracing**: `src/Encina.OpenTelemetry/MessagingStores/InstrumentedDeadLetterStore.cs`, an `internal sealed` decorator with its own `ActivitySource("Encina.Messaging.DeadLetter", "1.0")`, registered with `DecorateService<IDeadLetterStore>` next to the scheduled-message decorator (`src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs:150`).
   - Activities `encina.dlq.add`, `.query`, `.count`, `.replay_mark`, `.delete`, `.delete_many`, `.delete_expired`.
   - Tags `dlq.source_pattern`, `dlq.message_id` (a generated GUID, not a subject identifier), `dlq.count`, and `encina.error_code` on failure.
   - It must not copy `Failed(activity, err.Message)` from the existing decorators (`InstrumentedScheduledMessageStore.cs:45`, tracked by #1788). The source lives in the decorator that uses it, so #1790's dead-source problem does not repeat.
   - Add `"Encina.Messaging.DeadLetter"` to the sources `WithEncina` subscribes to (`tracing.AddSource(...)`, the messaging sources at `src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs:202-205`; `:179-185` are the core and sharding sources).
2. **Metrics**: new `src/Encina.Messaging/Diagnostics/DeadLetterMetrics.cs` on `Meter("Encina", "1.0")` (as `MessagingStoreMetrics.cs:17`), used by the orchestrator and manager:
   - `encina.dlq.messages_added_total` (tag `source_pattern`; no tenant id, SPEC-002 REQ-062);
   - `encina.dlq.duplicates_ignored_total` (`source_pattern`);
   - `encina.dlq.messages_replayed_total` (`outcome` = succeeded/failed);
   - `encina.dlq.messages_deleted_total` (`reason` = manual/expired);
   - `encina.dlq.store_failures_total` (`operation`, `error_code`).
3. **Logs** (`[LoggerMessage]`, `EventIdRanges.Messaging` 2800-2999, appended to `src/Encina.Messaging/DeadLetter/DeadLetterLog.cs`):
   - 2990 `DeadLetterStoreWriteFailed` (Warning: source pattern, error code);
   - 2991 `DeadLetterDuplicateIgnored` (Debug: source pattern, existing dead letter id);
   - 2992 `ReplayOutcomeNotRecorded` (Warning: message id, error code).
   - MongoDB (`EventIdRanges.MongoDB` 3100-3199, `src/Encina.MongoDB/Log.cs`): 3165 `AddedDeadLetterMessage` (Debug: id, source pattern); 3166 `CreatedDeadLetterIndexes` (Debug). 3163-3164 stay reserved for #718's `TransactionsUnavailable` and `TransientTransactionLabel` (`outbox-atomicity-ado-implementation-plan-718.md:837,863,1072`), which reserved first (B1).
   - No new range: `Messaging` and `MongoDB` are already mapped in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`. Re-check that 2990-2992 and 3165-3166 are free when work starts (2989 is taken by #1970; the highest MongoDB id in use is 3162).
4. XML docs of each log method name the range (`/// Event IDs: 2990-2992 (see EventIdRanges.Messaging)`).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 10</strong></summary>

```
You are implementing Phase 10 of issue #583 in Encina: observability of the persistent dead letter queue.

CONTEXT:
- Existing store decorators live in src/Encina.OpenTelemetry/MessagingStores/ and are registered with
  DecorateService<T> (ServiceCollectionExtensions.cs:150); they wrongly put EncinaError.Message on activities (#1788).
- Messaging metrics share Meter("Encina", "1.0") (src/Encina.Messaging/Diagnostics/MessagingStoreMetrics.cs).
- EventIdRanges.Messaging is 2800-2999: 2945-2957 are DeadLetterLog, 2963-2988 are reserved by #718/#1200/#1251,
  2989 by #1970. EventIdRanges.MongoDB is 3100-3199, highest used 3162.

TASK:
1. InstrumentedDeadLetterStore with ActivitySource "Encina.Messaging.DeadLetter", registered and subscribed.
2. DeadLetterMetrics with the five counters of the plan, called from DeadLetterOrchestrator and DeadLetterManager.
3. [LoggerMessage] 2990-2992 in DeadLetterLog.cs and 3165-3166 in src/Encina.MongoDB/Log.cs.

KEY RULES:
- No payloads, no EncinaError.Message, no exception messages in tags, metrics or logs; error code only.
- No tenant id in tags, metrics or logs (SPEC-002 REQ-062).
- EventIds packed sequentially inside the registered ranges; re-check they are free before writing.

REFERENCE FILES:
- src/Encina.OpenTelemetry/MessagingStores/InstrumentedScheduledMessageStore.cs
- src/Encina.OpenTelemetry/ServiceCollectionExtensions.cs
- src/Encina.Messaging/Diagnostics/MessagingStoreMetrics.cs
- src/Encina.Messaging/DeadLetter/DeadLetterLog.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

### Phase 11: Testing

> **Goal**: Every flag of every touched file reaches its manifest target; the contract holds on the 10 providers against real databases.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit tests** (`tests/Encina.UnitTests/Messaging/DeadLetter/`, existing `DeadLetterOrchestratorTests.cs`, `DeadLetterManagerTests.cs`):
   - tenant and source-id stamping; the duplicate path (no callback, existing row returned);
   - B7 manager tests: the ambient tenant is applied to reads, replays and deletes; `AllTenants = true` reads every tenant; no ambient tenant behaves as before; an explicit `TenantId` wins; `GetStatisticsAsync` follows the same default; the claim expires after `ReplayClaimTimeout` (with `FakeTimeProvider`);
   - statistics from counts (no full read; assert `GetMessagesAsync` is never called with `take > 1`);
   - every store `Left` propagated and logged by code; the health check's `Unhealthy` on `Left`; `IsExpiredAt` boundary with `FakeTimeProvider`;
   - `AddDeadLetterQueueServices` for each flag combination; the fake store's new members;
   - `InstrumentedDeadLetterStore` (activity names, tags, no message in any tag).
2. **Guard tests** (`tests/Encina.GuardTests/`):
   - null arguments of every public constructor and method of the 10 stores, the 8 factories, the decorator, the manager and the orchestrator;
   - `skip < 0`, `take` outside `1..MaxPageSize`, `Guid.Empty`, a blank `replayResult`;
   - the options setters.
3. **Contract tests** (`tests/Encina.ContractTests/Messaging/DeadLetter/DeadLetterStoreContract.cs`): an abstract class run against `FakeDeadLetterStore`. The integration classes reuse it for the 10 stores. It covers:
   - add then get round-trips every field;
   - ascending order, and `newestFirst`;
   - each filter field alone and combined;
   - the duplicate source key returns `false` and keeps one row;
   - `MarkAsReplayedAsync` is true once, then false; the atomic replay claim is won by exactly one of two concurrent callers and can be re-won after the claim expires; `ReplayResult` holds an outcome code of at most 256 characters;
   - `DeleteAllAsync(DeadLetterFilter.All)` deletes the whole queue in one statement (B9);
   - on PostgreSQL, `ExpiresAtUtc == now` is covered with a non-UTC session time zone (B4);
   - `DeleteManyAsync` and `DeleteExpiredAsync` remove exactly the matching rows, with the `ExpiresAtUtc == now` boundary;
   - `GetCountAsync` equals the number of rows read by paging `GetMessagesAsync` (pages of at most `DeadLetterStoreLimits.MaxPageSize`) until a short page; the data set spans more than one page.
4. **Property tests** (`tests/Encina.PropertyTests/Messaging/DeadLetter/DeadLetterStorePropertyTests.cs`), FsCheck over random sequences of add, replay mark, delete and clock advance on the fake:
   - pages are sorted and disjoint, and their union equals the filtered count;
   - expired deletion never removes a row with `ExpiresAtUtc > now` or `null`;
   - adds with the same source key are idempotent.
5. **Integration tests** (real databases, shared collections, `[Trait("Category", "Integration")]`):
   - `tests/Encina.IntegrationTests/ADO/{SqlServer,PostgreSQL,MySQL}/DeadLetter/DeadLetterStoreADOTests.cs` (`[Collection("ADO-<Db>")]`);
   - `Dapper/{...}/DeadLetter/DeadLetterStoreDapperTests.cs` (`[Collection("Dapper-<Db>")]`);
   - `Infrastructure/EntityFrameworkCore/{SqlServer,PostgreSQL,MySQL}/DeadLetter/DeadLetterStoreEF{Db}Tests.cs` (`EFCore-<Db>`);
   - `Infrastructure/MongoDB/Stores/DeadLetterStoreMongoDBIntegrationTests.cs` (`[Collection(MongoDbCollection.Name)]`).
   - Each derives from the contract base, calls `_fixture.ClearAllDataAsync()` in `InitializeAsync` (pattern `tests/Encina.IntegrationTests/ADO/SqlServer/Scheduling/ScheduledMessageStoreADOTests.cs:14-28`), and adds two tests: a concurrent duplicate insert (two connections, one row survives) and a two-tenant filter test.
   - Per family, one end-to-end test: register the provider with `UseDeadLetterQueue = true`, `DeadLetterOrchestrator.AddAsync`, then `IDeadLetterManager.ReplayAsync` through a test handler, then `CleanupExpiredAsync` after advancing a `FakeTimeProvider`.
   - Extend `tests/Encina.IntegrationTests/SchemaScripts/{PostgreSql,MySql}SchemaScriptsIntegrationTests.cs` to run `029_*` and check the six indexes.
6. **DI tests**: `ValidateOnBuild` + `ValidateScopes` per family and MongoDB, plus the `AddEncinaADOWithTenancy` path of each ADO package (B5). Registration order: the application's own store (or the fake) is kept when registered before and after the provider, under the order rules of B10 (`TryAdd`: the first registration wins; a plain `AddScoped`: the last; tests use `ReplaceWithFakes()` when the provider was registered first).
7. **Telemetry test** (in-memory exporter): one add, replay and cleanup emit the activities and counters, and no tag, metric dimension or log carries a payload, `EncinaError.Message` or exception message.
8. **Load tests**: justification file `tests/Encina.LoadTests/Messaging/DeadLetter/DeadLetterStore.md`. The DLQ is written only on terminal failure and read by operators; concurrency safety is covered by the duplicate-insert integration test and the idempotency property.
9. **Benchmarks**: justification file `tests/Encina.BenchmarkTests/Encina.Benchmarks/Messaging/DeadLetter/DeadLetterStore.md` (not a hot path).
10. **Coverage manifests**: per-file `targets` and one-sentence `justifications` for every new or touched `src/` file. Indicative targets:
    - stores: unit 0 (justified: real SQL is exercised by integration), guard 90, integration 85;
    - factories and entities: unit 90;
    - orchestrator and manager: unit 90, guard 90;
    - health check: unit 95;
    - decorator: unit 90.
    - Files: `.github/coverage-manifest/Encina.Messaging.json`, `Encina.ADO.{SqlServer,PostgreSQL,MySQL}.json`, `Encina.Dapper.{SqlServer,PostgreSQL,MySQL}.json`, `Encina.EntityFrameworkCore.json`, `Encina.MongoDB.json`, `Encina.OpenTelemetry.json`, `Encina.Testing.Fakes.json`.
    - Then run `coverage-report.cs --check-justifications` and the local CRAP table.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 11</strong></summary>

```
You are implementing Phase 11 of issue #583 in Encina: tests for the persistent dead letter stores.

CONTEXT:
- Phases 1-10 delivered the reshaped contract, the fake, 10 stores, registration, decorator, metrics and logs.
- Integration fixtures are shared collections: ADO-/Dapper- x SqlServer/PostgreSQL/MySQL, EFCore-<Db>,
  MongoDbCollection.Name; Phase 4 added DeadLetterMessages to the test schemas and ClearAllDataAsync.

TASK:
Write the unit, guard, contract (abstract base + fake), property and integration tests listed in the Phase 11
Tasks (10 providers, duplicate race, two-tenant filter, one end-to-end test per family), the DI and telemetry tests,
the load and benchmark justification files, and the per-file coverage targets with justifications; measure each
flag and the CRAP of changed methods.

KEY RULES:
- Tests execute real code; no reflection-only tests. Shouldly through Encina.Testing.Shouldly; FakeTimeProvider;
  no Thread.Sleep, no wall-clock dates.
- Integration tests use [Collection("<Family>-<Db>")] shared fixtures, never IClassFixture or new fixtures,
  never dispose the fixture, and call ClearAllDataAsync in InitializeAsync.
- Integration tests of a database feature are mandatory on all 10 providers; no .md justification for them.
- Every applicable flag reaches its target; CRAP <= 10 on every changed method.

REFERENCE FILES:
- tests/Encina.IntegrationTests/ADO/SqlServer/Scheduling/ScheduledMessageStoreADOTests.cs
- tests/Encina.IntegrationTests/Infrastructure/MongoDB/Stores/ScheduledMessageStoreMongoDBIntegrationTests.cs
- tests/Encina.UnitTests/Messaging/DeadLetter/DeadLetterOrchestratorTests.cs
- tests/Encina.PropertyTests/Messaging/Outbox/OutboxExhaustionPropertyTests.cs
- docs/testing/coverage-measurement-methodology.md
```

</details>

---

### Phase 12: Documentation & Finalization

> **Goal**: Documentation that matches the code, the ADR, the changelog fragment, zero warnings, all flags green.

<details>
<summary><strong>Tasks</strong></summary>

1. **XML documentation** on every new public member: the store contract (order, predicates, return values), the entities, factories, the helper with an `<example>`, and the shared-`DbContext` remark on `DeadLetterStoreEF`.
2. **`changelog.d/583-persistent-dead-letter-stores.added.md`**: the persistent DLQ on the 10 providers, enabled with `UseDeadLetterQueue`. Add a `.changed.md` fragment for the reshaped record and store contract (renamed and removed members, `bool` results), and a `.fixed.md` fragment for the swallowed store errors and the plaintext replay outcome (#2012). The health-check fix has its own fragment in #2011.
3. **ADR-046** (number reserved in `docs/architecture/adr/index.md` by this plan): "Persistent dead letter queue: oldest-first contract, idempotent capture by source message, tenant column". It records Choices 1, 5, 6 and 7 as the model #1991 and #584-#589 implement. Move the row from the reserved table to the ADR table when written.
4. **Feature documentation** (`encina-docs` skill): the provider registration, schema scripts and EF configuration section of `docs/features/dead-letter-queue.md`. If #1990 has created the page, extend it; otherwise create it with that section and leave the rest of #1990's items to #1990. Include a Mermaid diagram of capture, replay and expiry, and the per-provider table.
5. **Package READMEs**: `src/Encina.Messaging/README.md` (registration through `UseDeadLetterQueue`) and the eight provider READMEs (script `029`, EF `ApplyConfiguration`, MongoDB options).
6. **`docs/INVENTORY.md`**: the new `DeadLetter/` folders, scripts and diagnostics files.
7. **`PublicAPI.Unshipped.txt`**: verify the entries of Phases 1-10 (RS0016/RS0017/RS0036/RS0037 clean) in all 11 packages.
8. **`ROADMAP.md` / `docs/releases/`**: the DLQ in the v0.19.0 block (SPEC-000 DEC-008, merged as #2008).
9. **Build verification**: `dotnet build Encina.slnx --configuration Release` with 0 errors and 0 warnings.
10. **Test verification**: `dotnet test` all pass. Every coverage flag (unit, guard, contract, property, integration) reaches its own target in `.github/coverage-manifest/{Package}.json`. Record the per-file measurement, the CRAP table and the ADR-018 matrix in the PR.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 12</strong></summary>

```
You are implementing Phase 12 of issue #583 in Encina: documentation and finalization.

CONTEXT:
- The persistent DLQ is implemented and tested on the 10 providers; the record and store contract changed.
- #1990 owns the general DLQ documentation gaps; this issue owns the provider registration documentation.
- ADR number 046 is reserved for this plan in docs/architecture/adr/index.md.

TASK:
Complete XML docs; add the changelog fragments (added, changed, fixed); write ADR-046 and move its row; write or
extend docs/features/dead-letter-queue.md (provider section, Mermaid diagram) with the encina-docs skill; update
the Messaging and eight provider READMEs and docs/INVENTORY.md; verify PublicAPI files; build with zero
warnings; run every test flag and record per-file coverage and CRAP in the PR.

KEY RULES:
- English only; never edit the [Unreleased] section of CHANGELOG.md.
- No hand-typed coverage figures in docs: covref markers (SPEC-001).
- Every identifier on a docs page exists in src/ at the time of writing.

REFERENCE FILES:
- .claude/skills/encina-docs/SKILL.md
- changelog.d/README.md
- docs/architecture/adr/index.md
- src/Encina.EntityFrameworkCore/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Requirement | Relevance |
|--------|-------------|-----------|
| EIP "Dead Letter Channel" (Hohpe & Woolf) | A message that cannot be delivered is moved to a dedicated channel for inspection | The pattern `IDeadLetterStore` persists |
| SPEC-000 §2 (Multi-tenancy) and DEC-003 | Persisted shapes carry the tenant before 1.0; database features on the 10 providers | Choice 7; provider matrix |
| SPEC-000 DEC-008 (merged as #2008) | The DLQ enters 1.0 complete on the 10 providers | Scope of this plan |
| SPEC-002 REQ-017 | A dead-lettered outbox message stays in the outbox table and can be requeued | Why capture must be idempotent (Choice 6) |
| SPEC-002 REQ-061, REQ-062 | Tenant-aware and instrumented; no payloads or direct identifiers in telemetry | Choice 7; Phase 10 |
| GDPR Art. 5(1)(e) | Storage limitation | `RetentionPeriod` (7 days) and `DeleteExpiredAsync` on every provider |
| ADR-009, ADR-024 | Oracle and SQLite out of the matrix | 10 providers, not 13 |
| ADR-018, ADR-021 | Cross-cutting integration; EventId ranges | Matrix below; EventIds 2990-2992, 3165-3166 |

### Provider SQL Matrix

| Provider | Types | Insert-if-absent | Paging | Conditional replay mark |
|----------|-------|------------------|--------|-------------------------|
| SQL Server (ADO, Dapper) | `UNIQUEIDENTIFIER`, `NVARCHAR(n)`/`NVARCHAR(MAX)`, `DATETIME2(7)`, `INT`; replay claim: `UPDATE t SET ReplayClaimedAtUtc = @Now WHERE Id = @Id AND ReplayedAtUtc IS NULL AND (ReplayClaimedAtUtc IS NULL OR ReplayClaimedAtUtc <= @ClaimExpiredBefore)`, rows 1 = claimed; `@ClaimExpiredBefore` = now minus `DeadLetterOptions.ReplayClaimTimeout` (same shape on the other providers) | `INSERT INTO t (...) SELECT @Id, ... WHERE NOT EXISTS (SELECT 1 FROM t WITH (UPDLOCK, HOLDLOCK) WHERE SourcePattern = @SourcePattern AND SourceMessageId = @SourceMessageId)`; error 2627/2601 → `false` | `ORDER BY DeadLetteredAtUtc, Id OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY` | `UPDATE t SET ReplayedAtUtc = @Now, ReplayResult = @Result WHERE Id = @Id AND ReplayedAtUtc IS NULL` |
| PostgreSQL (ADO, Dapper) | `UUID`, `VARCHAR(n)`/`TEXT`, `TIMESTAMPTZ` for every `*AtUtc` column (B4), `INTEGER`; quoted PascalCase (Choice 9) | `INSERT ... ON CONFLICT ("SourcePattern", "SourceMessageId") DO NOTHING`; SQLSTATE `23505` → `false` | `ORDER BY "DeadLetteredAtUtc", "Id" LIMIT @Take OFFSET @Skip` | same `UPDATE`, quoted |
| MySQL (ADO, Dapper) | `CHAR(36)`, `VARCHAR(n)`/`LONGTEXT`, `DATETIME(6)`, `INT`; backticks | `INSERT INTO t (...) SELECT ... FROM DUAL WHERE NOT EXISTS (...)`; error 1062 → `false` (not `INSERT IGNORE`, which hides other errors, nor `ON DUPLICATE KEY UPDATE`, whose affected-rows count depends on `UseAffectedRows`) | `ORDER BY DeadLetteredAtUtc, Id LIMIT @Take OFFSET @Skip` | same `UPDATE` |
| EF Core (SQL Server, Npgsql, Pomelo) | from `DeadLetterMessageConfiguration` | `AnyAsync` on the key, then `AddAsync`; race → `DbUpdateException` → `Left(dlq.store_failed)` | `OrderBy/ThenBy/Skip/Take` | `ExecuteUpdateAsync` with `ReplayedAtUtc == null` |
| MongoDB | BSON; `_id` = `Id` | `InsertOneAsync`; `DuplicateKey` → `false` | `Sort` + `Skip` + `Limit` | `UpdateOneAsync` with `ReplayedAtUtc == null` in the filter |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IDeadLetterStore`, `IDeadLetterMessage`, `IDeadLetterMessageFactory` | `src/Encina.Messaging/DeadLetter/` | Reshaped in Phase 1, implemented 10 times |
| `DeadLetterOrchestrator`, `DeadLetterManager`, `DeadLetterCleanupProcessor` | `src/Encina.Messaging/DeadLetter/` | Callers of the store; fixed in Phase 2 |
| `DeadLetterHealthCheck` | `src/Encina.Messaging/Health/DeadLetterHealthCheck.cs` | Fails closed (Phase 2) |
| `AddEncinaDeadLetterQueue<TStore, TFactory>` | `src/Encina.Messaging/DeadLetter/DeadLetterServiceCollectionExtensions.cs:20` | Delegates to the new helper |
| `AddOutboxInboxSagaSchedulingServices` | `src/Encina.Messaging/MessagingServiceCollectionExtensions.cs:203` | Model for the shared helper (#1333) |
| `ScheduledMessageStoreADO` / `Dapper` / `EF` / `MongoDB` | `src/Encina.*/Scheduling/` | Closest store shape (entity + factory + store, `TimeProvider`, `EitherHelpers.TryAsync`) |
| `SqlIdentifierValidator` | used at `ScheduledMessageStoreADO.cs:34` | Table-name validation |
| `MongoDbIndexCreator` | `src/Encina.MongoDB/MongoDbIndexCreator.cs:61-66` | Gated index creation |
| `IRequestContextAccessor` / `IRequestContext.TenantId` | `src/Encina/Abstractions/IRequestContext.cs:125` | Tenant stamping |
| `FakeDeadLetterStore` | `src/Encina.Testing.Fakes/Stores/FakeDeadLetterStore.cs` | Contract and property tests |
| Instrumented store decorators | `src/Encina.OpenTelemetry/MessagingStores/` | Model for `InstrumentedDeadLetterStore` (minus the `err.Message` defect, #1788) |
| Shared integration fixtures | `tests/Encina.IntegrationTests/{ADO,Dapper}/Collections.cs`, `Infrastructure/EntityFrameworkCore/Collections.cs`, `MongoDbCollection` | 10-provider integration tests |
| Test schemas | `tests/Encina.TestInfrastructure/Schemas/*Schema.cs` | `CreateDeadLetterSchemaAsync` |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` | `Messaging` 2800-2999 | **2990-2992** new (`DeadLetterLog`). 2945-2957 are the existing `DeadLetterLog`; 2958-2962 `MessagingLog`; 2963-2969 reserved by #718, 2970-2976 by #1200, 2977-2988 by #1251; 2989 used by #1970. After this plan only 2993-2999 remain: the next Messaging plan should register a new range (for example in the free block 5450-6999) |
| `Encina.MongoDB` | `MongoDB` 3100-3199 | **3165-3166** new (`Log.cs`); highest used today 3162; 3163-3164 are reserved by #718 (B1) |
| `Encina.ADO.*`, `Encina.Dapper.*`, `Encina.EntityFrameworkCore` | their own ranges | No new log messages: store failures surface as `Left` and are logged by the orchestrator, manager or cleanup processor |
| `Encina.OpenTelemetry` | — | No logs; tracing only |

No range is registered by this plan.

### Open Questions to Verify During Implementation

1. Does Pomelo (version current when work starts) translate `ExecuteUpdateAsync` / `ExecuteDeleteAsync` with the filter predicates on .NET 10? If not, the EF MySQL path needs a fallback, verified by the EF MySQL integration test.
2. Has #718 landed? If so, the ADO.NET and Dapper stores take its transaction accessor and enlist every command. Has #1170 replaced `OpenConnectionAsync`? The new stores must use the replacement.
3. Is 2989 still the last Messaging id used, and 3162 the last MongoDB id? Take the next free block if not.
4. Has #1990 created `docs/features/dead-letter-queue.md`?

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Core contract and components | 10 | Record, data, context, filter, store, limits, codes, orchestrator, manager, health check |
| DI | 3 | Messaging helper, DLQ extensions, MongoDB options |
| Provider registrations | 8 | `ServiceCollectionExtensions.cs` of each provider package |
| Entities, factories, stores | 24 | 8 packages × 3 |
| EF configuration, MongoDB index creator and log | 3 | |
| SQL scripts | 12 | `029_*` and `000_CreateAllTables.sql` in 6 packages |
| Diagnostics | 3 | `DeadLetterMetrics`, `DeadLetterLog`, decorator (+ OTel registration) |
| Fakes | 2 | Fake message and store |
| PublicAPI files | 11 | Messaging, 8 providers, OpenTelemetry, Testing.Fakes |
| Tests | ~45 | Unit ~8, guard ~6, contract 1, property 1, integration ~22, schemas 3, DI/telemetry 2, justifications 2 |
| Documentation | ~16 | ADR-046, feature page, 9 READMEs, INVENTORY, 3 changelog fragments, ROADMAP |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a pre-1.0 .NET 10 / C# 14 library (no backward compatibility, no [Obsolete]). Messaging patterns live in
Encina.Messaging; database providers (ADO.NET x3, Dapper x3, EF Core x3 on SQL Server/PostgreSQL/MySQL, MongoDB)
implement the pattern stores. Operations return Either<EncinaError, T>; time comes from TimeProvider; database
calls are async with CancellationToken; EncinaError.Message never reaches logs, tags or health data. Issue #583:
persistent IDeadLetterStore on the 10 providers (Oracle and SQLite out of the matrix, ADR-009/ADR-024); #1991
(wiring the five sources) relies on it.

IMPLEMENTATION OVERVIEW:
1. Contract: IDeadLetterMessage gets ErrorCode (renamed), SourceMessageId, TenantId, IsExpiredAt(utcNow); loses
   ExceptionMessage and IsExpired. IDeadLetterStore: AddAsync -> bool (unique SourcePattern+SourceMessageId),
   oldest-first GetMessagesAsync with newestFirst, conditional MarkAsReplayedAsync -> bool, an atomic replay claim,
   DeleteManyAsync.
2. Core: orchestrator stamps tenant and source id, handles duplicates, computes statistics with counts; manager
   propagates every Left; health check fails closed; fake follows the contract.
3. DI: AddDeadLetterQueueServices<TStore, TFactory>(UseDeadLetterQueue, DeadLetterOptions) called by the 8
   provider registrations; MongoDB options gain UseDeadLetterQueue, DeadLetterOptions, collection name.
4. Persistence: DeadLetterMessages table (029 scripts, quoted PascalCase on PostgreSQL), EF configuration without
   HasFilter, MongoDB indexes without TTL; six indexes including the unique source key.
5. Stores: DeadLetterStoreADO/Dapper (x3 each), DeadLetterStoreEF, DeadLetterStoreMongoDB.
6. Observability: InstrumentedDeadLetterStore (ActivitySource "Encina.Messaging.DeadLetter"), DeadLetterMetrics on
   Meter("Encina"), EventIds 2990-2992 (Messaging) and 3165-3166 (MongoDB).
7. Replay (with #2012): ReplayResult is an outcome code (max 256), nullable ReplayClaimedAtUtc with an atomic claim
   before dispatch; the manager defaults to the ambient tenant (explicit all-tenants option); no ActorId/CausationId
   (#2013), no ModuleId (#747); #2011 (health check) lands first.
8. Tests on every flag, contract base reused by integration tests on the 10 providers, docs, ADR-046, changelog.

KEY PATTERNS:
- {Pattern}Store{Provider} naming (DeadLetterStoreADO, ...), DeadLetter/ folder per provider package.
- TryAdd for store and factory so the application's own store wins; health check via TryAddEnumerable.
- One statement per write; ADO/Dapper enlist through #718's accessor when present; EF shares the scoped DbContext,
  so failure-path callers (#1991) use a new scope.
- Shared [Collection] fixtures, ClearAllDataAsync in InitializeAsync; Shouldly via Encina.Testing.Shouldly;
  FakeTimeProvider; per-file coverage targets with justifications; CRAP <= 10 on changed methods; zero warnings.

REFERENCE FILES:
- src/Encina.Messaging/DeadLetter/*.cs, src/Encina.Messaging/Health/DeadLetterHealthCheck.cs
- src/Encina.Messaging/MessagingServiceCollectionExtensions.cs, src/Encina.Messaging/MessagingConfiguration.cs
- src/Encina.ADO.SqlServer/Scheduling/ScheduledMessageStoreADO.cs, src/Encina.Dapper.PostgreSQL/Scheduling/
  ScheduledMessageStoreDapper.cs, src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessageStoreEF.cs,
  src/Encina.MongoDB/Scheduling/ScheduledMessageStoreMongoDB.cs
- src/Encina.Testing.Fakes/Stores/FakeDeadLetterStore.cs
- src/Encina.OpenTelemetry/MessagingStores/InstrumentedScheduledMessageStore.cs
- src/Encina/Diagnostics/EventIdRanges.cs
- docs/plans/outbox-atomicity-ado-implementation-plan-718.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Write-once records read by operators and the health check; a cached count or page would hide new dead letters, which is the signal the queue exists for |
| 2 | OpenTelemetry | ✅ | `InstrumentedDeadLetterStore` with `ActivitySource("Encina.Messaging.DeadLetter")` and `DeadLetterMetrics` counters on `Meter("Encina")`; no tenant id and no payloads or error messages in telemetry (Phase 10) |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` 2990-2992 in `EventIdRanges.Messaging` and 3165-3166 in `EventIdRanges.MongoDB` (Phase 10; B1) |
| 4 | Health Checks | ✅ | `DeadLetterHealthCheck` fails closed on a store `Left` and registers through `TryAddEnumerable` (#2011, which lands first; Phases 2-3); provider database health checks already cover connectivity |
| 5 | Validation | ✅ | Store argument rules, `DeadLetterOptions` setter validation, source key lengths checked before I/O (Phases 1-3, 9) |
| 6 | Resilience | ❌ | Calls only the application's own database; a failed capture returns `Left` to the source, which owns retries (#1991), and the cleanup retries at its next interval; EF Core keeps its own execution strategy |
| 7 | Distributed Locks | ✅ | Capture is deduplicated by the unique key and deletes are idempotent. Two concurrent replays of one message are prevented by the atomic claim before dispatch (`ReplayClaimedAtUtc`, B3), implemented in this PR with #2012 |
| 8 | Transactions | ✅ | Each write is one statement; ADO.NET/Dapper enlist through #718's accessor when present; the EF store's shared-`DbContext` rule is documented for failure-path callers (#1991) (Phases 7, 9) |
| 9 | Idempotency | ✅ | Unique `(SourcePattern, SourceMessageId)`, `AddAsync -> bool`, `OnDeadLetter` once per source message (Choice 6) |
| 10 | Multi-Tenancy | ✅ | `TenantId` column stamped at capture, filter and index (Choice 7); the manager reads, replays and deletes default to the ambient `IRequestContext.TenantId` with an explicit all-tenants option (B7); replay under the persisted tenant is #2013 |
| 11 | Module Isolation | ⏭️ | Deferred to #747 (open; no column now, B6); `DeadLetterMessages` is added to its scope (comment already posted on #747). `SourcePattern` names a messaging pattern, not a module, so the issue's "Source field enables module filtering" does not hold |
| 12 | Audit Trail | ⏭️ | Replay and delete are operator actions on records that may hold personal data: #2013 (record them in `IOperationAuditStore`, with `ActorId` and `CausationId`); no new issue file |

---

## Prerequisites & Dependencies

### Ordering with related work

| Issue | State | Relation and recommended order |
|-------|-------|-------------------------------|
| #1991 | open, decided (a) 2026-10-07 | Follows this issue. It calls `DeadLetterOrchestrator` from the five sources and passes `SourceMessageId` and the source's tenant through `DeadLetterContext`. It must call from a new scope on EF Core. It relies on Choices 4-7. Its milestone (v0.14.0) is earlier than this issue's (v0.19.0) although it is blocked by it: the orchestrator should align the milestones |
| #771 | open | Moves `DeadLetterCleanupProcessor` to a recurring scheduled command. Independent of the store: the command will call the same `DeleteExpiredAsync`. Recommended after #583, so it migrates the final processor. If #771 lands first, Phase 3 registers its command instead of the hosted service |
| #718 | open, decided 2026-10-06 | Introduces `IDbTransactionAccessor`; the new ADO.NET/Dapper stores enlist through it if it has landed (Phase 5) |
| #1170, #1418 | open | No-op `OpenConnectionAsync` and token-less Dapper calls in existing stores; the new stores do not copy them |
| #1990 | open | DLQ documentation gaps; Phase 12 writes only the provider section of the same page |
| #1788 | open | `EncinaError.Message` on activities in the existing decorators; the new decorator avoids it |
| #747 | open | `ModuleId` on messaging entities (matrix row 11); `DeadLetterMessages` added to its scope, no column now |
| #2011 | open, v0.14.0 | Health check `Left` handling and `TryAddEnumerable` registration. Lands first as its own small PR; Phases 2 and 3 drop those items |
| #2012 | open, v0.14.0 | Manager errors, replay exclusivity (claim before dispatch) and the plaintext replay outcome. Folded into the #583 PR (`Fixes #583, #2012`) |
| #2013 | open, p0 | Replay under the stored tenant, operator audit, `ActorId` / `CausationId` columns (B8). Follows #583 |
| #737, #739, #1257 | open | Tenancy of outbox and scheduled rows. #1991 can pass a source tenant only once those rows carry one |
| #584-#589 | open, post-1.0 | Further store families; they implement the contract fixed here (ADR-046) |

### Advisable before or with this work

None blocks Phase 1. SPEC-000 DEC-008 is merged (#2008), so the PR can cite it. #2011 lands first as its own PR.

---

## Next Steps

1. Done: the maintainer decided Design Choices 1-9 and B1-B10 on 2026-10-09; the answers are in `## Maintainer Decisions` (last section).
2. The issues for the defects found by the plan exist; no new issue files are opened (maintainer decision B2). Order by priority:
   - #2011 (`[BUG]`, health check `Left` handling and `TryAddEnumerable`) lands first, as its own small PR; Phases 2 and 3 then drop those items.
   - #2012 (`[BUG]`, manager errors, replay exclusivity and the plaintext replay outcome) is folded into the #583 PR, which is `Fixes #583, #2012` (the claim before dispatch and the conditional `MarkAsReplayedAsync` change the store contract all 10 stores implement).
   - #2013 (`[FEATURE]`, p0: replay under the stored tenant and correlation, operator audit, and the `ActorId` / `CausationId` columns, B8) and #1991 (p0) follow #583.
3. Comment on #1991 (the contract it relies on and the new-scope rule on EF Core), on #771 (ordering) and on #1990 (Phase 12 writes only the provider section). The comment on #747 (add `DeadLetterMessages` to its `ModuleId` scope) is already posted.
4. Align the milestones of #1991 (v0.14.0) and #583 (v0.19.0), and retitle #583 to "10 database providers" (its title still says 13).
5. Link this plan from #583. When work starts, re-check every cited file and line, the EventIds and #718/#1170/#1990 against that day's `main`.
6. Implement Phases 1-12 in one PR `Fixes #583, #2012`: the contract change in Phase 1 does not build without Phases 2-8. #2011 lands before it as its own PR.

---

## Maintainer Decisions

Asked on 2026-10-09 after the re-check of the plan against `main` at `0a8afcde` (`artifacts/plans/583-recheck.md`); the answers are two comments on issue #583. One dated entry per Design Choice, numbered like the choices, then B1 to B11.

Storage (2026-10-09): the DLQ is stored on the 10 usual providers only (ADO.NET x3, Dapper x3, EF Core x3, MongoDB), next to the outbox; no Marten store (general-purpose messaging infrastructure per ADR-019, a mutable working set, PostgreSQL-only in Marten). No follow-up issue for Marten.

### 1 (2026-10-09) Persisted record shape

Option A: reshape, then persist (`ErrorMessage` to `ErrorCode`, `ExceptionMessage` removed, `SourceMessageId` and `TenantId` added). `ActorId` and `CausationId` are not added here (B8).

### 2 (2026-10-09) Expiry evaluation

Option A: `IsExpiredAt(DateTime utcNow)`, rule `ExpiresAtUtc <= now` shared by the manager, the fake and the 10 stores; a contract test pins the boundary.

### 3 (2026-10-09) Package placement

Option A: a `DeadLetter/` folder in each of the 8 provider packages; script `029_CreateDeadLetterMessagesTable.sql` in the six ADO/Dapper packages.

### 4 (2026-10-09) Registration

Option A: `UseDeadLetterQueue` drives `AddDeadLetterQueueServices` from every provider registration; plus B5 (`AddEncinaADOWithTenancy` also calls it, with a `ValidateOnBuild`/`ValidateScopes` DI test) and B10 (keep `TryAdd` for the fake, document the order rule, tests use `ReplaceWithFakes()`).

### 5 (2026-10-09) Store contract

Option A: oldest first by default with `newestFirst`; set-based `DeleteManyAsync`; expired rows counted in the database (`ExpiresAtOrBeforeUtc`); conditional `MarkAsReplayedAsync`; every store `Left` propagated. B9: `DeleteAllAsync(DeadLetterFilter.All)` keeps deleting the whole queue in one statement, documented in XML docs and covered by a test; no confirmation parameter.

### 6 (2026-10-09) Idempotent capture

Option A (maintainer): unique `(SourcePattern, SourceMessageId)` from table creation; `AddAsync` returns `bool` (false = already captured, not an error). #1991 is prioritized.

### 7 (2026-10-09) Tenancy

Option A, settled by SPEC-000 section 2: `TenantId` column stamped at capture, explicit filter on the store. See B7 for the manager default.

### 8 (2026-10-09) MongoDB expired-row cleanup

Option A, settled by provider coherence (AGENTS.md section 5) and `EnableAutomaticCleanup`: the same cleanup loop on all 10 providers; MongoDB gets an ordinary index, no TTL index.

### 9 (2026-10-09) PostgreSQL identifiers

Option A, settled by provider coherence: quoted PascalCase identifiers on PostgreSQL for ADO.NET, Dapper and EF Core. See B4 for the timestamp type.

### B1 (2026-10-09) MongoDB EventIds

Settled by AGENTS.md section 7 and #718's earlier reservation: Messaging keeps 2990-2992; the DLQ MongoDB ids become 3165-3166.

### B2 (2026-10-09) Existing issues #2011, #2012, #2013

#2011 first as its own small PR; #2012 folded into the #583 PR (`Fixes #583, #2012`); #2013 (p0) and #1991 (p0) after #583. Next Steps and matrix rows 7, 10 and 12 reference these issues instead of new issue files.

### B3 (2026-10-09) Replay claim and plaintext outcome

`ReplayResult` stores an outcome code only (max 256), never error text; a nullable `ReplayClaimedAtUtc` column and an atomic claim method before dispatch (with claim expiry), implemented in the #583 PR together with #2012. This PR is prioritized to start now.

### B4 (2026-10-09) PostgreSQL timestamp type

Settled by correctness: `TIMESTAMPTZ` for every `*AtUtc` column; the contract test covers `ExpiresAtUtc == now` with a non-UTC session time zone.

### B5 (2026-10-09) `AddEncinaADOWithTenancy`

As recorded under Choice 4.

### B6 (2026-10-09) `ModuleId`

Settled by the open #747: deferred to #747 (comment posted there to include `DeadLetterMessages`); no column now.

### B7 (2026-10-09) Tenant default in the manager

Settled by SPEC-002 REQ-061: `DeadLetterManager` reads, replays and deletes default to `IRequestContext.TenantId` when present, with an explicit all-tenants option for operator tooling; the store contract stays explicit; the cleanup processor and health check stay deployment-wide.

### B8 (2026-10-09) Actor and causation columns

`ActorId` and `CausationId` are not added by #583; #2013 adds them (recorded on #2013, now p0). #583 and #2013 are both done now, back to back.

### B9 (2026-10-09) `DeleteAllAsync(All)`

As recorded under Choice 5.

### B10 (2026-10-09) Fake registered after the provider

As recorded under Choice 4.

### B11 (2026-10-09) Smaller corrections

Applied without a decision: OpenTelemetry sources at `:202-205`, `HasFilter` at `:73`, MongoDB camelCase element names.
