# Implementation Plan: `Encina.Compliance.Blocking` — Blocked Data State (LOPDGDD art. 32 *bloqueo*)

> **Issue**: [#1189](https://github.com/dlrivada/Encina/issues/1189) (SPEC-002 tracking id **P-03**, priority P0)
> **Type**: Feature
> **Complexity**: Very high (12 phases, 10 database providers + Marten, ~140 files)
> **Estimated Scope**: ~6,000-8,000 lines of production code + ~5,000-6,500 lines of tests
> **Milestone**: v0.17.0 — Compliance Lifecycle
> **Parent EPIC**: [#1186](https://github.com/dlrivada/Encina/issues/1186) — EU regulatory readiness (SPEC-002)
> **Specification**: [SPEC-002](../specifications/SPEC-002-eu-regulatory-readiness.md) REQ-005, AC-005, scenario S14; cross-cutting REQ-061 (AC-043) and REQ-062 (AC-044)
> **Depends on**: [#1187](https://github.com/dlrivada/Encina/issues/1187) (P-01, retention floor; plan [retention-floor-implementation-plan-1187.md](retention-floor-implementation-plan-1187.md)); [#1248](https://github.com/dlrivada/Encina/issues/1248) (P-45, relational `IPersonalDataLocator`); PR [#1185](https://github.com/dlrivada/Encina/pull/1185) (ADR-031); a composable row-filter `[REFACTOR]` issue on the 10 providers, a prerequisite that must land BEFORE this plan (OD-3, settled 2026-09-23, issue to be opened)
> **Related**: [#1188](https://github.com/dlrivada/Encina/issues/1188) (P-02, DSR erasure arbitration, the main caller), [#1191](https://github.com/dlrivada/Encina/issues/1191) (P-04, subject key store; crypto-shredding at period end), [#1239](https://github.com/dlrivada/Encina/issues/1239) (P-37, `IBlobStore` blocking hooks), [#1193](https://github.com/dlrivada/Encina/issues/1193) (P-05, read audit of disclosures), [#1225](https://github.com/dlrivada/Encina/issues/1225) (P-23, EF/Dapper and Marten consistency)

---

## Summary

LOPDGDD art. 32 requires a Spanish controller to **block** data instead of destroying it when it rectifies or erases it: the data is kept, but identified and reserved so that nobody can process or even view it, except to make it available to courts, prosecutors and supervisory authorities while liabilities can still be claimed; after that it must be destroyed. Art. 32.4 allows a secure copy with digital evidence of authenticity where blocking is disproportionate. Nothing in `src/` implements this: the closest pieces are Art. 18 restriction (`ProcessingRestrictionPipelineBehavior`, which has no authority role and no destruction date) and `UnderLegalHold` (which suspends deletion, not access).

This plan adds a jurisdiction-neutral **blocked** state ("restriction with statutory release roles"):

1. **State and evidence.** A `BlockRecord` per blocked unit (subject or entity, data category, reason, legal basis, blocked-at, destroy-not-before) with an audited lifecycle `Requested → Blocked → (Disclosed)* → ReleasedForDestruction → Destroyed`.
2. **Invisibility.** Rows of blockable entities carry a block marker; every query made through Encina repositories and specifications on the 10 database providers excludes them, and blocked Marten streams cannot be loaded. A pipeline behavior refuses requests that would process a blocked subject's category.
3. **Blocking instead of deletion.** Where the application enables it, `IFunctionalRepository.DeleteAsync`, the DSR erasure strategy and the retention sweep (`ExpiryDisposition.Block`, P-01) block instead of deleting.
4. **Superseded versions.** A rectification of a blockable entity stores the previous version in a blocked-version vault.
5. **Audited disclosure.** Blocked data is released only through a purpose-bound disclosure to a configured statutory role; the audit trail records purpose, role and case reference, and the reads are recorded by read audit (P-05).
6. **Secure copy (art. 32.4).** A secure copy is written through an application port, its SHA-256 hash recorded in the audit trail, and the blocked original is then released for destruction.
7. **Destruction.** At `DestroyNotBeforeUtc`, a sweep destroys (deletes, or crypto-shreds once P-04 provides per-category keys) the blocked data, audited, reading time from `TimeProvider`.
8. **Application-written SQL.** Encina documents and ships the filter such queries must apply (column convention, SQL predicate helper per provider, EF `WhereNotBlocked()`), which the reference scenario's Dapper agenda query uses (AC-005).

**Standards**: LOPDGDD art. 32.1–32.5 [V]; GDPR Art. 5(1)(e), Art. 17, Art. 18, Art. 19; Código Civil art. 1964.2 (5-year general limitation, [K], SPEC-002 §12 question 5); Ley 41/2002 art. 17.1 (clinical retention floor, via P-01).

**Affected packages**: new `Encina.Compliance.Blocking`; `Encina.DomainModeling` (blockable marker, filter context); the 10 provider packages (`Encina.ADO.*`, `Encina.Dapper.*`, `Encina.EntityFrameworkCore`, `Encina.MongoDB`); `Encina.Marten` (blocked streams); `Encina.Compliance.Retention` (disposition `Block`); `Encina.Compliance.DataSubjectRights` (blocking erasure strategy).

**Provider category**: Database (10) plus Marten. Blocking touches repositories and specifications, so the Multi-Provider Implementation Rule applies; block records also live on the 10 providers plus Marten (OD-1, settled 2026-09-23). This deliberately departs from ADR-019 for this module, because blocking must commit in the same transaction as the application's own data; ADR-019 needs an addendum recording the exception (planned in Phase 10 alongside ADR-033).

---

## Interaction with P-01 (Retention Floor, #1187)

The same contract appears in [retention-floor-implementation-plan-1187.md](retention-floor-implementation-plan-1187.md), section "Interaction with P-03".

| Topic | Contract |
|-------|----------|
| **Who decides what** | Retention decides **when** (floor, maximum, anchor, hold). Blocking decides **how data is kept once it may no longer be processed** (hidden state, audited disclosure, secure copy, destruction). |
| **Retention expiry with `ExpiryDisposition.Block`** | Per P-01 OD-7 (settled 2026-09-23), P-01 does not ship a `Block` member: this plan adds `ExpiryDisposition.Block` to `Encina.Compliance.Retention` itself, together with the start-up validation that rejects it while no `IBlockingService` is registered, and wires the retention sweep to call `IBlockingService.BlockAsync` with an entity-scoped target built from the ADR-031 `RetentionErasureTarget` (record id, entity id, category, tenant, module). On `Right`, the retention record moves to the new status `Blocked` through a new event `RetentionRecordBlocked(RecordId, BlockId, OccurredAtUtc)` added to `Encina.Compliance.Retention` by this plan (Phase 8). |
| **A blocked record's retention clock** | Blocking neither stops nor resets the retention clock. `DestroyNotBeforeUtc = max(FloorEndsAtUtc, BlockedAtUtc + LimitationPeriod)`, with `FloorEndsAtUtc` read from P-01's `IRetentionFloorQuery`. A legal hold (retention's `ILegalHoldService`) suspends destruction; the destruction sweep re-checks both, fail closed, right before destroying. |
| **Erasure request under a floor** | P-02 (#1188) refuses a category whose floor has not elapsed, with the grantable-after date from P-01. Per OD-4 (settled 2026-09-23), whether the tenant's jurisdiction blocks depends on jurisdiction: for Spain every DSR erasure and every retention expiry blocks first (LOPDGDD art. 32.1, literal reading); a tenant with no jurisdiction configured does not block. Where blocking applies, P-02 calls `IBlockingService.BlockAsync(reason: ErasureRefused or ErasureGranted)`, so the retained data becomes invisible (S14). The block's destruction date still honours the floor. |
| **After destruction** | For a block created by the retention sweep, destruction marks the linked retention records `Deleted` (`DataDeleted`), so the retention stream stays the authoritative disposal trail. |
| **Anchor after blocking** | Per OD-11 (settled 2026-09-23, shared with P-01 OD-2), a new retention anchor on a blocked entity and category (a returning patient) never unblocks the data automatically: the block stays, the anchor is only recorded, and unblocking happens only through the explicit unblock operation (OD-11). |
| **Order of delivery** | P-01 lands first. |

---

## Design Choices

<details>
<summary><strong>1. Package Placement — new <code>Encina.Compliance.Blocking</code>, marker and filter context in <code>Encina.DomainModeling</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New `Encina.Compliance.Blocking` for the service; `IBlockable` and `IBlockedDataFilterContext` in `Encina.DomainModeling`** | Providers already reference `Encina.DomainModeling`, so they can filter without referencing a compliance package; the service is opt-in (pay for what you use) | One more package |
| **B) Inside `Encina.Compliance.DataSubjectRights`** | Close to erasure and restriction | DSR is Marten-only (ADR-019) while blocking must work on the 10 providers; retention would have to reference DSR again, which ADR-031 just removed |
| **C) Inside `Encina.Compliance.Retention`** | Close to the floor | Same Marten-only problem; blocking is also triggered by DSR and rectification, not only by retention |

### Chosen Option: **A**

### Rationale

- `Encina.DomainModeling` gets only the data-level contract: `IBlockable` (entity marker with `BlockedAtUtc` and `BlockId`), `BlockingColumns` (column/field naming convention) and `IBlockedDataFilterContext` (scoped bypass, used only inside a disclosure scope).
- `Encina.Compliance.Blocking` holds the domain (block records, reasons, states, release roles), the service, the pipeline behavior, the disclosure, secure-copy and destruction logic, the retention and DSR integration points, and the observability.
- Provider packages implement the store and the row writer in a `Blocking/` feature subfolder (CLAUDE.md, Feature Folders).

</details>

<details>
<summary><strong>2. Granularity — row-level block marker, category mapped to entity types</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Row-level: blockable tables carry `BlockedAtUtc` and `BlockId`; a data category maps to one or more entity types** | One predicate per query, indexable; works identically on SQL and MongoDB; destruction is `DELETE … WHERE BlockId = @id` | A row that mixes categories (contact data and clinical notes in one table) is blocked whole |
| **B) Field-level: move blocked field values to a vault and null them in the row** | Blocks one category inside a mixed row | Every read must merge vault values for disclosure; the row stays visible (only fields hidden), which does not meet "excluded from viewing" for the row's existence; complex on 10 providers |
| **C) Separate block registry joined by every query (`NOT EXISTS (SELECT … FROM BlockedEntities …)`)** | No change to application tables | A join per query on every provider; MongoDB needs `$lookup`; slower and harder for application-written SQL |

### Chosen Option: **A — row-level marker**

### Rationale

- The application maps each blockable entity type to one data category (`options.MapCategory("clinical", typeof(ClinicalNote), typeof(Episode))`). An entity type holding several categories must either be split or be blocked whole; the documentation says so. OD-2 (settled 2026-09-23) confirms row-level markers as the chosen granularity.
- The data category is the same free-form string retention uses (ADR-031), not the DSR `PersonalDataCategory` enum; see OD-10 for a shared vocabulary.
- The marker columns are application-owned schema. Encina provides the EF Core model convention, the SQL snippets per provider and the MongoDB field names; it does not own the application's tables.

</details>

<details>
<summary><strong>3. Filtering on the 10 Providers — composable row filters</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A third repository class per provider (`BlockingAwareFunctionalRepository{Provider}`)** | Follows the tenant-aware pattern | Tenancy, soft delete and blocking cannot combine (a tenant-aware repository would not filter blocked rows); the combinations explode; soft delete for Dapper and ADO is already dead code this way |
| **B) Composable row filters: `SpecificationSqlBuilder` (Dapper/ADO), `SpecificationFilterBuilder` (MongoDB) and the EF model take an ordered set of `IRowFilter<TEntity>` contributors; tenancy, soft delete and blocking become contributors** | One repository per provider; filters combine; fixes the unnamed-filter replacement in EF Core by using named query filters | A refactor of the existing tenant-aware repositories before blocking can land |
| **C) Post-filter in a provider-neutral decorator over `IFunctionalRepository`** | No provider code | Breaks paging and counts (a page of 20 returns fewer), loads blocked rows into memory; not "excluded from queries" |

