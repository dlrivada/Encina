# [REFACTOR] Migrate DPIA Module to Marten Event Sourcing — Implementation Plan

> **Issue**: [#781](https://github.com/dlrivada/Encina/issues/781)
> **Type**: Refactor (Event Sourcing Migration)
> **Complexity**: High — richest compliance module (assessment engine, pipeline behavior, risk criteria, DPO consultation, review reminders, auto-registration)
> **Prerequisites**: ADR-019 (#776) ✅, CrossBorderTransfer (#412) ✅, Consent (#777) ✅, DSR (#778) ✅, LawfulBasis (#779) ✅, BreachNotification (#780) ✅
> **Provider Category**: Event Sourcing (Marten) — replaces 13 database providers

---

## Summary

Migrate `Encina.Compliance.DPIA` from entity-based persistence with 13 database provider implementations to Marten event sourcing. The DPIA module is the most complex compliance module, featuring a composite risk evaluation engine, pipeline enforcement behavior, DPO consultation lifecycle, auto-registration hosted service, review reminder background service, and template-based assessment.

**Key transformation**:
- `DPIAAssessment` (sealed record) → `DPIAAggregate` (event-sourced aggregate)
- `IDPIAStore` + `IDPIAAuditStore` (13 provider implementations each) → `IAggregateRepository<DPIAAggregate>` (Marten)
- Audit trail moves from explicit `IDPIAAuditStore` to implicit event stream (events ARE the audit trail)
- New `IDPIAService` wraps aggregate operations with ROP, caching, and observability

**Estimated scope**:
- ~7 new files created
- ~25 satellite provider files deleted (stores + SQL scripts)
- ~10 core files modified (engine, pipeline behavior, DI, health check, reminder service)
- ~30 test files modified/deleted/created
- ~2 justification files preserved (Load/Benchmark)

**Affected packages**: `Encina.Compliance.DPIA` (core restructuring), 10 satellite packages (remove DPIA stores/scripts), 5 test projects

---

## Design Choices

<details>
<summary><strong>1. Aggregate Granularity — Single vs. Multiple Aggregates</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Single `DPIAAggregate`** | Simple, all lifecycle in one stream, easy to reason about | Longer event stream per assessment |
| **B) Separate aggregates per sub-concern** (Assessment + Consultation + Review) | Shorter streams, independent scaling | Over-engineered for the domain; consultation and review are always scoped to an assessment |

### Selected Option
**A) Single `DPIAAggregate`** — The DPIA lifecycle is inherently sequential (create → evaluate → consult → approve/reject → expire). All state transitions are scoped to one assessment. Splitting would add accidental complexity without domain benefit.

### Rationale
All previously migrated compliance modules (Consent, DSR, LawfulBasis, BreachNotification) use a single aggregate per entity. The DPIA domain, while richer, still represents a single assessment lifecycle. DPO consultation and review are child concerns, not independent aggregates.

</details>

<details>
<summary><strong>2. Assessment Engine Refactoring Strategy</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Engine calls aggregate directly** | Clean separation; engine creates/updates aggregate | Engine needs repository dependency; blurs service/engine boundary |
| **B) Engine remains pure (returns result), service orchestrates** | Engine stays focused on risk evaluation; service handles persistence | Requires new `IDPIAService` to coordinate |
| **C) Merge engine into service** | Fewer moving parts | Violates SRP; engine logic is independently valuable |

### Selected Option
**B) Engine remains pure, service orchestrates** — The `IDPIAAssessmentEngine` continues to evaluate risk criteria and return `DPIAResult`. A new `IDPIAService` orchestrates the full lifecycle: creating aggregates, calling the engine, applying results as events, managing DPO consultation, and handling persistence via `IAggregateRepository`.

### Rationale
The assessment engine's risk evaluation logic is domain-pure and independently testable. Mixing persistence concerns into it would reduce testability and violate SRP. The service pattern matches CrossBorderTransfer's `IApprovedTransferService`.

</details>

<details>
<summary><strong>3. Pipeline Behavior — Store vs. Read Model Dependency</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Pipeline queries `IDPIAService`** | ROP consistency; service handles caching | Adds service call overhead in hot path |
| **B) Pipeline queries `IReadModelRepository<DPIAReadModel>` directly** | Fast, direct read path; no intermediate abstraction | Couples pipeline to Marten read model infrastructure |
| **C) Pipeline queries `IDPIAStore` (keep interface, change impl)** | Minimal pipeline change | Keeps dead abstraction; confusing to maintain |

### Selected Option
**A) Pipeline queries `IDPIAService`** — The pipeline behavior calls `IDPIAService.GetAssessmentByRequestTypeAsync()` which queries the read model with caching. This maintains ROP consistency and allows the service to handle cache-aside logic.

### Rationale
The pipeline behavior is a hot path but DPIA checks are already cached per request type in `ConcurrentDictionary`. Adding a service call with cache-aside in `ICacheProvider` provides a clean layering without meaningful performance impact. The service becomes the single entry point for all DPIA operations.

</details>

<details>
<summary><strong>4. Event Design — Granularity of DPIA Events</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Fine-grained events** (Created, Evaluated, DPOConsultationRequested, DPOResponded, Approved, Rejected, RevisionRequested, Expired) | Full audit trail; each state transition is explicit | More events to maintain |
| **B) Coarse-grained events** (Created, Updated, StatusChanged) | Fewer event types | Loses domain semantics; audit trail less informative |

### Selected Option
**A) Fine-grained events** — Each meaningful domain action produces a distinct event. This is essential for GDPR Art. 5(2) accountability: regulators expect to see exactly when each step occurred.

### Rationale
DPIA is a legally mandated process. The event stream must prove: when the assessment was initiated, what risks were identified, when the DPO was consulted, what their decision was, when approval/rejection occurred, and when reviews happened. Coarse-grained events would lose this crucial timeline.

</details>

<details>
<summary><strong>5. Handling of Preserved Components</strong></summary>

### Components Preserved (unchanged or minimally modified)

| Component | Action | Reason |
|-----------|--------|--------|
| `IRiskCriterion` + 6 implementations | **Unchanged** | Pure domain logic, no persistence dependency |
| `IDPIATemplateProvider` + `DefaultDPIATemplateProvider` | **Unchanged** | Template logic is independent of persistence |
| `RequiresDPIAAttribute` | **Unchanged** | Attribute-based metadata, no persistence dependency |
| `DPIAAutoDetector` | **Unchanged** | Heuristic detection, no persistence dependency |
| `DPIAOptions` | **Modified** | Remove `TrackAuditTrail` (inherent in ES), keep all other options |
| `DPIADiagnostics` + `DPIALogMessages` | **Modified** | Update log messages for new service methods, keep EventId range 8800-8899 |
| `DPIAErrors` | **Modified** | Update error codes for new service patterns |
| Model types (`DPIAResult`, `DPIAContext`, `RiskItem`, `Mitigation`, `DPOConsultation`, etc.) | **Unchanged** | Value objects used by aggregate and services |

