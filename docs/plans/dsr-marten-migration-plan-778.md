# Implementation Plan: Migrate `Encina.Compliance.DataSubjectRights` to Marten Event Sourcing

> **Issue**: [#778](https://github.com/dlrivada/Encina/issues/778)
> **Type**: Refactor
> **Complexity**: High (8 phases, ~30 files to create/modify, ~45+ files to remove)
> **Estimated Scope**: ~2,000-2,800 lines of new production code + ~1,500-2,200 lines of tests; ~6,000+ lines removed from 13-provider stores
> **Prerequisite**: [#776 ADR-019](https://github.com/dlrivada/Encina/issues/776) ✅ Completed, [#412 CrossBorderTransfer](https://github.com/dlrivada/Encina/issues/412) ✅ Completed (reference implementation), [#777 Consent Migration](https://github.com/dlrivada/Encina/issues/777) ✅ Completed (same migration pattern)

---

## Summary

Migrate `Encina.Compliance.DataSubjectRights` from entity-based persistence (13 database providers: ADO.NET ×4, Dapper ×4, EF Core ×4, MongoDB ×1) to **Marten event sourcing** (PostgreSQL only), as mandated by [ADR-019](../architecture/adr/019-compliance-event-sourcing-marten.md).

The refactoring transforms the current mutable `DSRRequest` entity and `IDSRRequestStore`/`IDSRAuditStore` interfaces (with 26+ provider-specific implementations) into:

- **1 event-sourced aggregate** (`DSRRequestAggregate`) with immutable domain events
- **1 read model** (`DSRRequestReadModel`) with a Marten projection
- **1 service interface** (`IDSRService`) replacing the two store interfaces and the handler interface
- **Audit trail inherent in the event stream** — no separate `IDSRAuditStore` needed

The `ProcessingRestrictionPipelineBehavior`, `DataSubjectRightsOptions`, `DataSubjectRightsDiagnostics`, `DataSubjectRightsHealthCheck`, `[PersonalData]`/`[RestrictProcessing]` attributes, auto-registration infrastructure, and all handler/executor/exporter abstractions (`IDataSubjectRightsHandler`, `IPersonalDataLocator`, `IDataErasureExecutor`, `IDataPortabilityExporter`, etc.) are **preserved** — they are not store-dependent but their wiring is adapted. The old in-memory implementations (`InMemoryDSRRequestStore`, `InMemoryDSRAuditStore`) are **deleted** — unit tests use NSubstitute mocks of `IAggregateRepository<DSRRequestAggregate>`, and integration tests use real Marten with PostgreSQL in Docker.

**Provider category**: Event Sourcing (Marten/PostgreSQL — specialized provider, not the 13-database category).

**New dependencies for `Encina.Compliance.DataSubjectRights`**:

- `Encina.DomainModeling` — `AggregateBase`, `IAggregate` (DDD building blocks for event-sourced aggregates)
- `Encina.Marten` — `IAggregateRepository<T>`, `IReadModel`, `IProjectionHandler` (event store infrastructure)
- `Encina.Caching` — `ICacheProvider` (optional caching for read model queries)

> **Note on event types**: Event store events (e.g., `DSRRequestSubmitted`) are **sealed records implementing `INotification`** — they do NOT extend `DomainEvent`/`IDomainEvent` from DomainModeling. Marten manages event metadata (timestamps, sequence, correlation). By implementing `INotification`, these events are **automatically published** by `EventPublishingPipelineBehavior` after successful command execution, eliminating the need for a separate notification layer. The existing `Notifications/` mediator events (`DataErasedNotification`, `DataRectifiedNotification`, etc.) are **preserved** — they are published by the handler to notify external subscribers of completed operations, which is a different concern from lifecycle tracking.

**Packages affected**:

- `Encina.Compliance.DataSubjectRights` — restructured (aggregates, events, projections, service)
- `Encina.ADO.Sqlite`, `Encina.ADO.SqlServer`, `Encina.ADO.PostgreSQL`, `Encina.ADO.MySQL` — DSR stores **removed**
- `Encina.Dapper.Sqlite`, `Encina.Dapper.SqlServer`, `Encina.Dapper.PostgreSQL`, `Encina.Dapper.MySQL` — DSR stores **removed**
- `Encina.EntityFrameworkCore` — DSR stores, entities, configurations **removed**
- `Encina.MongoDB` — DSR stores, documents **removed**

---

## Design Choices

<details>
<summary><strong>1. Aggregate Design — Single DSRRequestAggregate per request</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Single aggregate per DSR request (by request ID)** | Natural boundary — each DSR has its own lifecycle, clear state machine, independent streams | Many streams if high volume of DSR requests (unlikely in practice) |
| **B) Single aggregate per data subject (all requests)** | Fewer streams, subject-level queries natural | Contention when a subject has concurrent requests (e.g., access + portability), violates single-responsibility |
| **C) Aggregate per right type** | Clean separation by GDPR article | Artificial boundary — a single request lifecycle doesn't span right types |

### Chosen Option: **A — Single DSRRequestAggregate per request**

### Rationale

- Matches GDPR's granularity: each DSR request has a unique lifecycle with its own 30-day deadline (Art. 12(3))
- Clean state machine: Received → IdentityVerified → InProgress → Completed/Denied/Extended/Expired
- Independent streams avoid contention — a subject can file multiple requests simultaneously
- Follows CrossBorderTransfer pattern where each aggregate has a clear, bounded lifecycle
- Marten handles many small streams efficiently via PostgreSQL JSONB

</details>

<details>
<summary><strong>2. Interface Transformation — Replace 3 interfaces with 1 IDSRService</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Single `IDSRService` combining stores + handler** | Clean API, matches ES model, simpler DI, handler operations naturally track lifecycle | Larger interface |
| **B) Keep 2 store interfaces + handler, adapt to ES** | Minimal API surface change | Artificial separation — audit is inherent in ES, request tracking is aggregate state |
| **C) Separate command/query services** | Clean CQRS separation | Over-engineering for a compliance module |

### Chosen Option: **A — Single `IDSRService`**

### Rationale

- In event sourcing, the audit trail IS the event stream — a separate `IDSRAuditStore` adds no value
- Request tracking is aggregate state, not a separate store concern
- The handler operations (access, erasure, portability, etc.) naturally produce lifecycle events on the aggregate
- Follows CrossBorderTransfer pattern: `IApprovedTransferService`, `ITIAService` — each is a single service
- Pre-1.0: breaking changes are expected and encouraged
- `IDataSubjectRightsHandler` is absorbed into `IDSRService` — the handler IS the service

</details>

<details>
<summary><strong>3. Event Model — 7 lifecycle events, unified (ES + mediator)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Fine-grained lifecycle events, unified ES + mediator** | One type = one fact (DRY), auto-published via `EventPublishingPipelineBehavior` | ES events depend on `INotification` (marker interface — zero cost) |
| **B) Separate ES + mediator types** | ES events decoupled from mediator | Two types per fact, manual mapping, more maintenance |
| **C) Generic `DSRRequestStateChanged` event** | One event type | Loses semantic meaning of transitions |

### Chosen Option: **A — Fine-grained unified events implementing `INotification`**

### Events

| Event | Trigger | GDPR Article | Maps to DSRRequestStatus |
|-------|---------|:------------:|--------------------------|
| `DSRRequestSubmitted` | Data subject submits a request | Art. 15-22 | `Received` |
| `DSRRequestVerified` | Identity verification completed | Art. 12(6) | `IdentityVerified` |
| `DSRRequestProcessing` | Processing begins | Art. 12(3) | `InProgress` |
| `DSRRequestCompleted` | Request fulfilled successfully | Art. 12(3) | `Completed` |
| `DSRRequestDenied` | Request rejected with reason | Art. 12(4) | `Rejected` |
| `DSRRequestExtended` | Deadline extended (up to 2 months) | Art. 12(3) | `Extended` |
| `DSRRequestExpired` | Deadline passed without completion | Art. 12(3) | `Expired` |

### Rationale

- **DDD principle**: In event sourcing, aggregate events ARE domain events. The separation into "event store events" and "mediator events" is an implementation artifact.
- **Infrastructure already supports it**: `EventPublishingPipelineBehavior` filters with `.OfType<INotification>()` — events implementing `INotification` are automatically published.
- **`INotification` is a marker interface** with zero methods — implementing it adds no coupling.
- **Existing `Notifications/` are preserved**: `DataErasedNotification`, `DataRectifiedNotification`, etc. remain — they notify external subscribers of completed handler operations, not lifecycle transitions.

</details>

<details>
<summary><strong>4. Handler Preservation — Keep IDataSubjectRightsHandler operations in IDSRService</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Absorb handler into IDSRService** | Single service for lifecycle + operations, cleaner DI | Larger interface |
| **B) Keep IDataSubjectRightsHandler separate** | Separation of concerns | Artificial — handler needs to update lifecycle state, which is now on the aggregate |
| **C) Move operations to pipeline behaviors** | Automated processing | Over-engineered — DSR operations need explicit orchestration |

