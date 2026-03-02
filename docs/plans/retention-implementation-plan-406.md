# Implementation Plan: `Encina.Compliance.Retention` — Data Retention and Automatic Deletion (GDPR Art. 5(1)(e))

> **Issue**: [#406](https://github.com/dlrivada/Encina/issues/406)
> **Type**: Feature
> **Complexity**: High (10 phases, 13 database providers, ~130 files)
> **Estimated Scope**: ~5,000-7,000 lines of production code + ~3,500-5,000 lines of tests

---

## Summary

Implement data retention policies with automatic deletion for GDPR Article 5(1)(e) — Storage Limitation. This package provides category-based retention policy management, automatic expiration tracking, scheduled deletion enforcement, legal hold support (litigation holds that suspend deletion), expiration alerts, and a comprehensive audit trail for all deletion actions.

The implementation integrates with the existing `Encina.Compliance.DataSubjectRights` package (reusing `IDataErasureExecutor` for actual data deletion) and follows the same satellite-provider architecture established by `Encina.Compliance.Anonymization` and `Encina.Compliance.Consent`, delivering store implementations across all 13 database providers with dedicated observability, health checks, and auto-registration.

**Affected packages:**
- `Encina.Compliance.Retention` (new core package)
- `Encina.ADO.Sqlite`, `Encina.ADO.SqlServer`, `Encina.ADO.PostgreSQL`, `Encina.ADO.MySQL` (satellite stores)
- `Encina.Dapper.Sqlite`, `Encina.Dapper.SqlServer`, `Encina.Dapper.PostgreSQL`, `Encina.Dapper.MySQL` (satellite stores)
- `Encina.EntityFrameworkCore` (satellite stores)
- `Encina.MongoDB` (satellite store)

**Provider category**: Database (13 providers) — same as Consent, DSR, Anonymization.

---

## Design Choices

<details>
<summary><strong>1. Package Placement — New <code>Encina.Compliance.Retention</code> package</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New `Encina.Compliance.Retention` package** | Clean separation, own pipeline behavior, own scheduler, own observability, independent lifecycle | New NuGet package to maintain |
| **B) Extend `Encina.Compliance.GDPR`** | Single package | Bloats GDPR core, violates SRP, retention is a distinct domain |
| **C) Extend `Encina.Compliance.DataSubjectRights`** | Reuse erasure executor directly | DSR is about data subject requests, retention is about automated policies — different trigger mechanisms |

### Chosen Option: **A — New `Encina.Compliance.Retention` package**

### Rationale

- Retention is a distinct compliance domain: GDPR Art. 5(1)(e) (Storage Limitation) is separate from Arts. 15-22 (DSR)
- Follows the established pattern: Consent, DSR, Anonymization each have their own packages
- References `Encina.Compliance.DataSubjectRights` to reuse `IDataErasureExecutor` for actual data deletion (composition over duplication)
- References `Encina.Compliance.GDPR` for shared types (`[ProcessesPersonalData]`)
- Own pipeline behavior (`RetentionValidationPipelineBehavior`) validates data creation against retention policies
- Own scheduler (`RetentionEnforcementService`) handles automatic deletion on a configurable interval

</details>

<details>
<summary><strong>2. Domain Model Design — Policy + Record separation with Legal Hold as first-class entity</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Separate RetentionPolicy + RetentionRecord + LegalHold entities** | Clean domain model, each entity has clear lifecycle, queryable independently | Three store interfaces to implement |
| **B) Single RetentionRecord with embedded policy** | Simpler model, fewer stores | Policy duplicated per record, hard to update policies globally |
| **C) Policy-only (compute expiration at query time)** | No tracking records | Can't audit, can't handle legal holds, no expiration alerts |

### Chosen Option: **A — Separate entities**

### Rationale

- `RetentionPolicy` defines the rules: data category, retention period, auto-delete flag, legal basis
- `RetentionRecord` tracks individual data items against policies: entity ID, category, creation date, expiration date, status
- `LegalHold` is a first-class entity: hold ID, entity ID, reason, applied date, released date — suspends deletion regardless of policy
- Three store interfaces: `IRetentionPolicyStore`, `IRetentionRecordStore`, `ILegalHoldStore`
- Each entity has independent CRUD + specialized queries (expired records, active holds, etc.)
- Maps cleanly to GDPR requirements: policies for Art. 5(1)(e), records for tracking, holds for litigation

</details>

<details>
<summary><strong>3. Deletion Mechanism — Delegate to <code>IDataErasureExecutor</code> from DSR</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Delegate to `IDataErasureExecutor` from DSR** | Reuses battle-tested erasure logic, consistent audit trail, respects LegalRetention flags | Creates dependency on DSR package |
| **B) Own deletion implementation** | No external dependency | Duplicates erasure logic, inconsistent behavior |
| **C) Direct SQL DELETE** | Fast, simple | No audit trail, no field-level control, ignores LegalRetention |

### Chosen Option: **A — Delegate to `IDataErasureExecutor`**

### Rationale

- DSR already has a comprehensive erasure pipeline: locate data → filter by scope → partition (erasable vs. retained) → execute strategy → report results
- Retention creates an `ErasureScope` with `ErasureReason.NoLongerNecessary` (Art. 17(1)(a)) and delegates
- Consistent audit trail across manual DSR erasure and automated retention erasure
- Respects `HasLegalRetention` flags on `PersonalDataLocation` entries
- Optional dependency: if DSR is not registered, retention logs a warning and skips automated deletion (degraded mode)

</details>

<details>
<summary><strong>4. Scheduler Architecture — <code>IHostedService</code> with configurable interval</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `IHostedService` with `PeriodicTimer`** | Built-in .NET, no external dependency, configurable interval | Single-instance (not distributed) |
| **B) Hangfire/Quartz adapter** | Distributed, persistent jobs | External dependency, heavy for core package |
| **C) Manual enforcement only** | Simple, no background processing | Defeats purpose of automatic deletion |

### Chosen Option: **A — `IHostedService` with `PeriodicTimer`**

### Rationale

- `RetentionEnforcementService` implements `IHostedService` using `PeriodicTimer` for periodic enforcement
- Configurable interval via `RetentionOptions.EnforcementIntervalMinutes` (default: 60 minutes)
- Queries `IRetentionRecordStore.GetExpiredRecordsAsync()` → filters out legal holds → delegates to `IDataErasureExecutor`
- Single-instance is acceptable: retention enforcement is idempotent (re-running produces same result)
- For distributed scenarios: users can disable the built-in scheduler and use Hangfire/Quartz (future adapter in `Encina.Hangfire`)
- Optional: `RetentionOptions.EnableAutomaticEnforcement` flag (default: `true`) — set to `false` for manual-only enforcement

</details>

<details>
<summary><strong>5. Pipeline Behavior — <code>RetentionValidationPipelineBehavior</code> for data creation tracking</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Pipeline behavior that auto-creates RetentionRecords on response** | Automatic tracking, no manual registration needed, attribute-driven | Overhead per request |
| **B) Manual `IRetentionRecordStore.TrackAsync()` calls** | Explicit, no overhead | Easy to forget, boilerplate |
| **C) Database triggers** | Automatic, no application code | Provider-specific, hard to test, no GDPR metadata |

### Chosen Option: **A — Pipeline behavior**

### Rationale

- `RetentionValidationPipelineBehavior<TRequest, TResponse>` scans response types for `[RetentionPeriod]` attribute
- When data is created (response contains entities decorated with `[RetentionPeriod]`), automatically creates `RetentionRecord` entries
- Static per-generic-type attribute caching (same pattern as Anonymization/DSR behaviors)
- Three enforcement modes: `Block` (fail if no retention policy defined for category), `Warn` (log warning), `Disabled` (skip)
- Attribute `[RetentionPeriod(days: 365)]` on response types/properties specifies retention period
- Integrates with `IRetentionPolicyStore` to resolve policies by data category

</details>

<details>
<summary><strong>6. Legal Hold Model — First-class entity with cascading protection</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Separate LegalHold entity with per-entity application** | Granular control, can hold individual entities, clear audit trail | More complex model |
| **B) Flag on RetentionRecord** | Simple, single table | Can't track hold metadata (reason, who applied, when released) |
| **C) Category-level holds** | Broad protection | Too coarse, can't hold specific entities |

### Chosen Option: **A — Separate entity with per-entity application**

### Rationale

- `LegalHold` record: `Id`, `EntityId`, `Reason`, `AppliedByUserId`, `AppliedAtUtc`, `ReleasedAtUtc?`, `ReleasedByUserId?`, `IsActive` (computed)
- `ILegalHoldStore` provides CRUD + `IsUnderHoldAsync(entityId)` for fast lookup
- Enforcement flow: before deleting a record, `IRetentionEnforcer` checks `ILegalHoldStore.IsUnderHoldAsync(entityId)` → if active hold, skip deletion and set status to `UnderLegalHold`
- When hold is released, record status reverts to `Expired` and becomes eligible for next enforcement cycle
- Audit trail: hold application and release are logged as `RetentionAuditEntry` records
- Supports GDPR Art. 17(3)(e): legal claims exemption

</details>

