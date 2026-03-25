# Implementation Plan: `Encina.Compliance.DPIA` — Data Protection Impact Assessment Automation (Arts. 35-36)

> **Issue**: [#409](https://github.com/dlrivada/Encina/issues/409)
> **Type**: Feature
> **Complexity**: High (12 phases, 13 database providers + InMemory, ~140 files)
> **Estimated Scope**: ~5,500-7,000 lines of production code + ~4,000-5,500 lines of tests

---

## Summary

Implement automated Data Protection Impact Assessment (DPIA) covering GDPR Articles 35-36. This package provides risk scoring with a pluggable assessment engine, template management for processing types, DPO consultation tracking, prior consultation detection (Art. 36), assessment lifecycle storage, and a `DPIARequiredPipelineBehavior` that blocks or warns when requests marked with `[RequiresDPIA]` lack a current assessment.

The implementation follows the same satellite-provider architecture established by `Encina.Compliance.BreachNotification`, `Encina.Compliance.Retention`, and other compliance satellites, delivering store implementations across all 13 database providers plus an InMemory provider for testing and lightweight deployments.

**Provider category**: Database (13 providers) + InMemory — `IDPIAStore` and `IDPIAAuditStore` require persistence across ADO.NET (×4), Dapper (×4), EF Core (×4), MongoDB (×1), and InMemory (×1, built-in default for testing/development).

---

## Design Choices

<details>
<summary><strong>1. Package Placement — New <code>Encina.Compliance.DPIA</code> package</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New `Encina.Compliance.DPIA` package** | Clean separation, own pipeline, own observability, independent versioning, matches issue spec | New NuGet package |
| **B) Extend `Encina.Compliance.GDPR`** | Single package, shares GDPR options | Bloats GDPR core (~60+ files), DPIA is a distinct assessment lifecycle |
| **C) Add to `Encina.Compliance.DataSubjectRights`** | Shares data subject concern | DPIA is about processing assessment, not individual rights — different domain |

### Chosen Option: **A — New `Encina.Compliance.DPIA` package**

### Rationale

- DPIA covers 2 GDPR articles (35-36) with its own domain model (assessments, risk items, mitigations, templates), pipeline behavior, and observability
- Follows the established 1-package-per-compliance-domain pattern: GDPR core (Art. 30), Consent (Art. 7), DSR (Arts. 15-22), Retention (Art. 5(1)(e)), DataResidency (Art. 44-49), Anonymization, BreachNotification (Arts. 33-34)
- References `Encina.Compliance.GDPR` for shared types (`IDataProtectionOfficer`, `GDPROptions.DataProtectionOfficer`) — soft dependency
- Optional integration with other compliance packages (BreachNotification for risk assessment, Anonymization for mitigation suggestions)

</details>

<details>
<summary><strong>2. Persistence Strategy — Full 13-provider persistence + InMemory</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) In-memory default + pluggable `IDPIAStore`** | Simple, fast for most apps, user-overridable, no 13-provider overhead | No built-in persistent storage, users must implement their own |
| **B) Full 13-provider persistence + InMemory** | Consistent with BreachNotification/Retention, audit trail persisted by default, multi-instance safe, InMemory for testing | More files (~40 stores + ~100 integration tests) |
| **C) File-based storage (JSON)** | Easy to version-control assessments | File I/O issues, not suitable for multi-instance deployments |

### Chosen Option: **B — Full 13-provider persistence + InMemory**

### Rationale

- **Consistency**: All compliance satellites (BreachNotification, Retention, DataResidency, DSR) provide full 13-provider persistence — DPIA should follow the same pattern
- **Audit requirements**: DPIA assessments have compliance audit obligations (Art. 35(7)); persistent storage ensures assessments and DPO consultations are durably recorded and queryable
- **Multi-instance safety**: Applications running multiple instances need shared persistent storage for assessment state — InMemory only works for single-instance or testing
- **InMemory as testing/dev provider**: `InMemoryDPIAStore` ships as the default in the core package, enabling quick setup, unit testing, and development without database infrastructure
- **TryAdd override pattern**: Satellite providers register their implementations via `TryAdd`, so `InMemoryDPIAStore` is replaced when any database provider is registered — zero configuration conflict
- The `IDPIAStore` and `IDPIAAuditStore` interfaces are implemented by all 13 database providers (ADO.NET ×4, Dapper ×4, EF Core ×4, MongoDB ×1) plus InMemory

</details>

<details>
<summary><strong>3. Risk Scoring Model — Multi-dimensional risk matrix with pluggable criteria</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Multi-dimensional risk matrix with `IRiskCriterion` strategy** | Extensible, users add custom criteria, built-in criteria for GDPR Art. 35(3), testable | Requires criterion registration |
| **B) Single numeric score** | Simple, easy to compare | Loses dimensional information (what type of risk?), hard to justify to DPO |
| **C) External risk tool integration only** | Leverages existing tools | No built-in assessment, depends on external systems |

### Chosen Option: **A — Multi-dimensional risk matrix with `IRiskCriterion` strategy**

### Rationale

- `IRiskCriterion` interface evaluates a single risk dimension: `EvaluateAsync(DPIAContext) → RiskItem`
- `DefaultDPIAAssessmentEngine` iterates all registered criteria, aggregates `RiskItem` scores into `DPIAResult`
- Built-in criteria cover Art. 35(3) mandatory triggers:
  - `SystematicProfilingCriterion` — Art. 35(3)(a)
  - `SpecialCategoryDataCriterion` — Art. 35(3)(b)
  - `SystematicMonitoringCriterion` — Art. 35(3)(c)
  - `AutomatedDecisionMakingCriterion` — AI/ML, behavioral tracking
  - `LargeScaleProcessingCriterion` — Volume-based risk
  - `VulnerableSubjectsCriterion` — Children, employees, patients
- `OverallRisk` computed from max individual risk (conservative approach, matches ICO guidance)
- `RequiresPriorConsultation` triggered when `OverallRisk >= VeryHigh` and mitigations insufficient (Art. 36)
- Custom criteria: `options.AddRiskCriterion<MyCustomCriterion>()`

</details>

<details>
<summary><strong>4. Assessment Lifecycle — Entity-based tracking with review cycle</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Entity-based `DPIAAssessment` with status and review tracking** | Queryable, tracks DPO approval, supports review cycles, audit trail | Slightly more complex than simple result storage |
| **B) Simple result storage (write-once)** | Minimal, just stores last assessment | No lifecycle tracking, no DPO approval flow, no review reminders |
| **C) Event-sourced assessment history** | Full immutable history | Over-engineered for assessment frequency (~yearly reviews) |

### Chosen Option: **A — Entity-based tracking with review cycle**

### Rationale

- `DPIAAssessment` tracks the full lifecycle: `Created`, `InReview`, `Approved`, `Rejected`, `RequiresRevision`, `Expired`
- `DPOConsultation` record tracks DPO involvement: who reviewed, when, decision, conditions
- Review cycle: `NextReviewAtUtc = ApprovedAtUtc.AddMonths(options.ReviewIntervalMonths)` (configurable, default 12)
- `DPIAAssessmentStatus` determines if processing can proceed (only `Approved` allows it in `Block` mode)
- Audit entries track all status transitions and DPO decisions
- `IDPIAStore` provides CRUD + query-by-request-type + expiration queries

</details>

<details>
<summary><strong>5. Template System — Pluggable template provider with built-in GDPR templates</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Pluggable `IDPIATemplateProvider` with built-in templates** | Extensible, users customize templates, built-in covers Art. 35 requirements | Template registration |
| **B) Hard-coded assessment flow** | Simple, works for basic needs | Not customizable per processing type or jurisdiction |
| **C) External template files (JSON/YAML)** | Easy to edit without code | File management, deployment concerns |

### Chosen Option: **A — Pluggable `IDPIATemplateProvider` with built-in templates**

### Rationale

- `IDPIATemplateProvider` returns `DPIATemplate` for a given processing type
- `DPIATemplate` contains: `Name`, `Description`, `RequiredSections`, `RiskCategories`, `SuggestedMitigations`
- `DefaultDPIATemplateProvider` includes built-in templates for common processing types:
  - `"profiling"` — Systematic evaluation of personal aspects (Art. 35(3)(a))
  - `"special-category"` — Large-scale processing of special category data (Art. 35(3)(b))
  - `"public-monitoring"` — Systematic monitoring of public areas (Art. 35(3)(c))
  - `"ai-ml"` — AI/ML systems, automated decision-making
  - `"biometric"` — Biometric data processing
  - `"health-data"` — Large-scale health data processing
  - `"general"` — Default template for any processing type
- Users add custom templates: `services.AddSingleton<IDPIATemplateProvider, MyCustomTemplateProvider>()`
- Templates support `IReadOnlyList<DPIASection>` for structured assessment with required fields

</details>

<details>
<summary><strong>6. Pipeline Behavior — Attribute-driven with assessment validation</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `[RequiresDPIA]` attribute + `DPIARequiredPipelineBehavior` with enforcement modes** | Consistent with existing compliance behaviors, declarative, cached | Requires attribute on each request type |
| **B) Auto-detection only (no attribute)** | Zero ceremony, all requests evaluated | False positives, performance overhead on every request |
| **C) Configuration-only (options list of request types)** | Centralized config | Easy to forget, disconnected from code, not self-documenting |

### Chosen Option: **A — Attribute-driven with enforcement modes**

### Rationale

- `[RequiresDPIA]` attribute with optional `Reason`, `ProcessingType`, and `ReviewRequired` properties
- `DPIARequiredPipelineBehavior<TRequest, TResponse>` checks:
  1. Is attribute present? (cached via `ConcurrentDictionary`)
  2. Does a current, approved `DPIAAssessment` exist for this request type?
  3. Is the assessment expired (past `NextReviewAtUtc`)?
  4. Did the assessment require prior consultation (Art. 36)?
- Three enforcement modes: `Block` (reject without assessment), `Warn` (log warning, proceed), `Disabled` (skip entirely)
- `options.AutoDetectHighRisk = true` enables optional auto-detection for requests matching high-risk triggers (profiling, special categories, etc.) — supplements attribute-based detection
- Assessment lookup is cached per request type in a `ConcurrentDictionary<Type, DPIAAssessment?>` with TTL refresh

</details>

<details>
<summary><strong>7. DPO Consultation Tracking — First-class domain model with notification support</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) First-class `DPOConsultation` record with notification events** | Full tracking, audit trail, notification integration, matches Art. 35(2) requirement | More domain objects |
| **B) Simple string field on assessment** | Minimal, just DPO name + date | No structured tracking, no notification, no rejection handling |
| **C) External workflow integration only** | Leverages existing tools | No built-in tracking, depends on external systems |

### Chosen Option: **A — First-class `DPOConsultation` record with notification events**

### Rationale

- Art. 35(2) requires: "The controller shall seek the advice of the data protection officer, where designated, when carrying out a data protection impact assessment"
- `DPOConsultation` record: `DPOName`, `DPOEmail`, `RequestedAtUtc`, `RespondedAtUtc`, `Decision` (Approved/Rejected/ConditionallyApproved), `Conditions`, `Comments`
- `IDPIAAssessmentEngine.RequestDPOConsultationAsync()` creates consultation record and publishes `DPOConsultationRequested` notification
- DPO info sourced from `GDPROptions.DataProtectionOfficer` (optional dependency) or `DPIAOptions.DPONotificationEmail`
- When `RequiresPriorConsultation = true` (Art. 36), the assessment flags this and logs it — actual supervisory authority notification is the user's responsibility

