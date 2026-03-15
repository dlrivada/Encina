# Implementation Plan: Migrate `Encina.Compliance.Consent` to Marten Event Sourcing

> **Issue**: [#777](https://github.com/dlrivada/Encina/issues/777)
> **Type**: Refactor
> **Complexity**: High (8 phases, ~35 files to create/modify, ~50+ files to remove)
> **Estimated Scope**: ~2,200-3,000 lines of new production code + ~1,800-2,500 lines of tests; ~8,000+ lines removed from 13-provider stores
> **Prerequisite**: [#776 ADR-019](https://github.com/dlrivada/Encina/issues/776) ✅ Completed, [#412 CrossBorderTransfer](https://github.com/dlrivada/Encina/issues/412) ✅ Completed (reference implementation)

---

## Summary

Migrate `Encina.Compliance.Consent` from entity-based persistence (13 database providers: ADO.NET ×4, Dapper ×4, EF Core ×4, MongoDB ×1) to **Marten event sourcing** (PostgreSQL only), as mandated by [ADR-019](../architecture/adr/019-compliance-event-sourcing-marten.md).

The refactoring transforms the current mutable `ConsentRecord` entity and `IConsentStore`/`IConsentAuditStore`/`IConsentVersionManager` interfaces (with 39+ provider-specific implementations) into:

- **1 event-sourced aggregate** (`ConsentAggregate`) with immutable domain events
- **1 read model** (`ConsentReadModel`) with a Marten projection
- **1 service interface** (`IConsentService`) replacing the three store interfaces
- **Audit trail inherent in the event stream** — no separate `IConsentAuditStore` needed

The `ConsentRequiredPipelineBehavior`, `ConsentOptions`, `ConsentDiagnostics`, `ConsentHealthCheck`, and `[RequireConsent]` attribute are **preserved** (they are not store-dependent). The old in-memory implementations (`InMemoryConsentStore`, `InMemoryConsentAuditStore`, `InMemoryConsentVersionManager`) are **deleted** — unit tests use NSubstitute mocks of `IAggregateRepository<ConsentAggregate>`, and integration tests use real Marten with PostgreSQL in Docker.

**Provider category**: Event Sourcing (Marten/PostgreSQL — specialized provider, not the 13-database category).

**New dependencies for `Encina.Compliance.Consent`**:

- `Encina.DomainModeling` — `AggregateBase`, `IAggregate` (DDD building blocks for event-sourced aggregates)
- `Encina.Marten` — `IAggregateRepository<T>`, `IReadModel`, `IProjectionHandler` (event store infrastructure)
- `Encina.Caching` — `ICacheProvider` (optional caching for read model queries)

> **Note on event types**: Event store events (e.g., `ConsentGranted`) are **sealed records implementing `INotification`** — they do NOT extend `DomainEvent`/`IDomainEvent` from DomainModeling. Marten manages event metadata (timestamps, sequence, correlation). By implementing `INotification`, these events are **automatically published** by `EventPublishingPipelineBehavior` after successful command execution, eliminating the need for a separate notification layer. The existing mediator domain events (`ConsentGrantedEvent`, `ConsentWithdrawnEvent`, etc.) from `Events/` can be **removed** — the event-sourced events replace them as the single source of truth for both state changes and notifications.

**Packages affected**:

- `Encina.Compliance.Consent` — restructured (aggregates, events, projections, service)
- `Encina.ADO.Sqlite`, `Encina.ADO.SqlServer`, `Encina.ADO.PostgreSQL`, `Encina.ADO.MySQL` — Consent stores **removed**
- `Encina.Dapper.Sqlite`, `Encina.Dapper.SqlServer`, `Encina.Dapper.PostgreSQL`, `Encina.Dapper.MySQL` — Consent stores **removed**
- `Encina.EntityFrameworkCore` — Consent stores, entities, configurations **removed**
- `Encina.MongoDB` — Consent stores, documents **removed**

---

## Design Choices

<details>
<summary><strong>1. Aggregate Design — Single ConsentAggregate per (SubjectId, Purpose)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Single aggregate per (SubjectId, Purpose)** | Natural aggregate boundary, one stream per consent decision, simple lifecycle | Many small streams (one per subject-purpose pair) |
| **B) Single aggregate per SubjectId (all purposes)** | Fewer streams, bulk operations natural | Large aggregates, contention on concurrent purpose changes, violates single-responsibility |
| **C) Multiple aggregates (Consent + Version + Audit)** | Mirrors current 3-interface design | Over-engineered — version management and audit are naturally part of consent lifecycle in ES |

### Chosen Option: **A — Single ConsentAggregate per (SubjectId, Purpose)**

### Rationale

- Matches GDPR's granularity: consent is per-purpose (Art. 6(1)(a))
- Aggregate ID = deterministic from `(SubjectId, Purpose)` — enables natural lookup
- Small, focused streams with clear lifecycle: Grant → (Renew/VersionChange)* → Withdraw/Expire
- Version management becomes events on the same stream (no separate aggregate)
- Follows CrossBorderTransfer pattern where each aggregate has a clear, bounded lifecycle
- Marten handles many small streams efficiently via PostgreSQL JSONB

</details>

<details>
<summary><strong>2. Interface Transformation — Replace 3 interfaces with 1 IConsentService</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Single `IConsentService` combining all operations** | Clean API, matches ES model (commands + queries on same service), simpler DI | Larger interface |
| **B) Keep 3 interfaces, adapt to ES** | Minimal API surface change | Artificial separation — audit is inherent in ES, version management is part of aggregate |
| **C) CQRS split (IConsentCommandService + IConsentQueryService)** | Clean CQRS separation | Over-engineering for a compliance module that isn't a high-throughput system |

### Chosen Option: **A — Single `IConsentService`**

### Rationale

- In event sourcing, the audit trail IS the event stream — a separate `IConsentAuditStore` adds no value
- Version management is a command on the aggregate, not a separate concern
- Follows CrossBorderTransfer pattern: `ITIAService`, `ISCCService`, `IApprovedTransferService` — each is a single service
- Read operations use the `ConsentReadModel` projection — no need for a separate query interface
- Pre-1.0: breaking changes are expected and encouraged

</details>

<details>
<summary><strong>3. Event Model — 6 fine-grained events, unified (ES + mediator)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Fine-grained events (6 types), unified ES + mediator** | One type = one fact (DRY), auto-published via `EventPublishingPipelineBehavior`, less code | ES events depend on `INotification` (marker interface — zero cost) |
| **B) Fine-grained events, separate ES + mediator types** | ES events decoupled from mediator | Two types per fact, manual mapping, more maintenance, ×9 modules |
| **C) Generic `ConsentStateChanged` event** | One event type, simpler projections | Loses semantic meaning, harder to query specific transitions |
| **D) CRUD-style events (Created, Updated, Deleted)** | Familiar pattern | Doesn't capture domain intent (why was consent changed?) |

### Chosen Option: **A — Fine-grained unified events implementing `INotification`**

### Events

| Event | Trigger | GDPR Article | Replaces |
|-------|---------|:------------:|----------|
| `ConsentGranted` | Data subject gives consent | Art. 6(1)(a), 7(1) | `ConsentGrantedEvent` |
| `ConsentWithdrawn` | Data subject withdraws consent | Art. 7(3) | `ConsentWithdrawnEvent` |
| `ConsentExpired` | Consent passes expiration date | Art. 7 (implied) | `ConsentExpiredEvent` |
| `ConsentRenewed` | Data subject re-confirms consent | Art. 7(1) | — (new) |
| `ConsentVersionChanged` | Terms updated, reconsent may be required | Art. 7 | `ConsentVersionChangedEvent` |
| `ConsentReconsentProvided` | Data subject consents under new terms | Art. 7(1) | — (new) |

### Rationale

