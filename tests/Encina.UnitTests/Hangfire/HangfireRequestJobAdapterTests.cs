using Encina.Hangfire;
using Encina.Testing.Shouldly;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Hangfire;

public class HangfireRequestJobAdapterTests
{
    private readonly IEncina _encina;
    private readonly FakeLogger<HangfireRequestJobAdapter<TestRequest, TestResponse>> _logger;
    private readonly HangfireRequestJobAdapter<TestRequest, TestResponse> _adapter;

    public HangfireRequestJobAdapterTests()
    {
        _encina = Substitute.For<IEncina>();
        _logger = new FakeLogger<HangfireRequestJobAdapter<TestRequest, TestResponse>>();
        _adapter = new HangfireRequestJobAdapter<TestRequest, TestResponse>(_encina, _logger);
    }

    [Fact]
    public async Task ExecuteAsync_WithSuccessfulRequest_ReturnsRight()
    {
        // Arrange
        var request = new TestRequest("test-data");
        var expectedResponse = new TestResponse("success");
        _encina.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, TestResponse>(expectedResponse));

        // Act
        var result = await _adapter.ExecuteAsync(request);

        // Assert
        result.ShouldBeSuccess().ShouldBe(expectedResponse);

        await _encina.Received(1).Send(
            Arg.Is<TestRequest>(r => r.Data == "test-data"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithFailedRequest_ReturnsLeft()
    {
        // Arrange
        var request = new TestRequest("test-data");
        var error = EncinaErrors.Create("test.error", "Test error message");
        _encina.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, TestResponse>(error));

        // Act
        var result = await _adapter.ExecuteAsync(request);

        // Assert
        result.ShouldBeError(e => e.Message.ShouldBe("Test error message"));
    }

    [Fact]
    public async Task ExecuteAsync_LogsExecutionStart()
    {
        // Arrange
        var request = new TestRequest("test-data");
        _encina.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, TestResponse>(new TestResponse("success")));

        // Act
        await _adapter.ExecuteAsync(request);

        // Assert
        var logEntry = _logger.Collector.GetSnapshot()
            .FirstOrDefault(r => r.Message.Contains("Executing Hangfire job"));
        logEntry.ShouldNotBeNull();
        logEntry!.Level.ShouldBe(LogLevel.Information);
    }

    [Fact]
    public async Task ExecuteAsync_OnSuccess_LogsCompletion()
    {
        // Arrange
        var request = new TestRequest("test-data");
        _encina.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, TestResponse>(new TestResponse("success")));

        // Act
        await _adapter.ExecuteAsync(request);

        // Assert
        var logEntry = _logger.Collector.GetSnapshot()
            .FirstOrDefault(r => r.Message.Contains("completed successfully"));
        logEntry.ShouldNotBeNull();
        logEntry!.Level.ShouldBe(LogLevel.Information);
    }

    [Fact]
    public async Task ExecuteAsync_OnFailure_LogsError()
    {
        // Arrange
        var request = new TestRequest("test-data");
        var error = EncinaErrors.Create("test.error", "Test error");
        _encina.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, TestResponse>(error));

        // Act
        await _adapter.ExecuteAsync(request);

        // Assert
        var logEntry = _logger.Collector.GetSnapshot()
            .FirstOrDefault(r => r.Message.Contains("failed"));
        logEntry.ShouldNotBeNull();
        logEntry!.Level.ShouldBe(LogLevel.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WhenExceptionThrown_LogsAndRethrows()
    {
        // Arrange
        var request = new TestRequest("test-data");
        var exception = new InvalidOperationException("Test exception");
        _encina.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns<Either<EncinaError, TestResponse>>(_ => throw exception);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _adapter.ExecuteAsync(request));

        var logEntry = _logger.Collector.GetSnapshot()
            .FirstOrDefault(r => r.Message.Contains("Unhandled exception"));
        logEntry.ShouldNotBeNull();
        logEntry!.Level.ShouldBe(LogLevel.Error);
        logEntry.Exception.ShouldBe(exception);
    }

    [Fact]
    public async Task ExecuteAsync_PassesCancellationToken()
    {
        // Arrange
        var request = new TestRequest("test-data");
        var cts = new CancellationTokenSource();
        _encina.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, TestResponse>(new TestResponse("success")));

        // Act
        await _adapter.ExecuteAsync(request, cts.Token);

        // Assert
        await _encina.Received(1).Send(
            Arg.Any<TestRequest>(),
            Arg.Is<CancellationToken>(ct => ct == cts.Token));
    }

    // Test types
    public record TestRequest(string Data) : IRequest<TestResponse>;
    public record TestResponse(string Result);
}