</details>

---

## Architecture & Structure

### Target Directory Structure

```
src/Encina.Compliance.DPIA/
├── Abstractions/
│   ├── IDPIAAssessmentEngine.cs          ← PRESERVED (pure risk evaluation)
│   ├── IDPIAService.cs                   ← NEW (aggregate lifecycle orchestrator)
│   ├── IDPIATemplateProvider.cs          ← PRESERVED
│   └── IRiskCriterion.cs                 ← PRESERVED
├── Aggregates/
│   └── DPIAAggregate.cs                  ← NEW (event-sourced aggregate)
├── Attributes/
│   └── RequiresDPIAAttribute.cs          ← PRESERVED
├── Diagnostics/
│   ├── DPIADiagnostics.cs               ← MODIFIED (add service counters)
│   └── DPIALogMessages.cs               ← MODIFIED (add service log messages)
├── Events/
│   └── DPIAEvents.cs                    ← NEW (all domain events)
├── Health/
│   └── DPIAHealthCheck.cs               ← MODIFIED (check Marten connectivity)
├── Model/                               ← PRESERVED (all value objects unchanged)
│   ├── DPIAAssessment.cs                ← PRESERVED (used as snapshot/DTO)
│   ├── DPIAAssessmentStatus.cs          ← PRESERVED
│   ├── DPIAContext.cs                   ← PRESERVED
│   ├── DPIAEnforcementMode.cs           ← PRESERVED
│   ├── DPIAResult.cs                    ← PRESERVED
│   ├── DPIASection.cs                   ← PRESERVED
│   ├── DPIATemplate.cs                  ← PRESERVED
│   ├── DPOConsultation.cs               ← PRESERVED
│   ├── DPOConsultationDecision.cs       ← PRESERVED
│   ├── HighRiskTriggers.cs              ← PRESERVED
│   ├── Mitigation.cs                    ← PRESERVED
│   ├── RiskItem.cs                      ← PRESERVED
│   └── RiskLevel.cs                     ← PRESERVED
├── Notifications/                       ← PRESERVED (kept for backward-compat with handlers)
│   ├── DPIAAssessmentCompleted.cs       ← PRESERVED
│   ├── DPIAAssessmentExpired.cs         ← PRESERVED
│   └── DPOConsultationRequested.cs      ← PRESERVED
├── ReadModels/
│   ├── DPIAReadModel.cs                 ← NEW (Marten inline projection target)
│   └── DPIAProjection.cs               ← NEW (event → read model)
├── RiskCriteria/                        ← PRESERVED (all 6 criteria unchanged)
├── Services/
│   └── DefaultDPIAService.cs            ← NEW (aggregate lifecycle orchestrator)
├── DefaultDPIAAssessmentEngine.cs       ← MODIFIED (remove store/audit deps)
├── DefaultDPIATemplateProvider.cs       ← PRESERVED
├── DPIAAutoDetector.cs                  ← PRESERVED
├── DPIAAutoRegistrationDescriptor.cs    ← PRESERVED
├── DPIAAutoRegistrationHostedService.cs ← MODIFIED (use IDPIAService)
├── DPIAErrors.cs                        ← MODIFIED (update for service errors)
├── DPIAMartenExtensions.cs              ← NEW (Marten aggregate registration)
├── DPIAOptions.cs                       ← MODIFIED (remove TrackAuditTrail)
├── DPIAOptionsValidator.cs              ← PRESERVED
├── DPIARequiredPipelineBehavior.cs      ← MODIFIED (use IDPIAService)
├── DPIAReviewReminderService.cs         ← MODIFIED (use IDPIAService)
├── ServiceCollectionExtensions.cs       ← MODIFIED (remove InMemory registrations)
├── PublicAPI.Shipped.txt                ← MODIFIED
└── PublicAPI.Unshipped.txt              ← MODIFIED
```

### Files to DELETE

**Satellite Provider Stores (25 files)**:
- `src/Encina.ADO.{Sqlite,SqlServer,PostgreSQL,MySQL}/DPIA/DPIAStoreADO.cs` (4)
- `src/Encina.ADO.{Sqlite,SqlServer,PostgreSQL,MySQL}/DPIA/DPIAAuditStoreADO.cs` (4)
- `src/Encina.Dapper.{Sqlite,SqlServer,PostgreSQL,MySQL}/DPIA/DPIAStoreDapper.cs` (4)
- `src/Encina.Dapper.{Sqlite,SqlServer,PostgreSQL,MySQL}/DPIA/DPIAAuditStoreDapper.cs` (4)
- `src/Encina.EntityFrameworkCore/DPIA/DPIAStoreEF.cs` (1)
- `src/Encina.EntityFrameworkCore/DPIA/DPIAAuditStoreEF.cs` (1)
- `src/Encina.EntityFrameworkCore/DPIA/DPIAAssessmentEntityConfiguration.cs` (1)
- `src/Encina.EntityFrameworkCore/DPIA/DPIAAuditEntryEntityConfiguration.cs` (1)
- `src/Encina.EntityFrameworkCore/DPIA/DPIAModelBuilderExtensions.cs` (1)
- `src/Encina.MongoDB/DPIA/DPIAStoreMongoDB.cs` (1)
- `src/Encina.MongoDB/DPIA/DPIAAuditStoreMongoDB.cs` (1)
- `src/Encina.MongoDB/DPIA/DPIAAssessmentDocument.cs` (1)
- `src/Encina.MongoDB/DPIA/DPIAAuditEntryDocument.cs` (1)

**SQL Schema Scripts (16 files)**:
- `src/Encina.{ADO,Dapper}.{Sqlite,SqlServer,PostgreSQL,MySQL}/Scripts/023_CreateDPIAAssessmentsTable.sql` (8)
- `src/Encina.{ADO,Dapper}.{Sqlite,SqlServer,PostgreSQL,MySQL}/Scripts/024_CreateDPIAAuditEntriesTable.sql` (8)

**Core Module (6 files)**:
- `src/Encina.Compliance.DPIA/IDPIAStore.cs`
- `src/Encina.Compliance.DPIA/IDPIAAuditStore.cs`
- `src/Encina.Compliance.DPIA/InMemoryDPIAStore.cs`
- `src/Encina.Compliance.DPIA/InMemoryDPIAAuditStore.cs`
- `src/Encina.Compliance.DPIA/DPIAAssessmentEntity.cs`
- `src/Encina.Compliance.DPIA/DPIAAuditEntryEntity.cs`
- `src/Encina.Compliance.DPIA/DPIAAssessmentMapper.cs`
- `src/Encina.Compliance.DPIA/DPIAAuditEntryMapper.cs`

---

## Implementation Phases (5 Phases)

### Phase 1: Aggregate, Events & Read Model (~5 new files, ~300 lines)

<details>
<summary><strong>Tasks</strong></summary>

