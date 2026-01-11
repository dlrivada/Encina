# Health Checks Integration Guide

This guide explains how to integrate Encina's health checks with the ASP.NET Core health checks ecosystem and the `AspNetCore.HealthChecks.*` community packages.

## Overview

Encina provides health checks for **messaging patterns** (Outbox, Inbox, Saga, Scheduling), while the `AspNetCore.HealthChecks.*` packages provide health checks for **infrastructure** (databases, caches, message brokers, etc.).

These are complementary:

| Category | Provider | Examples |
|----------|----------|----------|
| **Pattern Health** | Encina | Outbox backlog, Inbox processing, Saga state, Scheduled messages |
| **Infrastructure Health** | AspNetCore.HealthChecks.* | Database connectivity, Redis availability, RabbitMQ connection |
| **Module Health** | Encina (Modular Monolith) | Per-module aggregated health checks |

## Quick Start

```csharp
builder.Services.AddHealthChecks()
    // Encina: messaging patterns
    .AddEncinaHealthChecks(sp)

    // Infrastructure: database
    .AddNpgSql(connectionString, tags: ["db", "ready"])

    // Infrastructure: cache
    .AddRedis(redisConnection, tags: ["cache", "ready"]);
```

## Architecture by Application Type

### Microservice Architecture

In a microservice, you typically need:

1. **Encina health checks** for messaging patterns
2. **Infrastructure health checks** for databases, caches, brokers
3. **Downstream service health checks** for dependent services

```csharp
var builder = WebApplication.CreateBuilder(args);

// Configure Encina
builder.Services.AddEncina(config =>
{
    config.UseEntityFrameworkCore<AppDbContext>(ef =>
    {
        ef.UseOutbox = true;
        ef.UseInbox = true;
    });
});

// Configure Health Checks
builder.Services.AddHealthChecks()
    // Encina patterns (Outbox, Inbox, etc.)
    .AddEncinaHealthChecks(builder.Services.BuildServiceProvider())

    // Primary database
    .AddNpgSql(
        builder.Configuration.GetConnectionString("Default")!,
        name: "postgres",
        tags: ["db", "ready", "critical"])

    // Redis cache
    .AddRedis(
        builder.Configuration.GetConnectionString("Redis")!,
        name: "redis",
        tags: ["cache", "ready"])

    // RabbitMQ message broker
    .AddRabbitMQ(
        builder.Configuration.GetConnectionString("RabbitMQ")!,
        name: "rabbitmq",
        tags: ["messaging", "ready"])

    // Downstream services
    .AddUrlGroup(
        new Uri("https://orders-api.internal/health"),
        name: "orders-service",
        tags: ["downstream", "ready"]);

var app = builder.Build();

// Map health endpoints
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false // No checks for liveness
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();
```

### Modular Monolith Architecture

In a modular monolith, you have additional module-level health checks:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Configure modules
builder.Services.AddEncinaModules(config =>
{
    config.AddModule<OrdersModule>();
    config.AddModule<PaymentsModule>();
    config.AddModule<InventoryModule>();
});

// Configure Encina
builder.Services.AddEncina(config =>
{
    config.UseEntityFrameworkCore<AppDbContext>(ef =>
    {
        ef.UseOutbox = true;
        ef.UseSagas = true;
    });
});

// Configure Health Checks
builder.Services.AddHealthChecks()
    // Encina patterns
    .AddEncinaHealthChecks(builder.Services.BuildServiceProvider())

    // Module health checks (aggregates all modules)
    .AddEncinaModuleHealthChecks()

    // Or check specific modules
    .AddEncinaModuleHealthChecks<OrdersModule>()
    .AddEncinaModuleHealthChecks<PaymentsModule>()

    // Infrastructure
    .AddSqlServer(
        builder.Configuration.GetConnectionString("Default")!,
        tags: ["db", "ready"]);

var app = builder.Build();

// Map health endpoints
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/modules", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("modules")
});

app.Run();
```

## Implementing Module Health Checks

To add health checks to your modules, implement `IModuleWithHealthChecks`:

```csharp
public class OrdersModule : IModuleWithHealthChecks
{
    public string Name => "Orders";

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IOrderService, OrderService>();
        // ... other services
    }

    public IEnumerable<IEncinaHealthCheck> GetHealthChecks()
    {
        yield return new OrdersDatabaseHealthCheck();
        yield return new OrdersPaymentGatewayHealthCheck();
    }
}
```

For module-specific health checks with additional context:

```csharp
public class OrdersDatabaseHealthCheck : IModuleHealthCheck
{
    public string Name => "orders-database";
    public string ModuleName => "Orders";
    public IReadOnlyCollection<string> Tags => ["database", "module", "orders"];

