using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.Polly.Tests;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/>.
/// Verifies DI registration and service collection extension methods.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaPolly_ShouldRegisterAllBehaviors()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaPolly();

        // Assert
        var behaviorDescriptors = services.Where(sd => sd.ServiceType == typeof(IPipelineBehavior<,>)).ToList();

        behaviorDescriptors.Count.ShouldBe(4, "Retry, CircuitBreaker, RateLimiting, and Bulkhead behaviors should be registered");

        var hasRetryBehavior = behaviorDescriptors.Any(d => d.ImplementationType?.Name.Contains("RetryPipelineBehavior") == true);
        var hasCircuitBreakerBehavior = behaviorDescriptors.Any(d => d.ImplementationType?.Name.Contains("CircuitBreakerPipelineBehavior") == true);
        var hasRateLimitingBehavior = behaviorDescriptors.Any(d => d.ImplementationType?.Name.Contains("RateLimitingPipelineBehavior") == true);
        var hasBulkheadBehavior = behaviorDescriptors.Any(d => d.ImplementationType?.Name.Contains("BulkheadPipelineBehavior") == true);

        hasRetryBehavior.ShouldBeTrue("RetryPipelineBehavior should be registered");
        hasCircuitBreakerBehavior.ShouldBeTrue("CircuitBreakerPipelineBehavior should be registered");
        hasRateLimitingBehavior.ShouldBeTrue("RateLimitingPipelineBehavior should be registered");
        hasBulkheadBehavior.ShouldBeTrue("BulkheadPipelineBehavior should be registered");
    }

    [Fact]
    public void AddEncinaPolly_ShouldRegisterRateLimiterAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaPolly();

        // Assert
        var rateLimiterDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(IRateLimiter));

        rateLimiterDescriptor.ShouldNotBeNull("IRateLimiter should be registered");
        rateLimiterDescriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton, "IRateLimiter should be singleton for shared state");
        rateLimiterDescriptor.ImplementationType.ShouldBe(typeof(AdaptiveRateLimiter));
    }

    [Fact]
    public void AddEncinaPolly_ShouldReturnServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaPolly();

        // Assert
        result.ShouldBeSameAs(services, "should return same service collection for fluent chaining");
    }

    [Fact]
    public void AddEncinaPolly_ShouldRegisterAsTransient()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaPolly();

        // Assert - verify the registered behaviors are Transient
        var behaviorDescriptors = services.Where(sd => sd.ServiceType == typeof(IPipelineBehavior<,>)).ToList();

        behaviorDescriptors.ShouldNotBeEmpty();
        behaviorDescriptors.ShouldAllBe(descriptor => descriptor.Lifetime == ServiceLifetime.Transient, "all behaviors should be transient");
    }

    [Fact]
    public void AddEncinaPolly_NullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        var act = () => services.AddEncinaPolly();
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddEncinaPolly_WithConfigureAction_ShouldRegisterBehaviors()
    {
        // Arrange
        var services = new ServiceCollection();
        var configureWasCalled = false;

        // Act
        services.AddEncinaPolly(options =>
        {
            configureWasCalled = true;
            options.EnableTelemetry = false;
            options.EnableLogging = false;
        });

        // Assert
        configureWasCalled.ShouldBeTrue("configure action should be invoked");

        var hasRetryBehavior = services.Any(sd =>
            sd.ServiceType == typeof(IPipelineBehavior<,>) &&
            sd.ImplementationType?.Name.Contains("RetryPipelineBehavior") == true);

        hasRetryBehavior.ShouldBeTrue("behaviors should be registered even with configure action");
    }

    [Fact]
    public void AddEncinaPolly_WithConfigureAction_ShouldRegisterOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaPolly(options =>
        {
            options.EnableTelemetry = false;
            options.EnableLogging = true;
        });

        // Assert
        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<EncinaPollyOptions>();

        var nonNullOptions = options.ShouldNotBeNull("EncinaPollyOptions should be registered");
        nonNullOptions.EnableTelemetry.ShouldBeFalse();
        nonNullOptions.EnableLogging.ShouldBeTrue();
    }

    [Fact]
    public void AddEncinaPolly_WithConfigureAction_NullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        var act = () => services.AddEncinaPolly(options => { });
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddEncinaPolly_WithConfigureAction_NullConfigure_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var act = () => services.AddEncinaPolly(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("configure");
    }

    [Fact]
    public void AddEncinaPolly_WithConfigureAction_ShouldReturnServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaPolly(options => { });

        // Assert
        result.ShouldBeSameAs(services, "should return same service collection for fluent chaining");
    }

    [Fact]
    public void EncinaPollyOptions_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var options = new EncinaPollyOptions();

        // Assert
        options.EnableTelemetry.ShouldBeTrue("telemetry should be enabled by default");
        options.EnableLogging.ShouldBeTrue("logging should be enabled by default");
    }

    [Fact]
    public void EncinaPollyOptions_CanSetProperties()
    {
        // Act
        var options = new EncinaPollyOptions
        {
            EnableTelemetry = false,
            EnableLogging = false
        };

        // Assert
        options.EnableTelemetry.ShouldBeFalse();
        options.EnableLogging.ShouldBeFalse();
    }

    [Fact]
    public void AddEncinaPolly_MultipleCalls_ShouldAddDuplicates()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaPolly();
        services.AddEncinaPolly();

        // Assert - AddTransient allows duplicates (not Try)
        var behaviorDescriptors = services.Where(sd => sd.ServiceType == typeof(IPipelineBehavior<,>)).ToList();

        behaviorDescriptors.Count.ShouldBe(8, "all four behaviors should be registered twice (4 + 4)");
    }
}