**1.1 Create `Events/DPIAEvents.cs`**
- Namespace: `Encina.Compliance.DPIA.Events`
- All events are `sealed record` implementing `INotification`
- Events:
  - `DPIACreated(Guid AssessmentId, string RequestTypeName, string? ProcessingType, string? Reason, DateTimeOffset OccurredAtUtc, string? TenantId, string? ModuleId)`
  - `DPIAEvaluated(Guid AssessmentId, RiskLevel OverallRisk, IReadOnlyList<RiskItem> IdentifiedRisks, IReadOnlyList<Mitigation> ProposedMitigations, bool RequiresPriorConsultation, DateTimeOffset OccurredAtUtc)`
  - `DPIADPOConsultationRequested(Guid AssessmentId, Guid ConsultationId, string DPOName, string DPOEmail, DateTimeOffset OccurredAtUtc)`
  - `DPIADPOResponded(Guid AssessmentId, Guid ConsultationId, DPOConsultationDecision Decision, string? Comments, IReadOnlyList<string>? Conditions, DateTimeOffset OccurredAtUtc)`
  - `DPIAApproved(Guid AssessmentId, string ApprovedBy, DateTimeOffset? NextReviewAtUtc, DateTimeOffset OccurredAtUtc)`
  - `DPIARejected(Guid AssessmentId, string RejectedBy, string Reason, DateTimeOffset OccurredAtUtc)`
  - `DPIARevisionRequested(Guid AssessmentId, string RequestedBy, string Reason, DateTimeOffset OccurredAtUtc)`
  - `DPIAExpired(Guid AssessmentId, DateTimeOffset OccurredAtUtc)`

**1.2 Create `Aggregates/DPIAAggregate.cs`**
- Namespace: `Encina.Compliance.DPIA.Aggregates`
- Extends `AggregateBase`
- State properties (all `private set`): `RequestTypeName`, `ProcessingType`, `Reason`, `Status`, `Result`, `DPOConsultation`, `ApprovedAtUtc`, `NextReviewAtUtc`, `TenantId`, `ModuleId`
- Factory method: `static DPIAAggregate Create(Guid id, string requestTypeName, string? processingType, string? reason, DateTimeOffset occurredAtUtc, string? tenantId, string? moduleId)`
- Command methods: `Evaluate(...)`, `RequestDPOConsultation(...)`, `RecordDPOResponse(...)`, `Approve(...)`, `Reject(...)`, `RequestRevision(...)`, `Expire()`
- `protected override void Apply(object domainEvent)` with switch on all event types
- `public bool IsCurrent(DateTimeOffset nowUtc)` query method

**1.3 Create `ReadModels/DPIAReadModel.cs`**
- Namespace: `Encina.Compliance.DPIA.ReadModels`
- `sealed class` implementing `IReadModel`
- Properties mirror aggregate state with `{ get; set; }` (mutable for projection)
- Includes `LastModifiedAtUtc`, `Version`
- `IsCurrent(DateTimeOffset nowUtc)` query method

**1.4 Create `ReadModels/DPIAProjection.cs`**
- Namespace: `Encina.Compliance.DPIA.ReadModels`
- Implements `IProjection<DPIAReadModel>`, `IProjectionCreator<DPIACreated, DPIAReadModel>`, `IProjectionHandler<TEvent, DPIAReadModel>` for each subsequent event
- `ProjectionName => "DPIAProjection"`
- `Create(DPIACreated)` → new read model
- `Apply(DPIAEvaluated, ...)`, `Apply(DPIAApproved, ...)`, etc.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
CONTEXT:
You are migrating the Encina.Compliance.DPIA module from entity-based persistence to Marten event sourcing.
This follows the exact pattern established by CrossBorderTransfer, Consent, DSR, LawfulBasis, and BreachNotification.

TASK:
Create 4 new files in src/Encina.Compliance.DPIA/:

1. Events/DPIAEvents.cs — All domain events as sealed records implementing INotification (from MediatR).
   Events: DPIACreated, DPIAEvaluated, DPIADPOConsultationRequested, DPIADPOResponded, DPIAApproved,
   DPIARejected, DPIARevisionRequested, DPIAExpired.
   Each event includes OccurredAtUtc. DPIACreated includes TenantId/ModuleId.
   Include XML docs with GDPR article references.

