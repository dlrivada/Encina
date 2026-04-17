using Encina.Quartz;
using Quartz;

namespace Encina.UnitTests.Quartz;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/> ScheduleRequest, ScheduleNotification,
/// AddRequestJob, and AddNotificationJob extension methods.
/// </summary>
public class ServiceCollectionExtensionsSchedulingTests
{
    [Fact]
    public async Task ScheduleRequest_WithCustomJobKey_SchedulesJob()
    {
        // Arrange
        var scheduler = Substitute.For<IScheduler>();
        var request = new TestScheduleRequest("data");
        var trigger = TriggerBuilder.Create()
            .WithIdentity("test-trigger")
            .StartNow()
            .Build();
        var jobKey = new JobKey("custom-job-key");

        // Act
        var triggerKey = await scheduler.ScheduleRequest<TestScheduleRequest, TestScheduleResponse>(
            request, trigger, jobKey);

        // Assert
        triggerKey.ShouldBe(trigger.Key);
        await scheduler.Received(1).ScheduleJob(
            Arg.Is<IJobDetail>(j => j.Key == jobKey
                && j.JobDataMap[QuartzConstants.RequestKey] == (object)request),
            Arg.Is<ITrigger>(t => t.Key == trigger.Key),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScheduleRequest_WithoutJobKey_GeneratesKey()
    {
        // Arrange
        var scheduler = Substitute.For<IScheduler>();
        var request = new TestScheduleRequest("data");
        var trigger = TriggerBuilder.Create()
            .WithIdentity("auto-trigger")
            .StartNow()
            .Build();

        // Act
        var triggerKey = await scheduler.ScheduleRequest<TestScheduleRequest, TestScheduleResponse>(
            request, trigger);

        // Assert
        triggerKey.ShouldBe(trigger.Key);
        await scheduler.Received(1).ScheduleJob(
            Arg.Is<IJobDetail>(j => j.Key.Name.StartsWith("Request-TestScheduleRequest-")),
            Arg.Any<ITrigger>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScheduleNotification_WithCustomJobKey_SchedulesJob()
    {
        // Arrange
        var scheduler = Substitute.For<IScheduler>();
        var notification = new TestScheduleNotification("msg");
        var trigger = TriggerBuilder.Create()
            .WithIdentity("notif-trigger")
            .StartNow()
            .Build();
        var jobKey = new JobKey("custom-notif-key");

        // Act
        var triggerKey = await scheduler.ScheduleNotification(
            notification, trigger, jobKey);

        // Assert
        triggerKey.ShouldBe(trigger.Key);
        await scheduler.Received(1).ScheduleJob(
            Arg.Is<IJobDetail>(j => j.Key == jobKey
                && j.JobDataMap[QuartzConstants.NotificationKey] == (object)notification),
            Arg.Any<ITrigger>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScheduleNotification_WithoutJobKey_GeneratesKey()
    {
        // Arrange
        var scheduler = Substitute.For<IScheduler>();
        var notification = new TestScheduleNotification("msg");
        var trigger = TriggerBuilder.Create()
            .WithIdentity("auto-notif-trigger")
            .StartNow()
            .Build();

        // Act
        var triggerKey = await scheduler.ScheduleNotification(notification, trigger);

        // Assert
        triggerKey.ShouldBe(trigger.Key);
        await scheduler.Received(1).ScheduleJob(
            Arg.Is<IJobDetail>(j => j.Key.Name.StartsWith("Notification-TestScheduleNotification-")),
            Arg.Any<ITrigger>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void AddEncinaQuartz_WithNullConfigure_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaQuartz(configure: null, configureOptions: null);

        // Assert
        result.ShouldBe(services);
    }

    // Test types
    public record TestScheduleRequest(string Data) : IRequest<TestScheduleResponse>;
    public record TestScheduleResponse(string Result);
    public record TestScheduleNotification(string Message) : INotification;
}
