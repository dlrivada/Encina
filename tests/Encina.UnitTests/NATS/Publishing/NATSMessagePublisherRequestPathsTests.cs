#pragma warning disable CA2012 // NSubstitute ValueTask stubbing pattern

using System.Text;
using Encina.Diagnostics;
using Encina.NATS;
using Encina.Testing;
using Microsoft.Extensions.Logging.Testing;
using NATS.Client.Core;
using NATS.Client.JetStream;

namespace Encina.UnitTests.NATS.Publishing;

/// <summary>
/// Unit tests for the request/reply paths of <see cref="NATSMessagePublisher.RequestAsync{TRequest, TResponse}"/>:
/// the reply, an empty reply, the timeout and the failure.
/// </summary>
public sealed class NATSMessagePublisherRequestPathsTests : IAsyncDisposable
{
    private const string SentinelMessage = "sentinel-secret-message";

    private readonly INatsConnection _connection = Substitute.For<INatsConnection>();
    private readonly FakeLogger<NATSMessagePublisher> _logger = new();
    private readonly NATSMessagePublisher _publisher;

    public NATSMessagePublisherRequestPathsTests()
    {
        _publisher = new NATSMessagePublisher(
            _connection,
            Substitute.For<INatsJSContext>(),
            _logger,
            Options.Create(new EncinaNATSOptions { SubjectPrefix = "test" }));
    }

    public ValueTask DisposeAsync() => _publisher.DisposeAsync();

    [Fact]
    public async Task RequestAsync_WithAReply_ReturnsTheDeserializedResponse()
    {
        // Arrange
        SetupReply(Encoding.UTF8.GetBytes("""{"Text":"pong"}"""));

        // Act
        var result = await _publisher.RequestAsync<Ping, Pong>(new Ping("ping"));

        // Assert
        var response = result.ShouldBeSuccess();
        response.Text.ShouldBe("pong");
    }

    [Fact]
    public async Task RequestAsync_WithANullReply_ReturnsDeserializeFailed()
    {
        // Arrange
        SetupReply(Encoding.UTF8.GetBytes("null"));

        // Act
        var result = await _publisher.RequestAsync<Ping, Pong>(new Ping("ping"));

        // Assert
        result.ShouldBeErrorWithCode("NATS_DESERIALIZE_FAILED");
    }

    [Fact]
    public async Task RequestAsync_WhenTheRequestTimesOut_ReturnsTimeoutWithoutLoggingAnException()
    {
        // Arrange
        SetupFailure(new OperationCanceledException());

        // Act
        var result = await _publisher.RequestAsync<Ping, Pong>(new Ping("ping"), timeout: TimeSpan.FromSeconds(5));

        // Assert
        result.ShouldBeErrorWithCode("NATS_REQUEST_TIMEOUT");
        _logger.Collector.GetSnapshot().ShouldAllBe(r => r.Exception == null);
    }

    [Fact]
    public async Task RequestAsync_WhenTheCallerCancels_ReturnsRequestFailedAndLogsARedactedException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        SetupFailure(new OperationCanceledException(SentinelMessage));

        // Act
        var result = await _publisher.RequestAsync<Ping, Pong>(new Ping("ping"), cancellationToken: cts.Token);

        // Assert
        result.ShouldBeErrorWithCode("NATS_REQUEST_FAILED");
        AssertSingleRedactedLog();
    }

    [Fact]
    public async Task RequestAsync_WhenTheConnectionThrows_ReturnsRequestFailedAndLogsARedactedException()
    {
        // Arrange
        SetupFailure(new InvalidOperationException(SentinelMessage));

        // Act
        var result = await _publisher.RequestAsync<Ping, Pong>(new Ping("ping"));

        // Assert
        result.ShouldBeErrorWithCode("NATS_REQUEST_FAILED");
        AssertSingleRedactedLog();
    }

    private void SetupReply(byte[] data) =>
        _connection.RequestAsync<byte[], byte[]>(
                Arg.Any<string>(),
                Arg.Any<byte[]?>(),
                Arg.Any<NatsHeaders?>(),
                Arg.Any<INatsSerialize<byte[]>?>(),
                Arg.Any<INatsDeserialize<byte[]>?>(),
                Arg.Any<NatsPubOpts?>(),
                Arg.Any<NatsSubOpts?>(),
                Arg.Any<CancellationToken>())
            .Returns(new ValueTask<NatsMsg<byte[]>>(new NatsMsg<byte[]> { Data = data }));

    private void SetupFailure(Exception exception) =>
        _connection.RequestAsync<byte[], byte[]>(
                Arg.Any<string>(),
                Arg.Any<byte[]?>(),
                Arg.Any<NatsHeaders?>(),
                Arg.Any<INatsSerialize<byte[]>?>(),
                Arg.Any<INatsDeserialize<byte[]>?>(),
                Arg.Any<NatsPubOpts?>(),
                Arg.Any<NatsSubOpts?>(),
                Arg.Any<CancellationToken>())
            .Returns<ValueTask<NatsMsg<byte[]>>>(_ => throw exception);

    private void AssertSingleRedactedLog()
    {
        var entry = _logger.Collector.GetSnapshot().Single(r => r.Exception is not null);
        entry.Exception.ShouldBeOfType<RedactedException>();
        entry.Exception!.ToString().ShouldNotContain(SentinelMessage);
        entry.Message.ShouldNotContain(SentinelMessage);
    }

    private sealed record Ping(string Text);

    private sealed record Pong(string Text);
}