### Chosen Option: **A — Absorb handler into IDSRService**

### Rationale

- The handler operations (HandleAccessAsync, HandleErasureAsync, etc.) naturally produce lifecycle transitions:
  - Submit → Verify → Start processing → Execute operation → Complete/Deny
- With ES, the handler must update the aggregate's lifecycle state after each operation
- Having a single `IDSRService` that manages both lifecycle and operations avoids circular dependencies
- The `IPersonalDataLocator`, `IDataErasureExecutor`, `IDataPortabilityExporter` abstractions remain as internal dependencies — they do the actual work, the service orchestrates
- Follows Consent pattern: `IConsentService` absorbed `IConsentStore` + `IConsentAuditStore` + `IConsentVersionManager`

</details>

<details>
<summary><strong>5. Testing Strategy — Mocks for unit tests, real Marten for integration (no InMemory)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) In-memory service** | No infrastructure for unit tests | Production code nobody deploys; mocks are more flexible |
| **B) Mock `IAggregateRepository<T>` + real Marten in Docker** | No unnecessary production code; precise per-test control | Requires Docker for integration tests (already available) |
| **C) In-memory Marten session mock** | Realistic | Complex, fragile, anti-pattern |

### Chosen Option: **B — Mock-based unit tests + real Marten integration tests**

### Rationale

- **CrossBorderTransfer has NO InMemory service** — unit tests mock `IAggregateRepository<T>` with NSubstitute
- **Consent migration (#777) followed the same pattern** — no `InMemoryConsentService`
- **Aggregates can be tested directly** (plain objects — no persistence needed)
- **Eliminates maintenance burden**: no `InMemoryDSRService` × 9 compliance modules

</details>

<details>
<summary><strong>6. Store Removal Strategy — Delete all 13-provider implementations</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Delete immediately, clean break** | No dead code, clear migration signal | Breaking change |
| **B) Keep as deprecated** | Backward compatibility | Pre-1.0: no backward compatibility; dead code prohibited |
| **C) Move to `.backup/`** | Recoverable | Unnecessary — git history preserves everything |

### Chosen Option: **A — Delete immediately**

### Rationale

- Pre-1.0 policy: "No `[Obsolete]`, no backward compatibility, no migration helpers"
- ADR-019: "Store interfaces with 13-provider implementations are replaced, not deprecated"
- Git history preserves all deleted code
- Removes ~6,000+ lines of provider-specific SQL and mapping code

</details>

---

## Implementation Phases

### Phase 1: Domain Events & Aggregate ✅ Completed

> **Goal**: Create the event-sourced aggregate with domain events that capture the full DSR request lifecycle.

<details>
<summary><strong>Tasks</strong></summary>

#### New files in `src/Encina.Compliance.DataSubjectRights/`

1. **`Events/DSRRequestEvents.cs`** — All 7 immutable event records implementing `: INotification`
   - `DSRRequestSubmitted(Guid RequestId, string SubjectId, DataSubjectRight RightType, DateTimeOffset ReceivedAtUtc, DateTimeOffset DeadlineAtUtc, string? RequestDetails, string? TenantId, string? ModuleId) : INotification`
   - `DSRRequestVerified(Guid RequestId, string VerifiedBy, DateTimeOffset VerifiedAtUtc, string? TenantId, string? ModuleId) : INotification`
   - `DSRRequestProcessing(Guid RequestId, string? ProcessedByUserId, DateTimeOffset StartedAtUtc, string? TenantId, string? ModuleId) : INotification`
   - `DSRRequestCompleted(Guid RequestId, DateTimeOffset CompletedAtUtc, string? TenantId, string? ModuleId) : INotification`
   - `DSRRequestDenied(Guid RequestId, string RejectionReason, DateTimeOffset DeniedAtUtc, string? TenantId, string? ModuleId) : INotification`
   - `DSRRequestExtended(Guid RequestId, string ExtensionReason, DateTimeOffset ExtendedDeadlineAtUtc, DateTimeOffset ExtendedAtUtc, string? TenantId, string? ModuleId) : INotification`
   - `DSRRequestExpired(Guid RequestId, DateTimeOffset ExpiredAtUtc, string? TenantId, string? ModuleId) : INotification`
   - All events are **unified**: persisted by Marten AND auto-published by `EventPublishingPipelineBehavior`

