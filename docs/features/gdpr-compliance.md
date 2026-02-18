# GDPR Compliance in Encina

This guide explains how to enforce GDPR compliance declaratively at the CQRS pipeline level using the `Encina.Compliance.GDPR` package. Processing activity tracking operates independently of the transport layer, ensuring consistent Article 30 compliance across all entry points.

## Table of Contents

1. [Overview](#overview)
2. [The Problem](#the-problem)
3. [The Solution](#the-solution)
4. [Quick Start](#quick-start)
5. [Processing Activity Attributes](#processing-activity-attributes)
6. [Processing Activity Registry](#processing-activity-registry)
7. [RoPA Export](#ropa-export)
8. [Configuration Options](#configuration-options)
9. [Enforcement Modes](#enforcement-modes)
10. [Observability](#observability)
11. [Health Check](#health-check)
12. [Error Handling](#error-handling)
13. [Best Practices](#best-practices)
14. [Testing](#testing)
15. [FAQ](#faq)

---

## Overview

Encina.Compliance.GDPR provides attribute-based GDPR compliance at the CQRS pipeline level:

| Component | Description |
|-----------|-------------|
| **Processing Activity Attributes** | Declarative GDPR Article 30 metadata on request types |
| **GDPRCompliancePipelineBehavior** | Pipeline behavior that validates compliance and short-circuits on failure |
| **IProcessingActivityRegistry** | Central registry for all processing activities |
| **RoPA Exporters** | JSON and CSV export of Record of Processing Activities |
| **GDPROptions** | Configuration for enforcement mode, controller info, auto-registration |

### Why Pipeline-Level GDPR?

| Benefit | Description |
|---------|-------------|
| **Automatic tracking** | Processing activities are tracked whenever a request processes personal data |
| **Declarative** | GDPR metadata lives with the request type, not scattered across documentation |
| **Enforceable** | Unregistered processing can be blocked at runtime |
| **Auditable** | RoPA can be exported at any time for regulatory submission |
| **Transport-agnostic** | Same compliance applies whether the request comes via HTTP, message queue, or serverless |

---

## The Problem

GDPR Article 30 requires organizations to maintain a Record of Processing Activities (RoPA). Traditionally this is maintained in spreadsheets or external tools, disconnected from the actual code:

```csharp
// Problem 1: GDPR documentation exists only in a spreadsheet
// No enforcement at runtime that the documented processing matches reality

// Problem 2: New features process personal data without updating the RoPA
public sealed record ExportUserDataQuery(Guid UserId) : IQuery<UserDataExport>;
// Who updates the spreadsheet when this query is added?

// Problem 3: No runtime verification that processing is lawful
// A developer can deploy code processing personal data without any compliance check
```

---

## The Solution

With Encina.Compliance.GDPR, processing activities are declared directly on request types and enforced at the pipeline level:

```csharp
// Processing activity is declared with the request type
[ProcessingActivity(
    Purpose = "Export user's personal data for portability request",
    LawfulBasis = LawfulBasis.LegalObligation,
    DataCategories = ["Name", "Email", "Address", "OrderHistory"],
    DataSubjects = ["Customers"],
    RetentionDays = 30,
    SecurityMeasures = "AES-256 encryption, TLS in transit")]
public sealed record ExportUserDataQuery(Guid UserId) : IQuery<UserDataExport>;

// Pipeline automatically:
// 1. Detects the [ProcessingActivity] attribute
// 2. Looks up the registry entry
// 3. Validates compliance
// 4. Logs the processing activity with OpenTelemetry
// 5. Blocks if non-compliant (in Enforce mode)
```

---

## Quick Start

### 1. Install the package

```bash
dotnet add package Encina.Compliance.GDPR
```

### 2. Register services

```csharp
services.AddEncinaGDPR(options =>
{
    options.ControllerName = "Acme Corp";
    options.ControllerEmail = "privacy@acme.com";
    options.DataProtectionOfficer = new DataProtectionOfficer(
        "Jane Doe", "dpo@acme.com", "+1-555-0100");
    options.BlockUnregisteredProcessing = true;
    options.EnforcementMode = GDPREnforcementMode.Enforce;
    options.AssembliesToScan.Add(typeof(Program).Assembly);
});
```

### 3. Decorate request types

```csharp
[ProcessingActivity(
    Purpose = "Process customer orders",
    LawfulBasis = LawfulBasis.Contract,
    DataCategories = ["Name", "Email", "ShippingAddress"],
    DataSubjects = ["Customers"],
    RetentionDays = 730,
    SecurityMeasures = "AES-256 encryption at rest")]
public sealed record PlaceOrderCommand(OrderData Data) : ICommand<OrderId>;
```

### 4. Export RoPA

```csharp
var registry = serviceProvider.GetRequiredService<IProcessingActivityRegistry>();
var exporter = serviceProvider.GetRequiredService<JsonRoPAExporter>();

var activities = await registry.GetAllActivitiesAsync();
var list = activities.Match(Right: a => a, Left: _ => []);

var metadata = new RoPAExportMetadata(
    "Acme Corp", "privacy@acme.com", DateTimeOffset.UtcNow,
    new DataProtectionOfficer("Jane Doe", "dpo@acme.com"));

var result = await exporter.ExportAsync(list, metadata);
// result.Content contains the RoPA as a byte array
```

---

## Processing Activity Attributes

### `[ProcessingActivity]` — Full Declaration

Declares a complete GDPR Article 30 processing activity:

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `Purpose` | `string` | Yes | Purpose of processing (Article 30(1)(b)) |
| `LawfulBasis` | `LawfulBasis` | Yes | Legal basis under Article 6(1) |
| `DataCategories` | `string[]` | Yes | Categories of personal data (Article 30(1)(c)) |
| `DataSubjects` | `string[]` | Yes | Categories of data subjects (Article 30(1)(c)) |
| `RetentionDays` | `int` | No | Retention period in days (Article 30(1)(f)) |
| `SecurityMeasures` | `string` | No | Technical/organizational measures (Article 30(1)(g)) |
| `Recipients` | `string[]` | No | Recipients of personal data (Article 30(1)(d)) |
| `ThirdCountryTransfers` | `string` | No | Third country transfer safeguards (Article 30(1)(e)) |
| `Safeguards` | `string` | No | Additional safeguards description |

### `[ProcessesPersonalData]` — Marker Attribute

Lightweight marker indicating the request processes personal data but does not declare the full activity inline. The activity must be registered separately in the `IProcessingActivityRegistry`.

```csharp
// Use when activity details are managed externally (e.g., database-backed registry)
[ProcessesPersonalData]
public sealed record ImportUserDataCommand(Stream Data) : ICommand;
```

---

## Processing Activity Registry

The `IProcessingActivityRegistry` manages processing activities:

```csharp
public interface IProcessingActivityRegistry
{
    ValueTask<Either<EncinaError, Unit>> RegisterActivityAsync(ProcessingActivity activity, ...);
    ValueTask<Either<EncinaError, Option<ProcessingActivity>>> GetActivityByRequestTypeAsync(Type requestType, ...);
    ValueTask<Either<EncinaError, IReadOnlyList<ProcessingActivity>>> GetAllActivitiesAsync(...);
    ValueTask<Either<EncinaError, Unit>> UpdateActivityAsync(ProcessingActivity activity, ...);
}
```

### Default: InMemoryProcessingActivityRegistry

Thread-safe, singleton-scoped, in-memory implementation. Suitable for most applications where activities are declared via attributes.

### Custom Implementation

Register a custom implementation (e.g., database-backed) before `AddEncinaGDPR()`:

```csharp
services.AddSingleton<IProcessingActivityRegistry, DatabaseProcessingActivityRegistry>();
services.AddEncinaGDPR(); // Won't override your registration (TryAdd semantics)
```

---

## RoPA Export

Two built-in exporters for Article 30 compliance reporting:

| Exporter | Content Type | Use Case |
|----------|-------------|----------|
| `JsonRoPAExporter` | `application/json` | API responses, archival |
| `CsvRoPAExporter` | `text/csv` | Regulatory submission, spreadsheet import |

Both return `RoPAExportResult` containing the export as a byte array with metadata.

---

## Configuration Options

| Option | Default | Description |
|--------|---------|-------------|
| `ControllerName` | `null` | Organization name (Article 30(1)(a)) |
| `ControllerEmail` | `null` | Controller contact email |
| `DataProtectionOfficer` | `null` | DPO details (Articles 37-39) |
| `BlockUnregisteredProcessing` | `false` | Block unregistered processing activities |
| `EnforcementMode` | `Enforce` | `Enforce` or `WarnOnly` |
| `AutoRegisterFromAttributes` | `true` | Scan assemblies at startup |
| `AddHealthCheck` | `false` | Register health check |
| `AssembliesToScan` | `[]` | Assemblies for auto-registration |

---

## Enforcement Modes

| Mode | Non-Compliant Behavior | Use Case |
|------|----------------------|----------|
| `Enforce` | Returns `EncinaError`, blocks request | Production |
| `WarnOnly` | Logs warning, request proceeds | Migration, staging |

Combined with `BlockUnregisteredProcessing`:

| `BlockUnregisteredProcessing` | `EnforcementMode` | Unregistered Request | Non-Compliant Request |
|------|------|------|------|
| `false` | `Enforce` | Logs warning, proceeds | Returns error |
| `false` | `WarnOnly` | Logs warning, proceeds | Logs warning, proceeds |
| `true` | `Enforce` | Returns error | Returns error |
| `true` | `WarnOnly` | Returns error | Logs warning, proceeds |

---

## Observability

- **Tracing**: `Encina.Compliance.GDPR` ActivitySource with lawful basis and request type tags
- **Logging**: Structured log events via `LoggerMessage.Define` (zero-allocation)
- **Health Check**: Verifies registry population, controller info, and validator registration

---

## Error Handling

All errors follow Encina's Railway Oriented Programming pattern (`Either<EncinaError, T>`):

| Error Code | Meaning |
|------------|---------|
| `gdpr.unregistered_activity` | Request processes personal data but has no registry entry |
| `gdpr.compliance_validation_failed` | Compliance validator reported non-compliance |
| `gdpr.registry_lookup_failed` | Registry lookup error (wraps inner error) |
| `gdpr.ropa_export_serialization_failed` | RoPA export serialization error |

---

## Best Practices

1. **Declare activities with attributes** — Keep GDPR metadata with the code that processes data
2. **Use `Enforce` mode in production** — Block non-compliant processing
3. **Use `WarnOnly` during migration** — Identify gaps without breaking existing functionality
4. **Export RoPA regularly** — Automate periodic RoPA exports for compliance records
5. **Configure controller info** — Required for valid RoPA under Article 30(1)(a)
6. **Register custom validators** — Add business-specific compliance rules via `IGDPRComplianceValidator`
7. **Enable health check** — Monitor GDPR configuration health in production

---

## Testing

The package includes 135 tests across 4 test projects:

| Project | Tests | Coverage |
|---------|-------|----------|
| UnitTests | 100 | Core logic, pipeline behavior, registry, exporters, validators |
| GuardTests | 11 | Null checks on all public methods |
| PropertyTests | 8 | Invariants (register-then-get, count consistency, export counts) |
| ContractTests | 16 | Interface contracts, DI registration verification |

---

## FAQ

### Do I need to annotate every request type?

No. Only request types that process personal data need `[ProcessingActivity]` or `[ProcessesPersonalData]`. Requests without these attributes bypass all GDPR checks with zero overhead.

### Can I use a database-backed registry?

Yes. Implement `IProcessingActivityRegistry` and register it before `AddEncinaGDPR()`. The default `InMemoryProcessingActivityRegistry` will not override your registration (TryAdd semantics).

### How does auto-registration work?

At startup, a hosted service scans the configured assemblies for `[ProcessingActivity]` attributes and registers them in the `IProcessingActivityRegistry`. This is idempotent — duplicate registrations are skipped.

### What happens if the registry lookup fails?

The pipeline wraps the error in `GDPRErrors.RegistryLookupFailed` and returns it as an `EncinaError` (Left side of Either). The inner error is preserved for debugging.

---

## Related

- [Security Authorization](security-authorization.md) — Transport-agnostic security at the pipeline level
- [Encina.Compliance.GDPR README](../../src/Encina.Compliance.GDPR/README.md) — Package quick reference