<details>
<summary><strong>7. Expiration Alert Mechanism — Domain notification events</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Domain notification events via Encina pipeline** | Uses existing `INotificationPublisher`, decoupled, handlers per bounded context | Requires notification handlers |
| **B) Callback/delegate pattern** | Simple, no framework dependency | Not testable, not discoverable |
| **C) Polling API only** | No events needed | Passive, easy to miss approaching expirations |

### Chosen Option: **A — Domain notification events**

### Rationale

- `DataExpiringNotification` published when data is within `AlertBeforeExpirationDays` of expiration
- `DataDeletedNotification` published after successful automatic deletion
- `LegalHoldAppliedNotification` / `LegalHoldReleasedNotification` for hold lifecycle
- Users implement `INotificationHandler<DataExpiringNotification>` to handle alerts (email, dashboard, etc.)
- Integrates with Outbox pattern for reliable delivery if configured
- `RetentionOptions.PublishNotifications` flag controls emission (default: `true`)

</details>

---

## Implementation Phases

### Phase 1: Core Models, Enums & Domain Records

> **Goal**: Establish the foundational types that all other phases depend on.

<details>
<summary><strong>Tasks</strong></summary>

#### New project: `src/Encina.Compliance.Retention/`

1. **Create project file** `Encina.Compliance.Retention.csproj`
   - Target: `net10.0`
   - Dependencies: `Encina.Compliance.GDPR`, `Encina.Compliance.DataSubjectRights`, `LanguageExt.Core`, `Microsoft.Extensions.Logging.Abstractions`, `Microsoft.Extensions.Options`, `Microsoft.Extensions.Hosting.Abstractions`, `Microsoft.Extensions.Diagnostics.HealthChecks`
   - Enable nullable, implicit usings, XML doc
   - `InternalsVisibleTo` for all 13 providers + test assemblies
   - `PublicAPI.Shipped.txt` + `PublicAPI.Unshipped.txt`

2. **Enums** (`Model/` folder):
   - `RetentionStatus` — `Active`, `Expired`, `Deleted`, `UnderLegalHold`
   - `RetentionPolicyType` — `TimeBased`, `EventBased`, `ConsentBased`
   - `DeletionOutcome` — `Deleted`, `Retained`, `Failed`, `HeldByLegalHold`, `Skipped`

3. **Domain records** (`Model/` folder):
   - `RetentionPolicy` — sealed record: `Id (string)`, `DataCategory (string)`, `RetentionPeriod (TimeSpan)`, `AutoDelete (bool)`, `Reason (string?)`, `LegalBasis (string?)`, `PolicyType (RetentionPolicyType)`, `CreatedAtUtc (DateTimeOffset)`, `LastModifiedAtUtc (DateTimeOffset?)`
   - `RetentionRecord` — sealed record: `Id (string)`, `EntityId (string)`, `DataCategory (string)`, `PolicyId (string?)`, `CreatedAtUtc (DateTimeOffset)`, `ExpiresAtUtc (DateTimeOffset)`, `Status (RetentionStatus)`, `DeletedAtUtc (DateTimeOffset?)`, `LegalHoldId (string?)`
   - `LegalHold` — sealed record: `Id (string)`, `EntityId (string)`, `Reason (string)`, `AppliedByUserId (string?)`, `AppliedAtUtc (DateTimeOffset)`, `ReleasedAtUtc (DateTimeOffset?)`, `ReleasedByUserId (string?)`, `IsActive (bool)` (computed: `ReleasedAtUtc is null`)
   - `DeletionResult` — sealed record: `TotalRecordsEvaluated (int)`, `RecordsDeleted (int)`, `RecordsRetained (int)`, `RecordsFailed (int)`, `RecordsUnderHold (int)`, `Details (IReadOnlyList<DeletionDetail>)`, `ExecutedAtUtc (DateTimeOffset)`
   - `DeletionDetail` — sealed record: `EntityId (string)`, `DataCategory (string)`, `Outcome (DeletionOutcome)`, `Reason (string?)`
   - `ExpiringData` — sealed record: `EntityId (string)`, `DataCategory (string)`, `ExpiresAtUtc (DateTimeOffset)`, `PolicyId (string?)`, `DaysUntilExpiration (int)`
   - `RetentionAuditEntry` — sealed record: `Id (string)`, `Action (string)`, `EntityId (string?)`, `DataCategory (string?)`, `Detail (string?)`, `PerformedByUserId (string?)`, `OccurredAtUtc (DateTimeOffset)`

4. **Notification records** (`Notifications/` folder):
   - `DataExpiringNotification` — sealed record implementing `INotification`: `EntityId`, `DataCategory`, `ExpiresAtUtc`, `DaysUntilExpiration`, `OccurredAtUtc`
   - `DataDeletedNotification` — sealed record implementing `INotification`: `EntityId`, `DataCategory`, `DeletedAtUtc`, `PolicyId`
   - `LegalHoldAppliedNotification` — sealed record implementing `INotification`: `HoldId`, `EntityId`, `Reason`, `AppliedAtUtc`
   - `LegalHoldReleasedNotification` — sealed record implementing `INotification`: `HoldId`, `EntityId`, `ReleasedAtUtc`
   - `RetentionEnforcementCompletedNotification` — sealed record implementing `INotification`: `DeletionResult`, `OccurredAtUtc`

