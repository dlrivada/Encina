# Encina.AzureFunctions

Azure Functions integration for Encina with Railway Oriented Programming support.

## Features

- ✅ HTTP Trigger integration with automatic result-to-response conversion
- ✅ Queue Trigger support for message processing
- ✅ Timer Trigger support for scheduled operations
- ✅ Request context enrichment (correlation ID, user ID, tenant ID)
- ✅ RFC 7807 Problem Details for error responses
- ✅ Health check for monitoring
- ✅ Middleware for request context propagation

## Installation

```bash
dotnet add package Encina.AzureFunctions
```

## Quick Start

### 1. Configure Services

```csharp
var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(builder =>
    {
        builder.UseEncinaMiddleware();
    })
    .ConfigureServices(services =>
    {
        services.AddEncina(typeof(Program).Assembly);
        services.AddEncinaAzureFunctions();
    })
    .Build();

await host.RunAsync();
```

### 2. Create HTTP Trigger Functions

```csharp
public class OrderFunctions
{
    private readonly IEncina _encina;

    public OrderFunctions(IEncina encina) => _encina = encina;

    [Function("CreateOrder")]
    public async Task<HttpResponseData> CreateOrder(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders")]
        HttpRequestData req)
    {
        var command = await req.ReadFromJsonAsync<CreateOrder>();
        var result = await _encina.Send(command!);

        return await result.ToCreatedResponse(req, order => $"/orders/{order.Id}");
    }

    [Function("GetOrder")]
    public async Task<HttpResponseData> GetOrder(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "orders/{id}")]
        HttpRequestData req,
        Guid id)
    {
        var query = new GetOrderById(id);
        var result = await _encina.Send(query);

        return await result.ToHttpResponseData(req);
    }

    [Function("DeleteOrder")]
    public async Task<HttpResponseData> DeleteOrder(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "orders/{id}")]
        HttpRequestData req,
        Guid id)
    {
        var command = new DeleteOrder(id);
        var result = await _encina.Send(command);

        return await result.ToNoContentResponse(req);
    }
}
```

### 3. Create Queue Trigger Functions

```csharp
public class PaymentFunctions
{
    private readonly IEncina _encina;

    public PaymentFunctions(IEncina encina) => _encina = encina;

    [Function("ProcessPayment")]
    public async Task ProcessPayment(
        [QueueTrigger("payments")] ProcessPaymentCommand command,
        FunctionContext context)
    {
        var correlationId = context.GetCorrelationId();

        var result = await _encina.Send(command);

        result.Match(
            Right: _ => { /* Success */ },
            Left: error => throw new InvalidOperationException(error.Message)
        );
    }
}
```

### 4. Create Timer Trigger Functions

```csharp
public class MaintenanceFunctions
{
    private readonly IEncina _encina;

    public MaintenanceFunctions(IEncina encina) => _encina = encina;

    [Function("CleanupExpiredSessions")]
    public async Task CleanupExpiredSessions(
        [TimerTrigger("0 0 * * * *")] TimerInfo timer,
        FunctionContext context)
    {
        var command = new CleanupExpiredSessions();
        var result = await _encina.Send(command);

        result.Match(
            Right: count => Console.WriteLine($"Cleaned up {count} sessions"),
            Left: error => Console.WriteLine($"Cleanup failed: {error.Message}")
        );
    }
}
```

## Configuration

```csharp
services.AddEncinaAzureFunctions(options =>
{
    // Request context enrichment
    options.EnableRequestContextEnrichment = true;

    // Custom header names
    options.CorrelationIdHeader = "X-Request-ID";
    options.TenantIdHeader = "X-Tenant-ID";

    // Claims configuration
    options.UserIdClaimType = "sub";
    options.TenantIdClaimType = "tid";

    // Error responses (production-safe by default)
    options.IncludeExceptionDetailsInResponse = false;

    // Health check configuration
    options.ProviderHealthCheck.Enabled = true;
    options.ProviderHealthCheck.Name = "azure-functions";
    options.ProviderHealthCheck.Tags = ["encina", "serverless", "ready"];
});
```

