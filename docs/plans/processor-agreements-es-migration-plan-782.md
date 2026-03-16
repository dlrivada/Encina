# [REFACTOR] Migrate ProcessorAgreements Module to Marten Event Sourcing — Implementation Plan

> **Issue**: [#782](https://github.com/dlrivada/Encina/issues/782)
> **Type**: Refactor (Event Sourcing Migration)
> **Complexity**: Medium-High — two aggregates (Processor + DPA), pipeline behavior, expiration monitoring, sub-processor hierarchy
> **Prerequisites**: ADR-019 (#776) ✅, CrossBorderTransfer (#412) ✅, Consent (#777) ✅, DSR (#778) ✅, LawfulBasis (#779) ✅, BreachNotification (#780) ✅, DPIA (#781) ✅
> **Provider Category**: Event Sourcing (Marten) — replaces 13 database providers

---

## Summary

Migrate `Encina.Compliance.ProcessorAgreements` from entity-based persistence with 13 database provider implementations to Marten event sourcing. The module manages GDPR Art. 28 Data Processing Agreements (DPAs) between controllers and processors, including sub-processor hierarchies, mandatory term compliance, and expiration monitoring.

**Key transformation**:
- `Processor` (sealed record) → `ProcessorAggregate` (event-sourced aggregate)
- `DataProcessingAgreement` (sealed record) → `DPAAggregate` (event-sourced aggregate)
- `IProcessorRegistry` + `IDPAStore` + `IProcessorAuditStore` (13 provider implementations each) → `IAggregateRepository<ProcessorAggregate>` + `IAggregateRepository<DPAAggregate>` (Marten)
- Audit trail moves from explicit `IProcessorAuditStore` to implicit event streams (events ARE the audit trail)
- New `IProcessorService` + `IDPAService` wrap aggregate operations with ROP, caching, and observability

**Estimated scope**:
- ~10 new files created (aggregates, events, read models, projections, services, abstractions, Marten extensions)
- ~39+ satellite provider files deleted (stores + SQL scripts across 13 providers)
- ~8 core files modified (pipeline behavior, DI, health check, expiration handler, diagnostics, options, errors)
- ~41 test files modified/deleted/created
- ~1,500-2,000 lines of new/modified production code + ~1,000-1,500 lines of tests

**Affected packages**: `Encina.Compliance.ProcessorAgreements` (core restructuring), 10 satellite packages (remove ProcessorAgreements stores/scripts), 5 test projects

---

## Design Choices

<details>
<summary><strong>1. Aggregate Granularity — Two Aggregates (Processor + DPA) vs. Single Aggregate</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Two aggregates: `ProcessorAggregate` + `DPAAggregate`** | Clear bounded contexts; processors have independent lifecycle from DPAs; multiple DPAs per processor is natural (history); mirrors the existing `IProcessorRegistry` vs `IDPAStore` separation | Two event streams to manage; cross-aggregate queries need read models |
| **B) Single `ProcessorAggregate` with embedded DPAs** | All data in one stream; simpler queries | Unbounded growth — processors accumulate DPAs over time; violates aggregate sizing best practice; couples identity to contractual state |

### Selected Option
**A) Two aggregates** — `ProcessorAggregate` manages processor identity, contact info, sub-processor authorization type, and hierarchy depth. `DPAAggregate` manages the contractual lifecycle (execute → amend → audit → renew → terminate) for a specific processor relationship.

### Rationale
The domain has two distinct lifecycles: (1) processor identity is long-lived and changes infrequently, (2) DPAs are temporal with status transitions and expiration. Embedding DPAs inside the processor aggregate would create an ever-growing event stream. The existing code already separates these concerns (`IProcessorRegistry` vs `IDPAStore`). Two aggregates also match the issue's proposed structure.

</details>

<details>
<summary><strong>2. Sub-Processor Hierarchy — Aggregate State vs. Read Model Query</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) ProcessorAggregate stores ParentProcessorId; hierarchy resolved via read model** | Aggregate stays simple; BFS traversal on read model (same as current in-memory approach) | Requires read model for hierarchy queries |
| **B) ProcessorAggregate maintains child list** | Direct child access on aggregate | Aggregate bloat; coupling between parent/child aggregates; complex event handling |
| **C) Separate HierarchyAggregate** | Clean hierarchy management | Over-engineered for the depth ≤ 10 constraint |

### Selected Option
**A) Aggregate stores ParentProcessorId, hierarchy resolved via read model** — The `ProcessorAggregate` stores `ParentProcessorId` and `Depth`. The `ProcessorReadModel` mirrors these fields. `GetSubProcessorsAsync` and `GetFullSubProcessorChainAsync` query the read model collection with BFS traversal, exactly as the current implementation does in-memory.

### Rationale
The current code already loads all processors and traverses in-memory (provider-agnostic BFS). This approach is simple, works with the bounded depth constraint (MaxSubProcessorDepth ≤ 10), and avoids coupling between aggregate instances.

</details>

<details>
<summary><strong>3. Service Interface Design — New Services Replacing Stores</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New `IProcessorService` + `IDPAService`** | Consistent with migrated modules (Consent, DSR, LawfulBasis, BreachNotification, DPIA); clean CQRS separation | Breaking change |
| **B) Keep `IProcessorRegistry` + `IDPAStore` as facades** | Minimal API change | Leaks old entity-based abstractions; inconsistent with other migrated modules |
| **C) Single `IProcessorAgreementService`** | One service entry point | Too many responsibilities; hard to name methods clearly for both processors and DPAs |

### Selected Option
**A) New `IProcessorService` + `IDPAService`** — Commands go through aggregates (write), queries go through read models (read). `IProcessorRegistry`, `IDPAStore`, `IProcessorAuditStore` are all deleted.

### Rationale
Consistent with all 6 previously migrated modules. Pre-1.0: breaking changes are expected and encouraged. The service pattern provides ROP, caching, observability, and proper error handling in one layer.

</details>

<details>
<summary><strong>4. Audit Trail Strategy — ES Events ARE the Audit Trail</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) ES events as audit trail** | Zero extra storage; immutable by design; complete timeline; events carry more info than audit entries | Requires Marten event stream queries for audit reports |
| **B) Keep separate `IProcessorAuditStore`** | Familiar API | Redundant with ES events; two sources of truth |

### Selected Option
**A) ES events as audit trail** — `ProcessorAgreementAuditEntry`, `IProcessorAuditStore`, and `InMemoryProcessorAuditStore` are all deleted. For audit queries, the services expose `GetProcessorHistoryAsync()` and `GetDPAHistoryAsync()` which read the event stream.

### Rationale
Every migrated compliance module uses this pattern. Event sourcing inherently satisfies GDPR Art. 5(2) accountability: every state change is an immutable event with timestamp and metadata.

</details>

<details>
<summary><strong>5. Pipeline Behavior — Queries IDPAService Instead of Stores</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Pipeline queries `IDPAService`** | ROP consistency; service handles caching; single entry point | Adds service call in hot path |
| **B) Pipeline queries read model directly** | Fast, direct read | Couples pipeline to Marten infrastructure |
| **C) Keep `IDPAValidator` querying `IDPAStore`** | Minimal pipeline change | Keeps dead abstractions |

### Selected Option
**A) Pipeline queries `IDPAService`** — The `ProcessorValidationPipelineBehavior` replaces its `IDPAValidator` dependency with `IDPAService`. The service's `HasValidDPAAsync` provides the fast-path boolean check with caching. `DefaultDPAValidator` is deleted.

### Rationale
The pipeline already caches attribute lookups per request type in `ConcurrentDictionary`. Adding `IDPAService` with `ICacheProvider` cache-aside provides a clean layering. `DefaultDPAValidator` was just orchestrating store calls — the service replaces that role entirely.

</details>

<details>
<summary><strong>6. DPA Event Granularity — Fine-Grained Lifecycle Events</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Fine-grained events** (Executed, Amended, Audited, Renewed, Terminated, Expired) | Full GDPR Art. 28 audit trail; each contractual action is explicit | More event types |
| **B) Coarse-grained events** (Created, Updated, StatusChanged) | Fewer types | Loses legal semantics; audit trail less informative |

### Selected Option
**A) Fine-grained events** — Each DPA lifecycle action is a distinct event. This is essential for GDPR Art. 28 accountability: regulators expect to see exactly when each contractual action occurred.

