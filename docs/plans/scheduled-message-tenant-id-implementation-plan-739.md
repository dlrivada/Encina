# Implementation Plan: `TenantId` on `ScheduledMessage` — Tenant-Scoped Scheduling Rows, Queries, Cancel and Dispatch

> **Issue**: [#739](https://github.com/dlrivada/Encina/issues/739)
> **Type**: Feature (SPEC-002 REQ-061, AC-043; delivered with #1164, tracked by #1186)
> **Complexity**: High (8 phases, 10 database providers, 15 pull requests, about 119 file touches)
> **Estimated Scope**: ~600 lines of production code + ~3,200 lines of tests, SQL scripts, manifests and documentation
> **Related plans** (cited by issue number, not by link): #737 (the outbox row, the template of this plan), #738 (the saga row, the template of the split), #760 (the inbox row), #1257 (the shared tenant-scope primitive and the default-tenant rule), #346 (the tenant gate and `[TenantAgnostic]`), #2676 (tenant telemetry helper), #735 (the dispatch context built in the processor), #1222 and #146 (schedule key and cancel and reschedule; both wait for this plan), #152 (tenant tag on scheduling telemetry), #159 (tenant-scoped statistics), #1164 (envelope; restores user and correlation)

---

## Summary

Give the scheduled message a tenant: `IScheduledMessage.TenantId`, stamped when `SchedulerOrchestrator` creates the row, persisted by all 10 database providers, readable through a strict tenant-filtered query, cancellable only by its tenant, restored in the dispatch context, and fail-closed when a multi-tenant application schedules a message with no tenant. Multi-tenancy is required in 1.0 for every capability SPEC-002 adds or changes (REQ-061, AC-043, reference scenario S11); #737, #739 and #760 are delivered with #1164, and #738 adds the saga row.

**State of `origin/main` (`1c067b45b`), verified by reading and grep (corrects the issue text).**

- The issue says "13 database providers" and "`ITenantContext`". The matrix is 10 (`AGENTS.md` section 5): ADO.NET x3, Dapper x3, EF Core x3 (one `ScheduledMessageStoreEF` serves SQL Server, PostgreSQL and MySQL) and MongoDB. `ITenantContext` does not exist; the ambient tenant is `IRequestContext.TenantId` (`src/Encina/Abstractions/IRequestContext.cs:125`, `WithTenantId` at line 185).
- `IScheduledMessage` (`src/Encina.Messaging/Scheduling/IScheduledMessage.cs`) has `Id`, `RequestType`, `Content`, `ScheduledAtUtc`, `CreatedAtUtc`, `ProcessedAtUtc`, `ErrorMessage`, `RetryCount`, `NextRetryAtUtc`, `IsRecurring`, `CronExpression`, `LastExecutedAtUtc` and two computed members; there is no tenant. `ScheduledMessageProcessor.cs` documents it ("`IScheduledMessage` does not carry TenantId yet. See Issue #739").
- Rows are created in one place family: `SchedulerOrchestrator.ScheduleAsync` and `ScheduleRecurringAsync` call `IScheduledMessageFactory.Create(id, requestType, content, scheduledAtUtc, createdAtUtc, isRecurring, cronExpression)` and then `IScheduledMessageStore.AddAsync`. The orchestrator is scoped; its constructor ends with `ICronParser? cronParser = null, TimeProvider? timeProvider = null, DeadLetterSourceCapture? deadLetterCapture = null`; `new SchedulerOrchestrator(` appears 28 times in 8 files.
- Implementers (grep over `src` and `tests`): `IScheduledMessage` has 13 (8 provider classes, `FakeScheduledMessage` in `Encina.Testing.Fakes`, the class in `ScheduledMessageFaker.cs` (`Encina.Testing.Bogus`), the one in `ArbitraryMessages.cs` (`Encina.Testing.FsCheck`), two `BenchmarkEntityFactory` classes); `IScheduledMessageStore` has 10 (8 stores, `InstrumentedScheduledMessageStore`, `FakeScheduledMessageStore`); `IScheduledMessageFactory` has 12 (8 provider factories and 4 test doubles) and its signature does not change in this plan.
- The processor is cross-tenant: `GetDueMessagesAsync(batchSize, maxRetries)` filters by state only, and the dispatch lambda is `(msg, type, req, ct) => dispatcher.DispatchAsync(type, req, ct)`. The plan of #735 changes the dispatcher to take an `IRequestContext` built per message in that lambda; this plan adds the row's tenant to that context.
- Id-based operations (`CancelAsync(Guid)`, `MarkAsProcessedAsync`, `MarkAsFailedAsync`, `RescheduleRecurringMessageAsync`) take only the id: a caller who knows an id can cancel any tenant's message today. #146 (reschedule) and #1222 (cancel by key) make #739 a hard prerequisite of their public pull requests.
- The tenant precedents: dead letter tables carry `TenantId VARCHAR(128) NULL` (`src/Encina.ADO.MySQL/Scripts/000_CreateAllTables.sql`, `COLLATE utf8mb4_bin`), and #737 and #738 add the same column to the outbox and saga rows.

**Not in scope**: restoring user, correlation and causation at dispatch (#1164); tenant-fair batching of the processor (the issue does not ask for it); the tenant tag on scheduling telemetry (#152, its last pull request); tenant-scoped statistics (#159, its last pull request); the outbox, saga and inbox rows (#737, #738, #760).

**Affected packages**: `Encina.Messaging`, `Encina.ADO.SqlServer|PostgreSQL|MySQL`, `Encina.Dapper.SqlServer|PostgreSQL|MySQL`, `Encina.EntityFrameworkCore`, `Encina.MongoDB`, `Encina.OpenTelemetry` (instrumented store), `Encina.Testing.Fakes`, `Encina.Testing.Bogus`, `Encina.Testing.FsCheck`, and two benchmark projects.

**Provider category**: Database (10): ADO.NET x3, Dapper x3, EF Core x3 (one store class), MongoDB.

---

## Design Choices

<details>
<summary><strong>1. The tenant when tenancy is off (Dilemma D1)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `null`: `TenantId` stays null and no filter applies** | The issue's own shape; no change for applications without tenancy | A null row is a special case in every predicate; a unique index on `(TenantId, ScheduleKey)` (#1222) treats NULLs as equal on SQL Server and as distinct on PostgreSQL and MySQL, so key uniqueness would differ per provider |
| **B) The reserved default tenant of #1257 (`default`), stamped when tenancy is off** | One uniform rule and the wording of SPEC-002 AC-043 ("with tenancy off, a fixed default tenant applies"); plain `TenantId = @TenantId` filters; the composite unique index behaves the same on all 10 providers | Turning tenancy on later keeps `default` on old rows |

### Chosen Option: **B — the reserved default tenant, whatever #1257 decides, read from `TenantScope.TenantId`** (recommended, pending the maintainer)

### Rationale

- B wins on the unique index and one rule; A wins if #1257 keeps null. This plan never hard-codes `null` or `"default"`: the orchestrator writes `scope.TenantId` from `ITenantScopeResolver.ResolveWrite`, and the column stays nullable in both cases. The reserved id is injective with the tenant-qualified keys of #1222: tenant ids compare ordinally (binary collation) and `default` is rejected as a real tenant by #1257's resolver.

</details>

<details>
<summary><strong>2. The tenant-filtered query (Dilemma D2)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A new strict member `Task<Either<EncinaError, IEnumerable<IScheduledMessage>>> GetPendingByTenantAsync(string tenantId, int batchSize, CancellationToken cancellationToken = default)` returning the tenant's unprocessed messages (due or not, recurring included) ordered by `ScheduledAtUtc`; a null tenant is `ArgumentNullException`, a blank or padded one `ArgumentException`; the existing due member stays the system-scope read** | Fail closed by shape; gives a tenant visibility into its own scheduled operations (the issue's motivation); additive | One interface member on 10 implementers |
| **B) An optional `string? tenantId = null` on `GetDueMessagesAsync`** | No new member | Null would mean "all tenants" (fail open); the processor's call is the null case; due-only, so a tenant cannot see future messages |
| **C) No query; callers filter in memory** | No API growth | Loads other tenants' rows, contradicting AC-043 |

