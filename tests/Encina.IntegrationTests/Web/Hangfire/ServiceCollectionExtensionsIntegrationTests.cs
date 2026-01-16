using Hangfire;
using Hangfire.InMemory;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.IntegrationTests.Web.Hangfire;

/// <summary>
/// Integration tests for Hangfire ServiceCollectionExtensions.
/// Tests the extension methods with a real Hangfire InMemory storage.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ServiceCollectionExtensionsIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IBackgroundJobClient _jobClient;
    private readonly IRecurringJobManager _recurringJobManager;

    public ServiceCollectionExtensionsIntegrationTests()
    {
        var services = new ServiceCollection();

        // Setup Hangfire with InMemory storage
        services.AddHangfire(config => config.UseInMemoryStorage());
        services.AddHangfireServer();

        // Setup Encina
        services.AddEncina();
        services.AddEncinaHangfire();
        services.AddTransient<IRequestHandler<IntegrationTestRequest, string>, IntegrationTestRequestHandler>();
        services.AddTransient<INotificationHandler<IntegrationTestNotification>, IntegrationTestNotificationHandler>();

        _serviceProvider = services.BuildServiceProvider();
        _jobClient = _serviceProvider.GetRequiredService<IBackgroundJobClient>();
        _recurringJobManager = _serviceProvider.GetRequiredService<IRecurringJobManager>();
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }

    [Fact]
    public void EnqueueRequest_WithValidRequest_ReturnsJobId()
    {
        // Arrange
        var request = new IntegrationTestRequest("enqueue-test");

        // Act
        var jobId = _jobClient.EnqueueRequest<IntegrationTestRequest, string>(request);

        // Assert
        jobId.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void ScheduleRequestWithDelay_WithValidRequest_ReturnsJobId()
    {
        // Arrange
        var request = new IntegrationTestRequest("schedule-delay-test");

        // Act
        var jobId = _jobClient.ScheduleRequestWithDelay<IntegrationTestRequest, string>(
            request,
            TimeSpan.FromMinutes(5));

        // Assert
        jobId.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void ScheduleRequestAt_WithValidRequest_ReturnsJobId()
    {
        // Arrange
        var request = new IntegrationTestRequest("schedule-at-test");
        var enqueueAt = DateTimeOffset.UtcNow.AddHours(1);

        // Act
        var jobId = _jobClient.ScheduleRequestAt<IntegrationTestRequest, string>(request, enqueueAt);

        // Assert
        jobId.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void EnqueueNotification_WithValidNotification_ReturnsJobId()
    {
        // Arrange
        var notification = new IntegrationTestNotification("enqueue-notification-test");

        // Act
        var jobId = _jobClient.EnqueueNotification(notification);

        // Assert
        jobId.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void ScheduleNotificationWithDelay_WithValidNotification_ReturnsJobId()
    {
        // Arrange
        var notification = new IntegrationTestNotification("schedule-notification-delay-test");

        // Act
        var jobId = _jobClient.ScheduleNotificationWithDelay(notification, TimeSpan.FromMinutes(10));

        // Assert
        jobId.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void ScheduleNotificationAt_WithValidNotification_ReturnsJobId()
    {
        // Arrange
        var notification = new IntegrationTestNotification("schedule-notification-at-test");
        var enqueueAt = DateTimeOffset.UtcNow.AddHours(2);

        // Act
        var jobId = _jobClient.ScheduleNotificationAt(notification, enqueueAt);

        // Assert
        jobId.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void AddOrUpdateRecurringRequest_WithValidRequest_DoesNotThrow()
    {
        // Arrange
        var request = new IntegrationTestRequest("recurring-request-test");

        // Act
        var exception = Record.Exception(() =>
            _recurringJobManager.AddOrUpdateRecurringRequest<IntegrationTestRequest, string>(
                "test-recurring-request",
                request,
                Cron.Daily()));

        // Assert
        exception.ShouldBeNull();
    }

    [Fact]
    public void AddOrUpdateRecurringRequest_WithOptions_DoesNotThrow()
    {
        // Arrange
        var request = new IntegrationTestRequest("recurring-request-options-test");
        var options = new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc };

        // Act
        var exception = Record.Exception(() =>
            _recurringJobManager.AddOrUpdateRecurringRequest<IntegrationTestRequest, string>(
                "test-recurring-request-options",
                request,
                Cron.Hourly(),
                options));

        // Assert
        exception.ShouldBeNull();
    }

    [Fact]
    public void AddOrUpdateRecurringNotification_WithValidNotification_DoesNotThrow()
    {
        // Arrange
        var notification = new IntegrationTestNotification("recurring-notification-test");

        // Act
        var exception = Record.Exception(() =>
            _recurringJobManager.AddOrUpdateRecurringNotification(
                "test-recurring-notification",
                notification,
                Cron.Weekly()));

        // Assert
        exception.ShouldBeNull();
    }

    [Fact]
    public void AddOrUpdateRecurringNotification_WithOptions_DoesNotThrow()
    {
        // Arrange
        var notification = new IntegrationTestNotification("recurring-notification-options-test");
        var options = new RecurringJobOptions { TimeZone = TimeZoneInfo.Local };

        // Act
        var exception = Record.Exception(() =>
            _recurringJobManager.AddOrUpdateRecurringNotification(
                "test-recurring-notification-options",
                notification,
                Cron.Monthly(),
                options));

        // Assert
        exception.ShouldBeNull();
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