### Rationale
DPAs are legally binding documents. The event stream must prove: when the agreement was executed, what amendments were made, when audits occurred, when renewal happened, and why termination was initiated. Coarse-grained events would lose this crucial contractual timeline.

</details>

---

## Architecture & Structure

### Target Directory Structure

```
src/Encina.Compliance.ProcessorAgreements/
├── Abstractions/
│   ├── IProcessorService.cs                    ← NEW (processor lifecycle orchestrator)
│   ├── IDPAService.cs                          ← NEW (DPA lifecycle orchestrator)
│   └── IDPAValidator.cs                        ← DELETED (replaced by IDPAService)
├── Aggregates/
│   ├── ProcessorAggregate.cs                   ← NEW (event-sourced aggregate)
│   └── DPAAggregate.cs                         ← NEW (event-sourced aggregate)
├── Attributes/
│   └── RequiresProcessorAttribute.cs           ← PRESERVED
├── Diagnostics/
│   ├── ProcessorAgreementDiagnostics.cs        ← MODIFIED (add service counters)
│   └── ProcessorAgreementLogMessages.cs        ← MODIFIED (add service log messages)
├── Events/
│   ├── ProcessorEvents.cs                      ← NEW (Processor domain events)
│   └── DPAEvents.cs                            ← NEW (DPA domain events)
├── Health/
│   └── ProcessorAgreementHealthCheck.cs        ← MODIFIED (check Marten connectivity)
├── Model/                                      ← PRESERVED (value objects, enums)
│   ├── DPAMandatoryTerms.cs                   ← PRESERVED
│   ├── DPAStatus.cs                           ← PRESERVED
│   ├── DPAValidationResult.cs                 ← PRESERVED (used by service query)
│   ├── ProcessorAgreementEnforcementMode.cs   ← PRESERVED
│   └── SubProcessorAuthorizationType.cs       ← PRESERVED
├── Notifications/                              ← PRESERVED (kept for handler compatibility)
│   ├── ProcessorRegisteredNotification.cs      ← PRESERVED
│   ├── DPASignedNotification.cs               ← PRESERVED
│   ├── DPAExpiringNotification.cs             ← PRESERVED
│   ├── DPAExpiredNotification.cs              ← PRESERVED
│   ├── DPATerminatedNotification.cs           ← PRESERVED
│   ├── SubProcessorAddedNotification.cs       ← PRESERVED
│   └── SubProcessorRemovedNotification.cs     ← PRESERVED
├── ReadModels/
│   ├── ProcessorReadModel.cs                   ← NEW (Marten inline projection target)
│   ├── ProcessorProjection.cs                  ← NEW (event → read model)
│   ├── DPAReadModel.cs                        ← NEW (Marten inline projection target)
│   └── DPAProjection.cs                       ← NEW (event → read model)
├── Scheduling/
│   ├── CheckDPAExpirationCommand.cs           ← PRESERVED
│   └── CheckDPAExpirationHandler.cs           ← MODIFIED (use IDPAService)
├── Services/
│   ├── DefaultProcessorService.cs              ← NEW (aggregate lifecycle orchestrator)
│   └── DefaultDPAService.cs                   ← NEW (aggregate lifecycle orchestrator)
├── ProcessorAgreementErrors.cs                 ← MODIFIED (update for service errors)
├── ProcessorAgreementOptions.cs                ← MODIFIED (remove TrackAuditTrail)
├── ProcessorAgreementOptionsValidator.cs       ← PRESERVED
├── ProcessorValidationPipelineBehavior.cs      ← MODIFIED (use IDPAService)
├── ProcessorAgreementsMartenExtensions.cs      ← NEW (Marten aggregate registration)
├── ServiceCollectionExtensions.cs              ← MODIFIED (remove InMemory registrations)
├── PublicAPI.Shipped.txt                       ← MODIFIED
└── PublicAPI.Unshipped.txt                     ← MODIFIED
```

### Files to DELETE

**Core Module (10 files)**:
- `src/Encina.Compliance.ProcessorAgreements/Abstractions/IProcessorRegistry.cs`
- `src/Encina.Compliance.ProcessorAgreements/Abstractions/IDPAStore.cs`
- `src/Encina.Compliance.ProcessorAgreements/Abstractions/IDPAValidator.cs`
- `src/Encina.Compliance.ProcessorAgreements/Abstractions/IProcessorAuditStore.cs`
- `src/Encina.Compliance.ProcessorAgreements/InMemoryProcessorRegistry.cs`
- `src/Encina.Compliance.ProcessorAgreements/InMemoryDPAStore.cs`
- `src/Encina.Compliance.ProcessorAgreements/InMemoryProcessorAuditStore.cs`
- `src/Encina.Compliance.ProcessorAgreements/DefaultDPAValidator.cs`
- `src/Encina.Compliance.ProcessorAgreements/Model/Processor.cs` (replaced by aggregate)
- `src/Encina.Compliance.ProcessorAgreements/Model/DataProcessingAgreement.cs` (replaced by aggregate)
- `src/Encina.Compliance.ProcessorAgreements/Model/ProcessorAgreementAuditEntry.cs`

**Persistence Entities & Mappers (6 files)**:
- `src/Encina.Compliance.ProcessorAgreements/ProcessorEntity.cs`
- `src/Encina.Compliance.ProcessorAgreements/DataProcessingAgreementEntity.cs`
- `src/Encina.Compliance.ProcessorAgreements/ProcessorAgreementAuditEntryEntity.cs`
- `src/Encina.Compliance.ProcessorAgreements/ProcessorMapper.cs`
- `src/Encina.Compliance.ProcessorAgreements/DataProcessingAgreementMapper.cs`
- `src/Encina.Compliance.ProcessorAgreements/ProcessorAgreementAuditEntryMapper.cs`

**Satellite Provider Stores (~39 files)**:

*EF Core (3 + configurations)*:
- `src/Encina.EntityFrameworkCore/ProcessorAgreements/ProcessorRegistryEF.cs`
- `src/Encina.EntityFrameworkCore/ProcessorAgreements/DPAStoreEF.cs`
- `src/Encina.EntityFrameworkCore/ProcessorAgreements/ProcessorAuditStoreEF.cs`
- Any EntityConfiguration files for ProcessorAgreements

*ADO.NET (12 files + scripts)*:
- `src/Encina.ADO.{Sqlite,SqlServer,PostgreSQL,MySQL}/ProcessorAgreements/ProcessorRegistryADO.cs` (4)
- `src/Encina.ADO.{Sqlite,SqlServer,PostgreSQL,MySQL}/ProcessorAgreements/DPAStoreADO.cs` (4)
- `src/Encina.ADO.{Sqlite,SqlServer,PostgreSQL,MySQL}/ProcessorAgreements/ProcessorAuditStoreADO.cs` (4)

*Dapper (12 files)*:
- `src/Encina.Dapper.{Sqlite,SqlServer,PostgreSQL,MySQL}/ProcessorAgreements/ProcessorRegistryDapper.cs` (4)
- `src/Encina.Dapper.{Sqlite,SqlServer,PostgreSQL,MySQL}/ProcessorAgreements/DPAStoreDapper.cs` (4)
- `src/Encina.Dapper.{Sqlite,SqlServer,PostgreSQL,MySQL}/ProcessorAgreements/ProcessorAuditStoreDapper.cs` (4)

*MongoDB (6 files)*:
- `src/Encina.MongoDB/ProcessorAgreements/ProcessorRegistryMongoDB.cs`
- `src/Encina.MongoDB/ProcessorAgreements/DPAStoreMongoDB.cs`
- `src/Encina.MongoDB/ProcessorAgreements/ProcessorAuditStoreMongoDB.cs`
- `src/Encina.MongoDB/ProcessorAgreements/ProcessorDocument.cs`
- `src/Encina.MongoDB/ProcessorAgreements/DataProcessingAgreementDocument.cs`
- `src/Encina.MongoDB/ProcessorAgreements/ProcessorAgreementAuditEntryDocument.cs`

*SQL Schema Scripts*:
- `src/Encina.{ADO,Dapper}.{Sqlite,SqlServer,PostgreSQL,MySQL}/Scripts/027_CreateProcessorAgreementAuditEntriesTable.sql` (8)
- Any processor/DPA table creation scripts in the 02x range

