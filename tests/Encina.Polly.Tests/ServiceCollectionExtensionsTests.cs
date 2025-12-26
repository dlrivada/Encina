using Microsoft.Extensions.DependencyInjection;

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

        behaviorDescriptors.Should().HaveCount(4, "Retry, CircuitBreaker, RateLimiting, and Bulkhead behaviors should be registered");

        var hasRetryBehavior = behaviorDescriptors.Any(d => d.ImplementationType?.Name.Contains("RetryPipelineBehavior") == true);
        var hasCircuitBreakerBehavior = behaviorDescriptors.Any(d => d.ImplementationType?.Name.Contains("CircuitBreakerPipelineBehavior") == true);
        var hasRateLimitingBehavior = behaviorDescriptors.Any(d => d.ImplementationType?.Name.Contains("RateLimitingPipelineBehavior") == true);
        var hasBulkheadBehavior = behaviorDescriptors.Any(d => d.ImplementationType?.Name.Contains("BulkheadPipelineBehavior") == true);

        hasRetryBehavior.Should().BeTrue("RetryPipelineBehavior should be registered");
        hasCircuitBreakerBehavior.Should().BeTrue("CircuitBreakerPipelineBehavior should be registered");
        hasRateLimitingBehavior.Should().BeTrue("RateLimitingPipelineBehavior should be registered");
        hasBulkheadBehavior.Should().BeTrue("BulkheadPipelineBehavior should be registered");
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

        rateLimiterDescriptor.Should().NotBeNull("IRateLimiter should be registered");
        rateLimiterDescriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton, "IRateLimiter should be singleton for shared state");
        rateLimiterDescriptor.ImplementationType.Should().Be(typeof(AdaptiveRateLimiter));
    }

    [Fact]
    public void AddEncinaPolly_ShouldReturnServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaPolly();

        // Assert
        result.Should().BeSameAs(services, "should return same service collection for fluent chaining");
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

        behaviorDescriptors.Should().NotBeEmpty();
        behaviorDescriptors.Should().AllSatisfy(descriptor =>
        {
            descriptor.Lifetime.Should().Be(ServiceLifetime.Transient, "all behaviors should be transient");
        });
    }

    [Fact]
    public void AddEncinaPolly_NullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        var act = () => services.AddEncinaPolly();
        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
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
        configureWasCalled.Should().BeTrue("configure action should be invoked");

        var hasRetryBehavior = services.Any(sd =>
            sd.ServiceType == typeof(IPipelineBehavior<,>) &&
            sd.ImplementationType?.Name.Contains("RetryPipelineBehavior") == true);

        hasRetryBehavior.Should().BeTrue("behaviors should be registered even with configure action");
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
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<EncinaPollyOptions>();

        options.Should().NotBeNull("EncinaPollyOptions should be registered");
        options!.EnableTelemetry.Should().BeFalse();
        options.EnableLogging.Should().BeTrue();
    }

    [Fact]
    public void AddEncinaPolly_WithConfigureAction_NullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        var act = () => services.AddEncinaPolly(options => { });
        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddEncinaPolly_WithConfigureAction_NullConfigure_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var act = () => services.AddEncinaPolly(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("configure");
    }

    [Fact]
    public void AddEncinaPolly_WithConfigureAction_ShouldReturnServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaPolly(options => { });

        // Assert
        result.Should().BeSameAs(services, "should return same service collection for fluent chaining");
    }

    [Fact]
    public void EncinaPollyOptions_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var options = new EncinaPollyOptions();

        // Assert
        options.EnableTelemetry.Should().BeTrue("telemetry should be enabled by default");
        options.EnableLogging.Should().BeTrue("logging should be enabled by default");
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
        options.EnableTelemetry.Should().BeFalse();
        options.EnableLogging.Should().BeFalse();
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

        behaviorDescriptors.Should().HaveCount(8, "all four behaviors should be registered twice (4 + 4)");
    }
}
