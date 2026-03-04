# Encina.Compliance.BreachNotification

[![NuGet](https://img.shields.io/nuget/v/Encina.Compliance.BreachNotification.svg)](https://www.nuget.org/packages/Encina.Compliance.BreachNotification/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](../../LICENSE)

GDPR 72-hour breach notification compliance for Encina. Provides pipeline-level breach detection, supervisory authority and data subject notification, phased reporting, deadline monitoring, and immutable audit trail. Implements GDPR Articles 33 and 34.

## Features

- **Pipeline-Level Detection** -- `BreachDetectionPipelineBehavior` evaluates security events from `[BreachMonitored]` requests against detection rules
- **Pluggable Detection Engine** -- `IBreachDetector` with `IBreachDetectionRule` interface for custom and built-in rules
- **Four Built-In Rules** -- `UnauthorizedAccessRule`, `MassDataExfiltrationRule`, `PrivilegeEscalationRule`, `AnomalousQueryPatternRule`
- **72-Hour Deadline Tracking** -- Automatic deadline calculation from detection, with configurable alert thresholds
- **Phased Reporting** -- `IBreachHandler.AddPhasedReportAsync` for incremental disclosure per Article 33(4)
- **Authority Notification** -- `IBreachNotifier.NotifyAuthorityAsync` per Article 33(1)
- **Data Subject Notification** -- `IBreachNotifier.NotifyDataSubjectsAsync` per Article 34(1) with exemption support (Art. 34(3))
- **Deadline Monitoring** -- `BreachDeadlineMonitorService` background service with configurable check intervals
- **Three Enforcement Modes** -- `Block` (reject), `Warn` (log and proceed), `Disabled` (no-op)
- **Immutable Audit Trail** -- Every breach operation recorded via `IBreachAuditStore` per Article 33(5)
- **Domain Notifications** -- `BreachDetectedNotification`, `AuthorityNotifiedNotification`, `SubjectsNotifiedNotification`, `DeadlineWarningNotification`, `BreachResolvedNotification`
- **Railway Oriented Programming** -- All operations return `Either<EncinaError, T>`, no exceptions
- **Full Observability** -- OpenTelemetry tracing (4 activity types), 6 counters, 3 histograms, structured log events, health check
- **13 Database Providers** -- ADO.NET, Dapper, EF Core (SQLite, SQL Server, PostgreSQL, MySQL) + MongoDB
- **.NET 10 Compatible** -- Built with latest C# features

## Installation

```bash
dotnet add package Encina.Compliance.BreachNotification
```

## Quick Start

### 1. Register Services

```csharp
services.AddEncina(config =>
    config.RegisterServicesFromAssemblyContaining<Program>());

services.AddEncinaBreachNotification(options =>
{
    options.EnforcementMode = BreachDetectionEnforcementMode.Block;
    options.NotificationDeadlineHours = 72;
    options.EnableDeadlineMonitoring = true;
    options.AddHealthCheck = true;
    options.SupervisoryAuthority = "dpa@authority.eu";
    options.AssembliesToScan.Add(typeof(Program).Assembly);
});
```

### 2. Mark Requests for Breach Monitoring

```csharp
[BreachMonitored(SecurityEventType.UnauthorizedAccess)]
public sealed record SensitiveDataQuery(string UserId)
    : IRequest<Either<EncinaError, SensitiveData>>;
```

### 3. Handle Detected Breaches

```csharp
var handler = serviceProvider.GetRequiredService<IBreachHandler>();

// Handle a detected breach (creates formal record)
var result = await handler.HandleDetectedBreachAsync(potentialBreach);

result.Match(
    Right: breach => Console.WriteLine(
        $"Breach '{breach.Id}' recorded. Deadline: {breach.NotificationDeadlineUtc}"),
    Left: error => Console.WriteLine($"Failed: {error.Message}"));
```

### 4. Notify Supervisory Authority

```csharp
// Notify authority within 72 hours (Art. 33)
var notifyResult = await handler.NotifyAuthorityAsync(breachId);

notifyResult.Match(
    Right: result => Console.WriteLine(
        $"Authority notified: {result.Outcome}"),
    Left: error => Console.WriteLine($"Notification failed: {error.Message}"));
```

### 5. Add Phased Reports

```csharp
// Submit additional information as it becomes available (Art. 33(4))
var report = await handler.AddPhasedReportAsync(
    breachId,
    "Updated impact assessment: 1,500 records affected",
    userId: "dpo@company.com");
```

### 6. Notify Data Subjects

```csharp
// Notify affected individuals when high risk (Art. 34)
var subjectResult = await handler.NotifySubjectsAsync(
    breachId,
    subjectIds: ["user-001", "user-002", "user-003"]);
```

### 7. Resolve the Breach

```csharp
// Close the breach with resolution summary (Art. 33(3)(d))
var resolveResult = await handler.ResolveBreachAsync(
    breachId,
    "Root cause identified and patched. All affected users notified.");
```

## Custom Detection Rules

```csharp
public sealed class GeoLocationAnomalyRule : IBreachDetectionRule
{
    public string Name => "GeoLocationAnomaly";
    public string Description => "Detects access from unexpected geographic locations";

    public ValueTask<Either<EncinaError, Option<PotentialBreach>>> EvaluateAsync(
        SecurityEvent securityEvent,
        CancellationToken cancellationToken = default)
    {
        // Your detection logic here
    }
}

// Register via options
services.AddEncinaBreachNotification(options =>
{
    options.AddDetectionRule<GeoLocationAnomalyRule>();
});
```

## Enforcement Modes

| Mode | Behavior | Use Case |
|------|----------|----------|
| `Block` | Returns error when breach detected | Production (recommended) |
| `Warn` | Logs warning, allows response through | Migration/testing phase (default) |
| `Disabled` | Skips all breach detection entirely | Development environments |

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `EnforcementMode` | `BreachDetectionEnforcementMode` | `Warn` | Pipeline behavior enforcement mode |
| `NotificationDeadlineHours` | `int` | `72` | Hours from detection to authority notification deadline (Art. 33(1)) |
| `AlertAtHoursRemaining` | `int[]` | `[48,24,12,6,1]` | Threshold hours for deadline warning notifications |
| `SupervisoryAuthority` | `string?` | `null` | Authority contact (must be configured for production) |
| `AutoNotifyOnHighSeverity` | `bool` | `false` | Auto-notify authority for High+ severity breaches |
| `PhasedReportingEnabled` | `bool` | `true` | Enable phased reporting per Art. 33(4) |
| `SubjectNotificationSeverityThreshold` | `BreachSeverity` | `High` | Minimum severity for data subject notification |
| `PublishNotifications` | `bool` | `true` | Publish domain notifications |
| `TrackAuditTrail` | `bool` | `true` | Record audit entries per Art. 33(5) |
| `EnableDeadlineMonitoring` | `bool` | `false` | Enable background deadline monitoring service |
| `DeadlineCheckInterval` | `TimeSpan` | `15 min` | Interval between deadline monitoring checks |
| `AddHealthCheck` | `bool` | `false` | Register health check |
| `UnauthorizedAccessThreshold` | `int` | `5` | Threshold for built-in unauthorized access rule |
| `DataExfiltrationThresholdMB` | `int` | `100` | Threshold (MB) for built-in exfiltration rule |
| `AnomalousQueryThreshold` | `int` | `1000` | Threshold for built-in anomalous query rule |

## Error Codes

| Code | Meaning |
|------|---------|
| `breach.not_found` | Breach record not found |
| `breach.already_exists` | Breach record already exists |
| `breach.already_resolved` | Action attempted on resolved breach |
| `breach.detection_failed` | Detection engine failure |
| `breach.notification_failed` | Generic notification failure |
| `breach.authority_notification_failed` | Authority notification failure |
| `breach.subject_notification_failed` | Data subject notification failure |
| `breach.deadline_expired` | 72-hour deadline has passed |
| `breach.store_error` | Persistence store operation failure |
| `breach.invalid_parameter` | Invalid parameter provided |
| `breach.rule_evaluation_failed` | Detection rule evaluation failure |
| `breach.phased_report_failed` | Phased report submission failure |
| `breach.detected` | Pipeline blocked request due to breach detection |
| `breach.exemption_invalid` | Invalid Art. 34(3) exemption |

## Custom Implementations

Register custom implementations before `AddEncinaBreachNotification()` to override defaults (TryAdd semantics):

```csharp
// Custom store implementations (e.g., database-backed)
services.AddSingleton<IBreachRecordStore, DatabaseBreachRecordStore>();
services.AddSingleton<IBreachAuditStore, DatabaseBreachAuditStore>();

// Custom high-level services
services.AddSingleton<IBreachDetector, SiemIntegrationDetector>();
services.AddSingleton<IBreachNotifier, EmailBreachNotifier>();

services.AddEncinaBreachNotification(options =>
{
    options.EnforcementMode = BreachDetectionEnforcementMode.Block;
});
```

## Database Providers

The core package ships with `InMemoryBreachRecordStore` and `InMemoryBreachAuditStore` for development and testing. Database-backed implementations for the 13 providers are available via satellite packages:

```csharp
// ADO.NET (SQLite example)
services.AddEncinaADO(config =>
{
    config.UseBreachNotification = true;
});

// Dapper (SQL Server example)
services.AddEncinaDapper(config =>
{
    config.UseBreachNotification = true;
});

// EF Core (PostgreSQL example)
services.AddEncinaEntityFrameworkCore<AppDbContext>(config =>
{
    config.UseBreachNotification = true;
});
```

## Observability

- **Tracing**: `Encina.Compliance.BreachNotification` ActivitySource with 4 activity types (`BreachNotification.Detection`, `BreachNotification.Notification`, `BreachNotification.Pipeline`, `BreachNotification.DeadlineCheck`)
- **Metrics**: 6 counters (`breach.detected.total`, `breach.notification.authority.total`, `breach.notification.subjects.total`, `breach.pipeline.executions.total`, `breach.phased_reports.total`, `breach.resolved.total`) and 3 histograms (`breach.time_to_notification.hours`, `breach.detection.duration.ms`, `breach.pipeline.duration.ms`)
- **Logging**: Structured log events via `[LoggerMessage]` source generator (zero-allocation)
- **Health Check**: Verifies store connectivity, detector availability, and overdue breach status

## Health Check

Enable via `BreachNotificationOptions.AddHealthCheck`:

```csharp
services.AddEncinaBreachNotification(options =>
{
    options.AddHealthCheck = true;
});
```

The health check (`encina-breach-notification`) verifies:
- `BreachNotificationOptions` are configured
- `IBreachRecordStore` is resolvable
- `IBreachAuditStore` is resolvable (when `TrackAuditTrail` is enabled)
- `IBreachDetector` is resolvable
- `IBreachHandler` is resolvable

Tags: `encina`, `gdpr`, `breach-notification`, `compliance`, `ready`

## Testing

```csharp
// Use in-memory stores for unit testing (registered by default)
services.AddEncinaBreachNotification(options =>
{
    options.EnforcementMode = BreachDetectionEnforcementMode.Block;
    options.EnableDeadlineMonitoring = false; // Disable background service in tests
    options.NotificationDeadlineHours = 72;
});
```

## Related Packages

| Package | Description |
|---------|-------------|
| `Encina` | Core CQRS pipeline with `IPipelineBehavior` |
| `Encina.Compliance.Retention` | GDPR Article 5(1)(e) data retention management |
| `Encina.Compliance.DataSubjectRights` | GDPR Articles 15-22 data subject rights |
| `Encina.Compliance.GDPR` | GDPR processing activity tracking and RoPA |
| `Encina.Compliance.Consent` | GDPR Article 7 consent management |
| `Encina.Compliance.LawfulBasis` | GDPR Article 6 lawful basis tracking |

## GDPR Compliance

This package implements key GDPR requirements:

| Article | Requirement | Implementation |
|---------|-------------|----------------|
| **33(1)** | Notify authority within 72 hours | `BreachDeadlineMonitorService`, `DeadlineStatus`, `NotificationDeadlineHours` |
| **33(3)** | Required notification content | `BreachRecord` with severity, nature, DPO, consequences, measures |
| **33(4)** | Phased reporting when full info unavailable | `IBreachHandler.AddPhasedReportAsync`, `PhasedReport` |
| **33(5)** | Document all breaches and remedial action | `IBreachAuditStore`, `TrackAuditTrail` option |
| **34(1)** | Notify data subjects when high risk | `IBreachHandler.NotifySubjectsAsync`, `SubjectNotificationSeverityThreshold` |
| **34(3)** | Exemptions from subject notification | `SubjectNotificationExemption` enum (encryption, risk eliminated, disproportionate effort) |

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.
