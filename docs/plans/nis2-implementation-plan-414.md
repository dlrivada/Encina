# Implementation Plan: `Encina.Compliance.NIS2` — NIS2 Directive Cybersecurity Compliance

> **Issue**: [#414](https://github.com/dlrivada/Encina/issues/414)
> **Type**: Feature
> **Complexity**: Very High (8 phases, stateless rule engine, ~80 files)
> **Estimated Scope**: ~3,500–4,500 lines of production code + ~2,500–3,500 lines of tests
> **Milestone**: v0.13.0 — Security & Compliance
> **Provider Category**: None — stateless rule engine, no persistence layer

---

## Summary

Implement NIS2 Directive (EU 2022/2555) cybersecurity compliance patterns as a **stateless rule engine**. The package validates compliance posture against the 10 mandatory cybersecurity measures (Article 21), enforces MFA and encryption requirements via pipeline behaviors, assesses supply chain risk, and validates incident notification timelines (24h/72h/1mo).

**Architecture**: Stateless — no event sourcing, no database stores, no InMemory implementations. Incident persistence is delegated to the existing `Encina.Compliance.BreachNotification` module. NIS2 provides the **policy layer** (rules, validation, enforcement) while BreachNotification provides the **persistence layer** (event-sourced incident lifecycle).

**Affected packages**:
- `Encina.Compliance.NIS2` (new package)
- `Encina.AspNetCore` (optional NIS2 middleware/endpoints, deferred)
- `Encina.OpenTelemetry` (activity source + meter registration)

---

## Design Choices

<details>
<summary><strong>1. Package Placement — New <code>Encina.Compliance.NIS2</code> package</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New `Encina.Compliance.NIS2` package** | Clean separation, own domain model, dedicated observability, follows existing compliance module pattern | New NuGet package to maintain |
| **B) Extend `Encina.Compliance.GDPR`** | Single compliance package | NIS2 is a separate EU directive with different scope and requirements; would bloat GDPR |
| **C) Extend `Encina.Security`** | Security-adjacent | NIS2 is regulatory compliance, not security infrastructure |

### Chosen Option: **A — New `Encina.Compliance.NIS2` package**

### Rationale

- NIS2 is a distinct EU Directive (2022/2555), separate from GDPR (2016/679)
- Follows the established pattern: each compliance module (`Consent`, `DSR`, `DPIA`, `BreachNotification`, etc.) is its own package
- Has its own domain model (10 measures, entity types, sectors, incident timelines)
- References `Encina.Compliance.BreachNotification` for incident persistence integration
- No dependency on database providers (stateless)

</details>

<details>
<summary><strong>2. Architecture — Stateless rule engine (no persistence)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Stateless rule engine** | Simple, fast, no DB dependencies, composable with existing modules | Can't track compliance history natively |
| **B) Event-sourced compliance tracking** | Full audit trail, temporal queries | Overkill for policy checks; BreachNotification already handles incident ES |
| **C) Entity-based persistence (13 providers)** | Queryable compliance records | Massive implementation scope for stateless checks |

### Chosen Option: **A — Stateless rule engine**

### Rationale

- Per the issue owner's comment: "Architecture: Stateless rule engine. No ES migration needed. No InMemory implementations needed."
- NIS2 compliance is a policy evaluation: "Are the 10 measures satisfied?" — this is configuration + runtime checks, not persisted state
- Incident handling with persistence already exists in `Encina.Compliance.BreachNotification` (event-sourced with 72h timelines)
- Compliance decisions are audited via the existing `IAuditStore` integration (audit trail cross-cutting concern)
- Reduces scope from ~200 files (13-provider) to ~80 files (stateless)

</details>

<details>
<summary><strong>3. Incident Handling Strategy — Delegate to BreachNotification + NIS2 timeline adapter</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) NIS2 timeline adapter over BreachNotification** | Reuses existing ES incident lifecycle, adds NIS2-specific deadlines (24h/72h/1mo) | Couples to BreachNotification |
| **B) Standalone NIS2 incident management** | Full control | Duplicates BreachNotification's incident lifecycle |
| **C) Pure validation only (no incident management)** | Simplest | Misses a key NIS2 requirement (Article 23) |

### Chosen Option: **A — NIS2 timeline adapter over BreachNotification**

### Rationale

- NIS2 Article 23 requires incident reporting with specific timelines: 24h early warning, 72h notification, 1-month final report
- GDPR Article 33 already requires 72h notification — BreachNotification implements this with event-sourced lifecycle
- NIS2 extends this with an additional 24h early warning requirement and 1-month final report
- `INIS2IncidentHandler` wraps `IBreachNotificationService` and adds NIS2-specific timeline validation
- Optional integration: if BreachNotification is not registered, NIS2 incident handler works standalone with stateless timeline validation only (no persistence)

</details>

<details>
<summary><strong>4. Pipeline Behavior Design — Composite behavior with attribute routing</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Single composite `NIS2CompliancePipelineBehavior`** | One behavior handles all NIS2 checks (MFA, encryption, supply chain, critical ops) | Complexity in one class |
| **B) Separate behaviors per concern** | Clean SRP, can enable/disable individually | 4+ pipeline behaviors registered, ordering complexity |
| **C) Middleware-only (no pipeline behavior)** | ASP.NET Core integration | Only works for HTTP requests, not for domain commands |

### Chosen Option: **A — Single composite behavior with internal dispatching**

### Rationale

- Follows the pattern established by `GDPRCompliancePipelineBehavior` and `BreachDetectionPipelineBehavior`
- Uses attribute detection (`[NIS2Critical]`, `[RequireMFA]`, `[NIS2SupplyChainCheck]`) with static per-type caching
- Internal dispatch to specialized validators: `IMFAEnforcer`, `IEncryptionValidator`, `ISupplyChainSecurityValidator`
- Three enforcement modes: `Block`, `Warn`, `Disabled` (consistent with all Encina behaviors)
- Single DI registration, clean ordering

</details>

<details>
<summary><strong>5. Compliance Measure Model — Enum-based checklist with pluggable evaluators</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Enum of 10 measures + `INS2MeasureEvaluator` per measure** | Clean strategy pattern, extensible, testable per measure | Interface per measure |
| **B) Single validator with hardcoded checks** | Simple | Not extensible, hard to test individually |
| **C) JSON/YAML rule definition** | Runtime configurable | Loses type safety, harder to validate |

### Chosen Option: **A — Enum-based checklist with pluggable evaluators**

### Rationale

- `NIS2Measure` enum with 10 values maps 1:1 to Article 21 requirements
- `INIS2MeasureEvaluator` interface: `ValueTask<Either<EncinaError, NIS2MeasureResult>> EvaluateAsync(NIS2MeasureContext, CancellationToken)`
- `INIS2ComplianceValidator` aggregates all evaluators and returns a `NIS2ComplianceResult` with per-measure status
- Default evaluators for each measure check configuration + runtime state
- Users can replace/extend evaluators via DI for custom compliance logic
- Matches the issue specification: "10 mandatory measures checklist"

</details>

<details>
<summary><strong>6. Supply Chain Security — Supplier registry with risk assessment rules</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Configuration-based supplier registry + stateless risk rules** | Simple, opt-in supplier definitions, no DB | Static risk assessment |
| **B) Persisted supplier database** | Dynamic risk tracking | Requires 13 providers, massive scope |
| **C) External integration only** | Flexible | No built-in functionality |

### Chosen Option: **A — Configuration-based supplier registry + stateless risk rules**

### Rationale

