using Encina.Hangfire;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;

namespace Encina.UnitTests.Hangfire;

/// <summary>
/// Unit tests for Hangfire client extension methods (EnqueueRequest, ScheduleRequest, etc.).
/// </summary>
public class ServiceCollectionExtensionsHangfireClientTests
{
    [Fact]
    public void EnqueueRequest_CallsClientCreate()
    {
        // Arrange
        var client = Substitute.For<IBackgroundJobClient>();
        client.Create(Arg.Any<Job>(), Arg.Any<IState>())
            .Returns("job-1");

        var request = new TestHfRequest("data");

        // Act
        var jobId = client.EnqueueRequest<TestHfRequest, TestHfResponse>(request);

        // Assert
        jobId.ShouldBe("job-1");
        client.Received(1).Create(Arg.Any<Job>(), Arg.Any<IState>());
    }

    [Fact]
    public void ScheduleRequestWithDelay_CallsClientCreate()
    {
        // Arrange
        var client = Substitute.For<IBackgroundJobClient>();
        client.Create(Arg.Any<Job>(), Arg.Any<IState>())
            .Returns("job-2");

        var request = new TestHfRequest("delayed");

        // Act
        var jobId = client.ScheduleRequestWithDelay<TestHfRequest, TestHfResponse>(
            request, TimeSpan.FromMinutes(5));

        // Assert
        jobId.ShouldBe("job-2");
    }

    [Fact]
    public void ScheduleRequestAt_CallsClientCreate()
    {
        // Arrange
        var client = Substitute.For<IBackgroundJobClient>();
        client.Create(Arg.Any<Job>(), Arg.Any<IState>())
            .Returns("job-3");

        var request = new TestHfRequest("scheduled");

        // Act
        var jobId = client.ScheduleRequestAt<TestHfRequest, TestHfResponse>(
            request, DateTimeOffset.UtcNow.AddHours(1));

        // Assert
        jobId.ShouldBe("job-3");
    }

    [Fact]
    public void EnqueueNotification_CallsClientCreate()
    {
        // Arrange
        var client = Substitute.For<IBackgroundJobClient>();
        client.Create(Arg.Any<Job>(), Arg.Any<IState>())
            .Returns("job-4");

        var notification = new TestHfNotification("event");

        // Act
        var jobId = client.EnqueueNotification(notification);

        // Assert
        jobId.ShouldBe("job-4");
    }

    [Fact]
    public void ScheduleNotificationWithDelay_CallsClientCreate()
    {
        // Arrange
        var client = Substitute.For<IBackgroundJobClient>();
        client.Create(Arg.Any<Job>(), Arg.Any<IState>())
            .Returns("job-5");

        var notification = new TestHfNotification("delayed-event");

        // Act
        var jobId = client.ScheduleNotificationWithDelay(notification, TimeSpan.FromMinutes(10));

        // Assert
        jobId.ShouldBe("job-5");
    }

    [Fact]
    public void ScheduleNotificationAt_CallsClientCreate()
    {
        // Arrange
        var client = Substitute.For<IBackgroundJobClient>();
        client.Create(Arg.Any<Job>(), Arg.Any<IState>())
            .Returns("job-6");

        var notification = new TestHfNotification("at-event");

        // Act
        var jobId = client.ScheduleNotificationAt(notification, DateTimeOffset.UtcNow.AddHours(2));

        // Assert
        jobId.ShouldBe("job-6");
    }

    [Fact]
    public void AddOrUpdateRecurringRequest_CallsManager()
    {
        // Arrange
        var manager = Substitute.For<IRecurringJobManager>();
        var request = new TestHfRequest("recurring");

        // Act
        manager.AddOrUpdateRecurringRequest<TestHfRequest, TestHfResponse>(
            "recurring-job-1", request, Cron.Daily());

        // Assert
        manager.Received(1).AddOrUpdate(
            Arg.Is<string>(id => id == "recurring-job-1"),
            Arg.Any<Job>(),
            Arg.Is<string>(c => c == Cron.Daily()),
            Arg.Any<RecurringJobOptions>());
    }

    [Fact]
    public void AddOrUpdateRecurringNotification_CallsManager()
    {
        // Arrange
        var manager = Substitute.For<IRecurringJobManager>();
        var notification = new TestHfNotification("recurring-notif");

        // Act
        manager.AddOrUpdateRecurringNotification(
            "recurring-notif-1", notification, Cron.Hourly());

        // Assert
        manager.Received(1).AddOrUpdate(
            Arg.Is<string>(id => id == "recurring-notif-1"),
            Arg.Any<Job>(),
            Arg.Is<string>(c => c == Cron.Hourly()),
            Arg.Any<RecurringJobOptions>());
    }

    [Fact]
    public void AddOrUpdateRecurringRequest_WithCustomOptions_PassesOptions()
    {
        // Arrange
        var manager = Substitute.For<IRecurringJobManager>();
        var request = new TestHfRequest("custom-opts");
        var options = new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc };

        // Act
        manager.AddOrUpdateRecurringRequest<TestHfRequest, TestHfResponse>(
            "custom-opts-job", request, Cron.Weekly(), options);

        // Assert
        manager.Received(1).AddOrUpdate(
            Arg.Any<string>(),
            Arg.Any<Job>(),
            Arg.Any<string>(),
            Arg.Is<RecurringJobOptions>(o => o.TimeZone == TimeZoneInfo.Utc));
    }

    // Test types
    public record TestHfRequest(string Data) : IRequest<TestHfResponse>;
    public record TestHfResponse(string Result);
    public record TestHfNotification(string Message) : INotification;
}
