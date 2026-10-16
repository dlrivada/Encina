# Encina.Compliance.DataSubjectRights

[![NuGet](https://img.shields.io/nuget/v/Encina.Compliance.DataSubjectRights.svg)](https://www.nuget.org/packages/Encina.Compliance.DataSubjectRights/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](../../LICENSE)

GDPR Data Subject Rights management for Encina. Provides declarative processing restriction enforcement at the CQRS pipeline level with full request lifecycle management via Marten event sourcing, data access/export/erasure orchestration, and compliance health checks. Implements GDPR Articles 12, 15-22.

## Features

- **Event-Sourced DSR Requests** -- `DSRRequestAggregate` with full lifecycle: submit, verify, process, complete, deny, extend, expire
- **CQRS Architecture** -- Commands via `IAggregateRepository<DSRRequestAggregate>`, queries via `IReadModelRepository<DSRRequestReadModel>`
- **7 Domain Events** -- `DSRRequestSubmitted`, `Verified`, `Processing`, `Completed`, `Denied`, `Extended`, `Expired`
- **Declarative Restriction Enforcement** -- `[RestrictProcessing]` attribute on request types (Article 18)
- **Pipeline-Level Enforcement** -- `ProcessingRestrictionPipelineBehavior` validates restrictions before handler execution
- **Three Enforcement Modes** -- `Block` (reject), `Warn` (log and proceed), `Disabled` (no-op)
- **8 GDPR Rights** -- Access, Rectification, Erasure, Restriction, Portability, Objection, AutomatedDecisionMaking, Notification
- **Personal Data Discovery** -- `[PersonalData]` attribute with automated scanning via `IPersonalDataLocator`
- **Data Erasure Orchestration** -- `IDataErasureExecutor` with pluggable `IDataErasureStrategy` (default: HardDelete) and Article 17(3) exemptions
- **Data Portability Exports** -- `IDataPortabilityExporter` with built-in JSON, CSV, and XML format writers
- **Audit Trail via Event Stream** -- Full audit history inherent in the Marten event stream (no separate audit store needed)
- **Domain Notifications** -- `DataErasedNotification`, `DataRectifiedNotification`, `ProcessingRestrictedNotification`, `RestrictionLiftedNotification`
- **Railway Oriented Programming** -- All operations return `Either<EncinaError, T>`, no exceptions
- **Cache-Aside Pattern** -- `ICacheProvider` integration with fire-and-forget invalidation
- **Marten Projections** -- `DSRRequestProjection` transforms events to `DSRRequestReadModel` for efficient querying
- **Full Observability** -- OpenTelemetry tracing, structured logging (EventId 8300-8349), counters and histograms, health check
- **PostgreSQL via Marten** -- Event store + document DB for event sourcing and projections
- **.NET 10 Compatible** -- Built with latest C# features

## Installation

```bash
dotnet add package Encina.Compliance.DataSubjectRights
dotnet add package Encina.Marten  # Required: Marten event sourcing infrastructure
```

## Quick Start

### 1. Register Services

```csharp
// Register Encina core
services.AddEncina(config =>
    config.RegisterServicesFromAssemblyContaining<Program>());

// Register DSR module (options, pipeline behavior, validator, service)
services.AddEncinaDataSubjectRights(options =>
{
    options.RestrictionEnforcementMode = DSREnforcementMode.Block;
    options.DefaultDeadlineDays = 30;
    options.TrackAuditTrail = true;
    options.AutoRegisterFromAttributes = true;
    options.AssembliesToScan.Add(typeof(Program).Assembly);
});

// Register Marten aggregate + projection for DSR
services.AddDSRRequestAggregates();
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

### 3. Submit a DSR Request (Event-Sourced)

```csharp
var dsrService = serviceProvider.GetRequiredService<IDSRService>();

// Submit an access request (Article 15)
var result = await dsrService.SubmitRequestAsync(
    subjectId: "user-123",
    rightType: DataSubjectRight.Access,
    requestDetails: "I want a copy of all my personal data");

// result: Either<EncinaError, Guid> — the DSR request aggregate ID

// Verify identity (Article 12(6))
await dsrService.VerifyIdentityAsync(requestId, verifiedBy: "admin-1");

// Handle access request
var accessResult = await dsrService.HandleAccessAsync(
    new AccessRequest("user-123", IncludeProcessingActivities: true));

// Complete the request
await dsrService.CompleteRequestAsync(requestId);
```

### 4. Handle Erasure (Article 17)

```csharp
var erasureResult = await dsrService.HandleErasureAsync(
    new ErasureRequest("user-123", ErasureReason.ConsentWithdrawn, scope: null));

// erasureResult: Either<EncinaError, ErasureResult>
// Includes FieldsErased, FieldsRetained, FieldsFailed, RetentionReasons, Exemptions
```

### 5. Check Processing Restrictions (Article 18)

```csharp
// Decorate requests that should respect processing restrictions
[RestrictProcessing(SubjectIdProperty = nameof(CustomerId))]
public sealed record UpdateCustomerProfileCommand(string CustomerId, string NewEmail) : ICommand;

// No attribute: pipeline skips restriction checks entirely (zero overhead)
public sealed record GetPublicCatalogQuery() : IQuery<CatalogDto>;
```

When a data subject has an active processing restriction and `RestrictionEnforcementMode` is `Block`, the pipeline returns `DSRErrors.RestrictionActive` before the handler executes.

### 6. Query DSR State

```csharp
// Get a specific request (cached)
var request = await dsrService.GetRequestAsync(requestId);

// Check if subject has active restriction
var hasRestriction = await dsrService.HasActiveRestrictionAsync("user-123");

// Get all pending requests
var pending = await dsrService.GetPendingRequestsAsync();

// Get overdue requests
var overdue = await dsrService.GetOverdueRequestsAsync();
```

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

```
                  SubmitRequest
                       |
                       v
               +---------------+
               |   Received    |
               +-------+-------+
                       |
                 VerifyIdentity
                       |
                       v
               +---------------+
               |IdentityVerified|
               +-------+-------+
                       |
                StartProcessing
                       |
                       v
               +---------------+
               |  InProgress   |
               +-------+-------+
                       |
          +------------+------------+
          |                         |
       Complete       Deny(DSRRejectionReason, explanation)
          |                         |
          v                         v
   +-----------+            +-----------+
   | Completed |            | Rejected  |
   +-----------+            +-----------+

  Any non-terminal status:
  - Extend -> Extended (deadline extended by up to 2 months)
  - Expire -> Expired (deadline passed without completion)
```

### Refusing a request

`IDSRService.DenyRequestAsync` takes a `DSRRejectionReason` code and the explanation given to the data subject. Both are stored on the event stream and exposed as `RejectionReason` / `RejectionExplanation` on `DSRRequestAggregate` and `DSRRequestReadModel`.

```csharp
var denied = await dsrService.DenyRequestAsync(
    requestId, DSRRejectionReason.Excessive, "Repeated identical requests; see request history.");
// denied: Either<EncinaError, Unit>
```

| Code | Value | Legal basis |
|------|-------|-------------|
| `ManifestlyUnfounded` | 1 | GDPR Art. 12(5) |
| `Excessive` | 2 | GDPR Art. 12(5) |
| `IdentityNotVerifiable` | 3 | GDPR Art. 12(2) with Art. 11(2) |
| `RestrictedByLaw` | 4 | GDPR Art. 23 |
| `RightNotApplicable` | 5 | GDPR Art. 17(3), 20(1) or 21(1) |
| `Other` | 99 | GDPR Art. 12(4) |
| `AbusiveIntent` | 100 | Draft law: COM(2025) 837, amending GDPR Art. 12(5); needs `EnableOmnibusArticle12RefusalGrounds` |

Encina only records `Excessive`; the controller judges repetition from the request history. `DSRRejectionReasonExtensions` provides `LegalBasis` and `IsDraftLawGround`.

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
| `EnableOmnibusArticle12RefusalGrounds` | `bool` | `false` | Accept the draft-law refusal reason `DSRRejectionReason.AbusiveIntent` (COM(2025) 837, draft amendment of GDPR Art. 12(5)); when `false`, `DenyRequestAsync` returns `dsr.rejection_reason_not_enabled` |
| `AddHealthCheck` | `bool` | `false` | Register health check with `IHealthChecksBuilder` |
| `AssembliesToScan` | `List<Assembly>` | `[]` | Assemblies to scan for `[PersonalData]` attributes |
| `DefaultErasableCategories` | `HashSet<PersonalDataCategory>` | `[]` | Categories erasable by default (empty = all) |
| `DefaultPortableCategories` | `HashSet<PersonalDataCategory>` | `[]` | Categories included in portability exports (empty = all) |

## Multi-tenancy

`DefaultDSRService` reads the ambient tenant from `IRequestContextAccessor.RequestContext.TenantId` and scopes every operation to it (#1963; background in [SPEC-002](../../docs/specifications/SPEC-002-eu-regulatory-readiness.md) DEC-006 and DEC-009):

- The queries `GetRequestsBySubjectAsync`, `GetPendingRequestsAsync`, `GetOverdueRequestsAsync` and `HasActiveRestrictionAsync` filter `DSRRequestReadModel.TenantId` by the ambient tenant.
- `GetRequestAsync` and the lifecycle commands (`VerifyIdentityAsync`, `StartProcessingAsync`, `CompleteRequestAsync`, `DenyRequestAsync`, `ExtendDeadlineAsync`, `ExpireRequestAsync`) treat another tenant's request as not found (`dsr.request_not_found`) and never change it.
- `SubmitRequestAsync` records the request under the ambient tenant when `tenantId` is `null`, and refuses a `tenantId` that contradicts it with `dsr.tenant_required`. `HandleRestrictionAsync` records the restriction under the ambient tenant.
- Cache keys are tenant-scoped: `dsr:tenant:{tenantId|-}:request:{id}` and `dsr:tenant:{tenantId|-}:restriction:{subjectId}` (`-` when there is no tenant).

`DataSubjectRightsOptions.RequireTenantContext` (`bool?`, default `null`) decides what happens when the ambient tenant is missing:

| Value | Behavior |
|-------|----------|
| `null` (default) | Auto-detect: fail closed with `dsr.tenant_required` when `Encina.Tenancy` is registered (`ITenantProvider` resolves), otherwise run unscoped. Single-tenant applications need no configuration. |
| `true` | Always fail closed with `dsr.tenant_required` when the tenant is missing. |
| `false` | Explicit opt-out; a missing tenant runs an unscoped operation. Logged once at startup when `Encina.Tenancy` is registered. |

Two components follow the same rule:

- `ProcessingRestrictionPipelineBehavior` in `Block` mode blocks the request when the restriction lookup returns `dsr.tenant_required`, instead of the usual fail-open on store errors. In `Warn` mode the request proceeds.
- `DataSubjectRightsHealthCheck` skips the overdue-request check (data key `overdueRequestCheck` = `skipped: no tenant context`) when the probe has no tenant in a multi-tenant application, because the tenant-scoped query is refused.

## Error Codes

| Code | Meaning |
|------|---------|
| `dsr.request_not_found` | No DSR request found with the given identifier (or it belongs to another tenant) |
| `dsr.tenant_required` | Tenant isolation is required and the ambient tenant is missing, or a submitted `tenantId` contradicts it (`DSRErrors.TenantRequiredCode`) |
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
| `dsr.service_error` | DSR service operation failed |
| `dsr.event_history_unavailable` | Event history retrieval not yet available via Marten |
| `dsr.rectification_failed` | Data rectification operation failed (Article 16) |
| `dsr.objection_rejected` | Objection rejected due to compelling legitimate grounds (Article 21) |
| `dsr.invalid_request` | The DSR request is invalid |
| `dsr.rejection_reason_not_enabled` | The refusal reason is draft law and `EnableOmnibusArticle12RefusalGrounds` is off |

## Custom Implementations

Register custom implementations before `AddEncinaDataSubjectRights()` to override defaults (TryAdd semantics):

```csharp
// Custom DSR service (e.g., external API-backed)
services.AddScoped<IDSRService, MyDSRService>();

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

## Testing

### Unit Tests (Mock Dependencies)

```csharp
// Mock the Marten repositories for unit testing
var repository = Substitute.For<IAggregateRepository<DSRRequestAggregate>>();
var readModelRepository = Substitute.For<IReadModelRepository<DSRRequestReadModel>>();
var cache = Substitute.For<ICacheProvider>();

var service = new DefaultDSRService(
    repository, readModelRepository, locator, erasureExecutor,
    portabilityExporter, processingActivityRegistry, cache,
    TimeProvider.System, NullLogger<DefaultDSRService>.Instance);
```

### Integration Tests (Docker + Marten)

```csharp
// Full integration tests require PostgreSQL via Docker/Testcontainers
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
public class DSRIntegrationTests
{
    private readonly MartenFixture _fixture;
    // ... test against real Marten event store
}
```

## Observability

- **Tracing**: `Encina.Compliance.DataSubjectRights` ActivitySource with DSR-specific activities (`DSR.Request`, `DSR.Erasure`, `DSR.Portability.Export`, `DSR.Restriction.Check`)
- **Refusals**: `dsr.requests.denied.total` counts refusals (tags `dsr.right_type`, `dsr.rejection_reason`); the `DSR.AggregateCommand` activity carries the reason; log EventIds 8311 (denied) and 8312 (draft-law reason not enabled)
- **Metrics** (a subset; the full list is in `DataSubjectRightsDiagnostics`): counters (`dsr.requests.total`, `dsr.erasure.fields_erased.total`, `dsr.erasure.fields_retained.total`, `dsr.portability.exports.total`, `dsr.restriction.checks.total`) and histograms (`dsr.request.duration`, `dsr.erasure.duration`, `dsr.portability.duration`)
- **Logging**: Structured log events via `[LoggerMessage]` source generator (zero-allocation), event IDs 8300-8349
- **Health Check**: Verifies required services are registered and resolvable from DI

## Related Packages

| Package | Description |
|---------|-------------|
| `Encina` | Core CQRS pipeline with `IPipelineBehavior` |
| `Encina.Marten` | Marten event sourcing infrastructure |
| `Encina.Compliance.GDPR` | GDPR processing activity tracking and RoPA |
| `Encina.Compliance.Consent` | GDPR Article 7 consent management |
| `Encina.Compliance.LawfulBasis` | GDPR Article 6 lawful basis tracking |

## GDPR Compliance

This package implements key GDPR requirements:

| Article | Requirement | Implementation |
|---------|-------------|----------------|
| **11(2), 12(2)** | Refuse when the controller cannot identify the subject | `DSRRejectionReason.IdentityNotVerifiable` |
| **12(5)** | Refuse manifestly unfounded or excessive requests | `DSRRejectionReason.ManifestlyUnfounded`, `Excessive`; draft `AbusiveIntent` (COM(2025) 837) |
| **23** | Restrictions by Union or Member State law | `DSRRejectionReason.RestrictedByLaw` |
| **12(3)** | Respond within one month, extendable by 2 months | `DefaultDeadlineDays`, `MaxExtensionDays`, `DSRRequestStatus.Extended` |
| **12(6)** | Verify identity before processing | `VerifyIdentityAsync`, `DSRRequestStatus.IdentityVerified` |
| **15** | Right of access to personal data | `IDSRService.HandleAccessAsync`, `AccessResponse` |
| **16** | Right to rectification | `IDSRService.HandleRectificationAsync` |
| **17** | Right to erasure (right to be forgotten) | `IDataErasureExecutor`, `IDataErasureStrategy`, `ErasureExemption` |
| **17(3)** | Exemptions from erasure | `ErasureExemption` enum, `PersonalDataAttribute.LegalRetention` |
| **18** | Right to restriction of processing | `ProcessingRestrictionPipelineBehavior`, `[RestrictProcessing]` |
| **19** | Notification obligation to third parties | `DataSubjectRight.Notification`, notification publishing |
| **20** | Right to data portability | `IDataPortabilityExporter`, JSON/CSV/XML `IExportFormatWriter` |
| **21** | Right to object | `IDSRService.HandleObjectionAsync` |
| **22** | Automated individual decision-making | `DataSubjectRight.AutomatedDecisionMaking` |

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.