- Suppliers defined in `NIS2Options.AddSupplier()` configuration (matches issue spec)
- `ISupplyChainSecurityValidator` evaluates suppliers against configured risk rules
- `SupplierRiskLevel` enum: `Low`, `Medium`, `High`, `Critical`
- Assessment checks: last assessment date, risk level, mitigation measures
- `[NIS2SupplyChainCheck("supplier-id")]` attribute triggers pipeline validation
- No persistence — supplier registry is in-memory from configuration

</details>

---

## Implementation Phases

### Phase 1: Core Models & Enums

<details>
<summary>Tasks</summary>

1. **Create project** `src/Encina.Compliance.NIS2/Encina.Compliance.NIS2.csproj`
   - Target `net10.0`, nullable enabled
   - References: `Encina` (core), optionally `Encina.Compliance.BreachNotification`

2. **`NIS2EntityType.cs`** — `Encina.Compliance.NIS2.Model`
   - Enum: `Essential`, `Important`
   - XML docs referencing NIS2 Article 3

3. **`NIS2Sector.cs`** — `Encina.Compliance.NIS2.Model`
   - Enum with 18 sectors from Annexes I & II: `Energy`, `Transport`, `Banking`, `FinancialMarketInfrastructure`, `Health`, `DrinkingWater`, `WasteWater`, `DigitalInfrastructure`, `ICTServiceManagement`, `PublicAdministration`, `Space`, `PostalAndCourier`, `WasteManagement`, `ChemicalManufacturing`, `FoodProduction`, `Manufacturing`, `DigitalProviders`, `Research`

4. **`NIS2Measure.cs`** — `Encina.Compliance.NIS2.Model`
   - Enum of 10 measures from Article 21(2): `RiskAnalysisAndSecurityPolicies`, `IncidentHandling`, `BusinessContinuity`, `SupplyChainSecurity`, `NetworkAndSystemSecurity`, `EffectivenessAssessment`, `CyberHygiene`, `Cryptography`, `HumanResourcesSecurity`, `MultiFactorAuthentication`

5. **`NIS2MeasureResult.cs`** — `Encina.Compliance.NIS2.Model`
   - Sealed record: `Measure` (NIS2Measure), `IsSatisfied` (bool), `Details` (string), `Recommendations` (IReadOnlyList<string>)

6. **`NIS2ComplianceResult.cs`** — `Encina.Compliance.NIS2.Model`
   - Sealed record: `IsCompliant` (bool), `EntityType` (NIS2EntityType), `Sector` (NIS2Sector), `MeasureResults` (IReadOnlyList<NIS2MeasureResult>), `MissingMeasures` (IReadOnlyList<NIS2Measure>), `EvaluatedAtUtc` (DateTimeOffset)
   - Computed: `CompliancePercentage`, `MissingCount`

7. **`NIS2IncidentSeverity.cs`** — `Encina.Compliance.NIS2.Model`
   - Enum: `Low`, `Medium`, `High`, `Critical`

8. **`NIS2Incident.cs`** — `Encina.Compliance.NIS2.Model`
   - Sealed record matching issue spec: `Id`, `Description`, `Severity`, `DetectedAtUtc`, `IsSignificant`, `AffectedServices`, `InitialAssessment`, `EarlyWarningAtUtc` (24h), `IncidentNotificationAtUtc` (72h), `FinalReportAtUtc` (1mo)
   - Static factory method `Create()`

9. **`NIS2NotificationPhase.cs`** — `Encina.Compliance.NIS2.Model`
   - Enum: `EarlyWarning` (24h), `IncidentNotification` (72h), `IntermediateReport` (on request), `FinalReport` (1 month)

10. **`SupplierRiskLevel.cs`** — `Encina.Compliance.NIS2.Model`
    - Enum: `Low`, `Medium`, `High`, `Critical`

11. **`SupplierInfo.cs`** — `Encina.Compliance.NIS2.Model`
    - Sealed record: `SupplierId`, `Name`, `RiskLevel`, `LastAssessmentAtUtc`, `MitigationMeasures`, `CertificationStatus`

12. **`SupplierRisk.cs`** — `Encina.Compliance.NIS2.Model`
    - Sealed record: `SupplierId`, `RiskLevel`, `RiskDescription`, `RecommendedActions`

13. **`SupplyChainAssessment.cs`** — `Encina.Compliance.NIS2.Model`
    - Sealed record: `SupplierId`, `OverallRisk`, `Risks`, `AssessedAtUtc`, `NextAssessmentDueAtUtc`

14. **`NIS2EnforcementMode.cs`** — `Encina.Compliance.NIS2.Model`
    - Enum: `Block`, `Warn`, `Disabled`

15. **`ManagementAccountabilityRecord.cs`** — `Encina.Compliance.NIS2.Model`
    - Sealed record: `ResponsiblePerson`, `Role`, `AcknowledgedAtUtc`, `ComplianceAreas`, `TrainingCompletedAtUtc`

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```
CONTEXT:
You are implementing the Encina.Compliance.NIS2 package for NIS2 Directive (EU 2022/2555) compliance.
This is a stateless rule engine — no database persistence, no event sourcing.
Project: D:\Proyectos\Encina, .NET 10 / C# 14, nullable enabled.

TASK:
Create the project and all domain model files for Phase 1.

1. Create src/Encina.Compliance.NIS2/Encina.Compliance.NIS2.csproj targeting net10.0 with nullable enabled.
   Reference Encina (core) project. Follow existing .csproj patterns from src/Encina.Compliance.BreachNotification/.

2. Create all model records and enums in src/Encina.Compliance.NIS2/Model/:
   - NIS2EntityType.cs (Essential, Important — Article 3)
   - NIS2Sector.cs (18 sectors from Annexes I & II)
   - NIS2Measure.cs (10 measures from Article 21(2))
   - NIS2MeasureResult.cs (sealed record: Measure, IsSatisfied, Details, Recommendations)
   - NIS2ComplianceResult.cs (sealed record: IsCompliant, EntityType, Sector, MeasureResults, MissingMeasures, EvaluatedAtUtc + computed properties)
   - NIS2IncidentSeverity.cs (Low, Medium, High, Critical)
   - NIS2Incident.cs (sealed record with Create() factory — matches issue spec exactly)
   - NIS2NotificationPhase.cs (EarlyWarning 24h, IncidentNotification 72h, IntermediateReport, FinalReport 1mo)
   - SupplierRiskLevel.cs (Low, Medium, High, Critical)
   - SupplierInfo.cs (sealed record: SupplierId, Name, RiskLevel, LastAssessmentAtUtc, etc.)
   - SupplierRisk.cs (sealed record: SupplierId, RiskLevel, RiskDescription, RecommendedActions)
   - SupplyChainAssessment.cs (sealed record: SupplierId, OverallRisk, Risks, AssessedAtUtc, NextAssessmentDueAtUtc)
   - NIS2EnforcementMode.cs (Block, Warn, Disabled)
   - ManagementAccountabilityRecord.cs (sealed record: ResponsiblePerson, Role, AcknowledgedAtUtc, etc.)

KEY RULES:
- All records are sealed, all with XML documentation referencing NIS2 articles
- Use DateTimeOffset (not DateTime) for timestamps with "AtUtc" suffix
- IReadOnlyList<T> for collections (never List<T> in public API)
- Static factory methods Create() where applicable
- Namespace: Encina.Compliance.NIS2.Model

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/Model/BreachRecord.cs (record pattern)
- src/Encina.Compliance.BreachNotification/Model/BreachSeverity.cs (enum pattern)
- src/Encina.Compliance.BreachNotification/Encina.Compliance.BreachNotification.csproj (project structure)
```

</details>

---

### Phase 2: Core Interfaces & Abstractions