### Chosen Option: **A — one strict, additive member** (recommended, pending the maintainer)

### Rationale

- A wins when strictness must be structural (AC-043: tenant A never reads tenant B's rows); B wins only if fewer members matter more. The predicate extends the existing pending predicate (`ProcessedAtUtc IS NULL OR IsRecurring = 1`, as in the six SQL stores) with `AND TenantId = @TenantId` (SQL Server `TOP (@BatchSize)`, PostgreSQL and MySQL `LIMIT @BatchSize`); EF Core `m.TenantId == tenantId`; MongoDB `Builders<ScheduledMessage>.Filter.Eq(m => m.TenantId, tenantId)`.

</details>

<details>
<summary><strong>3. Where the tenant comes from at write time (Dilemma D3)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ITenantScopeResolver.ResolveWrite(null, allowTenantless)` of #1257 (a new optional constructor parameter of `SchedulerOrchestrator`, appended after `deadLetterCapture`); the orchestrator sets `message.TenantId` after `IScheduledMessageFactory.Create(...)` and before `IScheduledMessageStore.AddAsync`** | One fail-closed rule shared with the other SPEC-002 stores; the factory interface and its 12 implementers do not change; the settable-property route that #1222 recommends for new columns; the optional parameter keeps the constructor order valid (CS1737) and the 28 `new SchedulerOrchestrator(` sites compile unchanged | Depends on #1257's first pull request |
| **B) `IRequestContextAccessor` read directly in the orchestrator** | No dependency on #1257 | A fourth copy of the tenant rule (the drift #1257 describes) |
| **C) A `tenantId` parameter on `ScheduleAsync` or on `IScheduledMessageFactory.Create`** | No ambient state | Breaks every caller and 12 factory implementers; contradicts "auto-populated" |

### Chosen Option: **A — the shared resolver, stamping after `Create`** (recommended, pending the maintainer)

### Rationale

- A wins because five SPEC-002 consumers already share the one signature (#798, #845, #752, the key stores, #737); B wins only if #1257's Design Choice 1 is not accepted. The orchestrator never calls `SaveChangesAsync` (the caller or the processor commits), so the tenant is written with the row by the same `AddAsync` call and no atomicity claim is made. When #152's last pull request merges, it appends `MultiTenancyMarker? multiTenancy = null` after this parameter; both are optional and last.

</details>

<details>
<summary><strong>4. A multi-tenant application schedules a message with no tenant</strong></summary>

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (compliance and security gates fail closed when context is missing; the only opt-out is explicit and logged), SPEC-002 DEC-006 and AC-043, and the `[TenantAgnostic]` opt-out defined by #346 (Design Choice 2 of that plan).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ScheduleAsync` and `ScheduleRecurringAsync` return `Left(encina.tenancy.tenant_required)` (ADR-062 category `InvalidRequest`, created by #1257) when tenancy is on and no tenant resolves; a request type marked `[TenantAgnostic]` is the explicit, logged opt-out and is stored with a no-tenant row** | A forgotten tenant can only deny, never store an unowned row; platform jobs stay possible | A background scheduler with no restored tenant fails until it sets one (#1164) |
| **B) Store a null-tenant row and continue** | Never blocks | Fail open: the row is invisible to every tenant query and cannot be isolated |

### Chosen Option: **A — fail closed, `[TenantAgnostic]` as the explicit opt-out**

### Rationale

- Only A complies. The decision matrix (one unit-test row per line):

  | Tenancy on | Ambient tenant | Request `[TenantAgnostic]` | Result |
  |---|---|---|---|
  | yes | absent | no | `Left(encina.tenancy.tenant_required)`, nothing stored |
  | yes | T | no | stored with `TenantId = T` |
  | yes | absent | yes | stored with a no-tenant row, logged once per type (EventId 2390) |
  | yes | T | yes | stored with `TenantId = T` (the attribute only lifts the requirement) |
  | no | absent | any | stored with `scope.TenantId` (null under D1 A, `default` under D1 B) |
  | no | T | any | stored with `TenantId = T` |

- No new error code is created: the `Left` values are #1257's `encina.tenancy.tenant_required` (`InvalidRequest`) and `encina.tenancy.tenant_mismatch` (`Forbidden`). The attribute is created by #346; the worker of pull request 12b confirms its `AttributeUsage` accepts request classes.

</details>

<details>
<summary><strong>5. Column shape, collation and index</strong></summary>

**Class:** Trivial
**Settled by:** the dead letter tables (`TenantId VARCHAR(128) NULL`, `src/Encina.ADO.MySQL/Scripts/000_CreateAllTables.sql`) and the 128-character tenant id limit and binary collation chosen in #737 and #1257.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `TenantId NVARCHAR(128) NULL` (SQL Server, `Latin1_General_100_BIN2`), `VARCHAR(128) NULL` (PostgreSQL), `VARCHAR(128) COLLATE utf8mb4_bin NULL` (MySQL), a string field in MongoDB; an index `(TenantId, ProcessedAtUtc, ScheduledAtUtc)`; when #1222 has merged, its unique index on the schedule key becomes `(TenantId, ScheduleKey)`** | Matches the neighbouring tables; ids compare case-sensitively on every provider, so two tenants that differ only by case never alias; serves the strict query | One more index per table |
| **B) A separate tenant table with a foreign key** | Normalized | No other messaging table does it; a join on the hot path |

### Chosen Option: **A — nullable binary-collated string with a composite index**

### Rationale

- The due query keeps its existing index `IX_ScheduledMessages_ScheduledAt_Processed`. No migration helper: the project is pre-1.0 and the scripts are `CREATE TABLE` scripts (`AGENTS.md` section 1); `Scripts/000_CreateAllTables.sql` and `Scripts/004_CreateScheduledMessagesTable.sql` of each of the six ADO.NET and Dapper packages are edited in place, the EF Core configuration (`ScheduledMessageConfiguration`) takes an explicit binary collation as `DeadLetterMessageConfiguration` does, and `MongoDbIndexCreator.CreateSchedulingIndexesAsync` (line 192) adds `IX_Scheduling_Tenant`.

</details>

<details>
<summary><strong>6. Cancel only inside the tenant (Dilemma D4)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A strict store member `CancelForTenantAsync(Guid messageId, string tenantId, CancellationToken)` (`DELETE ... WHERE Id = @Id AND TenantId = @TenantId`; zero rows is `Right`, as cancel is idempotent in #146); `SchedulerOrchestrator.CancelAsync` calls it with the resolved tenant when tenancy is on and the existing `CancelAsync(Guid)` when it is off** | Fail closed: another tenant's id affects nothing, and the answer does not reveal whether the id exists; unblocks #146 and #1222 | One more member on 10 implementers |
| **B) Leave `CancelAsync(Guid)` global (a system operation)** | No change | Any caller who knows an id cancels any tenant's message: contradicts AC-043 |
| **C) Read the row first and compare tenants, returning `tenant_mismatch`** | Explicit diagnosis | Leaks that the id exists to a caller of another tenant; two round trips |