2. **`Aggregates/DSRRequestAggregate.cs`** — Event-sourced aggregate
   - Extends `AggregateBase` (from `Encina.DomainModeling`)
   - Properties: `SubjectId`, `RightType (DataSubjectRight)`, `Status (DSRRequestStatus)`, `ReceivedAtUtc`, `DeadlineAtUtc`, `CompletedAtUtc?`, `VerifiedAtUtc?`, `ExtensionReason?`, `ExtendedDeadlineAtUtc?`, `RejectionReason?`, `RequestDetails?`, `ProcessedByUserId?`, `TenantId?`, `ModuleId?`
   - Factory: `static DSRRequestAggregate Submit(Guid id, string subjectId, DataSubjectRight rightType, DateTimeOffset receivedAtUtc, string? requestDetails, string? tenantId, string? moduleId)` — calculates 30-day deadline
   - Commands:
     - `Verify(string verifiedBy, DateTimeOffset verifiedAtUtc)`
     - `StartProcessing(string? processedByUserId, DateTimeOffset startedAtUtc)`
     - `Complete(DateTimeOffset completedAtUtc)`
     - `Deny(string rejectionReason, DateTimeOffset deniedAtUtc)`
     - `Extend(string extensionReason, DateTimeOffset extendedAtUtc)` — calculates 2-month extension
     - `Expire(DateTimeOffset expiredAtUtc)`
   - `Apply(object domainEvent)` — switch expression for all 7 event types
   - Invariants: cannot verify already-verified, cannot complete non-in-progress, cannot deny completed, etc.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of migrating Encina.Compliance.DataSubjectRights to Marten event sourcing (Issue #778).

CONTEXT:
- Encina is a .NET 10 / C# 14 library using Railway Oriented Programming (Either<EncinaError, T>)
- This is a REFACTOR of an existing module: src/Encina.Compliance.DataSubjectRights/
- ADR-019 mandates migration from entity-based to event-sourced persistence
- Reference implementation: src/Encina.Compliance.CrossBorderTransfer/Aggregates/ and Events/
- Consent migration (#777) followed the same pattern — see src/Encina.Compliance.Consent/Aggregates/
- AggregateBase is from Encina.DomainModeling (Id: Guid, Version: int, RaiseEvent<T>, Apply(object))
- Events are sealed records with DateTimeOffset timestamps
- DSRRequestStatus enum already exists: Received, IdentityVerified, InProgress, Completed, Rejected, Extended, Expired
- DataSubjectRight enum already exists: Access, Rectification, Erasure, Restriction, Portability, Objection, AutomatedDecisionMaking, Notification

TASK:
1. Create Events/DSRRequestEvents.cs with 7 sealed record event types implementing `: INotification`
   (DSRRequestSubmitted, DSRRequestVerified, DSRRequestProcessing, DSRRequestCompleted, DSRRequestDenied, DSRRequestExtended, DSRRequestExpired)
2. Create Aggregates/DSRRequestAggregate.cs extending AggregateBase with full lifecycle support
3. Each event must include TenantId/ModuleId (for cross-cutting integration)
4. Aggregate MUST validate invariants (e.g., cannot complete non-in-progress request)
5. Submit factory method calculates 30-day deadline: DeadlineAtUtc = ReceivedAtUtc.AddDays(30) (Art. 12(3))
6. Extend command calculates 2-month extension: ExtendedDeadlineAtUtc = DeadlineAtUtc.AddMonths(2) (Art. 12(3))

KEY RULES:
- .NET 10 / C# 14, nullable enabled
- All types are sealed records (events) or sealed classes (aggregate)
- All public types need XML documentation with <summary>, <remarks>, GDPR article references
- Events implement INotification (marker interface from Encina core) — auto-published by EventPublishingPipelineBehavior
- Events are UNIFIED: same type persisted by Marten AND published to mediator subscribers
- Events are immutable facts — no methods, only data
- Aggregate state changes ONLY through RaiseEvent → Apply pattern
- DO NOT delete existing Notifications/ files (DataErasedNotification, etc.) — they serve a different purpose
- Follow exact patterns from src/Encina.Compliance.CrossBorderTransfer/Events/ApprovedTransferEvents.cs
  and src/Encina.Compliance.Consent/Events/ConsentEvents.cs

REFERENCE FILES:
- src/Encina.Compliance.CrossBorderTransfer/Aggregates/ApprovedTransferAggregate.cs
- src/Encina.Compliance.CrossBorderTransfer/Events/ApprovedTransferEvents.cs
- src/Encina.Compliance.Consent/Aggregates/ConsentAggregate.cs
- src/Encina.Compliance.Consent/Events/ConsentEvents.cs
- src/Encina.Compliance.DataSubjectRights/Model/DSRRequest.cs (existing entity — source for properties)
- src/Encina.Compliance.DataSubjectRights/Model/DSRRequestStatus.cs (existing enum — reuse)
- src/Encina.Compliance.DataSubjectRights/Model/DataSubjectRight.cs (existing enum — reuse)
- src/Encina.DomainModeling/AggregateBase.cs (base class)
```

</details>

---

### Phase 2: Read Model & Projection ✅ Completed

> **Goal**: Create the query-optimized read model and Marten projection for efficient DSR request queries.

<details>
<summary><strong>Tasks</strong></summary>

#### New files in `src/Encina.Compliance.DataSubjectRights/`

1. **`Projections/DSRRequestReadModel.cs`** — Query-optimized view
   - Implements `IReadModel` (from `Encina.Marten.Projections`)
   - Properties mirror `DSRRequest` but with mutable setters for projection updates:
     `Id (Guid)`, `SubjectId`, `RightType (DataSubjectRight)`, `Status (DSRRequestStatus)`, `ReceivedAtUtc`, `DeadlineAtUtc`, `CompletedAtUtc?`, `VerifiedAtUtc?`, `ExtensionReason?`, `ExtendedDeadlineAtUtc?`, `RejectionReason?`, `RequestDetails?`, `ProcessedByUserId?`, `TenantId?`, `ModuleId?`, `LastModifiedAtUtc`, `Version (int)`
   - Helper methods: `IsOverdue(DateTimeOffset nowUtc)`, `HasActiveRestriction` (computed from Status + RightType)

2. **`Projections/DSRRequestProjection.cs`** — Event → ReadModel transformation
   - Implements `IProjectionHandler<DSRRequestSubmitted, DSRRequestReadModel>` (creates)
   - Implements `IProjectionHandler<DSRRequestVerified, DSRRequestReadModel>` (updates Status, VerifiedAtUtc)
   - Implements `IProjectionHandler<DSRRequestProcessing, DSRRequestReadModel>` (updates Status, ProcessedByUserId)
   - Implements `IProjectionHandler<DSRRequestCompleted, DSRRequestReadModel>` (updates Status, CompletedAtUtc)
   - Implements `IProjectionHandler<DSRRequestDenied, DSRRequestReadModel>` (updates Status, RejectionReason, CompletedAtUtc)
   - Implements `IProjectionHandler<DSRRequestExtended, DSRRequestReadModel>` (updates Status, ExtensionReason, ExtendedDeadlineAtUtc)
   - Implements `IProjectionHandler<DSRRequestExpired, DSRRequestReadModel>` (updates Status)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of migrating Encina.Compliance.DataSubjectRights to Marten event sourcing (Issue #778).

CONTEXT:
- Phase 1 completed: DSRRequestAggregate and 7 event types exist
- Read models are query-optimized views built from event streams
- IReadModel from Encina.Marten.Projections is the marker interface
- IProjectionHandler<TEvent, TReadModel> processes individual events into read model updates
- DSRRequestStatus enum: Received, IdentityVerified, InProgress, Completed, Rejected, Extended, Expired

TASK:
1. Create Projections/DSRRequestReadModel.cs implementing IReadModel
2. Create Projections/DSRRequestProjection.cs implementing IProjectionHandler for each event type
3. The read model replaces the old DSRRequest entity for query purposes

KEY RULES:
- ReadModel has mutable setters (projections update properties incrementally)
- ReadModel must include LastModifiedAtUtc (updated on every event)
- DSRRequestSubmitted creates the read model; all other events update it
- Include helper: IsOverdue(DateTimeOffset nowUtc) — checks deadline vs current time
- Include helper: HasActiveRestriction — true if Status is Received/IdentityVerified/InProgress AND RightType is Restriction
- Follow patterns from src/Encina.Compliance.CrossBorderTransfer/ReadModels/
- Follow patterns from src/Encina.Compliance.Consent/ReadModels/

REFERENCE FILES:
- src/Encina.Compliance.CrossBorderTransfer/ReadModels/ApprovedTransferReadModel.cs
- src/Encina.Compliance.Consent/ReadModels/ConsentReadModel.cs
- src/Encina.Marten/Projections/IReadModel.cs
- src/Encina.Marten/Projections/IProjectionHandler.cs
- src/Encina.Compliance.DataSubjectRights/Events/DSRRequestEvents.cs (Phase 1 output)
- src/Encina.Compliance.DataSubjectRights/Model/DSRRequest.cs (existing — field reference)
```

</details>

---

### Phase 3: Service Interface & Implementation ✅ Completed

> **Goal**: Define `IDSRService` replacing `IDSRRequestStore` + `IDSRAuditStore` + `IDataSubjectRightsHandler`, and implement `DefaultDSRService` using `IAggregateRepository<DSRRequestAggregate>`. No in-memory implementation.

<details>
<summary><strong>Tasks</strong></summary>

#### New files in `src/Encina.Compliance.DataSubjectRights/`

1. **`Abstractions/IDSRService.cs`** — New unified service interface
   - Lifecycle command methods (return `ValueTask<Either<EncinaError, T>>`):
     - `SubmitRequestAsync(Guid id, string subjectId, DataSubjectRight rightType, DateTimeOffset receivedAtUtc, string? requestDetails, string? tenantId, string? moduleId, CancellationToken)` → `Either<EncinaError, Guid>`
     - `VerifyIdentityAsync(Guid requestId, string verifiedBy, CancellationToken)` → `Either<EncinaError, Unit>`
     - `StartProcessingAsync(Guid requestId, string? processedByUserId, CancellationToken)` → `Either<EncinaError, Unit>`
     - `CompleteRequestAsync(Guid requestId, CancellationToken)` → `Either<EncinaError, Unit>`
     - `DenyRequestAsync(Guid requestId, string rejectionReason, CancellationToken)` → `Either<EncinaError, Unit>`
     - `ExtendDeadlineAsync(Guid requestId, string extensionReason, CancellationToken)` → `Either<EncinaError, Unit>`
   - Handler operations (from `IDataSubjectRightsHandler`):
     - `HandleAccessAsync(AccessRequest request, CancellationToken)` → `Either<EncinaError, AccessResponse>`
     - `HandleRectificationAsync(RectificationRequest request, CancellationToken)` → `Either<EncinaError, Unit>`
     - `HandleErasureAsync(ErasureRequest request, CancellationToken)` → `Either<EncinaError, ErasureResult>`
     - `HandleRestrictionAsync(RestrictionRequest request, CancellationToken)` → `Either<EncinaError, Unit>`
     - `HandlePortabilityAsync(PortabilityRequest request, CancellationToken)` → `Either<EncinaError, PortabilityResponse>`
     - `HandleObjectionAsync(ObjectionRequest request, CancellationToken)` → `Either<EncinaError, Unit>`
   - Query methods:
     - `GetRequestAsync(Guid requestId, CancellationToken)` → `Either<EncinaError, DSRRequestReadModel>`
     - `GetRequestsBySubjectAsync(string subjectId, CancellationToken)` → `Either<EncinaError, IReadOnlyList<DSRRequestReadModel>>`
     - `GetPendingRequestsAsync(CancellationToken)` → `Either<EncinaError, IReadOnlyList<DSRRequestReadModel>>`
     - `GetOverdueRequestsAsync(CancellationToken)` → `Either<EncinaError, IReadOnlyList<DSRRequestReadModel>>`
     - `HasActiveRestrictionAsync(string subjectId, CancellationToken)` → `Either<EncinaError, bool>`
     - `GetRequestHistoryAsync(Guid requestId, CancellationToken)` → `Either<EncinaError, IReadOnlyList<object>>` (raw event stream for audit)

2. **`Services/DefaultDSRService.cs`** — Implementation using Marten
   - Constructor: `IAggregateRepository<DSRRequestAggregate>`, `IReadModelRepository<DSRRequestReadModel>`, `IPersonalDataLocator`, `IDataErasureExecutor`, `IDataPortabilityExporter`, `ICacheProvider` (optional), `TimeProvider`, `ILogger<DefaultDSRService>`
   - Lifecycle commands: load aggregate → execute command → save via `_repository.SaveAsync()`
   - Handler operations: orchestrate the operation using existing executors/locators, then update aggregate lifecycle
   - Query methods: query `IReadModelRepository<DSRRequestReadModel>` projections
   - `HasActiveRestrictionAsync`: queries read model for active restriction requests by subject
   - Cache invalidation on write operations (cache key pattern: `"dsr:request:{id}"`, `"dsr:subject:{subjectId}"`)

#### Files to DELETE from `src/Encina.Compliance.DataSubjectRights/`

1. **DELETE** `Abstractions/IDSRRequestStore.cs` — Replaced by `IDSRService`
2. **DELETE** `Abstractions/IDSRAuditStore.cs` — Inherent in event stream
3. **DELETE** `Abstractions/IDataSubjectRightsHandler.cs` — Absorbed into `IDSRService`
4. **DELETE** `InMemory/InMemoryDSRRequestStore.cs` — No longer needed (unit tests mock `IAggregateRepository<T>`)
5. **DELETE** `InMemory/InMemoryDSRAuditStore.cs` — Audit is inherent in event stream
6. **DELETE** `DefaultDataSubjectRightsHandler.cs` — Logic moved to `DefaultDSRService`
7. **DELETE** `DSRRequestEntity.cs` — No entity-based persistence
8. **DELETE** `DSRRequestMapper.cs` — No entity mapping needed
9. **DELETE** `DSRAuditEntryEntity.cs` — No entity-based persistence
10. **DELETE** `DSRAuditEntryMapper.cs` — No entity mapping needed

#### Files to KEEP (not store-dependent)

- `Model/DSRRequest.cs` — Domain record (still useful as reference type/DTO)
- `Model/DSRAuditEntry.cs` — May be useful for serialization; ES events replace its purpose
- All `Model/*.cs` (DataSubjectRight, DSRRequestStatus, AccessResponse, PortabilityResponse, ErasureResult, etc.)
- All `Requests/*.cs` (AccessRequest, ErasureRequest, etc.)
- All `Notifications/*.cs` (DataErasedNotification, etc.)
- All `Abstractions/` EXCEPT the 3 deleted above (IPersonalDataLocator, IDataErasureExecutor, IDataPortabilityExporter, IExportFormatWriter, IDataSubjectIdExtractor, IDataErasureStrategy remain)
- All `Erasure/*.cs`, `Export/*.cs`, `Locators/*.cs` (handler infrastructure)
- All `Attributes/*.cs` (PersonalData, RestrictProcessing)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of migrating Encina.Compliance.DataSubjectRights to Marten event sourcing (Issue #778).

CONTEXT:
- Phases 1-2 completed: DSRRequestAggregate, 7 event types, DSRRequestReadModel, DSRRequestProjection exist
- This phase replaces IDSRRequestStore + IDSRAuditStore + IDataSubjectRightsHandler with a single IDSRService
- DefaultDSRService uses IAggregateRepository<DSRRequestAggregate> for writes and IReadModelRepository<DSRRequestReadModel> for queries
- The service also orchestrates handler operations (access, erasure, portability, etc.) using existing executor/locator abstractions
- No InMemory implementation — unit tests mock IAggregateRepository<T>; integration tests use real Marten
- Railway Oriented Programming: all methods return Either<EncinaError, T>

TASK:
1. Create Abstractions/IDSRService.cs with lifecycle commands + handler operations + query methods
2. Create Services/DefaultDSRService.cs using IAggregateRepository + IReadModelRepository + existing executors/locators
3. DELETE old interfaces: IDSRRequestStore.cs, IDSRAuditStore.cs, IDataSubjectRightsHandler.cs
4. DELETE old implementations: InMemoryDSRRequestStore.cs, InMemoryDSRAuditStore.cs, DefaultDataSubjectRightsHandler.cs
5. DELETE entity/mapper files: DSRRequestEntity.cs, DSRRequestMapper.cs, DSRAuditEntryEntity.cs, DSRAuditEntryMapper.cs

KEY RULES:
- All methods return ValueTask<Either<EncinaError, T>> (ROP)
- DefaultDSRService: load aggregate → command → SaveAsync for lifecycle methods
- Handler operations (HandleAccessAsync, HandleErasureAsync, etc.): orchestrate using IPersonalDataLocator, IDataErasureExecutor, IDataPortabilityExporter, then update aggregate lifecycle
- HasActiveRestrictionAsync: query read model for active restriction requests by subject
- GetRequestHistoryAsync: returns raw event stream (audit trail)
- Use TryAddScoped in DI — DefaultDSRService is the only implementation
- PRESERVE existing Notifications/, Erasure/, Export/, Locators/, Attributes/ — they are not store-dependent
- Use DSRErrors for all error responses (existing error factory)
- Follow patterns from src/Encina.Compliance.CrossBorderTransfer/Services/DefaultApprovedTransferService.cs
- Follow patterns from src/Encina.Compliance.Consent/Services/DefaultConsentService.cs

REFERENCE FILES:
- src/Encina.Compliance.CrossBorderTransfer/Services/DefaultApprovedTransferService.cs
- src/Encina.Compliance.Consent/Services/DefaultConsentService.cs
- src/Encina.Compliance.Consent/Abstractions/IConsentService.cs
- src/Encina.Marten/IAggregateRepository.cs
- src/Encina.Marten/Projections/IReadModelRepository.cs
- src/Encina.Compliance.DataSubjectRights/Abstractions/IDSRRequestStore.cs (being replaced — read for API parity)
- src/Encina.Compliance.DataSubjectRights/Abstractions/IDataSubjectRightsHandler.cs (being absorbed)
- src/Encina.Compliance.DataSubjectRights/DefaultDataSubjectRightsHandler.cs (logic being moved)
- src/Encina.Compliance.DataSubjectRights/DSRErrors.cs (reuse error factory)
```

</details>

---

### Phase 4: Configuration, DI & Marten Registration ✅ Completed

> **Goal**: Update `ServiceCollectionExtensions` and create Marten aggregate registration extensions.

<details>
<summary><strong>Tasks</strong></summary>

#### Modified files in `src/Encina.Compliance.DataSubjectRights/`

1. **`Encina.Compliance.DataSubjectRights.csproj`** — Add new project dependencies
   - Add `ProjectReference` to `Encina.DomainModeling` (for `AggregateBase`, `IAggregate`)
   - Add `ProjectReference` to `Encina.Marten` (for `IAggregateRepository<T>`, `IReadModel`, `IProjectionHandler`)
   - Add `ProjectReference` to `Encina.Caching` (for `ICacheProvider` in `DefaultDSRService`)
   - Follows the exact dependency pattern from `Encina.Compliance.CrossBorderTransfer.csproj`

2. **`ServiceCollectionExtensions.cs`** — Update DI registration
   - Replace `IDSRRequestStore → InMemoryDSRRequestStore` with `IDSRService → DefaultDSRService` (TryAddScoped)
   - Remove `IDSRAuditStore → InMemoryDSRAuditStore` registration
   - Remove `IDataSubjectRightsHandler → DefaultDataSubjectRightsHandler` registration
   - Keep: `DataSubjectRightsOptions`, `TimeProvider`, `IDataErasureExecutor`, `IDataErasureStrategy`, `IDataPortabilityExporter`, `IDataSubjectIdExtractor`, export format writers, `ProcessingRestrictionPipelineBehavior<,>`, health check, auto-registration

3. **`DSRMartenExtensions.cs`** — New Marten aggregate registration
   - `AddDSRRequestAggregates(this IServiceCollection services)` — registers `IAggregateRepository<DSRRequestAggregate>`
   - Follows `CrossBorderTransferMartenExtensions` pattern exactly
   - Separate from core registration so Marten is opt-in

4. **`ProcessingRestrictionPipelineBehavior.cs`** — Update dependency
   - Change from using `IDSRRequestStore.HasActiveRestrictionAsync()` to `IDSRService.HasActiveRestrictionAsync()`

5. **`DataSubjectRightsOptions.cs`** — Minor cleanup
   - Remove any references to store-related configuration if present

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of migrating Encina.Compliance.DataSubjectRights to Marten event sourcing (Issue #778).

CONTEXT:
- Phases 1-3 completed: DSRRequestAggregate, events, read model, IDSRService, DefaultDSRService
- Old interfaces (IDSRRequestStore, IDSRAuditStore, IDataSubjectRightsHandler) have been deleted
- Old implementations deleted — no InMemory implementations remain
- ProcessingRestrictionPipelineBehavior currently depends on IDSRRequestStore — must be updated to IDSRService

TASK:
1. Update Encina.Compliance.DataSubjectRights.csproj — add ProjectReferences to Encina.DomainModeling, Encina.Marten, Encina.Caching
2. Update ServiceCollectionExtensions.cs — replace old store/handler registrations with IDSRService → DefaultDSRService
3. Create DSRMartenExtensions.cs — AddDSRRequestAggregates() for Marten
4. Update ProcessingRestrictionPipelineBehavior.cs — change IDSRRequestStore dependency to IDSRService
5. Update DataSubjectRightsOptions.cs if it references deleted interfaces

KEY RULES:
- AddEncinaDataSubjectRights() registers DefaultDSRService (TryAddScoped — user can override)
- AddDSRRequestAggregates() registers Marten IAggregateRepository<DSRRequestAggregate> — call separately
- ProcessingRestrictionPipelineBehavior: only change is dependency injection — behavior logic stays the same
- Follow exactly the DI pattern from src/Encina.Compliance.CrossBorderTransfer/ServiceCollectionExtensions.cs
- Follow exactly the Marten registration from src/Encina.Compliance.CrossBorderTransfer/CrossBorderTransferMartenExtensions.cs
- Follow exactly the Consent migration pattern from src/Encina.Compliance.Consent/ServiceCollectionExtensions.cs

REFERENCE FILES:
- src/Encina.Compliance.CrossBorderTransfer/ServiceCollectionExtensions.cs
- src/Encina.Compliance.CrossBorderTransfer/CrossBorderTransferMartenExtensions.cs
- src/Encina.Compliance.Consent/ServiceCollectionExtensions.cs
- src/Encina.Compliance.Consent/ConsentMartenExtensions.cs
- src/Encina.Compliance.DataSubjectRights/ServiceCollectionExtensions.cs (current — being modified)
- src/Encina.Compliance.DataSubjectRights/ProcessingRestrictionPipelineBehavior.cs (current — being modified)
```

</details>

---

### Phase 5: Remove 13-Provider Store Implementations ✅ Completed

> **Goal**: Delete all provider-specific DSR store implementations and their DI registrations.

<details>
<summary><strong>Tasks</strong></summary>

#### DELETE from ADO.NET providers (8 files)

1. `src/Encina.ADO.Sqlite/DataSubjectRights/DSRRequestStoreADO.cs`
2. `src/Encina.ADO.Sqlite/DataSubjectRights/DSRAuditStoreADO.cs`
3. `src/Encina.ADO.SqlServer/DataSubjectRights/DSRRequestStoreADO.cs`
4. `src/Encina.ADO.SqlServer/DataSubjectRights/DSRAuditStoreADO.cs`
5. `src/Encina.ADO.PostgreSQL/DataSubjectRights/DSRRequestStoreADO.cs`
6. `src/Encina.ADO.PostgreSQL/DataSubjectRights/DSRAuditStoreADO.cs`
7. `src/Encina.ADO.MySQL/DataSubjectRights/DSRRequestStoreADO.cs`
8. `src/Encina.ADO.MySQL/DataSubjectRights/DSRAuditStoreADO.cs`

#### DELETE from Dapper providers (8 files)

1. `src/Encina.Dapper.Sqlite/DataSubjectRights/DSRRequestStoreDapper.cs`
2. `src/Encina.Dapper.Sqlite/DataSubjectRights/DSRAuditStoreDapper.cs`
3. `src/Encina.Dapper.SqlServer/DataSubjectRights/DSRRequestStoreDapper.cs`
4. `src/Encina.Dapper.SqlServer/DataSubjectRights/DSRAuditStoreDapper.cs`
5. `src/Encina.Dapper.PostgreSQL/DataSubjectRights/DSRRequestStoreDapper.cs`
6. `src/Encina.Dapper.PostgreSQL/DataSubjectRights/DSRAuditStoreDapper.cs`
7. `src/Encina.Dapper.MySQL/DataSubjectRights/DSRRequestStoreDapper.cs`
8. `src/Encina.Dapper.MySQL/DataSubjectRights/DSRAuditStoreDapper.cs`

#### DELETE from EF Core (7 files)

1. `src/Encina.EntityFrameworkCore/DataSubjectRights/DSRRequestStoreEF.cs`
2. `src/Encina.EntityFrameworkCore/DataSubjectRights/DSRAuditStoreEF.cs`
3. `src/Encina.EntityFrameworkCore/DataSubjectRights/DSRRequestEntityConfiguration.cs`
4. `src/Encina.EntityFrameworkCore/DataSubjectRights/DSRAuditEntryEntityConfiguration.cs`
5. `src/Encina.EntityFrameworkCore/DataSubjectRights/DSRModelBuilderExtensions.cs`

#### DELETE from MongoDB (4 files)

1. `src/Encina.MongoDB/DataSubjectRights/DSRRequestStoreMongoDB.cs`
2. `src/Encina.MongoDB/DataSubjectRights/DSRAuditStoreMongoDB.cs`
3. `src/Encina.MongoDB/DataSubjectRights/DSRRequestDocument.cs`
4. `src/Encina.MongoDB/DataSubjectRights/DSRAuditEntryDocument.cs`

#### MODIFY ServiceCollectionExtensions in each provider (10 files)

1. Remove `if (config.UseDataSubjectRights) { ... }` blocks from:
    - `src/Encina.ADO.Sqlite/ServiceCollectionExtensions.cs`
    - `src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs`
    - `src/Encina.ADO.PostgreSQL/ServiceCollectionExtensions.cs`
    - `src/Encina.ADO.MySQL/ServiceCollectionExtensions.cs`
    - `src/Encina.Dapper.Sqlite/ServiceCollectionExtensions.cs`
    - `src/Encina.Dapper.SqlServer/ServiceCollectionExtensions.cs`
    - `src/Encina.Dapper.PostgreSQL/ServiceCollectionExtensions.cs`
    - `src/Encina.Dapper.MySQL/ServiceCollectionExtensions.cs`
    - `src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs`
    - `src/Encina.MongoDB/ServiceCollectionExtensions.cs`

2. Remove `UseDataSubjectRights` property from provider config options if present

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of migrating Encina.Compliance.DataSubjectRights to Marten event sourcing (Issue #778).

CONTEXT:
- Phases 1-4 completed: new aggregate, events, service, DI are in place
- This phase removes ALL 13-provider DSR store implementations
- ADR-019 mandates: "Store interfaces with 13-provider implementations are replaced, not deprecated"
- Pre-1.0: no backward compatibility, no [Obsolete], no migration helpers

TASK:
1. DELETE all DataSubjectRights/ folders and files from ADO.NET providers (4 providers × 2 stores)
2. DELETE all DataSubjectRights/ folders and files from Dapper providers (4 providers × 2 stores)
3. DELETE all DataSubjectRights/ folders and files from EF Core (stores + entities + configurations + model builder)
4. DELETE all DataSubjectRights/ folders and files from MongoDB (stores + documents)
5. REMOVE UseDataSubjectRights registration blocks from all provider ServiceCollectionExtensions
6. Remove UseDataSubjectRights property from provider config options if it exists

KEY RULES:
- Delete files completely — no [Obsolete], no comments, no backup
- Git history preserves everything
- Clean up empty DataSubjectRights/ directories after deletion
- ServiceCollectionExtensions: remove the `if (config.UseDataSubjectRights)` block and its contents
- Do NOT remove other UseXxx blocks (e.g., UseOutbox, UseLawfulBasis) — only UseDataSubjectRights
- Verify no compile errors after deletion

REFERENCE FILES:
- src/Encina.ADO.Sqlite/ServiceCollectionExtensions.cs (look for UseDataSubjectRights block)
- src/Encina.Dapper.SqlServer/ServiceCollectionExtensions.cs (look for UseDataSubjectRights block)
- src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs (look for UseDataSubjectRights block)
- src/Encina.MongoDB/ServiceCollectionExtensions.cs (look for UseDataSubjectRights block)
```

</details>

---

### Phase 6: Observability Update ✅ Completed

> **Goal**: Update diagnostics to reflect the event-sourced model while keeping existing EventId range (8300-8346).

<details>
<summary><strong>Tasks</strong></summary>

#### Modified files in `src/Encina.Compliance.DataSubjectRights/`

1. **`Diagnostics/DataSubjectRightsDiagnostics.cs`** — Update metrics
   - Keep existing: ActivitySource `Encina.Compliance.DataSubjectRights`, Meter, all dsr.* counters and histograms
   - Add new counters for aggregate lifecycle operations:
     - `dsr.requests.submitted.total` — DSR requests submitted
     - `dsr.requests.verified.total` — identity verifications
     - `dsr.requests.completed.total` — requests completed
     - `dsr.requests.denied.total` — requests denied
     - `dsr.requests.extended.total` — deadline extensions
     - `dsr.requests.expired.total` — expired requests
   - Update Activity tags to match event-sourced model

2. **`Diagnostics/DSRLogMessages.cs`** — Update log messages
   - Keep pipeline-related logs (8335-8341) — unchanged (ProcessingRestrictionPipelineBehavior)
   - Keep auto-registration logs (8300-8309) — unchanged
   - Keep health check logs (8310-8319) — unchanged
   - Replace handler logs (8320-8329) with service-level logs:
     - 8320: `DSRRequestSubmittedViaService` (replaces handler request started)
     - 8321: `DSRRequestCompletedViaService` (replaces handler request completed)
     - 8322: `DSRRequestFailed` (replaces handler request failed)
     - 8323-8329: Access, erasure, portability, rectification operations (keep semantics, update source)
   - Remove store-specific logs if any — store operations are now aggregate operations
   - Keep notification logs (8342-8345) — unchanged

3. **`Health/DataSubjectRightsHealthCheck.cs`** — Update dependency
   - Change from checking `IDSRRequestStore` to checking `IDSRService`
   - Verify `IDSRService` is resolvable via scoped resolution
   - Keep overdue request check via `IDSRService.GetOverdueRequestsAsync()`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of migrating Encina.Compliance.DataSubjectRights to Marten event sourcing (Issue #778).

CONTEXT:
- Phases 1-5 completed: event-sourced model in place, old stores deleted
- Observability already exists (DataSubjectRightsDiagnostics.cs, DSRLogMessages.cs, DataSubjectRightsHealthCheck.cs)
- EventId range: 8300-8346 (must stay within this range, no collisions)
- Consent uses 8200-8259; CrossBorderTransfer uses 8500-8555; Anonymization uses 8400-8499

TASK:
1. Update DataSubjectRightsDiagnostics.cs — add aggregate lifecycle counters (dsr.requests.submitted.total, etc.)
2. Update DSRLogMessages.cs — replace handler/store logs with service-level logs (keep same EventIds where possible)
3. Update DataSubjectRightsHealthCheck.cs — check IDSRService instead of IDSRRequestStore

KEY RULES:
- Keep EventId range 8300-8346 — no collisions
- Pipeline logs (8335-8341) are UNCHANGED — ProcessingRestrictionPipelineBehavior is preserved
- Auto-registration logs (8300-8309) are UNCHANGED
- Health check logs (8310-8319) are UNCHANGED
- Handler/service logs (8320-8329) get updated but keep same EventIds
- Notification logs (8342-8345) are UNCHANGED
- Follow DataSubjectRightsDiagnostics patterns for new counters

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/Diagnostics/DataSubjectRightsDiagnostics.cs (current)
- src/Encina.Compliance.DataSubjectRights/Diagnostics/DSRLogMessages.cs (current)
- src/Encina.Compliance.DataSubjectRights/Health/DataSubjectRightsHealthCheck.cs (current)
- src/Encina.Compliance.CrossBorderTransfer/Diagnostics/CrossBorderTransferDiagnostics.cs (reference)
- src/Encina.Compliance.Consent/Diagnostics/ConsentDiagnostics.cs (reference — recently migrated)
```

</details>

---

### Phase 7: Testing ✅ Completed

> **Goal**: Update all DSR tests to use the new event-sourced model.

<details>
<summary><strong>Tasks</strong></summary>

#### Unit Tests — `tests/Encina.UnitTests/Compliance/DataSubjectRights/`

1. **NEW** `Aggregates/DSRRequestAggregateTests.cs`
   - Test all aggregate commands and state transitions
   - Test invariant violations (complete non-in-progress, deny completed, extend expired)
   - Test event generation (each command produces correct event)
   - Test Apply() reconstructs correct state from events
   - Test 30-day deadline calculation, 2-month extension calculation
   - ~30-35 tests

2. **NEW** `Events/DSRRequestEventsTests.cs`
   - Test all 7 event record types — equality, immutability, required properties

3. **NEW** `Projections/DSRRequestProjectionTests.cs`
   - Test projection handler transforms events to read model correctly
   - Test full lifecycle: Submit → Verify → Process → Complete — verify read model state
   - Test IsOverdue() and HasActiveRestriction helpers
   - ~15-20 tests

4. **NEW** `Services/DefaultDSRServiceTests.cs`
   - Mock `IAggregateRepository<DSRRequestAggregate>` and `IReadModelRepository<DSRRequestReadModel>`
   - Test lifecycle commands delegate to aggregate correctly
   - Test handler operations orchestrate correctly
   - Test query methods use read model repository
   - Test error handling (aggregate not found, save failure)
   - Test cache invalidation on write operations
   - ~25-30 tests

5. **UPDATE** `ProcessingRestrictionPipelineBehaviorTests.cs`
   - Change mocked dependency from `IDSRRequestStore` to `IDSRService`
   - All test logic remains the same

6. **DELETE** old store tests:
   - `InMemoryDSRRequestStoreTests.cs`
   - `InMemoryDSRAuditStoreTests.cs`

7. **KEEP** (no changes needed):
   - `Model/DSRRequestTests.cs` (record still exists)
   - `Model/DSRRequestStatusTests.cs`
   - `DSRErrorsTests.cs`
   - `DataSubjectRightsOptionsTests.cs`, `DataSubjectRightsOptionsValidatorTests.cs`
   - Erasure, export, locator tests (not store-dependent)

#### Guard Tests — `tests/Encina.GuardTests/Compliance/DataSubjectRights/`

1. **NEW** `DSRRequestAggregateGuardTests.cs` — null checks on factory/command parameters
2. **NEW** `DefaultDSRServiceGuardTests.cs` — null checks on service method parameters
3. **UPDATE** `ProcessingRestrictionPipelineBehaviorGuardTests.cs` — update constructor dependencies
4. **DELETE** `InMemoryDSRRequestStoreGuardTests.cs`, `InMemoryDSRAuditStoreGuardTests.cs`

#### Property Tests — `tests/Encina.PropertyTests/Compliance/DataSubjectRights/`

1. **NEW** `DSRRequestAggregatePropertyTests.cs`
    - Invariant: Submit then Complete always produces Completed status (through valid transitions)
    - Invariant: Event stream replay always produces same state
    - Invariant: Version increments monotonically
    - Invariant: Deadline is always ReceivedAtUtc + 30 days

#### Contract Tests — `tests/Encina.ContractTests/Compliance/DataSubjectRights/`

1. **UPDATE** `IDSRRequestStoreContractTests.cs` → rename to `IDSRServiceContractTests.cs`
    - Test `IDSRService` contract (method signatures, return types, ROP patterns)
2. **DELETE** `IDSRAuditStoreContractTests.cs` — audit store removed

#### Integration Tests — `tests/Encina.IntegrationTests/`

1. **DELETE** all 13-provider DSR integration tests:
    - `ADO/*/DataSubjectRights/DSRRequestStore*Tests.cs` (4 providers × N test files)
    - `Dapper/*/DataSubjectRights/DSRRequestStore*Tests.cs` (4 providers × N test files)
    - `Infrastructure/EntityFrameworkCore/*/DataSubjectRights/DSRRequestStore*Tests.cs` (4 providers × N test files)
    - `Infrastructure/MongoDB/DataSubjectRights/DSRRequestStore*Tests.cs`

2. **NEW** `Compliance/DataSubjectRights/DSRMartenIntegrationTests.cs`
    - Uses Marten PostgreSQL fixture
    - Tests full aggregate lifecycle via `IDSRService`
    - Tests projection produces correct read model
    - Tests event stream audit trail (GetRequestHistoryAsync)
    - Tests `EventPublishingPipelineBehavior` auto-publishes unified events
    - Tests overdue request detection
    - ~15-20 tests

3. **UPDATE** `Compliance/DataSubjectRights/DSRPipelineIntegrationTests.cs`
    - Update to use `IDSRService` instead of `IDSRRequestStore`

#### Load/Benchmark Tests

1. **UPDATE** `tests/Encina.LoadTests/Compliance/DataSubjectRights/DSRRequestStoreLoadTests.cs` — update dependencies
2. **UPDATE** `tests/Encina.BenchmarkTests/Encina.Benchmarks/Compliance/DataSubjectRights/` — update dependencies

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of migrating Encina.Compliance.DataSubjectRights to Marten event sourcing (Issue #778).

CONTEXT:
- Phases 1-6 completed: full event-sourced DSR model, observability updated, old stores deleted
- Testing must cover: aggregate behavior, events, projections, service, pipeline behavior
- 13-provider integration tests are removed — replaced by Marten integration tests
- Guard, property, and contract tests need updating

TASK:
1. Create unit tests: DSRRequestAggregateTests, DSRRequestEventsTests, DSRRequestProjectionTests, DefaultDSRServiceTests
2. Update ProcessingRestrictionPipelineBehaviorTests (IDSRRequestStore → IDSRService)
3. Delete old store tests (InMemoryDSRRequestStoreTests, InMemoryDSRAuditStoreTests)
4. Create guard tests for new classes, delete old guard tests
5. Create property tests for aggregate invariants
6. Update contract tests (rename, delete audit store contract)
7. Delete all 13-provider integration tests
8. Create Marten integration test
9. Update pipeline integration test
10. Update load/benchmark tests

KEY RULES:
- Follow AAA pattern (Arrange, Act, Assert)
- Test ONE thing per test method
- Use descriptive test names: MethodName_Scenario_ExpectedResult
- Mock IAggregateRepository and IReadModelRepository in unit tests
- Integration tests use real Marten/PostgreSQL via xUnit [Collection] fixtures
- Guard tests: verify ArgumentNullException for all required parameters
- Property tests: use FsCheck for invariant verification
- Don't test implementation details — test behavior

REFERENCE FILES:
- tests/Encina.UnitTests/Compliance/CrossBorderTransfer/ (reference test patterns)
- tests/Encina.UnitTests/Compliance/Consent/ (recently migrated — closest reference)
- tests/Encina.GuardTests/Compliance/CrossBorderTransfer/
- tests/Encina.PropertyTests/Compliance/CrossBorderTransfer/
- tests/Encina.IntegrationTests/Compliance/ (reference Marten integration tests)
```

</details>

---

### Phase 8: Documentation & Finalization ✅ Completed

> **Goal**: Update all documentation, public API tracking, and verify build.

<details>
<summary><strong>Tasks</strong></summary>

1. **`PublicAPI.Unshipped.txt`** — Update public API surface
   - Remove: `IDSRRequestStore`, `IDSRAuditStore`, `IDataSubjectRightsHandler` and all their methods
   - Remove: `InMemoryDSRRequestStore`, `InMemoryDSRAuditStore`
   - Remove: `DefaultDataSubjectRightsHandler`
   - Remove: `DSRRequestEntity`, `DSRRequestMapper`, `DSRAuditEntryEntity`, `DSRAuditEntryMapper`
   - Add: `IDSRService` and all methods
   - Add: `DSRRequestAggregate` and public members
   - Add: `DSRRequestReadModel` and properties
   - Add: All 7 event records
   - Add: `DSRMartenExtensions.AddDSRRequestAggregates()`
   - Add: `DSRRequestProjection`

2. **`CHANGELOG.md`** — Add entry under Unreleased

   ```markdown
   ### Changed
   - **Encina.Compliance.DataSubjectRights**: Migrated from entity-based persistence (13 database providers) to Marten event sourcing (PostgreSQL). This provides an immutable audit trail for GDPR Art. 5(2) accountability and complete DSR lifecycle tracking. See ADR-019. (Fixes #778)
     - `IDSRRequestStore`, `IDSRAuditStore`, `IDataSubjectRightsHandler` → replaced by `IDSRService`
     - 13-provider store implementations removed (ADO.NET, Dapper, EF Core, MongoDB)
     - New: `DSRRequestAggregate`, `DSRRequestReadModel`, `DSRRequestProjection`, 7 lifecycle events
     - New dependencies: `Encina.DomainModeling`, `Encina.Marten`, `Encina.Caching`
     - Registration: `services.AddEncinaDataSubjectRights()` + `services.AddDSRRequestAggregates()` (Marten)
     - Preserved: `ProcessingRestrictionPipelineBehavior`, all handler infrastructure (locators, executors, exporters), attributes, auto-registration, notifications
   ```

3. **`ROADMAP.md`** — Update if DSR migration was a planned item

4. **`src/Encina.Compliance.DataSubjectRights/README.md`** — Update
   - Document event-sourced architecture
   - Update registration examples
   - Document PostgreSQL requirement (Marten)
   - Update usage examples (IDSRService API)

5. **`docs/INVENTORY.md`** — Remove deleted files, add new files

6. **XML documentation** — Verify all new public APIs have `<summary>`, `<remarks>`, `<param>`, `<returns>`, `<example>`

7. **Build verification**:

   ```bash
   dotnet build Encina.slnx --configuration Release
   ```

   Target: 0 errors, 0 warnings

8. **Test verification**:

   ```bash
   dotnet test Encina.slnx --configuration Release
   ```

   Target: all tests pass

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 (final) of migrating Encina.Compliance.DataSubjectRights to Marten event sourcing (Issue #778).

CONTEXT:
- Phases 1-7 completed: full migration done, tests updated
- This phase is documentation, public API tracking, and build verification
- CLAUDE.md mandates: "XML documentation on all public APIs", "PublicAPI.Unshipped.txt must track all public symbols"

TASK:
1. Update PublicAPI.Unshipped.txt — remove old interfaces/classes, add new ones
2. Add CHANGELOG.md entry under ### Changed in Unreleased section
3. Update README.md for the DataSubjectRights package
4. Update docs/INVENTORY.md if it exists
5. Verify all new public APIs have XML documentation
6. Run dotnet build --configuration Release → verify 0 errors, 0 warnings
7. Run dotnet test → verify all tests pass

KEY RULES:
- PublicAPI format: Namespace.Type.Member(params) -> ReturnType
- Nullable annotations: string! (non-null), string? (nullable)
- CHANGELOG follows Keep a Changelog format
- No AI attribution in commits (per CLAUDE.md)
- README should show registration (AddEncinaDataSubjectRights + AddDSRRequestAggregates) and testing strategy

REFERENCE FILES:
- src/Encina.Compliance.DataSubjectRights/PublicAPI.Unshipped.txt (current)
- src/Encina.Compliance.CrossBorderTransfer/README.md (reference)
- src/Encina.Compliance.Consent/README.md (recently migrated — closest reference)
- CHANGELOG.md (current)
```

</details>

---

## Research

### Relevant Standards/Specifications

| Standard | Article/Section | Relevance |
|----------|----------------|-----------|
| GDPR Art. 5(2) | Accountability principle | Must prove compliance — event sourcing provides immutable evidence |
| GDPR Art. 12(3) | Time limits | 30-day response deadline, 2-month extension — lifecycle tracking via ES |
| GDPR Art. 12(4) | Refusal communication | Denial must be justified — `DSRRequestDenied` event captures reason |
| GDPR Art. 12(6) | Identity verification | Verification step before processing — tracked as `DSRRequestVerified` event |
| GDPR Art. 15 | Right of access | `HandleAccessAsync` operation |
| GDPR Art. 16 | Right to rectification | `HandleRectificationAsync` operation |
| GDPR Art. 17 | Right to erasure | `HandleErasureAsync` operation; crypto-shredding for ES personal data |
| GDPR Art. 18 | Right to restriction | `HandleRestrictionAsync` operation; `ProcessingRestrictionPipelineBehavior` enforcement |
| GDPR Art. 19 | Notification obligation | `DataRectifiedNotification`, `DataErasedNotification` etc. preserved |
| GDPR Art. 20 | Right to data portability | `HandlePortabilityAsync` operation |
| GDPR Art. 21 | Right to object | `HandleObjectionAsync` operation |
| GDPR Art. 22 | Automated decision-making | `DataSubjectRight.AutomatedDecisionMaking` enum value |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `AggregateBase` | `src/Encina.DomainModeling/` | Base class for DSRRequestAggregate |
| `IAggregateRepository<T>` | `src/Encina.Marten/` | Persistence for DSRRequestAggregate |
| `IReadModel`, `IProjectionHandler` | `src/Encina.Marten/Projections/` | DSRRequestReadModel and DSRRequestProjection |
| `IReadModelRepository<T>` | `src/Encina.Marten/Projections/` | Query read models in DefaultDSRService |
| `AddAggregateRepository<T>()` | `src/Encina.Marten/ServiceCollectionExtensions.cs` | DI registration for aggregate repository |
| `ICacheProvider` | `src/Encina.Caching/` | Optional caching in DefaultDSRService |
| `DataSubjectRightsOptions` | `src/Encina.Compliance.DataSubjectRights/` | Preserved — configuration unchanged |
| `DataSubjectRightsDiagnostics` | `src/Encina.Compliance.DataSubjectRights/Diagnostics/` | Updated — add lifecycle counters |
| `ProcessingRestrictionPipelineBehavior` | `src/Encina.Compliance.DataSubjectRights/` | Adapted — IDSRRequestStore → IDSRService |
| `DataSubjectRightsHealthCheck` | `src/Encina.Compliance.DataSubjectRights/Health/` | Adapted — check IDSRService |
| `IPersonalDataLocator` | `src/Encina.Compliance.DataSubjectRights/Abstractions/` | Preserved — used by DefaultDSRService |
| `IDataErasureExecutor` | `src/Encina.Compliance.DataSubjectRights/Abstractions/` | Preserved — used by DefaultDSRService |
| `IDataPortabilityExporter` | `src/Encina.Compliance.DataSubjectRights/Abstractions/` | Preserved — used by DefaultDSRService |
| `[PersonalData]`, `[RestrictProcessing]` | `src/Encina.Compliance.DataSubjectRights/Attributes/` | Preserved — no changes |
| `DSRRequestStatus` enum | `src/Encina.Compliance.DataSubjectRights/Model/` | Preserved — reused in aggregate and read model |
| `DataSubjectRight` enum | `src/Encina.Compliance.DataSubjectRights/Model/` | Preserved — reused in aggregate and events |

> **Note on InMemory**: No `InMemoryDSRService` is created. CrossBorderTransfer and Consent (reference implementations) have no InMemory implementations either. Unit tests mock `IAggregateRepository<T>` with NSubstitute; integration tests use real Marten + PostgreSQL in Docker.

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| Encina.Marten | 1-32 | Aggregate lifecycle |
| Encina.Security | 8000-8004 | Security events |
| Encina.Compliance.GDPR | 8100-8199 | GDPR core |
| Encina.Compliance.Consent | 8200-8259 | Consent |
| **Encina.Compliance.DataSubjectRights** | **8300-8346** | **DSR (keeping same range)** |
| Encina.Compliance.Anonymization | 8400-8499 | Anonymization |
| Encina.Compliance.CrossBorderTransfer | 8500-8559 | Cross-border transfer |
| Encina.Compliance.DataResidency | 8600-8699 | Data residency |

> Note: DSR EventIds 8300-8346 are preserved. Handler logs (8320-8329) are updated to service-level semantics. No new EventId ranges needed.

### Estimated File Count

| Category | New | Modified | Deleted | Total Impact |
|----------|:---:|:--------:|:-------:|:------------:|
| Aggregates | 1 | 0 | 0 | 1 |
| Events (ES, unified) | 1 | 0 | 0 | 1 |
| Read Models/Projections | 2 | 0 | 0 | 2 |
| Service Interfaces | 1 | 0 | 3 | 4 |
| Service Implementations | 1 | 0 | 2 | 3 |
| Entity/Mapper (DELETE) | 0 | 0 | 4 | 4 |
| Configuration/DI | 1 | 2 | 0 | 3 |
| Diagnostics | 0 | 3 | 0 | 3 |
| Provider Stores (DELETE) | 0 | 0 | ~27 | 27 |
| Provider DI (MODIFY) | 0 | 10 | 0 | 10 |
| Unit Tests | 4 | 1 | 2 | 7 |
| Guard Tests | 2 | 1 | 2 | 5 |
| Property Tests | 1 | 0 | 0 | 1 |
| Contract Tests | 0 | 1 | 1 | 2 |
| Integration Tests | 1 | 2 | ~13 | 16 |
| Documentation | 0 | 4 | 0 | 4 |
| **TOTAL** | **~15** | **~24** | **~54** | **~93** |

---

## Combined AI Agent Prompt

<details>
<summary><strong>Complete Implementation Prompt — All Phases</strong></summary>

```
You are migrating Encina.Compliance.DataSubjectRights from entity-based persistence to Marten event sourcing (Issue #778).

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 compliance library using Railway Oriented Programming (Either<EncinaError, T>)
- Pre-1.0: breaking changes acceptable, no backward compatibility
- ADR-019 mandates migration of all stateful compliance modules to Marten event sourcing
- CrossBorderTransfer (#412) is the reference implementation — follow its patterns exactly
- Consent (#777) was migrated using the same pattern — follow as closest reference
- AggregateBase from Encina.DomainModeling provides: Id, Version, RaiseEvent<T>, Apply(object)
- IAggregateRepository<T> from Encina.Marten provides: LoadAsync, SaveAsync, CreateAsync
- IReadModel, IProjectionHandler from Encina.Marten.Projections for query-side projections

IMPLEMENTATION OVERVIEW:

Phase 1 — Events & Aggregate:
- Create Events/DSRRequestEvents.cs with 7 sealed record events implementing : INotification
  (DSRRequestSubmitted, DSRRequestVerified, DSRRequestProcessing, DSRRequestCompleted,
   DSRRequestDenied, DSRRequestExtended, DSRRequestExpired)
- Events are auto-published by EventPublishingPipelineBehavior after Marten commit
- Create Aggregates/DSRRequestAggregate.cs extending AggregateBase
- Aggregate tracks full DSR request lifecycle with state machine invariants
- Existing Notifications/ (DataErasedNotification, etc.) are PRESERVED — different concern

Phase 2 — Read Model & Projection:
- Create Projections/DSRRequestReadModel.cs implementing IReadModel
- Create Projections/DSRRequestProjection.cs implementing IProjectionHandler for each event

Phase 3 — Service Interface & Implementation:
- Create Abstractions/IDSRService.cs (replaces IDSRRequestStore + IDSRAuditStore + IDataSubjectRightsHandler)
- Create Services/DefaultDSRService.cs (uses IAggregateRepository + IReadModelRepository + existing executors/locators)
- No InMemory implementation — unit tests mock IAggregateRepository<T>
- DELETE old interfaces, in-memory implementations, handler, entity/mapper files

Phase 4 — Configuration & DI:
- Update ServiceCollectionExtensions.cs (IDSRService → DefaultDSRService via TryAddScoped)
- Create DSRMartenExtensions.cs (AddDSRRequestAggregates)
- Update ProcessingRestrictionPipelineBehavior (IDSRRequestStore → IDSRService)

Phase 5 — Remove 13-Provider Stores:
- DELETE all DataSubjectRights/ folders from ADO.NET (4), Dapper (4), EF Core, MongoDB
- REMOVE UseDataSubjectRights blocks from provider ServiceCollectionExtensions

Phase 6 — Observability:
- Update DataSubjectRightsDiagnostics.cs (add lifecycle counters)
- Update DSRLogMessages.cs (rename handler logs to service logs)
- Update DataSubjectRightsHealthCheck.cs (IDSRRequestStore → IDSRService)

Phase 7 — Testing:
- NEW: DSRRequestAggregateTests, DSRRequestEventsTests, DSRRequestProjectionTests, DefaultDSRServiceTests
- UPDATE: Pipeline behavior tests, guard tests, contract tests
- DELETE: Old store tests, 13-provider integration tests
- NEW: Marten integration test (real PostgreSQL in Docker)

Phase 8 — Documentation:
- Update PublicAPI.Unshipped.txt, CHANGELOG.md, README.md, INVENTORY.md
- Build and test verification

KEY PATTERNS TO FOLLOW:
- Aggregate: see src/Encina.Compliance.CrossBorderTransfer/Aggregates/ApprovedTransferAggregate.cs
- Events: see src/Encina.Compliance.CrossBorderTransfer/Events/ApprovedTransferEvents.cs
- Service: see src/Encina.Compliance.CrossBorderTransfer/Services/DefaultApprovedTransferService.cs
- DI: see src/Encina.Compliance.CrossBorderTransfer/ServiceCollectionExtensions.cs
- Marten extensions: see src/Encina.Compliance.CrossBorderTransfer/CrossBorderTransferMartenExtensions.cs
- Consent migration: see src/Encina.Compliance.Consent/ (all recently migrated files)
- Tests: see tests/Encina.UnitTests/Compliance/CrossBorderTransfer/

KEY RULES:
- .NET 10, C# 14, nullable enabled
- ROP: Either<EncinaError, T> on all service methods
- UNIFIED EVENTS: ES events implement : INotification — single type for both persistence and mediator
  EventPublishingPipelineBehavior auto-publishes them (.OfType<INotification>() filter)
- Existing Notifications/ (DataErasedNotification, etc.) are PRESERVED — handler operation results
- Events are sealed records, aggregates are sealed classes
- All public types need XML documentation
- EventId range: 8300-8346 (existing allocation)
- New project dependencies: Encina.DomainModeling, Encina.Marten, Encina.Caching
- TenantId and ModuleId on events/aggregate for cross-cutting support
- NO InMemory implementation — DefaultDSRService registered via TryAddScoped
- No [Obsolete], no migration helpers, no backward compatibility
- PRESERVE all handler infrastructure: IPersonalDataLocator, IDataErasureExecutor, IDataPortabilityExporter,
  Erasure/, Export/, Locators/, Attributes/, Notifications/, Requests/, most Model/ files
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | **Caching** | ✅ Include | `DefaultDSRService` caches read model queries via `ICacheProvider` (optional dependency). Cache invalidation on write operations. Pattern: `"dsr:request:{id}"`, `"dsr:subject:{subjectId}"` |
| 2 | **OpenTelemetry** | ✅ Include | Existing `DataSubjectRightsDiagnostics` updated — ActivitySource preserved, new counters for lifecycle operations (dsr.requests.submitted.total, dsr.requests.completed.total, etc.) |
| 3 | **Structured Logging** | ✅ Include | Existing `DSRLogMessages.cs` updated — handler logs renamed to service logs, same EventId range 8300-8346 |
| 4 | **Health Checks** | ✅ Include | Existing `DataSubjectRightsHealthCheck` updated — checks `IDSRService` resolvability and overdue requests instead of `IDSRRequestStore` |
| 5 | **Validation** | ✅ Include | Aggregate invariants enforce valid state transitions. `ProcessingRestrictionPipelineBehavior` preserved — adapted to use `IDSRService` |
| 6 | **Resilience** | ❌ N/A | Marten handles PostgreSQL retries internally. Handler operations (erasure, portability) use existing strategy/executor patterns |
| 7 | **Distributed Locks** | ❌ N/A | Marten uses optimistic concurrency (aggregate versioning). No shared mutable state requiring external locks |
| 8 | **Transactions** | ✅ Include | Inherent in Marten — events + projections committed in a single PostgreSQL transaction |
| 9 | **Idempotency** | ✅ Include | Aggregate versioning prevents duplicate event application. Optimistic concurrency via `Version` |
| 10 | **Multi-Tenancy** | ✅ Include | `TenantId` field on aggregate and events. Marten supports native tenancy via `IDocumentSession` conjoined tenancy |
| 11 | **Module Isolation** | ✅ Include | `ModuleId` field on aggregate and events for modular monolith scoping |
| 12 | **Audit Trail** | ✅ Include | **Inherent** — the event stream IS the audit trail. `GetRequestHistoryAsync()` returns raw event stream. No separate audit store needed. Separate `DSRAuditEntry` / `IDSRAuditStore` are removed |

---

## Dependencies & Prerequisites

| Dependency | Status | Notes |
|------------|--------|-------|
| [#776 ADR-019: Compliance Event Sourcing Strategy](https://github.com/dlrivada/Encina/issues/776) | ✅ Completed | Architectural decision in place |
| [#412 CrossBorderTransfer (reference implementation)](https://github.com/dlrivada/Encina/issues/412) | ✅ Completed | Pattern established |
| [#777 Consent Migration](https://github.com/dlrivada/Encina/issues/777) | ✅ Completed | Same migration pattern validated |
| `Encina.Marten` package | ✅ Production | `IAggregateRepository<T>`, `IReadModel`, `IProjectionHandler`, `AddAggregateRepository<T>()` |
| `Encina.DomainModeling` package | ✅ Production | `AggregateBase`, `IAggregate` |
| PostgreSQL infrastructure | ✅ Available | Docker Compose profile: `databases` |

No blocking prerequisites identified. All required infrastructure exists.
