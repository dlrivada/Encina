# Encina.AwsLambda

AWS Lambda integration for the Encina library, providing seamless serverless function execution with Railway Oriented Programming support.

## Features

- **API Gateway Integration**: Convert Encina results to API Gateway responses (REST API and HTTP API)
- **SQS Trigger Support**: Process SQS messages with batch failure handling
- **EventBridge Integration**: Handle EventBridge (CloudWatch Events) with automatic deserialization
- **RFC 7807 Problem Details**: Standardized error responses
- **Context Enrichment**: Extract correlation ID and user ID from Lambda context, and resolve the tenant ID with `ResolveTenantId`
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
            context.Logger.LogError($"Failed: {error.GetCode().IfNone("encina.unknown")}"));
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
| `EnableRequestContextEnrichment` | Auto-enrich context with correlation ID and user ID | `true` |
| `CorrelationIdHeader` | Header name for correlation ID | `X-Correlation-ID` |
| `TenantIdHeader` | Header name for tenant ID, read only when `TrustTenantIdHeader` is `true` | `X-Tenant-ID` |
| `TrustTenantIdHeader` | Let the header supply the tenant when the request has no verified claim; each use logs a Warning | `false` |
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

## Error Category to HTTP Status Mapping

The API Gateway helpers (`ToApiGatewayResponse`, `ToProblemDetailsResponse`, `ToCreatedResponse`, `ToNoContentResponse`, `ToHttpApiResponse`) read the `ErrorCategory` of the error through `ErrorCategoryHttp` ([ADR-062](../../docs/architecture/adr/062-error-category-contract.md)). They read only the category; core infers a category from the code only for errors created without one (see the first bullet below).

| Error category | HTTP status |
| --- | --- |
| `Internal` | 500 |
| `Validation` | 400 |
| `InvalidRequest` | 400 |
| `Unauthenticated` | 401 |
| `Forbidden` | 403 |
| `NotFound` | 404 |
| `Conflict` | 409 |
| `AlreadyExists` | 409 |
| `FailedPrecondition` | 400 |
| `RateLimited` | 429 |
| `Unavailable` | 503 |
| `Timeout` | 504 |
| `UpstreamFailure` | 502 |
| `Cancelled` | 499 |

- An error with no category falls back to the legacy inference of core during the migration; an error with no code answers 500.
- An explicit `statusCode` (or `errorStatusCode`) argument overrides the status of the category.
- The problem `title` is the reason phrase of the status written.
- `Retry-After` (delta-seconds) is written for 429 and 503 when the error carries `EncinaErrorMetadataKeys.RetryAfter` (a `TimeSpan`).
- `WWW-Authenticate` is written on every 401, with the value of `EncinaErrorMetadataKeys.AuthenticateChallenge` or `Bearer`.
- The body never carries the error message (no `detail` member): only `status`, `title` and an `extensions` object holding `errorCode` (omitted when the error has no code).

Errors produced by the package helpers have these categories:

| Error code | Category |
| --- | --- |
| `eventbridge.detail_null`, `eventbridge.deserialization_failed`, `sqs.deserialization_failed` | `InvalidRequest` |
| `eventbridge.processing_failed` | `Internal` |
| `eventbridge.processing_cancelled` (the delegate was cancelled) | `Cancelled` |
| `tenancy.tenant_mismatch`, `tenancy.invalid_tenant_claim` | `Forbidden` |

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

    // Resolve the tenant: the authorizer claim wins; a different header is a Left
    // (tenancy.tenant_mismatch); the header alone counts only with TrustTenantIdHeader
    // options: EncinaAwsLambdaOptions, logger: ILogger (both injected)
    Either<EncinaError, Option<string>> tenant =
        context.ResolveTenantId(request, options, logger);

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
    Left: error => Console.WriteLine($"Failed: {error.GetCode().IfNone("unknown")}"));
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

## Performance

<!-- docref-table: bench:aws/* -->
<!-- /docref-table -->

*Auto-generated from benchmark data. See [methodology](../../docs/testing/performance-measurement-methodology.md).*

## Testing

Encina tests cloud features only locally, never against the real cloud. If you run this package in production, you do so under your own responsibility and risk.

The package is tested in process. The tests in `tests/Encina.IntegrationTests/Cloud/AwsLambda` deserialize real `Amazon.Lambda` event objects (`SQSEvent`, `CloudWatchEvent` for EventBridge, `APIGatewayProxyRequest` and `APIGatewayProxyResponse`, and the HTTP API v2 types) with the Lambda JSON serializer and drive a real `IEncina` pipeline built from dependency injection.

Running inside a real AWS Lambda runtime, or in the LocalStack Lambda service, is not tested. The [justification](../../tests/Encina.IntegrationTests/Cloud/AwsLambda/AwsLambdaLocalStackRuntime.md) explains why.

## Related Packages

- **Encina** - Core library with Railway Oriented Programming
- **Encina.AzureFunctions** - Azure Functions integration
- **Encina.AspNetCore** - ASP.NET Core integration