### Chosen Option: **B — composable row filters**

### Rationale

- The research for this plan found that filters do not compose on any provider: tenant and soft-delete filters live in separate repository classes on Dapper, ADO and MongoDB, and EF Core registers unnamed `HasQueryFilter` calls that replace each other on the same entity (`TenantDbContext.ApplyTenantQueryFilters` and `EntityConfigurationExtensions.ApplySoftDeleteQueryFilters`). The soft-delete builders on Dapper and ADO (`SoftDeleteSpecificationSqlBuilder`) are referenced by nothing.
- Contract: `IRowFilter<TEntity>` exposes `bool AppliesTo(Type entityType)` and, per provider family, a fragment builder: `SqlRowFilterFragment Build(ISqlDialect dialect, string tableAlias)` (Dapper/ADO), `FilterDefinition<TEntity> Build()` (MongoDB), and for EF Core a named query filter registered by a model convention (`HasQueryFilter("Encina.Blocking", e => e.BlockedAtUtc == null)`; EF Core 10 named filters — verify the exact API against EF Core 10.0.12 during Phase 1).
- `GetByIdAsync` stops writing inline SQL and goes through the builder, so every read path gets the same predicate.
- Because the refactor also changes tenancy and soft delete, OD-3 (settled 2026-09-23) makes it its own `[REFACTOR]` issue (issue to be opened: composable row filters — `IRowFilter<TEntity>`, per-provider fragment builders, EF Core named query filters — replacing the tenant and soft-delete filter classes on the 10 providers), landing **before** this plan as a prerequisite rather than as Phase 1 here.

</details>

<details>
<summary><strong>4. Block Record Persistence — a store on the 10 providers and Marten, evidence in the audit trail</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Event-sourced `BlockAggregate` on Marten (ADR-019 pattern)** | Event history as evidence; consistent with the other compliance modules | Blocking would need PostgreSQL even when the application's data lives on SQL Server or MySQL, while the rows it hides live on all 10 providers; SQL Server and MySQL users would get the filter but no way to block |
| **B) `IBlockRecordStore` on the 10 providers plus Marten; every state transition also writes an `AuditEntry` through `IAuditStore`** | Blocking works wherever the application's data lives; the block record and the row markers can be written in one database transaction; the audit trail (10 providers + Marten) carries the evidence | Not event-sourced; the history lives in the audit store rather than in a stream |
| **C) Only audit entries, no block records** | Least code | Destruction scheduling and disclosure need a queryable current state |

### Chosen Option: **B** (OD-1, settled 2026-09-23: confirmed, departing deliberately from the ADR-019 pattern for this module because blocking must commit in the same transaction as the application's own data; see the ADR-019 addendum noted under Provider category)

### Rationale

- Atomicity: `BlockAsync` writes the `BlockRecord` and sets the row markers in the **same** transaction of the application's database (through the provider's `IUnitOfWork`), so there is never a block without hidden rows or hidden rows without a block. With option A this would be a dual write (the REQ-037 / P-23 problem).
- Evidence: each transition (`Blocked`, `Disclosed`, `SecureCopyCreated`, `ReleasedForDestruction`, `Destroyed`, `DestructionFailed`) writes an `AuditEntry` (`Action = "blocking.<transition>"`, `EntityType = "BlockRecord"`, `EntityId = blockId`, metadata with reason, role, purpose, case reference, hash, method; no personal data) in the same unit of work where the audit store shares the connection, otherwise immediately after with a retry.
- Store table `BlockRecords` (Encina-owned): `Id`, `TenantId`, `ModuleId`, `SubjectId?`, `EntityType?`, `EntityId?`, `DataCategory`, `Reason`, `LegalBasis`, `Jurisdiction`, `State`, `BlockedAtUtc`, `DestroyNotBeforeUtc`, `LimitationPeriod`, `RetentionRecordId?`, `SourceReference?`, `SecureCopyHash?`, `SecureCopyAlgorithm?`, `SecureCopyLocation?`, `DestroyedAtUtc?`, `DestructionMethod?`, `RowCount`, `CreatedAtUtc`, `LastUpdatedAtUtc`, `Version`.

</details>

<details>
<summary><strong>5. Blocking Instead of Deleting — three entry points</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Only an explicit `IBlockingService.BlockAsync` API** | Simple | The SPEC asks for a blocking erasure strategy "instead of hard delete" on repositories; an application calling `DeleteAsync` would still destroy |
| **B) Explicit API + `BlockingErasureStrategy : IDataErasureStrategy` (DSR) + retention disposition `Block` + a provider-neutral `BlockingRepository<TEntity,TId>` decorator that turns `DeleteAsync` on a blockable entity into a block** | Every Encina deletion path can block; the decorator needs no provider code because deletion interception is not a query | More surface |

### Chosen Option: **B**

### Rationale

- `BlockingErasureStrategy` replaces DSR's `HardDeleteErasureStrategy`, which today deletes nothing (it only logs), when blocking is registered.
- The repository decorator decides per entity type and tenant jurisdiction (`BlockingOptions.BlockOnDelete`); `DeleteRangeAsync(spec)` blocks the matching rows through the row writer, one block per call.
- Queries are **not** handled by the decorator (design choice 3 explains why post-filtering is wrong).

</details>

<details>
<summary><strong>6. Disclosure — a scoped, audited bypass for statutory roles</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A global "include blocked" flag on repositories** | Simple | Too easy to misuse; not purpose-bound |
| **B) `IBlockingService.OpenDisclosureAsync(DisclosureRequest)` returns an `IAsyncDisposable` disclosure scope; inside it `IBlockedDataFilterContext.IncludeBlockedFor(blockId)` lets queries see only the rows of that block; opening checks the role and writes the audit entry; the reads are read-audited with the disclosure purpose** | Purpose-bound, role-checked, narrow (one block), evidenced twice (disclosure audit + read audit) | Requires the scoped filter context to be honoured by every provider filter (Phase 1) |
| **C) Export the blocked data to a package handed to the authority** | No bypass in live queries | Duplicates the secure-copy path; authorities may need to query |

### Chosen Option: **B**, with C available through the secure-copy writer

### Rationale

- `DisclosureRequest(Guid BlockId, string Role, string Purpose, string CaseReference, string RequestedBy)`; `Role` must be in `BlockingOptions.ReleaseRoles` (for example `court`, `prosecutor`, `supervisory-authority`) and the current actor must hold it according to `IBlockingReleaseAuthorizer` (default: the role claim in `IRequestContext`). Per OD-7 (settled 2026-09-23), release roles stay free-form strings checked against the actor's roles; ABAC (`Encina.Security.ABAC`) integration is optional and not required for this plan.
- The filter predicate inside the scope becomes `BlockedAtUtc IS NULL OR BlockId = @disclosedBlockId`; nothing else is revealed.
- `SensitiveDataAccessedNotification` is not reused (it is dead code in `Encina.Security.Audit`); the disclosure itself is the audit event.

</details>

<details>
<summary><strong>7. Destruction — Encina destroys what it blocked, through the row writer or a crypto-shredding port</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reuse the ADR-031 `IRetentionDataEraser` port** | One application port for erasure | Only retention-created blocks have a retention target; DSR-created blocks do not |
| **B) Encina's row writer deletes rows by `BlockId` (`DELETE … WHERE BlockId = @id AND TenantId = @t`) on the 10 providers; an optional `IBlockedDataDestroyer` port lets the application destroy data Encina does not own (files, remote systems); crypto-shredding once P-04 provides per-category keys** | Encina can prove what it destroyed (row count per table); the port covers the rest | Two mechanisms |

### Chosen Option: **B**

### Rationale

- The destruction sweep (`BlockDestructionService : BackgroundService`) runs under an `IDistributedLockProvider` cycle lock, per tenant, selects blocks with `State in (Blocked, ReleasedForDestruction)` and `DestroyNotBeforeUtc <= now`, re-checks legal holds and the retention floor (fail closed), calls the row writer and the optional port, and records `Destroyed` with the method and row count.
- Crypto-shredding at period end (REQ-005, REQ-006) needs keys scoped per subject and category; until P-04 (#1191) and #1144 deliver them, `DestructionMethod.CryptoShred` is rejected by the options validator.

</details>

<details>
<summary><strong>8. Superseded Versions After Rectification — a blocked-version vault</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Rely on temporal tables** | No new store | Only SQL Server and PostgreSQL have temporal repositories in Encina; MySQL and MongoDB have none |
| **B) A `BlockedVersions` vault on the 10 providers and Marten: before an update of a blockable entity whose category is configured for rectification blocking, the previous version is serialised into the vault as a blocked row (same `BlockId` rules, same destruction)** | Works on every provider; superseded versions are invisible by construction (the vault is never queried outside a disclosure scope) | Stores a serialised copy (JSON) of the previous version |
| **C) Leave superseded versions to the application** | No work | AC-005 requires Encina to block them |

