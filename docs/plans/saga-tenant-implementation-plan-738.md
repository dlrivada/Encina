# Implementation Plan: `TenantId` on `SagaState` — Tenant-Scoped Saga Execution and Isolation

> **Issue**: [#738](https://github.com/dlrivada/Encina/issues/738)
> **Type**: Feature
> **Complexity**: High (8 phases, 10 database providers, about 60 files in 6 pull requests)
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
| **B) In every store query as well (`WHERE TenantId = @tenant`), with `GetAsync(Guid sagaId, string? tenantId, ...)` and the orchestrator check as a second layer** | Defence in depth: other tenants' rows never leave the database | The `ISagaStore` signature changes in 10 stores, `FakeSagaStore` and `InstrumentedSagaStore` |

### Chosen Option: **B — Store filter plus orchestrator check** (recommended, pending the maintainer)

### Rationale

- Option B wins under strict isolation requirements (the reference application handles health data in Spain; SPEC-002 AC-043 requires a two-tenant test showing tenant A never reads tenant B's data). Option A wins when the identical behavior across 10 providers is the priority and the orchestrator is the only caller.
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
| **A) A saga with a tenant is advanced only when the ambient tenant equals it; a missing ambient tenant on a tenant-owned saga is denied** | Compliance gates fail closed | A caller without a context cannot advance a tenant's saga (the background paths are D4) |
| **B) Allow when the ambient tenant is missing** | Fewer breakages | Violates the rule |

### Chosen Option: **A — Fail closed**

### Rationale

- **Class:** Settled
- **Settled by:** `AGENTS.md` section 3 (compliance and security gates fail closed; the only opt-out is explicit and logged) and SPEC-002 AC-043.

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
- **Settled by:** `AGENTS.md` section 3 (pay-for-what-you-use; opt-in) and section 4 (descriptive names); the type follows `src/Encina.ADO.MySQL/Scripts/000_CreateAllTables.sql` line 173 (`TenantId VARCHAR(128) ... NULL`, with `utf8mb4_bin` collation on MySQL so tenant ids compare case-sensitively). The PostgreSQL identifier case rules and the SQL Server column types follow the neighbouring columns of each script.
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

- **Class:** Settled
- **Settled by:** the issue's own requirement ("tenant-filtered saga queries") and SPEC-002 AC-043 ("queries ... filter by it").

</details>

---

## Implementation Phases

### Phase 1: Core Contract and Orchestrator

> **Goal**: The tenant in the interface, the factory, the error and the orchestrator rule.

<details>
<summary>Tasks</summary>

