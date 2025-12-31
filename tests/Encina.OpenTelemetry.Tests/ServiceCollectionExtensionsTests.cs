using Shouldly;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Xunit;

namespace Encina.OpenTelemetry.Tests;

/// <summary>
/// Tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void WithEncina_ShouldReturnBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddOpenTelemetry();

        // Act
        var result = builder.WithEncina();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeSameAs(builder);
    }

    [Fact]
    public void WithEncina_WithOptions_ShouldReturnBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddOpenTelemetry();
        var options = new EncinaOpenTelemetryOptions
        {
            ServiceName = "TestService",
            ServiceVersion = "1.2.3"
        };

        // Act
        var result = builder.WithEncina(options);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeSameAs(builder);
    }

    [Fact]
    public void WithEncina_WithNullOptions_ShouldUseDefaults()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddOpenTelemetry();
        var expectedDefaults = new EncinaOpenTelemetryOptions();

        // Act
        var result = builder.WithEncina(null);

        // Assert
        using var serviceProvider = services.BuildServiceProvider();
        var registeredOptions = serviceProvider.GetService<EncinaOpenTelemetryOptions>();

        result.ShouldNotBeNull();
        result.ShouldBeSameAs(builder);
        registeredOptions.ShouldNotBeNull();
        registeredOptions.ServiceName.ShouldBe(expectedDefaults.ServiceName);
        registeredOptions.ServiceVersion.ShouldBe(expectedDefaults.ServiceVersion);
        registeredOptions.EnableMessagingEnrichers.ShouldBe(expectedDefaults.EnableMessagingEnrichers);
    }

    [Fact]
    public void AddEncinaInstrumentation_TracerProviderBuilder_ShouldReturnBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var telemetryBuilder = services.AddOpenTelemetry();
        TracerProviderBuilder? tracerBuilder = null;

        telemetryBuilder.WithTracing(builder =>
        {
            tracerBuilder = builder;
        });

        tracerBuilder.ShouldNotBeNull();

        // Act
        var result = tracerBuilder.AddEncinaInstrumentation();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeSameAs(tracerBuilder);
    }

    [Fact]
    public void AddEncinaInstrumentation_MeterProviderBuilder_ShouldReturnBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var telemetryBuilder = services.AddOpenTelemetry();
        MeterProviderBuilder? meterBuilder = null;

        telemetryBuilder.WithMetrics(builder =>
        {
            meterBuilder = builder;
        });

        meterBuilder.ShouldNotBeNull();

        // Act
        var result = meterBuilder.AddEncinaInstrumentation();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeSameAs(meterBuilder);
    }
}
