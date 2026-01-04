# Phase 4: Messaging Test Doubles Checklist

**Goal**: Use FakeOutboxStore/FakeInboxStore for messaging pattern tests.

**Estimated Effort**: 2-4 hours per project

---

## Pre-Migration Checklist

- [ ] Phase 3 completed
- [ ] Run `dotnet test` to establish baseline
- [ ] Create branch: `feature/testing-dogfooding-phase4`

## Find and Replace Patterns

### Pattern 1: Mock Outbox Store

**Search for**:
```csharp
var mockStore = Substitute.For<IOutboxStore>();
mockStore.AddAsync(Arg.Any<IOutboxMessage>(), Arg.Any<CancellationToken>())
    .Returns(Task.CompletedTask);
```

**Replace with**:
```csharp
var store = new FakeOutboxStore();
// No setup needed - FakeOutboxStore tracks all operations
```

### Pattern 2: Verify Message Was Added

**Search for**:
```csharp
await mockStore.Received(1).AddAsync(
    Arg.Is<IOutboxMessage>(m => m.NotificationType.Contains("OrderCreated")),
    Arg.Any<CancellationToken>());
```

**Replace with**:
```csharp
store.WasMessageAdded<OrderCreatedEvent>().ShouldBeTrue();
// Or for more detail:
store.AddedMessages.Count.ShouldBe(1);
store.AddedMessages[0].Content.ShouldContain("orderId");
```

### Pattern 3: Verify Message Content

**Search for**:
```csharp
mockStore.ReceivedCalls()
    .Where(c => c.GetMethodInfo().Name == "AddAsync")
    .Select(c => c.GetArguments()[0])
    .Cast<IOutboxMessage>()
    .First().Content.ShouldContain("expected");
```

**Replace with**:
```csharp
var message = store.AddedMessages[0];
message.Content.ShouldContain("expected");
```

### Pattern 4: Mock Inbox Store

**Search for**:
```csharp
var mockInbox = Substitute.For<IInboxStore>();
mockInbox.GetMessageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
    .Returns((IInboxMessage?)null);
```

**Replace with**:
```csharp
var store = new FakeInboxStore();
// Empty by default - returns null for GetMessageAsync
```

### Pattern 5: Inbox Idempotency Testing

**Search for**:
```csharp
mockInbox.GetMessageAsync("msg-123", Arg.Any<CancellationToken>())
    .Returns(new InboxMessage { MessageId = "msg-123", ProcessedAtUtc = DateTime.UtcNow });
```

**Replace with**:
```csharp
var store = new FakeInboxStore();
await store.AddAsync(new FakeInboxMessage
{
    MessageId = "msg-123",
    ProcessedAtUtc = DateTime.UtcNow,
    Response = """{"result":"cached"}"""
});
store.IsMessageProcessed("msg-123").ShouldBeTrue();
```

## Add Required Usings

```csharp
using Encina.Testing.Fakes.Stores;
using Encina.Testing.Fakes.Models;
using Encina.Testing.Messaging;  // For test helpers
```

## Use Type Aliases (If Needed)

If you have conflicts with Bogus fakers:
```csharp
using FakeOutbox = Encina.Testing.Fakes.Stores.FakeOutboxStore;
using FakeOutboxMsg = Encina.Testing.Fakes.Models.FakeOutboxMessage;
```

## Given/When/Then Pattern

For more expressive tests, use the test helpers:

```csharp
[Fact]
public void OutboxPattern_MessageIsAdded()
{
    var helper = new OutboxTestHelper();

    helper.GivenEmptyOutbox();

    helper.WhenMessageAdded(new OrderCreatedEvent(
        OrderId: Guid.NewGuid(),
        CustomerId: "CUST-001",
        Amount: 100m,
        CreatedAtUtc: DateTime.UtcNow));

    helper.ThenOutboxContains<OrderCreatedEvent>();
    helper.ThenOutboxHasCount(1);
}
```

## Grep Patterns to Find Candidates

```bash
grep -r "Substitute.For<IOutboxStore>" tests/
grep -r "Substitute.For<IInboxStore>" tests/
grep -r "mock.*OutboxStore" tests/
grep -r "\.Received.*AddAsync" tests/
```

## Verification

- [ ] Run `dotnet test` - all tests still pass
- [ ] Remove NSubstitute mocks where FakeOutboxStore/FakeInboxStore used
- [ ] Tests are more readable with dedicated assertions

## Post-Migration

- [ ] Commit: `refactor(tests): migrate to FakeOutboxStore and FakeInboxStore`
- [ ] Update tracking issue #498

---

## FakeOutboxStore API Reference

### Properties
```csharp
store.Messages                 // All messages (read-only)
store.AddedMessages           // Messages added (for verification)
store.ProcessedMessageIds     // IDs of processed messages
store.FailedMessageIds        // IDs of failed messages
store.SaveChangesCallCount    // Times SaveChangesAsync was called
```

### Methods
```csharp
store.WasMessageAdded<T>()           // Check by generic type
store.WasMessageAdded("TypeName")    // Check by type name string
store.GetMessage(messageId)          // Get specific message
store.Clear()                        // Reset all state
store.ClearTracking()               // Clear only tracking data
```

## FakeInboxStore API Reference

### Properties
```csharp
store.Messages                  // All messages
store.AddedMessages            // Messages added
store.ProcessedMessageIds      // Processed message IDs
store.FailedMessageIds         // Failed message IDs
store.RemovedMessageIds        // Removed message IDs
```

### Methods
```csharp
store.IsMessageProcessed(messageId)  // Check if processed
store.GetMessage(messageId)          // Get specific message
store.Clear()                        // Reset all state
```
