using Encina.Quartz.Tests.Fakers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Quartz;

namespace Encina.Quartz.Tests;

public class QuartzNotificationJobTests
{
    private readonly IEncina _encina;
    private readonly FakeLogger<QuartzNotificationJob<TestNotification>> _logger;
    private readonly QuartzNotificationJob<TestNotification> _job;
    private readonly IJobExecutionContext _context;
    private readonly TestNotificationFaker _notificationFaker;

    public QuartzNotificationJobTests()
    {
        _encina = Substitute.For<IEncina>();
        _logger = new FakeLogger<QuartzNotificationJob<TestNotification>>();
        _job = new QuartzNotificationJob<TestNotification>(_encina, _logger);
        _context = Substitute.For<IJobExecutionContext>();
        _notificationFaker = new TestNotificationFaker();

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
        var notification = _notificationFaker.WithMessage("test-message").Generate();
        _context.JobDetail.JobDataMap.Put(QuartzConstants.NotificationKey, notification);

        // Act
        await _job.Execute(_context);

        // Assert
        await _encina.Received(1).Publish(
            Arg.Is<TestNotification>(n => n.Message == "test-message"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_WithMissingNotification_ThrowsJobExecutionException()
    {
        // Arrange - Don't add notification to JobDataMap

        // Act & Assert
        var exception = await Assert.ThrowsAsync<JobExecutionException>(() => _job.Execute(_context));
        exception.Message.ShouldContain("TestNotification");
        exception.Message.ShouldContain("not found in JobDataMap");
    }

    [Fact]
    public async Task Execute_LogsPublishingStart()
    {
        // Arrange
        var notification = _notificationFaker.Generate();
        _context.JobDetail.JobDataMap.Put(QuartzConstants.NotificationKey, notification);

        // Act
        await _job.Execute(_context);

        // Assert
        var logEntry = _logger.Collector.GetSnapshot()
            .FirstOrDefault(r => r.Message.Contains("Publishing Quartz notification"));
        logEntry.ShouldNotBeNull();
        logEntry!.Level.ShouldBe(LogLevel.Information);
    }

    [Fact]
    public async Task Execute_OnSuccess_LogsCompletion()
    {
        // Arrange
        var notification = _notificationFaker.Generate();
        _context.JobDetail.JobDataMap.Put(QuartzConstants.NotificationKey, notification);

        // Act
        await _job.Execute(_context);

        // Assert
        var logEntry = _logger.Collector.GetSnapshot()
            .FirstOrDefault(r => r.Message.Contains("completed successfully"));
        logEntry.ShouldNotBeNull();
        logEntry!.Level.ShouldBe(LogLevel.Information);
    }

    [Fact]
    public async Task Execute_WhenExceptionThrown_LogsAndWrapsInJobExecutionException()
    {
        // Arrange
        var notification = _notificationFaker.Generate();
        var exception = new InvalidOperationException("Test exception");
        _context.JobDetail.JobDataMap.Put(QuartzConstants.NotificationKey, notification);
        _encina.When(m => m.Publish(Arg.Any<TestNotification>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw exception);

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
        var notification = _notificationFaker.Generate();
        var cts = new CancellationTokenSource();
        _context.JobDetail.JobDataMap.Put(QuartzConstants.NotificationKey, notification);
        _context.CancellationToken.Returns(cts.Token);

        // Act
        await _job.Execute(_context);

        // Assert
        await _encina.Received(1).Publish(
            Arg.Any<TestNotification>(),
            Arg.Is<CancellationToken>(ct => ct == cts.Token));
    }
}
