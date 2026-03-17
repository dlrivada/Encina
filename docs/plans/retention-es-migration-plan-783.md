# [REFACTOR] Migrate Retention Module to Marten Event Sourcing — Implementation Plan

> **Issue**: [#783](https://github.com/dlrivada/Encina/issues/783)
> **Type**: Refactor (Event Sourcing Migration)
> **Complexity**: High — three aggregates (RetentionPolicy + RetentionRecord + LegalHold), pipeline behavior, background enforcement service, auto-registration, fluent policy configuration, integration with DataSubjectRights erasure
> **Prerequisites**: ADR-019 (#776) ✅, CrossBorderTransfer (#412) ✅, BreachNotification (#780) ✅, DPIA (#781) ✅, ProcessorAgreements (#782) ✅
> **Provider Category**: Event Sourcing (Marten) — replaces 13 database providers

---

## Summary

Migrate `Encina.Compliance.Retention` from entity-based persistence with 13 database provider implementations to Marten event sourcing. The module implements GDPR Art. 5(1)(e) storage limitation requirements through automated data retention management, legal hold capabilities, and enforcement-driven deletion/anonymization.

**Key transformation**:
- `RetentionPolicy` (sealed record) → `RetentionPolicyAggregate` (event-sourced aggregate)
- `RetentionRecord` (sealed record) → `RetentionRecordAggregate` (event-sourced aggregate)
- `LegalHold` (sealed record) → `LegalHoldAggregate` (event-sourced aggregate)
- `IRetentionPolicyStore` + `IRetentionRecordStore` + `ILegalHoldStore` + `IRetentionAuditStore` (13 provider implementations each) → `IAggregateRepository<T>` (Marten)
- `DefaultRetentionPolicy` + `DefaultRetentionEnforcer` + `DefaultLegalHoldManager` → `IRetentionPolicyService` + `IRetentionRecordService` + `ILegalHoldService`
- Audit trail moves from explicit `IRetentionAuditStore` to implicit event streams (events ARE the audit trail)

**Estimated scope**:
- ~19 new files created (aggregates, events, read models, projections, services, abstractions, Marten extensions)
- ~49 satellite provider files deleted (stores + documents + configurations across 13 providers)
- ~26 core files deleted (interfaces, InMemory stores, services, entities, mappers, models)
- ~11 core files modified (pipeline behavior, enforcement service, hosted services, DI, diagnostics, health check, options, errors)
- ~45 test files deleted, ~18 created, ~5 modified
- ~2,300 lines of new/modified production code + ~1,500 lines of tests

**Affected packages**: `Encina.Compliance.Retention` (core restructuring), 10 satellite packages (remove Retention stores), 5 test projects

---

## Design Choices

<details>
<summary><strong>1. Aggregate Granularity — Three Aggregates vs. Two (Issue Proposal)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Two aggregates: `RetentionPolicyAggregate` + `LegalHoldAggregate`** (issue proposal) | Matches issue sketch; fewer aggregate types | RetentionRecord tracking has no natural home; embedding records in policy aggregate creates unbounded growth (thousands of entities per policy) |
| **B) Three aggregates: `RetentionPolicyAggregate` + `RetentionRecordAggregate` + `LegalHoldAggregate`** | Clean lifecycle per domain concept; small event streams per instance; mirrors existing 4-store separation (minus audit which becomes implicit) | More aggregate types; cross-aggregate coordination for legal holds |
| **C) Single aggregate with embedded records and holds** | All data in one stream | Unbounded growth; violates aggregate sizing best practice |

### Selected Option
**B) Three aggregates** — `RetentionPolicyAggregate` manages policy definitions (create, update, deactivate). `RetentionRecordAggregate` tracks individual entity retention lifecycle (tracked → expired → deleted/anonymized, with hold/release transitions). `LegalHoldAggregate` manages hold lifecycle (place → lift).

### Rationale
The domain has three distinct lifecycles: (1) policy definitions are long-lived and change infrequently, (2) retention records are per-entity with a status machine driven by enforcement, (3) legal holds are per-entity-per-reason with independent place/lift lifecycle. Embedding records in policies would create unbounded event streams — a single "customer-data" policy might track millions of entities. This is analogous to ProcessorAgreements having separate Processor + DPA aggregates. The issue's 2-aggregate proposal was a high-level sketch; implementation refinement to 3 aggregates is justified by domain analysis.

</details>

<details>
<summary><strong>2. Service Interface Design — Three Services Replacing Seven Abstractions</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Three services: `IRetentionPolicyService` + `IRetentionRecordService` + `ILegalHoldService`** | One service per aggregate; clean CQRS; consistent with migrated modules | Breaking change (pre-1.0, acceptable) |
| **B) Single `IRetentionService`** | One entry point | Too many responsibilities; hard to navigate API |
| **C) Keep existing interfaces as facades** | No API change | Leaks entity-based abstractions; inconsistent with 7 other migrated modules |

### Selected Option
**A) Three services** — Each wraps one aggregate with ROP, caching, and observability. Replaces `IRetentionPolicyStore`, `IRetentionRecordStore`, `ILegalHoldStore`, `IRetentionAuditStore`, `IRetentionPolicy`, `IRetentionEnforcer`, and `ILegalHoldManager`.

### Rationale
Consistent with all previously migrated modules (Consent, DSR, LawfulBasis, BreachNotification, DPIA, ProcessorAgreements). Three services map 1:1 to three aggregates. The `IRetentionRecordService` absorbs the enforcement logic from `DefaultRetentionEnforcer` and period resolution from `DefaultRetentionPolicy`. Pre-1.0: breaking changes are expected.

</details>

<details>
<summary><strong>3. Audit Trail Strategy — ES Events ARE the Audit Trail</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) ES events as audit trail** | Zero extra storage; immutable by design; complete timeline; events carry more info than `RetentionAuditEntry` | Requires Marten event stream queries for audit reports |
| **B) Keep separate `IRetentionAuditStore`** | Familiar API | Redundant with ES events; two sources of truth |

### Selected Option
**A) ES events as audit trail** — `RetentionAuditEntry`, `IRetentionAuditStore`, and `InMemoryRetentionAuditStore` are all deleted. For audit queries, the services expose `GetPolicyHistoryAsync()`, `GetRecordHistoryAsync()`, and `GetHoldHistoryAsync()` which read event streams.

### Rationale
Every migrated compliance module uses this pattern. Event sourcing inherently satisfies GDPR Art. 5(2) accountability: every state change is an immutable event with timestamp, actor, and metadata. `RetentionAuditEntry` actions (PolicyCreated, RecordTracked, EnforcementExecuted, etc.) map directly to ES events.

</details>

<details>
<summary><strong>4. Enforcement Service — Preserved as BackgroundService, Rewired to Services</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Preserve `RetentionEnforcementService` as BackgroundService, rewire to use services** | Consistent with `TransferExpirationMonitor` pattern; minimal behavioral change | Needs rewiring |
| **B) Replace with scheduled command handler** | Consistent with ProcessorAgreements `CheckDPAExpirationHandler` pattern | Enforcement is more complex than expiration check; doesn't fit single-command pattern well |
| **C) Move enforcement into `IRetentionRecordService`** | Single responsibility | Background scheduling shouldn't be in a scoped service |

### Selected Option
**A) Preserve BackgroundService, rewire to services** — `RetentionEnforcementService` keeps its `PeriodicTimer` and scoped resolution pattern. It calls `IRetentionRecordService.GetExpiredRecordsAsync()`, `ILegalHoldService.IsUnderHoldAsync()`, and `IRetentionRecordService.ExecuteRetentionAsync()` instead of using stores directly.

### Rationale
The enforcement flow is complex (query expired → check holds → coordinate with `IDataErasureExecutor` → mark deleted → publish notifications). A BackgroundService is the right abstraction. The CrossBorderTransfer module's `TransferExpirationMonitor` follows this exact pattern.

</details>

<details>
<summary><strong>5. Pipeline Behavior — Rewired to IRetentionRecordService</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Pipeline calls `IRetentionRecordService.TrackEntityAsync()`** | Clean; service handles aggregate creation, caching, logging | Service call in pipeline path |
| **B) Pipeline calls `IAggregateRepository` directly** | No service overhead | Couples pipeline to Marten; bypasses caching and observability |
| **C) Remove pipeline, require explicit tracking** | Simpler pipeline | Loses auto-tracking from `[RetentionPeriod]` attribute (key DX feature) |

### Selected Option
**A) Pipeline calls `IRetentionRecordService`** — The `RetentionValidationPipelineBehavior` replaces `IRetentionRecordStore` dependency with `IRetentionRecordService`. Entity tracking goes through the service which handles aggregate creation, caching, and metrics. Attribute caching in `ConcurrentDictionary` and enforcement modes (Block/Warn/Disabled) are preserved.

### Rationale
The pipeline's attribute-based auto-tracking is a key developer experience feature. Rewiring to the service layer maintains this DX while routing through the standardized ES path with proper observability.

</details>

<details>
<summary><strong>6. Auto-Registration & Fluent Policy Hosted Services — Modified, Not Deleted</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Modify hosted services to call `IRetentionPolicyService`** | Preserves auto-registration DX; minimal behavioral change | Requires rewiring |
| **B) Delete hosted services, require explicit policy creation** | Simpler module | Loses `[RetentionPeriod]` auto-discovery and `options.AddPolicy()` fluent API (key DX features) |

### Selected Option
**A) Modify hosted services** — `RetentionAutoRegistrationHostedService` and `RetentionFluentPolicyHostedService` are modified to call `IRetentionPolicyService.CreatePolicyAsync()` instead of `IRetentionPolicyStore.CreateAsync()`. `RetentionAutoRegistrationDescriptor` and `RetentionFluentPolicyDescriptor` are preserved unchanged.

### Rationale
Auto-registration from `[RetentionPeriod]` attributes and fluent policy configuration via `options.AddPolicy()` are core DX features that don't depend on the persistence strategy. They just need to target the new service interface.

</details>