- **DDD principle**: In event sourcing, aggregate events ARE domain events. The separation into "event store events" and "mediator events" is an implementation artifact, not a domain concept.
- **Infrastructure already supports it**: `EventPublishingPipelineBehavior` (line 66) filters with `.OfType<INotification>()` — events implementing `INotification` are automatically published to the mediator after commit. No manual mapping needed.
- **`INotification` is a marker interface** with zero methods — implementing it adds no coupling or complexity.
- **Eliminates duplication**: Without unification, each of the 9 compliance modules migrating to ES would have 2 sets of events. With unification, half the types.
- **CrossBorderTransfer already updated**: TIA/SCC/Transfer events now implement `: INotification` (established pattern).
- **Existing mediator events (`ConsentGrantedEvent`, etc.) are deleted** — the ES events replace them as the single source of truth for both state changes and notifications.

</details>

<details>
<summary><strong>4. Pipeline Behavior — Adapt ConsentRequiredPipelineBehavior to use IConsentService</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Modify existing behavior to use `IConsentService.HasValidConsentAsync`** | Minimal change, same external behavior | Needs careful wiring |
| **B) Rewrite pipeline behavior from scratch** | Clean slate | Unnecessary — the behavior logic is store-agnostic |
| **C) Remove pipeline behavior, use middleware** | ASP.NET standard | Loses MediatR integration, different pattern from other modules |

### Chosen Option: **A — Adapt existing behavior**

### Rationale

- `ConsentRequiredPipelineBehavior` already works correctly — it just needs its dependency changed from `IConsentStore` to `IConsentService`
- The attribute caching, enforcement modes, OpenTelemetry integration, and logging are all preserved
- Single-line change in the constructor dependency
- All existing pipeline behavior tests remain valid with minimal adaptation

</details>

<details>
<summary><strong>5. Testing Strategy — Mocks for unit tests, real Marten for integration tests (no InMemory implementation)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) In-memory service with internal event list** | No infrastructure for unit tests | Production code nobody deploys; mocks are more flexible; adds maintenance burden ×9 modules |
| **B) Mock `IAggregateRepository<T>` + real Marten in Docker for integration** | No unnecessary production code; mocks are precise per-test; integration tests validate real behavior | Requires Docker for integration tests (already available) |
| **C) In-memory Marten session mock** | Realistic | Complex, fragile, mocking an event store is an anti-pattern |

### Chosen Option: **B — Mock-based unit tests + real Marten integration tests**

### Rationale

- **CrossBorderTransfer (reference implementation) has NO InMemory service** — unit tests mock `IAggregateRepository<T>` with NSubstitute; no `InMemoryTIAService` exists
- **Mocks are superior for unit tests**: more flexible, per-test behavior control, no shared state, <1ms execution
- **InMemory service adds zero production value**: nobody deploys GDPR compliance without real persistence
- **Integration tests use real Marten + PostgreSQL in Docker**: exactly like the 13 DB providers use Docker — coherent testing strategy
- **Aggregates can be tested directly** (they're plain objects — no persistence needed)
- **Eliminates maintenance burden**: no `InMemoryConsentService` ×9 compliance modules to maintain

</details>

<details>
<summary><strong>6. Store Removal Strategy — Delete all 13-provider implementations</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Delete immediately, clean break** | No dead code, clear migration signal | Breaking change |
| **B) Keep as deprecated** | Backward compatibility | Pre-1.0: no backward compatibility required; dead code is prohibited |
| **C) Move to `.backup/` folder** | Recoverable | Unnecessary — git history preserves everything |

### Chosen Option: **A — Delete immediately**

### Rationale

- Pre-1.0 policy: "No `[Obsolete]`, no backward compatibility, no migration helpers"
- ADR-019: "Store interfaces with 13-provider implementations are replaced, not deprecated"
- Git history preserves all deleted code for reference
- Removes ~8,000+ lines of provider-specific SQL and mapping code

</details>

---

## Implementation Phases

### Phase 1: Domain Events & Aggregate

> **Goal**: Create the event-sourced aggregate with domain events that capture the full consent lifecycle.

<details>
<summary><strong>Tasks</strong></summary>

#### New files in `src/Encina.Compliance.Consent/`

1. **`Events/ConsentEvents.cs`** — All 6 immutable event records implementing `: INotification`
   - `ConsentGranted(...) : INotification` — Replaces `ConsentGrantedEvent`
   - `ConsentWithdrawn(...) : INotification` — Replaces `ConsentWithdrawnEvent`
   - `ConsentExpired(...) : INotification` — Replaces `ConsentExpiredEvent`
   - `ConsentRenewed(...) : INotification` — New (no prior mediator event)
   - `ConsentVersionChanged(...) : INotification` — Replaces `ConsentVersionChangedEvent`
   - `ConsentReconsentProvided(...) : INotification` — New (no prior mediator event)
   - All events are **unified**: persisted by Marten AND auto-published by `EventPublishingPipelineBehavior`

#### Files to DELETE from `src/Encina.Compliance.Consent/Events/`

1. **DELETE** `Events/ConsentGrantedEvent.cs` — Replaced by `ConsentGranted`
2. **DELETE** `Events/ConsentWithdrawnEvent.cs` — Replaced by `ConsentWithdrawn`
3. **DELETE** `Events/ConsentExpiredEvent.cs` — Replaced by `ConsentExpired`
4. **DELETE** `Events/ConsentVersionChangedEvent.cs` — Replaced by `ConsentVersionChanged`

#### New files in `src/Encina.Compliance.Consent/`

