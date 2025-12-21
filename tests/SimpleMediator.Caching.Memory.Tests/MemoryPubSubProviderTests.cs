namespace SimpleMediator.Caching.Memory.Tests;

/// <summary>
/// Unit tests for <see cref="MemoryPubSubProvider"/>.
/// </summary>
public sealed class MemoryPubSubProviderTests
{
    private readonly MemoryPubSubProvider _sut;

    public MemoryPubSubProviderTests()
    {
        _sut = new MemoryPubSubProvider(NullLogger<MemoryPubSubProvider>.Instance);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>("logger", () =>
            new MemoryPubSubProvider(null!));
    }

    #endregion

    #region PublishAsync Tests

    [Fact]
    public async Task PublishAsync_WithNullChannel_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("channel", () =>
            _sut.PublishAsync(null!, "message", CancellationToken.None));
    }

    [Fact]
    public async Task PublishAsync_WithNullMessage_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("message", () =>
            _sut.PublishAsync("channel", null!, CancellationToken.None));
    }

    [Fact]
    public async Task PublishAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.PublishAsync("channel", "message", cts.Token));
    }

    [Fact]
    public async Task PublishAsync_WithSubscriber_NotifiesSubscriber()
    {
        // Arrange
        var receivedMessages = new List<string>();
        await _sut.SubscribeAsync("test-channel", msg =>
        {
            receivedMessages.Add(msg);
            return Task.CompletedTask;
        }, CancellationToken.None);

        // Act
        await _sut.PublishAsync("test-channel", "test-message", CancellationToken.None);

        // Assert
        receivedMessages.Should().ContainSingle()
            .Which.Should().Be("test-message");
    }

    [Fact]
    public async Task PublishAsync_WithNoSubscribers_DoesNotThrow()
    {
        // Act & Assert
        await _sut.Invoking(s => s.PublishAsync("no-subscribers", "message", CancellationToken.None))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsyncTyped_WithNullChannel_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("channel", () =>
            _sut.PublishAsync<TestMessage>(null!, new TestMessage("test"), CancellationToken.None));
    }

    [Fact]
    public async Task PublishAsyncTyped_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.PublishAsync<TestMessage>("channel", new TestMessage("test"), cts.Token));
    }

    [Fact]
    public async Task PublishAsyncTyped_SerializesMessage()
    {
        // Arrange
        var receivedMessages = new List<string>();
        await _sut.SubscribeAsync("typed-channel", msg =>
        {
            receivedMessages.Add(msg);
            return Task.CompletedTask;
        }, CancellationToken.None);

        var message = new TestMessage("Hello");

        // Act
        await _sut.PublishAsync("typed-channel", message, CancellationToken.None);

        // Assert
        receivedMessages.Should().ContainSingle()
            .Which.Should().Contain("\"content\":\"Hello\"");
    }

    #endregion

    #region SubscribeAsync Tests

    [Fact]
    public async Task SubscribeAsync_WithNullChannel_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("channel", () =>
            _sut.SubscribeAsync(null!, _ => Task.CompletedTask, CancellationToken.None));
    }

    [Fact]
    public async Task SubscribeAsync_WithNullHandler_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("handler", () =>
            _sut.SubscribeAsync("channel", (Func<string, Task>)null!, CancellationToken.None));
    }

    [Fact]
    public async Task SubscribeAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.SubscribeAsync("channel", _ => Task.CompletedTask, cts.Token));
    }

    [Fact]
    public async Task SubscribeAsync_ReturnsDisposableSubscription()
    {
        // Act
        var subscription = await _sut.SubscribeAsync("test", _ => Task.CompletedTask, CancellationToken.None);

        // Assert
        subscription.Should().NotBeNull();
        subscription.Should().BeAssignableTo<IAsyncDisposable>();
    }

    [Fact]
    public async Task SubscribeAsync_MultipleSubscribers_AllReceiveMessages()
    {
        // Arrange
        var received1 = new List<string>();
        var received2 = new List<string>();

        await _sut.SubscribeAsync("multi-channel", msg =>
        {
            received1.Add(msg);
            return Task.CompletedTask;
        }, CancellationToken.None);

        await _sut.SubscribeAsync("multi-channel", msg =>
        {
            received2.Add(msg);
            return Task.CompletedTask;
        }, CancellationToken.None);

        // Act
        await _sut.PublishAsync("multi-channel", "broadcast", CancellationToken.None);

        // Assert
        received1.Should().ContainSingle().Which.Should().Be("broadcast");
        received2.Should().ContainSingle().Which.Should().Be("broadcast");
    }

    [Fact]
    public async Task SubscribeAsync_DisposingSubscription_StopsReceivingMessages()
    {
        // Arrange
        var received = new List<string>();
        var subscription = await _sut.SubscribeAsync("dispose-channel", msg =>
        {
            received.Add(msg);
            return Task.CompletedTask;
        }, CancellationToken.None);

        // Act
        await _sut.PublishAsync("dispose-channel", "before-dispose", CancellationToken.None);
        await subscription.DisposeAsync();
        await _sut.PublishAsync("dispose-channel", "after-dispose", CancellationToken.None);

        // Assert
        received.Should().ContainSingle().Which.Should().Be("before-dispose");
    }

    #endregion

    #region SubscribeAsync<T> Tests

    [Fact]
    public async Task SubscribeAsyncTyped_WithNullChannel_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("channel", () =>
            _sut.SubscribeAsync<TestMessage>(null!, _ => Task.CompletedTask, CancellationToken.None));
    }

    [Fact]
    public async Task SubscribeAsyncTyped_WithNullHandler_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("handler", () =>
            _sut.SubscribeAsync<TestMessage>("channel", null!, CancellationToken.None));
    }

    [Fact]
    public async Task SubscribeAsyncTyped_DeserializesMessage()
    {
        // Arrange
        var received = new List<TestMessage>();
        await _sut.SubscribeAsync<TestMessage>("typed-sub-channel", msg =>
        {
            received.Add(msg);
            return Task.CompletedTask;
        }, CancellationToken.None);

        var message = new TestMessage("Hello Typed");

        // Act
        await _sut.PublishAsync("typed-sub-channel", message, CancellationToken.None);

        // Assert
        received.Should().ContainSingle()
            .Which.Content.Should().Be("Hello Typed");
    }

    #endregion

    #region SubscribePatternAsync Tests

    [Fact]
    public async Task SubscribePatternAsync_WithNullPattern_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("pattern", () =>
            _sut.SubscribePatternAsync(null!, (_, _) => Task.CompletedTask, CancellationToken.None));
    }

    [Fact]
    public async Task SubscribePatternAsync_WithNullHandler_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("handler", () =>
            _sut.SubscribePatternAsync("pattern*", null!, CancellationToken.None));
    }

    [Fact]
    public async Task SubscribePatternAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.SubscribePatternAsync("pattern*", (_, _) => Task.CompletedTask, cts.Token));
    }

    [Fact]
    public async Task SubscribePatternAsync_WithWildcard_ReceivesMatchingChannels()
    {
        // Arrange
        var received = new List<(string Channel, string Message)>();
        await _sut.SubscribePatternAsync("user:*", (channel, msg) =>
        {
            received.Add((channel, msg));
            return Task.CompletedTask;
        }, CancellationToken.None);

        // Act
        await _sut.PublishAsync("user:1:updated", "User 1 updated", CancellationToken.None);
        await _sut.PublishAsync("user:2:deleted", "User 2 deleted", CancellationToken.None);
        await _sut.PublishAsync("product:1:updated", "Product 1 updated", CancellationToken.None);

        // Assert
        received.Should().HaveCount(2);
        received.Should().Contain(x => x.Channel == "user:1:updated" && x.Message == "User 1 updated");
        received.Should().Contain(x => x.Channel == "user:2:deleted" && x.Message == "User 2 deleted");
    }

    [Fact]
    public async Task SubscribePatternAsync_DisposingSubscription_StopsReceivingMessages()
    {
        // Arrange
        var received = new List<(string Channel, string Message)>();
        var subscription = await _sut.SubscribePatternAsync("orders:*", (channel, msg) =>
        {
            received.Add((channel, msg));
            return Task.CompletedTask;
        }, CancellationToken.None);

        // Act
        await _sut.PublishAsync("orders:created", "Order 1 created", CancellationToken.None);
        await subscription.DisposeAsync();
        await _sut.PublishAsync("orders:updated", "Order 1 updated", CancellationToken.None);

        // Assert
        received.Should().ContainSingle()
            .Which.Should().Be(("orders:created", "Order 1 created"));
    }

    #endregion

    #region UnsubscribeAsync Tests

    [Fact]
    public async Task UnsubscribeAsync_WithNullChannel_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("channel", () =>
            _sut.UnsubscribeAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task UnsubscribeAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.UnsubscribeAsync("channel", cts.Token));
    }

    [Fact]
    public async Task UnsubscribeAsync_RemovesAllSubscribers()
    {
        // Arrange
        var received = new List<string>();
        await _sut.SubscribeAsync("unsub-channel", msg =>
        {
            received.Add(msg);
            return Task.CompletedTask;
        }, CancellationToken.None);

        // Act
        await _sut.UnsubscribeAsync("unsub-channel", CancellationToken.None);
        await _sut.PublishAsync("unsub-channel", "after-unsub", CancellationToken.None);

        // Assert
        received.Should().BeEmpty();
    }

    #endregion

    #region UnsubscribePatternAsync Tests

    [Fact]
    public async Task UnsubscribePatternAsync_WithNullPattern_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("pattern", () =>
            _sut.UnsubscribePatternAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task UnsubscribePatternAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.UnsubscribePatternAsync("pattern*", cts.Token));
    }

    [Fact]
    public async Task UnsubscribePatternAsync_RemovesPatternSubscribers()
    {
        // Arrange
        var received = new List<(string Channel, string Message)>();
        await _sut.SubscribePatternAsync("cache:*", (channel, msg) =>
        {
            received.Add((channel, msg));
            return Task.CompletedTask;
        }, CancellationToken.None);

        // Act
        await _sut.UnsubscribePatternAsync("cache:*", CancellationToken.None);
        await _sut.PublishAsync("cache:invalidated", "Cache cleared", CancellationToken.None);

        // Assert
        received.Should().BeEmpty();
    }

    #endregion

    #region GetSubscriberCountAsync Tests

    [Fact]
    public async Task GetSubscriberCountAsync_WithNullChannel_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("channel", () =>
            _sut.GetSubscriberCountAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubscriberCountAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.GetSubscriberCountAsync("channel", cts.Token));
    }

    [Fact]
    public async Task GetSubscriberCountAsync_WithNoSubscribers_ReturnsZero()
    {
        // Act
        var count = await _sut.GetSubscriberCountAsync("no-subs", CancellationToken.None);

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public async Task GetSubscriberCountAsync_CountsChannelSubscribers()
    {
        // Arrange
        await _sut.SubscribeAsync("count-channel", _ => Task.CompletedTask, CancellationToken.None);
        await _sut.SubscribeAsync("count-channel", _ => Task.CompletedTask, CancellationToken.None);

        // Act
        var count = await _sut.GetSubscriberCountAsync("count-channel", CancellationToken.None);

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public async Task GetSubscriberCountAsync_CountsPatternSubscribers()
    {
        // Arrange
        await _sut.SubscribePatternAsync("events:*", (_, _) => Task.CompletedTask, CancellationToken.None);
        await _sut.SubscribeAsync("events:user", _ => Task.CompletedTask, CancellationToken.None);

        // Act
        var count = await _sut.GetSubscriberCountAsync("events:user", CancellationToken.None);

        // Assert
        count.Should().Be(2); // 1 channel + 1 pattern
    }

    #endregion

    private sealed record TestMessage(string Content);
}
