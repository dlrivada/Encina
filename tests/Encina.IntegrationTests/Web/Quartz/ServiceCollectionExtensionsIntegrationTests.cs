using System.Collections.Specialized;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Quartz.Impl;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.IntegrationTests.Web.Quartz;

/// <summary>
/// Integration tests for Quartz ServiceCollectionExtensions.
/// Tests the extension methods with a real Quartz scheduler.
/// </summary>
[Trait("Category", "Integration")]
[Collection("Quartz ServiceExtensions")]
public sealed class ServiceCollectionExtensionsIntegrationTests : IAsyncLifetime
{
    private IScheduler? _scheduler;
    private static readonly string SchedulerName = $"ServiceExtTests_{Guid.NewGuid():N}";

    public async ValueTask InitializeAsync()
    {
        // Use unique scheduler name to avoid conflicts with other tests
        var properties = new NameValueCollection
        {
            ["quartz.scheduler.instanceName"] = SchedulerName,
            ["quartz.threadPool.threadCount"] = "3",
            ["quartz.jobStore.type"] = "Quartz.Simpl.RAMJobStore, Quartz"
        };
        var factory = new StdSchedulerFactory(properties);
        _scheduler = await factory.GetScheduler();
        await _scheduler.Start();
    }

    public async ValueTask DisposeAsync()
    {
        if (_scheduler != null)
        {
            await _scheduler.Shutdown(waitForJobsToComplete: false);
        }
    }

    [Fact]
    public async Task ScheduleRequest_WithValidRequest_ReturnsTriggerKey()
    {
        // Arrange
        var request = new IntegrationTestRequest("schedule-request-test");
        var trigger = TriggerBuilder.Create()
            .WithIdentity("test-trigger-1")
            .StartAt(DateTimeOffset.UtcNow.AddHours(1))
            .Build();

        // Act
        var triggerKey = await _scheduler!.ScheduleRequest<IntegrationTestRequest, string>(request, trigger);

        // Assert
        triggerKey.ShouldNotBeNull();
        triggerKey.Name.ShouldBe("test-trigger-1");
    }

    [Fact]
    public async Task ScheduleRequest_WithCustomJobKey_UsesProvidedKey()
    {
        // Arrange
        var request = new IntegrationTestRequest("custom-key-test");
        var jobKey = new JobKey("custom-job-key");
        var trigger = TriggerBuilder.Create()
            .WithIdentity("test-trigger-2")
            .StartAt(DateTimeOffset.UtcNow.AddHours(1))
            .Build();

        // Act
        await _scheduler!.ScheduleRequest<IntegrationTestRequest, string>(request, trigger, jobKey);

        // Assert
        var job = await _scheduler!.GetJobDetail(jobKey);
        job.ShouldNotBeNull();
    }

    [Fact]
    public async Task ScheduleNotification_WithValidNotification_ReturnsTriggerKey()
    {
        // Arrange
        var notification = new IntegrationTestNotification("schedule-notification-test");
        var trigger = TriggerBuilder.Create()
            .WithIdentity("test-trigger-3")
            .StartAt(DateTimeOffset.UtcNow.AddHours(1))
            .Build();

        // Act
        var triggerKey = await _scheduler!.ScheduleNotification(notification, trigger);

        // Assert
        triggerKey.ShouldNotBeNull();
        triggerKey.Name.ShouldBe("test-trigger-3");
    }

    [Fact]
    public async Task ScheduleNotification_WithCustomJobKey_UsesProvidedKey()
    {
        // Arrange
        var notification = new IntegrationTestNotification("custom-key-notification-test");
        var jobKey = new JobKey("custom-notification-job-key");
        var trigger = TriggerBuilder.Create()
            .WithIdentity("test-trigger-4")
            .StartAt(DateTimeOffset.UtcNow.AddHours(1))
            .Build();

        // Act
        await _scheduler!.ScheduleNotification(notification, trigger, jobKey);

        // Assert
        var job = await _scheduler!.GetJobDetail(jobKey);
        job.ShouldNotBeNull();
    }

