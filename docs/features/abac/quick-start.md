# ABAC Quick Start

Get Attribute-Based Access Control working in your Encina application in 15 minutes.

## 1. Installation

Add the NuGet package to your project:

```bash
dotnet add package Encina.Security.ABAC
```

> **Prerequisite**: .NET 10 and an existing Encina application with MediatR pipeline configured.

## 2. Register ABAC Services

In your `Program.cs` or service registration, call `AddEncinaABAC()`:

```csharp
using Encina.Security.ABAC;

services.AddEncinaABAC(options =>
{
    options.EnforcementMode = ABACEnforcementMode.Block;
});
```

This registers the full XACML 3.0 evaluation engine, including:

- **IPolicyDecisionPoint** (PDP) -- evaluates access requests against policies
- **IPolicyAdministrationPoint** (PAP) -- stores and manages policies (in-memory by default)
- **IAttributeProvider** -- collects subject, resource, and environment attributes
- **ABACPipelineBehavior** -- MediatR behavior that intercepts requests decorated with ABAC attributes

All registrations use `TryAdd`, so you can register custom implementations _before_ calling `AddEncinaABAC()` and they will take precedence.

## 3. Define Your First Policy

Use `PolicyBuilder` to define a policy that permits access for the Engineering department:

```csharp
using Encina.Security.ABAC;
using Encina.Security.ABAC.Builders;

var engineeringAccessPolicy = new PolicyBuilder("engineering-access")
    .WithDescription("Allow Engineering department to access code review resources")
    .WithAlgorithm(CombiningAlgorithmId.DenyOverrides)
    .AddRule("allow-engineering", Effect.Permit, rule => rule
        .WithDescription("Permit when subject belongs to Engineering")
        .WithTarget(t => t
            .AnyOf(any => any
                .AllOf(all => all
                    .MatchAttribute(
                        AttributeCategory.Subject,
                        "department",
                        ConditionOperator.Equals,
                        "Engineering")))))
    .Build();
```

Key concepts:

- **Target** determines _which_ requests the policy applies to (filter)
- **Rule** declares the authorization effect (`Permit` or `Deny`) when its conditions are met
- **CombiningAlgorithm** resolves conflicts when multiple rules produce different effects

## 4. Seed Policies at Startup

Pass your policy to `ABACOptions.SeedPolicies` so it is loaded into the PAP when the application starts:

```csharp
services.AddEncinaABAC(options =>
{
    options.EnforcementMode = ABACEnforcementMode.Block;

    options.SeedPolicies.Add(engineeringAccessPolicy);
});
```

The `ABACPolicySeedingHostedService` runs at startup and inserts every entry from `SeedPolicies` and `SeedPolicySets` into the PAP. Duplicate IDs are logged as warnings and skipped.

## 5. Implement IAttributeProvider

The default `IAttributeProvider` returns empty dictionaries. Replace it with a provider that extracts real attributes from your application context:

```csharp
using Encina.Security.ABAC;
using Microsoft.AspNetCore.Http;

public sealed class ClaimsAttributeProvider(IHttpContextAccessor httpContextAccessor)
    : IAttributeProvider
{
    public ValueTask<IReadOnlyDictionary<string, object>> GetSubjectAttributesAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var user = httpContextAccessor.HttpContext?.User;
        var attributes = new Dictionary<string, object>
        {
            ["department"] = user?.FindFirst("department")?.Value ?? string.Empty,
            ["role"] = user?.FindFirst("role")?.Value ?? string.Empty,
        };
        return ValueTask.FromResult<IReadOnlyDictionary<string, object>>(attributes);
    }

    public ValueTask<IReadOnlyDictionary<string, object>> GetResourceAttributesAsync<TResource>(
        TResource resource,
        CancellationToken cancellationToken = default)
    {
        var attributes = new Dictionary<string, object>
        {
            ["resourceType"] = typeof(TResource).Name,
        };
        return ValueTask.FromResult<IReadOnlyDictionary<string, object>>(attributes);
    }

    public ValueTask<IReadOnlyDictionary<string, object>> GetEnvironmentAttributesAsync(
        CancellationToken cancellationToken = default)
    {
        var attributes = new Dictionary<string, object>
        {
            ["currentTime"] = DateTime.UtcNow,
            ["isBusinessHours"] = DateTime.UtcNow.Hour is >= 9 and < 17,
        };
        return ValueTask.FromResult<IReadOnlyDictionary<string, object>>(attributes);
    }
}
```

