---
title: "Data Retention in Encina"
layout: default
parent: "Features"
---

# Data Retention in Encina

This guide explains how to manage GDPR Article 5(1)(e) storage limitation -- declarative data retention management at the CQRS pipeline level using the `Encina.Compliance.Retention` package. Retention enforcement operates independently of the transport layer, ensuring consistent storage limitation compliance across all entry points.

## Table of Contents

1. [Overview](#overview)
2. [The Problem](#the-problem)
3. [The Solution](#the-solution)
4. [Quick Start](#quick-start)
5. [RetentionPeriod Attribute](#retentionperiod-attribute)
6. [Retention Record Lifecycle](#retention-record-lifecycle)
7. [Retention Policies](#retention-policies)
8. [Legal Holds](#legal-holds)
9. [Enforcement Service](#enforcement-service)
10. [Audit Trail](#audit-trail)
11. [Domain Notifications](#domain-notifications)
12. [Configuration Options](#configuration-options)
13. [Enforcement Modes](#enforcement-modes)
14. [Database Providers](#database-providers)
15. [Observability](#observability)
16. [Health Check](#health-check)
17. [Error Handling](#error-handling)
18. [Integration with DataSubjectRights](#integration-with-datasubjectrights)
19. [Best Practices](#best-practices)
20. [Testing](#testing)
21. [FAQ](#faq)

---

## Overview

Encina.Compliance.Retention provides attribute-based retention tracking and automated enforcement at the CQRS pipeline level:

| Component | Description |
|-----------|-------------|
| **`[RetentionPeriod]` Attribute** | Declarative retention requirements on response classes and properties |
| **`RetentionValidationPipelineBehavior`** | Pipeline behavior that automatically creates retention records when decorated responses are returned |
| **`IRetentionPolicy`** | Policy resolution with fallback to default retention period |
| **`IRetentionEnforcer`** | Enforcement orchestration -- finds expired records, checks legal holds, executes deletion |
| **`ILegalHoldManager`** | Full legal hold lifecycle with notifications and audit trail |
| **`IRetentionRecordStore`** | Retention record persistence (lifecycle tracking per data entity) |
| **`IRetentionPolicyStore`** | Policy persistence (one retention policy per data category) |
| **`IRetentionAuditStore`** | Immutable audit trail for all retention operations |
| **`ILegalHoldStore`** | Legal hold persistence (litigation hold management) |
| **`RetentionEnforcementService`** | `BackgroundService` with `PeriodicTimer` for automated enforcement cycles |
| **`RetentionOptions`** | Configuration for enforcement mode, intervals, alerts, and auto-registration |

### Why Pipeline-Level Retention?

| Benefit | Description |
|---------|-------------|
| **Automatic tracking** | Retention records are created whenever a `[RetentionPeriod]`-decorated response is returned from a handler |
| **Declarative** | Retention requirements live with the response types, not scattered across services |
| **Transport-agnostic** | Same retention tracking for HTTP, message queue, gRPC, and serverless |
| **Automated enforcement** | Background service periodically identifies and deletes expired data |
| **Legal hold support** | Litigation holds suspend deletion per Article 17(3)(e) |
| **Auditable** | Every retention action is recorded with timestamps, actors, and compliance metadata |

---

## The Problem

GDPR Article 5(1)(e) requires that personal data be kept for no longer than is necessary for the purposes for which it is processed. Organizations face several challenges with storage limitation compliance:

- **No systematic tracking** of when data was created and when it should expire
- **No automated enforcement** of deletion policies, leading to data accumulating indefinitely
- **Legal hold management** is complex when litigation requires suspending deletion for specific entities
- **No audit trail** to demonstrate compliance with the accountability principle (Article 5(2))
- **Inconsistent retention periods** when policies are implemented ad-hoc across teams
- **Manual deletion processes** that are error-prone and difficult to verify during regulatory audits
- **No advance warning** before data reaches its retention deadline, preventing proactive review

---

## The Solution

Encina solves this with a unified retention pipeline covering the full data lifecycle:

```text
Request → Handler → Response → [RetentionValidationPipelineBehavior]
                                       |
                                       +-- No [RetentionPeriod] attributes? → Skip (zero overhead)
                                       +-- Disabled mode? → Skip
                                       +-- Extract entity ID from response (Id or EntityId property)
                                       +-- Resolve retention period from attribute
                                       +-- Create RetentionRecord in IRetentionRecordStore
                                       |   +-- Success → Return response
                                       |   +-- Failure + Block mode → Return error
                                       |   +-- Failure + Warn mode → Log warning, return response

[RetentionEnforcementService] (periodic background service)
       |
       +-- Check for expiring data → Publish DataExpiringNotification
       +-- Get expired records from IRetentionRecordStore
       +-- For each expired record:
       |   +-- Check ILegalHoldStore.IsUnderHoldAsync
       |   |   +-- Under hold → Update to UnderLegalHold, skip deletion
       |   +-- Delegate deletion to IDataErasureExecutor (from DSR module)
       |   +-- Update record status to Deleted
       |   +-- Record audit entry
       |   +-- Publish DataDeletedNotification
       +-- Publish RetentionEnforcementCompletedNotification
```

---

## Quick Start

### 1. Install the Package

```bash
dotnet add package Encina.Compliance.Retention
```

### 2. Decorate Response Types with Retention Periods

```csharp
// Class-level: all instances of this response are tracked with a 7-year retention
[RetentionPeriod(Years = 7, DataCategory = "financial-records",
    Reason = "German tax law (AO section 147)")]
public sealed record CreateInvoiceResponse
{
    public string Id { get; init; } = string.Empty;
    public decimal Amount { get; init; }
}

// Property-level: specific fields with different retention periods
public sealed record CreateCustomerResponse
{
    public string Id { get; init; } = string.Empty;

    [RetentionPeriod(Days = 365, DataCategory = "marketing-consent",
        Reason = "Consent validity period", AutoDelete = true)]
    public string? MarketingPreferences { get; init; }
}
```

### 3. Register Services

```csharp
services.AddEncina(config =>
    config.RegisterServicesFromAssemblyContaining<Program>());

services.AddEncinaRetention(options =>
{
    options.EnforcementMode = RetentionEnforcementMode.Warn;
    options.DefaultRetentionPeriod = TimeSpan.FromDays(365);
    options.AlertBeforeExpirationDays = 30;
    options.AddHealthCheck = true;
    options.AssembliesToScan.Add(typeof(Program).Assembly);
});
```

### 4. Configure Policies via Fluent API

```csharp
services.AddEncinaRetention(options =>
{
    options.AddPolicy("user-profiles", policy =>
    {
        policy.RetainForDays(365);
        policy.WithAutoDelete();
        policy.WithReason("GDPR Article 5(1)(e) - storage limitation");
    });

    options.AddPolicy("audit-logs", policy =>
    {
        policy.RetainForYears(7);
        policy.WithAutoDelete(false);
        policy.WithLegalBasis("Legal retention requirement");
    });

    options.AddPolicy("session-logs", policy =>
    {
        policy.RetainFor(TimeSpan.FromDays(90));
        policy.WithAutoDelete();
        policy.WithReason("No business need beyond 90 days");
    });
});
```

### 5. Manage Legal Holds

```csharp
var holdManager = serviceProvider.GetRequiredService<ILegalHoldManager>();

// Apply a legal hold to prevent deletion during litigation
var hold = LegalHold.Create(
    entityId: "invoice-12345",
    reason: "Pending tax audit for fiscal year 2024",
    appliedByUserId: "legal-counsel@company.com");

await holdManager.ApplyHoldAsync("invoice-12345", hold, cancellationToken);

// Release the hold when litigation concludes
await holdManager.ReleaseHoldAsync(hold.Id, "legal-counsel@company.com", cancellationToken);
```

---

## RetentionPeriod Attribute

The `[RetentionPeriod]` attribute marks response types or properties as subject to retention tracking:

```csharp
// Apply to a class -- all instances retain for 7 years
[RetentionPeriod(Years = 7, DataCategory = "financial-records",
    Reason = "German tax law (AO section 147)")]
public sealed record Invoice(string Id, decimal Amount, DateTimeOffset CreatedAtUtc);

// Apply to a property -- specific field retention
public sealed record CustomerProfile
{
    public string Id { get; init; } = string.Empty;

    [RetentionPeriod(Days = 365, DataCategory = "marketing-consent",
        Reason = "Consent validity period", AutoDelete = true)]
    public string? MarketingPreferences { get; init; }
}

// Minimal usage with days only
[RetentionPeriod(Days = 90)]
public sealed record SessionLog(string SessionId, DateTimeOffset StartedAtUtc);
```

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Days` | `int` | `0` | Retention period in days (mutually exclusive with `Years`) |
| `Years` | `int` | `0` | Retention period in years, approximated as 365 days per year (mutually exclusive with `Days`) |
| `DataCategory` | `string?` | `null` | Maps to a `RetentionPolicy` category for policy resolution; defaults to type name if not set |
| `Reason` | `string?` | `null` | Documents the legal or business justification for the retention period (Article 5(2) accountability) |
| `AutoDelete` | `bool` | `true` | Whether data should be automatically deleted when the retention period expires |

The computed `RetentionPeriod` property resolves the appropriate `TimeSpan` from `Days` or `Years`. If both are set, `Days` takes precedence.

---

## Retention Record Lifecycle

Each retention record progresses through a defined lifecycle tracked by `RetentionStatus`:

```text
Active → Expired → Deleted
  |         ↑
  +→ UnderLegalHold → Active (if within period after hold release)
                    → Expired (if past expiration after hold release)
```

| Status | Description |
|--------|-------------|
| `Active` | Data is within its retention period and must not be deleted |
| `Expired` | Retention period has elapsed; data is eligible for deletion |
| `Deleted` | Data has been successfully deleted by the enforcement process |
| `UnderLegalHold` | A legal hold suspends deletion regardless of expiration (Article 17(3)(e)) |

### Creating Retention Records

Retention records are typically created automatically by the `RetentionValidationPipelineBehavior`. They can also be created manually:

```csharp
var store = serviceProvider.GetRequiredService<IRetentionRecordStore>();

var record = RetentionRecord.Create(
    entityId: "order-12345",
    dataCategory: "financial-records",
    createdAtUtc: DateTimeOffset.UtcNow,
    expiresAtUtc: DateTimeOffset.UtcNow.AddYears(7),
    policyId: "policy-001");

await store.CreateAsync(record);
```

### Querying Records

```csharp
// Get all records for an entity
var records = await store.GetByEntityIdAsync("order-12345");

// Get all expired records eligible for deletion
var expired = await store.GetExpiredRecordsAsync();

// Get records expiring within the next 30 days
var expiring = await store.GetExpiringWithinAsync(TimeSpan.FromDays(30));
```

---

## Retention Policies

A `RetentionPolicy` defines how long data of a given category should be retained:

```csharp
var policyStore = serviceProvider.GetRequiredService<IRetentionPolicyStore>();

var policy = RetentionPolicy.Create(
    dataCategory: "financial-records",
    retentionPeriod: RetentionPolicy.FromYears(7),
    autoDelete: true,
    reason: "German tax law (AO section 147)",
    legalBasis: "Legal obligation (Art. 6(1)(c))",
    policyType: RetentionPolicyType.TimeBased);

await policyStore.CreateAsync(policy);
```

### Policy Types

| Type | Description |
|------|-------------|
| `TimeBased` | Retention period measured from data creation timestamp (most common) |
| `EventBased` | Retention starts from a specific business event (contract termination, employee departure) |
| `ConsentBased` | Data retained until consent is withdrawn (Article 7(3)) |

### Policy Resolution

The `IRetentionPolicy` service resolves retention periods with a fallback chain:

1. Look up explicit `RetentionPolicy` via `IRetentionPolicyStore.GetByCategoryAsync`
2. If no explicit policy exists, fall back to `RetentionOptions.DefaultRetentionPeriod`
3. If no default is configured, return `NoPolicyForCategory` error

```csharp
var retentionPolicy = serviceProvider.GetRequiredService<IRetentionPolicy>();

// Resolve retention period for a data category
var period = await retentionPolicy.GetRetentionPeriodAsync("financial-records");

// Check if a specific entity has exceeded its retention period
var expired = await retentionPolicy.IsExpiredAsync("order-12345", "financial-records");
```

---

## Legal Holds

Legal holds (litigation holds) suspend data deletion for specific entities per GDPR Article 17(3)(e):

```csharp
var holdManager = serviceProvider.GetRequiredService<ILegalHoldManager>();

// Apply a hold
var hold = LegalHold.Create(
    entityId: "invoice-12345",
    reason: "Pending tax audit for fiscal year 2024",
    appliedByUserId: "legal-counsel@company.com");

var applyResult = await holdManager.ApplyHoldAsync("invoice-12345", hold);

// Check hold status
var isHeld = await holdManager.IsUnderHoldAsync("invoice-12345");

// Get all active holds across the system
var activeHolds = await holdManager.GetActiveHoldsAsync();

// Release when litigation concludes
var releaseResult = await holdManager.ReleaseHoldAsync(hold.Id, "legal-counsel@company.com");
```

### How Legal Holds Work

When a hold is **applied**:

1. The `LegalHold` record is persisted via `ILegalHoldStore`
2. Matching `RetentionRecord` entries are updated to `UnderLegalHold` status
3. An audit entry is recorded via `IRetentionAuditStore`
4. A `LegalHoldAppliedNotification` is published

When a hold is **released**:

1. The `LegalHold` record is updated with release metadata
2. Matching records revert to `Expired` (if past expiration) or `Active` (if still within period)
3. An audit entry is recorded
4. A `LegalHoldReleasedNotification` is published

Multiple holds can exist for the same entity. The status remains `UnderLegalHold` until all holds are released.

---

## Enforcement Service

The `RetentionEnforcementService` is a `BackgroundService` that runs periodic enforcement cycles using `PeriodicTimer`:

```text
Service Start → First cycle immediately → Wait for timer tick → Next cycle → ...
```

Each enforcement cycle:

1. Checks for data expiring within the `AlertBeforeExpirationDays` window and publishes `DataExpiringNotification`
2. Calls `IRetentionEnforcer.EnforceRetentionAsync` to process expired records
3. The enforcer queries expired records, checks legal holds, delegates deletion, and updates statuses

### Manual Enforcement

If `EnableAutomaticEnforcement` is `false`, trigger enforcement manually:

```csharp
var enforcer = serviceProvider.GetRequiredService<IRetentionEnforcer>();

var result = await enforcer.EnforceRetentionAsync(cancellationToken);

result.Match(
    Right: deletion => Console.WriteLine(
        $"Enforcement complete: {deletion.RecordsDeleted} deleted, " +
        $"{deletion.RecordsUnderHold} held, {deletion.RecordsFailed} failed"),
    Left: error => Console.WriteLine($"Enforcement failed: {error.Message}"));
```

### Querying Expiring Data

```csharp
var expiring = await enforcer.GetExpiringDataAsync(TimeSpan.FromDays(30));

expiring.Match(
    Right: data =>
    {
        foreach (var item in data)
        {
            Console.WriteLine($"{item.EntityId} expires in {item.DaysUntilExpiration} days");
        }
    },
    Left: error => Console.WriteLine($"Query failed: {error.Message}"));
```

---

## Audit Trail

Every retention action is recorded in an immutable audit trail for compliance evidence:

```csharp
var auditStore = serviceProvider.GetRequiredService<IRetentionAuditStore>();

// Record an audit entry
var entry = RetentionAuditEntry.Create(
    action: "DataDeleted",
    entityId: "order-12345",
    dataCategory: "financial-records",
    detail: "Retention period expired (7 years), auto-deleted by enforcement service",
    performedByUserId: "system");

await auditStore.RecordAsync(entry);

// Retrieve the audit trail for an entity
var trail = await auditStore.GetByEntityIdAsync("order-12345");
```

Typical audit actions: `PolicyCreated`, `RecordTracked`, `EnforcementExecuted`, `RecordDeleted`, `LegalHoldApplied`, `LegalHoldReleased`, `ExpirationAlertSent`.

---

## Domain Notifications

The retention module publishes domain notifications via `INotification` at key lifecycle points:

| Notification | Trigger | Properties |
|-------------|---------|------------|
| `DataExpiringNotification` | Data approaching expiration (within alert window) | EntityId, DataCategory, ExpiresAtUtc, DaysUntilExpiration, OccurredAtUtc |
| `DataDeletedNotification` | Data deleted by enforcement (Art. 5(1)(e)) | EntityId, DataCategory, DeletedAtUtc, PolicyId |
| `LegalHoldAppliedNotification` | Legal hold applied (Art. 17(3)(e)) | HoldId, EntityId, Reason, AppliedAtUtc |
| `LegalHoldReleasedNotification` | Legal hold released | HoldId, EntityId, ReleasedAtUtc |
| `RetentionEnforcementCompletedNotification` | Enforcement cycle completed | Result (DeletionResult), OccurredAtUtc |

Subscribe to notifications using standard Encina notification handlers:

```csharp
public sealed class ExpiringDataNotificationHandler
    : INotificationHandler<DataExpiringNotification>
{
    public Task Handle(DataExpiringNotification notification, CancellationToken cancellationToken)
    {
        // Send alert to data controller, update compliance dashboard, etc.
        return Task.CompletedTask;
    }
}
```

Notifications can be disabled via `options.PublishNotifications = false`.

---

## Configuration Options

```csharp
services.AddEncinaRetention(options =>
{
    // Default retention period when no category-specific policy exists
    options.DefaultRetentionPeriod = TimeSpan.FromDays(365);

    // Days before expiration to publish DataExpiringNotification
    options.AlertBeforeExpirationDays = 30;

    // Publish domain notifications at lifecycle events
    options.PublishNotifications = true;

    // Record audit trail entries (Article 5(2) accountability)
    options.TrackAuditTrail = true;

    // Enable background enforcement service
    options.EnableAutomaticEnforcement = true;

    // How often enforcement runs
    options.EnforcementInterval = TimeSpan.FromMinutes(60);

    // Pipeline behavior enforcement mode
    options.EnforcementMode = RetentionEnforcementMode.Warn;

    // Auto-scan assemblies for [RetentionPeriod] attributes at startup
    options.AutoRegisterFromAttributes = true;
    options.AssembliesToScan.Add(typeof(Program).Assembly);

    // Register health check
    options.AddHealthCheck = true;

    // Configure policies via fluent API
    options.AddPolicy("user-profiles", p =>
        p.RetainForDays(365).WithAutoDelete().WithReason("Storage limitation"));
});
```

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `DefaultRetentionPeriod` | `TimeSpan?` | `null` | Fallback retention period when no category-specific policy exists |
| `AlertBeforeExpirationDays` | `int` | `30` | Days before expiration to publish `DataExpiringNotification` |
| `PublishNotifications` | `bool` | `true` | Publish domain notifications at lifecycle events |
| `TrackAuditTrail` | `bool` | `true` | Record audit trail entries for accountability (Article 5(2)) |
| `AddHealthCheck` | `bool` | `false` | Register health check with `IHealthChecksBuilder` |
| `EnableAutomaticEnforcement` | `bool` | `true` | Enable background enforcement service |
| `EnforcementInterval` | `TimeSpan` | `60 min` | How often enforcement runs |
| `EnforcementMode` | `RetentionEnforcementMode` | `Warn` | Pipeline behavior enforcement mode (Block / Warn / Disabled) |
| `AutoRegisterFromAttributes` | `bool` | `true` | Scan assemblies for `[RetentionPeriod]` attributes at startup |
| `AssembliesToScan` | `List<Assembly>` | `[]` | Assemblies to scan for attribute-based policy discovery |

---

## Enforcement Modes

| Mode | Pipeline Behavior | Use Case |
|------|-------------------|----------|
| `Block` | Returns error if retention record creation fails | Production (GDPR Article 5(1)(e) compliant) |
| `Warn` | Logs warning, allows response through | Migration/testing phase |
| `Disabled` | Skips all retention tracking entirely (no-op) | Development environments |

---

## Database Providers

The in-memory stores (`InMemoryRetentionRecordStore`, `InMemoryRetentionPolicyStore`, `InMemoryLegalHoldStore`, `InMemoryRetentionAuditStore`) are suitable for development and testing. For production, use a database-backed provider:

| Provider Category | Providers | Registration |
|-------------------|-----------|-------------|
| ADO.NET | SQL Server, PostgreSQL, MySQL | `config.UseRetention = true` in `AddEncinaADO()` |
| Dapper | SQL Server, PostgreSQL, MySQL | `config.UseRetention = true` in `AddEncinaDapper()` |
| EF Core | SQL Server, PostgreSQL, MySQL | `config.UseRetention = true` in `AddEncinaEntityFrameworkCore()` |
| MongoDB | MongoDB | `config.UseRetention = true` in `AddEncinaMongoDB()` |

Each provider registers `IRetentionRecordStore`, `IRetentionPolicyStore`, `ILegalHoldStore`, and `IRetentionAuditStore` backed by the corresponding database.

All 13 database provider implementations are planned. The in-memory stores are the default fallback when no database provider is registered.

---

## Observability

### OpenTelemetry Tracing

The module creates activities with the `Encina.Compliance.Retention` ActivitySource:

| Activity | Tags |
|----------|------|
| `Retention.Pipeline` | `retention.request_type`, `retention.response_type`, `retention.outcome` |
| `Retention.Enforcement` | `retention.outcome`, `retention.records_processed` |
| `Retention.Deletion` | `retention.entity_id`, `retention.data_category`, `retention.outcome` |
| `Retention.LegalHold` | `retention.hold_id`, `retention.entity_id`, `retention.outcome` |
| `Retention.PolicyResolution` | `retention.data_category`, `retention.outcome` |
| `Retention.Audit` | `retention.action`, `retention.outcome` |

### Metrics

| Metric | Type | Description |
|--------|------|-------------|
| `retention.pipeline.executions.total` | Counter | Total pipeline executions (tagged by `retention.outcome`) |
| `retention.enforcement.cycles.total` | Counter | Total enforcement cycles (tagged by `retention.outcome`) |
| `retention.records.created.total` | Counter | Total retention records created (tagged by `retention.data_category`) |
| `retention.records.deleted.total` | Counter | Total records deleted during enforcement (tagged by `retention.outcome`) |
| `retention.records.held.total` | Counter | Total records skipped due to legal hold |
| `retention.records.failed.total` | Counter | Total deletion failures (tagged by `retention.failure_reason`) |
| `retention.legal_holds.applied.total` | Counter | Total legal holds applied |
| `retention.legal_holds.released.total` | Counter | Total legal holds released |
| `retention.policies.resolved.total` | Counter | Total policy resolutions (tagged by `retention.outcome`) |
| `retention.audit.entries.total` | Counter | Total audit entries recorded (tagged by `retention.action`) |
| `retention.enforcement.duration` | Histogram | Duration of enforcement cycle (ms) |
| `retention.pipeline.duration` | Histogram | Duration of pipeline execution (ms) |
| `retention.deletion.duration` | Histogram | Duration of individual record deletion (ms) |

### Structured Logging

Log events using `[LoggerMessage]` source generator for zero-allocation structured logging. Event IDs are allocated in the 8500-8569 range:

| EventId Range | Category | Key Events |
|---------------|----------|------------|
| 8500-8509 | Pipeline behavior | Pipeline skipped, started, completed, entity ID not found, record creation blocked/warned, pipeline error |
| 8510-8519 | Enforcement service | Service started/disabled, cycle completed/failed/cancelled, expiring data found, erasure executor missing |
| 8520-8529 | Auto-registration | Registration completed/skipped, policy discovered, policy already exists, registration failed |
| 8530-8539 | Health check | Health check completed |
| 8540-8549 | Legal hold management | Hold applied/released/already active/not found, hold status cascaded, deletion skipped |
| 8550-8559 | Retention policy | Period resolved (from policy/default), no policy for category, expiration checked, record status recalculated, erasure failed |
| 8560-8569 | Audit trail | Entry recorded/failed, notification failed, enforcement cancelled, expiring data found/check failed |

---

## Health Check

Opt-in via `options.AddHealthCheck = true`:

```csharp
services.AddEncinaRetention(options =>
{
    options.AddHealthCheck = true;
});
```

The health check (`encina-retention`) verifies:

- `RetentionOptions` are configured
- `IRetentionRecordStore` is resolvable from DI
- `IRetentionPolicyStore` is resolvable from DI
- `IRetentionEnforcer` is resolvable from DI
- `ILegalHoldStore` is resolvable (Degraded if missing)
- `IRetentionAuditStore` is resolvable when `TrackAuditTrail` is enabled (Degraded if missing)

Tags: `encina`, `gdpr`, `retention`, `compliance`, `ready`.

Health check data includes `enforcementMode`, `enableAutomaticEnforcement`, `enforcementInterval`, and the concrete type names of all resolved stores.

---

## Error Handling

All operations return `Either<EncinaError, T>`:

```csharp
var enforcer = serviceProvider.GetRequiredService<IRetentionEnforcer>();

var result = await enforcer.EnforceRetentionAsync(cancellationToken);

result.Match(
    Right: deletion =>
    {
        logger.LogInformation("Deleted {Count} records", deletion.RecordsDeleted);
    },
    Left: error =>
    {
        logger.LogError("Enforcement failed: {Code} - {Message}", error.Code, error.Message);
    }
);
```

### Error Codes

| Code | Description |
|------|-------------|
| `retention.policy_not_found` | Retention policy ID does not exist |
| `retention.policy_already_exists` | A policy already exists for the given data category |
| `retention.record_not_found` | Retention record ID does not exist |
| `retention.record_already_exists` | Retention record ID already exists (duplicate) |
| `retention.hold_not_found` | Legal hold ID does not exist |
| `retention.hold_already_active` | An active legal hold already exists for the entity |
| `retention.hold_already_released` | The legal hold has already been released |
| `retention.enforcement_failed` | Retention enforcement cycle failed |
| `retention.deletion_failed` | Data deletion failed during enforcement |
| `retention.store_error` | Store persistence operation failed |
| `retention.invalid_parameter` | Invalid parameter provided to a retention operation |
| `retention.no_policy_for_category` | No retention policy defined for the data category |
| `retention.pipeline_record_creation_failed` | Pipeline behavior failed to create a retention record |
| `retention.pipeline_entity_id_not_found` | Could not resolve entity ID from response type |

---

## Integration with DataSubjectRights

The `DefaultRetentionEnforcer` uses `IDataErasureExecutor` from `Encina.Compliance.DataSubjectRights` to perform physical data deletion during enforcement cycles. This enables unified deletion through the same erasure infrastructure used by DSR erasure requests (Article 17).

If `IDataErasureExecutor` is not registered, the enforcer operates in degraded mode: retention records are marked as `Deleted` but no physical erasure occurs. A warning is logged at EventId 8519.

To enable full integration:

```csharp
// Register DSR for erasure infrastructure
services.AddEncinaDataSubjectRights();

// Register Retention for lifecycle management
services.AddEncinaRetention(options =>
{
    options.EnableAutomaticEnforcement = true;
});
```

---

## Best Practices

1. **Set `DataCategory` on `[RetentionPeriod]`** -- explicit categories enable proper policy resolution and reporting; without them, the type name is used as a fallback
2. **Use legal holds proactively before litigation** -- apply holds as soon as litigation is anticipated, not just after it begins, to ensure Article 17(3)(e) compliance
3. **Configure `AlertBeforeExpirationDays`** -- advance warning enables data controllers to review upcoming deletions and intervene if necessary
4. **Track the audit trail** -- keep `TrackAuditTrail = true` in production for accountability evidence during regulatory audits (Article 5(2))
5. **Start with `Warn` mode, switch to `Block` when ready** -- `Warn` mode lets you observe retention tracking without breaking existing workflows; switch to `Block` once all policies are defined
6. **Define explicit policies for every data category** -- avoid relying on `DefaultRetentionPeriod` in production; per Article 5(1)(e), controllers should establish explicit retention periods
7. **Register `IDataErasureExecutor` for physical deletion** -- without DSR integration, the enforcer only marks records as deleted without physical erasure
8. **Monitor the health check** -- enable `AddHealthCheck = true` and configure alerts for degraded status to catch missing stores early
9. **Use `TimeProvider` for testable time-based logic** -- the pipeline behavior and enforcement service accept `TimeProvider` for deterministic testing

---

## Testing

### Unit Tests with In-Memory Stores

```csharp
var recordStore = new InMemoryRetentionRecordStore(
    TimeProvider.System,
    NullLogger<InMemoryRetentionRecordStore>.Instance);

// Create a retention record
var record = RetentionRecord.Create(
    entityId: "order-12345",
    dataCategory: "financial-records",
    createdAtUtc: DateTimeOffset.UtcNow,
    expiresAtUtc: DateTimeOffset.UtcNow.AddYears(7));

await recordStore.CreateAsync(record);

// Verify it exists
var result = await recordStore.GetByIdAsync(record.Id);
Assert.True(result.IsRight);
```

### Full Pipeline Test

```csharp
var services = new ServiceCollection();
services.AddEncina(c => c.RegisterServicesFromAssemblyContaining<CreateInvoiceCommand>());
services.AddEncinaRetention(o =>
    o.EnforcementMode = RetentionEnforcementMode.Block);

var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();

var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
var result = await encina.Send(new CreateInvoiceCommand("INV-001", 1500.00m));

// Verify retention record was created
var recordStore = scope.ServiceProvider.GetRequiredService<IRetentionRecordStore>();
var records = await recordStore.GetByEntityIdAsync("INV-001");
Assert.True(records.IsRight);
```

### Legal Hold Test

```csharp
var holdManager = serviceProvider.GetRequiredService<ILegalHoldManager>();

// Apply a hold
var hold = LegalHold.Create("entity-001", "Litigation pending", "admin");
var applyResult = await holdManager.ApplyHoldAsync("entity-001", hold);
Assert.True(applyResult.IsRight);

// Verify entity is under hold
var isHeld = await holdManager.IsUnderHoldAsync("entity-001");
isHeld.Match(
    Right: held => Assert.True(held),
    Left: _ => Assert.Fail("Should not fail"));

// Release and verify
var releaseResult = await holdManager.ReleaseHoldAsync(hold.Id, "admin");
Assert.True(releaseResult.IsRight);
```

---

## FAQ

**Q: How does the pipeline behavior decide which responses to track?**
The `RetentionValidationPipelineBehavior` checks for `[RetentionPeriod]` attributes on the response type (class level) or its properties. Attribute presence is cached statically per closed generic type, so there is zero reflection overhead after the first resolution.

**Q: How does the pipeline resolve the entity ID from the response?**
The behavior looks for a public readable property named `EntityId` (preferred) or `Id` (fallback) on the response type, using case-insensitive matching. If neither property exists, the behavior returns `PipelineEntityIdNotFound` error in Block mode or logs a warning in Warn mode.

**Q: What happens if no `[RetentionPeriod]` attribute is present on the response?**
The pipeline behavior skips all retention tracking with zero overhead. Attribute presence is resolved once per closed generic type via a `static readonly` field.

**Q: What is the difference between InMemory and database stores?**
The in-memory stores (`InMemoryRetentionRecordStore`, `InMemoryRetentionPolicyStore`, `InMemoryLegalHoldStore`, `InMemoryRetentionAuditStore`) use `ConcurrentDictionary` for storage. They are suitable for development and testing only. For production, register a database-backed provider that provides durable persistence and survives application restarts.

**Q: Can I register custom implementations before calling `AddEncinaRetention`?**
Yes. All service registrations use `TryAdd`, so existing registrations are preserved. Register your custom `IRetentionRecordStore`, `IRetentionEnforcer`, or `ILegalHoldManager` before calling `AddEncinaRetention()`.

**Q: What happens if the enforcement service encounters an error during a cycle?**
Individual enforcement cycle failures are logged but never crash the host. The service continues running and attempts enforcement again on the next cycle. This ensures that transient errors do not disrupt the entire retention system.

**Q: How does the enforcer handle records under legal hold?**
The `DefaultRetentionEnforcer` checks `ILegalHoldStore.IsUnderHoldAsync` for each expired record before deletion. Records under active legal hold are skipped and their status is updated to `UnderLegalHold`. They will be re-evaluated in subsequent enforcement cycles after the hold is released.

**Q: Can I use the retention module without the DSR module?**
Yes. If `IDataErasureExecutor` is not registered, the enforcer operates in degraded mode: retention records are marked as `Deleted` but no physical data erasure occurs. A warning is logged indicating that DSR integration is not configured.

**Q: How are retention policies auto-registered from attributes?**
When `AutoRegisterFromAttributes` is `true`, the `RetentionAutoRegistrationHostedService` scans the configured assemblies for types and properties decorated with `[RetentionPeriod]`. For each discovered `DataCategory` without an existing policy, a new `RetentionPolicy` is created in the store at startup.

**Q: How often does the enforcement service run?**
By default, every 60 minutes (`EnforcementInterval = TimeSpan.FromMinutes(60)`). The first cycle runs immediately at service startup, then subsequent cycles are triggered by `PeriodicTimer`. Shorter intervals increase enforcement responsiveness but may add database load.