<details>
<summary>Tasks</summary>

1. **`INIS2ComplianceValidator.cs`** — `Encina.Compliance.NIS2.Abstractions`
   - `ValidateAsync(CancellationToken)` → `ValueTask<Either<EncinaError, NIS2ComplianceResult>>`
   - `GetMissingRequirementsAsync(CancellationToken)` → `ValueTask<Either<EncinaError, IReadOnlyList<NIS2Measure>>>`

2. **`INIS2MeasureEvaluator.cs`** — `Encina.Compliance.NIS2.Abstractions`
   - `NIS2Measure Measure { get; }`
   - `EvaluateAsync(NIS2MeasureContext, CancellationToken)` → `ValueTask<Either<EncinaError, NIS2MeasureResult>>`

3. **`NIS2MeasureContext.cs`** — `Encina.Compliance.NIS2.Model`
   - Sealed record: `Options` (NIS2Options), `TimeProvider`, `ServiceProvider` (IServiceProvider)

4. **`INIS2IncidentHandler.cs`** — `Encina.Compliance.NIS2.Abstractions`
   - `ReportIncidentAsync(NIS2Incident, CancellationToken)` → `ValueTask<Either<EncinaError, Unit>>`
   - `IsWithinNotificationDeadlineAsync(NIS2Incident, NIS2NotificationPhase, CancellationToken)` → `ValueTask<Either<EncinaError, bool>>`
   - `GetNextDeadlineAsync(NIS2Incident, CancellationToken)` → `ValueTask<Either<EncinaError, (NIS2NotificationPhase Phase, DateTimeOffset Deadline)>>`

5. **`ISupplyChainSecurityValidator.cs`** — `Encina.Compliance.NIS2.Abstractions`
   - `AssessSupplierAsync(string supplierId, CancellationToken)` → `ValueTask<Either<EncinaError, SupplyChainAssessment>>`
   - `GetSupplierRisksAsync(CancellationToken)` → `ValueTask<Either<EncinaError, IReadOnlyList<SupplierRisk>>>`
   - `ValidateSupplierForOperationAsync(string supplierId, CancellationToken)` → `ValueTask<Either<EncinaError, bool>>`

6. **`IMFAEnforcer.cs`** — `Encina.Compliance.NIS2.Abstractions`
   - `IsMFAEnabledAsync(string userId, CancellationToken)` → `ValueTask<Either<EncinaError, bool>>`
   - `RequireMFAAsync<TRequest>(TRequest request, IRequestContext context, CancellationToken)` → `ValueTask<Either<EncinaError, Unit>>`

7. **`IEncryptionValidator.cs`** — `Encina.Compliance.NIS2.Abstractions`
   - `IsDataEncryptedAtRestAsync(string dataCategory, CancellationToken)` → `ValueTask<Either<EncinaError, bool>>`
   - `IsDataEncryptedInTransitAsync(string endpoint, CancellationToken)` → `ValueTask<Either<EncinaError, bool>>`
   - `ValidateEncryptionPolicyAsync(CancellationToken)` → `ValueTask<Either<EncinaError, bool>>`

8. **`NIS2Errors.cs`** — `Encina.Compliance.NIS2`
   - Static error factory class with codes: `nis2.compliance_check_failed`, `nis2.measure_not_satisfied`, `nis2.mfa_required`, `nis2.encryption_required`, `nis2.supplier_risk_high`, `nis2.deadline_exceeded`, `nis2.incident_report_failed`, `nis2.supply_chain_check_failed`, `nis2.management_accountability_missing`, `nis2.pipeline_blocked`

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```
CONTEXT:
You are implementing Encina.Compliance.NIS2 — Phase 2: Core Interfaces.
The package is a stateless rule engine for NIS2 Directive compliance.
All methods return Either<EncinaError, T> (Railway Oriented Programming).
Project: D:\Proyectos\Encina, .NET 10, C# 14, nullable enabled.

TASK:
Create all interfaces and the error factory class.

1. Create abstractions in src/Encina.Compliance.NIS2/Abstractions/:
   - INIS2ComplianceValidator.cs — aggregate compliance check (ValidateAsync, GetMissingRequirementsAsync)
   - INIS2MeasureEvaluator.cs — per-measure evaluator (Measure property, EvaluateAsync)
   - INIS2IncidentHandler.cs — incident timeline handler (ReportIncidentAsync, IsWithinNotificationDeadlineAsync, GetNextDeadlineAsync)
   - ISupplyChainSecurityValidator.cs — supply chain assessment (AssessSupplierAsync, GetSupplierRisksAsync, ValidateSupplierForOperationAsync)
   - IMFAEnforcer.cs — MFA enforcement (IsMFAEnabledAsync, RequireMFAAsync)
   - IEncryptionValidator.cs — encryption validation (AtRest, InTransit, Policy)

2. Create NIS2MeasureContext.cs in Model/ (sealed record: Options, TimeProvider, ServiceProvider)

3. Create NIS2Errors.cs in root namespace — static class with factory methods for all NIS2-specific errors.

KEY RULES:
- All async methods return ValueTask<Either<EncinaError, T>>
- CancellationToken on every async method (last parameter, default value)
- XML docs on every interface method referencing NIS2 articles
- Error codes prefixed with "nis2." (e.g., "nis2.mfa_required")
- Follow NIS2Errors pattern from BreachNotificationErrors (static factory methods returning EncinaError)

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/Abstractions/IBreachNotificationService.cs
- src/Encina.Compliance.BreachNotification/Abstractions/IBreachDetectionRule.cs
- src/Encina.Compliance.BreachNotification/BreachNotificationErrors.cs
- src/Encina/Errors/EncinaErrors.cs
```

</details>

---

### Phase 3: Attributes & Default Implementations

<details>
<summary>Tasks</summary>

1. **`NIS2CriticalAttribute.cs`** — `Encina.Compliance.NIS2.Attributes`
   - `[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]`
   - Property: `Description` (string?, optional justification)

2. **`RequireMFAAttribute.cs`** — `Encina.Compliance.NIS2.Attributes`
   - `[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]`
   - Property: `Reason` (string?, optional)

3. **`NIS2SupplyChainCheckAttribute.cs`** — `Encina.Compliance.NIS2.Attributes`
   - `[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]`
   - Constructor parameter: `supplierId` (string, required)
   - Property: `MinimumRiskLevel` (SupplierRiskLevel, default: Medium)

4. **`DefaultNIS2ComplianceValidator.cs`** — `Encina.Compliance.NIS2`
   - Implements `INIS2ComplianceValidator`
   - Constructor: `IEnumerable<INIS2MeasureEvaluator> evaluators`, `IOptions<NIS2Options> options`, `TimeProvider timeProvider`
   - Aggregates results from all registered evaluators
   - Returns `NIS2ComplianceResult` with per-measure breakdown

5. **`DefaultNIS2IncidentHandler.cs`** — `Encina.Compliance.NIS2`
   - Implements `INIS2IncidentHandler`
   - Constructor: `IOptions<NIS2Options> options`, `TimeProvider timeProvider`, `ILogger<DefaultNIS2IncidentHandler> logger`
   - Stateless timeline validation: calculates deadlines from `DetectedAtUtc`
   - Optional: if `IBreachNotificationService` is registered, delegates incident persistence

6. **`DefaultSupplyChainSecurityValidator.cs`** — `Encina.Compliance.NIS2`
   - Implements `ISupplyChainSecurityValidator`
   - Constructor: `IOptions<NIS2Options> options`, `TimeProvider timeProvider`
   - Evaluates suppliers from `NIS2Options.Suppliers` registry
   - Checks: risk level, last assessment date, certification status

