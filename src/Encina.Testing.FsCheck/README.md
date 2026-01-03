# Encina.Testing.FsCheck

FsCheck property-based testing extensions for Encina. Includes arbitraries for core types, messaging entities, and common properties for validating handler invariants.

## Installation

```bash
dotnet add package Encina.Testing.FsCheck
```

## Why Property-Based Testing?

Property-based testing complements example-based tests by:

- **Testing invariants** across many random inputs
- **Finding edge cases** automatically
- **Ensuring properties hold** regardless of input
- **Validating handler idempotency**, commutativity, and other behavioral properties

## Quick Start

### 1. Register Arbitraries

```csharp
using Encina.Testing.FsCheck;

// Register all Encina arbitraries once (typically in test setup)
EncinaArbitraries.Register();
```

### 2. Write Property Tests

```csharp
using FsCheck;
using FsCheck.Xunit;
using Encina.Testing.FsCheck;

public class MyPropertyTests : PropertyTestBase  // Auto-registers arbitraries
{
    [EncinaProperty]  // Uses Encina arbitraries automatically
    public Property EncinaError_HasNonEmptyMessage(EncinaError error)
    {
        return EncinaProperties.ErrorHasNonEmptyMessage(error);
    }

    [EncinaProperty]
    public Property Either_IsExclusivelyLeftOrRight(Either<EncinaError, int> either)
    {
        return EncinaProperties.EitherIsExclusive(either);
    }
}
```

## Available Arbitraries

### Core Types

| Arbitrary | Description |
|-----------|-------------|
| `EncinaArbitraries.EncinaError()` | Generates errors with various messages |
| `EncinaArbitraries.EncinaErrorWithException()` | Errors with exception metadata |
| `EncinaArbitraries.RequestContext()` | Request contexts with random properties |
| `EncinaArbitraries.EitherOf<T>()` | Either values (Left or Right) |
| `EncinaArbitraries.SuccessEither<T>()` | Only Right values |
| `EncinaArbitraries.FailureEither<T>()` | Only Left values |

### Messaging Types

| Arbitrary | Description |
|-----------|-------------|
| `EncinaArbitraries.OutboxMessage()` | Outbox messages in various states |
| `EncinaArbitraries.PendingOutboxMessage()` | Only pending (unprocessed) messages |
| `EncinaArbitraries.FailedOutboxMessage()` | Messages with error information |
| `EncinaArbitraries.InboxMessage()` | Inbox messages in various states |
| `EncinaArbitraries.SagaState()` | Saga states across lifecycle stages |
| `EncinaArbitraries.ScheduledMessage()` | Scheduled messages |
| `EncinaArbitraries.RecurringScheduledMessage()` | Recurring messages with cron expressions |

## Pre-Built Properties

### Either Properties

```csharp
// Verify Either is exclusively Left or Right
EncinaProperties.EitherIsExclusive(either);

// Verify Map preserves Right state
EncinaProperties.MapPreservesRightState(either, x => x * 2);

// Verify Map preserves Left error
EncinaProperties.MapPreservesLeftError(either, x => x.ToString());
```

### EncinaError Properties

```csharp
// Verify error has non-empty message
EncinaProperties.ErrorHasNonEmptyMessage(error);

// Verify string-to-error preserves message
EncinaProperties.ErrorFromStringPreservesMessage(message);
```

### RequestContext Properties

```csharp
// Verify context has correlation ID
EncinaProperties.ContextHasCorrelationId(context);

// Verify WithMetadata is immutable
EncinaProperties.WithMetadataIsImmutable(context, key, value);
```

### Messaging Properties

```csharp
// Outbox
EncinaProperties.OutboxProcessedStateIsConsistent(message);
EncinaProperties.OutboxDeadLetterIsConsistent(message, maxRetries);
EncinaProperties.OutboxHasRequiredFields(message);

// Inbox
EncinaProperties.InboxProcessedStateIsConsistent(message);
EncinaProperties.InboxHasRequiredFields(message);

// Saga
EncinaProperties.SagaStatusIsValid(state);
EncinaProperties.SagaHasRequiredFields(state);
EncinaProperties.SagaCurrentStepIsNonNegative(state);

// Scheduled
EncinaProperties.RecurringHasCronExpression(message);
EncinaProperties.ScheduledHasRequiredFields(message);
```

### Handler Properties

```csharp
// Verify handler is deterministic (pure function)
EncinaProperties.HandlerIsDeterministic(handler, request);

// Async version
EncinaProperties.AsyncHandlerIsDeterministic(asyncHandler, request);
```

## Generator Extensions

### Either Generators

