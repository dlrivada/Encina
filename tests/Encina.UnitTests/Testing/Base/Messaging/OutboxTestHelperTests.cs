using Encina.Testing;
using Encina.Testing.Fakes.Models;
using Encina.Testing.Messaging;
using Encina.Testing.Time;
using FakeOutboxMessage = Encina.Testing.Fakes.Models.FakeOutboxMessage;

namespace Encina.UnitTests.Testing.Base.Messaging;

/// <summary>
/// Unit tests for <see cref="OutboxTestHelper"/>.
/// </summary>
public sealed class OutboxTestHelperTests : IDisposable
{
    private readonly OutboxTestHelper _helper;

    public OutboxTestHelperTests()
    {
        _helper = new OutboxTestHelper(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }

    public void Dispose()
    {
        _helper.Dispose();
    }

    #region Given Tests

    [Fact]
    public void GivenEmptyOutbox_ShouldClearStore()
    {
        // Act
        _helper.GivenEmptyOutbox();

        // Assert
        _helper.Store.GetMessages().ShouldBeEmpty();
    }

    [Fact]
    public void GivenPendingMessage_ShouldAddMessageToStore()
    {
        // Act
        _helper.GivenPendingMessage(new TestNotification { Value = "test" });

        // Assert
        _helper.Store.GetMessages().Count.ShouldBe(1);
        _helper.Store.GetMessages().First().ProcessedAtUtc.ShouldBeNull();
    }

    [Fact]
    public void GivenProcessedMessage_ShouldAddProcessedMessageToStore()
    {
        // Act
        _helper.GivenProcessedMessage(new TestNotification { Value = "test" });

        // Assert
        _helper.Store.GetMessages().Count.ShouldBe(1);
        _helper.Store.GetMessages().First().ProcessedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void GivenFailedMessage_ShouldAddFailedMessageWithRetryCount()
    {
        // Act
        _helper.GivenFailedMessage(new TestNotification { Value = "test" }, retryCount: 3, errorMessage: "Failed");

        // Assert
        var message = _helper.Store.GetMessages().First();
        message.RetryCount.ShouldBe(3);
        message.ErrorMessage.ShouldBe("Failed");
    }

    #endregion

    #region When Tests

    [Fact]
    public void WhenMessageAdded_ShouldAddMessageToStore()
    {
        // Act
        _helper
            .GivenEmptyOutbox()
            .WhenMessageAdded(new TestNotification { Value = "test" });

        // Assert
        _helper.Store.GetAddedMessages().Count.ShouldBe(1);
    }

    [Fact]
    public void WhenMessageProcessed_ShouldMarkAsProcessed()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = new FakeOutboxMessage
        {
            Id = messageId,
            NotificationType = "Test",
            Content = "{}",
            CreatedAtUtc = _helper.TimeProvider.GetUtcNow().UtcDateTime
        };

        // Act
        _helper.Store.AddAsync(message).GetAwaiter().GetResult();
        _helper.WhenMessageProcessed(messageId);

        // Assert
        _helper.Store.GetProcessedMessageIds().ShouldContain(messageId);
    }

    [Fact]
    public void WhenMessageFailed_ShouldMarkAsFailed()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = new FakeOutboxMessage
        {
            Id = messageId,
            NotificationType = "Test",
            Content = "{}",
            CreatedAtUtc = _helper.TimeProvider.GetUtcNow().UtcDateTime
        };

        // Act
        _helper.Store.AddAsync(message).GetAwaiter().GetResult();
        _helper.WhenMessageFailed(messageId, "Error message");

        // Assert
        _helper.Store.GetFailedMessageIds().ShouldContain(messageId);
    }

    [Fact]
    public void When_WithException_ShouldCaptureException()
    {
        // Act
        _helper
            .GivenEmptyOutbox()
            .When(_ => throw new InvalidOperationException("Test exception"));

        // Assert - ThenThrows returns the caught exception, it doesn't throw
        var ex = _helper.ThenThrows<InvalidOperationException>();
        ex.Message.ShouldBe("Test exception");
    }

    #endregion

    #region Then Tests

    [Fact]
    public void ThenOutboxContains_WhenMessageExists_ShouldPass()
    {
        // Act & Assert
        Should.NotThrow(() =>
            _helper
                .GivenEmptyOutbox()
                .WhenMessageAdded(new TestNotification { Value = "test" })
                .ThenOutboxContains<TestNotification>());
    }

