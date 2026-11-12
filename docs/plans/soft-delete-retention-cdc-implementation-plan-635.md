# Implementation Plan: Soft Delete Retention Enforcement via CDC

> **Issue**: [#635](https://github.com/dlrivada/Encina/issues/635) (`area-cdc`, `priority-low`, `p1-recommended`)
> **Type**: Feature
> **Complexity**: High (a CDC handler, a retention engine decision, a purge executor on the 10 providers, a follow-up for crypto-shredding)
> **Estimated Scope**: ~1,400-1,900 lines of production code + ~2,500-3,200 lines of tests (about 158 files and up to about 10,050 line-changes in total with manifests, PublicAPI and documentation), delivered in 19 pull requests of at most 15 files and 1,500 lines each
> **Related plans of the same family (#633, #634, #636, #2393)**: independent of #633 and #2393. #634 and #636 add other CDC handlers through the same `CdcConfiguration` seam; #636 also decides how a CDC consumer excludes infrastructure tables. Sibling plans are referred to by issue number only.

---

## Summary

When a soft delete is captured by CDC, the permanent (hard) delete of the row is arranged for the end of a configured retention period, so personal data is not kept longer than needed. Nothing enforces a retention period for soft-deleted rows today.

### What the code does today (2026-11-12, `main` at `c49b987e9`)

1. **Soft delete exists, retention enforcement for it does not.** `ISoftDeletable` has `IsDeleted`, `DeletedAtUtc`, `DeletedBy` (read-only view) and `ISoftDeletableEntity` adds the mutable setters (`src/Encina.DomainModeling/IAuditable.cs`); neither interface exposes an entity id. For EF Core `SoftDeleteInterceptor` turns a delete into an update (`src/Encina.EntityFrameworkCore/SoftDelete/SoftDeleteInterceptor.cs`, `TimeProvider` injected). `ISoftDeleteRepository<TEntity, TId>` (`src/Encina.DomainModeling/Repository.cs:272`) declares `RestoreAsync` and `HardDeleteAsync`; only EF Core implements it (`SoftDeleteRepositoryEF`). MongoDB's `SoftDeletableFunctionalRepositoryMongoDB` implements `IFunctionalRepository<TEntity, TId>` with soft delete built in, not `ISoftDeleteRepository` (`SoftDeletableFunctionalRepositoryMongoDB.cs:65`, returning `EncinaError`). The ADO.NET and Dapper packages have `ISoftDeleteEntityMapping<TEntity, TId>` (column and property names, `GetIsDeleted`, `SetDeletedAtUtc`) and a specification SQL builder, but no hard delete.
2. **A retention module already exists, and the issue does not mention it.** `Encina.Compliance.Retention` tracks retention per entity and data category (`IRetentionRecordService.TrackEntityAsync(entityId, dataCategory, policyId, retentionPeriod, tenantId, moduleId)`), honors legal holds (`ILegalHoldService.HasActiveHoldsAsync`), marks records expired in `RetentionEnforcementService`, delegates the erasure to an application-supplied `IRetentionDataEraser.EraseAsync(RetentionErasureTarget)` and publishes notifications. Its aggregates are event-sourced on Marten (`RetentionMartenExtensions`). Scheduling a hard delete outside it would ignore legal holds.
3. **Scheduling carries domain messages.** `SchedulerOrchestrator.ScheduleAsync<TRequest>(request, executeAt, ct)` (`src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs`) persists the request in the scheduled message store; AGENTS.md section 3 says Encina scheduling carries domain messages.
4. **The handler contract and its limits.** `IChangeEventHandler<TEntity>` is routed one handler per entity type (`CdcDispatcher.cs:61-70`, `ToFrozenDictionary` keyed by entity type) and an update with no before image is a `DeserializationFailed` `Left` (`CdcDispatcher.cs:189`). SQL Server Change Tracking never supplies a before image for an update, so the issue's handler (`false -> true` on `IsDeleted`) cannot run there until the dispatcher accepts such updates. A `Left` from the dispatcher is neither retried nor dead-lettered (#2343, open).
5. **There is no entity key store.** The issue's `_keyStore.DestroyKeyAsync(entityId, ct)` has no counterpart; crypto-shredding exists only in `Encina.Marten.GDPR` for event-sourced data (`[CryptoShredded]`).

### Scope

- **Implements**: #635 without crypto-shredding. `CryptoShredOnExpiry` is not delivered; the follow-up issue is drafted with this plan.
- **Affected packages**: the bridge package of Design Choice 8 (recommended `Encina.Cdc.SoftDeleteRetention`: handler, options, sweep, configuration; otherwise `Encina.Cdc`), `Encina.DomainModeling` (the `ISoftDeletePurger` contract), `Encina.Compliance.Retention` (consumer, if Design Choice 1 picks the module; possibly a small change for Design Choices 5 and 11), `Encina.Messaging` (consumer, if it picks scheduling), `Encina.ADO.SqlServer|PostgreSQL|MySQL`, `Encina.Dapper.SqlServer|PostgreSQL|MySQL`, `Encina.EntityFrameworkCore`, `Encina.MongoDB` (purge executor), `Encina.OpenTelemetry`.
- **Provider category**: Database, all 10 providers (AGENTS.md section 5): the CDC capture comes from four connectors, the purge executor is implemented per provider.
- **Compliance scope**: the plan cites GDPR and the Spanish organic law 3/2018 (LOPDGDD); texts of other jurisdictions in the issue (CCPA) are out of scope.
- **Corrections to the issue text**: `RetentionPolicy` becomes `SoftDeleteRetentionPolicy` (the Retention module already has policies); the policy needs an id selector because `ISoftDeletable` has no id; `AddPolicy<TEntity, TId>(idSelector, configure)`.
- **Package placement** (Design Choice 8): the soft-delete specific code (policy, options, handler, tracker, eraser, sweep, configuration) goes to a bridge package, recommended name `Encina.Cdc.SoftDeleteRetention`, not into `Encina.Cdc`, whose project references are only `Encina`, `Encina.Caching` and `Encina.Messaging` (`Encina.Cdc.csproj`). Putting the Retention adapter into `Encina.Cdc` would make every CDC user depend on `Encina.Compliance.Retention` and, through it, on `Encina.Marten`. The final name and placement are the maintainer's.
- **Split into pull requests** (counts include tests, manifests and PublicAPI lines of the code in the PR; the numbering is the merge order and a PR only uses types created in an earlier PR, so `ISoftDeletePurger` comes first):

  | PR | Content (phase) | Files | Lines (est.) | Depends on |
  |----|-----------------|-------|--------------|------------|
  | 1 | `ISoftDeletePurger`, its result type, its item type, a contract test base, PublicAPI, manifest in `Encina.DomainModeling` (Phase 4 task 1) | 6 | ~250 | none |
  | 2 | The bridge project, created here because it is the first PR that adds files to it (project file, `Encina.slnx` entry, `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt`, manifest), then policy, builder, options, validator, error codes (with categories), unit and guard tests (Phase 1) | 13 | ~700 | 1 |
  | 3 | `ISoftDeleteRetentionTracker`, `SoftDeleteObservation`, `SoftDeleteRetentionCdcHandler<TEntity>`, `SoftDeleteRetentionLog` (4870-4875), the `AssemblyRanges` entry that maps the bridge assembly to the `Cdc` range in `EncinaEventIdAllocationTests` (checked and run here, where the first log class appears), PublicAPI edit, handler unit, guard, log and property (expiry arithmetic) tests, manifest (Phase 2) | 11 | ~650 | 2 |
  | 4-6 | Purgers ADO.NET for SQL Server (PR 4), PostgreSQL (PR 5), MySQL (PR 6): purger, DI extension, PublicAPI, manifest, unit test, guard test, integration test, contract class of the 10-row matrix (Phase 4) | 8 each | ~450 each | 1 |
  | 7-9 | Purgers Dapper for SQL Server (PR 7), PostgreSQL (PR 8), MySQL (PR 9), same eight files each (Phase 4) | 8 each | ~450 each | 1 |
  | 10 | EF Core purger: purger, DI extension, PublicAPI, manifest, unit, guard, 3 integration classes and 3 contract classes (one per database) (Phase 5) | 12 | ~700 | 1 |
  | 11 | MongoDB purger: purger, DI extension, PublicAPI, manifest, unit, guard, integration, contract class (Phase 5) | 8 | ~450 | 1 |
  | 12 | Module change in `Encina.Compliance.Retention`, only if the maintainer's answers to Design Choices 5 and 11 need it (a distinct restored outcome, an expiry-based overload): `RetentionEnforcementService`, the eraser result type, PublicAPI, manifest, 2 unit tests, 1 guard test | 7 | ~400 | none |
  | 13 | `RetentionModuleSoftDeleteTracker`, `SoftDeleteRetentionEraser` composite, PublicAPI, manifest, 2 unit tests, guard test, DI order proof (both registration orders, application eraser kept) (Phase 3 tasks 1-2) | 8 | ~700 | 3, 12 |
  | 14 | `SoftDeleteRetentionSweepService` + unit and guard tests + manifest (Phase 3 task 3) | 7 | ~450 | 4-11, 13 |
  | 15 | `WithSoftDeleteRetention`, registration method, startup validator, DI proof, PublicAPI, manifest (Phase 6) | 8 | ~500 | 13, 14 |
  | 16 | Metrics, activity source, audit entry, OpenTelemetry subscription, unit and guard tests, manifest (Phase 7) | 8 | ~500 | 15 |
  | 17 | End-to-end integration tests on the 9 relational providers (shared base + 9 classes) (Phase 8 task 5) | 10 | ~1,100 | 4-10, 15 |
  | 18 | End-to-end MongoDB and Retention-module-on-Marten integration tests, load and benchmark justification `.md` files (Phase 8) | 5 | ~500 | 17 |
  | 19 | Changelog fragment, bridge README, `Encina.Cdc` README, `docs/features/cdc.md`, Retention page, `docs/INVENTORY.md`, ADR (Phase 9) | 7 | ~450 | 18 |

  Total: 19 PRs, 158 files, up to about 10,050 line-changes (the sum of the per-PR upper estimates); no PR exceeds 15 files or 1,500 lines. PRs 4-11 are independent of each other after PR 1. PR 12 is skipped if the answers to Design Choices 5 and 11 need no module change (then 18 PRs and 151 files).

---

## Design Choices

<details>
<summary><strong>1. The retention engine — Encina scheduling or the Retention module</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Scheduling** (the issue): the handler schedules a `HardDeleteCommand` (entity type and id) at `DeletedAtUtc + RetentionPeriod` through `SchedulerOrchestrator` | Works on any database with no Marten; small; the scheduled message store already persists and retries | No legal hold check, no retention record, no expiry notifications; a second retention mechanism beside the module; cancelling after a restore is not possible, the command re-reads the row instead |
| **B) Retention module**: the handler calls `IRetentionRecordService.TrackEntityAsync` with the remaining period; `RetentionEnforcementService` expires the record, checks legal holds, and calls an Encina-provided `SoftDeleteRetentionEraser` that purges the row | One retention bookkeeping; legal holds honored (GDPR Art. 17(3)(e) legal claims); the event-sourced record is the audit of when deletion was requested and executed | Requires `Encina.Compliance.Retention` and Marten on PostgreSQL even for an application whose data is on SQL Server; the module's clock starts at tracking, so the handler passes `DeletedAtUtc + period - now` |
| **C) Both behind one interface**, the application picks | Serves both deployments | Two implementations to build and test; double the surface |

