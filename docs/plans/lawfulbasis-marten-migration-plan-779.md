# Implementation Plan: Migrate `Encina.Compliance.LawfulBasis` to Marten Event Sourcing

> **Issue**: [#779](https://github.com/dlrivada/Encina/issues/779)
> **Type**: Refactor
> **Complexity**: High (8 phases, ~30 files to create/modify, ~40+ files to remove)
> **Estimated Scope**: ~1,800-2,500 lines of new production code + ~1,200-1,800 lines of tests; ~5,000+ lines removed from 13-provider stores
> **Prerequisite**: [#776 ADR-019](https://github.com/dlrivada/Encina/issues/776) ✅ Completed, [#412 CrossBorderTransfer](https://github.com/dlrivada/Encina/issues/412) ✅ Completed (reference implementation), [#777 Consent Migration](https://github.com/dlrivada/Encina/issues/777) ✅ Completed, [#778 DSR Migration](https://github.com/dlrivada/Encina/issues/778) ✅ Completed

---

## Summary

Migrate `Encina.Compliance.LawfulBasis` from entity-based persistence (13 database providers: ADO.NET ×4, Dapper ×4, EF Core ×4, MongoDB ×1) to **Marten event sourcing** (PostgreSQL only), as mandated by [ADR-019](../architecture/adr/019-compliance-event-sourcing-marten.md).

The module is currently embedded within the `Encina.Compliance.GDPR` package (`src/Encina.Compliance.GDPR/LawfulBasis/`). This migration **extracts it into its own package** (`src/Encina.Compliance.LawfulBasis/`) — following the same pattern as Consent, DataSubjectRights, and CrossBorderTransfer — and transforms it to event sourcing.

The refactoring transforms the current mutable `LawfulBasisRegistration` entity, `LIARecord` entity, and `ILawfulBasisRegistry`/`ILIAStore`/`ILegitimateInterestAssessment` interfaces (with 26+ provider-specific implementations) into:

- **2 event-sourced aggregates**: `LawfulBasisAggregate` (registration lifecycle) + `LIAAggregate` (Legitimate Interest Assessment lifecycle)
- **2 read models**: `LawfulBasisReadModel` + `LIAReadModel` with Marten projections
- **1 service interface** (`ILawfulBasisService`) replacing the three store/assessment interfaces
- **Audit trail inherent in the event stream** — no separate audit infrastructure needed

The `LawfulBasisValidationPipelineBehavior`, `LawfulBasisOptions`, `LawfulBasisAttribute`, `LawfulBasis` enum, `LawfulBasisEnforcementMode`, auto-registration infrastructure, and `ILawfulBasisSubjectIdExtractor` are **preserved** — the pipeline behavior and provider are adapted to depend on `ILawfulBasisService`. The old in-memory implementations (`InMemoryLawfulBasisRegistry`, `InMemoryLIAStore`) are **deleted** — unit tests use NSubstitute mocks of `IAggregateRepository<T>`, and integration tests use real Marten with PostgreSQL in Docker.

**Provider category**: Event Sourcing (Marten/PostgreSQL — specialized provider, not the 13-database category).

**New dependencies for `Encina.Compliance.LawfulBasis`**:

- `Encina.DomainModeling` — `AggregateBase`, `IAggregate` (DDD building blocks for event-sourced aggregates)
- `Encina.Marten` — `IAggregateRepository<T>`, `IReadModel`, `IProjectionHandler` (event store infrastructure)
- `Encina.Caching` — `ICacheProvider` (optional caching for read model queries)
- `Encina.Compliance.GDPR` — `LawfulBasis` enum, `LawfulBasisAttribute`, shared GDPR types

> **Note on event types**: Event store events (e.g., `LawfulBasisRegistered`) are **sealed records implementing `INotification`** — they do NOT extend `DomainEvent`/`IDomainEvent` from DomainModeling. Marten manages event metadata (timestamps, sequence, correlation). By implementing `INotification`, these events are **automatically published** by `EventPublishingPipelineBehavior` after successful command execution.

> **Note on package extraction**: The `LawfulBasis` enum and `LawfulBasisAttribute` **remain in `Encina.Compliance.GDPR`** — they are core GDPR types used by other modules (e.g., `ProcessingActivityAttribute`). Everything else (registrations, LIA records, pipeline behavior, validation, health checks, diagnostics, auto-registration) moves to the new `Encina.Compliance.LawfulBasis` package.

**Packages affected**:

- `Encina.Compliance.LawfulBasis` — **new package** (extracted from `Encina.Compliance.GDPR`)
- `Encina.Compliance.GDPR` — LawfulBasis subfolder, abstractions, pipeline, options, diagnostics **removed**
- `Encina.ADO.Sqlite`, `Encina.ADO.SqlServer`, `Encina.ADO.PostgreSQL`, `Encina.ADO.MySQL` — LawfulBasis stores **removed**
- `Encina.Dapper.Sqlite`, `Encina.Dapper.SqlServer`, `Encina.Dapper.PostgreSQL`, `Encina.Dapper.MySQL` — LawfulBasis stores **removed**
- `Encina.EntityFrameworkCore` — LawfulBasis stores, entities, configurations **removed**
- `Encina.MongoDB` — LawfulBasis stores, documents **removed**

---

## Design Choices

<details>
<summary><strong>1. Aggregate Design — Two aggregates: LawfulBasisAggregate + LIAAggregate</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Two aggregates (LawfulBasis + LIA)** | Separate lifecycles (registration vs. assessment), follows CrossBorderTransfer 3-aggregate pattern, LIA can be referenced by multiple registrations, clear domain boundaries | Two streams per registration-with-LIA |
| **B) Single aggregate combining both** | Fewer aggregates, simpler DI | Violates single-responsibility — registration lifecycle and assessment lifecycle are independent. LIA reuse across registrations becomes awkward |
| **C) Single aggregate + LIA as value object** | Simple model | LIA records are not simple values — they have governance metadata, review cycles, and DPO involvement |

### Chosen Option: **A — Two aggregates**

### Rationale

- **LawfulBasisAggregate**: Manages the mapping of a request type to a lawful basis. Lifecycle: Register → Change → Revoke. Aggregate ID is deterministic from `RequestTypeName` (one registration per request type)
- **LIAAggregate**: Manages the EDPB three-part test document. Lifecycle: Create → Approve/Reject → Schedule Review. Aggregate ID is the LIA reference string (e.g., `"LIA-2024-FRAUD-001"`)
- LIA records can be referenced by multiple registrations (many-to-one)
- Follows CrossBorderTransfer pattern: TIAAggregate, SCCAgreementAggregate, ApprovedTransferAggregate — separate lifecycles, separate aggregates
- Marten handles many small streams efficiently via PostgreSQL JSONB

</details>

<details>
<summary><strong>2. Interface Transformation — Replace 3 interfaces with 1 ILawfulBasisService</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Single `ILawfulBasisService` combining all operations** | Clean API, matches ES model, simpler DI | Larger interface |
| **B) Keep 3 interfaces, adapt to ES** | Minimal API change | Artificial separation — registry, LIA store, and validation are tightly coupled in ES |
| **C) CQRS split (command + query services)** | Clean CQRS | Over-engineering for a compliance module |

### Chosen Option: **A — Single `ILawfulBasisService`**

### Rationale

- In event sourcing, the registry IS the aggregate — a separate `ILawfulBasisRegistry` adds no value
- LIA management is a command on the aggregate, not a separate store concern
- `ILegitimateInterestAssessment` validation logic can be absorbed into the service (it validates LIA existence + approval)
- Follows CrossBorderTransfer/Consent/DSR pattern: each module has a single unified service
- Pre-1.0: breaking changes are expected and encouraged

</details>

<details>
<summary><strong>3. Event Model — 3 registration events + 4 LIA events, unified (ES + mediator)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Fine-grained events, unified ES + mediator** | One type = one fact (DRY), auto-published via `EventPublishingPipelineBehavior` | ES events depend on `INotification` (marker interface — zero cost) |
| **B) Separate ES + mediator types** | ES events decoupled from mediator | Two types per fact, manual mapping, more maintenance |
| **C) Generic `LawfulBasisStateChanged` event** | One event type | Loses semantic meaning |

### Chosen Option: **A — Fine-grained unified events implementing `INotification`**

### LawfulBasisAggregate Events

| Event | Trigger | GDPR Article |
|-------|---------|:------------:|
| `LawfulBasisRegistered` | Request type mapped to a lawful basis | Art. 6(1) |
| `LawfulBasisChanged` | Basis changed (e.g., consent → legitimate interest) | Art. 6(1) |
| `LawfulBasisRevoked` | Registration removed | Art. 6(1) |

### LIAAggregate Events

| Event | Trigger | GDPR Article |
|-------|---------|:------------:|
| `LIACreated` | New EDPB 3-part assessment documented | Art. 6(1)(f) |
| `LIAApproved` | DPO/assessor approves the LIA | Art. 6(1)(f) |
| `LIARejected` | DPO/assessor rejects the LIA | Art. 6(1)(f) |
| `LIAReviewScheduled` | Next periodic review date set/updated | Art. 6(1)(f) |