## HTTP Response Extensions

### ToHttpResponseData

Converts `Either<EncinaError, T>` to an HTTP response:

```csharp
var result = await _encina.Send(query);
return await result.ToHttpResponseData(req);
```

### ToCreatedResponse

Creates a 201 Created response with Location header:

```csharp
var result = await _encina.Send(command);
return await result.ToCreatedResponse(req, order => $"/orders/{order.Id}");
```

### ToNoContentResponse

Creates a 204 No Content response for void operations:

```csharp
var result = await _encina.Send(deleteCommand);
return await result.ToNoContentResponse(req);
```

### ToProblemDetailsResponse

Converts errors to RFC 7807 Problem Details:

```csharp
if (result.IsLeft)
{
    var error = result.LeftToSeq().First();
    return await error.ToProblemDetailsResponse(req);
}
```

## Error Code to HTTP Status Mapping

| Error Code Pattern | HTTP Status |
|--------------------|-------------|
| `validation.*` | 400 Bad Request |
| `encina.guard.validation_failed` | 400 Bad Request |
| `authorization.unauthenticated` | 401 Unauthorized |
| `authorization.*` | 403 Forbidden |
| `*.not_found`, `*.missing` | 404 Not Found |
| `encina.request.handler_missing` | 404 Not Found |
| `*.conflict`, `*.already_exists`, `*.duplicate` | 409 Conflict |
| Other errors | 500 Internal Server Error |

## Context Extraction

Access context information from function context:

```csharp
public async Task MyFunction(FunctionContext context)
{
    var correlationId = context.GetCorrelationId();
    var userId = context.GetUserId();
    var tenantId = context.GetTenantId();
    var invocationId = context.GetInvocationId();
}
```

## Middleware

The Encina middleware provides:

- **Automatic correlation ID extraction/generation**
- **User ID extraction from claims**
- **Tenant ID extraction from headers or claims**
- **Structured logging for function execution**

Enable it in your configuration:

```csharp
.ConfigureFunctionsWorkerDefaults(builder =>
{
    builder.UseEncinaMiddleware();
})
```

## Health Checks

The package registers an `IEncinaHealthCheck` that verifies configuration:

```csharp
public class HealthFunction
{
    private readonly IEnumerable<IEncinaHealthCheck> _healthChecks;

    public HealthFunction(IEnumerable<IEncinaHealthCheck> healthChecks)
    {
        _healthChecks = healthChecks;
    }

    [Function("Health")]
    public async Task<HttpResponseData> Health(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")]
        HttpRequestData req)
    {
        var results = new List<object>();

        foreach (var check in _healthChecks)
        {
            var result = await check.CheckHealthAsync();
            results.Add(new { check.Name, result.Status, result.Description });
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(results);
        return response;
    }
}
```

## Integration with Other Encina Packages

### With Encina.FluentValidation

```csharp
services.AddEncina(typeof(Program).Assembly);
services.AddEncinaFluentValidation(typeof(Program).Assembly);
services.AddEncinaAzureFunctions();
```

### With Encina.EntityFrameworkCore

```csharp
services.AddEncina(typeof(Program).Assembly);
services.AddDbContext<AppDbContext>();
services.AddEncinaEntityFrameworkCore<AppDbContext>(config =>
{
    config.UseOutbox = true;
});
services.AddEncinaAzureFunctions();
```

## Best Practices

1. **Always use the middleware** for consistent context propagation
2. **Use appropriate response extensions** for each operation type
3. **Configure correlation headers** to match your infrastructure
4. **Disable exception details in production** for security
5. **Implement health check endpoints** for monitoring
6. **Use structured logging** through the ILogger interface

## Requirements

- .NET 10.0
- Azure Functions Worker SDK 2.0+

## License

MIT