7. **`DefaultMFAEnforcer.cs`** — `Encina.Compliance.NIS2`
   - Implements `IMFAEnforcer`
   - Default implementation always returns `true` (assumes MFA is handled externally)
   - Users override with their auth system integration

8. **`DefaultEncryptionValidator.cs`** — `Encina.Compliance.NIS2`
   - Implements `IEncryptionValidator`
   - Default implementation returns `true` for configured categories
   - Users override for actual encryption posture checks

9. **Measure Evaluators** — `Encina.Compliance.NIS2/Evaluators/`
   - One class per measure (10 total): `RiskAnalysisEvaluator`, `IncidentHandlingEvaluator`, `BusinessContinuityEvaluator`, `SupplyChainSecurityEvaluator`, `NetworkSecurityEvaluator`, `EffectivenessAssessmentEvaluator`, `CyberHygieneEvaluator`, `CryptographyEvaluator`, `HumanResourcesSecurityEvaluator`, `MultiFactorAuthenticationEvaluator`
   - Each checks relevant configuration + registered services
   - Returns `NIS2MeasureResult` with details and recommendations

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```
CONTEXT:
You are implementing Encina.Compliance.NIS2 — Phase 3: Attributes & Default Implementations.
The package is a stateless rule engine. Default implementations check configuration and registered services.
Phase 1 (models) and Phase 2 (interfaces) are complete.

TASK:
1. Create attributes in src/Encina.Compliance.NIS2/Attributes/:
   - NIS2CriticalAttribute.cs — marks critical infrastructure operations
   - RequireMFAAttribute.cs — requires MFA for decorated request
   - NIS2SupplyChainCheckAttribute.cs — validates supplier before processing (takes supplierId)

2. Create default implementations in src/Encina.Compliance.NIS2/:
   - DefaultNIS2ComplianceValidator.cs — aggregates all INIS2MeasureEvaluator results
   - DefaultNIS2IncidentHandler.cs — stateless timeline validation with optional BreachNotification delegation
   - DefaultSupplyChainSecurityValidator.cs — configuration-based supplier assessment
   - DefaultMFAEnforcer.cs — default pass-through (users override)
   - DefaultEncryptionValidator.cs — configuration-based validation (users override)

3. Create 10 measure evaluators in src/Encina.Compliance.NIS2/Evaluators/:
   One per NIS2Measure enum value. Each implements INIS2MeasureEvaluator.
   Each evaluator checks NIS2Options configuration for its measure area.

KEY RULES:
- Attributes: [AttributeUsage] with correct Targets, AllowMultiple, Inherited
- Default implementations use TryAdd so users can override before calling AddEncinaNIS2()
- Evaluators are registered as IEnumerable<INIS2MeasureEvaluator> (strategy pattern)
- DefaultNIS2IncidentHandler: use IServiceProvider.GetService<IBreachNotificationService>() for optional integration
- All XML documented with NIS2 article references

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/Attributes/BreachMonitoredAttribute.cs
- src/Encina.Compliance.BreachNotification/DefaultBreachNotifier.cs
- src/Encina.Compliance.BreachNotification/Detection/DefaultBreachDetector.cs
```

</details>

---

### Phase 4: Pipeline Behavior

<details>
<summary>Tasks</summary>

1. **`NIS2CompliancePipelineBehavior.cs`** — `Encina.Compliance.NIS2`
   - `IPipelineBehavior<TRequest, TResponse>` implementation
   - Static per-generic-type attribute caching (`ConcurrentDictionary<Type, NIS2AttributeInfo>`)
   - Checks for: `[NIS2Critical]`, `[RequireMFA]`, `[NIS2SupplyChainCheck]`
   - Dispatches to: `IMFAEnforcer`, `IEncryptionValidator`, `ISupplyChainSecurityValidator`
   - Three enforcement modes: `Block`, `Warn`, `Disabled`
   - Records OpenTelemetry spans and counters
   - Pre-execution checks (before calling `nextStep()`)

2. **`NIS2AttributeInfo.cs`** — `Encina.Compliance.NIS2` (internal)
   - Internal sealed record: `IsNIS2Critical`, `RequiresMFA`, `SupplyChainChecks` (list of supplier IDs), `HasAnyAttribute`
   - Static factory: `FromType(Type requestType)`

</details>

<details>
<summary>Prompt for AI Agents — Phase 4</summary>

```
CONTEXT:
You are implementing Encina.Compliance.NIS2 — Phase 4: Pipeline Behavior.
This is the core enforcement mechanism that intercepts requests and validates NIS2 compliance.
Phases 1-3 are complete (models, interfaces, attributes, default implementations).

TASK:
Create the NIS2 pipeline behavior in src/Encina.Compliance.NIS2/:

1. NIS2AttributeInfo.cs (internal sealed record):
   - IsNIS2Critical, RequiresMFA, SupplyChainChecks (IReadOnlyList<string>), HasAnyAttribute
   - Static factory FromType(Type) that reads all NIS2 attributes from a request type

2. NIS2CompliancePipelineBehavior.cs:
   - Implements IPipelineBehavior<TRequest, TResponse>
   - Static ConcurrentDictionary<Type, NIS2AttributeInfo> for per-type attribute caching
   - Constructor: IMFAEnforcer, IEncryptionValidator, ISupplyChainSecurityValidator, IOptions<NIS2Options>, ILogger, TimeProvider
   - Handle method:
     a) Check enforcement mode (Disabled → skip)
     b) Get/cache attribute info
     c) If HasAnyAttribute is false → skip
     d) If RequiresMFA → call IMFAEnforcer.RequireMFAAsync()
     e) If SupplyChainChecks not empty → call ISupplyChainSecurityValidator.ValidateSupplierForOperationAsync() for each
     f) If IsNIS2Critical → start activity span, record metrics
     g) Call nextStep() (execute handler)
     h) Return result
   - On Block mode failure: return Left<EncinaError> with NIS2Errors
   - On Warn mode failure: log warning, continue
   - Record OpenTelemetry spans and counters for all checks

KEY RULES:
- Static per-generic-type caching pattern (same as BreachDetectionPipelineBehavior)
- Pre-execution checks (validate BEFORE calling handler)
- Three enforcement modes (Block/Warn/Disabled) from NIS2Options.EnforcementMode
- Catch exceptions in enforcement, log them, apply enforcement mode logic
- Use partial class for LoggerMessage integration

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/BreachDetectionPipelineBehavior.cs (exact pattern)
- src/Encina.Compliance.GDPR/GDPRCompliancePipelineBehavior.cs (attribute caching pattern)
- src/Encina.Security.Audit/AuditPipelineBehavior.cs (pre/post execution pattern)
```

</details>

---

### Phase 5: Configuration & DI

<details>
<summary>Tasks</summary>

1. **`NIS2Options.cs`** — `Encina.Compliance.NIS2`
   - `EntityType` (NIS2EntityType, default: Essential)
   - `Sector` (NIS2Sector, required)
   - `EnforcementMode` (NIS2EnforcementMode, default: Warn)
   - `EnforceMFA` (bool, default: true)
   - `EnforceEncryption` (bool, default: true)
   - `IncidentNotificationHours` (int, default: 24 — early warning)
   - `CompetentAuthority` (string? — CSIRT/authority contact)
   - `Suppliers` (internal dictionary, populated by `AddSupplier()`)
   - `AddSupplier(string id, Action<SupplierInfo> configure)` — fluent supplier registration
   - `ManagementAccountability` (ManagementAccountabilityRecord? — Article 20)
   - `AddHealthCheck` (bool, default: false)
   - `PublishNotifications` (bool, default: true)
   - `AssembliesToScan` (HashSet<Assembly>)
   - `EncryptedDataCategories` (HashSet<string> — categories validated for encryption at rest)
   - `EncryptedEndpoints` (HashSet<string> — endpoints validated for encryption in transit)

