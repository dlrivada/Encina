# Implementation Plan: Soft Delete Retention Enforcement via CDC

> **Issue**: [#635](https://github.com/dlrivada/Encina/issues/635) (`area-cdc`, `priority-low`, `p1-recommended`)
> **Type**: Feature
> **Complexity**: High (a CDC handler, a retention engine decision, a purge executor on the 10 providers, a follow-up for crypto-shredding)
> **Estimated Scope**: ~1,400-1,900 lines of production code + ~2,500-3,200 lines of tests, delivered in four pull requests
> **Related plans of the same family (#633, #634, #636, #2393)**: independent of #633 and #2393. #634 and #636 add other CDC handlers through the same `CdcConfiguration` seam; #636 also decides how a CDC consumer excludes infrastructure tables. Sibling plans are referred to by issue number only.

---

## Summary

When a soft delete is captured by CDC, the permanent (hard) delete of the row is arranged for the end of a configured retention period, so personal data is not kept longer than needed. Nothing enforces a retention period for soft-deleted rows today.

### What the code does today (2026-11-11, `main` at `942c0f124`)

1. **Soft delete exists, retention enforcement for it does not.** `ISoftDeletable` has `IsDeleted`, `DeletedAtUtc`, `DeletedBy` (read-only view) and `ISoftDeletableEntity` adds the mutable setters (`src/Encina.DomainModeling/IAuditable.cs`); neither interface exposes an entity id. For EF Core `SoftDeleteInterceptor` turns a delete into an update (`src/Encina.EntityFrameworkCore/SoftDelete/SoftDeleteInterceptor.cs`, `TimeProvider` injected). `ISoftDeleteRepository<TEntity, TId>` (`src/Encina.DomainModeling/Repository.cs:272`) declares `RestoreAsync` and `HardDeleteAsync`; implementations exist only for EF Core (`SoftDeleteRepositoryEF`) and MongoDB (`SoftDeletableFunctionalRepositoryMongoDB`). The ADO.NET and Dapper packages have `ISoftDeleteEntityMapping<TEntity, TId>` (column and property names, `GetIsDeleted`, `SetDeletedAtUtc`) and a specification SQL builder, but no hard delete.
2. **A retention module already exists, and the issue does not mention it.** `Encina.Compliance.Retention` tracks retention per entity and data category (`IRetentionRecordService.TrackEntityAsync(entityId, dataCategory, policyId, retentionPeriod, tenantId, moduleId)`), honors legal holds (`ILegalHoldService.HasActiveHoldsAsync`), marks records expired in `RetentionEnforcementService`, delegates the erasure to an application-supplied `IRetentionDataEraser.EraseAsync(RetentionErasureTarget)` and publishes notifications. Its aggregates are event-sourced on Marten (`RetentionMartenExtensions`). Scheduling a hard delete outside it would ignore legal holds.
3. **Scheduling carries domain messages.** `SchedulerOrchestrator.ScheduleAsync<TRequest>(request, executeAt, ct)` (`src/Encina.Messaging/Scheduling/SchedulerOrchestrator.cs`) persists the request in the scheduled message store; AGENTS.md section 3 says Encina scheduling carries domain messages.
4. **The handler contract and its limits.** `IChangeEventHandler<TEntity>` is routed one handler per entity type (`CdcDispatcher.cs:61-70`, `ToFrozenDictionary` keyed by entity type) and an update with no before image is a `DeserializationFailed` `Left` (`CdcDispatcher.cs:189`). SQL Server Change Tracking never supplies a before image for an update, so the issue's handler (`false -> true` on `IsDeleted`) cannot run there until the dispatcher accepts such updates. A `Left` from the dispatcher is neither retried nor dead-lettered (#2343, open).
5. **There is no entity key store.** The issue's `_keyStore.DestroyKeyAsync(entityId, ct)` has no counterpart; crypto-shredding exists only in `Encina.Marten.GDPR` for event-sourced data (`[CryptoShredded]`).

### Scope

- **Implements**: #635 without crypto-shredding. `CryptoShredOnExpiry` is not delivered; the follow-up issue is drafted with this plan.
- **Affected packages**: `Encina.Cdc` (handler, options, sweep, configuration), `Encina.DomainModeling` (read-only), `Encina.Compliance.Retention` (consumer, if Design Choice 1 picks the module), `Encina.Messaging` (consumer, if it picks scheduling), `Encina.ADO.SqlServer|PostgreSQL|MySQL`, `Encina.Dapper.SqlServer|PostgreSQL|MySQL`, `Encina.EntityFrameworkCore`, `Encina.MongoDB` (purge executor), `Encina.OpenTelemetry`.
- **Provider category**: Database, all 10 providers (AGENTS.md section 5): the CDC capture comes from four connectors, the purge executor is implemented per provider.
- **Compliance scope**: the plan cites GDPR and the Spanish organic law 3/2018 (LOPDGDD); texts of other jurisdictions in the issue (CCPA) are out of scope.
- **Corrections to the issue text**: `RetentionPolicy` becomes `SoftDeleteRetentionPolicy` (the Retention module already has policies); the policy needs an id selector because `ISoftDeletable` has no id; `AddPolicy<TEntity, TId>(idSelector, configure)`.
- **Split into pull requests**: PR 1 = Phases 1-3 (core, handler, engine adapter, about 13 files); PR 2 = Phase 4 (purge executor, ADO.NET and Dapper, 6 packages, about 13 files); PR 3 = Phase 5 (EF Core and MongoDB purge, about 6 files); PR 4 = Phases 6-9 remainder (configuration validation, observability, integration tests, documentation).

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
<summary><strong>5. Restore before expiry and duplicates</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The eraser re-reads the row at execution and purges only if `IsDeleted` is still true; the handler tracks an entity once (it looks the entity up first)** | A restored row is never purged; a duplicate CDC delivery does not create a second record | One read per purge; one lookup per captured soft delete |
| **B) Cancel the tracking on restore** | No execution-time read | The module has no cancel operation; the scheduler would need a cancel API and a stored message id per entity |

### Chosen Option: **A — re-read at execution, deduplicate at tracking**

### Rationale

Restore (`RestoreAsync`) clears `IsDeleted`; checking it at the moment of the irreversible action is the safe place.

**Class:** Settled
**Settled by:** AGENTS.md section 6 (function 9, idempotency) and AGENTS.md section 3 (a `Left` fails the operation)

</details>

<details>
<summary><strong>6. How the purge reaches the 10 providers</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `ISoftDeletePurger` in `Encina.DomainModeling`, implemented per provider package: EF Core and MongoDB delegate to their `ISoftDeleteRepository`, ADO.NET and Dapper build `DELETE ... WHERE id = @id AND IsDeleted = 1` from `ISoftDeleteEntityMapping`** | Same contract on the 10 providers; the repository stays optional | Six new SQL implementations |
| **B) Require the application to implement `IRetentionDataEraser`** | No Encina code per provider | Every application rewrites the same purge; no provider coherence |

