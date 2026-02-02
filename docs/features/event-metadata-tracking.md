# Event Metadata Tracking

This document describes the event metadata tracking feature in Encina.Marten, which enables correlation and causation ID tracking for distributed tracing and debugging of event-sourced systems.

## Overview

When building event-sourced systems, it's essential to track relationships between events for:

- **Debugging**: Trace which command triggered which events
- **Distributed Tracing**: Correlate events across services with OpenTelemetry
- **Audit**: Understand the causal chain of business operations
- **Replay Analysis**: Identify which events belong to the same logical operation

Encina.Marten provides automatic metadata enrichment and query capabilities for these scenarios.

## Configuration

### Basic Setup

```csharp
services.AddEncinaMarten(options =>
{
    // Metadata options are accessed via options.Metadata
    // Most features are enabled by default
});
```

### Full Configuration

```csharp
services.AddEncinaMarten(options =>
{
    // Core tracking (enabled by default)
    options.Metadata.CorrelationIdEnabled = true;   // Links related events
    options.Metadata.CausationIdEnabled = true;     // Links cause-effect events
    options.Metadata.CaptureUserId = true;          // Captures IRequestContext.UserId
    options.Metadata.CaptureTenantId = true;        // Captures IRequestContext.TenantId
    options.Metadata.CaptureTimestamp = true;       // Captures IRequestContext.Timestamp
    options.Metadata.HeadersEnabled = true;         // Enables header storage

    // Optional features (disabled by default)
    options.Metadata.CaptureCommitSha = true;
    options.Metadata.CommitSha = Environment.GetEnvironmentVariable("COMMIT_SHA");
    options.Metadata.CaptureSemanticVersion = true;
    options.Metadata.SemanticVersion = "1.2.3";

    // Custom headers (static values)
    options.Metadata.CustomHeaders["Environment"] = "Production";
    options.Metadata.CustomHeaders["Region"] = "eu-west-1";
});
```

### Disabling All Metadata

```csharp
services.AddEncinaMarten(options =>
{
    options.Metadata.CorrelationIdEnabled = false;
    options.Metadata.CausationIdEnabled = false;
    options.Metadata.CaptureUserId = false;
    options.Metadata.CaptureTenantId = false;
    options.Metadata.CaptureTimestamp = false;
    options.Metadata.CaptureCommitSha = false;
    options.Metadata.CaptureSemanticVersion = false;
    options.Metadata.HeadersEnabled = false;
});
```

## Automatic Propagation

### From Request Context

When saving aggregates via `MartenAggregateRepository`, metadata is automatically extracted from `IRequestContext`:

```csharp
public class CreateOrderHandler : ICommandHandler<CreateOrderCommand, OrderId>
{
    private readonly IAggregateRepository<Order> _repository;

    public async ValueTask<Either<EncinaError, OrderId>> Handle(
        CreateOrderCommand command,
        IRequestContext context,  // Contains CorrelationId, UserId, TenantId, etc.
        CancellationToken cancellationToken)
    {
        var order = Order.Create(command.CustomerId, command.Items);

        // Metadata is automatically enriched from context
        await _repository.SaveAsync(order, context, cancellationToken);

        return order.Id;
    }
}
```

The following context properties are automatically captured:

| Context Property | Header Name | Condition |
|-----------------|-------------|-----------|
| `CorrelationId` | (native Marten field) | `CorrelationIdEnabled` |
| `CorrelationId` (as causation) | (native Marten field) | `CausationIdEnabled` and no explicit causation |
| `UserId` | `"UserId"` | `CaptureUserId` and non-empty |
| `TenantId` | `"TenantId"` | `CaptureTenantId` and non-empty |
| `Timestamp` | `"Timestamp"` (ISO 8601) | `CaptureTimestamp` |

### Explicit Causation ID

To set an explicit causation ID (e.g., linking to a previous event):

```csharp
var contextWithCausation = context.WithMetadata("CausationId", previousEventId.ToString());
await _repository.SaveAsync(order, contextWithCausation, cancellationToken);
```

