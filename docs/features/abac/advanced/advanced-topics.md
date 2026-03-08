# Advanced ABAC Topics

This guide covers advanced patterns, extensibility points, and integration strategies for
Encina's Attribute-Based Access Control (ABAC) engine. It assumes familiarity with the
core ABAC concepts (policies, rules, targets, combining algorithms) covered in the main
documentation.

---

## 1. Overview

Encina.Security.ABAC follows the XACML 3.0 architecture with four extension points that
advanced users typically customize:

| Component | Interface | Default Implementation |
|-----------|-----------|------------------------|
| Policy Administration Point | `IPolicyAdministrationPoint` | `InMemoryPolicyAdministrationPoint` |
| Policy Decision Point | `IPolicyDecisionPoint` | `XACMLPolicyDecisionPoint` |
| Attribute Provider | `IAttributeProvider` | `DefaultAttributeProvider` |
| Function Registry | `IFunctionRegistry` | `DefaultFunctionRegistry` |

All default registrations use `TryAdd`, so you can register custom implementations
**before** calling `AddEncinaABAC()` and the defaults will be skipped.

---

## 2. Custom Policy Administration Point

The `IPolicyAdministrationPoint` manages the lifecycle of policies and policy sets through
hierarchical CRUD operations. The built-in `InMemoryPolicyAdministrationPoint` stores
everything in `ConcurrentDictionary` instances -- suitable for development and testing, but
data is lost on process restart.

For production, implement a persistent PAP backed by a database, Keycloak, or OPA.

### 2.1 The Interface

```csharp
public interface IPolicyAdministrationPoint
{
    // PolicySet CRUD
    ValueTask<Either<EncinaError, IReadOnlyList<PolicySet>>> GetPolicySetsAsync(CancellationToken ct = default);
    ValueTask<Either<EncinaError, Option<PolicySet>>> GetPolicySetAsync(string policySetId, CancellationToken ct = default);
    ValueTask<Either<EncinaError, Unit>> AddPolicySetAsync(PolicySet policySet, CancellationToken ct = default);
    ValueTask<Either<EncinaError, Unit>> UpdatePolicySetAsync(PolicySet policySet, CancellationToken ct = default);
    ValueTask<Either<EncinaError, Unit>> RemovePolicySetAsync(string policySetId, CancellationToken ct = default);

    // Policy CRUD
    ValueTask<Either<EncinaError, IReadOnlyList<Policy>>> GetPoliciesAsync(string? policySetId, CancellationToken ct = default);
    ValueTask<Either<EncinaError, Option<Policy>>> GetPolicyAsync(string policyId, CancellationToken ct = default);
    ValueTask<Either<EncinaError, Unit>> AddPolicyAsync(Policy policy, string? parentPolicySetId, CancellationToken ct = default);
    ValueTask<Either<EncinaError, Unit>> UpdatePolicyAsync(Policy policy, CancellationToken ct = default);
    ValueTask<Either<EncinaError, Unit>> RemovePolicyAsync(string policyId, CancellationToken ct = default);
}
```

All operations return `Either<EncinaError, T>` following Railway Oriented Programming.

### 2.2 Database-Backed Example

```csharp
public sealed class SqlPolicyAdministrationPoint(
    AppDbContext dbContext,
    ILogger<SqlPolicyAdministrationPoint> logger) : IPolicyAdministrationPoint
{
    public async ValueTask<Either<EncinaError, Unit>> AddPolicySetAsync(
        PolicySet policySet, CancellationToken ct = default)
    {
        var entity = PolicySetEntity.FromModel(policySet);
        dbContext.PolicySets.Add(entity);
        await dbContext.SaveChangesAsync(ct);
        logger.LogDebug("Policy set '{Id}' persisted to database", policySet.Id);
        return unit;
    }

    // ... implement remaining methods
}
```

### 2.3 Registration

Register your custom PAP **before** calling `AddEncinaABAC()`:

```csharp
services.AddSingleton<IPolicyAdministrationPoint, SqlPolicyAdministrationPoint>();
services.AddEncinaABAC(); // InMemoryPolicyAdministrationPoint is skipped (TryAdd)
```

---

## 3. Custom Function Registration

The `DefaultFunctionRegistry` pre-registers all standard XACML 3.0 functions (equality,
comparison, arithmetic, string, logical, bag, set, higher-order, type conversion, regex).
You can add business-specific functions via `ABACOptions.AddFunction()`.

