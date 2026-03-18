# [REFACTOR] Migrate DataResidency Module to Marten Event Sourcing — Implementation Plan

> **Issue**: [#784](https://github.com/dlrivada/Encina/issues/784)
> **Type**: Refactor (Event Sourcing Migration)
> **Complexity**: High — two aggregates (ResidencyPolicy + DataLocation), pipeline behavior, auto-registration, fluent policy configuration, stateless validation services to rewire, tight coupling with CrossBorderTransfer module
> **Prerequisites**: ADR-019 (#776) ✅, CrossBorderTransfer (#412) ✅, BreachNotification (#780) ✅, DPIA (#781) ✅, ProcessorAgreements (#782) ✅, Retention (#783) ✅
> **Provider Category**: Event Sourcing (Marten) — replaces 13 database providers

---

## Summary

Migrate `Encina.Compliance.DataResidency` from entity-based persistence with 13 database provider implementations to Marten event sourcing. The module implements GDPR Chapter V (Articles 44–49) data sovereignty enforcement through region-based residency policies, data location tracking, cross-border transfer validation, and sovereignty violation detection.

**Key transformation**:
- `ResidencyPolicyDescriptor` (sealed record) → `ResidencyPolicyAggregate` (event-sourced aggregate)
- `DataLocation` (sealed record) → `DataLocationAggregate` (event-sourced aggregate)
- `IResidencyPolicyStore` + `IDataLocationStore` + `IResidencyAuditStore` (13 provider implementations each) → `IAggregateRepository<T>` (Marten)
- `IDataResidencyPolicy` + `DefaultDataResidencyPolicy` → absorbed into `IResidencyPolicyService`
- `DefaultCrossBorderTransferValidator` + `DefaultRegionRouter` → rewired to use `IResidencyPolicyService`
- Audit trail moves from explicit `IResidencyAuditStore` to implicit event streams (events ARE the audit trail)

**Estimated scope**:
- ~14 new files created (aggregates, events, read models, projections, services, abstractions, Marten extensions)
- ~39 satellite provider files deleted (stores across 13 providers)
- ~18 core files deleted (interfaces, InMemory stores, services, entities, mappers, models)
- ~10 core files modified (pipeline behavior, hosted services, DI, diagnostics, health check, options, errors, validator, router)
- ~30 test files deleted, ~16 created, ~5 modified
- ~1,800 lines of new/modified production code + ~1,200 lines of tests

**Affected packages**: `Encina.Compliance.DataResidency` (core restructuring), 10 satellite packages (remove DataResidency stores), 5 test projects

---

## Design Choices

<details>
<summary><strong>1. Aggregate Granularity — Two Aggregates Matching Domain Lifecycles</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Two aggregates: `ResidencyPolicyAggregate` + `DataLocationAggregate`** | Matches domain lifecycles — policies are long-lived and infrequently changed, locations are entity-level with independent lifecycle; mirrors issue proposal | Sovereignty violations recorded on DataLocation aggregate require cross-referencing policy |
| **B) Three aggregates: add separate `SovereigntyViolationAggregate`** | Clean violation tracking in isolation | Violations are inherently about a specific data location — separating creates coordination overhead without benefit |
| **C) Single `DataResidencyAggregate`** | Simple | Unbounded growth — one policy may govern thousands of locations |

### Selected Option
**A) Two aggregates** — `ResidencyPolicyAggregate` manages data category policies (allowed regions, adequacy requirements, legal transfer bases). `DataLocationAggregate` tracks individual entity data locations (register, migrate, verify, detect/resolve violations).

### Rationale
The domain has two distinct lifecycles: (1) policies define where data categories are allowed — they change rarely and are shared across many entities, (2) data locations track where specific entities are stored — they change frequently (migrations, verifications). Sovereignty violations are events *on* a location (a specific entity's data was found in the wrong region), so they belong to `DataLocationAggregate`. This is analogous to Retention having `RetentionPolicyAggregate` (long-lived definitions) and `RetentionRecordAggregate` (per-entity tracking).

</details>

<details>
<summary><strong>2. Service Interface Design — Two Services + Preserved Stateless Services</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Two services: `IResidencyPolicyService` + `IDataLocationService`; preserve `ICrossBorderTransferValidator`, `IRegionContextProvider`, `IAdequacyDecisionProvider`, `IRegionRouter`** | One service per aggregate; absorbs `IDataResidencyPolicy` evaluation logic; preserves stateless services that don't depend on persistence | Breaking change to `IDataResidencyPolicy` consumers (pre-1.0, acceptable) |
| **B) Single `IResidencyService`** (issue proposal) | One entry point | Too many responsibilities; mixes policy CRUD with location tracking |
| **C) Keep all existing interfaces as facades** | No API change | Leaks entity-based abstractions; inconsistent with 7 other migrated modules |

### Selected Option
**A) Two services** — `IResidencyPolicyService` absorbs `IResidencyPolicyStore` CRUD + `IDataResidencyPolicy` evaluation methods (IsAllowed, GetAllowedRegions). `IDataLocationService` absorbs `IDataLocationStore` operations. `ICrossBorderTransferValidator`, `IRegionContextProvider`, `IAdequacyDecisionProvider` are preserved as stateless services (no persistence dependency). `DefaultCrossBorderTransferValidator` and `DefaultRegionRouter` are rewired to use `IResidencyPolicyService`.

### Rationale
Consistent with all previously migrated modules — one service per aggregate. The stateless validation services (`ICrossBorderTransferValidator`, `IRegionContextProvider`, `IAdequacyDecisionProvider`, `IRegionRouter`) perform pure computation or context resolution — they don't own state and don't need aggregate-based rewiring. Only `DefaultCrossBorderTransferValidator` and `DefaultRegionRouter` reference `IResidencyPolicyStore`/`IDataResidencyPolicy` and need rewiring to the new service.

</details>

<details>
<summary><strong>3. Audit Trail Strategy — ES Events ARE the Audit Trail</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) ES events as audit trail** | Zero extra storage; immutable by design; events carry richer data than `ResidencyAuditEntry`; structured logging + OTel cover pipeline checks | Pipeline policy checks without state changes are not in ES (covered by logging/OTel) |
| **B) Keep separate `IResidencyAuditStore`** | Familiar API; captures pipeline checks | Redundant with ES events; two sources of truth |
| **C) Separate `AuditEntryAggregate` for pipeline checks** | All checks in ES | Massive event stream growth from every pipeline execution; poor ES fit |

### Selected Option
**A) ES events as audit trail** — `ResidencyAuditEntry`, `IResidencyAuditStore`, and `InMemoryResidencyAuditStore` are all deleted. State-changing operations (policy CRUD, location register/migrate/verify/remove, violation detect/resolve) are captured as ES events. Pipeline policy checks (allow/block/warn without state change) are captured by structured logging (EventId 8600-8609) and OpenTelemetry traces — these already provide full observability.

### Rationale
Every migrated compliance module uses this pattern. Event sourcing inherently satisfies GDPR Art. 5(2) accountability: every state change is an immutable event with timestamp and metadata. The `ResidencyAuditEntry` actions (PolicyCheck, CrossBorderTransfer, LocationRecord, Violation) map to either ES events or pipeline logging.

</details>

<details>
<summary><strong>4. Pipeline Behavior — Rewired to Services, Audit Recording Removed</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Pipeline calls `IResidencyPolicyService` + `IDataLocationService`; removes audit recording** | Clean; services handle aggregate lifecycle, caching, logging; audit inherent in ES | Fire-and-forget pattern for location recording still works via service |
| **B) Pipeline calls `IAggregateRepository` directly** | No service overhead | Couples pipeline to Marten; bypasses caching and observability |
| **C) Remove pipeline, require explicit calls** | Simpler pipeline | Loses attribute-based enforcement (key DX feature) |

### Selected Option
**A) Pipeline calls services** — `DataResidencyPipelineBehavior` replaces `IDataResidencyPolicy` with `IResidencyPolicyService` (for `IsAllowedAsync`, `GetAllowedRegionsAsync`), replaces `IDataLocationStore` with `IDataLocationService` (for `RegisterLocationAsync`), and removes `IResidencyAuditStore` dependency entirely. Attribute caching, enforcement modes, and `ICrossBorderTransferValidator` usage are preserved unchanged.

### Rationale
The pipeline's attribute-based enforcement (`[DataResidency]`, `[NoCrossBorderTransfer]`) is a key DX feature. Rewiring to services maintains this while routing through the standardized ES path with proper observability. Audit recording removal is justified because: (1) pipeline logging already captures all policy check outcomes at EventId 8600-8609, (2) location recording via service creates ES events, (3) removing `TrackAuditTrail` eliminates redundant configuration.

</details>