</details>

---

## Implementation Phases

### Phase 1: Core Models & Enums

> **Goal**: Define all domain records, enums, and value objects for DPIA assessment.

<details>
<summary><strong>Tasks</strong></summary>

1. **Create project** `src/Encina.Compliance.DPIA/Encina.Compliance.DPIA.csproj`:
   - Target `net10.0`, nullable enabled
   - Reference `Encina` (core) for `Either`, `EncinaError`, `IPipelineBehavior`, `INotification`
   - Optional reference to `Encina.Compliance.GDPR` for `IDataProtectionOfficer`

2. **`RiskLevel.cs`**:
   - `public enum RiskLevel { Low, Medium, High, VeryHigh }`
   - XML docs with GDPR Art. 35 severity references

3. **`DPIAAssessmentStatus.cs`**:
   - `public enum DPIAAssessmentStatus { Draft, InReview, Approved, Rejected, RequiresRevision, Expired }`

4. **`DPOConsultationDecision.cs`**:
   - `public enum DPOConsultationDecision { Pending, Approved, Rejected, ConditionallyApproved }`

5. **`DPIAEnforcementMode.cs`**:
   - `public enum DPIAEnforcementMode { Block, Warn, Disabled }`

6. **`RiskItem.cs`**:
   - `public sealed record RiskItem`
   - Properties: `string Category`, `RiskLevel Level`, `string Description`, `string? MitigationSuggestion`

7. **`Mitigation.cs`**:
   - `public sealed record Mitigation`
   - Properties: `string Description`, `string Category`, `bool IsImplemented`, `DateTimeOffset? ImplementedAtUtc`

8. **`DPIAResult.cs`**:
   - `public sealed record DPIAResult`
   - Properties: `RiskLevel OverallRisk`, `IReadOnlyList<RiskItem> IdentifiedRisks`, `IReadOnlyList<Mitigation> ProposedMitigations`, `bool RequiresPriorConsultation`, `DateTimeOffset AssessedAtUtc`, `string? AssessedBy`
   - Method: `bool IsAcceptable => OverallRisk <= RiskLevel.Medium`

9. **`DPOConsultation.cs`**:
   - `public sealed record DPOConsultation`
   - Properties: `Guid Id`, `string DPOName`, `string DPOEmail`, `DateTimeOffset RequestedAtUtc`, `DateTimeOffset? RespondedAtUtc`, `DPOConsultationDecision Decision`, `string? Conditions`, `string? Comments`

10. **`DPIAAssessment.cs`**:
    - `public sealed record DPIAAssessment`
    - Properties: `Guid Id`, `string RequestTypeName`, `Type? RequestType`, `DPIAAssessmentStatus Status`, `DPIAResult? Result`, `DPOConsultation? DPOConsultation`, `string? ProcessingType`, `string? Reason`, `DateTimeOffset CreatedAtUtc`, `DateTimeOffset? ApprovedAtUtc`, `DateTimeOffset? NextReviewAtUtc`, `IReadOnlyList<DPIAAuditEntry> AuditTrail`
    - Method: `bool IsCurrent(DateTimeOffset nowUtc) => Status == DPIAAssessmentStatus.Approved && (NextReviewAtUtc is null || NextReviewAtUtc > nowUtc)`

11. **`DPIAAuditEntry.cs`**:
    - `public sealed record DPIAAuditEntry`
    - Properties: `Guid Id`, `Guid AssessmentId`, `string Action`, `string? PerformedBy`, `DateTimeOffset OccurredAtUtc`, `string? Details`

12. **`DPIASection.cs`**:
    - `public sealed record DPIASection`
    - Properties: `string Name`, `string Description`, `bool IsRequired`, `IReadOnlyList<string> Questions`

13. **`DPIATemplate.cs`**:
    - `public sealed record DPIATemplate`
    - Properties: `string Name`, `string Description`, `string ProcessingType`, `IReadOnlyList<DPIASection> Sections`, `IReadOnlyList<string> RiskCategories`, `IReadOnlyList<string> SuggestedMitigations`

14. **`DPIAContext.cs`** (input for risk criteria evaluation):
    - `public sealed record DPIAContext`
    - Properties: `Type RequestType`, `string? ProcessingType`, `IReadOnlyList<string> DataCategories`, `IReadOnlyList<string> HighRiskTriggers`, `DPIATemplate? Template`, `IDictionary<string, object> Metadata`

