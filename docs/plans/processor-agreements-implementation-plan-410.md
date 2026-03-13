# Implementation Plan: `Encina.Compliance.ProcessorAgreements` — Data Processing Agreement Management (Art. 28)

> **Issue**: [#410](https://github.com/dlrivada/Encina/issues/410)
> **Type**: Feature
> **Complexity**: High (14 phases, 13 database providers + in-memory, ~200+ files)
> **Estimated Scope**: ~8,000-10,000 lines of production code + ~5,000-6,000 lines of tests

---

## Summary

Implement Data Processing Agreement (DPA) management covering GDPR Article 28 — processor obligations and contracts. This package provides a processor registry, DPA validation engine, sub-processor tracking, SCC (Standard Contractual Clauses) compliance, expiration alerting, and a `ProcessorValidationPipelineBehavior` that blocks requests targeting processors without valid DPAs.

The implementation follows the satellite-compliance architecture established by `Encina.Compliance.DPIA` and `Encina.Compliance.Consent`, delivering in-memory default stores, a pipeline behavior with enforcement modes, observability, health checks, and auto-registration.

**Provider category**: None — this is a provider-independent compliance package. Database providers can override the in-memory stores via the `TryAdd` pattern (same as DPIA).

**Affected packages**:

- `Encina.Compliance.ProcessorAgreements` (NEW)
- `Encina.Compliance.GDPR` (reference dependency for shared types)

---

## Design Choices

<details>
<summary><strong>1. Package Placement — New <code>Encina.Compliance.ProcessorAgreements</code> package</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New `Encina.Compliance.ProcessorAgreements` package** | Clean separation, own domain model, own pipeline behavior, own observability, independent versioning | New NuGet package, more projects to maintain |
| **B) Extend `Encina.Compliance.GDPR`** | Single package, shared config | Bloats GDPR core (~60+ files), violates SRP, Art. 28 is a distinct domain |
| **C) Merge with DPIA** | Both relate to processor compliance | Different concerns — DPIA is risk assessment, DPA is contractual compliance |

### Chosen Option: **A — New `Encina.Compliance.ProcessorAgreements` package**

### Rationale

- Art. 28 DPA management is a distinct GDPR domain with its own entities (processors, sub-processors, agreements), lifecycle (signing, renewal, termination), and enforcement logic
- Follows the established pattern: `Encina.Compliance.DPIA` (Art. 35), `Encina.Compliance.Consent` (Art. 7), `Encina.Compliance.DataSubjectRights` (Arts. 15-22) are all separate packages
- References `Encina.Compliance.GDPR` for shared types (`GDPRErrors` helpers, `ProcessingActivity`)
- Keeps the core GDPR package focused on fundamental compliance (lawful basis, processing activities, RoPA)
- In-memory defaults allow immediate use; satellite providers can override stores via `TryAdd` pattern

</details>

<details>
<summary><strong>2. Domain Model Design — Separate <code>Processor</code> and <code>DataProcessingAgreement</code> entities</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Rich `ProcessorRecord` with embedded DPA state** | Single entity encapsulates processor + agreement state, simple queries | Larger record, status transitions need careful design, can't track DPA history |
| **B) Separate `Processor` and `DataProcessingAgreement` entities** | Normalized model, supports multiple DPAs per processor over time, cleaner SRP, DPA renewal = new entity | More complex queries, two stores needed |
| **C) Simple flat record** | Minimal, fast to implement | Doesn't capture sub-processor relationships or DPA lifecycle |

### Chosen Option: **B — Separate `Processor` and `DataProcessingAgreement` entities**

### Rationale

- **Real-world modeling**: A processor is a long-lived entity (e.g., "Stripe"); DPAs are temporal agreements that get renewed, terminated, and replaced. Separating them models this lifecycle correctly.
- **DPA history**: When a DPA expires and is renewed, the old DPA record is preserved as `Terminated`/`Expired` while a new `Active` DPA is created. This provides a complete audit trail of all contractual relationships — critical for GDPR accountability (Art. 5(2)).
- **Cleaner queries**: "Show all processors" is simple; "Show all DPAs expiring in 30 days" is simple; "Show processor X's DPA history" is simple. No filtering within a monolithic entity.
- `Processor` holds identity: `Id`, `Name`, `Country`, `ContactEmail`, `SubProcessorIds`, `SubProcessorAuthorizationType`, `TenantId`, `ModuleId`
- `DataProcessingAgreement` holds contractual state: `Id`, `ProcessorId` (FK), `Status`, `SignedAtUtc`, `ExpiresAtUtc`, `MandatoryTerms`, `HasSCCs`, `ProcessingPurposes`
- `IProcessorRegistry` manages `Processor` entities
- `IDPAStore` manages `DataProcessingAgreement` entities (new interface)
- `IDPAValidator` queries both stores to produce `DPAValidationResult`

</details>

<details>
<summary><strong>3. DPA Validation Strategy — Composite validation with mandatory term checks</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Composite validation with per-term checks** | Granular Art. 28(3) compliance, identifies specific missing terms | More complex validation logic |
| **B) Simple exists-and-active check** | Fast, minimal code | Doesn't verify DPA content compliance |
| **C) External contract management integration** | Real-world DPA tracking | Over-engineered, external dependency |

### Chosen Option: **A — Composite validation with mandatory term checks**

### Rationale

- `IDPAValidator.ValidateAsync` returns `DPAValidationResult` with per-term compliance status
- Art. 28(3) defines 8 mandatory terms — each is tracked as a boolean in `DPAMandatoryTerms`
- `DPAValidationResult` reports: overall validity, list of missing/non-compliant terms, expiration warnings
- This enables actionable compliance reporting: "Processor X is missing terms 3 and 5"
- Simple `HasValidDPAAsync` provides the fast-path check for pipeline behavior (exists + active + not expired)

</details>

<details>
<summary><strong>4. Pipeline Behavior — <code>ProcessorValidationPipelineBehavior</code> with attribute-based routing</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Attribute-based with `[RequiresProcessor]` and store lookup** | Declarative, follows DPIA/Consent pattern, real-time validation | Store query per request for annotated types |
| **B) Configuration-based processor mapping** | No attributes needed, central config | Hard to track which requests use which processor |
| **C) Middleware-level validation** | Catches all requests | Too broad, can't distinguish processor-specific operations |

### Chosen Option: **A — Attribute-based with store lookup**

### Rationale

- `[RequiresProcessor(ProcessorId = "stripe")]` marks request types that depend on a specific processor
- Pipeline behavior checks: processor exists → DPA is active → DPA not expired → all mandatory terms present
- Three enforcement modes: `Block`, `Warn`, `Disabled` (same pattern as DPIA/Consent)
- Static `ConcurrentDictionary` cache for attribute resolution (zero reflection on hot path)
- Integrates with existing Encina pipeline infrastructure

</details>

<details>
<summary><strong>5. Sub-Processor Management — Parent ID + depth tracking with bounded hierarchy</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Flat list with parent reference** | Simple queries, no recursive traversal | Doesn't model deep hierarchies, can't trace full chain |
| **B) Tree structure with recursive queries** | Models real-world sub-processor chains | Complex queries, potential infinite recursion |
| **C) Parent ID + depth tracking** | Balanced — supports hierarchy with bounded depth, prevents infinite chains, enables "full chain" queries | Slightly more complex than flat list |

### Chosen Option: **C — Parent ID + depth tracking**

### Rationale

- Art. 28(2) requires prior specific or general written authorization for sub-processors — including sub-processors of sub-processors
- `Processor` entity has: `ParentProcessorId (string?)` (null = top-level processor) + `Depth (int)` (0 = top-level, 1 = direct sub-processor, 2 = sub-sub-processor)
- **Bounded depth**: configurable `MaxSubProcessorDepth` in options (default: 3), prevents unbounded chains
- `IProcessorRegistry.GetSubProcessorsAsync(processorId)` returns direct sub-processors (depth = parent.Depth + 1)
- `IProcessorRegistry.GetFullSubProcessorChainAsync(processorId)` returns all descendants recursively (bounded by MaxSubProcessorDepth)
- Validation: `RegisterProcessorAsync` rejects registrations that would exceed `MaxSubProcessorDepth`
- `SubProcessorAuthorizationType` enum (`Specific`, `General`) tracks the authorization model per relationship
- Enables compliance queries: "Show full processor chain for Stripe" → Stripe → Stripe Sub-1 → Stripe Sub-1-A
- Sub-processor change notifications handled via domain events (`SubProcessorAddedNotification`, `SubProcessorRemovedNotification`)

</details>

<details>
<summary><strong>6. DPA Expiration Alerting — Scheduled message via Encina.Scheduling</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Background hosted service with periodic checks** | Follows DPIA expiration monitoring pattern, reliable | Reinvents scheduling, misses distributed lock/retry/persistence that Scheduling already provides |
| **B) On-demand check during pipeline validation** | No background service | Doesn't alert until a request hits the processor |
| **C) Scheduled message via Encina.Scheduling** | Leverages existing infrastructure, gets persistence/retry/distributed locks for free, promotes internal integration, less custom code | Adds dependency on `Encina.Messaging` (scheduling) |

### Chosen Option: **C — Scheduled message via Encina.Scheduling**

### Rationale

- **Internal coherence**: Encina already has scheduling infrastructure — creating a separate `IHostedService` with `PeriodicTimer` duplicates capability that exists and is well-tested
- **Free benefits**: Encina.Scheduling provides persistence (survives restarts), distributed locks (no double-execution in multi-instance), retry on failure, observability — all of which would need to be hand-built in a hosted service
- **Less code**: Instead of `DPAExpirationMonitorService` (~100+ lines with PeriodicTimer, IDisposable, CancellationToken plumbing), we define a `CheckDPAExpirationCommand : ICommand<Unit>` and a handler
- **Validates the pattern**: A compliance package using Encina.Scheduling internally serves as a real-world example and validates the scheduling API
- Implementation:
  - `CheckDPAExpirationCommand : ICommand<Unit>` — scheduled command
  - `CheckDPAExpirationHandler : ICommandHandler<CheckDPAExpirationCommand, Unit>` — queries `IDPAStore.GetExpiringAsync(threshold)`, publishes `DPAExpiringNotification` / `DPAExpiredNotification`
  - Registration in `ServiceCollectionExtensions`: when `EnableExpirationMonitoring = true`, schedules a recurring `CheckDPAExpirationCommand` with `ExpirationCheckInterval`
- Only registered when `EnableExpirationMonitoring = true` (opt-in)
- Dependency: `Encina.Messaging` (already a transitive dependency via Encina core)

</details>

---

## Implementation Phases

### Phase 1: Core Models, Enums & Domain Records

> **Goal**: Establish the foundational types that all other phases depend on — separate `Processor` and `DataProcessingAgreement` entities (DC 2), sub-processor depth hierarchy (DC 5).

<details>
<summary><strong>Tasks</strong></summary>

#### New project: `src/Encina.Compliance.ProcessorAgreements/`

1. **Create project file** `Encina.Compliance.ProcessorAgreements.csproj`
   - Target: `net10.0`
   - Dependencies: `Encina.Compliance.GDPR`, `Encina.Messaging` (for scheduling, DC 6), `LanguageExt.Core`, `Microsoft.Extensions.Logging.Abstractions`, `Microsoft.Extensions.Options`
   - Enable nullable, implicit usings, XML doc

2. **Enums** (`Model/` folder):
   - `DPAStatus` — `Active`, `Expired`, `PendingRenewal`, `Terminated`
   - `SubProcessorAuthorizationType` — `Specific`, `General` (Art. 28(2))
   - `ProcessorAgreementEnforcementMode` — `Block`, `Warn`, `Disabled`

