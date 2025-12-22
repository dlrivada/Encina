using LanguageExt;
using Microsoft.Extensions.Logging;
using Quartz;
using Shouldly;
using Encina.Quartz;
using static LanguageExt.Prelude;

namespace Encina.Quartz.PropertyTests;

/// <summary>
/// Property-based tests for QuartzNotificationJob.
/// Verifies invariants hold across different scenarios.
/// </summary>
public sealed class QuartzNotificationJobPropertyTests
{
    [Fact]
    public async Task Property_SuccessfulPublication_AlwaysCompletes()
    {
        // Property: When mediator succeeds, job ALWAYS completes without exception

        var testNotifications = new[]
        {
            new TestNotification("message1"),
            new TestNotification("message2"),
            new TestNotification("message3"),
        };

        foreach (var notification in testNotifications)
        {
            // Arrange
            var mediator = Substitute.For<IMediator>();
            var logger = Substitute.For<ILogger<QuartzNotificationJob<TestNotification>>>();
            var job = new QuartzNotificationJob<TestNotification>(mediator, logger);
            var context = CreateJobExecutionContext(notification);

            mediator.Publish(notification, Arg.Any<CancellationToken>())
                .Returns(Right<MediatorError, Unit>(unit));

            // Act & Assert - Should not throw
            await job.Execute(context);

            await mediator.Received(1).Publish(notification, Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task Property_Idempotency_SameNotificationSameOutcome()
    {
        // Property: Same notification ALWAYS produces same outcome

        var notification = new TestNotification("idempotent-test");
        var mediator = Substitute.For<IMediator>();
        var logger = Substitute.For<ILogger<QuartzNotificationJob<TestNotification>>>();
        var job = new QuartzNotificationJob<TestNotification>(mediator, logger);

        mediator.Publish(notification, Arg.Any<CancellationToken>())
            .Returns(Right<MediatorError, Unit>(unit));

        // Act - Multiple publications
        await job.Execute(CreateJobExecutionContext(notification));
        await job.Execute(CreateJobExecutionContext(notification));
        await job.Execute(CreateJobExecutionContext(notification));

        // Assert - Mediator invoked 3 times (once per call)
        await mediator.Received(3).Publish(notification, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Property_ConcurrentPublication_ThreadSafe()
    {
        // Property: Concurrent publications are thread-safe

        var mediator = Substitute.For<IMediator>();
        var logger = Substitute.For<ILogger<QuartzNotificationJob<TestNotification>>>();
        var job = new QuartzNotificationJob<TestNotification>(mediator, logger);

        mediator.Publish(Arg.Any<TestNotification>(), Arg.Any<CancellationToken>())
            .Returns(Right<MediatorError, Unit>(unit));

        // Act - Execute concurrently
        var tasks = Enumerable.Range(0, 20)
            .Select(i => Task.Run(async () =>
            {
                var context = CreateJobExecutionContext(new TestNotification($"msg-{i}"));
                await job.Execute(context);
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert - All calls complete without exception
        tasks.All(t => t.IsCompletedSuccessfully).ShouldBeTrue();
    }

    [Fact]
    public async Task Property_MediatorInvocation_AlwaysCalledExactlyOnce()
    {
        // Property: Mediator ALWAYS invoked exactly once per publication

        var testNotifications = new[]
        {
            new TestNotification("notif1"),
            new TestNotification("notif2"),
            new TestNotification("notif3"),
        };

        foreach (var notification in testNotifications)
        {
            var mediator = Substitute.For<IMediator>();
            var logger = Substitute.For<ILogger<QuartzNotificationJob<TestNotification>>>();
            var job = new QuartzNotificationJob<TestNotification>(mediator, logger);
            var context = CreateJobExecutionContext(notification);

            mediator.Publish(notification, Arg.Any<CancellationToken>())
                .Returns(Right<MediatorError, Unit>(unit));

            // Act
            await job.Execute(context);

            // Assert
            await mediator.Received(1).Publish(notification, Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task Property_MissingNotification_AlwaysThrowsJobExecutionException()
    {
        // Property: Missing notification ALWAYS throws JobExecutionException

        var mediator = Substitute.For<IMediator>();
        var logger = Substitute.For<ILogger<QuartzNotificationJob<TestNotification>>>();
        var job = new QuartzNotificationJob<TestNotification>(mediator, logger);
        var context = CreateJobExecutionContext<TestNotification>(null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<JobExecutionException>(() => job.Execute(context));
        exception.Message.ShouldContain("not found in JobDataMap");
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

// Test types
public sealed record TestNotification(string Message) : INotification;