### Chosen Option: **B**

### Rationale

- Capture point: the `BlockingRepository` decorator's `UpdateAsync`/`UpdateRangeAsync` loads the current row inside the same unit of work and writes it to the vault before delegating the update; the DSR rectification handler goes through the same repository.
- The vault payload may contain personal data; it is encrypted at rest when column encryption (P-46, #1250) is configured, and it is destroyed with the block. OD-9 (settled 2026-09-23) confirms the blocked-version vault on the 10 providers and Marten as the approach.

</details>

<details>
<summary><strong>9. Marten Streams — blocked stream marker and a blocking-aware aggregate repository</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Marten's native stream archiving** | Archived streams are excluded from Marten queries natively | Must be verified in Marten 9.38 (no `ArchiveStream` use exists in `src`); un-archiving for disclosure may not be supported; destruction still needs deletion |
| **B) A blocked-stream marker document (`BlockedStream { StreamId, BlockId, TenantId }`) and a `BlockingAwareAggregateRepository<T>` decorator that returns `Left(blocking.data_blocked)` on `LoadAsync` unless a disclosure scope covers the block; read models of blocked streams are filtered through the same `IRowFilter` on Marten LINQ queries** | Same semantics as the relational providers; disclosure works | A decorator on `IAggregateRepository` and a filter on read-model queries |

### Chosen Option: **B**, keeping A as an implementation detail if Phase 6 finds native archiving compatible with disclosure

### Rationale

- This applies to application aggregates stored through `Encina.Marten`, not to the compliance modules' own streams (consents, DSR requests), whose history is evidence and must stay readable to the controller.

</details>

---

## Prerequisite: Row-Filter Composition Refactor (separate `[REFACTOR]` issue, OD-3)

Per OD-3 (settled 2026-09-23), the composable row-filter refactor below is **not** Phase 1 of this plan. It is its own `[REFACTOR]` issue (issue to be opened: composable row filters replacing the tenant and soft-delete filter classes on the 10 providers) and must land and merge **before** work on this plan's phases starts, because Phase 5 (provider implementations) and Phase 7 (the blocking repository decorator) build directly on the filter contract it introduces. The task list below is kept here as the specification for that prerequisite issue; this plan's own phases are numbered starting at Phase 2 to avoid renumbering every cross-reference in this document.

> **Goal**: One repository per provider with an ordered set of composable row filters; tenancy and soft delete become filters; EF Core uses named query filters.

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.DomainModeling/Filtering/IRowFilter.cs`** (new) — `public interface IRowFilter { string Name { get; } int Order { get; } bool AppliesTo(Type entityType); }`
2. **Dapper ×3 and ADO ×3** — `Repository/ISqlRowFilter.cs` (`SqlRowFilterFragment Build(Type entityType, string? tableAlias)` returning SQL text and parameters), `Repository/SpecificationSqlBuilder.cs` (accepts `IReadOnlyList<ISqlRowFilter>`; `WHERE f1 AND f2 AND (spec)`), `Repository/FunctionalRepository{Dapper|ADO}.cs` (takes the filters by constructor; `GetByIdAsync` builds through the builder), `Tenancy/TenantRowFilter.cs` (replaces `TenantAwareSpecificationSqlBuilder` and `TenantAwareFunctionalRepository{Dapper|ADO}`), `SoftDelete/SoftDeleteRowFilter.cs` (replaces the unused `SoftDeleteSpecificationSqlBuilder`)
3. **MongoDB** — `Repository/IMongoRowFilter.cs`, `SpecificationFilterBuilder` composition, `TenantRowFilterMongoDB`, `SoftDeleteRowFilterMongoDB`; `TenantAwareFunctionalRepositoryMongoDB` and `SoftDeletableFunctionalRepositoryMongoDB` merged into `FunctionalRepositoryMongoDB`
4. **EF Core** — `Filtering/EncinaQueryFilterConvention.cs`: tenant and soft-delete filters registered as **named** query filters (`"Encina.Tenancy"`, `"Encina.SoftDelete"`); `TenantDbContext.ApplyTenantQueryFilters` and `ApplySoftDeleteQueryFilters` rewritten on it
5. **DI** — `AddTenantAwareRepository<TEntity,TId>` and `AddEncinaSoftDeleteRepository` register filters instead of repository classes; `ISoftDeleteFilterContext`/`IIncludeDeleted` are either wired into `SoftDeleteRowFilter` or removed (today nothing reads them)
6. **Tests** — existing tenancy and soft-delete integration tests on the 10 providers must pass unchanged; new tests prove tenant + soft delete combine on one entity on every provider

</details>

<details>
<summary><strong>Prompt for AI Agents — Row-Filter Prerequisite</strong></summary>

```
You are implementing the row-filter composition refactor that is a prerequisite for issue #1189 (SPEC-002
P-03): composable row filters on the 10 database providers (ADO.NET, Dapper x SqlServer/PostgreSQL/MySQL,
EF Core, MongoDB).

CONTEXT:
- Today each cross-cutting filter is a separate IFunctionalRepository implementation:
  TenantAwareFunctionalRepository{Dapper|ADO|MongoDB} + TenantAwareSpecificationSqlBuilder, and on MongoDB a
  separate SoftDeletableFunctionalRepositoryMongoDB. Dapper/ADO SoftDeleteSpecificationSqlBuilder is referenced
  by nothing. EF Core registers unnamed HasQueryFilter calls (TenantDbContext.ApplyTenantQueryFilters and
  EntityConfigurationExtensions.ApplySoftDeleteQueryFilters) that replace each other on the same entity.
- Blocking (later phases) needs a third filter that combines with both.

TASK:
Introduce IRowFilter (DomainModeling) and provider-family fragments (ISqlRowFilter, IMongoRowFilter, EF named
query-filter convention). Make SpecificationSqlBuilder / SpecificationFilterBuilder compose them. Collapse the
tenant-aware and soft-delete repository classes into the single FunctionalRepository per provider with injected
filters. Keep every existing tenancy and soft-delete test green; add tests that both filters combine.

KEY RULES:
- Parameters only, never string-concatenated values; identifiers validated (SqlIdentifierValidator).
- SQL Server [col], PostgreSQL "col", MySQL `col` quoting per provider.
- GetByIdAsync must go through the builder (no inline WHERE that bypasses filters).
- EF Core 10 named query filters: verify the API on EF Core 10.0.12 before use; if unavailable, build one combined
  lambda per entity in a single convention.
- Async database calls with CancellationToken only.

REFERENCE FILES:
- src/Encina.Dapper.PostgreSQL/Tenancy/TenantAwareFunctionalRepositoryDapper.cs
- src/Encina.Dapper.PostgreSQL/Repository/SpecificationSqlBuilder.cs
- src/Encina.Dapper.PostgreSQL/SoftDelete/SoftDeleteSpecificationSqlBuilder.cs
- src/Encina.MongoDB/Tenancy/TenantAwareFunctionalRepositoryMongoDB.cs
- src/Encina.EntityFrameworkCore/Tenancy/TenantDbContext.cs
- src/Encina.EntityFrameworkCore/Configuration/EntityConfigurationExtensions.cs
```

</details>

---

## Implementation Phases

> Phase numbering starts at 2 because Phase 1 (row-filter composition) moved to the prerequisite section above (OD-3, settled 2026-09-23); the phases below are this plan's own scope.

### Phase 2: Core Model and Abstractions

> **Goal**: The public contract of blocking.

<details>
<summary><strong>Tasks</strong></summary>

#### `src/Encina.DomainModeling/Blocking/`

1. `IBlockable` — `DateTimeOffset? BlockedAtUtc { get; }`, `Guid? BlockId { get; }`; `IBlockableEntity : IBlockable` with setters for writers
2. `BlockingColumns` — constants `BlockedAtUtc`, `BlockId`; MongoDB names `blocked_at_utc`, `block_id`
3. `IBlockedDataFilterContext` — `Guid? DisclosedBlockId { get; }`; `IDisposable Disclose(Guid blockId)` (internal setter used by the blocking service); scoped

#### New project `src/Encina.Compliance.Blocking/`

4. **Project file** — `net10.0`; references `Encina`, `Encina.DomainModeling`, `Encina.Security.Audit` (for `IAuditStore`), `Encina.Compliance.Retention` (for `ILegalHoldService`; `CalendarPeriod` itself comes from core `Encina`, P-01 OD-6). Per OD-6 (settled 2026-09-23), `Encina.Compliance.Blocking` references `Encina.Compliance.Retention` directly for the floor query and holds, and Retention exposes a port (`IRetentionFloorQuery`) that this plan's services consume; Blocking does not implement a port for Retention
5. **Model/** — `BlockRecord` (sealed record, fields of design choice 4), `BlockState { Requested, Blocked, ReleasedForDestruction, Destroyed, DestructionFailed }`, `BlockReason { ErasureGranted, ErasureRefused, Rectification, RetentionExpired, Manual }`, `BlockTarget` (discriminated: `BySubject(subjectId, category)`, `ByEntity(entityType, entityId, category)`, `ByRows(IReadOnlyList<BlockedRowRef>)`), `BlockedRowRef(Type EntityType, string EntityId)`, `DisclosureRequest`, `SecureCopyResult(string Location, string Hash, string Algorithm)`, `DestructionMethod { Delete, CryptoShred, ApplicationPort }`
6. **Abstractions/** —
   - `IBlockingService`: `BlockAsync(BlockRequest request, CancellationToken ct)` → `Either<EncinaError, BlockRecord>`; `IsBlockedAsync(string? tenantId, string subjectOrEntityId, string dataCategory, CancellationToken ct)` → `Either<EncinaError, bool>`; `OpenDisclosureAsync(DisclosureRequest request, CancellationToken ct)` → `Either<EncinaError, IAsyncDisposable>`; `CreateSecureCopyAsync(Guid blockId, CancellationToken ct)` → `Either<EncinaError, SecureCopyResult>`; `GetBlockAsync(Guid blockId, CancellationToken ct)`; `QueryBlocksAsync(BlockQuery query, CancellationToken ct)`
   - `IBlockRecordStore` (provider-implemented): `AddAsync`, `UpdateAsync` (optimistic `Version`), `GetAsync`, `QueryAsync(BlockQuery)`, `GetDueForDestructionAsync(string? tenantId, DateTimeOffset nowUtc, int batchSize)`
   - `IBlockedRowStore` (provider-implemented): `MarkBlockedAsync(Guid blockId, string? tenantId, IReadOnlyList<BlockedRowRef> rows, DateTimeOffset blockedAtUtc, CancellationToken ct)` → `Either<EncinaError, int>`; `DestroyAsync(Guid blockId, string? tenantId, CancellationToken ct)` → `Either<EncinaError, IReadOnlyDictionary<string, int>>` (rows per table)
   - `IBlockedVersionVault` (provider-implemented): `StoreAsync(Guid blockId, string? tenantId, BlockedRowRef row, string payloadJson, CancellationToken ct)`, `DestroyAsync(Guid blockId, string? tenantId, CancellationToken ct)`
   - `ISecureCopyWriter` (application port): `WriteAsync(SecureCopyContent content, CancellationToken ct)` → `Either<EncinaError, string>` (location)
   - `IBlockedDataDestroyer` (optional application port): `DestroyAsync(BlockRecord block, CancellationToken ct)` → `Either<EncinaError, Unit>`
   - `IBlockingReleaseAuthorizer`: `AuthorizeAsync(DisclosureRequest request, IRequestContext context, CancellationToken ct)` → `Either<EncinaError, Unit>`
7. **`BlockingErrors.cs`** — `blocking.data_blocked`, `blocking.not_found`, `blocking.invalid_transition`, `blocking.role_not_allowed`, `blocking.actor_lacks_role`, `blocking.purpose_required`, `blocking.floor_not_elapsed`, `blocking.legal_hold_active`, `blocking.no_rows_located`, `blocking.store_error`, `blocking.secure_copy_failed`, `blocking.destruction_failed`, `blocking.category_not_mapped`, `blocking.tenant_required`
8. **`PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt`**

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of issue #1189: the core model and abstractions of blocking.

CONTEXT:
- Blocking (LOPDGDD art. 32) keeps data but hides it from all processing and viewing; release only through an
  audited, purpose-bound disclosure to a statutory role; destruction at the end of a limitation period.
- Data categories are free-form strings (the retention vocabulary of ADR-031), not the DSR PersonalDataCategory
  enum.
- Row-level markers (BlockedAtUtc, BlockId) on blockable entities; a category maps to entity types.

TASK:
Create IBlockable, IBlockableEntity, BlockingColumns and IBlockedDataFilterContext in
src/Encina.DomainModeling/Blocking/, and the new project src/Encina.Compliance.Blocking with the Model/,
Abstractions/ and BlockingErrors listed in the Phase 2 tasks.

KEY RULES:
- ROP: every store and service method returns ValueTask<Either<EncinaError, T>>.
- Timestamps are DateTimeOffset with the AtUtc suffix; no clock reads in model types.
- XML docs cite LOPDGDD art. 32 and GDPR Art. 17/18 as the law an application may apply, not as defaults.
- PublicAPI.Unshipped.txt complete.

REFERENCE FILES:
- src/Encina.DomainModeling/IAuditable.cs (ISoftDeletable pattern)
- src/Encina.Compliance.DataSubjectRights/Abstractions/ (interface style)
- src/Encina.Compliance.Retention/Abstractions/ILegalHoldService.cs
```

