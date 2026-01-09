using Encina.Quartz.Tests.Fakers;
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

        // Act - calling AddEncinaQuartz multiple times should not throw
        services.AddEncinaQuartz();
        services.AddEncinaQuartz();

        // Assert - Verify core services are registered (at least once)
        // Note: Quartz.NET may add some services multiple times when called repeatedly,
        // which is acceptable behavior for the library. We verify that required services exist.
        var schedulerFactoryDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(ISchedulerFactory));
        schedulerFactoryDescriptor.ShouldNotBeNull("ISchedulerFactory should be registered");

        var hostedServiceDescriptor = services.FirstOrDefault(sd =>
            sd.ServiceType == typeof(IHostedService) &&
            sd.ImplementationType != null &&
            typeof(IHostedService).IsAssignableFrom(sd.ImplementationType));
        hostedServiceDescriptor.ShouldNotBeNull("IHostedService for Quartz should be registered");
    }

    // Note: ScheduleRequest, ScheduleNotification, AddRequestJob, and AddNotificationJob
    // are extension methods that require IScheduler or IServiceCollectionQuartzConfigurator
    // These would be better tested as integration tests with a running Quartz scheduler
    // For unit tests, we've verified the service registration works correctly
}