<details>
<summary><strong>5. Stateless Services — Preserved and Rewired</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Preserve `ICrossBorderTransferValidator`, `IRegionContextProvider`, `IAdequacyDecisionProvider`, `IRegionRouter`; rewire store-dependent implementations** | Minimal disruption; these interfaces define behavior, not persistence; `IRegionContextProvider` and `IAdequacyDecisionProvider` have zero store dependency | `DefaultCrossBorderTransferValidator` and `DefaultRegionRouter` need modification |
| **B) Absorb all into `IResidencyPolicyService`** | Single entry point | Bloated interface; mixes persistence concerns with stateless computation; `IRegionContextProvider` has nothing to do with policies |
| **C) Delete all, replace with aggregate-only approach** | Clean ES purity | Loses specialized validation logic (adequacy decisions, transfer basis evaluation) |

### Selected Option
**A) Preserve and rewire** — `IRegionContextProvider` (no changes needed, zero store dependency), `IAdequacyDecisionProvider` (no changes needed, static data), `ICrossBorderTransferValidator` (interface unchanged, `DefaultCrossBorderTransferValidator` rewired from `IResidencyPolicyStore` to `IResidencyPolicyService`), `IRegionRouter` (interface unchanged, `DefaultRegionRouter` rewired from `IDataResidencyPolicy` to `IResidencyPolicyService`).

### Rationale
These interfaces define domain capabilities independent of persistence: region resolution is about HTTP context, adequacy decisions are about static EU Commission data, cross-border validation is about legal basis evaluation, routing is about policy-based region selection. Only their implementations that happened to call stores need rewiring.

</details>

---

## Architecture & Structure

### Target Directory Structure

```
src/Encina.Compliance.DataResidency/
├── Abstractions/
│   ├── IResidencyPolicyService.cs              ← NEW (policy lifecycle + evaluation)
│   ├── IDataLocationService.cs                 ← NEW (location lifecycle)
│   ├── ICrossBorderTransferValidator.cs        ← PRESERVED
│   ├── IRegionContextProvider.cs               ← PRESERVED
│   ├── IAdequacyDecisionProvider.cs            ← PRESERVED
│   └── IRegionRouter.cs                        ← PRESERVED
├── Aggregates/
│   ├── ResidencyPolicyAggregate.cs             ← NEW (event-sourced aggregate)
│   └── DataLocationAggregate.cs                ← NEW (event-sourced aggregate)
├── Attributes/
│   ├── DataResidencyAttribute.cs               ← PRESERVED
│   ├── DataResidencyAttributeInfo.cs           ← PRESERVED
│   ├── NoCrossBorderTransferAttribute.cs       ← PRESERVED
│   └── NoCrossBorderTransferInfo.cs            ← PRESERVED
├── Diagnostics/
│   ├── DataResidencyDiagnostics.cs             ← MODIFIED (add service counters)
│   └── DataResidencyLogMessages.cs             ← MODIFIED (add service log messages)
├── Events/
│   ├── ResidencyPolicyEvents.cs                ← NEW (policy domain events)
│   └── DataLocationEvents.cs                   ← NEW (location domain events)
├── Health/
│   └── DataResidencyHealthCheck.cs             ← MODIFIED (check services, not stores)
├── Model/                                      ← PRESERVED (value objects, enums)
│   ├── Region.cs                               ← PRESERVED
│   ├── RegionRegistry.cs                       ← PRESERVED
│   ├── RegionGroup.cs                          ← PRESERVED
│   ├── StorageType.cs                          ← PRESERVED
│   ├── ResidencyAction.cs                      ← PRESERVED
│   ├── ResidencyOutcome.cs                     ← PRESERVED
│   ├── TransferLegalBasis.cs                   ← PRESERVED
│   ├── DataProtectionLevel.cs                  ← PRESERVED
│   └── TransferValidationResult.cs             ← PRESERVED
├── ReadModels/
│   ├── ResidencyPolicyReadModel.cs             ← NEW (Marten projection target)
│   ├── ResidencyPolicyProjection.cs            ← NEW (event → read model)
│   ├── DataLocationReadModel.cs                ← NEW (Marten projection target)
│   └── DataLocationProjection.cs               ← NEW (event → read model)
├── Services/
│   ├── DefaultResidencyPolicyService.cs        ← NEW (aggregate lifecycle orchestrator)
│   ├── DefaultDataLocationService.cs           ← NEW (aggregate lifecycle orchestrator)
│   ├── DefaultCrossBorderTransferValidator.cs  ← MODIFIED (use IResidencyPolicyService)
│   ├── DefaultRegionContextProvider.cs         ← PRESERVED
│   ├── DefaultAdequacyDecisionProvider.cs      ← PRESERVED
│   └── DefaultRegionRouter.cs                  ← MODIFIED (use IResidencyPolicyService)
├── DataResidencyErrors.cs                      ← MODIFIED (add service errors)
├── DataResidencyOptions.cs                     ← MODIFIED (remove TrackAuditTrail)
├── DataResidencyOptionsValidator.cs            ← PRESERVED
├── DataResidencyEnforcementMode.cs             ← PRESERVED
├── DataResidencyAutoRegistrationDescriptor.cs  ← PRESERVED
├── DataResidencyAutoRegistrationHostedService.cs ← MODIFIED (use IResidencyPolicyService)
├── DataResidencyFluentPolicyDescriptor.cs      ← PRESERVED
├── DataResidencyFluentPolicyHostedService.cs   ← MODIFIED (use IResidencyPolicyService)
├── DataResidencyPipelineBehavior.cs            ← MODIFIED (use services)
├── ResidencyPolicyBuilder.cs                   ← PRESERVED
├── DataResidencyMartenExtensions.cs            ← NEW (Marten aggregate registration)
├── ServiceCollectionExtensions.cs              ← MODIFIED (replace InMemory with services)
├── PublicAPI.Shipped.txt                       ← MODIFIED
└── PublicAPI.Unshipped.txt                     ← MODIFIED
```

### Files to DELETE

**Core Module (18 files)**:
- `Abstractions/IResidencyPolicyStore.cs`
- `Abstractions/IDataLocationStore.cs`
- `Abstractions/IResidencyAuditStore.cs`
- `Abstractions/IDataResidencyPolicy.cs`
- `InMemoryResidencyPolicyStore.cs` (or InMemory/)
- `InMemoryDataLocationStore.cs`
- `InMemoryResidencyAuditStore.cs`
- `DefaultDataResidencyPolicy.cs` (absorbed into service)
- `Model/DataLocation.cs` (replaced by aggregate)
- `Model/ResidencyPolicyDescriptor.cs` (replaced by aggregate)
- `Model/ResidencyAuditEntry.cs` (replaced by ES events)
- `DataLocationEntity.cs`
- `ResidencyPolicyEntity.cs`
- `ResidencyAuditEntryEntity.cs`
- `DataLocationMapper.cs`
- `ResidencyPolicyMapper.cs`
- `ResidencyAuditEntryMapper.cs`
- `DataResidencyFluentPolicyEntry.cs` (if separate file; internal DTO for fluent config — may need to update or keep if used by builder)

**Satellite Provider Stores (~39 files)**:

*ADO.NET (12 files)*:
- `src/Encina.ADO.{Sqlite,SqlServer,PostgreSQL,MySQL}/DataResidency/DataLocationStoreADO.cs` (4)
- `src/Encina.ADO.{Sqlite,SqlServer,PostgreSQL,MySQL}/DataResidency/ResidencyPolicyStoreADO.cs` (4)
- `src/Encina.ADO.{Sqlite,SqlServer,PostgreSQL,MySQL}/DataResidency/ResidencyAuditStoreADO.cs` (4)

*Dapper (12 files)*:
- `src/Encina.Dapper.{Sqlite,SqlServer,PostgreSQL,MySQL}/DataResidency/DataLocationStoreDapper.cs` (4)
- `src/Encina.Dapper.{Sqlite,SqlServer,PostgreSQL,MySQL}/DataResidency/ResidencyPolicyStoreDapper.cs` (4)
- `src/Encina.Dapper.{Sqlite,SqlServer,PostgreSQL,MySQL}/DataResidency/ResidencyAuditStoreDapper.cs` (4)

*EF Core (~6 files)*:
- `src/Encina.EntityFrameworkCore/DataResidency/DataLocationStoreEF.cs`
- `src/Encina.EntityFrameworkCore/DataResidency/ResidencyPolicyStoreEF.cs`
- `src/Encina.EntityFrameworkCore/DataResidency/ResidencyAuditStoreEF.cs`
- `src/Encina.EntityFrameworkCore/DataResidency/DataResidencyModelBuilderExtensions.cs` (if exists)
- `src/Encina.EntityFrameworkCore/DataResidency/*EntityConfiguration.cs` (if exist)

*MongoDB (~6 files)*:
- `src/Encina.MongoDB/DataResidency/DataLocationStoreMongoDB.cs`
- `src/Encina.MongoDB/DataResidency/ResidencyPolicyStoreMongoDB.cs`
- `src/Encina.MongoDB/DataResidency/ResidencyAuditStoreMongoDB.cs`
- `src/Encina.MongoDB/DataResidency/*Document.cs` (if exist)

---

## Implementation Phases (6 Phases)

