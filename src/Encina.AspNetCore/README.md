# Encina.AspNetCore

[![NuGet](https://img.shields.io/nuget/v/Encina.AspNetCore.svg)](https://www.nuget.org/packages/Encina.AspNetCore/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](../../LICENSE)

ASP.NET Core integration for Encina with Railway Oriented Programming support. This package provides seamless integration between Encina and ASP.NET Core, enabling request context enrichment, authorization, and standardized error handling.

## Features

- ✅ **Request Context Enrichment** - Automatic extraction of CorrelationId, UserId, TenantId, and IdempotencyKey from HttpContext
- ✅ **Authorization Pipeline Behavior** - CQRS-aware declarative authorization with `[Authorize]`, `[ResourceAuthorize]`, and auto-applied default policies
- ✅ **RFC 7807 Problem Details** - Intelligent error mapping from `EncinaError` to standardized HTTP responses
- ✅ **Thread-Safe Context Access** - AsyncLocal-based `IRequestContextAccessor` for safe context propagation
- ✅ **Distributed Tracing** - Automatic correlation ID propagation and Activity integration
- ✅ **.NET 10 Compatible** - Built with latest ASP.NET Core APIs

## Installation

```bash
dotnet add package Encina.AspNetCore
```

## Quick Start

### 1. Register Services

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register Encina with ASP.NET Core integration
builder.Services.AddEncina(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
}, typeof(Program).Assembly);

builder.Services.AddEncinaAspNetCore(options =>
{
    // Optional: Customize header names
    options.CorrelationIdHeader = "X-Correlation-ID"; // Default
    options.TenantIdHeader = "X-Tenant-ID"; // Default
    options.IdempotencyKeyHeader = "X-Idempotency-Key"; // Default

    // Optional: Include request path in Problem Details
    options.IncludeRequestPathInProblemDetails = false; // Default

    // Optional: Include exception details (only in Development by default)
    options.IncludeExceptionDetails = false;
});

// Authorization with CQRS-aware defaults (replaces plain AddAuthorization)
builder.Services.AddEncinaAuthorization(
    auth =>
    {
        auth.AutoApplyPolicies = true; // Auto-apply default policies to commands/queries
    },
    policies =>
    {
        policies.AddRolePolicy("AdminOnly", "Admin");
    });
```

### 2. Configure Middleware Pipeline

```csharp
var app = builder.Build();

app.UseAuthentication(); // Must come before UseEncinaContext
app.UseEncinaContext(); // Enriches IRequestContext from HttpContext
app.UseAuthorization();

app.MapControllers();
app.Run();
```

### 3. Use in Minimal APIs

```csharp
app.MapPost("/api/users", async (CreateUserCommand command, IEncina Encina, HttpContext httpContext) =>
{
    var result = await Encina.Send(command);

    return result.Match(
        Right: user => Results.Created($"/api/users/{user.Id}", user),
        Left: error => error.ToProblemDetails(httpContext)
    );
});
```

### 4. Use in Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IEncina _Encina;

    public UsersController(IEncina Encina)
    {
        _Encina = Encina;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserCommand command)
    {
        var result = await _Encina.Send(command);

        return result.Match(
            Right: user => Created($"/api/users/{user.Id}", user),
            Left: error => error.ToActionResult(HttpContext)
        );
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var result = await _Encina.Send(new GetUserQuery(id));

        return result.Match(
            Right: user => Ok(user),
            Left: error => error.ToActionResult(HttpContext)
        );
    }
}
```

## Core Components

### 1. Request Context Middleware

The `EncinaContextMiddleware` automatically enriches `IRequestContext` from the incoming HTTP request:

**Extracted Values:**

- **CorrelationId**: From `X-Correlation-ID` header, `Activity.Current.Id`, or auto-generated GUID
- **UserId**: From `ClaimsPrincipal` (ClaimTypes.NameIdentifier by default)
- **TenantId**: From `tenant_id` claim or `X-Tenant-ID` header
- **IdempotencyKey**: From `X-Idempotency-Key` header

**Automatic Features:**

- Sets correlation ID in response headers
- Integrates with distributed tracing (Activity)
- Thread-safe context propagation via AsyncLocal

**Example:**

```csharp
public class CreateOrderHandler : ICommandHandler<CreateOrderCommand, Order>
{
    private readonly IRequestContextAccessor _contextAccessor;

    public CreateOrderHandler(IRequestContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor;
    }

    public async ValueTask<Either<EncinaError, Order>> Handle(
        CreateOrderCommand request,
        IRequestContext context,
        CancellationToken cancellationToken)
    {
        // Access context from anywhere in the handler
        var userId = context.UserId; // From authenticated user
        var tenantId = context.TenantId; // For multi-tenancy
        var correlationId = context.CorrelationId; // For tracing

        // ... handler logic
    }
}
```

### 2. Authorization Behavior

The `AuthorizationPipelineBehavior` enforces declarative authorization on requests using ASP.NET Core's native authorization system. It supports standard attributes, CQRS-aware default policies, and resource-based authorization.

**Supported Authorization Types:**

- **Authentication Required**: `[Authorize]`
- **Role-Based**: `[Authorize(Roles = "Admin")]` or `[Authorize(Roles = "Admin,Manager")]`
- **Policy-Based**: `[Authorize(Policy = "RequireElevation")]`
- **Resource-Based**: `[ResourceAuthorize("CanEditOrder")]` — request is passed as the resource
- **Multiple Attributes**: Combine multiple `[Authorize]` attributes (all must pass)
- **CQRS Defaults**: Auto-apply `DefaultCommandPolicy`/`DefaultQueryPolicy` when no attributes present
- **Allow Anonymous**: `[AllowAnonymous]` bypasses all authorization

**Example:**

```csharp
// Requires authentication
[Authorize]
public record GetProfileQuery : IQuery<UserProfile>;

// Requires Admin role
[Authorize(Roles = "Admin")]
public record DeleteUserCommand(int UserId) : ICommand<Unit>;

// Requires custom policy
[Authorize(Policy = "RequireElevation")]
public record PromoteUserCommand(int UserId, string NewRole) : ICommand<Unit>;

// Resource-based authorization (request is the resource)
[ResourceAuthorize("CanEditOrder")]
public record UpdateOrderCommand(OrderId Id, string NewStatus) : ICommand<Order>;

// With AutoApplyPolicies=true, this command gets DefaultCommandPolicy automatically
public record CreateItemCommand(string Name) : ICommand<ItemId>;

// Opt-out of all authorization
[AllowAnonymous]
public record GetPublicStatusQuery : IQuery<ServiceStatus>;
```

**Error Codes:**

| Scenario | Error Code | HTTP Status |
|----------|-----------|-------------|
| Not authenticated | `encina.authorization.unauthorized` | 401 |
| Missing roles | `encina.authorization.forbidden` | 403 |
| Policy failed | `encina.authorization.policy_failed` | 403 |
| Resource authorization denied | `encina.authorization.resource_denied` | 403 |

> For full documentation including `IResourceAuthorizer`, policy helpers, and testing patterns, see [Authorization Feature Docs](../../docs/features/authorization.md).

### 3. Problem Details Extensions

Convert `EncinaError` to standardized RFC 7807 Problem Details responses:

**Intelligent Error Mapping:**

| Error Code Pattern | HTTP Status | Title |
|-------------------|-------------|-------|
| `validation.*` | 400 | Bad Request |
| `Encina.guard.validation_failed` | 400 | Bad Request |
| `encina.authorization.unauthorized` | 401 | Unauthorized |
| `encina.authorization.*` | 403 | Forbidden |
| `*.not_found` | 404 | Not Found |
| `*.missing` | 404 | Not Found |
| `Encina.request.handler_missing` | 404 | Not Found |
| `*.conflict` | 409 | Conflict |
| `*.already_exists` | 409 | Conflict |
| `*.duplicate` | 409 | Conflict |
| Default | 500 | Internal Server Error |

**Example:**

```csharp
// In Minimal API
app.MapPost("/api/orders", async (CreateOrderCommand cmd, IEncina Encina, HttpContext ctx) =>
{
    var result = await Encina.Send(cmd);

    return result.Match(
        Right: order => Results.Created($"/api/orders/{order.Id}", order),
        Left: error => error.ToProblemDetails(ctx) // Returns IResult
    );
});

// In Controller
[HttpPost]
public async Task<IActionResult> Create(CreateOrderCommand cmd)
{
    var result = await _Encina.Send(cmd);

    return result.Match(
        Right: order => Created($"/api/orders/{order.Id}", order),
        Left: error => error.ToActionResult(HttpContext) // Returns IActionResult
    );
}
```

**Generated Problem Details:**

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Email address is required",
  "traceId": "00-a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6-q7r8s9t0u1v2w3x4-01",
  "correlationId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "errorCode": "validation.email_required"
}
```

**Optional Request Path:**

```csharp
services.AddEncinaAspNetCore(options =>
{
    options.IncludeRequestPathInProblemDetails = true;
});
```

This adds the `instance` field:

```json
{
  "instance": "/api/users",
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "User not found"
}
```

**Development Mode Exception Details:**

In Development environment, exception details are automatically included:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "An error occurred while processing the request",
  "exception": {
    "type": "System.InvalidOperationException",
    "message": "Database connection failed",
    "stackTrace": "at MyApp.OrderHandler.Handle(...)"
  }
}
```