Register it **before** `AddEncinaABAC()` so it takes precedence over the default:

```csharp
services.AddHttpContextAccessor();
services.AddScoped<IAttributeProvider, ClaimsAttributeProvider>();
services.AddEncinaABAC(options =>
{
    options.SeedPolicies.Add(engineeringAccessPolicy);
});
```

## 6. Decorate Your Request with [RequirePolicy]

Apply `[RequirePolicy]` to any MediatR request to enforce ABAC evaluation:

```csharp
using Encina.Security.ABAC;

[RequirePolicy("engineering-access")]
public sealed record GetCodeReviewsQuery(Guid ProjectId) : IQuery<List<CodeReviewDto>>;
```

When this query enters the MediatR pipeline, `ABACPipelineBehavior` will:

1. Collect attributes via your `IAttributeProvider`
2. Evaluate the `"engineering-access"` policy in the PDP
3. **Permit** -- the request continues to the handler
4. **Deny** -- the request is blocked with an authorization error

### Multiple Policies

Stack multiple attributes for layered authorization. By default, **all must permit** (AND logic):

```csharp
[RequirePolicy("engineering-access")]
[RequirePolicy("senior-reviewer")]
public sealed record ApproveCodeReviewCommand(Guid ReviewId) : ICommand;
```

Set `AllMustPass = false` if **any one** policy permitting is sufficient (OR logic):

```csharp
[RequirePolicy("admin-override", AllMustPass = false)]
[RequirePolicy("engineering-access", AllMustPass = false)]
public sealed record GetCodeReviewsQuery(Guid ProjectId) : IQuery<List<CodeReviewDto>>;
```

## 7. Test It

Send a request and observe the ABAC evaluation:

```csharp
// User with department = "Engineering" in their claims
var result = await mediator.Send(new GetCodeReviewsQuery(projectId));
// --> Permit: request reaches the handler

// User with department = "Marketing"
var result = await mediator.Send(new GetCodeReviewsQuery(projectId));
// --> Deny: request is blocked by ABACPipelineBehavior
```

### Debug with Warn Mode

During development, switch to `Warn` mode to log denials without blocking:

```csharp
services.AddEncinaABAC(options =>
{
    options.EnforcementMode = ABACEnforcementMode.Warn;
    options.SeedPolicies.Add(engineeringAccessPolicy);
});
```

Denied requests will proceed to the handler, but a warning is logged with the full evaluation result.

## 8. Alternative: Use [RequireCondition] for Simple Cases

For straightforward checks that do not need a full policy definition, use `[RequireCondition]` with an inline EEL (Encina Expression Language) expression:

```csharp
using Encina.Security.ABAC;

[RequireCondition("subject.department == 'Engineering'")]
public sealed record GetCodeReviewsQuery(Guid ProjectId) : IQuery<List<CodeReviewDto>>;
```

EEL expressions are compiled once via Roslyn and cached. They have access to `subject`, `resource`, `action`, and `environment` attribute bags.

More examples:

```csharp
// Time-based restriction
[RequireCondition("environment.isBusinessHours == true")]
public sealed record ProcessPayrollCommand(Guid PayrollId) : ICommand;

// Clearance level check
[RequireCondition("subject.clearanceLevel >= resource.classification")]
public sealed record GetClassifiedDocumentQuery(Guid DocumentId) : IQuery<DocumentDto>;
```

### Validate Expressions at Startup

Enable fail-fast validation so invalid EEL expressions are caught before the first request:

```csharp
services.AddEncinaABAC(options =>
{
    options.ValidateExpressionsAtStartup = true;
    options.ExpressionScanAssemblies.Add(typeof(GetCodeReviewsQuery).Assembly);
});
```

## 9. Next Steps

- **[Policy Modeling (XACML 3.0)](xacml/)** -- PolicySets, combining algorithms, obligations, and advice
- **[Encina Expression Language (EEL)](eel/)** -- full expression syntax, custom functions, Roslyn integration
- **[Advanced Topics](advanced/)** -- custom PAP backends, obligation handlers, multi-tenancy, health checks
- **[API Reference](reference/)** -- complete API documentation for all ABAC types
