# Read/Write Database Separation in Encina

This guide explains how to implement read/write database separation (CQRS physical split) in Encina applications. This pattern routes query operations to read replicas and command operations to the primary database, improving scalability and performance.

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Configuration](#configuration)
4. [Provider-Specific Setup](#provider-specific-setup)
5. [Replica Selection Strategies](#replica-selection-strategies)
6. [ForceWriteDatabase Attribute](#forcewritedatabase-attribute)
7. [Replication Lag Considerations](#replication-lag-considerations)
8. [Health Checks](#health-checks)
9. [Combined with Multi-Tenancy](#combined-with-multi-tenancy)
10. [Combined with Module Isolation](#combined-with-module-isolation)
11. [FAQ](#faq)

---

## Overview

Read/write separation routes database operations based on their intent:

| Operation Type | Intent | Target | Use Case |
|---------------|--------|--------|----------|
| `ICommand<T>` | Write | Primary | Inserts, updates, deletes |
| `IQuery<T>` | Read | Replica(s) | Read-only queries |
| `IQuery<T>` with `[ForceWriteDatabase]` | ForceWrite | Primary | Read-after-write consistency |

> **Key Benefit**: Encina's read/write separation works identically across EF Core, Dapper, ADO.NET, and MongoDB. Switch providers without changing your application logic.

### Why Read/Write Separation?

1. **Scale reads independently**: Add read replicas without affecting write performance
2. **Reduce primary load**: Offload reporting and analytics queries to replicas
3. **Geographic distribution**: Place replicas closer to users for lower latency
4. **Improved availability**: Read operations continue during primary failover
5. **Cost optimization**: Use smaller primary, larger read replicas

### How It Works

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           Application                                    │
├─────────────────────────────────────────────────────────────────────────┤
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │                    Encina Mediator Pipeline                       │   │
│  │  ┌─────────────────────────────────────────────────────────────┐ │   │
│  │  │         ReadWriteRoutingPipelineBehavior                    │ │   │
│  │  │                                                             │ │   │
│  │  │  ICommand<T>  ──────────►  DatabaseIntent.Write             │ │   │
│  │  │  IQuery<T>    ──────────►  DatabaseIntent.Read              │ │   │
│  │  │  [ForceWrite] ──────────►  DatabaseIntent.ForceWrite        │ │   │
│  │  └─────────────────────────────────────────────────────────────┘ │   │
│  └──────────────────────────────────────────────────────────────────┘   │
│         │                           │                    │              │
│         ▼                           ▼                    ▼              │
│  ┌─────────────┐           ┌─────────────┐      ┌─────────────┐        │
│  │   Write     │           │    Read     │      │ ForceWrite  │        │
│  │   Intent    │           │   Intent    │      │   Intent    │        │
│  └──────┬──────┘           └──────┬──────┘      └──────┬──────┘        │
│         │                         │                    │               │
│  ═══════╪═════════════════════════╪════════════════════╪═══  Routing   │
│         │                         │                    │               │
│         ▼                         ▼                    ▼               │
│  ┌─────────────┐           ┌─────────────┐      ┌─────────────┐        │
│  │   PRIMARY   │           │   REPLICA   │      │   PRIMARY   │        │
│  │  Database   │───sync───►│  Database   │      │  Database   │        │
│  │  (writes)   │           │  (reads)    │      │ (consistent)│        │
│  └─────────────┘           └─────────────┘      └─────────────┘        │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Architecture

### Core Abstractions

The read/write separation feature is built on these core types in `Encina.Messaging.ReadWriteSeparation`:

```csharp
// Intent enum - determines where the operation should be routed
public enum DatabaseIntent
{
    Read,       // Route to read replica
    Write,      // Route to primary (default)
    ForceWrite  // Force primary even for queries
}

// Routing context - AsyncLocal storage for current intent
public static class DatabaseRoutingContext
{
    public static DatabaseIntent CurrentIntent { get; }
    public static void SetIntent(DatabaseIntent intent);
}

// Scoped intent - sets intent for duration of scope
public sealed class DatabaseRoutingScope : IDisposable
{
    public DatabaseRoutingScope(DatabaseIntent intent);
    public void Dispose(); // Restores previous intent
}

// Attribute for queries requiring primary access
[AttributeUsage(AttributeTargets.Class)]
public sealed class ForceWriteDatabaseAttribute : Attribute
{
    public string? Reason { get; init; }
}
```

### Pipeline Behavior

Each provider includes a `ReadWriteRoutingPipelineBehavior<TRequest, TResponse>` that:

1. Inspects the request type (`ICommand<T>` vs `IQuery<T>`)
2. Checks for `[ForceWriteDatabase]` attribute
3. Creates a `DatabaseRoutingScope` with appropriate intent
4. Executes the handler within that scope
5. Restores the previous context after completion

```csharp
public sealed class ReadWriteRoutingPipelineBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        var intent = DetermineIntent(request);

        using var scope = new DatabaseRoutingScope(intent);

        return await nextStep().ConfigureAwait(false);
    }
}
```

---

## Configuration

### EF Core Configuration

```csharp
services.AddEncinaEntityFrameworkCore<AppDbContext>(options =>
{
    options.UseReadWriteSeparation = true;
    options.ReadWriteSeparationOptions.ReplicaConnectionStrings = new[]
    {
        "Server=replica1;Database=MyApp;...",
        "Server=replica2;Database=MyApp;..."
    };
    options.ReadWriteSeparationOptions.ReplicaSelectionStrategy =
        ReplicaSelectionStrategy.RoundRobin;
    options.ReadWriteSeparationOptions.ValidateOnStartup = true;
});
```

### Dapper Configuration

```csharp
services.AddEncinaDapper(options =>
{
    options.ConnectionString = "Server=primary;Database=MyApp;...";
    options.UseReadWriteSeparation = true;
    options.ReadWriteSeparationOptions.ReplicaConnectionStrings = new[]
    {
        "Server=replica1;Database=MyApp;...",
        "Server=replica2;Database=MyApp;..."
    };
    options.ReadWriteSeparationOptions.ReplicaSelectionStrategy =
        ReplicaSelectionStrategy.LeastConnections;
});
```

### ADO.NET Configuration

```csharp
services.AddEncinaADO(options =>
{
    options.ConnectionString = "Server=primary;Database=MyApp;...";
    options.UseReadWriteSeparation = true;
    options.ReadWriteSeparationOptions.ReplicaConnectionStrings = new[]
    {
        "Server=replica1;Database=MyApp;..."
    };
    options.ReadWriteSeparationOptions.ReplicaSelectionStrategy =
        ReplicaSelectionStrategy.Random;
});
```

### MongoDB Configuration

MongoDB uses read preferences instead of separate connection strings:

```csharp
services.AddEncinaMongoDB(options =>
{
    // Connection string should include replica set
    options.ConnectionString = "mongodb://primary,secondary1,secondary2/?replicaSet=rs0";
    options.DatabaseName = "MyApp";
    options.UseReadWriteSeparation = true;

    // MongoDB-specific read preference settings
    options.ReadWriteSeparationOptions.ReadPreference = MongoReadPreference.SecondaryPreferred;
    options.ReadWriteSeparationOptions.ReadConcern = MongoReadConcern.Majority;
    options.ReadWriteSeparationOptions.MaxStaleness = TimeSpan.FromMinutes(2);
});
```

---

## Provider-Specific Setup

### EF Core Setup

EF Core uses `IReadWriteDbContextFactory<TContext>` for creating context instances with appropriate connections:

```csharp
// Repository using read/write separation
public class OrderRepository : IOrderRepository
{
    private readonly IReadWriteDbContextFactory<AppDbContext> _contextFactory;

    public OrderRepository(IReadWriteDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    // Query handler - automatically uses read replica
    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        // GetContextAsync checks DatabaseRoutingContext.CurrentIntent
        await using var context = await _contextFactory.GetContextAsync(ct);
        return await context.Orders.FindAsync([id], ct);
    }

    // Command handler - automatically uses primary
    public async Task AddAsync(Order order, CancellationToken ct)
    {
        await using var context = await _contextFactory.GetContextAsync(ct);
        context.Orders.Add(order);
        await context.SaveChangesAsync(ct);
    }
}
```

### Dapper Setup

Dapper uses `IReadWriteConnectionFactory` for obtaining connections:

```csharp
public class ProductRepository : IProductRepository
{
    private readonly IReadWriteConnectionFactory _connectionFactory;

    public ProductRepository(IReadWriteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    // Query - uses read replica
    public async Task<IEnumerable<Product>> GetAllAsync(CancellationToken ct)
    {
        await using var connection = await _connectionFactory.GetConnectionAsync(ct);
        return await connection.QueryAsync<Product>(
            "SELECT * FROM Products WHERE IsActive = 1");
    }

    // Command - uses primary
    public async Task UpdatePriceAsync(Guid id, decimal newPrice, CancellationToken ct)
    {
        await using var connection = await _connectionFactory.GetConnectionAsync(ct);
        await connection.ExecuteAsync(
            "UPDATE Products SET Price = @Price WHERE Id = @Id",
            new { Price = newPrice, Id = id });
    }
}
```

### ADO.NET Setup

ADO.NET uses `IReadWriteConnectionFactory` similarly:

```csharp
public class CustomerRepository : ICustomerRepository
{
    private readonly IReadWriteConnectionFactory _connectionFactory;

    public CustomerRepository(IReadWriteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    // Explicit read/write methods
    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        // GetReadConnectionAsync always returns read replica
        await using var connection = await _connectionFactory.GetReadConnectionAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Customers WHERE Id = @Id";
        command.Parameters.Add(new SqlParameter("@Id", id));

        await using var reader = await command.ExecuteReaderAsync(ct);
        return reader.Read() ? MapCustomer(reader) : null;
    }

    public async Task CreateAsync(Customer customer, CancellationToken ct)
    {
        // GetWriteConnectionAsync always returns primary
        await using var connection = await _connectionFactory.GetWriteConnectionAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO Customers (Id, Name, Email) VALUES (@Id, @Name, @Email)";
        // ... add parameters
        await command.ExecuteNonQueryAsync(ct);
    }
}
```

### MongoDB Setup

MongoDB uses `IReadWriteMongoCollectionFactory` with read preferences:

```csharp
public class InventoryRepository : IInventoryRepository
{
    private readonly IReadWriteMongoCollectionFactory _collectionFactory;

    public InventoryRepository(IReadWriteMongoCollectionFactory collectionFactory)
    {
        _collectionFactory = collectionFactory;
    }

    // Query - uses SecondaryPreferred read preference
    public async Task<InventoryItem?> GetBySkuAsync(string sku, CancellationToken ct)
    {
        // GetCollectionAsync checks DatabaseRoutingContext.CurrentIntent
        var collection = await _collectionFactory.GetCollectionAsync<InventoryItem>(
            "inventory", ct);

        return await collection
            .Find(i => i.Sku == sku)
            .FirstOrDefaultAsync(ct);
    }

    // Command - uses Primary read preference
    public async Task UpdateQuantityAsync(string sku, int delta, CancellationToken ct)
    {
        var collection = await _collectionFactory.GetCollectionAsync<InventoryItem>(
            "inventory", ct);

        await collection.UpdateOneAsync(
            i => i.Sku == sku,
            Builders<InventoryItem>.Update.Inc(i => i.Quantity, delta),
            cancellationToken: ct);
    }

    // Explicit read collection
    public async Task<IEnumerable<InventoryItem>> SearchAsync(string query, CancellationToken ct)
    {
        // GetReadCollectionAsync always uses configured read preference
        var collection = await _collectionFactory.GetReadCollectionAsync<InventoryItem>(
            "inventory", ct);

        return await collection
            .Find(Builders<InventoryItem>.Filter.Text(query))
            .ToListAsync(ct);
    }
}
```

---

## Replica Selection Strategies

For SQL providers (EF Core, Dapper, ADO.NET), three replica selection strategies are available:

### RoundRobin (Default)

Distributes requests evenly across all replicas in sequence:

```
Request 1 → Replica A
Request 2 → Replica B
Request 3 → Replica C
Request 4 → Replica A
...
```

```csharp
options.ReadWriteSeparationOptions.ReplicaSelectionStrategy =
    ReplicaSelectionStrategy.RoundRobin;
```

**Best for**: Uniform replica capacity, balanced workloads.

### Random

Randomly selects a replica for each request:

```
Request 1 → Replica B
Request 2 → Replica A
Request 3 → Replica A
Request 4 → Replica C
...
```

```csharp
options.ReadWriteSeparationOptions.ReplicaSelectionStrategy =
    ReplicaSelectionStrategy.Random;
```

**Best for**: Simple distribution, avoiding hot spots.

### LeastConnections

Routes to the replica with the fewest active connections:

```
Replica A: 5 connections
Replica B: 3 connections  ← Selected
Replica C: 8 connections

Request → Replica B
```

```csharp
options.ReadWriteSeparationOptions.ReplicaSelectionStrategy =
    ReplicaSelectionStrategy.LeastConnections;
```

**Best for**: Variable query complexity, mixed workloads.

### MongoDB Read Preferences

MongoDB uses native read preferences instead of replica selection:

| Preference | Behavior |
|------------|----------|
| `Primary` | Always read from primary |
| `PrimaryPreferred` | Prefer primary, fallback to secondary |
| `Secondary` | Always read from secondary |
| `SecondaryPreferred` | Prefer secondary, fallback to primary (default) |
| `Nearest` | Read from lowest latency member |

```csharp
options.ReadWriteSeparationOptions.ReadPreference = MongoReadPreference.SecondaryPreferred;
```

---

## ForceWriteDatabase Attribute

Use `[ForceWriteDatabase]` for queries that require read-after-write consistency:

```csharp
// Standard query - routes to replica
public sealed record GetProductsQuery : IQuery<IReadOnlyList<Product>>;

// Force primary - for read-after-write consistency
[ForceWriteDatabase(Reason = "Must read latest inventory after update")]
public sealed record GetInventoryAfterUpdateQuery(Guid ProductId) : IQuery<Inventory>;

// Common patterns requiring ForceWrite:
// - Reading immediately after writing
// - Business-critical reads that cannot tolerate stale data
// - Transactional reads that must see uncommitted changes
```

### When to Use ForceWriteDatabase

| Scenario | Use ForceWriteDatabase? | Reason |
|----------|-------------------------|--------|
| Dashboard analytics | No | Stale data acceptable |
| Order confirmation display | Yes | User just placed order |
| Inventory check before purchase | Yes | Must be accurate |
| Product catalog browsing | No | Slightly stale OK |
| User profile after update | Yes | User expects changes |
| Report generation | No | Historical data OK |

### Implementation Pattern

```csharp
// Command handler that writes data
public class UpdateInventoryHandler
    : ICommandHandler<UpdateInventoryCommand, InventoryUpdated>
{
    public async ValueTask<Either<EncinaError, InventoryUpdated>> Handle(
        UpdateInventoryCommand command,
        IRequestContext context,
        CancellationToken ct)
    {
        // Write to primary
        await _repository.UpdateQuantityAsync(command.Sku, command.Delta, ct);

        return new InventoryUpdated(command.Sku);
    }
}

// Query handler that needs consistency
[ForceWriteDatabase(Reason = "Read-after-write for inventory confirmation")]
public sealed record GetCurrentInventoryQuery(string Sku) : IQuery<Inventory>;

public class GetCurrentInventoryHandler
    : IQueryHandler<GetCurrentInventoryQuery, Inventory>
{
    public async ValueTask<Either<EncinaError, Inventory>> Handle(
        GetCurrentInventoryQuery query,
        IRequestContext context,
        CancellationToken ct)
    {
        // Automatically routed to primary due to ForceWriteDatabase
        var inventory = await _repository.GetBySkuAsync(query.Sku, ct);

        return inventory is not null
            ? inventory
            : EncinaError.New("Inventory not found");
    }
}
```

---

## Replication Lag Considerations

Replication lag is the delay between writes on the primary and their visibility on replicas.

### Understanding Replication Lag

```
┌─────────────┐                              ┌─────────────┐
│   PRIMARY   │                              │   REPLICA   │
│             │                              │             │
│ T0: Write   │───── Replication Delay ─────►│ T0+Δ: Read  │
│    Order #1 │         (5-500ms)            │    Order #1 │
│             │                              │             │
└─────────────┘                              └─────────────┘

During lag window (T0 to T0+Δ):
- Primary: Order #1 exists
- Replica: Order #1 does NOT exist yet
```

### Typical Lag Values

| Database | Typical Lag | Maximum Lag |
|----------|-------------|-------------|
| SQL Server Always On | 1-10ms | 100ms |
| PostgreSQL Streaming | 5-50ms | 500ms |
| MySQL Replication | 10-100ms | 1-5s |
| MongoDB Replica Set | 1-100ms | seconds |

### Mitigating Replication Lag

1. **Use ForceWriteDatabase for critical reads**:

```csharp
[ForceWriteDatabase(Reason = "Order confirmation must be accurate")]
public sealed record GetOrderConfirmationQuery(Guid OrderId) : IQuery<OrderConfirmation>;
```

1. **Configure MaxStaleness for MongoDB**:

```csharp
options.ReadWriteSeparationOptions.MaxStaleness = TimeSpan.FromSeconds(30);
```

1. **Use causal consistency (MongoDB)**:

```csharp
// MongoDB sessions provide causal consistency
await using var session = await _client.StartSessionAsync();
session.StartTransaction(new TransactionOptions(
    readConcern: ReadConcern.Majority,
    writeConcern: WriteConcern.WMajority));
```

1. **Implement retry with primary fallback**:

```csharp
public async Task<Order?> GetOrderWithFallbackAsync(Guid id, CancellationToken ct)
{
    // Try replica first
    var order = await _replicaRepository.GetByIdAsync(id, ct);

    if (order is null)
    {
        // Fallback to primary (may be replication lag)
        order = await _primaryRepository.GetByIdAsync(id, ct);
    }

    return order;
}
```

---

## Health Checks

Each provider registers health checks for monitoring read/write separation status.

### SQL Provider Health Checks

```csharp
// Health check verifies:
// - Primary connection is available
// - At least one replica is available
// - Replication lag is within acceptable bounds (if configured)

services.AddHealthChecks()
    .AddCheck<ReadWriteSeparationHealthCheck>("read-write-separation");
```

Health status meanings:

| Status | Condition | Action |
|--------|-----------|--------|
| Healthy | Primary + ≥1 replica available | Normal operation |
| Degraded | Primary available, no replicas | All reads go to primary |
| Unhealthy | Primary unavailable | Critical - failover needed |

### MongoDB Health Check

```csharp
// MongoDB health check inspects replica set topology:
// - Checks IMongoClient.Cluster.Description
// - Verifies primary and secondary availability

public sealed class ReadWriteMongoHealthCheck : EncinaHealthCheck
{
    protected override Task<HealthCheckResult> CheckHealthCoreAsync(CancellationToken ct)
    {
        var description = _mongoClient.Cluster.Description;

        // Count members by type
        var primaryCount = description.Servers.Count(s => s.Type == ServerType.ReplicaSetPrimary);
        var secondaryCount = description.Servers.Count(s => s.Type == ServerType.ReplicaSetSecondary);

        if (primaryCount == 0)
            return Unhealthy("No primary available");

        if (secondaryCount == 0)
            return Degraded("No secondaries available, all reads use primary");

        return Healthy($"1 primary, {secondaryCount} secondary(ies) available");
    }
}
```

### Monitoring Dashboard

```csharp
// Example: Expose metrics for monitoring
app.MapGet("/metrics/read-write", async (IEncinaHealthCheck[] checks) =>
{
    var rwCheck = checks.OfType<ReadWriteSeparationHealthCheck>().First();
    var result = await rwCheck.CheckHealthAsync();

    return new
    {
        Status = result.Status.ToString(),
        PrimaryAvailable = result.Data["primary_available"],
        ReplicaCount = result.Data["replica_count"],
        ActiveStrategy = result.Data["strategy"]
    };
});
```

---

## Combined with Multi-Tenancy

Read/write separation works seamlessly with multi-tenancy:

```csharp
// Configure both features
services.AddEncinaEntityFrameworkCore<AppDbContext>(options =>
{
    // Multi-tenancy settings
    options.UseTenancy = true;
    options.TenancyOptions.Strategy = TenantIsolationStrategy.SchemaPerTenant;

    // Read/write separation settings
    options.UseReadWriteSeparation = true;
    options.ReadWriteSeparationOptions.ReplicaConnectionStrings = new[]
    {
        "Server=replica1;Database=MyApp;...",
        "Server=replica2;Database=MyApp;..."
    };
});
```

### Routing Order

When both features are enabled, the routing order is:

```
Request
   │
   ▼
┌─────────────────────────┐
│ TenantRoutingBehavior   │  ← Determines tenant context
└───────────┬─────────────┘
            │
            ▼
┌─────────────────────────┐
│ ReadWriteRoutingBehavior│  ← Determines read/write intent
└───────────┬─────────────┘
            │
            ▼
┌─────────────────────────┐
│ Connection Selection    │
│ - Tenant's connection   │
│ - Primary or Replica    │
└─────────────────────────┘
```

### Per-Tenant Replica Configuration

```csharp
// Advanced: Different replicas per tenant
options.TenancyOptions.Tenants = new Dictionary<string, TenantConfiguration>
{
    ["tenant-a"] = new TenantConfiguration
    {
        ConnectionString = "Server=primary-a;...",
        ReadReplicas = new[] { "Server=replica-a1;...", "Server=replica-a2;..." }
    },
    ["tenant-b"] = new TenantConfiguration
    {
        ConnectionString = "Server=primary-b;...",
        ReadReplicas = new[] { "Server=replica-b1;..." }
    }
};
```

---

## Combined with Module Isolation

Read/write separation also works with module isolation:

```csharp
// Configure all three features
services.AddEncinaDapper(options =>
{
    options.ConnectionString = "Server=primary;Database=MyApp;...";

    // Module isolation
    options.UseModuleIsolation = true;
    options.ModuleIsolationOptions.Strategy = ModuleIsolationStrategy.SchemaWithPermissions;

    // Read/write separation
    options.UseReadWriteSeparation = true;
    options.ReadWriteSeparationOptions.ReplicaConnectionStrings = new[]
    {
        "Server=replica1;Database=MyApp;..."
    };
});
```

### Connection Resolution

```
┌─────────────────────────────────────────────────────────────────┐
│                     Connection Resolution                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  1. Module Context    →  Determines allowed schemas              │
│  2. Read/Write Intent →  Selects primary or replica              │
│  3. Replica Strategy  →  Selects specific replica (if multiple)  │
│                                                                  │
│  Result: Connection with correct permissions + target server     │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## FAQ

### General Questions

**Q: Does read/write separation require code changes?**

A: No. Once configured, the `ReadWriteRoutingPipelineBehavior` automatically routes based on request type. Existing `ICommand<T>` and `IQuery<T>` handlers work without modification.

**Q: What happens if all replicas are unavailable?**

A: The health check reports "Degraded" status, and all operations automatically fall back to the primary database. No data loss or errors occur.

**Q: Can I use read/write separation without Encina's mediator?**

A: Yes. You can use `IReadWriteConnectionFactory` or `IReadWriteDbContextFactory` directly and call `GetReadConnectionAsync()` / `GetWriteConnectionAsync()` explicitly.

### SQL Provider Questions

**Q: How do I handle transactions that span multiple queries?**

A: Transactions always use the primary database. When you call `BeginTransactionAsync()`, all subsequent operations in that transaction use the primary, regardless of whether they're queries or commands.

**Q: What's the overhead of read/write separation?**

A: Minimal. The routing decision is a simple type check and attribute lookup. Connection pooling is maintained per connection string.

**Q: Can I configure different replicas for different query types?**

A: Not directly. Use custom `IReadWriteConnectionFactory` implementations for advanced routing needs.

### MongoDB Questions

**Q: Why doesn't MongoDB use separate connection strings for replicas?**

A: MongoDB's driver handles replica routing internally via read preferences. The replica set topology is discovered automatically from the connection string.

**Q: What's the difference between Secondary and SecondaryPreferred?**

A: `Secondary` fails if no secondaries are available. `SecondaryPreferred` falls back to primary, making it more resilient.

**Q: How does MaxStaleness work?**

A: MongoDB won't route reads to secondaries that are more than `MaxStaleness` behind the primary. This prevents reading very stale data.

### Performance Questions

**Q: How do I measure the benefit of read/write separation?**

A: Monitor these metrics:

- Primary database CPU/IO utilization (should decrease)
- Query latency from different geographic regions
- Replica lag times
- Connection pool utilization per server

**Q: Should I use read/write separation for a small application?**

A: Generally no. The complexity outweighs benefits for applications with < 100 queries/second. Consider when:

- Primary database is a bottleneck
- You need geographic distribution
- Read workload significantly exceeds write workload

### Troubleshooting

**Q: My queries are still hitting the primary. Why?**

A: Check:

1. `UseReadWriteSeparation = true` in configuration
2. Request implements `IQuery<T>`, not just `IRequest<T>`
3. No `[ForceWriteDatabase]` attribute on the query
4. Health check shows replicas available

**Q: How do I debug routing decisions?**

A: Enable debug logging:

```csharp
services.AddLogging(builder => builder
    .AddFilter("Encina.*.ReadWriteSeparation", LogLevel.Debug));
```

Log output shows routing decisions:

```
[GetProductsQuery] Setting database intent to Read (CorrelationId: abc-123)
[CreateOrderCommand] Setting database intent to Write (CorrelationId: def-456)
```

---

## Related Documentation

- [Multi-Tenancy Guide](multi-tenancy.md)
- [Module Isolation Guide](module-isolation.md)
- [Health Checks Overview](../guides/health-checks.md)
- [Connection Management](../guides/connection-management.md)

---

## Related Issues

- [#283 - Read/Write Database Separation (CQRS Physical Split)](https://github.com/dlrivada/Encina/issues/283)
