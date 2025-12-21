namespace SimpleMediator.Caching.ContractTests;

/// <summary>
/// Contract tests that verify all IPubSubProvider implementations follow the same behavioral contract.
/// </summary>
public abstract class IPubSubProviderContractTests : IAsyncLifetime
{
    protected IPubSubProvider Provider { get; private set; } = null!;

    protected abstract IPubSubProvider CreateProvider();

    public Task InitializeAsync()
    {
        Provider = CreateProvider();
        return Task.CompletedTask;
    }

    public virtual Task DisposeAsync() => Task.CompletedTask;

    #region PublishAsync Contract

    [Fact]
    public async Task PublishAsync_WithNullChannel_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Provider.PublishAsync(null!, "message", CancellationToken.None));
    }

    [Fact]
    public async Task PublishAsync_WithNullMessage_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Provider.PublishAsync("channel", null!, CancellationToken.None));
    }

    [Fact]
    public async Task PublishAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            Provider.PublishAsync("channel", "message", cts.Token));
    }

    [Fact]
    public async Task PublishAsync_WithSubscriber_DeliversMessage()
    {
        // Arrange
        var channel = $"contract-pub-{Guid.NewGuid():N}";
        var receivedMessages = new List<string>();
        await Provider.SubscribeAsync(channel, msg =>
        {
            receivedMessages.Add(msg);
            return Task.CompletedTask;
        }, CancellationToken.None);

        // Act
        await Provider.PublishAsync(channel, "test-message", CancellationToken.None);

        // Assert
        receivedMessages.Should().ContainSingle().Which.Should().Be("test-message");
    }

    [Fact]
    public async Task PublishAsync_WithNoSubscribers_DoesNotThrow()
    {
        // Act & Assert
        await Provider.Invoking(p => p.PublishAsync("no-subscribers", "message", CancellationToken.None))
            .Should().NotThrowAsync();
    }

    #endregion

    #region PublishAsync<T> Contract

    [Fact]
    public async Task PublishAsyncTyped_WithNullChannel_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Provider.PublishAsync<TestMessage>(null!, new TestMessage("test"), CancellationToken.None));
    }

    [Fact]
    public async Task PublishAsyncTyped_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            Provider.PublishAsync("channel", new TestMessage("test"), cts.Token));
    }

    #endregion

    #region SubscribeAsync Contract

    [Fact]
    public async Task SubscribeAsync_WithNullChannel_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Provider.SubscribeAsync(null!, _ => Task.CompletedTask, CancellationToken.None));
    }

    [Fact]
    public async Task SubscribeAsync_WithNullHandler_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Provider.SubscribeAsync("channel", (Func<string, Task>)null!, CancellationToken.None));
    }

    [Fact]
    public async Task SubscribeAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            Provider.SubscribeAsync("channel", _ => Task.CompletedTask, cts.Token));
    }

    [Fact]
    public async Task SubscribeAsync_ReturnsDisposableSubscription()
    {
        // Act
        var subscription = await Provider.SubscribeAsync("test", _ => Task.CompletedTask, CancellationToken.None);

        // Assert
        subscription.Should().NotBeNull();
        subscription.Should().BeAssignableTo<IAsyncDisposable>();

        // Cleanup
        await subscription.DisposeAsync();
    }

    [Fact]
    public async Task SubscribeAsync_MultipleSubscribers_AllReceiveMessages()
    {
        // Arrange
        var channel = $"contract-multi-sub-{Guid.NewGuid():N}";
        var received1 = new List<string>();
        var received2 = new List<string>();

        await Provider.SubscribeAsync(channel, msg =>
        {
            received1.Add(msg);
            return Task.CompletedTask;
        }, CancellationToken.None);

        await Provider.SubscribeAsync(channel, msg =>
        {
            received2.Add(msg);
            return Task.CompletedTask;
        }, CancellationToken.None);

        // Act
        await Provider.PublishAsync(channel, "broadcast", CancellationToken.None);

        // Assert
        received1.Should().ContainSingle().Which.Should().Be("broadcast");
        received2.Should().ContainSingle().Which.Should().Be("broadcast");
    }

    [Fact]
    public async Task SubscribeAsync_DisposingSubscription_StopsReceivingMessages()
    {
        // Arrange
        var channel = $"contract-dispose-{Guid.NewGuid():N}";
        var received = new List<string>();
        var subscription = await Provider.SubscribeAsync(channel, msg =>
        {
            received.Add(msg);
            return Task.CompletedTask;
        }, CancellationToken.None);

        // Act
        await Provider.PublishAsync(channel, "before-dispose", CancellationToken.None);
        await subscription.DisposeAsync();
        await Provider.PublishAsync(channel, "after-dispose", CancellationToken.None);

        // Assert
        received.Should().ContainSingle().Which.Should().Be("before-dispose");
    }

    #endregion

    #region SubscribePatternAsync Contract

    [Fact]
    public async Task SubscribePatternAsync_WithNullPattern_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Provider.SubscribePatternAsync(null!, (_, _) => Task.CompletedTask, CancellationToken.None));
    }

    [Fact]
    public async Task SubscribePatternAsync_WithNullHandler_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Provider.SubscribePatternAsync("pattern*", null!, CancellationToken.None));
    }

    [Fact]
    public async Task SubscribePatternAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            Provider.SubscribePatternAsync("pattern*", (_, _) => Task.CompletedTask, cts.Token));
    }

    [Fact]
    public async Task SubscribePatternAsync_WithWildcard_ReceivesMatchingChannels()
    {
        // Arrange
        var prefix = $"pattern-{Guid.NewGuid():N}";
        var received = new List<(string Channel, string Message)>();
        await Provider.SubscribePatternAsync($"{prefix}:*", (channel, msg) =>
        {
            received.Add((channel, msg));
            return Task.CompletedTask;
        }, CancellationToken.None);

        // Act
        await Provider.PublishAsync($"{prefix}:a", "Message A", CancellationToken.None);
        await Provider.PublishAsync($"{prefix}:b", "Message B", CancellationToken.None);
        await Provider.PublishAsync("other:c", "Message C", CancellationToken.None);

        // Assert
        received.Should().HaveCount(2);
        received.Should().Contain(x => x.Channel == $"{prefix}:a" && x.Message == "Message A");
        received.Should().Contain(x => x.Channel == $"{prefix}:b" && x.Message == "Message B");
    }

    #endregion

    #region UnsubscribeAsync Contract

    [Fact]
    public async Task UnsubscribeAsync_WithNullChannel_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Provider.UnsubscribeAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task UnsubscribeAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            Provider.UnsubscribeAsync("channel", cts.Token));
    }

    [Fact]
    public async Task UnsubscribeAsync_RemovesAllSubscribers()
    {
        // Arrange
        var channel = $"contract-unsub-{Guid.NewGuid():N}";
        var received = new List<string>();
        await Provider.SubscribeAsync(channel, msg =>
        {
            received.Add(msg);
            return Task.CompletedTask;
        }, CancellationToken.None);

        // Act
        await Provider.UnsubscribeAsync(channel, CancellationToken.None);
        await Provider.PublishAsync(channel, "after-unsub", CancellationToken.None);

        // Assert
        received.Should().BeEmpty();
    }

    #endregion

    #region UnsubscribePatternAsync Contract

    [Fact]
    public async Task UnsubscribePatternAsync_WithNullPattern_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Provider.UnsubscribePatternAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task UnsubscribePatternAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            Provider.UnsubscribePatternAsync("pattern*", cts.Token));
    }

    #endregion

    #region GetSubscriberCountAsync Contract

    [Fact]
    public async Task GetSubscriberCountAsync_WithNullChannel_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Provider.GetSubscriberCountAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubscriberCountAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            Provider.GetSubscriberCountAsync("channel", cts.Token));
    }

    [Fact]
    public async Task GetSubscriberCountAsync_WithNoSubscribers_ReturnsZero()
    {
        // Act
        var count = await Provider.GetSubscriberCountAsync($"no-subs-{Guid.NewGuid():N}", CancellationToken.None);

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public async Task GetSubscriberCountAsync_CountsSubscribers()
    {
        // Arrange
        var channel = $"contract-count-{Guid.NewGuid():N}";
        await Provider.SubscribeAsync(channel, _ => Task.CompletedTask, CancellationToken.None);
        await Provider.SubscribeAsync(channel, _ => Task.CompletedTask, CancellationToken.None);

        // Act
        var count = await Provider.GetSubscriberCountAsync(channel, CancellationToken.None);

        // Assert
        count.Should().Be(2);
    }

    #endregion

    protected sealed record TestMessage(string Content);
}

/// <summary>
/// Contract tests for MemoryPubSubProvider.
/// </summary>
public sealed class MemoryPubSubProviderContractTests : IPubSubProviderContractTests
{
    protected override IPubSubProvider CreateProvider()
    {
        return new MemoryPubSubProvider(NullLogger<MemoryPubSubProvider>.Instance);
    }
}
