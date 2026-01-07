using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;

namespace Encina.Caching.Redis.Tests;

/// <summary>
/// Unit tests for <see cref="RedisPubSubProvider"/>.
/// </summary>
public sealed class RedisPubSubProviderTests
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ISubscriber _subscriber;
    private readonly ILogger<RedisPubSubProvider> _logger;

    public RedisPubSubProviderTests()
    {
        _connectionMultiplexer = Substitute.For<IConnectionMultiplexer>();
        _subscriber = Substitute.For<ISubscriber>();
        _logger = NullLogger<RedisPubSubProvider>.Instance;

        _connectionMultiplexer.GetSubscriber(Arg.Any<object?>())
            .Returns(_subscriber);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullConnection_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new RedisPubSubProvider(null!, _logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new RedisPubSubProvider(_connectionMultiplexer, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act
        var provider = CreateProvider();

        // Assert
        provider.ShouldNotBeNull();
    }

    #endregion

    #region PublishAsync (string) Tests

    [Fact]
    public async Task PublishAsync_String_WithNullChannel_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            provider.PublishAsync(null!, "message", CancellationToken.None));
    }

    [Fact]
    public async Task PublishAsync_String_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            provider.PublishAsync("channel", null!, CancellationToken.None));
    }

    [Fact]
    public async Task PublishAsync_String_WithCancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var provider = CreateProvider();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            provider.PublishAsync("channel", "message", cts.Token));
    }

    [Fact]
    public async Task PublishAsync_String_WithValidParameters_PublishesMessage()
    {
        // Arrange
        var provider = CreateProvider();
        var channel = "test-channel";
        var message = "test-message";

        _subscriber.PublishAsync(Arg.Any<RedisChannel>(), Arg.Any<RedisValue>(), Arg.Any<CommandFlags>())
            .Returns(1L);

        // Act
        await provider.PublishAsync(channel, message, CancellationToken.None);

        // Assert - method completes without exception
    }

    #endregion

    #region PublishAsync<T> Tests

    [Fact]
    public async Task PublishAsync_Generic_WithNullChannel_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            provider.PublishAsync(null!, new TestMessage("data"), CancellationToken.None));
    }

    [Fact]
    public async Task PublishAsync_Generic_WithCancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var provider = CreateProvider();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            provider.PublishAsync("channel", new TestMessage("data"), cts.Token));
    }

    [Fact]
    public async Task PublishAsync_Generic_WithValidParameters_SerializesAndPublishes()
    {
        // Arrange
        var provider = CreateProvider();
        var channel = "test-channel";
        var message = new TestMessage("test-data");

        _subscriber.PublishAsync(Arg.Any<RedisChannel>(), Arg.Any<RedisValue>(), Arg.Any<CommandFlags>())
            .Returns(1L);

        // Act
        await provider.PublishAsync(channel, message, CancellationToken.None);

        // Assert - method completes without exception
    }

    #endregion

    #region SubscribeAsync (string handler) Tests

    [Fact]
    public async Task SubscribeAsync_String_WithNullChannel_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            provider.SubscribeAsync(null!, _ => Task.CompletedTask, CancellationToken.None));
    }

    [Fact]
    public async Task SubscribeAsync_String_WithNullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            provider.SubscribeAsync("channel", (Func<string, Task>)null!, CancellationToken.None));
    }

    [Fact]
    public async Task SubscribeAsync_String_WithCancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var provider = CreateProvider();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            provider.SubscribeAsync("channel", _ => Task.CompletedTask, cts.Token));
    }

    [Fact]
    public async Task SubscribeAsync_String_WithValidParameters_ReturnsSubscription()
    {
        // Arrange
        var provider = CreateProvider();
        var channel = "test-channel";

        // Act
        var subscription = await provider.SubscribeAsync(channel, _ => Task.CompletedTask, CancellationToken.None);

        // Assert
        subscription.ShouldNotBeNull();
    }

    #endregion

    #region SubscribeAsync<T> Tests

    [Fact]
    public async Task SubscribeAsync_Generic_WithNullChannel_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            provider.SubscribeAsync<TestMessage>(null!, _ => Task.CompletedTask, CancellationToken.None));
    }

    [Fact]
    public async Task SubscribeAsync_Generic_WithNullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            provider.SubscribeAsync("channel", (Func<TestMessage, Task>)null!, CancellationToken.None));
    }

    [Fact]
    public async Task SubscribeAsync_Generic_WithCancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var provider = CreateProvider();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            provider.SubscribeAsync<TestMessage>("channel", _ => Task.CompletedTask, cts.Token));
    }

    [Fact]
    public async Task SubscribeAsync_Generic_WithValidParameters_ReturnsSubscription()
    {
        // Arrange
        var provider = CreateProvider();
        var channel = "test-channel";

        // Act
        var subscription = await provider.SubscribeAsync<TestMessage>(
            channel,
            _ => Task.CompletedTask,
            CancellationToken.None);

        // Assert
        subscription.ShouldNotBeNull();
    }

    #endregion

    #region SubscribePatternAsync Tests

    [Fact]
    public async Task SubscribePatternAsync_WithNullPattern_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            provider.SubscribePatternAsync(null!, (_, _) => Task.CompletedTask, CancellationToken.None));
    }

    [Fact]
    public async Task SubscribePatternAsync_WithNullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            provider.SubscribePatternAsync("pattern:*", null!, CancellationToken.None));
    }

    [Fact]
    public async Task SubscribePatternAsync_WithCancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var provider = CreateProvider();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            provider.SubscribePatternAsync("pattern:*", (_, _) => Task.CompletedTask, cts.Token));
    }

    [Fact]
    public async Task SubscribePatternAsync_WithValidParameters_ReturnsSubscription()
    {
        // Arrange
        var provider = CreateProvider();
        var pattern = "test:*";

        // Act
        var subscription = await provider.SubscribePatternAsync(
            pattern,
            (_, _) => Task.CompletedTask,
            CancellationToken.None);

        // Assert
        subscription.ShouldNotBeNull();
    }

    #endregion

    #region UnsubscribeAsync Tests

    [Fact]
    public async Task UnsubscribeAsync_WithNullChannel_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            provider.UnsubscribeAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task UnsubscribeAsync_WithCancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var provider = CreateProvider();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            provider.UnsubscribeAsync("channel", cts.Token));
    }

    [Fact]
    public async Task UnsubscribeAsync_WithValidChannel_UnsubscribesSuccessfully()
    {
        // Arrange
        var provider = CreateProvider();
        var channel = "test-channel";

        // Act
        await provider.UnsubscribeAsync(channel, CancellationToken.None);

        // Assert - method completes without exception
    }

    #endregion

    #region UnsubscribePatternAsync Tests

    [Fact]
    public async Task UnsubscribePatternAsync_WithNullPattern_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            provider.UnsubscribePatternAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task UnsubscribePatternAsync_WithCancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var provider = CreateProvider();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            provider.UnsubscribePatternAsync("pattern:*", cts.Token));
    }

    [Fact]
    public async Task UnsubscribePatternAsync_WithValidPattern_UnsubscribesSuccessfully()
    {
        // Arrange
        var provider = CreateProvider();
        var pattern = "test:*";

        // Act
        await provider.UnsubscribePatternAsync(pattern, CancellationToken.None);

        // Assert - method completes without exception
    }

    #endregion

    #region GetSubscriberCountAsync Tests

    [Fact]
    public async Task GetSubscriberCountAsync_WithNullChannel_ThrowsArgumentNullException()
    {
        // Arrange
        var provider = CreateProvider();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            provider.GetSubscriberCountAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubscriberCountAsync_WithCancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var provider = CreateProvider();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            provider.GetSubscriberCountAsync("channel", cts.Token));
    }

    [Fact]
    public async Task GetSubscriberCountAsync_WithValidChannel_ReturnsCount()
    {
        // Arrange
        var provider = CreateProvider();
        var channel = "test-channel";

        _subscriber.PublishAsync(
            Arg.Any<RedisChannel>(),
            Arg.Any<RedisValue>(),
            CommandFlags.DemandMaster)
            .Returns(5L);

        // Act
        var count = await provider.GetSubscriberCountAsync(channel, CancellationToken.None);

        // Assert
        count.ShouldBe(5L);
    }

    #endregion

    #region Helper Methods

    private RedisPubSubProvider CreateProvider()
    {
        return new RedisPubSubProvider(_connectionMultiplexer, _logger);
    }

    #endregion

    #region Test Types

    private sealed record TestMessage(string Data);

    #endregion
}
