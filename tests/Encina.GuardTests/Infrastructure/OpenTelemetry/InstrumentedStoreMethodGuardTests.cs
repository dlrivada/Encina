using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Encina.OpenTelemetry.MessagingStores;

namespace Encina.GuardTests.Infrastructure.OpenTelemetry;

/// <summary>
/// Guard tests that exercise ALL public methods of instrumented messaging store wrappers.
/// Each method delegates to the inner store wrapped in Activity tracing.
/// These tests verify that delegation occurs and results flow through correctly.
/// </summary>
public sealed class InstrumentedStoreMethodGuardTests
{
    #region InstrumentedOutboxStore Methods

    [Fact]
    public async Task OutboxStore_AddAsync_DelegatesToInner()
    {
        var inner = Substitute.For<IOutboxStore>();
        var message = Substitute.For<IOutboxMessage>();
        message.NotificationType.Returns("TestEvent");
        message.Id.Returns(Guid.NewGuid());
        inner.AddAsync(message, Arg.Any<CancellationToken>())
            .Returns(Prelude.Right<EncinaError, Unit>(Unit.Default));

        var sut = new InstrumentedOutboxStore(inner);

        var result = await sut.AddAsync(message);

        result.IsRight.ShouldBeTrue();
        await inner.Received(1).AddAsync(message, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OutboxStore_AddAsync_PropagatesError()
    {
        var inner = Substitute.For<IOutboxStore>();
        var message = Substitute.For<IOutboxMessage>();
        message.NotificationType.Returns("TestEvent");
        message.Id.Returns(Guid.NewGuid());
        var error = EncinaError.New("test-error");
        inner.AddAsync(message, Arg.Any<CancellationToken>())
            .Returns(Prelude.Left<EncinaError, Unit>(error));

        var sut = new InstrumentedOutboxStore(inner);

        var result = await sut.AddAsync(message);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task OutboxStore_GetPendingMessagesAsync_DelegatesToInner()
    {
        var inner = Substitute.For<IOutboxStore>();
        var messages = new List<IOutboxMessage>();
        inner.GetPendingMessagesAsync(10, 3, Arg.Any<CancellationToken>())
            .Returns(Prelude.Right<EncinaError, IEnumerable<IOutboxMessage>>(messages));

        var sut = new InstrumentedOutboxStore(inner);

        var result = await sut.GetPendingMessagesAsync(10, 3);

        result.IsRight.ShouldBeTrue();
        await inner.Received(1).GetPendingMessagesAsync(10, 3, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OutboxStore_GetPendingMessagesAsync_PropagatesError()
    {
        var inner = Substitute.For<IOutboxStore>();
        var error = EncinaError.New("db-failure");
        inner.GetPendingMessagesAsync(10, 3, Arg.Any<CancellationToken>())
            .Returns(Prelude.Left<EncinaError, IEnumerable<IOutboxMessage>>(error));

        var sut = new InstrumentedOutboxStore(inner);

        var result = await sut.GetPendingMessagesAsync(10, 3);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task OutboxStore_MarkAsProcessedAsync_DelegatesToInner()
    {
        var inner = Substitute.For<IOutboxStore>();
        var id = Guid.NewGuid();
        inner.MarkAsProcessedAsync(id, Arg.Any<CancellationToken>())
            .Returns(Prelude.Right<EncinaError, Unit>(Unit.Default));

        var sut = new InstrumentedOutboxStore(inner);

        var result = await sut.MarkAsProcessedAsync(id);

        result.IsRight.ShouldBeTrue();
        await inner.Received(1).MarkAsProcessedAsync(id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OutboxStore_MarkAsFailedAsync_DelegatesToInner()
    {
        var inner = Substitute.For<IOutboxStore>();
        var id = Guid.NewGuid();
        var nextRetry = DateTime.UtcNow.AddMinutes(5);
        inner.MarkAsFailedAsync(id, "err", nextRetry, Arg.Any<CancellationToken>())
            .Returns(Prelude.Right<EncinaError, Unit>(Unit.Default));

        var sut = new InstrumentedOutboxStore(inner);

        var result = await sut.MarkAsFailedAsync(id, "err", nextRetry);

        result.IsRight.ShouldBeTrue();
        await inner.Received(1).MarkAsFailedAsync(id, "err", nextRetry, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OutboxStore_SaveChangesAsync_DelegatesToInner()
    {
        var inner = Substitute.For<IOutboxStore>();
        inner.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Prelude.Right<EncinaError, Unit>(Unit.Default));

        var sut = new InstrumentedOutboxStore(inner);

        var result = await sut.SaveChangesAsync();

        result.IsRight.ShouldBeTrue();
        await inner.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region InstrumentedInboxStore Methods

    [Fact]
    public async Task InboxStore_GetMessageAsync_DelegatesToInner()
    {
        var inner = Substitute.For<IInboxStore>();
        inner.GetMessageAsync("msg-1", Arg.Any<CancellationToken>())
            .Returns(Prelude.Right<EncinaError, Option<IInboxMessage>>(Option<IInboxMessage>.None));

        var sut = new InstrumentedInboxStore(inner);

        var result = await sut.GetMessageAsync("msg-1");

        result.IsRight.ShouldBeTrue();
        await inner.Received(1).GetMessageAsync("msg-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InboxStore_GetMessageAsync_WhenDuplicateFound_DelegatesToInner()
    {
        var inner = Substitute.For<IInboxStore>();
        var existing = Substitute.For<IInboxMessage>();
        inner.GetMessageAsync("msg-1", Arg.Any<CancellationToken>())
            .Returns(Prelude.Right<EncinaError, Option<IInboxMessage>>(Option<IInboxMessage>.Some(existing)));

        var sut = new InstrumentedInboxStore(inner);

        var result = await sut.GetMessageAsync("msg-1");

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task InboxStore_AddAsync_DelegatesToInner()
    {
        var inner = Substitute.For<IInboxStore>();
        var message = Substitute.For<IInboxMessage>();
        message.RequestType.Returns("TestCmd");
        message.MessageId.Returns("msg-1");
        inner.AddAsync(message, Arg.Any<CancellationToken>())
            .Returns(Prelude.Right<EncinaError, Unit>(Unit.Default));

        var sut = new InstrumentedInboxStore(inner);

        var result = await sut.AddAsync(message);

        result.IsRight.ShouldBeTrue();
        await inner.Received(1).AddAsync(message, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InboxStore_MarkAsProcessedAsync_DelegatesToInner()
    {
        var inner = Substitute.For<IInboxStore>();
        inner.MarkAsProcessedAsync("msg-1", "ok", Arg.Any<CancellationToken>())
            .Returns(Prelude.Right<EncinaError, Unit>(Unit.Default));

        var sut = new InstrumentedInboxStore(inner);

        var result = await sut.MarkAsProcessedAsync("msg-1", "ok");

        result.IsRight.ShouldBeTrue();
        await inner.Received(1).MarkAsProcessedAsync("msg-1", "ok", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InboxStore_MarkAsFailedAsync_DelegatesToInner()
    {
        var inner = Substitute.For<IInboxStore>();
        var nextRetry = DateTime.UtcNow.AddMinutes(5);
        inner.MarkAsFailedAsync("msg-1", "err", nextRetry, Arg.Any<CancellationToken>())
            .Returns(Prelude.Right<EncinaError, Unit>(Unit.Default));

        var sut = new InstrumentedInboxStore(inner);

        var result = await sut.MarkAsFailedAsync("msg-1", "err", nextRetry);

        result.IsRight.ShouldBeTrue();
        await inner.Received(1).MarkAsFailedAsync("msg-1", "err", nextRetry, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InboxStore_IncrementRetryCountAsync_DelegatesToInner()
    {
        var inner = Substitute.For<IInboxStore>();
        inner.IncrementRetryCountAsync("msg-1", Arg.Any<CancellationToken>())
            .Returns(Prelude.Right<EncinaError, Unit>(Unit.Default));

        var sut = new InstrumentedInboxStore(inner);

        var result = await sut.IncrementRetryCountAsync("msg-1");

        result.IsRight.ShouldBeTrue();
        await inner.Received(1).IncrementRetryCountAsync("msg-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InboxStore_GetExpiredMessagesAsync_DelegatesToInner()
    {
        var inner = Substitute.For<IInboxStore>();
        var messages = new List<IInboxMessage>();
        inner.GetExpiredMessagesAsync(50, Arg.Any<CancellationToken>())
            .Returns(Prelude.Right<EncinaError, IEnumerable<IInboxMessage>>(messages));

        var sut = new InstrumentedInboxStore(inner);

        var result = await sut.GetExpiredMessagesAsync(50);

        result.IsRight.ShouldBeTrue();
        await inner.Received(1).GetExpiredMessagesAsync(50, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InboxStore_RemoveExpiredMessagesAsync_DelegatesToInner()
    {
        var inner = Substitute.For<IInboxStore>();
        var ids = new[] { "msg-1", "msg-2" };
        inner.RemoveExpiredMessagesAsync(ids, Arg.Any<CancellationToken>())
            .Returns(Prelude.Right<EncinaError, Unit>(Unit.Default));

        var sut = new InstrumentedInboxStore(inner);

        var result = await sut.RemoveExpiredMessagesAsync(ids);

        result.IsRight.ShouldBeTrue();
        await inner.Received(1).RemoveExpiredMessagesAsync(ids, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InboxStore_SaveChangesAsync_DelegatesToInner()
    {
        var inner = Substitute.For<IInboxStore>();
        inner.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Prelude.Right<EncinaError, Unit>(Unit.Default));

        var sut = new InstrumentedInboxStore(inner);

        var result = await sut.SaveChangesAsync();

        result.IsRight.ShouldBeTrue();
        await inner.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region InstrumentedSagaStore Methods

    [Fact]
    public async Task SagaStore_GetAsync_DelegatesToInner()
    {
        var inner = Substitute.For<ISagaStore>();
        var sagaId = Guid.NewGuid();
        inner.GetAsync(sagaId, Arg.Any<CancellationToken>())
            .Returns(Prelude.Right<EncinaError, Option<ISagaState>>(Option<ISagaState>.None));

        var sut = new InstrumentedSagaStore(inner);

        var result = await sut.GetAsync(sagaId);

        result.IsRight.ShouldBeTrue();
        await inner.Received(1).GetAsync(sagaId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SagaStore_GetAsync_WhenFound_DelegatesToInner()
    {
        var inner = Substitute.For<ISagaStore>();
        var sagaId = Guid.NewGuid();
        var sagaState = Substitute.For<ISagaState>();
        sagaState.SagaType.Returns("OrderSaga");
        sagaState.Status.Returns("Active");
        inner.GetAsync(sagaId, Arg.Any<CancellationToken>())
            .Returns(Prelude.Right<EncinaError, Option<ISagaState>>(Option<ISagaState>.Some(sagaState)));

        var sut = new InstrumentedSagaStore(inner);

        var result = await sut.GetAsync(sagaId);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task SagaStore_AddAsync_DelegatesToInner()
    {
        var inner = Substitute.For<ISagaStore>();
        var sagaState = Substitute.For<ISagaState>();
        sagaState.SagaType.Returns("OrderSaga");
        sagaState.SagaId.Returns(Guid.NewGuid());
        inner.AddAsync(sagaState, Arg.Any<CancellationToken>())
            .Returns(Prelude.Right<EncinaError, Unit>(Unit.Default));

        var sut = new InstrumentedSagaStore(inner);

        var result = await sut.AddAsync(sagaState);

        result.IsRight.ShouldBeTrue();
        await inner.Received(1).AddAsync(sagaState, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SagaStore_UpdateAsync_DelegatesToInner()
    {
        var inner = Substitute.For<ISagaStore>();
        var sagaState = Substitute.For<ISagaState>();
        sagaState.SagaType.Returns("OrderSaga");
        sagaState.SagaId.Returns(Guid.NewGuid());
        sagaState.Status.Returns("Completed");
        inner.UpdateAsync(sagaState, Arg.Any<CancellationToken>())
            .Returns(Prelude.Right<EncinaError, Unit>(Unit.Default));

        var sut = new InstrumentedSagaStore(inner);

        var result = await sut.UpdateAsync(sagaState);

        result.IsRight.ShouldBeTrue();
        await inner.Received(1).UpdateAsync(sagaState, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SagaStore_GetStuckSagasAsync_DelegatesToInner()
    {
        var inner = Substitute.For<ISagaStore>();
        var sagas = new List<ISagaState>();
        inner.GetStuckSagasAsync(TimeSpan.FromHours(1), 10, Arg.Any<CancellationToken>())
            .Returns(Prelude.Right<EncinaError, IEnumerable<ISagaState>>(sagas));

        var sut = new InstrumentedSagaStore(inner);

        var result = await sut.GetStuckSagasAsync(TimeSpan.FromHours(1), 10);

        result.IsRight.ShouldBeTrue();
        await inner.Received(1).GetStuckSagasAsync(TimeSpan.FromHours(1), 10, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SagaStore_GetExpiredSagasAsync_DelegatesToInner()
    {
        var inner = Substitute.For<ISagaStore>();
        var sagas = new List<ISagaState>();
        inner.GetExpiredSagasAsync(10, Arg.Any<CancellationToken>())
            .Returns(Prelude.Right<EncinaError, IEnumerable<ISagaState>>(sagas));

        var sut = new InstrumentedSagaStore(inner);

        var result = await sut.GetExpiredSagasAsync(10);

        result.IsRight.ShouldBeTrue();
        await inner.Received(1).GetExpiredSagasAsync(10, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SagaStore_SaveChangesAsync_DelegatesToInner()
    {
        var inner = Substitute.For<ISagaStore>();
        inner.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Prelude.Right<EncinaError, Unit>(Unit.Default));

        var sut = new InstrumentedSagaStore(inner);

        var result = await sut.SaveChangesAsync();

        result.IsRight.ShouldBeTrue();
        await inner.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region InstrumentedScheduledMessageStore Methods

    [Fact]
    public async Task ScheduledStore_AddAsync_DelegatesToInner()
    {
        var inner = Substitute.For<IScheduledMessageStore>();
        var message = Substitute.For<IScheduledMessage>();
        message.RequestType.Returns("TestCmd");
        message.Id.Returns(Guid.NewGuid());
        message.ScheduledAtUtc.Returns(DateTime.UtcNow.AddHours(1));
        inner.AddAsync(message, Arg.Any<CancellationToken>())
            .Returns(Prelude.Right<EncinaError, Unit>(Unit.Default));

        var sut = new InstrumentedScheduledMessageStore(inner);

        var result = await sut.AddAsync(message);

        result.IsRight.ShouldBeTrue();
        await inner.Received(1).AddAsync(message, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScheduledStore_GetDueMessagesAsync_DelegatesToInner()
    {
        var inner = Substitute.For<IScheduledMessageStore>();
        var messages = new List<IScheduledMessage>();
        inner.GetDueMessagesAsync(10, 3, Arg.Any<CancellationToken>())
            .Returns(Prelude.Right<EncinaError, IEnumerable<IScheduledMessage>>(messages));

        var sut = new InstrumentedScheduledMessageStore(inner);

        var result = await sut.GetDueMessagesAsync(10, 3);

        result.IsRight.ShouldBeTrue();
        await inner.Received(1).GetDueMessagesAsync(10, 3, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScheduledStore_MarkAsProcessedAsync_DelegatesToInner()
    {
        var inner = Substitute.For<IScheduledMessageStore>();
        var id = Guid.NewGuid();
        inner.MarkAsProcessedAsync(id, Arg.Any<CancellationToken>())
            .Returns(Prelude.Right<EncinaError, Unit>(Unit.Default));

        var sut = new InstrumentedScheduledMessageStore(inner);

        var result = await sut.MarkAsProcessedAsync(id);

        result.IsRight.ShouldBeTrue();
        await inner.Received(1).MarkAsProcessedAsync(id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScheduledStore_MarkAsFailedAsync_DelegatesToInner()
    {
        var inner = Substitute.For<IScheduledMessageStore>();
        var id = Guid.NewGuid();
        var nextRetry = DateTime.UtcNow.AddMinutes(5);
        inner.MarkAsFailedAsync(id, "err", nextRetry, Arg.Any<CancellationToken>())
            .Returns(Prelude.Right<EncinaError, Unit>(Unit.Default));

        var sut = new InstrumentedScheduledMessageStore(inner);

        var result = await sut.MarkAsFailedAsync(id, "err", nextRetry);

        result.IsRight.ShouldBeTrue();
        await inner.Received(1).MarkAsFailedAsync(id, "err", nextRetry, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScheduledStore_RescheduleRecurringMessageAsync_DelegatesToInner()
    {
        var inner = Substitute.For<IScheduledMessageStore>();
        var id = Guid.NewGuid();
        var nextScheduled = DateTime.UtcNow.AddHours(1);
        inner.RescheduleRecurringMessageAsync(id, nextScheduled, Arg.Any<CancellationToken>())
            .Returns(Prelude.Right<EncinaError, Unit>(Unit.Default));

        var sut = new InstrumentedScheduledMessageStore(inner);

        var result = await sut.RescheduleRecurringMessageAsync(id, nextScheduled);

        result.IsRight.ShouldBeTrue();
        await inner.Received(1).RescheduleRecurringMessageAsync(id, nextScheduled, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScheduledStore_CancelAsync_DelegatesToInner()
    {
        var inner = Substitute.For<IScheduledMessageStore>();
        var id = Guid.NewGuid();
        inner.CancelAsync(id, Arg.Any<CancellationToken>())
            .Returns(Prelude.Right<EncinaError, Unit>(Unit.Default));

        var sut = new InstrumentedScheduledMessageStore(inner);

        var result = await sut.CancelAsync(id);

        result.IsRight.ShouldBeTrue();
        await inner.Received(1).CancelAsync(id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScheduledStore_SaveChangesAsync_DelegatesToInner()
    {
        var inner = Substitute.For<IScheduledMessageStore>();
        inner.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Prelude.Right<EncinaError, Unit>(Unit.Default));

        var sut = new InstrumentedScheduledMessageStore(inner);

        var result = await sut.SaveChangesAsync();

        result.IsRight.ShouldBeTrue();
        await inner.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion
}
