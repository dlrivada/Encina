using Bogus;
using Encina.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.UnitTests.Caching.Memory;

/// <summary>
/// Unit tests for <see cref="MemoryPubSubProvider"/>.
/// </summary>
public sealed class MemoryPubSubProviderTests
{
    private readonly MemoryPubSubProvider _sut;
    private readonly Faker _faker;

    public MemoryPubSubProviderTests()
    {
        _sut = new MemoryPubSubProvider(NullLogger<MemoryPubSubProvider>.Instance);
        _faker = new Faker();
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
        // Arrange
        var channel = _faker.Lorem.Word();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("message", () =>
            _sut.PublishAsync(channel, null!, CancellationToken.None));
    }

    [Fact]
    public async Task PublishAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var channel = _faker.Lorem.Word();
        var message = _faker.Lorem.Sentence();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.PublishAsync(channel, message, cts.Token));
    }

    [Fact]
    public async Task PublishAsync_WithNoSubscribers_DoesNotThrow()
    {
        // Arrange
        var channel = _faker.Lorem.Word();
        var message = _faker.Lorem.Sentence();

        // Act
        var exception = await Record.ExceptionAsync(() =>
            _sut.PublishAsync(channel, message, CancellationToken.None));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task PublishAsync_WithSubscriber_DeliveryMessageToSubscriber()
    {
        // Arrange
        var channel = _faker.Lorem.Word();
        var expectedMessage = _faker.Lorem.Sentence();
        string? receivedMessage = null;

        await _sut.SubscribeAsync(channel, msg =>
        {
            receivedMessage = msg;
            return Task.CompletedTask;
        }, CancellationToken.None);

        // Act
        await _sut.PublishAsync(channel, expectedMessage, CancellationToken.None);

        // Assert
        receivedMessage.ShouldBe(expectedMessage);
    }

    [Fact]
    public async Task PublishAsync_WithMultipleSubscribers_DeliversToAll()
    {
        // Arrange
        var channel = _faker.Lorem.Word();
        var expectedMessage = _faker.Lorem.Sentence();
        var receivedMessages = new List<string>();

        await _sut.SubscribeAsync(channel, msg =>
        {
            receivedMessages.Add($"1:{msg}");
            return Task.CompletedTask;
        }, CancellationToken.None);

        await _sut.SubscribeAsync(channel, msg =>
        {
            receivedMessages.Add($"2:{msg}");
            return Task.CompletedTask;
        }, CancellationToken.None);

        // Act
        await _sut.PublishAsync(channel, expectedMessage, CancellationToken.None);

        // Assert
        receivedMessages.Count.ShouldBe(2);
        receivedMessages.ShouldContain($"1:{expectedMessage}");
        receivedMessages.ShouldContain($"2:{expectedMessage}");
    }

    #endregion

    #region PublishAsync<T> Tests

    [Fact]
    public async Task PublishAsyncT_WithNullChannel_ThrowsArgumentNullException()
    {
        // Arrange
        var message = new TestMessage(_faker.Random.Guid(), _faker.Lorem.Sentence());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("channel", () =>
            _sut.PublishAsync(null!, message, CancellationToken.None));
    }

    [Fact]
    public async Task PublishAsyncT_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var channel = _faker.Lorem.Word();
        var message = new TestMessage(_faker.Random.Guid(), _faker.Lorem.Sentence());
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.PublishAsync(channel, message, cts.Token));
    }

    [Fact]
    public async Task PublishAsyncT_WithSubscriber_SerializesAndDeliveryMessage()
    {
        // Arrange
        var channel = _faker.Lorem.Word();
        var expectedMessage = new TestMessage(_faker.Random.Guid(), _faker.Lorem.Sentence());
        string? receivedJson = null;

        await _sut.SubscribeAsync(channel, msg =>
        {
            receivedJson = msg;
            return Task.CompletedTask;
        }, CancellationToken.None);

        // Act
        await _sut.PublishAsync(channel, expectedMessage, CancellationToken.None);

        // Assert
        receivedJson.ShouldNotBeNull();
        receivedJson.ShouldContain(expectedMessage.Id.ToString());
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
        // Arrange
        var channel = _faker.Lorem.Word();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("handler", () =>
            _sut.SubscribeAsync(channel, null!, CancellationToken.None));
    }

    [Fact]
    public async Task SubscribeAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var channel = _faker.Lorem.Word();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.SubscribeAsync(channel, _ => Task.CompletedTask, cts.Token));
    }

    [Fact]
    public async Task SubscribeAsync_ReturnsDisposable()
    {
        // Arrange
        var channel = _faker.Lorem.Word();

        // Act
        var subscription = await _sut.SubscribeAsync(channel, _ => Task.CompletedTask, CancellationToken.None);

        // Assert
        subscription.ShouldNotBeNull();
    }

    [Fact]
    public async Task SubscribeAsync_WhenDisposed_StopsReceivingMessages()
    {
        // Arrange
        var channel = _faker.Lorem.Word();
        var receivedMessages = new List<string>();

        var subscription = await _sut.SubscribeAsync(channel, msg =>
        {
            receivedMessages.Add(msg);
            return Task.CompletedTask;
        }, CancellationToken.None);

        // Act
        await _sut.PublishAsync(channel, "first", CancellationToken.None);
        await subscription.DisposeAsync();
        await _sut.PublishAsync(channel, "second", CancellationToken.None);

        // Assert
        receivedMessages.Count.ShouldBe(1);
        receivedMessages.ShouldContain("first");
    }

    #endregion

    #region SubscribeAsync<T> Tests

    [Fact]
    public async Task SubscribeAsyncT_WithNullChannel_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("channel", () =>
            _sut.SubscribeAsync<TestMessage>(null!, _ => Task.CompletedTask, CancellationToken.None));
    }

    [Fact]
    public async Task SubscribeAsyncT_WithNullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var channel = _faker.Lorem.Word();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("handler", () =>
            _sut.SubscribeAsync<TestMessage>(channel, null!, CancellationToken.None));
    }

    [Fact]
    public async Task SubscribeAsyncT_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var channel = _faker.Lorem.Word();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.SubscribeAsync<TestMessage>(channel, _ => Task.CompletedTask, cts.Token));
    }

    [Fact]
    public async Task SubscribeAsyncT_DeserializesMessageCorrectly()
    {
        // Arrange
        var channel = _faker.Lorem.Word();
        var expectedMessage = new TestMessage(_faker.Random.Guid(), _faker.Lorem.Sentence());
        TestMessage? receivedMessage = null;

        await _sut.SubscribeAsync<TestMessage>(channel, msg =>
        {
            receivedMessage = msg;
            return Task.CompletedTask;
        }, CancellationToken.None);

        // Act
        await _sut.PublishAsync(channel, expectedMessage, CancellationToken.None);

        // Assert
        receivedMessage.ShouldNotBeNull();
        receivedMessage!.Id.ShouldBe(expectedMessage.Id);
        receivedMessage.Content.ShouldBe(expectedMessage.Content);
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
        // Arrange
        var pattern = "test:*";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("handler", () =>
            _sut.SubscribePatternAsync(pattern, null!, CancellationToken.None));
    }

    [Fact]
    public async Task SubscribePatternAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var pattern = "test:*";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.SubscribePatternAsync(pattern, (_, _) => Task.CompletedTask, cts.Token));
    }

    [Fact]
    public async Task SubscribePatternAsync_WithWildcardPattern_ReceivesMatchingChannels()
    {
        // Arrange
        var receivedMessages = new List<(string Channel, string Message)>();

        await _sut.SubscribePatternAsync("test:*", (channel, message) =>
        {
            receivedMessages.Add((channel, message));
            return Task.CompletedTask;
        }, CancellationToken.None);

        // Act
        await _sut.PublishAsync("test:one", "message1", CancellationToken.None);
        await _sut.PublishAsync("test:two", "message2", CancellationToken.None);
        await _sut.PublishAsync("other:three", "message3", CancellationToken.None);

        // Assert
        receivedMessages.Count.ShouldBe(2);
        receivedMessages.ShouldContain(("test:one", "message1"));
        receivedMessages.ShouldContain(("test:two", "message2"));
    }

    [Fact]
    public async Task SubscribePatternAsync_WithSingleCharWildcard_ReceivesMatchingChannels()
    {
        // Arrange
        var receivedMessages = new List<(string Channel, string Message)>();

        await _sut.SubscribePatternAsync("test:?", (channel, message) =>
        {
            receivedMessages.Add((channel, message));
            return Task.CompletedTask;
        }, CancellationToken.None);

        // Act
        await _sut.PublishAsync("test:a", "message1", CancellationToken.None);
        await _sut.PublishAsync("test:b", "message2", CancellationToken.None);
        await _sut.PublishAsync("test:ab", "message3", CancellationToken.None);

        // Assert
        receivedMessages.Count.ShouldBe(2);
        receivedMessages.ShouldContain(("test:a", "message1"));
        receivedMessages.ShouldContain(("test:b", "message2"));
    }

    [Fact]
    public async Task SubscribePatternAsync_WhenDisposed_StopsReceivingMessages()
    {
        // Arrange
        var receivedMessages = new List<string>();

        var subscription = await _sut.SubscribePatternAsync("test:*", (_, message) =>
        {
            receivedMessages.Add(message);
            return Task.CompletedTask;
        }, CancellationToken.None);

        // Act
        await _sut.PublishAsync("test:one", "first", CancellationToken.None);
        await subscription.DisposeAsync();
        await _sut.PublishAsync("test:two", "second", CancellationToken.None);

        // Assert
        receivedMessages.Count.ShouldBe(1);
        receivedMessages.ShouldContain("first");
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
        var channel = _faker.Lorem.Word();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.UnsubscribeAsync(channel, cts.Token));
    }

    [Fact]
    public async Task UnsubscribeAsync_RemovesAllSubscribersFromChannel()
    {
        // Arrange
        var channel = _faker.Lorem.Word();
        var receivedMessages = new List<string>();

        await _sut.SubscribeAsync(channel, msg =>
        {
            receivedMessages.Add(msg);
            return Task.CompletedTask;
        }, CancellationToken.None);

        // Act
        await _sut.PublishAsync(channel, "first", CancellationToken.None);
        await _sut.UnsubscribeAsync(channel, CancellationToken.None);
        await _sut.PublishAsync(channel, "second", CancellationToken.None);

        // Assert
        receivedMessages.Count.ShouldBe(1);
        receivedMessages.ShouldContain("first");
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
        var pattern = "test:*";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.UnsubscribePatternAsync(pattern, cts.Token));
    }

    [Fact]
    public async Task UnsubscribePatternAsync_RemovesAllSubscribersFromPattern()
    {
        // Arrange
        var receivedMessages = new List<string>();

        await _sut.SubscribePatternAsync("test:*", (_, message) =>
        {
            receivedMessages.Add(message);
            return Task.CompletedTask;
        }, CancellationToken.None);

        // Act
        await _sut.PublishAsync("test:one", "first", CancellationToken.None);
        await _sut.UnsubscribePatternAsync("test:*", CancellationToken.None);
        await _sut.PublishAsync("test:two", "second", CancellationToken.None);

        // Assert
        receivedMessages.Count.ShouldBe(1);
        receivedMessages.ShouldContain("first");
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
        var channel = _faker.Lorem.Word();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.GetSubscriberCountAsync(channel, cts.Token));
    }

    [Fact]
    public async Task GetSubscriberCountAsync_WithNoSubscribers_ReturnsZero()
    {
        // Arrange
        var channel = _faker.Lorem.Word();

        // Act
        var count = await _sut.GetSubscriberCountAsync(channel, CancellationToken.None);

        // Assert
        count.ShouldBe(0);
    }

    [Fact]
    public async Task GetSubscriberCountAsync_WithChannelSubscribers_ReturnsCount()
    {
        // Arrange
        var channel = _faker.Lorem.Word();
        await _sut.SubscribeAsync(channel, _ => Task.CompletedTask, CancellationToken.None);
        await _sut.SubscribeAsync(channel, _ => Task.CompletedTask, CancellationToken.None);

        // Act
        var count = await _sut.GetSubscriberCountAsync(channel, CancellationToken.None);

        // Assert
        count.ShouldBe(2);
    }

    [Fact]
    public async Task GetSubscriberCountAsync_IncludesPatternSubscribers()
    {
        // Arrange
        await _sut.SubscribeAsync("test:specific", _ => Task.CompletedTask, CancellationToken.None);
        await _sut.SubscribePatternAsync("test:*", (_, _) => Task.CompletedTask, CancellationToken.None);

        // Act
        var count = await _sut.GetSubscriberCountAsync("test:specific", CancellationToken.None);

        // Assert
        count.ShouldBe(2);
    }

    #endregion

    private sealed record TestMessage(Guid Id, string Content);
}
