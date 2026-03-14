# Implementation Plan: `Encina.Compliance.CrossBorderTransfer` — International Data Transfers (Schrems II)

> **Issue**: [#412](https://github.com/dlrivada/Encina/issues/412)
> **Type**: Feature
> **Complexity**: High (10 phases, Marten event sourcing, ~65-80 files)
> **Estimated Scope**: ~4,500-6,000 lines of production code + ~3,000-4,000 lines of tests
> **Prerequisites**: [#776](https://github.com/dlrivada/Encina/issues/776) (ADR-019: Compliance ES Strategy with Marten)

---

## Summary

Implement international data transfer validation for GDPR Articles 44-49 and the Schrems II judgment (CJEU C-311/18). This package provides Transfer Impact Assessments (TIA), Standard Contractual Clauses (SCC) validation with module tracking, adequacy decision registry, supplementary measures management, and a `TransferBlockingPipelineBehavior` that enforces transfer compliance at the request pipeline level.

The implementation uses **Marten event sourcing** for all stateful entities (TIA, SCC Agreements, Approved Transfers), providing an immutable audit trail for GDPR Art. 5(2) accountability. Marten is a specialized event sourcing provider (PostgreSQL-backed), analogous to how Redis is specialized for caching. This is the first compliance module to establish the Marten ES pattern; subsequent modules (#777-#784) will follow the same architecture.

The implementation **builds on top of `Encina.Compliance.DataResidency`**, which already provides `Region`, `RegionRegistry`, `TransferLegalBasis`, `TransferValidationResult`, `ICrossBorderTransferValidator`, and `IAdequacyDecisionProvider`. This module adds the **higher-level orchestration layer**: TIA workflows, SCC lifecycle management, supplementary measures tracking, and approved transfer registries — the operational compliance layer that Schrems II demands beyond basic region validation.

**Affected packages**:
- `Encina.Compliance.CrossBorderTransfer` (new)
- References: `Encina` (core), `Encina.DomainModeling` (IAggregate, AggregateBase), `Encina.Compliance.DataResidency` (Region, TransferLegalBasis, IAdequacyDecisionProvider), `Encina.Caching` (ICacheProvider)
- Integration: `Encina.Marten` (IAggregateRepository, projections, snapshots)

**Provider category**: Event Sourcing (Marten/PostgreSQL) — specialized provider, analogous to Redis for caching. Not subject to the 13-database-provider rule.

---

## Design Choices

<details>
<summary><strong>1. Package Placement — New <code>Encina.Compliance.CrossBorderTransfer</code> package that extends DataResidency</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New `Encina.Compliance.CrossBorderTransfer` package** | Clean separation, own pipeline behavior, own observability, reuses DataResidency types | New NuGet package, dependency on DataResidency |
| **B) Extend `Encina.Compliance.DataResidency`** | Single package, no additional dependency | Bloats DataResidency (~30 files already), TIA/SCC are distinct operational concerns |
| **C) Add to `Encina.Compliance.GDPR`** | Centralized compliance | GDPR core is already large, Art. 44-49 is a specialized domain |

### Chosen Option: **A — New `Encina.Compliance.CrossBorderTransfer` package**

### Rationale

- DataResidency already provides the foundational layer: `Region`, `RegionRegistry`, `TransferLegalBasis`, `TransferValidationResult`, `ICrossBorderTransferValidator`, `IAdequacyDecisionProvider`
- CrossBorderTransfer adds the **operational/workflow layer**: TIA management, SCC tracking, supplementary measures, approved transfer registries
- Separation of concerns: DataResidency = "where can data go?", CrossBorderTransfer = "what mechanisms are in place for international transfers?"
- Takes a `ProjectReference` to `Encina.Compliance.DataResidency` to reuse types, avoiding duplication
- Follows the established pattern: each GDPR concern area gets its own package

</details>

<details>
<summary><strong>2. Domain Model — Event-sourced aggregates with Marten</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Entity-based model with in-memory stores** | Simple, no PostgreSQL dependency, overridable via TryAdd | No immutable audit trail, current-state only, cannot prove compliance history |
| **B) Entity-based model with 13 database providers** | Persistence across all providers, consistent with other modules | No immutable audit trail, enormous scope (13 provider implementations), state mutations lose history |
| **C) Event-sourced model with Marten** | Immutable audit trail, full state reconstruction, GDPR Art. 5(2) accountability built-in, leverages existing mature infrastructure | Requires PostgreSQL, additional dependency on Encina.Marten |

### Chosen Option: **C — Event-sourced model with Marten**

### Rationale

- **GDPR Art. 5(2) accountability**: Compliance modules must be able to **prove** compliance, not just assert it. An event-sourced model records every state change as an immutable event with timestamp, userId, and full context.
- **Marten as specialized provider**: Just as Redis is specialized for caching and RabbitMQ for messaging, Marten is specialized for event sourcing. The 13-database-provider rule applies to CRUD features, not to specialized infrastructure. Implementing event sourcing on top of SQLite/MySQL/SQL Server would be reinventing Marten.
- **Existing infrastructure**: `Encina.Marten` already provides `IAggregateRepository<T>`, snapshots, projections, event versioning, health checks, and observability — all production-ready.
- **Domain fit**: TIA has complex lifecycle (Draft → InProgress → PendingDPOReview → Completed/Expired). SCC Agreements have lifecycle (Registered → Active → Revoked/Expired). Approved Transfers have lifecycle (Approved → Revoked/Expired/Renewed). Event sourcing captures every transition.
- **Three aggregates**:
  - `TIAAggregate` (extends `AggregateBase`): Transfer Impact Assessment lifecycle
  - `SCCAgreementAggregate` (extends `AggregateBase`): SCC agreement lifecycle
  - `ApprovedTransferAggregate` (extends `AggregateBase`): Approved transfer lifecycle
- **Projections**: Marten inline projections build read models (`TIAReadModel`, `SCCAgreementReadModel`, `ApprovedTransferReadModel`) for efficient queries.
- **Architectural precedent**: This is the first compliance module to use Marten ES. ADR-019 (#776) documents this architectural decision. Existing compliance modules (Consent, DSR, etc.) will be refactored to follow this pattern (#777-#784).

</details>

<details>
<summary><strong>3. Pipeline Behavior — Transfer validation with attribute-driven enforcement</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `TransferBlockingPipelineBehavior` with `[RequiresCrossBorderTransfer]` attribute** | Declarative, cached, consistent with other compliance behaviors | Attribute per request type |
| **B) Automatic detection via `IRegionContextProvider`** | Zero attributes needed | Implicit, hard to reason about, performance concerns |
| **C) Manual validation calls only** | Simple, explicit | No pipeline enforcement, easy to forget |

### Chosen Option: **A — Attribute-driven pipeline behavior**

### Rationale

- `[RequiresCrossBorderTransfer(Destination = "US", DataCategory = "personal-data")]` on request types marks them for transfer validation
- Pipeline behavior checks: (1) attribute present? (2) approved transfer exists? (3) TIA completed if required? (4) SCC valid if basis is SCCs?
- Three-mode enforcement: Block/Warn/Disabled, consistent with all other compliance behaviors
- Attribute cached in static `ConcurrentDictionary<Type, RequiresCrossBorderTransferAttribute?>` — zero reflection after first access
- Auto-detection mode (`options.AutoDetectTransfers = true`) can use `IRegionContextProvider` from DataResidency as a complement

</details>

<details>
<summary><strong>4. SCC Module Design — Enum-based with 4 modules matching EU 2021 SCCs</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Enum with 4 modules** | Type-safe, matches EU spec exactly, simple | Cannot extend for future SCC revisions |
| **B) String-based module identifier** | Extensible, future-proof | No compile-time safety, inconsistent naming |
| **C) Class hierarchy per module** | Rich behavior per module | Over-engineered for what's essentially a label |

### Chosen Option: **A — Enum with 4 modules**

### Rationale

- The EU 2021 SCCs define exactly 4 modules: Controller→Controller, Controller→Processor, Processor→Processor, Processor→Controller
- Enum values match the issue specification exactly
- If simplified SCCs arrive (expected Q2 2025), the enum can be extended without breaking changes (pre-1.0)
- Each module has specific clause requirements that can be validated (e.g., Module 2 requires sub-processor authorization)

</details>

<details>
<summary><strong>5. TIA Risk Scoring — Numeric score with threshold-based assessment</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Numeric risk score (0.0-1.0) with configurable threshold** | Quantifiable, threshold-configurable, consistent with DPIA scoring | Subjective inputs |
| **B) Categorical risk levels (Low/Medium/High/Critical)** | Simple to understand | No granularity for threshold decisions |
| **C) Multi-dimensional risk matrix** | Detailed analysis, per-factor scoring | Complex, over-engineered for pre-1.0 |

### Chosen Option: **A — Numeric risk score with threshold**

### Rationale