15. **`HighRiskTrigger.cs`** (well-known trigger constants):
    - `public static class HighRiskTriggers`
    - Constants: `BiometricData`, `HealthData`, `AutomatedDecisionMaking`, `SystematicProfiling`, `PublicMonitoring`, `SpecialCategoryData`, `LargeScaleProcessing`, `VulnerableSubjects`, `NovelTechnology`, `CrossBorderTransfer`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of Encina.Compliance.DPIA (Issue #409).

CONTEXT:
- New compliance package: src/Encina.Compliance.DPIA/
- .NET 10 / C# 14, nullable enabled, Railway Oriented Programming
- All domain types are sealed records (immutable value objects)
- Follows patterns from Encina.Compliance.BreachNotification and Encina.Compliance.Retention

TASK:
Create the project file and all domain models, enums, and value objects.

KEY RULES:
- All public types require XML documentation with GDPR article references
- Enums: Low→Medium→High→VeryHigh for RiskLevel (4 levels, matches ICO guidance)
- DPIAAssessmentStatus: Draft, InReview, Approved, Rejected, RequiresRevision, Expired
- DPIAResult: immutable record with OverallRisk, IdentifiedRisks, ProposedMitigations, RequiresPriorConsultation
- DPIAAssessment: full lifecycle with DPOConsultation, review schedule
- IsCurrent() method: checks Status == Approved AND not past NextReviewAtUtc
- DPIAContext: input for risk criteria, carries processing metadata
- HighRiskTriggers: static class with string constants (not enum — extensible)
- All DateTimeOffset properties use "AtUtc" suffix

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/BreachRecord.cs (domain record pattern)
- src/Encina.Compliance.Retention/RetentionRecord.cs (domain record pattern)
- src/Encina.Compliance.GDPR/IDataProtectionOfficer.cs (DPO interface)
```

</details>

---

### Phase 2: Core Interfaces & Abstractions

> **Goal**: Define all service interfaces, the `[RequiresDPIA]` attribute, error factory, and notification events.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Abstractions/IDPIAAssessmentEngine.cs`**:
   - `public interface IDPIAAssessmentEngine`
   - `ValueTask<Either<EncinaError, DPIAResult>> AssessAsync(DPIAContext context, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, bool>> RequiresDPIAAsync(Type requestType, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, DPOConsultation>> RequestDPOConsultationAsync(Guid assessmentId, CancellationToken ct)`

2. **`Abstractions/IRiskCriterion.cs`**:
   - `public interface IRiskCriterion`
   - `string Name { get; }`
   - `ValueTask<RiskItem?> EvaluateAsync(DPIAContext context, CancellationToken ct)` — returns null if criterion not applicable

3. **`Abstractions/IDPIATemplateProvider.cs`**:
   - `public interface IDPIATemplateProvider`
   - `ValueTask<Either<EncinaError, DPIATemplate>> GetTemplateAsync(string processingType, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, IReadOnlyList<DPIATemplate>>> GetAllTemplatesAsync(CancellationToken ct)`

4. **`Abstractions/IDPIAStore.cs`**:
   - `public interface IDPIAStore`
   - `ValueTask<Either<EncinaError, Unit>> SaveAssessmentAsync(DPIAAssessment assessment, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, DPIAAssessment?>> GetAssessmentAsync(string requestTypeName, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, DPIAAssessment?>> GetAssessmentByIdAsync(Guid assessmentId, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, IReadOnlyList<DPIAAssessment>>> GetExpiredAssessmentsAsync(DateTimeOffset nowUtc, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, IReadOnlyList<DPIAAssessment>>> GetAllAssessmentsAsync(CancellationToken ct)`
   - `ValueTask<Either<EncinaError, Unit>> DeleteAssessmentAsync(Guid assessmentId, CancellationToken ct)`

5. **`Abstractions/IDPIAAuditStore.cs`**:
   - `public interface IDPIAAuditStore`
   - `ValueTask<Either<EncinaError, Unit>> RecordAuditEntryAsync(DPIAAuditEntry entry, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, IReadOnlyList<DPIAAuditEntry>>> GetAuditTrailAsync(Guid assessmentId, CancellationToken ct)`

6. **`Attributes/RequiresDPIAAttribute.cs`**:
   - `[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]`
   - `public sealed class RequiresDPIAAttribute : Attribute`
   - Properties: `string? Reason`, `string? ProcessingType`, `bool ReviewRequired = true`
   - XML docs with Art. 35 reference

7. **`DPIAErrors.cs`**:
   - `public static class DPIAErrors`
   - Error codes: `dpia.assessment_required`, `dpia.assessment_expired`, `dpia.assessment_rejected`, `dpia.prior_consultation_required`, `dpia.dpo_consultation_required`, `dpia.risk_too_high`, `dpia.store_error`, `dpia.template_not_found`
   - Static factory methods returning `EncinaError`

8. **`Notifications/DPIAAssessmentCompleted.cs`**:
   - `public sealed record DPIAAssessmentCompleted(Guid AssessmentId, string RequestTypeName, RiskLevel OverallRisk, bool RequiresPriorConsultation) : INotification`

9. **`Notifications/DPOConsultationRequested.cs`**:
   - `public sealed record DPOConsultationRequested(Guid AssessmentId, Guid ConsultationId, string DPOEmail) : INotification`

10. **`Notifications/DPIAAssessmentExpired.cs`**:
    - `public sealed record DPIAAssessmentExpired(Guid AssessmentId, string RequestTypeName, DateTimeOffset ExpiredAtUtc) : INotification`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of Encina.Compliance.DPIA (Issue #409).

CONTEXT:
- Phase 1 models are implemented (DPIAAssessment, DPIAResult, RiskItem, etc.)
- All store methods return ValueTask<Either<EncinaError, T>> (ROP pattern)
- Interfaces are in Abstractions/ subfolder
- Attributes are in Attributes/ subfolder
- Notifications are in Notifications/ subfolder

TASK:
Create all service interfaces, [RequiresDPIA] attribute, error factory, and notification events.

KEY RULES:
- IDPIAAssessmentEngine: main orchestrator, handles assessment + DPO consultation
- IRiskCriterion: single risk dimension evaluator, returns RiskItem? (null = not applicable)
- IDPIAStore: CRUD for assessments, keyed by requestTypeName (string, not Type — for serialization)
- IDPIAAuditStore: separate audit trail
- IDPIATemplateProvider: built-in + custom templates
- [RequiresDPIA] attribute: class-level, AllowMultiple=false, Inherited=true
- DPIAErrors: static class with const error codes + factory methods returning EncinaError
- Notifications: INotification records (DPIAAssessmentCompleted, DPOConsultationRequested, DPIAAssessmentExpired)
- All interfaces require XML documentation with GDPR article references

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/Abstractions/IBreachDetectionRule.cs (criterion pattern)
- src/Encina.Compliance.BreachNotification/Abstractions/IBreachRecordStore.cs (store pattern)
- src/Encina.Compliance.Consent/Attributes/RequireConsentAttribute.cs (attribute pattern)
- src/Encina.Compliance.BreachNotification/BreachNotificationErrors.cs (error factory pattern)
```

</details>

---

### Phase 3: InMemory Implementations & Core Logic

> **Goal**: Implement InMemory stores (default/testing provider), default assessment engine, template provider, and 6 built-in risk criteria.

<details>
<summary><strong>Tasks</strong></summary>

1. **`InMemoryDPIAStore.cs`**:
   - `internal sealed class InMemoryDPIAStore : IDPIAStore`
   - `ConcurrentDictionary<string, DPIAAssessment>` keyed by `RequestTypeName`
   - `ConcurrentDictionary<Guid, DPIAAssessment>` keyed by `Id` (secondary index)
   - All methods return `Right` with results
   - Thread-safe via `ConcurrentDictionary`

2. **`InMemoryDPIAAuditStore.cs`**:
   - `internal sealed class InMemoryDPIAAuditStore : IDPIAAuditStore`
   - `ConcurrentDictionary<Guid, List<DPIAAuditEntry>>` keyed by `AssessmentId`

3. **`DefaultDPIAAssessmentEngine.cs`**:
   - `public sealed class DefaultDPIAAssessmentEngine : IDPIAAssessmentEngine`
   - Constructor: `IEnumerable<IRiskCriterion> criteria`, `IDPIAStore store`, `IDPIAAuditStore auditStore`, `IDPIATemplateProvider templateProvider`, `IOptions<DPIAOptions> options`, `TimeProvider timeProvider`, `ILogger<DefaultDPIAAssessmentEngine> logger`
   - `AssessAsync`: iterates all criteria, collects `RiskItem` results, computes overall risk (max), generates mitigations from template, creates `DPIAResult`
   - `RequiresDPIAAsync`: checks if request type has `[RequiresDPIA]` or matches auto-detection triggers
   - `RequestDPOConsultationAsync`: creates `DPOConsultation` record, updates assessment, records audit entry
   - Overall risk = `risks.Max(r => r.Level)` (conservative — one VeryHigh makes the whole assessment VeryHigh)
   - `RequiresPriorConsultation = OverallRisk >= RiskLevel.VeryHigh && !mitigations.All(m => m.IsImplemented)` (Art. 36)

4. **`DefaultDPIATemplateProvider.cs`**:
   - `public sealed class DefaultDPIATemplateProvider : IDPIATemplateProvider`
   - `Dictionary<string, DPIATemplate>` with 7 built-in templates (profiling, special-category, public-monitoring, ai-ml, biometric, health-data, general)
   - `GetTemplateAsync`: returns matching template or "general" fallback
   - `GetAllTemplatesAsync`: returns all registered templates

5. **`RiskCriteria/SystematicProfilingCriterion.cs`** — Art. 35(3)(a):
   - `public sealed class SystematicProfilingCriterion : IRiskCriterion`
   - Evaluates if `DPIAContext.HighRiskTriggers` contains `HighRiskTriggers.SystematicProfiling`
   - Returns `RiskItem` with `RiskLevel.High` or `RiskLevel.VeryHigh` (if combined with automated decisions)

6. **`RiskCriteria/SpecialCategoryDataCriterion.cs`** — Art. 35(3)(b):
   - Evaluates if data categories include special categories (health, biometric, genetic, racial, political, religious, sexual, criminal)
   - Returns `RiskLevel.High` for any special category, `VeryHigh` for large-scale

7. **`RiskCriteria/SystematicMonitoringCriterion.cs`** — Art. 35(3)(c):
   - Evaluates if triggers include `PublicMonitoring`
   - Returns `RiskLevel.High`

8. **`RiskCriteria/AutomatedDecisionMakingCriterion.cs`**:
   - Evaluates AI/ML, behavioral tracking triggers
   - Returns `RiskLevel.High` or `VeryHigh` (if affecting legal/significant effects)

9. **`RiskCriteria/LargeScaleProcessingCriterion.cs`**:
   - Evaluates `LargeScaleProcessing` trigger
   - Returns `RiskLevel.Medium` or `High` (combined with other factors)

10. **`RiskCriteria/VulnerableSubjectsCriterion.cs`**:
    - Evaluates `VulnerableSubjects` trigger (children, employees, patients)
    - Returns `RiskLevel.High`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of Encina.Compliance.DPIA (Issue #409).

CONTEXT:
- Phases 1-2 are implemented (models, interfaces, attributes, errors, notifications)
- Default implementations use in-memory stores (ConcurrentDictionary)
- 6 built-in risk criteria cover GDPR Art. 35(3) mandatory triggers
- Risk criteria are in RiskCriteria/ subfolder

TASK:
Create in-memory stores, default assessment engine, template provider, and 6 built-in risk criteria.

KEY RULES:
- InMemoryDPIAStore: ConcurrentDictionary, thread-safe, dual index (by requestTypeName + by Id)
- DefaultDPIAAssessmentEngine: iterates IRiskCriterion instances, computes max risk level
- OverallRisk = max(individual RiskItem.Level values) — conservative approach
- RequiresPriorConsultation = OverallRisk >= VeryHigh && not all mitigations implemented
- DefaultDPIATemplateProvider: 7 built-in templates, "general" as fallback
- Each criterion: evaluates DPIAContext.HighRiskTriggers and DataCategories
- Special category data: health, biometric, genetic, racial, political, religious, sexual, criminal
- Criterion returns null if not applicable (not all criteria fire for every assessment)
- TimeProvider for deterministic testing
- All implementations are sealed, internal where possible (public if needed by DI)

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/DefaultBreachDetector.cs (composite evaluator pattern)
- src/Encina.Compliance.BreachNotification/InMemoryBreachRecordStore.cs (in-memory store pattern)
- src/Encina.Compliance.BreachNotification/DetectionRules/ (rule/criterion pattern)
```

</details>

---

### Phase 4: Persistence Entity, Mapper & SQL Scripts

> **Goal**: Create the persistence layer shared infrastructure used by all 13 database providers.

<details>
<summary><strong>Tasks</strong></summary>

1. **Persistence entity** (`DPIAAssessmentEntity.cs` in core package):
   - `public sealed class DPIAAssessmentEntity` with string/primitive properties:
     - `Id (string)`, `RequestTypeName (string)`, `ProcessingType (string?)`, `Reason (string?)`
     - `StatusValue (int)` (maps to `DPIAAssessmentStatus` enum)
     - `ResultJson (string?)` (JSON-serialized `DPIAResult` — includes risks, mitigations, overall risk)
     - `DPOConsultationJson (string?)` (JSON-serialized `DPOConsultation`)
     - `CreatedAtUtc (string)` (ISO 8601 for SQLite compatibility), `ApprovedAtUtc (string?)`, `NextReviewAtUtc (string?)`
     - `TenantId (string?)`, `ModuleId (string?)`

2. **Audit entity** (`DPIAAuditEntryEntity.cs`):
   - `public sealed class DPIAAuditEntryEntity`: `Id (string)`, `AssessmentId (string)`, `Action (string)`, `PerformedBy (string?)`, `OccurredAtUtc (string)`, `Details (string?)`

3. **Mapper** (`DPIAAssessmentMapper.cs`):
   - `public static class DPIAAssessmentMapper`
   - `ToEntity(DPIAAssessment) → DPIAAssessmentEntity` (domain → persistence)
   - `ToDomain(DPIAAssessmentEntity) → DPIAAssessment?` (persistence → domain, null if invalid)
   - JSON serialization for `DPIAResult` and `DPOConsultation` (nested records stored as JSON)

4. **Audit mapper** (`DPIAAuditEntryMapper.cs`):
   - `ToEntity(DPIAAuditEntry) → DPIAAuditEntryEntity`
   - `ToDomain(DPIAAuditEntryEntity) → DPIAAuditEntry`

5. **SQL scripts** (`.github/scripts/` folder — referenced by satellite providers):
   - Table: `DPIAAssessments` — columns matching entity properties
   - Table: `DPIAAuditEntries` — columns matching audit entity
   - Provider-specific DDL:
     - **SQLite**: TEXT for dates (ISO 8601), INTEGER for enums, TEXT for JSON columns
     - **SQL Server**: DATETIMEOFFSET for dates, INT for enums, NVARCHAR(MAX) for JSON
     - **PostgreSQL**: TIMESTAMPTZ for dates, INTEGER for enums, JSONB for JSON columns
     - **MySQL**: DATETIME(6) for dates, INT for enums, JSON for JSON columns
   - Indexes: PRIMARY KEY on Id, UNIQUE INDEX on `(RequestTypeName, TenantId, ModuleId)`, INDEX on StatusValue, INDEX on NextReviewAtUtc

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of Encina.Compliance.DPIA (Issue #409).

CONTEXT:
- Phases 1-3 are implemented (models, interfaces, default InMemory impls, risk criteria)
- Persistence entities are plain classes (not records) for ORM compatibility
- Mappers convert between domain records and persistence entities
- SQL scripts are per-provider due to DDL differences

TASK:
Create persistence entities, mappers, and SQL DDL scripts for all 4 database engines.

KEY RULES:
- Entity classes use public get/set properties (mutable for ORMs)
- DPIAResult and DPOConsultation stored as JSON strings (complex nested objects)
- Mapper.ToEntity uses Guid.ToString("D") for Id
- Mapper.ToDomain returns null if entity state is invalid (defensive)
- SQL scripts: CREATE TABLE IF NOT EXISTS (SQLite), IF NOT EXISTS pattern for others
- SQLite: TEXT for DateTime (ISO 8601 "O" format), INTEGER for enums
- SQL Server: NVARCHAR(450) for string keys, DATETIMEOFFSET(7), INT for enums, NVARCHAR(MAX) for JSON
- PostgreSQL: TEXT for strings, TIMESTAMPTZ, INTEGER for enums, JSONB for JSON
- MySQL: VARCHAR(450) for string keys, DATETIME(6), INT for enums, JSON for JSON
- All tables: PRIMARY KEY on Id, UNIQUE INDEX on (RequestTypeName, TenantId, ModuleId)
- DPIAAuditEntries: FK to DPIAAssessments.Id, INDEX on AssessmentId

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/BreachRecordEntity.cs (entity pattern)
- src/Encina.Compliance.BreachNotification/BreachRecordMapper.cs (mapper pattern)
- src/Encina.Compliance.Retention/RetentionRecordEntity.cs (entity pattern)
```

</details>

---

### Phase 5: Multi-Provider Persistence — 13 Database Providers

> **Goal**: Implement `IDPIAStore` and `IDPIAAuditStore` across all 13 providers with satellite DI registration.

<details>
<summary><strong>Tasks</strong></summary>

#### 5a. ADO.NET Providers (×4)

For each ADO provider (`Encina.ADO.Sqlite`, `Encina.ADO.SqlServer`, `Encina.ADO.PostgreSQL`, `Encina.ADO.MySQL`):

1. `DPIA/DPIAStoreADO.cs` — implements `IDPIAStore`
   - Constructor: `IDbConnection connection`, `string tableName = "DPIAAssessments"`, `string auditTableName = "DPIAAuditEntries"`, `TimeProvider? timeProvider = null`
   - Uses `IDbCommand` / `IDataReader` pattern
   - `GetByRequestTypeAsync`: `SELECT ... FROM DPIAAssessments WHERE RequestTypeName = @RequestTypeName AND (TenantId = @TenantId OR TenantId IS NULL) AND (ModuleId = @ModuleId OR ModuleId IS NULL)`
   - `GetExpiredAsync`: `SELECT ... WHERE NextReviewAtUtc < @NowUtc AND StatusValue = @Approved`
   - Provider-specific SQL: TOP vs LIMIT, parameter syntax, date handling

2. `DPIA/DPIAAuditStoreADO.cs` — implements `IDPIAAuditStore`
   - `GetAuditTrailAsync`: `SELECT ... WHERE AssessmentId = @AssessmentId ORDER BY OccurredAtUtc DESC`

3. **DI registration** in each provider's `ServiceCollectionExtensions.cs`:
   - Add `AddEncinaDPIA{Provider}(services, connectionString)` method
   - Registers `IDPIAStore` → `DPIAStoreADO` as singleton
   - Registers `IDPIAAuditStore` → `DPIAAuditStoreADO` as singleton

#### 5b. Dapper Providers (×4)

For each Dapper provider (`Encina.Dapper.Sqlite`, `Encina.Dapper.SqlServer`, `Encina.Dapper.PostgreSQL`, `Encina.Dapper.MySQL`):

1. `DPIA/DPIAStoreDapper.cs` — implements `IDPIAStore`
   - Uses `Dapper.ExecuteAsync`, `Dapper.QueryAsync`, `Dapper.ExecuteScalarAsync`
   - Anonymous parameter objects
   - Provider-specific SQL (same differences as ADO)
   - **SQLite DateTime**: ALWAYS use `@NowUtc` parameter with `DateTime.UtcNow`, NEVER `datetime('now')`

2. `DPIA/DPIAAuditStoreDapper.cs` — implements `IDPIAAuditStore`

3. **DI registration** — `AddEncinaDPIADapper{Provider}(services, connectionString)`

#### 5c. EF Core Provider

In `Encina.EntityFrameworkCore`:

1. `DPIA/DPIAStoreEF.cs` — implements `IDPIAStore`
   - Uses `DbContext.Set<DPIAAssessmentEntity>()` with LINQ

2. `DPIA/DPIAAuditStoreEF.cs` — implements `IDPIAAuditStore`

3. `DPIA/DPIAAssessmentEntityConfiguration.cs` — `IEntityTypeConfiguration<DPIAAssessmentEntity>`

4. `DPIA/DPIAAuditEntryEntityConfiguration.cs` — `IEntityTypeConfiguration<DPIAAuditEntryEntity>`

5. `DPIA/DPIAModelBuilderExtensions.cs` — `modelBuilder.ApplyDPIAConfiguration()`

6. **DI registration** in `ServiceCollectionExtensions.cs` — integrate with `AddEncinaEntityFrameworkCore` options

#### 5d. MongoDB Provider

In `Encina.MongoDB`:

1. `DPIA/DPIAStoreMongoDB.cs` — implements `IDPIAStore`
   - Uses `IMongoCollection<DPIAAssessmentDocument>`

2. `DPIA/DPIAAuditStoreMongoDB.cs` — implements `IDPIAAuditStore`

3. `DPIA/DPIAAssessmentDocument.cs` — MongoDB document class with `FromDomain` / `ToDomain`
   - `DPOConsultation` stored as embedded document (not separate collection)

4. `DPIA/DPIAAuditEntryDocument.cs` — MongoDB audit document

5. **DI registration** — integrate with `AddEncinaMongoDB` options

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of Encina.Compliance.DPIA (Issue #409).

CONTEXT:
- Phases 1-4 are implemented (core package + entities + mappers + SQL scripts)
- ALL 13 database providers must be implemented: ADO.NET (Sqlite, SqlServer, PostgreSQL, MySQL), Dapper (same 4), EF Core (4 under one package), MongoDB
- Each provider creates a DPIA/ subfolder in its source project

TASK:
Implement IDPIAStore and IDPIAAuditStore for all 13 providers, plus DI registration.

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
  - DPOConsultation is EMBEDDED document within DPIAAssessmentDocument (not separate collection)
- SQLite DateTime: ALWAYS use "O" format for serialization, DateTimeStyles.RoundtripKind for parsing, NEVER use datetime('now')
- All stores validate table names via SqlIdentifierValidator.ValidateTableName()
- GetExpiredAsync must be efficient: filtered query with status = Approved and NextReviewAtUtc < now
- DI: satellite methods called BEFORE AddEncinaDPIA (TryAdd pattern)
- TimeProvider injection for testable time-dependent queries
- Tenant/Module scoping: queries include optional TenantId/ModuleId WHERE clauses

REFERENCE FILES:
- src/Encina.ADO.Sqlite/Retention/RetentionRecordStoreADO.cs (ADO pattern)
- src/Encina.Dapper.Sqlite/Retention/RetentionRecordStoreDapper.cs (Dapper pattern)
- src/Encina.EntityFrameworkCore/Retention/ (EF Core pattern)
- src/Encina.MongoDB/Retention/ (MongoDB pattern)
- Provider ServiceCollectionExtensions.cs in each satellite package
```

</details>

---

### Phase 6: Pipeline Behavior & Auto-Detection

> **Goal**: Implement `DPIARequiredPipelineBehavior` and auto-detection logic for high-risk processing.

<details>
<summary><strong>Tasks</strong></summary>

1. **`DPIARequiredPipelineBehavior.cs`**:
   - `public sealed class DPIARequiredPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>`
   - `private static readonly ConcurrentDictionary<Type, RequiresDPIAAttribute?> AttributeCache = new()`
   - Constructor: `IDPIAStore store`, `IDPIAAssessmentEngine engine`, `IOptions<DPIAOptions> options`, `TimeProvider timeProvider`, `ILogger<DPIARequiredPipelineBehavior<TRequest, TResponse>> logger`
   - `Handle` flow:
     1. Check enforcement mode — if `Disabled`, skip
     2. Check attribute cache — if no `[RequiresDPIA]`, check auto-detection (if enabled)
     3. Look up current assessment in store
     4. If no assessment found:
        - `Block` mode: return `Left(DPIAErrors.AssessmentRequired(typeof(TRequest)))`
        - `Warn` mode: log warning, proceed
     5. If assessment expired:
        - `Block` mode: return `Left(DPIAErrors.AssessmentExpired(...))`
        - `Warn` mode: log warning, proceed
     6. If assessment not approved:
        - `Block` mode: return `Left(DPIAErrors.AssessmentNotApproved(...))`
        - `Warn` mode: log warning, proceed
     7. Log compliance status, proceed with `nextStep()`
   - Emit diagnostics: activity, counters, structured logs

2. **`DPIAAutoDetector.cs`**:
   - `internal sealed class DPIAAutoDetector`
   - `static bool IsHighRisk(Type requestType, DPIAOptions options)` — checks:
     - Request type name matches any configured high-risk trigger pattern
     - Request type implements known high-risk interfaces (if any)
     - Metadata annotations indicate special category data
   - Used by pipeline behavior when `options.AutoDetectHighRisk = true`

3. **`DPIAAutoRegistrationDescriptor.cs`**:
   - `internal sealed record DPIAAutoRegistrationDescriptor(IReadOnlyList<Assembly> AssembliesToScan)`

4. **`DPIAAutoRegistrationHostedService.cs`**:
   - `internal sealed class DPIAAutoRegistrationHostedService : IHostedService`
   - Scans assemblies for `[RequiresDPIA]` attributes
   - Registers request types that need DPIA assessment
   - Creates draft assessments in `IDPIAStore` for types without existing assessments
   - Logs count of registered types

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of Encina.Compliance.DPIA (Issue #409).

CONTEXT:
- Phases 1-5 are implemented (models, interfaces, default implementations, risk criteria, persistence entities, 13-provider stores)
- Pipeline behavior follows the established pattern from 7 existing compliance packages
- Static attribute caching via ConcurrentDictionary per generic type

TASK:
Create DPIARequiredPipelineBehavior, DPIAAutoDetector, and auto-registration hosted service.

KEY RULES:
- Pipeline behavior: static ConcurrentDictionary<Type, RequiresDPIAAttribute?> AttributeCache
- Three enforcement modes: Block (return Left), Warn (log + proceed), Disabled (skip entirely)
- Assessment validation: exists? + approved? + not expired?
- Auto-detection: only when options.AutoDetectHighRisk = true (supplements attribute-based)
- Auto-registration: IHostedService scans assemblies for [RequiresDPIA], creates draft assessments
- Observability: Activity + Counter + structured log on every check
- ConfigureAwait(false) on all awaits
- Return await nextStep().ConfigureAwait(false) when proceeding

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/BreachDetectionPipelineBehavior.cs (pipeline behavior pattern)
- src/Encina.Compliance.Consent/ConsentRequiredPipelineBehavior.cs (attribute caching pattern)
- src/Encina.Compliance.GDPR/GDPRAutoRegistrationHostedService.cs (auto-registration pattern)
```

</details>

---

### Phase 7: Configuration, DI & Health Check

> **Goal**: Implement options, validation, DI registration, review reminder service, and health check.

<details>
<summary><strong>Tasks</strong></summary>

1. **`DPIAOptions.cs`**:
   - `public sealed class DPIAOptions`
   - Properties:
     - `DPIAEnforcementMode EnforcementMode { get; set; } = DPIAEnforcementMode.Block`
     - `bool AutoDetectHighRisk { get; set; } = false`
     - `string? DPONotificationEmail { get; set; }`
     - `int ReviewIntervalMonths { get; set; } = 12`
     - `bool BlockWithoutDPIA { get; set; } = true` (alias for Block enforcement)
     - `bool AutoRegisterFromAttributes { get; set; } = true`
     - `bool AddHealthCheck { get; set; } = false`
     - `List<string> HighRiskTriggers { get; } = []` (user-defined triggers)
     - `List<Assembly> AssembliesToScan { get; } = []`

2. **`DPIAOptionsValidator.cs`**:
   - `internal sealed class DPIAOptionsValidator : IValidateOptions<DPIAOptions>`
   - Validates: `ReviewIntervalMonths > 0`, `ReviewIntervalMonths <= 60`
   - Warning if `EnforcementMode == Block` and no `DPONotificationEmail` configured

3. **`ServiceCollectionExtensions.cs`**:
   - `public static IServiceCollection AddEncinaDPIA(this IServiceCollection services, Action<DPIAOptions>? configure = null)`
   - Registration order:
     1. Configure options + validator
     2. `TryAddSingleton(TimeProvider.System)`
     3. `TryAddSingleton<IDPIAStore, InMemoryDPIAStore>()`
     4. `TryAddSingleton<IDPIAAuditStore, InMemoryDPIAAuditStore>()`
     5. `TryAddSingleton<IDPIATemplateProvider, DefaultDPIATemplateProvider>()`
     6. `TryAddScoped<IDPIAAssessmentEngine, DefaultDPIAAssessmentEngine>()`
     7. Register built-in `IRiskCriterion` implementations (all 6)
     8. `TryAddTransient(typeof(IPipelineBehavior<,>), typeof(DPIARequiredPipelineBehavior<,>))`
     9. If `options.AutoRegisterFromAttributes`: register `DPIAAutoRegistrationDescriptor` + `DPIAAutoRegistrationHostedService`
     10. If `options.AddHealthCheck`: register `DPIAHealthCheck`
   - Optional: `AddDPIAReviewReminder()` registers `DPIAReviewReminderService`
   - **Note**: Satellite providers (Phase 5) register their store implementations via `TryAdd` BEFORE this method is called — InMemoryDPIAStore acts as fallback default when no database provider is configured

4. **`DPIAReviewReminderService.cs`**:
   - `internal sealed class DPIAReviewReminderService : BackgroundService`
   - Periodically checks for assessments nearing review deadline
   - Publishes `DPIAAssessmentExpired` notification for expired assessments
   - Configurable check interval (default: daily)

5. **`Health/DPIAHealthCheck.cs`**:
   - `public sealed class DPIAHealthCheck : IHealthCheck`
   - `public const string DefaultName = "encina-dpia"`
   - `public static readonly string[] Tags = ["encina", "dpia", "compliance", "gdpr", "ready"]`
   - Checks:
     1. Options configured and valid
     2. `IDPIAStore` resolvable
     3. `IDPIAAssessmentEngine` resolvable
     4. If any registered request types lack assessments → `Degraded`
     5. If any assessments are expired → `Degraded`
   - Returns `Healthy`, `Degraded`, or `Unhealthy`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of Encina.Compliance.DPIA (Issue #409).

CONTEXT:
- Phases 1-6 are implemented (models, interfaces, InMemory impls, persistence entities, 13-provider stores, pipeline behavior)
- DI follows the established compliance package pattern: AddEncina{Feature}()
- All core services use TryAdd (override-friendly)
- Health check follows scoped resolution pattern

TASK:
Create DPIAOptions, validator, ServiceCollectionExtensions, review reminder service, and health check.

KEY RULES:
- DPIAOptions: BlockWithoutDPIA is alias for EnforcementMode.Block
- DI registration order: options → TimeProvider → stores → template → engine → criteria → pipeline → auto-registration → health check
- TryAddSingleton for stores and template provider (user can override via satellite providers)
- InMemoryDPIAStore registered as default — satellite providers (Phase 5) replace via TryAdd
- TryAddScoped for assessment engine (request-scoped)
- TryAddTransient for pipeline behavior (generic open type)
- Built-in IRiskCriterion: register ALL 6 with TryAddEnumerable
- Review reminder: BackgroundService with periodic expired assessment check
- Health check: const DefaultName, static Tags array, scoped resolution via CreateScope()
- Health check checks: options valid, store resolvable, engine resolvable, no missing assessments, no expired

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/ServiceCollectionExtensions.cs (DI pattern)
- src/Encina.Compliance.BreachNotification/Health/BreachNotificationHealthCheck.cs (health check pattern)
- src/Encina.Compliance.Retention/RetentionEnforcementService.cs (BackgroundService pattern)
- src/Encina.Compliance.BreachNotification/BreachNotificationOptions.cs (options pattern)
```

</details>

---

### Phase 8: ASP.NET Core Integration

> **Goal**: Add ASP.NET Core endpoint extensions for DPIA assessment management.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Encina.AspNetCore` extension** — `DPIAEndpointExtensions.cs`:
   - `MapDPIAEndpoints(this IEndpointRouteBuilder endpoints, string prefix = "/api/dpia")` — adds:
     - `GET /api/dpia/assessments` — list all assessments
     - `GET /api/dpia/assessments/{id}` — get assessment by ID
     - `POST /api/dpia/assessments/{requestType}/assess` — trigger assessment for a request type
     - `POST /api/dpia/assessments/{id}/approve` — DPO approves assessment
     - `POST /api/dpia/assessments/{id}/reject` — DPO rejects assessment
     - `GET /api/dpia/templates` — list available templates
     - `GET /api/dpia/expired` — list expired assessments needing review
   - All endpoints return `ProblemDetails` on error (standard ASP.NET Core pattern)
   - Endpoints are optional — user must call `MapDPIAEndpoints()` explicitly

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 of Encina.Compliance.DPIA (Issue #409).

CONTEXT:
- Phases 1-7 are implemented (full core + 13-provider stores + pipeline + DI + health check)
- ASP.NET Core integration provides management endpoints
- Optional — user must explicitly call MapDPIAEndpoints()

TASK:
Add DPIA management endpoints to Encina.AspNetCore.

KEY RULES:
- Endpoints use minimal API pattern (MapGet, MapPost)
- All endpoints resolve IDPIAStore, IDPIAAssessmentEngine from DI
- Return ProblemDetails for errors (RFC 9457)
- Endpoints are behind a configurable prefix ("/api/dpia" default)
- Authorization is the user's responsibility (no built-in auth)
- Do NOT add tests here — all tests will be added in Phase 11 (Testing)

REFERENCE FILES:
- src/Encina.AspNetCore/ (existing ASP.NET Core integration patterns)
```

</details>

---

### Phase 9: Cross-Cutting Integration

> **Goal**: Integrate DPIA with other compliance and cross-cutting modules, including ASP.NET Core endpoints.

<details>
<summary><strong>Tasks</strong></summary>

1. **GDPR Integration** (`Integration/GDPRIntegration.cs`):
   - `internal static class GDPRIntegration`
   - Helper to extract DPO info from `GDPROptions.DataProtectionOfficer` when `DPIAOptions.DPONotificationEmail` is not set
   - Conditional compilation with `#if` or runtime service resolution (soft dependency)

2. **Audit Trail Integration**:
   - `DPIAAuditEntry` records all assessment lifecycle events
   - All state transitions go through `IDPIAAuditStore.RecordAuditEntryAsync()`
   - Audit actions: `Created`, `Assessed`, `DPOConsultationRequested`, `DPOConsultationResponded`, `Approved`, `Rejected`, `Expired`, `Deleted`
   - **ASP.NET Core endpoints**: approve/reject/assess endpoints create audit entries through the engine (no direct audit store calls from endpoints)

3. **Multi-Tenancy Support** (`ITenantContext` integration):
   - `DPIAAssessment` includes optional `string? TenantId` property
   - Assessment lookup is tenant-scoped when `ITenantContext` is available
   - In-memory store supports per-tenant assessment isolation
   - **ASP.NET Core endpoints**: resolve `ITenantContext` from HttpContext to scope queries per tenant

4. **Module Isolation Support** (`IModuleContext` integration):
   - `DPIAAssessment` includes optional `string? ModuleId` property
   - Assessment lookup is module-scoped in modular monolith scenarios
   - **ASP.NET Core endpoints**: resolve `IModuleContext` from HttpContext to scope queries per module

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
You are implementing Phase 9 of Encina.Compliance.DPIA (Issue #409).

CONTEXT:
- Phases 1-8 are implemented (full core + 13-provider stores + pipeline + DI + ASP.NET Core endpoints)
- Cross-cutting integration connects DPIA with other Encina modules
- Soft dependencies (resolve from DI, null if not registered)
- ASP.NET Core endpoints (Phase 8) must also be integrated with cross-cutting concerns

TASK:
Add GDPR DPO integration, audit trail, multi-tenancy, and module isolation support across all code including endpoints.

KEY RULES:
- GDPR integration: resolve IDataProtectionOfficer from DI if DPIAOptions.DPONotificationEmail is null
- Audit trail: every state change creates DPIAAuditEntry via IDPIAAuditStore — endpoints go through engine, not direct audit calls
- Multi-tenancy: optional TenantId on DPIAAssessment, resolve ITenantContext if available — ASP.NET Core endpoints resolve from HttpContext
- Module isolation: optional ModuleId on DPIAAssessment, resolve IModuleContext if available — ASP.NET Core endpoints resolve from HttpContext
- All integrations are soft dependencies — DPIA works without any of them

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/BreachDetectionPipelineBehavior.cs (cross-cutting integration)
- src/Encina.Compliance.Retention/RetentionValidationPipelineBehavior.cs (tenant/module integration)
- src/Encina.AspNetCore/ (endpoint cross-cutting patterns)
```

</details>

---

### Phase 10: Observability — Diagnostics, Metrics & Logging

> **Goal**: Add OpenTelemetry traces, counters, histograms, and structured logging across all code including ASP.NET Core endpoints.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Diagnostics/DPIADiagnostics.cs`**:
   - `internal static class DPIADiagnostics`
   - `ActivitySource`: `"Encina.Compliance.DPIA"`, version `"1.0"`
   - `Meter`: `"Encina.Compliance.DPIA"`, version `"1.0"`
   - **Counters** (Counter<long>):
     - `dpia.assessment.total` — tagged by `outcome` (completed, failed), `risk_level`
     - `dpia.pipeline.executions.total` — tagged by `outcome` (approved, blocked, warned, skipped)
     - `dpia.dpo_consultation.total` — tagged by `decision` (approved, rejected, conditional, pending)
     - `dpia.review_reminder.total` — tagged by `outcome` (expired, approaching)
     - `dpia.risk_criterion.evaluations.total` — tagged by `criterion_name`, `outcome` (applicable, not_applicable)
     - `dpia.endpoint.requests.total` — tagged by `endpoint` (assessments, assess, approve, reject, templates, expired), `status_code`
   - **Histograms** (Histogram<double>):
     - `dpia.assessment.duration.ms` — assessment engine execution time
     - `dpia.pipeline.duration.ms` — pipeline behavior overhead
     - `dpia.endpoint.duration.ms` — ASP.NET Core endpoint response time
   - **Tag constants**:
     - `TagRequestType`, `TagOutcome`, `TagRiskLevel`, `TagCriterionName`, `TagDecision`, `TagEnforcementMode`, `TagEndpoint`, `TagStatusCode`
   - **Activity helpers**:
     - `StartAssessment(string requestTypeName)` → `Activity?`
     - `StartPipelineExecution(string requestTypeName)` → `Activity?`
     - `StartDPOConsultation(Guid assessmentId)` → `Activity?`
     - `StartReviewCheck()` → `Activity?`
     - `StartCriterionEvaluation(string criterionName)` → `Activity?`
     - `StartEndpointExecution(string endpointName)` → `Activity?`
   - **Outcome recorders**: `RecordCompleted`, `RecordFailed`, `RecordSkipped`

2. **`Diagnostics/DPIALogMessages.cs`**:
   - `internal static partial class DPIALogMessages` using `[LoggerMessage]` source generator
   - **Event ID range: 8900-8999**:
     - 8900-8909: Pipeline behavior
       - 8900: Pipeline disabled
       - 8901: Pipeline no attribute
       - 8902: Pipeline started
       - 8903: Pipeline assessment found (approved)
       - 8904: Pipeline assessment missing (blocked)
       - 8905: Pipeline assessment missing (warned)
       - 8906: Pipeline assessment expired (blocked)
       - 8907: Pipeline assessment expired (warned)
       - 8908: Pipeline completed
     - 8910-8919: Assessment engine
       - 8910: Assessment started
       - 8911: Risk criterion evaluated
       - 8912: Assessment completed
       - 8913: Prior consultation required
       - 8914: Assessment engine error
     - 8920-8929: DPO consultation
       - 8920: DPO consultation requested
       - 8921: DPO consultation responded
       - 8922: DPO approval granted
       - 8923: DPO approval rejected
       - 8924: DPO conditional approval
     - 8930-8939: Assessment lifecycle
       - 8930: Assessment created
       - 8931: Assessment approved
       - 8932: Assessment rejected
       - 8933: Assessment expired
       - 8934: Assessment deleted
     - 8940-8949: Review reminder
       - 8940: Review check started
       - 8941: Review approaching deadline
       - 8942: Review expired
       - 8943: Review check completed
     - 8950-8959: Health check
       - 8950: Health check completed
       - 8951: Health check degraded
     - 8960-8969: Auto-registration
       - 8960: Auto-registration started
       - 8961: Auto-registration completed
       - 8962: Auto-registration type found
     - 8970-8979: Store operations
       - 8970: Store error
       - 8971: Store operation completed
     - 8980-8989: Audit trail
       - 8980: Audit entry recorded
       - 8981: Audit trail queried
     - 8990-8999: ASP.NET Core endpoints
       - 8990: Endpoint request received
       - 8991: Endpoint request completed
       - 8992: Endpoint request failed
       - 8993: Endpoint assessment triggered
       - 8994: Endpoint DPO action processed

3. **Integrate observability** into existing code (Phases 3-8):
   - Add Activity and Counter calls to `DefaultDPIAAssessmentEngine`, pipeline behavior, review reminder, health check
   - All risk criteria emit individual evaluation metrics
   - **ASP.NET Core endpoints**: each endpoint wrapped with Activity span + counter + structured log on entry/exit/error

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 10</strong></summary>

```
You are implementing Phase 10 of Encina.Compliance.DPIA (Issue #409).

CONTEXT:
- Phases 1-9 are implemented (full core + 13-provider stores + pipeline + DI + ASP.NET Core + cross-cutting)
- Observability follows OpenTelemetry patterns: ActivitySource, Meter, ILogger
- Event IDs in 8900-8999 range (new range, avoids collision with existing packages)
- ASP.NET Core endpoints (Phase 8) must also have observability

TASK:
Create DPIADiagnostics, DPIALogMessages, and integrate observability into ALL existing code including ASP.NET Core endpoints.

KEY RULES:
- ActivitySource name: "Encina.Compliance.DPIA" (matches package name)
- Meter: new Meter("Encina.Compliance.DPIA", "1.0")
- All counters use tag-based dimensions for flexible dashboards
- Key metric: dpia.pipeline.executions.total with outcome tags (approved/blocked/warned/skipped)
- NEW: dpia.endpoint.requests.total counter for ASP.NET Core endpoints with endpoint + status_code tags
- NEW: dpia.endpoint.duration.ms histogram for endpoint response times
- NEW: StartEndpointExecution activity helper for endpoint tracing
- NEW: Event IDs 8990-8999 for ASP.NET Core endpoint log messages
- DPIALogMessages uses [LoggerMessage] source generator (partial class, partial methods)
- Activity helpers check Source.HasListeners() before creating activities
- SetTag uses string constants from tag fields
- Event ID range: 8900-8999

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/Diagnostics/BreachNotificationDiagnostics.cs (ActivitySource + counters + histograms)
- src/Encina.Compliance.BreachNotification/Diagnostics/BreachNotificationLogMessages.cs ([LoggerMessage] pattern)
- src/Encina.Compliance.Retention/Diagnostics/RetentionDiagnostics.cs (separate meter)
- src/Encina.AspNetCore/ (endpoint observability patterns)
```

</details>

---

### Phase 11: Testing — 7 Test Types

> **Goal**: Comprehensive test coverage across all test categories, covering ALL preceding phases (1-10) including ASP.NET Core endpoints.

<details>
<summary><strong>Tasks</strong></summary>

#### 11a. Unit Tests (`tests/Encina.UnitTests/Compliance/DPIA/`)

- `RiskLevelTests.cs` — enum value ordering, comparison
- `DPIAAssessmentTests.cs` — IsCurrent() logic, status transitions, review deadline calculation
- `DPIAResultTests.cs` — IsAcceptable property, RequiresPriorConsultation logic
- `DPOConsultationTests.cs` — decision states, pending → responded transitions
- `DPIAContextTests.cs` — metadata handling, trigger matching
- `DefaultDPIAAssessmentEngineTests.cs` — risk aggregation (max), prior consultation detection, DPO consultation flow, empty criteria
- `DefaultDPIATemplateProviderTests.cs` — all 7 built-in templates, fallback to "general", unknown type
- `SystematicProfilingCriterionTests.cs` — trigger matching, risk level assignment
- `SpecialCategoryDataCriterionTests.cs` — all 8 special categories, large-scale detection
- `SystematicMonitoringCriterionTests.cs` — public monitoring trigger
- `AutomatedDecisionMakingCriterionTests.cs` — AI/ML triggers, legal effects
- `LargeScaleProcessingCriterionTests.cs` — volume trigger, combined factors
- `VulnerableSubjectsCriterionTests.cs` — children, employees, patients
- `DPIARequiredPipelineBehaviorTests.cs` — Block/Warn/Disabled modes, attribute caching, assessment lookup, expired assessment, auto-detection
- `DPIAAutoDetectorTests.cs` — trigger matching, no false positives on standard requests
- `InMemoryDPIAStoreTests.cs` — CRUD, get-by-type, get-expired, thread safety
- `InMemoryDPIAAuditStoreTests.cs` — record + query audit trail
- `DPIAReviewReminderServiceTests.cs` — periodic check, expired assessment notification
- `DPIAOptionsValidatorTests.cs` — validation rules
- `ServiceCollectionExtensionsTests.cs` — DI registration verification
- `DPIAHealthCheckTests.cs` — healthy/degraded/unhealthy scenarios
- `DPIAErrorsTests.cs` — all error factory methods produce correct codes
- `DPIAEndpointExtensionsTests.cs` — endpoint registration, route mapping, ProblemDetails responses, approve/reject/assess delegate logic

**Target**: ~90-110 unit tests

#### 11b. Guard Tests (`tests/Encina.GuardTests/Compliance/DPIA/`)

- All public constructors and methods: null checks for non-nullable parameters
- Cover: all interface implementations, options, attributes, pipeline behavior, risk criteria, assessment engine
- Cover ASP.NET Core: `DPIAEndpointExtensions` public methods (null `IEndpointRouteBuilder`, null prefix)
- Use `GuardClauses.xUnit` library

**Target**: ~35-55 guard tests

#### 11c. Contract Tests (`tests/Encina.ContractTests/Compliance/DPIA/`)

- `IDPIAStoreContractTests.cs` — verify store implementations follow the same contract
- `IRiskCriterionContractTests.cs` — all 6 criteria follow the same interface contract
- `IDPIATemplateProviderContractTests.cs` — template provider contract
- `IDPIAAssessmentEngineContractTests.cs` — engine contract

**Target**: ~15-25 contract tests

#### 11d. Property Tests (`tests/Encina.PropertyTests/Compliance/DPIA/`)

- `DPIAAssessmentPropertyTests.cs` — IsCurrent invariant: approved + not past review date = current
- `RiskAggregationPropertyTests.cs` — max(risks) = overall risk for any combination of risk items
- `DPIAResultPropertyTests.cs` — IsAcceptable iff OverallRisk <= Medium
- `ReviewDeadlinePropertyTests.cs` — NextReviewAtUtc = ApprovedAtUtc + ReviewIntervalMonths

**Target**: ~12-18 property tests

#### 11e. Integration Tests (`tests/Encina.IntegrationTests/Compliance/DPIA/`)

**Core integration tests** (with InMemory store):

- `DPIAAssessmentEngineIntegrationTests.cs` — full assessment flow with all 6 criteria
- `DPIAPipelineBehaviorIntegrationTests.cs` — end-to-end pipeline with real DI container
- `DPIAAutoRegistrationIntegrationTests.cs` — hosted service scans test assemblies
- `DPIAReviewReminderIntegrationTests.cs` — background service with time manipulation

**ASP.NET Core endpoint integration tests** (with `WebApplicationFactory`):

- `DPIAEndpointIntegrationTests.cs` — full HTTP request/response cycle:
  - `GET /api/dpia/assessments` returns 200 with list
  - `POST /api/dpia/assessments/{requestType}/assess` triggers assessment through engine
  - `POST /api/dpia/assessments/{id}/approve` returns 200 and updates status
  - `POST /api/dpia/assessments/{id}/reject` returns 200 and updates status
  - `GET /api/dpia/expired` returns only expired assessments
  - Invalid ID returns ProblemDetails with 404
  - Missing engine dependency returns ProblemDetails with 500
  - Tenant-scoped: requests with `X-Tenant-Id` header return tenant-filtered results
  - Module-scoped: requests with `X-Module-Id` header return module-filtered results

**Database integration tests** — for ALL 13 providers:

- `DPIAStore{Provider}IntegrationTests.cs` — CRUD, get-by-request-type, get-expired, tenant/module scoping against real DB
- `DPIAAuditStore{Provider}IntegrationTests.cs` — audit CRUD against real DB
- Each uses `[Collection("{Provider}")]` fixtures (existing collections)
- `InitializeAsync` creates schema + clears data
- Tests: AddAssessment → GetByRequestType, UpdateAssessment, GetExpired, GetByStatus, tenant-scoped queries

**Target**: ~130-165 integration tests (~10 tests × 13 providers + ~15 core + ~10 ASP.NET Core endpoint tests)

#### 11f. Load Tests (`tests/Encina.LoadTests/Compliance/DPIA/`)

- `DPIAStoreConcurrencyLoadTests.cs` — concurrent assessment writes from multiple simulated instances
- `DPIAPipelineBehaviorLoadTests.md` — justification document (assessment lookup is O(1) dictionary lookup, not a hot path; DPIA is checked once per request type, not per request)
- `DPIAEndpointLoadTests.md` — justification document (endpoints are admin-facing management operations, not high-throughput request paths)

**Target**: 1 load test class + 2 justification documents

#### 11g. Benchmark Tests (`tests/Encina.BenchmarkTests/Compliance/DPIA/`)

- `RiskCriterionBenchmarks.cs` — criterion evaluation overhead per DPIAContext
- `PipelineBehaviorBenchmarks.cs` — attribute caching + assessment lookup overhead
- `MapperBenchmarks.cs` — entity ↔ domain mapping + JSON serialization overhead
- `AssessmentEngineBenchmarks.md` — justification (full assessment runs ~5-30 per app lifecycle, not a hot path)
- `EndpointBenchmarks.md` — justification (management endpoints, not hot paths; HTTP overhead dominates any application logic)

**Target**: 3 benchmark classes + 2 justification documents

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 11</strong></summary>

```
You are implementing Phase 11 of Encina.Compliance.DPIA (Issue #409).

CONTEXT:
- Phases 1-10 are fully implemented (core + 13 providers + pipeline + DI + ASP.NET Core endpoints + cross-cutting + observability)
- 7 test types must be implemented: Unit, Guard, Contract, Property, Integration, Load, Benchmark
- Integration tests must cover ALL 13 database providers + InMemory + ASP.NET Core endpoints
- Tests follow AAA pattern, descriptive names, single responsibility
- ASP.NET Core endpoints (Phase 8) need full test coverage across applicable test types

TASK:
Create comprehensive test coverage across all 7 test types, covering ALL preceding code (Phases 1-10).

KEY RULES:
Unit Tests:
- Mock all dependencies (Moq/NSubstitute)
- Test each risk criterion independently with various DPIAContext inputs
- Test assessment engine: max-risk aggregation, prior consultation, DPO flow
- Pipeline behavior: 3 enforcement modes, assessment found/missing/expired
- Test mappers: domain ↔ entity round-trip, JSON serialization of DPIAResult/DPOConsultation
- ASP.NET Core: test endpoint registration, route mapping, ProblemDetails responses, approve/reject/assess delegates
- Fast execution (<1ms per test)

Guard Tests:
- GuardClauses.xUnit for all public constructors/methods
- Cover: engine, store, audit store, criteria, pipeline behavior, template provider, mappers
- Cover ASP.NET Core: DPIAEndpointExtensions public methods (null IEndpointRouteBuilder, null prefix)

Contract Tests:
- Abstract base with concrete per-implementation
- All 6 IRiskCriterion implementations follow same contract
- IDPIAStore: CRUD + query invariants — verify all 14 store implementations (13 DB + InMemory) follow identical behavior

Property Tests:
- FsCheck generators for DPIAAssessment, RiskItem, DPIAResult
- Invariants: IsCurrent, IsAcceptable, max-risk aggregation, review deadline
- Mapper round-trip: domain → entity → domain preserves all fields

Integration Tests:
- [Collection("ADO-Sqlite")] etc. — reuse existing fixtures
- ClearAllDataAsync in InitializeAsync
- Create schema (DPIAAssessments, DPIAAuditEntries tables)
- Test CRUD: AddAssessment → GetByRequestType → UpdateStatus → GetExpired
- Test tenant/module scoping: same requestTypeName, different tenants
- SQLite: NEVER dispose shared connection from fixture
- Core integration tests: end-to-end assessment flow with real DI container
- ASP.NET Core: WebApplicationFactory endpoint tests — full HTTP cycle, ProblemDetails on error, tenant/module header scoping

Load Tests:
- DPIAStoreConcurrencyLoadTests.cs — concurrent assessment writes from multiple instances
- DPIAPipelineBehaviorLoadTests.md — justification (assessment lookup is O(1), not hot path)
- DPIAEndpointLoadTests.md — justification (management endpoints, not high-throughput)

Benchmark Tests:
- BenchmarkSwitcher (NOT BenchmarkRunner)
- Results to artifacts/performance/
- Criterion evaluation + pipeline behavior overhead + mapper serialization overhead
- EndpointBenchmarks.md — justification (HTTP overhead dominates, not hot path)

REFERENCE FILES:
- tests/Encina.UnitTests/Compliance/BreachNotification/ (unit test patterns)
- tests/Encina.GuardTests/Compliance/BreachNotification/ (guard test patterns)
- tests/Encina.PropertyTests/Compliance/BreachNotification/ (FsCheck patterns)
- tests/Encina.ContractTests/Compliance/BreachNotification/ (contract test patterns)
- tests/Encina.IntegrationTests/Compliance/BreachNotification/ (integration test patterns with Collection fixtures)
- tests/Encina.IntegrationTests/AspNetCore/ (ASP.NET Core endpoint integration test patterns)
```

</details>

---

### Phase 12: Documentation & Finalization

> **Goal**: Update all project documentation, verify build, and finalize.

<details>
<summary><strong>Tasks</strong></summary>

1. **XML documentation review** — Verify all public APIs have:
   - `<summary>` with GDPR article references
   - `<remarks>` for complex behavior
   - `<param>` for all parameters
   - `<returns>` for return values
   - `<example>` for key usage scenarios

2. **CHANGELOG.md** — Add entry under Unreleased:
   - `### Added`
   - `- Encina.Compliance.DPIA — GDPR Data Protection Impact Assessment automation (Articles 35-36) with IDPIAAssessmentEngine, IRiskCriterion, IDPIATemplateProvider, IDPIAStore, DPIARequiredPipelineBehavior, [RequiresDPIA] attribute, DPO consultation tracking, review reminders, 6 built-in risk criteria, and 7 built-in templates (Fixes #409)`

3. **ROADMAP.md** — Update if milestone or planned feature is affected

4. **Package README.md** — Create `src/Encina.Compliance.DPIA/README.md` with:
   - Package overview and GDPR article references
   - Quick start: `AddEncinaDPIA()` configuration
   - `[RequiresDPIA]` attribute usage
   - Risk criteria customization
   - DPO consultation flow
   - Template management
   - ASP.NET Core endpoint integration

5. **docs/features/dpia.md** — Feature documentation:
   - Comprehensive usage guide
   - Configuration reference
   - Risk scoring algorithm explanation
   - Template customization guide
   - DPO workflow documentation
   - Prior consultation (Art. 36) detection rules
   - Integration with other compliance modules

6. **docs/INVENTORY.md** — Update with new package entry

7. **`PublicAPI.Unshipped.txt`** — Ensure all public symbols are tracked

8. **Build verification**:
   - `dotnet build Encina.slnx --configuration Release` → 0 errors, 0 warnings
   - `dotnet test` → all pass, coverage ≥85%

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 12</strong></summary>

```
You are implementing Phase 12 of Encina.Compliance.DPIA (Issue #409).

CONTEXT:
- Phases 1-11 are fully implemented and tested
- Documentation and finalization remaining

TASK:
Update CHANGELOG.md, create package README.md, feature docs, update INVENTORY.md, verify build.

KEY RULES:
- CHANGELOG.md: add under ### Added in Unreleased section
- README.md: comprehensive with quick start, configuration, risk criteria, DPO flow, templates
- docs/features/dpia.md: full usage guide with Art. 35/36 references
- INVENTORY.md: add Encina.Compliance.DPIA entry
- Build must produce 0 errors and 0 warnings
- All tests must pass
- PublicAPI.Unshipped.txt must be complete
- Commit message: "feat: add Encina.Compliance.DPIA - GDPR Data Protection Impact Assessment with risk scoring and DPO consultation (Fixes #409)"

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/README.md (README pattern)
- docs/features/ (existing feature documentation)
- CHANGELOG.md (existing format)
```

</details>

---

## Research

### GDPR Article References

| Article | Requirement | Key Details |
|---------|-------------|-------------|
| Art. 35(1) | DPIA obligation | "Where a type of processing... is likely to result in a high risk to the rights and freedoms of natural persons, the controller shall... carry out an assessment" |
| Art. 35(2) | DPO consultation | "The controller shall seek the advice of the data protection officer, where designated" |
| Art. 35(3) | Mandatory triggers | (a) systematic profiling with significant effects, (b) large-scale special category data, (c) systematic monitoring of public areas |
| Art. 35(4) | Supervisory authority lists | Lists of processing operations requiring DPIA |
| Art. 35(5) | Exemption lists | Lists of processing operations NOT requiring DPIA |
| Art. 35(6) | Single DPIA for similar | Single assessment may address similar processing operations |
| Art. 35(7) | Minimum content | Systematic description, necessity/proportionality, risks, measures |
| Art. 35(8) | Codes of conduct | Compliance with approved codes of conduct shall be taken into account |
| Art. 35(9) | Data subject views | "seek the views of data subjects or their representatives" where appropriate |
| Art. 35(10) | Existing processing | DPIA for processing already underway if significant risk change |
| Art. 35(11) | Review obligation | "where necessary... carry out a review to assess if processing is performed in accordance with the DPIA" |
| Art. 36(1) | Prior consultation | "The controller shall consult the supervisory authority prior to processing where a DPIA indicates that the processing would result in a high risk" |
| Art. 36(2) | Consultation content | Controller shall provide: DPIA, responsibilities, measures and safeguards, DPO contact |
| Art. 36(3) | Response timeframe | Supervisory authority shall provide written advice within 8 weeks (extendable to 14) |
| Recital 84 | DPIA purpose | "to evaluate, in particular, the origin, nature, particularity and severity of that risk" |
| Recital 89 | Art. 20 notification elimination | DPIA replaces prior Art. 20 notification requirement |
| Recital 90 | DPIA content guidance | Detailed guidance on systematic description, necessity, proportionality assessment |
| Recital 91 | Large-scale processing | Definition of "large scale" for DPIA triggers |
| Recital 92 | Broader DPIA | Assessment may go beyond individual processing to broader set |
| Recital 93 | New legislation context | DPIA in the context of adoption of new legislation |
| Recital 94 | Prior consultation details | High residual risk requires supervisory authority consultation |
| Recital 95 | Processor involvement | Processor shall assist controller in conducting DPIA |

### ICO (Information Commissioner's Office) DPIA Guidance

| Requirement | Implementation Mapping |
|-------------|----------------------|
| 9 criteria for screening (WP29) | `IRiskCriterion` implementations cover 6 of 9; users extend for remaining 3 |
| Systematic description of processing | `DPIATemplate.Sections` with required description sections |
| Assessment of necessity and proportionality | `DPIASection` with "necessity" and "proportionality" questions |
| Assessment of risks to rights and freedoms | `IRiskCriterion` evaluation → `RiskItem` collection |
| Measures to address risks | `Mitigation` records with implementation tracking |
| DPO advice documented | `DPOConsultation` record with full decision tracking |
| Review at appropriate intervals | `DPIAReviewReminderService` with configurable interval |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in DPIA |
|-----------|----------|---------------|
| `IPipelineBehavior<,>` | `Encina` core | `DPIARequiredPipelineBehavior` registration |
| `INotification` / `INotificationPublisher` | `Encina` core | `DPIAAssessmentCompleted`, `DPOConsultationRequested`, `DPIAAssessmentExpired` |
| `EncinaError` | `Encina` core | Error factory pattern via `DPIAErrors` |
| `Either<L, R>` | `Encina` core | ROP on all store/engine methods |
| `TimeProvider` | .NET 10 BCL | Testable time for review deadlines |
| `IDataProtectionOfficer` | `Encina.Compliance.GDPR` | DPO info for consultation (soft dependency) |
| `GDPROptions.DataProtectionOfficer` | `Encina.Compliance.GDPR` | Fallback DPO source |
| Pipeline behavior patterns | 7 compliance packages | Attribute caching, enforcement modes, observability |
| Health check pattern | 7 compliance packages | Scoped resolution, DefaultName, Tags |
| Auto-registration pattern | 7 compliance packages | `IHostedService` assembly scanning |
| `IOptions<T>` validation | 7 compliance packages | `IValidateOptions<DPIAOptions>` |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.GDPR` | 8100-8199 | Core, LawfulBasis, ProcessingActivity |
| `Encina.Compliance.Consent` | 8200-8299 | Consent lifecycle, audit |
| `Encina.Compliance.DataSubjectRights` | 8300-8399 | DSR lifecycle, restriction, erasure |
| `Encina.Compliance.Anonymization` | 8400-8499 | Anonymization, pseudonymization |
| `Encina.Compliance.Retention` | 8500-8599 | Retention enforcement, legal holds |
| `Encina.Compliance.DataResidency` | 8600-8699 | Residency, transfer validation |
| `Encina.Compliance.BreachNotification` | 8700-8799 | Breach detection, notification |
| **`Encina.Compliance.DPIA`** | **8900-8999** | **New — Assessment, DPO consultation, risk scoring** |

> **Note**: 8800-8899 is intentionally skipped to maintain spacing and allow future expansion of BreachNotification if needed.

### File Count Estimate

| Category | Files | Notes |
|----------|-------|-------|
| Core models & enums (Phase 1) | ~15 | Records, enums, value objects |
| Interfaces & abstractions (Phase 2) | ~10 | Store, engine, criterion, template, attribute, errors, notifications |
| Default implementations (Phase 3) | ~10 | InMemory stores, engine, template provider, 6 risk criteria |
| Persistence entities & SQL (Phase 4) | ~7 | Entities, mappers, SQL scripts (4 providers) |
| ADO.NET ×4 (Phase 5a) | ~12 | 3 files × 4 providers (store, audit store, DI) |
| Dapper ×4 (Phase 5b) | ~12 | 3 files × 4 providers |
| EF Core (Phase 5c) | ~9 | Stores, entity configs, model builder extensions, DI |
| MongoDB (Phase 5d) | ~7 | Stores, documents, DI |
| Pipeline & auto-detection (Phase 6) | ~4 | Pipeline behavior, auto-detector, auto-registration |
| Configuration & DI (Phase 7) | ~5 | Options, validator, DI extensions, review service, health check |
| ASP.NET Core (Phase 8) | ~2 | Endpoint extensions (no tests — tests in Phase 11) |
| Cross-cutting integration (Phase 9) | ~3 | GDPR integration, tenant/module support |
| Observability (Phase 10) | ~2 | Diagnostics, log messages |
| Tests (Phase 11) | ~45-55 | Unit, guard, contract, property, integration (13 providers + ASP.NET Core endpoints), load, benchmark |
| Documentation (Phase 12) | ~5 | README, feature docs, CHANGELOG, INVENTORY, PublicAPI |
| **Total** | **~140-150** | |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing Encina.Compliance.DPIA for Issue #409 — GDPR Data Protection Impact Assessment Automation (Articles 35-36).

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library for CQRS + Event Sourcing
- Pre-1.0: no backward compatibility needed, best solution always
- Railway Oriented Programming: Either<EncinaError, T> everywhere
- 8 existing compliance packages follow identical architectural patterns
- Full 13-provider persistence + InMemory (same as BreachNotification, Retention, DataResidency)

IMPLEMENTATION OVERVIEW:
New package: src/Encina.Compliance.DPIA/
References Encina (core) for Either, EncinaError, IPipelineBehavior, INotification
Soft dependency on Encina.Compliance.GDPR for IDataProtectionOfficer
Store implementations in satellite packages: ADO.NET (×4), Dapper (×4), EF Core (×4), MongoDB (×1), InMemory (built-in)

Phase 1: Core models (DPIAAssessment, DPIAResult, RiskItem, Mitigation, DPOConsultation, DPIATemplate, DPIAContext, enums)
Phase 2: Interfaces (IDPIAAssessmentEngine, IRiskCriterion, IDPIATemplateProvider, IDPIAStore, IDPIAAuditStore) + [RequiresDPIA] attribute + DPIAErrors + notifications
Phase 3: InMemory implementations (InMemory stores, DefaultDPIAAssessmentEngine, DefaultDPIATemplateProvider, 6 built-in risk criteria)
Phase 4: Persistence entities, mappers, SQL DDL scripts (4 database engines)
Phase 5: Multi-provider persistence — 13 database providers (ADO ×4, Dapper ×4, EF Core ×4, MongoDB ×1)
Phase 6: DPIARequiredPipelineBehavior + DPIAAutoDetector + auto-registration hosted service
Phase 7: DPIAOptions + validator + ServiceCollectionExtensions + DPIAReviewReminderService + health check
Phase 8: ASP.NET Core management endpoints (/api/dpia/) — MapDPIAEndpoints() minimal API
Phase 9: Cross-cutting integration (GDPR DPO, audit trail, multi-tenancy, module isolation — includes ASP.NET Core endpoint integration)
Phase 10: Observability (ActivitySource, Meter, [LoggerMessage] event IDs 8900-8999 — includes endpoint metrics 8990-8999)
Phase 11: Testing (7 types: Unit ~100, Guard ~45, Contract ~20, Property ~15, Integration ~150 (13 providers + ASP.NET Core endpoints), Load, Benchmark)
Phase 12: Documentation (README, feature docs, CHANGELOG, INVENTORY, PublicAPI)

KEY PATTERNS:
- All store/engine methods: ValueTask<Either<EncinaError, T>>
- Full 13-provider persistence: IDPIAStore and IDPIAAuditStore across ADO.NET (×4), Dapper (×4), EF Core (×4), MongoDB (×1)
- InMemory provider as default fallback for testing/development (ConcurrentDictionary, thread-safe)
- DPIAResult and DPOConsultation stored as JSON in persistence layer
- Risk scoring: max(individual criterion levels) = overall risk (conservative)
- Prior consultation (Art. 36): OverallRisk >= VeryHigh && mitigations not all implemented
- DPO consultation: first-class DPOConsultation record with decision tracking
- Pipeline behavior: static attribute caching, 3 enforcement modes (Block/Warn/Disabled)
- 6 built-in risk criteria: SystematicProfiling, SpecialCategoryData, SystematicMonitoring, AutomatedDecisionMaking, LargeScaleProcessing, VulnerableSubjects
- 7 built-in templates: profiling, special-category, public-monitoring, ai-ml, biometric, health-data, general
- Review reminders: BackgroundService, configurable interval (default 12 months)
- Health check: const DefaultName = "encina-dpia", scoped resolution
- Auto-registration: IHostedService scans [RequiresDPIA] attributes
- Event ID range: 8900-8999
- SQLite DateTime: ALWAYS use "O" format, NEVER datetime('now')
- Provider-specific SQL: TOP vs LIMIT, DATETIMEOFFSET vs TIMESTAMPTZ vs DATETIME(6)
- TenantId + ModuleId in all provider WHERE clauses
- All public APIs: XML documentation with GDPR Art. 35/36 references

REFERENCE FILES:
- BreachNotification package: src/Encina.Compliance.BreachNotification/ (closest architectural reference — same 13-provider pattern)
- Retention package: src/Encina.Compliance.Retention/ (BackgroundService, health check)
- GDPR package: src/Encina.Compliance.GDPR/ (IDataProtectionOfficer, auto-registration)
- Consent package: src/Encina.Compliance.Consent/ (attribute + pipeline behavior)
- ADO stores: src/Encina.ADO.Sqlite/Retention/ (ADO pattern)
- Dapper stores: src/Encina.Dapper.Sqlite/Retention/ (Dapper pattern)
- EF Core stores: src/Encina.EntityFrameworkCore/Retention/ (EF Core pattern)
- MongoDB stores: src/Encina.MongoDB/Retention/ (MongoDB pattern)
- Tests: tests/Encina.IntegrationTests/Compliance/BreachNotification/ (integration test patterns with Collection fixtures)
- ASP.NET Core tests: tests/Encina.IntegrationTests/AspNetCore/ (endpoint integration test patterns)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | **Caching** | ✅ Phase 6 | Pipeline behavior caches assessment lookups locally (`ConcurrentDictionary<Type, DPIAAssessment?>` with TTL); assessments are low-volume, `ICacheProvider` integration unnecessary |
| 2 | **OpenTelemetry** | ✅ Phase 10 | ActivitySource + Meter + Counter<long> + Histogram<double> for assessment, pipeline, DPO consultation, ASP.NET Core endpoints |
| 3 | **Structured Logging** | ✅ Phase 10 | `[LoggerMessage]` source generator, Event IDs 8900-8999 (including 8990-8999 for ASP.NET Core endpoints), all lifecycle events logged |
| 4 | **Health Checks** | ✅ Phase 7 | `DPIAHealthCheck` with DefaultName, checks store + engine + expired assessments |
| 5 | **Validation** | ✅ Phase 7 | `DPIAOptionsValidator : IValidateOptions<DPIAOptions>`, validates ReviewIntervalMonths, DPO config |
| 6 | **Resilience** | ⏭️ [#764](https://github.com/dlrivada/Encina/issues/764) (v0.19.0) | Database store operations (13 providers) can fail — leverages `DatabaseCircuitBreakerPipelineBehavior`, `IDatabaseHealthMonitor`, `DatabaseTransientErrorPredicate`; `DPIAReviewReminderService` loop provides natural retry for periodic checks |
| 7 | **Distributed Locks** | ⏭️ [#763](https://github.com/dlrivada/Encina/issues/763) | `DPIAReviewReminderService` needs lock for multi-instance safety (duplicate `DPIAAssessmentExpired` notifications); also covered by #717 (Leader Election) |
| 8 | **Transactions** | ✅ Phase 5 | Database stores use provider-level transactions for atomic assessment + audit operations; EF Core uses `SaveChangesAsync` (implicit transaction); ADO/Dapper use explicit `IDbTransaction` where needed |
| 9 | **Idempotency** | ❌ N/A | Assessment operations keyed by `requestTypeName` → natural upsert idempotency; duplicate assess calls produce same result |
| 10 | **Multi-Tenancy** | ✅ Phase 9 | Optional `TenantId` on `DPIAAssessment`, tenant-scoped lookup when `ITenantContext` available; all 13 providers include TenantId in WHERE clauses; ASP.NET Core endpoints resolve `ITenantContext` from HttpContext |
| 11 | **Module Isolation** | ✅ Phase 9 | Optional `ModuleId` on `DPIAAssessment`, module-scoped in modular monolith scenarios; all 13 providers include ModuleId in WHERE clauses; ASP.NET Core endpoints resolve `IModuleContext` from HttpContext |
| 12 | **Audit Trail** | ✅ Phase 9 | `IDPIAAuditStore` records all lifecycle events; `DPIAAuditEntry` tracks assessments, DPO decisions; persisted across all 13 providers; ASP.NET Core endpoints create audit entries through the engine |

> **Deferred issues created**:
> - [#763](https://github.com/dlrivada/Encina/issues/763) — Distributed Lock for DPIAReviewReminderService (linked to EPIC #758)
> - [#764](https://github.com/dlrivada/Encina/issues/764) — Resilience Policies for DPIA Database Store Operations (v0.19.0, EPIC #758 Resilience section)

---

## Prerequisites & Dependencies

### Required Prerequisites

| Prerequisite | Status | Notes |
|-------------|--------|-------|
| `Encina` core (Either, EncinaError, IPipelineBehavior, INotification) | ✅ Available | Existing core package |
| `Encina.Compliance.GDPR` (IDataProtectionOfficer) | ✅ Available | Soft dependency — DPIA works without it |

### Recommended (Not Blocking)

| Dependency | Issue | Notes |
|-----------|-------|-------|
| Processor Agreements (#410) | Open | Art. 35(2) mentions processor involvement — DPIA can reference processor agreements if available |
| Privacy by Design (#411) | Open | Art. 25 principles complement DPIA mitigations — future integration |
| AI Act Compliance (#415) | Open | AI Act mandates DPIA for high-risk AI systems — future integration for AI-specific templates |

No blocking prerequisites — DPIA can be implemented independently with optional soft references.

---

## Next Steps

1. **Review and approve this plan**
2. Publish as comment on Issue #409
3. Begin Phase 1 implementation in a new session
4. Each phase should be a self-contained commit
5. Final commit references `Fixes #409`