### 3.1 Implementing IXACMLFunction

```csharp
public sealed class GeoDistanceFunction : IXACMLFunction
{
    public string ReturnType => XACMLDataTypes.Double;

    public object? Evaluate(IReadOnlyList<object?> arguments)
    {
        if (arguments.Count != 4)
            throw new InvalidOperationException(
                "geo-distance requires 4 arguments: lat1, lon1, lat2, lon2.");

        var lat1 = Convert.ToDouble(arguments[0]);
        var lon1 = Convert.ToDouble(arguments[1]);
        var lat2 = Convert.ToDouble(arguments[2]);
        var lon2 = Convert.ToDouble(arguments[3]);

        return HaversineDistance(lat1, lon1, lat2, lon2);
    }

    private static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Haversine formula implementation
        const double R = 6371.0; // Earth radius in km
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}
```

### 3.2 Registration via Options

```csharp
services.AddEncinaABAC(options =>
{
    options.AddFunction("custom:geo-distance", new GeoDistanceFunction())
           .AddFunction("custom:risk-score", new RiskScoreFunction());
});
```

Functions are registered into the `IFunctionRegistry` singleton during DI configuration
and are immediately available for condition evaluation across all policies.

---

## 4. Custom Attribute Providers

The `IAttributeProvider` bridges your domain model and the XACML attribute model. The
default implementation extracts basic claims from the HTTP context, but most applications
need richer attributes from databases or external services.

### 4.1 The Interface

```csharp
public interface IAttributeProvider
{
    ValueTask<IReadOnlyDictionary<string, object>> GetSubjectAttributesAsync(
        string userId, CancellationToken ct = default);

    ValueTask<IReadOnlyDictionary<string, object>> GetResourceAttributesAsync<TResource>(
        TResource resource, CancellationToken ct = default);

    ValueTask<IReadOnlyDictionary<string, object>> GetEnvironmentAttributesAsync(
        CancellationToken ct = default);
}
```

### 4.2 Database-Enriched Provider

```csharp
public sealed class EnrichedAttributeProvider(
    IUserRepository userRepo,
    IHttpContextAccessor httpContext,
    TimeProvider timeProvider) : IAttributeProvider
{
    public async ValueTask<IReadOnlyDictionary<string, object>> GetSubjectAttributesAsync(
        string userId, CancellationToken ct = default)
    {
        var user = await userRepo.GetByIdAsync(userId, ct);
        return new Dictionary<string, object>
        {
            ["department"] = user.Department,
            ["clearanceLevel"] = user.ClearanceLevel,
            ["region"] = user.Region,
            ["isContractor"] = user.EmploymentType == EmploymentType.Contractor
        };
    }

    public ValueTask<IReadOnlyDictionary<string, object>> GetResourceAttributesAsync<TResource>(
        TResource resource, CancellationToken ct = default)
    {
        var attrs = new Dictionary<string, object>();
        if (resource is IClassifiedResource classified)
        {
            attrs["classification"] = classified.Classification.ToString();
            attrs["ownerId"] = classified.OwnerId.ToString();
        }
        return ValueTask.FromResult<IReadOnlyDictionary<string, object>>(attrs);
    }

    public ValueTask<IReadOnlyDictionary<string, object>> GetEnvironmentAttributesAsync(
        CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow();
        var ip = httpContext.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return ValueTask.FromResult<IReadOnlyDictionary<string, object>>(
            new Dictionary<string, object>
            {
                ["currentTime"] = now,
                ["ipAddress"] = ip,
                ["isBusinessHours"] = now.Hour >= 8 && now.Hour < 18
            });
    }
}
```

Register the custom provider before `AddEncinaABAC()`:

```csharp
services.AddScoped<IAttributeProvider, EnrichedAttributeProvider>();
services.AddEncinaABAC();
```

---

## 5. Variable Definitions

XACML 3.0 section 7.8 defines variable definitions as reusable sub-expressions scoped to a single
`Policy`. They avoid duplication when the same logic is needed across multiple rules.

### 5.1 Defining Variables

