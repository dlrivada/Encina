using Shouldly;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Encina.OpenTelemetry.Tests.Contracts;

/// <summary>
/// Contract tests for <see cref="ServiceCollectionExtensions"/> to verify
/// correct integration with OpenTelemetry builders and DI container.
/// </summary>
public sealed class ServiceCollectionExtensionsContractTests
{
    [Fact]
    public void AddEncinaOpenTelemetry_ShouldReturnSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaOpenTelemetry();

        // Assert
        result.ShouldBeSameAs(services, "extension method should return the same IServiceCollection for chaining");
    }

    [Fact]
    public void AddEncinaOpenTelemetry_ShouldRegisterOptionsAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaOpenTelemetry(options =>
        {
            options.ServiceName = "TestService";
            options.ServiceVersion = "2.0.0";
        });

        // Assert - Inspect ServiceCollection directly instead of building provider
        var optionsDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(EncinaOpenTelemetryOptions));
        optionsDescriptor.ShouldNotBeNull("options should be registered in DI container");
        optionsDescriptor!.Lifetime.ShouldBe(ServiceLifetime.Singleton, "options should be registered with singleton lifetime");
    }

    [Fact]
    public void AddEncinaOpenTelemetry_WhenMessagingEnrichersEnabled_ShouldRegisterBehavior()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaOpenTelemetry(options =>
        {
            options.EnableMessagingEnrichers = true;
        });

        // Assert - Inspect ServiceCollection directly
        var behaviorDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType?.Name.Contains("MessagingEnricher") == true);

        behaviorDescriptor.ShouldNotBeNull("MessagingEnricherPipelineBehavior should be registered");
        behaviorDescriptor!.Lifetime.ShouldBe(ServiceLifetime.Transient, "pipeline behaviors should have transient lifetime");
    }

    [Fact]
    public void AddEncinaOpenTelemetry_WhenMessagingEnrichersDisabled_ShouldNotRegisterBehavior()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaOpenTelemetry(options =>
        {
            options.EnableMessagingEnrichers = false;
        });

        // Assert
        var behaviorDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType?.Name.Contains("MessagingEnricher") == true);

        behaviorDescriptor.ShouldBeNull("MessagingEnricherPipelineBehavior should not be registered when disabled");
    }

    [Fact]
    public void WithEncina_ShouldReturnSameBuilderInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddOpenTelemetry();

        // Act
        var result = builder.WithEncina();

        // Assert
        result.ShouldBeSameAs(builder, "extension method should return the same OpenTelemetryBuilder for chaining");
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public void WithEncina_ShouldConfigureResourceWithServiceInfo()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddOpenTelemetry();
        var options = new EncinaOpenTelemetryOptions
        {
            ServiceName = "ContractTest",
            ServiceVersion = "3.0.0"
        };

        // Act
        var act = () => builder.WithEncina(options);

        // Assert - Smoke test: verify configuration doesn't throw
        // Integration tests will verify actual resource attributes (service.name, service.version, deployment.environment)
        Should.NotThrow(act, "WithEncina configuration should not throw");
    }

    [Fact]
    public void WithEncina_WithNullOptions_ShouldUseDefaultOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddOpenTelemetry();

        // Act
        var act = () => builder.WithEncina(null);

        // Assert
        Should.NotThrow(act, "null options should be replaced with default options");
    }

    [Fact]
    public void AddEncinaInstrumentation_TracerProvider_ShouldReturnSameBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOpenTelemetry().WithTracing(tracing =>
        {
            // Act
            var result = tracing.AddEncinaInstrumentation();

            // Assert
            result.ShouldBeSameAs(tracing, "extension method should return the same TracerProviderBuilder for chaining");
        });
    }

    [Fact]
    public void AddEncinaInstrumentation_MeterProvider_ShouldReturnSameBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOpenTelemetry().WithMetrics(metrics =>
        {
            // Act
            var result = metrics.AddEncinaInstrumentation();

            // Assert
            result.ShouldBeSameAs(metrics, "extension method should return the same MeterProviderBuilder for chaining");
        });
    }

    [Fact]
    public void AddEncinaOpenTelemetry_ShouldNotThrowWhenCalledMultipleTimes()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () =>
        {
            services.AddEncinaOpenTelemetry();
            services.AddEncinaOpenTelemetry();
            services.AddEncinaOpenTelemetry();
        };

        // Assert
        Should.NotThrow(act, "calling multiple times should be idempotent due to TryAddSingleton");
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public void WithEncina_ShouldConfigureTracingWithEncinaSource()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddOpenTelemetry();

        // Act
        var act = () => builder.WithEncina();

        // Assert - Smoke test: verify configuration doesn't throw
        // Integration tests will verify actual tracing configuration
        Should.NotThrow(act, "WithEncina tracing configuration should not throw");
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public void WithEncina_ShouldConfigureMetricsWithEncinaMeter()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = services.AddOpenTelemetry();

        // Act
        var act = () => builder.WithEncina();

        // Assert - Smoke test: verify configuration doesn't throw
        // Integration tests will verify actual metrics configuration
        Should.NotThrow(act, "WithEncina metrics configuration should not throw");
    }
}