2. **`NIS2OptionsValidator.cs`** — `Encina.Compliance.NIS2`
   - `IValidateOptions<NIS2Options>`
   - Validates: Sector is set, IncidentNotificationHours > 0, CompetentAuthority not empty if EntityType is Essential

3. **`ServiceCollectionExtensions.cs`** — `Encina.Compliance.NIS2`
   - `AddEncinaNIS2(this IServiceCollection, Action<NIS2Options>?)` → IServiceCollection
   - Registers: options, validator, all interfaces with default implementations
   - Registers all 10 measure evaluators as `INIS2MeasureEvaluator`
   - Registers pipeline behavior
   - Conditional: health check, notification integration
   - Uses `TryAdd` throughout

4. **`PublicAPI.Shipped.txt`** and **`PublicAPI.Unshipped.txt`**
   - Track all public API surface

</details>

<details>
<summary>Prompt for AI Agents — Phase 5</summary>

```
CONTEXT:
You are implementing Encina.Compliance.NIS2 — Phase 5: Configuration & DI.
Phases 1-4 are complete. Now wire everything together.

TASK:
1. Create NIS2Options.cs — configuration class matching the issue spec.
   Include AddSupplier() fluent method for supplier registration.
   All properties with XML docs referencing NIS2 articles.

2. Create NIS2OptionsValidator.cs — IValidateOptions<NIS2Options>.
   Validate Sector is set, notification hours positive, authority contact for Essential entities.

3. Create ServiceCollectionExtensions.cs — AddEncinaNIS2().
   Register ALL services with TryAdd:
   - INIS2ComplianceValidator → DefaultNIS2ComplianceValidator (Scoped)
   - INIS2IncidentHandler → DefaultNIS2IncidentHandler (Scoped)
   - ISupplyChainSecurityValidator → DefaultSupplyChainSecurityValidator (Singleton)
   - IMFAEnforcer → DefaultMFAEnforcer (Singleton)
   - IEncryptionValidator → DefaultEncryptionValidator (Singleton)
   - All 10 INIS2MeasureEvaluator implementations (Singleton)
   - NIS2CompliancePipelineBehavior (Transient, via TryAddTransient typeof)
   - TimeProvider (TryAddSingleton, TimeProvider.System)
   - IValidateOptions → NIS2OptionsValidator (Singleton)
   Conditional: health check if AddHealthCheck = true

4. Create PublicAPI.Shipped.txt (empty) and PublicAPI.Unshipped.txt (all public symbols).

KEY RULES:
- TryAdd for all registrations (allows user override before AddEncinaNIS2)
- Options pattern: Configure + IValidateOptions
- XML documentation with <example> blocks on AddEncinaNIS2
- Guard clause: ArgumentNullException.ThrowIfNull(services)

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/ServiceCollectionExtensions.cs
- src/Encina.Compliance.BreachNotification/BreachNotificationOptions.cs
- src/Encina.Compliance.BreachNotification/BreachNotificationOptionsValidator.cs
```

</details>

---

### Phase 6: Cross-Cutting Integration

<details>
<summary>Tasks</summary>

1. **Health Check** — `Encina.Compliance.NIS2/Health/NIS2ComplianceHealthCheck.cs`
   - Extends `EncinaHealthCheck`
   - `DefaultName = "encina-nis2-compliance"`
   - Tags: `["nis2", "compliance", "security", "ready"]`
   - Checks: `INIS2ComplianceValidator.ValidateAsync()` — Healthy if all 10 measures satisfied, Degraded if partial, Unhealthy if critical measures missing
   - Scoped resolution via `IServiceProvider.CreateScope()`

2. **Audit Trail Integration** — Compliance decisions published as audit events
   - Pipeline behavior records `NIS2ComplianceChecked` audit entry via `IAuditStore` (optional dependency)
   - Captures: request type, which checks ran, which passed/failed, enforcement action taken
   - Only records if `IAuditStore` is registered (graceful degradation)

3. **Multi-Tenancy Consideration** — Document as ❌ N/A per owner's comment
   - NIS2 rules are universal (not tenant-specific) per the architecture decision

</details>

<details>
<summary>Prompt for AI Agents — Phase 6</summary>

```
CONTEXT:
You are implementing Encina.Compliance.NIS2 — Phase 6: Cross-Cutting Integration.
The package integrates with health checks and audit trail.

TASK:
1. Create Health/NIS2ComplianceHealthCheck.cs:
   - Extends EncinaHealthCheck (base class from Encina core)
   - DefaultName = "encina-nis2-compliance"
   - Tags: ["nis2", "compliance", "security", "ready"]
   - Resolves INIS2ComplianceValidator via IServiceProvider.CreateScope()
   - Calls ValidateAsync() — Healthy if IsCompliant, Degraded if partial, Unhealthy if critical measures fail
   - Reports compliance percentage in health check data

2. Update NIS2CompliancePipelineBehavior to optionally record audit entries:
   - If IAuditStore is available (resolved via IServiceProvider), record compliance check results
   - Fire-and-forget: don't block pipeline for audit recording failures
   - Record: request type, enforcement mode, checks performed, results, action taken

KEY RULES:
- Health check uses scoped resolution (CreateScope pattern)
- Audit trail is optional — graceful degradation if IAuditStore not registered
- No hard dependency on Encina.Security.Audit — use IServiceProvider.GetService<IAuditStore>()
- DefaultName const and Tags static array (match existing health check pattern)

REFERENCE FILES:
- src/Encina.Security.Audit/Health/AuditStoreHealthCheck.cs
- src/Encina.Compliance.BreachNotification/Health/BreachNotificationHealthCheck.cs
- src/Encina.Security.Audit/AuditPipelineBehavior.cs (fire-and-forget audit pattern)
```

</details>

---

### Phase 7: Observability

<details>
<summary>Tasks</summary>

1. **`NIS2Diagnostics.cs`** — `Encina.Compliance.NIS2/Diagnostics/`
   - `ActivitySource`: `"Encina.Compliance.NIS2"`, version `"1.0"`
   - `Meter`: same name/version
   - **Tag constants**:
     - `nis2.measure`, `nis2.outcome`, `nis2.enforcement_mode`
     - `nis2.entity_type`, `nis2.sector`, `nis2.request_type`
     - `nis2.supplier_id`, `nis2.risk_level`
     - `nis2.notification_phase`, `nis2.incident_severity`
   - **Counters** (Counter<long>):
     - `nis2.compliance.checks.total` — compliance validation invocations
     - `nis2.pipeline.executions.total` — pipeline behavior invocations
     - `nis2.pipeline.blocked.total` — requests blocked by NIS2 enforcement
     - `nis2.mfa.checks.total` — MFA enforcement checks
     - `nis2.encryption.checks.total` — encryption validation checks
     - `nis2.supply_chain.checks.total` — supply chain validations
     - `nis2.incidents.reported.total` — incidents reported
     - `nis2.measures.satisfied.total` — measures passing (tagged by measure)
     - `nis2.measures.failed.total` — measures failing (tagged by measure)
   - **Histograms**:
     - `nis2.compliance.check.duration.ms` — compliance check duration
     - `nis2.pipeline.duration.ms` — pipeline behavior duration
   - **Activity helpers**: `StartComplianceCheck()`, `StartPipelineExecution()`, `StartIncidentReport()`, `StartSupplyChainAssessment()`