- TIA evaluates: (1) destination country legal framework, (2) data category sensitivity, (3) available safeguards, (4) government access risk
- Score 0.0 = minimal risk (adequacy decision country), 1.0 = maximum risk (no legal protections)
- `options.TIARiskThreshold = 0.6` — transfers with risk score above threshold require supplementary measures or are blocked
- Matches DPIA risk scoring pattern (`DPIARiskScore`) for consistency
- Risk factors are pluggable via `ITIARiskAssessor` strategy interface

</details>

<details>
<summary><strong>6. Supplementary Measures — Registry pattern with categorized measures</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Registry of typed supplementary measures** | Structured, categorized (technical/contractual/organizational), validatable | More types |
| **B) Simple string list** | Minimal code | No categorization, no validation, no audit trail |
| **C) Policy-based with enforcement** | Automatic enforcement | Too opinionated, hard to map to real-world measures |

### Chosen Option: **A — Registry pattern**

### Rationale

- EDPB Recommendations 01/2020 categorize supplementary measures into 3 types: Technical (encryption, pseudonymization), Contractual (transparency obligations, audit rights), and Organizational (policies, impact assessments)
- `SupplementaryMeasure` record with `Type`, `Description`, `IsImplemented`, `ImplementedAtUtc`
- Registry allows tracking which measures have been implemented per transfer route
- Pipeline behavior can verify that all required measures are marked as implemented before allowing transfer

</details>

