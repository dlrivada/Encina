---
title: "XACML Obligations & Advice"
layout: default
parent: "Features"
---

# XACML Obligations & Advice

## Overview

XACML 3.0 defines two categories of post-decision actions that accompany an authorization
decision: **obligations** (mandatory) and **advice** (optional). These allow policies to
prescribe side effects that the Policy Enforcement Point (PEP) must or should perform after
the Policy Decision Point (PDP) returns its verdict.

Common post-decision actions include audit logging, user notifications, data masking, rate
limiting, and multi-factor authentication escalation. The critical distinction between
obligations and advice lies in their failure semantics: an obligation failure overrides a
Permit decision to Deny, while advice failures are logged but never affect the decision.

| Concept | Type | Failure Impact | XACML Reference |
|---------|------|----------------|-----------------|
| Obligation | `Obligation` | **Mandatory** -- failure overrides Permit to Deny | Section 7.18 |
| Advice | `AdviceExpression` | **Optional** -- failure is logged, decision unchanged | Section 7.18 |

## Obligations

An obligation is a mandatory action that the PEP **must** execute after the authorization
decision. Obligations are defined within policies and rules, and are included in the
`PolicyDecision` only when the decision effect matches the obligation's `FulfillOn` value.

```csharp
public sealed record Obligation
{
    public required string Id { get; init; }
    public required FulfillOn FulfillOn { get; init; }
    public required IReadOnlyList<AttributeAssignment> AttributeAssignments { get; init; }
}
```

The `Id` property is the key used to match obligations with their handlers. The PEP iterates
through all obligations in the decision, finds a handler via `IObligationHandler.CanHandle()`,
and executes it.

Example: an audit logging obligation triggered on Permit:

```csharp
var auditObligation = new Obligation
{
    Id = "audit-access",
    FulfillOn = FulfillOn.Permit,
    AttributeAssignments =
    [
        new AttributeAssignment
        {
            AttributeId = "reason",
            Value = new AttributeValue
            {
                DataType = XACMLDataTypes.String,
                Value = "Sensitive resource accessed"
            }
        }
    ]
};
```

## Advice

An advice expression is an optional recommendation that the PEP **may** choose to act on.
Unlike obligations, the PEP can ignore advice without affecting the authorization decision.

```csharp
public sealed record AdviceExpression
{
    public required string Id { get; init; }
    public required FulfillOn AppliesTo { get; init; }
    public required IReadOnlyList<AttributeAssignment> AttributeAssignments { get; init; }
}
```

Advice uses the same structural model as obligations (identifier, trigger condition,
attribute assignments) but with fundamentally different failure semantics.

Example: a notification advice triggered on Deny:

```csharp
var notifyAdvice = new AdviceExpression
{
    Id = "notify-user",
    AppliesTo = FulfillOn.Deny,
    AttributeAssignments =
    [
        new AttributeAssignment
        {
            AttributeId = "message",
            Value = new AttributeValue
            {
                DataType = XACMLDataTypes.String,
                Value = "Contact your manager to request access."
            }
        }
    ]
};
```

## FulfillOn / AppliesTo

Both obligations and advice are conditional on the authorization decision effect. The
`FulfillOn` enum specifies when the action should trigger:

```csharp
public enum FulfillOn
{
    Permit,   // Execute when the decision is Permit
    Deny      // Execute when the decision is Deny
}
```

The PDP filters obligations and advice before returning the `PolicyDecision`. Only those
whose `FulfillOn` / `AppliesTo` matches the final effect are included:

| Decision Effect | Obligations Included | Advice Included |
|-----------------|---------------------|-----------------|
| `Permit` | Those with `FulfillOn = Permit` | Those with `AppliesTo = Permit` |
| `Deny` | Those with `FulfillOn = Deny` | Those with `AppliesTo = Deny` |
| `NotApplicable` | None | None |
| `Indeterminate` | None | None |

Typical usage patterns:

| FulfillOn | Obligation Examples | Advice Examples |
|-----------|--------------------|-----------------|
| `Permit` | Audit logging, data masking, MFA escalation | Usage tracking, resource provisioning hints |
| `Deny` | Security alert, denial logging | User guidance, escalation suggestions |

## Obligation Handlers

The `IObligationHandler` interface defines the contract for executing obligations. Each
handler declares which obligation identifiers it can process and provides the execution logic:

```csharp
public interface IObligationHandler
{
    bool CanHandle(string obligationId);

    ValueTask<Either<EncinaError, Unit>> HandleAsync(
        Obligation obligation,
        PolicyEvaluationContext context,
        CancellationToken cancellationToken = default);
}
```

The `HandleAsync` method returns `Either<EncinaError, Unit>` following Encina's Railway
Oriented Programming pattern. A `Left` (error) result means the obligation could not be
fulfilled, which triggers the XACML obligation failure semantics.

### Example: Audit Logging Handler