## Custom Metadata Enrichers

For dynamic metadata that depends on the event being stored, implement `IEventMetadataEnricher`:

```csharp
public class AuditEnricher : IEventMetadataEnricher
{
    private readonly IHttpContextAccessor _httpContext;

    public AuditEnricher(IHttpContextAccessor httpContext)
    {
        _httpContext = httpContext;
    }

    public IDictionary<string, object> EnrichMetadata(object domainEvent, IRequestContext context)
    {
        var metadata = new Dictionary<string, object>();

        // Add IP address if available
        var ip = _httpContext.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        if (!string.IsNullOrEmpty(ip))
        {
            metadata["ClientIP"] = ip;
        }

        // Add user agent
        var userAgent = _httpContext.HttpContext?.Request?.Headers?.UserAgent.ToString();
        if (!string.IsNullOrEmpty(userAgent))
        {
            metadata["UserAgent"] = userAgent;
        }

        return metadata;
    }
}
```

Register enrichers with DI:

```csharp
services.AddSingleton<IEventMetadataEnricher, AuditEnricher>();
```

## Querying Events by Metadata

### IEventMetadataQuery Interface

Inject `IEventMetadataQuery` to query events by their metadata:

```csharp
public class EventTraceService
{
    private readonly IEventMetadataQuery _query;

    public EventTraceService(IEventMetadataQuery query)
    {
        _query = query;
    }

    public async Task<IReadOnlyList<EventWithMetadata>> GetWorkflowEvents(string correlationId)
    {
        var result = await _query.GetEventsByCorrelationIdAsync(correlationId);
        return result.Match(
            r => r.Events,
            error => throw new InvalidOperationException(error.ToString()));
    }
}
```

### Available Query Methods

```csharp
// Find all events in a workflow
var result = await query.GetEventsByCorrelationIdAsync("correlation-123");

// Find events caused by a specific action
var caused = await query.GetEventsByCausationIdAsync("command-456");

// Trace event ancestry (what caused this event)
var ancestors = await query.GetCausalChainAsync(eventId, CausalChainDirection.Ancestors);

// Trace event descendants (what this event caused)
var descendants = await query.GetCausalChainAsync(eventId, CausalChainDirection.Descendants);

// Get single event with full metadata
var event = await query.GetEventByIdAsync(eventId);
```

### Pagination and Filtering

```csharp
var options = new EventQueryOptions
{
    Skip = 0,
    Take = 50,                           // Max 1000
    StreamId = specificAggregateId,      // Filter by stream
    EventTypes = ["OrderCreated", "OrderShipped"],  // Filter by type
    FromTimestamp = startDate,           // Time range filter
    ToTimestamp = endDate,
};

var result = await query.GetEventsByCorrelationIdAsync(correlationId, options);

// Result includes pagination info
Console.WriteLine($"Total: {result.TotalCount}");
Console.WriteLine($"Has more: {result.HasMore}");
```

## OpenTelemetry Integration

### Generic Activity Enrichment

Use `EventMetadataActivityEnricher` for basic correlation tracking:

```csharp
using Encina.OpenTelemetry.Enrichers;
using System.Diagnostics;

// At the start of request processing
var activity = Activity.Current;
EventMetadataActivityEnricher.EnrichWithCorrelationIds(
    activity,
    context.CorrelationId,
    causationId: commandId);

// After querying events
var result = await query.GetEventsByCorrelationIdAsync(correlationId);
EventMetadataActivityEnricher.EnrichWithQueryResult(
    activity,
    result.TotalCount,
    result.Events.Count,
    result.HasMore,
    correlationId);
```

### Marten-Specific Enrichment

Use `MartenActivityEnricher` when working with Marten types:

