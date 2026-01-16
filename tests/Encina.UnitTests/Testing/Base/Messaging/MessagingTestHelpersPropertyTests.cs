using Encina.Messaging.Sagas;
using Encina.Testing;
using Encina.Testing.Fakes.Models;
using Encina.Testing.Messaging;
using FsCheck;
using FsCheck.Xunit;
using FakeOutboxMessage = Encina.Testing.Fakes.Models.FakeOutboxMessage;

namespace Encina.UnitTests.Testing.Base.Messaging;

/// <summary>
/// Property-based tests for messaging test helpers using FsCheck.
/// </summary>
public sealed class MessagingTestHelpersPropertyTests
{
    #region OutboxTestHelper Property Tests

    [Property(MaxTest = 100)]
    public bool OutboxTestHelper_AddedMessages_AreAlwaysTracked(NonEmptyString content)
    {
        using var helper = new OutboxTestHelper();
        var notification = new TestNotification { Value = content.Get };

        helper
            .GivenEmptyOutbox()
            .WhenMessageAdded(notification);

        return helper.Store.GetAddedMessages().Count == 1;
    }

    [Property(MaxTest = 50)]
    public bool OutboxTestHelper_MultipleMessages_AllTracked(PositiveInt count)
    {
        var messageCount = Math.Min(count.Get, 10); // Limit for performance
        using var helper = new OutboxTestHelper();
        helper.GivenEmptyOutbox();

        for (var i = 0; i < messageCount; i++)
        {
            helper.WhenMessageAdded(new TestNotification { Value = $"msg-{i}" });
        }

        return helper.Store.GetAddedMessages().Count == messageCount;
    }

    [Property(MaxTest = 100)]
    public bool OutboxTestHelper_ProcessedMessages_AreMarkedCorrectly(Guid messageId)
    {
        if (messageId == Guid.Empty) return true; // Skip empty GUIDs

        using var helper = new OutboxTestHelper();
        var message = new FakeOutboxMessage
        {
            Id = messageId,
            NotificationType = "Test",
            Content = "{}",
            CreatedAtUtc = DateTime.UtcNow
        };

        helper.Store.AddAsync(message).GetAwaiter().GetResult();
        helper.WhenMessageProcessed(messageId);

        return helper.Store.GetProcessedMessageIds().Contains(messageId);
    }

    [Property(MaxTest = 100)]
    public bool OutboxTestHelper_TimeAdvance_IsMonotonic(PositiveInt minutes)
    {
        using var helper = new OutboxTestHelper();
        var startTime = helper.TimeProvider.GetUtcNow();
        var minutesToAdvance = Math.Min(minutes.Get, 1000);

        helper.AdvanceTimeByMinutes(minutesToAdvance);

        return helper.TimeProvider.GetUtcNow() == startTime.AddMinutes(minutesToAdvance);
    }

    [Property(MaxTest = 50)]
    public bool OutboxTestHelper_GivenEmptyOutbox_AlwaysClearsState()
    {
        using var helper = new OutboxTestHelper();

        // Add some messages first
        helper.WhenMessageAdded(new TestNotification { Value = "test1" });
        helper.WhenMessageAdded(new TestNotification { Value = "test2" });

        // Clear
        helper.GivenEmptyOutbox();

        return helper.Store.GetMessages().Count == 0;
    }

    #endregion

    #region InboxTestHelper Property Tests

    [Property(MaxTest = 100)]
    public bool InboxTestHelper_ProcessedMessages_HaveCachedResponse(NonEmptyString messageId)
    {
        using var helper = new InboxTestHelper();
        var response = new TestResponse { Success = true, Value = 42 };

        helper.GivenProcessedMessage(messageId.Get, response);

        var message = helper.Store.GetMessage(messageId.Get);
        return message is not null && message.IsProcessed && !string.IsNullOrEmpty(message.Response);
    }

    [Property(MaxTest = 100)]
    public bool InboxTestHelper_MessageIds_AreUnique(NonEmptyString id1, NonEmptyString id2)
    {
        if (id1.Get == id2.Get) return true; // Skip identical IDs

        using var helper = new InboxTestHelper();
        helper.GivenEmptyInbox();
        helper.WhenMessageRegistered(id1.Get, "Test1");
        helper.WhenMessageRegistered(id2.Get, "Test2");

        return helper.Store.GetMessages().Count == 2;
    }