```csharp
var isHighValueVar = new VariableDefinition
{
    VariableId = "isHighValue",
    Expression = new Apply
    {
        FunctionId = "integer-greater-than",
        Arguments =
        [
            new AttributeDesignator
            {
                Category = AttributeCategory.Resource,
                AttributeId = "amount",
                DataType = "integer"
            },
            new AttributeValue { DataType = "integer", Value = 10000 }
        ]
    }
};
```

### 5.2 Referencing Variables in Rules

Once defined, a variable can be referenced from any rule condition within the same policy
via `VariableReference`:

```csharp
var policy = new Policy
{
    Id = "financial-approval",
    Version = "1.0",
    Rules = [requireManagerForHighValueRule, standardApprovalRule],
    Algorithm = CombiningAlgorithmId.DenyOverrides,
    VariableDefinitions = [isHighValueVar],
    Obligations = [],
    Advice = []
};
```

Variables are evaluated lazily on first reference and cached for the duration of that
single policy evaluation pass.

---

## 6. Multiple [RequirePolicy] Attributes

The `RequirePolicyAttribute` supports `AllowMultiple = true`, enabling multiple policy
requirements on a single request.

### 6.1 AND Logic (Default)

When `AllMustPass = true` (the default), **all** referenced policies must produce a Permit
decision:

```csharp
[RequirePolicy("data-classification")]
[RequirePolicy("department-access")]
public sealed record GetClassifiedDocumentQuery(Guid DocumentId) : IQuery<DocumentDto>;
```

Both `data-classification` and `department-access` must Permit. If either Denies, the
request is rejected.

### 6.2 OR Logic

Set `AllMustPass = false` on all attributes to switch to OR logic, where **any single**
Permit suffices:

```csharp
[RequirePolicy("admin-override", AllMustPass = false)]
[RequirePolicy("standard-access", AllMustPass = false)]
public sealed record GetResourceQuery(Guid ResourceId) : IQuery<ResourceDto>;
```

If the user satisfies `admin-override` or `standard-access`, the request proceeds.

### 6.3 Mixing Strategies

You can combine AND and OR by structuring policies within policy sets that use appropriate
combining algorithms. The `AllMustPass` property controls only the pipeline-level behavior
for the attributes on that request class.

---

## 7. Nested PolicySets

XACML 3.0 section 7.11 allows `PolicySet` to recursively contain other `PolicySet`
elements, enabling arbitrarily deep hierarchies for enterprise scenarios.

### 7.1 Hierarchical Structure

```csharp
var teamPolicies = new PolicySet
{
    Id = "engineering-team-policies",
    Policies = [codeReviewPolicy, deploymentPolicy],
    PolicySets = [],
    Algorithm = CombiningAlgorithmId.PermitOverrides,
    Obligations = [],
    Advice = []
};

var departmentPolicies = new PolicySet
{
    Id = "engineering-department-policies",
    Policies = [generalEngineeringPolicy],
    PolicySets = [teamPolicies],          // Nested policy set
    Algorithm = CombiningAlgorithmId.DenyOverrides,
    Obligations = [],
    Advice = []
};

var organizationPolicies = new PolicySet
{
    Id = "organization-policies",
    Policies = [globalCompliancePolicy],
    PolicySets = [departmentPolicies],    // Two levels deep
    Algorithm = CombiningAlgorithmId.DenyOverrides,
    Obligations = [],
    Advice = []
};
```

### 7.2 Evaluation Order

The combining algorithm at each level determines how child effects are aggregated upward.
With `DenyOverrides` at the organization level, a deny from any nested level propagates
up.

---

## 8. Policy Priority

Both `Policy` and `PolicySet` have a `Priority` property (default `0`) that affects
evaluation order when used with ordered combining algorithms.

### 8.1 How Priority Works

- **Lower values** indicate **higher priority** (0 is evaluated first).
- Used by `CombiningAlgorithmId.FirstApplicable` and ordered algorithm variants.
- When two policies share the same priority, their declaration order is used as tiebreaker.

```csharp
var emergencyOverride = new Policy
{
    Id = "emergency-override",
    Priority = 0,       // Evaluated first
    Rules = [permitAllDuringEmergency],
    Algorithm = CombiningAlgorithmId.PermitOverrides,
    Obligations = [],
    Advice = [],
    VariableDefinitions = []
};

var standardPolicy = new Policy
{
    Id = "standard-access",
    Priority = 10,      // Evaluated second
    Rules = [normalAccessRules],
    Algorithm = CombiningAlgorithmId.DenyOverrides,
    Obligations = [],
    Advice = [],
    VariableDefinitions = []
};
```

