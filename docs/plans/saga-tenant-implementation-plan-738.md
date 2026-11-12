# Implementation Plan: `TenantId` on `SagaState` — Tenant-Scoped Saga Execution and Isolation

> **Issue**: [#738](https://github.com/dlrivada/Encina/issues/738)
> **Type**: Feature
> **Complexity**: High (8 phases, 10 database providers, about 90 files in 14 pull requests)
> **Estimated Scope**: ~900 lines of production code + ~2,600 lines of tests

---

## Summary

Add the tenant to the saga: `ISagaState.TenantId`, filled at `SagaOrchestrator.StartAsync` from the ambient `IRequestContext.TenantId`, persisted by all 10 database providers, and enforced fail-closed when a saga is advanced, completed, compensated or failed. A tenant-aware query lists the active sagas of one tenant. Multi-tenancy is required in 1.0 for every capability SPEC-002 adds or changes (REQ-061, AC-043, reference scenario S11); #737, #739 and #760 are delivered with #1164 (SPEC-002 P-50), and #738 adds the tenant to the saga row.

**Today's code** (checked against main 7a0049c07): `ISagaState` (`src/Encina.Messaging/Sagas/ISagaState.cs`) has no tenant; `ISagaStateFactory.Create(...)` (declared in `SagaOrchestrator.cs`) takes no tenant; `ISagaStore` has `GetAsync(sagaId)`, `AddAsync`, `UpdateAsync`, `GetStuckSagasAsync`, `GetExpiredSagasAsync` and `SaveChangesAsync`. The tenant precedent inside the same package is `DeadLetterManager` (`AmbientTenantId()` over `IRequestContextAccessor`, line 100) and `InboxPipelineBehavior` (line 87, `TenantId = context.TenantId`); the dead letter tables already carry `TenantId VARCHAR(128)` (for example `src/Encina.ADO.MySQL/Scripts/000_CreateAllTables.sql`, line 173).

**Not in scope**: audit of saga transitions (#750), module scoping (#747), correlation by external key (#1237), tenant-scoped sagas of tenants created after the fact (there is no migration: pre-1.0).

**Affected packages**: `Encina.Messaging`, `Encina.ADO.SqlServer|PostgreSQL|MySQL`, `Encina.Dapper.SqlServer|PostgreSQL|MySQL`, `Encina.EntityFrameworkCore`, `Encina.MongoDB`, `Encina.OpenTelemetry` (tenant tag), `Encina.Testing.Fakes` and `Encina.Testing.Bogus` (fake store and faker).

**Provider category**: Database (10): ADO.NET x3, Dapper x3, EF Core x3 (one `SagaStoreEF` serves the three databases), MongoDB.

**Shared with the saga family** (separate plans): #720 (compensation transaction) and #1237 (correlation) also change `ISagaStore`, `ISagaState` implementations and the saga SQL scripts. This plan owns the `TenantId` column and the tenant parameter of the store methods; the other plans rebase onto it. Each plan states its own EventId slice inside the existing `MessagingSaga` range 2200-2299 (this plan 2230-2249); no new range is needed.

---

## Design Choices

<details>
<summary><strong>1. Error returned on a tenant mismatch (Dilemma D1)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Return the existing `saga.not_found` code** | Does not reveal that another tenant's saga exists | A wrong-tenant bug is harder to diagnose |
| **B) A new `saga.tenant_mismatch` code in `SagaErrorCodes`** | Explicit diagnosis | Leaks the existence of the saga id to a caller of another tenant |

### Chosen Option: **A — `saga.not_found`** (recommended, pending the maintainer)

### Rationale

- Option A wins in multi-tenant SaaS exposed to untrusted callers; Option B wins in internal deployments and during development.
- The denial is logged with the error code and both tenant ids' presence (never the ids' values in metrics), so the operator can still diagnose it.
- **Class:** Dilemma

</details>

<details>
<summary><strong>2. Where the tenant filter is enforced (Dilemma D2)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) In the orchestrator, after loading the row** | One implementation for all 10 providers; `ISagaStore.GetAsync` keeps its signature | The store still returns another tenant's row to the orchestrator |
| **B) In every store query as well, with a new tenant-aware lookup `GetForTenantAsync(Guid sagaId, string? tenantId, ...)` that the orchestrator's tenant paths use, and the orchestrator check as a second layer** | Defence in depth: other tenants' rows never leave the database | The `ISagaStore` interface gains two members implemented by 9 stores, `FakeSagaStore` and `InstrumentedSagaStore` |

### Chosen Option: **B — Store filter plus orchestrator check** (recommended, pending the maintainer)

### Rationale