2. Aggregates/DPIAAggregate.cs — Event-sourced aggregate extending AggregateBase.
   Properties (private set): RequestTypeName, ProcessingType, Reason, Status (DPIAAssessmentStatus),
   Result (DPIAResult?), DPOConsultation (DPOConsultation?), ApprovedAtUtc, NextReviewAtUtc, TenantId, ModuleId.
   Factory: static Create() method. Commands: Evaluate(), RequestDPOConsultation(),
   RecordDPOResponse(), Approve(), Reject(), RequestRevision(), Expire().
   Apply() handles all event types. Guard clauses enforce invariants (e.g., can't approve if not evaluated).
   IsCurrent(nowUtc) query method (same logic as current DPIAAssessment.IsCurrent).

3. ReadModels/DPIAReadModel.cs — sealed class implementing IReadModel with mutable properties.
   Mirrors aggregate state. Includes LastModifiedAtUtc, Version. IsCurrent() query method.

4. ReadModels/DPIAProjection.cs — Implements IProjection<DPIAReadModel> +
   IProjectionCreator<DPIACreated, DPIAReadModel> + IProjectionHandler for each event type.

KEY RULES:
- .NET 10 / C# 14, nullable enabled
- All events: sealed record, implement INotification
- Aggregate: extends AggregateBase (from Encina.DomainModeling)
- Use ArgumentException.ThrowIfNullOrWhiteSpace for parameter validation
- Use InvalidOperationException for invariant violations
- Status transitions: Draft → InReview → Approved/Rejected/RequiresRevision → Expired
- Include comprehensive XML documentation

REFERENCE FILES:
- src/Encina.Compliance.CrossBorderTransfer/Aggregates/ApprovedTransferAggregate.cs
- src/Encina.Compliance.CrossBorderTransfer/Events/ApprovedTransferEvents.cs
- src/Encina.Compliance.CrossBorderTransfer/ReadModels/ApprovedTransferReadModel.cs
- src/Encina.Compliance.CrossBorderTransfer/ReadModels/ApprovedTransferProjection.cs (if exists)
- src/Encina.Compliance.DPIA/Model/DPIAAssessment.cs (current entity to inform aggregate design)
- src/Encina.Compliance.DPIA/Model/DPIAAssessmentStatus.cs
- src/Encina.Compliance.DPIA/Model/DPIAResult.cs
- src/Encina.Compliance.DPIA/Model/DPOConsultation.cs
```

</details>

---

### Phase 2: Service Layer, DI & Marten Registration (~3 new files, ~400 lines)

<details>
<summary><strong>Tasks</strong></summary>

**2.1 Create `Abstractions/IDPIAService.cs`**
- Namespace: `Encina.Compliance.DPIA`
- Public interface with ROP return types (`ValueTask<Either<EncinaError, T>>`)
- Write operations:
  - `CreateAssessmentAsync(requestTypeName, processingType?, reason?, tenantId?, moduleId?, ct)` → `Guid`
  - `EvaluateAssessmentAsync(assessmentId, context, ct)` → `DPIAResult`
  - `RequestDPOConsultationAsync(assessmentId, ct)` → `DPOConsultation`
  - `RecordDPOResponseAsync(assessmentId, consultationId, decision, comments?, conditions?, ct)` → `Unit`
  - `ApproveAssessmentAsync(assessmentId, approvedBy, nextReviewAtUtc?, ct)` → `Unit`
  - `RejectAssessmentAsync(assessmentId, rejectedBy, reason, ct)` → `Unit`
  - `RequestRevisionAsync(assessmentId, requestedBy, reason, ct)` → `Unit`
  - `ExpireAssessmentAsync(assessmentId, ct)` → `Unit`
- Read operations:
  - `GetAssessmentAsync(assessmentId, ct)` → `DPIAReadModel`
  - `GetAssessmentByRequestTypeAsync(requestTypeName, ct)` → `Option<DPIAReadModel>`
  - `GetExpiredAssessmentsAsync(nowUtc, ct)` → `IReadOnlyList<DPIAReadModel>`
  - `GetAllAssessmentsAsync(ct)` → `IReadOnlyList<DPIAReadModel>`
  - `GetAssessmentHistoryAsync(assessmentId, ct)` → `IReadOnlyList<object>`

**2.2 Create `Services/DefaultDPIAService.cs`**
- Namespace: `Encina.Compliance.DPIA.Services`
- `internal sealed class` implementing `IDPIAService`
- Dependencies: `IAggregateRepository<DPIAAggregate>`, `IReadModelRepository<DPIAReadModel>`, `IDPIAAssessmentEngine`, `ICacheProvider`, `TimeProvider`, `IOptions<DPIAOptions>`, `ILogger<DefaultDPIAService>`
- Write methods: load aggregate → execute command → save → invalidate cache → return
- Read methods: query read model repo with cache-aside
- Cache key pattern: `"dpia:{assessmentId}"`, `"dpia:type:{requestTypeName}"`
- Exception handling: `ArgumentException` → domain error, `InvalidOperationException` → business error, general → store error
- Fire-and-forget cache invalidation

**2.3 Create `DPIAMartenExtensions.cs`**
- Namespace: `Encina.Compliance.DPIA`
- `public static IServiceCollection AddDPIAAggregates(this IServiceCollection services)`
- Registers `AddAggregateRepository<DPIAAggregate>()`
- Registers `AddProjection<DPIAProjection, DPIAReadModel>()`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
CONTEXT:
Continuing the DPIA Marten migration. Phase 1 created DPIAAggregate, DPIAEvents, DPIAReadModel, and DPIAProjection.
Now create the service layer that orchestrates aggregate operations.

TASK:
Create 3 new files:

1. Abstractions/IDPIAService.cs — Public service interface.
   Write operations: CreateAssessmentAsync, EvaluateAssessmentAsync, RequestDPOConsultationAsync,
   RecordDPOResponseAsync, ApproveAssessmentAsync, RejectAssessmentAsync, RequestRevisionAsync,
   ExpireAssessmentAsync. All return ValueTask<Either<EncinaError, T>>.
   Read operations: GetAssessmentAsync, GetAssessmentByRequestTypeAsync, GetExpiredAssessmentsAsync,
   GetAllAssessmentsAsync, GetAssessmentHistoryAsync.
   Parameters include tenantId?, moduleId?, CancellationToken.

2. Services/DefaultDPIAService.cs — internal sealed class implementing IDPIAService.
   Dependencies: IAggregateRepository<DPIAAggregate>, IReadModelRepository<DPIAReadModel>,
   IDPIAAssessmentEngine, ICacheProvider, TimeProvider, IOptions<DPIAOptions>, ILogger.
   Pattern: try-catch with ROP. Load aggregate → execute domain method → save → invalidate cache.
   For EvaluateAssessmentAsync: call IDPIAAssessmentEngine.AssessAsync, then aggregate.Evaluate(result).
   For DPO consultation: resolve DPO contact from options or IDataProtectionOfficer (optional dep).
   Cache keys: "dpia:{id}" and "dpia:type:{requestTypeName}".

3. DPIAMartenExtensions.cs — AddDPIAAggregates() extension method.
   Registers IAggregateRepository<DPIAAggregate> and AddProjection<DPIAProjection, DPIAReadModel>.

KEY RULES:
- Follow exact pattern from DefaultApprovedTransferService in CrossBorderTransfer
- ROP: Either<EncinaError, T> everywhere, use Match/MatchAsync
- Cache invalidation is fire-and-forget
- Log at Debug level on entry, structured [LoggerMessage] for outcomes
- Increment DPIADiagnostics counters on success

REFERENCE FILES:
- src/Encina.Compliance.CrossBorderTransfer/Abstractions/IApprovedTransferService.cs
- src/Encina.Compliance.CrossBorderTransfer/Services/DefaultApprovedTransferService.cs
- src/Encina.Compliance.CrossBorderTransfer/CrossBorderTransferMartenExtensions.cs
- src/Encina.Compliance.DPIA/DefaultDPIAAssessmentEngine.cs (current engine to integrate)
- src/Encina.Compliance.DPIA/DPIAErrors.cs
- src/Encina.Compliance.DPIA/DPIAOptions.cs
```

</details>

---

### Phase 3: Modify Existing Components (~10 files modified, ~47 files deleted)

<details>
<summary><strong>Tasks</strong></summary>

**3.1 Modify `DefaultDPIAAssessmentEngine.cs`**
- Remove `IDPIAStore` and `IDPIAAuditStore` constructor dependencies
- Remove `_store` and `_auditStore` fields
- Remove `RecordAssessmentAuditAsync` method (audit is now via events)
- Remove `RecordConsultationAuditAsync` method
- Remove `CreateConsultationAsync` method (moved to service)
- `AssessAsync` becomes pure: evaluates criteria, returns `DPIAResult` (no persistence side-effects)
- `RequestDPOConsultationAsync` → REMOVE from engine (moved to `IDPIAService`)
- Keep `RequiresDPIAAsync` (attribute check, no persistence needed)

**3.2 Modify `IDPIAAssessmentEngine.cs`**
- Remove `RequestDPOConsultationAsync` method (moved to `IDPIAService`)
- Keep `AssessAsync` and `RequiresDPIAAsync`

**3.3 Modify `DPIARequiredPipelineBehavior.cs`**
- Replace `IDPIAStore _store` dependency with `IDPIAService _service`
- Update `GetAssessmentAsync` call to `_service.GetAssessmentByRequestTypeAsync`
- Adapt to `DPIAReadModel` return type instead of `DPIAAssessment`
- Read model has same `IsCurrent()` method, minimal adaptation needed

**3.4 Modify `DPIAReviewReminderService.cs`**
- Replace `IDPIAStore` resolution with `IDPIAService`
- Update `GetExpiredAssessmentsAsync` call
- Update published notification to use read model fields

**3.5 Modify `DPIAAutoRegistrationHostedService.cs`**
- Replace `IDPIAStore` dependency with `IDPIAService`
- Change `SaveAssessmentAsync` to `CreateAssessmentAsync`
- Adapt to service return type (ROP Either)

**3.6 Modify `ServiceCollectionExtensions.cs`**
- Remove `TryAddSingleton<IDPIAStore, InMemoryDPIAStore>()`
- Remove `TryAddSingleton<IDPIAAuditStore, InMemoryDPIAAuditStore>()`
- Add `TryAddScoped<IDPIAService, DefaultDPIAService>()`
- Update `IDPIAAssessmentEngine` registration (now Scoped, fewer deps)

**3.7 Modify `DPIAOptions.cs`**
- Remove `TrackAuditTrail` property (inherent in event sourcing)

**3.8 Modify `Health/DPIAHealthCheck.cs`**
- Replace store/audit store resolvability checks with `IDPIAService` resolvability
- Check `IAggregateRepository<DPIAAggregate>` is resolvable (Marten connectivity)

**3.9 Modify `DPIAErrors.cs`**
- Add service-level errors: `ServiceError(operation, message, exception?)`
- Keep existing error codes

**3.10 Delete core files**
- `IDPIAStore.cs`, `IDPIAAuditStore.cs`
- `InMemoryDPIAStore.cs`, `InMemoryDPIAAuditStore.cs`
- `DPIAAssessmentEntity.cs`, `DPIAAuditEntryEntity.cs`
- `DPIAAssessmentMapper.cs`, `DPIAAuditEntryMapper.cs`

**3.11 Delete satellite provider files (25 store files)**
- All DPIA store/audit store files in ADO (8), Dapper (8), EF Core (5), MongoDB (4)

**3.12 Delete SQL schema scripts (16 files)**
- All `023_CreateDPIAAssessmentsTable.sql` and `024_CreateDPIAAuditEntriesTable.sql`

**3.13 Update satellite `ServiceCollectionExtensions.cs` (10 packages)**
- Remove DPIA store registrations from each satellite package's DI

**3.14 Update `PublicAPI.Unshipped.txt`**
- Remove entries for deleted types (IDPIAStore, IDPIAAuditStore, InMemory*, Entity*, Mapper*)
- Add entries for new types (DPIAAggregate, all events, DPIAReadModel, DPIAProjection, IDPIAService, DPIAMartenExtensions)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
CONTEXT:
Continuing the DPIA Marten migration. Phases 1-2 created the aggregate, events, read model, projection,
service interface/implementation, and Marten extensions. Now modify existing components to use the new
service-based architecture and delete obsolete files.

TASK:
A) MODIFY existing files (10 files):