<details>
<summary><strong>7. Legal Hold Cross-Aggregate Coordination</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) ILegalHoldService coordinates: create hold aggregate + raise RecordHeld event on record aggregate** | Full audit trail on both aggregates; record read model reflects hold status directly | Cross-aggregate writes (sequential, not transactional) |
| **B) Hold status derived at query time from LegalHoldReadModel** | No cross-aggregate coordination | Record read model doesn't reflect current status; enforcement must always join two read models |
| **C) Embed hold status in LegalHoldAggregate only** | Simple | Record aggregate loses hold lifecycle events |

### Selected Option
**A) Cross-aggregate coordination in service** — When `ILegalHoldService.PlaceHoldAsync()` is called, it (1) creates `LegalHoldAggregate` and (2) raises `RetentionRecordHeld` on the affected `RetentionRecordAggregate`. When lifting, it (1) lifts the hold and (2) raises `RetentionRecordReleased` if no other active holds remain. Operations are sequential via `IAggregateRepository`, not transactional.

### Rationale
This ensures the `RetentionRecordReadModel` always reflects the current hold status without cross-read-model joins. The enforcement service can query a single read model for the complete picture. Sequential (non-transactional) coordination is acceptable because eventual consistency is fine for hold status — the enforcement service rechecks holds before deletion anyway.

</details>

---

## Architecture & Structure

### Target Directory Structure

```
src/Encina.Compliance.Retention/
├── Abstractions/
│   ├── IRetentionPolicyService.cs               ← NEW (policy lifecycle orchestrator)
│   ├── IRetentionRecordService.cs               ← NEW (record tracking + enforcement)
│   └── ILegalHoldService.cs                     ← NEW (hold lifecycle orchestrator)
├── Aggregates/
│   ├── RetentionPolicyAggregate.cs              ← NEW (event-sourced aggregate)
│   ├── RetentionRecordAggregate.cs              ← NEW (event-sourced aggregate)
│   └── LegalHoldAggregate.cs                    ← NEW (event-sourced aggregate)
├── Attributes/
│   └── RetentionPeriodAttribute.cs              ← PRESERVED
├── Diagnostics/
│   ├── RetentionDiagnostics.cs                  ← MODIFIED (add service counters)
│   └── RetentionLogMessages.cs                  ← MODIFIED (add service log messages)
├── Events/
│   ├── RetentionPolicyEvents.cs                 ← NEW (policy domain events)
│   ├── RetentionRecordEvents.cs                 ← NEW (record domain events)
│   └── LegalHoldEvents.cs                       ← NEW (hold domain events)
├── Health/
│   └── RetentionHealthCheck.cs                  ← MODIFIED (check services, not stores)
├── Model/                                       ← PRESERVED (value objects, enums)
│   ├── RetentionStatus.cs                       ← PRESERVED
│   ├── RetentionPolicyType.cs                   ← PRESERVED
│   ├── RetentionEnforcementMode.cs              ← PRESERVED (moved from root)
│   ├── DeletionResult.cs                        ← PRESERVED
│   ├── DeletionDetail.cs                        ← PRESERVED
│   ├── DeletionOutcome.cs                       ← PRESERVED
│   └── ExpiringData.cs                          ← PRESERVED
├── Notifications/                               ← PRESERVED
│   ├── DataDeletedNotification.cs               ← PRESERVED
│   ├── DataExpiringNotification.cs              ← PRESERVED
│   ├── LegalHoldAppliedNotification.cs          ← PRESERVED
│   ├── LegalHoldReleasedNotification.cs         ← PRESERVED
│   └── RetentionEnforcementCompletedNotification.cs ← PRESERVED
├── ReadModels/
│   ├── RetentionPolicyReadModel.cs              ← NEW (Marten projection target)
│   ├── RetentionPolicyProjection.cs             ← NEW (event → read model)
│   ├── RetentionRecordReadModel.cs              ← NEW (Marten projection target)
│   ├── RetentionRecordProjection.cs             ← NEW (event → read model)
│   ├── LegalHoldReadModel.cs                    ← NEW (Marten projection target)
│   └── LegalHoldProjection.cs                   ← NEW (event → read model)
├── Services/
│   ├── DefaultRetentionPolicyService.cs         ← NEW (aggregate lifecycle orchestrator)
│   ├── DefaultRetentionRecordService.cs         ← NEW (aggregate + enforcement orchestrator)
│   └── DefaultLegalHoldService.cs               ← NEW (aggregate lifecycle orchestrator)
├── RetentionErrors.cs                           ← MODIFIED (update for service errors)
├── RetentionOptions.cs                          ← MODIFIED (remove TrackAuditTrail)
├── RetentionOptionsValidator.cs                 ← PRESERVED
├── RetentionAutoRegistrationDescriptor.cs       ← PRESERVED
├── RetentionAutoRegistrationHostedService.cs    ← MODIFIED (use IRetentionPolicyService)
├── RetentionFluentPolicyDescriptor.cs           ← PRESERVED
├── RetentionFluentPolicyHostedService.cs        ← MODIFIED (use IRetentionPolicyService)
├── RetentionEnforcementService.cs               ← MODIFIED (use services)
├── RetentionValidationPipelineBehavior.cs       ← MODIFIED (use IRetentionRecordService)
├── RetentionMartenExtensions.cs                 ← NEW (Marten aggregate registration)
├── ServiceCollectionExtensions.cs               ← MODIFIED (replace InMemory with services)
├── PublicAPI.Shipped.txt                        ← MODIFIED
└── PublicAPI.Unshipped.txt                      ← MODIFIED
```

### Files to DELETE

**Core Module (26 files)**:
- `Abstractions/IRetentionPolicyStore.cs`
- `Abstractions/IRetentionRecordStore.cs`
- `Abstractions/ILegalHoldStore.cs`
- `Abstractions/IRetentionAuditStore.cs`
- `Abstractions/IRetentionPolicy.cs`
- `Abstractions/IRetentionEnforcer.cs`
- `Abstractions/ILegalHoldManager.cs`
- `InMemory/InMemoryRetentionPolicyStore.cs`
- `InMemory/InMemoryRetentionRecordStore.cs`
- `InMemory/InMemoryLegalHoldStore.cs`
- `InMemory/InMemoryRetentionAuditStore.cs`
- `DefaultRetentionPolicy.cs`
- `DefaultRetentionEnforcer.cs`
- `DefaultLegalHoldManager.cs`
- `Model/RetentionPolicy.cs` (replaced by aggregate)
- `Model/RetentionRecord.cs` (replaced by aggregate)
- `Model/LegalHold.cs` (replaced by aggregate)
- `Model/RetentionAuditEntry.cs` (replaced by ES events)
- `RetentionRecordEntity.cs`
- `RetentionPolicyEntity.cs`
- `LegalHoldEntity.cs`
- `RetentionAuditEntryEntity.cs`
- `RetentionRecordMapper.cs`
- `RetentionPolicyMapper.cs`
- `LegalHoldMapper.cs`
- `RetentionAuditEntryMapper.cs`

**Satellite Provider Stores (~49 files)**:

*ADO.NET (16 files)*:
- `src/Encina.ADO.{Sqlite,SqlServer,PostgreSQL,MySQL}/Retention/RetentionPolicyStoreADO.cs` (4)
- `src/Encina.ADO.{Sqlite,SqlServer,PostgreSQL,MySQL}/Retention/RetentionRecordStoreADO.cs` (4)
- `src/Encina.ADO.{Sqlite,SqlServer,PostgreSQL,MySQL}/Retention/LegalHoldStoreADO.cs` (4)
- `src/Encina.ADO.{Sqlite,SqlServer,PostgreSQL,MySQL}/Retention/RetentionAuditStoreADO.cs` (4)

*Dapper (16 files)*:
- `src/Encina.Dapper.{Sqlite,SqlServer,PostgreSQL,MySQL}/Retention/RetentionPolicyStoreDapper.cs` (4)
- `src/Encina.Dapper.{Sqlite,SqlServer,PostgreSQL,MySQL}/Retention/RetentionRecordStoreDapper.cs` (4)
- `src/Encina.Dapper.{Sqlite,SqlServer,PostgreSQL,MySQL}/Retention/LegalHoldStoreDapper.cs` (4)
- `src/Encina.Dapper.{Sqlite,SqlServer,PostgreSQL,MySQL}/Retention/RetentionAuditStoreDapper.cs` (4)

*EF Core (9 files)*:
- `src/Encina.EntityFrameworkCore/Retention/RetentionPolicyStoreEF.cs`
- `src/Encina.EntityFrameworkCore/Retention/RetentionRecordStoreEF.cs`
- `src/Encina.EntityFrameworkCore/Retention/LegalHoldStoreEF.cs`
- `src/Encina.EntityFrameworkCore/Retention/RetentionAuditStoreEF.cs`
- `src/Encina.EntityFrameworkCore/Retention/RetentionPolicyEntityConfiguration.cs`
- `src/Encina.EntityFrameworkCore/Retention/RetentionRecordEntityConfiguration.cs`
- `src/Encina.EntityFrameworkCore/Retention/LegalHoldEntityConfiguration.cs`
- `src/Encina.EntityFrameworkCore/Retention/RetentionAuditEntryEntityConfiguration.cs`
- `src/Encina.EntityFrameworkCore/Retention/RetentionModelBuilderExtensions.cs`

*MongoDB (8 files)*:
- `src/Encina.MongoDB/Retention/RetentionPolicyStoreMongoDB.cs`
- `src/Encina.MongoDB/Retention/RetentionRecordStoreMongoDB.cs`
- `src/Encina.MongoDB/Retention/LegalHoldStoreMongoDB.cs`
- `src/Encina.MongoDB/Retention/RetentionAuditStoreMongoDB.cs`
- `src/Encina.MongoDB/Retention/RetentionPolicyDocument.cs`
- `src/Encina.MongoDB/Retention/RetentionRecordDocument.cs`
- `src/Encina.MongoDB/Retention/LegalHoldDocument.cs`
- `src/Encina.MongoDB/Retention/RetentionAuditEntryDocument.cs`

