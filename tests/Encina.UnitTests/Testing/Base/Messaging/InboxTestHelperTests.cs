using Encina.Testing;
using Encina.Testing.Messaging;

namespace Encina.UnitTests.Testing.Base.Messaging;

/// <summary>
/// Unit tests for <see cref="InboxTestHelper"/>.
/// </summary>
public sealed class InboxTestHelperTests : IDisposable
{
    private readonly InboxTestHelper _helper;

    public InboxTestHelperTests()
    {
        _helper = new InboxTestHelper(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }

    public void Dispose()
    {
        _helper.Dispose();
    }

    #region Given Tests

    [Fact]
    public void GivenEmptyInbox_ShouldClearStore()
    {
        // Act
        _helper.GivenEmptyInbox();

        // Assert
        _helper.Store.GetMessages().ShouldBeEmpty();
    }

    [Fact]
    public void GivenProcessedMessage_ShouldAddProcessedMessageWithCachedResponse()
    {
        // Act
        _helper.GivenProcessedMessage("msg-123", new TestResponse { Success = true });

        // Assert
        var message = _helper.Store.GetMessage("msg-123");
        message.ShouldNotBeNull();
        message.IsProcessed.ShouldBeTrue();
        message.Response.ShouldNotBeNull();
    }

    [Fact]
    public void GivenPendingMessage_ShouldAddUnprocessedMessage()
    {
        // Act
        _helper.GivenPendingMessage("msg-456", "OrderCommand");

        // Assert
        var message = _helper.Store.GetMessage("msg-456");
        message.ShouldNotBeNull();
        message.IsProcessed.ShouldBeFalse();
        message.RequestType.ShouldBe("OrderCommand");
    }

    [Fact]
    public void GivenFailedMessage_ShouldAddFailedMessageWithRetryCount()
    {
        // Act
        _helper.GivenFailedMessage("msg-789", retryCount: 2, errorMessage: "Connection timeout");

        // Assert
        var message = _helper.Store.GetMessage("msg-789");
        message.ShouldNotBeNull();
        message.RetryCount.ShouldBe(2);
        message.ErrorMessage.ShouldBe("Connection timeout");
    }

    [Fact]
    public void GivenExpiredMessage_ShouldAddExpiredMessage()
    {
        // Act
        _helper.GivenExpiredMessage("msg-expired");

        // Assert
        var message = _helper.Store.GetMessage("msg-expired");
        message.ShouldNotBeNull();
        message.IsExpired().ShouldBeTrue();
    }

    #endregion

    #region When Tests

    [Fact]
    public void WhenMessageReceived_ShouldRetrieveExistingMessage()
    {
        // Act
        var exception = Record.Exception(() =>
            _helper
                .GivenProcessedMessage("msg-123", new TestResponse { Success = true })
                .WhenMessageReceived("msg-123")
                .ThenNoException());

        // Assert - no exception means message was retrieved
        Assert.Null(exception);
    }

    [Fact]
    public void WhenMessageRegistered_ShouldAddNewMessage()
    {
        // Act
        _helper
            .GivenEmptyInbox()
            .WhenMessageRegistered("new-msg", "TestRequest");

        // Assert
        _helper.Store.GetAddedMessages().Count.ShouldBe(1);
    }

    [Fact]
    public void WhenMessageProcessed_ShouldMarkAsProcessed()
    {
        // Act
        _helper
            .GivenPendingMessage("msg-123", "TestRequest")
            .WhenMessageProcessed("msg-123", new TestResponse { Success = true });

        // Assert
        _helper.Store.GetProcessedMessageIds().ShouldContain("msg-123");
    }

    [Fact]
    public void WhenMessageFailed_ShouldMarkAsFailed()
    {
        // Act
        _helper
            .GivenPendingMessage("msg-123", "TestRequest")
            .WhenMessageFailed("msg-123", "Processing error");

        // Assert
        _helper.Store.GetFailedMessageIds().ShouldContain("msg-123");
    }

    #endregion

    #region Then Tests - Idempotency

    [Fact]
    public void ThenMessageWasAlreadyProcessed_WhenProcessed_ShouldPass()
    {
        // Act & Assert
        Should.NotThrow(() =>
            _helper
                .GivenProcessedMessage("msg-123", new TestResponse { Success = true })
                .WhenMessageReceived("msg-123")
                .ThenMessageWasAlreadyProcessed("msg-123"));
    }

    [Fact]
    public void ThenMessageWasAlreadyProcessed_WhenNotProcessed_ShouldThrow()
    {
        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            _helper
                .GivenPendingMessage("msg-123", "TestRequest")
                .WhenMessageReceived("msg-123")
                .ThenMessageWasAlreadyProcessed("msg-123"));
    }