3. **Domain records** (`Model/` folder):

   **`Processor`** — sealed record (processor identity, long-lived entity):
   - `Id (string)`, `Name (string)`, `Country (string)`, `ContactEmail (string?)`
   - `ParentProcessorId (string?)` — null for top-level processors; references parent processor Id for sub-processors (DC 5)
   - `Depth (int)` — 0 = top-level, 1 = direct sub-processor, 2 = sub-sub-processor, etc. (DC 5)
   - `SubProcessorAuthorizationType` — `Specific` or `General` (Art. 28(2))
   - `TenantId (string?)`, `ModuleId (string?)`
   - `CreatedAtUtc (DateTimeOffset)`, `LastUpdatedAtUtc (DateTimeOffset)`

   **`DataProcessingAgreement`** — sealed record (contractual state, temporal entity):
   - `Id (string)`, `ProcessorId (string)` — FK to Processor
   - `Status (DPAStatus)` — Active, Expired, PendingRenewal, Terminated
   - `SignedAtUtc (DateTimeOffset)`, `ExpiresAtUtc (DateTimeOffset?)`
   - `MandatoryTerms (DPAMandatoryTerms)` — Art. 28(3) compliance
   - `HasSCCs (bool)` — Standard Contractual Clauses present
   - `ProcessingPurposes (IReadOnlyList<string>)` — documented processing purposes
   - `TenantId (string?)`, `ModuleId (string?)`
   - `CreatedAtUtc (DateTimeOffset)`, `LastUpdatedAtUtc (DateTimeOffset)`
   - Computed: `IsActive(DateTimeOffset nowUtc)` → `Status == Active && (ExpiresAtUtc is null || ExpiresAtUtc > nowUtc)`

   **`DPAMandatoryTerms`** — sealed record (Art. 28(3) eight mandatory terms):
   - `ProcessOnDocumentedInstructions (bool)` — Art. 28(3)(a)
   - `ConfidentialityObligations (bool)` — Art. 28(3)(b)
   - `SecurityMeasures (bool)` — Art. 28(3)(c)
   - `SubProcessorRequirements (bool)` — Art. 28(3)(d)
   - `DataSubjectRightsAssistance (bool)` — Art. 28(3)(e)
   - `ComplianceAssistance (bool)` — Art. 28(3)(f)
   - `DataDeletionOrReturn (bool)` — Art. 28(3)(g)
   - `AuditRights (bool)` — Art. 28(3)(h)
   - Computed: `IsFullyCompliant` → all 8 terms are `true`
   - `MissingTerms` → list of term names that are `false`

   **`DPAValidationResult`** — sealed record:
   - `ProcessorId (string)`, `DPAId (string?)`, `IsValid (bool)`, `Status (DPAStatus?)`
   - `MissingTerms (IReadOnlyList<string>)`, `Warnings (IReadOnlyList<string>)`
   - `DaysUntilExpiration (int?)`, `ValidatedAtUtc (DateTimeOffset)`

   **`ProcessorAgreementAuditEntry`** — sealed record:
   - `Id (string)`, `ProcessorId (string)`, `DPAId (string?)`, `Action (string)`, `Detail (string?)`
   - `PerformedByUserId (string?)`, `OccurredAtUtc (DateTimeOffset)`

4. **Notification records** (`Notifications/` folder):
   - `ProcessorRegisteredNotification : INotification` — `ProcessorId`, `ProcessorName`, `OccurredAtUtc`
   - `DPASignedNotification : INotification` — `ProcessorId`, `DPAId`, `ProcessorName`, `SignedAtUtc`, `OccurredAtUtc`
   - `DPAExpiringNotification : INotification` — `ProcessorId`, `DPAId`, `ProcessorName`, `ExpiresAtUtc`, `DaysUntilExpiration`, `OccurredAtUtc`
   - `DPAExpiredNotification : INotification` — `ProcessorId`, `DPAId`, `ProcessorName`, `ExpiredAtUtc`, `OccurredAtUtc`
   - `DPATerminatedNotification : INotification` — `ProcessorId`, `DPAId`, `ProcessorName`, `OccurredAtUtc`
   - `SubProcessorAddedNotification : INotification` — `ProcessorId`, `SubProcessorId`, `Depth`, `OccurredAtUtc`
   - `SubProcessorRemovedNotification : INotification` — `ProcessorId`, `SubProcessorId`, `OccurredAtUtc`

