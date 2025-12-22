using Encina.Quartz;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Quartz;
using static LanguageExt.Prelude;

namespace Encina.Quartz.ContractTests;

/// <summary>
/// Contract tests for QuartzNotificationJob.
/// Verifies that the job correctly implements its contract.
/// </summary>
public sealed class QuartzNotificationJobContractTests
{
    [Fact]
    public async Task Execute_WithValidNotification_ShouldInvokeMediatorPublish()
    {
        // Arrange
        var mediator = Substitute.For<IMediator>();
        var logger = Substitute.For<ILogger<QuartzNotificationJob<TestNotification>>>();
        var job = new QuartzNotificationJob<TestNotification>(mediator, logger);
        var notification = new TestNotification("test message");

        var context = CreateJobExecutionContext(notification);

        mediator.Publish(notification, Arg.Any<CancellationToken>())
            .Returns(Right<MediatorError, Unit>(unit));

        // Act
        await job.Execute(context);

        // Assert
        await mediator.Received(1).Publish(notification, Arg.Any<CancellationToken>());
    }


    [Fact]
    public async Task Execute_WithMissingNotification_ShouldThrowJobExecutionException()
    {
        // Arrange
        var mediator = Substitute.For<IMediator>();
        var logger = Substitute.For<ILogger<QuartzNotificationJob<TestNotification>>>();
        var job = new QuartzNotificationJob<TestNotification>(mediator, logger);

        var context = CreateJobExecutionContext<TestNotification>(null); // No notification in JobDataMap

        // Act & Assert
        var exception = await Assert.ThrowsAsync<JobExecutionException>(() =>
            job.Execute(context));

        exception.Message.Should().Contain("not found in JobDataMap");
    }

    // Helper methods
    private static IJobExecutionContext CreateJobExecutionContext<TNotification>(TNotification? notification)
        where TNotification : class
    {
        var context = Substitute.For<IJobExecutionContext>();
        var jobDetail = Substitute.For<IJobDetail>();
        var jobDataMap = new JobDataMap();

        if (notification is not null)
        {
            jobDataMap.Put(QuartzConstants.NotificationKey, notification);
        }

        jobDetail.JobDataMap.Returns(jobDataMap);
        jobDetail.Key.Returns(new JobKey("test-job"));
        context.JobDetail.Returns(jobDetail);
        context.CancellationToken.Returns(CancellationToken.None);

        return context;
    }

}

// Test types (must be public for NSubstitute proxying with strong-named assemblies)
public sealed record TestNotification(string Message) : INotification;
