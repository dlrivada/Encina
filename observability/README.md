# Encina Observability Stack

Production-ready observability solution using OpenTelemetry, Prometheus, Jaeger, Loki, and Grafana.

## 🎯 Stack Overview

```
.NET Application
      ↓
OpenTelemetry SDK
      ↓
OpenTelemetry Collector (OTLP receiver)
      ↓
   ┌──┴──────┬──────────┐
   ↓         ↓          ↓
Prometheus  Jaeger     Loki
(Metrics)   (Traces)   (Logs)
   └──┬──────┴──────────┘
      ↓
   Grafana
(Unified Dashboards)
```

## 🚀 Quick Start

### 1. Start the Observability Stack

```bash
cd D:\Proyectos\Encina
docker-compose -f docker-compose.observability.yml up -d
```

### 2. Configure Your .NET Application

```csharp
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;

var builder = WebApplication.CreateBuilder(args);

// Add Encina
builder.Services.AddEncina(config => { });

// Add OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("my-service", serviceVersion: "1.0.0"))
    .WithTracing(tracing => tracing
        .AddEncinaInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://localhost:4317");
        }))
    .WithMetrics(metrics => metrics
        .AddEncinaInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://localhost:4317");
        }))
    .WithLogging(logging => logging
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://localhost:4317");
        }));
```

### 3. Access the Dashboards

- **Grafana**: <http://localhost:3000> (default dashboard auto-loaded)
- **Prometheus**: <http://localhost:9090>
- **Jaeger UI**: <http://localhost:16686>
- **Loki**: <http://localhost:3100>

## 📊 Available Dashboards

### Encina Overview Dashboard

Pre-configured dashboard with:

- **Request Rate**: Real-time req/sec gauge
- **Latency**: P50, P95, P99 latency graphs
- **Success vs Error Rate**: Pie chart showing request outcomes
- **Distributed Traces**: Jaeger trace viewer
- **Application Logs**: Loki log aggregation

## 🔍 Example Queries

### Prometheus Queries

```promql
# Request rate
rate(Encina_requests_total[5m])

# P95 latency
histogram_quantile(0.95, sum(rate(Encina_request_duration_seconds_bucket[5m])) by (le))

# Error rate
rate(Encina_requests_total{status="error"}[5m])
```

### Loki Queries (LogQL)

```logql
# All Encina logs
{service_name="Encina"}

# Error logs only
{service_name="Encina"} |= "error"

# Logs with trace correlation
{service_name="Encina"} | json | traceID != ""
```

### Jaeger Queries

- Service: `Encina`
- Operation: `Encina.Send`
- Tags: `request.type`, `handler.name`, `error`

## 🛠️ Configuration Files

### OpenTelemetry Collector

**File**: `observability/otel-collector-config.yaml`

- Receives: OTLP gRPC (4317), OTLP HTTP (4318)
- Exports to: Prometheus, Jaeger, Loki
- Processors: batch, memory_limiter, resource

### Prometheus

**File**: `observability/prometheus.yml`

- Scrapes: OpenTelemetry Collector (8889)
- Retention: 15 days (default)
- OTLP write receiver enabled

### Loki

**File**: `observability/loki-config.yaml`

- Retention: 7 days
- Storage: Filesystem (local development)
- Schema: v13 (tsdb)

### Grafana

**Files**:

- `observability/grafana/provisioning/datasources/datasources.yaml`
- `observability/grafana/provisioning/dashboards/dashboards.yaml`

- Datasources: Prometheus, Jaeger, Loki (auto-configured)
- Dashboards: Auto-loaded from `/var/lib/grafana/dashboards`

## 📈 Metrics Exposed by Encina

| Metric | Type | Description |
|--------|------|-------------|
| `Encina_requests_total` | Counter | Total requests (by type, status) |
| `Encina_request_duration_seconds` | Histogram | Request duration (P50, P95, P99) |
| `Encina_pipeline_duration_seconds` | Histogram | Pipeline execution time |
| `Encina_handler_duration_seconds` | Histogram | Handler execution time |
| `Encina_validation_failures_total` | Counter | Validation failures |
| `Encina_outbox_messages_total` | Counter | Outbox messages published |
| `Encina_inbox_messages_total` | Counter | Inbox messages processed |
| `Encina_saga_executions_total` | Counter | Saga executions |

## 🏷️ Trace Attributes

Traces include the following attributes:

- `request.type`: Request type name
- `request.id`: Unique request ID
- `handler.name`: Handler class name
- `pipeline.behavior`: Behavior name (if any)
- `error`: Error message (if failed)
- `validation.errors`: Validation error count
- `outbox.message_id`: Outbox message ID
- `inbox.message_id`: Inbox message ID
- `saga.id`: Saga ID
- `saga.step`: Current saga step

## 🔧 Troubleshooting

### Collector Not Receiving Data

```bash
# Check collector logs
docker logs otel-collector

# Verify endpoint is reachable
curl http://localhost:4317
```

### Prometheus Not Scraping

```bash
# Check Prometheus targets
open http://localhost:9090/targets

# Verify collector metrics endpoint
curl http://localhost:8889/metrics
```

### Grafana Dashboard Empty

1. Verify datasources are configured: <http://localhost:3000/datasources>
2. Check that data is flowing to Prometheus: <http://localhost:9090/graph>
3. Verify time range in Grafana (default: last 1 hour)

### Loki Not Receiving Logs

```bash
# Check Loki health
curl http://localhost:3100/ready

# Verify log ingestion
curl -G -s "http://localhost:3100/loki/api/v1/query" --data-urlencode 'query={service_name="Encina"}'
```

## 🔒 Production Considerations

### Security

- Enable authentication on Grafana
- Use TLS for OTLP endpoints
- Restrict network access to observability stack
- Rotate API keys regularly

### Scalability

- Use remote storage for Prometheus (Thanos, Cortex)
- Deploy Loki with object storage (S3, GCS)
- Run OpenTelemetry Collector as sidecar or gateway
- Consider Jaeger with Elasticsearch/Cassandra backend

### Performance

- Adjust sampling rate for traces (use probabilistic sampler)
- Set appropriate retention periods
- Monitor collector resource usage
- Use batch processors to reduce network overhead

## 📚 Additional Resources

- [OpenTelemetry .NET Documentation](https://opentelemetry.io/docs/instrumentation/net/)
- [Prometheus Query Language](https://prometheus.io/docs/prometheus/latest/querying/basics/)
- [Jaeger Tracing](https://www.jaegertracing.io/docs/)
- [Loki LogQL](https://grafana.com/docs/loki/latest/logql/)
- [Grafana Dashboards](https://grafana.com/docs/grafana/latest/dashboards/)

## 🛑 Stop the Stack

```bash
docker-compose -f docker-compose.observability.yml down

# With volume cleanup
docker-compose -f docker-compose.observability.yml down -v
```

## 📝 License

This observability stack configuration is part of Encina and follows the same license.