---

## Implementation Phases (6 Phases)

### Phase 1: Events & Aggregates (~6 new files, ~500 lines)

> **Goal**: Create the event-sourced aggregates and domain events that model the processor and DPA lifecycles.

<details>
<summary><strong>Tasks</strong></summary>

**1.1 Create `Events/ProcessorEvents.cs`**
- Namespace: `Encina.Compliance.ProcessorAgreements.Events`
- All events are `sealed record` implementing `INotification`
- Events:
  - `ProcessorRegistered(Guid ProcessorId, string Name, string Country, string? ContactEmail, string? ParentProcessorId, int Depth, SubProcessorAuthorizationType AuthorizationType, DateTimeOffset OccurredAtUtc, string? TenantId, string? ModuleId)`
  - `ProcessorUpdated(Guid ProcessorId, string Name, string Country, string? ContactEmail, SubProcessorAuthorizationType AuthorizationType, DateTimeOffset OccurredAtUtc)`
  - `ProcessorRemoved(Guid ProcessorId, string Reason, DateTimeOffset OccurredAtUtc)`
  - `SubProcessorAdded(Guid ProcessorId, Guid SubProcessorId, string SubProcessorName, int Depth, DateTimeOffset OccurredAtUtc)`
  - `SubProcessorRemoved(Guid ProcessorId, Guid SubProcessorId, string Reason, DateTimeOffset OccurredAtUtc)`

**1.2 Create `Events/DPAEvents.cs`**
- Namespace: `Encina.Compliance.ProcessorAgreements.Events`
- Events:
  - `DPAExecuted(Guid DPAId, Guid ProcessorId, DPAMandatoryTerms MandatoryTerms, bool HasSCCs, IReadOnlyList<string> ProcessingPurposes, DateTimeOffset SignedAtUtc, DateTimeOffset? ExpiresAtUtc, DateTimeOffset OccurredAtUtc, string? TenantId, string? ModuleId)`
  - `DPAAmended(Guid DPAId, DPAMandatoryTerms UpdatedTerms, bool HasSCCs, IReadOnlyList<string> ProcessingPurposes, string AmendmentReason, DateTimeOffset OccurredAtUtc)`
  - `DPAAudited(Guid DPAId, string AuditorId, string AuditFindings, DateTimeOffset OccurredAtUtc)`
  - `DPARenewed(Guid DPAId, DateTimeOffset NewExpiresAtUtc, DateTimeOffset OccurredAtUtc)`
  - `DPATerminated(Guid DPAId, string Reason, DateTimeOffset OccurredAtUtc)`
  - `DPAExpired(Guid DPAId, DateTimeOffset OccurredAtUtc)`
  - `DPAMarkedPendingRenewal(Guid DPAId, DateTimeOffset OccurredAtUtc)`

**1.3 Create `Aggregates/ProcessorAggregate.cs`**
- Namespace: `Encina.Compliance.ProcessorAgreements.Aggregates`
- Extends `AggregateBase`
- State properties (`private set`): `Name`, `Country`, `ContactEmail`, `ParentProcessorId`, `Depth`, `AuthorizationType`, `IsRemoved`, `TenantId`, `ModuleId`, `CreatedAtUtc`, `LastUpdatedAtUtc`
- Factory method: `static ProcessorAggregate Register(Guid id, string name, string country, string? contactEmail, string? parentProcessorId, int depth, SubProcessorAuthorizationType authorizationType, DateTimeOffset occurredAtUtc, string? tenantId, string? moduleId)`
- Command methods: `Update(...)`, `Remove(...)`, `AddSubProcessor(...)`, `RemoveSubProcessor(...)`
- `protected override void Apply(object domainEvent)` with switch on all event types
- Guard clauses enforce invariants (e.g., cannot update a removed processor)

**1.4 Create `Aggregates/DPAAggregate.cs`**
- Namespace: `Encina.Compliance.ProcessorAgreements.Aggregates`
- Extends `AggregateBase`
- State properties (`private set`): `ProcessorId`, `Status`, `MandatoryTerms`, `HasSCCs`, `ProcessingPurposes`, `SignedAtUtc`, `ExpiresAtUtc`, `TenantId`, `ModuleId`, `CreatedAtUtc`, `LastUpdatedAtUtc`
- Factory method: `static DPAAggregate Execute(Guid id, Guid processorId, DPAMandatoryTerms mandatoryTerms, bool hasSCCs, IReadOnlyList<string> processingPurposes, DateTimeOffset signedAtUtc, DateTimeOffset? expiresAtUtc, DateTimeOffset occurredAtUtc, string? tenantId, string? moduleId)`
- Command methods: `Amend(...)`, `Audit(...)`, `Renew(...)`, `Terminate(...)`, `MarkExpired()`, `MarkPendingRenewal()`
- Query methods: `IsActive(DateTimeOffset nowUtc)` → `bool`
- `protected override void Apply(object domainEvent)` with switch on all event types
- State transitions: Active → Amended (stays Active), Active → PendingRenewal → Renewed (back to Active), Active → Terminated, Active → Expired

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
You are migrating the Encina.Compliance.ProcessorAgreements module from entity-based persistence to Marten event sourcing.
This follows the exact pattern established by CrossBorderTransfer, Consent, DSR, LawfulBasis, BreachNotification, and DPIA.

TASK:
Create 4 new files in src/Encina.Compliance.ProcessorAgreements/:

1. Events/ProcessorEvents.cs — 5 domain events as sealed records implementing INotification.
   Events: ProcessorRegistered, ProcessorUpdated, ProcessorRemoved, SubProcessorAdded, SubProcessorRemoved.
   ProcessorRegistered includes TenantId/ModuleId. All events include OccurredAtUtc.
   Include XML docs with GDPR Art. 28 references.

2. Events/DPAEvents.cs — 7 domain events as sealed records implementing INotification.
   Events: DPAExecuted, DPAAmended, DPAAudited, DPARenewed, DPATerminated, DPAExpired, DPAMarkedPendingRenewal.
   DPAExecuted includes TenantId/ModuleId, ProcessorId, MandatoryTerms, HasSCCs, ProcessingPurposes.
   Include XML docs with GDPR Art. 28(3) references.

3. Aggregates/ProcessorAggregate.cs — Event-sourced aggregate extending AggregateBase.
   Properties (private set): Name, Country, ContactEmail, ParentProcessorId, Depth,
   AuthorizationType (SubProcessorAuthorizationType), IsRemoved, TenantId, ModuleId, CreatedAtUtc, LastUpdatedAtUtc.
   Factory: static Register(). Commands: Update(), Remove(), AddSubProcessor(), RemoveSubProcessor().
   Apply() handles all 5 processor event types. Guard clauses enforce invariants.

4. Aggregates/DPAAggregate.cs — Event-sourced aggregate extending AggregateBase.
   Properties (private set): ProcessorId, Status (DPAStatus), MandatoryTerms (DPAMandatoryTerms),
   HasSCCs, ProcessingPurposes, SignedAtUtc, ExpiresAtUtc, TenantId, ModuleId, CreatedAtUtc, LastUpdatedAtUtc.
   Factory: static Execute(). Commands: Amend(), Audit(), Renew(), Terminate(), MarkExpired(), MarkPendingRenewal().
   Query: IsActive(nowUtc). Apply() handles all 7 DPA event types.

KEY RULES:
- .NET 10 / C# 14, nullable enabled
- Events are sealed records implementing INotification (from Encina core, NOT MediatR)
- Timestamps use DateTimeOffset with AtUtc suffix
- Aggregate validates state transitions (throw InvalidOperationException for invalid transitions)
- Factory method is static, behavior methods are instance
- Apply method uses switch on domainEvent type
- XML documentation on all public types with GDPR article references (Art. 28, 5(2))
- Guard clauses: ArgumentNullException.ThrowIfNull, ArgumentException.ThrowIfNullOrWhiteSpace
- SubProcessorAuthorizationType, DPAStatus, DPAMandatoryTerms are EXISTING types in Model/

