# Encina.EntityFrameworkCore

Entity Framework Core implementation for Encina messaging patterns including Outbox, Inbox, Saga orchestration, and Scheduled messages.

## Features

- **Outbox Pattern**: Reliable event publishing with at-least-once delivery guarantees
- **Inbox Pattern**: Idempotent message processing with exactly-once semantics
- **Saga Orchestration**: Distributed transaction coordination with compensation support
- **Scheduled Messages**: Delayed and recurring command execution
- **Transaction Management**: Automatic database transaction handling based on Railway Oriented Programming results

## Installation

```bash
dotnet add package Encina.EntityFrameworkCore
```

## Quick Start

### 1. Configure DbContext

Add the messaging entities to your DbContext:

```csharp
using Microsoft.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Outbox;
using Encina.EntityFrameworkCore.Inbox;
using Encina.EntityFrameworkCore.Sagas;
using Encina.EntityFrameworkCore.Scheduling;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Add DbSets for the patterns you need
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<SagaState> SagaStates => Set<SagaState>();
    public DbSet<ScheduledMessage> ScheduledMessages => Set<ScheduledMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply configurations for the patterns you need
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new SagaStateConfiguration());
        modelBuilder.ApplyConfiguration(new ScheduledMessageConfiguration());
    }
}
```

### 2. Register Services

All messaging patterns are **opt-in**. Enable only what you need:

```csharp
using Encina.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Simple configuration - just transactions
builder.Services.AddEncinaEntityFrameworkCore<AppDbContext>(config =>
{
    config.UseTransactions = true;
});

// Full configuration - all patterns enabled
builder.Services.AddEncinaEntityFrameworkCore<AppDbContext>(config =>
{
    config.UseTransactions = true;
    config.UseOutbox = true;
    config.UseInbox = true;
    config.UseSagas = true;
    config.UseScheduling = true;

    // Customize options
    config.OutboxOptions.ProcessingInterval = TimeSpan.FromSeconds(30);
    config.OutboxOptions.MaxRetries = 5;

    config.InboxOptions.MaxRetries = 3;
    config.InboxOptions.MessageRetentionPeriod = TimeSpan.FromDays(30);

    config.SchedulingOptions.BatchSize = 100;
    config.SchedulingOptions.EnableRecurringMessages = true;
});
```

### 3. Run Migrations

Generate and apply migrations to create the database tables:

```bash
dotnet ef migrations add AddMessagingPatterns
dotnet ef database update
```

## Messaging Patterns

### Outbox Pattern

Ensures reliable event publishing by storing events in the database within the same transaction as your domain changes.

**Use cases:**

- Publishing domain events after entity changes
- Guaranteed event delivery to message brokers
- Decoupling event publication from business logic

**Example:**

```csharp
public class OrderPlacedNotification : INotification
{
    public Guid OrderId { get; init; }
    public decimal TotalAmount { get; init; }
}

public class PlaceOrderCommandHandler : IRequestHandler<PlaceOrderCommand, Order>
{
    private readonly AppDbContext _dbContext;

    public async Task<Either<MediatorError, Order>> Handle(
        PlaceOrderCommand request,
        CancellationToken cancellationToken)
    {
        var order = new Order(request.CustomerId, request.Items);
        _dbContext.Orders.Add(order);

        // Event will be stored in outbox table automatically
        // via OutboxPostProcessor and published later by OutboxProcessor
        await _dbContext.SaveChangesAsync(cancellationToken);

        return order;
    }
}
```

**How it works:**

1. `OutboxPostProcessor` intercepts successful request results
2. Serializes notifications to `OutboxMessages` table
3. Background `OutboxProcessor` polls for pending messages
4. Publishes messages and marks them as processed
5. Automatic retry with exponential backoff on failures

**Configuration:**

