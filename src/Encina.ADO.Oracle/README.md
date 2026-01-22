# Encina.ADO.Oracle

Oracle Database implementation of Encina messaging patterns using raw ADO.NET for maximum performance.
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://opensource.org/licenses/MIT)

**Pure ADO.NET provider for Encina messaging patterns** - Zero external dependencies (except Oracle.ManagedDataAccess.Core), maximum performance, and complete control over SQL execution.

Encina.ADO.Oracle implements messaging patterns (Outbox, Inbox, Transactions) using raw ADO.NET with OracleCommand and OracleDataReader, offering the lightest possible overhead and full SQL transparency.

## Features

- **✅ Zero Dependencies**: Only Oracle.ManagedDataAccess.Core (no ORMs, no micro-ORMs)
- **✅ Maximum Performance**: Raw OracleCommand/OracleDataReader execution
- **✅ Full SQL Control**: Complete visibility into executed queries
- **✅ Outbox Pattern**: At-least-once delivery for reliable event publishing
- **✅ Inbox Pattern**: Exactly-once semantics for idempotent processing
- **✅ Transaction Management**: Automatic commit/rollback based on ROP results
- **✅ Bulk Operations**: High-performance bulk inserts using Oracle Array Binding
- **✅ Railway Oriented Programming**: Native `Either<EncinaError, T>` support
- **✅ Oracle Optimized**: Bind variables, optimized indexes, PL/SQL support
- **✅ .NET 10 Native**: Built for modern .NET with nullable reference types

## Installation

```bash
dotnet add package Encina.ADO.Oracle
```

## Quick Start

### 1. Basic Setup

```csharp
using Encina.ADO.Oracle;

// Register with connection string
services.AddEncinaADOOracle(
    connectionString: "User Id=myuser;Password=mypass;Data Source=localhost:1521/XEPDB1;",
    configure: config =>
    {
        config.UseOutbox = true;
        config.UseInbox = true;
        config.UseTransactions = true;
    });

// Or with custom IDbConnection factory
services.AddEncinaADOOracle(
    connectionFactory: sp =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        return new OracleConnection(config.GetConnectionString("Default"));
    },
    configure: config =>
    {
        config.UseOutbox = true;
        config.UseInbox = true;
        config.UseTransactions = true;
    });
```

### 2. Database Schema

Run the SQL migration scripts in order:

```bash
# Option 1: Run all at once
sqlplus myuser/mypass@localhost:1521/XEPDB1 @Scripts/000_CreateAllTables.sql

# Option 2: Run individually
sqlplus myuser/mypass@localhost:1521/XEPDB1 @Scripts/001_CreateOutboxMessagesTable.sql
sqlplus myuser/mypass@localhost:1521/XEPDB1 @Scripts/002_CreateInboxMessagesTable.sql
sqlplus myuser/mypass@localhost:1521/XEPDB1 @Scripts/003_CreateSagaStatesTable.sql
sqlplus myuser/mypass@localhost:1521/XEPDB1 @Scripts/004_CreateScheduledMessagesTable.sql
```

### 3. Outbox Pattern (Reliable Event Publishing)

```csharp
// Define your domain events
public record OrderCreatedEvent(Guid OrderId, decimal Total) : INotification;

// Implement IHasNotifications on your command
public record CreateOrderCommand(decimal Total) : ICommand<Order>, IHasNotifications
{
    private readonly List<INotification> _notifications = new();

    public void AddNotification(INotification notification)
        => _notifications.Add(notification);

    public IEnumerable<INotification> GetNotifications() => _notifications;
}

// Handler emits domain events
public class CreateOrderHandler : ICommandHandler<CreateOrderCommand, Order>
{
    public async ValueTask<Either<EncinaError, Order>> Handle(
        CreateOrderCommand request,
        IRequestContext context,
        CancellationToken cancellationToken)
    {
        var order = new Order { Total = request.Total };

        // Instead of publishing immediately, add to outbox
        request.AddNotification(new OrderCreatedEvent(order.Id, order.Total));

        return order;
    }
}

// Events are:
// 1. Stored in OutboxMessages table (same transaction as domain changes)
// 2. Processed by background OutboxProcessor
// 3. Published through Encina with retry logic
```

