# Implementation Plan: `Encina.Security.Audit` — Evidential Read Audit

> **Issue**: [#1193](https://github.com/dlrivada/Encina/issues/1193) (SPEC-002 tracking id **P-05**, priority P0)
> **Type**: Feature
> **Complexity**: High (10 phases, 10 database providers + Marten, ~70 files)
> **Estimated Scope**: ~2,500-3,300 lines of production code + ~2,800-3,600 lines of tests
> **Milestone**: v0.17.0 — Compliance Lifecycle
> **Parent EPIC**: [#1186](https://github.com/dlrivada/Encina/issues/1186) — EU regulatory readiness (SPEC-002)
> **Specification**: [SPEC-002](../specifications/SPEC-002-eu-regulatory-readiness.md) REQ-007, AC-007, scenario S13; cross-cutting REQ-061 (AC-043) and REQ-062 (AC-044)
> **Depends on**: [#1135](https://github.com/dlrivada/Encina/issues/1135) (EF audit stores let provider exceptions escape; open); #1128 and #1129 (closed)
> **Related**: [#1194](https://github.com/dlrivada/Encina/issues/1194) (P-06, query-level read audit, builds on this entry shape), [#798](https://github.com/dlrivada/Encina/issues/798) (tenancy filtering for Security.Audit), [#767](https://github.com/dlrivada/Encina/issues/767) (move `ReadAuditRetentionService` to Encina scheduling), [#751](https://github.com/dlrivada/Encina/issues/751) (ABAC decision audit), [#1189](https://github.com/dlrivada/Encina/issues/1189) (P-03, disclosure reads are read-audited), [read-auditing-implementation-plan-573.md](read-auditing-implementation-plan-573.md) (the original design)

---

## Summary

AEPD PD-00068-2026 [S] reads GDPR Art. 15 as entitling a patient to know who accessed their data, when and which data; EHDS Art. 9 [S] requires such access information from 26 March 2029. Encina's read audit cannot serve as that evidence today:

- **Fire-and-forget.** `AuditedRepository.LogReadAccessAsync` is started with `_ = …` and never awaited; failures are swallowed, and the `Either` returned by `IReadAuditStore.LogReadAsync` is ignored, so a `Left` still counts as "recorded". On EF Core the detached write runs concurrently with the caller on the same scoped `DbContext`, and `ReadAuditStoreEF.LogReadAsync` calls `SaveChangesAsync` on it (which also commits any pending change of the application).
- **No ids for collections.** `EntityId` is set only by `GetByIdAsync`; `GetAll`, `Find`, `FindOne` and both `GetPaged` overloads record `null`.
- **Purpose only warns.** `RequirePurpose` logs EventId 1702 and continues; `ReadAuditErrors.PurposeRequired` exists but is unused.
- **No subject, no category.** No layer (record, entity, SQL scripts, MongoDB document, Marten event, query) has a data-subject id or a data category; `GetUserAccessHistoryAsync` looks up by the accessor, not by the person whose data was read.
- **One retention for everything.** `ReadAuditOptions.RetentionDays = 365`, one global cutoff; Marten purges by destroying time-period keys.
- **The 10 providers are not covered.** `AuditedRepository` and `AuditedReadOnlyRepository` decorate `IRepository<TEntity,TId>` and `IReadOnlyRepository<TEntity,TId>`, which none of the 10 provider repositories implement (they implement `IFunctionalRepository<TEntity,TId>`), and nothing in `src/` registers the decorators. Reads through Encina's provider repositories are not audited at all.
- **Sampling.** `AuditReadsFor<TEntity>(samplingRate)` audits a random fraction of reads, which cannot serve as evidence.

This plan makes read audit evidential:

1. A **functional-repository decorator** (`AuditedFunctionalRepository<TEntity,TId>`) audits reads on all 10 providers and returns `Left` when the audit fails in fail-closed mode.
2. **Fail-closed option** (`ReadAuditFailureMode.FailClosed`): the entry is written before the data is returned; a store failure fails the read.
3. **One entry per returned entity**, written as one batch, with the entity id, the data-subject id and the data category.
4. **Purpose enforcement** with a reject mode that fails before the inner read runs.
5. **Per-subject access query** `GetAccessesForSubjectAsync` (who, when, which data, which purpose), tenant-scoped.
6. **Retention per data category**, with a 3-year default for categories the application declares special. **The 3-year default is a documented recommendation, not a legal requirement**: no rule applicable today fixes a period (EHDS Art. 9 applies only from 26 March 2029; AEPD PD-00068-2026 fixes none). XML documentation and README say so (REQ-007, INV-002).

**Standards**: GDPR Art. 5(2), Art. 15, Art. 30, Art. 32; AEPD PD-00068-2026 (13 July 2026) [S]; EHDS Reg. (EU) 2025/327 Art. 9 [S]; Ley 41/2002 art. 16 and Código Deontológico del Psicólogo art. 46 (access restricted to the professional) as the reason the reference application (a small Spanish psychology practice) needs the evidence.

**Affected packages**: `Encina.Security.Audit` (it already references `Encina.DomainModeling`, so the new decorator lives there); `Encina.ADO.{SqlServer,PostgreSQL,MySQL}`, `Encina.Dapper.{SqlServer,PostgreSQL,MySQL}`, `Encina.EntityFrameworkCore`, `Encina.MongoDB` (read-audit stores and schema); `Encina.Audit.Marten`.

**Provider category**: Database (10) plus Marten (`Encina.Audit.Marten`). `IReadAuditStore` already has implementations on all 10 providers and Marten; every change to its shape is made on all of them.

---

## Design Choices

<details>
<summary><strong>1. What Gets Audited — a decorator over <code>IFunctionalRepository</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep decorating `IRepository`/`IReadOnlyRepository`** | No new type | No provider implements those interfaces (only `SoftDeleteRepositoryEF` and the temporal repositories); the 10-provider read path stays unaudited; `Option`/list return types cannot carry a `Left` for fail-closed |
| **B) New `AuditedFunctionalRepository<TEntity,TId> : IFunctionalRepository<TEntity,TId>` (and `AuditedFunctionalReadRepository` for `IFunctionalReadRepository`), registered by `AddReadAuditedRepository<TEntity,TId>()` as a decorator of the registered repository** | Covers the 10 providers with one provider-neutral type; `Either` return types carry the fail-closed `Left` naturally | The two old decorators become redundant |
| **C) Provider-level auditing inside each `FunctionalRepository{Provider}`** | No decorator registration | 10 copies of the same logic; mixes audit into data access |

### Chosen Option: **B**

### Rationale

- The decorator audits `GetByIdAsync`, both `ListAsync`, `FirstOrDefaultAsync` and the three `GetPagedAsync`; `CountAsync` and `AnyAsync` expose no entity data and pass through (as today); writes pass through.
- Whether the old `AuditedRepository`/`AuditedReadOnlyRepository` are removed (pre-1.0, no users) or kept for `ISoftDeleteRepository`/`ITemporalRepository` users is OD-1; the plan removes them and adds the decorator for `ITemporalRepository` only if the maintainer wants temporal reads audited.
- Registration helper: `services.AddReadAuditedRepository<Patient, Guid>()` replaces the `IFunctionalRepository<Patient,Guid>` descriptor with a factory that wraps the previous one (no Scrutor dependency).

</details>

<details>
<summary><strong>2. Entry Granularity — one entry per returned entity, written as one batch</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One entry per read with the list of ids in `Metadata`** | One row per read | The per-subject query cannot use an index; a page of 50 patients hides who was read |
| **B) One entry per returned entity, all entries of one read written in one batch and linked by a `ReadOperationId`** | The per-subject query is an indexed lookup; matches S13 ("one read-audit entry per patient"); the batch keeps it to one round trip | More rows (a page of 50 = 50 rows) |
| **C) One entry per data subject with the entity ids of that subject** | Fewer rows when a subject has many entities in one read | Grouping logic in the decorator; the entity id is what a patient asks about ("which data") |

