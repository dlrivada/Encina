# Implementation Plan: Data-Level Immutable-Record Guard on the 10 Database Providers

> **Issue**: [#1278](https://github.com/dlrivada/Encina/issues/1278)
> **Type**: Feature
> **Complexity**: Medium-High (12 phases across `Encina.DomainModeling`, `Encina.Security.Audit` and 8 provider packages, no new package, 103 file changes in 12 pull requests; see Estimated File Count)
> **Estimated Scope**: about 7,650 changed lines in total (production code, tests, manifests, PublicAPI and documentation), per pull request in Estimated File Count
> **Origin**: follow-up of #1187 (retention floors), decision OD-5 of 2026-09-23
> **Depends on**: nothing open; #1187 is related (its eraser needs the bypass of Design Choice 3)

---

## Summary

An `IImmutableRecord` marker interface in `Encina.DomainModeling` and a provider-neutral decorator `ImmutableRecordGuardRepository<TEntity, TId>` over `IFunctionalRepository<TEntity, TId>` that refuses every update and delete of an entity that implements the marker, returning `Left` and changing nothing. A correction is a new record added through `AddAsync` (Design Choice 11 settles how it is linked). The decision (P-01 OD-5, 2026-09-23) is that the retention-level freeze of #1187 is not enough for direct repository writes, so the guard sits at the repository boundary for all providers.

**What exists today** (verified on `origin/main`): `IFunctionalRepository<TEntity, TId>` (`src/Encina.DomainModeling/FunctionalRepository.cs`) has `AddAsync`, `UpdateAsync`, `DeleteAsync(TId)`, `DeleteAsync(TEntity)`, `AddRangeAsync`, `UpdateRangeAsync`, `DeleteRangeAsync` and `UpdateImmutableAsync` (the last updates entities that are C# immutable records through `with`; the name does not mean the marker of this issue). The Encina write paths to the same rows are:

- **Registered repository**: eight packages register it in `AddEncinaRepository<TEntity, TId>` with a `new FunctionalRepository...` in a scoped factory (`Encina.ADO.SqlServer|PostgreSQL|MySQL`, `Encina.Dapper.SqlServer|PostgreSQL|MySQL`, `Encina.EntityFrameworkCore`, `Encina.MongoDB`).
- **Tenant-aware repository**: a separate method, `AddTenantAwareRepository<TEntity, TId>` in `src/Encina.{ADO,Dapper}.{SqlServer,PostgreSQL,MySQL}/Tenancy/TenancyServiceCollectionExtensions.cs` and `src/Encina.MongoDB/Tenancy/TenancyServiceCollectionExtensions.cs` (seven files), each registering `IFunctionalRepository<TEntity, TId>` with `new TenantAwareFunctionalRepository...`.
- **Unit of Work**: `IUnitOfWork.Repository<TEntity, TId>()` creates `UnitOfWorkRepository*` instances directly in `UnitOfWorkADO` (x3), `UnitOfWorkDapper` (x3), `UnitOfWorkEF` and `UnitOfWorkMongoDB`.
- **Bulk operations**: `IBulkOperations<TEntity>` (`BulkUpdateAsync`, `BulkDeleteAsync`, `BulkMergeAsync`, each returning `Task<Either<EncinaError, int>>`) is implemented by `BulkOperationsADO` (ADO SqlServer), `BulkOperationsPostgreSQL` (ADO PostgreSQL), `BulkOperationsMySQL` (ADO MySQL), `BulkOperationsDapper` (in each Dapper package), `BulkOperationsEF` with its three public inner classes `BulkOperationsEFSqlServer`, `BulkOperationsEFPostgreSql` and `BulkOperationsEFMySql`, and `BulkOperationsMongoDB`. Only the ADO SqlServer, Dapper SqlServer, EF Core and MongoDB classes are registered in DI; the others are constructed by the application (public constructors).
- **Soft-delete repositories**: `AddEncinaSoftDeleteRepository` in EF Core registers `SoftDeleteRepositoryEF : ISoftDeleteRepository<TEntity, TId>`, whose base `IRepository<TEntity, TId>` (`src/Encina.DomainModeling/Repository.cs`) has `void Update`, `void UpdateRange`, `void Remove`, `void RemoveRange`, plus `RemoveByIdAsync` and the soft-delete members; in MongoDB it registers `SoftDeletableFunctionalRepositoryMongoDB : IFunctionalRepository<TEntity, TId>`.
- `AuditedRepository : IRepository<TEntity, TId>` (`src/Encina.Security.Audit/AuditedRepository.cs`) is a read-audit decorator that delegates writes to the repository it wraps; it adds no write path of its own.
- `InstrumentedFunctionalRepository` and `InstrumentedBulkOperations` (`src/Encina.OpenTelemetry/`) are decorators that no Encina registration method applies today; the application wraps the resolved service with them.

`RepositoryErrors` holds the `Repository.{Category}` codes; `RepositoryLog` uses EventIds 1100-1103 of `DomainModelingRepository` 1100-1199. Nothing named `IImmutableRecord` or `ImmutableRecordGuard` exists in `src/`.

**What this plan delivers**

1. `IImmutableRecord`, the decorator, a cached per-type check, `RepositoryErrors.ImmutableRecord<TEntity>(operation)` (code `Repository.ImmutableRecord`, with an ADR-062 category) and a metric and log for refusals, all in `Encina.DomainModeling`.
2. A type-bound, logged bypass scope for the lawful end-of-life deletion (Design Choice 3), audited durably before the delete (Design Choice 10), through an observer port with an adapter in `Encina.Security.Audit`.
3. The guard on every Encina write path of the 10 providers: the registered and tenant-aware repositories, the Unit of Work repositories, the bulk update, delete and merge, and the soft-delete registrations (Design Choices 1, 2 and 9).
4. Tests on all 10 providers.

**Out of scope**: a lock-after-signing lifecycle (a record that is editable until a moment and immutable after it is an instance-level state; this issue is the type-level marker, and a draft is modelled as a different entity type); database triggers (rejected: 10 dialects, untestable in unit tests); raw `DbContext`, SQL or driver writes, which no repository decorator can stop (documented, with the recommended permissions, in the feature page); the retention-level freeze itself (#1187).

**Standards**: GDPR Art. 5(1)(f) integrity and Art. 5(2) accountability; SPEC-002 REQ-006 and REQ-019 (see Research); AGENTS.md section 5 (provider coherence); ADR-062 (error category contract, merged).

**Affected packages**: `Encina.DomainModeling`, `Encina.Security.Audit` (the audit adapter), `Encina.ADO.SqlServer`, `Encina.ADO.PostgreSQL`, `Encina.ADO.MySQL`, `Encina.Dapper.SqlServer`, `Encina.Dapper.PostgreSQL`, `Encina.Dapper.MySQL`, `Encina.EntityFrameworkCore`, `Encina.MongoDB`.

**Provider category**: Database (10): ADO.NET x3, Dapper x3, EF Core x3 (one registration serves the three EF databases; the three EF bulk classes are guarded separately), MongoDB. The logic lives in the decorator and one static check, so provider-specific code is limited to registration, the Unit of Work and the bulk classes.

---

## Design Choices

Eight Dilemmas are open (1, 2, 3, 6, 7, 9, 10 and 11); the maintainer answers them when work on the issue starts. Choices 4, 5 and 8 are Settled or Trivial. The phases are written for the recommended options; where another option changes a phase, the choice says how.

<details>
<summary><strong>1. How the guard is applied to an entity</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Automatic.** Each provider's `AddEncinaRepository<TEntity, TId>` and `AddTenantAwareRepository<TEntity, TId>` wraps the repository it creates when `typeof(IImmutableRecord).IsAssignableFrom(typeof(TEntity))` (the check is cached per type) | The marker on the type is the whole declaration; cannot be forgotten; independent of registration order, as AGENTS.md requires | Fifteen registration methods change; an entity type that cannot implement the interface (third party) cannot be guarded |
| **B) Explicit opt-in** `services.AddEncinaImmutableRecordGuard<TEntity, TId>()` that decorates the registered descriptor | Works for types that cannot carry the marker; the issue's wording "registered per entity" | Order-dependent: it must run after `AddEncinaRepository`, which the registration rule forbids relying on; a forgotten call leaves the entity unguarded |

### Chosen Option: **A — Automatic wrapping from the marker** (recommended, pending the maintainer)

### Rationale

- Immutability is a property of the record type; making the type declare it and the registration honor it leaves no unguarded configuration. "Registered per entity" is satisfied because each entity registers its own repository.
- B wins when the entities to protect are generated or third-party types that cannot implement `IImmutableRecord`; an extension for that case can be added later without changing A.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Write paths beyond the registered repository</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Guard every Encina write path.** The registered and tenant-aware repositories (Design Choice 1), the repositories created by `IUnitOfWork.Repository<T,TId>()` (wrapped with the same decorator), the bulk update, delete and merge of all ten bulk classes (checked inside each class with `ImmutableRecordGuard.IsImmutable<TEntity>()`, so an application that constructs a bulk class directly is guarded too; insert is allowed), and the soft-delete registrations (Design Choice 9). `AuditedRepository` needs no change: it delegates writes to the repository it wraps | The acceptance criterion "through any Encina repository ... returns `Left` and changes nothing" holds on every Encina path | Many files across the providers; a second check to keep in step |
| **B) Registered repository only.** Document that the Unit of Work, bulk operations and soft-delete repositories are not guarded | Smallest change | An application using the Unit of Work or `BulkUpdateAsync` silently rewrites an immutable record through Encina itself |

### Chosen Option: **A — Guard every Encina write path** (recommended, pending the maintainer)

### Rationale

- A guard that a documented Encina API walks around does not enforce immutability at "the repository boundary". Raw `DbContext` or SQL stay outside, and the feature page says so.
- B wins if the maintainer wants the first pull requests small and accepts a documented gap until follow-ups land. With B, Phases 4-11 keep only the registration changes.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Lawful end-of-life deletion of an immutable record</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An explicit, logged, type-bound bypass scope.** `IDisposable ImmutableRecordGuardBypass.Begin<TEntity>(string reason)` (an `AsyncLocal` scope; `ArgumentException` for a null, empty or whitespace reason) lets the decorator pass deletes of `TEntity` only, inside that scope only; each bypassed delete is logged, counted and audited first (Design Choice 10); only the retention eraser (#1187) is meant to call it | Retention expiry and erasure after the floor can delete through the same repository; the opt-out is explicit, logged and limited to one type | A new public concept that application code could misuse |
| **B) No bypass.** The eraser deletes through the provider's raw access | Smallest public surface | The eraser needs provider-specific code on 10 providers, which is what the decorator was meant to avoid; deletion then escapes the audit |

### Chosen Option: **A — Explicit, logged, type-bound bypass scope** (recommended, pending the maintainer)

### Rationale

- The #1187 plan records that "deletion at the end of the period must bypass the guard (the eraser uses its own path)"; A gives that path one audited shape. Only a delete passes the bypass: an update of an immutable record is never allowed. The scope is bound to one entity type, so erasing one record type never opens the others.
- B wins if the maintainer prefers that the guard has no escape hatch at all and the eraser is built on raw provider access.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Marker plus provider-neutral decorator over <code>IFunctionalRepository</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IImmutableRecord` in `Encina.DomainModeling` and `ImmutableRecordGuardRepository<TEntity,TId>`, refusing `UpdateAsync`, `UpdateRangeAsync`, `UpdateImmutableAsync`, both `DeleteAsync` and `DeleteRangeAsync` with `Left`; `AddAsync`, `AddRangeAsync` and all reads delegate** | One decorator for all providers; no SQL per dialect; unit-testable | Cannot stop writes that bypass Encina |
| **B) Retention-level freeze only** | Already delivered by #1187 | Does not stop direct repository writes |
| **C) Database triggers per provider** | Stops raw SQL too | 10 dialects and not testable in unit tests |

### Chosen Option: **A — Marker and decorator**

### Rationale

- Decided by the maintainer on 2026-09-23 (P-01 OD-5) and rejected C in the issue body; the refusal is checked before the inner repository is called, so nothing changes.

**Class:** Settled
**Settled by:** #1187 plan, OD-5 (maintainer decision of 2026-09-23); #1278 (body and comment of 2026-10-17); AGENTS.md section 5 (provider coherence: logic in the shared layer)

</details>

<details>
<summary><strong>5. Refusal is a coded <code>Left</code>; the check is type-level and cached</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `RepositoryErrors.ImmutableRecord<TEntity>(operation)` with code `Repository.ImmutableRecord`, a fixed message naming the entity type and operation (no entity values); the marker check is a static generic cache per entity type** | ROP; no exception; zero per-call reflection | None significant |
| **B) Throw `InvalidOperationException`** | Familiar | Exceptions for business logic are forbidden |

### Chosen Option: **A — Coded `Left`, cached check**

### Rationale

- The error follows the `Repository.{Category}` pattern of `RepositoryErrors`; its `ErrorCategory` is Design Choice 6.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (Railway Oriented Programming); ADR-001, ADR-006; `src/Encina.DomainModeling/RepositoryErrors.cs` (`Repository.{Category}` codes)

</details>

<details>
<summary><strong>6. <code>ErrorCategory</code> of <code>Repository.ImmutableRecord</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ErrorCategory.Conflict`** (409) | ADR-062 rule R2 puts an invalid transition in `Conflict`; changing a record that may not change is closest to that | A host answers 409, which clients may retry after a reload |
| **B) `ErrorCategory.Forbidden`** (403) | ADR-062 rule R6 puts denials in `Forbidden`; the guard denies the write | Reads as an authorization failure, which it is not: no principal would be allowed |

