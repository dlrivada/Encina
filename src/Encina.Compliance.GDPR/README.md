# Encina.Compliance.GDPR

[![NuGet](https://img.shields.io/nuget/v/Encina.Compliance.GDPR.svg)](https://www.nuget.org/packages/Encina.Compliance.GDPR/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](../../LICENSE)

GDPR compliance abstractions and pipeline behavior for Encina. Provides declarative, attribute-based processing activity tracking with automatic Record of Processing Activities (RoPA) generation, ensuring GDPR Article 30 compliance at the CQRS pipeline level.

## Features

- **Declarative Processing Activities** - Attribute-based registration of personal data processing activities
- **Automatic RoPA Generation** - Export Record of Processing Activities in JSON or CSV formats
- **Pipeline Enforcement** - Compliance validation at the CQRS pipeline level via `GDPRCompliancePipelineBehavior`
- **Railway Oriented Programming** - Compliance failures return `EncinaError`, no exceptions
- **Enforcement Modes** - `Enforce` (block non-compliant requests) or `WarnOnly` (log and proceed)
- **Auto-Registration** - Scan assemblies for `[ProcessingActivity]` attributes at startup
- **Full Observability** - OpenTelemetry tracing, structured logging, health check
- **Extensible** - Custom `IProcessingActivityRegistry` and `IGDPRComplianceValidator` implementations
- **.NET 10 Compatible** - Built with latest C# features

## Installation

```bash
dotnet add package Encina.Compliance.GDPR
```

## Quick Start

### 1. Register Services

```csharp
services.AddEncinaGDPR(options =>
{
    options.ControllerName = "Acme Corp";
    options.ControllerEmail = "privacy@acme.com";
    options.DataProtectionOfficer = new DataProtectionOfficer("Jane Doe", "dpo@acme.com", "+1-555-0100");
    options.BlockUnregisteredProcessing = true;
    options.EnforcementMode = GDPREnforcementMode.Enforce;
    options.AssembliesToScan.Add(typeof(Program).Assembly);
});
```

### 2. Decorate Request Types

```csharp
// Full processing activity declaration
[ProcessingActivity(
    Purpose = "Process customer orders",
    LawfulBasis = LawfulBasis.Contract,
    DataCategories = ["Name", "Email", "Address"],
    DataSubjects = ["Customers"],
    RetentionDays = 730,
    SecurityMeasures = "AES-256 encryption at rest")]
public sealed record PlaceOrderCommand(OrderData Data) : ICommand<OrderId>;

// Marker-only: flags that personal data is processed (requires registry entry)
[ProcessesPersonalData]
public sealed record ExportUserDataQuery(Guid UserId) : IQuery<UserDataExport>;

// No attribute: pipeline skips GDPR checks entirely
public sealed record GetPublicCatalogQuery() : IQuery<CatalogDto>;
```

### 3. Export RoPA

```csharp
var registry = serviceProvider.GetRequiredService<IProcessingActivityRegistry>();
var exporter = serviceProvider.GetRequiredService<JsonRoPAExporter>();

var activitiesResult = await registry.GetAllActivitiesAsync();
var activities = activitiesResult.Match(Right: a => a, Left: _ => []);

var metadata = new RoPAExportMetadata(
    ControllerName: "Acme Corp",
    ControllerEmail: "privacy@acme.com",
    ExportedAtUtc: DateTimeOffset.UtcNow,
    DataProtectionOfficer: new DataProtectionOfficer("Jane Doe", "dpo@acme.com"));

var result = await exporter.ExportAsync(activities, metadata);
// result contains JSON byte[] with the full RoPA
```

## Processing Activity Attributes

| Attribute | Description | Pipeline Behavior |
|-----------|-------------|-------------------|
| `[ProcessingActivity(...)]` | Full GDPR Article 30 declaration with purpose, lawful basis, categories, retention | Validates compliance, logs activity |
| `[ProcessesPersonalData]` | Marker indicating personal data processing (requires registry entry) | Checks registry, enforces if configured |
| *(none)* | No personal data processing | Pipeline skips all GDPR checks |

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ControllerName` | `string?` | `null` | Organization name (Article 30(1)(a)) |
| `ControllerEmail` | `string?` | `null` | Controller contact email |
| `DataProtectionOfficer` | `IDataProtectionOfficer?` | `null` | DPO contact details (Articles 37-39) |
| `BlockUnregisteredProcessing` | `bool` | `false` | Block requests without registry entry |
| `EnforcementMode` | `GDPREnforcementMode` | `Enforce` | `Enforce` or `WarnOnly` |
| `AutoRegisterFromAttributes` | `bool` | `true` | Scan assemblies at startup |
| `AddHealthCheck` | `bool` | `false` | Register GDPR health check |
| `AssembliesToScan` | `List<Assembly>` | `[]` | Assemblies for auto-registration |

## Lawful Basis (Article 6(1))

| Value | GDPR Article |
|-------|-------------|
| `Consent` | 6(1)(a) - Data subject consent |
| `Contract` | 6(1)(b) - Contractual necessity |
| `LegalObligation` | 6(1)(c) - Legal obligation |
| `VitalInterests` | 6(1)(d) - Vital interests |
| `PublicTask` | 6(1)(e) - Public interest/authority |
| `LegitimateInterests` | 6(1)(f) - Legitimate interests |

## Error Codes

| Code | Meaning |
|------|---------|
| `gdpr.unregistered_activity` | Request processes personal data but has no registry entry |
| `gdpr.compliance_validation_failed` | Compliance validator reported non-compliance |
| `gdpr.registry_lookup_failed` | Failed to look up processing activity in registry |
| `gdpr.ropa_export_serialization_failed` | RoPA export serialization error |

## Custom Implementations

Register custom implementations before `AddEncinaGDPR()` to override defaults (TryAdd semantics):

```csharp
// Custom registry (e.g., database-backed)
services.AddSingleton<IProcessingActivityRegistry, DatabaseProcessingActivityRegistry>();

// Custom compliance validator
services.AddScoped<IGDPRComplianceValidator, MyComplianceValidator>();

services.AddEncinaGDPR(options =>
{
    options.AutoRegisterFromAttributes = false; // Manual registry management
});
```

## Observability

- **Tracing**: `Encina.Compliance.GDPR` ActivitySource with processing activity tags
- **Logging**: Structured log events via `LoggerMessage.Define` (zero-allocation)
- **Health Check**: Verifies registry population, controller info, and validator registration

## Related Packages

| Package | Description |
|---------|-------------|
| `Encina` | Core CQRS pipeline with `IPipelineBehavior` |
| `Encina.Security` | Transport-agnostic authorization pipeline |

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.