---

## Implementation Phases (6 Phases)

| Phase | Description | Status |
|-------|-------------|--------|
| Phase 1 | Events & Aggregates | ✅ Complete |
| Phase 2 | Read Models & Projections | ✅ Complete |
| Phase 3 | Service Interfaces & Implementations | ✅ Complete |
| Phase 4 | Wiring — DI, Pipeline, Health Check, Background Services | ✅ Complete |
| Phase 5 | Delete Old Code & Update Satellite Packages | ✅ Complete |
| Phase 6 | Observability, Testing & Documentation | ✅ Complete |

**Final verification**: 0 build errors, 0 warnings, 358 tests passing (298 unit + 46 guard + 14 property).

### Phase 1: Events & Aggregates (~9 new files, ~700 lines)

> **Goal**: Create the event-sourced aggregates and domain events that model the retention policy, record tracking, and legal hold lifecycles.

<details>
<summary><strong>Tasks</strong></summary>

**1.1 Create `Events/RetentionPolicyEvents.cs`**
- Namespace: `Encina.Compliance.Retention.Events`
- All events are `sealed record` implementing `INotification`
- Events:
  - `RetentionPolicyCreated(Guid PolicyId, string DataCategory, TimeSpan RetentionPeriod, bool AutoDelete, RetentionPolicyType PolicyType, string? Reason, string? LegalBasis, DateTimeOffset OccurredAtUtc, string? TenantId, string? ModuleId)`
  - `RetentionPolicyUpdated(Guid PolicyId, TimeSpan RetentionPeriod, bool AutoDelete, string? Reason, string? LegalBasis, DateTimeOffset OccurredAtUtc)`
  - `RetentionPolicyDeactivated(Guid PolicyId, string Reason, DateTimeOffset OccurredAtUtc)`

**1.2 Create `Events/RetentionRecordEvents.cs`**
- Events:
  - `RetentionRecordTracked(Guid RecordId, string EntityId, string DataCategory, Guid PolicyId, TimeSpan RetentionPeriod, DateTimeOffset ExpiresAtUtc, DateTimeOffset OccurredAtUtc, string? TenantId, string? ModuleId)`
  - `RetentionRecordExpired(Guid RecordId, string EntityId, string DataCategory, DateTimeOffset OccurredAtUtc)`
  - `RetentionRecordHeld(Guid RecordId, string EntityId, Guid LegalHoldId, DateTimeOffset OccurredAtUtc)`
  - `RetentionRecordReleased(Guid RecordId, string EntityId, Guid LegalHoldId, DateTimeOffset OccurredAtUtc)`
  - `DataDeleted(Guid RecordId, string EntityId, string DataCategory, Guid PolicyId, DateTimeOffset DeletedAtUtc)`
  - `DataAnonymized(Guid RecordId, string EntityId, string DataCategory, Guid PolicyId, DateTimeOffset AnonymizedAtUtc)`

**1.3 Create `Events/LegalHoldEvents.cs`**
- Events:
  - `LegalHoldPlaced(Guid HoldId, string EntityId, string Reason, string AppliedByUserId, DateTimeOffset AppliedAtUtc, string? TenantId, string? ModuleId)`
  - `LegalHoldLifted(Guid HoldId, string EntityId, string ReleasedByUserId, DateTimeOffset ReleasedAtUtc)`

**1.4 Create `Aggregates/RetentionPolicyAggregate.cs`**
- Extends `AggregateBase`
- Properties (`private set`): `DataCategory`, `RetentionPeriod`, `AutoDelete`, `PolicyType`, `Reason`, `LegalBasis`, `IsActive`, `TenantId`, `ModuleId`, `CreatedAtUtc`, `LastUpdatedAtUtc`
- Factory: `static RetentionPolicyAggregate Create(Guid id, string dataCategory, TimeSpan retentionPeriod, bool autoDelete, RetentionPolicyType policyType, string? reason, string? legalBasis, DateTimeOffset occurredAtUtc, string? tenantId, string? moduleId)`
- Commands: `Update(...)`, `Deactivate(string reason, DateTimeOffset occurredAtUtc)`
- `protected override void Apply(object domainEvent)` with switch on all 3 event types
- Guard: cannot update/deactivate a deactivated policy

**1.5 Create `Aggregates/RetentionRecordAggregate.cs`**
- Extends `AggregateBase`
- Properties (`private set`): `EntityId`, `DataCategory`, `PolicyId`, `RetentionPeriod`, `Status` (RetentionStatus), `ExpiresAtUtc`, `LegalHoldId` (nullable), `TenantId`, `ModuleId`, `CreatedAtUtc`, `LastUpdatedAtUtc`
- Factory: `static RetentionRecordAggregate Track(Guid id, string entityId, string dataCategory, Guid policyId, TimeSpan retentionPeriod, DateTimeOffset expiresAtUtc, DateTimeOffset occurredAtUtc, string? tenantId, string? moduleId)`
- Commands: `MarkExpired(DateTimeOffset occurredAtUtc)`, `Hold(Guid legalHoldId, DateTimeOffset occurredAtUtc)`, `Release(Guid legalHoldId, DateTimeOffset occurredAtUtc)`, `MarkDeleted(DateTimeOffset deletedAtUtc)`, `MarkAnonymized(DateTimeOffset anonymizedAtUtc)`
- State machine guards:
  - Cannot expire a deleted/anonymized record
  - Cannot hold a deleted/anonymized record
  - Cannot delete/anonymize an already deleted/anonymized record
  - Cannot release a record not under hold

**1.6 Create `Aggregates/LegalHoldAggregate.cs`**
- Extends `AggregateBase`
- Properties (`private set`): `EntityId`, `Reason`, `AppliedByUserId`, `IsActive`, `ReleasedByUserId`, `AppliedAtUtc`, `ReleasedAtUtc`, `TenantId`, `ModuleId`
- Factory: `static LegalHoldAggregate Place(Guid id, string entityId, string reason, string appliedByUserId, DateTimeOffset appliedAtUtc, string? tenantId, string? moduleId)`
- Commands: `Lift(string releasedByUserId, DateTimeOffset releasedAtUtc)`
- Guard: cannot lift an already-lifted hold

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
You are migrating the Encina.Compliance.Retention module from entity-based persistence to Marten event sourcing.
This follows the exact pattern established by CrossBorderTransfer, BreachNotification, DPIA, and ProcessorAgreements.

TASK:
Create 6 new files in src/Encina.Compliance.Retention/:

1. Events/RetentionPolicyEvents.cs — 3 domain events as sealed records implementing INotification.
   Events: RetentionPolicyCreated (full policy data + TenantId/ModuleId), RetentionPolicyUpdated,
   RetentionPolicyDeactivated. All events include OccurredAtUtc.

2. Events/RetentionRecordEvents.cs — 6 domain events as sealed records implementing INotification.
   Events: RetentionRecordTracked (entity tracking start), RetentionRecordExpired, RetentionRecordHeld,
   RetentionRecordReleased, DataDeleted, DataAnonymized. RetentionRecordTracked includes TenantId/ModuleId.

3. Events/LegalHoldEvents.cs — 2 domain events as sealed records implementing INotification.
   Events: LegalHoldPlaced (with AppliedByUserId + TenantId/ModuleId), LegalHoldLifted (with ReleasedByUserId).

4. Aggregates/RetentionPolicyAggregate.cs — Event-sourced aggregate extending AggregateBase.
   Properties (private set): DataCategory, RetentionPeriod (TimeSpan), AutoDelete, PolicyType
   (RetentionPolicyType), Reason, LegalBasis, IsActive, TenantId, ModuleId, CreatedAtUtc, LastUpdatedAtUtc.
   Factory: static Create(). Commands: Update(), Deactivate(). Apply() handles all 3 event types.

5. Aggregates/RetentionRecordAggregate.cs — Event-sourced aggregate extending AggregateBase.
   Properties (private set): EntityId, DataCategory, PolicyId, RetentionPeriod (TimeSpan),
   Status (RetentionStatus), ExpiresAtUtc, LegalHoldId (Guid?), TenantId, ModuleId, CreatedAtUtc, LastUpdatedAtUtc.
   Factory: static Track(). Commands: MarkExpired(), Hold(), Release(), MarkDeleted(), MarkAnonymized().
   State machine with guards (cannot delete already deleted, etc.). Apply() handles all 6 event types.

6. Aggregates/LegalHoldAggregate.cs — Event-sourced aggregate extending AggregateBase.
   Properties (private set): EntityId, Reason, AppliedByUserId, IsActive, ReleasedByUserId,
   AppliedAtUtc, ReleasedAtUtc, TenantId, ModuleId.
   Factory: static Place(). Commands: Lift(). Guard: cannot lift already-lifted hold.