### Chosen Option: **A — `Conflict`** (recommended, pending the maintainer)

### Rationale

- ADR-062 (merged) requires a category on every `EncinaErrors.Create` in `src/` and its completeness scan fails otherwise; the code uses the category overload. Rules R2 and R6 both read on this case, so the choice is the maintainer's. `FailedPrecondition` is excluded until #3420 (ADR-062 R2); #3420 (open) owns how compliance and security denials are answered by the hosts, so the worker re-checks this category against it when the issue is picked up.
- B wins when the maintainer wants every refusal of a compliance control to look the same to clients.

**Class:** Dilemma

</details>

<details>
<summary><strong>7. Position of the guard among the decorators</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The guard is the outermost wrapper Encina registers** (above the tenant-aware repository); the OpenTelemetry decorators (`InstrumentedFunctionalRepository`, `InstrumentedBulkOperations`), which the application applies around the resolved service, sit outside it, so a refusal is traced and metered as a failed repository operation with the code `Repository.ImmutableRecord`, besides the guard's own counter | A refusal never touches the database, even for a missing tenant; refusals are visible in the normal repository telemetry | A refusal counts as a repository error in the OpenTelemetry metrics |
| **B) The guard inside the tenant-aware repository** | Tenant checks first | A tenant error can mask the refusal; the guard sees pre-filtered calls |

