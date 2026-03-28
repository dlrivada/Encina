---
title: "Breach Notification in Encina"
layout: default
parent: "Features"
---

# Breach Notification in Encina

This guide explains how to manage GDPR Articles 33-34 breach notification -- pipeline-level breach detection, 72-hour supervisory authority notification, data subject communication, and phased reporting using the `Encina.Compliance.BreachNotification` package. Breach detection operates at the CQRS pipeline level, ensuring consistent monitoring across all entry points.

## Table of Contents

1. [Overview](#overview)
2. [The Problem](#the-problem)
3. [The Solution](#the-solution)
4. [Quick Start](#quick-start)
5. [BreachMonitored Attribute](#breachmonitored-attribute)
6. [Breach Lifecycle](#breach-lifecycle)
7. [Detection Engine](#detection-engine)
8. [Built-In Detection Rules](#built-in-detection-rules)
9. [Custom Detection Rules](#custom-detection-rules)
10. [Notification Workflow](#notification-workflow)
11. [Phased Reporting](#phased-reporting)
12. [Deadline Monitoring](#deadline-monitoring)
13. [Data Subject Notification](#data-subject-notification)
14. [Audit Trail](#audit-trail)
15. [Domain Notifications](#domain-notifications)
16. [Configuration Options](#configuration-options)
17. [Enforcement Modes](#enforcement-modes)
18. [Database Providers](#database-providers)
19. [Observability](#observability)
20. [Health Check](#health-check)
21. [Error Handling](#error-handling)
22. [Best Practices](#best-practices)
23. [Testing](#testing)
24. [FAQ](#faq)

---

## Overview

Encina.Compliance.BreachNotification provides attribute-based breach detection and notification management at the CQRS pipeline level:

| Component | Description |
|-----------|-------------|
| **`[BreachMonitored]` Attribute** | Marks request types for automatic security event generation and detection |
| **`BreachDetectionPipelineBehavior`** | Pipeline behavior that generates security events and evaluates detection rules |
| **`IBreachDetector`** | Evaluates security events against all registered `IBreachDetectionRule` implementations |
| **`IBreachHandler`** | Orchestrates the full breach lifecycle (detection, notification, phased reporting, resolution) |
| **`IBreachNotifier`** | Delivers notifications to supervisory authorities and data subjects |
| **`IBreachRecordStore`** | Breach record persistence (lifecycle tracking per breach) |
| **`IBreachAuditStore`** | Immutable audit trail for all breach operations |
| **`BreachDeadlineMonitorService`** | `BackgroundService` with `PeriodicTimer` for 72-hour deadline tracking |
| **`BreachNotificationOptions`** | Configuration for enforcement mode, deadlines, thresholds, and monitoring |

### Why Pipeline-Level Breach Detection?

| Benefit | Description |
|---------|-------------|
| **Automatic monitoring** | Security events are generated whenever a `[BreachMonitored]`-decorated request passes through the pipeline |
| **Declarative** | Monitoring requirements live with the request types, not scattered across services |
| **Transport-agnostic** | Same breach detection for HTTP, message queue, gRPC, and serverless |
| **Pluggable rules** | Add custom detection rules via `IBreachDetectionRule` or use the four built-in rules |
| **72-hour compliance** | Automatic deadline calculation and proactive alerts per Article 33(1) |
| **Auditable** | Every breach operation is recorded with timestamps, actors, and compliance metadata |

---

## The Problem

GDPR Articles 33 and 34 impose strict breach notification requirements on data controllers:

- **72-hour deadline** for notifying the supervisory authority (Art. 33(1)) with no systematic tracking
- **No automated detection** of security incidents that constitute personal data breaches
- **No phased reporting** infrastructure when full information is not immediately available (Art. 33(4))
- **No audit trail** to demonstrate compliance with the documentation requirement (Art. 33(5))
- **Manual notification processes** that are error-prone and may miss the 72-hour deadline
- **No data subject notification workflow** for high-risk breaches (Art. 34(1))
- **No exemption tracking** for Article 34(3) exemptions (encryption, risk elimination, disproportionate effort)

---

## The Solution

Encina solves this with a unified breach detection and notification pipeline:

```text
Request → Handler → Response → [BreachDetectionPipelineBehavior]
                                       |
                                       +-- No [BreachMonitored] attribute? → Skip (zero overhead)
                                       +-- Disabled mode? → Skip
                                       +-- Generate SecurityEvent from request metadata
                                       +-- Evaluate all IBreachDetectionRule implementations
                                       |   +-- No breaches detected → Return response
                                       |   +-- Breach detected + Block mode → Return error
                                       |   +-- Breach detected + Warn mode → Log warning, return response
                                       +-- For each detected breach:
                                           +-- Create BreachRecord via IBreachHandler
                                           +-- Start 72-hour deadline
                                           +-- Publish BreachDetectedNotification
                                           +-- Record audit entry

[BreachDeadlineMonitorService] (periodic background service)
       |
       +-- Query active breaches from IBreachRecordStore
       +-- For each approaching deadline:
       |   +-- Calculate remaining hours
       |   +-- Publish DeadlineWarningNotification at configured thresholds
       +-- For overdue breaches:
           +-- Log critical warning
           +-- Record audit entry
```

---

## Quick Start

### 1. Install the Package

```bash
dotnet add package Encina.Compliance.BreachNotification
```

### 2. Decorate Request Types with Breach Monitoring

```csharp
// Monitor for unauthorized access attempts
[BreachMonitored(SecurityEventType.UnauthorizedAccess)]
public sealed record AccessSensitiveDataQuery(string UserId)
    : IRequest<Either<EncinaError, SensitiveData>>;

// Monitor for data exfiltration patterns
[BreachMonitored(SecurityEventType.DataExfiltration)]
public sealed record BulkExportCommand(string ExportType, int RecordCount)
    : IRequest<Either<EncinaError, ExportResult>>;
```

### 3. Register Services

```csharp
services.AddEncina(config =>
    config.RegisterServicesFromAssemblyContaining<Program>());

services.AddEncinaBreachNotification(options =>
{
    options.EnforcementMode = BreachDetectionEnforcementMode.Warn;
    options.NotificationDeadlineHours = 72;
    options.EnableDeadlineMonitoring = true;
    options.SupervisoryAuthority = "dpa@supervisory-authority.eu";
    options.AddHealthCheck = true;
    options.AssembliesToScan.Add(typeof(Program).Assembly);
});
```

### 4. Handle and Notify

```csharp
var handler = serviceProvider.GetRequiredService<IBreachHandler>();

// Create formal breach record
var breachResult = await handler.HandleDetectedBreachAsync(potentialBreach);

// Notify authority within 72 hours (Art. 33)
await handler.NotifyAuthorityAsync(breachId);

// Notify data subjects if high risk (Art. 34)
await handler.NotifySubjectsAsync(breachId, affectedSubjectIds);

// Resolve the breach
await handler.ResolveBreachAsync(breachId, "Root cause patched, all users notified.");
```

---

## BreachMonitored Attribute

The `[BreachMonitored]` attribute marks request types for automatic security event generation:

```csharp
// Specify the type of security event to generate
[BreachMonitored(SecurityEventType.UnauthorizedAccess)]
public sealed record LoginAttemptCommand(string Username)
    : IRequest<Either<EncinaError, LoginResult>>;

[BreachMonitored(SecurityEventType.AnomalousQuery)]
public sealed record SearchPersonalDataQuery(string SearchTerm)
    : IRequest<Either<EncinaError, SearchResult>>;
```

| SecurityEventType | Description | Built-In Rule |
|-------------------|-------------|---------------|
| `UnauthorizedAccess` | Unauthorized access attempts (login failures, permission violations) | `UnauthorizedAccessRule` |
| `DataExfiltration` | Large-scale data access or download patterns | `MassDataExfiltrationRule` |
| `PrivilegeEscalation` | Unexpected elevation of user privileges | `PrivilegeEscalationRule` |
| `AnomalousQuery` | Unusual query patterns against personal data | `AnomalousQueryPatternRule` |

---

## Breach Lifecycle

Each breach progresses through a defined lifecycle tracked by `BreachStatus`:

```text
Detected → AuthorityNotified → SubjectsNotified → Resolved
    |              |                    |
    +→ (overdue)   +→ PhasedReports     +→ Exemption applied
```

| Status | Description |
|--------|-------------|
| `Detected` | Breach identified, formal record created, 72-hour clock started |
| `AuthorityNotified` | Supervisory authority notified per Article 33 |
| `SubjectsNotified` | Affected data subjects notified per Article 34 |
| `Resolved` | Breach closed with resolution summary per Article 33(3)(d) |

---

## Detection Engine

The `IBreachDetector` evaluates security events against all registered `IBreachDetectionRule` implementations:

```csharp
public interface IBreachDetector
{
    ValueTask<Either<EncinaError, IReadOnlyList<PotentialBreach>>> DetectAsync(
        SecurityEvent securityEvent,
        CancellationToken cancellationToken = default);
}
```

The `DefaultBreachDetector` iterates all registered rules and collects matches. Each rule returns `Option<PotentialBreach>` -- `Some` if the rule matches, `None` if it does not.

---

## Built-In Detection Rules

| Rule | Triggers On | Severity | Configurable Threshold |
|------|-------------|----------|------------------------|
| `UnauthorizedAccessRule` | `SecurityEventType.UnauthorizedAccess` | High | `UnauthorizedAccessThreshold` (default: 5) |
| `MassDataExfiltrationRule` | `SecurityEventType.DataExfiltration` | Critical | `DataExfiltrationThresholdMB` (default: 100 MB) |
| `PrivilegeEscalationRule` | `SecurityEventType.PrivilegeEscalation` | High | N/A (always triggers) |
| `AnomalousQueryPatternRule` | `SecurityEventType.AnomalousQuery` | Medium | `AnomalousQueryThreshold` (default: 1000) |

All built-in rules are registered via `TryAddEnumerable` and include recommended remediation actions in the `PotentialBreach` result.

---

## Custom Detection Rules

Implement `IBreachDetectionRule` for domain-specific detection:

```csharp
public sealed class OffHoursAccessRule : IBreachDetectionRule
{
    public string Name => "OffHoursAccess";
    public string Description => "Detects data access outside business hours";

    public ValueTask<Either<EncinaError, Option<PotentialBreach>>> EvaluateAsync(
        SecurityEvent securityEvent,
        CancellationToken cancellationToken = default)
    {
        var hour = securityEvent.OccurredAtUtc.Hour;
        if (hour < 6 || hour > 22)
        {
            var breach = PotentialBreach.Create(
                detectionRuleName: Name,
                severity: BreachSeverity.Medium,
                description: $"Access from '{securityEvent.Source}' at {hour}:00 UTC",
                securityEvent: securityEvent,
                recommendedActions: ["Verify user identity", "Review access logs"]);

            return new(Right<EncinaError, Option<PotentialBreach>>(breach));
        }

        return new(Right<EncinaError, Option<PotentialBreach>>(None));
    }
}

// Register via options
services.AddEncinaBreachNotification(options =>
{
    options.AddDetectionRule<OffHoursAccessRule>();
    options.AddDetectionRule<GeoLocationAnomalyRule>();
});
```

---

## Notification Workflow

### Authority Notification (Article 33)

```csharp
var handler = serviceProvider.GetRequiredService<IBreachHandler>();

// Step 1: Notify authority
var result = await handler.NotifyAuthorityAsync(breachId);

result.Match(
    Right: notification =>
    {
        Console.WriteLine($"Authority notified: {notification.Outcome}");
        Console.WriteLine($"Notified at: {notification.NotifiedAtUtc}");
    },
    Left: error => Console.WriteLine($"Failed: {error.Code} - {error.Message}"));
```

### Deadline Status

```csharp
var deadline = await handler.GetDeadlineStatusAsync(breachId);

deadline.Match(
    Right: status =>
    {
        Console.WriteLine($"Hours remaining: {status.HoursRemaining}");
        Console.WriteLine($"Is overdue: {status.IsOverdue}");
    },
    Left: error => Console.WriteLine($"Failed: {error.Message}"));
```

---

## Phased Reporting

Per Article 33(4), information may be provided in phases when full details are not immediately available:

```csharp
// Phase 1: Initial assessment
await handler.AddPhasedReportAsync(breachId,
    "Initial assessment: unauthorized access to customer database detected.",
    userId: "dpo@company.com");

// Phase 2: Impact analysis
await handler.AddPhasedReportAsync(breachId,
    "Impact assessment: approximately 1,500 customer records accessed. "
    + "Personal data includes names, emails, and phone numbers.",
    userId: "security@company.com");

// Phase 3: Remediation
await handler.AddPhasedReportAsync(breachId,
    "Remediation: access credentials rotated, vulnerability patched, "
    + "affected users notified via email.",
    userId: "cto@company.com");
```

Each phased report is automatically numbered and timestamped.

---

## Deadline Monitoring

The `BreachDeadlineMonitorService` runs periodic checks and publishes `DeadlineWarningNotification` events:

```csharp
services.AddEncinaBreachNotification(options =>
{
    options.EnableDeadlineMonitoring = true;
    options.DeadlineCheckInterval = TimeSpan.FromMinutes(15);
    options.AlertAtHoursRemaining = [48, 24, 12, 6, 1];
});
```

Subscribe to deadline warnings:

```csharp
public sealed class DeadlineAlertHandler
    : INotificationHandler<DeadlineWarningNotification>
{
    public Task Handle(DeadlineWarningNotification notification, CancellationToken ct)
    {
        // Send urgent alert to DPO, escalate via PagerDuty, etc.
        return Task.CompletedTask;
    }
}
```

---

## Data Subject Notification

Per Article 34(1), notify data subjects when the breach is likely to result in high risk:

```csharp
// Notify specific affected users
var result = await handler.NotifySubjectsAsync(breachId,
    subjectIds: ["user-001", "user-002", "user-003"]);
```

### Article 34(3) Exemptions

The `SubjectNotificationExemption` enum supports three exemption conditions:

| Exemption | Article | Description |
|-----------|---------|-------------|
| `EncryptionOrPseudonymization` | 34(3)(a) | Data rendered unintelligible to unauthorized persons |
| `RiskEliminated` | 34(3)(b) | Subsequent measures eliminated the high risk |
| `DisproportionateEffort` | 34(3)(c) | Individual notification requires disproportionate effort (public communication used instead) |

---

## Audit Trail

Every breach operation is recorded in an immutable audit trail for Article 33(5) compliance:

```csharp
var auditStore = serviceProvider.GetRequiredService<IBreachAuditStore>();

// Retrieve the full audit trail for a breach
var trail = await auditStore.GetByBreachIdAsync(breachId);
```

Typical audit actions: `BreachDetected`, `AuthorityNotified`, `SubjectsNotified`, `PhasedReportAdded`, `BreachResolved`, `DeadlineWarning`.

---

## Domain Notifications

The breach notification module publishes domain notifications at key lifecycle points:

| Notification | Trigger | Key Properties |
|-------------|---------|----------------|
| `BreachDetectedNotification` | New breach recorded | BreachId, Severity, DetectionRuleName |
| `AuthorityNotifiedNotification` | Authority notified per Art. 33 | BreachId, NotifiedAtUtc |
| `SubjectsNotifiedNotification` | Data subjects notified per Art. 34 | BreachId, SubjectCount |
| `DeadlineWarningNotification` | 72-hour deadline approaching | BreachId, HoursRemaining |
| `BreachResolvedNotification` | Breach closed | BreachId, ResolutionSummary |

Notifications can be disabled via `options.PublishNotifications = false`.

---

## Configuration Options

```csharp
services.AddEncinaBreachNotification(options =>
{
    // Pipeline enforcement mode
    options.EnforcementMode = BreachDetectionEnforcementMode.Warn;

    // 72-hour deadline (Art. 33(1))
    options.NotificationDeadlineHours = 72;

    // Alert thresholds
    options.AlertAtHoursRemaining = [48, 24, 12, 6, 1];

    // Authority contact
    options.SupervisoryAuthority = "dpa@authority.eu";

    // Automatic authority notification for high+ severity
    options.AutoNotifyOnHighSeverity = false;

    // Phased reporting (Art. 33(4))
    options.PhasedReportingEnabled = true;

    // Data subject notification threshold (Art. 34)
    options.SubjectNotificationSeverityThreshold = BreachSeverity.High;

    // Notifications and audit
    options.PublishNotifications = true;
    options.TrackAuditTrail = true;

    // Deadline monitoring
    options.EnableDeadlineMonitoring = true;
    options.DeadlineCheckInterval = TimeSpan.FromMinutes(15);

    // Health check
    options.AddHealthCheck = true;

    // Built-in rule thresholds
    options.UnauthorizedAccessThreshold = 5;
    options.DataExfiltrationThresholdMB = 100;
    options.AnomalousQueryThreshold = 1000;

    // Assembly scanning
    options.AssembliesToScan.Add(typeof(Program).Assembly);

    // Custom detection rules
    options.AddDetectionRule<MyCustomRule>();
});
```

---

## Enforcement Modes

| Mode | Pipeline Behavior | Use Case |
|------|-------------------|----------|
| `Block` | Returns error when breach detected | Production (GDPR Article 33 compliant) |
| `Warn` | Logs warning, allows response through | Migration/testing phase (default) |
| `Disabled` | Skips all breach detection entirely (no-op) | Development environments |

---

## Database Providers

The in-memory stores (`InMemoryBreachRecordStore`, `InMemoryBreachAuditStore`) are suitable for development and testing. For production, use a database-backed provider:

| Provider Category | Providers | Registration |
|-------------------|-----------|-------------|
| ADO.NET | SQL Server, PostgreSQL, MySQL | `config.UseBreachNotification = true` in `AddEncinaADO()` |
| Dapper | SQL Server, PostgreSQL, MySQL | `config.UseBreachNotification = true` in `AddEncinaDapper()` |
| EF Core | SQL Server, PostgreSQL, MySQL | `config.UseBreachNotification = true` in `AddEncinaEntityFrameworkCore()` |
| MongoDB | MongoDB | `config.UseBreachNotification = true` in `AddEncinaMongoDB()` |

Each provider registers `IBreachRecordStore` and `IBreachAuditStore` backed by the corresponding database.

---

## Observability

### OpenTelemetry Tracing

The module creates activities with the `Encina.Compliance.BreachNotification` ActivitySource:

| Activity | Tags |
|----------|------|
| `BreachNotification.Detection` | `breach.detection_rule`, `breach.outcome` |
| `BreachNotification.Notification` | `breach.notification_type`, `breach.id`, `breach.outcome` |
| `BreachNotification.Pipeline` | `breach.request_type`, `breach.outcome` |
| `BreachNotification.DeadlineCheck` | `breach.outcome` |

### Metrics

| Metric | Type | Description |
|--------|------|-------------|
| `breach.detected.total` | Counter | Total breaches detected (tagged by `breach.severity`, `breach.detection_rule`) |
| `breach.notification.authority.total` | Counter | Authority notification attempts (tagged by `breach.outcome`) |
| `breach.notification.subjects.total` | Counter | Data subject notification attempts (tagged by `breach.outcome`, `breach.subject_count`) |
| `breach.pipeline.executions.total` | Counter | Pipeline executions (tagged by `breach.outcome`) |
| `breach.phased_reports.total` | Counter | Phased reports submitted (tagged by `breach.id`) |
| `breach.resolved.total` | Counter | Breaches resolved (tagged by `breach.severity`) |
| `breach.time_to_notification.hours` | Histogram | Time from detection to authority notification (key Article 33(1) compliance metric) |
| `breach.detection.duration.ms` | Histogram | Detection rule evaluation duration |
| `breach.pipeline.duration.ms` | Histogram | Pipeline behavior execution duration |

### Structured Logging

Log events using `[LoggerMessage]` source generator for zero-allocation structured logging.

---

## Health Check

Opt-in via `options.AddHealthCheck = true`:

```csharp
services.AddEncinaBreachNotification(options =>
{
    options.AddHealthCheck = true;
});
```

The health check (`encina-breach-notification`) verifies:

- `BreachNotificationOptions` are configured
- `IBreachRecordStore` is resolvable from DI
- `IBreachAuditStore` is resolvable when `TrackAuditTrail` is enabled
- `IBreachDetector` is resolvable from DI
- `IBreachHandler` is resolvable from DI

Tags: `encina`, `gdpr`, `breach-notification`, `compliance`, `ready`.

---

## Error Handling

All operations return `Either<EncinaError, T>`:

```csharp
var result = await handler.NotifyAuthorityAsync(breachId);

result.Match(
    Right: notification =>
    {
        logger.LogInformation("Authority notified: {Outcome}", notification.Outcome);
    },
    Left: error =>
    {
        logger.LogError("Notification failed: {Code} - {Message}", error.Code, error.Message);
    }
);
```

### Error Codes

| Code | Description |
|------|-------------|
| `breach.not_found` | Breach record ID does not exist |
| `breach.already_exists` | Breach record ID already exists (duplicate) |
| `breach.already_resolved` | Action attempted on an already-resolved breach |
| `breach.detection_failed` | Detection engine failure |
| `breach.notification_failed` | Generic notification failure |
| `breach.authority_notification_failed` | Authority notification failed |
| `breach.subject_notification_failed` | Data subject notification failed |
| `breach.deadline_expired` | 72-hour deadline has passed |
| `breach.store_error` | Store persistence operation failed |
| `breach.invalid_parameter` | Invalid parameter provided |
| `breach.rule_evaluation_failed` | Detection rule evaluation failed |
| `breach.phased_report_failed` | Phased report submission failed |
| `breach.detected` | Pipeline blocked request due to breach detection |
| `breach.exemption_invalid` | Invalid Article 34(3) exemption |

---

## Best Practices

1. **Start with `Warn` mode, switch to `Block` when ready** -- `Warn` mode lets you observe detection behavior without blocking requests; switch to `Block` once rules are tuned
2. **Configure `SupervisoryAuthority` for production** -- set the authority contact before going live; the options validator warns if missing
3. **Enable deadline monitoring** -- set `EnableDeadlineMonitoring = true` and subscribe to `DeadlineWarningNotification` for proactive alerting
4. **Use phased reporting** -- don't wait for all information; submit initial reports within 72 hours and follow up with phased reports per Article 33(4)
5. **Track the audit trail** -- keep `TrackAuditTrail = true` in production for Article 33(5) compliance evidence
6. **Implement custom `IBreachNotifier`** -- the default notifier logs notifications; implement a real notifier that sends emails, calls APIs, or integrates with incident management tools
7. **Register database-backed stores for production** -- in-memory stores do not survive restarts
8. **Tune detection thresholds** -- adjust `UnauthorizedAccessThreshold`, `DataExfiltrationThresholdMB`, and `AnomalousQueryThreshold` to match your application's normal patterns
9. **Use `TimeProvider` for testable time-based logic** -- the deadline monitor and handler accept `TimeProvider` for deterministic testing
10. **Monitor the `breach.time_to_notification.hours` histogram** -- this is the key compliance metric for the 72-hour deadline

---

## Testing

### Unit Tests with In-Memory Stores

```csharp
var recordStore = new InMemoryBreachRecordStore(
    TimeProvider.System,
    NullLogger<InMemoryBreachRecordStore>.Instance);

var breach = BreachRecord.Create(
    potentialBreach,
    notificationDeadlineHours: 72,
    TimeProvider.System);

await recordStore.RecordBreachAsync(breach);

var result = await recordStore.GetBreachAsync(breach.Id);
result.IsRight.Should().BeTrue();
```

### Full Pipeline Test

```csharp
var services = new ServiceCollection();
services.AddEncina(c => c.RegisterServicesFromAssemblyContaining<TestCommand>());
services.AddEncinaBreachNotification(o =>
{
    o.EnforcementMode = BreachDetectionEnforcementMode.Block;
    o.EnableDeadlineMonitoring = false;
});

var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();

var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
var result = await encina.Send(new SensitiveDataQuery("test-user"));

// Verify breach was detected and blocked
result.IsLeft.Should().BeTrue();
```

---

## FAQ

**Q: How does the pipeline behavior decide which requests to monitor?**
The `BreachDetectionPipelineBehavior` checks for `[BreachMonitored]` attributes on the request type. Attribute presence is cached statically per closed generic type, so there is zero reflection overhead after the first resolution.

**Q: What happens if no `[BreachMonitored]` attribute is present on the request?**
The pipeline behavior skips all breach detection with zero overhead.

**Q: How are detection rules evaluated?**
The `DefaultBreachDetector` evaluates all registered `IBreachDetectionRule` implementations against the security event. Rules return `Option<PotentialBreach>` -- `Some` if the rule matches, `None` if it does not. All matching breaches are collected and returned.

**Q: Can I register custom implementations before calling `AddEncinaBreachNotification`?**
Yes. All service registrations use `TryAdd`, so existing registrations are preserved. Register your custom `IBreachRecordStore`, `IBreachNotifier`, or `IBreachHandler` before calling `AddEncinaBreachNotification()`.

**Q: What happens if the deadline monitoring service encounters an error?**
Individual monitoring cycle failures are logged but never crash the host. The service continues running and attempts monitoring again on the next cycle.

**Q: Can I use the module without the deadline monitoring service?**
Yes. Set `EnableDeadlineMonitoring = false` (default). You can manually check deadline status via `handler.GetDeadlineStatusAsync(breachId)`.

**Q: How does the 72-hour deadline work?**
The deadline is calculated from the security event's `OccurredAtUtc` timestamp (the moment of "awareness" per Article 33(1)). The configurable `NotificationDeadlineHours` defaults to 72. The `BreachDeadlineMonitorService` publishes `DeadlineWarningNotification` events at the configured thresholds.

**Q: What is the difference between InMemory and database stores?**
The in-memory stores use `ConcurrentDictionary` for storage. They are suitable for development and testing only. For production, register a database-backed provider that provides durable persistence.