### 4. Request Context Accessor

Thread-safe access to `IRequestContext` anywhere in your application:

```csharp
public class AuditService
{
    private readonly IRequestContextAccessor _contextAccessor;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IRequestContextAccessor contextAccessor, ILogger<AuditService> logger)
    {
        _contextAccessor = contextAccessor;
        _logger = logger;
    }

    public void LogAction(string action)
    {
        var context = _contextAccessor.RequestContext;

        _logger.LogInformation(
            "User {UserId} performed {Action} (CorrelationId: {CorrelationId})",
            context?.UserId ?? "anonymous",
            action,
            context?.CorrelationId);
    }
}
```

**Key Features:**

- Uses `AsyncLocal<T>` for safe context propagation across async calls
- Isolated between concurrent requests
- Null when accessed outside of request context

## Configuration Options

### EncinaAspNetCoreOptions

```csharp
services.AddEncinaAspNetCore(options =>
{
    // Header name for correlation ID (default: "X-Correlation-ID")
    options.CorrelationIdHeader = "X-Request-ID";

    // Header name for tenant ID (default: "X-Tenant-ID")
    options.TenantIdHeader = "X-Customer-ID";

    // Header name for idempotency key (default: "X-Idempotency-Key")
    options.IdempotencyKeyHeader = "Idempotency-Key";

    // Claim type for user ID (default: ClaimTypes.NameIdentifier)
    options.UserIdClaimType = "sub"; // OIDC standard

    // Claim type for tenant ID (default: "tenant_id")
    options.TenantIdClaimType = "tid"; // Azure AD

    // Include request path in Problem Details (default: false)
    options.IncludeRequestPathInProblemDetails = true;

    // Include exception details in Problem Details (default: false, auto in Development)
    options.IncludeExceptionDetails = false;
});
```

