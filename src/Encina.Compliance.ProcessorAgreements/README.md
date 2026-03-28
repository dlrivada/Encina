# Encina.Compliance.ProcessorAgreements

[![NuGet](https://img.shields.io/nuget/v/Encina.Compliance.ProcessorAgreements.svg)](https://www.nuget.org/packages/Encina.Compliance.ProcessorAgreements/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](../../LICENSE)

GDPR Article 28 compliance for Encina. Provides processor registry, Data Processing Agreement lifecycle management, sub-processor hierarchy tracking, mandatory terms validation, SCC compliance, expiration monitoring, and pipeline-level enforcement.

## Features

- **Processor Registry** -- `IProcessorRegistry` manages processor identity with hierarchical sub-processor tracking
- **DPA Lifecycle** -- `IDPAStore` manages agreement lifecycle (Active, Expired, PendingRenewal, Terminated)
- **Mandatory Terms Validation** -- `DPAMandatoryTerms` tracks all eight Article 28(3)(a)-(h) contractual clauses
- **DPA Validator** -- `IDPAValidator` with `ValidateAsync`, `HasValidDPAAsync`, `ValidateAllAsync` for compliance checks
- **Pipeline Enforcement** -- `ProcessorValidationPipelineBehavior` auto-validates DPA before processing `[RequiresProcessor]` requests
- **Three Enforcement Modes** -- `Block` (reject), `Warn` (log and proceed), `Disabled` (no-op)
- **Sub-Processor Hierarchy** -- Depth-limited processor chains with Specific/General authorization types (Art. 28(2))
- **Expiration Monitoring** -- `CheckDPAExpirationHandler` scheduled command detects expiring/expired agreements
- **Seven Notifications** -- `ProcessorRegistered`, `DPASigned`, `DPAExpiring`, `DPAExpired`, `DPATerminated`, `SubProcessorAdded`, `SubProcessorRemoved`
- **Immutable Audit Trail** -- Every operation recorded via `IProcessorAuditStore` per Article 5(2)
- **Railway Oriented Programming** -- All operations return `Either<EncinaError, T>`, no exceptions
- **Full Observability** -- OpenTelemetry tracing, structured logging via `[LoggerMessage]`, health check
- **10 Database Providers** -- ADO.NET, Dapper, EF Core (SQL Server, PostgreSQL, MySQL) + MongoDB
- **.NET 10 Compatible** -- Built with latest C# features

## Installation

```bash
dotnet add package Encina.Compliance.ProcessorAgreements
```

## Quick Start

### 1. Register Services

```csharp
services.AddEncina(config =>
    config.RegisterServicesFromAssemblyContaining<Program>());

services.AddEncinaProcessorAgreements(options =>
{
    options.EnforcementMode = ProcessorAgreementEnforcementMode.Block;
    options.MaxSubProcessorDepth = 3;
    options.ExpirationWarningDays = 30;
    options.TrackAuditTrail = true;
    options.AddHealthCheck = true;
});
```

### 2. Register a Processor

```csharp
var registry = serviceProvider.GetRequiredService<IProcessorRegistry>();

var processor = new Processor
{
    Id = "stripe-payments",
    Name = "Stripe Inc.",
    Country = "US",
    ContactEmail = "privacy@stripe.com",
    Depth = 0,
    SubProcessorAuthorizationType = SubProcessorAuthorizationType.Specific,
    CreatedAtUtc = DateTimeOffset.UtcNow,
    LastUpdatedAtUtc = DateTimeOffset.UtcNow
};

var result = await registry.RegisterProcessorAsync(processor);
// result: Either<EncinaError, Unit>
```

### 3. Create a Data Processing Agreement

```csharp
var store = serviceProvider.GetRequiredService<IDPAStore>();

var dpa = new DataProcessingAgreement
{
    Id = Guid.NewGuid().ToString(),
    ProcessorId = "stripe-payments",
    Status = DPAStatus.Active,
    SignedAtUtc = DateTimeOffset.UtcNow,
    ExpiresAtUtc = DateTimeOffset.UtcNow.AddYears(2),
    HasSCCs = true,
    ProcessingPurposes = ["Payment processing", "Fraud detection"],
    MandatoryTerms = new DPAMandatoryTerms
    {
        ProcessOnDocumentedInstructions = true,
        ConfidentialityObligations = true,
        SecurityMeasures = true,
        SubProcessorRequirements = true,
        DataSubjectRightsAssistance = true,
        ComplianceAssistance = true,
        DataDeletionOrReturn = true,
        AuditRights = true
    },
    CreatedAtUtc = DateTimeOffset.UtcNow,
    LastUpdatedAtUtc = DateTimeOffset.UtcNow
};

await store.AddAsync(dpa);
```

### 4. Mark Requests Requiring Valid DPA

```csharp
[RequiresProcessor(ProcessorId = "stripe-payments")]
public sealed record ProcessPaymentCommand(decimal Amount, string Currency)
    : ICommand<Either<EncinaError, PaymentResult>>;
```

### 5. Validate Compliance

```csharp
var validator = serviceProvider.GetRequiredService<IDPAValidator>();

// Quick check (hot path)
var hasValid = await validator.HasValidDPAAsync("stripe-payments");

// Detailed validation
var result = await validator.ValidateAsync("stripe-payments");
result.Match(
    Right: validation =>
    {
        Console.WriteLine($"Valid: {validation.IsValid}");
        Console.WriteLine($"Missing terms: {string.Join(", ", validation.MissingTerms)}");
        Console.WriteLine($"Days until expiration: {validation.DaysUntilExpiration}");
    },
    Left: error => Console.WriteLine($"Error: {error.Message}")
);
```

### 6. Track Sub-Processors

```csharp
var subProcessor = new Processor
{
    Id = "aws-hosting",
    Name = "Amazon Web Services",
    Country = "US",
    ParentProcessorId = "stripe-payments",
    Depth = 1,
    SubProcessorAuthorizationType = SubProcessorAuthorizationType.General,
    CreatedAtUtc = DateTimeOffset.UtcNow,
    LastUpdatedAtUtc = DateTimeOffset.UtcNow
};

await registry.RegisterProcessorAsync(subProcessor);

// Get direct sub-processors
var subs = await registry.GetSubProcessorsAsync("stripe-payments");

// Get full hierarchy (recursive)
var chain = await registry.GetFullSubProcessorChainAsync("stripe-payments");
```

## Configuration

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `EnforcementMode` | `ProcessorAgreementEnforcementMode` | `Warn` | How to handle requests without valid DPA |
| `BlockWithoutValidDPA` | `bool` | `false` | Convenience alias — sets `EnforcementMode = Block` |
| `MaxSubProcessorDepth` | `int` | `3` | Maximum sub-processor hierarchy depth (1-10) |
| `EnableExpirationMonitoring` | `bool` | `false` | Enable scheduled expiration checks |
| `ExpirationCheckInterval` | `TimeSpan` | `1 hour` | Frequency of expiration checks |
| `ExpirationWarningDays` | `int` | `30` | Days before expiration to trigger warnings |
| `TrackAuditTrail` | `bool` | `true` | Record audit entries for all operations |
| `AddHealthCheck` | `bool` | `false` | Register health check in DI |

## Pipeline Behavior

The `ProcessorValidationPipelineBehavior` intercepts requests decorated with `[RequiresProcessor]`:

```
Request → [RequiresProcessor] found? → IDPAValidator.HasValidDPAAsync()
                                           ├── Valid DPA → Continue pipeline
                                           ├── Invalid (Block mode) → Return EncinaError
                                           └── Invalid (Warn mode) → Log warning, continue
```

Requests without `[RequiresProcessor]` bypass all checks with zero overhead.

## Database Providers

The core package includes in-memory implementations for development and testing. For production, use a database-backed provider:

### EF Core

```csharp
services.AddEncinaEntityFrameworkCore<AppDbContext>(config =>
{
    config.UseProcessorAgreements = true;
});
```

### ADO.NET

```csharp
// SQL Server
services.AddEncinaAdoSqlServer(connectionString, config =>
{
    config.UseProcessorAgreements = true;
});

// PostgreSQL
services.AddEncinaAdoPostgreSQL(connectionString, config =>
{
    config.UseProcessorAgreements = true;
});
```

### Dapper

```csharp
services.AddEncinaDapperSqlServer(connectionString, config =>
{
    config.UseProcessorAgreements = true;
});
```

### MongoDB

```csharp
services.AddEncinaMongoDB(connectionString, config =>
{
    config.UseProcessorAgreements = true;
});
```

## Error Codes

| Code | Description |
|------|-------------|
| `processor.not_found` | Processor not in registry |
| `processor.already_exists` | Duplicate processor ID |
| `processor.dpa_not_found` | DPA not found by ID |
| `processor.dpa_missing` | No active DPA for processor |
| `processor.dpa_expired` | DPA past expiration date |
| `processor.dpa_terminated` | DPA explicitly terminated |
| `processor.dpa_pending_renewal` | DPA approaching expiration |
| `processor.dpa_incomplete` | Missing mandatory Article 28(3) terms |
| `processor.sub_processor_unauthorized` | Sub-processor not authorized |
| `processor.sub_processor_depth_exceeded` | Hierarchy depth limit exceeded |
| `processor.scc_required` | Standard Contractual Clauses required |
| `processor.store_error` | Persistence layer failure |
| `processor.validation_failed` | General validation failure |

## Observability

### Tracing

OpenTelemetry activities via `Encina.Compliance.ProcessorAgreements` ActivitySource with tags: `processor.id`, `processor.name`, `dpa.id`, `dpa.status`, `enforcement.mode`.

### Health Check

```csharp
options.AddHealthCheck = true;
// Tags: "encina", "compliance", "processor-agreements", "ready"
```

Verifies `IDPAValidator`, `IProcessorRegistry`, `IDPAStore`, and `IProcessorAuditStore` are resolvable from DI.

## Testing

```csharp
// Use in-memory stores for unit tests (registered by default)
services.AddEncinaProcessorAgreements(options =>
{
    options.EnforcementMode = ProcessorAgreementEnforcementMode.Block;
});

// All stores use ConcurrentDictionary — thread-safe for parallel tests
```

## Related Packages

| Package | Purpose |
|---------|---------|
| `Encina.Compliance.GDPR` | Core GDPR abstractions, RoPA |
| `Encina.Compliance.Consent` | Consent management (Art. 7) |
| `Encina.Compliance.DPIA` | Impact assessment (Art. 35) |
| `Encina.Compliance.DataSubjectRights` | Subject rights (Arts. 15-22) |
| `Encina.Compliance.DataResidency` | Data sovereignty (Ch. V) |
| `Encina.Compliance.Retention` | Retention policies (Art. 5(1)(e)) |

## GDPR Article 28 Reference

| Article | Topic | Coverage |
|---------|-------|----------|
| Art. 28(1) | Processor selection | `IProcessorRegistry` with validation |
| Art. 28(2) | Sub-processor authorization | `SubProcessorAuthorizationType` (Specific/General) |
| Art. 28(3)(a) | Documented instructions | `DPAMandatoryTerms.ProcessOnDocumentedInstructions` |
| Art. 28(3)(b) | Confidentiality | `DPAMandatoryTerms.ConfidentialityObligations` |
| Art. 28(3)(c) | Security measures | `DPAMandatoryTerms.SecurityMeasures` |
| Art. 28(3)(d) | Sub-processor requirements | `DPAMandatoryTerms.SubProcessorRequirements` |
| Art. 28(3)(e) | Data subject rights | `DPAMandatoryTerms.DataSubjectRightsAssistance` |
| Art. 28(3)(f) | Compliance assistance | `DPAMandatoryTerms.ComplianceAssistance` |
| Art. 28(3)(g) | Data deletion/return | `DPAMandatoryTerms.DataDeletionOrReturn` |
| Art. 28(3)(h) | Audit rights | `DPAMandatoryTerms.AuditRights` |
| Art. 5(2) | Accountability | `IProcessorAuditStore` audit trail |

## License

MIT - See [LICENSE](../../LICENSE) for details.
