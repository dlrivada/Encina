using Encina.Testing;
using Encina.Testing.Messaging;

namespace Encina.UnitTests.Testing.Base.Messaging;

/// <summary>
/// Unit tests for <see cref="SchedulingTestHelper"/>.
/// </summary>
public sealed class SchedulingTestHelperTests : IDisposable
{
    private readonly SchedulingTestHelper _helper;

    public SchedulingTestHelperTests()
    {
        _helper = new SchedulingTestHelper(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }

    public void Dispose()
    {
        _helper.Dispose();
    }

    #region Given Tests

    [Fact]
    public void GivenNoScheduledMessages_ShouldClearStore()
    {
        // Act
        _helper.GivenNoScheduledMessages();

        // Assert
        _helper.Store.GetMessages().ShouldBeEmpty();
    }

    [Fact]
    public void GivenScheduledMessage_ShouldAddMessageScheduledInFuture()
    {
        // Act
        _helper.GivenScheduledMessage(
            new SendReminderCommand { UserId = "user-123" },
            scheduledIn: TimeSpan.FromHours(24));

        // Assert
        var message = _helper.Store.GetMessages().First();
        message.ScheduledAtUtc.ShouldBeGreaterThan(_helper.TimeProvider.GetUtcNow().UtcDateTime);
        message.IsRecurring.ShouldBeFalse();
    }

    [Fact]
    public void GivenRecurringMessage_ShouldAddRecurringMessage()
    {
        // Act
        _helper.GivenRecurringMessage(
            new DailyReportCommand { ReportId = "RPT-001", RecipientId = "user-123", IncludeMetrics = true },
            cronExpression: "0 0 * * *", // Daily at midnight
            nextRunIn: TimeSpan.FromHours(12));

        // Assert
        var message = _helper.Store.GetMessages().First();
        message.IsRecurring.ShouldBeTrue();
        message.CronExpression.ShouldBe("0 0 * * *");
    }

    [Fact]
    public void GivenDueMessage_ShouldAddMessageThatIsDue()
    {
        // Act
        _helper.GivenDueMessage(new SendReminderCommand { UserId = "user-123" });

        // Assert
        var message = _helper.Store.GetMessages().First();
        message.ScheduledAtUtc.ShouldBeLessThanOrEqualTo(_helper.TimeProvider.GetUtcNow().UtcDateTime);
    }

    [Fact]
    public void GivenProcessedMessage_ShouldAddProcessedMessage()
    {
        // Act
        _helper.GivenProcessedMessage(new SendReminderCommand { UserId = "user-123" });

        // Assert
        var message = _helper.Store.GetMessages().First();
        message.IsProcessed.ShouldBeTrue();
        message.ProcessedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void GivenFailedMessage_ShouldAddFailedMessage()
    {
        // Act
        _helper.GivenFailedMessage(
            new SendReminderCommand { UserId = "user-123" },
            retryCount: 2,
            errorMessage: "Service unavailable");

        // Assert
        var message = _helper.Store.GetMessages().First();
        message.RetryCount.ShouldBe(2);
        message.ErrorMessage.ShouldBe("Service unavailable");
    }

    [Fact]
    public void GivenCancelledMessage_ShouldAddCancelledMessage()
    {
        // Act
        _helper.GivenCancelledMessage(new SendReminderCommand { UserId = "user-123" });

        // Assert
        var message = _helper.Store.GetMessages().First();
        message.IsCancelled.ShouldBeTrue();
    }

    #endregion

    #region When Tests

    [Fact]
    public void WhenMessageScheduled_ShouldAddOneTimeMessage()
    {
        // Act
        _helper
            .GivenNoScheduledMessages()
            .WhenMessageScheduled(new SendReminderCommand { UserId = "user-123" }, TimeSpan.FromHours(1));

        // Assert
        _helper.Store.GetAddedMessages().Count.ShouldBe(1);
        _helper.LastMessageId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void WhenRecurringMessageScheduled_ShouldAddRecurringMessage()
    {
        // Act
        _helper
            .GivenNoScheduledMessages()
            .WhenRecurringMessageScheduled(
                new DailyReportCommand { ReportId = "RPT-002", RecipientId = "admin", IncludeMetrics = false },
                "0 0 * * *",
                TimeSpan.FromHours(12));

        // Assert
        var message = _helper.Store.GetMessages().First();
        message.IsRecurring.ShouldBeTrue();
    }

    [Fact]
    public void WhenMessageProcessed_ShouldMarkAsProcessed()
    {
        // Arrange
        _helper.GivenDueMessage(new SendReminderCommand { UserId = "user-123" });
        var messageId = _helper.LastMessageId;

        // Act
        _helper.WhenMessageProcessed(messageId);

        // Assert
        _helper.Store.GetProcessedMessageIds().ShouldContain(messageId);
    }

    [Fact]
    public void WhenLastMessageProcessed_ShouldMarkLastMessageAsProcessed()
    {
        // Arrange
        _helper.GivenDueMessage(new SendReminderCommand { UserId = "user-123" });

        // Act
        _helper.WhenLastMessageProcessed();

        // Assert
        _helper.Store.GetProcessedMessageIds().ShouldContain(_helper.LastMessageId);
    }

    [Fact]
    public void WhenMessageFailed_ShouldMarkAsFailed()
    {
        // Arrange
        _helper.GivenDueMessage(new SendReminderCommand { UserId = "user-123" });
        var messageId = _helper.LastMessageId;

        // Act
        _helper.WhenMessageFailed(messageId, "Connection timeout");

        // Assert
        _helper.Store.GetFailedMessageIds().ShouldContain(messageId);
    }

    [Fact]
    public void WhenMessageCancelled_ShouldCancelMessage()
    {
        // Arrange
        _helper.GivenScheduledMessage(
            new SendReminderCommand { UserId = "user-123" },
            TimeSpan.FromHours(1));
        var messageId = _helper.LastMessageId;

        // Act
        _helper.WhenMessageCancelled(messageId);

        // Assert
        _helper.Store.GetCancelledMessageIds().ShouldContain(messageId);
    }

    [Fact]
    public void WhenMessageRescheduled_ShouldRescheduleMessage()
    {
        // Arrange
        _helper.GivenRecurringMessage(
            new DailyReportCommand { ReportId = "RPT-003", RecipientId = "team", IncludeMetrics = true },
            "0 0 * * *",
            TimeSpan.FromHours(12));
        var messageId = _helper.LastMessageId;
        var nextTime = _helper.TimeProvider.GetUtcNow().UtcDateTime.AddDays(1);

        // Act
        _helper.WhenMessageRescheduled(messageId, nextTime);

        // Assert
        _helper.Store.GetRescheduledMessageIds().ShouldContain(messageId);
    }

    #endregion

    #region Then Tests

    [Fact]
    public void ThenMessageWasScheduled_WhenScheduled_ShouldPass()
    {
        // Act & Assert
        Should.NotThrow(() =>
            _helper
                .GivenNoScheduledMessages()
                .WhenMessageScheduled(new SendReminderCommand { UserId = "user-123" }, TimeSpan.FromHours(1))
                .ThenMessageWasScheduled<SendReminderCommand>());
    }

    [Fact]
    public void ThenMessageWasScheduled_WhenNotScheduled_ShouldThrow()
    {
        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            _helper
                .GivenNoScheduledMessages()
                .WhenMessageScheduled(new SendReminderCommand { UserId = "user-123" }, TimeSpan.FromHours(1))
                .ThenMessageWasScheduled<DailyReportCommand>());
    }

    [Fact]
    public void ThenMessageIsDue_WhenDue_ShouldPass()
    {
        // Act & Assert
        Should.NotThrow(() =>
            _helper
                .GivenNoScheduledMessages()
                .WhenMessageScheduled(new SendReminderCommand { UserId = "user-123" }, TimeSpan.FromHours(1))
                .AdvanceTimeByHours(2)
                .WhenNothing()
                .ThenMessageIsDue<SendReminderCommand>());
    }

    [Fact]
    public void ThenMessageIsNotDue_WhenNotDue_ShouldPass()
    {
        // Arrange
        _helper.GivenScheduledMessage(
            new SendReminderCommand { UserId = "user-123" },
            TimeSpan.FromHours(24));

        // Act & Assert - message is scheduled for tomorrow
        Should.NotThrow(() =>
            _helper
                .WhenNothing()
                .ThenMessageIsNotDue(_helper.LastMessageId));
    }

    [Fact]
    public void ThenMessageWasProcessed_ShouldVerifyProcessing()
    {
        // Arrange
        _helper.GivenDueMessage(new SendReminderCommand { UserId = "user-123" });
        var messageId = _helper.LastMessageId;

        // Act & Assert
        Should.NotThrow(() =>
            _helper
                .WhenMessageProcessed(messageId)
                .ThenMessageWasProcessed(messageId));
    }

    [Fact]
    public void ThenMessageWasFailed_ShouldVerifyFailure()
    {
        // Arrange
        _helper.GivenDueMessage(new SendReminderCommand { UserId = "user-123" });
        var messageId = _helper.LastMessageId;

        // Act & Assert
        Should.NotThrow(() =>
            _helper
                .WhenMessageFailed(messageId)
                .ThenMessageWasFailed(messageId));
    }

    [Fact]
    public void ThenMessageWasCancelled_ShouldVerifyCancellation()
    {
        // Arrange
        _helper.GivenScheduledMessage(
            new SendReminderCommand { UserId = "user-123" },
            TimeSpan.FromHours(1));
        var messageId = _helper.LastMessageId;

        // Act & Assert
        Should.NotThrow(() =>
            _helper
                .WhenMessageCancelled(messageId)
                .ThenMessageWasCancelled(messageId));
    }

    [Fact]
    public void ThenMessageWasRescheduled_ShouldVerifyRescheduling()
    {
        // Arrange
        _helper.GivenRecurringMessage(
            new DailyReportCommand { ReportId = "RPT-004", RecipientId = "ops", IncludeMetrics = false },
            "0 0 * * *",
            TimeSpan.FromHours(12));
        var messageId = _helper.LastMessageId;

        // Act & Assert
        Should.NotThrow(() =>
            _helper
                .WhenMessageRescheduled(messageId, _helper.TimeProvider.GetUtcNow().UtcDateTime.AddDays(1))
                .ThenMessageWasRescheduled(messageId));
    }

    [Fact]
    public void ThenNoScheduledMessages_WhenEmpty_ShouldPass()
    {
        // Act & Assert
        Should.NotThrow(() =>
            _helper
                .GivenNoScheduledMessages()
                .WhenNothing()
                .ThenNoScheduledMessages());
    }

    [Fact]
    public void ThenScheduledMessageCount_WhenCorrect_ShouldPass()
    {
        // Act & Assert
        Should.NotThrow(() =>
            _helper
                .GivenNoScheduledMessages()
                .WhenMessageScheduled(new SendReminderCommand { UserId = "1" }, TimeSpan.FromHours(1))
                .ThenScheduledMessageCount(1));
    }

    [Fact]
    public void ThenMessageIsRecurring_WhenRecurring_ShouldPass()
    {
        // Arrange
        _helper.GivenRecurringMessage(
            new DailyReportCommand { ReportId = "RPT-005", RecipientId = "exec", IncludeMetrics = true },
            "0 0 * * *",
            TimeSpan.FromHours(12));

        // Act & Assert
        Should.NotThrow(() =>
            _helper
                .WhenNothing()
                .ThenMessageIsRecurring(_helper.LastMessageId));
    }

    [Fact]
    public void ThenMessageHasCron_WhenCorrect_ShouldPass()
    {
        // Arrange
        _helper.GivenRecurringMessage(
            new DailyReportCommand { ReportId = "RPT-006", RecipientId = "finance", IncludeMetrics = false },
            "0 0 * * *",
            TimeSpan.FromHours(12));

        // Act & Assert
        Should.NotThrow(() =>
            _helper
                .WhenNothing()
                .ThenMessageHasCron(_helper.LastMessageId, "0 0 * * *"));
    }

    [Fact]
    public void GetScheduledMessage_ShouldReturnDeserializedRequest()
    {
        // Act
        _helper
            .GivenNoScheduledMessages()
            .WhenMessageScheduled(
                new SendReminderCommand { UserId = "user-456", Message = "Don't forget!" },
                TimeSpan.FromHours(1));

        var request = _helper.GetScheduledMessage<SendReminderCommand>();

        // Assert
        request.UserId.ShouldBe("user-456");
        request.Message.ShouldBe("Don't forget!");
    }

    #endregion

    #region Time Control Tests

    [Fact]
    public void AdvanceTimeBy_ShouldAdvanceTime()
    {
        // Arrange
        var startTime = _helper.TimeProvider.GetUtcNow();

        // Act
        _helper.AdvanceTimeBy(TimeSpan.FromHours(2));

        // Assert
        _helper.TimeProvider.GetUtcNow().ShouldBe(startTime.AddHours(2));
    }

    [Fact]
    public void AdvanceTimeByMinutes_ShouldAdvanceTime()
    {
        // Arrange
        var startTime = _helper.TimeProvider.GetUtcNow();

        // Act
        _helper.AdvanceTimeByMinutes(45);

        // Assert
        _helper.TimeProvider.GetUtcNow().ShouldBe(startTime.AddMinutes(45));
    }

    [Fact]
    public void AdvanceTimeByHours_ShouldAdvanceTime()
    {
        // Arrange
        var startTime = _helper.TimeProvider.GetUtcNow();

        // Act
        _helper.AdvanceTimeByHours(8);

        // Assert
        _helper.TimeProvider.GetUtcNow().ShouldBe(startTime.AddHours(8));
    }

    [Fact]
    public void AdvanceTimeByDays_ShouldAdvanceTime()
    {
        // Arrange
        var startTime = _helper.TimeProvider.GetUtcNow();

        // Act
        _helper.AdvanceTimeByDays(3);

        // Assert
        _helper.TimeProvider.GetUtcNow().ShouldBe(startTime.AddDays(3));
    }

    [Fact]
    public void AdvanceTimeUntilDue_ShouldAdvanceUntilMessageIsDue()
    {
        // Arrange
        _helper.GivenScheduledMessage(
            new SendReminderCommand { UserId = "user-123" },
            TimeSpan.FromHours(5));
        var messageId = _helper.LastMessageId;
        var message = _helper.Store.GetMessage(messageId);

        // Act
        _helper.AdvanceTimeUntilDue(messageId);

        // Assert
        _helper.TimeProvider.GetUtcNow().UtcDateTime.ShouldBeGreaterThan(message!.ScheduledAtUtc);
    }

    [Fact]
    public void GetCurrentTime_ShouldReturnCurrentFakeTime()
    {
        // Arrange
        var expected = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // Assert
        _helper.GetCurrentTime().ShouldBe(expected);
    }

    [Fact]
    public async Task GetDueMessagesAsync_ShouldReturnDueMessages()
    {
        // Arrange - schedule message that is already due (in the past)
        _helper
            .GivenDueMessage(new SendReminderCommand { UserId = "1" });

        // Message should be due immediately
        _helper.WhenNothing();
        var dueMessages = await _helper.GetDueMessagesAsync();
        dueMessages.Count().ShouldBe(1);
    }

    #endregion

    #region Flow Validation Tests

    [Fact]
    public void ThenAssertions_BeforeWhen_ShouldThrow()
    {
        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            _helper
                .GivenNoScheduledMessages()
                .ThenNoScheduledMessages());
    }

    [Fact]
    public void FluentChaining_ShouldWork()
    {
        // Act & Assert
        Should.NotThrow(() =>
            _helper
                .GivenNoScheduledMessages()
                .WhenMessageScheduled(new SendReminderCommand { UserId = "user-123" }, TimeSpan.FromHours(1))
                .ThenNoException()
                .ThenMessageWasScheduled<SendReminderCommand>()
                .ThenScheduledMessageCount(1)
                .ThenMessageIsNotDue(_helper.LastMessageId));
    }

    [Fact]
    public void TimeTravel_CompleteWorkflow_ShouldWork()
    {
        // Arrange & Act & Assert
        Should.NotThrow(() =>
            _helper
                .GivenNoScheduledMessages()
                .WhenMessageScheduled(
                    new SendReminderCommand { UserId = "user-123" },
                    TimeSpan.FromHours(24))
                .ThenMessageWasScheduled<SendReminderCommand>()
                .ThenMessageIsNotDue(_helper.LastMessageId)
                .AdvanceTimeByHours(25)
                .WhenNothing()
                .ThenMessageIsDue<SendReminderCommand>());
    }

    #endregion

    #region Test Types

    private sealed class SendReminderCommand
    {
        public string UserId { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
    }

    private sealed class DailyReportCommand
    {
        public required string ReportId { get; init; }
        public required string RecipientId { get; init; }
        public required bool IncludeMetrics { get; init; }
    }

    #endregion
}
