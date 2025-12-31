using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.Hangfire.Tests;

public class HangfireRequestJobAdapterTests
{
    private readonly IEncina _Encina;
    private readonly ILogger<HangfireRequestJobAdapter<TestRequest, TestResponse>> _logger;
    private readonly HangfireRequestJobAdapter<TestRequest, TestResponse> _adapter;

    public HangfireRequestJobAdapterTests()
    {
        _Encina = Substitute.For<IEncina>();
        _logger = Substitute.For<ILogger<HangfireRequestJobAdapter<TestRequest, TestResponse>>>();
        _adapter = new HangfireRequestJobAdapter<TestRequest, TestResponse>(_Encina, _logger);
    }

    [Fact]
    public async Task ExecuteAsync_WithSuccessfulRequest_ReturnsRight()
    {
        // Arrange
        var request = new TestRequest("test-data");
        var expectedResponse = new TestResponse("success");
        _Encina.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, TestResponse>(expectedResponse));

        // Act
        var result = await _adapter.ExecuteAsync(request);

        // Assert
        result.ShouldBeSuccess().ShouldBe(expectedResponse);

        await _Encina.Received(1).Send(
            Arg.Is<TestRequest>(r => r.Data == "test-data"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithFailedRequest_ReturnsLeft()
    {
        // Arrange
        var request = new TestRequest("test-data");
        var error = EncinaErrors.Create("test.error", "Test error message");
        _Encina.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, TestResponse>(error));

        // Act
        var result = await _adapter.ExecuteAsync(request);

        // Assert
        result.ShouldBeError(e => e.Message.ShouldBe("Test error message"));
    }

    [Fact(Skip = "Issue #6: LoggerMessage delegates incompatible with NSubstitute.Received()")]
    public async Task ExecuteAsync_LogsExecutionStart()
    {
        // Arrange
        var request = new TestRequest("test-data");
        _Encina.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, TestResponse>(new TestResponse("success")));

        // Act
        await _adapter.ExecuteAsync(request);

        // Assert
        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Executing Hangfire job")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact(Skip = "Issue #6: LoggerMessage delegates incompatible with NSubstitute.Received()")]
    public async Task ExecuteAsync_OnSuccess_LogsCompletion()
    {
        // Arrange
        var request = new TestRequest("test-data");
        _Encina.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, TestResponse>(new TestResponse("success")));

        // Act
        await _adapter.ExecuteAsync(request);

        // Assert
        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("completed successfully")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact(Skip = "Issue #6: LoggerMessage delegates incompatible with NSubstitute.Received()")]
    public async Task ExecuteAsync_OnFailure_LogsError()
    {
        // Arrange
        var request = new TestRequest("test-data");
        var error = EncinaErrors.Create("test.error", "Test error");
        _Encina.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, TestResponse>(error));

        // Act
        await _adapter.ExecuteAsync(request);

        // Assert
        _logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("failed")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact(Skip = "Issue #6: LoggerMessage delegates incompatible with NSubstitute.Received()")]
    public async Task ExecuteAsync_WhenExceptionThrown_LogsAndRethrows()
    {
        // Arrange
        var request = new TestRequest("test-data");
        var exception = new InvalidOperationException("Test exception");
        _Encina.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns<Either<EncinaError, TestResponse>>(_ => throw exception);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _adapter.ExecuteAsync(request));

        _logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Unhandled exception")),
            exception,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task ExecuteAsync_PassesCancellationToken()
    {
        // Arrange
        var request = new TestRequest("test-data");
        var cts = new CancellationTokenSource();
        _Encina.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, TestResponse>(new TestResponse("success")));

        // Act
        await _adapter.ExecuteAsync(request, cts.Token);

        // Assert
        await _Encina.Received(1).Send(
            Arg.Any<TestRequest>(),
            Arg.Is<CancellationToken>(ct => ct == cts.Token));
    }

    // Test types
    public record TestRequest(string Data) : IRequest<TestResponse>;
    public record TestResponse(string Result);
}
