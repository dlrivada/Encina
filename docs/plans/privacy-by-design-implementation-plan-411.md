# Implementation Plan: `Encina.Compliance.PrivacyByDesign` — Privacy by Design Enforcement (Art. 25)

> **Issue**: [#411](https://github.com/dlrivada/Encina/issues/411)
> **Type**: Feature
> **Complexity**: Medium (9 phases, no database providers, ~50-60 files)
> **Estimated Scope**: ~2,800-3,800 lines of production code + ~2,000-2,500 lines of tests

---

## Summary

Implement Privacy by Design and by Default enforcement covering GDPR Article 25 and Recital 78. This package provides declarative data minimization validation, purpose limitation enforcement, and default privacy settings through a pipeline behavior that analyzes request types at compile-time (via attributes) and enforces policies at runtime.

The implementation is **provider-independent** — Privacy by Design is a stateless validation concern that operates on request metadata (attributes, field analysis), not on persisted state. No database providers are needed. The package follows the same cross-cutting pipeline behavior architecture established by `Encina.Compliance.DPIA` and `Encina.Compliance.ProcessorAgreements`.

**Affected packages**:
- `Encina.Compliance.PrivacyByDesign` (new)
- References: `Encina` (core), `Encina.Compliance.GDPR` (shared types)

**Provider category**: None — stateless validation, no persistence required.

---

## Design Choices

<details>
<summary><strong>1. Package Placement — New <code>Encina.Compliance.PrivacyByDesign</code> package</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New `Encina.Compliance.PrivacyByDesign` package** | Clean separation, own pipeline behavior, own observability, independent versioning | New NuGet package |
| **B) Extend `Encina.Compliance.GDPR`** | Single package, shared config | Bloats GDPR core, Art. 25 is a distinct enforcement concern |
| **C) Add to `Encina.Compliance.DPIA`** | Related to risk assessment | Different scope — DPIA is assessment, PbD is enforcement |

### Chosen Option: **A — New `Encina.Compliance.PrivacyByDesign` package**

### Rationale

- Art. 25 is a standalone enforcement obligation distinct from DPIA (Art. 35) and processing activity tracking
- Follows established pattern: each GDPR article group gets its own package (Consent = Art. 7, DPIA = Art. 35, DSR = Arts. 15-22)
- Privacy by Design has its own domain model (minimization analysis, purpose rules, default settings) that warrants separation
- References `Encina.Compliance.GDPR` for shared types but doesn't depend on any store infrastructure

</details>

<details>
<summary><strong>2. Validation Model — Attribute-based with reflection caching and configurable purpose rules</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Attributes + configurable purpose rules** | Declarative at field level, purpose rules configurable via options, cached reflection | Two complementary mechanisms to learn |
| **B) Attributes only** | Simple, compile-time visible | Cannot define purpose-specific allowed fields at runtime |
| **C) Fluent API registration only** | Full runtime control | Verbose, easy to forget fields, no compile-time visibility |
| **D) Source generator** | Zero reflection, compile-time analysis | Significantly more complex, limited to compile-time info |

### Chosen Option: **A — Attributes + configurable purpose rules**

### Rationale

- Three complementary attributes:
  - `[NotStrictlyNecessary(Reason = "...")]` — marks fields not essential for the declared purpose
  - `[PurposeLimitation("billing")]` — limits field usage to a specific declared purpose
  - `[PrivacyDefault(false)]` — declares the privacy-respecting default value for opt-in fields
- Purpose rules configured via `PrivacyByDesignOptions.AddPurpose("billing", p => p.AllowedFields = [...])` for runtime validation
- `[EnforceDataMinimization]` on the request class activates per-request analysis
- Attribute lookups cached in static `ConcurrentDictionary<Type, FieldMetadata[]>` — zero reflection overhead after first access
- Matches existing attribute-based patterns: `[RequiresDPIA]`, `[RequiresProcessor]`, `[ProcessesPersonalData]`

</details>

<details>
<summary><strong>3. Enforcement Model — Three-mode pipeline behavior (Block/Warn/Disabled)</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Three-mode enforcement (Block/Warn/Disabled)** | Consistent with all other compliance behaviors, gradual rollout | Slightly more complex options |
| **B) Boolean block/allow** | Simple | No gradual rollout, inconsistent with DPIA/ProcessorAgreements |
| **C) Per-validation-type modes** | Maximum granularity | Over-engineered, confusing configuration |

### Chosen Option: **A — Three-mode enforcement**

### Rationale

- `PrivacyByDesignEnforcementMode.Block` — returns `Left<EncinaError>` on violation
- `PrivacyByDesignEnforcementMode.Warn` — logs warning, records metrics, allows through
- `PrivacyByDesignEnforcementMode.Disabled` — skips entirely (zero overhead)
- Consistent with `DPIAEnforcementMode` and `ProcessorAgreementEnforcementMode`
- Allows teams to deploy in Warn mode first, then switch to Block after baselining
- Individual validation types also controllable: `options.EnforceDataMinimization`, `options.EnforcePurposeLimitation`, `options.EnforceDefaultPrivacy`

</details>

<details>
<summary><strong>4. Minimization Analysis — Reflection-based field analyzer with report generation</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Reflection-based analyzer with caching and reports** | Automatic discovery, actionable output | Initial reflection cost (mitigated by caching) |
| **B) Manual field declarations** | No reflection | Tedious, error-prone, fields easily forgotten |
| **C) Source generator** | Zero runtime cost | Complex build infrastructure, hard to debug |

### Chosen Option: **A — Reflection-based analyzer with caching**

### Rationale

- `IDataMinimizationAnalyzer.AnalyzeAsync<TRequest>()` returns `MinimizationReport` with:
  - `NecessaryFields` — fields without `[NotStrictlyNecessary]`
  - `UnnecessaryFields` — fields with `[NotStrictlyNecessary]` that have non-default values
  - `MinimizationScore` — ratio of necessary to total fields (0.0-1.0)
  - `Recommendations` — actionable suggestions
- `DefaultDataMinimizationAnalyzer` caches field metadata per request type in static `ConcurrentDictionary`
- Reports are useful both at runtime (pipeline behavior) and for compliance audits
- Stateless: no store needed, analysis is purely type-metadata + current request values

</details>

<details>
<summary><strong>5. Privacy Default Validation — Value-level check against declared defaults</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Attribute value comparison** | Declarative, checked in pipeline | Limited to simple value types (bool, string, enum) |
| **B) Convention-based (all booleans default false)** | Zero attributes | Too opinionated, doesn't work for string/enum fields |
| **C) Builder pattern enforcement** | Strong guarantees | Requires changing how request objects are created |

### Chosen Option: **A — Attribute value comparison**

### Rationale

- `[PrivacyDefault(false)]` declares the privacy-respecting default for the field
- Pipeline behavior checks if the request's actual value matches the declared default
- If value differs from privacy default AND no explicit consent/override, it's flagged
- Works for: `bool`, `string?`, `int`, `enum` — covers common opt-in/opt-out patterns
- Example: `[PrivacyDefault(false)] public bool ShareWithPartners` — if value is `true` without explicit opt-in, violation reported
- `PrivacyDefaultValidation` is a separate concern from minimization — can be enabled/disabled independently

</details>

<details>
<summary><strong>6. Audit Trail Integration — Domain notifications via Encina notification pipeline</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Domain notifications** | Uses existing Encina notification pipeline, decoupled | Requires notification handler for audit capture |
| **B) Direct audit store** | Direct persistence | Adds store dependency, violates stateless nature |
| **C) Logging only** | Simple | Not queryable, not suitable for compliance reporting |

### Chosen Option: **A — Domain notifications**

### Rationale

