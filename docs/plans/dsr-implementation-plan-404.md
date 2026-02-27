# Implementation Plan: `Encina.Compliance.DataSubjectRights` — GDPR Rights Management (Arts. 15-22)

> **Issue**: [#404](https://github.com/dlrivada/Encina/issues/404)
> **Type**: Feature
> **Complexity**: High (10 phases, 13 database providers, ~120 files)
> **Estimated Scope**: ~4,500-6,000 lines of production code + ~3,000-4,000 lines of tests

---

## Summary

Implement comprehensive Data Subject Rights (DSR) management covering GDPR Articles 15-22. This package provides automated handling of access, rectification, erasure, restriction, portability, and objection requests with full lifecycle tracking, 30-day SLA compliance, third-party notification (Art. 19), and a `ProcessingRestrictionPipelineBehavior` that blocks requests targeting restricted subjects.

The implementation follows the same satellite-provider architecture established by `Encina.Compliance.Consent` and `Encina.Compliance.GDPR` (LawfulBasis, ProcessingActivity), delivering store implementations across all 13 database providers with dedicated observability, health checks, and auto-registration.

---

## Design Choices

<details>
<summary><strong>1. Package Placement — New <code>Encina.Compliance.DataSubjectRights</code> package</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New `Encina.Compliance.DataSubjectRights` package** | Clean separation, own pipeline behavior, own observability, independent versioning | New NuGet package, more projects to maintain |
| **B) Extend `Encina.Compliance.GDPR`** | Single package, shared config | Bloats GDPR core (~60 files already), violates SRP, DSR is 8 articles worth of domain |
| **C) Per-article packages** | Maximum granularity | Package explosion (8+ packages), excessive DI ceremony |

### Chosen Option: **A — New `Encina.Compliance.DataSubjectRights` package**

### Rationale

- DSR spans 8 GDPR articles with its own domain model, pipeline behavior, store interfaces, and observability
- Follows the established pattern: `Encina.Compliance.Consent` is a separate package for Art. 7, so DSR (Arts. 15-22) deserves its own
- Keeps `Encina.Compliance.GDPR` focused on core compliance (processing activities, lawful basis, RoPA)
- References `Encina.Compliance.GDPR` for shared types (`LawfulBasis`, `GDPRErrors` helpers, `[ProcessesPersonalData]`)
- Satellite providers add DSR stores in their existing `DataSubjectRights/` subfolder (same pattern as `Consent/`, `LawfulBasis/`, `ProcessingActivity/`)

</details>

<details>
<summary><strong>2. DSR Request Lifecycle Model — Entity-based tracking with state machine</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Simple status enum on a tracking entity** | Easy to implement, queryable, fits 13-provider pattern | Limited state transition validation |
| **B) Full saga/state machine** | Formal state transitions, compensation logic | Over-engineered for request tracking, conflicts with existing Saga pattern |
| **C) Event-sourced lifecycle** | Complete audit trail, time-travel | Requires event store, incompatible with CRUD providers |

### Chosen Option: **A — Entity-based tracking with status enum**

### Rationale

- A `DSRRequest` domain record tracks each request with: `SubjectId`, `RightType`, `Status`, `ReceivedAtUtc`, `DeadlineAtUtc`, `CompletedAtUtc`, `ExtensionReason`
- `DSRRequestStatus` enum: `Received`, `IdentityVerified`, `InProgress`, `Completed`, `Rejected`, `Extended`, `Expired`
- `IDSRRequestStore` provides CRUD + query-by-status + deadline queries
- 30-day deadline calculated at receipt: `DeadlineAtUtc = ReceivedAtUtc.AddDays(30)` (Art. 12(3))
- Extension support: up to 2 additional months for complex requests (Art. 12(3) second paragraph)
- Audit trail via separate `DSRAuditEntry` records (same pattern as `ConsentAuditEntry`)
- Status transitions validated in the domain layer, not in the store

</details>

<details>
<summary><strong>3. <code>[PersonalData]</code> Attribute Design — Property-level with metadata</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Property-level attribute with Category, Erasable, Portable, LegalRetention** | Precise field marking, enables automated locator, matches issue spec | Requires reflection scanning |
| **B) Class-level attribute (like `[ProcessesPersonalData]`)** | Simple, already exists | Too coarse — can't distinguish erasable vs retained fields |
| **C) Fluent API registration only** | No reflection, explicit | Verbose, easy to forget fields |

### Chosen Option: **A — Property-level attribute with metadata**

### Rationale

- Matches the issue specification exactly: `[PersonalData(Category = "Identity", Erasable = true, Portable = true)]`
- Property-level targeting (`AttributeTargets.Property`) complements the existing class-level `[ProcessesPersonalData]`
- `Category` enables grouping (Identity, Contact, Financial, Health, etc.)
- `Erasable` flag respects Art. 17(3) exemptions (legal retention)
- `Portable` flag determines Art. 20 export eligibility
- `LegalRetention` flag explicitly marks fields that MUST be retained despite erasure requests
- Scanned at startup by auto-registration to build a `PersonalDataMap` — no runtime reflection in hot paths

</details>

<details>
<summary><strong>4. Erasure Strategy — Pluggable via <code>IDataErasureStrategy</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Pluggable strategy pattern** | Users choose hard delete, soft delete, or crypto-shredding | Strategy interface, slightly more DI |
| **B) Hard delete only** | Simple, GDPR-compliant | No flexibility for legal retention or encrypted stores |
| **C) Soft delete with scheduled hard delete** | Reversible, grace period | Doesn't truly erase, may violate Art. 17 |

### Chosen Option: **A — Pluggable strategy pattern**

### Rationale

- `IDataErasureStrategy` interface with `EraseFieldAsync(PersonalDataLocation, CancellationToken)`
- Three built-in strategies:
  - `HardDeleteErasureStrategy` — Sets fields to null/default (default)
  - `SoftDeleteErasureStrategy` — Marks as deleted with cleanup scheduler
  - `CryptoShredErasureStrategy` — Destroys encryption keys (for systems using per-field encryption)
- `DefaultDataErasureExecutor` orchestrates: calls `IPersonalDataLocator` → filters by `Erasable` → applies `IDataErasureStrategy` per field
- Respects `LegalRetention = true` fields unconditionally (Art. 17(3) exemption)
- Returns `ErasureResult` with counts: `FieldsErased`, `FieldsRetained`, `FieldsFailed`, `RetentionReasons`

</details>

<details>
<summary><strong>5. Data Locator Pattern — Composite with auto-discovery</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Composite locator with attribute-based auto-discovery** | Automatic + extensible, scans `[PersonalData]` at startup | Needs reflection at startup |
| **B) Manual registration only** | Full control, no magic | Easy to forget fields, no automated audit |
| **C) EF Core metadata-based** | Leverages existing model | Ties to EF Core, doesn't work for Dapper/ADO |

### Chosen Option: **A — Composite locator with auto-discovery**

### Rationale

- `IPersonalDataLocator` returns `IReadOnlyList<PersonalDataLocation>` for a subject
- `CompositePersonalDataLocator` aggregates results from multiple locators (one per bounded context)
- `AttributeBasedPersonalDataLocator` scans types with `[PersonalData]` properties at startup (built from auto-registration)
- Users register custom locators for external systems: `services.AddPersonalDataLocator<LegacyCRMLocator>()`
- `PersonalDataLocation` record: `EntityType`, `EntityId`, `FieldName`, `Category`, `IsErasable`, `IsPortable`, `CurrentValue`
- No runtime reflection — the auto-registration builds a `PersonalDataMap` (dictionary of type → field metadata) at startup

</details>

<details>
<summary><strong>6. Art. 19 Third-Party Notification — Domain event via Encina notification pipeline</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Domain notification events** | Uses existing Encina notification pipeline, decoupled | Requires notification handlers for each recipient |
| **B) Explicit `IThirdPartyNotifier` interface** | Direct, testable | Couples DSR to notification logic |
| **C) Outbox-based notification** | Reliable delivery | Requires outbox setup, heavy for simple notifications |

### Chosen Option: **A — Domain notification events**

### Rationale

- Publish `DataErasedNotification`, `DataRectifiedNotification`, `ProcessingRestrictedNotification` via Encina's `INotificationPublisher`
- Each notification carries: `SubjectId`, `AffectedFields`, `Timestamp`, `DSRRequestId`
- Third-party recipients implement `INotificationHandler<DataErasedNotification>` in their bounded context
- Integrates with existing Outbox pattern for reliable delivery if configured
- Audit trail: notifications are logged as `DSRAuditEntry` records
- `DataSubjectRightsOptions.PublishNotifications` flag controls emission (default: `true`)

</details>

