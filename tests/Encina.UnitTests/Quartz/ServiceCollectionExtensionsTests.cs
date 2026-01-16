using Encina.Messaging.Health;
using Encina.Quartz;
using Encina.Quartz.Health;
using Encina.UnitTests.Quartz.Fakers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;

namespace Encina.UnitTests.Quartz;

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

    [Fact]
    public void AddEncinaQuartz_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaQuartz();

        // Assert
        result.ShouldBe(services);
    }

    [Fact]
    public void AddEncinaQuartz_WithOptionsConfiguration_AppliesOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var optionsConfigureCalled = false;

        // Act
        services.AddEncinaQuartz(configureOptions: options =>
        {
            optionsConfigureCalled = true;
        });

        // Assert
        optionsConfigureCalled.ShouldBeTrue();
    }

    [Fact]
    public void AddEncinaQuartz_WithHealthCheckEnabled_RegistersHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<ISchedulerFactory>());

        // Act
        services.AddEncinaQuartz(configureOptions: options =>
        {
            options.ProviderHealthCheck.Enabled = true;
        });

        // Assert
        services.ShouldContain(sd => sd.ServiceType == typeof(IEncinaHealthCheck));
        services.ShouldContain(sd => sd.ServiceType == typeof(ProviderHealthCheckOptions));
    }

    [Fact]
    public void AddEncinaQuartz_WithHealthCheckDisabled_DoesNotRegisterHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaQuartz(configureOptions: options =>
        {
            options.ProviderHealthCheck.Enabled = false;
        });

        // Assert
        services.ShouldNotContain(sd => sd.ServiceType == typeof(IEncinaHealthCheck));
    }

    [Fact]
    public void AddEncinaQuartz_WithBothConfigureAndOptions_AppliesBoth()
    {
        // Arrange
        var services = new ServiceCollection();
        var quartzConfigureCalled = false;
        var optionsConfigureCalled = false;

        // Act
        services.AddEncinaQuartz(
            configure: config =>
            {
                quartzConfigureCalled = true;
            },
            configureOptions: options =>
            {
                optionsConfigureCalled = true;
            });

        // Assert
        quartzConfigureCalled.ShouldBeTrue();
        optionsConfigureCalled.ShouldBeTrue();
    }

    // Note: ScheduleRequest, ScheduleNotification, AddRequestJob, and AddNotificationJob
    // are extension methods that require IScheduler or IServiceCollectionQuartzConfigurator
    // These would be better tested as integration tests with a running Quartz scheduler
    // For unit tests, we've verified the service registration works correctly
}