KEY RULES:
- .NET 10 / C# 14, nullable enabled
- Events are sealed records implementing INotification (from Encina core, NOT MediatR)
- Timestamps use DateTimeOffset with AtUtc suffix
- Aggregate validates state transitions (throw InvalidOperationException for invalid transitions)
- Factory method is static, behavior methods are instance
- Apply method uses switch on domainEvent type
- XML documentation on all public types with GDPR article references (Art. 5(1)(e), 5(2), 17(3)(e))
- Guard clauses: ArgumentNullException.ThrowIfNull, ArgumentException.ThrowIfNullOrWhiteSpace
- RetentionStatus, RetentionPolicyType are EXISTING types in Model/ — PRESERVE them

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/Aggregates/BreachAggregate.cs (aggregate pattern)
- src/Encina.Compliance.BreachNotification/Events/BreachNotificationEvents.cs (events pattern)
- src/Encina.Compliance.ProcessorAgreements/Aggregates/DPAAggregate.cs (multi-state aggregate)
- src/Encina.Compliance.Retention/Model/RetentionStatus.cs (existing enum)
- src/Encina.Compliance.Retention/Model/RetentionPolicyType.cs (existing enum)
```

</details>

---

### Phase 2: Read Models & Projections (~6 new files, ~500 lines)

> **Goal**: Create query-side read models and Marten projections for all three aggregates.

<details>
<summary><strong>Tasks</strong></summary>

**2.1 Create `ReadModels/RetentionPolicyReadModel.cs`**
- Implements `IReadModel`
- Properties (`{ get; set; }`): `Guid Id`, `string DataCategory`, `TimeSpan RetentionPeriod`, `bool AutoDelete`, `RetentionPolicyType PolicyType`, `string? Reason`, `string? LegalBasis`, `bool IsActive`, `string? TenantId`, `string? ModuleId`, `DateTimeOffset CreatedAtUtc`, `DateTimeOffset LastModifiedAtUtc`, `int Version`

**2.2 Create `ReadModels/RetentionPolicyProjection.cs`**
- Implements `IProjection<RetentionPolicyReadModel>`, `IProjectionCreator<RetentionPolicyCreated, RetentionPolicyReadModel>`, `IProjectionHandler` for Updated/Deactivated
- `ProjectionName => "RetentionPolicyProjection"`
- Each handler increments `Version++` and updates `LastModifiedAtUtc`

**2.3 Create `ReadModels/RetentionRecordReadModel.cs`**
- Implements `IReadModel`
- Properties: `Guid Id`, `string EntityId`, `string DataCategory`, `Guid PolicyId`, `TimeSpan RetentionPeriod`, `RetentionStatus Status`, `DateTimeOffset ExpiresAtUtc`, `Guid? LegalHoldId`, `DateTimeOffset? DeletedAtUtc`, `DateTimeOffset? AnonymizedAtUtc`, `string? TenantId`, `string? ModuleId`, `DateTimeOffset CreatedAtUtc`, `DateTimeOffset LastModifiedAtUtc`, `int Version`
- Helper: `bool IsExpired(DateTimeOffset nowUtc)` → `Status == Active && ExpiresAtUtc <= nowUtc`

**2.4 Create `ReadModels/RetentionRecordProjection.cs`**
- Implements `IProjection<RetentionRecordReadModel>`, `IProjectionCreator<RetentionRecordTracked, RetentionRecordReadModel>`, `IProjectionHandler` for Expired, Held, Released, DataDeleted, DataAnonymized
- Status mapping: Tracked→Active, Expired→Expired, Held→UnderLegalHold, Released→(recalculate: Expired if past deadline, Active otherwise), DataDeleted→Deleted, DataAnonymized→Deleted

**2.5 Create `ReadModels/LegalHoldReadModel.cs`**
- Implements `IReadModel`
- Properties: `Guid Id`, `string EntityId`, `string Reason`, `string AppliedByUserId`, `bool IsActive`, `string? ReleasedByUserId`, `DateTimeOffset AppliedAtUtc`, `DateTimeOffset? ReleasedAtUtc`, `string? TenantId`, `string? ModuleId`, `DateTimeOffset LastModifiedAtUtc`, `int Version`

**2.6 Create `ReadModels/LegalHoldProjection.cs`**
- Implements `IProjection<LegalHoldReadModel>`, `IProjectionCreator<LegalHoldPlaced, LegalHoldReadModel>`, `IProjectionHandler<LegalHoldLifted, LegalHoldReadModel>`
- `ProjectionName => "LegalHoldProjection"`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
You are implementing Phase 2 of Issue #783 — Migrate Retention to Marten event sourcing.
Phase 1 is complete: 3 aggregates (RetentionPolicyAggregate, RetentionRecordAggregate, LegalHoldAggregate)
and 11 domain events exist.

TASK:
Create 6 files in src/Encina.Compliance.Retention/ReadModels/:

1. RetentionPolicyReadModel.cs — sealed class implementing IReadModel with mutable properties.
   Mirrors RetentionPolicyAggregate state. Includes LastModifiedAtUtc, Version.

2. RetentionPolicyProjection.cs — IProjection<RetentionPolicyReadModel> +
   IProjectionCreator<RetentionPolicyCreated> + IProjectionHandler for Updated, Deactivated.

3. RetentionRecordReadModel.cs — sealed class implementing IReadModel.
   Mirrors RetentionRecordAggregate state. Properties include Status (RetentionStatus),
   ExpiresAtUtc, LegalHoldId, DeletedAtUtc, AnonymizedAtUtc.
   Helper: IsExpired(nowUtc) method.

4. RetentionRecordProjection.cs — IProjection<RetentionRecordReadModel> +
   IProjectionCreator<RetentionRecordTracked> + IProjectionHandler for Expired, Held, Released,
   DataDeleted, DataAnonymized. Status mapping: Tracked→Active, Held→UnderLegalHold,
   Released→recalculate based on ExpiresAtUtc.

5. LegalHoldReadModel.cs — sealed class implementing IReadModel. Includes IsActive bool.

6. LegalHoldProjection.cs — IProjection<LegalHoldReadModel> +
   IProjectionCreator<LegalHoldPlaced> + IProjectionHandler<LegalHoldLifted>.

KEY RULES:
- Read models have mutable properties (get; set;) for projection updates
- Always increment Version and update LastModifiedAtUtc on each event
- ProjectionName property returns a unique string
- XML documentation on all public types
- .NET 10 / C# 14, nullable enabled

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/ReadModels/BreachReadModel.cs
- src/Encina.Compliance.BreachNotification/ReadModels/BreachProjection.cs
- src/Encina.Compliance.ProcessorAgreements/ReadModels/DPAReadModel.cs
- src/Encina.Compliance.ProcessorAgreements/ReadModels/DPAProjection.cs
```

</details>

---

### Phase 3: Service Interfaces & Implementations (~6 new files, ~900 lines)

> **Goal**: Create three service interfaces and their default implementations with aggregate lifecycle management, read model queries, caching, and observability.

<details>
<summary><strong>Tasks</strong></summary>

**3.1 Create `Abstractions/IRetentionPolicyService.cs`**
- Namespace: `Encina.Compliance.Retention.Abstractions`
- **Commands**:
  - `CreatePolicyAsync(string dataCategory, TimeSpan retentionPeriod, bool autoDelete, RetentionPolicyType policyType, string? reason, string? legalBasis, string? tenantId, string? moduleId, CancellationToken ct)` → `ValueTask<Either<EncinaError, Guid>>`
  - `UpdatePolicyAsync(Guid policyId, TimeSpan retentionPeriod, bool autoDelete, string? reason, string? legalBasis, CancellationToken ct)` → `ValueTask<Either<EncinaError, Unit>>`
  - `DeactivatePolicyAsync(Guid policyId, string reason, CancellationToken ct)` → `ValueTask<Either<EncinaError, Unit>>`
- **Queries**:
  - `GetPolicyAsync(Guid policyId, CancellationToken ct)` → `ValueTask<Either<EncinaError, RetentionPolicyReadModel>>`
  - `GetPolicyByCategoryAsync(string dataCategory, CancellationToken ct)` → `ValueTask<Either<EncinaError, RetentionPolicyReadModel>>`
  - `GetAllPoliciesAsync(CancellationToken ct)` → `ValueTask<Either<EncinaError, IReadOnlyList<RetentionPolicyReadModel>>>`
  - `GetRetentionPeriodAsync(string dataCategory, CancellationToken ct)` → `ValueTask<Either<EncinaError, TimeSpan>>` (resolves period from policy, replaces IRetentionPolicy)
  - `GetPolicyHistoryAsync(Guid policyId, CancellationToken ct)` → `ValueTask<Either<EncinaError, IReadOnlyList<object>>>` (event stream = audit)

**3.2 Create `Abstractions/IRetentionRecordService.cs`**
- **Commands**:
  - `TrackEntityAsync(string entityId, string dataCategory, Guid policyId, TimeSpan retentionPeriod, string? tenantId, string? moduleId, CancellationToken ct)` → `ValueTask<Either<EncinaError, Guid>>`
  - `MarkExpiredAsync(Guid recordId, CancellationToken ct)` → `ValueTask<Either<EncinaError, Unit>>`
  - `HoldRecordAsync(Guid recordId, Guid legalHoldId, CancellationToken ct)` → `ValueTask<Either<EncinaError, Unit>>`
  - `ReleaseRecordAsync(Guid recordId, Guid legalHoldId, CancellationToken ct)` → `ValueTask<Either<EncinaError, Unit>>`
  - `MarkDeletedAsync(Guid recordId, CancellationToken ct)` → `ValueTask<Either<EncinaError, Unit>>`
  - `MarkAnonymizedAsync(Guid recordId, CancellationToken ct)` → `ValueTask<Either<EncinaError, Unit>>`
- **Queries**:
  - `GetRecordAsync(Guid recordId, CancellationToken ct)` → `ValueTask<Either<EncinaError, RetentionRecordReadModel>>`
  - `GetRecordsByEntityIdAsync(string entityId, CancellationToken ct)` → `ValueTask<Either<EncinaError, IReadOnlyList<RetentionRecordReadModel>>>`
  - `GetExpiredRecordsAsync(CancellationToken ct)` → `ValueTask<Either<EncinaError, IReadOnlyList<RetentionRecordReadModel>>>`
  - `GetExpiringWithinAsync(TimeSpan window, CancellationToken ct)` → `ValueTask<Either<EncinaError, IReadOnlyList<RetentionRecordReadModel>>>`
  - `IsExpiredAsync(string entityId, string dataCategory, CancellationToken ct)` → `ValueTask<Either<EncinaError, bool>>` (replaces IRetentionPolicy.IsExpiredAsync)
  - `GetRecordHistoryAsync(Guid recordId, CancellationToken ct)` → `ValueTask<Either<EncinaError, IReadOnlyList<object>>>`

**3.3 Create `Abstractions/ILegalHoldService.cs`**
- **Commands**:
  - `PlaceHoldAsync(string entityId, string reason, string appliedByUserId, string? tenantId, string? moduleId, CancellationToken ct)` → `ValueTask<Either<EncinaError, Guid>>`
  - `LiftHoldAsync(Guid holdId, string releasedByUserId, CancellationToken ct)` → `ValueTask<Either<EncinaError, Unit>>`