<details>
<summary><strong>7. Pipeline Behavior — <code>ProcessingRestrictionPipelineBehavior</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Dedicated pipeline behavior with IDSRRequestStore lookup** | Real-time restriction check, follows LawfulBasis pattern | Store query on every request for restricted subjects |
| **B) In-memory cache of restricted subjects** | Fast lookups, no DB per request | Stale data, cache invalidation complexity |
| **C) Attribute-only (compile-time)** | Zero runtime cost | Can't restrict dynamically per subject |

### Chosen Option: **A — Dedicated pipeline behavior with store lookup**

### Rationale

- `ProcessingRestrictionPipelineBehavior<TRequest, TResponse>` checks if the request's subject has an active restriction (Art. 18)
- Only activates for requests marked with `[ProcessesPersonalData]` or `[PersonalData]` — skips all others
- Subject ID extracted via `IDataSubjectIdExtractor` (reuses or extends `ILawfulBasisSubjectIdExtractor` pattern)
- Uses `IDSRRequestStore.HasActiveRestrictionAsync(subjectId)` — lightweight query (single-row EXISTS check)
- Three enforcement modes: `Block` (reject with error), `Warn` (log + continue), `Disabled` (skip)
- Restriction scope: only restricts processing, NOT storage (Art. 18(2) — "stored but not processed")
- Uses `ConcurrentDictionary` cache for attribute detection (same pattern as LawfulBasis behavior)
- OpenTelemetry: dedicated counters for restriction checks (passed/blocked)

</details>

<details>
<summary><strong>8. Export Format Implementation — Strategy per format with streaming</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Per-format exporter strategy** | Clean separation, extensible | Multiple classes |
| **B) Single exporter with format switch** | Simple, one class | Violates OCP, hard to add formats |
| **C) Template-based serialization** | Flexible | Over-engineered |

### Chosen Option: **A — Per-format exporter strategy**

### Rationale

- `IDataPortabilityExporter` has a single `ExportAsync(subjectId, format, ct)` method
- `DefaultDataPortabilityExporter` delegates to `IExportFormatWriter` implementations:
  - `JsonExportFormatWriter` — System.Text.Json, structured output
  - `CsvExportFormatWriter` — RFC 4180 compliant
  - `XmlExportFormatWriter` — Standard XML with schema
- Each writer receives `IReadOnlyList<PersonalDataLocation>` and produces `ExportedData` (byte array + content type)
- `ExportedData` record: `byte[] Content`, `string ContentType`, `string FileName`, `ExportFormat Format`
- Follows the same pattern as `IRoPAExporter` (JSON/CSV exporters in GDPR core)

</details>

---

## Implementation Phases

### Phase 1: Core Models, Enums & Domain Records

> **Goal**: Establish the foundational types that all other phases depend on.

<details>
<summary><strong>Tasks</strong></summary>

#### New project: `src/Encina.Compliance.DataSubjectRights/`

1. **Create project file** `Encina.Compliance.DataSubjectRights.csproj`
   - Target: `net10.0`
   - Dependencies: `Encina.Compliance.GDPR`, `LanguageExt.Core`, `Microsoft.Extensions.Logging.Abstractions`, `Microsoft.Extensions.Options`
   - Enable nullable, implicit usings, XML doc

2. **Enums** (`Model/` folder):
   - `DataSubjectRight` — `Access`, `Rectification`, `Erasure`, `Restriction`, `Portability`, `Objection`, `AutomatedDecisionMaking`, `Notification`
   - `ErasureReason` — 6 values mapping to Art. 17(1)(a-f): `NoLongerNecessary`, `ConsentWithdrawn`, `ObjectionToProcessing`, `UnlawfulProcessing`, `LegalObligation`, `ChildData`
   - `ExportFormat` — `JSON`, `CSV`, `XML`
   - `DSRRequestStatus` — `Received`, `IdentityVerified`, `InProgress`, `Completed`, `Rejected`, `Extended`, `Expired`
   - `ErasureExemption` — Art. 17(3): `FreedomOfExpression`, `LegalObligation`, `PublicHealth`, `Archiving`, `LegalClaims`
   - `PersonalDataCategory` — `Identity`, `Contact`, `Financial`, `Health`, `Biometric`, `Genetic`, `Location`, `Online`, `Employment`, `Education`, `Other`

3. **Domain records** (`Model/` folder):
   - `DSRRequest` — sealed record: `Id (string)`, `SubjectId`, `RightType (DataSubjectRight)`, `Status`, `ReceivedAtUtc`, `DeadlineAtUtc`, `CompletedAtUtc?`, `ExtensionReason?`, `ExtendedDeadlineAtUtc?`, `RejectionReason?`, `RequestDetails?`, `VerifiedAtUtc?`, `ProcessedByUserId?`
   - `PersonalDataLocation` — sealed record: `EntityType (Type)`, `EntityId (string)`, `FieldName`, `Category (PersonalDataCategory)`, `IsErasable`, `IsPortable`, `HasLegalRetention`, `CurrentValue (object?)`
   - `PersonalDataField` — sealed record: `PropertyName`, `Category`, `IsErasable`, `IsPortable`, `HasLegalRetention` (metadata from attribute scanning)
   - `ErasureResult` — sealed record: `FieldsErased (int)`, `FieldsRetained (int)`, `FieldsFailed (int)`, `RetentionReasons (IReadOnlyList<RetentionDetail>)`, `Exemptions (IReadOnlyList<ErasureExemption>)`
   - `RetentionDetail` — sealed record: `FieldName`, `EntityType`, `Reason (string)`
   - `ExportedData` — sealed record: `Content (byte[])`, `ContentType (string)`, `FileName (string)`, `Format (ExportFormat)`, `FieldCount (int)`
   - `AccessResponse` — sealed record: `SubjectId`, `Data (IReadOnlyList<PersonalDataLocation>)`, `ProcessingActivities (IReadOnlyList<ProcessingActivity>)`, `GeneratedAtUtc`
   - `PortabilityResponse` — sealed record: `SubjectId`, `ExportedData`, `GeneratedAtUtc`
   - `ErasureScope` — sealed record: `Categories (IReadOnlyList<PersonalDataCategory>?)`, `SpecificFields (IReadOnlyList<string>?)`, `Reason (ErasureReason)`, `ExemptionsToApply (IReadOnlyList<ErasureExemption>?)`

4. **Request/Response types** (`Requests/` folder):
   - `AccessRequest` — sealed record: `SubjectId`, `IncludeProcessingActivities (bool)`
   - `RectificationRequest` — sealed record: `SubjectId`, `FieldName`, `NewValue (object)`, `EntityType (Type?)`, `EntityId (string?)`
   - `ErasureRequest` — sealed record: `SubjectId`, `Reason (ErasureReason)`, `Scope (ErasureScope?)`
   - `RestrictionRequest` — sealed record: `SubjectId`, `Reason (string)`, `Scope (IReadOnlyList<PersonalDataCategory>?)`
   - `PortabilityRequest` — sealed record: `SubjectId`, `Format (ExportFormat)`, `Categories (IReadOnlyList<PersonalDataCategory>?)`
   - `ObjectionRequest` — sealed record: `SubjectId`, `ProcessingPurpose (string)`, `Reason (string)`

5. **DSR audit** (`Model/` folder):
   - `DSRAuditEntry` — sealed record: `Id (string)`, `DSRRequestId (string)`, `Action (string)`, `Detail (string?)`, `PerformedByUserId (string?)`, `OccurredAtUtc`

6. **Notification records** (`Notifications/` folder):
   - `DataErasedNotification` — sealed record implementing `INotification`: `SubjectId`, `AffectedFields`, `DSRRequestId`, `OccurredAtUtc`
   - `DataRectifiedNotification` — sealed record implementing `INotification`: `SubjectId`, `FieldName`, `DSRRequestId`, `OccurredAtUtc`
   - `ProcessingRestrictedNotification` — sealed record implementing `INotification`: `SubjectId`, `DSRRequestId`, `OccurredAtUtc`
   - `RestrictionLiftedNotification` — sealed record implementing `INotification`: `SubjectId`, `DSRRequestId`, `OccurredAtUtc`