- Option B wins under strict isolation requirements (the reference application handles health data in Spain; SPEC-002 AC-043 requires a two-tenant test showing tenant A never reads tenant B's data). Option A wins when the identical behavior across 10 providers is the priority and the orchestrator is the only caller.
- The lookup is a NEW member next to `GetAsync(Guid sagaId, ...)`, not a replacement: the one-argument `GetAsync` stays as the system-scope lookup that the background paths of D4 need (they load a row of any tenant), and being additive it lets each provider pull request build alone (D5). The orchestrator's tenant paths (`AdvanceAsync`, `CompleteAsync`, `StartCompensationAsync`, `CompensateStepAsync`, `FailAsync`) never call the unfiltered `GetAsync`; a unit test with a recording fake store fails if they do.
- **Store predicate** (strict, fails closed): with a non-null tenant, `WHERE SagaId = @SagaId AND TenantId = @TenantId`; with a null tenant, `WHERE SagaId = @SagaId AND TenantId IS NULL` (written `AND ((@TenantId IS NULL AND TenantId IS NULL) OR TenantId = @TenantId)` where one statement serves both). Never `@TenantId IS NULL OR ...`, which would let a caller without a tenant read every tenant's saga. Under D3 option B the tenant is never null, so the predicate is plain `TenantId = @TenantId`.
- **Class:** Dilemma

</details>

<details>
<summary><strong>3. The tenant when tenancy is off (Dilemma D3)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `null`: `string? TenantId` stays null and no filter applies** | The issue's own shape; no behavior change for applications without tenancy | A saga row with null and a request with a tenant is a special case every store must define |
| **B) A fixed default tenant constant applied when no tenant is configured, as SPEC-002 AC-043 words it ("with tenancy off, a fixed default tenant applies")** | One uniform rule; the filter is always `TenantId = @tenant` | A constant that must be defined once and shared with #737/#739/#760 (#1164); existing code has no such constant today |

### Chosen Option: **B — A fixed default tenant** (recommended, pending the maintainer)

### Rationale

- Option B wins when the platform-wide tenancy design (#1164) defines the constant: one rule for every table. Option A wins if #1164 keeps null for "no tenant" and the saga should stay consistent with the dead letter table (`TenantId` null when there is no ambient tenant).
- The column stays nullable in both cases (`VARCHAR(128) NULL`, as the dead letter tables); only the value written differs.
- Store predicate per option (see D2): option B is plain `TenantId = @TenantId`; option A is `TenantId IS NULL` for a null tenant and `TenantId = @TenantId` otherwise. Neither lets a null tenant match every row.
- **Class:** Dilemma

</details>

<details>
<summary><strong>4. Tenant scope of background paths (Dilemma D4)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Background methods (`TimeoutAsync`, stuck and expired processing) act under the tenant of the row they load, and log it** | No ambient tenant needed; nothing becomes unprocessable | A background caller can act on any tenant's saga by design |
| **B) An explicit, logged system scope that background services opt into** | The opt-out is visible in code (`AGENTS.md` section 3) | One more concept; every background service must set it |

### Chosen Option: **A — Act under the row's own tenant** (recommended, pending the maintainer)

### Rationale

- `GetStuckSagasAsync` and `GetExpiredSagasAsync` return rows of all tenants and run without a request context; denying them would stop timeouts. Option A wins because the caller is infrastructure that already holds the row; Option B wins when the maintainer wants every cross-tenant action to be an explicit, auditable opt-in.
- SPEC-002 REQ-061 ("background processing restores the tenant from persisted context and never mixes tenants in one unit of work") and AC-043 ("a background cycle run for tenant A never acts on a record of tenant B") bear on option A, which processes a batch that holds all tenants: each row is processed under its own restored tenant (the ambient tenant is set from the row for the duration of that row) and its own unit of work, never one unit of work across rows of different tenants. Option B satisfies the same requirement with an explicit scope per tenant.
- **Class:** Dilemma

</details>

