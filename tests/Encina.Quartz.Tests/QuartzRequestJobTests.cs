using Encina.Quartz.Tests.Fakers;
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
    private readonly TestRequestFaker _requestFaker;
    private readonly TestResponseFaker _responseFaker;

    public QuartzRequestJobTests()
    {
        _encina = Substitute.For<IEncina>();
        _logger = new FakeLogger<QuartzRequestJob<TestRequest, TestResponse>>();
        _job = new QuartzRequestJob<TestRequest, TestResponse>(_encina, _logger);
        _context = Substitute.For<IJobExecutionContext>();
        _requestFaker = new TestRequestFaker();
        _responseFaker = new TestResponseFaker();

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
        var request = _requestFaker.WithData("test-data").Generate();
        var expectedResponse = _responseFaker.WithSuccess().Generate();
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
        var request = _requestFaker.WithData("test-data").Generate();
        var error = EncinaErrors.Create("test.error", "Test error message");
        _context.JobDetail.JobDataMap.Put(QuartzConstants.RequestKey, request);
        _encina.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, TestResponse>(error));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<JobExecutionException>(() => _job.Execute(_context));
        exception.Message.ShouldBe("Test error message");
    }

    [Fact]
    public async Task Execute_WithMissingRequest_ThrowsJobExecutionException()
    {
        // Arrange - Don't add request to JobDataMap

        // Act & Assert
        var exception = await Assert.ThrowsAsync<JobExecutionException>(() => _job.Execute(_context));
        exception.Message.ShouldContain("TestRequest");
        exception.Message.ShouldContain("not found in JobDataMap");
    }

    [Fact]
    public async Task Execute_LogsExecutionStart()
    {
        // Arrange
        var request = _requestFaker.Generate();
        _context.JobDetail.JobDataMap.Put(QuartzConstants.RequestKey, request);
        _encina.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, TestResponse>(_responseFaker.WithSuccess().Generate()));

        // Act
        await _job.Execute(_context);

        // Assert
        var logEntry = _logger.Collector.GetSnapshot()
            .FirstOrDefault(r => r.Message.Contains("Executing Quartz job"));
        logEntry.ShouldNotBeNull();
        logEntry!.Level.ShouldBe(LogLevel.Information);
    }

    [Fact]
    public async Task Execute_OnSuccess_LogsCompletion()
    {
        // Arrange
        var request = _requestFaker.Generate();
        _context.JobDetail.JobDataMap.Put(QuartzConstants.RequestKey, request);
        _encina.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, TestResponse>(_responseFaker.WithSuccess().Generate()));

        // Act
        await _job.Execute(_context);

        // Assert
        var logEntry = _logger.Collector.GetSnapshot()
            .FirstOrDefault(r => r.Message.Contains("completed successfully"));
        logEntry.ShouldNotBeNull();
        logEntry!.Level.ShouldBe(LogLevel.Information);
    }

    [Fact]
    public async Task Execute_OnFailure_LogsError()
    {
        // Arrange
        var request = _requestFaker.Generate();
        var error = EncinaErrors.Create("test.error", "Test error");
        _context.JobDetail.JobDataMap.Put(QuartzConstants.RequestKey, request);
        _encina.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, TestResponse>(error));

        // Act & Assert
        await Assert.ThrowsAsync<JobExecutionException>(async () => await _job.Execute(_context));

        // Assert
        var logEntry = _logger.Collector.GetSnapshot()
            .FirstOrDefault(r => r.Message.Contains("failed"));
        logEntry.ShouldNotBeNull();
        logEntry!.Level.ShouldBe(LogLevel.Error);
    }

    [Fact]
    public async Task Execute_WhenExceptionThrown_LogsAndWrapsInJobExecutionException()
    {
        // Arrange
        var request = _requestFaker.Generate();
        var exception = new InvalidOperationException("Test exception");
        _context.JobDetail.JobDataMap.Put(QuartzConstants.RequestKey, request);
        _encina.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns<Either<EncinaError, TestResponse>>(_ => throw exception);

        // Act & Assert
        var jobException = await Assert.ThrowsAsync<JobExecutionException>(() => _job.Execute(_context));
        jobException.InnerException.ShouldBe(exception);

        var logEntry = _logger.Collector.GetSnapshot()
            .FirstOrDefault(r => r.Message.Contains("Unhandled exception"));
        logEntry.ShouldNotBeNull();
        logEntry!.Level.ShouldBe(LogLevel.Error);
        logEntry.Exception.ShouldBe(exception);
    }

    [Fact]
    public async Task Execute_PassesCancellationToken()
    {
        // Arrange
        var request = _requestFaker.Generate();
        var cts = new CancellationTokenSource();
        _context.JobDetail.JobDataMap.Put(QuartzConstants.RequestKey, request);
        _context.CancellationToken.Returns(cts.Token);
        _encina.Send(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, TestResponse>(_responseFaker.WithSuccess().Generate()));

        // Act
        await _job.Execute(_context);

        // Assert
        await _encina.Received(1).Send(
            Arg.Any<TestRequest>(),
            Arg.Is<CancellationToken>(ct => ct == cts.Token));
    }
}