**Outbox Configuration**:

```csharp
config.UseOutbox = true;
config.OutboxOptions.ProcessingInterval = TimeSpan.FromSeconds(5);
config.OutboxOptions.BatchSize = 100;
config.OutboxOptions.MaxRetries = 3;
config.OutboxOptions.BaseRetryDelay = TimeSpan.FromSeconds(5);
```

### 4. Inbox Pattern (Idempotent Processing)

```csharp
// Mark command as idempotent
public record ProcessPaymentCommand(Guid PaymentId, decimal Amount)
    : ICommand<Receipt>, IIdempotentRequest;

// In ASP.NET Core controller
[HttpPost("payments")]
public async Task<IActionResult> ProcessPayment(
    [FromBody] ProcessPaymentCommand command,
    [FromHeader(Name = "Idempotency-Key")] string idempotencyKey)
{
    // Set IdempotencyKey in request context
    var context = RequestContext.Create() with { IdempotencyKey = idempotencyKey };

    var result = await _Encina.Send(command, context);

    return result.Match(
        Right: receipt => Ok(receipt),
        Left: error => error.ToProblemDetails(HttpContext)
    );
}

// If the same idempotency key is sent again:
// - Returns cached response immediately
// - Handler is NOT executed again
// - Guarantees exactly-once processing
```

**Inbox Configuration**:

```csharp
config.UseInbox = true;
config.InboxOptions.MaxRetries = 3;
config.InboxOptions.MessageRetentionPeriod = TimeSpan.FromDays(7);
config.InboxOptions.EnableAutomaticPurge = true;
config.InboxOptions.PurgeInterval = TimeSpan.FromHours(24);
```

### 5. Transaction Management

```csharp
// Configure transactions
config.UseTransactions = true;

// Transactions are automatic based on ROP results
public class CreateOrderHandler : ICommandHandler<CreateOrderCommand, Order>
{
    private readonly IDbConnection _connection;

    public async ValueTask<Either<EncinaError, Order>> Handle(
        CreateOrderCommand request,
        IRequestContext context,
        CancellationToken cancellationToken)
    {
        // Transaction already started by TransactionPipelineBehavior

        // Execute domain logic
        var order = await SaveOrderAsync(request);

        // Return Right - transaction commits automatically
        return order;

        // Return Left or throw exception - transaction rolls back automatically
    }
}
```

## Bulk Operations

High-performance bulk database operations using Oracle Array Binding for maximum throughput.

### Performance Comparison (Oracle 21c XE, 1,000 entities)

| Operation | Loop Time | Bulk Time | Improvement |
|-----------|-----------|-----------|-------------|
| **Insert** | ~6,500ms | ~75ms | **87x faster** |
| **Update** | ~6,800ms | ~95ms | **72x faster** |
| **Delete** | ~6,200ms | ~25ms | **248x faster** |

> **Note**: Uses Oracle Array Binding (ODP.NET feature) for all operations, achieving exceptional performance.

### Using IBulkOperations

```csharp
// Get bulk operations from Unit of Work
var bulkOps = unitOfWork.BulkOperations<Order>();

// Bulk insert 10,000 orders using Oracle Array Binding
var orders = GenerateOrders(10_000);
var result = await bulkOps.BulkInsertAsync(orders);

result.Match(
    Right: count => _logger.LogInformation("Inserted {Count} orders", count),
    Left: error => _logger.LogError("Bulk insert failed: {Error}", error.Message)
);
```

### Available Operations

| Operation | Implementation | Description |
|-----------|----------------|-------------|
| `BulkInsertAsync` | Array Binding | Insert thousands of rows in seconds |
| `BulkUpdateAsync` | Array Binding + MERGE | Update multiple rows efficiently |
| `BulkDeleteAsync` | Array Binding | Delete by primary key |
| `BulkMergeAsync` | MERGE statement | Upsert (insert or update) |
| `BulkReadAsync` | SELECT with IN | Read multiple entities by IDs |

### Configuration

```csharp
var config = BulkConfig.Default with
{
    BatchSize = 10000,             // Oracle handles large batches well
    BulkCopyTimeout = 300,         // Timeout in seconds
    PreserveInsertOrder = true,    // Maintain order
    PropertiesToInclude = ["Status", "UpdatedAt"]  // Partial updates
};

await bulkOps.BulkUpdateAsync(entities, config);
```