    public async Task<HealthCheckResult> CheckHealthAsync(
        CancellationToken cancellationToken = default)
    {
        // Check database connectivity
        var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

        return canConnect
            ? HealthCheckResult.Healthy("Orders database is accessible")
            : HealthCheckResult.Unhealthy("Cannot connect to Orders database");
    }
}
```

## Recommended NuGet Packages

The `AspNetCore.HealthChecks.*` ecosystem provides health checks for most infrastructure components:

### Databases

| Database | Package | NuGet |
|----------|---------|-------|
| PostgreSQL | `AspNetCore.HealthChecks.NpgSql` | [![NuGet](https://img.shields.io/nuget/v/AspNetCore.HealthChecks.NpgSql.svg)](https://www.nuget.org/packages/AspNetCore.HealthChecks.NpgSql/) |
| SQL Server | `AspNetCore.HealthChecks.SqlServer` | [![NuGet](https://img.shields.io/nuget/v/AspNetCore.HealthChecks.SqlServer.svg)](https://www.nuget.org/packages/AspNetCore.HealthChecks.SqlServer/) |
| MySQL | `AspNetCore.HealthChecks.MySql` | [![NuGet](https://img.shields.io/nuget/v/AspNetCore.HealthChecks.MySql.svg)](https://www.nuget.org/packages/AspNetCore.HealthChecks.MySql/) |
| Oracle | `AspNetCore.HealthChecks.Oracle` | [![NuGet](https://img.shields.io/nuget/v/AspNetCore.HealthChecks.Oracle.svg)](https://www.nuget.org/packages/AspNetCore.HealthChecks.Oracle/) |
| SQLite | `AspNetCore.HealthChecks.Sqlite` | [![NuGet](https://img.shields.io/nuget/v/AspNetCore.HealthChecks.Sqlite.svg)](https://www.nuget.org/packages/AspNetCore.HealthChecks.Sqlite/) |
| MongoDB | `AspNetCore.HealthChecks.MongoDb` | [![NuGet](https://img.shields.io/nuget/v/AspNetCore.HealthChecks.MongoDb.svg)](https://www.nuget.org/packages/AspNetCore.HealthChecks.MongoDb/) |
| CosmosDB | `AspNetCore.HealthChecks.CosmosDb` | [![NuGet](https://img.shields.io/nuget/v/AspNetCore.HealthChecks.CosmosDb.svg)](https://www.nuget.org/packages/AspNetCore.HealthChecks.CosmosDb/) |
| DynamoDB | `AspNetCore.HealthChecks.DynamoDb` | [![NuGet](https://img.shields.io/nuget/v/AspNetCore.HealthChecks.DynamoDb.svg)](https://www.nuget.org/packages/AspNetCore.HealthChecks.DynamoDb/) |

### Caching

| Cache | Package | NuGet |
|-------|---------|-------|
| Redis | `AspNetCore.HealthChecks.Redis` | [![NuGet](https://img.shields.io/nuget/v/AspNetCore.HealthChecks.Redis.svg)](https://www.nuget.org/packages/AspNetCore.HealthChecks.Redis/) |

### Message Brokers

| Broker | Package | NuGet |
|--------|---------|-------|
| RabbitMQ | `AspNetCore.HealthChecks.RabbitMQ` | [![NuGet](https://img.shields.io/nuget/v/AspNetCore.HealthChecks.RabbitMQ.svg)](https://www.nuget.org/packages/AspNetCore.HealthChecks.RabbitMQ/) |
| Kafka | `AspNetCore.HealthChecks.Kafka` | [![NuGet](https://img.shields.io/nuget/v/AspNetCore.HealthChecks.Kafka.svg)](https://www.nuget.org/packages/AspNetCore.HealthChecks.Kafka/) |
| Azure Service Bus | `AspNetCore.HealthChecks.AzureServiceBus` | [![NuGet](https://img.shields.io/nuget/v/AspNetCore.HealthChecks.AzureServiceBus.svg)](https://www.nuget.org/packages/AspNetCore.HealthChecks.AzureServiceBus/) |
| AWS SQS | `AspNetCore.HealthChecks.Aws.Sqs` | [![NuGet](https://img.shields.io/nuget/v/AspNetCore.HealthChecks.Aws.Sqs.svg)](https://www.nuget.org/packages/AspNetCore.HealthChecks.Aws.Sqs/) |

### Cloud Services

| Service | Package | NuGet |
|---------|---------|-------|
| Azure Blob Storage | `AspNetCore.HealthChecks.AzureStorage` | [![NuGet](https://img.shields.io/nuget/v/AspNetCore.HealthChecks.AzureStorage.svg)](https://www.nuget.org/packages/AspNetCore.HealthChecks.AzureStorage/) |
| Azure Key Vault | `AspNetCore.HealthChecks.AzureKeyVault` | [![NuGet](https://img.shields.io/nuget/v/AspNetCore.HealthChecks.AzureKeyVault.svg)](https://www.nuget.org/packages/AspNetCore.HealthChecks.AzureKeyVault/) |
| AWS S3 | `AspNetCore.HealthChecks.Aws.S3` | [![NuGet](https://img.shields.io/nuget/v/AspNetCore.HealthChecks.Aws.S3.svg)](https://www.nuget.org/packages/AspNetCore.HealthChecks.Aws.S3/) |

### External Services

| Service | Package | NuGet |
|---------|---------|-------|
| HTTP/URLs | `AspNetCore.HealthChecks.Uris` | [![NuGet](https://img.shields.io/nuget/v/AspNetCore.HealthChecks.Uris.svg)](https://www.nuget.org/packages/AspNetCore.HealthChecks.Uris/) |
| SignalR | `AspNetCore.HealthChecks.SignalR` | [![NuGet](https://img.shields.io/nuget/v/AspNetCore.HealthChecks.SignalR.svg)](https://www.nuget.org/packages/AspNetCore.HealthChecks.SignalR/) |

### Health Check UI

| Feature | Package | NuGet |
|---------|---------|-------|
| Health Check UI | `AspNetCore.HealthChecks.UI` | [![NuGet](https://img.shields.io/nuget/v/AspNetCore.HealthChecks.UI.svg)](https://www.nuget.org/packages/AspNetCore.HealthChecks.UI/) |
| UI Client | `AspNetCore.HealthChecks.UI.Client` | [![NuGet](https://img.shields.io/nuget/v/AspNetCore.HealthChecks.UI.Client.svg)](https://www.nuget.org/packages/AspNetCore.HealthChecks.UI.Client/) |

## Kubernetes Configuration

### Understanding Probes

Kubernetes uses three types of probes:

| Probe | Purpose | When it Fails |
|-------|---------|---------------|
| **Liveness** | Is the process alive? | Container is restarted |
| **Readiness** | Can it handle requests? | Removed from load balancer |
| **Startup** | Has it finished starting? | Liveness/readiness delayed |

### Recommended Configuration

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: my-service
spec:
  template:
    spec:
      containers:
      - name: my-service
        image: my-service:latest
        ports:
        - containerPort: 8080

        # Liveness: Is the process alive?
        # - Should return quickly
        # - Should NOT check external dependencies
        # - Failure = container restart
        livenessProbe:
          httpGet:
            path: /health/live
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 15
          timeoutSeconds: 5
          failureThreshold: 3

        # Readiness: Can it handle traffic?
        # - Should check critical dependencies
        # - Failure = removed from load balancer (no restart)
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 10
          timeoutSeconds: 10
          failureThreshold: 3

        # Startup: Has the app finished starting?
        # - Useful for slow-starting containers
        # - Disables liveness/readiness until successful
        startupProbe:
          httpGet:
            path: /health/ready
            port: 8080
          initialDelaySeconds: 0
          periodSeconds: 5
          timeoutSeconds: 10
          failureThreshold: 30  # 30 * 5s = 150s max startup time
```