| Phase | Description | Status |
|-------|-------------|--------|
| Phase 1 | Events & Aggregates | ✅ Complete |
| Phase 2 | Read Models & Projections | ✅ Complete |
| Phase 3 | Service Interfaces & Implementations | ✅ Complete |
| Phase 4 | Wiring — DI, Pipeline, Health Check, Hosted Services | ✅ Complete |
| Phase 5 | Delete Old Code & Update Satellite Packages | ✅ Complete |
| Phase 6 | Observability, Testing & Documentation | ✅ Complete |

### Phase 1: Events & Aggregates (~4 new files, ~500 lines)

> **Goal**: Create the event-sourced aggregates and domain events that model the residency policy and data location lifecycles.

<details>
<summary><strong>Tasks</strong></summary>

**1.1 Create `Events/ResidencyPolicyEvents.cs`**
- Namespace: `Encina.Compliance.DataResidency.Events`
- All events are `sealed record` implementing `INotification`
- Events:
  - `ResidencyPolicyCreated(Guid PolicyId, string DataCategory, IReadOnlyList<string> AllowedRegionCodes, bool RequireAdequacyDecision, IReadOnlyList<TransferLegalBasis> AllowedTransferBases, string? TenantId, string? ModuleId)` — initial policy creation
  - `ResidencyPolicyUpdated(Guid PolicyId, IReadOnlyList<string> AllowedRegionCodes, bool RequireAdequacyDecision, IReadOnlyList<TransferLegalBasis> AllowedTransferBases)` — policy modification
  - `ResidencyPolicyDeleted(Guid PolicyId, string Reason)` — policy soft-deletion

**1.2 Create `Events/DataLocationEvents.cs`**
- Events:
  - `DataLocationRegistered(Guid LocationId, string EntityId, string DataCategory, string RegionCode, StorageType StorageType, DateTimeOffset StoredAtUtc, IReadOnlyDictionary<string, string>? Metadata, string? TenantId, string? ModuleId)` — entity stored in a region
  - `DataLocationMigrated(Guid LocationId, string EntityId, string PreviousRegionCode, string NewRegionCode, string Reason)` — data moved between regions
  - `DataLocationVerified(Guid LocationId, DateTimeOffset VerifiedAtUtc)` — periodic residency verification
  - `DataLocationRemoved(Guid LocationId, string EntityId, string Reason)` — location tracking removed
  - `SovereigntyViolationDetected(Guid LocationId, string EntityId, string DataCategory, string ViolatingRegionCode, string Details)` — data found in non-allowed region
  - `SovereigntyViolationResolved(Guid LocationId, string EntityId, string Resolution)` — violation remediated

**1.3 Create `Aggregates/ResidencyPolicyAggregate.cs`**
- Extends `AggregateBase`
- Properties (`private set`): `DataCategory`, `AllowedRegionCodes` (IReadOnlyList<string>), `RequireAdequacyDecision`, `AllowedTransferBases` (IReadOnlyList<TransferLegalBasis>), `IsActive`, `TenantId`, `ModuleId`
- Factory: `static ResidencyPolicyAggregate Create(Guid id, string dataCategory, IReadOnlyList<string> allowedRegionCodes, bool requireAdequacyDecision, IReadOnlyList<TransferLegalBasis> allowedTransferBases, string? tenantId, string? moduleId)`
- Commands: `Update(IReadOnlyList<string> allowedRegionCodes, bool requireAdequacyDecision, IReadOnlyList<TransferLegalBasis> allowedTransferBases)`, `Delete(string reason)`
- `protected override void Apply(object domainEvent)` with switch on all 3 event types
- Guards: cannot update/delete a deleted policy

**1.4 Create `Aggregates/DataLocationAggregate.cs`**
- Extends `AggregateBase`
- Properties (`private set`): `EntityId`, `DataCategory`, `RegionCode`, `StorageType`, `StoredAtUtc`, `LastVerifiedAtUtc` (nullable), `Metadata`, `IsRemoved`, `HasViolation`, `ViolationDetails`, `TenantId`, `ModuleId`
- Factory: `static DataLocationAggregate Register(Guid id, string entityId, string dataCategory, string regionCode, StorageType storageType, DateTimeOffset storedAtUtc, IReadOnlyDictionary<string, string>? metadata, string? tenantId, string? moduleId)`
- Commands: `Migrate(string newRegionCode, string reason)`, `Verify(DateTimeOffset verifiedAtUtc)`, `Remove(string reason)`, `DetectViolation(string dataCategory, string violatingRegionCode, string details)`, `ResolveViolation(string resolution)`
- State machine guards:
  - Cannot operate on removed location (migrate, verify, detect)
  - Cannot resolve a violation that doesn't exist
  - Cannot detect a violation on an already-violated location (must resolve first)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
You are migrating the Encina.Compliance.DataResidency module from entity-based persistence to Marten event sourcing.
This follows the exact pattern established by CrossBorderTransfer, BreachNotification, DPIA, ProcessorAgreements, and Retention.

TASK:
Create 4 new files in src/Encina.Compliance.DataResidency/:

1. Events/ResidencyPolicyEvents.cs — 3 domain events as sealed records implementing INotification.
   Events: ResidencyPolicyCreated (full policy data + TenantId/ModuleId), ResidencyPolicyUpdated
   (changed fields), ResidencyPolicyDeleted (with Reason). AllowedRegionCodes as IReadOnlyList<string>,
   AllowedTransferBases as IReadOnlyList<TransferLegalBasis>.

2. Events/DataLocationEvents.cs — 6 domain events as sealed records implementing INotification.
   Events: DataLocationRegistered (entity + region + storage type + metadata + TenantId/ModuleId),
   DataLocationMigrated (previous + new region + reason), DataLocationVerified (verifiedAtUtc),
   DataLocationRemoved (entityId + reason), SovereigntyViolationDetected (entity + category + region + details),
   SovereigntyViolationResolved (entityId + resolution). RegionCode as string, StorageType as enum.

3. Aggregates/ResidencyPolicyAggregate.cs — Event-sourced aggregate extending AggregateBase.
   Properties (private set): DataCategory, AllowedRegionCodes (IReadOnlyList<string>),
   RequireAdequacyDecision, AllowedTransferBases (IReadOnlyList<TransferLegalBasis>), IsActive, TenantId, ModuleId.
   Factory: static Create(). Commands: Update(), Delete(). Apply() handles all 3 event types.
   Guard: cannot update/delete a deleted policy (throw InvalidOperationException).

4. Aggregates/DataLocationAggregate.cs — Event-sourced aggregate extending AggregateBase.
   Properties (private set): EntityId, DataCategory, RegionCode, StorageType, StoredAtUtc,
   LastVerifiedAtUtc (DateTimeOffset?), Metadata, IsRemoved, HasViolation, ViolationDetails, TenantId, ModuleId.
   Factory: static Register(). Commands: Migrate(), Verify(), Remove(), DetectViolation(), ResolveViolation().
   State machine: cannot operate on removed; cannot detect violation if already violated; cannot resolve if no violation.

KEY RULES:
- .NET 10 / C# 14, nullable enabled
- Events are sealed records implementing INotification (from Encina core, NOT MediatR)
- Events use primitive types: string for region codes, enum for StorageType/TransferLegalBasis
- Aggregate validates state transitions (throw InvalidOperationException for invalid transitions)
- Factory method is static, behavior methods are instance
- Apply method uses switch on domainEvent type
- XML documentation on all public types with GDPR article references (Art. 44-49, 5(2), 30)
- Guard clauses: ArgumentNullException.ThrowIfNull, ArgumentException.ThrowIfNullOrWhiteSpace