### Rationale

- **DDD principle**: In event sourcing, aggregate events ARE domain events
- **Infrastructure already supports it**: `EventPublishingPipelineBehavior` filters with `.OfType<INotification>()` — events implementing `INotification` are automatically published
- **`INotification` is a marker interface** with zero methods — zero coupling cost
- **Follows established pattern**: CrossBorderTransfer, Consent, DSR all use unified events

</details>

<details>
<summary><strong>4. Package Extraction — New Encina.Compliance.LawfulBasis package</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Extract to new `Encina.Compliance.LawfulBasis` package** | Follows Consent/DSR/CrossBorderTransfer pattern, independent versioning, clean dependency graph | New .csproj, new solution entry |
| **B) Keep inside `Encina.Compliance.GDPR`** | No new package | GDPR package becomes bloated with aggregate infrastructure; doesn't match other compliance modules |

### Chosen Option: **A — Extract to new package**

### Rationale

- Consent, DataSubjectRights, CrossBorderTransfer are all separate packages — LawfulBasis should follow the same pattern
- `LawfulBasis` enum and `LawfulBasisAttribute` stay in GDPR (they're core types used by other modules)
- New package has clear dependencies: GDPR (for enum/attribute), DomainModeling, Marten, Caching
- Clean separation: GDPR = core types + orchestration, LawfulBasis = domain + persistence

</details>

<details>
<summary><strong>5. Pipeline Behavior — Adapt LawfulBasisValidationPipelineBehavior to use ILawfulBasisService</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Modify existing behavior to use `ILawfulBasisService`** | Minimal change, same external behavior | Needs careful wiring |
| **B) Rewrite pipeline behavior from scratch** | Clean slate | Unnecessary — the behavior logic is store-agnostic |

### Chosen Option: **A — Adapt existing behavior**

### Rationale

- `LawfulBasisValidationPipelineBehavior` already works correctly — it just needs its dependencies changed from `ILawfulBasisRegistry`/`ILIAStore`/`ILegitimateInterestAssessment` to `ILawfulBasisService`
- The attribute caching, enforcement modes, OpenTelemetry integration, and logging are all preserved
- `DefaultLawfulBasisProvider` is adapted similarly

</details>

<details>
<summary><strong>6. Testing Strategy — Mocks for unit tests, real Marten for integration tests (no InMemory)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) In-memory service** | No infrastructure for unit tests | Production code nobody deploys; adds maintenance burden ×9 modules |
| **B) Mock `IAggregateRepository<T>` + real Marten in Docker** | No unnecessary production code; precise per-test control | Requires Docker for integration tests (already available) |

### Chosen Option: **B — Mock-based unit tests + real Marten integration tests**

### Rationale

- **CrossBorderTransfer, Consent, DSR all follow this pattern** — no InMemory service implementations
- **Aggregates can be tested directly** (they're plain objects — no persistence needed)
- **Eliminates maintenance burden**: no `InMemoryLawfulBasisService` × 9 compliance modules

</details>

<details>
<summary><strong>7. Store Removal Strategy — Delete all 13-provider implementations</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Delete immediately, clean break** | No dead code, clear migration signal | Breaking change |
| **B) Keep as deprecated** | Backward compatibility | Pre-1.0: no backward compatibility; dead code prohibited |

### Chosen Option: **A — Delete immediately**

### Rationale

- Pre-1.0 policy: "No `[Obsolete]`, no backward compatibility, no migration helpers"
- ADR-019: "Store interfaces with 13-provider implementations are replaced, not deprecated"
- Git history preserves all deleted code
- Removes ~5,000+ lines of provider-specific SQL and mapping code

</details>

---

## Implementation Phases

### Phase 1: Package Extraction & Domain Events & Aggregates

> **Goal**: Create the new `Encina.Compliance.LawfulBasis` package, define the event-sourced aggregates and domain events.

<details>
<summary><strong>Tasks</strong></summary>

#### New package: `src/Encina.Compliance.LawfulBasis/`

1. **`Encina.Compliance.LawfulBasis.csproj`** — New project file
   - `<TargetFramework>net10.0</TargetFramework>`, nullable enabled
   - ProjectReferences: `Encina`, `Encina.DomainModeling`, `Encina.Marten`, `Encina.Caching`, `Encina.Compliance.GDPR`
   - PackageReferences: `Microsoft.Extensions.Diagnostics.HealthChecks`, `Microsoft.CodeAnalysis.PublicApiAnalyzers`
   - Add to `Encina.slnx` solution

2. **`Events/LawfulBasisEvents.cs`** — 3 sealed record events implementing `: INotification`
   - `LawfulBasisRegistered(Guid RegistrationId, string RequestTypeName, LawfulBasis Basis, string? Purpose, string? LIAReference, string? LegalReference, string? ContractReference, DateTimeOffset RegisteredAtUtc, string? TenantId, string? ModuleId) : INotification`
   - `LawfulBasisChanged(Guid RegistrationId, LawfulBasis OldBasis, LawfulBasis NewBasis, string? Purpose, string? LIAReference, string? LegalReference, string? ContractReference, DateTimeOffset ChangedAtUtc, string? TenantId, string? ModuleId) : INotification`
   - `LawfulBasisRevoked(Guid RegistrationId, string Reason, DateTimeOffset RevokedAtUtc, string? TenantId, string? ModuleId) : INotification`

3. **`Events/LIAEvents.cs`** — 4 sealed record events implementing `: INotification`
   - `LIACreated(Guid LIAId, string Reference, string Name, string Purpose, string LegitimateInterest, string Benefits, string ConsequencesIfNotProcessed, string NecessityJustification, IReadOnlyList<string> AlternativesConsidered, string DataMinimisationNotes, string NatureOfData, string ReasonableExpectations, string ImpactAssessment, IReadOnlyList<string> Safeguards, string AssessedBy, bool DPOInvolvement, DateTimeOffset AssessedAtUtc, string? Conditions, string? TenantId, string? ModuleId) : INotification`
   - `LIAApproved(Guid LIAId, string Conclusion, string ApprovedBy, DateTimeOffset ApprovedAtUtc, string? TenantId, string? ModuleId) : INotification`
   - `LIARejected(Guid LIAId, string Conclusion, string RejectedBy, DateTimeOffset RejectedAtUtc, string? TenantId, string? ModuleId) : INotification`
   - `LIAReviewScheduled(Guid LIAId, DateTimeOffset NextReviewAtUtc, string ScheduledBy, DateTimeOffset ScheduledAtUtc, string? TenantId, string? ModuleId) : INotification`

4. **`Aggregates/LawfulBasisAggregate.cs`** — Event-sourced aggregate
   - Extends `AggregateBase` (from `Encina.DomainModeling`)
   - Properties: `RequestTypeName (string)`, `Basis (LawfulBasis)`, `Purpose?`, `LIAReference?`, `LegalReference?`, `ContractReference?`, `RegisteredAtUtc`, `IsRevoked`, `RevokedAtUtc?`, `TenantId?`, `ModuleId?`
   - Factory: `static LawfulBasisAggregate Register(Guid id, string requestTypeName, LawfulBasis basis, string? purpose, string? liaReference, string? legalReference, string? contractReference, DateTimeOffset registeredAtUtc, string? tenantId, string? moduleId)`
   - Commands: `ChangeBasis(LawfulBasis newBasis, string? purpose, string? liaReference, string? legalReference, string? contractReference, DateTimeOffset changedAtUtc)`, `Revoke(string reason, DateTimeOffset revokedAtUtc)`
   - `Apply(object domainEvent)` — switch for 3 event types
   - Invariants: cannot change revoked registration, cannot revoke already-revoked

