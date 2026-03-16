# Encina.Compliance.DPIA

[![NuGet](https://img.shields.io/nuget/v/Encina.Compliance.DPIA.svg)](https://www.nuget.org/packages/Encina.Compliance.DPIA/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](../../LICENSE)

GDPR Data Protection Impact Assessment compliance for Encina. Uses **Marten event sourcing** for immutable audit trail and full assessment lifecycle management. Provides risk assessment engine, DPO consultation workflow, pipeline-level DPIA enforcement, and expiration monitoring. Implements GDPR Articles 35 and 36.

## Features

- **Event-Sourced Aggregate** -- `DPIAAggregate` with full lifecycle: Create → Evaluate → DPO Consult → Approve/Reject/Revise → Expire
- **CQRS Read Model** -- `DPIAReadModel` projected via `DPIAProjection` for efficient queries
- **Unified Service Interface** -- `IDPIAService` with 8 write operations and 5 query operations
- **Risk Assessment Engine** -- `IDPIAAssessmentEngine` evaluates processing activities against configurable risk criteria
- **Six Built-In Risk Criteria** -- `SystematicProfilingCriterion`, `SpecialCategoryDataCriterion`, `SystematicMonitoringCriterion`, `AutomatedDecisionMakingCriterion`, `LargeScaleProcessingCriterion`, `VulnerableSubjectsCriterion`
- **Pluggable Risk Criteria** -- `IRiskCriterion` interface for custom risk evaluation returning `RiskItem`
- **DPO Consultation Workflow** -- `DPOConsultation` with decision tracking (Pending, Approved, Rejected, ConditionallyApproved)
- **Pipeline Enforcement** -- `DPIARequiredPipelineBehavior` auto-validates DPIA before processing `[RequiresDPIA]` requests
- **Three Enforcement Modes** -- `Block` (reject), `Warn` (log and proceed), `Disabled` (no-op)
- **Template System** -- `IDPIATemplateProvider` with pre-configured templates for common processing types
- **Expiration Monitoring** -- `DPIAReviewReminderService` background service detects expired assessments
- **Auto-Registration** -- Scans assemblies for `[RequiresDPIA]` attributes and creates draft assessments at startup
- **Immutable Audit Trail** -- Event stream provides complete audit trail per Article 5(2) accountability
- **Domain Notifications** -- `DPIAAssessmentCompleted`, `DPIAAssessmentExpired`, `DPOConsultationRequested`
- **Railway Oriented Programming** -- All operations return `Either<EncinaError, T>`, no exceptions
- **Full Observability** -- OpenTelemetry tracing (5 activity types), 17 counters, 3 histograms, structured log events, health check
- **Marten Event Sourcing** -- PostgreSQL-backed event store with inline projections
- **.NET 10 Compatible** -- Built with latest C# features

## Installation

```bash
dotnet add package Encina.Compliance.DPIA
```

## Quick Start

### 1. Register Services

```csharp
services.AddEncina(config =>
    config.RegisterServicesFromAssemblyContaining<Program>());

services.AddEncinaDPIA(options =>
{
    options.EnforcementMode = DPIAEnforcementMode.Block;
    options.DefaultReviewPeriod = TimeSpan.FromDays(365);
    options.DPOName = "Jane Smith";
    options.DPOEmail = "dpo@company.eu";
    options.AddHealthCheck = true;
    options.AssembliesToScan.Add(typeof(Program).Assembly);
});

// Register Marten aggregate + projection
services.AddDPIAAggregates();
```

### 2. Mark Requests Requiring DPIA

```csharp
[RequiresDPIA(ProcessingType = "Profiling", Reason = "Automated credit scoring")]
public sealed record CreditScoreQuery(string UserId)
    : IRequest<Either<EncinaError, CreditScore>>;
```

### 3. Run a Risk Assessment

```csharp
var engine = serviceProvider.GetRequiredService<IDPIAAssessmentEngine>();

var context = new DPIAContext
{
    RequestType = typeof(CreditScoreQuery),
    ProcessingType = "Profiling",
    DataCategories = ["Financial", "Behavioral"],
    HighRiskTriggers = [HighRiskTriggers.SystematicProfiling, HighRiskTriggers.AutomatedDecisionMaking]
};

var result = await engine.AssessAsync(context);

result.Match(
    Right: dpia => Console.WriteLine(
        $"Risk: {dpia.OverallRisk}, Risks: {dpia.IdentifiedRisks.Count}, " +
        $"Prior consultation: {dpia.RequiresPriorConsultation}"),
    Left: error => Console.WriteLine($"Assessment failed: {error.Message}"));
```

### 4. Request DPO Consultation

```csharp
var service = serviceProvider.GetRequiredService<IDPIAService>();

// Request DPO review when assessment identifies high risk (Art. 36)
var consultation = await service.RequestDPOConsultationAsync(assessmentId);

consultation.Match(
    Right: consultationId => Console.WriteLine(
        $"DPO consultation '{consultationId}' requested"),
    Left: error => Console.WriteLine($"Failed: {error.Message}"));
```

### 5. Query Assessments

```csharp
var service = serviceProvider.GetRequiredService<IDPIAService>();

// Get assessment by request type
var assessment = await service.GetAssessmentByRequestTypeAsync("CreditScoreQuery");

// Get all expired assessments
var expired = await service.GetExpiredAssessmentsAsync();
```

### 6. Review Event History (Audit Trail)

```csharp
var service = serviceProvider.GetRequiredService<IDPIAService>();

// Event stream provides the full audit trail (Art. 5(2) accountability)
var history = await service.GetAssessmentHistoryAsync(assessmentId);

history.Match(
    Right: events => events.ToList().ForEach(e =>
        Console.WriteLine($"Event: {e.GetType().Name}")),
    Left: error => Console.WriteLine($"History error: {error.Message}"));
```

## Custom Risk Criteria

```csharp
public sealed class CrossBorderTransferCriterion : IRiskCriterion
{
    public string Name => "CrossBorderTransfer";

    public ValueTask<RiskItem?> EvaluateAsync(
        DPIAContext context,
        CancellationToken cancellationToken = default)
    {
        if (context.HighRiskTriggers.Contains(HighRiskTriggers.CrossBorderTransfer))
        {
            return ValueTask.FromResult<RiskItem?>(new RiskItem(
                "CrossBorderTransfer",
                RiskLevel.High,
                "Data transferred outside EEA without adequate safeguards",
                "Implement Standard Contractual Clauses (SCCs)"));
        }

        return ValueTask.FromResult<RiskItem?>(null);
    }
}
```

## Enforcement Modes

| Mode | Behavior | Use Case |
|------|----------|----------|
| `Block` | Returns error when no valid DPIA exists | Production (recommended) |
| `Warn` | Logs warning, allows request through | Migration/testing phase (default) |
| `Disabled` | Skips all DPIA checks entirely | Development environments |

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `EnforcementMode` | `DPIAEnforcementMode` | `Warn` | Pipeline behavior enforcement mode |
| `DefaultReviewPeriod` | `TimeSpan` | `365 days` | Default period before assessment requires review |
| `DPOName` | `string?` | `null` | Data Protection Officer name |
| `DPOEmail` | `string?` | `null` | Data Protection Officer email |
| `PublishNotifications` | `bool` | `true` | Publish domain notifications |
| `EnableExpirationMonitoring` | `bool` | `false` | Enable background expiration monitoring service |
| `ExpirationCheckInterval` | `TimeSpan` | `1 hour` | Interval between expiration monitoring checks |
| `AutoRegisterFromAttributes` | `bool` | `true` | Auto-register draft assessments from `[RequiresDPIA]` at startup |
| `AutoDetectHighRisk` | `bool` | `true` | Auto-detect high-risk processing patterns |
| `BlockWithoutDPIA` | `bool` | `false` | Block requests without any DPIA assessment |
| `AddHealthCheck` | `bool` | `false` | Register health check |

## Error Codes

| Code | Meaning |
|------|---------|
| `dpia.assessment_required` | No valid DPIA assessment exists for the request |
| `dpia.assessment_expired` | DPIA assessment has passed its review date |
| `dpia.assessment_rejected` | DPIA assessment was rejected |
| `dpia.risk_too_high` | Risk level exceeds acceptable threshold |
| `dpia.dpo_consultation_required` | DPO consultation required before proceeding |
| `dpia.prior_consultation_required` | Prior consultation with supervisory authority required (Art. 36) |
| `dpia.store_error` | Persistence store operation failure |
| `dpia.template_not_found` | No template found for the specified processing type |

## Custom Implementations

Register custom implementations before `AddEncinaDPIA()` to override defaults (TryAdd semantics):

```csharp
// Custom template provider
services.AddSingleton<IDPIATemplateProvider, OrganizationTemplateProvider>();

services.AddEncinaDPIA(options =>
{
    options.EnforcementMode = DPIAEnforcementMode.Block;
});

// Register Marten aggregate + projection (required)
services.AddDPIAAggregates();
```

## Persistence

This package uses **Marten event sourcing** (PostgreSQL) for persistence. The event-sourced aggregate provides:

- Immutable event stream as audit trail (Art. 5(2))
- Full assessment history via `GetAssessmentHistoryAsync()`
- CQRS read model (`DPIAReadModel`) projected inline from events
- Cache-aside pattern via `ICacheProvider` for read performance

## Observability

- **Tracing**: `Encina.Compliance.DPIA` ActivitySource with 5 activity types (`DPIA.PipelineCheck`, `DPIA.Assessment`, `DPIA.DPOConsultation`, `DPIA.ReviewReminderCycle`, `DPIA.Endpoint`)
- **Metrics**: 17 counters (10 pipeline/engine + 7 service-level: `dpia.service.assessments.created`, `.evaluated`, `.approved`, `.rejected`, `.revision_requested`, `.expired`, `dpia.service.errors.total`) and 3 histograms (`dpia.pipeline.check.duration`, `dpia.assessment.duration`, `dpia.endpoint.duration`)
- **Logging**: Structured log events via `[LoggerMessage]` source generator (zero-allocation)
- **Health Check**: Verifies DI configuration and store connectivity

## Health Check

Enable via `DPIAOptions.AddHealthCheck`:

```csharp
services.AddEncinaDPIA(options =>
{
    options.AddHealthCheck = true;
});
```

The health check (`encina-dpia`) verifies:
- `DPIAOptions` are configured
- `IDPIAService` is resolvable
- `IDPIAAssessmentEngine` is resolvable

Tags: `encina`, `gdpr`, `dpia`, `compliance`, `ready`

## Testing

```csharp
// Use in-memory stores for unit testing (registered by default)
services.AddEncinaDPIA(options =>
{
    options.EnforcementMode = DPIAEnforcementMode.Block;
    options.EnableExpirationMonitoring = false; // Disable background service in tests
    options.DefaultReviewPeriod = TimeSpan.FromDays(365);
});
```

## Related Packages

| Package | Description |
|---------|-------------|
| `Encina` | Core CQRS pipeline with `IPipelineBehavior` |
| `Encina.Compliance.BreachNotification` | GDPR Articles 33-34 breach notification |
| `Encina.Compliance.Retention` | GDPR Article 5(1)(e) data retention management |
| `Encina.Compliance.DataSubjectRights` | GDPR Articles 15-22 data subject rights |
| `Encina.Compliance.GDPR` | GDPR processing activity tracking and RoPA |
| `Encina.Compliance.Consent` | GDPR Article 7 consent management |
| `Encina.Compliance.LawfulBasis` | GDPR Article 6 lawful basis tracking |

## GDPR Compliance

This package implements key GDPR requirements:

| Article | Requirement | Implementation |
|---------|-------------|----------------|
| **35(1)** | Mandatory DPIA for high-risk processing | `IDPIAAssessmentEngine`, `DPIARequiredPipelineBehavior` |
| **35(3)(a)** | Systematic profiling with legal effects | `SystematicProfilingCriterion` |
| **35(3)(b)** | Special category data at scale | `SpecialCategoryDataCriterion` |
| **35(3)(c)** | Systematic monitoring of public areas | `SystematicMonitoringCriterion` |
| **35(7)** | Required assessment content | `DPIATemplate` with sections and questions |
| **35(9)** | Seek views of data subjects | `DPIAContext` metadata |
| **36(1)** | Prior consultation with supervisory authority | `DPOConsultation` workflow, `RequiresPriorConsultation` flag |
| **5(2)** | Accountability principle | Event stream provides immutable audit trail |

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.