    [Property(MaxTest = 100)]
    public bool InboxTestHelper_IdempotencyCheck_PreservesOriginalResponse(NonEmptyString messageId, int value)
    {
        using var helper = new InboxTestHelper();
        var originalResponse = new TestResponse { Success = true, Value = value };

        helper.GivenProcessedMessage(messageId.Get, originalResponse);
        helper.WhenMessageReceived(messageId.Get);

        var message = helper.Store.GetMessage(messageId.Get);
        return message!.Response!.Contains(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Property(MaxTest = 50)]
    public bool InboxTestHelper_GivenEmptyInbox_AlwaysClearsState()
    {
        using var helper = new InboxTestHelper();

        // Add some messages first
        helper.WhenMessageRegistered("msg1", "Test");
        helper.WhenMessageRegistered("msg2", "Test");

        // Clear
        helper.GivenEmptyInbox();

        return helper.Store.GetMessages().Count == 0;
    }

    [Property(MaxTest = 100)]
    public bool InboxTestHelper_RegisteredMessages_AreTrackedAsAdded(NonEmptyString messageId)
    {
        using var helper = new InboxTestHelper();
        helper.GivenEmptyInbox();
        helper.WhenMessageRegistered(messageId.Get, "TestType");

        return helper.Store.GetAddedMessages().Count == 1;
    }

    #endregion

    #region SagaTestHelper Property Tests

    [Property(MaxTest = 100)]
    public bool SagaTestHelper_NewSagas_StartAtStepZero(Guid sagaId, NonEmptyString orderId)
    {
        if (sagaId == Guid.Empty) return true; // Skip empty GUIDs

        using var helper = new SagaTestHelper();
        helper.GivenNewSaga<TestSaga, TestSagaData>(
            sagaId,
            new TestSagaData { OrderId = orderId.Get });

        var saga = helper.Store.GetSaga(sagaId);
        return saga is not null &&
               saga.CurrentStep == 0 &&
               saga.Status == SagaStatus.Running;
    }

    [Property(MaxTest = 100)]
    public bool SagaTestHelper_StepAdvancement_IsIncremental(PositiveInt advanceCount)
    {
        var steps = Math.Min(advanceCount.Get, 10);
        var sagaId = Guid.NewGuid();

        using var helper = new SagaTestHelper();
        helper.GivenRunningSaga<TestSaga, TestSagaData>(
            sagaId,
            new TestSagaData { OrderId = "ORD-1" },
            currentStep: 0);

        for (var i = 0; i < steps; i++)
        {
            helper.WhenSagaAdvancesToNextStep(sagaId);
        }

        var saga = helper.Store.GetSaga(sagaId);
        return saga!.CurrentStep == steps;
    }

    [Property(MaxTest = 100)]
    public bool SagaTestHelper_CompletedSagas_HaveCompletedTimestamp(Guid sagaId)
    {
        if (sagaId == Guid.Empty) return true; // Skip empty GUIDs

        using var helper = new SagaTestHelper();
        helper.GivenRunningSaga<TestSaga, TestSagaData>(
            sagaId,
            new TestSagaData { OrderId = "ORD-1" });

        helper.WhenSagaCompletes(sagaId);

        var saga = helper.Store.GetSaga(sagaId);
        return saga!.Status == SagaStatus.Completed && saga.CompletedAtUtc.HasValue;
    }

    [Property(MaxTest = 100)]
    public bool SagaTestHelper_FailedSagas_HaveErrorMessage(Guid sagaId, NonEmptyString errorMessage)
    {
        if (sagaId == Guid.Empty) return true; // Skip empty GUIDs

        using var helper = new SagaTestHelper();
        helper.GivenRunningSaga<TestSaga, TestSagaData>(
            sagaId,
            new TestSagaData { OrderId = "ORD-1" });

        helper.WhenSagaFails(sagaId, errorMessage.Get);

        var saga = helper.Store.GetSaga(sagaId);
        return saga!.Status == SagaStatus.Failed &&
               saga.ErrorMessage == errorMessage.Get;
    }

    [Property(MaxTest = 100)]
    public bool SagaTestHelper_CompensatingSagas_HaveCorrectStatus(Guid sagaId)
    {
        if (sagaId == Guid.Empty) return true; // Skip empty GUIDs

        using var helper = new SagaTestHelper();
        helper.GivenRunningSaga<TestSaga, TestSagaData>(
            sagaId,
            new TestSagaData { OrderId = "ORD-1" });

        helper.WhenSagaStartsCompensating(sagaId);

        var saga = helper.Store.GetSaga(sagaId);
        return saga!.Status == SagaStatus.Compensating;
    }

    [Property(MaxTest = 50)]
    public bool SagaTestHelper_AddedSagas_AreTrackedInStore()
    {
        var sagaId = Guid.NewGuid();

        using var helper = new SagaTestHelper();
        helper.GivenNewSaga<TestSaga, TestSagaData>(
            sagaId,
            new TestSagaData { OrderId = "ORD-1" });

        return helper.Store.GetAddedSagas().Count == 1;
    }

    #endregion

    #region SchedulingTestHelper Property Tests

    [Property(MaxTest = 100)]
    public bool SchedulingTestHelper_ScheduledMessages_HaveFutureTime(PositiveInt delayMinutes)
    {
        var minutes = Math.Min(delayMinutes.Get, 10000);

        using var helper = new SchedulingTestHelper();
        var startTime = helper.TimeProvider.GetUtcNow().UtcDateTime;

        helper
            .GivenNoScheduledMessages()
            .WhenMessageScheduled(new TestRequest { Id = "1" }, TimeSpan.FromMinutes(minutes));

        var message = helper.Store.GetMessages().First();
        return message.ScheduledAtUtc >= startTime.AddMinutes(minutes);
    }

    [Property(MaxTest = 100)]
    public bool SchedulingTestHelper_CancelledMessages_AreMarkedAsCancelled()
    {
        using var helper = new SchedulingTestHelper();
        helper.GivenDueMessage(new TestRequest { Id = "1" });
        var messageId = helper.LastMessageId;

        helper.WhenMessageCancelled(messageId);

        var message = helper.Store.GetMessage(messageId);
        return message!.IsCancelled;
    }

    [Property(MaxTest = 100)]
    public bool SchedulingTestHelper_TimeAdvance_AffectsMessageDueness(PositiveInt scheduleMinutes, PositiveInt advanceMinutes)
    {
        var schedule = Math.Min(scheduleMinutes.Get, 100);
        var advance = Math.Min(advanceMinutes.Get, 200);

        using var helper = new SchedulingTestHelper();
        helper
            .GivenNoScheduledMessages()
            .WhenMessageScheduled(new TestRequest { Id = "1" }, TimeSpan.FromMinutes(schedule));

        var messageId = helper.LastMessageId;
        helper.AdvanceTimeByMinutes(advance);

        var message = helper.Store.GetMessage(messageId);
        var isDue = message!.ScheduledAtUtc <= helper.TimeProvider.GetUtcNow().UtcDateTime;

        return (advance >= schedule) == isDue;
    }

    [Property(MaxTest = 50)]
    public bool SchedulingTestHelper_RecurringMessages_HaveIsRecurringFlag()
    {
        using var helper = new SchedulingTestHelper();
        helper
            .GivenNoScheduledMessages()
            .WhenRecurringMessageScheduled(
                new TestRequest { Id = "1" },
                "0 0 * * *", // Daily at midnight
                TimeSpan.FromHours(1));

        var message = helper.Store.GetMessages().First();
        return message.IsRecurring && message.CronExpression == "0 0 * * *";
    }

    [Fact]
    public void SchedulingTestHelper_ProcessedMessages_AreTracked()
    {
        using var helper = new SchedulingTestHelper();
        helper.GivenDueMessage(new TestRequest { Id = "1" });
        var actualMessageId = helper.LastMessageId;

        helper.WhenMessageProcessed(actualMessageId);

        helper.Store.GetProcessedMessageIds().ShouldContain(actualMessageId);
    }

    [Property(MaxTest = 50)]
    public bool SchedulingTestHelper_GivenNoScheduledMessages_ClearsStore()
    {
        using var helper = new SchedulingTestHelper();

        // Add some messages first
        helper.WhenMessageScheduled(new TestRequest { Id = "1" }, TimeSpan.FromHours(1));
        helper.WhenMessageScheduled(new TestRequest { Id = "2" }, TimeSpan.FromHours(2));

        // Clear
        helper.GivenNoScheduledMessages();

        return helper.Store.GetMessages().Count == 0;
    }

    [Property(MaxTest = 100)]
    public bool SchedulingTestHelper_AdvanceTimeUntilDue_MakesMessageDue()
    {
        using var helper = new SchedulingTestHelper();
        helper
            .GivenNoScheduledMessages()
            .WhenMessageScheduled(new TestRequest { Id = "1" }, TimeSpan.FromHours(5));

        var messageId = helper.LastMessageId;
        helper.AdvanceTimeUntilDue(messageId);

        var message = helper.Store.GetMessage(messageId);
        return message!.ScheduledAtUtc <= helper.TimeProvider.GetUtcNow().UtcDateTime;
    }

    #endregion

    #region Test Types

    private sealed class TestNotification
    {
        public string Value { get; init; } = string.Empty;
    }

    private sealed class TestResponse
    {
        public bool Success { get; init; }
        public int Value { get; init; }
    }

    private sealed class TestSaga { }

    private sealed class TestSagaData
    {
        public string OrderId { get; init; } = string.Empty;
    }

    private sealed class TestRequest
    {
        public string Id { get; init; } = string.Empty;
    }

    #endregion
}
