# Encina.OpenTelemetry

OpenTelemetry integration for Encina - automatic tracing, metrics, and logging for Encina operations.

## Installation

```bash
dotnet add package Encina.OpenTelemetry
```

## Features

- **Automatic Tracing**: Integrates with Encina's built-in `ActivitySource` for distributed tracing
- **Metrics Collection**: Exposes Encina metrics through OpenTelemetry's `Meter` API
- **Runtime Instrumentation**: Includes .NET runtime metrics (GC, ThreadPool, etc.)
- **Easy Configuration**: Simple extension methods for OpenTelemetry builders

## Quick Start

### Basic Configuration

```csharp
using Encina.OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);

// Add Encina
builder.Services.AddEncina(config =>
{
    config.RegisterServicesFromAssemblyContaining<Program>();
});

// Add OpenTelemetry with Encina instrumentation
builder.Services.AddOpenTelemetry()
    .WithEncina(new EncinaOpenTelemetryOptions
    {
        ServiceName = "MyApplication",
        ServiceVersion = "1.0.0"
    })
    .WithTracing(tracing =>
    {
        tracing.AddConsoleExporter(); // or Jaeger, Zipkin, etc.
    })
    .WithMetrics(metrics =>
    {
        metrics.AddConsoleExporter(); // or Prometheus, etc.
    });

var app = builder.Build();
app.Run();
```

### Advanced Configuration

```csharp
builder.Services.AddOpenTelemetry()
    .WithEncina(new EncinaOpenTelemetryOptions
    {
        ServiceName = "MyApplication",
        ServiceVersion = "1.0.0"
    })
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddJaegerExporter(options =>
            {
                options.Endpoint = new Uri("http://localhost:14268");
            });
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddPrometheusExporter();
    });
```

### Using Individual Builders

You can also add Encina instrumentation to existing OpenTelemetry builder pipelines:

```csharp
// For tracing only
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddEncinaInstrumentation()
               .AddConsoleExporter();
    });

// For metrics only
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddEncinaInstrumentation()
               .AddConsoleExporter();
    });
```

## What Gets Instrumented?

### Tracing

Encina automatically creates `Activity` spans for:

- **Commands**: Each command execution creates a span tagged with `request.kind=command`
- **Queries**: Each query execution creates a span tagged with `request.kind=query`
- **Notifications**: Each notification dispatch creates spans for fan-out handlers

All spans include:

- Request type name
- Handler type name
- Execution duration
- Success/failure status
- Error details (if failed)

### Event Metadata (Correlation & Causation)

When using Encina.Marten for event sourcing, you can enrich activities with event metadata for end-to-end distributed tracing:

```csharp
using Encina.OpenTelemetry.Enrichers;
using System.Diagnostics;

// Enrich with correlation IDs from RequestContext
var activity = Activity.Current;
EventMetadataActivityEnricher.EnrichWithCorrelationIds(
    activity,
    requestContext.CorrelationId,
    commandId); // causation ID

// Enrich with full event details
EventMetadataActivityEnricher.EnrichWithEvent(
    activity,
    eventId: eventWithMetadata.Id,
    streamId: eventWithMetadata.StreamId,
    eventTypeName: eventWithMetadata.EventTypeName,
    version: eventWithMetadata.Version,
    sequence: eventWithMetadata.Sequence,
    timestamp: eventWithMetadata.Timestamp,
    correlationId: eventWithMetadata.CorrelationId,
    causationId: eventWithMetadata.CausationId);
```

Event metadata tags follow semantic conventions:

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

For Marten-specific types, use `MartenActivityEnricher` from `Encina.Marten.Instrumentation`:

```csharp
using Encina.Marten.Instrumentation;

// Enrich directly from EventWithMetadata
MartenActivityEnricher.EnrichWithEvent(activity, eventWithMetadata);

// Enrich from query results
MartenActivityEnricher.EnrichWithQueryResult(activity, queryResult);

// Enrich from causal chain traversal
MartenActivityEnricher.EnrichWithCausalChain(activity, events, CausalChainDirection.Ancestors);
```

### Metrics

Encina exposes the following metrics:

