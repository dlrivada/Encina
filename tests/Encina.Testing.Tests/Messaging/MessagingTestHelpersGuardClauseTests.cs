using Encina.Testing.Fakes.Stores;
using Encina.Testing.Messaging;
using Encina.Testing.Time;

namespace Encina.Testing.Tests.Messaging;

/// <summary>
/// Guard clause tests for messaging test helpers.
/// Verifies that null arguments throw ArgumentNullException.
/// </summary>
public sealed class MessagingTestHelpersGuardClauseTests
{
    #region OutboxTestHelper Guard Clauses

    [Fact]
    public void OutboxTestHelper_Constructor_WithNullStore_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new OutboxTestHelper(null!, new FakeTimeProvider()));
        ex.ParamName.ShouldBe("store");
    }

    [Fact]
    public void OutboxTestHelper_Constructor_WithNullTimeProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new OutboxTestHelper(new FakeOutboxStore(), null!));
        ex.ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void OutboxTestHelper_GivenMessages_WithNullMessages_ThrowsArgumentNullException()
    {
        // Arrange
        using var helper = new OutboxTestHelper();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            helper.GivenMessages(null!));
    }

    [Fact]
    public void OutboxTestHelper_GivenPendingMessage_WithNullNotification_ThrowsArgumentNullException()
    {
        // Arrange
        using var helper = new OutboxTestHelper();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            helper.GivenPendingMessage<TestNotification>(null!));
    }

    [Fact]
    public void OutboxTestHelper_WhenMessageAdded_WithNullNotification_ThrowsArgumentNullException()
    {
        // Arrange
        using var helper = new OutboxTestHelper();
        helper.GivenEmptyOutbox();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            helper.WhenMessageAdded<TestNotification>(null!));
    }

    [Fact]
    public void OutboxTestHelper_When_WithNullAction_ThrowsArgumentNullException()
    {
        // Arrange
        using var helper = new OutboxTestHelper();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            helper.When(null!));
    }

    [Fact]
    public void OutboxTestHelper_WhenAsync_WithNullAction_ThrowsArgumentNullException()
    {
        // Arrange
        using var helper = new OutboxTestHelper();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            helper.WhenAsync(null!));
    }

    [Fact]
    public void OutboxTestHelper_ThenOutboxContains_WithNullPredicate_ThrowsArgumentNullException()
    {
        // Arrange
        using var helper = new OutboxTestHelper();
        helper.GivenEmptyOutbox().WhenMessageAdded(new TestNotification { Value = "test" });

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            helper.ThenOutboxContains<TestNotification>(null!));
    }

    #endregion

    #region InboxTestHelper Guard Clauses

    [Fact]
    public void InboxTestHelper_Constructor_WithNullStore_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new InboxTestHelper(null!, new FakeTimeProvider()));
        ex.ParamName.ShouldBe("store");
    }

    [Fact]
    public void InboxTestHelper_Constructor_WithNullTimeProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new InboxTestHelper(new FakeInboxStore(), null!));
        ex.ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void InboxTestHelper_GivenProcessedMessage_WithNullMessageId_ThrowsArgumentNullException()
    {
        // Arrange
        using var helper = new InboxTestHelper();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            helper.GivenProcessedMessage(null!, new TestResponse()));
    }

    [Fact]
    public void InboxTestHelper_GivenProcessedMessage_WithNullResponse_ThrowsArgumentNullException()
    {
        // Arrange
        using var helper = new InboxTestHelper();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            helper.GivenProcessedMessage<TestResponse>("msg-1", null!));
    }

    [Fact]
    public void InboxTestHelper_WhenMessageReceived_WithNullMessageId_ThrowsArgumentNullException()
    {
        // Arrange
        using var helper = new InboxTestHelper();
        helper.GivenEmptyInbox();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            helper.WhenMessageReceived(null!));
    }

    [Fact]
    public void InboxTestHelper_WhenMessageProcessed_WithNullMessageId_ThrowsArgumentNullException()
    {
        // Arrange
        using var helper = new InboxTestHelper();
        helper.GivenEmptyInbox();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            helper.WhenMessageProcessed(null!, new TestResponse()));
    }

    [Fact]
    public void InboxTestHelper_WhenMessageProcessed_WithNullResponse_ThrowsArgumentNullException()
    {
        // Arrange
        using var helper = new InboxTestHelper();
        helper.GivenEmptyInbox();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            helper.WhenMessageProcessed<TestResponse>("msg-1", null!));
    }

    [Fact]
    public void InboxTestHelper_When_WithNullAction_ThrowsArgumentNullException()
    {
        // Arrange
        using var helper = new InboxTestHelper();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            helper.When(null!));
    }

    [Fact]
    public void InboxTestHelper_ThenCachedResponseIs_WithNullPredicate_ThrowsArgumentNullException()
    {
        // Arrange
        using var helper = new InboxTestHelper();
        helper.GivenProcessedMessage("msg-1", new TestResponse())
            .WhenMessageReceived("msg-1");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            helper.ThenCachedResponseIs<TestResponse>(null!));
    }

    #endregion

    #region SagaTestHelper Guard Clauses

    [Fact]
    public void SagaTestHelper_Constructor_WithNullStore_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new SagaTestHelper(null!, new FakeTimeProvider()));
        ex.ParamName.ShouldBe("store");
    }

    [Fact]
    public void SagaTestHelper_Constructor_WithNullTimeProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new SagaTestHelper(new FakeSagaStore(), null!));
        ex.ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void SagaTestHelper_GivenNewSaga_WithNullData_ThrowsArgumentNullException()
    {
        // Arrange
        using var helper = new SagaTestHelper();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            helper.GivenNewSaga<TestSaga, TestSagaData>(Guid.NewGuid(), null!));
    }

    [Fact]
    public void SagaTestHelper_WhenSagaStarts_WithNullData_ThrowsArgumentNullException()
    {
        // Arrange
        using var helper = new SagaTestHelper();
        helper.GivenNoSagas();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            helper.WhenSagaStarts<TestSaga, TestSagaData>(Guid.NewGuid(), null!));
    }

    [Fact]
    public void SagaTestHelper_WhenSagaDataUpdated_WithNullData_ThrowsArgumentNullException()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        using var helper = new SagaTestHelper();
        helper.GivenRunningSaga<TestSaga, TestSagaData>(sagaId, new TestSagaData { OrderId = "ORD-1" });

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            helper.WhenSagaDataUpdated<TestSagaData>(sagaId, null!));
    }

    [Fact]
    public void SagaTestHelper_When_WithNullAction_ThrowsArgumentNullException()
    {
        // Arrange
        using var helper = new SagaTestHelper();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            helper.When(null!));
    }

    [Fact]
    public void SagaTestHelper_ThenSagaData_WithNullPredicate_ThrowsArgumentNullException()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        using var helper = new SagaTestHelper();
        helper.GivenRunningSaga<TestSaga, TestSagaData>(sagaId, new TestSagaData { OrderId = "ORD-1" })
            .When(_ => { });

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            helper.ThenSagaData<TestSagaData>(sagaId, null!));
    }

    #endregion

    #region SchedulingTestHelper Guard Clauses

    [Fact]
    public void SchedulingTestHelper_Constructor_WithNullStore_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new SchedulingTestHelper(null!, new FakeTimeProvider()));
        ex.ParamName.ShouldBe("store");
    }

    [Fact]
    public void SchedulingTestHelper_Constructor_WithNullTimeProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new SchedulingTestHelper(new FakeScheduledMessageStore(), null!));
        ex.ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void SchedulingTestHelper_GivenScheduledMessage_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        using var helper = new SchedulingTestHelper();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            helper.GivenScheduledMessage<TestRequest>(null!, TimeSpan.FromHours(1)));
    }

    [Fact]
    public void SchedulingTestHelper_GivenRecurringMessage_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        using var helper = new SchedulingTestHelper();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            helper.GivenRecurringMessage<TestRequest>(null!, "0 0 * * *", TimeSpan.FromHours(1)));
    }

    [Fact]
    public void SchedulingTestHelper_GivenRecurringMessage_WithNullCron_ThrowsArgumentNullException()
    {
        // Arrange
        using var helper = new SchedulingTestHelper();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            helper.GivenRecurringMessage(new TestRequest(), null!, TimeSpan.FromHours(1)));
    }

    [Fact]
    public void SchedulingTestHelper_WhenMessageScheduled_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        using var helper = new SchedulingTestHelper();
        helper.GivenNoScheduledMessages();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            helper.WhenMessageScheduled<TestRequest>(null!, TimeSpan.FromHours(1)));
    }

    [Fact]
    public void SchedulingTestHelper_WhenRecurringMessageScheduled_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        using var helper = new SchedulingTestHelper();
        helper.GivenNoScheduledMessages();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            helper.WhenRecurringMessageScheduled<TestRequest>(null!, "0 0 * * *", TimeSpan.FromHours(1)));
    }

    [Fact]
    public void SchedulingTestHelper_WhenRecurringMessageScheduled_WithNullCron_ThrowsArgumentNullException()
    {
        // Arrange
        using var helper = new SchedulingTestHelper();
        helper.GivenNoScheduledMessages();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            helper.WhenRecurringMessageScheduled(new TestRequest(), null!, TimeSpan.FromHours(1)));
    }

    [Fact]
    public void SchedulingTestHelper_When_WithNullAction_ThrowsArgumentNullException()
    {
        // Arrange
        using var helper = new SchedulingTestHelper();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            helper.When(null!));
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
