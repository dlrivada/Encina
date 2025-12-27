# Encina.AwsLambda

AWS Lambda integration for the Encina library, providing seamless serverless function execution with Railway Oriented Programming support.

## Features

- **API Gateway Integration**: Convert Encina results to API Gateway responses (REST API and HTTP API)
- **SQS Trigger Support**: Process SQS messages with batch failure handling
- **EventBridge Integration**: Handle EventBridge (CloudWatch Events) with automatic deserialization
- **RFC 7807 Problem Details**: Standardized error responses
- **Context Enrichment**: Extract correlation ID, user ID, and tenant ID from Lambda context
- **Health Checks**: Built-in health check implementation

## Installation

```bash
dotnet add package Encina.AwsLambda
```

## Quick Start

### API Gateway Handler

```csharp
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Encina;
using Encina.AwsLambda;

public class OrderFunctions
{
    private readonly IEncina _encina;

    public OrderFunctions(IEncina encina) => _encina = encina;

    public async Task<APIGatewayProxyResponse> CreateOrder(
        APIGatewayProxyRequest request,
        ILambdaContext context)
    {
        var command = JsonSerializer.Deserialize<CreateOrder>(request.Body);
        var result = await _encina.Send(command!);

        // Automatically converts Either<EncinaError, T> to API Gateway response
        return result.ToCreatedResponse(order => $"/orders/{order.Id}");
    }

    public async Task<APIGatewayProxyResponse> GetOrder(
        APIGatewayProxyRequest request,
        ILambdaContext context)
    {
        var orderId = request.PathParameters["id"];
        var result = await _encina.Send(new GetOrderById(orderId));

        return result.ToApiGatewayResponse();
    }
}
```

### SQS Handler with Batch Processing

```csharp
using Amazon.Lambda.SQSEvents;
using Encina.AwsLambda;

public class QueueProcessor
{
    private readonly IEncina _encina;
    private readonly ILogger<QueueProcessor> _logger;

    public async Task<SQSBatchResponse> ProcessOrders(
        SQSEvent sqsEvent,
        ILambdaContext context)
    {
        // Process batch with automatic partial failure reporting
        return await SqsMessageHandler.ProcessBatchAsync<ProcessOrder, OrderResult>(
            sqsEvent,
            async command => await _encina.Send(command),
            _logger);
    }
}
```

### EventBridge Handler

```csharp
using Amazon.Lambda.CloudWatchEvents;
using Encina.AwsLambda;

public class EventHandler
{
    private readonly IEncina _encina;

    public async Task HandleOrderCreated(
        CloudWatchEvent<OrderCreatedEvent> eventBridgeEvent,
        ILambdaContext context)
    {
        var result = await EventBridgeHandler.ProcessAsync(
            eventBridgeEvent,
            async detail => await _encina.Publish(
                new SendOrderConfirmation(detail.OrderId)));

        result.IfLeft(error =>
            context.Logger.LogError($"Failed: {error.Message}"));
    }
}
```

## Configuration

### Dependency Injection Setup

```csharp
// In your startup/DI configuration
var services = new ServiceCollection();

services.AddEncina(typeof(Program).Assembly);
services.AddEncinaAwsLambda(options =>
{
    options.EnableRequestContextEnrichment = true;
    options.CorrelationIdHeader = "X-Correlation-ID";
    options.EnableSqsBatchItemFailures = true;
    options.UseApiGatewayV2Format = false; // Use REST API format
});

var serviceProvider = services.BuildServiceProvider();
```

### Configuration Options

| Option | Description | Default |
|--------|-------------|---------|
| `EnableRequestContextEnrichment` | Auto-enrich context with correlation ID, user ID, tenant ID | `true` |
| `CorrelationIdHeader` | Header name for correlation ID | `X-Correlation-ID` |
| `TenantIdHeader` | Header name for tenant ID | `X-Tenant-ID` |
| `UserIdClaimType` | JWT claim type for user ID | `sub` |
| `TenantIdClaimType` | JWT claim type for tenant ID | `tenant_id` |
| `IncludeExceptionDetailsInResponse` | Include exception details in errors | `false` |
| `UseApiGatewayV2Format` | Use HTTP API (V2) response format | `false` |
| `EnableSqsBatchItemFailures` | Enable partial batch failure reporting | `true` |

