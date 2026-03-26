using Encina.NATS;
using Encina.Testing;
using NATS.Client.Core;
using NATS.Client.JetStream;

namespace Encina.UnitTests.NATS.Publishing;

/// <summary>
/// Additional unit tests for <see cref="NATSMessagePublisher"/> covering
/// edge cases and additional paths not covered by the main test file.
/// </summary>
public sealed class NATSMessagePublisherAdditionalTests
{
    private readonly INatsConnection _connection;
    private readonly INatsJSContext _jetStream;
    private readonly ILogger<NATSMessagePublisher> _logger;
    private readonly IOptions<EncinaNATSOptions> _options;

    public NATSMessagePublisherAdditionalTests()
    {
        _connection = Substitute.For<INatsConnection>();
        _jetStream = Substitute.For<INatsJSContext>();
        _logger = Substitute.For<ILogger<NATSMessagePublisher>>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _options = Options.Create(new EncinaNATSOptions
        {
            SubjectPrefix = "test"
        });
    }

    [Fact]
    public async Task PublishAsync_DefaultSubject_UsesSubjectPrefix()
    {
        // Arrange
        var publisher = new NATSMessagePublisher(_connection, _jetStream, _logger, _options);
        var message = new DetailedTestMessage { Id = 42, Name = "Test", Description = "Full message" };

        // Act
        var result = await publisher.PublishAsync(message);

        // Assert
        result.ShouldBeSuccess();

        await _connection.Received(1).PublishAsync(
            "test.DetailedTestMessage",
            Arg.Any<byte[]>(),
            Arg.Any<NatsHeaders?>(),
            Arg.Any<string?>(),
            Arg.Any<INatsSerialize<byte[]>?>(),
            Arg.Any<NatsPubOpts?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task JetStreamPublishAsync_WithNullJetStream_ReturnsDescriptiveError()
    {
        // Arrange
        var publisher = new NATSMessagePublisher(_connection, null, _logger, _options);
        var message = new DetailedTestMessage { Id = 1, Name = "Test" };

        // Act
        var result = await publisher.JetStreamPublishAsync(message);

        // Assert
        result.ShouldBeError();
        result.ShouldBeErrorWithCode("NATS_JETSTREAM_NOT_ENABLED");
        result.ShouldBeErrorContaining("UseJetStream = true");
    }

    [Fact]
    public async Task RequestAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var publisher = new NATSMessagePublisher(_connection, _jetStream, _logger, _options);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            publisher.RequestAsync<DetailedTestMessage, DetailedTestMessage>(null!).AsTask());
    }

    [Fact]
    public async Task DisposeAsync_DisposesConnection()
    {
        // Arrange
        var publisher = new NATSMessagePublisher(_connection, _jetStream, _logger, _options);

        // Act
        await publisher.DisposeAsync();

        // Assert
        await _connection.Received(1).DisposeAsync();
    }

    private sealed record DetailedTestMessage
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
    }
}
