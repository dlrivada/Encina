using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.Abstractions;
using Encina.Compliance.BreachNotification.Model;

using Shouldly;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Compliance.BreachNotification;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions.AddEncinaBreachNotification"/>.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaBreachNotification_DefaultOptions_RegistersCoreServices()
    {
        var services = new ServiceCollection();

        services.AddEncinaBreachNotification();

        services.ShouldContain(s => s.ServiceType == typeof(IBreachDetector));
        services.ShouldContain(s => s.ServiceType == typeof(IBreachNotifier));
        services.ShouldContain(s => s.ServiceType == typeof(IBreachNotificationService));
    }

    [Fact]
    public void AddEncinaBreachNotification_WithConfigure_AppliesOptions()
    {
        var services = new ServiceCollection();

        services.AddEncinaBreachNotification(options =>
        {
            options.EnforcementMode = BreachDetectionEnforcementMode.Block;
            options.NotificationDeadlineHours = 48;
        });

        services.ShouldContain(s => s.ServiceType == typeof(IBreachDetector));
    }

    [Fact]
    public void AddEncinaBreachNotification_WithHealthCheck_RegistersServices()
    {
        var services = new ServiceCollection();

        services.AddEncinaBreachNotification(options =>
        {
            options.AddHealthCheck = true;
        });

        // Health checks are registered — service count should be higher than without
        var withoutHealthCheck = new ServiceCollection();
        withoutHealthCheck.AddEncinaBreachNotification(options =>
        {
            options.AddHealthCheck = false;
        });

        services.Count.ShouldBeGreaterThan(withoutHealthCheck.Count);
    }

    [Fact]
    public void AddEncinaBreachNotification_WithDeadlineMonitoring_RegistersHostedService()
    {
        var services = new ServiceCollection();

        services.AddEncinaBreachNotification(options =>
        {
            options.EnableDeadlineMonitoring = true;
        });

        services.ShouldContain(s =>
            s.ServiceType == typeof(IHostedService) &&
            s.ImplementationType == typeof(BreachDeadlineMonitorService));
    }

    [Fact]
    public void AddEncinaBreachNotification_WithoutDeadlineMonitoring_DoesNotRegisterHostedService()
    {
        var services = new ServiceCollection();

        services.AddEncinaBreachNotification(options =>
        {
            options.EnableDeadlineMonitoring = false;
        });

        services.ShouldNotContain(s =>
            s.ImplementationType == typeof(BreachDeadlineMonitorService));
    }

    [Fact]
    public void AddEncinaBreachNotification_RegistersPipelineBehavior()
    {
        var services = new ServiceCollection();

        services.AddEncinaBreachNotification();

        services.ShouldContain(s =>
            s.ServiceType == typeof(IPipelineBehavior<,>));
    }

    [Fact]
    public void AddEncinaBreachNotification_RegistersBuiltInDetectionRules()
    {
        var services = new ServiceCollection();

        services.AddEncinaBreachNotification();

        services.ShouldContain(s => s.ServiceType == typeof(IBreachDetectionRule));
    }

    [Fact]
    public void AddEncinaBreachNotification_RegistersOptionsValidator()
    {
        var services = new ServiceCollection();

        services.AddEncinaBreachNotification();

        services.ShouldContain(s =>
            s.ServiceType == typeof(IValidateOptions<BreachNotificationOptions>));
    }

    [Fact]
    public void AddEncinaBreachNotification_ReturnsServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddEncinaBreachNotification();

        result.ShouldBeSameAs(services);
    }
}
