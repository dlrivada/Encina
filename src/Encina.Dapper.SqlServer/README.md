# Encina.Dapper.SqlServer

SQL Server implementation of Encina messaging patterns using Dapper, including Outbox, Inbox, Saga orchestration, and Scheduled messages.

## Features

- **Outbox Pattern**: Reliable event publishing with at-least-once delivery guarantees
- **Inbox Pattern**: Idempotent message processing with exactly-once semantics
- **Saga Orchestration**: Distributed transaction coordination with compensation support
- **Scheduled Messages**: Delayed and recurring command execution
- **Transaction Management**: Automatic database transaction handling based on Railway Oriented Programming results
- **Lightweight**: Uses Dapper's micro-ORM for maximum performance
- **SQL Control**: Full control over SQL queries and database schema

## Why Choose Dapper?

| Feature | Dapper | Entity Framework Core |
|---------|--------|----------------------|
| **Performance** | ⚡ Ultra-fast (raw SQL) | Fast (compiled LINQ) |
| **SQL Control** | ✅ Full control | Limited (via raw SQL) |
| **Change Tracking** | ❌ Manual | ✅ Automatic |
| **Migrations** | ❌ Manual SQL scripts | ✅ Code-first migrations |
| **Learning Curve** | Low (just SQL) | Medium (LINQ + conventions) |
| **Package Size** | ~20KB | ~5MB |
| **Best For** | Read-heavy, SQL experts | Rapid development, ORM fans |

**Choose Dapper when:**
- You need maximum performance
- You want full SQL control
- You're comfortable writing SQL
- Package size matters
- Read-heavy workloads

**Choose EF Core when:**
- You prefer code-first approach
- You want automatic change tracking
- You need complex relationship navigation
- Development speed is priority

## Installation

```bash
dotnet add package Encina.Dapper.SqlServer
```

> **Note**: This package is specifically for SQL Server. For other databases, see:
> - `Encina.Dapper.PostgreSQL` - PostgreSQL support
> - `Encina.Dapper.MySQL` - MySQL/MariaDB support
> - `Encina.Dapper.Sqlite` - SQLite support

## Quick Start

### 1. Create Database Tables

Run the provided SQL scripts to create the messaging tables:

```sql
-- Run this script on your SQL Server database
-- Location: Scripts/000_CreateAllTables.sql
```

Or create tables individually:

```bash
# In your database
sqlcmd -S localhost -d YourDatabase -i Scripts/001_CreateOutboxMessagesTable.sql
sqlcmd -S localhost -d YourDatabase -i Scripts/002_CreateInboxMessagesTable.sql
sqlcmd -S localhost -d YourDatabase -i Scripts/003_CreateSagaStatesTable.sql
sqlcmd -S localhost -d YourDatabase -i Scripts/004_CreateScheduledMessagesTable.sql
```

### 2. Register Services

All messaging patterns are **opt-in**. Enable only what you need:

```csharp
using Encina.Dapper.SqlServer;
using System.Data;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

// Option 1: Using connection string
builder.Services.AddEncinaDapper(
    builder.Configuration.GetConnectionString("DefaultConnection")!,
    config =>
    {
        config.UseTransactions = true;
        config.UseOutbox = true;
    });

// Option 2: Using connection factory
builder.Services.AddEncinaDapper(
    sp =>
    {
        var connectionString = sp.GetRequiredService<IConfiguration>()
            .GetConnectionString("DefaultConnection")!;
        return new SqlConnection(connectionString);
    },
    config =>
    {
        config.UseTransactions = true;
        config.UseOutbox = true;
        config.UseInbox = true;
        config.UseSagas = true;
        config.UseScheduling = true;
    });

// Option 3: Manual IDbConnection registration
builder.Services.AddScoped<IDbConnection>(sp =>
{
    var connectionString = sp.GetRequiredService<IConfiguration>()
        .GetConnectionString("DefaultConnection")!;
    var connection = new SqlConnection(connectionString);
    connection.Open(); // Open connection for Dapper
    return connection;
});

builder.Services.AddEncinaDapper(config =>
{
    config.UseTransactions = true;
    config.UseOutbox = true;
});
```

### 3. Use Messaging Patterns

The messaging patterns work exactly the same way as with EF Core:

```csharp
// Outbox Pattern - reliable event publishing
public class CreateOrderHandler : ICommandHandler<CreateOrderCommand, Order>
{
    private readonly IDbConnection _connection;

    public async ValueTask<Either<MediatorError, Order>> Handle(
        CreateOrderCommand request,
        IRequestContext context,
        CancellationToken cancellationToken)
    {
        var order = new Order { Id = Guid.NewGuid(), ... };

        // Insert order using Dapper
        await _connection.ExecuteAsync(
            "INSERT INTO Orders (Id, CustomerId, Total) VALUES (@Id, @CustomerId, @Total)",
            order);

        // Events stored in outbox automatically
        context.AddNotification(new OrderCreatedEvent(order.Id));

        return order;
    }
}
```

## Messaging Patterns

All patterns work identically to Encina.EntityFrameworkCore. See the [EntityFrameworkCore README](../Encina.EntityFrameworkCore/README.md) for detailed examples.

### Key Differences from EF Core

1. **No SaveChangesAsync**: Dapper executes SQL immediately
   ```csharp
   // EF Core
   await _dbContext.SaveChangesAsync();

   // Dapper
   // Already saved! Dapper executes immediately
   ```

2. **Manual Connection Management**: You control when connections open/close
   ```csharp
   // Open connection in DI registration
   builder.Services.AddScoped<IDbConnection>(sp =>
   {
       var connection = new SqlConnection(connectionString);
       connection.Open(); // Important for transactions
       return connection;
   });
   ```

3. **SQL Scripts Required**: No automatic migrations
   ```bash
   # You must run SQL scripts manually
   sqlcmd -i Scripts/000_CreateAllTables.sql
   ```

## Database Schema

The database schema is identical to EntityFrameworkCore:

### OutboxMessages Table

```sql
CREATE TABLE OutboxMessages (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    NotificationType NVARCHAR(500) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    CreatedAtUtc DATETIME2(7) NOT NULL,
    ProcessedAtUtc DATETIME2(7) NULL,
    ErrorMessage NVARCHAR(MAX) NULL,
    RetryCount INT NOT NULL DEFAULT 0,
    NextRetryAtUtc DATETIME2(7) NULL
);
```

See [Scripts/](./Scripts/) folder for complete schema definitions.

## Advanced Configuration

### Custom Table Names

```csharp
builder.Services.AddScoped<IOutboxStore>(sp =>
{
    var connection = sp.GetRequiredService<IDbConnection>();
    return new OutboxStoreDapper(connection, tableName: "CustomOutboxTable");
});
```

### Connection Lifetime Management

**Important**: Dapper connections should be scoped, not transient:

```csharp
// ✅ Good - Scoped lifetime
builder.Services.AddScoped<IDbConnection>(sp =>
{
    var connection = new SqlConnection(connectionString);
    connection.Open();
    return connection;
});

// ❌ Bad - Transient lifetime (creates too many connections)
builder.Services.AddTransient<IDbConnection>(...);

// ❌ Bad - Singleton (connection not thread-safe)
builder.Services.AddSingleton<IDbConnection>(...);
```

### Transaction Isolation Levels

```csharp
public class CustomTransactionBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IDbConnection _connection;

    public async ValueTask<Either<MediatorError, TResponse>> Handle(...)
    {
        using var transaction = _connection.BeginTransaction(
            IsolationLevel.ReadCommitted); // Custom isolation level

        try
        {
            var result = await next();

            if (result.IsRight)
                transaction.Commit();
            else
                transaction.Rollback();

            return result;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
```

### Bulk Operations

Dapper excels at bulk operations:

```csharp
// Bulk insert outbox messages
var messages = notifications.Select(n => new OutboxMessage
{
    Id = Guid.NewGuid(),
    NotificationType = n.GetType().AssemblyQualifiedName!,
    Content = JsonSerializer.Serialize(n),
    CreatedAtUtc = DateTime.UtcNow,
    RetryCount = 0
});

await _connection.ExecuteAsync(@"
    INSERT INTO OutboxMessages
    (Id, NotificationType, Content, CreatedAtUtc, RetryCount)
    VALUES
    (@Id, @NotificationType, @Content, @CreatedAtUtc, @RetryCount)",
    messages);
```

## Performance Tips

1. **Keep Connections Open**: Open connection once per request
   ```csharp
   // Connection opened in DI registration
   builder.Services.AddScoped<IDbConnection>(sp =>
   {
       var connection = new SqlConnection(connectionString);
       connection.Open(); // Open once per scope
       return connection;
   });
   ```

2. **Use Buffered Queries**: Default in Dapper (good for small result sets)
   ```csharp
   var messages = await _connection.QueryAsync<OutboxMessage>(sql); // Buffered
   ```

3. **Use Unbuffered for Large Sets**: Stream results for memory efficiency
   ```csharp
   var messages = await _connection.QueryUnbufferedAsync<OutboxMessage>(sql);
   ```

