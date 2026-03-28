# Data Residency in Encina

This guide explains how to enforce GDPR Chapter V (Articles 44-49) data sovereignty and residency requirements -- declarative data residency enforcement at the CQRS pipeline level using the `Encina.Compliance.DataResidency` package. Residency enforcement operates independently of the transport layer, ensuring consistent data sovereignty compliance across all entry points.

## Table of Contents

1. [Overview](#overview)
2. [The Problem](#the-problem)
3. [The Solution](#the-solution)
4. [Quick Start](#quick-start)
5. [DataResidency Attribute](#dataresidency-attribute)
6. [NoCrossBorderTransfer Attribute](#nocrossbordertransfer-attribute)
7. [Region Model & Registry](#region-model--registry)
8. [Cross-Border Transfer Validation](#cross-border-transfer-validation)
9. [Fluent Policy Builder](#fluent-policy-builder)
10. [Audit Trail](#audit-trail)
11. [Configuration Options](#configuration-options)
12. [Enforcement Modes](#enforcement-modes)
13. [Database Providers](#database-providers)
14. [Observability](#observability)
15. [Health Check](#health-check)
16. [Error Handling](#error-handling)
17. [Integration with Other Compliance Modules](#integration-with-other-compliance-modules)
18. [Best Practices](#best-practices)
19. [Testing](#testing)
20. [FAQ](#faq)

---

## Overview

Encina.Compliance.DataResidency provides attribute-based data residency enforcement and cross-border transfer validation at the CQRS pipeline level:

| Component | Description |
|-----------|-------------|
| **`[DataResidency]` Attribute** | Declarative residency requirements on request types (allowed regions, adequacy decision requirement) |
| **`[NoCrossBorderTransfer]` Attribute** | Blanket prohibition of any cross-border transfer for highly sensitive data |
| **`DataResidencyPipelineBehavior`** | Pipeline behavior that validates the current region against declared policies before handler execution |
| **`Region`** | Sealed record representing a geographic region with EU/EEA membership, adequacy status, and protection level |
| **`RegionRegistry`** | Registry of 50+ pre-defined regions (EU members, EEA, adequacy countries, major non-adequate countries) |
| **`ICrossBorderTransferValidator`** | 5-step GDPR Chapter V validation hierarchy for cross-border transfers |
| **`IResidencyPolicyStore`** | Residency policy descriptor persistence (one policy per data category) |
| **`IDataLocationStore`** | Data location tracking -- records where entities are physically stored (Article 30) |
| **`IResidencyAuditStore`** | Immutable audit trail for all residency enforcement decisions |
| **`DataResidencyOptions`** | Configuration for enforcement mode, region, tracking, and policies |

### Why Pipeline-Level Residency?

| Benefit | Description |
|---------|-------------|
| **Pre-execution validation** | Residency policies are validated before the handler executes, preventing non-compliant processing |
| **Declarative** | Residency requirements live with the request types, not scattered across services |
| **Transport-agnostic** | Same residency enforcement for HTTP, message queue, gRPC, and serverless |
| **Zero-overhead opt-in** | Attribute presence is cached statically per closed generic type; zero reflection after first resolution |
| **Cross-border validation** | GDPR Chapter V hierarchy (adequacy, SCCs, BCRs, derogations) is evaluated automatically |
| **Data location tracking** | Records where data is stored for Article 30 compliance and regulatory audits |
| **Auditable** | Every enforcement decision is recorded with region, legal basis, and compliance metadata |

---

## The Problem

GDPR Chapter V (Articles 44-49) restricts international transfers of personal data to countries that do not ensure an adequate level of data protection. Organizations face several challenges with data residency compliance:

- **No region-aware processing** -- applications process requests without knowing which geographic region they operate in or whether the region is compliant
- **No cross-border transfer validation** -- data flows between regions without checking adequacy decisions, SCCs, or BCRs
- **No policy enforcement** -- residency policies exist in documentation but are not enforced in code
- **No data location tracking** -- no systematic record of where data is physically stored, making Article 30 compliance difficult
- **No audit trail** -- no evidence of compliance decisions for supervisory authority inquiries (Article 58)
- **Inconsistent enforcement** -- different teams apply different rules for the same data categories
- **Manual compliance checks** -- residency verification is a manual process prone to human error

---

## The Solution

Encina solves this with a unified residency pipeline that validates region compliance before processing and tracks data locations after:

```text
Request → [DataResidencyPipelineBehavior] (pre-handler validation)
                 |
                 +-- Step 1: Disabled mode? → Skip (no-op, zero overhead)
                 +-- Step 2: No [DataResidency] or [NoCrossBorderTransfer] attributes? → Skip
                 +-- Step 3: Resolve current region from IRegionContextProvider
                 |   +-- Failure + Block mode → Return error
                 |   +-- Failure + Warn mode → Proceed without validation
                 +-- Step 4: [DataResidency] → Check allowed regions via IDataResidencyPolicy
                 |   +-- Region not allowed + Block → Return error
                 |   +-- Adequacy required but missing + Block → Return error
                 +-- Step 5: [NoCrossBorderTransfer] → Record constraint in audit trail
                 +-- Step 6: Call next handler
                 +-- Step 7: Record data location (on success, if TrackDataLocations enabled)
                 +-- Step 8: Record outcome audit entry (if TrackAuditTrail enabled)

ICrossBorderTransferValidator (5-step GDPR Chapter V hierarchy)
       |
       +-- Step 1: Same region → Always allowed (no cross-border transfer)
       +-- Step 2: Both within EEA → Always allowed (free movement)
       +-- Step 3: Destination has adequacy decision (Art. 45) → Allowed
       +-- Step 4: Appropriate safeguards (SCCs, BCRs — Art. 46) → Allowed with safeguards
       +-- Step 5: No valid mechanism → Denied
```

---

## Quick Start

### 1. Install the Package

```bash
dotnet add package Encina.Compliance.DataResidency
```

### 2. Decorate Request Types with Residency Requirements

```csharp
// Allow processing only in Germany and France
[DataResidency("DE", "FR", DataCategory = "healthcare-data")]
public record CreatePatientRecordCommand(string PatientId) : ICommand<PatientId>;

// EU-only with adequacy decision requirement
[DataResidency("DE", "FR", "NL", "BE",
    DataCategory = "financial-records",
    RequireAdequacyDecision = true)]
public record CreateInvoiceCommand(string CustomerId) : ICommand<InvoiceId>;

// Strict no-transfer constraint for classified data
[NoCrossBorderTransfer(
    DataCategory = "classified-records",
    Reason = "National security regulation prohibits cross-border transfer")]
public record ProcessClassifiedDocumentCommand(string DocumentId) : ICommand;
```

### 3. Register Services

```csharp
services.AddEncina(config =>
    config.RegisterServicesFromAssemblyContaining<Program>());

services.AddEncinaDataResidency(options =>
{
    options.DefaultRegion = RegionRegistry.DE;
    options.EnforcementMode = DataResidencyEnforcementMode.Warn;
    options.TrackDataLocations = true;
    options.TrackAuditTrail = true;
    options.AddHealthCheck = true;
    options.AssembliesToScan.Add(typeof(Program).Assembly);
});
```

### 4. Configure Policies via Fluent API

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
        policy.AllowRegions(RegionRegistry.DE, RegionRegistry.FR);
        policy.AllowTransferBasis(TransferLegalBasis.StandardContractualClauses);
    });

    options.AddPolicy("marketing-data", policy =>
    {
        policy.AllowEEA();
        policy.AllowAdequate();
    });
});
```

### 5. Validate Cross-Border Transfers

```csharp
var validator = serviceProvider.GetRequiredService<ICrossBorderTransferValidator>();

var result = await validator.ValidateTransferAsync(
    source: RegionRegistry.DE,
    destination: RegionRegistry.US,
    dataCategory: "personal-data",
    cancellationToken);

result.Match(
    Right: transfer =>
    {
        if (transfer.IsAllowed)
        {
            Console.WriteLine($"Transfer allowed via {transfer.LegalBasis}");
            foreach (var safeguard in transfer.RequiredSafeguards)
                Console.WriteLine($"  Required: {safeguard}");
            foreach (var warning in transfer.Warnings)
                Console.WriteLine($"  Warning: {warning}");
        }
        else
        {
            Console.WriteLine($"Transfer denied: {transfer.DenialReason}");
        }
    },
    Left: error => Console.WriteLine($"Validation error: {error.Message}"));
```

---

## DataResidency Attribute

The `[DataResidency]` attribute marks request types as subject to data residency enforcement:

```csharp
// Restrict processing to specific regions
[DataResidency("DE", "FR", DataCategory = "healthcare-data")]
public record CreatePatientCommand(string PatientId) : ICommand<PatientId>;

// Require adequacy decision for the processing region
[DataResidency("DE", "FR", "NL", "BE",
    DataCategory = "financial-records",
    RequireAdequacyDecision = true)]
public record CreateInvoiceCommand(string CustomerId) : ICommand<InvoiceId>;

// No region restriction (all regions allowed), but tracked
[DataResidency(DataCategory = "general-data")]
public record CreateNoteCommand(string Content) : ICommand<NoteId>;
```

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `AllowedRegionCodes` | `string[]` | `[]` | Region codes where processing is permitted (ISO 3166-1 alpha-2 or custom identifiers); empty means no restriction |
| `DataCategory` | `string?` | `null` | Maps to a residency policy category; defaults to the request type name if not set |
| `RequireAdequacyDecision` | `bool` | `false` | When `true`, the processing region must have an EU adequacy decision (Article 45) |

The attribute is applied to the **request type** (not the response) and is evaluated **before** the handler executes. Each region code should match a `Region.Code` value (e.g., "DE", "FR", "US", "EU", or a custom identifier like "AZURE-WESTEU").

---

## NoCrossBorderTransfer Attribute

The `[NoCrossBorderTransfer]` attribute declares that data processed by a request must remain in the current processing region with no cross-border transfer permitted:

```csharp
// Strict no-transfer policy for classified data
[NoCrossBorderTransfer(
    DataCategory = "classified-records",
    Reason = "National security regulation prohibits any cross-border transfer")]
public record ProcessClassifiedDocumentCommand(string DocumentId) : ICommand;

// Simple no-transfer constraint
[NoCrossBorderTransfer]
public record UpdateLocalHealthRecordCommand(string PatientId) : ICommand;
```

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `DataCategory` | `string?` | `null` | Maps to a policy category; defaults to the request type name if not set |
| `Reason` | `string?` | `null` | Documents the legal or business justification for prohibiting cross-border transfers (Article 5(2) accountability) |

This attribute enforces a blanket prohibition on cross-border transfers -- the strictest level of data sovereignty enforcement. It is suitable for highly sensitive data categories where ANY transfer is prohibited regardless of adequacy decisions, SCCs, or other transfer mechanisms.

---

## Region Model & Registry

### Region Record

`Region` is a sealed record that represents a geographic region for data residency enforcement:

```csharp
// Access well-known regions from the registry
var germany = RegionRegistry.DE;
var japan = RegionRegistry.JP;

// Create a custom region for private cloud
var custom = Region.Create(
    code: "AZURE-WESTEU",
    country: "NL",
    isEU: true,
    isEEA: true,
    hasAdequacyDecision: true,
    protectionLevel: DataProtectionLevel.High);
```

| Property | Type | Description |
|----------|------|-------------|
| `Code` | `string` | Region code identifier (ISO 3166-1 alpha-2, regional code, or custom). Case-insensitive equality. |
| `Country` | `string` | ISO 3166-1 alpha-2 country code for the primary country |
| `IsEU` | `bool` | Whether the region is within the European Union (27 member states) |
| `IsEEA` | `bool` | Whether the region is within the European Economic Area (EU + IS, LI, NO) |
| `HasAdequacyDecision` | `bool` | Whether the European Commission has issued an adequacy decision (Art. 45) |
| `ProtectionLevel` | `DataProtectionLevel` | Overall data protection level: `High`, `Medium`, `Low`, or `Unknown` |

Region equality is based on **case-insensitive `Code` comparison**. Two regions with the same code (regardless of case) are considered equal.

When creating a region via `Region.Create`, if `isEU` or `isEEA` is `true`, `HasAdequacyDecision` is automatically set to `true`.

### DataProtectionLevel

| Level | Description | Examples |
|-------|-------------|---------|
| `High` | Comprehensive data protection framework | EU/EEA, countries with adequacy decisions |
| `Medium` | Partial or sector-specific protection | US (DPF), Brazil (LGPD), India (DPDP), Australia, Singapore |
| `Low` | Limited data protection framework | China (PIPL) |
| `Unknown` | Not assessed; treated as high-risk | Custom regions without evaluation |

### RegionRegistry

The `RegionRegistry` provides 50+ pre-defined regions organized in three categories:

**EU Member States (27):**
AT, BE, BG, HR, CY, CZ, DK, EE, FI, FR, DE, GR, HU, IE, IT, LV, LT, LU, MT, NL, PL, PT, RO, SK, SI, ES, SE

**EEA-only Countries (3):**
IS (Iceland), LI (Liechtenstein), NO (Norway)

**Countries with EU Adequacy Decisions (15):**
AD (Andorra), AR (Argentina), CA (Canada), FO (Faroe Islands), GG (Guernsey), IL (Israel), IM (Isle of Man), JP (Japan), JE (Jersey), NZ (New Zealand), KR (Republic of Korea), CH (Switzerland), GB (United Kingdom), UY (Uruguay), US (United States -- DPF)

**Major Non-Adequate Countries (5):**
AU (Australia), BR (Brazil), CN (China), IN (India), SG (Singapore)

**Composite Region:**
EU -- synthetic region representing the European Union as a whole.

### Aggregate Collections

| Collection | Count | Description |
|------------|-------|-------------|
| `RegionRegistry.EUMemberStates` | 27 | All EU member states; data flows freely under GDPR Art. 1(3) |
| `RegionRegistry.EEACountries` | 30 | EU + Iceland, Liechtenstein, Norway; GDPR applies throughout |
| `RegionRegistry.AdequacyCountries` | 15 | Non-EEA countries with adequacy decisions (Art. 45) |

### RegionGroup

`RegionGroup` provides named collections with efficient `Contains` lookups:

```csharp
// Use pre-built groups
var isInEU = RegionGroup.EUGroup.Contains(RegionRegistry.DE);    // true
var isInEU = RegionGroup.EUGroup.Contains(RegionRegistry.US);    // false

// Create a custom group
var apacOffices = new RegionGroup
{
    Name = "APAC Offices",
    Regions = new HashSet<Region> { RegionRegistry.JP, RegionRegistry.KR, RegionRegistry.SG }
};
```

### Region Lookup

```csharp
// Look up a region by code (case-insensitive)
var region = RegionRegistry.GetByCode("FR");    // returns RegionRegistry.FR
var unknown = RegionRegistry.GetByCode("XX");   // returns null
```

---

## Cross-Border Transfer Validation

The `ICrossBorderTransferValidator` evaluates cross-border data transfers following the GDPR Chapter V preference hierarchy:

### 5-Step GDPR Validation Hierarchy

| Step | Rule | GDPR Article | Outcome |
|------|------|--------------|---------|
| 1 | Same region | -- | Always allowed (no cross-border transfer) |
| 2 | Both regions within EEA | Art. 1(3) | Always allowed (free movement) |
| 3 | Destination has adequacy decision | Art. 45 | Allowed without additional safeguards |
| 4 | Appropriate safeguards configured (SCCs, BCRs) | Art. 46 | Allowed with required safeguards noted |
| 5 | No valid mechanism | Art. 44 | Denied |

### TransferValidationResult

Each validation returns a `TransferValidationResult`:

| Property | Type | Description |
|----------|------|-------------|
| `IsAllowed` | `bool` | Whether the transfer is permitted |
| `LegalBasis` | `TransferLegalBasis?` | The legal basis under which the transfer is permitted |
| `RequiredSafeguards` | `IReadOnlyList<string>` | Safeguards required for compliance (e.g., TIA, encryption) |
| `Warnings` | `IReadOnlyList<string>` | Non-blocking warnings about the transfer |
| `DenialReason` | `string?` | Reason for denial when `IsAllowed` is `false` |

### TransferLegalBasis

| Basis | GDPR Article | Description |
|-------|-------------|-------------|
| `AdequacyDecision` | Art. 45 | European Commission adequacy decision |
| `StandardContractualClauses` | Art. 46(2)(c) | Pre-approved contractual terms (SCCs) |
| `BindingCorporateRules` | Art. 47 | Intra-group transfer rules (BCRs) |
| `ExplicitConsent` | Art. 49(1)(a) | Explicit, informed consent of the data subject |
| `PublicInterest` | Art. 49(1)(d) | Important reasons of public interest |
| `LegalClaims` | Art. 49(1)(e) | Establishment, exercise, or defence of legal claims |
| `VitalInterests` | Art. 49(1)(f) | Vital interests of the data subject |
| `Derogation` | Art. 49 | Other Article 49 derogations |

### Usage Examples

```csharp
var validator = serviceProvider.GetRequiredService<ICrossBorderTransferValidator>();

// Intra-EEA transfer — always allowed
var result1 = await validator.ValidateTransferAsync(
    source: RegionRegistry.DE, destination: RegionRegistry.FR,
    dataCategory: "personal-data");

// Transfer to adequacy country — allowed (Art. 45)
var result2 = await validator.ValidateTransferAsync(
    source: RegionRegistry.DE, destination: RegionRegistry.JP,
    dataCategory: "personal-data");

// Transfer to non-adequate country — may require SCCs
var result3 = await validator.ValidateTransferAsync(
    source: RegionRegistry.DE, destination: RegionRegistry.BR,
    dataCategory: "personal-data");
```

### IAdequacyDecisionProvider

The `IAdequacyDecisionProvider` determines whether a region has an EU adequacy decision. The default implementation (`DefaultAdequacyDecisionProvider`) checks the `Region.HasAdequacyDecision` property and merges with any additional regions from `DataResidencyOptions.AdditionalAdequateRegions`.

```csharp
services.AddEncinaDataResidency(options =>
{
    // Add custom regions as adequate (e.g., private cloud zones)
    options.AdditionalAdequateRegions.Add(
        Region.Create("AZURE-WESTEU", "NL", isEU: true, isEEA: true,
            protectionLevel: DataProtectionLevel.High));
});
```

---

## Fluent Policy Builder

The `ResidencyPolicyBuilder` provides a fluent API for defining residency policies within `DataResidencyOptions.AddPolicy`:

```csharp
services.AddEncinaDataResidency(options =>
{
    // EU-only policy with adequacy requirement
    options.AddPolicy("healthcare-data", policy =>
    {
        policy.AllowEU();
        policy.RequireAdequacyDecision();
    });

    // EEA + adequate countries
    options.AddPolicy("marketing-data", policy =>
    {
        policy.AllowEEA();
        policy.AllowAdequate();
    });

    // Specific regions with SCCs allowed
    options.AddPolicy("financial-data", policy =>
    {
        policy.AllowRegions(RegionRegistry.DE, RegionRegistry.FR, RegionRegistry.CH);
        policy.AllowTransferBasis(TransferLegalBasis.StandardContractualClauses);
        policy.AllowTransferBasis(TransferLegalBasis.BindingCorporateRules);
    });

    // Permissive policy — EEA + all adequate + SCCs for others
    options.AddPolicy("general-data", policy =>
    {
        policy.AllowEEA();
        policy.AllowAdequate();
        policy.AllowTransferBasis(
            TransferLegalBasis.StandardContractualClauses,
            TransferLegalBasis.BindingCorporateRules,
            TransferLegalBasis.ExplicitConsent);
    });
});
```

### Builder Methods

| Method | Description |
|--------|-------------|
| `AllowRegions(params Region[])` | Adds specific regions to the allowed list |
| `AllowEU()` | Adds all 27 EU member states from `RegionRegistry.EUMemberStates` |
| `AllowEEA()` | Adds all 30 EEA countries from `RegionRegistry.EEACountries` |
| `AllowAdequate()` | Adds all 15 countries with adequacy decisions from `RegionRegistry.AdequacyCountries` |
| `RequireAdequacyDecision(bool)` | Sets whether the processing region must have an adequacy decision (Art. 45) |
| `AllowTransferBasis(params TransferLegalBasis[])` | Adds acceptable legal bases for cross-border transfers |

Policies configured via the fluent API are persisted to the `IResidencyPolicyStore` at startup. If a policy for the same data category already exists in the store, the fluent policy is skipped.

---

## Audit Trail

Every residency enforcement decision is recorded in an immutable audit trail for compliance evidence:

```csharp
var auditStore = serviceProvider.GetRequiredService<IResidencyAuditStore>();

// Record a manual audit entry
var entry = ResidencyAuditEntry.Create(
    dataCategory: "healthcare-data",
    sourceRegion: "DE",
    action: ResidencyAction.PolicyCheck,
    outcome: ResidencyOutcome.Allowed,
    entityId: "patient-12345",
    requestType: "CreatePatientCommand",
    details: "Processing allowed in region DE for healthcare data");

await auditStore.RecordAsync(entry);

// Retrieve the audit trail for an entity
var trail = await auditStore.GetByEntityAsync("patient-12345");
```

### ResidencyAction Types

| Action | Description |
|--------|-------------|
| `PolicyCheck` | A residency policy was checked for a data processing request |
| `CrossBorderTransfer` | A cross-border data transfer was validated |
| `LocationRecord` | A data location was recorded for tracking purposes |
| `Violation` | A residency policy violation was detected |
| `RegionRouting` | A request was routed to a specific region for processing |

### ResidencyOutcome Types

| Outcome | Description |
|---------|-------------|
| `Allowed` | The action was allowed -- processing or transfer is compliant |
| `Blocked` | The action was blocked -- processing or transfer was denied |
| `Warning` | A warning was issued but the action proceeded (Warn mode) |
| `Skipped` | The check was skipped entirely (Disabled mode or no attributes) |

---

## Configuration Options

```csharp
services.AddEncinaDataResidency(options =>
{
    // Default deployment region (fallback when IRegionContextProvider cannot resolve)
    options.DefaultRegion = RegionRegistry.DE;

    // Pipeline behavior enforcement mode
    options.EnforcementMode = DataResidencyEnforcementMode.Warn;

    // Track data locations via IDataLocationStore (Article 30)
    options.TrackDataLocations = true;

    // Record audit trail entries (Article 5(2) accountability)
    options.TrackAuditTrail = true;

    // Block non-compliant cross-border transfers
    options.BlockNonCompliantTransfers = true;

    // Register health check
    options.AddHealthCheck = true;

    // Auto-scan assemblies for [DataResidency] attributes at startup
    options.AutoRegisterFromAttributes = true;
    options.AssembliesToScan.Add(typeof(Program).Assembly);

    // Add custom adequate regions (e.g., private cloud zones)
    options.AdditionalAdequateRegions.Add(
        Region.Create("AZURE-WESTEU", "NL", isEU: true, isEEA: true,
            protectionLevel: DataProtectionLevel.High));

    // Configure policies via fluent API
    options.AddPolicy("healthcare-data", p =>
        p.AllowEU().RequireAdequacyDecision());
});
```

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `DefaultRegion` | `Region?` | `null` | Fallback region when `IRegionContextProvider` cannot resolve the current region |
| `EnforcementMode` | `DataResidencyEnforcementMode` | `Warn` | Pipeline behavior enforcement mode (Block / Warn / Disabled) |
| `TrackDataLocations` | `bool` | `true` | Record where data is stored via `IDataLocationStore` (Article 30) |
| `TrackAuditTrail` | `bool` | `true` | Record audit trail entries for accountability (Article 5(2)) |
| `BlockNonCompliantTransfers` | `bool` | `true` | Whether the transfer validator denies non-compliant transfers |
| `AddHealthCheck` | `bool` | `false` | Register health check with `IHealthChecksBuilder` |
| `AutoRegisterFromAttributes` | `bool` | `true` | Scan assemblies for `[DataResidency]` attributes at startup |
| `AssembliesToScan` | `List<Assembly>` | `[]` | Assemblies to scan for attribute-based policy discovery |
| `AdditionalAdequateRegions` | `List<Region>` | `[]` | Custom regions to treat as having an EU adequacy decision |

---

## Enforcement Modes

| Mode | Pipeline Behavior | Use Case |
|------|-------------------|----------|
| `Block` | Returns error if the region is not allowed or the policy check fails | Production (GDPR Chapter V compliant) |
| `Warn` | Logs warning, allows request through | Migration/testing phase |
| `Disabled` | Skips all residency checks entirely (no-op, no logging, no metrics) | Development environments |

```csharp
// Production: enforce strictly
options.EnforcementMode = DataResidencyEnforcementMode.Block;

// Testing: observe without blocking
options.EnforcementMode = DataResidencyEnforcementMode.Warn;

// Development: skip entirely
options.EnforcementMode = DataResidencyEnforcementMode.Disabled;
```

---

## Database Providers

The in-memory stores (`InMemoryResidencyPolicyStore`, `InMemoryDataLocationStore`, `InMemoryResidencyAuditStore`) are suitable for development and testing. For production, use a database-backed provider:

| Provider Category | Providers | Registration |
|-------------------|-----------|-------------|
| ADO.NET | SQL Server, PostgreSQL, MySQL | `config.UseDataResidency = true` in `AddEncinaADO()` |
| Dapper | SQL Server, PostgreSQL, MySQL | `config.UseDataResidency = true` in `AddEncinaDapper()` |
| EF Core | SQL Server, PostgreSQL, MySQL | `config.UseDataResidency = true` in `AddEncinaEntityFrameworkCore()` |
| MongoDB | MongoDB | `config.UseDataResidency = true` in `AddEncinaMongoDB()` |

Each provider registers `IResidencyPolicyStore`, `IDataLocationStore`, and `IResidencyAuditStore` backed by the corresponding database.

All 13 database provider implementations are available. The in-memory stores are the default fallback when no database provider is registered.

---

## Observability

### OpenTelemetry Tracing

The module creates activities with the `Encina.Compliance.DataResidency` ActivitySource:

| Activity | Tags |
|----------|------|
| `Residency.Pipeline` | `residency.request_type`, `residency.response_type`, `residency.outcome` |
| `Residency.TransferValidation` | `residency.source_region`, `residency.target_region`, `residency.outcome` |
| `Residency.LocationRecord` | `residency.entity_id`, `residency.target_region`, `residency.outcome` |

### Metrics

| Metric | Type | Description |
|--------|------|-------------|
| `residency.pipeline.executions.total` | Counter | Total pipeline executions (tagged by `residency.outcome`) |
| `residency.policy.checks.total` | Counter | Total policy checks (tagged by `residency.outcome`, `residency.data_category`) |
| `residency.transfers.total` | Counter | Total cross-border transfer validations (tagged by `residency.source_region`, `residency.target_region`) |
| `residency.transfers.blocked.total` | Counter | Total transfers blocked (tagged by `residency.failure_reason`) |
| `residency.locations.recorded.total` | Counter | Total data location records created (tagged by `residency.target_region`) |
| `residency.violations.total` | Counter | Total policy violations detected (tagged by `residency.data_category`) |
| `residency.audit.entries.total` | Counter | Total audit entries recorded (tagged by `residency.action`) |
| `residency.pipeline.duration` | Histogram | Duration of pipeline execution (ms) |
| `residency.transfer.validation.duration` | Histogram | Duration of cross-border transfer validation (ms) |

### Structured Logging

Log events use the `[LoggerMessage]` source generator for zero-allocation structured logging. Event IDs are allocated in the 8600-8674 range:

| EventId Range | Category | Key Events |
|---------------|----------|------------|
| 8600-8609 | Pipeline behavior | Pipeline disabled/skipped, check passed/blocked/warning, region resolution failed, pipeline started/completed/error |
| 8610-8619 | Transfer validation | Transfer allowed/denied, adequacy check, same-region skip, EEA free movement |
| 8620-8629 | Auto-registration | Registration started/completed/skipped, policy discovered/already exists, registration failed |
| 8630-8639 | Health check | Health check completed |
| 8640-8649 | Policy management | Policy created, policy not found |
| 8650-8659 | Location tracking | Location recorded, record failed, record exception |
| 8660-8669 | Audit trail | Audit entry recorded, entry failed |
| 8670-8679 | Region resolution | Resolved from header/tenant/default, resolution exhausted, custom region detected |

---

## Health Check

Opt-in via `options.AddHealthCheck = true`:

```csharp
services.AddEncinaDataResidency(options =>
{
    options.AddHealthCheck = true;
});
```

The health check (`encina-data-residency`) verifies:

- `DataResidencyOptions` are configured
- `IResidencyPolicyStore` is resolvable from DI
- `IDataLocationStore` is resolvable from DI
- `IRegionContextProvider` is resolvable from DI
- `ICrossBorderTransferValidator` is resolvable (Degraded if missing)
- `IResidencyAuditStore` is resolvable when `TrackAuditTrail` is enabled (Degraded if missing)

Tags: `encina`, `gdpr`, `data-residency`, `compliance`, `ready`.

Health check data includes `enforcementMode`, `trackDataLocations`, `trackAuditTrail`, and the concrete type names of all resolved stores.

---

## Error Handling

All operations return `Either<EncinaError, T>`:

```csharp
var policyService = serviceProvider.GetRequiredService<IDataResidencyPolicy>();

var result = await policyService.IsAllowedAsync("healthcare-data", RegionRegistry.US, cancellationToken);

result.Match(
    Right: isAllowed =>
    {
        if (isAllowed)
            logger.LogInformation("Region is allowed for healthcare data");
        else
            logger.LogWarning("Region is NOT allowed for healthcare data");
    },
    Left: error =>
    {
        logger.LogError("Policy check failed: {Code} - {Message}", error.Code, error.Message);
    }
);
```

### Error Codes

| Code | Description |
|------|-------------|
| `residency.region_not_allowed` | The target region is not allowed for the data category (Art. 44 violation) |
| `residency.cross_border_denied` | A cross-border transfer was denied (no adequacy, SCCs, or derogation) |
| `residency.region_not_resolved` | The current region could not be determined from any source |
| `residency.policy_not_found` | No residency policy is defined for the data category |
| `residency.policy_already_exists` | A residency policy already exists for the given data category |
| `residency.store_error` | A store persistence operation failed |
| `residency.transfer_validation_failed` | Transfer validation could not be performed |

---

## Integration with Other Compliance Modules

### With Encina.Compliance.Retention

Data residency and retention work together: residency controls **where** data can be stored, while retention controls **how long** data is kept. Combining both ensures storage limitation compliance (Article 5(1)(e)) with geographic constraints.

```csharp
// Register both compliance modules
services.AddEncinaDataResidency(options =>
{
    options.DefaultRegion = RegionRegistry.DE;
    options.EnforcementMode = DataResidencyEnforcementMode.Block;
});

services.AddEncinaRetention(options =>
{
    options.EnforcementMode = RetentionEnforcementMode.Block;
    options.DefaultRetentionPeriod = TimeSpan.FromDays(365);
});
```

### With Encina.Compliance.Anonymization

Data residency policies may require anonymization before cross-border transfers. Anonymized data that is no longer personal data may be exempt from Chapter V restrictions.

### With Encina.Compliance.DataSubjectRights

When processing data subject access requests (Article 15), the `IDataLocationStore` can identify where the subject's data is stored across regions, enabling complete responses. For erasure requests (Article 17), location records help identify all copies that must be deleted.

---

## Best Practices

1. **Set `DefaultRegion` in production** -- ensure region resolution always succeeds by configuring the deployment region; this prevents enforcement failures when `IRegionContextProvider` cannot resolve the region from the request context
2. **Start with `Warn` mode, switch to `Block` when ready** -- `Warn` mode lets you observe residency enforcement without breaking existing workflows; switch to `Block` once all policies are defined and validated
3. **Define explicit policies for every data category** -- avoid relying on catch-all policies; per Article 44, controllers should establish explicit transfer rules for each category of personal data
4. **Use `AllowEU()` and `AllowEEA()` instead of listing individual countries** -- the builder methods reference the canonical `RegionRegistry` collections, ensuring new EU/EEA members are automatically included
5. **Track the audit trail** -- keep `TrackAuditTrail = true` in production for accountability evidence during regulatory audits (Article 5(2)) and supervisory authority inquiries (Article 58)
6. **Track data locations** -- keep `TrackDataLocations = true` to maintain records of processing activities (Article 30) and enable compliance audits to answer "where is this data stored?"
7. **Configure `AdditionalAdequateRegions` for private cloud** -- if your organization operates private cloud zones that meet EU-equivalent protection standards, register them as adequate regions
8. **Monitor the health check** -- enable `AddHealthCheck = true` and configure alerts for degraded status to catch missing stores early
9. **Use `TimeProvider` for testable time-based logic** -- the pipeline behavior and auto-registration services accept `TimeProvider` for deterministic testing
10. **Implement a custom `IRegionContextProvider`** -- the default provider resolves from the `DefaultRegion` option; in production, implement a provider that resolves from HTTP headers, tenant configuration, or cloud metadata

---

## Testing

### Unit Tests with In-Memory Stores

```csharp
var policyStore = new InMemoryResidencyPolicyStore(
    NullLogger<InMemoryResidencyPolicyStore>.Instance);

// Create a residency policy
var policy = ResidencyPolicyDescriptor.Create(
    dataCategory: "healthcare-data",
    allowedRegions: RegionRegistry.EUMemberStates.ToList(),
    requireAdequacyDecision: true);

await policyStore.CreateAsync(policy);

// Verify it exists
var result = await policyStore.GetByCategoryAsync("healthcare-data");
Assert.True(result.IsRight);
```

### Full Pipeline Test

```csharp
var services = new ServiceCollection();
services.AddEncina(c => c.RegisterServicesFromAssemblyContaining<CreatePatientCommand>());
services.AddEncinaDataResidency(o =>
{
    o.DefaultRegion = RegionRegistry.DE;
    o.EnforcementMode = DataResidencyEnforcementMode.Block;
    o.AddPolicy("healthcare-data", p => p.AllowEU().RequireAdequacyDecision());
});

var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();

var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
var result = await encina.Send(new CreatePatientCommand("patient-001"));

// In Block mode with DefaultRegion = DE (EU), the request should succeed
Assert.True(result.IsRight);
```

### Cross-Border Transfer Validation Test

```csharp
var validator = serviceProvider.GetRequiredService<ICrossBorderTransferValidator>();

// Intra-EEA: always allowed
var intraEEA = await validator.ValidateTransferAsync(
    RegionRegistry.DE, RegionRegistry.NO, "personal-data");
intraEEA.Match(
    Right: r => Assert.True(r.IsAllowed),
    Left: _ => Assert.Fail("Should not fail"));

// To adequacy country: allowed via Art. 45
var toJapan = await validator.ValidateTransferAsync(
    RegionRegistry.DE, RegionRegistry.JP, "personal-data");
toJapan.Match(
    Right: r =>
    {
        Assert.True(r.IsAllowed);
        Assert.Equal(TransferLegalBasis.AdequacyDecision, r.LegalBasis);
    },
    Left: _ => Assert.Fail("Should not fail"));
```

### Data Location Tracking Test

```csharp
var locationStore = serviceProvider.GetRequiredService<IDataLocationStore>();

var location = DataLocation.Create(
    entityId: "customer-42",
    dataCategory: "personal-data",
    region: RegionRegistry.DE,
    storageType: StorageType.Primary);

await locationStore.RecordAsync(location);

var locations = await locationStore.GetByEntityAsync("customer-42");
locations.Match(
    Right: list => Assert.Single(list),
    Left: _ => Assert.Fail("Should not fail"));
```

---

## FAQ

**Q: How does the pipeline behavior decide which requests to validate?**
The `DataResidencyPipelineBehavior` checks for `[DataResidency]` and `[NoCrossBorderTransfer]` attributes on the request type. Attribute presence is cached statically per closed generic type via `static readonly` fields, so there is zero reflection overhead after the first resolution for each request/response pair.

**Q: When is the residency check performed -- before or after the handler?**
Residency validation (Steps 1-5) runs **before** the handler executes. If the region is not allowed in Block mode, the handler never runs. Data location recording (Step 7) and the outcome audit entry (Step 8) run **after** successful handler execution.

**Q: What happens if no `[DataResidency]` or `[NoCrossBorderTransfer]` attribute is present?**
The pipeline behavior skips all residency checks with zero overhead. Attribute presence is resolved once per closed generic type via `static readonly` fields.

**Q: How does the pipeline resolve the entity ID for data location tracking?**
The behavior looks for a public readable property named `EntityId` (preferred) or `Id` (fallback) on the response type, using case-insensitive matching. If neither property exists, data location tracking is silently skipped for that response.

**Q: What is the difference between InMemory and database stores?**
The in-memory stores (`InMemoryResidencyPolicyStore`, `InMemoryDataLocationStore`, `InMemoryResidencyAuditStore`) use `ConcurrentDictionary` for storage. They are suitable for development and testing only. For production, register a database-backed provider that provides durable persistence and survives application restarts.

**Q: Can I register custom implementations before calling `AddEncinaDataResidency`?**
Yes. All service registrations use `TryAdd`, so existing registrations are preserved. Register your custom `IResidencyPolicyStore`, `IRegionContextProvider`, `ICrossBorderTransferValidator`, or `IDataLocationStore` before calling `AddEncinaDataResidency()`.

**Q: How does the `IRegionContextProvider` resolve the current region?**
The default implementation (`DefaultRegionContextProvider`) resolves the region from `DataResidencyOptions.DefaultRegion`. In production, implement a custom provider that resolves from HTTP headers (e.g., `X-Data-Region`), tenant configuration, or cloud provider metadata (e.g., Azure region, AWS region).

**Q: How are residency policies auto-registered from attributes?**
When `AutoRegisterFromAttributes` is `true`, the `DataResidencyAutoRegistrationHostedService` scans the configured assemblies for types decorated with `[DataResidency]`. For each discovered `DataCategory` without an existing policy, a new `ResidencyPolicyDescriptor` is created in the store at startup.

**Q: What is the difference between `AllowedRegionCodes` on the attribute and `AllowedRegions` on the policy?**
The `[DataResidency]` attribute uses `string[]` region codes for simplicity in attribute syntax. The `ResidencyPolicyDescriptor` uses `IReadOnlyList<Region>` for richer metadata. The auto-registration process translates attribute codes into `Region` instances via `RegionRegistry.GetByCode`.

**Q: Can multiple compliance modules operate on the same request pipeline?**
Yes. Each compliance module (`DataResidency`, `Retention`, `Anonymization`, `DataSubjectRights`) registers its own pipeline behavior. They execute in the order registered with the DI container and operate independently.

**Q: How does the transfer validator handle the Schrems II requirements?**
When a transfer is allowed via Standard Contractual Clauses (Step 4), the `TransferValidationResult` includes required safeguards such as "Transfer Impact Assessment (TIA) recommended per Schrems II" and warnings about supplementary measures per EDPB Recommendations 01/2020. The validator does not block these transfers but ensures the requirements are documented.

**Q: What happens to data location records when the data is deleted?**
Call `IDataLocationStore.DeleteByEntityAsync(entityId)` after processing a data subject erasure request (Article 17) to remove location tracking records once the actual data has been deleted. This cleanup is not automatic -- integrate it with your erasure workflow.
