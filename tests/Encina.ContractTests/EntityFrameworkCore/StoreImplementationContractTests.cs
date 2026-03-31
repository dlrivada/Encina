using Encina.EntityFrameworkCore.Inbox;
using Encina.EntityFrameworkCore.Outbox;
using Encina.EntityFrameworkCore.Sagas;
using Encina.EntityFrameworkCore.Scheduling;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Encina.Testing.Shouldly;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Shouldly;
using SagaStatus = Encina.EntityFrameworkCore.Sagas.SagaStatus;

namespace Encina.ContractTests.EntityFrameworkCore;

/// <summary>
/// Contract tests that instantiate all 4 EF Core store implementations and execute real
/// CRUD operations against an InMemory database. This exercises the actual source code
/// in OutboxStoreEF, InboxStoreEF, SagaStoreEF, and ScheduledMessageStoreEF rather than
/// just using reflection.
/// </summary>
[Trait("Category", "Contract")]
[Trait("Feature", "Stores")]
public sealed class StoreImplementationContractTests : IDisposable
{
    private readonly ContractTestDbContext _dbContext;
    private readonly FakeTimeProvider _timeProvider;

    public StoreImplementationContractTests()
    {
        var options = new DbContextOptionsBuilder<ContractTestDbContext>()
            .UseInMemoryDatabase(databaseName: $"StoreContract_{Guid.NewGuid()}")
            .Options;

        _dbContext = new ContractTestDbContext(options);
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero));
    }

    public void Dispose() => _dbContext.Dispose();

    // ============================
    // OutboxStoreEF contracts
    // ============================

    [Fact]
    public async Task OutboxStore_AddAsync_ValidMessage_ShouldReturnRight()
    {
        // Exercises: OutboxStoreEF constructor (lines 32-37), AddAsync (lines 40-54)
        var store = new OutboxStoreEF(_dbContext, _timeProvider);
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "OrderPlacedEvent",
            Content = "{\"orderId\":1}",
            CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
        };

        var result = await store.AddAsync(message);

        result.ShouldBeRight();
    }

    [Fact]
    public async Task OutboxStore_AddAsync_WrongMessageType_ShouldReturnLeft()
    {
        // Exercises: OutboxStoreEF.AddAsync lines 44-49 (type check returning error)
        var store = new OutboxStoreEF(_dbContext, _timeProvider);
        var wrongMessage = NSubstitute.Substitute.For<IOutboxMessage>();

        var result = await store.AddAsync(wrongMessage);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task OutboxStore_MarkAsProcessed_ShouldSetTimestamp()
    {
        // Exercises: OutboxStoreEF.MarkAsProcessedAsync (lines 80-93) - sets ProcessedAtUtc, clears ErrorMessage
        var store = new OutboxStoreEF(_dbContext, _timeProvider);
        var id = Guid.NewGuid();
        _dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = id,
            NotificationType = "Test",
            Content = "{}",
            CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
        });
        await _dbContext.SaveChangesAsync();

        (await store.MarkAsProcessedAsync(id)).ShouldBeRight();
        (await store.SaveChangesAsync()).ShouldBeRight();

        var updated = await _dbContext.OutboxMessages.FindAsync(id);
        updated!.ProcessedAtUtc.ShouldNotBeNull();
        updated.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task OutboxStore_MarkAsFailed_ShouldSetErrorAndIncrementRetry()
    {
        // Exercises: OutboxStoreEF.MarkAsFailedAsync (lines 96-116)
        var store = new OutboxStoreEF(_dbContext, _timeProvider);
        var id = Guid.NewGuid();
        _dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = id,
            NotificationType = "Test",
            Content = "{}",
            CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            RetryCount = 0,
        });
        await _dbContext.SaveChangesAsync();

        var retryAt = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(5);
        (await store.MarkAsFailedAsync(id, "Connection timeout", retryAt)).ShouldBeRight();
        (await store.SaveChangesAsync()).ShouldBeRight();

        var updated = await _dbContext.OutboxMessages.FindAsync(id);
        updated!.ErrorMessage.ShouldBe("Connection timeout");
        updated.RetryCount.ShouldBe(1);
        updated.NextRetryAtUtc.ShouldBe(retryAt);
    }

    [Fact]
    public async Task OutboxStore_GetPendingMessages_ShouldReturnUnprocessed()
    {
        // Exercises: OutboxStoreEF.GetPendingMessagesAsync (lines 57-77)
        var store = new OutboxStoreEF(_dbContext, _timeProvider);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        _dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "Pending",
            Content = "{}",
            CreatedAtUtc = now.AddMinutes(-5),
        });
        _dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "Processed",
            Content = "{}",
            CreatedAtUtc = now.AddMinutes(-10),
            ProcessedAtUtc = now,
        });
        await _dbContext.SaveChangesAsync();

        var result = await store.GetPendingMessagesAsync(batchSize: 10, maxRetries: 3);
        result.IsRight.ShouldBeTrue();
        var messages = result.Match(Right: m => m.ToList(), Left: _ => []);
        messages.Count.ShouldBe(1);
        messages[0].NotificationType.ShouldBe("Pending");
    }

    [Fact]
    public async Task OutboxStore_SaveChangesAsync_ShouldPersist()
    {
        // Exercises: OutboxStoreEF.SaveChangesAsync (lines 119-125)
        var store = new OutboxStoreEF(_dbContext, _timeProvider);
        var msg = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            NotificationType = "SaveTest",
            Content = "{}",
            CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
        };

        (await store.AddAsync(msg)).ShouldBeRight();
        (await store.SaveChangesAsync()).ShouldBeRight();

        (await _dbContext.OutboxMessages.CountAsync()).ShouldBe(1);
    }

    // ============================
    // InboxStoreEF contracts
    // ============================

    [Fact]
    public async Task InboxStore_AddAndGet_ShouldRoundTrip()
    {
        // Exercises: InboxStoreEF constructor, AddAsync (lines 51-65), GetMessageAsync (lines 35-48)
        var store = new InboxStoreEF(_dbContext, _timeProvider);
        var messageId = $"msg-{Guid.NewGuid()}";
        var message = new InboxMessage
        {
            MessageId = messageId,
            RequestType = "CreateOrderCommand",
            ReceivedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            ExpiresAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddDays(7),
        };

        (await store.AddAsync(message)).ShouldBeRight();
        (await store.SaveChangesAsync()).ShouldBeRight();

        var getResult = await store.GetMessageAsync(messageId);
        getResult.IsRight.ShouldBeTrue();
        var opt = getResult.Match(Right: o => o, Left: _ => Option<IInboxMessage>.None);
        opt.IsSome.ShouldBeTrue();
    }

    [Fact]
    public async Task InboxStore_AddAsync_WrongMessageType_ShouldReturnLeft()
    {
        // Exercises: InboxStoreEF.AddAsync lines 55-59 (type check)
        var store = new InboxStoreEF(_dbContext, _timeProvider);
        var wrongMessage = NSubstitute.Substitute.For<IInboxMessage>();

        var result = await store.AddAsync(wrongMessage);
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task InboxStore_MarkAsProcessed_ShouldSetResponseAndTimestamp()
    {
        // Exercises: InboxStoreEF.MarkAsProcessedAsync (lines 68-85)
        var store = new InboxStoreEF(_dbContext, _timeProvider);
        var messageId = $"inbox-proc-{Guid.NewGuid()}";
        _dbContext.InboxMessages.Add(new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestCmd",
            ReceivedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            ExpiresAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddDays(7),
        });
        await _dbContext.SaveChangesAsync();

        (await store.MarkAsProcessedAsync(messageId, "{\"result\":\"ok\"}")).ShouldBeRight();
        (await store.SaveChangesAsync()).ShouldBeRight();

        var updated = await _dbContext.InboxMessages.FindAsync(messageId);
        updated!.ProcessedAtUtc.ShouldNotBeNull();
        updated.Response.ShouldBe("{\"result\":\"ok\"}");
        updated.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task InboxStore_MarkAsFailed_ShouldSetErrorAndIncrementRetry()
    {
        // Exercises: InboxStoreEF.MarkAsFailedAsync (lines 88-109)
        var store = new InboxStoreEF(_dbContext, _timeProvider);
        var messageId = $"inbox-fail-{Guid.NewGuid()}";
        _dbContext.InboxMessages.Add(new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestCmd",
            ReceivedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            ExpiresAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddDays(7),
        });
        await _dbContext.SaveChangesAsync();

        var retryAt = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(10);
        (await store.MarkAsFailedAsync(messageId, "Timeout", retryAt)).ShouldBeRight();
        (await store.SaveChangesAsync()).ShouldBeRight();

        var updated = await _dbContext.InboxMessages.FindAsync(messageId);
        updated!.ErrorMessage.ShouldBe("Timeout");
        updated.RetryCount.ShouldBe(1);
        updated.NextRetryAtUtc.ShouldBe(retryAt);
    }

    [Fact]
    public async Task InboxStore_IncrementRetryCount_ShouldIncrement()
    {
        // Exercises: InboxStoreEF.IncrementRetryCountAsync (lines 112-126)
        var store = new InboxStoreEF(_dbContext, _timeProvider);
        var messageId = $"inbox-retry-{Guid.NewGuid()}";
        _dbContext.InboxMessages.Add(new InboxMessage
        {
            MessageId = messageId,
            RequestType = "TestCmd",
            ReceivedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            ExpiresAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddDays(7),
            RetryCount = 2,
        });
        await _dbContext.SaveChangesAsync();

        (await store.IncrementRetryCountAsync(messageId)).ShouldBeRight();
        (await store.SaveChangesAsync()).ShouldBeRight();

        var updated = await _dbContext.InboxMessages.FindAsync(messageId);
        updated!.RetryCount.ShouldBe(3);
    }

    [Fact]
    public async Task InboxStore_GetExpiredMessages_ShouldReturnExpired()
    {
        // Exercises: InboxStoreEF.GetExpiredMessagesAsync (lines 129-145)
        var store = new InboxStoreEF(_dbContext, _timeProvider);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        _dbContext.InboxMessages.Add(new InboxMessage
        {
            MessageId = $"expired-{Guid.NewGuid()}",
            RequestType = "Test",
            ReceivedAtUtc = now.AddDays(-30),
            ExpiresAtUtc = now.AddMinutes(-1), // expired
        });
        _dbContext.InboxMessages.Add(new InboxMessage
        {
            MessageId = $"active-{Guid.NewGuid()}",
            RequestType = "Test",
            ReceivedAtUtc = now,
            ExpiresAtUtc = now.AddDays(7), // not expired
        });
        await _dbContext.SaveChangesAsync();

        var result = await store.GetExpiredMessagesAsync(10);
        result.IsRight.ShouldBeTrue();
        var messages = result.Match(Right: m => m.ToList(), Left: _ => []);
        messages.Count.ShouldBe(1);
    }

    [Fact]
    public async Task InboxStore_RemoveExpiredMessages_ShouldDelete()
    {
        // Exercises: InboxStoreEF.RemoveExpiredMessagesAsync (lines 148-162)
        var store = new InboxStoreEF(_dbContext, _timeProvider);
        var messageId = $"to-remove-{Guid.NewGuid()}";
        _dbContext.InboxMessages.Add(new InboxMessage
        {
            MessageId = messageId,
            RequestType = "Test",
            ReceivedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            ExpiresAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(-1),
        });
        await _dbContext.SaveChangesAsync();

        (await store.RemoveExpiredMessagesAsync([messageId])).ShouldBeRight();
        (await store.SaveChangesAsync()).ShouldBeRight();

        (await _dbContext.InboxMessages.FindAsync(messageId)).ShouldBeNull();
    }

    // ============================
    // SagaStoreEF contracts
    // ============================

    [Fact]
    public async Task SagaStore_AddAndGet_ShouldRoundTrip()
    {
        // Exercises: SagaStoreEF constructor, AddAsync (lines 49-63), GetAsync (lines 35-46)
        var store = new SagaStoreEF(_dbContext, _timeProvider);
        var sagaId = Guid.NewGuid();
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "OrderSaga",
            Data = "{\"orderId\":1}",
            Status = SagaStatus.Running,
            StartedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            LastUpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
        };

        (await store.AddAsync(saga)).ShouldBeRight();
        (await store.SaveChangesAsync()).ShouldBeRight();

        var getResult = await store.GetAsync(sagaId);
        getResult.IsRight.ShouldBeTrue();
        var opt = getResult.Match(Right: o => o, Left: _ => Option<ISagaState>.None);
        opt.IsSome.ShouldBeTrue();
    }

    [Fact]
    public async Task SagaStore_AddAsync_WrongType_ShouldReturnLeft()
    {
        // Exercises: SagaStoreEF.AddAsync lines 53-57 (type check)
        var store = new SagaStoreEF(_dbContext, _timeProvider);
        var wrongSaga = NSubstitute.Substitute.For<ISagaState>();

        var result = await store.AddAsync(wrongSaga);
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task SagaStore_UpdateAsync_ShouldSetLastUpdatedAtUtc()
    {
        // Exercises: SagaStoreEF.UpdateAsync (lines 66-81) - sets LastUpdatedAtUtc
        var store = new SagaStoreEF(_dbContext, _timeProvider);
        var sagaId = Guid.NewGuid();
        var initialTime = _timeProvider.GetUtcNow().UtcDateTime;
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "OrderSaga",
            Data = "{}",
            Status = SagaStatus.Running,
            StartedAtUtc = initialTime,
            LastUpdatedAtUtc = initialTime,
        };

        _dbContext.SagaStates.Add(saga);
        await _dbContext.SaveChangesAsync();

        // Advance time
        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        (await store.UpdateAsync(saga)).ShouldBeRight();
        (await store.SaveChangesAsync()).ShouldBeRight();

        var updated = await _dbContext.SagaStates.FindAsync(sagaId);
        updated!.LastUpdatedAtUtc.ShouldBeGreaterThan(initialTime);
    }

    [Fact]
    public async Task SagaStore_UpdateAsync_WrongType_ShouldReturnLeft()
    {
        // Exercises: SagaStoreEF.UpdateAsync lines 70-75 (type check)
        var store = new SagaStoreEF(_dbContext, _timeProvider);
        var wrongSaga = NSubstitute.Substitute.For<ISagaState>();

        var result = await store.UpdateAsync(wrongSaga);
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task SagaStore_GetStuckSagas_ShouldReturnOldRunning()
    {
        // Exercises: SagaStoreEF.GetStuckSagasAsync (lines 84-103)
        var store = new SagaStoreEF(_dbContext, _timeProvider);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        _dbContext.SagaStates.Add(new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "StuckSaga",
            Data = "{}",
            Status = SagaStatus.Running,
            StartedAtUtc = now.AddHours(-3),
            LastUpdatedAtUtc = now.AddHours(-2), // stuck: 2 hours old
        });
        _dbContext.SagaStates.Add(new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "RecentSaga",
            Data = "{}",
            Status = SagaStatus.Running,
            StartedAtUtc = now,
            LastUpdatedAtUtc = now, // not stuck: just updated
        });
        await _dbContext.SaveChangesAsync();

        var result = await store.GetStuckSagasAsync(olderThan: TimeSpan.FromHours(1), batchSize: 10);
        result.IsRight.ShouldBeTrue();
        var sagas = result.Match(Right: s => s.ToList(), Left: _ => []);
        sagas.Count.ShouldBe(1);
        sagas[0].SagaType.ShouldBe("StuckSaga");
    }

    // ============================
    // ScheduledMessageStoreEF contracts
    // ============================

    [Fact]
    public async Task ScheduledStore_AddAndGetDue_ShouldReturnDueMessages()
    {
        // Exercises: ScheduledMessageStoreEF constructor, AddAsync (lines 35-49),
        // GetDueMessagesAsync (lines 52-73)
        var store = new ScheduledMessageStoreEF(_dbContext, _timeProvider);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var due = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "SendReminderCommand",
            Content = "{}",
            ScheduledAtUtc = now.AddMinutes(-5), // past due
            CreatedAtUtc = now.AddHours(-1),
        };
        var future = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            RequestType = "SendReminderCommand",
            Content = "{}",
            ScheduledAtUtc = now.AddHours(1), // future
            CreatedAtUtc = now,
        };

        (await store.AddAsync(due)).ShouldBeRight();
        (await store.AddAsync(future)).ShouldBeRight();
        (await store.SaveChangesAsync()).ShouldBeRight();

        var result = await store.GetDueMessagesAsync(batchSize: 10, maxRetries: 3);
        result.IsRight.ShouldBeTrue();
        var messages = result.Match(Right: m => m.ToList(), Left: _ => []);
        messages.Count.ShouldBe(1);
        messages[0].RequestType.ShouldBe("SendReminderCommand");
    }

    [Fact]
    public async Task ScheduledStore_AddAsync_WrongType_ShouldReturnLeft()
    {
        // Exercises: ScheduledMessageStoreEF.AddAsync lines 39-43 (type check)
        var store = new ScheduledMessageStoreEF(_dbContext, _timeProvider);
        var wrongMessage = NSubstitute.Substitute.For<IScheduledMessage>();

        var result = await store.AddAsync(wrongMessage);
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task ScheduledStore_MarkAsProcessed_ShouldSetTimestamps()
    {
        // Exercises: ScheduledMessageStoreEF.MarkAsProcessedAsync (lines 76-91)
        var store = new ScheduledMessageStoreEF(_dbContext, _timeProvider);
        var id = Guid.NewGuid();
        _dbContext.ScheduledMessages.Add(new ScheduledMessage
        {
            Id = id,
            RequestType = "Test",
            Content = "{}",
            ScheduledAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
        });
        await _dbContext.SaveChangesAsync();

        (await store.MarkAsProcessedAsync(id)).ShouldBeRight();
        (await store.SaveChangesAsync()).ShouldBeRight();

        var updated = await _dbContext.ScheduledMessages.FindAsync(id);
        updated!.ProcessedAtUtc.ShouldNotBeNull();
        updated.LastExecutedAtUtc.ShouldNotBeNull();
        updated.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task ScheduledStore_MarkAsFailed_ShouldSetErrorAndIncrementRetry()
    {
        // Exercises: ScheduledMessageStoreEF.MarkAsFailedAsync (lines 94-114)
        var store = new ScheduledMessageStoreEF(_dbContext, _timeProvider);
        var id = Guid.NewGuid();
        _dbContext.ScheduledMessages.Add(new ScheduledMessage
        {
            Id = id,
            RequestType = "Test",
            Content = "{}",
            ScheduledAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
        });
        await _dbContext.SaveChangesAsync();

        (await store.MarkAsFailedAsync(id, "DB down", null)).ShouldBeRight();
        (await store.SaveChangesAsync()).ShouldBeRight();

        var updated = await _dbContext.ScheduledMessages.FindAsync(id);
        updated!.ErrorMessage.ShouldBe("DB down");
        updated.RetryCount.ShouldBe(1);
    }

    [Fact]
    public async Task ScheduledStore_RescheduleRecurring_ShouldResetState()
    {
        // Exercises: ScheduledMessageStoreEF.RescheduleRecurringMessageAsync (lines 117-136)
        var store = new ScheduledMessageStoreEF(_dbContext, _timeProvider);
        var id = Guid.NewGuid();
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        _dbContext.ScheduledMessages.Add(new ScheduledMessage
        {
            Id = id,
            RequestType = "RecurringJob",
            Content = "{}",
            ScheduledAtUtc = now.AddMinutes(-5),
            CreatedAtUtc = now.AddHours(-1),
            ProcessedAtUtc = now,
            RetryCount = 2,
            ErrorMessage = "previous error",
            IsRecurring = true,
        });
        await _dbContext.SaveChangesAsync();

        var nextRun = now.AddHours(1);
        (await store.RescheduleRecurringMessageAsync(id, nextRun)).ShouldBeRight();
        (await store.SaveChangesAsync()).ShouldBeRight();

        var updated = await _dbContext.ScheduledMessages.FindAsync(id);
        updated!.ScheduledAtUtc.ShouldBe(nextRun);
        updated.ProcessedAtUtc.ShouldBeNull();
        updated.ErrorMessage.ShouldBeNull();
        updated.RetryCount.ShouldBe(0);
        updated.NextRetryAtUtc.ShouldBeNull();
    }

    [Fact]
    public async Task ScheduledStore_CancelAsync_ShouldRemoveMessage()
    {
        // Exercises: ScheduledMessageStoreEF.CancelAsync (lines 139-151)
        var store = new ScheduledMessageStoreEF(_dbContext, _timeProvider);
        var id = Guid.NewGuid();
        _dbContext.ScheduledMessages.Add(new ScheduledMessage
        {
            Id = id,
            RequestType = "CancelMe",
            Content = "{}",
            ScheduledAtUtc = _timeProvider.GetUtcNow().UtcDateTime.AddHours(1),
            CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
        });
        await _dbContext.SaveChangesAsync();

        (await store.CancelAsync(id)).ShouldBeRight();
        (await store.SaveChangesAsync()).ShouldBeRight();

        (await _dbContext.ScheduledMessages.FindAsync(id)).ShouldBeNull();
    }
}
