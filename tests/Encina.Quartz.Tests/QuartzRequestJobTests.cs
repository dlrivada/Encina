using Encina.Quartz;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Quartz;
using static LanguageExt.Prelude;

namespace Encina.Quartz.Tests;

public class QuartzRequestJobTests
{
    private readonly IEncina _Encina;
    private readonly ILogger<QuartzRequestJob<TestRequest, TestResponse>> _logger;
    private readonly QuartzRequestJob<TestRequest, TestResponse> _job;
    private readonly IJobExecutionContext _context;

    public QuartzRequestJobTests()
    {
        _Encina = Substitute.For<IEncina>();
        _logger = Substitute.For<ILogger<QuartzRequestJob<TestRequest, TestResponse>>>();
        _job = new QuartzRequestJob<TestRequest, TestResponse>(_Encina, _logger);
        _context = Substitute.For<IJobExecutionContext>();

        // Setup default JobDataMap
        var jobDetail = Substitute.For<IJobDetail>();
        var jobDataMap = new JobDataMap();
        jobDetail.JobDataMap.Returns(jobDataMap);
        jobDetail.Key.Returns(new JobKey("test-job"));
        _context.JobDetail.Returns(jobDetail);
    }

    [Fact]
    public async Task Execute_WithSuccessfulRequest_CompletesSuccessfully()
    {
        // Arrange
        var request = new TestRequest("test-data");
        var expectedResponse = new TestResponse("success");
        _context.JobDetail.JobDataMap.Put(QuartzConstants.RequestKey, request);
        _Encina.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, TestResponse>(expectedResponse));

        // Act
        await _job.Execute(_context);

        // Assert
        await _Encina.Received(1).Send(
            Arg.Is<TestRequest>(r => r.Data == "test-data"),
            Arg.Any<CancellationToken>());

        _context.Received().Result = expectedResponse;
    }

    [Fact]
    public async Task Execute_WithFailedRequest_ThrowsJobExecutionException()
    {
        // Arrange
        var request = new TestRequest("test-data");
        var error = EncinaErrors.Create("test.error", "Test error message");
        _context.JobDetail.JobDataMap.Put(QuartzConstants.RequestKey, request);
        _Encina.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, TestResponse>(error));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<JobExecutionException>(() => _job.Execute(_context));
        exception.Message.Should().Be("Test error message");
    }

    [Fact]
    public async Task Execute_WithMissingRequest_ThrowsJobExecutionException()
    {
        // Arrange - Don't add request to JobDataMap

        // Act & Assert
        var exception = await Assert.ThrowsAsync<JobExecutionException>(() => _job.Execute(_context));
        exception.Message.Should().Contain("TestRequest");
        exception.Message.Should().Contain("not found in JobDataMap");
    }

    // TODO: LoggerMessage delegates are not compatible with NSubstitute verification
    // These tests need refactoring to use a different verification approach (Issue #6)
    [Fact(Skip = "Issue #6: LoggerMessage delegates incompatible with NSubstitute.Received()")]
    public async Task Execute_LogsExecutionStart()
    {
        // Arrange
        var request = new TestRequest("test-data");
        _context.JobDetail.JobDataMap.Put(QuartzConstants.RequestKey, request);
        _Encina.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, TestResponse>(new TestResponse("success")));

        // Act
        await _job.Execute(_context);

        // Assert
        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Executing Quartz job")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact(Skip = "Issue #6: LoggerMessage delegates incompatible with NSubstitute.Received()")]
    public async Task Execute_OnSuccess_LogsCompletion()
    {
        // Arrange
        var request = new TestRequest("test-data");
        _context.JobDetail.JobDataMap.Put(QuartzConstants.RequestKey, request);
        _Encina.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, TestResponse>(new TestResponse("success")));

        // Act
        await _job.Execute(_context);

        // Assert
        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("completed successfully")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact(Skip = "Issue #6: LoggerMessage delegates incompatible with NSubstitute.Received()")]
    public async Task Execute_OnFailure_LogsError()
    {
        // Arrange
        var request = new TestRequest("test-data");
        var error = EncinaErrors.Create("test.error", "Test error");
        _context.JobDetail.JobDataMap.Put(QuartzConstants.RequestKey, request);
        _Encina.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, TestResponse>(error));

        // Act
        try
        {
            await _job.Execute(_context);
        }
        catch (JobExecutionException)
        {
            // Expected
        }

        // Assert
        _logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("failed")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact(Skip = "Issue #6: LoggerMessage delegates incompatible with NSubstitute.Received()")]
    public async Task Execute_WhenExceptionThrown_LogsAndWrapsInJobExecutionException()
    {
        // Arrange
        var request = new TestRequest("test-data");
        var exception = new InvalidOperationException("Test exception");
        _context.JobDetail.JobDataMap.Put(QuartzConstants.RequestKey, request);
        _Encina.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns<Either<EncinaError, TestResponse>>(_ => throw exception);

        // Act & Assert
        var jobException = await Assert.ThrowsAsync<JobExecutionException>(() => _job.Execute(_context));
        jobException.InnerException.Should().Be(exception);

        _logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Unhandled exception")),
            exception,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Execute_PassesCancellationToken()
    {
        // Arrange
        var request = new TestRequest("test-data");
        var cts = new CancellationTokenSource();
        _context.JobDetail.JobDataMap.Put(QuartzConstants.RequestKey, request);
        _context.CancellationToken.Returns(cts.Token);
        _Encina.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, TestResponse>(new TestResponse("success")));

        // Act
        await _job.Execute(_context);

        // Assert
        await _Encina.Received(1).Send(
            Arg.Any<TestRequest>(),
            Arg.Is<CancellationToken>(ct => ct == cts.Token));
    }

    // Test types
    public record TestRequest(string Data) : IRequest<TestResponse>;
    public record TestResponse(string Result);
}