### Chosen Option: **B — the Retention module, behind `ISoftDeleteRetentionTracker`** (recommended, pending the maintainer)

### Rationale

Deleting a row that a legal hold covers is the failure the module exists to prevent, and a health-data application (SPEC-002) needs the hold and the record. A wins for an application that does not run the Retention module and Marten and accepts that holds are its own concern; C wins if both kinds of deployment are expected. The handler depends only on `ISoftDeleteRetentionTracker`, so switching engine does not change the CDC part. If A is chosen, Phase 3 builds `SchedulerSoftDeleteRetentionTracker` and `HardDeleteCommand` instead of the module adapter.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Whether CDC is the only trigger</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) CDC only** (the proposal) | Real time; no polling | Rows soft-deleted before the connector started, or whose tracking was lost, are never purged |
| **B) CDC plus a periodic reconciliation sweep** (`SoftDeleteRetentionSweepService`) that tracks any soft-deleted row without a retention record | Closes the gap; erasure is provable for all data | Polls the table at the sweep interval (default 24 hours), the cost the issue wanted to avoid, but only once a day |
| **C) Sweep only** | Simplest; no CDC infrastructure | The CDC feature the issue asks for is not delivered |

### Chosen Option: **B — CDC plus a daily sweep** (recommended, pending the maintainer)

### Rationale

Storage limitation (GDPR Art. 5(1)(e)) is a duty for all data, not for the data the connector happened to see. A wins when the connector runs from the start of the data's life and an operator accepts the gap; C wins when CDC is not deployed.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. The retention period when an entity has no policy</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A default period** (the issue used 30 days) | Works out of the box | Silently deletes data that the law requires to keep (the issue's own 7-year financial example) |
| **B) No default: an entity without a policy is never auto-purged and a startup validation reports it** | Never deletes by accident; fits regulated data | The operator must write a policy per entity |
| **C) A default that must be set explicitly at registration** | One decision per deployment | A single period for all entity types is rarely right |

### Chosen Option: **B — no default, validation reports missing policies** (recommended, pending the maintainer)

### Rationale

Deleting too early is irreversible; keeping too long is visible and fixable. A wins for non-regulated demo data; C wins when every entity of a deployment shares one period.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. What travels to the retention engine — identifiers only</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Entity type name (as data category) and entity id only** | The personal data being erased is not copied into the retention store or the scheduled message store | The eraser re-reads the row |
| **B) The entity image** | The eraser needs no read | Stores the personal data that the erasure is meant to remove, for the whole retention period |

### Chosen Option: **A — identifiers only**

### Rationale

Keeping the full image in another store until the purge defeats data minimisation and storage limitation.

**Class:** Settled
**Settled by:** GDPR Article 5(1)(c) and 5(1)(e); AGENTS.md section 3 (no sensitive data in plaintext storage)

</details>