- Publish `PrivacyViolationDetectedNotification` via Encina's `INotificationPublisher` when violations are detected
- Notification carries: `RequestTypeName`, `ViolationType`, `ViolatedFields`, `EnforcementMode`, `TenantId`, `ModuleId`, `OccurredAtUtc`
- Consumers (audit trail, SIEM, compliance dashboard) implement `INotificationHandler<PrivacyViolationDetectedNotification>`
- Keeps the package stateless — no store interface needed
- Integrates naturally with existing Outbox pattern for reliable delivery

</details>

<details>
<summary><strong>7. Module Isolation — Module-aware purpose registry and context propagation</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Module-aware purpose registry + context propagation** | Per-module privacy rules, consistent with existing IModuleExecutionContext | Optional IModuleExecutionContext dependency |
| **B) Defer to a separate issue** | Simpler initial implementation | Misses an easy integration that other packages partially do |
| **C) Full module-scoped enforcement** | Maximum isolation | Over-engineered for current needs |

### Chosen Option: **A — Module-aware purpose registry + context propagation**

### Rationale

- `IModuleExecutionContext` already exists in Encina core (AsyncLocal-based ambient context)
- Pipeline behavior resolves `IModuleExecutionContext` (optional dependency) and propagates `CurrentModule` to:
  - OpenTelemetry activity tags (`encina.module_id`)
  - Structured log parameters
  - Notification records (`ModuleId` property)
- `IPurposeRegistry` supports optional module scoping: `GetPurpose(purposeName, moduleId?)` — allows per-module purpose definitions
- `InMemoryPurposeRegistry` stores purposes with optional module qualifier: global purposes apply everywhere, module-scoped purposes override globals
- Consistent with how DPIA and ProcessorAgreements define `ModuleId` on their domain records
- No additional external dependencies — only uses existing `IModuleExecutionContext` from Encina core

</details>

---

## Implementation Phases

### Phase 1: Core Models, Enums & Attributes

> **Goal**: Establish the foundational types — enums, domain records, and declarative attributes.

<details>
<summary><strong>Tasks</strong></summary>

#### New project: `src/Encina.Compliance.PrivacyByDesign/`

1. **Create project file** `Encina.Compliance.PrivacyByDesign.csproj`
   - Target: `net10.0`
   - Dependencies: `Encina`, `LanguageExt.Core`, `Microsoft.Extensions.Logging.Abstractions`, `Microsoft.Extensions.Options`
   - Enable nullable, implicit usings, XML doc
   - Add to `Encina.slnx`

2. **Enums** (`Model/` folder):
   - `PrivacyByDesignEnforcementMode` — `Disabled`, `Warn`, `Block`
   - `PrivacyViolationType` — `DataMinimization`, `PurposeLimitation`, `DefaultPrivacy`
   - `PrivacyLevel` — `Minimum`, `Standard`, `Maximum`
   - `MinimizationSeverity` — `Info`, `Warning`, `Violation`

3. **Domain records** (`Model/` folder):
   - `MinimizationReport` — sealed record: `RequestTypeName (string)`, `NecessaryFields (IReadOnlyList<FieldInfo>)`, `UnnecessaryFields (IReadOnlyList<UnnecessaryFieldInfo>)`, `MinimizationScore (double)`, `Recommendations (IReadOnlyList<string>)`, `AnalyzedAtUtc (DateTimeOffset)`
   - `UnnecessaryFieldInfo` — sealed record: `FieldName (string)`, `Reason (string)`, `HasValue (bool)`, `Severity (MinimizationSeverity)`
   - `FieldInfo` — sealed record: `FieldName (string)`, `Purpose (string?)`, `IsRequired (bool)`
   - `PurposeValidationResult` — sealed record: `DeclaredPurpose (string)`, `AllowedFields (IReadOnlyList<string>)`, `ViolatingFields (IReadOnlyList<string>)`, `IsValid (bool)`
   - `DefaultPrivacyViolation` — sealed record: `FieldName (string)`, `ExpectedDefault (object?)`, `ActualValue (object?)`, `Description (string)`
   - `PrivacyValidationResult` — sealed record: `IsCompliant (bool)`, `Violations (IReadOnlyList<PrivacyViolationDetail>)`, `MinimizationReport (MinimizationReport?)`, `PurposeResults (IReadOnlyList<PurposeValidationResult>)`, `DefaultViolations (IReadOnlyList<DefaultPrivacyViolation>)`
   - `PrivacyViolationDetail` — sealed record: `ViolationType (PrivacyViolationType)`, `FieldName (string)`, `Message (string)`, `GDPRArticle (string)`
   - `PurposeDefinition` — sealed record: `Name (string)`, `Description (string?)`, `AllowedFields (IReadOnlyList<string>)`, `ModuleId (string?)` — null means global

4. **Attributes** (`Attributes/` folder):
   - `EnforceDataMinimizationAttribute` — `[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]`: no properties (marker)
   - `NotStrictlyNecessaryAttribute` — `[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]`: `string Reason { get; set; }`, `MinimizationSeverity Severity { get; set; } = MinimizationSeverity.Warning`
   - `PurposeLimitationAttribute` — `[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]`: `string Purpose { get; }` (constructor param)
   - `PrivacyDefaultAttribute` — `[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]`: `object? DefaultValue { get; }` (constructor param)

5. **Notification records** (`Notifications/` folder):
   - `PrivacyViolationDetectedNotification` — sealed record implementing `INotification`: `RequestTypeName (string)`, `ViolationType (PrivacyViolationType)`, `ViolatedFields (IReadOnlyList<string>)`, `EnforcementMode (PrivacyByDesignEnforcementMode)`, `TenantId (string?)`, `ModuleId (string?)`, `OccurredAtUtc (DateTimeOffset)`
   - `MinimizationAnalysisCompletedNotification` — sealed record implementing `INotification`: `RequestTypeName (string)`, `MinimizationScore (double)`, `UnnecessaryFieldCount (int)`, `ModuleId (string?)`, `OccurredAtUtc (DateTimeOffset)`

