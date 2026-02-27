# Data Subject Rights in Encina

This guide explains how to manage GDPR Data Subject Rights (Articles 15-22) declaratively at the CQRS pipeline level using the `Encina.Compliance.DataSubjectRights` package. Processing restriction enforcement operates independently of the transport layer, ensuring consistent Article 18 compliance across all entry points.

## Table of Contents

1. [Overview](#overview)
2. [The Problem](#the-problem)
3. [The Solution](#the-solution)
4. [Quick Start](#quick-start)
5. [PersonalData Attribute](#personaldata-attribute)
6. [RestrictProcessing Attribute](#restrictprocessing-attribute)
7. [DSR Request Lifecycle](#dsr-request-lifecycle)
8. [Data Access (Article 15)](#data-access-article-15)
9. [Data Erasure (Article 17)](#data-erasure-article-17)
10. [Data Portability (Article 20)](#data-portability-article-20)
11. [Processing Restriction (Article 18)](#processing-restriction-article-18)
12. [Audit Trail](#audit-trail)
13. [Domain Notifications](#domain-notifications)
14. [Configuration Options](#configuration-options)
15. [Enforcement Modes](#enforcement-modes)
16. [Database Providers](#database-providers)
17. [Observability](#observability)
18. [Health Check](#health-check)
19. [Error Handling](#error-handling)
20. [Best Practices](#best-practices)
21. [Testing](#testing)
22. [FAQ](#faq)

---

## Overview

Encina.Compliance.DataSubjectRights provides attribute-based personal data discovery and processing restriction enforcement at the CQRS pipeline level:

| Component | Description |
|-----------|-------------|
| **`[PersonalData]` Attribute** | Declarative personal data field marking on entity properties |
| **`[RestrictProcessing]` Attribute** | Declarative processing restriction check on request types |
| **`ProcessingRestrictionPipelineBehavior`** | Pipeline behavior that blocks/warns when a restricted subject is targeted |
| **`IDataSubjectRightsHandler`** | Central orchestrator for access, rectification, erasure, restriction, portability, and objection |
| **`IDSRRequestStore`** | Full DSR request lifecycle management (create, track, complete, extend) |
| **`IDSRAuditStore`** | Immutable audit trail for all DSR actions |
| **`IPersonalDataLocator`** | Automatic personal data discovery across entities |
| **`IDataErasureExecutor`** | Field-level erasure with Article 17(3) exemption support |
| **`IDataPortabilityExporter`** | JSON/CSV/XML data export for Article 20 portability |
| **`DataSubjectRightsOptions`** | Configuration for enforcement mode, deadlines, auto-registration |

### Why Pipeline-Level DSR?

| Benefit | Description |
|---------|-------------|
| **Automatic enforcement** | Processing restrictions are checked whenever a request processes personal data |
| **Declarative** | Personal data metadata and restriction requirements live with the types, not scattered across controllers |
| **Transport-agnostic** | Same restriction enforcement for HTTP, message queue, gRPC, and serverless |
| **Auditable** | Every DSR action is recorded with timestamps, actors, and compliance metadata |
| **Deadline-aware** | 30-day deadlines are tracked automatically with overdue monitoring |

---

## The Problem

GDPR Articles 15-22 grant data subjects rights over their personal data. Organizations receive Subject Access Requests (SARs), erasure requests ("right to be forgotten"), data portability requests, and processing restriction requests. Without a framework, applications typically struggle with:

- **Ad-hoc implementations** per team, leading to inconsistent handling of DSR requests
- **Missed deadlines** (30 days per GDPR Article 12(3)), with no systematic tracking or alerting
- **No audit trail** to demonstrate compliance with the accountability principle (Article 5(2))
- **Incomplete data discovery** when personal data is scattered across multiple entities and databases
- **Inconsistent restriction enforcement** where some code paths process restricted data while others block
- **No exemption handling** for Article 17(3) scenarios (legal retention, public health, legal claims)
- **Manual portability exports** requiring developers to build one-off scripts for each data subject request

---

## The Solution

Encina solves this with a unified DSR pipeline covering the full request lifecycle:

```text
DSR Request → [IDataSubjectRightsHandler] → Fulfillment
                       |
                       +-- Access (Art. 15): IPersonalDataLocator → data inventory
                       +-- Rectification (Art. 16): field-level update + notification
                       +-- Erasure (Art. 17): IDataErasureExecutor → exemption-aware erasure
                       +-- Restriction (Art. 18): IDSRRequestStore → active restriction flag
                       +-- Portability (Art. 20): IDataPortabilityExporter → JSON/CSV/XML
                       +-- Objection (Art. 21): processing purpose evaluation

Request → [ProcessingRestrictionPipelineBehavior] → Handler
                       |
                       +-- No personal data attributes? → Skip (zero overhead)
                       +-- Disabled mode? → Skip
                       +-- Extract SubjectId via cached reflection
                       +-- Check IDSRRequestStore.HasActiveRestrictionAsync
                       |   +-- No restriction → Proceed to handler
                       |   +-- Restriction + Block mode → Return DSRErrors.RestrictionActive
                       |   +-- Restriction + Warn mode → Log warning, proceed
                       +-- Store error → Fail-open (log, proceed)
```

---

## Quick Start

### 1. Install the Package

```bash
dotnet add package Encina.Compliance.DataSubjectRights
```

### 2. Mark Entity Properties with Personal Data Metadata

```csharp
public class Customer
{
    public Guid Id { get; set; }

    [PersonalData(Category = PersonalDataCategory.Identity)]
    public string FullName { get; set; } = string.Empty;

    [PersonalData(Category = PersonalDataCategory.Contact)]
    public string Email { get; set; } = string.Empty;

    [PersonalData(Category = PersonalDataCategory.Financial, LegalRetention = true,
        RetentionReason = "Tax records must be retained for 7 years per local law")]
    public string TaxId { get; set; } = string.Empty;
}
```

### 3. Register Services

```csharp
services.AddEncina(config =>
    config.RegisterServicesFromAssemblyContaining<Program>());

services.AddEncinaDataSubjectRights(options =>
{
    options.RestrictionEnforcementMode = DSREnforcementMode.Block;
    options.DefaultDeadlineDays = 30;
    options.AddHealthCheck = true;
    options.AssembliesToScan.Add(typeof(Program).Assembly);
});
```

### 4. Submit a DSR Request

```csharp
var handler = serviceProvider.GetRequiredService<IDataSubjectRightsHandler>();

// Access request (Article 15)
var accessResult = await handler.HandleAccessAsync(
    new AccessRequest("subject-123", IncludeProcessingActivities: true));

accessResult.Match(
    Right: response => Console.WriteLine($"Found {response.Data.Count} data locations"),
    Left: error => Console.WriteLine($"Access failed: {error.Message}"));
```

### 5. Decorate Requests for Restriction Enforcement

```csharp
[RestrictProcessing(SubjectIdProperty = nameof(CustomerId))]
public record UpdateCustomerProfileCommand(string CustomerId, string NewEmail) : ICommand;

// No attribute: pipeline skips restriction checks entirely
public record GetPublicCatalogQuery() : IQuery<CatalogDto>;
```

### 6. Execute Erasure or Export

```csharp
// Erasure (Article 17)
var erasureResult = await handler.HandleErasureAsync(
    new ErasureRequest("subject-123", ErasureReason.ConsentWithdrawn, Scope: null));

// Portability export (Article 20)
var exportResult = await handler.HandlePortabilityAsync(
    new PortabilityRequest("subject-123", ExportFormat.JSON, Categories: null));
```

---

## PersonalData Attribute

The `[PersonalData]` attribute marks entity properties as containing personal data subject to GDPR rights:

```csharp
public class Customer
{
    public Guid Id { get; set; }

    [PersonalData(Category = PersonalDataCategory.Identity)]
    public string FullName { get; set; } = string.Empty;

    [PersonalData(Category = PersonalDataCategory.Contact)]
    public string Email { get; set; } = string.Empty;

    [PersonalData(Category = PersonalDataCategory.Financial,
        LegalRetention = true,
        RetentionReason = "Tax records retained per Directive 2006/112/EC")]
    public string TaxId { get; set; } = string.Empty;

    [PersonalData(Category = PersonalDataCategory.Health,
        Erasable = true,
        Portable = false)]
    public string BloodType { get; set; } = string.Empty;
}
```

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Category` | `PersonalDataCategory` | `Other` | Classification for scoped operations (Identity, Contact, Financial, Health, etc.) |
| `Erasable` | `bool` | `true` | Whether the field can be erased under Article 17 |
| `Portable` | `bool` | `true` | Whether the field is included in portability exports (Article 20) |
| `LegalRetention` | `bool` | `false` | Whether legal retention prevents erasure (Article 17(3)) |
| `RetentionReason` | `string?` | `null` | Legal basis for retention (documented in erasure results) |

### Personal Data Categories

| Category | Description |
|----------|-------------|
| `Identity` | Name, date of birth, national ID, passport number |
| `Contact` | Email address, phone number, postal address |
| `Financial` | Bank account, payment information, salary, tax records |
| `Health` | Medical records, health conditions (Article 9 special category) |
| `Biometric` | Fingerprints, facial recognition (Article 9 when used for identification) |
| `Genetic` | DNA sequences, genetic test results (Article 9) |
| `Location` | GPS coordinates, travel history, geofencing data |
| `Online` | IP addresses, cookies, browsing history, device IDs (Recital 30) |
| `Employment` | Job title, employment history, performance reviews |
| `Education` | Academic records, qualifications, training history |
| `Other` | Data not fitting predefined categories |

---

## RestrictProcessing Attribute

The `[RestrictProcessing]` attribute declares that a request should be subject to processing restriction checks (Article 18):

```csharp
// With explicit subject ID property
[RestrictProcessing(SubjectIdProperty = nameof(CustomerId))]
public record UpdateCustomerProfileCommand(string CustomerId, string NewEmail) : ICommand;

// Using default subject ID extraction (via IDataSubjectIdExtractor)
[RestrictProcessing]
public record SendMarketingEmailCommand(string SubjectId) : ICommand;
```

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `SubjectIdProperty` | `string?` | No | Property name on the request containing the subject ID |

The pipeline behavior also detects `[ProcessesPersonalData]` and `[ProcessingActivity]` attributes from the GDPR module, triggering restriction checks when any personal data attribute is present.

---

## DSR Request Lifecycle

Each DSR request progresses through a defined lifecycle tracked by `DSRRequestStatus`:

```text
Received → IdentityVerified → InProgress → Completed
                                         → Rejected
         → Extended (deadline extension)
         → Expired (deadline exceeded)
```

| Status | Description |
|--------|-------------|
| `Received` | Request received; 30-day clock starts (Article 12(3)) |
| `IdentityVerified` | Subject identity confirmed (Article 12(6)) |
| `InProgress` | Request actively being processed |
| `Completed` | Request fulfilled successfully |
| `Rejected` | Request rejected with stated reason (Article 12(4)) |
| `Extended` | Deadline extended by up to 2 additional months (Article 12(3)) |
| `Expired` | Deadline passed without completion (potential compliance violation) |

### Creating a DSR Request

```csharp
var store = serviceProvider.GetRequiredService<IDSRRequestStore>();

var request = DSRRequest.Create(
    id: "req-001",
    subjectId: "subject-123",
    rightType: DataSubjectRight.Erasure,
    receivedAtUtc: DateTimeOffset.UtcNow,
    requestDetails: "Customer requests deletion of all marketing data");

await store.CreateAsync(request);
```

### Updating Status

```csharp
// Verify identity
await store.UpdateStatusAsync("req-001", DSRRequestStatus.IdentityVerified, reason: null);

// Begin processing
await store.UpdateStatusAsync("req-001", DSRRequestStatus.InProgress, reason: null);

// Complete
await store.UpdateStatusAsync("req-001", DSRRequestStatus.Completed, reason: null);

// Or reject
await store.UpdateStatusAsync("req-001", DSRRequestStatus.Rejected,
    reason: "Compelling legitimate grounds override subject's interests (Article 21(1))");
```

### Monitoring Deadlines

```csharp
// Get all overdue requests
var overdue = await store.GetOverdueRequestsAsync();

// Get all pending (active) requests
var pending = await store.GetPendingRequestsAsync();

// Get request history for a subject
var history = await store.GetBySubjectIdAsync("subject-123");
```

---

## Data Access (Article 15)

The `IDataSubjectRightsHandler.HandleAccessAsync` method fulfills Subject Access Requests:

```csharp
var handler = serviceProvider.GetRequiredService<IDataSubjectRightsHandler>();

var result = await handler.HandleAccessAsync(
    new AccessRequest("subject-123", IncludeProcessingActivities: true));

result.Match(
    Right: response =>
    {
        foreach (var location in response.Data)
        {
            Console.WriteLine($"{location.EntityType.Name}.{location.FieldName}: {location.CurrentValue}");
        }
    },
    Left: error => Console.WriteLine($"Access request failed: {error.Code}"));
```

The handler uses `IPersonalDataLocator` to discover all personal data fields decorated with `[PersonalData]` and returns their locations and current values.

---

## Data Erasure (Article 17)

The erasure workflow respects legal retention requirements and Article 17(3) exemptions:

```csharp
// Full erasure of all erasable data
var result = await handler.HandleErasureAsync(
    new ErasureRequest("subject-123", ErasureReason.ConsentWithdrawn, Scope: null));

// Scoped erasure targeting specific categories
var result = await handler.HandleErasureAsync(
    new ErasureRequest("subject-123", ErasureReason.NoLongerNecessary,
        new ErasureScope
        {
            Reason = ErasureReason.NoLongerNecessary,
            Categories = [PersonalDataCategory.Contact, PersonalDataCategory.Identity]
        }));

result.Match(
    Right: r => Console.WriteLine(
        $"Erased: {r.FieldsErased}, Retained: {r.FieldsRetained}, Failed: {r.FieldsFailed}"),
    Left: error => Console.WriteLine($"Erasure failed: {error.Code}"));
```

### Erasure Reasons (Article 17(1))

| Reason | Article | Description |
|--------|---------|-------------|
| `NoLongerNecessary` | 17(1)(a) | Data no longer needed for original purpose |
| `ConsentWithdrawn` | 17(1)(b) | Consent withdrawn, no other legal basis |
| `ObjectionToProcessing` | 17(1)(c) | Subject objects under Article 21 |
| `UnlawfulProcessing` | 17(1)(d) | Data processed unlawfully |
| `LegalObligation` | 17(1)(e) | Legal obligation requires erasure |
| `ChildData` | 17(1)(f) | Data collected from a child (Article 8(1)) |

### Erasure Exemptions (Article 17(3))

| Exemption | Article | Description |
|-----------|---------|-------------|
| `FreedomOfExpression` | 17(3)(a) | Freedom of expression and information |
| `LegalObligation` | 17(3)(b) | Compliance with legal obligation or public task |
| `PublicHealth` | 17(3)(c) | Public health reasons (Article 9(2)(h)(i)) |
| `Archiving` | 17(3)(d) | Archiving, scientific research, statistics (Article 89(1)) |
| `LegalClaims` | 17(3)(e) | Establishment, exercise, or defence of legal claims |

### Custom Erasure Strategies

Register a custom `IDataErasureStrategy` to control how fields are erased:

```csharp
public class AnonymizationErasureStrategy : IDataErasureStrategy
{
    public async ValueTask<Either<EncinaError, Unit>> EraseFieldAsync(
        PersonalDataLocation location, CancellationToken cancellationToken)
    {
        // Replace with anonymized values instead of hard delete
        var anonymized = location.FieldName switch
        {
            "Email" => "anonymized@example.com",
            "FullName" => "REDACTED",
            _ => null
        };
        // Apply anonymized value to the data store
        return Unit.Default;
    }
}

// Register before AddEncinaDataSubjectRights (TryAdd respects existing registrations)
services.AddSingleton<IDataErasureStrategy, AnonymizationErasureStrategy>();
services.AddEncinaDataSubjectRights();
```

---

## Data Portability (Article 20)

The portability exporter generates structured, machine-readable exports in JSON, CSV, or XML:

```csharp
// Export all portable data as JSON
var result = await handler.HandlePortabilityAsync(
    new PortabilityRequest("subject-123", ExportFormat.JSON, Categories: null));

// Export specific categories as CSV
var result = await handler.HandlePortabilityAsync(
    new PortabilityRequest("subject-123", ExportFormat.CSV,
        Categories: [PersonalDataCategory.Contact, PersonalDataCategory.Identity]));

result.Match(
    Right: response =>
    {
        var exported = response.ExportedData;
        File.WriteAllBytes(exported.FileName, exported.Content);
        Console.WriteLine($"Exported {exported.FieldCount} fields as {exported.ContentType}");
    },
    Left: error => Console.WriteLine($"Export failed: {error.Code}"));
```

### Supported Export Formats

| Format | Content Type | Description |
|--------|-------------|-------------|
| `JSON` | `application/json` | Structured, widely supported, machine-readable |
| `CSV` | `text/csv` | Tabular format, RFC 4180 compliant |
| `XML` | `application/xml` | Structured with schema support |

Only fields marked with `Portable = true` in the `[PersonalData]` attribute are included in portability exports.

---

## Processing Restriction (Article 18)

The `ProcessingRestrictionPipelineBehavior` checks whether the target data subject has an active restriction before allowing requests to proceed:

```csharp
// This command is blocked when subject-123 has an active restriction
[RestrictProcessing(SubjectIdProperty = nameof(CustomerId))]
public record UpdateCustomerProfileCommand(string CustomerId, string NewEmail) : ICommand;

var encina = serviceProvider.GetRequiredService<IEncina>();
var result = await encina.Send(new UpdateCustomerProfileCommand("subject-123", "new@email.com"));

result.Match(
    Right: _ => Console.WriteLine("Profile updated"),
    Left: error => Console.WriteLine($"Blocked: {error.Code}")  // "dsr.restriction_active"
);
```

### How Restriction Checking Works

1. The behavior checks for `[RestrictProcessing]`, `[ProcessesPersonalData]`, or `[ProcessingActivity]` attributes on the request type (cached per generic type, resolved once)
2. If no personal data attribute is present, the check is skipped with zero overhead
3. The subject ID is extracted via `SubjectIdProperty` reflection or `IDataSubjectIdExtractor` fallback
4. `IDSRRequestStore.HasActiveRestrictionAsync` is called to check for active restriction requests
5. Based on the enforcement mode, the request is blocked, warned, or allowed

### Applying a Restriction

```csharp
// Submit a restriction request through the handler
var result = await handler.HandleRestrictionAsync(
    new RestrictionRequest("subject-123", "Accuracy of data contested"));
```

---

## Audit Trail

Every DSR action is recorded in an immutable audit trail for compliance evidence:

```csharp
var auditStore = serviceProvider.GetRequiredService<IDSRAuditStore>();

// Record an audit entry
var entry = new DSRAuditEntry
{
    Id = Guid.NewGuid().ToString(),
    DSRRequestId = "req-001",
    Action = "ErasureExecuted",
    Detail = "Erased 12 fields across 3 entities",
    PerformedByUserId = "admin-456",
    OccurredAtUtc = DateTimeOffset.UtcNow
};

await auditStore.RecordAsync(entry);

// Retrieve the audit trail for a request
var trail = await auditStore.GetAuditTrailAsync("req-001");
```

Typical audit actions: `RequestReceived`, `IdentityVerified`, `ErasureExecuted`, `ThirdPartyNotified`, `RequestCompleted`, `RequestRejected`, `DeadlineExtended`.

---

## Domain Notifications

The DSR module publishes domain notifications via `INotification` at key lifecycle points:

| Notification | Trigger | Properties |
|-------------|---------|------------|
| `DataErasedNotification` | Personal data erased (Art. 17) | SubjectId, AffectedFields, DSRRequestId, OccurredAtUtc |
| `DataRectifiedNotification` | Data corrected (Art. 16) | SubjectId, FieldName, DSRRequestId, OccurredAtUtc |
| `ProcessingRestrictedNotification` | Restriction applied (Art. 18) | SubjectId, DSRRequestId, OccurredAtUtc |
| `RestrictionLiftedNotification` | Restriction lifted (Art. 18(3)) | SubjectId, DSRRequestId, OccurredAtUtc |

Subscribe to notifications using standard Encina notification handlers:

```csharp
public sealed class ErasureNotificationHandler : INotificationHandler<DataErasedNotification>
{
    public Task Handle(DataErasedNotification notification, CancellationToken cancellationToken)
    {
        // Propagate erasure to downstream systems and third-party processors (Article 19)
        return Task.CompletedTask;
    }
}
```

Notifications can be disabled via `options.PublishNotifications = false`.

---

## Configuration Options

```csharp
services.AddEncinaDataSubjectRights(options =>
{
    // Processing restriction enforcement mode
    options.RestrictionEnforcementMode = DSREnforcementMode.Block;

    // Default deadline for DSR requests (Article 12(3): 30 days)
    options.DefaultDeadlineDays = 30;

    // Maximum extension for complex requests (Article 12(3): 2 months)
    options.MaxExtensionDays = 60;

    // Track immutable audit trail (Article 5(2) accountability)
    options.TrackAuditTrail = true;

    // Publish domain notifications at lifecycle events
    options.PublishNotifications = true;

    // Auto-scan assemblies for [PersonalData] attributes at startup
    options.AutoRegisterFromAttributes = true;
    options.AssembliesToScan.Add(typeof(Program).Assembly);

    // Register health check
    options.AddHealthCheck = true;

    // Default erasable and portable categories
    options.DefaultErasableCategories.Add(PersonalDataCategory.Contact);
    options.DefaultErasableCategories.Add(PersonalDataCategory.Identity);
    options.DefaultPortableCategories.Add(PersonalDataCategory.Contact);
    options.DefaultPortableCategories.Add(PersonalDataCategory.Identity);
});
```

---

## Enforcement Modes

| Mode | Active Restriction | Use Case |
|------|-------------------|----------|
| `Block` | Returns `DSRErrors.RestrictionActive` error | Production (GDPR Article 18 compliant) |
| `Warn` | Logs warning at `Warning` level, proceeds | Migration/testing phase |
| `Disabled` | Skips all restriction checks (no-op) | Development environments |

---

## Database Providers

The in-memory stores (`InMemoryDSRRequestStore`, `InMemoryDSRAuditStore`) are suitable for development and testing. For production, use a database-backed provider:

| Provider Category | Providers | Registration |
|-------------------|-----------|-------------|
| ADO.NET | SQLite, SQL Server, PostgreSQL, MySQL | `config.UseDataSubjectRights = true` in `AddEncinaADO()` |
| Dapper | SQLite, SQL Server, PostgreSQL, MySQL | `config.UseDataSubjectRights = true` in `AddEncinaDapper()` |
| EF Core | SQLite, SQL Server, PostgreSQL, MySQL | `config.UseDataSubjectRights = true` in `AddEncinaEntityFrameworkCore()` |
| MongoDB | MongoDB | `config.UseDataSubjectRights = true` in `AddEncinaMongoDB()` |

Each provider registers `IDSRRequestStore` and `IDSRAuditStore` backed by the corresponding database.

All 13 database provider implementations are planned. The in-memory stores are the default fallback when no database provider is registered.

---

## Observability

### OpenTelemetry Tracing

The module creates activities with the `Encina.Compliance.DataSubjectRights` ActivitySource:

| Activity | Tags |
|----------|------|
| `DSR.Request` | `dsr.right_type`, `dsr.subject_id`, `dsr.outcome` |
| `DSR.Erasure` | `dsr.subject_id`, `dsr.outcome`, `dsr.failure_reason` |
| `DSR.Portability.Export` | `dsr.subject_id`, `dsr.format`, `dsr.outcome` |
| `DSR.Restriction.Check` | `dsr.request_type`, `dsr.outcome`, `dsr.enforcement_mode` |

### Metrics

| Metric | Type | Description |
|--------|------|-------------|
| `dsr.requests.total` | Counter | Total DSR requests processed (tagged by `right_type`, `outcome`) |
| `dsr.erasure.fields_erased.total` | Counter | Total personal data fields erased |
| `dsr.erasure.fields_retained.total` | Counter | Total fields retained due to legal requirements |
| `dsr.portability.exports.total` | Counter | Total portability export operations (tagged by `format`, `outcome`) |
| `dsr.restriction.checks.total` | Counter | Total processing restriction checks (tagged by `outcome`) |
| `dsr.request.duration` | Histogram | Duration of DSR request handling (ms) |
| `dsr.erasure.duration` | Histogram | Duration of erasure operations (ms) |
| `dsr.portability.duration` | Histogram | Duration of portability export operations (ms) |

### Structured Logging

Log events using `[LoggerMessage]` source generator for zero-allocation structured logging:

| EventId | Level | Message |
|---------|-------|---------|
| 8300 | Information | DSR auto-registration completed |
| 8320 | Information | DSR request started |
| 8321 | Information | DSR request completed |
| 8322 | Warning | DSR request failed |
| 8325 | Information | Erasure started |
| 8326 | Information | Erasure completed |
| 8330 | Information | Portability export started |
| 8331 | Information | Portability export completed |
| 8335 | Information | Processing restriction applied |
| 8336 | Warning | Processing blocked (Block mode) |
| 8337 | Warning | Processing allowed despite restriction (Warn mode) |
| 8344 | Warning | Audit entry recording failed |
| 8346 | Warning | Restriction check store error (fail-open) |

---

## Health Check

Opt-in via `options.AddHealthCheck = true`:

```csharp
services.AddEncinaDataSubjectRights(options =>
{
    options.AddHealthCheck = true;
});
```

The health check (`encina-dsr`) verifies:

- `DataSubjectRightsOptions` are configured
- `IDSRRequestStore` is resolvable from DI
- `IPersonalDataLocator` is resolvable (Degraded if missing)
- `IDataErasureExecutor` is resolvable (Degraded if missing)
- No overdue DSR requests exist (Degraded if any found)

Overdue request monitoring is a key compliance signal. The health check reports the count of overdue requests as structured data, enabling alerting on potential Article 12(3) deadline violations.

---

## Error Handling

All operations return `Either<EncinaError, T>`:

```csharp
var result = await handler.HandleErasureAsync(
    new ErasureRequest("subject-123", ErasureReason.ConsentWithdrawn, Scope: null));

result.Match(
    Right: erasureResult =>
    {
        logger.LogInformation("Erased {Fields} fields", erasureResult.FieldsErased);
    },
    Left: error =>
    {
        logger.LogError("Failed: {Code} - {Message}", error.Code, error.Message);
        // error.Details contains structured metadata (subjectId, rightType, requirement)
    }
);
```

### Error Codes

| Code | Description |
|------|-------------|
| `dsr.request_not_found` | DSR request ID does not exist |
| `dsr.request_already_completed` | Request is in a terminal state |
| `dsr.identity_not_verified` | Identity verification required (Article 12(6)) |
| `dsr.restriction_active` | Processing restriction blocks the request (Article 18) |
| `dsr.erasure_failed` | Erasure operation failed |
| `dsr.export_failed` | Data export failed |
| `dsr.format_not_supported` | No `IExportFormatWriter` for the requested format |
| `dsr.deadline_expired` | DSR request deadline exceeded (Article 12(3)) |
| `dsr.exemption_applies` | An Article 17(3) exemption prevents the operation |
| `dsr.subject_not_found` | Data subject not found in the system |
| `dsr.locator_failed` | Personal data locator failed |
| `dsr.store_error` | DSR store persistence error |
| `dsr.rectification_failed` | Data rectification failed |
| `dsr.objection_rejected` | Objection rejected (compelling legitimate grounds) |
| `dsr.invalid_request` | DSR request is structurally invalid |

---

## Best Practices

1. **Use `Block` mode in production** -- `Warn` mode is only for migration periods; production systems must enforce Article 18 restrictions
2. **Register a real `IPersonalDataLocator`** -- the InMemory stores are for development/testing only; production requires a locator that queries actual data stores
3. **Create custom `IDataErasureStrategy` implementations** -- `HardDeleteErasureStrategy` is the default; consider anonymization or crypto-shredding for GDPR-compliant field-level erasure
4. **Set up health check monitoring** -- enable `AddHealthCheck = true` and configure alerts for overdue DSR requests to prevent Article 12(3) deadline violations
5. **Subscribe to domain notifications** -- react to `DataErasedNotification` and `ProcessingRestrictedNotification` to propagate changes to third-party recipients (Article 19)
6. **Mark all personal data fields** -- use `[PersonalData]` on every entity property that contains personal data; missing attributes mean missing data in access and portability responses
7. **Document retention reasons** -- set `LegalRetention = true` and provide a `RetentionReason` string referencing the specific legal basis
8. **Configure deadline monitoring** -- use `GetOverdueRequestsAsync()` in scheduled jobs or the health check to catch approaching deadlines early
9. **Test with InMemory stores first** -- `InMemoryDSRRequestStore` and `InMemoryDSRAuditStore` provide fast, deterministic unit tests with `TimeProvider` injection

---

## Testing

### Unit Tests with In-Memory Stores

```csharp
var requestStore = new InMemoryDSRRequestStore(
    TimeProvider.System,
    NullLogger<InMemoryDSRRequestStore>.Instance);

// Create a DSR request
var request = DSRRequest.Create("req-001", "subject-123",
    DataSubjectRight.Erasure, DateTimeOffset.UtcNow);
await requestStore.CreateAsync(request);

// Verify it exists
var result = await requestStore.GetByIdAsync("req-001");
Assert.True(result.IsRight);

// Check for active restrictions
var hasRestriction = await requestStore.HasActiveRestrictionAsync("subject-123");
Assert.True(hasRestriction.IsRight);
```

### Full Pipeline Test

```csharp
var services = new ServiceCollection();
services.AddEncina(c => c.RegisterServicesFromAssemblyContaining<MyCommand>());
services.AddEncinaDataSubjectRights(o =>
    o.RestrictionEnforcementMode = DSREnforcementMode.Block);

// Seed an active restriction for the subject
var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();
var store = scope.ServiceProvider.GetRequiredService<IDSRRequestStore>();

var restriction = DSRRequest.Create("restriction-001", "restricted-user",
    DataSubjectRight.Restriction, DateTimeOffset.UtcNow);
await store.CreateAsync(restriction);

var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
var result = await encina.Send(new UpdateProfileCommand("restricted-user"));
Assert.True(result.IsLeft); // Blocked due to active restriction
```

### Integration Tests via DI Container

```csharp
var services = new ServiceCollection();
services.AddEncinaDataSubjectRights(options =>
{
    options.AddHealthCheck = true;
    options.AutoRegisterFromAttributes = true;
    options.AssembliesToScan.Add(typeof(Customer).Assembly);
});

var provider = services.BuildServiceProvider();
var handler = provider.GetRequiredService<IDataSubjectRightsHandler>();
var auditStore = provider.GetRequiredService<IDSRAuditStore>();

// Execute an access request
var accessResult = await handler.HandleAccessAsync(
    new AccessRequest("subject-123", IncludeProcessingActivities: false));
Assert.True(accessResult.IsRight);

// Verify audit trail was recorded
var trail = await auditStore.GetAuditTrailAsync("subject-123");
Assert.True(trail.IsRight);
```

---

## FAQ

**Q: How do I handle Article 17(3) exemptions for erasure?**
Mark the relevant entity properties with `LegalRetention = true` and `RetentionReason = "..."` on the `[PersonalData]` attribute. The erasure executor automatically skips these fields and documents them in the `ErasureResult.RetentionReasons` collection.

**Q: How do I extend the deadline (Article 12(3))?**
Call `store.UpdateStatusAsync(requestId, DSRRequestStatus.Extended, reason: "Complex request requiring cross-system coordination")`. The `ExtendedDeadlineAtUtc` can be set up to 60 additional days (2 months) beyond the original 30-day deadline.

**Q: What is the difference between InMemory and database stores?**
The `InMemoryDSRRequestStore` and `InMemoryDSRAuditStore` use `ConcurrentDictionary` for storage. They are suitable for development and testing only. For production, register a database-backed provider (ADO.NET, Dapper, EF Core, or MongoDB) that provides durable persistence and survives application restarts.

**Q: How does restriction enforcement work at the pipeline level?**
The `ProcessingRestrictionPipelineBehavior` runs before request handlers. It detects `[RestrictProcessing]`, `[ProcessesPersonalData]`, or `[ProcessingActivity]` attributes on the request type (cached per generic type). It extracts the subject ID and calls `IDSRRequestStore.HasActiveRestrictionAsync`. If a restriction exists, the behavior applies the configured enforcement mode (Block, Warn, or Disabled).

**Q: What happens if no `[RestrictProcessing]` or personal data attribute is present?**
The pipeline behavior skips all restriction checks with zero overhead. Attribute presence is cached statically per closed generic type.

**Q: Can I register custom implementations before calling `AddEncinaDataSubjectRights`?**
Yes. All service registrations use `TryAdd`, so existing registrations are preserved. Register your custom `IDSRRequestStore`, `IDataErasureStrategy`, or `IPersonalDataLocator` before calling `AddEncinaDataSubjectRights()`.

**Q: What happens if the restriction store fails during a pipeline check?**
The behavior uses a fail-open strategy: it logs a warning at event ID 8346 and allows the request to proceed. This prevents a store outage from blocking all requests in the system.

**Q: How are the 8 data subject rights mapped?**
The `DataSubjectRight` enum covers all eight rights: `Access` (Art. 15), `Rectification` (Art. 16), `Erasure` (Art. 17), `Restriction` (Art. 18), `Portability` (Art. 20), `Objection` (Art. 21), `AutomatedDecisionMaking` (Art. 22), and `Notification` (Art. 19).