### Chosen Option: **A — a strict cancel member** (recommended, pending the maintainer)

### Rationale

- A wins on AC-043 and on not leaking existence; B wins only if the cancel API is operator-only (then #146's public pull request would carry its own guard). The internal marks (`MarkAsProcessedAsync`, `MarkAsFailedAsync`, `RescheduleRecurringMessageAsync`) stay id-only: they are called by the processor with rows it just read.

</details>

<details>
<summary><strong>7. The tenant of a dispatched message and the #1164 envelope (Dilemma D5)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The typed `TenantId` column is the single source: the processor callback of #735 builds the dispatch context with `.WithTenantId(message.TenantId)` when the value is non-blank; the #1164 envelope carries user, correlation and causation only** | One value that the strict query, the index and the dispatch all read; they can never disagree | #1164's text lists `TenantId` in the envelope; it must drop it |
| **B) The envelope carries the tenant too and the column is a copy** | The envelope is self-contained | Two values for one fact; a mismatch needs its own rule |

### Chosen Option: **A — the column is the source** (recommended, pending the maintainer)

### Rationale

- A wins when a queryable, indexed tenant must also drive dispatch; B wins if the envelope travels outside the row (CDC and gRPC paths of #1164). A null tenant on a row of a non-agnostic request is denied by the tenant gate of #346 inside the dispatch, so the message follows the retry and dead-letter path: fail closed. Each run of a recurring message restores the tenant it was created with.

</details>

<details>
<summary><strong>8. Build order of the pull request split (Dilemma D6)</strong></summary>

**Class:** Dilemma

The change touches interfaces with many implementers (13 `IScheduledMessage`, 10 `IScheduledMessageStore`). A pull request that changes an interface before its implementers does not compile (`AGENTS.md` section 8), and a pull request is at most 15 files and 1,500 lines (section 11).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Implementers first, interface last: each provider pull request adds the `TenantId` property, `GetPendingByTenantAsync` and `CancelForTenantAsync` to its concrete classes with SQL and integration test; the interfaces, the decorator and the orchestrator come after every implementer has the members; the factory is untouched** | Every pull request builds and carries its own tests and manifest entries | The members exist on classes before the interfaces have them; 15 pull requests |
| **B) One pull request** | One review | About 119 file touches: over the limits (needs `size-exception-approved`) |
| **C) Default interface members as a scaffold** | The interface goes first | A compatibility layer merges (`AGENTS.md` section 1) with untested defaults |

