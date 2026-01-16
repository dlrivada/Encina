using Encina.RabbitMQ;
using Encina.Testing;
using LanguageExt;
using RabbitMQ.Client;

namespace Encina.UnitTests.RabbitMQ.Publishing;

/// <summary>
/// Unit tests for <see cref="RabbitMQMessagePublisher"/> using EitherAssertions.
/// </summary>
public sealed class RabbitMQMessagePublisherTests
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly ILogger<RabbitMQMessagePublisher> _logger;
    private readonly IOptions<EncinaRabbitMQOptions> _options;

    public RabbitMQMessagePublisherTests()
    {
        _connection = Substitute.For<IConnection>();
        _channel = Substitute.For<IChannel>();
        _logger = Substitute.For<ILogger<RabbitMQMessagePublisher>>();
        // Enable logging for coverage of LoggerMessage generated code
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _options = Options.Create(new EncinaRabbitMQOptions
        {
            ExchangeName = "test-exchange",
            Durable = true
        });
    }

    #region Constructor Guard Clause Tests

    [Fact]
    public void Constructor_WithNullConnection_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new RabbitMQMessagePublisher(null!, _channel, _logger, _options));
        ex.ParamName.ShouldBe("connection");
    }

    [Fact]
    public void Constructor_WithNullChannel_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new RabbitMQMessagePublisher(_connection, null!, _logger, _options));
        ex.ParamName.ShouldBe("channel");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new RabbitMQMessagePublisher(_connection, _channel, null!, _options));
        ex.ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new RabbitMQMessagePublisher(_connection, _channel, _logger, null!));
        ex.ParamName.ShouldBe("options");
    }

    #endregion

    #region PublishAsync Tests

    [Fact]
    public async Task PublishAsync_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var publisher = CreatePublisher();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            publisher.PublishAsync<TestMessage>(null!).AsTask());
    }

    [Fact]
    public async Task PublishAsync_WithValidMessage_ShouldBeSuccess()
    {
        // Arrange
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.PublishAsync(message);

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task PublishAsync_WithValidMessage_ShouldReturnUnit()
    {
        // Arrange
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.PublishAsync(message);

        // Assert
        var value = result.ShouldBeSuccess();
        value.ShouldBe(Unit.Default);
    }

    [Fact]
    public async Task PublishAsync_WithCustomRoutingKey_ShouldBeSuccess()
    {
        // Arrange
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };
        var routingKey = "custom.routing.key";

        // Act
        var result = await publisher.PublishAsync(message, routingKey);

        // Assert
        result.ShouldBeSuccessAnd()
            .ShouldSatisfy(u => u.ShouldBe(Unit.Default));
    }

    [Fact]
    public async Task PublishAsync_WhenChannelThrowsException_ShouldBeError()
    {
        // Arrange
        ConfigureChannelToThrow(new InvalidOperationException("Channel closed"));
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.PublishAsync(message);

        // Assert
        result.ShouldBeError();
    }

    [Fact]
    public async Task PublishAsync_WhenChannelThrowsException_ShouldReturnErrorWithCode()
    {
        // Arrange
        ConfigureChannelToThrow(new InvalidOperationException("Channel closed"));
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.PublishAsync(message);

        // Assert
        result.ShouldBeErrorWithCode("RABBITMQ_PUBLISH_FAILED");
    }

    [Fact]
    public async Task PublishAsync_WhenChannelThrowsException_ShouldContainErrorMessage()
    {
        // Arrange
        ConfigureChannelToThrow(new InvalidOperationException("Channel closed"));
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.PublishAsync(message);

        // Assert
        result.ShouldBeErrorContaining("Failed to publish message");
    }

    #endregion

    #region SendToQueueAsync Tests

    [Fact]
    public async Task SendToQueueAsync_WithNullQueueName_ThrowsArgumentNullException()
    {
        // Arrange
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            publisher.SendToQueueAsync(null!, message).AsTask());
    }

    [Fact]
    public async Task SendToQueueAsync_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var publisher = CreatePublisher();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            publisher.SendToQueueAsync<TestMessage>("queue", null!).AsTask());
    }

    [Fact]
    public async Task SendToQueueAsync_WithValidMessage_ShouldBeSuccess()
    {
        // Arrange
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };
        var queueName = "test-queue";

        // Act
        var result = await publisher.SendToQueueAsync(queueName, message);

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task SendToQueueAsync_WithValidMessage_ShouldReturnUnit()
    {
        // Arrange
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };
        var queueName = "test-queue";

        // Act
        var result = await publisher.SendToQueueAsync(queueName, message);

        // Assert
        result.ShouldBeSuccessAnd()
            .ShouldSatisfy(u => u.ShouldBe(Unit.Default));
    }

    [Fact]
    public async Task SendToQueueAsync_WhenChannelThrowsException_ShouldBeError()
    {
        // Arrange
        ConfigureChannelToThrow(new InvalidOperationException("Queue not found"));
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };
        var queueName = "test-queue";

        // Act
        var result = await publisher.SendToQueueAsync(queueName, message);

        // Assert
        result.ShouldBeErrorWithCode("RABBITMQ_SEND_FAILED");
    }

    [Fact]
    public async Task SendToQueueAsync_WhenChannelThrowsException_ShouldContainQueueName()
    {
        // Arrange
        ConfigureChannelToThrow(new InvalidOperationException("Queue not found"));
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };
        var queueName = "test-queue";

        // Act
        var result = await publisher.SendToQueueAsync(queueName, message);

        // Assert
        result.ShouldBeErrorContaining(queueName);
    }

    #endregion

    #region RequestAsync Tests

    [Fact]
    public async Task RequestAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var publisher = CreatePublisher();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            publisher.RequestAsync<TestRequest, TestResponse>(null!).AsTask());
    }

    [Fact]
    public async Task RequestAsync_ShouldReturnNotImplementedError()
    {
        // Arrange
        var publisher = CreatePublisher();
        var request = new TestRequest { Query = "test" };

        // Act
        var result = await publisher.RequestAsync<TestRequest, TestResponse>(request);

        // Assert
        result.ShouldBeErrorWithCode("RABBITMQ_RPC_NOT_IMPLEMENTED");
    }

    [Fact]
    public async Task RequestAsync_ShouldContainMassTransitSuggestion()
    {
        // Arrange
        var publisher = CreatePublisher();
        var request = new TestRequest { Query = "test" };

        // Act
        var result = await publisher.RequestAsync<TestRequest, TestResponse>(request);

        // Assert
        result.ShouldBeErrorContaining("MassTransit");
    }

    #endregion

    #region DisposeAsync Tests

    [Fact]
    public async Task DisposeAsync_ShouldCloseChannelAndConnection()
    {
        // Arrange
        var publisher = CreatePublisher();

        // Act
        await publisher.DisposeAsync();

        // Assert
        await _channel.Received(1).CloseAsync(Arg.Any<CancellationToken>());
        await _connection.Received(1).CloseAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region PublishAsync with Durable=false Tests

    [Fact]
    public async Task PublishAsync_WithDurableFalse_ShouldBeSuccess()
    {
        // Arrange
        var options = Options.Create(new EncinaRabbitMQOptions
        {
            ExchangeName = "test-exchange",
            Durable = false
        });
        var publisher = new RabbitMQMessagePublisher(_connection, _channel, _logger, options);
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.PublishAsync(message);

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task SendToQueueAsync_WithDurableFalse_ShouldBeSuccess()
    {
        // Arrange
        var options = Options.Create(new EncinaRabbitMQOptions
        {
            ExchangeName = "test-exchange",
            Durable = false
        });
        var publisher = new RabbitMQMessagePublisher(_connection, _channel, _logger, options);
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.SendToQueueAsync("queue", message);

        // Assert
        result.ShouldBeSuccess();
    }

    #endregion

    #region Helper Methods

    private RabbitMQMessagePublisher CreatePublisher()
    {
        return new RabbitMQMessagePublisher(_connection, _channel, _logger, _options);
    }

    private void ConfigureChannelToThrow(Exception exception)
    {
#pragma warning disable CA2012 // Use ValueTasks correctly - false positive in NSubstitute When() context
        _channel
            .When(c => c.BasicPublishAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<bool>(),
                Arg.Any<BasicProperties>(),
                Arg.Any<ReadOnlyMemory<byte>>(),
                Arg.Any<CancellationToken>()))
            .Do(_ => throw exception);
#pragma warning restore CA2012
    }

    #endregion

    #region Test Types

    private sealed record TestMessage
    {
        public int Id { get; init; }
        public string Content { get; init; } = string.Empty;
    }

    private sealed record TestRequest
    {
        public string Query { get; init; } = string.Empty;
    }

    private sealed record TestResponse
    {
        public string Result { get; init; } = string.Empty;
    }

    #endregion
}