### ASP.NET Core Endpoint Configuration

```csharp
// Liveness: Always returns 200 (process is alive)
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,  // No checks, just return 200
    ResponseWriter = (context, _) =>
    {
        context.Response.ContentType = "text/plain";
        return context.Response.WriteAsync("Healthy");
    }
});

// Readiness: Checks critical dependencies
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Full health report (for monitoring dashboards)
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

### Timeout Recommendations

| Probe Type | Timeout | Rationale |
|------------|---------|-----------|
| Liveness | 5s | Should be fast; only checks process health |
| Readiness | 10s | May check external dependencies |
| Startup | 10s | Same as readiness, but with more retries |

### Best Practices

1. **Liveness probes should be lightweight**
   - Don't check databases or external services
   - Return immediately if the process is running

2. **Readiness probes should check critical dependencies**
   - Database connectivity
   - Cache availability
   - Message broker connection
   - Downstream services (if critical)

3. **Use tags to organize health checks**
   ```csharp
   .AddNpgSql(conn, tags: ["db", "ready", "critical"])
   .AddRedis(redis, tags: ["cache", "ready"])
   .AddRabbitMQ(rabbit, tags: ["messaging"])  // Not tagged "ready" if optional
   ```

4. **Consider circuit breakers for downstream services**
   - Don't let slow downstream services cause pod restarts
   - Use degraded status instead of unhealthy for non-critical dependencies

## Health Check UI

For monitoring dashboards, add the Health Check UI:

```csharp
builder.Services.AddHealthChecksUI(options =>
{
    options.SetEvaluationTimeInSeconds(30);
    options.MaximumHistoryEntriesPerEndpoint(50);
    options.AddHealthCheckEndpoint("API", "/health");
})
.AddInMemoryStorage();

