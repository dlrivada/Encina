# Implementation Plan: `Encina.Compliance.ProcessorAgreements` — Data Processing Agreement Management (Art. 28)

> **Issue**: [#410](https://github.com/dlrivada/Encina/issues/410)
> **Type**: Feature
> **Complexity**: Medium (8 phases, provider-independent, ~50-60 files)
> **Estimated Scope**: ~2,500-3,500 lines of production code + ~2,000-2,500 lines of tests

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
<summary><strong>2. Domain Model Design — <code>ProcessorRecord</code> as rich aggregate with DPA lifecycle</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Rich `ProcessorRecord` with embedded DPA state** | Single entity encapsulates processor + agreement state, simple queries | Larger record, status transitions need careful design |
| **B) Separate `Processor` and `DataProcessingAgreement` entities** | Normalized model, supports multiple DPAs per processor over time | More complex queries, joins required, over-engineered for v1 |
| **C) Simple flat record** | Minimal, fast to implement | Doesn't capture sub-processor relationships or DPA lifecycle |

### Chosen Option: **A — Rich `ProcessorRecord` with embedded DPA state**

### Rationale

- A single `ProcessorRecord` tracks the processor AND its current DPA state — matches the issue specification exactly
- `DPAStatus` enum provides clear lifecycle: `Active`, `Expired`, `PendingRenewal`, `Terminated`
- Sub-processor tracking via `SubProcessorIds` list (Art. 28(2) — prior authorization for sub-processors)
- SCC compliance tracked via `HasSCCs` flag (Art. 46 — appropriate safeguards for international transfers)
- `DPAMandatoryTerms` value object captures Art. 28(3) mandatory clauses with boolean compliance flags
- If a full DPA version history is needed later, a separate `DPAHistory` entity can be added without breaking changes

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
<summary><strong>5. Sub-Processor Management — Hierarchical tracking with authorization workflow</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Flat list with parent reference** | Simple queries, no recursive traversal | Doesn't model deep hierarchies |
| **B) Tree structure with recursive queries** | Models real-world sub-processor chains | Complex queries, potential infinite recursion |
| **C) Parent ID + depth tracking** | Balanced — supports hierarchy with bounded depth | Slightly more complex than flat list |

### Chosen Option: **A — Flat list with parent reference**

### Rationale

- Art. 28(2) requires prior specific or general written authorization for sub-processors
- `ProcessorRecord.SubProcessorIds` maintains the direct sub-processor relationship
- `IProcessorRegistry.GetSubProcessorsAsync(processorId)` returns immediate sub-processors
- For typical enterprise use, sub-processor chains are rarely more than 2 levels deep
- The `SubProcessorAuthorizationType` enum (`Specific`, `General`) tracks the authorization model
- Sub-processor change notifications handled via domain events (`SubProcessorAddedNotification`, `SubProcessorRemovedNotification`)

</details>

<details>
<summary><strong>6. DPA Expiration Alerting — Background service with configurable lead time</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Background hosted service with periodic checks** | Follows DPIA expiration monitoring pattern, reliable | Adds a hosted service |
| **B) On-demand check during pipeline validation** | No background service | Doesn't alert until a request hits the processor |
| **C) Scheduled message via Encina.Scheduling** | Leverages existing infrastructure | Requires scheduling setup |

### Chosen Option: **A — Background hosted service with periodic checks**

### Rationale

- `DPAExpirationMonitorService : IHostedService` checks processors approaching expiration
- Configurable alert window: `AlertBeforeExpirationDays` (default: 30)
- Publishes `DPAExpiringNotification` when a DPA is within the alert window
- Publishes `DPAExpiredNotification` when a DPA has expired
- Follows the exact same pattern as `DPIAReviewReminderService`
- Only registered when `EnableExpirationMonitoring = true` (opt-in)

</details>

---

## Implementation Phases

### Phase 1: Core Models, Enums & Domain Records

> **Goal**: Establish the foundational types that all other phases depend on.

<details>
<summary><strong>Tasks</strong></summary>

#### New project: `src/Encina.Compliance.ProcessorAgreements/`

1. **Create project file** `Encina.Compliance.ProcessorAgreements.csproj`
   - Target: `net10.0`
   - Dependencies: `Encina.Compliance.GDPR`, `LanguageExt.Core`, `Microsoft.Extensions.Logging.Abstractions`, `Microsoft.Extensions.Options`, `Microsoft.Extensions.Hosting.Abstractions`
   - Enable nullable, implicit usings, XML doc

2. **Enums** (`Model/` folder):
   - `DPAStatus` — `Active`, `Expired`, `PendingRenewal`, `Terminated`
   - `SubProcessorAuthorizationType` — `Specific`, `General` (Art. 28(2))
   - `ProcessorAgreementEnforcementMode` — `Block`, `Warn`, `Disabled`

