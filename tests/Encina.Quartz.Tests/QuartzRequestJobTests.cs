using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Quartz;
using static LanguageExt.Prelude;

namespace Encina.Quartz.Tests;

public class QuartzRequestJobTests
{
    private readonly IEncina _encina;
    private readonly FakeLogger<QuartzRequestJob<TestRequest, TestResponse>> _logger;
    private readonly QuartzRequestJob<TestRequest, TestResponse> _job;
    private readonly IJobExecutionContext _context;

    public QuartzRequestJobTests()
    {
        _encina = Substitute.For<IEncina>();
        _logger = new FakeLogger<QuartzRequestJob<TestRequest, TestResponse>>();
        _job = new QuartzRequestJob<TestRequest, TestResponse>(_encina, _logger);
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
        _encina.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, TestResponse>(expectedResponse));

        // Act
        await _job.Execute(_context);

        // Assert
        await _encina.Received(1).Send(
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
        _encina.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
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

    [Fact]
    public async Task Execute_LogsExecutionStart()
    {
        // Arrange
        var request = new TestRequest("test-data");
        _context.JobDetail.JobDataMap.Put(QuartzConstants.RequestKey, request);
        _encina.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, TestResponse>(new TestResponse("success")));

        // Act
        await _job.Execute(_context);

        // Assert
        var logEntry = _logger.Collector.GetSnapshot()
            .FirstOrDefault(r => r.Message.Contains("Executing Quartz job"));
        logEntry.Should().NotBeNull();
        logEntry!.Level.Should().Be(LogLevel.Information);
    }

    [Fact]
    public async Task Execute_OnSuccess_LogsCompletion()
    {
        // Arrange
        var request = new TestRequest("test-data");
        _context.JobDetail.JobDataMap.Put(QuartzConstants.RequestKey, request);
        _encina.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, TestResponse>(new TestResponse("success")));

        // Act
        await _job.Execute(_context);

        // Assert
        var logEntry = _logger.Collector.GetSnapshot()
            .FirstOrDefault(r => r.Message.Contains("completed successfully"));
        logEntry.Should().NotBeNull();
        logEntry!.Level.Should().Be(LogLevel.Information);
    }

    [Fact]
    public async Task Execute_OnFailure_LogsError()
    {
        // Arrange
        var request = new TestRequest("test-data");
        var error = EncinaErrors.Create("test.error", "Test error");
        _context.JobDetail.JobDataMap.Put(QuartzConstants.RequestKey, request);
        _encina.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
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
        var logEntry = _logger.Collector.GetSnapshot()
            .FirstOrDefault(r => r.Message.Contains("failed"));
        logEntry.Should().NotBeNull();
        logEntry!.Level.Should().Be(LogLevel.Error);
    }

    [Fact]
    public async Task Execute_WhenExceptionThrown_LogsAndWrapsInJobExecutionException()
    {
        // Arrange
        var request = new TestRequest("test-data");
        var exception = new InvalidOperationException("Test exception");
        _context.JobDetail.JobDataMap.Put(QuartzConstants.RequestKey, request);
        _encina.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns<Either<EncinaError, TestResponse>>(_ => throw exception);

        // Act & Assert
        var jobException = await Assert.ThrowsAsync<JobExecutionException>(() => _job.Execute(_context));
        jobException.InnerException.Should().Be(exception);

        var logEntry = _logger.Collector.GetSnapshot()
            .FirstOrDefault(r => r.Message.Contains("Unhandled exception"));
        logEntry.Should().NotBeNull();
        logEntry!.Level.Should().Be(LogLevel.Error);
        logEntry.Exception.Should().Be(exception);
    }

    [Fact]
    public async Task Execute_PassesCancellationToken()
    {
        // Arrange
        var request = new TestRequest("test-data");
        var cts = new CancellationTokenSource();
        _context.JobDetail.JobDataMap.Put(QuartzConstants.RequestKey, request);
        _context.CancellationToken.Returns(cts.Token);
        _encina.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, TestResponse>(new TestResponse("success")));

        // Act
        await _job.Execute(_context);

        // Assert
        await _encina.Received(1).Send(
            Arg.Any<TestRequest>(),
            Arg.Is<CancellationToken>(ct => ct == cts.Token));
    }

    // Test types
    public record TestRequest(string Data) : IRequest<TestResponse>;
    public record TestResponse(string Result);
}
