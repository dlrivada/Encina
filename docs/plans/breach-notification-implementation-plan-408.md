# Implementation Plan: `Encina.Compliance.BreachNotification` — 72-Hour Data Breach Notification (Arts. 33-34)

> **Issue**: [#408](https://github.com/dlrivada/Encina/issues/408)
> **Type**: Feature
> **Complexity**: High (10 phases, 13 database providers, ~120 files)
> **Estimated Scope**: ~4,500-6,000 lines of production code + ~3,000-4,000 lines of tests

---

## Summary

Implement automated data breach detection and notification management covering GDPR Articles 33-34. This package provides breach detection via a pluggable rule engine, 72-hour deadline tracking for supervisory authority notification, phased reporting support, data subject notification (Art. 34), breach record persistence with full audit trail, and a `BreachDetectionPipelineBehavior` that evaluates security events against registered detection rules.

The implementation follows the same satellite-provider architecture established by `Encina.Compliance.Retention` and `Encina.Compliance.DataResidency`, delivering store implementations across all 13 database providers with dedicated observability, health checks, and deadline monitoring.

**Provider category**: Database (13 providers) — `IBreachRecordStore` and `IBreachAuditStore` require persistence across ADO.NET (×4), Dapper (×4), EF Core (×4), and MongoDB (×1).

---

## Design Choices

<details>
<summary><strong>1. Package Placement — New <code>Encina.Compliance.BreachNotification</code> package</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New `Encina.Compliance.BreachNotification` package** | Clean separation, own detection engine, own pipeline, own observability, independent versioning | New NuGet package, more projects to maintain |
| **B) Extend `Encina.Compliance.GDPR`** | Single package | Bloats GDPR core (~60+ files already), breach notification is a distinct compliance domain with its own lifecycle |
| **C) Extend `Encina.Security.Audit`** | Leverages existing audit store | Mixes security auditing with compliance notification — different domains and SLAs (audit is passive, breach is time-critical) |

### Chosen Option: **A — New `Encina.Compliance.BreachNotification` package**

### Rationale

- Breach notification spans 2 GDPR articles (33-34) with its own domain model (breach records, detection rules, severity, phased reports), pipeline behavior (detection), and observability
- Follows the established pattern: `Encina.Compliance.Consent` (Art. 7), `Encina.Compliance.DataSubjectRights` (Arts. 15-22), `Encina.Compliance.Retention` (Art. 5(1)(e)) are all separate packages
- Keeps `Encina.Compliance.GDPR` focused on core compliance (processing activities, lawful basis, RoPA)
- Optional integration with `Encina.Security.Audit` for security event ingestion — but no hard dependency
- Satellite providers add breach stores in their existing `BreachNotification/` subfolder

</details>

<details>
<summary><strong>2. Breach Record Model — Entity-based tracking with status machine and phased reporting</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Entity-based tracking with status enum and phased report support** | Queryable, fits 13-provider pattern, supports phased reporting, simple deadlines | Limited formal state machine validation |
| **B) Full saga/state machine (Encina.Messaging saga)** | Formal state transitions, compensation logic | Over-engineered — breach notification is a lifecycle tracker, not a distributed transaction |
| **C) Event-sourced breach log** | Complete immutable history | Requires event store, incompatible with CRUD providers, excessive for breach tracking |

### Chosen Option: **A — Entity-based tracking with status enum and phased reporting**

### Rationale

- A `BreachRecord` domain record tracks each breach with all Art. 33(3) required fields: Nature, ApproximateSubjectsAffected, CategoriesOfDataAffected, DPOContactDetails, LikelyConsequences, MeasuresTaken
- `BreachStatus` enum: `Detected`, `Investigating`, `AuthorityNotified`, `SubjectsNotified`, `Resolved`, `Closed`
- `BreachSeverity` enum: `Low`, `Medium`, `High`, `Critical`
- 72-hour deadline calculated at detection: `NotificationDeadlineUtc = DetectedAtUtc.AddHours(72)` (Art. 33(1))
- Phased reporting: `IReadOnlyList<PhasedReport>` attached to breach record — allows submitting initial report then supplementing (Art. 33(4))
- `IBreachRecordStore` provides CRUD + deadline queries + status updates
- Audit trail via separate `BreachAuditEntry` records (same pattern as `RetentionAuditEntry`)
- Status transitions validated in domain layer, not in store

</details>

<details>
<summary><strong>3. Detection Engine — Pluggable rule engine with composite evaluator</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Pluggable rule engine with `IBreachDetectionRule` and composite evaluator** | Extensible, users add custom rules, built-in rules for common patterns, testable | Requires rule registration |
| **B) Single monolithic detector** | Simple, one class | Violates OCP, hard to customize per application |
| **C) External SIEM integration only** | Leverages existing infrastructure | No application-level breach detection, depends on external tooling |

### Chosen Option: **A — Pluggable rule engine with composite evaluator**

### Rationale

- `IBreachDetectionRule` interface with single `EvaluateAsync(SecurityEvent, CancellationToken)` → `Either<EncinaError, PotentialBreach?>`
- `DefaultBreachDetector` implements `IBreachDetector` — iterates registered rules, aggregates results
- Four built-in rules:
  - `UnauthorizedAccessRule` — detects repeated failed authentication attempts
  - `MassDataExfiltrationRule` — detects large-volume data access patterns
  - `PrivilegeEscalationRule` — detects unauthorized privilege changes
  - `AnomalousQueryPatternRule` — detects unusual database query patterns
- Rules are registered via options: `options.AddDetectionRule<MyCustomRule>()`
- `SecurityEvent` is the input model — applications publish these for evaluation
- Detection can run via pipeline behavior or via explicit `IBreachDetector.DetectAsync()` call

</details>

<details>
<summary><strong>4. Notification Strategy — Pluggable notifiers with domain events</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IBreachNotifier` interface with default implementation + domain notifications** | Pluggable (email, webhook, API), testable, uses Encina notification pipeline | Strategy interface, more DI |
| **B) Hardcoded email notification** | Simple | Not extensible, assumes email infrastructure |
| **C) Outbox-only notification** | Reliable delivery | Requires outbox setup, too heavy for a notification interface |

### Chosen Option: **A — Pluggable notifier with domain events**

### Rationale

- `IBreachNotifier` with two methods: `NotifyAuthorityAsync(BreachRecord)` and `NotifyDataSubjectsAsync(BreachRecord, subjectIds)`
- `DefaultBreachNotifier` is a no-op that logs the notification (users implement their own: email, webhook, compliance API)
- Publishes Encina domain notifications: `AuthorityNotifiedNotification`, `SubjectsNotifiedNotification`, `BreachDetectedNotification`, `BreachResolvedNotification`
- Integrates with existing Outbox pattern for reliable delivery if configured
- Audit trail: every notification recorded as `BreachAuditEntry`
- `BreachNotificationOptions.AutoNotifyOnHighSeverity` flag controls automatic authority notification for High/Critical breaches

</details>

<details>
<summary><strong>5. Deadline Monitoring — Background service with configurable alerts</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IHostedService` background service with configurable alert thresholds** | Proactive monitoring, publishes deadline warnings, fits .NET hosting model | Background thread, needs careful lifecycle management |
| **B) On-demand deadline check only** | Simple, no background service | Risks missing 72-hour deadline if not checked |
| **C) Integration with Encina.Scheduling** | Leverages existing scheduling | Creates dependency on messaging infrastructure |

### Chosen Option: **A — Background service with configurable alert thresholds**

### Rationale

- `BreachDeadlineMonitorService : IHostedService` runs periodic checks (configurable interval, default 15 min)
- Alerts at configurable remaining-hour thresholds: `[48, 24, 12, 6, 1]` (matching issue spec)
- Publishes `DeadlineWarningNotification` at each threshold
- Logs urgent warnings when < 6 hours remaining
- Marks breaches as `Overdue` when deadline passes without authority notification
- Uses `TimeProvider` for testable time logic
- Optional: disabled by default, enabled via `options.EnableDeadlineMonitoring = true`

</details>

<details>
<summary><strong>6. Pipeline Behavior — <code>BreachDetectionPipelineBehavior</code></strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Dedicated pipeline behavior that evaluates security events** | Automatic breach detection in request pipeline, follows existing patterns | Additional overhead per request |
| **B) Manual-only breach detection (explicit API calls)** | Zero pipeline overhead | Easy to miss breaches, no automated detection |
| **C) Middleware (ASP.NET Core only)** | Runs early in HTTP pipeline | Tied to ASP.NET, doesn't work for non-HTTP commands |

### Chosen Option: **A — Dedicated pipeline behavior**

### Rationale