1. DefaultDPIAAssessmentEngine.cs — Remove IDPIAStore, IDPIAAuditStore deps. Remove RequestDPOConsultationAsync,
   RecordAssessmentAuditAsync, RecordConsultationAuditAsync, CreateConsultationAsync. Keep AssessAsync (pure),
   RequiresDPIAAsync. Constructor now: criteria, templateProvider, options, timeProvider, logger, dpo?.

2. Abstractions/IDPIAAssessmentEngine.cs — Remove RequestDPOConsultationAsync. Keep AssessAsync, RequiresDPIAAsync.

3. DPIARequiredPipelineBehavior.cs — Replace IDPIAStore with IDPIAService. Use GetAssessmentByRequestTypeAsync.
   Adapt to DPIAReadModel return type.

4. DPIAReviewReminderService.cs — Replace IDPIAStore with IDPIAService. Use GetExpiredAssessmentsAsync,
   ExpireAssessmentAsync. Adapt notification publishing.

5. DPIAAutoRegistrationHostedService.cs — Replace IDPIAStore with IDPIAService. Use CreateAssessmentAsync.

6. ServiceCollectionExtensions.cs — Remove InMemory store registrations. Add IDPIAService registration.
   Update IDPIAAssessmentEngine registration.

7. DPIAOptions.cs — Remove TrackAuditTrail property.

8. Health/DPIAHealthCheck.cs — Check IDPIAService resolvability instead of store/audit store.

9. DPIAErrors.cs — Add ServiceError factory method.

10. PublicAPI.Unshipped.txt — Remove deleted type entries, add new type entries.

B) DELETE files:
- Core: IDPIAStore.cs, IDPIAAuditStore.cs, InMemoryDPIAStore.cs, InMemoryDPIAAuditStore.cs,
  DPIAAssessmentEntity.cs, DPIAAuditEntryEntity.cs, DPIAAssessmentMapper.cs, DPIAAuditEntryMapper.cs
- ADO stores: src/Encina.ADO.{Sqlite,SqlServer,PostgreSQL,MySQL}/DPIA/ (all files)
- Dapper stores: src/Encina.Dapper.{Sqlite,SqlServer,PostgreSQL,MySQL}/DPIA/ (all files)
- EF Core: src/Encina.EntityFrameworkCore/DPIA/ (all files)
- MongoDB: src/Encina.MongoDB/DPIA/ (all files)
- SQL scripts: all 023_CreateDPIAAssessmentsTable.sql and 024_CreateDPIAAuditEntriesTable.sql

C) UPDATE satellite ServiceCollectionExtensions.cs (10 files) — Remove DPIA store registrations.