```csharp
config.OutboxOptions.ProcessingInterval = TimeSpan.FromSeconds(30);
config.OutboxOptions.BatchSize = 100;
config.OutboxOptions.MaxRetries = 3;
config.OutboxOptions.BaseRetryDelay = TimeSpan.FromSeconds(5);
config.OutboxOptions.EnableProcessor = true;
```

### Inbox Pattern

Prevents duplicate processing of external messages through idempotent storage.

**Use cases:**

- Webhook handling
- Message queue consumers
- External API callbacks
- Preventing duplicate processing

**Example:**

```csharp
public class WebhookController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpPost("webhooks/payment")]
    public async Task<IActionResult> HandlePaymentWebhook(
        [FromBody] PaymentWebhookRequest webhook,
        [FromHeader(Name = "X-Webhook-Id")] string webhookId)
    {
        var command = new ProcessPaymentCommand
        {
            OrderId = webhook.OrderId,
            Amount = webhook.Amount,
            Status = webhook.Status
        };

        // InboxPipelineBehavior will check if webhookId was already processed
        // If yes, returns cached response without executing handler
        // If no, executes handler and stores result in inbox
        var result = await _mediator.Send(command);

        return result.Match(
            success => Ok(success),
            error => BadRequest(error)
        );
    }
}
```

**How it works:**

1. `InboxPipelineBehavior` intercepts all requests
2. Checks if message ID exists in `InboxMessages` table
3. If exists and processed: returns cached response
4. If new: executes handler and stores result
5. If failed: stores error and allows retry up to MaxRetries

**Configuration:**

```csharp
config.InboxOptions.MaxRetries = 3;
config.InboxOptions.MessageRetentionPeriod = TimeSpan.FromDays(30);
config.InboxOptions.PurgeInterval = TimeSpan.FromHours(24);
config.InboxOptions.EnableAutomaticPurge = true;
config.InboxOptions.PurgeBatchSize = 100;
```

### Saga Pattern

Coordinates distributed transactions across multiple services with compensation support.

**Use cases:**

- Multi-step business processes
- Distributed transactions
- Long-running workflows
- Compensation/rollback scenarios

**Example:**

```csharp
public class OrderFulfillmentSaga : Saga<OrderFulfillmentSagaData>
{
    private readonly IMediator _mediator;
    private readonly ISagaStore _sagaStore;

    protected override async Task ExecuteAsync(
        OrderFulfillmentSagaData data,
        CancellationToken cancellationToken)
    {
        // Step 1: Reserve inventory
        await ExecuteStepAsync(
            async () => await _mediator.Send(new ReserveInventoryCommand(data.OrderId)),
            async () => await _mediator.Send(new ReleaseInventoryCommand(data.OrderId)),
            cancellationToken
        );

        // Step 2: Process payment
        await ExecuteStepAsync(
            async () => await _mediator.Send(new ProcessPaymentCommand(data.OrderId, data.Amount)),
            async () => await _mediator.Send(new RefundPaymentCommand(data.OrderId)),
            cancellationToken
        );

        // Step 3: Ship order
        await ExecuteStepAsync(
            async () => await _mediator.Send(new ShipOrderCommand(data.OrderId)),
            async () => await _mediator.Send(new CancelShipmentCommand(data.OrderId)),
            cancellationToken
        );
    }
}

public class OrderFulfillmentSagaData
{
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
}
```

**How it works:**

1. Saga state stored in `SagaStates` table
2. Each step execution updates `CurrentStep`
3. On failure: runs compensation actions in reverse order
4. `LastUpdatedAtUtc` tracks progress
5. Stuck saga detection for monitoring

**Configuration:**

```csharp
config.UseSagas = true;
```

### Scheduled Messages

Execute commands at specific times or on recurring schedules.

**Use cases:**

- Delayed command execution
- Recurring tasks
- Scheduled reminders
- Time-based workflows

**Example:**

