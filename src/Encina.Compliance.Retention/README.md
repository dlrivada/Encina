# Encina.Compliance.Retention

[![NuGet](https://img.shields.io/nuget/v/Encina.Compliance.Retention.svg)](https://www.nuget.org/packages/Encina.Compliance.Retention/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](../../LICENSE)

GDPR Storage Limitation compliance for Encina. Provides declarative data retention enforcement at the CQRS pipeline level with automatic expiration tracking, periodic deletion, legal hold management, and compliance health checks. Implements GDPR Article 5(1)(e).

## Features

- **Declarative Retention Periods** -- `[RetentionPeriod(Days = 365)]` attribute on response types and properties
- **Pipeline-Level Tracking** -- `RetentionValidationPipelineBehavior` creates retention records when data is created
- **Automatic Enforcement** -- `RetentionEnforcementService` (BackgroundService with PeriodicTimer) runs periodic deletion cycles
- **Legal Hold Support** -- `ILegalHoldManager` for litigation preservation per Article 17(3)(e)
- **Fluent Policy Configuration** -- `AddPolicy()` builder API with `RetainForDays()`, `RetainForYears()`, `WithAutoDelete()`, `WithLegalBasis()`
- **Three Enforcement Modes** -- `Block` (reject), `Warn` (log and proceed), `Disabled` (no-op)
- **Expiration Alerts** -- Proactive notifications for data approaching retention deadline
- **Category-Scoped Erasure** -- Delegates physical erasure of each expired record to the application's `IRetentionDataEraser`, which erases only that record's data category for that entity
- **Immutable Audit Trail** -- Every retention operation is recorded via `IRetentionAuditStore`
- **Domain Notifications** -- `DataExpiringNotification`, `DataDeletedNotification`, `LegalHoldAppliedNotification`, `LegalHoldReleasedNotification`, `RetentionEnforcementCompletedNotification`
- **Railway Oriented Programming** -- All operations return `Either<EncinaError, T>`, no exceptions
- **Full Observability** -- OpenTelemetry tracing, 10 counters, 3 histograms, 70 structured log events, health check
- **10 Database Providers** -- ADO.NET, Dapper, EF Core (SQL Server, PostgreSQL, MySQL) + MongoDB (planned)
- **.NET 10 Compatible** -- Built with latest C# features

## Installation

```bash
dotnet add package Encina.Compliance.Retention
```

## Quick Start

### 1. Register Services

```csharp
services.AddEncina(config =>
    config.RegisterServicesFromAssemblyContaining<Program>());

services.AddEncinaRetention(options =>
{
    options.EnforcementMode = RetentionEnforcementMode.Block;
    options.DefaultRetentionPeriod = TimeSpan.FromDays(365);
    options.AlertBeforeExpirationDays = 30;
    options.TrackAuditTrail = true;
    options.AutoRegisterFromAttributes = true;
    options.AssembliesToScan.Add(typeof(Program).Assembly);
});
```

### 2. Mark Data with Retention Attributes

```csharp
// Apply to a class -- all instances retain for 7 years
[RetentionPeriod(Years = 7, DataCategory = "financial-records",
    Reason = "German tax law (AO section 147)")]
public sealed record Invoice(string Id, decimal Amount, DateTimeOffset CreatedAtUtc);

// Apply to a property -- specific field retention
public sealed record CustomerProfile
{
    [RetentionPeriod(Days = 365, DataCategory = "marketing-consent",
        Reason = "Consent validity period", AutoDelete = true)]
    public string? MarketingPreferences { get; init; }
}

// Minimal usage with days only
[RetentionPeriod(Days = 90)]
public sealed record SessionLog(string SessionId, DateTimeOffset StartedAtUtc);
```

### 3. Configure Policies via Fluent API

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

    options.AddPolicy("session-data", policy =>
    {
        policy.RetainFor(TimeSpan.FromHours(24));
        policy.WithAutoDelete();
        policy.WithReason("Short-lived session data");
    });
});
```

### 4. Legal Hold Management

```csharp
var holdManager = serviceProvider.GetRequiredService<ILegalHoldManager>();

// Apply a legal hold to prevent deletion during litigation
var hold = LegalHold.Create(
    entityId: "invoice-12345",
    reason: "Pending tax audit for fiscal year 2024",
    appliedByUserId: "legal-counsel@company.com");

await holdManager.ApplyHoldAsync("invoice-12345", hold, cancellationToken);

// Check if an entity is under hold
var isHeld = await holdManager.IsUnderHoldAsync("invoice-12345", cancellationToken);

