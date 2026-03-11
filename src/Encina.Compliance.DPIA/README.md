# Encina.Compliance.DPIA

[![NuGet](https://img.shields.io/nuget/v/Encina.Compliance.DPIA.svg)](https://www.nuget.org/packages/Encina.Compliance.DPIA/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](../../LICENSE)

GDPR Data Protection Impact Assessment compliance for Encina. Provides risk assessment engine, DPO consultation workflow, pipeline-level DPIA enforcement, expiration monitoring, and immutable audit trail. Implements GDPR Articles 35 and 36.

## Features

- **Risk Assessment Engine** -- `IDPIAAssessmentEngine` evaluates processing activities against configurable risk criteria
- **Six Built-In Risk Criteria** -- `SystematicProfilingCriterion`, `SpecialCategoryDataCriterion`, `SystematicMonitoringCriterion`, `AutomatedDecisionMakingCriterion`, `LargeScaleProcessingCriterion`, `VulnerableSubjectsCriterion`
- **Pluggable Risk Criteria** -- `IRiskCriterion` interface for custom risk evaluation returning `RiskItem`
- **DPO Consultation Workflow** -- `DPOConsultation` with decision tracking (Pending, Approved, Rejected, ConditionallyApproved)
- **Pipeline Enforcement** -- `DPIARequiredPipelineBehavior` auto-validates DPIA before processing `[RequiresDPIA]` requests
- **Three Enforcement Modes** -- `Block` (reject), `Warn` (log and proceed), `Disabled` (no-op)
- **Template System** -- `IDPIATemplateProvider` with pre-configured templates for common processing types
- **Expiration Monitoring** -- `DPIAReviewReminderService` background service detects expired assessments
- **Auto-Registration** -- Scans assemblies for `[RequiresDPIA]` attributes and creates draft assessments at startup
- **Immutable Audit Trail** -- Every assessment operation recorded via `IDPIAAuditStore` per Article 5(2)
- **Domain Notifications** -- `DPIAAssessmentCompleted`, `DPIAAssessmentExpired`, `DPOConsultationRequested`
- **Railway Oriented Programming** -- All operations return `Either<EncinaError, T>`, no exceptions
- **Full Observability** -- OpenTelemetry tracing (5 activity types), 10 counters, 3 histograms, structured log events, health check
- **13 Database Providers** -- ADO.NET, Dapper, EF Core (SQLite, SQL Server, PostgreSQL, MySQL) + MongoDB
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
    options.TrackAuditTrail = true;
    options.AddHealthCheck = true;
    options.AssembliesToScan.Add(typeof(Program).Assembly);
});
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
// Request DPO review when assessment identifies high risk (Art. 36)
var consultation = await engine.RequestDPOConsultationAsync(assessmentId);

consultation.Match(
    Right: dpo => Console.WriteLine(
        $"DPO consultation '{dpo.Id}' requested from {dpo.DPOName}"),
    Left: error => Console.WriteLine($"Failed: {error.Message}"));
```

### 5. Query Assessments

```csharp
var store = serviceProvider.GetRequiredService<IDPIAStore>();

// Get assessment by request type
var assessment = await store.GetAssessmentAsync("CreditScoreQuery");

// Get all expired assessments
var expired = await store.GetExpiredAssessmentsAsync(DateTimeOffset.UtcNow);
```

### 6. Review Audit Trail

```csharp
var auditStore = serviceProvider.GetRequiredService<IDPIAAuditStore>();

var trail = await auditStore.GetAuditTrailAsync(assessmentId);

trail.Match(
    Right: entries => entries.ToList().ForEach(e =>
        Console.WriteLine($"[{e.OccurredAtUtc}] {e.Action} by {e.PerformedBy}")),
    Left: error => Console.WriteLine($"Audit trail error: {error.Message}"));
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
| `TrackAuditTrail` | `bool` | `true` | Record audit entries per Art. 5(2) |
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
// Custom store implementations (e.g., database-backed)
services.AddSingleton<IDPIAStore, DatabaseDPIAStore>();
services.AddSingleton<IDPIAAuditStore, DatabaseDPIAAuditStore>();

// Custom template provider
services.AddSingleton<IDPIATemplateProvider, OrganizationTemplateProvider>();

services.AddEncinaDPIA(options =>
{
    options.EnforcementMode = DPIAEnforcementMode.Block;
});
```

## Database Providers

The core package ships with `InMemoryDPIAStore` and `InMemoryDPIAAuditStore` for development and testing. Database-backed implementations for the 13 providers are available via satellite packages:

```csharp
// ADO.NET (SQLite example)
services.AddEncinaADO(config =>
{
    config.UseDPIA = true;
});

// Dapper (SQL Server example)
services.AddEncinaDapper(config =>
{
    config.UseDPIA = true;
});

// EF Core (PostgreSQL example)
services.AddEncinaEntityFrameworkCore<AppDbContext>(config =>
{
    config.UseDPIA = true;
});
```

## Observability

- **Tracing**: `Encina.Compliance.DPIA` ActivitySource with 5 activity types (`DPIA.PipelineCheck`, `DPIA.Assessment`, `DPIA.DPOConsultation`, `DPIA.ReviewReminderCycle`, `DPIA.Endpoint`)
- **Metrics**: 10 counters (`dpia.pipeline.checks.total`, `dpia.pipeline.checks.passed`, `dpia.pipeline.checks.failed`, `dpia.pipeline.checks.skipped`, `dpia.auto_registration.total`, `dpia.review_reminder.cycles.total`, `dpia.review_reminder.expired.total`, `dpia.assessment.total`, `dpia.dpo_consultation.total`, `dpia.endpoint.requests.total`) and 3 histograms (`dpia.pipeline.check.duration`, `dpia.assessment.duration`, `dpia.endpoint.duration`)
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
- `IDPIAStore` is resolvable
- `IDPIAAuditStore` is resolvable (when `TrackAuditTrail` is enabled)
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
| **5(2)** | Accountability principle | `IDPIAAuditStore` immutable audit trail |

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.
