using Encina.Testing;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Encina.NATS.Tests.Publishing;

/// <summary>
/// Unit tests for <see cref="NATSMessagePublisher"/> using EitherAssertions.
/// </summary>
public sealed class NATSMessagePublisherTests
{
    private readonly INatsConnection _connection;
    private readonly INatsJSContext _jetStream;
    private readonly ILogger<NATSMessagePublisher> _logger;
    private readonly IOptions<EncinaNATSOptions> _options;

    public NATSMessagePublisherTests()
    {
        _connection = Substitute.For<INatsConnection>();
        _jetStream = Substitute.For<INatsJSContext>();
        _logger = Substitute.For<ILogger<NATSMessagePublisher>>();
        // Enable logging for coverage of LoggerMessage generated code
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _options = Options.Create(new EncinaNATSOptions
        {
            SubjectPrefix = "test"
        });
    }

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
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute mock configuration pattern")]
    public async Task PublishAsync_WhenConnectionThrowsException_ShouldReturnError()
    {
        // Arrange
        _connection
            .When(c => c.PublishAsync(
                Arg.Any<string>(),
                Arg.Any<byte[]>(),
                Arg.Any<NatsHeaders?>(),
                Arg.Any<string?>(),
                Arg.Any<INatsSerialize<byte[]>?>(),
                Arg.Any<NatsPubOpts?>(),
                Arg.Any<CancellationToken>()))
            .Do(_ => throw new InvalidOperationException("Connection failed"));

        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.PublishAsync(message);

        // Assert
        result.ShouldBeError();
        result.ShouldBeErrorWithCode("NATS_PUBLISH_FAILED");
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

        // Verify the connection's PublishAsync was called with expected subject
        await _connection.Received(1).PublishAsync(
            "test.TestMessage",
            Arg.Any<byte[]>(),
            Arg.Any<NatsHeaders?>(),
            Arg.Any<string?>(),
            Arg.Any<INatsSerialize<byte[]>?>(),
            Arg.Any<NatsPubOpts?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_WithCustomSubject_ShouldBeSuccess()
    {
        // Arrange
        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };
        var customSubject = "custom.subject";

        // Act
        var result = await publisher.PublishAsync(message, customSubject);

        // Assert
        result.ShouldBeSuccess();

        // Verify the connection's PublishAsync was called with the custom subject
        await _connection.Received(1).PublishAsync(
            customSubject,
            Arg.Any<byte[]>(),
            Arg.Any<NatsHeaders?>(),
            Arg.Any<string?>(),
            Arg.Any<INatsSerialize<byte[]>?>(),
            Arg.Any<NatsPubOpts?>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region JetStreamPublishAsync Tests - Configuration Validation

    [Fact]
    public async Task JetStreamPublishAsync_WhenJetStreamNotEnabled_ShouldReturnHelpfulError()
    {
        // Arrange
        var publisherWithoutJetStream = CreatePublisherWithoutJetStream();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisherWithoutJetStream.JetStreamPublishAsync(message);

        // Assert
        result.ShouldBeErrorWithCode("NATS_JETSTREAM_NOT_ENABLED");
        result.ShouldBeErrorContaining("JetStream is not enabled");
        result.ShouldBeErrorContaining("UseJetStream = true");
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullConnection_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new NATSMessagePublisher(null!, _jetStream, _logger, _options));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new NATSMessagePublisher(_connection, _jetStream, null!, _options));
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new NATSMessagePublisher(_connection, _jetStream, _logger, null!));
    }