2. **`NIS2LogMessages.cs`** — `Encina.Compliance.NIS2/Diagnostics/`
   - EventId range: **9200–9299**
   - Allocation blocks:
     - `9200-9209`: Pipeline behavior (Disabled, NoAttributes, Started, Completed, Blocked, Warning, Error)
     - `9210-9219`: Compliance validation (Started, MeasureEvaluated, Completed, Failed)
     - `9220-9229`: MFA enforcement (CheckStarted, MFARequired, MFAPassed, MFAFailed)
     - `9230-9239`: Encryption validation (CheckStarted, AtRestPassed, InTransitPassed, Failed)
     - `9240-9249`: Supply chain (AssessmentStarted, SupplierChecked, RiskIdentified, Completed)
     - `9250-9259`: Incident handling (Reported, DeadlineChecked, DeadlineExceeded, PhaseNotification)
     - `9260-9269`: Health check (Completed, Degraded, Failed)
     - `9270-9279`: Management accountability (Checked, Missing, Outdated)
     - `9280-9299`: Reserved
   - All using `[LoggerMessage]` source generator

</details>

<details>
<summary>Prompt for AI Agents — Phase 7</summary>

```
CONTEXT:
You are implementing Encina.Compliance.NIS2 — Phase 7: Observability.
EventId range: 9200-9299 (confirmed no collision with existing ranges).

TASK:
1. Create Diagnostics/NIS2Diagnostics.cs:
   - ActivitySource "Encina.Compliance.NIS2" v1.0
   - Meter with same name
   - Tag constants (nis2.* prefix)
   - Counter<long> instruments for all key operations
   - Histogram instruments for duration tracking
   - Activity helper methods (StartComplianceCheck, etc.)

2. Create Diagnostics/NIS2LogMessages.cs:
   - [LoggerMessage] source generator pattern
   - EventIds 9200-9299, organized in blocks of 10
   - Pipeline behavior: 9200-9209
   - Compliance validation: 9210-9219
   - MFA enforcement: 9220-9229
   - Encryption validation: 9230-9239
   - Supply chain: 9240-9249
   - Incident handling: 9250-9259
   - Health check: 9260-9269
   - Management accountability: 9270-9279
   - Each message: Define delegate with LoggerMessage.Define<T>, extension method on ILogger

3. Integrate diagnostics into existing implementations:
   - Update NIS2CompliancePipelineBehavior to use NIS2Diagnostics spans and counters
   - Update DefaultNIS2ComplianceValidator to record compliance check metrics
   - Update DefaultNIS2IncidentHandler to record incident metrics

KEY RULES:
- LoggerMessage.Define<T> for zero-allocation logging
- Extension methods on ILogger for clean call sites
- Activity spans with RecordCompleted/RecordFailed/RecordSkipped
- Counters with dimensional tags (nis2.measure, nis2.outcome, etc.)
- Follow BreachNotification diagnostics pattern exactly

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/Diagnostics/BreachNotificationDiagnostics.cs
- src/Encina.Compliance.BreachNotification/Diagnostics/BreachNotificationLogMessages.cs
- src/Encina.Compliance.DataSubjectRights/Diagnostics/DataSubjectRightsDiagnostics.cs
```

</details>

---

### Phase 8: Testing & Documentation

<details>
<summary>Tasks</summary>

**Unit Tests** (`tests/Encina.UnitTests/Compliance/NIS2/`):
1. `NIS2ComplianceResultTests.cs` — model correctness, computed properties
2. `NIS2IncidentTests.cs` — incident creation, timeline calculations
3. `DefaultNIS2ComplianceValidatorTests.cs` — aggregation of measure evaluators
4. `DefaultNIS2IncidentHandlerTests.cs` — deadline validation, all notification phases
5. `DefaultSupplyChainSecurityValidatorTests.cs` — supplier assessment, risk detection
6. `DefaultMFAEnforcerTests.cs` — default behavior
7. `DefaultEncryptionValidatorTests.cs` — default behavior
8. `NIS2CompliancePipelineBehaviorTests.cs` — attribute caching, enforcement modes (Block/Warn/Disabled), MFA check, supply chain check, critical ops tracking
9. `NIS2OptionsValidatorTests.cs` — validation of required fields
10. `NIS2AttributeInfoTests.cs` — attribute detection from types
11. Per-evaluator tests (10 files): `RiskAnalysisEvaluatorTests.cs`, etc.
12. `NIS2ComplianceHealthCheckTests.cs` — healthy, degraded, unhealthy scenarios
13. `ServiceCollectionExtensionsTests.cs` — DI registration verification

**Guard Tests** (`tests/Encina.GuardTests/Compliance/NIS2/`):
14. `NIS2GuardTests.cs` — ArgumentNullException for all public method parameters

**Contract Tests** (`tests/Encina.ContractTests/Compliance/NIS2/`):
15. `NIS2MeasureEvaluatorContractTests.cs` — verify all 10 evaluators follow the same contract

**Property Tests** (`tests/Encina.PropertyTests/Compliance/NIS2/`):
16. `NIS2IncidentTimelinePropertyTests.cs` — FsCheck: deadline calculations always correct for any valid incident

**Documentation**:
17. `src/Encina.Compliance.NIS2/README.md` — package README
18. `docs/features/nis2-compliance.md` — feature documentation
19. `CHANGELOG.md` — add entry under Unreleased → Added
20. `ROADMAP.md` — update v0.13.0 milestone status
21. `docs/INVENTORY.md` — add new package and files
22. XML doc comments — all public APIs (already done in phases 1-7)
23. `PublicAPI.Unshipped.txt` — finalize all public symbols
24. Build verification: `dotnet build --configuration Release` → 0 errors, 0 warnings
25. Test verification: `dotnet test` → all pass

</details>

<details>
<summary>Prompt for AI Agents — Phase 8</summary>

```
CONTEXT:
You are implementing Encina.Compliance.NIS2 — Phase 8: Testing & Documentation.
All production code is complete (Phases 1-7). Now write tests and documentation.
Target: ≥85% line coverage.

TASK:
1. Create unit tests in tests/Encina.UnitTests/Compliance/NIS2/:
   - NIS2ComplianceResultTests.cs — model correctness, CompliancePercentage, MissingCount
   - NIS2IncidentTests.cs — Create factory, timeline calculations
   - DefaultNIS2ComplianceValidatorTests.cs — all measures pass, some fail, all fail scenarios
   - DefaultNIS2IncidentHandlerTests.cs — each notification phase deadline (24h, 72h, 1mo), within/exceeded
   - DefaultSupplyChainSecurityValidatorTests.cs — known supplier, unknown supplier, expired assessment
   - DefaultMFAEnforcerTests.cs — default returns true
   - DefaultEncryptionValidatorTests.cs — configured categories pass, unconfigured fail
   - NIS2CompliancePipelineBehaviorTests.cs:
     a) Disabled mode → passes through
     b) No attributes → passes through
     c) [RequireMFA] + MFA enabled → passes
     d) [RequireMFA] + MFA disabled + Block → returns error
     e) [RequireMFA] + MFA disabled + Warn → logs + passes
     f) [NIS2SupplyChainCheck] + valid supplier → passes
     g) [NIS2SupplyChainCheck] + risky supplier + Block → returns error
     h) [NIS2Critical] → records metrics
   - NIS2OptionsValidatorTests.cs — valid options, missing sector, invalid hours
   - NIS2AttributeInfoTests.cs — type with no attrs, type with all attrs, mixed
   - 10 evaluator test files (one per measure)
   - NIS2ComplianceHealthCheckTests.cs — healthy/degraded/unhealthy
   - ServiceCollectionExtensionsTests.cs — all services registered

2. Create guard tests in tests/Encina.GuardTests/Compliance/NIS2/

3. Create contract tests in tests/Encina.ContractTests/Compliance/NIS2/
   - Verify all 10 evaluators implement INIS2MeasureEvaluator correctly

4. Create property tests in tests/Encina.PropertyTests/Compliance/NIS2/
   - FsCheck: deadline calculations are monotonically increasing (24h < 72h < 1mo)

5. Documentation:
   - src/Encina.Compliance.NIS2/README.md
   - docs/features/nis2-compliance.md
   - Update CHANGELOG.md (Unreleased → Added)
   - Update ROADMAP.md
   - Update docs/INVENTORY.md

6. Build & test verification

KEY RULES:
- AAA pattern (Arrange, Act, Assert)
- Mock dependencies with NSubstitute
- Use TimeProvider.CreateFrozenTimeProvider() for deterministic time
- Descriptive test names: Method_Scenario_ExpectedResult
- No shared state between tests
- Tests in matching folder structure under tests/Encina.UnitTests/Compliance/NIS2/

REFERENCE FILES:
- tests/Encina.UnitTests/Compliance/BreachNotification/ (test patterns)
- tests/Encina.GuardTests/ (guard test structure)
- tests/Encina.ContractTests/ (contract test structure)
- tests/Encina.PropertyTests/ (property test structure)
```