1. **`Aggregates/ConsentAggregate.cs`** — Event-sourced aggregate
   - Extends `AggregateBase` (from `Encina.DomainModeling`)
   - Properties: `DataSubjectId`, `Purpose`, `Status` (ConsentStatus), `ConsentVersionId`, `Source`, `IpAddress`, `ProofOfConsent`, `Metadata`, `GivenAtUtc`, `WithdrawnAtUtc`, `ExpiresAtUtc`, `TenantId`, `ModuleId`
   - Factory: `static ConsentAggregate Grant(Guid id, string dataSubjectId, string purpose, string consentVersionId, string source, string? ipAddress, string? proofOfConsent, IReadOnlyDictionary<string, object?> metadata, DateTimeOffset? expiresAtUtc, string grantedBy, string? tenantId, string? moduleId)`
   - Commands: `Withdraw(string withdrawnBy, string? reason)`, `Expire()`, `Renew(string consentVersionId, DateTimeOffset? newExpiresAtUtc, string renewedBy, string? source)`, `ChangeVersion(string newVersionId, string description, bool requiresReconsent, string changedBy)`, `ProvideReconsent(string newConsentVersionId, string source, string? ipAddress, string? proofOfConsent, IReadOnlyDictionary<string, object?> metadata, DateTimeOffset? expiresAtUtc, string grantedBy)`
   - `Apply(object domainEvent)` — switch expression for all 6 event types
   - Invariants: cannot withdraw non-active consent, cannot expire already-expired consent, etc.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of migrating Encina.Compliance.Consent to Marten event sourcing (Issue #777).

CONTEXT:
- Encina is a .NET 10 / C# 14 library using Railway Oriented Programming (Either<EncinaError, T>)
- This is a REFACTOR of an existing module: src/Encina.Compliance.Consent/
- ADR-019 mandates migration from entity-based to event-sourced persistence
- Reference implementation: src/Encina.Compliance.CrossBorderTransfer/Aggregates/ and Events/
- AggregateBase is from Encina.DomainModeling (Id: Guid, Version: int, RaiseEvent<T>, Apply(object))
- Events are sealed records with DateTimeOffset timestamps
- ConsentStatus enum already exists: Active, Withdrawn, Expired, RequiresReconsent

TASK:
1. Create Events/ConsentEvents.cs with 6 sealed record event types implementing `: INotification`
   (ConsentGranted, ConsentWithdrawn, ConsentExpired, ConsentRenewed, ConsentVersionChanged, ConsentReconsentProvided)
2. DELETE old mediator events: Events/ConsentGrantedEvent.cs, ConsentWithdrawnEvent.cs, ConsentExpiredEvent.cs, ConsentVersionChangedEvent.cs
3. Create Aggregates/ConsentAggregate.cs extending AggregateBase with full lifecycle support
4. Each event must include TenantId/ModuleId where applicable (for cross-cutting integration)
5. Aggregate MUST validate invariants (e.g., cannot withdraw non-active consent)

KEY RULES:
- .NET 10 / C# 14, nullable enabled
- All types are sealed records (events) or sealed classes (aggregate)
- All public types need XML documentation with <summary>, <remarks>, GDPR article references
- Events implement INotification (marker interface from Encina core) — this makes them
  automatically publishable by EventPublishingPipelineBehavior after Marten commit
- Events are UNIFIED: same type persisted by Marten AND published to mediator subscribers
- Events are immutable facts — no methods, only data
- Aggregate state changes ONLY through RaiseEvent → Apply pattern
- ConsentGranted must capture all proof data (Source, IpAddress, ProofOfConsent, Metadata) for GDPR Art. 7(1)
- Follow exact patterns from src/Encina.Compliance.CrossBorderTransfer/Events/TIAEvents.cs
  (which also implement : INotification)

REFERENCE FILES:
- src/Encina.Compliance.CrossBorderTransfer/Aggregates/TIAAggregate.cs
- src/Encina.Compliance.CrossBorderTransfer/Events/TIAEvents.cs (events with : INotification)
- src/Encina.Compliance.Consent/Model/ConsentRecord.cs (existing entity — source for properties)
- src/Encina.Compliance.Consent/Model/ConsentStatus.cs (existing enum — reuse)
- src/Encina.Compliance.Consent/Model/ConsentVersion.cs (existing — version management events)
- src/Encina.Compliance.Consent/Events/ConsentGrantedEvent.cs (being DELETED — reference for field parity)
- src/Encina.DomainModeling/AggregateBase.cs (base class)
- src/Encina.Marten/EventPublishingPipelineBehavior.cs (auto-publishes INotification events)
```

</details>

---

### Phase 2: Read Model & Projection

> **Goal**: Create the query-optimized read model and Marten projection for efficient consent queries.

<details>
<summary><strong>Tasks</strong></summary>

#### New files in `src/Encina.Compliance.Consent/`

1. **`ReadModels/ConsentReadModel.cs`** — Query-optimized view
   - Implements `IReadModel` (from `Encina.Marten.Projections`)
   - Properties mirror `ConsentRecord` but with mutable setters for projection updates:
     `Id (Guid)`, `DataSubjectId`, `Purpose`, `Status (ConsentStatus)`, `ConsentVersionId`, `GivenAtUtc`, `WithdrawnAtUtc?`, `ExpiresAtUtc?`, `Source`, `IpAddress?`, `ProofOfConsent?`, `Metadata`, `TenantId?`, `ModuleId?`, `LastModifiedAtUtc`, `Version (int)`

2. **`ReadModels/ConsentProjection.cs`** — Event → ReadModel transformation
   - Implements `IProjectionHandler<ConsentGranted, ConsentReadModel>` (creates)
   - Implements `IProjectionHandler<ConsentWithdrawn, ConsentReadModel>` (updates Status)
   - Implements `IProjectionHandler<ConsentExpired, ConsentReadModel>` (updates Status)
   - Implements `IProjectionHandler<ConsentRenewed, ConsentReadModel>` (updates version, expiry)
   - Implements `IProjectionHandler<ConsentVersionChanged, ConsentReadModel>` (updates version, may set RequiresReconsent)
   - Implements `IProjectionHandler<ConsentReconsentProvided, ConsentReadModel>` (reactivates consent)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of migrating Encina.Compliance.Consent to Marten event sourcing (Issue #777).

CONTEXT:
- Phase 1 completed: ConsentAggregate and 6 event types exist
- Read models are query-optimized views built from event streams
- IReadModel from Encina.Marten.Projections is the marker interface
- IProjectionHandler<TEvent, TReadModel> processes individual events into read model updates
- ConsentStatus enum: Active, Withdrawn, Expired, RequiresReconsent

TASK:
1. Create ReadModels/ConsentReadModel.cs implementing IReadModel
2. Create ReadModels/ConsentProjection.cs implementing IProjectionHandler for each event type
3. The read model replaces the old ConsentRecord for query purposes

KEY RULES:
- ReadModel has mutable setters (projections update properties incrementally)
- ReadModel must include LastModifiedAtUtc (updated on every event)
- ConsentGranted creates the read model; all other events update it
- ConsentVersionChanged with RequiresReconsent=true → Status = RequiresReconsent
- ConsentReconsentProvided → Status = Active (re-activates)
- Follow patterns from src/Encina.Compliance.CrossBorderTransfer/ReadModels/

REFERENCE FILES:
- src/Encina.Compliance.CrossBorderTransfer/ReadModels/TIAReadModel.cs
- src/Encina.Marten/Projections/IReadModel.cs
- src/Encina.Marten/Projections/IProjectionHandler.cs
- src/Encina.Compliance.Consent/Events/ConsentEvents.cs (Phase 1 output)
```

</details>

---

### Phase 3: Service Interface & Implementation

> **Goal**: Define `IConsentService` replacing the 3 old interfaces, and implement `DefaultConsentService` using `IAggregateRepository<ConsentAggregate>`. No in-memory implementation — unit tests mock the repository; integration tests use real Marten.

<details>
<summary><strong>Tasks</strong></summary>

#### Modified/new files in `src/Encina.Compliance.Consent/`

1. **`Abstractions/IConsentService.cs`** — New unified service interface
   - Command methods (return `ValueTask<Either<EncinaError, T>>`):
     - `GrantConsentAsync(Guid id, string dataSubjectId, string purpose, string consentVersionId, string source, string? ipAddress, string? proofOfConsent, IReadOnlyDictionary<string, object?>? metadata, DateTimeOffset? expiresAtUtc, string grantedBy, string? tenantId, string? moduleId, CancellationToken)` → `Either<EncinaError, Guid>`
     - `WithdrawConsentAsync(Guid consentId, string withdrawnBy, string? reason, CancellationToken)` → `Either<EncinaError, Unit>`
     - `RenewConsentAsync(Guid consentId, string consentVersionId, DateTimeOffset? newExpiresAtUtc, string renewedBy, string? source, CancellationToken)` → `Either<EncinaError, Unit>`
     - `ProvideReconsentAsync(Guid consentId, string newConsentVersionId, string source, string? ipAddress, string? proofOfConsent, IReadOnlyDictionary<string, object?>? metadata, DateTimeOffset? expiresAtUtc, string grantedBy, CancellationToken)` → `Either<EncinaError, Unit>`
   - Query methods:
     - `GetConsentAsync(Guid consentId, CancellationToken)` → `Either<EncinaError, ConsentReadModel>`
     - `GetConsentBySubjectAndPurposeAsync(string subjectId, string purpose, CancellationToken)` → `Either<EncinaError, Option<ConsentReadModel>>`
     - `GetAllConsentsAsync(string subjectId, CancellationToken)` → `Either<EncinaError, IReadOnlyList<ConsentReadModel>>`
     - `HasValidConsentAsync(string subjectId, string purpose, CancellationToken)` → `Either<EncinaError, bool>`
     - `GetConsentHistoryAsync(Guid consentId, CancellationToken)` → `Either<EncinaError, IReadOnlyList<object>>` (raw event stream for audit)

2. **`Services/DefaultConsentService.cs`** — Implementation using Marten
   - Constructor: `IAggregateRepository<ConsentAggregate>`, `IReadModelRepository<ConsentReadModel>`, `ICacheProvider` (optional), `TimeProvider`, `ILogger<DefaultConsentService>`
   - Command methods: load aggregate → execute command → save via `_repository.SaveAsync()`
   - Query methods: query `IReadModelRepository<ConsentReadModel>` projections
   - `HasValidConsentAsync`: queries read model, checks Status == Active and not expired
   - Cache invalidation on write operations (cache key pattern: `"consent:{id}"`, `"consent:subject:{subjectId}:purpose:{purpose}"`)

#### Files to DELETE from `src/Encina.Compliance.Consent/`

1. **DELETE** `Abstractions/IConsentStore.cs` — Replaced by `IConsentService`
2. **DELETE** `Abstractions/IConsentAuditStore.cs` — Inherent in event stream
3. **DELETE** `Abstractions/IConsentVersionManager.cs` — Part of `IConsentService` (version changes are aggregate commands)
4. **DELETE** `InMemoryConsentStore.cs` — No longer needed (unit tests mock `IAggregateRepository<T>`)
5. **DELETE** `InMemoryConsentAuditStore.cs` — Audit is inherent in event stream
6. **DELETE** `InMemoryConsentVersionManager.cs` — No longer needed

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of migrating Encina.Compliance.Consent to Marten event sourcing (Issue #777).

CONTEXT:
- Phases 1-2 completed: ConsentAggregate, 6 event types, ConsentReadModel, ConsentProjection exist
- This phase replaces IConsentStore + IConsentAuditStore + IConsentVersionManager with a single IConsentService
- DefaultConsentService uses IAggregateRepository<ConsentAggregate> for writes and IReadModelRepository<ConsentReadModel> for queries
- No InMemory implementation — unit tests mock IAggregateRepository<T>; integration tests use real Marten
- Railway Oriented Programming: all methods return Either<EncinaError, T>

TASK:
1. Create Abstractions/IConsentService.cs with command + query methods
2. Create Services/DefaultConsentService.cs using IAggregateRepository + IReadModelRepository
3. DELETE old interfaces: IConsentStore.cs, IConsentAuditStore.cs, IConsentVersionManager.cs
4. DELETE old in-memory implementations: InMemoryConsentStore.cs, InMemoryConsentAuditStore.cs, InMemoryConsentVersionManager.cs

KEY RULES:
- All methods return ValueTask<Either<EncinaError, T>> (ROP)
- DefaultConsentService: load aggregate → command → SaveAsync pattern
- GetConsentHistoryAsync returns raw event stream (audit trail)
- HasValidConsentAsync: query read model, check Status == Active AND (ExpiresAtUtc is null OR > now)
- Use TryAddScoped in DI — DefaultConsentService is the only implementation
- Follow patterns from src/Encina.Compliance.CrossBorderTransfer/Services/DefaultTIAService.cs

REFERENCE FILES:
- src/Encina.Compliance.CrossBorderTransfer/Services/DefaultTIAService.cs
- src/Encina.Compliance.CrossBorderTransfer/Abstractions/ITIAService.cs
- src/Encina.Marten/IAggregateRepository.cs
- src/Encina.Marten/Projections/IReadModelRepository.cs
- src/Encina.Compliance.Consent/Abstractions/IConsentStore.cs (being replaced — read for API parity)
```

</details>

---

### Phase 4: Configuration, DI & Marten Registration

> **Goal**: Update `ServiceCollectionExtensions` and create Marten aggregate registration extensions.

<details>
<summary><strong>Tasks</strong></summary>

#### Modified files in `src/Encina.Compliance.Consent/`

1. **`Encina.Compliance.Consent.csproj`** — Add new project dependencies
   - Add `ProjectReference` to `Encina.DomainModeling` (for `AggregateBase`, `IAggregate`)
   - Add `ProjectReference` to `Encina.Marten` (for `IAggregateRepository<T>`, `IReadModel`, `IProjectionHandler`)
   - Add `ProjectReference` to `Encina.Caching` (for `ICacheProvider` in `DefaultConsentService`)
   - Follows the exact dependency pattern from `Encina.Compliance.CrossBorderTransfer.csproj`

2. **`ServiceCollectionExtensions.cs`** — Update DI registration
   - Replace `IConsentStore → InMemoryConsentStore` with `IConsentService → DefaultConsentService` (TryAddScoped — user can override)
   - Remove `IConsentAuditStore` and `IConsentVersionManager` registrations
   - Remove old `InMemoryConsentStore`, `InMemoryConsentAuditStore`, `InMemoryConsentVersionManager` registrations
   - Keep: `ConsentOptions`, `IConsentValidator`, `ConsentRequiredPipelineBehavior<,>`, health check, auto-registration
   - Update `ConsentRequiredPipelineBehavior` dependency from `IConsentStore` to `IConsentService`

3. **`ConsentMartenExtensions.cs`** — New Marten aggregate registration
   - `AddConsentAggregates(this IServiceCollection services)` — registers `IAggregateRepository<ConsentAggregate>`
   - Follows `CrossBorderTransferMartenExtensions` pattern exactly
   - Separate from core registration so Marten is opt-in

4. **`ConsentOptions.cs`** — Minor update
   - Remove `UseConsent` (was for provider ServiceCollectionExtensions) — now always-on when `AddEncinaConsent()` is called
   - Keep all other options: `EnforcementMode`, `DefaultExpirationDays`, `TrackConsentProof`, etc.

5. **`ConsentRequiredPipelineBehavior.cs`** — Update dependency
   - Change constructor parameter from `IConsentStore` to `IConsentService`
   - Change `HasValidConsentAsync` call — same method name, different interface

6. **`ConsentOptionsValidator.cs`** — Review and adapt if needed
   - ConsentOptionsValidator may reference removed interfaces — update

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of migrating Encina.Compliance.Consent to Marten event sourcing (Issue #777).

CONTEXT:
- Phases 1-3 completed: ConsentAggregate, events, read model, IConsentService, DefaultConsentService
- Old interfaces (IConsentStore, IConsentAuditStore, IConsentVersionManager) have been deleted
- Old InMemory implementations deleted — no InMemoryConsentService
- ConsentRequiredPipelineBehavior currently depends on IConsentStore — must be updated to IConsentService

TASK:
1. Update Encina.Compliance.Consent.csproj — add ProjectReferences to Encina.DomainModeling, Encina.Marten, Encina.Caching
2. Update ServiceCollectionExtensions.cs — replace old store registrations with IConsentService → DefaultConsentService
3. Create ConsentMartenExtensions.cs — AddConsentAggregates() for Marten
4. Update ConsentRequiredPipelineBehavior.cs — change IConsentStore dependency to IConsentService
5. Update ConsentOptionsValidator.cs if it references deleted interfaces
6. Update DefaultConsentValidator.cs if it references deleted interfaces

KEY RULES:
- AddEncinaConsent() registers DefaultConsentService (TryAddScoped — user can override)
- AddConsentAggregates() registers Marten IAggregateRepository<ConsentAggregate> — call separately
- ConsentRequiredPipelineBehavior: only change is dependency injection — behavior logic stays the same
- Follow exactly the DI pattern from src/Encina.Compliance.CrossBorderTransfer/ServiceCollectionExtensions.cs
- Follow exactly the Marten registration from src/Encina.Compliance.CrossBorderTransfer/CrossBorderTransferMartenExtensions.cs

REFERENCE FILES:
- src/Encina.Compliance.CrossBorderTransfer/ServiceCollectionExtensions.cs
- src/Encina.Compliance.CrossBorderTransfer/CrossBorderTransferMartenExtensions.cs
- src/Encina.Compliance.Consent/ServiceCollectionExtensions.cs (current — being modified)
- src/Encina.Compliance.Consent/ConsentRequiredPipelineBehavior.cs (current — being modified)
```

</details>

---

### Phase 5: Remove 13-Provider Store Implementations

> **Goal**: Delete all provider-specific consent store implementations and their DI registrations.

<details>
<summary><strong>Tasks</strong></summary>

#### DELETE from ADO.NET providers (12 files + 12 SQL scripts)

1. `src/Encina.ADO.Sqlite/Consent/ConsentStoreADO.cs`
2. `src/Encina.ADO.Sqlite/Consent/ConsentAuditStoreADO.cs`
3. `src/Encina.ADO.Sqlite/Consent/ConsentVersionManagerADO.cs`
4. `src/Encina.ADO.SqlServer/Consent/ConsentStoreADO.cs`
5. `src/Encina.ADO.SqlServer/Consent/ConsentAuditStoreADO.cs`
6. `src/Encina.ADO.SqlServer/Consent/ConsentVersionManagerADO.cs`
7. `src/Encina.ADO.PostgreSQL/Consent/ConsentStoreADO.cs`
8. `src/Encina.ADO.PostgreSQL/Consent/ConsentAuditStoreADO.cs`
9. `src/Encina.ADO.PostgreSQL/Consent/ConsentVersionManagerADO.cs`
10. `src/Encina.ADO.MySQL/Consent/ConsentStoreADO.cs`
11. `src/Encina.ADO.MySQL/Consent/ConsentAuditStoreADO.cs`
12. `src/Encina.ADO.MySQL/Consent/ConsentVersionManagerADO.cs`
13. SQL scripts: `src/Encina.ADO.*/Scripts/006_CreateConsentRecordsTable.sql`
14. SQL scripts: `src/Encina.ADO.*/Scripts/007_CreateConsentAuditEntriesTable.sql`
15. SQL scripts: `src/Encina.ADO.*/Scripts/008_CreateConsentVersionsTable.sql`

#### DELETE from Dapper providers (12 files)

1. `src/Encina.Dapper.Sqlite/Consent/ConsentStoreDapper.cs`
2. `src/Encina.Dapper.Sqlite/Consent/ConsentAuditStoreDapper.cs`
3. `src/Encina.Dapper.Sqlite/Consent/ConsentVersionManagerDapper.cs`
4. `src/Encina.Dapper.SqlServer/Consent/ConsentStoreDapper.cs`
5. `src/Encina.Dapper.SqlServer/Consent/ConsentAuditStoreDapper.cs`
6. `src/Encina.Dapper.SqlServer/Consent/ConsentVersionManagerDapper.cs`
7. `src/Encina.Dapper.PostgreSQL/Consent/ConsentStoreDapper.cs`
8. `src/Encina.Dapper.PostgreSQL/Consent/ConsentAuditStoreDapper.cs`
9. `src/Encina.Dapper.PostgreSQL/Consent/ConsentVersionManagerDapper.cs`
10. `src/Encina.Dapper.MySQL/Consent/ConsentStoreDapper.cs`
11. `src/Encina.Dapper.MySQL/Consent/ConsentAuditStoreDapper.cs`
12. `src/Encina.Dapper.MySQL/Consent/ConsentVersionManagerDapper.cs`

#### DELETE from EF Core (9 files)

1. `src/Encina.EntityFrameworkCore/Consent/ConsentStoreEF.cs`
2. `src/Encina.EntityFrameworkCore/Consent/ConsentAuditStoreEF.cs`
3. `src/Encina.EntityFrameworkCore/Consent/ConsentVersionManagerEF.cs`
4. `src/Encina.EntityFrameworkCore/Consent/ConsentRecordEntity.cs`
5. `src/Encina.EntityFrameworkCore/Consent/ConsentAuditEntryEntity.cs`
6. `src/Encina.EntityFrameworkCore/Consent/ConsentVersionEntity.cs`
7. `src/Encina.EntityFrameworkCore/Consent/ConsentRecordEntityConfiguration.cs`
8. `src/Encina.EntityFrameworkCore/Consent/ConsentAuditEntryEntityConfiguration.cs`
9. `src/Encina.EntityFrameworkCore/Consent/ConsentVersionEntityConfiguration.cs`
10. `src/Encina.EntityFrameworkCore/Consent/ConsentModelBuilderExtensions.cs`

#### DELETE from MongoDB (6 files)

1. `src/Encina.MongoDB/Consent/ConsentStoreMongoDB.cs`
2. `src/Encina.MongoDB/Consent/ConsentAuditStoreMongoDB.cs`
3. `src/Encina.MongoDB/Consent/ConsentVersionManagerMongoDB.cs`
4. `src/Encina.MongoDB/Consent/ConsentRecordDocument.cs`
5. `src/Encina.MongoDB/Consent/ConsentAuditEntryDocument.cs`
6. `src/Encina.MongoDB/Consent/ConsentVersionDocument.cs`

#### MODIFY ServiceCollectionExtensions in each provider (13 files)

1. Remove `if (config.UseConsent) { ... }` blocks from:
    - `src/Encina.ADO.Sqlite/ServiceCollectionExtensions.cs`
    - `src/Encina.ADO.SqlServer/ServiceCollectionExtensions.cs`
    - `src/Encina.ADO.PostgreSQL/ServiceCollectionExtensions.cs`
    - `src/Encina.ADO.MySQL/ServiceCollectionExtensions.cs`
    - `src/Encina.Dapper.Sqlite/ServiceCollectionExtensions.cs`
    - `src/Encina.Dapper.SqlServer/ServiceCollectionExtensions.cs`
    - `src/Encina.Dapper.PostgreSQL/ServiceCollectionExtensions.cs`
    - `src/Encina.Dapper.MySQL/ServiceCollectionExtensions.cs`
    - `src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs`
    - `src/Encina.MongoDB/ServiceCollectionExtensions.cs` (both overloads)

2. Remove `UseConsent` property from provider config options if present

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of migrating Encina.Compliance.Consent to Marten event sourcing (Issue #777).

CONTEXT:
- Phases 1-4 completed: new aggregate, events, service, DI are in place
- This phase removes ALL 13-provider consent store implementations
- ADR-019 mandates: "Store interfaces with 13-provider implementations are replaced, not deprecated"
- Pre-1.0: no backward compatibility, no [Obsolete], no migration helpers

TASK:
1. DELETE all Consent/ folders and files from ADO.NET providers (4 providers × 3 stores + SQL scripts)
2. DELETE all Consent/ folders and files from Dapper providers (4 providers × 3 stores)
3. DELETE all Consent/ folders and files from EF Core (stores + entities + configurations + model builder)
4. DELETE all Consent/ folders and files from MongoDB (stores + documents)
5. REMOVE UseConsent registration blocks from all 13 provider ServiceCollectionExtensions
6. Remove the UseConsent property from provider config options if it exists

KEY RULES:
- Delete files completely — no [Obsolete], no comments, no backup
- Git history preserves everything
- Clean up empty Consent/ directories after deletion
- ServiceCollectionExtensions: remove the `if (config.UseConsent)` block and its contents
- Do NOT remove other UseXxx blocks (e.g., UseOutbox, UseLawfulBasis) — only UseConsent
- Verify no compile errors after deletion

REFERENCE FILES:
- src/Encina.ADO.Sqlite/ServiceCollectionExtensions.cs (look for UseConsent block)
- src/Encina.Dapper.SqlServer/ServiceCollectionExtensions.cs (look for UseConsent block)
- src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs (look for UseConsent block)
- src/Encina.MongoDB/ServiceCollectionExtensions.cs (look for UseConsent block — TWO overloads)
```

</details>

---

### Phase 6: Observability Update

> **Goal**: Update diagnostics to reflect the event-sourced model while keeping existing EventId range (8200-8259).

<details>
<summary><strong>Tasks</strong></summary>

#### Modified files in `src/Encina.Compliance.Consent/`

1. **`Diagnostics/ConsentDiagnostics.cs`** — Update metrics
   - Keep existing: ActivitySource `Encina.Compliance.Consent`, Meter, consent.checks.* counters
   - Add new counters for aggregate operations:
     - `consent.granted.total` — consent grants
     - `consent.withdrawn.total` — consent withdrawals
     - `consent.expired.total` — consent expirations
     - `consent.renewed.total` — consent renewals
     - `consent.reconsent.total` — reconsent provisions
   - Update Activity tags to match event-sourced model

2. **`Diagnostics/ConsentLogMessages.cs`** — Update log messages
   - Keep pipeline-related logs (8200-8207) — unchanged
   - Replace store-level logs (8210-8216) with service-level logs:
     - 8210: `ConsentGrantedViaService` (replaces ConsentRecorded)
     - 8211: `ConsentWithdrawnViaService` (replaces ConsentWithdrawn)
     - 8212: `ConsentNotFound` (unchanged)
     - 8213: `ConsentQueried` (replaces ConsentFetched)
     - 8214: `ConsentExpiredViaService` (replaces ConsentExpiredDetected)
     - 8215: `ConsentRenewedViaService` (new)
     - 8216: `ConsentReconsentProvidedViaService` (new)
   - Remove audit store logs (8220-8221) — audit is inherent in event stream
   - Keep domain event logs (8230-8232) — unchanged
   - Keep auto-registration logs (8240-8243) — unchanged
   - Keep health check logs (8250) — unchanged

3. **`Health/ConsentHealthCheck.cs`** — Update dependency
   - Change from checking `IConsentStore` to checking `IConsentService`
   - Verify `IConsentService` is resolvable via scoped resolution

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of migrating Encina.Compliance.Consent to Marten event sourcing (Issue #777).

CONTEXT:
- Phases 1-5 completed: event-sourced model in place, old stores deleted
- Observability already exists (ConsentDiagnostics.cs, ConsentLogMessages.cs, ConsentHealthCheck.cs)
- EventId range: 8200-8259 (must stay within this range, no collisions)
- CrossBorderTransfer uses 8500-8555; GDPR uses 8100-8220; Marten uses 1-32

TASK:
1. Update ConsentDiagnostics.cs — add aggregate operation counters (consent.granted.total, etc.)
2. Update ConsentLogMessages.cs — replace store-level logs with service-level logs (keep same EventIds)
3. Update ConsentHealthCheck.cs — check IConsentService instead of IConsentStore

KEY RULES:
- Keep EventId range 8200-8259 — no collisions
- Pipeline logs (8200-8207) are UNCHANGED — they don't reference stores
- Store logs (8210-8216) get renamed but keep same EventIds for continuity
- Remove audit store logs (8220-8221) — audit is inherent in event stream
- Domain event, auto-registration, and health check logs stay the same
- Follow ConsentDiagnostics patterns for new counters (Counter<long>, dimensional tags)

REFERENCE FILES:
- src/Encina.Compliance.Consent/Diagnostics/ConsentDiagnostics.cs (current)
- src/Encina.Compliance.Consent/Diagnostics/ConsentLogMessages.cs (current)
- src/Encina.Compliance.Consent/Health/ConsentHealthCheck.cs (current)
- src/Encina.Compliance.CrossBorderTransfer/Diagnostics/CrossBorderTransferDiagnostics.cs (reference)
```

</details>

---

### Phase 7: Testing

> **Goal**: Update all consent tests to use the new event-sourced model.

<details>
<summary><strong>Tasks</strong></summary>

#### Unit Tests — `tests/Encina.UnitTests/Compliance/Consent/`

1. **NEW** `Aggregates/ConsentAggregateTests.cs`
   - Test all aggregate commands and state transitions
   - Test invariant violations (withdraw non-active, expire already-expired)
   - Test event generation (each command produces correct event)
   - Test Apply() reconstructs correct state from events
   - ~25-30 tests

2. **NEW** `Events/ConsentEventsTests.cs`
   - Test all 6 event record types — equality, immutability, required properties

3. **NEW** `ReadModels/ConsentProjectionTests.cs`
   - Test projection handler transforms events to read model correctly
   - Test full lifecycle: Grant → Withdraw → verify read model state
   - ~12-15 tests

4. **NEW** `Services/DefaultConsentServiceTests.cs`
   - Mock `IAggregateRepository<ConsentAggregate>` and `IReadModelRepository<ConsentReadModel>`
   - Test command methods delegate to aggregate correctly
   - Test query methods use read model repository
   - Test error handling (aggregate not found, save failure)
   - ~20-25 tests

5. **UPDATE** `ConsentRequiredPipelineBehaviorTests.cs`
   - Change mocked dependency from `IConsentStore` to `IConsentService`
   - All test logic remains the same

6. **DELETE** old store tests:
   - `InMemoryConsentStoreTests.cs`
   - `InMemoryConsentAuditStoreTests.cs`
   - `InMemoryConsentVersionManagerTests.cs`

7. **DELETE** old mediator event tests (replaced by unified ES events in item 2):
   - `Events/ConsentGrantedEventTests.cs`
   - `Events/ConsentWithdrawnEventTests.cs`
   - `Events/ConsentExpiredEventTests.cs`
   - `Events/ConsentVersionChangedEventTests.cs`

8. **KEEP** (no changes needed):
   - `Model/ConsentRecordTests.cs` (record still exists as reference type)
   - `Model/ConsentStatusTests.cs`
   - `Model/ConsentVersionTests.cs`
   - `ConsentOptionsTests.cs`, `ConsentOptionsValidatorTests.cs`
   - `RequireConsentAttributeTests.cs`
   - `ConsentErrorsTests.cs`

#### Guard Tests — `tests/Encina.GuardTests/Compliance/Consent/`

1. **NEW** `ConsentAggregateGuardTests.cs` — null checks on factory/command parameters
2. **NEW** `DefaultConsentServiceGuardTests.cs` — null checks on service method parameters
3. **UPDATE** `ConsentRequiredPipelineBehaviorGuardTests.cs` — update constructor dependencies
4. **DELETE** `InMemoryConsentStoreGuardTests.cs`, `InMemoryConsentVersionManagerGuardTests.cs`

#### Property Tests — `tests/Encina.PropertyTests/Compliance/Consent/`

1. **NEW** `ConsentAggregatePropertyTests.cs`
    - Invariant: Grant then Withdraw always produces Withdrawn status
    - Invariant: Event stream replay always produces same state
    - Invariant: Version increments monotonically

#### Contract Tests — `tests/Encina.ContractTests/Compliance/Consent/`

1. **UPDATE** `IConsentStoreContractTests.cs` → rename to `IConsentServiceContractTests.cs`
    - Test `IConsentService` contract (method signatures, return types, ROP patterns)

#### Integration Tests — `tests/Encina.IntegrationTests/`

1. **DELETE** all 13-provider consent integration tests:
    - `ADO/*/Consent/ConsentStore*Tests.cs` (4 providers × N test files)
    - `Dapper/*/Consent/ConsentStore*Tests.cs` (4 providers × N test files)
    - `Infrastructure/EntityFrameworkCore/*/Consent/ConsentStore*Tests.cs` (4 providers × N test files)
    - `Infrastructure/MongoDB/Consent/ConsentStore*Tests.cs`
    - `Infrastructure/EntityFrameworkCore/Consent/ConsentTestDbContext.cs`, `ConsentTestPostgreSqlDbContext.cs`

2. **NEW** `Compliance/Consent/ConsentMartenIntegrationTests.cs`
    - Uses Marten PostgreSQL fixture
    - Tests full aggregate lifecycle via `IConsentService`
    - Tests projection produces correct read model
    - Tests event stream audit trail (GetConsentHistoryAsync)
    - Tests `EventPublishingPipelineBehavior` auto-publishes unified events (verify `INotificationHandler<ConsentGranted>` receives event after commit)
    - ~12-18 tests

3. **UPDATE** `Compliance/Consent/ConsentPipelineIntegrationTests.cs`
    - Update to use `IConsentService` instead of `IConsentStore`

#### Load/Benchmark Tests

1. **UPDATE** `tests/Encina.LoadTests/Compliance/Consent/ConsentValidationLoadTests.cs` — update dependencies
2. **UPDATE** `tests/Encina.BenchmarkTests/Encina.Benchmarks/Compliance/Consent/` — update dependencies

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of migrating Encina.Compliance.Consent to Marten event sourcing (Issue #777).

CONTEXT:
- Phases 1-6 completed: full event-sourced consent model, observability updated, old stores deleted
- Testing must cover: aggregate behavior, events, projections, service, pipeline behavior
- 13-provider integration tests are removed — replaced by Marten integration tests
- Guard, property, and contract tests need updating

TASK:
1. Create unit tests: ConsentAggregateTests, ConsentEventsTests, ConsentProjectionTests, DefaultConsentServiceTests
2. Update ConsentRequiredPipelineBehaviorTests (IConsentStore → IConsentService)
3. Delete old store tests (InMemoryConsentStoreTests, etc.)
4. Create guard tests for new classes (ConsentAggregate, DefaultConsentService), delete old guard tests
5. Create property tests for aggregate invariants
6. Update contract tests
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
- tests/Encina.GuardTests/Compliance/CrossBorderTransfer/ (reference guard tests)
- tests/Encina.PropertyTests/Compliance/CrossBorderTransfer/ (reference property tests)
- tests/Encina.IntegrationTests/Compliance/ (reference Marten integration tests)
```

</details>

---

### Phase 8: Documentation & Finalization

> **Goal**: Update all documentation, public API tracking, and verify build.

<details>
<summary><strong>Tasks</strong></summary>

1. **`PublicAPI.Unshipped.txt`** — Update public API surface
   - Remove: `IConsentStore`, `IConsentAuditStore`, `IConsentVersionManager` and all their methods
   - Remove: `InMemoryConsentStore`, `InMemoryConsentAuditStore`, `InMemoryConsentVersionManager`
   - Add: `IConsentService` and all methods
   - Add: `ConsentAggregate` and public members
   - Add: `ConsentReadModel` and properties
   - Add: All 6 event records
   - Add: `ConsentMartenExtensions.AddConsentAggregates()`
   - Add: `ConsentProjection`

2. **`CHANGELOG.md`** — Add entry under Unreleased

   ```markdown
   ### Changed
   - **Encina.Compliance.Consent**: Migrated from entity-based persistence (13 database providers) to Marten event sourcing (PostgreSQL). This provides an immutable audit trail for GDPR Art. 7 accountability. See ADR-019. (Fixes #777)
     - `IConsentStore`, `IConsentAuditStore`, `IConsentVersionManager` → replaced by `IConsentService`
     - 13-provider store implementations removed (ADO.NET, Dapper, EF Core, MongoDB)
     - Separate mediator events (`ConsentGrantedEvent`, etc.) removed — ES events implement `INotification` and serve as both event store facts and mediator notifications (unified event model)
     - New: `ConsentAggregate`, `ConsentReadModel`, `ConsentProjection`
     - New dependencies: `Encina.DomainModeling`, `Encina.Marten`, `Encina.Caching`
     - Registration: `services.AddEncinaConsent()` + `services.AddConsentAggregates()` (Marten)
     - No InMemory implementation — unit tests mock `IAggregateRepository<T>`; integration tests use real Marten
   ```

3. **`ROADMAP.md`** — Update if Consent migration was a planned item

4. **`src/Encina.Compliance.Consent/README.md`** — Update
   - Document event-sourced architecture
   - Update registration examples (AddEncinaConsent + AddConsentAggregates)
   - Document PostgreSQL requirement (Marten)
   - Update usage examples (IConsentService API)
   - Document testing strategy: mock `IAggregateRepository<T>` for unit tests, Docker PostgreSQL for integration

5. **`docs/features/consent.md`** — Update if exists, or note in CHANGELOG

6. **`docs/INVENTORY.md`** — Remove deleted files, add new files

7. **XML documentation** — Verify all new public APIs have `<summary>`, `<remarks>`, `<param>`, `<returns>`, `<example>`

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
You are implementing Phase 8 (final) of migrating Encina.Compliance.Consent to Marten event sourcing (Issue #777).

CONTEXT:
- Phases 1-7 completed: full migration done, tests updated
- This phase is documentation, public API tracking, and build verification
- CLAUDE.md mandates: "XML documentation on all public APIs", "PublicAPI.Unshipped.txt must track all public symbols"

TASK:
1. Update PublicAPI.Unshipped.txt — remove old interfaces/classes, add new ones
2. Add CHANGELOG.md entry under ### Changed in Unreleased section
3. Update README.md for the Consent package
4. Update docs/INVENTORY.md if it exists
5. Verify all new public APIs have XML documentation
6. Run dotnet build --configuration Release → verify 0 errors, 0 warnings
7. Run dotnet test → verify all tests pass

KEY RULES:
- PublicAPI format: Namespace.Type.Member(params) -> ReturnType
- Nullable annotations: string! (non-null), string? (nullable)
- CHANGELOG follows Keep a Changelog format
- No AI attribution in commits (per CLAUDE.md)
- README should show registration (AddEncinaConsent + AddConsentAggregates) and testing strategy (mocks + Docker)

REFERENCE FILES:
- src/Encina.Compliance.Consent/PublicAPI.Unshipped.txt (current)
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
| GDPR Art. 6(1)(a) | Lawfulness: consent | Consent must be specific, informed, unambiguous |
| GDPR Art. 7(1) | Conditions for consent | Controller must demonstrate consent was given |
| GDPR Art. 7(3) | Withdrawal of consent | Must be as easy as giving consent |
| GDPR Art. 17 | Right to erasure | Crypto-shredding for event-sourced personal data |
| GDPR Art. 30 | Records of processing | Event stream serves as processing record |
| Schrems II (CJEU C-311/18) | Data transfer safeguards | Context for compliance module ecosystem |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `AggregateBase` | `src/Encina.DomainModeling/` | Base class for ConsentAggregate |
| `IAggregateRepository<T>` | `src/Encina.Marten/` | Persistence for ConsentAggregate |
| `IReadModel`, `IProjectionHandler` | `src/Encina.Marten/Projections/` | ConsentReadModel and ConsentProjection |
| `IReadModelRepository<T>` | `src/Encina.Marten/Projections/` | Query read models in DefaultConsentService |
| `AddAggregateRepository<T>()` | `src/Encina.Marten/ServiceCollectionExtensions.cs` | DI registration for aggregate repository |
| `ConsentOptions` | `src/Encina.Compliance.Consent/` | Preserved — configuration unchanged |
| `ConsentDiagnostics` | `src/Encina.Compliance.Consent/Diagnostics/` | Updated — add aggregate operation counters |
| `ConsentRequiredPipelineBehavior` | `src/Encina.Compliance.Consent/` | Adapted — IConsentStore → IConsentService |
| `ConsentHealthCheck` | `src/Encina.Compliance.Consent/Health/` | Adapted — check IConsentService |
| `[RequireConsent]` attribute | `src/Encina.Compliance.Consent/Attributes/` | Preserved — no changes |
| `ConsentStatus` enum | `src/Encina.Compliance.Consent/Model/` | Preserved — reused in aggregate and read model |

> **Note on InMemory**: No `InMemoryConsentService` is created. CrossBorderTransfer (reference implementation) has no InMemory implementation either. Unit tests mock `IAggregateRepository<T>` with NSubstitute; integration tests use real Marten + PostgreSQL in Docker.

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| Encina.Marten | 1-32 | Aggregate lifecycle |
| Encina.Security | 8000-8004 | Security events |
| Encina.OpenTelemetry | 7000-7009 | Resharding |
| Encina.Compliance.GDPR | 8100-8220 | GDPR core |
| **Encina.Compliance.Consent** | **8200-8259** | **Consent (keeping same range)** |
| Encina.Marten.GDPR | 8400-8415 | Crypto-shredding |
| Encina.Compliance.CrossBorderTransfer | 8500-8555 | Cross-border transfer |

> Note: Consent EventIds 8200-8259 are preserved. Store-level events (8210-8216) are renamed to service-level. Audit store events (8220-8221) are removed. No new EventIds needed.

### Estimated File Count

| Category | New | Modified | Deleted | Total Impact |
|----------|:---:|:--------:|:-------:|:------------:|
| Aggregates | 1 | 0 | 0 | 1 |
| Events (ES, unified) | 1 | 0 | 4 | 5 |
| Read Models/Projections | 2 | 0 | 0 | 2 |
| Service Interfaces | 1 | 0 | 3 | 4 |
| Service Implementations | 1 | 0 | 3 | 4 |
| Configuration/DI | 1 | 3 | 0 | 4 |
| Diagnostics | 0 | 3 | 0 | 3 |
| Provider Stores (DELETE) | 0 | 0 | ~43 | 43 |
| Provider DI (MODIFY) | 0 | 10 | 0 | 10 |
| Unit Tests | 4 | 1 | 7 | 12 |
| Guard Tests | 2 | 1 | 2 | 5 |
| Property Tests | 1 | 0 | 0 | 1 |
| Contract Tests | 0 | 1 | 0 | 1 |
| Integration Tests | 1 | 2 | ~13 | 16 |
| Documentation | 0 | 4 | 0 | 4 |
| **TOTAL** | **~15** | **~25** | **~75** | **~115** |

---

## Combined AI Agent Prompt

<details>
<summary><strong>Complete Implementation Prompt — All Phases</strong></summary>

```
You are migrating Encina.Compliance.Consent from entity-based persistence to Marten event sourcing (Issue #777).

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 compliance library using Railway Oriented Programming (Either<EncinaError, T>)
- Pre-1.0: breaking changes acceptable, no backward compatibility
- ADR-019 mandates migration of all stateful compliance modules to Marten event sourcing
- CrossBorderTransfer (#412) is the reference implementation — follow its patterns exactly
- AggregateBase from Encina.DomainModeling provides: Id, Version, RaiseEvent<T>, Apply(object)
- IAggregateRepository<T> from Encina.Marten provides: LoadAsync, SaveAsync, CreateAsync
- IReadModel, IProjectionHandler from Encina.Marten.Projections for query-side projections

IMPLEMENTATION OVERVIEW:

Phase 1 — Events & Aggregate:
- Create Events/ConsentEvents.cs with 6 sealed record events implementing : INotification
  (ConsentGranted, ConsentWithdrawn, ConsentExpired, ConsentRenewed, ConsentVersionChanged, ConsentReconsentProvided)
- DELETE old mediator events (ConsentGrantedEvent, ConsentWithdrawnEvent, ConsentExpiredEvent, ConsentVersionChangedEvent)
  — the ES events replace them as the single source of truth (unified event model)
- Events are auto-published by EventPublishingPipelineBehavior after Marten commit
- Create Aggregates/ConsentAggregate.cs extending AggregateBase
- Aggregate keyed by (SubjectId, Purpose) — one stream per consent decision

Phase 2 — Read Model & Projection:
- Create ReadModels/ConsentReadModel.cs implementing IReadModel
- Create ReadModels/ConsentProjection.cs implementing IProjectionHandler for each event

Phase 3 — Service Interface & Implementation:
- Create Abstractions/IConsentService.cs (replaces IConsentStore + IConsentAuditStore + IConsentVersionManager)
- Create Services/DefaultConsentService.cs (uses IAggregateRepository + IReadModelRepository)
- No InMemory implementation — unit tests mock IAggregateRepository<T>; integration tests use real Marten
- DELETE old interfaces and in-memory implementations

Phase 4 — Configuration & DI:
- Update ServiceCollectionExtensions.cs (IConsentService → DefaultConsentService via TryAddScoped)
- Create ConsentMartenExtensions.cs (AddConsentAggregates)
- Update ConsentRequiredPipelineBehavior (IConsentStore → IConsentService)

Phase 5 — Remove 13-Provider Stores:
- DELETE all Consent/ folders from ADO.NET (4), Dapper (4), EF Core, MongoDB
- DELETE SQL scripts for consent tables
- REMOVE UseConsent blocks from 13 ServiceCollectionExtensions

Phase 6 — Observability:
- Update ConsentDiagnostics.cs (add aggregate operation counters)
- Update ConsentLogMessages.cs (rename store logs to service logs)
- Update ConsentHealthCheck.cs (IConsentStore → IConsentService)

Phase 7 — Testing:
- NEW: ConsentAggregateTests, ConsentEventsTests, ConsentProjectionTests, DefaultConsentServiceTests
- UPDATE: Pipeline behavior tests, guard tests, contract tests
- DELETE: Old store tests, old InMemory tests, 13-provider integration tests
- NEW: Marten integration test (real PostgreSQL in Docker)

Phase 8 — Documentation:
- Update PublicAPI.Unshipped.txt, CHANGELOG.md, README.md, INVENTORY.md
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
  EventPublishingPipelineBehavior auto-publishes them (.OfType<INotification>() filter)
  No separate mediator event types — DELETE old ConsentGrantedEvent, ConsentWithdrawnEvent, etc.
- Events are sealed records, aggregates are sealed classes
- All public types need XML documentation
- EventId range: 8200-8259 (existing allocation, no new IDs needed)
- New project dependencies: Encina.DomainModeling, Encina.Marten, Encina.Caching
- TenantId and ModuleId on events/aggregate for cross-cutting support
- NO InMemory implementation — DefaultConsentService registered via TryAddScoped; unit tests mock IAggregateRepository<T>
- No [Obsolete], no migration helpers, no backward compatibility
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | **Caching** | ✅ Include | `DefaultConsentService` caches read model queries via `ICacheProvider` (optional dependency). Cache invalidation on write operations. Pattern: `"consent:{id}"`, `"consent:subject:{subjectId}:purpose:{purpose}"` |
| 2 | **OpenTelemetry** | ✅ Include | Existing `ConsentDiagnostics` updated — ActivitySource preserved, new counters for aggregate operations (consent.granted.total, consent.withdrawn.total, etc.) |
| 3 | **Structured Logging** | ✅ Include | Existing `ConsentLogMessages.cs` updated — store logs renamed to service logs, same EventId range 8200-8259 |
| 4 | **Health Checks** | ✅ Include | Existing `ConsentHealthCheck` updated — checks `IConsentService` resolvability instead of `IConsentStore` |
| 5 | **Validation** | ✅ Include | `DefaultConsentValidator` and `ConsentRequiredPipelineBehavior` preserved — adapted to use `IConsentService` |
| 6 | **Resilience** | ❌ N/A | Marten handles PostgreSQL retries internally. No external system calls in consent module |
| 7 | **Distributed Locks** | ❌ N/A | Marten uses optimistic concurrency (aggregate versioning). No shared mutable state requiring external locks |
| 8 | **Transactions** | ✅ Include | Inherent in Marten — events + projections committed in a single PostgreSQL transaction |
| 9 | **Idempotency** | ✅ Include | Aggregate versioning prevents duplicate event application. Correlation ID via event metadata enrichment |
| 10 | **Multi-Tenancy** | ✅ Include | `TenantId` field on aggregate and events. Marten supports native tenancy via `IDocumentSession` conjoined tenancy |
| 11 | **Module Isolation** | ✅ Include | `ModuleId` field on aggregate and events for modular monolith scoping |
| 12 | **Audit Trail** | ✅ Include | **Inherent** — the event stream IS the audit trail. `GetConsentHistoryAsync()` returns raw event stream. No separate audit store needed |

---

## Dependencies & Prerequisites

| Dependency | Status | Notes |
|------------|--------|-------|
| [#776 ADR-019: Compliance Event Sourcing Strategy](https://github.com/dlrivada/Encina/issues/776) | ✅ Completed | Architectural decision in place |
| [#412 CrossBorderTransfer (reference implementation)](https://github.com/dlrivada/Encina/issues/412) | ✅ Completed | Pattern established |
| `Encina.Marten` package | ✅ Production | `IAggregateRepository<T>`, `IReadModel`, `IProjectionHandler`, `AddAggregateRepository<T>()` |
| `Encina.DomainModeling` package | ✅ Production | `AggregateBase`, `IAggregate` |
| PostgreSQL infrastructure | ✅ Available | Docker Compose profile: `databases` |

No blocking prerequisites identified. All required infrastructure exists.