### Chosen Option: **A — `ISoftDeletePurger` per provider**

### Rationale

Provider-dependent behavior must exist on all 10 providers with the same contract.

**Class:** Settled
**Settled by:** AGENTS.md section 5 (every provider-dependent feature on all 10 providers)

</details>

---

## Implementation Phases

### Phase 1: Models, options and the policy

> **Goal**: pure types, no I/O.

<details>
<summary><strong>Tasks</strong></summary>

1. `src/Encina.Cdc/SoftDelete/SoftDeleteRetentionPolicy.cs`: `EntityType`, `DataCategory` (default entity type name), `RetentionPeriod`, `EntityIdSelector` (`Func<object, string>`); `CryptoShredOnExpiry` is not added.
2. `SoftDeleteRetentionOptions`: `AddPolicy<TEntity, TId>(Func<TEntity, TId> idSelector, Action<SoftDeleteRetentionPolicyBuilder> configure)`, `SweepInterval` (24 hours), `EnableSweep` (true); no default period.
3. `IValidateOptions` rejecting non-positive periods and policies with no id selector; a startup report (log, no failure) of soft-deletable entities with no policy.
4. `SoftDeleteRetentionErrors` (`encina.cdc.soft_delete_tracking_failed`, `...policy_missing`).

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

1. `SoftDeleteRetentionCdcHandler<TEntity> where TEntity : class, ISoftDeletable`: `HandleUpdateAsync` acts when `after.IsDeleted && (before is null || !before.IsDeleted)`; it computes `expiresAtUtc = after.DeletedAtUtc + period` and calls `ISoftDeleteRetentionTracker.TrackAsync(...)`; `HandleInsertAsync` acts when a row arrives already deleted (a snapshot); `HandleDeleteAsync` (a real delete) returns `Right(unit)` and logs.
2. `ISoftDeleteRetentionTracker` (internal): `TrackAsync(SoftDeleteObservation, CancellationToken)`; `SoftDeleteObservation` holds entity type, entity id, `DeletedAtUtc`, `TenantId`.
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