```csharp
public sealed class AuditLogObligationHandler(
    IAuditService auditService) : IObligationHandler
{
    public bool CanHandle(string obligationId) =>
        obligationId is "audit-access" or "audit-modification";

    public async ValueTask<Either<EncinaError, Unit>> HandleAsync(
        Obligation obligation,
        PolicyEvaluationContext context,
        CancellationToken cancellationToken = default)
    {
        var reason = obligation.AttributeAssignments
            .FirstOrDefault(a => a.AttributeId == "reason")
            ?.Value is AttributeValue { Value: string reasonText }
                ? reasonText
                : "No reason provided";

        await auditService.LogAsync(new AuditEntry
        {
            ObligationId = obligation.Id,
            RequestType = context.RequestType.Name,
            Reason = reason,
            TimestampUtc = DateTime.UtcNow
        }, cancellationToken);

        return Unit.Default;
    }
}
```

Register handlers in DI:

```csharp
services.AddSingleton<IObligationHandler, AuditLogObligationHandler>();
services.AddSingleton<IObligationHandler, DataMaskingObligationHandler>();
services.AddSingleton<IObligationHandler, NotificationAdviceHandler>();
```

Multiple handlers can be registered. The `ObligationExecutor` iterates through all registered
handlers to find the first one where `CanHandle()` returns `true` for each obligation ID.

## Obligation Failure Semantics

> **CRITICAL**: Per XACML 3.0 Section 7.18, if the PEP cannot fulfill an obligation, it
> **must deny access** regardless of the PDP's original Permit decision.

This is the most important security guarantee of the obligation system. Consider this scenario:

1. The PDP evaluates a policy and returns `Effect.Permit` with an `audit-access` obligation.
2. The PEP attempts to execute the `audit-access` obligation.
3. The audit service is unavailable, and the handler returns an error.
4. **The PEP overrides the Permit to Deny** -- the request is blocked.

This ensures that mandatory side effects (audit trails, compliance logging) are never silently
skipped. Without this guarantee, a system could permit access without the required audit record.

The `ObligationExecutor` implements this behavior:

```csharp
// If any handler is missing or fails, the result is Left<EncinaError>
var result = await executor.ExecuteObligationsAsync(
    decision.Obligations, evaluationContext, cancellationToken);

result.Match(
    Left: error =>
    {
        // Obligation failed: override Permit to Deny
        // Access is blocked regardless of the PDP decision
    },
    Right: _ =>
    {
        // All obligations fulfilled: proceed with the original decision
    });
```

There are two failure modes:

| Failure Mode | Description | Result |
|-------------|-------------|--------|
| Missing handler | No `IObligationHandler` with `CanHandle(id) == true` | `EncinaError` -- access denied |
| Handler error | `HandleAsync()` returns `Left<EncinaError>` | `EncinaError` -- access denied |

Both are logged with structured diagnostics via `ABACLogMessages` and tracked by
`ABACDiagnostics` counters (`ObligationFailed`, `ObligationNoHandler`).

## Advice Execution

Advice expressions are executed on a **best-effort basis**. The `ObligationExecutor`
processes advice separately from obligations, with different failure semantics:

- If no handler is registered for an advice ID, a warning is logged and execution continues.
- If a handler returns an error, the error is logged but the authorization decision is **not**
  affected.

```csharp
// Advice failures are logged but never change the decision
await executor.ExecuteAdviceAsync(
    decision.Advice, evaluationContext, cancellationToken);
// No result to check -- advice is fire-and-forget
```

The `ExecuteAdviceAsync` method returns `ValueTask` (not `ValueTask<Either<...>>`),
reflecting the non-mandatory nature of advice.

Advice handlers use the same `IObligationHandler` interface. The `ObligationExecutor`
wraps each `AdviceExpression` in a synthetic `Obligation` for handler compatibility, but
the failure path diverges: errors are logged as warnings and execution proceeds to the
next advice expression.

## AttributeAssignment

Both obligations and advice carry `AttributeAssignment` values that parameterize the
post-decision action. An attribute assignment provides a named, optionally categorized
value to the handler:

```csharp
public sealed record AttributeAssignment
{
    public required string AttributeId { get; init; }
    public AttributeCategory? Category { get; init; }
    public required IExpression Value { get; init; }
}
```

The `Value` property is an `IExpression`, which can be:

| Expression Type | Description | Use Case |
|----------------|-------------|----------|
| `AttributeValue` | Literal typed value | Static parameters (reason text, severity) |
| `AttributeDesignator` | Resolved at evaluation time | Dynamic parameters (user ID, resource name) |
| `Apply` | Computed expression | Derived parameters (formatted timestamps) |

Example with multiple assignment types:

```csharp
new AttributeAssignment
{
    AttributeId = "reason",
    Value = new AttributeValue { DataType = XACMLDataTypes.String, Value = "Compliance audit" }
},
new AttributeAssignment
{
    AttributeId = "userId",
    Category = AttributeCategory.Subject,
    Value = new AttributeDesignator
    {
        Category = AttributeCategory.Subject,
        AttributeId = "userId",
        DataType = XACMLDataTypes.String,
        MustBePresent = true
    }
}
```