REFERENCE FILES:
- src/Encina.Compliance.CrossBorderTransfer/Aggregates/TIAAggregate.cs (aggregate pattern)
- src/Encina.Compliance.CrossBorderTransfer/Events/TIAEvents.cs (events pattern)
- src/Encina.Compliance.BreachNotification/Aggregates/BreachAggregate.cs (aggregate pattern)
- src/Encina.Compliance.ProcessorAgreements/Model/DPAStatus.cs (existing enum)
- src/Encina.Compliance.ProcessorAgreements/Model/DPAMandatoryTerms.cs (existing value object)
- src/Encina.Compliance.ProcessorAgreements/Model/SubProcessorAuthorizationType.cs (existing enum)
```

</details>

---

### Phase 2: Read Models & Projections (~4 new files, ~300 lines)

> **Goal**: Create query-side read models and Marten projections for both Processor and DPA data.

<details>
<summary><strong>Tasks</strong></summary>

**2.1 Create `ReadModels/ProcessorReadModel.cs`**
- Namespace: `Encina.Compliance.ProcessorAgreements.ReadModels`
- Implements `IReadModel`
- Properties (`{ get; set; }` — mutable for projection): `Guid Id`, `string Name`, `string Country`, `string? ContactEmail`, `Guid? ParentProcessorId`, `int Depth`, `SubProcessorAuthorizationType AuthorizationType`, `bool IsRemoved`, `string? TenantId`, `string? ModuleId`, `DateTimeOffset CreatedAtUtc`, `DateTimeOffset LastModifiedAtUtc`, `int Version`

**2.2 Create `ReadModels/ProcessorProjection.cs`**
- Implements `IProjection<ProcessorReadModel>`, `IProjectionCreator<ProcessorRegistered, ProcessorReadModel>`, `IProjectionHandler<TEvent, ProcessorReadModel>` for each subsequent event
- `ProjectionName => "ProcessorProjection"`
- `Create(ProcessorRegistered)` → new read model
- `Apply(ProcessorUpdated, ...)`, `Apply(ProcessorRemoved, ...)`, etc.
- Each handler increments `Version++` and updates `LastModifiedAtUtc`

**2.3 Create `ReadModels/DPAReadModel.cs`**
- Implements `IReadModel`
- Properties: `Guid Id`, `Guid ProcessorId`, `DPAStatus Status`, `DPAMandatoryTerms MandatoryTerms`, `bool HasSCCs`, `List<string> ProcessingPurposes`, `DateTimeOffset SignedAtUtc`, `DateTimeOffset? ExpiresAtUtc`, `List<AuditRecord> AuditHistory`, `string? TenantId`, `string? ModuleId`, `DateTimeOffset CreatedAtUtc`, `DateTimeOffset LastModifiedAtUtc`, `int Version`
- Nested record: `AuditRecord(string AuditorId, string Findings, DateTimeOffset AuditedAtUtc)`
- Method: `bool IsActive(DateTimeOffset nowUtc)` → same logic as current `DataProcessingAgreement.IsActive`

**2.4 Create `ReadModels/DPAProjection.cs`**
- Implements `IProjection<DPAReadModel>`, `IProjectionCreator<DPAExecuted, DPAReadModel>`, `IProjectionHandler<TEvent, DPAReadModel>` for each subsequent event
- `ProjectionName => "DPAProjection"`
- Handles: `DPAAmended` (updates terms/SCCs/purposes), `DPAAudited` (adds to AuditHistory), `DPARenewed` (updates ExpiresAtUtc + status), `DPATerminated` (status = Terminated), `DPAExpired` (status = Expired), `DPAMarkedPendingRenewal` (status = PendingRenewal)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
You are implementing Phase 2 of Issue #782 — Migrate ProcessorAgreements to Marten event sourcing.
Phase 1 is complete: ProcessorAggregate, DPAAggregate, and 12 domain events exist.

TASK:
Create 4 files in src/Encina.Compliance.ProcessorAgreements/ReadModels/:

1. ProcessorReadModel.cs — sealed class implementing IReadModel with mutable properties (get; set;).
   Mirrors ProcessorAggregate state. Includes LastModifiedAtUtc, Version.

2. ProcessorProjection.cs — Implements IProjection<ProcessorReadModel> +
   IProjectionCreator<ProcessorRegistered, ProcessorReadModel> +
   IProjectionHandler for ProcessorUpdated, ProcessorRemoved, SubProcessorAdded, SubProcessorRemoved.
   ProjectionName = "ProcessorProjection".

3. DPAReadModel.cs — sealed class implementing IReadModel with mutable properties.
   Mirrors DPAAggregate state. Includes AuditHistory as List<AuditRecord>.
   Nested record: AuditRecord(string AuditorId, string Findings, DateTimeOffset AuditedAtUtc).
   IsActive(nowUtc) query method.

4. DPAProjection.cs — Implements IProjection<DPAReadModel> +
   IProjectionCreator<DPAExecuted, DPAReadModel> +
   IProjectionHandler for DPAAmended, DPAAudited, DPARenewed, DPATerminated, DPAExpired, DPAMarkedPendingRenewal.

KEY RULES:
- Read models have mutable properties (get; set;) for projection updates
- Always increment Version and update LastModifiedAtUtc on each event
- ProjectionName property returns a unique string
- XML documentation on all public types
- .NET 10 / C# 14, nullable enabled

REFERENCE FILES:
- src/Encina.Compliance.CrossBorderTransfer/ReadModels/TIAReadModel.cs
- src/Encina.Compliance.CrossBorderTransfer/ReadModels/TIAProjection.cs
- src/Encina.Compliance.BreachNotification/ReadModels/BreachReadModel.cs
- src/Encina.Compliance.BreachNotification/ReadModels/BreachProjection.cs
```

</details>

---

### Phase 3: Service Interfaces & Implementations (~4 new files, ~600 lines)

> **Goal**: Create `IProcessorService` + `IDPAService` with default implementations using aggregates, read models, caching, and observability.

<details>
<summary><strong>Tasks</strong></summary>

**3.1 Create `Abstractions/IProcessorService.cs`**
- Namespace: `Encina.Compliance.ProcessorAgreements.Abstractions`
- **Commands** (write via aggregate):
  - `RegisterProcessorAsync(string name, string country, string? contactEmail, string? parentProcessorId, int depth, SubProcessorAuthorizationType authorizationType, string? tenantId, string? moduleId, CancellationToken ct)` → `ValueTask<Either<EncinaError, Guid>>`
  - `UpdateProcessorAsync(Guid processorId, string name, string country, string? contactEmail, SubProcessorAuthorizationType authorizationType, CancellationToken ct)` → `ValueTask<Either<EncinaError, Unit>>`
  - `RemoveProcessorAsync(Guid processorId, string reason, CancellationToken ct)` → `ValueTask<Either<EncinaError, Unit>>`
- **Queries** (read via read model):
  - `GetProcessorAsync(Guid processorId, CancellationToken ct)` → `ValueTask<Either<EncinaError, ProcessorReadModel>>`
  - `GetAllProcessorsAsync(CancellationToken ct)` → `ValueTask<Either<EncinaError, IReadOnlyList<ProcessorReadModel>>>`
  - `GetSubProcessorsAsync(Guid processorId, CancellationToken ct)` → `ValueTask<Either<EncinaError, IReadOnlyList<ProcessorReadModel>>>`
  - `GetFullSubProcessorChainAsync(Guid processorId, CancellationToken ct)` → `ValueTask<Either<EncinaError, IReadOnlyList<ProcessorReadModel>>>`
  - `GetProcessorHistoryAsync(Guid processorId, CancellationToken ct)` → `ValueTask<Either<EncinaError, IReadOnlyList<object>>>` (event stream = audit)

**3.2 Create `Abstractions/IDPAService.cs`**
- **Commands**:
  - `ExecuteDPAAsync(Guid processorId, DPAMandatoryTerms mandatoryTerms, bool hasSCCs, IReadOnlyList<string> processingPurposes, DateTimeOffset signedAtUtc, DateTimeOffset? expiresAtUtc, string? tenantId, string? moduleId, CancellationToken ct)` → `ValueTask<Either<EncinaError, Guid>>`
  - `AmendDPAAsync(Guid dpaId, DPAMandatoryTerms updatedTerms, bool hasSCCs, IReadOnlyList<string> processingPurposes, string amendmentReason, CancellationToken ct)` → `ValueTask<Either<EncinaError, Unit>>`
  - `AuditDPAAsync(Guid dpaId, string auditorId, string auditFindings, CancellationToken ct)` → `ValueTask<Either<EncinaError, Unit>>`
  - `RenewDPAAsync(Guid dpaId, DateTimeOffset newExpiresAtUtc, CancellationToken ct)` → `ValueTask<Either<EncinaError, Unit>>`
  - `TerminateDPAAsync(Guid dpaId, string reason, CancellationToken ct)` → `ValueTask<Either<EncinaError, Unit>>`