7. **`PublicAPI.Unshipped.txt`** — Add all public types

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of Encina.Compliance.DataSubjectRights (Issue #404).

CONTEXT:
- Encina is a .NET 10 / C# 14 library using Railway Oriented Programming (Either<EncinaError, T>)
- This is a NEW project: src/Encina.Compliance.DataSubjectRights/
- Reference existing patterns in src/Encina.Compliance.GDPR/Model/ and src/Encina.Compliance.Consent/
- All domain models are sealed records with XML documentation
- Use LanguageExt for Option<T> and Either<L, R>
- Timestamps use DateTimeOffset with AtUtc suffix convention

TASK:
Create the project file and all model types listed in Phase 1 Tasks.

KEY RULES:
- Target net10.0, enable nullable, enable implicit usings
- All types are sealed records (not classes)
- All public types need XML documentation with <summary>, <remarks>, and GDPR article references
- Enums need [Description] or XML doc mapping to GDPR article subsections
- DSRRequest.DeadlineAtUtc = ReceivedAtUtc.AddDays(30) — enforced in factory/constructor
- ErasureResult must track both erased AND retained fields
- PersonalDataLocation.CurrentValue is object? (nullable, boxing acceptable for portability data)
- ExportedData carries byte[] Content, not Stream (simpler, works for all providers)
- Notification records implement INotification from Encina core
- Add PublicAPI.Unshipped.txt with all public symbols

REFERENCE FILES:
- src/Encina.Compliance.GDPR/Model/ProcessingActivity.cs (sealed record pattern)
- src/Encina.Compliance.GDPR/Model/LawfulBasis.cs (enum pattern)
- src/Encina.Compliance.Consent/ConsentRecord.cs (domain record with timestamps)
```

</details>

---

### Phase 2: Core Interfaces & Attributes

> **Goal**: Define the public API surface — interfaces, attributes, and error codes.

<details>
<summary><strong>Tasks</strong></summary>

1. **Attributes** (`Attributes/` folder):
   - `PersonalDataAttribute` — `[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]`
     - Properties: `Category (PersonalDataCategory)`, `Erasable (bool, default true)`, `Portable (bool, default true)`, `LegalRetention (bool, default false)`, `RetentionReason (string?)`
   - `RestrictProcessingAttribute` — `[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]`
     - Marker attribute for requests that should be subject to processing restriction checks
     - Properties: `SubjectIdProperty (string?)` — name of the property containing the subject ID

2. **Core interfaces** (`Abstractions/` folder):
   - `IDataSubjectRightsHandler` — 6 methods as specified in issue, all returning `ValueTask<Either<EncinaError, T>>`
   - `IPersonalDataLocator` — `LocateAllDataAsync(string subjectId, CancellationToken)` → `Either<EncinaError, IReadOnlyList<PersonalDataLocation>>`
   - `IDataErasureExecutor` — `EraseAsync(string subjectId, ErasureScope scope, CancellationToken)` → `Either<EncinaError, ErasureResult>`
   - `IDataErasureStrategy` — `EraseFieldAsync(PersonalDataLocation location, CancellationToken)` → `Either<EncinaError, Unit>`
   - `IDataPortabilityExporter` — `ExportAsync(string subjectId, ExportFormat format, CancellationToken)` → `Either<EncinaError, PortabilityResponse>`
   - `IExportFormatWriter` — `WriteAsync(IReadOnlyList<PersonalDataLocation> data, CancellationToken)` → `Either<EncinaError, ExportedData>`; property `ExportFormat SupportedFormat { get; }`
   - `IDSRRequestStore` — CRUD + queries:
     - `CreateAsync(DSRRequest, CancellationToken)` → `Either<EncinaError, Unit>`
     - `GetByIdAsync(string id, CancellationToken)` → `Either<EncinaError, Option<DSRRequest>>`
     - `GetBySubjectIdAsync(string subjectId, CancellationToken)` → `Either<EncinaError, IReadOnlyList<DSRRequest>>`
     - `UpdateStatusAsync(string id, DSRRequestStatus newStatus, string? reason, CancellationToken)` → `Either<EncinaError, Unit>`
     - `GetPendingRequestsAsync(CancellationToken)` → `Either<EncinaError, IReadOnlyList<DSRRequest>>`
     - `GetOverdueRequestsAsync(CancellationToken)` → `Either<EncinaError, IReadOnlyList<DSRRequest>>`
     - `HasActiveRestrictionAsync(string subjectId, CancellationToken)` → `Either<EncinaError, bool>`
     - `GetAllAsync(CancellationToken)` → `Either<EncinaError, IReadOnlyList<DSRRequest>>`
   - `IDSRAuditStore` — audit trail:
     - `RecordAsync(DSRAuditEntry, CancellationToken)` → `Either<EncinaError, Unit>`
     - `GetAuditTrailAsync(string dsrRequestId, CancellationToken)` → `Either<EncinaError, IReadOnlyList<DSRAuditEntry>>`
   - `IDataSubjectIdExtractor` — `ExtractSubjectId(TRequest request, IRequestContext context)` → `string?` (follows `ILawfulBasisSubjectIdExtractor` pattern)

3. **Error codes** (`DSRErrors.cs`):
   - Error code prefix: `dsr.`
   - Codes: `dsr.request_not_found`, `dsr.request_already_completed`, `dsr.identity_not_verified`, `dsr.restriction_active`, `dsr.erasure_failed`, `dsr.export_failed`, `dsr.format_not_supported`, `dsr.deadline_expired`, `dsr.exemption_applies`, `dsr.subject_not_found`, `dsr.locator_failed`, `dsr.store_error`, `dsr.rectification_failed`, `dsr.objection_rejected`, `dsr.invalid_request`
   - Follow `GDPRErrors.cs` pattern: `public static class DSRErrors` with factory methods

4. **`PublicAPI.Unshipped.txt`** — Update with all new public symbols

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of Encina.Compliance.DataSubjectRights (Issue #404).

CONTEXT:
- Phase 1 models are already implemented in src/Encina.Compliance.DataSubjectRights/Model/
- Encina uses Railway Oriented Programming: all store/handler methods return ValueTask<Either<EncinaError, T>>
- LanguageExt provides Option<T>, Either<L, R>, Unit
- Error codes follow the pattern in src/Encina.Compliance.GDPR/GDPRErrors.cs

TASK:
Create all interfaces, attributes, and error codes listed in Phase 2 Tasks.

KEY RULES:
- [PersonalData] targets properties (AttributeTargets.Property), not classes
- [RestrictProcessing] targets classes (AttributeTargets.Class) — marker for pipeline behavior
- All interface methods take CancellationToken as last parameter with default value
- IDSRRequestStore.HasActiveRestrictionAsync must be lightweight (used in pipeline behavior hot path)
- IDataErasureStrategy has a single EraseFieldAsync method — strategies are simple and composable
- IExportFormatWriter declares its SupportedFormat via property (strategy selection)
- DSRErrors follows the exact GDPRErrors pattern: static class, factory methods, EncinaErrors.Create(...)
- All interfaces need comprehensive XML documentation with GDPR article references

REFERENCE FILES:
- src/Encina.Compliance.GDPR/GDPRErrors.cs (error factory pattern)
- src/Encina.Compliance.GDPR/Abstractions/ILawfulBasisRegistry.cs (interface pattern)
- src/Encina.Compliance.Consent/Abstractions/IConsentStore.cs (7-method store interface)
- src/Encina.Compliance.GDPR/Attributes/ProcessesPersonalDataAttribute.cs (attribute pattern)
- src/Encina.Compliance.GDPR/Abstractions/ILawfulBasisSubjectIdExtractor.cs (subject ID extractor)
```

</details>

---

### Phase 3: Default Implementations & In-Memory Stores

> **Goal**: Provide working implementations for development/testing without database dependencies.

<details>
<summary><strong>Tasks</strong></summary>

1. **In-memory stores** (`InMemory/` folder):
   - `InMemoryDSRRequestStore : IDSRRequestStore` — `ConcurrentDictionary<string, DSRRequest>`, `TimeProvider` for time queries
   - `InMemoryDSRAuditStore : IDSRAuditStore` — `ConcurrentDictionary<string, List<DSRAuditEntry>>`
   - Both follow pattern from `InMemoryLawfulBasisRegistry`, `InMemoryConsentStore`

2. **Personal data locator** (`Locators/` folder):
   - `AttributeBasedPersonalDataLocator : IPersonalDataLocator` — uses `PersonalDataMap` (built from auto-registration) to identify fields, but requires user-provided data retrieval callbacks
   - `CompositePersonalDataLocator : IPersonalDataLocator` — aggregates results from `IEnumerable<IPersonalDataLocator>`

3. **Erasure strategies** (`Erasure/` folder):
   - `HardDeleteErasureStrategy : IDataErasureStrategy` — sets field to null/default
   - `DefaultDataErasureExecutor : IDataErasureExecutor` — orchestrates: locate → filter (Erasable, !LegalRetention) → apply strategy → build result

4. **Export format writers** (`Export/` folder):
   - `JsonExportFormatWriter : IExportFormatWriter` — `System.Text.Json` with `JsonSerializerOptions { WriteIndented = true }`
   - `CsvExportFormatWriter : IExportFormatWriter` — RFC 4180 compliant, headers from field names
   - `XmlExportFormatWriter : IExportFormatWriter` — `XDocument` based, root element `<PersonalData>`

5. **Default handler** (`DefaultDataSubjectRightsHandler.cs`):
   - Implements `IDataSubjectRightsHandler`
   - Orchestrates the flow for each right:
     - Access: `IPersonalDataLocator.LocateAllDataAsync` + optionally `IProcessingActivityRegistry.GetAllActivitiesAsync`
     - Rectification: locate specific field → update → publish `DataRectifiedNotification`
     - Erasure: `IDataErasureExecutor.EraseAsync` → update DSR request status → publish `DataErasedNotification`
     - Restriction: create restriction record in `IDSRRequestStore` → publish `ProcessingRestrictedNotification`
     - Portability: `IDataPortabilityExporter.ExportAsync`
     - Objection: record objection → update processing activity → publish notification
   - Records `DSRAuditEntry` for each operation
   - Updates `IDSRRequestStore` status at each lifecycle stage
   - Dependencies: `IDSRRequestStore`, `IDSRAuditStore`, `IPersonalDataLocator`, `IDataErasureExecutor`, `IDataPortabilityExporter`, `IOptions<DataSubjectRightsOptions>`, `ILogger`, `TimeProvider`

6. **Default data portability exporter** (`Export/DefaultDataPortabilityExporter.cs`):
   - Implements `IDataPortabilityExporter`
   - Resolves appropriate `IExportFormatWriter` from DI based on requested format
   - Calls `IPersonalDataLocator.LocateAllDataAsync` → filters by `IsPortable` → delegates to writer

7. **Subject ID extractor** (`DefaultDataSubjectIdExtractor.cs`):
   - Implements `IDataSubjectIdExtractor`
   - Uses reflection (cached) to find property named `SubjectId`, `UserId`, or property specified in `[RestrictProcessing(SubjectIdProperty = "...")]`
   - Fallback to `IRequestContext.UserId`

8. **Personal data map** (`PersonalDataMap.cs`):
   - `internal sealed class PersonalDataMap` — `IReadOnlyDictionary<Type, IReadOnlyList<PersonalDataField>>`
   - Built from `[PersonalData]` attribute scanning at startup
   - Immutable after construction (thread-safe)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of Encina.Compliance.DataSubjectRights (Issue #404).

CONTEXT:
- Phase 1 (models) and Phase 2 (interfaces, attributes, errors) are already implemented
- Encina uses ROP: all methods return ValueTask<Either<EncinaError, T>>
- In-memory stores use ConcurrentDictionary for thread safety
- TimeProvider is injected for testable time-dependent logic

TASK:
Create all default implementations listed in Phase 3 Tasks.

KEY RULES:
- InMemoryDSRRequestStore uses ConcurrentDictionary<string, DSRRequest> with ImmutableInterlocked for thread safety
- HasActiveRestrictionAsync must be O(n) scan at worst — filter by SubjectId + Status == Restriction + not completed
- DefaultDataErasureExecutor respects LegalRetention = true unconditionally — never erases retained fields
- Export writers produce byte[] not Stream (matches ExportedData.Content)
- DefaultDataSubjectRightsHandler is the main orchestrator — each method:
  1. Validates request
  2. Records audit entry (started)
  3. Executes the operation
  4. Updates DSR request status
  5. Records audit entry (completed/failed)
  6. Publishes Art. 19 notification if applicable
- CompositePersonalDataLocator aggregates all registered IPersonalDataLocator implementations
- PersonalDataMap is built once at startup, immutable, and thread-safe
- All constructors validate parameters with ArgumentNullException.ThrowIfNull

REFERENCE FILES:
- src/Encina.Compliance.GDPR/LawfulBasis/InMemoryLawfulBasisRegistry.cs (in-memory store pattern)
- src/Encina.Compliance.Consent/InMemoryConsentStore.cs (ConcurrentDictionary pattern)
- src/Encina.Compliance.GDPR/Export/JsonRoPAExporter.cs (JSON export pattern)
- src/Encina.Compliance.GDPR/Export/CsvRoPAExporter.cs (CSV export pattern)
```

</details>

---

### Phase 4: Pipeline Behavior — `ProcessingRestrictionPipelineBehavior`

> **Goal**: Implement the pipeline behavior that blocks requests targeting restricted data subjects (Art. 18).

<details>
<summary><strong>Tasks</strong></summary>

1. **`ProcessingRestrictionPipelineBehavior<TRequest, TResponse>`** (`ProcessingRestrictionPipelineBehavior.cs`):
   - Implements `IPipelineBehavior<TRequest, TResponse>` where `TRequest : IRequest<TResponse>`
   - Static per-generic-type attribute caching (same pattern as `LawfulBasisValidationPipelineBehavior`):
     - `private static readonly RestrictionAttributeInfo? CachedAttributeInfo = ResolveAttributeInfo()`
     - Checks for `[RestrictProcessing]`, `[ProcessesPersonalData]`, or `[ProcessingActivity]` on `TRequest`
   - `Handle` method flow:
     1. If `EnforcementMode == Disabled` → `nextStep()`
     2. If `CachedAttributeInfo is null` → `nextStep()` (not a personal data request)
     3. Extract subject ID via `IDataSubjectIdExtractor`
     4. If no subject ID → `nextStep()` (can't check restriction)
     5. Call `IDSRRequestStore.HasActiveRestrictionAsync(subjectId)`
     6. If restricted + Block mode → return `Left(DSRErrors.RestrictionActive(...))`
     7. If restricted + Warn mode → log warning + `nextStep()`
     8. If not restricted → `nextStep()`
   - Observability: traces via `DataSubjectRightsDiagnostics`, counters, structured logging

2. **Enforcement options** (in `DataSubjectRightsOptions.cs` — created in Phase 5 but define the enum here):
   - `DSREnforcementMode` enum: `Block`, `Warn`, `Disabled`

3. **`RestrictionAttributeInfo`** (private sealed record inside behavior):
   - `SubjectIdProperty (string?)`, `Source (string)`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of Encina.Compliance.DataSubjectRights (Issue #404).

CONTEXT:
- Phases 1-3 are already implemented (models, interfaces, in-memory stores, default handler)
- This pipeline behavior follows the EXACT same pattern as LawfulBasisValidationPipelineBehavior
- It intercepts requests to check if the data subject has an active processing restriction (Art. 18)

TASK:
Create ProcessingRestrictionPipelineBehavior<TRequest, TResponse>.

KEY RULES:
- Use static per-generic-type attribute caching: private static readonly RestrictionAttributeInfo? CachedAttributeInfo
- ResolveAttributeInfo() checks for [RestrictProcessing], [ProcessesPersonalData], or [ProcessingActivity] on typeof(TRequest)
- Subject ID extraction: first from attribute.SubjectIdProperty via reflection, fallback to IDataSubjectIdExtractor
- HasActiveRestrictionAsync is a lightweight EXISTS-style query — don't load full DSR records
- Three enforcement modes: Block returns Left(DSRErrors.RestrictionActive(...)), Warn logs + continues, Disabled skips
- All constructor parameters validated with ArgumentNullException.ThrowIfNull
- Dependencies: IDSRRequestStore, IDataSubjectIdExtractor, IOptions<DataSubjectRightsOptions>, ILogger

REFERENCE FILES:
- src/Encina.Compliance.GDPR/LawfulBasisValidationPipelineBehavior.cs (EXACT pattern to follow)
- src/Encina.Compliance.Consent/ConsentRequiredPipelineBehavior.cs (alternative pipeline pattern)
```

</details>

---

### Phase 5: Configuration, DI & Auto-Registration

> **Goal**: Wire everything together with options, service registration, auto-registration, and health check.

<details>
<summary><strong>Tasks</strong></summary>

1. **Options** (`DataSubjectRightsOptions.cs`):
   - `DSREnforcementMode RestrictionEnforcementMode { get; set; }` — default: `Block`
   - `bool AddHealthCheck { get; set; }` — default: `false`
   - `bool AutoRegisterFromAttributes { get; set; }` — default: `true`
   - `bool PublishNotifications { get; set; }` — default: `true`
   - `int DefaultDeadlineDays { get; set; }` — default: `30` (Art. 12(3))
   - `int MaxExtensionDays { get; set; }` — default: `60` (2 additional months)
   - `bool TrackAuditTrail { get; set; }` — default: `true`
   - `List<Assembly> AssembliesToScan { get; }` — default: `[]`
   - `HashSet<PersonalDataCategory> DefaultErasableCategories { get; }` — categories erasable by default
   - `HashSet<PersonalDataCategory> DefaultPortableCategories { get; }` — categories portable by default

2. **Options validator** (`DataSubjectRightsOptionsValidator.cs`):
   - `IValidateOptions<DataSubjectRightsOptions>`
   - Validates: `DefaultDeadlineDays > 0`, `MaxExtensionDays >= 0`, `MaxExtensionDays <= 60`

3. **Service collection extensions** (`ServiceCollectionExtensions.cs`):
   - `AddEncinaDataSubjectRights(this IServiceCollection services, Action<DataSubjectRightsOptions>? configure = null)`
   - Registers:
     - `DataSubjectRightsOptions` via `services.Configure()`
     - `DataSubjectRightsOptionsValidator` via `TryAddSingleton<IValidateOptions<>>`
     - `TimeProvider.System` via `TryAddSingleton` (standalone guard)
     - `IDSRRequestStore` → `InMemoryDSRRequestStore` via `TryAddSingleton`
     - `IDSRAuditStore` → `InMemoryDSRAuditStore` via `TryAddSingleton`
     - `IDataSubjectRightsHandler` → `DefaultDataSubjectRightsHandler` via `TryAddScoped`
     - `IDataErasureExecutor` → `DefaultDataErasureExecutor` via `TryAddScoped`
     - `IDataErasureStrategy` → `HardDeleteErasureStrategy` via `TryAddSingleton`
     - `IDataPortabilityExporter` → `DefaultDataPortabilityExporter` via `TryAddScoped`
     - `IDataSubjectIdExtractor` → `DefaultDataSubjectIdExtractor` via `TryAddSingleton`
     - `IExportFormatWriter` (JSON, CSV, XML) — register all three
     - `ProcessingRestrictionPipelineBehavior<,>` via `TryAddTransient(typeof(IPipelineBehavior<,>))`
   - Conditional: health check if `AddHealthCheck == true`
   - Conditional: auto-registration if `AutoRegisterFromAttributes == true`

4. **Auto-registration** (`DSRAutoRegistrationDescriptor.cs` + `DSRAutoRegistrationHostedService.cs`):
   - `internal sealed record DSRAutoRegistrationDescriptor(IReadOnlyList<Assembly> Assemblies)`
   - `internal sealed class DSRAutoRegistrationHostedService : IHostedService`
   - `StartAsync`: scans assemblies for types with properties marked `[PersonalData]`, builds `PersonalDataMap`, registers as singleton
   - Logs via dedicated event IDs

5. **Health check** (`Health/DataSubjectRightsHealthCheck.cs`):
   - `public sealed class DataSubjectRightsHealthCheck : IHealthCheck`
   - `const string DefaultName = "encina-dsr"`
   - Tags: `["encina", "gdpr", "dsr", "compliance", "ready"]`
   - Checks: `IDSRRequestStore` resolvable, `IPersonalDataLocator` resolvable, overdue requests count, `IDataErasureExecutor` resolvable
   - Warns (Degraded): overdue requests > 0, missing optional services
   - Uses scoped resolution pattern (same as `ConsentHealthCheck`)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of Encina.Compliance.DataSubjectRights (Issue #404).

CONTEXT:
- Phases 1-4 are implemented (models, interfaces, in-memory stores, handler, pipeline behavior)
- DI registration follows the TryAdd pattern — satellite providers pre-register concrete stores
- Auto-registration uses IHostedService with a descriptor record
- Health checks use IServiceProvider.CreateScope() for scoped service resolution

TASK:
Create options, DI registration, auto-registration, and health check.

KEY RULES:
- Options pattern: sealed class with defaults, IValidateOptions<T> for validation
- ServiceCollectionExtensions uses TryAdd* for all registrations (satellite overrides)
- Instantiate a local optionsInstance to read flags before DI is fully built (for conditional registration)
- Auto-registration scans for [PersonalData] on properties across assemblies → builds PersonalDataMap
- HostedService pattern: StartAsync does work, StopAsync returns Task.CompletedTask
- Health check returns Unhealthy if core services missing, Degraded if overdue requests exist, Healthy otherwise
- Pipeline behavior registered with TryAddTransient(typeof(IPipelineBehavior<,>), typeof(ProcessingRestrictionPipelineBehavior<,>))

REFERENCE FILES:
- src/Encina.Compliance.Consent/ConsentOptions.cs (options pattern)
- src/Encina.Compliance.Consent/ServiceCollectionExtensions.cs (DI registration pattern)
- src/Encina.Compliance.Consent/ConsentAutoRegistrationHostedService.cs (auto-registration)
- src/Encina.Compliance.Consent/Health/ConsentHealthCheck.cs (health check pattern)
- src/Encina.Compliance.GDPR/ServiceCollectionExtensions.cs (AddEncinaGDPR/AddEncinaLawfulBasis)
```

</details>

---

### Phase 6: Persistence Entity, Mapper & SQL Scripts

> **Goal**: Create the persistence layer shared infrastructure used by all 13 database providers.

<details>
<summary><strong>Tasks</strong></summary>

1. **Persistence entity** (`DSRRequestEntity.cs` in `Encina.Compliance.DataSubjectRights`):
   - `public sealed class DSRRequestEntity` with string-based properties:
     - `Id`, `SubjectId`, `RightTypeValue (int)`, `StatusValue (int)`, `ReceivedAtUtc (DateTimeOffset)`, `DeadlineAtUtc (DateTimeOffset)`, `CompletedAtUtc (DateTimeOffset?)`, `ExtensionReason (string?)`, `ExtendedDeadlineAtUtc (DateTimeOffset?)`, `RejectionReason (string?)`, `RequestDetails (string?)`, `VerifiedAtUtc (DateTimeOffset?)`, `ProcessedByUserId (string?)`

2. **DSR audit entity** (`DSRAuditEntryEntity.cs`):
   - `public sealed class DSRAuditEntryEntity`: `Id`, `DSRRequestId`, `Action`, `Detail (string?)`, `PerformedByUserId (string?)`, `OccurredAtUtc (DateTimeOffset)`

3. **Mapper** (`DSRRequestMapper.cs`):
   - `public static class DSRRequestMapper`
   - `ToEntity(DSRRequest) → DSRRequestEntity` (domain → persistence)
   - `ToDomain(DSRRequestEntity) → DSRRequest?` (persistence → domain, null if invalid)
   - Follow `LawfulBasisRegistrationMapper` pattern

4. **DSR audit mapper** (`DSRAuditEntryMapper.cs`):
   - `ToEntity(DSRAuditEntry) → DSRAuditEntryEntity`
   - `ToDomain(DSRAuditEntryEntity) → DSRAuditEntry`

5. **SQL scripts** (`Scripts/` folder in each satellite provider package):
   - Table: `DSRRequests` — columns matching entity properties
   - Table: `DSRAuditEntries` — columns matching audit entity
   - Provider-specific DDL:
     - **SQLite**: TEXT for dates (ISO 8601), INTEGER for enums
     - **SQL Server**: DATETIMEOFFSET for dates, INT for enums, NVARCHAR for strings
     - **PostgreSQL**: TIMESTAMPTZ for dates, INTEGER for enums, TEXT for strings
     - **MySQL**: DATETIME(6) for dates, INT for enums, VARCHAR for strings

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of Encina.Compliance.DataSubjectRights (Issue #404).

CONTEXT:
- Phases 1-5 are implemented (full core package with models, interfaces, default impls, DI)
- Persistence entities are plain classes (not records) for ORM compatibility
- Mappers convert between domain records and persistence entities
- SQL scripts are per-provider due to DDL differences

TASK:
Create persistence entities, mappers, and SQL DDL scripts for all 4 database engines.

KEY RULES:
- Entity classes use public get/set properties (mutable for ORMs)
- Mapper.ToEntity generates a new GUID Id (Guid.NewGuid().ToString("D"))
- Mapper.ToDomain returns null if entity state is invalid (defensive)
- SQL scripts: CREATE TABLE IF NOT EXISTS (SQLite), IF NOT EXISTS pattern for others
- SQLite: TEXT for DateTime (ISO 8601 format), INTEGER for enums and booleans
- SQL Server: NVARCHAR(450) for string keys, DATETIMEOFFSET(7), INT for enums
- PostgreSQL: TEXT for strings, TIMESTAMPTZ, INTEGER for enums
- MySQL: VARCHAR(450) for string keys, DATETIME(6), INT for enums
- All tables have PRIMARY KEY on Id, INDEX on SubjectId, INDEX on Status

REFERENCE FILES:
- src/Encina.Compliance.GDPR/LawfulBasis/LawfulBasisRegistrationEntity.cs (entity pattern)
- src/Encina.Compliance.GDPR/LawfulBasis/LawfulBasisRegistrationMapper.cs (mapper pattern)
- src/Encina.Compliance.Consent/ConsentRecordEntity.cs (consent entity)
```

</details>

---

### Phase 7: Multi-Provider Persistence — 13 Database Providers

> **Goal**: Implement `IDSRRequestStore` and `IDSRAuditStore` across all 13 providers with satellite DI registration.

<details>
<summary><strong>Tasks</strong></summary>

#### 7a. ADO.NET Providers (×4)

For each ADO provider (`Encina.ADO.Sqlite`, `Encina.ADO.SqlServer`, `Encina.ADO.PostgreSQL`, `Encina.ADO.MySQL`):

1. `DataSubjectRights/DSRRequestStoreADO.cs` — implements `IDSRRequestStore`
   - Constructor: `IDbConnection connection`, `string tableName = "DSRRequests"`, `TimeProvider? timeProvider = null`
   - Uses `IDbCommand` / `IDataReader` pattern (same as `ConsentStoreADO`)
   - `HasActiveRestrictionAsync`: `SELECT COUNT(1) FROM DSRRequests WHERE SubjectId = @SubjectId AND RightTypeValue = @RestrictionType AND StatusValue NOT IN (@Completed, @Rejected, @Expired)`
   - `GetOverdueRequestsAsync`: `SELECT * FROM DSRRequests WHERE DeadlineAtUtc < @NowUtc AND StatusValue NOT IN (@Completed, @Rejected, @Expired)`
   - Provider-specific SQL: TOP vs LIMIT, parameter syntax, date handling

2. `DataSubjectRights/DSRAuditStoreADO.cs` — implements `IDSRAuditStore`
   - Constructor: `IDbConnection connection`, `string tableName = "DSRAuditEntries"`
   - `GetAuditTrailAsync`: `SELECT ... WHERE DSRRequestId = @DSRRequestId ORDER BY OccurredAtUtc DESC`

3. **DI registration** in each provider's `ServiceCollectionExtensions.cs`:
   - Add `AddEncinaDSR{Provider}(services, connectionString)` method
   - Registers `IDSRRequestStore` → `DSRRequestStoreADO` as singleton
   - Registers `IDSRAuditStore` → `DSRAuditStoreADO` as singleton
   - Pattern: `services.TryAddSingleton<IDSRRequestStore>(new DSRRequestStoreADO(new SqliteConnection(connectionString)))`

#### 7b. Dapper Providers (×4)

For each Dapper provider (`Encina.Dapper.Sqlite`, `Encina.Dapper.SqlServer`, `Encina.Dapper.PostgreSQL`, `Encina.Dapper.MySQL`):

1. `DataSubjectRights/DSRRequestStoreDapper.cs` — implements `IDSRRequestStore`
   - Uses `Dapper.ExecuteAsync`, `Dapper.QueryAsync`, `Dapper.ExecuteScalarAsync`
   - Anonymous parameter objects
   - Provider-specific SQL (same differences as ADO)

2. `DataSubjectRights/DSRAuditStoreDapper.cs` — implements `IDSRAuditStore`

3. **DI registration** — `AddEncinaDSRDapper{Provider}(services, connectionString)`

#### 7c. EF Core Provider

In `Encina.EntityFrameworkCore`:

1. `DataSubjectRights/DSRRequestStoreEF.cs` — implements `IDSRRequestStore`
   - Uses `DbContext.Set<DSRRequestEntity>()` with LINQ
   - Entity mapping via `DSRRequestEntityConfiguration : IEntityTypeConfiguration<DSRRequestEntity>`

2. `DataSubjectRights/DSRAuditStoreEF.cs` — implements `IDSRAuditStore`

3. `DataSubjectRights/DSRRequestEntity.cs` (EF-specific entity if different from shared entity)

4. `DataSubjectRights/DSRRequestEntityConfiguration.cs` — Fluent API configuration

5. `DataSubjectRights/DSRAuditEntryEntityConfiguration.cs`

6. `DataSubjectRights/DSRModelBuilderExtensions.cs` — `modelBuilder.ApplyDSRConfiguration()`

7. **DI registration** in `ServiceCollectionExtensions.cs` — integrate with `AddEncinaEntityFrameworkCore` options

#### 7d. MongoDB Provider

In `Encina.MongoDB`:

1. `DataSubjectRights/DSRRequestStoreMongoDB.cs` — implements `IDSRRequestStore`
   - Uses `IMongoCollection<DSRRequestDocument>`
   - Collection name from `EncinaMongoDbOptions.Collections.DSRRequests`

2. `DataSubjectRights/DSRAuditStoreMongoDB.cs` — implements `IDSRAuditStore`

3. `DataSubjectRights/DSRRequestDocument.cs` — MongoDB document class with `FromDomain` / `ToDomain` methods

4. `DataSubjectRights/DSRAuditEntryDocument.cs` — MongoDB audit document

5. **DI registration** — integrate with `AddEncinaMongoDB` options

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of Encina.Compliance.DataSubjectRights (Issue #404).

CONTEXT:
- Phases 1-6 are implemented (core package + entities + mappers + SQL scripts)
- ALL 13 database providers must be implemented: ADO.NET (Sqlite, SqlServer, PostgreSQL, MySQL), Dapper (same 4), EF Core (4 under one package), MongoDB
- Each provider creates a DataSubjectRights/ subfolder in its source project

TASK:
Implement IDSRRequestStore and IDSRAuditStore for all 13 providers, plus DI registration.

KEY RULES:
Provider-specific SQL differences:
| Provider    | Parameters | LIMIT        | DateTime             | Boolean |
|-------------|-----------|--------------|----------------------|---------|
| SQLite      | @param    | LIMIT @n     | TEXT (ISO 8601 "O")  | 0/1     |
| SQL Server  | @param    | TOP (@n)     | DATETIMEOFFSET       | bit     |
| PostgreSQL  | @param    | LIMIT @n     | TIMESTAMPTZ          | true/false |
| MySQL       | @param    | LIMIT @n     | DATETIME(6)          | 0/1     |

- ADO.NET: IDbCommand/IDataReader, async via cast to provider-specific types
- Dapper: ExecuteAsync/QueryAsync with anonymous parameter objects
- EF Core: DbContext.Set<T>() with LINQ, IEntityTypeConfiguration for mapping
- MongoDB: IMongoCollection<T>, ReplaceOneAsync with IsUpsert, BulkWriteAsync
- SQLite DateTime: ALWAYS use "O" format for serialization, DateTimeStyles.RoundtripKind for parsing, NEVER use datetime('now')
- All stores validate table names via SqlIdentifierValidator.ValidateTableName()
- HasActiveRestrictionAsync must be efficient: single COUNT/EXISTS query, no full table scan
- DI: satellite methods called BEFORE AddEncinaDataSubjectRights (TryAdd pattern)
- TimeProvider injection for testable time-dependent queries

REFERENCE FILES:
- src/Encina.ADO.Sqlite/Consent/ConsentStoreADO.cs (ADO pattern)
- src/Encina.Dapper.Sqlite/Consent/ConsentStoreDapper.cs (Dapper pattern)
- src/Encina.EntityFrameworkCore/Consent/ConsentStoreEF.cs (EF Core pattern)
- src/Encina.MongoDB/Consent/ConsentStoreMongoDB.cs (MongoDB pattern)
- src/Encina.ADO.Sqlite/ServiceCollectionExtensions.cs (satellite DI registration)
```

</details>

---

### Phase 8: Observability — Diagnostics, Metrics & Logging

> **Goal**: Add OpenTelemetry traces, counters, and structured logging for DSR operations.

<details>
<summary><strong>Tasks</strong></summary>

1. **`DataSubjectRightsDiagnostics.cs`** (`Diagnostics/` folder):
   - `internal static class DataSubjectRightsDiagnostics`
   - `ActivitySource`: `"Encina.Compliance.DataSubjectRights"`, version `"1.0"`
   - `Meter`: Create new or reuse `GDPRDiagnostics.Meter` (prefer new: `"Encina.Compliance.DataSubjectRights"`)
   - **Counters** (Counter<long>):
     - `dsr_requests_total` — tagged by `right_type`, `outcome` (received, completed, rejected, expired)
     - `dsr_restriction_checks_total` — tagged by `outcome` (passed, blocked)
     - `dsr_erasure_operations_total` — tagged by `outcome` (success, partial, failed)
     - `dsr_export_operations_total` — tagged by `format`, `outcome`
   - **Tag constants**:
     - `TagRightType = "right_type"`, `TagOutcome = "outcome"`, `TagSubjectId = "subject.id"`, `TagFormat = "format"`, `TagStatus = "status"`, `TagFieldCount = "field_count"`
   - **Activity helpers**:
     - `StartDSRRequest(DataSubjectRight rightType, string subjectId)` → `Activity?`
     - `CompleteDSRRequest(Activity?, bool success, string? reason)` → void
     - `StartRestrictionCheck(Type requestType)` → `Activity?`
     - `CompleteRestrictionCheck(Activity?, bool blocked)` → void

2. **`DSRLogMessages.cs`** (`Diagnostics/` folder):
   - `internal static partial class DSRLogMessages` using `[LoggerMessage]` source generator
   - **Event ID range: 8300-8399** (new range for DSR package):
     - 8300: DSR request received
     - 8301: DSR request identity verified
     - 8302: DSR request processing started
     - 8303: DSR request completed
     - 8304: DSR request rejected
     - 8305: DSR request extended
     - 8306: DSR request expired (overdue)
     - 8310: Access request handled
     - 8311: Rectification executed
     - 8312: Erasure started
     - 8313: Erasure completed
     - 8314: Erasure partially completed (some fields retained)
     - 8315: Erasure field retained (legal retention)
     - 8316: Restriction applied
     - 8317: Restriction lifted
     - 8318: Portability export started
     - 8319: Portability export completed
     - 8320: Objection recorded
     - 8330: Restriction check started
     - 8331: Restriction check passed (no active restriction)
     - 8332: Restriction check blocked (active restriction found)
     - 8333: Restriction check skipped (no GDPR attributes)
     - 8334: Restriction enforcement warning (warn mode)
     - 8340: Art. 19 notification published
     - 8341: Art. 19 notification failed
     - 8350: Auto-registration completed
     - 8351: Auto-registration skipped
     - 8360: Health check completed
     - 8370: DSR store error

3. **Update existing `GDPRDiagnostics.cs`** — add DSR package to activity source list documentation (cross-reference only)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 of Encina.Compliance.DataSubjectRights (Issue #404).

CONTEXT:
- Phases 1-7 are implemented (full core + 13 providers)
- Observability follows OpenTelemetry patterns: ActivitySource for traces, Meter for metrics, ILogger for structured logs
- Event IDs in 8300-8399 range (new range, avoids collision with GDPR 8100-8220 and Consent 8200-8250)

TASK:
Create DataSubjectRightsDiagnostics, DSRLogMessages, and integrate observability into existing code.

KEY RULES:
- ActivitySource name: "Encina.Compliance.DataSubjectRights" (matches package name)
- Meter: new Meter("Encina.Compliance.DataSubjectRights", "1.0") — separate from GDPR core
- All counters use tag-based dimensions (right_type, outcome, format) for flexible dashboards
- DSRLogMessages uses [LoggerMessage] source generator (partial class, partial methods)
- Log messages follow structured logging: "DSR {Action}. SubjectId={SubjectId}, RightType={RightType}"
- Activity helpers check Source.HasListeners() before creating activities (avoid allocations)
- SetTag uses string constants from tag fields (TagRightType, TagOutcome, etc.)
- CompleteDSRRequest sets ActivityStatusCode.Ok or ActivityStatusCode.Error

REFERENCE FILES:
- src/Encina.Compliance.GDPR/Diagnostics/LawfulBasisDiagnostics.cs (ActivitySource + counters)
- src/Encina.Compliance.GDPR/Diagnostics/LawfulBasisLogMessages.cs ([LoggerMessage] source generator)
- src/Encina.Compliance.GDPR/Diagnostics/GDPRDiagnostics.cs (shared meter pattern)
- src/Encina.Compliance.GDPR/Diagnostics/GDPRLogMessages.cs (LoggerMessage.Define pattern)
```

</details>

---

### Phase 9: Testing — 7 Test Types

> **Goal**: Comprehensive test coverage across all test categories.

<details>
<summary><strong>Tasks</strong></summary>

#### 9a. Unit Tests (`tests/Encina.UnitTests/Compliance/DataSubjectRights/`)

- `DSRRequestTests.cs` — domain record validation, deadline calculation, status transitions
- `PersonalDataAttributeTests.cs` — attribute property defaults, metadata extraction
- `ErasureResultTests.cs` — retained vs erased counting, exemption tracking
- `DSRRequestMapperTests.cs` — domain ↔ entity round-trip
- `DSRAuditEntryMapperTests.cs` — audit domain ↔ entity round-trip
- `DefaultDataSubjectRightsHandlerTests.cs` — all 6 right handlers with mocked dependencies
- `DefaultDataErasureExecutorTests.cs` — erasure with LegalRetention skip, strategy delegation
- `HardDeleteErasureStrategyTests.cs` — field nullification
- `ProcessingRestrictionPipelineBehaviorTests.cs` — Block/Warn/Disabled modes, attribute caching
- `JsonExportFormatWriterTests.cs` — JSON output format validation
- `CsvExportFormatWriterTests.cs` — CSV RFC 4180 compliance
- `XmlExportFormatWriterTests.cs` — XML structure validation
- `DefaultDataPortabilityExporterTests.cs` — format routing, Portable filter
- `CompositePersonalDataLocatorTests.cs` — aggregation from multiple locators
- `InMemoryDSRRequestStoreTests.cs` — all CRUD operations, HasActiveRestriction, overdue queries
- `InMemoryDSRAuditStoreTests.cs` — record + query audit trail
- `PersonalDataMapTests.cs` — attribute scanning, immutability
- `DataSubjectRightsOptionsValidatorTests.cs` — validation rules
- `ServiceCollectionExtensionsTests.cs` — DI registration verification

**Target**: ~80-100 unit tests

#### 9b. Guard Tests (`tests/Encina.GuardTests/Compliance/DataSubjectRights/`)

- All public constructors and methods: null checks for non-nullable parameters
- Cover: all interfaces implementations, options, mappers, attributes, pipeline behavior
- Use `GuardClauses.xUnit` library

**Target**: ~40-60 guard tests

#### 9c. Contract Tests (`tests/Encina.ContractTests/Compliance/DataSubjectRights/`)

- `IDSRRequestStoreContractTests.cs` — verify all 13 store implementations follow the same contract
- `IDSRAuditStoreContractTests.cs` — verify audit store contract
- `IDataErasureStrategyContractTests.cs` — strategy interface contract
- `IExportFormatWriterContractTests.cs` — writer interface contract

**Target**: ~15-25 contract tests

#### 9d. Property Tests (`tests/Encina.PropertyTests/Compliance/DataSubjectRights/`)

- `DSRRequestPropertyTests.cs` — deadline always 30 days after receipt, status enum round-trip
- `DSRRequestMapperPropertyTests.cs` — domain → entity → domain round-trip preserves all fields
- `ErasureResultPropertyTests.cs` — FieldsErased + FieldsRetained + FieldsFailed = total fields
- `ExportFormatWriterPropertyTests.cs` — export then parse yields same data

**Target**: ~15-20 property tests

#### 9e. Integration Tests (`tests/Encina.IntegrationTests/Compliance/DataSubjectRights/`)

For ALL 13 providers:

- `DSRRequestStore{Provider}IntegrationTests.cs` — CRUD, HasActiveRestriction, overdue queries against real DB
- `DSRAuditStore{Provider}IntegrationTests.cs` — audit CRUD against real DB
- Each uses `[Collection("{Provider}")]` fixtures (existing collections)
- `InitializeAsync` creates schema + clears data
- Tests: Create → GetById, Create → GetBySubjectId, UpdateStatus, GetPending, GetOverdue, HasActiveRestriction

**Target**: ~100-130 integration tests (10 tests × 13 providers)

#### 9f. Load Tests (`tests/Encina.LoadTests/Compliance/DataSubjectRights/`)

- `DSRRequestStoreLoadTests.md` — justification document (store is thin DB wrapper, load is on DB)
- `ProcessingRestrictionBehaviorLoadTests.cs` — concurrent restriction checks under load (100 concurrent requests with mixed restricted/unrestricted subjects)

**Target**: 1 load test class + 1 justification

#### 9g. Benchmark Tests (`tests/Encina.BenchmarkTests/Compliance/DataSubjectRights/`)

- `RestrictionCheckBenchmarks.cs` — pipeline behavior overhead per request (attribute caching, store lookup)
- `ExportFormatBenchmarks.cs` — JSON vs CSV vs XML export performance for varying data sizes
- `PersonalDataMapBenchmarks.md` — justification (one-time startup cost, not hot path)

**Target**: 2 benchmark classes + 1 justification

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
You are implementing Phase 9 of Encina.Compliance.DataSubjectRights (Issue #404).

CONTEXT:
- Phases 1-8 are fully implemented (core + 13 providers + observability)
- 7 test types must be implemented: Unit, Guard, Contract, Property, Integration, Load, Benchmark
- Integration tests use shared [Collection] fixtures — NEVER create per-class fixtures
- Tests follow AAA pattern, descriptive names, single responsibility

TASK:
Create comprehensive test coverage across all 7 test types.

KEY RULES:
Unit Tests:
- Mock all dependencies (Moq/NSubstitute)
- Test each method independently
- Cover happy path + error paths + edge cases
- Fast execution (<1ms per test)

Guard Tests:
- Use GuardClauses.xUnit library
- Test all public constructors and methods with non-nullable parameters
- Verify ArgumentNullException for null inputs

Contract Tests:
- Verify all 13 IDSRRequestStore implementations follow identical behavior
- Use abstract base class with provider-specific derived classes

Property Tests:
- FsCheck generators for domain records
- Verify invariants: deadline = receipt + 30 days, mapper round-trip, etc.

Integration Tests:
- [Collection("ADO-Sqlite")] etc. — reuse existing fixtures
- ClearAllDataAsync in InitializeAsync
- Create schema if not exists
- Test real SQL against real databases
- SQLite: NEVER dispose shared connection from fixture

Load Tests:
- ProcessingRestrictionBehavior under concurrent access
- Use Task.WhenAll with 100+ concurrent requests

Benchmark Tests:
- BenchmarkSwitcher (NOT BenchmarkRunner)
- Materialize IQueryable results
- Results to artifacts/performance/

REFERENCE FILES:
- tests/Encina.UnitTests/Compliance/GDPR/ (unit test patterns)
- tests/Encina.GuardTests/Compliance/GDPR/ (guard test patterns)
- tests/Encina.IntegrationTests/ — look for Collection fixtures and existing patterns
- tests/Encina.PropertyTests/Compliance/GDPR/ (FsCheck patterns)
```

</details>

---

### Phase 10: Documentation & Finalization

> **Goal**: Update all project documentation, verify build, and finalize.

<details>
<summary><strong>Tasks</strong></summary>

1. **INVENTORY.md** — Update issue #404 entry as IMPLEMENTADO with:
   - Package details
   - Provider count
   - Test count and coverage
   - Key interfaces

2. **CHANGELOG.md** — Add entry under Unreleased:
   - `### Added`
   - `- Encina.Compliance.DataSubjectRights — GDPR Data Subject Rights management (Articles 15-22) with IDataSubjectRightsHandler, IPersonalDataLocator, IDataErasureExecutor, IDataPortabilityExporter, ProcessingRestrictionPipelineBehavior, [PersonalData] attribute, and IDSRRequestStore across all 13 database providers (Fixes #404)`

3. **`PublicAPI.Unshipped.txt`** — Final review, ensure all public types listed

4. **XML documentation review** — Verify all public APIs have `<summary>`, `<remarks>`, `<param>`, `<returns>`, `<example>` where appropriate

5. **Build verification**:
   - `dotnet build Encina.slnx --configuration Release` — 0 errors, 0 warnings
   - `dotnet test` — all tests pass

6. **Coverage check** — Verify ≥85% line coverage for the new package

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 10</strong></summary>

```
You are implementing Phase 10 of Encina.Compliance.DataSubjectRights (Issue #404).

CONTEXT:
- Phases 1-9 are fully implemented and tested
- Documentation and finalization remaining

TASK:
Update INVENTORY.md, CHANGELOG.md, verify build, and finalize.

KEY RULES:
- INVENTORY.md: mark #404 as ✅ IMPLEMENTADO with comprehensive description
- CHANGELOG.md: add under ### Added in Unreleased section
- Build must produce 0 errors and 0 warnings
- All tests must pass
- PublicAPI.Unshipped.txt must be complete and accurate
- Commit message: "feat: implement Encina.Compliance.DataSubjectRights — GDPR Rights Management (Arts. 15-22) with 13 database providers (Fixes #404)"
```

</details>

---

## Research

### GDPR Article References

| Article | Right | Key Requirements |
|---------|-------|------------------|
| Art. 12 | Transparency | 30-day deadline, extension up to 2 months, free of charge |
| Art. 15 | Access | Copy of personal data, processing purposes, recipients, retention period |
| Art. 16 | Rectification | Correct inaccurate data without undue delay |
| Art. 17 | Erasure | Delete when no longer necessary, consent withdrawn, objection, etc. |
| Art. 17(3) | Erasure exemptions | Freedom of expression, legal obligation, public health, archiving, legal claims |
| Art. 18 | Restriction | Mark data as restricted, storage-only (no processing) |
| Art. 19 | Notification | Notify recipients of rectification/erasure/restriction |
| Art. 20 | Portability | Machine-readable format (JSON/CSV/XML), structured, commonly used |
| Art. 21 | Objection | Right to object to processing based on Art. 6(1)(e/f) |
| Art. 22 | Automated decisions | Right not to be subject to solely automated decisions with legal effects |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in DSR |
|-----------|----------|-------------|
| `IProcessingActivityRegistry` | `Encina.Compliance.GDPR` | Access response includes processing activities |
| `[ProcessesPersonalData]` | `Encina.Compliance.GDPR` | Pipeline behavior attribute detection |
| `[ProcessingActivity]` | `Encina.Compliance.GDPR` | Pipeline behavior attribute detection |
| `IConsentStore` | `Encina.Compliance.Consent` | Verify consent status for erasure (Art. 17(1)(b)) |
| `EncinaErrors.Create()` | `Encina` core | Error factory pattern |
| `IPipelineBehavior<,>` | `Encina` core | Pipeline behavior registration |
| `INotification` / `INotificationPublisher` | `Encina` core | Art. 19 notifications |
| `TimeProvider` | .NET 10 BCL | Testable time-dependent logic |
| `IRoPAExporter` | `Encina.Compliance.GDPR` | Pattern for JSON/CSV exporters |
| Satellite provider structure | All 13 providers | Subfolder + DI registration pattern |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.GDPR` | 8100-8220 | Core, LawfulBasis, ProcessingActivity |
| `Encina.Compliance.Consent` | 8200-8250 | Consent lifecycle, audit, events |
| **`Encina.Compliance.DataSubjectRights`** | **8300-8399** | **New — DSR lifecycle, restriction, erasure, export** |

### File Count Estimate

| Category | Files | Notes |
|----------|-------|-------|
| Core package (Phases 1-5, 8) | ~35-40 | Models, interfaces, impls, diagnostics, DI |
| Persistence (Phase 6) | ~5 | Entities, mappers, shared |
| ADO.NET ×4 (Phase 7a) | ~12 | 3 files × 4 providers |
| Dapper ×4 (Phase 7b) | ~12 | 3 files × 4 providers |
| EF Core (Phase 7c) | ~8 | Stores, entities, configs, extensions |
| MongoDB (Phase 7d) | ~6 | Stores, documents |
| Tests (Phase 9) | ~40-50 | Across 7 test types |
| Documentation (Phase 10) | ~3 | INVENTORY, CHANGELOG, PublicAPI |
| **Total** | **~120-135** | |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing Encina.Compliance.DataSubjectRights for Issue #404 — GDPR Data Subject Rights Management (Articles 15-22).

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library for CQRS + Event Sourcing
- Pre-1.0: no backward compatibility needed, best solution always
- Railway Oriented Programming: Either<EncinaError, T> everywhere
- 13 database providers: ADO.NET (Sqlite, SqlServer, PostgreSQL, MySQL), Dapper (same 4), EF Core (same 4), MongoDB
- Satellite provider pattern: feature subfolder in each provider package
- TryAdd DI pattern: satellite providers register before core package

IMPLEMENTATION OVERVIEW:
New package: src/Encina.Compliance.DataSubjectRights/
References: Encina.Compliance.GDPR (for shared types)

Phase 1: Core models, enums (DSRRequest, ErasureReason, ExportFormat, DSRRequestStatus, PersonalDataLocation, etc.)
Phase 2: Interfaces (IDataSubjectRightsHandler, IPersonalDataLocator, IDataErasureExecutor, IDSRRequestStore, etc.) + [PersonalData] attribute + DSRErrors
Phase 3: Default implementations (InMemory stores, DefaultHandler, exporters, erasure strategy)
Phase 4: ProcessingRestrictionPipelineBehavior (Art. 18 restriction check in pipeline)
Phase 5: Options, DI registration, auto-registration, health check
Phase 6: Persistence entities, mappers, SQL scripts
Phase 7: 13 provider implementations (ADO ×4, Dapper ×4, EF Core, MongoDB) + satellite DI
Phase 8: Observability (ActivitySource, Meter, [LoggerMessage] event IDs 8300-8399)
Phase 9: Testing (7 types: Unit ~100, Guard ~50, Contract ~20, Property ~20, Integration ~130, Load, Benchmark)
Phase 10: Documentation (INVENTORY.md, CHANGELOG.md, PublicAPI.Unshipped.txt)

KEY PATTERNS:
- All stores: ValueTask<Either<EncinaError, T>>
- Store naming: DSRRequestStoreADO, DSRRequestStoreDapper, DSRRequestStoreEF, DSRRequestStoreMongoDB
- SQLite: TEXT dates (ISO 8601 "O"), never datetime('now'), NEVER dispose shared connection in tests
- Satellite DI: AddEncinaDSR{Provider}(services, connectionString) → called before AddEncinaDataSubjectRights
- Pipeline behavior: static per-generic-type attribute caching, 3 enforcement modes
- Health check: Unhealthy/Degraded/Healthy, scoped resolution, const DefaultName
- Integration tests: [Collection("Provider-DB")] shared fixtures, ClearAllDataAsync
- All public APIs: XML documentation with GDPR article references

REFERENCE FILES:
- Consent package: src/Encina.Compliance.Consent/ (closest architectural reference)
- GDPR package: src/Encina.Compliance.GDPR/ (shared types, LawfulBasis pipeline)
- Provider patterns: src/Encina.ADO.Sqlite/Consent/, src/Encina.Dapper.Sqlite/Consent/, src/Encina.EntityFrameworkCore/Consent/, src/Encina.MongoDB/Consent/
```

</details>

---

## Next Steps

1. **Review and approve this plan**
2. Publish as comment on Issue #404
3. Begin Phase 1 implementation in a new session
4. Each phase should be a self-contained commit
5. Final commit references `Fixes #404`