<details>
<summary><strong>5. Source of the tenant at saga start</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IRequestContextAccessor.RequestContext?.TenantId`, injected into `SagaOrchestrator` (optional constructor parameter)** | The same mechanism as `DeadLetterManager` and `InboxPipelineBehavior` | The orchestrator gains a constructor parameter |
| **B) A new `ITenantContext`** | Matches the wording of the original issue | The type does not exist in `src/`; it would add a parallel mechanism |

### Chosen Option: **A — The request context accessor**

### Rationale

- **Class:** Settled
- **Settled by:** the precedent in the same package: `src/Encina.Messaging/DeadLetter/DeadLetterManager.cs` line 100 and `src/Encina.Messaging/Inbox/InboxPipelineBehavior.cs` line 87; `IRequestContext.TenantId` is declared at `src/Encina/Abstractions/IRequestContext.cs` line 125.

</details>

<details>
<summary><strong>6. Fail closed on advance, complete, compensate, fail</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A saga is advanced only when the ambient tenant equals the saga's tenant; every other combination is denied, using the same "tenancy configured" trigger as `DeadLetterManager` (`_tenancy is not null`, the `MultiTenancyMarker` registered)** | Compliance gates fail closed; the sibling stores agree on the trigger | A caller without a context cannot advance a tenant's saga (the background paths are D4) |
| **B) Allow when the ambient tenant is missing** | Fewer breakages | Violates the rule |

### Chosen Option: **A — Fail closed**

### Rationale

- **Class:** Settled
- **Settled by:** `AGENTS.md` section 3 (compliance and security gates fail closed; the only opt-out is explicit and logged) and SPEC-002 AC-043; option B is the opposite of that rule.
- **Full matrix** (the orchestrator tenant paths; under D3 option B the "null" rows cannot occur, because the ambient tenant is the fixed default when tenancy is off):

  | Tenancy configured | Ambient tenant | Saga tenant | Result |
  |---|---|---|---|
  | yes | absent | any | Denied (`saga.not_found`, logged as tenant required) |
  | yes | T | T | Allowed |
  | yes | T | null | Denied (a null-tenant saga is not visible under tenancy) |
  | yes | T | other | Denied |
  | no | absent | null | Allowed |
  | no | absent | X | Denied (a tenant-owned saga is not visible without a tenant) |
  | no | T | T | Allowed |
  | no | T | null or other | Denied |

  One unit-test row per line. The orchestrator takes `MultiTenancyMarker?` as an optional constructor parameter next to `IRequestContextAccessor?`.

</details>

<details>
<summary><strong>7. Property shape, column and index</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `string? TenantId` on `ISagaState`; column `TenantId VARCHAR(128) NULL` (the type of the dead letter tables); composite index `(TenantId, Status, LastUpdatedAtUtc)`** | Matches existing tenant columns; supports the active-sagas-of-a-tenant query | One more index per table |
| **B) A separate tenant table** | Normalized | Overkill; no other messaging table does it |

### Chosen Option: **A — Nullable string column with a composite index**

### Rationale

- **Class:** Trivial
- **Convention followed:** the neighbouring column in the same package; the type follows `src/Encina.ADO.MySQL/Scripts/000_CreateAllTables.sql` line 173 (`TenantId VARCHAR(128) ... NULL`, with `utf8mb4_bin` collation on MySQL so tenant ids compare case-sensitively). The PostgreSQL identifier case rules and the SQL Server column types follow the neighbouring columns of each script.
- No migration helper: the project is pre-1.0 and the scripts are `CREATE TABLE` scripts (`AGENTS.md` section 1).

</details>

<details>
<summary><strong>8. Tenant-filtered query for active sagas</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `GetActiveByTenantAsync(string tenantId, int batchSize, CancellationToken)` on `ISagaStore`** | Answers "a tenant's active sagas" directly; one method per store | One more method in 10 stores |
| **B) No new query; callers filter in memory** | No API growth | Loads other tenants' rows, contradicting D2 |

### Chosen Option: **A — A dedicated store method**

### Rationale

- **Class:** Trivial (depends on D2: if D2 is answered A, option B, an in-memory filter over the unfiltered rows, is acceptable and this method is dropped)
- **Convention followed:** the issue's own requirement ("tenant-filtered saga queries") and SPEC-002 AC-043 ("queries ... filter by it"); the predicate is the strict one of D2 (a non-null tenant is plain equality).

</details>

<details>
<summary><strong>9. Build order of the pull request split (Dilemma D5)</strong></summary>

The change touches an interface with many implementers: `ISagaState` (10 state classes, `FakeSagaState`, `ArbitrarySagaState` in `src/Encina.Testing.FsCheck/ArbitraryMessages.cs`, the two benchmark `BenchmarkSagaState` classes in `tests/Encina.BenchmarkTests/Encina.ADO.Benchmarks|Encina.Dapper.Benchmarks/Infrastructure/BenchmarkEntityFactory.cs`, `TestSagaState` in `SagaOrchestratorTests.cs`) and `ISagaStore` (9 stores, `FakeSagaStore`, `InstrumentedSagaStore`, and the test doubles `RecordingSagaStore` in `tests/Encina.IntegrationTests/Infrastructure/Sagas/SagaRunnerOutcomeScenarios.cs` and `FailingUpdateStore` in `SagaRunnerTests.cs`). A pull request that changes an interface before its implementers does not compile (`AGENTS.md` section 8), and a pull request is one package or provider, at most 15 files and 1,500 lines counting tests and manifests (section 11).

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Implementers first, interface last: each provider pull request adds the `TenantId` property and the new members `GetForTenantAsync` and `GetActiveByTenantAsync` to its concrete classes (they compile against the old interface), with its SQL and its integration test; the last-but-one pull request adds the members to the interface and the orchestrator logic. The orchestrator stamps the tenant on the state after `ISagaStateFactory.Create(...)` instead of changing the factory signature, so no factory changes** | Every pull request builds and carries its own tests and manifest entries; no scaffold or compatibility code ever merges; the factory interface and its 10 implementations and 8 factory test files are untouched | The new members exist on the concrete classes for a few pull requests before the interface has them; 14 pull requests |
| B) One pull request with the whole change (about 60 files) | One review | Over the limits; needs the `size-exception-approved` label from the maintainer |
| C) Default interface members as a scaffold, removed in the last pull request | The interface is first | A compatibility layer merges, if only for a while (`AGENTS.md` section 1); the default behaviors are untested production code |

### Chosen Option: **A — Implementers first, interface last** (recommended, pending the maintainer)

### Rationale

- Option A wins because every step is green and reviewable and no transitional code merges. Option B wins if the maintainer prefers one large review and applies the label. Option C is rejected only by the rule against compatibility layers, which is the maintainer's to relax.
- **Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Core Contract and Orchestrator

> **Goal**: The tenant in the interfaces and the orchestrator rule (PR 12, after every implementer has the members).

<details>
<summary>Tasks</summary>

This phase is delivered in PR 12 of the split, after every implementer has the members (D5 option A); the provider phases come first in the order of the split.

1. `ISagaState`: add `string? TenantId { get; set; }`.
2. `ISagaStateFactory` is NOT changed (D5): `SagaOrchestrator.StartAsync` calls `Create(...)` as today and then sets `state.TenantId` before `AddAsync`.
3. `ISagaStore` (D2 recommendation): add `GetForTenantAsync(Guid sagaId, string? tenantId, CancellationToken)` and `GetActiveByTenantAsync(string tenantId, int batchSize, CancellationToken)`; `GetAsync(Guid sagaId, CancellationToken)` stays as the system-scope lookup (XML remarks say the orchestrator's tenant paths never call it).
4. `SagaOrchestrator`: optional constructor parameters `IRequestContextAccessor? requestContextAccessor` and `MultiTenancyMarker? tenancy` (the same trigger as `DeadLetterManager`); the ambient tenant is resolved as `DeadLetterManager.AmbientTenantId` does (an empty id is no tenant, edge white space is rejected); `StartAsync` stamps the ambient tenant (D3); `AdvanceAsync`, `CompleteAsync`, `StartCompensationAsync`, `CompensateStepAsync`, `FailAsync` read the row through `GetForTenantAsync` and apply the matrix of Design Choice 6, denying with `saga.not_found` (D1); `TimeoutAsync` and the stuck/expired paths act under the row's own tenant and unit of work (D4) and log it.
5. `SagaErrorCodes`: add `saga.tenant_mismatch` only if D1 is answered B.
6. `[LoggerMessage]` entries in the `MessagingSaga` range, ids packed sequentially from 2230 inside 2230-2249: tenant denied (2230), tenant required and not resolved (2231), background action under the row's tenant (2232); verify with the architecture test that none is used.
7. `PublicAPI.Unshipped.txt` of `Encina.Messaging`: the interface members and the changed `SagaOrchestrator` constructor (remove the old line, add the new one).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```
CONTEXT:
- Issue #738; plan docs/plans/saga-tenant-implementation-plan-738.md. Dilemmas D1-D5 are recommended, pending the maintainer: follow the Maintainer Decisions section if it exists.
- Encina is pre-1.0: no compatibility overloads, no migration helpers. All implementers already have TenantId, GetForTenantAsync and GetActiveByTenantAsync (earlier pull requests of the split).