    [Fact]
    public void ThenMessageIsNew_WhenNew_ShouldPass()
    {
        // Act & Assert
        Should.NotThrow(() =>
            _helper
                .GivenEmptyInbox()
                .WhenMessageRegistered("new-msg", "TestRequest")
                .ThenMessageIsNew("new-msg"));
    }

    [Fact]
    public void ThenCachedResponseIs_WhenMatchesPredicate_ShouldPass()
    {
        // Act & Assert
        Should.NotThrow(() =>
            _helper
                .GivenProcessedMessage("msg-123", new TestResponse { Success = true, Value = 42 })
                .WhenMessageReceived("msg-123")
                .ThenCachedResponseIs<TestResponse>(r => r.Success && r.Value == 42));
    }

    [Fact]
    public void ThenCachedResponseIs_WhenDoesNotMatch_ShouldThrow()
    {
        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            _helper
                .GivenProcessedMessage("msg-123", new TestResponse { Success = false })
                .WhenMessageReceived("msg-123")
                .ThenCachedResponseIs<TestResponse>(r => r.Success));
    }

    #endregion

    #region Then Tests - Assertions

    [Fact]
    public void ThenInboxIsEmpty_WhenEmpty_ShouldPass()
    {
        // Act & Assert
        Should.NotThrow(() =>
            _helper
                .GivenEmptyInbox()
                .When(_ => { })
                .ThenInboxIsEmpty());
    }

    [Fact]
    public void ThenInboxHasCount_WhenCorrect_ShouldPass()
    {
        // Act & Assert
        Should.NotThrow(() =>
            _helper
                .GivenPendingMessage("msg-1", "Test")
                .When(_ => { })
                .ThenInboxHasCount(1));
    }

    [Fact]
    public void ThenMessageWasProcessed_ShouldVerifyProcessing()
    {
        // Act & Assert
        Should.NotThrow(() =>
            _helper
                .GivenPendingMessage("msg-123", "TestRequest")
                .WhenMessageProcessed("msg-123", new TestResponse { Success = true })
                .ThenMessageWasProcessed("msg-123"));
    }

    [Fact]
    public void ThenMessageWasFailed_ShouldVerifyFailure()
    {
        // Act & Assert
        Should.NotThrow(() =>
            _helper
                .GivenPendingMessage("msg-123", "TestRequest")
                .WhenMessageFailed("msg-123", "Error")
                .ThenMessageWasFailed("msg-123"));
    }

    [Fact]
    public void GetCachedResponse_ShouldReturnDeserializedResponse()
    {
        // Act
        _helper
            .GivenProcessedMessage("msg-123", new TestResponse { Success = true, Value = 100 })
            .WhenMessageReceived("msg-123");

        var response = _helper.GetCachedResponse<TestResponse>("msg-123");

        // Assert
        response.Success.ShouldBeTrue();
        response.Value.ShouldBe(100);
    }

    #endregion

    #region Time Control Tests

    [Fact]
    public void AdvanceTimeBy_ShouldAdvanceTime()
    {
        // Arrange
        var startTime = _helper.TimeProvider.GetUtcNow();

        // Act
        _helper.AdvanceTimeBy(TimeSpan.FromHours(1));

        // Assert
        _helper.TimeProvider.GetUtcNow().ShouldBe(startTime.AddHours(1));
    }

    [Fact]
    public async Task AdvanceTimeByDays_ShouldExpireMessages()
    {
        // Arrange
        _helper.GivenPendingMessage("msg-123", "Test");

        // Act
        _helper.AdvanceTimeByDays(8); // Default expiry is 7 days

        // Assert
        var expiredMessages = await _helper.Store.GetExpiredMessagesAsync(10);
        expiredMessages.ShouldNotBeEmpty();
    }

    #endregion

    #region Flow Validation Tests

    [Fact]
    public void ThenAssertions_BeforeWhen_ShouldThrow()
    {
        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            _helper
                .GivenEmptyInbox()
                .ThenInboxIsEmpty());
    }

    [Fact]
    public void FluentChaining_ShouldWork()
    {
        // Act & Assert
        Should.NotThrow(() =>
            _helper
                .GivenEmptyInbox()
                .WhenMessageRegistered("msg-123", "TestRequest")
                .ThenNoException()
                .ThenInboxHasCount(1)
                .ThenMessageIsNew("msg-123"));
    }

    #endregion

    #region Test Types

    private sealed class TestResponse
    {
        public bool Success { get; init; }
        public int Value { get; init; }
    }

    #endregion
}