### Chosen Option: **A — Outermost of Encina's wrappers, inside the application's instrumentation** (recommended, pending the maintainer)

### Rationale

- The refusal depends only on the entity type and the operation, so it needs nothing from the tenant layer. No rule fixes the order, and B also respects AGENTS.md section 5, so this is a choice. B wins when the maintainer wants the tenant to be checked before any other answer.

**Class:** Dilemma

</details>

<details>
<summary><strong>8. EventIds, telemetry and audit port</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) EventIds 1104-1106 inside the existing `DomainModelingRepository` range (1104 `ImmutableRecordWriteRefused`, Warning; 1105 `ImmutableRecordBypassedDelete`, Warning; 1106 `ImmutableRecordBypassAuditFailed`, Error); counters `encina.repository.immutable_record.refusals` (tags `entity_type`, `operation`) and `encina.repository.immutable_record.bypasses` on the existing repository meter, ADR-054 names; a port `IImmutableRecordGuardObserver` in `Encina.DomainModeling` with an adapter in `Encina.Security.Audit` that writes an `OperationAuditEntry` through `IOperationAuditStore.RecordAsync`** | No new range; `DomainModeling` stays free of audit references (the audit package already references it) | The adapter lives in another package |
| **B) Call the audit store from `Encina.DomainModeling`** | One package | A new reference from the data layer to Security.Audit (it references DomainModeling: a cycle) |

### Chosen Option: **A — Existing range, ADR-054 names, observer port**

### Rationale

- Phase 1 confirms by grep and `EventIdRanges.GetAllRanges()` that 1104-1106 are free (on `main` 1100-1103 are used); the ids are packed. The batch EventId registry has no row for them yet; the orchestrator adds `DomainModelingRepository 1100-1199 | 1104-1106 | #1278`.

**Class:** Trivial
**Settled by:** AGENTS.md section 7 and ADR-021 (ids inside a package's existing range); ADR-054 settled point 4; ADR-036; `src/Encina.Security.Audit/Encina.Security.Audit.csproj` (references `Encina.DomainModeling`)

</details>

<details>
<summary><strong>9. Soft-delete repositories of a marked type</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Registration error.** `AddEncinaSoftDeleteRepository<TEntity, TId>` (EF Core and MongoDB) fails with an `InvalidOperationException` naming the type when `TEntity` implements `IImmutableRecord`: a record that may never change cannot be soft-deleted, restored or updated | A configuration error at start-up, not a business exception at run time; no `void` member has to report a refusal | An application cannot read an immutable type through the soft-delete query surface |
| **B) A `SaveChangesInterceptor` in EF Core** that refuses `Modified` and `Deleted` entries of marked types, plus the decorator on the MongoDB soft-delete repository | Also stops raw `DbContext` writes on EF Core | The refusal at `SaveChanges` surfaces as an exception, which AGENTS.md section 3 forbids for business logic; covers EF Core only |

A third option, an `IRepository` guard whose `void` members throw, is excluded by AGENTS.md section 3 (no exceptions for business logic).

### Chosen Option: **A — Registration error** (recommended, pending the maintainer)

### Rationale

- Both remaining options close the soft-delete path. I recommend A because it fails at start-up with a clear message and needs no run-time exception. B wins when the maintainer wants EF Core to refuse even raw `DbContext` writes and accepts an exception at `SaveChanges` for that case.

**Class:** Dilemma

</details>

<details>
<summary><strong>10. Audit of a bypassed delete</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Durable audit first.** The observer is asynchronous (`ValueTask<Either<EncinaError, Unit>> OnRefusedAsync(Type entityType, string operation, CancellationToken)` and `OnBypassedDeleteAsync(Type entityType, string reason, CancellationToken)`); the decorator awaits `OnBypassedDeleteAsync` before the inner delete and refuses the delete (`Repository.ImmutableRecord`, log 1106) when it returns `Left`; with no observer registered a bypassed delete is refused (fail closed); a refusal is audited by an awaited `OnRefusedAsync` whose failure is logged by error code and never turns the refusal into success | An erasure of an immutable record never happens without its audit entry; no queue | The eraser depends on the audit store being available |
| **B) Best-effort audit.** The observer queues its write; an audit failure is logged and never stops the delete | The eraser never waits for the audit store | A lawful-erasure delete can go through without an audit entry (fails open), and a queued write that fails reports success, which AGENTS.md section 3 forbids in background infrastructure |

### Chosen Option: **A — Durable audit before a bypassed delete** (recommended, pending the maintainer)

### Rationale

- Compliance gates fail closed and a `Left` from a store in infrastructure fails the operation (AGENTS.md section 3). B is listed because it is the cheaper design; it wins only if the maintainer accepts best-effort audit for erasures. The observer adapter is registered by `AddEncinaAudit` in `Encina.Security.Audit` with `TryAdd`.

**Class:** Dilemma

</details>

<details>
<summary><strong>11. How a correction is linked to the original</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Marker only; the application links.** The entity carries its own link (for example a `CorrectsId` property); Encina only guarantees that adding the correction succeeds and the original stays unchanged. The acceptance criterion "corrections via linked records work as expected" is proven by an integration test per provider with a test entity that carries `CorrectsId`: the correction is added, the original is unchanged, and both are read back with the link | No new contract; any linking scheme works | Encina does not verify that the linked original exists |
| **B) A linked-record contract.** `ICorrectionRecord<TId>` with `TId CorrectsId`; the decorator's `AddAsync` checks that the original exists and returns `Repository.NotFound` otherwise | The link is part of the API and verified | One more public type and a read on every correction add |

### Chosen Option: **A — Marker only, proven by a linked-record test** (recommended, pending the maintainer)

### Rationale

- The issue's body says a correction is a new record linked by the application; A keeps that and proves the criterion with a real linked record. B wins when the maintainer wants Encina to verify the link; it adds one source file and its tests to Phase 1.

