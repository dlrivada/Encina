# ABAC Configuration Reference

## Overview

Encina ABAC is configured through the `AddEncinaABAC()` extension method on `IServiceCollection`. All behavior is controlled via the `ABACOptions` class, which configures the XACML 3.0 evaluation engine, enforcement mode, custom functions, policy seeding, and health checks.

## Service Registration

### AddEncinaABAC()

```csharp
public static IServiceCollection AddEncinaABAC(
    this IServiceCollection services,
    Action<ABACOptions>? configure = null);
```

The method accepts an optional `Action<ABACOptions>` delegate. When omitted, all defaults apply.

**Registered services:**

| Service | Implementation | Lifetime |
|---------|---------------|----------|
| `ABACOptions` | Configured instance | Options pattern |
| `IFunctionRegistry` | `DefaultFunctionRegistry` | Singleton |
| `CombiningAlgorithmFactory` | Self | Singleton |
| `TargetEvaluator` | Self | Singleton |
| `ConditionEvaluator` | Self | Singleton |
| `IPolicyAdministrationPoint` | `InMemoryPolicyAdministrationPoint` or `PersistentPolicyAdministrationPoint` | Singleton |
| `IPolicyDecisionPoint` | `XACMLPolicyDecisionPoint` | Singleton |
| `IPolicyInformationPoint` | `DefaultPolicyInformationPoint` | Singleton |
| `IAttributeProvider` | `DefaultAttributeProvider` | Scoped |
| `ObligationExecutor` | Self | Scoped |
| `ABACPipelineBehavior<TRequest, TResponse>` | Self | Transient |
| `EELCompiler` | Self | Singleton |

All registrations use `TryAdd`, meaning you can register custom implementations **before** calling `AddEncinaABAC()` and they will not be overwritten.

**Conditional registrations:**

| Condition | Service Registered |
|-----------|-------------------|
| `SeedPolicySets` or `SeedPolicies` is non-empty | `ABACPolicySeedingHostedService` (IHostedService) |
| `ValidateExpressionsAtStartup` is `true` and `ExpressionScanAssemblies` is non-empty | `EELExpressionPrecompilationService` (IHostedService) |
| `AddHealthCheck` is `true` | `ABACHealthCheck` via `IHealthChecksBuilder` |
| `UsePersistentPAP` is `true` | `IPolicySerializer` → `DefaultPolicySerializer` (Singleton), `PersistentPolicyAdministrationPoint` (Singleton) |
| `UsePersistentPAP` is `true` and `PolicyCaching.Enabled` is `true` | `CachingPolicyStoreDecorator` wraps `IPolicyStore` |
| `PolicyCaching.EnablePubSubInvalidation` is `true` | `PolicyCachePubSubHostedService` (IHostedService) |