KEY RULES:
- Pre-1.0: no backward compatibility needed, clean deletion
- Pipeline behavior still uses ConcurrentDictionary attribute cache
- Review reminder still uses PeriodicTimer + IServiceScopeFactory
- DPIAReadModel.IsCurrent() replaces DPIAAssessment.IsCurrent()
- Follow established patterns from BreachNotification migration (#780)

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/ (recently migrated, shows diff pattern)
- Recent commits: 535d2db (BreachNotification), a945d1b (LawfulBasis)
- src/Encina.Compliance.DPIA/ (all current files to modify)
```

</details>

---

### Phase 4: Testing (~30 test files modified/created/deleted)

<details>
<summary><strong>Tasks</strong></summary>

**4.1 Unit Tests — NEW/MODIFY**

Create/update in `tests/Encina.UnitTests/Compliance/DPIA/`:

- **NEW** `Aggregates/DPIAAggregateTests.cs` — Test aggregate lifecycle:
  - `Create_ValidParams_RaisesCreatedEvent`
  - `Evaluate_WhenDraft_RaisesEvaluatedEvent`
  - `Evaluate_WhenNotDraft_ThrowsInvalidOperation`
  - `RequestDPOConsultation_WhenEvaluated_RaisesEvent`
  - `Approve_WhenInReview_RaisesApprovedEvent`
  - `Reject_WhenInReview_RaisesRejectedEvent`
  - `Expire_WhenApproved_RaisesExpiredEvent`
  - `IsCurrent_ApprovedAndNotExpired_ReturnsTrue`
  - Status transition invariant tests

- **NEW** `ReadModels/DPIAProjectionTests.cs` — Test projection:
  - `Create_FromDPIACreated_ReturnsReadModel`
  - `Apply_DPIAEvaluated_UpdatesResult`
  - `Apply_DPIAApproved_UpdatesStatus`
  - Version incrementing, LastModifiedAtUtc updates

- **NEW** `Services/DefaultDPIAServiceTests.cs` — Test service (mock repository + cache):
  - `CreateAssessmentAsync_ValidParams_ReturnsId`
  - `EvaluateAssessmentAsync_CallsEngine_AppliesResult`
  - `ApproveAssessmentAsync_UpdatesAggregate`
  - `GetAssessmentByRequestTypeAsync_CacheMiss_QueriesRepo`
  - `GetAssessmentByRequestTypeAsync_CacheHit_ReturnsCached`
  - Error path tests (load failure, save failure)

- **MODIFY** `DefaultDPIAAssessmentEngineTests.cs` — Remove store/audit dependencies from constructor setup. Remove DPO consultation tests (moved to service). Keep risk evaluation tests.

- **MODIFY** `DPIARequiredPipelineBehaviorTests.cs` — Replace mock IDPIAStore with mock IDPIAService. Update to use DPIAReadModel.

- **MODIFY** `DPIAReviewReminderServiceTests.cs` — Replace mock IDPIAStore with mock IDPIAService.

- **MODIFY** `DPIAHealthCheckTests.cs` — Update to check IDPIAService resolvability.

- **DELETE** `InMemoryDPIAStoreTests.cs` — Store removed
- **DELETE** `InMemoryDPIAAuditStoreTests.cs` — Audit store removed
- **DELETE** `DPIAAssessmentMapperTests.cs` — Mapper removed
- **DELETE** `DPIAAuditEntryMapperTests.cs` — Mapper removed

**4.2 Guard Tests — MODIFY/DELETE**

In `tests/Encina.GuardTests/Compliance/DPIA/`:

- **NEW** `DPIAAggregateGuardTests.cs` — Null checks on factory + command methods
- **NEW** `DefaultDPIAServiceGuardTests.cs` — Constructor null checks
- **DELETE** `InMemoryDPIAStoreGuardTests.cs`
- **DELETE** `InMemoryDPIAAuditStoreGuardTests.cs`
- **DELETE** `DPIAAssessmentMapperGuardTests.cs`
- **DELETE** `DPIAAuditEntryMapperGuardTests.cs`
- **MODIFY** `DefaultDPIAAssessmentEngineGuardTests.cs` — Remove store/audit params
- **MODIFY** `DPIARequiredPipelineBehaviorGuardTests.cs` — Replace IDPIAStore with IDPIAService

**4.3 Contract Tests — MODIFY/DELETE**

In `tests/Encina.ContractTests/Compliance/DPIA/`:

- **DELETE** `IDPIAStoreContractTests.cs` — Store removed
- **DELETE** `IDPIAAuditStoreContractTests.cs` — Audit store removed
- **DELETE** `DPIAStoreProviderContractTests.cs` — Provider contract removed
- **NEW** `IDPIAServiceContractTests.cs` — Verify service interface contract

**4.4 Property Tests — MODIFY/DELETE**

In `tests/Encina.PropertyTests/Compliance/DPIA/`:

- **MODIFY** `DPIAAssessmentPropertyTests.cs` — Update for aggregate (event replay consistency)
- **DELETE** `InMemoryDPIAStorePropertyTests.cs`
- **DELETE** `InMemoryDPIAAuditStorePropertyTests.cs`
- **NEW** `DPIAAggregatePropertyTests.cs` — FsCheck: status transitions are deterministic, event replay is idempotent

**4.5 Integration Tests — DELETE**

In `tests/Encina.IntegrationTests/`:

- **DELETE** all 13 provider-specific DPIA store tests (ADO ×4, Dapper ×4, EF Core ×4, MongoDB ×1)
- **DELETE** `Compliance/DPIA/DPIAPipelineIntegrationTests.cs` (will be recreated when Marten infra is available)

**4.6 Justification Files — PRESERVE**

- `tests/Encina.LoadTests/Compliance/DPIA/DPIA.md` — Keep (still applicable)
- `tests/Encina.BenchmarkTests/Compliance/DPIA/DPIA.md` — Keep (still applicable)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
CONTEXT:
The DPIA module has been migrated to Marten event sourcing in Phases 1-3. Now update all test projects
to match the new architecture. The old entity-based tests need to be replaced with aggregate/service tests.

TASK:
Update tests across 5 test projects:

A) UNIT TESTS (tests/Encina.UnitTests/Compliance/DPIA/):
- CREATE: Aggregates/DPIAAggregateTests.cs, ReadModels/DPIAProjectionTests.cs, Services/DefaultDPIAServiceTests.cs
- MODIFY: DefaultDPIAAssessmentEngineTests.cs, DPIARequiredPipelineBehaviorTests.cs,
  DPIAReviewReminderServiceTests.cs, DPIAHealthCheckTests.cs
- DELETE: InMemoryDPIAStoreTests.cs, InMemoryDPIAAuditStoreTests.cs, DPIAAssessmentMapperTests.cs,
  DPIAAuditEntryMapperTests.cs

B) GUARD TESTS (tests/Encina.GuardTests/Compliance/DPIA/):
- CREATE: DPIAAggregateGuardTests.cs, DefaultDPIAServiceGuardTests.cs
- MODIFY: DefaultDPIAAssessmentEngineGuardTests.cs, DPIARequiredPipelineBehaviorGuardTests.cs
- DELETE: InMemoryDPIAStoreGuardTests.cs, InMemoryDPIAAuditStoreGuardTests.cs,
  DPIAAssessmentMapperGuardTests.cs, DPIAAuditEntryMapperGuardTests.cs

C) CONTRACT TESTS (tests/Encina.ContractTests/Compliance/DPIA/):
- DELETE: IDPIAStoreContractTests.cs, IDPIAAuditStoreContractTests.cs, DPIAStoreProviderContractTests.cs
- CREATE: IDPIAServiceContractTests.cs

D) PROPERTY TESTS (tests/Encina.PropertyTests/Compliance/DPIA/):
- DELETE: InMemoryDPIAStorePropertyTests.cs, InMemoryDPIAAuditStorePropertyTests.cs
- CREATE: DPIAAggregatePropertyTests.cs (event replay idempotency, status transition invariants)
- MODIFY: DPIAAssessmentPropertyTests.cs

E) INTEGRATION TESTS (tests/Encina.IntegrationTests/):
- DELETE: All 14 DPIA integration test files (13 provider-specific + 1 pipeline)

KEY RULES:
- Mock IAggregateRepository<DPIAAggregate> and IReadModelRepository<DPIAReadModel> using NSubstitute
- Mock ICacheProvider using NSubstitute
- AAA pattern (Arrange, Act, Assert)
- Descriptive test names: Method_Scenario_ExpectedBehavior
- Each test tests ONE thing
- No shared state between tests

REFERENCE FILES:
- tests/Encina.UnitTests/Compliance/BreachNotification/ (recently migrated tests)
- tests/Encina.UnitTests/Compliance/Consent/Aggregates/ (aggregate test pattern)
- tests/Encina.UnitTests/Compliance/CrossBorderTransfer/ (reference implementation tests)
```

</details>

---

### Phase 5: Observability, Documentation & Finalization

<details>
<summary><strong>Tasks</strong></summary>

**5.1 Update Diagnostics**

- **`DPIADiagnostics.cs`** — Add counters for new service operations:
  - `dpia.service.create.total`, `dpia.service.evaluate.total`, `dpia.service.approve.total`, etc.
  - Keep existing pipeline/assessment/reminder counters

- **`DPIALogMessages.cs`** — Add log messages for service operations:
  - EventId 8870-8879 (currently "Store operations") → repurpose for "Service operations"
  - Add: `ServiceCreateAssessment`, `ServiceEvaluateAssessment`, `ServiceApproveAssessment`, etc.
  - Add: `CacheHit`, `CacheMiss`, `CacheInvalidated` in 8875-8879 range

**5.2 Documentation**

- **`CHANGELOG.md`** — Add under Unreleased / Changed:
  ```
  #### Encina.Compliance.DPIA — Migrated to Marten event sourcing (#781)
  - Replaced IDPIAStore (13 provider implementations) with DPIAAggregate (event-sourced)
  - Replaced IDPIAAuditStore with implicit event stream audit trail
  - Added IDPIAService for aggregate lifecycle orchestration with caching
  - Added DPIAReadModel with Marten inline projection
  - Removed all satellite DPIA stores (ADO, Dapper, EF Core, MongoDB)
  ```

- **`ROADMAP.md`** — Mark DPIA migration as completed under v0.13.0

- **`docs/INVENTORY.md`** — Update file listings for DPIA module

- **`src/Encina.Compliance.DPIA/README.md`** — Update to reflect ES architecture

**5.3 Build Verification**

- `dotnet build Encina.slnx --configuration Release` → 0 errors, 0 warnings
- `dotnet test tests/Encina.UnitTests --filter "FullyQualifiedName~DPIA"` → all pass
- `dotnet test tests/Encina.GuardTests --filter "FullyQualifiedName~DPIA"` → all pass
- `dotnet test tests/Encina.ContractTests --filter "FullyQualifiedName~DPIA"` → all pass
- `dotnet test tests/Encina.PropertyTests --filter "FullyQualifiedName~DPIA"` → all pass

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
CONTEXT:
The DPIA Marten migration is functionally complete (Phases 1-4). Now finalize observability,
documentation, and build verification.

TASK:
1. Update Diagnostics:
   - DPIADiagnostics.cs: Add service-level Counter<long> metrics (create, evaluate, approve, reject, etc.)
   - DPIALogMessages.cs: Add [LoggerMessage] entries for service operations in EventId range 8870-8879.
     Add cache hit/miss/invalidation log messages.

2. Update Documentation:
   - CHANGELOG.md: Add entry under Unreleased for DPIA ES migration (#781)
   - ROADMAP.md: Mark DPIA migration complete under v0.13.0
   - docs/INVENTORY.md: Update file listings
   - src/Encina.Compliance.DPIA/README.md: Update architecture description

3. Build & Test Verification:
   - Run: dotnet build Encina.slnx --configuration Release
   - Run: dotnet test tests/Encina.UnitTests --filter "FullyQualifiedName~DPIA"
   - Run: dotnet test tests/Encina.GuardTests --filter "FullyQualifiedName~DPIA"
   - Fix any compilation errors or test failures

KEY RULES:
- EventId range 8800-8899 (DPIA reserved)
- [LoggerMessage] source generator pattern (not LoggerMessage.Define)
- CHANGELOG follows Keep a Changelog format
- 0 warnings, 0 errors on Release build

REFERENCE FILES:
- src/Encina.Compliance.CrossBorderTransfer/Diagnostics/
- src/Encina.Compliance.BreachNotification/Diagnostics/
- CHANGELOG.md (current format)
- ROADMAP.md
```

</details>

---

## Research

### GDPR Article References

| Article | Requirement | How ES Supports It |
|---------|------------|-------------------|
| Art. 35(1) | DPIA required for high-risk processing | Aggregate guards enforce assessment completion |
| Art. 35(2) | DPO must be consulted | `DPIADPOConsultationRequested` event proves consultation |
| Art. 35(7) | Assessment content requirements | `DPIAEvaluated` captures risks, mitigations, consultation need |
| Art. 35(11) | Periodic review requirement | `DPIAExpired` event + `DPIAReviewReminderService` |
| Art. 36 | Prior consultation with supervisory authority | `DPIAResult.RequiresPriorConsultation` flag |
| Art. 5(2) | Accountability principle | Event stream IS the complete audit trail |

### Existing Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| `AggregateBase` | `Encina.DomainModeling` | Base class for DPIAAggregate |
| `IAggregateRepository<T>` | `Encina.Marten` | Aggregate persistence |
| `IReadModelRepository<T>` | `Encina.Marten` | Read model queries |
| `IProjection<T>` | `Encina.Marten` | Projection interface |
| `ICacheProvider` | `Encina.Caching` | Cache-aside for read models |
| `INotification` | `MediatR` | Event publishing via `EventPublishingPipelineBehavior` |
| `IPipelineBehavior<,>` | `MediatR` | Pipeline enforcement |
| `Either<EncinaError, T>` | `LanguageExt` | Railway Oriented Programming |

### EventId Allocation

| Package | Range | Notes |
|---------|-------|-------|
| Encina.Compliance.GDPR | 8100-8199 | Foundation module |
| Encina.Compliance.Consent | 8200-8299 | Migrated to ES |
| Encina.Compliance.DataSubjectRights | 8300-8349 | Migrated to ES |
| Encina.Compliance.LawfulBasis | 8350-8399 | Migrated to ES |
| Encina.Compliance.Anonymization | 8400-8499 | Pre-migration |
| Encina.Compliance.CrossBorderTransfer | 8500-8599 | Migrated to ES |
| Encina.Compliance.DataResidency | 8600-8699 | Pre-migration |
| Encina.Compliance.BreachNotification | 8700-8799 | Migrated to ES |
| **Encina.Compliance.DPIA** | **8800-8899** | **This migration — PRESERVED** |

### Estimated File Count

| Category | Created | Modified | Deleted | Total |
|----------|---------|----------|---------|-------|
| Core source (aggregate, events, service) | 7 | 10 | 8 | 25 |
| Satellite providers | 0 | 10 | 25 | 35 |
| SQL scripts | 0 | 0 | 16 | 16 |
| Unit tests | 3 | 4 | 4 | 11 |
| Guard tests | 2 | 2 | 4 | 8 |
| Contract tests | 1 | 0 | 3 | 4 |
| Property tests | 1 | 1 | 2 | 4 |
| Integration tests | 0 | 0 | 14 | 14 |
| Documentation | 0 | 4 | 0 | 4 |
| **Total** | **14** | **31** | **76** | **121** |

---

## Combined AI Agent Prompt

<details>
<summary><strong>Complete Implementation Prompt (All Phases)</strong></summary>

```
PROJECT CONTEXT:
You are migrating the Encina.Compliance.DPIA module from entity-based persistence (13 database providers)
to Marten event sourcing. This is issue #781 in the Encina project.

The DPIA (Data Protection Impact Assessment) module is a GDPR Art. 35 compliance tool that:
- Evaluates processing operations for high-risk to data subjects
- Enforces DPIA requirements via pipeline behavior with [RequiresDPIA] attribute
- Manages DPO consultation lifecycle
- Monitors assessment expiration for periodic review
- Auto-registers assessments from assembly scanning

IMPLEMENTATION OVERVIEW:
Phase 1: Create DPIAAggregate, DPIAEvents (8 event types), DPIAReadModel, DPIAProjection
Phase 2: Create IDPIAService interface, DefaultDPIAService implementation, DPIAMartenExtensions
Phase 3: Modify engine/pipeline/reminder/health/DI; delete IDPIAStore, IDPIAAuditStore, InMemory stores,
         all 25 satellite provider stores, 16 SQL scripts, entity/mapper files
Phase 4: Update all 5 test projects (create aggregate/service tests, delete store tests)
Phase 5: Update diagnostics, CHANGELOG, ROADMAP, INVENTORY, README, PublicAPI files

KEY PATTERNS (from CrossBorderTransfer reference):
- Aggregate: extends AggregateBase, private set properties, static factory, command methods raise events
- Events: sealed record implementing INotification, include OccurredAtUtc
- Read Model: sealed class implementing IReadModel, mutable properties for projection
- Service: internal sealed class, deps = IAggregateRepository + IReadModelRepository + ICacheProvider + TimeProvider
- DI: TryAddScoped for services, separate AddDPIAAggregates() for Marten registration
- Health: check IDPIAService + IAggregateRepository resolvability
- Cache: key = "dpia:{id}" and "dpia:type:{requestTypeName}", fire-and-forget invalidation

DPIA-SPECIFIC COMPLEXITY:
- Assessment engine (DefaultDPIAAssessmentEngine) stays as pure risk evaluator
- Pipeline behavior (DPIARequiredPipelineBehavior) switches from IDPIAStore to IDPIAService
- Review reminder (DPIAReviewReminderService) switches from IDPIAStore to IDPIAService
- Auto-registration (DPIAAutoRegistrationHostedService) switches from IDPIAStore to IDPIAService
- Risk criteria (6 implementations of IRiskCriterion) are PRESERVED unchanged
- Template provider is PRESERVED unchanged
- Value objects in Model/ are PRESERVED unchanged (DPIAResult, DPIAContext, RiskItem, Mitigation, etc.)

AGGREGATE STATUS TRANSITIONS:
Draft → (Evaluate) → InReview → (Approve) → Approved → (Expire) → Expired
                                → (Reject) → Rejected
                                → (RequestRevision) → RequiresRevision → (Evaluate again) → InReview

EVENT TYPES:
DPIACreated, DPIAEvaluated, DPIADPOConsultationRequested, DPIADPOResponded,
DPIAApproved, DPIARejected, DPIARevisionRequested, DPIAExpired

EVENTID RANGE: 8800-8899 (preserved, repurpose 8870-8879 for service operations)

REFERENCE FILES:
- src/Encina.Compliance.CrossBorderTransfer/ (complete reference implementation)
- src/Encina.Compliance.BreachNotification/ (recent migration)
- src/Encina.Compliance.DPIA/ (current implementation to migrate)
- docs/architecture/adr/019-compliance-event-sourcing-marten.md
- CLAUDE.md (project guidelines)
```

</details>

---

## Cross-Cutting Integration Matrix (per ADR-018)

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | **Caching** | ✅ Integrate | `ICacheProvider` in `DefaultDPIAService` for read model caching (cache-aside pattern) |
| 2 | **OpenTelemetry** | ✅ Integrate | Existing `DPIADiagnostics.ActivitySource` + `Meter` — add service-level counters |
| 3 | **Structured Logging** | ✅ Integrate | Existing `DPIALogMessages` EventId 8800-8899 — add service operation messages |
| 4 | **Health Checks** | ✅ Integrate | Existing `DPIAHealthCheck` — update to verify Marten/IDPIAService connectivity |
| 5 | **Validation** | ✅ Integrate | Aggregate command methods enforce invariants; `DPIAOptionsValidator` preserved |
| 6 | **Resilience** | ❌ N/A | Marten handles retry internally; no external system calls from DPIA service |
| 7 | **Distributed Locks** | ❌ N/A | Marten optimistic concurrency (aggregate Version) prevents duplicate operations |
| 8 | **Transactions** | ✅ Integrate | Marten `IDocumentSession.SaveChangesAsync()` is inherently transactional |
| 9 | **Idempotency** | ✅ Integrate | Marten aggregate version-based optimistic concurrency prevents duplicate events |
| 10 | **Multi-Tenancy** | ✅ Integrate | `TenantId` in aggregate, events, and read model (already in current design) |
| 11 | **Module Isolation** | ✅ Integrate | `ModuleId` in aggregate, events, and read model (already in current design) |
| 12 | **Audit Trail** | ✅ Integrate | Event stream IS the audit trail — `IDPIAAuditStore` removed, events are immutable facts |

---

## Risk Analysis

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Pipeline behavior regression | Medium | High | Comprehensive unit tests for DPIARequiredPipelineBehavior with IDPIAService mock |
| Review reminder service regression | Low | Medium | Unit tests verify IDPIAService interaction |
| Auto-registration regression | Low | Medium | Unit tests for DPIAAutoRegistrationHostedService |
| Satellite package compilation errors | Low | Low | Remove all DPIA references + build verification |
| Assessment engine behavior change | Low | High | Engine is simplified (fewer deps), keep all risk evaluation tests |

---

## Related Issues

- #776 — [SPIKE] ADR-019: Event Sourcing for Compliance Modules ✅
- #412 — CrossBorderTransfer (reference implementation) ✅
- #777 — Consent ES migration ✅
- #778 — DataSubjectRights ES migration ✅
- #779 — LawfulBasis ES migration ✅
- #780 — BreachNotification ES migration ✅
- #668 — EPIC v0.13.0 (parent)
- #409 — Original DPIA implementation
