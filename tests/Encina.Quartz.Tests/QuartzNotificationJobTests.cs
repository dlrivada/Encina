using Encina.Quartz;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Encina.Quartz.Tests;

public class QuartzNotificationJobTests
{
    private readonly IEncina _Encina;
    private readonly ILogger<QuartzNotificationJob<TestNotification>> _logger;
    private readonly QuartzNotificationJob<TestNotification> _job;
    private readonly IJobExecutionContext _context;

    public QuartzNotificationJobTests()
    {
        _Encina = Substitute.For<IEncina>();
        _logger = Substitute.For<ILogger<QuartzNotificationJob<TestNotification>>>();
        _job = new QuartzNotificationJob<TestNotification>(_Encina, _logger);
        _context = Substitute.For<IJobExecutionContext>();

        // Setup default JobDataMap
        var jobDetail = Substitute.For<IJobDetail>();
        var jobDataMap = new JobDataMap();
        jobDetail.JobDataMap.Returns(jobDataMap);
        jobDetail.Key.Returns(new JobKey("test-notification-job"));
        _context.JobDetail.Returns(jobDetail);
    }

    [Fact]
    public async Task Execute_PublishesNotification()
    {
        // Arrange
        var notification = new TestNotification("test-message");
        _context.JobDetail.JobDataMap.Put(QuartzConstants.NotificationKey, notification);

        // Act
        await _job.Execute(_context);

        // Assert
        await _Encina.Received(1).Publish(
            Arg.Is<TestNotification>(n => n.Message == "test-message"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_WithMissingNotification_ThrowsJobExecutionException()
    {
        // Arrange - Don't add notification to JobDataMap

        // Act & Assert
        var exception = await Assert.ThrowsAsync<JobExecutionException>(() => _job.Execute(_context));
        exception.Message.Should().Contain("TestNotification");
        exception.Message.Should().Contain("not found in JobDataMap");
    }

    // TODO: LoggerMessage delegates are not compatible with NSubstitute verification
    // These tests need refactoring to use a different verification approach (Issue #6)
    [Fact(Skip = "Issue #6: LoggerMessage delegates incompatible with NSubstitute.Received()")]
    public async Task Execute_LogsPublishingStart()
    {
        // Arrange
        var notification = new TestNotification("test-message");
        _context.JobDetail.JobDataMap.Put(QuartzConstants.NotificationKey, notification);

        // Act
        await _job.Execute(_context);

        // Assert
        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Publishing Quartz notification")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact(Skip = "Issue #6: LoggerMessage delegates incompatible with NSubstitute.Received()")]
    public async Task Execute_OnSuccess_LogsCompletion()
    {
        // Arrange
        var notification = new TestNotification("test-message");
        _context.JobDetail.JobDataMap.Put(QuartzConstants.NotificationKey, notification);

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
    public async Task Execute_WhenExceptionThrown_LogsAndWrapsInJobExecutionException()
    {
        // Arrange
        var notification = new TestNotification("test-message");
        var exception = new InvalidOperationException("Test exception");
        _context.JobDetail.JobDataMap.Put(QuartzConstants.NotificationKey, notification);
        _Encina.When(m => m.Publish(Arg.Any<TestNotification>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw exception);

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
        var notification = new TestNotification("test-message");
        var cts = new CancellationTokenSource();
        _context.JobDetail.JobDataMap.Put(QuartzConstants.NotificationKey, notification);
        _context.CancellationToken.Returns(cts.Token);

        // Act
        await _job.Execute(_context);

        // Assert
        await _Encina.Received(1).Publish(
            Arg.Any<TestNotification>(),
            Arg.Is<CancellationToken>(ct => ct == cts.Token));
    }

    // Test type
    public record TestNotification(string Message) : INotification;
}
