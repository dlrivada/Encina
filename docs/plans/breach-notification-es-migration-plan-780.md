# Implementation Plan: Migrate `Encina.Compliance.BreachNotification` to Marten Event Sourcing

> **Issue**: [#780](https://github.com/dlrivada/Encina/issues/780)
> **Type**: Refactor
> **Complexity**: Medium-High (8 phases, ~33 files deleted, ~15 files created, ~25 files modified)
> **Estimated Scope**: ~1,500-2,000 lines of new/modified production code + ~1,000-1,500 lines of tests
> **Prerequisites**: #776 (ADR-019), #777 (Consent ES migration), #778 (DSR ES migration), #779 (LawfulBasis ES migration) — all completed

---

## Summary

Migrate `Encina.Compliance.BreachNotification` from entity-based persistence (13 database providers) to Marten event sourcing, following the established pattern from the Consent (#777), DataSubjectRights (#778), and LawfulBasis (#779) migrations.

The migration replaces `IBreachRecordStore` + `IBreachAuditStore` (with 27 provider implementations) with a single `BreachAggregate` event-sourced aggregate via Marten. Domain events (`BreachDetected`, `BreachAssessed`, `BreachReportedToDPA`, etc.) become the immutable audit trail, satisfying GDPR Art. 5(2) accountability. The `BreachReadModel` projection provides query-side reads with caching via `ICacheProvider`.

The detection engine (`IBreachDetector`, `IBreachDetectionRule`, `BreachDetectionPipelineBehavior`, detection rules) and the notification infrastructure (`IBreachNotifier`, `DefaultBreachNotifier`) are **preserved** — they are domain logic independent of persistence.

---

## Design Choices

<details>
<summary><strong>1. Aggregate Design — Single <code>BreachAggregate</code> covering full breach lifecycle</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Single `BreachAggregate`** | One aggregate tracks full lifecycle (detect → assess → report → notify → contain → close), mirrors `BreachRecord` state machine | Larger aggregate with more events |
| **B) Separate aggregates for detection vs notification** | Smaller aggregates, clearer bounded contexts | Artificial split — breach lifecycle is inherently one workflow; Art. 33/34 require timeline reconstruction across the full lifecycle |

### Chosen Option: **A — Single `BreachAggregate`**

### Rationale

- The breach lifecycle is a single workflow: detection → assessment → authority notification (Art. 33) → subject notification (Art. 34) → containment → closure
- GDPR requires demonstrating the full timeline during regulatory investigations — splitting aggregates would complicate timeline reconstruction
- Matches the existing `BreachRecord` state machine (6 statuses: Detected → Investigating → AuthorityNotified → SubjectsNotified → Resolved → Closed)
- Consistent with reference implementations (Consent has one aggregate covering grant → withdraw → renew → expire)

</details>

<details>
<summary><strong>2. Service Interface Design — New <code>IBreachNotificationService</code> replacing <code>IBreachHandler</code> + stores</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New `IBreachNotificationService`** combining command + query | Clean API, follows Consent/DSR/LawfulBasis pattern, single service for all operations | Breaking change (pre-1.0, acceptable) |
| **B) Keep `IBreachHandler` and adapt internally** | No API change | Leaks old entity-based abstractions, inconsistent with other migrated modules |
| **C) Keep `IBreachHandler` as facade over ES** | Minimal API disruption | Extra indirection layer, still references old store interfaces in some methods |

### Chosen Option: **A — New `IBreachNotificationService`**

### Rationale

- Consistent with `IConsentService`, `IDSRService`, `ILawfulBasisService` — all migrated modules use `I{Module}Service`
- Commands go through aggregate (write), queries go through read model (read) — CQRS separation
- `IBreachRecordStore` and `IBreachAuditStore` become unnecessary (aggregate + events replace both)
- Pre-1.0: breaking changes are expected and encouraged

</details>

<details>
<summary><strong>3. Audit Trail Strategy — ES events ARE the audit trail</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) ES events as audit trail** | Zero extra storage, immutable by design, complete timeline | Requires Marten event stream queries for audit reports |
| **B) Keep separate `IBreachAuditStore`** | Familiar API, independent query | Redundant with ES events, extra write on every operation, two sources of truth |

### Chosen Option: **A — ES events as audit trail**

### Rationale

- Event sourcing inherently provides a complete, immutable audit trail — every state change is an event with timestamp and actor
- `BreachAuditEntry` records become redundant: `BreachDetected`, `BreachAssessed`, etc. carry all the same information plus more
- Eliminates `IBreachAuditStore` and `InMemoryBreachAuditStore` entirely
- For audit queries, the service exposes `GetBreachHistoryAsync()` which reads the event stream
- Consistent with how Consent/DSR/LawfulBasis handle audit (events = audit)

</details>

<details>
<summary><strong>4. Detection Engine Preservation — Keep <code>IBreachDetector</code>, rules, and pipeline behavior unchanged</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Preserve detection engine as-is** | No regression risk, detection is persistence-agnostic, pipeline behavior is well-tested | Some coupling to `PotentialBreach` model |
| **B) Refactor detection into aggregate commands** | Fully event-sourced detection | Over-engineering — detection evaluates transient security events, not aggregate state; rules are stateless |

### Chosen Option: **A — Preserve detection engine as-is**

### Rationale

- `IBreachDetector`, `IBreachDetectionRule`, and the 4 built-in rules are stateless evaluators — they take a `SecurityEvent` and return `PotentialBreach[]`
- `BreachDetectionPipelineBehavior` intercepts requests and feeds them through the detector — this is pipeline logic, not persistence
- `DefaultBreachHandler.HandleDetectedBreachAsync()` created a `BreachRecord` from a `PotentialBreach` — this becomes `BreachAggregate.Detect()` factory method
- The detector outputs feed into the aggregate, but the detector itself doesn't need ES

</details>

<details>
<summary><strong>5. Phased Reports — Modeled as events within the aggregate</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `BreachPhasedReportAdded` event on aggregate** | Natural fit — phased reports are state changes on a breach; queryable via read model | Read model needs a collection property for reports |
| **B) Separate aggregate per phased report** | Independent lifecycle | Over-engineering — reports are child entities of a breach, not independent aggregates |

### Chosen Option: **A — Event on aggregate**

### Rationale

- Art. 33(4) allows phased reporting — each phase is an incremental update to the same breach
- `BreachPhasedReportAdded` event carries report content; `BreachReadModel.PhasedReports` list accumulates them
- Consistent with how `BreachRecord.PhasedReports` worked as a nested collection
- Aggregate validates that breach is in an appropriate state for additional reports

</details>

---

## Implementation Phases

### Phase 1: Events & Aggregate