```csharp
using Encina.Marten.Instrumentation;

// Enrich with a single event
var eventResult = await query.GetEventByIdAsync(eventId);
eventResult.Match(
    ev => MartenActivityEnricher.EnrichWithEvent(activity, ev),
    _ => { });

// Enrich with query results
var queryResult = await query.GetEventsByCorrelationIdAsync(correlationId);
queryResult.Match(
    r => MartenActivityEnricher.EnrichWithQueryResult(activity, r),
    _ => { });

// Enrich with causal chain
var chain = await query.GetCausalChainAsync(eventId, CausalChainDirection.Ancestors);
chain.Match(
    events => MartenActivityEnricher.EnrichWithCausalChain(activity, events, CausalChainDirection.Ancestors),
    _ => { });
```

### Activity Tags

The enrichers add the following OpenTelemetry semantic tags:

| Tag | Description |
|-----|-------------|
| `event.message_id` | Unique event identifier |
| `event.correlation_id` | Links related events across a workflow |
| `event.causation_id` | Links cause-effect event relationships |
| `event.stream_id` | Aggregate/stream identifier |
| `event.type_name` | Event type name |
| `event.version` | Version within the stream |
| `event.sequence` | Global sequence number |
| `event.timestamp` | Event timestamp (ISO 8601) |
| `event.query.total_count` | Total matching events in query |
| `event.query.returned_count` | Events returned in current page |
| `event.query.has_more` | Whether more results are available |
| `event.causal_chain.depth` | Number of events in causal chain |
| `event.causal_chain.direction` | Ancestors or Descendants |

## Best Practices

### 1. Always Use Correlation IDs

Every request should have a correlation ID to trace its entire lifecycle:

```csharp
// In ASP.NET Core middleware
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
        ?? Guid.NewGuid().ToString();

    context.Items["CorrelationId"] = correlationId;
    context.Response.Headers["X-Correlation-Id"] = correlationId;

    await next();
});
```

### 2. Set Explicit Causation for Event-Driven Processing

When one event triggers another command, set the causation ID:

```csharp
public class OrderCreatedHandler : INotificationHandler<OrderCreated>
{
    public async Task Handle(OrderCreated notification, IRequestContext context)
    {
        // The new events should know they were caused by OrderCreated
        var contextWithCausation = context.WithMetadata(
            "CausationId",
            notification.EventId.ToString());

        await _mediator.Send(new NotifyCustomer(notification.OrderId), contextWithCausation);
    }
}
```

### 3. Use Custom Enrichers for Request Context

Capture request-specific metadata via enrichers:

```csharp
public class RequestEnricher : IEventMetadataEnricher
{
    public IDictionary<string, object> EnrichMetadata(object domainEvent, IRequestContext context)
    {
        return new Dictionary<string, object>
        {
            ["RequestId"] = Activity.Current?.Id ?? "unknown",
            ["TraceId"] = Activity.Current?.TraceId.ToString() ?? "unknown",
        };
    }
}
```

### 4. Index Metadata Fields for Performance

Ensure your Marten schema has indexes on metadata fields:

```csharp
services.AddMarten(options =>
{
    // Enable metadata storage
    options.Events.MetadataConfig.EnableAll();

    // Create indexes (done automatically by Marten)
});
```

## Error Handling

Query methods return `Either<EncinaError, T>` for explicit error handling:

```csharp
var result = await query.GetEventsByCorrelationIdAsync(correlationId);

result.Match(
    Right: queryResult =>
    {
        foreach (var ev in queryResult.Events)
        {
            Console.WriteLine($"{ev.EventTypeName} at {ev.Timestamp}");
        }
    },
    Left: error =>
    {
        error.GetCode().Match(
            code =>
            {
                if (code == MartenErrorCodes.InvalidQuery)
                    Console.WriteLine("Invalid query parameters");
                else if (code == MartenErrorCodes.QueryFailed)
                    Console.WriteLine("Database query failed");
            },
            () => Console.WriteLine($"Error: {error}"));
    });
```

## See Also

- [Marten Event Versioning](../marten/event-versioning.md)
- [OpenTelemetry Integration](../observability/opentelemetry.md)
- [Request Context](../core/request-context.md)
