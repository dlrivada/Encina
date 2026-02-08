# Database Resilience

> Connection pool monitoring, circuit breakers, and connection warm-up for all 13 database providers.

## Overview

Encina's database resilience feature provides production-ready monitoring and fault tolerance for database connections. All features are **opt-in** following Encina's pay-for-what-you-use philosophy.

Key capabilities:

- **Connection Pool Monitoring**: Real-time pool statistics via `IDatabaseHealthMonitor.GetPoolStatistics()`
- **Health Checks**: Active database health probing with `CheckHealthAsync()` returning `Healthy`, `Degraded`, or `Unhealthy`
- **Circuit Breaker**: Database-aware circuit breaker using Polly that fails fast when the database is unreachable
- **Transient Error Detection**: Provider-agnostic identification of recoverable database errors
- **Connection Warm-up**: Pre-establish connections during application startup to avoid cold-start latency

## Quick Start

### Basic Setup

```csharp
// Register your provider (ADO.NET, Dapper, or EF Core)
services.AddEncinaADO(connectionString, config => { });

// Health monitor is automatically registered as IDatabaseHealthMonitor

// Optional: Add circuit breaker pipeline behavior
services.AddEncinaPolly();
services.AddDatabaseCircuitBreaker(options =>
{
    options.FailureThreshold = 0.3;
    options.BreakDuration = TimeSpan.FromMinutes(1);
});
```

### Full Resilience Configuration

```csharp
services.AddEncinaEntityFrameworkCore<AppDbContext>(config =>
{
    config.UseResilience(options =>
    {
        options.EnablePoolMonitoring = true;
        options.EnableCircuitBreaker = true;
        options.CircuitBreaker.FailureThreshold = 0.3;
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(15);
        options.CircuitBreaker.BreakDuration = TimeSpan.FromMinutes(1);
        options.CircuitBreaker.MinimumThroughput = 20;
        options.WarmUpConnections = 5;
        options.HealthCheckInterval = TimeSpan.FromSeconds(30);
    });
});
```

## Core Types

### IDatabaseHealthMonitor

The central interface for database resilience monitoring. Each of the 13 providers registers its own implementation as a singleton.

```csharp
public interface IDatabaseHealthMonitor
{
    string ProviderName { get; }                    // e.g. "ado-sqlserver", "dapper-postgresql"
    bool IsCircuitOpen { get; }                     // Circuit breaker state (volatile bool)
    ConnectionPoolStats GetPoolStatistics();         // Point-in-time pool snapshot
    Task<DatabaseHealthResult> CheckHealthAsync(CancellationToken ct = default);
    Task ClearPoolAsync(CancellationToken ct = default);
}
```

### ConnectionPoolStats

Immutable record providing a snapshot of the connection pool state.

```csharp
var stats = monitor.GetPoolStatistics();

Console.WriteLine($"Active: {stats.ActiveConnections}");
Console.WriteLine($"Idle: {stats.IdleConnections}");
Console.WriteLine($"Total: {stats.TotalConnections}");
Console.WriteLine($"Pending: {stats.PendingRequests}");
Console.WriteLine($"Max: {stats.MaxPoolSize}");
Console.WriteLine($"Utilization: {stats.PoolUtilization:P0}");  // Computed, clamped [0, 1]
```

Providers that do not expose pool statistics (SQLite, EF Core) return `ConnectionPoolStats.CreateEmpty()`.

### DatabaseHealthResult

Readonly record struct with factory methods for health check results.

```csharp
var result = await monitor.CheckHealthAsync(ct);

switch (result.Status)
{
    case DatabaseHealthStatus.Healthy:
        // Database is functioning correctly
        break;
    case DatabaseHealthStatus.Degraded:
        // Reachable but pool utilization > 90%
        break;
    case DatabaseHealthStatus.Unhealthy:
        // Unreachable or circuit breaker is open
        break;
}

// Access additional data (pool stats, provider name)
var provider = result.Data["provider"];
```