- **Queries**:
  - `GetDPAAsync(Guid dpaId, CancellationToken ct)` → `ValueTask<Either<EncinaError, DPAReadModel>>`
  - `GetDPAsByProcessorIdAsync(Guid processorId, CancellationToken ct)` → `ValueTask<Either<EncinaError, IReadOnlyList<DPAReadModel>>>`
  - `GetActiveDPAByProcessorIdAsync(Guid processorId, CancellationToken ct)` → `ValueTask<Either<EncinaError, DPAReadModel>>`
  - `GetDPAsByStatusAsync(DPAStatus status, CancellationToken ct)` → `ValueTask<Either<EncinaError, IReadOnlyList<DPAReadModel>>>`
  - `GetExpiringDPAsAsync(DateTimeOffset threshold, CancellationToken ct)` → `ValueTask<Either<EncinaError, IReadOnlyList<DPAReadModel>>>`
  - `HasValidDPAAsync(Guid processorId, CancellationToken ct)` → `ValueTask<Either<EncinaError, bool>>` (fast path for pipeline)
  - `ValidateDPAAsync(Guid processorId, CancellationToken ct)` → `ValueTask<Either<EncinaError, DPAValidationResult>>` (detailed validation)
  - `GetDPAHistoryAsync(Guid dpaId, CancellationToken ct)` → `ValueTask<Either<EncinaError, IReadOnlyList<object>>>` (event stream = audit)

**3.3 Create `Services/DefaultProcessorService.cs`**
- Dependencies: `IAggregateRepository<ProcessorAggregate>`, `IReadModelRepository<ProcessorReadModel>`, `ICacheProvider`, `TimeProvider`, `ILogger<DefaultProcessorService>`
- Cache keys: `"pa:processor:{id}"`, `"pa:processors:all"`, `"pa:subs:{processorId}"`
- Command pattern: create/load aggregate → call behavior method → save → invalidate cache → record metric
- Query pattern: check cache → read from read model repo → populate cache (5 min TTL) → return
- Sub-processor chain: load all processors from read model, BFS traversal in-memory (same as current)

**3.4 Create `Services/DefaultDPAService.cs`**
- Dependencies: `IAggregateRepository<DPAAggregate>`, `IReadModelRepository<DPAReadModel>`, `ICacheProvider`, `TimeProvider`, `IOptions<ProcessorAgreementOptions>`, `ILogger<DefaultDPAService>`
- Cache keys: `"pa:dpa:{id}"`, `"pa:dpa:active:{processorId}"`, `"pa:dpa:status:{status}"`
- `HasValidDPAAsync`: fast boolean path — check cache for active DPA, verify not expired, verify mandatory terms complete
- `ValidateDPAAsync`: detailed validation — returns `DPAValidationResult` with missing terms, warnings, days until expiration (reuses existing `DPAValidationResult` model)
- Error handling: catch `InvalidOperationException` for state transition errors → return domain error

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
You are implementing Phase 3 of Issue #782 — Migrate ProcessorAgreements to Marten event sourcing.
Phases 1-2 are complete: two aggregates, 12 events, two read models, and two projections exist.

TASK:
Create 4 files:

1. Abstractions/IProcessorService.cs — Interface with 3 command methods (RegisterProcessorAsync,
   UpdateProcessorAsync, RemoveProcessorAsync) and 5 query methods (GetProcessorAsync,
   GetAllProcessorsAsync, GetSubProcessorsAsync, GetFullSubProcessorChainAsync, GetProcessorHistoryAsync).
   All return ValueTask<Either<EncinaError, T>>.

2. Abstractions/IDPAService.cs — Interface with 5 command methods (ExecuteDPAAsync, AmendDPAAsync,
   AuditDPAAsync, RenewDPAAsync, TerminateDPAAsync) and 8 query methods (GetDPAAsync,
   GetDPAsByProcessorIdAsync, GetActiveDPAByProcessorIdAsync, GetDPAsByStatusAsync,
   GetExpiringDPAsAsync, HasValidDPAAsync, ValidateDPAAsync, GetDPAHistoryAsync).

3. Services/DefaultProcessorService.cs — Implementation with IAggregateRepository<ProcessorAggregate>,
   IReadModelRepository<ProcessorReadModel>, ICacheProvider, TimeProvider, ILogger.
   Cache-aside pattern with "pa:processor:{id}" keys. BFS for sub-processor chain.

4. Services/DefaultDPAService.cs — Implementation with IAggregateRepository<DPAAggregate>,
   IReadModelRepository<DPAReadModel>, ICacheProvider, TimeProvider, IOptions<ProcessorAgreementOptions>,
   ILogger. HasValidDPAAsync is the fast pipeline path. ValidateDPAAsync returns DPAValidationResult.

KEY RULES:
- ROP: Either<EncinaError, T> on all methods
- Cache-aside: check cache → load → cache with 5 min TTL
- Invalidate cache after writes
- catch InvalidOperationException → return domain error
- catch Exception (not OperationCanceledException) → return store error
- Use ProcessorAgreementDiagnostics counters and logger extension methods
- XML docs with GDPR Art. 28 references

