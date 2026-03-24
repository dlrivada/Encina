using Encina.Redis.PubSub;
using StackExchange.Redis;

namespace Encina.GuardTests.Messaging.RedisPubSub;

/// <summary>
/// Guard clause tests for <see cref="RedisPubSubMessagePublisher"/>.
/// Verifies that null parameters are properly guarded.
/// </summary>
public sealed class RedisPubSubMessagePublisherGuardTests
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisPubSubMessagePublisher> _logger = NullLogger<RedisPubSubMessagePublisher>.Instance;
    private readonly IOptions<EncinaRedisPubSubOptions> _options = Options.Create(new EncinaRedisPubSubOptions());

    public RedisPubSubMessagePublisherGuardTests()
    {
        _redis = Substitute.For<IConnectionMultiplexer>();
        _redis.GetSubscriber(Arg.Any<object>()).Returns(Substitute.For<ISubscriber>());
    }

    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when redis is null.
    /// </summary>
    [Fact]
    public void Constructor_NullRedis_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new RedisPubSubMessagePublisher(null!, _logger, _options);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("redis");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new RedisPubSubMessagePublisher(_redis, null!, _options);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when options is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new RedisPubSubMessagePublisher(_redis, _logger, null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    #endregion

    #region PublishAsync Guards

    /// <summary>
    /// Verifies that PublishAsync throws ArgumentNullException when message is null.
    /// </summary>
    [Fact]
    public async Task PublishAsync_NullMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        var publisher = new RedisPubSubMessagePublisher(_redis, _logger, _options);

        // Act
        var act = async () => await publisher.PublishAsync<TestMessage>(null!);

        // Assert
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    #endregion

    #region SubscribeAsync Guards

    /// <summary>
    /// Verifies that SubscribeAsync throws ArgumentNullException when handler is null.
    /// </summary>
    [Fact]
    public async Task SubscribeAsync_NullHandler_ShouldThrowArgumentNullException()
    {
        // Arrange
        var publisher = new RedisPubSubMessagePublisher(_redis, _logger, _options);

        // Act
        var act = async () => await publisher.SubscribeAsync<TestMessage>(null!);

        // Assert
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    #endregion

    #region SubscribePatternAsync Guards

    /// <summary>
    /// Verifies that SubscribePatternAsync throws ArgumentNullException when pattern is null.
    /// </summary>
    [Fact]
    public async Task SubscribePatternAsync_NullPattern_ShouldThrowArgumentNullException()
    {
        // Arrange
        var publisher = new RedisPubSubMessagePublisher(_redis, _logger, _options);
        Func<string, TestMessage, ValueTask> handler = (_, _) => ValueTask.CompletedTask;

        // Act
        var act = async () => await publisher.SubscribePatternAsync(null!, handler);

        // Assert
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    /// <summary>
    /// Verifies that SubscribePatternAsync throws ArgumentNullException when handler is null.
    /// </summary>
    [Fact]
    public async Task SubscribePatternAsync_NullHandler_ShouldThrowArgumentNullException()
    {
        // Arrange
        var publisher = new RedisPubSubMessagePublisher(_redis, _logger, _options);

        // Act
        var act = async () => await publisher.SubscribePatternAsync<TestMessage>(
            "events.*", null!);

        // Assert
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    #endregion

    /// <summary>
    /// Test message type for guard tests.
    /// </summary>
    private sealed class TestMessage
    {
        public string Data { get; set; } = string.Empty;
    }
}