REFERENCE FILES:
- src/Encina.Compliance.CrossBorderTransfer/Aggregates/ApprovedTransferAggregate.cs (aggregate pattern)
- src/Encina.Compliance.CrossBorderTransfer/Events/ApprovedTransferEvents.cs (events pattern)
- src/Encina.Compliance.Retention/Aggregates/RetentionPolicyAggregate.cs (policy aggregate)
- src/Encina.Compliance.DataResidency/Model/StorageType.cs (existing enum)
- src/Encina.Compliance.DataResidency/Model/TransferLegalBasis.cs (existing enum)
- src/Encina.Compliance.DataResidency/Model/Region.cs (Region type — use .Code for event strings)
```

</details>

---

### Phase 2: Read Models & Projections (~4 new files, ~350 lines)

> **Goal**: Create query-side read models and Marten projections for both aggregates.

<details>
<summary><strong>Tasks</strong></summary>

**2.1 Create `ReadModels/ResidencyPolicyReadModel.cs`**
- Implements `IReadModel`
- Properties (`{ get; set; }`): `Guid Id`, `string DataCategory`, `IReadOnlyList<string> AllowedRegionCodes`, `bool RequireAdequacyDecision`, `IReadOnlyList<TransferLegalBasis> AllowedTransferBases`, `bool IsActive`, `string? TenantId`, `string? ModuleId`, `DateTimeOffset LastModifiedAtUtc`, `int Version`

**2.2 Create `ReadModels/ResidencyPolicyProjection.cs`**
- Implements `IProjection<ResidencyPolicyReadModel>`, `IProjectionCreator<ResidencyPolicyCreated, ResidencyPolicyReadModel>`, `IProjectionHandler` for Updated/Deleted
- `ProjectionName => "ResidencyPolicyProjection"`
- Each handler increments `Version++` and updates `LastModifiedAtUtc`

**2.3 Create `ReadModels/DataLocationReadModel.cs`**
- Implements `IReadModel`
- Properties: `Guid Id`, `string EntityId`, `string DataCategory`, `string RegionCode`, `StorageType StorageType`, `DateTimeOffset StoredAtUtc`, `DateTimeOffset? LastVerifiedAtUtc`, `IReadOnlyDictionary<string, string>? Metadata`, `bool IsRemoved`, `bool HasViolation`, `string? ViolationDetails`, `string? TenantId`, `string? ModuleId`, `DateTimeOffset LastModifiedAtUtc`, `int Version`

**2.4 Create `ReadModels/DataLocationProjection.cs`**
- Implements `IProjection<DataLocationReadModel>`, `IProjectionCreator<DataLocationRegistered, DataLocationReadModel>`, `IProjectionHandler` for Migrated, Verified, Removed, SovereigntyViolationDetected, SovereigntyViolationResolved
- Status mapping: Registered→active, Migrated→update region, Verified→update timestamp, Removed→IsRemoved=true, ViolationDetected→HasViolation=true, ViolationResolved→HasViolation=false

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
You are implementing Phase 2 of Issue #784 — Migrate DataResidency to Marten event sourcing.
Phase 1 is complete: 2 aggregates (ResidencyPolicyAggregate, DataLocationAggregate)
and 9 domain events exist.

TASK:
Create 4 files in src/Encina.Compliance.DataResidency/ReadModels/:

1. ResidencyPolicyReadModel.cs — sealed class implementing IReadModel with mutable properties.
   Mirrors ResidencyPolicyAggregate state: DataCategory, AllowedRegionCodes, RequireAdequacyDecision,
   AllowedTransferBases, IsActive, TenantId, ModuleId, LastModifiedAtUtc, Version.

2. ResidencyPolicyProjection.cs — IProjection<ResidencyPolicyReadModel> +
   IProjectionCreator<ResidencyPolicyCreated> + IProjectionHandler for Updated, Deleted.

3. DataLocationReadModel.cs — sealed class implementing IReadModel.
   Mirrors DataLocationAggregate state: EntityId, DataCategory, RegionCode, StorageType,
   StoredAtUtc, LastVerifiedAtUtc, Metadata, IsRemoved, HasViolation, ViolationDetails,
   TenantId, ModuleId, LastModifiedAtUtc, Version.

4. DataLocationProjection.cs — IProjection<DataLocationReadModel> +
   IProjectionCreator<DataLocationRegistered> + IProjectionHandler for Migrated, Verified,
   Removed, SovereigntyViolationDetected, SovereigntyViolationResolved.

KEY RULES:
- Read models have mutable properties (get; set;) for projection updates
- Always increment Version and update LastModifiedAtUtc on each event
- ProjectionName property returns a unique string
- XML documentation on all public types
- .NET 10 / C# 14, nullable enabled

REFERENCE FILES:
- src/Encina.Compliance.Retention/ReadModels/RetentionPolicyReadModel.cs
- src/Encina.Compliance.Retention/ReadModels/RetentionPolicyProjection.cs
- src/Encina.Compliance.CrossBorderTransfer/ReadModels/ApprovedTransferReadModel.cs
```

</details>

---

### Phase 3: Service Interfaces & Implementations (~4 new files, ~800 lines)

> **Goal**: Create two service interfaces and their default implementations with aggregate lifecycle management, read model queries, caching, and observability.

<details>
<summary><strong>Tasks</strong></summary>

**3.1 Create `Abstractions/IResidencyPolicyService.cs`**
- Namespace: `Encina.Compliance.DataResidency.Abstractions`
- **Commands**:
  - `CreatePolicyAsync(string dataCategory, IReadOnlyList<string> allowedRegionCodes, bool requireAdequacyDecision, IReadOnlyList<TransferLegalBasis> allowedTransferBases, string? tenantId, string? moduleId, CancellationToken ct)` → `ValueTask<Either<EncinaError, Guid>>`
  - `UpdatePolicyAsync(Guid policyId, IReadOnlyList<string> allowedRegionCodes, bool requireAdequacyDecision, IReadOnlyList<TransferLegalBasis> allowedTransferBases, CancellationToken ct)` → `ValueTask<Either<EncinaError, Unit>>`
  - `DeletePolicyAsync(Guid policyId, string reason, CancellationToken ct)` → `ValueTask<Either<EncinaError, Unit>>`
- **Queries**:
  - `GetPolicyAsync(Guid policyId, CancellationToken ct)` → `ValueTask<Either<EncinaError, ResidencyPolicyReadModel>>`
  - `GetPolicyByCategoryAsync(string dataCategory, CancellationToken ct)` → `ValueTask<Either<EncinaError, ResidencyPolicyReadModel>>`
  - `GetAllPoliciesAsync(CancellationToken ct)` → `ValueTask<Either<EncinaError, IReadOnlyList<ResidencyPolicyReadModel>>>`
  - `GetPolicyHistoryAsync(Guid policyId, CancellationToken ct)` → `ValueTask<Either<EncinaError, IReadOnlyList<object>>>`
- **Evaluation** (absorbs `IDataResidencyPolicy`):
  - `IsAllowedAsync(string dataCategory, Region targetRegion, CancellationToken ct)` → `ValueTask<Either<EncinaError, bool>>`
  - `GetAllowedRegionsAsync(string dataCategory, CancellationToken ct)` → `ValueTask<Either<EncinaError, IReadOnlyList<Region>>>`

**3.2 Create `Abstractions/IDataLocationService.cs`**
- **Commands**:
  - `RegisterLocationAsync(string entityId, string dataCategory, Region region, StorageType storageType, IReadOnlyDictionary<string, string>? metadata, string? tenantId, string? moduleId, CancellationToken ct)` → `ValueTask<Either<EncinaError, Guid>>`
  - `MigrateLocationAsync(Guid locationId, Region newRegion, string reason, CancellationToken ct)` → `ValueTask<Either<EncinaError, Unit>>`
  - `VerifyLocationAsync(Guid locationId, CancellationToken ct)` → `ValueTask<Either<EncinaError, Unit>>`
  - `RemoveLocationAsync(Guid locationId, string reason, CancellationToken ct)` → `ValueTask<Either<EncinaError, Unit>>`
  - `RemoveByEntityAsync(string entityId, string reason, CancellationToken ct)` → `ValueTask<Either<EncinaError, Unit>>`
  - `DetectViolationAsync(Guid locationId, string dataCategory, string violatingRegionCode, string details, CancellationToken ct)` → `ValueTask<Either<EncinaError, Unit>>`
  - `ResolveViolationAsync(Guid locationId, string resolution, CancellationToken ct)` → `ValueTask<Either<EncinaError, Unit>>`
- **Queries**:
  - `GetLocationAsync(Guid locationId, CancellationToken ct)` → `ValueTask<Either<EncinaError, DataLocationReadModel>>`
  - `GetByEntityAsync(string entityId, CancellationToken ct)` → `ValueTask<Either<EncinaError, IReadOnlyList<DataLocationReadModel>>>`
  - `GetByRegionAsync(string regionCode, CancellationToken ct)` → `ValueTask<Either<EncinaError, IReadOnlyList<DataLocationReadModel>>>`
  - `GetByCategoryAsync(string dataCategory, CancellationToken ct)` → `ValueTask<Either<EncinaError, IReadOnlyList<DataLocationReadModel>>>`
  - `GetViolationsAsync(CancellationToken ct)` → `ValueTask<Either<EncinaError, IReadOnlyList<DataLocationReadModel>>>`
  - `GetLocationHistoryAsync(Guid locationId, CancellationToken ct)` → `ValueTask<Either<EncinaError, IReadOnlyList<object>>>`

**3.3 Create `Services/DefaultResidencyPolicyService.cs`**
- Dependencies: `IAggregateRepository<ResidencyPolicyAggregate>`, `IReadModelRepository<ResidencyPolicyReadModel>`, `ICacheProvider`, `IOptions<DataResidencyOptions>`, `TimeProvider`, `ILogger<DefaultResidencyPolicyService>`
- Cache keys: `"dr:policy:{id}"`, `"dr:policy:cat:{dataCategory}"`, `"dr:policies:all"`
- Command pattern: create aggregate → persist via repository → invalidate cache → record metric
- Query pattern: check cache → read from read model repo → populate cache (5 min TTL) → return
- `IsAllowedAsync`: loads policy by category, checks if region code is in AllowedRegionCodes (empty list = no restrictions = all allowed)
- `GetAllowedRegionsAsync`: loads policy by category, resolves region codes via `RegionRegistry`

