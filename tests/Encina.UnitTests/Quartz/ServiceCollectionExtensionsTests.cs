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

    #region ScheduleRequest / ScheduleNotification

    [Fact]
    public async Task ScheduleRequest_SchedulesJobAndReturnsTriggerKey()
    {
        // Arrange
        var scheduler = Substitute.For<IScheduler>();
        var request = new TestQuartzRequest("test");
        var trigger = TriggerBuilder.Create()
            .WithIdentity("test-trigger")
            .StartNow()
            .Build();

        // Act
        var result = await scheduler.ScheduleRequest<TestQuartzRequest, TestQuartzResponse>(
            request, trigger);

        // Assert
        result.ShouldBe(trigger.Key);
        await scheduler.Received(1).ScheduleJob(
            Arg.Any<IJobDetail>(), Arg.Is(trigger), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScheduleRequest_WithCustomJobKey_UsesProvidedKey()
    {
        // Arrange
        var scheduler = Substitute.For<IScheduler>();
        var request = new TestQuartzRequest("custom");
        var trigger = TriggerBuilder.Create().WithIdentity("t2").StartNow().Build();
        var jobKey = new JobKey("my-custom-key");

        // Act
        await scheduler.ScheduleRequest<TestQuartzRequest, TestQuartzResponse>(
            request, trigger, jobKey);

        // Assert
        await scheduler.Received(1).ScheduleJob(
            Arg.Is<IJobDetail>(j => j.Key == jobKey),
            Arg.Any<ITrigger>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScheduleNotification_SchedulesJobAndReturnsTriggerKey()
    {
        // Arrange
        var scheduler = Substitute.For<IScheduler>();
        var notification = new TestQuartzNotification("event");
        var trigger = TriggerBuilder.Create().WithIdentity("t3").StartNow().Build();

        // Act
        var result = await scheduler.ScheduleNotification(notification, trigger);

        // Assert
        result.ShouldBe(trigger.Key);
        await scheduler.Received(1).ScheduleJob(
            Arg.Any<IJobDetail>(), Arg.Is(trigger), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScheduleNotification_WithCustomJobKey_UsesProvidedKey()
    {
        // Arrange
        var scheduler = Substitute.For<IScheduler>();
        var notification = new TestQuartzNotification("custom");
        var trigger = TriggerBuilder.Create().WithIdentity("t4").StartNow().Build();
        var jobKey = new JobKey("notif-key");

        // Act
        await scheduler.ScheduleNotification(notification, trigger, jobKey);

        // Assert
        await scheduler.Received(1).ScheduleJob(
            Arg.Is<IJobDetail>(j => j.Key == jobKey),
            Arg.Any<ITrigger>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Constants and Options

    [Fact]
    public void QuartzConstants_HasExpectedValues()
    {
        QuartzConstants.RequestKey.ShouldBe("EncinaRequest");
        QuartzConstants.NotificationKey.ShouldBe("EncinaNotification");
    }

    [Fact]
    public void EncinaQuartzOptions_HasDefaultProviderHealthCheck()
    {
        var options = new EncinaQuartzOptions();
        options.ProviderHealthCheck.ShouldNotBeNull();
        options.ProviderHealthCheck.Enabled.ShouldBeTrue();
    }

    #endregion

    // Test types
    public record TestQuartzRequest(string Data) : IRequest<TestQuartzResponse>;
    public record TestQuartzResponse(string Result);
    public record TestQuartzNotification(string Message) : INotification;
}
