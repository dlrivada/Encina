# Implementation Plan: `TenantId` on `OutboxMessage` — Tenant-Scoped Outbox Rows and Queries

> **Issue**: [#737](https://github.com/dlrivada/Encina/issues/737)
> **Type**: Feature (SPEC-002 REQ-061, AC-043; delivered with #1164, tracked by #1186)
> **Complexity**: High (7 phases, 10 database providers, 15 pull requests, about 110 file touches)
> **Estimated Scope**: ~700 lines of production code + ~3,500 lines of tests, SQL scripts, manifests and documentation
> **Related plans** (cited by issue number, not by link): #738 (the saga row, the template of this split), #760 (the inbox row, the sibling of this plan), #747 (module column on the same rows), #1254 (partition key on the same row), #1257 (the shared tenant-scope primitive and the default-tenant rule), #346 (the tenant gate and `[TenantAgnostic]`), #2676 (tenant telemetry helper)

---

## Summary

Give the outbox row a tenant: `IOutboxMessage.TenantId`, stamped when `OutboxOrchestrator.AddAsync` creates the row, persisted by all 10 database providers, readable through a strict tenant-filtered query, and fail-closed when a multi-tenant application writes a message with no tenant. Multi-tenancy is required in 1.0 for every capability SPEC-002 adds or changes (REQ-061, AC-043, reference scenario S11); #737, #739 and #760 are delivered with #1164 (SPEC-002 P-50), and #738 adds the tenant to the saga row.

**State of `origin/main` (`f484cdf4f`), verified by reading and grep (corrects the issue text).**

- The issue says "13 database providers". The matrix is 10 (`AGENTS.md` section 5): ADO.NET x3, Dapper x3, EF Core x3 (one `OutboxStoreEF` serves SQL Server, PostgreSQL and MySQL) and MongoDB. It also says "from `ITenantContext`"; that type does not exist. The ambient tenant is `IRequestContext.TenantId` (`src/Encina/Abstractions/IRequestContext.cs`), read through `IRequestContextAccessor`.
- `IOutboxMessage` (`src/Encina.Messaging/Outbox/IOutboxMessage.cs`) has `Id`, `NotificationType`, `Content`, `CreatedAtUtc`, `ProcessedAtUtc`, `ErrorMessage`, `RetryCount`, `NextRetryAtUtc` and two computed members; there is no tenant. `IOutboxStore` has nine members (`AddAsync`, `GetPendingMessagesAsync(batchSize, maxRetries)`, `MarkAsProcessedAsync`, `MarkAsFailedAsync`, `MarkAsExhaustedAsync`, `GetPendingCountAsync`, `GetExhaustedCountAsync`, `RequeueExhaustedAsync`, `SaveChangesAsync`), none filtered by anything but the retry state.
- Rows are created in one place: `OutboxOrchestrator.AddAsync` calls `IOutboxMessageFactory.Create(id, notificationType, content, createdAtUtc)` (`OutboxOrchestrator.cs` line 103; the factory interface is declared at line 262 of the same file) and `IOutboxStore.AddAsync`. The orchestrator is registered scoped (`MessagingServiceCollectionExtensions.cs` line 283) and its constructor ends with the optional parameters `TimeProvider? timeProvider = null, DeadLetter.DeadLetterSourceCapture? deadLetterCapture = null`.
- Implementers (grep over `src` and `tests` of class or record declarations): `IOutboxMessage` has 15 (8 provider classes, `FakeOutboxMessage` (`Encina.Testing.Fakes`), the `FakeOutboxMessage` class declared in `OutboxMessageFaker.cs` (`Encina.Testing.Bogus`), `ArbitraryOutboxMessage`, two `BenchmarkOutboxMessage`, `PendingMessage` in `RetryAfterCeilingProperties.cs`, `TestOutboxMessage` in `OutboxOrchestratorTests.cs`); `IOutboxStore` has 10 (8 provider stores, `InstrumentedOutboxStore`, `FakeOutboxStore`); `IOutboxMessageFactory` has 12 (8 provider factories, `FakeOutboxMessageFactory`, three `FixedIdOutboxMessageFactory` test doubles); its signature does not change in this plan.
- The tenant precedents in the same package: `DeadLetterOrchestrator` and `DeadLetterManager` read the ambient tenant (`AmbientTenantId()`) and the dead letter tables already carry `TenantId VARCHAR(128) NULL` (`src/Encina.ADO.MySQL/Scripts/000_CreateAllTables.sql` line 173, `COLLATE utf8mb4_bin`), whose EF configuration takes an explicit binary collation (`DeadLetterMessageConfiguration`, parameter `filterColumnCollation`), so a tenant id compares case-sensitively on SQL Server and MySQL. `InboxMetadata.TenantId` is the inbox's JSON-only tenant (#760).
- The sibling plans edit the same interfaces, scripts and stores: #1254 adds `IOutboxMessage.PartitionKey`, #747 adds `IOutboxMessage.ModuleId`, #1251 and #713 touch row claiming, #2385 the publisher. This plan follows their order rule (implementers first, interface last): whichever interface-changing pull request merges second adds its member to the other's implementers, and the 15 and 10 implementer lists are re-grepped when work starts.
- `OutboxBatchProcessor` (`src/Encina.Messaging/Outbox/OutboxBatchProcessor.cs`) reads rows of every tenant and passes `tenantId: null` to `RetryMetrics.RecordSkipped` (the only tenant-shaped call of the processor); restoring the tenant when a stored row is dispatched belongs to #1164 (open, no plan on `main`).

**Not in scope**: restoring the tenant (and the user and correlation) when a row is dispatched, which is #1164; tenant-prioritized or tenant-fair batching (Design Choice 8, follow-up issue file `artifacts/issues/outbox-tenant-fair-batching.md`); the module column (#747) and the partition key (#1254); a tenant on the scheduled message (#739), the saga (#738) and the inbox (#760).

**Affected packages**: `Encina.Messaging`, `Encina.ADO.SqlServer|PostgreSQL|MySQL`, `Encina.Dapper.SqlServer|PostgreSQL|MySQL`, `Encina.EntityFrameworkCore`, `Encina.MongoDB`, `Encina.OpenTelemetry` (instrumented store), `Encina.Testing.Fakes`, `Encina.Testing.Bogus`, `Encina.Testing.FsCheck`.

**Provider category**: Database (10): ADO.NET x3, Dapper x3, EF Core x3 (one store class), MongoDB.

---

## Design Choices

<details>
<summary><strong>1. The tenant when tenancy is off (Dilemma D1)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `null`: `TenantId` stays null and no filter applies** | The issue's own shape ("nullable TenantId for non-tenant apps"); no change for applications without tenancy | A null row and a request with a tenant is a special case every store must define; SQL treats NULL as distinct in unique indexes |
| **B) The reserved default tenant of #1257 Design Choice 2 (`default`), stamped when tenancy is off** | One uniform rule and the wording of SPEC-002 AC-043 ("with tenancy off, a fixed default tenant applies"); plain `TenantId = @TenantId` filters | Turning tenancy on later keeps `default` on old rows; shipped NULL rows need an alignment issue |

### Chosen Option: **B — the reserved default tenant, whatever #1257 decides, read from `TenantScope.TenantId`** (recommended, pending the maintainer)

### Rationale

- B wins when the platform-wide rule of #1257 (Design Choice 2) and #738 (Design Choice 3) is B: one answer for every row; A wins if that decision keeps null. This plan never hard-codes `null` or `"default"`: the orchestrator writes `scope.TenantId` from `ITenantScopeResolver.ResolveWrite`, so the answer is made once, in #1257, and the column stays nullable in both cases (`NVARCHAR/VARCHAR(128) NULL`; only the stamped value differs).
- Store predicate per outcome: under B plain `TenantId = @TenantId`; under A a null-tenant row is read through the existing unfiltered member, and `GetPendingByTenantAsync` always takes a non-blank tenant. No predicate ever lets a null tenant match every row.
- The reserved id is injective with the tenant-qualified identifiers of this plan: a tenant id is compared ordinally (binary collation, Design Choice 5) and `default` is rejected as a real tenant by #1257's resolver.

</details>

<details>
<summary><strong>2. The tenant-filtered query (Dilemma D2)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A new strict member `Task<Either<EncinaError, IEnumerable<IOutboxMessage>>> GetPendingByTenantAsync(string tenantId, int batchSize, int maxRetries, CancellationToken cancellationToken = default)`; a null tenant is `ArgumentNullException`, a blank or padded one `ArgumentException` (as `GetPendingMessagesAsync` throws for a bad batch size); the existing member stays the system-scope read of every tenant** | Fail closed by shape: a lost tenant can never widen a query; additive, so each provider pull request builds alone | One interface member on 10 implementers |
| **B) An optional `string? tenantId = null` parameter on `GetPendingMessagesAsync`** | No new member | Null would mean "all tenants" (fail open); the processor's own call would be the null case |
| **C) No query; callers filter in memory** | No API growth | Loads other tenants' rows, contradicting AC-043 |