### 8.2 Disabling Policies Without Removal

Use `IsEnabled = false` to temporarily skip a policy or policy set during evaluation
without removing it from the hierarchy. Disabled elements produce `NotApplicable`.

```csharp
var legacyPolicy = standardPolicy with { IsEnabled = false };
```

---

## 9. Dynamic Policy Management

The `IPolicyAdministrationPoint` supports full CRUD at runtime, enabling dynamic policy
management through admin UIs, APIs, or external policy engines.

### 9.1 Runtime CRUD

```csharp
public sealed class PolicyManagementEndpoint(IPolicyAdministrationPoint pap)
{
    public async Task<IResult> AddPolicy(PolicyDto dto, CancellationToken ct)
    {
        var policy = dto.ToModel();
        var result = await pap.AddPolicyAsync(policy, parentPolicySetId: dto.ParentId, ct);
        return result.Match<IResult>(
            Left: error => Results.BadRequest(error.Message),
            Right: _ => Results.Created($"/policies/{policy.Id}", policy.Id));
    }

    public async Task<IResult> UpdatePolicy(string policyId, PolicyDto dto, CancellationToken ct)
    {
        var policy = dto.ToModel() with { Id = policyId };
        var result = await pap.UpdatePolicyAsync(policy, ct);
        return result.Match<IResult>(
            Left: error => Results.NotFound(error.Message),
            Right: _ => Results.NoContent());
    }

    public async Task<IResult> RemovePolicy(string policyId, CancellationToken ct)
    {
        var result = await pap.RemovePolicyAsync(policyId, ct);
        return result.Match<IResult>(
            Left: error => Results.NotFound(error.Message),
            Right: _ => Results.NoContent());
    }
}
```

### 9.2 Thread Safety

`InMemoryPolicyAdministrationPoint` uses `ConcurrentDictionary` for thread-safe concurrent
access. Custom PAP implementations must ensure thread safety when multiple requests
modify policies concurrently.

---

## 10. Policy Seeding Service

The `ABACPolicySeedingHostedService` is an `IHostedService` that seeds policies into the
PAP during application startup. It is automatically registered when either
`ABACOptions.SeedPolicySets` or `ABACOptions.SeedPolicies` contains entries.

### 10.1 Configuration

```csharp
services.AddEncinaABAC(options =>
{
    // Seed policy sets (with contained policies)
    options.SeedPolicySets.Add(new PolicySet
    {
        Id = "access-control",
        Policies = [financePolicy, hrPolicy],
        PolicySets = [],
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Obligations = [],
        Advice = []
    });

    // Seed standalone policies (not belonging to any policy set)
    options.SeedPolicies.Add(new Policy
    {
        Id = "global-audit-policy",
        Rules = [auditRule],
        Algorithm = CombiningAlgorithmId.PermitOverrides,
        Obligations = [auditObligation],
        Advice = [],
        VariableDefinitions = []
    });
});
```

### 10.2 Behavior

- Runs once at startup via `IHostedService.StartAsync`.
- Duplicate IDs are logged as warnings and skipped (not errors).
- Logs a summary: `Seeding ABAC policies: 2 policy set(s), 1 standalone policy(ies)`.
- Failures for individual items do not prevent other items from being seeded.

### 10.3 Loading from External Sources

Combine seeding with a factory method that loads policies from JSON, a database, or an
external service:

```csharp
services.AddEncinaABAC(options =>
{
    var policySets = PolicyLoader.LoadFromEmbeddedJson("policies.json");
    foreach (var ps in policySets)
    {
        options.SeedPolicySets.Add(ps);
    }
});
```

---

## 11. Integration with Encina.Security (RBAC)

Encina.Security provides RBAC (Role-Based Access Control) via `[RequirePermission]` and
`[RequireRole]` attributes. Both pipelines can coexist in the same application, with RBAC
handling coarse-grained access and ABAC handling fine-grained, attribute-based decisions.

### 11.1 Side-by-Side Registration

