# Common Migration Patterns and Edge Cases

This document covers frequently encountered patterns and their solutions during the Encina.Testing.* migration.

---

## Common Patterns

### 1. Either Assertion Chains

**Before**:
```csharp
var result = await handler.Handle(command);
result.IsRight.ShouldBeTrue();
var order = result.Match(Right: o => o, Left: _ => throw new Exception());
order.Status.ShouldBe("Created");
order.CustomerId.ShouldBe("CUST-001");
```

**After**:
```csharp
var result = await handler.Handle(command);
var order = result.ShouldBeSuccess();
order.Status.ShouldBe("Created");
order.CustomerId.ShouldBe("CUST-001");
```

### 2. Multiple Error Assertions

**Before**:
```csharp
result.IsLeft.ShouldBeTrue();
result.IfLeft(err =>
{
    err.Code.ShouldStartWith("encina.validation");
    err.Message.ShouldContain("CustomerId");
});
```

**After**:
```csharp
var error = result.ShouldBeError();
error.Code.ShouldStartWith("encina.validation");
error.Message.ShouldContain("CustomerId");

// Or use specific assertion:
result.ShouldBeValidationError();
```

### 3. Conditional Error Checks

**Before**:
```csharp
if (result.IsLeft)
{
    result.IfLeft(err => err.Code.ShouldBe("specific.error"));
}
else
{
    Assert.Fail("Expected error");
}
```

**After**:
```csharp
result.ShouldBeErrorWithCode("specific.error");
```

### 4. Nullable Value Extraction

**Before**:
```csharp
var maybeValue = result.Match(Right: v => (int?)v, Left: _ => null);
maybeValue.HasValue.ShouldBeTrue();
maybeValue.Value.ShouldBe(42);
```

**After**:
```csharp
var value = result.ShouldBeSuccess();
value.ShouldBe(42);
```

### 5. Complex Faker Rules

**Before**:
```csharp
var faker = new Faker<Order>()
    .RuleFor(o => o.Id, f => Guid.NewGuid())
    .RuleFor(o => o.CustomerId, f => $"CUST-{f.Random.Number(1000, 9999)}")
    .RuleFor(o => o.CreatedAt, f => f.Date.Recent())
    .RuleFor(o => o.Amount, f => f.Finance.Amount(10, 1000));
```

**After**:
```csharp
var faker = new EncinaFaker<Order>()
    .RuleFor(o => o.Id, f => f.Random.GuidEntityId())
    .RuleFor(o => o.CustomerId, f => f.Random.UserId("CUST"))
    .RuleFor(o => o.CreatedAt, f => f.Date.RecentUtc())
    .RuleFor(o => o.Amount, f => f.Finance.Amount(10, 1000));
```

### 6. Outbox Message Verification

**Before**:
```csharp
await mockOutbox.Received(1).AddAsync(
    Arg.Is<IOutboxMessage>(m =>
        m.NotificationType.Contains("OrderCreated") &&
        m.Content.Contains(orderId.ToString())),
    Arg.Any<CancellationToken>());
```

**After**:
```csharp
store.WasMessageAdded<OrderCreatedEvent>().ShouldBeTrue();
var message = store.AddedMessages.Single();
message.Content.ShouldContain(orderId.ToString());
```

---

## Edge Cases

### 1. Type Ambiguity: FakeOutboxMessage

**Problem**: `FakeOutboxMessage` exists in both `Encina.Testing.Bogus` and `Encina.Testing.Fakes.Models`.

**Solution**: Use type aliases:
```csharp
using FakeOutboxMsg = Encina.Testing.Fakes.Models.FakeOutboxMessage;
using BogusOutboxMsg = Encina.Testing.Bogus.FakeOutboxMessage;

// Use the Fakes version for stores
var message = new FakeOutboxMsg { Id = Guid.NewGuid(), ... };
await store.AddAsync(message);
```

### 2. Missing Namespace: FsCheck.Fluent

**Problem**: `.ToProperty()` extension not found.

**Solution**: Add the required using:
```csharp
using FsCheck.Fluent;  // Required for .ToProperty()
```

### 3. Messaging Namespace Spelling

**Problem**: `Encina.Messaging.Saga` doesn't exist.

**Solution**: Use plural form:
```csharp
using Encina.Messaging.Sagas;  // Note the 's'
```

### 4. EncinaErrors Factory Method

**Problem**: `EncinaErrors.Validation()` or `EncinaErrors.NotFound()` don't exist.

**Solution**: Use the generic `Create` method:
```csharp
// Instead of: EncinaErrors.Validation("CustomerId", "Required")
EncinaErrors.Create("encina.validation.customerid", "CustomerId is required");

// Instead of: EncinaErrors.NotFound("Order", orderId)
EncinaErrors.Create("encina.notfound.order", $"Order {orderId} not found");
```

### 5. IOutboxMessageFactory Signature

**Problem**: `IOutboxMessageFactory.Create(object)` doesn't exist.

**Solution**: Use the full signature:
```csharp
var message = factory.Create(
    Guid.NewGuid(),                                    // id
    typeof(OrderCreatedEvent).FullName!,              // notificationType
    JsonSerializer.Serialize(notification),            // content
    DateTime.UtcNow);                                 // createdAtUtc
```

### 6. Either.Right Constructor Syntax

**Problem**: LanguageExt requires explicit type for Either construction.

**Solution**:
```csharp
// This works:
Either<EncinaError, OrderDto>.Right(new OrderDto { ... });

// This also works (if type can be inferred):
var result = new OrderDto { ... };
Either<EncinaError, OrderDto> either = result;
```

### 7. Pact HTTP Path Convention

**Problem**: Pact mock server expects specific paths.

**Solution**: Use standard Encina paths:
```csharp
// Commands: POST /api/commands/{CommandTypeName}
// Queries: POST /api/queries/{QueryTypeName}
// Notifications: POST /api/notifications/{NotificationTypeName}

await client.PostAsJsonAsync("/api/queries/GetOrderQuery", query);
```

### 8. Property Test Return Type

**Problem**: Property tests return `Property`, not `bool`.

**Solution**:
```csharp
// Wrong:
[EncinaProperty]
public bool MyProperty(int x) => x >= 0;

// Correct:
[EncinaProperty]
public Property MyProperty(int x) => (x >= 0).ToProperty();
```

### 9. FakeOutboxStore Thread Safety

**Problem**: Concurrent test access to FakeOutboxStore.

**Solution**: FakeOutboxStore uses `ConcurrentDictionary` internally and is thread-safe. No additional synchronization needed.

### 10. Saga Namespace vs Sagas Namespace

**Problem**: Different casing in different packages.

**Reference**:
```csharp
// Correct namespaces:
using Encina.Messaging.Sagas;          // For ISagaState, ISagaStore
using Encina.Messaging.Sagas.LowCeremony;  // For SagaRunner
```

---

## Troubleshooting

### Build Errors After Migration

1. **Clear NuGet caches**:
   ```bash
   dotnet nuget locals all --clear
   dotnet restore --force-evaluate
   ```

2. **Rebuild from clean**:
   ```bash
   dotnet clean
   dotnet build
   ```

### Tests Fail After Migration

1. **Check for behavior changes**: Encina.Testing.Shouldly assertions may have stricter checks.

2. **Check nullable handling**: `ShouldBeSuccess()` throws if result is Left.

3. **Check seed reproducibility**: If tests relied on random data, now they're deterministic.

### IDE IntelliSense Missing

1. **Restart IDE** after adding package references.
2. **Check global usings** are correctly configured.
3. **Verify project builds** successfully.
