# Encina.Compliance.Consent

[![NuGet](https://img.shields.io/nuget/v/Encina.Compliance.Consent.svg)](https://www.nuget.org/packages/Encina.Compliance.Consent/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](../../LICENSE)

GDPR-compliant consent management for Encina. Provides declarative, attribute-based consent enforcement at the CQRS pipeline level with full lifecycle management via Marten event sourcing. Implements GDPR Articles 6(1)(a), 7, and 8.

## Features

- **Event-Sourced Consent** — `ConsentAggregate` with full lifecycle: grant, withdraw, expire, renew, version change, reconsent
- **CQRS Architecture** — Commands via `IAggregateRepository<ConsentAggregate>`, queries via `IReadModelRepository<ConsentReadModel>`
- **8 Domain Events** — `ConsentGranted`, `ConsentWithdrawn`, `ConsentExpired`, `ConsentRenewed`, `ConsentVersionChanged`, `ConsentReconsentProvided`, `ConsentRenewedByGrant`, `ConsentRegranted`
- **Declarative Consent Enforcement** — `[RequireConsent("marketing")]` attribute on request types
- **Pipeline Enforcement** — `ConsentRequiredPipelineBehavior` validates consent before handler execution
- **Three Enforcement Modes** — `Block` (reject), `Warn` (log and proceed), `Disabled` (no-op)
- **Railway Oriented Programming** — All operations return `Either<EncinaError, T>`, no exceptions
- **Cache-Aside Pattern** — `ICacheProvider` integration with fire-and-forget invalidation
- **Marten Projections** — `ConsentProjection` transforms events to `ConsentReadModel` for efficient querying
- **Full Observability** — OpenTelemetry tracing, structured logging (EventId 8200-8299), 5 metric counters + 1 histogram, health check
- **PostgreSQL via Marten** — Event store + document DB for event sourcing and projections
- **.NET 10 Compatible** — Built with latest C# features

## Installation

```bash
dotnet add package Encina.Compliance.Consent
dotnet add package Encina.Marten  # Required: Marten event sourcing infrastructure
```

## Quick Start

### 1. Register Services

```csharp
// Register Encina core
services.AddEncina(config =>
    config.RegisterServicesFromAssemblyContaining<Program>());

// Register consent module (options, pipeline behavior, validator, service)
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

// Register Marten aggregate + projection for consent
services.AddConsentAggregates();
```

### 2. Decorate Request Types

```csharp
// Consent required: pipeline validates before handler execution
[RequireConsent(ConsentPurposes.Marketing, SubjectIdProperty = nameof(UserId))]
public sealed record SendMarketingEmailCommand(string UserId, string Content) : IRequest<Unit>;

// No attribute: pipeline skips consent checks entirely (zero overhead)
public sealed record GetPublicCatalogQuery() : IQuery<CatalogDto>;
```

### 3. Grant Consent (Event-Sourced)

```csharp
var consentService = serviceProvider.GetRequiredService<IConsentService>();

var result = await consentService.GrantConsentAsync(
    dataSubjectId: "user-123",
    purpose: ConsentPurposes.Marketing,
    consentVersionId: "marketing-v2",
    source: "web-form",
    grantedBy: "user-123",
    ipAddress: "192.168.1.1",
    proofOfConsent: "form-hash-abc",
    expiresAtUtc: DateTimeOffset.UtcNow.AddDays(365),
    occurredAtUtc: DateTimeOffset.UtcNow);  // optional: when the data subject acted

// result: Either<EncinaError, Guid> — the consent aggregate ID
```

`occurredAtUtc` is optional and defaults to the current time of the injected `TimeProvider`; see [Granting consent on an existing record](#granting-consent-on-an-existing-record).

### 4. Withdraw Consent (Article 7(3))

```csharp
var result = await consentService.WithdrawConsentAsync(
    consentId: consentId,
    withdrawnBy: "user-123",
    reason: "No longer interested");
// Raises ConsentWithdrawn domain event
```

### 5. Query Consent State

```csharp
// Check if valid consent exists (runtime expiration check included)
var hasConsent = await consentService.HasValidConsentAsync("user-123", ConsentPurposes.Marketing);

// Get consent read model by subject + purpose
var consent = await consentService.GetConsentBySubjectAndPurposeAsync("user-123", ConsentPurposes.Marketing);

// Get all consents for a data subject
var allConsents = await consentService.GetAllConsentsAsync("user-123");
```

## Granting consent on an existing record

There is one consent record per (tenant, data subject, purpose). `GrantConsentAsync` always returns that record's consent ID: a new one for the first grant, the existing one afterwards. A grant on an existing record is a new act with its own proof, version and expiry:

| Current status | What the grant records | Result |
|----------------|------------------------|--------|
| `Active` | `ConsentRenewedByGrant` | Renewal; identical repeats (double click, redelivered message) append identical events |
| `Withdrawn` or `Expired` | `ConsentRegranted` | Consent is `Active` again on the same stream; the withdrawal or expiry stays in the history |
| `RequiresReconsent` | `ConsentReconsentProvided` | Accepted only when `consentVersionId` is the current consent version; otherwise `consent.reconsent_required`, pointing to `ProvideReconsentAsync` |

Acts are ordered by the optional `occurredAtUtc` parameter, the timestamp of the act itself (default: the injected `TimeProvider`):

- A grant older than the consent's latest act (`LastActAtUtc`) is kept in the history without changing the state.
- Withdrawals, expiries and version changes always apply, and a withdrawal wins a tie with a grant at the same instant.
- A timestamp more than five minutes in the future is refused with `consent.act_in_future`.
- Concurrent grants on one key are retried on the reloaded stream, up to five attempts; a conflict that persists is returned as `marten.concurrency_conflict`.

> **Warning: the act timestamp comes from the client application.** An unsynchronised client clock can order acts inconsistently (a grant may be kept only in the history, or may supersede a later act), and Encina cannot detect or correct it. Because a late act is a history-only event, handlers of the consent events must read the current state (`HasValidConsentAsync`) instead of inferring it from a single event.

## Consent Lifecycle

```
                  GrantConsent
                       │
                       ▼
               ┌──────────────┐
               │    Active     │◄──── RenewConsent
               └──────┬───────┘          │
                      │                  │
          ┌───────────┼───────────┐      │
          │           │           │      │
          ▼           ▼           ▼      │
    Withdraw    ChangeVersion   Expire   │
          │      (requires       │       │
          │      reconsent)      │       │
          ▼           │          ▼       │
    ┌──────────┐     ▼     ┌─────────┐  │
    │Withdrawn │  ┌──────────────────┐│  │
    └──────────┘  │RequiresReconsent ││  │
                  └────────┬─────────┘│  │
                           │          │  │
                           ▼          │  │
                    ProvideReconsent ──┘──┘
                           │
                           ▼
                       Active
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
| `consent.not_found` | Consent aggregate not found by ID |
| `consent.invalid_state_transition` | Invalid state transition (e.g., withdraw from expired) |
| `consent.reconsent_required` | A grant on a record that requires reconsent used a version other than the current one; use `ProvideReconsentAsync` |
| `consent.act_in_future` | The act timestamp is more than five minutes ahead of the injected clock |
| `consent.service_error` | Internal service error during consent operation |
| `consent.event_history_unavailable` | Event history retrieval not yet available |

## Custom Implementations

Register custom services before `AddEncinaConsent()` to override defaults (TryAdd semantics):

```csharp
// Custom consent service (e.g., external API-backed)
services.AddScoped<IConsentService, MyConsentService>();

// Custom validator
services.AddScoped<IConsentValidator, MyConsentValidator>();

services.AddEncinaConsent(options =>
{
    options.AutoRegisterFromAttributes = false;
});
```

## Testing

### Unit Tests (Mock Dependencies)

```csharp
// Mock the Marten repositories for unit testing
var repository = Substitute.For<IAggregateRepository<ConsentAggregate>>();
var readModelRepository = Substitute.For<IReadModelRepository<ConsentReadModel>>();
var cache = Substitute.For<ICacheProvider>();

var service = new DefaultConsentService(
    repository, readModelRepository, cache,
    TimeProvider.System, NullLogger<DefaultConsentService>.Instance);
```

### Integration Tests (Docker + Marten)

```csharp
// Full integration tests require PostgreSQL via Docker/Testcontainers
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
public class ConsentIntegrationTests
{
    private readonly MartenFixture _fixture;
    // ... test against real Marten event store
}
```

## Observability

- **Tracing**: `Encina.Compliance.Consent` ActivitySource with consent-specific tags
- **Metrics**: 5 counters (`consent.granted.total`, `consent.withdrawn.total`, `consent.renewed.total`, `consent.reconsent.total`, `consent.expired.total`) + 1 histogram (`consent.validation.duration`)
- **Logging**: Structured log events via `LoggerMessage.Define` (EventId 8200-8299, zero-allocation)
- **Health Check**: Verifies `IConsentService` resolvability and DI configuration

## Related Packages

| Package | Description |
|---------|-------------|
| `Encina` | Core CQRS pipeline with `IPipelineBehavior` |
| `Encina.Marten` | Marten event sourcing infrastructure |
| `Encina.Compliance.GDPR` | GDPR processing activity tracking and RoPA |
| `Encina.Security` | Transport-agnostic authorization pipeline |

## GDPR Compliance

This package implements key GDPR requirements:

| Article | Requirement | Implementation |
|---------|-------------|----------------|
| **6(1)(a)** | Lawful processing based on consent | `ConsentAggregate` with status tracking via domain events |
| **7(1)** | Demonstrate consent was given | `ProofOfConsent` field, full event history |
| **7(2)** | Distinguishable consent request | Purpose-based granular consent |
| **7(3)** | Right to withdraw consent | `WithdrawConsentAsync`, as easy as granting |
| **8** | Child consent (age verification) | Extensible via custom `IConsentValidator` |

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.
