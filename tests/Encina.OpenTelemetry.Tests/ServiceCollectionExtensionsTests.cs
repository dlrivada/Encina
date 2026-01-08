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
    public void WithEncina_WithNullOptions_ShouldUseDefaultsAndReturnBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddOpenTelemetry();

        // Act - should accept null options and use defaults internally
        var result = builder.WithEncina(null);

        // Assert - method returns builder for fluent chaining
        result.ShouldNotBeNull();
        result.ShouldBeSameAs(builder);

        // Note: WithEncina does not register EncinaOpenTelemetryOptions as a service,
        // it only uses the options internally to configure OpenTelemetry resources and tracing.
        // This is by design - the options are configuration-time only.
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