3. **Domain records** (`Model/` folder):
   - `ProcessorRecord` — sealed record:
     - `Id (string)`, `Name (string)`, `Country (string)`, `ContactEmail (string?)`
     - `ProcessingPurposes (IReadOnlyList<string>)` — documented processing purposes
     - `DPASignedAtUtc (DateTimeOffset)`, `DPAExpiresAtUtc (DateTimeOffset?)`
     - `SubProcessorIds (IReadOnlyList<string>)`, `SubProcessorAuthorizationType`
     - `HasSCCs (bool)` — Standard Contractual Clauses present
     - `Status (DPAStatus)`
     - `MandatoryTerms (DPAMandatoryTerms)` — Art. 28(3) compliance
     - `TenantId (string?)`, `ModuleId (string?)`
     - `CreatedAtUtc (DateTimeOffset)`, `LastUpdatedAtUtc (DateTimeOffset)`
     - Computed: `IsActive` → `Status == Active && (DPAExpiresAtUtc is null || DPAExpiresAtUtc > now)`
   - `DPAMandatoryTerms` — sealed record (Art. 28(3) eight mandatory terms):
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
   - `DPAValidationResult` — sealed record:
     - `ProcessorId (string)`, `IsValid (bool)`, `Status (DPAStatus)`
     - `MissingTerms (IReadOnlyList<string>)`, `Warnings (IReadOnlyList<string>)`
     - `DaysUntilExpiration (int?)`, `ValidatedAtUtc (DateTimeOffset)`
   - `ProcessorAgreementAuditEntry` — sealed record:
     - `Id (string)`, `ProcessorId (string)`, `Action (string)`, `Detail (string?)`
     - `PerformedByUserId (string?)`, `OccurredAtUtc (DateTimeOffset)`

4. **Notification records** (`Notifications/` folder):
   - `ProcessorRegisteredNotification : INotification` — `ProcessorId`, `ProcessorName`, `OccurredAtUtc`
   - `DPAExpiringNotification : INotification` — `ProcessorId`, `ProcessorName`, `DPAExpiresAtUtc`, `DaysUntilExpiration`, `OccurredAtUtc`
   - `DPAExpiredNotification : INotification` — `ProcessorId`, `ProcessorName`, `ExpiredAtUtc`, `OccurredAtUtc`
   - `DPATerminatedNotification : INotification` — `ProcessorId`, `ProcessorName`, `OccurredAtUtc`
   - `SubProcessorAddedNotification : INotification` — `ProcessorId`, `SubProcessorId`, `OccurredAtUtc`
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

TASK:
Create the project file and all model types listed in Phase 1 Tasks:
- 3 enums: DPAStatus, SubProcessorAuthorizationType, ProcessorAgreementEnforcementMode
- 4 domain records: ProcessorRecord, DPAMandatoryTerms, DPAValidationResult, ProcessorAgreementAuditEntry
- 6 notification records implementing INotification
- PublicAPI.Unshipped.txt

KEY RULES:
- Target net10.0, enable nullable, enable implicit usings
- All types are sealed records (not classes)
- All public types need XML documentation with <summary>, <remarks>, and GDPR Article 28 references
- DPAMandatoryTerms has 8 bool properties mapping exactly to Art. 28(3)(a)-(h)
- DPAMandatoryTerms.IsFullyCompliant computed property checks all 8 terms
- ProcessorRecord.IsActive requires a DateTimeOffset nowUtc parameter (not property, method)
- Notification records implement INotification from Encina core
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

> **Goal**: Define the public API surface — interfaces, attributes, and error factory.

<details>
<summary><strong>Tasks</strong></summary>

1. **Attribute** (`Attributes/` folder):
   - `RequiresProcessorAttribute` — `[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]`
     - Properties: `ProcessorId (string)` — required, identifies the processor this request depends on
     - Purpose: marks request types that should be validated against the processor registry

2. **Core interfaces** (`Abstractions/` folder):
   - `IProcessorRegistry` — processor CRUD + sub-processor queries:
     - `RegisterProcessorAsync(ProcessorRecord, CancellationToken)` → `Either<EncinaError, Unit>`
     - `GetProcessorAsync(string processorId, CancellationToken)` → `Either<EncinaError, Option<ProcessorRecord>>`
     - `GetAllProcessorsAsync(CancellationToken)` → `Either<EncinaError, IReadOnlyList<ProcessorRecord>>`
     - `GetSubProcessorsAsync(string processorId, CancellationToken)` → `Either<EncinaError, IReadOnlyList<ProcessorRecord>>`
     - `UpdateProcessorAsync(ProcessorRecord, CancellationToken)` → `Either<EncinaError, Unit>`
     - `RemoveProcessorAsync(string processorId, CancellationToken)` → `Either<EncinaError, Unit>`
     - `GetProcessorsByStatusAsync(DPAStatus, CancellationToken)` → `Either<EncinaError, IReadOnlyList<ProcessorRecord>>`
     - `GetExpiringProcessorsAsync(DateTimeOffset threshold, CancellationToken)` → `Either<EncinaError, IReadOnlyList<ProcessorRecord>>`
   - `IDPAValidator` — DPA validation:
     - `ValidateAsync(string processorId, CancellationToken)` → `Either<EncinaError, DPAValidationResult>`
     - `HasValidDPAAsync(string processorId, CancellationToken)` → `Either<EncinaError, bool>`
     - `ValidateAllAsync(CancellationToken)` → `Either<EncinaError, IReadOnlyList<DPAValidationResult>>`
   - `IProcessorAuditStore` — audit trail:
     - `RecordAsync(ProcessorAgreementAuditEntry, CancellationToken)` → `Either<EncinaError, Unit>`
     - `GetAuditTrailAsync(string processorId, CancellationToken)` → `Either<EncinaError, IReadOnlyList<ProcessorAgreementAuditEntry>>`