## Custom Status Codes

Override the intelligent mapping for specific errors:

```csharp
app.MapDelete("/api/users/{id}", async (int id, IEncina Encina, HttpContext ctx) =>
{
    var result = await Encina.Send(new DeleteUserCommand(id));

    return result.Match(
        Right: _ => Results.NoContent(),
        Left: error => error.ToProblemDetails(ctx, statusCode: 410) // Custom: 410 Gone
    );
});
```

## Multi-Tenancy Example

```csharp
// The middleware automatically extracts tenant ID from claims or headers
public class GetOrdersQueryHandler : IQueryHandler<GetOrdersQuery, IEnumerable<Order>>
{
    private readonly IOrderRepository _repository;

    public async ValueTask<Either<EncinaError, IEnumerable<Order>>> Handle(
        GetOrdersQuery request,
        IRequestContext context,
        CancellationToken cancellationToken)
    {
        // context.TenantId is automatically populated from HttpContext
        if (context.TenantId == null)
            return EncinaErrors.Create("tenant.missing", "Tenant ID is required");

        var orders = await _repository.GetByTenantAsync(context.TenantId, cancellationToken);
        return orders;
    }
}
```

## Idempotency Example

```csharp
// Client sends idempotency key in header
// X-Idempotency-Key: 550e8400-e29b-41d4-a716-446655440000

public class ChargeCustomerHandler : ICommandHandler<ChargeCustomerCommand, Receipt>
{
    private readonly IPaymentService _paymentService;
    private readonly IReceiptRepository _receiptRepository;

    public async ValueTask<Either<EncinaError, Receipt>> Handle(
        ChargeCustomerCommand request,
        IRequestContext context,
        CancellationToken cancellationToken)
    {
        // Check if already processed (implement your own caching/storage)
        if (context.IdempotencyKey != null)
        {
            var existingReceipt = await _receiptRepository
                .GetByIdempotencyKeyAsync(context.IdempotencyKey, cancellationToken);

            if (existingReceipt != null)
                return existingReceipt; // Return cached result
        }

        // Process payment
        var receipt = await _paymentService.ChargeAsync(request.Amount, cancellationToken);

        // Store with idempotency key
        if (context.IdempotencyKey != null)
            await _receiptRepository.SaveAsync(receipt, context.IdempotencyKey, cancellationToken);

        return receipt;
    }
}
```

