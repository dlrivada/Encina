using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;

namespace Encina.Quartz.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaQuartz_RegistersQuartzServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaQuartz();

        // Assert - Verify Quartz services are registered via descriptor inspection
        var schedulerFactoryDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(ISchedulerFactory));
        schedulerFactoryDescriptor.ShouldNotBeNull("ISchedulerFactory should be registered");
    }

    [Fact]
    public void AddEncinaQuartz_RegistersQuartzHostedService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaQuartz();

        // Assert - Verify Quartz hosted service is registered
        services.ShouldContain(sd =>
            sd.ServiceType == typeof(IHostedService) &&
            sd.ImplementationType != null &&
            typeof(IHostedService).IsAssignableFrom(sd.ImplementationType),
            "A valid IHostedService implementation should be registered");
    }

    [Fact]
    public void AddEncinaQuartz_AllowsCustomConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var configurationCalled = false;

        // Act
        services.AddEncinaQuartz(config =>
        {
            configurationCalled = true;
            // Verify configuration action is called
            // (UseMicrosoftDependencyInjectionJobFactory is now default and obsolete)
        });

        // Assert
        configurationCalled.ShouldBeTrue();
    }

    [Fact]
    public void AddEncinaQuartz_CanBeCalledMultipleTimes()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaQuartz();
        var countAfterFirst = services.Count;

        services.AddEncinaQuartz(); // Should not throw
        var countAfterSecond = services.Count;

        // Assert - Verify no duplicate registrations were added
        countAfterSecond.ShouldBe(countAfterFirst,
            "Calling AddEncinaQuartz twice should not add duplicate registrations");

        // Additionally verify no duplicate service descriptors exist
        var duplicates = services
            .GroupBy(sd => (sd.ServiceType, sd.ImplementationType, sd.Lifetime))
            .Where(g => g.Count() > 1)
            .ToList();

        duplicates.ShouldBeEmpty(
            "No service descriptors should be duplicated");
    }

    // Note: ScheduleRequest, ScheduleNotification, AddRequestJob, and AddNotificationJob
    // are extension methods that require IScheduler or IServiceCollectionQuartzConfigurator
    // These would be better tested as integration tests with a running Quartz scheduler
    // For unit tests, we've verified the service registration works correctly

    // Test types
    public record TestRequest(string Data) : IRequest<TestResponse>;
    public record TestResponse(string Result);
    public record TestNotification(string Message) : INotification;
}