    [Fact]
    public void Constructor_WithNullJetStream_ShouldSucceed()
    {
        // Arrange & Act
        var publisher = new NATSMessagePublisher(_connection, null, _logger, _options);

        // Assert
        publisher.ShouldNotBeNull();
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
            publisher.RequestAsync<TestMessage, TestMessage>(null!).AsTask());
    }

    #endregion

    #region JetStreamPublishAsync Tests

    [Fact]
    public async Task JetStreamPublishAsync_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var publisher = CreatePublisher();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            publisher.JetStreamPublishAsync<TestMessage>(null!).AsTask());
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute mock configuration pattern")]
    public async Task JetStreamPublishAsync_WithValidMessage_ShouldReturnAck()
    {
        // Arrange
        var ack = new PubAckResponse { Stream = "test-stream", Seq = 42, Duplicate = false };

        _jetStream.PublishAsync<byte[]>(
            Arg.Any<string>(),
            Arg.Any<byte[]>(),
            Arg.Any<INatsSerialize<byte[]>?>(),
            Arg.Any<NatsJSPubOpts?>(),
            Arg.Any<NatsHeaders?>(),
            Arg.Any<CancellationToken>())
            .Returns(new ValueTask<PubAckResponse>(ack));

        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.JetStreamPublishAsync(message);

        // Assert
        result.ShouldBeSuccess();
        result.Match(
            publishAck =>
            {
                publishAck.Stream.ShouldBe("test-stream");
                publishAck.Sequence.ShouldBe(42UL);
                publishAck.Duplicate.ShouldBeFalse();
            },
            _ => Assert.Fail("Expected success"));
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute mock configuration pattern")]
    public async Task JetStreamPublishAsync_WithCustomSubject_ShouldUseCustomSubject()
    {
        // Arrange
        var ack = new PubAckResponse { Stream = "test-stream", Seq = 1, Duplicate = false };

        _jetStream.PublishAsync<byte[]>(
            Arg.Any<string>(),
            Arg.Any<byte[]>(),
            Arg.Any<INatsSerialize<byte[]>?>(),
            Arg.Any<NatsJSPubOpts?>(),
            Arg.Any<NatsHeaders?>(),
            Arg.Any<CancellationToken>())
            .Returns(new ValueTask<PubAckResponse>(ack));

        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.JetStreamPublishAsync(message, "custom.jetstream.subject");

        // Assert
        result.ShouldBeSuccess();

        await _jetStream.Received(1).PublishAsync<byte[]>(
            "custom.jetstream.subject",
            Arg.Any<byte[]>(),
            Arg.Any<INatsSerialize<byte[]>?>(),
            Arg.Any<NatsJSPubOpts?>(),
            Arg.Any<NatsHeaders?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute mock configuration pattern")]
    public async Task JetStreamPublishAsync_WhenDuplicate_ShouldReturnAckWithDuplicateFlag()
    {
        // Arrange
        var ack = new PubAckResponse { Stream = "test-stream", Seq = 42, Duplicate = true };

        _jetStream.PublishAsync<byte[]>(
            Arg.Any<string>(),
            Arg.Any<byte[]>(),
            Arg.Any<INatsSerialize<byte[]>?>(),
            Arg.Any<NatsJSPubOpts?>(),
            Arg.Any<NatsHeaders?>(),
            Arg.Any<CancellationToken>())
            .Returns(new ValueTask<PubAckResponse>(ack));

        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.JetStreamPublishAsync(message);

        // Assert
        result.ShouldBeSuccess();
        result.Match(
            publishAck => publishAck.Duplicate.ShouldBeTrue(),
            _ => Assert.Fail("Expected success"));
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute mock configuration pattern")]
    public async Task JetStreamPublishAsync_WhenStreamIsNull_ShouldReturnEmptyString()
    {
        // Arrange
        var ack = new PubAckResponse { Stream = null, Seq = 1, Duplicate = false };

        _jetStream.PublishAsync<byte[]>(
            Arg.Any<string>(),
            Arg.Any<byte[]>(),
            Arg.Any<INatsSerialize<byte[]>?>(),
            Arg.Any<NatsJSPubOpts?>(),
            Arg.Any<NatsHeaders?>(),
            Arg.Any<CancellationToken>())
            .Returns(new ValueTask<PubAckResponse>(ack));

        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.JetStreamPublishAsync(message);

        // Assert
        result.ShouldBeSuccess();
        result.Match(
            publishAck => publishAck.Stream.ShouldBe(string.Empty),
            _ => Assert.Fail("Expected success"));
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute mock configuration pattern")]
    public async Task JetStreamPublishAsync_WhenJetStreamThrowsException_ShouldReturnError()
    {
        // Arrange
        _jetStream.PublishAsync<byte[]>(
            Arg.Any<string>(),
            Arg.Any<byte[]>(),
            Arg.Any<INatsSerialize<byte[]>?>(),
            Arg.Any<NatsJSPubOpts?>(),
            Arg.Any<NatsHeaders?>(),
            Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("JetStream publish failed"));

        var publisher = CreatePublisher();
        var message = new TestMessage { Id = 1, Content = "Test" };

        // Act
        var result = await publisher.JetStreamPublishAsync(message);

        // Assert
        result.ShouldBeError();
        result.ShouldBeErrorWithCode("NATS_JETSTREAM_PUBLISH_FAILED");
    }

    #endregion

    #region DisposeAsync Tests

    [Fact]
    public async Task DisposeAsync_ShouldDisposeConnection()
    {
        // Arrange
        var publisher = CreatePublisher();

        // Act
        await publisher.DisposeAsync();

        // Assert
        await _connection.Received(1).DisposeAsync();
    }

    #endregion

    #region Helper Methods

    private NATSMessagePublisher CreatePublisher()
    {
        return new NATSMessagePublisher(_connection, _jetStream, _logger, _options);
    }

    private NATSMessagePublisher CreatePublisherWithoutJetStream()
    {
        return new NATSMessagePublisher(_connection, null, _logger, _options);
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