1. `ISagaState`: add `string? TenantId { get; set; }`.
2. `ISagaStateFactory.Create(...)`: add `string? tenantId = null` after `timeoutAtUtc` (declared in `src/Encina.Messaging/Sagas/SagaOrchestrator.cs`).
3. `ISagaStore` (D2 recommendation): `GetAsync(Guid sagaId, string? tenantId, CancellationToken)` replaces the one-argument form (no compatibility overload), plus `GetActiveByTenantAsync(string tenantId, int batchSize, CancellationToken)`.
4. `SagaOrchestrator`: optional constructor parameter `IRequestContextAccessor? requestContextAccessor`; `StartAsync` stamps the ambient tenant (D3); `AdvanceAsync`, `CompleteAsync`, `StartCompensationAsync`, `CompensateStepAsync`, `FailAsync` read the row through the tenant-aware `GetAsync` and deny a mismatch with `saga.not_found` (D1); `TimeoutAsync` and the stuck/expired paths act under the row's tenant (D4) and log it.
5. `SagaErrorCodes`: add `saga.tenant_mismatch` only if D1 is answered B.
6. `[LoggerMessage]` entries for the denial in the `MessagingSaga` range, ids 2230-2249 (verify with the architecture test that none is used).
7. `PublicAPI.Unshipped.txt` of `Encina.Messaging`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```
CONTEXT:
- Issue #738; plan docs/plans/saga-tenant-implementation-plan-738.md. Dilemmas D1-D4 are recommended, pending the maintainer: follow the Maintainer Decisions section if it exists.
- Encina is pre-1.0: no compatibility overloads, no migration helpers.

TASK:
Implement the Phase 1 tasks in src/Encina.Messaging.

KEY RULES:
- A tenant mismatch fails closed and never reveals another tenant's saga when D1 is A.
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

> **Goal**: Column, state class, factory and SQL in the six packages.

<details>
<summary>Tasks</summary>

1. SQL scripts: add `TenantId` and the composite index in `Scripts/003_CreateSagaStatesTable.sql` and `Scripts/000_CreateAllTables.sql` of `Encina.ADO.SqlServer|PostgreSQL|MySQL` and `Encina.Dapper.SqlServer|PostgreSQL|MySQL` (12 files). MySQL uses `VARCHAR(128) COLLATE utf8mb4_bin NULL`, PostgreSQL a quoted-identifier-aware lower-case column as its neighbours.
2. `Sagas/SagaState.cs` (6): `TenantId` property.
3. `Sagas/SagaStateFactory.cs` (6): pass `tenantId`.
4. `Sagas/SagaStoreADO.cs` (3) and `Sagas/SagaStoreDapper.cs` (3): include `TenantId` in `INSERT`, `UPDATE` and every `SELECT` column list; `GetAsync(sagaId, tenantId)` adds `AND (@TenantId IS NULL OR TenantId = @TenantId)` per D3; implement `GetActiveByTenantAsync` with the provider's paging syntax (SQL Server `TOP (@n)`, PostgreSQL and MySQL `LIMIT @n`). All database calls stay asynchronous with a `CancellationToken`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```
CONTEXT:
- Phase 1 changed ISagaState, ISagaStateFactory and ISagaStore. This phase updates the 6 ADO/Dapper packages (issue #738).

TASK:
Apply the Phase 2 tasks to every package; the six implementations must stay identical in behavior and differ only in SQL dialect.

KEY RULES:
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

> **Goal**: The same behavior in `SagaStoreEF` (three databases) and `SagaStoreMongoDB`.

<details>
<summary>Tasks</summary>

1. `src/Encina.EntityFrameworkCore/Sagas/SagaState.cs`: `TenantId`; `SagaStateConfiguration.cs`: `HasMaxLength(128)` and `HasIndex(x => new { x.TenantId, x.Status, x.LastUpdatedAtUtc })` (beside the existing indexes); `SagaStateFactory.cs`; `SagaStoreEF.cs`: tenant-aware `GetAsync` and `GetActiveByTenantAsync` as LINQ predicates. No EF migration is added (pre-1.0).
2. `src/Encina.MongoDB/Sagas/SagaState.cs`, `SagaStateFactory.cs`, `SagaStoreMongoDB.cs`: the property, the filter and the query; `MongoDbIndexCreator.CreateSagaIndexesAsync` (line 163) adds the compound index.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```
CONTEXT:
- Phases 1-2 are done (issue #738).

TASK:
Implement the Phase 3 tasks in Encina.EntityFrameworkCore and Encina.MongoDB.

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

1. `src/Encina.Testing.Fakes/Models/FakeSagaState.cs` and `Stores/FakeSagaStore.cs`: `TenantId` and the tenant-aware methods.
2. `src/Encina.Testing.Bogus/SagaStateFaker.cs`: `TenantId` rule.
3. `src/Encina.OpenTelemetry/MessagingStores/InstrumentedSagaStore.cs`: delegate the new methods and set the span tag `tenant.id` only when it is not high-cardinality for the backend in use (the activity tag, never a metric label).

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
- The tenant is a span tag only, never a metric label.

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

1. **Unit**: tenant stamping, fail-closed rule, D1 error, D4 background path, fake store, instrumented store.
2. **Guard**: new public methods with parameters.
3. **Contract**: `ISagaStore` tenant behavior identical across the real stores (run against `FakeSagaStore` and the provider stores).
4. **Property (FsCheck)**: a saga is never advanced by another tenant for any pair of tenants.
5. **Integration**: all 10 providers through the shared collections (`ADO-`, `Dapper-` x `SqlServer`/`PostgreSQL`/`MySQL`, `EFCore-` x three, MongoDB), including the two-tenant test of AC-043; `ClearAllDataAsync` in `InitializeAsync`; never `IClassFixture`.
6. **Load**: concurrent starts for two tenants (AGENTS.md section 9 says multi-tenancy is implemented); a justification `.md` only with a reason the plan does not foresee.
7. **Benchmark**: justification `tests/Encina.BenchmarkTests/Sagas/SagaTenant.md` (one extra column and predicate).
8. **Per-file targets** with a one-sentence justification in the manifests of each touched package (`Encina.Messaging`, the six ADO/Dapper packages, `Encina.EntityFrameworkCore`, `Encina.MongoDB`, `Encina.OpenTelemetry`, `Encina.Testing.Fakes`); measured with `dotnet test tests\Encina.<Flag>Tests --collect "XPlat Code Coverage"` and `.github/scripts/coverage-report.cs`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```
CONTEXT:
- Phases 1-6 are implemented (issue #738). AGENTS.md section 9 per-flag coverage model.

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
2. `changelog.d/738-saga-tenant-id.added.md`; the saga guide and schema documentation (through docs-writer); an ADR only if a Dilemma is decided architecturally (read `docs/architecture/adr/index.md` for the next free number).
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

| PR | Content | Approx. files | Approx. lines |
|----|---------|---------------|---------------|
| 1 | Phase 1: contract, orchestrator, error, logs, PublicAPI, unit tests | 10 | 700 |
| 2 | Phase 2a: ADO.NET (3 providers: state, factory, store, 2 scripts each) | 15 | 900 |
| 3 | Phase 2b: Dapper (3 providers) | 15 | 900 |
| 4 | Phase 3: EF Core and MongoDB | 10 | 600 |
| 5 | Phase 4 and 6: fakes, faker, instrumented store, counter | 8 | 400 |
| 6 | Phase 7 and 8: integration, contract and property tests, docs | 14 | 1,400 |

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
| `SagaOrchestrator`, `ISagaStateFactory` | `src/Encina.Messaging/Sagas/SagaOrchestrator.cs` | Tenant stamping and check |
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
| Messaging core | ~6 | Interface, factory, store, orchestrator, errors, logs |
| ADO.NET x3 and Dapper x3 | ~36 | State, factory, store, 2 scripts, per package |
| EF Core and MongoDB | ~9 | State, configuration, factory, store, index creator |
| Fakes, faker, instrumented store | ~4 | |
| Tests | ~14 | Unit, guard, contract, property, 10 integration (grouped), justification |
| Documentation | ~3 | Changelog fragment, guide, schema docs |
| **Total** | **~72 across 6 pull requests** | |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina is a pre-1.0 .NET 10 / C# 14 library; Either<EncinaError, T>; no [Obsolete], no compatibility layers, no migrations.
- Issue #738: TenantId on the saga row for all 10 database providers (SPEC-002 REQ-061, AC-043). Plan: docs/plans/saga-tenant-implementation-plan-738.md; Dilemmas D1-D4 are recommended, pending the maintainer.

IMPLEMENTATION OVERVIEW:
1. Contract and orchestrator in Encina.Messaging (Phase 1).
2. ADO.NET and Dapper packages (Phase 2); EF Core and MongoDB (Phase 3).
3. Fakes, faker and instrumented store (Phase 4); cross-cutting and observability (Phases 5-6).
4. Tests on real databases and documentation (Phases 7-8).

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
| 2 | OpenTelemetry | ✅ | Phase 4 and 6: tenant as a span tag, a denial counter without tenant label |
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

1. The maintainer answers D1 to D4 when work on #738 starts; the answers go into `## Maintainer Decisions` and the markers are removed.
2. Brief pull request 1 first; the provider pull requests follow in order.
