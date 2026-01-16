using Azure.Messaging.ServiceBus;
using Encina.AzureServiceBus;
using Encina.Testing.Shouldly;
using NSubstitute.ExceptionExtensions;

namespace Encina.UnitTests.AzureServiceBus.Publishing;

/// <summary>
/// Unit tests for <see cref="AzureServiceBusMessagePublisher"/> using EitherAssertions.
/// </summary>
public sealed class AzureServiceBusMessagePublisherTests
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;
    private readonly ILogger<AzureServiceBusMessagePublisher> _logger;
    private readonly IOptions<EncinaAzureServiceBusOptions> _options;

    public AzureServiceBusMessagePublisherTests()
    {
        _client = Substitute.For<ServiceBusClient>();
        _sender = Substitute.For<ServiceBusSender>();
        _logger = Substitute.For<ILogger<AzureServiceBusMessagePublisher>>();
        // Enable logging for coverage of LoggerMessage generated code
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _options = Options.Create(new EncinaAzureServiceBusOptions
        {
            DefaultQueueName = "test-queue",
            DefaultTopicName = "test-topic"
        });

        _client.CreateSender(Arg.Any<string>()).Returns(_sender);
    }

    #region Constructor Guard Clause Tests

    [Fact]
    public void Constructor_WithNullClient_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new AzureServiceBusMessagePublisher(null!, _logger, _options));
        ex.ParamName.ShouldBe("client");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new AzureServiceBusMessagePublisher(_client, null!, _options));
        ex.ParamName.ShouldBe("logger");
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new AzureServiceBusMessagePublisher(_client, _logger, null!));
        ex.ParamName.ShouldBe("options");
    }

    #endregion

    #region SendToQueueAsync Tests

    [Fact]
    public async Task SendToQueueAsync_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var publisher = CreatePublisher();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            publisher.SendToQueueAsync<TestMessage>(null!).AsTask());
    }

    [Fact]
    public async Task SendToQueueAsync_WithValidMessage_ShouldReturnUnit()
    {
        // Arrange
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.SendToQueueAsync(message);

        // Assert
        var value = result.ShouldBeSuccess();
        value.ShouldBe(Unit.Default);
    }

    [Fact]
    public async Task SendToQueueAsync_WithCustomQueueName_ShouldUseCustomQueue()
    {
        // Arrange
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };
        var customQueueName = "custom-queue";

        // Act
        var result = await publisher.SendToQueueAsync(message, customQueueName);

        // Assert
        result.ShouldBeSuccess();
        _client.Received(1).CreateSender(customQueueName);
    }

    [Fact]
    public async Task SendToQueueAsync_WhenSenderThrowsException_ShouldBeError()
    {
        // Arrange
        ConfigureSenderToThrow(new ServiceBusException("Queue not found", ServiceBusFailureReason.MessagingEntityNotFound));
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.SendToQueueAsync(message);

        // Assert
        result.ShouldBeError();
    }

    [Fact]
    public async Task SendToQueueAsync_WhenSenderThrowsException_ShouldReturnErrorWithCode()
    {
        // Arrange
        ConfigureSenderToThrow(new ServiceBusException("Queue not found", ServiceBusFailureReason.MessagingEntityNotFound));
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.SendToQueueAsync(message);

        // Assert
        result.ShouldBeErrorWithCode("AZURE_SB_SEND_FAILED");
    }

    [Fact]
    public async Task SendToQueueAsync_WhenSenderThrowsException_ShouldContainErrorMessage()
    {
        // Arrange
        ConfigureSenderToThrow(new InvalidOperationException("Connection lost"));
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.SendToQueueAsync(message);

        // Assert
        result.ShouldBeErrorContaining("Failed to send message");
    }

    #endregion

    #region PublishToTopicAsync Tests

    [Fact]
    public async Task PublishToTopicAsync_WithValidMessage_ShouldReturnUnit()
    {
        // Arrange
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.PublishToTopicAsync(message);

        // Assert
        var value = result.ShouldBeSuccess();
        value.ShouldBe(Unit.Default);
    }

    [Fact]
    public async Task PublishToTopicAsync_WithCustomTopicName_ShouldUseCustomTopic()
    {
        // Arrange
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };
        var customTopicName = "custom-topic";

        // Act
        var result = await publisher.PublishToTopicAsync(message, customTopicName);

        // Assert
        result.ShouldBeSuccess();
        _client.Received(1).CreateSender(customTopicName);
    }

    [Fact]
    public async Task PublishToTopicAsync_WhenSenderThrowsException_ShouldReturnErrorWithCode()
    {
        // Arrange
        ConfigureSenderToThrow(new ServiceBusException("Topic not found", ServiceBusFailureReason.MessagingEntityNotFound));
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.PublishToTopicAsync(message);

        // Assert
        result.ShouldBeErrorWithCode("AZURE_SB_PUBLISH_FAILED");
    }

    [Fact]
    public async Task PublishToTopicAsync_WhenSenderThrowsException_ShouldContainTopicInErrorMessage()
    {
        // Arrange
        ConfigureSenderToThrow(new InvalidOperationException("Connection lost"));
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.PublishToTopicAsync(message);

        // Assert
        result.ShouldBeErrorContaining("topic");
    }

    #endregion

    #region ScheduleAsync Tests

    [Fact]
    public async Task ScheduleAsync_WithValidMessage_ShouldReturnSequenceNumber()
    {
        // Arrange
        var expectedSequenceNumber = 12345L;
        ConfigureSenderScheduleSuccess(expectedSequenceNumber);
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };
        var scheduledTime = DateTimeOffset.UtcNow.AddMinutes(30);

        // Act
        var result = await publisher.ScheduleAsync(message, scheduledTime);

        // Assert
        var value = result.ShouldBeSuccess();
        value.ShouldBe(expectedSequenceNumber);
        await _sender.Received(1).ScheduleMessageAsync(
            Arg.Any<ServiceBusMessage>(),
            scheduledTime,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScheduleAsync_WhenSenderThrowsException_ShouldReturnErrorWithCode()
    {
        // Arrange
        ConfigureSenderScheduleToThrow(new InvalidOperationException("Scheduling failed"));
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };
        var scheduledTime = DateTimeOffset.UtcNow.AddMinutes(30);

        // Act
        var result = await publisher.ScheduleAsync(message, scheduledTime);

        // Assert
        result.ShouldBeErrorWithCode("AZURE_SB_SCHEDULE_FAILED");
    }

    #endregion

    #region CancelScheduledAsync Tests

    [Fact]
    public async Task CancelScheduledAsync_WithValidSequenceNumber_ShouldReturnUnit()
    {
        // Arrange
        var publisher = CreatePublisher();
        var sequenceNumber = 12345L;

        // Act
        var result = await publisher.CancelScheduledAsync(sequenceNumber);

        // Assert
        var value = result.ShouldBeSuccess();
        value.ShouldBe(Unit.Default);
        await _sender.Received(1).CancelScheduledMessageAsync(
            sequenceNumber,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelScheduledAsync_WhenSenderThrowsException_ShouldReturnErrorWithCode()
    {
        // Arrange
        ConfigureSenderCancelToThrow(new ServiceBusException("Message not found", ServiceBusFailureReason.MessageNotFound));
        var publisher = CreatePublisher();
        var sequenceNumber = 12345L;

        // Act
        var result = await publisher.CancelScheduledAsync(sequenceNumber);

        // Assert
        result.ShouldBeErrorWithCode("AZURE_SB_CANCEL_FAILED");
    }

    [Fact]
    public async Task CancelScheduledAsync_WhenSenderThrowsException_ShouldContainSequenceNumberInError()
    {
        // Arrange
        ConfigureSenderCancelToThrow(new InvalidOperationException("Cancel failed"));
        var publisher = CreatePublisher();
        var sequenceNumber = 12345L;

        // Act
        var result = await publisher.CancelScheduledAsync(sequenceNumber);

        // Assert
        result.ShouldBeErrorContaining(sequenceNumber.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    #endregion

    #region Helper Methods

    private AzureServiceBusMessagePublisher CreatePublisher()
    {
        return new AzureServiceBusMessagePublisher(_client, _logger, _options);
    }

    private void ConfigureSenderToThrow(Exception exception)
    {
        _sender
            .SendMessageAsync(Arg.Any<ServiceBusMessage>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(exception);
    }

    private void ConfigureSenderScheduleSuccess(long sequenceNumber)
    {
        _sender
            .ScheduleMessageAsync(Arg.Any<ServiceBusMessage>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(sequenceNumber);
    }

    private void ConfigureSenderScheduleToThrow(Exception exception)
    {
        _sender
            .ScheduleMessageAsync(Arg.Any<ServiceBusMessage>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(exception);
    }

    private void ConfigureSenderCancelToThrow(Exception exception)
    {
        _sender
            .CancelScheduledMessageAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(exception);
    }

    #endregion

    #region Test Types

    private sealed record TestMessage
    {
        public required int Id { get; init; }
        public required string Content { get; init; }
    }

    #endregion
}