REFERENCE FILES:
- src/Encina.Compliance.CrossBorderTransfer/Services/DefaultTIAService.cs (service pattern)
- src/Encina.Compliance.CrossBorderTransfer/Abstractions/ITIAService.cs (interface pattern)
- src/Encina.Compliance.BreachNotification/Services/DefaultBreachNotificationService.cs
- src/Encina.Compliance.ProcessorAgreements/ProcessorAgreementErrors.cs (existing errors)
- src/Encina.Compliance.ProcessorAgreements/Model/DPAValidationResult.cs (reuse for ValidateDPAAsync)
```

</details>

---

### Phase 4: Wiring — DI, Pipeline, Health Check, Expiration Handler, Marten Extensions (~8 files modified/created, ~300 lines)

> **Goal**: Wire everything together: update DI registrations, pipeline behavior, health check, expiration handler, and create Marten extensions.

<details>
<summary><strong>Tasks</strong></summary>

**4.1 Create `ProcessorAgreementsMartenExtensions.cs`**
- `public static IServiceCollection AddProcessorAgreementsAggregates(this IServiceCollection services)`
- Registers: `AddAggregateRepository<ProcessorAggregate>()`, `AddAggregateRepository<DPAAggregate>()`
- Registers: `AddProjection<ProcessorProjection, ProcessorReadModel>()`, `AddProjection<DPAProjection, DPAReadModel>()`

**4.2 Modify `ServiceCollectionExtensions.cs`**
- Remove: `TryAddSingleton<IProcessorRegistry, InMemoryProcessorRegistry>`
- Remove: `TryAddSingleton<IDPAStore, InMemoryDPAStore>`
- Remove: `TryAddSingleton<IProcessorAuditStore, InMemoryProcessorAuditStore>`
- Remove: `TryAddScoped<IDPAValidator, DefaultDPAValidator>`
- Add: `TryAddScoped<IProcessorService, DefaultProcessorService>`
- Add: `TryAddScoped<IDPAService, DefaultDPAService>`
- Keep: `ProcessorValidationPipelineBehavior<,>` registration
- Keep: `CheckDPAExpirationHandler` registration
- Keep: Health check and TimeProvider registrations

**4.3 Modify `ProcessorValidationPipelineBehavior.cs`**
- Replace `IDPAValidator` dependency with `IDPAService`
- Replace `IProcessorAuditStore` dependency (audit is implicit in ES)
- `HasValidDPAAsync` calls `_dpaService.HasValidDPAAsync(processorId, ct)`
- `ValidateAsync` calls `_dpaService.ValidateDPAAsync(processorId, ct)`
- Remove audit recording code (events handle this automatically)
- Keep: attribute caching in `ConcurrentDictionary`, enforcement modes, diagnostics

**4.4 Modify `Health/ProcessorAgreementHealthCheck.cs`**
- Replace store resolution checks with service resolution checks
- Check: `IProcessorService` resolvable
- Check: `IDPAService` resolvable
- Remove: `IProcessorAuditStore` check (no longer exists)
- Keep: options configuration check, expired DPA check (via `IDPAService.GetDPAsByStatusAsync`)

**4.5 Modify `Scheduling/CheckDPAExpirationHandler.cs`**
- Replace `IDPAStore` + `IProcessorRegistry` + `IProcessorAuditStore` dependencies with `IDPAService`
- `GetExpiringAsync` → `_dpaService.GetExpiringDPAsAsync(threshold, ct)`
- For expired: call `_dpaService.TerminateDPAAsync()` or mark expired via service
- For approaching: publish `DPAExpiringNotification` (keep notification pattern)
- Remove audit recording (events handle this)

**4.6 Modify `ProcessorAgreementOptions.cs`**
- Remove `TrackAuditTrail` property (inherent in ES — events ARE the audit trail)
- Keep all other properties unchanged

**4.7 Modify `ProcessorAgreementErrors.cs`**
- Update/add error codes for new service patterns
- Add: `StoreError(string operation, Exception ex)` for repository failures
- Keep existing error codes that are still relevant (processor.not_found, processor.dpa_expired, etc.)

**4.8 Modify `.csproj`**
- Add reference: `Encina.Caching` (for `ICacheProvider`)
- Add reference: `Encina.Marten` (for `AggregateBase`, `IAggregateRepository`)
- Remove: `Encina.Messaging` reference if no longer needed (check dependencies)
- Keep: `Encina.Compliance.GDPR`, health checks, public API analyzers

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
You are implementing Phase 4 of Issue #782 — Migrate ProcessorAgreements to Marten event sourcing.
Phases 1-3 are complete: aggregates, events, read models, projections, service interfaces, and
default service implementations all exist.

TASK:
Create 1 new file and modify 7 existing files:

1. CREATE ProcessorAgreementsMartenExtensions.cs — Extension method AddProcessorAgreementsAggregates()
   that registers AddAggregateRepository for both aggregates and AddProjection for both projections.

2. MODIFY ServiceCollectionExtensions.cs — Remove all InMemory store registrations and IDPAValidator.
   Add IProcessorService → DefaultProcessorService, IDPAService → DefaultDPAService (TryAddScoped).

3. MODIFY ProcessorValidationPipelineBehavior.cs — Replace IDPAValidator with IDPAService.
   Remove IProcessorAuditStore dependency. Update HasValidDPAAsync/ValidateAsync calls.

4. MODIFY Health/ProcessorAgreementHealthCheck.cs — Replace store checks with service checks.

5. MODIFY Scheduling/CheckDPAExpirationHandler.cs — Replace store dependencies with IDPAService.

6. MODIFY ProcessorAgreementOptions.cs — Remove TrackAuditTrail property.

7. MODIFY ProcessorAgreementErrors.cs — Add StoreError method for repository failures.

8. MODIFY .csproj — Add Encina.Caching and Encina.Marten references.

KEY RULES:
- Use TryAdd pattern for all service registrations
- Pipeline behavior must keep ConcurrentDictionary attribute caching
- Pipeline behavior must keep enforcement modes (Block/Warn/Disabled)
- Health check uses scoped resolution via IServiceProvider.CreateScope()
- Marten extensions is a SEPARATE file from ServiceCollectionExtensions

REFERENCE FILES:
- src/Encina.Compliance.CrossBorderTransfer/CrossBorderTransferMartenExtensions.cs
- src/Encina.Compliance.CrossBorderTransfer/ServiceCollectionExtensions.cs
- src/Encina.Compliance.DPIA/ServiceCollectionExtensions.cs (recently migrated)
- src/Encina.Compliance.DPIA/DPIARequiredPipelineBehavior.cs (pipeline after migration)
```

</details>

---

### Phase 5: Delete Old Code & Update Satellite Packages (~55+ files deleted, ~10 satellite ServiceCollectionExtensions modified)

> **Goal**: Remove all entity-based persistence code — InMemory stores, persistence entities, mappers, satellite provider stores, SQL scripts, and their DI registrations.

<details>
<summary><strong>Tasks</strong></summary>

**5.1 Delete Core Module Files**
- `Abstractions/IProcessorRegistry.cs`
- `Abstractions/IDPAStore.cs`
- `Abstractions/IDPAValidator.cs`
- `Abstractions/IProcessorAuditStore.cs`
- `InMemoryProcessorRegistry.cs`
- `InMemoryDPAStore.cs`
- `InMemoryProcessorAuditStore.cs`
- `DefaultDPAValidator.cs`
- `Model/Processor.cs` (replaced by ProcessorAggregate)
- `Model/DataProcessingAgreement.cs` (replaced by DPAAggregate)
- `Model/ProcessorAgreementAuditEntry.cs` (replaced by ES events)
- `ProcessorEntity.cs`
- `DataProcessingAgreementEntity.cs`
- `ProcessorAgreementAuditEntryEntity.cs`
- `ProcessorMapper.cs`
- `DataProcessingAgreementMapper.cs`
- `ProcessorAgreementAuditEntryMapper.cs`

**5.2 Delete All Satellite Provider Stores**
- All ProcessorAgreements files in Encina.ADO.{Sqlite,SqlServer,PostgreSQL,MySQL} (12 files)
- All ProcessorAgreements files in Encina.Dapper.{Sqlite,SqlServer,PostgreSQL,MySQL} (12 files)
- All ProcessorAgreements files in Encina.EntityFrameworkCore (3+ files)
- All ProcessorAgreements files in Encina.MongoDB (6 files)
- All ProcessorAgreements SQL scripts (8+ files)

**5.3 Update Satellite ServiceCollectionExtensions**
- Remove ProcessorAgreements store registrations from each satellite package's `ServiceCollectionExtensions.cs`:
  - `Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs`
  - `Encina.ADO.{Sqlite,SqlServer,PostgreSQL,MySQL}/ServiceCollectionExtensions.cs` (4)
  - `Encina.Dapper.{Sqlite,SqlServer,PostgreSQL,MySQL}/ServiceCollectionExtensions.cs` (4)
  - `Encina.MongoDB/ServiceCollectionExtensions.cs`

**5.4 Update PublicAPI Files**
- Move deleted public symbols from `PublicAPI.Unshipped.txt` (or Shipped.txt) to reflect removal
- Add new public symbols for aggregates, events, read models, services, Marten extensions

**5.5 Remove InternalsVisibleTo (if no longer needed)**
- Review `.csproj` — satellite packages no longer need visibility into ProcessorAgreements internals
- Keep test project visibility

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
You are implementing Phase 5 of Issue #782 — Migrate ProcessorAgreements to Marten event sourcing.
Phases 1-4 are complete: new aggregates, events, read models, projections, services, and wiring
are all in place. Now we need to delete the old entity-based code.

TASK:
1. DELETE all files listed in the "Files to DELETE" section of the plan (17 core + 33+ satellite files)
2. UPDATE ServiceCollectionExtensions in ALL 10 satellite packages — remove ProcessorAgreements
   store registrations (IProcessorRegistry, IDPAStore, IProcessorAuditStore replacements)
3. UPDATE PublicAPI.Shipped.txt and PublicAPI.Unshipped.txt — remove deleted symbols, add new ones
4. VERIFY: run `dotnet build src/Encina.Compliance.ProcessorAgreements/` to confirm no broken references

KEY RULES:
- Delete entire ProcessorAgreements/ subdirectories in satellite packages
- In satellite ServiceCollectionExtensions: search for "ProcessorRegistry", "DPAStore",
  "ProcessorAuditStore" registrations and remove them
- Do NOT delete Model/DPAMandatoryTerms.cs, Model/DPAStatus.cs, Model/DPAValidationResult.cs,
  Model/ProcessorAgreementEnforcementMode.cs, Model/SubProcessorAuthorizationType.cs — these are
  value objects still used by the aggregates and services
- Do NOT delete Notifications/ — these are still published by services
- Do NOT delete Attributes/RequiresProcessorAttribute.cs — still used by pipeline behavior
- After deleting, fix any compilation errors (missing usings, broken references)

