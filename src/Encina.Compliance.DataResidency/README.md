# Encina.Compliance.DataResidency

[![NuGet](https://img.shields.io/nuget/v/Encina.Compliance.DataResidency.svg)](https://www.nuget.org/packages/Encina.Compliance.DataResidency/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](../../LICENSE)

GDPR Chapter V (Articles 44-49) data sovereignty and residency enforcement for Encina. Provides declarative, attribute-based data residency controls at the CQRS pipeline level with region-based routing, cross-border transfer validation, data location tracking, and immutable audit trail for transfer accountability.

## Features

- **Declarative Residency Policies** -- `[DataResidency("healthcare-data", AllowedRegions = "DE,FR,NL")]` attribute on request types
- **Cross-Border Transfer Validation** -- Five-step GDPR hierarchy: same country, EU/EEA, adequacy decision, allowed safeguards, deny
- **Pipeline-Level Enforcement** -- `DataResidencyPipelineBehavior` validates residency and transfer compliance before processing
- **Region Registry** -- 50+ pre-defined regions (27 EU, 3 EEA, 15+ adequacy countries) with `RegionRegistry.GetByCode`
- **Fluent Policy Configuration** -- `AddPolicy()` builder API with `AllowEU()`, `AllowEEA()`, `AllowAdequate()`, `RequireAdequacyDecision()`
- **Three Enforcement Modes** -- `Block` (reject), `Warn` (log and proceed), `Disabled` (no-op)
- **Data Location Tracking** -- Record where data is stored for GDPR Article 30 compliance
- **No-Cross-Border Attribute** -- `[NoCrossBorderTransfer]` for same-region-only processing
- **Immutable Audit Trail** -- Every residency decision recorded via `IResidencyAuditStore`
- **Railway Oriented Programming** -- All operations return `Either<EncinaError, T>`, no exceptions
- **Full Observability** -- OpenTelemetry tracing, 7 counters, 2 histograms, 35 structured log events, health check
- **13 Database Providers** -- ADO.NET, Dapper, EF Core (SQLite, SQL Server, PostgreSQL, MySQL) + MongoDB
- **.NET 10 Compatible** -- Built with latest C# features

## Installation

```bash
dotnet add package Encina.Compliance.DataResidency
```

## Quick Start

### 1. Register Services

```csharp
services.AddEncina(config =>
    config.RegisterServicesFromAssemblyContaining<Program>());

services.AddEncinaDataResidency(options =>
{
    options.DefaultRegion = RegionRegistry.DE;
    options.EnforcementMode = DataResidencyEnforcementMode.Block;
    options.TrackDataLocations = true;
    options.TrackAuditTrail = true;
    options.AutoRegisterFromAttributes = true;
    options.AssembliesToScan.Add(typeof(Program).Assembly);
});
```

### 2. Mark Data with Residency Attributes

```csharp
// Healthcare data must stay in EU
[DataResidency("healthcare-data", AllowedRegions = "DE,FR,NL",
    RequireAdequacyDecision = true)]
public sealed record GetPatientRecordsQuery(string PatientId) : IRequest<PatientRecords>;

// Financial data restricted to specific regions
[DataResidency("financial-data", AllowedRegions = "DE,FR,LU,CH")]
public sealed record ProcessPaymentCommand(string OrderId) : IRequest<PaymentResult>;

// Prevent any cross-border transfer
[NoCrossBorderTransfer]
public sealed record GetClassifiedDocumentQuery(string DocId) : IRequest<Document>;
```

### 3. Configure Policies via Fluent API

```csharp
services.AddEncinaDataResidency(options =>
{
    options.DefaultRegion = RegionRegistry.DE;

    options.AddPolicy("healthcare-data", policy =>
    {
        policy.AllowEU();
        policy.RequireAdequacyDecision();
    });

    options.AddPolicy("financial-data", policy =>
    {
        policy.AllowRegions(RegionRegistry.DE, RegionRegistry.FR, RegionRegistry.LU);
        policy.AllowTransferBasis(TransferLegalBasis.StandardContractualClauses);
    });

    options.AddPolicy("general-data", policy =>
    {
        policy.AllowEEA();
        policy.AllowAdequate();
    });
});
```

### 4. Cross-Border Transfer Validation

```csharp
var validator = serviceProvider.GetRequiredService<ICrossBorderTransferValidator>();

// Validate a transfer from Germany to the US
var result = await validator.ValidateTransferAsync(
    source: RegionRegistry.DE,
    destination: RegionRegistry.US,
    dataCategory: "healthcare-data",
    cancellationToken);

result.Match(
    Right: transfer => Console.WriteLine(transfer.IsAllowed
        ? $"Transfer allowed: {transfer.LegalBasis}"
        : $"Transfer denied: {transfer.Reason}"),
    Left: error => Console.WriteLine($"Validation error: {error.Message}"));
```

### 5. Region Routing

```csharp
var router = serviceProvider.GetRequiredService<IRegionRouter>();

// Determine where a request should be processed
var regionResult = await router.DetermineTargetRegionAsync<GetPatientRecordsQuery>(
    new GetPatientRecordsQuery("patient-123"),
    cancellationToken);

regionResult.Match(
    Right: region => Console.WriteLine($"Route to: {region.Code} ({region.Name})"),
    Left: error => Console.WriteLine($"Routing failed: {error.Message}"));
```

## Enforcement Modes

| Mode | Behavior | Use Case |
|------|----------|----------|
| `Block` | Non-compliant transfers/storage are rejected with `EncinaError` | Production (recommended) |
| `Warn` | Log warning, allow response to proceed | Migration/testing phase (default) |
| `Disabled` | Skip all residency checks | Development environments |

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `DefaultRegion` | `Region?` | `null` | Default deployment region (fallback for region resolution) |
| `EnforcementMode` | `DataResidencyEnforcementMode` | `Warn` | How to handle residency violations |
| `TrackDataLocations` | `bool` | `true` | Record data locations via `IDataLocationStore` |
| `TrackAuditTrail` | `bool` | `true` | Record all residency decisions in audit store |
| `BlockNonCompliantTransfers` | `bool` | `true` | Deny transfers without adequate legal basis |
| `AddHealthCheck` | `bool` | `false` | Register health check with `IHealthChecksBuilder` |
| `AutoRegisterFromAttributes` | `bool` | `true` | Scan assemblies for `[DataResidency]` at startup |
| `AssembliesToScan` | `List<Assembly>` | `[]` | Assemblies to scan for `[DataResidency]` attributes |
| `AdditionalAdequateRegions` | `List<Region>` | `[]` | Custom regions to treat as having adequacy decisions |

## Error Codes

| Code | Meaning |
|------|---------|
| `residency.region_not_allowed` | Target region is not allowed for the data category |
| `residency.cross_border_denied` | Cross-border transfer denied (no adequate legal basis) |
| `residency.region_not_resolved` | Could not determine the current deployment region |
| `residency.policy_not_found` | No residency policy defined for the data category |
| `residency.policy_already_exists` | A policy already exists for the data category |
| `residency.store_error` | Persistence store operation failed |
| `residency.transfer_validation_failed` | Transfer validation could not be performed |

## Custom Implementations

Register custom implementations before `AddEncinaDataResidency()` to override defaults (TryAdd semantics):

```csharp
// Custom store implementations (e.g., database-backed)
services.AddSingleton<IResidencyPolicyStore, DatabaseResidencyPolicyStore>();
services.AddSingleton<IDataLocationStore, DatabaseDataLocationStore>();
services.AddSingleton<IResidencyAuditStore, DatabaseResidencyAuditStore>();

// Custom service implementations
services.AddSingleton<IRegionContextProvider, HttpHeaderRegionContextProvider>();
services.AddSingleton<IAdequacyDecisionProvider, CustomAdequacyDecisionProvider>();

services.AddEncinaDataResidency(options =>
{
    options.EnforcementMode = DataResidencyEnforcementMode.Block;
    options.AutoRegisterFromAttributes = false;
});
```

## Database Providers

The core package ships with `InMemoryResidencyPolicyStore`, `InMemoryDataLocationStore`, and `InMemoryResidencyAuditStore` for development and testing. Database-backed implementations for the 13 providers are available via satellite packages:

```csharp
// ADO.NET (SQLite example)
services.AddEncinaADO(config =>
{
    config.UseDataResidency = true;
});

// Dapper (SQL Server example)
services.AddEncinaDapper(config =>
{
    config.UseDataResidency = true;
});

// EF Core (PostgreSQL example)
services.AddEncinaEntityFrameworkCore<AppDbContext>(config =>
{
    config.UseDataResidency = true;
});
```

## Observability

- **Tracing**: `Encina.Compliance.DataResidency` ActivitySource with 3 activity types (`Residency.Pipeline`, `Residency.TransferValidation`, `Residency.LocationRecord`)
- **Metrics**: 7 counters (`residency.pipeline.executions.total`, `residency.policy.checks.total`, `residency.transfers.total`, `residency.transfers.blocked.total`, `residency.locations.recorded.total`, `residency.violations.total`, `residency.audit.entries.total`) and 2 histograms (`residency.pipeline.duration`, `residency.transfer.validation.duration`)
- **Logging**: 35 structured log events via `[LoggerMessage]` source generator (zero-allocation), event IDs 8600-8674
- **Health Check**: Verifies store connectivity, required services, and options configuration

## Health Check

Enable via `DataResidencyOptions.AddHealthCheck`:

```csharp
services.AddEncinaDataResidency(options =>
{
    options.AddHealthCheck = true;
});
```

The health check (`encina-data-residency`) verifies:
- `DataResidencyOptions` are configured
- `IResidencyPolicyStore` is resolvable
- `IDataLocationStore` is resolvable
- `ICrossBorderTransferValidator` is resolvable
- `IResidencyAuditStore` is resolvable when `TrackAuditTrail` is enabled

Tags: `encina`, `compliance`, `data-residency`, `ready`

## Testing

```csharp
// Use in-memory stores for unit testing (registered by default)
services.AddEncinaDataResidency(options =>
{
    options.DefaultRegion = RegionRegistry.DE;
    options.EnforcementMode = DataResidencyEnforcementMode.Block;
    options.AddPolicy("test-data", p => p.AllowEU().RequireAdequacyDecision());
});
```

## Related Packages

| Package | Description |
|---------|-------------|
| `Encina` | Core CQRS pipeline with `IPipelineBehavior` |
| `Encina.Compliance.GDPR` | GDPR processing activity tracking and RoPA |
| `Encina.Compliance.Consent` | GDPR Article 7 consent management |
| `Encina.Compliance.DataSubjectRights` | GDPR Articles 15-22 data subject rights management |
| `Encina.Compliance.Retention` | GDPR Article 5(1)(e) data retention management |
| `Encina.Compliance.Anonymization` | GDPR Article 4(5) data anonymization and pseudonymization |
| `Encina.Compliance.LawfulBasis` | GDPR Article 6 lawful basis tracking |

## GDPR Compliance

This package implements key GDPR requirements:

| Article | Requirement | Implementation |
|---------|-------------|----------------|
| **44** | General principle for transfers | `DataResidencyPipelineBehavior`, `IDataResidencyPolicy` |
| **45** | Adequacy decisions | `IAdequacyDecisionProvider`, `RegionRegistry.AdequacyCountries` |
| **46** | Appropriate safeguards (SCCs, BCRs) | `ICrossBorderTransferValidator`, `TransferLegalBasis` |
| **47** | Binding corporate rules | `TransferLegalBasis.BindingCorporateRules` |
| **49** | Derogations for specific situations | `TransferLegalBasis.ExplicitConsent`, `.PublicInterest`, `.VitalInterests` |
| **5(2)** | Accountability | `IResidencyAuditStore`, `TrackAuditTrail` option |
| **30** | Records of processing activities | `IDataLocationStore`, `TrackDataLocations` option |

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.
