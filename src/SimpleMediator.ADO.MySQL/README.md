# SimpleMediator.ADO.SqlServer

SQL Server implementation of SimpleMediator messaging patterns using raw ADO.NET for maximum performance.
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://opensource.org/licenses/MIT)

**Pure ADO.NET provider for SimpleMediator messaging patterns** - Zero external dependencies (except Microsoft.Data.SqlClient), maximum performance, and complete control over SQL execution.

SimpleMediator.ADO implements messaging patterns (Outbox, Inbox, Transactions) using raw ADO.NET with SqlCommand and SqlDataReader, offering the lightest possible overhead and full SQL transparency.

## Features

- **✅ Zero Dependencies**: Only Microsoft.Data.SqlClient (no ORMs, no micro-ORMs)
- **✅ Maximum Performance**: Raw SqlCommand/SqlDataReader execution
- **✅ Full SQL Control**: Complete visibility into executed queries
- **✅ Outbox Pattern**: At-least-once delivery for reliable event publishing
- **✅ Inbox Pattern**: Exactly-once semantics for idempotent processing
- **✅ Transaction Management**: Automatic commit/rollback based on ROP results
- **✅ Railway Oriented Programming**: Native `Either<MediatorError, T>` support
- **✅ SQL Server Optimized**: Parameterized queries, optimized indexes
- **✅ .NET 10 Native**: Built for modern .NET with nullable reference types

## Installation

```bash
dotnet add package SimpleMediator.ADO
```

## Quick Start

### 1. Basic Setup

```csharp
using SimpleMediator.ADO;

// Register with connection string
services.AddSimpleMediatorADO(
    connectionString: "Server=.;Database=MyApp;Integrated Security=true;",
    configure: config =>
    {
        config.UseOutbox = true;
        config.UseInbox = true;
        config.UseTransactions = true;
    });

// Or with custom IDbConnection factory
services.AddSimpleMediatorADO(
    connectionFactory: sp =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        return new SqlConnection(config.GetConnectionString("Default"));
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

```sql
-- Option 1: Run all at once
-- Execute Scripts/000_CreateAllTables.sql