// Release the hold when litigation concludes
await holdManager.ReleaseHoldAsync(hold.Id, "legal-counsel@company.com", cancellationToken);

// List all active holds for compliance reporting
var activeHolds = await holdManager.GetActiveHoldsAsync(cancellationToken);
```

### 5. Enforcement Configuration

```csharp
services.AddEncinaRetention(options =>
{
    // Automatic enforcement (default: enabled)
    options.EnableAutomaticEnforcement = true;
    options.EnforcementInterval = TimeSpan.FromMinutes(60);

    // Alert before expiration
    options.AlertBeforeExpirationDays = 30;

    // Notifications and audit
    options.PublishNotifications = true;
    options.TrackAuditTrail = true;
});

// Manual enforcement (if automatic is disabled)
var enforcer = serviceProvider.GetRequiredService<IRetentionEnforcer>();
var result = await enforcer.EnforceRetentionAsync(cancellationToken);

result.Match(
    Right: deletion => Console.WriteLine(
        $"Enforcement complete: {deletion.RecordsDeleted} deleted, " +
        $"{deletion.RecordsUnderHold} held, {deletion.RecordsFailed} failed"),
    Left: error => Console.WriteLine($"Enforcement failed: {error.Message}"));

// Query data approaching expiration for proactive alerts
var expiring = await enforcer.GetExpiringDataAsync(TimeSpan.FromDays(30), cancellationToken);
```

## Enforcement Modes

| Mode | Behavior | Use Case |
|------|----------|----------|
| `Block` | Retention record creation failures block the response | Production (recommended) |
| `Warn` | Log warning, allow response to proceed | Migration/testing phase (default) |
| `Disabled` | Skip all retention tracking | Development environments |

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `EnforcementMode` | `RetentionEnforcementMode` | `Warn` | How to handle retention record creation failures |
| `DefaultRetentionPeriod` | `TimeSpan?` | `null` | Default retention period when no category-specific policy exists |
| `AlertBeforeExpirationDays` | `int` | `30` | Days before expiration to generate alerts |
| `PublishNotifications` | `bool` | `true` | Publish domain notifications for retention lifecycle events |
| `TrackAuditTrail` | `bool` | `true` | Record all retention operations in audit store |
| `AddHealthCheck` | `bool` | `false` | Register health check with `IHealthChecksBuilder` |
| `EnableAutomaticEnforcement` | `bool` | `true` | Enable background enforcement service |
| `EnforcementInterval` | `TimeSpan` | `60 min` | Interval between automatic enforcement cycles |
| `AutoRegisterFromAttributes` | `bool` | `true` | Scan assemblies for `[RetentionPeriod]` at startup |
| `AssembliesToScan` | `List<Assembly>` | `[]` | Assemblies to scan for `[RetentionPeriod]` attributes |

## Error Codes

| Code | Meaning |
|------|---------|
| `retention.policy_not_found` | No retention policy found with the given identifier |
| `retention.policy_already_exists` | A retention policy already exists for the data category |
| `retention.record_not_found` | No retention record found with the given identifier |
| `retention.record_already_exists` | A retention record already exists for the entity |
| `retention.hold_not_found` | No legal hold found with the given identifier |
| `retention.hold_already_active` | An active legal hold already exists for the entity |
| `retention.enforcement_failed` | The retention enforcement cycle failed |
| `retention.deletion_failed` | Data deletion failed during enforcement |
| `retention.store_error` | Retention persistence store operation failed |
| `retention.invalid_parameter` | An invalid parameter was provided to a retention operation |
| `retention.no_policy_for_category` | No retention policy defined for the requested data category |
| `retention.pipeline_record_creation_failed` | The pipeline behavior failed to create a retention record |
| `retention.pipeline_entity_id_not_found` | Could not resolve an entity ID from the response type |

## Custom Implementations

Register custom implementations before `AddEncinaRetention()` to override defaults (TryAdd semantics):

```csharp
// Custom store implementations (e.g., database-backed)
services.AddSingleton<IRetentionRecordStore, DatabaseRetentionRecordStore>();
services.AddSingleton<IRetentionPolicyStore, DatabaseRetentionPolicyStore>();
services.AddSingleton<ILegalHoldStore, DatabaseLegalHoldStore>();
services.AddSingleton<IRetentionAuditStore, DatabaseRetentionAuditStore>();

// Custom service implementations
services.AddSingleton<IRetentionPolicy, CustomRetentionPolicy>();
services.AddSingleton<IRetentionEnforcer, CustomRetentionEnforcer>();
services.AddSingleton<ILegalHoldManager, CustomLegalHoldManager>();