5. **`Aggregates/LIAAggregate.cs`** — Event-sourced aggregate
   - Extends `AggregateBase`
   - Properties: `Reference (string)`, `Name`, `Purpose`, all EDPB 3-part test fields, `Outcome (LIAOutcome)`, `Conclusion?`, `AssessedBy`, `DPOInvolvement`, `AssessedAtUtc`, `Conditions?`, `NextReviewAtUtc?`, `TenantId?`, `ModuleId?`
   - Factory: `static LIAAggregate Create(Guid id, string reference, string name, string purpose, ... all 3-part fields ..., string assessedBy, bool dpoInvolvement, DateTimeOffset assessedAtUtc, string? conditions, string? tenantId, string? moduleId)`
   - Commands: `Approve(string conclusion, string approvedBy, DateTimeOffset approvedAtUtc)`, `Reject(string conclusion, string rejectedBy, DateTimeOffset rejectedAtUtc)`, `ScheduleReview(DateTimeOffset nextReviewAtUtc, string scheduledBy, DateTimeOffset scheduledAtUtc)`
   - `Apply(object domainEvent)` — switch for 4 event types
   - Invariants: cannot approve/reject already-decided LIA, cannot approve LIA with outcome RequiresReview without changing outcome

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of migrating Encina.Compliance.LawfulBasis to Marten event sourcing (Issue #779).

CONTEXT:
- Encina is a .NET 10 / C# 14 library using Railway Oriented Programming (Either<EncinaError, T>)
- LawfulBasis code is currently inside src/Encina.Compliance.GDPR/LawfulBasis/ — it needs to be EXTRACTED into a new package
- ADR-019 mandates migration from entity-based to event-sourced persistence
- Reference implementation: src/Encina.Compliance.CrossBorderTransfer/Aggregates/ and Events/
- Consent (#777) and DSR (#778) migrations are completed — follow their exact patterns
- AggregateBase is from Encina.DomainModeling (Id: Guid, Version: int, RaiseEvent<T>, Apply(object))
- Events are sealed records with DateTimeOffset timestamps
- LawfulBasis enum and LawfulBasisAttribute STAY in Encina.Compliance.GDPR — only the module code moves
- LIAOutcome enum already exists in Encina.Compliance.GDPR: Approved, Rejected, RequiresReview

TASK:
1. Create new project: src/Encina.Compliance.LawfulBasis/Encina.Compliance.LawfulBasis.csproj
   - References: Encina, Encina.DomainModeling, Encina.Marten, Encina.Caching, Encina.Compliance.GDPR
   - Add to Encina.slnx solution
2. Create Events/LawfulBasisEvents.cs with 3 sealed record events (LawfulBasisRegistered, LawfulBasisChanged, LawfulBasisRevoked) implementing : INotification
3. Create Events/LIAEvents.cs with 4 sealed record events (LIACreated, LIAApproved, LIARejected, LIAReviewScheduled) implementing : INotification
4. Create Aggregates/LawfulBasisAggregate.cs extending AggregateBase
5. Create Aggregates/LIAAggregate.cs extending AggregateBase
6. Each event must include TenantId/ModuleId (for cross-cutting integration)
7. Aggregates MUST validate invariants

KEY RULES:
- .NET 10 / C# 14, nullable enabled
- All types are sealed records (events) or sealed classes (aggregates)
- All public types need XML documentation with <summary>, <remarks>, GDPR article references
- Events implement INotification (marker interface from Encina core)
- Events are UNIFIED: same type persisted by Marten AND published to mediator subscribers
- Events are immutable facts — no methods, only data
- Aggregate state changes ONLY through RaiseEvent → Apply pattern
- Follow exact patterns from src/Encina.Compliance.CrossBorderTransfer/Events/TIAEvents.cs
  and src/Encina.Compliance.Consent/Events/ConsentEvents.cs

REFERENCE FILES:
- src/Encina.Compliance.CrossBorderTransfer/Aggregates/TIAAggregate.cs
- src/Encina.Compliance.CrossBorderTransfer/Events/TIAEvents.cs
- src/Encina.Compliance.Consent/Aggregates/ConsentAggregate.cs
- src/Encina.Compliance.GDPR/LawfulBasis/LawfulBasisRegistration.cs (existing entity — source for properties)
- src/Encina.Compliance.GDPR/LawfulBasis/LIARecord.cs (existing entity — source for LIA properties)
- src/Encina.Compliance.GDPR/Model/LawfulBasis.cs (enum — stays in GDPR)
- src/Encina.Compliance.GDPR/LawfulBasis/LIAOutcome.cs (enum — stays in GDPR)
- src/Encina.DomainModeling/AggregateBase.cs (base class)
- src/Encina.Compliance.CrossBorderTransfer/Encina.Compliance.CrossBorderTransfer.csproj (reference .csproj)
```

</details>

---

### Phase 2: Read Models & Projections

> **Goal**: Create the query-optimized read models and Marten projections for both aggregates.

<details>
<summary><strong>Tasks</strong></summary>

#### New files in `src/Encina.Compliance.LawfulBasis/`

1. **`ReadModels/LawfulBasisReadModel.cs`** — Query-optimized view for registrations
   - Implements `IReadModel` (from `Encina.Marten.Projections`)
   - Properties: `Id (Guid)`, `RequestTypeName`, `Basis (LawfulBasis)`, `Purpose?`, `LIAReference?`, `LegalReference?`, `ContractReference?`, `RegisteredAtUtc`, `IsRevoked`, `RevokedAtUtc?`, `TenantId?`, `ModuleId?`, `LastModifiedAtUtc`, `Version (int)`

2. **`ReadModels/LawfulBasisProjection.cs`** — Event → ReadModel transformation
   - `IProjectionCreator<LawfulBasisRegistered, LawfulBasisReadModel>` (creates)
   - `IProjectionHandler<LawfulBasisChanged, LawfulBasisReadModel>` (updates)
   - `IProjectionHandler<LawfulBasisRevoked, LawfulBasisReadModel>` (updates)

3. **`ReadModels/LIAReadModel.cs`** — Query-optimized view for LIA records
   - Implements `IReadModel`
   - Properties mirror `LIARecord` + `Id (Guid)`, `Outcome (LIAOutcome)`, `Conclusion?`, `NextReviewAtUtc?`, `TenantId?`, `ModuleId?`, `LastModifiedAtUtc`, `Version (int)`

4. **`ReadModels/LIAProjection.cs`** — Event → ReadModel transformation
   - `IProjectionCreator<LIACreated, LIAReadModel>` (creates)
   - `IProjectionHandler<LIAApproved, LIAReadModel>` (updates)
   - `IProjectionHandler<LIARejected, LIAReadModel>` (updates)
   - `IProjectionHandler<LIAReviewScheduled, LIAReadModel>` (updates)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of migrating Encina.Compliance.LawfulBasis to Marten event sourcing (Issue #779).

CONTEXT:
- Phase 1 completed: LawfulBasisAggregate, LIAAggregate, and 7 event types exist in src/Encina.Compliance.LawfulBasis/
- Read models are query-optimized views built from event streams
- IReadModel from Encina.Marten.Projections is the marker interface
- IProjectionHandler<TEvent, TReadModel> processes individual events into read model updates

TASK:
1. Create ReadModels/LawfulBasisReadModel.cs implementing IReadModel
2. Create ReadModels/LawfulBasisProjection.cs implementing IProjectionHandler for each registration event
3. Create ReadModels/LIAReadModel.cs implementing IReadModel
4. Create ReadModels/LIAProjection.cs implementing IProjectionHandler for each LIA event

KEY RULES:
- ReadModel has mutable setters (projections update properties incrementally)
- ReadModel must include LastModifiedAtUtc (updated on every event)
- Creator events create the read model; handler events update it
- LIAReadModel must include all EDPB 3-part test fields for audit/reporting
- Follow patterns from src/Encina.Compliance.Consent/ReadModels/

REFERENCE FILES:
- src/Encina.Compliance.Consent/ReadModels/ConsentReadModel.cs
- src/Encina.Compliance.Consent/ReadModels/ConsentProjection.cs
- src/Encina.Marten/Projections/IReadModel.cs
- src/Encina.Marten/Projections/IProjectionHandler.cs
```

</details>

---

### Phase 3: Service Interface & Implementation

> **Goal**: Define `ILawfulBasisService` replacing `ILawfulBasisRegistry` + `ILIAStore` + `ILegitimateInterestAssessment`, and implement `DefaultLawfulBasisService`. No in-memory implementation.

<details>
<summary><strong>Tasks</strong></summary>

#### New files in `src/Encina.Compliance.LawfulBasis/`

1. **`Abstractions/ILawfulBasisService.cs`** — Unified service interface
   - Registration commands (return `ValueTask<Either<EncinaError, T>>`):
     - `RegisterAsync(Guid id, string requestTypeName, LawfulBasis basis, string? purpose, string? liaReference, string? legalReference, string? contractReference, string? tenantId, string? moduleId, CancellationToken)` → `Either<EncinaError, Guid>`
     - `ChangeBasisAsync(Guid registrationId, LawfulBasis newBasis, string? purpose, string? liaReference, string? legalReference, string? contractReference, CancellationToken)` → `Either<EncinaError, Unit>`
     - `RevokeAsync(Guid registrationId, string reason, CancellationToken)` → `Either<EncinaError, Unit>`
   - LIA commands:
     - `CreateLIAAsync(Guid id, string reference, string name, string purpose, ... all EDPB fields ..., CancellationToken)` → `Either<EncinaError, Guid>`
     - `ApproveLIAAsync(Guid liaId, string conclusion, string approvedBy, CancellationToken)` → `Either<EncinaError, Unit>`
     - `RejectLIAAsync(Guid liaId, string conclusion, string rejectedBy, CancellationToken)` → `Either<EncinaError, Unit>`
     - `ScheduleLIAReviewAsync(Guid liaId, DateTimeOffset nextReviewAtUtc, string scheduledBy, CancellationToken)` → `Either<EncinaError, Unit>`
   - Query methods:
     - `GetRegistrationAsync(Guid registrationId, CancellationToken)` → `Either<EncinaError, LawfulBasisReadModel>`
     - `GetRegistrationByRequestTypeAsync(string requestTypeName, CancellationToken)` → `Either<EncinaError, Option<LawfulBasisReadModel>>`
     - `GetAllRegistrationsAsync(CancellationToken)` → `Either<EncinaError, IReadOnlyList<LawfulBasisReadModel>>`
     - `GetLIAAsync(Guid liaId, CancellationToken)` → `Either<EncinaError, LIAReadModel>`
     - `GetLIAByReferenceAsync(string liaReference, CancellationToken)` → `Either<EncinaError, Option<LIAReadModel>>`
     - `GetPendingLIAReviewsAsync(CancellationToken)` → `Either<EncinaError, IReadOnlyList<LIAReadModel>>`
     - `HasApprovedLIAAsync(string liaReference, CancellationToken)` → `Either<EncinaError, bool>`

2. **`Services/DefaultLawfulBasisService.cs`** — Implementation using Marten
   - Constructor: `IAggregateRepository<LawfulBasisAggregate>`, `IAggregateRepository<LIAAggregate>`, `IReadModelRepository<LawfulBasisReadModel>`, `IReadModelRepository<LIAReadModel>`, `ICacheProvider`, `TimeProvider`, `ILogger<DefaultLawfulBasisService>`
   - Command methods: load aggregate → execute command → save via `_repository.SaveAsync()`
   - Query methods: query `IReadModelRepository<T>` projections with cache-aside pattern
   - `HasApprovedLIAAsync`: queries LIA read model, checks Outcome == Approved
   - Cache key patterns: `"lb:reg:{id}"`, `"lb:reg:type:{requestTypeName}"`, `"lb:lia:{id}"`, `"lb:lia:ref:{reference}"`

3. **`Errors/LawfulBasisErrors.cs`** — Domain error definitions
   - `RegistrationNotFound(Guid id)`, `LIANotFound(Guid id)`, `LIANotFoundByReference(string reference)`
   - `RegistrationAlreadyRevoked(Guid id)`, `LIAAlreadyDecided(Guid id)`
   - `StoreError(string operation, Exception ex)`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of migrating Encina.Compliance.LawfulBasis to Marten event sourcing (Issue #779).

CONTEXT:
- Phases 1-2 completed: aggregates, events, read models, projections exist
- This phase replaces ILawfulBasisRegistry + ILIAStore + ILegitimateInterestAssessment with a single ILawfulBasisService
- DefaultLawfulBasisService uses IAggregateRepository<T> for writes and IReadModelRepository<T> for queries
- No InMemory implementation — unit tests mock IAggregateRepository<T>; integration tests use real Marten
- Railway Oriented Programming: all methods return Either<EncinaError, T>

TASK:
1. Create Abstractions/ILawfulBasisService.cs with registration + LIA command + query methods
2. Create Services/DefaultLawfulBasisService.cs using IAggregateRepository + IReadModelRepository + ICacheProvider
3. Create Errors/LawfulBasisErrors.cs with domain-specific error constants

KEY RULES:
- All methods return ValueTask<Either<EncinaError, T>> (ROP)
- DefaultLawfulBasisService: load aggregate → command → SaveAsync pattern
- Cache-aside pattern for reads: check cache → miss → load from repository → cache with 5min TTL
- Fire-and-forget cache invalidation on writes
- Follow patterns from src/Encina.Compliance.CrossBorderTransfer/Services/DefaultApprovedTransferService.cs

REFERENCE FILES:
- src/Encina.Compliance.CrossBorderTransfer/Services/DefaultApprovedTransferService.cs
- src/Encina.Compliance.CrossBorderTransfer/Abstractions/IApprovedTransferService.cs
- src/Encina.Compliance.CrossBorderTransfer/Errors/CrossBorderTransferErrors.cs
- src/Encina.Marten/IAggregateRepository.cs
- src/Encina.Marten/Projections/IReadModelRepository.cs
```

</details>

---

### Phase 4: Configuration, DI, Pipeline & Marten Registration

> **Goal**: Move configuration, pipeline behavior, auto-registration, and provider from GDPR to the new package. Create Marten registration extensions.

<details>
<summary><strong>Tasks</strong></summary>

#### MOVE from `src/Encina.Compliance.GDPR/` to `src/Encina.Compliance.LawfulBasis/`

These files are **moved** (not just copied) — update namespaces from `Encina.Compliance.GDPR` to `Encina.Compliance.LawfulBasis`:

1. `LawfulBasisOptions.cs` → `LawfulBasisOptions.cs`
2. `LawfulBasisOptionsValidator.cs` → `LawfulBasisOptionsValidator.cs`
3. `LawfulBasisEnforcementMode.cs` → `LawfulBasisEnforcementMode.cs`
4. `LawfulBasisValidationPipelineBehavior.cs` → `Pipeline/LawfulBasisValidationPipelineBehavior.cs`
5. `LawfulBasisAutoRegistrationDescriptor.cs` → `AutoRegistration/LawfulBasisAutoRegistrationDescriptor.cs`
6. `LawfulBasisAutoRegistrationHostedService.cs` → `AutoRegistration/LawfulBasisAutoRegistrationHostedService.cs`
7. `LawfulBasis/DefaultLawfulBasisProvider.cs` → `Services/DefaultLawfulBasisProvider.cs`
8. `LawfulBasis/DefaultLegitimateInterestAssessment.cs` → absorbed into `DefaultLawfulBasisService` (DELETE)
9. `LawfulBasis/LawfulBasisValidationResult.cs` → `Model/LawfulBasisValidationResult.cs`
10. `LawfulBasis/LIAValidationResult.cs` → `Model/LIAValidationResult.cs`

#### MODIFY after move

1. **`Pipeline/LawfulBasisValidationPipelineBehavior.cs`** — Update dependencies
   - Change `ILawfulBasisRegistry` → `ILawfulBasisService`
   - Change `ILIAStore` → `ILawfulBasisService`
   - Change `ILegitimateInterestAssessment` → `ILawfulBasisService` (use `HasApprovedLIAAsync`)
   - Update namespace to `Encina.Compliance.LawfulBasis`

2. **`Services/DefaultLawfulBasisProvider.cs`** — Update dependencies
   - Change `ILawfulBasisRegistry` → `ILawfulBasisService`
   - Change `ILIAStore` → `ILawfulBasisService`
   - Update namespace

3. **`AutoRegistration/LawfulBasisAutoRegistrationHostedService.cs`** — Update dependency
   - Change `ILawfulBasisRegistry.RegisterAsync` → `ILawfulBasisService.RegisterAsync`
   - Update namespace

#### NEW files

1. **`ServiceCollectionExtensions.cs`** — DI registration
   - `AddEncinaLawfulBasis(Action<LawfulBasisOptions>? configure)` method
   - Register `ILawfulBasisService → DefaultLawfulBasisService` (TryAddScoped)
   - Register `ILawfulBasisProvider → DefaultLawfulBasisProvider` (TryAddScoped)
   - Register `LawfulBasisValidationPipelineBehavior<,>` (Transient)
   - Register `TimeProvider.System` (TryAddSingleton)
   - Conditional: health check, auto-registration hosted service
   - Keep `ILawfulBasisSubjectIdExtractor` (stays in GDPR since it's used by consent validation)

2. **`LawfulBasisMartenExtensions.cs`** — Marten aggregate registration
   - `AddLawfulBasisAggregates(this IServiceCollection services)`:
     - `services.AddAggregateRepository<LawfulBasisAggregate>()`
     - `services.AddAggregateRepository<LIAAggregate>()`
     - `services.AddProjection<LawfulBasisProjection, LawfulBasisReadModel>()`
     - `services.AddProjection<LIAProjection, LIAReadModel>()`

#### DELETE from `src/Encina.Compliance.GDPR/`

1. All files listed in MOVE section above
2. `Abstractions/ILawfulBasisRegistry.cs`
3. `Abstractions/ILIAStore.cs`
4. `Abstractions/ILegitimateInterestAssessment` interface (if separate from DefaultLegitimateInterestAssessment)
5. `LawfulBasis/InMemoryLawfulBasisRegistry.cs`
6. `LawfulBasis/InMemoryLIAStore.cs`
7. `LawfulBasis/LawfulBasisRegistration.cs` (replaced by aggregate)
8. `LawfulBasis/LawfulBasisRegistrationEntity.cs` (no longer needed)
9. `LawfulBasis/LawfulBasisRegistrationMapper.cs` (no longer needed)
10. `LawfulBasis/LIARecord.cs` (replaced by aggregate)
11. `LawfulBasis/LIARecordEntity.cs` (no longer needed)
12. `LawfulBasis/LIARecordMapper.cs` (no longer needed)
13. `Diagnostics/LawfulBasisDiagnostics.cs` (moved to new package — Phase 6)
14. `Diagnostics/LawfulBasisLogMessages.cs` (moved to new package — Phase 6)
15. `Health/LawfulBasisHealthCheck.cs` (moved to new package — Phase 6)

#### MODIFY `src/Encina.Compliance.GDPR/`

1. **`ServiceCollectionExtensions.cs`** — Remove `AddEncinaLawfulBasis()` if it exists here
2. Remove LawfulBasis-related DI registrations — these now live in the new package

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of migrating Encina.Compliance.LawfulBasis to Marten event sourcing (Issue #779).

CONTEXT:
- Phases 1-3 completed: aggregates, events, read models, ILawfulBasisService, DefaultLawfulBasisService exist
- This phase EXTRACTS code from Encina.Compliance.GDPR into the new Encina.Compliance.LawfulBasis package
- LawfulBasis enum and LawfulBasisAttribute STAY in Encina.Compliance.GDPR
- Pipeline behavior, options, auto-registration, provider are MOVED and ADAPTED
- Old interfaces (ILawfulBasisRegistry, ILIAStore) and InMemory implementations are DELETED

TASK:
1. Move files from Encina.Compliance.GDPR to Encina.Compliance.LawfulBasis (update namespaces)
2. Update LawfulBasisValidationPipelineBehavior dependencies (ILawfulBasisRegistry/ILIAStore → ILawfulBasisService)
3. Update DefaultLawfulBasisProvider dependencies
4. Update LawfulBasisAutoRegistrationHostedService dependencies
5. Create ServiceCollectionExtensions.cs with AddEncinaLawfulBasis()
6. Create LawfulBasisMartenExtensions.cs with AddLawfulBasisAggregates()
7. Delete old interfaces, InMemory implementations, entities, mappers from GDPR package
8. Clean up GDPR ServiceCollectionExtensions (remove LawfulBasis registrations)

KEY RULES:
- AddEncinaLawfulBasis() registers DefaultLawfulBasisService (TryAddScoped)
- AddLawfulBasisAggregates() registers Marten IAggregateRepository<T> for both aggregates — call separately
- Follow DI pattern from src/Encina.Compliance.CrossBorderTransfer/ServiceCollectionExtensions.cs
- Follow Marten registration from src/Encina.Compliance.CrossBorderTransfer/CrossBorderTransferMartenExtensions.cs

REFERENCE FILES:
- src/Encina.Compliance.CrossBorderTransfer/ServiceCollectionExtensions.cs
- src/Encina.Compliance.CrossBorderTransfer/CrossBorderTransferMartenExtensions.cs
- src/Encina.Compliance.Consent/ServiceCollectionExtensions.cs
- src/Encina.Compliance.GDPR/ServiceCollectionExtensions.cs (current — being modified)
```

</details>

---

### Phase 5: Remove 13-Provider Store Implementations

> **Goal**: Delete all provider-specific LawfulBasis store implementations and their DI registrations.

<details>
<summary><strong>Tasks</strong></summary>

#### DELETE from ADO.NET providers (8 files + SQL scripts)

1. `src/Encina.ADO.Sqlite/LawfulBasis/LawfulBasisRegistryADO.cs`
2. `src/Encina.ADO.Sqlite/LawfulBasis/LIAStoreADO.cs`
3. `src/Encina.ADO.SqlServer/LawfulBasis/LawfulBasisRegistryADO.cs`
4. `src/Encina.ADO.SqlServer/LawfulBasis/LIAStoreADO.cs`
5. `src/Encina.ADO.PostgreSQL/LawfulBasis/LawfulBasisRegistryADO.cs`
6. `src/Encina.ADO.PostgreSQL/LawfulBasis/LIAStoreADO.cs`
7. `src/Encina.ADO.MySQL/LawfulBasis/LawfulBasisRegistryADO.cs`
8. `src/Encina.ADO.MySQL/LawfulBasis/LIAStoreADO.cs`
9. SQL scripts: `src/Encina.ADO.*/Scripts/009_CreateLawfulBasisRegistrationsTable.sql` (and LIA table scripts)

#### DELETE from Dapper providers (8 files)

1. `src/Encina.Dapper.Sqlite/LawfulBasis/LawfulBasisRegistryDapper.cs`
2. `src/Encina.Dapper.Sqlite/LawfulBasis/LIAStoreDapper.cs`
3. `src/Encina.Dapper.SqlServer/LawfulBasis/LawfulBasisRegistryDapper.cs`
4. `src/Encina.Dapper.SqlServer/LawfulBasis/LIAStoreDapper.cs`
5. `src/Encina.Dapper.PostgreSQL/LawfulBasis/LawfulBasisRegistryDapper.cs`
6. `src/Encina.Dapper.PostgreSQL/LawfulBasis/LIAStoreDapper.cs`
7. `src/Encina.Dapper.MySQL/LawfulBasis/LawfulBasisRegistryDapper.cs`
8. `src/Encina.Dapper.MySQL/LawfulBasis/LIAStoreDapper.cs`

#### DELETE from EF Core (5 files)

1. `src/Encina.EntityFrameworkCore/LawfulBasis/LawfulBasisRegistryEF.cs`
2. `src/Encina.EntityFrameworkCore/LawfulBasis/LIAStoreEF.cs`
3. `src/Encina.EntityFrameworkCore/LawfulBasis/LawfulBasisRegistrationEntityConfiguration.cs`
4. `src/Encina.EntityFrameworkCore/LawfulBasis/LIARecordEntityConfiguration.cs`
5. `src/Encina.EntityFrameworkCore/LawfulBasis/LawfulBasisModelBuilderExtensions.cs`

#### DELETE from MongoDB (4 files)

1. `src/Encina.MongoDB/LawfulBasis/LawfulBasisRegistryMongoDB.cs`
2. `src/Encina.MongoDB/LawfulBasis/LIAStoreMongoDB.cs`
3. `src/Encina.MongoDB/LawfulBasis/LawfulBasisRegistrationDocument.cs`
4. `src/Encina.MongoDB/LawfulBasis/LIARecordDocument.cs`

#### MODIFY ServiceCollectionExtensions in each provider (10+ files)

1. Remove `if (config.UseLawfulBasis) { ... }` blocks from all 13 provider ServiceCollectionExtensions
2. Remove `UseLawfulBasis` property from provider config options if present

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of migrating Encina.Compliance.LawfulBasis to Marten event sourcing (Issue #779).

CONTEXT:
- Phases 1-4 completed: new aggregate, events, service, DI are in place
- This phase removes ALL 13-provider lawful basis store implementations
- ADR-019 mandates: "Store interfaces with 13-provider implementations are replaced, not deprecated"
- Pre-1.0: no backward compatibility, no [Obsolete], no migration helpers

TASK:
1. DELETE all LawfulBasis/ folders from ADO.NET providers (4 providers × 2 stores + SQL scripts)
2. DELETE all LawfulBasis/ folders from Dapper providers (4 providers × 2 stores)
3. DELETE all LawfulBasis/ folders from EF Core (stores + configurations + model builder)
4. DELETE all LawfulBasis/ folders from MongoDB (stores + documents)
5. REMOVE UseLawfulBasis registration blocks from all provider ServiceCollectionExtensions
6. Clean up empty LawfulBasis/ directories after deletion

KEY RULES:
- Delete files completely — no [Obsolete], no comments, no backup
- Git history preserves everything
- Do NOT remove other UseXxx blocks — only UseLawfulBasis
- Verify no compile errors after deletion

REFERENCE FILES:
- src/Encina.ADO.Sqlite/ServiceCollectionExtensions.cs (look for UseLawfulBasis block)
- src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs (look for UseLawfulBasis block)
- src/Encina.MongoDB/ServiceCollectionExtensions.cs (look for UseLawfulBasis block)
```

</details>

---

### Phase 6: Observability

> **Goal**: Move diagnostics to the new package with updated EventId range (8350-8399) and add aggregate operation counters.

<details>
<summary><strong>Tasks</strong></summary>

#### New files in `src/Encina.Compliance.LawfulBasis/`

1. **`Diagnostics/LawfulBasisDiagnostics.cs`** — OpenTelemetry instrumentation
   - ActivitySource: `Encina.Compliance.LawfulBasis`
   - Meter: `Encina.Compliance.LawfulBasis`
   - Counters:
     - `lawful_basis.registrations.total` (tags: `basis`, `outcome`)
     - `lawful_basis.changes.total` (tags: `old_basis`, `new_basis`)
     - `lawful_basis.revocations.total`
     - `lawful_basis.lia.created.total`
     - `lawful_basis.lia.approved.total`
     - `lawful_basis.lia.rejected.total`
     - `lawful_basis.validations.total` (tags: `basis`, `outcome`) — preserved from current
     - `lawful_basis.consent_checks.total` (tags: `outcome`) — preserved
     - `lawful_basis.lia_checks.total` (tags: `outcome`) — preserved

2. **`Diagnostics/LawfulBasisLogMessages.cs`** — Structured logging (EventId range **8350-8399**)
   - Pipeline logs (preserved logic, new EventIds):
     - 8350: `ValidationStarted`
     - 8351: `ValidationPassed`
     - 8352: `ValidationFailed`
     - 8353: `ValidationSkipped`
     - 8354: `BasisNotDeclared`
     - 8355: `AttributeConflictDetected`
     - 8356: `ConsentCheckStarted`
     - 8357: `ConsentCheckPassed`
     - 8358: `ConsentCheckFailed`
     - 8359: `LIACheckStarted`
     - 8360: `LIACheckResult`
     - 8361: `EnforcementWarning`
     - 8362: `ProviderNotRegistered`
   - Service-level logs (new):
     - 8370: `RegistrationCreated`
     - 8371: `BasisChanged`
     - 8372: `RegistrationRevoked`
     - 8373: `LIACreated`
     - 8374: `LIAApproved`
     - 8375: `LIARejected`
     - 8376: `LIAReviewScheduled`
     - 8377: `CacheHit`
     - 8378: `StoreError`
   - Auto-registration logs:
     - 8380: `AutoRegistrationStarted`
     - 8381: `AutoRegistrationCompleted`
     - 8382: `AutoRegistrationFailed`
   - Health check logs:
     - 8390: `HealthCheckCompleted`

3. **`Health/LawfulBasisHealthCheck.cs`** — Health check (moved + adapted)
   - Check `ILawfulBasisService` resolvability via scoped resolution
   - Report registration count and pending LIA reviews
   - `DefaultName` const, `Tags` static array

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of migrating Encina.Compliance.LawfulBasis to Marten event sourcing (Issue #779).

CONTEXT:
- Phases 1-5 completed: event-sourced model in place, old stores deleted
- Diagnostics are being CREATED in the new package (the old ones in GDPR were deleted in Phase 4)
- EventId range: 8350-8399 (new range for the extracted package — avoids collision with GDPR 8200-8259)

TASK:
1. Create Diagnostics/LawfulBasisDiagnostics.cs — ActivitySource + Meter + counters
2. Create Diagnostics/LawfulBasisLogMessages.cs — [LoggerMessage] with EventIds 8350-8399
3. Create Health/LawfulBasisHealthCheck.cs — check ILawfulBasisService

KEY RULES:
- EventId range 8350-8399 — no collisions with other modules
- Follow CrossBorderTransfer diagnostics patterns (Counter<long>, dimensional tags)
- Health check uses scoped resolution via IServiceProvider.CreateScope()
- [LoggerMessage] source generator for high-performance logging

REFERENCE FILES:
- src/Encina.Compliance.CrossBorderTransfer/Diagnostics/CrossBorderTransferDiagnostics.cs
- src/Encina.Compliance.CrossBorderTransfer/Diagnostics/CrossBorderTransferLogMessages.cs
- src/Encina.Compliance.CrossBorderTransfer/Health/CrossBorderTransferHealthCheck.cs
- src/Encina.Compliance.Consent/Health/ConsentHealthCheck.cs
```

</details>

---

### Phase 7: Testing

> **Goal**: Create tests for the new event-sourced model, delete old store tests.

<details>
<summary><strong>Tasks</strong></summary>

#### Unit Tests — `tests/Encina.UnitTests/Compliance/LawfulBasis/`

1. **NEW** `Aggregates/LawfulBasisAggregateTests.cs`
   - Test all aggregate commands and state transitions
   - Test invariant violations (change revoked, revoke already-revoked)
   - Test event generation (each command produces correct event)
   - Test Apply() reconstructs correct state from events
   - ~15-20 tests

2. **NEW** `Aggregates/LIAAggregateTests.cs`
   - Test Create, Approve, Reject, ScheduleReview
   - Test invariant violations (approve already-decided)
   - ~15-20 tests

3. **NEW** `Events/LawfulBasisEventsTests.cs`
   - Test all 7 event record types — equality, immutability
   - ~7-10 tests

4. **NEW** `ReadModels/LawfulBasisProjectionTests.cs`
   - Test projection handlers transform events to read model correctly
   - ~6-8 tests

5. **NEW** `ReadModels/LIAProjectionTests.cs`
   - Test LIA projection handlers
   - ~6-8 tests

6. **NEW** `Services/DefaultLawfulBasisServiceTests.cs`
   - Mock `IAggregateRepository<T>` and `IReadModelRepository<T>`
   - Test command methods delegate to aggregate correctly
   - Test query methods use read model repository
   - Test error handling (aggregate not found, save failure)
   - Test cache invalidation on writes
   - ~25-30 tests

7. **UPDATE** `LawfulBasisValidationPipelineBehaviorTests.cs`
   - Move from `tests/Encina.UnitTests/Compliance/GDPR/` to `tests/Encina.UnitTests/Compliance/LawfulBasis/`
   - Change mocked dependencies to `ILawfulBasisService`
   - Update namespace

8. **DELETE** old tests:
   - `tests/Encina.UnitTests/Compliance/GDPR/InMemoryLawfulBasisRegistryTests.cs`
   - `tests/Encina.UnitTests/Compliance/GDPR/LawfulBasisRegistrationTests.cs` (entity tests)
   - Any other LawfulBasis-specific tests in the GDPR folder

#### Guard Tests — `tests/Encina.GuardTests/Compliance/LawfulBasis/`

1. **NEW** `LawfulBasisAggregateGuardTests.cs` — null checks on factory/command parameters
2. **NEW** `LIAAggregateGuardTests.cs` — null checks on factory/command parameters
3. **NEW** `DefaultLawfulBasisServiceGuardTests.cs` — null checks on service method parameters
4. **MOVE/UPDATE** from `tests/Encina.GuardTests/Compliance/GDPR/LawfulBasisGuardTests.cs`

#### Property Tests — `tests/Encina.PropertyTests/Compliance/LawfulBasis/`

1. **NEW** `LawfulBasisAggregatePropertyTests.cs`
   - Invariant: Register then Revoke always produces IsRevoked=true
   - Invariant: Event stream replay always produces same state
   - Invariant: Version increments monotonically

2. **NEW** `LIAAggregatePropertyTests.cs`
   - Invariant: Create then Approve always produces Outcome=Approved

#### Contract Tests — `tests/Encina.ContractTests/Compliance/LawfulBasis/`

1. **NEW/MOVE** `ILawfulBasisServiceContractTests.cs`
   - Test `ILawfulBasisService` contract (method signatures, return types, ROP patterns)

#### Integration Tests

1. **DELETE** all 13-provider LawfulBasis integration tests:
   - `tests/Encina.IntegrationTests/ADO/*/LawfulBasis/` (8 files)
   - `tests/Encina.IntegrationTests/Dapper/*/LawfulBasis/` (8 files, estimated)
   - `tests/Encina.IntegrationTests/Infrastructure/EntityFrameworkCore/*/LawfulBasis/` (files)
   - `tests/Encina.IntegrationTests/Infrastructure/MongoDB/LawfulBasis/` (files)

2. **NEW** `tests/Encina.IntegrationTests/Compliance/LawfulBasis/LawfulBasisMartenIntegrationTests.cs`
   - Uses Marten PostgreSQL fixture
   - Tests full registration lifecycle via `ILawfulBasisService`
   - Tests full LIA lifecycle via `ILawfulBasisService`
   - Tests projection produces correct read models
   - Tests event stream audit trail
   - ~12-18 tests

#### Load/Benchmark Tests

1. **UPDATE** `tests/Encina.LoadTests/Compliance/GDPR/LawfulBasisValidationLoadTests.cs` — update dependencies, move to LawfulBasis folder
2. **UPDATE** `tests/Encina.BenchmarkTests/` — update dependencies if needed

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of migrating Encina.Compliance.LawfulBasis to Marten event sourcing (Issue #779).

CONTEXT:
- Phases 1-6 completed: full event-sourced LawfulBasis model, observability, old stores deleted
- Testing must cover: 2 aggregates, 7 events, 2 projections, 1 service, pipeline behavior
- 13-provider integration tests are removed — replaced by Marten integration test
- Guard, property, and contract tests need creating/updating

TASK:
1. Create unit tests: LawfulBasisAggregateTests, LIAAggregateTests, EventsTests, ProjectionTests, ServiceTests
2. Move and update pipeline behavior tests (GDPR → LawfulBasis, ILawfulBasisRegistry → ILawfulBasisService)
3. Delete old store/entity tests from GDPR test folders
4. Create guard tests for aggregates and service
5. Create property tests for aggregate invariants
6. Create contract tests for ILawfulBasisService
7. Delete all 13-provider integration tests
8. Create Marten integration test
9. Update load/benchmark tests

KEY RULES:
- Follow AAA pattern (Arrange, Act, Assert)
- Use descriptive test names: MethodName_Scenario_ExpectedResult
- Mock IAggregateRepository and IReadModelRepository in unit tests
- Integration tests use real Marten/PostgreSQL via xUnit [Collection] fixtures
- Guard tests: verify ArgumentNullException for all required parameters
- Property tests: use FsCheck for invariant verification

REFERENCE FILES:
- tests/Encina.UnitTests/Compliance/CrossBorderTransfer/ (reference test patterns)
- tests/Encina.UnitTests/Compliance/Consent/ (recent migration tests)
- tests/Encina.IntegrationTests/Compliance/ (reference Marten integration tests)
```

</details>

---

### Phase 8: Documentation & Finalization

> **Goal**: Update all documentation, public API tracking, and verify build.

<details>
<summary><strong>Tasks</strong></summary>

1. **`PublicAPI.Shipped.txt` + `PublicAPI.Unshipped.txt`** — Create for new package
   - Add: `ILawfulBasisService` and all methods
   - Add: `LawfulBasisAggregate`, `LIAAggregate` and public members
   - Add: `LawfulBasisReadModel`, `LIAReadModel` and properties
   - Add: All 7 event records
   - Add: `LawfulBasisMartenExtensions.AddLawfulBasisAggregates()`
   - Add: `ServiceCollectionExtensions.AddEncinaLawfulBasis()`

2. **`CHANGELOG.md`** — Add entry under Unreleased

   ```markdown
   ### Changed
   - **Encina.Compliance.LawfulBasis**: New package — extracted from `Encina.Compliance.GDPR` and migrated from entity-based persistence (13 database providers) to Marten event sourcing (PostgreSQL). Provides immutable audit trail for GDPR Art. 6 accountability. See ADR-019. (Fixes #779)
     - `ILawfulBasisRegistry`, `ILIAStore`, `ILegitimateInterestAssessment` → replaced by `ILawfulBasisService`
     - 13-provider store implementations removed (ADO.NET, Dapper, EF Core, MongoDB)
     - New: `LawfulBasisAggregate`, `LIAAggregate`, `LawfulBasisReadModel`, `LIAReadModel`
     - New dependencies: `Encina.DomainModeling`, `Encina.Marten`, `Encina.Caching`
     - Registration: `services.AddEncinaLawfulBasis()` + `services.AddLawfulBasisAggregates()` (Marten)
     - `LawfulBasis` enum and `LawfulBasisAttribute` remain in `Encina.Compliance.GDPR`
   ```

3. **`ROADMAP.md`** — Update if v0.13.0 milestone references LawfulBasis

4. **`src/Encina.Compliance.LawfulBasis/README.md`** — Create package README
   - Document event-sourced architecture
   - Registration examples (AddEncinaLawfulBasis + AddLawfulBasisAggregates)
   - Document PostgreSQL requirement (Marten)
   - API usage examples (ILawfulBasisService)

5. **`docs/INVENTORY.md`** — Update: remove deleted files, add new package files

6. **XML documentation** — Verify all new public APIs have `<summary>`, `<remarks>`, `<param>`, `<returns>`

7. **Update `Encina.Compliance.GDPR/PublicAPI.Unshipped.txt`** — Remove entries for deleted interfaces/types

8. **Build verification**:

   ```bash
   dotnet build Encina.slnx --configuration Release
   ```

   Target: 0 errors, 0 warnings

9. **Test verification**:

   ```bash
   dotnet test Encina.slnx --configuration Release
   ```

   Target: all tests pass

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 (final) of migrating Encina.Compliance.LawfulBasis to Marten event sourcing (Issue #779).

CONTEXT:
- Phases 1-7 completed: full migration done, tests updated
- This phase is documentation, public API tracking, and build verification
- CLAUDE.md mandates: "XML documentation on all public APIs", "PublicAPI.Unshipped.txt must track all public symbols"

TASK:
1. Create PublicAPI.Shipped.txt + PublicAPI.Unshipped.txt for new package
2. Update Encina.Compliance.GDPR PublicAPI.Unshipped.txt — remove deleted types
3. Add CHANGELOG.md entry under ### Changed in Unreleased section
4. Create README.md for the LawfulBasis package
5. Update docs/INVENTORY.md
6. Verify all new public APIs have XML documentation
7. Run dotnet build --configuration Release → verify 0 errors, 0 warnings
8. Run dotnet test → verify all tests pass

KEY RULES:
- PublicAPI format: Namespace.Type.Member(params) -> ReturnType
- Nullable annotations: string! (non-null), string? (nullable)
- CHANGELOG follows Keep a Changelog format
- No AI attribution in commits (per CLAUDE.md)
- README should show registration and testing strategy

REFERENCE FILES:
- src/Encina.Compliance.CrossBorderTransfer/PublicAPI.Unshipped.txt (reference)
- src/Encina.Compliance.CrossBorderTransfer/README.md (reference README pattern)
- CHANGELOG.md (current)
```

</details>

---

## Research

### Relevant Standards/Specifications

| Standard | Article/Section | Relevance |
|----------|----------------|-----------|
| GDPR Art. 5(2) | Accountability principle | Must prove compliance — event sourcing provides immutable evidence |
| GDPR Art. 6(1) | Lawfulness of processing | Six lawful bases — the core domain of this module |
| GDPR Art. 6(1)(a) | Consent basis | Validated via ConsentStatusProvider integration |
| GDPR Art. 6(1)(f) | Legitimate interests | Requires Legitimate Interest Assessment (LIA) — EDPB 3-part test |
| EDPB Guidelines | Three-Part Test | Purpose test, necessity test, balancing test — LIAAggregate structure |
| GDPR Art. 17 | Right to erasure | Crypto-shredding for event-sourced personal data |
| GDPR Art. 30 | Records of processing | Event stream serves as processing record |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `AggregateBase` | `src/Encina.DomainModeling/` | Base class for LawfulBasisAggregate and LIAAggregate |
| `IAggregateRepository<T>` | `src/Encina.Marten/` | Persistence for both aggregates |
| `IReadModel`, `IProjectionHandler` | `src/Encina.Marten/Projections/` | Read models and projections |
| `IReadModelRepository<T>` | `src/Encina.Marten/Projections/` | Query read models in DefaultLawfulBasisService |
| `AddAggregateRepository<T>()` | `src/Encina.Marten/ServiceCollectionExtensions.cs` | DI registration for aggregate repositories |
| `LawfulBasis` enum | `src/Encina.Compliance.GDPR/Model/` | Core GDPR type — stays in GDPR |
| `LawfulBasisAttribute` | `src/Encina.Compliance.GDPR/Attributes/` | Attribute — stays in GDPR |
| `LIAOutcome` enum | `src/Encina.Compliance.GDPR/LawfulBasis/` | Approved/Rejected/RequiresReview — stays in GDPR |
| `LawfulBasisOptions` | `src/Encina.Compliance.GDPR/` | Moved to new package |
| `LawfulBasisValidationPipelineBehavior` | `src/Encina.Compliance.GDPR/` | Moved and adapted |
| `DefaultLawfulBasisProvider` | `src/Encina.Compliance.GDPR/LawfulBasis/` | Moved and adapted |
| `ILawfulBasisSubjectIdExtractor` | `src/Encina.Compliance.GDPR/Abstractions/` | Stays in GDPR (used by consent checks) |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| Encina.Marten | 1-32 | Aggregate lifecycle |
| Encina.Security | 8000-8099 | RBAC, authorization |
| Encina.Compliance.GDPR | 8100-8199 | GDPR core |
| Encina.Compliance.Consent | 8200-8259 | Consent |
| Encina.Compliance.DataSubjectRights | 8300-8349 | DSR |
| **Encina.Compliance.LawfulBasis** | **8350-8399** | **LawfulBasis (new range)** |
| Encina.Marten.GDPR | 8400-8499 | Crypto-shredding, anonymization |
| Encina.Compliance.CrossBorderTransfer | 8500-8559 | Cross-border transfer |

### Estimated File Count

| Category | New | Modified | Deleted | Total Impact |
|----------|:---:|:--------:|:-------:|:------------:|
| Package (.csproj, solution) | 1 | 1 | 0 | 2 |
| Aggregates | 2 | 0 | 0 | 2 |
| Events | 2 | 0 | 0 | 2 |
| Read Models/Projections | 4 | 0 | 0 | 4 |
| Service Interface + Impl | 2 | 0 | 0 | 2 |
| Errors | 1 | 0 | 0 | 1 |
| Configuration/DI/Marten | 3 | 0 | 0 | 3 |
| Pipeline/Provider (moved) | 0 | 4 | 0 | 4 |
| Auto-Registration (moved) | 0 | 2 | 0 | 2 |
| Model (moved) | 0 | 2 | 0 | 2 |
| Diagnostics | 2 | 0 | 0 | 2 |
| Health | 1 | 0 | 0 | 1 |
| GDPR package cleanup | 0 | 2 | ~18 | 20 |
| Provider Stores (DELETE) | 0 | 0 | ~25 | 25 |
| Provider DI (MODIFY) | 0 | 10 | 0 | 10 |
| Unit Tests | 6 | 1 | ~3 | 10 |
| Guard Tests | 3 | 1 | 0 | 4 |
| Property Tests | 2 | 0 | 0 | 2 |
| Contract Tests | 1 | 0 | 0 | 1 |
| Integration Tests | 1 | 0 | ~16 | 17 |
| Documentation | 4 | 3 | 0 | 7 |
| **TOTAL** | **~35** | **~26** | **~62** | **~123** |

---

## Combined AI Agent Prompt

<details>
<summary><strong>Complete Implementation Prompt — All Phases</strong></summary>

```
You are migrating Encina.Compliance.LawfulBasis from entity-based persistence to Marten event sourcing (Issue #779).

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 compliance library using Railway Oriented Programming (Either<EncinaError, T>)
- Pre-1.0: breaking changes acceptable, no backward compatibility
- ADR-019 mandates migration of all stateful compliance modules to Marten event sourcing
- CrossBorderTransfer (#412) is the reference implementation — follow its patterns exactly
- Consent (#777) and DSR (#778) migrations are completed — follow their patterns
- AggregateBase from Encina.DomainModeling provides: Id, Version, RaiseEvent<T>, Apply(object)
- IAggregateRepository<T> from Encina.Marten provides: LoadAsync, SaveAsync, CreateAsync
- IReadModel, IProjectionHandler from Encina.Marten.Projections for query-side projections

IMPORTANT: LawfulBasis code is currently INSIDE Encina.Compliance.GDPR package.
This migration EXTRACTS it into a NEW Encina.Compliance.LawfulBasis package.
The LawfulBasis enum, LawfulBasisAttribute, LIAOutcome enum STAY in Encina.Compliance.GDPR.

IMPLEMENTATION OVERVIEW:

Phase 1 — Package, Events & Aggregates:
- Create new Encina.Compliance.LawfulBasis package (.csproj, add to solution)
- Create Events/LawfulBasisEvents.cs with 3 sealed record events implementing : INotification
  (LawfulBasisRegistered, LawfulBasisChanged, LawfulBasisRevoked)
- Create Events/LIAEvents.cs with 4 sealed record events implementing : INotification
  (LIACreated, LIAApproved, LIARejected, LIAReviewScheduled)
- Create Aggregates/LawfulBasisAggregate.cs extending AggregateBase
  (keyed by RequestTypeName — one registration per request type)
- Create Aggregates/LIAAggregate.cs extending AggregateBase
  (keyed by LIA reference — one LIA per assessment)

Phase 2 — Read Models & Projections:
- Create ReadModels/LawfulBasisReadModel.cs + LawfulBasisProjection.cs
- Create ReadModels/LIAReadModel.cs + LIAProjection.cs

Phase 3 — Service Interface & Implementation:
- Create Abstractions/ILawfulBasisService.cs (replaces ILawfulBasisRegistry + ILIAStore + ILegitimateInterestAssessment)
- Create Services/DefaultLawfulBasisService.cs (uses IAggregateRepository + IReadModelRepository + ICacheProvider)
- Create Errors/LawfulBasisErrors.cs
- No InMemory implementation

Phase 4 — Configuration, DI, Pipeline & Marten:
- MOVE from GDPR to LawfulBasis: LawfulBasisOptions, LawfulBasisOptionsValidator, LawfulBasisEnforcementMode,
  LawfulBasisValidationPipelineBehavior, DefaultLawfulBasisProvider, auto-registration
- ADAPT moved files: ILawfulBasisRegistry/ILIAStore → ILawfulBasisService
- Create ServiceCollectionExtensions.cs (AddEncinaLawfulBasis)
- Create LawfulBasisMartenExtensions.cs (AddLawfulBasisAggregates)
- DELETE from GDPR: old interfaces, InMemory implementations, entities, mappers

Phase 5 — Remove 13-Provider Stores:
- DELETE all LawfulBasis/ folders from ADO.NET (4), Dapper (4), EF Core, MongoDB
- DELETE SQL scripts for lawful basis tables
- REMOVE UseLawfulBasis blocks from 13 ServiceCollectionExtensions

Phase 6 — Observability:
- Create Diagnostics/LawfulBasisDiagnostics.cs (ActivitySource + Meter)
- Create Diagnostics/LawfulBasisLogMessages.cs (EventId range 8350-8399)
- Create Health/LawfulBasisHealthCheck.cs

Phase 7 — Testing:
- NEW: Aggregate tests (×2), event tests, projection tests (×2), service tests
- MOVE/UPDATE: Pipeline behavior tests from GDPR
- DELETE: Old InMemory store tests, old entity tests, 13-provider integration tests
- NEW: Marten integration test (real PostgreSQL in Docker)
- NEW: Guard, property, contract tests

Phase 8 — Documentation:
- PublicAPI.Shipped.txt + PublicAPI.Unshipped.txt (new package)
- Update GDPR PublicAPI.Unshipped.txt (remove deleted types)
- CHANGELOG.md, README.md, INVENTORY.md
- Build and test verification

KEY PATTERNS TO FOLLOW:
- Aggregate: see src/Encina.Compliance.CrossBorderTransfer/Aggregates/TIAAggregate.cs
- Events: see src/Encina.Compliance.CrossBorderTransfer/Events/TIAEvents.cs
- Service: see src/Encina.Compliance.CrossBorderTransfer/Services/DefaultTIAService.cs
- DI: see src/Encina.Compliance.CrossBorderTransfer/ServiceCollectionExtensions.cs
- Marten extensions: see src/Encina.Compliance.CrossBorderTransfer/CrossBorderTransferMartenExtensions.cs
- Tests: see tests/Encina.UnitTests/Compliance/CrossBorderTransfer/

KEY RULES:
- .NET 10, C# 14, nullable enabled
- ROP: Either<EncinaError, T> on all service methods
- UNIFIED EVENTS: ES events implement : INotification — single type for both persistence and mediator
- Events are sealed records, aggregates are sealed classes
- All public types need XML documentation
- EventId range: 8350-8399 (new allocation for extracted package)
- TenantId and ModuleId on events/aggregate for cross-cutting support
- NO InMemory implementation — DefaultLawfulBasisService via TryAddScoped
- No [Obsolete], no migration helpers, no backward compatibility
- LawfulBasis enum + LawfulBasisAttribute + LIAOutcome enum STAY in Encina.Compliance.GDPR
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | **Caching** | ✅ Include | `DefaultLawfulBasisService` caches read model queries via `ICacheProvider` (optional dependency). Cache invalidation on write operations. Pattern: `"lb:reg:{id}"`, `"lb:lia:ref:{reference}"` |
| 2 | **OpenTelemetry** | ✅ Include | New `LawfulBasisDiagnostics` with ActivitySource `Encina.Compliance.LawfulBasis`, Meter with counters for registrations, changes, revocations, LIA operations, and pipeline validations |
| 3 | **Structured Logging** | ✅ Include | New `LawfulBasisLogMessages.cs` with `[LoggerMessage]` source generator, EventId range 8350-8399 |
| 4 | **Health Checks** | ✅ Include | `LawfulBasisHealthCheck` verifies `ILawfulBasisService` resolvability, reports registration count and pending LIA reviews |
| 5 | **Validation** | ✅ Include | `LawfulBasisValidationPipelineBehavior` preserved and adapted — validates basis declarations, consent checks, LIA approval via `ILawfulBasisService` |
| 6 | **Resilience** | ❌ N/A | Marten handles PostgreSQL retries internally. No external system calls in lawful basis module |
| 7 | **Distributed Locks** | ❌ N/A | Marten uses optimistic concurrency (aggregate versioning). No shared mutable state requiring external locks |
| 8 | **Transactions** | ✅ Include | Inherent in Marten — events + projections committed in a single PostgreSQL transaction via `IDocumentSession.SaveChangesAsync()` |
| 9 | **Idempotency** | ✅ Include | Aggregate versioning prevents duplicate event application. Optimistic concurrency via aggregate `Version` |
| 10 | **Multi-Tenancy** | ✅ Include | `TenantId` field on both aggregates and all events. Marten supports native tenancy |
| 11 | **Module Isolation** | ✅ Include | `ModuleId` field on both aggregates and all events for modular monolith scoping |
| 12 | **Audit Trail** | ✅ Include | **Inherent** — the event stream IS the audit trail. GDPR Art. 5(2) accountability satisfied by immutable event history of all basis determinations and LIA assessments |

---

## Dependencies & Prerequisites

| Dependency | Status | Notes |
|------------|--------|-------|
| [#776 ADR-019: Compliance Event Sourcing Strategy](https://github.com/dlrivada/Encina/issues/776) | ✅ Completed | Architectural decision in place |
| [#412 CrossBorderTransfer (reference implementation)](https://github.com/dlrivada/Encina/issues/412) | ✅ Completed | Pattern established |
| [#777 Consent Migration](https://github.com/dlrivada/Encina/issues/777) | ✅ Completed | Same migration pattern |
| [#778 DSR Migration](https://github.com/dlrivada/Encina/issues/778) | ✅ Completed | Same migration pattern |
| `Encina.Marten` package | ✅ Production | `IAggregateRepository<T>`, `IReadModel`, `IProjectionHandler` |
| `Encina.DomainModeling` package | ✅ Production | `AggregateBase`, `IAggregate` |
| `Encina.Caching` package | ✅ Production | `ICacheProvider` |
| PostgreSQL infrastructure | ✅ Available | Docker Compose profile: `databases` |

No blocking prerequisites identified. All required infrastructure exists.