    [Fact]
    public void ThenOutboxContains_WhenMessageNotExists_ShouldThrow()
    {
        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            _helper
                .GivenEmptyOutbox()
                .WhenMessageAdded(new TestNotification { Value = "test" })
                .ThenOutboxContains<OtherNotification>());
    }

    [Fact]
    public void ThenOutboxContains_WithPredicate_WhenMatch_ShouldPass()
    {
        // Act & Assert
        Should.NotThrow(() =>
            _helper
                .GivenEmptyOutbox()
                .WhenMessageAdded(new TestNotification { Value = "expected" })
                .ThenOutboxContains<TestNotification>(n => n.Value == "expected"));
    }

    [Fact]
    public void ThenOutboxContains_WithPredicate_WhenNoMatch_ShouldThrow()
    {
        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            _helper
                .GivenEmptyOutbox()
                .WhenMessageAdded(new TestNotification { Value = "actual" })
                .ThenOutboxContains<TestNotification>(n => n.Value == "expected"));
    }

    [Fact]
    public void ThenOutboxIsEmpty_WhenEmpty_ShouldPass()
    {
        // Act & Assert
        Should.NotThrow(() =>
            _helper
                .GivenEmptyOutbox()
                .When(_ => { }) // No-op
                .ThenOutboxIsEmpty());
    }

    [Fact]
    public void ThenOutboxIsEmpty_WhenNotEmpty_ShouldThrow()
    {
        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            _helper
                .GivenEmptyOutbox()
                .WhenMessageAdded(new TestNotification { Value = "test" })
                .ThenOutboxIsEmpty());
    }

    [Fact]
    public void ThenOutboxHasCount_WhenCorrect_ShouldPass()
    {
        // Act & Assert
        Should.NotThrow(() =>
            _helper
                .GivenEmptyOutbox()
                .WhenMessageAdded(new TestNotification { Value = "1" })
                .ThenOutboxHasCount(1));
    }

    [Fact]
    public void ThenOutboxHasCount_WhenIncorrect_ShouldThrow()
    {
        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            _helper
                .GivenEmptyOutbox()
                .WhenMessageAdded(new TestNotification { Value = "1" })
                .ThenOutboxHasCount(2));
    }

    [Fact]
    public void ThenNoException_WhenNoException_ShouldPass()
    {
        // Act & Assert
        Should.NotThrow(() =>
            _helper
                .GivenEmptyOutbox()
                .When(_ => { })
                .ThenNoException());
    }

    [Fact]
    public void ThenNoException_WhenExceptionThrown_ShouldThrow()
    {
        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            _helper
                .GivenEmptyOutbox()
                .When(_ => throw new InvalidOperationException())
                .ThenNoException());
    }

    [Fact]
    public void GetMessage_ShouldReturnDeserializedNotification()
    {
        // Act
        _helper
            .GivenEmptyOutbox()
            .WhenMessageAdded(new TestNotification { Value = "test-value" });

        var notification = _helper.GetMessage<TestNotification>();

        // Assert
        notification.ShouldNotBeNull();
        notification.Value.ShouldBe("test-value");
    }

    [Fact]
    public void GetMessages_ShouldReturnAllMatchingNotifications()
    {
        // Act
        _helper
            .GivenEmptyOutbox()
            .WhenMessageAdded(new TestNotification { Value = "1" });

        _helper.WhenMessageAdded(new TestNotification { Value = "2" });

        var notifications = _helper.GetMessages<TestNotification>();

        // Assert
        notifications.Count.ShouldBe(2);
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
        _helper.AdvanceTimeByMinutes(30);

        // Assert
        _helper.TimeProvider.GetUtcNow().ShouldBe(startTime.AddMinutes(30));
    }

    #endregion

    #region Flow Validation Tests

    [Fact]
    public void ThenAssertions_BeforeWhen_ShouldThrow()
    {
        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            _helper
                .GivenEmptyOutbox()
                .ThenOutboxIsEmpty());
    }

    [Fact]
    public void FluentChaining_ShouldWork()
    {
        // Act & Assert
        Should.NotThrow(() =>
            _helper
                .GivenEmptyOutbox()
                .WhenMessageAdded(new TestNotification { Value = "test" })
                .ThenNoException()
                .ThenOutboxContains<TestNotification>()
                .ThenOutboxHasCount(1));
    }

    #endregion

    #region Test Types

    private sealed class TestNotification
    {
        public string Value { get; init; } = string.Empty;
    }

    private sealed class OtherNotification
    {
        public string Value { get; init; } = string.Empty;
    }

    #endregion
}