TASK:
Implement the Phase 1 tasks in src/Encina.Messaging.

KEY RULES:
- A tenant mismatch fails closed and never reveals another tenant's saga when D1 is A; the full matrix of Design Choice 6 is one unit-test row per line.
- The tenant paths never call the unfiltered ISagaStore.GetAsync (a recording fake store test fails if they do).
- Do not change ISagaStateFactory; stamp state.TenantId after Create.
- Time from TimeProvider; EncinaError.Message never logged; exceptions through ForLogging().
- EventIds inside 2230-2249 of the registered MessagingSaga range (2200-2299), sequential.
- Update PublicAPI.Unshipped.txt; zero warnings.

REFERENCE FILES:
- src/Encina.Messaging/Sagas/SagaOrchestrator.cs
- src/Encina.Messaging/Sagas/ISagaStore.cs
- src/Encina.Messaging/DeadLetter/DeadLetterManager.cs (AmbientTenantId, line 100)
- src/Encina.Messaging/Inbox/InboxPipelineBehavior.cs (line 87)
```

</details>

---

### Phase 2: ADO.NET and Dapper Providers (6)

> **Goal**: Column, state class and SQL in the six packages, one pull request per package (PRs 1 to 6), each carrying its integration test and manifest entries.

<details>
<summary>Tasks</summary>

Per package (`Encina.ADO.SqlServer|PostgreSQL|MySQL`, `Encina.Dapper.SqlServer|PostgreSQL|MySQL`):

1. SQL scripts: add `TenantId` and the composite index in `Scripts/003_CreateSagaStatesTable.sql` and `Scripts/000_CreateAllTables.sql` (2 files per package). MySQL uses `VARCHAR(128) COLLATE utf8mb4_bin NULL`, PostgreSQL a quoted-identifier-aware lower-case column as its neighbours.
2. `Sagas/SagaState.cs`: `TenantId` property (compiles against the old interface; the factory is unchanged and stays untouched).
3. `Sagas/SagaStoreADO.cs` or `Sagas/SagaStoreDapper.cs`: include `TenantId` in `INSERT`, `UPDATE` and every `SELECT` column list; add the public method `GetForTenantAsync(Guid sagaId, string? tenantId, CancellationToken)` with the strict predicate of D2 (`AND ((@TenantId IS NULL AND TenantId IS NULL) OR TenantId = @TenantId)`, plain `TenantId = @TenantId` under D3 option B) and `GetActiveByTenantAsync` with the provider's paging syntax (SQL Server `TOP (@n)`, PostgreSQL and MySQL `LIMIT @n`). Both are concrete-class members until PR 12 adds them to `ISagaStore`. All database calls stay asynchronous with a `CancellationToken`.
4. The package's `PublicAPI.Unshipped.txt`, its coverage manifest entries (per-file targets of Phase 7), and the store-level integration test of the package (`[Collection("ADO-<Db>")]` or `[Collection("Dapper-<Db>")]`): tenant round trip, strict `GetForTenantAsync` (a different tenant and a null tenant get `None`), `GetActiveByTenantAsync` isolation between two tenants.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```
CONTEXT:
- Issue #738. The interfaces are NOT changed yet (D5): this pull request adds TenantId and the two tenant members to ONE package's concrete classes so that it builds against the current interfaces.

