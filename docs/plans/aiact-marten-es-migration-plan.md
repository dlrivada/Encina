# Implementation Plan: `Encina.Compliance.AIAct` — Marten Event Sourcing Migration

> **Issue**: [#847](https://github.com/dlrivada/Encina/issues/847) (child of [#415](https://github.com/dlrivada/Encina/issues/415))
> **Type**: Feature / Architectural Migration
> **Complexity**: High (8 phases, Marten ES, 2 aggregates, ~40 files)
> **Estimated Scope**: ~2,000-2,500 lines of production code + ~1,500-2,000 lines of tests
> **Related**: [ADR-019](../architecture/adr/019-compliance-event-sourcing-marten.md), [#839](https://github.com/dlrivada/Encina/issues/839), [#842](https://github.com/dlrivada/Encina/issues/842), [#845](https://github.com/dlrivada/Encina/issues/845), [#846](https://github.com/dlrivada/Encina/issues/846)

---

## Summary

Migrate `Encina.Compliance.AIAct` from in-memory-only persistence (`ConcurrentDictionary`) to Marten event sourcing (PostgreSQL), following the pattern established by ADR-019 and already applied to 9 other compliance modules (Consent, DataSubjectRights, LawfulBasis, BreachNotification, DPIA, ProcessorAgreements, Retention, DataResidency, CrossBorderTransfer).

The migration introduces **2 event-sourced aggregates** (`AISystemAggregate` for the AI system registry lifecycle, `HumanOversightAggregate` for human decision records), **inline projections** to read models, and a **service layer** with CQRS separation. The stateless components (classifier, validator, pipeline behavior) remain unchanged — they continue to evaluate compliance at runtime using registry data.

**Why now**: The current in-memory implementation loses all data on restart, cannot provide regulatory audit trails (Art. 6(3) reclassification history, Art. 14 human decision evidence), and is the only compliance module without immutable event sourcing — creating an inconsistency gap in the v0.13.0 Security & Compliance milestone.

**Affected packages**: `Encina.Compliance.AIAct` (modified — add aggregates, events, projections, services, Marten registration)

**Provider category**: Event Sourcing (Marten/PostgreSQL) — per ADR-019, compliance modules use Marten exclusively (not 13-provider)

---

## Design Choices

<details>
<summary><strong>1. Package Placement — Aggregates in Core vs Separate Marten Package</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Aggregates in core `Encina.Compliance.AIAct`** | Single package, simpler DI, follows Consent/DPIA/BreachNotification pattern | Core takes Marten dependency (via `AggregateBase` from `Encina.DomainModeling`) |
| **B — Separate `Encina.Marten.AIAct` satellite** | Core stays dependency-free, clean separation | Extra package, more complex DI registration, no precedent in compliance modules |

### Chosen Option: **A — Aggregates in core package**

### Rationale

All 9 migrated compliance modules (Consent, DPIA, BreachNotification, CrossBorderTransfer, etc.) place aggregates, events, projections, and services directly in the core package. `AggregateBase` comes from `Encina.DomainModeling` which is already a dependency. Marten-specific registration uses a separate `AddXxxAggregates()` extension method that can be called independently. This is the established and proven pattern — deviating would create inconsistency.

</details>

<details>
<summary><strong>2. Aggregate Design — Single vs Multiple Aggregates</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Single `AISystemAggregate` for everything** | Simple, one stream per system | Violates SRP: mixes system lifecycle with human decisions |
| **B — Two aggregates: `AISystemAggregate` + `HumanOversightAggregate`** | SRP, independent lifecycles, follows CrossBorderTransfer 3-aggregate pattern | Cross-aggregate references needed, slightly more complex |
| **C — Three aggregates (add `ConformityAssessmentAggregate`)** | Complete coverage | Over-engineering — conformity assessment is a separate child issue (#844) |

### Chosen Option: **B — Two aggregates**

### Rationale

AI system registration/reclassification and human oversight decisions are **independent lifecycles** with different actors, timelines, and audit requirements. An AI system is registered once and reclassified occasionally (admin actor), while human decisions happen per-request (reviewer actor). CrossBorderTransfer successfully uses 3 aggregates (TIA, SCC, ApprovedTransfer) for the same reason. Conformity assessment (#844) can be added as a third aggregate in a future child issue without modifying the first two.

</details>

<details>
<summary><strong>3. AISystemAggregate Lifecycle — State Machine Design</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Minimal: Registered → Reclassified** | Matches current InMemory behavior exactly | Misses decommissioning, compliance evaluation history |
| **B — Full: Registered → Active → Reclassified → Suspended → Decommissioned** | Complete lifecycle per Art. 6(3) | Complex, some states may not apply initially |
| **C — Pragmatic: Registered → Active → Reclassified ↺ → Decommissioned** | Covers all regulatory requirements without over-engineering | Minor complexity vs A |

### Chosen Option: **C — Pragmatic lifecycle**

### Rationale

Art. 6(3) requires tracking risk level changes over time, and Art. 51 requires AI providers to maintain registration records. The Registered → Active → Reclassified ↺ → Decommissioned lifecycle covers these requirements. Reclassification is a repeatable event (can happen multiple times), which event sourcing handles naturally. The `Suspended` state from Option B can be added later if needed — pre-1.0 means we can evolve freely.

</details>

<details>
<summary><strong>4. HumanOversightAggregate Design — Per-Decision vs Per-System</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — One aggregate per human decision** | Simple, immutable, one event per stream | Many small streams, hard to query "all decisions for system X" |
| **B — One aggregate per AI system (collects decisions)** | Natural grouping, easy "show me all decisions for system X" | Growing stream, mixes concerns with system lifecycle |
| **C — One aggregate per (SystemId + CorrelationId) oversight session** | Groups related decisions (request → review → decide → override) | Right granularity for Art. 14 audit trail |

### Chosen Option: **C — Per oversight session**

### Rationale

Art. 14 requires documenting the human oversight **process**, not just individual decisions. An oversight session naturally groups: the initial review request, the human decision, potential escalations, and potential overrides. This maps to the BreachNotification pattern (one aggregate per breach incident with multiple lifecycle events). The `CorrelationId` ties the oversight session to the original AI system request.

</details>

<details>
<summary><strong>5. Service Layer — IConsentService Pattern vs Direct Repository Access</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Direct IAggregateRepository injection in pipeline behavior** | Simpler, fewer abstractions | Leaks infrastructure into pipeline behavior, harder to test |
| **B — Service layer (IAISystemService, IHumanOversightService)** | Encapsulates aggregate operations, cache integration, testable | More code |

### Chosen Option: **B — Service layer**

### Rationale

All migrated compliance modules (Consent, CrossBorderTransfer, DPIA) use a service layer that wraps `IAggregateRepository`. The service handles: aggregate loading/saving, cache invalidation, error mapping to `Either<EncinaError, T>`, and OpenTelemetry recording. The pipeline behavior and health check consume the service, not the raw repository.

</details>

<details>
<summary><strong>6. Migration Strategy — Replace InMemory vs Keep Both</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — Replace InMemoryAISystemRegistry completely** | Clean, no dual-path code | InMemory is useful for testing without PostgreSQL |
| **B — Keep InMemory as default, add Marten as opt-in** | Backward compatible, gradual adoption | Dual code paths, complexity |
| **C — InMemory for testing only, Marten for production (TryAdd pattern)** | Best of both: InMemory in tests, Marten in prod | Slight config complexity |

### Chosen Option: **C — InMemory for testing, Marten for production**

### Rationale

This follows the exact Consent module pattern. `AddEncinaAIAct()` registers default in-memory services via `TryAdd`. `AddAIActAggregates()` registers Marten aggregate repositories that override the in-memory versions. In tests, you skip the Marten call and get fast in-memory behavior. In production, you call both and get persistent event sourcing. The `InMemoryAISystemRegistry` stays but becomes a testing utility.

</details>

<details>
<summary><strong>7. Read Model Caching — Cache Layer for Query Performance</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A — No caching, always query Marten** | Simple, always consistent | Unnecessary DB load for frequently-read registry data |
| **B — ICacheProvider with TTL + invalidation** | Performance, follows Consent pattern | Cache staleness during TTL window |

### Chosen Option: **B — ICacheProvider with TTL + invalidation**

### Rationale

AI system registry data is read on every decorated request (pipeline behavior checks classification). Caching read models with 5-minute TTL and fire-and-forget invalidation on mutations follows the Consent and CrossBorderTransfer pattern. Staleness is acceptable — a 5-minute delay in reflecting a reclassification is not a compliance risk.

</details>

---

## Implementation Phases

### Phase 1: Domain Events & Aggregate Models

> **Goal**: Define all domain events and aggregate state for both AI system lifecycle and human oversight.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `src/Encina.Compliance.AIAct/Events/AISystemEvents.cs`**
   - `AISystemRegistered` record : `INotification`
     - `Guid SystemId`, `string Name`, `AISystemCategory Category`, `AIRiskLevel RiskLevel`, `IReadOnlyList<ProhibitedPractice> ProhibitedPractices`, `string? Provider`, `string? Version`, `string RegisteredBy`, `DateTimeOffset OccurredAtUtc`, `string? TenantId`, `string? ModuleId`
   - `AISystemReclassified` record : `INotification`
     - `Guid SystemId`, `AIRiskLevel PreviousRiskLevel`, `AIRiskLevel NewRiskLevel`, `string Reason`, `string ReclassifiedBy`, `DateTimeOffset OccurredAtUtc`
   - `AISystemComplianceEvaluated` record : `INotification`
     - `Guid SystemId`, `AIRiskLevel RiskLevel`, `bool IsCompliant`, `IReadOnlyList<string> Violations`, `bool RequiresHumanOversight`, `bool RequiresTransparency`, `DateTimeOffset EvaluatedAtUtc`
   - `AISystemDecommissioned` record : `INotification`
     - `Guid SystemId`, `string Reason`, `string DecommissionedBy`, `DateTimeOffset OccurredAtUtc`

2. **Create `src/Encina.Compliance.AIAct/Events/HumanOversightEvents.cs`**
   - `HumanReviewRequested` record : `INotification`
     - `Guid SessionId`, `string SystemId`, `string RequestType`, `string? CorrelationId`, `string RequestedBy`, `DateTimeOffset OccurredAtUtc`, `string? TenantId`, `string? ModuleId`
   - `HumanDecisionRecorded` record : `INotification`
     - `Guid SessionId`, `string ReviewerId`, `string Decision` (Approved/Rejected/Escalated), `string Rationale`, `DateTimeOffset OccurredAtUtc`
   - `HumanDecisionEscalated` record : `INotification`
     - `Guid SessionId`, `string EscalatedBy`, `string EscalationReason`, `string EscalatedTo`, `DateTimeOffset OccurredAtUtc`
   - `HumanDecisionOverridden` record : `INotification`
     - `Guid SessionId`, `string OverriddenBy`, `string OriginalDecision`, `string NewDecision`, `string OverrideReason`, `DateTimeOffset OccurredAtUtc`

3. **Create `src/Encina.Compliance.AIAct/Aggregates/AISystemAggregate.cs`**
   - Extends `AggregateBase`
   - Properties: `Name`, `Category`, `RiskLevel`, `ProhibitedPractices`, `Provider`, `Version`, `Status` (Registered/Active/Decommissioned), `RegisteredBy`, `RegisteredAtUtc`, `LastReclassifiedAtUtc`, `TenantId`, `ModuleId`
   - Static factory: `Register(...)` → raises `AISystemRegistered`
   - Command: `Reclassify(newRiskLevel, reason, reclassifiedBy, occurredAtUtc)` → raises `AISystemReclassified`
   - Command: `RecordComplianceEvaluation(...)` → raises `AISystemComplianceEvaluated`
   - Command: `Decommission(reason, decommissionedBy, occurredAtUtc)` → raises `AISystemDecommissioned`
   - `Apply(object domainEvent)` override with pattern matching for all 4 event types
   - Invariants: cannot reclassify decommissioned system, cannot decommission already decommissioned

4. **Create `src/Encina.Compliance.AIAct/Aggregates/HumanOversightAggregate.cs`**
   - Extends `AggregateBase`
   - Properties: `SystemId`, `RequestType`, `CorrelationId`, `Status` (Requested/UnderReview/Decided/Escalated/Overridden), `Decision`, `Rationale`, `ReviewerId`, `RequestedBy`, `RequestedAtUtc`, `DecidedAtUtc`, `TenantId`, `ModuleId`
   - Static factory: `RequestReview(...)` → raises `HumanReviewRequested`
   - Command: `RecordDecision(reviewerId, decision, rationale, occurredAtUtc)` → raises `HumanDecisionRecorded`
   - Command: `Escalate(escalatedBy, reason, escalatedTo, occurredAtUtc)` → raises `HumanDecisionEscalated`
   - Command: `Override(overriddenBy, newDecision, reason, occurredAtUtc)` → raises `HumanDecisionOverridden`
   - `Apply(object domainEvent)` override with pattern matching for all 4 event types
   - Invariants: cannot decide without request, cannot escalate after final decision, cannot override without existing decision

5. **Create `src/Encina.Compliance.AIAct/Model/AISystemStatus.cs`**
   - Enum: `Registered`, `Active`, `Decommissioned`

6. **Create `src/Encina.Compliance.AIAct/Model/OversightSessionStatus.cs`**
   - Enum: `Requested`, `UnderReview`, `Decided`, `Escalated`, `Overridden`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of Encina.Compliance.AIAct Marten ES Migration.

CONTEXT:
- Encina.Compliance.AIAct is a stateless compliance engine already implemented (43 .cs files).
- Currently uses ConcurrentDictionary for AI system registry and human decisions — data lost on restart.
- Per ADR-019, all stateful compliance modules must use Marten event sourcing for immutable audit trails.
- 9 other compliance modules have already been migrated (Consent, DPIA, BreachNotification, etc.).

TASK:
Create domain events and aggregates for two lifecycles:
1. AISystemAggregate — Registered → Active → Reclassified ↺ → Decommissioned
2. HumanOversightAggregate — Requested → UnderReview → Decided → Escalated/Overridden

KEY RULES:
- Events are sealed records implementing INotification
- All events include DateTimeOffset OccurredAtUtc
- All aggregates include TenantId? and ModuleId? for multi-tenancy/module isolation
- Aggregates extend AggregateBase from Encina.DomainModeling
- Use RaiseEvent() in command methods, pattern matching in Apply()
- Static factory methods for aggregate creation (e.g., Register(), RequestReview())
- Guard clauses with ArgumentNullException.ThrowIfNullOrEmpty() for required params
- Invariant enforcement: throw InvalidOperationException for invalid state transitions
- IReadOnlyList<T> for collection properties (not List<T>)
- Namespace: Encina.Compliance.AIAct.Events, Encina.Compliance.AIAct.Aggregates

REFERENCE FILES:
- src/Encina.Compliance.Consent/Aggregates/ConsentAggregate.cs (aggregate pattern)
- src/Encina.Compliance.Consent/Events/ConsentEvents.cs (event record pattern)
- src/Encina.Compliance.BreachNotification/Aggregates/BreachAggregate.cs (lifecycle pattern)
- src/Encina.Compliance.CrossBorderTransfer/Aggregates/TIAAggregate.cs (multi-aggregate pattern)
```

</details>

---

### Phase 2: Read Models & Projections

> **Goal**: Create Marten-compatible read models and inline projections for CQRS query side.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `src/Encina.Compliance.AIAct/ReadModels/AISystemReadModel.cs`**
   - Implements `IReadModel`
   - Properties (all mutable for projection): `Guid Id`, `string Name`, `AISystemCategory Category`, `AIRiskLevel RiskLevel`, `AISystemStatus Status`, `IReadOnlyList<ProhibitedPractice> ProhibitedPractices`, `string? Provider`, `string? Version`, `string RegisteredBy`, `DateTimeOffset RegisteredAtUtc`, `DateTimeOffset? LastReclassifiedAtUtc`, `DateTimeOffset? DecommissionedAtUtc`, `bool IsProhibited` (computed from ProhibitedPractices), `int ComplianceEvaluationCount`, `DateTimeOffset? LastEvaluatedAtUtc`, `string? TenantId`, `string? ModuleId`, `DateTimeOffset LastModifiedAtUtc`, `int Version`

2. **Create `src/Encina.Compliance.AIAct/ReadModels/AISystemProjection.cs`**
   - Implements `IProjection<AISystemReadModel>`, `IProjectionCreator<AISystemRegistered, AISystemReadModel>`, `IProjectionHandler<AISystemReclassified, AISystemReadModel>`, `IProjectionHandler<AISystemComplianceEvaluated, AISystemReadModel>`, `IProjectionHandler<AISystemDecommissioned, AISystemReadModel>`
   - `ProjectionName => "AISystemProjection"`
   - `Create()` from `AISystemRegistered` → initialize all fields, Status = Active, Version = 1
   - `Apply(AISystemReclassified)` → update RiskLevel, LastReclassifiedAtUtc, increment Version
   - `Apply(AISystemComplianceEvaluated)` → update evaluation count and timestamp, increment Version
   - `Apply(AISystemDecommissioned)` → Status = Decommissioned, DecommissionedAtUtc, increment Version

3. **Create `src/Encina.Compliance.AIAct/ReadModels/HumanOversightReadModel.cs`**
   - Implements `IReadModel`
   - Properties: `Guid Id`, `string SystemId`, `string RequestType`, `string? CorrelationId`, `OversightSessionStatus Status`, `string? ReviewerId`, `string? Decision`, `string? Rationale`, `string RequestedBy`, `DateTimeOffset RequestedAtUtc`, `DateTimeOffset? DecidedAtUtc`, `DateTimeOffset? EscalatedAtUtc`, `DateTimeOffset? OverriddenAtUtc`, `string? TenantId`, `string? ModuleId`, `DateTimeOffset LastModifiedAtUtc`, `int Version`

4. **Create `src/Encina.Compliance.AIAct/ReadModels/HumanOversightProjection.cs`**
   - Implements `IProjection<HumanOversightReadModel>`, `IProjectionCreator<HumanReviewRequested, HumanOversightReadModel>`, `IProjectionHandler<HumanDecisionRecorded, HumanOversightReadModel>`, `IProjectionHandler<HumanDecisionEscalated, HumanOversightReadModel>`, `IProjectionHandler<HumanDecisionOverridden, HumanOversightReadModel>`
   - `ProjectionName => "HumanOversightProjection"`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of Encina.Compliance.AIAct Marten ES Migration.

CONTEXT:
- Phase 1 created aggregates and events for AISystem and HumanOversight lifecycles.
- Now create read models (CQRS query side) and inline projections that transform events to read models.
- Read models are mutable classes (projection updates them), aggregates are immutable (events reconstruct them).

TASK:
Create 4 files: 2 read models and 2 projections (one pair per aggregate).

KEY RULES:
- Read models implement IReadModel (from Encina.Marten)
- Read models have mutable public setters (projections update them)
- Read models include LastModifiedAtUtc and Version for cache invalidation
- Projections implement IProjection<T>, IProjectionCreator<TFirstEvent, T>, IProjectionHandler<TEvent, T>
- Projections are idempotent — applying the same event twice produces the same result
- Version increments on every event
- Namespace: Encina.Compliance.AIAct.ReadModels

REFERENCE FILES:
- src/Encina.Compliance.Consent/ReadModels/ConsentReadModel.cs
- src/Encina.Compliance.Consent/ReadModels/ConsentProjection.cs
- src/Encina.Compliance.CrossBorderTransfer/ReadModels/ApprovedTransferReadModel.cs
```

</details>

---

### Phase 3: Service Layer (CQRS)

> **Goal**: Create service interfaces and implementations that wrap aggregate repositories with cache, error handling, and observability.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `src/Encina.Compliance.AIAct/Abstractions/IAISystemService.cs`**
   - `ValueTask<Either<EncinaError, AISystemReadModel>> RegisterSystemAsync(string name, AISystemCategory category, AIRiskLevel riskLevel, IReadOnlyList<ProhibitedPractice> prohibitedPractices, string registeredBy, string? provider, string? version, string? tenantId, string? moduleId, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, AISystemReadModel>> GetSystemAsync(Guid systemId, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, AISystemReadModel>> GetSystemByNameAsync(string name, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, IReadOnlyList<AISystemReadModel>>> GetAllSystemsAsync(CancellationToken ct)`
   - `ValueTask<Either<EncinaError, IReadOnlyList<AISystemReadModel>>> GetSystemsByRiskLevelAsync(AIRiskLevel riskLevel, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, Unit>> ReclassifySystemAsync(Guid systemId, AIRiskLevel newRiskLevel, string reason, string reclassifiedBy, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, Unit>> DecommissionSystemAsync(Guid systemId, string reason, string decommissionedBy, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, bool>> IsRegisteredAsync(string systemId, CancellationToken ct)`

2. **Create `src/Encina.Compliance.AIAct/Abstractions/IHumanOversightService.cs`**
   - `ValueTask<Either<EncinaError, HumanOversightReadModel>> RequestReviewAsync(string systemId, string requestType, string? correlationId, string requestedBy, string? tenantId, string? moduleId, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, Unit>> RecordDecisionAsync(Guid sessionId, string reviewerId, string decision, string rationale, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, Unit>> EscalateAsync(Guid sessionId, string escalatedBy, string reason, string escalatedTo, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, Unit>> OverrideAsync(Guid sessionId, string overriddenBy, string newDecision, string reason, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, HumanOversightReadModel>> GetSessionAsync(Guid sessionId, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, bool>> HasApprovalAsync(string systemId, string? correlationId, CancellationToken ct)`

3. **Create `src/Encina.Compliance.AIAct/Services/DefaultAISystemService.cs`**
   - Dependencies: `IAggregateRepository<AISystemAggregate>`, `IReadModelRepository<AISystemReadModel>`, `ICacheProvider`, `TimeProvider`, `ILogger<DefaultAISystemService>`
   - Write ops: Load aggregate → call command → save → invalidate cache
   - Read ops: Try cache → fallback to read model repository
   - Cache keys: `"aiact:system:{id}"`, `"aiact:system:name:{name}"`
   - Cache TTL: 5 minutes

4. **Create `src/Encina.Compliance.AIAct/Services/DefaultHumanOversightService.cs`**
   - Dependencies: `IAggregateRepository<HumanOversightAggregate>`, `IReadModelRepository<HumanOversightReadModel>`, `ICacheProvider`, `TimeProvider`, `ILogger<DefaultHumanOversightService>`
   - Cache keys: `"aiact:oversight:{sessionId}"`, `"aiact:oversight:system:{systemId}:latest"`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of Encina.Compliance.AIAct Marten ES Migration.

CONTEXT:
- Phase 1 created aggregates and events. Phase 2 created read models and projections.
- Now create the CQRS service layer that wraps aggregate operations.
- Services handle: aggregate load/save, cache invalidation, error mapping to Either<EncinaError, T>.

TASK:
Create 2 service interfaces (IAISystemService, IHumanOversightService) and 2 implementations.

KEY RULES:
- All methods return ValueTask<Either<EncinaError, T>> (ROP)
- Use ICacheProvider for read model caching (5-min TTL)
- Fire-and-forget cache invalidation on mutations (don't await)
- Services are Scoped lifetime (new instance per request)
- Use TimeProvider for UTC timestamps (testable)
- Log structured messages via ILogger<T>
- Handle InvalidOperationException from aggregates → map to EncinaError

REFERENCE FILES:
- src/Encina.Compliance.Consent/Services/DefaultConsentService.cs
- src/Encina.Compliance.CrossBorderTransfer/Services/DefaultApprovedTransferService.cs
- src/Encina.Compliance.Consent/Abstractions/IConsentService.cs
```

</details>

---

### Phase 4: Marten Registration & DI Update

> **Goal**: Wire up Marten aggregate repositories, projections, and update DI registration to use services.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create `src/Encina.Compliance.AIAct/AIActMartenExtensions.cs`**
   - `AddAIActAggregates(this IServiceCollection services)` → registers:
     - `AddAggregateRepository<AISystemAggregate>()`
     - `AddAggregateRepository<HumanOversightAggregate>()`
     - `AddProjection<AISystemProjection, AISystemReadModel>()`
     - `AddProjection<HumanOversightProjection, HumanOversightReadModel>()`

2. **Modify `src/Encina.Compliance.AIAct/ServiceCollectionExtensions.cs`**
   - Add `TryAddScoped<IAISystemService, DefaultAISystemService>()`
   - Add `TryAddScoped<IHumanOversightService, DefaultHumanOversightService>()`
   - Keep existing registrations (pipeline behavior, classifier, validator, etc.)
   - The existing `IAISystemRegistry` and `IHumanOversightEnforcer` remain for backward compatibility — services override them when Marten is configured

3. **Update `src/Encina.Compliance.AIAct/AIActCompliancePipelineBehavior.cs`**
   - Add optional `IAISystemService?` dependency (resolved via DI, null if Marten not configured)
   - When `IAISystemService` is available, use it for system lookups instead of `IAIActComplianceValidator`
   - Fallback to existing validator when service is not registered (in-memory mode)

4. **Update `src/Encina.Compliance.AIAct/Health/AIActHealthCheck.cs`**
   - When `IAISystemService` is registered, use it to check system count
   - Add check for Marten connectivity (aggregate repository resolution)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of Encina.Compliance.AIAct Marten ES Migration.

CONTEXT:
- Phases 1-3 created aggregates, events, projections, read models, and services.
- Now wire up Marten registration and update existing DI to integrate the new services.
- The pattern is: AddEncinaAIAct() registers defaults (TryAdd), AddAIActAggregates() registers Marten.

TASK:
1. Create AIActMartenExtensions.cs with AddAIActAggregates()
2. Update ServiceCollectionExtensions.cs to register new services
3. Update pipeline behavior to use IAISystemService when available
4. Update health check for Marten integration

KEY RULES:
- Use TryAdd semantics everywhere (allow override)
- Marten registration is a separate method (opt-in)
- Pipeline behavior must work in both modes (in-memory and Marten)
- Keep existing InMemoryAISystemRegistry for testing scenarios

REFERENCE FILES:
- src/Encina.Compliance.Consent/ConsentMartenExtensions.cs
- src/Encina.Compliance.Consent/ServiceCollectionExtensions.cs
```

</details>

---

### Phase 5: Cross-Cutting Integration

> **Goal**: Integrate TenantId, ModuleId, distributed locks, and audit trail with event-sourced aggregates.

<details>
<summary><strong>Tasks</strong></summary>

1. **TenantId/ModuleId propagation**
   - All events already include `TenantId?` and `ModuleId?` (from Phase 1)
   - Read models include both fields (from Phase 2)
   - Services propagate from caller parameters through aggregate factory methods

2. **Idempotency**
   - Marten event streams are naturally idempotent (aggregate version prevents duplicate events)
   - Projections are idempotent by design (applying same event twice = same result)

3. **Transactions**
   - Marten session provides inherent transaction (events + projections committed atomically)

4. **Resilience** — Deferred to separate issue
   - Marten PostgreSQL calls may fail — Polly retry/circuit breaker to be added later
   - Create GitHub issue: "[DEBT] Add resilience policies to AIAct Marten operations"

5. **Distributed Locks** — Not applicable
   - Marten uses optimistic concurrency (aggregate version) instead of distributed locks

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of Encina.Compliance.AIAct Marten ES Migration.

CONTEXT:
- All aggregates, events, projections, services, and DI are implemented.
- Now verify cross-cutting integration is complete.

TASK:
1. Verify TenantId/ModuleId propagation through all events and read models
2. Verify idempotency of projections
3. Document deferred items (resilience) and create GitHub issue if needed

KEY RULES:
- TenantId and ModuleId must flow: caller → service → aggregate → event → projection → read model
- No distributed locks needed (Marten optimistic concurrency handles this)
- Resilience deferred to separate issue

REFERENCE FILES:
- src/Encina.Compliance.Consent/Aggregates/ConsentAggregate.cs (TenantId pattern)
```

</details>

---

### Phase 6: Observability Updates

> **Goal**: Extend existing observability to cover aggregate operations, service layer, and Marten interactions.

<details>
<summary><strong>Tasks</strong></summary>

1. **Update `src/Encina.Compliance.AIAct/Diagnostics/AIActDiagnostics.cs`**
   - Add new Activity names: `"aiact.system.register"`, `"aiact.system.reclassify"`, `"aiact.system.decommission"`, `"aiact.oversight.request"`, `"aiact.oversight.decide"`, `"aiact.oversight.escalate"`, `"aiact.oversight.override"`
   - Add new metric counters: `aiact.system.registered`, `aiact.system.reclassified`, `aiact.system.decommissioned`, `aiact.oversight.requested`, `aiact.oversight.decided`, `aiact.oversight.escalated`

2. **Update `src/Encina.Compliance.AIAct/Diagnostics/AIActLogMessages.cs`**
   - Add new log messages for service operations (EventIds 9540-9570, within existing 9500-9599 range):
     - 9540: System registered
     - 9541: System reclassified
     - 9542: System decommissioned
     - 9543: System lookup (cache hit/miss)
     - 9550: Oversight review requested
     - 9551: Oversight decision recorded
     - 9552: Oversight escalated
     - 9553: Oversight overridden
     - 9560: Marten aggregate save succeeded
     - 9561: Marten aggregate save failed

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of Encina.Compliance.AIAct Marten ES Migration.

CONTEXT:
- EventId range 9500-9599 is registered for AIAct. Current usage: 9500-9530 (pipeline/classification).
- Now add EventIds 9540-9570 for service layer and Marten operations.

TASK:
Update existing diagnostics files with new activities, metrics, and log messages.

KEY RULES:
- Use [LoggerMessage] source generator (not LoggerMessage.Define)
- EventIds within registered range 9500-9599
- Pack sequentially (9540, 9541, 9542...) — no sparse allocation
- ActivitySource name: "Encina.Compliance.AIAct" (already exists)
- Meter name: "Encina.Compliance.AIAct" (already exists)

REFERENCE FILES:
- src/Encina.Compliance.AIAct/Diagnostics/AIActDiagnostics.cs (existing)
- src/Encina.Compliance.AIAct/Diagnostics/AIActLogMessages.cs (existing)
- src/Encina/Diagnostics/EventIdRanges.cs (range: 9500-9599)
```

</details>

---

### Phase 7: Testing

> **Goal**: Comprehensive test coverage for aggregates, events, projections, services, and updated pipeline behavior.

<details>
<summary><strong>Tasks</strong></summary>

1. **Unit Tests** (`tests/Encina.UnitTests/Compliance/AIAct/`)
   - `Aggregates/AISystemAggregateTests.cs` — Register, Reclassify, Decommission, invariant violations
   - `Aggregates/HumanOversightAggregateTests.cs` — RequestReview, RecordDecision, Escalate, Override, invariant violations
   - `ReadModels/AISystemProjectionTests.cs` — Create from event, apply reclassification, apply decommission
   - `ReadModels/HumanOversightProjectionTests.cs` — Create from event, apply decision, apply escalation, apply override
   - `Services/DefaultAISystemServiceTests.cs` — Mock IAggregateRepository, verify cache invalidation
   - `Services/DefaultHumanOversightServiceTests.cs` — Mock IAggregateRepository, verify cache invalidation
   - Update `AIActCompliancePipelineBehaviorTests.cs` — Add tests for service-backed mode

2. **Guard Tests** (`tests/Encina.GuardTests/Compliance/AIAct/`)
   - Update `AIActGuardTests.cs` — Add guard tests for new service methods, aggregate constructors

3. **Contract Tests** (`tests/Encina.ContractTests/Compliance/AIAct/`)
   - Update `AIActContractTests.cs` — Add IAISystemService and IHumanOversightService contract tests

4. **Property Tests** (`tests/Encina.PropertyTests/Compliance/AIAct/`)
   - Update `AIActPropertyTests.cs` — Add aggregate invariant properties:
     - Register-then-get round-trip
     - Reclassification always changes risk level
     - Decommissioned system cannot be reclassified
     - Human decision always has rationale

5. **Integration Tests** (`tests/Encina.IntegrationTests/Compliance/AIAct/`)
   - `AIActMartenIntegrationTests.cs` — Full round-trip with real Marten/PostgreSQL:
     - Register system → query read model → reclassify → verify projection updated
     - Request review → record decision → verify audit trail
   - Use `[Collection("Marten-PostgreSQL")]` shared fixture
   - Requires Docker PostgreSQL container

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of Encina.Compliance.AIAct Marten ES Migration.

CONTEXT:
- All production code is complete. Now create comprehensive tests.
- Existing AIAct tests: 88 unit, 23 guard, 13 contract, 11 property (all pass).
- Add tests for new aggregates, projections, services.

TASK:
1. Unit tests for both aggregates (invariant enforcement, Apply() correctness)
2. Unit tests for both projections (event → read model transformation)
3. Unit tests for both services (mock repository, verify cache invalidation)
4. Guard tests for new public methods
5. Contract tests for new service interfaces
6. Property tests for aggregate invariants (FsCheck)
7. Integration tests with real Marten/PostgreSQL (via Docker)

KEY RULES:
- Unit tests: AAA pattern, NSubstitute mocks, one assert per test
- Guard tests: ArgumentNullException with Shouldly
- Contract tests: interface compliance with Shouldly
- Property tests: FsCheck [Property(MaxTest = 100)]
- Integration tests: [Collection("Marten-PostgreSQL")] shared fixture
- No [Collection] fixtures for unit/guard/contract/property tests (no database)

REFERENCE FILES:
- tests/Encina.UnitTests/Compliance/AIAct/ (existing AIAct tests)
- tests/Encina.UnitTests/Compliance/GDPR/ (GDPR test patterns)
- tests/Encina.IntegrationTests/Compliance/ (Marten integration patterns)
```

</details>

---

### Phase 8: Documentation & Finalization

> **Goal**: Update all documentation, verify build and tests, ensure PublicAPI tracking.

<details>
<summary><strong>Tasks</strong></summary>

1. **XML doc comments** on all new public APIs (aggregates, events, read models, services, projections)

2. **Update `CHANGELOG.md`** — Add entry under Unreleased/Changed:
   - "Encina.Compliance.AIAct — Migrated to Marten Event Sourcing"

3. **Update `ROADMAP.md`** — Note ES migration under v0.13.0

4. **Update `src/Encina.Compliance.AIAct/README.md`** — Add Marten ES section

5. **Update `docs/features/aiact-compliance.md`** — Add Event Sourcing section

6. **Create `docs/architecture/adr/022-aiact-marten-event-sourcing.md`** — ADR documenting:
   - Decision: Migrate AIAct from in-memory to Marten ES
   - Context: Consistency with ADR-019, regulatory audit trail requirements
   - Consequences: PostgreSQL dependency for compliance, improved auditability

7. **Update `src/Encina.Compliance.AIAct/PublicAPI.Unshipped.txt`** — Add all new public symbols

8. **Update `docs/INVENTORY.md`** — Note Marten ES support

9. **Build verification**: `dotnet build --configuration Release` → 0 errors, 0 warnings

10. **Test verification**: `dotnet test` → all pass, coverage ≥ 85%

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 of Encina.Compliance.AIAct Marten ES Migration.

CONTEXT:
- All production code and tests are complete.
- Now update documentation, verify build, ensure everything is tracked.

TASK:
1. XML doc comments on ALL new public APIs
2. Update CHANGELOG.md, ROADMAP.md, README.md, docs/features/aiact-compliance.md
3. Create ADR-022 for the Marten ES migration decision
4. Update PublicAPI.Unshipped.txt with all new public symbols
5. Build and test verification

KEY RULES:
- No AI attribution in commits
- English only in code, comments, docs
- ADR follows existing format in docs/architecture/adr/

REFERENCE FILES:
- docs/architecture/adr/019-compliance-event-sourcing-marten.md (ADR pattern)
- CHANGELOG.md (entry format)
- docs/features/nis2-compliance.md (feature doc pattern)
```

</details>

---

## Research

### EU AI Act Articles Requiring Persistent Audit Trail

| Article | Requirement | Aggregate | Event Type |
|---------|-------------|-----------|------------|
| Art. 5 | Prohibited practices must be blocked and logged | AISystemAggregate | AISystemComplianceEvaluated |
| Art. 6(3) | Risk reclassification must be documented | AISystemAggregate | AISystemReclassified |
| Art. 12 | Automatic logging of AI system operations | Both | All events (event stream = log) |
| Art. 14 | Human oversight decisions must be recorded | HumanOversightAggregate | HumanDecisionRecorded, Escalated, Overridden |
| Art. 51 | AI providers must maintain system registration | AISystemAggregate | AISystemRegistered, Decommissioned |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in This Feature |
|-----------|----------|----------------------|
| `AggregateBase` | `Encina.DomainModeling` | Base class for both aggregates |
| `IAggregateRepository<T>` | `Encina.Marten` | Persist/load aggregates |
| `IReadModelRepository<T>` | `Encina.Marten` | Query read models |
| `IProjection<T>` | `Encina.Marten` | Inline projection interface |
| `IProjectionCreator<T1,T2>` | `Encina.Marten` | Create read model from first event |
| `IProjectionHandler<T1,T2>` | `Encina.Marten` | Update read model from subsequent events |
| `ICacheProvider` | `Encina.Caching` | Cache read models |
| `EventPublishingPipelineBehavior` | `Encina.Marten` | Auto-publish events as INotification |
| `ProjectionContext` | `Encina.Marten` | Event metadata in projections |

### Event ID Allocation

| Package | Range | Current Usage | Notes |
|---------|-------|---------------|-------|
| `Encina.Compliance.AIAct` | 9500-9599 | 9500-9530 (pipeline) | 9540-9570 for services (Phase 6) |

### Estimated File Count

| Category | New Files | Modified Files | Total |
|----------|-----------|----------------|-------|
| Events | 2 | 0 | 2 |
| Aggregates | 2 | 0 | 2 |
| Read Models | 2 | 0 | 2 |
| Projections | 2 | 0 | 2 |
| Services | 4 (2 interfaces + 2 impl) | 0 | 4 |
| Marten Registration | 1 | 0 | 1 |
| Model (enums) | 2 | 0 | 2 |
| DI / Pipeline / Health | 0 | 3 | 3 |
| Diagnostics | 0 | 2 | 2 |
| Documentation | 1 (ADR) | 5 | 6 |
| Tests (new) | ~15 | ~4 | ~19 |
| PublicAPI | 0 | 1 | 1 |
| **Total** | **~31** | **~15** | **~46** |

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | **Caching** | ✅ Included | ICacheProvider for read model queries (5-min TTL, fire-and-forget invalidation) |
| 2 | **OpenTelemetry** | ✅ Included | Extend existing ActivitySource/Meter with service-level activities and counters |
| 3 | **Structured Logging** | ✅ Included | EventIds 9540-9570 via [LoggerMessage] source generator |
| 4 | **Health Checks** | ✅ Included | Update AIActHealthCheck to verify Marten connectivity and aggregate repository resolution |
| 5 | **Validation** | ✅ Included | Aggregate invariants enforce domain rules (invalid state transitions throw) |
| 6 | **Resilience** | ⏭️ Deferred | Marten PostgreSQL calls need Polly retry — create GitHub issue |
| 7 | **Distributed Locks** | ❌ N/A | Marten optimistic concurrency (aggregate version) replaces distributed locks |
| 8 | **Transactions** | ✅ Included | Marten session provides inherent transaction (events + projections in single commit) |
| 9 | **Idempotency** | ✅ Included | Event streams naturally deduplicate; projections are idempotent by design |
| 10 | **Multi-Tenancy** | ✅ Included | TenantId on all events, aggregates, and read models ([#845](https://github.com/dlrivada/Encina/issues/845)) |
| 11 | **Module Isolation** | ✅ Included | ModuleId on all events, aggregates, and read models ([#846](https://github.com/dlrivada/Encina/issues/846)) |
| 12 | **Audit Trail** | ✅ Included | Event stream IS the audit trail — inherent with Marten ES |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are migrating Encina.Compliance.AIAct from in-memory persistence to Marten event sourcing.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library using Railway Oriented Programming (Either<EncinaError, T>)
- The AIAct package (src/Encina.Compliance.AIAct/) is a stateless compliance engine already implemented
- Currently uses ConcurrentDictionary for AI system registry and human decisions — data lost on restart
- Per ADR-019 (docs/architecture/adr/019-compliance-event-sourcing-marten.md), all stateful compliance modules use Marten ES
- 9 other compliance modules have been migrated: Consent, DPIA, BreachNotification, CrossBorderTransfer, etc.

IMPLEMENTATION OVERVIEW:
1. Phase 1: Domain Events & Aggregates (AISystemAggregate + HumanOversightAggregate)
2. Phase 2: Read Models & Inline Projections
3. Phase 3: Service Layer (IAISystemService + IHumanOversightService)
4. Phase 4: Marten Registration & DI Update
5. Phase 5: Cross-Cutting Integration (TenantId, ModuleId, idempotency)
6. Phase 6: Observability Updates (EventIds 9540-9570)
7. Phase 7: Testing (unit, guard, contract, property, integration)
8. Phase 8: Documentation & Finalization

KEY PATTERNS:
- Aggregates extend AggregateBase, use RaiseEvent() + Apply() pattern
- Events are sealed records implementing INotification
- Read models implement IReadModel with mutable setters
- Projections implement IProjection<T> + IProjectionCreator + IProjectionHandler
- Services wrap IAggregateRepository + IReadModelRepository with ICacheProvider
- Marten registration via separate AddAIActAggregates() extension method
- TryAdd semantics everywhere for override flexibility

REFERENCE FILES:
- src/Encina.Compliance.Consent/ (complete ES compliance module)
- src/Encina.Compliance.CrossBorderTransfer/ (multi-aggregate reference)
- src/Encina.Compliance.BreachNotification/ (lifecycle with timeline)
- src/Encina.Compliance.DPIA/ (assessment workflow)
- src/Encina.Marten/ (base ES infrastructure)
- docs/architecture/adr/019-compliance-event-sourcing-marten.md (architectural decision)
```

</details>

---

## Future Aggregates

The following aggregates are identified as future extensions based on open child issues of #415. They are **not part of this migration** but should follow the same Marten ES patterns established here.

| Issue | Aggregate | Lifecycle States | EU AI Act Articles |
|-------|-----------|-----------------|-------------------|
| [#836](https://github.com/dlrivada/Encina/issues/836) | `RiskAssessmentAggregate` | Initiated → Evaluated → Approved/Rejected → Reassessed | Art. 6 (risk classification), Art. 9 (risk management) |
| [#837](https://github.com/dlrivada/Encina/issues/837) | `DatasetGovernanceAggregate` | Registered → Validated → BiasAssessed → Certified/Rejected | Art. 10 (data governance), Art. 15 (accuracy) |
| [#844](https://github.com/dlrivada/Encina/issues/844) | `ConformityAssessmentAggregate` | Initiated → Documented → Reviewed → Certified/Failed → Renewed | Art. 43 (conformity assessment), Art. 49 (CE marking) |

**Implementation notes:**

- Each aggregate follows `AggregateBase` with `RaiseEvent()` + `Apply()` pattern
- Events implement `INotification` for auto-publishing
- Inline projections to read models with `IProjection<T>`
- Service layer with `IAggregateRepository<T>` + `IReadModelRepository<T>` + `ICacheProvider`
- TenantId/ModuleId propagation on all events and read models
- EventIds within the registered 9500-9599 range (allocate remaining slots 9570-9599)

---

## Next Steps

1. **Review and approve this plan**
2. Create GitHub issue for the migration (child of #415)
3. Begin Phase 1 implementation
4. Each phase should be a self-contained commit
5. Final commit references the new issue
