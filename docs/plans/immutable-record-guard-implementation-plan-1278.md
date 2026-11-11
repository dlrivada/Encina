# Implementation Plan: Data-Level Immutable-Record Guard on the 10 Database Providers

> **Issue**: [#1278](https://github.com/dlrivada/Encina/issues/1278)
> **Type**: Feature
> **Complexity**: Medium-High (5 phases across `Encina.DomainModeling` and 8 provider packages, no new package, about 40 files in 5 pull requests)
> **Estimated Scope**: ~800-1,100 lines of production code + ~1,800-2,300 lines of tests
> **Origin**: follow-up of #1187 (retention floors), decision OD-5 of 2026-09-23
> **Depends on**: nothing open; #1187 is related (its eraser needs the bypass of Design Choice 3)

---

## Summary

An `IImmutableRecord` marker interface in `Encina.DomainModeling` and a provider-neutral decorator `ImmutableRecordGuardRepository<TEntity, TId>` over `IFunctionalRepository<TEntity, TId>` that refuses every update and delete of an entity that implements the marker, returning `Left` and changing nothing. A correction is a new record added through `AddAsync` and linked by the application. The decision (P-01 OD-5, 2026-09-23) is that the retention-level freeze of #1187 is not enough for direct repository writes, so the guard sits at the repository boundary for all providers.

**What exists today** (verified on `origin/main`): `IFunctionalRepository<TEntity, TId>` (`src/Encina.DomainModeling/FunctionalRepository.cs`) has `AddAsync`, `UpdateAsync`, `DeleteAsync(TId)`, `DeleteAsync(TEntity)`, `AddRangeAsync`, `UpdateRangeAsync`, `DeleteRangeAsync` and `UpdateImmutableAsync` (the last updates entities that are C# immutable records through `with`; the name does not mean the marker of this issue). Eight packages register it in `AddEncinaRepository<TEntity, TId>` with a single `new FunctionalRepository...` in a scoped factory (`Encina.ADO.SqlServer|PostgreSQL|MySQL`, `Encina.Dapper.SqlServer|PostgreSQL|MySQL`, `Encina.EntityFrameworkCore`, `Encina.MongoDB`). Tenant-aware repositories exist (`src/Encina.ADO.SqlServer/Tenancy/TenantAwareFunctionalRepositoryADO.cs` and siblings). Two other paths reach the same rows without the registered repository: `IUnitOfWork.Repository<TEntity, TId>()` creates repositories directly in `UnitOfWorkADO`, `UnitOfWorkDapper`, `UnitOfWorkEF` and `UnitOfWorkMongoDB`, and `IBulkOperations<TEntity>` (`BulkUpdateAsync`, `BulkDeleteAsync`, `BulkMergeAsync`) is implemented per provider. `RepositoryErrors` holds the `Repository.{Category}` codes; `RepositoryLog` uses EventIds 1100-1103 of `DomainModelingRepository` 1100-1199. Nothing named `IImmutableRecord` or `ImmutableRecordGuard` exists in `src/`.

**What this plan delivers**

1. `IImmutableRecord`, the decorator, a cached per-type check, `RepositoryErrors.ImmutableRecord<TEntity>(operation)` (code `Repository.ImmutableRecord`) and a metric and log for refusals, all in `Encina.DomainModeling`.
2. Automatic wrapping in the eight `AddEncinaRepository` registrations when the entity implements the marker (Design Choice 1), and the same guard on the Unit of Work repositories.
3. The guard on bulk update, delete and merge (Design Choice 2) and an explicit, logged bypass scope for the lawful end-of-life deletion (Design Choice 3).
4. An audit entry for each refused write through a port with an adapter in `Encina.Security.Audit`.
5. Tests on all 10 providers.

**Out of scope**: a lock-after-signing lifecycle (a record that is editable until a moment and immutable after it is an instance-level state; this issue is the type-level marker, and a draft is modelled as a different entity type); database triggers (rejected: 10 dialects, untestable in unit tests); raw `DbContext`, SQL or driver writes, which no repository decorator can stop (documented, with the recommended permissions, in the feature page); the retention-level freeze itself (#1187).

**Standards**: GDPR Art. 5(1)(f) integrity and Art. 5(2) accountability; SPEC-002 REQ-006 and REQ-019 (see Research); AGENTS.md section 5 (provider coherence).

**Affected packages**: `Encina.DomainModeling`, `Encina.ADO.SqlServer`, `Encina.ADO.PostgreSQL`, `Encina.ADO.MySQL`, `Encina.Dapper.SqlServer`, `Encina.Dapper.PostgreSQL`, `Encina.Dapper.MySQL`, `Encina.EntityFrameworkCore`, `Encina.MongoDB`, `Encina.Security.Audit` (the audit adapter).

**Provider category**: Database (10): ADO.NET x3, Dapper x3, EF Core x3 (one registration serves the three EF databases), MongoDB. The logic lives in the decorator, so provider-specific code is limited to registration and the Unit of Work and bulk wrappers.

---

## Design Choices

Three Dilemmas are open (1 to 3); the maintainer answers them when work on the issue starts. Choices 4 to 7 are Settled or Trivial.

<details>
<summary><strong>1. How the guard is applied to an entity</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Automatic.** Each provider's `AddEncinaRepository<TEntity, TId>` wraps the repository it creates when `typeof(IImmutableRecord).IsAssignableFrom(typeof(TEntity))` (the check is cached per type) | The marker on the type is the whole declaration; cannot be forgotten; independent of registration order, as AGENTS.md requires | Eight registration points change; an entity type that cannot implement the interface (third party) cannot be guarded |
| **B) Explicit opt-in** `services.AddEncinaImmutableRecordGuard<TEntity, TId>()` that decorates the registered descriptor | Works for types that cannot carry the marker; the issue's wording "registered per entity" | Order-dependent: it must run after `AddEncinaRepository`, which the registration rule forbids relying on; a forgotten call leaves the entity unguarded |

### Chosen Option: **A — Automatic wrapping from the marker** (recommended, pending the maintainer)

### Rationale

- Immutability is a property of the record type; making the type declare it and the registration honor it leaves no unguarded configuration. "Registered per entity" is satisfied because each entity registers its own repository.
- B wins when the entities to protect are generated or third-party types that cannot implement `IImmutableRecord`; an extension for that case can be added later without changing A.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Bypass paths: Unit of Work repositories and bulk operations</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Guard all three Encina write paths.** The registered repository, the repositories created by `IUnitOfWork.Repository<T,TId>()` (wrapped with the same decorator) and `IBulkOperations<T>` update, delete and merge (refused for marked types; insert is allowed) | The acceptance criterion "through any Encina repository ... returns `Left` and changes nothing" holds on every Encina path | About eight more files (four Unit of Work classes, the bulk classes of the six providers that have them) and a second wrapper to keep in step |
| **B) Registered repository only.** Document that the Unit of Work and bulk operations are not guarded | Smallest change | An application using the Unit of Work or `BulkUpdateAsync` silently rewrites an immutable record through Encina itself |

### Chosen Option: **A — Guard the repository, the Unit of Work and the bulk operations** (recommended, pending the maintainer)

### Rationale

- A guard that a documented Encina API walks around does not enforce immutability at "the repository boundary". Raw `DbContext` or SQL stay outside, and the feature page says so.
- B wins if the maintainer wants the first pull request small and accepts a documented gap until the Unit of Work and bulk follow-ups.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Lawful end-of-life deletion of an immutable record</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) An explicit, logged bypass scope.** `ImmutableRecordGuardBypass.Begin(reason)` (scoped, `AsyncLocal`, requires a non-blank reason) lets the decorator pass deletes in that scope only; each use is logged, counted and audited; only the retention eraser (#1187) is meant to call it | Retention expiry and erasure after the floor can delete through the same repository; the opt-out is explicit and logged as AGENTS.md requires | A new public concept that application code could misuse |
| **B) No bypass.** The eraser deletes through the provider's raw access | Smallest public surface | The eraser needs provider-specific code on 10 providers, which is what the decorator was meant to avoid; deletion then escapes the audit |