6. **`PublicAPI.Unshipped.txt`** — Add all public types

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of Encina.Compliance.PrivacyByDesign (Issue #411).

CONTEXT:
- Encina is a .NET 10 / C# 14 library using Railway Oriented Programming (Either<EncinaError, T>)
- This is a NEW project: src/Encina.Compliance.PrivacyByDesign/
- Reference existing patterns in src/Encina.Compliance.DPIA/Model/ and src/Encina.Compliance.DPIA/Attributes/
- All domain models are sealed records with XML documentation
- Use LanguageExt for Option<T> and Either<L, R>
- Timestamps use DateTimeOffset with AtUtc suffix convention
- GDPR Art. 25 covers: data minimization, purpose limitation, privacy defaults

TASK:
1. Create the project file: src/Encina.Compliance.PrivacyByDesign/Encina.Compliance.PrivacyByDesign.csproj
   - Target: net10.0, enable nullable, implicit usings, XML doc
   - Dependencies: Encina (project ref), LanguageExt.Core, Microsoft.Extensions.Logging.Abstractions, Microsoft.Extensions.Options
2. Create all enums in Model/ folder
3. Create all domain records in Model/ folder (sealed records, XML docs with GDPR article references)
4. Create all attributes in Attributes/ folder
5. Create notification records in Notifications/ folder (include ModuleId property)
6. Create PublicAPI.Unshipped.txt with all public symbols
7. Add the project to Encina.slnx

KEY RULES:
- All types are sealed records (not classes)
- All public types need XML documentation with <summary>, <remarks>, and GDPR article references
- Attributes use AttributeTargets.Property (except EnforceDataMinimization which targets Class)
- Notification records implement INotification from Encina core
- MinimizationScore is a double between 0.0 (no unnecessary fields) and 1.0 (all fields necessary)
- PrivacyDefaultAttribute stores object? DefaultValue (boxed primitives)
- PurposeDefinition includes optional ModuleId for module-scoped purposes

REFERENCE FILES:
- src/Encina.Compliance.DPIA/Model/DPIAAssessment.cs (sealed record pattern with ModuleId)
- src/Encina.Compliance.DPIA/Model/DPIAEnforcementMode.cs (enum pattern)
- src/Encina.Compliance.DPIA/Attributes/RequiresDPIAAttribute.cs (attribute pattern)
- src/Encina.Compliance.DPIA/Notifications/DPIAAssessmentCompletedNotification.cs (notification pattern)
- src/Encina.Compliance.DPIA/Encina.Compliance.DPIA.csproj (project file pattern)
```

</details>

---

### Phase 2: Core Interfaces & Error Codes

> **Goal**: Define the public API surface — interfaces and error factory.

<details>
<summary><strong>Tasks</strong></summary>

1. **Interfaces** (`Abstractions/` folder):
   - `IPrivacyByDesignValidator` — Main validation orchestrator
     - `ValidateDataMinimizationAsync<TRequest>(TRequest request, CancellationToken ct) -> ValueTask<Either<EncinaError, PrivacyValidationResult>>`
     - `ValidatePurposeLimitationAsync<TRequest>(TRequest request, string declaredPurpose, CancellationToken ct) -> ValueTask<Either<EncinaError, PurposeValidationResult>>`
     - `ValidateDefaultPrivacyAsync<TRequest>(TRequest request, CancellationToken ct) -> ValueTask<Either<EncinaError, IReadOnlyList<DefaultPrivacyViolation>>>`
     - `ValidateAllAsync<TRequest>(TRequest request, string? declaredPurpose, CancellationToken ct) -> ValueTask<Either<EncinaError, PrivacyValidationResult>>`
   - `IDataMinimizationAnalyzer` — Reflection-based field analysis
     - `AnalyzeAsync<TRequest>(TRequest request, CancellationToken ct) -> ValueTask<Either<EncinaError, MinimizationReport>>`
     - `GetFieldMetadata<TRequest>() -> IReadOnlyList<FieldInfo>` (cached metadata accessor)
   - `IPurposeRegistry` — Purpose definition registry (module-aware)
     - `GetPurpose(string purposeName, string? moduleId = null) -> Option<PurposeDefinition>`
     - `GetAllPurposes(string? moduleId = null) -> IReadOnlyList<PurposeDefinition>`
     - `RegisterPurpose(PurposeDefinition purpose) -> void`

2. **Error factory** (`PrivacyByDesignErrors.cs`):
   - `DataMinimizationViolation(string requestTypeName, IReadOnlyList<string> violatingFields)` — code: `pbd.data_minimization_violation`
   - `PurposeLimitationViolation(string requestTypeName, string purpose, IReadOnlyList<string> violatingFields)` — code: `pbd.purpose_limitation_violation`
   - `DefaultPrivacyViolation(string requestTypeName, IReadOnlyList<string> violatingFields)` — code: `pbd.default_privacy_violation`
   - `UndeclaredPurpose(string purposeName)` — code: `pbd.undeclared_purpose`
   - `ValidationFailed(string requestTypeName, string detail, Exception? ex)` — code: `pbd.validation_failed`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of Encina.Compliance.PrivacyByDesign (Issue #411).

CONTEXT:
- Phase 1 is complete: models, enums, attributes, notifications exist
- Encina uses Railway Oriented Programming: Either<EncinaError, T> on all async methods
- Error codes follow the pattern: {feature_lowercase}.{category} (e.g., "pbd.data_minimization_violation")
- All interfaces go in Abstractions/ folder

TASK:
1. Create IPrivacyByDesignValidator in Abstractions/ — main validation orchestrator with 4 methods
2. Create IDataMinimizationAnalyzer in Abstractions/ — reflection-based field analysis
3. Create IPurposeRegistry in Abstractions/ — module-aware purpose definition registry
   - GetPurpose(string purposeName, string? moduleId = null) — looks up module-specific first, then global
   - GetAllPurposes(string? moduleId = null) — returns module-specific + global purposes
4. Create PrivacyByDesignErrors.cs — static error factory following EncinaErrors.Create() pattern

KEY RULES:
- All async methods return ValueTask<Either<EncinaError, T>>
- Non-async methods return Option<T> or plain types
- IPurposeRegistry is module-aware: moduleId parameter defaults to null (global scope)
- Error codes: "pbd.{violation_type}" prefix
- Errors include metadata Dictionary<string, object?> with relevant context
- Errors reference GDPR Art. 25 in messages
- All interfaces need XML docs with <summary>, <remarks>, GDPR references
- Update PublicAPI.Unshipped.txt

REFERENCE FILES:
- src/Encina.Compliance.DPIA/Abstractions/IDPIAStore.cs (interface pattern)
- src/Encina.Compliance.DPIA/Abstractions/IDPIAAssessmentEngine.cs (engine interface pattern)
- src/Encina.Compliance.DPIA/DPIAErrors.cs (error factory pattern)
- src/Encina.Compliance.ProcessorAgreements/ProcessorAgreementErrors.cs (error factory pattern)
```

</details>

---

### Phase 3: Default Implementations

> **Goal**: Implement the core validation logic — analyzer, validator, purpose registry.

<details>
<summary><strong>Tasks</strong></summary>

1. **`DefaultDataMinimizationAnalyzer`** — Implements `IDataMinimizationAnalyzer`
   - Namespace: `Encina.Compliance.PrivacyByDesign`
   - Dependencies: `ILogger<DefaultDataMinimizationAnalyzer>`, `TimeProvider`
   - Static `ConcurrentDictionary<Type, FieldMetadataCache>` for per-type reflection cache
   - `FieldMetadataCache` — internal record: property infos, attribute metadata, cached at first access
   - `AnalyzeAsync<TRequest>()`:
     1. Get or build field metadata cache for `typeof(TRequest)`
     2. For each property with `[NotStrictlyNecessary]`: check if value is non-default
     3. Calculate `MinimizationScore = necessaryCount / totalCount`
     4. Generate recommendations based on unnecessary fields with values
   - `GetFieldMetadata<TRequest>()` — returns cached field list
   - Registered as: `Scoped` (via TryAdd)

2. **`InMemoryPurposeRegistry`** — Implements `IPurposeRegistry`
   - Thread-safe `ConcurrentDictionary<(string Name, string? ModuleId), PurposeDefinition>`
   - `GetPurpose(name, moduleId)`: looks up module-specific first (`(name, moduleId)`), falls back to global (`(name, null)`)
   - `GetAllPurposes(moduleId)`: returns module-specific purposes + global purposes (module overrides global)
   - Populated from `PrivacyByDesignOptions.Purposes` at registration time
   - Registered as: `Singleton` (via TryAdd)

3. **`DefaultPrivacyByDesignValidator`** — Implements `IPrivacyByDesignValidator`
   - Dependencies: `IDataMinimizationAnalyzer`, `IPurposeRegistry`, `IOptions<PrivacyByDesignOptions>`, `ILogger<...>`, `TimeProvider`
   - `ValidateDataMinimizationAsync<TRequest>()`:
     1. Analyze fields via `IDataMinimizationAnalyzer`
     2. Check if unnecessary fields have non-default values
     3. Return violations based on severity settings
   - `ValidatePurposeLimitationAsync<TRequest>()`:
     1. Look up purpose in `IPurposeRegistry` (with optional moduleId for module-scoped lookups)
     2. Check each field with `[PurposeLimitation]` — verify purpose matches declared purpose
     3. Check fields without `[PurposeLimitation]` against purpose's allowed fields list
   - `ValidateDefaultPrivacyAsync<TRequest>()`:
     1. Find all properties with `[PrivacyDefault]`
     2. Compare actual value against declared default
     3. Report violations where value != expected default
   - `ValidateAllAsync<TRequest>()` — orchestrates all three validations
   - Registered as: `Scoped` (via TryAdd)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of Encina.Compliance.PrivacyByDesign (Issue #411).

CONTEXT:
- Phases 1-2 are complete: models, enums, attributes, interfaces, errors exist
- This phase implements the core validation logic
- All implementations are stateless — no database/store dependencies
- Uses static ConcurrentDictionary for reflection caching (same pattern as DPIA/ProcessorAgreements pipeline behaviors)

TASK:
1. Create DefaultDataMinimizationAnalyzer — implements IDataMinimizationAnalyzer
   - Cache field metadata per type in static ConcurrentDictionary<Type, FieldMetadataCache>
   - FieldMetadataCache: internal sealed record with PropertyInfo[], NotStrictlyNecessaryAttribute?[], PurposeLimitationAttribute?[], PrivacyDefaultAttribute?[]
   - Analyze: iterate properties, check values against defaults, calculate score
2. Create InMemoryPurposeRegistry — implements IPurposeRegistry
   - ConcurrentDictionary<(string Name, string? ModuleId), PurposeDefinition> internally
   - Module-aware: GetPurpose(name, moduleId) looks up module-specific first, then global fallback
   - GetAllPurposes(moduleId) returns module-specific + global (module overrides global when same name)
   - Populated from options at DI time
3. Create DefaultPrivacyByDesignValidator — implements IPrivacyByDesignValidator
   - Orchestrates all three validation types
   - Each method returns Either<EncinaError, result>
   - ValidateAllAsync combines results from all three

KEY RULES:
- Static ConcurrentDictionary for reflection caching — zero overhead after first call per type
- Use GetOrAdd with static lambda to avoid closures
- All async methods return ValueTask<Either<EncinaError, T>>
- Use LanguageExt Right/Left helpers
- Catch exceptions in try/catch, return Left with PrivacyByDesignErrors.ValidationFailed
- Log with ILogger (actual LoggerMessage definitions come in Phase 7)
- TimeProvider for all timestamps
- XML documentation on all public members

REFERENCE FILES:
- src/Encina.Compliance.DPIA/DefaultDPIAAssessmentEngine.cs (engine implementation pattern)
- src/Encina.Compliance.DPIA/DPIARequiredPipelineBehavior.cs (ConcurrentDictionary caching pattern)
- src/Encina.Compliance.ProcessorAgreements/InMemoryProcessorRegistry.cs (in-memory registry pattern)
```

</details>

---

### Phase 4: Pipeline Behavior

> **Goal**: Implement `DataMinimizationPipelineBehavior` — the enforcement point in the request pipeline.

<details>
<summary><strong>Tasks</strong></summary>

1. **`DataMinimizationPipelineBehavior<TRequest, TResponse>`** — Implements `IPipelineBehavior<TRequest, TResponse>`
   - Namespace: `Encina.Compliance.PrivacyByDesign`
   - Dependencies: `IPrivacyByDesignValidator`, `IOptions<PrivacyByDesignOptions>`, `TimeProvider`, `ILogger<...>`
   - Optional dependencies (resolved via `IServiceProvider`):
     - `INotificationPublisher?` — for audit trail notifications
     - `IModuleExecutionContext?` — for module isolation context
   - Static `ConcurrentDictionary<Type, EnforceDataMinimizationAttribute?>` for attribute caching
   - `Handle()` implementation:
     1. Check enforcement mode — skip if `Disabled`
     2. Check for `[EnforceDataMinimization]` attribute (cached)
     3. If no attribute, skip (log trace)
     4. Start OpenTelemetry activity
     5. Propagate tenant context (`context.TenantId`) and module context (`IModuleExecutionContext.CurrentModule`) to traces
     6. Call `IPrivacyByDesignValidator.ValidateAllAsync()`
     7. On violations:
        - **Block mode**: return `Left<EncinaError>` with violation details
        - **Warn mode**: log warning, record metrics, publish notification (with TenantId + ModuleId), proceed
     8. On success: record passed metrics, proceed to `nextStep()`
     9. Catch exceptions: Block mode returns error, Warn mode logs and proceeds
   - Registered as: `Transient` (via TryAdd)

2. **Purpose extraction helper** (private within behavior):
   - Attempt to extract purpose from request type name, `[PurposeLimitation]` attributes, or options default
   - Falls back to `null` if no purpose can be determined (purpose validation skipped)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of Encina.Compliance.PrivacyByDesign (Issue #411).

CONTEXT:
- Phases 1-3 are complete: models, interfaces, default implementations exist
- This phase creates the pipeline behavior — the enforcement point
- Follows the exact same pattern as DPIARequiredPipelineBehavior and ProcessorAgreementPipelineBehavior

TASK:
Create DataMinimizationPipelineBehavior<TRequest, TResponse> implementing IPipelineBehavior<TRequest, TResponse>
- where TRequest : IRequest<TResponse>

Pipeline flow:
1. Check PrivacyByDesignOptions.EnforcementMode → skip if Disabled
2. Check for [EnforceDataMinimization] attribute (static ConcurrentDictionary cache)
3. If no attribute → skip, log trace
4. Start Activity via PrivacyByDesignDiagnostics.StartPipelineCheck()
5. Propagate context.TenantId to activity tags ("encina.tenant_id")
6. Resolve IModuleExecutionContext (optional) and propagate CurrentModule to activity tags ("encina.module_id")
7. Call _validator.ValidateAllAsync() with request
8. If violations found:
   - Block mode: return Left<EncinaError>(PrivacyByDesignErrors.DataMinimizationViolation(...))
   - Warn mode: log, record metrics, publish PrivacyViolationDetectedNotification (include TenantId + ModuleId), return nextStep()
9. If no violations: record passed, return nextStep()
10. Exception handling: Block → return error, Warn → log + continue

KEY RULES:
- Static ConcurrentDictionary<Type, EnforceDataMinimizationAttribute?> for attribute lookup
- Use GetOrAdd with static lambda: type.GetCustomAttribute<EnforceDataMinimizationAttribute>()
- INotificationPublisher is optional (resolve via IServiceProvider?.GetService<INotificationPublisher>())
- IModuleExecutionContext is optional (resolve via IServiceProvider?.GetService<IModuleExecutionContext>())
- Use Stopwatch.GetTimestamp() for duration measurement
- Record counters: pipeline.checks.total, .passed, .failed, .skipped
- Record histogram: pipeline.check.duration (ms)
- ArgumentNullException.ThrowIfNull on all constructor and Handle parameters
- XML documentation referencing GDPR Art. 25

REFERENCE FILES:
- src/Encina.Compliance.DPIA/DPIARequiredPipelineBehavior.cs (EXACT pattern to follow)
- src/Encina.Compliance.ProcessorAgreements/ProcessorAgreementPipelineBehavior.cs (alternative reference)
- src/Encina/Modules/Isolation/IModuleExecutionContext.cs (module context interface)
```

</details>

---

### Phase 5: Configuration & DI

> **Goal**: Options class, options validator, DI registration.

<details>
<summary><strong>Tasks</strong></summary>

1. **`PrivacyByDesignOptions`** — Configuration options (sealed class)
   - `EnforcementMode` — `PrivacyByDesignEnforcementMode` (default: `Warn`)
   - `EnforceDataMinimization` — `bool` (default: `true`)
   - `EnforcePurposeLimitation` — `bool` (default: `true`)
   - `EnforceDefaultPrivacy` — `bool` (default: `true`)
   - `DefaultPrivacyLevel` — `PrivacyLevel` (default: `Maximum`)
   - `BlockExcessiveCollection` — `bool` convenience alias for `EnforcementMode == Block` when minimization fails
   - `LogMinimizationViolations` — `bool` (default: `true`)
   - `MinimizationScoreThreshold` — `double` (default: `0.8`) — score below which violations are flagged
   - `Purposes` — `Dictionary<string, PurposeDefinition>` (populated via `AddPurpose()`)
   - `AddPurpose(string name, Action<PurposeBuilder> configure)` — fluent purpose registration (global scope)
   - `AddPurpose(string name, string moduleId, Action<PurposeBuilder> configure)` — fluent purpose registration (module scope)
   - `AddHealthCheck` — `bool` (default: `false`)
   - `AssembliesToScan` — `List<Assembly>` (for auto-discovery)

2. **`PurposeBuilder`** — Fluent builder for `PurposeDefinition`
   - `AllowedFields` — `List<string>`
   - `Description` — `string?`
   - Internal `ModuleId` — `string?`
   - Builds `PurposeDefinition` record

3. **`PrivacyByDesignOptionsValidator`** — Implements `IValidateOptions<PrivacyByDesignOptions>`
   - Validates `MinimizationScoreThreshold` is between 0.0 and 1.0
   - Validates purpose names are non-empty
   - Validates purpose allowed fields are non-empty

4. **`ServiceCollectionExtensions`** — DI registration
   - `AddEncinaPrivacyByDesign(this IServiceCollection services, Action<PrivacyByDesignOptions>? configure = null)`
     - Configure + validate options
     - `TryAddSingleton(TimeProvider.System)`
     - `TryAddSingleton<IPurposeRegistry, InMemoryPurposeRegistry>` — populate from options.Purposes (both global and module-scoped)
     - `TryAddScoped<IDataMinimizationAnalyzer, DefaultDataMinimizationAnalyzer>`
     - `TryAddScoped<IPrivacyByDesignValidator, DefaultPrivacyByDesignValidator>`
     - `TryAddTransient(typeof(IPipelineBehavior<,>), typeof(DataMinimizationPipelineBehavior<,>))`
     - Conditional: health check if `AddHealthCheck = true`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of Encina.Compliance.PrivacyByDesign (Issue #411).

CONTEXT:
- Phases 1-4 are complete: models, interfaces, implementations, pipeline behavior exist
- This phase adds configuration, DI registration
- Observability (diagnostics + logging) will be added in Phase 7
- Follows the exact patterns from Encina.Compliance.DPIA

TASK:
1. Create PrivacyByDesignOptions (sealed class) with all configuration properties
   - Include AddPurpose overloads for both global and module-scoped purposes
2. Create PurposeBuilder for fluent purpose registration (with optional ModuleId)
3. Create PrivacyByDesignOptionsValidator (IValidateOptions<PrivacyByDesignOptions>)
4. Create ServiceCollectionExtensions.cs — AddEncinaPrivacyByDesign() registration
   - InMemoryPurposeRegistry populated from options: both global and module-scoped purposes

KEY RULES:
- Options: sealed class, not record (mutable for configuration)
- All DI uses TryAdd* pattern (allows override before AddEncina* call)
- Pipeline behavior: TryAddTransient(typeof(IPipelineBehavior<,>), typeof(DataMinimizationPipelineBehavior<,>))
- Health check: conditional (only if AddHealthCheck = true)
- Purpose registration supports module scoping via AddPurpose(name, moduleId, configure)
- XML documentation on all public APIs
- Update PublicAPI.Unshipped.txt

REFERENCE FILES:
- src/Encina.Compliance.DPIA/DPIAOptions.cs (options class pattern)
- src/Encina.Compliance.DPIA/DPIAOptionsValidator.cs (options validator pattern)
- src/Encina.Compliance.DPIA/ServiceCollectionExtensions.cs (DI registration pattern)
```

</details>

---

### Phase 6: Cross-Cutting Integration

> **Goal**: Wire up multi-tenancy propagation, module isolation, audit trail notifications, and GDPR module enrichment.

<details>
<summary><strong>Tasks</strong></summary>

1. **Multi-tenancy integration** (in pipeline behavior):
   - Propagate `context.TenantId` to:
     - OpenTelemetry activity tags (`encina.tenant_id`)
     - Notification records (`TenantId` property)
     - Log message structured parameters
   - Ensure per-tenant privacy options could be supported in future (configuration extensibility)

2. **Module isolation integration** (in pipeline behavior and purpose registry):
   - Resolve `IModuleExecutionContext` (optional) from service provider
   - Propagate `CurrentModule` to:
     - OpenTelemetry activity tags (`encina.module_id`)
     - Notification records (`ModuleId` property)
     - Log message structured parameters
   - Pass `moduleId` to `IPurposeRegistry.GetPurpose()` for module-scoped purpose lookups
   - When module context is available, purpose validation prefers module-specific definitions over globals
   - When module context is not available (no module isolation configured), fall back to global purposes

3. **Audit trail integration** (in pipeline behavior):
   - On violation detection (both Block and Warn modes):
     - Publish `PrivacyViolationDetectedNotification` via `INotificationPublisher` (if registered)
     - Include: request type, violation type, affected fields, enforcement mode, tenant, module
   - On successful minimization analysis:
     - Publish `MinimizationAnalysisCompletedNotification` (if `LogMinimizationViolations` is enabled)
   - Notifications are fire-and-forget — failures don't block the pipeline

4. **Integration with existing GDPR infrastructure**:
   - Optional reference to `[ProcessesPersonalData]` from `Encina.Compliance.GDPR` for additional context
   - If a request has both `[EnforceDataMinimization]` and `[ProcessesPersonalData]`, enrich violation reports

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of Encina.Compliance.PrivacyByDesign (Issue #411).

CONTEXT:
- Phases 1-5 are complete: full package with configuration and DI
- This phase integrates all cross-cutting concerns: multi-tenancy, module isolation, audit trail, GDPR module
- The package is stateless — no stores to integrate with

TASK:
1. Update DataMinimizationPipelineBehavior to:
   a. Propagate TenantId from IRequestContext to activity tags, notifications, and logs
   b. Resolve IModuleExecutionContext (optional) and propagate CurrentModule to:
      - Activity tags ("encina.module_id")
      - Notification records (ModuleId property)
      - Structured log parameters
   c. Pass moduleId to validator for module-scoped purpose lookups
2. Add notification publishing in the pipeline behavior:
   - Resolve INotificationPublisher via IServiceProvider (optional, nullable)
   - On violation: publish PrivacyViolationDetectedNotification (with TenantId + ModuleId)
   - On analysis completion: publish MinimizationAnalysisCompletedNotification (if enabled, with ModuleId)
   - Wrap in try/catch — notification failures don't block pipeline
3. Update DefaultPrivacyByDesignValidator:
   - Accept optional moduleId parameter in purpose-related methods
   - Pass moduleId to IPurposeRegistry.GetPurpose(name, moduleId)
4. Add optional [ProcessesPersonalData] awareness:
   - If request has both [EnforceDataMinimization] and [ProcessesPersonalData], include processing activity context in violation details

KEY RULES:
- INotificationPublisher is optional dependency — resolve via service provider, not constructor
- IModuleExecutionContext is optional dependency — resolve via service provider, not constructor
- Module context falls back gracefully when not configured (null moduleId → global purposes only)
- Notification failures are logged at Warning level but never throw
- All new structured parameters added to existing LoggerMessage definitions

REFERENCE FILES:
- src/Encina.Compliance.DPIA/DPIARequiredPipelineBehavior.cs (tenant propagation pattern)
- src/Encina.Compliance.ProcessorAgreements/ProcessorAgreementPipelineBehavior.cs (notification publishing pattern)
- src/Encina/Modules/Isolation/IModuleExecutionContext.cs (module context interface)
- src/Encina/Modules/Isolation/ModuleExecutionContext.cs (AsyncLocal implementation)
```

</details>

---

### Phase 7: Observability

> **Goal**: Add full observability — OpenTelemetry tracing, metrics, structured logging, and health checks.

<details>
<summary><strong>Tasks</strong></summary>

1. **Diagnostics** (`Diagnostics/` folder):
   - `PrivacyByDesignDiagnostics` — internal static class
     - `ActivitySource` named `Encina.Compliance.PrivacyByDesign`, version `1.0`
     - `Meter` named `Encina.Compliance.PrivacyByDesign`, version `1.0`
     - **Counters**:
       - `pbd.pipeline.checks.total` — total pipeline checks
       - `pbd.pipeline.checks.passed` — checks that passed
       - `pbd.pipeline.checks.failed` — checks that failed (Block mode)
       - `pbd.pipeline.checks.skipped` — checks skipped (no attribute or Disabled)
       - `pbd.minimization.violations` — data minimization violations detected
       - `pbd.purpose.violations` — purpose limitation violations detected
       - `pbd.default.violations` — default privacy violations detected
     - **Histogram**:
       - `pbd.pipeline.check.duration` (unit: `ms`) — duration of pipeline check
     - **Tag constants**:
       - `pbd.request_type`, `pbd.violation_type`, `pbd.enforcement_mode`, `pbd.failure_reason`, `pbd.purpose`
     - **Helper methods**: `StartPipelineCheck(string requestTypeName)`, `RecordPassed(Activity?)`, `RecordFailed(Activity?, string failureReason)`, `RecordWarned(Activity?, string failureReason)`

2. **Structured logging** (`Diagnostics/` folder):
   - `PrivacyByDesignLogMessages` — internal static partial class with `[LoggerMessage]` source generator
   - **Event ID range: 9000-9099** (non-overlapping with all other compliance packages)
   - Sub-allocations:
     - 9000-9009: Pipeline behavior (disabled, no attribute, started, passed, failed, warned, blocked, error)
     - 9010-9019: Minimization analysis (started, completed, violation found, score calculated, recommendation)
     - 9020-9029: Purpose validation (started, passed, violation, undeclared purpose)
     - 9030-9039: Default privacy (started, violation, compliant)
     - 9040-9049: Health check (started, healthy, unhealthy)
     - 9050-9059: Configuration and registration (options validated, purposes registered, module purposes registered)
   - All messages include structured parameters (not string interpolation)
   - Include `moduleId` parameter in relevant log messages

3. **Health check** (`Health/` folder):
   - `PrivacyByDesignHealthCheck` — Implements `IHealthCheck`
     - `DefaultName` const: `"Encina.Compliance.PrivacyByDesign"`
     - `Tags` static array: `["encina", "compliance", "privacy-by-design"]`
     - Verifies: validator is resolvable, options are valid, purposes are registered
     - Scoped resolution via `IServiceProvider.CreateScope()`

4. **Wire diagnostics into pipeline behavior and implementations**:
   - Update `DataMinimizationPipelineBehavior` to use `PrivacyByDesignDiagnostics` helpers and `PrivacyByDesignLogMessages`
   - Update `DefaultDataMinimizationAnalyzer` to emit analysis log messages
   - Update `DefaultPrivacyByDesignValidator` to emit validation log messages
   - Update `ServiceCollectionExtensions` to emit registration log messages

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of Encina.Compliance.PrivacyByDesign (Issue #411).

CONTEXT:
- Phases 1-6 are complete: full package with cross-cutting integration
- This phase adds full observability: OpenTelemetry traces, metrics, structured logging, health checks
- Follows the exact patterns from Encina.Compliance.DPIA/Diagnostics/

TASK:
1. Create Diagnostics/PrivacyByDesignDiagnostics.cs — ActivitySource, Meter, Counters, Histograms, Tags, helpers
2. Create Diagnostics/PrivacyByDesignLogMessages.cs — [LoggerMessage] definitions, Event IDs 9000-9099
3. Create Health/PrivacyByDesignHealthCheck.cs — IHealthCheck implementation
4. Wire diagnostics into:
   - DataMinimizationPipelineBehavior (traces, counters, histograms, log messages)
   - DefaultDataMinimizationAnalyzer (analysis log messages)
   - DefaultPrivacyByDesignValidator (validation log messages)
   - ServiceCollectionExtensions (registration log messages)

KEY RULES:
- Event IDs: 9000-9099 range (non-overlapping with DPIA 8800-8899 and ProcessorAgreements 8900-8999)
- LoggerMessage: use [LoggerMessage] source generator attribute, internal static partial methods
- Diagnostics: internal static class, ActivitySource + Meter with "Encina.Compliance.PrivacyByDesign" name and "1.0" version
- Counter names: "pbd.pipeline.checks.total", "pbd.minimization.violations", etc.
- Tag names: "pbd.request_type", "pbd.violation_type", etc.
- Health check: DefaultName const, Tags static array, scoped resolution via IServiceProvider.CreateScope()
- Include moduleId in relevant log messages where available
- All helpers are internal static
- Log messages reference GDPR articles where applicable

REFERENCE FILES:
- src/Encina.Compliance.DPIA/Diagnostics/DPIADiagnostics.cs (diagnostics pattern)
- src/Encina.Compliance.DPIA/Diagnostics/DPIALogMessages.cs (log messages pattern with event ID allocation)
- src/Encina.Compliance.DPIA/Health/DPIAHealthCheck.cs (health check pattern)
- src/Encina.Compliance.ProcessorAgreements/Diagnostics/ProcessorAgreementDiagnostics.cs (alternative diagnostics reference)
- src/Encina.Compliance.ProcessorAgreements/Diagnostics/ProcessorAgreementLogMessages.cs (alternative log messages reference)
```

</details>

---

### Phase 8: Testing

> **Goal**: Comprehensive test coverage across all test types.

<details>
<summary><strong>Tasks</strong></summary>

#### Unit Tests (`tests/Encina.UnitTests/Compliance/PrivacyByDesign/`)

1. **`PrivacyByDesignErrorsTests.cs`** — Error factory methods return correct codes, messages, metadata
2. **`DefaultDataMinimizationAnalyzerTests.cs`** — Field analysis, caching, score calculation
3. **`DefaultPrivacyByDesignValidatorTests.cs`** — All three validation types, combined validation, module-scoped purpose lookups
4. **`DataMinimizationPipelineBehaviorTests.cs`** — Enforcement modes (Block/Warn/Disabled), attribute caching, tenant propagation, module propagation
5. **`InMemoryPurposeRegistryTests.cs`** — Purpose registration, lookup, concurrent access, module-scoped vs global fallback
6. **`PrivacyByDesignOptionsValidatorTests.cs`** — Options validation (threshold bounds, empty purposes)
7. **`ServiceCollectionExtensionsTests.cs`** — DI registration, TryAdd override, conditional health check, module-scoped purpose registration
8. **Model tests**: `MinimizationReportTests.cs`, `PurposeValidationResultTests.cs`, `PrivacyValidationResultTests.cs`
9. **Attribute tests**: `NotStrictlyNecessaryAttributeTests.cs`, `PurposeLimitationAttributeTests.cs`, `PrivacyDefaultAttributeTests.cs`, `EnforceDataMinimizationAttributeTests.cs`

#### Guard Tests (`tests/Encina.GuardTests/Compliance/PrivacyByDesign/`)

10. **`DataMinimizationPipelineBehaviorGuardTests.cs`** — Null checks on constructor and Handle parameters
11. **`DefaultPrivacyByDesignValidatorGuardTests.cs`** — Null checks on constructor and method parameters
12. **`DefaultDataMinimizationAnalyzerGuardTests.cs`** — Null checks on constructor and method parameters
13. **`ServiceCollectionExtensionsGuardTests.cs`** — Null service collection

#### Contract Tests (`tests/Encina.ContractTests/Compliance/PrivacyByDesign/`)

14. **`IPrivacyByDesignValidatorContractTests.cs`** — Verify DefaultPrivacyByDesignValidator follows interface contract
15. **`IDataMinimizationAnalyzerContractTests.cs`** — Verify DefaultDataMinimizationAnalyzer follows interface contract
16. **`IPurposeRegistryContractTests.cs`** — Verify InMemoryPurposeRegistry follows interface contract (including module scoping)

#### Property Tests (`tests/Encina.PropertyTests/Compliance/PrivacyByDesign/`)

17. **`MinimizationReportPropertyTests.cs`** — FsCheck: score always in [0.0, 1.0], field counts consistent
18. **`PrivacyValidationPropertyTests.cs`** — FsCheck: IsCompliant ↔ Violations.Count == 0 invariant
19. **`PurposeRegistryPropertyTests.cs`** — FsCheck: registered purpose always retrievable, module-specific overrides global

#### Integration Tests (`tests/Encina.IntegrationTests/Compliance/PrivacyByDesign/`)

20. **`PrivacyByDesignPipelineIntegrationTests.cs`** — Full DI → pipeline → validation lifecycle
    - DI registration verification
    - Options configuration
    - Pipeline behavior with real validator
    - Block/Warn/Disabled modes end-to-end
    - Multi-tenancy propagation
    - Module isolation propagation (with mock IModuleExecutionContext)
    - Purpose-based validation with configured purposes (both global and module-scoped)

#### Load Tests (`tests/Encina.LoadTests/Compliance/PrivacyByDesign/`)

21. **`PrivacyByDesign.md`** — Justification: stateless validation, no concurrent shared state, no I/O contention. Reflection is cached in static ConcurrentDictionary. Adequate coverage from unit + integration tests.

#### Benchmark Tests (`tests/Encina.BenchmarkTests/Compliance/PrivacyByDesign/`)

22. **`PrivacyByDesign.md`** — Justification: attribute reflection is cached (zero overhead after first call), validation logic is simple property comparisons. Not a hot path relative to database operations.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 of Encina.Compliance.PrivacyByDesign (Issue #411).

CONTEXT:
- Phases 1-7 are complete: full package with cross-cutting integration and observability
- This phase creates comprehensive test coverage
- No database providers = no provider-specific integration tests
- Stateless validation = load/benchmark tests justified via .md files
- Module isolation is included — test module-scoped purpose lookups and ModuleId propagation

TASK:
Create all test files listed in the Phase 8 Tasks section.

Test categories:
1. Unit Tests (11+ test classes) — tests/Encina.UnitTests/Compliance/PrivacyByDesign/
2. Guard Tests (4 test classes) — tests/Encina.GuardTests/Compliance/PrivacyByDesign/
3. Contract Tests (3 test classes) — tests/Encina.ContractTests/Compliance/PrivacyByDesign/
4. Property Tests (3 test classes) — tests/Encina.PropertyTests/Compliance/PrivacyByDesign/
5. Integration Tests (1 comprehensive test class) — tests/Encina.IntegrationTests/Compliance/PrivacyByDesign/
6. Load Tests justification .md — tests/Encina.LoadTests/Compliance/PrivacyByDesign/
7. Benchmark Tests justification .md — tests/Encina.BenchmarkTests/Compliance/PrivacyByDesign/

Test helper types needed:
- TestRequest records with [EnforceDataMinimization] + various attribute combinations
- TestRequestWithoutAttribute (no enforcement, for skip tests)
- TestRequestWithPurpose (with [PurposeLimitation] fields)
- TestRequestWithDefaults (with [PrivacyDefault] fields)

Module isolation specific tests:
- Verify ModuleId propagated to notification records
- Verify module-scoped purposes override global purposes
- Verify fallback to global when no module context
- Verify InMemoryPurposeRegistry module-aware lookups

KEY RULES:
- Unit tests: AAA pattern, NSubstitute for mocks, Shouldly assertions
- Guard tests: Should.Throw<ArgumentNullException>(), verify ParamName
- Contract tests: abstract base class pattern, Either<,> assertions
- Property tests: FsCheck [Property(MaxTest = 50)], return bool
- Integration tests: [Trait("Category", "Integration")], real DI ServiceCollection
- Load/Benchmark: .md justification with clear technical reasoning
- Test classes: sealed, inherit IAsyncLifetime where needed
- No Thread.Sleep — proper async patterns
- All tests independent, no shared mutable state

REFERENCE FILES:
- tests/Encina.UnitTests/Compliance/DPIA/DPIAErrorsTests.cs (unit test pattern)
- tests/Encina.GuardTests/Compliance/Anonymization/AnonymizationPipelineBehaviorGuardTests.cs (guard test pattern)
- tests/Encina.ContractTests/Compliance/Anonymization/ITokenMappingStoreContractTests.cs (contract test pattern)
- tests/Encina.PropertyTests/Compliance/Anonymization/TokenMappingPropertyTests.cs (property test pattern)
- tests/Encina.IntegrationTests/Compliance/DPIA/DPIAPipelineIntegrationTests.cs (integration test pattern)
```

</details>

---

### Phase 9: Documentation & Finalization

> **Goal**: Update all project documentation, verify build, and finalize.

<details>
<summary><strong>Tasks</strong></summary>

1. **XML documentation review** — Verify all public APIs have `<summary>`, `<remarks>`, `<param>`, `<returns>`, `<example>` where appropriate. All GDPR article references included.

2. **CHANGELOG.md** — Add entry under Unreleased:
   - `### Added`
   - `- Encina.Compliance.PrivacyByDesign — GDPR Privacy by Design enforcement (Article 25) with IPrivacyByDesignValidator, IDataMinimizationAnalyzer, DataMinimizationPipelineBehavior, [EnforceDataMinimization], [NotStrictlyNecessary], [PurposeLimitation], [PrivacyDefault] attributes, module-aware purpose registry, and minimization reporting (Fixes #411)`

3. **ROADMAP.md** — Update if milestone v0.13.0 is affected

4. **docs/INVENTORY.md** — Update with new package entry

5. **`PublicAPI.Unshipped.txt`** — Final review, ensure all public types listed

6. **Build verification**:
   - `dotnet build Encina.slnx --configuration Release` — 0 errors, 0 warnings
   - `dotnet test` — all tests pass

7. **Coverage check** — Verify ≥85% line coverage for the new package

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
You are implementing Phase 9 of Encina.Compliance.PrivacyByDesign (Issue #411).

CONTEXT:
- Phases 1-8 are fully implemented and tested
- Documentation and finalization remaining

TASK:
1. Review and complete XML documentation on all public APIs
2. Update CHANGELOG.md under ### Added in Unreleased section
3. Update ROADMAP.md if v0.13.0 milestone references this feature
4. Update docs/INVENTORY.md with new package entry
5. Final review of PublicAPI.Unshipped.txt
6. Build verification: dotnet build Encina.slnx --configuration Release → 0 errors, 0 warnings
7. Test verification: dotnet test → all pass, coverage ≥85%

KEY RULES:
- CHANGELOG.md: follows Keep a Changelog format
- INVENTORY.md: mark #411 as ✅ IMPLEMENTADO with comprehensive description
- Build must produce 0 errors and 0 warnings
- All tests must pass
- PublicAPI.Unshipped.txt must be complete and accurate
- Commit message: "feat: add Encina.Compliance.PrivacyByDesign — GDPR Privacy by Design enforcement (Art. 25) (Fixes #411)"
- NO Co-Authored-By or AI references in commit message
```

</details>

---

## Research

### GDPR Article References

| Article | Topic | Key Requirements |
|---------|-------|------------------|
| Art. 25(1) | Data protection by design | Implement appropriate technical measures at time of design and processing |
| Art. 25(2) | Data protection by default | Only personal data necessary for each specific purpose processed by default |
| Art. 25(3) | Certification | Approved certification mechanisms may demonstrate compliance |
| Recital 78 | Appropriate measures | Data minimization, pseudonymization, transparency, security functions |

### Privacy by Design Principles (Ann Cavoukian)

| Principle | Implementation in Package |
|-----------|--------------------------|
| Proactive not reactive | Pipeline behavior prevents violations before handler executes |
| Privacy as default | `[PrivacyDefault]` attribute enforces privacy-respecting defaults |
| Privacy embedded into design | Attributes on domain models make privacy requirements declarative |
| Full functionality (positive-sum) | Warn mode allows gradual adoption without blocking |
| End-to-end security | Integration with OpenTelemetry for full traceability |
| Visibility and transparency | MinimizationReport provides clear, actionable analysis |
| Respect for user privacy | Purpose limitation ensures data used only for declared purposes |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in PrivacyByDesign |
|-----------|----------|--------------------------|
| `IPipelineBehavior<,>` | `Encina` core | Pipeline behavior registration |
| `INotification` / `INotificationPublisher` | `Encina` core | Audit trail notifications |
| `IRequestContext` | `Encina` core | TenantId propagation |
| `IModuleExecutionContext` | `Encina` core (`Modules/Isolation/`) | Module context propagation (CurrentModule) |
| `EncinaErrors.Create()` | `Encina` core | Error factory pattern |
| `[ProcessesPersonalData]` | `Encina.Compliance.GDPR` | Optional enrichment context |
| `TimeProvider` | .NET 10 BCL | Testable time-dependent logic |
| `Either<EncinaError, T>` | `LanguageExt` | Railway Oriented Programming |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.GDPR` | 8100-8199 | Core GDPR |
| `Encina.Compliance.Consent` | 8200-8299 | Consent lifecycle |
| `Encina.Compliance.DataSubjectRights` | 8300-8399 | DSR lifecycle |
| `Encina.Compliance.Anonymization` | 8400-8499 | Anonymization |
| `Encina.Compliance.Retention` | 8500-8599 | Data retention |
| `Encina.Compliance.DataResidency` | 8600-8699 | Data residency |
| `Encina.Compliance.BreachNotification` | 8700-8799 | Breach notification |
| `Encina.Compliance.DPIA` | 8800-8899 | Impact assessments |
| `Encina.Compliance.ProcessorAgreements` | 8900-8999 | Processor agreements |
| **`Encina.Compliance.PrivacyByDesign`** | **9000-9099** | **Pipeline, minimization, purpose, defaults, health** |

### File Count Estimate

| Category | Files | Notes |
|----------|-------|-------|
| Core package (Phases 1-7) | ~28-33 | Models, interfaces, impls, diagnostics, DI, attributes, health |
| Tests — Unit | ~11-13 | Comprehensive unit coverage |
| Tests — Guard | ~4 | Null parameter validation |
| Tests — Contract | ~3 | Interface contract verification |
| Tests — Property | ~3 | FsCheck invariants |
| Tests — Integration | ~1 | Full pipeline lifecycle |
| Tests — Justifications | ~2 | Load + Benchmark .md files |
| Documentation | ~3-4 | CHANGELOG, INVENTORY, PublicAPI |
| **Total** | **~55-63** | |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing Encina.Compliance.PrivacyByDesign for Issue #411 — GDPR Privacy by Design Enforcement (Article 25).

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library for CQRS + Event Sourcing
- Pre-1.0: no backward compatibility needed, best solution always
- Railway Oriented Programming: Either<EncinaError, T> everywhere
- This is a STATELESS package — no database providers needed
- Follows the same patterns as Encina.Compliance.DPIA and Encina.Compliance.ProcessorAgreements
- Module Isolation is INCLUDED — uses IModuleExecutionContext for module-aware purpose registry

IMPLEMENTATION OVERVIEW:
New package: src/Encina.Compliance.PrivacyByDesign/
References: Encina (core), optionally Encina.Compliance.GDPR (for [ProcessesPersonalData])

Phase 1: Core models, enums, attributes (PrivacyByDesignEnforcementMode, PrivacyViolationType, PrivacyLevel,
         MinimizationReport, [EnforceDataMinimization], [NotStrictlyNecessary], [PurposeLimitation], [PrivacyDefault])
         PurposeDefinition includes ModuleId. Notifications include ModuleId.
Phase 2: Interfaces (IPrivacyByDesignValidator, IDataMinimizationAnalyzer, IPurposeRegistry with module-aware API) + PrivacyByDesignErrors
Phase 3: Default implementations (DefaultDataMinimizationAnalyzer, InMemoryPurposeRegistry with module scoping, DefaultPrivacyByDesignValidator)
Phase 4: DataMinimizationPipelineBehavior (enforcement point, attribute caching, three modes, IModuleExecutionContext optional dep)
Phase 5: Options (with module-scoped AddPurpose), DI registration
Phase 6: Cross-cutting integration (multi-tenancy, module isolation propagation, audit notifications, GDPR module)
Phase 7: Observability (ActivitySource, Meter, [LoggerMessage] event IDs 9000-9099, HealthCheck)
Phase 8: Testing (Unit ~11, Guard ~4, Contract ~3, Property ~3, Integration ~1, Load/Benchmark justification .md)
Phase 9: Documentation (CHANGELOG.md, INVENTORY.md, PublicAPI.Unshipped.txt, build verification)

KEY PATTERNS:
- All validation methods: ValueTask<Either<EncinaError, T>>
- Error codes: "pbd.{violation_type}" prefix
- Static ConcurrentDictionary<Type, T> for reflection caching (zero overhead after first call)
- Pipeline behavior: 3 enforcement modes (Block/Warn/Disabled), static attribute cache
- Module isolation: IModuleExecutionContext (optional) → CurrentModule propagated to traces, logs, notifications
- Purpose registry: module-aware — GetPurpose(name, moduleId?) looks up module-specific first, falls back to global
- Diagnostics: ActivitySource + Meter + [LoggerMessage] source generator, event IDs 9000-9099
- Health check: const DefaultName, Tags static array, scoped resolution via IServiceProvider.CreateScope()
- DI: TryAdd* pattern, AddEncinaPrivacyByDesign() extension method
- Options: sealed class with fluent AddPurpose() method (global and module-scoped overloads)
- Notifications: INotification records for audit trail (fire-and-forget via INotificationPublisher), include TenantId + ModuleId
- All public APIs: XML documentation with GDPR Art. 25 references
- No [Obsolete], no backward compatibility, no migration helpers

REFERENCE FILES:
- src/Encina.Compliance.DPIA/ (closest architectural reference — pipeline behavior, diagnostics, options, DI)
- src/Encina.Compliance.ProcessorAgreements/ (alternative reference — pipeline + notifications)
- src/Encina/Modules/Isolation/IModuleExecutionContext.cs (module context interface)
- src/Encina/Modules/Isolation/ModuleExecutionContext.cs (AsyncLocal implementation)
- tests/Encina.UnitTests/Compliance/DPIA/ (unit test patterns)
- tests/Encina.GuardTests/Compliance/ (guard test patterns)
- tests/Encina.IntegrationTests/Compliance/DPIA/ (integration test patterns)
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | **Caching** | ❌ N/A | Validation is stateless per-request; reflection results cached in static dictionaries (not ICacheProvider) |
| 2 | **OpenTelemetry** | ✅ Included | ActivitySource + Meter in Phase 7, traces in pipeline behavior |
| 3 | **Structured Logging** | ✅ Included | [LoggerMessage] source generator, event IDs 9000-9099, in Phase 7 |
| 4 | **Health Checks** | ✅ Included | PrivacyByDesignHealthCheck, conditional registration, in Phase 7 |
| 5 | **Validation** | ✅ Included | Core feature — data minimization, purpose limitation, default privacy |
| 6 | **Resilience** | ❌ N/A | In-process validation only, no external calls, no I/O, no network |
| 7 | **Distributed Locks** | ❌ N/A | Stateless validation with no shared mutable state; ConcurrentDictionary is thread-safe by design |
| 8 | **Transactions** | ❌ N/A | No state persistence, no database operations |
| 9 | **Idempotency** | ❌ N/A | Stateless pure function: same input always produces same output |
| 10 | **Multi-Tenancy** | ✅ Included | TenantId propagated to traces, logs, and notifications (Phase 6) |
| 11 | **Module Isolation** | ✅ Included | IModuleExecutionContext propagated to traces/logs/notifications; module-aware IPurposeRegistry with per-module purpose definitions (Phase 6) |
| 12 | **Audit Trail** | ✅ Included | PrivacyViolationDetectedNotification published on violations via INotificationPublisher (Phase 6) |