### Chosen Option: **A — one strict, additive member** (recommended, pending the maintainer)

### Rationale

- A wins when strictness must be structural (SPEC-002 AC-043: queries filter by the tenant and tenant A never reads tenant B's rows); B wins if the only goal is fewer interface members. The predicate extends the existing pending predicate with `AND TenantId = @TenantId` (SQL Server `TOP (@BatchSize)`, PostgreSQL and MySQL `LIMIT @BatchSize`), the EF form is `m.TenantId == tenantId` and the MongoDB form `Builders<OutboxMessage>.Filter.Eq(m => m.TenantId, tenantId)`.
- `GetPendingCountAsync` and the exhausted counters keep their global meaning (a platform view); a per-tenant count is not planned: nothing consumes it.

</details>

<details>
<summary><strong>3. Where the tenant comes from at write time (Dilemma D3)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ITenantScopeResolver.ResolveWrite(null, allowTenantless)` of #1257 (an optional constructor parameter of `OutboxOrchestrator`, after `deadLetterCapture`), and the orchestrator sets `message.TenantId` after `IOutboxMessageFactory.Create(...)` and before `IOutboxStore.AddAsync`** | One fail-closed rule shared with the other SPEC-002 stores; the factory interface and its 12 implementers do not change; the optional parameter keeps the constructor order valid (optional parameters last) | Depends on #1257's PR A1 |
| **B) `IRequestContextAccessor` read directly in the orchestrator, as `DeadLetterOrchestrator.AmbientTenantId()` does** | No dependency on #1257 | A fourth copy of the tenant rule (the drift #1257 describes) |
| **C) A `tenantId` parameter on `AddAsync` or on `IOutboxMessageFactory.Create`** | No ambient state | Breaks every caller and 12 factory implementers; contradicts "auto-populated" |

### Chosen Option: **A — the shared resolver, stamping after `Create`** (recommended, pending the maintainer)

### Rationale

- A wins because five SPEC-002 consumers already share the one signature (#798, #845, #752, the key stores); B wins only if #1257's Design Choice 1 is not accepted, in which case the same stamping uses the accessor and the matrix of Design Choice 4 is unchanged. C is rejected by the issue's own acceptance criterion.
- The orchestrator never calls `SaveChangesAsync` (the caller commits), so the tenant is written with the row by the same `IOutboxStore.AddAsync` call; no atomicity claim is made.

</details>

<details>
<summary><strong>4. A multi-tenant application writes a message with no tenant</strong></summary>

**Class:** Settled
**Settled by:** `AGENTS.md` section 3 (compliance and security gates fail closed when context is missing; the only opt-out is explicit and logged), SPEC-002 DEC-006 and AC-043, and the maintainer principle of 2026-10-30 recorded in #3111; the explicit opt-out is the `[TenantAgnostic]` attribute that #346 (Design Choice 2, Settled) defines.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `AddAsync` returns `Left` with the resolver's `encina.tenancy.tenant_required` (ADR-062 category `InvalidRequest`) when tenancy is on and no tenant is resolved; a notification type marked `[TenantAgnostic]` is the explicit, logged opt-out and is stored with a no-tenant row (`TenantScope(null, true)`)** | A forgotten tenant can only deny, never store an unowned row; platform events stay possible | A background producer with no restored tenant fails (it must set the tenant, see #1164) |
| **B) Store a null-tenant row and continue** | Never blocks | Fail open: the row is invisible to every tenant query and cannot be isolated |

### Chosen Option: **A — Fail closed, `[TenantAgnostic]` as the explicit opt-out**

### Rationale

- Only A complies with the fail-closed rule. The decision matrix is one unit-test row per line (the resolver's own table, applied to the outbox):

  | Tenancy on | Ambient tenant | Notification `[TenantAgnostic]` | Result |
  |---|---|---|---|
  | yes | absent | no | `Left(encina.tenancy.tenant_required)`, nothing stored |
  | yes | T | no | stored with `TenantId = T` |
  | yes | absent | yes | stored with a no-tenant row, logged once per type (EventId 2008) |
  | yes | T | yes | stored with `TenantId = T` (the attribute only lifts the requirement) |
  | no | absent | any | stored with `scope.TenantId` (null under D1 A, `default` under D1 B) |
  | no | T | any | stored with `TenantId = T` |

- No new error code is created: the `Left` values are #1257's `encina.tenancy.tenant_required` (`InvalidRequest`) and `encina.tenancy.tenant_mismatch` (`Forbidden`). The attribute is created by #346's PR A; the worker of PR 12b confirms its `AttributeUsage` accepts notification classes and widens it there if it does not.

</details>

<details>
<summary><strong>5. Column shape, collation and index</strong></summary>

**Class:** Trivial
**Settled by:** the dead letter tables (`TenantId VARCHAR(128) NULL`, `src/Encina.ADO.MySQL/Scripts/000_CreateAllTables.sql` line 173; `DeadLetterMessageConfiguration` with the binary collation constants) and the 128-character tenant id limit of #1257.

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `TenantId NVARCHAR(128) NULL` (SQL Server, binary collation `Latin1_General_100_BIN2`), `VARCHAR(128) NULL` (PostgreSQL), `VARCHAR(128) COLLATE utf8mb4_bin NULL` (MySQL), a string field in MongoDB; an index `(TenantId, ProcessedAtUtc, NextRetryAtUtc)`** | Matches the neighbouring tables; tenant ids compare case-sensitively on every provider, so two tenants that differ only by case never alias; the index serves the strict pending query | One more index per table |
| **B) A separate tenant table with a foreign key** | Normalized | No other messaging table does it; a join on the hot pending path |

### Chosen Option: **A — nullable binary-collated string with a composite index**

### Rationale

- The pending path filters `ProcessedAtUtc IS NULL` first, so the tenant leads the index only for the tenant query; the existing `IX_OutboxMessages_ProcessedAt_RetryCount` (SQL Server) and `IX_OutboxMessages_Processing` (EF Core) stay.
- No migration helper: the project is pre-1.0 and the scripts are `CREATE TABLE` scripts (`AGENTS.md` section 1); `Scripts/000_CreateAllTables.sql` and `Scripts/001_CreateOutboxMessagesTable.sql` of each of the six ADO.NET and Dapper packages are edited in place.

</details>

<details>
<summary><strong>6. The tenant of a dispatched row and the #1164 envelope (Dilemma D4)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The typed `TenantId` column is the single source of the tenant; the #1164 envelope carries `UserId`, `CorrelationId` and `CausationId` only, and the processor restores the tenant from the column** | One value that the strict query, the index and the dispatch all read; the envelope and the column can never disagree | #1164's text lists `TenantId` in the envelope; it must drop it |
| **B) The envelope carries the tenant too and the column is a copy** | The envelope is self-contained | Two values for one fact; a mismatch needs its own rule |

### Chosen Option: **A — the column is the source** (recommended, pending the maintainer)

### Rationale

- A wins when a queryable, indexed tenant must also drive dispatch; B wins if the envelope is to be transported outside the outbox row (the CDC and gRPC paths of #1164, which have no column). A mismatch under B is denied with `encina.tenancy.tenant_mismatch` (`Forbidden`).
- This plan only persists the column; the restoring processor is #1164's.

</details>

<details>
<summary><strong>7. Build order of the pull request split (Dilemma D5)</strong></summary>

**Class:** Dilemma

The change touches an interface with many implementers (the 15 `IOutboxMessage` and 10 `IOutboxStore` implementers of the Summary). A pull request that changes an interface before its implementers does not compile (`AGENTS.md` section 8), and a pull request is one package or provider, at most 15 files and 1,500 lines counting tests and manifests (section 11).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Implementers first, interface last: each provider pull request adds the `TenantId` property and `GetPendingByTenantAsync` to its concrete classes (they compile against the old interface) with its SQL and its integration test; the interface, the decorator and the orchestrator come after every implementer has the members; the orchestrator stamps after `Create`, so the factory is untouched** | Every pull request builds and carries its own tests and manifest entries; no scaffold merges | The members exist on classes before the interfaces have them; 15 pull requests |
| **B) One pull request** | One review | About 110 file touches: over the limits (needs `size-exception-approved`) |
| **C) Default interface members as a scaffold** | The interface goes first | A compatibility layer merges (`AGENTS.md` section 1) and its defaults are untested production code |

### Chosen Option: **A — implementers first, interface last** (recommended, pending the maintainer)

### Rationale

- A wins because the limits are hard gates and a half-wired interface breaks the build between pull requests; the order is the one #738 and #761 recommend. B wins if the maintainer prefers one large review and applies the label; C is rejected only by the rule against compatibility layers, which is the maintainer's to relax.

</details>

<details>
<summary><strong>8. Tenant-prioritized processing (Dilemma D6)</strong></summary>

**Class:** Dilemma

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Not built here; a follow-up issue file records tenant-fair batching (a per-tenant quota inside one batch)** | The issue marks it "optional"; the design needs its own measurement (the pending query ranks by `CreatedAtUtc` across tenants) | A noisy tenant can still delay others until the follow-up lands |
| **B) Build round-robin batching in `OutboxBatchProcessor` now** | Closes the motivation of the issue | One query per tenant or a windowed query on 10 providers, and a rule for tenants that appear after the batch starts: a feature of its own |

### Chosen Option: **A — a follow-up issue** (recommended, pending the maintainer)

### Rationale

- A wins when the tenant column and the strict query must land first (they are what fairness is built on); B wins if a noisy-neighbour outage is already observed in the reference application. The issue file is `artifacts/issues/outbox-tenant-fair-batching.md` (a `[FEATURE]`, written with this plan).

</details>

---

## Implementation Phases

### Phase 1: ADO.NET and Dapper Providers (6)

> **Goal**: The column, the state class and the tenant query in the six packages, one pull request per package (PRs 1 to 6), each carrying its tests and manifest entries.

<details>
<summary>Tasks</summary>

Per package (`Encina.ADO.SqlServer|PostgreSQL|MySQL`, `Encina.Dapper.SqlServer|PostgreSQL|MySQL`):

1. SQL scripts: add `TenantId` and the index `(TenantId, ProcessedAtUtc, NextRetryAtUtc)` in `Scripts/001_CreateOutboxMessagesTable.sql` and `Scripts/000_CreateAllTables.sql` (Design Choice 5; MySQL `COLLATE utf8mb4_bin`, SQL Server `COLLATE Latin1_General_100_BIN2`, PostgreSQL unquoted lower-case or quoted names as the neighbouring columns of each script).
2. `Outbox/OutboxMessage.cs`: `public string? TenantId { get; set; }` (compiles against the old interface).
3. `Outbox/OutboxStoreADO.cs` or `Outbox/OutboxStoreDapper.cs`: add `TenantId` to the `INSERT` column and parameter lists (the pending `SELECT` already uses `*`, so the column is read), and the public method `GetPendingByTenantAsync(string tenantId, int batchSize, int maxRetries, CancellationToken)` with the strict predicate of Design Choice 2 and the existing argument checks (`StoreValidationMessages`); a blank tenant throws `ArgumentException`. Database calls stay asynchronous with a `CancellationToken`. The member is a concrete-class member until PR 12a adds it to `IOutboxStore`.
4. The package's `PublicAPI.Unshipped.txt`, its `.github/coverage-manifest/<Package>.json` entries, a store-level integration test (`[Collection("ADO-<Db>")]` or `[Collection("Dapper-<Db>")]`; `ClearAllDataAsync` in `InitializeAsync`; never `IClassFixture`): tenant round trip, `GetPendingByTenantAsync` isolation between two tenants, a tenant differing only by case is another tenant, and a guard test edit for the new member (null tenant, blank tenant, non-positive batch size, negative `maxRetries`).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```
CONTEXT:
- Issue #737; plan docs/plans/outbox-tenant-id-implementation-plan-737.md. Dilemmas D1-D6 are recommended, pending the maintainer: follow the Maintainer Decisions section if it exists.
- Encina is pre-1.0: no compatibility overloads, no migration helpers. The interfaces are NOT changed yet (D5): this pull request adds TenantId and GetPendingByTenantAsync to ONE package's concrete classes so that it builds against the current interfaces.

TASK:
Apply the Phase 1 tasks to the one package of this pull request; the six implementations must behave identically and differ only in SQL dialect.

KEY RULES:
- Strict predicate: GetPendingByTenantAsync takes a non-blank tenant; no null tenant ever matches every row.
- SQL Server: @param, TOP (@n), NVARCHAR with BIN2 collation; PostgreSQL: LIMIT @n, case-sensitive identifiers; MySQL: LIMIT @n, backticks, utf8mb4_bin on the tenant column.
- Asynchronous database calls with a CancellationToken; never IDbConnection.Open().
- Do not drop existing columns or add migrations; edit both scripts of the package.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs
- src/Encina.Dapper.PostgreSQL/Outbox/OutboxStoreDapper.cs
- src/Encina.ADO.SqlServer/Scripts/001_CreateOutboxMessagesTable.sql
- src/Encina.ADO.MySQL/Scripts/000_CreateAllTables.sql (dead letter TenantId, line 173)
```

</details>

---

### Phase 2: EF Core and MongoDB Providers

> **Goal**: The same behavior in `OutboxStoreEF` (three databases, PR 7) and `OutboxStoreMongoDB` (PR 8).

<details>
<summary>Tasks</summary>

1. `src/Encina.EntityFrameworkCore/Outbox/OutboxMessage.cs`: `TenantId`; `OutboxMessageConfiguration.cs`: `HasMaxLength(128)`, the binary collation passed in explicitly as `DeadLetterMessageConfiguration` does (no model silently gets a case-insensitive default) and `HasIndex(x => new { x.TenantId, x.ProcessedAtUtc, x.NextRetryAtUtc })` named `IX_OutboxMessages_Tenant`; `OutboxStoreEF.cs`: `GetPendingByTenantAsync` as the existing LINQ predicate plus `m.TenantId == tenantId`. No EF migration is added (pre-1.0). Integration tests: one class per database (`EFCore-SqlServer`, `EFCore-PostgreSQL`, `EFCore-MySQL`) and the guard edit.
2. `src/Encina.MongoDB/Outbox/OutboxMessage.cs` and `OutboxStoreMongoDB.cs`: the property and the filter; `MongoDbIndexCreator.CreateOutboxIndexesAsync` (line 113) adds the compound index `IX_Outbox_Tenant`, guarded by `UseOutbox` as the existing indexes; one integration test class on the MongoDB fixture, the guard edit, PublicAPI and manifest entries.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```
CONTEXT:
- Phase 1 pull requests are merged (issue #737); the interfaces are still unchanged (D5).

TASK:
Implement the Phase 2 tasks, one pull request per package (Encina.EntityFrameworkCore, then Encina.MongoDB).

KEY RULES:
- One OutboxStoreEF behavior for SQL Server, PostgreSQL and MySQL, proven by an integration test class on each database.
- The binary collation is passed explicitly per database (SQL Server and MySQL), null on PostgreSQL, as DeadLetterMessageConfiguration requires.
- MongoDB index creation goes through MongoDbIndexCreator, guarded by UseOutbox.

REFERENCE FILES:
- src/Encina.EntityFrameworkCore/Outbox/OutboxMessageConfiguration.cs
- src/Encina.EntityFrameworkCore/DeadLetter/DeadLetterMessageConfiguration.cs
- src/Encina.EntityFrameworkCore/Outbox/OutboxStoreEF.cs
- src/Encina.MongoDB/Outbox/OutboxStoreMongoDB.cs
- src/Encina.MongoDB/MongoDbIndexCreator.cs
```

</details>

---

### Phase 3: Fakes, Bogus, FsCheck and Test Doubles

> **Goal**: The test infrastructure follows the interface; every pull request still builds against the old one.

<details>
<summary>Tasks</summary>

1. PR 9, `src/Encina.Testing.Fakes/Models/FakeOutboxMessage.cs` and `Stores/FakeOutboxStore.cs`: `TenantId` and `GetPendingByTenantAsync` with the strict predicate, plus `PublicAPI.Unshipped.txt`, manifest, unit tests and the guard edit.
2. PR 10, `src/Encina.Testing.Bogus/OutboxMessageFaker.cs` (a `TenantId` rule and `WithTenant(...)`) and `ArbitraryOutboxMessage` in `src/Encina.Testing.FsCheck/ArbitraryMessages.cs`, with the Bogus `PublicAPI.Unshipped.txt` and both manifests.
3. PR 11 (test doubles only): the two `BenchmarkOutboxMessage` classes (`tests/Encina.BenchmarkTests/Encina.ADO.Benchmarks|Encina.Dapper.Benchmarks/Infrastructure/BenchmarkEntityFactory.cs`), `PendingMessage` (`tests/Encina.PropertyTests/Messaging/RetryAfterCeilingProperties.cs`) and `TestOutboxMessage` (`tests/Encina.UnitTests/Messaging/Outbox/OutboxOrchestratorTests.cs`) get the property; they compile against the old interface.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```
CONTEXT:
- Issue #737. The interfaces are NOT changed yet: PRs 9 to 11 add the members to the classes only.

TASK:
Update FakeOutboxMessage, FakeOutboxStore (PR 9), OutboxMessageFaker, ArbitraryOutboxMessage (PR 10) and the four test doubles (PR 11).

KEY RULES:
- The fake store behaves exactly like the real stores for the tenant rules (the contract test of PR 12a runs against it too).
- Do not touch the interfaces and do not call the new members through an IOutboxStore field (that is the decorator, PR 12a).

REFERENCE FILES:
- src/Encina.Testing.Fakes/Stores/FakeOutboxStore.cs
- src/Encina.Testing.Bogus/OutboxMessageFaker.cs
- src/Encina.Testing.FsCheck/ArbitraryMessages.cs
```

</details>

---

### Phase 4: Interface, Decorator and Orchestrator

> **Goal**: The tenant in the contract and the write rule (PRs 12a and 12b, after every implementer has the members).

<details>
<summary>Tasks</summary>

1. PR 12a, `IOutboxMessage`: `string? TenantId { get; set; }`; `IOutboxStore`: `GetPendingByTenantAsync` (Design Choice 2); `src/Encina.OpenTelemetry/MessagingStores/InstrumentedOutboxStore.cs` (an `IOutboxStore` implementer) delegates the new member to `_inner`; PublicAPI of `Encina.Messaging` and `Encina.OpenTelemetry` (`PublicAPI/PublicAPI.Unshipped.txt`), both manifests, the decorator test edit and the new contract test `OutboxStoreTenantContractTests` (an abstract suite run against `FakeOutboxStore` and the provider stores).
2. PR 12b, `OutboxOrchestrator`: the optional constructor parameter `ITenantScopeResolver? tenantScopeResolver = null` after `deadLetterCapture` (CS1737: optional parameters stay last); `AddAsync` resolves the write scope (matrix of Design Choice 4; the `[TenantAgnostic]` lookup is cached per runtime type in a static `ConcurrentDictionary<Type, bool>`), sets `message.TenantId = scope.TenantId` after `Create`, and returns the resolver's `Left` before touching the store; `OutboxBatchProcessor` passes `message.TenantId` instead of `null` to `RetryMetrics.RecordSkipped` (the metric already has a tenant parameter; ADR-054 D4 applies it only when multi-tenancy is on).
3. `OutboxTenantLog` (new, `src/Encina.Messaging/Diagnostics/OutboxTenantLog.cs`, `[LoggerMessage]` source generator), EventIds packed from 2007 inside the registered `MessagingOutbox` range (2000-2099; 2000-2006 are in use; verified free on `main`): 2007 write denied, no tenant (Warning, error code and notification type only), 2008 tenant-agnostic notification stored without a tenant (Information, once per type), 2009 tenant mismatch denied (Warning, error code only). The architecture test already maps `Encina.Messaging` to `MessagingOutbox`; no range registration is needed.
4. Tests of PR 12b: the full matrix of Design Choice 4 (one row per line), "the factory is not called with a tenant", a recording fake store proving `Left` stops before `AddAsync`, guard edits of `OutboxOrchestratorGuardTests` for the new constructor parameter and the DI test that builds the provider with `ValidateOnBuild` and `ValidateScopes` and resolves `OutboxOrchestrator` with and without #1257's resolver.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```
CONTEXT:
- Issue #737; plan docs/plans/outbox-tenant-id-implementation-plan-737.md. All implementers already have TenantId and GetPendingByTenantAsync (PRs 1 to 11). #1257's ITenantScopeResolver and #346's [TenantAgnostic] are merged.

TASK:
PR 12a: add the two members to the interfaces and the decorator. PR 12b: stamp the tenant in OutboxOrchestrator.AddAsync, add OutboxTenantLog and pass the row tenant to RetryMetrics.

KEY RULES:
- Fail closed: a multi-tenant application with no tenant stores nothing and returns Left; [TenantAgnostic] is the only opt-out and is logged once per type.
- Do not change IOutboxMessageFactory; stamp message.TenantId after Create.
- The orchestrator never calls SaveChangesAsync; make no atomicity claim.
- EncinaError.Message is never logged; exceptions through ForLogging(); EventIds 2007-2009 only, packed.
- Update PublicAPI.Unshipped.txt (remove the old OutboxOrchestrator constructor line, add the new one); zero warnings.

REFERENCE FILES:
- src/Encina.Messaging/Outbox/OutboxOrchestrator.cs
- src/Encina.Messaging/Outbox/IOutboxStore.cs
- src/Encina.OpenTelemetry/MessagingStores/InstrumentedOutboxStore.cs
- src/Encina.Messaging/DeadLetter/DeadLetterOrchestrator.cs (AmbientTenantId)
```

</details>

---

### Phase 5: Cross-Cutting Integration and Observability

> **Goal**: The functions marked Include in the matrix.

<details>
<summary>Tasks</summary>

1. OpenTelemetry (in PR 13, after #2676's helper `SetTenantTag`/`WithTenantTag` merges; it is a prerequisite of that pull request): `InstrumentedOutboxStore` sets the attribute `encina.tenant_id` on the activity of the tenant query and the add, and the orchestrator tags its existing outbox metric, only when multi-tenancy is on (ADR-054 D4); the cardinality risk is documented in the observability guide (#906). No tenant value goes into a log: the three log messages of Phase 4 carry error codes and the notification type.
2. Structured logging: `OutboxTenantLog` of Phase 4.
3. Multi-tenancy: the feature itself; the two-tenant test of AC-043 is Phase 6 PR 14.
4. Deferred: restoring the tenant on dispatch (#1164), the module (#747), the audit of outbox operations (no store exists; see the matrix).

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```
CONTEXT:
- Issue #737; cross-cutting matrix of docs/plans/outbox-tenant-id-implementation-plan-737.md.

TASK:
Wire the tenant attribute on the outbox spans and metric through the #2676 helper, and check that every function marked Include is wired and the deferred ones stay out of the diff.

KEY RULES:
- ADR-054 D4: attribute encina.tenant_id and metric tag only when multi-tenancy is on; no baggage.
- No tenant id and no EncinaError.Message in any log.

REFERENCE FILES:
- AGENTS.md section 6
- docs/architecture/adr/054-observability-conventions-and-trace-propagation.md
- src/Encina.OpenTelemetry/MessagingStores/InstrumentedOutboxStore.cs
```

</details>

---

### Phase 6: Testing

> **Goal**: Per-flag coverage at the manifest targets, including real databases for all 10 providers.

<details>
<summary>Tasks</summary>

Tests travel with the pull request that adds the code they cover (see the split); none waits for the end.

1. **Unit** (PRs 9, 12a, 12b): the matrix of Design Choice 4, the fake store, the decorator.
2. **Guard** (PRs 1 to 9, 12b): the new public methods and the new constructor parameter (`GetPendingByTenantAsync`: null tenant, blank tenant, non-positive batch size, negative `maxRetries`; `OutboxOrchestrator`: null `ITenantScopeResolver` is valid, so a test of the valid result, not an exception test).
3. **Contract** (PR 12a): `OutboxStoreTenantContractTests` run against `FakeOutboxStore` and the provider stores.
4. **Property (FsCheck)** (PR 12b): for any pair of tenants, `GetPendingByTenantAsync(A)` never returns a row stamped B.
5. **Integration** (PRs 1 to 8, then PR 13): the store-level tests in each provider pull request; the orchestrator-level two-tenant test of AC-043 on all 10 providers through the shared collections (`ADO-`, `Dapper-` x `SqlServer`/`PostgreSQL`/`MySQL`, `EFCore-` x three, MongoDB) in PR 13, with `ClearAllDataAsync` in `InitializeAsync`.
6. **Load** (PR 13): concurrent `AddAsync` for two tenants against the shared fixture (`AGENTS.md` section 9: multi-tenancy implements load tests).
7. **Benchmark**: justification `tests/Encina.BenchmarkTests/Outbox/OutboxTenant.md` (one extra column and one predicate on a non-hot path), in PR 13.
8. **Per-file targets** (in each package manifest, one-sentence justifications written there; measured with `dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage"` and `.github/scripts/coverage-report.cs`). Starting values; the existing targets of touched files are held, never lowered:

   | File group | Unit | Guard | Contract | Property | Integration | Justification |
   |---|---|---|---|---|---|---|
   | `OutboxOrchestrator.cs` (`Encina.Messaging`) | 95 | 85 | 0 | 60 | 85 | Every row of the matrix is testable with a fake store (unit held at 95, guard 85 and integration 85 as today); the property flag proves isolation for any tenant pair; contract has no reachable line here |
   | `OutboxTenantLog.cs` (new) | 90 | 0 | 0 | 0 | 0 | Generated logging methods exercised with a fake logger; guard 0 because the generated partial has no argument checks to reach |
   | `IOutboxMessage.cs`, `IOutboxStore.cs` | 0 | 0 | 0 | 0 | 0 | Interfaces without executable code (the manifest already declares `defaultTests: []`) |
   | Provider stores `OutboxStoreADO`, `OutboxStoreDapper`, `OutboxStoreEF`, `OutboxStoreMongoDB` | 0 | 10 | 80 | 0 | 92 | The SQL, LINQ and Mongo filters are provable only on real databases (`AGENTS.md` section 5; ADO SQL Server integration is 92 today); guard reaches the argument checks, contract the shared behavior |
   | `FakeOutboxStore`, `FakeOutboxMessage`, `OutboxMessageFaker`, `ArbitraryOutboxMessage` | 90 | 90 | 80 | 0 | 0 | Test doubles that must behave like the real stores for the tenant rules |
   | `InstrumentedOutboxStore` (`Encina.OpenTelemetry`) | 90 | 90 | 0 | 0 | 0 | Delegation plus one attribute, fully reachable through a listener test |

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```
CONTEXT:
- Issue #737. Tests travel with the pull request that adds the code they cover; the orchestrator-level two-tenant test is PR 13. AGENTS.md section 9 per-flag coverage model; guard tests for every new public method and constructor parameter (#3798).

TASK:
Write the tests of Phase 6 Tasks 1-7 and fill the manifest targets.

KEY RULES:
- Integration tests on real databases for all 10 providers, never a .md; shared [Collection] fixtures; Shouldly via Encina.Testing.Shouldly.
- Tests execute real code; a test never swallows an exception; no Thread.Sleep.
- A nullable parameter that accepts null gets a test of the valid result in GuardTests.

REFERENCE FILES:
- tests/Encina.UnitTests/Messaging/Outbox/OutboxOrchestratorTests.cs
- tests/Encina.GuardTests/Messaging/Outbox/OutboxOrchestratorGuardTests.cs
- docs/testing/integration-tests.md
- .github/coverage-manifest/Encina.Messaging.json
```

</details>

---

### Phase 7: Documentation & Finalization

<details>
<summary>Tasks</summary>

1. XML docs on every new public member (`<summary>`, `<param>`, `<returns>`, `<remarks>`); `PublicAPI.Unshipped.txt` per touched package.
2. `changelog.d/737-outbox-tenant-id.added.md`; the messaging guide section on tenant-scoped outbox rows (through `docs-writer`); an ADR only if a Dilemma is decided architecturally (ADR number assigned when the ADR pull request opens).
3. `dotnet build --configuration Release` with 0 warnings; `dotnet test` with every flag at its target.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```
CONTEXT:
- Issue #737 is implemented and tested.

TASK:
Write the documentation set of Phase 7 Tasks through the specialists (docs-writer for pages, mechanical-fixer for the changelog fragment and PublicAPI lines).

KEY RULES:
- Coverage figures are cited with covref markers, never typed.
- Public text never names a private client application.

REFERENCE FILES:
- changelog.d/README.md
- src/Encina.Messaging/README.md
```

</details>

---

### Proposed split into pull requests (limit 15 files and 1500 lines each)

Order rule (D5 option A): implementers first, interface last; every pull request builds, carries its own tests, `PublicAPI.Unshipped.txt` and manifest entries. Counts include tests and manifests; each provider row is the message class, the store, two scripts, PublicAPI, manifest, one store-level integration test and one guard edit.

| PR | Content | Approx. files | Approx. lines |
|----|---------|---------------|---------------|
| 1 | `Encina.ADO.SqlServer`: the provider set above | 8 | 430 |
| 2 | `Encina.ADO.PostgreSQL`: same set | 8 | 430 |
| 3 | `Encina.ADO.MySQL`: same set | 8 | 430 |
| 4 | `Encina.Dapper.SqlServer`: same set | 8 | 430 |
| 5 | `Encina.Dapper.PostgreSQL`: same set | 8 | 430 |
| 6 | `Encina.Dapper.MySQL`: same set | 8 | 430 |
| 7 | `Encina.EntityFrameworkCore`: message, configuration, store, PublicAPI, manifest, 3 integration classes, guard edit | 9 | 560 |
| 8 | `Encina.MongoDB`: message, store, index creator, PublicAPI, manifest, integration test, guard edit | 7 | 400 |
| 9 | `Encina.Testing.Fakes`: `FakeOutboxMessage`, `FakeOutboxStore`, PublicAPI, manifest, unit test, guard edit | 6 | 330 |
| 10 | `Encina.Testing.Bogus` and `Encina.Testing.FsCheck`: faker, arbitrary, Bogus PublicAPI, 2 manifests, faker test edit | 6 | 220 |
| 11 | Test doubles only: 2 benchmark states, `PendingMessage`, `TestOutboxMessage` | 4 | 100 |
| 12a | `IOutboxMessage`, `IOutboxStore`, `InstrumentedOutboxStore`, PublicAPI x2, manifests x2, decorator test edit, contract test | 9 | 520 |
| 12b | `OutboxOrchestrator`, `OutboxBatchProcessor`, `OutboxTenantLog`, PublicAPI, manifest, unit test (matrix), property test, DI test, guard edit, orchestrator test edit | 11 | 900 |
| 13 | Orchestrator two-tenant integration test on 10 providers (4 files holding 10 classes), load test, benchmark justification, tenant attribute on the instrumented store and its test (needs #2676) | 8 | 1,000 |
| 14 | Documentation: changelog fragment, messaging guide section, inventory line (through `docs-writer`, `mechanical-fixer`) | 3 | 200 |

A pull request that measures over 15 files or 1,500 lines when it is briefed is split again. PRs 1 to 11 may merge in any order; 12a needs 1 to 11; 12b needs 12a, #1257's PR A1 and #346's PR A; PR 13 needs 12b and #2676's helper.

---

## Research

### Relevant Standards & Specifications

| Reference | What it defines | Use in this feature |
|-----------|-----------------|---------------------|
| SPEC-002 REQ-061, AC-043, scenario S11 | Tenant-aware capabilities, a two-tenant isolation test, a fixed default tenant when tenancy is off | Stamping, strict query, test |
| SPEC-002 DEC-006 | Gates fail closed | Design Choice 4 |
| ADR-054 D4 | Tenant as span attribute and metric tag only when multi-tenancy is on, no baggage | Phase 5 |
| ADR-062 | Error category contract | The `Left` values reuse `tenant_required` (`InvalidRequest`) and `tenant_mismatch` (`Forbidden`) |
| `AGENTS.md` sections 3, 5, 6, 7 | Fail closed, 10 providers, 12 functions, EventIds | Provider table, matrix, ids |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IRequestContext.TenantId`, `IRequestContextAccessor` | `src/Encina/Abstractions/` | Source of the ambient tenant |
| `MultiTenancyMarker` | `src/Encina/Abstractions/MultiTenancyMarker.cs` | The "tenancy is on" signal the resolver reads |
| `ITenantScopeResolver`, `TenantScope`, `TenantCacheKey` | #1257 PR A1 (core, namespace `Encina`) | Write scope and fail-closed rule |
| `[TenantAgnostic]` | #346 PR A (core) | The explicit opt-out |
| `OutboxOrchestrator`, `IOutboxMessageFactory` | `src/Encina.Messaging/Outbox/OutboxOrchestrator.cs` | Stamping after `Create` |
| `DeadLetterMessageConfiguration` (binary collation constants) | `src/Encina.EntityFrameworkCore/DeadLetter/` | Collation pattern |
| `MongoDbIndexCreator.CreateOutboxIndexesAsync` | `src/Encina.MongoDB/MongoDbIndexCreator.cs` | Compound index |
| `InstrumentedOutboxStore`, `FakeOutboxStore` | `src/Encina.OpenTelemetry/MessagingStores/`, `src/Encina.Testing.Fakes/Stores/` | Decorator and double |
| `RetryMetrics.RecordSkipped` | `src/Encina.Messaging/Recoverability/` | Existing tenant parameter, passed `null` today |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` (outbox) | `MessagingOutbox` 2000-2099 | Existing range; 2000-2006 in use; **this plan 2007-2009** (`OutboxTenantLog`); verified free on `origin/main` and absent from the plan-batch registry; no new range |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Messaging core | 6 | Two interfaces, orchestrator, batch processor, log file, PublicAPI |
| ADO.NET x3 and Dapper x3 | 36 | Message, store, 2 scripts, PublicAPI, manifest per package |
| EF Core and MongoDB | 7 | Message, configuration, store, index creator, 2 PublicAPI, 2 manifests (count shown by the split) |
| Fakes, faker, arbitrary, decorator, test doubles | 14 | With PublicAPI and manifests |
| Tests | 35 | Store-level and orchestrator-level integration (20 classes in the split), unit, contract, property, guard, DI, load |
| Documentation | 3 | Fragment, guide, inventory |
| **Total** | **about 110 across 15 pull requests** | The sum of the split rows is the authority |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no [Obsolete], no compatibility layers, no migrations.
- Issue #737: TenantId on the outbox row for all 10 database providers (SPEC-002 REQ-061, AC-043). Plan: docs/plans/outbox-tenant-id-implementation-plan-737.md; Dilemmas D1-D6 are recommended, pending the maintainer.

IMPLEMENTATION OVERVIEW (the order of the 15 pull requests; every one builds):
1. ADO.NET and Dapper packages, one per pull request (Phase 1); EF Core and MongoDB (Phase 2).
2. Fakes, faker, arbitrary, test doubles (Phase 3).
3. Interfaces and decorator (PR 12a), then the orchestrator write rule (PR 12b) (Phase 4).
4. Observability, orchestrator-level tests on real databases, documentation (Phases 5-7).

KEY PATTERNS:
- Tenant from ITenantScopeResolver.ResolveWrite (#1257); stamp after IOutboxMessageFactory.Create; fail closed, [TenantAgnostic] as the only logged opt-out.
- Strict GetPendingByTenantAsync; a null or blank tenant never matches every row.
- Dialects: SQL Server TOP (@n), NVARCHAR BIN2; PostgreSQL and MySQL LIMIT @n; MySQL utf8mb4_bin.
- Async database calls with CancellationToken; TimeProvider for time; no EncinaError.Message in logs; EventIds 2007-2009.
- Tests: Shouldly, shared [Collection] fixtures, per-flag coverage from the manifests, guard tests for new public API.

REFERENCE FILES:
- src/Encina.Messaging/Outbox/OutboxOrchestrator.cs
- src/Encina.Messaging/Outbox/IOutboxStore.cs
- src/Encina.EntityFrameworkCore/Outbox/OutboxStoreEF.cs
- src/Encina.MongoDB/Outbox/OutboxStoreMongoDB.cs
- docs/plans/saga-tenant-implementation-plan-738.md (the sibling template; read on main)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: pending rows need read-your-writes; the pending-count cache of #700 keys on the store and is unaffected |
| 2 | OpenTelemetry | ✅ | Phase 5: attribute `encina.tenant_id` and metric tag only when multi-tenancy is on (ADR-054 D4), through #2676's helper; no baggage |
| 3 | Structured Logging | ✅ | Phase 4: `OutboxTenantLog`, EventIds 2007-2009 in `MessagingOutbox`; error codes and notification type only |
| 4 | Health Checks | ❌ | Not applicable: no new dependency; the existing outbox health check is unchanged |
| 5 | Validation | ✅ | Phase 1 and 4: the resolver bounds the tenant (at most 128 characters, blank is no tenant, padded rejected); the store rejects a blank tenant argument |
| 6 | Resilience | ❌ | Not applicable: no external call is added |
| 7 | Distributed Locks | ❌ | Not applicable: tenant scoping does not change row claiming (#713, #1251) |
| 8 | Transactions | ❌ | Not applicable: the column is written with the row by the same `AddAsync`; the orchestrator never calls `SaveChangesAsync` and commit ownership stays with the caller (ADR-048) |
| 9 | Idempotency | ❌ | Not applicable: the outbox is at-least-once; the inbox sibling is #760 and dispatch idempotency is #1220 |
| 10 | Multi-Tenancy | ✅ | This is the feature |
| 11 | Module Isolation | ⏭️ | Deferred: `ModuleId` on the outbox row is #747 (open, plan on main) |
| 12 | Audit Trail | ⏭️ | Deferred: tenant on audited messaging operations has no store today; the operation audit of messaging is covered by #750 (saga) and #1636 (module) and no outbox audit issue exists, so none is opened here |

---

## Prerequisites & Dependencies

- #1257 PR A1 (`ITenantScopeResolver`, the default-tenant rule) before PR 12b; #346 PR A (`[TenantAgnostic]`) before PR 12b; #2676's helper before PR 13.
- Coordinate the row shape with #747 (module), #1254 (partition key), #1251 and #2385, which edit the same files; the PRs of this plan touch the same lines of the scripts and stores, so the second to merge rebases.
- #1164 restores the tenant on dispatch from the column (Design Choice 6).

---

## Next Steps

1. The maintainer answers D1 to D6 when work on #737 starts; the answers go into `## Maintainer Decisions` and the markers are removed.
2. Brief pull request 1 first; the provider pull requests follow in order, then 12a and 12b.