### Oracle Array Binding Details

Oracle Array Binding allows sending multiple parameter values in a single round-trip:

```csharp
// Internal implementation uses ArrayBindCount
command.ArrayBindCount = entities.Count;
command.Parameters.Add(":Id", OracleDbType.Raw, ids, ParameterDirection.Input);
command.Parameters.Add(":Name", OracleDbType.Varchar2, names, ParameterDirection.Input);
// Single ExecuteNonQueryAsync inserts all rows
```

## Performance Comparison

Encina.ADO vs Dapper vs Entity Framework Core (1,000 outbox messages):

| Provider | Execution Time | Relative Speed | Memory Allocated |
|----------|---------------|----------------|------------------|
| **ADO.NET** | **52ms** | **1.00x (baseline)** | **~16KB** |
| Dapper | 85ms | 1.63x slower | ~21KB |
| EF Core | 155ms | 2.98x slower | ~82KB |

> Benchmarks run on .NET 10, Oracle 21c XE, Intel Core i9-13900KS.

**Why ADO.NET is faster:**

- No expression tree compilation (Dapper)
- No change tracking overhead (EF Core)
- Direct OracleCommand/OracleDataReader usage
- Oracle Array Binding for bulk operations
- Minimal allocations
- Zero reflection

## ADO.NET Implementation Details

### Raw SQL Execution Pattern

```csharp
public async Task AddAsync(IOutboxMessage message, CancellationToken cancellationToken)
{
    var sql = """
        INSERT INTO OutboxMessages
        (Id, NotificationType, Content, CreatedAtUtc, RetryCount)
        VALUES
        (:Id, :NotificationType, :Content, :CreatedAtUtc, :RetryCount)
        """;

    await using var command = _connection.CreateCommand();
    command.CommandText = sql;

    // Bind variables (Oracle uses : prefix)
    command.Parameters.Add(":Id", OracleDbType.Raw).Value = message.Id.ToByteArray();
    command.Parameters.Add(":NotificationType", OracleDbType.Varchar2).Value = message.NotificationType;
    command.Parameters.Add(":Content", OracleDbType.Clob).Value = message.Content;
    command.Parameters.Add(":CreatedAtUtc", OracleDbType.TimeStamp).Value = message.CreatedAtUtc;
    command.Parameters.Add(":RetryCount", OracleDbType.Int32).Value = message.RetryCount;

    if (_connection.State != ConnectionState.Open)
        await _connection.OpenAsync(cancellationToken);

    await command.ExecuteNonQueryAsync(cancellationToken);
}
```

### OracleDataReader Mapping

```csharp
await using var reader = await command.ExecuteReaderAsync(cancellationToken);
while (await reader.ReadAsync(cancellationToken))
{
    messages.Add(new OutboxMessage
    {
        Id = new Guid((byte[])reader.GetValue(reader.GetOrdinal("Id"))),
        NotificationType = reader.GetString(reader.GetOrdinal("NotificationType")),
        Content = reader.GetString(reader.GetOrdinal("Content")),
        CreatedAtUtc = reader.GetDateTime(reader.GetOrdinal("CreatedAtUtc")),
        ProcessedAtUtc = reader.IsDBNull(reader.GetOrdinal("ProcessedAtUtc"))
            ? null
            : reader.GetDateTime(reader.GetOrdinal("ProcessedAtUtc")),
        RetryCount = reader.GetInt32(reader.GetOrdinal("RetryCount"))
    });
}
```

## Migration from Dapper/EF Core

### From Dapper.Oracle

Encina.ADO.Oracle uses the same interfaces and entities as Encina.Dapper.Oracle:

```csharp
// Before (Dapper)
services.AddEncinaDapperOracle(connectionString, config => { ... });

// After (ADO.NET)
services.AddEncinaADOOracle(connectionString, config => { ... });
```

SQL schema is identical - no migration required!

### From Entity Framework Core

1. **Export data** (if needed):

   ```sql
   CREATE TABLE OutboxMessages_Backup AS SELECT * FROM OutboxMessages;
   CREATE TABLE InboxMessages_Backup AS SELECT * FROM InboxMessages;
   ```

