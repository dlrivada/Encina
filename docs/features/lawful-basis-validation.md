# Lawful Basis Validation in Encina

This guide explains how to enforce GDPR Article 6 lawful basis validation declaratively at the CQRS pipeline level using the `Encina.Compliance.GDPR` package. Lawful basis validation operates independently of the transport layer, ensuring consistent Article 6 compliance across all entry points.

## Table of Contents

1. [Overview](#overview)
2. [The Problem](#the-problem)
3. [The Solution](#the-solution)
4. [Quick Start](#quick-start)
5. [LawfulBasis Attribute](#lawfulbasis-attribute)
6. [Lawful Basis Registry](#lawful-basis-registry)
7. [Legitimate Interest Assessment (LIA)](#legitimate-interest-assessment-lia)
8. [Consent Integration](#consent-integration)
9. [Enforcement Modes](#enforcement-modes)
10. [Configuration Options](#configuration-options)
11. [Database Providers](#database-providers)
12. [Observability](#observability)
13. [Error Handling](#error-handling)
14. [Best Practices](#best-practices)
15. [Testing](#testing)
16. [FAQ](#faq)

---

## Overview

Encina's Lawful Basis Validation extends the existing `Encina.Compliance.GDPR` package with Article 6 enforcement at the pipeline level:

| Component | Description |
|-----------|-------------|
| **`[LawfulBasis]` Attribute** | Declarative lawful basis declaration on request types |
| **`LawfulBasisValidationPipelineBehavior`** | Pipeline behavior that validates lawful basis and short-circuits on failure |
| **`ILawfulBasisRegistry`** | Central registry for lawful basis declarations per request type |
| **`ILIAStore`** | Legitimate Interest Assessment persistence and retrieval |
| **`ILegitimateInterestAssessment`** | EDPB three-part test validation for legitimate interest basis |
| **`IConsentStatusProvider`** | Consent verification for consent-based processing |
| **`LawfulBasisOptions`** | Configuration for enforcement mode, assembly scanning, LIA requirements |

### Why Pipeline-Level Lawful Basis?

| Benefit | Description |
|---------|-------------|
| **Automatic enforcement** | Lawful basis is validated whenever a request processes personal data |
| **Declarative** | Legal basis lives with the request type, not scattered across documentation |
| **Transport-agnostic** | Same enforcement for HTTP, message queue, gRPC, and serverless |
| **LIA workflow** | Legitimate Interest Assessments are validated per EDPB guidelines |
| **Zero overhead** | Static attribute caching means zero reflection cost after first access |

---

## The Problem

GDPR Article 6 requires that every processing operation has a valid lawful basis. Applications typically struggle with:

- **Undocumented processing basis** across controllers and services
- **No runtime validation** that declared basis is still valid
- **Missing LIA documentation** for legitimate interest claims
- **No consent verification** when consent is the chosen basis
- **Inconsistent enforcement** across different transport layers

---

## The Solution

Encina solves this with a single attribute and pipeline behavior:

```text
Request → [LawfulBasisValidationPipelineBehavior] → Handler
                    │
                    ├── No [LawfulBasis]? → Skip (zero overhead)
                    ├── Disabled mode? → Skip
                    ├── Lookup registry for declared basis
                    ├── Basis = Consent? → Verify via IConsentStatusProvider
                    ├── Basis = LegitimateInterests? → Validate LIA via ILIAStore
                    ├── Valid? → Proceed to handler
                    ├── Invalid + Block mode? → Return EncinaError
                    └── Invalid + Warn mode? → Log warning, proceed
```

---

## Quick Start

### 1. Services Are Already in Encina.Compliance.GDPR

Lawful Basis Validation is part of the existing `Encina.Compliance.GDPR` package — no additional package required.

### 2. Register Services

```csharp
services.AddEncina(config =>
    config.RegisterServicesFromAssemblyContaining<Program>());

services.AddEncinaGDPR(options =>
{
    // Existing GDPR/RoPA configuration...
});

services.AddEncinaLawfulBasis(options =>
{
    options.EnforcementMode = LawfulBasisEnforcementMode.Block;
    options.ValidateLIAForLegitimateInterests = true;
    options.ScanAssembly(typeof(Program).Assembly);
});
```

### 3. Decorate Request Types

```csharp
[LawfulBasis(LawfulBasis.Consent)]
public sealed record SendMarketingEmailCommand(string UserId, string Content) : IRequest<Unit>;

[LawfulBasis(LawfulBasis.Contract)]
public sealed record ProcessOrderCommand(string OrderId) : IRequest<OrderResult>;

[LawfulBasis(LawfulBasis.LegitimateInterests, LIAReference = "LIA-ANALYTICS-001")]
public sealed record TrackUserBehaviorCommand(string UserId) : IRequest<Unit>;

// No attribute: pipeline skips lawful basis checks entirely
public sealed record GetPublicCatalogQuery() : IQuery<CatalogDto>;
```

### 4. Register a Legitimate Interest Assessment (if needed)

```csharp
var liaStore = serviceProvider.GetRequiredService<ILIAStore>();

var lia = new LIARecord
{
    Id = "LIA-ANALYTICS-001",
    Name = "User Behavior Analytics",
    Purpose = "Improve product recommendations",
    LegitimateInterest = "Better user experience through personalization",
    Benefits = "Improved conversion rates and user satisfaction",
    ConsequencesIfNotProcessed = "Generic recommendations, reduced engagement",
    NecessityJustification = "Analytics is the least intrusive method",
    AlternativesConsidered = ["Surveys", "Focus groups"],
    DataMinimisationNotes = "Only anonymized behavioral data",
    NatureOfData = "Browsing patterns, click streams",
    ReasonableExpectations = "Users expect personalized recommendations",
    ImpactAssessment = "Minimal impact — anonymized data only",
    Safeguards = ["Anonymization", "Access controls", "30-day retention"],
    Outcome = LIAOutcome.Approved,
    Conclusion = "Approved: legitimate interest outweighs data subject rights",
    AssessedAtUtc = DateTimeOffset.UtcNow,
    AssessedBy = "Jane Smith, DPO"
};

await liaStore.StoreAsync(lia);
```

### 5. Send Request (Lawful Basis Validated Automatically)

```csharp
var encina = serviceProvider.GetRequiredService<IEncina>();
var result = await encina.Send(new TrackUserBehaviorCommand("user-123"));

result.Match(
    Right: _ => Console.WriteLine("Tracking started"),
    Left: error => Console.WriteLine($"Blocked: {error.Code}")
    // Possible codes: "lawful_basis.missing", "lawful_basis.lia_rejected", etc.
);
```

---

## LawfulBasis Attribute

The `[LawfulBasis]` attribute declares the legal basis for processing on request types:

```csharp
// Simple basis declaration
[LawfulBasis(LawfulBasis.Contract)]
public sealed record FulfillOrderCommand(string OrderId) : IRequest<Unit>;

// With LIA reference for legitimate interests
[LawfulBasis(LawfulBasis.LegitimateInterests, LIAReference = "LIA-FRAUD-001")]
public sealed record FraudDetectionCommand(string TransactionId) : IRequest<FraudResult>;

// Legal obligation
[LawfulBasis(LawfulBasis.LegalObligation)]
public sealed record TaxReportingCommand(string TaxYear) : IRequest<TaxReport>;
```

### The Six Lawful Bases (Article 6(1))

| Basis | Use Case | Additional Validation |
|-------|----------|----------------------|
| `Consent` | Marketing emails, cookies, profiling | Verifies active consent via `IConsentStatusProvider` |
| `Contract` | Order fulfillment, account management | No additional validation needed |
| `LegalObligation` | Tax reporting, regulatory compliance | No additional validation needed |
| `VitalInterests` | Emergency medical processing | No additional validation needed |
| `PublicTask` | Government services, public interest | No additional validation needed |
| `LegitimateInterests` | Fraud detection, analytics, security | Validates LIA via `ILIAStore` (if configured) |

---

## Lawful Basis Registry

The registry maintains all lawful basis declarations:

```csharp
var registry = serviceProvider.GetRequiredService<ILawfulBasisRegistry>();

// Manual registration
await registry.RegisterAsync(new LawfulBasisRegistration
{
    RequestType = typeof(ProcessOrderCommand),
    Basis = LawfulBasis.Contract,
    Description = "Order fulfillment requires contract basis",
    RegisteredAtUtc = DateTimeOffset.UtcNow,
    Source = "manual"
});

// Auto-registration from assembly attributes (recommended)
registry.AutoRegisterFromAssemblies([typeof(Program).Assembly]);

// Query registrations
var result = await registry.GetByRequestTypeAsync(typeof(ProcessOrderCommand));
```

### Auto-Registration

The `LawfulBasisAutoRegistrationHostedService` automatically scans assemblies at startup:

```csharp
services.AddEncinaLawfulBasis(options =>
{
    options.ScanAssembly(typeof(Program).Assembly);
    options.ScanAssembly(typeof(SharedCommands).Assembly);
});
```

---

## Legitimate Interest Assessment (LIA)

When `LawfulBasis.LegitimateInterests` is declared, the pipeline can validate a Legitimate Interest Assessment following the EDPB three-part test:

### The EDPB Three-Part Test

| Test | LIARecord Property | Description |
|------|-------------------|-------------|
| **Purpose Test** | `Purpose`, `LegitimateInterest` | Is the interest legitimate and clearly defined? |
| **Necessity Test** | `NecessityJustification`, `AlternativesConsidered`, `DataMinimisationNotes` | Is processing necessary and proportionate? |
| **Balancing Test** | `NatureOfData`, `ReasonableExpectations`, `ImpactAssessment`, `Safeguards` | Do the controller's interests override data subject rights? |

### LIA Outcomes

| Outcome | Effect |
|---------|--------|
| `Approved` | Processing proceeds normally |
| `Rejected` | Processing is blocked (in Block mode) |
| `RequiresReview` | Processing is blocked pending DPO review |

### Querying Pending Reviews

```csharp
var store = serviceProvider.GetRequiredService<ILIAStore>();
var pending = await store.GetPendingReviewAsync();
// Returns all LIA records with RequiresReview outcome
```

---

## Consent Integration

When `LawfulBasis.Consent` is the declared basis, the pipeline automatically verifies consent status:

```csharp
// Implement IConsentStatusProvider to bridge with your consent system
public sealed class ConsentBridge : IConsentStatusProvider
{
    private readonly IConsentStore _consentStore;

    public ConsentBridge(IConsentStore consentStore) => _consentStore = consentStore;

    public async ValueTask<ConsentCheckResult> CheckConsentAsync(
        string subjectId, IReadOnlyList<string> purposes, CancellationToken ct = default)
    {
        var valid = await _consentStore.HasValidConsentAsync(subjectId, purposes.First(), ct);
        return valid.Match(
            Right: isValid => isValid
                ? ConsentCheckResult.Granted
                : ConsentCheckResult.Denied("No active consent found"),
            Left: error => ConsentCheckResult.Denied(error.Message));
    }
}
```

---

## Enforcement Modes

Three enforcement modes control pipeline behavior:

| Mode | Behavior |
|------|----------|
| `Block` | Invalid basis returns `EncinaError` and short-circuits the pipeline |
| `Warn` | Invalid basis logs a warning but allows processing to continue |
| `Disabled` | Pipeline behavior is completely skipped (zero overhead) |

```csharp
services.AddEncinaLawfulBasis(options =>
{
    // Production: enforce strictly
    options.EnforcementMode = LawfulBasisEnforcementMode.Block;

    // Development: warn but don't block
    options.EnforcementMode = LawfulBasisEnforcementMode.Warn;

    // Testing: disable entirely
    options.EnforcementMode = LawfulBasisEnforcementMode.Disabled;
});
```

---

## Configuration Options

```csharp
services.AddEncinaLawfulBasis(options =>
{
    // Enforcement mode (default: Warn)
    options.EnforcementMode = LawfulBasisEnforcementMode.Block;

    // Validate LIA for LegitimateInterests basis (default: true)
    options.ValidateLIAForLegitimateInterests = true;

    // Assembly scanning for auto-registration
    options.ScanAssembly(typeof(Program).Assembly);
    options.ScanAssembly(typeof(SharedModule).Assembly);
});
```

---

## Database Providers

LawfulBasis registrations and LIA records can be persisted across all 13 supported providers:

| Category | Providers | Registry Store | LIA Store |
|----------|-----------|---------------|-----------|
| **ADO.NET** | Sqlite, SqlServer, PostgreSQL, MySQL | `LawfulBasisRegistryAdo*` | `LIAStoreAdo*` |
| **Dapper** | Sqlite, SqlServer, PostgreSQL, MySQL | `LawfulBasisRegistryDapper*` | `LIAStoreDapper*` |
| **EF Core** | Sqlite, SqlServer, PostgreSQL, MySQL | `LawfulBasisRegistryEF` | `LIAStoreEF` |
| **MongoDB** | MongoDB | `LawfulBasisRegistryMongo` | `LIAStoreMongo` |

Each provider implements `ILawfulBasisRegistry` and `ILIAStore` with provider-specific SQL/queries and upsert semantics.

---

## Observability

### OpenTelemetry Tracing

The pipeline emits traces via a dedicated `Encina.Compliance.GDPR.LawfulBasis` ActivitySource:

| Tag | Description |
|-----|-------------|
| `request.type` | The request type being validated |
| `lawful_basis.declared` | Whether a lawful basis was declared |
| `basis` | The declared lawful basis (e.g., "Consent") |
| `lawful_basis.valid` | Whether the validation passed |
| `failure_reason` | Reason for validation failure |

### Metrics

Three counters track validation activity:

| Counter | Description |
|---------|-------------|
| `lawful_basis_validations_total` | Total validations, tagged with `basis` and `outcome` |
| `lawful_basis_consent_checks_total` | Total consent status checks, tagged with `outcome` |
| `lawful_basis_lia_checks_total` | Total LIA checks, tagged with `outcome` |

---

## Error Handling

The pipeline returns structured `EncinaError` codes:

| Error Code | Description |
|------------|-------------|
| `lawful_basis.missing` | No `[LawfulBasis]` attribute and not in registry |
| `lawful_basis.not_registered` | Attribute found but not registered in registry |
| `lawful_basis.consent_required` | Consent basis declared but no `IConsentStatusProvider` registered |
| `lawful_basis.consent_denied` | Consent check returned denied |
| `lawful_basis.lia_required` | Legitimate interest declared but no LIA found |
| `lawful_basis.lia_rejected` | LIA outcome is Rejected |
| `lawful_basis.lia_pending` | LIA outcome is RequiresReview |

---

## Best Practices

### 1. Always Declare Basis on Data Processing Commands

```csharp
// Good: explicit basis
[LawfulBasis(LawfulBasis.Contract)]
public sealed record CreateAccountCommand(string Email) : IRequest<AccountId>;

// Bad: no basis — will be blocked in Block mode
public sealed record CreateAccountCommand(string Email) : IRequest<AccountId>;
```

### 2. Use LIA References for Legitimate Interest

```csharp
// Good: traceable LIA reference
[LawfulBasis(LawfulBasis.LegitimateInterests, LIAReference = "LIA-FRAUD-2024")]
public sealed record DetectFraudCommand(string TxId) : IRequest<FraudResult>;
```

### 3. Keep LIA Records Updated

Review and update LIA records periodically, especially when:
- Processing scope changes
- New data categories are added
- Regulatory guidance changes (EDPB opinions)

### 4. Start with Warn Mode

```csharp
// Development/staging: identify missing declarations
options.EnforcementMode = LawfulBasisEnforcementMode.Warn;

// Production: enforce after fixing all warnings
options.EnforcementMode = LawfulBasisEnforcementMode.Block;
```

---

## Testing

### Unit Testing with In-Memory Stores

```csharp
var registry = new InMemoryLawfulBasisRegistry();
var liaStore = new InMemoryLIAStore();

// Register a basis
await registry.RegisterAsync(new LawfulBasisRegistration
{
    RequestType = typeof(MyCommand),
    Basis = LawfulBasis.Contract,
    RegisteredAtUtc = DateTimeOffset.UtcNow,
    Source = "test"
});

// Verify registration
var result = await registry.GetByRequestTypeAsync(typeof(MyCommand));
result.IsRight.Should().BeTrue();
```

### Testing with Encina.Testing.Fakes

```csharp
// The pipeline behavior can be tested by registering fake providers
services.AddSingleton<ILawfulBasisRegistry>(new InMemoryLawfulBasisRegistry());
services.AddSingleton<ILIAStore>(new InMemoryLIAStore());
services.AddSingleton<IConsentStatusProvider>(new AlwaysGrantedConsentProvider());
```

### Test Coverage

The Lawful Basis feature has comprehensive test coverage:

| Test Type | Count | Scope |
|-----------|-------|-------|
| Unit Tests | 70 | Pipeline behavior, all code paths, enforcement modes |
| Integration Tests | 137 | All 13 database providers (ADO ×4, Dapper ×4, EF Core ×4, MongoDB) |
| Guard Tests | 19 | Null parameter validation on all public methods |
| Property Tests | 17 | FsCheck invariants for registry, store, and validation results |
| Contract Tests | 26 | Interface contract compliance for all implementations |
| Benchmarks | 18 | 11 store operations + 7 pipeline scenarios (BenchmarkDotNet) |
| Load Tests | 8 | 50 concurrent workers × 10K operations per scenario |

### Performance Benchmarks

Measured with BenchmarkDotNet (`[MemoryDiagnoser]`, `[RankColumn]`). Run via:

```bash
cd tests/Encina.BenchmarkTests/Encina.Benchmarks
dotnet run -c Release -- --filter "*LawfulBasis*"
```

**Pipeline overhead** (end-to-end with DI, single invocation, `--job dry`):

| Scenario | Mean | Allocated | Notes |
|----------|------|-----------|-------|
| Disabled mode (no validation) | ~1.1 ms | ~29 KB | Baseline: pipeline skip path |
| No `[LawfulBasis]` attribute | ~1.2 ms | ~30 KB | Early exit, no registry lookup |
| Block mode, Contract basis | ~21 ms | ~30 KB | First-call includes JIT; steady-state is sub-ms |
| Block mode, missing basis | ~1.7 ms | ~30 KB | Rejected before handler execution |
| Consent basis, consent present | ~4.5 ms | ~37 KB | Includes `IConsentStatusProvider` check |
| LegitimateInterests, LIA approved | ~5.4 ms | ~37 KB | Includes `ILIAStore` lookup |

**Store operations** (in-memory, 1000 pre-seeded records, `--job dry`):

| Operation | Mean | Allocated | Notes |
|-----------|------|-----------|-------|
| Registry: GetByRequestType | ~560 µs | ~6 KB | ConcurrentDictionary O(1) lookup |
| Registry: GetByRequestTypeName | ~536 µs | ~1 KB | String-keyed lookup |
| Registry: Register (upsert) | ~520 µs | ~6 KB | Idempotent write |
| LIA Store: GetByReference | ~558 µs | ~1.4 KB | ConcurrentDictionary O(1) |
| LIA Store: Store | ~544 µs | ~2 KB | Upsert semantics |
| LIA Store: GetPendingReview | ~677 µs | ~15 KB | Filtered enumeration |
| FromAttribute (decorated) | ~176 µs | ~6 KB | Cached after first access |
| FromAttribute (undecorated) | ~157 µs | ~4 KB | Early return (no attribute) |

> **Note**: Dry-mode results (1 iteration) include JIT overhead. For production-quality measurements, run with `--job short` or default configuration.

### Load Test Scenarios

8 high-concurrency scenarios (50 workers × 10,000 operations each):

| Scenario | Operations | Validation |
|----------|-----------|------------|
| Registry concurrent reads | 500,000 | Throughput stability, zero errors |
| Registry concurrent writes | 500,000 | All upserts succeed, no contention |
| LIA Store concurrent reads | 500,000 | Consistent results under load |
| LIA Store concurrent writes | 500,000 | Thread-safe ConcurrentDictionary |
| LIA GetPendingReview under load | 500,000 | Correct filtering while writes occur |
| Mixed Registry + LIA operations | 500,000 | Cross-store concurrent access |
| Pipeline validation concurrent | 50,000 | End-to-end pipeline under load |
| Registry latency distribution | 500,000 | P50/P95/P99 percentile tracking |

Run via:

```csharp
await LawfulBasisValidationLoadTests.RunAllAsync();
```

---

## FAQ

### Q: Do I need a separate package for Lawful Basis?

No. Lawful Basis Validation is part of `Encina.Compliance.GDPR`. Register it with `AddEncinaLawfulBasis()`.

### Q: What happens if no `[LawfulBasis]` attribute is present?

The pipeline behavior skips entirely with zero overhead (no reflection, no registry lookup). Only decorated request types are validated.

### Q: How does this relate to Consent Management?

Lawful Basis Validation determines *which* legal basis applies. When the basis is `Consent`, it delegates to `IConsentStatusProvider` to verify active consent. The `Encina.Compliance.Consent` package provides a full consent lifecycle; Lawful Basis uses it as a validation source.

### Q: What if my LIA is pending review?

If `LIAOutcome.RequiresReview` is the outcome, the pipeline returns `lawful_basis.lia_pending` in Block mode. Use `ILIAStore.GetPendingReviewAsync()` to find all assessments awaiting DPO review.

### Q: Can I register lawful basis at runtime?

Yes. Use `ILawfulBasisRegistry.RegisterAsync()` for dynamic registration. However, the recommended approach is declarative via `[LawfulBasis]` attributes with auto-registration at startup.

### Q: What is the performance overhead?

Minimal. The attribute is cached per closed generic type via `static readonly` fields (CLR static initialization guarantee). Registry lookups use `ConcurrentDictionary` with O(1) amortized cost. BenchmarkDotNet dry-mode measurements show store lookups at ~500-560 µs (includes JIT overhead on first call) and the full pipeline path at ~1-5 ms depending on basis type. Steady-state performance after JIT warmup is sub-millisecond for simple bases (Contract, LegalObligation) and low-millisecond for bases requiring external validation (Consent, LegitimateInterests). Run `dotnet run -c Release -- --filter "*LawfulBasis*"` in the Benchmarks project for production-quality measurements on your hardware.

---

## Related Resources

- [GDPR Article 6](https://gdpr-info.eu/art-6-gdpr/) — Lawfulness of processing
- [ICO Lawful Basis Interactive Guidance Tool](https://ico.org.uk/for-organisations/guidance-for-organisations/lawful-basis-interactive-guidance-tool/)
- [EDPB Legitimate Interest Guidelines](https://edpb.europa.eu/our-work-tools/our-documents/opinion-board-art-64/guidelines-legitimate-interest_en)
- [Consent Management Feature Guide](consent-management.md) — For consent-based processing
- [GDPR Compliance Feature Guide](gdpr-compliance.md) — For RoPA and processing activity tracking