<details>
<summary><strong>5. Restore before expiry, re-delete and duplicates</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) One retention record per false-to-true transition; the handler deduplicates only an exact redelivery (same entity, data category, tenant and `DeletedAtUtc`); the eraser re-reads the row at execution, purges only if `IsDeleted` is still true, and for a restored row returns a distinct outcome so the record is not marked Deleted; an older record of a re-deleted entity is held back by the module's sibling withholding (`GetSiblingsAsync`: same entity, category, tenant and module)** | A restored row is never purged and never reported as erased; a restore followed by a second delete gets a fresh period from the second deletion | Needs a distinct "not erased, row restored" outcome that `RetentionEnforcementService` does not mark Deleted (it calls `MarkDeletedAsync` after a successful erasure, `RetentionEnforcementService.cs:559`), which may be a module change in its own PR |
| **B) Cancel the tracking on restore (the handler sees `true -> false` and cancels the record)** | The record never lies; no execution-time re-read for the restore case | The module has no cancel operation, so it needs a new one in `Encina.Compliance.Retention`; the scheduler alternative would need a cancel API and a stored message id per entity |
| **C) Deduplicate by entity and data category only (skip tracking when any Active record exists)** | Simplest | A restored then re-deleted row is purged at the first deletion's expiry instead of the second's, before its retention period ends; the eraser's `Right` for a restored row marks the record Deleted although nothing was erased |

### Chosen Option: **A — a record per transition, exact-redelivery dedup, distinct restored outcome** (recommended, pending the maintainer)

### Rationale