## ABACOptions Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `EnforcementMode` | `ABACEnforcementMode` | `Block` | Controls how Deny decisions are enforced. See [ABACEnforcementMode](#abacenforcementmode). |
| `DefaultNotApplicableEffect` | `Effect` | `Deny` | Effect applied when no policy matches the request (NotApplicable). `Deny` = closed-world; `Permit` = open-world. |
| `IncludeAdvice` | `bool` | `true` | When `true`, advice expressions from policies are included in evaluation results and executed on a best-effort basis. |
| `FailOnMissingObligationHandler` | `bool` | `true` | When `true`, a missing obligation handler causes an immediate deny (per XACML 3.0 section 7.18). Set to `false` only during development. |
| `AddHealthCheck` | `bool` | `false` | When `true`, registers an `ABACHealthCheck` that verifies at least one policy or policy set is loaded. Returns `Degraded` if the PAP is empty. |
| `ValidateExpressionsAtStartup` | `bool` | `false` | When `true`, scans assemblies in `ExpressionScanAssemblies` for `RequireConditionAttribute` and compiles all EEL expressions at startup. Throws `InvalidOperationException` on failure. |
| `ExpressionScanAssemblies` | `List<Assembly>` | `[]` | Assemblies to scan for `RequireConditionAttribute` when `ValidateExpressionsAtStartup` is `true`. If the list is empty, a debug log is emitted and no validation occurs. |
| `CustomFunctions` | `List<(string FunctionId, IXACMLFunction Function)>` | `[]` | Custom XACML functions registered into `IFunctionRegistry` at startup, in addition to the standard XACML 3.0 built-in functions. |
| `UsePersistentPAP` | `bool` | `false` | When `true`, registers `PersistentPolicyAdministrationPoint` backed by `IPolicyStore`. Requires a provider package. See [Persistent PAP](persistent-pap.md). |
| `PolicyCaching` | `PolicyCachingOptions` | See below | Configuration for the caching decorator. Only applicable when `UsePersistentPAP = true`. |
| `SeedPolicySets` | `List<PolicySet>` | `[]` | Policy sets to seed into the PAP at application startup via `ABACPolicySeedingHostedService`. Duplicates are logged as warnings and skipped. |
| `SeedPolicies` | `List<Policy>` | `[]` | Standalone policies to seed into the PAP at startup. Duplicates are logged as warnings and skipped. |

## ABACEnforcementMode

The `ABACEnforcementMode` enum controls the Policy Enforcement Point (PEP) behavior in the pipeline:

| Value | Behavior |
|-------|----------|
| `Block` | **Production mode.** Deny decisions block request execution and return an `EncinaError`. Obligations are enforced. This is the default. |
| `Warn` | **Observation mode.** Deny decisions are logged as warnings but requests proceed normally. Useful for validating policies before enabling enforcement. |
| `Disabled` | **Bypass mode.** ABAC evaluation is completely skipped. No policies are evaluated, no obligations are executed. Useful during development or for feature-flagging ABAC. |

**Gradual rollout strategy:** Start with `Warn` to observe decisions in production logs, validate that policies behave as expected, then switch to `Block`.

## AddFunction() Method

Register custom XACML functions for use in policy conditions:

```csharp
public ABACOptions AddFunction(string functionId, IXACMLFunction function);
```

- `functionId` must be a non-empty string. Use a namespace prefix (e.g., `custom:`) to avoid collisions with standard XACML functions.
- `function` must implement `IXACMLFunction`.
- Returns the `ABACOptions` instance for fluent chaining.
- Throws `ArgumentException` if `functionId` is null or whitespace.
- Throws `ArgumentNullException` if `function` is null.

```csharp
options.AddFunction("custom:geo-within", new GeoWithinFunction())
       .AddFunction("custom:risk-score", new RiskScoreFunction());
```

## Policy Seeding

Seed policies into the PAP at application startup using `SeedPolicySets` and `SeedPolicies`:

```csharp
services.AddEncinaABAC(options =>
{
    // Seed a policy set (contains child policies)
    options.SeedPolicySets.Add(new PolicySet
    {
        PolicySetId = "medical-records",
        CombiningAlgorithmId = "deny-overrides",
        Policies = [doctorAccessPolicy, nurseAccessPolicy]
    });

    // Seed a standalone policy
    options.SeedPolicies.Add(new Policy
    {
        PolicyId = "admin-bypass",
        RuleCombiningAlgorithmId = "permit-overrides",
        Rules = [adminRule]
    });
});
```

When either list is non-empty, an `ABACPolicySeedingHostedService` is automatically registered. This hosted service runs at startup and calls the `IPolicyAdministrationPoint` to add each policy or policy set. If a duplicate ID is encountered, a warning is logged and the duplicate is skipped.

## Startup Expression Validation

Enable fail-fast validation of EEL (Encina Expression Language) expressions:

```csharp
services.AddEncinaABAC(options =>
{
    options.ValidateExpressionsAtStartup = true;
    options.ExpressionScanAssemblies.Add(typeof(MyCommand).Assembly);
});
```

