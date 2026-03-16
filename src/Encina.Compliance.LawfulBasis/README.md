# Encina.Compliance.LawfulBasis

[![NuGet](https://img.shields.io/nuget/v/Encina.Compliance.LawfulBasis.svg)](https://www.nuget.org/packages/Encina.Compliance.LawfulBasis/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](../../LICENSE)

GDPR Article 6(1) lawful basis management for Encina. Provides declarative, attribute-based lawful basis enforcement at the CQRS pipeline level with Legitimate Interest Assessment (LIA) following the EDPB three-part test. Uses Marten event sourcing for full audit trail and accountability compliance.

## Features

- **Event-Sourced Lawful Basis** — `LawfulBasisAggregate` with lifecycle: register, change basis, revoke
- **Legitimate Interest Assessment** — `LIAAggregate` with EDPB three-part test: create, approve, reject, schedule review
- **CQRS Architecture** — Commands via `IAggregateRepository<T>`, queries via `IReadModelRepository<T>`
- **7 Domain Events** — `LawfulBasisRegistered`, `LawfulBasisChanged`, `LawfulBasisRevoked`, `LIACreated`, `LIAApproved`, `LIARejected`, `LIAReviewScheduled`
- **Declarative Basis Enforcement** — `[LawfulBasis(LawfulBasis.Contract)]` attribute on request types
- **Pipeline Enforcement** — `LawfulBasisValidationPipelineBehavior` validates basis before handler execution
- **Three Enforcement Modes** — `Block` (reject), `Warn` (log and proceed), `Disabled` (no-op)
- **Railway Oriented Programming** — All operations return `Either<EncinaError, T>`, no exceptions
- **Cache-Aside Pattern** — `ICacheProvider` integration with fire-and-forget invalidation
- **Auto-Registration** — Scan assemblies for `[LawfulBasis]` attributes at startup
- **Marten Projections** — `LawfulBasisProjection` and `LIAProjection` for efficient querying
- **Full Observability** — OpenTelemetry tracing, structured logging (EventId 8350-8386), 9 metric counters + 1 histogram, health check
- **PostgreSQL via Marten** — Event store + document DB for event sourcing and projections
- **.NET 10 Compatible** — Built with latest C# features

## Installation

```bash
dotnet add package Encina.Compliance.LawfulBasis
dotnet add package Encina.Marten  # Required: Marten event sourcing infrastructure
```

## Quick Start

### 1. Register Services

```csharp
// Register Encina core
services.AddEncina(config =>
    config.RegisterServicesFromAssemblyContaining<Program>());

// Register lawful basis module (options, pipeline behavior, provider, service)
services.AddEncinaLawfulBasis(options =>
{
    options.EnforcementMode = LawfulBasisEnforcementMode.Block;
    options.RequireDeclaredBasis = true;
    options.ValidateLIAForLegitimateInterests = true;
    options.ValidateConsentForConsentBasis = true;
    options.AutoRegisterFromAttributes = true;
    options.ScanAssemblyContaining<Program>();

    // Programmatic default bases for types without attributes
    options.DefaultBasis<ProcessPaymentCommand>(LawfulBasis.Contract);
});

// Register Marten aggregate + projections for lawful basis
services.AddLawfulBasisAggregates();
```

### 2. Decorate Request Types

```csharp
// Declarative basis: pipeline validates before handler execution
[LawfulBasis(LawfulBasis.Contract, Purpose = "Order fulfillment")]
public sealed record ProcessOrderCommand(Guid OrderId) : IRequest<Unit>;

// Legitimate interests with LIA reference
[LawfulBasis(LawfulBasis.LegitimateInterests,
    Purpose = "Fraud detection",
    LIAReference = "LIA-2024-FRAUD-001")]
public sealed record DetectFraudCommand(string TransactionId) : IRequest<Unit>;

// Legal obligation
[LawfulBasis(LawfulBasis.LegalObligation,
    Purpose = "Tax reporting",
    LegalReference = "Tax Code §42")]
public sealed record GenerateTaxReportCommand(int Year) : IRequest<Unit>;

// No attribute: pipeline skips validation entirely (zero overhead)
public sealed record GetPublicCatalogQuery() : IQuery<CatalogDto>;
```

### 3. Register Lawful Basis (Event-Sourced)

```csharp
var service = serviceProvider.GetRequiredService<ILawfulBasisService>();

var result = await service.RegisterAsync(
    id: Guid.NewGuid(),
    requestTypeName: typeof(ProcessOrderCommand).AssemblyQualifiedName!,
    basis: LawfulBasis.Contract,
    purpose: "Order fulfillment",
    liaReference: null,
    legalReference: null,
    contractReference: "CONTRACT-001");

// result: Either<EncinaError, Guid> — the registration aggregate ID
```

### 4. Create a Legitimate Interest Assessment (EDPB Three-Part Test)

```csharp
var liaResult = await service.CreateLIAAsync(
    id: Guid.NewGuid(),
    reference: "LIA-2024-FRAUD-001",
    name: "Fraud Detection Assessment",
    purpose: "Fraud prevention processing",
    // Purpose Test
    legitimateInterest: "Preventing fraudulent transactions to protect customers",
    benefits: "Reduced fraud losses, increased customer trust",
    consequencesIfNotProcessed: "Increased exposure to fraud, financial losses",
    // Necessity Test
    necessityJustification: "Processing is strictly necessary for real-time fraud detection",
    alternativesConsidered: ["Manual review", "Rule-based systems"],
    dataMinimisationNotes: "Only transaction metadata is processed",
    // Balancing Test
    natureOfData: "Transaction amounts, IP addresses, device fingerprints",
    reasonableExpectations: "Customers expect their bank to protect against fraud",
    impactAssessment: "Minimal impact with pseudonymization and access controls",
    safeguards: ["Pseudonymization", "Role-based access", "90-day retention"],
    assessedBy: "Data Protection Officer",
    dpoInvolvement: true);

// Approve the LIA
await service.ApproveLIAAsync(
    liaId: liaResult.Match(id => id, _ => throw new Exception()),
    conclusion: "Legitimate interest outweighs data subject rights",
    approvedBy: "dpo-1");

// Schedule periodic review (GDPR best practice)
await service.ScheduleLIAReviewAsync(
    liaId: liaId,
    nextReviewAtUtc: DateTimeOffset.UtcNow.AddYears(1),
    scheduledBy: "compliance-officer");
```

### 5. Query Registrations and LIA State

```csharp
// Get all registrations
var registrations = await service.GetAllRegistrationsAsync();

// Find registration by request type
var registration = await service.GetRegistrationByRequestTypeAsync(
    typeof(ProcessOrderCommand).AssemblyQualifiedName!);

// Check if a LIA has been approved
var hasApproved = await service.HasApprovedLIAAsync("LIA-2024-FRAUD-001");

// Get pending LIA reviews
var pending = await service.GetPendingLIAReviewsAsync();
```

## Lawful Basis Lifecycle

```
                Register
                    │
                    ▼
            ┌──────────────┐
            │    Active     │◄──── ChangeBasis
            └──────┬───────┘         │
                   │                 │
                   │       ┌─────────┘
                   │       │
                   ▼       │
                Revoke     │
                   │       │
                   ▼       │
            ┌──────────────┐
            │   Revoked    │ (terminal state)
            └──────────────┘
```

## LIA Lifecycle (EDPB Three-Part Test)

```
              Create LIA
                  │
                  ▼
          ┌───────────────┐
          │RequiresReview │
          └───────┬───────┘
                  │
          ┌───────┴───────┐
          │               │
          ▼               ▼
       Approve         Reject
          │               │
          ▼               ▼
    ┌──────────┐   ┌──────────┐
    │ Approved │   │ Rejected │
    └────┬─────┘   └──────────┘
         │
         ▼
   ScheduleReview
         │
         ▼
   (periodic review)
```

## GDPR Article 6(1) — Six Lawful Bases

| Basis | Enum Value | When to Use | Typical Reference |
|-------|-----------|-------------|-------------------|
| **Consent** | `LawfulBasis.Consent` | Data subject has given consent | Consent ID |
| **Contract** | `LawfulBasis.Contract` | Necessary for contract performance | Contract reference |
| **Legal Obligation** | `LawfulBasis.LegalObligation` | Required by law | Legal provision |
| **Vital Interests** | `LawfulBasis.VitalInterests` | Protect life-critical interests | — |
| **Public Task** | `LawfulBasis.PublicTask` | Official authority or public interest | — |
| **Legitimate Interests** | `LawfulBasis.LegitimateInterests` | Legitimate interest (requires LIA) | LIA reference |

## Enforcement Modes

| Mode | Behavior | Use Case |
|------|----------|----------|
| `Block` | Reject requests without declared basis | Production (recommended) |
| `Warn` | Log warning, allow request to proceed | Migration/testing phase |
| `Disabled` | Skip all basis validation | Development environments |

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `EnforcementMode` | `LawfulBasisEnforcementMode` | `Block` | How to handle missing basis |
| `RequireDeclaredBasis` | `bool` | `true` | Require basis for personal data processing |
| `ValidateLIAForLegitimateInterests` | `bool` | `true` | Require approved LIA for legitimate interests |
| `ValidateConsentForConsentBasis` | `bool` | `true` | Verify active consent via `IConsentStatusProvider` |
| `AutoRegisterFromAttributes` | `bool` | `true` | Scan assemblies at startup |
| `AddHealthCheck` | `bool` | `false` | Register health check |

## Error Codes

| Code | Meaning |
|------|---------|
| `lawfulbasis.registration_not_found` | No registration found for the given ID |
| `lawfulbasis.registration_already_revoked` | Attempt to modify a revoked registration |
| `lawfulbasis.lia_not_found` | LIA aggregate not found by ID |
| `lawfulbasis.lia_not_found_by_reference` | LIA not found by reference string |
| `lawfulbasis.lia_already_decided` | LIA already approved or rejected |
| `lawfulbasis.invalid_state_transition` | Invalid aggregate state transition |
| `lawfulbasis.store_error` | Internal store/repository error |

## Custom Implementations

Register custom services before `AddEncinaLawfulBasis()` to override defaults (TryAdd semantics):

```csharp
// Custom lawful basis service (e.g., external API-backed)
services.AddScoped<ILawfulBasisService, MyLawfulBasisService>();

// Custom lawful basis provider (e.g., configuration-based)
services.AddScoped<ILawfulBasisProvider, MyLawfulBasisProvider>();

services.AddEncinaLawfulBasis(options =>
{
    options.AutoRegisterFromAttributes = false;
});
```

## Testing

### Unit Tests (Mock Dependencies)

```csharp
// Mock the Marten repositories for unit testing
var repository = Substitute.For<IAggregateRepository<LawfulBasisAggregate>>();
var liaRepository = Substitute.For<IAggregateRepository<LIAAggregate>>();
var readModelRepository = Substitute.For<IReadModelRepository<LawfulBasisReadModel>>();
var liaReadModelRepository = Substitute.For<IReadModelRepository<LIAReadModel>>();
var cache = Substitute.For<ICacheProvider>();

var service = new DefaultLawfulBasisService(
    repository, liaRepository,
    readModelRepository, liaReadModelRepository,
    cache, TimeProvider.System,
    NullLogger<DefaultLawfulBasisService>.Instance);
```

### Integration Tests (Docker + Marten)

```csharp
// Full integration tests require PostgreSQL via Docker/Testcontainers
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
public class LawfulBasisIntegrationTests
{
    private readonly MartenFixture _fixture;
    // ... test against real Marten event store
}
```

## Observability

- **Tracing**: `Encina.Compliance.LawfulBasis` ActivitySource with basis-specific tags (`lawfulbasis.request_type`, `lawfulbasis.basis`, `lawfulbasis.outcome`)
- **Metrics**: 9 counters (`lawfulbasis.validations.total`, `lawfulbasis.consent_checks.total`, `lawfulbasis.lia_checks.total`, `lawfulbasis.registrations.created`, `lawfulbasis.registrations.revoked`, `lawfulbasis.basis.changed`, `lawfulbasis.lia.created`, `lawfulbasis.lia.approved`, `lawfulbasis.lia.rejected`) + 1 histogram (`lawfulbasis.validation.duration`)
- **Logging**: 17 structured log events via `[LoggerMessage]` source generator (EventId 8350-8386, zero-allocation)
- **Health Check**: Verifies `ILawfulBasisService` resolvability and reports pending LIA reviews

## Related Packages

| Package | Description |
|---------|-------------|
| `Encina` | Core CQRS pipeline with `IPipelineBehavior` |
| `Encina.DomainModeling` | `AggregateBase` for event-sourced aggregates |
| `Encina.Marten` | Marten event sourcing infrastructure |
| `Encina.Caching` | `ICacheProvider` for cache-aside pattern |
| `Encina.Compliance.GDPR` | `LawfulBasis` enum, `LawfulBasisAttribute`, RoPA |
| `Encina.Compliance.Consent` | Consent management (for consent-based processing) |

## GDPR Compliance

This package implements key GDPR requirements:

| Article | Requirement | Implementation |
|---------|-------------|----------------|
| **5(2)** | Accountability principle | Full event-sourced audit trail of all basis changes |
| **6(1)** | Lawful processing requires legal basis | `LawfulBasisAggregate` with 6 basis types |
| **6(1)(a)** | Consent as lawful basis | Integration with `IConsentStatusProvider` |
| **6(1)(f)** | Legitimate interests | `LIAAggregate` with EDPB three-part test |
| **Art. 6** | Document lawful basis | Event stream provides immutable documentation |

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.
