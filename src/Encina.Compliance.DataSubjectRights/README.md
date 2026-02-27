# Encina.Compliance.DataSubjectRights

[![NuGet](https://img.shields.io/nuget/v/Encina.Compliance.DataSubjectRights.svg)](https://www.nuget.org/packages/Encina.Compliance.DataSubjectRights/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](../../LICENSE)

GDPR Data Subject Rights management for Encina. Provides declarative processing restriction enforcement at the CQRS pipeline level with full request lifecycle management, audit trail, data access/export/erasure orchestration, and compliance health checks. Implements GDPR Articles 12, 15-22.

## Features

- **Declarative Restriction Enforcement** -- `[RestrictProcessing]` attribute on request types (Article 18)
- **Full DSR Request Lifecycle** -- Received, IdentityVerified, InProgress, Completed, Rejected, Extended, Expired
- **8 GDPR Rights** -- Access, Rectification, Erasure, Restriction, Portability, Objection, AutomatedDecisionMaking, Notification
- **Pipeline-Level Enforcement** -- `ProcessingRestrictionPipelineBehavior` validates restrictions before handler execution
- **Three Enforcement Modes** -- `Block` (reject), `Warn` (log and proceed), `Disabled` (no-op)
- **Personal Data Discovery** -- `[PersonalData]` attribute with automated scanning via `IPersonalDataLocator`
- **Data Erasure Orchestration** -- `IDataErasureExecutor` with pluggable `IDataErasureStrategy` (default: HardDelete) and Article 17(3) exemptions
- **Data Portability Exports** -- `IDataPortabilityExporter` with built-in JSON, CSV, and XML format writers
- **Immutable Audit Trail** -- Every DSR operation is recorded via `IDSRAuditStore`
- **Domain Notifications** -- `DataErasedNotification`, `DataRectifiedNotification`, `ProcessingRestrictedNotification`, `RestrictionLiftedNotification`
- **Railway Oriented Programming** -- All operations return `Either<EncinaError, T>`, no exceptions
- **Full Observability** -- OpenTelemetry tracing, 5 counters, 3 histograms, 28 structured log events, health check
- **13 Database Providers** -- ADO.NET, Dapper, EF Core (SQLite, SQL Server, PostgreSQL, MySQL) + MongoDB (planned)
- **.NET 10 Compatible** -- Built with latest C# features

## Installation

```bash
dotnet add package Encina.Compliance.DataSubjectRights
```

## Quick Start

### 1. Register Services

```csharp
services.AddEncina(config =>
    config.RegisterServicesFromAssemblyContaining<Program>());

services.AddEncinaDataSubjectRights(options =>
{
    options.RestrictionEnforcementMode = DSREnforcementMode.Block;
    options.DefaultDeadlineDays = 30;
    options.TrackAuditTrail = true;
    options.AutoRegisterFromAttributes = true;
    options.AssembliesToScan.Add(typeof(Program).Assembly);
});
```

### 2. Mark Personal Data with Attributes

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

    [PersonalData(Category = PersonalDataCategory.Health, Erasable = true, Portable = false)]
    public string BloodType { get; set; } = string.Empty;
}
```

### 3. Submit a DSR Request

```csharp
var handler = serviceProvider.GetRequiredService<IDataSubjectRightsHandler>();

// Submit an access request (Article 15)
var request = new AccessRequest
{
    SubjectId = "user-123",
    IncludeProcessingActivities = true
};

var result = await handler.HandleAccessAsync(request);
// result: Either<EncinaError, AccessResponse>

// Submit an erasure request (Article 17)
var erasureRequest = new ErasureRequest
{
    SubjectId = "user-123",
    Reason = ErasureReason.WithdrawnConsent
};

var erasureResult = await handler.HandleErasureAsync(erasureRequest);
// erasureResult: Either<EncinaError, ErasureResult>
```

### 4. Check Processing Restrictions (Article 18)

```csharp
// Decorate requests that should respect processing restrictions
[RestrictProcessing(SubjectIdProperty = nameof(CustomerId))]
public sealed record UpdateCustomerProfileCommand(string CustomerId, string NewEmail) : ICommand;

// No attribute: pipeline skips restriction checks entirely (zero overhead)
public sealed record GetPublicCatalogQuery() : IQuery<CatalogDto>;
```

When a data subject has an active processing restriction and `RestrictionEnforcementMode` is `Block`, the pipeline returns `DSRErrors.RestrictionActive` before the handler executes.

## Data Subject Rights

| Right | Enum Value | GDPR Article | Description |
|-------|------------|--------------|-------------|
| Access | `DataSubjectRight.Access` | Art. 15 | Obtain confirmation and copy of personal data |
| Rectification | `DataSubjectRight.Rectification` | Art. 16 | Correct inaccurate personal data |
| Erasure | `DataSubjectRight.Erasure` | Art. 17 | Erase personal data ("right to be forgotten") |
| Restriction | `DataSubjectRight.Restriction` | Art. 18 | Restrict processing of personal data |
| Portability | `DataSubjectRight.Portability` | Art. 20 | Receive data in machine-readable format |
| Objection | `DataSubjectRight.Objection` | Art. 21 | Object to processing based on legitimate interests |
| Automated Decision-Making | `DataSubjectRight.AutomatedDecisionMaking` | Art. 22 | Right not to be subject to automated decisions |
| Notification | `DataSubjectRight.Notification` | Art. 19 | Notify third parties of rectification/erasure/restriction |

## Request Lifecycle

Each DSR request follows a defined lifecycle:

| Status | Description |
|--------|-------------|
| `Received` | Request received, 30-day clock starts (Article 12(3)) |
| `IdentityVerified` | Data subject identity confirmed (Article 12(6)) |
| `InProgress` | Request is actively being processed |
| `Completed` | Request fulfilled successfully |
| `Rejected` | Request rejected with stated reason (Article 12(4)) |
| `Extended` | Deadline extended by up to 2 months for complex requests (Article 12(3)) |
| `Expired` | Deadline passed without completion (potential compliance violation) |

## Enforcement Modes

| Mode | Behavior | Use Case |
|------|----------|----------|
| `Block` | Reject requests targeting restricted data subjects | Production (recommended) |
| `Warn` | Log warning, allow request to proceed | Migration/testing phase |
| `Disabled` | Skip all restriction validation | Development environments |

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `RestrictionEnforcementMode` | `DSREnforcementMode` | `Block` | How to handle active processing restrictions |
| `DefaultDeadlineDays` | `int` | `30` | Days to complete a DSR request (Article 12(3)) |
| `MaxExtensionDays` | `int` | `60` | Maximum extension for complex requests (Article 12(3)) |
| `TrackAuditTrail` | `bool` | `true` | Record all DSR operations in audit store |
| `PublishNotifications` | `bool` | `true` | Publish domain notifications for DSR lifecycle events |
| `AutoRegisterFromAttributes` | `bool` | `true` | Scan assemblies for `[PersonalData]` at startup |
| `AddHealthCheck` | `bool` | `false` | Register health check with `IHealthChecksBuilder` |
| `AssembliesToScan` | `List<Assembly>` | `[]` | Assemblies to scan for `[PersonalData]` attributes |
| `DefaultErasableCategories` | `HashSet<PersonalDataCategory>` | `[]` | Categories erasable by default (empty = all) |
| `DefaultPortableCategories` | `HashSet<PersonalDataCategory>` | `[]` | Categories included in portability exports (empty = all) |

## Error Codes

| Code | Meaning |
|------|---------|
| `dsr.request_not_found` | No DSR request found with the given identifier |
| `dsr.request_already_completed` | The request has already been completed and cannot be modified |
| `dsr.identity_not_verified` | Identity verification required before processing (Article 12(6)) |
| `dsr.restriction_active` | Processing is restricted for the data subject (Article 18) |
| `dsr.deadline_expired` | The statutory deadline has expired (Article 12(3)) |
| `dsr.erasure_failed` | Data erasure operation failed (Article 17) |
| `dsr.export_failed` | Data export operation failed (Article 20) |
| `dsr.format_not_supported` | Requested export format has no registered writer |
| `dsr.exemption_applies` | An Article 17(3) exemption prevents the operation |
| `dsr.subject_not_found` | Data subject not found in the system |
| `dsr.locator_failed` | Personal data locator encountered an error |
| `dsr.store_error` | DSR persistence store operation failed |
| `dsr.rectification_failed` | Data rectification operation failed (Article 16) |
| `dsr.objection_rejected` | Objection rejected due to compelling legitimate grounds (Article 21) |
| `dsr.invalid_request` | The DSR request is invalid |

## Custom Implementations

Register custom implementations before `AddEncinaDataSubjectRights()` to override defaults (TryAdd semantics):

```csharp
// Custom request store (e.g., database-backed)
services.AddSingleton<IDSRRequestStore, DatabaseDSRRequestStore>();

// Custom audit store
services.AddSingleton<IDSRAuditStore, DatabaseDSRAuditStore>();

// Custom erasure strategy (e.g., anonymization instead of hard delete)
services.AddSingleton<IDataErasureStrategy, AnonymizationErasureStrategy>();

// Custom subject ID extractor
services.AddSingleton<IDataSubjectIdExtractor, ClaimsPrincipalIdExtractor>();

services.AddEncinaDataSubjectRights(options =>
{
    options.RestrictionEnforcementMode = DSREnforcementMode.Block;
    options.AutoRegisterFromAttributes = false;
});
```

## Database Providers

The core package ships with `InMemoryDSRRequestStore` and `InMemoryDSRAuditStore` for development and testing. Database-backed implementations for the 13 providers are planned for future phases:

```csharp
// Future: ADO.NET (SQLite example)
services.AddEncinaADO(config =>
{
    config.UseDataSubjectRights = true;
});

// Future: Dapper (SQL Server example)
services.AddEncinaDapper(config =>
{
    config.UseDataSubjectRights = true;
});

// Future: EF Core (PostgreSQL example)
services.AddEncinaEntityFrameworkCore<AppDbContext>(config =>
{
    config.UseDataSubjectRights = true;
});
```

## Observability

- **Tracing**: `Encina.Compliance.DataSubjectRights` ActivitySource with DSR-specific activities (`DSR.Request`, `DSR.Erasure`, `DSR.Portability.Export`, `DSR.Restriction.Check`)
- **Metrics**: 5 counters (`dsr.requests.total`, `dsr.erasure.fields_erased.total`, `dsr.erasure.fields_retained.total`, `dsr.portability.exports.total`, `dsr.restriction.checks.total`) and 3 histograms (`dsr.request.duration`, `dsr.erasure.duration`, `dsr.portability.duration`)
- **Logging**: 28 structured log events via `[LoggerMessage]` source generator (zero-allocation), event IDs 8300-8346
- **Health Check**: Verifies store connectivity, required services, and checks for overdue requests

## Related Packages

| Package | Description |
|---------|-------------|
| `Encina` | Core CQRS pipeline with `IPipelineBehavior` |
| `Encina.Compliance.GDPR` | GDPR processing activity tracking and RoPA |
| `Encina.Compliance.Consent` | GDPR Article 7 consent management |
| `Encina.Compliance.LawfulBasis` | GDPR Article 6 lawful basis tracking |

## GDPR Compliance

This package implements key GDPR requirements:

| Article | Requirement | Implementation |
|---------|-------------|----------------|
| **12(3)** | Respond within one month, extendable by 2 months | `DefaultDeadlineDays`, `MaxExtensionDays`, `DSRRequestStatus.Extended` |
| **12(6)** | Verify identity before processing | `DSRRequestStatus.IdentityVerified`, `IdentityNotVerified` error |
| **15** | Right of access to personal data | `IDataSubjectRightsHandler.HandleAccessAsync`, `AccessResponse` |
| **16** | Right to rectification | `IDataSubjectRightsHandler.HandleRectificationAsync` |
| **17** | Right to erasure (right to be forgotten) | `IDataErasureExecutor`, `IDataErasureStrategy`, `ErasureExemption` |
| **17(3)** | Exemptions from erasure | `ErasureExemption` enum, `PersonalDataAttribute.LegalRetention` |
| **18** | Right to restriction of processing | `ProcessingRestrictionPipelineBehavior`, `[RestrictProcessing]` |
| **19** | Notification obligation to third parties | `DataSubjectRight.Notification`, notification publishing |
| **20** | Right to data portability | `IDataPortabilityExporter`, JSON/CSV/XML `IExportFormatWriter` |
| **21** | Right to object | `IDataSubjectRightsHandler.HandleObjectionAsync` |
| **22** | Automated individual decision-making | `DataSubjectRight.AutomatedDecisionMaking` |

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.
