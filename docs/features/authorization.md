# Policy-Based Authorization

Encina integrates with ASP.NET Core's native authorization system to provide CQRS-aware, declarative authorization for commands and queries. This feature **extends** ASP.NET Core Authorization — it does not replace it.

> **Key Principle**: Every policy, role check, and claim check goes through `IAuthorizationService`. Encina adds CQRS-aware defaults and Railway Oriented Programming (ROP) semantics on top.

## Quick Start

```csharp
// Program.cs
builder.Services.AddEncinaAuthorization(
    auth =>
    {
        auth.AutoApplyPolicies = true; // Enable CQRS-aware defaults
        auth.DefaultCommandPolicy = "RequireAuthenticated";
        auth.DefaultQueryPolicy = "RequireAuthenticated";
    },
    policies =>
    {
        policies.AddRolePolicy("AdminOnly", "Admin", "SuperAdmin");
        policies.AddClaimPolicy("SalesDepartment", "department", "sales");
        policies.AddAuthenticatedPolicy("MustBeLoggedIn");
    });
```

## Configuration

### AuthorizationConfiguration

| Property | Default | Description |
|----------|---------|-------------|
| `DefaultCommandPolicy` | `"RequireAuthenticated"` | Default policy for `ICommand<T>` requests without explicit attributes |
| `DefaultQueryPolicy` | `"RequireAuthenticated"` | Default policy for `IQuery<T>` requests without explicit attributes |
| `AutoApplyPolicies` | `false` | When `true`, automatically applies CQRS-type default policies |
| `RequireAuthenticationByDefault` | `true` | Rejects unauthenticated requests when auto-apply is enabled |

Both `DefaultCommandPolicy` and `DefaultQueryPolicy` default to `"RequireAuthenticated"` following the **secure-by-default** principle. Override only when you have a specific need (e.g., allowing anonymous queries).

### Secure Defaults

```csharp
// All commands and queries require authentication by default.
// Only opt-out explicitly with [AllowAnonymous]:
[AllowAnonymous]
public record GetPublicStatusQuery : IQuery<ServiceStatus>;
```

## Authorization Modes

### 1. Standard ASP.NET Core Attributes

Use the native `[Authorize]` attribute on request types. The `AuthorizationPipelineBehavior` enforces them automatically:

```csharp
// Requires authentication
[Authorize]
public record GetProfileQuery : IQuery<UserProfile>;

// Requires Admin role
[Authorize(Roles = "Admin")]
public record DeleteUserCommand(int UserId) : ICommand<Unit>;

// Requires custom policy
[Authorize(Policy = "RequireElevation")]
public record TransferFundsCommand(decimal Amount) : ICommand<Receipt>;

// Multiple attributes (AND logic - all must pass)
[Authorize(Roles = "Admin")]
[Authorize(Policy = "RequireApproval")]
public record DeleteTenantCommand(int TenantId) : ICommand<Unit>;
```

### 2. CQRS-Aware Default Policies

When `AutoApplyPolicies = true`, requests without explicit `[Authorize]` or `[AllowAnonymous]` attributes automatically receive a default policy based on their CQRS type:

- **Commands** (`ICommand<T>`): `DefaultCommandPolicy` is applied
- **Queries** (`IQuery<T>`): `DefaultQueryPolicy` is applied

```csharp
// With AutoApplyPolicies = true:

// This command has no attributes, so DefaultCommandPolicy applies automatically
public record CreateOrderCommand(string Product) : ICommand<OrderId>;

// This query has no attributes, so DefaultQueryPolicy applies automatically
public record GetOrdersQuery : IQuery<List<Order>>;

// This query opts out — [AllowAnonymous] bypasses all authorization
[AllowAnonymous]
public record GetPublicCatalogQuery : IQuery<Catalog>;
```

### 3. Resource-Based Authorization

Use `[ResourceAuthorize]` when the authorization decision depends on the request data (e.g., ownership checks). The request object is passed as the **resource** to ASP.NET Core's `IAuthorizationService.AuthorizeAsync(user, resource, policy)`:

```csharp
// Step 1: Mark the command for resource-based authorization
[ResourceAuthorize("CanEditOrder")]
public record UpdateOrderCommand(OrderId Id, string NewStatus) : ICommand<Order>;

// Step 2: Register the policy with a custom requirement
builder.Services.AddEncinaAuthorization(configurePolicies: policies =>
{
    policies.AddPolicy("CanEditOrder", p =>
        p.Requirements.Add(new OrderOwnerRequirement()));
});

// Step 3: Implement the authorization handler
public class OrderOwnerHandler
    : AuthorizationHandler<OrderOwnerRequirement, UpdateOrderCommand>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OrderOwnerRequirement requirement,
        UpdateOrderCommand resource)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == resource.OwnerId)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

// Step 4: Register the handler
builder.Services.AddSingleton<IAuthorizationHandler, OrderOwnerHandler>();
```

`[ResourceAuthorize]` can be combined with `[Authorize]` — both are checked (AND logic).

### 4. IResourceAuthorizer (In-Handler Authorization)

For scenarios where authorization depends on data loaded from the database (not available at attribute time), inject `IResourceAuthorizer` into your handler:

```csharp
public class UpdateOrderHandler : ICommandHandler<UpdateOrderCommand, Order>
{
    private readonly IResourceAuthorizer _authorizer;
    private readonly IOrderRepository _orders;

    public UpdateOrderHandler(IResourceAuthorizer authorizer, IOrderRepository orders)
    {
        _authorizer = authorizer;
        _orders = orders;
    }

    public async Task<Either<EncinaError, Order>> Handle(
        UpdateOrderCommand command, CancellationToken ct)
    {
        var order = await _orders.GetByIdAsync(command.OrderId, ct);

        // Authorize against the loaded resource
        var authResult = await _authorizer.AuthorizeAsync(order, "CanEditOrder", ct);
        if (authResult.IsLeft)
            return authResult.Map<Order>(_ => default!);

        // Proceed with update...
        order.UpdateStatus(command.NewStatus);
        return order;
    }
}
```

`IResourceAuthorizer` is a thin facade over `IAuthorizationService` that converts `AuthorizationResult` into `Either<EncinaError, bool>` for seamless ROP integration.

## Policy Helper Extensions

Convenience methods for registering common ASP.NET Core policies. These are thin wrappers over `AuthorizationOptions` — they create standard policies, not a parallel system:

```csharp
builder.Services.AddEncinaAuthorization(configurePolicies: policies =>
{
    // Role policy (OR semantics — any role satisfies)
    policies.AddRolePolicy("CanManageOrders", "Admin", "OrderManager");

    // Claim policy (requires specific claim type and value)
    policies.AddClaimPolicy("SalesDepartment", "department", "sales", "marketing");

    // Claim existence policy (any value)
    policies.AddClaimPolicy("HasEmail", "email");

    // Authentication-only policy
    policies.AddAuthenticatedPolicy("MustBeLoggedIn");

    // Standard ASP.NET Core policy builder (full flexibility)
    policies.AddPolicy("CanEditOrder", p =>
        p.Requirements.Add(new OrderOwnerRequirement()));
});
```

## Error Codes

All authorization errors use structured `EncinaErrorCodes` constants and include detailed metadata:

| Scenario | Error Code | HTTP Status |
|----------|-----------|-------------|
| No authenticated user | `encina.authorization.unauthorized` | 401 |
| User lacks required role | `encina.authorization.forbidden` | 403 |
| Policy not satisfied | `encina.authorization.policy_failed` | 403 |
| Resource authorization denied | `encina.authorization.resource_denied` | 403 |

Every error includes structured metadata via `GetDetails()`:

```csharp
result.IfLeft(error =>
{
    var code = error.GetCode();     // Option<string>
    var details = error.GetDetails(); // IReadOnlyDictionary<string, object?>
    // details contains: requestType, stage, policy, userId, failureReasons
});
```

## Pipeline Flow

The `AuthorizationPipelineBehavior` processes authorization in this order:

```
1. [AllowAnonymous]?         → Skip all authorization, proceed
2. Collect attributes        → [Authorize], [ResourceAuthorize]
3. CQRS auto-apply?          → Apply DefaultCommandPolicy/DefaultQueryPolicy if no attributes
4. No authorization needed?  → Proceed
5. HTTP context available?   → Left(Unauthorized) if missing
6. User authenticated?       → Left(Unauthorized) if not
7. [Authorize] attributes    → Check policies and roles (AND logic)
8. [ResourceAuthorize]       → Check resource-based policy (request as resource)
9. Auto-applied policy       → Check CQRS default policy
10. All passed               → Proceed to handler
```

## Registration

```csharp
// Minimal registration (secure defaults)
builder.Services.AddEncinaAuthorization();

// Full configuration
builder.Services.AddEncinaAuthorization(
    auth =>
    {
        auth.AutoApplyPolicies = true;
        auth.DefaultCommandPolicy = "RequireAuthenticated";
        auth.DefaultQueryPolicy = "RequireAuthenticated";
        auth.RequireAuthenticationByDefault = true;
    },
    policies =>
    {
        policies.AddRolePolicy("AdminOnly", "Admin");
        policies.AddClaimPolicy("SalesDept", "department", "sales");
    });
```

This registers:
- `AuthorizationConfiguration` via `IOptions<AuthorizationConfiguration>`
- `IResourceAuthorizer` as a scoped service
- A `"RequireAuthenticated"` policy (if not already registered)
- ASP.NET Core's `IAuthorizationService` and `IHttpContextAccessor`

## Testing

### Unit Testing with Mocks

```csharp
// Mock IResourceAuthorizer for handler tests
var authorizer = Substitute.For<IResourceAuthorizer>();
authorizer
    .AuthorizeAsync(Arg.Any<Order>(), "CanEditOrder", Arg.Any<CancellationToken>())
    .Returns(Right<EncinaError, bool>(true));

var handler = new UpdateOrderHandler(authorizer, orderRepo);
```

### Integration Testing with Real Infrastructure

```csharp
var services = new ServiceCollection();
services.AddLogging();
services.AddHttpContextAccessor();
services.AddEncinaAuthorization(
    auth => { auth.AutoApplyPolicies = true; },
    policies => { policies.AddRolePolicy("Admin", "Admin"); });

var provider = services.BuildServiceProvider();
var authorizer = provider.GetRequiredService<IResourceAuthorizer>();
```

## Related Documentation

- [ASP.NET Core Authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/introduction) — Microsoft's official authorization documentation
- [Resource-Based Authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/resourcebased) — AuthorizationHandler with resources
- [Policy-Based Authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies) — Custom policies and requirements