### Chosen Option: **A — Explicit logged bypass scope** (recommended, pending the maintainer)

### Rationale

- The #1187 plan records that "deletion at the end of the period must bypass the guard (the eraser uses its own path)"; A gives that path one audited shape. Only a delete passes the bypass: an update of an immutable record is never allowed.
- B wins if the maintainer prefers that the guard has no escape hatch at all and the eraser is built on raw provider access.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Marker plus provider-neutral decorator over `IFunctionalRepository`</strong></summary>

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
<summary><strong>5. Refusal is a coded `Left`; the check is type-level and cached</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `RepositoryErrors.ImmutableRecord<TEntity>(operation)` with code `Repository.ImmutableRecord`, a fixed message naming the entity type and operation (no entity values); the marker check is a static generic cache per entity type** | ROP; no exception; zero per-call reflection | None significant |
| **B) Throw `InvalidOperationException`** | Familiar | Exceptions for business logic are forbidden |

### Chosen Option: **A — Coded `Left`, cached check**

### Rationale

- The error follows the `Repository.{Category}` pattern of `RepositoryErrors`.

**Class:** Settled
**Settled by:** AGENTS.md section 3 (Railway Oriented Programming); ADR-001, ADR-006; `src/Encina.DomainModeling/RepositoryErrors.cs` (`Repository.{Category}` codes)