REFERENCE FILES:
- Recent git history: see how DPIA (#781), BreachNotification (#780) handled satellite deletion
- src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs (to find registration patterns)
- src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs (to find registration patterns)
```

</details>

---

### Phase 6: Observability, Testing & Documentation (~20+ test files modified/created, ~5 files modified)

> **Goal**: Update diagnostics, create/update all test files, update documentation.

<details>
<summary><strong>Tasks</strong></summary>

**6.1 Modify `Diagnostics/ProcessorAgreementDiagnostics.cs`**
- Add service-level counters:
  - `processor.registered` (Counter) — Processors registered
  - `processor.updated` (Counter) — Processors updated
  - `processor.removed` (Counter) — Processors removed
  - `dpa.executed` (Counter) — DPAs executed
  - `dpa.amended` (Counter) — DPAs amended
  - `dpa.audited` (Counter) — DPAs audited
  - `dpa.renewed` (Counter) — DPAs renewed
  - `dpa.terminated` (Counter) — DPAs terminated
- Keep existing pipeline and expiration counters

**6.2 Modify `Diagnostics/ProcessorAgreementLogMessages.cs`**
- Add service-level log messages in existing EventId range (8900-8999):
  - Use 8970-8989 range for new service operations (currently unused)
  - `ProcessorCreated` (8970), `ProcessorUpdated` (8971), `ProcessorRemoved` (8972)
  - `DPAExecuted` (8975), `DPAAmended` (8976), `DPAAudited` (8977), `DPARenewed` (8978), `DPATerminated` (8979)
  - `CacheHit` (8980), `CacheMiss` (8981), `CacheInvalidated` (8982)
  - `ServiceStoreError` (8985)
- Keep all existing pipeline and expiration log messages

**6.3 Update Unit Tests**
- **Delete tests for removed classes**: InMemoryProcessorRegistryTests, InMemoryDPAStoreTests, InMemoryProcessorAuditStoreTests, DefaultDPAValidatorTests, all mapper tests (Processor, DPA, AuditEntry), entity tests
- **Create new tests**:
  - `ProcessorAggregateTests.cs` — test factory method, command methods, Apply, state transitions, invariants
  - `DPAAggregateTests.cs` — test factory method, command methods, Apply, state transitions, IsActive
  - `ProcessorProjectionTests.cs` — test Create and all Apply handlers
  - `DPAProjectionTests.cs` — test Create and all Apply handlers
  - `DefaultProcessorServiceTests.cs` — mock IAggregateRepository, IReadModelRepository, ICacheProvider; test commands and queries
  - `DefaultDPAServiceTests.cs` — same pattern; test HasValidDPAAsync and ValidateDPAAsync thoroughly
- **Modify existing tests**:
  - `ProcessorValidationPipelineBehaviorTests.cs` — replace IDPAValidator mock with IDPAService mock
  - `CheckDPAExpirationHandlerTests.cs` — replace store mocks with IDPAService mock
  - `ProcessorAgreementHealthCheckTests.cs` — replace store resolution mocks with service resolution mocks
  - `ServiceCollectionExtensionsTests.cs` — verify IProcessorService/IDPAService registration

**6.4 Update Guard Tests**
- Delete guard tests for removed classes (InMemory stores, DefaultDPAValidator, mappers)
- Create guard tests for new classes (aggregates, services, projections)

**6.5 Update Property Tests**
- Delete property tests for removed models (Processor record, DataProcessingAgreement record)
- Create property tests for aggregates (round-trip via events, invariants hold for all inputs)
- Keep: DPAMandatoryTermsPropertyTests, DPAValidationResultPropertyTests, ProcessorAgreementOptionsPropertyTests

**6.6 Update Contract Tests**
- Delete contract tests for removed interfaces (IProcessorRegistryContractTests, IDPAStoreContractTests, IProcessorAuditStoreContractTests, InMemory variants)
- Create contract tests for new service interfaces if multiple implementations expected (likely not needed — services have single default implementation)

**6.7 Documentation**
- Update `CHANGELOG.md` — add entry under Unreleased section:
  ```
  ### Changed
  - Migrated `Encina.Compliance.ProcessorAgreements` from entity-based persistence to Marten event sourcing (#782)
  - Replaced `IProcessorRegistry`, `IDPAStore`, `IProcessorAuditStore` with `IProcessorService`, `IDPAService`
  - Removed 13 satellite provider implementations (ADO.NET ×4, Dapper ×4, EF Core, MongoDB)
  - Event stream provides immutable GDPR Art. 5(2) audit trail

  ### Removed
  - `IProcessorRegistry`, `IDPAStore`, `IDPAValidator`, `IProcessorAuditStore` interfaces
  - All InMemory store implementations
  - All persistence entities and mappers
  - `ProcessorAgreementOptions.TrackAuditTrail` property (inherent in event sourcing)
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
You are implementing Phase 6 of Issue #782 — Migrate ProcessorAgreements to Marten event sourcing.
Phases 1-5 are complete: all new code is in place, old code is deleted, build succeeds.

TASK:
1. UPDATE Diagnostics/ProcessorAgreementDiagnostics.cs — Add service-level counters for
   processor and DPA operations (processor.registered, dpa.executed, etc.)

2. UPDATE Diagnostics/ProcessorAgreementLogMessages.cs — Add log messages in 8970-8989 range
   for service operations (ProcessorCreated, DPAExecuted, CacheHit, etc.)

3. DELETE obsolete test files:
   - tests/Encina.UnitTests/Compliance/ProcessorAgreements/ — InMemoryProcessorRegistryTests,
     InMemoryDPAStoreTests, InMemoryProcessorAuditStoreTests, DefaultDPAValidatorTests,
     ProcessorMapperTests, DataProcessingAgreementMapperTests, ProcessorAgreementAuditEntryMapperTests
   - tests/Encina.GuardTests/Compliance/ProcessorAgreements/ — InMemory*, Mapper*, DefaultDPAValidator*
   - tests/Encina.ContractTests/Compliance/ProcessorAgreements/ — All files (InMemory contract tests)
   - tests/Encina.PropertyTests/Compliance/ProcessorAgreements/ — ProcessorPropertyTests,
     DataProcessingAgreementPropertyTests, InMemoryProcessorRegistryPropertyTests

4. CREATE new test files:
   - Unit tests: ProcessorAggregateTests, DPAAggregateTests, ProcessorProjectionTests,
     DPAProjectionTests, DefaultProcessorServiceTests, DefaultDPAServiceTests
   - Guard tests: ProcessorAggregateGuardTests, DPAAggregateGuardTests,
     DefaultProcessorServiceGuardTests, DefaultDPAServiceGuardTests
   - Property tests: ProcessorAggregatePropertyTests, DPAAggregatePropertyTests

5. MODIFY existing test files:
   - ProcessorValidationPipelineBehaviorTests — IDPAValidator → IDPAService
   - CheckDPAExpirationHandlerTests — store mocks → IDPAService mock
   - ProcessorAgreementHealthCheckTests — store checks → service checks
   - ServiceCollectionExtensionsTests — verify new registrations

6. UPDATE CHANGELOG.md, docs/INVENTORY.md

7. VERIFY: dotnet build --configuration Release && dotnet test

KEY RULES:
- Unit tests use NSubstitute for mocking, FluentAssertions for assertions
- Guard tests use Shouldly for assertions
- Property tests use FsCheck with [Property(MaxTest = 50)]
- EventId range 8970-8989 for new service log messages (currently unused)
- Test files follow AAA pattern
- All tests must be deterministic

REFERENCE FILES:
- tests/Encina.UnitTests/Compliance/CrossBorderTransfer/ (aggregate test patterns)
- tests/Encina.UnitTests/Compliance/BreachNotification/ (service test patterns)
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
You are migrating Encina.Compliance.ProcessorAgreements from entity-based persistence (13 database
providers) to Marten event sourcing. This is Issue #782.

Six compliance modules have already been migrated: Consent (#777), DataSubjectRights (#778),
LawfulBasis (#779), BreachNotification (#780), DPIA (#781), and CrossBorderTransfer (#412, reference).
Follow the EXACT patterns established by these migrations.

IMPLEMENTATION OVERVIEW:

Phase 1 — Create Events & Aggregates:
- Events/ProcessorEvents.cs: 5 events (ProcessorRegistered, ProcessorUpdated, ProcessorRemoved,
  SubProcessorAdded, SubProcessorRemoved)
- Events/DPAEvents.cs: 7 events (DPAExecuted, DPAAmended, DPAAudited, DPARenewed, DPATerminated,
  DPAExpired, DPAMarkedPendingRenewal)
- Aggregates/ProcessorAggregate.cs: extends AggregateBase, factory Register(), commands Update/Remove/AddSubProcessor/RemoveSubProcessor
- Aggregates/DPAAggregate.cs: extends AggregateBase, factory Execute(), commands Amend/Audit/Renew/Terminate/MarkExpired/MarkPendingRenewal

Phase 2 — Create Read Models & Projections:
- ReadModels/ProcessorReadModel.cs: IReadModel, mirrors aggregate state
- ReadModels/ProcessorProjection.cs: IProjection<ProcessorReadModel>
- ReadModels/DPAReadModel.cs: IReadModel, includes AuditHistory list
- ReadModels/DPAProjection.cs: IProjection<DPAReadModel>

Phase 3 — Create Services:
- Abstractions/IProcessorService.cs: 3 commands + 5 queries
- Abstractions/IDPAService.cs: 5 commands + 8 queries (including HasValidDPAAsync fast path)
- Services/DefaultProcessorService.cs: aggregate + read model + cache + logging
- Services/DefaultDPAService.cs: aggregate + read model + cache + validation + logging

Phase 4 — Wiring:
- ProcessorAgreementsMartenExtensions.cs: register aggregates and projections
- Update ServiceCollectionExtensions.cs: replace InMemory stores with services
- Update ProcessorValidationPipelineBehavior: IDPAValidator → IDPAService
- Update HealthCheck: store checks → service checks
- Update CheckDPAExpirationHandler: store deps → IDPAService
- Update Options: remove TrackAuditTrail
- Update Errors: add StoreError
- Update .csproj: add Encina.Caching and Encina.Marten references

Phase 5 — Delete Old Code:
- Delete 17 core files (interfaces, InMemory stores, entities, mappers, validator)
- Delete 39+ satellite provider files (ADO×12, Dapper×12, EF×3+, MongoDB×6, scripts×8+)
- Update 10 satellite ServiceCollectionExtensions
- Update PublicAPI files

Phase 6 — Observability, Testing & Documentation:
- Add service counters to Diagnostics (EventId 8970-8989)
- Delete ~20 obsolete test files, create ~12 new test files, modify ~4 test files
- Update CHANGELOG.md, INVENTORY.md

KEY PATTERNS:
- All events: sealed record implementing INotification, include OccurredAtUtc
- Aggregates: extend AggregateBase, static factory, instance commands, Apply(object) switch
- Services: IAggregateRepository<T> + IReadModelRepository<T> + ICacheProvider
- ROP: Either<EncinaError, T> on all service methods
- Cache keys: "pa:{entity}:{id}" prefix
- Guard clauses: ArgumentNullException.ThrowIfNull, ArgumentException.ThrowIfNullOrWhiteSpace
- .NET 10 / C# 14, nullable enabled, XML docs on all public APIs

REFERENCE FILES:
- src/Encina.Compliance.CrossBorderTransfer/ (complete reference implementation)
- src/Encina.Compliance.BreachNotification/ (recently migrated)
- src/Encina.Compliance.DPIA/ (most complex migration)
- src/Encina.Compliance.ProcessorAgreements/ (current code to migrate)
- docs/plans/dpia-es-migration-plan-781.md (plan format reference)
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Article | Relevance |
|----------|---------|-----------|
| GDPR Art. 5(2) | Accountability | Event stream provides immutable proof of all processor relationship changes |
| GDPR Art. 28(1) | Controller-Processor obligations | Processor registration and DPA requirement |
| GDPR Art. 28(2) | Sub-processor authorization | Sub-processor hierarchy and authorization types |
| GDPR Art. 28(3)(a-h) | Mandatory DPA terms | DPAMandatoryTerms value object (8 boolean fields) |
| GDPR Art. 28(4) | Sub-processor obligations | Sub-processor chain tracking with depth limits |
| GDPR Art. 28(9) | Written form requirement | DPA execution and amendment events provide timestamped proof |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `AggregateBase` | `Encina.Marten` | Base class for ProcessorAggregate and DPAAggregate |
| `IAggregateRepository<T>` | `Encina.Marten` | Persistence of aggregates |
| `IReadModel` | `Encina.Marten` | Base interface for read models |
| `IReadModelRepository<T>` | `Encina.Marten` | Query read models |
| `IProjection<T>` | `Encina.Marten` | Projection interface |
| `ICacheProvider` | `Encina.Caching` | Cache-aside pattern in services |
| `INotification` | `Encina` | Event publishing via EventPublishingPipelineBehavior |
| `Either<L, R>` | `Encina` | ROP error handling |
| `EncinaError` | `Encina` | Error type |
| `IPipelineBehavior<,>` | `Encina` | Pipeline enforcement |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| DataSubjectRights | 8300-8349 | Migrated to ES |
| LawfulBasis | 8350-8399 | Migrated to ES |
| CrossBorderTransfer | 8500-8549 | Reference implementation |
| DataResidency | 8600-8699 | |
| BreachNotification | 8700-8799 | Migrated to ES |
| DPIA | 8800-8899 | Migrated to ES |
| **ProcessorAgreements** | **8900-8999** | **Current: 8900-8963. New service msgs: 8970-8989** |

### Estimated File Count

| Category | Created | Modified | Deleted |
|----------|---------|----------|---------|
| Events | 2 | 0 | 0 |
| Aggregates | 2 | 0 | 0 |
| Read Models | 4 | 0 | 0 |
| Services + Abstractions | 4 | 0 | 0 |
| Marten Extensions | 1 | 0 | 0 |
| Core modifications | 0 | 8 | 17 |
| Satellite deletions | 0 | 10 | 39+ |
| Tests (new) | ~12 | ~4 | ~20 |
| Documentation | 0 | 2 | 0 |
| **Total** | **~25** | **~24** | **~76+** |

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ✅ Include | `ICacheProvider` in both services for read model caching (cache-aside pattern) |
| 2 | OpenTelemetry | ✅ Include | Existing `ActivitySource` + `Meter` in `ProcessorAgreementDiagnostics`; add service-level counters |
| 3 | Structured Logging | ✅ Include | Existing `ProcessorAgreementLogMessages`; add service operation messages in 8970-8989 range |
| 4 | Health Checks | ✅ Include | Update existing `ProcessorAgreementHealthCheck` to check services instead of stores |
| 5 | Validation | ✅ Include | `ProcessorAgreementOptionsValidator` preserved; `ValidateDPAAsync` on IDPAService provides detailed validation |
| 6 | Resilience | ❌ N/A | Marten handles PostgreSQL connection resilience internally |
| 7 | Distributed Locks | ❌ N/A | No concurrent shared state requiring locking; Marten optimistic concurrency handles aggregate conflicts |
| 8 | Transactions | ✅ Include | Handled by Marten `IDocumentSession.SaveChangesAsync()` — no separate IUnitOfWork needed |
| 9 | Idempotency | ✅ Include | Marten optimistic concurrency via aggregate `Version` prevents duplicate operations |
| 10 | Multi-Tenancy | ✅ Include | `TenantId` on all events and aggregates; propagated through services |
| 11 | Module Isolation | ✅ Include | `ModuleId` on all events and aggregates; propagated through services |
| 12 | Audit Trail | ✅ Include | ES events ARE the audit trail — `GetProcessorHistoryAsync` / `GetDPAHistoryAsync` expose event streams |

---

## Components Preserved (Unchanged or Minimally Modified)

| Component | Action | Reason |
|-----------|--------|--------|
| `RequiresProcessorAttribute` | **Unchanged** | Attribute-based metadata, no persistence dependency |
| `DPAMandatoryTerms` | **Unchanged** | Value object used by aggregate and services |
| `DPAStatus` | **Unchanged** | Enum used by aggregate state |
| `DPAValidationResult` | **Unchanged** | DTO returned by IDPAService.ValidateDPAAsync |
| `ProcessorAgreementEnforcementMode` | **Unchanged** | Enum for pipeline behavior modes |
| `SubProcessorAuthorizationType` | **Unchanged** | Enum for processor authorization |
| All 7 Notifications | **Unchanged** | Published by services, consumed by handlers |
| `CheckDPAExpirationCommand` | **Unchanged** | Scheduling command |
| `ProcessorAgreementOptionsValidator` | **Unchanged** | Options validation |
