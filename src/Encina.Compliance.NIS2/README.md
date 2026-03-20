# Encina.Compliance.NIS2

[![NuGet](https://img.shields.io/nuget/v/Encina.Compliance.NIS2.svg)](https://www.nuget.org/packages/Encina.Compliance.NIS2/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](../../LICENSE)

NIS2 Directive (EU 2022/2555) compliance for Encina. Provides a stateless rule engine that evaluates the 10 mandatory cybersecurity risk-management measures (Article 21(2)), enforces multi-factor authentication, validates encryption policies, assesses supply chain security, tracks incident notification timelines (24h/72h/1mo), and records management accountability (Article 20). All checks run at the CQRS pipeline level, ensuring consistent enforcement across HTTP, messaging, gRPC, and serverless transports.

## Features

- **10 Mandatory Measures (Art. 21(2))** -- Pluggable `INIS2MeasureEvaluator` for each measure: risk analysis, incident handling, business continuity, supply chain, network security, effectiveness assessment, cyber hygiene, cryptography, HR security, and MFA
- **Pipeline-Level Enforcement** -- `NIS2CompliancePipelineBehavior` evaluates `[NIS2Critical]`, `[RequireMFA]`, and `[NIS2SupplyChainCheck]` attributes before request execution
- **MFA Enforcement (Art. 21(2)(j))** -- `[RequireMFA]` attribute with pluggable `IMFAEnforcer` for identity provider integration
- **Encryption Validation (Art. 21(2)(h))** -- `IEncryptionValidator` verifies data-at-rest and in-transit encryption for declared categories and endpoints
- **Supply Chain Security (Art. 21(2)(d))** -- `[NIS2SupplyChainCheck("supplier-id")]` with configurable risk thresholds and supplier registry
- **Incident Notification Timeline (Art. 23(4))** -- Four-phase process: 24h early warning, 72h notification, intermediate reports, 1-month final report
- **Management Accountability (Art. 20)** -- `ManagementAccountabilityRecord` tracks management body approval and cybersecurity training compliance
- **Three Enforcement Modes** -- `Block` (reject), `Warn` (log and proceed), `Disabled` (no-op)
- **Entity Classification** -- `Essential` vs `Important` entity types with 18 sectors across Annexes I and II
- **Railway Oriented Programming** -- All operations return `Either<EncinaError, T>`, no exceptions
- **Full Observability** -- OpenTelemetry tracing (5 activity types), 9 counters, 4 histograms, structured log events, health check
- **.NET 10 Compatible** -- Built with latest C# features

## Installation

```bash
dotnet add package Encina.Compliance.NIS2
```

## Quick Start

### 1. Register Services

```csharp
services.AddEncina(config =>
    config.RegisterServicesFromAssemblyContaining<Program>());

services.AddEncinaNIS2(options =>
{
    options.EntityType = NIS2EntityType.Essential;
    options.Sector = NIS2Sector.DigitalInfrastructure;
    options.EnforcementMode = NIS2EnforcementMode.Block;
    options.EnforceMFA = true;
    options.EnforceEncryption = true;
    options.CompetentAuthority = "bsi@bsi.bund.de";
    options.AddHealthCheck = true;

    // Declare encryption coverage (Art. 21(2)(h))
    options.EncryptedDataCategories.Add("PII");
    options.EncryptedEndpoints.Add("https://api.example.com");

    // Declare organizational measures
    options.HasRiskAnalysisPolicy = true;
    options.HasIncidentHandlingProcedures = true;
    options.HasBusinessContinuityPlan = true;
    options.HasNetworkSecurityPolicy = true;
    options.HasEffectivenessAssessment = true;
    options.HasCyberHygieneProgram = true;
    options.HasHumanResourcesSecurity = true;

    // Register suppliers (Art. 21(2)(d))
    options.AddSupplier("payment-provider", supplier =>
    {
        supplier.Name = "PayCorp";
        supplier.RiskLevel = SupplierRiskLevel.High;
        supplier.LastAssessmentAtUtc = DateTimeOffset.UtcNow.AddMonths(-3);
        supplier.CertificationStatus = "ISO 27001";
    });

    // Management accountability (Art. 20)
    options.ManagementAccountability = new ManagementAccountabilityRecord
    {
        ApprovedByManagement = true,
        ApprovalDateUtc = new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero),
        TrainingCompletedByManagement = true,
        TrainingDateUtc = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero)
    };
});
```

### 2. Mark Critical Operations

```csharp
[NIS2Critical(Description = "Core payment processing — critical infrastructure")]
public sealed record ProcessPaymentCommand(decimal Amount)
    : ICommand<PaymentResult>;
```

### 3. Require MFA for Sensitive Operations

```csharp
[RequireMFA(Reason = "Administrative operation requiring elevated authentication")]
public sealed record DeleteUserCommand(string UserId)
    : ICommand<Unit>;
```

### 4. Validate Supply Chain Security

```csharp
[NIS2SupplyChainCheck("payment-provider")]
public sealed record ProcessExternalPaymentCommand(decimal Amount)
    : ICommand<Unit>;

// Multiple suppliers with custom risk thresholds
[NIS2SupplyChainCheck("cloud-provider")]
[NIS2SupplyChainCheck("data-processor", MinimumRiskLevel = SupplierRiskLevel.Medium)]
public sealed record MigrateDataCommand(string DataSetId)
    : ICommand<Unit>;
```

### 5. Report Incidents (Art. 23)

```csharp
var handler = serviceProvider.GetRequiredService<INIS2IncidentHandler>();

// Report a significant incident
var result = await handler.ReportIncidentAsync(incident);

result.Match(
    Right: reported => Console.WriteLine(
        $"Incident '{reported.Id}' reported. Phase: {reported.CurrentPhase}"),
    Left: error => Console.WriteLine($"Report failed: {error.Message}"));

// Advance to next notification phase
var advanceResult = await handler.AdvancePhaseAsync(incidentId);
```

### 6. Validate Overall Compliance

```csharp
var validator = serviceProvider.GetRequiredService<INIS2ComplianceValidator>();

var result = await validator.ValidateAsync();

result.Match(
    Right: compliance => Console.WriteLine(
        $"Compliance: {compliance.CompliancePercentage}% — "
        + $"{compliance.MissingCount} measure(s) missing"),
    Left: error => Console.WriteLine($"Validation failed: {error.Message}"));
```

## Attributes

| Attribute | Article | Purpose |
|-----------|---------|---------|
| `[NIS2Critical]` | Art. 21 | Marks a request as a critical infrastructure operation; enables enhanced observability |
| `[RequireMFA]` | Art. 21(2)(j) | Requires MFA before request execution; invokes `IMFAEnforcer` |
| `[NIS2SupplyChainCheck("id")]` | Art. 21(2)(d) | Validates supplier risk level before request execution; supports `AllowMultiple` |

## Enforcement Modes

| Mode | Behavior | Use Case |
|------|----------|----------|
| `Block` | Returns error when a compliance check fails | Production (recommended) |
| `Warn` | Logs warning, allows response through | Migration/testing phase (default) |
| `Disabled` | Skips all NIS2 compliance checks entirely | Development environments |

## The 10 Mandatory Measures (Art. 21(2))

| Measure | Article | Evaluator | NIS2Options Flag |
|---------|---------|-----------|------------------|
| Risk analysis and security policies | (a) | `RiskAnalysisEvaluator` | `HasRiskAnalysisPolicy` |
| Incident handling | (b) | `IncidentHandlingEvaluator` | `HasIncidentHandlingProcedures` |
| Business continuity | (c) | `BusinessContinuityEvaluator` | `HasBusinessContinuityPlan` |
| Supply chain security | (d) | `SupplyChainSecurityEvaluator` | `Suppliers` registry |
| Network and system security | (e) | `NetworkSecurityEvaluator` | `HasNetworkSecurityPolicy` |
| Effectiveness assessment | (f) | `EffectivenessAssessmentEvaluator` | `HasEffectivenessAssessment` |
| Cyber hygiene and training | (g) | `CyberHygieneEvaluator` | `HasCyberHygieneProgram` |
| Cryptography and encryption | (h) | `CryptographyEvaluator` | `EnforceEncryption` |
| HR security and access control | (i) | `HumanResourcesSecurityEvaluator` | `HasHumanResourcesSecurity` |
| Multi-factor authentication | (j) | `MultiFactorAuthenticationEvaluator` | `EnforceMFA` |

## Incident Notification Timeline (Art. 23(4))

| Phase | Deadline | Description |
|-------|----------|-------------|
| `EarlyWarning` | 24 hours | Preliminary alert to CSIRT/competent authority (Art. 23(4)(a)) |
| `IncidentNotification` | 72 hours | Initial assessment with severity, impact, and indicators (Art. 23(4)(b)) |
| `IntermediateReport` | On request | Status updates upon CSIRT/authority request (Art. 23(4)(c)) |
| `FinalReport` | 1 month | Detailed description, root cause, mitigation measures (Art. 23(4)(d)) |

## Cross-Cutting Integrations

All integrations are opt-in — they activate only when the corresponding service is registered in DI.

### Resilience

All external service calls are protected by a two-tier resilience strategy:

1. **Polly v8 pipeline** — Register a `ResiliencePipeline` with key `"nis2-external"` for retry, circuit breaker, and timeout
2. **Timeout fallback** — When no Polly pipeline is registered, calls use `NIS2Options.ExternalCallTimeout` (default: 5s)

Any exception in external calls returns a safe fallback — compliance evaluation never fails due to infrastructure outages.

```csharp
// Optional: register a Polly pipeline for NIS2 external calls
services.AddResiliencePipeline("nis2-external", builder =>
{
    builder.AddRetry(new() { MaxRetryAttempts = 2 })
           .AddCircuitBreaker(new())
           .AddTimeout(TimeSpan.FromSeconds(3));
});
```

### Caching & Multi-Tenancy

When `ICacheProvider` is registered, compliance results are cached for `NIS2Options.ComplianceCacheTTL` (default: 5 min, set to `TimeSpan.Zero` to disable). When `IRequestContext` provides a `TenantId`, cache keys are scoped per-tenant (`nis2:compliance:{tenantId}`).

### Breach Notification Forwarding

When `IBreachNotificationService` is registered, significant NIS2 incidents are automatically forwarded with severity mapping (Critical→Critical, High→High, Medium→Medium, Low→Low). Forwarding failures do not block incident reporting.

### Encryption Infrastructure Verification

When `IKeyProvider` is registered, `ValidateEncryptionPolicyAsync` verifies the encryption infrastructure has an active key — catching misconfigured key vaults beyond configuration-only checks.

### GDPR Alignment

When `IProcessingActivityRegistry` is registered, the `RiskAnalysisEvaluator` enriches Art. 21(2)(a) evaluation with DPIA context (Art. 35 processing activity count).

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ExternalCallTimeout` | `TimeSpan` | `5s` | Timeout for external service calls when no Polly pipeline is registered |
| `ComplianceCacheTTL` | `TimeSpan` | `5min` | Cache TTL for compliance results (`TimeSpan.Zero` disables caching) |
| `EntityType` | `NIS2EntityType` | `Essential` | Entity classification (Essential or Important) per Art. 3 |
| `Sector` | `NIS2Sector` | — | Sector per Annexes I/II (18 sectors) |
| `EnforcementMode` | `NIS2EnforcementMode` | `Warn` | Pipeline behavior enforcement mode |
| `EnforceMFA` | `bool` | `true` | Enable MFA enforcement for `[RequireMFA]` requests (Art. 21(2)(j)) |
| `EnforceEncryption` | `bool` | `true` | Enable encryption validation (Art. 21(2)(h)) |
| `IncidentNotificationHours` | `int` | `24` | Early warning deadline in hours (Art. 23(4)(a)) |
| `CompetentAuthority` | `string?` | `null` | CSIRT/authority contact (Art. 23(1)) |
| `ManagementAccountability` | `ManagementAccountabilityRecord?` | `null` | Management body approval and training (Art. 20) |
| `PublishNotifications` | `bool` | `true` | Publish domain notifications for compliance events |
| `AddHealthCheck` | `bool` | `false` | Register NIS2 compliance health check |
| `HasRiskAnalysisPolicy` | `bool` | `false` | Risk analysis policy in place (Art. 21(2)(a)) |
| `HasIncidentHandlingProcedures` | `bool` | `false` | Incident handling procedures in place (Art. 21(2)(b)) |
| `HasBusinessContinuityPlan` | `bool` | `false` | Business continuity plans in place (Art. 21(2)(c)) |
| `HasNetworkSecurityPolicy` | `bool` | `false` | Network/system security policies in place (Art. 21(2)(e)) |
| `HasEffectivenessAssessment` | `bool` | `false` | Effectiveness assessment procedures in place (Art. 21(2)(f)) |
| `HasCyberHygieneProgram` | `bool` | `false` | Cyber hygiene and training programs in place (Art. 21(2)(g)) |
| `HasHumanResourcesSecurity` | `bool` | `false` | HR security and access control policies in place (Art. 21(2)(i)) |
| `EncryptedDataCategories` | `HashSet<string>` | empty | Data categories confirmed encrypted at rest (Art. 21(2)(h)) |
| `EncryptedEndpoints` | `HashSet<string>` | empty | Endpoints confirmed to use encryption in transit (Art. 21(2)(h)) |

## Error Codes

| Code | Meaning |
|------|---------|
| `nis2.compliance_check_failed` | Overall compliance validation failed |
| `nis2.measure_not_satisfied` | Specific Art. 21(2) measure not satisfied |
| `nis2.measure_evaluation_failed` | Measure evaluator execution failure |
| `nis2.mfa_required` | MFA not enabled for `[RequireMFA]` request |
| `nis2.encryption_required` | Encryption requirements not met |
| `nis2.supplier_risk_high` | Supplier risk level exceeds threshold |
| `nis2.supply_chain_check_failed` | Supply chain validation failure |
| `nis2.supplier_not_found` | Supplier not in configured registry |
| `nis2.deadline_exceeded` | Notification deadline exceeded |
| `nis2.incident_report_failed` | Incident reporting failure |
| `nis2.all_phases_complete` | All notification phases already complete |
| `nis2.management_accountability_missing` | Management accountability not configured |
| `nis2.pipeline_blocked` | Request blocked by NIS2 enforcement |

## Custom Implementations

Register custom implementations before `AddEncinaNIS2()` to override defaults (TryAdd semantics):

```csharp
// Custom MFA enforcer (e.g., Azure AD MFA integration)
services.AddSingleton<IMFAEnforcer, AzureAdMFAEnforcer>();

// Custom encryption validator (e.g., checks actual infrastructure)
services.AddSingleton<IEncryptionValidator, InfrastructureEncryptionValidator>();

// Custom supply chain validator (e.g., integrates with GRC platform)
services.AddSingleton<ISupplyChainSecurityValidator, GrcSupplyChainValidator>();

// Custom incident handler (e.g., SIEM/SOAR integration)
services.AddScoped<INIS2IncidentHandler, SiemIncidentHandler>();

services.AddEncinaNIS2(options =>
{
    options.EntityType = NIS2EntityType.Essential;
    options.Sector = NIS2Sector.Energy;
    options.EnforcementMode = NIS2EnforcementMode.Block;
});
```

## Observability

- **Tracing**: `Encina.Compliance.NIS2` ActivitySource with 5 activity types (`NIS2.Pipeline`, `NIS2.ComplianceCheck`, `NIS2.MeasureEvaluation`, `NIS2.IncidentReport`, `NIS2.SupplyChainAssessment`)
- **Metrics**: 9 counters (`nis2.pipeline.executions.total`, `nis2.compliance.checks.total`, `nis2.measure.evaluations.total`, `nis2.mfa.checks.total`, `nis2.supply_chain.checks.total`, `nis2.encryption.checks.total`, `nis2.incident.reports.total`, `nis2.incident.deadline_checks.total`, `nis2.supply_chain.assessments.total`) and 4 histograms (`nis2.pipeline.duration.ms`, `nis2.compliance.check.duration.ms`, `nis2.measure.evaluation.duration.ms`, `nis2.supply_chain.assessment.duration.ms`)
- **Logging**: Structured log events via `[LoggerMessage]` source generator (zero-allocation, EventIds 9200-9209)
- **Health Check**: Evaluates all 10 mandatory measures — Healthy (all satisfied), Degraded (gaps exist), Unhealthy (validation failed)

## Health Check

Enable via `NIS2Options.AddHealthCheck`:

```csharp
services.AddEncinaNIS2(options =>
{
    options.AddHealthCheck = true;
});
```

The health check (`encina-nis2-compliance`) resolves `INIS2ComplianceValidator` and runs a full compliance validation:

- **Healthy** — All 10 NIS2 mandatory measures are satisfied
- **Degraded** — Some measures are satisfied but gaps exist (reports compliance percentage and missing measures)
- **Unhealthy** — Compliance validation failed or threw an exception

Tags: `encina`, `nis2`, `compliance`, `security`, `ready`

## Entity Classification

| Type | Supervision | Maximum Fines (Art. 34) | Sectors |
|------|-------------|------------------------|---------|
| **Essential** | Ex-ante (proactive) | EUR 10M or 2% worldwide turnover | Annex I (11 sectors) |
| **Important** | Ex-post (reactive) | EUR 7M or 1.4% worldwide turnover | Annex II (7 sectors) |

## Testing

```csharp
// Use default implementations for unit testing
services.AddEncinaNIS2(options =>
{
    options.EntityType = NIS2EntityType.Essential;
    options.Sector = NIS2Sector.DigitalInfrastructure;
    options.EnforcementMode = NIS2EnforcementMode.Block;
    options.HasRiskAnalysisPolicy = true;
    options.HasIncidentHandlingProcedures = true;
    // ... set all measure flags for a fully compliant test scenario
});
```

## Related Packages

| Package | Description |
|---------|-------------|
| `Encina` | Core CQRS pipeline with `IPipelineBehavior` |
| `Encina.Security` | Core security abstractions and authorization pipeline |
| `Encina.Security.Audit` | Immutable audit trail for compliance |
| `Encina.Compliance.BreachNotification` | GDPR Articles 33-34 breach notification |
| `Encina.Compliance.GDPR` | GDPR processing activity tracking and RoPA |
| `Encina.Compliance.Consent` | GDPR Article 7 consent management |

## NIS2 Compliance

This package implements key NIS2 Directive requirements:

| Article | Requirement | Implementation |
|---------|-------------|----------------|
| **20(1)** | Management body approval of cybersecurity measures | `ManagementAccountabilityRecord.ApprovedByManagement` |
| **20(2)** | Management body cybersecurity training | `ManagementAccountabilityRecord.TrainingCompletedByManagement` |
| **21(1)** | Appropriate and proportionate risk-management measures | `INIS2ComplianceValidator.ValidateAsync()`, `NIS2ComplianceResult` |
| **21(2)(a-j)** | 10 mandatory cybersecurity measures | 10 `INIS2MeasureEvaluator` implementations |
| **21(2)(d)** | Supply chain security | `[NIS2SupplyChainCheck]`, `ISupplyChainSecurityValidator` |
| **21(2)(h)** | Cryptography and encryption policies | `IEncryptionValidator`, `EncryptedDataCategories`, `EncryptedEndpoints` |
| **21(2)(j)** | Multi-factor authentication | `[RequireMFA]`, `IMFAEnforcer` |
| **23(1)** | Notify CSIRT/competent authority | `INIS2IncidentHandler`, `CompetentAuthority` option |
| **23(4)(a)** | 24h early warning | `NIS2NotificationPhase.EarlyWarning`, `IncidentNotificationHours` |
| **23(4)(b)** | 72h incident notification | `NIS2NotificationPhase.IncidentNotification` |
| **23(4)(c)** | Intermediate reports on request | `NIS2NotificationPhase.IntermediateReport` |
| **23(4)(d)** | 1-month final report | `NIS2NotificationPhase.FinalReport` |
| **34** | Administrative fines | `NIS2EntityType` (Essential: EUR 10M/2%, Important: EUR 7M/1.4%) |

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.