**3.4 Create `Services/DefaultDataLocationService.cs`**
- Dependencies: `IAggregateRepository<DataLocationAggregate>`, `IReadModelRepository<DataLocationReadModel>`, `ICacheProvider`, `TimeProvider`, `ILogger<DefaultDataLocationService>`
- Cache keys: `"dr:location:{id}"`, `"dr:locations:entity:{entityId}"`
- `RegisterLocationAsync`: creates `DataLocationAggregate.Register()` with `Region.Code` as regionCode
- `RemoveByEntityAsync`: queries read model for entity, removes each location aggregate
- `GetViolationsAsync`: queries read model for `HasViolation == true && IsRemoved == false`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
You are implementing Phase 3 of Issue #784 — Migrate DataResidency to Marten event sourcing.
Phases 1-2 are complete: two aggregates, 9 events, two read models, and two projections exist.

TASK:
Create 4 files:

1. Abstractions/IResidencyPolicyService.cs — Interface with 3 command methods (CreatePolicyAsync,
   UpdatePolicyAsync, DeletePolicyAsync), 4 query methods (GetPolicyAsync, GetPolicyByCategoryAsync,
   GetAllPoliciesAsync, GetPolicyHistoryAsync), and 2 evaluation methods absorbed from IDataResidencyPolicy
   (IsAllowedAsync, GetAllowedRegionsAsync). All return ValueTask<Either<EncinaError, T>>.

2. Abstractions/IDataLocationService.cs — Interface with 7 command methods (RegisterLocationAsync,
   MigrateLocationAsync, VerifyLocationAsync, RemoveLocationAsync, RemoveByEntityAsync,
   DetectViolationAsync, ResolveViolationAsync) and 6 query methods (GetLocationAsync,
   GetByEntityAsync, GetByRegionAsync, GetByCategoryAsync, GetViolationsAsync, GetLocationHistoryAsync).

3. Services/DefaultResidencyPolicyService.cs — Implementation with IAggregateRepository<ResidencyPolicyAggregate>,
   IReadModelRepository<ResidencyPolicyReadModel>, ICacheProvider, IOptions<DataResidencyOptions>,
   TimeProvider, ILogger. Cache-aside with "dr:policy:{id}" keys. IsAllowedAsync checks if
   Region.Code is in AllowedRegionCodes (empty = all allowed). GetAllowedRegionsAsync resolves
   codes to Region objects via RegionRegistry.

4. Services/DefaultDataLocationService.cs — Implementation with IAggregateRepository<DataLocationAggregate>,
   IReadModelRepository<DataLocationReadModel>, ICacheProvider, TimeProvider, ILogger.
   RegisterLocationAsync uses Region.Code. RemoveByEntityAsync iterates entity locations.
   GetViolationsAsync queries HasViolation == true.

KEY RULES:
- ROP: Either<EncinaError, T> on all methods
- Cache-aside: check cache → load → cache with 5 min TTL
- Invalidate cache after writes (fire-and-forget async)
- catch InvalidOperationException → return domain error
- catch Exception (not OperationCanceledException) → return store error
- Use DataResidencyDiagnostics counters and logger extension methods
- XML docs with GDPR Art. 44-49, 5(2), 30 references

REFERENCE FILES:
- src/Encina.Compliance.CrossBorderTransfer/Services/DefaultTIAService.cs (service pattern)
- src/Encina.Compliance.CrossBorderTransfer/Abstractions/ITIAService.cs (interface pattern)
- src/Encina.Compliance.Retention/Services/DefaultRetentionPolicyService.cs (policy service)
- src/Encina.Compliance.DataResidency/DataResidencyErrors.cs (existing errors)
- src/Encina.Compliance.DataResidency/Model/Region.cs (Region type, RegionRegistry)
```

</details>

---

### Phase 4: Wiring — DI, Pipeline, Health Check, Hosted Services, Marten Extensions (~11 files modified/created, ~400 lines)

> **Goal**: Wire everything together: update DI registrations, pipeline behavior, health check, hosted services, stateless service rewiring, and create Marten extensions.

<details>
<summary><strong>Tasks</strong></summary>

**4.1 Create `DataResidencyMartenExtensions.cs`**
- `public static IServiceCollection AddDataResidencyAggregates(this IServiceCollection services)`
- Registers: `AddAggregateRepository<ResidencyPolicyAggregate>()`, `AddAggregateRepository<DataLocationAggregate>()`
- Registers: `AddProjection<ResidencyPolicyProjection, ResidencyPolicyReadModel>()`, `AddProjection<DataLocationProjection, DataLocationReadModel>()`

**4.2 Modify `ServiceCollectionExtensions.cs`**
- Remove: `TryAddSingleton<IResidencyPolicyStore, InMemoryResidencyPolicyStore>`
- Remove: `TryAddSingleton<IDataLocationStore, InMemoryDataLocationStore>`
- Remove: `TryAddSingleton<IResidencyAuditStore, InMemoryResidencyAuditStore>`
- Remove: `TryAddSingleton<IDataResidencyPolicy, DefaultDataResidencyPolicy>`
- Add: `TryAddScoped<IResidencyPolicyService, DefaultResidencyPolicyService>`
- Add: `TryAddScoped<IDataLocationService, DefaultDataLocationService>`
- Keep: `ICrossBorderTransferValidator`, `IRegionContextProvider`, `IAdequacyDecisionProvider`, `IRegionRouter` registrations
- Keep: `DataResidencyPipelineBehavior<,>` registration
- Keep: Health check, TimeProvider, auto-registration services

**4.3 Modify `DataResidencyPipelineBehavior.cs`**
- Replace `IDataResidencyPolicy` dependency with `IResidencyPolicyService`
- Replace `IDataLocationStore` dependency with `IDataLocationService`
- Remove `IResidencyAuditStore` dependency (audit is implicit in ES)
- Policy evaluation: `_policyService.IsAllowedAsync(category, region, ct)` and `_policyService.GetAllowedRegionsAsync(category, ct)`
- Location recording: `_locationService.RegisterLocationAsync(entityId, category, region, storageType, metadata, tenantId, moduleId, ct)`
- Remove `TrackAuditTrail` checks and `TryRecordAuditAsync` calls
- Keep: `ConcurrentDictionary` attribute caching, enforcement modes, `ICrossBorderTransferValidator` usage, diagnostics

**4.4 Modify `DefaultCrossBorderTransferValidator.cs`**
- Replace `IResidencyPolicyStore` dependency with `IResidencyPolicyService`
- Policy lookup: `_policyService.GetPolicyByCategoryAsync(category, ct)` instead of `_policyStore.GetByCategoryAsync(category, ct)`
- Access allowed transfer bases from `ResidencyPolicyReadModel.AllowedTransferBases`

**4.5 Modify `DefaultRegionRouter.cs`**
- Replace `IDataResidencyPolicy` dependency with `IResidencyPolicyService`
- Route resolution: `_policyService.GetAllowedRegionsAsync(category, ct)` and `_policyService.IsAllowedAsync(category, region, ct)`

**4.6 Modify `DataResidencyAutoRegistrationHostedService.cs`**
- Replace `IResidencyPolicyStore` dependency with `IResidencyPolicyService`
- Policy creation: `_policyService.CreatePolicyAsync(descriptor.DataCategory, allowedRegionCodes, ...)`

**4.7 Modify `DataResidencyFluentPolicyHostedService.cs`**
- Replace `IResidencyPolicyStore` dependency with `IResidencyPolicyService`
- Policy creation: `_policyService.CreatePolicyAsync(...)` from fluent descriptors

**4.8 Modify `Health/DataResidencyHealthCheck.cs`**
- Replace store resolution checks with service resolution checks
- Check: `IResidencyPolicyService` resolvable
- Check: `IDataLocationService` resolvable
- Remove: `IResidencyAuditStore` check
- Keep: `IRegionContextProvider`, `ICrossBorderTransferValidator` checks
- Keep: options configuration check

**4.9 Modify `DataResidencyOptions.cs`**
- Remove `TrackAuditTrail` property (inherent in ES — events ARE the audit trail)
- Keep all other properties: `DefaultRegion`, `EnforcementMode`, `TrackDataLocations`, `BlockNonCompliantTransfers`, `AddHealthCheck`, `AdditionalAdequateRegions`, `AutoRegisterFromAttributes`, `AssembliesToScan`, `AddPolicy()`

**4.10 Modify `DataResidencyErrors.cs`**
- Keep existing error codes
- Ensure `StoreError` method exists for repository failures (add if needed)
- Add error codes for new service operations if needed (e.g., `LocationNotFound`, `ViolationNotFound`)

**4.11 Modify `.csproj`**
- Add reference: `Encina.Caching` (for `ICacheProvider`)
- Add reference: `Encina.Marten` (for `AggregateBase`, `IAggregateRepository`, `IReadModel`, `IReadModelRepository`)
- Keep: `Encina` (core), health checks, hosting, logging, options, public API analyzers

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
You are implementing Phase 4 of Issue #784 — Migrate DataResidency to Marten event sourcing.
Phases 1-3 are complete: aggregates, events, read models, projections, service interfaces, and
default service implementations all exist.

TASK:
Create 1 new file and modify 10 existing files:

1. CREATE DataResidencyMartenExtensions.cs — Extension method AddDataResidencyAggregates() that
   registers AddAggregateRepository for both aggregates and AddProjection for both projections.

2. MODIFY ServiceCollectionExtensions.cs — Remove all InMemory store registrations and
   IDataResidencyPolicy/DefaultDataResidencyPolicy. Add IResidencyPolicyService and
   IDataLocationService (TryAddScoped). Keep stateless service registrations.

3. MODIFY DataResidencyPipelineBehavior.cs — Replace IDataResidencyPolicy with IResidencyPolicyService,
   IDataLocationStore with IDataLocationService, remove IResidencyAuditStore. Remove
   TrackAuditTrail checks. Keep attribute caching, enforcement modes, ICrossBorderTransferValidator.

4. MODIFY DefaultCrossBorderTransferValidator.cs — Replace IResidencyPolicyStore with
   IResidencyPolicyService. Use GetPolicyByCategoryAsync().

5. MODIFY DefaultRegionRouter.cs — Replace IDataResidencyPolicy with IResidencyPolicyService.

6. MODIFY DataResidencyAutoRegistrationHostedService.cs — Replace IResidencyPolicyStore with
   IResidencyPolicyService.

7. MODIFY DataResidencyFluentPolicyHostedService.cs — Replace IResidencyPolicyStore with
   IResidencyPolicyService.

8. MODIFY Health/DataResidencyHealthCheck.cs — Replace store checks with service checks.

9. MODIFY DataResidencyOptions.cs — Remove TrackAuditTrail property.

10. MODIFY DataResidencyErrors.cs — Add LocationNotFound, ViolationNotFound if needed.

11. MODIFY .csproj — Add Encina.Caching and Encina.Marten references.

KEY RULES:
- Use TryAdd pattern for all service registrations
- Pipeline behavior must keep ConcurrentDictionary attribute caching
- Pipeline behavior must keep enforcement modes (Block/Warn/Disabled)
- Health check uses scoped resolution via IServiceProvider.CreateScope()
- Marten extensions is a SEPARATE file from ServiceCollectionExtensions
- Stateless services (IRegionContextProvider, IAdequacyDecisionProvider) stay as-is

REFERENCE FILES:
- src/Encina.Compliance.CrossBorderTransfer/CrossBorderTransferMartenExtensions.cs
- src/Encina.Compliance.Retention/ServiceCollectionExtensions.cs (recently migrated)
- src/Encina.Compliance.Retention/RetentionValidationPipelineBehavior.cs (pipeline after migration)
- src/Encina.Compliance.CrossBorderTransfer/Health/CrossBorderTransferHealthCheck.cs
```