| Metric Name | Type | Description | Labels |
|-------------|------|-------------|--------|
| `Encina.request.success` | Counter | Successful requests | `request.kind`, `request.name` |
| `Encina.request.failure` | Counter | Failed requests | `request.kind`, `request.name`, `failure.reason` |
| `Encina.request.duration` | Histogram | Request execution time (ms) | `request.kind`, `request.name` |

### Migration Metrics

When using sharded migration coordination (`Encina.Sharding.Migrations`), the following metrics are automatically recorded:

| Metric Name | Type | Description | Labels |
|-------------|------|-------------|--------|
| `encina.migration.shards_migrated_total` | Counter | Shards successfully migrated | `migration.id`, `migration.strategy` |
| `encina.migration.shards_failed_total` | Counter | Shards that failed migration | `migration.id`, `migration.strategy` |
| `encina.migration.duration_per_shard_ms` | Histogram | Per-shard migration duration (ms) | `migration.id`, `migration.shard.id` |
| `encina.migration.total_duration_ms` | Histogram | Total coordination duration (ms) | `migration.id`, `migration.strategy` |
| `encina.migration.drift_detected_count` | ObservableGauge | Number of drifted shards detected | — |
| `encina.migration.rollbacks_total` | Counter | Rollback operations executed | `migration.id` |

Configure migration metrics callbacks:

```csharp
services.AddSingleton(new MigrationMetricsCallbacks(
    driftDetectedCount: () => myDriftCounter));
```

### Migration Tracing

Three trace activities are available via `MigrationActivitySource` (`Encina.Migration`):

- **`StartMigrationCoordination`**: Parent span for the entire migration coordination
- **`StartShardMigration`**: Per-shard migration span (child of coordination)
- **`Complete`**: Enriches the coordination span with final results

14 activity tags are automatically added under `ActivityTagNames.Migration.*` (e.g., `migration.id`, `migration.strategy`, `migration.shard.id`, `migration.shard.outcome`, `migration.duration_ms`).

### Schema Drift Health Check

`SchemaDriftHealthCheck` periodically checks for schema drift across shards and reports:

- **Healthy**: No drift detected
- **Degraded**: Drift detected in non-critical tables
- **Unhealthy**: Drift detected in critical tables

```csharp
// Register the health check
services.AddHealthChecks()
    .AddCheck<SchemaDriftHealthCheck>("schema-drift");

// Configure options
services.Configure<SchemaDriftHealthCheckOptions>(options =>
{
    options.Timeout = TimeSpan.FromSeconds(30);
    options.CriticalTables = ["Orders", "Customers"];
});
```

### Resharding Metrics

When using online resharding (`Encina.Sharding.Resharding`), the following metrics are automatically recorded:

| Metric Name | Type | Description | Tags |
|-------------|------|-------------|------|
| `encina.resharding.phase_duration_ms` | Histogram | Per-phase execution duration (ms) | `resharding.id`, `resharding.phase` |
| `encina.resharding.rows_copied_total` | Counter | Total rows copied during batch copy | `resharding.source_shard`, `resharding.target_shard` |
| `encina.resharding.rows_per_second` | ObservableGauge | Current copy throughput | — |
| `encina.resharding.cdc_lag_ms` | ObservableGauge | Current CDC replication lag (ms) | — |
| `encina.resharding.verification_mismatches_total` | Counter | Verification mismatches detected | `resharding.id` |
| `encina.resharding.cutover_duration_ms` | Histogram | Cutover phase duration (ms) | `resharding.id` |
| `encina.resharding.active_resharding_count` | ObservableGauge | Currently active resharding operations | — |

Configure resharding metrics callbacks:

```csharp
services.AddSingleton(new ReshardingMetricsCallbacks(
    rowsPerSecondCallback: () => currentRowsPerSecond,
    cdcLagMsCallback: () => currentCdcLagMs,
    activeReshardingCountCallback: () => activeCount));
```

### Resharding Tracing

Two trace activities are available via `ReshardingActivitySource` (`Encina.Resharding`):

- **`StartReshardingExecution`**: Parent span for the entire resharding operation, tagged with `resharding.id`, `resharding.step_count`, `resharding.estimated_rows`
- **`StartPhaseExecution`**: Per-phase child span, tagged with `resharding.id`, `resharding.phase`
- **`Complete`**: Enriches and disposes the activity with outcome status, duration, and optional error details

