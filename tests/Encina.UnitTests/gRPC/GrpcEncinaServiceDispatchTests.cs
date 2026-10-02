#pragma warning disable CA2012 // NSubstitute ValueTask stubbing pattern

using System.Text.Json;
using Encina.Diagnostics;
using Encina.gRPC;
using Encina.Testing;
using LanguageExt;
using Microsoft.Extensions.Logging.Testing;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.gRPC;

/// <summary>
/// Unit tests for the dispatch paths of <see cref="GrpcEncinaService"/>: the reflective call into
/// <see cref="IEncina"/>, the response serialization and the failure paths, including that every
/// logged exception is redacted.
/// </summary>
public sealed class GrpcEncinaServiceDispatchTests
{
    private const string SentinelMessage = "sentinel-secret-message";

    private readonly IEncina _encina = Substitute.For<IEncina>();
    private readonly ITypeResolver _typeResolver = Substitute.For<ITypeResolver>();
    private readonly FakeLogger<GrpcEncinaService> _logger = new();
    private readonly GrpcEncinaService _service;

    public GrpcEncinaServiceDispatchTests()
    {
        _service = new GrpcEncinaService(_encina, _logger, _typeResolver, Options.Create(new EncinaGrpcOptions()));
    }

    [Fact]
    public async Task SendAsync_WhenEncinaReturnsAResponse_ReturnsItsSerializedBytes()
    {
        // Arrange
        _typeResolver.ResolveRequestType("echo").Returns(typeof(EchoRequest));
        _encina.Send(Arg.Is<EchoRequest>(r => r.Text == "hi"), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, EchoResponse>(new EchoResponse("hi back", null))));

        // Act
        var result = await _service.SendAsync("echo", JsonSerializer.SerializeToUtf8Bytes(new EchoRequest("hi")));

        // Assert
        var bytes = result.ShouldBeSuccess();
        JsonSerializer.Deserialize<EchoResponse>(bytes)!.Text.ShouldBe("hi back");
    }

    [Fact]
    public async Task SendAsync_WhenEncinaReturnsAnError_ReturnsThatError()
    {
        // Arrange
        _typeResolver.ResolveRequestType("echo").Returns(typeof(EchoRequest));
        _encina.Send(Arg.Any<EchoRequest>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Left<EncinaError, EchoResponse>(EncinaErrors.Create("DOWNSTREAM_ERROR", "downstream"))));

        // Act
        var result = await _service.SendAsync("echo", JsonSerializer.SerializeToUtf8Bytes(new EchoRequest("hi")));

        // Assert
        result.ShouldBeErrorWithCode("DOWNSTREAM_ERROR");
    }

    [Fact]
    public async Task SendAsync_WhenTheResponseCannotBeSerialized_ReturnsSerializeFailedAndLogsARedactedException()
    {
        // Arrange
        var cyclic = new Cyclic();
        cyclic.Next = cyclic;
        _typeResolver.ResolveRequestType("echo").Returns(typeof(EchoRequest));
        _encina.Send(Arg.Any<EchoRequest>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, EchoResponse>(new EchoResponse("loop", cyclic))));

        // Act
        var result = await _service.SendAsync("echo", JsonSerializer.SerializeToUtf8Bytes(new EchoRequest("hi")));

        // Assert
        result.ShouldBeErrorWithCode("GRPC_SERIALIZE_FAILED");
        AssertSingleRedactedLog();
    }

    [Fact]
    public async Task SendAsync_WhenEncinaThrows_ReturnsSendFailedAndLogsARedactedException()
    {
        // Arrange
        _typeResolver.ResolveRequestType("echo").Returns(typeof(EchoRequest));
        _encina.Send(Arg.Any<EchoRequest>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, EchoResponse>>>(_ => throw new InvalidOperationException(SentinelMessage));

        // Act
        var result = await _service.SendAsync("echo", JsonSerializer.SerializeToUtf8Bytes(new EchoRequest("hi")));

        // Assert
        result.ShouldBeErrorWithCode("GRPC_SEND_FAILED");
        AssertSingleRedactedLog();
    }

    [Fact]
    public async Task PublishAsync_WhenEncinaReturnsAnError_ReturnsThatError()
    {
        // Arrange
        _typeResolver.ResolveNotificationType("note").Returns(typeof(Note));
        _encina.Publish(Arg.Any<Note>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Left<EncinaError, Unit>(EncinaErrors.Create("DOWNSTREAM_ERROR", "downstream"))));

        // Act
        var result = await _service.PublishAsync("note", JsonSerializer.SerializeToUtf8Bytes(new Note("n")));

        // Assert
        result.ShouldBeErrorWithCode("DOWNSTREAM_ERROR");
    }

    [Fact]
    public async Task PublishAsync_WhenEncinaThrows_ReturnsPublishFailedAndLogsARedactedException()
    {
        // Arrange
        _typeResolver.ResolveNotificationType("note").Returns(typeof(Note));
        _encina.Publish(Arg.Any<Note>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, Unit>>>(_ => throw new InvalidOperationException(SentinelMessage));

        // Act
        var result = await _service.PublishAsync("note", JsonSerializer.SerializeToUtf8Bytes(new Note("n")));

        // Assert
        result.ShouldBeErrorWithCode("GRPC_PUBLISH_FAILED");
        AssertSingleRedactedLog();
    }

    private void AssertSingleRedactedLog()
    {
        var entry = _logger.Collector.GetSnapshot().Single(r => r.Exception is not null);
        entry.Exception.ShouldBeOfType<RedactedException>();
        entry.Exception!.ToString().ShouldNotContain(SentinelMessage);
        entry.Message.ShouldNotContain(SentinelMessage);
    }

    public sealed class Cyclic
    {
        public Cyclic? Next { get; set; }
    }

    public sealed record EchoResponse(string Text, Cyclic? Loop);

    public sealed record EchoRequest(string Text) : ICommand<EchoResponse>;

    public sealed record Note(string Text) : INotification;
}