3. **Error codes** (`ProcessorAgreementErrors.cs`):
   - Error code prefix: `processor.`
   - Codes:
     - `processor.not_found` — processor ID not in registry
     - `processor.already_exists` — duplicate processor ID
     - `processor.dpa_missing` — no DPA signed for processor
     - `processor.dpa_expired` — DPA has expired
     - `processor.dpa_terminated` — DPA was terminated
     - `processor.dpa_pending_renewal` — DPA is pending renewal
     - `processor.dpa_incomplete` — mandatory terms not fully met (with list of missing terms)
     - `processor.sub_processor_unauthorized` — sub-processor not authorized
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

TASK:
Create the attribute, all interfaces, and error factory listed in Phase 2 Tasks.

KEY RULES:
- [RequiresProcessor] targets classes (AttributeTargets.Class) — marks request types
- ProcessorId is a required string property on the attribute
- All interface methods take CancellationToken as last parameter
- IProcessorRegistry.GetSubProcessorsAsync returns the actual ProcessorRecord objects (not just IDs)
- IDPAValidator.HasValidDPAAsync is lightweight — used in pipeline behavior hot path
- IProcessorAuditStore follows the same pattern as IDPIAAuditStore
- ProcessorAgreementErrors follows the exact DPIAErrors pattern: static class, factory methods
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

> **Goal**: Provide working implementations for development/testing without database dependencies.

<details>
<summary><strong>Tasks</strong></summary>

1. **In-memory stores**:
   - `InMemoryProcessorRegistry : IProcessorRegistry` — `ConcurrentDictionary<string, ProcessorRecord>`, `TimeProvider` for expiration queries
     - `GetSubProcessorsAsync`: resolves `SubProcessorIds` to full `ProcessorRecord` objects
     - `GetExpiringProcessorsAsync`: filters by `DPAExpiresAtUtc <= threshold`
     - `GetProcessorsByStatusAsync`: filters by `Status`
   - `InMemoryProcessorAuditStore : IProcessorAuditStore` — `ConcurrentDictionary<string, List<ProcessorAgreementAuditEntry>>`

2. **DPA Validator** (`DefaultDPAValidator.cs`):
   - Implements `IDPAValidator`
   - Dependencies: `IProcessorRegistry`, `IOptions<ProcessorAgreementOptions>`, `TimeProvider`, `ILogger`
   - `ValidateAsync` flow:
     1. Get processor from registry
     2. Check DPA status (Active, Expired, Terminated, PendingRenewal)
     3. Check expiration date against `TimeProvider.GetUtcNow()`
     4. Check mandatory terms via `DPAMandatoryTerms.IsFullyCompliant`
     5. Check sub-processor authorization if `RequireSubProcessorAuthorization` is enabled
     6. Generate warnings for approaching expiration (`AlertBeforeExpirationDays`)
     7. Return `DPAValidationResult` with all findings
   - `HasValidDPAAsync`: lightweight check — processor exists + status Active + not expired + terms compliant

3. **Persistence entities & mappers**:
   - `ProcessorRecordEntity` — mutable class for ORM compatibility
   - `ProcessorRecordMapper` — `ToEntity(ProcessorRecord)` / `ToDomain(ProcessorRecordEntity)`
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

TASK:
Create all default implementations listed in Phase 3 Tasks:
- InMemoryProcessorRegistry, InMemoryProcessorAuditStore
- DefaultDPAValidator
- Persistence entities and mappers

KEY RULES:
- InMemoryProcessorRegistry uses ConcurrentDictionary<string, ProcessorRecord>
- GetSubProcessorsAsync resolves SubProcessorIds to actual ProcessorRecord objects from the same dictionary
- GetExpiringProcessorsAsync filters where DPAExpiresAtUtc is not null AND <= threshold
- DefaultDPAValidator checks ALL mandatory terms — reports individual missing terms in result
- HasValidDPAAsync is the fast-path: exists + Active + not expired + fully compliant (no detailed reporting)
- Entity classes use public get/set properties (mutable for ORMs)
- Mapper.ToEntity/ToDomain follow the DPIAAssessmentMapper pattern
- DPAMandatoryTerms serialized as 8 individual boolean columns in entities
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

### Phase 4: Pipeline Behavior — `ProcessorValidationPipelineBehavior`