Options A and B both satisfy idempotency (AGENTS.md section 6 function 9) and "a `Left` fails the operation" (section 3), so the cited rules do not choose between them; C fails the issue's "restore before expiry cancels the effect" and the GDPR Article 5(2) accountability record, and is excluded. Restore (`RestoreAsync`) clears `IsDeleted`; checking it at the moment of the irreversible action is the safe place, and A keeps the engine unchanged apart from the outcome. Whether the outcome is a new return value or a module cancel operation is for the maintainer; either way the plan's PR 12 carries the module change if needed.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. How the purge reaches the 10 providers</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ISoftDeletePurger` in `Encina.DomainModeling`, implemented per provider package without loading the entity: ADO.NET and Dapper build `DELETE ... WHERE id = @id AND IsDeleted = 1` from `ISoftDeleteEntityMapping`, EF Core uses `ExecuteDeleteAsync` with `IgnoreQueryFilters()`, MongoDB uses `DeleteOneAsync` with a filter on id and the deleted flag** | Same contract on the 10 providers; the repository stays optional; no entity image is loaded | Six SQL implementations plus two others (8 purger implementations) |
| **B) Require the application to implement `IRetentionDataEraser`** | No Encina code per provider | Every application rewrites the same purge; no provider coherence |

### Chosen Option: **A — `ISoftDeletePurger` per provider**

### Rationale

Provider-dependent behavior must exist on all 10 providers with the same contract.

**Class:** Settled
**Settled by:** AGENTS.md section 5 (every provider-dependent feature on all 10 providers); the issue's provider matrix, which lists "Hard delete execution: this issue" on all 10 rows, is what rules out B

</details>

<details>
<summary><strong>7. Tenant scope of the purge</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The purger takes the tenant and filters on it; the eraser denies (returns a `Left` and logs once) when the entity is tenant-scoped and `TenantId` is missing; the tracker deduplicates comparing the tenant; the policy supplies an optional `TenantIdSelector`. The validator learns that an entity is tenant-scoped from an explicit declaration on the policy builder (`WithTenant(selector)` or `NotTenantScoped()`), cross-checked against the tenancy signals of every provider, with the marker `Encina.Tenancy.ITenantEntity` as the universal check: when the entity type implements `ITenantEntity` (the signal EF Core tenancy uses, `EfCoreTenancyOptions.cs:28`), or an `ITenantEntityMapping<TEntity, TId>` exists for it (ADO.NET, Dapper and MongoDB tenancy folders), and the policy declares neither `WithTenant` nor `NotTenantScoped`, startup fails (fail closed); an entity with no signal at all and no declaration is also rejected, so a missing declaration can never pass** | Cannot delete another tenant's row; matches the eraser contract of the Retention module | One more selector in the policy |
| **B) Purge by id alone (the earlier draft)** | Simplest | In a multi-tenant table whose keys are unique per tenant only, it can delete another tenant's row; fails open |

### Chosen Option: **A — tenant-scoped purge, deny when the tenant is missing**

### Rationale

`IRetentionDataEraser` tells implementers to scope every statement to `target.TenantId` or refuse when it is missing, because enforcement runs with no ambient tenant (`IRetentionDataEraser.cs`, example at lines 66-77). Compliance gates fail closed. The tenant reaches the handler from the entity through the policy's `TenantIdSelector`; deferring the broader tenant work (keys, credentials, background cycles) to #1257 does not cover this purge, so the scoping is part of this plan.

**Class:** Settled
**Settled by:** AGENTS.md section 3 ("Compliance and security gates fail closed": the eraser is a gate on an irreversible action, missing tenant means deny); `IRetentionDataEraser` contract; B is the fail-open shape

</details>

<details>
<summary><strong>8. Package placement of the adapter, the eraser and the sweep</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A bridge package, recommended name `Encina.Cdc.SoftDeleteRetention`, referencing `Encina.Cdc`, `Encina.DomainModeling` and `Encina.Compliance.Retention`** | `Encina.Cdc` keeps its dependencies (`Encina`, `Encina.Caching`, `Encina.Messaging`); pay-for-what-you-use; the Marten dependency of the Retention module stays with those who use it | A new package with its own README, manifest and PublicAPI; its log class needs the `Cdc` range mapped to a second assembly in `EncinaEventIdAllocationTests` (checked in PR 3, where the first log class appears: the batch registry notes that an assembly can map to more than one range) |
| **B) Inside `Encina.Compliance.Retention`, referencing `Encina.Cdc`** | No new package | The Retention module would depend on CDC |
| **C) Inside `Encina.Cdc` (the earlier draft)** | No new package | Every CDC user depends on `Encina.Compliance.Retention` and through it on `Encina.Marten`; `where TEntity : ISoftDeletable` also adds `Encina.DomainModeling` |

### Chosen Option: **A — bridge package** (recommended, pending the maintainer)

### Rationale

C breaks pay-for-what-you-use (AGENTS.md section 1) for every CDC user and is excluded; A and B both comply, so the choice between them is the maintainer's. The eraser must be a composite: enforcement resolves a single `IRetentionDataEraser` through `GetService` (`RetentionEnforcementService.cs:137`), so the Encina eraser wraps the application's eraser for the categories it does not own, in either registration order, without overriding it (AGENTS.md section 3, registration completeness); PR 13 proves both orders with a DI test using `ValidateOnBuild` and `ValidateScopes`.

**Class:** Dilemma

</details>

<details>
<summary><strong>9. Expiry when <code>DeletedAtUtc</code> is null</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Fall back to `ChangeContext.Metadata.CapturedAtUtc` in the handler; the sweep uses the date returned by `EnumerateDeletedAsync` and, when that is also null, the sweep time from `TimeProvider`** | No new clock read in the handler (the value is already on the event); one rule for the handler | `CapturedAtUtc` is not the source commit time: every connector sets it from `_timeProvider.GetUtcNow()` at read time (`SqlServerCdcConnector.cs:259-261`, `PostgresCdcConnector.cs:177-179`, `MySqlCdcConnector.cs:184-186`, `MongoCdcConnector.cs:237-239`, `DebeziumEventMapper.cs:148-150`), so after an outage or a replay the period starts at the replay time and the real deletion date is lost |
| **B) `TimeProvider` now at handling time** | Simple; same drift as A (the processing clock) with no event dependency | Same drift after a replay; equal in effect to A, only the clock read moves into the handler |
| **C) Use the source commit timestamp, which needs each connector to carry it on `ChangeMetadata` (a connector change in the five connector packages)** | The period starts at the true deletion; no drift on replay | A change in five connectors and the metadata contract, outside this plan's files; where the source offers no commit time the fallback to A still applies |
| **D) Return a `Left` and do not track** | Never guesses | An entity without a `DeletedAtUtc` value is never purged, which is a silent storage-limitation gap |

### Chosen Option: **A — processing-time fallback, then sweep time** (recommended, pending the maintainer)

### Rationale

A and B are equivalent in effect (both use the processing clock), so the real trade-off is between accepting processing-time drift (A, B), paying for a connector change to remove it (C), or never purging such rows (D). A is recommended because it needs no connector change and drifts toward keeping data longer, which is visible and fixable, whereas an early purge is irreversible. `ISoftDeletable.DeletedAtUtc` is `DateTime?`, and the ADO.NET and Dapper mapping has `DeletedAtColumnName` as `string?`, so a soft-deletable entity may not have the column at all. A `DateTime` of `Kind.Unspecified` after deserialization is treated as UTC (decision T3). `EnumerateDeletedAsync` therefore returns items with the identifier, the tenant and a nullable `DeletedAtUtc`, not bare ids.

**Class:** Dilemma

</details>

<details>
<summary><strong>10. Atomicity of the purge and its audit entry</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The purge and the audit entry in one transaction where the audit store shares the database** | The issue's row ("hard delete executes in a transaction with its audit record") | The audit store is often another database or store (a Marten event store for the record, an `IOperationAuditStore` of another provider); MongoDB without a replica set has no transaction; not uniform on the 10 providers |
| **B) Purge first, then the audit entry; if the audit write fails the eraser returns a `Left` so enforcement retries; on retry the purger reports `AlreadyGone` and the audit entry is written with that outcome, keyed by the retention record id so a retry never writes twice** | Uniform on the 10 providers; the failure is visible and retried | A window where the row is gone and the audit entry is not yet written |
| **C) Audit best effort (the earlier draft: written after the purge, only when an audit store is registered)** | Simplest | An audit failure is swallowed; breaks AGENTS.md section 3 |

### Chosen Option: **B — purge, then audit with retry on failure** (recommended, pending the maintainer)

### Rationale

C is excluded by the rule that errors in background infrastructure are never swallowed. A or B can both satisfy it; B is recommended because A cannot be uniform. Under Design Choice 1 option B the audit source is the event-sourced retention record plus the operation audit entry; under Design Choice 1 option A there is no retention record, so the startup validator requires an `IOperationAuditStore` and fails if it is missing, otherwise the acceptance criterion "deletion requested and executed are audited" would depend on an optional store.

**Class:** Dilemma

</details>

<details>
<summary><strong>11. The period stored on the retention record</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Pass the remaining period (`expiresAtUtc - now`, clamped at zero) to `TrackEntityAsync`** | No module change | `IRetentionRecordService.TrackEntityAsync` documents `retentionPeriod` as "copied from the policy" (`IRetentionRecordService.cs:57`) and stores it, so the record carries a shorter period than the policy; a clamped zero expires the record at once and its erasure runs on the next enforcement cycle |
| **B) Add an expiry-based overload to the module that takes `expiresAtUtc` and keeps the policy period on the record** | The audit record states the real period | A change in `Encina.Compliance.Retention`, in its own PR |

### Chosen Option: **A — remaining period, documented** (recommended, pending the maintainer)

### Rationale

A needs no module change and is correct for erasure timing; the cost is an audit record with a shorter period, which the docs state. B is cleaner for accountability and is for the maintainer to prefer.

**Class:** Dilemma

</details>

### Smaller decisions (Trivial)

| Id | Decision | Class | Basis |
|----|----------|-------|-------|
| T1 | Purgers and the tracker are registered scoped (`TryAddScoped`), never singleton | Trivial | `IDbConnection` is scoped (`Encina.ADO.SqlServer/ServiceCollectionExtensions.cs:299`) and `SoftDeletePurgerEF<TDbContext>` needs a scoped `DbContext`; a singleton would fail `ValidateScopes` |
| T2 | Policy table name: the policy builder takes the table (`ForTable`), required; `WithSoftDeleteRetention` adds the `TableMapping` from it (`AddPolicy<TEntity, TId>` carries no table). The startup report of soft-deletable entities without a policy reads the registered `ISoftDeleteEntityMapping` implementations (ADO.NET, Dapper), the `ISoftDeletable` entity types of the EF Core model and the soft-deletable Mongo repositories; best effort per provider, a log and no failure | Trivial | Report only; reversible |
| T3 | A `DateTime` with `Kind.Unspecified` read from CDC or the database is treated as UTC (`DateTime.SpecifyKind`) | Trivial | The `AtUtc` suffix convention (AGENTS.md section 4) |
| T4 | SQL identifiers come from `ISoftDeleteEntityMapping` only and are quoted per database (`[x]`, `"x"`, backticks) and validated with the existing `SqlIdentifierValidator`, which the specification builder already uses for the table name (`SoftDeleteSpecificationSqlBuilder.cs:135`); the purger reuses it and adds no new pattern; the column names are already validated when the mapping is built (`SoftDeleteEntityMappingBuilder.cs:81-230` validates each column with `SqlIdentifierValidator`), so validating again in the purger is defense in depth, not a gap; identifiers never come from user input | Trivial | Reuse of the existing validator |
| T5 | The eraser logs a restored row at Information, once per record | Trivial | Operational detail |
| T6 | Error codes `soft_delete_tracking_failed`, `policy_missing`, `purge_failed`, `restored` each declare their category through the merged error category contract of ADR 062 (`EncinaErrors.Create(code, ErrorCategory.X, ...)`, PR #3599; configuration lookups are Internal); a `GetCategory` test covers them | Trivial | Convention of the error code classes |
| T7 | Surrogate keys: the entity id is copied into immutable Marten retention events and stays after the purge; the docs state that soft-delete retention requires surrogate keys (a natural personal identifier such as an email or a national id must not be the key). A keyed hash of the id is a possible later option | Trivial | GDPR Article 5(1)(c); documented consequence |
| T8 | A concurrent handler/sweep race can create two records for one occurrence; the module's sibling settlement erases both together and a test proves it (matrix row 7) | Trivial | `RetentionEnforcementService` sibling handling (`GetSiblingsAsync`) |

---

## Implementation Phases

### Phase 1: Models, options and the policy

> **Goal**: pure types, no I/O.

<details>
<summary><strong>Tasks</strong></summary>

1. `SoftDeleteRetentionPolicy.cs` (in the package chosen by Design Choice 8; recommended `Encina.Cdc.SoftDeleteRetention`): `EntityType`, `TableName` (decision T2), `DataCategory` (default entity type name), `RetentionPeriod`, `EntityIdSelector` (`Func<object, string>`), optional `TenantIdSelector` (Design Choice 7); `CryptoShredOnExpiry` is not added.
2. `SoftDeleteRetentionOptions`: `AddPolicy<TEntity, TId>(Func<TEntity, TId> idSelector, Action<SoftDeleteRetentionPolicyBuilder> configure)` (the builder has `ForTable`, `RetainFor`, `WithTenant`, `NotTenantScoped`), `SweepInterval` (24 hours), `EnableSweep` (true); no default period (Design Choice 3).
3. `IValidateOptions` rejecting non-positive periods, policies with no id selector or no table, and tenant-scoped entities with no tenant selector; a startup report (log, no failure) of soft-deletable entities with no policy, sourced as in decision T2; under Design Choice 1 option A and Design Choice 10, a missing `IOperationAuditStore` fails startup.
4. `SoftDeleteRetentionErrors` (`encina.cdc.soft_delete_tracking_failed`, `...policy_missing`, `...purge_failed`, `...restored`), each with its category from the merged error category contract of ADR 062 (decision T6) and a `GetCategory` test.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT: ISoftDeletable has no id; the Retention module already has policies. No period is assumed when none is configured.
TASK: Add SoftDeleteRetentionPolicy, its builder, SoftDeleteRetentionOptions with a validator and the error codes.
KEY RULES: no default retention period; AtUtc suffix; Either returns; XML docs with examples; nothing here names a
private application.
REFERENCE FILES: src/Encina.Compliance.Retention/RetentionOptions.cs, src/Encina.DomainModeling/IAuditable.cs, src/Encina.Cdc/Messaging/CdcMessagingOptions.cs.
```