```csharp
public class OrderService
{
    private readonly IScheduledMessageStore _scheduler;

    public async Task ScheduleOrderReminder(Guid orderId)
    {
        var command = new SendOrderReminderCommand { OrderId = orderId };

        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = typeof(SendOrderReminderCommand).AssemblyQualifiedName!,
            Content = JsonSerializer.Serialize(command),
            ScheduledAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow,
            IsRecurring = false,
            RetryCount = 0
        };

        await _scheduler.AddAsync(message);
        await _scheduler.SaveChangesAsync();
    }

    public async Task ScheduleDailyReports()
    {
        var command = new GenerateDailyReportCommand();

        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = typeof(GenerateDailyReportCommand).AssemblyQualifiedName!,
            Content = JsonSerializer.Serialize(command),
            ScheduledAtUtc = DateTime.UtcNow.Date.AddDays(1), // Tomorrow at midnight
            CreatedAtUtc = DateTime.UtcNow,
            IsRecurring = true,
            CronExpression = "0 0 * * *", // Daily at midnight
            RetryCount = 0
        };

        await _scheduler.AddAsync(message);
        await _scheduler.SaveChangesAsync();
    }
}
```

**How it works:**

1. Messages stored in `ScheduledMessages` table
2. Background processor polls for due messages
3. Deserializes and executes commands via mediator
4. Marks as processed on success
5. Reschedules recurring messages automatically
6. Dead-letter handling for max retry failures

**Configuration:**

```csharp
config.SchedulingOptions.ProcessingInterval = TimeSpan.FromSeconds(30);
config.SchedulingOptions.BatchSize = 100;
config.SchedulingOptions.MaxRetries = 3;
config.SchedulingOptions.BaseRetryDelay = TimeSpan.FromSeconds(5);
config.SchedulingOptions.EnableProcessor = true;
config.SchedulingOptions.EnableRecurringMessages = true;
```

### Transaction Management

Automatically wraps request handlers in database transactions with commit/rollback based on ROP results.

**Use cases:**

- Ensuring data consistency
- Atomic operations across multiple entities
- Automatic rollback on failures

**Example:**

```csharp
public class TransferMoneyCommandHandler : IRequestHandler<TransferMoneyCommand, TransferResult>
{
    private readonly AppDbContext _dbContext;

    public async Task<Either<MediatorError, TransferResult>> Handle(
        TransferMoneyCommand request,
        CancellationToken cancellationToken)
    {
        var fromAccount = await _dbContext.Accounts.FindAsync(request.FromAccountId);
        var toAccount = await _dbContext.Accounts.FindAsync(request.ToAccountId);

        if (fromAccount == null || toAccount == null)
            return MediatorError.NotFound("Account not found");

        if (fromAccount.Balance < request.Amount)
            return MediatorError.Validation("Insufficient balance");

        fromAccount.Balance -= request.Amount;
        toAccount.Balance += request.Amount;

        // TransactionPipelineBehavior will:
        // 1. Begin transaction before this handler
        // 2. Commit on Right (success)
        // 3. Rollback on Left (error)

        return new TransferResult { TransactionId = Guid.NewGuid() };
    }
}
```

**How it works:**

1. `TransactionPipelineBehavior` wraps handler execution
2. Begins transaction before handler invocation
3. Commits transaction on `Right<TResponse>` (success)
4. Rolls back transaction on `Left<MediatorError>` (failure)
5. Exception safety with proper disposal

**Configuration:**

```csharp
config.UseTransactions = true;
```

## Database Schema

### OutboxMessages Table

| Column | Type | Description |
|--------|------|-------------|
| Id | Guid | Primary key |
| NotificationType | string | Full type name of notification |
| Content | string | JSON serialized notification |
| CreatedAtUtc | DateTime | Creation timestamp |
| ProcessedAtUtc | DateTime? | Processing timestamp |
| ErrorMessage | string? | Error details if failed |
| RetryCount | int | Number of retry attempts |
| NextRetryAtUtc | DateTime? | Next retry schedule |

### InboxMessages Table