The `EELExpressionPrecompilationService` scans the specified assemblies for `RequireConditionAttribute` decorations, compiles each EEL expression, and throws `InvalidOperationException` if any expression fails to compile. This catches invalid expressions at startup rather than at request time.

## Complete Configuration Examples

### Minimal (Defaults)

```csharp
services.AddEncinaABAC();
```

Uses `Block` enforcement, `Deny` for not-applicable, advice enabled, obligation handler failure enforced, no health check, no startup validation.

### Typical Application

```csharp
services.AddEncinaABAC(options =>
{
    options.EnforcementMode = ABACEnforcementMode.Block;
    options.DefaultNotApplicableEffect = Effect.Deny;
    options.AddHealthCheck = true;

    options.SeedPolicySets.Add(myOrganizationPolicies);
});
```

### Enterprise with Custom Functions and Startup Validation

```csharp
services.AddEncinaABAC(options =>
{
    options.EnforcementMode = ABACEnforcementMode.Block;
    options.DefaultNotApplicableEffect = Effect.Deny;
    options.IncludeAdvice = true;
    options.FailOnMissingObligationHandler = true;
    options.AddHealthCheck = true;

    // Validate all EEL expressions at startup
    options.ValidateExpressionsAtStartup = true;
    options.ExpressionScanAssemblies.Add(typeof(Program).Assembly);

    // Register domain-specific functions
    options.AddFunction("custom:geo-distance", new GeoDistanceFunction())
           .AddFunction("custom:risk-score", new RiskScoreFunction())
           .AddFunction("custom:business-hours", new BusinessHoursFunction());

    // Seed policies
    options.SeedPolicySets.Add(compliancePolicies);
    options.SeedPolicySets.Add(regionalPolicies);
    options.SeedPolicies.Add(emergencyOverridePolicy);
});
```

### Development / Observation Mode

```csharp
services.AddEncinaABAC(options =>
{
    options.EnforcementMode = ABACEnforcementMode.Warn;
    options.FailOnMissingObligationHandler = false;  // Allow soft-fail
    options.AddHealthCheck = true;

    options.SeedPolicySets.Add(draftPolicies);
});
```

### Persistent PAP (Database-Backed)

Enable database-backed policy storage with optional caching:

```csharp
// 1. Register a provider with ABAC policy store
services.AddEncinaEntityFrameworkCore<AppDbContext>(c => c.UseABACPolicyStore = true);

// 2. Configure ABAC with persistent PAP
services.AddEncinaABAC(options =>
{
    options.UsePersistentPAP = true;

    // Optional: enable caching with cross-instance invalidation
    options.PolicyCaching.Enabled = true;
    options.PolicyCaching.Duration = TimeSpan.FromMinutes(15);
    options.PolicyCaching.EnablePubSubInvalidation = true;

    options.SeedPolicySets.Add(myOrganizationPolicies);
});
```

When `UsePersistentPAP = true`, the `PersistentPolicyAdministrationPoint` is registered instead of `InMemoryPolicyAdministrationPoint`. An `IPolicyStore` must be provided by a database provider package (EF Core, Dapper, ADO.NET, or MongoDB).

> See [Persistent PAP Reference](persistent-pap.md) for full details including schema, caching options, and supported providers.

### Custom Implementation Override

Register a custom implementation before calling `AddEncinaABAC()`. The `TryAdd` semantics will preserve your registration for most services:

```csharp
// Custom serializer (overrides DefaultPolicySerializer)
services.AddSingleton<IPolicySerializer, MyCustomSerializer>();

// AddEncinaABAC preserves the custom serializer
services.AddEncinaABAC(options => { options.UsePersistentPAP = true; });
```

This pattern applies to: `IFunctionRegistry`, `IPolicyDecisionPoint`, `IPolicyInformationPoint`, `IAttributeProvider`, `IPolicySerializer`, or `ObligationExecutor`.