### Chosen Option: **A — implementers first, interface last** (recommended, pending the maintainer)

### Rationale

- A wins because the limits are hard gates and a half-wired interface breaks the build; it is the order of #737, #738 and #1222. B wins if the maintainer prefers one large review; C is rejected by the compatibility rule, which is the maintainer's to relax. The same build-order choice is a Dilemma in #159 too (one classification across the family).

</details>

<details>
<summary><strong>9. Cancelling a stored `[TenantAgnostic]` row (Dilemma D7)</strong></summary>

**Class:** Dilemma

Design Choice 4 stores a row with no tenant for a `[TenantAgnostic]` request type while tenancy is on. `SchedulerOrchestrator.CancelAsync` calls `CancelForTenantAsync` with the resolved tenant when tenancy is on, and with no resolved tenant it denies (fail closed), so such a row cannot be cancelled through the orchestrator.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The orchestrator stays strict; a platform caller with no tenant cancels through the store's system-scope `CancelAsync(Guid)` (the id-only member, kept for the processor and operators)** | No new member; the strict path never touches a null-tenant row; the opt-out stays explicit because a platform caller injects the store, not the tenant-scoped orchestrator | The platform caller bypasses the orchestrator (no logging there) |
| **B) A second orchestrator method `CancelTenantAgnosticAsync(Guid)` that deletes only rows whose `TenantId IS NULL` and whose request type is `[TenantAgnostic]`** | One public, logged path; cannot touch any tenant's row | One more public method and one more store member (`CancelAgnosticAsync`) on 10 implementers |
| **C) `CancelAsync` with no tenant cancels a null-tenant row** | One method | Fail open for a caller that merely lost its tenant |

### Chosen Option: **A — the system-scope store member for platform callers** (recommended, pending the maintainer)

### Rationale

- A wins on size and on failing closed in the tenant-scoped API; B wins if platform jobs must be audited through the orchestrator; C is rejected by `AGENTS.md` section 3.

</details>

---

## Implementation Phases

### Phase 1: ADO.NET and Dapper providers (pull requests 1 to 6)

> **Goal**: The column, the state class and the two tenant members in the six packages, one pull request per package, each carrying its tests and manifest entries. Pull requests 1 to 3 are the ADO.NET packages (SQL Server, PostgreSQL, MySQL) and 4 to 6 the Dapper packages in the same database order; each Dapper pull request merges after the ADO.NET pull request of its database, because both share one inline test schema (task 5). Size: 9 files for pull requests 1 to 3 (8 plus the test schema file) and for pull request 4 (8 plus `DapperStoreEnlistmentTests.cs`), 8 for pull requests 5 and 6.

<details>
<summary>Tasks</summary>

Per package (`Encina.ADO.SqlServer|PostgreSQL|MySQL`, `Encina.Dapper.SqlServer|PostgreSQL|MySQL`):