5. **`PublicAPI.Unshipped.txt`** — Add all public types

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of Encina.Compliance.ProcessorAgreements (Issue #410).

CONTEXT:
- Encina is a .NET 10 / C# 14 library using Railway Oriented Programming (Either<EncinaError, T>)
- This is a NEW project: src/Encina.Compliance.ProcessorAgreements/
- Reference existing patterns in src/Encina.Compliance.DPIA/Model/ and src/Encina.Compliance.Consent/
- All domain models are sealed records with XML documentation
- Use LanguageExt for Option<T> and Either<L, R>
- Timestamps use DateTimeOffset with AtUtc suffix convention

DESIGN DECISIONS:
- DC 2: SEPARATE entities — Processor (identity, long-lived) and DataProcessingAgreement (contractual state, temporal)
- DC 5: DEPTH TRACKING — Processor has ParentProcessorId (string?) + Depth (int), bounded hierarchy
- DC 6: Encina.Scheduling — project depends on Encina.Messaging (no IHostedService)

TASK:
Create the project file and all model types listed in Phase 1 Tasks:
- 3 enums: DPAStatus, SubProcessorAuthorizationType, ProcessorAgreementEnforcementMode
- 5 domain records: Processor (with ParentProcessorId/Depth), DataProcessingAgreement, DPAMandatoryTerms, DPAValidationResult, ProcessorAgreementAuditEntry
- 7 notification records implementing INotification
- PublicAPI.Unshipped.txt

KEY RULES:
- Target net10.0, enable nullable, enable implicit usings
- All types are sealed records (not classes)
- All public types need XML documentation with <summary>, <remarks>, and GDPR Article 28 references
- Processor entity: ParentProcessorId is null for top-level, Depth is 0 for top-level
- DataProcessingAgreement: ProcessorId is FK to Processor.Id, owns DPAStatus and DPAMandatoryTerms
- DataProcessingAgreement.IsActive(nowUtc) is a method (not property) — checks Status == Active && not expired
- DPAMandatoryTerms has 8 bool properties mapping exactly to Art. 28(3)(a)-(h)
- DPAMandatoryTerms.IsFullyCompliant computed property checks all 8 terms
- DPAValidationResult includes both ProcessorId and DPAId (nullable — no DPA may exist)
- ProcessorAgreementAuditEntry includes both ProcessorId and DPAId (nullable)
- Notification records implement INotification from Encina core
- SubProcessorAddedNotification includes Depth field
- Add PublicAPI.Unshipped.txt with all public symbols

REFERENCE FILES:
- src/Encina.Compliance.DPIA/Model/DPIAAssessment.cs (sealed record pattern)
- src/Encina.Compliance.DPIA/Model/DPIAAssessmentStatus.cs (enum pattern)
- src/Encina.Compliance.DPIA/Notifications/DPIAAssessmentCompleted.cs (notification pattern)
- src/Encina.Compliance.DPIA/Encina.Compliance.DPIA.csproj (project file)
```

</details>

---

### Phase 2: Core Interfaces, Attributes & Error Codes

> **Goal**: Define the public API surface — `IProcessorRegistry` (processor CRUD + depth hierarchy), `IDPAStore` (DPA CRUD), `IDPAValidator`, attributes, and error factory.

<details>
<summary><strong>Tasks</strong></summary>

1. **Attribute** (`Attributes/` folder):
   - `RequiresProcessorAttribute` — `[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]`
     - Properties: `ProcessorId (string)` — required, identifies the processor this request depends on
     - Purpose: marks request types that should be validated against the processor registry

2. **Core interfaces** (`Abstractions/` folder):

   **`IProcessorRegistry`** — manages `Processor` entities (identity + hierarchy):
   - `RegisterProcessorAsync(Processor, CancellationToken)` → `Either<EncinaError, Unit>`
   - `GetProcessorAsync(string processorId, CancellationToken)` → `Either<EncinaError, Option<Processor>>`
   - `GetAllProcessorsAsync(CancellationToken)` → `Either<EncinaError, IReadOnlyList<Processor>>`
   - `UpdateProcessorAsync(Processor, CancellationToken)` → `Either<EncinaError, Unit>`
   - `RemoveProcessorAsync(string processorId, CancellationToken)` → `Either<EncinaError, Unit>`
   - `GetSubProcessorsAsync(string processorId, CancellationToken)` → `Either<EncinaError, IReadOnlyList<Processor>>` — direct children (Depth = parent.Depth + 1)
   - `GetFullSubProcessorChainAsync(string processorId, CancellationToken)` → `Either<EncinaError, IReadOnlyList<Processor>>` — all descendants recursively, bounded by `MaxSubProcessorDepth` (DC 5)

   **`IDPAStore`** — manages `DataProcessingAgreement` entities (NEW — DC 2):
   - `AddAsync(DataProcessingAgreement, CancellationToken)` → `Either<EncinaError, Unit>`
   - `GetByIdAsync(string dpaId, CancellationToken)` → `Either<EncinaError, Option<DataProcessingAgreement>>`
   - `GetByProcessorIdAsync(string processorId, CancellationToken)` → `Either<EncinaError, IReadOnlyList<DataProcessingAgreement>>` — all DPAs for a processor (history)
   - `GetActiveByProcessorIdAsync(string processorId, CancellationToken)` → `Either<EncinaError, Option<DataProcessingAgreement>>` — current active DPA
   - `UpdateAsync(DataProcessingAgreement, CancellationToken)` → `Either<EncinaError, Unit>`
   - `GetByStatusAsync(DPAStatus, CancellationToken)` → `Either<EncinaError, IReadOnlyList<DataProcessingAgreement>>`
   - `GetExpiringAsync(DateTimeOffset threshold, CancellationToken)` → `Either<EncinaError, IReadOnlyList<DataProcessingAgreement>>` — DPAs expiring before threshold

   **`IDPAValidator`** — DPA validation (queries both IProcessorRegistry + IDPAStore):
   - `ValidateAsync(string processorId, CancellationToken)` → `Either<EncinaError, DPAValidationResult>`
   - `HasValidDPAAsync(string processorId, CancellationToken)` → `Either<EncinaError, bool>`
   - `ValidateAllAsync(CancellationToken)` → `Either<EncinaError, IReadOnlyList<DPAValidationResult>>`

   **`IProcessorAuditStore`** — audit trail:
   - `RecordAsync(ProcessorAgreementAuditEntry, CancellationToken)` → `Either<EncinaError, Unit>`
   - `GetAuditTrailAsync(string processorId, CancellationToken)` → `Either<EncinaError, IReadOnlyList<ProcessorAgreementAuditEntry>>`

3. **Error codes** (`ProcessorAgreementErrors.cs`):
   - Error code prefix: `processor.`
   - Codes:
     - `processor.not_found` — processor ID not in registry
     - `processor.already_exists` — duplicate processor ID
     - `processor.dpa_not_found` — DPA ID not found
     - `processor.dpa_missing` — no active DPA for processor
     - `processor.dpa_expired` — DPA has expired
     - `processor.dpa_terminated` — DPA was terminated
     - `processor.dpa_pending_renewal` — DPA is pending renewal
     - `processor.dpa_incomplete` — mandatory terms not fully met (with list of missing terms)
     - `processor.sub_processor_unauthorized` — sub-processor not authorized
     - `processor.sub_processor_depth_exceeded` — registration would exceed `MaxSubProcessorDepth` (DC 5)
     - `processor.scc_required` — cross-border transfer without SCCs
     - `processor.store_error` — infrastructure/store error
     - `processor.validation_failed` — general validation failure
   - Follow `DPIAErrors.cs` pattern: `public static class ProcessorAgreementErrors` with factory methods

4. **`PublicAPI.Unshipped.txt`** — Update with all new public symbols

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of Encina.Compliance.ProcessorAgreements (Issue #410).

CONTEXT:
- Phase 1 models are already implemented in src/Encina.Compliance.ProcessorAgreements/Model/
- Encina uses Railway Oriented Programming: all store/handler methods return ValueTask<Either<EncinaError, T>>
- LanguageExt provides Option<T>, Either<L, R>, Unit
- Error codes follow the pattern in src/Encina.Compliance.DPIA/DPIAErrors.cs

DESIGN DECISIONS:
- DC 2: Processor and DataProcessingAgreement are SEPARATE entities with separate stores
- DC 5: IProcessorRegistry has GetSubProcessorsAsync (direct) + GetFullSubProcessorChainAsync (recursive, bounded)
- Error code "processor.sub_processor_depth_exceeded" for depth violations

TASK:
Create the attribute, all interfaces, and error factory listed in Phase 2 Tasks:
- [RequiresProcessor] attribute
- IProcessorRegistry (7 methods — Processor CRUD + hierarchy queries)
- IDPAStore (7 methods — DataProcessingAgreement CRUD + status/expiration queries) — NEW
- IDPAValidator (3 methods — queries both stores)
- IProcessorAuditStore (2 methods — audit trail)
- ProcessorAgreementErrors static factory

KEY RULES:
- [RequiresProcessor] targets classes (AttributeTargets.Class) — marks request types
- ProcessorId is a required string property on the attribute
- All interface methods take CancellationToken as last parameter
- IProcessorRegistry manages Processor entities ONLY (no DPA state)
- IDPAStore manages DataProcessingAgreement entities (separate store, DC 2)
- IDPAStore.GetActiveByProcessorIdAsync returns Option<DataProcessingAgreement> — at most ONE active DPA per processor
- IDPAStore.GetByProcessorIdAsync returns all DPAs (history) for a processor
- IDPAStore.GetExpiringAsync returns DPAs with ExpiresAtUtc <= threshold AND Status == Active
- IDPAValidator.HasValidDPAAsync is lightweight — used in pipeline behavior hot path
- GetFullSubProcessorChainAsync recursively traverses ParentProcessorId hierarchy (bounded by MaxSubProcessorDepth)
- IProcessorAuditStore.GetAuditTrailAsync includes entries for both processor and DPA operations
- ProcessorAgreementErrors includes "sub_processor_depth_exceeded" for depth validation failures
- All errors include GDPR article references in metadata
- All interfaces need comprehensive XML documentation with Art. 28 references

REFERENCE FILES:
- src/Encina.Compliance.DPIA/DPIAErrors.cs (error factory pattern)
- src/Encina.Compliance.DPIA/Abstractions/IDPIAStore.cs (store interface pattern)
- src/Encina.Compliance.DPIA/Abstractions/IDPIAAuditStore.cs (audit store pattern)
- src/Encina.Compliance.DPIA/Attributes/RequiresDPIAAttribute.cs (attribute pattern)
```

</details>

---

### Phase 3: Default Implementations & In-Memory Stores

> **Goal**: Provide working implementations for development/testing without database dependencies — separate stores for `Processor` and `DataProcessingAgreement` (DC 2), depth validation (DC 5).

<details>
<summary><strong>Tasks</strong></summary>

1. **In-memory stores**:

   **`InMemoryProcessorRegistry : IProcessorRegistry`** — `ConcurrentDictionary<string, Processor>`:
   - `RegisterProcessorAsync`: validates depth ≤ `MaxSubProcessorDepth`, validates ParentProcessorId exists if non-null, rejects duplicates
   - `GetSubProcessorsAsync(processorId)`: filters by `ParentProcessorId == processorId` (direct children)
   - `GetFullSubProcessorChainAsync(processorId)`: BFS/DFS traversal of `ParentProcessorId` links, bounded by `MaxSubProcessorDepth` (DC 5)
   - Depth validation on registration: if `ParentProcessorId` is set, `Depth` must equal `parent.Depth + 1` and must be ≤ `MaxSubProcessorDepth`

   **`InMemoryDPAStore : IDPAStore`** — `ConcurrentDictionary<string, DataProcessingAgreement>` (NEW — DC 2):
   - `GetActiveByProcessorIdAsync`: filters by `ProcessorId == x && Status == Active`
   - `GetExpiringAsync(threshold)`: filters where `ExpiresAtUtc is not null && ExpiresAtUtc <= threshold && Status == Active`
   - `GetByProcessorIdAsync`: returns all DPAs for a processor (full history)
   - `GetByStatusAsync`: filters by `Status`

   **`InMemoryProcessorAuditStore : IProcessorAuditStore`** — `ConcurrentDictionary<string, List<ProcessorAgreementAuditEntry>>`

2. **DPA Validator** (`DefaultDPAValidator.cs`):
   - Implements `IDPAValidator`
   - Dependencies: `IProcessorRegistry`, `IDPAStore`, `IOptions<ProcessorAgreementOptions>`, `TimeProvider`, `ILogger`
   - `ValidateAsync` flow:
     1. Get processor from `IProcessorRegistry`
     2. Get active DPA from `IDPAStore.GetActiveByProcessorIdAsync`
     3. If no active DPA → invalid, return result with `dpa_missing`
     4. Check DPA status (Active, Expired, Terminated, PendingRenewal)
     5. Check DPA expiration against `TimeProvider.GetUtcNow()`
     6. Check mandatory terms via `DPAMandatoryTerms.IsFullyCompliant`
     7. Check sub-processor authorization if `RequireSubProcessorAuthorization` is enabled
     8. Generate warnings for approaching expiration (`AlertBeforeExpirationDays`)
     9. Return `DPAValidationResult` with all findings (includes DPAId)
   - `HasValidDPAAsync`: lightweight — processor exists + active DPA exists + not expired + terms compliant

3. **Persistence entities & mappers**:
   - `ProcessorEntity` — mutable class (Id, Name, Country, ContactEmail, ParentProcessorId, Depth, SubProcessorAuthorizationType, TenantId, ModuleId, CreatedAtUtc, LastUpdatedAtUtc)
   - `ProcessorMapper` — `ToEntity(Processor)` / `ToDomain(ProcessorEntity)`
   - `DataProcessingAgreementEntity` — mutable class (Id, ProcessorId, Status, SignedAtUtc, ExpiresAtUtc, 8 mandatory term booleans, HasSCCs, ProcessingPurposes serialized as JSON, TenantId, ModuleId, CreatedAtUtc, LastUpdatedAtUtc)
   - `DataProcessingAgreementMapper` — `ToEntity(DataProcessingAgreement)` / `ToDomain(DataProcessingAgreementEntity)`
   - `ProcessorAgreementAuditEntryEntity` — mutable class
   - `ProcessorAgreementAuditEntryMapper` — `ToEntity` / `ToDomain`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of Encina.Compliance.ProcessorAgreements (Issue #410).

CONTEXT:
- Phase 1 (models) and Phase 2 (interfaces, attributes, errors) are already implemented
- Encina uses ROP: all methods return ValueTask<Either<EncinaError, T>>
- In-memory stores use ConcurrentDictionary for thread safety
- TimeProvider is injected for testable time-dependent logic

DESIGN DECISIONS:
- DC 2: Separate InMemoryProcessorRegistry (Processor entities) and InMemoryDPAStore (DataProcessingAgreement entities)
- DC 5: Depth validation in RegisterProcessorAsync; GetFullSubProcessorChainAsync traverses ParentProcessorId hierarchy
- DefaultDPAValidator queries BOTH IProcessorRegistry and IDPAStore

TASK:
Create all default implementations listed in Phase 3 Tasks:
- InMemoryProcessorRegistry (with depth validation and hierarchy traversal)
- InMemoryDPAStore (NEW — DataProcessingAgreement CRUD)
- InMemoryProcessorAuditStore
- DefaultDPAValidator (queries both stores)
- Persistence entities and mappers (Processor + DPA separate)

KEY RULES:
- InMemoryProcessorRegistry uses ConcurrentDictionary<string, Processor>
- RegisterProcessorAsync MUST validate: if ParentProcessorId is set, parent must exist AND Depth must equal parent.Depth + 1 AND Depth ≤ MaxSubProcessorDepth
- Return "processor.sub_processor_depth_exceeded" error if depth check fails
- GetSubProcessorsAsync filters ParentProcessorId == given ID (direct children)
- GetFullSubProcessorChainAsync: BFS traversal, collect all descendants, bounded by MaxSubProcessorDepth
- InMemoryDPAStore uses ConcurrentDictionary<string, DataProcessingAgreement>
- GetActiveByProcessorIdAsync returns at most ONE active DPA (Option<DataProcessingAgreement>)
- DefaultDPAValidator needs BOTH IProcessorRegistry and IDPAStore injected
- HasValidDPAAsync is the fast-path: processor exists + active DPA + not expired + fully compliant
- Entity classes use public get/set properties (mutable for ORMs)
- DPAMandatoryTerms serialized as 8 individual boolean columns in DataProcessingAgreementEntity
- ProcessingPurposes serialized as JSON string in entity
- All constructors validate parameters with ArgumentNullException.ThrowIfNull

REFERENCE FILES:
- src/Encina.Compliance.DPIA/InMemoryDPIAStore.cs (ConcurrentDictionary pattern)
- src/Encina.Compliance.DPIA/InMemoryDPIAAuditStore.cs (audit store pattern)
- src/Encina.Compliance.DPIA/DefaultDPIAAssessmentEngine.cs (validation engine pattern)
- src/Encina.Compliance.DPIA/DPIAAssessmentEntity.cs (entity pattern)
- src/Encina.Compliance.DPIA/DPIAAssessmentMapper.cs (mapper pattern)
```

</details>

---

### Phase 4: MessagingConfiguration & `UseProcessorAgreements` Flag

> **Goal**: Add `UseProcessorAgreements` property to `MessagingConfiguration` and wire it into `IsAnyEnabled` check, following the DPIA `UseDPIA` pattern.

<details>
<summary><strong>Tasks</strong></summary>

1. **`MessagingConfiguration.cs`** (`src/Encina.Messaging/`):
   - Add `public bool UseProcessorAgreements { get; set; }` property
   - Include in `IsAnyEnabled` check (same location as `UseDPIA`)

2. **PublicAPI.Unshipped.txt** — Update for Encina.Messaging

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of Encina.Compliance.ProcessorAgreements (Issue #410).

CONTEXT:
- Phases 1-3 are implemented (core models, interfaces, in-memory stores)
- MessagingConfiguration needs UseProcessorAgreements flag (same as UseDPIA)
- This flag is needed by provider implementations in subsequent phases

TASK:
- Add UseProcessorAgreements to MessagingConfiguration.cs
- Add to IsAnyEnabled computed property
- Update PublicAPI.Unshipped.txt

REFERENCE:
- src/Encina.Messaging/MessagingConfiguration.cs — UseDPIA property and IsAnyEnabled
```

</details>

---

### Phase 5: Entity Framework Core Provider

> **Goal**: Implement `IProcessorRegistry`, `IDPAStore`, and `IProcessorAuditStore` for EF Core — supports SQLite, SQL Server, PostgreSQL, and MySQL (4 of 13 providers).

<details>
<summary><strong>Tasks</strong></summary>

1. **Store implementations** (`src/Encina.EntityFrameworkCore/ProcessorAgreements/`):

   **`ProcessorRegistryEF : IProcessorRegistry`**:
   - Dependencies: `DbContext`, `ProcessorMapper`, `ILogger`
   - All 7 methods using EF Core LINQ queries
   - `GetSubProcessorsAsync`: `Where(p => p.ParentProcessorId == processorId)`
   - `GetFullSubProcessorChainAsync`: recursive query (load all, BFS in memory) or CTE via raw SQL
   - `RegisterProcessorAsync`: depth validation before `AddAsync` + `SaveChangesAsync`

   **`DPAStoreEF : IDPAStore`**:
   - Dependencies: `DbContext`, `DataProcessingAgreementMapper`, `ILogger`
   - All 7 methods using EF Core LINQ queries
   - `GetActiveByProcessorIdAsync`: `Where(d => d.ProcessorId == x && d.StatusValue == Active)`
   - `GetExpiringAsync`: `Where(d => d.ExpiresAtUtc != null && d.ExpiresAtUtc <= threshold && d.StatusValue == Active)`

   **`ProcessorAuditStoreEF : IProcessorAuditStore`**:
   - Dependencies: `DbContext`, `ProcessorAgreementAuditEntryMapper`, `ILogger`
   - 2 methods: `RecordAsync` + `GetAuditTrailAsync`

2. **Entity configurations** (`src/Encina.EntityFrameworkCore/ProcessorAgreements/`):
   - `ProcessorEntityConfiguration : IEntityTypeConfiguration<ProcessorEntity>` — indexes on ParentProcessorId, TenantId
   - `DataProcessingAgreementEntityConfiguration : IEntityTypeConfiguration<DataProcessingAgreementEntity>` — indexes on ProcessorId, Status, ExpiresAtUtc
   - `ProcessorAgreementAuditEntryEntityConfiguration : IEntityTypeConfiguration<ProcessorAgreementAuditEntryEntity>` — index on ProcessorId

3. **ModelBuilder extension**:
   - `ProcessorAgreementModelBuilderExtensions.ApplyProcessorAgreementConfiguration(this ModelBuilder)`

4. **DI registration** (`ServiceCollectionExtensions.cs`):
   - Add `UseProcessorAgreements` check:
     ```csharp
     if (config.UseProcessorAgreements)
     {
         services.TryAddScoped<IProcessorRegistry, ProcessorAgreements.ProcessorRegistryEF>();
         services.TryAddScoped<IDPAStore, ProcessorAgreements.DPAStoreEF>();
         services.TryAddScoped<IProcessorAuditStore, ProcessorAgreements.ProcessorAuditStoreEF>();
     }
     ```

5. **PublicAPI.Unshipped.txt** — Update with new public symbols

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of Encina.Compliance.ProcessorAgreements (Issue #410).

CONTEXT:
- Phases 1-3 are implemented (core package with in-memory stores)
- Phase 4 added UseProcessorAgreements to MessagingConfiguration
- Now implementing EF Core provider for all 3 stores: IProcessorRegistry, IDPAStore, IProcessorAuditStore
- Follow EXACTLY the pattern in src/Encina.EntityFrameworkCore/DPIA/ (DPIAStoreEF, DPIAAuditStoreEF, entity configs, ModelBuilder extensions)
- EF Core provider covers 4 databases: SQLite, SQL Server, PostgreSQL, MySQL (provider-agnostic via EF Core)

DESIGN DECISIONS:
- DC 2: Separate ProcessorRegistryEF and DPAStoreEF (not one combined store)
- DC 5: GetFullSubProcessorChainAsync needs recursive query — BFS in memory after loading all processors for the tenant

TASK:
Create in src/Encina.EntityFrameworkCore/ProcessorAgreements/:
- ProcessorRegistryEF.cs (7 methods)
- DPAStoreEF.cs (7 methods)
- ProcessorAuditStoreEF.cs (2 methods)
- ProcessorEntityConfiguration.cs
- DataProcessingAgreementEntityConfiguration.cs
- ProcessorAgreementAuditEntryEntityConfiguration.cs
- ProcessorAgreementModelBuilderExtensions.cs
Update: ServiceCollectionExtensions.cs (add UseProcessorAgreements block), PublicAPI.Unshipped.txt

KEY RULES:
- All methods return ValueTask<Either<EncinaError, T>>
- Catch OperationCanceledException and re-throw
- Convert other exceptions to EncinaError with "processor.store_error" code
- Use ProcessorMapper, DataProcessingAgreementMapper, ProcessorAgreementAuditEntryMapper (from core package)
- Entity types use persistence entities from core package (ProcessorEntity, DataProcessingAgreementEntity, ProcessorAgreementAuditEntryEntity)
- DPAMandatoryTerms serialized as 8 individual boolean columns (not JSON)
- ProcessingPurposes serialized as JSON string column

REFERENCE FILES:
- src/Encina.EntityFrameworkCore/DPIA/DPIAStoreEF.cs (exact pattern to follow)
- src/Encina.EntityFrameworkCore/DPIA/DPIAAuditStoreEF.cs
- src/Encina.EntityFrameworkCore/DPIA/DPIAAssessmentEntityConfiguration.cs
- src/Encina.EntityFrameworkCore/DPIA/DPIAModelBuilderExtensions.cs
- src/Encina.EntityFrameworkCore/ServiceCollectionExtensions.cs (UseDPIA registration pattern)
```

</details>

---

### Phase 6: ADO.NET Providers (4 databases)

> **Goal**: Implement `IProcessorRegistry`, `IDPAStore`, and `IProcessorAuditStore` for ADO.NET — separate implementations for SQLite, SQL Server, PostgreSQL, and MySQL (4 of 13 providers).

<details>
<summary><strong>Tasks</strong></summary>

1. **SQL Server** (`src/Encina.ADO.SqlServer/ProcessorAgreements/`):
   - `ProcessorRegistryADO.cs` — raw ADO.NET with `@param` parameters, `MERGE` for upsert, `DATETIME2(7)`
   - `DPAStoreADO.cs` — DPA CRUD + status/expiration queries
   - `ProcessorAuditStoreADO.cs` — audit trail
   - `Scripts/025_CreateProcessorsTable.sql` — `NVARCHAR(36)` IDs, `INT` depth, `NVARCHAR(MAX)` JSON
   - `Scripts/026_CreateDataProcessingAgreementsTable.sql` — 8 `BIT` columns for mandatory terms
   - `Scripts/027_CreateProcessorAgreementAuditEntriesTable.sql`

2. **SQLite** (`src/Encina.ADO.Sqlite/ProcessorAgreements/`):
   - Same 3 store classes + 3 SQL scripts
   - `TEXT` for all strings, `INTEGER` for enums/depth, ISO 8601 `ToString("O")` for DateTime
   - **CRITICAL**: Never use `datetime('now')` — use parameterized `@NowUtc` with `DateTime.UtcNow`

3. **PostgreSQL** (`src/Encina.ADO.PostgreSQL/ProcessorAgreements/`):
   - Same 3 store classes + 3 SQL scripts
   - `TIMESTAMPTZ` for timestamps, `JSONB` for ProcessingPurposes, lowercase column names

4. **MySQL** (`src/Encina.ADO.MySQL/ProcessorAgreements/`):
   - Same 3 store classes + 3 SQL scripts
   - Backtick identifiers, `DATETIME` for timestamps

5. **DI registration** — Update each provider's `ServiceCollectionExtensions.cs`:
   ```csharp
   if (config.UseProcessorAgreements)
   {
       services.TryAddScoped<IProcessorRegistry, ProcessorAgreements.ProcessorRegistryADO>();
       services.TryAddScoped<IDPAStore, ProcessorAgreements.DPAStoreADO>();
       services.TryAddScoped<IProcessorAuditStore, ProcessorAgreements.ProcessorAuditStoreADO>();
   }
   ```

6. **PublicAPI.Unshipped.txt** — Update for each provider package

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of Encina.Compliance.ProcessorAgreements (Issue #410).

CONTEXT:
- Phases 1-3 (core) and Phase 4 (MessagingConfiguration) and Phase 5 (EF Core) are implemented
- Now implementing ADO.NET provider for all 4 databases: SqlServer, SQLite, PostgreSQL, MySQL
- Follow EXACTLY the pattern in src/Encina.ADO.{Provider}/DPIA/ (DPIAStoreADO, DPIAAuditStoreADO, SQL scripts)
- Each provider has its own project and SQL dialect

TASK:
For EACH of 4 ADO.NET providers, create in src/Encina.ADO.{Provider}/ProcessorAgreements/:
- ProcessorRegistryADO.cs (7 methods with raw SQL)
- DPAStoreADO.cs (7 methods with raw SQL)
- ProcessorAuditStoreADO.cs (2 methods with raw SQL)
- Scripts/025_CreateProcessorsTable.sql
- Scripts/026_CreateDataProcessingAgreementsTable.sql
- Scripts/027_CreateProcessorAgreementAuditEntriesTable.sql
Update: ServiceCollectionExtensions.cs for each provider

KEY RULES:
- Constructor: IDbConnection, string tableName (configurable), TimeProvider?
- Connection state management: check ConnectionState.Open before query
- AddParameter helper: IDbCommand + name + value
- MapToEntity helper: IDataReader → entity
- DPAMandatoryTerms: 8 individual boolean columns
- ProcessingPurposes: JSON string column
- SQL differences per provider (see CLAUDE.md Provider-specific SQL differences table)
- SQLite: TEXT for DateTime (ISO 8601 "O"), INTEGER for booleans (0/1)
- SQL Server: MERGE for upsert, DATETIME2(7), BIT for booleans
- PostgreSQL: ON CONFLICT DO UPDATE, TIMESTAMPTZ, BOOLEAN, JSONB, lowercase columns
- MySQL: INSERT...ON DUPLICATE KEY UPDATE, DATETIME, TINYINT(1) for booleans, backtick identifiers

REFERENCE FILES:
- src/Encina.ADO.SqlServer/DPIA/DPIAStoreADO.cs (SqlServer ADO pattern)
- src/Encina.ADO.Sqlite/DPIA/DPIAStoreADO.cs (SQLite ADO pattern — DateTime handling!)
- src/Encina.ADO.PostgreSQL/DPIA/DPIAStoreADO.cs (PostgreSQL ADO pattern)
- src/Encina.ADO.MySQL/DPIA/DPIAStoreADO.cs (MySQL ADO pattern)
- src/Encina.ADO.SqlServer/Scripts/023_CreateDPIAAssessmentsTable.sql (schema pattern)
```

</details>

---

### Phase 7: Dapper Providers (4 databases)

> **Goal**: Implement `IProcessorRegistry`, `IDPAStore`, and `IProcessorAuditStore` for Dapper — separate implementations for SQLite, SQL Server, PostgreSQL, and MySQL (4 of 13 providers).

<details>
<summary><strong>Tasks</strong></summary>

1. **SQL Server** (`src/Encina.Dapper.SqlServer/ProcessorAgreements/`):
   - `ProcessorRegistryDapper.cs` — Dapper with anonymous type parameters, dynamic row mapping
   - `DPAStoreDapper.cs` — DPA CRUD + status/expiration queries
   - `ProcessorAuditStoreDapper.cs` — audit trail

2. **SQLite** (`src/Encina.Dapper.Sqlite/ProcessorAgreements/`):
   - Same 3 store classes
   - **CRITICAL**: Never use `datetime('now')` — use parameterized `@NowUtc`
   - DateTime stored as ISO 8601 TEXT

3. **PostgreSQL** (`src/Encina.Dapper.PostgreSQL/ProcessorAgreements/`):
   - Same 3 store classes
   - Lowercase column names, JSONB for JSON fields

4. **MySQL** (`src/Encina.Dapper.MySQL/ProcessorAgreements/`):
   - Same 3 store classes
   - Backtick identifiers

5. **DI registration** — Update each provider's `ServiceCollectionExtensions.cs`:
   ```csharp
   if (config.UseProcessorAgreements)
   {
       services.TryAddScoped<IProcessorRegistry, ProcessorAgreements.ProcessorRegistryDapper>();
       services.TryAddScoped<IDPAStore, ProcessorAgreements.DPAStoreDapper>();
       services.TryAddScoped<IProcessorAuditStore, ProcessorAgreements.ProcessorAuditStoreDapper>();
   }
   ```

6. **PublicAPI.Unshipped.txt** — Update for each provider package

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of Encina.Compliance.ProcessorAgreements (Issue #410).

CONTEXT:
- Phases 1-3 (core), Phase 4 (MessagingConfiguration), Phase 5 (EF Core), Phase 6 (ADO.NET) are implemented
- Now implementing Dapper provider for all 4 databases: SqlServer, SQLite, PostgreSQL, MySQL
- Follow EXACTLY the pattern in src/Encina.Dapper.{Provider}/DPIA/ (DPIAStoreDapper, DPIAAuditStoreDapper)
- Dapper uses dynamic row mapping and anonymous type parameters

TASK:
For EACH of 4 Dapper providers, create in src/Encina.Dapper.{Provider}/ProcessorAgreements/:
- ProcessorRegistryDapper.cs (7 methods)
- DPAStoreDapper.cs (7 methods)
- ProcessorAuditStoreDapper.cs (2 methods)
Update: ServiceCollectionExtensions.cs for each provider

KEY RULES:
- Constructor: IDbConnection, string tableName, TimeProvider?
- Use Dapper's ExecuteAsync/QueryAsync with anonymous type parameters
- Dynamic row mapping: (string)row.Id, (int)row.Depth, etc.
- Null handling: row.Prop is null or DBNull ? null : (type)row.Prop
- Same SQL dialects as Phase 6 (ADO.NET) but with Dapper syntax
- Reuse same SQL scripts from Phase 6 (Dapper shares schema with ADO.NET)
- DPAMandatoryTerms: 8 individual columns mapped to/from dynamic

REFERENCE FILES:
- src/Encina.Dapper.SqlServer/DPIA/DPIAStoreDapper.cs (Dapper SqlServer pattern)
- src/Encina.Dapper.Sqlite/DPIA/DPIAStoreDapper.cs (Dapper SQLite pattern)
- src/Encina.Dapper.PostgreSQL/DPIA/DPIAStoreDapper.cs (Dapper PostgreSQL pattern)
- src/Encina.Dapper.MySQL/DPIA/DPIAStoreDapper.cs (Dapper MySQL pattern)
```

</details>

---

### Phase 8: MongoDB Provider

> **Goal**: Implement `IProcessorRegistry`, `IDPAStore`, and `IProcessorAuditStore` for MongoDB (1 of 13 providers).

<details>
<summary><strong>Tasks</strong></summary>

1. **Store implementations** (`src/Encina.MongoDB/ProcessorAgreements/`):

   **`ProcessorRegistryMongoDB : IProcessorRegistry`**:
   - Collection: `config.Collections.Processors` (new collection name in options)
   - BSON document mapping via `ProcessorDocument`
   - `GetSubProcessorsAsync`: `Builders<>.Filter.Eq(d => d.ParentProcessorId, processorId)`
   - `GetFullSubProcessorChainAsync`: iterative queries or aggregation pipeline

   **`DPAStoreMongoDB : IDPAStore`**:
   - Collection: `config.Collections.DataProcessingAgreements` (new collection name)
   - `GetActiveByProcessorIdAsync`: combined filter `ProcessorId == x && StatusValue == Active`
   - `GetExpiringAsync`: filter `ExpiresAtUtc != null && ExpiresAtUtc <= threshold && StatusValue == Active`

   **`ProcessorAuditStoreMongoDB : IProcessorAuditStore`**:
   - Collection: `config.Collections.ProcessorAgreementAuditEntries`

2. **BSON documents** (`src/Encina.MongoDB/ProcessorAgreements/`):
   - `ProcessorDocument` — `[BsonId]`, `[BsonElement]`, `FromProcessor()` / `ToProcessor()` static methods
   - `DataProcessingAgreementDocument` — 8 boolean fields for mandatory terms, ProcessingPurposes as `List<string>`
   - `ProcessorAgreementAuditEntryDocument` — 1:1 mapping

3. **MongoDB options update** (`EncinaMongoDbOptions.cs`):
   - Add collection names: `Processors`, `DataProcessingAgreements`, `ProcessorAgreementAuditEntries`

4. **DI registration** (`ServiceCollectionExtensions.cs`):
   ```csharp
   if (options.UseProcessorAgreements)
   {
       services.AddScoped<IProcessorRegistry, ProcessorAgreements.ProcessorRegistryMongoDB>();
       services.AddScoped<IDPAStore, ProcessorAgreements.DPAStoreMongoDB>();
       services.AddScoped<IProcessorAuditStore, ProcessorAgreements.ProcessorAuditStoreMongoDB>();
   }
   ```

5. **PublicAPI.Unshipped.txt** — Update

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 of Encina.Compliance.ProcessorAgreements (Issue #410).

CONTEXT:
- Phases 1-3 (core), Phase 4 (MessagingConfiguration), Phases 5-7 (EF Core + ADO.NET + Dapper) are implemented
- Now implementing MongoDB provider for all 3 stores
- Follow EXACTLY the pattern in src/Encina.MongoDB/DPIA/ (DPIAStoreMongoDB, documents)

TASK:
Create in src/Encina.MongoDB/ProcessorAgreements/:
- ProcessorRegistryMongoDB.cs (7 methods)
- DPAStoreMongoDB.cs (7 methods)
- ProcessorAuditStoreMongoDB.cs (2 methods)
- ProcessorDocument.cs (BSON document with FromProcessor/ToProcessor)
- DataProcessingAgreementDocument.cs (BSON document)
- ProcessorAgreementAuditEntryDocument.cs (BSON document)
Update: EncinaMongoDbOptions.cs (new collection names), ServiceCollectionExtensions.cs

KEY RULES:
- Constructor: IMongoClient, IOptions<EncinaMongoDbOptions>, ILogger<T>
- Collection access: mongoClient.GetDatabase(config.DatabaseName).GetCollection<TDocument>(config.Collections.X)
- Upsert: ReplaceOneAsync with ReplaceOptions { IsUpsert = true }
- Filter: Builders<TDocument>.Filter.Eq(d => d.Field, value)
- GUID stored as string "D" format
- DateTime with [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
- DPAMandatoryTerms: 8 individual BSON boolean fields
- ProcessingPurposes: native List<string> in BSON (no JSON serialization needed)
- ILogger injected and used for debug logging

REFERENCE FILES:
- src/Encina.MongoDB/DPIA/DPIAStoreMongoDB.cs (MongoDB store pattern)
- src/Encina.MongoDB/DPIA/DPIAAssessmentDocument.cs (BSON document pattern)
- src/Encina.MongoDB/ServiceCollectionExtensions.cs (registration pattern)
- src/Encina.MongoDB/EncinaMongoDbOptions.cs (collection names pattern)
```

</details>

---

### Phase 9: Pipeline Behavior — `ProcessorValidationPipelineBehavior`

> **Goal**: Implement the pipeline behavior that validates processor DPAs before request execution. Uses `IDPAValidator` which queries both `IProcessorRegistry` and `IDPAStore` internally (DC 2).

<details>
<summary><strong>Tasks</strong></summary>

1. **`ProcessorValidationPipelineBehavior<TRequest, TResponse>`** (`ProcessorValidationPipelineBehavior.cs`):
   - Implements `IPipelineBehavior<TRequest, TResponse>` where `TRequest : IRequest<TResponse>`
   - Static per-generic-type attribute caching:
     - `private static readonly ConcurrentDictionary<Type, RequiresProcessorAttribute?> AttributeCache = new()`
   - `Handle` method flow:
     1. If `EnforcementMode == Disabled` → `nextStep()`
     2. Resolve `[RequiresProcessor]` from `typeof(TRequest)` (cached)
     3. If no attribute → `nextStep()` (not a processor-dependent request)
     4. Start activity + structured logging
     5. Call `IDPAValidator.HasValidDPAAsync(attribute.ProcessorId)` — fast path, checks both Processor existence and active DPA
     6. If valid → record passed + `nextStep()`
     7. If invalid + Block mode → call `IDPAValidator.ValidateAsync` for detailed result (includes DPAId, missing terms, expiration) → return `Left(error)`
     8. If invalid + Warn mode → log warning + `nextStep()`
   - Observability: traces via `ProcessorAgreementDiagnostics`, counters, structured logging
   - Dependencies: `IDPAValidator`, `IOptions<ProcessorAgreementOptions>`, `TimeProvider`, `ILogger`

2. **Validation detail on Block**: When blocking, `ValidateAsync` returns `DPAValidationResult` with `DPAId`, `MissingTerms`, `DaysUntilExpiration` — all included in the `EncinaError` metadata for actionable error messages.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
You are implementing Phase 9 of Encina.Compliance.ProcessorAgreements (Issue #410).

CONTEXT:
- Phases 1-8 are implemented (core models, interfaces, in-memory stores, MessagingConfiguration, all 13 provider implementations)
- This pipeline behavior follows the EXACT same pattern as DPIARequiredPipelineBehavior
- It intercepts requests marked with [RequiresProcessor] and validates the referenced processor's DPA
- IDPAValidator internally queries both IProcessorRegistry (Processor) and IDPAStore (DataProcessingAgreement) — DC 2

TASK:
Create ProcessorValidationPipelineBehavior<TRequest, TResponse>.

KEY RULES:
- Use static ConcurrentDictionary<Type, RequiresProcessorAttribute?> for attribute caching
- Two-level validation: fast HasValidDPAAsync for pass-through, detailed ValidateAsync only when blocking
- Three enforcement modes: Block returns Left(error with details), Warn logs + continues, Disabled skips
- On Block: include DPAValidationResult details in the error (DPAId, missing terms, expiration info)
- Pipeline behavior depends on IDPAValidator only (not IProcessorRegistry or IDPAStore directly)
- All constructor parameters validated with ArgumentNullException.ThrowIfNull
- Stopwatch.GetTimestamp() for duration measurement (same as DPIA pattern)
- Activity tracing with tags: processor_id, enforcement_mode, outcome, failure_reason
- Metric recording: total checks, passed, failed counters + duration histogram

REFERENCE FILES:
- src/Encina.Compliance.DPIA/DPIARequiredPipelineBehavior.cs (EXACT pattern to follow)
- src/Encina.Compliance.DPIA/Diagnostics/DPIADiagnostics.cs (diagnostics helpers)
```

</details>

---

### Phase 10: Configuration, DI & Scheduled Expiration Monitoring

> **Goal**: Wire everything together with options (including `MaxSubProcessorDepth` for DC 5), service registration (including `IDPAStore` for DC 2), and scheduled expiration checks via Encina.Scheduling (DC 6).

<details>
<summary><strong>Tasks</strong></summary>

1. **Options** (`ProcessorAgreementOptions.cs`):
   - `ProcessorAgreementEnforcementMode EnforcementMode { get; set; }` — default: `Warn`
   - `bool BlockWithoutValidDPA { get; set; }` — alias for `EnforcementMode.Block`
   - `int AlertBeforeExpirationDays { get; set; }` — default: `30`
   - `bool RequireSubProcessorAuthorization { get; set; }` — default: `true`
   - `int MaxSubProcessorDepth { get; set; }` — default: `3` (DC 5 — bounds the sub-processor hierarchy)
   - `bool NotifyOnSubProcessorChange { get; set; }` — default: `true`
   - `bool PublishNotifications { get; set; }` — default: `true`
   - `bool TrackAuditTrail { get; set; }` — default: `true`
   - `bool EnableExpirationMonitoring { get; set; }` — default: `false`
   - `TimeSpan ExpirationCheckInterval { get; set; }` — default: `TimeSpan.FromHours(1)`
   - `bool AddHealthCheck { get; set; }` — default: `false`

2. **Options validator** (`ProcessorAgreementOptionsValidator.cs`):
   - `IValidateOptions<ProcessorAgreementOptions>`
   - Validates: `AlertBeforeExpirationDays > 0`, `ExpirationCheckInterval > TimeSpan.Zero`, `MaxSubProcessorDepth > 0 && MaxSubProcessorDepth <= 10`

3. **Service collection extensions** (`ServiceCollectionExtensions.cs`):
   - `AddEncinaProcessorAgreements(this IServiceCollection services, Action<ProcessorAgreementOptions>? configure = null)`
   - Registers:
     - `ProcessorAgreementOptions` via `services.Configure()`
     - `ProcessorAgreementOptionsValidator` via `TryAddSingleton<IValidateOptions<>>`
     - `TimeProvider.System` via `TryAddSingleton`
     - `IProcessorRegistry` → `InMemoryProcessorRegistry` via `TryAddSingleton`
     - `IDPAStore` → `InMemoryDPAStore` via `TryAddSingleton` (NEW — DC 2)
     - `IProcessorAuditStore` → `InMemoryProcessorAuditStore` via `TryAddSingleton`
     - `IDPAValidator` → `DefaultDPAValidator` via `TryAddScoped`
     - `ProcessorValidationPipelineBehavior<,>` via `TryAddTransient(typeof(IPipelineBehavior<,>))`
   - Conditional: health check if `AddHealthCheck == true`
   - Conditional: expiration monitoring via Encina.Scheduling if `EnableExpirationMonitoring == true` (DC 6)

4. **Expiration monitoring via Encina.Scheduling** (DC 6 — replaces IHostedService):

   **`CheckDPAExpirationCommand : ICommand<Unit>`** (`Commands/` folder):
   - Scheduled command — no properties needed (uses options for configuration)

   **`CheckDPAExpirationHandler : ICommandHandler<CheckDPAExpirationCommand, Unit>`** (`Commands/` folder):
   - Dependencies: `IDPAStore`, `IOptions<ProcessorAgreementOptions>`, `TimeProvider`, `ILogger`
   - On execution:
     1. Query `IDPAStore.GetExpiringAsync(nowUtc + AlertBeforeExpirationDays)` → DPAs approaching expiration
     2. Query `IDPAStore.GetByStatusAsync(DPAStatus.Active)` → check for newly expired (ExpiresAtUtc ≤ nowUtc)
     3. For expiring: publish `DPAExpiringNotification` with `DaysUntilExpiration`
     4. For newly expired: update DPA status to `Expired` via `IDPAStore.UpdateAsync`, publish `DPAExpiredNotification`
   - All operations wrapped in try/catch with structured logging

   **DI registration** (in `ServiceCollectionExtensions`):
   - When `EnableExpirationMonitoring == true`:
     - Register `CheckDPAExpirationHandler` as command handler
     - Schedule recurring `CheckDPAExpirationCommand` with `ExpirationCheckInterval` via Encina.Scheduling API
   - This replaces the `DPAExpirationMonitorService : IHostedService` pattern — Encina.Scheduling provides persistence, distributed locks, retry, and observability for free

5. **Health check** (`Health/ProcessorAgreementHealthCheck.cs`):
   - `public sealed class ProcessorAgreementHealthCheck : IHealthCheck`
   - `const string DefaultName = "encina-processor-agreements"`
   - Tags: `["encina", "gdpr", "processor", "dpa", "compliance", "ready"]`
   - Checks: registry resolvable + DPA store resolvable, expired DPAs (degraded), incomplete mandatory terms (degraded)
   - Uses scoped resolution pattern

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 10</strong></summary>

```
You are implementing Phase 10 of Encina.Compliance.ProcessorAgreements (Issue #410).

CONTEXT:
- Phases 1-9 are implemented (core models, interfaces, stores, MessagingConfiguration, all 13 providers, pipeline behavior)
- DI registration follows the TryAdd pattern — satellite providers pre-register concrete stores
- Health checks use IServiceProvider.CreateScope() for scoped service resolution

DESIGN DECISIONS:
- DC 2: Register IDPAStore → InMemoryDPAStore (NEW store for DataProcessingAgreement entities)
- DC 5: MaxSubProcessorDepth option (default: 3), validated in options validator
- DC 6: Use Encina.Scheduling instead of IHostedService for expiration monitoring
  - CheckDPAExpirationCommand : ICommand<Unit> — scheduled command
  - CheckDPAExpirationHandler : ICommandHandler — queries IDPAStore, publishes notifications
  - Registered via Encina.Scheduling API when EnableExpirationMonitoring == true

TASK:
Create options, DI registration, scheduled command + handler for expiration monitoring, and health check.

KEY RULES:
- Options pattern: sealed class with defaults, IValidateOptions<T> for validation
- MaxSubProcessorDepth validated: > 0 && <= 10
- ServiceCollectionExtensions uses TryAdd* for all registrations (satellite overrides)
- Instantiate a local optionsInstance to read flags before DI is fully built
- IDPAStore registered with TryAddSingleton (alongside IProcessorRegistry)
- NO DPAExpirationMonitorService or IHostedService — use CheckDPAExpirationCommand + handler instead
- CheckDPAExpirationHandler queries IDPAStore.GetExpiringAsync and IDPAStore.GetByStatusAsync
- When EnableExpirationMonitoring == true, schedule recurring CheckDPAExpirationCommand with ExpirationCheckInterval
- Health check: Unhealthy if core services missing, Degraded if expired/incomplete DPAs, Healthy otherwise
- Pipeline behavior registered with TryAddTransient(typeof(IPipelineBehavior<,>))
- BlockWithoutValidDPA is a convenience alias (same pattern as DPIA.BlockWithoutDPIA)

REFERENCE FILES:
- src/Encina.Compliance.DPIA/DPIAOptions.cs (options pattern with BlockWithoutDPIA alias)
- src/Encina.Compliance.DPIA/DPIAOptionsValidator.cs (options validator)
- src/Encina.Compliance.DPIA/ServiceCollectionExtensions.cs (DI registration pattern)
- src/Encina.Compliance.DPIA/Health/DPIAHealthCheck.cs (health check pattern)
- src/Encina.Messaging/ (Encina.Scheduling API — ICommand, ICommandHandler, scheduling registration)
```

</details>

---

### Phase 11: Cross-Cutting Integration

> **⚠️ CRITICAL**: This phase has the authority to REVERT decisions and REFACTOR code from ANY previous phase (1-10). The planning and decision-making process is imperfect. If integration analysis reveals that any previous implementation is not coherent with the rest of Encina, this phase MUST fix it — even if that means refactoring hundreds or thousands of lines of code. Always choose integration and coherence with the rest of Encina over preserving previous work.

> **Goal**: Evaluate ALL code from Phases 1-10 against Encina's 12 transversal functions. Fix any integration gaps, even if that requires substantial refactoring of previous phases. Coherence with Encina's architecture always takes precedence.

<details>
<summary><strong>Tasks</strong></summary>

1. **Multi-Tenancy** (✅ Included):
   - `Processor.TenantId` and `DataProcessingAgreement.TenantId` fields already in models (Phase 1)
   - Pipeline behavior: propagate `IRequestContext.TenantId` to activity tags
   - `InMemoryProcessorRegistry`: filter by TenantId when `IRequestContext` is available
   - `InMemoryDPAStore`: filter by TenantId when available
   - Both stores respect tenant isolation

2. **Audit Trail** (✅ Included):
   - `IProcessorAuditStore` already defined (Phase 2) — entries include both `ProcessorId` and `DPAId` (nullable)
   - `DefaultDPAValidator`: record audit entry on every validation
   - Pipeline behavior: record audit entry on block/warn
   - Processor lifecycle: audit on register, update, remove
   - DPA lifecycle: audit on add, update (status change), terminate, expire
   - Sub-processor operations: audit on add/remove with depth info (DC 5)
   - `CheckDPAExpirationHandler`: audit on expiration detection and status update (DC 6)

3. **Validation** (✅ Included):
   - `ProcessorAgreementOptionsValidator` validates configuration including `MaxSubProcessorDepth` (Phase 10)
   - `DefaultDPAValidator.ValidateAsync` validates DPA content against Art. 28(3) requirements
   - `Processor` validation in registry: required fields (Id, Name, Country), valid ParentProcessorId (must exist), Depth consistency (DC 5)
   - `DataProcessingAgreement` validation in DPA store: required fields (Id, ProcessorId, SignedAtUtc), ProcessorId must exist in registry

4. **Transactions** (✅ Included):
   - Store operations (register, update, remove) are atomic
   - In-memory stores use ConcurrentDictionary for thread-safe operations
   - Database-backed stores participate in ambient transactions via `IUnitOfWork`

5. **Caching** (✅ Included):
   - Cache `IProcessorRegistry` lookups (hot path in pipeline behavior — called on every request with `[RequiresProcessorAgreement]`)
   - Use `ICacheProvider` abstraction (same pattern as DPIA caching)
   - Cache keys: `processor:{processorId}`, `processor:list:{tenantId}`, `dpa:{dpaId}`, `dpa:active:{processorId}`
   - Invalidation: on register/update/remove processor, on add/update/terminate DPA
   - TTL: configurable via `ProcessorAgreementOptions.CacheTtl` (default: 5 minutes)
   - All 13 database providers + in-memory store benefit from caching layer
   - Decorator pattern: `CachingProcessorRegistry` wraps `IProcessorRegistry`, `CachingDPAStore` wraps `IDPAStore`

6. **Distributed Locks** (✅ Included):
   - Protect concurrent DPA status transitions (e.g., two simultaneous expire operations on same DPA)
   - Protect concurrent processor registration (especially sub-processor depth validation — race condition on parent lookup + depth check)
   - Use `IDistributedLockProvider` abstraction (same pattern as other Encina features)
   - Lock keys: `processor-agreement:dpa:{dpaId}`, `processor-agreement:processor:{processorId}`
   - Lock timeout: configurable via `ProcessorAgreementOptions.LockTimeout` (default: 30 seconds)
   - In-memory stores: `SemaphoreSlim` for single-process scenarios
   - Database-backed stores: `IDistributedLockProvider` for multi-instance scenarios
   - `CheckDPAExpirationHandler`: acquire lock before batch-updating expired DPAs (prevents duplicate processing in scaled-out deployments)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 11</strong></summary>

```
You are implementing Phase 11 of Encina.Compliance.ProcessorAgreements (Issue #410).

CRITICAL: This phase can REFACTOR any code from Phases 1-10. If any previous implementation doesn't integrate properly with Encina's cross-cutting concerns, FIX IT regardless of the refactoring cost.

CONTEXT:
- Phases 1-10 are implemented (full core package with pipeline behavior, DI, health check, scheduling, all 13 providers)
- Review ALL stores (in-memory + EF Core + ADO.NET + Dapper + MongoDB) for cross-cutting compliance
- Cross-cutting integrations must be woven into existing code, not added as separate files
- Multi-tenancy, audit trail, validation, transactions, caching, and distributed locks are the active integrations
- DC 2: Processor and DataProcessingAgreement are separate entities with separate stores
- DC 5: Depth tracking requires validation on registration and audit on sub-processor operations
- DC 6: CheckDPAExpirationHandler (not IHostedService) needs audit trail integration

TASK:
Ensure all cross-cutting integrations are properly wired across ALL code from Phases 1-10:
1. Multi-Tenancy: TenantId propagation in pipeline behavior, BOTH store filtering (in-memory, EF Core, ADO.NET, Dapper, MongoDB)
2. Audit Trail: Record entries in validator, pipeline behavior, CheckDPAExpirationHandler, sub-processor operations
3. Validation: Processor validation (required fields + depth), DPA validation (required fields + ProcessorId exists)
4. Transactions: Atomic store operations (in-memory: ConcurrentDictionary; DB: IUnitOfWork)
5. Caching: ICacheProvider decorator on IProcessorRegistry and IDPAStore (hot path optimization for pipeline behavior)
6. Distributed Locks: IDistributedLockProvider for concurrent DPA status transitions, processor registration (depth race condition), CheckDPAExpirationHandler batch updates

KEY RULES:
- TenantId comes from IRequestContext in pipeline behavior, from entity TenantId fields in stores
- Audit entries include BOTH ProcessorId and DPAId where applicable (DPAId nullable for processor-only operations)
- Record audit on sub-processor add/remove with depth information
- CheckDPAExpirationHandler records audit when it detects expiring/expired DPAs and updates statuses
- Processor registration validates: ParentProcessorId exists, Depth == parent.Depth + 1, Depth ≤ MaxSubProcessorDepth
- DPA addition validates: ProcessorId exists in IProcessorRegistry
- Caching: decorator pattern (CachingProcessorRegistry, CachingDPAStore), invalidate on mutations, TenantId-aware cache keys
- Distributed Locks: lock before DPA status transitions and processor registration depth validation, CheckDPAExpirationHandler acquires lock for batch updates
- Do NOT add unnecessary complexity — keep integrations minimal and focused
- If any previous implementation doesn't integrate properly with Encina's cross-cutting concerns, FIX IT regardless of the refactoring cost

REFERENCE FILES:
- src/Encina.Compliance.DPIA/DPIARequiredPipelineBehavior.cs (tenant context propagation)
- src/Encina.Compliance.DPIA/InMemoryDPIAAuditStore.cs (audit recording pattern)
- src/Encina.Caching/ICacheProvider.cs (caching abstraction)
- src/Encina/DistributedLock/IDistributedLockProvider.cs (distributed lock abstraction)
```

</details>

---

### Phase 12: Observability — Diagnostics, Metrics & Logging

> **Goal**: Add comprehensive observability to ALL code from Phases 1-11 — ActivitySource traces, Meter counters/histograms, and [LoggerMessage] structured logging across the core package and all 13 database provider implementations.

<details>
<summary><strong>Tasks</strong></summary>

1. **`ProcessorAgreementDiagnostics.cs`** (`Diagnostics/` folder):
   - `internal static class ProcessorAgreementDiagnostics`
   - `ActivitySource`: `"Encina.Compliance.ProcessorAgreements"`, version `"1.0"`
   - `Meter`: `"Encina.Compliance.ProcessorAgreements"`, version `"1.0"`
   - **Counters** (Counter<long>):
     - `processor.pipeline.checks.total` — tagged by `processor_id`, `outcome`
     - `processor.pipeline.checks.passed` — tagged by `processor_id`
     - `processor.pipeline.checks.failed` — tagged by `processor_id`, `failure_reason`
     - `processor.pipeline.checks.skipped` — tagged by `request_type`
     - `processor.validation.total` — tagged by `processor_id`, `outcome`
     - `processor.registrations.total` — tagged by `status`
     - `processor.dpa.total` — tagged by `processor_id`, `status` (NEW — DC 2, tracks DPA lifecycle)
     - `processor.expirations.total` — tagged by `processor_id`
     - `processor.sub_processor.depth` — tagged by `processor_id`, `depth` (NEW — DC 5, tracks depth distribution)
   - **Histograms** (Histogram<double>):
     - `processor.pipeline.check.duration` — tagged by `request_type`
     - `processor.validation.duration` — tagged by `processor_id`
   - **Tag constants**: `TagProcessorId`, `TagDPAId`, `TagRequestType`, `TagOutcome`, `TagFailureReason`, `TagEnforcementMode`, `TagStatus`, `TagDepth`
   - **Activity helpers**:
     - `StartPipelineCheck(string requestTypeName)` → `Activity?`
     - `StartExpirationCheck()` → `Activity?` (NEW — DC 6, for scheduled command)
     - `RecordPassed(Activity?)` / `RecordFailed(Activity?, string reason)` / `RecordWarned(Activity?, string reason)` / `RecordSkipped(Activity?)`

2. **`ProcessorAgreementLogMessages.cs`** (`Diagnostics/` folder):
   - `internal static partial class ProcessorAgreementLogMessages` using `[LoggerMessage]` source generator
   - **Event ID range: 8900-8999** (next available after DPIA's 8800-8899):
     - 8900-8909: Pipeline behavior (started, passed, failed, blocked, warned, disabled, no attribute, error)
     - 8910-8919: Validation (started, completed, invalid, missing terms, expired, SCC required)
     - 8920-8929: Processor registry operations (registered, updated, removed, not found, duplicate, depth exceeded)
     - 8930-8939: DPA store operations (added, updated, status changed, not found) (UPDATED — DC 2)
     - 8940-8949: Expiration check command (started, expiring found, expired found, status updated, notification published) (UPDATED — DC 6)
     - 8950-8959: Sub-processor operations (added, removed, unauthorized, depth exceeded, chain queried) (UPDATED — DC 5)
     - 8960-8969: Health check (completed, degraded, unhealthy)
     - 8970-8979: Audit trail (recorded, query completed)
     - 8980-8989: Store errors
     - 8990-8999: Reserved

3. **Integrate observability into existing code**:
   - Pipeline behavior: traces + metrics + logging (already partially done in Phase 9)
   - DefaultDPAValidator: traces + logging for validation operations (queries both stores)
   - CheckDPAExpirationHandler: activity tracing + logging for scheduled scan cycles (DC 6)
   - InMemoryProcessorRegistry: logging for CRUD operations + depth validation (DC 5)
   - InMemoryDPAStore: logging for DPA CRUD operations (DC 2)
   - ALL provider stores (EF Core, ADO.NET x4, Dapper x4, MongoDB): ensure every store operation emits Activity spans and structured logs

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 12</strong></summary>

```
You are implementing Phase 12 of Encina.Compliance.ProcessorAgreements (Issue #410).

CONTEXT:
- Phases 1-11 are implemented (core + all 13 providers + pipeline behavior + DI + cross-cutting integrations)
- Observability follows OpenTelemetry patterns: ActivitySource for traces, Meter for metrics, ILogger for logs
- Event IDs in 8900-8999 range (new range, avoids collision with DPIA 8800-8899)
- Add observability to ALL stores (in-memory, EF Core, ADO.NET x4, Dapper x4, MongoDB)
- Ensure every store operation emits Activity spans and structured logs

DESIGN DECISIONS:
- DC 2: Separate Processor and DataProcessingAgreement → separate log categories for registry vs DPA store
- DC 5: Depth tracking → log depth exceeded events, sub_processor.depth counter
- DC 6: CheckDPAExpirationHandler → StartExpirationCheck activity, handler-level logging (not IHostedService)

TASK:
Create ProcessorAgreementDiagnostics, ProcessorAgreementLogMessages, and integrate into ALL existing code across the core package and all 13 provider implementations.

KEY RULES:
- ActivitySource name: "Encina.Compliance.ProcessorAgreements" (matches package name)
- Meter: new Meter("Encina.Compliance.ProcessorAgreements", "1.0")
- TagDPAId added for DPA-specific counters (DC 2)
- TagDepth added for sub-processor depth tracking (DC 5)
- StartExpirationCheck activity for the scheduled command handler (DC 6)
- Event IDs 8920-8929 for processor registry, 8930-8939 for DPA store (separate ranges, DC 2)
- Event IDs 8950-8959 include "depth exceeded" event (DC 5)
- Event IDs 8940-8949 for CheckDPAExpirationHandler (DC 6, not background service)
- All counters use tag-based dimensions for flexible dashboards
- ProcessorAgreementLogMessages uses [LoggerMessage] source generator (partial class, partial methods)
- Log messages: structured with named parameters, NO PII in log messages
- Activity helpers check Source.HasListeners() before creating activities
- Duration recording uses Stopwatch.GetTimestamp/GetElapsedTime pattern

REFERENCE FILES:
- src/Encina.Compliance.DPIA/Diagnostics/DPIADiagnostics.cs (ActivitySource + Meter + helpers)
- src/Encina.Compliance.DPIA/Diagnostics/DPIALogMessages.cs ([LoggerMessage] source generator)
```

</details>

---

### Phase 13: Testing

> **Goal**: Comprehensive test coverage for ALL code from Phases 1-12 — unit tests for core package, guard tests for all public APIs, contract tests for interfaces, property tests for invariants, and integration tests for all 13 database providers using Docker/Testcontainers.

<details>
<summary><strong>Tasks</strong></summary>

#### 13a. Unit Tests (`tests/Encina.UnitTests/Compliance/ProcessorAgreements/`)

- `ProcessorTests.cs` — domain record validation, ParentProcessorId/Depth defaults (DC 2/5)
- `DataProcessingAgreementTests.cs` — domain record validation, IsActive(nowUtc) logic, DPA lifecycle (DC 2)
- `DPAMandatoryTermsTests.cs` — all 8 terms, MissingTerms list, IsFullyCompliant computed property
- `DPAValidationResultTests.cs` — validity logic, warning generation, DPAId inclusion
- `ProcessorMapperTests.cs` — Processor domain ↔ entity round-trip (includes ParentProcessorId/Depth) (DC 2/5)
- `DataProcessingAgreementMapperTests.cs` — DPA domain ↔ entity round-trip (DC 2)
- `ProcessorAgreementAuditEntryMapperTests.cs` — audit domain ↔ entity round-trip
- `DefaultDPAValidatorTests.cs` — all validation paths (valid, expired, terminated, missing terms, no DPA, sub-processor), queries both stores (DC 2)
- `InMemoryProcessorRegistryTests.cs` — CRUD, sub-processor depth validation, GetSubProcessorsAsync, GetFullSubProcessorChainAsync, MaxSubProcessorDepth enforcement (DC 5)
- `InMemoryDPAStoreTests.cs` — DPA CRUD, GetActiveByProcessorIdAsync, GetExpiringAsync, GetByStatusAsync, history queries (DC 2)
- `InMemoryProcessorAuditStoreTests.cs` — record + query audit trail (includes DPAId)
- `ProcessorValidationPipelineBehaviorTests.cs` — Block/Warn/Disabled modes, attribute caching, two-level validation
- `ProcessorAgreementOptionsValidatorTests.cs` — validation rules including MaxSubProcessorDepth bounds (DC 5)
- `ServiceCollectionExtensionsTests.cs` — DI registration verification (IDPAStore, scheduling registration) (DC 2/6)
- `CheckDPAExpirationHandlerTests.cs` — expiring detection, expired status update, notification publishing (DC 6)

**Target**: ~80-100 unit tests

#### 13b. Guard Tests (`tests/Encina.GuardTests/Compliance/ProcessorAgreements/`)

- All public constructors and methods: null checks for non-nullable parameters
- Cover: IProcessorRegistry implementations, IDPAStore implementations, IDPAValidator, pipeline behavior, options, mappers, CheckDPAExpirationHandler

**Target**: ~40-50 guard tests

#### 13c. Contract Tests (`tests/Encina.ContractTests/Compliance/ProcessorAgreements/`)

- `IProcessorRegistryContractTests.cs` — verify registry contract (CRUD, hierarchy queries, depth validation) (DC 2/5)
- `IDPAStoreContractTests.cs` — verify DPA store contract (CRUD, active/expiring/status queries) (DC 2)
- `IDPAValidatorContractTests.cs` — verify validator contract (valid/invalid/expired, queries both stores) (DC 2)
- `IProcessorAuditStoreContractTests.cs` — verify audit store contract (includes DPAId)

**Target**: ~15-20 contract tests

#### 13d. Property Tests (`tests/Encina.PropertyTests/Compliance/ProcessorAgreements/`)

- `ProcessorPropertyTests.cs` — Depth ≥ 0, ParentProcessorId null ↔ Depth == 0 invariant (DC 5)
- `DataProcessingAgreementPropertyTests.cs` — IsActive(now) logic for all status/expiration combinations (DC 2)
- `DPAMandatoryTermsPropertyTests.cs` — IsFullyCompliant ↔ all 8 terms true, MissingTerms count consistency
- `ProcessorMapperPropertyTests.cs` — Processor domain → entity → domain round-trip preserves all fields (DC 2/5)
- `DataProcessingAgreementMapperPropertyTests.cs` — DPA domain → entity → domain round-trip preserves all fields (DC 2)

**Target**: ~15-20 property tests

#### 13e. Integration Tests (`tests/Encina.IntegrationTests/`)

1. **ADO.NET integration tests** (`tests/Encina.IntegrationTests/ADO/{Provider}/ProcessorAgreements/`):
   - `ProcessorRegistryADOTests.cs` — CRUD, depth validation, sub-processor hierarchy, GetFullSubProcessorChainAsync
   - `DPAStoreADOTests.cs` — CRUD, active/expiring/status queries, DPA history
   - `ProcessorAuditStoreADOTests.cs` — record + query audit trail
   - For each: SqlServer, PostgreSQL, MySQL, SQLite (4 x 3 = 12 test files)
   - Use `[Collection("ADO-{Database}")]` fixtures

2. **Dapper integration tests** (`tests/Encina.IntegrationTests/Dapper/{Provider}/ProcessorAgreements/`):
   - Same 3 test files x 4 databases = 12 test files
   - Use `[Collection("Dapper-{Database}")]` fixtures

3. **EF Core integration tests** (`tests/Encina.IntegrationTests/Compliance/ProcessorAgreements/EFCore/`):
   - Same 3 test files x 4 databases = 12 test files
   - Use `[Collection("EFCore-{Database}")]` fixtures

4. **MongoDB integration tests** (`tests/Encina.IntegrationTests/Compliance/ProcessorAgreements/MongoDB/`):
   - 3 test files for MongoDB
   - Use `[Collection("MongoDB")]` fixture

5. **Test coverage per store**:
   - `RegisterProcessorAsync` — valid, duplicate, sub-processor with depth
   - `GetSubProcessorsAsync` / `GetFullSubProcessorChainAsync` — hierarchy traversal
   - `AddAsync` / `GetByIdAsync` / `UpdateAsync` — basic CRUD
   - `GetActiveByProcessorIdAsync` — active DPA retrieval
   - `GetExpiringAsync` — expiration threshold queries
   - `GetByStatusAsync` — status filtering
   - `RecordAsync` / `GetAuditTrailAsync` — audit trail
   - All tests verify `Either.IsRight` for success paths

**Target**: ~15-20 tests per provider variant x 13 providers = ~200-250 integration tests

#### 13f. Load Tests — `.md` justification

- `ProcessorAgreements.LoadTests.md` — justification: pipeline behavior uses in-memory store with ConcurrentDictionary; load characteristics are identical to DPIA pipeline behavior. When database-backed stores are implemented, load tests will be meaningful.

#### 13g. Benchmark Tests — `.md` justification

- `ProcessorAgreements.BenchmarkTests.md` — justification: pipeline behavior follows identical pattern to DPIA (attribute caching + store lookup). DPIA benchmarks already establish the performance baseline for this pattern. Specific benchmarks can be added when unique performance characteristics emerge.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 13</strong></summary>

```
You are implementing Phase 13 of Encina.Compliance.ProcessorAgreements (Issue #410).

CONTEXT:
- Phases 1-12 are fully implemented (ALL production code including observability)
- Create ALL test types: unit, guard, contract, property, AND integration
- Integration tests: 3 stores x 13 providers = 39 test files using Docker/Testcontainers
- Use [Collection] fixtures per CLAUDE.md rules
- 2 justified skips: Load (in-memory only), Benchmark (same pattern as DPIA)

DESIGN DECISIONS:
- DC 2: Separate Processor and DataProcessingAgreement entities → separate test files for each
- DC 5: Depth tracking → test depth validation, GetFullSubProcessorChainAsync, MaxSubProcessorDepth
- DC 6: CheckDPAExpirationHandler (not IHostedService) → test command handler, not background service

TASK:
Create comprehensive test coverage for ALL code from Phases 1-12.

KEY RULES:
Unit Tests:
- Mock all dependencies
- Test each method independently
- Cover happy path + error paths + edge cases
- Separate test files: ProcessorTests, DataProcessingAgreementTests, InMemoryDPAStoreTests (DC 2)
- InMemoryProcessorRegistryTests must cover depth validation, GetFullSubProcessorChainAsync, MaxSubProcessorDepth (DC 5)
- CheckDPAExpirationHandlerTests must cover expiration detection, status update, notification publishing (DC 6)
- Verify all 8 mandatory terms individually in DPAMandatoryTermsTests
- Pipeline behavior: test all 3 enforcement modes + attribute caching + two-level validation

Guard Tests:
- Use GuardClauses.xUnit library
- All public constructors and methods with non-nullable parameters
- Include CheckDPAExpirationHandler constructor guards (DC 6)

Contract Tests:
- Abstract base test class with InMemory-derived test classes
- Verify IProcessorRegistry contract (CRUD + hierarchy), IDPAStore contract (CRUD + queries), validation contract, audit contract
- IDPAStoreContractTests is NEW (DC 2)

Property Tests:
- FsCheck generators for Processor (with ParentProcessorId/Depth), DataProcessingAgreement, DPAMandatoryTerms
- Invariants: IsFullyCompliant ↔ all terms true, IsActive(now) logic, mapper round-trips for BOTH entities
- Processor depth invariant: ParentProcessorId null ↔ Depth == 0 (DC 5)

Integration Tests:
- ALWAYS use [Collection("Provider-Database")] — NEVER IClassFixture<T>
- Constructor injection of fixture
- InitializeAsync: _fixture.ClearAllDataAsync()
- DisposeAsync: return Task.CompletedTask
- SQLite: NEVER dispose shared connection from _fixture.CreateConnection()
- Test names: descriptive, follow AAA pattern
- Assert Either.IsRight for success paths
- Assert Either.IsLeft with error code for failure paths
- [Trait("Category", "Integration")] + [Trait("Database", "{Database}")]
- ADO: 4 databases x 3 stores = 12 test files
- Dapper: 4 databases x 3 stores = 12 test files
- EF Core: 4 databases x 3 stores = 12 test files
- MongoDB: 3 test files
- Total: 39 integration test files

REFERENCE FILES:
- tests/Encina.UnitTests/Compliance/DPIA/ (unit test patterns for compliance)
- tests/Encina.GuardTests/Compliance/DPIA/ (guard test patterns)
- tests/Encina.ContractTests/Compliance/DPIA/ (contract test patterns)
- tests/Encina.PropertyTests/Compliance/DPIA/ (property test patterns)
- tests/Encina.IntegrationTests/ (existing DPIA integration tests for all providers)
- CLAUDE.md Collection Fixtures section (mandatory patterns)
```

</details>

---

### Phase 14: Documentation

> **Goal**: Comprehensive documentation covering ALL code from Phases 1-13 — XML doc comments on all public APIs, CHANGELOG entry, ROADMAP update, package README, feature documentation, INVENTORY update, and PublicAPI.txt verification. This is the final phase because it documents everything including tests.

<details>
<summary><strong>Tasks</strong></summary>

1. **XML doc comments** on all public APIs (`<summary>`, `<remarks>`, `<param>`, `<returns>`, `<example>`)
2. **CHANGELOG.md** — add entry under Unreleased section:

   ```
   ### Added
   - `Encina.Compliance.ProcessorAgreements` — GDPR Art. 28 Data Processing Agreement management with processor registry, DPA validation, sub-processor tracking, SCC compliance, and pipeline enforcement (#410)
   ```

3. **ROADMAP.md** — update v0.13.0 Security & Compliance milestone
4. **Package README.md** — `src/Encina.Compliance.ProcessorAgreements/README.md` with usage guide covering all providers (EF Core, ADO.NET, Dapper, MongoDB)
5. **docs/features/processor-agreements.md** — feature documentation with configuration examples for all 13 providers
6. **docs/INVENTORY.md** — add new package entry
7. **PublicAPI.Unshipped.txt** — verify ALL public symbols are tracked (core + all provider packages)
8. **Build verification**: `dotnet build --configuration Release` → 0 errors, 0 warnings
9. **Test verification**: `dotnet test` → all pass

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 14</strong></summary>

```
You are implementing Phase 14 of Encina.Compliance.ProcessorAgreements (Issue #410).

CONTEXT:
- Phases 1-13 are fully implemented and tested
- Document EVERYTHING: core package, all 13 provider implementations, pipeline behavior, configuration, observability, and test patterns
- README must include examples for all provider types (EF Core, ADO.NET, Dapper, MongoDB)

TASK:
Create comprehensive documentation covering ALL code from Phases 1-13.

KEY RULES:
- XML doc comments on ALL public APIs with <summary>, <remarks>, <param>, <returns>, <example>
- CHANGELOG entry under ### Added with issue reference (#410)
- ROADMAP update for v0.13.0 Security & Compliance milestone
- Package README: quick start, configuration, pipeline behavior, sub-processor tracking (depth hierarchy), scheduling examples, provider examples for EF Core, ADO.NET, Dapper, MongoDB
- Feature doc: Art. 28 reference, all 8 mandatory terms explained, separate Processor/DPA lifecycle, examples for all 13 providers
- INVENTORY.md: add new package entry
- PublicAPI.Unshipped.txt: verify ALL public symbols tracked across core + all provider packages
- Build verification: dotnet build --configuration Release → 0 errors, 0 warnings
- Test verification: dotnet test → all pass

REFERENCE FILES:
- src/Encina.Compliance.DPIA/README.md (package README format)
- docs/features/ (existing feature documentation)
- CHANGELOG.md (existing format)
- ROADMAP.md (existing milestones)
- docs/INVENTORY.md (existing package entries)
```

</details>

---

## Research

### Relevant Standards/Specifications

| Standard | Article/Section | Relevance |
|----------|----------------|-----------|
| GDPR Article 28 | Processor obligations | Core article — DPA mandatory terms, sub-processors |
| GDPR Article 28(2) | Sub-processor authorization | Prior written authorization required |
| GDPR Article 28(3)(a-h) | 8 mandatory DPA terms | Compliance checklist for DPA content |
| GDPR Article 28(9) | Written form requirement | DPA must be in writing (electronic form accepted) |
| GDPR Article 46 | Standard Contractual Clauses | International transfer safeguards |
| GDPR Article 5(2) | Accountability principle | Audit trail requirement |
| GDPR Article 30 | Records of processing | Processor registration feeds RoPA |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| `IPipelineBehavior<,>` | `Encina/` | Pipeline behavior registration |
| `INotification` | `Encina/` | Domain notification publishing |
| `IRequestContext` | `Encina/` | TenantId, ModuleId context |
| `EncinaError` | `Encina/` | ROP error type |
| `DPIARequiredPipelineBehavior` | `Encina.Compliance.DPIA/` | Pattern reference for pipeline behavior |
| `DPIADiagnostics` | `Encina.Compliance.DPIA/Diagnostics/` | Diagnostics pattern reference |
| `DPIAOptions` | `Encina.Compliance.DPIA/` | Options pattern reference |
| `DPIAReviewReminderService` | `Encina.Compliance.DPIA/` | Background service pattern reference (DC 6 uses Encina.Scheduling instead) |
| `DPIAHealthCheck` | `Encina.Compliance.DPIA/Health/` | Health check pattern reference |
| `GDPRErrors` | `Encina.Compliance.GDPR/` | Error factory pattern reference |
| `ICommand<T>` / `ICommandHandler` | `Encina.Messaging/` | Scheduling command pattern (DC 6) |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| Encina.Compliance.GDPR | 8100-8199 | Core GDPR |
| Encina.Compliance.Consent | 8200-8299 | Consent management |
| Encina.Compliance.DataSubjectRights | 8300-8399 | DSR management |
| Encina.Compliance.Anonymization | 8400-8499 | Anonymization |
| Encina.Compliance.Retention | 8500-8599 | Data retention |
| Encina.Compliance.DataResidency | 8600-8699 | Data residency |
| Encina.Compliance.BreachNotification | 8700-8799 | Breach notification |
| Encina.Compliance.DPIA | 8800-8899 | DPIA management |
| **Encina.Compliance.ProcessorAgreements** | **8900-8999** | **NEW — DPA management** |

### Estimated File Count

| Category | Count | Notes |
|----------|-------|-------|
| **Core package (Phases 1-3)** | | |
| Project file (.csproj) | 1 | |
| Models & enums | 9 | Processor, DataProcessingAgreement, DPAMandatoryTerms, DPAValidationResult, AuditEntry + 3 enums + DPAStatus (DC 2 adds 1) |
| Interfaces | 4 | IProcessorRegistry, IDPAStore (NEW — DC 2), IDPAValidator, IProcessorAuditStore |
| Attributes | 1 | |
| Error factory | 1 | |
| Default implementations | 6 | InMemoryProcessorRegistry, InMemoryDPAStore (NEW — DC 2), InMemoryProcessorAuditStore, DefaultDPAValidator, CheckDPAExpirationCommand, CheckDPAExpirationHandler (DC 6) |
| Persistence entities & mappers | 6 | ProcessorEntity + Mapper, DPAEntity + Mapper (DC 2 adds 2), AuditEntity + Mapper |
| Pipeline behavior | 1 | |
| Options & validation | 2 | |
| DI registration | 1 | |
| Health check | 1 | |
| Diagnostics | 2 | |
| Notifications | 7 | +1 DPASignedNotification (DC 2) |
| PublicAPI files | 2 | |
| **Core subtotal** | **~44** | |
| **Provider implementations (Phases 4-8)** | | |
| MessagingConfiguration (Phase 4) | 1 | UseProcessorAgreements flag |
| EF Core stores (Phase 5) | 7 | 3 stores + 3 entity configs + 1 ModelBuilder extension |
| ADO.NET stores (Phase 6) | 24 | 3 stores x 4 providers + 3 SQL scripts x 4 providers |
| Dapper stores (Phase 7) | 12 | 3 stores x 4 providers (reuse ADO SQL scripts) |
| MongoDB stores (Phase 8) | 6 | 3 stores + 3 BSON documents |
| DI registration updates | 10 | 1 EF Core + 4 ADO + 4 Dapper + 1 MongoDB |
| PublicAPI updates | 10 | Per provider package |
| **Provider subtotal** | **~70** | |
| **Testing (Phase 13)** | | |
| Unit tests | ~15 | Core package unit tests |
| Guard tests | ~4 | |
| Contract tests | ~4 | |
| Property tests | ~5 | |
| Integration tests | ~39 | 3 stores x 13 providers |
| Justification .md files | 2 | Load + Benchmark |
| **Testing subtotal** | **~69** | |
| **Documentation (Phase 14)** | | |
| README, features, INVENTORY, etc. | ~5 | Package README, feature doc, CHANGELOG, ROADMAP, INVENTORY |
| **Documentation subtotal** | **~5** | |
| **Total** | **~188** | |

---

## Combined AI Agent Prompt

<details>
<summary><strong>Complete Implementation Prompt — All Phases</strong></summary>

```
You are implementing Encina.Compliance.ProcessorAgreements (Issue #410) — GDPR Article 28 Data Processing Agreement management.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library using Railway Oriented Programming (Either<EncinaError, T>)
- Pre-1.0: choose the best solution, no backward compatibility concerns
- Nullable reference types enabled everywhere
- All domain models are sealed records with XML documentation
- LanguageExt provides Option<T>, Either<L, R>, Unit
- Timestamps use DateTimeOffset with AtUtc suffix convention
- Pipeline behaviors use static ConcurrentDictionary for attribute caching
- DI uses TryAdd* pattern for satellite provider override support
- Compliance packages follow a consistent structure (see DPIA, Consent, DSR packages)

CRITICAL DESIGN DECISIONS:
- DC 2: SEPARATE ENTITIES — Processor (identity, long-lived) and DataProcessingAgreement (contractual state, temporal)
  - IProcessorRegistry manages Processor entities
  - IDPAStore manages DataProcessingAgreement entities (NEW separate store)
  - IDPAValidator queries BOTH stores
- DC 5: DEPTH TRACKING — Processor has ParentProcessorId (string?) + Depth (int)
  - 0 = top-level, 1 = direct sub-processor, etc.
  - MaxSubProcessorDepth option (default: 3) bounds the hierarchy
  - GetFullSubProcessorChainAsync traverses ParentProcessorId recursively
  - RegisterProcessorAsync validates depth constraints
- DC 6: ENCINA.SCHEDULING — Expiration monitoring uses scheduled command (not IHostedService)
  - CheckDPAExpirationCommand : ICommand<Unit>
  - CheckDPAExpirationHandler : ICommandHandler — queries IDPAStore, publishes notifications
  - Registered when EnableExpirationMonitoring == true
  - Project depends on Encina.Messaging (for scheduling)

IMPLEMENTATION OVERVIEW:
Create a NEW package: src/Encina.Compliance.ProcessorAgreements/ + provider implementations in 13 satellite packages

Phase 1 — Core Models: 3 enums, 5 domain records (Processor, DataProcessingAgreement, DPAMandatoryTerms, DPAValidationResult, AuditEntry), 7 notifications
Phase 2 — Interfaces: IProcessorRegistry (7 methods + hierarchy), IDPAStore (7 methods — NEW), IDPAValidator (3 methods), IProcessorAuditStore (2 methods), [RequiresProcessor] attribute, ProcessorAgreementErrors
Phase 3 — Implementations: InMemoryProcessorRegistry (with depth validation), InMemoryDPAStore (NEW), InMemoryProcessorAuditStore, DefaultDPAValidator (queries both stores), entities + mappers for both
Phase 4 — MessagingConfiguration: UseProcessorAgreements flag + IsAnyEnabled
Phase 5 — EF Core Provider: 3 stores (ProcessorRegistryEF, DPAStoreEF, ProcessorAuditStoreEF) + entity configs + ModelBuilder extensions — covers 4 databases
Phase 6 — ADO.NET Providers: 3 stores x 4 databases (SqlServer, SQLite, PostgreSQL, MySQL) + SQL scripts — 12 store implementations
Phase 7 — Dapper Providers: 3 stores x 4 databases — 12 store implementations (reuse ADO SQL scripts)
Phase 8 — MongoDB Provider: 3 stores + 3 BSON documents
Phase 9 — Pipeline: ProcessorValidationPipelineBehavior<TRequest, TResponse> with Block/Warn/Disabled modes
Phase 10 — DI: ProcessorAgreementOptions (with MaxSubProcessorDepth), ServiceCollectionExtensions (registers IDPAStore), CheckDPAExpirationCommand/Handler (via Scheduling), HealthCheck
Phase 11 — Cross-cutting: Evaluate ALL code from Phases 1-10 against 12 transversal functions. REFACTOR any previous phase if needed for coherence. Multi-tenancy (TenantId on both entities), audit trail (ProcessorId + DPAId), validation (depth + fields), transactions
Phase 12 — Observability: ActivitySource + Meter + [LoggerMessage] (EventIds 8900-8999) across core and ALL 13 provider implementations
Phase 13 — Testing: Unit (~90), Guard (~45), Contract (~17), Property (~17), Integration (3 stores x 13 providers = 39 test files via Docker/Testcontainers)
Phase 14 — Documentation: XML doc comments, CHANGELOG, ROADMAP, package README (all providers), feature docs, INVENTORY, PublicAPI.txt verification

KEY PATTERNS (from DPIA reference):
- Pipeline: ConcurrentDictionary<Type, Attribute?> cache, Stopwatch.GetTimestamp, Activity + Counter + Histogram
- Options: sealed class, IValidateOptions<T>, BlockWithoutValidDPA alias, MaxSubProcessorDepth
- DI: TryAdd*, conditional registration from local options instance, Encina.Scheduling for recurring commands
- Health: DefaultName const, Tags static array, scoped resolution
- Diagnostics: static ActivitySource + Meter, [LoggerMessage] source generator
- Errors: static class with factory methods, error code prefix "processor.", includes "sub_processor_depth_exceeded"
- Scheduling: CheckDPAExpirationCommand/Handler replaces IHostedService pattern (DC 6)

DPAMandatoryTerms (Art. 28(3)):
(a) ProcessOnDocumentedInstructions
(b) ConfidentialityObligations
(c) SecurityMeasures
(d) SubProcessorRequirements
(e) DataSubjectRightsAssistance
(f) ComplianceAssistance
(g) DataDeletionOrReturn
(h) AuditRights

REFERENCE FILES:
- src/Encina.Compliance.DPIA/ (primary pattern reference — structure, DI, pipeline, diagnostics, health)
- src/Encina.Compliance.GDPR/GDPRErrors.cs (error factory)
- src/Encina.Compliance.Consent/ (alternative compliance package reference)
- src/Encina.Messaging/ (Encina.Scheduling API — ICommand, ICommandHandler)
- tests/Encina.UnitTests/Compliance/DPIA/ (unit test patterns)
- tests/Encina.IntegrationTests/ (integration test patterns for all providers)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | **Caching** | ❌ Not Applicable | Compliance validation requires real-time accuracy; no Encina compliance module caches store results. |
| 2 | **OpenTelemetry** | ✅ Included | Already implemented: ActivitySource for pipeline checks + validations + background monitoring. Meter for counters + histograms. |
| 3 | **Structured Logging** | ✅ Included | Already implemented: [LoggerMessage] source generator, EventIds 8900-8949, no PII in logs. |
| 4 | **Health Checks** | ✅ Included | Already implemented: ProcessorAgreementHealthCheck: registry health, expired DPAs, incomplete terms. |
| 5 | **Validation** | ✅ Included | Already implemented: ProcessorAgreementOptionsValidator + DefaultDPAValidator with Art. 28(3) term checks. |
| 6 | **Resilience** | ⏭️ Deferred #773 | Correct deferral — all stores are local DB with in-memory defaults. No external calls to wrap with retry/circuit breaker. |
| 7 | **Distributed Locks** | ❌ Not Applicable | ConcurrentDictionary handles in-memory concurrency; DB providers use database-level locking. No cross-process coordination needed. |
| 8 | **Transactions** | ✅ Included | Per-operation atomicity in DB providers. In-memory stores use ConcurrentDictionary thread-safety. |
| 9 | **Idempotency** | ⏭️ Deferred #774 | Correct deferral — store-level "already exists" errors sufficient. InboxPipelineBehavior needed when consuming from queues. |
| 10 | **Multi-Tenancy** | ✅ Included | TenantId fields exist in Processor, DPA, and AuditEntry models. Filtering is cross-package (tenant-aware store decorators). |
| 11 | **Module Isolation** | ⏭️ Deferred #775 | Correct deferral — ModuleId fields exist in all models. Store-level filtering is a cross-package initiative (~1,400+ lines across 14 stores). |
| 12 | **Audit Trail** | ✅ Included | Phase 11: Wired IProcessorAuditStore into ProcessorValidationPipelineBehavior (Block path) and CheckDPAExpirationHandler (expired + expiring). TrackAuditTrail option added. TenantId/ModuleId propagated through all 13 provider stores + MongoDB document. |