> **Goal**: Create the event-sourced aggregate and domain events that model the breach lifecycle.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `Events/BreachNotificationEvents.cs`** — All domain events as sealed records implementing `INotification`:
   - `BreachDetected(Guid BreachId, string Nature, BreachSeverity Severity, string DetectedByRule, int EstimatedAffectedSubjects, string Description, string? DetectedByUserId, DateTimeOffset DetectedAtUtc, string? TenantId, string? ModuleId)`
   - `BreachAssessed(Guid BreachId, BreachSeverity UpdatedSeverity, int UpdatedAffectedSubjects, string AssessmentSummary, string AssessedByUserId, DateTimeOffset AssessedAtUtc, string? TenantId, string? ModuleId)`
   - `BreachReportedToDPA(Guid BreachId, string AuthorityName, string AuthorityContactInfo, string ReportSummary, string ReportedByUserId, DateTimeOffset ReportedAtUtc, string? TenantId, string? ModuleId)`
   - `BreachNotifiedToSubjects(Guid BreachId, int SubjectCount, string CommunicationMethod, SubjectNotificationExemption Exemption, string NotifiedByUserId, DateTimeOffset NotifiedAtUtc, string? TenantId, string? ModuleId)`
   - `BreachPhasedReportAdded(Guid BreachId, int PhaseNumber, string ReportContent, string SubmittedByUserId, DateTimeOffset SubmittedAtUtc, string? TenantId, string? ModuleId)`
   - `BreachContained(Guid BreachId, string ContainmentMeasures, string ContainedByUserId, DateTimeOffset ContainedAtUtc, string? TenantId, string? ModuleId)`
   - `BreachClosed(Guid BreachId, string ResolutionSummary, string ClosedByUserId, DateTimeOffset ClosedAtUtc, string? TenantId, string? ModuleId)`

2. **Create `Aggregates/BreachAggregate.cs`**:
   - Inherits from `AggregateBase`
   - **Properties**: `Nature`, `Severity`, `Status (BreachStatus)`, `EstimatedAffectedSubjects`, `Description`, `DetectedAtUtc`, `AssessedAtUtc?`, `ReportedToDPAAtUtc?`, `NotifiedSubjectsAtUtc?`, `ContainedAtUtc?`, `ClosedAtUtc?`, `AuthorityName?`, `SubjectCount`, `PhasedReportCount`, `DeadlineUtc`, `TenantId?`, `ModuleId?`
   - **Factory method**: `static BreachAggregate Detect(Guid id, string nature, BreachSeverity severity, string detectedByRule, int estimatedAffectedSubjects, string description, string? detectedByUserId, DateTimeOffset detectedAtUtc, string? tenantId, string? moduleId)` — raises `BreachDetected`, calculates `DeadlineUtc = detectedAtUtc.AddHours(72)`
   - **Behavior methods**:
     - `Assess(BreachSeverity updatedSeverity, int updatedAffectedSubjects, string assessmentSummary, string assessedByUserId, DateTimeOffset assessedAtUtc)` — valid from `Detected`; raises `BreachAssessed`, transitions to `Investigating`
     - `ReportToDPA(string authorityName, string authorityContactInfo, string reportSummary, string reportedByUserId, DateTimeOffset reportedAtUtc)` — valid from `Detected` or `Investigating`; raises `BreachReportedToDPA`, transitions to `AuthorityNotified`
     - `NotifySubjects(int subjectCount, string communicationMethod, SubjectNotificationExemption exemption, string notifiedByUserId, DateTimeOffset notifiedAtUtc)` — valid from `AuthorityNotified`; raises `BreachNotifiedToSubjects`, transitions to `SubjectsNotified`
     - `AddPhasedReport(string reportContent, string submittedByUserId, DateTimeOffset submittedAtUtc)` — valid when NOT `Closed`; raises `BreachPhasedReportAdded`
     - `Contain(string containmentMeasures, string containedByUserId, DateTimeOffset containedAtUtc)` — valid when NOT `Closed`; raises `BreachContained`
     - `Close(string resolutionSummary, string closedByUserId, DateTimeOffset closedAtUtc)` — valid from `SubjectsNotified` or `Resolved`; raises `BreachClosed`, transitions to `Closed`
   - **Apply method**: `protected override void Apply(object domainEvent)` — switch on event type, update state

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of Issue #780 — Migrate BreachNotification to Marten event sourcing.