1. SQL scripts: add `TenantId` and the index `(TenantId, ProcessedAtUtc, ScheduledAtUtc)` in `Scripts/004_CreateScheduledMessagesTable.sql` and `Scripts/000_CreateAllTables.sql` (Design Choice 5; MySQL `COLLATE utf8mb4_bin`, SQL Server `COLLATE Latin1_General_100_BIN2`).
2. `Scheduling/ScheduledMessage.cs`: `public string? TenantId { get; set; }` (compiles against the old interface).
3. `Scheduling/ScheduledMessageStoreADO.cs` or `ScheduledMessageStoreDapper.cs`: add `TenantId` to the `INSERT` column and parameter lists and to `MapToScheduledMessage` (ADO.NET; Dapper maps by name), and the public methods `GetPendingByTenantAsync(string tenantId, int batchSize, CancellationToken)` and `CancelForTenantAsync(Guid messageId, string tenantId, CancellationToken)` with the strict predicates of Design Choices 2 and 6 and the existing argument checks (`StoreValidationMessages`); a blank tenant throws `ArgumentException`. Database calls stay asynchronous with a `CancellationToken`. The members are concrete until pull request 12a adds them to `IScheduledMessageStore`.
4. The package's `PublicAPI.Unshipped.txt`, its `.github/coverage-manifest/<Package>.json` entries, a store-level integration test (`[Collection("ADO-<Db>")]` or `[Collection("Dapper-<Db>")]`; `ClearAllDataAsync` in `InitializeAsync`; never `IClassFixture`): tenant round trip, `GetPendingByTenantAsync` isolation between two tenants including a recurring and a future message, a tenant differing only by case is another tenant, `CancelForTenantAsync` of another tenant's id leaves the row, and a guard test edit (null tenant, blank tenant, non-positive batch size, empty id).
5. The integration tests do not run the `Scripts/*.sql` files: the ADO.NET and Dapper integration fixtures build `ScheduledMessages` from inline DDL in `tests/Encina.TestInfrastructure/Schemas/`. Pull request 1 edits `SqlServerSchema.cs` (line 101, `CREATE TABLE ScheduledMessages`), pull request 2 edits `PostgreSqlSchema.cs` (line 101, `CREATE TABLE IF NOT EXISTS scheduledmessages`, lowercase columns) and pull request 3 edits `MySqlSchema.cs` (line 91): each adds the `TenantId` column with the binary collation of task 1 (lowercase `tenantid` in PostgreSQL) and the tenant index. The Dapper pull request of the same database reuses that edit. Pull request 4 also edits `tests/Encina.UnitTests/Dapper/Messaging/DapperStoreEnlistmentTests.cs` (the SQLite DDL at line 272 of `ScheduledMessages` lacks the column, and the store's `INSERT` now writes it). Verified with `git grep -n -i -E "create table[^;]*scheduledmessages" -- . ":!*.md"` on `origin/main`: the other creators are the 12 SQL scripts of the six packages (task 1), the three CREATE statements in each of `AdoSchemaBuilder.cs` and `DapperSchemaBuilder.cs` (pull request 11), and nothing else; there is no EF migration or model snapshot in the repository, and the EF contexts of the tests (`TestEFDbContext`, `TestPostgreSqlDbContext`, `ContractTestDbContext`, the two `BenchmarkDbContext` classes) map the entity by convention or through `ScheduledMessageConfiguration`, so the new property needs no edit there.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```
CONTEXT:
- Issue #739; plan docs/plans/scheduled-message-tenant-id-implementation-plan-739.md. Dilemmas D1-D6 are recommended, pending the maintainer: follow the Maintainer Decisions section if it exists.
- Encina is pre-1.0: no compatibility overloads, no migration helpers. The interfaces are NOT changed yet: this pull request adds TenantId and the two members to ONE package's concrete classes so it builds against the current interfaces.

TASK:
Apply the Phase 1 tasks to the one package of this pull request; the six implementations behave identically and differ only in SQL dialect.

KEY RULES:
- Strict predicates: no null or blank tenant ever matches every row.
- SQL Server: @param, TOP (@n), NVARCHAR with BIN2 collation; PostgreSQL: LIMIT @n, case-sensitive identifiers; MySQL: LIMIT @n, backticks, utf8mb4_bin on the tenant column.
- Asynchronous database calls with a CancellationToken; never IDbConnection.Open().

REFERENCE FILES:
- src/Encina.ADO.SqlServer/Scheduling/ScheduledMessageStoreADO.cs
- src/Encina.Dapper.PostgreSQL/Scheduling/ScheduledMessageStoreDapper.cs
- src/Encina.ADO.SqlServer/Scripts/004_CreateScheduledMessagesTable.sql
- src/Encina.ADO.MySQL/Scripts/000_CreateAllTables.sql
```

</details>

---

### Phase 2: EF Core and MongoDB providers (pull requests 7 and 8)

<details>
<summary>Tasks</summary>

1. `src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessage.cs`: `TenantId`; `ScheduledMessageConfiguration.cs`: `HasMaxLength(128)`, the explicit binary collation and `HasIndex(x => new { x.TenantId, x.ProcessedAtUtc, x.ScheduledAtUtc })` named `IX_ScheduledMessages_Tenant`; `ScheduledMessageStoreEF.cs`: the two members as LINQ (`m.TenantId == tenantId`; cancel as `ExecuteDeleteAsync` on the filtered set). No EF migration is added (pre-1.0). Integration tests: one class per database (`EFCore-SqlServer`, `EFCore-PostgreSQL`, `EFCore-MySQL`), the guard edit, PublicAPI and manifest.
2. `src/Encina.MongoDB/Scheduling/ScheduledMessage.cs` and `ScheduledMessageStoreMongoDB.cs`: the property and the two filters; `MongoDbIndexCreator.CreateSchedulingIndexesAsync` adds the compound index `IX_Scheduling_Tenant`, guarded by `UseScheduling` as the existing indexes; one integration test class on the MongoDB fixture, the guard edit, PublicAPI and manifest.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```
CONTEXT:
- Phase 1 pattern applies; EF Core serves three databases through one store class, so integration tests run once per database collection.

TASK:
Add TenantId, GetPendingByTenantAsync and CancelForTenantAsync to ScheduledMessageStoreEF and ScheduledMessageStoreMongoDB, with tests, in two pull requests.

KEY RULES:
- Pass the binary collation explicitly in the EF configuration; no model gets a case-insensitive default.
- No migration is generated; MongoDB indexes are created by MongoDbIndexCreator.

REFERENCE FILES:
- src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessageStoreEF.cs
- src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessageConfiguration.cs
- src/Encina.MongoDB/MongoDbIndexCreator.cs
```

</details>

---

### Phase 3: Testing packages and benchmark doubles (pull requests 9 to 11)

<details>
<summary>Tasks</summary>

1. PR 9, `Encina.Testing.Fakes`: `FakeScheduledMessage.TenantId`; `FakeScheduledMessageStore` implements the two members in memory with the same predicates; unit tests, PublicAPI, manifest.
2. PR 10: `ScheduledMessageFaker` (Bogus, class at line 181) and `ArbitraryMessages` (FsCheck, line 132) gain the property with a generator that includes `default`, case-only variants and null; PublicAPI x2 and manifests x2.
3. PR 11: the two `BenchmarkEntityFactory` classes (`tests/Encina.BenchmarkTests/Encina.ADO.Benchmarks` and `Encina.Dapper.Benchmarks`), `Infrastructure/BenchmarkEntityFactory.cs` line 301 in each, gain the property (they build against the old interface). The benchmark DDL builders `tests/Encina.BenchmarkTests/Encina.ADO.Benchmarks/Infrastructure/AdoSchemaBuilder.cs` and `tests/Encina.BenchmarkTests/Encina.Dapper.Benchmarks/Infrastructure/DapperSchemaBuilder.cs` (each with the SQL Server, PostgreSQL and MySQL `CREATE TABLE ScheduledMessages` statements at lines 236, 251 and 266) gain the column and the index in the same pull request; benchmarks are not part of the test run, so the gap between pull requests 1 to 6 and 11 breaks no gate. PR 11 is 4 files (the two factories and the two builders).

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```
CONTEXT:
- The 13 IScheduledMessage implementers are listed in the plan; the eight provider classes were done in Phases 1-2.

TASK:
Add the TenantId property (and the two store members in the fake) to the remaining implementers.

KEY RULES:
- Test doubles use the same predicates as the real stores; generators cover the reserved id and case-only variants.
- Each pull request builds against the old interface.

REFERENCE FILES:
- src/Encina.Testing.Fakes/Stores/FakeScheduledMessageStore.cs
- src/Encina.Testing.Bogus/ScheduledMessageFaker.cs
- src/Encina.Testing.FsCheck/ArbitraryMessages.cs
```

</details>

---

### Phase 4: Interface, orchestrator and processor (pull requests 12a and 12b)

<details>
<summary>Tasks</summary>

1. **PR 12a**: `IScheduledMessage.TenantId` and the two `IScheduledMessageStore` members; `InstrumentedScheduledMessageStore` pass-throughs with spans carrying the error code only; its unit test; a contract test over the fake and one real store; PublicAPI and manifests.
2. **PR 12b** (needs #1257's first pull request, #346's attribute and #735's pull request A): `SchedulerOrchestrator` takes `ITenantScopeResolver? tenantScopeResolver = null` last (Design Choice 3; the constructor line at `src/Encina.Messaging/PublicAPI.Unshipped.txt:870` is edited in the same pull request, RS0016/RS0017), stamps the tenant in `ScheduleAsync` and `ScheduleRecurringAsync`, and `CancelAsync` uses `CancelForTenantAsync` when tenancy is on; `ScheduledMessageProcessor` adds `.WithTenantId(message.TenantId)` to the dispatch context of #735; `SchedulingTenantLog` (internal static partial, EventIds 2390-2392: 2390 no-tenant row accepted for a `[TenantAgnostic]` type, once per type, Information, type name only; 2391 schedule denied for a missing tenant, Warning, error code only; 2392 dispatch of a row with no tenant, Debug, message id only); tests: `SchedulerOrchestratorTenantTests` (the six-row matrix of Design Choice 4, recurring, cancel of another tenant's id), `ScheduledMessageProcessorTenantTests` (the context carries the row's tenant; a null-tenant row yields no tenant), guard test for the new optional parameter (null accepted and valid), property test (the stored tenant equals the resolved tenant for any non-blank id).
3. The EventIds are inside the registered `MessagingScheduling` (2300-2399); the registry row for 2390-2392 is added in this pull request.
4. PR 12b file list (10 files): `SchedulerOrchestrator.cs`, `ScheduledMessageProcessor.cs`, `SchedulingTenantLog.cs`, the registry row (`docs/engineering/eventid-block-registry.md`), `SchedulerOrchestratorTenantTests.cs`, `ScheduledMessageProcessorTenantTests.cs`, the guard test edit (`SchedulerOrchestratorGuardTests.cs`), the property test, `PublicAPI.Unshipped.txt` of `Encina.Messaging` and the `Encina.Messaging.json` manifest.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```
CONTEXT:
- All implementers have the members (Phases 1-3). #1257, #346 and #735 PR A have merged.

TASK:
Add the interface members, the decorator, the orchestrator stamping and strict cancel, and the processor tenant restore.

KEY RULES:
- Fail closed: tenancy on and no tenant is Left(encina.tenancy.tenant_required) unless the request type is [TenantAgnostic] (explicit and logged).
- The orchestrator never calls SaveChangesAsync; make no atomicity claim.
- New constructor parameter optional and last; no raw tenant id in logs or tags.

REFERENCE FILES:
- src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs
- src/Encina.Messaging/Scheduling/ScheduledMessageProcessor.cs
- src/Encina.OpenTelemetry/MessagingStores/InstrumentedScheduledMessageStore.cs
```

</details>

---

### Phase 5: Cross-cutting integration and tenant isolation tests (pull request 13)

<details>
<summary>Tasks</summary>

1. A shared two-tenant scenario (`tests/Encina.IntegrationTests/Messaging/Scheduling/TenantScheduledMessageScenario.cs`, like `OutboxAtomicityScenario`) run on the 10 providers: tenant A schedules, tenant B sees none of it, B cannot cancel it, the processor dispatches A's message with A's tenant in the context, a tenant named `default` is a real value, and tenancy off stamps the reserved tenant. Per-family classes (ADO.NET, Dapper, EF Core x3 collections, MongoDB). Load test: tenant-filtered query under concurrent schedule (the table of `AGENTS.md` section 9 asks load for multi-tenancy); benchmark: justification `.md` (the strict query is the indexed path of a non-hot loop). PR 13 is 13 files: the scenario, 10 per-provider classes (3 ADO.NET, 3 Dapper, 3 EF Core, 1 MongoDB), the load test and the benchmark `.md`; it adds no source, so no manifest or fixture edit; a fixture edit that turns out to be needed moves to PR 14 to stay at 15 files or fewer.
2. Cross-tenant follow-ups to existing plans: #1222's unique index becomes `(TenantId, ScheduleKey)`; #146 and #1222 lift their wait on this plan; #152 and #159 add their tenant dimension. These are edits in their pull requests, listed in their plans.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```
CONTEXT:
- Orchestrator, processor and the ten stores carry the tenant (Phases 1-4).

TASK:
Write the shared two-tenant scenario and its per-family classes.

KEY RULES:
- Shared [Collection] fixtures only; ClearAllDataAsync in InitializeAsync; never IClassFixture or a per-class container.
- Assertions through Shouldly; cover the reserved id and a case-only tenant.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/Scheduling/SchedulerCycleScenarios.cs
- the OutboxAtomicityScenario referenced by the plan of #720
```

</details>

---

### Phase 6: Observability and logging

<details>
<summary>Tasks</summary>

1. The three log events of Phase 4 are the observability of this plan; the tenant tag on spans and instruments is #152's last pull request (`TenantTelemetry.ValueFor`), so no instrument is added here and no raw tenant id enters a log or tag (tenant ids appear only as a span attribute when tenancy is on, ADR-054 D4).
2. `InstrumentedScheduledMessageStore` spans for the two new members, named by the convention of #152 (`encina.scheduling.store.get_pending_by_tenant`, `.cancel_for_tenant`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```
CONTEXT:
- Logging and decorator spans are created in Phase 4; this phase only checks the rules.

TASK:
Verify that no log, tag or status carries a tenant id or EncinaError.Message, and add the decorator span names.

KEY RULES:
- Error code only; tenant ids never in metric tags except through TenantTelemetry when tenancy is on.

REFERENCE FILES:
- src/Encina/Diagnostics/TenantTelemetry.cs
- src/Encina.Messaging/Diagnostics/SchedulingStoreLog.cs
```

</details>

---

### Phase 7: Per-file coverage targets (entered in the pull request that carries the tests)

<details>
<summary>Tasks</summary>

| File | PR | Flag and target | Justification |
| --- | --- | --- | --- |
| `ScheduledMessageStoreADO.cs` of the three ADO.NET packages | 1 to 3 | integration 90, guard 10 (the current per-file targets, not lowered) | Real-database tests cover every predicate, the case-only tenant and the strict cancel. |
| `ScheduledMessageStoreDapper.cs` of the three Dapper packages | 4 to 6 | integration 90, guard 9 (current, not lowered) | Same real-database cases. |
| `ScheduledMessageStoreEF.cs` | 7 | integration 90 (the file has no per-file target; the package default is 25, raised here) | Three database collections cover the two members. |
| `ScheduledMessageStoreMongoDB.cs` | 8 | unit 92, guard 10, integration 95 (current, not lowered) | The MongoDB fixture covers both filters and the index. |
| each of the 8 `ScheduledMessage.cs` | 1 to 8 | unit 30 and guard 10 (the package defaults, unchanged; 40 and 10 for EF Core, 30 and 10 for MongoDB) | The property bag gains one auto-property with no branch; the stores' tests construct it. |
| `Scheduling/SchedulerOrchestrator.cs` | 12b | unit 90, guard 30, integration 65 (current, not lowered) | The six-row matrix and the cancel paths are unit tests with a substitute store and resolver. |
| `Scheduling/ScheduledMessageProcessor.cs` | 12b | unit 70, guard 20 (the package defaults, not lowered) | The context assertion drives the real processor loop with `FakeTimeProvider`. |
| `Diagnostics/SchedulingTenantLog.cs` (new) | 12b | unit 0, guard 0 | `[LoggerMessage]` declarations with no parameters to guard and no executable body in the file. |
| `FakeScheduledMessageStore.cs` | 9 | unit 95 (the package default is 60, raised), guard 15 | The fake mirrors the predicates and is tested with the same cases. |
| `InstrumentedScheduledMessageStore.cs` | 12a | unit 76, guard 71 (current, not lowered) | The pass-through and span are unit-tested. |

Measure with `dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage" --results-directory artifacts\coverage\<Flag>Tests` and `dotnet run --file .github/scripts/coverage-report.cs -- --input artifacts/coverage --output artifacts/coverage-report`. CRAP: keep each store member at comp 10 or less; the orchestrator's tenant step is one private method.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```
CONTEXT:
- Targets live in .github/coverage-manifest/{Package}.json and never go down.

TASK:
Enter the per-file targets of the table with one-sentence justifications.

KEY RULES:
- Read the manifest and write numbers; tests execute real code.

REFERENCE FILES:
- .github/coverage-manifest/Encina.Messaging.json
- docs/testing/coverage-measurement-methodology.md
```

</details>

---

### Phase 8: Documentation and finalization (pull request 14)

<details>
<summary>Tasks</summary>

1. Through `docs-writer`: the scheduling guide section (tenant stamping, the fail-closed rule, `[TenantAgnostic]`, strict query and cancel, the dispatch tenant), the schema documentation of the ten providers; through `mechanical-fixer`: `changelog.d/739-scheduled-message-tenant-id.added.md` and PublicAPI lines.
2. Build verification (`dotnet build --configuration Release`, zero warnings) and the tests of every flag.

</details>

<details>
<summary>Prompt for AI Agents — Phase 8</summary>

```
CONTEXT:
- Documentation pages go through docs-writer; fragments and PublicAPI through mechanical-fixer.

TASK:
Finish documentation, fragment and verification.

KEY RULES:
- Never type coverage figures by hand; no private client application is named.

REFERENCE FILES:
- changelog.d/README.md
- .claude/skills/encina-docs/SKILL.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Requirement | Key Details |
|----------|-------------|-------------|
| SPEC-002 REQ-061, AC-043, S11 | Tenant on every capability SPEC-002 changes; queries filter by tenant | Phases 1 to 5 |
| SPEC-002 DEC-006, `AGENTS.md` section 3 | Gates fail closed; opt-out explicit and logged | Design Choice 4 |
| ADR-054 D4 | Tenant tag only when tenancy is on; no baggage | Tag owned by #152 |
| ADR-062 | `ErrorCategory` per error code | No new code; `tenant_required` is `InvalidRequest`, `tenant_mismatch` is `Forbidden` |
| `AGENTS.md` section 5 | Database features on all 10 providers | Phases 1 and 2 |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `IRequestContext.TenantId`, `WithTenantId` | `src/Encina/Abstractions/IRequestContext.cs` | The ambient tenant and the dispatch context |
| `ITenantScopeResolver`, `TenantScope` (#1257) | core | Write-time stamping and fail-closed rule |
| `[TenantAgnostic]` (#346) | core | Explicit opt-out |
| `ScheduledMessageStore*` (8) | provider `Scheduling/` folders | Receive the members |
| `DeadLetterMessageConfiguration` | `src/Encina.EntityFrameworkCore/` | Binary collation precedent |
| `MongoDbIndexCreator.CreateSchedulingIndexesAsync` | `src/Encina.MongoDB/` | Tenant index |
| `ScheduledMessageProcessor` and the dispatch context of #735 | `src/Encina.Messaging/Scheduling/` | Tenant restore |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` (`MessagingScheduling`, existing 2300-2399) | **2390-2392** | Free on `origin/main` (used 2300-2306 and 2320-2325; the registry holds 2326-2389; #159 takes 2307-2309); registry row added in PR 12b; 2393-2399 stay free |
| New range | none | No block is needed |

### Estimated File Count

| Category | Files | Notes |
|----------|------:|-------|
| Production | 38 | 8 `ScheduledMessage.cs`, 8 stores, 12 SQL scripts counted as 12 (6 packages x 2), EF configuration, Mongo index creator, interfaces 2, orchestrator, processor, log, decorator, fake x2 |
| Test infrastructure DDL | 5 | The 3 `TestInfrastructure/Schemas` files, `DapperStoreEnlistmentTests.cs`, and the 2 benchmark schema builders (counted here once, not in the other rows) |
| Tests | 40 | 8 store integration classes plus 2 extra EF databases, 8 guard edits, orchestrator, processor, contract, property, scenario files, fake and decorator tests |
| Coverage manifests | 16 | One per touched package per pull request |
| PublicAPI files | 13 | Per touched package, plus the `SchedulerOrchestrator` constructor line in 12b |
| Documentation and changelog | 6 | Guide, schema docs, fragment and lines |
| **Total touches** | **119** | In 15 pull requests, in order PR 1 to 6: 9, 9, 9, 9, 8, 8; PR 7 to 11: 9, 7, 6, 6, 4; PR 12a: 8; PR 12b: 10; PR 13: 13; PR 14: 4 (every one at most 15) |

---

## Combined AI Agent Prompts

<details>
<summary>Full combined prompt for all phases</summary>

```
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no compatibility layers; time from TimeProvider; database features on all 10 providers.
- Issue #739: tenant on the scheduled message, following the plan of #737 (outbox) and #738 (saga).

IMPLEMENTATION OVERVIEW:
PR1-6 ADO.NET and Dapper (column, scripts, inline test schema, concrete members, tests; a Dapper PR merges after the ADO.NET PR of its database); PR7 EF Core; PR8 MongoDB; PR9 Fakes; PR10 Bogus and FsCheck; PR11 benchmark doubles and their DDL builders; PR12a interfaces and decorator; PR12b orchestrator, processor, log; PR13 two-tenant scenario; PR14 docs.

KEY PATTERNS:
- Implementers first, interface last; optional parameters last (CS1737); the factory signature does not change.
- Fail closed on a missing tenant; [TenantAgnostic] is the explicit logged opt-out; strict queries and cancel by tenant.
- Binary collation for tenant ids; no raw tenant id in logs or tags; error codes only.

REFERENCE FILES:
- src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs
- src/Encina.ADO.SqlServer/Scheduling/ScheduledMessageStoreADO.cs
- src/Encina.EntityFrameworkCore/Scheduling/ScheduledMessageStoreEF.cs
- src/Encina.MongoDB/Scheduling/ScheduledMessageStoreMongoDB.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Rows are read by the processor and the strict query; no cache is planned |
| 2 | OpenTelemetry | ⏭️ | The tenant tag is #152's last pull request; decorator spans for the new members are in Phase 6 |
| 3 | Structured Logging | ✅ | Phase 4: events 2390-2392, no tenant id in any message |
| 4 | Health Checks | ❌ | `SchedulingHealthCheck` stays cross-tenant (a platform view) |
| 5 | Validation | ✅ | Strict members reject null and blank tenants; resolver validates the id |
| 6 | Resilience | ❌ | Local database operations |
| 7 | Distributed Locks | ⏭️ | Tenant-scoped locks are #741; the processor cycle lock is #716 |
| 8 | Transactions | ❌ | The tenant is written by the same `AddAsync` call; the orchestrator does not commit and no atomicity claim is made |
| 9 | Idempotency | ❌ | The dispatch key of #735 is tenant-free by its own choice; consumers compose the tenant |
| 10 | Multi-Tenancy | ✅ | This is the feature: Phases 1 to 5 |
| 11 | Module Isolation | ⏭️ | The module column is #747, on the same row; it follows this plan's order rule |
| 12 | Audit Trail | ⏭️ | Audit of background processing is #749 |

---

## Prerequisites & Dependencies

| Item | Relation | Note |
|------|----------|------|
| #1257 | Hard for PR 12b | `ITenantScopeResolver`, the default-tenant rule, `tenant_required` |
| #346 | Hard for PR 12b | `[TenantAgnostic]` |
| #735 | Hard for PR 12b | The dispatch context built in the processor |
| #737, #738, #760 | Siblings | Same column shape; whichever interface-changing pull request merges second adds its member to the other's implementers |
| #1222, #146 | Wait for this plan | Schedule key index and public cancel and reschedule |
| #747, #1254 | Siblings | Module and partition columns on the same row |
| #1164 | Sibling | User and correlation restore; drops the tenant from its envelope (D5) |
| #152, #159 | Later | Tenant tag, tenant-scoped statistics |

---

## Next Steps

1. Review this plan; the Dilemmas (choices 1, 2, 3, 6, 7, 8 and 9: tenancy-off value, the strict query, the write-time source, strict cancel, the dispatch tenant, the build order and the cancel of a `[TenantAgnostic]` row) are answered by the maintainer when work starts; the answers are then recorded in a `## Maintainer Decisions` section added at that time.
2. Implement pull requests 1 to 3 (ADO.NET), then 4 to 6 (Dapper, each after the ADO.NET pull request of its database), then 7 to 11 in any order, then 12a, 12b (after #1257, #346 and #735), 13 and 14.
3. Link the plan from #739.