<details>
<summary><strong>7. Caching Strategy — ICacheProvider from Encina.Caching</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IMemoryCache` for projection read models** | Standard .NET, no additional dependency | Single-node only, no distributed invalidation, no tag-based invalidation, no stampede protection |
| **B) `ICacheProvider` from Encina.Caching** | Full caching infrastructure (8 providers), distributed invalidation, tag-based invalidation, stampede protection, fail-safe, eager refresh | Additional package dependency |
| **C) No caching** | Simplest | Every pipeline validation requires aggregate reload from event store |

### Chosen Option: **B — `ICacheProvider` from Encina.Caching**

### Rationale

- The pipeline behavior is a hot path — every request with `[RequiresCrossBorderTransfer]` triggers a transfer validation
- Approved transfers change infrequently; caching read models with short TTL (5 min default, configurable) is highly effective
- `ICacheProvider` from `Encina.Caching` provides the full caching infrastructure that Encina already supports: 8 providers (Memory, Hybrid, Redis, Valkey, Dragonfly, Garnet, KeyDB, Memcached), tag-based invalidation, stampede protection, fail-safe patterns, and eager refresh
- Tag-based invalidation is especially useful: when a TIA aggregate is updated, invalidate all cache entries tagged `"tia:{id}"` in one operation
- In distributed deployments (multiple app instances), `ICacheProvider` with a distributed backend (Redis, etc.) ensures cache coherence across nodes — critical for compliance correctness
- Write-through pattern: cache invalidated when aggregate commands are processed
- Consistent with Encina philosophy: use the framework's own infrastructure, not lower-level .NET primitives

</details>

---

## Implementation Phases

### Phase 1: Core Models & Enums

<details>
<summary>Tasks</summary>

1. **Create project structure** `src/Encina.Compliance.CrossBorderTransfer/`
   - `Encina.Compliance.CrossBorderTransfer.csproj` — .NET 10, refs `Encina`, `Encina.DomainModeling`, `Encina.Compliance.DataResidency`, `Encina.Caching`
   - `PublicAPI.Shipped.txt` (empty), `PublicAPI.Unshipped.txt` (empty)

2. **`Model/SCCModule.cs`** — `Encina.Compliance.CrossBorderTransfer.Model`
   - Enum: `ControllerToController = 0`, `ControllerToProcessor = 1`, `ProcessorToProcessor = 2`, `ProcessorToController = 3`

3. **`Model/SupplementaryMeasureType.cs`** — `Encina.Compliance.CrossBorderTransfer.Model`
   - Enum: `Technical = 0`, `Contractual = 1`, `Organizational = 2`

4. **`Model/TransferBasis.cs`** — `Encina.Compliance.CrossBorderTransfer.Model`
   - Enum: `AdequacyDecision = 0`, `SCCs = 1`, `BindingCorporateRules = 2`, `Derogation = 3`, `Blocked = 4`

5. **`Model/TIAStatus.cs`** — `Encina.Compliance.CrossBorderTransfer.Model`
   - Enum: `Draft = 0`, `InProgress = 1`, `PendingDPOReview = 2`, `Completed = 3`, `Expired = 4`

6. **`Model/SupplementaryMeasure.cs`** — `Encina.Compliance.CrossBorderTransfer.Model`
   - Sealed record with: `Id` (Guid), `Type` (SupplementaryMeasureType), `Description` (string), `IsImplemented` (bool), `ImplementedAtUtc` (DateTimeOffset?)

7. **`Model/TransferValidationOutcome.cs`** — `Encina.Compliance.CrossBorderTransfer.Model`
   - Sealed record with: `IsAllowed` (bool), `Basis` (TransferBasis), `SupplementaryMeasuresRequired` (IReadOnlyList\<string>), `TIARequired` (bool), `BlockReason` (string?), `SCCModuleRequired` (SCCModule?), `Warnings` (IReadOnlyList\<string>)
   - Factory methods: `Allow(TransferBasis, ...)`, `Block(string reason)`

8. **`Model/TransferRequest.cs`** — `Encina.Compliance.CrossBorderTransfer.Model`
   - Sealed record: `SourceCountryCode` (string), `DestinationCountryCode` (string), `DataCategory` (string), `ProcessorId` (string?), `TenantId` (string?), `ModuleId` (string?)

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```
CONTEXT:
You are implementing Phase 1 of Encina.Compliance.CrossBorderTransfer (Issue #412).
This is a new compliance package for GDPR Articles 44-49 (Schrems II) international data transfer validation.
The package builds on Encina.Compliance.DataResidency which already provides Region, RegionRegistry, TransferLegalBasis.
This module uses Marten event sourcing — but Phase 1 only creates value objects and enums (no aggregates yet).

TASK:
1. Create the project at src/Encina.Compliance.CrossBorderTransfer/
2. Create .csproj with .NET 10, ProjectReference to Encina, Encina.DomainModeling, Encina.Compliance.DataResidency, Encina.Caching
3. Include PublicAPI.Shipped.txt (empty), PublicAPI.Unshipped.txt (empty)
4. Add InternalsVisibleTo for Encina.UnitTests, Encina.IntegrationTests, Encina.PropertyTests, Encina.ContractTests, Encina.GuardTests
5. Create all Model files: SCCModule, SupplementaryMeasureType, TransferBasis, TIAStatus, SupplementaryMeasure, TransferValidationOutcome, TransferRequest

KEY RULES:
- .NET 10 / C# 14, nullable enabled
- All records are sealed with required properties
- All timestamp properties use DateTimeOffset with AtUtc suffix
- Include TenantId/ModuleId on entities that are stored (TransferRequest has them for pipeline context)
- Full XML documentation on all public types and members
- Do NOT create aggregates or events yet — those are Phase 2

REFERENCE FILES:
- src/Encina.Compliance.DataResidency/Model/ — for Region, TransferLegalBasis patterns
- src/Encina.Compliance.Consent/Model/ — for ConsentRecord sealed record pattern
- src/Encina.Compliance.DPIA/Model/ — for DPIARiskScore pattern
```

</details>

### Phase 2: Event-Sourced Aggregates & Domain Events

<details>
<summary>Tasks</summary>

1. **`Events/TIAEvents.cs`** — `Encina.Compliance.CrossBorderTransfer.Events`
   - All events are sealed records. Each event represents an immutable fact.
   - `TIACreated` { TIAId, SourceCountryCode, DestinationCountryCode, DataCategory, CreatedBy }
   - `TIARiskAssessed` { TIAId, RiskScore (double), Findings (string?), AssessorId }
   - `TIASupplementaryMeasureRequired` { TIAId, MeasureId, MeasureType, Description }
   - `TIASubmittedForDPOReview` { TIAId, SubmittedBy }
   - `TIADPOApproved` { TIAId, ReviewedBy }
   - `TIADPORejected` { TIAId, ReviewedBy, Reason }
   - `TIACompleted` { TIAId }
   - `TIAExpired` { TIAId }

2. **`Events/SCCEvents.cs`** — `Encina.Compliance.CrossBorderTransfer.Events`
   - `SCCAgreementRegistered` { AgreementId, ProcessorId, Module (SCCModule), Version, ExecutedAtUtc, ExpiresAtUtc?, TenantId?, ModuleId? }
   - `SCCSupplementaryMeasureAdded` { AgreementId, MeasureId, MeasureType, Description }
   - `SCCAgreementRevoked` { AgreementId, Reason, RevokedBy }
   - `SCCAgreementExpired` { AgreementId }

3. **`Events/ApprovedTransferEvents.cs`** — `Encina.Compliance.CrossBorderTransfer.Events`
   - `TransferApproved` { TransferId, SourceCountryCode, DestinationCountryCode, DataCategory, Basis (TransferBasis), SCCAgreementId?, TIAId?, ApprovedBy, ExpiresAtUtc?, TenantId?, ModuleId? }
   - `TransferRevoked` { TransferId, Reason, RevokedBy }
   - `TransferExpired` { TransferId }
   - `TransferRenewed` { TransferId, NewExpiresAtUtc, RenewedBy }

4. **`Aggregates/TIAAggregate.cs`** — `Encina.Compliance.CrossBorderTransfer.Aggregates`
   - Extends `AggregateBase` (from `Encina.DomainModeling`)
   - Properties (derived from events): `SourceCountryCode`, `DestinationCountryCode`, `DataCategory`, `RiskScore`, `Status` (TIAStatus), `Findings`, `AssessorId`, `DPOReviewedAtUtc`, `CompletedAtUtc`, `RequiredSupplementaryMeasures` (List\<SupplementaryMeasure>), `TenantId`, `ModuleId`
   - Command methods (raise events):
     - `static Create(Guid id, string source, string destination, string dataCategory, string createdBy)` → raises `TIACreated`
     - `AssessRisk(double score, string? findings, string assessorId)` → raises `TIARiskAssessed`
     - `RequireSupplementaryMeasure(Guid measureId, SupplementaryMeasureType type, string description)` → raises `TIASupplementaryMeasureRequired`
     - `SubmitForDPOReview(string submittedBy)` → raises `TIASubmittedForDPOReview`
     - `ApproveDPOReview(string reviewedBy)` → raises `TIADPOApproved`
     - `RejectDPOReview(string reviewedBy, string reason)` → raises `TIADPORejected`
     - `Complete()` → raises `TIACompleted`
     - `Expire()` → raises `TIAExpired`
   - `Apply(object @event)` handler for each event type

5. **`Aggregates/SCCAgreementAggregate.cs`** — `Encina.Compliance.CrossBorderTransfer.Aggregates`
   - Extends `AggregateBase`
   - Properties: `ProcessorId`, `Module` (SCCModule), `Version`, `ExecutedAtUtc`, `ExpiresAtUtc`, `IsRevoked`, `RevokedAtUtc`, `SupplementaryMeasures` (List\<SupplementaryMeasure>), `TenantId`, `ModuleId`
   - Command methods:
     - `static Register(Guid id, string processorId, SCCModule module, string version, DateTimeOffset executedAtUtc, DateTimeOffset? expiresAtUtc, string? tenantId, string? moduleId)` → raises `SCCAgreementRegistered`
     - `AddSupplementaryMeasure(Guid measureId, SupplementaryMeasureType type, string description)` → raises `SCCSupplementaryMeasureAdded`
     - `Revoke(string reason, string revokedBy)` → raises `SCCAgreementRevoked`
     - `Expire()` → raises `SCCAgreementExpired`
   - `IsValid(DateTimeOffset nowUtc)` → computed from state

6. **`Aggregates/ApprovedTransferAggregate.cs`** — `Encina.Compliance.CrossBorderTransfer.Aggregates`
   - Extends `AggregateBase`
   - Properties: `SourceCountryCode`, `DestinationCountryCode`, `DataCategory`, `Basis` (TransferBasis), `SCCAgreementId`, `TIAId`, `ApprovedBy`, `ExpiresAtUtc`, `IsRevoked`, `RevokedAtUtc`, `TenantId`, `ModuleId`
   - Command methods:
     - `static Approve(Guid id, string source, string destination, string dataCategory, TransferBasis basis, Guid? sccId, Guid? tiaId, string approvedBy, DateTimeOffset? expiresAtUtc, string? tenantId, string? moduleId)` → raises `TransferApproved`
     - `Revoke(string reason, string revokedBy)` → raises `TransferRevoked`
     - `Expire()` → raises `TransferExpired`
     - `Renew(DateTimeOffset newExpiresAtUtc, string renewedBy)` → raises `TransferRenewed`
   - `IsValid(DateTimeOffset nowUtc)` → computed from state

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```
CONTEXT:
You are implementing Phase 2 of Encina.Compliance.CrossBorderTransfer (Issue #412).
Phase 1 created the value objects, enums, and project structure.
This phase creates event-sourced aggregates using Encina's AggregateBase pattern from Encina.DomainModeling.

TASK:
1. Create Events/ folder with TIAEvents.cs, SCCEvents.cs, ApprovedTransferEvents.cs
2. Create Aggregates/ folder with TIAAggregate.cs, SCCAgreementAggregate.cs, ApprovedTransferAggregate.cs
3. Each aggregate extends AggregateBase from Encina.DomainModeling
4. Each aggregate has command methods that call RaiseEvent<T>() to produce events
5. Each aggregate has Apply(object @event) to handle events and mutate internal state
6. Aggregates enforce invariants (e.g., cannot complete TIA without risk assessment, cannot revoke already-revoked SCC)

KEY RULES:
- Events are sealed records — immutable facts, no behavior
- Aggregates use RaiseEvent<T>() from AggregateBase (not direct state mutation)
- Apply() handles each event type via pattern matching
- Guard clauses: throw InvalidOperationException for invalid state transitions
- Command methods are void (events are raised, not returned)
- Static factory methods for creation: TIAAggregate.Create(...) raises TIACreated
- Full XML documentation on all public types and members
- TenantId/ModuleId on creation events for multi-tenancy/module isolation

REFERENCE FILES:
- src/Encina.DomainModeling/AggregateBase.cs — base class with RaiseEvent<T>() and Apply() pattern
- src/Encina.DomainModeling/IAggregate.cs — interface: Id, Version, UncommittedEvents, ClearUncommittedEvents()
- src/Encina.Compliance.CrossBorderTransfer/Model/ — value objects from Phase 1
```

</details>

### Phase 3: Core Interfaces & Services

<details>
<summary>Tasks</summary>

1. **`Abstractions/ITIAService.cs`** — `Encina.Compliance.CrossBorderTransfer.Abstractions`
   - Service interface for TIA lifecycle operations (wraps aggregate repository):
   - `CreateTIAAsync(string source, string destination, string dataCategory, string createdBy, CancellationToken ct)` → `Either<EncinaError, Guid>`
   - `AssessRiskAsync(Guid tiaId, double riskScore, string? findings, string assessorId, CancellationToken ct)` → `Either<EncinaError, Unit>`
   - `RequireSupplementaryMeasureAsync(Guid tiaId, SupplementaryMeasureType type, string description, CancellationToken ct)` → `Either<EncinaError, Unit>`
   - `SubmitForDPOReviewAsync(Guid tiaId, string submittedBy, CancellationToken ct)` → `Either<EncinaError, Unit>`
   - `CompleteDPOReviewAsync(Guid tiaId, bool approved, string reviewedBy, string? reason, CancellationToken ct)` → `Either<EncinaError, Unit>`
   - `GetTIAAsync(Guid tiaId, CancellationToken ct)` → `Either<EncinaError, TIAReadModel?>`
   - `GetTIAByRouteAsync(string source, string destination, string dataCategory, CancellationToken ct)` → `Either<EncinaError, TIAReadModel?>`

2. **`Abstractions/ISCCService.cs`** — `Encina.Compliance.CrossBorderTransfer.Abstractions`
   - `RegisterAgreementAsync(string processorId, SCCModule module, string version, DateTimeOffset executedAtUtc, DateTimeOffset? expiresAtUtc, string? tenantId, CancellationToken ct)` → `Either<EncinaError, Guid>`
   - `AddSupplementaryMeasureAsync(Guid agreementId, SupplementaryMeasureType type, string description, CancellationToken ct)` → `Either<EncinaError, Unit>`
   - `RevokeAgreementAsync(Guid agreementId, string reason, string revokedBy, CancellationToken ct)` → `Either<EncinaError, Unit>`
   - `GetAgreementAsync(Guid agreementId, CancellationToken ct)` → `Either<EncinaError, SCCAgreementReadModel?>`
   - `ValidateAgreementAsync(string processorId, SCCModule module, CancellationToken ct)` → `Either<EncinaError, SCCValidationResult>`

3. **`Abstractions/IApprovedTransferService.cs`** — `Encina.Compliance.CrossBorderTransfer.Abstractions`
   - `ApproveTransferAsync(string source, string destination, string dataCategory, TransferBasis basis, Guid? sccId, Guid? tiaId, string approvedBy, DateTimeOffset? expiresAtUtc, string? tenantId, CancellationToken ct)` → `Either<EncinaError, Guid>`
   - `RevokeTransferAsync(Guid transferId, string reason, string revokedBy, CancellationToken ct)` → `Either<EncinaError, Unit>`
   - `RenewTransferAsync(Guid transferId, DateTimeOffset newExpiresAtUtc, string renewedBy, CancellationToken ct)` → `Either<EncinaError, Unit>`
   - `GetApprovedTransferAsync(string source, string destination, string dataCategory, CancellationToken ct)` → `Either<EncinaError, ApprovedTransferReadModel?>`
   - `IsTransferApprovedAsync(string source, string destination, string dataCategory, CancellationToken ct)` → `Either<EncinaError, bool>`

4. **`Abstractions/ITransferValidator.cs`** — `Encina.Compliance.CrossBorderTransfer.Abstractions`
   - `ValidateAsync(TransferRequest request, CancellationToken ct)` → `Either<EncinaError, TransferValidationOutcome>`
   - Orchestrates: adequacy check → approved transfer check → SCC check → TIA requirement check

5. **`Abstractions/ITIARiskAssessor.cs`** — `Encina.Compliance.CrossBorderTransfer.Abstractions`
   - Strategy interface for pluggable risk assessment
   - `AssessRiskAsync(string destinationCountryCode, string dataCategory, CancellationToken ct)` → `Either<EncinaError, TIARiskAssessment>`
   - `TIARiskAssessment` record: `Score` (double), `Factors` (IReadOnlyList\<string>), `Recommendations` (IReadOnlyList\<string>)

6. **`ReadModels/TIAReadModel.cs`** — `Encina.Compliance.CrossBorderTransfer.ReadModels`
   - Sealed record: projected view from TIAAggregate events
   - Properties: `Id`, `SourceCountryCode`, `DestinationCountryCode`, `DataCategory`, `RiskScore`, `Status`, `Findings`, `AssessorId`, `DPOReviewedAtUtc`, `CompletedAtUtc`, `RequiredSupplementaryMeasures`, `TenantId`, `ModuleId`, `CreatedAtUtc`, `LastModifiedAtUtc`

7. **`ReadModels/SCCAgreementReadModel.cs`** — `Encina.Compliance.CrossBorderTransfer.ReadModels`
   - Sealed record: projected view from SCCAgreementAggregate events
   - Properties: `Id`, `ProcessorId`, `Module`, `Version`, `ExecutedAtUtc`, `ExpiresAtUtc`, `IsRevoked`, `RevokedAtUtc`, `SupplementaryMeasures`, `TenantId`, `ModuleId`
   - `IsValid(DateTimeOffset nowUtc)` computed property

8. **`ReadModels/ApprovedTransferReadModel.cs`** — `Encina.Compliance.CrossBorderTransfer.ReadModels`
   - Sealed record: projected view from ApprovedTransferAggregate events
   - Properties: `Id`, `SourceCountryCode`, `DestinationCountryCode`, `DataCategory`, `Basis`, `SCCAgreementId`, `TIAId`, `ApprovedBy`, `ExpiresAtUtc`, `IsRevoked`, `RevokedAtUtc`, `TenantId`, `ModuleId`
   - `IsValid(DateTimeOffset nowUtc)` computed property

9. **`Model/SCCValidationResult.cs`** — `Encina.Compliance.CrossBorderTransfer.Model`
   - Sealed record: `IsValid` (bool), `AgreementId` (Guid?), `Module` (SCCModule?), `Version` (string?), `MissingMeasures` (IReadOnlyList\<string>), `Issues` (IReadOnlyList\<string>)

10. **`Errors/CrossBorderTransferErrors.cs`** — `Encina.Compliance.CrossBorderTransfer.Errors`
    - Static class with factory methods returning `EncinaError`:
    - `TIANotFound(Guid id)`, `TIAAlreadyCompleted(Guid id)`, `TIANotAssessed(Guid id)`
    - `SCCAgreementNotFound(Guid id)`, `SCCAgreementAlreadyRevoked(Guid id)`
    - `TransferNotFound(Guid id)`, `TransferAlreadyRevoked(Guid id)`
    - `TransferBlocked(string reason)`, `InvalidStateTransition(string from, string to)`

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```
CONTEXT:
You are implementing Phase 3 of Encina.Compliance.CrossBorderTransfer (Issue #412).
Phase 1 created value objects/enums. Phase 2 created event-sourced aggregates.
This phase creates service interfaces, read models, and error definitions.
The services wrap IAggregateRepository<T> from Encina.Marten and provide a clean API for consumers.

TASK:
1. Create Abstractions/ folder with ITIAService.cs, ISCCService.cs, IApprovedTransferService.cs, ITransferValidator.cs, ITIARiskAssessor.cs
2. Create ReadModels/ folder with TIAReadModel.cs, SCCAgreementReadModel.cs, ApprovedTransferReadModel.cs
3. Create Errors/CrossBorderTransferErrors.cs with static error factories
4. Create Model/SCCValidationResult.cs

KEY RULES:
- All service methods return ValueTask<Either<EncinaError, T>> (ROP pattern)
- Read models are sealed records with computed properties (IsValid, etc.)
- Errors use EncinaError, never exceptions for business logic
- Full XML documentation
- Services are the public API — aggregates are internal implementation details

REFERENCE FILES:
- src/Encina.Compliance.Consent/Abstractions/ — for IConsentStore pattern (but we use Service not Store)
- src/Encina.Compliance.Consent/Errors/ — for error factory pattern
- src/Encina.Marten/IAggregateRepository.cs — for LoadAsync/SaveAsync/CreateAsync signatures
- src/Encina.Compliance.CrossBorderTransfer/Aggregates/ — aggregates from Phase 2
```

</details>

### Phase 4: Service Implementations

<details>
<summary>Tasks</summary>

1. **`Services/DefaultTIAService.cs`** — `Encina.Compliance.CrossBorderTransfer.Services`
   - Implements `ITIAService`
   - Constructor: `IAggregateRepository<TIAAggregate> repository, ICacheProvider cache, TimeProvider timeProvider, ILogger<DefaultTIAService> logger`
   - Each method: load aggregate → call command method → save → invalidate cache → return result
   - Query methods: load aggregate → project to read model (or use cache)
   - `GetTIAByRouteAsync`: loads all TIA aggregate IDs, projects each, filters by route

2. **`Services/DefaultSCCService.cs`** — `Encina.Compliance.CrossBorderTransfer.Services`
   - Implements `ISCCService`
   - Constructor: `IAggregateRepository<SCCAgreementAggregate> repository, ICacheProvider cache, TimeProvider timeProvider, ILogger<DefaultSCCService> logger`
   - `ValidateAgreementAsync`: loads by processorId, checks module match, expiry, revocation, supplementary measures

3. **`Services/DefaultApprovedTransferService.cs`** — `Encina.Compliance.CrossBorderTransfer.Services`
   - Implements `IApprovedTransferService`
   - Constructor: `IAggregateRepository<ApprovedTransferAggregate> repository, ICacheProvider cache, TimeProvider timeProvider, ILogger<DefaultApprovedTransferService> logger`
   - `GetApprovedTransferAsync` and `IsTransferApprovedAsync`: cached by route key

4. **`Services/DefaultTransferValidator.cs`** — `Encina.Compliance.CrossBorderTransfer.Services`
   - Implements `ITransferValidator`
   - Constructor: `IAdequacyDecisionProvider adequacyProvider, IApprovedTransferService transferService, ISCCService sccService, ITIAService tiaService, IOptions<CrossBorderTransferOptions> options, ILogger<DefaultTransferValidator> logger`
   - Validation chain: (1) adequacy decision? → Allow(AdequacyDecision), (2) approved transfer exists? → return cached result, (3) SCC in place? → validate, (4) TIA required? → check completion, (5) Block

5. **`Services/DefaultTIARiskAssessor.cs`** — `Encina.Compliance.CrossBorderTransfer.Services`
   - Implements `ITIARiskAssessor`
   - Default rule-based risk assessment considering: adequacy status, intelligence sharing alliances (Five Eyes, Nine Eyes, Fourteen Eyes), government surveillance legislation, independent DPA existence
   - Returns `TIARiskAssessment` with score, factors, and recommendations

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```
CONTEXT:
You are implementing Phase 4 of Encina.Compliance.CrossBorderTransfer (Issue #412).
Phases 1-3 created models, aggregates, interfaces, and read models.
This phase implements the services that orchestrate aggregate operations via IAggregateRepository<T>.

TASK:
1. Create Services/ folder with DefaultTIAService.cs, DefaultSCCService.cs, DefaultApprovedTransferService.cs, DefaultTransferValidator.cs, DefaultTIARiskAssessor.cs
2. Each service wraps IAggregateRepository<T> from Encina.Marten
3. Services project aggregates to read models for query operations
4. Services use ICacheProvider from Encina.Caching for caching read models (configurable TTL, tag-based invalidation)
5. DefaultTransferValidator chains: adequacy → approved transfer → SCC → TIA → block

KEY RULES:
- Inject IAggregateRepository<T> (from Encina.Marten), ICacheProvider (from Encina.Caching), TimeProvider, ILogger<T>
- All methods return ValueTask<Either<EncinaError, T>>
- Cache invalidation on write operations via tag-based invalidation (ICacheProvider.InvalidateByTagAsync)
- Cache key pattern: "cbt:{aggregateType}:{routeKey}" where routeKey = "{source}:{destination}:{dataCategory}"
- Cache tags: "cbt:tia:{id}", "cbt:scc:{id}", "cbt:transfer:{id}" for targeted invalidation
- Log all operations using ILogger (structured logging, not LoggerMessage yet — Phase 8 adds that)
- Guard clauses with ArgumentNullException.ThrowIfNull on constructor parameters

REFERENCE FILES:
- src/Encina.Marten/MartenAggregateRepository.cs — LoadAsync, SaveAsync, CreateAsync patterns
- src/Encina.Compliance.Consent/Validators/DefaultConsentValidator.cs — for validation chain pattern
- src/Encina.Compliance.DataResidency/Abstractions/IAdequacyDecisionProvider.cs — for adequacy check integration
```

</details>

### Phase 5: Attribute & Pipeline Behavior

<details>
<summary>Tasks</summary>

1. **`Attributes/RequiresCrossBorderTransferAttribute.cs`** — `Encina.Compliance.CrossBorderTransfer.Attributes`
   - `[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]`
   - Properties: `Destination` (string — ISO 3166-1 alpha-2), `DataCategory` (string), `SourceProperty` (string?), `DestinationProperty` (string?)
   - When `DestinationProperty` is set, destination is extracted from the request object at runtime

2. **`CrossBorderTransferEnforcementMode.cs`** — `Encina.Compliance.CrossBorderTransfer`
   - Enum: `Block = 0`, `Warn = 1`, `Disabled = 2`

3. **`Pipeline/TransferBlockingPipelineBehavior.cs`** — `Encina.Compliance.CrossBorderTransfer.Pipeline`
   - Implements `IPipelineBehavior<TRequest, TResponse>` from MediatR/Encina
   - Static `ConcurrentDictionary<Type, RequiresCrossBorderTransferAttribute?>` for attribute caching
   - Workflow: (1) check enforcement mode, (2) detect attribute (cached), (3) extract destination, (4) call `ITransferValidator.ValidateAsync()`, (5) handle result (Block/Warn)
   - OpenTelemetry: `Activity` with tags, `Meter` with counters
   - Multi-tenancy: resolve `ITenantContext` for tenant-scoped validations
   - Module isolation: resolve `IModuleContext` for module-scoped validations

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```
CONTEXT:
You are implementing Phase 5 of Encina.Compliance.CrossBorderTransfer (Issue #412).
Phase 4 implemented services. This phase adds the attribute and pipeline behavior for automatic enforcement.

TASK:
1. Create Attributes/RequiresCrossBorderTransferAttribute.cs
2. Create CrossBorderTransferEnforcementMode.cs enum
3. Create Pipeline/TransferBlockingPipelineBehavior.cs

KEY RULES:
- Attribute caching: static ConcurrentDictionary<Type, RequiresCrossBorderTransferAttribute?> — zero reflection after first access
- Pipeline behavior follows EXACTLY the Consent pattern: check enforcement mode → detect attribute → extract context → validate → handle result
- Three modes: Block (return error), Warn (log warning, continue), Disabled (skip)
- OpenTelemetry: Activity from CrossBorderTransferDiagnostics.ActivitySource, Counter<long> for transfers_validated, transfers_blocked, transfers_warned
- Multi-tenancy: TryResolve ITenantContext for tenant-scoped transfer validation
- Module isolation: TryResolve IModuleContext for module-scoped validation
- Destination extraction: from attribute property OR from request property via cached reflection

REFERENCE FILES:
- src/Encina.Compliance.Consent/Pipeline/ConsentRequiredPipelineBehavior.cs — EXACT pattern to follow
- src/Encina.Compliance.Consent/Attributes/RequireConsentAttribute.cs — attribute pattern
```

</details>

### Phase 6: Configuration, DI & Options

<details>
<summary>Tasks</summary>

1. **`CrossBorderTransferOptions.cs`** — `Encina.Compliance.CrossBorderTransfer`
   - `EnforcementMode` (CrossBorderTransferEnforcementMode) — default `Block`
   - `TIARiskThreshold` (double) — default `0.6`
   - `DefaultTIAExpirationDays` (int?) — default `365`
   - `DefaultSCCExpirationDays` (int?) — default `null` (no auto-expiry)
   - `DefaultTransferExpirationDays` (int?) — default `365`
   - `AutoDetectTransfers` (bool) — default `false`
   - `CacheEnabled` (bool) — default `true`
   - `CacheTTLMinutes` (int) — default `5`
   - `AddHealthCheck` (bool) — default `false`
   - `RequireTIAForNonAdequate` (bool) — default `true`
   - `RequireSCCForNonAdequate` (bool) — default `true`

2. **`CrossBorderTransferOptionsValidator.cs`** — `Encina.Compliance.CrossBorderTransfer`
   - Validates: `TIARiskThreshold` in 0.0-1.0, `CacheTTLMinutes` > 0, expiration days > 0

3. **`ServiceCollectionExtensions.cs`** — `Encina.Compliance.CrossBorderTransfer`
   - `AddEncinaCrossBorderTransfer(this IServiceCollection services, Action<CrossBorderTransferOptions>? configure = null)`
   - Registers: options, services (TryAdd), pipeline behavior, diagnostics, health check (conditional)
   - Registration order:
     - `TryAddScoped<ITIAService, DefaultTIAService>()`
     - `TryAddScoped<ISCCService, DefaultSCCService>()`
     - `TryAddScoped<IApprovedTransferService, DefaultApprovedTransferService>()`
     - `TryAddScoped<ITransferValidator, DefaultTransferValidator>()`
     - `TryAddScoped<ITIARiskAssessor, DefaultTIARiskAssessor>()`
     - `AddTransient(typeof(IPipelineBehavior<,>), typeof(TransferBlockingPipelineBehavior<,>))`

4. **`CrossBorderTransferMartenExtensions.cs`** — `Encina.Compliance.CrossBorderTransfer`
   - `AddCrossBorderTransferAggregates(this IServiceCollection services)`
   - Registers aggregate repositories with Marten:
     - `services.AddAggregateRepository<TIAAggregate>()`
     - `services.AddAggregateRepository<SCCAgreementAggregate>()`
     - `services.AddAggregateRepository<ApprovedTransferAggregate>()`
   - Called from `AddEncinaCrossBorderTransfer()` or separately by the user

5. **Update `Encina.Messaging/MessagingConfiguration.cs`**
   - Add `public bool UseCrossBorderTransfer { get; set; }` property with XML docs

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```
CONTEXT:
You are implementing Phase 6 of Encina.Compliance.CrossBorderTransfer (Issue #412).
This phase creates configuration, DI registration, and Marten aggregate registration.

TASK:
1. Create CrossBorderTransferOptions.cs with all configuration properties
2. Create CrossBorderTransferOptionsValidator.cs for options validation
3. Create ServiceCollectionExtensions.cs with AddEncinaCrossBorderTransfer()
4. Create CrossBorderTransferMartenExtensions.cs for aggregate repository registration
5. Update MessagingConfiguration.cs — add UseCrossBorderTransfer property

KEY RULES:
- Use TryAdd* for all services (allows user override)
- Pipeline behavior uses AddTransient (not TryAdd — must always be in pipeline)
- Options validation: IValidateOptions<CrossBorderTransferOptions>
- Health check registration is conditional (options.AddHealthCheck)
- Marten aggregates: use services.AddAggregateRepository<T>() from Encina.Marten
- Follow EXACTLY the Consent ServiceCollectionExtensions pattern

REFERENCE FILES:
- src/Encina.Compliance.Consent/ServiceCollectionExtensions.cs — registration pattern
- src/Encina.Marten/ServiceCollectionExtensions.cs — AddAggregateRepository<T>() method
- src/Encina.Messaging/MessagingConfiguration.cs — UseConsent property pattern (line ~635)
```

</details>

### Phase 7: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. **Multi-Tenancy integration**
   - All service methods propagate `TenantId` from `ITenantContext` (resolved via DI)
   - Aggregate creation events include `TenantId`
   - Read model queries filter by `TenantId` when tenant context is available
   - Pipeline behavior resolves `ITenantContext` for tenant-scoped validation

2. **Module Isolation integration**
   - All service methods propagate `ModuleId` from `IModuleContext` (resolved via DI)
   - Aggregate creation events include `ModuleId`
   - Read model queries filter by `ModuleId` when module context is available

3. **Audit Trail integration**
   - Event sourcing IS the audit trail — every state change is an immutable event
   - Events include `UserId`, `TenantId`, `ModuleId`, timestamps
   - Marten event metadata enrichment adds `CorrelationId`, `CausationId`
   - No additional audit store needed — the event stream IS the audit log

4. **Caching integration** (via `ICacheProvider` from `Encina.Caching`)
   - `ICacheProvider` caches read models in service implementations
   - Cache key: `"cbt:{type}:{route}"` for route-based lookups
   - Tags: `"cbt:tia:{id}"`, `"cbt:scc:{id}"`, `"cbt:transfer:{id}"` for targeted invalidation
   - TTL configurable via `CrossBorderTransferOptions.CacheTTLMinutes`
   - Write-through: cache invalidated on aggregate command execution via tag-based invalidation
   - Distributed: works across multiple app instances when backed by Redis/Valkey/etc.
   - Stampede protection: `ICacheProvider` handles concurrent cache misses automatically

5. **Transaction integration**
   - Marten `IDocumentSession` handles transactions implicitly
   - `SaveChangesAsync()` is atomic — events are committed or rolled back together
   - No explicit `IUnitOfWork` needed (Marten session IS the unit of work)

6. **Validation integration**
   - `CrossBorderTransferOptionsValidator` validates configuration at startup
   - Service methods validate inputs (null checks, range checks) before aggregate operations
   - Pipeline behavior validates transfer requests before enforcement

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```
CONTEXT:
You are implementing Phase 7 of Encina.Compliance.CrossBorderTransfer (Issue #412).
This phase integrates cross-cutting concerns into the existing services and pipeline behavior.

TASK:
1. Update DefaultTIAService, DefaultSCCService, DefaultApprovedTransferService:
   - Inject optional ITenantContext and IModuleContext via IServiceProvider.GetService<T>()
   - Propagate TenantId/ModuleId to aggregate creation events
   - Filter read model queries by TenantId/ModuleId when context is available
2. Update TransferBlockingPipelineBehavior:
   - Resolve ITenantContext/IModuleContext from DI
   - Pass tenant/module context to ITransferValidator
3. Verify caching is correctly implemented in services (Phase 4 should have done this)
4. Verify Marten event metadata enrichment is working (CorrelationId, CausationId)

KEY RULES:
- ITenantContext and IModuleContext are OPTIONAL — resolve via GetService, not GetRequiredService
- When TenantId is null, queries return ALL tenants (admin/system context)
- Audit trail is automatic via event sourcing — no additional code needed
- Transactions are automatic via Marten IDocumentSession

REFERENCE FILES:
- src/Encina.Compliance.Consent/Pipeline/ConsentRequiredPipelineBehavior.cs — ITenantContext resolution
- src/Encina.Marten/ConfigureMartenEventMetadata.cs — CorrelationId/CausationId enrichment
```

</details>

### Phase 8: Observability

<details>
<summary>Tasks</summary>

1. **`Diagnostics/CrossBorderTransferDiagnostics.cs`** — `Encina.Compliance.CrossBorderTransfer.Diagnostics`
   - `ActivitySource` named `"Encina.Compliance.CrossBorderTransfer"`
   - `Meter` named `"Encina.Compliance.CrossBorderTransfer"`
   - Counters:
     - `encina.compliance.transfer.validated` — transfers validated (tags: source, destination, basis, allowed)
     - `encina.compliance.transfer.blocked` — transfers blocked (tags: source, destination, reason)
     - `encina.compliance.tia.created` — TIAs created
     - `encina.compliance.tia.completed` — TIAs completed (tags: risk_level)
     - `encina.compliance.scc.registered` — SCC agreements registered (tags: module)
     - `encina.compliance.scc.revoked` — SCC agreements revoked

2. **`Diagnostics/CrossBorderTransferLogMessages.cs`** — `Encina.Compliance.CrossBorderTransfer.Diagnostics`
   - Event IDs in range **8500-8549** (non-colliding — existing ranges: 7000-7009, 8000-8004, 8100-8250, 8400-8415)
   - `LoggerMessage.Define<>()` for high-performance structured logging:
     - 8500: TransferValidationStarted
     - 8501: TransferValidationCompleted (allowed/blocked)
     - 8502: TransferBlockedByPolicy
     - 8503: TransferAllowedByAdequacy
     - 8504: TransferAllowedBySCC
     - 8505: TransferRequiresTIA
     - 8510: TIACreated
     - 8511: TIARiskAssessed
     - 8512: TIACompleted
     - 8513: TIAExpired
     - 8520: SCCAgreementRegistered
     - 8521: SCCAgreementRevoked
     - 8522: SCCAgreementExpired
     - 8530: ApprovedTransferCreated
     - 8531: ApprovedTransferRevoked
     - 8532: ApprovedTransferRenewed
     - 8540: CacheHit
     - 8541: CacheMiss
     - 8542: CacheInvalidated

3. **Update services** — Replace `ILogger` calls with `CrossBorderTransferLogMessages` extension methods

</details>

<details>
<summary>Prompt for AI Agents — Phase 8</summary>

```
CONTEXT:
You are implementing Phase 8 of Encina.Compliance.CrossBorderTransfer (Issue #412).
This phase adds full observability: OpenTelemetry ActivitySource + Meter, and high-performance LoggerMessage structured logging.

TASK:
1. Create Diagnostics/CrossBorderTransferDiagnostics.cs with ActivitySource + Meter + Counter<long> definitions
2. Create Diagnostics/CrossBorderTransferLogMessages.cs with LoggerMessage.Define<>() for all log events
3. Update all service implementations to use LogMessages extension methods instead of direct ILogger calls
4. Update TransferBlockingPipelineBehavior to use ActivitySource and Meter counters

KEY RULES:
- EventId range: 8500-8549 (MUST NOT collide with: 7000-7009, 8000-8004, 8100-8250, 8400-8415)
- LoggerMessage.Define<>() for zero-allocation logging (not [LoggerMessage] source generator — keep consistent with ConsentLogMessages pattern)
- ActivitySource: one Activity per validation (with tags for source, destination, basis, result)
- Meter: Counter<long> with dimensional tags
- All counters use TagList for tags

REFERENCE FILES:
- src/Encina.Compliance.Consent/Diagnostics/ConsentDiagnostics.cs — ActivitySource + Meter pattern
- src/Encina.Compliance.Consent/Diagnostics/ConsentLogMessages.cs — LoggerMessage.Define pattern with EventId(8200+)
```

</details>

### Phase 9: Health Check & Notifications

<details>
<summary>Tasks</summary>

1. **`Health/CrossBorderTransferHealthCheck.cs`** — `Encina.Compliance.CrossBorderTransfer.Health`
   - Implements `IHealthCheck`
   - `DefaultName` const = `"encina_compliance_crossbordertransfer"`
   - `Tags` static = `["encina", "compliance", "cross-border-transfer", "gdpr"]`
   - Checks: (1) Marten event store connectivity (load any aggregate), (2) adequacy decision provider reachable, (3) ICacheProvider operational
   - Scoped resolution via `IServiceProvider.CreateScope()`

2. **`Notifications/TransferExpirationMonitor.cs`** — `Encina.Compliance.CrossBorderTransfer.Notifications`
   - `BackgroundService` that periodically checks for expiring TIAs, SCCs, and approved transfers
   - Raises domain events: `TIAExpired`, `SCCAgreementExpired`, `TransferExpired`
   - Configurable check interval via `CrossBorderTransferOptions`
   - Uses `TimeProvider` for testable time

3. **`Notifications/TransferNotificationEvents.cs`** — `Encina.Compliance.CrossBorderTransfer.Notifications`
   - Domain events for integration with notification systems:
   - `TransferExpiringNotification` { TransferId, ExpiresAtUtc, DaysUntilExpiry }
   - `TIAExpiringNotification` { TIAId, ExpiresAtUtc, DaysUntilExpiry }

</details>

<details>
<summary>Prompt for AI Agents — Phase 9</summary>

```
CONTEXT:
You are implementing Phase 9 of Encina.Compliance.CrossBorderTransfer (Issue #412).

TASK:
1. Create Health/CrossBorderTransferHealthCheck.cs
2. Create Notifications/TransferExpirationMonitor.cs (BackgroundService)
3. Create Notifications/TransferNotificationEvents.cs

KEY RULES:
- Health check: DefaultName const, Tags static array, scoped resolution via CreateScope()
- BackgroundService: use PeriodicTimer, inject TimeProvider for testability
- Expiration monitor: load aggregates, check ExpiresAtUtc against now, raise expiration events
- Notifications are domain events (sealed records) — integration with external notification systems is out of scope

REFERENCE FILES:
- src/Encina.Compliance.Consent/Health/ConsentHealthCheck.cs — health check pattern
- src/Encina.Compliance.Retention/Notifications/ — expiration monitoring pattern (if exists)
```

</details>

### Phase 10: Testing

<details>
<summary>Tasks</summary>

**Unit Tests** (`tests/Encina.UnitTests/Compliance/CrossBorderTransfer/`)

1. `Aggregates/TIAAggregatTests.cs` — Test lifecycle: Create → AssessRisk → RequireMeasure → SubmitDPO → ApproveDPO → Complete. Invalid transitions. Event production.
2. `Aggregates/SCCAgreementAggregateTests.cs` — Register → AddMeasure → Revoke. IsValid checks. Expired state.
3. `Aggregates/ApprovedTransferAggregateTests.cs` — Approve → Revoke. Renew. Expired state.
4. `Services/DefaultTransferValidatorTests.cs` — Mock services: adequacy → SCC → TIA → block chain. All branches.
5. `Services/DefaultTIAServiceTests.cs` — Mock IAggregateRepository, test CRUD operations, cache behavior.
6. `Services/DefaultSCCServiceTests.cs` — Mock IAggregateRepository, test validation logic.
7. `Services/DefaultApprovedTransferServiceTests.cs` — Mock IAggregateRepository, test route lookup.
8. `Services/DefaultTIARiskAssessorTests.cs` — Test risk scoring for various countries.
9. `Pipeline/TransferBlockingPipelineBehaviorTests.cs` — Attribute detection, enforcement modes, validation calls.
10. `CrossBorderTransferOptionsValidatorTests.cs` — Valid/invalid configurations.

**Guard Tests** (`tests/Encina.GuardTests/Compliance/CrossBorderTransfer/`)

11. `TIAServiceGuardTests.cs` — ArgumentNullException on all null parameters.
12. `SCCServiceGuardTests.cs` — ArgumentNullException.
13. `ApprovedTransferServiceGuardTests.cs` — ArgumentNullException.
14. `TransferValidatorGuardTests.cs` — ArgumentNullException.

**Property Tests** (`tests/Encina.PropertyTests/Compliance/CrossBorderTransfer/`)

15. `TIAAggregatePropertyTests.cs` — FsCheck: round-trip (create → events → replay → same state), invariants (version = event count), risk score bounds.
16. `TransferValidationPropertyTests.cs` — FsCheck: valid requests always get a result, adequacy countries always allowed.

**Contract Tests** (`tests/Encina.ContractTests/Compliance/CrossBorderTransfer/`)

17. `ITIAServiceContractTests.cs` — Verify service contract: create returns valid ID, assess updates state, etc.
18. `ITransferValidatorContractTests.cs` — Verify validation contract.

**Integration Tests** (`tests/Encina.IntegrationTests/Compliance/CrossBorderTransfer/`)

19. `TIAMartenIntegrationTests.cs` — Real Marten with PostgreSQL. Full lifecycle: create → save → reload → verify state. Uses `[Collection("EFCore-PostgreSQL")]` fixture (Marten uses PostgreSQL).
20. `SCCAgreementMartenIntegrationTests.cs` — Real Marten. Register → revoke → reload.
21. `ApprovedTransferMartenIntegrationTests.cs` — Real Marten. Approve → query by route.
22. `TransferValidatorIntegrationTests.cs` — Full pipeline with real Marten. End-to-end validation.

**Justification Documents**

23. `tests/Encina.LoadTests/Compliance/CrossBorderTransfer/CrossBorderTransfer.md` — LoadTest justification: transfer validation is not a concurrent hot path (compliance decisions are infrequent).
24. `tests/Encina.BenchmarkTests/Compliance/CrossBorderTransfer/CrossBorderTransfer.md` — Benchmark justification: pipeline behavior caching is O(1), no hot path to optimize.

</details>

<details>
<summary>Prompt for AI Agents — Phase 10</summary>

```
CONTEXT:
You are implementing Phase 10 of Encina.Compliance.CrossBorderTransfer (Issue #412).
This phase creates all tests following Encina testing standards.

TASK:
1. Create unit tests for all aggregates, services, pipeline behavior, and options validator
2. Create guard tests for all public service methods
3. Create property tests with FsCheck for aggregate invariants and validation round-trips
4. Create contract tests for service interfaces
5. Create integration tests with real Marten/PostgreSQL
6. Create justification .md documents for LoadTests and BenchmarkTests

KEY RULES:
- Unit tests: AAA pattern, mock all dependencies, fast execution
- Guard tests: ArgumentNullException for all public method parameters
- Property tests: FsCheck generators for domain types, invariant verification
- Contract tests: verify interface contracts
- Integration tests: use [Collection("EFCore-PostgreSQL")] fixture (Marten uses PostgreSQL)
- Integration test cleanup: use fixture.ClearAllDataAsync() in InitializeAsync
- Justification docs: follow the standard format from CLAUDE.md
- Test naming: Method_Scenario_ExpectedResult

REFERENCE FILES:
- tests/Encina.UnitTests/Compliance/Consent/ — for unit test patterns
- tests/Encina.GuardTests/ — for guard test patterns
- tests/Encina.IntegrationTests/ — for Collection fixture patterns
- tests/Encina.PropertyTests/ — for FsCheck patterns
```

</details>

### Phase 11: Documentation & Finalization

<details>
<summary>Tasks</summary>

1. **XML doc comments** — Verify all public APIs have `<summary>`, `<remarks>`, `<param>`, `<returns>`, `<example>` tags
2. **`CHANGELOG.md`** — Add entry under Unreleased:
   ```
   ### Added
   - `Encina.Compliance.CrossBorderTransfer` — International data transfer validation (GDPR Arts. 44-49, Schrems II)
     - Event-sourced TIA, SCC Agreement, and Approved Transfer aggregates via Marten
     - `TransferBlockingPipelineBehavior` with `[RequiresCrossBorderTransfer]` attribute
     - Default TIA risk assessor with country risk scoring
     - Adequacy decision integration with DataResidency
     - Full observability: ActivitySource, Meter, LoggerMessage (EventIds 8500-8549)
     - Health check for Marten connectivity
     - Transfer expiration monitoring (BackgroundService)
     - Multi-tenancy and module isolation support
   ```
3. **`ROADMAP.md`** — Update if Cross-Border Transfer was listed as planned
4. **`docs/features/cross-border-transfer.md`** — Feature documentation:
   - Usage guide with code examples
   - Configuration reference
   - SCC module explanation with EU 2021 SCCs context
   - TIA workflow documentation
   - Integration with DataResidency
   - Marten event sourcing explanation and PostgreSQL requirement
5. **`docs/INVENTORY.md`** — Add new package and files
6. **`PublicAPI.Unshipped.txt`** — Ensure all public symbols are tracked
7. **Build verification**: `dotnet build --configuration Release` → 0 errors, 0 warnings
8. **Test verification**: `dotnet test` → all pass

</details>

<details>
<summary>Prompt for AI Agents — Phase 11</summary>

```
CONTEXT:
You are implementing Phase 11 (final) of Encina.Compliance.CrossBorderTransfer (Issue #412).
All code is implemented. This phase handles documentation and verification.

TASK:
1. Verify XML doc comments on all public APIs
2. Update CHANGELOG.md with new entries under ### Added
3. Update ROADMAP.md if applicable
4. Create docs/features/cross-border-transfer.md with usage guide
5. Update docs/INVENTORY.md with new files
6. Populate PublicAPI.Unshipped.txt
7. Run: dotnet build --configuration Release (must be 0 errors, 0 warnings)
8. Run: dotnet test (all must pass)

KEY RULES:
- CHANGELOG follows Keep a Changelog format
- Feature docs include: overview, quickstart, configuration, advanced usage, Marten requirement
- PublicAPI entries: "Namespace.Type.Member(params) -> ReturnType" format
- Build must produce 0 warnings (fix any CA warnings)
- All tests must pass

REFERENCE FILES:
- CHANGELOG.md — existing format
- docs/features/ — existing feature documentation
- docs/INVENTORY.md — existing inventory
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Reference | Relevance |
|----------|-----------|-----------|
| GDPR Art. 44 | General principle for transfers | Base requirement — transfers need safeguards |
| GDPR Art. 45 | Adequacy decisions | AdequacyDecisionRegistry, automatic allow for adequate countries |
| GDPR Art. 46(2)(c) | Standard Contractual Clauses | SCCAgreement lifecycle, module tracking |
| GDPR Art. 47 | Binding Corporate Rules | TransferBasis.BindingCorporateRules |
| GDPR Art. 49 | Derogations | TransferBasis.Derogation for specific situations |
| GDPR Art. 5(2) | Accountability principle | Event sourcing provides immutable audit trail |
| Schrems II (C-311/18) | CJEU judgment invalidating Privacy Shield | TIA requirement, supplementary measures |
| EU 2021 SCCs | Commission Decision 2021/914 | 4-module SCC structure |
| EDPB 01/2020 | Supplementary measures recommendations | Technical/Contractual/Organizational categorization |
| EU-US DPF | Data Privacy Framework (2023) | Adequacy decision for USA |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| `AggregateBase` | `Encina.DomainModeling/AggregateBase.cs` | Base class for event-sourced aggregates |
| `IAggregate` | `Encina.DomainModeling/IAggregate.cs` | Interface: Id, Version, UncommittedEvents |
| `IAggregateRepository<T>` | `Encina.Marten/IAggregateRepository.cs` | Load/Save/Create aggregate streams |
| `MartenAggregateRepository<T>` | `Encina.Marten/MartenAggregateRepository.cs` | Marten implementation of aggregate persistence |
| `Region` / `RegionRegistry` | `Encina.Compliance.DataResidency/Model/` | Country/region model |
| `IAdequacyDecisionProvider` | `Encina.Compliance.DataResidency/Abstractions/` | Adequacy decision lookup |
| `TransferLegalBasis` | `Encina.Compliance.DataResidency/Model/` | Legal basis enum for transfers |
| `IPipelineBehavior<,>` | `Encina.Messaging` | Pipeline behavior interface |
| `EncinaError` | `Encina/EncinaError.cs` | ROP error type |
| `ICacheProvider` | `Encina.Caching/ICacheProvider.cs` | Caching abstraction (8 providers) |
| `ConsentLogMessages` | `Encina.Compliance.Consent/Diagnostics/` | LoggerMessage.Define pattern |
| `ConsentHealthCheck` | `Encina.Compliance.Consent/Health/` | Health check pattern |
| `ConsentRequiredPipelineBehavior` | `Encina.Compliance.Consent/Pipeline/` | Pipeline behavior pattern |

### Event ID Allocation

| Package | Range | Status |
|---------|-------|--------|
| Encina.Security | 8000-8099 | In use (8000-8004) |
| Encina.Compliance.GDPR | 8100-8199 | In use (8100-8220, with overlap) |
| Encina.Compliance.Consent | 8200-8299 | In use (8200-8250) |
| Encina.Compliance.DataSubjectRights | 8300-8399 | Reserved (empty) |
| Encina.Marten.GDPR | 8400-8499 | In use (8400-8415) |
| **Encina.Compliance.CrossBorderTransfer** | **8500-8549** | **NEW — this module** |
| Future compliance modules | 8550-8999 | Available |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Core Models & Enums | 8 | Value objects, enums |
| Events | 3 | TIA, SCC, ApprovedTransfer event records |
| Aggregates | 3 | Event-sourced aggregates extending AggregateBase |
| Interfaces | 5 | Service abstractions |
| Read Models | 3 | Projected views |
| Services | 5 | Default implementations |
| Pipeline | 3 | Attribute, enforcement mode, behavior |
| Configuration | 4 | Options, validator, DI, Marten extensions |
| Diagnostics | 2 | ActivitySource/Meter, LoggerMessage |
| Health | 1 | Health check |
| Notifications | 2 | Expiration monitor, events |
| Errors | 1 | Error factory |
| Project files | 3 | .csproj, PublicAPI.Shipped/Unshipped |
| **Production subtotal** | **~43** | |
| Unit Tests | 10 | Aggregates, services, pipeline, options |
| Guard Tests | 4 | All service parameters |
| Property Tests | 2 | FsCheck aggregate invariants |
| Contract Tests | 2 | Service contracts |
| Integration Tests | 4 | Marten PostgreSQL tests |
| Justification Docs | 2 | Load, Benchmark |
| **Test subtotal** | **~24** | |
| **Grand Total** | **~67** | |

---

## Combined AI Agent Prompt

<details>
<summary>Complete Implementation Prompt (All Phases)</summary>

```
PROJECT CONTEXT:
Encina is a .NET 10 / C# 14 framework for building enterprise applications with Railway Oriented
Programming (Either<EncinaError, T>). You are implementing Encina.Compliance.CrossBorderTransfer
(Issue #412) — international data transfer validation for GDPR Articles 44-49 and Schrems II.

ARCHITECTURAL DECISION (ADR-019, #776):
Compliance modules use Marten event sourcing (PostgreSQL) for GDPR Art. 5(2) immutable audit trail.
Marten is a specialized event sourcing provider, analogous to how Redis is specialized for caching.
This is the FIRST compliance module to use this architecture — it establishes the pattern.

IMPLEMENTATION OVERVIEW:
1. Value objects & enums (SCCModule, TransferBasis, TIAStatus, SupplementaryMeasure, etc.)
2. Event-sourced aggregates (TIAAggregate, SCCAgreementAggregate, ApprovedTransferAggregate)
   extending AggregateBase from Encina.DomainModeling
3. Domain events (TIACreated, SCCAgreementRegistered, TransferApproved, etc.) — immutable records
4. Service interfaces + implementations (wrapping IAggregateRepository<T> from Encina.Marten)
5. Read models (TIAReadModel, SCCAgreementReadModel, ApprovedTransferReadModel)
6. TransferBlockingPipelineBehavior with [RequiresCrossBorderTransfer] attribute
7. Configuration: CrossBorderTransferOptions, ServiceCollectionExtensions
8. Cross-cutting: multi-tenancy, module isolation, caching (ICacheProvider from Encina.Caching with tag-based invalidation), audit trail (event sourcing)
9. Observability: ActivitySource, Meter, LoggerMessage (EventIds 8500-8549)
10. Health check, expiration monitoring (BackgroundService)
11. Full test suite: unit, guard, property, contract, integration (Marten/PostgreSQL)

KEY PATTERNS:
- Aggregates: extend AggregateBase, use RaiseEvent<T>(), implement Apply(object @event)
- Services: inject IAggregateRepository<T>, return ValueTask<Either<EncinaError, T>>
- Pipeline: ConcurrentDictionary attribute cache, 3-mode enforcement (Block/Warn/Disabled)
- Caching: ICacheProvider (from Encina.Caching) on services, tag-based write-through invalidation
- Events: sealed records, immutable facts
- Registration: TryAdd for overridable services, AddTransient for pipeline behavior

REFERENCE FILES:
- src/Encina.DomainModeling/AggregateBase.cs — ES base class
- src/Encina.DomainModeling/IAggregate.cs — ES interface
- src/Encina.Marten/IAggregateRepository.cs — Load/Save/Create
- src/Encina.Marten/MartenAggregateRepository.cs — Implementation
- src/Encina.Marten/ServiceCollectionExtensions.cs — AddAggregateRepository<T>()
- src/Encina.Caching/ICacheProvider.cs — Caching abstraction with tag-based invalidation
- src/Encina.Compliance.Consent/ — Complete compliance module reference
- src/Encina.Compliance.DataResidency/ — Region, AdequacyDecision types
- src/Encina.Compliance.Consent/Diagnostics/ConsentLogMessages.cs — LoggerMessage pattern
- src/Encina.Compliance.Consent/Pipeline/ConsentRequiredPipelineBehavior.cs — Pipeline behavior pattern
- src/Encina.Messaging/MessagingConfiguration.cs — Configuration flag pattern
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | **Caching** | ✅ Included | `ICacheProvider` from `Encina.Caching` for read model projections, tag-based invalidation, configurable TTL, distributed cache support (8 providers) |
| 2 | **OpenTelemetry** | ✅ Included | `ActivitySource` + `Meter` with dimensional counters in Phase 8 |
| 3 | **Structured Logging** | ✅ Included | `LoggerMessage.Define<>()` with EventIds 8500-8549 in Phase 8 |
| 4 | **Health Checks** | ✅ Included | `CrossBorderTransferHealthCheck` checking Marten connectivity in Phase 9 |
| 5 | **Validation** | ✅ Included | `CrossBorderTransferOptionsValidator` + input validation in services in Phase 6 |
| 6 | **Resilience** | ❌ N/A | No external system calls. DB resilience handled at Marten/PostgreSQL connection level (connection string retry, `NpgsqlDataSourceBuilder` resilience). Not the compliance module's responsibility. |
| 7 | **Distributed Locks** | ❌ N/A | Event sourcing uses optimistic concurrency via aggregate version numbers. Marten detects concurrent writes and raises `EventStreamUnexpectedMaxEventIdException`. No distributed lock needed. |
| 8 | **Transactions** | ✅ Included | Marten `IDocumentSession` provides implicit transactions — `SaveChangesAsync()` is atomic |
| 9 | **Idempotency** | ❌ N/A | Event sourcing provides natural idempotency: aggregates validate state before accepting commands (e.g., "TIA already completed" rejects duplicate completion). Optimistic concurrency detects duplicate writes. |
| 10 | **Multi-Tenancy** | ✅ Included | `TenantId` on all creation events, services resolve `ITenantContext`, queries filter by tenant in Phase 7 |
| 11 | **Module Isolation** | ✅ Included | `ModuleId` on all creation events, services resolve `IModuleContext`, queries filter by module in Phase 7 |
| 12 | **Audit Trail** | ✅ Included | Event sourcing IS the audit trail — every state change is an immutable event with timestamp, userId, correlationId. Marten metadata enrichment adds correlation/causation IDs automatically. |