CONTEXT:
- Encina is a .NET 10 / C# 14 library using Railway Oriented Programming (Either<EncinaError, T>)
- This is a REFACTOR of an existing package: src/Encina.Compliance.BreachNotification/
- Three compliance modules have already been migrated: Consent (#777), DataSubjectRights (#778), LawfulBasis (#779)
- Follow the EXACT patterns from those migrations
- All domain events are sealed records implementing INotification
- Aggregates inherit from AggregateBase (from Encina.Marten)
- Include TenantId and ModuleId on all events for multi-tenancy/module isolation
- BreachStatus enum already exists: Detected, Investigating, AuthorityNotified, SubjectsNotified, Resolved, Closed
- BreachSeverity enum already exists: Low, Medium, High, Critical
- SubjectNotificationExemption enum already exists
- DeadlineUtc = DetectedAtUtc + 72 hours (Art. 33(1))

TASK:
1. Create Events/BreachNotificationEvents.cs with 7 event records
2. Create Aggregates/BreachAggregate.cs with factory method, 6 behavior methods, and Apply

KEY RULES:
- Events are sealed records implementing INotification (from Encina core)
- Timestamps use DateTimeOffset with AtUtc suffix
- Aggregate validates state transitions (throw InvalidOperationException for invalid transitions)
- Factory method is static, behavior methods are instance
- Apply method uses switch expression on domainEvent type
- XML documentation on all public types with GDPR article references (Art. 33, 34, 5(2))
- Guard clauses: ArgumentNullException.ThrowIfNull, ArgumentException.ThrowIfNullOrWhiteSpace

REFERENCE FILES:
- src/Encina.Compliance.Consent/Aggregates/ConsentAggregate.cs (aggregate pattern)
- src/Encina.Compliance.Consent/Events/ConsentEvents.cs (events pattern)
- src/Encina.Compliance.DataSubjectRights/Aggregates/DSRRequestAggregate.cs (aggregate pattern)
- src/Encina.Compliance.BreachNotification/Model/BreachStatus.cs (existing enum)
- src/Encina.Compliance.BreachNotification/Model/BreachSeverity.cs (existing enum)
```

</details>

---

### Phase 2: Read Model & Projection

> **Goal**: Create the query-side read model and Marten projection for breach data.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `ReadModels/BreachReadModel.cs`**:
   - Implements `IReadModel`
   - Properties mirror aggregate state: `Guid Id`, `string Nature`, `BreachSeverity Severity`, `BreachStatus Status`, `int EstimatedAffectedSubjects`, `string Description`, `DateTimeOffset DetectedAtUtc`, `DateTimeOffset DeadlineUtc`, `DateTimeOffset? AssessedAtUtc`, `DateTimeOffset? ReportedToDPAAtUtc`, `DateTimeOffset? NotifiedSubjectsAtUtc`, `DateTimeOffset? ContainedAtUtc`, `DateTimeOffset? ClosedAtUtc`, `string? AuthorityName`, `int SubjectCount`, `List<PhasedReportSummary> PhasedReports`, `string? TenantId`, `string? ModuleId`, `DateTimeOffset LastModifiedAtUtc`, `int Version`
   - Nested record: `PhasedReportSummary(int PhaseNumber, string ReportContent, DateTimeOffset SubmittedAtUtc)`

2. **Create `ReadModels/BreachProjection.cs`**:
   - Implements `IProjection<BreachReadModel>`
   - Implements `IProjectionCreator<BreachDetected, BreachReadModel>` — initializes read model
   - Implements `IProjectionHandler<T, BreachReadModel>` for each subsequent event:
     - `BreachAssessed` → updates severity, affected subjects, assessment timestamp
     - `BreachReportedToDPA` → updates authority info, report timestamp, status
     - `BreachNotifiedToSubjects` → updates subject count, notification timestamp, status
     - `BreachPhasedReportAdded` → adds to PhasedReports list, increments count
     - `BreachContained` → updates containment timestamp
     - `BreachClosed` → updates resolution, close timestamp, status
   - Each handler increments `Version++` and updates `LastModifiedAtUtc`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of Issue #780 — Migrate BreachNotification to Marten event sourcing.

CONTEXT:
- Phase 1 is complete: BreachAggregate and 7 domain events exist
- Read models implement IReadModel interface (from Encina.Marten)
- Projections implement IProjection<T>, IProjectionCreator<TEvent, T>, IProjectionHandler<TEvent, T>
- Read models are mutable (projections update them in place)
- Always increment Version and update LastModifiedAtUtc on each event

TASK:
1. Create ReadModels/BreachReadModel.cs
2. Create ReadModels/BreachProjection.cs

KEY RULES:
- BreachReadModel has mutable properties (get; set;) for projection updates
- PhasedReports is a List<PhasedReportSummary> initialized as empty
- PhasedReportSummary is a nested sealed record
- Projection.Create() initializes the read model from BreachDetected event
- Each Apply() handler returns the updated read model
- ProjectionName property returns "BreachProjection"
- XML documentation on all public types

REFERENCE FILES:
- src/Encina.Compliance.Consent/ReadModels/ConsentReadModel.cs
- src/Encina.Compliance.Consent/ReadModels/ConsentProjection.cs
- src/Encina.Compliance.DataSubjectRights/ReadModels/DSRRequestReadModel.cs
- src/Encina.Compliance.DataSubjectRights/ReadModels/DSRRequestProjection.cs
```

</details>

---

### Phase 3: Service Interface & Implementation

> **Goal**: Create `IBreachNotificationService` (replacing `IBreachHandler` + `IBreachRecordStore` + `IBreachAuditStore`) with `DefaultBreachNotificationService` using aggregate and read model.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `Abstractions/IBreachNotificationService.cs`**:
   - **Commands** (write via aggregate):
     - `RecordBreachAsync(string nature, BreachSeverity severity, string detectedByRule, int estimatedAffectedSubjects, string description, string? detectedByUserId, string? tenantId, string? moduleId, CancellationToken ct)` → `ValueTask<Either<EncinaError, Guid>>`
     - `AssessBreachAsync(Guid breachId, BreachSeverity updatedSeverity, int updatedAffectedSubjects, string assessmentSummary, string assessedByUserId, CancellationToken ct)` → `ValueTask<Either<EncinaError, Unit>>`
     - `ReportToDPAAsync(Guid breachId, string authorityName, string authorityContactInfo, string reportSummary, string reportedByUserId, CancellationToken ct)` → `ValueTask<Either<EncinaError, Unit>>`
     - `NotifySubjectsAsync(Guid breachId, int subjectCount, string communicationMethod, SubjectNotificationExemption exemption, string notifiedByUserId, CancellationToken ct)` → `ValueTask<Either<EncinaError, Unit>>`
     - `AddPhasedReportAsync(Guid breachId, string reportContent, string submittedByUserId, CancellationToken ct)` → `ValueTask<Either<EncinaError, Unit>>`
     - `ContainBreachAsync(Guid breachId, string containmentMeasures, string containedByUserId, CancellationToken ct)` → `ValueTask<Either<EncinaError, Unit>>`
     - `CloseBreachAsync(Guid breachId, string resolutionSummary, string closedByUserId, CancellationToken ct)` → `ValueTask<Either<EncinaError, Unit>>`
   - **Queries** (read via read model):
     - `GetBreachAsync(Guid breachId, CancellationToken ct)` → `ValueTask<Either<EncinaError, BreachReadModel>>`
     - `GetBreachesByStatusAsync(BreachStatus status, CancellationToken ct)` → `ValueTask<Either<EncinaError, IReadOnlyList<BreachReadModel>>>`
     - `GetOverdueBreachesAsync(CancellationToken ct)` → `ValueTask<Either<EncinaError, IReadOnlyList<BreachReadModel>>>`
     - `GetDeadlineStatusAsync(Guid breachId, CancellationToken ct)` → `ValueTask<Either<EncinaError, DeadlineStatus>>`
     - `GetBreachHistoryAsync(Guid breachId, CancellationToken ct)` → `ValueTask<Either<EncinaError, IReadOnlyList<object>>>` (event stream = audit trail)

2. **Create `Services/DefaultBreachNotificationService.cs`**:
   - Dependencies: `IAggregateRepository<BreachAggregate>`, `IReadModelRepository<BreachReadModel>`, `ICacheProvider`, `TimeProvider`, `IOptions<BreachNotificationOptions>`, `ILogger<DefaultBreachNotificationService>`
   - Command methods: load/create aggregate → call behavior method → save → invalidate cache → record metric
   - Query methods: check cache → read from read model repo → populate cache → return
   - Cache keys: `"breach:{id}"`, `"breach:status:{status}"`, `"breach:overdue"`
   - Error handling: catch `InvalidOperationException` for state transition errors → return domain error; catch general exceptions → return service error
   - Diagnostics: increment counters on each operation, log via `BreachNotificationLogMessages`

3. **Update `BreachNotificationErrors.cs`** — Add new error codes if needed:
   - `breach.aggregate_not_found` — when aggregate doesn't exist
   - `breach.invalid_state_transition` — when aggregate rejects a state change
   - `breach.service_error` — general service error wrapper
   - Keep existing error codes that are still applicable

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of Issue #780 — Migrate BreachNotification to Marten event sourcing.

CONTEXT:
- Phases 1-2 complete: BreachAggregate, events, BreachReadModel, BreachProjection exist
- The service replaces IBreachHandler + IBreachRecordStore + IBreachAuditStore with a single interface
- Commands go through IAggregateRepository<BreachAggregate> (write side)
- Queries go through IReadModelRepository<BreachReadModel> (read side)
- ICacheProvider provides cache-aside pattern for read operations
- All methods return ValueTask<Either<EncinaError, T>> (ROP)

TASK:
1. Create Abstractions/IBreachNotificationService.cs with 7 commands + 5 queries
2. Create Services/DefaultBreachNotificationService.cs implementing the interface
3. Update BreachNotificationErrors.cs with new error codes

KEY RULES:
- Commands: load aggregate → call method → save → invalidate cache → return
- Queries: check cache → load read model → populate cache → return
- Cache keys follow "breach:{id}" pattern with TTL from options or 5 minutes default
- Catch InvalidOperationException for state transitions → return BreachNotificationErrors.InvalidStateTransition
- Catch Exception (except OperationCanceledException) → return BreachNotificationErrors.ServiceError
- Log every operation via BreachNotificationLogMessages extension methods
- Increment counters via BreachNotificationDiagnostics on each operation
- RecordBreachAsync creates a new aggregate, all others load existing
- GetBreachHistoryAsync reads event stream from IAggregateRepository

REFERENCE FILES:
- src/Encina.Compliance.Consent/Services/DefaultConsentService.cs (service pattern)
- src/Encina.Compliance.Consent/Abstractions/IConsentService.cs (interface pattern)
- src/Encina.Compliance.DataSubjectRights/Services/DefaultDSRService.cs (service pattern)
- src/Encina.Compliance.BreachNotification/BreachNotificationErrors.cs (existing errors)
```

</details>

---

### Phase 4: Configuration, DI, Marten Extensions & Health Check

> **Goal**: Update DI registration to use ES-based services, add Marten extensions, update health check.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `BreachNotificationMartenExtensions.cs`**:
   - `public static IServiceCollection AddBreachNotificationAggregates(this IServiceCollection services)`
   - Registers: `AddAggregateRepository<BreachAggregate>()`, `AddProjection<BreachProjection, BreachReadModel>()`

2. **Update `ServiceCollectionExtensions.cs`**:
   - Remove registration of `IBreachRecordStore` → `InMemoryBreachRecordStore`
   - Remove registration of `IBreachAuditStore` → `InMemoryBreachAuditStore`
   - Remove registration of `IBreachHandler` → `DefaultBreachHandler`
   - Add registration of `IBreachNotificationService` → `DefaultBreachNotificationService` (TryAddScoped)
   - Keep all detection-related registrations (IBreachDetector, IBreachNotifier, rules, pipeline behavior)
   - Keep optional health check and deadline monitor registrations
   - Keep options and options validator

3. **Update `Health/BreachNotificationHealthCheck.cs`**:
   - Remove checks for `IBreachRecordStore` and `IBreachAuditStore`
   - Add check for `IBreachNotificationService` resolvable
   - Add check for `IAggregateRepository<BreachAggregate>` resolvable (verifies Marten/PostgreSQL connectivity)
   - Keep overdue breach check (now via `IBreachNotificationService.GetOverdueBreachesAsync()`)

4. **Update `BreachDeadlineMonitorService.cs`**:
   - Replace `IBreachRecordStore` dependency with `IBreachNotificationService`
   - Update `GetOverdueBreachesAsync` and `GetApproachingDeadlineAsync` calls to use new service
   - Update `DeadlineWarningNotification` publishing logic

5. **Update `BreachNotificationOptions.cs`**:
   - Remove store-related options if any exist
   - Keep all detection, notification, deadline, and enforcement options
   - Keep `AddHealthCheck`, `EnableDeadlineMonitoring`, etc.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of Issue #780 — Migrate BreachNotification to Marten event sourcing.

CONTEXT:
- Phases 1-3 complete: BreachAggregate, events, read model, projection, IBreachNotificationService, DefaultBreachNotificationService exist
- DI registration must switch from entity-based stores to ES-based services
- The detection engine (IBreachDetector, rules, pipeline behavior) remains unchanged
- Health check must verify Marten connectivity instead of InMemory store status
- BreachDeadlineMonitorService must use IBreachNotificationService instead of IBreachRecordStore

TASK:
1. Create BreachNotificationMartenExtensions.cs
2. Update ServiceCollectionExtensions.cs — remove old, add new
3. Update BreachNotificationHealthCheck.cs — verify Marten connectivity
4. Update BreachDeadlineMonitorService.cs — use new service
5. Review BreachNotificationOptions.cs for any needed changes

KEY RULES:
- Marten extensions: AddAggregateRepository<T>() + AddProjection<TProjection, TReadModel>()
- DI: use TryAddScoped for IBreachNotificationService (allows override)
- Health check: scoped resolution via IServiceProvider.CreateScope()
- Deadline monitor: replace IBreachRecordStore calls with IBreachNotificationService equivalents
- Keep all existing detection and notification registrations untouched
- Keep IBreachNotifier registration (it's the notification delivery abstraction)

REFERENCE FILES:
- src/Encina.Compliance.Consent/ConsentMartenExtensions.cs (Marten extensions pattern)
- src/Encina.Compliance.Consent/ServiceCollectionExtensions.cs (DI after ES migration)
- src/Encina.Compliance.Consent/Health/ConsentHealthCheck.cs (health check pattern)
- src/Encina.Compliance.BreachNotification/ServiceCollectionExtensions.cs (current DI)
- src/Encina.Compliance.BreachNotification/Health/BreachNotificationHealthCheck.cs (current health check)
- src/Encina.Compliance.BreachNotification/BreachDeadlineMonitorService.cs (current deadline monitor)
```

</details>

---

### Phase 5: Remove Provider Implementations & Old Infrastructure

> **Goal**: Delete all 27 provider-specific store implementations and old core infrastructure that is no longer needed.

<details>
<summary><strong>Tasks</strong></summary>

#### 5a. Delete Provider Files (27 files)

**ADO.NET (8 files)**:
- `src/Encina.ADO.Sqlite/BreachNotification/BreachRecordStoreADO.cs`
- `src/Encina.ADO.Sqlite/BreachNotification/BreachAuditStoreADO.cs`
- `src/Encina.ADO.SqlServer/BreachNotification/BreachRecordStoreADO.cs`
- `src/Encina.ADO.SqlServer/BreachNotification/BreachAuditStoreADO.cs`
- `src/Encina.ADO.PostgreSQL/BreachNotification/BreachRecordStoreADO.cs`
- `src/Encina.ADO.PostgreSQL/BreachNotification/BreachAuditStoreADO.cs`
- `src/Encina.ADO.MySQL/BreachNotification/BreachRecordStoreADO.cs`
- `src/Encina.ADO.MySQL/BreachNotification/BreachAuditStoreADO.cs`

**Dapper (8 files)**:
- `src/Encina.Dapper.Sqlite/BreachNotification/BreachRecordStoreDapper.cs`
- `src/Encina.Dapper.Sqlite/BreachNotification/BreachAuditStoreDapper.cs`
- `src/Encina.Dapper.SqlServer/BreachNotification/BreachRecordStoreDapper.cs`
- `src/Encina.Dapper.SqlServer/BreachNotification/BreachAuditStoreDapper.cs`
- `src/Encina.Dapper.PostgreSQL/BreachNotification/BreachRecordStoreDapper.cs`
- `src/Encina.Dapper.PostgreSQL/BreachNotification/BreachAuditStoreDapper.cs`
- `src/Encina.Dapper.MySQL/BreachNotification/BreachRecordStoreDapper.cs`
- `src/Encina.Dapper.MySQL/BreachNotification/BreachAuditStoreDapper.cs`

**EF Core (6 files)**:
- `src/Encina.EntityFrameworkCore/BreachNotification/BreachRecordStoreEF.cs`
- `src/Encina.EntityFrameworkCore/BreachNotification/BreachAuditStoreEF.cs`
- `src/Encina.EntityFrameworkCore/BreachNotification/BreachRecordEntityConfiguration.cs`
- `src/Encina.EntityFrameworkCore/BreachNotification/PhasedReportEntityConfiguration.cs`
- `src/Encina.EntityFrameworkCore/BreachNotification/BreachAuditEntryEntityConfiguration.cs`
- `src/Encina.EntityFrameworkCore/BreachNotification/BreachNotificationModelBuilderExtensions.cs`

**MongoDB (5 files)**:
- `src/Encina.MongoDB/BreachNotification/BreachRecordStoreMongoDB.cs`
- `src/Encina.MongoDB/BreachNotification/BreachAuditStoreMongoDB.cs`
- `src/Encina.MongoDB/BreachNotification/BreachRecordDocument.cs`
- `src/Encina.MongoDB/BreachNotification/PhasedReportDocument.cs`
- `src/Encina.MongoDB/BreachNotification/BreachAuditEntryDocument.cs`

#### 5b. Delete Old Core Infrastructure (9 files)

- `src/Encina.Compliance.BreachNotification/Abstractions/IBreachRecordStore.cs`
- `src/Encina.Compliance.BreachNotification/Abstractions/IBreachAuditStore.cs`
- `src/Encina.Compliance.BreachNotification/Abstractions/IBreachHandler.cs`
- `src/Encina.Compliance.BreachNotification/InMemory/InMemoryBreachRecordStore.cs`
- `src/Encina.Compliance.BreachNotification/InMemory/InMemoryBreachAuditStore.cs`
- `src/Encina.Compliance.BreachNotification/DefaultBreachHandler.cs`
- `src/Encina.Compliance.BreachNotification/BreachRecordEntity.cs`
- `src/Encina.Compliance.BreachNotification/PhasedReportEntity.cs`
- `src/Encina.Compliance.BreachNotification/BreachAuditEntryEntity.cs`
- `src/Encina.Compliance.BreachNotification/BreachRecordMapper.cs`
- `src/Encina.Compliance.BreachNotification/PhasedReportMapper.cs`
- `src/Encina.Compliance.BreachNotification/BreachAuditEntryMapper.cs`

#### 5c. Update Satellite Provider DI Registrations

- Remove BreachNotification store registrations from each satellite package's `ServiceCollectionExtensions.cs`
- Remove BreachNotification-related DI methods (e.g., `AddEncinaBreachNotification{Provider}()`)

#### 5d. Verify Build

- Run `dotnet build Encina.slnx --configuration Release` — expect compilation errors from removed types
- Fix any remaining references to deleted types across the solution
- Ensure 0 errors, 0 warnings

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of Issue #780 — Migrate BreachNotification to Marten event sourcing.

CONTEXT:
- Phases 1-4 complete: ES infrastructure is fully in place (aggregate, events, projection, service, DI, health check)
- Now we remove ALL old entity-based infrastructure: 27 provider files + 12 core files
- This is the "demolition" phase — carefully delete files and fix compilation errors

TASK:
1. Delete all 27 provider store files (ADO ×8, Dapper ×8, EF Core ×6, MongoDB ×5)
2. Delete the BreachNotification/ folders in each provider package if now empty
3. Delete 12 old core files (IBreachRecordStore, IBreachAuditStore, IBreachHandler, InMemory stores, entities, mappers, DefaultBreachHandler)
4. Remove BreachNotification DI registrations from satellite provider ServiceCollectionExtensions
5. Fix any remaining compilation errors from removed types
6. Build verification: dotnet build Encina.slnx --configuration Release → 0 errors

KEY RULES:
- DELETE the files, do NOT mark them as [Obsolete]
- Check each satellite provider's ServiceCollectionExtensions.cs for BreachNotification registrations to remove
- After deletion, grep for references to removed types: IBreachRecordStore, IBreachAuditStore, IBreachHandler, InMemoryBreachRecordStore, InMemoryBreachAuditStore, DefaultBreachHandler, BreachRecordEntity, etc.
- Fix any broken references — most should point to IBreachNotificationService now
- Keep: IBreachDetector, IBreachNotifier, IBreachDetectionRule, DefaultBreachDetector, DefaultBreachNotifier, detection rules, pipeline behavior, SecurityEventFactory
- Keep: Model types (BreachRecord, SecurityEvent, PotentialBreach, etc.) — some may still be used by detection engine
- Keep: Notifications (BreachDetectedNotification, etc.) — used by pipeline/handler
- Check if BreachRecord.cs is still needed by detection engine or can be replaced

REFERENCE FILES:
- git log for Consent/DSR/LawfulBasis migrations to see what was deleted in those
```

</details>

---

### Phase 6: Update Diagnostics

> **Goal**: Update observability instrumentation to reflect the ES-based architecture.

<details>
<summary><strong>Tasks</strong></summary>

1. **Update `Diagnostics/BreachNotificationDiagnostics.cs`**:
   - Keep existing `ActivitySource` and `Meter` names (same package, just different implementation)
   - Review counters — keep all that are still applicable:
     - `breach.detected.total` ✅ (still valid — detection engine unchanged)
     - `breach.notification.authority.total` ✅ (now from service)
     - `breach.notification.subjects.total` ✅ (now from service)
     - `breach.pipeline.executions.total` ✅ (pipeline behavior unchanged)
     - `breach.phased_reports.total` ✅ (now from service)
     - `breach.resolved.total` → rename to `breach.closed.total` (matches new lifecycle)
   - Add new counters:
     - `breach.assessed.total` — assessment operations
     - `breach.contained.total` — containment operations
     - `breach.cache.hits.total` — cache hit tracking
   - Keep histograms: `breach.time_to_notification.hours`, `breach.detection.duration.ms`, `breach.pipeline.duration.ms`
   - Keep all activity helpers and tag constants

2. **Update `Diagnostics/BreachNotificationLogMessages.cs`**:
   - Remove log messages for store operations (InMemory store, entity mapping, etc.)
   - Add log messages for ES operations:
     - `BreachAggregateLoaded` (EventId 8780)
     - `BreachAggregateSaved` (EventId 8781)
     - `BreachCacheHit` (EventId 8782)
     - `BreachCacheInvalidated` (EventId 8783)
     - `BreachInvalidStateTransition` (EventId 8784)
     - `BreachServiceError` (EventId 8785)
   - Keep all existing detection, pipeline, notification, and deadline log messages
   - Event ID range: continue within 8700-8799 (existing allocation)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of Issue #780 — Migrate BreachNotification to Marten event sourcing.

CONTEXT:
- Phases 1-5 complete: ES infrastructure is in place, old code is deleted
- Diagnostics module uses EventId range 8700-8799 (allocated, no collision)
- ActivitySource name: "Encina.Compliance.BreachNotification" (unchanged)
- Meter name: "Encina.Compliance.BreachNotification" (unchanged)

TASK:
1. Update BreachNotificationDiagnostics.cs — add new counters, remove obsolete ones
2. Update BreachNotificationLogMessages.cs — add ES-specific log messages, remove store-specific ones

KEY RULES:
- Keep existing EventId range 8700-8799
- New log messages use EventIds 8780-8789 (end of range)
- Remove log messages that reference removed types (InMemoryBreachRecordStore, etc.)
- Add cache hit/miss logging following Consent pattern
- LoggerMessage uses source generator ([LoggerMessage] attribute) or LoggerMessage.Define pattern — match existing style
- Tag constants are internal static readonly strings

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/Diagnostics/BreachNotificationDiagnostics.cs (current)
- src/Encina.Compliance.BreachNotification/Diagnostics/BreachNotificationLogMessages.cs (current)
- src/Encina.Compliance.Consent/Diagnostics/ConsentDiagnostics.cs (ES pattern)
```

</details>

---

### Phase 7: Update Tests

> **Goal**: Update all test files to work with the ES-based architecture, add new aggregate and service tests.

<details>
<summary><strong>Tasks</strong></summary>

#### 7a. Delete Obsolete Tests

- `tests/Encina.UnitTests/Compliance/BreachNotification/InMemoryBreachRecordStoreTests.cs` — store no longer exists
- `tests/Encina.UnitTests/Compliance/BreachNotification/InMemoryBreachAuditStoreTests.cs` — store no longer exists
- `tests/Encina.UnitTests/Compliance/BreachNotification/DefaultBreachHandlerTests.cs` — handler replaced by service
- `tests/Encina.GuardTests/Compliance/BreachNotification/InMemoryBreachRecordStoreGuardTests.cs` — store removed
- `tests/Encina.GuardTests/Compliance/BreachNotification/InMemoryBreachAuditStoreGuardTests.cs` — store removed
- `tests/Encina.GuardTests/Compliance/BreachNotification/DefaultBreachHandlerGuardTests.cs` — handler removed
- `tests/Encina.ContractTests/Compliance/BreachNotification/InMemoryBreachRecordStoreContractTests.cs` — store removed
- `tests/Encina.ContractTests/Compliance/BreachNotification/InMemoryBreachAuditStoreContractTests.cs` — store removed
- `tests/Encina.ContractTests/Compliance/BreachNotification/IBreachRecordStoreContractTests.cs` — interface removed
- `tests/Encina.ContractTests/Compliance/BreachNotification/IBreachAuditStoreContractTests.cs` — interface removed

#### 7b. Create New Unit Tests

- `tests/Encina.UnitTests/Compliance/BreachNotification/BreachAggregateTests.cs`:
  - Test factory method `Detect()` — sets initial state, raises `BreachDetected`, calculates 72h deadline
  - Test `Assess()` — valid from Detected, raises `BreachAssessed`, transitions to Investigating
  - Test `ReportToDPA()` — valid from Detected/Investigating, raises `BreachReportedToDPA`, transitions to AuthorityNotified
  - Test `NotifySubjects()` — valid from AuthorityNotified, raises `BreachNotifiedToSubjects`, transitions to SubjectsNotified
  - Test `AddPhasedReport()` — valid when not Closed, raises `BreachPhasedReportAdded`
  - Test `Contain()` — valid when not Closed, raises `BreachContained`
  - Test `Close()` — valid from SubjectsNotified/Resolved, raises `BreachClosed`, transitions to Closed
  - Test invalid state transitions (throw `InvalidOperationException`)
  - Test guard clauses on each method (null/whitespace parameters)
  - Test `UncommittedEvents` contains correct event types

- `tests/Encina.UnitTests/Compliance/BreachNotification/BreachProjectionTests.cs`:
  - Test `Create()` with `BreachDetected` — initializes all read model fields
  - Test `Apply()` for each event type — verifies correct field updates
  - Test `Version` increments and `LastModifiedAtUtc` updates on each event

- `tests/Encina.UnitTests/Compliance/BreachNotification/DefaultBreachNotificationServiceTests.cs`:
  - Mock `IAggregateRepository<BreachAggregate>`, `IReadModelRepository<BreachReadModel>`, `ICacheProvider`
  - Test each command method: creates/loads aggregate, saves, invalidates cache
  - Test each query method: checks cache, falls back to read model
  - Test error handling: InvalidOperationException → domain error, Exception → service error
  - Test cache hit path vs cache miss path

#### 7c. Create New Guard Tests

- `tests/Encina.GuardTests/Compliance/BreachNotification/BreachAggregateGuardTests.cs`:
  - Guard tests for `Detect()` factory method parameters
  - Guard tests for all behavior method parameters

- `tests/Encina.GuardTests/Compliance/BreachNotification/DefaultBreachNotificationServiceGuardTests.cs`:
  - Guard tests for constructor parameters
  - Guard tests for all service method parameters

#### 7d. Update Existing Tests

- `tests/Encina.UnitTests/Compliance/BreachNotification/BreachNotificationErrorsTests.cs` — add tests for new error codes
- `tests/Encina.UnitTests/Compliance/BreachNotification/BreachNotificationOptionsTests.cs` — update if options changed
- `tests/Encina.UnitTests/Compliance/BreachNotification/BreachDetectionPipelineBehaviorTests.cs` — should still pass (detection engine unchanged)
- `tests/Encina.UnitTests/Compliance/BreachNotification/DetectionRulesTests.cs` — should still pass
- `tests/Encina.IntegrationTests/Compliance/BreachNotification/BreachNotificationPipelineIntegrationTests.cs` — update to use `IBreachNotificationService` instead of `IBreachRecordStore`/`IBreachHandler`
- `tests/Encina.PropertyTests/Compliance/BreachNotification/BreachRecordPropertyTests.cs` — update if `BreachRecord` changed

#### 7e. Build & Run Tests

- `dotnet build Encina.slnx --configuration Release` → 0 errors
- `dotnet test tests/Encina.UnitTests/ --filter "FullyQualifiedName~BreachNotification"` → all pass
- `dotnet test tests/Encina.GuardTests/ --filter "FullyQualifiedName~BreachNotification"` → all pass

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of Issue #780 — Migrate BreachNotification to Marten event sourcing.

CONTEXT:
- Phases 1-6 complete: full ES migration done, old code deleted, diagnostics updated
- Tests must be updated to reflect the new architecture
- Some tests are obsolete (store/handler tests), some need updating, some are new
- Detection engine tests should be unaffected (pipeline behavior, rules)

TASK:
1. Delete 10 obsolete test files (store tests, handler tests, contract tests for removed interfaces)
2. Create BreachAggregateTests.cs — test factory method, behavior methods, state transitions, invalid transitions
3. Create BreachProjectionTests.cs — test Create and Apply for each event type
4. Create DefaultBreachNotificationServiceTests.cs — test commands, queries, error handling, caching
5. Create guard tests for aggregate and service
6. Update BreachNotificationPipelineIntegrationTests.cs to use new service
7. Update property tests if model types changed
8. Verify all tests compile and pass

KEY RULES:
- Aggregate tests: verify both state changes AND event generation (UncommittedEvents)
- Service tests: mock IAggregateRepository<BreachAggregate>, IReadModelRepository<BreachReadModel>, ICacheProvider
- Use NSubstitute for mocking (project standard)
- Use FluentAssertions for assertions (project standard)
- Test invalid state transitions: e.g., Close() from Detected should throw InvalidOperationException
- Test guard clauses in separate guard test files (Shouldly assertions)
- Follow AAA pattern, descriptive test names, single assertion per test

REFERENCE FILES:
- tests/Encina.UnitTests/Compliance/Consent/ConsentAggregateTests.cs (aggregate test pattern)
- tests/Encina.GuardTests/Compliance/Consent/ConsentAggregateGuardTests.cs (guard test pattern)
- tests/Encina.UnitTests/Compliance/BreachNotification/ (existing tests to review)
- tests/Encina.GuardTests/Compliance/BreachNotification/ (existing guard tests)
```

</details>

---

### Phase 8: Documentation & Finalization

> **Goal**: Update all project documentation, PublicAPI tracking, verify build, and finalize.

<details>
<summary><strong>Tasks</strong></summary>

1. **Update `PublicAPI.Unshipped.txt`**:
   - Remove entries for deleted public types: `IBreachRecordStore`, `IBreachAuditStore`, `IBreachHandler`, `InMemoryBreachRecordStore`, `InMemoryBreachAuditStore`, `DefaultBreachHandler`, entity classes, mapper classes
   - Add entries for new public types: `BreachAggregate`, all 7 events, `BreachReadModel`, `BreachProjection`, `IBreachNotificationService`, `DefaultBreachNotificationService`, `BreachNotificationMartenExtensions`
   - Keep entries for preserved types (detection, notification, options, etc.)

2. **Update `CHANGELOG.md`** — Add entry under Unreleased:
   ```
   ### Changed
   - Encina.Compliance.BreachNotification — Migrated from entity-based persistence (13 database providers) to Marten event sourcing. `IBreachRecordStore` + `IBreachAuditStore` + `IBreachHandler` replaced by `IBreachNotificationService` with event-sourced `BreachAggregate`. Domain events (`BreachDetected`, `BreachAssessed`, `BreachReportedToDPA`, etc.) provide immutable audit trail for GDPR Arts. 33-34 accountability. Detection engine (`IBreachDetector`, pipeline behavior, rules) preserved unchanged. (Fixes #780)

   ### Removed
   - All 13 database provider implementations for BreachNotification (ADO.NET ×4, Dapper ×4, EF Core ×4, MongoDB) — replaced by Marten event sourcing
   - `IBreachRecordStore`, `IBreachAuditStore`, `IBreachHandler` interfaces — replaced by `IBreachNotificationService`
   - `InMemoryBreachRecordStore`, `InMemoryBreachAuditStore` — no longer needed with ES
   - Entity classes and mappers (`BreachRecordEntity`, `BreachAuditEntryEntity`, `PhasedReportEntity`, mappers) — replaced by aggregate + events
   ```

3. **Update `ROADMAP.md`** — Mark BreachNotification ES migration as completed

4. **Update `docs/INVENTORY.md`** — Update BreachNotification entry to reflect ES architecture

5. **XML documentation review** — Verify all new public APIs have `<summary>`, `<remarks>`, `<param>`, `<returns>` with GDPR article references

6. **Build verification**:
   - `dotnet build Encina.slnx --configuration Release` → 0 errors, 0 warnings
   - `dotnet test` → all tests pass

7. **Commit**: Reference `Fixes #780` in commit message

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 of Issue #780 — Migrate BreachNotification to Marten event sourcing.

CONTEXT:
- Phases 1-7 complete: full migration done, tests updated and passing
- Documentation and finalization remaining

TASK:
1. Update PublicAPI.Unshipped.txt — remove deleted types, add new types
2. Update CHANGELOG.md — add under ### Changed and ### Removed in Unreleased section
3. Update ROADMAP.md — mark BreachNotification ES migration as completed
4. Update docs/INVENTORY.md — reflect new ES architecture
5. Verify XML documentation on all new public APIs
6. Build verification: dotnet build --configuration Release → 0 errors, 0 warnings
7. Test verification: dotnet test → all pass

KEY RULES:
- CHANGELOG follows Keep a Changelog format
- PublicAPI.Unshipped.txt uses nullable annotations (string!, string?)
- Build must produce 0 errors AND 0 warnings
- Commit message: "refactor: migrate Encina.Compliance.BreachNotification to Marten event sourcing (Fixes #780)"
- No AI attribution in commit messages
```

</details>

---

## Research

### GDPR Article References

| Article | Topic | Relevance to BreachNotification |
|---------|-------|--------------------------------|
| Art. 33 | Notification to supervisory authority | 72-hour deadline, phased reporting (33(4)), documentation requirement (33(5)) |
| Art. 34 | Communication to data subjects | High-risk breaches, exemptions (34(3)), communication methods |
| Art. 5(2) | Accountability principle | Event-sourced audit trail proves compliance timeline |
| Art. 58 | Powers of supervisory authorities | Breach records must be available for inspection |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in Migration |
|-----------|----------|-------------------|
| `AggregateBase` | `Encina.Marten` | Base class for `BreachAggregate` |
| `IAggregateRepository<T>` | `Encina.Marten` | Load/save aggregate |
| `IReadModelRepository<T>` | `Encina.Marten` | Query read model |
| `IProjection<T>`, `IProjectionCreator`, `IProjectionHandler` | `Encina.Marten` | Projection interfaces |
| `IReadModel` | `Encina.Marten` | Read model marker interface |
| `ICacheProvider` | `Encina.Caching` | Cache-aside for read model queries |
| `INotification` | `Encina` core | Domain events implement this for auto-publishing |
| `IBreachDetector` | Current package | Preserved — detection engine unchanged |
| `IBreachNotifier` | Current package | Preserved — notification delivery unchanged |
| `BreachDetectionPipelineBehavior` | Current package | Preserved — pipeline behavior unchanged |
| `BreachDeadlineMonitorService` | Current package | Updated to use new service |
| `BreachNotificationDiagnostics` | Current package | Updated — same ActivitySource/Meter |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.GDPR` | 8100-8199 | Core GDPR |
| `Encina.Compliance.Consent` | 8200-8299 | Consent lifecycle |
| `Encina.Compliance.DataSubjectRights` | 8300-8349 | DSR operations |
| `Encina.Compliance.LawfulBasis` | 8350-8399 | LawfulBasis validation |
| **`Encina.Compliance.BreachNotification`** | **8700-8799** | **Existing allocation — no change** |

### File Count Estimate

| Category | Created | Modified | Deleted | Notes |
|----------|---------|----------|---------|-------|
| Phase 1: Events & Aggregate | 2 | 0 | 0 | Events, Aggregate |
| Phase 2: Read Model & Projection | 2 | 0 | 0 | ReadModel, Projection |
| Phase 3: Service | 2 | 1 | 0 | Interface, Implementation, Errors update |
| Phase 4: DI & Config | 1 | 4 | 0 | Marten extensions, DI, Health, Deadline, Options |
| Phase 5: Remove Providers | 0 | ~5 | 39 | 27 provider + 12 core files deleted |
| Phase 6: Diagnostics | 0 | 2 | 0 | Diagnostics, LogMessages |
| Phase 7: Tests | 5 | ~5 | 10 | New tests, updated tests, deleted tests |
| Phase 8: Documentation | 0 | ~5 | 0 | PublicAPI, CHANGELOG, etc. |
| **Total** | **~12** | **~22** | **~49** | |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing Issue #780 — Migrate Encina.Compliance.BreachNotification to Marten event sourcing.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library for CQRS + Event Sourcing
- Pre-1.0: no backward compatibility needed, best solution always
- Railway Oriented Programming: Either<EncinaError, T> everywhere
- Three compliance modules already migrated to ES: Consent (#777), DataSubjectRights (#778), LawfulBasis (#779)
- Follow the EXACT patterns from those migrations

MIGRATION OVERVIEW:
This is a REFACTOR, not a new feature. The existing BreachNotification module:
- Has entity-based persistence with 13 database provider implementations (27 files)
- Uses IBreachRecordStore + IBreachAuditStore for persistence
- Uses IBreachHandler as orchestrator
- Has a detection engine (IBreachDetector, rules, pipeline behavior) that is persistence-agnostic

After migration:
- BreachAggregate replaces BreachRecord as the write model
- 7 domain events replace direct entity mutations
- BreachReadModel + BreachProjection replace store queries
- IBreachNotificationService replaces IBreachHandler + IBreachRecordStore + IBreachAuditStore
- All 27 provider files + 12 core infrastructure files are DELETED
- Detection engine is PRESERVED unchanged

IMPLEMENTATION PHASES:
Phase 1: Events & Aggregate — BreachNotificationEvents.cs (7 events), BreachAggregate.cs
Phase 2: Read Model & Projection — BreachReadModel.cs, BreachProjection.cs
Phase 3: Service — IBreachNotificationService.cs, DefaultBreachNotificationService.cs, errors update
Phase 4: Configuration — BreachNotificationMartenExtensions.cs, update DI, health check, deadline monitor
Phase 5: Remove Old — Delete 27 provider files + 12 core files, fix compilation
Phase 6: Diagnostics — Update counters, log messages for ES operations
Phase 7: Tests — Delete 10 obsolete tests, create 5 new test files, update 5 existing
Phase 8: Documentation — PublicAPI, CHANGELOG, INVENTORY, build verification

KEY PATTERNS:
- Aggregate: inherits AggregateBase, static factory, behavior methods raise events, Apply updates state
- Events: sealed records implementing INotification, include TenantId/ModuleId
- ReadModel: mutable class implementing IReadModel, Version + LastModifiedAtUtc
- Projection: IProjection<T> + IProjectionCreator + IProjectionHandler per event
- Service: commands via aggregate, queries via read model, ICacheProvider for caching
- Marten extensions: AddAggregateRepository<T>(), AddProjection<TProjection, TReadModel>()
- DI: TryAddScoped for service, TryAdd pattern for all registrations
- Error handling: InvalidOperationException → domain error, Exception → service error
- Diagnostics: existing EventId range 8700-8799, same ActivitySource/Meter names
- Tests: NSubstitute for mocking, FluentAssertions/Shouldly for assertions

REFERENCE FILES:
- src/Encina.Compliance.Consent/ (complete ES migration reference)
- src/Encina.Compliance.DataSubjectRights/ (complete ES migration reference)
- src/Encina.Compliance.LawfulBasis/ (complete ES migration reference)
- src/Encina.Compliance.BreachNotification/ (current code to migrate)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | **Caching** | ✅ Include | `ICacheProvider` in `DefaultBreachNotificationService` for read model queries. Cache-aside with invalidation on write. |
| 2 | **OpenTelemetry** | ✅ Include | Existing `ActivitySource` + `Meter` updated for ES operations (Phase 6). |
| 3 | **Structured Logging** | ✅ Include | Existing `[LoggerMessage]` updated — remove store messages, add ES messages (Phase 6). |
| 4 | **Health Checks** | ✅ Include | Updated to verify Marten/PostgreSQL connectivity instead of InMemory store (Phase 4). |
| 5 | **Validation** | ❌ N/A | Input validation handled by aggregate guard clauses and service parameter validation — no separate validation provider needed. |
| 6 | **Resilience** | ❌ N/A | No external service calls in breach recording. `IBreachNotifier` (external notification) is a separate concern with its own resilience. |
| 7 | **Distributed Locks** | ❌ N/A | Marten optimistic concurrency via aggregate `Version` prevents duplicate operations — no distributed lock needed. |
| 8 | **Transactions** | ✅ Include | Handled by Marten `IDocumentSession.SaveChangesAsync()` — no separate `IUnitOfWork` needed. Aggregate save is atomic. |
| 9 | **Idempotency** | ✅ Include | Marten optimistic concurrency via aggregate `Version` prevents duplicate operations. |
| 10 | **Multi-Tenancy** | ✅ Include | `TenantId` property on aggregate and all events. |
| 11 | **Module Isolation** | ✅ Include | `ModuleId` property on aggregate and all events. |
| 12 | **Audit Trail** | ✅ Include | ES events ARE the audit trail — `GetBreachHistoryAsync()` reads event stream. Separate audit store eliminated. |

---

## Next Steps

1. **Review and approve this plan**
2. Begin Phase 1 implementation
3. Each phase should be verified with a build before proceeding
4. Final commit references `Fixes #780`