**Class:** Dilemma

</details>

---

## Implementation Phases

Each phase is one pull request of at most 15 files and 1500 changed lines; the file lists below are exact and count the tests, the PublicAPI files and the coverage manifests. No pull request uses a type created by a later one. Each pull request enters, per `src/` file and per flag, the coverage targets that its own tests meet, each with a one-sentence justification (AGENTS.md section 9, #1762). Every provider phase has unit tests of the changed registration, Unit of Work and bulk code, including a DI test that builds the provider with `ValidateOnBuild` and `ValidateScopes` and proves the wrapping in any registration order and for the tenant-aware registration, and integration tests on its databases.

### Phase 1: Marker, Decorator, Error and Refusal Telemetry (DomainModeling)

> **Goal**: The guard refuses writes on any `IFunctionalRepository`. **Size**: 14 files, about 1,050 lines.

<details>
<summary>Tasks</summary>

Namespace `Encina.DomainModeling`; folder `ImmutableRecords/`.

1. `IImmutableRecord.cs`: marker interface with XML docs (type-level; a correction is a new record).
2. `ImmutableRecordGuardRepository.cs` (public sealed, `IFunctionalRepository<TEntity, TId>` with the same constraints as the interface, no `IImmutableRecord` constraint, so registrations can wrap after the cached check): constructor `(IFunctionalRepository<TEntity, TId> inner, IImmutableRecordGuardObserver? observer)`; refuses `UpdateAsync`, `UpdateRangeAsync`, `UpdateImmutableAsync`, both `DeleteAsync` and `DeleteRangeAsync` before calling the inner repository, awaiting `OnRefusedAsync` when an observer exists (its failure is logged by error code and the call still returns the refusal); delegates `AddAsync`, `AddRangeAsync` and all reads.
3. `ImmutableRecordGuard.cs` (public static): `bool IsImmutable<TEntity>()` (static generic cache) and `IFunctionalRepository<TEntity, TId> Wrap<TEntity, TId>(IFunctionalRepository<TEntity, TId> inner, IServiceProvider services)` returning the inner repository itself for unmarked types and otherwise the decorator with the observer resolved optionally from `services`. `IImmutableRecordGuardObserver.cs` (the port with both members of Design Choice 10; `OnBypassedDeleteAsync` is first called in Phase 2).
4. `RepositoryErrors.cs`: `ImmutableRecord<TEntity>(string operation)` through the category overload of `EncinaErrors.Create` with the category of Design Choice 6; `Diagnostics/RepositoryLog.cs`: 1104; `Diagnostics/RepositoryMetrics.cs`: the refusals counter; `PublicAPI.Unshipped.txt`.
5. Tests: `tests/Encina.UnitTests/DomainModeling/ImmutableRecords/ImmutableRecordGuardRepositoryTests.cs` (every refused method returns `Left` and the inner mock is never called; adds and reads delegate; log 1104 and the counter), `.../ImmutableRecordGuardTests.cs` (cache, `Wrap` for marked and unmarked types), `tests/Encina.GuardTests/DomainModeling/ImmutableRecords/ImmutableRecordGuardGuardTests.cs`, `tests/Encina.PropertyTests/DomainModeling/ImmutableRecordGuardPropertyTests.cs` (FsCheck: no sequence of update or delete calls on a marked entity succeeds), `tests/Encina.ContractTests/DomainModeling/ImmutableRecordGuardRepositoryContractTests.cs` (the `IFunctionalRepository` contract on the decorator over an in-memory inner repository).
6. `.github/coverage-manifest/Encina.DomainModeling.json`: unit, guard, property and contract targets of the new and changed files.

Exact file list (14): `IImmutableRecord.cs`, `ImmutableRecordGuardRepository.cs`, `ImmutableRecordGuard.cs`, `IImmutableRecordGuardObserver.cs`, `RepositoryErrors.cs`, `RepositoryLog.cs`, `RepositoryMetrics.cs`, `PublicAPI.Unshipped.txt`, the manifest, and the 5 test files of task 5.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT:
- Issue #1278; plan docs/plans/immutable-record-guard-implementation-plan-1278.md. Maintainer Decisions override the "recommended" choices.
- IFunctionalRepository is in src/Encina.DomainModeling/FunctionalRepository.cs; errors in RepositoryErrors.cs; logs in
  Diagnostics/RepositoryLog.cs (ids 1100-1103 used). ADR-062 (error categories) is merged.

TASK:
Create IImmutableRecord, the decorator, the cached guard, the observer port, the error with its ErrorCategory, the log message
1104 and the refusals counter with their tests. Exactly the 14 files of the Phase 1 file list; at most 1500 lines.

KEY RULES:
- A refused call never reaches the inner repository; the error is a coded Left with no entity values.
- UpdateImmutableAsync (C# immutable-record update) is refused as well; the name has nothing to do with the marker.
- No static mutable state except the generic type cache; XML docs on all public members; RS0016 must not fire.

REFERENCE FILES:
- src/Encina.DomainModeling/FunctionalRepository.cs
- src/Encina.DomainModeling/RepositoryErrors.cs
- src/Encina.DomainModeling/Diagnostics/RepositoryLog.cs
```

</details>

---

### Phase 2: Bypass Scope and Observer Port (DomainModeling)

> **Goal**: A lawful deletion passes only inside a type-bound scope and only after its audit is written. **Size**: 10 files, about 750 lines.

<details>
<summary>Tasks</summary>

1. `ImmutableRecords/ImmutableRecordGuardBypass.cs` (public static): `IDisposable Begin<TEntity>(string reason)` (Design Choice 3).
2. `ImmutableRecordGuardRepository.cs`: inside a scope for `TEntity` a delete awaits `OnBypassedDeleteAsync` and then delegates; with no observer or on `Left` it is refused (Design Choice 10).
3. `Diagnostics/RepositoryLog.cs`: 1105 and 1106; `Diagnostics/RepositoryMetrics.cs`: the bypasses counter; `PublicAPI.Unshipped.txt`.
4. Tests: `tests/Encina.UnitTests/DomainModeling/ImmutableRecords/ImmutableRecordGuardBypassTests.cs` (delete passes in scope; update never; another type is not bypassed; scope ends on dispose and flows across `await`), `.../ImmutableRecordGuardRepositoryTests.cs` (extended: observer calls, audit `Left` refuses the bypassed delete, no observer refuses it, log 1105 and 1106), `tests/Encina.GuardTests/DomainModeling/ImmutableRecords/ImmutableRecordGuardGuardTests.cs` (extended: null, empty and whitespace reason), `tests/Encina.PropertyTests/DomainModeling/ImmutableRecordGuardPropertyTests.cs` (extended: no update succeeds even inside a scope).
5. Manifest: the targets of the new and changed files.

Exact file list (10): `ImmutableRecordGuardBypass.cs`, `ImmutableRecordGuardRepository.cs`, `RepositoryLog.cs`, `RepositoryMetrics.cs`, `PublicAPI.Unshipped.txt`, the manifest, and the 4 test files of task 4.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT:
- Phase 1 is merged. Plan Design Choices 3 and 10.

TASK:
Add the type-bound bypass scope, its use of the observer port, EventIds 1105-1106 and the bypasses counter, with tests.
Exactly the 10 files of the Phase 2 file list.

KEY RULES:
- Fail closed: a bypassed delete happens only after a successful audit write; no observer means refused.
- An update is never bypassed; the AsyncLocal scope is the only new static state.

REFERENCE FILES:
- src/Encina.DomainModeling/ImmutableRecords/ImmutableRecordGuardRepository.cs (Phase 1)
- src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs
```

</details>

---

### Phase 3: Audit Adapter (Security.Audit)

> **Goal**: Refusals and bypassed deletes are written to the operation audit store. **Size**: 7 files, about 450 lines.

<details>
<summary>Tasks</summary>

1. `src/Encina.Security.Audit/ImmutableRecords/ImmutableRecordGuardAuditObserver.cs`: implements `IImmutableRecordGuardObserver`; one `OperationAuditEntry` per refusal and per bypassed delete, awaited through `IOperationAuditStore.RecordAsync`, returning its `Either`; entity type, operation and reason only, never entity values.
2. `src/Encina.Security.Audit/ServiceCollectionExtensions.cs` (`AddEncinaAudit`): registers the observer with `TryAdd`; `PublicAPI.Unshipped.txt`; `.github/coverage-manifest/Encina.Security.Audit.json`.
3. Tests: `tests/Encina.UnitTests/Security/Audit/ImmutableRecordGuardAuditObserverTests.cs` (entry shape, `Left` passed through), `tests/Encina.GuardTests/Security/Audit/ImmutableRecordGuardAuditObserverGuardTests.cs`, `tests/Encina.UnitTests/Security/Audit/ServiceCollectionExtensionsTests.cs` (extended: the observer is registered; `ValidateOnBuild` and `ValidateScopes`).
4. Manifest: unit and guard targets of the two source files.

Exact file list (7): the observer, `ServiceCollectionExtensions.cs`, `PublicAPI.Unshipped.txt`, the manifest, and the 3 test files.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT:
- Phases 1-2 merged. AddEncinaAudit is in src/Encina.Security.Audit/ServiceCollectionExtensions.cs.

TASK:
Implement the audit observer and its registration with tests. Exactly the 7 files of the Phase 3 file list.

KEY RULES:
- The audit write is awaited and its Left is returned, never swallowed; no entity values in the entry.

REFERENCE FILES:
- src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs
- src/Encina.Security.Audit/ServiceCollectionExtensions.cs
```

</details>

---

### Phase 4: EF Core

> **Goal**: Every EF Core write path is guarded on SqlServer, PostgreSQL and MySQL. **Size**: 11 files, about 900 lines.

<details>
<summary>Tasks</summary>

1. `src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs`: `AddEncinaRepository<TEntity, TId>` wraps with `ImmutableRecordGuard.Wrap`; `AddEncinaSoftDeleteRepository<TEntity, TId>` fails for a marked type (Design Choice 9).
2. `src/Encina.EntityFrameworkCore/UnitOfWork/UnitOfWorkEF.cs`: `Repository<TEntity, TId>()` wraps the repository it creates.
3. `src/Encina.EntityFrameworkCore/BulkOperations/BulkOperationsEFSqlServer.cs`, `BulkOperationsEFPostgreSql.cs`, `BulkOperationsEFMySql.cs`: `BulkUpdateAsync`, `BulkDeleteAsync` and `BulkMergeAsync` return the coded `Left` for marked types before any database call (`BulkOperationsEF` delegates to them).
4. Tests: `tests/Encina.UnitTests/EntityFrameworkCore/ImmutableRecords/ImmutableRecordGuardRegistrationEFTests.cs` (wrapping in any order, soft-delete registration error, Unit of Work wrapping, DI with `ValidateOnBuild` and `ValidateScopes`), `.../ImmutableRecordGuardBulkEFTests.cs` (the three bulk classes refuse before any call), and integration files `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/{SqlServer,PostgreSQL,MySQL}/ImmutableRecords/ImmutableRecordGuardEF{SqlServer,PostgreSql,MySql}Tests.cs` (`[Collection("EFCore-SqlServer")]` and siblings: update and delete through the registered repository, the Unit of Work and bulk operations return `Left` and the row is unchanged; a correction linked through `CorrectsId` is added and read back with the original unchanged, Design Choice 11; a delete inside a bypass scope with an audit observer succeeds).
5. `.github/coverage-manifest/Encina.EntityFrameworkCore.json`: unit and integration targets of the five changed source files.

Exact file list (11): the 5 source files of tasks 1-3, the manifest, the 2 unit test files and the 3 integration test files.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT:
- Phases 1-3 merged. Plan Design Choices 1, 2, 7, 9 and 11.

TASK:
Guard the EF Core registration, soft-delete registration, Unit of Work and bulk classes, with tests. Exactly the 11 files of the
Phase 4 file list.

KEY RULES:
- The refusal happens before any database call; registration works in any order and keeps an application's own registration.
- Integration tests use [Collection("EFCore-SqlServer")] and siblings, ClearAllDataAsync in InitializeAsync.

REFERENCE FILES:
- src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs
- src/Encina.EntityFrameworkCore/UnitOfWork/UnitOfWorkEF.cs
- src/Encina.EntityFrameworkCore/BulkOperations/BulkOperationsEF.cs
```

</details>

---

### Phase 5: MongoDB

> **Goal**: Every MongoDB write path is guarded. **Size**: 8 files, about 600 lines.

<details>
<summary>Tasks</summary>

1. `src/Encina.MongoDB/ServiceCollectionExtensions.cs` (`AddEncinaRepository` wraps; `AddEncinaSoftDeleteRepository` fails for a marked type), `src/Encina.MongoDB/Tenancy/TenancyServiceCollectionExtensions.cs` (`AddTenantAwareRepository` wraps), `src/Encina.MongoDB/UnitOfWork/UnitOfWorkMongoDB.cs`, `src/Encina.MongoDB/BulkOperations/BulkOperationsMongoDB.cs`.
2. Tests: `tests/Encina.UnitTests/MongoDB/ImmutableRecords/ImmutableRecordGuardRegistrationMongoDBTests.cs`, `.../ImmutableRecordGuardBulkMongoDBTests.cs`, `tests/Encina.IntegrationTests/Infrastructure/MongoDB/ImmutableRecords/ImmutableRecordGuardMongoDBTests.cs` (the shared MongoDB collection; the Phase 4 cases).
3. `.github/coverage-manifest/Encina.MongoDB.json`: unit and integration targets of the four source files.

Exact file list (8): the 4 source files, the manifest and the 3 test files.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT:
- Phase 4 merged; mirror it for MongoDB, including the tenant-aware registration.

TASK:
Exactly the 8 files of the Phase 5 file list.

KEY RULES:
- Asynchronous calls with a CancellationToken; shared collection fixture only; the refusal happens before any call.

REFERENCE FILES:
- src/Encina.MongoDB/ServiceCollectionExtensions.cs
- src/Encina.MongoDB/Tenancy/TenancyServiceCollectionExtensions.cs
```

</details>

---

### Phase 6: ADO.NET SqlServer

> **Goal**: Every ADO SqlServer write path is guarded. **Size**: 8 files, about 600 lines.

<details>
<summary>Tasks</summary>

1. `src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs` (`AddEncinaRepository` wraps), `src/Encina.ADO.SqlServer/Tenancy/TenancyServiceCollectionExtensions.cs` (`AddTenantAwareRepository` wraps), `src/Encina.ADO.SqlServer/UnitOfWork/UnitOfWorkADO.cs`, `src/Encina.ADO.SqlServer/BulkOperations/BulkOperationsADO.cs`.
2. Tests: `tests/Encina.UnitTests/ADO/SqlServer/ImmutableRecords/ImmutableRecordGuardRegistrationADOTests.cs`, `.../ImmutableRecordGuardBulkADOTests.cs`, `tests/Encina.IntegrationTests/ADO/SqlServer/ImmutableRecords/ImmutableRecordGuardADOSqlServerTests.cs` (`[Collection("ADO-SqlServer")]`, the Phase 4 cases).
3. `.github/coverage-manifest/Encina.ADO.SqlServer.json`: unit and integration targets of the four source files.

Exact file list (8): the 4 source files, the manifest and the 3 test files.

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```text
CONTEXT:
- Phases 1-5 merged. The ADO registration builds FunctionalRepositoryADO in a scoped factory.

TASK:
Exactly the 8 files of the Phase 6 file list.

KEY RULES:
- Identical behavior and error code to the other providers; shared collection fixtures only.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs
- src/Encina.ADO.SqlServer/UnitOfWork/UnitOfWorkADO.cs
- src/Encina.ADO.SqlServer/BulkOperations/BulkOperationsADO.cs
```

</details>

---

### Phase 7: ADO.NET PostgreSQL

> **Goal**: Every ADO PostgreSQL write path is guarded. **Size**: 8 files, about 600 lines.

<details>
<summary>Tasks</summary>

1. `src/Encina.ADO.PostgreSQL/ServiceCollectionExtensions.cs`, `Tenancy/TenancyServiceCollectionExtensions.cs`, `UnitOfWork/UnitOfWorkADO.cs`, `BulkOperations/BulkOperationsPostgreSQL.cs` (constructed by the application, so the check sits in the class).
2. Tests: `tests/Encina.UnitTests/ADO/PostgreSQL/ImmutableRecords/ImmutableRecordGuardRegistrationADOTests.cs`, `.../ImmutableRecordGuardBulkADOTests.cs`, `tests/Encina.IntegrationTests/ADO/PostgreSQL/ImmutableRecords/ImmutableRecordGuardADOPostgreSqlTests.cs` (`[Collection("ADO-PostgreSQL")]`).
3. `.github/coverage-manifest/Encina.ADO.PostgreSQL.json`: unit and integration targets of the four source files.

Exact file list (8): the 4 source files, the manifest and the 3 test files.

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```text
CONTEXT:
- Phase 6 merged; mirror it for ADO PostgreSQL.

TASK:
Exactly the 8 files of the Phase 7 file list.

KEY RULES:
- BulkOperationsPostgreSQL is guarded inside the class; [Collection("ADO-PostgreSQL")].

REFERENCE FILES:
- src/Encina.ADO.PostgreSQL/BulkOperations/BulkOperationsPostgreSQL.cs
```

</details>

---

### Phase 8: ADO.NET MySQL

> **Goal**: Every ADO MySQL write path is guarded. **Size**: 8 files, about 600 lines.

<details>
<summary>Tasks</summary>

1. `src/Encina.ADO.MySQL/ServiceCollectionExtensions.cs`, `Tenancy/TenancyServiceCollectionExtensions.cs`, `UnitOfWork/UnitOfWorkADO.cs`, `BulkOperations/BulkOperationsMySQL.cs`.
2. Tests: `tests/Encina.UnitTests/ADO/MySQL/ImmutableRecords/ImmutableRecordGuardRegistrationADOTests.cs`, `.../ImmutableRecordGuardBulkADOTests.cs`, `tests/Encina.IntegrationTests/ADO/MySQL/ImmutableRecords/ImmutableRecordGuardADOMySqlTests.cs` (`[Collection("ADO-MySQL")]`).
3. `.github/coverage-manifest/Encina.ADO.MySQL.json`: unit and integration targets of the four source files.

Exact file list (8): the 4 source files, the manifest and the 3 test files.

</details>

<details>
<summary>Prompt for AI Agents — Phase 8</summary>

```text
CONTEXT:
- Phase 6 merged; mirror it for ADO MySQL.

TASK:
Exactly the 8 files of the Phase 8 file list.

KEY RULES:
- BulkOperationsMySQL is guarded inside the class; [Collection("ADO-MySQL")].

REFERENCE FILES:
- src/Encina.ADO.MySQL/BulkOperations/BulkOperationsMySQL.cs
```

</details>

---

### Phase 9: Dapper SqlServer

> **Goal**: Every Dapper SqlServer write path is guarded. **Size**: 8 files, about 600 lines.

<details>
<summary>Tasks</summary>

1. `src/Encina.Dapper.SqlServer/ServiceCollectionExtensions.cs`, `Tenancy/TenancyServiceCollectionExtensions.cs`, `UnitOfWork/UnitOfWorkDapper.cs`, `BulkOperations/BulkOperationsDapper.cs`.
2. Tests: `tests/Encina.UnitTests/Dapper/SqlServer/ImmutableRecords/ImmutableRecordGuardRegistrationDapperTests.cs`, `.../ImmutableRecordGuardBulkDapperTests.cs`, `tests/Encina.IntegrationTests/Dapper/SqlServer/ImmutableRecords/ImmutableRecordGuardDapperSqlServerTests.cs` (`[Collection("Dapper-SqlServer")]`).
3. `.github/coverage-manifest/Encina.Dapper.SqlServer.json`: unit and integration targets of the four source files.

Exact file list (8): the 4 source files, the manifest and the 3 test files.

</details>

<details>
<summary>Prompt for AI Agents — Phase 9</summary>

```text
CONTEXT:
- Phases 1-8 merged; the Dapper packages mirror the ADO ones.

TASK:
Exactly the 8 files of the Phase 9 file list.

KEY RULES:
- Identical behavior and error code; [Collection("Dapper-SqlServer")].

REFERENCE FILES:
- src/Encina.Dapper.SqlServer/ServiceCollectionExtensions.cs
- src/Encina.Dapper.SqlServer/UnitOfWork/UnitOfWorkDapper.cs
- src/Encina.Dapper.SqlServer/BulkOperations/BulkOperationsDapper.cs
```

</details>

---

### Phase 10: Dapper PostgreSQL

> **Goal**: Every Dapper PostgreSQL write path is guarded. **Size**: 8 files, about 600 lines.

<details>
<summary>Tasks</summary>

1. `src/Encina.Dapper.PostgreSQL/ServiceCollectionExtensions.cs`, `Tenancy/TenancyServiceCollectionExtensions.cs`, `UnitOfWork/UnitOfWorkDapper.cs`, `BulkOperations/BulkOperationsDapper.cs`.
2. Tests: `tests/Encina.UnitTests/Dapper/PostgreSQL/ImmutableRecords/ImmutableRecordGuardRegistrationDapperTests.cs`, `.../ImmutableRecordGuardBulkDapperTests.cs`, `tests/Encina.IntegrationTests/Dapper/PostgreSQL/ImmutableRecords/ImmutableRecordGuardDapperPostgreSqlTests.cs` (`[Collection("Dapper-PostgreSQL")]`).
3. `.github/coverage-manifest/Encina.Dapper.PostgreSQL.json`: unit and integration targets of the four source files.

Exact file list (8): the 4 source files, the manifest and the 3 test files.

</details>

<details>
<summary>Prompt for AI Agents — Phase 10</summary>

```text
CONTEXT:
- Phase 9 merged; mirror it for Dapper PostgreSQL.

TASK:
Exactly the 8 files of the Phase 10 file list.

KEY RULES:
- [Collection("Dapper-PostgreSQL")]; the bulk class is guarded inside the class.

REFERENCE FILES:
- src/Encina.Dapper.PostgreSQL/BulkOperations/BulkOperationsDapper.cs
```

</details>

---

### Phase 11: Dapper MySQL

> **Goal**: Every Dapper MySQL write path is guarded. **Size**: 8 files, about 600 lines.

<details>
<summary>Tasks</summary>

1. `src/Encina.Dapper.MySQL/ServiceCollectionExtensions.cs`, `Tenancy/TenancyServiceCollectionExtensions.cs`, `UnitOfWork/UnitOfWorkDapper.cs`, `BulkOperations/BulkOperationsDapper.cs`.
2. Tests: `tests/Encina.UnitTests/Dapper/MySQL/ImmutableRecords/ImmutableRecordGuardRegistrationDapperTests.cs`, `.../ImmutableRecordGuardBulkDapperTests.cs`, `tests/Encina.IntegrationTests/Dapper/MySQL/ImmutableRecords/ImmutableRecordGuardDapperMySqlTests.cs` (`[Collection("Dapper-MySQL")]`).
3. `.github/coverage-manifest/Encina.Dapper.MySQL.json`: unit and integration targets of the four source files.

Exact file list (8): the 4 source files, the manifest and the 3 test files.

</details>

<details>
<summary>Prompt for AI Agents — Phase 11</summary>

```text
CONTEXT:
- Phase 9 merged; mirror it for Dapper MySQL.

TASK:
Exactly the 8 files of the Phase 11 file list.

KEY RULES:
- [Collection("Dapper-MySQL")]; the bulk class is guarded inside the class.

REFERENCE FILES:
- src/Encina.Dapper.MySQL/BulkOperations/BulkOperationsDapper.cs
```

</details>

---

### Phase 12: Justifications and Documentation

> **Goal**: The feature is documented and recorded. **Size**: 5 files, about 300 lines.

<details>
<summary>Tasks</summary>

1. Load and benchmark justifications following the template of AGENTS.md section 9: `tests/Encina.LoadTests/DomainModeling/ImmutableRecordGuard.md` and `tests/Encina.BenchmarkTests/Encina.Benchmarks/DomainModeling/ImmutableRecordGuard.md` (the decorator is a cached type check and a return).
2. Documentation by the docs-writer agent: `docs/features/immutable-records.md` (marker, corrections as linked records, the bypass scope and its audit, the soft-delete registration error, what the guard cannot stop), `src/Encina.DomainModeling/README.md`; `changelog.d/1278-immutable-record-guard.added.md`. An ADR is optional (the choices follow OD-5); the feature page cites it.
3. `dotnet build --configuration Release` 0 warnings; `dotnet test` green; per-flag coverage at the manifest targets of the ten packages; CRAP of every new or changed method at most 10.

Exact file list (5): the 2 justification files, the feature page, the README and the changelog fragment.

</details>

<details>
<summary>Prompt for AI Agents — Phase 12</summary>

```text
CONTEXT:
- Phases 1-11 merged. Documentation goes through docs-writer; the changelog fragment through mechanical-fixer.

TASK:
Write the two justification files, the feature page, the README section and the changelog fragment.

KEY RULES:
- Justification files follow the template of AGENTS.md section 9; English only; coverage figures only through covref markers.

REFERENCE FILES:
- src/Encina.DomainModeling/README.md
- changelog.d/README.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Provision | What it asks of this feature |
|--------|-----------|------------------------------|
| GDPR | Art. 5(1)(f), 5(2) | Integrity of records that must not change; accountability for refusals and bypasses |
| SPEC-002 | REQ-006, REQ-019 | REQ-006 (as read in the #1187 plan): immutable records are kept apart from erasable data, so their class must be enforceable; REQ-019: any opt-out explicit and logged |
| #1187 plan | OD-5 (2026-09-23) | Marker plus decorator as a follow-up on all 10 providers; retention-level freeze already in P-01 |
| ADR-062 | Error category contract | The category of `Repository.ImmutableRecord` (Design Choice 6) |
| AGENTS.md | Sections 3 and 5 | Fail closed, no exceptions for business logic; one decorator, provider-specific code limited to registration |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| Repository contract | `src/Encina.DomainModeling/FunctionalRepository.cs` | The interface the decorator implements |
| Errors, log, metrics | `src/Encina.DomainModeling/RepositoryErrors.cs`, `Diagnostics/RepositoryLog.cs`, `Diagnostics/RepositoryMetrics.cs` | Error code, EventIds 1104-1106, counters |
| Provider registrations | `src/Encina.*/ServiceCollectionExtensions.cs` (`AddEncinaRepository<TEntity, TId>`, `AddEncinaSoftDeleteRepository<TEntity, TId>`) and `src/Encina.*/Tenancy/TenancyServiceCollectionExtensions.cs` (`AddTenantAwareRepository<TEntity, TId>`) | Fifteen wrapping points and two registration errors |
| Unit of Work | `src/Encina.ADO.*/UnitOfWork/UnitOfWorkADO.cs`, `Encina.Dapper.*/UnitOfWork/UnitOfWorkDapper.cs`, `Encina.EntityFrameworkCore/UnitOfWork/UnitOfWorkEF.cs`, `Encina.MongoDB/UnitOfWork/UnitOfWorkMongoDB.cs` | Second write path |
| Bulk operations | `BulkOperationsADO`, `BulkOperationsPostgreSQL`, `BulkOperationsMySQL`, `BulkOperationsDapper` (x3), `BulkOperationsEFSqlServer`, `BulkOperationsEFPostgreSql`, `BulkOperationsEFMySql`, `BulkOperationsMongoDB` | Third write path |
| Soft delete | `src/Encina.EntityFrameworkCore/SoftDelete/SoftDeleteRepositoryEF.cs`, `src/Encina.MongoDB/SoftDelete/SoftDeletableFunctionalRepositoryMongoDB.cs` | Refused for marked types at registration |
| OpenTelemetry decorators | `src/Encina.OpenTelemetry/Repository/InstrumentedFunctionalRepository.cs`, `BulkOperations/InstrumentedBulkOperations.cs` | Applied by the application outside the guard (Design Choice 7) |
| Operation audit | `src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs`, `AddEncinaAudit` | Adapter target and registration |
| Coverage manifests | `.github/coverage-manifest/Encina.DomainModeling.json`, `Encina.Security.Audit.json` and the provider manifests | Per-file, per-flag targets |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.DomainModeling` | 1104-1106 inside `DomainModelingRepository` 1100-1199 | 1104 `ImmutableRecordWriteRefused` (Warning, Phase 1), 1105 `ImmutableRecordBypassedDelete` (Warning) and 1106 `ImmutableRecordBypassAuditFailed` (Error, Phase 2); free on `main`; the batch EventId registry row is added by the orchestrator |
| `Encina.Security.Audit` | none | No new message; the adapter returns the store's `Either` |

### Estimated File Count

| Pull request | Phase | Files | Lines (about) | Contents |
|--------------|-------|-------|---------------|----------|
| PR 1 | 1 | 14 | 1,050 | marker, decorator, guard, observer port, error, log, metrics; PublicAPI; manifest; 5 test files |
| PR 2 | 2 | 10 | 750 | bypass, decorator, log, metrics; PublicAPI; manifest; 4 test files |
| PR 3 | 3 | 7 | 450 | audit observer, registration; PublicAPI; manifest; 3 test files |
| PR 4 | 4 | 11 | 900 | EF registration, Unit of Work, 3 bulk classes; manifest; 5 test files |
| PR 5 | 5 | 8 | 600 | MongoDB registration, tenancy registration, Unit of Work, bulk; manifest; 3 test files |
| PR 6 | 6 | 8 | 600 | ADO SqlServer, same shape |
| PR 7 | 7 | 8 | 600 | ADO PostgreSQL, same shape |
| PR 8 | 8 | 8 | 600 | ADO MySQL, same shape |
| PR 9 | 9 | 8 | 600 | Dapper SqlServer, same shape |
| PR 10 | 10 | 8 | 600 | Dapper PostgreSQL, same shape |
| PR 11 | 11 | 8 | 600 | Dapper MySQL, same shape |
| PR 12 | 12 | 5 | 300 | 2 justification files, feature page, README, changelog fragment |
| **Total** | | **103** | **7,650** | 103 file changes in 12 pull requests; the DomainModeling manifest, PublicAPI and several DomainModeling files change in more than one pull request, so fewer distinct files are touched |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library, pre-1.0, Railway Oriented Programming, no compatibility layers. Rules: AGENTS.md.
- Issue #1278 adds IImmutableRecord and ImmutableRecordGuardRepository (decision OD-5 of 2026-09-23) on all 10 providers.
  The Maintainer Decisions of the plan override its recommendations.

IMPLEMENTATION OVERVIEW:
1. DomainModeling: marker, decorator, cached guard, observer port, coded error with an ADR-062 category, EventId 1104, refusals counter.
2. DomainModeling: type-bound bypass scope audited through the asynchronous observer port, EventIds 1105-1106, bypasses counter.
3. Security.Audit: the observer adapter registered by AddEncinaAudit.
4. EF Core, MongoDB, ADO x3 and Dapper x3, one pull request per package: wrap AddEncinaRepository and AddTenantAwareRepository,
   the Unit of Work, refuse bulk update/delete/merge inside each bulk class, soft-delete registration error.
5. Justification files and documentation.

KEY PATTERNS:
- A refused update or delete returns Repository.ImmutableRecord as a Left and never reaches the inner repository; adds,
  corrections and reads pass.
- Wrapping is automatic from the marker, order-independent, outermost of Encina's wrappers.
- A bypassed delete is type-bound, logged, counted and audited first; it fails closed without an observer or on an audit Left.
- Shared [Collection] fixtures for integration tests; async database calls with a CancellationToken; CRAP <= 10 on changed methods.

REFERENCE FILES:
- src/Encina.DomainModeling/FunctionalRepository.cs
- src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs
- src/Encina.ADO.SqlServer/Tenancy/TenancyServiceCollectionExtensions.cs
- docs/plans/retention-floor-implementation-plan-1187.md
- .github/coverage-manifest/Encina.DomainModeling.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | The marker check is cached per type in memory; no data read path to cache |
| 2 | OpenTelemetry | ✅ | Phases 1-2: counters `encina.repository.immutable_record.refusals` and `.bypasses` with `entity_type` and `operation` tags; refusals also appear in the application's repository instrumentation (Design Choice 7) |
| 3 | Structured Logging | ✅ | Phases 1-2: `[LoggerMessage]` 1104-1106 inside `DomainModelingRepository`, error code and entity type only |
| 4 | Health Checks | ❌ | No checkable dependency |
| 5 | Validation | ❌ | The bypass reason is a guarded argument; no input pipeline involved |
| 6 | Resilience | ❌ | No external call |
| 7 | Distributed Locks | ❌ | Stateless decorator |
| 8 | Transactions | ❌ | A refusal changes nothing; adds and bypassed deletes use the inner repository's own unit of work |
| 9 | Idempotency | ❌ | Not a message entry point |
| 10 | Multi-Tenancy | ✅ | Phases 5-11: the tenant-aware registrations are wrapped too; the decorator itself is tenant-agnostic (Design Choice 7) |
| 11 | Module Isolation | ❌ | No module scoping required |
| 12 | Audit Trail | ✅ | Phases 2-3: an `OperationAuditEntry` per refusal and per bypassed delete, written before the delete (Design Choice 10) |

---

## Prerequisites & Dependencies

### Required Prerequisites

- None open. The registration points, the Unit of Work classes, the bulk classes and the soft-delete registrations exist on `origin/main`.

### Recommended (Not Blocking)

- #1187 (retention floors): its eraser is the intended user of the bypass scope (Design Choice 3); its plan records the dependency.
- #3420 (host answers to compliance denials): re-check the category of Design Choice 6 against it.
- #1778 (entity-change audit store): unrelated to the refusal audit, which uses the operation audit store.

---

## Next Steps

1. Link this plan from #1278 and #1187.
2. When #1278 is picked up, re-check each Design Choice against the code; present the Dilemmas to the maintainer and record the answers in `## Maintainer Decisions`; re-count the file lists of the phases a decision changes (Design Choices 2, 9 and 11).
3. Ship Phases 1 to 12 as separate pull requests referencing #1278; the last one carries `Fixes #1278`.