    [Fact]
    public async Task ScheduleRequest_JobDataContainsRequest()
    {
        // Arrange
        var request = new IntegrationTestRequest("job-data-test");
        var jobKey = new JobKey("job-data-test-key");
        var trigger = TriggerBuilder.Create()
            .WithIdentity("test-trigger-5")
            .StartAt(DateTimeOffset.UtcNow.AddHours(1))
            .Build();

        // Act
        await _scheduler!.ScheduleRequest<IntegrationTestRequest, string>(request, trigger, jobKey);
        var jobDetail = await _scheduler!.GetJobDetail(jobKey);

        // Assert
        jobDetail.ShouldNotBeNull();
        jobDetail!.JobDataMap.ContainsKey(QuartzConstants.RequestKey).ShouldBeTrue();
    }

    [Fact]
    public async Task ScheduleNotification_JobDataContainsNotification()
    {
        // Arrange
        var notification = new IntegrationTestNotification("notification-data-test");
        var jobKey = new JobKey("notification-data-test-key");
        var trigger = TriggerBuilder.Create()
            .WithIdentity("test-trigger-6")
            .StartAt(DateTimeOffset.UtcNow.AddHours(1))
            .Build();

        // Act
        await _scheduler!.ScheduleNotification(notification, trigger, jobKey);
        var jobDetail = await _scheduler!.GetJobDetail(jobKey);

        // Assert
        jobDetail.ShouldNotBeNull();
        jobDetail!.JobDataMap.ContainsKey(QuartzConstants.NotificationKey).ShouldBeTrue();
    }

    [Fact]
    public async Task GetScheduler_ReturnsStartedScheduler()
    {
        // Assert
        _scheduler.ShouldNotBeNull();
        _scheduler!.IsStarted.ShouldBeTrue();
    }

    [Fact]
    public async Task ScheduleRequest_WithoutJobKey_GeneratesUniqueKey()
    {
        // Arrange
        var request = new IntegrationTestRequest("auto-key-test");
        var trigger = TriggerBuilder.Create()
            .WithIdentity("test-trigger-7")
            .StartAt(DateTimeOffset.UtcNow.AddHours(1))
            .Build();

        // Act
        var triggerKey = await _scheduler!.ScheduleRequest<IntegrationTestRequest, string>(request, trigger);

        // Assert
        triggerKey.ShouldNotBeNull();
        var triggerState = await _scheduler!.GetTriggerState(triggerKey);
        triggerState.ShouldBe(TriggerState.Normal);
    }

    [Fact]
    public async Task ScheduleNotification_WithoutJobKey_GeneratesUniqueKey()
    {
        // Arrange
        var notification = new IntegrationTestNotification("auto-key-notification-test");
        var trigger = TriggerBuilder.Create()
            .WithIdentity("test-trigger-8")
            .StartAt(DateTimeOffset.UtcNow.AddHours(1))
            .Build();

        // Act
        var triggerKey = await _scheduler!.ScheduleNotification(notification, trigger);

        // Assert
        triggerKey.ShouldNotBeNull();
        var triggerState = await _scheduler!.GetTriggerState(triggerKey);
        triggerState.ShouldBe(TriggerState.Normal);
    }
}

// Test types for integration tests
public sealed record IntegrationTestRequest(string Data) : IRequest<string>;

public sealed class IntegrationTestRequestHandler : IRequestHandler<IntegrationTestRequest, string>
{
    public Task<Either<EncinaError, string>> Handle(
        IntegrationTestRequest request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Right<EncinaError, string>($"Processed: {request.Data}"));
    }
}

public sealed record IntegrationTestNotification(string Message) : INotification;

public sealed class IntegrationTestNotificationHandler : INotificationHandler<IntegrationTestNotification>
{
    public Task<Either<EncinaError, Unit>> Handle(
        IntegrationTestNotification notification,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Right<EncinaError, Unit>(unit));
    }
}