| Column | Type | Description |
|--------|------|-------------|
| MessageId | string | Primary key (external message ID) |
| RequestType | string | Full type name of request |
| ReceivedAtUtc | DateTime | Reception timestamp |
| ProcessedAtUtc | DateTime? | Processing timestamp |
| ExpiresAtUtc | DateTime | Expiration timestamp |
| Response | string? | Cached response JSON |
| ErrorMessage | string? | Error details if failed |
| RetryCount | int | Number of retry attempts |
| NextRetryAtUtc | DateTime? | Next retry schedule |

### SagaStates Table

| Column | Type | Description |
|--------|------|-------------|
| SagaId | Guid | Primary key |
| SagaType | string | Full type name of saga |
| Data | string | JSON serialized saga data |
| Status | int | SagaStatus enum value |
| StartedAtUtc | DateTime | Start timestamp |
| LastUpdatedAtUtc | DateTime | Last update timestamp |
| CompletedAtUtc | DateTime? | Completion timestamp |
| ErrorMessage | string? | Error details if failed |
| CurrentStep | int | Current execution step |

### ScheduledMessages Table

| Column | Type | Description |
|--------|------|-------------|
| Id | Guid | Primary key |
| RequestType | string | Full type name of request |
| Content | string | JSON serialized request |
| ScheduledAtUtc | DateTime | Scheduled execution time |
| CreatedAtUtc | DateTime | Creation timestamp |
| ProcessedAtUtc | DateTime? | Processing timestamp |
| LastExecutedAtUtc | DateTime? | Last execution attempt |
| ErrorMessage | string? | Error details if failed |
| RetryCount | int | Number of retry attempts |
| NextRetryAtUtc | DateTime? | Next retry schedule |
| IsRecurring | bool | Recurring message flag |
| CronExpression | string? | Cron expression for recurring |

## Advanced Configuration

### Custom Retry Strategies

```csharp
config.OutboxOptions.BaseRetryDelay = TimeSpan.FromSeconds(5);
config.OutboxOptions.MaxRetries = 5;

// Exponential backoff is automatic:
// Retry 1: 5 seconds
// Retry 2: 25 seconds  (5 * 1^2)
// Retry 3: 45 seconds  (5 * 2^2)
// Retry 4: 80 seconds  (5 * 3^2)
// Retry 5: 125 seconds (5 * 4^2)
```

### Dead Letter Handling

Messages exceeding `MaxRetries` are automatically marked as dead-lettered:

```csharp
// Query dead-lettered outbox messages
var deadLettered = await dbContext.OutboxMessages
    .Where(m => m.ProcessedAtUtc == null && m.RetryCount >= maxRetries)
    .ToListAsync();

// Query dead-lettered scheduled messages
var stuckMessages = await dbContext.ScheduledMessages
    .Where(m => m.ProcessedAtUtc == null && m.RetryCount >= maxRetries)
    .ToListAsync();
```

### Monitoring Stuck Sagas

```csharp
var sagaStore = serviceProvider.GetRequiredService<ISagaStore>();

// Find sagas stuck for more than 1 hour
var stuckSagas = await sagaStore.GetStuckSagasAsync(
    olderThan: TimeSpan.FromHours(1),
    batchSize: 100
);

foreach (var saga in stuckSagas)
{
    // Log, alert, or compensate
    _logger.LogWarning(
        "Saga {SagaId} of type {SagaType} stuck at step {Step}",
        saga.SagaId,
        saga.SagaType,
        saga.CurrentStep
    );
}
```

### Message Retention and Cleanup

```csharp
config.InboxOptions.MessageRetentionPeriod = TimeSpan.FromDays(30);
config.InboxOptions.EnableAutomaticPurge = true;
config.InboxOptions.PurgeInterval = TimeSpan.FromHours(24);
config.InboxOptions.PurgeBatchSize = 100;

// Manual cleanup
var inboxStore = serviceProvider.GetRequiredService<IInboxStore>();
var expired = await inboxStore.GetExpiredMessagesAsync(batchSize: 100);
var messageIds = expired.Select(m => m.MessageId).ToArray();
await inboxStore.RemoveExpiredMessagesAsync(messageIds);
await inboxStore.SaveChangesAsync();
```