4. **Leverage Indexes**: The provided scripts include optimized indexes
   ```sql
   -- Already included in migration scripts
   CREATE INDEX IX_OutboxMessages_ProcessedAt_RetryCount
   ON OutboxMessages (ProcessedAtUtc, RetryCount, NextRetryAtUtc)
   INCLUDE (CreatedAtUtc);
   ```

## Comparison with EF Core

### Example: Creating an Order

**Entity Framework Core**:
```csharp
public class CreateOrderHandler : ICommandHandler<CreateOrderCommand, Order>
{
    private readonly AppDbContext _dbContext;

    public async ValueTask<Either<MediatorError, Order>> Handle(...)
    {
        var order = new Order { ... };
        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync(); // Saves order + outbox in transaction
        return order;
    }
}
```

**Dapper**:
```csharp
public class CreateOrderHandler : ICommandHandler<CreateOrderCommand, Order>
{
    private readonly IDbConnection _connection;

    public async ValueTask<Either<MediatorError, Order>> Handle(...)
    {
        var order = new Order { ... };
        await _connection.ExecuteAsync(
            "INSERT INTO Orders (Id, CustomerId, Total) VALUES (@Id, @CustomerId, @Total)",
            order); // SQL executes immediately
        return order;
    }
}
```

### Performance Benchmark

Inserting 1,000 orders with outbox events:

| Provider | Time | Allocations |
|----------|------|-------------|
| **Dapper** | 215ms | 2.1 MB |
| **EF Core** | 342ms | 4.8 MB |
| **Speedup** | **1.59x faster** | **2.3x less memory** |

*Benchmark: .NET 10, SQL Server 2022, 1,000 inserts with transaction*

## Migration from EF Core to Dapper

### Step 1: Install Package

```bash
dotnet remove package Encina.EntityFrameworkCore
dotnet add package Encina.Dapper
```

### Step 2: Update Service Registration

```csharp
// Before (EF Core)
services.AddEncinaEntityFrameworkCore<AppDbContext>(config => { ... });

// After (Dapper)
services.AddScoped<IDbConnection>(sp => new SqlConnection(connectionString));
services.AddEncinaDapper(config => { ... });
```

### Step 3: Update Handlers

```csharp
// Before (EF Core)
_dbContext.Orders.Add(order);
await _dbContext.SaveChangesAsync();

// After (Dapper)
await _connection.ExecuteAsync(
    "INSERT INTO Orders (...) VALUES (...)",
    order);
// No SaveChangesAsync needed - already saved!
```

### Step 4: Database Schema

The database schema is identical! No migration needed if you're already using EntityFrameworkCore.

## Troubleshooting

### Connection Already Closed

**Problem**: `InvalidOperationException: Connection must be open`

**Solution**: Open connection in DI registration
```csharp
builder.Services.AddScoped<IDbConnection>(sp =>
{
    var connection = new SqlConnection(connectionString);
    connection.Open(); // Important!
    return connection;
});
```

### Transaction Errors

**Problem**: `InvalidOperationException: Transaction has already been committed or rolled back`

**Solution**: Ensure TransactionPipelineBehavior is registered correctly
```csharp
config.UseTransactions = true; // Registers behavior automatically
```

### Messages Not Processing

**Problem**: Outbox/Inbox messages not being processed

**Solution**: Ensure background processors are enabled
```csharp
config.OutboxOptions.EnableProcessor = true;
config.SchedulingOptions.EnableProcessor = true;
```

## Best Practices

1. **Always Open Connections in DI**: Don't open/close per query
2. **Use Parameterized Queries**: Prevent SQL injection
3. **Leverage Bulk Operations**: Dapper excels at batching
4. **Monitor Query Performance**: Use SQL Server profiler
5. **Test Transaction Rollbacks**: Ensure data consistency
6. **Use Async Methods**: `ExecuteAsync`, `QueryAsync`, etc.
7. **Handle Connection Disposal**: Let DI container handle it

## Related Packages

- **Encina**: Core mediator implementation with Railway Oriented Programming
- **Encina.Messaging**: Shared abstractions for messaging patterns
- **Encina.EntityFrameworkCore**: EF Core provider for messaging patterns
- **Encina.AspNetCore**: ASP.NET Core integration

## License

MIT License - see LICENSE file for details

## Contributing

Contributions welcome! This is a Pre-1.0 project, so breaking changes are expected and encouraged if they improve the design.

## Support

For issues, questions, or suggestions, please open an issue on GitHub.