- **Queries**:
  - `GetHoldAsync(Guid holdId, CancellationToken ct)` → `ValueTask<Either<EncinaError, LegalHoldReadModel>>`
  - `IsUnderHoldAsync(string entityId, CancellationToken ct)` → `ValueTask<Either<EncinaError, bool>>`
  - `GetActiveHoldsAsync(CancellationToken ct)` → `ValueTask<Either<EncinaError, IReadOnlyList<LegalHoldReadModel>>>`
  - `GetActiveHoldsByEntityIdAsync(string entityId, CancellationToken ct)` → `ValueTask<Either<EncinaError, IReadOnlyList<LegalHoldReadModel>>>`
  - `GetHoldHistoryAsync(Guid holdId, CancellationToken ct)` → `ValueTask<Either<EncinaError, IReadOnlyList<object>>>`

**3.4 Create `Services/DefaultRetentionPolicyService.cs`**
- Dependencies: `IAggregateRepository<RetentionPolicyAggregate>`, `IReadModelRepository<RetentionPolicyReadModel>`, `ICacheProvider`, `IOptions<RetentionOptions>`, `TimeProvider`, `ILogger<DefaultRetentionPolicyService>`
- Cache keys: `"ret:policy:{id}"`, `"ret:policy:cat:{dataCategory}"`, `"ret:policies:all"`
- Command pattern: create aggregate → persist via repository → invalidate cache → record metric
- Query pattern: check cache → read from read model repo → populate cache (5 min TTL) → return
- `GetRetentionPeriodAsync`: resolves from policy, falls back to `RetentionOptions.DefaultRetentionPeriod`

**3.5 Create `Services/DefaultRetentionRecordService.cs`**
- Dependencies: `IAggregateRepository<RetentionRecordAggregate>`, `IReadModelRepository<RetentionRecordReadModel>`, `ICacheProvider`, `TimeProvider`, `ILogger<DefaultRetentionRecordService>`
- Cache keys: `"ret:record:{id}"`, `"ret:records:entity:{entityId}"`
- `TrackEntityAsync`: creates `RetentionRecordAggregate.Track()` with computed ExpiresAtUtc (now + retentionPeriod)
- `GetExpiredRecordsAsync`: queries read model for Status=Active AND ExpiresAtUtc <= now
- `GetExpiringWithinAsync`: queries read model for Status=Active AND ExpiresAtUtc <= now+window AND ExpiresAtUtc > now

**3.6 Create `Services/DefaultLegalHoldService.cs`**
- Dependencies: `IAggregateRepository<LegalHoldAggregate>`, `IReadModelRepository<LegalHoldReadModel>`, `IRetentionRecordService`, `ICacheProvider`, `TimeProvider`, `ILogger<DefaultLegalHoldService>`
- Cache keys: `"ret:hold:{id}"`, `"ret:holds:entity:{entityId}"`
- `PlaceHoldAsync`: (1) create `LegalHoldAggregate.Place()`, (2) find retention records for entity via `IRetentionRecordService`, (3) call `IRetentionRecordService.HoldRecordAsync()` for each
- `LiftHoldAsync`: (1) lift hold on aggregate, (2) check if entity has other active holds, (3) if no remaining holds → call `IRetentionRecordService.ReleaseRecordAsync()` for each
- `IsUnderHoldAsync`: query LegalHoldReadModel for active holds on entity

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
You are implementing Phase 3 of Issue #783 — Migrate Retention to Marten event sourcing.
Phases 1-2 are complete: three aggregates, 11 events, three read models, and three projections exist.

TASK:
Create 6 files:

1. Abstractions/IRetentionPolicyService.cs — Interface with 3 command methods (CreatePolicyAsync,
   UpdatePolicyAsync, DeactivatePolicyAsync) and 5 query methods (GetPolicyAsync,
   GetPolicyByCategoryAsync, GetAllPoliciesAsync, GetRetentionPeriodAsync, GetPolicyHistoryAsync).
   All return ValueTask<Either<EncinaError, T>>.

2. Abstractions/IRetentionRecordService.cs — Interface with 6 command methods (TrackEntityAsync,
   MarkExpiredAsync, HoldRecordAsync, ReleaseRecordAsync, MarkDeletedAsync, MarkAnonymizedAsync)
   and 6 query methods (GetRecordAsync, GetRecordsByEntityIdAsync, GetExpiredRecordsAsync,
   GetExpiringWithinAsync, IsExpiredAsync, GetRecordHistoryAsync).

3. Abstractions/ILegalHoldService.cs — Interface with 2 command methods (PlaceHoldAsync, LiftHoldAsync)
   and 5 query methods (GetHoldAsync, IsUnderHoldAsync, GetActiveHoldsAsync,
   GetActiveHoldsByEntityIdAsync, GetHoldHistoryAsync).

4. Services/DefaultRetentionPolicyService.cs — Implementation with IAggregateRepository<RetentionPolicyAggregate>,
   IReadModelRepository<RetentionPolicyReadModel>, ICacheProvider, IOptions<RetentionOptions>,
   TimeProvider, ILogger. Cache-aside with "ret:policy:{id}" keys. GetRetentionPeriodAsync
   falls back to RetentionOptions.DefaultRetentionPeriod.

5. Services/DefaultRetentionRecordService.cs — Implementation with IAggregateRepository<RetentionRecordAggregate>,
   IReadModelRepository<RetentionRecordReadModel>, ICacheProvider, TimeProvider, ILogger.
   TrackEntityAsync computes ExpiresAtUtc = now + retentionPeriod.
   GetExpiredRecordsAsync queries for Status=Active AND ExpiresAtUtc <= now.

6. Services/DefaultLegalHoldService.cs — Implementation with IAggregateRepository<LegalHoldAggregate>,
   IReadModelRepository<LegalHoldReadModel>, IRetentionRecordService, ICacheProvider,
   TimeProvider, ILogger. PlaceHoldAsync creates hold + holds affected records.
   LiftHoldAsync lifts hold + releases records if no remaining holds.

KEY RULES:
- ROP: Either<EncinaError, T> on all methods
- Cache-aside: check cache → load → cache with 5 min TTL
- Invalidate cache after writes (fire-and-forget async)
- catch InvalidOperationException → return domain error
- catch Exception (not OperationCanceledException) → return store error
- Use RetentionDiagnostics counters and logger extension methods
- XML docs with GDPR Art. 5(1)(e), 5(2), 17(3)(e) references