### Chosen Option: **B**

### Rationale

- New store method `LogReadsAsync(IReadOnlyList<ReadAuditEntry> entries, CancellationToken)`; each provider writes the batch in one command or transaction (multi-row `INSERT`, EF `AddRange` + one `SaveChangesAsync`, MongoDB `InsertManyAsync`, one Marten session).
- A read that returns nothing still writes one entry with `EntityCount = 0` and no entity id (evidence that a lookup happened), as today.
- The batch size is bounded by the query's own page size; an unpaged `ListAsync` over thousands of rows writes thousands of entries, which is the correct evidence and is documented as a reason to page.

</details>

<details>
<summary><strong>3. Subject and Category Declaration — fluent, per entity type, compiled accessors</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Attributes on the entity (`[ReadAuditSubject]` on a property, `[ReadAuditCategory("health")]` on the class)** | Declarative, close to the model | Reflection at first use; puts audit concerns in the domain model; a category that depends on configuration cannot be expressed |
| **B) Fluent registration in `ReadAuditOptions`: `AuditReadsFor<Patient>(r => r.SubjectId(p => p.Id).Category("health"))`, compiled once to a `Func<TEntity, string?>`** | No reflection per call; keeps the domain model clean; category from configuration | Registration code per entity type |
| **C) An interface on the entity (`IReadAuditSubject.DataSubjectId`)** | Simple | Same domain-model intrusion as A; one fixed category per type |

### Chosen Option: **B**, with the entity id itself as the default subject for entity types registered with `.SubjectIsEntity()`

### Rationale

- `ReadAuditEntityRegistration<TEntity>` holds `Func<TEntity, string?> SubjectAccessor`, `Func<TEntity, string?> EntityIdAccessor` (default `IEntity<TId>.Id.ToString()`), `string? DataCategory`, `ReadAuditFailureMode? FailureMode`, `PurposeEnforcement? PurposeEnforcement`.
- Subject ids of any type (string, `Guid`, strongly-typed ids) are converted with the same rules as the DSR subject extractor of #1149 (`DefaultDataSubjectIdExtractor`), so a subject id reads the same in DSR, consent and read audit. P-06 (#1194) resolves subjects from requests and responses and should share the conversion; OD-3 asks whether to extract a shared `IDataSubjectIdConverter` into core now.
- `AuditReadsFor<TEntity>(double samplingRate)` stays for non-evidential auditing; the validator rejects a sampling rate below 1.0 for an entity type whose failure mode is `FailClosed` or whose category is special.

</details>

<details>
<summary><strong>4. Failure Semantics — awaited writes; fail-closed returns <code>Left</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep fire-and-forget; add fail-closed as an awaited path** | Lowest latency in fail-open mode | Keeps the concurrent use of the scoped `DbContext` and connection in fail-open mode (a defect on EF Core); two code paths |
| **B) Always await the write; the failure mode decides whether a failure becomes `Left` (fail-closed) or a logged, metered warning (fail-open)** | One code path; no concurrent use of scoped resources; the `Either` of the store is honoured in both modes | Every audited read waits for the audit write |
| **C) Queue entries to a background writer (channel) in fail-open mode** | Low latency, no shared scope | Entries lost on crash; more moving parts; not evidence |

### Chosen Option: **B**

### Rationale

- Order in the decorator: purpose check (reject before reading) → inner read → build entries from the returned entities → `LogReadsAsync` → return the data or `Left(read_audit.audit_write_failed)` in fail-closed mode.
- EF Core: `ReadAuditStoreEF` writes through its own `DbContext` instance from `IDbContextFactory<TContext>` when one is registered (or a dedicated scope), never `SaveChangesAsync` on the application's context, so an audited read never commits the application's pending changes (OD-9 covers the registration requirement).
- Default failure mode: `ReadAuditOptions.FailureMode = FailClosed` (SPEC-002 INV-005 and DEC-006: compliance-relevant behaviour fails closed unless an explicit, logged opt-out says otherwise); `FailOpen` is the opt-out, logged once at start-up. OD-4 asks the maintainer to confirm the default.
- Fail-closed with the in-memory store outside the `Development` environment fails at start-up (`ReadAuditStartupValidator`), so evidence is never silently kept in memory.

</details>

<details>
<summary><strong>5. Purpose Enforcement — three modes, reject before reading</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Keep `bool RequirePurpose` (warn)** | No change | REQ-007 requires a reject mode |
| **B) `PurposeEnforcement { None, Warn, Reject }` globally and per entity type** | Explicit; per-type override for clinical data | Replaces a public `bool` (pre-1.0, fine) |

### Chosen Option: **B**

### Rationale

- `Reject` returns `Left(ReadAuditErrors.PurposeRequired(entityType, userId))` **before** the inner read, so no data is read without a declared purpose; the rejection is metered and logged (no entry is written, since nothing was read; OD-6 asks whether a refused attempt should also be recorded as an audit entry).
- `IReadAuditContext.WithPurpose` remains the way to declare a purpose; P-03's disclosure scope sets it automatically.

</details>

<details>
<summary><strong>6. Retention per Data Category</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One global period (today)** | Simple | Cannot keep special-category access evidence longer than other access logs |
| **B) A default period plus a map per category; a set of categories the application declares special, with a 3-year recommended default for them** | Matches REQ-007; purges stay one query per category | Purge runs once per configured category |
| **C) Period stored on each entry at write time** | Purge is one query (`WHERE ExpiresAtUtc < now`) | A configuration change does not affect entries already written; one more column |

### Chosen Option: **B**

### Rationale

- `ReadAuditRetentionOptions`: `DefaultPeriod` (calendar period, default 365 days as today), `CategoryPeriods` (map), `SpecialCategories` (set; empty by default: which categories are special is the application's legal decision), `SpecialCategoryDefaultPeriod` (default **3 years**, calendar arithmetic; XML doc and README: "a documented recommendation, not a legal requirement").
- New store method `PurgeEntriesAsync(ReadAuditPurgeCriteria criteria, CancellationToken)` with `OlderThanUtc`, `DataCategory` (exact) or `ExcludeCategories` (for the default bucket), `TenantId?`; the old `PurgeEntriesAsync(DateTimeOffset)` is removed.
- `ReadAuditRetentionService` computes one cutoff per category from `TimeProvider` and purges each; entries without a category use the default bucket. Moving it to Encina scheduling stays with #767.
- The period type: OD-7 asks whether P-01's `CalendarPeriod` moves to core `Encina` so that read audit (which does not reference Retention) uses the same type; otherwise read audit carries its own `(Years, Months, Days)` value.
- Marten: purge is crypto-shredding of time-period keys (ADR-020). Per-category retention re-keys read-audit entries by `(category, period)` so that destroying the keys of one category's expired periods leaves other categories readable (OD-8).

</details>

<details>
<summary><strong>7. Per-Subject Access Query and Tenant Scope</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Extend `ReadAuditQuery` with `DataSubjectId` and `DataCategory` only** | One query API | The Art. 15 answer ("who, when, which data, which purpose") needs a stable projection and a mandatory tenant |
| **B) A dedicated `GetAccessesForSubjectAsync(ReadAuditSubjectQuery query)` returning `SubjectAccessRecord`s, plus the two filters on `ReadAuditQuery`** | A purpose-built, stable answer for Art. 15 and EHDS Art. 9; tenant required when tenancy is on | Two ways to query |