</details>

<details>
<summary><strong>6. Wrapping order with tenant-aware repositories</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The guard is the outermost wrapper** (above the tenant-aware repository) | A refusal never touches the database, even for a missing tenant; the guard stays tenant-agnostic | None significant |
| **B) The guard inside the tenant-aware repository** | Tenant checks first | A tenant error can mask the refusal; the guard sees pre-filtered calls |

### Chosen Option: **A — Outermost wrapper**

### Rationale

- The refusal depends only on the entity type and the operation, so it needs nothing from the tenant layer; tests cover the tenant-aware and plain variants.

**Class:** Trivial
**Settled by:** AGENTS.md section 5 (provider coherence); `src/Encina.ADO.SqlServer/Tenancy/TenantAwareFunctionalRepositoryADO.cs` (the existing wrapper this sits above)

</details>

<details>
<summary><strong>7. EventIds, telemetry and audit port</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) EventIds 1104-1106 inside the existing `DomainModelingRepository` range (1104 refusal, 1105 bypass begun, 1106 bypass denied for a blank reason); a counter `encina.repository.immutable_record.refusals` (tags `entity_type`, `operation`) and `encina.repository.immutable_record.bypasses` on the existing repository meter, ADR-054 names; a port `IImmutableRecordGuardObserver` in `Encina.DomainModeling` with an adapter in `Encina.Security.Audit` that writes an `OperationAuditEntry`** | No new range; `DomainModeling` stays free of audit references (the audit package already references it) | The adapter lives in another package |
| **B) Call the audit store from `Encina.DomainModeling`** | One package | A new reference from the data layer to Security.Audit (it references DomainModeling: a cycle) |

### Chosen Option: **A — Existing range, ADR-054 names, observer port**

### Rationale

- Phase 1 confirms by grep and `EventIdRanges.GetAllRanges()` that 1104-1106 are free.

