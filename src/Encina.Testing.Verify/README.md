# Encina.Testing.Verify

Verify snapshot testing integration for Encina. Provides helpers for snapshot testing Either results, aggregates, sagas, and messaging entities with automatic scrubbing of timestamps and IDs.

## Installation

```bash
dotnet add package Encina.Testing.Verify
```

For xUnit tests, also add:

```bash
dotnet add package Verify.Xunit
```

## Quick Start

### 1. Initialize Verify Settings

Add a module initializer to configure Verify with Encina-specific scrubbers:

```csharp
using System.Runtime.CompilerServices;
using Encina.Testing.Verify;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        EncinaVerifySettings.Initialize();
    }
}
```

### 2. Use in Tests

```csharp
using Encina.Testing.Verify;

public class OrderHandlerTests
{
    [Fact]
    public async Task CreateOrder_ReturnsExpectedResponse()
    {
        // Arrange
        var handler = new CreateOrderHandler();
        var command = new CreateOrder { CustomerId = "CUST-001" };

        // Act
        var result = await handler.Handle(command);

        // Assert - snapshot testing
        await Verifier.Verify(EncinaVerify.PrepareEither(result));
    }
}
```

## Features

### Either Result Verification

```csharp
// Verify any Either result (shows IsRight, Value or Error)
var result = await handler.Handle(command);
await Verifier.Verify(EncinaVerify.PrepareEither(result));

// Extract and verify success value (throws if error)
var response = EncinaVerify.ExtractSuccess(result);
await Verifier.Verify(response);

// Extract and verify error (throws if success)
var error = EncinaVerify.ExtractError(result);
await Verifier.Verify(error);
```

### Aggregate Event Verification

```csharp
// Verify uncommitted events from an aggregate
var order = new OrderAggregate();
order.Create("CUST-001", items);
order.Confirm();

await Verifier.Verify(EncinaVerify.PrepareUncommittedEvents(order));
```

Output:
```json
{
  "AggregateId": "Guid_1",
  "AggregateVersion": 2,
  "EventCount": 2,
  "Events": [
    {
      "Index": 0,
      "EventType": "OrderCreated",
      "Event": { "OrderId": "Guid_1", "CustomerId": "CUST-001" }
    },
    {
      "Index": 1,
      "EventType": "OrderConfirmed",
      "Event": { "OrderId": "Guid_1" }
    }
  ]
}
```

### Messaging Entity Verification

```csharp
// Outbox messages
var messages = await outboxStore.GetPendingAsync();
await Verifier.Verify(EncinaVerify.PrepareOutboxMessages(messages));

// Inbox messages
var inboxMessages = await inboxStore.GetAllAsync();
await Verifier.Verify(EncinaVerify.PrepareInboxMessages(inboxMessages));

// Saga state
var saga = await sagaStore.GetAsync(sagaId);
await Verifier.Verify(EncinaVerify.PrepareSagaState(saga));

// Scheduled messages
var scheduled = await scheduledStore.GetPendingAsync();
await Verifier.Verify(EncinaVerify.PrepareScheduledMessages(scheduled));

// Dead letter messages
var deadLetters = await deadLetterStore.GetAllAsync();
await Verifier.Verify(EncinaVerify.PrepareDeadLetterMessages(deadLetters));
```

## Automatic Scrubbing

When `EncinaVerifySettings.Initialize()` is called, the following are automatically scrubbed:

### Timestamps
All Encina timestamp properties are scrubbed:
- `CreatedAtUtc`, `ProcessedAtUtc`, `ReceivedAtUtc`
- `ScheduledAtUtc`, `StartedAtUtc`, `CompletedAtUtc`
- `LastUpdatedAtUtc`, `ExpiresAtUtc`, `NextRetryAtUtc`
- `DeadLetteredAtUtc`, `FirstFailedAtUtc`, `ReplayedAtUtc`
- `LastExecutedAtUtc`, `TimeoutAtUtc`

### GUIDs
GUIDs are replaced with deterministic placeholders (`Guid_1`, `Guid_2`, etc.) for stable snapshots.

### Stack Traces
Stack traces in error messages are automatically removed.

### ISO 8601 Timestamps
Any ISO 8601 timestamp in content is replaced with `[TIMESTAMP]`.

## API Reference

### EncinaVerifySettings

| Method | Description |
|--------|-------------|
| `Initialize()` | Configures Verify with Encina scrubbers and converters. Idempotent. |

### EncinaVerify

| Method | Description |
|--------|-------------|
| `PrepareEither<TLeft, TRight>(either)` | Prepares an Either for snapshot verification |
| `ExtractSuccess<TResponse>(result)` | Extracts success value or throws |
| `ExtractError<TResponse>(result)` | Extracts error or throws |
| `PrepareUncommittedEvents(aggregate)` | Prepares aggregate events for verification |
| `PrepareOutboxMessages(messages)` | Prepares outbox messages for verification |
| `PrepareInboxMessages(messages)` | Prepares inbox messages for verification |
| `PrepareSagaState(sagaState)` | Prepares saga state for verification |
| `PrepareScheduledMessages(messages)` | Prepares scheduled messages for verification |
| `PrepareDeadLetterMessages(messages)` | Prepares dead letter messages for verification |

## Dependencies

- [Verify](https://github.com/VerifyTests/Verify) - Snapshot testing framework
- Encina - Core library
- Encina.Messaging - Messaging patterns
- Encina.DomainModeling - Aggregate support

## License

This project is licensed under the MIT License.
