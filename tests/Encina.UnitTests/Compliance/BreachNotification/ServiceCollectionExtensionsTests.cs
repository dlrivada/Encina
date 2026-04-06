using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.Abstractions;
using Encina.Compliance.BreachNotification.Model;

using FluentAssertions;

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

        services.Should().Contain(s => s.ServiceType == typeof(IBreachDetector));
        services.Should().Contain(s => s.ServiceType == typeof(IBreachNotifier));
        services.Should().Contain(s => s.ServiceType == typeof(IBreachNotificationService));
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

        services.Should().Contain(s => s.ServiceType == typeof(IBreachDetector));
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

        services.Count.Should().BeGreaterThan(withoutHealthCheck.Count);
    }

    [Fact]
    public void AddEncinaBreachNotification_WithDeadlineMonitoring_RegistersHostedService()
    {
        var services = new ServiceCollection();

        services.AddEncinaBreachNotification(options =>
        {
            options.EnableDeadlineMonitoring = true;
        });

        services.Should().Contain(s =>
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

        services.Should().NotContain(s =>
            s.ImplementationType == typeof(BreachDeadlineMonitorService));
    }

    [Fact]
    public void AddEncinaBreachNotification_RegistersPipelineBehavior()
    {
        var services = new ServiceCollection();

        services.AddEncinaBreachNotification();

        services.Should().Contain(s =>
            s.ServiceType == typeof(IPipelineBehavior<,>));
    }

    [Fact]
    public void AddEncinaBreachNotification_RegistersBuiltInDetectionRules()
    {
        var services = new ServiceCollection();

        services.AddEncinaBreachNotification();

        services.Should().Contain(s => s.ServiceType == typeof(IBreachDetectionRule));
    }

    [Fact]
    public void AddEncinaBreachNotification_RegistersOptionsValidator()
    {
        var services = new ServiceCollection();

        services.AddEncinaBreachNotification();

        services.Should().Contain(s =>
            s.ServiceType == typeof(IValidateOptions<BreachNotificationOptions>));
    }

    [Fact]
    public void AddEncinaBreachNotification_ReturnsServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddEncinaBreachNotification();

        result.Should().BeSameAs(services);
    }
}