## API Gateway Response Extensions

### Standard Response

```csharp
// Success: 200 OK with JSON body
// Error: Appropriate status code with Problem Details
var result = await encina.Send(query);
return result.ToApiGatewayResponse();
```

### Created Response (201)

```csharp
// Success: 201 Created with Location header
var result = await encina.Send(createCommand);
return result.ToCreatedResponse(entity => $"/api/entities/{entity.Id}");
```

### No Content Response (204)

```csharp
// Success: 204 No Content
// Error: Problem Details response
var result = await encina.Send(deleteCommand);
return result.ToNoContentResponse();
```

### HTTP API (V2) Response

```csharp
// For API Gateway HTTP APIs (not REST APIs)
var result = await encina.Send(query);
return result.ToHttpApiResponse();
```

## Error Code to HTTP Status Mapping

| Error Code Pattern | HTTP Status |
|-------------------|-------------|
| `validation.*` | 400 Bad Request |
| `encina.guard.validation_failed` | 400 Bad Request |
| `authorization.unauthenticated` | 401 Unauthorized |
| `authorization.*` | 403 Forbidden |
| `*.not_found`, `*.missing` | 404 Not Found |
| `*.conflict`, `*.already_exists`, `*.duplicate` | 409 Conflict |
| (default) | 500 Internal Server Error |

## Lambda Context Extensions

```csharp
public async Task<APIGatewayProxyResponse> MyHandler(
    APIGatewayProxyRequest request,
    ILambdaContext context)
{
    // Get correlation ID (from header or AWS Request ID)
    var correlationId = context.GetCorrelationId(request);

    // Get user ID from JWT claims
    var userId = context.GetUserId(request);

    // Get tenant ID from header or claims
    var tenantId = context.GetTenantId(request);

    // Get remaining execution time
    var remainingMs = context.GetRemainingTimeMs();

    // ...
}
```

## SQS Batch Processing

The `SqsMessageHandler` provides several methods for processing SQS messages:

### Process with Partial Failures

```csharp
// Returns SQSBatchResponse with failed message IDs
var response = await SqsMessageHandler.ProcessBatchAsync(
    sqsEvent,
    async record =>
    {
        var message = JsonSerializer.Deserialize<MyMessage>(record.Body);
        return await ProcessMessage(message);
    },
    logger);
```

### Process All or Fail

```csharp
// Stops on first error
var result = await SqsMessageHandler.ProcessAllAsync(
    sqsEvent,
    async record => await ProcessRecord(record));

result.Match(
    Right: _ => Console.WriteLine("All processed"),
    Left: error => Console.WriteLine($"Failed: {error.Message}"));
```

## Health Checks

The package includes an `AwsLambdaHealthCheck` that validates configuration and reports Lambda environment information:

```csharp
// Registered automatically via AddEncinaAwsLambda()
var healthChecks = serviceProvider.GetServices<IEncinaHealthCheck>();
foreach (var check in healthChecks)
{
    var result = await check.CheckHealthAsync();
    Console.WriteLine($"{check.Name}: {result.Status}");
}
```

## Cold Start Optimization

For optimal cold start performance:

1. Use source generators for serialization
2. Minimize dependencies
3. Use `[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]`
4. Consider using Native AOT compilation

```csharp
[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

public class Function
{
    private static readonly IServiceProvider _serviceProvider;

    // Static constructor for cold start initialization
    static Function()
    {
        var services = new ServiceCollection();
        services.AddEncina(typeof(Function).Assembly);
        services.AddEncinaAwsLambda();
        _serviceProvider = services.BuildServiceProvider();
    }
}
```

## Related Packages

- **Encina** - Core library with Railway Oriented Programming
- **Encina.AzureFunctions** - Azure Functions integration
- **Encina.AspNetCore** - ASP.NET Core integration