</details>

---

### Phase 3: Default Service, State Machine and Pipeline Behavior

> **Goal**: Block, query and refuse processing.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Services/DefaultBlockingService.cs`** — constructor `(IBlockRecordStore, IBlockedRowStore, IBlockedDataLocator, IAuditStore, IRetentionFloorQuery, ILegalHoldService, IUnitOfWork?, IRequestContext, IBlockedDataFilterContext, IBlockingReleaseAuthorizer, ICacheProvider?, IOptions<BlockingOptions>, TimeProvider, ILogger<DefaultBlockingService>)`
   - `BlockAsync`: resolve tenant (request context or explicit; `Left(blocking.tenant_required)` when tenancy is on and none is available), locate rows (`BySubject` through `IBlockedDataLocator`, which adapts P-45's relational `IPersonalDataLocator` and the category mapping; `ByEntity` directly), compute `DestroyNotBeforeUtc = max(floorEnd, now + LimitationPeriod(jurisdiction, category))`, then in one unit of work: `IBlockRecordStore.AddAsync` + `IBlockedRowStore.MarkBlockedAsync` + audit entry; evict caches (Phase 9)
   - Idempotent: the same `SourceReference` (for example a DSR request id or a retention record id) and category returns the existing block
2. **`Model/BlockStateMachine.cs`** — allowed transitions; `Left(blocking.invalid_transition)` otherwise
3. **`BlockedDataPipelineBehavior<TRequest,TResponse>`** — mirrors `ProcessingRestrictionPipelineBehavior`: static per-generic-type attribute cache (`[ProcessesPersonalData]`, `[ProcessingActivity]`, new `[ProcessesDataCategory("clinical")]`), subject extraction through DSR's `IDataSubjectIdExtractor` (#1149 semantics), `IsBlockedAsync` → `Left(blocking.data_blocked)` in `Block` mode; **fails closed** on store errors (unlike the restriction behavior, which fails open; SPEC-002 INV-005); modes `Block`/`Warn`/`Disabled`, default `Block`
4. **`Services/IBlockedDataLocator` + `RelationalBlockedDataLocator`** — adapts `IPersonalDataLocator` (P-45) to `BlockedRowRef` lists filtered by the category mapping; returns `Left(blocking.no_rows_located)` rather than an empty success when a subject has mapped categories but no rows are found (so a misconfigured locator is visible)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of issue #1189: DefaultBlockingService, the block state machine, the
BlockedDataPipelineBehavior and the relational blocked-data locator.

CONTEXT:
- Phase 2 defined the abstractions. P-01 (#1187) provides IRetentionFloorQuery (FloorEndsAtUtc) and
  ILegalHoldService. P-45 (#1248) provides the relational IPersonalDataLocator on the 10 providers.
- The block record and the row markers are written in the same database transaction (IUnitOfWork of the
  provider). Each transition writes an AuditEntry via IAuditStore with no personal data in metadata.

TASK:
Implement the items of the Phase 3 tasks.

KEY RULES:
- DestroyNotBeforeUtc = max(FloorEndsAtUtc, BlockedAtUtc + LimitationPeriod); any error reading floors or holds
  returns Left (fail closed).
- The pipeline behavior fails closed on store errors; default mode Block; static attribute cache per generic type.
- Idempotent BlockAsync keyed by (tenant, SourceReference, category).
- TimeProvider only.

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/ProcessingRestrictionPipelineBehavior.cs
- src/Encina.Compliance.DataSubjectRights/Abstractions/IPersonalDataLocator.cs
- src/Encina.Security.Audit/Abstractions/IAuditStore.cs, AuditEntry.cs
```

</details>

---

### Phase 4: Disclosure, Secure Copy and Destruction

<details>
<summary><strong>Tasks</strong></summary>