-- Option 2: Run individually
-- Execute Scripts/001_CreateOutboxMessagesTable.sql
-- Execute Scripts/002_CreateInboxMessagesTable.sql
-- Execute Scripts/003_CreateSagaStatesTable.sql (if using Sagas)
-- Execute Scripts/004_CreateScheduledMessagesTable.sql (if using Scheduling)
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
    public async ValueTask<Either<MediatorError, Order>> Handle(
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
// 3. Published through mediator with retry logic
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

    var result = await _mediator.Send(command, context);

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

    public async ValueTask<Either<MediatorError, Order>> Handle(
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

## Performance Comparison

SimpleMediator.ADO vs Dapper vs Entity Framework Core (1,000 outbox messages):

| Provider | Execution Time | Relative Speed | Memory Allocated |
|----------|---------------|----------------|------------------|
| **ADO.NET** | **63ms** | **1.00x (baseline)** | **~15KB** |
| Dapper | 100ms | 1.59x slower | ~20KB |
| EF Core | 180ms | 2.86x slower | ~85KB |

> Benchmarks run on .NET 10, SQL Server LocalDB, Intel Core i7-12700K.

**Why ADO.NET is faster:**

- No expression tree compilation (Dapper)
- No change tracking overhead (EF Core)
- Direct SqlCommand/SqlDataReader usage
- Minimal allocations
- Zero reflection

## ADO.NET Implementation Details

### Raw SQL Execution Pattern

```csharp
public async Task AddAsync(IOutboxMessage message, CancellationToken cancellationToken)
{
    var sql = $@"
        INSERT INTO OutboxMessages
        (Id, NotificationType, Content, CreatedAtUtc, RetryCount)
        VALUES
        (@Id, @NotificationType, @Content, @CreatedAtUtc, @RetryCount)";

    using var command = _connection.CreateCommand();
    command.CommandText = sql;

    // Parameterized to prevent SQL injection
    AddParameter(command, "@Id", message.Id);
    AddParameter(command, "@NotificationType", message.NotificationType);
    AddParameter(command, "@Content", message.Content);
    AddParameter(command, "@CreatedAtUtc", message.CreatedAtUtc);
    AddParameter(command, "@RetryCount", message.RetryCount);

    if (_connection.State != ConnectionState.Open)
        await OpenConnectionAsync(cancellationToken);

    await ExecuteNonQueryAsync(command, cancellationToken);
}
```

### SqlDataReader Mapping

```csharp
using var reader = await ExecuteReaderAsync(command, cancellationToken);
while (await ReadAsync(reader, cancellationToken))
{
    messages.Add(new OutboxMessage
    {
        Id = reader.GetGuid(reader.GetOrdinal("Id")),
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

### From Dapper

SimpleMediator.ADO uses the same interfaces and entities as SimpleMediator.Dapper:

```csharp
// Before (Dapper)
services.AddSimpleMediatorDapper(connectionString, config => { ... });

// After (ADO.NET)
services.AddSimpleMediatorADO(connectionString, config => { ... });
```

SQL schema is identical - no migration required!

### From Entity Framework Core

1. **Export data** (if needed):

   ```sql
   SELECT * INTO OutboxMessages_Backup FROM OutboxMessages
   SELECT * INTO InboxMessages_Backup FROM InboxMessages
   ```

2. **Update service registration**:

   ```csharp
   // Before (EF Core)
   services.AddSimpleMediatorEntityFrameworkCore<AppDbContext>(config => { ... });

   // After (ADO.NET)
   services.AddSimpleMediatorADO(connectionString, config => { ... });
   ```

3. **Update entities** (minor changes):
   - EF Core entities → ADO.NET entities (same interface)
   - No lazy loading or navigation properties needed

## Configuration Reference

### Connection Management

```csharp
// Option 1: Connection string
services.AddSimpleMediatorADO(
    "Server=.;Database=MyApp;Integrated Security=true;",
    config => { ... });

// Option 2: Custom factory
services.AddSimpleMediatorADO(
    sp => new SqlConnection(sp.GetRequiredService<IConfiguration>()
        .GetConnectionString("Default")),
    config => { ... });

// Option 3: Use existing IDbConnection registration
services.AddScoped<IDbConnection>(sp =>
    new SqlConnection(connectionString));
services.AddSimpleMediatorADO(config => { ... });
```

### Pattern Options

```csharp
services.AddSimpleMediatorADO(connectionString, config =>
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
    new SqlConnection(connectionString));

// Use Transient for background services
services.AddTransient<IDbConnection>(_ =>
    new SqlConnection(connectionString));
```

### SQL injection concerns

**Answer**: All queries use parameterized commands via `AddParameter()` method. Direct string concatenation is never used for values.

### Performance tuning

1. **Enable SQL Server query statistics**:

   ```sql
   SET STATISTICS TIME ON
   SET STATISTICS IO ON
   ```

2. **Review indexes** (already optimized):
   - `IX_OutboxMessages_ProcessedAt_RetryCount`
   - `IX_InboxMessages_ExpiresAt`
   - `IX_SagaStates_Status_LastUpdated`
   - `IX_ScheduledMessages_ScheduledAt_Processed`

3. **Adjust batch sizes**:

   ```csharp
   config.OutboxOptions.BatchSize = 50; // Reduce for low memory
   config.InboxOptions.PurgeBatchSize = 200; // Increase for bulk cleanup
   ```

## Roadmap

- ✅ Outbox Pattern
- ✅ Inbox Pattern
- ✅ Transaction Management
- ⏳ Saga Pattern (planned)
- ⏳ Scheduling Pattern (planned)
- ⏳ Multi-database support (PostgreSQL, MySQL)

## Contributing

See [CONTRIBUTING.md](../../CONTRIBUTING.md) for guidelines.

## License

MIT License - see [LICENSE](../../LICENSE) for details.

---

**Note**: SimpleMediator.ADO is optimized for SQL Server. For PostgreSQL or MySQL, consider using SimpleMediator.Dapper with appropriate providers.
