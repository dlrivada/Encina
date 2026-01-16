using Confluent.Kafka;
using Encina.Kafka;
using Encina.Testing;

namespace Encina.UnitTests.Kafka.Publishing;

/// <summary>
/// Unit tests for <see cref="KafkaMessagePublisher"/> using EitherAssertions.
/// </summary>
public sealed class KafkaMessagePublisherTests
{
    private readonly IProducer<string, byte[]> _producer;
    private readonly ILogger<KafkaMessagePublisher> _logger;
    private readonly IOptions<EncinaKafkaOptions> _options;

    public KafkaMessagePublisherTests()
    {
        _producer = Substitute.For<IProducer<string, byte[]>>();
        _logger = Substitute.For<ILogger<KafkaMessagePublisher>>();
        // Enable logging for coverage of LoggerMessage generated code
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _options = Options.Create(new EncinaKafkaOptions
        {
            DefaultEventTopic = "test-events",
            DefaultCommandTopic = "test-commands"
        });
    }

    #region Constructor Guard Clause Tests

    [Fact]
    public void Constructor_WithNullProducer_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new KafkaMessagePublisher(null!, _logger, _options));
        ex.ParamName.ShouldBe("producer");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new KafkaMessagePublisher(_producer, null!, _options));
        ex.ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new KafkaMessagePublisher(_producer, _logger, null!));
        ex.ParamName.ShouldBe("options");
    }

    #endregion

    #region ProduceAsync Tests

    [Fact]
    public async Task ProduceAsync_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var publisher = CreatePublisher();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            publisher.ProduceAsync<TestMessage>(null!).AsTask());
    }

    [Fact]
    public async Task ProduceAsync_WithValidMessage_ShouldBeSuccess()
    {
        // Arrange
        ConfigureProducerSuccess("test-events", 0, 1);
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.ProduceAsync(message);

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task ProduceAsync_WithValidMessage_ShouldReturnDeliveryResult()
    {
        // Arrange
        ConfigureProducerSuccess("test-events", 2, 100);
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.ProduceAsync(message);

        // Assert
        result.ShouldBeSuccessAnd()
            .ShouldSatisfy(dr =>
            {
                dr.Topic.ShouldBe("test-events");
                dr.Partition.ShouldBe(2);
                dr.Offset.ShouldBe(100);
            });
    }

    [Fact]
    public async Task ProduceAsync_WithCustomTopic_ShouldUseCustomTopic()
    {
        // Arrange
        var customTopic = "custom-topic";
        ConfigureProducerSuccess(customTopic, 0, 1);
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.ProduceAsync(message, customTopic);

        // Assert
        result.ShouldBeSuccessAnd()
            .ShouldSatisfy(dr => dr.Topic.ShouldBe(customTopic));
    }

    [Fact]
    public async Task ProduceAsync_WithCustomKey_ShouldBeSuccess()
    {
        // Arrange
        ConfigureProducerSuccess("test-events", 0, 1);
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };
        var key = "custom-key-123";

        // Act
        var result = await publisher.ProduceAsync(message, key: key);

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task ProduceAsync_WhenProducerThrowsKafkaException_ShouldBeError()
    {
        // Arrange
        ConfigureProducerToThrow(new KafkaException(new Error(ErrorCode.BrokerNotAvailable, "Broker not available")));
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.ProduceAsync(message);

        // Assert
        result.ShouldBeError();
    }

    [Fact]
    public async Task ProduceAsync_WhenProducerThrowsException_ShouldReturnErrorWithCode()
    {
        // Arrange
        ConfigureProducerToThrow(new InvalidOperationException("Connection lost"));
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.ProduceAsync(message);

        // Assert
        result.ShouldBeErrorWithCode("KAFKA_PRODUCE_FAILED");
    }

    [Fact]
    public async Task ProduceAsync_WhenProducerThrowsException_ShouldContainTopicInErrorMessage()
    {
        // Arrange
        ConfigureProducerToThrow(new InvalidOperationException("Connection lost"));
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.ProduceAsync(message);

        // Assert
        result.ShouldBeErrorContaining("test-events");
    }

    #endregion

    #region ProduceBatchAsync Tests

    [Fact]
    public async Task ProduceBatchAsync_WithNullMessages_ThrowsArgumentNullException()
    {
        // Arrange
        var publisher = CreatePublisher();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            publisher.ProduceBatchAsync<TestMessage>(null!).AsTask());
    }

    [Fact]
    public async Task ProduceBatchAsync_WithValidMessages_ShouldBeSuccess()
    {
        // Arrange
        ConfigureProducerSuccess("test-events", 0, 1);
        var publisher = CreatePublisher();
        var messages = new[]
        {
            (new TestMessage { Id = 1, Content = "Test1" }, (string?)null),
            (new TestMessage { Id = 2, Content = "Test2" }, (string?)"key-2")
        };

        // Act
        var result = await publisher.ProduceBatchAsync(messages);

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task ProduceBatchAsync_WithValidMessages_ShouldReturnAllResults()
    {
        // Arrange
        ConfigureProducerSuccess("test-events", 0, 1);
        var publisher = CreatePublisher();
        var messages = new[]
        {
            (new TestMessage { Id = 1, Content = "Test1" }, (string?)null),
            (new TestMessage { Id = 2, Content = "Test2" }, (string?)"key-2"),
            (new TestMessage { Id = 3, Content = "Test3" }, (string?)"key-3")
        };

        // Act
        var result = await publisher.ProduceBatchAsync(messages);

        // Assert
        result.ShouldBeSuccessAnd()
            .ShouldSatisfy(results => results.Count.ShouldBe(3));
    }

    [Fact]
    public async Task ProduceBatchAsync_WhenProducerThrowsException_ShouldBeError()
    {
        // Arrange
        ConfigureProducerToThrow(new InvalidOperationException("Connection lost"));
        var publisher = CreatePublisher();
        var messages = new[]
        {
            (new TestMessage { Id = 1, Content = "Test1" }, (string?)null)
        };

        // Act
        var result = await publisher.ProduceBatchAsync(messages);

        // Assert
        result.ShouldBeErrorWithCode("KAFKA_PRODUCE_FAILED");
    }

    #endregion

    #region ProduceWithHeadersAsync Tests

    [Fact]
    public async Task ProduceWithHeadersAsync_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var publisher = CreatePublisher();
        var headers = new Dictionary<string, byte[]>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            publisher.ProduceWithHeadersAsync<TestMessage>(null!, headers).AsTask());
    }

    [Fact]
    public async Task ProduceWithHeadersAsync_WithNullHeaders_ThrowsArgumentNullException()
    {
        // Arrange
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            publisher.ProduceWithHeadersAsync(message, null!).AsTask());
    }

    [Fact]
    public async Task ProduceWithHeadersAsync_WithValidMessageAndHeaders_ShouldBeSuccess()
    {
        // Arrange
        ConfigureProducerSuccess("test-events", 0, 1);
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };
        var headers = new Dictionary<string, byte[]>
        {
            { "CorrelationId", System.Text.Encoding.UTF8.GetBytes("corr-123") }
        };

        // Act
        var result = await publisher.ProduceWithHeadersAsync(message, headers);

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task ProduceWithHeadersAsync_WithMultipleHeaders_ShouldBeSuccess()
    {
        // Arrange
        ConfigureProducerSuccess("test-events", 1, 50);
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };
        var headers = new Dictionary<string, byte[]>
        {
            { "CorrelationId", System.Text.Encoding.UTF8.GetBytes("corr-123") },
            { "Source", System.Text.Encoding.UTF8.GetBytes("test-service") },
            { "Version", System.Text.Encoding.UTF8.GetBytes("1.0") }
        };

        // Act
        var result = await publisher.ProduceWithHeadersAsync(message, headers);

        // Assert
        result.ShouldBeSuccessAnd()
            .ShouldSatisfy(dr =>
            {
                dr.Partition.ShouldBe(1);
                dr.Offset.ShouldBe(50);
            });
    }

    [Fact]
    public async Task ProduceWithHeadersAsync_WhenProducerThrowsException_ShouldBeError()
    {
        // Arrange
        ConfigureProducerToThrow(new InvalidOperationException("Connection lost"));
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };
        var headers = new Dictionary<string, byte[]>();

        // Act
        var result = await publisher.ProduceWithHeadersAsync(message, headers);

        // Assert
        result.ShouldBeErrorWithCode("KAFKA_PRODUCE_HEADERS_FAILED");
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_ShouldFlushAndDisposeProducer()
    {
        // Arrange
        var publisher = CreatePublisher();

        // Act
        publisher.Dispose();

        // Assert
        _producer.Received(1).Flush(Arg.Any<TimeSpan>());
        _producer.Received(1).Dispose();
    }

    #endregion

    #region Helper Methods

    private KafkaMessagePublisher CreatePublisher()
    {
        return new KafkaMessagePublisher(_producer, _logger, _options);
    }

    private void ConfigureProducerSuccess(string topic, int partition, long offset)
    {
        // Create DeliveryResult without Timestamp (it's read-only and can cause issues)
        // The publisher implementation accesses Timestamp.UtcDateTime which will work
        _producer
            .ProduceAsync(
                Arg.Any<string>(),
                Arg.Any<Message<string, byte[]>>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var result = new DeliveryResult<string, byte[]>
                {
                    Topic = topic,
                    Partition = new Partition(partition),
                    Offset = new Offset(offset),
                    Message = callInfo.Arg<Message<string, byte[]>>()
                };
                return Task.FromResult(result);
            });
    }

    private void ConfigureProducerToThrow(Exception exception)
    {
        _producer
            .ProduceAsync(
                Arg.Any<string>(),
                Arg.Any<Message<string, byte[]>>(),
                Arg.Any<CancellationToken>())
            .Returns<DeliveryResult<string, byte[]>>(_ => throw exception);
    }

    #endregion

    #region Test Types

    private sealed record TestMessage
    {
        public int Id { get; init; }
        public string Content { get; init; } = string.Empty;
    }

    #endregion
}