</details>

---

## Research

### NIS2 Directive References

| Article | Requirement | Implementation |
|---------|------------|----------------|
| Art. 3 | Entity classification (Essential/Important) | `NIS2EntityType` enum |
| Art. 20 | Management accountability | `ManagementAccountabilityRecord` |
| Art. 21(2) | 10 mandatory cybersecurity measures | `NIS2Measure` enum + 10 evaluators |
| Art. 21(2)(a) | Risk analysis and security policies | `RiskAnalysisEvaluator` |
| Art. 21(2)(b) | Incident handling | `IncidentHandlingEvaluator` + `INIS2IncidentHandler` |
| Art. 21(2)(c) | Business continuity | `BusinessContinuityEvaluator` |
| Art. 21(2)(d) | Supply chain security | `SupplyChainSecurityEvaluator` + `ISupplyChainSecurityValidator` |
| Art. 21(2)(e) | Network/system security | `NetworkSecurityEvaluator` |
| Art. 21(2)(f) | Effectiveness assessment | `EffectivenessAssessmentEvaluator` |
| Art. 21(2)(g) | Cybersecurity hygiene and training | `CyberHygieneEvaluator` |
| Art. 21(2)(h) | Cryptography and encryption | `CryptographyEvaluator` + `IEncryptionValidator` |
| Art. 21(2)(i) | HR security and access control | `HumanResourcesSecurityEvaluator` |
| Art. 21(2)(j) | Multi-factor authentication | `MultiFactorAuthenticationEvaluator` + `IMFAEnforcer` |
| Art. 23(1) | Early warning (24h) | `INIS2IncidentHandler.IsWithinNotificationDeadlineAsync()` |
| Art. 23(2) | Incident notification (72h) | `NIS2NotificationPhase.IncidentNotification` |
| Art. 23(4) | Final report (1 month) | `NIS2NotificationPhase.FinalReport` |
| Annex I | Essential entity sectors (11) | `NIS2Sector` enum (first 11 values) |
| Annex II | Important entity sectors (7) | `NIS2Sector` enum (remaining 7 values) |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in NIS2 |
|-----------|----------|---------------|
| `IPipelineBehavior<,>` | `src/Encina/Pipeline/` | NIS2CompliancePipelineBehavior base interface |
| `Either<EncinaError, T>` | `src/Encina/Errors/` | All method return types |
| `EncinaHealthCheck` | `src/Encina/Health/` | NIS2ComplianceHealthCheck base class |
| `IBreachNotificationService` | `src/Encina.Compliance.BreachNotification/` | Optional incident persistence delegation |
| `IAuditStore` | `src/Encina.Security.Audit/` | Optional audit trail for compliance decisions |
| `TimeProvider` | .NET 10 BCL | Deterministic time for deadline calculations |
| `IOptions<T>` / `IValidateOptions<T>` | Microsoft.Extensions.Options | Configuration pattern |
| `ActivitySource` / `Meter` | System.Diagnostics | Observability pattern |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| Security | 8000–8099 | Core security |
| GDPR | 8100–8199 | GDPR compliance |
| Consent | 8200–8299 | Consent management |
| DataSubjectRights | 8300–8399 | DSR management |
| Anonymization | 8400–8499 | PII anonymization |
| Retention / CrossBorderTransfer | 8500–8599 | Data retention & transfers |
| DataResidency | 8600–8699 | Regional sovereignty |
| BreachNotification | 8700–8799 | Breach lifecycle |
| DPIA | 8800–8899 | Impact assessment |
| PrivacyByDesign / ProcessorAgreements | 8900–8999 | Privacy patterns (⚠️ collision) |
| ABAC | 9000–9199 | Access control |
| **NIS2** | **9200–9299** | **NIS2 compliance (this plan)** |

### Estimated File Count

| Category | Files | Lines (est.) |
|----------|-------|-------------|
| Models & Enums | 15 | ~600 |
| Interfaces | 7 | ~250 |
| Attributes | 3 | ~100 |
| Default Implementations | 5 | ~500 |
| Measure Evaluators | 10 | ~800 |
| Pipeline Behavior | 2 | ~350 |
| Configuration & DI | 3 | ~400 |
| Health Check | 1 | ~80 |
| Diagnostics | 2 | ~500 |
| Errors | 1 | ~150 |
| **Production Total** | **~49** | **~3,730** |
| Unit Tests | ~25 | ~2,500 |
| Guard Tests | 1 | ~200 |
| Contract Tests | 1 | ~150 |
| Property Tests | 1 | ~100 |
| Documentation | 5 | ~500 |
| **Test + Docs Total** | **~33** | **~3,450** |
| **Grand Total** | **~82** | **~7,180** |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Complete Implementation Prompt — All Phases</strong></summary>

