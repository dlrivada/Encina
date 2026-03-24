using Encina.Redis.PubSub;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using StackExchange.Redis;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Messaging.RedisPubSub;

/// <summary>
/// Tests for the <see cref="RedisPubSubMessagePublisher"/> class.
/// </summary>
public sealed class RedisPubSubMessagePublisherTests
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ISubscriber _subscriber;
    private readonly IOptions<EncinaRedisPubSubOptions> _options;
    private readonly ILogger<RedisPubSubMessagePublisher> _logger;

    public RedisPubSubMessagePublisherTests()
    {
        _redis = Substitute.For<IConnectionMultiplexer>();
        _subscriber = Substitute.For<ISubscriber>();
        _redis.GetSubscriber(Arg.Any<object?>()).Returns(_subscriber);

        _options = Options.Create(new EncinaRedisPubSubOptions
        {
            ChannelPrefix = "test",
            EventChannel = "events",
            CommandChannel = "commands"
        });

        _logger = NullLogger<RedisPubSubMessagePublisher>.Instance;
    }

    private RedisPubSubMessagePublisher CreateSut(TimeProvider? timeProvider = null)
        => new(_redis, _logger, _options, timeProvider);

    #region Constructor

    [Fact]
    public void Constructor_NullRedis_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new RedisPubSubMessagePublisher(null!, _logger, _options));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new RedisPubSubMessagePublisher(_redis, null!, _options));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new RedisPubSubMessagePublisher(_redis, _logger, null!));
    }

    [Fact]
    public void Constructor_NullTimeProvider_UsesSystemTimeProvider()
    {
        // Act — should not throw
        var sut = new RedisPubSubMessagePublisher(_redis, _logger, _options, null);

        // Assert — publisher was created successfully
        sut.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithTimeProvider_UsesProvidedTimeProvider()
    {
        // Arrange
        var timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(new DateTimeOffset(2026, 3, 24, 12, 0, 0, TimeSpan.Zero));

        // Act — should not throw
        var sut = new RedisPubSubMessagePublisher(_redis, _logger, _options, timeProvider);

        sut.ShouldNotBeNull();
    }

    #endregion

    #region PublishAsync

    [Fact]
    public async Task PublishAsync_NullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await sut.PublishAsync<TestMessage>(null!));
    }

    [Fact]
    public async Task PublishAsync_ValidMessage_ReturnsRightWithSubscriberCount()
    {
        // Arrange
        _subscriber
            .PublishAsync(Arg.Any<RedisChannel>(), Arg.Any<RedisValue>(), Arg.Any<CommandFlags>())
            .Returns(3L);

        var sut = CreateSut();
        var message = new TestMessage { Content = "Hello" };

        // Act
        var result = await sut.PublishAsync(message);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: count => count.ShouldBe(3L),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task PublishAsync_WithoutChannel_UsesDefaultChannel()
    {
        // Arrange
        RedisChannel capturedChannel = default;
        _subscriber
            .PublishAsync(Arg.Do<RedisChannel>(c => capturedChannel = c), Arg.Any<RedisValue>(), Arg.Any<CommandFlags>())
            .Returns(1L);

        var sut = CreateSut();

        // Act
        await sut.PublishAsync(new TestMessage { Content = "Hello" });

        // Assert
        capturedChannel.ToString().ShouldBe("test:events");
    }

    [Fact]
    public async Task PublishAsync_WithCustomChannel_UsesProvidedChannel()
    {
        // Arrange
        RedisChannel capturedChannel = default;
        _subscriber
            .PublishAsync(Arg.Do<RedisChannel>(c => capturedChannel = c), Arg.Any<RedisValue>(), Arg.Any<CommandFlags>())
            .Returns(1L);

        var sut = CreateSut();

        // Act
        await sut.PublishAsync(new TestMessage { Content = "Hello" }, channel: "my-custom-channel");

        // Assert
        capturedChannel.ToString().ShouldBe("my-custom-channel");
    }

    [Fact]
    public async Task PublishAsync_SerializesMessageWithWrapper()
    {
        // Arrange
        RedisValue capturedPayload = default;
        _subscriber
            .PublishAsync(Arg.Any<RedisChannel>(), Arg.Do<RedisValue>(v => capturedPayload = v), Arg.Any<CommandFlags>())
            .Returns(1L);

        var sut = CreateSut();
        var message = new TestMessage { Content = "Serialized" };

        // Act
        await sut.PublishAsync(message);

        // Assert
        var payloadStr = capturedPayload.ToString();
        payloadStr.ShouldContain("\"MessageType\":");
        payloadStr.ShouldContain("\"Payload\":");
        payloadStr.ShouldContain("\"TimestampUtc\":");
        payloadStr.ShouldContain("Serialized");
    }

    [Fact]
    public async Task PublishAsync_UsesTimeProviderForTimestamp()
    {
        // Arrange
        var fixedTime = new DateTimeOffset(2026, 3, 24, 15, 30, 0, TimeSpan.Zero);
        var timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(fixedTime);

        RedisValue capturedPayload = default;
        _subscriber
            .PublishAsync(Arg.Any<RedisChannel>(), Arg.Do<RedisValue>(v => capturedPayload = v), Arg.Any<CommandFlags>())
            .Returns(1L);

        var sut = CreateSut(timeProvider);

        // Act
        await sut.PublishAsync(new TestMessage { Content = "Timed" });

        // Assert
        var payloadStr = capturedPayload.ToString();
        payloadStr.ShouldContain("2026-03-24");
    }

    [Fact]
    public async Task PublishAsync_WhenRedisThrows_ReturnsLeftWithError()
    {
        // Arrange
        _subscriber
            .PublishAsync(Arg.Any<RedisChannel>(), Arg.Any<RedisValue>(), Arg.Any<CommandFlags>())
            .Returns<long>(x => throw new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Connection refused"));

        var sut = CreateSut();

        // Act
        var result = await sut.PublishAsync(new TestMessage { Content = "Fail" });

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error =>
            {
                error.Message.ShouldContain("Failed to publish message");
                error.Message.ShouldContain(nameof(TestMessage));
            });
    }

    [Fact]
    public async Task PublishAsync_WhenGenericExceptionThrown_ReturnsLeftWithError()
    {
        // Arrange
        _subscriber
            .PublishAsync(Arg.Any<RedisChannel>(), Arg.Any<RedisValue>(), Arg.Any<CommandFlags>())
            .Returns<long>(x => throw new InvalidOperationException("Something went wrong"));

        var sut = CreateSut();

        // Act
        var result = await sut.PublishAsync(new TestMessage { Content = "Fail" });

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.Message.ShouldContain("Failed to publish message"));
    }

    [Fact]
    public async Task PublishAsync_WithZeroSubscribers_ReturnsRightWithZero()
    {
        // Arrange
        _subscriber
            .PublishAsync(Arg.Any<RedisChannel>(), Arg.Any<RedisValue>(), Arg.Any<CommandFlags>())
            .Returns(0L);

        var sut = CreateSut();

        // Act
        var result = await sut.PublishAsync(new TestMessage { Content = "No listeners" });

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: count => count.ShouldBe(0L),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task PublishAsync_ErrorMessage_IncludesChannelName()
    {
        // Arrange
        _subscriber
            .PublishAsync(Arg.Any<RedisChannel>(), Arg.Any<RedisValue>(), Arg.Any<CommandFlags>())
            .Returns<long>(x => throw new RedisTimeoutException("Timeout", CommandStatus.Unknown));

        var sut = CreateSut();

        // Act
        var result = await sut.PublishAsync(new TestMessage { Content = "Timeout" }, channel: "my-channel");

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.Message.ShouldContain("my-channel"));
    }

    #endregion

    #region PublishAsync — Logging

    [Fact]
    public async Task PublishAsync_Success_LogsPublishingAndSuccess()
    {
        // Arrange
        var fakeLogger = new FakeLogger<RedisPubSubMessagePublisher>();
        _subscriber
            .PublishAsync(Arg.Any<RedisChannel>(), Arg.Any<RedisValue>(), Arg.Any<CommandFlags>())
            .Returns(2L);

        var sut = new RedisPubSubMessagePublisher(_redis, fakeLogger, _options);

        // Act
        await sut.PublishAsync(new TestMessage { Content = "Logged" });

        // Assert — should log at least the publishing and success messages
        fakeLogger.Collector.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task PublishAsync_Failure_LogsError()
    {
        // Arrange
        var fakeLogger = new FakeLogger<RedisPubSubMessagePublisher>();
        _subscriber
            .PublishAsync(Arg.Any<RedisChannel>(), Arg.Any<RedisValue>(), Arg.Any<CommandFlags>())
            .Returns<long>(x => throw new InvalidOperationException("Boom"));

        var sut = new RedisPubSubMessagePublisher(_redis, fakeLogger, _options);

        // Act
        await sut.PublishAsync(new TestMessage { Content = "Logged Fail" });

        // Assert — should log the publishing attempt and the error
        fakeLogger.Collector.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    #endregion

    #region PublishAsync — Channel Prefix Scenarios

    [Fact]
    public async Task PublishAsync_DefaultChannel_CombinesPrefixAndEventChannel()
    {
        // Arrange
        var customOptions = Options.Create(new EncinaRedisPubSubOptions
        {
            ChannelPrefix = "myapp",
            EventChannel = "domain-events"
        });

        var redis = Substitute.For<IConnectionMultiplexer>();
        var subscriber = Substitute.For<ISubscriber>();
        redis.GetSubscriber(Arg.Any<object?>()).Returns(subscriber);

        RedisChannel capturedChannel = default;
        subscriber
            .PublishAsync(Arg.Do<RedisChannel>(c => capturedChannel = c), Arg.Any<RedisValue>(), Arg.Any<CommandFlags>())
            .Returns(1L);

        var sut = new RedisPubSubMessagePublisher(redis, _logger, customOptions);

        // Act
        await sut.PublishAsync(new TestMessage { Content = "Prefixed" });

        // Assert
        capturedChannel.ToString().ShouldBe("myapp:domain-events");
    }

    #endregion

    #region SubscribeAsync

    [Fact]
    public async Task SubscribeAsync_NullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await sut.SubscribeAsync<TestMessage>(null!));
    }

    #endregion

    #region SubscribePatternAsync

    [Fact]
    public async Task SubscribePatternAsync_NullPattern_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await sut.SubscribePatternAsync<TestMessage>(null!, (channel, msg) => ValueTask.CompletedTask));
    }

    [Fact]
    public async Task SubscribePatternAsync_NullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await sut.SubscribePatternAsync<TestMessage>("events.*", null!));
    }

    #endregion

    #region MessageType in Wrapper

    [Fact]
    public async Task PublishAsync_IncludesFullTypeNameInMessageType()
    {
        // Arrange
        RedisValue capturedPayload = default;
        _subscriber
            .PublishAsync(Arg.Any<RedisChannel>(), Arg.Do<RedisValue>(v => capturedPayload = v), Arg.Any<CommandFlags>())
            .Returns(1L);

        var sut = CreateSut();

        // Act
        await sut.PublishAsync(new TestMessage { Content = "TypeInfo" });

        // Assert
        var payloadStr = capturedPayload.ToString();
        // JSON escapes '+' in nested class names as \u002B
        var expectedTypeName = typeof(TestMessage).FullName!.Replace("+", "\\u002B");
        payloadStr.ShouldContain(expectedTypeName);
    }

    #endregion

    #region Test Message

    /// <summary>
    /// A simple message class used for testing.
    /// </summary>
    private sealed class TestMessage
    {
        public string Content { get; set; } = string.Empty;
    }

    #endregion
}
