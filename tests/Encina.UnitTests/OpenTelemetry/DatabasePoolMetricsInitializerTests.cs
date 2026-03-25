using Encina.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.OpenTelemetry;

/// <summary>
/// Tests that verify metrics initializers are registered as hosted services
/// via <see cref="ServiceCollectionExtensions.AddEncinaOpenTelemetry"/>.
/// </summary>
public sealed class MetricsInitializerRegistrationTests
{
    [Fact]
    public void AddEncinaOpenTelemetry_RegistersHostedServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaOpenTelemetry();

        // Assert - Verify hosted services are registered (initializers)
        var hostedServiceDescriptors = services
            .Where(d => d.ServiceType == typeof(IHostedService))
            .ToList();

        hostedServiceDescriptors.Count.ShouldBeGreaterThan(0,
            "AddEncinaOpenTelemetry should register multiple hosted service initializers");
    }

    [Fact]
    public void AddEncinaOpenTelemetry_RegistersMultipleMetricsInitializers()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaOpenTelemetry();

        // Assert - Count hosted services to verify all initializers are registered
        var hostedServiceCount = services
            .Count(d => d.ServiceType == typeof(IHostedService));

        // There should be at least 17 initializers based on ServiceCollectionExtensions
        hostedServiceCount.ShouldBeGreaterThanOrEqualTo(17);
    }

    [Fact]
    public void AddEncinaOpenTelemetry_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaOpenTelemetry());
    }

    [Fact]
    public void AddEncinaOpenTelemetry_WithConfigureAction_AppliesOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaOpenTelemetry(options =>
        {
            options.ServiceName = "CustomService";
            options.ServiceVersion = "2.0.0";
        });

        // Assert
        var optionsDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(EncinaOpenTelemetryOptions));
        optionsDescriptor.ShouldNotBeNull();

        // Verify the options instance has the configured values
        var instance = optionsDescriptor.ImplementationInstance as EncinaOpenTelemetryOptions;
        instance.ShouldNotBeNull();
        instance.ServiceName.ShouldBe("CustomService");
        instance.ServiceVersion.ShouldBe("2.0.0");
    }

    [Fact]
    public void AddEncinaOpenTelemetry_WithNullConfigure_UsesDefaults()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaOpenTelemetry(null);

        // Assert
        var optionsDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(EncinaOpenTelemetryOptions));
        optionsDescriptor.ShouldNotBeNull();

        var instance = optionsDescriptor.ImplementationInstance as EncinaOpenTelemetryOptions;
        instance.ShouldNotBeNull();
        instance.ServiceName.ShouldBe("Encina");
        instance.ServiceVersion.ShouldBe("1.0.0");
    }
}