</details>

### Phase 2: The CDC handler

> **Goal**: detect a soft delete and hand it to the tracker.

<details>
<summary><strong>Tasks</strong></summary>

1. `SoftDeleteRetentionCdcHandler<TEntity> where TEntity : class, ISoftDeletable`: `HandleUpdateAsync` acts when `after.IsDeleted && (before is null || !before.IsDeleted)`; it computes `expiresAtUtc = DeletedAtUtc + period`, where `DeletedAtUtc` is `after.DeletedAtUtc` or, when null, the change timestamp `CapturedAtUtc` (Design Choice 9; an `Unspecified` kind is read as UTC, T3), and calls `ISoftDeleteRetentionTracker.TrackAsync(...)`; `HandleInsertAsync` acts when a row arrives already deleted (a snapshot); `HandleDeleteAsync` (a real delete) returns `Right(unit)` and logs once per entity type ("skipped", reason real delete).
2. `ISoftDeleteRetentionTracker` (internal): `TrackAsync(SoftDeleteObservation, CancellationToken)`; `SoftDeleteObservation` holds entity type, entity id, `DeletedAtUtc`, `TenantId` (from the policy's `TenantIdSelector`).
3. A `Left` from the tracker is returned unchanged.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT: SQL Server updates have no before image; the dispatcher prerequisite lets the handler see after only.
TASK: Implement the handler and the tracker abstraction.
KEY RULES: never swallow a Left; only identifiers cross into the tracker; TimeProvider for any clock; the handler
is scoped; log ids and types only.
REFERENCE FILES: src/Encina.Cdc/Messaging/OutboxCdcHandler.cs, src/Encina.Cdc/Processing/CdcDispatcher.cs, src/Encina.DomainModeling/IAuditable.cs.
```

</details>

### Phase 3: The engine adapter (recommended: Retention module)

> **Goal**: tracking arranges the later purge.

<details>
<summary><strong>Tasks</strong></summary>

1. `RetentionModuleSoftDeleteTracker : ISoftDeleteRetentionTracker`: looks up `IRetentionRecordService.GetRecordsByEntityAsync(entityId)` and treats only an exact redelivery as a duplicate (same entity, data category, tenant and `DeletedAtUtc`, Design Choice 5), resolves the policy id through `IRetentionPolicyService.GetPolicyByCategoryAsync(dataCategory)` (creating it from `SoftDeleteRetentionPolicy` at startup through `RetentionOptions.AddPolicy`), then `TrackEntityAsync(entityId, dataCategory, policyId, max(expiresAtUtc - now, 0), tenantId)` (the remaining period, Design Choice 11).
2. `SoftDeleteRetentionEraser : IRetentionDataEraser`: for a target whose data category is a registered soft-delete policy, denies when a tenant-scoped entity has no `TenantId` (Design Choice 7), resolves `ISoftDeletePurger`, purges only if the row is still deleted and for a restored row returns the distinct outcome of Design Choice 5; after a purge it writes the audit entry and returns a `Left` when that write fails (Design Choice 10); other categories fall through to the application's own eraser through a composite that works in either registration order (Design Choice 8).
3. `SoftDeleteRetentionSweepService : BackgroundService` (`PeriodicTimer` through `TimeProvider`, catch per iteration, an `OperationCanceledException` from the stopping token ends the loop): asks `ISoftDeletePurger.EnumerateDeletedAsync` for deleted rows (identifier, tenant, nullable `DeletedAtUtc`) with no record and tracks them.
4. If Design Choice 1 picks scheduling instead: `SchedulerSoftDeleteRetentionTracker` and `HardDeleteCommand(EntityType, EntityId)` with its handler, plus an optional `ILegalHoldService` check.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT: IRetentionRecordService.TrackEntityAsync starts the clock at tracking; RetentionEnforcementService calls
IRetentionDataEraser for expired, unheld records.
TASK: Implement the tracker adapter, the eraser and the sweep service for the chosen engine.
KEY RULES: the loop never lets an exception escape and handles a Left like any background Left (fail the
operation visibly, count it, keep running); identifiers only; TimeProvider; error codes in logs.
REFERENCE FILES: src/Encina.Compliance.Retention/RetentionEnforcementService.cs, src/Encina.Compliance.Retention/Abstractions/IRetentionRecordService.cs,
src/Encina.Compliance.Retention/Abstractions/IRetentionDataEraser.cs, src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs.
```

</details>

### Phase 4: Purge executor, ADO.NET and Dapper (6 packages)

> **Goal**: `ISoftDeletePurger` on SQL Server, PostgreSQL and MySQL for both families.

<details>
<summary><strong>Tasks</strong></summary>

1. `ISoftDeletePurger` in `Encina.DomainModeling` (PR 1): `PurgeAsync<TEntity, TId>(TId id, string? tenantId, CancellationToken)` returning an outcome (`Purged`, `AlreadyGone`, `Restored`; the delete filters on id, tenant when given, and the deleted flag, and is idempotent when the row is gone) and `EnumerateDeletedAsync<TEntity>(...)` (a page of items with identifier, tenant and nullable `DeletedAtUtc`, Design Choice 9).
2. `SoftDeletePurgerADO` and `SoftDeletePurgerDapper` per database package, built from `ISoftDeleteEntityMapping<TEntity, TId>`: parameterized `DELETE ... WHERE <key> = @id AND <IsDeleted> = <true literal for the database>` (`bit`, `true`, `1`); identifiers quoted per database and validated (decision T4).
3. Registration `AddEncinaSoftDeletePurger*` per package with `TryAddScoped` (decision T1).
4. Integration tests on the shared `[Collection]` fixtures of the six families.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT: The ADO.NET and Dapper packages have ISoftDeleteEntityMapping but no hard delete.
TASK: Add ISoftDeletePurger and the six SQL implementations with registration and integration tests.
KEY RULES: async only with the CancellationToken; parameters only, identifiers come from the mapping never from
user input; shared [Collection] fixtures; error codes, not provider messages.
REFERENCE FILES: src/Encina.ADO.SqlServer/SoftDelete/ISoftDeleteEntityMapping.cs, src/Encina.Dapper.PostgreSQL/SoftDelete/SoftDeleteSpecificationSqlBuilder.cs.
```

</details>

### Phase 5: Purge executor, EF Core and MongoDB

> **Goal**: the remaining four providers.

<details>
<summary><strong>Tasks</strong></summary>

1. EF Core: `SoftDeletePurgerEF<TDbContext>` using `IgnoreQueryFilters()` and `ExecuteDeleteAsync(ct)` on rows with `IsDeleted` true (one package, three databases).
2. MongoDB: `SoftDeletePurgerMongoDB` using `DeleteOneAsync` with a filter on id and `isDeleted`.
3. Registration and integration tests on `EFCore-*` and the MongoDB fixtures.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT: EF Core has ISoftDeleteRepository.HardDeleteAsync, which loads the entity first; MongoDB's soft delete is
inside its IFunctionalRepository implementation.
TASK: Implement ISoftDeletePurger for both, without loading the entity image, scoped to the tenant when given.
KEY RULES: no synchronous calls; the filter always includes the deleted flag; idempotent when the row is gone.
REFERENCE FILES: src/Encina.EntityFrameworkCore/SoftDelete/SoftDeleteRepositoryEF.cs, src/Encina.MongoDB/SoftDelete/SoftDeletableFunctionalRepositoryMongoDB.cs.
```

</details>

### Phase 6: Configuration, DI and validation

> **Goal**: `WithSoftDeleteRetention` registers everything and fails loudly when misconfigured.

<details>
<summary><strong>Tasks</strong></summary>

1. `WithSoftDeleteRetention(Action<SoftDeleteRetentionOptions>)` (an instance method on `CdcConfiguration` if Design Choice 8 picks `Encina.Cdc`, an extension method on it in the bridge package otherwise): adds a `TableMapping` from the policy's `TableName` and a `HandlerRegistration` per policy; sets `CdcOptions.UseSoftDeleteRetention` only in the first case.
2. `ServiceCollectionExtensions`: private `RegisterSoftDeleteRetentionServices` (options, validator, tracker, eraser, sweep, purger check).
3. Startup validator: a policy for a table that already has another handler is rejected until the dispatcher allows several; with the Retention module chosen, `IRetentionRecordService` must be registered.
4. DI test with `ValidateOnBuild` and `ValidateScopes`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT: Three CDC plans edit CdcConfiguration, CdcOptions and ServiceCollectionExtensions; keep each edit isolated.
TASK: Add WithSoftDeleteRetention, the registration method, the validator and the DI test.
KEY RULES: every option type and dependency resolved is registered; disabled by default; no [Obsolete].
REFERENCE FILES: src/Encina.Cdc/CdcConfiguration.cs, src/Encina.Cdc/ServiceCollectionExtensions.cs.
```

</details>

### Phase 7: Observability and audit

> **Goal**: tracking, purges and failures are visible and auditable.

<details>
<summary><strong>Tasks</strong></summary>

1. `SoftDeleteRetentionMetrics` (meter `Encina.Cdc.SoftDeleteRetention`): counters `encina.cdc.retention.tracked`, `encina.cdc.retention.purged`, `encina.cdc.retention.errors` (tag `entity.type`).
2. Activity source spans around tracking and purging.
3. `SoftDeleteRetentionLog` (EventIds 4870-4875, six ids packed sequentially; created in PR 3): 4870 tracked, 4871 skipped (reason: exact redelivery, restored before expiry, real delete observed), 4872 purged, 4873 operation failed (tracking or purge; the error code names which), 4874 missing policy, 4875 sweep failed; error code only.
4. Audit (Design Choice 10, recommended option B): the module's event-sourced retention record is the audit of the request and the execution; the eraser also writes an operation audit entry through `IOperationAuditStore` after the purge, keyed by the retention record id; a failed audit write makes the eraser return a `Left` so enforcement retries, and the retry writes the entry with outcome `AlreadyGone`. Under Design Choice 1 option A the audit store is required at startup.
5. `Encina.OpenTelemetry` subscription.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
CONTEXT: GDPR accountability (Article 5(2)) needs evidence of when deletion was requested and when it ran.
TASK: Add metrics, spans, logs, the audit entry and the OpenTelemetry subscription.
KEY RULES: EventIds 4870-4875 only; tags carry entity type names and ids, never personal data; ids only in logs.
REFERENCE FILES: src/Encina.Cdc/Caching/Diagnostics/CacheInvalidationMetrics.cs, src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs.
```

</details>

### Phase 8: Testing

> **Goal**: each applicable flag reaches its manifest target.

<details>
<summary><strong>Tasks</strong></summary>

1. Unit: handler (false to true, already deleted, restore, no before image, real delete, `DeletedAtUtc` null and `Kind.Unspecified`), policy resolution, missing policy, tracker deduplication (exact redelivery skipped; restore then re-delete tracked as a new record), eraser re-read and the restored outcome (the record is not marked Deleted), tenant missing denies, audit-write failure returns a `Left` and the retry writes once, sweep loop (a failing iteration never ends it), handler/sweep race settled by the module's siblings.
2. Guard: all public methods.
3. Contract: `ISoftDeletePurger` on all 10 providers.
4. Property: expiry arithmetic never goes negative and is monotonic in `DeletedAtUtc`.
5. Integration: soft delete, capture and purge on the 10 providers' shared fixtures; the Retention module path on PostgreSQL with Marten through Testcontainers.
6. Load and benchmark: justification files (one row per soft delete).
7. Per-file targets (below) are entered in the manifests by the PR that creates each file.

#### Provider matrix

Capture prerequisites per connector (they decide whether the handler sees a before image) and the purger per database:

| # | Family | Database | CDC capture and before image | Purger |
|---|--------|----------|------------------------------|--------|
| 1 | ADO.NET | SQL Server | Change Tracking (`CHANGETABLE` polling); never a before image, the handler works from the after image (dispatcher prerequisite) | `SoftDeletePurgerADO` (`bit`) |
| 2 | ADO.NET | PostgreSQL | Logical decoding; `REPLICA IDENTITY FULL` required for before values (`PostgresCdcConnector.cs:24`) | `SoftDeletePurgerADO` (`true`) |
| 3 | ADO.NET | MySQL | Binlog; full row image required | `SoftDeletePurgerADO` (`1`) |
| 4 | Dapper | SQL Server | as row 1 | `SoftDeletePurgerDapper` |
| 5 | Dapper | PostgreSQL | as row 2 | `SoftDeletePurgerDapper` |
| 6 | Dapper | MySQL | as row 3 | `SoftDeletePurgerDapper` |
| 7 | EF Core | SQL Server | as row 1 | `SoftDeletePurgerEF` (`IgnoreQueryFilters`, `ExecuteDeleteAsync`) |
| 8 | EF Core | PostgreSQL | as row 2 | `SoftDeletePurgerEF` |
| 9 | EF Core | MySQL | as row 3 | `SoftDeletePurgerEF` |
| 10 | MongoDB | MongoDB | Change streams; the before image needs pre-images enabled (`MongoCdcConnector.cs:244`) and the after image defaults to `UpdateLookup`, which reads the current document, not the one at change time | `SoftDeletePurgerMongoDB` (`DeleteOneAsync`) |

Each row has contract, integration and (for the purger) unit and guard coverage per the PR table. Other applicability-matrix rows (caches, transports, locks, validation providers) are not applicable.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
CONTEXT: Coverage is per flag (AGENTS.md section 9). Event-sourced compliance modules are tested against Marten on
PostgreSQL through Testcontainers, with no in-memory store.
TASK: Write the tests above and compare each flag with the manifest targets.
KEY RULES: real code under test; Shouldly via Encina.Testing.Shouldly; shared [Collection] fixtures; FakeTimeProvider;
no Thread.Sleep.
REFERENCE FILES: .github/coverage-manifest/Encina.Cdc.json, tests/Encina.IntegrationTests, docs/testing/integration-tests.md.
```

</details>

### Phase 9: Documentation and finalization

> **Goal**: users can configure retention for soft-deleted data.

<details>
<summary><strong>Tasks</strong></summary>

1. XML docs on all new public APIs.
2. `changelog.d/635-soft-delete-retention-cdc.added.md`.
3. `src/Encina.Cdc/README.md`, `docs/features/cdc.md`, the Retention module page (the soft-delete path, legal holds, the sweep), and a short note that Spanish law keeps blocked data available to authorities during the liability period (LOPDGDD art. 32), which is what the retention period expresses.
4. ADR number assigned when the ADR pull request opens (retention enforcement and the engine choice); the number is taken from the batch registry; 062 is already taken by the merged error category contract.
4a. The feature page states the consequences for operators: surrogate keys are required (decision T7), a tenant-scoped entity without a tenant selector fails startup, and the per-connector before-image table.
5. `PublicAPI.Unshipped.txt` lines; `docs/INVENTORY.md`.
6. Verification: build with zero warnings, tests, per-flag coverage.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
CONTEXT: The feature is code-complete after Phase 8.
TASK: Documentation, changelog fragment, PublicAPI lines, verification.
KEY RULES: never type coverage figures by hand; English only; compliance texts cover Spain and the EU only; follow the
encina-docs skill; never edit CHANGELOG.md.
REFERENCE FILES: .claude/skills/encina-docs/SKILL.md, changelog.d/README.md, docs/features/cdc.md.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | Relevance |
|--------|-----------|
| GDPR Article 5(1)(c) and 5(1)(e) | Data minimisation and storage limitation: identifiers only; purge after the period |
| GDPR Article 5(2) | Accountability: evidence of when deletion was requested and executed |
| GDPR Article 17 and 17(3) | Erasure and its exceptions (legal obligation, legal claims): legal holds block the purge |
| Spanish organic law 3/2018 (LOPDGDD), art. 32 | Blocking of data: data is kept unavailable but retained for authorities during the liability period, then destroyed |
| AGENTS.md section 3 | A `Left` fails the operation; loops catch per iteration; messaging scheduling carries domain messages |
| SPEC-002 | Regulatory readiness; the health-data reference application needs legal holds |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|----------------------|
| `ISoftDeletable`, `ISoftDeleteRepository` | `src/Encina.DomainModeling` | Soft delete contract, restore and hard delete |
| `SoftDeleteInterceptor` | `src/Encina.EntityFrameworkCore/SoftDelete` | Produces the update that CDC captures |
| `ISoftDeleteEntityMapping` | ADO.NET and Dapper `SoftDelete/` folders | Column names for the purge SQL |
| `IRetentionRecordService`, `ILegalHoldService`, `IRetentionDataEraser`, `RetentionEnforcementService` | `src/Encina.Compliance.Retention` | Engine (recommended option) |
| `SchedulerOrchestrator` | `src/Encina.Messaging/Scheduling` | Engine (alternative option) |
| `CdcConfiguration`, `CdcDispatcher`, `IChangeEventHandler<T>` | `src/Encina.Cdc` | Capture and fluent registration |
| `IOperationAuditStore` | `src/Encina.Security.Audit` | Audit of the purge |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Cdc` (existing range 4800-4899) | 4870-4875 | `SoftDeleteRetentionLog`: six messages, packed sequentially (the phase lists them); 4876-4879 are not claimed. The CDC family block in the batch registry is 4854-4891: 4854-4859 belong to #2393, 4860-4863 to #634, 4880-4885 to #636. If Design Choice 8 picks the bridge package, the allocation test maps that assembly to the `Cdc` range as well (checked in PR 3) |

### Per-file coverage targets (AGENTS.md section 9)

Per flag: U unit, G guard, C contract, P property, I integration; entered in the package manifests, never lowered afterwards. The U, G, C and P targets of a file are entered by the PR that creates (or edits) it together with the tests that meet them; an I target is entered by the PR that adds the integration test that meets it: the purger rows in their own provider PRs (4-11), the handler, tracker and eraser integration targets in PR 17 (relational) and PR 18 (Marten and MongoDB). No PR therefore reports a target it cannot reach.

| File (new) | U | G | C | P | I | Justification |
|------------|---|---|---|---|---|---------------|
| `SoftDeleteRetentionPolicy`, builder, options, validator | 90 | 90 | 0 | 0 | 0 | Pure validation and configuration, all branches reachable |
| `SoftDeleteRetentionErrors` | 90 | 0 | 0 | 0 | 0 | Factories plus the category test |
| `SoftDeleteRetentionCdcHandler.cs` | 90 | 90 | 0 | 80 (expiry arithmetic) | 70 | Each operation and the null-date branches with fakes; connector-driven integration |
| `RetentionModuleSoftDeleteTracker.cs` | 85 | 90 | 0 | 0 | 80 | Dedup, restore/re-delete and policy resolution with fakes; the Marten path on PostgreSQL |
| `SoftDeleteRetentionEraser.cs` | 90 | 90 | 0 | 0 | 80 | Tenant deny, restored outcome, audit failure and retry, composite fall-through |
| `SoftDeleteRetentionSweepService.cs` | 85 | 90 | 0 | 0 | 0 | Loop behavior with a fake timer and purger |
| `ISoftDeletePurger` (contract base) and the 8 purgers | 70 | 90 | 85 | 0 | 90 | SQL and filters only exercised against real databases; contract base is shared by the 10 provider rows |
| Metrics, activity source, log | 85 | 0 | 0 | 0 | 0 | Meter listener, generated logger |
| Registration, `WithSoftDeleteRetention`, startup validator | 90 | 90 | 0 | 0 | 0 | DI proof builds the provider in both registration orders |
| `RetentionEnforcementService.cs` (edited in PR 12, whole file) | 90 | 0 | 0 | 0 | 0 | Whole-file target is the current manifest target of the file or this value if higher; the restored-outcome branch is unit-testable with fakes |
| The eraser result type (edited in PR 12) | 90 | 90 | 0 | 0 | 0 | Small type; new outcome covered by the same unit tests |
| The 8 provider `ServiceCollectionExtensions.cs` files (edited in PRs 4-11, one row each in the manifests) | 90 | 0 | 0 | 0 | 80 | Whole-file target is the current manifest target or this value if higher; the purger registration is reached by the DI proof and the integration test of the same PR |

CRAP stays at or below 10 on every changed method: the handler update path is one condition, the null-date fallback is a small helper.

### Estimated File Count

The per-PR counts are in "Split into pull requests" (158 files); this table groups the same file touches (a file edited in two PRs counts in each) and sums to that total.

| Category | Files | Notes |
|----------|-------|-------|
| Production under `src/`, new or edited | 39 | PR 1: purger interface, result type, item type (3); PR 2: policy, builder, options, validator, errors (5); PR 3: tracker interface, observation, handler, log (4); PRs 4-11: purger and DI extension per package (16); PR 12: `RetentionEnforcementService`, eraser result type (2); PR 13: tracker adapter, eraser (2); PR 14: sweep (1); PR 15: `WithSoftDeleteRetention` and the startup validator (2); PR 16: metrics, activity source, OpenTelemetry subscription, eraser audit edit (4) |
| Project and solution | 2 | bridge project file and `Encina.slnx` entry (PR 2) |
| PublicAPI and manifests | 32 | PR 1: 2; PR 2: 3; PR 3: 2; PRs 4-9: 12; PRs 10-13: 8; PR 14: 1; PRs 15-16: 4 |
| Tests | 76 | contract base (1); PR 2: 3; PR 3: 5 (handler unit, guard, log test, handler property test, allocation test edit); PRs 4-9: 24; PR 10: 8; PR 11: 4; PR 12: 3; PR 13: 4; PR 14: 5; PR 15: 4; PR 16: 2; PR 17: 10; PR 18: 3 |
| Justification `.md` | 2 | load and benchmark (PR 18) |
| Docs, changelog | 7 | fragment, bridge README, `Encina.Cdc` README, feature page, Retention page, inventory, ADR (PR 19) |

Total 39 + 2 + 32 + 76 + 2 + 7 = 158.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 / C# 14 library with nullable reference types. You are implementing
issue #635: soft delete retention enforcement via CDC. Read AGENTS.md first.

IMPLEMENTATION OVERVIEW: Phase 1 adds SoftDeleteRetentionPolicy and options (no default period). Phase 2 adds
SoftDeleteRetentionCdcHandler<TEntity> and ISoftDeleteRetentionTracker. Phase 3 adds the engine adapter (the Retention
module by default), the eraser that re-reads the row, and the daily sweep. Phases 4-5 add ISoftDeletePurger on the 10
providers. Phase 6 adds WithSoftDeleteRetention and validation. Phase 7 adds metrics, spans, logs (EventIds
4870-4875) and the audit entry. Phase 8 tests, Phase 9 documents. Deliver as the 19 pull requests of the Summary
after the CDC prerequisites land.

KEY PATTERNS: identifiers only reach the retention engine; a Left is never swallowed; legal holds are honored; the
loop catches per iteration; opt-in; TimeProvider; ids and types in logs and tags, never personal data; registration
completeness proven by a DI test; all 10 providers for the purge.

REFERENCE FILES: src/Encina.Cdc/CdcConfiguration.cs, src/Encina.Cdc/Processing/CdcDispatcher.cs,
src/Encina.Compliance.Retention/RetentionEnforcementService.cs, src/Encina.DomainModeling/Repository.cs,
src/Encina.ADO.SqlServer/SoftDelete/ISoftDeleteEntityMapping.cs, .github/coverage-manifest/Encina.Cdc.json.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: policy lookup is a dictionary built at startup |
| 2 | OpenTelemetry | ✅ | Phase 7: tracked, purged and error counters, spans |
| 3 | Structured Logging | ✅ | `SoftDeleteRetentionLog`, EventIds 4870-4875 |
| 4 | Health Checks | ⏭️ | An overdue-purge indicator is the processor liveness work of #2396; the connector health is the existing `CdcHealthCheck` |
| 5 | Validation | ✅ | Options validated at startup; missing policy report; event payload validation is #730 |
| 6 | Resilience | ✅ | CDC retry and dead letter after #2343 and #2344; the Retention module retries a failed erasure on the next cycle |
| 7 | Distributed Locks | ⏭️ | Defer: the sweep and the handler may run on every instance and race, creating two records for one occurrence; the module's sibling settlement erases both together (decision T8, proven by a test). #1251 is row claiming in the outbox and scheduler processors, not this sweep; a single CDC consumer per source is a follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/cdc-single-consumer-leader-election.md) |
| 8 | Transactions | ⏭️ | The purge is one atomic statement; the audit entry is written after it and a failed audit write is retried (Design Choice 10, a Dilemma: same transaction where the provider allows is the alternative). The issue's row says the hard delete executes in a transaction with its audit record; this plan departs from it unless the maintainer picks option A |
| 9 | Idempotency | ✅ | An exact redelivery finds the existing record (Design Choice 5); a purge of a gone row succeeds as `AlreadyGone` |
| 10 | Multi-Tenancy | ✅ | The purge is tenant-scoped and denies when the tenant is missing (Design Choice 7); per-tenant retention periods and the wider tenant work follow #1257 |
| 11 | Module Isolation | ❌ | Not applicable: infrastructure-level |
| 12 | Audit Trail | ✅ | The retention record and the operation audit entry (Phase 7) |

---

## Prerequisites & Dependencies

| Item | State | Effect |
|------|-------|--------|
| #2343 (a `Left` from the dispatcher is neither retried nor dead-lettered) | open | Required for "a scheduling or tracking failure fails the CDC operation" |
| Dispatcher issue (updates without a before image; several handlers per table) | follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/cdc-dispatcher-update-without-before-image.md) | SQL Server updates have no before image; a soft-deletable entity usually has other handlers |
| Follow-up for entity crypto-shredding | follow-up issue to be opened (draft: artifacts/decisions/plan-batch/followups/entity-crypto-shredding-soft-delete.md) | Replaces `CryptoShredOnExpiry`, which has no key store to call |
| `Encina.Compliance.Retention` and Marten (if Design Choice 1 picks the module) | exists | PostgreSQL event store required |
| #1250 (column encryption keys) | open | Candidate key source for the crypto-shredding follow-up |

---

## Next Steps

1. Review and approve this plan; the maintainer answers the Dilemmas (choices 1, 2, 3, 5, 8, 9, 10 and 11) when work on the issue starts; none is decided here.
2. Publish the plan link as a comment on #635.
3. Begin PR 1 (the purger contract) after the dispatcher prerequisites land.
4. Each phase is a self-contained commit; the final commit references `Fixes #635`.