// ...

app.MapHealthChecksUI(options =>
{
    options.UIPath = "/health-ui";
});
```

## Complete Example

Here's a complete example combining all concepts:

```csharp
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Encina configuration
builder.Services.AddEncina(config =>
{
    config.UseEntityFrameworkCore<AppDbContext>(ef =>
    {
        ef.UseOutbox = true;
        ef.UseInbox = true;
        ef.UseSagas = true;
    });
});

// Health checks configuration
builder.Services.AddHealthChecks()
    // Encina patterns
    .AddEncinaHealthChecks(builder.Services.BuildServiceProvider())

    // Database (critical)
    .AddNpgSql(
        builder.Configuration.GetConnectionString("Database")!,
        name: "database",
        tags: ["db", "ready", "critical"],
        timeout: TimeSpan.FromSeconds(5))

    // Redis cache (critical for performance)
    .AddRedis(
        builder.Configuration.GetConnectionString("Redis")!,
        name: "redis",
        tags: ["cache", "ready"],
        timeout: TimeSpan.FromSeconds(3))

    // RabbitMQ (critical for messaging)
    .AddRabbitMQ(
        builder.Configuration.GetConnectionString("RabbitMQ")!,
        name: "rabbitmq",
        tags: ["messaging", "ready"],
        timeout: TimeSpan.FromSeconds(5))

    // Downstream: Orders service
    .AddUrlGroup(
        new Uri(builder.Configuration["Services:Orders:HealthUrl"]!),
        name: "orders-service",
        tags: ["downstream", "ready"],
        timeout: TimeSpan.FromSeconds(10))

    // Downstream: Payments service (optional - degraded if unavailable)
    .AddUrlGroup(
        new Uri(builder.Configuration["Services:Payments:HealthUrl"]!),
        name: "payments-service",
        tags: ["downstream"],  // Not tagged "ready" - won't affect readiness
        timeout: TimeSpan.FromSeconds(10));

// Health Check UI (optional)
builder.Services.AddHealthChecksUI(options =>
{
    options.SetEvaluationTimeInSeconds(30);
    options.MaximumHistoryEntriesPerEndpoint(50);
})
.AddInMemoryStorage();

var app = builder.Build();

// Liveness: Always healthy if process is running
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

// Readiness: Check critical dependencies
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Full health report
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Health Check UI dashboard
app.MapHealthChecksUI(options => options.UIPath = "/health-ui");

app.Run();
```

## Summary

| What to Check | How | Tags |
|---------------|-----|------|
| Encina patterns | `AddEncinaHealthChecks()` | `encina`, `ready` |
| Module health | `AddEncinaModuleHealthChecks()` | `encina`, `modules`, `ready` |
| Databases | `AspNetCore.HealthChecks.*` packages | `db`, `ready` |
| Caches | `AspNetCore.HealthChecks.Redis` etc. | `cache`, `ready` |
| Message brokers | `AspNetCore.HealthChecks.RabbitMQ` etc. | `messaging`, `ready` |
| Downstream services | `AspNetCore.HealthChecks.Uris` | `downstream`, `ready` |

## See Also

- [Encina.AspNetCore README](../../src/Encina.AspNetCore/README.md)
- [ASP.NET Core Health Checks Documentation](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)
- [AspNetCore.Diagnostics.HealthChecks GitHub](https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks)
- [Kubernetes Probes Documentation](https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-startup-probes/)
