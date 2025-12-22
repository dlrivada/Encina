# Encina.Messaging

[![NuGet](https://img.shields.io/nuget/v/Encina.Messaging.svg)](https://www.nuget.org/packages/Encina.Messaging/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](../../LICENSE)

**Provider-agnostic abstractions for distributed messaging patterns in Encina.**

This package provides interfaces and base implementations for common distributed system patterns, allowing you to choose your preferred persistence provider (Entity Framework Core, Dapper, ADO.NET, etc.) while maintaining consistency across your application.

## Philosophy

### 🎯 **All Patterns Are OPTIONAL**

Encina.Messaging follows a **pay-for-what-you-use** philosophy. If your application is a simple CRUD app, you might only use transactions. If you're building a complex distributed system, you can enable all patterns.

```csharp
// Simple app - only transactions
services.AddEncinaMessaging(config =>
{
    config.UseTransactions = true;
});

// Distributed system - all patterns
services.AddEncinaMessaging(config =>
{
    config.UseTransactions = true;
    config.UseOutbox = true;
    config.UseInbox = true;
    config.UseSagas = true;
    config.UseScheduling = true;
});
```

### 🔌 **Provider Agnostic**

Choose your data access strategy without vendor lock-in:

| Provider | Package | Use Case |
|----------|---------|----------|
| **Entity Framework Core** | `Encina.EntityFrameworkCore` | Full ORM, change tracking, migrations |
| **Dapper** | `Encina.Dapper` | Lightweight, SQL control, performance |
| **ADO.NET** | `Encina.Data` | Maximum performance, full control |
| **Custom** | Implement `IOutboxStore`, etc. | NoSQL, message queues, etc. |

### ♻️ **Consistent Across Providers**

The Outbox pattern works the same way whether you use EF Core, Dapper, or ADO.NET:

```csharp
// Same code, different providers
services.AddEncinaOutbox<AppDbContext>();  // EF Core
services.AddEncinaOutbox(dapperConnection); // Dapper
services.AddEncinaOutbox(adoConnection);    // ADO.NET
```

## Messaging Patterns

### 1. **Outbox Pattern** (Reliable Event Publishing)

**Problem**: Events published before database commit can be lost if the system crashes.

**Solution**: Store events in the database (same transaction), publish later by background processor.

**Guarantees**: At-least-once delivery, durability, ordering.

```csharp
// Domain event
public record OrderCreatedEvent(Guid OrderId) : INotification;

// Command handler
public class CreateOrderHandler : ICommandHandler<CreateOrderCommand, Order>
{
    public async ValueTask<Either<MediatorError, Order>> Handle(...)
    {
        var order = new Order { Id = Guid.NewGuid(), ... };
        _dbContext.Orders.Add(order);

        // Add event - will be stored in outbox (same transaction)
        order.AddDomainEvent(new OrderCreatedEvent(order.Id));

        await _dbContext.SaveChangesAsync();

        return order;
        // Events published asynchronously by OutboxProcessor
    }
}
```

### 2. **Inbox Pattern** (Idempotent Message Processing)

**Problem**: Messages from queues/webhooks can arrive multiple times, causing duplicate processing.

**Solution**: Track processed messages by ID, return cached response for duplicates.

**Guarantees**: Exactly-once processing.

```csharp
// Mark command as idempotent
public record ProcessPaymentCommand(decimal Amount, string PaymentId)
    : ICommand<Receipt>, IIdempotentRequest;

// If same PaymentId arrives twice, cached receipt is returned
var result1 = await mediator.Send(command); // Processes
var result2 = await mediator.Send(command); // Returns cached result
```

### 3. **Saga Pattern** (Distributed Transactions)

**Problem**: Multi-step business processes spanning services/aggregates can fail mid-way.

**Solution**: Orchestrate steps with compensating actions to rollback on failure.

```csharp
public class OrderProcessingSaga : Saga<OrderData>
{
    protected override void ConfigureSteps()
    {
        // Step 1: Reserve inventory
        AddStep(
            execute: async (data, ctx, ct) =>
            {
                var result = await _mediator.Send(new ReserveInventoryCommand(...), ct);
                return result.Match(
                    Right: reservation => { data.ReservationId = reservation.Id; return data; },
                    Left: error => error
                );
            },
            compensate: async (data, ctx, ct) =>
            {
                // Rollback: Cancel reservation
                if (data.ReservationId.HasValue)
                    await _mediator.Send(new CancelReservationCommand(data.ReservationId.Value), ct);
            }
        );

        // Step 2: Charge customer
        AddStep(...);

        // Step 3: Ship order
        AddStep(...);
    }
}

// If Step 2 or 3 fails, Step 1 compensation runs automatically
```

### 4. **Scheduled Messages** (Delayed Execution)

**Problem**: Need to execute commands in the future (reminders, timeouts, recurring tasks).

**Solution**: Persist messages with execution time, process by background scheduler.

```csharp
public class OrderHandler : ICommandHandler<CreateOrderCommand, Order>
{
    private readonly IMessageScheduler _scheduler;

    public async ValueTask<Either<MediatorError, Order>> Handle(...)
    {
        var order = new Order { ... };

        // Cancel order if not paid within 30 minutes
        await _scheduler.ScheduleAsync(
            new CancelUnpaidOrderCommand(order.Id),
            delay: TimeSpan.FromMinutes(30),
            context,
            cancellationToken);

        // Send reminder 24 hours before delivery
        await _scheduler.ScheduleAsync(
            new SendDeliveryReminderCommand(order.Id),
            executeAt: order.DeliveryDate.AddHours(-24),
            context,
            cancellationToken);

        return order;
    }
}
```

### 5. **Transactions** (Automatic Database Transactions)

**Problem**: Manual transaction management is verbose and error-prone.

**Solution**: Declarative transactions with automatic commit/rollback.

```csharp
// Marker interface
public record CreateOrderCommand(...) : ICommand<Order>, ITransactionalCommand;

// Or attribute
[Transaction(IsolationLevel = IsolationLevel.ReadCommitted)]
public record UpdateInventoryCommand(...) : ICommand;

// Transaction automatically wraps handler:
// - Begins before handler
// - Commits if Right
// - Rolls back if Left or exception
```

## Scheduling vs Hangfire/Quartz.NET

### Key Differences

| Feature | Encina.Scheduling | Hangfire | Quartz.NET |
|---------|--------------------------|----------|------------|
| **Focus** | Domain messages (commands/events) | General-purpose jobs | General-purpose jobs |
| **Integration** | Native mediator pattern | External library | External library |
| **Persistence** | Your database (EF/Dapper/ADO) | Own schema | Own schema |
| **UI** | No (use your own tools) | Dashboard included | No (3rd party) |
| **Cron Jobs** | Yes | Yes | Yes |
| **Delayed Messages** | Yes (primary use case) | Yes | Yes |
| **Distributed** | Via database | Via database/Redis | Via database |

### When to Use What?

**Use Encina.Scheduling when:**

- Scheduling domain commands/events (cancel order, send reminder)
- Want consistent messaging patterns (outbox, inbox, scheduling)
- Already using Encina
- Prefer your existing database over separate job schema

**Use Hangfire/Quartz when:**

- Need job management UI
- Scheduling non-domain tasks (report generation, cleanup)
- Complex scheduling requirements (multiple calendars, holidays)
- Want mature ecosystem with extensive plugins

### Can You Use Both?

**Absolutely!** They're complementary:

```csharp
// Encina.Scheduling for domain messages
await _scheduler.ScheduleAsync(
    new CancelOrderCommand(orderId),
    delay: TimeSpan.FromMinutes(30));

// Hangfire for infrastructure tasks
BackgroundJob.Schedule(
    () => CleanupTempFiles(),
    TimeSpan.FromHours(1));
```

### Integration Adapters (Future)

We plan to provide adapters so you can use Hangfire/Quartz as the backend:

```csharp
// Use Hangfire as scheduling backend
services.AddEncinaScheduling()
    .UseHangfire();

// Use Quartz as scheduling backend
services.AddEncinaScheduling()
    .UseQuartz();

// Same IMessageScheduler API, different implementation
```

## Pattern Coherence Across Providers

All messaging patterns share the same interfaces regardless of provider:

### Outbox Pattern

```csharp
// Interface (same for all providers)
public interface IOutboxMessage
{
    Guid Id { get; set; }
    string NotificationType { get; set; }
    string Content { get; set; }
    DateTime CreatedAtUtc { get; set; }
    DateTime? ProcessedAtUtc { get; set; }
    // ... consistent across providers
}

public interface IOutboxStore
{
    Task AddAsync(IOutboxMessage message, CancellationToken ct);
    Task<IEnumerable<IOutboxMessage>> GetPendingMessagesAsync(int batchSize, int maxRetries, CancellationToken ct);
    Task MarkAsProcessedAsync(Guid messageId, CancellationToken ct);
    Task MarkAsFailedAsync(Guid messageId, string error, DateTime? nextRetryAt, CancellationToken ct);
}

// Entity Framework Core implementation
public class OutboxMessageEF : IOutboxMessage
{
    // EF-specific: navigation properties, shadow properties, etc.
}

public class OutboxStoreEF : IOutboxStore
{
    private readonly DbContext _dbContext;

    public async Task AddAsync(IOutboxMessage message, CancellationToken ct)
    {
        _dbContext.Set<OutboxMessageEF>().Add((OutboxMessageEF)message);
        await _dbContext.SaveChangesAsync(ct);
    }
    // ... implements interface
}

// Dapper implementation
public class OutboxMessageDapper : IOutboxMessage
{
    // Dapper-specific: simple POCO, no navigation properties
}

public class OutboxStoreDapper : IOutboxStore
{
    private readonly IDbConnection _connection;

    public async Task AddAsync(IOutboxMessage message, CancellationToken ct)
    {
        await _connection.ExecuteAsync(
            "INSERT INTO OutboxMessages (...) VALUES (...)",
            message);
    }
    // ... implements interface
}
```

### Benefits of This Approach

1. **Migration Path**: Switch from EF Core to Dapper without changing domain code
2. **Testing**: Mock `IOutboxStore` instead of DbContext
3. **Polyglot Persistence**: Use EF Core for write model, Dapper for read model
4. **Learning**: Understand patterns independent of ORM

## Configuration Examples

### Minimal (Transactions Only)

```csharp
services.AddEncinaEntityFrameworkCore<AppDbContext>(config =>
{
    config.UseTransactions = true;
});
```

### Standard (Transactions + Outbox)

```csharp
services.AddEncinaEntityFrameworkCore<AppDbContext>(config =>
{
    config.UseTransactions = true;
    config.UseOutbox = true;
});
```

### Advanced (All Patterns)

```csharp
services.AddEncinaEntityFrameworkCore<AppDbContext>(config =>
{
    config.UseTransactions = true;
    config.UseOutbox = true;
    config.UseInbox = true;
    config.UseSagas = true;
    config.UseScheduling = true;
});
```

### Fluent Builder (Alternative Syntax)

```csharp
services.AddEncinaEntityFrameworkCore<AppDbContext>()
    .AddTransactions()
    .AddOutbox(options =>
    {
        options.BatchSize = 50;
        options.MaxRetries = 5;
    })
    .AddInbox(options =>
    {
        options.MessageRetentionPeriod = TimeSpan.FromDays(7);
    })
    .AddSagas()
    .AddScheduling();
```

## Inspiration

This package draws inspiration from mature messaging frameworks:

- **Wolverine** (ASP.NET): Outbox, Inbox, Sagas, Scheduled messages
- **MassTransit** (.NET): Saga orchestration, message scheduling
- **NServiceBus** (.NET): Outbox pattern, saga persistence
- **Axon Framework** (Java): Event sourcing, sagas
- **Temporal** (Polyglot): Workflow orchestration, durable execution

## Roadmap

- ✅ Core abstractions (IOutboxMessage, IInboxMessage, etc.)
- ✅ Messaging configuration (opt-in patterns)
- ⏳ Entity Framework Core provider
- ⏳ Dapper provider
- ⏳ ADO.NET provider
- ⏳ Hangfire adapter
- ⏳ Quartz.NET adapter
- ⏳ Comprehensive tests
- ⏳ Documentation and examples

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.