```csharp
using FsCheck.Fluent;

// Generate Either values from any generator
var eitherGen = ArbMap.Default.GeneratorFor<int>().ToEither();

// Generate only successes
var successGen = ArbMap.Default.GeneratorFor<int>().ToSuccess();

// Generate only failures
var failureGen = EncinaArbitraries.EncinaError().Generator.ToFailure<int>();
```

### Nullable Generators

```csharp
// Generate nullable reference types (20% null by default)
var nullableGen = GenExtensions.NonEmptyString().OrNull(0.3);

// Generate nullable value types
var nullableIntGen = ArbMap.Default.GeneratorFor<int>().OrNullValue(0.2);
```

### String Generators

```csharp
// Non-empty strings
var gen = GenExtensions.NonEmptyString();

// Alphanumeric strings with length bounds
var alphaGen = GenExtensions.AlphaNumericString(5, 20);

// Email addresses
var emailGen = GenExtensions.EmailAddress();
```

### JSON Generators

```csharp
// Generate JSON objects with up to 5 properties
var jsonGen = GenExtensions.JsonObject(5);
```

### DateTime Generators

```csharp
// UTC dates within range of today
var dateGen = GenExtensions.UtcDateTime(365);

// Past dates only
var pastGen = GenExtensions.PastUtcDateTime(30);

// Future dates only
var futureGen = GenExtensions.FutureUtcDateTime(30);
```

### Other Generators

```csharp
// Cron expressions
var cronGen = GenExtensions.CronExpression();

// Positive decimals
var decimalGen = GenExtensions.PositiveDecimal(0.01m, 100m);

// Collections
var listGen = ArbMap.Default.GeneratorFor<int>().ListOf(2, 10);
var nonEmptyListGen = ArbMap.Default.GeneratorFor<int>().NonEmptyListOf(5);
```

## Property Attributes

| Attribute | Description |
|-----------|-------------|
| `[EncinaProperty]` | Standard property with 100 tests |
| `[EncinaProperty(50)]` | Custom number of tests |
| `[QuickProperty]` | Fast feedback with 20 tests |
| `[ThoroughProperty]` | Comprehensive with 1000 tests |

## Base Classes

### PropertyTestBase

Inherit from `PropertyTestBase` to auto-register Encina arbitraries:

```csharp
public class MyTests : PropertyTestBase
{
    [EncinaProperty]
    public Property MyProperty(EncinaError error)
    {
        // Encina types are automatically generated
        return error.Message != null;
    }
}
```

### Configuration Constants

```csharp
// Use these constants to configure tests:
PropertyTestConfig.DefaultMaxTest;  // 100 tests
PropertyTestConfig.QuickMaxTest;    // 20 tests
PropertyTestConfig.ThoroughMaxTest; // 1000 tests
PropertyTestConfig.DefaultEndSize;  // 100
PropertyTestConfig.ThoroughEndSize; // 200
```

## Example: Testing a Handler

```csharp
using FsCheck;
using FsCheck.Fluent;
using Encina.Testing.FsCheck;

public class CreateOrderHandlerPropertyTests : PropertyTestBase
{
    private readonly CreateOrderHandler _handler;

    public CreateOrderHandlerPropertyTests()
    {
        _handler = new CreateOrderHandler(/* dependencies */);
    }

    [EncinaProperty]
    public async Task<Property> CreateOrder_AlwaysReturnsValidOrderId(CreateOrderCommand command)
    {
        // Arrange & Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - use .ToProperty() pattern for FsCheck 3.x
        return result.Match(
            Left: error => (
                !string.IsNullOrWhiteSpace(error.Code) &&
                !string.IsNullOrWhiteSpace(error.Message) &&
                error.Code.StartsWith("ORDER_", StringComparison.Ordinal)
            ).ToProperty().Label($"Error should have valid code and message: {error}"),
            Right: orderId => (orderId.Value != Guid.Empty)
                .ToProperty().Label("OrderId should not be empty")
        );
    }

    [EncinaProperty]
    public Property CreateOrder_IsDeterministic(CreateOrderCommand command)
    {
        return EncinaProperties.AsyncHandlerIsDeterministic(
            (cmd, ct) => _handler.Handle(cmd, ct),
            command);
    }
}
```

## Related Packages

- **[Encina.Testing.Bogus](https://www.nuget.org/packages/Encina.Testing.Bogus)** - Test data generation with Bogus
- **[Encina.Testing.Fakes](https://www.nuget.org/packages/Encina.Testing.Fakes)** - In-memory test doubles
- **[Encina.Testing.Shouldly](https://www.nuget.org/packages/Encina.Testing.Shouldly)** - Fluent assertions for Either

## License

MIT License - See LICENSE file for details.