- `BreachDetectionPipelineBehavior<TRequest, TResponse>` checks if the request or its response indicates a potential breach
- Only activates for requests marked with `[BreachMonitored]` attribute — skips all others (zero overhead for unmarked requests)
- Static per-generic-type attribute caching (same pattern as `LawfulBasisValidationPipelineBehavior`, `RetentionValidationPipelineBehavior`)
- When activated: wraps `nextStep()`, inspects response for error patterns, publishes `SecurityEvent` to detection engine
- Three enforcement modes: `Block` (halt on detected breach), `Warn` (log + continue), `Disabled` (skip)
- Lightweight: only creates a `SecurityEvent` from request metadata — the actual detection happens asynchronously via `IBreachDetector`

</details>

<details>
<summary><strong>7. Art. 34 Data Subject Notification — Risk-based threshold</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Risk-based threshold with configurable severity gate** | Matches Art. 34(1) "high risk" requirement, configurable per application | Requires severity assessment |
| **B) Always notify all subjects** | Simple | Over-notification, may not be required per Art. 34(3) exemptions |
| **C) Manual decision only** | Full control | Risks non-compliance if humans delay |

### Chosen Option: **A — Risk-based threshold with configurable severity gate**

### Rationale

- Art. 34(1) requires data subject notification only when breach "is likely to result in a high risk to the rights and freedoms of natural persons"
- `BreachNotificationOptions.SubjectNotificationSeverityThreshold` — default: `BreachSeverity.High`
- Breaches meeting or exceeding threshold automatically trigger subject notification workflow
- Art. 34(3) exemptions tracked: `SubjectNotificationExemption` enum with `EncryptionProtected`, `MitigatingMeasures`, `DisproportionateEffort`
- Exemption requires public communication instead of individual notification
- `IBreachNotifier.NotifyDataSubjectsAsync` called by handler when threshold met and no exemption applies

</details>

---

## Implementation Phases

### Phase 1: Core Models, Enums & Domain Records

> **Goal**: Establish the foundational types that all other phases depend on.

<details>
<summary><strong>Tasks</strong></summary>

#### New project: `src/Encina.Compliance.BreachNotification/`

1. **Create project file** `Encina.Compliance.BreachNotification.csproj`
   - Target: `net10.0`
   - Dependencies: `Encina` (core), `LanguageExt.Core`, `Microsoft.Extensions.Logging.Abstractions`, `Microsoft.Extensions.Options`
   - No hard dependency on `Encina.Compliance.GDPR` or `Encina.Security.Audit` — optional integration
   - Enable nullable, implicit usings, XML doc

2. **Enums** (`Model/` folder):
   - `BreachSeverity` — `Low`, `Medium`, `High`, `Critical`
   - `BreachStatus` — `Detected`, `Investigating`, `AuthorityNotified`, `SubjectsNotified`, `Resolved`, `Closed`
   - `SecurityEventType` — `UnauthorizedAccess`, `DataExfiltration`, `PrivilegeEscalation`, `AnomalousQuery`, `DataModification`, `SystemIntrusion`, `MalwareDetected`, `InsiderThreat`, `Custom`
   - `SubjectNotificationExemption` — `None`, `EncryptionProtected`, `MitigatingMeasures`, `DisproportionateEffort` (Art. 34(3))
   - `NotificationOutcome` — `Sent`, `Failed`, `Pending`, `Exempted`
   - `BreachDetectionEnforcementMode` — `Block`, `Warn`, `Disabled`

3. **Domain records** (`Model/` folder):
   - `BreachRecord` — sealed record with all Art. 33(3) required fields:
     - `Id (Guid)`, `Nature (string)`, `ApproximateSubjectsAffected (int)`, `CategoriesOfDataAffected (IReadOnlyList<string>)`, `DPOContactDetails (string)`, `LikelyConsequences (string)`, `MeasuresTaken (string)`
     - `DetectedAtUtc (DateTimeOffset)`, `NotificationDeadlineUtc (DateTimeOffset)`, `NotifiedAuthorityAtUtc (DateTimeOffset?)`, `NotifiedSubjectsAtUtc (DateTimeOffset?)`
     - `Severity (BreachSeverity)`, `Status (BreachStatus)`
     - `PhasedReports (IReadOnlyList<PhasedReport>)`, `DelayReason (string?)`
     - `SubjectNotificationExemption (SubjectNotificationExemption)`
     - `ResolvedAtUtc (DateTimeOffset?)`, `ResolutionSummary (string?)`
   - `PhasedReport` — sealed record: `Id (Guid)`, `BreachId (Guid)`, `ReportNumber (int)`, `Content (string)`, `SubmittedAtUtc (DateTimeOffset)`, `SubmittedByUserId (string?)`
   - `SecurityEvent` — sealed record: `Id (Guid)`, `EventType (SecurityEventType)`, `Source (string)`, `Description (string)`, `OccurredAtUtc (DateTimeOffset)`, `UserId (string?)`, `IpAddress (string?)`, `AffectedEntityType (string?)`, `AffectedEntityId (string?)`, `Metadata (IReadOnlyDictionary<string, object?>?)`
   - `PotentialBreach` — sealed record: `DetectionRuleName (string)`, `Severity (BreachSeverity)`, `Description (string)`, `SecurityEvent (SecurityEvent)`, `DetectedAtUtc (DateTimeOffset)`, `RecommendedActions (IReadOnlyList<string>?)`
   - `NotificationResult` — sealed record: `Outcome (NotificationOutcome)`, `SentAtUtc (DateTimeOffset?)`, `Recipient (string?)`, `ErrorMessage (string?)`, `BreachId (Guid)`
   - `DeadlineStatus` — sealed record: `BreachId (Guid)`, `DetectedAtUtc (DateTimeOffset)`, `DeadlineUtc (DateTimeOffset)`, `RemainingHours (double)`, `IsOverdue (bool)`, `Status (BreachStatus)`
   - `BreachAuditEntry` — sealed record: `Id (Guid)`, `BreachId (Guid)`, `Action (string)`, `Detail (string?)`, `PerformedByUserId (string?)`, `OccurredAtUtc (DateTimeOffset)`

4. **Notification records** (`Notifications/` folder):
   - `BreachDetectedNotification` — sealed record implementing `INotification`: `BreachId`, `Severity`, `Nature`, `OccurredAtUtc`
   - `AuthorityNotifiedNotification` — sealed record implementing `INotification`: `BreachId`, `NotifiedAtUtc`, `Authority`
   - `SubjectsNotifiedNotification` — sealed record implementing `INotification`: `BreachId`, `NotifiedAtUtc`, `SubjectCount`
   - `DeadlineWarningNotification` — sealed record implementing `INotification`: `BreachId`, `RemainingHours`, `DeadlineUtc`
   - `BreachResolvedNotification` — sealed record implementing `INotification`: `BreachId`, `ResolvedAtUtc`, `ResolutionSummary`

