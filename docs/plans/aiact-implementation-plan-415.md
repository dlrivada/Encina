# Implementation Plan: `Encina.Compliance.AIAct` — EU AI Act Core Abstractions & Compliance Engine

> **Issue**: [#415](https://github.com/dlrivada/Encina/issues/415)
> **Type**: Feature
> **Complexity**: Very High (8 phases, stateless rule engine, ~50 files)
> **Estimated Scope**: ~3,500-4,500 lines of production code + ~2,500-3,500 lines of tests

---

## Summary

Implement the **foundational package** for EU AI Act (Regulation EU 2024/1689) compliance within Encina. This package provides AI system risk classification (Art. 6), prohibited use blocking (Art. 5), data quality validation framework (Art. 10), bias detection model (Art. 10.2f), human oversight enforcement (Art. 14), transparency disclosures (Art. 13/50), and an AI system registry — all enforced via a `AIActCompliancePipelineBehavior`.

This is the **base issue** for AI Act coverage. Like `Encina.Compliance.GDPR` (#402) for GDPR, this package establishes core abstractions that deeper domain-specific child issues will build upon. The full AI Act implementation spans **9 additional child issues** covering risk management lifecycle, data governance depth, human oversight persistence, technical documentation, transparency, bias detection, record-keeping, GPAI obligations, and conformity assessment.

**Affected packages**: `Encina.Compliance.AIAct` (new), `Encina` (EventIdRanges registration)

**Provider category**: None — this is a stateless compliance rule engine (no database persistence in the core package). Child issues that require persistence (e.g., human oversight decision records) will implement across all 13 database providers.

---

## Design Choices

<details>
<summary><strong>1. Package Placement — New <code>Encina.Compliance.AIAct</code> package</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New `Encina.Compliance.AIAct` package** | Clean separation, own pipeline behavior, own observability, follows GDPR/NIS2 pattern | New NuGet package |
| **B) Extend `Encina.Compliance.GDPR`** | Shared config | Wrong regulation, bloats GDPR, violates SRP |
| **C) Generic `Encina.Compliance.AIGovernance`** | Future-proof name | Doesn't match the specific regulation, confusing |

### Chosen Option: **A — New `Encina.Compliance.AIAct` package**

### Rationale

- The EU AI Act is a distinct regulation from GDPR, with its own domain model, risk levels, and enforcement requirements
- Follows the established compliance package pattern: `Encina.Compliance.GDPR`, `Encina.Compliance.NIS2`, `Encina.Compliance.AIAct`
- References `Encina` core for `IPipelineBehavior`, `Either<EncinaError, T>`, `IRequest`, `INotification`
- No dependency on `Encina.Compliance.GDPR` in the core package (though child issues may integrate with GDPR for data governance overlap)
- Satellite child issues will extend this package: `Encina.Compliance.AIAct.HumanOversight`, `Encina.Compliance.AIAct.DataGovernance`, etc.

</details>

<details>
<summary><strong>2. Risk Classification Model — In-memory registry with attribute-based classification</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) In-memory registry with attribute scanning** | Fast classification, zero DB overhead, follows GDPR RoPA pattern | Registry lost on restart (acceptable for stateless config) |
| **B) Database-backed classification store** | Persistent, queryable | Over-engineered for static config, adds 13-provider requirement to core |
| **C) External classification API** | Flexible, centralized | External dependency, latency, availability risk |

### Chosen Option: **A — In-memory registry with attribute scanning**

### Rationale

- AI system classification is typically static configuration (system X is high-risk, system Y is limited-risk)
- `IAISystemRegistry` backed by `InMemoryAISystemRegistry` — populated from `AIActOptions.RegisterAISystem()` and `[HighRiskAI]` attribute scanning at startup
- `TryAdd` registration allows users to replace with a persistent implementation if needed
- Same pattern as `InMemoryProcessingActivityRegistry` in GDPR
- Auto-registration via `IHostedService` scans assemblies for `[HighRiskAI]`, `[RequireHumanOversight]`, and `[AITransparency]` attributes

</details>

<details>
<summary><strong>3. Pipeline Behavior Strategy — Single unified behavior with multi-stage checks</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Single `AIActCompliancePipelineBehavior` with multi-stage checks** | One behavior, clear execution order, single attribute cache | Complex behavior with multiple concerns |
| **B) Separate behaviors per concern** (classification, oversight, transparency) | SRP per behavior | Multiple pipeline registrations, ordering issues, repeated attribute lookups |
| **C) Middleware chain pattern** | Composable, flexible | Non-standard for Encina, harder to test |

### Chosen Option: **A — Single unified behavior with multi-stage checks**

### Rationale

- Follows NIS2/GDPR pattern: one behavior per compliance regulation
- Execution stages within the behavior:
  1. Classify AI system risk level (from registry + attributes)
  2. Block if prohibited use case (Art. 5)
  3. Enforce human oversight for high-risk (Art. 14)
  4. Add transparency disclosures for limited-risk (Art. 13/50)
  5. Log AI decision for audit (Art. 12)
- Single `ConcurrentDictionary<Type, AIActAttributeInfo?>` caches all attribute info per request type
- Early exit if no AI Act attributes detected (zero overhead for non-AI requests)
- Enforcement mode: `Block` (reject non-compliant), `Warn` (log + continue), `Disabled` (skip)

</details>

<details>
<summary><strong>4. Attribute Design — Three purpose-specific attributes with rich metadata</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Three purpose-specific attributes** (`[HighRiskAI]`, `[RequireHumanOversight]`, `[AITransparency]`) | Clear intent, composable, matches issue spec | Three attributes instead of one |
| **B) Single `[AIAct]` attribute with flags** | One attribute, all options | Complex attribute, confusing API |
| **C) Fluent registration only (no attributes)** | No reflection | Verbose, easy to forget |

### Chosen Option: **A — Three purpose-specific attributes**

### Rationale

- Each attribute maps to a specific AI Act obligation:
  - `[HighRiskAI(Category = AISystemCategory.Employment)]` → Art. 6 classification + Art. 8-15 requirements
  - `[RequireHumanOversight(Reason = "...")]` → Art. 14 human oversight
  - `[AITransparency("This was generated by AI")]` → Art. 13/50 transparency
- Composable: a request can have multiple attributes (high-risk + oversight + transparency)
- Attribute caching in pipeline behavior eliminates runtime reflection cost
- Same pattern as GDPR (`[ProcessingActivity]`, `[ProcessesPersonalData]`, `[LawfulBasis]`)
- All attributes use `AllowMultiple = false, Inherited = true`

</details>

<details>
<summary><strong>5. Bias Detection Model — Statistical framework with pluggable thresholds</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) BiasIndicator records with pluggable thresholds** | Framework-agnostic, provides structure, users plug in ML detection | Doesn't detect bias itself |
| **B) Built-in ML bias detection** | Complete solution | Massive scope, ML dependency, outside Encina's domain |
| **C) External bias API integration** | Leverages existing tools | Vendor lock-in, external dependency |

### Chosen Option: **A — BiasIndicator records with pluggable thresholds**

### Rationale

