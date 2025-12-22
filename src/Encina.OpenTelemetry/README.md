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

### Metrics

Encina exposes the following metrics:

| Metric Name | Type | Description | Labels |
|-------------|------|-------------|--------|
| `Encina.request.success` | Counter | Successful requests | `request.kind`, `request.name` |
| `Encina.request.failure` | Counter | Failed requests | `request.kind`, `request.name`, `failure.reason` |
| `Encina.request.duration` | Histogram | Request execution time (ms) | `request.kind`, `request.name` |

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