5. **`PublicAPI.Unshipped.txt`** — Add all public types

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of Encina.Compliance.BreachNotification (Issue #408).

CONTEXT:
- Encina is a .NET 10 / C# 14 library using Railway Oriented Programming (Either<EncinaError, T>)
- This is a NEW project: src/Encina.Compliance.BreachNotification/
- Reference existing patterns in src/Encina.Compliance.Retention/Model/ and src/Encina.Compliance.DataResidency/Model/
- All domain models are sealed records with XML documentation
- Use LanguageExt for Option<T> and Either<L, R>
- Timestamps use DateTimeOffset with AtUtc suffix convention

TASK:
Create the project file and all model types listed in Phase 1 Tasks.

KEY RULES:
- Target net10.0, enable nullable, enable implicit usings
- All types are sealed records (not classes)
- All public types need XML documentation with <summary>, <remarks>, and GDPR article references (Art. 33 and Art. 34)
- BreachRecord.NotificationDeadlineUtc = DetectedAtUtc.AddHours(72) — enforced in factory/constructor
- PhasedReport supports Art. 33(4): "Where, and in so far as, it is not possible to provide the information at the same time, the information may be provided in phases"
- SecurityEvent is the input for breach detection rules — applications create these from their security infrastructure
- NotificationResult tracks outcome of notification attempts (authority + data subjects)
- Notification records implement INotification from Encina core
- BreachRecord must NOT have a hard dependency on Encina.Security.Audit — optional integration via events
- Add PublicAPI.Unshipped.txt and PublicAPI.Shipped.txt with all public symbols

REFERENCE FILES:
- src/Encina.Compliance.Retention/Model/RetentionRecord.cs (sealed record pattern)
- src/Encina.Compliance.Retention/Model/RetentionStatus.cs (enum pattern)
- src/Encina.Compliance.Retention/Notifications/ (notification record pattern)
- src/Encina.Compliance.DataResidency/Model/ (domain model pattern)
```

</details>

---

### Phase 2: Core Interfaces & Error Codes

> **Goal**: Define the public API surface — interfaces, attributes, and error codes.

<details>
<summary><strong>Tasks</strong></summary>

1. **Attributes** (`Attributes/` folder):
   - `BreachMonitoredAttribute` — `[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]`
     - Marker attribute for requests that should be evaluated by the breach detection pipeline behavior
     - Properties: `SecurityEventType EventType { get; set; }` — which event type to generate from this request (default: `Custom`)

2. **Core interfaces** (`Abstractions/` folder):
   - `IBreachDetector` — breach detection orchestrator:
     - `DetectAsync(SecurityEvent securityEvent, CancellationToken)` → `Either<EncinaError, IReadOnlyList<PotentialBreach>>`
     - `RegisterDetectionRule(IBreachDetectionRule rule)` — add rules at runtime
     - `GetRegisteredRulesAsync(CancellationToken)` → `Either<EncinaError, IReadOnlyList<string>>` (rule names)
   - `IBreachDetectionRule` — individual detection rule:
     - `string Name { get; }` — unique name
     - `EvaluateAsync(SecurityEvent securityEvent, CancellationToken)` → `Either<EncinaError, PotentialBreach?>`
   - `IBreachNotifier` — notification dispatcher:
     - `NotifyAuthorityAsync(BreachRecord breach, CancellationToken)` → `Either<EncinaError, NotificationResult>`
     - `NotifyDataSubjectsAsync(BreachRecord breach, IEnumerable<string> subjectIds, CancellationToken)` → `Either<EncinaError, NotificationResult>`
   - `IBreachRecordStore` — CRUD + queries:
     - `RecordBreachAsync(BreachRecord breach, CancellationToken)` → `Either<EncinaError, Unit>`
     - `GetBreachAsync(Guid breachId, CancellationToken)` → `Either<EncinaError, Option<BreachRecord>>`
     - `UpdateBreachAsync(BreachRecord breach, CancellationToken)` → `Either<EncinaError, Unit>`
     - `GetBreachesByStatusAsync(BreachStatus status, CancellationToken)` → `Either<EncinaError, IReadOnlyList<BreachRecord>>`
     - `GetOverdueBreachesAsync(CancellationToken)` → `Either<EncinaError, IReadOnlyList<BreachRecord>>` (deadline passed, not yet notified)
     - `GetApproachingDeadlineAsync(int hoursRemaining, CancellationToken)` → `Either<EncinaError, IReadOnlyList<DeadlineStatus>>`
     - `AddPhasedReportAsync(Guid breachId, PhasedReport report, CancellationToken)` → `Either<EncinaError, Unit>`
     - `GetAllAsync(CancellationToken)` → `Either<EncinaError, IReadOnlyList<BreachRecord>>`
   - `IBreachAuditStore` — audit trail:
     - `RecordAsync(BreachAuditEntry entry, CancellationToken)` → `Either<EncinaError, Unit>`
     - `GetAuditTrailAsync(Guid breachId, CancellationToken)` → `Either<EncinaError, IReadOnlyList<BreachAuditEntry>>`
   - `IBreachHandler` — high-level orchestrator:
     - `HandleDetectedBreachAsync(PotentialBreach breach, CancellationToken)` → `Either<EncinaError, BreachRecord>`
     - `NotifyAuthorityAsync(Guid breachId, CancellationToken)` → `Either<EncinaError, NotificationResult>`
     - `NotifySubjectsAsync(Guid breachId, IEnumerable<string> subjectIds, CancellationToken)` → `Either<EncinaError, NotificationResult>`
     - `AddPhasedReportAsync(Guid breachId, string content, string? userId, CancellationToken)` → `Either<EncinaError, PhasedReport>`
     - `ResolveBreachAsync(Guid breachId, string resolutionSummary, CancellationToken)` → `Either<EncinaError, Unit>`
     - `GetDeadlineStatusAsync(Guid breachId, CancellationToken)` → `Either<EncinaError, DeadlineStatus>`

3. **Error codes** (`BreachNotificationErrors.cs`):
   - Error code prefix: `breach.`
   - Codes: `breach.not_found`, `breach.already_exists`, `breach.already_resolved`, `breach.detection_failed`, `breach.notification_failed`, `breach.authority_notification_failed`, `breach.subject_notification_failed`, `breach.deadline_expired`, `breach.store_error`, `breach.invalid_parameter`, `breach.rule_evaluation_failed`, `breach.phased_report_failed`, `breach.exemption_invalid`
   - Follow `RetentionErrors.cs` pattern: `public static class BreachNotificationErrors` with factory methods

4. **`PublicAPI.Unshipped.txt`** — Update with all new public symbols

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of Encina.Compliance.BreachNotification (Issue #408).

CONTEXT:
- Phase 1 models are already implemented in src/Encina.Compliance.BreachNotification/Model/
- Encina uses Railway Oriented Programming: all store/handler methods return ValueTask<Either<EncinaError, T>>
- LanguageExt provides Option<T>, Either<L, R>, Unit
- Error codes follow the pattern in src/Encina.Compliance.Retention/RetentionErrors.cs

TASK:
Create all interfaces, attributes, and error codes listed in Phase 2 Tasks.

KEY RULES:
- [BreachMonitored] targets classes (AttributeTargets.Class) — marker for pipeline behavior
- All interface methods take CancellationToken as last parameter with default value
- IBreachRecordStore.GetApproachingDeadlineAsync must be efficient (used by deadline monitor)
- IBreachDetector.DetectAsync iterates all registered rules — one SecurityEvent, multiple rules
- IBreachNotifier has two separate methods: authority (Art. 33) and subjects (Art. 34)
- BreachNotificationErrors follows the EXACT RetentionErrors pattern: static class, factory methods, EncinaErrors.Create(...)
- All interfaces need comprehensive XML documentation with GDPR Article 33/34 references
- IBreachRecordStore must support phased reporting via AddPhasedReportAsync (Art. 33(4))

REFERENCE FILES:
- src/Encina.Compliance.Retention/RetentionErrors.cs (error factory pattern)
- src/Encina.Compliance.Retention/Abstractions/IRetentionRecordStore.cs (store interface pattern)
- src/Encina.Compliance.Retention/Abstractions/IRetentionEnforcer.cs (orchestrator interface)
- src/Encina.Compliance.DataResidency/Abstractions/ (interface naming pattern)
```

</details>

---

### Phase 3: Default Implementations & In-Memory Stores

> **Goal**: Provide working implementations for development/testing without database dependencies.

<details>
<summary><strong>Tasks</strong></summary>

1. **In-memory stores** (`InMemory/` folder):
   - `InMemoryBreachRecordStore : IBreachRecordStore` — `ConcurrentDictionary<Guid, BreachRecord>`, `TimeProvider` for deadline queries
   - `InMemoryBreachAuditStore : IBreachAuditStore` — `ConcurrentDictionary<Guid, List<BreachAuditEntry>>`
   - Both follow pattern from `InMemoryRetentionRecordStore`, `InMemoryRetentionAuditStore`

2. **Breach detector** (`Detection/` folder):
   - `DefaultBreachDetector : IBreachDetector` — iterates `IEnumerable<IBreachDetectionRule>`, aggregates results from all rules
   - Thread-safe rule registration via `ConcurrentBag<IBreachDetectionRule>`
   - Logs each rule evaluation via structured logging

3. **Built-in detection rules** (`Detection/Rules/` folder):
   - `UnauthorizedAccessRule : IBreachDetectionRule` — evaluates `SecurityEventType.UnauthorizedAccess` events against configurable threshold (e.g., 5 failed attempts from same IP)
   - `MassDataExfiltrationRule : IBreachDetectionRule` — evaluates `SecurityEventType.DataExfiltration` events, detects large volume data access
   - `PrivilegeEscalationRule : IBreachDetectionRule` — evaluates `SecurityEventType.PrivilegeEscalation` events
   - `AnomalousQueryPatternRule : IBreachDetectionRule` — evaluates `SecurityEventType.AnomalousQuery` events, detects unusual query volumes

4. **Breach notifier** (`DefaultBreachNotifier.cs`):
   - `DefaultBreachNotifier : IBreachNotifier` — logs notification (no-op for actual delivery)
   - Users override with their own implementation (email, webhook, compliance API, etc.)
   - Returns `NotificationResult` with `Outcome.Sent` and timestamp

5. **Default handler** (`DefaultBreachHandler.cs`):
   - Implements `IBreachHandler`
   - Orchestrates the full breach lifecycle:
     - `HandleDetectedBreachAsync`: create record, set 72h deadline, publish `BreachDetectedNotification`, record audit entry
     - `NotifyAuthorityAsync`: call `IBreachNotifier.NotifyAuthorityAsync`, update status, record audit, publish `AuthorityNotifiedNotification`
     - `NotifySubjectsAsync`: check severity threshold + exemptions, call `IBreachNotifier.NotifyDataSubjectsAsync`, update status, record audit, publish `SubjectsNotifiedNotification`
     - `AddPhasedReportAsync`: add report to breach record, record audit entry
     - `ResolveBreachAsync`: update status to Resolved, record audit, publish `BreachResolvedNotification`
     - `GetDeadlineStatusAsync`: calculate remaining hours from `TimeProvider`
   - Dependencies: `IBreachRecordStore`, `IBreachAuditStore`, `IBreachNotifier`, `IOptions<BreachNotificationOptions>`, `ILogger`, `TimeProvider`

6. **`PublicAPI.Unshipped.txt`** — Update with all new public types

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of Encina.Compliance.BreachNotification (Issue #408).

CONTEXT:
- Phase 1 (models) and Phase 2 (interfaces, attributes, errors) are already implemented
- Encina uses ROP: all methods return ValueTask<Either<EncinaError, T>>
- In-memory stores use ConcurrentDictionary for thread safety
- TimeProvider is injected for testable time-dependent logic

TASK:
Create all default implementations listed in Phase 3 Tasks.

KEY RULES:
- InMemoryBreachRecordStore uses ConcurrentDictionary<Guid, BreachRecord> with immutable record updates
- GetApproachingDeadlineAsync calculates remaining hours using TimeProvider.GetUtcNow()
- GetOverdueBreachesAsync: NotificationDeadlineUtc < NowUtc AND Status NOT IN (AuthorityNotified, SubjectsNotified, Resolved, Closed)
- DefaultBreachDetector iterates ALL registered rules for each SecurityEvent — a single event can match multiple rules
- Built-in rules use configurable thresholds from BreachNotificationOptions (e.g., UnauthorizedAccessThreshold = 5)
- DefaultBreachNotifier is a no-op logging implementation — real notification is user-provided
- DefaultBreachHandler is the main orchestrator — each method:
  1. Validates input
  2. Records audit entry (started)
  3. Executes the operation
  4. Updates breach status
  5. Records audit entry (completed/failed)
  6. Publishes notification if applicable
- 72-hour deadline: BreachRecord.NotificationDeadlineUtc = DetectedAtUtc.AddHours(72)
- All constructors validate parameters with ArgumentNullException.ThrowIfNull

REFERENCE FILES:
- src/Encina.Compliance.Retention/InMemory/InMemoryRetentionRecordStore.cs (in-memory store pattern)
- src/Encina.Compliance.Retention/InMemory/InMemoryRetentionAuditStore.cs (audit store pattern)
- src/Encina.Compliance.Retention/DefaultRetentionEnforcer.cs (orchestrator pattern)
```

</details>

---

### Phase 4: Pipeline Behavior — `BreachDetectionPipelineBehavior`

> **Goal**: Implement the pipeline behavior that evaluates requests for potential breach indicators.

<details>
<summary><strong>Tasks</strong></summary>

1. **`BreachDetectionPipelineBehavior<TRequest, TResponse>`** (`BreachDetectionPipelineBehavior.cs`):
   - Implements `IPipelineBehavior<TRequest, TResponse>` where `TRequest : IRequest<TResponse>`
   - Static per-generic-type attribute caching (same pattern as `RetentionValidationPipelineBehavior`):
     - `private static readonly BreachMonitoredInfo? CachedAttributeInfo = ResolveAttributeInfo()`
     - Checks for `[BreachMonitored]` attribute on `TRequest`
   - `Handle` method flow:
     1. If `EnforcementMode == Disabled` → `nextStep()`
     2. If `CachedAttributeInfo is null` → `nextStep()` (not a monitored request)
     3. Execute `nextStep()` to get the response
     4. Create `SecurityEvent` from request metadata + response outcome
     5. Call `IBreachDetector.DetectAsync(securityEvent)` — fire-and-forget style (don't block response)
     6. If breaches detected + Block mode → return `Left(BreachNotificationErrors.BreachDetected(...))`
     7. If breaches detected + Warn mode → log warning + return response
     8. If no breaches → return response
   - Observability: traces via `BreachNotificationDiagnostics`, counters, structured logging

2. **`BreachMonitoredInfo`** (private sealed record inside behavior):
   - `SecurityEventType EventType`, `string Source`

3. **`SecurityEventFactory`** (`Detection/SecurityEventFactory.cs`):
   - `internal static class SecurityEventFactory`
   - `Create(TRequest request, BreachMonitoredInfo info, TimeProvider timeProvider)` → `SecurityEvent`
   - Extracts metadata from request: user ID, IP address, entity type/id
   - Uses reflection (cached) for property extraction

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of Encina.Compliance.BreachNotification (Issue #408).

CONTEXT:
- Phases 1-3 are already implemented (models, interfaces, in-memory stores, default handler, detector, notifier)
- This pipeline behavior follows the EXACT same pattern as RetentionValidationPipelineBehavior
- It intercepts requests to evaluate them for potential breach indicators

TASK:
Create BreachDetectionPipelineBehavior<TRequest, TResponse> and SecurityEventFactory.

KEY RULES:
- Use static per-generic-type attribute caching: private static readonly BreachMonitoredInfo? CachedAttributeInfo
- ResolveAttributeInfo() checks for [BreachMonitored] on typeof(TRequest)
- Pipeline executes nextStep() FIRST, then evaluates the result for breach indicators
- Detection should NOT block the response in Warn mode — only log and continue
- In Block mode, detection result determines whether to return the original response or Left(error)
- SecurityEventFactory creates SecurityEvent from request metadata (cached reflection for property access)
- Three enforcement modes: Block returns Left(BreachNotificationErrors.BreachDetected(...)), Warn logs + continues, Disabled skips
- All constructor parameters validated with ArgumentNullException.ThrowIfNull
- Dependencies: IBreachDetector, IBreachHandler, IOptions<BreachNotificationOptions>, ILogger, TimeProvider

REFERENCE FILES:
- src/Encina.Compliance.Retention/RetentionValidationPipelineBehavior.cs (EXACT pattern to follow)
- src/Encina.Compliance.DataResidency/DataResidencyPipelineBehavior.cs (alternative pipeline pattern)
```

</details>

---

### Phase 5: Configuration, DI & Deadline Monitoring

> **Goal**: Wire everything together with options, service registration, deadline monitoring service, and health check.

<details>
<summary><strong>Tasks</strong></summary>

1. **Options** (`BreachNotificationOptions.cs`):
   - `BreachDetectionEnforcementMode EnforcementMode { get; set; }` — default: `Warn`
   - `int NotificationDeadlineHours { get; set; }` — default: `72` (Art. 33(1))
   - `int[] AlertAtHoursRemaining { get; set; }` — default: `[48, 24, 12, 6, 1]`
   - `string? SupervisoryAuthority { get; set; }` — e.g., `"dpc@dataprotection.ie"`
   - `bool AutoNotifyOnHighSeverity { get; set; }` — default: `false`
   - `bool PhasedReportingEnabled { get; set; }` — default: `true`
   - `BreachSeverity SubjectNotificationSeverityThreshold { get; set; }` — default: `BreachSeverity.High`
   - `bool EnableDeadlineMonitoring { get; set; }` — default: `false`
   - `TimeSpan DeadlineCheckInterval { get; set; }` — default: `TimeSpan.FromMinutes(15)`
   - `bool AddHealthCheck { get; set; }` — default: `false`
   - `bool TrackAuditTrail { get; set; }` — default: `true`
   - `bool PublishNotifications { get; set; }` — default: `true`
   - `List<Assembly> AssembliesToScan { get; }` — default: `[]`
   - Internal rule storage: `internal List<Type> DetectionRuleTypes { get; } = [];`
   - Fluent API: `AddDetectionRule<TRule>()` where `TRule : IBreachDetectionRule`

2. **Options validator** (`BreachNotificationOptionsValidator.cs`):
   - `IValidateOptions<BreachNotificationOptions>`
   - Validates: `NotificationDeadlineHours > 0`, `AlertAtHoursRemaining` values positive and < deadline, `DeadlineCheckInterval > 0`

3. **Service collection extensions** (`ServiceCollectionExtensions.cs`):
   - `AddEncinaBreachNotification(this IServiceCollection services, Action<BreachNotificationOptions>? configure = null)`
   - Registers:
     - `BreachNotificationOptions` via `services.Configure()`
     - `BreachNotificationOptionsValidator` via `TryAddSingleton<IValidateOptions<>>`
     - `TimeProvider.System` via `TryAddSingleton`
     - `IBreachRecordStore` → `InMemoryBreachRecordStore` via `TryAddSingleton`
     - `IBreachAuditStore` → `InMemoryBreachAuditStore` via `TryAddSingleton`
     - `IBreachDetector` → `DefaultBreachDetector` via `TryAddSingleton`
     - `IBreachNotifier` → `DefaultBreachNotifier` via `TryAddSingleton`
     - `IBreachHandler` → `DefaultBreachHandler` via `TryAddScoped`
     - `BreachDetectionPipelineBehavior<,>` via `TryAddTransient(typeof(IPipelineBehavior<,>))`
     - All configured detection rules from `DetectionRuleTypes`
   - Conditional: health check if `AddHealthCheck == true`
   - Conditional: deadline monitoring if `EnableDeadlineMonitoring == true`

4. **Deadline monitoring service** (`BreachDeadlineMonitorService.cs`):
   - `internal sealed class BreachDeadlineMonitorService : BackgroundService`
   - `ExecuteAsync`: periodic loop using `PeriodicTimer(options.DeadlineCheckInterval)`
   - Each tick: query `IBreachRecordStore.GetApproachingDeadlineAsync` for each alert threshold
   - Publish `DeadlineWarningNotification` at each threshold
   - Mark breaches as overdue when deadline passes
   - Uses scoped `IServiceProvider` for store resolution
   - Logs via dedicated event IDs

5. **Health check** (`Health/BreachNotificationHealthCheck.cs`):
   - `public sealed class BreachNotificationHealthCheck : IHealthCheck`
   - `const string DefaultName = "encina-breach-notification"`
   - Tags: `["encina", "gdpr", "breach", "compliance", "ready"]`
   - Checks: `IBreachRecordStore` resolvable, `IBreachDetector` resolvable, overdue breaches count, `IBreachNotifier` resolvable
   - Warns (Degraded): overdue breaches > 0, approaching deadline breaches
   - Uses scoped resolution pattern (same as `RetentionHealthCheck`)

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of Encina.Compliance.BreachNotification (Issue #408).

CONTEXT:
- Phases 1-4 are implemented (models, interfaces, in-memory stores, handler, detector, notifier, pipeline behavior)
- DI registration follows the TryAdd pattern — satellite providers pre-register concrete stores
- Deadline monitoring uses BackgroundService with PeriodicTimer
- Health checks use IServiceProvider.CreateScope() for scoped service resolution

TASK:
Create options, DI registration, deadline monitoring service, and health check.

KEY RULES:
- Options pattern: sealed class with defaults, IValidateOptions<T> for validation
- ServiceCollectionExtensions uses TryAdd* for all registrations (satellite overrides)
- Instantiate a local optionsInstance to read flags before DI is fully built (for conditional registration)
- DetectionRuleTypes stores rule types for DI registration — AddDetectionRule<TRule>() adds to this list
- BackgroundService pattern: PeriodicTimer in ExecuteAsync, scoped IServiceProvider for store access
- Health check returns Unhealthy if core services missing, Degraded if overdue breaches exist, Healthy otherwise
- Pipeline behavior registered with TryAddTransient(typeof(IPipelineBehavior<,>), typeof(BreachDetectionPipelineBehavior<,>))
- 72-hour deadline is the primary compliance metric — health check MUST report approaching deadlines

REFERENCE FILES:
- src/Encina.Compliance.Retention/RetentionOptions.cs (options pattern)
- src/Encina.Compliance.Retention/ServiceCollectionExtensions.cs (DI registration)
- src/Encina.Compliance.Retention/RetentionEnforcementService.cs (BackgroundService pattern)
- src/Encina.Compliance.Retention/Health/RetentionHealthCheck.cs (health check pattern)
```

</details>

---

### Phase 6: Persistence Entity, Mapper & SQL Scripts

> **Goal**: Create the persistence layer shared infrastructure used by all 13 database providers.

<details>
<summary><strong>Tasks</strong></summary>

1. **Persistence entity** (`BreachRecordEntity.cs` in core package):
   - `public sealed class BreachRecordEntity` with string/primitive properties:
     - `Id (string)`, `Nature (string)`, `ApproximateSubjectsAffected (int)`, `CategoriesOfDataAffectedJson (string)` (JSON array), `DPOContactDetails (string)`, `LikelyConsequences (string)`, `MeasuresTaken (string)`
     - `DetectedAtUtc (string)` (ISO 8601 for SQLite), `NotificationDeadlineUtc (string)`, `NotifiedAuthorityAtUtc (string?)`, `NotifiedSubjectsAtUtc (string?)`
     - `SeverityValue (int)`, `StatusValue (int)`, `SubjectNotificationExemptionValue (int)`
     - `PhasedReportsJson (string?)` (JSON array of PhasedReport), `DelayReason (string?)`
     - `ResolvedAtUtc (string?)`, `ResolutionSummary (string?)`

2. **Phased report entity** (`PhasedReportEntity.cs`):
   - `public sealed class PhasedReportEntity`: `Id (string)`, `BreachId (string)`, `ReportNumber (int)`, `Content (string)`, `SubmittedAtUtc (string)`, `SubmittedByUserId (string?)`

3. **Breach audit entity** (`BreachAuditEntryEntity.cs`):
   - `public sealed class BreachAuditEntryEntity`: `Id (string)`, `BreachId (string)`, `Action (string)`, `Detail (string?)`, `PerformedByUserId (string?)`, `OccurredAtUtc (string)`

4. **Mapper** (`BreachRecordMapper.cs`):
   - `public static class BreachRecordMapper`
   - `ToEntity(BreachRecord) → BreachRecordEntity` (domain → persistence)
   - `ToDomain(BreachRecordEntity) → BreachRecord?` (persistence → domain, null if invalid)
   - JSON serialization for `CategoriesOfDataAffected` and `PhasedReports`

5. **Phased report mapper** (`PhasedReportMapper.cs`):
   - `ToEntity(PhasedReport) → PhasedReportEntity`
   - `ToDomain(PhasedReportEntity) → PhasedReport`

6. **Breach audit mapper** (`BreachAuditEntryMapper.cs`):
   - `ToEntity(BreachAuditEntry) → BreachAuditEntryEntity`
   - `ToDomain(BreachAuditEntryEntity) → BreachAuditEntry`

7. **SQL scripts** (`Scripts/` folder — referenced by satellite providers):
   - Table: `BreachRecords` — columns matching entity properties
   - Table: `PhasedReports` — separate table for phased reports (1:N with BreachRecords)
   - Table: `BreachAuditEntries` — columns matching audit entity
   - Provider-specific DDL:
     - **SQLite**: TEXT for dates (ISO 8601), INTEGER for enums
     - **SQL Server**: DATETIMEOFFSET for dates, INT for enums, NVARCHAR for strings
     - **PostgreSQL**: TIMESTAMPTZ for dates, INTEGER for enums, TEXT for strings
     - **MySQL**: DATETIME(6) for dates, INT for enums, VARCHAR for strings

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of Encina.Compliance.BreachNotification (Issue #408).

CONTEXT:
- Phases 1-5 are implemented (full core package with models, interfaces, default impls, DI)
- Persistence entities are plain classes (not records) for ORM compatibility
- Mappers convert between domain records and persistence entities
- SQL scripts are per-provider due to DDL differences

TASK:
Create persistence entities, mappers, and SQL DDL scripts for all 4 database engines.

KEY RULES:
- Entity classes use public get/set properties (mutable for ORMs)
- BreachRecord has nested data: CategoriesOfDataAffected stored as JSON string, PhasedReports as JSON or separate table
- PhasedReports stored in SEPARATE table (PhasedReports) with FK to BreachRecords — normalized for efficient queries
- Mapper.ToEntity uses Guid.ToString("D") for Id
- Mapper.ToDomain returns null if entity state is invalid (defensive)
- SQL scripts: CREATE TABLE IF NOT EXISTS (SQLite), IF NOT EXISTS pattern for others
- SQLite: TEXT for DateTime (ISO 8601 format), INTEGER for enums and booleans
- SQL Server: NVARCHAR(450) for string keys, DATETIMEOFFSET(7), INT for enums
- PostgreSQL: TEXT for strings, TIMESTAMPTZ, INTEGER for enums
- MySQL: VARCHAR(450) for string keys, DATETIME(6), INT for enums
- All tables have PRIMARY KEY on Id, INDEX on Status, INDEX on DetectedAtUtc
- PhasedReports has FK to BreachRecords.Id, INDEX on BreachId

REFERENCE FILES:
- src/Encina.Compliance.Retention/RetentionRecordEntity.cs (entity pattern)
- src/Encina.Compliance.Retention/RetentionRecordMapper.cs (mapper pattern)
- src/Encina.Compliance.DataResidency/DataLocationEntity.cs (entity pattern)
```

</details>

---

### Phase 7: Multi-Provider Persistence — 13 Database Providers

> **Goal**: Implement `IBreachRecordStore` and `IBreachAuditStore` across all 13 providers with satellite DI registration.

<details>
<summary><strong>Tasks</strong></summary>

#### 7a. ADO.NET Providers (×4)

For each ADO provider (`Encina.ADO.Sqlite`, `Encina.ADO.SqlServer`, `Encina.ADO.PostgreSQL`, `Encina.ADO.MySQL`):

1. `BreachNotification/BreachRecordStoreADO.cs` — implements `IBreachRecordStore`
   - Constructor: `IDbConnection connection`, `string tableName = "BreachRecords"`, `string phasedReportsTableName = "PhasedReports"`, `TimeProvider? timeProvider = null`
   - Uses `IDbCommand` / `IDataReader` pattern
   - `GetApproachingDeadlineAsync`: `SELECT Id, DetectedAtUtc, NotificationDeadlineUtc, StatusValue FROM BreachRecords WHERE NotificationDeadlineUtc > @NowUtc AND NotificationDeadlineUtc < @ThresholdUtc AND StatusValue NOT IN (@Resolved, @Closed)`
   - `GetOverdueBreachesAsync`: `SELECT * FROM BreachRecords WHERE NotificationDeadlineUtc < @NowUtc AND StatusValue NOT IN (@AuthorityNotified, @SubjectsNotified, @Resolved, @Closed)`
   - Provider-specific SQL: TOP vs LIMIT, parameter syntax, date handling

2. `BreachNotification/BreachAuditStoreADO.cs` — implements `IBreachAuditStore`
   - `GetAuditTrailAsync`: `SELECT ... WHERE BreachId = @BreachId ORDER BY OccurredAtUtc DESC`

3. **DI registration** in each provider's `ServiceCollectionExtensions.cs`:
   - Add `AddEncinaBreachNotification{Provider}(services, connectionString)` method
   - Registers `IBreachRecordStore` → `BreachRecordStoreADO` as singleton
   - Registers `IBreachAuditStore` → `BreachAuditStoreADO` as singleton

#### 7b. Dapper Providers (×4)

For each Dapper provider (`Encina.Dapper.Sqlite`, `Encina.Dapper.SqlServer`, `Encina.Dapper.PostgreSQL`, `Encina.Dapper.MySQL`):

1. `BreachNotification/BreachRecordStoreDapper.cs` — implements `IBreachRecordStore`
   - Uses `Dapper.ExecuteAsync`, `Dapper.QueryAsync`, `Dapper.ExecuteScalarAsync`
   - Anonymous parameter objects
   - Provider-specific SQL (same differences as ADO)

2. `BreachNotification/BreachAuditStoreDapper.cs` — implements `IBreachAuditStore`

3. **DI registration** — `AddEncinaBreachNotificationDapper{Provider}(services, connectionString)`

#### 7c. EF Core Provider

In `Encina.EntityFrameworkCore`:

1. `BreachNotification/BreachRecordStoreEF.cs` — implements `IBreachRecordStore`
   - Uses `DbContext.Set<BreachRecordEntity>()` with LINQ

2. `BreachNotification/BreachAuditStoreEF.cs` — implements `IBreachAuditStore`

3. `BreachNotification/BreachRecordEntityConfiguration.cs` — `IEntityTypeConfiguration<BreachRecordEntity>`

4. `BreachNotification/PhasedReportEntityConfiguration.cs` — `IEntityTypeConfiguration<PhasedReportEntity>`

5. `BreachNotification/BreachAuditEntryEntityConfiguration.cs` — `IEntityTypeConfiguration<BreachAuditEntryEntity>`

6. `BreachNotification/BreachModelBuilderExtensions.cs` — `modelBuilder.ApplyBreachNotificationConfiguration()`

7. **DI registration** in `ServiceCollectionExtensions.cs` — integrate with `AddEncinaEntityFrameworkCore` options

#### 7d. MongoDB Provider

In `Encina.MongoDB`:

1. `BreachNotification/BreachRecordStoreMongoDB.cs` — implements `IBreachRecordStore`
   - Uses `IMongoCollection<BreachRecordDocument>`

2. `BreachNotification/BreachAuditStoreMongoDB.cs` — implements `IBreachAuditStore`

3. `BreachNotification/BreachRecordDocument.cs` — MongoDB document class with `FromDomain` / `ToDomain`

4. `BreachNotification/PhasedReportDocument.cs` — embedded document within BreachRecordDocument

5. `BreachNotification/BreachAuditEntryDocument.cs` — MongoDB audit document

6. **DI registration** — integrate with `AddEncinaMongoDB` options

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of Encina.Compliance.BreachNotification (Issue #408).

CONTEXT:
- Phases 1-6 are implemented (core package + entities + mappers + SQL scripts)
- ALL 13 database providers must be implemented: ADO.NET (Sqlite, SqlServer, PostgreSQL, MySQL), Dapper (same 4), EF Core (4 under one package), MongoDB
- Each provider creates a BreachNotification/ subfolder in its source project

TASK:
Implement IBreachRecordStore and IBreachAuditStore for all 13 providers, plus DI registration.

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
  - PhasedReports are EMBEDDED documents within BreachRecordDocument (not separate collection)
- SQLite DateTime: ALWAYS use "O" format for serialization, DateTimeStyles.RoundtripKind for parsing, NEVER use datetime('now')
- All stores validate table names via SqlIdentifierValidator.ValidateTableName()
- GetApproachingDeadlineAsync must be efficient: filtered query with status exclusion
- DI: satellite methods called BEFORE AddEncinaBreachNotification (TryAdd pattern)
- TimeProvider injection for testable time-dependent queries

REFERENCE FILES:
- src/Encina.ADO.Sqlite/Retention/RetentionRecordStoreADO.cs (ADO pattern)
- src/Encina.Dapper.Sqlite/Retention/RetentionRecordStoreDapper.cs (Dapper pattern)
- src/Encina.EntityFrameworkCore/Retention/ (EF Core pattern)
- src/Encina.MongoDB/Retention/ (MongoDB pattern)
- Provider ServiceCollectionExtensions.cs in each satellite package
```

</details>

---

### Phase 8: Observability — Diagnostics, Metrics & Logging

> **Goal**: Add OpenTelemetry traces, counters, and structured logging for breach notification operations.

<details>
<summary><strong>Tasks</strong></summary>

1. **`BreachNotificationDiagnostics.cs`** (`Diagnostics/` folder):
   - `internal static class BreachNotificationDiagnostics`
   - `ActivitySource`: `"Encina.Compliance.BreachNotification"`, version `"1.0"`
   - `Meter`: `"Encina.Compliance.BreachNotification"`, version `"1.0"`
   - **Counters** (Counter<long>):
     - `breach.detected.total` — tagged by `severity`, `detection_rule`
     - `breach.notification.authority.total` — tagged by `outcome` (sent, failed, pending)
     - `breach.notification.subjects.total` — tagged by `outcome`, `subject_count`
     - `breach.pipeline.executions.total` — tagged by `outcome` (detected, passed, skipped)
     - `breach.phased_reports.total` — tagged by `breach_id`
     - `breach.resolved.total` — tagged by `severity`
   - **Histograms** (Histogram<double>):
     - `breach.time_to_notification.hours` — time from detection to authority notification
     - `breach.detection.duration.ms` — rule evaluation duration
     - `breach.pipeline.duration.ms` — pipeline behavior overhead
   - **Tag constants**:
     - `TagSeverity = "breach.severity"`, `TagOutcome = "breach.outcome"`, `TagDetectionRule = "breach.detection_rule"`, `TagBreachId = "breach.id"`, `TagStatus = "breach.status"`, `TagSubjectCount = "breach.subject_count"`
   - **Activity helpers**:
     - `StartBreachDetection(string ruleName)` → `Activity?`
     - `StartNotification(string type, Guid breachId)` → `Activity?`
     - `StartPipelineExecution(string requestTypeName)` → `Activity?`
     - `StartDeadlineCheck()` → `Activity?`
   - **Outcome recorders**:
     - `RecordCompleted(Activity?)`, `RecordFailed(Activity?, string reason)`, `RecordSkipped(Activity?)`

2. **`BreachNotificationLogMessages.cs`** (`Diagnostics/` folder):
   - `internal static partial class BreachNotificationLogMessages` using `[LoggerMessage]` source generator
   - **Event ID range: 8700-8799**:
     - 8700-8709: Pipeline behavior
       - 8700: Pipeline disabled
       - 8701: Pipeline no attributes
       - 8702: Pipeline started
       - 8703: Pipeline breach detected
       - 8704: Pipeline breach blocked
       - 8705: Pipeline breach warning
       - 8706: Pipeline completed (no breach)
     - 8710-8719: Detection engine
       - 8710: Detection started
       - 8711: Detection rule matched
       - 8712: Detection rule no match
       - 8713: Detection completed
       - 8714: Detection rule evaluation failed
     - 8720-8729: Notification
       - 8720: Authority notification started
       - 8721: Authority notification sent
       - 8722: Authority notification failed
       - 8723: Subject notification started
       - 8724: Subject notification sent
       - 8725: Subject notification failed
       - 8726: Subject notification exempted
     - 8730-8739: Breach lifecycle
       - 8730: Breach recorded
       - 8731: Breach status updated
       - 8732: Phased report added
       - 8733: Breach resolved
       - 8734: Breach closed
     - 8740-8749: Deadline monitoring
       - 8740: Deadline check started
       - 8741: Deadline warning (approaching)
       - 8742: Deadline overdue
       - 8743: Deadline check completed
     - 8750-8759: Health check
       - 8750: Health check completed
       - 8751: Health check degraded
     - 8760-8769: Audit trail
       - 8760: Audit entry recorded
       - 8761: Audit trail queried
     - 8770-8779: Store operations
       - 8770: Store error
       - 8771: Store operation completed

3. **Integrate observability** into existing code (Phase 3-5 implementations):
   - Add `Activity` and `Counter` calls to `DefaultBreachHandler`, `DefaultBreachDetector`, pipeline behavior, deadline monitor

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 of Encina.Compliance.BreachNotification (Issue #408).

CONTEXT:
- Phases 1-7 are implemented (full core + 13 providers)
- Observability follows OpenTelemetry patterns: ActivitySource for traces, Meter for metrics, ILogger for structured logs
- Event IDs in 8700-8799 range (new range, avoids collision with GDPR 8100-8199, Consent 8200-8299, DSR 8300-8399, Anonymization 8400-8499, Retention 8500-8599, DataResidency 8600-8699)

TASK:
Create BreachNotificationDiagnostics, BreachNotificationLogMessages, and integrate observability into existing code.

KEY RULES:
- ActivitySource name: "Encina.Compliance.BreachNotification" (matches package name)
- Meter: new Meter("Encina.Compliance.BreachNotification", "1.0") — separate from other modules
- All counters use tag-based dimensions (severity, outcome, detection_rule) for flexible dashboards
- Key metric: breach.time_to_notification.hours — measures compliance with 72-hour deadline
- BreachNotificationLogMessages uses [LoggerMessage] source generator (partial class, partial methods)
- Log messages follow structured logging: "Breach {Action}. BreachId={BreachId}, Severity={Severity}"
- Activity helpers check Source.HasListeners() before creating activities (avoid allocations)
- SetTag uses string constants from tag fields
- CompleteDSRRequest sets ActivityStatusCode.Ok or ActivityStatusCode.Error

REFERENCE FILES:
- src/Encina.Compliance.Retention/Diagnostics/RetentionDiagnostics.cs (ActivitySource + counters + histograms)
- src/Encina.Compliance.Retention/Diagnostics/RetentionLogMessages.cs ([LoggerMessage] source generator)
- src/Encina.Compliance.DataResidency/Diagnostics/DataResidencyDiagnostics.cs (separate meter pattern)
```

</details>

---

### Phase 9: Testing — 7 Test Types

> **Goal**: Comprehensive test coverage across all test categories.

<details>
<summary><strong>Tasks</strong></summary>

#### 9a. Unit Tests (`tests/Encina.UnitTests/Compliance/BreachNotification/`)

- `BreachRecordTests.cs` — domain record validation, deadline calculation (72h), status transitions
- `PhasedReportTests.cs` — report numbering, content validation
- `SecurityEventTests.cs` — event type mapping, metadata handling
- `BreachRecordMapperTests.cs` — domain ↔ entity round-trip, JSON serialization of categories/reports
- `BreachAuditEntryMapperTests.cs` — audit domain ↔ entity round-trip
- `DefaultBreachHandlerTests.cs` — all lifecycle methods with mocked dependencies
- `DefaultBreachDetectorTests.cs` — rule iteration, aggregation, empty rules
- `DefaultBreachNotifierTests.cs` — no-op notification, result creation
- `UnauthorizedAccessRuleTests.cs` — threshold-based detection
- `MassDataExfiltrationRuleTests.cs` — volume-based detection
- `PrivilegeEscalationRuleTests.cs` — escalation detection
- `AnomalousQueryPatternRuleTests.cs` — query pattern detection
- `BreachDetectionPipelineBehaviorTests.cs` — Block/Warn/Disabled modes, attribute caching
- `SecurityEventFactoryTests.cs` — event creation from request metadata
- `InMemoryBreachRecordStoreTests.cs` — all CRUD operations, deadline queries, phased reports
- `InMemoryBreachAuditStoreTests.cs` — record + query audit trail
- `BreachDeadlineMonitorServiceTests.cs` — periodic check, alert thresholds, overdue marking
- `BreachNotificationOptionsValidatorTests.cs` — validation rules
- `ServiceCollectionExtensionsTests.cs` — DI registration verification
- `BreachNotificationHealthCheckTests.cs` — healthy/degraded/unhealthy scenarios

**Target**: ~90-110 unit tests

#### 9b. Guard Tests (`tests/Encina.GuardTests/Compliance/BreachNotification/`)

- All public constructors and methods: null checks for non-nullable parameters
- Cover: all interface implementations, options, mappers, attributes, pipeline behavior, detection rules
- Use `GuardClauses.xUnit` library

**Target**: ~40-60 guard tests

#### 9c. Contract Tests (`tests/Encina.ContractTests/Compliance/BreachNotification/`)

- `IBreachRecordStoreContractTests.cs` — verify all 13 store implementations follow the same contract
- `IBreachAuditStoreContractTests.cs` — verify audit store contract
- `IBreachDetectionRuleContractTests.cs` — detection rule interface contract
- `IBreachNotifierContractTests.cs` — notifier interface contract

**Target**: ~15-25 contract tests

#### 9d. Property Tests (`tests/Encina.PropertyTests/Compliance/BreachNotification/`)

- `BreachRecordPropertyTests.cs` — deadline always 72h after detection, status enum round-trip
- `BreachRecordMapperPropertyTests.cs` — domain → entity → domain round-trip preserves all fields
- `PhasedReportPropertyTests.cs` — report number always positive, content non-empty
- `DeadlineStatusPropertyTests.cs` — IsOverdue iff RemainingHours < 0

**Target**: ~15-20 property tests

#### 9e. Integration Tests (`tests/Encina.IntegrationTests/Compliance/BreachNotification/`)

For ALL 13 providers:

- `BreachRecordStore{Provider}IntegrationTests.cs` — CRUD, deadline queries, phased reports, status updates against real DB
- `BreachAuditStore{Provider}IntegrationTests.cs` — audit CRUD against real DB
- Each uses `[Collection("{Provider}")]` fixtures (existing collections)
- `InitializeAsync` creates schema + clears data
- Tests: RecordBreach → GetBreach, UpdateBreach, GetByStatus, GetOverdue, GetApproachingDeadline, AddPhasedReport

**Target**: ~100-130 integration tests (10 tests × 13 providers)

#### 9f. Load Tests (`tests/Encina.LoadTests/Compliance/BreachNotification/`)

- `BreachRecordStoreLoadTests.md` — justification document (store is thin DB wrapper, load is on DB)
- `BreachDetectionPipelineBehaviorLoadTests.cs` — concurrent detection under load (100 concurrent requests)
- `BreachDeadlineMonitorLoadTests.cs` — deadline check under high breach volume

**Target**: 2 load test classes + 1 justification

#### 9g. Benchmark Tests (`tests/Encina.BenchmarkTests/Compliance/BreachNotification/`)

- `DetectionRuleBenchmarks.cs` — rule evaluation overhead per SecurityEvent
- `PipelineBehaviorBenchmarks.cs` — pipeline behavior overhead (attribute caching, detection)
- `DeadlineCalculationBenchmarks.md` — justification (trivial arithmetic, not hot path)

**Target**: 2 benchmark classes + 1 justification

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
You are implementing Phase 9 of Encina.Compliance.BreachNotification (Issue #408).

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
- 72-hour deadline calculation is critical — test extensively
- Test all 4 built-in detection rules with various SecurityEvent inputs
- Fast execution (<1ms per test)

Guard Tests:
- Use GuardClauses.xUnit library
- Test all public constructors and methods with non-nullable parameters

Contract Tests:
- Verify all 13 IBreachRecordStore implementations follow identical behavior
- Use abstract base class with provider-specific derived classes

Property Tests:
- FsCheck generators for domain records
- Verify invariants: deadline = detection + 72h, mapper round-trip, IsOverdue logic

Integration Tests:
- [Collection("ADO-Sqlite")] etc. — reuse existing fixtures
- ClearAllDataAsync in InitializeAsync
- Create schema (BreachRecords, PhasedReports, BreachAuditEntries tables)
- Test phased reporting: RecordBreach → AddPhasedReport → GetBreach (includes reports)
- Test deadline queries with controlled TimeProvider
- SQLite: NEVER dispose shared connection from fixture

Load Tests:
- BreachDetectionPipelineBehavior under concurrent access
- DeadlineMonitor with 1000+ breach records

Benchmark Tests:
- BenchmarkSwitcher (NOT BenchmarkRunner)
- Results to artifacts/performance/

REFERENCE FILES:
- tests/Encina.UnitTests/Compliance/Retention/ (unit test patterns)
- tests/Encina.GuardTests/Compliance/Retention/ (guard test patterns)
- tests/Encina.IntegrationTests/Compliance/Retention/ (integration test patterns with Collection fixtures)
- tests/Encina.PropertyTests/Compliance/Retention/ (FsCheck patterns)
```

</details>

---

### Phase 10: Documentation & Finalization

> **Goal**: Update all project documentation, verify build, and finalize.

<details>
<summary><strong>Tasks</strong></summary>

1. **INVENTORY.md** — Update issue #408 entry with:
   - Package details
   - Provider count
   - Test count and coverage
   - Key interfaces

2. **CHANGELOG.md** — Add entry under Unreleased:
   - `### Added`
   - `- Encina.Compliance.BreachNotification — GDPR 72-hour data breach notification (Articles 33-34) with IBreachDetector, IBreachNotifier, IBreachRecordStore, BreachDetectionPipelineBehavior, [BreachMonitored] attribute, deadline monitoring, phased reporting, and 4 built-in detection rules across all 13 database providers (Fixes #408)`

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
You are implementing Phase 10 of Encina.Compliance.BreachNotification (Issue #408).

CONTEXT:
- Phases 1-9 are fully implemented and tested
- Documentation and finalization remaining

TASK:
Update INVENTORY.md, CHANGELOG.md, verify build, and finalize.

KEY RULES:
- INVENTORY.md: mark #408 as IMPLEMENTADO with comprehensive description
- CHANGELOG.md: add under ### Added in Unreleased section
- Build must produce 0 errors and 0 warnings
- All tests must pass
- PublicAPI.Unshipped.txt must be complete and accurate
- Commit message: "feat: add Encina.Compliance.BreachNotification - GDPR 72-hour breach notification with detection engine and 13 database providers (Fixes #408)"
```

</details>

---

## Research

### GDPR Article References

| Article | Requirement | Key Details |
|---------|-------------|-------------|
| Art. 33(1) | Notification to authority | "not later than 72 hours after having become aware of it" |
| Art. 33(2) | Processor notification | Processor shall notify controller without undue delay |
| Art. 33(3) | Notification content | Nature, subjects affected, data categories, DPO contact, consequences, measures |
| Art. 33(4) | Phased reporting | "information may be provided in phases without undue further delay" |
| Art. 33(5) | Documentation | Controller shall document breach facts, effects, remedial actions |
| Art. 34(1) | Communication to subject | "likely to result in a high risk" → communicate without undue delay |
| Art. 34(2) | Communication content | Clear and plain language, nature of breach, DPO contact, consequences, measures |
| Art. 34(3) | Exemptions | Encryption, mitigating measures, disproportionate effort (use public communication) |
| Art. 34(4) | Authority may require | Supervisory authority may require controller to notify subjects |
| Recital 85 | Without undue delay | "should be notified to the supervisory authority without undue delay" |
| Recital 86 | 72-hour deadline | Explanation of the 72-hour requirement and phased reporting |
| Recital 87 | Assessment criteria | Factors for determining notification obligation |
| Recital 88 | Format and procedures | Notification format, procedures, and electronic means |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in BreachNotification |
|-----------|----------|---------------------------|
| `IPipelineBehavior<,>` | `Encina` core | Pipeline behavior registration for `BreachDetectionPipelineBehavior` |
| `INotification` / `INotificationPublisher` | `Encina` core | Domain notifications (BreachDetected, AuthorityNotified, etc.) |
| `EncinaErrors.Create()` | `Encina` core | Error factory pattern |
| `TimeProvider` | .NET 10 BCL | Testable time-dependent logic (72h deadline, alerts) |
| `IAuditStore` | `Encina.Security.Audit` | Optional integration — consume audit events as security events |
| `IOutboxStore` | `Encina.Messaging` | Optional reliable notification delivery via outbox |
| Satellite provider structure | All 13 providers | Subfolder + DI registration pattern |
| `RetentionValidationPipelineBehavior` | `Encina.Compliance.Retention` | Pipeline behavior attribute caching pattern |
| `RetentionEnforcementService` | `Encina.Compliance.Retention` | BackgroundService pattern for deadline monitoring |
| `RetentionHealthCheck` | `Encina.Compliance.Retention` | Health check with degraded state pattern |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.GDPR` | 8100-8199 | Core, LawfulBasis, ProcessingActivity |
| `Encina.Compliance.Consent` | 8200-8299 | Consent lifecycle, audit, events |
| `Encina.Compliance.DataSubjectRights` | 8300-8399 | DSR lifecycle, restriction, erasure, export |
| `Encina.Compliance.Anonymization` | 8400-8499 | Anonymization and pseudonymization |
| `Encina.Compliance.Retention` | 8500-8599 | Retention enforcement, legal holds |
| `Encina.Compliance.DataResidency` | 8600-8699 | Data residency, transfer validation |
| **`Encina.Compliance.BreachNotification`** | **8700-8799** | **New — Breach detection, notification, deadline monitoring** |

### File Count Estimate

| Category | Files | Notes |
|----------|-------|-------|
| Core package (Phases 1-5, 8) | ~35-40 | Models, interfaces, impls, detection rules, diagnostics, DI, deadline monitor |
| Persistence (Phase 6) | ~7 | Entities, mappers (3 tables) |
| ADO.NET ×4 (Phase 7a) | ~12 | 3 files × 4 providers |
| Dapper ×4 (Phase 7b) | ~12 | 3 files × 4 providers |
| EF Core (Phase 7c) | ~9 | Stores, entities, configs, extensions |
| MongoDB (Phase 7d) | ~7 | Stores, documents |
| Tests (Phase 9) | ~40-50 | Across 7 test types |
| Documentation (Phase 10) | ~3 | INVENTORY, CHANGELOG, PublicAPI |
| **Total** | **~120-135** | |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing Encina.Compliance.BreachNotification for Issue #408 — GDPR 72-Hour Data Breach Notification (Articles 33-34).

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library for CQRS + Event Sourcing
- Pre-1.0: no backward compatibility needed, best solution always
- Railway Oriented Programming: Either<EncinaError, T> everywhere
- 13 database providers: ADO.NET (Sqlite, SqlServer, PostgreSQL, MySQL), Dapper (same 4), EF Core (same 4), MongoDB
- Satellite provider pattern: feature subfolder in each provider package
- TryAdd DI pattern: satellite providers register before core package

IMPLEMENTATION OVERVIEW:
New package: src/Encina.Compliance.BreachNotification/
No hard dependency on Encina.Compliance.GDPR or Encina.Security.Audit — optional integration via events

Phase 1: Core models, enums (BreachRecord, BreachSeverity, BreachStatus, SecurityEvent, PotentialBreach, PhasedReport, etc.)
Phase 2: Interfaces (IBreachDetector, IBreachDetectionRule, IBreachNotifier, IBreachRecordStore, IBreachAuditStore, IBreachHandler) + [BreachMonitored] attribute + BreachNotificationErrors
Phase 3: Default implementations (InMemory stores, DefaultBreachDetector, DefaultBreachNotifier, DefaultBreachHandler, 4 built-in detection rules)
Phase 4: BreachDetectionPipelineBehavior (evaluates requests for breach indicators) + SecurityEventFactory
Phase 5: Options, DI registration, BreachDeadlineMonitorService (BackgroundService), health check
Phase 6: Persistence entities, mappers, SQL scripts (3 tables: BreachRecords, PhasedReports, BreachAuditEntries)
Phase 7: 13 provider implementations (ADO ×4, Dapper ×4, EF Core, MongoDB) + satellite DI
Phase 8: Observability (ActivitySource, Meter, [LoggerMessage] event IDs 8700-8799)
Phase 9: Testing (7 types: Unit ~100, Guard ~50, Contract ~20, Property ~20, Integration ~130, Load, Benchmark)
Phase 10: Documentation (INVENTORY.md, CHANGELOG.md, PublicAPI.Unshipped.txt)

KEY PATTERNS:
- All stores: ValueTask<Either<EncinaError, T>>
- Store naming: BreachRecordStoreADO, BreachRecordStoreDapper, BreachRecordStoreEF, BreachRecordStoreMongoDB
- SQLite: TEXT dates (ISO 8601 "O"), never datetime('now'), NEVER dispose shared connection in tests
- Satellite DI: AddEncinaBreachNotification{Provider}(services, connectionString) → called before AddEncinaBreachNotification
- Pipeline behavior: static per-generic-type attribute caching, 3 enforcement modes (Block/Warn/Disabled)
- Detection engine: IBreachDetectionRule interface, composite evaluator, 4 built-in rules
- 72-hour deadline: NotificationDeadlineUtc = DetectedAtUtc.AddHours(72) — core compliance metric
- Phased reporting: PhasedReports table (1:N with BreachRecords) supports Art. 33(4)
- Deadline monitoring: BackgroundService with configurable alert thresholds [48, 24, 12, 6, 1] hours
- Health check: Unhealthy/Degraded/Healthy, scoped resolution, const DefaultName
- Integration tests: [Collection("Provider-DB")] shared fixtures, ClearAllDataAsync
- All public APIs: XML documentation with GDPR Article 33/34 references
- Event ID range: 8700-8799

REFERENCE FILES:
- Retention package: src/Encina.Compliance.Retention/ (closest architectural reference — also has enforcement service, health check, entities, mappers)
- DataResidency package: src/Encina.Compliance.DataResidency/ (pipeline behavior, policy management)
- Provider patterns: src/Encina.ADO.Sqlite/Retention/, src/Encina.Dapper.Sqlite/Retention/, src/Encina.EntityFrameworkCore/Retention/, src/Encina.MongoDB/Retention/
```

</details>

---

## Next Steps

1. **Review and approve this plan**
2. Publish as comment on Issue #408
3. Begin Phase 1 implementation in a new session
4. Each phase should be a self-contained commit
5. Final commit references `Fixes #408`
