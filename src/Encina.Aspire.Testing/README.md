# Encina.Aspire.Testing

Integration testing support for Encina-based applications using .NET Aspire's `DistributedApplicationTestingBuilder`.

## Installation

```bash
dotnet add package Encina.Aspire.Testing
```

## Features

- **WithEncinaTestSupport()** - Extension for `DistributedApplicationTestingBuilder`
- **Test data reset helpers** - Clear outbox, inbox, sagas, scheduled messages, dead letters
- **Assertion extensions** - Verify messaging patterns (outbox, inbox, saga, dead letter)
- **Wait helpers** - Wait for async operations to complete
- **Failure simulation** - Simulate timeouts, failures, and dead letters

## Quick Start

```csharp
using System.Net.Http.Json;
using Aspire.Hosting.Testing;
using Encina.Aspire.Testing;
using Xunit;

public class OrderSagaIntegrationTests : IAsyncLifetime
{
    private DistributedApplication _app = null!;

    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.MyAppHost>();

        // Add Encina test support
        builder.WithEncinaTestSupport(options =>
        {
            options.ClearOutboxBeforeTest = true;
            options.ClearInboxBeforeTest = true;
            options.ResetSagasBeforeTest = true;
            options.DefaultWaitTimeout = TimeSpan.FromSeconds(30);
        });

        _app = await builder.BuildAsync();
        await _app.StartAsync();
    }

    [Fact]
    public async Task CreateOrder_ShouldPublishEvent()
    {
        // Arrange
        var httpClient = _app.CreateHttpClient("api");

        // Act
        await httpClient.PostAsJsonAsync("/orders", new { ProductId = "123" });

        // Assert - Verify outbox contains the event
        await _app.AssertOutboxContainsAsync<OrderCreatedEvent>();
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();
}
```

## Configuration Options

```csharp
builder.WithEncinaTestSupport(options =>
{
    // Data cleanup before tests (all true by default)
    options.ClearOutboxBeforeTest = true;
    options.ClearInboxBeforeTest = true;
    options.ResetSagasBeforeTest = true;
    options.ClearScheduledMessagesBeforeTest = true;
    options.ClearDeadLetterBeforeTest = true;

    // Wait operation settings
    options.DefaultWaitTimeout = TimeSpan.FromSeconds(30);
    options.PollingInterval = TimeSpan.FromMilliseconds(100);
});
```

## Assertions

### Outbox Assertions

```csharp
// Assert outbox contains a specific notification type
await app.AssertOutboxContainsAsync<OrderCreatedEvent>();

// Assert with custom predicate
await app.AssertOutboxContainsAsync(
    m => m.NotificationType.Contains("Order"),
    timeout: TimeSpan.FromSeconds(10));

// Get pending outbox messages for inspection
var pending = app.GetPendingOutboxMessages();
```

### Inbox Assertions

```csharp
// Assert a specific message was processed
await app.AssertInboxProcessedAsync("message-id-123");

// Assert a message type was processed
await app.AssertInboxProcessedAsync<ProcessPaymentCommand>();
```

### Saga Assertions

```csharp
// Assert saga completed
await app.AssertSagaCompletedAsync<OrderFulfillmentSaga>();

// Assert with predicate
await app.AssertSagaCompletedAsync<OrderFulfillmentSaga>(
    saga => saga.SagaId == expectedSagaId);

// Assert saga was compensated
await app.AssertSagaCompensatedAsync<OrderFulfillmentSaga>();

// Get running sagas
var running = app.GetRunningSagas<OrderFulfillmentSaga>();
```

### Dead Letter Assertions

```csharp
// Assert dead letter contains a message type
await app.AssertDeadLetterContainsAsync<FailedCommand>();

// Get all dead letter messages
var deadLetters = app.GetDeadLetterMessages();
```

## Wait Helpers

```csharp
// Wait for all outbox messages to be processed
await app.WaitForOutboxProcessingAsync(timeout: TimeSpan.FromSeconds(30));

// Wait for a specific saga to complete
await app.WaitForSagaCompletionAsync<OrderSaga>(sagaId);
```

## Failure Simulation

Simulate failures to test error handling and resilience:

```csharp
// Simulate saga timeout
app.SimulateSagaTimeout(sagaId);

// Simulate saga failure
app.SimulateSagaFailure(sagaId, "Payment gateway unavailable");

// Simulate outbox message failure
app.SimulateOutboxMessageFailure(messageId, "Network error");

// Simulate dead letter
app.SimulateOutboxDeadLetter(messageId, maxRetries: 3);

// Simulate inbox message failure
app.SimulateInboxMessageFailure("message-id", "Processing error");

// Simulate inbox expiration
app.SimulateInboxExpiration("message-id");

// Add message directly to dead letter
var dlId = await app.AddToDeadLetterAsync(
    requestType: "OrderCommand",
    requestContent: "{}",
    sourcePattern: "Outbox",
    errorMessage: "Max retries exceeded");
```

## Accessing Test Context

```csharp
// Get the test context for direct store access
var context = app.GetEncinaTestContext();

// Access individual stores
var outboxStore = context.OutboxStore;
var sagaStore = context.SagaStore;

// Clear specific stores
context.ClearOutbox();
context.ClearSagas();
context.ClearAll(); // Respects options
```

## Dependencies

- [Aspire.Hosting.Testing](https://www.nuget.org/packages/Aspire.Hosting.Testing) - .NET Aspire testing infrastructure
- [Encina.Testing.Fakes](../Encina.Testing.Fakes) - In-memory fake implementations

## Related Packages

- [Encina.Testing](../Encina.Testing) - Base testing utilities
- [Encina.Testing.Fakes](../Encina.Testing.Fakes) - Fake implementations for unit testing
- [Encina.Testing.Bogus](../Encina.Testing.Bogus) - Test data generation
- [Encina.Testing.Respawn](../Encina.Testing.Respawn) - Database cleanup

## License

MIT