## ObligationExecutor

The `ObligationExecutor` is the internal service that orchestrates obligation and advice
execution. It is registered automatically by `AddEncinaABAC()` and used by the
`ABACPipelineBehavior`:

```csharp
public sealed class ObligationExecutor
{
    // Obligations: mandatory, failure = deny access
    public ValueTask<Either<EncinaError, Unit>> ExecuteObligationsAsync(
        IReadOnlyList<Obligation> obligations,
        PolicyEvaluationContext context,
        CancellationToken cancellationToken);

    // Advice: optional, failure = logged warning
    public ValueTask ExecuteAdviceAsync(
        IReadOnlyList<AdviceExpression> adviceExpressions,
        PolicyEvaluationContext context,
        CancellationToken cancellationToken);
}
```

The executor resolves handlers from the DI container (`IEnumerable<IObligationHandler>`) and
processes obligations and advice sequentially. Execution timing is tracked via
`ABACDiagnostics.ObligationDuration` for observability.

## Real-World Examples

### Audit Logging Obligation

Ensure every permitted access to financial data is audited:

```csharp
// In the policy definition
var obligation = new ObligationBuilder("audit-financial-access")
    .OnPermit()
    .WithAttribute("reason", "Financial data accessed")
    .WithAttribute("severity", "high")
    .Build();
```

### Email Notification Advice

Suggest notifying the resource owner when access is denied:

```csharp
var advice = new AdviceBuilder("notify-resource-owner")
    .OnDeny()
    .WithAttribute("message", "An access attempt to your resource was denied.")
    .WithAttribute("channel", "email")
    .Build();
```

### Data Masking Obligation

Require sensitive fields to be masked when a user without full clearance accesses a record:

```csharp
var maskObligation = new ObligationBuilder("mask-sensitive-fields")
    .OnPermit()
    .WithAttribute("fields", "ssn,creditCard,salary")
    .WithAttribute("maskPattern", "***-**-####")
    .Build();
```

The corresponding handler would intercept the response and apply masking before returning
the data to the caller.

### Rate Limiting Obligation

Enforce rate limits as a mandatory post-permit action:

```csharp
var rateLimitObligation = new ObligationBuilder("enforce-rate-limit")
    .OnPermit()
    .WithAttribute("maxRequests", "100")
    .WithAttribute("windowMinutes", "60")
    .Build();
```

### Building with ObligationBuilder and AdviceBuilder

The fluent builders simplify obligation and advice construction:

```csharp
// ObligationBuilder
var obligation = new ObligationBuilder("log-access")
    .OnPermit()                                        // Trigger on Permit
    .WithAttribute("reason", "Audit trail")            // Literal string value
    .WithAttribute("timestamp",
        ConditionBuilder.DateTimeValue(DateTime.UtcNow)) // Typed expression value
    .WithAttribute("resourceId",
        AttributeCategory.Resource,                     // Category-scoped assignment
        new AttributeDesignator
        {
            Category = AttributeCategory.Resource,
            AttributeId = "resourceId",
            DataType = XACMLDataTypes.String,
            MustBePresent = true
        })
    .Build();

// AdviceBuilder
var advice = new AdviceBuilder("suggest-mfa")
    .OnPermit()
    .WithAttribute("message", "Consider enabling MFA for this resource.")
    .WithAttribute("escalationType", "mfa")
    .Build();
```

Both builders follow the same pattern: set the trigger condition (`OnPermit()` or
`OnDeny()`), add attribute assignments, and call `Build()` to produce an immutable record.

## Configuring Obligation Behavior

The `ABACOptions.FailOnMissingObligationHandler` setting controls whether a missing
obligation handler causes an immediate deny:

```csharp
services.AddEncinaABAC(options =>
{
    // Production: missing handler = deny (XACML 3.0 compliant, default)
    options.FailOnMissingObligationHandler = true;

    // Development: missing handler = logged warning, access continues
    // options.FailOnMissingObligationHandler = false;
});
```

> **WARNING**: Setting `FailOnMissingObligationHandler = false` in production violates
> XACML 3.0 Section 7.18. Use this setting **only** during development when obligation
> handlers are not yet implemented.

The `IncludeAdvice` option controls whether advice expressions are collected and executed:

```csharp
services.AddEncinaABAC(options =>
{
    // Include advice in decisions (default: true)
    options.IncludeAdvice = true;

    // Disable advice for performance when not needed
    // options.IncludeAdvice = false;
});
```

When `IncludeAdvice` is `false`, the `PolicyEvaluationContext.IncludeAdvice` flag is set to
`false`, and the PDP skips advice collection. This can improve performance when advice
expressions are defined in policies but not needed at runtime.

## See Also

- [Architecture](architecture.md) -- ABAC component architecture and PEP/PDP/PIP data flow
- [Effects](effects.md) -- XACML 3.0 four-effect model (Permit, Deny, NotApplicable, Indeterminate)
- [Policy Language](policy-language.md) -- Defining policies, rules, and attaching obligations/advice