> **Goal**: Implement the pipeline behavior that validates processor DPAs before request execution.

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
     5. Call `IDPAValidator.HasValidDPAAsync(attribute.ProcessorId)`
     6. If valid → record passed + `nextStep()`
     7. If invalid + Block mode → call `IDPAValidator.ValidateAsync` for detailed result → return `Left(error)`
     8. If invalid + Warn mode → log warning + `nextStep()`
   - Observability: traces via `ProcessorAgreementDiagnostics`, counters, structured logging
   - Dependencies: `IDPAValidator`, `IOptions<ProcessorAgreementOptions>`, `TimeProvider`, `ILogger`

2. **Validation detail on Block**: When blocking, perform full `ValidateAsync` to include detailed failure reasons in the error (missing terms, expired date, etc.)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of Encina.Compliance.ProcessorAgreements (Issue #410).

CONTEXT:
- Phases 1-3 are implemented (models, interfaces, in-memory stores, validator)
- This pipeline behavior follows the EXACT same pattern as DPIARequiredPipelineBehavior
- It intercepts requests marked with [RequiresProcessor] and validates the referenced processor's DPA

TASK:
Create ProcessorValidationPipelineBehavior<TRequest, TResponse>.

KEY RULES:
- Use static ConcurrentDictionary<Type, RequiresProcessorAttribute?> for attribute caching
- Two-level validation: fast HasValidDPAAsync for pass-through, detailed ValidateAsync only when blocking
- Three enforcement modes: Block returns Left(error with details), Warn logs + continues, Disabled skips
- On Block: include DPAValidationResult details in the error (missing terms, expiration info)
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

### Phase 5: Configuration, DI & Background Services

> **Goal**: Wire everything together with options, service registration, auto-registration, and expiration monitoring.

<details>
<summary><strong>Tasks</strong></summary>

1. **Options** (`ProcessorAgreementOptions.cs`):
   - `ProcessorAgreementEnforcementMode EnforcementMode { get; set; }` — default: `Warn`
   - `bool BlockWithoutValidDPA { get; set; }` — alias for `EnforcementMode.Block`
   - `int AlertBeforeExpirationDays { get; set; }` — default: `30`
   - `bool RequireSubProcessorAuthorization { get; set; }` — default: `true`
   - `bool NotifyOnSubProcessorChange { get; set; }` — default: `true`
   - `bool PublishNotifications { get; set; }` — default: `true`
   - `bool TrackAuditTrail { get; set; }` — default: `true`
   - `bool EnableExpirationMonitoring { get; set; }` — default: `false`
   - `TimeSpan ExpirationCheckInterval { get; set; }` — default: `TimeSpan.FromHours(1)`
   - `bool AddHealthCheck { get; set; }` — default: `false`

2. **Options validator** (`ProcessorAgreementOptionsValidator.cs`):
   - `IValidateOptions<ProcessorAgreementOptions>`
   - Validates: `AlertBeforeExpirationDays > 0`, `ExpirationCheckInterval > TimeSpan.Zero`

3. **Service collection extensions** (`ServiceCollectionExtensions.cs`):
   - `AddEncinaProcessorAgreements(this IServiceCollection services, Action<ProcessorAgreementOptions>? configure = null)`
   - Registers:
     - `ProcessorAgreementOptions` via `services.Configure()`
     - `ProcessorAgreementOptionsValidator` via `TryAddSingleton<IValidateOptions<>>`
     - `TimeProvider.System` via `TryAddSingleton`
     - `IProcessorRegistry` → `InMemoryProcessorRegistry` via `TryAddSingleton`
     - `IProcessorAuditStore` → `InMemoryProcessorAuditStore` via `TryAddSingleton`
     - `IDPAValidator` → `DefaultDPAValidator` via `TryAddScoped`
     - `ProcessorValidationPipelineBehavior<,>` via `TryAddTransient(typeof(IPipelineBehavior<,>))`
   - Conditional: health check if `AddHealthCheck == true`
   - Conditional: expiration monitoring if `EnableExpirationMonitoring == true`

4. **Expiration monitoring** (`DPAExpirationMonitorService.cs`):
   - `internal sealed class DPAExpirationMonitorService : IHostedService, IDisposable`
   - Periodic check using `PeriodicTimer` (same pattern as `DPIAReviewReminderService`)
   - On each tick: query `IProcessorRegistry.GetExpiringProcessorsAsync` → publish notifications
   - Also checks for newly expired processors → updates status if needed

5. **Health check** (`Health/ProcessorAgreementHealthCheck.cs`):
   - `public sealed class ProcessorAgreementHealthCheck : IHealthCheck`
   - `const string DefaultName = "encina-processor-agreements"`
   - Tags: `["encina", "gdpr", "processor", "dpa", "compliance", "ready"]`
   - Checks: registry resolvable, expired DPAs (degraded), incomplete mandatory terms (degraded)
   - Uses scoped resolution pattern

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of Encina.Compliance.ProcessorAgreements (Issue #410).

CONTEXT:
- Phases 1-4 are implemented (models, interfaces, stores, validator, pipeline behavior)
- DI registration follows the TryAdd pattern — satellite providers pre-register concrete stores
- Background services use IHostedService with PeriodicTimer
- Health checks use IServiceProvider.CreateScope() for scoped service resolution

TASK:
Create options, DI registration, expiration monitoring, and health check.

KEY RULES:
- Options pattern: sealed class with defaults, IValidateOptions<T> for validation
- ServiceCollectionExtensions uses TryAdd* for all registrations (satellite overrides)
- Instantiate a local optionsInstance to read flags before DI is fully built
- DPAExpirationMonitorService follows DPIAReviewReminderService pattern exactly
- Health check: Unhealthy if core services missing, Degraded if expired/incomplete DPAs, Healthy otherwise
- Pipeline behavior registered with TryAddTransient(typeof(IPipelineBehavior<,>))
- BlockWithoutValidDPA is a convenience alias (same pattern as DPIA.BlockWithoutDPIA)

REFERENCE FILES:
- src/Encina.Compliance.DPIA/DPIAOptions.cs (options pattern with BlockWithoutDPIA alias)
- src/Encina.Compliance.DPIA/DPIAOptionsValidator.cs (options validator)
- src/Encina.Compliance.DPIA/ServiceCollectionExtensions.cs (DI registration pattern)
- src/Encina.Compliance.DPIA/DPIAReviewReminderService.cs (background service pattern)
- src/Encina.Compliance.DPIA/Health/DPIAHealthCheck.cs (health check pattern)
```

</details>

---

### Phase 6: Cross-Cutting Integration

> **Goal**: Integrate with transversal functions marked ✅ in the cross-cutting matrix.

<details>
<summary><strong>Tasks</strong></summary>

1. **Multi-Tenancy** (✅ Included):
   - `ProcessorRecord.TenantId` field already in model (Phase 1)
   - Pipeline behavior: propagate `IRequestContext.TenantId` to activity tags
   - `InMemoryProcessorRegistry`: filter by TenantId when `IRequestContext` is available
   - Store queries respect tenant isolation

2. **Audit Trail** (✅ Included):
   - `IProcessorAuditStore` already defined (Phase 2)
   - `DefaultDPAValidator`: record audit entry on every validation
   - Pipeline behavior: record audit entry on block/warn
   - DPA lifecycle events: record on register, update, terminate, expire
   - Audit entries include: action, detail, user ID, timestamp

3. **Validation** (✅ Included):
   - `ProcessorAgreementOptionsValidator` validates configuration (Phase 5)
   - `DefaultDPAValidator.ValidateAsync` validates DPA content against Art. 28(3) requirements
   - `ProcessorRecord` validation: required fields (Id, Name, Country), valid DPASignedAtUtc

4. **Transactions** (✅ Included):
   - Store operations (register, update, remove) are atomic
   - In-memory stores use ConcurrentDictionary for thread-safe operations
   - Database-backed stores (future) will participate in ambient transactions

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of Encina.Compliance.ProcessorAgreements (Issue #410).

CONTEXT:
- Phases 1-5 are implemented (full core package with pipeline behavior, DI, health check)
- Cross-cutting integrations must be woven into existing code, not added as separate files
- Multi-tenancy, audit trail, validation, and transactions are the active integrations

TASK:
Ensure all cross-cutting integrations are properly wired:
1. Multi-Tenancy: TenantId propagation in pipeline behavior, store filtering
2. Audit Trail: Record entries in validator, pipeline behavior, DPAExpirationMonitorService
3. Validation: ProcessorRecord validation in registry (reject incomplete records)
4. Transactions: Atomic store operations

KEY RULES:
- TenantId comes from IRequestContext in pipeline behavior, from ProcessorRecord.TenantId in stores
- Audit entries use the pattern: new ProcessorAgreementAuditEntry { Action = "...", Detail = "...", OccurredAtUtc = timeProvider.GetUtcNow() }
- Record audit BEFORE and AFTER significant operations (started/completed/failed)
- Validation happens at the boundary: RegisterProcessorAsync validates required fields before persisting
- Do NOT add unnecessary complexity — keep integrations minimal and focused

REFERENCE FILES:
- src/Encina.Compliance.DPIA/DPIARequiredPipelineBehavior.cs (tenant context propagation)
- src/Encina.Compliance.DPIA/InMemoryDPIAAuditStore.cs (audit recording pattern)
```

</details>

---

### Phase 7: Observability — Diagnostics, Metrics & Logging

> **Goal**: Add OpenTelemetry traces, counters, and structured logging for processor agreement operations.

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
     - `processor.expirations.total` — tagged by `processor_id`
   - **Histograms** (Histogram<double>):
     - `processor.pipeline.check.duration` — tagged by `request_type`
     - `processor.validation.duration` — tagged by `processor_id`
   - **Tag constants**: `TagProcessorId`, `TagRequestType`, `TagOutcome`, `TagFailureReason`, `TagEnforcementMode`, `TagStatus`
   - **Activity helpers**:
     - `StartPipelineCheck(string requestTypeName)` → `Activity?`
     - `RecordPassed(Activity?)` / `RecordFailed(Activity?, string reason)` / `RecordWarned(Activity?, string reason)` / `RecordSkipped(Activity?)`

2. **`ProcessorAgreementLogMessages.cs`** (`Diagnostics/` folder):
   - `internal static partial class ProcessorAgreementLogMessages` using `[LoggerMessage]` source generator
   - **Event ID range: 8900-8999** (next available after DPIA's 8800-8899):
     - 8900-8909: Pipeline behavior (started, passed, failed, blocked, warned, disabled, no attribute, error)
     - 8910-8919: Validation (started, completed, invalid, missing terms, expired, SCC required)
     - 8920-8929: Registry operations (registered, updated, removed, not found, duplicate)
     - 8930-8939: Expiration monitoring (check started, expiring found, expired found, notification published)
     - 8940-8949: Sub-processor operations (added, removed, unauthorized, change notification)
     - 8950-8959: Health check (completed, degraded, unhealthy)
     - 8960-8969: Audit trail (recorded, query completed)
     - 8970-8979: Store errors
     - 8980-8999: Reserved

3. **Integrate observability into existing code**:
   - Pipeline behavior: traces + metrics + logging (already partially done in Phase 4)
   - DefaultDPAValidator: traces + logging for validation operations
   - DPAExpirationMonitorService: logging for background scan cycles
   - InMemoryProcessorRegistry: logging for CRUD operations

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of Encina.Compliance.ProcessorAgreements (Issue #410).

CONTEXT:
- Phases 1-6 are implemented (full core + cross-cutting integrations)
- Observability follows OpenTelemetry patterns: ActivitySource for traces, Meter for metrics, ILogger for logs
- Event IDs in 8900-8999 range (new range, avoids collision with DPIA 8800-8899)

TASK:
Create ProcessorAgreementDiagnostics, ProcessorAgreementLogMessages, and integrate into existing code.

KEY RULES:
- ActivitySource name: "Encina.Compliance.ProcessorAgreements" (matches package name)
- Meter: new Meter("Encina.Compliance.ProcessorAgreements", "1.0")
- All counters use tag-based dimensions for flexible dashboards
- ProcessorAgreementLogMessages uses [LoggerMessage] source generator (partial class, partial methods)
- Log messages: structured with named parameters, NO PII in log messages
- Activity helpers check Source.HasListeners() before creating activities
- Duration recording uses Stopwatch.GetTimestamp/GetElapsedTime pattern
- CompletePipelineCheck sets ActivityStatusCode.Ok or ActivityStatusCode.Error

REFERENCE FILES:
- src/Encina.Compliance.DPIA/Diagnostics/DPIADiagnostics.cs (ActivitySource + Meter + helpers)
- src/Encina.Compliance.DPIA/Diagnostics/DPIALogMessages.cs ([LoggerMessage] source generator)
```

</details>

---

### Phase 8: Testing & Documentation

> **Goal**: Comprehensive test coverage and project documentation.

<details>
<summary><strong>Tasks</strong></summary>

#### 8a. Unit Tests (`tests/Encina.UnitTests/Compliance/ProcessorAgreements/`)

- `ProcessorRecordTests.cs` — domain record validation, IsActive logic, DPAMandatoryTerms.IsFullyCompliant
- `DPAMandatoryTermsTests.cs` — all 8 terms, MissingTerms list, IsFullyCompliant computed property
- `DPAValidationResultTests.cs` — validity logic, warning generation
- `ProcessorRecordMapperTests.cs` — domain ↔ entity round-trip
- `ProcessorAgreementAuditEntryMapperTests.cs` — audit domain ↔ entity round-trip
- `DefaultDPAValidatorTests.cs` — all validation paths (valid, expired, terminated, missing terms, sub-processor)
- `InMemoryProcessorRegistryTests.cs` — all CRUD, sub-processor resolution, status/expiration queries
- `InMemoryProcessorAuditStoreTests.cs` — record + query audit trail
- `ProcessorValidationPipelineBehaviorTests.cs` — Block/Warn/Disabled modes, attribute caching, two-level validation
- `ProcessorAgreementOptionsValidatorTests.cs` — validation rules
- `ServiceCollectionExtensionsTests.cs` — DI registration verification
- `DPAExpirationMonitorServiceTests.cs` — periodic check behavior, notification publishing

**Target**: ~60-80 unit tests

#### 8b. Guard Tests (`tests/Encina.GuardTests/Compliance/ProcessorAgreements/`)

- All public constructors and methods: null checks for non-nullable parameters
- Cover: IProcessorRegistry implementations, IDPAValidator, pipeline behavior, options, mappers

**Target**: ~30-40 guard tests

#### 8c. Contract Tests (`tests/Encina.ContractTests/Compliance/ProcessorAgreements/`)

- `IProcessorRegistryContractTests.cs` — verify registry contract (CRUD, queries)
- `IDPAValidatorContractTests.cs` — verify validator contract (valid/invalid/expired)
- `IProcessorAuditStoreContractTests.cs` — verify audit store contract

**Target**: ~10-15 contract tests

#### 8d. Property Tests (`tests/Encina.PropertyTests/Compliance/ProcessorAgreements/`)

- `ProcessorRecordPropertyTests.cs` — IsActive logic for all status/expiration combinations
- `DPAMandatoryTermsPropertyTests.cs` — IsFullyCompliant ↔ all 8 terms true, MissingTerms count consistency
- `ProcessorRecordMapperPropertyTests.cs` — domain → entity → domain round-trip preserves all fields

**Target**: ~10-15 property tests

#### 8e. Integration Tests — `.md` justification

- `ProcessorAgreements.IntegrationTests.md` — justification: this is a provider-independent package with in-memory stores only; no real database interactions to test. Database-backed store implementations will be tested when satellite provider packages are created.

#### 8f. Load Tests — `.md` justification

- `ProcessorAgreements.LoadTests.md` — justification: pipeline behavior uses in-memory store with ConcurrentDictionary; load characteristics are identical to DPIA pipeline behavior. When database-backed stores are implemented, load tests will be meaningful.

#### 8g. Benchmark Tests — `.md` justification

- `ProcessorAgreements.BenchmarkTests.md` — justification: pipeline behavior follows identical pattern to DPIA (attribute caching + store lookup). DPIA benchmarks already establish the performance baseline for this pattern. Specific benchmarks can be added when unique performance characteristics emerge.

#### 8h. Documentation

1. **XML doc comments** on all public APIs (`<summary>`, `<remarks>`, `<param>`, `<returns>`, `<example>`)
2. **CHANGELOG.md** — add entry under Unreleased section:
   ```
   ### Added
   - `Encina.Compliance.ProcessorAgreements` — GDPR Art. 28 Data Processing Agreement management with processor registry, DPA validation, sub-processor tracking, SCC compliance, and pipeline enforcement (#410)
   ```
3. **ROADMAP.md** — update v0.13.0 Security & Compliance milestone
4. **Package README.md** — `src/Encina.Compliance.ProcessorAgreements/README.md` with usage guide
5. **docs/features/processor-agreements.md** — feature documentation with configuration examples
6. **docs/INVENTORY.md** — add new package entry
7. **PublicAPI.Unshipped.txt** — verify all public symbols are tracked
8. **Build verification**: `dotnet build --configuration Release` → 0 errors, 0 warnings
9. **Test verification**: `dotnet test` → all pass

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 of Encina.Compliance.ProcessorAgreements (Issue #410).

CONTEXT:
- Phases 1-7 are fully implemented (core package with models, interfaces, stores, validator, pipeline behavior, DI, background services, observability)
- 4 active test types: Unit, Guard, Contract, Property
- 3 justified skips: Integration (no DB), Load (in-memory only), Benchmark (same pattern as DPIA)
- Documentation covers: XML docs, CHANGELOG, ROADMAP, README, feature docs, INVENTORY

TASK:
Create comprehensive test coverage and all documentation.

KEY RULES:
Unit Tests:
- Mock all dependencies
- Test each method independently
- Cover happy path + error paths + edge cases
- Verify all 8 mandatory terms individually in DPAMandatoryTermsTests
- Pipeline behavior: test all 3 enforcement modes + attribute caching + two-level validation

Guard Tests:
- Use GuardClauses.xUnit library
- All public constructors and methods with non-nullable parameters

Contract Tests:
- Abstract base test class with InMemory-derived test classes
- Verify CRUD contract, validation contract, audit contract

Property Tests:
- FsCheck generators for ProcessorRecord, DPAMandatoryTerms
- Invariants: IsFullyCompliant ↔ all terms true, IsActive logic, mapper round-trip

Documentation:
- Package README with quick start, configuration, pipeline behavior, sub-processor tracking examples
- Feature doc with Art. 28 reference, all 8 mandatory terms explained
- CHANGELOG entry under ### Added
- INVENTORY.md update

REFERENCE FILES:
- tests/Encina.UnitTests/Compliance/DPIA/ (unit test patterns for compliance)
- tests/Encina.GuardTests/Compliance/DPIA/ (guard test patterns)
- tests/Encina.ContractTests/Compliance/DPIA/ (contract test patterns)
- tests/Encina.PropertyTests/Compliance/DPIA/ (property test patterns)
- src/Encina.Compliance.DPIA/README.md (package README format)
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
| `DPIAReviewReminderService` | `Encina.Compliance.DPIA/` | Background service pattern reference |
| `DPIAHealthCheck` | `Encina.Compliance.DPIA/Health/` | Health check pattern reference |
| `GDPRErrors` | `Encina.Compliance.GDPR/` | Error factory pattern reference |

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

| Category | Count |
|----------|-------|
| Project file (.csproj) | 1 |
| Models & enums | 8 |
| Interfaces | 3 |
| Attributes | 1 |
| Error factory | 1 |
| Default implementations | 5 |
| Persistence entities & mappers | 4 |
| Pipeline behavior | 1 |
| Options & validation | 2 |
| DI registration | 1 |
| Background services | 1 |
| Health check | 1 |
| Diagnostics | 2 |
| Notifications | 6 |
| PublicAPI files | 2 |
| **Production subtotal** | **~39** |
| Unit tests | ~12 |
| Guard tests | ~3 |
| Contract tests | ~3 |
| Property tests | ~3 |
| Justification .md files | 3 |
| Documentation (README, features, etc.) | 3 |
| **Test & docs subtotal** | **~27** |
| **Total** | **~66** |

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

IMPLEMENTATION OVERVIEW:
Create a NEW package: src/Encina.Compliance.ProcessorAgreements/

Phase 1 — Core Models: 3 enums (DPAStatus, SubProcessorAuthorizationType, ProcessorAgreementEnforcementMode), 4 domain records (ProcessorRecord with DPAMandatoryTerms, DPAValidationResult, AuditEntry), 6 notifications
Phase 2 — Interfaces: IProcessorRegistry (8 methods), IDPAValidator (3 methods), IProcessorAuditStore (2 methods), [RequiresProcessor] attribute, ProcessorAgreementErrors static factory
Phase 3 — Implementations: InMemoryProcessorRegistry, InMemoryProcessorAuditStore, DefaultDPAValidator, persistence entities + mappers
Phase 4 — Pipeline: ProcessorValidationPipelineBehavior<TRequest, TResponse> with Block/Warn/Disabled modes
Phase 5 — DI: ProcessorAgreementOptions, ServiceCollectionExtensions, DPAExpirationMonitorService, HealthCheck
Phase 6 — Cross-cutting: Multi-tenancy (TenantId), audit trail, validation, transactions
Phase 7 — Observability: ActivitySource + Meter + [LoggerMessage] (EventIds 8900-8999)
Phase 8 — Testing: Unit (~70), Guard (~35), Contract (~12), Property (~12) + documentation

KEY PATTERNS (from DPIA reference):
- Pipeline: ConcurrentDictionary<Type, Attribute?> cache, Stopwatch.GetTimestamp, Activity + Counter + Histogram
- Options: sealed class, IValidateOptions<T>, BlockWithoutValidDPA alias
- DI: TryAdd*, TryAddEnumerable, conditional registration from local options instance
- Health: DefaultName const, Tags static array, scoped resolution
- Diagnostics: static ActivitySource + Meter, [LoggerMessage] source generator
- Errors: static class with factory methods, error code prefix "processor."
- Background: IHostedService with PeriodicTimer, enabled via options flag

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
- tests/Encina.UnitTests/Compliance/DPIA/ (unit test patterns)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | **Caching** | ⏭️ Deferred | Cache processor registry lookups for pipeline behavior hot path. Create issue when database-backed stores are implemented. |
| 2 | **OpenTelemetry** | ✅ Included | ActivitySource for pipeline checks + validations + background monitoring. Meter for counters + histograms. Phase 7. |
| 3 | **Structured Logging** | ✅ Included | [LoggerMessage] source generator, EventIds 8900-8999, no PII in logs. Phase 7. |
| 4 | **Health Checks** | ✅ Included | ProcessorAgreementHealthCheck: registry health, expired DPAs, incomplete terms. Phase 5. |
| 5 | **Validation** | ✅ Included | ProcessorAgreementOptionsValidator + DPA content validation against Art. 28(3). Phases 3, 5. |
| 6 | **Resilience** | ⏭️ Deferred | Retry/circuit breaker for external registry calls. N/A for in-memory stores. |
| 7 | **Distributed Locks** | ⏭️ Deferred | Concurrent DPA updates. N/A for ConcurrentDictionary; needed when database-backed. |
| 8 | **Transactions** | ✅ Included | Atomic store operations (ConcurrentDictionary is thread-safe). Phase 6. |
| 9 | **Idempotency** | ⏭️ Deferred | Duplicate processor registration protection. Currently handled by "already exists" error. |
| 10 | **Multi-Tenancy** | ✅ Included | TenantId on ProcessorRecord, context propagation in pipeline behavior. Phase 6. |
| 11 | **Module Isolation** | ⏭️ Deferred | ModuleId on ProcessorRecord (field exists), module-scoped enforcement deferred. |
| 12 | **Audit Trail** | ✅ Included | IProcessorAuditStore, audit entries for all DPA lifecycle operations. Phases 2, 3, 6. |

---

## Next Steps

1. **Review** — Review this plan and approve or request changes
2. **Publish** — Post plan summary as comment on Issue #410
3. **Implement** — Execute phases 1-8 sequentially, committing after each phase
4. **Verify** — Build + test: `dotnet build --configuration Release` (0 warnings), `dotnet test` (all pass, ≥85% coverage)
5. **Finalize** — Final commit with `Fixes #410`
