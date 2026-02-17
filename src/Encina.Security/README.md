# Encina.Security

[![NuGet](https://img.shields.io/nuget/v/Encina.Security.svg)](https://www.nuget.org/packages/Encina.Security/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](../../LICENSE)

Transport-agnostic security abstractions and pipeline behavior for Encina. Provides declarative, attribute-based authorization at the CQRS pipeline level, ensuring consistent enforcement across HTTP, messaging, gRPC, and serverless transports.

## Features

- **Declarative Security** - 7 composable security attributes for authentication, roles, permissions, claims, and ownership
- **Transport-Agnostic** - Same security rules apply regardless of entry point (HTTP, messaging, gRPC, serverless)
- **Railway Oriented Programming** - Authorization failures return `EncinaError`, no exceptions
- **Extensible Evaluators** - Plug in custom permission and ownership evaluators (OPA, Casbin, database-backed)
- **Full Observability** - OpenTelemetry tracing, 4 metric instruments, 5 structured log events
- **Health Check** - Optional DI verification health check
- **.NET 10 Compatible** - Built with latest C# features

## Installation

```bash
dotnet add package Encina.Security
```

## Quick Start

### 1. Register Services

```csharp
services.AddEncinaSecurity(options =>
{
    options.RequireAuthenticatedByDefault = true;
    options.AddHealthCheck = true;
});
```

### 2. Decorate Request Types

```csharp
// Public endpoint - no security
[AllowAnonymous]
public sealed record GetPublicCatalogQuery() : IQuery<CatalogDto>;

// Requires authentication + specific permission
[DenyAnonymous]
[RequirePermission("orders:read")]
public sealed record GetOrderQuery(Guid OrderId) : IQuery<OrderDto>;

// Requires authentication + role + ownership
[DenyAnonymous]
[RequireRole("Manager", "Admin")]
[RequireOwnership("OwnerId")]
public sealed record UpdateOrderCommand(Guid Id, string OwnerId, OrderData Data) : ICommand;
```

### 3. Set Security Context

```csharp
// In ASP.NET Core middleware
app.Use(async (httpContext, next) =>
{
    var accessor = httpContext.RequestServices
        .GetRequiredService<ISecurityContextAccessor>();
    accessor.SecurityContext = new SecurityContext(httpContext.User);
    await next();
});
```

## Security Attributes

| Attribute | Description | Logic |
|-----------|-------------|-------|
| `[AllowAnonymous]` | Bypasses all security checks | Bypass |
| `[DenyAnonymous]` | Requires authenticated identity | Gate |
| `[RequireRole("A", "B")]` | User needs any listed role | OR |
| `[RequireAllRoles("A", "B")]` | User needs all listed roles | AND |
| `[RequirePermission("x:y")]` | Permission via `IPermissionEvaluator` | OR (or AND with `RequireAll`) |
| `[RequireClaim("type", "value")]` | Claim existence or exact match | Match |
| `[RequireOwnership("Prop")]` | Resource ownership via `IResourceOwnershipEvaluator` | Match |

## Custom Evaluators

Register custom evaluators before `AddEncinaSecurity()` to override defaults (TryAdd semantics):

```csharp
// Custom permission evaluator (e.g., database-backed)
services.AddScoped<IPermissionEvaluator, DatabasePermissionEvaluator>();
services.AddEncinaSecurity(); // Won't override your registration
```

## Error Codes

| Code | Meaning |
|------|---------|
| `security.unauthenticated` | User is not authenticated |
| `security.insufficient_roles` | User lacks required roles |
| `security.permission_denied` | User lacks required permissions |
| `security.claim_missing` | Required claim missing or wrong value |
| `security.not_owner` | User is not the resource owner |
| `security.missing_context` | Security context not available |

## Observability

- **Tracing**: `Encina.Security` ActivitySource with outcome/denial tags
- **Metrics**: `security.authorization.total`, `.allowed`, `.denied` (counters), `.duration` (histogram)
- **Logging**: EventId 8000-8004 via `LoggerMessage.Define` (zero-allocation)

## Documentation

- [Security Authorization Guide](../../docs/features/security-authorization.md) - Complete feature documentation
- [API Reference](https://docs.encina.dev/api/Encina.Security) - Full API documentation

## Related Packages

| Package | Description |
|---------|-------------|
| `Encina.Security.Audit` | Audit trail logging with multi-provider support |
| `Encina.AspNetCore` | ASP.NET Core integration with HTTP-level authorization |

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.