1. `RetentionModuleSoftDeleteTracker : ISoftDeleteRetentionTracker`: looks up `IRetentionRecordService.GetRecordsByEntityAsync(entityId)` for an existing record of the same data category (deduplication), resolves the policy id through `IRetentionPolicyService.GetPolicyByCategoryAsync(dataCategory)` (creating it from `SoftDeleteRetentionPolicy` at startup through `RetentionOptions.AddPolicy`), then `TrackEntityAsync(entityId, dataCategory, policyId, max(expiresAtUtc - now, 0), tenantId)`.
2. `SoftDeleteRetentionEraser : IRetentionDataEraser`: for a target whose data category is a registered soft-delete policy, resolves `ISoftDeletePurger` and purges only if the row is still deleted (Design Choice 5); other categories fall through to the application's own eraser through a composite.
3. `SoftDeleteRetentionSweepService : BackgroundService` (`PeriodicTimer` through `TimeProvider`, catch per iteration, an `OperationCanceledException` from the stopping token ends the loop): asks `ISoftDeletePurger.EnumerateDeletedAsync` for deleted rows with no record and tracks them.
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

1. `ISoftDeletePurger` in `Encina.DomainModeling`: `PurgeAsync<TEntity, TId>(TId id, CancellationToken)` (re-reads, purges only when still deleted, idempotent when the row is gone) and `EnumerateDeletedAsync<TEntity>(...)` (page of ids of deleted rows).
2. `SoftDeletePurgerADO` and `SoftDeletePurgerDapper` per database package, built from `ISoftDeleteEntityMapping<TEntity, TId>`: parameterized `DELETE ... WHERE <key> = @id AND <IsDeleted> = <true literal for the database>` (`bit`, `true`, `1`).
3. Registration `AddEncinaSoftDeletePurger*` per package with `TryAddSingleton`.
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
CONTEXT: EF Core and MongoDB already have ISoftDeleteRepository.HardDeleteAsync, which loads the entity first.
TASK: Implement ISoftDeletePurger for both, without loading the entity image.
KEY RULES: no synchronous calls; the filter always includes the deleted flag; idempotent when the row is gone.
REFERENCE FILES: src/Encina.EntityFrameworkCore/SoftDelete/SoftDeleteRepositoryEF.cs, src/Encina.MongoDB/SoftDelete/SoftDeletableFunctionalRepositoryMongoDB.cs.
```

</details>

### Phase 6: Configuration, DI and validation

> **Goal**: `WithSoftDeleteRetention` registers everything and fails loudly when misconfigured.

<details>
<summary><strong>Tasks</strong></summary>

1. `CdcConfiguration.WithSoftDeleteRetention(Action<SoftDeleteRetentionOptions>)`: sets `CdcOptions.UseSoftDeleteRetention`, adds a `TableMapping` and a `HandlerRegistration` per policy.
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
3. `SoftDeleteRetentionLog` (EventIds 4870-4879): tracked, skipped duplicate, purged, restored before expiry, missing policy, sweep failed (error code only).
4. Audit: the module's event-sourced retention record is the audit of the request and the execution; the purge executor additionally emits an operation audit entry through `IOperationAuditStore` when it is registered.
5. `Encina.OpenTelemetry` subscription.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
CONTEXT: GDPR accountability (Article 5(2)) needs evidence of when deletion was requested and when it ran.
TASK: Add metrics, spans, logs, the audit entry and the OpenTelemetry subscription.
KEY RULES: EventIds 4870-4879 only; tags carry entity type names and ids, never personal data; ids only in logs.
REFERENCE FILES: src/Encina.Cdc/Caching/Diagnostics/CacheInvalidationMetrics.cs, src/Encina.Security.Audit/Abstractions/IOperationAuditStore.cs.
```

