using Hangfire;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Encina.Hangfire;
using static LanguageExt.Prelude;

namespace Encina.Hangfire.Tests;

public class HangfireRequestJobAdapterTests
{
    private readonly IMediator _mediator;
    private readonly ILogger<HangfireRequestJobAdapter<TestRequest, TestResponse>> _logger;
    private readonly HangfireRequestJobAdapter<TestRequest, TestResponse> _adapter;

    public HangfireRequestJobAdapterTests()
    {
        _mediator = Substitute.For<IMediator>();
        _logger = Substitute.For<ILogger<HangfireRequestJobAdapter<TestRequest, TestResponse>>>();
        _adapter = new HangfireRequestJobAdapter<TestRequest, TestResponse>(_mediator, _logger);
    }

    [Fact]
    public async Task ExecuteAsync_WithSuccessfulRequest_ReturnsRight()
    {
        // Arrange
        var request = new TestRequest("test-data");
        var expectedResponse = new TestResponse("success");
        _mediator.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Right<MediatorError, TestResponse>(expectedResponse));

        // Act
        var result = await _adapter.ExecuteAsync(request);

        // Assert
        result.IsRight.Should().BeTrue();
        var actualResponse = result.Match(
            Right: response => response,
            Left: _ => throw new InvalidOperationException("Should not be Left")
        );
        actualResponse.Should().Be(expectedResponse);

        await _mediator.Received(1).Send(
            Arg.Is<TestRequest>(r => r.Data == "test-data"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithFailedRequest_ReturnsLeft()
    {
        // Arrange
        var request = new TestRequest("test-data");
        var error = MediatorErrors.Create("test.error", "Test error message");
        _mediator.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Left<MediatorError, TestResponse>(error));

        // Act
        var result = await _adapter.ExecuteAsync(request);

        // Assert
        result.IsLeft.Should().BeTrue();
        var actualError = result.Match(
            Right: _ => throw new InvalidOperationException("Should not be Right"),
            Left: err => err
        );
        actualError.Message.Should().Be("Test error message");
    }

    [Fact]
    public async Task ExecuteAsync_LogsExecutionStart()
    {
        // Arrange
        var request = new TestRequest("test-data");
        _mediator.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Right<MediatorError, TestResponse>(new TestResponse("success")));

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

    [Fact]
    public async Task ExecuteAsync_OnSuccess_LogsCompletion()
    {
        // Arrange
        var request = new TestRequest("test-data");
        _mediator.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Right<MediatorError, TestResponse>(new TestResponse("success")));

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

    [Fact]
    public async Task ExecuteAsync_OnFailure_LogsError()
    {
        // Arrange
        var request = new TestRequest("test-data");
        var error = MediatorErrors.Create("test.error", "Test error");
        _mediator.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Left<MediatorError, TestResponse>(error));

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

    [Fact]
    public async Task ExecuteAsync_WhenExceptionThrown_LogsAndRethrows()
    {
        // Arrange
        var request = new TestRequest("test-data");
        var exception = new InvalidOperationException("Test exception");
        _mediator.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns<Either<MediatorError, TestResponse>>(_ => throw exception);

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
        _mediator.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Right<MediatorError, TestResponse>(new TestResponse("success")));

        // Act
        await _adapter.ExecuteAsync(request, cts.Token);

        // Assert
        await _mediator.Received(1).Send(
            Arg.Any<TestRequest>(),
            Arg.Is<CancellationToken>(ct => ct == cts.Token));
    }

    // Test types
    public record TestRequest(string Data) : IRequest<TestResponse>;
    public record TestResponse(string Result);
}
