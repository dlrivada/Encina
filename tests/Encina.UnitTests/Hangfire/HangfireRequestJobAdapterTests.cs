using Encina.Hangfire;
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
    public async Task ExecuteAsync_WithSuccessfulRequest_ReturnsResponse()
    {
        // Arrange
        var request = new TestRequest("test-data");
        var expectedResponse = new TestResponse("success");
        _encina.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, TestResponse>(expectedResponse));

        // Act
        var result = await _adapter.ExecuteAndReturnResultAsync(request);

        // Assert
        result.ShouldBe(expectedResponse);

        await _encina.Received(1).Send(
            Arg.Is<TestRequest>(r => r.Data == "test-data"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithFailedRequest_ThrowsEncinaJobFailedException()
    {
        // Arrange
        var request = new TestRequest("test-data");
        var error = EncinaErrors.Create("test.error", "Test error message");
        _encina.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, TestResponse>(error));

        // Act
        var exception = await Should.ThrowAsync<EncinaJobFailedException>(() =>
            _adapter.ExecuteAsync(request));

        // Assert
        // A Left result must surface as a thrown exception (mirroring QuartzRequestJob's
        // JobExecutionException) so Hangfire marks the job Failed and retries it.
        exception.ErrorCode.ShouldBe("test.error");
        exception.Message.ShouldContain("test.error");
        exception.Message.ShouldNotContain("Test error message");
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
        // ExecuteAsync now throws EncinaJobFailedException on Left, after logging the failure.
        await Should.ThrowAsync<EncinaJobFailedException>(() => _adapter.ExecuteAsync(request));

        // Assert
        var logEntry = _logger.Collector.GetSnapshot()
            .FirstOrDefault(r => r.Message.Contains("failed"));
        logEntry.ShouldNotBeNull();
        logEntry!.Level.ShouldBe(LogLevel.Error);
    }

    [Fact]
    public async Task ExecuteAsync_OnFailure_LogsErrorCodeOnlyNotErrorMessage()
    {
        // Arrange: the failure message may contain personal data (e.g. a subject id from
        // ConsentErrors/DSRErrors); the log line must carry only the error code (#1173).
        var request = new TestRequest("test-data");
        var error = EncinaErrors.Create("test.error", "Failure for subject 'patient-123'");
        _encina.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, TestResponse>(error));

        // Act
        await Should.ThrowAsync<EncinaJobFailedException>(() => _adapter.ExecuteAsync(request));

        // Assert
        var logEntry = _logger.Collector.GetSnapshot()
            .FirstOrDefault(r => r.Message.Contains("failed"));
        logEntry.ShouldNotBeNull();
        logEntry!.Message.ShouldContain("test.error");
        logEntry.Message.ShouldNotContain("patient-123");
        logEntry.Message.ShouldNotContain("Failure for subject");
    }

    [Fact]
    public void ExecuteAsync_DefaultEntryPoint_ReturnTypeDoesNotExposeResponse()
    {
        // The default enqueue entry point must not persist the handler's response in
        // Hangfire storage (#1173): its return type is plain Task, not
        // Task<Either<EncinaError, TResponse>>, so Hangfire's job storage has nothing to
        // serialize. ExecuteAndReturnResultAsync is the explicit, documented opt-in.
        var method = typeof(HangfireRequestJobAdapter<TestRequest, TestResponse>)
            .GetMethod(nameof(HangfireRequestJobAdapter<TestRequest, TestResponse>.ExecuteAsync));

        method.ShouldNotBeNull();
        method!.ReturnType.ShouldBe(typeof(Task));
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