2. **Update service registration**:

   ```csharp
   // Before (EF Core)
   services.AddEncinaEntityFrameworkCore<AppDbContext>(config => { ... });

   // After (ADO.NET)
   services.AddEncinaADOOracle(connectionString, config => { ... });
   ```

3. **Update entities** (minor changes):
   - EF Core entities → ADO.NET entities (same interface)
   - No lazy loading or navigation properties needed

## Configuration Reference

### Connection Management

```csharp
// Option 1: Connection string
services.AddEncinaADOOracle(
    "User Id=myuser;Password=mypass;Data Source=localhost:1521/XEPDB1;",
    config => { ... });

// Option 2: Custom factory
services.AddEncinaADOOracle(
    sp => new OracleConnection(sp.GetRequiredService<IConfiguration>()
        .GetConnectionString("Default")),
    config => { ... });

// Option 3: Use existing IDbConnection registration
services.AddScoped<IDbConnection>(sp =>
    new OracleConnection(connectionString));
services.AddEncinaADOOracle(config => { ... });
```

### Pattern Options

```csharp
services.AddEncinaADOOracle(connectionString, config =>
{
    // Outbox Pattern
    config.UseOutbox = true;
    config.OutboxOptions.ProcessingInterval = TimeSpan.FromSeconds(5);
    config.OutboxOptions.BatchSize = 100;
    config.OutboxOptions.MaxRetries = 3;
    config.OutboxOptions.BaseRetryDelay = TimeSpan.FromSeconds(5);
    config.OutboxOptions.EnableProcessor = true;

    // Inbox Pattern
    config.UseInbox = true;
    config.InboxOptions.MaxRetries = 3;
    config.InboxOptions.MessageRetentionPeriod = TimeSpan.FromDays(7);
    config.InboxOptions.EnableAutomaticPurge = true;
    config.InboxOptions.PurgeInterval = TimeSpan.FromHours(24);
    config.InboxOptions.PurgeBatchSize = 100;

    // Transaction Management
    config.UseTransactions = true;

    // Saga Pattern (Coming Soon)
    // config.UseSagas = true;

    // Scheduling Pattern (Coming Soon)
    // config.UseScheduling = true;
});
```

## Troubleshooting

### Connection is not open

**Error**: `InvalidOperationException: Connection must be open.`

**Solution**: Ensure connection is registered with correct lifetime:

```csharp
// Use Scoped for web applications
services.AddScoped<IDbConnection>(_ =>
    new OracleConnection(connectionString));

// Use Transient for background services
services.AddTransient<IDbConnection>(_ =>
    new OracleConnection(connectionString));
```

### SQL injection concerns

**Answer**: All queries use bind variables (`:paramName`). Direct string concatenation is never used for values.

### Performance tuning

1. **Enable Oracle query tracing**:

   ```sql
   ALTER SESSION SET SQL_TRACE = TRUE;
   -- or use DBMS_MONITOR package for more control
   ```

2. **Review indexes** (already optimized):
   - `IX_OUTBOX_PROCESSED_RETRY`
   - `IX_INBOX_EXPIRES`
   - `IX_SAGA_STATUS_UPDATED`
   - `IX_SCHEDULED_AT_PROCESSED`

3. **Adjust batch sizes**:

   ```csharp
   config.OutboxOptions.BatchSize = 500; // Oracle handles large batches well
   config.InboxOptions.PurgeBatchSize = 1000; // Increase for bulk cleanup
   ```

4. **Connection pooling**:

   ```csharp
   // ODP.NET enables pooling by default
   var connectionString = "User Id=myuser;Password=mypass;Data Source=localhost:1521/XEPDB1;Min Pool Size=5;Max Pool Size=100;";
   ```

## Roadmap

- ✅ Outbox Pattern
- ✅ Inbox Pattern
- ✅ Transaction Management
- ✅ Bulk Operations (Array Binding)
- ⏳ Saga Pattern (planned)
- ⏳ Scheduling Pattern (planned)

## Contributing

See [CONTRIBUTING.md](../../CONTRIBUTING.md) for guidelines.

## License

MIT License - see [LICENSE](../../LICENSE) for details.