```
PROJECT CONTEXT:
You are implementing the Encina.Compliance.NIS2 package for NIS2 Directive (EU 2022/2555) compliance.
Project: D:\Proyectos\Encina
Tech: .NET 10, C# 14, nullable enabled
Architecture: Stateless rule engine — no database persistence, no event sourcing
Pattern: Either<EncinaError, T> for all fallible operations (Railway Oriented Programming)

IMPLEMENTATION OVERVIEW:
A new NuGet package (Encina.Compliance.NIS2) that provides:
1. Domain model: 10 NIS2 measures (Art. 21), entity types, sectors, incident records
2. Interfaces: INIS2ComplianceValidator, INIS2IncidentHandler, ISupplyChainSecurityValidator, IMFAEnforcer, IEncryptionValidator
3. Default implementations: Stateless validators checking configuration
4. Pipeline behavior: NIS2CompliancePipelineBehavior with attribute routing ([NIS2Critical], [RequireMFA], [NIS2SupplyChainCheck])
5. 10 measure evaluators (strategy pattern via INIS2MeasureEvaluator)
6. Health check: NIS2ComplianceHealthCheck
7. Observability: ActivitySource + Meter + [LoggerMessage] (EventIds 9200-9299)
8. Configuration: NIS2Options with AddEncinaNIS2() DI extension
9. Optional integration with BreachNotification for incident persistence

KEY PATTERNS:
- Static per-type attribute caching in pipeline behavior (ConcurrentDictionary<Type, T>)
- TryAdd for all DI registrations (allows override before AddEncinaNIS2)
- Three enforcement modes: Block/Warn/Disabled
- Scoped resolution in health checks via IServiceProvider.CreateScope()
- Optional dependencies via IServiceProvider.GetService<T>() (IAuditStore, IBreachNotificationService)
- LoggerMessage.Define<T> for zero-allocation logging
- IValidateOptions<NIS2Options> for startup validation

REFERENCE FILES:
- src/Encina.Compliance.BreachNotification/ — closest existing compliance module pattern
- src/Encina.Compliance.GDPR/ — pipeline behavior and DI patterns
- src/Encina.Security.Audit/ — health check and audit patterns
- tests/Encina.UnitTests/Compliance/BreachNotification/ — test patterns

FILE STRUCTURE:
src/Encina.Compliance.NIS2/
├── Abstractions/ (7 interfaces)
├── Attributes/ (3 attributes)
├── Diagnostics/ (2 files: Diagnostics + LogMessages)
├── Evaluators/ (10 measure evaluators)
├── Health/ (1 health check)
├── Model/ (15 records/enums)
├── DefaultNIS2ComplianceValidator.cs
├── DefaultNIS2IncidentHandler.cs
├── DefaultSupplyChainSecurityValidator.cs
├── DefaultMFAEnforcer.cs
├── DefaultEncryptionValidator.cs
├── NIS2AttributeInfo.cs (internal)
├── NIS2CompliancePipelineBehavior.cs
├── NIS2Errors.cs
├── NIS2Options.cs
├── NIS2OptionsValidator.cs
├── ServiceCollectionExtensions.cs
├── PublicAPI.Shipped.txt
├── PublicAPI.Unshipped.txt
└── README.md

tests/Encina.UnitTests/Compliance/NIS2/ (~25 test files)
tests/Encina.GuardTests/Compliance/NIS2/ (1 file)
tests/Encina.ContractTests/Compliance/NIS2/ (1 file)
tests/Encina.PropertyTests/Compliance/NIS2/ (1 file)

CRITICAL RULES:
- .NET 10 / C# 14, nullable enabled
- All methods return Either<EncinaError, T> — no exceptions for business logic
- CancellationToken on every async method
- XML documentation on every public API (referencing NIS2 articles)
- No [Obsolete], no backward compatibility
- Error codes prefixed "nis2." (e.g., "nis2.mfa_required")
- EventId range: 9200-9299 (no collisions)
- Package is stateless — no database stores, no InMemory implementations
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | **Caching** | ✅ Included | `ICacheProvider` resolved once per validation via DI. Cache key scoped by tenant: `nis2:compliance:{tenantId}:{entityType}:{sector}`. Cache writes are **awaited** (not fire-and-forget) — compliance context requires confirmation. Both cache read/write wrapped in `NIS2ResilienceHelper`. Graceful degradation when cache not registered. |
| 2 | **OpenTelemetry** | ✅ Included | ActivitySource + Meter + counters/histograms for all compliance checks (Phase 7) |
| 3 | **Structured Logging** | ✅ Included | [LoggerMessage] source generator, EventIds 9200-9287 (Phase 7 + cross-cutting events 9280-9287) |
| 4 | **Health Checks** | ✅ Included | NIS2ComplianceHealthCheck validates all 10 measures (Phase 6) |
| 5 | **Validation** | ✅ Included | `IValidateOptions<NIS2Options>` via `NIS2OptionsValidator` — validates at startup: enum ranges (Sector, EntityType, EnforcementMode), positive IncidentNotificationHours, CompetentAuthority for Essential entities, encryption coherence (EnforceEncryption requires categories or endpoints), ExternalCallTimeout > 0, ComplianceCacheTTL ≥ 0, supplier name/risk level validity. |
| 6 | **Resilience** | ✅ Included | `NIS2ResilienceHelper` wraps ALL external calls with dual strategy: (1) `ResiliencePipelineProvider<string>` with pipeline key `"nis2-external"` (Polly v8), (2) timeout fallback via `CancellationTokenSource.CancelAfter`. Protected calls: `IKeyProvider`, `IBreachNotificationService`, `ICacheProvider`, `IProcessingActivityRegistry`. Configurable timeout via `NIS2Options.ExternalCallTimeout` (default 5s). |
| 7 | **Distributed Locks** | ❌ N/A | No shared state — stateless rule engine |
| 8 | **Transactions** | ❌ N/A | Stateless — no state changes |
| 9 | **Idempotency** | ❌ N/A | Stateless — checks are naturally idempotent |
| 10 | **Multi-Tenancy** | ✅ Included | `IRequestContext.TenantId` resolved from DI, propagated into `NIS2MeasureContext.TenantId`. Cache keys incorporate tenant ID to prevent cross-tenant cache pollution. |
| 11 | **Module Isolation** | ❌ N/A | Cross-cutting by nature — compliance rules apply across all modules |
| 12 | **Audit Trail** | ✅ Included | Optional IAuditStore integration — compliance decisions recorded if audit is configured (Phase 6). EffectivenessAssessmentEvaluator validates audit infrastructure presence. |

**Cross-cutting integrations with other Encina modules** (resolved via optional DI, all protected by `NIS2ResilienceHelper`):
- ✅ `Encina.Compliance.BreachNotification` — `DefaultNIS2IncidentHandler.ReportIncidentAsync` **awaits** forwarding to `IBreachNotificationService` (was fire-and-forget, fixed — compliance requires confirmation). Wrapped in `NIS2ResilienceHelper.ExecuteAsync`.
- ✅ `Encina.Security.Encryption` — `DefaultEncryptionValidator.ValidateEncryptionPolicyAsync` verifies `IKeyProvider` active keys AND uses the result (bug fixed: `hasActiveKey` was computed but discarded). Returns false when key provider exists but has no active key. Wrapped in `NIS2ResilienceHelper.ExecuteAsync`.
- ✅ `Encina.Security.ABAC` — `HumanResourcesSecurityEvaluator` checks `IPolicyDecisionPoint` availability for access control policy enforcement.
- ✅ `Encina.Compliance.GDPR` — `RiskAnalysisEvaluator` checks `IGDPRComplianceValidator` AND verifies actual processing activity count via `IProcessingActivityRegistry.GetAllActivitiesAsync()` (not just presence — distinguishes "N activities" vs "empty registry" vs "no registry"). Wrapped in `NIS2ResilienceHelper.ExecuteAsync`.
- ✅ `Encina.Caching` — `DefaultNIS2ComplianceValidator` caches compliance results via `ICacheProvider` with tenant-scoped keys. Single resolve per validation, awaited writes, resilience-protected.
- ✅ `Encina.Security.Audit` — `EffectivenessAssessmentEvaluator` validates `IAuditStore`/`IReadAuditStore` availability.

---

## Prerequisites & Dependencies

| Dependency | Status | Notes |
|------------|--------|-------|
| `Encina` (core) | ✅ Available | IPipelineBehavior, Either, EncinaError, EncinaHealthCheck |
| `Encina.Compliance.BreachNotification` | ✅ Available | Optional — for incident persistence delegation |
| `Encina.Security.Audit` | ✅ Available | Optional — for audit trail of compliance decisions |
| EventId range 9200-9299 | ✅ Available | No collisions (ABAC ends at ~9105) |

**No blocking prerequisites**. The package can be implemented independently.

**Note**: EventId collision detected between PrivacyByDesign (8900-8959) and ProcessorAgreements (8900-8985). This should be addressed in a separate bug fix issue but does not block NIS2 implementation.
