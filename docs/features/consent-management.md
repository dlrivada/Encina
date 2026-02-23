# Consent Management in Encina

This guide explains how to enforce GDPR-compliant consent management declaratively at the CQRS pipeline level using the `Encina.Compliance.Consent` package. Consent validation operates independently of the transport layer, ensuring consistent Article 7 compliance across all entry points.

## Table of Contents

1. [Overview](#overview)
2. [The Problem](#the-problem)
3. [The Solution](#the-solution)
4. [Quick Start](#quick-start)
5. [RequireConsent Attribute](#requireconsent-attribute)
6. [Consent Store](#consent-store)
7. [Consent Validation](#consent-validation)
8. [Consent Versioning](#consent-versioning)
9. [Audit Trail](#audit-trail)
10. [Domain Events](#domain-events)
11. [Bulk Operations](#bulk-operations)
12. [Configuration Options](#configuration-options)
13. [Enforcement Modes](#enforcement-modes)
14. [Database Providers](#database-providers)
15. [Observability](#observability)
16. [Health Check](#health-check)
17. [Error Handling](#error-handling)
18. [Best Practices](#best-practices)
19. [Testing](#testing)
20. [FAQ](#faq)

---

## Overview

Encina.Compliance.Consent provides attribute-based consent enforcement at the CQRS pipeline level:

| Component | Description |
|-----------|-------------|
| **`[RequireConsent]` Attribute** | Declarative consent requirement on request types |
| **`ConsentRequiredPipelineBehavior`** | Pipeline behavior that validates consent and short-circuits on failure |
| **`IConsentStore`** | Full consent lifecycle management (record, query, withdraw, validate) |
| **`IConsentValidator`** | Consent validation with version checking and expiration detection |
| **`IConsentVersionManager`** | Consent version tracking with re-consent triggers |
| **`IConsentAuditStore`** | Immutable audit trail for all consent actions |
| **`ConsentOptions`** | Configuration for enforcement mode, purposes, auto-registration |

### Why Pipeline-Level Consent?

| Benefit | Description |
|---------|-------------|
| **Automatic enforcement** | Consent is validated whenever a request processes personal data |
| **Declarative** | Consent requirements live with the request type, not scattered across controllers |
| **Transport-agnostic** | Same consent enforcement for HTTP, message queue, gRPC, and serverless |
| **Auditable** | Every consent action is recorded with timestamps and metadata |
| **Version-aware** | Consent version changes trigger automatic re-consent requirements |

---

## The Problem

GDPR Article 7 requires explicit, informed, freely given consent for processing personal data. Applications typically struggle with:

- **Scattered consent checks** across controllers, services, and middleware
- **Missing consent validation** on internal/background processing paths
- **No audit trail** to demonstrate consent was given (Article 7(1))
- **No version tracking** when consent terms change
- **Withdrawal difficulty** — consent withdrawal must be as easy as granting (Article 7(3))
- **Inconsistent enforcement** across different transport layers

---

## The Solution

Encina solves this with a single attribute and pipeline behavior:

```text
Request → [ConsentRequiredPipelineBehavior] → Handler
                    │
                    ├── No [RequireConsent]? → Skip (zero overhead)
                    ├── Disabled mode? → Skip
                    ├── Extract SubjectId via cached reflection
                    ├── Validate consent via IConsentValidator
                    │   ├── Check IConsentStore for active consent
                    │   ├── Check expiration
                    │   └── Check version currency
                    ├── Valid? → Proceed to handler
                    ├── Invalid + Block mode? → Return EncinaError
                    └── Invalid + Warn mode? → Log warning, proceed
```

---

## Quick Start

### 1. Install the Package

```bash
dotnet add package Encina.Compliance.Consent
```

### 2. Register Services

```csharp
services.AddEncina(config =>
    config.RegisterServicesFromAssemblyContaining<Program>());

services.AddEncinaConsent(options =>
{
    options.EnforcementMode = ConsentEnforcementMode.Block;
    options.DefaultExpirationDays = 365;
    options.DefinePurpose(ConsentPurposes.Marketing, p =>
    {
        p.Description = "Email marketing campaigns";
        p.RequiresExplicitOptIn = true;
        p.DefaultExpirationDays = 365;
    });
    options.DefinePurpose(ConsentPurposes.Analytics, p =>
    {
        p.Description = "Usage analytics";
        p.DefaultExpirationDays = 180;
    });
});
```

### 3. Decorate Request Types

```csharp
[RequireConsent(ConsentPurposes.Marketing, SubjectIdProperty = nameof(UserId))]
public sealed record SendMarketingEmailCommand(string UserId, string Content) : IRequest<Unit>;

// No attribute: pipeline skips consent checks entirely
public sealed record GetPublicCatalogQuery() : IQuery<CatalogDto>;
```

### 4. Record Consent

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
    Metadata = new Dictionary<string, object?>
    {
        ["formVersion"] = "2.1",
        ["pageUrl"] = "/consent/marketing"
    }
};

var result = await store.RecordConsentAsync(consent);
```

### 5. Send Request (Consent Validated Automatically)

```csharp
var encina = serviceProvider.GetRequiredService<IEncina>();
var result = await encina.Send(new SendMarketingEmailCommand("user-123", "Spring sale!"));

result.Match(
    Right: _ => Console.WriteLine("Email sent"),
    Left: error => Console.WriteLine($"Blocked: {error.Code}")  // "consent.missing"
);
```

---

## RequireConsent Attribute

The `[RequireConsent]` attribute declares consent requirements on request types:

```csharp
// Single purpose
[RequireConsent(ConsentPurposes.Marketing, SubjectIdProperty = nameof(UserId))]
public sealed record SendNewsletterCommand(string UserId) : IRequest<Unit>;

// Multiple purposes (ALL required)
[RequireConsent(ConsentPurposes.Analytics, ConsentPurposes.Profiling,
    SubjectIdProperty = nameof(CustomerId))]
public sealed record CreateUserProfileCommand(string CustomerId) : IRequest<ProfileId>;

// Custom error message
[RequireConsent(ConsentPurposes.ThirdPartySharing,
    SubjectIdProperty = nameof(UserId),
    ErrorMessage = "User must consent to third-party data sharing")]
public sealed record ShareDataCommand(string UserId) : IRequest<Unit>;
```

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `Purposes` | `string[]` | Yes | Consent purposes to validate (constructor parameter) |
| `SubjectIdProperty` | `string?` | No | Property name on the request containing the subject ID |
| `ErrorMessage` | `string?` | No | Custom error message for missing consent |

---

## Consent Store

The `IConsentStore` interface provides full consent lifecycle management:

```csharp
// Record new consent
await store.RecordConsentAsync(consentRecord);

// Query consent for a subject and purpose
var consent = await store.GetConsentAsync("user-123", ConsentPurposes.Marketing);
// Returns Either<EncinaError, Option<ConsentRecord>>

// Get all consents for a subject (consent dashboard)
var allConsents = await store.GetAllConsentsAsync("user-123");

// Check if valid consent exists (quick check)
var hasConsent = await store.HasValidConsentAsync("user-123", ConsentPurposes.Marketing);

// Withdraw consent (Article 7(3))
await store.WithdrawConsentAsync("user-123", ConsentPurposes.Marketing);
```

### Consent Status Lifecycle

```text
Active → Withdrawn (via WithdrawConsentAsync)
Active → Expired (automatic, based on ExpiresAtUtc)
Active → RequiresReconsent (via version change)
```

---

## Consent Validation

The `DefaultConsentValidator` performs multi-step validation per purpose:

1. **Store lookup** — Check `IConsentStore.HasValidConsentAsync`
2. **Status check** — Verify consent is `Active`
3. **Expiration check** — Verify `ExpiresAtUtc` has not passed
4. **Version check** — Verify consent version matches current version via `IConsentVersionManager`

```csharp
var validator = serviceProvider.GetRequiredService<IConsentValidator>();
var result = await validator.ValidateAsync("user-123",
    [ConsentPurposes.Marketing, ConsentPurposes.Analytics]);

if (result.IsRight)
{
    var validation = result.Match(Right: v => v, Left: _ => null!);
    if (validation.IsValid)
        Console.WriteLine("All consents valid");
    else
        Console.WriteLine($"Missing: {string.Join(", ", validation.MissingPurposes)}");
}
```

---

## Consent Versioning

When consent terms change, the `IConsentVersionManager` tracks versions and triggers re-consent:

```csharp
var versionManager = serviceProvider.GetRequiredService<IConsentVersionManager>();

// Publish a new version (triggers re-consent if RequiresExplicitReconsent = true)
await versionManager.PublishNewVersionAsync(new ConsentVersion
{
    VersionId = "marketing-v3",
    Purpose = ConsentPurposes.Marketing,
    EffectiveFromUtc = DateTimeOffset.UtcNow,
    Description = "Updated marketing terms with third-party sharing details",
    RequiresExplicitReconsent = true
});

// Check if a user needs to re-consent
var needsReconsent = await versionManager.RequiresReconsentAsync(
    "user-123", ConsentPurposes.Marketing);
```

---

## Audit Trail

Every consent action is recorded in an immutable audit trail:

```csharp
var auditStore = serviceProvider.GetRequiredService<IConsentAuditStore>();

// Get full audit trail for a subject
var trail = await auditStore.GetAuditTrailAsync("user-123");

// Filter by purpose
var marketingTrail = await auditStore.GetAuditTrailAsync("user-123",
    purpose: ConsentPurposes.Marketing);
```

Audit actions: `Granted`, `Withdrawn`, `Expired`, `VersionChanged`.

---

## Domain Events

The consent module publishes domain events via `IEncina`:

| Event | Trigger | Properties |
|-------|---------|------------|
| `ConsentGrantedEvent` | Consent recorded | SubjectId, Purpose, ConsentVersionId, Source, ExpiresAtUtc |
| `ConsentWithdrawnEvent` | Consent withdrawn | SubjectId, Purpose, OccurredAtUtc |
| `ConsentExpiredEvent` | Expired consent detected | SubjectId, Purpose, ExpiredAtUtc |
| `ConsentVersionChangedEvent` | New version published | Purpose, NewVersionId, RequiresExplicitReconsent |

Subscribe to events using standard Encina notification handlers:

```csharp
public sealed class ConsentWithdrawnHandler : INotificationHandler<ConsentWithdrawnEvent>
{
    public Task Handle(ConsentWithdrawnEvent notification, CancellationToken cancellationToken)
    {
        // Stop marketing campaigns for this user
        return Task.CompletedTask;
    }
}
```

---

## Bulk Operations

For scenarios like import, migration, or "withdraw all" dashboard actions:

```csharp
// Batch record
var consents = users.Select(u => new ConsentRecord { SubjectId = u.Id, ... });
var result = await store.BulkRecordConsentAsync(consents);
// result.SuccessCount, result.FailureCount, result.Errors

// Batch withdraw (single subject, multiple purposes)
var result = await store.BulkWithdrawConsentAsync("user-123",
    [ConsentPurposes.Marketing, ConsentPurposes.Analytics, ConsentPurposes.Profiling]);
```

---

## Configuration Options

```csharp
services.AddEncinaConsent(options =>
{
    // Enforcement mode
    options.EnforcementMode = ConsentEnforcementMode.Block;

    // Default expiration (null = no expiration)
    options.DefaultExpirationDays = 365;

    // Require explicit opt-in
    options.RequireExplicitConsent = true;

    // Allow per-purpose withdrawal
    options.AllowGranularWithdrawal = true;

    // Track proof of consent
    options.TrackConsentProof = false;

    // Reject unknown purposes
    options.FailOnUnknownPurpose = false;

    // Auto-register purposes from [RequireConsent] attributes
    options.AutoRegisterFromAttributes = true;
    options.AssembliesToScan.Add(typeof(Program).Assembly);

    // Register health check
    options.AddHealthCheck = true;

    // Define purposes with per-purpose configuration
    options.DefinePurpose(ConsentPurposes.Marketing, p =>
    {
        p.Description = "Email marketing campaigns";
        p.RequiresExplicitOptIn = true;
        p.DefaultExpirationDays = 365;
        p.CanBeWithdrawnAnytime = true;
    });
});
```

---

## Enforcement Modes

| Mode | Missing Consent | Use Case |
|------|----------------|----------|
| `Block` | Returns `ConsentErrors.MissingConsent` error | Production (GDPR-compliant) |
| `Warn` | Logs warning at `Warning` level, proceeds | Migration/testing phase |
| `Disabled` | Skips all validation (no-op) | Development environments |

---

## Database Providers

The in-memory stores are suitable for development and testing. For production, use a database-backed provider:

| Provider Category | Providers | Registration |
|-------------------|-----------|-------------|
| ADO.NET | SQLite, SQL Server, PostgreSQL, MySQL | `config.UseConsent = true` in `AddEncinaADO()` |
| Dapper | SQLite, SQL Server, PostgreSQL, MySQL | `config.UseConsent = true` in `AddEncinaDapper()` |
| EF Core | SQLite, SQL Server, PostgreSQL, MySQL | `config.UseConsent = true` in `AddEncinaEntityFrameworkCore()` |
| MongoDB | MongoDB | `config.UseConsent = true` in `AddEncinaMongoDB()` |

Each provider registers `IConsentStore`, `IConsentAuditStore`, and `IConsentVersionManager`.

---

## Observability

### OpenTelemetry Tracing

The pipeline behavior creates activities with the `Encina.Compliance.Consent` ActivitySource:

- `consent.request_type` — Request type name
- `consent.subject_id` — Data subject identifier
- `consent.purposes` — Required purposes
- `consent.enforcement_mode` — Current enforcement mode
- `consent.outcome` — `allowed` / `denied` / `warned`

### Structured Logging

6 log events using `LoggerMessage.Define` for zero-allocation structured logging:

| EventId | Level | Message |
|---------|-------|---------|
| 9000 | Information | Consent validated |
| 9001 | Warning | Missing consent (warn mode) |
| 9002 | Warning | Consent expired |
| 9003 | Information | Consent enforcement disabled |
| 9004 | Error | Consent validation failed |
| 9005 | Information | Consent auto-registration completed |

---

## Health Check

Opt-in via `options.AddHealthCheck = true`:

```csharp
services.AddEncinaConsent(options =>
{
    options.AddHealthCheck = true;
});
```

The health check (`encina-consent`) verifies:

- `IConsentStore` is resolvable from DI
- `IConsentValidator` is resolvable from DI
- `IConsentVersionManager` is resolvable from DI

---

## Error Handling

All operations return `Either<EncinaError, T>`:

```csharp
var result = await store.RecordConsentAsync(consent);

result.Match(
    Right: _ => logger.LogInformation("Consent recorded"),
    Left: error =>
    {
        logger.LogError("Failed: {Code} - {Message}", error.Code, error.Message);
        // error.Details contains structured metadata
    }
);
```

---

## Best Practices

1. **Use `Block` mode in production** — `Warn` mode is only for migration periods
2. **Define all purposes upfront** — Use `DefinePurpose()` with descriptions for transparency
3. **Set expiration dates** — GDPR requires time-limited consent where possible
4. **Track proof of consent** — Enable `TrackConsentProof` for regulatory audits
5. **Subscribe to domain events** — React to consent changes (stop campaigns, purge data)
6. **Use bulk operations** — For migration and "withdraw all" scenarios
7. **Version your consent terms** — Use `IConsentVersionManager` when terms change
8. **Test with `InMemoryConsentStore`** — Fast, deterministic unit tests with `TimeProvider` injection

---

## Testing

### Unit Tests with In-Memory Store

```csharp
var store = new InMemoryConsentStore(
    TimeProvider.System,
    NullLogger<InMemoryConsentStore>.Instance);

// Seed consent
await store.RecordConsentAsync(new ConsentRecord
{
    Id = Guid.NewGuid(),
    SubjectId = "test-user",
    Purpose = ConsentPurposes.Marketing,
    Status = ConsentStatus.Active,
    ConsentVersionId = "v1",
    GivenAtUtc = DateTimeOffset.UtcNow,
    Source = "test",
    Metadata = new Dictionary<string, object?>()
});

// Validate
var result = await store.HasValidConsentAsync("test-user", ConsentPurposes.Marketing);
Assert.True(result.IsRight);
```

### Full Pipeline Test

```csharp
var services = new ServiceCollection();
services.AddEncina(c => c.RegisterServicesFromAssemblyContaining<MyCommand>());
services.AddEncinaConsent(o => o.EnforcementMode = ConsentEnforcementMode.Block);
services.AddScoped<IRequestHandler<MyCommand, int>, MyHandler>();

var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();
var encina = scope.ServiceProvider.GetRequiredService<IEncina>();

var result = await encina.Send(new MyCommand("user-without-consent"));
Assert.True(result.IsLeft); // Blocked due to missing consent
```

---

## FAQ

**Q: What happens if no `[RequireConsent]` attribute is present?**
The pipeline behavior skips all consent checks with zero overhead (attribute presence is cached).

**Q: Can I use multiple purposes on a single request?**
Yes. `[RequireConsent("marketing", "analytics")]` requires ALL listed purposes to have valid consent.

**Q: How is the subject ID extracted?**
Via the `SubjectIdProperty` parameter, which uses cached reflection to read the property value from the request.

**Q: What if consent expires during request processing?**
The `InMemoryConsentStore` detects expiration at read time and updates the status to `Expired`, publishing a `ConsentExpiredEvent`.

**Q: How do I migrate from another consent system?**
Use `BulkRecordConsentAsync` to import existing consent records. Set `AutoRegisterFromAttributes = false` during migration.