```csharp
// RBAC pipeline (coarse-grained: roles, permissions)
services.AddEncinaSecurity(options =>
{
    options.RequireAuthenticatedByDefault = true;
    options.AddHealthCheck = true;
});

// ABAC pipeline (fine-grained: attributes, conditions)
services.AddEncinaABAC(options =>
{
    options.EnforcementMode = ABACEnforcementMode.Block;
    options.DefaultNotApplicableEffect = Effect.Deny;
    options.AddHealthCheck = true;
});
```

### 11.2 Combining RBAC and ABAC on a Single Request

```csharp
// RBAC: user must have "finance:read" permission
[RequirePermission("finance:read")]
// ABAC: user's department must match and data classification rules apply
[RequirePolicy("financial-data-access")]
public sealed record GetFinancialReportQuery(Guid ReportId) : IQuery<ReportDto>;
```

The RBAC `SecurityPipelineBehavior` and the ABAC `ABACPipelineBehavior` execute as
separate MediatR pipeline behaviors. Both must permit for the request to proceed.

### 11.3 When to Use Which

| Scenario | Recommended Approach |
|----------|---------------------|
| User must have a specific role | RBAC (`[RequireRole]`) |
| User must have a specific permission | RBAC (`[RequirePermission]`) |
| Access depends on resource attributes | ABAC (`[RequirePolicy]`) |
| Access depends on time/location/context | ABAC (`[RequirePolicy]`) |
| Simple role checks + attribute conditions | Both |

---

## 12. Testing ABAC Policies

Encina provides `EELTestHelper` for testing EEL (Encina Expression Language) expressions
and standard patterns for mocking the `IPolicyDecisionPoint`.

### 12.1 Validating All Expressions at Test Time

Use `EELTestHelper.ValidateAllExpressionsAsync` to catch invalid EEL expressions before
they reach production:

```csharp
[Fact]
public async Task AllExpressions_ShouldCompile()
{
    await EELTestHelper.ValidateAllExpressionsAsync(typeof(MyCommand).Assembly);
}
```

This scans the assembly for all `[RequireCondition]` attributes, compiles every expression,
and throws `InvalidOperationException` with details if any fail.

### 12.2 Evaluating Individual Expressions

```csharp
[Fact]
public async Task ShouldPermitFinanceDepartment()
{
    var result = await EELTestHelper.EvaluateAsync(
        "user.department == \"Finance\" && resource.amount > 10000",
        new
        {
            user = new { department = "Finance" },
            resource = new { amount = 50000 },
            environment = new { },
            action = new { name = "approve" }
        });

    Assert.True(result.IsRight);
    result.IfRight(value => Assert.True(value));
}
```

### 12.3 Assertion Helpers

```csharp
// Assert that an expression compiles
await EELTestHelper.AssertCompilesAsync("user.role == \"admin\"");

// Assert that a malformed expression does NOT compile
await EELTestHelper.AssertDoesNotCompileAsync("user.role ==== \"admin\"");
```

### 12.4 Mocking IPolicyDecisionPoint

For unit testing request handlers or pipeline behaviors, mock the PDP to return
controlled decisions:

```csharp
[Fact]
public async Task Handler_WhenPolicyPermits_ShouldReturnData()
{
    var mockPdp = new Mock<IPolicyDecisionPoint>();
    mockPdp.Setup(p => p.EvaluateAsync(It.IsAny<PolicyEvaluationContext>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(new PolicyDecision
           {
               Effect = Effect.Permit,
               Obligations = [],
               Advice = []
           });

    // Inject mockPdp.Object into the pipeline behavior or handler under test
}

[Fact]
public async Task Handler_WhenPolicyDenies_ShouldRejectRequest()
{
    var mockPdp = new Mock<IPolicyDecisionPoint>();
    mockPdp.Setup(p => p.EvaluateAsync(It.IsAny<PolicyEvaluationContext>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(new PolicyDecision
           {
               Effect = Effect.Deny,
               Obligations = [],
               Advice = []
           });

    // Assert that the pipeline returns an authorization error
}
```

### 12.5 Startup Expression Validation

For fail-fast behavior in staging/production, enable expression precompilation:

```csharp
services.AddEncinaABAC(options =>
{
    options.ValidateExpressionsAtStartup = true;
    options.ExpressionScanAssemblies.Add(typeof(MyCommand).Assembly);
});
```

The `EELExpressionPrecompilationService` scans the specified assemblies at startup and
throws `InvalidOperationException` if any `[RequireCondition]` expression fails to
compile, preventing the application from starting with invalid policy expressions.