REFERENCE FILES:
- src/Encina.Compliance.CrossBorderTransfer/Services/DefaultTIAService.cs (service pattern)
- src/Encina.Compliance.CrossBorderTransfer/Abstractions/ITIAService.cs (interface pattern)
- src/Encina.Compliance.BreachNotification/Services/DefaultBreachNotificationService.cs
- src/Encina.Compliance.Retention/RetentionErrors.cs (existing errors)
- src/Encina.Compliance.Retention/Model/DeletionResult.cs (preserved for enforcement)
```

</details>

---

### Phase 4: Wiring — DI, Pipeline, Health Check, Background Services, Marten Extensions (~12 files modified/created, ~500 lines)

> **Goal**: Wire everything together: update DI registrations, pipeline behavior, health check, enforcement service, hosted services, and create Marten extensions.

<details>
<summary><strong>Tasks</strong></summary>

**4.1 Create `RetentionMartenExtensions.cs`**
- `public static IServiceCollection AddRetentionAggregates(this IServiceCollection services)`
- Registers: `AddAggregateRepository<RetentionPolicyAggregate>()`, `AddAggregateRepository<RetentionRecordAggregate>()`, `AddAggregateRepository<LegalHoldAggregate>()`
- Registers: `AddProjection<RetentionPolicyProjection, RetentionPolicyReadModel>()`, `AddProjection<RetentionRecordProjection, RetentionRecordReadModel>()`, `AddProjection<LegalHoldProjection, LegalHoldReadModel>()`

**4.2 Modify `ServiceCollectionExtensions.cs`**
- Remove: `TryAddSingleton<IRetentionPolicyStore, InMemoryRetentionPolicyStore>`
- Remove: `TryAddSingleton<IRetentionRecordStore, InMemoryRetentionRecordStore>`
- Remove: `TryAddSingleton<ILegalHoldStore, InMemoryLegalHoldStore>`
- Remove: `TryAddSingleton<IRetentionAuditStore, InMemoryRetentionAuditStore>`
- Remove: `TryAddSingleton<IRetentionPolicy, DefaultRetentionPolicy>`
- Remove: `TryAddSingleton<IRetentionEnforcer, DefaultRetentionEnforcer>`
- Remove: `TryAddSingleton<ILegalHoldManager, DefaultLegalHoldManager>`
- Add: `TryAddScoped<IRetentionPolicyService, DefaultRetentionPolicyService>`
- Add: `TryAddScoped<IRetentionRecordService, DefaultRetentionRecordService>`
- Add: `TryAddScoped<ILegalHoldService, DefaultLegalHoldService>`
- Keep: `RetentionValidationPipelineBehavior<,>` registration
- Keep: Health check, TimeProvider, enforcement service, auto-registration services

**4.3 Modify `RetentionValidationPipelineBehavior.cs`**
- Replace `IRetentionRecordStore` dependency with `IRetentionRecordService`
- Replace `IRetentionPolicyStore` dependency with `IRetentionPolicyService`
- Remove `IRetentionAuditStore` dependency (audit is implicit in ES)
- Record creation: `_recordService.TrackEntityAsync(entityId, category, policyId, period, ...)`
- Period resolution: `_policyService.GetRetentionPeriodAsync(category, ct)` or `_policyService.GetPolicyByCategoryAsync(category, ct)`
- Keep: `ConcurrentDictionary` attribute caching, enforcement modes, diagnostics

**4.4 Modify `RetentionEnforcementService.cs`**
- Replace `IRetentionRecordStore`, `ILegalHoldStore`, `IRetentionAuditStore` dependencies with `IRetentionRecordService`, `ILegalHoldService`
- Optional `IDataErasureExecutor` dependency preserved (for physical deletion)
- Enforcement flow:
  1. `_recordService.GetExpiredRecordsAsync()` → expired records
  2. For each: `_holdService.IsUnderHoldAsync(record.EntityId)` → check holds
  3. If held: `_recordService.HoldRecordAsync()` (update status)
  4. If not held: optionally call `IDataErasureExecutor`, then `_recordService.MarkDeletedAsync()`
  5. Publish `RetentionEnforcementCompletedNotification` with `DeletionResult`
- Remove audit recording code (events handle this automatically)
- Keep: `PeriodicTimer`, scoped resolution, alert window for expiring data

**4.5 Modify `RetentionAutoRegistrationHostedService.cs`**
- Replace `IRetentionPolicyStore` dependency with `IRetentionPolicyService`
- Policy creation: `_policyService.CreatePolicyAsync(descriptor.DataCategory, descriptor.RetentionPeriod, descriptor.AutoDelete, ...)`
- Keep: assembly scanning, descriptor creation

**4.6 Modify `RetentionFluentPolicyHostedService.cs`**
- Replace `IRetentionPolicyStore` dependency with `IRetentionPolicyService`
- Policy creation: `_policyService.CreatePolicyAsync(...)` from fluent descriptors
- Keep: `RetentionFluentPolicyDescriptor` usage

**4.7 Modify `Health/RetentionHealthCheck.cs`**
- Replace store resolution checks with service resolution checks
- Check: `IRetentionPolicyService` resolvable
- Check: `IRetentionRecordService` resolvable
- Check: `ILegalHoldService` resolvable
- Remove: `IRetentionAuditStore` check
- Optional: `IDataErasureExecutor` → Degraded if missing (keep current pattern)
- Keep: options configuration check

**4.8 Modify `RetentionOptions.cs`**
- Remove `TrackAuditTrail` property (inherent in ES — events ARE the audit trail)
- Keep all other properties: `DefaultRetentionPeriod`, `AlertBeforeExpirationDays`, `PublishNotifications`, `AddHealthCheck`, `EnableAutomaticEnforcement`, `EnforcementInterval`, `EnforcementMode`, `AutoRegisterFromAttributes`, `AssembliesToScan`, `AddPolicy()`

**4.9 Modify `RetentionErrors.cs`**
- Add: `StoreError(string operation, Exception ex)` for repository failures
- Keep existing error codes that are still relevant
- Review and remove codes referencing deleted interfaces

**4.10 Modify `.csproj`**
- Add reference: `Encina.Caching` (for `ICacheProvider`)
- Add reference: `Encina.Marten` (for `AggregateBase`, `IAggregateRepository`, `IReadModel`, `IReadModelRepository`)
- Keep: `Encina.Compliance.GDPR`, health checks, public API analyzers

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
You are implementing Phase 4 of Issue #783 — Migrate Retention to Marten event sourcing.
Phases 1-3 are complete: aggregates, events, read models, projections, service interfaces, and
default service implementations all exist.

TASK:
Create 1 new file and modify 10 existing files:

1. CREATE RetentionMartenExtensions.cs — Extension method AddRetentionAggregates() that registers
   AddAggregateRepository for all 3 aggregates and AddProjection for all 3 projections.

2. MODIFY ServiceCollectionExtensions.cs — Remove all InMemory store registrations and old service
   interfaces (IRetentionPolicy, IRetentionEnforcer, ILegalHoldManager). Add IRetentionPolicyService,
   IRetentionRecordService, ILegalHoldService (TryAddScoped).

3. MODIFY RetentionValidationPipelineBehavior.cs — Replace store dependencies with service dependencies.
   Record tracking via IRetentionRecordService.TrackEntityAsync(). Period resolution via
   IRetentionPolicyService.GetRetentionPeriodAsync(). Remove audit recording.

4. MODIFY RetentionEnforcementService.cs — Replace store dependencies with IRetentionRecordService +
   ILegalHoldService. Keep IDataErasureExecutor as optional. Use service methods for enforcement flow.

5. MODIFY RetentionAutoRegistrationHostedService.cs — Replace IRetentionPolicyStore with IRetentionPolicyService.

6. MODIFY RetentionFluentPolicyHostedService.cs — Replace IRetentionPolicyStore with IRetentionPolicyService.

7. MODIFY Health/RetentionHealthCheck.cs — Replace store checks with service checks.

8. MODIFY RetentionOptions.cs — Remove TrackAuditTrail property.

9. MODIFY RetentionErrors.cs — Add StoreError method, review error codes.

10. MODIFY .csproj — Add Encina.Caching and Encina.Marten references.

KEY RULES:
- Use TryAdd pattern for all service registrations
- Pipeline behavior must keep ConcurrentDictionary attribute caching
- Pipeline behavior must keep enforcement modes (Block/Warn/Disabled)
- Health check uses scoped resolution via IServiceProvider.CreateScope()
- Marten extensions is a SEPARATE file from ServiceCollectionExtensions
- EnforcementService keeps PeriodicTimer and DeletionResult return pattern

REFERENCE FILES:
- src/Encina.Compliance.CrossBorderTransfer/CrossBorderTransferMartenExtensions.cs
- src/Encina.Compliance.ProcessorAgreements/ServiceCollectionExtensions.cs (recently migrated)
- src/Encina.Compliance.ProcessorAgreements/ProcessorValidationPipelineBehavior.cs (pipeline after migration)
- src/Encina.Compliance.CrossBorderTransfer/Health/CrossBorderTransferHealthCheck.cs
```

</details>

---

### Phase 5: Delete Old Code & Update Satellite Packages (~75+ files deleted, ~10 satellite ServiceCollectionExtensions modified)

> **Goal**: Remove all entity-based persistence code — InMemory stores, services, entities, mappers, satellite provider stores, and their DI registrations.

<details>
<summary><strong>Tasks</strong></summary>

**5.1 Delete Core Module Files (26 files)**
- All 7 files in `Abstractions/` (IRetentionPolicyStore, IRetentionRecordStore, ILegalHoldStore, IRetentionAuditStore, IRetentionPolicy, IRetentionEnforcer, ILegalHoldManager)
- All 4 files in `InMemory/` (InMemoryRetentionPolicyStore, InMemoryRetentionRecordStore, InMemoryLegalHoldStore, InMemoryRetentionAuditStore)
- 3 service files: DefaultRetentionPolicy, DefaultRetentionEnforcer, DefaultLegalHoldManager
- 4 model files: RetentionPolicy.cs, RetentionRecord.cs, LegalHold.cs, RetentionAuditEntry.cs
- 4 entity files: RetentionRecordEntity, RetentionPolicyEntity, LegalHoldEntity, RetentionAuditEntryEntity
- 4 mapper files: RetentionRecordMapper, RetentionPolicyMapper, LegalHoldMapper, RetentionAuditEntryMapper

**5.2 Delete All Satellite Provider Stores (49 files)**
- All Retention files in `Encina.ADO.{Sqlite,SqlServer,PostgreSQL,MySQL}` (16 files)
- All Retention files in `Encina.Dapper.{Sqlite,SqlServer,PostgreSQL,MySQL}` (16 files)
- All Retention files in `Encina.EntityFrameworkCore` (9 files)
- All Retention files in `Encina.MongoDB` (8 files)

**5.3 Update Satellite ServiceCollectionExtensions (10 files)**
- Remove Retention store registrations from each satellite package:
  - `Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs`
  - `Encina.ADO.{Sqlite,SqlServer,PostgreSQL,MySQL}/ServiceCollectionExtensions.cs` (4)
  - `Encina.Dapper.{Sqlite,SqlServer,PostgreSQL,MySQL}/ServiceCollectionExtensions.cs` (4)
  - `Encina.MongoDB/ServiceCollectionExtensions.cs`

**5.4 Update PublicAPI Files**
- Move deleted public symbols from `PublicAPI.Unshipped.txt` (or Shipped.txt) to reflect removal
- Add new public symbols for aggregates, events, read models, projections, services, Marten extensions

**5.5 Verify Build**
- `dotnet build src/Encina.Compliance.Retention/` → 0 errors
- Fix any broken references from satellite packages

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
You are implementing Phase 5 of Issue #783 — Migrate Retention to Marten event sourcing.
Phases 1-4 are complete: new aggregates, events, read models, projections, services, and wiring
are all in place. Now we need to delete the old entity-based code.

TASK:
1. DELETE all files listed in the plan's "Files to DELETE" section (26 core + 49 satellite files)
2. UPDATE ServiceCollectionExtensions in ALL 10 satellite packages — remove Retention
   store registrations (IRetentionPolicyStore, IRetentionRecordStore, ILegalHoldStore,
   IRetentionAuditStore replacements)
3. UPDATE PublicAPI.Shipped.txt and PublicAPI.Unshipped.txt — remove deleted symbols, add new ones
4. VERIFY: run `dotnet build src/Encina.Compliance.Retention/` to confirm no broken references

KEY RULES:
- Delete entire Retention/ subdirectories in satellite packages
- In satellite ServiceCollectionExtensions: search for "RetentionPolicyStore", "RetentionRecordStore",
  "LegalHoldStore", "RetentionAuditStore" registrations and remove them
- Do NOT delete Model/RetentionStatus.cs, Model/RetentionPolicyType.cs, Model/DeletionResult.cs,
  Model/DeletionDetail.cs, Model/DeletionOutcome.cs, Model/ExpiringData.cs,
  Model/RetentionEnforcementMode.cs — these are value objects still used
- Do NOT delete Notifications/ — still published by services
- Do NOT delete Attributes/RetentionPeriodAttribute.cs — still used by pipeline behavior
- Do NOT delete RetentionAutoRegistrationDescriptor, RetentionFluentPolicyDescriptor — still used
- After deleting, fix any compilation errors (missing usings, broken references)