</details>

### Phase 8: Testing

> **Goal**: each applicable flag reaches its manifest target.

<details>
<summary><strong>Tasks</strong></summary>

1. Unit: handler (false to true, already deleted, restore, no before image, real delete), policy resolution, missing policy, tracker deduplication, eraser re-read, sweep loop (a failing iteration never ends it).
2. Guard: all public methods.
3. Contract: `ISoftDeletePurger` on all 10 providers.
4. Property: expiry arithmetic never goes negative and is monotonic in `DeletedAtUtc`.
5. Integration: soft delete, capture and purge on the 10 providers' shared fixtures; the Retention module path on PostgreSQL with Marten through Testcontainers.
6. Load and benchmark: justification files (one row per soft delete).

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
4. ADR number assigned when the ADR pull request opens (retention enforcement and the engine choice).
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
| `Encina.Cdc` (existing range 4800-4899) | 4870-4879 | `SoftDeleteRetentionLog`; 4854-4859 belong to #2393, 4860-4869 to #634, 4880-4899 to #636 |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Production (`Encina.Cdc`) | 12 | policy, builder, options, errors, handler, tracker abstraction and adapter, eraser, sweep, log, metrics, activity source |
| Purge executors | 9 | `ISoftDeletePurger` plus 8 provider implementations |
| Edits to shared files | 4 | `CdcConfiguration`, `CdcOptions`, `ServiceCollectionExtensions`, provider registrations |
| Tests | ~30 | unit, guard, contract (10 providers), property, integration |
| Docs, changelog, manifests | ~10 | README, feature pages, fragment, manifest entries |

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
4870-4879) and the audit entry. Phase 8 tests, Phase 9 documents. Deliver as four pull requests after the CDC
prerequisites land.

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
| 3 | Structured Logging | ✅ | `SoftDeleteRetentionLog`, EventIds 4870-4879 |
| 4 | Health Checks | ⏭️ | An overdue-purge indicator is the processor liveness work of #2396; the connector health is the existing `CdcHealthCheck` |
| 5 | Validation | ✅ | Options validated at startup; missing policy report; event payload validation is #730 |
| 6 | Resilience | ✅ | CDC retry and dead letter after #2343 and #2344; the Retention module retries a failed erasure on the next cycle |
| 7 | Distributed Locks | ⏭️ | Concurrent enforcement across instances follows the row claiming and leader election work of #1251 |
| 8 | Transactions | ✅ | The purge is one atomic statement; the audit entry is written after it by the purge executor |
| 9 | Idempotency | ✅ | Duplicate CDC events find the existing record (Design Choice 5); a purge of a gone row succeeds |
| 10 | Multi-Tenancy | ⏭️ | Per-tenant retention periods follow #1257; the tracker already passes `TenantId` when the entity has one |
| 11 | Module Isolation | ❌ | Not applicable: infrastructure-level |
| 12 | Audit Trail | ✅ | The retention record and the operation audit entry (Phase 7) |

---

## Prerequisites & Dependencies

| Item | State | Effect |
|------|-------|--------|
| #2343 (a `Left` from the dispatcher is neither retried nor dead-lettered) | open | Required for "a scheduling or tracking failure fails the CDC operation" |
| Dispatcher issue drafted with this plan (updates without a before image; several handlers per table) | to be opened | SQL Server updates have no before image; a soft-deletable entity usually has other handlers |
| Follow-up for entity crypto-shredding (drafted with this plan) | to be opened | Replaces `CryptoShredOnExpiry`, which has no key store to call |
| `Encina.Compliance.Retention` and Marten (if Design Choice 1 picks the module) | exists | PostgreSQL event store required |
| #1250 (column encryption keys) | open | Candidate key source for the crypto-shredding follow-up |

---

## Next Steps

1. Review and approve this plan; the maintainer answers the three Dilemmas (choices 1, 2 and 3) when work on the issue starts.
2. Publish the plan link as a comment on #635.
3. Begin PR 1 (Phases 1-3) after the dispatcher prerequisites land.
4. Each phase is a self-contained commit; the final commit references `Fixes #635`.
