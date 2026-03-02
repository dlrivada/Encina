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
- **DSR Integration** -- Delegates physical erasure to `IDataErasureExecutor` from `Encina.Compliance.DataSubjectRights`
- **Immutable Audit Trail** -- Every retention operation is recorded via `IRetentionAuditStore`
- **Domain Notifications** -- `DataExpiringNotification`, `DataDeletedNotification`, `LegalHoldAppliedNotification`, `LegalHoldReleasedNotification`, `RetentionEnforcementCompletedNotification`
- **Railway Oriented Programming** -- All operations return `Either<EncinaError, T>`, no exceptions
- **Full Observability** -- OpenTelemetry tracing, 10 counters, 3 histograms, 70 structured log events, health check
- **13 Database Providers** -- ADO.NET, Dapper, EF Core (SQLite, SQL Server, PostgreSQL, MySQL) + MongoDB (planned)
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
| `retention.hold_already_released` | The legal hold has already been released |
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

The core package ships with `InMemoryRetentionRecordStore`, `InMemoryRetentionPolicyStore`, `InMemoryLegalHoldStore`, and `InMemoryRetentionAuditStore` for development and testing. Database-backed implementations for the 13 providers are available via satellite packages:

```csharp
// ADO.NET (SQLite example)
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
- `IRetentionRecordStore` is resolvable
- `IRetentionPolicyStore` is resolvable
- `IRetentionEnforcer` is resolvable
- `ILegalHoldStore` is resolvable (optional, Degraded if missing)
- `IRetentionAuditStore` is resolvable when `TrackAuditTrail` is enabled

Tags: `encina`, `gdpr`, `retention`, `compliance`, `ready`

## Integration with DataSubjectRights

The Retention module integrates with `Encina.Compliance.DataSubjectRights` for physical data erasure. When the `RetentionEnforcementService` identifies expired records, it delegates actual deletion to `IDataErasureExecutor` (registered by the DSR module):

```csharp
// Register both modules for full compliance
services.AddEncinaDataSubjectRights(options =>
{
    options.RestrictionEnforcementMode = DSREnforcementMode.Block;
});

services.AddEncinaRetention(options =>
{
    options.EnforcementMode = RetentionEnforcementMode.Block;
    options.EnableAutomaticEnforcement = true;
});
```

If `IDataErasureExecutor` is not registered, the enforcer operates in degraded mode: records are marked as deleted but no physical erasure occurs. A warning is logged indicating DSR integration is not configured.

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
| **17(1)(a)** | Right to erasure when data no longer necessary | `IRetentionEnforcer.EnforceRetentionAsync`, `IDataErasureExecutor` integration |
| **17(3)(e)** | Legal claims exemption from erasure | `ILegalHoldManager`, `LegalHold`, `RetentionStatus.UnderLegalHold` |
| **Recital 39** | Time limits for erasure or periodic review | `EnforcementInterval`, `AlertBeforeExpirationDays`, `GetExpiringDataAsync` |

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.