## Error Handling Best Practices

### 1. Domain-Specific Error Codes

Use consistent error code patterns:

```csharp
// Validation errors
EncinaErrors.Create("validation.email_invalid", "Email address is not valid");
EncinaErrors.Create("validation.password_weak", "Password must be at least 8 characters");

// Not found errors
EncinaErrors.Create("user.not_found", $"User with ID {id} was not found");
EncinaErrors.Create("order.not_found", $"Order with ID {id} was not found");

// Conflict errors
EncinaErrors.Create("user.already_exists", $"User with email {email} already exists");
EncinaErrors.Create("order.already_shipped", "Cannot modify order that has been shipped");

// Authorization errors (handled by library via EncinaErrorCodes constants)
EncinaErrors.Forbidden("RequireElevation"); // encina.authorization.forbidden
EncinaErrors.Unauthorized();                // encina.authorization.unauthorized
```

### 2. Rich Error Details

Add structured metadata for client consumption:

```csharp
return EncinaErrors.Create(
    code: "validation.failed",
    message: "Request validation failed",
    metadata: new Dictionary<string, object?>
    {
        ["validationErrors"] = new[]
        {
            new { Field = "Email", Error = "Email is required" },
            new { Field = "Password", Error = "Password must be at least 8 characters" }
        }
    });
```

This metadata is automatically included in the Problem Details response.

## Integration with Other Packages

Encina.AspNetCore works seamlessly with other Encina satellite packages:

### With FluentValidation

```csharp
builder.Services.AddEncina(cfg =>
{
    cfg.AddFluentValidation(); // Validation happens before authorization
}, assemblies);

builder.Services.AddEncinaAspNetCore();
```

Pipeline order: Validation → Authorization → Handler

### With GuardClauses

```csharp
public record CreateOrderCommand(int CustomerId, decimal Amount) : ICommand<Order>
{
    public void Validate()
    {
        Guard.Against.NegativeOrZero(CustomerId);
        Guard.Against.NegativeOrZero(Amount);
    }
}
```

Pipeline order: Guard Clauses → Validation → Authorization → Handler

## Testing

### Unit Testing Handlers

```csharp
[Fact]
public async Task Handle_ValidRequest_ReturnsUser()
{
    // Arrange
    var handler = new GetUserHandler(_mockRepository.Object);
    var context = RequestContext.CreateForTest(
        userId: "test-user",
        correlationId: "test-correlation-id"
    );

    // Act
    var result = await handler.Handle(
        new GetUserQuery(123),
        context,
        CancellationToken.None);

    // Assert
    result.IsRight.Should().BeTrue();
}
```

### Integration Testing

```csharp
public class UsersControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public UsersControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateUser_ValidCommand_Returns201()
    {
        // Arrange
        var command = new CreateUserCommand("test@example.com", "password123");
        var correlationId = Guid.NewGuid().ToString();

        _client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);

        // Act
        var response = await _client.PostAsJsonAsync("/api/users", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Should().ContainKey("X-Correlation-ID");
    }
}
```

## Performance Considerations

- **AsyncLocal Overhead**: Minimal (~10ns per access)
- **Reflection Caching**: Authorization attributes are cached via expression tree compilation
- **Zero Allocations**: Hot paths avoid allocations where possible
- **Thread-Safe**: All components are thread-safe and support high concurrency

## Module Health Checks

For applications using the Modular Monolith pattern, Encina.AspNetCore provides health check integration for modules that implement `IModuleWithHealthChecks`.

### Implementing Health Checks in Modules

```csharp
using Encina.AspNetCore.Modules;
using Encina.Messaging.Health;

public class OrdersModule : IModuleWithHealthChecks
{
    private readonly string _connectionString;

    public OrdersModule(string connectionString)
    {
        _connectionString = connectionString;
    }

    public string Name => "Orders";

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IOrderRepository, OrderRepository>();
    }

    public IEnumerable<IEncinaHealthCheck> GetHealthChecks()
    {
        yield return new OrdersDatabaseHealthCheck(_connectionString);
        yield return new OrdersQueueHealthCheck();
    }
}
```

