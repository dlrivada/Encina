using LanguageExt;
using Microsoft.Extensions.Logging;
using Quartz;
using Shouldly;
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
        // Property: When Encina succeeds, job ALWAYS completes without exception

        var testNotifications = new[]
        {
            new TestNotification("message1"),
            new TestNotification("message2"),
            new TestNotification("message3"),
        };

        foreach (var notification in testNotifications)
        {
            // Arrange
            var Encina = Substitute.For<IEncina>();
            var logger = Substitute.For<ILogger<QuartzNotificationJob<TestNotification>>>();
            var job = new QuartzNotificationJob<TestNotification>(Encina, logger);
            var context = CreateJobExecutionContext(notification);

            Encina.Publish(notification, Arg.Any<CancellationToken>())
                .Returns(Right<EncinaError, Unit>(unit));

            // Act & Assert - Should not throw
            await job.Execute(context);

            await Encina.Received(1).Publish(notification, Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task Property_Idempotency_SameNotificationSameOutcome()
    {
        // Property: Same notification ALWAYS produces same outcome

        var notification = new TestNotification("idempotent-test");
        var Encina = Substitute.For<IEncina>();
        var logger = Substitute.For<ILogger<QuartzNotificationJob<TestNotification>>>();
        var job = new QuartzNotificationJob<TestNotification>(Encina, logger);

        Encina.Publish(notification, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(unit));

        // Act - Multiple publications
        await job.Execute(CreateJobExecutionContext(notification));
        await job.Execute(CreateJobExecutionContext(notification));
        await job.Execute(CreateJobExecutionContext(notification));

        // Assert - Encina invoked 3 times (once per call)
        await Encina.Received(3).Publish(notification, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Property_ConcurrentPublication_ThreadSafe()
    {
        // Property: Concurrent publications are thread-safe

        var Encina = Substitute.For<IEncina>();
        var logger = Substitute.For<ILogger<QuartzNotificationJob<TestNotification>>>();
        var job = new QuartzNotificationJob<TestNotification>(Encina, logger);

        Encina.Publish(Arg.Any<TestNotification>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(unit));

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
    public async Task Property_EncinaInvocation_AlwaysCalledExactlyOnce()
    {
        // Property: Encina ALWAYS invoked exactly once per publication

        var testNotifications = new[]
        {
            new TestNotification("notif1"),
            new TestNotification("notif2"),
            new TestNotification("notif3"),
        };

        foreach (var notification in testNotifications)
        {
            var Encina = Substitute.For<IEncina>();
            var logger = Substitute.For<ILogger<QuartzNotificationJob<TestNotification>>>();
            var job = new QuartzNotificationJob<TestNotification>(Encina, logger);
            var context = CreateJobExecutionContext(notification);

            Encina.Publish(notification, Arg.Any<CancellationToken>())
                .Returns(Right<EncinaError, Unit>(unit));

            // Act
            await job.Execute(context);

            // Assert
            await Encina.Received(1).Publish(notification, Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task Property_MissingNotification_AlwaysThrowsJobExecutionException()
    {
        // Property: Missing notification ALWAYS throws JobExecutionException

        var Encina = Substitute.For<IEncina>();
        var logger = Substitute.For<ILogger<QuartzNotificationJob<TestNotification>>>();
        var job = new QuartzNotificationJob<TestNotification>(Encina, logger);
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