TASK:
Apply the Phase 2 tasks to the one package of this pull request; the six implementations must stay identical in behavior and differ only in SQL dialect.

KEY RULES:
- Strict predicate of D2: a null tenant never matches every row.
- SQL Server: @param, TOP (@n); PostgreSQL: LIMIT @n, case-sensitive identifiers; MySQL: LIMIT @n, backticks, utf8mb4_bin on the tenant column.
- Asynchronous database calls with a CancellationToken; never IDbConnection.Open().
- Do not drop existing columns or add migrations.

REFERENCE FILES:
- src/Encina.ADO.PostgreSQL/Sagas/SagaStoreADO.cs
- src/Encina.Dapper.SqlServer/Sagas/SagaStoreDapper.cs
- src/Encina.ADO.SqlServer/Scripts/003_CreateSagaStatesTable.sql
- src/Encina.ADO.MySQL/Scripts/000_CreateAllTables.sql (dead letter TenantId, line 173)
```

</details>

---

### Phase 3: EF Core and MongoDB Providers

> **Goal**: The same behavior in `SagaStoreEF` (three databases, PR 7) and `SagaStoreMongoDB` (PR 8), each with its integration tests, PublicAPI and manifest entries.

<details>
<summary>Tasks</summary>

1. `src/Encina.EntityFrameworkCore/Sagas/SagaState.cs`: `TenantId`; `SagaStateConfiguration.cs`: `HasMaxLength(128)` and `HasIndex(x => new { x.TenantId, x.Status, x.LastUpdatedAtUtc })` (beside the existing indexes); `SagaStoreEF.cs`: `GetForTenantAsync` and `GetActiveByTenantAsync` as LINQ predicates with the strict equality of D2. No EF migration is added (pre-1.0). Integration tests: one class per database (`EFCore-SqlServer`, `EFCore-PostgreSQL`, `EFCore-MySQL`).
2. `src/Encina.MongoDB/Sagas/SagaState.cs`, `SagaStoreMongoDB.cs`: the property, the filter and the query; `MongoDbIndexCreator.CreateSagaIndexesAsync` (line 163) adds the compound index. One integration test class on the MongoDB fixture.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```
CONTEXT:
- Phase 2 pull requests are merged (issue #738); the interfaces are still unchanged (D5).

TASK:
Implement the Phase 3 tasks, one pull request per package (Encina.EntityFrameworkCore, then Encina.MongoDB).

KEY RULES:
- One SagaStoreEF behavior for SQL Server, PostgreSQL and MySQL; verified by integration tests on each database.
- Index creation in MongoDB goes through MongoDbIndexCreator, guarded by UseSagas.

REFERENCE FILES:
- src/Encina.EntityFrameworkCore/Sagas/SagaStateConfiguration.cs
- src/Encina.EntityFrameworkCore/Sagas/SagaStoreEF.cs
- src/Encina.MongoDB/Sagas/SagaStoreMongoDB.cs
- src/Encina.MongoDB/MongoDbIndexCreator.cs
```

</details>

---

### Phase 4: Fakes, Bogus and Instrumented Store

> **Goal**: The test infrastructure and the tracing wrapper follow the interface.

<details>
<summary>Tasks</summary>

PRs 9 to 11 of the split (each builds against the old interfaces, D5):

1. PR 9, `src/Encina.Testing.Fakes/Models/FakeSagaState.cs` and `Stores/FakeSagaStore.cs`: `TenantId` and the tenant-aware methods with the strict predicate, plus `PublicAPI.Unshipped.txt`, manifest and unit tests.
2. PR 10, `src/Encina.Testing.Bogus/SagaStateFaker.cs` (`TenantId` rule) and `ArbitrarySagaState` in `src/Encina.Testing.FsCheck/ArbitraryMessages.cs` (the `TenantId` property), with their PublicAPI and manifest entries.
3. PR 11, `src/Encina.OpenTelemetry/MessagingStores/InstrumentedSagaStore.cs`: delegate the new methods and set the span tag `tenant.id` ALWAYS on the activity (when it has listeners) and NEVER on a metric; the two benchmark `BenchmarkSagaState` classes (`tests/Encina.BenchmarkTests/Encina.ADO.Benchmarks|Encina.Dapper.Benchmarks/Infrastructure/BenchmarkEntityFactory.cs`) get the property; the test doubles `RecordingSagaStore` (`tests/Encina.IntegrationTests/Infrastructure/Sagas/SagaRunnerOutcomeScenarios.cs`), `FailingUpdateStore` (`SagaRunnerTests.cs`) and `TestSagaState` (`SagaOrchestratorTests.cs`) get the members.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```
CONTEXT:
- ISagaStore gained a tenant parameter and GetActiveByTenantAsync (issue #738).

TASK:
Update FakeSagaStore, FakeSagaState, SagaStateFaker and InstrumentedSagaStore.

KEY RULES:
- The fake store behaves exactly like the real stores for the tenant rules (contract tests run against it too).
- The tenant is a span tag only (always set when the activity has listeners), never a metric label.
- Each pull request builds against the current interfaces: add the members to the classes, do not touch the interfaces.

REFERENCE FILES:
- src/Encina.Testing.Fakes/Stores/FakeSagaStore.cs
- src/Encina.OpenTelemetry/MessagingStores/InstrumentedSagaStore.cs
- src/Encina.Testing.Bogus/SagaStateFaker.cs
```

</details>

---

### Phase 5: Cross-Cutting Integration

> **Goal**: The functions marked Include in the matrix.

<details>
<summary>Tasks</summary>

1. OpenTelemetry: the tenant tag of Phase 4.
2. Structured logging: the denial message of Phase 1, error code only.
3. Multi-tenancy: the feature itself; the two-tenant test of SPEC-002 AC-043 lives in Phase 7.
4. Deferred: audit with tenant (#750), module scoping (#747), correlation (#1237).

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```
CONTEXT:
- Cross-cutting matrix of docs/plans/saga-tenant-implementation-plan-738.md.

TASK:
Check that every function marked Include is wired and that the deferred ones stay out of the diff.

KEY RULES:
- No metric label carries the tenant; no log carries a payload.

REFERENCE FILES:
- AGENTS.md section 6
- docs/plans/saga-tenant-implementation-plan-738.md
```

</details>

---

### Phase 6: Observability

> **Goal**: Denials are visible without leaking tenants.

<details>
<summary>Tasks</summary>

1. A counter `encina.saga.tenant_denied` (`Counter<long>`) on the existing saga meter source, tag `saga.type` only.
2. The `[LoggerMessage]` entries of Phase 1; run `EncinaEventIdAllocationTests`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```
CONTEXT:
- Issue #738. The saga logs live in the MessagingSaga range 2200-2299; this plan uses 2230-2249.

TASK:
Add the counter and the log messages; run the architecture tests.

KEY RULES:
- [LoggerMessage] source generator; sequential ids; error code only.

REFERENCE FILES:
- src/Encina.Messaging/Diagnostics/SagaActivitySource.cs
- src/Encina.Messaging/Diagnostics/MessagingStoreMetrics.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

### Phase 7: Testing

> **Goal**: Per-flag coverage at the manifest targets, including real databases for all 10 providers.

<details>
<summary>Tasks</summary>

Tests travel with the pull request that adds the code they cover (see the split); none waits for the end.

1. **Unit** (PR 12, plus the fake and instrumented stores in PRs 9 and 11): tenant stamping, the full matrix of Design Choice 6 (one row per line), D1 error, D4 background path, "tenant paths never call the unfiltered `GetAsync`", fake store, instrumented store.
2. **Guard**: new public methods with parameters, in the pull request that adds them.
3. **Contract** (PR 12): `ISagaStore` tenant behavior identical across the real stores (run against `FakeSagaStore` and the provider stores).
4. **Property (FsCheck)** (PR 12): a saga is never advanced by another tenant for any pair of tenants.
5. **Integration**: the store-level tests in each provider pull request (PRs 1 to 8, see Phases 2 and 3); the orchestrator-level two-tenant test of AC-043 on all 10 providers through the shared collections (`ADO-`, `Dapper-` x `SqlServer`/`PostgreSQL`/`MySQL`, `EFCore-` x three, MongoDB) in PR 13; `ClearAllDataAsync` in `InitializeAsync`; never `IClassFixture`.
6. **Load** (PR 13): concurrent starts for two tenants (AGENTS.md section 9 says multi-tenancy is implemented); a justification `.md` only with a reason the plan does not foresee.
7. **Benchmark**: justification `tests/Encina.BenchmarkTests/Sagas/SagaTenant.md` (one extra column and predicate), in PR 13.
8. **Per-file targets** in the manifests of each touched package, each flag with a one-sentence justification written in the manifest; measured with `dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage"` and `.github/scripts/coverage-report.cs`. Starting values:

   | File group | Unit | Guard | Contract | Property | Integration | Justification |
   |---|---|---|---|---|---|---|
   | `SagaOrchestrator` tenant code (`Encina.Messaging`) | 95 | 90 | 80 | 60 | 0 | Every row of the matrix and each denial are testable with fake stores; the property flag proves isolation for any tenant pair; the real stores are covered by their own integration flag |
   | Provider stores and state classes (ADO.NET x3, Dapper x3, EF Core, MongoDB) | 0 | 90 | 80 | 0 | 80 | The SQL, LINQ and Mongo filters can only be proved against real databases (`AGENTS.md` section 5); guard and contract cover the new members |
   | `FakeSagaStore`, `FakeSagaState`, `SagaStateFaker`, `ArbitrarySagaState` | 90 | 90 | 80 | 0 | 0 | Test doubles that must behave like the real stores for the tenant rules; the contract test runs against them |
   | `InstrumentedSagaStore` (`Encina.OpenTelemetry`) | 90 | 90 | 0 | 0 | 0 | Delegation plus one span tag, fully reachable through a listener test |

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```
CONTEXT:
- Issue #738. Tests travel with the pull request that adds the code they cover (see the split); the orchestrator-level two-tenant test is PR 13. AGENTS.md section 9 per-flag coverage model.

TASK:
Write the tests of Phase 7 Tasks 1-7 and fill the manifest targets.

KEY RULES:
- Integration tests on real databases for all 10 providers, never a .md; Shouldly via Encina.Testing.Shouldly.
- Tests execute real code; a test never swallows an exception.

REFERENCE FILES:
- tests/Encina.UnitTests/Messaging/Sagas/SagaOrchestratorTests.cs
- docs/testing/integration-tests.md
- .github/coverage-manifest/Encina.Messaging.json
```

</details>

---

### Phase 8: Documentation & Finalization

<details>
<summary>Tasks</summary>

1. XML docs on every new public member; `PublicAPI.Unshipped.txt` per touched package.
2. `changelog.d/738-saga-tenant-id.added.md`; the saga guide and schema documentation (through docs-writer); an ADR only if a Dilemma is decided architecturally (ADR number assigned when the ADR pull request opens).
3. `dotnet build --configuration Release` with 0 warnings; `dotnet test` with every flag at its target.

</details>

<details>
<summary>Prompt for AI Agents — Phase 8</summary>

```
CONTEXT:
- Issue #738 is implemented and tested.

TASK:
Write the documentation set of Phase 8 Tasks.

KEY RULES:
- Documentation pages go through the docs-writer agent; coverage figures are cited with covref markers, never typed.

REFERENCE FILES:
- changelog.d/README.md
- docs/messaging/sagas.md (if present)
```

</details>

---

### Proposed split into pull requests (limit 15 files and 1500 lines each)

Order rule (D5 option A): implementers first, interface last; every pull request builds, carries its own tests, `PublicAPI.Unshipped.txt` and manifest entries, and is one package (or one test-infrastructure group). Counts include tests and manifests.

| PR | Content | Approx. files | Approx. lines |
|----|---------|---------------|---------------|
| 1 | `Encina.ADO.SqlServer`: `SagaState`, `SagaStoreADO`, 2 scripts, PublicAPI, manifest, store integration test | 7 | 450 |
| 2 | `Encina.ADO.PostgreSQL`: same set | 7 | 450 |
| 3 | `Encina.ADO.MySQL`: same set | 7 | 450 |
| 4 | `Encina.Dapper.SqlServer`: same set | 7 | 450 |
| 5 | `Encina.Dapper.PostgreSQL`: same set | 7 | 450 |
| 6 | `Encina.Dapper.MySQL`: same set | 7 | 450 |
| 7 | `Encina.EntityFrameworkCore`: state, configuration, store, PublicAPI, manifest, 3 integration test classes | 8 | 550 |
| 8 | `Encina.MongoDB`: state, store, index creator, PublicAPI, manifest, integration test | 6 | 400 |
| 9 | `Encina.Testing.Fakes`: `FakeSagaState`, `FakeSagaStore`, PublicAPI, manifest, unit tests | 6 | 400 |
| 10 | `Encina.Testing.Bogus` and `Encina.Testing.FsCheck`: `SagaStateFaker`, `ArbitrarySagaState`, PublicAPI x2, manifests x2 | 6 | 200 |
| 11 | `Encina.OpenTelemetry` `InstrumentedSagaStore` (+ PublicAPI, manifest, unit test), 2 benchmark states, 3 test doubles | 9 | 450 |
| 12 | Interface and orchestrator: `ISagaState`, `ISagaStore`, `SagaOrchestrator`, saga log file, saga metrics, PublicAPI, manifest, unit, contract and property tests | 11 | 1,100 |
| 13 | Orchestrator two-tenant integration test (AC-043) on 10 providers, load test, benchmark justification | 12 | 1,000 |
| 14 | Documentation: changelog fragment, saga guide, schema documentation, ADR if a Dilemma needs one (through docs-writer) | 4 | 300 |

A pull request that measures over 15 files or 1,500 lines when it is briefed is split again.

---

## Research

### Relevant Standards & Specifications

| Reference | What it defines | Use in this feature |
|-----------|-----------------|---------------------|
| SPEC-002 REQ-061, AC-043 | Tenant-aware capabilities, a two-tenant test, a fixed default tenant when tenancy is off | Tenant stamping, filter and test |
| SPEC-002 scenario S11 | Reference multi-tenant scenario | Acceptance |
| `AGENTS.md` section 3 | Gates fail closed; `EncinaError.Message` never logged | Fail-closed rule, logs |
| `AGENTS.md` section 5 | 10 database providers | Provider table |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| `IRequestContext.TenantId` | `src/Encina/Abstractions/IRequestContext.cs` | Source of the tenant |
| `DeadLetterManager.AmbientTenantId` | `src/Encina.Messaging/DeadLetter/DeadLetterManager.cs` | Precedent for resolving and trimming the ambient tenant |
| `InboxPipelineBehavior` | `src/Encina.Messaging/Inbox/InboxPipelineBehavior.cs` | Precedent (line 87) |
| `SagaOrchestrator`, `ISagaStateFactory` | `src/Encina.Messaging/Sagas/SagaOrchestrator.cs` | Tenant stamping (after `Create`, the factory is unchanged) and check |
| `ISagaStore` and 9 store files | `src/Encina.*/Sagas/` | Tenant column and filter |
| `SagaStateConfiguration` | `src/Encina.EntityFrameworkCore/Sagas/SagaStateConfiguration.cs` | Index |
| `MongoDbIndexCreator` | `src/Encina.MongoDB/MongoDbIndexCreator.cs` | Compound index |
| `InstrumentedSagaStore` | `src/Encina.OpenTelemetry/MessagingStores/InstrumentedSagaStore.cs` | Span tag |
| `FakeSagaStore` | `src/Encina.Testing.Fakes/Stores/FakeSagaStore.cs` | Test double |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Messaging` (saga) | 2200-2299 | Existing `MessagingSaga`; `SagaStoreLog` uses 2200-2205 |
| **This plan (#738)** | **2230-2249** | **Tenant denial messages; slice agreed within the saga family: #720 2206-2229, #738 2230-2249, #750 2250-2274, #1237 2275-2299** |

No new range is registered; the architecture test (`EncinaEventIdAllocationTests`) already maps `Encina.Messaging` to `MessagingSaga`.

### Provider Matrix

| Provider | Column + state | Store filter | Integration test |
|----------|----------------|--------------|------------------|
| ADO-SqlServer, ADO-PostgreSQL, ADO-MySQL | Required | Required | `[Collection("ADO-<Db>")]` |
| Dapper-SqlServer, Dapper-PostgreSQL, Dapper-MySQL | Required | Required | `[Collection("Dapper-<Db>")]` |
| EFCore-SqlServer, EFCore-PostgreSQL, EFCore-MySQL | one `SagaStoreEF` | Required | `[Collection("EFCore-<Db>")]` x3 |
| MongoDB | Required | Required | MongoDB fixture |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Messaging core | ~7 | Two interfaces, orchestrator, logs, metrics, PublicAPI, manifest (no factory change) |
| ADO.NET x3 and Dapper x3 | ~30 | State, store, 2 scripts, PublicAPI, manifest per package (the 6 integration tests are counted under tests) |
| EF Core and MongoDB | ~9 | State, configuration, store, PublicAPI, manifest, index creator |
| Fakes, faker, arbitrary, instrumented store, benchmark states, test doubles | ~16 | With their PublicAPI and manifests |
| Tests | ~24 | Unit, contract, property, 10 store-level and 10 orchestrator-level integration classes (counted in the PR rows), load, justification |
| Documentation | ~4 | Changelog fragment, guide, schema docs, ADR if needed |
| **Total** | **~90 across 14 pull requests** | Counts by PR in the split table above; the sum of its rows is the authority |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no [Obsolete], no compatibility layers, no migrations.
- Issue #738: TenantId on the saga row for all 10 database providers (SPEC-002 REQ-061, AC-043). Plan: docs/plans/saga-tenant-implementation-plan-738.md; Dilemmas D1-D5 are recommended, pending the maintainer.

IMPLEMENTATION OVERVIEW (the order of the 14 pull requests; every one builds):
1. ADO.NET and Dapper packages, one per pull request (Phase 2); EF Core and MongoDB (Phase 3).
2. Fakes, faker, arbitrary, instrumented store, benchmark states, test doubles (Phase 4).
3. Interfaces and orchestrator in Encina.Messaging (Phase 1), with cross-cutting and observability (Phases 5-6).
4. Orchestrator-level tests on real databases, then documentation (Phases 7-8).

KEY PATTERNS:
- Tenant from IRequestContextAccessor (as DeadLetterManager); mismatch fails closed; background paths act under the row's tenant when D4 is A.
- Dialects: SQL Server TOP (@n); PostgreSQL and MySQL LIMIT @n; MySQL tenant column utf8mb4_bin.
- Async database calls with CancellationToken; TimeProvider for time; no EncinaError.Message in logs.
- Tests: Shouldly, shared [Collection] fixtures, per-flag coverage from the manifests.

REFERENCE FILES:
- src/Encina.Messaging/Sagas/SagaOrchestrator.cs
- src/Encina.Messaging/Sagas/ISagaStore.cs
- src/Encina.Messaging/DeadLetter/DeadLetterManager.cs
- src/Encina.EntityFrameworkCore/Sagas/SagaStoreEF.cs
- src/Encina.MongoDB/Sagas/SagaStoreMongoDB.cs
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: saga state needs read-your-writes |
| 2 | OpenTelemetry | ✅ | Phase 4 and 6: tenant as a span tag only (always on the activity, never on a metric), a denial counter without tenant label |
| 3 | Structured Logging | ✅ | Phase 1 and 6: denial message in the saga range, error code only |
| 4 | Health Checks | ❌ | Not applicable: no new dependency; the existing `SagaHealthCheck` is unchanged |
| 5 | Validation | ❌ | Not applicable: tenant checking is a security gate, not input validation |
| 6 | Resilience | ❌ | Not applicable: no external call |
| 7 | Distributed Locks | ❌ | Not applicable: tenant scoping does not change locking |
| 8 | Transactions | ❌ | Not applicable: the column is written in the same save as the saga state |
| 9 | Idempotency | ❌ | Not applicable: no interaction with idempotency keys (#736) |
| 10 | Multi-Tenancy | ✅ | This is the feature |
| 11 | Module Isolation | ⏭️ | Deferred: `ModuleId` on messaging entities is #747 |
| 12 | Audit Trail | ⏭️ | Deferred: saga transition audit is #750, which carries the tenant once this lands |

---

## Prerequisites & Dependencies

- #1164 (SPEC-002 P-50) defines the platform tenancy design; the answer to D3 must agree with it.
- Coordinate the schema with #720 and #1237, which also change the saga scripts and stores.

---

## Next Steps

1. The maintainer answers D1 to D5 when work on #738 starts; the answers go into `## Maintainer Decisions` and the markers are removed.
2. Brief pull request 1 first; the provider pull requests follow in order.