5. **`PublicAPI.Unshipped.txt`** — Add all public types

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of Encina.Compliance.Retention (Issue #406).

CONTEXT:
- Encina is a .NET 10 / C# 14 library using Railway Oriented Programming (Either<EncinaError, T>)
- This is a NEW project: src/Encina.Compliance.Retention/
- Reference existing patterns in src/Encina.Compliance.Anonymization/Model/ and src/Encina.Compliance.DataSubjectRights/Model/
- All domain models are sealed records with XML documentation
- Use LanguageExt for Option<T> and Either<L, R>
- Timestamps use DateTimeOffset with AtUtc suffix convention
- Notification records implement INotification from Encina core

TASK:
Create the project file and all model types listed in Phase 1 Tasks.

KEY RULES:
- Target net10.0, enable nullable, enable implicit usings
- All types are sealed records (not classes)
- All public types need XML documentation with <summary>, <remarks>, and GDPR article references
- LegalHold.IsActive is a computed property: ReleasedAtUtc is null
- ExpiringData.DaysUntilExpiration is computed from ExpiresAtUtc - DateTimeOffset.UtcNow
- RetentionPolicy.RetentionPeriod uses TimeSpan (supports days, months, years via factory methods)
- DeletionResult tracks all outcomes: deleted, retained, failed, held
- Notification records follow INotification pattern from Encina core
- Add InternalsVisibleTo for all 13 providers and all test assemblies
- Add PublicAPI.Unshipped.txt with all public symbols
- Reference Encina.Compliance.DataSubjectRights for ErasureScope, ErasureResult types

REFERENCE FILES:
- src/Encina.Compliance.Anonymization/Model/AnonymizationAuditEntry.cs (audit record pattern)
- src/Encina.Compliance.DataSubjectRights/Model/ErasureResult.cs (deletion result pattern)
- src/Encina.Compliance.DataSubjectRights/Model/DSRRequest.cs (sealed record pattern with status)
- src/Encina.Compliance.DataSubjectRights/Notifications/ (INotification pattern)
- src/Encina.Compliance.Anonymization/Encina.Compliance.Anonymization.csproj (project file pattern)
```

</details>

---

### Phase 2: Core Interfaces, Attributes & Error Codes

> **Goal**: Define the public API surface — interfaces, attributes, and error codes.

<details>
<summary><strong>Tasks</strong></summary>

1. **Attributes** (`Attributes/` folder):
   - `RetentionPeriodAttribute` — `[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]`
     - Properties: `Days (int)`, `Years (int)` (mutually exclusive — use one), `Reason (string?)`, `AutoDelete (bool, default true)`, `DataCategory (string?)`
     - Computed: `RetentionPeriod` → `TimeSpan.FromDays(Days)` or `TimeSpan.FromDays(Years * 365)`

2. **Core interfaces** (`Abstractions/` folder):
   - `IRetentionPolicy` — retention period resolution:
     - `GetRetentionPeriodAsync(string dataCategory, CancellationToken)` → `Either<EncinaError, TimeSpan>`
     - `IsExpiredAsync(string entityId, string dataCategory, CancellationToken)` → `Either<EncinaError, bool>`
   - `IRetentionEnforcer` — enforcement orchestration:
     - `EnforceRetentionAsync(CancellationToken)` → `Either<EncinaError, DeletionResult>`
     - `GetExpiringDataAsync(TimeSpan within, CancellationToken)` → `Either<EncinaError, IReadOnlyList<ExpiringData>>`
   - `ILegalHoldManager` — legal hold lifecycle:
     - `ApplyHoldAsync(string entityId, LegalHold hold, CancellationToken)` → `Either<EncinaError, Unit>`
     - `ReleaseHoldAsync(string holdId, string? releasedByUserId, CancellationToken)` → `Either<EncinaError, Unit>`
     - `IsUnderHoldAsync(string entityId, CancellationToken)` → `Either<EncinaError, bool>`
     - `GetActiveHoldsAsync(CancellationToken)` → `Either<EncinaError, IReadOnlyList<LegalHold>>`
   - `IRetentionPolicyStore` — policy CRUD:
     - `CreateAsync(RetentionPolicy policy, CancellationToken)` → `Either<EncinaError, Unit>`
     - `GetByIdAsync(string policyId, CancellationToken)` → `Either<EncinaError, Option<RetentionPolicy>>`
     - `GetByCategoryAsync(string dataCategory, CancellationToken)` → `Either<EncinaError, Option<RetentionPolicy>>`
     - `GetAllAsync(CancellationToken)` → `Either<EncinaError, IReadOnlyList<RetentionPolicy>>`
     - `UpdateAsync(RetentionPolicy policy, CancellationToken)` → `Either<EncinaError, Unit>`
     - `DeleteAsync(string policyId, CancellationToken)` → `Either<EncinaError, Unit>`
   - `IRetentionRecordStore` — record tracking:
     - `CreateAsync(RetentionRecord record, CancellationToken)` → `Either<EncinaError, Unit>`
     - `GetByIdAsync(string recordId, CancellationToken)` → `Either<EncinaError, Option<RetentionRecord>>`
     - `GetByEntityIdAsync(string entityId, CancellationToken)` → `Either<EncinaError, IReadOnlyList<RetentionRecord>>`
     - `GetExpiredRecordsAsync(CancellationToken)` → `Either<EncinaError, IReadOnlyList<RetentionRecord>>`
     - `GetExpiringWithinAsync(TimeSpan within, CancellationToken)` → `Either<EncinaError, IReadOnlyList<RetentionRecord>>`
     - `UpdateStatusAsync(string recordId, RetentionStatus newStatus, CancellationToken)` → `Either<EncinaError, Unit>`
     - `GetAllAsync(CancellationToken)` → `Either<EncinaError, IReadOnlyList<RetentionRecord>>`
   - `ILegalHoldStore` — legal hold persistence:
     - `CreateAsync(LegalHold hold, CancellationToken)` → `Either<EncinaError, Unit>`
     - `GetByIdAsync(string holdId, CancellationToken)` → `Either<EncinaError, Option<LegalHold>>`
     - `GetByEntityIdAsync(string entityId, CancellationToken)` → `Either<EncinaError, IReadOnlyList<LegalHold>>`
     - `IsUnderHoldAsync(string entityId, CancellationToken)` → `Either<EncinaError, bool>`
     - `GetActiveHoldsAsync(CancellationToken)` → `Either<EncinaError, IReadOnlyList<LegalHold>>`
     - `ReleaseAsync(string holdId, string? releasedByUserId, DateTimeOffset releasedAtUtc, CancellationToken)` → `Either<EncinaError, Unit>`
     - `GetAllAsync(CancellationToken)` → `Either<EncinaError, IReadOnlyList<LegalHold>>`
   - `IRetentionAuditStore` — audit trail:
     - `RecordAsync(RetentionAuditEntry entry, CancellationToken)` → `Either<EncinaError, Unit>`
     - `GetByEntityIdAsync(string entityId, CancellationToken)` → `Either<EncinaError, IReadOnlyList<RetentionAuditEntry>>`
     - `GetAllAsync(CancellationToken)` → `Either<EncinaError, IReadOnlyList<RetentionAuditEntry>>`

3. **Error codes** (`RetentionErrors.cs`):
   - `PolicyNotFoundCode = "retention.policy_not_found"`
   - `PolicyAlreadyExistsCode = "retention.policy_already_exists"`
   - `RecordNotFoundCode = "retention.record_not_found"`
   - `RecordAlreadyExistsCode = "retention.record_already_exists"`
   - `HoldNotFoundCode = "retention.hold_not_found"`
   - `HoldAlreadyActiveCode = "retention.hold_already_active"`
   - `HoldAlreadyReleasedCode = "retention.hold_already_released"`
   - `EnforcementFailedCode = "retention.enforcement_failed"`
   - `DeletionFailedCode = "retention.deletion_failed"`
   - `StoreErrorCode = "retention.store_error"`
   - `InvalidParameterCode = "retention.invalid_parameter"`
   - `NoPolicyForCategoryCode = "retention.no_policy_for_category"`
   - Factory methods: `PolicyNotFound(string policyId)`, `RecordNotFound(string recordId)`, `HoldNotFound(string holdId)`, `HoldAlreadyActive(string entityId)`, `HoldAlreadyReleased(string holdId)`, `EnforcementFailed(string reason, Exception? ex)`, `DeletionFailed(string entityId, string reason)`, `StoreError(string operation, string message, Exception? ex)`, `InvalidParameter(string paramName, string message)`, `NoPolicyForCategory(string dataCategory)`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of Encina.Compliance.Retention (Issue #406).

CONTEXT:
- Phase 1 is implemented (all model types, enums, notification records)
- Encina uses Railway Oriented Programming: Either<EncinaError, T> on all store/handler methods
- All stores return ValueTask<Either<EncinaError, T>>
- Optional results use Option<T> from LanguageExt
- Collection returns use IReadOnlyList<T>
- Void returns use Unit from LanguageExt
- Error factory methods use EncinaErrors.Create() pattern

TASK:
Create all interfaces, the RetentionPeriodAttribute, and RetentionErrors static class.

KEY RULES:
- RetentionPeriodAttribute targets Class | Property with AllowMultiple = false
- Days and Years are mutually exclusive — validate in computed RetentionPeriod property
- All interface methods return ValueTask<Either<EncinaError, T>> with CancellationToken
- IRetentionRecordStore.GetExpiredRecordsAsync returns records where ExpiresAtUtc < now AND Status == Active
- IRetentionRecordStore.GetExpiringWithinAsync returns records expiring within the given TimeSpan
- ILegalHoldStore.IsUnderHoldAsync returns true if ANY active hold exists for the entity
- Error codes follow "retention.{category}" pattern
- Error factory methods include metadata dictionary with stage key
- All public types need comprehensive XML documentation with GDPR Art. 5(1)(e) references

REFERENCE FILES:
- src/Encina.Compliance.Anonymization/Abstractions/ (store interface pattern)
- src/Encina.Compliance.Anonymization/AnonymizationErrors.cs (error factory pattern)
- src/Encina.Compliance.Anonymization/Attributes/ (attribute pattern)
- src/Encina.Compliance.DataSubjectRights/Abstractions/IDSRRequestStore.cs (store with query methods)
```

</details>

---

### Phase 3: Default Implementations & InMemory Stores

> **Goal**: Provide working default implementations for development and testing.

<details>
<summary><strong>Tasks</strong></summary>

1. **InMemory stores** (`InMemory/` folder):
   - `InMemoryRetentionPolicyStore` — `ConcurrentDictionary<string, RetentionPolicy>` keyed by Id, secondary index by DataCategory
     - Testing helpers: `Count`, `Clear()`, `GetAllPolicies()`
   - `InMemoryRetentionRecordStore` — `ConcurrentDictionary<string, RetentionRecord>` keyed by Id, secondary index by EntityId
     - `GetExpiredRecordsAsync`: filter by `ExpiresAtUtc < TimeProvider.GetUtcNow() && Status == Active`
     - `GetExpiringWithinAsync`: filter by `ExpiresAtUtc <= TimeProvider.GetUtcNow() + within && Status == Active`
     - Testing helpers: `Count`, `Clear()`, `GetAllRecords()`
   - `InMemoryLegalHoldStore` — `ConcurrentDictionary<string, LegalHold>` keyed by Id, secondary index by EntityId
     - `IsUnderHoldAsync`: check if any hold for entityId has `IsActive == true`
     - Testing helpers: `Count`, `Clear()`
   - `InMemoryRetentionAuditStore` — `ConcurrentDictionary<string, RetentionAuditEntry>` keyed by Id
     - `GetByEntityIdAsync`: filter + order by OccurredAtUtc descending
     - Testing helpers: `Count`, `Clear()`

2. **Default `IRetentionPolicy`** (`DefaultRetentionPolicy.cs`):
   - Constructor: `(IRetentionPolicyStore policyStore, IRetentionRecordStore recordStore, TimeProvider timeProvider, ILogger<DefaultRetentionPolicy>)`
   - `GetRetentionPeriodAsync`: lookup policy by category → return period, or return default from options
   - `IsExpiredAsync`: lookup record by entityId + category → check ExpiresAtUtc against current time

3. **Default `IRetentionEnforcer`** (`DefaultRetentionEnforcer.cs`):
   - Constructor: `(IRetentionRecordStore recordStore, ILegalHoldStore holdStore, IRetentionAuditStore auditStore, IOptions<RetentionOptions> options, IServiceProvider serviceProvider, TimeProvider timeProvider, ILogger<DefaultRetentionEnforcer>)`
   - `EnforceRetentionAsync`:
     1. Get expired records from store
     2. For each: check legal hold → if held, update status to `UnderLegalHold`
     3. For non-held: optionally delegate to `IDataErasureExecutor` (resolved from IServiceProvider, may not be registered)
     4. Update record status to `Deleted`
     5. Record audit entries
     6. Publish notifications
     7. Return `DeletionResult`
   - `GetExpiringDataAsync`: query records expiring within timespan, map to `ExpiringData`

4. **Default `ILegalHoldManager`** (`DefaultLegalHoldManager.cs`):
   - Constructor: `(ILegalHoldStore holdStore, IRetentionRecordStore recordStore, IRetentionAuditStore auditStore, ILogger<DefaultLegalHoldManager>)`
   - `ApplyHoldAsync`: create hold + update matching retention records to `UnderLegalHold` status + audit
   - `ReleaseHoldAsync`: release hold + revert matching records to `Expired` status (if still expired) + audit
   - `IsUnderHoldAsync`: delegate to store
   - `GetActiveHoldsAsync`: delegate to store

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of Encina.Compliance.Retention (Issue #406).

CONTEXT:
- Phases 1-2 are implemented (models, interfaces, errors, attributes)
- InMemory stores are the default implementations, registered via TryAdd (overridable by satellite providers)
- All stores use ConcurrentDictionary for thread safety
- TimeProvider injection for testable time-dependent logic
- IDataErasureExecutor from DSR is an OPTIONAL dependency (may not be registered)

TASK:
Create InMemory stores and default service implementations.

KEY RULES:
- InMemory stores: ConcurrentDictionary, testing helpers (Count, Clear), ArgumentNullException.ThrowIfNull
- DefaultRetentionEnforcer: resolve IDataErasureExecutor via IServiceProvider.GetService<T>() — null if DSR not registered
- DefaultRetentionEnforcer: if IDataErasureExecutor is null, skip actual data deletion but still update record status and audit
- DefaultLegalHoldManager: applying a hold cascades to all retention records for that entityId
- DefaultLegalHoldManager: releasing a hold recalculates record status (Expired if past ExpiresAtUtc, Active if not)
- Use TimeProvider.GetUtcNow() instead of DateTimeOffset.UtcNow for testability
- All methods return Either<EncinaError, T> — never throw exceptions for business logic
- Log operations using ILogger (placeholder until Phase 8 adds [LoggerMessage])

REFERENCE FILES:
- src/Encina.Compliance.Anonymization/InMemory/InMemoryTokenMappingStore.cs (InMemory pattern)
- src/Encina.Compliance.Anonymization/InMemory/InMemoryAnonymizationAuditStore.cs (audit store)
- src/Encina.Compliance.DataSubjectRights/Erasure/DefaultDataErasureExecutor.cs (erasure delegation pattern)
- src/Encina.Compliance.DataSubjectRights/DefaultDataSubjectRightsHandler.cs (handler with optional deps)
```

</details>

---

### Phase 4: Pipeline Behavior & Enforcement Service

> **Goal**: Implement the `RetentionValidationPipelineBehavior` and the `RetentionEnforcementService` background scheduler.

<details>
<summary><strong>Tasks</strong></summary>

1. **`RetentionValidationPipelineBehavior.cs`** (root):
   - `sealed class RetentionValidationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>`
   - Constructor: `(IRetentionPolicyStore policyStore, IRetentionRecordStore recordStore, IOptions<RetentionOptions> options, TimeProvider timeProvider, ILogger<RetentionValidationPipelineBehavior<TRequest, TResponse>>)`
   - Static cached `RetentionAttributeInfo?` per closed generic type
   - Handle flow:
     1. If enforcement mode is `Disabled` → call `nextStep()`, return
     2. If no `[RetentionPeriod]` attributes on TResponse → call `nextStep()`, return
     3. Call `nextStep()` → get response
     4. If response is error → return error
     5. For each decorated property: create `RetentionRecord` with computed `ExpiresAtUtc`
     6. Store records via `IRetentionRecordStore.CreateAsync()`
     7. Return response
   - On failure:
     - Block mode → return `RetentionErrors.NoPolicyForCategory(category)`
     - Warn mode → log warning, return response untouched

2. **`RetentionEnforcementService.cs`** (root):
   - `internal sealed class RetentionEnforcementService : IHostedService, IDisposable`
   - Constructor: `(IServiceProvider serviceProvider, IOptions<RetentionOptions> options, ILogger<RetentionEnforcementService>)`
   - Uses `PeriodicTimer` with interval from `RetentionOptions.EnforcementIntervalMinutes`
   - `StartAsync`: start background loop if `EnableAutomaticEnforcement` is true
   - Background loop:
     1. Create scope via `IServiceProvider.CreateScope()`
     2. Resolve `IRetentionEnforcer` from scope
     3. Call `EnforceRetentionAsync()`
     4. Resolve `IRetentionEnforcer.GetExpiringDataAsync()` for alert window
     5. Publish `DataExpiringNotification` for items within alert window
     6. Log results
     7. Handle errors gracefully (log + continue)
   - `StopAsync`: cancel background loop
   - `Dispose`: dispose timer

3. **`RetentionAttributeInfo.cs`** (internal, root):
   - Internal record holding cached reflection data for `[RetentionPeriod]` attributes on TResponse
   - Fields: `IReadOnlyList<(PropertyInfo Property, RetentionPeriodAttribute Attribute)> DecoratedProperties`

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of Encina.Compliance.Retention (Issue #406).

CONTEXT:
- Phases 1-3 are implemented (models, interfaces, stores, default implementations)
- Pipeline behavior follows the exact pattern from Anonymization: static cached attribute info per closed generic type
- Enforcement service is a background IHostedService using PeriodicTimer
- Three enforcement modes: Block, Warn, Disabled

TASK:
Create RetentionValidationPipelineBehavior, RetentionEnforcementService, and RetentionAttributeInfo.

KEY RULES:
Pipeline Behavior:
- Static field: private static readonly RetentionAttributeInfo? CachedAttributeInfo = ResolveAttributeInfo()
- ResolveAttributeInfo scans TResponse properties for [RetentionPeriod] attribute
- Returns null if no decorated properties found (signals "skip entirely")
- ExpiresAtUtc = TimeProvider.GetUtcNow() + attribute.RetentionPeriod
- Enforcement mode checked first (Disabled → immediate passthrough)
- No attribute → immediate passthrough (no overhead)

Enforcement Service:
- PeriodicTimer with configurable interval (default 60 min)
- Creates IServiceScope per tick (scoped services)
- Graceful error handling: log + continue on failure, never crash the host
- CancellationToken from StopAsync propagates to enforcement
- Configurable via RetentionOptions.EnableAutomaticEnforcement (default true)
- Alert window: RetentionOptions.AlertBeforeExpirationDays (default 7)

REFERENCE FILES:
- src/Encina.Compliance.Anonymization/AnonymizationPipelineBehavior.cs (pipeline behavior pattern)
- src/Encina.Compliance.DataSubjectRights/ProcessingRestrictionPipelineBehavior.cs (alternative behavior pattern)
- src/Encina.Compliance.Anonymization/AnonymizationAutoRegistrationHostedService.cs (IHostedService pattern)
```

</details>

---

### Phase 5: Configuration, DI & Auto-Registration

> **Goal**: Options, validation, DI registration, health check, and auto-registration.

<details>
<summary><strong>Tasks</strong></summary>

1. **`RetentionEnforcementMode.cs`** (root):
   - `public enum RetentionEnforcementMode { Block = 0, Warn = 1, Disabled = 2 }`

2. **`RetentionOptions.cs`** (root):
   - `public sealed class RetentionOptions`
   - Properties:
     - `EnforcementMode` (RetentionEnforcementMode, default: Block)
     - `DefaultRetentionDays` (int, default: 365)
     - `AutoDeleteExpired` (bool, default: true)
     - `EnableAutomaticEnforcement` (bool, default: true)
     - `EnforcementIntervalMinutes` (int, default: 60)
     - `AlertBeforeExpirationDays` (int, default: 7)
     - `BackupDeletionDeadlineDays` (int, default: 30)
     - `TrackAuditTrail` (bool, default: true)
     - `PublishNotifications` (bool, default: true)
     - `AddHealthCheck` (bool, default: false)
     - `AutoRegisterFromAttributes` (bool, default: true)
     - `AssembliesToScan` (List<Assembly>, default: [])
   - Method: `AddPolicy(string dataCategory, Action<RetentionPolicyBuilder> configure)` — fluent policy configuration
   - `RetentionPolicyBuilder` nested class: `RetentionDays`, `RetentionYears`, `Reason`, `AutoDelete`, `LegalBasis`

3. **`RetentionOptionsValidator.cs`** (root):
   - `internal sealed class RetentionOptionsValidator : IValidateOptions<RetentionOptions>`
   - Validates: EnforcementMode defined, DefaultRetentionDays > 0, EnforcementIntervalMinutes > 0, AlertBeforeExpirationDays >= 0

4. **`RetentionAutoRegistrationDescriptor.cs`** (root):
   - `internal sealed record RetentionAutoRegistrationDescriptor(IReadOnlyList<Assembly> Assemblies)`

5. **`RetentionAutoRegistrationHostedService.cs`** (root):
   - `internal sealed class RetentionAutoRegistrationHostedService : IHostedService`
   - Constructor: `(RetentionAutoRegistrationDescriptor descriptor, IOptions<RetentionOptions> options, IRetentionPolicyStore policyStore, ILogger<...>)`
   - `StartAsync`: scan assemblies for `[RetentionPeriod]` attributes, auto-create `RetentionPolicy` entries for discovered categories
   - Log discovered types, field count, and created policies

6. **`Health/RetentionHealthCheck.cs`**:
   - `public sealed class RetentionHealthCheck : IHealthCheck`
   - `public const string DefaultName = "encina-retention"`
   - `private static readonly string[] DefaultTags = ["encina", "gdpr", "retention", "compliance", "ready"]`
   - Checks: options configured, IRetentionPolicyStore resolvable, IRetentionRecordStore resolvable, ILegalHoldStore resolvable, optional IDataErasureExecutor (Degraded if missing when AutoDeleteExpired)

7. **`ServiceCollectionExtensions.cs`** (root):
   - `public static IServiceCollection AddEncinaRetention(this IServiceCollection services, Action<RetentionOptions>? configure = null)`
   - Registers:
     - `RetentionOptions` + `RetentionOptionsValidator`
     - `TimeProvider` (TryAddSingleton)
     - `IRetentionPolicyStore` → `InMemoryRetentionPolicyStore` (TryAddSingleton)
     - `IRetentionRecordStore` → `InMemoryRetentionRecordStore` (TryAddSingleton)
     - `ILegalHoldStore` → `InMemoryLegalHoldStore` (TryAddSingleton)
     - `IRetentionAuditStore` → `InMemoryRetentionAuditStore` (TryAddSingleton)
     - `IRetentionPolicy` → `DefaultRetentionPolicy` (TryAddScoped)
     - `IRetentionEnforcer` → `DefaultRetentionEnforcer` (TryAddScoped)
     - `ILegalHoldManager` → `DefaultLegalHoldManager` (TryAddScoped)
     - `IPipelineBehavior<,>` → `RetentionValidationPipelineBehavior<,>` (TryAddTransient)
     - `RetentionEnforcementService` (AddHostedService, conditional on EnableAutomaticEnforcement)
     - Health check (conditional on AddHealthCheck)
     - Auto-registration hosted service + descriptor (conditional on AutoRegisterFromAttributes)
   - Create policies from `RetentionOptions.AddPolicy()` configuration

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of Encina.Compliance.Retention (Issue #406).

CONTEXT:
- Phases 1-4 are implemented (models, interfaces, stores, implementations, pipeline, scheduler)
- DI registration follows the exact TryAdd pattern from Anonymization
- Options validation uses IValidateOptions<T> pattern
- Health check follows const DefaultName + static DefaultTags pattern

TASK:
Create RetentionEnforcementMode, RetentionOptions, validator, auto-registration, health check, and ServiceCollectionExtensions.

KEY RULES:
- All DI registrations use TryAdd (allows satellite providers to override before core registration)
- RetentionOptions.AddPolicy() creates RetentionPolicyBuilder for fluent configuration
- Auto-registration scans for [RetentionPeriod] attributes and creates matching RetentionPolicy entries
- Health check creates scoped IServiceProvider to verify DI graph
- Enforcement service only registered if EnableAutomaticEnforcement = true
- Health check only registered if AddHealthCheck = true
- Auto-registration only registered if AutoRegisterFromAttributes = true AND AssembliesToScan.Count > 0
- Instantiate options locally (new RetentionOptions()) to inspect flags before registration

REFERENCE FILES:
- src/Encina.Compliance.Anonymization/ServiceCollectionExtensions.cs (DI pattern)
- src/Encina.Compliance.Anonymization/AnonymizationOptions.cs (options pattern)
- src/Encina.Compliance.Anonymization/AnonymizationOptionsValidator.cs (validator pattern)
- src/Encina.Compliance.Anonymization/Health/AnonymizationHealthCheck.cs (health check pattern)
- src/Encina.Compliance.Anonymization/AnonymizationAutoRegistrationHostedService.cs (auto-registration)
- src/Encina.Compliance.Anonymization/AnonymizationEnforcementMode.cs (enforcement mode enum)
```

</details>

---

### Phase 6: Persistence Entities, Mappers & SQL Scripts

> **Goal**: Define database-facing entities, domain ↔ entity mappers, and provider-specific SQL scripts.

<details>
<summary><strong>Tasks</strong></summary>

1. **Persistence entities** (root):
   - `RetentionPolicyEntity.cs` — flat POCO: `Id`, `DataCategory`, `RetentionPeriodTicks (long)`, `AutoDelete (bool)`, `Reason`, `LegalBasis`, `PolicyType (int)`, `CreatedAtUtc`, `LastModifiedAtUtc`
   - `RetentionRecordEntity.cs` — flat POCO: `Id`, `EntityId`, `DataCategory`, `PolicyId`, `CreatedAtUtc`, `ExpiresAtUtc`, `Status (int)`, `DeletedAtUtc`, `LegalHoldId`
   - `LegalHoldEntity.cs` — flat POCO: `Id`, `EntityId`, `Reason`, `AppliedByUserId`, `AppliedAtUtc`, `ReleasedAtUtc`, `ReleasedByUserId`
   - `RetentionAuditEntryEntity.cs` — flat POCO: `Id`, `Action`, `EntityId`, `DataCategory`, `Detail`, `PerformedByUserId`, `OccurredAtUtc`

2. **Mappers** (root):
   - `RetentionPolicyMapper.cs` — static class: `ToDomain(RetentionPolicyEntity)` → `RetentionPolicy`, `ToEntity(RetentionPolicy)` → `RetentionPolicyEntity`
   - `RetentionRecordMapper.cs` — static class: `ToDomain(RetentionRecordEntity)` → `RetentionRecord`, `ToEntity(RetentionRecord)` → `RetentionRecordEntity`
   - `LegalHoldMapper.cs` — static class: `ToDomain(LegalHoldEntity)` → `LegalHold`, `ToEntity(LegalHold)` → `LegalHoldEntity`
   - `RetentionAuditMapper.cs` — static class: `ToDomain(RetentionAuditEntryEntity)` → `RetentionAuditEntry`, `ToEntity(RetentionAuditEntry)` → `RetentionAuditEntryEntity`

3. **SQL table schema reference** (documentation, not executed — for provider implementations):
   - `RetentionPolicies` table: Id (PK), DataCategory (UNIQUE), RetentionPeriodTicks, AutoDelete, Reason, LegalBasis, PolicyType, CreatedAtUtc, LastModifiedAtUtc
   - `RetentionRecords` table: Id (PK), EntityId (INDEX), DataCategory, PolicyId (FK nullable), CreatedAtUtc, ExpiresAtUtc (INDEX), Status, DeletedAtUtc, LegalHoldId
   - `LegalHolds` table: Id (PK), EntityId (INDEX), Reason, AppliedByUserId, AppliedAtUtc, ReleasedAtUtc, ReleasedByUserId
   - `RetentionAuditEntries` table: Id (PK), Action, EntityId, DataCategory, Detail, PerformedByUserId, OccurredAtUtc

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of Encina.Compliance.Retention (Issue #406).

CONTEXT:
- Phases 1-5 are implemented (full core package with DI)
- Persistence entities are flat POCOs mapping to database tables
- Mappers are static classes converting between domain records and entities
- RetentionPeriod stored as Ticks (long) for database portability
- Status and PolicyType stored as int for database portability

TASK:
Create persistence entities, static mappers, and SQL schema documentation.

KEY RULES:
- Entities are simple POCOs (no domain logic, no validation)
- Mappers are static classes with ToDomain/ToEntity methods
- RetentionPolicy.RetentionPeriod ↔ RetentionPolicyEntity.RetentionPeriodTicks (TimeSpan.Ticks / new TimeSpan(ticks))
- RetentionStatus enum ↔ int cast
- RetentionPolicyType enum ↔ int cast
- LegalHold.IsActive is computed in domain (not stored)
- DateTimeOffset storage: SQL Server uses datetimeoffset(7), SQLite uses TEXT (ISO 8601 "O"), PostgreSQL uses timestamptz, MySQL uses datetime(6)
- All public types need XML documentation

REFERENCE FILES:
- src/Encina.Compliance.Anonymization/TokenMappingEntity.cs (entity pattern)
- src/Encina.Compliance.Anonymization/TokenMappingMapper.cs (mapper pattern)
- src/Encina.Compliance.DataSubjectRights/DSRRequestEntity.cs (entity with status enum)
- src/Encina.Compliance.DataSubjectRights/DSRRequestMapper.cs (mapper with enum conversion)
```

</details>

---

### Phase 7: Multi-Provider Store Implementations (All 13 Providers)

> **Goal**: Implement `IRetentionPolicyStore`, `IRetentionRecordStore`, `ILegalHoldStore`, and `IRetentionAuditStore` across all 13 database providers.

<details>
<summary><strong>Tasks</strong></summary>

#### Phase 7a: ADO.NET Providers (4 providers × 4 stores = 16 files)

For each of **Sqlite, SqlServer, PostgreSQL, MySQL**:

1. `RetentionPolicyStoreADO.cs` — in `src/Encina.ADO.{Provider}/Retention/`
   - Constructor: `(Func<DbConnection> connectionFactory, TimeProvider timeProvider, ILogger<...>)`
   - Raw SQL with `DbCommand` and `DbDataReader`
   - Provider-specific SQL syntax (TOP vs LIMIT, parameter style, date format)

2. `RetentionRecordStoreADO.cs` — in `src/Encina.ADO.{Provider}/Retention/`
   - `GetExpiredRecordsAsync`: `WHERE ExpiresAtUtc < @NowUtc AND Status = @ActiveStatus`
   - SQLite: dates as ISO 8601 TEXT, parameterized `@NowUtc` (never `datetime('now')`)

3. `LegalHoldStoreADO.cs` — in `src/Encina.ADO.{Provider}/Retention/`
   - `IsUnderHoldAsync`: `SELECT CASE WHEN EXISTS(...) THEN 1 ELSE 0 END`

4. `RetentionAuditStoreADO.cs` — in `src/Encina.ADO.{Provider}/Retention/`

5. `ServiceCollectionExtensions.cs` — update in each ADO provider to add `AddEncinaRetentionADO{Provider}()`

#### Phase 7b: Dapper Providers (4 providers × 4 stores = 16 files)

For each of **Sqlite, SqlServer, PostgreSQL, MySQL**:

1-4. Same 4 stores as ADO but using Dapper's `QueryAsync`, `ExecuteAsync`, `QueryFirstOrDefaultAsync`
5. `ServiceCollectionExtensions.cs` — `AddEncinaRetentionDapper{Provider}()`

#### Phase 7c: EF Core Provider (4 store classes + entity configs + DbContext extension)

1. `RetentionPolicyStoreEF.cs` — in `src/Encina.EntityFrameworkCore/Retention/`
2. `RetentionRecordStoreEF.cs`
3. `LegalHoldStoreEF.cs`
4. `RetentionAuditStoreEF.cs`
5. Entity configurations: `RetentionPolicyEntityConfiguration.cs`, `RetentionRecordEntityConfiguration.cs`, `LegalHoldEntityConfiguration.cs`, `RetentionAuditEntryEntityConfiguration.cs`
6. `ServiceCollectionExtensions.cs` — `AddEncinaRetentionEFCore()`

#### Phase 7d: MongoDB Provider (4 store classes + document models)

1. `RetentionPolicyStoreMongoDB.cs` — in `src/Encina.MongoDB/Retention/`
2. `RetentionRecordStoreMongoDB.cs`
3. `LegalHoldStoreMongoDB.cs`
4. `RetentionAuditStoreMongoDB.cs`
5. MongoDB document models (if different from entities)
6. `ServiceCollectionExtensions.cs` — `AddEncinaRetentionMongoDB()`

#### Satellite DI Pattern

All satellite registrations:
- Replace InMemory stores with provider-specific stores (using `TryAdd` → registered BEFORE core `AddEncinaRetention()`)
- Accept connection string or factory as parameter
- Return `IServiceCollection` for chaining

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 7</strong></summary>

```
You are implementing Phase 7 of Encina.Compliance.Retention (Issue #406).

CONTEXT:
- Phases 1-6 are implemented (full core + persistence entities + mappers)
- 13 database providers must implement 4 stores each: IRetentionPolicyStore, IRetentionRecordStore, ILegalHoldStore, IRetentionAuditStore
- Satellite DI pattern: AddEncinaRetention{Provider}() registers provider-specific stores BEFORE AddEncinaRetention()

TASK:
Implement all 13 provider stores (4 stores × 13 providers) with satellite DI extensions.

KEY RULES:
Provider-specific SQL:
- SQLite: @param, LIMIT @n, 0/1 booleans, TEXT dates (ISO 8601 "O"), never datetime('now'), use parameterized @NowUtc
- SQL Server: @param, TOP (@n), bit booleans, datetimeoffset(7)
- PostgreSQL: @param, LIMIT @n, true/false booleans, timestamptz, case-sensitive identifiers
- MySQL: @param, LIMIT @n, 0/1 booleans, backtick identifiers, datetime(6)

ADO.NET Pattern:
- Use DbCommand + DbDataReader
- Wrap in try/catch returning Either<EncinaError, T>
- Connection from factory (never dispose shared connection for SQLite)

Dapper Pattern:
- Use QueryAsync<TEntity>, ExecuteAsync, QueryFirstOrDefaultAsync<TEntity>
- Map entities via static mappers (ToDomain/ToEntity)

EF Core Pattern:
- Use DbContext with IEntityTypeConfiguration<T> for fluent configuration
- LINQ queries, no raw SQL
- SaveChangesAsync returns row count

MongoDB Pattern:
- Use IMongoCollection<TDocument>
- Filter definitions, UpdateDefinitions
- InsertOneAsync, Find, ReplaceOneAsync

Satellite DI:
- AddEncinaRetention{Provider}(services, connectionString) or connectionFactory
- All use TryAdd to allow user override
- Registered BEFORE AddEncinaRetention() in user's startup

REFERENCE FILES:
- src/Encina.ADO.Sqlite/Consent/ (ADO SQLite store pattern)
- src/Encina.Dapper.Sqlite/Consent/ (Dapper SQLite store pattern)
- src/Encina.EntityFrameworkCore/Consent/ (EF Core store pattern)
- src/Encina.MongoDB/Consent/ (MongoDB store pattern)
- src/Encina.ADO.SqlServer/Consent/ (ADO SQL Server store pattern)
- src/Encina.Dapper.PostgreSQL/Consent/ (Dapper PostgreSQL pattern)
```

</details>

---

### Phase 8: Observability — ActivitySource, Meter & LoggerMessage

> **Goal**: Full OpenTelemetry instrumentation and structured logging.

<details>
<summary><strong>Tasks</strong></summary>

1. **`Diagnostics/RetentionDiagnostics.cs`**:
   - `internal static class RetentionDiagnostics`
   - `ActivitySource`: `"Encina.Compliance.Retention"`, version `"1.0"`
   - `Meter`: `"Encina.Compliance.Retention"`, version `"1.0"`
   - **Tag constants**:
     - `TagOutcome = "retention.outcome"` — completed, blocked, warned, failed, skipped
     - `TagDataCategory = "retention.data_category"`
     - `TagPolicyId = "retention.policy_id"`
     - `TagEntityId = "retention.entity_id"`
     - `TagEnforcementMode = "retention.enforcement_mode"`
     - `TagHoldId = "retention.hold_id"`
     - `TagAction = "retention.action"` — enforce, track, hold_apply, hold_release
   - **Counters** (Counter<long>):
     - `PipelineExecutionsTotal` — `retention.pipeline.executions.total` — tagged by outcome
     - `RecordsTrackedTotal` — `retention.records.tracked.total` — tagged by data_category
     - `EnforcementExecutionsTotal` — `retention.enforcement.executions.total` — tagged by outcome
     - `RecordsDeletedTotal` — `retention.records.deleted.total` — tagged by data_category
     - `RecordsRetainedTotal` — `retention.records.retained.total` — tagged by data_category (held, not expired)
     - `LegalHoldsAppliedTotal` — `retention.legal_holds.applied.total`
     - `LegalHoldsReleasedTotal` — `retention.legal_holds.released.total`
     - `ExpirationAlertsTotal` — `retention.expiration_alerts.total` — tagged by data_category
   - **Histograms** (Histogram<double>):
     - `PipelineDuration` — `retention.pipeline.duration` (ms)
     - `EnforcementDuration` — `retention.enforcement.duration` (ms)
   - **Activity helpers**:
     - `StartPipelineExecution(string requestType, string responseType)` → `Activity?`
     - `StartEnforcement()` → `Activity?`
     - `StartRecordTracking(string entityId, string dataCategory)` → `Activity?`
     - `StartLegalHoldOperation(string holdId, string action)` → `Activity?`
   - **Outcome recorders**:
     - `RecordCompleted(Activity?, int recordsAffected)`
     - `RecordFailed(Activity?, string reason)`
     - `RecordSkipped(Activity?)`
     - `RecordBlocked(Activity?, string dataCategory)`
     - `RecordWarned(Activity?, string dataCategory)`

2. **`Diagnostics/RetentionLogMessages.cs`**:
   - `internal static partial class RetentionLogMessages`
   - **Event ID range: 8500-8599**:
     - 8500: Pipeline started
     - 8501: Pipeline completed (records tracked)
     - 8502: Pipeline skipped (no attributes)
     - 8503: Pipeline blocked (no policy for category)
     - 8504: Pipeline warned (no policy for category)
     - 8510: Record tracked (entity registered for retention)
     - 8511: Record expired
     - 8512: Record deleted
     - 8513: Record status updated
     - 8520: Enforcement started
     - 8521: Enforcement completed (summary)
     - 8522: Enforcement skipped (no expired records)
     - 8523: Enforcement failed
     - 8524: Enforcement record skipped (under legal hold)
     - 8530: Legal hold applied
     - 8531: Legal hold released
     - 8532: Legal hold check performed
     - 8540: Expiration alert sent
     - 8541: Expiration alert window evaluated
     - 8550: Auto-registration completed
     - 8551: Auto-registration skipped
     - 8552: Auto-registration policy created
     - 8560: Health check completed
     - 8561: Health check degraded
     - 8570: Store error
     - 8571: Policy not found
     - 8580: Audit entry recorded

3. **Integration**: Wire diagnostics into existing implementations:
   - `DefaultRetentionEnforcer` → start enforcement activity, record counters
   - `DefaultLegalHoldManager` → start hold activity, record hold counters
   - `RetentionValidationPipelineBehavior` → start pipeline activity, record pipeline counters
   - `RetentionEnforcementService` → log enforcement cycle results

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 8</strong></summary>

```
You are implementing Phase 8 of Encina.Compliance.Retention (Issue #406).

CONTEXT:
- Phases 1-7 are implemented (full core + 13 providers)
- Observability follows OpenTelemetry patterns: ActivitySource for traces, Meter for metrics, ILogger for structured logs
- Event IDs in 8500-8599 range (avoids collision with GDPR 8100-8199, Consent 8200-8299, DSR 8300-8399, Anonymization 8400-8499)

TASK:
Create RetentionDiagnostics, RetentionLogMessages, and integrate observability into existing code.

KEY RULES:
- ActivitySource name: "Encina.Compliance.Retention" (matches package name)
- Meter: new Meter("Encina.Compliance.Retention", "1.0")
- All counters use tag-based dimensions for flexible dashboards
- RetentionLogMessages uses [LoggerMessage] source generator (partial class, partial methods)
- Log messages follow structured logging: "Retention {Action}. EntityId={EntityId}, Category={DataCategory}"
- Activity helpers check Source.HasListeners() before creating activities
- SetTag uses string constants from tag fields
- RecordCompleted sets ActivityStatusCode.Ok, RecordFailed sets ActivityStatusCode.Error
- After creating log messages, update all existing implementations to use them instead of raw ILogger calls

REFERENCE FILES:
- src/Encina.Compliance.Anonymization/Diagnostics/AnonymizationDiagnostics.cs (ActivitySource + counters)
- src/Encina.Compliance.Anonymization/Diagnostics/AnonymizationLogMessages.cs ([LoggerMessage] source generator)
- src/Encina.Compliance.DataSubjectRights/Diagnostics/ (DSR diagnostics pattern)
```

</details>

---

### Phase 9: Testing — 7 Test Types

> **Goal**: Comprehensive test coverage across all test categories.

<details>
<summary><strong>Tasks</strong></summary>

#### 9a. Unit Tests (`tests/Encina.UnitTests/Compliance/Retention/`)

- `RetentionPolicyTests.cs` — domain record creation, TimeSpan construction
- `RetentionRecordTests.cs` — status transitions, expiration computation
- `LegalHoldTests.cs` — IsActive computation, release logic
- `DeletionResultTests.cs` — outcome counting, detail aggregation
- `RetentionPeriodAttributeTests.cs` — Days vs Years, computed RetentionPeriod
- `RetentionPolicyMapperTests.cs` — domain ↔ entity round-trip, TimeSpan ↔ Ticks
- `RetentionRecordMapperTests.cs` — domain ↔ entity round-trip, status enum ↔ int
- `LegalHoldMapperTests.cs` — domain ↔ entity round-trip, IsActive computation
- `RetentionAuditMapperTests.cs` — domain ↔ entity round-trip
- `DefaultRetentionPolicyTests.cs` — GetRetentionPeriod, IsExpired with mocked store
- `DefaultRetentionEnforcerTests.cs` — enforcement with/without erasure executor, legal hold skip, audit trail
- `DefaultLegalHoldManagerTests.cs` — apply/release hold, cascading status updates
- `RetentionValidationPipelineBehaviorTests.cs` — Block/Warn/Disabled modes, attribute caching, record creation
- `RetentionEnforcementServiceTests.cs` — timer-based execution, graceful error handling, alert publishing
- `InMemoryRetentionPolicyStoreTests.cs` — CRUD operations, GetByCategory
- `InMemoryRetentionRecordStoreTests.cs` — CRUD, GetExpired, GetExpiringWithin
- `InMemoryLegalHoldStoreTests.cs` — CRUD, IsUnderHold, GetActiveHolds
- `InMemoryRetentionAuditStoreTests.cs` — record + query audit trail
- `RetentionOptionsValidatorTests.cs` — validation rules
- `ServiceCollectionExtensionsTests.cs` — DI registration verification
- `RetentionHealthCheckTests.cs` — Healthy/Degraded/Unhealthy scenarios
- `RetentionErrorsTests.cs` — error factory methods, metadata

**Target**: ~100-120 unit tests

#### 9b. Guard Tests (`tests/Encina.GuardTests/Compliance/Retention/`)

- All public constructors and methods: null checks for non-nullable parameters
- Cover: all interface implementations, options, mappers, attributes, pipeline behavior, stores
- Use `GuardClauses.xUnit` library

**Target**: ~50-70 guard tests

#### 9c. Contract Tests (`tests/Encina.ContractTests/Compliance/Retention/`)

- `IRetentionPolicyStoreContractTests.cs` — verify all 13 store implementations follow the same contract
- `IRetentionRecordStoreContractTests.cs` — verify record store contract
- `ILegalHoldStoreContractTests.cs` — verify hold store contract
- `IRetentionAuditStoreContractTests.cs` — verify audit store contract

**Target**: ~20-30 contract tests

#### 9d. Property Tests (`tests/Encina.PropertyTests/Compliance/Retention/`)

- `RetentionPolicyPropertyTests.cs` — RetentionPeriod always positive, mapper round-trip
- `RetentionRecordPropertyTests.cs` — ExpiresAtUtc always after CreatedAtUtc, status transitions
- `LegalHoldPropertyTests.cs` — IsActive iff ReleasedAtUtc is null
- `RetentionPolicyMapperPropertyTests.cs` — domain → entity → domain preserves all fields
- `DeletionResultPropertyTests.cs` — sum of all outcomes equals TotalRecordsEvaluated

**Target**: ~15-25 property tests

#### 9e. Integration Tests (`tests/Encina.IntegrationTests/Compliance/Retention/`)

For ALL 13 providers:

- `RetentionPolicyStore{Provider}IntegrationTests.cs` — CRUD against real DB
- `RetentionRecordStore{Provider}IntegrationTests.cs` — CRUD, GetExpired, GetExpiringWithin against real DB
- `LegalHoldStore{Provider}IntegrationTests.cs` — CRUD, IsUnderHold against real DB
- `RetentionAuditStore{Provider}IntegrationTests.cs` — record + query against real DB
- Each uses `[Collection("{Provider}")]` fixtures (existing collections)
- `InitializeAsync` creates schema + clears data
- Tests: Create → GetById, Create → GetByCategory, UpdateStatus, GetExpired, IsUnderHold

**Target**: ~130-160 integration tests (10 tests × 4 stores × ~3-4 providers active)

#### 9f. Load Tests (`tests/Encina.LoadTests/Compliance/Retention/`)

- `RetentionEnforcerLoadTests.cs` — concurrent enforcement under load (100 expired records, concurrent legal hold checks)
- `RetentionStoreLoadTests.md` — justification document (store is thin DB wrapper, load is on DB)

**Target**: 1 load test class + 1 justification

#### 9g. Benchmark Tests (`tests/Encina.BenchmarkTests/Compliance/Retention/`)

- `RetentionPipelineBenchmarks.cs` — pipeline behavior overhead per request (attribute caching, record creation)
- `RetentionEnforcerBenchmarks.md` — justification (enforcement is scheduled background, not hot path)

**Target**: 1 benchmark class + 1 justification

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 9</strong></summary>

```
You are implementing Phase 9 of Encina.Compliance.Retention (Issue #406).

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
- Verify all 13 store implementations follow identical behavior
- Use abstract base class with provider-specific derived classes

Property Tests:
- FsCheck generators for domain records
- Verify invariants: ExpiresAtUtc > CreatedAtUtc, mapper round-trip, DeletionResult sum

Integration Tests:
- [Collection("ADO-Sqlite")] etc. — reuse existing fixtures
- ClearAllDataAsync in InitializeAsync
- Create schema if not exists
- Test real SQL against real databases
- SQLite: NEVER dispose shared connection from fixture

Load Tests:
- RetentionEnforcer under concurrent access with legal holds
- Use Task.WhenAll with 100+ concurrent records

Benchmark Tests:
- BenchmarkSwitcher (NOT BenchmarkRunner)
- Materialize IQueryable results
- Results to artifacts/performance/

REFERENCE FILES:
- tests/Encina.UnitTests/Compliance/Anonymization/ (unit test patterns)
- tests/Encina.GuardTests/Compliance/Anonymization/ (guard test patterns)
- tests/Encina.IntegrationTests/ — look for Collection fixtures and existing patterns
- tests/Encina.PropertyTests/Compliance/ (FsCheck patterns)
```

</details>

---

### Phase 10: Documentation & Finalization

> **Goal**: Update all project documentation, verify build, and finalize.

<details>
<summary><strong>Tasks</strong></summary>

1. **XML documentation review** — Verify all public APIs have `<summary>`, `<remarks>`, `<param>`, `<returns>`, `<example>` where appropriate. GDPR article references in remarks.

2. **CHANGELOG.md** — Add entry under Unreleased:
   - `### Added`
   - `- Encina.Compliance.Retention — GDPR Art. 5(1)(e) data retention and automatic deletion with IRetentionPolicy, IRetentionEnforcer, ILegalHoldManager, RetentionValidationPipelineBehavior, [RetentionPeriod] attribute, RetentionEnforcementService, and 4 store interfaces (IRetentionPolicyStore, IRetentionRecordStore, ILegalHoldStore, IRetentionAuditStore) across all 13 database providers (Fixes #406)`

3. **INVENTORY.md** — Update issue #406 entry as IMPLEMENTADO

4. **`PublicAPI.Unshipped.txt`** — Final review, ensure all public types listed

5. **Build verification**:
   - `dotnet build Encina.slnx --configuration Release` — 0 errors, 0 warnings
   - `dotnet test` — all tests pass

6. **Coverage check** — Verify ≥85% line coverage for the new package

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 10</strong></summary>

```
You are implementing Phase 10 of Encina.Compliance.Retention (Issue #406).

CONTEXT:
- Phases 1-9 are fully implemented and tested
- Documentation and finalization remaining

TASK:
Update CHANGELOG.md, INVENTORY.md, verify build, and finalize.

KEY RULES:
- CHANGELOG.md: add under ### Added in Unreleased section
- INVENTORY.md: mark #406 as IMPLEMENTADO with comprehensive description
- Build must produce 0 errors and 0 warnings
- All tests must pass
- PublicAPI.Unshipped.txt must be complete and accurate
- Commit message: "feat: add Encina.Compliance.Retention - GDPR Art. 5(1)(e) data retention and automatic deletion with 13 database providers (Fixes #406)"
```

</details>

---

## Research

### GDPR Article References

| Article | Requirement | Relevance to Retention |
|---------|-------------|----------------------|
| Art. 5(1)(e) | Storage limitation — data kept no longer than necessary | **Primary article** — defines the need for retention policies |
| Recital 39 | Time limits for erasure or periodic review | Justifies automatic enforcement and alerts |
| Art. 17(1)(a) | Erasure when data no longer necessary | Grounds for automated deletion on policy expiration |
| Art. 17(3)(e) | Exemption for legal claims | Legal hold mechanism (litigation hold) |
| Art. 30(1)(f) | RoPA must include retention periods | Integration with ProcessingActivity registry |
| Art. 6 | Lawful basis for processing | Retention policies must reference legal basis |

### Industry-Specific Retention Periods

| Category | Period | Jurisdiction | Source |
|----------|--------|-------------|--------|
| Financial/Tax records | 6-10 years | Germany (AO §147) | Tax code |
| Healthcare records | 20 years | France (Code de la santé publique) | Health regulation |
| Employment records | 3 years | UK (Limitation Act 1980) | Employment law |
| Marketing consent | Until withdrawn | EU-wide (GDPR Art. 7) | GDPR |
| Session/access logs | 30-90 days | Industry practice | Security standards |
| Customer orders | 3-6 years | EU (consumer rights) | Warranty/returns |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in Retention |
|-----------|----------|-------------------|
| `IDataErasureExecutor` | `Encina.Compliance.DataSubjectRights` | Delegate actual data deletion |
| `ErasureScope` | `Encina.Compliance.DataSubjectRights` | Define scope for retention-triggered deletion |
| `ErasureResult` | `Encina.Compliance.DataSubjectRights` | Capture deletion results |
| `[ProcessesPersonalData]` | `Encina.Compliance.GDPR` | Pipeline behavior attribute detection |
| `IProcessingActivityRegistry` | `Encina.Compliance.GDPR` | Link retention policies to processing activities |
| `EncinaErrors.Create()` | `Encina` core | Error factory pattern |
| `IPipelineBehavior<,>` | `Encina` core | Pipeline behavior registration |
| `INotification` / `INotificationPublisher` | `Encina` core | Expiration alerts and deletion notifications |
| `TimeProvider` | .NET 10 BCL | Testable time-dependent logic |
| Satellite provider structure | All 13 providers | Subfolder + DI registration pattern |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| `Encina.Compliance.GDPR` | 8100-8199 | Core, LawfulBasis, ProcessingActivity |
| `Encina.Compliance.Consent` | 8200-8299 | Consent lifecycle, audit, events |
| `Encina.Compliance.DataSubjectRights` | 8300-8399 | DSR lifecycle, restriction, erasure, export |
| `Encina.Compliance.Anonymization` | 8400-8499 | Pipeline, techniques, tokenization, risk assessment |
| **`Encina.Compliance.Retention`** | **8500-8599** | **Pipeline, records, enforcement, holds, alerts, audit** |

### File Count Estimate

| Category | Files | Notes |
|----------|-------|-------|
| Core package (Phases 1-5, 8) | ~35-40 | Models, interfaces, impls, diagnostics, DI, scheduler |
| Persistence (Phase 6) | ~8 | 4 entities + 4 mappers |
| ADO.NET ×4 (Phase 7a) | ~20 | 5 files × 4 providers (4 stores + DI ext) |
| Dapper ×4 (Phase 7b) | ~20 | 5 files × 4 providers |
| EF Core (Phase 7c) | ~12 | 4 stores + 4 configs + DbContext ext + DI |
| MongoDB (Phase 7d) | ~6 | 4 stores + docs + DI |
| Tests (Phase 9) | ~50-60 | Across 7 test types |
| Documentation (Phase 10) | ~3 | INVENTORY, CHANGELOG, PublicAPI |
| **Total** | **~130-170** | |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
You are implementing Encina.Compliance.Retention for Issue #406 — GDPR Art. 5(1)(e) Data Retention and Automatic Deletion.

PROJECT CONTEXT:
- Encina is a .NET 10 / C# 14 library for CQRS + Event Sourcing
- Pre-1.0: no backward compatibility needed, best solution always
- Railway Oriented Programming: Either<EncinaError, T> everywhere
- 13 database providers: ADO.NET (Sqlite, SqlServer, PostgreSQL, MySQL), Dapper (same 4), EF Core (same 4), MongoDB
- Satellite provider pattern: feature subfolder in each provider package
- TryAdd DI pattern: satellite providers register before core package

IMPLEMENTATION OVERVIEW:
New package: src/Encina.Compliance.Retention/
References: Encina.Compliance.GDPR (shared types), Encina.Compliance.DataSubjectRights (IDataErasureExecutor)

Phase 1: Core models, enums (RetentionPolicy, RetentionRecord, LegalHold, DeletionResult, ExpiringData, RetentionAuditEntry, notifications)
Phase 2: Interfaces (IRetentionPolicy, IRetentionEnforcer, ILegalHoldManager, IRetentionPolicyStore, IRetentionRecordStore, ILegalHoldStore, IRetentionAuditStore) + [RetentionPeriod] attribute + RetentionErrors
Phase 3: Default implementations (InMemory stores, DefaultRetentionPolicy, DefaultRetentionEnforcer, DefaultLegalHoldManager)
Phase 4: RetentionValidationPipelineBehavior (track new data) + RetentionEnforcementService (scheduled deletion)
Phase 5: Options, DI registration, auto-registration, health check, enforcement mode
Phase 6: Persistence entities, mappers
Phase 7: 13 provider implementations (ADO ×4, Dapper ×4, EF Core, MongoDB) with 4 stores each + satellite DI
Phase 8: Observability (ActivitySource, Meter, [LoggerMessage] event IDs 8500-8599)
Phase 9: Testing (7 types: Unit ~120, Guard ~60, Contract ~25, Property ~20, Integration ~150, Load, Benchmark)
Phase 10: Documentation (CHANGELOG.md, INVENTORY.md, PublicAPI.Unshipped.txt)

KEY PATTERNS:
- All stores: ValueTask<Either<EncinaError, T>>
- Store naming: RetentionPolicyStoreADO, RetentionRecordStoreDapper, LegalHoldStoreEF, RetentionAuditStoreMongoDB
- SQLite: TEXT dates (ISO 8601 "O"), never datetime('now'), NEVER dispose shared connection in tests
- Satellite DI: AddEncinaRetention{Provider}(services, connectionString) → called BEFORE AddEncinaRetention()
- Pipeline behavior: static per-generic-type attribute caching, 3 enforcement modes (Block/Warn/Disabled)
- Health check: Unhealthy/Degraded/Healthy, scoped resolution, const DefaultName
- Integration tests: [Collection("Provider-DB")] shared fixtures, ClearAllDataAsync
- Observability: ActivitySource + Meter + [LoggerMessage] source generator
- All public APIs: XML documentation with GDPR Art. 5(1)(e) references
- RetentionEnforcementService: IHostedService with PeriodicTimer, configurable interval
- IDataErasureExecutor: optional dependency, resolved via IServiceProvider.GetService<T>()
- Legal holds: suspend deletion, cascading status update on apply/release

REFERENCE FILES:
- Anonymization package: src/Encina.Compliance.Anonymization/ (closest architectural reference)
- DSR package: src/Encina.Compliance.DataSubjectRights/ (erasure executor integration)
- GDPR package: src/Encina.Compliance.GDPR/ (shared types)
- Provider patterns: src/Encina.ADO.Sqlite/Consent/, src/Encina.Dapper.Sqlite/Consent/, src/Encina.EntityFrameworkCore/Consent/, src/Encina.MongoDB/Consent/
```

</details>

---

## Next Steps

1. **Review and approve this plan**
2. Publish as comment on Issue #406
3. Begin Phase 1 implementation in a new session
4. Each phase should be a self-contained commit
5. Final commit references `Fixes #406`