### Registering Module Health Checks

```csharp
// Register all module health checks
builder.Services
    .AddHealthChecks()
    .AddEncinaModuleHealthChecks();

// Or register health checks for a specific module
builder.Services
    .AddHealthChecks()
    .AddEncinaModuleHealthChecks<OrdersModule>();
```

### Exposing Health Check Endpoints

```csharp
// All module health checks
app.MapHealthChecks("/health/modules", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("modules")
});

// All encina health checks (includes modules)
app.MapHealthChecks("/health/encina", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("encina")
});
```

### Default Tags

Module health checks are automatically tagged with:
- `encina` - All Encina health checks
- `ready` - Readiness probe compatible
- `modules` - Module-specific health checks

## Health Checks in Microservices

For microservices architectures, Encina provides health checks for infrastructure dependencies (databases, message brokers, caches) through its provider packages. For checking downstream service dependencies, we recommend combining Encina health checks with existing .NET ecosystem packages.

### Combining Encina with Downstream Service Checks

```csharp
// Install: dotnet add package AspNetCore.HealthChecks.Uris

builder.Services
    .AddHealthChecks()
    // Encina infrastructure health checks
    .AddEncinaHealthChecks()           // All registered IEncinaHealthCheck
    .AddEncinaOutbox()                 // Outbox pattern monitoring
    .AddEncinaSaga()                   // Saga state monitoring
    // Downstream service dependencies
    .AddUrlGroup(
        new Uri("https://orders-api/health"),
        name: "orders-service",
        tags: ["downstream", "ready"])
    .AddUrlGroup(
        new Uri("https://payments-api/health"),
        name: "payments-service",
        tags: ["downstream", "ready"])
    .AddUrlGroup(
        new Uri("https://inventory-api/health"),
        name: "inventory-service",
        tags: ["downstream", "critical"]);
```

### Kubernetes Probes Configuration

```csharp
// Liveness - Is the service alive? (only local checks)
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

// Readiness - Can the service handle traffic? (includes dependencies)
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

// Startup - Has the service started? (one-time checks)
app.MapHealthChecks("/health/startup", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("startup")
});
```

### Recommended Packages for Microservices

| Package | Purpose |
|---------|---------|
| `AspNetCore.HealthChecks.Uris` | HTTP endpoint checks for downstream services |
| `Grpc.AspNetCore.HealthChecks` | gRPC service health checks |
| `AspNetCore.HealthChecks.Kubernetes` | Kubernetes API health checks |
| `AspNetCore.HealthChecks.OpenIdConnect` | Identity provider health checks |

### Example: Complete Microservice Health Configuration

```csharp
builder.Services
    .AddHealthChecks()
    // Infrastructure (Encina providers)
    .AddEncinaHealthChecks()                          // All Encina checks
    // Database (from Encina.Dapper.PostgreSQL)
    // Automatically registered when using AddEncinaDapper()
    // Message broker (from Encina.RabbitMQ)
    // Automatically registered when using AddEncinaRabbitMQ()
    // Cache (from Encina.Caching.Redis)
    // Automatically registered when using AddEncinaRedis()
    // Downstream services
    .AddUrlGroup(new Uri("https://auth-service/health"), "auth-service", tags: ["downstream", "critical"])
    .AddUrlGroup(new Uri("https://notification-service/health"), "notification-service", tags: ["downstream"]);

// Expose endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready") || check.Tags.Contains("downstream")
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false // Empty check = always healthy if service responds
});
```

> **For a comprehensive guide** on integrating Encina health checks with the ASP.NET Core ecosystem, Kubernetes probes, and recommended NuGet packages, see [Health Checks Integration Guide](../../docs/guides/health-checks.md).

## Roadmap

This package does NOT include ORM-specific features. For data access integration, see:

- **Encina.EntityFrameworkCore** - Transaction management, outbox pattern, multi-tenancy query filters
- **Encina.Dapper** - Lightweight data access with minimal overhead
- **Encina.Data** - Pure ADO.NET for maximum performance

## Additional Resources

- [Encina Core Documentation](../../README.md)
- [Railway Oriented Programming Guide](../../docs/architecture/adr/ADR-006-Pure-ROP-with-Fail-Fast-Exception-Handling.md)
- [IRequestContext Design](../../docs/architecture/adr/ADR-007-IRequestContext-for-Extensibility.md)
- [RFC 7807 - Problem Details](https://tools.ietf.org/html/rfc7807)

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.