Activity enrichment is also available via `ReshardingActivityEnricher` for custom span decoration.

### Resharding Health Check

`ReshardingHealthCheck` monitors active resharding operations and classifies health:

| Condition | Status | Description |
|-----------|--------|-------------|
| No active operations | **Healthy** | "No active resharding operations." |
| Active and progressing | **Degraded** | "N resharding operation(s) in progress." |
| Failed without rollback | **Unhealthy** | "N failed without rollback" |
| Exceeded max duration | **Unhealthy** | "N exceeded max duration of Xh" |
| State store error | **Unhealthy** | "Failed to query resharding state: ..." |
| Timeout | **Unhealthy** | "Resharding health check timed out after Xs." |

```csharp
// Register the health check
services.AddHealthChecks()
    .Add(new HealthCheckRegistration(
        "resharding",
        sp => new ReshardingHealthCheck(
            sp.GetRequiredService<IReshardingStateStore>(),
            new ReshardingHealthCheckOptions()),
        failureStatus: HealthStatus.Degraded,
        tags: ["resharding", "sharding"]));

// Configure options
var options = new ReshardingHealthCheckOptions
{
    MaxReshardingDuration = TimeSpan.FromHours(4),  // Default: 2h
    Timeout = TimeSpan.FromSeconds(15)              // Default: 30s
};
```

## Configuration Options

### EncinaOpenTelemetryOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ServiceName` | `string` | `"Encina"` | Service name for OpenTelemetry resource |
| `ServiceVersion` | `string` | `"1.0.0"` | Service version for OpenTelemetry resource |

## Integration with Observability Platforms

### Jaeger (Distributed Tracing)

```csharp
builder.Services.AddOpenTelemetry()
    .WithEncina()
    .WithTracing(tracing =>
    {
        tracing.AddJaegerExporter(options =>
        {
            options.AgentHost = "localhost";
            options.AgentPort = 6831;
        });
    });
```

### Prometheus (Metrics)

```csharp
builder.Services.AddOpenTelemetry()
    .WithEncina()
    .WithMetrics(metrics =>
    {
        metrics.AddPrometheusExporter();
    });

app.UseOpenTelemetryPrometheusScrapingEndpoint(); // Exposes /metrics endpoint
```

### Azure Monitor / Application Insights

```csharp
builder.Services.AddOpenTelemetry()
    .WithEncina()
    .UseAzureMonitor(options =>
    {
        options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
    });
```

## Best Practices

1. **Set Meaningful Service Names**: Use descriptive service names that reflect your application's purpose
2. **Include Version Information**: Always set `ServiceVersion` for tracking deployments
3. **Use Sampling in Production**: Configure trace sampling to reduce overhead:

   ```csharp
   tracing.SetSampler(new TraceIdRatioBasedSampler(0.1)); // 10% sampling
   ```

4. **Monitor Metric Cardinality**: Be cautious with high-cardinality labels
5. **Correlate Logs with Traces**: Use OpenTelemetry's logging integration for complete observability

## Performance Considerations

- **Minimal Overhead**: Encina's built-in instrumentation uses `ActivitySource` which is highly optimized
- **Zero Cost When Disabled**: If no listeners are registered, instrumentation has near-zero overhead
- **Async-Friendly**: All instrumentation is async-aware and doesn't block execution

## Examples

See the `/samples` directory in the Encina repository for complete examples.

## Contributing

Contributions are welcome! Please see the main Encina repository for contribution guidelines.

## License

This project is licensed under the same license as Encina. See LICENSE for details.

## Resources

- [Encina Documentation](https://github.com/yourusername/Encina)
- [OpenTelemetry .NET Documentation](https://opentelemetry.io/docs/languages/net/)
- [OpenTelemetry Specification](https://opentelemetry.io/docs/specs/otel/)

---

**Sources:**

- [Microsoft.CodeAnalysis.PublicApiAnalyzers](https://www.nuget.org/packages/Microsoft.CodeAnalysis.PublicApiAnalyzers/)
- [.NET 10 Documentation](https://learn.microsoft.com/en-us/dotnet/)
- [PublicApiAnalyzers Help](https://github.com/dotnet/roslyn-analyzers/blob/ab7019ee000e20e0b822f6fca7d64eef4e09995d/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md)
