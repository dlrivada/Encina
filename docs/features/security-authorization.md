# Security Authorization in Encina

This guide explains how to enforce security requirements declaratively at the CQRS pipeline level using the `Encina.Security` package. Authorization operates independently of the transport layer (HTTP, messaging, gRPC, serverless), ensuring consistent enforcement across all entry points.

## Table of Contents

1. [Overview](#overview)
2. [The Problem](#the-problem)
3. [The Solution](#the-solution)
4. [Quick Start](#quick-start)
5. [Security Attributes](#security-attributes)
6. [Security Context](#security-context)
7. [Custom Evaluators](#custom-evaluators)
8. [Configuration Options](#configuration-options)
9. [Observability](#observability)
10. [Health Check](#health-check)
11. [Error Handling](#error-handling)
12. [Best Practices](#best-practices)
13. [Testing](#testing)
14. [FAQ](#faq)

---

## Overview

Encina.Security provides attribute-based, transport-agnostic authorization at the CQRS pipeline level:

| Component | Description |
|-----------|-------------|
| **Security Attributes** | Declarative authorization requirements on request types |
| **SecurityPipelineBehavior** | Pipeline behavior that evaluates attributes and short-circuits on failure |
| **SecurityContext** | Immutable per-request security state (identity, roles, permissions) |
| **Permission/Ownership Evaluators** | Extensible evaluation of permissions and resource ownership |
| **SecurityOptions** | Configuration for claim types, default policies, health checks |

### Why Transport-Agnostic Security?

| Benefit | Description |
|---------|-------------|
| **Consistent enforcement** | Same security rules apply whether the request comes via HTTP, message queue, gRPC, or serverless trigger |
| **Declarative** | Security requirements live with the request type, not scattered across controllers or middleware |
| **Composable** | Combine multiple attributes for layered security (authentication + role + permission + ownership) |
| **Extensible** | Plug in custom evaluators for external auth systems (OPA, Casbin, Azure AD) |
| **Observable** | Built-in OpenTelemetry tracing, metrics, and structured logging |

---

## The Problem

Traditional ASP.NET Core authorization is coupled to the HTTP pipeline:

```csharp
// Problem 1: Security lives in the controller, not with the request
[Authorize(Roles = "Admin")]
[HttpGet("orders/{id}")]
public async Task<IActionResult> GetOrder(Guid id)
{
    // What about the same logic via message queue?
    // What about gRPC? Serverless?
}

// Problem 2: Duplicate security logic across transports
public class OrderMessageHandler
{
    public async Task Handle(GetOrderMessage message)
    {
        // Must re-implement the same role check here
        if (!user.IsInRole("Admin")) throw new UnauthorizedException();
    }
}
```

---

## The Solution

With Encina.Security, authorization lives with the request type and is enforced automatically by the pipeline:

```csharp
// Security declaration lives with the request - enforced everywhere
[DenyAnonymous]
[RequirePermission("orders:read")]
public sealed record GetOrderQuery(Guid OrderId) : IQuery<OrderDto>;

// Works the same via HTTP, messaging, gRPC, or serverless
// No duplicate security logic needed
```

---

## Quick Start

### 1. Install the Package

```bash
dotnet add package Encina.Security
```

### 2. Register Services

```csharp
services.AddEncinaSecurity(options =>
{
    options.RequireAuthenticatedByDefault = false; // default
    options.AddHealthCheck = true;                 // optional
});
```

### 3. Decorate Request Types

```csharp
// Public endpoint - no security
[AllowAnonymous]
public sealed record GetPublicCatalogQuery() : IQuery<CatalogDto>;

// Requires authentication
[DenyAnonymous]
public sealed record GetProfileQuery() : IQuery<ProfileDto>;

// Requires specific role
[DenyAnonymous]
[RequireRole("Admin", "Manager")]
public sealed record ApproveOrderCommand(Guid OrderId) : ICommand;

// Requires specific permission
[DenyAnonymous]
[RequirePermission("orders:create")]
public sealed record CreateOrderCommand(OrderData Data) : ICommand;

// Requires resource ownership
[DenyAnonymous]
[RequireOwnership("OwnerId")]
public sealed record UpdateOrderCommand(Guid OrderId, string OwnerId) : ICommand;
```

### 4. Set Security Context (ASP.NET Core Example)

```csharp
// In middleware or pipeline setup
app.Use(async (httpContext, next) =>
{
    var accessor = httpContext.RequestServices.GetRequiredService<ISecurityContextAccessor>();
    accessor.SecurityContext = new SecurityContext(httpContext.User);
    await next();
});
```

---

## Security Attributes

### Evaluation Order

Attributes are evaluated in priority order. The pipeline short-circuits on the first failure.

| Priority | Attribute | Check |
|----------|-----------|-------|
| 0 (bypass) | `[AllowAnonymous]` | Skips all checks |
| 1 | `[DenyAnonymous]` | `IsAuthenticated == true` |
| 2 | `[RequireRole]` | User has any listed role (OR) |
| 3 | `[RequireAllRoles]` | User has all listed roles (AND) |
| 4 | `[RequirePermission]` | Permission check via `IPermissionEvaluator` |
| 5 | `[RequireClaim]` | Claim existence or exact value match |
| 6 | `[RequireOwnership]` | Resource ownership via `IResourceOwnershipEvaluator` |

### Custom Order

Override default priority with the `Order` property:

```csharp
[RequirePermission("orders:read", Order = 1)]  // Check permission first
[DenyAnonymous(Order = 2)]                      // Then check authentication
public sealed record GetOrderQuery(Guid Id) : IQuery<OrderDto>;
```

### Combining Attributes

Multiple attributes can be combined freely. All must pass (AND logic between attributes):

```csharp
[DenyAnonymous]                              // Must be authenticated
[RequireRole("Manager", "Admin")]            // Must have Manager OR Admin role
[RequirePermission("orders:approve")]        // Must have orders:approve permission
[RequireOwnership("ReviewerId")]             // Must be the reviewer
public sealed record ApproveOrderCommand(Guid OrderId, string ReviewerId) : ICommand;
```

---

## Security Context

### Claims Extraction

`SecurityContext` extracts claims from `ClaimsPrincipal` using configurable claim types:

| Property | Default Claim Type | Fallback |
|----------|--------------------|----------|
| `UserId` | `sub` | `ClaimTypes.NameIdentifier` |
| `TenantId` | `tenant_id` | — |
| `Roles` | `role` | `ClaimTypes.Role` |
| `Permissions` | `permission` | — |

### Anonymous Context

```csharp
// For unauthenticated requests
var anonymous = SecurityContext.Anonymous;
// anonymous.IsAuthenticated == false
// anonymous.UserId == null
// anonymous.Roles == empty set
// anonymous.Permissions == empty set
```

---

## Custom Evaluators

### Custom Permission Evaluator

Replace the default in-memory evaluator with a database or external service:

```csharp
public sealed class DatabasePermissionEvaluator : IPermissionEvaluator
{
    private readonly IPermissionRepository _repo;

    public DatabasePermissionEvaluator(IPermissionRepository repo) => _repo = repo;

    public async ValueTask<bool> HasPermissionAsync(
        ISecurityContext context, string permission, CancellationToken ct)
    {
        if (context.UserId is null) return false;
        return await _repo.UserHasPermissionAsync(context.UserId, permission, ct);
    }

    public async ValueTask<bool> HasAnyPermissionAsync(
        ISecurityContext context, IEnumerable<string> permissions, CancellationToken ct)
    {
        if (context.UserId is null) return false;
        return await _repo.UserHasAnyPermissionAsync(context.UserId, permissions, ct);
    }

    public async ValueTask<bool> HasAllPermissionsAsync(
        ISecurityContext context, IEnumerable<string> permissions, CancellationToken ct)
    {
        if (context.UserId is null) return false;
        return await _repo.UserHasAllPermissionsAsync(context.UserId, permissions, ct);
    }
}

// Register BEFORE AddEncinaSecurity (TryAdd won't override)
services.AddScoped<IPermissionEvaluator, DatabasePermissionEvaluator>();
services.AddEncinaSecurity();
```

### Custom Ownership Evaluator

```csharp
public sealed class TeamOwnershipEvaluator : IResourceOwnershipEvaluator
{
    private readonly ITeamService _teamService;

    public TeamOwnershipEvaluator(ITeamService teamService) => _teamService = teamService;

    public async ValueTask<bool> IsOwnerAsync<TResource>(
        ISecurityContext context, TResource resource, string propertyName, CancellationToken ct)
    {
        // Support team-based ownership
        var ownerId = typeof(TResource).GetProperty(propertyName)?.GetValue(resource)?.ToString();
        if (context.UserId is null || ownerId is null) return false;

        // Direct ownership
        if (context.UserId == ownerId) return true;

        // Team ownership: check if user is in the same team as the owner
        return await _teamService.AreInSameTeamAsync(context.UserId, ownerId, ct);
    }
}
```

---

## Configuration Options

```csharp
services.AddEncinaSecurity(options =>
{
    // Require authentication for all requests (even without security attributes)
    // Use [AllowAnonymous] to opt out individual requests
    options.RequireAuthenticatedByDefault = true;

    // Throw error when SecurityContext is not available
    // (useful to catch misconfigured middleware)
    options.ThrowOnMissingSecurityContext = true;

    // Register health check
    options.AddHealthCheck = true;

    // Customize claim types
    options.UserIdClaimType = "sub";           // default
    options.RoleClaimType = "role";            // default
    options.PermissionClaimType = "permission"; // default
    options.TenantIdClaimType = "tenant_id";   // default
});
```

---

## Observability

### OpenTelemetry Tracing

Activity source: `Encina.Security`

| Tag | Description |
|-----|-------------|
| `security.request_type` | Name of the request type being authorized |
| `security.user_id` | User ID from security context |
| `security.outcome` | `allowed` or `denied` |
| `security.denial_reason` | Error code when denied |

Each attribute evaluation emits an activity event: `{AttributeType}.evaluated`.

### Metrics

Meter: `Encina.Security`

| Instrument | Type | Description |
|------------|------|-------------|
| `security.authorization.total` | Counter | Total authorization evaluations |
| `security.authorization.allowed` | Counter | Allowed evaluations |
| `security.authorization.denied` | Counter | Denied evaluations |
| `security.authorization.duration` | Histogram (ms) | Duration of authorization evaluation |

### Structured Logging

| EventId | Level | Message |
|---------|-------|---------|
| 8000 | Debug | Authorization started |
| 8001 | Information | Authorization allowed |
| 8002 | Warning | Authorization denied |
| 8003 | Debug | AllowAnonymous bypass |
| 8004 | Warning | Missing security context |

All log messages use `LoggerMessage.Define` for zero-allocation structured logging.

---

## Health Check

Enable via `SecurityOptions.AddHealthCheck = true`. The health check verifies that all core security services are registered and resolvable from DI:

- `ISecurityContextAccessor`
- `IPermissionEvaluator`
- `IResourceOwnershipEvaluator`

```text
GET /health
{
  "status": "Healthy",
  "checks": {
    "encina-security": {
      "status": "Healthy",
      "description": "All security services are registered and resolvable.",
      "tags": ["encina", "security", "ready"]
    }
  }
}
```

---

## Error Handling

Authorization failures return `EncinaError` via Railway Oriented Programming (no exceptions):

| Error Code | Meaning |
|------------|---------|
| `security.unauthenticated` | User is not authenticated |
| `security.insufficient_roles` | User lacks required roles |
| `security.permission_denied` | User lacks required permissions |
| `security.claim_missing` | Required claim is missing or has wrong value |
| `security.not_owner` | User is not the resource owner |
| `security.missing_context` | Security context not available |

All errors include structured metadata: `requestType`, `stage`, `userId`, `requirement`.

---

## Best Practices

1. **Declare security with the request, not the handler**: Put attributes on the request type, not in handler logic
2. **Register custom evaluators before `AddEncinaSecurity()`**: `TryAdd` semantics mean your registrations take precedence
3. **Use `RequireAuthenticatedByDefault = true`** in production: Opt out public endpoints with `[AllowAnonymous]`
4. **Prefer `[RequirePermission]` over `[RequireRole]`**: Permissions are more granular and easier to manage at scale
5. **Use `[RequireOwnership]` for resource-level access control**: Ensures users can only access their own resources
6. **Monitor `security.authorization.denied` metric**: Alerts on unusual denial patterns can indicate attacks

---

## Testing

Use `FakeLogger<T>` and mock evaluators for unit testing:

```csharp
// Arrange
var accessor = Substitute.For<ISecurityContextAccessor>();
accessor.SecurityContext.Returns(new SecurityContext(authenticatedPrincipal));

var behavior = new SecurityPipelineBehavior<MyCommand, Unit>(
    accessor,
    Substitute.For<IPermissionEvaluator>(),
    Substitute.For<IResourceOwnershipEvaluator>(),
    Options.Create(new SecurityOptions()),
    new FakeLogger<SecurityPipelineBehavior<MyCommand, Unit>>());

// Act
var result = await behavior.Handle(command, context, next, ct);

// Assert
result.IsRight.Should().BeTrue(); // Allowed
```

---

## FAQ

### How does this relate to ASP.NET Core `[Authorize]`?

`Encina.Security` operates at the CQRS pipeline level, not the HTTP middleware level. This means the same security rules apply regardless of whether the request comes from an HTTP controller, a message queue consumer, a gRPC service, or a serverless function trigger.

### Can I use both ASP.NET Core authorization and Encina.Security?

Yes. ASP.NET Core authorization runs at the HTTP middleware level (before the request reaches your handlers), while Encina.Security runs inside the CQRS pipeline. They complement each other.

### What happens when no security attributes are present?

By default, requests without security attributes pass through without checks. Set `RequireAuthenticatedByDefault = true` to require authentication for all requests, then use `[AllowAnonymous]` to opt out specific request types.

### How do I handle admin users who bypass ownership checks?

Combine attributes with role checks. The current implementation evaluates all attributes (AND logic between attributes), so you would need a custom evaluator or a separate admin-specific request type. Future versions may support attribute-level bypass policies.
