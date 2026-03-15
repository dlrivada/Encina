# Cross-Border Data Transfer Compliance in Encina

This guide explains how to enforce GDPR Chapter V (Articles 44-49) and Schrems II compliance for international personal data transfers using the `Encina.Compliance.CrossBorderTransfer` package. Transfer validation operates at the CQRS pipeline level, ensuring consistent enforcement across all entry points.

## Table of Contents

1. [Overview](#overview)
2. [The Problem](#the-problem)
3. [The Solution](#the-solution)
4. [Quick Start](#quick-start)
5. [Transfer Validation Attribute](#transfer-validation-attribute)
6. [Transfer Validation Chain](#transfer-validation-chain)
7. [Transfer Impact Assessments (TIA)](#transfer-impact-assessments-tia)
8. [Standard Contractual Clauses (SCC)](#standard-contractual-clauses-scc)
9. [Approved Transfers](#approved-transfers)
10. [Expiration Monitoring](#expiration-monitoring)
11. [Configuration Options](#configuration-options)
12. [Enforcement Modes](#enforcement-modes)
13. [Marten Integration](#marten-integration)
14. [Observability](#observability)
15. [Health Check](#health-check)
16. [Error Handling](#error-handling)
17. [Best Practices](#best-practices)
18. [Testing](#testing)
19. [FAQ](#faq)

---

## Overview

Encina.Compliance.CrossBorderTransfer provides pipeline-level enforcement of GDPR Chapter V requirements for international data transfers:

| Component | Description |
|-----------|-------------|
| **`[RequiresCrossBorderTransfer]`** | Declarative attribute marking requests that involve cross-border data transfers |
| **`TransferBlockingPipelineBehavior`** | Pipeline behavior that validates transfers and short-circuits on failure |
| **`ITransferValidator`** | Cascading validation chain: adequacy → approved transfer → SCC → TIA → derogation |
| **`ITIAService`** | Transfer Impact Assessment lifecycle management (Schrems II) |
| **`ISCCService`** | Standard Contractual Clauses agreement management (Art. 46) |
| **`IApprovedTransferService`** | Transfer authorization lifecycle with expiration tracking |
| **`TransferExpirationMonitor`** | Background service for proactive expiration alerting |

### Why Pipeline-Level Transfer Compliance?

| Benefit | Description |
|---------|-------------|
| **Automatic enforcement** | Transfers are validated before any handler executes |
| **Declarative** | Transfer requirements live with the request type, not scattered across services |
| **Cascading validation** | Multiple GDPR mechanisms are evaluated in priority order |
| **Proactive monitoring** | Background service alerts before transfers, TIAs, or SCCs expire |
| **Transport-agnostic** | Same compliance applies via HTTP, messaging, gRPC, or serverless |

---

## The Problem

GDPR Chapter V (Articles 44-49) restricts international personal data transfers. The Schrems II judgment (CJEU C-311/18) added Transfer Impact Assessment requirements. Organizations must verify legal mechanisms before each transfer:

```csharp
// Problem 1: Transfer compliance checked manually (if at all)
public async Task<UserData> SyncToUSAsync(Guid userId)
{
    // Who checks if this transfer has a valid legal basis?
    // Who verifies the TIA is current and the SCC agreement is active?
    return await _usService.ReplicateAsync(userId);
}

// Problem 2: Expired SCC agreements or TIAs go unnoticed
// until a supervisory authority audit
```

---

## The Solution

Encina enforces transfer compliance automatically in the CQRS pipeline:

```csharp
// Transfer requirements are declared on the request type
[RequiresCrossBorderTransfer(Destination = "US", DataCategory = "personal-data")]
public sealed record SyncUserToUSCommand(Guid UserId) : ICommand<Unit>;

// The pipeline behavior validates the transfer before the handler runs.
// If no valid legal basis exists (adequacy, SCC, TIA, etc.), the request
// is blocked with an EncinaError — the handler never executes.
```

---

## Quick Start

### 1. Install the Package

```bash
dotnet add package Encina.Compliance.CrossBorderTransfer
```

### 2. Register Services

```csharp
// In Program.cs or Startup.cs
services.AddEncinaCrossBorderTransfer(options =>
{
    options.EnforcementMode = CrossBorderTransferEnforcementMode.Block;
    options.DefaultSourceCountryCode = "DE"; // Germany
    options.TIARiskThreshold = 0.6;
    options.AddHealthCheck = true;
});

// Register Marten aggregates (required for event-sourced stores)
services.AddCrossBorderTransferAggregates();
```

### 3. Declare Transfer Requirements

```csharp
// Static destination
[RequiresCrossBorderTransfer(Destination = "US", DataCategory = "personal-data")]
public sealed record SyncToUSCommand(Guid UserId) : ICommand<Unit>;

// Dynamic destination from request property
[RequiresCrossBorderTransfer(
    DestinationProperty = "TargetCountry",
    DataCategory = "health-data")]
public sealed record TransferPatientRecordsCommand(
    string TargetCountry,
    Guid PatientId) : ICommand<Unit>;
```

### 4. Set Up Transfer Infrastructure

Before transfers are allowed, you need to establish the legal mechanisms:

```csharp
// 1. Create a TIA for the route
var tiaResult = await tiaService.CreateAsync(
    sourceCountryCode: "DE",
    destinationCountryCode: "US",
    dataCategory: "personal-data",
    assessor: "privacy-team@example.com");

// 2. Assess risk
await tiaService.AssessRiskAsync(tiaId, cancellationToken);

// 3. Submit for DPO review
await tiaService.SubmitForDPOReviewAsync(tiaId, "privacy-officer@example.com");

// 4. Complete DPO review
await tiaService.CompleteDPOReviewAsync(tiaId, approved: true, "DPO approved");

// 5. Register SCC agreement
var sccResult = await sccService.RegisterAsync(
    processorId: "us-processor-001",
    module: SCCModule.ControllerToProcessor,
    sourceCountryCode: "DE",
    destinationCountryCode: "US");

// 6. Approve the transfer
var transferResult = await approvedTransferService.ApproveTransferAsync(
    sourceCountryCode: "DE",
    destinationCountryCode: "US",
    dataCategory: "personal-data",
    basis: TransferBasis.StandardContractualClauses,
    sccAgreementId: sccId,
    tiaId: tiaId,
    approvedBy: "dpo@example.com");
```

---

## Transfer Validation Attribute

The `[RequiresCrossBorderTransfer]` attribute declares that a request involves an international data transfer:

| Property | Type | Description |
|----------|------|-------------|
| `Destination` | `string?` | Static ISO 3166-1 alpha-2 destination country code |
| `DestinationProperty` | `string?` | Request property name containing the destination country (cached reflection) |
| `SourceProperty` | `string?` | Request property name containing the source country |
| `DataCategory` | `string` | Category of personal data being transferred (default: `"personal-data"`) |

```csharp
// Static destination
[RequiresCrossBorderTransfer(Destination = "CN", DataCategory = "sensitive-data")]
public sealed record ReplicateToChinaCommand : ICommand<Unit>;

// Dynamic source and destination
[RequiresCrossBorderTransfer(
    SourceProperty = "FromCountry",
    DestinationProperty = "ToCountry",
    DataCategory = "financial-data")]
public sealed record ReplicateDataCommand(
    string FromCountry,
    string ToCountry) : ICommand<Unit>;
```

---

## Transfer Validation Chain

The `ITransferValidator` evaluates transfers through a cascading chain:

```
1. Adequacy Decision (Art. 45)
   ├── Country has EU adequacy decision? → ALLOWED (AdequacyDecision)
   └── No → continue

2. Approved Transfer Check
   ├── Active approved transfer for this route? → ALLOWED (basis from approval)
   └── No → continue

3. SCC Agreement Check (Art. 46)
   ├── Valid SCC agreement for this route? → ALLOWED (StandardContractualClauses)
   └── No → continue

4. TIA Check (Schrems II)
   ├── Completed TIA with acceptable risk? → ALLOWED (with conditions)
   └── No → continue

5. Derogation Evaluation (Art. 49)
   └── No derogation applies → BLOCKED
```

The first matching mechanism determines the `TransferBasis`. If none applies, the transfer is blocked.

---

## Transfer Impact Assessments (TIA)

TIAs are required by the Schrems II judgment for transfers based on SCCs or BCRs to countries without an adequacy decision.

### TIA Lifecycle

```
Draft → RiskAssessed → PendingDPOReview → Approved/Rejected
```

### Risk Assessment

The `ITIARiskAssessor` assigns risk scores based on destination country characteristics:

| Country Category | Risk Score Range | Examples |
|------------------|------------------|----------|
| EU/EEA | 0.0 - 0.1 | DE, FR, NL |
| Adequacy Decision | 0.1 - 0.3 | JP, NZ, KR |
| Partial Adequacy | 0.3 - 0.5 | CA (PIPEDA) |
| No Adequacy | 0.5 - 0.7 | US, IN, BR |
| High Surveillance | 0.7 - 0.9 | CN, RU |

### Supplementary Measures

When risk is above the threshold, supplementary measures can be added:

```csharp
await tiaService.RequireSupplementaryMeasureAsync(
    tiaId,
    SupplementaryMeasureType.Technical,
    "End-to-end encryption with customer-managed keys",
    DateTimeOffset.UtcNow);
```

Types: `Technical`, `Organizational`, `Contractual`.

---

## Standard Contractual Clauses (SCC)

SCC agreements (Art. 46(2)(c)) provide appropriate safeguards for transfers without an adequacy decision.

### SCC Module Types

| Module | Parties | Use Case |
|--------|---------|----------|
| `ControllerToController` | Controller → Controller | Sharing with partner organizations |
| `ControllerToProcessor` | Controller → Processor | Outsourcing to service providers |
| `ProcessorToProcessor` | Processor → Processor | Sub-processing chains |
| `ProcessorToSubProcessor` | Processor → Sub-Processor | Sub-processor agreements |

```csharp
var result = await sccService.RegisterAsync(
    processorId: "cloud-provider-001",
    module: SCCModule.ControllerToProcessor,
    sourceCountryCode: "DE",
    destinationCountryCode: "US",
    tenantId: "tenant-001");
```

---

## Approved Transfers

Approved transfers represent explicit authorization for a specific data transfer route:

```csharp
// Approve a transfer
var result = await approvedTransferService.ApproveTransferAsync(
    sourceCountryCode: "DE",
    destinationCountryCode: "US",
    dataCategory: "personal-data",
    basis: TransferBasis.StandardContractualClauses,
    sccAgreementId: sccId,
    tiaId: tiaId,
    approvedBy: "dpo@example.com",
    expiresAtUtc: DateTimeOffset.UtcNow.AddYears(1));

// Check if a transfer is approved
var isApproved = await approvedTransferService.IsTransferApprovedAsync(
    "DE", "US", "personal-data");

// Renew before expiration
await approvedTransferService.RenewTransferAsync(
    transferId, DateTimeOffset.UtcNow.AddYears(1), "dpo@example.com");

// Revoke if circumstances change
await approvedTransferService.RevokeTransferAsync(
    transferId, "dpo@example.com", "Legal basis no longer valid");
```

---

## Expiration Monitoring

The `TransferExpirationMonitor` is a `BackgroundService` that proactively checks for expiring or expired transfers, TIAs, and SCC agreements:

```csharp
services.AddEncinaCrossBorderTransfer(options =>
{
    options.EnableExpirationMonitoring = true;
    options.ExpirationCheckInterval = TimeSpan.FromHours(1);
    options.AlertBeforeExpirationDays = 30;
    options.PublishExpirationNotifications = true;
});
```

### Notification Events

| Event | When Published |
|-------|---------------|
| `TransferExpiringNotification` | Transfer approaching expiration |
| `TransferExpiredNotification` | Transfer has expired |
| `TIAExpiringNotification` | TIA approaching expiration |
| `TIAExpiredNotification` | TIA has expired |
| `SCCAgreementExpiringNotification` | SCC agreement approaching expiration |
| `SCCAgreementExpiredNotification` | SCC agreement has expired |

### Handling Notifications

```csharp
public sealed class TransferExpirationHandler
    : INotificationHandler<TransferExpiringNotification>
{
    public async Task Handle(
        TransferExpiringNotification notification,
        CancellationToken cancellationToken)
    {
        // Send email to DPO, update dashboard, trigger renewal workflow
        await _emailService.SendAsync(
            to: "dpo@example.com",
            subject: $"Transfer {notification.TransferId} expires in {notification.DaysUntilExpiration} days",
            body: $"Route: {notification.SourceCountryCode} → {notification.DestinationCountryCode}");
    }
}
```

---

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `EnforcementMode` | `CrossBorderTransferEnforcementMode` | `Block` | Enforcement mode |
| `DefaultSourceCountryCode` | `string` | `"DE"` | Default source country (ISO 3166-1 alpha-2) |
| `TIARiskThreshold` | `double` | `0.6` | Risk threshold for TIA assessments (0.0-1.0) |
| `DefaultTIAExpirationDays` | `int?` | `365` | TIA expiration (null = no expiry) |
| `DefaultSCCExpirationDays` | `int?` | `null` | SCC agreement expiration |
| `DefaultTransferExpirationDays` | `int?` | `365` | Transfer authorization expiration |
| `AutoDetectTransfers` | `bool` | `false` | Auto-detect cross-border transfers |
| `CacheEnabled` | `bool` | `true` | Cache validation results |
| `CacheTTLMinutes` | `int` | `5` | Cache TTL in minutes |
| `AddHealthCheck` | `bool` | `false` | Register health check |
| `RequireTIAForNonAdequate` | `bool` | `true` | Require TIA for non-adequate countries |
| `RequireSCCForNonAdequate` | `bool` | `true` | Require SCC for non-adequate countries |
| `EnableExpirationMonitoring` | `bool` | `false` | Enable background expiration monitor |
| `ExpirationCheckInterval` | `TimeSpan` | `1 hour` | Interval between expiration checks |
| `AlertBeforeExpirationDays` | `int` | `30` | Days before expiration to start alerting |
| `PublishExpirationNotifications` | `bool` | `true` | Publish expiration notifications via IEncina |

---

## Enforcement Modes

| Mode | Behavior |
|------|----------|
| `Block` | Non-compliant transfers are blocked with `EncinaError` (default) |
| `Warn` | Non-compliant transfers are logged as warnings but allowed to proceed |
| `Disabled` | No enforcement — useful for development environments |

```csharp
// Production
options.EnforcementMode = CrossBorderTransferEnforcementMode.Block;

// Migration / staging
options.EnforcementMode = CrossBorderTransferEnforcementMode.Warn;

// Development
options.EnforcementMode = CrossBorderTransferEnforcementMode.Disabled;
```

---

## Marten Integration

This package uses Marten for event-sourced aggregates. You must configure Marten and register the aggregates:

```csharp
// 1. Configure Marten (PostgreSQL event store)
services.AddMarten(options =>
{
    options.Connection(connectionString);
});

// 2. Register cross-border transfer aggregates
services.AddCrossBorderTransferAggregates();

// 3. Register cross-border transfer services
services.AddEncinaCrossBorderTransfer(options =>
{
    options.EnforcementMode = CrossBorderTransferEnforcementMode.Block;
});
```

The three event-sourced aggregates are:

- `TIAAggregate` — Transfer Impact Assessment lifecycle
- `SCCAgreementAggregate` — SCC agreement lifecycle
- `ApprovedTransferAggregate` — Transfer authorization lifecycle

### Automatic Event Publishing

All event-sourced events implement `INotification`, which means they are **automatically published** by `EventPublishingPipelineBehavior` after successful command execution. You can subscribe to aggregate state changes directly:

```csharp
// React to a TIA being completed — no polling or separate notification needed
public sealed class TIACompletedHandler : INotificationHandler<TIACompleted>
{
    public async Task<Either<EncinaError, Unit>> Handle(
        TIACompleted notification,
        CancellationToken cancellationToken)
    {
        // Update dashboard, notify compliance team, trigger next workflow step
        await _complianceService.OnTIACompletedAsync(notification.TIAId);
        return Unit.Default;
    }
}

// React to a transfer being revoked
public sealed class TransferRevokedHandler : INotificationHandler<TransferRevoked>
{
    public async Task<Either<EncinaError, Unit>> Handle(
        TransferRevoked notification,
        CancellationToken cancellationToken)
    {
        // Block data flows, alert DPO, log compliance gap
        await _dataFlowService.SuspendRouteAsync(notification.TransferId);
        return Unit.Default;
    }
}
```

This unifies the event model: event-sourced events serve both as the aggregate's state-change mechanism and as publishable notifications. The separate `Notifications/` events (`TransferExpiringNotification`, etc.) remain for monitoring-specific concerns that originate outside the aggregate lifecycle.

---

## Observability

### OpenTelemetry Tracing

Activity source: `Encina.Compliance.CrossBorderTransfer`

| Tag | Description |
|-----|-------------|
| `crossborder.source` | Source country code |
| `crossborder.destination` | Destination country code |
| `crossborder.data_category` | Data category |
| `crossborder.outcome` | Validation outcome (passed/blocked/warned/skipped) |
| `crossborder.basis` | Legal basis (if allowed) |
| `crossborder.request_type` | Request type name |
| `crossborder.failure_reason` | Reason for blocking (if blocked) |
| `crossborder.enforcement_mode` | Current enforcement mode |

### Metrics

Meter: `Encina.Compliance.CrossBorderTransfer`

| Metric | Type | Description |
|--------|------|-------------|
| `crossborder.checks.total` | Counter | Total compliance evaluations |
| `crossborder.checks.passed` | Counter | Evaluations that allowed the transfer |
| `crossborder.checks.blocked` | Counter | Evaluations that blocked the transfer |
| `crossborder.checks.warned` | Counter | Evaluations that warned but allowed |
| `crossborder.checks.skipped` | Counter | Requests without the attribute |
| `crossborder.check.duration` | Histogram (ms) | Evaluation duration |
| `crossborder.tia.created` | Counter | TIAs created |
| `crossborder.tia.completed` | Counter | TIAs completed |
| `crossborder.scc.registered` | Counter | SCC agreements registered |
| `crossborder.scc.revoked` | Counter | SCC agreements revoked |
| `crossborder.transfer.approved` | Counter | Transfers approved |
| `crossborder.transfer.revoked` | Counter | Transfers revoked |

### Structured Logging

26 log events using `LoggerMessage.Define` (zero-allocation), EventId range 8500-8555.

---

## Health Check

```csharp
options.AddHealthCheck = true;
```

Verifies all cross-border transfer services are registered and resolvable. Tags: `encina`, `compliance`, `cross-border-transfer`, `ready`.

---

## Error Handling

All operations return `Either<EncinaError, T>` following Railway Oriented Programming:

```csharp
var result = await tiaService.CreateAsync("DE", "US", "personal-data", "assessor");

result.Match(
    Right: tiaId => Console.WriteLine($"TIA created: {tiaId}"),
    Left: error => Console.WriteLine($"Error [{error.Code}]: {error.Message}"));
```

Error codes follow the `crossborder.*` convention. See `CrossBorderTransferErrors` for all error factory methods.

---

## Best Practices

1. **Set up TIAs first** — Create and complete TIAs before establishing SCC agreements or approving transfers
2. **Enable expiration monitoring** — Proactive alerts prevent compliance gaps from expired mechanisms
3. **Use `Block` mode in production** — `Warn` mode is for migration periods only
4. **Register custom `ITIARiskAssessor`** — Override the default to match your organization's risk methodology
5. **Configure `DefaultSourceCountryCode`** — Set to your primary data center location
6. **Review TIAs periodically** — The legal landscape of destination countries changes; TIAs should be reassessed
7. **Use `AddCrossBorderTransferAggregates()`** — Required for Marten event store integration

---

## Testing

Use `Encina.Testing.Fakes` for unit testing transfer compliance:

```csharp
// Register fakes for testing
services.AddScoped<ITIAService, FakeTIAService>();
services.AddScoped<ISCCService, FakeSCCService>();
services.AddScoped<IApprovedTransferService, FakeApprovedTransferService>();
services.AddScoped<ITransferValidator, FakeTransferValidator>();

// Or override the risk assessor
services.AddScoped<ITIARiskAssessor, TestRiskAssessor>();
services.AddEncinaCrossBorderTransfer(options =>
{
    options.EnforcementMode = CrossBorderTransferEnforcementMode.Disabled;
});
```

---

## FAQ

### Do I need Marten/PostgreSQL?

Yes. The three aggregates (`TIAAggregate`, `SCCAgreementAggregate`, `ApprovedTransferAggregate`) are event-sourced and require Marten as the event store. Marten requires PostgreSQL.

### What countries have EU adequacy decisions?

The validator includes built-in knowledge of countries with adequacy decisions (e.g., Japan, New Zealand, South Korea, UK, Switzerland, Canada, Israel, Argentina, Uruguay). This list is updated to reflect current EU Commission decisions.

### Can I customize the risk scoring?

Yes. Register your own `ITIARiskAssessor` implementation before calling `AddEncinaCrossBorderTransfer()`. Your implementation will be used instead of the default.

### How does caching work?

When `CacheEnabled = true`, validation results are cached per route (source + destination + data category) for `CacheTTLMinutes`. Cache is invalidated when transfers, TIAs, or SCC agreements are modified.

### What happens when a transfer expires?

If `EnableExpirationMonitoring = true`, the background service publishes `TransferExpiredNotification`. The pipeline behavior will block new requests for that route until a new transfer is approved or the existing one is renewed.