- Encina is a data governance library, not an ML framework — it provides the **reporting and enforcement structure**, not the detection algorithms
- `BiasIndicator` record with `ProtectedAttribute`, `DisparateImpactRatio`, `ConfidenceInterval`, `ExceedsThreshold` (per @desiorac's suggestion)
- `IDataQualityValidator.DetectBiasAsync()` returns `BiasReport` — users implement with their ML tooling (ML.NET, FairLearn, etc.)
- `DefaultDataQualityValidator` provides a basic implementation that checks thresholds and generates reports
- Deeper bias detection with statistical tests deferred to child issue (AI Act Bias Detection & Fairness)

</details>

<details>
<summary><strong>6. Human Decision Record — Domain model in core, persistence in child issue</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Domain model in core, persistence in child issue** | Core stays stateless, clean separation | Human decisions not persisted until child issue |
| **B) Full persistence in core (13 providers)** | Complete solution in one issue | Massively increases scope, delays delivery |
| **C) Event-sourced decisions via Marten** | Immutable audit trail | Ties to single provider |

### Chosen Option: **A — Domain model in core, persistence in child issue**

### Rationale

- `HumanDecisionRecord` as a sealed record in core: `DecisionId`, `ReviewerId`, `ReviewedAtUtc`, `Decision`, `Rationale` (per @desiorac's Art. 14 suggestion)
- `IHumanOversightEnforcer` interface in core with `RequiresHumanReviewAsync` and `RecordHumanDecisionAsync`
- `DefaultHumanOversightEnforcer` provides in-memory enforcement (checks attribute, blocks if no human review recorded)
- Full persistence with `IHumanDecisionStore` across 13 database providers is a child issue: "AI Act Human Oversight & Decision Records"
- This keeps the core package stateless and lightweight while providing the contracts for deeper implementation

</details>

<details>
<summary><strong>7. Risk Level Reclassification — Registry mutation with audit notification</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Registry supports reclassification with domain event** | Flexible, auditable, per @desiorac's suggestion | Registry mutation must be thread-safe |
| **B) Immutable classification (register once)** | Simple, no concurrency concerns | Systems can't evolve their risk level |
| **C) Version-stamped classifications** | Full history | Over-engineered for in-memory registry |

### Chosen Option: **A — Registry supports reclassification with domain event**

### Rationale

- `IAISystemRegistry.ReclassifyAsync(systemId, newRiskLevel, reason, ct)` method
- Publishes `AISystemReclassifiedNotification` via `INotificationPublisher` for audit trail
- `InMemoryAISystemRegistry` uses `ConcurrentDictionary` for thread-safe mutation
- Reclassification reason and timestamp captured in the notification for compliance evidence
- Deeper lifecycle tracking (risk assessment history, mitigation evidence) deferred to child issue "AI Act Risk Management Lifecycle"

</details>

---

## Implementation Phases

### Phase 1: Core Models, Enums & Domain Records

> **Goal**: Establish foundational types that all other phases depend on.

<details>
<summary><strong>Tasks</strong></summary>

#### New project: `src/Encina.Compliance.AIAct/`

1. **Create project file** `Encina.Compliance.AIAct.csproj`
   - Target: `net10.0`
   - Dependencies: `Encina`, `Microsoft.Extensions.Diagnostics.HealthChecks`, `Microsoft.CodeAnalysis.PublicApiAnalyzers`
   - Enable nullable, implicit usings, XML doc generation
   - `InternalsVisibleTo`: 10 satellite providers + 6 test assemblies

2. **Enums** (`Model/` folder):
   - `AIRiskLevel` — `Prohibited`, `HighRisk`, `LimitedRisk`, `MinimalRisk` (Art. 6)
   - `AISystemCategory` — `BiometricIdentification`, `CriticalInfrastructure`, `EducationVocationalTraining`, `EmploymentWorkersManagement`, `EssentialServices`, `LawEnforcement`, `MigrationAsylumBorderControl`, `JusticeAdministration`, `GeneralPurposeAI`, `EmotionRecognition`, `SocialScoring`, `RealTimeBiometricPublic` (Arts. 5-6, Annex III)
   - `AIActEnforcementMode` — `Block`, `Warn`, `Disabled`
   - `ProhibitedPractice` — `SocialScoring`, `RealTimeBiometricPublicSpaces`, `EmotionRecognitionWorkplace`, `EmotionRecognitionEducation`, `UntargetedFacialScraping`, `PredictivePolicing`, `BiometricCategorisation`, `SublimininalManipulation` (Art. 5)
   - `TransparencyObligationType` — `AIGeneratedContent`, `DeepfakeContent`, `EmotionRecognition`, `BiometricCategorisation`, `ChatbotInteraction` (Art. 50)

3. **Domain records** (`Model/` folder):
   - `AISystemRegistration` — sealed record: `SystemId (string)`, `Name (string)`, `Category (AISystemCategory)`, `RiskLevel (AIRiskLevel)`, `Provider (string?)`, `Version (string?)`, `Description (string?)`, `RegisteredAtUtc (DateTimeOffset)`, `DeploymentContext (string?)`, `ProhibitedPractices (IReadOnlyList<ProhibitedPractice>)`
   - `DataQualityReport` — sealed record: `DatasetId (string)`, `CompletenessScore (double)`, `AccuracyScore (double)`, `ConsistencyScore (double)`, `IdentifiedGaps (IReadOnlyList<DataGap>)`, `BiasIndicators (IReadOnlyList<BiasIndicator>)`, `MeetsAIActRequirements (bool)`, `EvaluatedAtUtc (DateTimeOffset)`
   - `DataGap` — sealed record: `Category (string)`, `Description (string)`, `Severity (DataGapSeverity)`, `AffectedRecords (int?)`
   - `DataGapSeverity` — enum: `Low`, `Medium`, `High`, `Critical`
   - `BiasIndicator` — sealed record: `ProtectedAttribute (string)`, `DisparateImpactRatio (double)`, `ConfidenceInterval (double)`, `ExceedsThreshold (bool)`, `SampleSize (int?)`, `Methodology (string?)`
   - `BiasReport` — sealed record: `DatasetId (string)`, `ProtectedAttributes (IReadOnlyList<string>)`, `Indicators (IReadOnlyList<BiasIndicator>)`, `OverallFairness (bool)`, `EvaluatedAtUtc (DateTimeOffset)`
   - `HumanDecisionRecord` — sealed record: `DecisionId (Guid)`, `SystemId (string)`, `ReviewerId (string)`, `ReviewedAtUtc (DateTimeOffset)`, `Decision (string)`, `Rationale (string)`, `RequestTypeName (string?)`, `CorrelationId (string?)`
   - `TechnicalDocumentation` — sealed record: `SystemId (string)`, `Description (string)`, `DesignSpecifications (string?)`, `DataGovernancePractices (string?)`, `RiskManagementMeasures (string?)`, `AccuracyMetrics (string?)`, `RobustnessMetrics (string?)`, `HumanOversightMechanisms (string?)`, `GeneratedAtUtc (DateTimeOffset)`
   - `AIActComplianceResult` — sealed record: `SystemId (string)`, `RiskLevel (AIRiskLevel)`, `IsProhibited (bool)`, `RequiresHumanOversight (bool)`, `RequiresTransparency (bool)`, `Violations (IReadOnlyList<string>)`, `EvaluatedAtUtc (DateTimeOffset)`
   - `ReclassificationRecord` — sealed record: `SystemId (string)`, `PreviousRiskLevel (AIRiskLevel)`, `NewRiskLevel (AIRiskLevel)`, `Reason (string)`, `ReclassifiedAtUtc (DateTimeOffset)`, `ReclassifiedBy (string?)`

4. **Notification records** (`Notifications/` folder):
   - `AISystemReclassifiedNotification` — implements `INotification`: `SystemId`, `PreviousRiskLevel`, `NewRiskLevel`, `Reason`, `OccurredAtUtc`
   - `ProhibitedUseBlockedNotification` — implements `INotification`: `RequestTypeName`, `SystemId`, `Practice`, `OccurredAtUtc`
   - `HumanOversightRequiredNotification` — implements `INotification`: `RequestTypeName`, `SystemId`, `Reason`, `OccurredAtUtc`