REFERENCE FILES:
- Recent git history: see how ProcessorAgreements (#782), DPIA (#781) handled satellite deletion
- src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs (to find registration patterns)
```

</details>

---

### Phase 6: Observability, Testing & Documentation (~18 test files created, ~45 deleted, ~5 modified, ~5 production files modified)

> **Goal**: Update diagnostics, create/update all test files, update documentation.

<details>
<summary><strong>Tasks</strong></summary>

**6.1 Modify `Diagnostics/RetentionDiagnostics.cs`**
- Add service-level counters:
  - `retention.policies.created` (Counter) — Policies created via service
  - `retention.policies.updated` (Counter) — Policies updated
  - `retention.policies.deactivated` (Counter) — Policies deactivated
  - `retention.records.tracked` (Counter) — Records tracked via service
  - `retention.records.expired` (Counter) — Records marked expired
  - `retention.records.deleted.total` — (keep existing, used by service)
  - `retention.records.anonymized` (Counter) — Records anonymized
  - `retention.legal_holds.placed` (Counter) — Holds placed via service
  - `retention.legal_holds.lifted` (Counter) — Holds lifted via service
- Keep existing pipeline and enforcement counters

**6.2 Modify `Diagnostics/RetentionLogMessages.cs`**
- Add service-level log messages using EventId range 8570-8599 (unused within Retention's 8500-8599 range):
  - `PolicyCreated` (8570), `PolicyUpdated` (8571), `PolicyDeactivated` (8572)
  - `RecordTracked` (8575), `RecordExpired` (8576), `RecordDeleted` (8577), `RecordAnonymized` (8578)
  - `HoldPlaced` (8580), `HoldLifted` (8581)
  - `CacheHit` (8585), `CacheMiss` (8586), `CacheInvalidated` (8587)
  - `ServiceStoreError` (8590)
- Keep all existing pipeline and enforcement log messages (8500-8569)

**6.3 Update Unit Tests**
- **Delete tests for removed classes** (~19 files):
  - InMemory store tests (4): InMemoryRetentionRecordStoreTests, InMemoryRetentionPolicyStoreTests, InMemoryRetentionAuditStoreTests, InMemoryLegalHoldStoreTests
  - Service tests (3): DefaultRetentionPolicyTests, DefaultRetentionEnforcerTests, DefaultLegalHoldManagerTests
  - Model tests (4): RetentionRecordTests, RetentionPolicyTests, LegalHoldTests, RetentionAuditEntryTests
  - Mapper tests (4): RetentionRecordMapperTests, RetentionPolicyMapperTests, LegalHoldMapperTests, RetentionAuditEntryMapperTests
- **Create new tests** (~8 files):
  - `RetentionPolicyAggregateTests.cs` — factory, commands, Apply, state transitions, invariants
  - `RetentionRecordAggregateTests.cs` — factory, commands, state machine, guards
  - `LegalHoldAggregateTests.cs` — factory, Lift, guards
  - `RetentionPolicyProjectionTests.cs` — Create and all Apply handlers
  - `RetentionRecordProjectionTests.cs` — Create and all Apply handlers (status mapping)
  - `LegalHoldProjectionTests.cs` — Create and Apply
  - `DefaultRetentionPolicyServiceTests.cs` — mock IAggregateRepository, IReadModelRepository, ICacheProvider
  - `DefaultRetentionRecordServiceTests.cs` — mock repos, test TrackEntityAsync, GetExpiredRecordsAsync
  - `DefaultLegalHoldServiceTests.cs` — mock repos + IRetentionRecordService, test cross-aggregate coordination
- **Modify existing tests** (~3 files):
  - `RetentionValidationPipelineBehaviorTests.cs` → replace store mocks with service mocks
  - `RetentionOptionsTests.cs` → remove TrackAuditTrail test
  - `RetentionErrorsTests.cs` → update for new/removed error codes

**6.4 Update Guard Tests**
- Delete guard tests for removed classes (~12 files): InMemory stores (4), DefaultRetentionPolicy, DefaultRetentionEnforcer, DefaultLegalHoldManager, mappers (4)
- Create guard tests for new classes (~6 files): 3 aggregates, 3 services

**6.5 Update Property Tests**
- Delete property tests for removed models (~5 files): RetentionRecordPropertyTests, RetentionPolicyPropertyTests, LegalHoldPropertyTests, InMemoryRetentionRecordStorePropertyTests, RetentionRecordMapperPropertyTests
- Create property tests for aggregates (~3 files): RetentionPolicyAggregatePropertyTests, RetentionRecordAggregatePropertyTests, LegalHoldAggregatePropertyTests

**6.6 Update Contract Tests**
- Delete contract tests for removed interfaces (~8 files): all IRetentionPolicyStore, IRetentionRecordStore, ILegalHoldStore, IRetentionAuditStore contract tests + InMemory variants

**6.7 Update Integration Tests**
- Delete: `RetentionPipelineIntegrationTests.cs` (depends on old stores)
- Create: Updated pipeline integration test using new services (if applicable)

**6.8 Documentation**
- Update `CHANGELOG.md`:
  ```
  ### Changed
  - Migrated `Encina.Compliance.Retention` from entity-based persistence to Marten event sourcing (#783)
  - Replaced 7 abstractions (IRetentionPolicyStore, IRetentionRecordStore, ILegalHoldStore,
    IRetentionAuditStore, IRetentionPolicy, IRetentionEnforcer, ILegalHoldManager) with 3 services
    (IRetentionPolicyService, IRetentionRecordService, ILegalHoldService)
  - Removed 13 satellite provider implementations (ADO.NET ×4, Dapper ×4, EF Core, MongoDB)
  - Event stream provides immutable GDPR Art. 5(2) audit trail

  ### Removed
  - All `IRetention*Store` interfaces and InMemory implementations
  - All `Default*` service implementations (DefaultRetentionPolicy, DefaultRetentionEnforcer, DefaultLegalHoldManager)
  - All persistence entities and mappers
  - `RetentionOptions.TrackAuditTrail` property (inherent in event sourcing)
  ```
- Update `docs/INVENTORY.md` — reflect new file structure
- Update package README if it references old interfaces
- Build verification: `dotnet build --configuration Release` → 0 errors, 0 warnings
- Test verification: `dotnet test` → all pass

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
CONTEXT:
You are implementing Phase 6 of Issue #783 — Migrate Retention to Marten event sourcing.
Phases 1-5 are complete: all new code is in place, old code is deleted, build succeeds.

TASK:
1. UPDATE Diagnostics/RetentionDiagnostics.cs — Add service-level counters for policy, record,
   and hold operations (retention.policies.created, retention.records.tracked, etc.)

2. UPDATE Diagnostics/RetentionLogMessages.cs — Add log messages in 8570-8599 range for service
   operations (PolicyCreated, RecordTracked, HoldPlaced, CacheHit, etc.)

3. DELETE obsolete test files:
   - tests/Encina.UnitTests/Compliance/Retention/ — InMemory*Tests, Default*Tests, *MapperTests,
     RetentionRecordTests, RetentionPolicyTests, LegalHoldTests, RetentionAuditEntryTests
   - tests/Encina.GuardTests/Compliance/Retention/ — InMemory*, Default*, *MapperGuardTests
   - tests/Encina.ContractTests/Compliance/Retention/ — All files
   - tests/Encina.PropertyTests/Compliance/Retention/ — Retention*PropertyTests, LegalHold*,
     InMemory*, RetentionRecordMapper*

4. CREATE new test files:
   - Unit tests: RetentionPolicyAggregateTests, RetentionRecordAggregateTests, LegalHoldAggregateTests,
     RetentionPolicyProjectionTests, RetentionRecordProjectionTests, LegalHoldProjectionTests,
     DefaultRetentionPolicyServiceTests, DefaultRetentionRecordServiceTests, DefaultLegalHoldServiceTests
   - Guard tests: RetentionPolicyAggregateGuardTests, RetentionRecordAggregateGuardTests,
     LegalHoldAggregateGuardTests, DefaultRetentionPolicyServiceGuardTests,
     DefaultRetentionRecordServiceGuardTests, DefaultLegalHoldServiceGuardTests
   - Property tests: RetentionPolicyAggregatePropertyTests, RetentionRecordAggregatePropertyTests,
     LegalHoldAggregatePropertyTests

5. MODIFY existing test files:
   - RetentionValidationPipelineBehaviorTests — store mocks → service mocks
   - RetentionOptionsTests — remove TrackAuditTrail test
   - RetentionErrorsTests — update for new/removed error codes

6. UPDATE CHANGELOG.md, docs/INVENTORY.md

7. VERIFY: dotnet build --configuration Release && dotnet test

KEY RULES:
- Unit tests use NSubstitute for mocking, FluentAssertions for assertions
- Guard tests use Shouldly for assertions
- Property tests use FsCheck with [Property(MaxTest = 50)]
- EventId range 8570-8599 for new service log messages
- Test files follow AAA pattern, deterministic, independent
- Aggregate tests: use helper methods for each lifecycle state (CreateTrackedAggregate, etc.)

REFERENCE FILES:
- tests/Encina.UnitTests/Compliance/BreachNotification/ (aggregate test patterns)
- tests/Encina.UnitTests/Compliance/ProcessorAgreements/ (service test patterns)
- tests/Encina.GuardTests/Compliance/CrossBorderTransfer/ (guard test patterns)
- tests/Encina.PropertyTests/Compliance/CrossBorderTransfer/ (property test patterns)
```

</details>

---

## Combined AI Agent Prompt

<details>
<summary><strong>Complete prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 library implementing CQRS, event sourcing, and GDPR compliance patterns.
You are migrating Encina.Compliance.Retention from entity-based persistence (13 database providers)
to Marten event sourcing. This is Issue #783.

Seven compliance modules have already been migrated: Consent (#777), DataSubjectRights (#778),
LawfulBasis (#779), BreachNotification (#780), DPIA (#781), ProcessorAgreements (#782),
and CrossBorderTransfer (#412, reference). Follow the EXACT patterns established by these migrations.

IMPLEMENTATION OVERVIEW:

Phase 1 — Create Events & Aggregates:
- Events/RetentionPolicyEvents.cs: 3 events (RetentionPolicyCreated, RetentionPolicyUpdated,
  RetentionPolicyDeactivated)
- Events/RetentionRecordEvents.cs: 6 events (RetentionRecordTracked, RetentionRecordExpired,
  RetentionRecordHeld, RetentionRecordReleased, DataDeleted, DataAnonymized)
- Events/LegalHoldEvents.cs: 2 events (LegalHoldPlaced, LegalHoldLifted)
- Aggregates/RetentionPolicyAggregate.cs: extends AggregateBase, factory Create(), commands Update/Deactivate
- Aggregates/RetentionRecordAggregate.cs: extends AggregateBase, factory Track(), commands
  MarkExpired/Hold/Release/MarkDeleted/MarkAnonymized (state machine with guards)
- Aggregates/LegalHoldAggregate.cs: extends AggregateBase, factory Place(), command Lift

Phase 2 — Create Read Models & Projections:
- ReadModels/RetentionPolicyReadModel.cs: IReadModel, mirrors aggregate state
- ReadModels/RetentionPolicyProjection.cs: IProjection<RetentionPolicyReadModel>
- ReadModels/RetentionRecordReadModel.cs: IReadModel, includes Status and hold tracking
- ReadModels/RetentionRecordProjection.cs: IProjection<RetentionRecordReadModel>
- ReadModels/LegalHoldReadModel.cs: IReadModel, includes IsActive
- ReadModels/LegalHoldProjection.cs: IProjection<LegalHoldReadModel>

Phase 3 — Create Services:
- Abstractions/IRetentionPolicyService.cs: 3 commands + 5 queries
- Abstractions/IRetentionRecordService.cs: 6 commands + 6 queries
- Abstractions/ILegalHoldService.cs: 2 commands + 5 queries
- Services/DefaultRetentionPolicyService.cs: aggregate + read model + cache + logging
- Services/DefaultRetentionRecordService.cs: aggregate + read model + cache + logging
- Services/DefaultLegalHoldService.cs: aggregate + read model + IRetentionRecordService + cache + logging

Phase 4 — Wiring:
- RetentionMartenExtensions.cs: register 3 aggregates and 3 projections
- Update ServiceCollectionExtensions.cs: replace 7 InMemory/default registrations with 3 services
- Update RetentionValidationPipelineBehavior: stores → services
- Update RetentionEnforcementService: stores → services, keep IDataErasureExecutor
- Update RetentionAutoRegistrationHostedService: store → IRetentionPolicyService
- Update RetentionFluentPolicyHostedService: store → IRetentionPolicyService
- Update HealthCheck: store checks → service checks
- Update Options: remove TrackAuditTrail
- Update Errors: add StoreError
- Update .csproj: add Encina.Caching and Encina.Marten references

Phase 5 — Delete Old Code:
- Delete 26 core files (7 interfaces, 4 InMemory stores, 3 services, 4 models, 4 entities, 4 mappers)
- Delete 49 satellite provider files (ADO×16, Dapper×16, EF×9, MongoDB×8)
- Update 10 satellite ServiceCollectionExtensions
- Update PublicAPI files

Phase 6 — Observability, Testing & Documentation:
- Add service counters to Diagnostics (EventId 8570-8599)
- Delete ~45 obsolete test files, create ~18 new test files, modify ~5 test files
- Update CHANGELOG.md, INVENTORY.md

KEY PATTERNS:
- All events: sealed record implementing INotification, include OccurredAtUtc (or domain-specific timestamp)
- Aggregates: extend AggregateBase, static factory, instance commands, Apply(object) switch
- Services: IAggregateRepository<T> + IReadModelRepository<T> + ICacheProvider
- ROP: Either<EncinaError, T> on all service methods
- Cache keys: "ret:{entity}:{id}" prefix, 5 min TTL
- Guard clauses: ArgumentNullException.ThrowIfNull, ArgumentException.ThrowIfNullOrWhiteSpace
- .NET 10 / C# 14, nullable enabled, XML docs on all public APIs
- Cross-aggregate coordination: ILegalHoldService calls IRetentionRecordService for hold/release propagation

REFERENCE FILES:
- src/Encina.Compliance.CrossBorderTransfer/ (complete reference implementation)
- src/Encina.Compliance.BreachNotification/ (recently migrated, single aggregate)
- src/Encina.Compliance.ProcessorAgreements/ (most recent migration, two aggregates)
- src/Encina.Compliance.Retention/ (current code to migrate)
- docs/plans/processor-agreements-es-migration-plan-782.md (plan format reference)
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Article | Relevance |
|----------|---------|-----------|
| GDPR Art. 5(1)(e) | Storage limitation | Core principle — data must not be kept longer than necessary |
| GDPR Art. 5(2) | Accountability | Event stream provides immutable proof of retention policy execution |
| GDPR Art. 17(1) | Right to erasure | Enforcement service executes data deletion |
| GDPR Art. 17(3)(e) | Erasure exceptions | Legal hold implementation — "for the establishment, exercise or defence of legal claims" |
| GDPR Recital 39 | Periodic review | ExpiringData alerts enable proactive retention review |
| GDPR Art. 30(1)(f) | Records of processing — time limits | Policy definitions track retention periods per data category |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `AggregateBase` | `Encina.Marten` | Base class for all 3 aggregates |
| `IAggregateRepository<T>` | `Encina.Marten` | Persistence of aggregates |
| `IReadModel` | `Encina.Marten` | Base interface for read models |
| `IReadModelRepository<T>` | `Encina.Marten` | Query read models |
| `IProjection<T>` | `Encina.Marten` | Projection interface |
| `ICacheProvider` | `Encina.Caching` | Cache-aside pattern in services |
| `INotification` | `Encina` | Event publishing via EventPublishingPipelineBehavior |
| `Either<L, R>` | `Encina` | ROP error handling |
| `EncinaError` | `Encina` | Error type |
| `IPipelineBehavior<,>` | `Encina` | Pipeline enforcement |
| `IDataErasureExecutor` | `Encina.Compliance.DataSubjectRights` | Optional physical deletion (degraded mode if absent) |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| GDPR | 8100-8220 | |
| Consent | 8260-8299 | |
| DataSubjectRights | 8300-8399 | Migrated to ES |
| Anonymization | 8400-8499 | |
| **Retention** | **8500-8599** | **Current: 8500-8569. New service msgs: 8570-8599** |
| DataResidency | 8600-8699 | |
| BreachNotification | 8700-8799 | Migrated to ES |
| DPIA | 8800-8899 | Migrated to ES |
| ProcessorAgreements | 8900-8999 | Migrated to ES |

### Estimated File Count

| Category | Created | Modified | Deleted |
|----------|---------|----------|---------|
| Events | 3 | 0 | 0 |
| Aggregates | 3 | 0 | 0 |
| Read Models + Projections | 6 | 0 | 0 |
| Services + Abstractions | 6 | 0 | 0 |
| Marten Extensions | 1 | 0 | 0 |
| Core modifications | 0 | 11 | 26 |
| Satellite deletions | 0 | 10 | 49 |
| Tests (new) | ~18 | ~5 | ~45 |
| Documentation | 0 | 2 | 0 |
| **Total** | **~37** | **~28** | **~120** |

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Include | `ICacheProvider` in all 3 services for read model caching (cache-aside pattern with "ret:" prefix) |
| 2 | OpenTelemetry | ✅ Include | Existing `ActivitySource` + `Meter` in `RetentionDiagnostics`; add service-level counters for policy/record/hold operations |
| 3 | Structured Logging | ✅ Include | Existing `RetentionLogMessages`; add service operation messages in 8570-8599 range |
| 4 | Health Checks | ✅ Include | Update existing `RetentionHealthCheck` to check services instead of stores; degraded if `IDataErasureExecutor` absent |
| 5 | Validation | ✅ Include | `RetentionOptionsValidator` preserved; aggregate guard clauses validate state transitions |
| 6 | Resilience | ❌ N/A | Marten handles PostgreSQL connection resilience internally |
| 7 | Distributed Locks | ⏭️ Defer | `RetentionEnforcementService` (BackgroundService) should use `IDistributedLockProvider` in multi-instance deployments to prevent concurrent enforcement runs. Create separate issue. |
| 8 | Transactions | ✅ Include | Handled by Marten `IDocumentSession.SaveChangesAsync()` — no separate IUnitOfWork needed |
| 9 | Idempotency | ✅ Include | Marten optimistic concurrency via aggregate `Version` prevents duplicate operations |
| 10 | Multi-Tenancy | ✅ Include | `TenantId` on all events and aggregates; propagated through services |
| 11 | Module Isolation | ✅ Include | `ModuleId` on all events and aggregates; propagated through services |
| 12 | Audit Trail | ✅ Include | ES events ARE the audit trail — `Get*HistoryAsync()` methods expose event streams per aggregate |

---

## Components Preserved (Unchanged or Minimally Modified)

| Component | Action | Reason |
|-----------|--------|--------|
| `RetentionPeriodAttribute` | **Unchanged** | Attribute-based metadata, no persistence dependency |
| `RetentionStatus` | **Unchanged** | Enum used by aggregate and read model |
| `RetentionPolicyType` | **Unchanged** | Enum used by aggregate state |
| `RetentionEnforcementMode` | **Unchanged** | Enum for pipeline behavior modes |
| `DeletionResult` + `DeletionDetail` + `DeletionOutcome` | **Unchanged** | DTOs returned by enforcement service |
| `ExpiringData` | **Unchanged** | DTO for expiration alerts |
| All 5 Notifications | **Unchanged** | Published by services and enforcement, consumed by handlers |
| `RetentionAutoRegistrationDescriptor` | **Unchanged** | Value object for attribute scanning |
| `RetentionFluentPolicyDescriptor` | **Unchanged** | Value object for fluent policy config |
| `RetentionOptionsValidator` | **Unchanged** | Options validation |