## Migration from Other Patterns

### From MediatR + EF SaveChanges

**Before:**

```csharp
_dbContext.Orders.Add(order);
await _dbContext.SaveChangesAsync();
await _mediator.Publish(new OrderPlacedEvent(order.Id));
```

**After:**

```csharp
// Enable Outbox pattern
config.UseOutbox = true;

// Handler code stays the same
_dbContext.Orders.Add(order);
await _dbContext.SaveChangesAsync();
// Event automatically stored in outbox and published later
```

### From Manual Idempotency

**Before:**

```csharp
var existing = await _dbContext.ProcessedWebhooks.FindAsync(webhookId);
if (existing != null)
    return existing.Response;

var result = await ProcessWebhook(webhook);
_dbContext.ProcessedWebhooks.Add(new ProcessedWebhook { Id = webhookId, Response = result });
await _dbContext.SaveChangesAsync();
return result;
```

**After:**

```csharp
// Enable Inbox pattern
config.UseInbox = true;

// InboxPipelineBehavior handles idempotency automatically
var result = await _mediator.Send(new ProcessWebhookCommand(webhook));
return result;
```

## Best Practices

1. **Enable only needed patterns**: Don't enable all patterns if you don't need them
2. **Configure batch sizes**: Adjust based on your load and database performance
3. **Monitor dead letters**: Set up alerts for messages exceeding max retries
4. **Use appropriate retention**: Balance between audit requirements and storage costs
5. **Saga timeouts**: Implement timeout logic in saga orchestrators
6. **Idempotent handlers**: Even with Inbox, design handlers to be idempotent
7. **Structured logging**: Log saga IDs and message IDs for traceability
8. **Database indexes**: Add indexes on frequently queried columns (ProcessedAtUtc, ScheduledAtUtc, etc.)

## Performance Considerations

- **Batch processing**: Outbox and scheduling processors use batching to reduce database round-trips
- **In-memory database**: Tests use EF Core InMemory provider for fast execution
- **Polling intervals**: Adjust `ProcessingInterval` based on your latency requirements
- **Connection pooling**: EF Core handles connection pooling automatically
- **Query optimization**: All queries use appropriate indexes and filtering

## Troubleshooting

### Messages not being processed

1. Check that background processors are enabled:

   ```csharp
   config.OutboxOptions.EnableProcessor = true;
   config.SchedulingOptions.EnableProcessor = true;
   ```

2. Verify IHostedService registration:

   ```csharp
   // Should be automatic, but verify in logs
   var hostedServices = serviceProvider.GetServices<IHostedService>();
   ```

3. Check application logs for processor errors

### High retry counts

1. Investigate error messages in database
2. Increase retry delay:

   ```csharp
   config.OutboxOptions.BaseRetryDelay = TimeSpan.FromSeconds(30);
   ```

3. Add circuit breaker for external dependencies

### Saga stuck in Running state

1. Query stuck sagas:

   ```csharp
   var stuck = await sagaStore.GetStuckSagasAsync(TimeSpan.FromHours(1), 10);
   ```

2. Implement timeout compensation logic
3. Monitor `LastUpdatedAtUtc` for progress tracking

## Related Packages

- **Encina**: Core mediator implementation with Railway Oriented Programming
- **Encina.Messaging**: Shared abstractions for messaging patterns
- **Encina.AspNetCore**: ASP.NET Core integration with endpoint filters and middleware

## License

MIT License - see LICENSE file for details

## Contributing

Contributions welcome! This is a Pre-1.0 project, so breaking changes are expected and encouraged if they improve the design.

## Support

For issues, questions, or suggestions, please open an issue on GitHub.
