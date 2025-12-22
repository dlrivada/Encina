using FluentAssertions;
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
        result.Should().NotBeNull();
        result.Should().BeSameAs(builder);
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
        result.Should().NotBeNull();
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void WithEncina_WithNullOptions_ShouldUseDefaults()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddOpenTelemetry();

        // Act
        var result = builder.WithEncina(null);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(builder);
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

        // Act & Assert
        tracerBuilder.Should().NotBeNull();
        var result = tracerBuilder!.AddEncinaInstrumentation();
        result.Should().NotBeNull();
        result.Should().BeSameAs(tracerBuilder);
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

        // Act & Assert
        meterBuilder.Should().NotBeNull();
        var result = meterBuilder!.AddEncinaInstrumentation();
        result.Should().NotBeNull();
        result.Should().BeSameAs(meterBuilder);
    }
}