</details>

---

### Phase 5: Delete Old Code & Update Satellite Packages (~57+ files deleted, ~10 satellite ServiceCollectionExtensions modified)

> **Goal**: Remove all entity-based persistence code — InMemory stores, services, entities, mappers, satellite provider stores, and their DI registrations.

<details>
<summary><strong>Tasks</strong></summary>

**5.1 Delete Core Module Files (~18 files)**
- All 4 files in `Abstractions/` that are replaced: `IResidencyPolicyStore`, `IDataLocationStore`, `IResidencyAuditStore`, `IDataResidencyPolicy`
- All 3 files InMemory stores: `InMemoryResidencyPolicyStore`, `InMemoryDataLocationStore`, `InMemoryResidencyAuditStore`
- 1 service file: `DefaultDataResidencyPolicy` (absorbed into `DefaultResidencyPolicyService`)
- 3 model files: `DataLocation.cs`, `ResidencyPolicyDescriptor.cs`, `ResidencyAuditEntry.cs`
- 3 entity files: `DataLocationEntity.cs`, `ResidencyPolicyEntity.cs`, `ResidencyAuditEntryEntity.cs`
- 3 mapper files: `DataLocationMapper.cs`, `ResidencyPolicyMapper.cs`, `ResidencyAuditEntryMapper.cs`
- `DataResidencyFluentPolicyEntry.cs` (if separate file — verify and update if needed)

**5.2 Delete All Satellite Provider Stores (~39 files)**
- All DataResidency files in `Encina.ADO.{Sqlite,SqlServer,PostgreSQL,MySQL}` (12 files)
- All DataResidency files in `Encina.Dapper.{Sqlite,SqlServer,PostgreSQL,MySQL}` (12 files)
- All DataResidency files in `Encina.EntityFrameworkCore` (~6 files incl. configs)
- All DataResidency files in `Encina.MongoDB` (~6 files incl. documents)

**5.3 Update Satellite ServiceCollectionExtensions (10 files)**
- Remove DataResidency store registrations from each satellite package:
  - `Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs`
  - `Encina.ADO.{Sqlite,SqlServer,PostgreSQL,MySQL}/ServiceCollectionExtensions.cs` (4)
  - `Encina.Dapper.{Sqlite,SqlServer,PostgreSQL,MySQL}/ServiceCollectionExtensions.cs` (4)
  - `Encina.MongoDB/ServiceCollectionExtensions.cs`

**5.4 Update PublicAPI Files**
- Move deleted public symbols from `PublicAPI.Unshipped.txt` to reflect removal
- Add new public symbols for aggregates, events, read models, projections, services, Marten extensions

**5.5 Verify Build**
- `dotnet build src/Encina.Compliance.DataResidency/` → 0 errors
- Fix any broken references from satellite packages

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
You are implementing Phase 5 of Issue #784 — Migrate DataResidency to Marten event sourcing.
Phases 1-4 are complete: new aggregates, events, read models, projections, services, and wiring
are all in place. Now we need to delete the old entity-based code.

TASK:
1. DELETE all files listed in the plan's "Files to DELETE" section (~18 core + ~39 satellite files)
2. UPDATE ServiceCollectionExtensions in ALL 10 satellite packages — remove DataResidency
   store registrations (IResidencyPolicyStore, IDataLocationStore, IResidencyAuditStore replacements)
3. UPDATE PublicAPI.Shipped.txt and PublicAPI.Unshipped.txt — remove deleted symbols, add new ones
4. VERIFY: run `dotnet build src/Encina.Compliance.DataResidency/` to confirm no broken references

KEY RULES:
- Delete entire DataResidency/ subdirectories in satellite packages
- In satellite ServiceCollectionExtensions: search for "ResidencyPolicyStore", "DataLocationStore",
  "ResidencyAuditStore" registrations and remove them
- Do NOT delete Model/Region.cs, Model/RegionRegistry.cs, Model/RegionGroup.cs,
  Model/StorageType.cs, Model/ResidencyAction.cs, Model/ResidencyOutcome.cs,
  Model/TransferLegalBasis.cs, Model/DataProtectionLevel.cs, Model/TransferValidationResult.cs
  — these are value objects still used
- Do NOT delete Attributes/ — still used by pipeline behavior
- Do NOT delete DataResidencyAutoRegistrationDescriptor, DataResidencyFluentPolicyDescriptor — still used
- Do NOT delete DefaultCrossBorderTransferValidator, DefaultRegionContextProvider,
  DefaultAdequacyDecisionProvider, DefaultRegionRouter — preserved/rewired stateless services
- After deleting, fix any compilation errors (missing usings, broken references)