**Class:** Trivial
**Settled by:** AGENTS.md section 7 and ADR-021 (ids inside a package's existing range); ADR-054 settled point 4; ADR-036; `src/Encina.Security.Audit/Encina.Security.Audit.csproj` (references `Encina.DomainModeling`)

</details>

---

## Implementation Phases

Each phase is one pull request of at most 15 files and 1500 changed lines.

### Phase 1: Marker, Decorator, Error, Bypass, Metrics and Logs (DomainModeling)

> **Goal**: The guard works on any `IFunctionalRepository`. **Size**: about 13 files, 900 lines.

<details>
<summary>Tasks</summary>

Namespace `Encina.DomainModeling`; folder `ImmutableRecords/`.

1. `IImmutableRecord.cs`: marker interface with XML docs (type-level; a correction is a new record).
2. `ImmutableRecordGuardRepository.cs` (public sealed, `IFunctionalRepository<TEntity, TId>`, `where TEntity : class, IImmutableRecord`? The decorator is generic over any `TEntity` and checks the cached marker at construction, so registrations can wrap conditionally; for a non-marked type it is never created): constructor takes the inner repository and an optional `IImmutableRecordGuardObserver?`; refuses `UpdateAsync`, `UpdateRangeAsync`, `UpdateImmutableAsync`, `DeleteAsync` (both) and `DeleteRangeAsync` before calling the inner repository; delegates `AddAsync`, `AddRangeAsync` and all reads.
3. `ImmutableRecordGuard.cs` (public static): `IsImmutable<TEntity>()` (static generic cache), `Wrap<TEntity, TId>(inner, observer)` returning the inner repository itself for unmarked types.
4. `ImmutableRecordGuardBypass.cs` (public static, Design Choice 3): `Begin(string reason)` returns an `IDisposable` scope over an `AsyncLocal`; blank reason returns a `Left`-style failure through `TryBegin` (the API is `Either<EncinaError, IDisposable>`); the decorator lets only deletes pass inside a scope and reports each use to the observer.
5. `IImmutableRecordGuardObserver.cs`: `void Refused(Type entityType, string operation)` and `void BypassBegun(Type entityType, string reason)` (synchronous, never throws; the audit adapter queues its write).
6. `RepositoryErrors.cs`: `ImmutableRecord<TEntity>(string operation)`; `RepositoryLog.cs`: 1104-1106; `RepositoryMetrics.cs`: the two counters; `PublicAPI.Unshipped.txt`.
7. Tests: decorator (every refused method returns `Left` and the inner mock is never called; adds and reads delegate), guard cache, bypass (delete passes in scope, update never, blank reason refused, scope ends on dispose and across `await`), metrics and log tests; guard tests for every public member; property test (FsCheck): no sequence of update or delete calls on a marked entity succeeds without a bypass scope; contract tests; manifest targets in `.github/coverage-manifest/Encina.DomainModeling.json`.

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT:
- Issue #1278; plan docs/plans/immutable-record-guard-implementation-plan-1278.md. Maintainer Decisions override the "recommended" choices.
- IFunctionalRepository is in src/Encina.DomainModeling/FunctionalRepository.cs; errors in RepositoryErrors.cs; logs in Diagnostics/RepositoryLog.cs (ids 1100-1103 used).

TASK:
Create IImmutableRecord, the decorator, the cached guard, the bypass scope, the observer port, the error, the log messages 1104-1106
and the counters with their tests. At most 15 files and 1500 lines.

KEY RULES:
- A refused call never reaches the inner repository; the error is a coded Left with no entity values.
- UpdateImmutableAsync (C# immutable-record update) is refused as well; the name has nothing to do with the marker.
- No static mutable state except the generic type cache and the AsyncLocal bypass; XML docs on all public members; RS0016 must not fire.

REFERENCE FILES:
- src/Encina.DomainModeling/FunctionalRepository.cs
- src/Encina.DomainModeling/RepositoryErrors.cs
- src/Encina.DomainModeling/Diagnostics/RepositoryLog.cs and RepositoryMetrics.cs
```

</details>

---

### Phase 2: EF Core and MongoDB — Registration, Unit of Work and Bulk Operations

> **Goal**: Two provider families are guarded on every Encina write path. **Size**: about 13 files, 1000 lines.

<details>
<summary>Tasks</summary>

1. `Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs` and `Encina.MongoDB/ServiceCollectionExtensions.cs`: `AddEncinaRepository<TEntity, TId>` wraps the repository (the tenant-aware one when tenancy is on) with `ImmutableRecordGuard.Wrap(...)`; the observer is resolved optionally.
2. `UnitOfWorkEF.Repository<TEntity, TId>()` and `UnitOfWorkMongoDB.Repository<TEntity, TId>()` wrap the repository they create the same way.
3. `BulkOperationsEF` and `BulkOperationsMongoDB`: `BulkUpdateAsync`, `BulkDeleteAsync` and `BulkMergeAsync` return the coded `Left` for marked types (before any database call); `BulkInsertAsync` and `BulkReadAsync` are unchanged.
4. Tests: guard tests; integration tests on EF SqlServer, PostgreSQL, MySQL and MongoDB with shared collection fixtures (an update and a delete through the registered repository, through the Unit of Work and through bulk operations return `Left` and the row is unchanged; an add and a correction record succeed; a delete inside a bypass scope succeeds); contract tests; manifest targets for the touched files.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT:
- Phase 1 is merged. Plan Design Choices 1, 2 and 6 (Maintainer Decisions override the recommendations).

TASK:
Wrap the EF Core and MongoDB repositories in registration and in the Unit of Work, and refuse bulk update/delete/merge for marked types, with tests.
At most 15 files and 1500 lines.

KEY RULES:
- Database calls asynchronous with a CancellationToken; the refusal happens before any call.
- Integration tests use [Collection("EFCore-SqlServer")] and siblings, ClearAllDataAsync in InitializeAsync; never IClassFixture or per-class containers.
- Registration must work in any order and not override an application's own registration.

REFERENCE FILES:
- src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs
- src/Encina.EntityFrameworkCore/UnitOfWork/UnitOfWorkEF.cs
- src/Encina.MongoDB/BulkOperations/BulkOperationsMongoDB.cs
```

</details>

---

### Phase 3: ADO.NET (SqlServer, PostgreSQL, MySQL)

> **Goal**: The ADO providers are guarded on every Encina write path. **Size**: about 12 files, 900 lines.

<details>
<summary>Tasks</summary>

1. `Encina.ADO.SqlServer|PostgreSQL|MySQL/ServiceCollectionExtensions.cs`: wrap in `AddEncinaRepository<TEntity, TId>` (the factory that builds `FunctionalRepositoryADO`, or the tenant-aware class).
2. `UnitOfWorkADO.Repository<TEntity, TId>()` in the three packages wraps the repository it creates (`UnitOfWorkRepositoryADO`).
3. `BulkOperationsADO` where it exists (SqlServer today; PostgreSQL and MySQL when their bulk classes exist, otherwise note it in the PR) refuses bulk update, delete and merge for marked types.
4. Tests: guard tests; integration tests with `[Collection("ADO-SqlServer")]`, `[Collection("ADO-PostgreSQL")]`, `[Collection("ADO-MySQL")]` with the same cases as Phase 2; contract tests across the three; manifest targets.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT:
- Phases 1-2 merged. The ADO registration builds FunctionalRepositoryADO in a scoped factory.

TASK:
Wrap the ADO repositories in registration and in the Unit of Work, guard the bulk operations that exist, and add the tests.
At most 15 files and 1500 lines.

KEY RULES:
- Identical behavior and error code on the three providers; only registration code differs.
- Shared collection fixtures only; async database calls with a CancellationToken.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs
- src/Encina.ADO.SqlServer/UnitOfWork/UnitOfWorkADO.cs
- src/Encina.ADO.SqlServer/BulkOperations/BulkOperationsADO.cs
```

</details>

---

### Phase 4: Dapper (SqlServer, PostgreSQL, MySQL)

> **Goal**: The Dapper providers are guarded on every Encina write path. **Size**: about 12 files, 900 lines.

<details>
<summary>Tasks</summary>

1. `Encina.Dapper.SqlServer|PostgreSQL|MySQL/ServiceCollectionExtensions.cs`: wrap in `AddEncinaRepository<TEntity, TId>`.
2. `UnitOfWorkDapper.Repository<TEntity, TId>()` in the three packages wraps the repository it creates.
3. `BulkOperationsDapper` in the three packages refuses bulk update, delete and merge for marked types.
4. Tests: guard tests; integration tests with `[Collection("Dapper-SqlServer")]`, `[Collection("Dapper-PostgreSQL")]`, `[Collection("Dapper-MySQL")]` (cases as Phase 2); contract tests across the three; manifest targets.

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```text
CONTEXT:
- Phases 1-3 merged; the Dapper packages mirror the ADO ones.

TASK:
Wrap the Dapper repositories in registration and in the Unit of Work, guard the bulk operations, and add the tests.
At most 15 files and 1500 lines.

KEY RULES:
- Identical behavior and error code on the three providers; shared collection fixtures only.

REFERENCE FILES:
- src/Encina.Dapper.SqlServer/ServiceCollectionExtensions.cs
- src/Encina.Dapper.SqlServer/UnitOfWork/UnitOfWorkDapper.cs
- src/Encina.Dapper.SqlServer/BulkOperations/BulkOperationsDapper.cs
```

</details>

---

### Phase 5: Audit Adapter, Cross-Provider Tests and Documentation

> **Goal**: Refusals are audited, all 10 providers are proven equal and the feature is documented. **Size**: about 10 files, 700 lines.

<details>
<summary>Tasks</summary>

1. `src/Encina.Security.Audit/ImmutableRecordGuardAuditObserver.cs`: implements `IImmutableRecordGuardObserver`, writes an `OperationAuditEntry` per refusal and per bypass (`IOperationAuditStore.RecordAsync`, awaited off the caller's path through the audit pipeline conventions; an audit failure is logged by error code and never changes the refusal); registered by the audit package's `AddEncina...` with `TryAdd`; tests.
2. Cross-provider contract test in `tests/Encina.ContractTests/`: the same cases against every provider's repository registration, Unit of Work and bulk class (the ten providers behave identically).
3. Telemetry: in-memory exporter test of the two counters; load and benchmark `.md` justifications (`tests/Encina.LoadTests/DomainModeling/ImmutableRecordGuard.md` and the benchmark sibling: the decorator is a type check and a return).
4. Documentation by the docs-writer agent: `docs/features/` page "Immutable records" (marker, corrections as linked records, the bypass scope, what the guard cannot stop), package README of `Encina.DomainModeling`, XML docs with examples, `changelog.d/1278-immutable-record-guard.added.md`; ROADMAP and INVENTORY when affected. An ADR is optional (the choices follow OD-5); the feature page cites it.
5. `dotnet build --configuration Release` 0 warnings; `dotnet test` green; per-flag coverage at the manifest targets of the ten packages; CRAP of every new or changed method at most 10.

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```text
CONTEXT:
- Phases 1-4 merged. Documentation goes through docs-writer; the changelog fragment and PublicAPI lines through mechanical-fixer.

TASK:
Write the audit observer, the cross-provider contract test, the telemetry test, the two justification files and the documentation.

KEY RULES:
- An audit failure never changes the refusal; no entity values in the audit entry or any tag.
- Justification files follow the template of AGENTS.md section 9; English only; coverage figures only through covref markers.

REFERENCE FILES:
- src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs
- tests/Encina.ContractTests/ (an existing cross-provider repository contract test)
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
| AGENTS.md | Section 5 | One decorator, provider-specific code limited to registration |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| Repository contract | `src/Encina.DomainModeling/FunctionalRepository.cs` | The interface the decorator implements |
| Errors, log, metrics | `src/Encina.DomainModeling/RepositoryErrors.cs`, `Diagnostics/RepositoryLog.cs`, `Diagnostics/RepositoryMetrics.cs` | Error code, EventIds 1104-1106, counters |
| Provider registrations | `src/Encina.*/ServiceCollectionExtensions.cs` (`AddEncinaRepository<TEntity, TId>`) | Eight wrapping points |
| Unit of Work | `src/Encina.ADO.*/UnitOfWork/UnitOfWorkADO.cs`, `Encina.Dapper.*/UnitOfWork/`, `Encina.EntityFrameworkCore/UnitOfWork/UnitOfWorkEF.cs`, `Encina.MongoDB/UnitOfWork/UnitOfWorkMongoDB.cs` | Second write path |
| Bulk operations | `src/Encina.*/BulkOperations/BulkOperations*.cs` | Third write path |
| Tenant-aware repositories | `src/Encina.ADO.SqlServer/Tenancy/TenantAwareFunctionalRepositoryADO.cs` and siblings | Wrapped from outside |
| Operation audit | `src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs` | Adapter target |
| Coverage manifests | `.github/coverage-manifest/Encina.DomainModeling.json` and the provider manifests | Per-file, per-flag targets |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.DomainModeling` | 1104-1106 inside `DomainModelingRepository` 1100-1199 | Existing range; 1104 `ImmutableRecordWriteRefused` (Warning), 1105 `ImmutableRecordBypassBegun` (Warning), 1106 `ImmutableRecordBypassDenied` (Warning); verify the tail is free in Phase 1 |
| `Encina.Security.Audit` | existing range | No new message planned; the adapter logs by existing conventions |
| Plan family block | 6600-6649 | Not used by this plan (assigned to #687, #688) |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Source: DomainModeling (marker, decorator, guard, bypass, observer, error, log, metrics) | 8 | Phase 1 |
| Source: 8 provider packages (registration, Unit of Work, bulk) | 22 | Edits to existing files |
| Source: audit adapter | 2 | Phase 5 |
| Tests (unit, guard, contract, property, integration) | 22 | Integration across 10 providers in shared collections |
| Justification files, manifests, PublicAPI | 9 | Manifests of ten packages are edited across the phases |
| Documentation and changelog | 3 | Docs through docs-writer |
| **Total** | **~66** | In 5 pull requests of at most 15 files |

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
1. DomainModeling: marker, decorator, cached guard, logged bypass scope, observer port, coded error, EventIds 1104-1106, counters.
2. EF Core and MongoDB: wrap in AddEncinaRepository, in the Unit of Work and refuse bulk update/delete/merge for marked types.
3. ADO x3 and Dapper x3: the same wrapping; identical behavior across providers.
4. Audit adapter in Encina.Security.Audit, cross-provider contract and telemetry tests, justification files, documentation.

KEY PATTERNS:
- A refused update or delete returns Repository.ImmutableRecord as a Left and never reaches the inner repository; adds, corrections and reads pass.
- Wrapping is automatic from the marker, order-independent, outermost above the tenant-aware repository.
- The bypass scope is explicit, logged, counted and audited, and passes only deletes.
- Shared [Collection] fixtures for integration tests; async database calls with a CancellationToken; CRAP <= 10 on changed methods.

REFERENCE FILES:
- src/Encina.DomainModeling/FunctionalRepository.cs
- src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs
- docs/plans/retention-floor-implementation-plan-1187.md
- .github/coverage-manifest/Encina.DomainModeling.json
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | The marker check is cached per type in memory; no data read path to cache |
| 2 | OpenTelemetry | ✅ | Phase 1: counters `encina.repository.immutable_record.refusals` and `.bypasses` with `entity_type` and `operation` tags |
| 3 | Structured Logging | ✅ | Phase 1: `[LoggerMessage]` 1104-1106 inside `DomainModelingRepository`, error code and entity type only |
| 4 | Health Checks | ❌ | No checkable dependency |
| 5 | Validation | ❌ | The bypass reason is validated in place; no input pipeline involved |
| 6 | Resilience | ❌ | No external call |
| 7 | Distributed Locks | ❌ | Stateless decorator |
| 8 | Transactions | ❌ | A refusal changes nothing; adds and bypassed deletes use the inner repository's own unit of work |
| 9 | Idempotency | ❌ | Not a message entry point |
| 10 | Multi-Tenancy | ❌ | The decorator is tenant-agnostic and sits above the tenant-aware repository (Design Choice 6) |
| 11 | Module Isolation | ❌ | No module scoping required |
| 12 | Audit Trail | ✅ | Phase 5: an `OperationAuditEntry` per refusal and per bypass through the observer port and the Security.Audit adapter |

---

## Prerequisites & Dependencies

### Required Prerequisites

- None open. The registration points, the Unit of Work classes and the bulk classes exist on `origin/main`.

### Recommended (Not Blocking)

- #1187 (retention floors): its eraser is the intended user of the bypass scope (Design Choice 3); its plan records the dependency.
- #1778 (entity-change audit store): unrelated to the refusal audit, which uses the operation audit store.

---

## Next Steps

1. Link this plan from #1278 and #1187.
2. When #1278 is picked up, re-check each Design Choice against the code; present the three Dilemmas to the maintainer and record the answers in `## Maintainer Decisions`.
3. Ship Phases 1 to 5 as separate pull requests referencing #1278; the last one carries `Fixes #1278`.
