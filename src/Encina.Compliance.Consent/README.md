# Encina.Compliance.Consent

[![NuGet](https://img.shields.io/nuget/v/Encina.Compliance.Consent.svg)](https://www.nuget.org/packages/Encina.Compliance.Consent/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](../../LICENSE)

GDPR-compliant consent management for Encina. Provides declarative, attribute-based consent enforcement at the CQRS pipeline level with full lifecycle management, version tracking, audit trail, and domain event publishing. Implements GDPR Articles 6(1)(a), 7, and 8.

## Features

- **Declarative Consent Enforcement** — `[RequireConsent("marketing")]` attribute on request types
- **Full Consent Lifecycle** — Record, validate, withdraw, and expire consent with `IConsentStore`
- **Consent Versioning** — Track consent version changes and trigger re-consent via `IConsentVersionManager`
- **Immutable Audit Trail** — Every consent action is recorded via `IConsentAuditStore`
- **Domain Events** — `ConsentGrantedEvent`, `ConsentWithdrawnEvent`, `ConsentExpiredEvent`, `ConsentVersionChangedEvent`
- **Pipeline Enforcement** — `ConsentRequiredPipelineBehavior` validates consent before handler execution
- **Three Enforcement Modes** — `Block` (reject), `Warn` (log and proceed), `Disabled` (no-op)
- **Railway Oriented Programming** — All operations return `Either<EncinaError, T>`, no exceptions
- **Bulk Operations** — Batch record and withdraw consent with per-item error tracking
- **Full Observability** — OpenTelemetry tracing, structured logging, health check
- **13 Database Providers** — ADO.NET, Dapper, EF Core (SQLite, SQL Server, PostgreSQL, MySQL) + MongoDB
- **.NET 10 Compatible** — Built with latest C# features

## Installation

```bash
dotnet add package Encina.Compliance.Consent
```

## Quick Start

### 1. Register Services

```csharp
services.AddEncina(config =>
    config.RegisterServicesFromAssemblyContaining<Program>());

services.AddEncinaConsent(options =>
{
    options.EnforcementMode = ConsentEnforcementMode.Block;
    options.DefaultExpirationDays = 365;
    options.AutoRegisterFromAttributes = true;
    options.AssembliesToScan.Add(typeof(Program).Assembly);
    options.DefinePurpose(ConsentPurposes.Marketing, p =>
    {
        p.Description = "Email marketing campaigns";
        p.RequiresExplicitOptIn = true;
        p.DefaultExpirationDays = 365;
    });
});
```

### 2. Decorate Request Types

```csharp
// Consent required: pipeline validates before handler execution
[RequireConsent(ConsentPurposes.Marketing, SubjectIdProperty = nameof(UserId))]
public sealed record SendMarketingEmailCommand(string UserId, string Content) : IRequest<Unit>;

// No attribute: pipeline skips consent checks entirely (zero overhead)
public sealed record GetPublicCatalogQuery() : IQuery<CatalogDto>;
```

### 3. Record Consent

```csharp
var store = serviceProvider.GetRequiredService<IConsentStore>();

var consent = new ConsentRecord
{
    Id = Guid.NewGuid(),
    SubjectId = "user-123",
    Purpose = ConsentPurposes.Marketing,
    Status = ConsentStatus.Active,
    ConsentVersionId = "marketing-v2",
    GivenAtUtc = DateTimeOffset.UtcNow,
    Source = "web-form",
    Metadata = new Dictionary<string, object?>()
};

var result = await store.RecordConsentAsync(consent);
// result: Either<EncinaError, Unit>
```

### 4. Withdraw Consent (Article 7(3))

```csharp
var result = await store.WithdrawConsentAsync("user-123", ConsentPurposes.Marketing);
// Publishes ConsentWithdrawnEvent via IEncina
```

## Consent Purposes

| Constant | Value | Description |
|----------|-------|-------------|
| `ConsentPurposes.Marketing` | `"marketing"` | Email/SMS marketing campaigns |
| `ConsentPurposes.Analytics` | `"analytics"` | Usage analytics and tracking |
| `ConsentPurposes.Personalization` | `"personalization"` | Content personalization |
| `ConsentPurposes.ThirdPartySharing` | `"third-party-sharing"` | Sharing data with third parties |
| `ConsentPurposes.Profiling` | `"profiling"` | Automated decision-making |
| `ConsentPurposes.Newsletter` | `"newsletter"` | Newsletter subscriptions |
| `ConsentPurposes.LocationTracking` | `"location-tracking"` | GPS/location data collection |
| `ConsentPurposes.CrossBorderTransfer` | `"cross-border-transfer"` | International data transfers |

## Enforcement Modes

| Mode | Behavior | Use Case |
|------|----------|----------|
| `Block` | Reject requests without valid consent | Production (recommended) |
| `Warn` | Log warning, allow request to proceed | Migration/testing phase |
| `Disabled` | Skip all consent validation | Development environments |

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `EnforcementMode` | `ConsentEnforcementMode` | `Block` | How to handle missing consent |
| `DefaultExpirationDays` | `int?` | `null` | Auto-expiration for new consents |
| `RequireExplicitConsent` | `bool` | `true` | Require explicit opt-in |
| `AutoRegisterFromAttributes` | `bool` | `true` | Scan assemblies at startup |
| `AllowGranularWithdrawal` | `bool` | `true` | Per-purpose withdrawal |
| `TrackConsentProof` | `bool` | `false` | Store proof of consent |
| `FailOnUnknownPurpose` | `bool` | `false` | Reject undefined purposes |
| `AddHealthCheck` | `bool` | `false` | Register health check |

## Error Codes

| Code | Meaning |
|------|---------|
| `consent.missing` | No consent record found for the subject and purpose |
| `consent.withdrawn` | Consent was previously withdrawn |
| `consent.expired` | Consent record has expired |
| `consent.requires_reconsent` | Consent version changed, re-consent needed |
| `consent.version_mismatch` | Consent was given for a different version |

## Custom Implementations

Register custom stores before `AddEncinaConsent()` to override defaults (TryAdd semantics):

```csharp
// Custom consent store (e.g., database-backed)
services.AddScoped<IConsentStore, DatabaseConsentStore>();

// Custom validator
services.AddScoped<IConsentValidator, MyConsentValidator>();

services.AddEncinaConsent(options =>
{
    options.AutoRegisterFromAttributes = false;
});
```

## Database Providers

Use the corresponding ADO.NET, Dapper, or EF Core package with `UseConsent = true`:

```csharp
// ADO.NET (SQLite example)
services.AddEncinaADO(config =>
{
    config.UseConsent = true;
});

// Dapper (SQL Server example)
services.AddEncinaDapper(config =>
{
    config.UseConsent = true;
});

// EF Core (PostgreSQL example)
services.AddEncinaEntityFrameworkCore<AppDbContext>(config =>
{
    config.UseConsent = true;
});
```

## Observability

- **Tracing**: `Encina.Compliance.Consent` ActivitySource with consent-specific tags
- **Logging**: 6 structured log events via `LoggerMessage.Define` (zero-allocation)
- **Health Check**: Verifies store connectivity and DI configuration

## Related Packages

| Package | Description |
|---------|-------------|
| `Encina` | Core CQRS pipeline with `IPipelineBehavior` |
| `Encina.Compliance.GDPR` | GDPR processing activity tracking and RoPA |
| `Encina.Security` | Transport-agnostic authorization pipeline |

## GDPR Compliance

This package implements key GDPR requirements:

| Article | Requirement | Implementation |
|---------|-------------|----------------|
| **6(1)(a)** | Lawful processing based on consent | `ConsentRecord` with status tracking |
| **7(1)** | Demonstrate consent was given | `ProofOfConsent` field, `IConsentAuditStore` |
| **7(2)** | Distinguishable consent request | Purpose-based granular consent |
| **7(3)** | Right to withdraw consent | `WithdrawConsentAsync`, as easy as granting |
| **8** | Child consent (age verification) | Extensible via custom `IConsentValidator` |

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.