services.AddEncinaRetention(options =>
{
    options.EnforcementMode = RetentionEnforcementMode.Block;
    options.AutoRegisterFromAttributes = false;
});
```

## Database Providers

The core package ships with `InMemoryRetentionRecordStore`, `InMemoryRetentionPolicyStore`, `InMemoryLegalHoldStore`, and `InMemoryRetentionAuditStore` for development and testing. Database-backed implementations for the 10 providers are available via satellite packages:

```csharp
// ADO.NET (SQL Server example)
services.AddEncinaADO(config =>
{
    config.UseRetention = true;
});

// Dapper (SQL Server example)
services.AddEncinaDapper(config =>
{
    config.UseRetention = true;
});

// EF Core (PostgreSQL example)
services.AddEncinaEntityFrameworkCore<AppDbContext>(config =>
{
    config.UseRetention = true;
});
```

## Observability

- **Tracing**: `Encina.Compliance.Retention` ActivitySource with retention-specific activities (`Retention.Pipeline`, `Retention.Enforcement`, `Retention.Deletion`, `Retention.LegalHold`, `Retention.PolicyResolution`, `Retention.Audit`)
- **Metrics**: 10 counters (`retention.pipeline.executions.total`, `retention.enforcement.cycles.total`, `retention.records.created.total`, `retention.records.deleted.total`, `retention.records.held.total`, `retention.records.failed.total`, `retention.legal_holds.applied.total`, `retention.legal_holds.released.total`, `retention.policies.resolved.total`, `retention.audit.entries.total`) and 3 histograms (`retention.enforcement.duration`, `retention.pipeline.duration`, `retention.deletion.duration`)
- **Logging**: 70 structured log events via `[LoggerMessage]` source generator (zero-allocation), event IDs 8500-8569
- **Health Check**: Verifies store connectivity, required services, enforcement service status, and legal hold availability

## Health Check

Enable via `RetentionOptions.AddHealthCheck`:

```csharp
services.AddEncinaRetention(options =>
{
    options.AddHealthCheck = true;
});
```

The health check (`encina-retention`) verifies:
- `RetentionOptions` are configured
- `IRetentionRecordService` is resolvable
- `IRetentionPolicyService` is resolvable
- `ILegalHoldService` is resolvable (optional, Degraded if missing)
- An `IRetentionDataEraser` is registered when `EnableAutomaticEnforcement` is on (Degraded if missing, because expired data would never be erased)

Tags: `encina`, `gdpr`, `retention`, `compliance`, `ready`

## Erasing Expired Data

Each retention record carries an entity and a data category, so one entity can have several records with different periods (for example a patient's contact data kept for one year and the clinical record kept for five). When a record expires, `RetentionEnforcementService` calls `IRetentionDataEraser.EraseAsync` with a `RetentionErasureTarget` (record id, entity id, data category, expiry, tenant and module) and marks the record `Deleted` only after it returns `Right`. The eraser must erase that category of data for that entity and nothing else; the entity's other categories are still within their own periods. The application implements it, because only the application knows where each category of data lives:

```csharp
// Program.cs
services.AddScoped<IRetentionDataEraser, PatientDataEraser>();
```

```csharp
// PatientDataEraser.cs
public sealed class PatientDataEraser(AppDbContext db) : IRetentionDataEraser
{
    public async ValueTask<Either<EncinaError, Unit>> EraseAsync(
        RetentionErasureTarget target, CancellationToken cancellationToken = default)
    {
        // A multi-tenant application: there is no ambient tenant in the background enforcement
        // scope, so scope every statement to target.TenantId, or refuse when it is missing.
        if (string.IsNullOrEmpty(target.TenantId))
        {
            return EncinaError.New($"Retention record '{target.RecordId}' has no tenant; refusing to erase.");
        }

        switch (target.DataCategory)
        {
            case "patient-contact":
                await db.PatientContacts
                    .IgnoreQueryFilters()
                    .Where(c => c.TenantId == target.TenantId && c.PatientId == target.EntityId)
                    .ExecuteDeleteAsync(cancellationToken);
                return Unit.Default;

            case "clinical-record":
                await db.ClinicalNotes
                    .IgnoreQueryFilters()
                    .Where(n => n.TenantId == target.TenantId && n.PatientId == target.EntityId)
                    .ExecuteDeleteAsync(cancellationToken);
                return Unit.Default;

            default:
                return EncinaError.New($"No eraser for retention category '{target.DataCategory}'.");
        }
    }
}
```

Return `Left` when any of the data could not be erased: the record stays `Expired` and the next cycle retries it, so the eraser must be idempotent.

Scope the erasure by `target.TenantId` (and `target.ModuleId` when modules are isolated): the enforcement service runs in a background scope with no ambient tenant, so tenant query filters do not apply on their own. Return `Left`, never `Right`, when the tenant scope cannot be established. `RetentionValidationPipelineBehavior` records the request's tenant and module on each record.

One entity can also have several records in the same category (one per tracking call, for example one per clinical episode). While another record of the same entity, category, tenant and module is still retained (`Active` within its period, or `UnderLegalHold`), an expired record stays `Expired` and nothing is erased (EventId 8591); when the last one expires, the category is erased once and all of them are marked `Deleted`.

If no `IRetentionDataEraser` is registered, the enforcer erases nothing and never marks a record deleted: expired records stay `Expired`, are counted as failed and a warning (EventId 8519) is logged once per enforcement cycle.

### Why not the data subject rights erasure executor

`IDataErasureExecutor` from `Encina.Compliance.DataSubjectRights` erases by data subject and by `PersonalDataCategory`. A retention record identifies an entity (which is not necessarily a data subject) and a free-form retention category (which does not map one-to-one onto a `PersonalDataCategory`). Passing the entity id as a subject id and no category, as earlier versions did, erased every category of the entity when any one record expired (#1160). An eraser may still delegate to `IDataErasureExecutor` when, in your application, the entity is the data subject and you own the mapping from retention categories to personal data categories. See [ADR-031](../../docs/architecture/adr/031-retention-erasure-port.md).

## Lifting Legal Holds

`ILegalHoldService.LiftHoldAsync` lifts the hold and, when no other active hold remains on the entity, releases the entity's held records (`UnderLegalHold` → `Expired` or `Active`). It never reports a success it did not achieve:

- If whether another hold remains cannot be determined, no record is released (fail closed) and the call returns `retention.hold_release_incomplete` (EventId 8589).
- If some records cannot be released, the others still are; the call returns `retention.hold_release_incomplete` with the failed record ids in the error details under `failedRecordIds` (EventId 8588 per record).
- Calling `LiftHoldAsync` again for the same hold does not lift it twice: it retries the release of the records still held (EventId 8590, with the id of the user who retried) and returns `Right` once they are all released.
- A record already released counts as released even if a stale read model still lists it as held (`ReleaseRecordAsync` is idempotent, EventId 8593).
- An empty `releasedByUserId` is rejected with `retention.invalid_parameter` on every call, retries included.

Records that are not released stay `UnderLegalHold`, so the enforcement cycle never erases them.

## Testing

```csharp
// Use in-memory stores for unit testing (registered by default)
services.AddEncinaRetention(options =>
{
    options.EnforcementMode = RetentionEnforcementMode.Block;
    options.EnableAutomaticEnforcement = false; // Disable background service in tests
    options.AddPolicy("test-data", p => p.RetainForDays(30).WithAutoDelete());
});
```

## Related Packages

| Package | Description |
|---------|-------------|
| `Encina` | Core CQRS pipeline with `IPipelineBehavior` |
| `Encina.Compliance.DataSubjectRights` | GDPR Articles 15-22 data subject rights management |
| `Encina.Compliance.GDPR` | GDPR processing activity tracking and RoPA |
| `Encina.Compliance.Consent` | GDPR Article 7 consent management |
| `Encina.Compliance.LawfulBasis` | GDPR Article 6 lawful basis tracking |
| `Encina.Compliance.Anonymization` | GDPR Article 4(5) data anonymization and pseudonymization |

## GDPR Compliance

This package implements key GDPR requirements:

| Article | Requirement | Implementation |
|---------|-------------|----------------|
| **5(1)(e)** | Storage limitation -- data kept no longer than necessary | `RetentionEnforcementService`, `[RetentionPeriod]`, `IRetentionPolicy` |
| **5(2)** | Accountability -- demonstrate compliance | `IRetentionAuditStore`, `TrackAuditTrail` option |
| **17(1)(a)** | Right to erasure when data no longer necessary | `RetentionEnforcementService`, `IRetentionDataEraser` (category-scoped) |
| **17(3)(e)** | Legal claims exemption from erasure | `ILegalHoldManager`, `LegalHold`, `RetentionStatus.UnderLegalHold` |
| **Recital 39** | Time limits for erasure or periodic review | `EnforcementInterval`, `AlertBeforeExpirationDays`, `GetExpiringDataAsync` |

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.