### Chosen Option: **B**

### Rationale

- `ReadAuditSubjectQuery(string? TenantId, string DataSubjectId, DateTimeOffset FromUtc, DateTimeOffset ToUtc, string? DataCategory = null, int PageNumber = 1, int PageSize = 100)`; `SubjectAccessRecord(DateTimeOffset AccessedAtUtc, string? UserId, string EntityType, string? EntityId, string? DataCategory, string? Purpose, ReadAccessMethod AccessMethod, string? CorrelationId)`; result `PagedResult<SubjectAccessRecord>`.
- Tenant: `IReadAuditQueryService.GetAccessesForSubjectAsync(subjectId, fromUtc, toUtc, …)` takes the tenant from `IRequestContext` and fails closed (`Left(read_audit.tenant_required)`) when tenancy is on and there is none; the store method takes the tenant explicitly for background exports. Tenant filtering of the other existing queries is #798.
- `DataSubjectId` storage: plaintext and indexed in the relational and document stores, like `EntityId` today; on Marten it is a read-model field (the event encrypts it with the entry's key, as it does `UserId`). OD-5 asks whether to store a keyed hash instead.

</details>

---

## Implementation Phases

### Phase 1: Entry Shape, Options and Errors

<details>
<summary><strong>Tasks</strong></summary>

#### `src/Encina.Security.Audit/`

1. **`ReadAuditEntry.cs`** (modify) — add `string? DataSubjectId`, `string? DataCategory`, `Guid? ReadOperationId`
2. **`ReadAuditEntryEntity.cs`, `ReadAuditEntryMapper.cs`** (modify) — same fields
3. **`ReadAuditFailureMode.cs`** (new) — `FailOpen = 0`, `FailClosed = 1`
4. **`PurposeEnforcement.cs`** (new) — `None = 0`, `Warn = 1`, `Reject = 2`
5. **`ReadAuditEntityRegistration.cs`** (new) — fluent builder `SubjectId(Func<TEntity, object?>)`, `SubjectIsEntity()`, `Category(string)`, `FailureMode(ReadAuditFailureMode)`, `Purpose(PurposeEnforcement)`, `SamplingRate(double)`; compiled accessors
6. **`ReadAuditRetentionOptions.cs`** (new) — design choice 6
7. **`ReadAuditOptions.cs`** (modify) — `FailureMode` (default `FailClosed`), `PurposeEnforcement` (replaces `RequirePurpose`, default `Warn`), `Retention` (replaces `RetentionDays`), `AuditReadsFor<TEntity>(Action<ReadAuditEntityRegistration<TEntity>>)`, `AddHealthCheck`; remove the unused `BatchSize`
8. **`ReadAuditOptionsValidator.cs`** (new, `IValidateOptions<ReadAuditOptions>`) — sampling < 1.0 rejected for fail-closed or special-category types; periods positive; special categories have a period; `ExcludeSystemAccess = true` rejected for fail-closed types (a system actor's read is an access)
9. **`ReadAuditErrors.cs`** (modify) — `read_audit.audit_write_failed`, `read_audit.tenant_required`, `read_audit.subject_unresolved`; `PurposeRequired` now used
10. **`ReadAuditQuery.cs`** (modify) — `DataSubjectId`, `DataCategory` filters and builder methods `ForSubject`, `ForCategory`
11. **`ReadAuditSubjectQuery.cs`, `SubjectAccessRecord.cs`, `ReadAuditPurgeCriteria.cs`** (new)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of issue #1193 (SPEC-002 P-05) in src/Encina.Security.Audit.

CONTEXT:
- ReadAuditEntry today: Id, EntityType, EntityId, UserId, TenantId, AccessedAtUtc, CorrelationId, Purpose,
  AccessMethod, EntityCount, Metadata. No subject id, no data category.
- ReadAuditOptions today: Enabled, ExcludeSystemAccess, RequirePurpose (warn only), BatchSize (unused),
  RetentionDays = 365, EnableAutoPurge, PurgeIntervalHours, AuditReadsFor<TEntity>(samplingRate).
- Pre-1.0: replace members, no [Obsolete].

TASK:
Add the entry fields, the failure-mode and purpose-enforcement enums, per-entity fluent registration with
compiled accessors, per-category retention options, the options validator, the new errors, the query filters and
the new query/purge records listed in the Phase 1 tasks.

KEY RULES:
- XML docs on SpecialCategoryDefaultPeriod and in the README text: "The 3-year default is a documented
  recommendation, not a legal requirement: no rule applicable today fixes a period (EHDS Art. 9 applies from
  26 March 2029; AEPD PD-00068-2026 fixes none)."
- Subject ids of any type converted with the same rules as DSR's DefaultDataSubjectIdExtractor (#1149).
- No DateTime.UtcNow; no reflection per call (compile accessors once).
- PublicAPI.Unshipped.txt complete.

REFERENCE FILES:
- src/Encina.Security.Audit/ReadAuditEntry.cs, ReadAuditOptions.cs, ReadAuditErrors.cs, ReadAuditQuery.cs
- src/Encina.Compliance.DataSubjectRights/DefaultDataSubjectIdExtractor.cs
```

</details>

---

### Phase 2: Store Contract

<details>
<summary><strong>Tasks</strong></summary>

1. **`Abstractions/IReadAuditStore.cs`** (modify)
   - Add `ValueTask<Either<EncinaError, Unit>> LogReadsAsync(IReadOnlyList<ReadAuditEntry> entries, CancellationToken ct = default)`
   - Add `ValueTask<Either<EncinaError, PagedResult<SubjectAccessRecord>>> GetAccessesForSubjectAsync(ReadAuditSubjectQuery query, CancellationToken ct = default)`
   - Replace `PurgeEntriesAsync(DateTimeOffset)` with `PurgeEntriesAsync(ReadAuditPurgeCriteria criteria, CancellationToken ct = default)`
   - Add `ValueTask<Either<EncinaError, Unit>> ProbeAsync(CancellationToken ct = default)` (cheap health probe)
2. **`InMemoryReadAuditStore.cs`** (modify) — implement the new members
3. **`Abstractions/IReadAuditQueryService.cs` + `ReadAuditQueryService.cs`** (new) — tenant from `IRequestContext`, fail closed when tenancy is on and no tenant; `GetAccessesForSubjectAsync(string subjectId, DateTimeOffset fromUtc, DateTimeOffset toUtc, string? dataCategory = null, int pageNumber = 1, int pageSize = 100, CancellationToken ct = default)`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #1193: the IReadAuditStore contract changes.

TASK:
Add LogReadsAsync, GetAccessesForSubjectAsync, PurgeEntriesAsync(ReadAuditPurgeCriteria) and ProbeAsync to
IReadAuditStore; implement them in InMemoryReadAuditStore; add IReadAuditQueryService/ReadAuditQueryService.

KEY RULES:
- ROP everywhere; InMemory store honours tenant, subject, category and purge criteria exactly as the database
  stores will (the contract tests in Phase 8 run against it too).
- Tenant required when tenancy is on (Left(read_audit.tenant_required)); with tenancy off, tenant null matches
  entries with a null tenant.

REFERENCE FILES:
- src/Encina.Security.Audit/Abstractions/IReadAuditStore.cs
- src/Encina.Security.Audit/InMemoryReadAuditStore.cs
- src/Encina/Core/RequestContext.cs
```

</details>

---

### Phase 3: The Decorator — `AuditedFunctionalRepository`

<details>
<summary><strong>Tasks</strong></summary>

1. **`AuditedFunctionalReadRepository<TEntity,TId>`** (new) : `IFunctionalReadRepository<TEntity,TId>` and **`AuditedFunctionalRepository<TEntity,TId>`** (new) : `IFunctionalRepository<TEntity,TId>`; constructor `(inner, IReadAuditStore, IRequestContext, IReadAuditContext, IOptions<ReadAuditOptions>, TimeProvider, ILogger<…>)`
   - Flow per audited method: registration lookup (static per closed generic type) → enabled/sampling → purpose enforcement (reject before reading) → inner call → on `Right`, build one entry per returned entity (entity id, subject id, category, shared `ReadOperationId`, `AccessMethod = Repository`, `Metadata["method"]`) or one empty entry → `LogReadsAsync` awaited → fail-closed `Left(read_audit.audit_write_failed)` or fail-open warning
   - A subject accessor that throws or returns null for a registered subject type: `Left(read_audit.subject_unresolved)` in fail-closed mode (no silent fallback, as #1149 requires for DSR), warning in fail-open mode
2. **`ReadAuditRepositoryServiceCollectionExtensions.AddReadAuditedRepository<TEntity,TId>()`** (new) — decorates the registered `IFunctionalRepository<TEntity,TId>` and `IFunctionalReadRepository<TEntity,TId>`; throws at registration when no inner registration exists
3. **`AuditedRepository.cs`, `AuditedReadOnlyRepository.cs`** — removed (OD-1)
4. **`Notifications/SensitiveDataAccessedNotification.cs`** — removed (published by nothing), or published on fail-closed special-category reads if the maintainer prefers (listed under OD-1)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #1193: AuditedFunctionalRepository / AuditedFunctionalReadRepository.

CONTEXT:
- The 10 provider repositories (FunctionalRepositoryADO/Dapper x3, FunctionalRepositoryEF,
  FunctionalRepositoryMongoDB) implement IFunctionalRepository<TEntity,TId> and return Either.
- The existing AuditedRepository decorates IRepository (implemented by no provider) fire-and-forget; it is
  removed in this phase.

TASK:
Implement the decorators and AddReadAuditedRepository<TEntity,TId>() as listed.

KEY RULES:
- Await the audit write in every mode; never "_ = task".
- Purpose Reject: return Left before calling the inner repository.
- One entry per returned entity; one batch per read (LogReadsAsync); shared ReadOperationId.
- Fail-closed: any Left or exception from the store -> Left(read_audit.audit_write_failed); data not returned.
- Static per-closed-generic-type cache of the entity registration.
- No subject ids in logs or telemetry tags.

REFERENCE FILES:
- src/Encina.Security.Audit/AuditedRepository.cs (current behaviour to replace)
- src/Encina.DomainModeling/FunctionalRepository.cs
- src/Encina.Compliance.DataSubjectRights/ProcessingRestrictionPipelineBehavior.cs (static cache pattern)
```

</details>

---

### Phase 4: Configuration, DI, Start-up Validation and Health

<details>
<summary><strong>Tasks</strong></summary>

1. **`ServiceCollectionExtensions.AddEncinaReadAuditing`** (modify) — register `ReadAuditOptionsValidator`, `IReadAuditQueryService`, `ReadAuditStartupValidator` (hosted, checks: fail-closed + `InMemoryReadAuditStore` outside `Development` → start-up failure; registered types without a subject accessor when special → failure), the health check when `AddHealthCheck` (default `true` when any type is fail-closed); the decorator reads `IOptions<ReadAuditOptions>` (today it takes the raw options singleton while the retention service takes `IOptions`)
2. **Registration order** — provider registrations (`UseReadAuditStore` on ADO, Dapper, EF Core, MongoDB) switch from `TryAdd` to `Replace` for `IReadAuditStore`, as `Encina.Audit.Marten` already does, so calling `AddEncinaReadAuditing` first no longer leaves the in-memory store in place; the in-memory store stays a `TryAdd` default
3. **`Health/ReadAuditStoreHealthCheck.cs`** (modify) — probe with `ProbeAsync`; `Unhealthy` when the store fails and any type is fail-closed, `Degraded` otherwise

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #1193: registration, start-up validation and the health check.

CONTEXT:
- Today AddEncinaReadAuditing TryAdds a singleton InMemoryReadAuditStore; ADO and Dapper TryAdd their scoped
  stores, so the order of registration decides silently which store wins. Marten uses services.Replace.

TASK:
Implement the Phase 4 tasks across Encina.Security.Audit and the provider ServiceCollectionExtensions.

KEY RULES:
- Evidence is never silently kept in memory: fail-closed + InMemory store outside Development fails start-up.
- Health check: DefaultName const, static Tags, scoped resolution via IServiceProvider.CreateScope().

REFERENCE FILES:
- src/Encina.Security.Audit/ServiceCollectionExtensions.cs
- src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs (UseReadAuditStore registration)
- src/Encina.Audit.Marten/ServiceCollectionExtensions.cs (Replace pattern)
- src/Encina.Security.Audit/Health/ReadAuditStoreHealthCheck.cs
```

</details>

---

### Phase 5: Provider Implementations — 10 Database Providers

<details>
<summary><strong>Tasks</strong></summary>

#### 5a. ADO.NET ×3 and 5b. Dapper ×3 (`Auditing/ReadAuditStore{ADO|Dapper}.cs`, `Scripts/020_CreateReadAuditEntriesTable.sql`, `000_CreateAllTables.sql`)

1. Columns: `DataSubjectId` (SQL Server `NVARCHAR(256) NULL`, PostgreSQL `TEXT`, MySQL `VARCHAR(256)`), `DataCategory` (`NVARCHAR(128)` / `TEXT` / `VARCHAR(128)`), `ReadOperationId` (`UNIQUEIDENTIFIER` / `UUID` / `CHAR(36)`, nullable)
2. Indexes: `IX_ReadAuditEntries_Subject (TenantId, DataSubjectId, AccessedAtUtc)`, `IX_ReadAuditEntries_Category_AccessedAt (DataCategory, AccessedAtUtc)` (filtered on SQL Server and PostgreSQL where the existing scripts filter)
3. `LogReadsAsync` — one multi-row `INSERT` per batch (chunked at the provider's parameter limit: 2,100 on SQL Server), inside one transaction
4. `GetAccessesForSubjectAsync` — `WHERE TenantId = @t (or IS NULL) AND DataSubjectId = @s AND AccessedAtUtc >= @from AND AccessedAtUtc < @to [AND DataCategory = @c] ORDER BY AccessedAtUtc DESC` with provider pagination (`OFFSET/FETCH`, `LIMIT/OFFSET`)
5. `PurgeEntriesAsync(criteria)` — category or excluded-categories predicate, tenant optional
6. `ProbeAsync` — `SELECT 1 FROM <table> WHERE 1 = 0`-style probe
7. Catch provider exceptions and return `Left` in every method (the #1135 pattern, applied here to the new members)

#### 5c. EF Core (`Auditing/ReadAuditStoreEF.cs`, `ReadAuditEntryEntityConfiguration.cs`)

1. Properties and indexes as above (`HasFilter(IndexFilters.IsNotNull(...))` as fixed in #1128)
2. Writes through a dedicated context (`IDbContextFactory<TContext>` when registered, otherwise a new scope), never `SaveChangesAsync` on the application's scoped `DbContext` (OD-9)
3. `ModelBuilder.ApplyEncinaReadAudit()` extension so applications stop calling `ApplyConfiguration` by hand
4. Requires #1135 (exception handling) to have merged, or includes its fix for the read-audit store

#### 5d. MongoDB (`Auditing/ReadAuditStoreMongoDB.cs`, `ReadAuditEntryDocument.cs`)

1. BSON fields `data_subject_id`, `data_category`, `read_operation_id`
2. Indexes created at start-up: `{tenant_id, data_subject_id, accessed_at_utc}`, `{data_category, accessed_at_utc}` (no index creation exists today)
3. `InsertManyAsync` for batches; `DeleteManyAsync` for purge

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #1193: the read-audit store changes on the 10 database providers.

CONTEXT:
- Stores: src/Encina.{ADO|Dapper}.{SqlServer|PostgreSQL|MySQL}/Auditing/ReadAuditStore{ADO|Dapper}.cs,
  src/Encina.EntityFrameworkCore/Auditing/ReadAuditStoreEF.cs, src/Encina.MongoDB/Auditing/ReadAuditStoreMongoDB.cs.
- Scripts: Scripts/020_CreateReadAuditEntriesTable.sql and 000_CreateAllTables.sql per ADO/Dapper package
  (the Dapper scripts are byte-identical to the ADO ones; keep them so).
- Table: [dbo].[ReadAuditEntries] (SQL Server), readauditentries (PostgreSQL), `ReadAuditEntries` (MySQL).

TASK:
Add the three columns and two indexes; implement LogReadsAsync, GetAccessesForSubjectAsync,
PurgeEntriesAsync(criteria) and ProbeAsync on every provider as listed in 5a-5d.

KEY RULES:
- Parameters only; provider quoting; chunk multi-row inserts under the parameter limit.
- Every method returns Left on provider exceptions (never lets them escape; #1135).
- EF Core: never SaveChangesAsync on the application's DbContext; use a dedicated context.
- Async calls with CancellationToken only.

REFERENCE FILES:
- src/Encina.ADO.SqlServer/Auditing/ReadAuditStoreADO.cs
- src/Encina.EntityFrameworkCore/Auditing/ReadAuditEntryEntityConfiguration.cs
- src/Encina.MongoDB/Auditing/ReadAuditEntryDocument.cs
```

</details>

---

### Phase 6: Marten (`Encina.Audit.Marten`)

<details>
<summary><strong>Tasks</strong></summary>

1. `Events/ReadAuditEntryRecordedEvent.cs` — add `EncryptedDataSubjectId`, `DataCategory` (plaintext, needed to pick the key), `ReadOperationId`
2. `Projections/ReadAuditEntryReadModel.cs` and projection — `DataSubjectId` (decrypted at projection time as `UserId` is today, or kept only while the key exists), `DataCategory`, `ReadOperationId`; Marten index `(TenantId, DataSubjectId, AccessedAtUtc)`
3. `MartenReadAuditStore` — `LogReadsAsync` in one session; `GetAccessesForSubjectAsync` on the read model; per-category purge through keys scoped by `(category, period)` (OD-8); `ProbeAsync`
4. `MartenAuditRetentionService` — per-category cutoffs from `ReadAuditRetentionOptions`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of issue #1193 in src/Encina.Audit.Marten.

CONTEXT:
- MartenReadAuditStore is event-sourced; UserId, Purpose and Metadata are encrypted with temporal keys
  (ADR-020); purge destroys keys older than the cutoff (DestroyKeysBeforeAsync), returning key periods, not rows.

TASK:
Add subject, category and operation id to the event and read model; implement the new store members; re-key
temporal keys by (category, period) so per-category purge destroys only that category's expired keys.

KEY RULES:
- DataSubjectId is personal data: encrypted in the event like UserId.
- Marten projections take dependencies through IDocumentOperations and constructor injection, never
  IServiceProvider; keep one test registering the projection with a real store.
- There is no Marten read-audit integration test today; add one (Phase 8).

REFERENCE FILES:
- src/Encina.Audit.Marten/MartenReadAuditStore.cs
- src/Encina.Audit.Marten/Events/ReadAuditEntryRecordedEvent.cs
- docs/architecture/adr/020-temporal-crypto-shredding-audit-store.md
```

</details>

---

### Phase 7: Observability

<details>
<summary><strong>Tasks</strong></summary>

1. **`Diagnostics/ReadAuditActivitySource.cs`** (`Encina.ReadAudit`, unchanged name) — activities `ReadAudit.Write` (tags `encina.tenant_id`, `read_audit.entity_type`, `read_audit.data_category`, `read_audit.entity_count`, `read_audit.failure_mode`, `read_audit.outcome`) and `ReadAudit.QuerySubject` (tags `encina.tenant_id`, `read_audit.outcome`; never the subject id)
2. **`Diagnostics/ReadAuditMeter.cs`** (`Encina.ReadAudit`) — keep `read_audit.entries_logged.total` (add tags `data_category`, `encina.tenant_id`), `read_audit.log_failures.total` (add `failure_mode`); add `read_audit.reads_failed_closed.total`, `read_audit.purpose_rejections.total`, `read_audit.subject_queries.total`; start using the existing but unused `read_audit.log.duration.ms` and `read_audit.query.duration.ms` histograms and `read_audit.queries.total`; entries purged per category
3. **`Diagnostics/ReadAuditLog.cs`** — new `[LoggerMessage]` in `SecurityAuditRead` (1700–1799) packed from 1739: `ReadFailedClosed` (1739, Error), `AuditWriteFailedOpen` (1740, Warning), `PurposeRejected` (1741, Warning), `SubjectUnresolved` (1742, Warning), `SubjectQueryExecuted` (1743, Information), `SubjectQueryFailed` (1744, Error), `CategoryPurgeCompleted` (1745, Information), `CategoryPurgeFailed` (1746, Error), `FailOpenConfigured` (1747, Warning, logged once at start-up), `InMemoryStoreInProduction` (1748, Critical), `SamplingIgnoredForEvidentialType` (1749, Warning); remove or wire the unused 1710–1712 and 1720–1722 messages
4. `Encina.Audit.Marten` messages use the `AuditMarten` range (2550–2599)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 (observability) of issue #1193.

CONTEXT:
- ActivitySource and Meter "Encina.ReadAudit"; EventIds in SecurityAuditRead = (1700, 1799); used today:
  1700-1702, 1710-1712, 1720-1722, 1730-1738. AuditMarten = (2550, 2599).

TASK:
Add the activities, tags, counters and [LoggerMessage] entries of the Phase 7 tasks; use the existing unused
histograms; decide per unused message whether to wire or remove it.

KEY RULES:
- No subject ids, entity ids or purposes in tags or messages (REQ-062); tenant as "encina.tenant_id".
- EventIds packed from 1739 with no gaps.
- EncinaEventIdAllocationTests must stay green.

REFERENCE FILES:
- src/Encina.Security.Audit/Diagnostics/ReadAuditActivitySource.cs, ReadAuditMeter.cs, ReadAuditLog.cs
- src/Encina/Diagnostics/EventIdRanges.cs
```

</details>

---

### Phase 8: Testing

<details>
<summary><strong>Tasks</strong></summary>

#### 8a. Unit Tests (`tests/Encina.UnitTests/Security/Audit/`)

- `AuditedFunctionalRepositoryTests` — every audited method; one entry per entity; empty-result entry; purpose `Reject` before the inner call (inner not invoked); fail-closed `Left` on store `Left` and on exception; fail-open warning; subject accessor null/throwing; sampling ignored for evidential types
- `ReadAuditOptionsValidatorTests`, `ReadAuditStartupValidatorTests`, `ReadAuditRetentionServiceTests` (per-category cutoffs under `FakeTimeProvider`, 3-year default across a leap day)
- `ReadAuditQueryServiceTests` (tenant required, fail closed)
- Telemetry: in-memory exporter asserts activities and counters carry `encina.tenant_id` and no subject id; log capture asserts no subject id, entity id or purpose in messages

#### 8b. Guard Tests (`tests/Encina.GuardTests/Security/Audit/` and provider `Auditing/` folders)

- New decorators, registration helper, query service, store members on all providers

#### 8c. Contract Tests (`tests/Encina.ContractTests/Security/Audit/ReadAudit/`)

- `ReadAuditStoreContractTests` extended: the same batch, subject query, purge-by-category and tenant cases produce the same results on InMemory and on every provider store (instantiated for real)

#### 8d. Property Tests (`tests/Encina.PropertyTests/Security/Audit/ReadAudit/`)

- For any generated list of entities returned by a fake inner repository, every returned entity id has exactly one entry with the right subject and category, and all entries share one `ReadOperationId`
- Purge by category never deletes an entry of another category or newer than its cutoff

#### 8e. Integration Tests (`tests/Encina.IntegrationTests/Security/Audit/ReadAudit/`)

- Per provider (existing classes extended): schema with the new columns and indexes, `LogReadsAsync` batches, subject query with pagination and tenant, purge by category, probe; two-tenant isolation (AC-043)
- `AuditedFunctionalRepository` end to end on each provider collection: a paged read writes one entry per row; a fail-closed read fails when the audit table is unavailable (drop or rename it inside the test's own schema)
- Marten: new `MartenReadAuditStoreIntegrationTests` (none exists today), including per-category key purge
- `[Collection]` fixtures, `ClearAllDataAsync` in `InitializeAsync`

#### 8f. Load Tests

- `tests/Encina.LoadTests/Security/Audit/ReadAudit/ReadAuditLoadTests.md` exists; update it: awaited writes change the latency profile of audited reads; justify, or add a small NBomber scenario for a paged read of 50 rows with fail-closed auditing (OD-10)

#### 8g. Benchmark Tests

- `tests/Encina.BenchmarkTests/Encina.Benchmarks/Security/Audit/ReadAudit/ReadAuditBenchmarks.md` exists; replace with `AuditedFunctionalRepositoryBenchmarks` (entry building for 1, 50, 500 entities; accessor cost) since audited reads are now on the request path

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 (testing) of issue #1193.

CONTEXT:
- Existing tests: tests/Encina.UnitTests/Security/Audit/, tests/Encina.GuardTests/Security/Audit/,
  tests/Encina.ContractTests/Security/Audit/ReadAudit/, tests/Encina.PropertyTests/Security/Audit/ReadAudit/,
  tests/Encina.IntegrationTests/Security/Audit/ReadAudit/ (ADO, Dapper, EF Core x3; MongoDB). No Marten
  read-audit integration test exists.
- Coverage manifest: .github/coverage-manifest/Encina.Security.Audit.json (unit 70, guard 20, contract 15) plus
  the provider and Audit.Marten manifests.

TASK:
Write the tests of 8a-8g, evidencing AC-007 (fail-closed, ids for collection reads, purpose reject, per-subject
query, per-category retention with the 3-year recommended default), AC-043 and AC-044.

KEY RULES:
- Real package code in every test type; contract tests instantiate every store.
- FakeTimeProvider; Shouldly via Encina.Testing.Shouldly; FsCheck via Encina.Testing.FsCheck.
- Update the manifests with the new files and flags; each flag must reach its target.

REFERENCE FILES:
- tests/Encina.UnitTests/Security/Audit/AuditedRepositoryTests.cs (to replace)
- tests/Encina.IntegrationTests/Security/Audit/ReadAudit/
```

</details>

---

### Phase 9: Cross-Cutting Integration

<details>
<summary><strong>Tasks</strong></summary>

1. **Multi-tenancy** — entries carry `IRequestContext.TenantId` (as today); the per-subject query and purge-by-tenant honour it; with tenancy off, `null` tenant, no configuration. The remaining tenant filtering of `GetAccessHistoryAsync`, `GetUserAccessHistoryAsync` and `QueryAsync` is #798; this plan does not duplicate it.
2. **P-03 disclosure** — P-03's disclosure scope sets `IReadAuditContext` purpose; reads inside it are audited with that purpose (no code here beyond keeping `IReadAuditContext.WithPurpose` scoped and restorable).
3. **P-06 (#1194)** — the entry shape, `LogReadsAsync` and the subject-id conversion are the contract P-06 builds on; document it in the XML docs of `IReadAuditStore`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
You are implementing Phase 9 (cross-cutting integration) of issue #1193.

TASK:
Verify tenant scoping of the new store members on every provider and Marten; make IReadAuditContext purpose
restorable (a disposable WithPurpose scope) for the P-03 disclosure scope; document the P-06 contract in XML.

KEY RULES:
- Do not implement #798 here; link it where tenant filtering of older queries is missing.

REFERENCE FILES:
- src/Encina.Security.Audit/ReadAuditContext.cs
- docs/plans/blocked-data-state-implementation-plan-1189.md (design choice 6)
```

</details>

---

### Phase 10: Documentation and Finalization

<details>
<summary><strong>Tasks</strong></summary>

1. XML documentation on all new or changed public APIs; the special-category default documented as a recommendation, not a legal requirement
2. `changelog.d/1193-evidential-read-audit.added.md`, `changelog.d/1193-evidential-read-audit.changed.md` (breaking: decorators replaced, `RequirePurpose`/`RetentionDays`/`BatchSize` replaced, awaited writes, store contract), `changelog.d/1193-evidential-read-audit.fixed.md` (EF store no longer commits the application's pending changes; registration order no longer keeps the in-memory store)
3. `src/Encina.Security.Audit/README.md` (the package has none today; #1203 tracks READMEs — coordinate so one PR writes it)
4. [`docs/features/read-auditing.md`](../features/read-auditing.md) — replace the fire-and-forget diagram, document fail-closed, purpose modes, per-entity registration, the per-subject query, retention per category and the recommendation wording; the example `RetentionDays = 2555` goes
5. `docs/INVENTORY.md`, `PublicAPI.Unshipped.txt` in every touched package
6. No ADR needed unless OD-1 keeps both decorator families (then a short ADR records why)
7. `dotnet build Encina.slnx --configuration Release` → 0 warnings; `dotnet test` → all pass; every coverage flag at its target

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 10</strong></summary>

```
You are finalising issue #1193.

TASK:
Write XML docs, changelog.d fragments (bullets starting with "-"), the package README (coordinate with #1203),
update docs/features/read-auditing.md, INVENTORY and PublicAPI files; run
dotnet run .github/scripts/changelog-fragments.cs -- --check; build with zero warnings; run the tests.

KEY RULES:
- State in XML docs and README: "The 3-year default is a documented recommendation, not a legal requirement."
- English only; never name the reference application.
- No hand-typed coverage figures.

REFERENCE FILES:
- docs/features/read-auditing.md
- changelog.d/README.md
```

</details>

---

## Research

### Standards and Legal Sources

| Source | Provision | Relevance |
|--------|-----------|-----------|
| GDPR | Art. 15 | Right of access; AEPD reads it as including who, when and which data were accessed [S] |
| GDPR | Art. 5(2), 30, 32 | Accountability, records, security of processing (access logging) |
| AEPD | PD-00068-2026 (13 July 2026) | Patients may learn the identity, time and data of accesses [S] (SPEC-002 §12 question 3: read the original) |
| EHDS | Reg. (EU) 2025/327 Art. 9 | Access information available at least 3 years, from 26 March 2029 [S] |
| Ley 41/2002 | Art. 16 | Access to clinical records limited to what each professional needs |
| SPEC-002 | REQ-007, AC-007, S13, REQ-061, REQ-062, INV-002, INV-005 | The requirement this plan implements |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `IReadAuditStore` and 10 + 1 implementations | `Encina.Security.Audit`, provider `Auditing/` folders, `Encina.Audit.Marten` | Extended contract |
| `IReadAuditContext` | `Encina.Security.Audit/Abstractions/` | Declared purpose |
| `ReadAuditErrors.PurposeRequired` | `Encina.Security.Audit/ReadAuditErrors.cs` | Reject mode (exists, unused) |
| `ReadAuditStoreHealthCheck` | `Encina.Security.Audit/Health/` | Health for fail-closed |
| `IFunctionalRepository` | `Encina.DomainModeling/FunctionalRepository.cs` | Decorated interface |
| `DefaultDataSubjectIdExtractor` (#1149) | `Encina.Compliance.DataSubjectRights` | Subject-id conversion rules |
| Temporal crypto-shredding (ADR-020) | `Encina.Audit.Marten` | Per-category key scoping |
| `IRequestContext` | `Encina/Core/RequestContext.cs` | Actor, tenant, correlation |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Security.Audit` (read audit) | 1700–1799 (`SecurityAuditRead`, registered) | New messages 1739–1749, packed after the last used id (1738) |
| `Encina.Audit.Marten` | 2550–2599 (`AuditMarten`, registered) | Only if the Marten store logs new events |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Core (Phases 1-4, 7) | ~20 | entry, options, registration, validator, errors, query types, store contract, InMemory, decorators, query service, DI, health, diagnostics |
| ADO ×3 + Dapper ×3 (Phase 5) | ~18 | store + 2 scripts × 6 |
| EF Core (Phase 5) | ~4 | store, configuration, model extension, DI |
| MongoDB (Phase 5) | ~3 | store, document, DI |
| Marten (Phase 6) | ~5 | event, read model, projection, store, retention service |
| Tests (Phase 8) | ~25 | across 7 test types |
| Documentation (Phase 10) | ~7 | |
| **Total** | **~82** | |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing issue #1193 (SPEC-002 P-05): evidential read audit in Encina.Security.Audit, the 10
database providers and Encina.Audit.Marten.

PROJECT CONTEXT:
- .NET 10 / C# 14, ROP, pre-1.0 (breaking changes preferred).
- 10 providers: ADO.NET and Dapper (SqlServer, PostgreSQL, MySQL), EF Core, MongoDB; plus Marten.
- Prerequisite: #1135 (EF audit stores must return Left instead of letting exceptions escape).

IMPLEMENTATION OVERVIEW:
Phase 1: entry fields (DataSubjectId, DataCategory, ReadOperationId), failure mode, purpose enforcement,
         per-entity fluent registration, per-category retention (3-year recommended special default), validator
Phase 2: IReadAuditStore: LogReadsAsync, GetAccessesForSubjectAsync, PurgeEntriesAsync(criteria), ProbeAsync;
         IReadAuditQueryService (tenant from IRequestContext, fail closed)
Phase 3: AuditedFunctionalRepository / AuditedFunctionalReadRepository (awaited writes, Left on fail-closed,
         purpose reject before reading, one entry per entity); old IRepository decorators removed
Phase 4: DI, Replace-based provider registration, start-up validation, health check
Phase 5: 10 providers: columns, indexes, batch insert, subject query, purge by category, probe
Phase 6: Marten: encrypted subject id, read model, keys per (category, period)
Phase 7: telemetry "Encina.ReadAudit"; EventIds 1739-1749
Phase 8: unit, guard, contract, property, integration (10 + Marten), load/benchmark updates
Phase 9: tenancy, P-03 disclosure purpose, P-06 contract
Phase 10: docs, changelog.d, README, feature page, INVENTORY, PublicAPI

KEY PATTERNS:
- Never fire-and-forget; fail-closed is the default (INV-005) with a logged opt-out.
- No subject ids, entity ids or purposes in telemetry; tenant as "encina.tenant_id".
- "The 3-year default is a documented recommendation, not a legal requirement."

REFERENCE FILES:
- src/Encina.Security.Audit/ (whole package)
- src/Encina.ADO.SqlServer/Auditing/ReadAuditStoreADO.cs and Scripts/020_CreateReadAuditEntriesTable.sql
- src/Encina.Audit.Marten/MartenReadAuditStore.cs
- docs/specifications/SPEC-002-eu-regulatory-readiness.md (REQ-007, AC-007, S13)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | No read path that benefits from caching: audit writes are never cached, and the per-subject query must reflect every entry |
| 2 | OpenTelemetry | ✅ | `ReadAudit.Write` and `ReadAudit.QuerySubject` activities; counters and the existing, unused histograms put to use; `encina.tenant_id`; no subject ids |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` 1739–1749 in `SecurityAuditRead` (ADR-021) |
| 4 | Health Checks | ✅ | `ReadAuditStoreHealthCheck` registered when a type is fail-closed, probing with `ProbeAsync` |
| 5 | Validation | ✅ | `ReadAuditOptionsValidator` and `ReadAuditStartupValidator` (sampling, periods, in-memory store in production) |
| 6 | Resilience | ❌ | Local database store; fail-closed is the explicit behaviour on failure and retrying inside a read would only add latency |
| 7 | Distributed Locks | ❌ | Entries are append-only; the purge is idempotent (deleting already-deleted rows is harmless), so concurrent purges on several hosts are safe |
| 8 | Transactions | ❌ | The batch of one read is written atomically by the store itself; the audit write is deliberately not part of the application's transaction (an audited read must be recorded even if the caller later rolls back) |
| 9 | Idempotency | ❌ | Not a message or request entry point; each read is a distinct access and must be recorded as such |
| 10 | Multi-Tenancy | ✅ | REQ-061/AC-043: entries carry the tenant; the per-subject query and purge are tenant-scoped; the rest of the audit queries are #798 |
| 11 | Module Isolation | ❌ | SPEC-002 requires no module scoping; `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ✅ | This feature is the read audit trail |

---

## Provider Matrix

| Provider | Subject, category and operation columns | Batch write | Per-subject query | Per-category purge | Integration test |
|----------|:-:|:-:|:-:|:-:|:-:|
| ADO-SqlServer | ✅ | ✅ | ✅ | ✅ | ✅ |
| ADO-PostgreSQL | ✅ | ✅ | ✅ | ✅ | ✅ |
| ADO-MySQL | ✅ | ✅ | ✅ | ✅ | ✅ |
| Dapper-SqlServer | ✅ | ✅ | ✅ | ✅ | ✅ |
| Dapper-PostgreSQL | ✅ | ✅ | ✅ | ✅ | ✅ |
| Dapper-MySQL | ✅ | ✅ | ✅ | ✅ | ✅ |
| EFCore-SqlServer | ✅ | ✅ | ✅ | ✅ | ✅ |
| EFCore-PostgreSQL | ✅ | ✅ | ✅ | ✅ | ✅ |
| EFCore-MySQL | ✅ | ✅ | ✅ | ✅ | ✅ |
| MongoDB | ✅ | ✅ | ✅ | ✅ | ✅ |
| Marten (`Encina.Audit.Marten`) | ✅ (subject encrypted) | ✅ | ✅ | ✅ (keys per category and period) | ✅ (new) |

## Test Matrix

| Test Type | Required? | Scope | Notes |
|-----------|:---------:|-------|-------|
| UnitTests | ✅ | Decorators, options, validators, retention service, query service, telemetry redaction | |
| GuardTests | ✅ | Public constructors and methods, all stores | |
| ContractTests | ✅ | Same store contract on InMemory, 10 providers and Marten | Real instances |
| PropertyTests | ✅ | Every returned id has an entry; category purge isolation | FsCheck |
| IntegrationTests | ✅ | 10 providers and Marten; fail-closed end to end; two tenants | Marten test is new |
| LoadTests | 📄 | Update `ReadAuditLoadTests.md` (or small NBomber scenario, OD-10) | Awaited writes |
| BenchmarkTests | ✅ | Entry building for 1/50/500 entities | Replaces `ReadAuditBenchmarks.md` |

## Public API Changes

| Change | Kind |
|--------|------|
| `ReadAuditEntry.DataSubjectId`, `DataCategory`, `ReadOperationId` (and entity, document, event fields) | Added |
| `ReadAuditFailureMode`, `PurposeEnforcement`, `ReadAuditEntityRegistration<TEntity>`, `ReadAuditRetentionOptions`, `ReadAuditSubjectQuery`, `SubjectAccessRecord`, `ReadAuditPurgeCriteria` | Added |
| `IReadAuditStore.LogReadsAsync`, `GetAccessesForSubjectAsync`, `ProbeAsync`; `IReadAuditQueryService` | Added |
| `AuditedFunctionalRepository<TEntity,TId>`, `AuditedFunctionalReadRepository<TEntity,TId>`, `AddReadAuditedRepository<TEntity,TId>()`, `ModelBuilder.ApplyEncinaReadAudit()` | Added |
| `IReadAuditStore.PurgeEntriesAsync(DateTimeOffset)` → `PurgeEntriesAsync(ReadAuditPurgeCriteria)` | Changed (breaking) |
| `ReadAuditOptions.RequirePurpose` → `PurposeEnforcement`; `RetentionDays` → `Retention`; `BatchSize` removed | Changed / removed (breaking) |
| `AuditedRepository<TEntity,TId>`, `AuditedReadOnlyRepository<TEntity,TId>`, `SensitiveDataAccessedNotification` | Removed (OD-1) |
| Provider registration of `IReadAuditStore` uses `Replace` | Changed (behaviour) |

## Migration Notes

None for users (pre-1.0). The read-audit tables gain three nullable columns and two indexes on the 10 providers; the scripts `020_CreateReadAuditEntriesTable.sql` and `000_CreateAllTables.sql` and the EF configuration are updated in place; development databases are recreated. Marten read-audit events gain fields; existing development streams are recreated.

## Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Awaited audit writes add latency to every audited read | Slower reads | Only registered entity types are audited; batch write per read; benchmark and load evidence (Phase 8) |
| Fail-closed by default makes reads depend on the audit store | Outage of the store blocks clinical reads | Health check; explicit, logged fail-open opt-out; OD-4 |
| Unpaged `ListAsync` over large tables writes many entries | Large audit tables | Documented; per-category retention; paging recommended |
| #1135 not merged | EF store exceptions escape | Include the read-audit part of the #1135 fix in Phase 5 if it has not merged |
| S13 reads through a Dapper query handler, not a repository | AC-007's S13 cannot pass with this issue alone | P-06 (#1194) provides query-level audit; S13 passes when both land (spec gap below) |
| Subject ids stored in plaintext | Audit store holds personal data | Same exposure as `EntityId` today; per-category retention; OD-5 offers keyed hashing |

---

## Open Decisions for the Maintainer

1. **OD-1 — Old decorators.** Remove `AuditedRepository`/`AuditedReadOnlyRepository` (they decorate interfaces no provider implements and nothing registers) and `SensitiveDataAccessedNotification` (published by nothing), as the plan does, or keep them for `ISoftDeleteRepository`/`ITemporalRepository` users and publish the notification on special-category reads?
2. **OD-2 — Entry granularity.** One entry per returned entity in one batch (the plan), one entry per read with the ids in metadata, or one entry per data subject?
3. **OD-3 — Subject declaration and sharing with P-06.** Fluent per-entity registration (the plan), attributes, or an entity interface? Extract a shared `IDataSubjectIdConverter` into core now, so that DSR (#1149), read audit (P-05) and query-level audit (P-06) convert subject ids identically?
4. **OD-4 — Default failure mode.** Fail-closed by default with a logged fail-open opt-out (the plan, following INV-005 and DEC-006), or fail-open by default with fail-closed as an option (the wording of REQ-007: "an option makes the read fail")?
5. **OD-5 — Storing the subject id.** Plaintext and indexed (the plan, like `EntityId` today), or a keyed hash (HMAC with a tenant key) so that the audit store holds no direct identifier and the query hashes its input?
6. **OD-6 — Refused attempts.** When purpose enforcement rejects a read, record an audit entry of the attempt (no data read) or only log and meter it (the plan)?
7. **OD-7 — Period type.** Move P-01's `CalendarPeriod` into core `Encina` so that read audit and blocking share it, or give read audit its own calendar-period value?
8. **OD-8 — Marten per-category retention.** Re-key temporal keys by `(category, period)` (the plan), or purge Marten read-audit entries by deletion instead of crypto-shredding for per-category periods?
9. **OD-9 — EF Core write context.** Require `IDbContextFactory<TContext>` for the EF read-audit store (clean, explicit), or create a dedicated scope per write (works without extra registration, costs a context per read)?
10. **OD-10 — Load evidence.** Is a small NBomber scenario for fail-closed audited paged reads wanted, or is the updated `.md` justification plus the benchmark enough?
11. **OD-11 — Retention per tenant.** Is per-category retention a deployment-level setting (the plan), or must each tenant (each practice is its own controller) be able to set its own periods?

## Spec Gaps Found

- REQ-007 speaks of "collection and paged reads" through repositories, but the read-audit decorators wrap `IRepository`/`IReadOnlyRepository`, which none of the 10 providers implement, and nothing registers them: today no read through Encina's provider repositories is audited. The plan adds the functional-repository decorator.
- AC-007 lists S13 as passing, but S13 reads through a Dapper query handler (REQ-008, P-06 #1194), which repository audit does not see; S13 needs both P-05 and P-06.
- REQ-007 does not address sampling (`AuditReadsFor<TEntity>(samplingRate)`), which is incompatible with evidence; the plan rejects sampling for evidential types.
- REQ-007 does not say whether fail-closed is the default; INV-005 and DEC-006 suggest it is (OD-4).
- REQ-007 does not consider that the subject id in the audit store is itself personal data (OD-5).
- `ReadAuditStoreEF` saves through the application's scoped `DbContext` and the decorator writes concurrently with the caller: a defect beyond REQ-007 that this plan fixes (the changelog lists it under "fixed").
- Provider registration order silently keeps the in-memory store when `AddEncinaReadAuditing` is called first (ADO and Dapper use `TryAdd`); fixed here.
- The data-category vocabulary differs across Retention, DSR, blocking and read audit (see P-03 OD-10).

---

## Next Steps

1. Review and approve this plan; decide OD-1 … OD-11
2. Link it from issue #1193
3. Land #1135 first, or fold its read-audit part into Phase 5
4. One commit per phase; the final commit references `Fixes #1193`