### DatabaseResilienceOptions

All options are disabled by default:

| Option | Default | Description |
|--------|---------|-------------|
| `EnablePoolMonitoring` | `false` | Register `IDatabaseHealthMonitor` |
| `EnableCircuitBreaker` | `false` | Enable circuit breaker for database operations |
| `WarmUpConnections` | `0` | Connections to establish on startup |
| `HealthCheckInterval` | `TimeSpan.Zero` | Periodic health check interval (0 = disabled) |

### DatabaseCircuitBreakerOptions

| Option | Default | Description |
|--------|---------|-------------|
| `FailureThreshold` | `0.5` | Failure rate to open circuit (0-1) |
| `SamplingDuration` | `10s` | Window for measuring failure rate |
| `BreakDuration` | `30s` | How long circuit stays open |
| `MinimumThroughput` | `10` | Min operations before evaluating |
| `IncludeTimeouts` | `true` | Count timeouts as failures |
| `IncludeConnectionFailures` | `true` | Count connection failures |

## Circuit Breaker

The `DatabaseCircuitBreakerPipelineBehavior<TRequest, TResponse>` integrates with Encina's pipeline as a behavior that wraps every command/query handler.

### How It Works

1. **Fast-path check**: If `IDatabaseHealthMonitor.IsCircuitOpen` is `true`, the behavior immediately returns an error without invoking the handler
2. **Polly integration**: Uses `Polly.CircuitBreaker` with provider-specific configuration
3. **State transitions**: Monitors `Opened`, `Closed`, and `HalfOpened` transitions with logging
4. **Per-provider circuits**: Each provider name gets its own circuit breaker instance (cached in `ConcurrentDictionary`)

### Transient Error Detection

`DatabaseTransientErrorPredicate` identifies recoverable errors by exception type name, avoiding hard assembly references:

| Provider | Exception Types |
|----------|----------------|
| SQL Server | `SqlException` |
| PostgreSQL | `NpgsqlException`, `PostgresException` |
| MySQL | `MySqlException` |
| MongoDB | `MongoException`, `MongoConnectionException` |
| SQLite | `SqliteException` |
| Generic | `DbException`, `TimeoutException`, `SocketException`, `IOException` |

The predicate walks the exception hierarchy and checks inner exceptions.

### Recommended Thresholds

| Scenario | FailureThreshold | BreakDuration | MinimumThroughput |
|----------|:----------------:|:-------------:|:-----------------:|
| Low-traffic API | 0.5 | 30s | 5 |
| High-traffic API | 0.3 | 60s | 20 |
| Background worker | 0.5 | 120s | 10 |
| Health check endpoint | 0.8 | 15s | 3 |

## Provider-Specific Behavior

### Pool Statistics by Provider

| Provider | ActiveConnections | IdleConnections | TotalConnections | MaxPoolSize |
|----------|:-----------------:|:---------------:|:----------------:|:-----------:|
| SQL Server (ADO/Dapper) | `RetrieveStatistics()` | `RetrieveStatistics()` | `RetrieveStatistics()` | Connection string |
| PostgreSQL (ADO/Dapper) | 0 | 0 | 0 | Connection string |
| MySQL (ADO/Dapper) | 0 | 0 | 0 | Connection string |
| SQLite (ADO/Dapper) | `CreateEmpty()` | `CreateEmpty()` | `CreateEmpty()` | `CreateEmpty()` |
| EF Core (all DBs) | `CreateEmpty()` | `CreateEmpty()` | `CreateEmpty()` | `CreateEmpty()` |
| MongoDB | Cluster servers | 0 | Server count | Server count |

### Pool Clearing by Provider

| Provider | Method |
|----------|--------|
| SQL Server | `SqlConnection.ClearAllPools()` |
| PostgreSQL | `NpgsqlConnection.ClearAllPools()` |
| MySQL | `MySqlConnection.ClearPoolAsync()` |
| SQLite | No-op (no traditional pooling) |
| EF Core | No-op (delegates to ADO.NET driver) |
| MongoDB | No-op (internal pool management) |

