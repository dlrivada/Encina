using Shouldly;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Encina.OpenTelemetry.Tests.Guards;

/// <summary>
/// Guard clause tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public sealed class ServiceCollectionExtensionsGuardTests
{
    [Fact]
    public void AddEncinaOpenTelemetry_WithNullServices_ShouldThrow()
    {
        // Act
        var act = () => ServiceCollectionExtensions.AddEncinaOpenTelemetry(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddEncinaOpenTelemetry_WithNullConfigure_ShouldNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddEncinaOpenTelemetry(null);

        // Assert
        Should.NotThrow(act);
    }

    [Fact]
    public void WithEncina_WithNullBuilder_ShouldThrow()
    {
        // Act
        var act = () => ServiceCollectionExtensions.WithEncina(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("builder");
    }

    [Fact]
    public void WithEncina_WithNullOptions_ShouldNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddOpenTelemetry();

        // Act
        var act = () => builder.WithEncina(null);

        // Assert
        Should.NotThrow(act);
    }

    [Fact]
    public void AddEncinaInstrumentation_TracerProvider_WithNullBuilder_ShouldThrow()
    {
        // Act
        var act = () => ServiceCollectionExtensions.AddEncinaInstrumentation((global::OpenTelemetry.Trace.TracerProviderBuilder)null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("builder");
    }

    [Fact]
    public void AddEncinaInstrumentation_MeterProvider_WithNullBuilder_ShouldThrow()
    {
        // Act
        var act = () => ServiceCollectionExtensions.AddEncinaInstrumentation((global::OpenTelemetry.Metrics.MeterProviderBuilder)null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("builder");
    }
}
