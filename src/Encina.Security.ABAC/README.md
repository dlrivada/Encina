# Encina.Security.ABAC

[![NuGet](https://img.shields.io/nuget/v/Encina.Security.ABAC.svg)](https://www.nuget.org/packages/Encina.Security.ABAC/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](../../LICENSE)

XACML 3.0-based Attribute-Based Access Control (ABAC) engine for Encina. Provides fine-grained, context-aware authorization using attributes of the subject, resource, action, and environment — without XML.

## Features

- **XACML 3.0 Semantics** - Full policy evaluation model (PolicySet/Policy/Rule hierarchy, four-effect model, 8 combining algorithms)
- **No XML** - C#-idiomatic sealed records and fluent builders replace XACML XML
- **70+ Standard Functions** - Equality, comparison, arithmetic, string, logical, bag, set, higher-order, type conversion, regex
- **EEL (Encina Expression Language)** - Write policy conditions as inline C# boolean expressions compiled by Roslyn
- **Obligations & Advice** - Mandatory post-decision actions (XACML 3.0 section 7.18) with pluggable handlers
- **Full Observability** - OpenTelemetry tracing, 9 counters, 2 histograms, 23 structured log events
- **Pipeline Integration** - Seamless integration with Encina's CQRS pipeline via `ABACPipelineBehavior`
- **Railway Oriented Programming** - All operations return `Either<EncinaError, T>`, no exceptions
- **Health Check** - Optional health check verifying PAP policy loading
- **.NET 10 Compatible** - Built with latest C# 14 features

## Installation

```bash
dotnet add package Encina.Security.ABAC
```

## Quick Start

### 1. Register Services

```csharp
services.AddEncinaABAC(options =>
{
    options.DefaultNotApplicableEffect = Effect.Deny;
    options.EnforcementMode = ABACEnforcementMode.Block;
    options.AddHealthCheck = true;
});
```

### 2. Define Policies

#### Option A: Fluent Builder DSL

```csharp
services.AddEncinaABAC(options =>
{
    options.SeedPolicySets.Add(
        new PolicySetBuilder("finance-policies")
            .AddPolicy(new PolicyBuilder("approve-transactions")
                .ForResourceType<ApproveTransaction>()
                .AddRule(new RuleBuilder("finance-only", Effect.Permit)
                    .WithCondition(ConditionBuilder.And(
                        ConditionBuilder.Equal(
                            AttributeCategory.Subject, "department", XACMLDataTypes.String, "Finance"),
                        ConditionBuilder.LessThanOrEqual(
                            AttributeCategory.Resource, "amount", XACMLDataTypes.Integer, 50000)))
                    .Build())
                .WithAlgorithm(CombiningAlgorithmId.DenyOverrides)
                .Build())
            .WithAlgorithm(CombiningAlgorithmId.DenyOverrides)
            .Build());
});
```

#### Option B: EEL Inline Expressions

```csharp
[RequireCondition("user.department == \"Finance\" && resource.amount <= 50000")]
public sealed record ApproveTransaction(decimal Amount) : IRequest<ApprovalResult>;
```

### 3. Implement Attribute Providers

```csharp
public sealed class AppAttributeProvider(IHttpContextAccessor httpContext) : IAttributeProvider
{
    public Task<IReadOnlyDictionary<string, object?>> GetAttributesAsync(
        AttributeCategory category, CancellationToken ct = default)
    {
        var attrs = new Dictionary<string, object?>();

        if (category == AttributeCategory.Subject)
        {
            var user = httpContext.HttpContext?.User;
            attrs["department"] = user?.FindFirst("department")?.Value;
            attrs["clearanceLevel"] = int.Parse(user?.FindFirst("clearance")?.Value ?? "0");
        }

        return Task.FromResult<IReadOnlyDictionary<string, object?>>(attrs);
    }
}

// Register before AddEncinaABAC()
services.AddSingleton<IAttributeProvider, AppAttributeProvider>();
```

### 4. Decorate Request Types

```csharp
// Policy-based authorization
[RequirePolicy("finance-policies")]
public sealed record GetFinancialReport(Guid ReportId) : IQuery<ReportDto>;

// Inline expression authorization
[RequireCondition("user.roles.Contains(\"admin\") || user.clearanceLevel >= 5")]
public sealed record AccessClassifiedDocument(Guid DocId) : IQuery<DocumentDto>;

// Multiple conditions (AND logic by default)
[RequirePolicy("global-security")]
[RequireCondition("environment.isBusinessHours")]
public sealed record TransferFunds(decimal Amount) : ICommand<TransferResult>;
```

## Two Authorization Models

| Feature | EEL (Inline Expressions) | XACML Expression Trees |
|---------|--------------------------|------------------------|
| **Syntax** | `"user.department == \"Finance\""` | `Apply` + `AttributeDesignator` nodes |
| **Best for** | Simple-to-moderate conditions | Complex, composable policies |
| **Definition** | `[RequireCondition]` attribute | Fluent builders / PAP API |
| **Compilation** | Roslyn at startup (~50-100ms) | Immediate (C# objects) |
| **Combining** | AND between multiple attributes | 8 XACML combining algorithms |
| **Obligations** | Not supported | Full XACML 3.0 support |

## Combining Algorithms

| Algorithm | Behavior |
|-----------|----------|
| `DenyOverrides` | Any Deny wins (recommended default) |
| `PermitOverrides` | Any Permit wins |
| `FirstApplicable` | First matching rule wins |
| `OnlyOneApplicable` | Exactly one must match |
| `DenyUnlessPermit` | Default deny, no edge cases |
| `PermitUnlessDeny` | Default permit |
| `OrderedDenyOverrides` | DenyOverrides with deterministic obligation order |
| `OrderedPermitOverrides` | PermitOverrides with deterministic obligation order |

## Obligations & Advice

```csharp
// Implement an obligation handler
public sealed class AuditObligationHandler(IAuditService audit) : IObligationHandler
{
    public bool CanHandle(string obligationId) => obligationId == "audit-access";

    public async Task<Either<EncinaError, Unit>> HandleAsync(
        Obligation obligation, CancellationToken ct = default)
    {
        await audit.LogAccessAsync(obligation.Attributes);
        return Unit.Default;
    }
}

// Register in DI
services.AddSingleton<IObligationHandler, AuditObligationHandler>();
```

Per XACML 3.0 section 7.18: if an obligation handler fails or is missing, access is automatically denied.

## Configuration

| Option | Default | Description |
|--------|---------|-------------|
| `EnforcementMode` | `Block` | `Block`, `Warn` (log only), or `Disabled` |
| `DefaultNotApplicableEffect` | `Deny` | What to do when no policy matches |
| `IncludeAdvice` | `true` | Execute advice expressions after decision |
| `FailOnMissingObligationHandler` | `true` | Deny access if obligation handler is missing |
| `ValidateExpressionsAtStartup` | `false` | Pre-compile all EEL expressions at startup |
| `AddHealthCheck` | `false` | Register ABAC health check |

## Observability

- **Tracing**: `Encina.Security.ABAC` ActivitySource with `ABAC.Evaluate` spans
- **Metrics**: 9 counters (`abac.evaluation.*`, `abac.obligation.*`, `abac.advice.*`) + 2 histograms (`abac.evaluation.duration`, `abac.obligation.duration`)
- **Logging**: 23 structured log events (EventId 9000-9022) via `[LoggerMessage]` source generator
- **Health Check**: `encina-abac` with tags `encina`, `security`, `abac`, `ready`

## Documentation

- [Quick Start Guide](../../docs/features/abac/quick-start.md)
- [Tutorials (8 scenarios)](../../docs/features/abac/tutorials.md)
- [XACML Architecture](../../docs/features/abac/xacml/architecture.md)
- [Policy Language](../../docs/features/abac/xacml/policy-language.md)
- [Combining Algorithms](../../docs/features/abac/xacml/combining-algorithms.md)
- [EEL Guide](../../docs/features/abac/eel/guide.md)
- [EEL Cookbook (17 patterns)](../../docs/features/abac/eel/cookbook.md)
- [Fluent DSL Guide](../../docs/features/abac/dsl-guide.md)
- [Configuration Reference](../../docs/features/abac/reference/configuration.md)
- [Error Reference](../../docs/features/abac/reference/errors.md)
- [Function Library (70+)](../../docs/features/abac/reference/function-library.md)
- [Cheat Sheet](../../docs/features/abac/reference/cheat-sheet.md)

## Architecture Decision Records

- [ADR-015: XACML 3.0 as ABAC Foundation](../../docs/architecture/adr/015-xacml-3.0-abac-foundation.md)
- [ADR-016: Roslyn Scripting for Expression Language](../../docs/architecture/adr/016-roslyn-expression-language.md)
- [ADR-017: EEL Naming and Design](../../docs/architecture/adr/017-eel-naming-design.md)

## Related Packages

- [`Encina.Security`](../Encina.Security/README.md) - Core RBAC security (can be combined with ABAC)
- [`Encina.Security.Encryption`](../Encina.Security.Encryption/README.md) - Field-level encryption
- [`Encina.Security.AntiTampering`](../Encina.Security.AntiTampering/README.md) - Request integrity verification

## License

[MIT](../../LICENSE)