5. **`PublicAPI.Shipped.txt`** — Empty (new package)
6. **`PublicAPI.Unshipped.txt`** — Add all public types

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of Encina.Compliance.AIAct (Issue #415).

CONTEXT:
- Encina is a .NET 10 / C# 14 library using Railway Oriented Programming (Either<EncinaError, T>)
- This is a NEW project: src/Encina.Compliance.AIAct/
- Reference existing patterns in src/Encina.Compliance.GDPR/Model/ and src/Encina.Compliance.NIS2/Model/
- All domain models are sealed records with XML documentation
- Use LanguageExt for Option<T> and Either<L, R>
- Timestamps use DateTimeOffset with AtUtc suffix convention
- Enums need XML doc mapping to EU AI Act article subsections

TASK:
Create the project file (Encina.Compliance.AIAct.csproj) and all model types listed in Phase 1 Tasks.

KEY RULES:
- Target net10.0, enable nullable, enable implicit usings
- All types are sealed records (not classes), except enums
- All public types need XML documentation with <summary>, <remarks>, and AI Act article references
- Enums document which AI Act article each value maps to
- BiasIndicator includes ConfidenceInterval (per community feedback on #415)
- HumanDecisionRecord includes Rationale (per Art. 14 requirement)
- No [Obsolete] attributes, no backward compatibility
- csproj includes InternalsVisibleTo for: Encina.ADO.Sqlite, Encina.ADO.SqlServer, Encina.ADO.PostgreSQL, Encina.ADO.MySQL, Encina.Dapper.Sqlite, Encina.Dapper.SqlServer, Encina.Dapper.PostgreSQL, Encina.Dapper.MySQL, Encina.EntityFrameworkCore, Encina.MongoDB, Encina.UnitTests, Encina.IntegrationTests, Encina.ContractTests, Encina.PropertyTests, Encina.GuardTests, Encina.BenchmarkTests
- Notification records implement INotification from Encina core

REFERENCE FILES:
- src/Encina.Compliance.GDPR/Model/LawfulBasis.cs (enum pattern)
- src/Encina.Compliance.GDPR/Model/ProcessingActivity.cs (record pattern)
- src/Encina.Compliance.GDPR/Encina.Compliance.GDPR.csproj (project structure)
- src/Encina.Compliance.NIS2/Model/ (NIS2 enum patterns with article references)
```

</details>

---

### Phase 2: Core Interfaces & Abstractions

> **Goal**: Define the contracts that the pipeline behavior and external consumers will use.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Abstractions/IAIActClassifier.cs`**
   - `ValueTask<Either<EncinaError, AIRiskLevel>> ClassifySystemAsync(string systemId, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, bool>> IsProhibitedAsync(string systemId, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, AIActComplianceResult>> EvaluateComplianceAsync(string systemId, CancellationToken ct)`

2. **`Abstractions/IAISystemRegistry.cs`**
   - `ValueTask<Either<EncinaError, AISystemRegistration>> GetSystemAsync(string systemId, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, Unit>> RegisterSystemAsync(AISystemRegistration registration, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, Unit>> ReclassifyAsync(string systemId, AIRiskLevel newLevel, string reason, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, IReadOnlyList<AISystemRegistration>>> GetSystemsByRiskLevelAsync(AIRiskLevel level, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, IReadOnlyList<AISystemRegistration>>> GetAllSystemsAsync(CancellationToken ct)`
   - `bool IsRegistered(string systemId)`

3. **`Abstractions/IDataQualityValidator.cs`**
   - `ValueTask<Either<EncinaError, DataQualityReport>> ValidateTrainingDataAsync(string datasetId, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, BiasReport>> DetectBiasAsync(string datasetId, IReadOnlyList<string> protectedAttributes, CancellationToken ct)`

4. **`Abstractions/IHumanOversightEnforcer.cs`**
   - `ValueTask<Either<EncinaError, bool>> RequiresHumanReviewAsync<TRequest>(TRequest request, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, Unit>> RecordHumanDecisionAsync(HumanDecisionRecord decision, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, bool>> HasHumanApprovalAsync(Guid decisionId, CancellationToken ct)`

5. **`Abstractions/IAIActDocumentation.cs`**
   - `ValueTask<Either<EncinaError, TechnicalDocumentation>> GenerateDocumentationAsync(string systemId, CancellationToken ct)`
   - `ValueTask<Either<EncinaError, Unit>> UpdateDocumentationAsync(string systemId, TechnicalDocumentation doc, CancellationToken ct)`

6. **`Abstractions/IAIActComplianceValidator.cs`**
   - `ValueTask<Either<EncinaError, AIActComplianceResult>> ValidateAsync<TRequest>(TRequest request, string? systemId, CancellationToken ct)`
   - Main orchestration interface called by the pipeline behavior

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of Encina.Compliance.AIAct (Issue #415).

CONTEXT:
- Phase 1 created all model types in src/Encina.Compliance.AIAct/Model/
- Now define the core abstractions (interfaces) that the pipeline behavior and consumers will use
- All methods return Either<EncinaError, T> following ROP pattern
- Use ValueTask for async methods
- Reference GDPR interfaces: IGDPRComplianceValidator, IProcessingActivityRegistry

TASK:
Create all interface files in src/Encina.Compliance.AIAct/Abstractions/.

KEY RULES:
- All interfaces have XML documentation with <summary>, <remarks>, <param>, <returns>
- All async methods accept CancellationToken as last parameter
- Return types use Either<EncinaError, T> from LanguageExt (never throw exceptions)
- ValueTask (not Task) for all async returns
- Interface names start with I
- Each interface in its own file
- Reference specific AI Act articles in XML docs

REFERENCE FILES:
- src/Encina.Compliance.GDPR/Abstractions/IGDPRComplianceValidator.cs
- src/Encina.Compliance.GDPR/Abstractions/IProcessingActivityRegistry.cs
- src/Encina.Compliance.NIS2/Abstractions/ (NIS2 interface patterns)
```

</details>

---

### Phase 3: Attributes & Auto-Registration

> **Goal**: Create the marker attributes and the startup scanning mechanism.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Attributes/HighRiskAIAttribute.cs`**
   - `[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]`
   - Properties: `Category (AISystemCategory)` (required), `SystemId (string?)`, `Provider (string?)`, `Version (string?)`, `Description (string?)`
   - XML doc references Art. 6 and Annex III

2. **`Attributes/RequireHumanOversightAttribute.cs`**
   - `[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]`
   - Properties: `Reason (string)` (required), `SystemId (string?)`
   - XML doc references Art. 14

3. **`Attributes/AITransparencyAttribute.cs`**
   - `[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]`
   - Properties: `DisclosureText (string)` (constructor parameter), `ObligationType (TransparencyObligationType)`
   - XML doc references Art. 13 and Art. 50

4. **`AIActAutoRegistrationDescriptor.cs`**
   - Sealed record: `Assemblies (IReadOnlyList<Assembly>)`
   - Passed to hosted service for attribute scanning

5. **`AIActAutoRegistrationHostedService.cs`** — `IHostedService`
   - Scans assemblies for `[HighRiskAI]`, `[RequireHumanOversight]`, `[AITransparency]` attributes
   - Registers discovered systems in `IAISystemRegistry`
   - Logs completion with counts (info) or skipped (debug)
   - Handles non-`InMemoryAISystemRegistry` gracefully

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of Encina.Compliance.AIAct (Issue #415).

CONTEXT:
- Phases 1-2 created models and interfaces in src/Encina.Compliance.AIAct/
- Now create marker attributes and the hosted service for startup assembly scanning
- Follow the GDPR pattern: ProcessingActivityAttribute + GDPRAutoRegistrationHostedService

TASK:
Create the three attribute classes and the auto-registration infrastructure.

KEY RULES:
- AttributeUsage: AllowMultiple = false, Inherited = true
- All attributes sealed
- Auto-registration hosted service scans assemblies, registers in IAISystemRegistry
- Use ILogger with structured logging (temporary — formal LogMessages in Phase 6)
- Handle non-InMemory registries gracefully (log warning if unsupported)
- Use CancellationToken properly

REFERENCE FILES:
- src/Encina.Compliance.GDPR/Attributes/ProcessingActivityAttribute.cs
- src/Encina.Compliance.GDPR/GDPRAutoRegistrationHostedService.cs
- src/Encina.Compliance.GDPR/GDPRAutoRegistrationDescriptor.cs
- src/Encina.Compliance.NIS2/Attributes/NIS2CriticalAttribute.cs
```

</details>

---

### Phase 4: Default Implementations

> **Goal**: Provide working default implementations of all core interfaces.

<details>
<summary><strong>Tasks</strong></summary>

1. **`InMemoryAISystemRegistry.cs`**
   - Implements `IAISystemRegistry`
   - Backed by `ConcurrentDictionary<string, AISystemRegistration>`
   - `ReclassifyAsync` creates `ReclassificationRecord`, publishes `AISystemReclassifiedNotification` via optional `INotificationPublisher`
   - Thread-safe reads and writes
   - `IsRegistered` is synchronous (direct dictionary lookup)

2. **`DefaultAIActClassifier.cs`**
   - Implements `IAIActClassifier`
   - Depends on `IAISystemRegistry`
   - `ClassifySystemAsync` → looks up registry, returns `RiskLevel`
   - `IsProhibitedAsync` → checks if system's `RiskLevel == Prohibited` or has prohibited practices
   - `EvaluateComplianceAsync` → builds `AIActComplianceResult` by evaluating all compliance dimensions

3. **`DefaultDataQualityValidator.cs`**
   - Implements `IDataQualityValidator`
   - `ValidateTrainingDataAsync` → returns `DataQualityReport` with default thresholds (completeness ≥ 0.9, accuracy ≥ 0.85, consistency ≥ 0.9)
   - `DetectBiasAsync` → returns `BiasReport` using configurable `DisparateImpactThreshold` (default 0.8, per EEOC four-fifths rule)
   - NOTE: Default implementation provides framework structure — users override with real ML-backed validation

4. **`DefaultHumanOversightEnforcer.cs`**
   - Implements `IHumanOversightEnforcer`
   - In-memory tracking of decisions via `ConcurrentDictionary<Guid, HumanDecisionRecord>`
   - `RequiresHumanReviewAsync` → checks `[RequireHumanOversight]` attribute on request type
   - `RecordHumanDecisionAsync` → stores decision in memory
   - `HasHumanApprovalAsync` → checks if decision exists
   - NOTE: Persistent storage in child issue "AI Act Human Oversight & Decision Records"

5. **`DefaultAIActDocumentation.cs`**
   - Implements `IAIActDocumentation`
   - `GenerateDocumentationAsync` → builds `TechnicalDocumentation` from `IAISystemRegistry` data
   - Template-based generation with placeholders for user-supplied content
   - NOTE: Rich documentation generation in child issue "AI Act Technical Documentation"

6. **`DefaultAIActComplianceValidator.cs`**
   - Implements `IAIActComplianceValidator`
   - Orchestrates: classifier → prohibited check → oversight requirement → transparency check
   - Returns `AIActComplianceResult` with all violations
   - Depends on: `IAIActClassifier`, `IAISystemRegistry`, `IHumanOversightEnforcer`, `AIActOptions`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of Encina.Compliance.AIAct (Issue #415).

CONTEXT:
- Phases 1-3 created models, interfaces, attributes, and auto-registration
- Now implement the default (in-memory) implementations of all core interfaces
- All methods return Either<EncinaError, T> — never throw for business logic
- Follow the GDPR patterns: DefaultGDPRComplianceValidator, InMemoryProcessingActivityRegistry

TASK:
Create default implementations for all 6 interfaces.

KEY RULES:
- All classes sealed
- Use ConcurrentDictionary for thread-safe in-memory storage
- ROP: return Left(error) for failures, Right(result) for successes
- DefaultDataQualityValidator provides threshold-based framework (not real ML)
- DefaultHumanOversightEnforcer tracks decisions in-memory (persistence in child issue)
- InMemoryAISystemRegistry publishes AISystemReclassifiedNotification on reclassification (inject optional INotificationPublisher)
- Use TimeProvider for timestamps (not DateTime.UtcNow directly)
- Null checks with ArgumentNullException.ThrowIfNull on all public methods

REFERENCE FILES:
- src/Encina.Compliance.GDPR/DefaultGDPRComplianceValidator.cs
- src/Encina.Compliance.GDPR/InMemoryProcessingActivityRegistry.cs
- src/Encina.Compliance.NIS2/ (default NIS2 implementations)
```

</details>

---

### Phase 5: Pipeline Behavior & Errors

> **Goal**: Implement the core compliance enforcement pipeline and error definitions.

<details>
<summary><strong>Tasks</strong></summary>

1. **`AIActErrors.cs`** — static error factory class
   - Error codes:
     - `aiact.prohibited_use` — Art. 5 violation
     - `aiact.unregistered_system` — system not in registry
     - `aiact.high_risk_without_oversight` — Art. 14 violation
     - `aiact.human_oversight_required` — oversight not recorded
     - `aiact.transparency_missing` — Art. 13/50 violation
     - `aiact.data_quality_failed` — Art. 10 violation
     - `aiact.bias_threshold_exceeded` — Art. 10.2f violation
     - `aiact.classification_failed` — classification engine error
     - `aiact.registry_error` — registry operation error
     - `aiact.reclassification_failed` — reclassification error
     - `aiact.documentation_error` — documentation generation error
   - Each method returns `EncinaError` with structured metadata: `request_type`, `system_id`, `article`, `risk_level`

2. **`AIActCompliancePipelineBehavior.cs`**
   - `sealed class AIActCompliancePipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>`
   - Dependencies: `IAIActComplianceValidator`, `IAISystemRegistry`, `IHumanOversightEnforcer`, `IOptions<AIActOptions>`, `ILogger<AIActCompliancePipelineBehavior<TRequest, TResponse>>`, `TimeProvider`
   - Static attribute cache: `ConcurrentDictionary<Type, AIActAttributeInfo?>`
   - `AIActAttributeInfo` — internal record: `HighRiskAI?`, `RequireHumanOversight?`, `AITransparency?`, `SystemId?`
   - Execution flow:
     1. Cache lookup / attribute scan (early exit if no AI Act attributes)
     2. Resolve system from registry (by attribute `SystemId` or request type name)
     3. Block if prohibited (return `Left(AIActErrors.ProhibitedUse(...))`)
     4. Enforce human oversight for high-risk (block or warn based on enforcement mode)
     5. Record transparency obligation (tag activity, log disclosure)
     6. Proceed to next handler
   - Enforcement mode from `AIActOptions.EnforcementMode`:
     - `Block` → return `Left(error)` for violations
     - `Warn` → log warning, continue
     - `Disabled` → skip all checks
   - OpenTelemetry: start activity, record counters (via Phase 6 diagnostics)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of Encina.Compliance.AIAct (Issue #415).

CONTEXT:
- Phases 1-4 created models, interfaces, attributes, and default implementations
- Now implement the pipeline behavior and error definitions
- This is the CORE enforcement mechanism — the most critical component
- Follow GDPR pipeline behavior pattern exactly

TASK:
Create AIActErrors.cs and AIActCompliancePipelineBehavior.cs.

KEY RULES:
- Pipeline behavior: sealed class, IPipelineBehavior<TRequest, TResponse>
- Static ConcurrentDictionary<Type, AIActAttributeInfo?> for attribute caching
- Early exit if no AI Act attributes (zero overhead for non-AI requests)
- Three enforcement modes: Block, Warn, Disabled
- ROP: return Either<EncinaError, TResponse>
- Errors include structured metadata with article references
- Use Stopwatch.GetTimestamp() for timing (not DateTime)
- Do NOT add diagnostics calls yet (Phase 6) — use placeholder comments
- Error codes follow pattern: "aiact.{domain}.{failure}" (e.g., "aiact.prohibited_use")

REFERENCE FILES:
- src/Encina.Compliance.GDPR/GDPRCompliancePipelineBehavior.cs (CRITICAL — follow this pattern)
- src/Encina.Compliance.GDPR/GDPRErrors.cs (error factory pattern)
- src/Encina.Compliance.NIS2/NIS2CompliancePipelineBehavior.cs (alternative reference)
```

</details>

---

### Phase 6: Configuration, DI & Observability

> **Goal**: Wire everything together with DI registration, options, and full observability.

<details>
<summary><strong>Tasks</strong></summary>

1. **`AIActOptions.cs`** — sealed class
   - `DefaultRiskAssessment (bool)` — enable risk assessment in pipeline (default: `true`)
   - `BlockProhibitedUses (bool)` — block Art. 5 violations at runtime (default: `true`)
   - `EnforceHumanOversight (bool)` — enforce Art. 14 human review (default: `true`)
   - `RequireTransparencyDisclosure (bool)` — enforce Art. 13/50 (default: `true`)
   - `BiasDetectionEnabled (bool)` — enable bias checks in data quality (default: `false`)
   - `DisparateImpactThreshold (double)` — threshold for bias detection (default: `0.8`)
   - `EnforcementMode (AIActEnforcementMode)` — `Block`, `Warn`, `Disabled` (default: `Block`)
   - `AutoRegisterFromAttributes (bool)` — scan assemblies at startup (default: `true`)
   - `AddHealthCheck (bool)` — register health check (default: `false`)
   - `AssembliesToScan (List<Assembly>)` — assemblies for attribute scanning
   - `RegisterAISystem(string systemId, Action<AISystemRegistrationBuilder> configure)` — fluent API for system registration
   - `RegisteredSystems (IReadOnlyList<AISystemRegistration>)` — systems registered via options

2. **`AISystemRegistrationBuilder.cs`** — sealed class
   - Fluent builder for `AISystemRegistration`: `Category()`, `RiskLevel()`, `Provider()`, `Version()`, `Description()`, `DeploymentContext()`
   - `Build(string systemId, TimeProvider timeProvider)` → `AISystemRegistration`

3. **`AIActOptionsValidator.cs`** — `IValidateOptions<AIActOptions>`
   - Validates: `DisparateImpactThreshold` ∈ (0, 1], at least one feature enabled, valid system registrations

4. **`ServiceCollectionExtensions.cs`**
   - `AddEncinaAIAct(this IServiceCollection services, Action<AIActOptions>? configure = null)`
   - Registers: `AIActOptions` (Configure + Validate), `TimeProvider.System`, `IAISystemRegistry` → `InMemoryAISystemRegistry` (singleton, TryAdd), `IAIActClassifier` → `DefaultAIActClassifier` (scoped, TryAdd), `IDataQualityValidator` → `DefaultDataQualityValidator` (scoped, TryAdd), `IHumanOversightEnforcer` → `DefaultHumanOversightEnforcer` (singleton, TryAdd), `IAIActDocumentation` → `DefaultAIActDocumentation` (scoped, TryAdd), `IAIActComplianceValidator` → `DefaultAIActComplianceValidator` (scoped, TryAdd), `IPipelineBehavior<,>` → `AIActCompliancePipelineBehavior<,>` (transient, TryAdd)
   - Conditional: `AIActHealthCheck` (if `options.AddHealthCheck`), `AIActAutoRegistrationHostedService` (if `options.AutoRegisterFromAttributes`)
   - Pre-registers systems from `AIActOptions.RegisteredSystems`

5. **Register EventId range** in `src/Encina/Diagnostics/EventIdRanges.cs`:
   - `public static readonly (int Min, int Max) ComplianceAIAct = (9500, 9599);`
   - Add in the "Reserved" section, converting it to "AI Act compliance"

6. **`Diagnostics/AIActDiagnostics.cs`** — internal static class
   - `ActivitySource` = `new("Encina.Compliance.AIAct", "1.0")`
   - `Meter` = `new("Encina.Compliance.AIAct", "1.0")`
   - Counters: `aiact.compliance_check.total`, `aiact.compliance_check.passed`, `aiact.compliance_check.failed`, `aiact.compliance_check.skipped`, `aiact.prohibited_blocked`, `aiact.human_oversight_required`, `aiact.transparency_disclosed`, `aiact.reclassification.total`
   - Histogram: `aiact.compliance_check.duration` (ms)
   - Tag constants: `TagRequestType`, `TagSystemId`, `TagRiskLevel`, `TagCategory`, `TagEnforcementMode`, `TagFailureReason`
   - Helper methods: `StartComplianceCheck(string requestType)`, `RecordCheckResult(Activity?, TimeSpan, TagList)`

7. **`Diagnostics/AIActLogMessages.cs`** — internal static class
   - EventId allocation: 9500-9530 (within ComplianceAIAct range 9500-9599)
   - Events:
     - 9500: `ComplianceCheckStarted` (Debug) — `RequestType={RequestType}`
     - 9501: `ComplianceCheckPassed` (Info) — `RequestType={RequestType}, RiskLevel={RiskLevel}`
     - 9502: `ComplianceCheckFailed` (Warning) — `RequestType={RequestType}, Reason={Reason}`
     - 9503: `ProhibitedUseBlocked` (Error) — `RequestType={RequestType}, SystemId={SystemId}, Practice={Practice}`
     - 9504: `HumanOversightRequired` (Warning) — `RequestType={RequestType}, SystemId={SystemId}`
     - 9505: `HumanOversightSatisfied` (Info) — `RequestType={RequestType}, ReviewerId={ReviewerId}`
     - 9506: `TransparencyDisclosed` (Info) — `RequestType={RequestType}, DisclosureText={Text}`
     - 9507: `SystemRegistered` (Info) — `SystemId={SystemId}, RiskLevel={RiskLevel}, Category={Category}`
     - 9508: `SystemReclassified` (Warning) — `SystemId={SystemId}, From={From}, To={To}, Reason={Reason}`
     - 9509: `UnregisteredSystemAccessed` (Warning) — `SystemId={SystemId}`
     - 9510: `ComplianceCheckSkipped` (Trace) — `RequestType={RequestType}` (no attributes)
     - 9511: `EnforcementWarning` (Warning) — `RequestType={RequestType}, Violation={Violation}` (warn mode)
     - 9512: `AutoRegistrationCompleted` (Info) — `SystemCount={Count}, AssemblyCount={AssemblyCount}`
     - 9513: `AutoRegistrationSkipped` (Debug) — no assemblies configured
     - 9514: `DataQualityValidationStarted` (Debug) — `DatasetId={DatasetId}`
     - 9515: `DataQualityValidationCompleted` (Info) — `DatasetId={DatasetId}, MeetsRequirements={Meets}`
     - 9516: `BiasDetected` (Warning) — `DatasetId={DatasetId}, Attribute={Attribute}, Ratio={Ratio}`
     - 9517: `HealthCheckCompleted` (Debug) — `Status={Status}`
   - Use `LoggerMessage.Define<T>()` source generator pattern (matching GDPR LogMessages)

8. **Update pipeline behavior** — Wire in `AIActDiagnostics` and `AIActLogMessages` calls

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of Encina.Compliance.AIAct (Issue #415).

CONTEXT:
- Phases 1-5 created models, interfaces, attributes, default implementations, pipeline behavior, and errors
- Now wire everything with DI, options, diagnostics, and structured logging
- Register EventId range 9500-9599 in EventIdRanges.cs
- Follow GDPR DI and diagnostics patterns exactly

TASK:
Create AIActOptions, ServiceCollectionExtensions, diagnostics, and log messages.
Update EventIdRanges.cs with the new range.
Wire diagnostics into the existing pipeline behavior.

KEY RULES:
- ServiceCollectionExtensions: use TryAdd* for all registrations (allow override)
- Always add TimeProvider.System via TryAddSingleton
- Options validated via IValidateOptions implementation
- Health check conditional on options.AddHealthCheck
- Auto-registration conditional on options.AutoRegisterFromAttributes
- EventIds 9500-9530 within ComplianceAIAct range (9500-9599)
- LoggerMessage.Define<T>() pattern (not [LoggerMessage] attribute — match GDPR style)
- ActivitySource named "Encina.Compliance.AIAct"
- Meter named "Encina.Compliance.AIAct"

REFERENCE FILES:
- src/Encina.Compliance.GDPR/ServiceCollectionExtensions.cs (DI pattern)
- src/Encina.Compliance.GDPR/GDPROptions.cs (options pattern)
- src/Encina.Compliance.GDPR/GDPROptionsValidator.cs (validation pattern)
- src/Encina.Compliance.GDPR/Diagnostics/GDPRDiagnostics.cs (observability pattern)
- src/Encina.Compliance.GDPR/Diagnostics/GDPRLogMessages.cs (structured logging pattern)
- src/Encina/Diagnostics/EventIdRanges.cs (EventId registration)
```

</details>

---

### Phase 7: Health Check

> **Goal**: Provide runtime health verification for the AI Act compliance engine.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Health/AIActHealthCheck.cs`**
   - `public sealed class AIActHealthCheck : IHealthCheck`
   - `public const string DefaultName = "encina-aiact";`
   - `private static readonly string[] DefaultTags = ["encina", "aiact", "compliance", "ready"];`
   - Checks:
     1. `AIActOptions` configured correctly
     2. `IAISystemRegistry` resolvable and has registered systems
     3. `IAIActClassifier` registered
     4. `IHumanOversightEnforcer` registered
     5. At least one feature enabled (`BlockProhibitedUses || EnforceHumanOversight || RequireTransparencyDisclosure`)
   - Returns `Healthy`, `Degraded` (no systems registered), or `Unhealthy` (critical failure)
   - Uses scoped resolution via `IServiceProvider.CreateScope()`
   - Records metadata: system count, risk level distribution, enforcement mode

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of Encina.Compliance.AIAct (Issue #415).

CONTEXT:
- All core components exist. Now add health check.
- Follow the GDPR health check pattern exactly.

TASK:
Create Health/AIActHealthCheck.cs.

KEY RULES:
- DefaultName const + DefaultTags static array
- Use IServiceProvider.CreateScope() for scoped service resolution
- Check: options, registry, classifier, enforcer, at least one feature enabled
- Return Healthy/Degraded/Unhealthy with metadata dictionary
- Log completion via AIActLogMessages.HealthCheckCompleted

REFERENCE FILES:
- src/Encina.Compliance.GDPR/Health/GDPRHealthCheck.cs
- src/Encina.Compliance.NIS2/Health/ (NIS2 health check)
```

</details>

---

### Phase 8: Testing & Documentation

> **Goal**: Comprehensive test coverage and documentation.

<details>
<summary><strong>Tasks</strong></summary>

#### Unit Tests (`tests/Encina.UnitTests/Compliance/AIAct/`)

1. **`Model/AIRiskLevelTests.cs`** — enum value coverage
2. **`Model/AISystemCategoryTests.cs`** — enum value coverage
3. **`Model/BiasIndicatorTests.cs`** — record equality, threshold checks
4. **`Model/HumanDecisionRecordTests.cs`** — record creation, rationale requirement
5. **`Model/DataQualityReportTests.cs`** — MeetsAIActRequirements logic
6. **`InMemoryAISystemRegistryTests.cs`** — register, get, reclassify, thread safety
7. **`DefaultAIActClassifierTests.cs`** — classify, prohibited check, compliance evaluation
8. **`DefaultDataQualityValidatorTests.cs`** — validation, bias detection, thresholds
9. **`DefaultHumanOversightEnforcerTests.cs`** — require review, record decision, approval check
10. **`DefaultAIActComplianceValidatorTests.cs`** — orchestration, all paths
11. **`AIActCompliancePipelineBehaviorTests.cs`** — attribute caching, enforcement modes, early exit, prohibited blocking
12. **`AIActErrorsTests.cs`** — all error factory methods, metadata verification
13. **`AIActOptionsValidatorTests.cs`** — valid/invalid options, threshold validation
14. **`ServiceCollectionExtensionsTests.cs`** — registration, TryAdd override, health check toggle
15. **`AIActAutoRegistrationHostedServiceTests.cs`** — assembly scanning, registration
16. **`Attributes/HighRiskAIAttributeTests.cs`** — attribute construction
17. **`Attributes/RequireHumanOversightAttributeTests.cs`** — attribute construction
18. **`Attributes/AITransparencyAttributeTests.cs`** — attribute construction
19. **`Health/AIActHealthCheckTests.cs`** — healthy, degraded, unhealthy paths

#### Guard Tests (`tests/Encina.GuardTests/Compliance/AIAct/`)

20. **`InMemoryAISystemRegistryGuardTests.cs`** — null arguments on all public methods
21. **`DefaultAIActClassifierGuardTests.cs`** — null arguments
22. **`DefaultDataQualityValidatorGuardTests.cs`** — null arguments
23. **`DefaultHumanOversightEnforcerGuardTests.cs`** — null arguments
24. **`DefaultAIActComplianceValidatorGuardTests.cs`** — null arguments
25. **`AIActCompliancePipelineBehaviorGuardTests.cs`** — null constructor args

#### Contract Tests (`tests/Encina.ContractTests/Compliance/AIAct/`)

26. **`IAISystemRegistryContractTests.cs`** — verify all implementations follow registry contract
27. **`IAIActClassifierContractTests.cs`** — classifier contract
28. **`IHumanOversightEnforcerContractTests.cs`** — enforcer contract

#### Property Tests (`tests/Encina.PropertyTests/Compliance/AIAct/`)

29. **`BiasIndicatorPropertyTests.cs`** — FsCheck: DisparateImpactRatio always > 0, ConfidenceInterval in (0,1]
30. **`DataQualityReportPropertyTests.cs`** — scores always in [0,1], MeetsRequirements consistent with scores

#### Load/Benchmark Tests

31. **`tests/Encina.BenchmarkTests/Encina.Benchmarks/Compliance/AIAct/AIAct-Benchmarks.md`** — justification (pipeline checks are ~μs, not a hot path)
32. **`tests/Encina.LoadTests/Compliance/AIAct/AIAct-LoadTests.md`** — justification (stateless validation, no concurrency concerns)

#### Documentation

33. **Update `CHANGELOG.md`** — add `Encina.Compliance.AIAct` entry under `### Added` in Unreleased
34. **Update `ROADMAP.md`** — mark AI Act core as implemented
35. **Create `src/Encina.Compliance.AIAct/README.md`** — package README with usage examples
36. **Update `docs/INVENTORY.md`** — add new package and files
37. **Update `PublicAPI.Unshipped.txt`** — ensure all public symbols tracked
38. **Build verification**: `dotnet build --configuration Release` → 0 errors, 0 warnings
39. **Test verification**: `dotnet test` → all pass

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 of Encina.Compliance.AIAct (Issue #415).

CONTEXT:
- All production code is complete in src/Encina.Compliance.AIAct/
- Now create comprehensive tests and documentation
- Tests follow the consolidated test organization pattern
- Target ≥85% line coverage

TASK:
Create all test files listed in Phase 8 Tasks, plus documentation updates.

KEY RULES:
- Unit tests: AAA pattern, descriptive names, one assert per test, mock dependencies
- Guard tests: verify ArgumentNullException for all public method parameters
- Contract tests: verify all implementations of an interface follow the same contract
- Property tests: FsCheck generators for domain invariants
- Benchmark/Load: .md justification files (stateless engine, not a hot path)
- Test file location: tests/Encina.UnitTests/Compliance/AIAct/
- No [Collection] fixtures needed (no database persistence in this package)
- Mock IAISystemRegistry, IAIActClassifier, etc. with NSubstitute or Moq (match project convention)

REFERENCE FILES:
- tests/Encina.UnitTests/Compliance/GDPR/ (all GDPR test patterns)
- tests/Encina.GuardTests/Compliance/GDPR/ (guard test patterns)
- tests/Encina.ContractTests/Compliance/GDPR/ (contract test patterns)
- tests/Encina.PropertyTests/Compliance/GDPR/ (property test patterns)
```

</details>

---

## Research

### EU AI Act Key Articles Covered by This Package

| Article | Topic | Coverage in #415 |
|---------|-------|-----------------|
| Art. 5 | Prohibited AI Practices | `ProhibitedPractice` enum, runtime blocking in pipeline |
| Art. 6 | Classification of High-Risk AI | `AIRiskLevel`, `AISystemCategory`, `IAIActClassifier` |
| Art. 10 | Data and Data Governance | `IDataQualityValidator`, `DataQualityReport`, `BiasIndicator` |
| Art. 11 | Technical Documentation | `IAIActDocumentation`, `TechnicalDocumentation` model |
| Art. 12 | Record-Keeping | Structured logging via `AIActLogMessages` |
| Art. 13 | Transparency (High-Risk) | `[AITransparency]` attribute, pipeline disclosure |
| Art. 14 | Human Oversight | `IHumanOversightEnforcer`, `[RequireHumanOversight]` |
| Art. 50 | Transparency (All AI) | `TransparencyObligationType` enum, disclosure pipeline |

### EU AI Act Articles Deferred to Child Issues

| Article | Topic | Child Issue |
|---------|-------|------------|
| Art. 9 | Risk Management System | AI Act Risk Management Lifecycle |
| Art. 10 (deep) | Data Governance (full) | AI Act Data Governance & Quality |
| Art. 10.2f | Bias Detection (statistical) | AI Act Bias Detection & Fairness |
| Art. 14 (persistent) | Human Oversight Records | AI Act Human Oversight & Decision Records |
| Art. 11 (full) | Technical Documentation (generation) | AI Act Technical Documentation |
| Art. 13 (deep) | Transparency (full obligations) | AI Act Transparency & Disclosure |
| Art. 12 (persistent) | Record-Keeping (persistent) | AI Act Record-Keeping & Automatic Logging |
| Arts. 51-56 | General-Purpose AI Models | AI Act GPAI Model Governance |
| Art. 43 | Conformity Assessment | AI Act Conformity Assessment Support |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in AI Act |
|-----------|----------|----------------|
| `IPipelineBehavior<,>` | `src/Encina/` | Pipeline behavior base |
| `Either<EncinaError, T>` | `src/Encina/` (via LanguageExt) | ROP error handling |
| `INotification` / `INotificationPublisher` | `src/Encina/` | Reclassification events |
| `EventIdRanges` | `src/Encina/Diagnostics/` | EventId registration |
| `EncinaErrors.Create()` | `src/Encina/` | Error factory |
| GDPR compliance patterns | `src/Encina.Compliance.GDPR/` | Architecture reference |
| NIS2 compliance patterns | `src/Encina.Compliance.NIS2/` | Architecture reference |
| Health check patterns | `src/Encina.Compliance.GDPR/Health/` | Health check template |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.AIAct` | 9500-9599 | Core AI Act compliance |

Sub-allocation within 9500-9599:

| Sub-range | Purpose |
|-----------|---------|
| 9500-9510 | Pipeline behavior (check started/passed/failed, prohibited blocked, skipped) |
| 9511-9517 | Features (oversight, transparency, registration, reclassification, data quality, bias, health) |
| 9518-9530 | Reserved for core extensions |
| 9531-9599 | Reserved for child packages (if they share the range) |

### Estimated File Count

| Category | Count |
|----------|-------|
| Production code (src/) | ~25 files |
| Unit tests | ~19 files |
| Guard tests | ~6 files |
| Contract tests | ~3 files |
| Property tests | ~2 files |
| Justification docs | ~2 files |
| Documentation | ~4 files |
| **Total** | **~61 files** |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt (All Phases)</strong></summary>

```
You are implementing Encina.Compliance.AIAct — EU AI Act Core Abstractions & Compliance Engine (Issue #415).

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 data governance library
- Pre-1.0: choose the best solution, not the compatible one
- Railway Oriented Programming: Either<EncinaError, T> on all store/handler methods
- No [Obsolete], no backward compatibility, no migration helpers
- All public APIs need XML documentation
- Nullable reference types enabled everywhere

IMPLEMENTATION OVERVIEW:
Create new package src/Encina.Compliance.AIAct/ with:

1. MODELS (Model/ folder):
   - Enums: AIRiskLevel, AISystemCategory, AIActEnforcementMode, ProhibitedPractice, TransparencyObligationType, DataGapSeverity
   - Records: AISystemRegistration, DataQualityReport, DataGap, BiasIndicator, BiasReport, HumanDecisionRecord, TechnicalDocumentation, AIActComplianceResult, ReclassificationRecord
   - Notifications: AISystemReclassifiedNotification, ProhibitedUseBlockedNotification, HumanOversightRequiredNotification

2. INTERFACES (Abstractions/ folder):
   - IAIActClassifier, IAISystemRegistry, IDataQualityValidator, IHumanOversightEnforcer, IAIActDocumentation, IAIActComplianceValidator

3. ATTRIBUTES (Attributes/ folder):
   - HighRiskAIAttribute, RequireHumanOversightAttribute, AITransparencyAttribute

4. DEFAULT IMPLEMENTATIONS (root):
   - InMemoryAISystemRegistry, DefaultAIActClassifier, DefaultDataQualityValidator, DefaultHumanOversightEnforcer, DefaultAIActDocumentation, DefaultAIActComplianceValidator

5. PIPELINE & ERRORS (root):
   - AIActCompliancePipelineBehavior<TRequest, TResponse>, AIActErrors

6. CONFIGURATION & DI (root):
   - AIActOptions, AISystemRegistrationBuilder, AIActOptionsValidator, ServiceCollectionExtensions

7. DIAGNOSTICS (Diagnostics/ folder):
   - AIActDiagnostics (ActivitySource + Meter), AIActLogMessages (EventIds 9500-9530)

8. HEALTH (Health/ folder):
   - AIActHealthCheck

9. AUTO-REGISTRATION (root):
   - AIActAutoRegistrationDescriptor, AIActAutoRegistrationHostedService

10. EventIdRanges.cs UPDATE:
    - Add ComplianceAIAct = (9500, 9599) in src/Encina/Diagnostics/EventIdRanges.cs

KEY PATTERNS TO FOLLOW:
- Pipeline behavior: static ConcurrentDictionary<Type, AttributeInfo?> for attribute caching
- Early exit if no attributes (zero overhead for non-AI requests)
- DI: TryAdd* for all registrations, allow override
- Options: sealed class with IValidateOptions validator
- Errors: static factory methods returning EncinaError with metadata
- Health check: DefaultName const, DefaultTags static array, scoped resolution
- Diagnostics: ActivitySource + Meter + LoggerMessage.Define
- Auto-registration: IHostedService scanning assemblies for attributes

REFERENCE FILES:
- src/Encina.Compliance.GDPR/ (primary architecture reference)
- src/Encina.Compliance.NIS2/ (secondary reference)
- src/Encina/Diagnostics/EventIdRanges.cs (EventId registration)
- tests/Encina.UnitTests/Compliance/GDPR/ (test patterns)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | **Caching** | ❌ N/A | Stateless rule evaluation; classification results are deterministic from registry config — no benefit from caching |
| 2 | **OpenTelemetry** | ✅ Included | `ActivitySource` + `Meter` with counters for classification, prohibited blocking, oversight, transparency; histogram for check duration |
| 3 | **Structured Logging** | ✅ Included | `AIActLogMessages` with EventIds 9500-9530; all compliance decisions logged with structured context |
| 4 | **Health Checks** | ✅ Included | `AIActHealthCheck` verifying registry, classifier, enforcer, and options configuration |
| 5 | **Validation** | ✅ Included | `AIActOptionsValidator` (IValidateOptions); pipeline validates AI system compliance at runtime |
| 6 | **Resilience** | ❌ N/A | No external service calls in core package (stateless, in-memory); child issues with external APIs may need resilience |
| 7 | **Distributed Locks** | ❌ N/A | No shared mutable state across instances; registry is configuration-driven, not runtime-mutated across nodes |
| 8 | **Transactions** | ❌ N/A | Stateless compliance checks; no database writes in core package |
| 9 | **Idempotency** | ❌ N/A | Compliance checks are deterministic read-only evaluations |
| 10 | **Multi-Tenancy** | ⏭️ Deferred | Multi-tenant AI Act enforcement (different jurisdictions may have different rules) — relevant for child issue when persistence is added |
| 11 | **Module Isolation** | ⏭️ Deferred | Module-scoped AI Act rules — relevant when modular monolith support is added |
| 12 | **Audit Trail** | ✅ Included | Pipeline behavior publishes `ProhibitedUseBlockedNotification` and `HumanOversightRequiredNotification` via `INotificationPublisher`; reclassification events published for audit; structured logging provides operational audit; persistent audit trail deferred to child issue |

**Deferred items**:
- Multi-Tenancy (⏭️): [#845](https://github.com/dlrivada/Encina/issues/845) — Tenant-scoped AI system classification and enforcement
- Module Isolation (⏭️): [#846](https://github.com/dlrivada/Encina/issues/846) — ModuleId-scoped AI Act enforcement

---

## Child Issues (Full AI Act Coverage)

> These issues will be created to complement #415 and provide comprehensive AI Act coverage. Each follows the same pattern as GDPR child issues (#403-#412).

### 1. AI Act Risk Management System (Art. 9) — [#836](https://github.com/dlrivada/Encina/issues/836)

**Scope**: Risk assessment lifecycle, mitigation tracking, continuous monitoring, residual risk documentation
**Package**: `Encina.Compliance.AIAct.RiskManagement` or extension within core
**Dependencies**: #415 (core AI Act)
**Complexity**: High

### 2. AI Act Data Governance & Quality (Art. 10) — [#837](https://github.com/dlrivada/Encina/issues/837)

**Scope**: Deep data quality framework, dataset metadata management, training data lineage, data collection process validation, gap analysis
**Package**: `Encina.Compliance.AIAct.DataGovernance`
**Dependencies**: #415 (core AI Act)
**Complexity**: Very High

### 3. AI Act Bias Detection & Fairness Metrics (Art. 10.2f) — [#838](https://github.com/dlrivada/Encina/issues/838)

**Scope**: Statistical bias analysis (disparate impact, equalized odds, demographic parity), fairness metrics framework, protected attribute management, pluggable ML backend integration
**Package**: `Encina.Compliance.AIAct.BiasDetection`
**Dependencies**: #415 (core AI Act), #837 (DataGovernance)
**Complexity**: High

### 4. AI Act Human Oversight & Decision Records (Art. 14) — [#839](https://github.com/dlrivada/Encina/issues/839)

**Scope**: `IHumanDecisionStore` with persistence across 13 database providers, override tracking, escalation workflows, review queue management
**Package**: `Encina.Compliance.AIAct.HumanOversight`
**Dependencies**: #415 (core AI Act)
**Complexity**: Very High (13 providers)

### 5. AI Act Technical Documentation Generation (Art. 11) — [#840](https://github.com/dlrivada/Encina/issues/840)

**Scope**: Structured document generation from registry and runtime data, template system, conformity evidence collection, export to standard formats
**Package**: `Encina.Compliance.AIAct.TechnicalDocumentation`
**Dependencies**: #415 (core AI Act), #836, #837, #839
**Complexity**: High

### 6. AI Act Transparency & Disclosure Obligations (Art. 13 + Art. 50) — [#841](https://github.com/dlrivada/Encina/issues/841)

**Scope**: Deep transparency framework, AI-generated content marking, deepfake disclosure, chatbot interaction notification, emotion recognition disclosure, biometric categorization notification
**Package**: Extension within core or `Encina.Compliance.AIAct.Transparency`
**Dependencies**: #415 (core AI Act)
**Complexity**: Medium

### 7. AI Act Record-Keeping & Automatic Logging (Art. 12) — [#842](https://github.com/dlrivada/Encina/issues/842)

**Scope**: Automatic event capture for all AI system operations, log retention policies, structured audit trail persistence, conformity assessment log generation
**Package**: Extension within core (leverages existing structured logging + audit infrastructure)
**Dependencies**: #415 (core AI Act), #573 (Read Auditing)
**Complexity**: Medium

### 8. AI Act General-Purpose AI Model Governance (Arts. 51-56) — [#843](https://github.com/dlrivada/Encina/issues/843)

**Scope**: GPAI model classification, systemic risk assessment, model cards, copyright compliance, transparency for GPAI
**Package**: `Encina.Compliance.AIAct.GPAI`
**Dependencies**: #415 (core AI Act)
**Complexity**: High

### 9. AI Act Conformity Assessment Support (Art. 43) — [#844](https://github.com/dlrivada/Encina/issues/844)

**Scope**: Self-assessment workflow engine, evidence collection framework, third-party assessment preparation, EU declaration of conformity template
**Package**: `Encina.Compliance.AIAct.ConformityAssessment`
**Dependencies**: #415 (core AI Act), #840 (TechnicalDocumentation), #803 (Attestation)
**Complexity**: High