### Health Check Query

All relational providers execute `SELECT 1` via the base class `DatabaseHealthMonitorBase`. MongoDB uses the `ping` command on the admin database.

## Architecture

```
IDatabaseHealthMonitor (core interface, Encina package)
│
├── DatabaseHealthMonitorBase (abstract, Encina.Messaging)
│   ├── SqliteDatabaseHealthMonitor (Encina.ADO.Sqlite)
│   ├── SqlServerDatabaseHealthMonitor (Encina.ADO.SqlServer)
│   ├── PostgreSqlDatabaseHealthMonitor (Encina.ADO.PostgreSQL)
│   ├── MySqlDatabaseHealthMonitor (Encina.ADO.MySQL)
│   ├── DapperSqliteDatabaseHealthMonitor (Encina.Dapper.Sqlite)
│   ├── DapperSqlServerDatabaseHealthMonitor (Encina.Dapper.SqlServer)
│   ├── DapperPostgreSqlDatabaseHealthMonitor (Encina.Dapper.PostgreSQL)
│   ├── DapperMySqlDatabaseHealthMonitor (Encina.Dapper.MySQL)
│   └── EfCoreDatabaseHealthMonitor (Encina.EntityFrameworkCore)
│
└── MongoDbDatabaseHealthMonitor (Encina.MongoDB, direct implementation)
```

### Design Decisions

- **Template Method Pattern**: `DatabaseHealthMonitorBase` provides shared logic (`CheckHealthAsync`, `GetPoolStatistics`) and delegates provider-specific work to `GetPoolStatisticsCore()` and `ClearPoolCoreAsync()`
- **Volatile circuit state**: `IsCircuitOpen` uses `volatile bool` for lock-free atomic reads
- **Scoped connections**: Each `CheckHealthAsync` call creates a new scoped connection — no shared state between invocations
- **`TryAddSingleton`**: Providers register with `TryAddSingleton<IDatabaseHealthMonitor>` so the first registration wins when multiple providers are present (ADO.NET and Dapper share the same underlying pool)

## EF Core Interceptor

`ConnectionPoolMonitoringInterceptor` tracks connection lifecycle events:

```csharp
// Automatically registered when resilience is enabled
services.AddEncinaEntityFrameworkCore<AppDbContext>(config =>
{
    config.UseResilience(options =>
    {
        options.EnablePoolMonitoring = true;
    });
});

// Thread-safe counters
var interceptor = serviceProvider.GetService<ConnectionPoolMonitoringInterceptor>();
Console.WriteLine($"Total opened: {interceptor.TotalConnectionsOpened}");
Console.WriteLine($"Total failed: {interceptor.TotalConnectionsFailed}");
```

## OpenTelemetry Metrics

`DatabasePoolMetrics` exposes pool statistics as OpenTelemetry gauges:

| Metric Name | Type | Tags | Description |
|-------------|------|------|-------------|
| `encina.db.pool.active` | Gauge | `provider` | Active connections |
| `encina.db.pool.idle` | Gauge | `provider` | Idle connections |
| `encina.db.pool.total` | Gauge | `provider` | Total connections |
| `encina.db.pool.pending` | Gauge | `provider` | Pending requests |
| `encina.db.pool.utilization` | Gauge | `provider` | Pool utilization ratio |
| `encina.db.circuit.open` | Gauge | `provider` | Circuit breaker state (0/1) |

## Related

- **Issue**: [#290 - Connection Pool Resilience](https://github.com/dlrivada/Encina/issues/290)
- **Source**: `src/Encina/Database/` (core types), `src/Encina.Messaging/Health/` (base class), `src/Encina.Polly/` (circuit breaker)
- **Tests**: 189 tests across 5 test types (Unit, Guard, Contract, Property, Integration)
