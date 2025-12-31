# Encina.Testing.Bogus

[Bogus](https://github.com/bchavez/Bogus) integration for generating realistic test data for Encina applications. Includes pre-built fakers for messaging entities and a base class for domain-specific fakers.

## Installation

```bash
dotnet add package Encina.Testing.Bogus
```

## Why Bogus?

Traditional test data approaches use simple defaults like "Test User" or hardcoded values. Bogus provides:

- **Realistic data**: Names, addresses, emails, prices, dates
- **Reproducible seeds**: Same seed produces identical data every time
- **Fluent API**: Easy customization and method chaining
- **Localization**: Support for 50+ locales
- **Edge cases**: Random generation helps discover edge cases

## Quick Start

### Basic Usage

```csharp
using Encina.Testing.Bogus;

// Create a faker with default seed (reproducible)
var faker = new EncinaFaker<CreateOrder>()
    .RuleFor(o => o.CustomerId, f => f.Random.UserId())
    .RuleFor(o => o.Amount, f => f.Finance.Amount())
    .RuleFor(o => o.ShippingAddress, f => f.Address.FullAddress());

var order = faker.Generate();
```

### Messaging Entity Fakers

Pre-built fakers for Encina messaging entities:

```csharp
// Outbox messages
var outboxFaker = new OutboxMessageFaker();
var pendingMessage = outboxFaker.Generate();
var processedMessage = outboxFaker.AsProcessed().Generate();
var failedMessage = outboxFaker.AsFailed(retryCount: 3).Generate();

// Inbox messages
var inboxFaker = new InboxMessageFaker();
var message = inboxFaker.AsProcessed("{\"result\": \"ok\"}").Generate();
var expiredMessage = inboxFaker.AsExpired().Generate();

// Saga states
var sagaFaker = new SagaStateFaker();
var runningSaga = sagaFaker.Generate();
var completedSaga = sagaFaker.AsCompleted().Generate();
var failedSaga = sagaFaker.AsFailed("Payment timeout").Generate();

// Scheduled messages
var scheduledFaker = new ScheduledMessageFaker();
var pendingJob = scheduledFaker.Generate();
var recurringJob = scheduledFaker.AsRecurring("0 0 * * *").Generate();
var dueJob = scheduledFaker.AsDue().Generate();
```

## Reproducibility

All fakers use a default seed (12345) for reproducible results:

```csharp
[Fact]
public void Order_ShouldBeReproducible()
{
    var faker1 = new EncinaFaker<Order>();
    var faker2 = new EncinaFaker<Order>();

    faker1.RuleFor(o => o.Name, f => f.Name.FullName());
    faker2.RuleFor(o => o.Name, f => f.Name.FullName());

    var order1 = faker1.Generate();
    var order2 = faker2.Generate();

    order1.Name.ShouldBe(order2.Name); // Same name!
}

// Custom seed
var faker = new EncinaFaker<Order>().UseSeed(42);
```

## Extension Methods

Convenience methods for common Encina patterns:

### Quick Reference

```csharp
using Bogus;
using Encina.Testing.Bogus;

var faker = new Faker();

// All extension methods work seamlessly with Bogus:
var correlationId = faker.Random.CorrelationId();
var userId = faker.Random.UserId();
var notificationType = faker.NotificationType();
var recentDate = faker.Date.RecentUtc();
var (startDate, endDate) = faker.Date.DateRangeValue();
```

### Complete Examples

```csharp
using Bogus;
using Encina.Testing.Bogus;

var faker = new Faker();

// Identifiers
var correlationId = faker.Random.CorrelationId();    // Guid
var userId = faker.Random.UserId();                   // "user_abc12345"
var tenantId = faker.Random.TenantId("org");         // "org_xyz789"
var idempotencyKey = faker.Random.IdempotencyKey();  // Guid string

// Message types
var notificationType = faker.NotificationType();  // "OrderCreated", "PaymentReceived", etc.
var requestType = faker.RequestType();            // "CreateOrder", "ProcessPayment", etc.
var sagaType = faker.SagaType();                  // "OrderFulfillmentSaga", etc.
var sagaStatus = faker.SagaStatus();              // "Running", "Completed", "Failed", etc.

// UTC dates
var recentDate = faker.Date.RecentUtc();         // UTC timestamp in past 7 days
var futureDate = faker.Date.SoonUtc(30);         // UTC timestamp in next 30 days

// JSON content
var json = faker.JsonContent(3);                 // {"word1": "sentence...", ...}

// Domain Model - Entity IDs (Guid, int, long, string)
var guidId = faker.Random.EntityId<Guid>();
var intId = faker.Random.EntityId<int>();
var customIntId = faker.Random.IntEntityId(1000, 9999);
var prefixedStringId = faker.Random.StringEntityId(8, "ORD"); // "ORD_a1b2c3d4"

// Domain Model - Strongly-Typed ID values
var guidIdValue = faker.Random.StronglyTypedIdValue<Guid>();
var productIdValue = faker.Random.IntStronglyTypedIdValue();
var skuValue = faker.Random.StringStronglyTypedIdValue(8, "SKU");

// Domain Model - Value Objects
var quantityValue = faker.Random.QuantityValue();           // 0-1000
var smallQty = faker.Random.QuantityValue(1, 10);           // 1-10
var percentValue = faker.Random.PercentageValue();          // 0.00-100.00
var discount = faker.Random.PercentageValue(5, 50);         // 5.00-50.00
var (startDate, endDate) = faker.Date.DateRangeValue();     // Date range tuple
var (startTime, endTime) = faker.Date.TimeRangeValue();     // Time range tuple
```

## OutboxMessageFaker

Generate outbox messages for testing:

```csharp
var faker = new OutboxMessageFaker();

// Pending message (default)
var pending = faker.Generate();
// pending.ProcessedAtUtc == null
// pending.IsProcessed == false

// Processed message
var processed = new OutboxMessageFaker().AsProcessed().Generate();
// processed.ProcessedAtUtc != null
// processed.IsProcessed == true

// Failed message with retries
var failed = new OutboxMessageFaker().AsFailed(retryCount: 5).Generate();
// failed.ErrorMessage != null
// failed.RetryCount == 5
// failed.NextRetryAtUtc != null

// Custom notification type
var custom = new OutboxMessageFaker()
    .WithNotificationType("OrderShipped")
    .WithContent("{\"orderId\": \"123\"}")
    .Generate();
```

## InboxMessageFaker

Generate inbox messages for idempotency testing:

```csharp
var faker = new InboxMessageFaker();

// Pending message
var pending = faker.Generate();

// Processed with response
var processed = new InboxMessageFaker()
    .AsProcessed("{\"result\": \"success\"}")
    .Generate();

// Failed message
var failed = new InboxMessageFaker().AsFailed().Generate();

// Expired message (for cleanup tests)
var expired = new InboxMessageFaker().AsExpired().Generate();
// expired.IsExpired() == true

// Custom message ID
var custom = new InboxMessageFaker()
    .WithMessageId("idempotency-key-123")
    .WithRequestType("CreateOrder")
    .Generate();
```

## SagaStateFaker

Generate saga states for orchestration testing:

```csharp
var faker = new SagaStateFaker();

// Running saga (default)
var running = faker.Generate();
// running.Status == "Running"

// Completed saga
var completed = new SagaStateFaker().AsCompleted().Generate();
// completed.Status == "Completed"
// completed.CompletedAtUtc != null

// Compensating saga
var compensating = new SagaStateFaker().AsCompensating().Generate();
// compensating.Status == "Compensating"

// Failed saga
var failed = new SagaStateFaker().AsFailed("Payment declined").Generate();
// failed.Status == "Failed"
// failed.ErrorMessage == "Payment declined"

// Timed out saga
var timedOut = new SagaStateFaker().AsTimedOut().Generate();
// timedOut.Status == "TimedOut"

// Custom saga
var custom = new SagaStateFaker()
    .WithSagaType("OrderFulfillmentSaga")
    .WithSagaId(Guid.Parse("..."))
    .WithData("{\"orderId\": \"123\"}")
    .AtStep(3)
    .Generate();
```

## ScheduledMessageFaker

Generate scheduled messages for scheduling tests:

```csharp
var faker = new ScheduledMessageFaker();

// Future scheduled message (default)
var future = faker.Generate();
// future.IsDue() == false

// Due message (ready to execute)
var due = new ScheduledMessageFaker().AsDue().Generate();
// due.IsDue() == true

// Processed message
var processed = new ScheduledMessageFaker().AsProcessed().Generate();
// processed.IsProcessed == true

// Recurring message
var recurring = new ScheduledMessageFaker()
    .AsRecurring("0 0 * * *")  // Daily at midnight
    .Generate();
// recurring.IsRecurring == true
// recurring.CronExpression == "0 0 * * *"

// Recurring with last execution
var executed = new ScheduledMessageFaker()
    .AsRecurringExecuted()
    .Generate();
// executed.LastExecutedAtUtc != null

// Custom schedule
var custom = new ScheduledMessageFaker()
    .WithRequestType("SendDailyReport")
    .ScheduledAt(DateTime.UtcNow.AddHours(1))
    .Generate();
```

## Generating Multiple Items

```csharp
var faker = new OutboxMessageFaker();

// Generate 10 messages
var messages = faker.Generate(10);

// Generate with different states
var pending = new OutboxMessageFaker().Generate(5);
var processed = new OutboxMessageFaker().AsProcessed().Generate(3);
var failed = new OutboxMessageFaker().AsFailed().Generate(2);
```

## Localization

```csharp
// Spanish locale
var faker = new EncinaFaker<Customer>("es")
    .RuleFor(c => c.Name, f => f.Name.FullName())
    .RuleFor(c => c.Address, f => f.Address.FullAddress());

// Change locale after creation
var faker2 = new EncinaFaker<Customer>().WithLocale("de");
```

## Custom Domain Fakers

Create reusable fakers for your domain entities:

```csharp
public sealed class CreateOrderFaker : EncinaFaker<CreateOrder>
{
    public CreateOrderFaker()
    {
        RuleFor(o => o.CustomerId, f => f.Random.UserId());
        RuleFor(o => o.ShippingAddress, f => f.Address.FullAddress());
        RuleFor(o => o.Items, f => new OrderItemFaker().Generate(f.Random.Int(1, 5)));
    }

    public CreateOrderFaker WithCustomer(string customerId)
    {
        RuleFor(o => o.CustomerId, _ => customerId);
        return this;
    }

    public CreateOrderFaker WithItems(int count)
    {
        RuleFor(o => o.Items, f => new OrderItemFaker().Generate(count));
        return this;
    }
}

// Usage
var order = new CreateOrderFaker()
    .WithCustomer("customer-123")
    .WithItems(3)
    .Generate();
```

## Integration with Existing Tests

```csharp
public class OrderServiceTests
{
    private readonly OutboxMessageFaker _outboxFaker = new();
    private readonly SagaStateFaker _sagaFaker = new();

    [Fact]
    public async Task ProcessOrder_ShouldCreateOutboxMessage()
    {
        // Arrange - Generate realistic test data
        var expectedMessage = _outboxFaker
            .WithNotificationType("OrderCreated")
            .Generate();

        // Act
        await _service.ProcessOrderAsync(order);

        // Assert
        var message = await _outboxStore.GetByIdAsync(expectedMessage.Id);
        message.NotificationType.ShouldBe("OrderCreated");
    }

    [Fact]
    public async Task SagaProcessor_ShouldHandleFailedSaga()
    {
        // Arrange - Generate a failed saga
        var failedSaga = _sagaFaker
            .AsFailed("External service unavailable")
            .Generate();

        // Act & Assert
        await _processor.HandleFailedSagaAsync(failedSaga);
    }
}
```

## Request/Command/Query Fakers

Create fakers for CQRS requests:

```csharp
// Command faker
public sealed class CreateOrderFaker : EncinaFaker<CreateOrderCommand>
{
    public CreateOrderFaker()
    {
        RuleFor(c => c.CustomerId, f => f.Random.GuidStronglyTypedIdValue());
        RuleFor(c => c.CorrelationId, f => f.Random.CorrelationId());
        RuleFor(c => c.Items, f => new OrderItemFaker().Generate(f.Random.Int(1, 5)));
    }
}

// Query faker
public sealed class GetOrderByIdFaker : EncinaFaker<GetOrderByIdQuery>
{
    public GetOrderByIdFaker()
    {
        RuleFor(q => q.OrderId, f => f.Random.GuidStronglyTypedIdValue());
        RuleFor(q => q.UserId, f => f.Random.UserId());
        RuleFor(q => q.TenantId, f => f.Random.TenantId());
    }
}

// Usage
var command = new CreateOrderFaker().Generate();
var query = new GetOrderByIdFaker().Generate();
```

## Entity and Aggregate Fakers

Create fakers for domain entities:

```csharp
// Entity faker
public sealed class ProductFaker : EncinaFaker<Product>
{
    public ProductFaker()
    {
        CustomInstantiator(f => new Product(
            ProductId.From(f.Random.IntStronglyTypedIdValue())));

        RuleFor(p => p.Name, f => f.Commerce.ProductName());
        RuleFor(p => p.Price, f => f.Finance.Amount(10, 1000));
        RuleFor(p => p.Quantity, f => Quantity.From(f.Random.QuantityValue(1, 100)));
    }
}

// Aggregate faker with domain events
public sealed class OrderFaker : EncinaFaker<Order>
{
    public OrderFaker()
    {
        CustomInstantiator(f => new Order(
            OrderId.From(f.Random.GuidStronglyTypedIdValue()),
            CustomerId.From(f.Random.GuidStronglyTypedIdValue())));

        RuleFor(o => o.Status, _ => OrderStatus.Pending);
        RuleFor(o => o.CreatedAtUtc, f => f.Date.RecentUtc(30));
    }

    public OrderFaker AsShipped()
    {
        RuleFor(o => o.Status, _ => OrderStatus.Shipped);
        RuleFor(o => o.ShippedAtUtc, f => f.Date.RecentUtc(7));
        return this;
    }
}

// Usage
var product = new ProductFaker().Generate();
var pendingOrder = new OrderFaker().Generate();
var shippedOrder = new OrderFaker().AsShipped().Generate();
```

## Related Packages

- **Encina.Testing.Fakes** - In-memory fakes for IEncina and stores
- **Encina.Testing.Shouldly** - Shouldly assertions for Either and Aggregates
- **Encina.Testing.Respawn** - Database reset for integration tests
- **Encina.Testing.WireMock** - HTTP API mocking

## License

MIT License - see LICENSE file for details.
