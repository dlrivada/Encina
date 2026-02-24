# Encina.Compliance.GDPR

[![NuGet](https://img.shields.io/nuget/v/Encina.Compliance.GDPR.svg)](https://www.nuget.org/packages/Encina.Compliance.GDPR/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](../../LICENSE)

GDPR compliance abstractions and pipeline behaviors for Encina. Provides declarative, attribute-based processing activity tracking with automatic Record of Processing Activities (RoPA) generation (Article 30), and lawful basis validation with Legitimate Interest Assessment support (Article 6).

## Features

- **Declarative Processing Activities** - Attribute-based registration of personal data processing activities
- **Automatic RoPA Generation** - Export Record of Processing Activities in JSON or CSV formats
- **Lawful Basis Validation** - Attribute-driven Article 6(1) enforcement at the pipeline level
- **Legitimate Interest Assessment** - Full EDPB three-part test support with persistent LIA records
- **Consent Integration** - Bridge pattern to validate active consent for consent-based processing
- **Pipeline Enforcement** - Compliance validation at the CQRS pipeline level via `GDPRCompliancePipelineBehavior` and `LawfulBasisValidationPipelineBehavior`
- **Railway Oriented Programming** - Compliance failures return `EncinaError`, no exceptions
- **Enforcement Modes** - `Block` (reject non-compliant), `Warn` (log and proceed), or `Disabled`
- **Auto-Registration** - Scan assemblies for `[ProcessingActivity]` and `[LawfulBasis]` attributes at startup
- **Full Observability** - OpenTelemetry tracing, structured logging, health checks
- **13 Persistence Providers** - ADO.NET, Dapper, EF Core (4 databases each) + MongoDB
- **Extensible** - Custom `IProcessingActivityRegistry`, `ILawfulBasisRegistry`, `ILIAStore` implementations
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

## Configuration Options (Processing Activities)

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

## Lawful Basis Validation

The `[LawfulBasis]` attribute declares the legal ground for processing personal data on any Encina request. When `LawfulBasisValidationPipelineBehavior` is registered, it validates that every request touching personal data has a valid, registered lawful basis.

### Attribute Examples

```csharp
// Contract-based processing
[LawfulBasis(LawfulBasis.Contract)]
public sealed record ProcessOrderCommand(string OrderId) : ICommand<Result>;

// Consent-based with explicit purpose
[LawfulBasis(LawfulBasis.Consent, Purpose = "Send marketing newsletters")]
public sealed record SendNewsletterCommand(string Email) : ICommand;

// Legitimate interests with LIA reference
[LawfulBasis(LawfulBasis.LegitimateInterests,
    Purpose = "Fraud detection and prevention",
    LIAReference = "LIA-2024-FRAUD-001")]
public sealed record AnalyzeTransactionCommand(Guid TransactionId) : ICommand<FraudScore>;

// Legal obligation with statute reference
[LawfulBasis(LawfulBasis.LegalObligation,
    Purpose = "Tax reporting compliance",
    LegalReference = "EU VAT Directive 2006/112/EC")]
public sealed record GenerateTaxReportCommand(int Year) : ICommand<TaxReport>;

// Contract with terms reference
[LawfulBasis(LawfulBasis.Contract,
    Purpose = "Fulfill customer orders",
    ContractReference = "Terms of Service v2.1")]
public sealed record CreateOrderCommand(OrderData Data) : ICommand<OrderId>;
```

### Attribute Properties

| Property | Required | Relevant Basis | Description |
|----------|----------|----------------|-------------|
| `Basis` | Yes | All | The Article 6(1) lawful basis |
| `Purpose` | Recommended | All | Human-readable processing purpose |
| `LIAReference` | Yes* | `LegitimateInterests` | Reference to a Legitimate Interest Assessment |
| `LegalReference` | Recommended | `LegalObligation` | Specific law, regulation, or directive |
| `ContractReference` | Recommended | `Contract` | Contract or terms of service reference |

*Required when `ValidateLIAForLegitimateInterests` is `true` (default).

### Precedence Over `[ProcessingActivity]`

When both `[LawfulBasis]` and `[ProcessingActivity]` are present on a request type, `[LawfulBasis]` takes precedence for lawful basis resolution. If the two attributes declare different bases, a warning is logged (EventId 8207) and `[LawfulBasis]` wins.

```csharp
// [LawfulBasis] wins — basis is LegitimateInterests, not Contract
[ProcessingActivity(LawfulBasis = LawfulBasis.Contract, Purpose = "Order processing")]
[LawfulBasis(LawfulBasis.LegitimateInterests, LIAReference = "LIA-001")]
public sealed record ProcessOrderCommand(Guid Id) : ICommand<Result>;
```

### Programmatic Registration

For request types that cannot use attributes (third-party types, generated code), register a default basis via options:

```csharp
services.AddEncinaLawfulBasis(options =>
{
    options.DefaultBasis<GetUserProfileQuery>(LawfulBasis.Contract);
    options.DefaultBasis<AnalyticsCommand>(LawfulBasis.LegitimateInterests);
});
```

Attribute-based registrations take priority over programmatic defaults.

## Configuration Options (Lawful Basis)

```csharp
services.AddEncinaLawfulBasis(options =>
{
    options.EnforcementMode = LawfulBasisEnforcementMode.Warn;
    options.RequireDeclaredBasis = true;
    options.ValidateConsentForConsentBasis = true;
    options.ValidateLIAForLegitimateInterests = true;
    options.AutoRegisterFromAttributes = true;
    options.AddHealthCheck = true;
    options.ScanAssemblyContaining<Program>();
});
```

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `EnforcementMode` | `LawfulBasisEnforcementMode` | `Block` | `Block` (reject), `Warn` (log and proceed), or `Disabled` (no-op) |
| `RequireDeclaredBasis` | `bool` | `true` | Require all personal-data requests to have a declared basis |
| `ValidateConsentForConsentBasis` | `bool` | `true` | Check active consent via `IConsentStatusProvider` for consent-based processing |
| `ValidateLIAForLegitimateInterests` | `bool` | `true` | Validate LIA existence and approval for legitimate interests |
| `AutoRegisterFromAttributes` | `bool` | `true` | Scan assemblies for `[LawfulBasis]` attributes at startup |
| `AddHealthCheck` | `bool` | `false` | Register lawful basis health check |
| `AssembliesToScan` | `HashSet<Assembly>` | `[]` | Assemblies to scan for auto-registration |
| `DefaultBases` | `Dictionary<Type, LawfulBasis>` | `{}` | Programmatic basis registrations |

### Enforcement Modes

| Mode | Behavior | Use Case |
|------|----------|----------|
| `Block` | Rejects requests without valid lawful basis | Production with full compliance |
| `Warn` | Logs warning but allows processing | Migration phase, gradual adoption |
| `Disabled` | Pipeline behavior is a no-op | Development, external compliance management |

## Legitimate Interest Assessment (LIA)

Article 6(1)(f) requires a documented assessment demonstrating that the controller's legitimate interests are not overridden by the data subject's rights. Encina implements the EDPB three-part test:

### EDPB Three-Part Test

| Test | Fields | Question |
|------|--------|----------|
| **1. Purpose** | `LegitimateInterest`, `Benefits`, `ConsequencesIfNotProcessed` | Is the interest legitimate and clearly articulated? |
| **2. Necessity** | `NecessityJustification`, `AlternativesConsidered`, `DataMinimisationNotes` | Is the processing necessary and proportionate? |
| **3. Balancing** | `NatureOfData`, `ReasonableExpectations`, `ImpactAssessment`, `Safeguards` | Do the individual's rights override the interest? |

### LIARecord Fields

| Category | Field | Type | Description |
|----------|-------|------|-------------|
| **Identification** | `Id` | `string` | Unique reference (e.g., `"LIA-2024-FRAUD-001"`) |
| | `Name` | `string` | Human-readable name |
| | `Purpose` | `string` | Processing purpose this LIA covers |
| **Purpose Test** | `LegitimateInterest` | `string` | Description of the legitimate interest |
| | `Benefits` | `string` | Benefits to controller, subject, or third parties |
| | `ConsequencesIfNotProcessed` | `string` | What happens without this processing |
| **Necessity Test** | `NecessityJustification` | `string` | Why processing is necessary |
| | `AlternativesConsidered` | `IReadOnlyList<string>` | Less intrusive alternatives evaluated |
| | `DataMinimisationNotes` | `string` | Data minimisation measures applied |
| **Balancing Test** | `NatureOfData` | `string` | Nature of personal data processed |
| | `ReasonableExpectations` | `string` | Data subject's reasonable expectations |
| | `ImpactAssessment` | `string` | Impact on rights and freedoms |
| | `Safeguards` | `IReadOnlyList<string>` | Safeguards to mitigate impact |
| **Outcome** | `Outcome` | `LIAOutcome` | `Approved`, `Rejected`, or `RequiresReview` |
| | `Conclusion` | `string` | Summary conclusion |
| | `Conditions` | `string?` | Conditions attached to approval |
| **Governance** | `AssessedAtUtc` | `DateTimeOffset` | When the assessment was conducted |
| | `AssessedBy` | `string` | Person who conducted the assessment |
| | `DPOInvolvement` | `bool` | Whether the DPO was consulted |
| | `NextReviewAtUtc` | `DateTimeOffset?` | Next periodic review date |

### Creating and Storing a LIA

```csharp
// 1. Create the LIA record
var lia = new LIARecord
{
    Id = "LIA-2024-FRAUD-001",
    Name = "Fraud Detection LIA",
    Purpose = "Detect and prevent fraudulent transactions",
    // Purpose test
    LegitimateInterest = "Protect financial integrity and prevent losses",
    Benefits = "Reduced fraud, lower chargebacks, safer transactions",
    ConsequencesIfNotProcessed = "Significant financial losses and increased fraud risk",
    // Necessity test
    NecessityJustification = "Real-time analysis is essential for fraud prevention",
    AlternativesConsidered = ["Manual review", "Post-hoc analysis"],
    DataMinimisationNotes = "Only transaction metadata is analyzed",
    // Balancing test
    NatureOfData = "Transaction amounts, timestamps, merchant categories",
    ReasonableExpectations = "Customers expect their bank to protect against fraud",
    ImpactAssessment = "Minimal impact; benefits significantly outweigh risks",
    Safeguards = ["Automated alerts only", "Human review before account action", "Data encrypted at rest"],
    // Outcome
    Outcome = LIAOutcome.Approved,
    Conclusion = "Legitimate interest outweighs minimal impact on data subjects",
    // Governance
    AssessedBy = "Data Protection Officer",
    AssessedAtUtc = DateTimeOffset.UtcNow,
    NextReviewAtUtc = DateTimeOffset.UtcNow.AddYears(1)
};

// 2. Store it via ILIAStore
var liaStore = serviceProvider.GetRequiredService<ILIAStore>();
var result = await liaStore.StoreAsync(lia);

// 3. Reference it from your request type
[LawfulBasis(LawfulBasis.LegitimateInterests,
    Purpose = "Fraud detection and prevention",
    LIAReference = "LIA-2024-FRAUD-001")]
public sealed record AnalyzeTransactionCommand(Guid Id) : ICommand<FraudScore>;
```

The pipeline behavior automatically validates that the referenced LIA exists and is approved when processing a request with `LawfulBasis.LegitimateInterests`.

## Consent Integration

When the lawful basis is `Consent`, the pipeline behavior can validate that the data subject has active consent. This uses a bridge pattern between the GDPR and Consent modules.

### Architecture

```
Encina.Compliance.GDPR                    Encina.Compliance.Consent
├── IConsentStatusProvider (interface)     ├── ConsentStatusProviderAdapter (implements IConsentStatusProvider)
├── ILawfulBasisSubjectIdExtractor        └── Registered automatically by AddEncinaConsent()
└── LawfulBasisValidationPipelineBehavior
```

### Setup

```csharp
// 1. Register consent management (automatically registers the adapter)
services.AddEncinaConsent(options =>
{
    options.ControllerName = "Acme Corp";
});

// 2. Register lawful basis validation
services.AddEncinaLawfulBasis(options =>
{
    options.ValidateConsentForConsentBasis = true; // default
    options.ScanAssemblyContaining<Program>();
});

// 3. Decorate your request
[LawfulBasis(LawfulBasis.Consent, Purpose = "Send marketing newsletters")]
public sealed record SendNewsletterCommand(string Email) : ICommand;
```

When `ValidateConsentForConsentBasis` is `true`:
1. The behavior extracts the subject ID via `ILawfulBasisSubjectIdExtractor`
2. Queries `IConsentStatusProvider.HasActiveConsentAsync()` for the declared purpose
3. Blocks or warns if no active consent is found (depending on enforcement mode)

If no `IConsentStatusProvider` is registered, a warning is logged (EventId 8209) and consent validation is skipped.

## Persistence Providers

The in-memory `ILawfulBasisRegistry` and `ILIAStore` are suitable for development. For production, use one of the 13 database-backed providers:

### ADO.NET Providers

```bash
dotnet add package Encina.ADO.Sqlite      # or SqlServer, PostgreSQL, MySQL
```

```csharp
services.AddEncinaLawfulBasis();
services.AddEncinaLawfulBasisADOSqlite(connectionString);
```

### Dapper Providers

```bash
dotnet add package Encina.Dapper.PostgreSQL  # or Sqlite, SqlServer, MySQL
```

```csharp
services.AddEncinaLawfulBasis();
services.AddEncinaLawfulBasisDapperPostgreSQL(connectionString);
```

### EF Core Provider

```bash
dotnet add package Encina.EntityFrameworkCore
```

```csharp
// DbContext must call modelBuilder.ApplyLawfulBasisConfiguration()
services.AddEncinaLawfulBasis();
services.AddEncinaLawfulBasisEFCore();
```

### MongoDB Provider

```bash
dotnet add package Encina.MongoDB
```

```csharp
services.AddEncinaLawfulBasis();
services.AddEncinaLawfulBasisMongoDB(connectionString, databaseName: "MyApp");
```

### Provider Extension Methods

| Provider | Extension Method |
|----------|------------------|
| ADO.NET SQLite | `AddEncinaLawfulBasisADOSqlite(connectionString)` |
| ADO.NET SQL Server | `AddEncinaLawfulBasisADOSqlServer(connectionString)` |
| ADO.NET PostgreSQL | `AddEncinaLawfulBasisADOPostgreSQL(connectionString)` |
| ADO.NET MySQL | `AddEncinaLawfulBasisADOMySQL(connectionString)` |
| Dapper SQLite | `AddEncinaLawfulBasisDapperSqlite(connectionString)` |
| Dapper SQL Server | `AddEncinaLawfulBasisDapperSqlServer(connectionString)` |
| Dapper PostgreSQL | `AddEncinaLawfulBasisDapperPostgreSQL(connectionString)` |
| Dapper MySQL | `AddEncinaLawfulBasisDapperMySQL(connectionString)` |
| EF Core (multi-DB) | `AddEncinaLawfulBasisEFCore()` |
| MongoDB | `AddEncinaLawfulBasisMongoDB(connectionString, databaseName)` |

All providers implement the same `ILawfulBasisRegistry` and `ILIAStore` interfaces, making it trivial to switch providers by changing only the DI registration.

## Error Codes

### Processing Activity Errors

| Code | Meaning |
|------|---------|
| `gdpr.unregistered_activity` | Request processes personal data but has no registry entry |
| `gdpr.compliance_validation_failed` | Compliance validator reported non-compliance |
| `gdpr.registry_lookup_failed` | Failed to look up processing activity in registry |
| `gdpr.ropa_export_serialization_failed` | RoPA export serialization error |

### Lawful Basis Errors

| Code | Meaning |
|------|---------|
| `gdpr.lawful_basis_not_declared` | Request processes personal data without a declared lawful basis |
| `gdpr.consent_not_found` | No active consent found for the data subject and purpose |
| `gdpr.consent_provider_not_registered` | `IConsentStatusProvider` required but not registered |
| `gdpr.lia_not_found` | Referenced LIA record does not exist |
| `gdpr.lia_not_approved` | Referenced LIA exists but outcome is not `Approved` |
| `gdpr.lawful_basis_store_error` | Error accessing the lawful basis registry store |
| `gdpr.lia_store_error` | Error accessing the LIA store |

## Custom Implementations

Register custom implementations before `AddEncinaGDPR()` or `AddEncinaLawfulBasis()` to override defaults (TryAdd semantics):

```csharp
// Custom registry (e.g., database-backed)
services.AddSingleton<IProcessingActivityRegistry, DatabaseProcessingActivityRegistry>();

// Custom compliance validator
services.AddScoped<IGDPRComplianceValidator, MyComplianceValidator>();

services.AddEncinaGDPR(options =>
{
    options.AutoRegisterFromAttributes = false; // Manual registry management
});

// Custom lawful basis registry or LIA store
services.AddSingleton<ILawfulBasisRegistry, MyCustomLawfulBasisRegistry>();
services.AddSingleton<ILIAStore, MyCustomLIAStore>();

services.AddEncinaLawfulBasis(options =>
{
    options.EnforcementMode = LawfulBasisEnforcementMode.Block;
});
```

## Observability

### Tracing

Two dedicated `ActivitySource` instances for fine-grained filtering:

| ActivitySource | Scope |
|---------------|-------|
| `Encina.Compliance.GDPR` | Processing activity compliance checks |
| `Encina.Compliance.GDPR.LawfulBasis` | Lawful basis validation, consent checks, LIA checks |

### Metrics

All counters are emitted under the `Encina.Compliance.GDPR` meter with tag-based dimensions:

| Metric | Tags | Description |
|--------|------|-------------|
| `lawful_basis_validations_total` | `basis`, `outcome` | Total lawful basis validations |
| `lawful_basis_consent_checks_total` | `outcome` | Consent status checks for consent-based processing |
| `lawful_basis_lia_checks_total` | `outcome` | Legitimate Interest Assessment checks |

### Structured Logging

Zero-allocation logging via `[LoggerMessage]` source generator. Lawful basis events use EventIds 8200-8216:

| EventId | Level | Description |
|---------|-------|-------------|
| 8200 | Debug | Validation started |
| 8201 | Information | Validation passed |
| 8202 | Warning | Validation failed |
| 8203 | Debug | Consent check started |
| 8204 | Warning | Consent check failed |
| 8205 | Debug | LIA check started |
| 8206 | Warning | LIA check failed |
| 8207 | Warning | Attribute conflict: `[LawfulBasis]` and `[ProcessingActivity]` declare different bases |
| 8208 | Warning | No lawful basis declared for personal-data request |
| 8209 | Warning | Consent-based processing but no `IConsentStatusProvider` registered |
| 8210 | Trace | Validation skipped (no GDPR attributes) |
| 8211-8213 | Various | Auto-registration and health check events |
| 8214 | Information | Consent check passed |
| 8215 | Information | LIA check passed |
| 8216 | Warning | Lawful basis violation in warn mode (processing allowed) |

### Health Check

When `AddHealthCheck = true`, a health check is registered that:
- Verifies access to `ILawfulBasisRegistry` and `ILIAStore`
- Counts total lawful basis registrations
- Reports pending LIA reviews (outcome = `RequiresReview`)
- Returns `Degraded` if pending reviews exist, `Healthy` otherwise

## Related Packages

| Package | Description |
|---------|-------------|
| `Encina` | Core CQRS pipeline with `IPipelineBehavior` |
| `Encina.Compliance.Consent` | GDPR Article 7 consent management (provides `IConsentStatusProvider` adapter) |
| `Encina.Security` | Transport-agnostic authorization pipeline |
| `Encina.ADO.*` | ADO.NET persistence providers |
| `Encina.Dapper.*` | Dapper persistence providers |
| `Encina.EntityFrameworkCore` | EF Core persistence provider |
| `Encina.MongoDB` | MongoDB persistence provider |

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.