1. **Disclosure** — `OpenDisclosureAsync`: validate role ∈ `ReleaseRoles`, purpose and case reference non-empty, `IBlockingReleaseAuthorizer` (default `ClaimsBlockingReleaseAuthorizer` checks the actor's roles in `IRequestContext`); write `AuditEntry(Action = "blocking.disclosed")` with role, purpose, case reference; set `IBlockedDataFilterContext.Disclose(blockId)`; set `IReadAuditContext.WithPurpose($"blocking-disclosure:{caseReference}")` so P-05 records every read with the purpose; the returned scope restores both on dispose
2. **Secure copy (art. 32.4)** — `CreateSecureCopyAsync`: open an internal disclosure scope (role `system:secure-copy`, audited), read the blocked rows and vault versions, canonicalise to JSON (sorted keys, UTC ISO 8601), compute SHA-256, call `ISecureCopyWriter.WriteAsync`, record hash, algorithm and location on the block and in an audit entry, move the block to `ReleasedForDestruction` with `DestroyNotBeforeUtc = max(FloorEndsAtUtc, now)` (per OD-8, settled 2026-09-23, the retention floor still binds the original after a secure copy)
3. **Destruction sweep** — `BlockDestructionService : BackgroundService` with `(IServiceScopeFactory, IOptions<BlockingOptions>, IDistributedLockProvider?, TimeProvider, ILogger)`; per cycle and per tenant: select due blocks, re-check hold and floor (fail closed), `IBlockedRowStore.DestroyAsync`, `IBlockedVersionVault.DestroyAsync`, optional `IBlockedDataDestroyer`, `Destroyed` with method and row counts, audit entry, notify retention (Phase 8); `DestructionFailed` on error, retried next cycle; cycle lock through `IDistributedLockProvider` when registered (warning logged once when not, for single-host deployments)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of issue #1189: audited disclosure, the art. 32.4 secure copy and the
destruction sweep.

CONTEXT:
- IBlockedDataFilterContext (DomainModeling) lets queries see the rows of exactly one block inside a
  disclosure scope; the provider row filters (Phase 1 + Phase 5) honour it.
- IReadAuditContext (Encina.Security.Audit) carries the read purpose; P-05 (#1193) records reads with it.

TASK:
Implement OpenDisclosureAsync, CreateSecureCopyAsync and BlockDestructionService as listed.

KEY RULES:
- Disclosure: role in ReleaseRoles, actor authorised, purpose and case reference required; audit before data.
- Secure copy: canonical JSON, SHA-256, hash recorded in the audit entry and on the block, then
  ReleasedForDestruction.
- Destruction: under a distributed cycle lock when available; re-check legal hold and floor right before
  destroying; fail closed; audited with method and row counts; TimeProvider only (FakeTimeProvider in tests).

REFERENCE FILES:
- src/Encina.Compliance.Retention/RetentionEnforcementService.cs (sweep structure, as changed by PR #1185)
- src/Encina.Security.Audit/Abstractions/IReadAuditContext.cs
- src/Encina.DistributedLock.*/ (IDistributedLockProvider usage)
```

</details>

---

### Phase 5: Provider Implementations — 10 Database Providers

<details>
<summary><strong>Tasks</strong></summary>

#### 5a. ADO.NET (SqlServer, PostgreSQL, MySQL) and 5b. Dapper (same three)

Per package, in `Blocking/`:
1. `BlockingRowFilter{ADO|Dapper}` : `ISqlRowFilter` — `(<alias>.BlockedAtUtc IS NULL OR <alias>.BlockId = @__disclosedBlockId)` when a disclosure scope is open, else `<alias>.BlockedAtUtc IS NULL`; applies to entity types implementing `IBlockable` or mapped as blockable
2. `BlockRecordStore{ADO|Dapper}` : `IBlockRecordStore` — table `BlockRecords`; optimistic concurrency on `Version`
3. `BlockedRowStore{ADO|Dapper}` : `IBlockedRowStore` — `UPDATE <table> SET BlockedAtUtc = @at, BlockId = @id WHERE Id IN (…) AND TenantId = @t AND BlockedAtUtc IS NULL` (tenant predicate only on tenant entities), `DELETE … WHERE BlockId = @id AND TenantId = @t`; table names from the entity mappings
4. `BlockedVersionVault{ADO|Dapper}` : `IBlockedVersionVault` — table `BlockedVersions` (`Id`, `BlockId`, `TenantId`, `EntityType`, `EntityId`, `PayloadJson`, `CapturedAtUtc`)
5. `Scripts/0NN_CreateBlockRecordsTable.sql`, `0NN_CreateBlockedVersionsTable.sql`, and `BlockingColumns.sql` (the `ALTER TABLE` snippet an application runs on its blockable tables, with the index `IX_<table>_BlockedAtUtc`); update `000_CreateAllTables.sql`
6. `ServiceCollectionExtensions` — `MessagingConfiguration.UseBlocking` (TryAdd, called before `AddEncinaBlocking`)

Provider notes: SQL Server `bit`/`DATETIMEOFFSET`/`UNIQUEIDENTIFIER`, `[col]`; PostgreSQL `timestamptz`/`uuid`, `"col"`; MySQL `DATETIME(6)`/`CHAR(36)`, `` `col` ``.

#### 5c. EF Core

1. `Blocking/BlockingModelConvention.cs` — for `IBlockable` entities: properties (or shadow properties when the entity only is mapped as blockable), named query filter `"Encina.Blocking"` reading `IBlockedDataFilterContext` from the `DbContext` (same technique as `TenantDbContext.CurrentTenantId`), index on `BlockedAtUtc`
2. `Blocking/BlockRecordStoreEF`, `BlockedRowStoreEF` (`ExecuteUpdateAsync`/`ExecuteDeleteAsync` with `IgnoreQueryFilters(["Encina.Blocking"])` scoped to the blocking filter only), `BlockedVersionVaultEF`, entity configurations, `ModelBuilder.ApplyEncinaBlocking()`
3. `QueryableBlockingExtensions.WhereNotBlocked<T>()` for EF projections the application writes itself

#### 5d. MongoDB

1. `Blocking/BlockingRowFilterMongoDB` : `IMongoRowFilter` (`blocked_at_utc` null or `block_id` = disclosed)
2. `BlockRecordStoreMongoDB` (collection `block_records`), `BlockedRowStoreMongoDB` (`UpdateManyAsync` / `DeleteManyAsync` with tenant filter), `BlockedVersionVaultMongoDB` (collection `blocked_versions`); indexes created at start-up

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of issue #1189: blocking on the 10 database providers.

CONTEXT:
- Phase 1 made row filters composable (ISqlRowFilter, IMongoRowFilter, EF named query filters).
- IBlockRecordStore, IBlockedRowStore and IBlockedVersionVault are defined in Encina.Compliance.Blocking.
- Providers: ADO.NET and Dapper for SqlServer, PostgreSQL and MySQL; EF Core; MongoDB. Store naming:
  {Feature}Store{Provider} (BlockRecordStoreADO, BlockRecordStoreDapper, BlockRecordStoreEF, BlockRecordStoreMongoDB).

TASK:
Implement the Blocking/ subfolder of every provider package as listed in 5a-5d, including scripts, EF
configurations, MongoDB indexes and DI registration.

KEY RULES:
- Tenant predicate on every UPDATE/DELETE of tenant entities; never block or destroy another tenant's rows.
- The disclosure predicate reveals exactly one block (BlockId = @disclosedBlockId), nothing else.
- Parameters only; identifier validation; provider-specific quoting and types.
- Async calls with CancellationToken; no IDbConnection.Open().
- TryAdd* registrations called before the core AddEncinaBlocking.

REFERENCE FILES:
- src/Encina.Dapper.PostgreSQL/Auditing/ReadAuditStoreDapper.cs (store pattern)
- src/Encina.ADO.SqlServer/Scripts/ (script numbering)
- src/Encina.EntityFrameworkCore/Tenancy/TenantDbContext.cs (filter reading a DbContext property)
- src/Encina.MongoDB/Auditing/ReadAuditStoreMongoDB.cs
```

</details>

---

### Phase 6: Marten Streams

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Marten/Blocking/BlockedStream.cs` (document: `StreamId`, `BlockId`, `TenantId`, `BlockedAtUtc`)
2. `BlockingAwareAggregateRepository<TAggregate>` decorator over `IAggregateRepository<TAggregate>` — `LoadAsync` returns `Left(blocking.data_blocked)` for a blocked stream unless the disclosure scope covers its block
3. `BlockedRowStoreMarten` : `IBlockedRowStore` for streams (`BlockedRowRef.EntityType` = aggregate type, `EntityId` = stream id); destruction deletes the stream (`Events.ArchiveStream` or hard delete — verify Marten 9.38 APIs; hard delete for destruction)
4. `BlockRecordStoreMarten`, `BlockedVersionVaultMarten` (documents)
5. Read-model filter on `IReadModelRepository` queries for read models whose id is a blocked stream id (`IBlockableReadModel` marker)
6. Registration `AddEncinaMartenBlocking()`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of issue #1189: blocking for application aggregates stored through Encina.Marten.

TASK:
Implement the BlockedStream document, BlockingAwareAggregateRepository decorator, BlockedRowStoreMarten,
BlockRecordStoreMarten, BlockedVersionVaultMarten and the read-model filter listed in the Phase 6 tasks.

KEY RULES:
- Do not apply blocking to the compliance modules' own streams (their history is evidence).
- Verify Marten 9.38 archiving and stream-deletion APIs before relying on them.
- Marten projections take dependencies through IDocumentOperations and constructor injection, never
  IServiceProvider; keep one test that registers each projection with a real store.

REFERENCE FILES:
- src/Encina.Marten/MartenAggregateRepository.cs
- src/Encina.Marten/Projections/MartenReadModelRepository.cs
```

</details>

---

### Phase 7: Blocking Instead of Deleting — Repository Decorator, DSR Strategy, Rectification Vault

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Compliance.Blocking/Repositories/BlockingRepository<TEntity,TId>`** — provider-neutral decorator over `IFunctionalRepository<TEntity,TId>` for blockable entities: `DeleteAsync`/`DeleteRangeAsync` → `IBlockingService.BlockAsync(ByRows, reason: ErasureGranted)` when `BlockOnDelete` applies to the tenant's jurisdiction; `UpdateAsync`/`UpdateRangeAsync`/`UpdateImmutableAsync` → capture the previous version into `IBlockedVersionVault` under a `Rectification` block when `BlockOnRectification` applies; reads delegate unchanged (filtering is in the providers)
2. **`AddBlockingRepository<TEntity,TId>()`** — decorates the registered `IFunctionalRepository`
3. **`src/Encina.Compliance.DataSubjectRights/Erasure/BlockingErasureStrategy.cs`** — `IDataErasureStrategy` that groups located fields by entity and calls `IBlockingService.BlockAsync(ByRows)`; registered by `AddEncinaBlocking` with `Replace` (so it wins over the no-op `HardDeleteErasureStrategy` regardless of registration order). Per OD-5(b) (settled 2026-09-23), `BlockingErasureStrategy` belongs to this issue (P-03), not to P-02

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of issue #1189: blocking as the erasure strategy of repositories, DSR erasure and
rectification.

TASK:
Implement BlockingRepository<TEntity,TId>, AddBlockingRepository<TEntity,TId>() and BlockingErasureStrategy.

KEY RULES:
- The decorator never filters reads (providers do); it only intercepts deletes and updates of blockable entities.
- Capture the previous version in the same unit of work as the update.
- BlockingErasureStrategy replaces HardDeleteErasureStrategy (a no-op today) with services.Replace.

REFERENCE FILES:
- src/Encina.DomainModeling/FunctionalRepository.cs
- src/Encina.Compliance.DataSubjectRights/Erasure/HardDeleteErasureStrategy.cs
- src/Encina.Compliance.DataSubjectRights/Erasure/DefaultDataErasureExecutor.cs
```

</details>

---

### Phase 8: Retention Integration (P-01 contract)

<details>
<summary><strong>Tasks</strong></summary>

1. **`src/Encina.Compliance.Retention`** — add `RetentionStatus.Blocked = 5`, event `RetentionRecordBlocked(RecordId, BlockId, OccurredAtUtc)`, aggregate method `MarkBlocked(Guid blockId, DateTimeOffset)` (from `Expired`), read-model field `BlockId`; `EffectiveRetentionState.Blocked` set by `IRetentionFloorQuery`
2. **Retention port for blocking** — `IRetentionBlockingHandler` in Retention (`BlockAsync(RetentionErasureTarget target, CancellationToken)` → `Either<EncinaError, Guid>`), implemented in `Encina.Compliance.Blocking` by `RetentionBlockingHandler`; the sweep calls it for `ExpiryDisposition.Block`; P-01's start-up validation checks for this port (so Retention does not reference Blocking)
3. **Destruction feedback** — after `Destroyed`, `RetentionBlockingHandler` calls `IRetentionRecordService.MarkDeletedAsync` for the linked record ids (`DataDeleted` with the block id in metadata)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 of issue #1189: the retention side of the P-01/P-03 contract.

CONTEXT:
- P-01 (#1187) added ExpiryDisposition.Block (rejected at start-up without a blocking service) and
  IRetentionFloorQuery. ADR-031 (PR #1185) defines RetentionErasureTarget.
- Retention must not reference Encina.Compliance.Blocking; it defines a port that Blocking implements.

TASK:
Add RetentionStatus.Blocked, RetentionRecordBlocked, MarkBlocked, the IRetentionBlockingHandler port and its
implementation in Encina.Compliance.Blocking; wire the sweep; mark linked records Deleted after destruction.

KEY RULES:
- Blocking never resets the retention clock; the block's destruction date honours FloorEndsAtUtc.
- Deterministic Apply(); events carry every instant.

REFERENCE FILES:
- docs/plans/retention-floor-implementation-plan-1187.md (Interaction with P-03)
- src/Encina.Compliance.Retention/Aggregates/RetentionRecordAggregate.cs
```

</details>

---

### Phase 9: Configuration, DI and Cross-Cutting Integration

<details>
<summary><strong>Tasks</strong></summary>

1. **`BlockingOptions`** — `ReleaseRoles` (set), `LimitationPeriods` (`(jurisdiction, category) → CalendarPeriod`, no default: a blockable category without a period fails start-up validation, per OD-5(a) settled 2026-09-23), `BlockOnDelete` / `BlockOnRectification` / `BlockOnErasure` per jurisdiction, `CategoryMappings`, `DestructionInterval` (default 1 hour), `DestructionBatchSize` (default 100), `EnforcementMode` of the pipeline behavior (default `Block`), `AllowedDestructionMethods`, `AddHealthCheck`
2. **`BlockingOptionsValidator`** — periods, mappings, roles non-empty when blocking is on, `CryptoShred` rejected until a per-category key provider is registered
3. **`AddEncinaBlocking(Action<BlockingOptions>)`** — service, behavior, locator adapter, authorizer, destruction hosted service, retention handler, DSR strategy replacement, health check
4. **Caching** — on block and destruction: `ICacheProvider.RemoveByPatternAsync($"{QueryCacheOptions.KeyPrefix}:*:{entityType}:*")` for each blocked entity type (EF's `QueryCacheInterceptor` pattern, needed because Dapper/ADO writes bypass it), plus `BlockingOptions.CacheKeyPatterns` declared by the application; `IsBlockedAsync` results are not cached across requests
5. **Multi-tenancy** — block records, row markers, vault rows and destruction per tenant; `IsBlockedAsync` and disclosure require the tenant; with tenancy off a fixed default tenant (`null`) applies
6. **Audit trail** — every transition audited (design choice 4); disclosures also read-audited (P-05)
7. **Transactions** — block record + markers + audit (where the audit store shares the connection) in one unit of work

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
You are implementing Phase 9 of issue #1189: options, validation, DI and cross-cutting integration.

TASK:
Implement BlockingOptions, BlockingOptionsValidator and AddEncinaBlocking; add cache eviction on block and
destruction; verify tenant scoping of every store call; wire audit entries and the unit of work.

KEY RULES:
- No default limitation period: the application states it (SPEC-002 §2.1, application owns legal decisions).
- Fail closed everywhere a decision cannot be made (INV-005).
- TryAdd* for services; services.Replace for the DSR erasure strategy.

REFERENCE FILES:
- src/Encina.Compliance.Retention/ServiceCollectionExtensions.cs
- src/Encina.EntityFrameworkCore/Caching/QueryCacheInterceptor.cs
- src/Encina.Caching/Abstractions/ICacheProvider.cs
```

</details>

---

### Phase 10: Observability

<details>
<summary><strong>Tasks</strong></summary>

1. **Register** `ComplianceBlocking = (8950, 8999)` in `src/Encina/Diagnostics/EventIdRanges.cs` (the only free slot of the compliance block) and add `Encina.Compliance.Blocking` to `AssemblyRanges` in `tests/Encina.UnitTests/Testing/Architecture/EncinaEventIdAllocationTests.cs`
2. **`Diagnostics/BlockingDiagnostics.cs`** — `ActivitySource` and `Meter` `"Encina.Compliance.Blocking"`; activities `Blocking.Block`, `Blocking.Disclose`, `Blocking.SecureCopy`, `Blocking.Destroy`, `Blocking.Check` with tags `encina.tenant_id`, `blocking.data_category`, `blocking.reason`, `blocking.role`, `blocking.outcome`, `blocking.method`
3. Counters `blocking.blocked.total` (category, reason), `blocking.rows.blocked.total`, `blocking.disclosures.total` (role), `blocking.secure_copies.total`, `blocking.destroyed.total` (method), `blocking.destruction.failed.total`, `blocking.requests.refused.total` (pipeline behavior); histogram `blocking.destruction.duration` (ms); observable gauge `blocking.destruction.overdue` (blocks past `DestroyNotBeforeUtc` + one interval)
4. **`Diagnostics/BlockingLogMessages.cs`** — `[LoggerMessage]` 8950 onwards, packed: block requested/applied/failed, idempotent hit, no rows located, request refused, disclosure opened/denied, secure copy created/failed, destruction cycle started/completed/skipped (lock), block destroyed, destruction failed, hold or floor defers destruction, options invalid
5. **Health check** — `BlockingHealthCheck` (`DefaultName = "encina-blocking"`, tags `encina, compliance, blocking, ready`): `Degraded` when overdue destructions exceed a threshold, `Unhealthy` when the block record store cannot be read; registered when `AddHealthCheck` is set. The issue marked health checks N/A; REQ-062 asks for one where there is a checkable dependency, and the block record store and the overdue destruction backlog are such dependencies.
6. **Provider-side log messages**, if any, use each provider package's registered range

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 10</strong></summary>

```
You are implementing Phase 10 (observability) of issue #1189.

TASK:
Register ComplianceBlocking = (8950, 8999) in EventIdRanges.cs and in EncinaEventIdAllocationTests; add
BlockingDiagnostics (ActivitySource and Meter "Encina.Compliance.Blocking"), the counters, histogram and gauge,
the [LoggerMessage] set from 8950 packed sequentially, and BlockingHealthCheck.

KEY RULES:
- No subject ids, entity ids, purposes or case references in tags or log messages (REQ-062); block ids
  (random Guids) are allowed; tenant id as the "encina.tenant_id" tag.
- Health check: DefaultName const, static Tags, scoped resolution through IServiceProvider.CreateScope().

REFERENCE FILES:
- src/Encina/Diagnostics/EventIdRanges.cs
- src/Encina.Compliance.Retention/Diagnostics/RetentionDiagnostics.cs
- src/Encina.Compliance.Retention/Health/RetentionHealthCheck.cs
```

</details>

---

### Phase 11: Testing

<details>
<summary><strong>Tasks</strong></summary>

#### 11a. Unit Tests (`tests/Encina.UnitTests/Compliance/Blocking/`, plus provider folders for filters)

- State machine, `DefaultBlockingService` (idempotency, destroy-not-before computation, fail-closed paths), pipeline behavior (modes, fail closed), disclosure (role, authoriser, purpose), secure copy (canonical JSON, SHA-256 of a known payload), destruction sweep (hold and floor re-check, lock), repository decorator, DSR strategy, options validator
- Row-filter SQL fragments per provider (SQL Server, PostgreSQL, MySQL quoting), MongoDB filter definitions, EF convention
- Telemetry tests with an in-memory exporter (activities and counters carry `encina.tenant_id`); log-capture test that no message carries subject ids, entity ids, purposes or case references

#### 11b. Guard Tests (`tests/Encina.GuardTests/Compliance/Blocking/` and provider folders)

- Every public constructor and method, including the 10 provider stores and filters

#### 11c. Contract Tests (`tests/Encina.ContractTests/Compliance/Blocking/`)

- `IBlockRecordStore`, `IBlockedRowStore`, `IBlockedVersionVault` contracts run against every provider implementation (same inputs, same results), instantiating the real stores

#### 11d. Property Tests (`tests/Encina.PropertyTests/Compliance/Blocking/`)

- For randomly generated specifications (criteria, ordering, paging) over randomly blocked rows, no query returns a blocked row outside a disclosure scope, and inside a scope only the disclosed block's rows appear (run on the in-process SQL builders and the MongoDB filter builder; the database-backed variant lives in integration tests)
- `DestroyNotBeforeUtc` is never earlier than the floor end or the limitation period

#### 11e. Integration Tests (`tests/Encina.IntegrationTests/Compliance/Blocking/`)

- One class per provider and collection (`ADO-SqlServer`, `ADO-PostgreSQL`, `ADO-MySQL`, `Dapper-*`, `EFCore-*`, MongoDB, Marten): blocked rows absent from `GetByIdAsync`, `ListAsync`, `FirstOrDefaultAsync`, `CountAsync`, `AnyAsync` and the three `GetPagedAsync`; tenant + soft delete + blocking combined; disclosure scope reveals one block; destruction deletes rows and vault versions under `FakeTimeProvider`; two-tenant isolation (AC-043)
- `BlockingDapperAgendaQueryTests` — an application-written Dapper query using the shipped predicate helper returns no blocked row (AC-005's application part)
- Retention integration on Marten: expiry with `ExpiryDisposition.Block` → blocked, then destroyed after `max(floor, limitation)`
- `[Collection]` fixtures, `ClearAllDataAsync` in `InitializeAsync`

#### 11f. Load Tests

- `tests/Encina.LoadTests/Compliance/Blocking/Blocking.md` — justify: blocking and disclosure are rare, per-subject administrative operations; the per-query cost is one indexed predicate covered by existing repository load tests

#### 11g. Benchmark Tests

- `tests/Encina.BenchmarkTests/Encina.Benchmarks/Compliance/Blocking/BlockingFilterBenchmarks.cs` — SQL builder with 0, 1, 2 and 3 composed filters (the row-filter prerequisite refactor touches the hot path of every repository query); or a `.md` justification if the maintainer prefers

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 11</strong></summary>

```
You are implementing Phase 11 (testing) of issue #1189.

CONTEXT:
- 10 database providers + Marten; integration tests use the shared [Collection] fixtures listed in CLAUDE.md;
  never create per-class fixtures; ClearAllDataAsync in InitializeAsync.
- Shouldly via Encina.Testing.Shouldly; FsCheck via Encina.Testing.FsCheck; FakeTimeProvider.

TASK:
Write the tests of 11a-11g, evidencing AC-005 (every sub-criterion), AC-043 and AC-044.

KEY RULES:
- Tests execute real package code; contract and property tests instantiate the real stores and builders.
- Create .github/coverage-manifest/Encina.Compliance.Blocking.json with per-file flags and targets
  (unit, guard, contract, property, integration) and add the new provider files to their manifests.

REFERENCE FILES:
- tests/Encina.IntegrationTests/Security/Audit/ReadAudit/ (per-provider integration test layout)
- tests/Encina.ContractTests/Security/Audit/ReadAudit/ReadAuditContractTests.cs
```

</details>

---

### Phase 12: Documentation and Finalization

<details>
<summary><strong>Tasks</strong></summary>

1. XML documentation on all public APIs
2. `changelog.d/1189-blocked-data-state.added.md`; `changelog.d/1189-blocked-data-state.changed.md` (row-filter refactor, tenant-aware repository classes removed, DSR default strategy replaced when blocking is on)
3. `src/Encina.Compliance.Blocking/README.md`; provider READMEs (Blocking section)
4. `docs/features/blocked-data-state.md` — how blocking works, configuration, the column convention and SQL snippets per provider, the predicate helper and `WhereNotBlocked()` for application-written queries, disclosure, secure copy, destruction, interaction with retention and DSR; states plainly that queries the application writes itself are the application's part (REQ-005)
5. ADR-033 (proposed) — composable row filters; blocking persistence on the 10 providers plus Marten instead of Marten-only (OD-1, settled 2026-09-23); row-level granularity; plus an ADR-019 addendum recording the exception for this module
6. `docs/INVENTORY.md`, `PublicAPI.Unshipped.txt` in every touched package
7. `dotnet build Encina.slnx --configuration Release` → 0 warnings; `dotnet test` → all pass; every coverage flag at its manifest target

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 12</strong></summary>

```
You are finalising issue #1189.

TASK:
Write XML docs, changelog.d fragments (bullets starting with "-"), the package README, provider README sections,
docs/features/blocked-data-state.md and ADR-033; update INVENTORY and PublicAPI files; run
dotnet run .github/scripts/changelog-fragments.cs -- --check, build with zero warnings, run the tests.

KEY RULES:
- English only; never name the reference application.
- LOPDGDD art. 32 is presented as the Spanish rule an application may apply; the design is jurisdiction-neutral.
- No hand-typed coverage figures (SPEC-001 covref markers only).

REFERENCE FILES:
- changelog.d/README.md
- docs/features/data-retention.md (feature page style)
```

</details>

---

## Research

### Standards and Legal Sources

| Source | Provision | Relevance |
|--------|-----------|-----------|
| LOPDGDD (LO 3/2018) | Art. 32.1–32.3 | Block on rectification or erasure; no processing, including viewing; available only to courts, prosecutors and authorities during limitation periods; then destroy [V] |
| LOPDGDD | Art. 32.4 | Secure copy with digital evidence of authenticity where blocking is disproportionate [V] |
| GDPR | Art. 17, 18, 19 | Erasure, restriction, notification of recipients |
| GDPR | Art. 5(1)(e), 5(2) | Storage limitation; accountability (audit of every transition) |
| Código Civil | Art. 1964.2 | General 5-year limitation period [K] (SPEC-002 §12 question 5) |
| SPEC-002 | REQ-005, AC-005, S14, REQ-061, REQ-062 | The requirement this plan implements |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `IFunctionalRepository`, `SpecificationSqlBuilder`, `SpecificationFilterBuilder` | `Encina.DomainModeling`, provider `Repository/` folders | Filter composition point (Phase 1) |
| Tenant-aware repositories and EF tenant filter | provider `Tenancy/` folders, `TenantDbContext` | Model for a filter reading a scoped context; merged into composable filters |
| `ISoftDeletable`, soft-delete builders | `Encina.DomainModeling`, provider `SoftDelete/` folders | Second filter to compose; Dapper/ADO builders currently unused |
| `ProcessingRestrictionPipelineBehavior` | `Encina.Compliance.DataSubjectRights` | Pattern for `BlockedDataPipelineBehavior` |
| `IPersonalDataLocator` (relational, P-45) | #1248 | Locating a subject's rows |
| `IRetentionFloorQuery`, `ILegalHoldService`, `CalendarPeriod` | P-01 (#1187), `Encina.Compliance.Retention` | Destruction date, holds, periods |
| `IAuditStore`, `IReadAuditContext` | `Encina.Security.Audit` | Evidence of transitions; purpose of disclosure reads |
| `IDistributedLockProvider` | `Encina.DistributedLock.*` | Destruction cycle lock |
| `ICacheProvider.RemoveByPatternAsync`, `QueryCacheInterceptor` | `Encina.Caching`, `Encina.EntityFrameworkCore` | Evicting cached blocked data |
| `IAggregateRepository`, `IReadModelRepository` | `Encina.Marten` | Blocked streams |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.Blocking` | **8950–8999** (new `ComplianceBlocking`) | The last free slot of the compliance block (8100–8999); register first (ADR-021) |
| Provider packages | their registered ranges | Only if a provider store logs; none planned |
| `Encina.Compliance.Retention` | 8500–8599 | Phase 8 adds at most two messages, in the free slots left after P-01 |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Phase 1 refactor | ~35 | 10 providers: filters, builders, repositories, DI; tests adjusted |
| Core package (Phases 2-4, 9-10) | ~30 | model, abstractions, service, behavior, disclosure, secure copy, sweep, options, diagnostics, health |
| DomainModeling | ~4 | marker, columns, filter context, row filter |
| ADO ×3 + Dapper ×3 (Phase 5) | ~36 | 4 classes + 3 scripts × 6 |
| EF Core (Phase 5) | ~7 | convention, stores, configurations, extensions |
| MongoDB (Phase 5) | ~5 | filter, stores, documents |
| Marten (Phase 6) | ~6 | document, decorator, stores, filter |
| Retention and DSR integration (Phases 7-8) | ~6 | |
| Tests (Phase 11) | ~55 | across 7 test types and 11 stores |
| Documentation (Phase 12) | ~8 | |
| **Total** | **~190** | Of which ~35 belong to the Phase 1 refactor |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing issue #1189 (SPEC-002 P-03): a blocked data state (LOPDGDD art. 32 bloqueo) on the 10
database providers and Marten.

PROJECT CONTEXT:
- .NET 10 / C# 14, ROP (Either<EncinaError, T>), pre-1.0: breaking changes preferred over layers.
- 10 database providers: ADO.NET and Dapper for SqlServer/PostgreSQL/MySQL, EF Core, MongoDB; plus Marten.
- Satellite pattern: feature subfolder Blocking/ per provider; TryAdd registrations before the core.
- Prerequisites: P-01 #1187 (floor, ExpiryDisposition.Block, IRetentionFloorQuery), P-45 #1248 (relational
  IPersonalDataLocator), PR #1185 (ADR-031).

IMPLEMENTATION OVERVIEW:
Phase 1: composable row filters on the 10 providers (tenancy + soft delete + blocking combine; EF named filters)
Phase 2: IBlockable etc. in DomainModeling; new Encina.Compliance.Blocking abstractions and model
Phase 3: DefaultBlockingService, state machine, BlockedDataPipelineBehavior (fail closed), locator adapter
Phase 4: audited disclosure scope, art. 32.4 secure copy (SHA-256), destruction sweep under a lock
Phase 5: provider stores, row writer, vault, scripts, EF convention, MongoDB filter (10 providers)
Phase 6: Marten blocked streams
Phase 7: blocking repository decorator, DSR BlockingErasureStrategy, rectification vault
Phase 8: retention integration (RetentionStatus.Blocked, IRetentionBlockingHandler)
Phase 9: options, validation, DI, caching eviction, tenancy, audit, transactions
Phase 10: ActivitySource/Meter "Encina.Compliance.Blocking", EventIds 8950-8999, health check
Phase 11: unit, guard, contract, property, integration (10 providers + Marten), load (.md), benchmark
Phase 12: docs, changelog.d, ADR-033, INVENTORY, PublicAPI

KEY PATTERNS:
- Row-level markers BlockedAtUtc/BlockId; a data category maps to entity types.
- Block record + row markers in one unit of work; every transition audited through IAuditStore.
- DestroyNotBeforeUtc = max(FloorEndsAtUtc, BlockedAtUtc + LimitationPeriod); holds suspend destruction.
- Disclosure reveals exactly one block, for an authorised statutory role, with purpose and case reference.
- Fail closed everywhere; tenant-scoped everywhere; no personal data in telemetry.

REFERENCE FILES:
- docs/plans/retention-floor-implementation-plan-1187.md
- src/Encina.Dapper.PostgreSQL/Tenancy/TenantAwareFunctionalRepositoryDapper.cs
- src/Encina.Compliance.DataSubjectRights/ProcessingRestrictionPipelineBehavior.cs
- docs/specifications/SPEC-002-eu-regulatory-readiness.md (REQ-005, AC-005, S14)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ | Evict query-cache entries of blocked entity types on block and destruction (Dapper/ADO writes bypass EF's interceptor); application-declared patterns; `IsBlockedAsync` not cached across requests |
| 2 | OpenTelemetry | ✅ | `Encina.Compliance.Blocking` ActivitySource and Meter; `encina.tenant_id` attribute; no subject or entity ids |
| 3 | Structured Logging | ✅ | `[LoggerMessage]` in the new `ComplianceBlocking` range 8950–8999 (ADR-021) |
| 4 | Health Checks | ✅ | `BlockingHealthCheck` (block store readable, overdue destructions); the issue said N/A, but REQ-062 asks for one where a checkable dependency exists |
| 5 | Validation | ✅ | Options validator (periods, mappings, roles, methods); request validation of disclosure input |
| 6 | Resilience | ❌ | No call to an external system; the application's `ISecureCopyWriter` and `IBlockedDataDestroyer` are responsible for their own retries, and failures leave the block in a retriable state |
| 7 | Distributed Locks | ✅ | Destruction sweep cycle lock through `IDistributedLockProvider` (the issue marked N/A for block and disclose, which stay lock-free: they act on one block with optimistic concurrency) |
| 8 | Transactions | ✅ | Block record, row markers and (where possible) audit entry in one unit of work; version capture in the same unit of work as the update |
| 9 | Idempotency | ❌ | Not a message or request entry point; `BlockAsync` is idempotent by (tenant, source reference, category) by design |
| 10 | Multi-Tenancy | ✅ | REQ-061/AC-043: records, markers, vault, disclosure and destruction per tenant; tenant predicate on every write |
| 11 | Module Isolation | ❌ | SPEC-002 requires no module scoping; `ModuleId` is stored on block records for traceability only; `ModuleId` in messaging stays with #747 |
| 12 | Audit Trail | ✅ | Every transition audited with reason, role, purpose, case reference, hash and method; disclosure reads read-audited (P-05) |

---

## Provider Matrix

| Provider | Row marker convention | Row filter | Block record store | Row writer / destroy | Version vault | Integration test |
|----------|:-:|:-:|:-:|:-:|:-:|:-:|
| ADO-SqlServer | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| ADO-PostgreSQL | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| ADO-MySQL | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Dapper-SqlServer | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Dapper-PostgreSQL | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Dapper-MySQL | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| EFCore-SqlServer | ✅ | ✅ (named filter) | ✅ | ✅ | ✅ | ✅ |
| EFCore-PostgreSQL | ✅ | ✅ (named filter) | ✅ | ✅ | ✅ | ✅ |
| EFCore-MySQL | ✅ | ✅ (named filter) | ✅ | ✅ | ✅ | ✅ |
| MongoDB | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Marten (streams) | stream marker | aggregate repository decorator + read-model filter | ✅ | ✅ | ✅ | ✅ |

## Test Matrix

| Test Type | Required? | Scope | Notes |
|-----------|:---------:|-------|-------|
| UnitTests | ✅ | Every new type and branch; SQL fragments per dialect; telemetry redaction | |
| GuardTests | ✅ | Public constructors and methods, all providers | |
| ContractTests | ✅ | Store and row-writer contracts across the 10 providers and Marten | Real implementations |
| PropertyTests | ✅ | Blocked rows never returned by generated specifications; destruction date invariant | FsCheck |
| IntegrationTests | ✅ | 10 providers and Marten via `[Collection]` fixtures; two tenants; Dapper agenda query with the helper | AC-005 |
| LoadTests | 📄 | `Blocking.md` justification | Administrative, per-subject operations |
| BenchmarkTests | ✅ | Composed row filters in the SQL builder | Phase 1 touches every query path |

## Public API Changes

| Change | Kind |
|--------|------|
| New package `Encina.Compliance.Blocking` (service, stores, ports, options, behavior, errors, diagnostics) | Added |
| `IBlockable`, `IBlockableEntity`, `BlockingColumns`, `IBlockedDataFilterContext`, `IRowFilter` in `Encina.DomainModeling` | Added |
| `Blocking/` stores and filters in the 10 provider packages; `ApplyEncinaBlocking()`, `WhereNotBlocked()` | Added |
| `TenantAwareFunctionalRepositoryDapper`, `TenantAwareFunctionalRepositoryADO`, `TenantAwareFunctionalRepositoryMongoDB`, `TenantAwareSpecificationSqlBuilder`, `SoftDeleteSpecificationSqlBuilder`, `SoftDeletableFunctionalRepositoryMongoDB` | Removed (replaced by composable filters, Phase 1) |
| `SpecificationSqlBuilder` / `SpecificationFilterBuilder` constructors | Changed (breaking) |
| `RetentionStatus.Blocked`, `RetentionRecordBlocked`, `IRetentionBlockingHandler` in `Encina.Compliance.Retention` | Added |
| `BlockingErasureStrategy` in `Encina.Compliance.DataSubjectRights` | Added |

## Migration Notes

None for users (pre-1.0). Applications that adopt blocking add the two marker columns (or MongoDB fields) to their blockable tables; the plan ships the SQL snippets per provider and the EF convention. The row-filter prerequisite refactor removes the tenant-aware repository classes; registration helpers keep their names so application code changes only where it referenced the classes directly.

## Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Row-filter prerequisite touches every repository query on 10 providers | Regressions in tenancy isolation | Keep every existing tenancy test; add combination tests; delivered as its own `[REFACTOR]` issue (OD-3) so it can be reviewed and merged independently |
| EF Core 10 named query filter API differs from the assumption | EF phase design changes | Verify in the prerequisite issue before building on it; fallback: one combined lambda per entity |
| Row-level granularity blocks mixed-category rows whole | Over-blocking of data still needed | Documented; category mapping validation warns when an entity type is mapped to two categories (OD-2, settled 2026-09-23: row-level markers accepted) |
| Application-written SQL forgets the predicate | Blocked data visible (the application's part) | Helper, EF extension, feature page, analyzer as a later idea; the reference scenario proves the pattern |
| Destruction depends on the application's optional ports | Data outside Encina not destroyed | Health check shows overdue destructions; the port failure keeps the block `DestructionFailed` and retried |
| Scope size (~190 files) | Long-running PR, hard review | Split into sub-issues along the phases (OD-12, settled 2026-09-23) |

---

## Decisions (settled 2026-09-23)

1. **OD-1 — Persistence of block records.** A store on the 10 providers plus Marten, with evidence in the audit trail, or an event-sourced `BlockAggregate` on Marten as ADR-019 does for the other compliance modules (PostgreSQL only)?
   **Decision:** a store on the 10 providers plus Marten, with evidence in the audit trail. This deliberately departs from ADR-019 for this module, because blocking must commit in the same transaction as the application's own data; ADR-019 needs an addendum recording the exception (planned alongside ADR-033, Phase 10).
2. **OD-2 — Granularity.** Row-level markers with a category-to-entity-type mapping, blocking mixed-category rows whole, or field-level blocking with a vault for field values?
   **Decision:** row-level markers, as the plan implements.
3. **OD-3 — Row-filter composition.** Land the composable-filter refactor of the 10 providers as Phase 1 of this issue, or as a separate `[REFACTOR]` issue first?
   **Decision:** the composable row-filter refactor of the 10 providers becomes its own `[REFACTOR]` issue (issue to be opened) that lands **before** this plan, as a prerequisite. It has been removed from this plan's Phase 1 (see the "Prerequisite" section above); this plan's phases are numbered from Phase 2.
4. **OD-4 — When blocking replaces deletion.** Per jurisdiction, does every DSR erasure and every retention expiry block first (the literal reading of LOPDGDD art. 32.1 for Spain), or only data retained after a refused erasure (the S14 wording)? What is the default for a tenant with no jurisdiction configured?
   **Decision:** blocking depends on the jurisdiction. For Spain, every DSR erasure and every retention expiry blocks first (literal reading of LOPDGDD art. 32.1). A tenant with no jurisdiction configured does not block.
5. **OD-5 — Limitation periods and the DSR strategy.** (a) No default limitation period, start-up failure when a blockable category has none, or a 5-year default (Código Civil art. 1964.2, [K])? (b) Does `BlockingErasureStrategy` belong to this issue or to P-02 (#1188)?
   **Decision:** (a) no default limitation period; start-up fails when a blockable category has none, as the plan implements. (b) `BlockingErasureStrategy` belongs to this issue.
6. **OD-6 — Dependency direction with Retention.** `Encina.Compliance.Blocking` references `Encina.Compliance.Retention` (for the floor query and holds) and Retention exposes a port that Blocking implements, or both talk through ports in a shared package?
   **Decision:** Blocking references Retention, and Retention exposes a port that Blocking implements, as the plan implements.
7. **OD-7 — Release roles.** Free-form role strings configured by the application and checked against the actor's roles, or a fixed enum of statutory roles plus custom ones? Should the check go through ABAC instead of claims?
   **Decision:** free-form role strings checked against the actor's roles, as the plan implements. ABAC (`Encina.Security.ABAC`) integration is optional and not required.
8. **OD-8 — Floor after a secure copy.** Once an art. 32.4 secure copy exists, may the original be destroyed before the retention floor ends, or does the floor still bind the original?
   **Decision:** the retention floor still binds the original after a secure copy, as the plan implements.
9. **OD-9 — Superseded versions.** A blocked-version vault on the 10 providers and Marten, temporal tables where available, or leave versions to the application?
   **Decision:** a blocked-version vault on the 10 providers and Marten, as the plan implements.
10. **OD-10 — Shared data-category vocabulary.** Retention (string), DSR (`PersonalDataCategory` enum), blocking (string) and read audit (P-05, string) name categories differently.
    **Decision:** introduce one shared data-category type in core `Encina` now, used by Retention, DSR, Blocking and read audit. The migration of existing modules onto it is planned within this plan's phases or as a prerequisite issue (issue to be opened: migrate Retention, DSR and read audit's free-form category strings and the DSR `PersonalDataCategory` enum onto the shared core type).
11. **OD-11 — Unblocking.** May blocked data ever be unblocked, and if so by whom and with what audit? Linked to P-01 OD-2.
    **Decision:** unblocking is allowed only through an explicit operation, requiring a configured role and a mandatory reason, and writing an audit entry. It never happens automatically (including on a new retention anchor, P-01 OD-2).
12. **OD-12 — Delivery.** One issue as planned, or split into sub-issues?
    **Decision:** split into sub-issues (issues to be opened): core and disclosure; providers; Marten; vault; retention and DSR wiring. The row-filter refactor is already its own separate prerequisite issue under OD-3.

## Spec Gaps Found

- REQ-005 blocks "data" by category, but relational storage is by row; a row mixing categories cannot be blocked for one category without field-level machinery (OD-2, resolved: row-level markers accepted, mixed-category rows blocked whole).
- REQ-005 requires blocking "versions superseded by a rectification", but Encina keeps no previous versions except in the SQL Server and PostgreSQL temporal repositories (OD-9, resolved with the blocked-version vault).
- REQ-005 does not state whether, under LOPDGDD art. 32, every erasure blocks first or only refused or retained data does (OD-4, resolved: jurisdiction-dependent, Spain blocks first).
- REQ-005 assumes a working deletion path to replace, but DSR's default `HardDeleteErasureStrategy` deletes nothing, and no relational `IPersonalDataLocator` exists yet (P-45, #1248).
- The repository layer has no filter composition: tenant and soft-delete filters live in separate repository classes on Dapper, ADO and MongoDB; the Dapper and ADO soft-delete builders are unused; EF Core's unnamed filters replace each other on an entity that is both tenant-scoped and soft-deletable (to be verified; if confirmed, a tenant filter can be lost, which is a defect of its own). Fixed by the OD-3 prerequisite issue.
- `ISoftDeleteFilterContext`/`IIncludeDeleted` are set by `SoftDeleteQueryFilterBehavior` but read by nothing.
- The data-category vocabulary differs across Retention, DSR, blocking and read audit (OD-10, resolved with a shared core type).
- REQ-062 asks for a health check where a checkable dependency exists; the issue marked it N/A.

---

## Next Steps

1. OD-1 … OD-12 are settled (2026-09-23); link this plan from issue #1189
2. Open the prerequisite `[REFACTOR]` issue (OD-3) and the sub-issues of OD-12 and OD-10 before starting
3. One commit per phase; the final commit references `Fixes #1189`