REFERENCE FILES:
- Recent git history: see how Retention (#783), ProcessorAgreements (#782) handled satellite deletion
- src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs (to find registration patterns)
```

</details>

---

### Phase 6: Observability, Testing & Documentation (~16 test files created, ~30 deleted, ~5 modified, ~5 production files modified)

> **Goal**: Update diagnostics, create/update all test files, update documentation.

<details>
<summary><strong>Tasks</strong></summary>

**6.1 Modify `Diagnostics/DataResidencyDiagnostics.cs`**
- Add service-level counters:
  - `residency.policies.created` (Counter) — Policies created via service
  - `residency.policies.updated` (Counter) — Policies updated
  - `residency.policies.deleted` (Counter) — Policies deleted
  - `residency.locations.registered` (Counter) — Locations registered via service
  - `residency.locations.migrated` (Counter) — Locations migrated between regions
  - `residency.locations.verified` (Counter) — Locations verified
  - `residency.locations.removed` (Counter) — Locations removed
  - `residency.violations.detected` (Counter) — Sovereignty violations detected
  - `residency.violations.resolved` (Counter) — Sovereignty violations resolved
- Keep existing pipeline and transfer counters

**6.2 Modify `Diagnostics/DataResidencyLogMessages.cs`**
- Add service-level log messages in 8680-8699 range (currently unused within DataResidency's 8600-8699 range):
  - `PolicyCreated` (8680), `PolicyUpdated` (8681), `PolicyDeleted` (8682)
  - `LocationRegistered` (8685), `LocationMigrated` (8686), `LocationVerified` (8687), `LocationRemoved` (8688)
  - `ViolationDetected` (8689), `ViolationResolved` (8690)
  - `CacheHit` (8692), `CacheMiss` (8693), `CacheInvalidated` (8694)
  - `ServiceStoreError` (8695)
- Keep all existing pipeline and validation log messages (8600-8679)

**6.3 Update Unit Tests**
- **Delete tests for removed classes** (~15 files):
  - InMemory store tests (3): InMemoryResidencyPolicyStoreTests, InMemoryDataLocationStoreTests, InMemoryResidencyAuditStoreTests
  - Service tests (1): DefaultDataResidencyPolicyTests
  - Model tests (3): DataLocationTests, ResidencyPolicyDescriptorTests, ResidencyAuditEntryTests
  - Mapper tests (3): DataLocationMapperTests, ResidencyPolicyMapperTests, ResidencyAuditEntryMapperTests
- **Create new tests** (~7 files):
  - `ResidencyPolicyAggregateTests.cs` — factory, commands, Apply, state transitions, invariants
  - `DataLocationAggregateTests.cs` — factory, commands, state machine, guards
  - `ResidencyPolicyProjectionTests.cs` — Create and all Apply handlers
  - `DataLocationProjectionTests.cs` — Create and all Apply handlers
  - `DefaultResidencyPolicyServiceTests.cs` — mock IAggregateRepository, IReadModelRepository, ICacheProvider
  - `DefaultDataLocationServiceTests.cs` — mock repos, test RegisterLocationAsync, GetViolationsAsync
  - `DataResidencyMartenExtensionsTests.cs` — verify DI registration (if needed)
- **Modify existing tests** (~3 files):
  - `DataResidencyPipelineBehaviorTests.cs` (if exists) → replace store mocks with service mocks
  - `DataResidencyOptionsTests.cs` → remove TrackAuditTrail test
  - `DataResidencyErrorsTests.cs` → update for new/removed error codes

**6.4 Update Guard Tests**
- Delete guard tests for removed classes (~6 files): InMemory stores (3), DefaultDataResidencyPolicy, mapper guard tests
- Create guard tests for new classes (~4 files): 2 aggregates, 2 services

**6.5 Update Property Tests**
- Delete property tests for removed models (~3 files): DataResidencyMapperPropertyTests, DataResidencyModelPropertyTests, etc.
- Create property tests for aggregates (~2 files): ResidencyPolicyAggregatePropertyTests, DataLocationAggregatePropertyTests

**6.6 Update Contract Tests**
- Delete contract tests for removed interfaces (~6 files): all IResidencyPolicyStore, IDataLocationStore, IResidencyAuditStore contract tests + InMemory variants

**6.7 Update Integration Tests**
- Delete: `DataResidencyPipelineIntegrationTests.cs` (depends on old stores)
- Create: `DataResidencyAggregateIntegrationTests.cs` — Marten-based aggregate persistence tests using `[Collection(MartenCollection.Name)]`
- Create: `DataResidencyPipelineIntegrationTests.cs` — Updated pipeline integration test using new services

**6.8 Documentation**
- Update `CHANGELOG.md`:
  ```
  ### Changed
  - Migrated `Encina.Compliance.DataResidency` from entity-based persistence to Marten event sourcing (#784)
  - Replaced 4 abstractions (IResidencyPolicyStore, IDataLocationStore, IResidencyAuditStore,
    IDataResidencyPolicy) with 2 services (IResidencyPolicyService, IDataLocationService)
  - Removed 13 satellite provider implementations (ADO.NET ×4, Dapper ×4, EF Core, MongoDB)
  - Event stream provides immutable GDPR Art. 5(2) audit trail
  - Rewired DefaultCrossBorderTransferValidator and DefaultRegionRouter to use IResidencyPolicyService

  ### Removed
  - All `IResidency*Store`, `IDataLocationStore` interfaces and InMemory implementations
  - `IDataResidencyPolicy` and `DefaultDataResidencyPolicy` (absorbed into IResidencyPolicyService)
  - All persistence entities and mappers
  - `DataResidencyOptions.TrackAuditTrail` property (inherent in event sourcing)
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
You are implementing Phase 6 of Issue #784 — Migrate DataResidency to Marten event sourcing.
Phases 1-5 are complete: all new code is in place, old code is deleted, build succeeds.

TASK:
1. UPDATE Diagnostics/DataResidencyDiagnostics.cs — Add service-level counters for policy, location,
   and violation operations (residency.policies.created, residency.locations.registered, etc.)

2. UPDATE Diagnostics/DataResidencyLogMessages.cs — Add log messages in 8680-8699 range for service
   operations (PolicyCreated, LocationRegistered, ViolationDetected, CacheHit, etc.)

3. DELETE obsolete test files:
   - tests/Encina.UnitTests/Compliance/DataResidency/ — InMemory*Tests, DefaultDataResidencyPolicyTests,
     DataLocationTests, ResidencyPolicyDescriptorTests, ResidencyAuditEntryTests, *MapperTests
   - tests/Encina.GuardTests/Compliance/DataResidency/ — InMemory*, DefaultDataResidencyPolicy*, MapperGuardTests
   - tests/Encina.ContractTests/Compliance/DataResidency/ — All files
   - tests/Encina.PropertyTests/Compliance/DataResidency/ — DataResidencyMapperPropertyTests, DataResidencyModelPropertyTests

4. CREATE new test files:
   - Unit tests: ResidencyPolicyAggregateTests, DataLocationAggregateTests,
     ResidencyPolicyProjectionTests, DataLocationProjectionTests,
     DefaultResidencyPolicyServiceTests, DefaultDataLocationServiceTests
   - Guard tests: ResidencyPolicyAggregateGuardTests, DataLocationAggregateGuardTests,
     DefaultResidencyPolicyServiceGuardTests, DefaultDataLocationServiceGuardTests
   - Property tests: ResidencyPolicyAggregatePropertyTests, DataLocationAggregatePropertyTests
   - Integration tests: DataResidencyAggregateIntegrationTests (Marten fixtures)

5. MODIFY existing test files:
   - DataResidencyPipelineBehaviorTests (if exists) — store mocks → service mocks
   - DataResidencyOptionsTests — remove TrackAuditTrail test
   - DataResidencyErrorsTests — update for new/removed error codes

6. UPDATE CHANGELOG.md, docs/INVENTORY.md

7. VERIFY: dotnet build --configuration Release && dotnet test

KEY RULES:
- Unit tests use NSubstitute for mocking, Shouldly for assertions
- Guard tests use Shouldly for assertions
- Property tests use FsCheck with [Property(MaxTest = 50)]
- Integration tests use [Collection(MartenCollection.Name)], [Trait("Category", "Integration")]
- EventId range 8680-8699 for new service log messages
- Test files follow AAA pattern, deterministic, independent
- Aggregate tests: use helper methods for lifecycle states

REFERENCE FILES:
- tests/Encina.UnitTests/Compliance/CrossBorderTransfer/ (aggregate test patterns)
- tests/Encina.UnitTests/Compliance/Retention/ (service test patterns)
- tests/Encina.IntegrationTests/Compliance/CrossBorderTransfer/ (Marten integration tests)
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
You are migrating Encina.Compliance.DataResidency from entity-based persistence (13 database providers)
to Marten event sourcing. This is Issue #784.

Eight compliance modules have already been migrated: Consent (#777), DataSubjectRights (#778),
LawfulBasis (#779), BreachNotification (#780), DPIA (#781), ProcessorAgreements (#782),
Retention (#783), and CrossBorderTransfer (#412, reference). Follow the EXACT patterns established
by these migrations.

IMPLEMENTATION OVERVIEW:

Phase 1 — Create Events & Aggregates:
- Events/ResidencyPolicyEvents.cs: 3 events (ResidencyPolicyCreated, ResidencyPolicyUpdated,
  ResidencyPolicyDeleted)
- Events/DataLocationEvents.cs: 6 events (DataLocationRegistered, DataLocationMigrated,
  DataLocationVerified, DataLocationRemoved, SovereigntyViolationDetected, SovereigntyViolationResolved)
- Aggregates/ResidencyPolicyAggregate.cs: extends AggregateBase, factory Create(), commands
  Update/Delete, guards for deleted state
- Aggregates/DataLocationAggregate.cs: extends AggregateBase, factory Register(), commands
  Migrate/Verify/Remove/DetectViolation/ResolveViolation (state machine with guards)

Phase 2 — Create Read Models & Projections:
- ReadModels/ResidencyPolicyReadModel.cs: IReadModel, mirrors aggregate state
- ReadModels/ResidencyPolicyProjection.cs: IProjection<ResidencyPolicyReadModel>
- ReadModels/DataLocationReadModel.cs: IReadModel, includes violation tracking
- ReadModels/DataLocationProjection.cs: IProjection<DataLocationReadModel>

Phase 3 — Create Services:
- Abstractions/IResidencyPolicyService.cs: 3 commands + 4 queries + 2 evaluation methods
  (absorbs IDataResidencyPolicy)
- Abstractions/IDataLocationService.cs: 7 commands + 6 queries
- Services/DefaultResidencyPolicyService.cs: aggregate + read model + cache + logging
- Services/DefaultDataLocationService.cs: aggregate + read model + cache + logging

Phase 4 — Wiring:
- DataResidencyMartenExtensions.cs: register 2 aggregates and 2 projections
- Update ServiceCollectionExtensions.cs: replace InMemory/default registrations with 2 services
- Update DataResidencyPipelineBehavior: stores → services, remove audit recording
- Update DefaultCrossBorderTransferValidator: IResidencyPolicyStore → IResidencyPolicyService
- Update DefaultRegionRouter: IDataResidencyPolicy → IResidencyPolicyService
- Update DataResidencyAutoRegistrationHostedService: store → IResidencyPolicyService
- Update DataResidencyFluentPolicyHostedService: store → IResidencyPolicyService
- Update HealthCheck: store checks → service checks
- Update Options: remove TrackAuditTrail
- Update Errors: add service error codes
- Update .csproj: add Encina.Caching and Encina.Marten references

Phase 5 — Delete Old Code:
- Delete 18 core files (4 interfaces, 3 InMemory stores, 1 service, 3 models, 3 entities, 3 mappers, 1 entry)
- Delete ~39 satellite provider files (ADO×12, Dapper×12, EF×~6, MongoDB×~6)
- Update 10 satellite ServiceCollectionExtensions
- Update PublicAPI files

Phase 6 — Observability, Testing & Documentation:
- Add service counters to Diagnostics (EventId 8680-8699)
- Delete ~30 obsolete test files, create ~16 new test files, modify ~5 test files
- Update CHANGELOG.md, INVENTORY.md

KEY PATTERNS:
- All events: sealed record implementing INotification, use primitive types (string for region codes)
- Aggregates: extend AggregateBase, static factory, instance commands, Apply(object) switch
- Services: IAggregateRepository<T> + IReadModelRepository<T> + ICacheProvider
- ROP: Either<EncinaError, T> on all service methods
- Cache keys: "dr:{entity}:{id}" prefix, 5 min TTL
- Guard clauses: ArgumentNullException.ThrowIfNull, ArgumentException.ThrowIfNullOrWhiteSpace
- .NET 10 / C# 14, nullable enabled, XML docs on all public APIs
- Stateless services preserved: ICrossBorderTransferValidator, IRegionContextProvider,
  IAdequacyDecisionProvider, IRegionRouter (rewired where needed)

REFERENCE FILES:
- src/Encina.Compliance.CrossBorderTransfer/ (complete reference implementation)
- src/Encina.Compliance.Retention/ (most recent migration, three aggregates)
- src/Encina.Compliance.ProcessorAgreements/ (recent migration, two aggregates)
- src/Encina.Compliance.DataResidency/ (current code to migrate)
- docs/plans/retention-es-migration-plan-783.md (plan format reference)
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Article | Relevance |
|----------|---------|-----------|
| GDPR Art. 44 | General principle for transfers | Cross-border transfer compliance gate |
| GDPR Art. 45 | Adequacy decisions | `IAdequacyDecisionProvider` checks Art. 45 status |
| GDPR Art. 46 | Appropriate safeguards | `TransferLegalBasis` enum captures SCCs, BCRs |
| GDPR Art. 49 | Derogations | Transfer validator considers Art. 49 exceptions |
| GDPR Art. 5(2) | Accountability | Event stream provides immutable proof of residency compliance |
| GDPR Art. 30 | Records of processing | Location tracking per entity and data category |
| GDPR Art. 58 | Supervisory authority powers | Audit trail available for regulatory inquiries |
| Schrems II | EU-US data transfers | Historical policy decisions must be reconstructable |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `AggregateBase` | `Encina.Marten` | Base class for both aggregates |
| `IAggregateRepository<T>` | `Encina.Marten` | Persistence of aggregates |
| `IReadModel` | `Encina.Marten` | Base interface for read models |
| `IReadModelRepository<T>` | `Encina.Marten` | Query read models |
| `IProjection<T>` | `Encina.Marten` | Projection interface |
| `ICacheProvider` | `Encina.Caching` | Cache-aside pattern in services |
| `INotification` | `Encina` | Event publishing via EventPublishingPipelineBehavior |
| `Either<L, R>` | `Encina` | ROP error handling |
| `EncinaError` | `Encina` | Error type |
| `IPipelineBehavior<,>` | `Encina` | Pipeline enforcement |
| `Region` / `RegionRegistry` | `Encina.Compliance.DataResidency.Model` | Preserved value objects |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| GDPR | 8100-8220 | |
| Consent | 8260-8299 | |
| DataSubjectRights | 8300-8399 | Migrated to ES |
| Anonymization | 8400-8499 | |
| Retention | 8500-8599 | Migrated to ES |
| **DataResidency** | **8600-8699** | **Current: 8600-8679. New service msgs: 8680-8699** |
| BreachNotification | 8700-8799 | Migrated to ES |
| DPIA | 8800-8899 | Migrated to ES |
| ProcessorAgreements | 8900-8999 | Migrated to ES |

### Estimated File Count

| Category | Created | Modified | Deleted |
|----------|---------|----------|---------|
| Events | 2 | 0 | 0 |
| Aggregates | 2 | 0 | 0 |
| Read Models + Projections | 4 | 0 | 0 |
| Services + Abstractions | 4 | 0 | 0 |
| Marten Extensions | 1 | 0 | 0 |
| Core modifications | 0 | 10 | 18 |
| Satellite deletions | 0 | 10 | ~39 |
| Tests (new) | ~16 | ~5 | ~30 |
| Documentation | 0 | 2 | 0 |
| **Total** | **~29** | **~27** | **~87** |

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Include | `ICacheProvider` in both services for read model caching (cache-aside pattern with "dr:" prefix) |
| 2 | OpenTelemetry | ✅ Include | Existing `ActivitySource` + `Meter` in `DataResidencyDiagnostics`; add service-level counters for policy/location/violation operations |
| 3 | Structured Logging | ✅ Include | Existing `DataResidencyLogMessages`; add service operation messages in 8680-8699 range |
| 4 | Health Checks | ✅ Include | Update existing `DataResidencyHealthCheck` to check services instead of stores; degraded if optional services absent |
| 5 | Validation | ✅ Include | `DataResidencyOptionsValidator` preserved; aggregate guard clauses validate state transitions |
| 6 | Resilience | ❌ N/A | Marten handles PostgreSQL connection resilience internally |
| 7 | Distributed Locks | ❌ N/A | No background service in DataResidency (unlike Retention's enforcement service) |
| 8 | Transactions | ✅ Include | Handled by Marten `IDocumentSession.SaveChangesAsync()` — no separate IUnitOfWork needed |
| 9 | Idempotency | ✅ Include | Marten optimistic concurrency via aggregate `Version` prevents duplicate operations |
| 10 | Multi-Tenancy | ✅ Include | `TenantId` on all events and aggregates; propagated through services |
| 11 | Module Isolation | ✅ Include | `ModuleId` on all events and aggregates; propagated through services |
| 12 | Audit Trail | ✅ Include | ES events ARE the audit trail — `GetPolicyHistoryAsync()` and `GetLocationHistoryAsync()` expose event streams per aggregate |

---

## Components Preserved (Unchanged or Minimally Modified)

| Component | Action | Reason |
|-----------|--------|--------|
| `DataResidencyAttribute` | **Unchanged** | Attribute-based metadata, no persistence dependency |
| `DataResidencyAttributeInfo` | **Unchanged** | Cached attribute data, no persistence dependency |
| `NoCrossBorderTransferAttribute` | **Unchanged** | Attribute constraint, no persistence dependency |
| `NoCrossBorderTransferInfo` | **Unchanged** | Cached constraint data, no persistence dependency |
| `Region` / `RegionRegistry` / `RegionGroup` | **Unchanged** | Value objects used by aggregates and services |
| `StorageType` | **Unchanged** | Enum used by aggregate and read model |
| `ResidencyAction` | **Unchanged** | Enum used by pipeline logging |
| `ResidencyOutcome` | **Unchanged** | Enum used by pipeline logging |
| `TransferLegalBasis` | **Unchanged** | Enum used by policy aggregate |
| `DataProtectionLevel` | **Unchanged** | Enum for region classification |
| `TransferValidationResult` | **Unchanged** | DTO returned by validator |
| `DataResidencyEnforcementMode` | **Unchanged** | Enum for pipeline behavior modes |
| `ResidencyPolicyBuilder` | **Unchanged** | Fluent builder for policy configuration |
| `DataResidencyAutoRegistrationDescriptor` | **Unchanged** | Value object for attribute scanning |
| `DataResidencyFluentPolicyDescriptor` | **Unchanged** | Value object for fluent policy config |
| `DataResidencyOptionsValidator` | **Unchanged** | Options validation |
| `ICrossBorderTransferValidator` | **Interface unchanged** | Stateless validation contract |
| `IRegionContextProvider` | **Unchanged** | Context resolution contract, no store dependency |
| `IAdequacyDecisionProvider` | **Unchanged** | Static data contract, no store dependency |
| `IRegionRouter` | **Interface unchanged** | Routing contract |
| `DefaultRegionContextProvider` | **Unchanged** | No store dependency |
| `DefaultAdequacyDecisionProvider` | **Unchanged** | No store dependency |
| `DefaultCrossBorderTransferValidator` | **Modified** | Rewired from `IResidencyPolicyStore` to `IResidencyPolicyService` |
| `DefaultRegionRouter` | **Modified** | Rewired from `IDataResidencyPolicy` to `IResidencyPolicyService` |
