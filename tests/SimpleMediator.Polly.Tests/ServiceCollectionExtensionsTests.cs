using Microsoft.Extensions.DependencyInjection;

namespace SimpleMediator.Polly.Tests;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/>.
/// Verifies DI registration and service collection extension methods.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSimpleMediatorPolly_ShouldRegisterBothBehaviors()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSimpleMediatorPolly();

        // Assert
        var behaviorDescriptors = services.Where(sd => sd.ServiceType == typeof(IPipelineBehavior<,>)).ToList();

        behaviorDescriptors.Should().HaveCount(2, "both Retry and CircuitBreaker behaviors should be registered");

        var hasRetryBehavior = behaviorDescriptors.Any(d => d.ImplementationType?.Name.Contains("RetryPipelineBehavior") == true);
        var hasCircuitBreakerBehavior = behaviorDescriptors.Any(d => d.ImplementationType?.Name.Contains("CircuitBreakerPipelineBehavior") == true);

        hasRetryBehavior.Should().BeTrue("RetryPipelineBehavior should be registered");
        hasCircuitBreakerBehavior.Should().BeTrue("CircuitBreakerPipelineBehavior should be registered");
    }

    [Fact]
    public void AddSimpleMediatorPolly_ShouldReturnServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddSimpleMediatorPolly();

        // Assert
        result.Should().BeSameAs(services, "should return same service collection for fluent chaining");
    }

    [Fact]
    public void AddSimpleMediatorPolly_ShouldRegisterAsTransient()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSimpleMediatorPolly();

        // Assert - verify the registered behaviors are Transient
        var behaviorDescriptors = services.Where(sd => sd.ServiceType == typeof(IPipelineBehavior<,>)).ToList();

        behaviorDescriptors.Should().NotBeEmpty();
        behaviorDescriptors.Should().AllSatisfy(descriptor =>
        {
            descriptor.Lifetime.Should().Be(ServiceLifetime.Transient, "all behaviors should be transient");
        });
    }

    [Fact]
    public void AddSimpleMediatorPolly_NullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        var act = () => services.AddSimpleMediatorPolly();
        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddSimpleMediatorPolly_WithConfigureAction_ShouldRegisterBehaviors()
    {
        // Arrange
        var services = new ServiceCollection();
        var configureWasCalled = false;

        // Act
        services.AddSimpleMediatorPolly(options =>
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
    public void AddSimpleMediatorPolly_WithConfigureAction_ShouldRegisterOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSimpleMediatorPolly(options =>
        {
            options.EnableTelemetry = false;
            options.EnableLogging = true;
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<SimpleMediatorPollyOptions>();

        options.Should().NotBeNull("SimpleMediatorPollyOptions should be registered");
        options!.EnableTelemetry.Should().BeFalse();
        options.EnableLogging.Should().BeTrue();
    }

    [Fact]
    public void AddSimpleMediatorPolly_WithConfigureAction_NullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        var act = () => services.AddSimpleMediatorPolly(options => { });
        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddSimpleMediatorPolly_WithConfigureAction_NullConfigure_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var act = () => services.AddSimpleMediatorPolly(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("configure");
    }

    [Fact]
    public void AddSimpleMediatorPolly_WithConfigureAction_ShouldReturnServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddSimpleMediatorPolly(options => { });

        // Assert
        result.Should().BeSameAs(services, "should return same service collection for fluent chaining");
    }

    [Fact]
    public void SimpleMediatorPollyOptions_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var options = new SimpleMediatorPollyOptions();

        // Assert
        options.EnableTelemetry.Should().BeTrue("telemetry should be enabled by default");
        options.EnableLogging.Should().BeTrue("logging should be enabled by default");
    }

    [Fact]
    public void SimpleMediatorPollyOptions_CanSetProperties()
    {
        // Act
        var options = new SimpleMediatorPollyOptions
        {
            EnableTelemetry = false,
            EnableLogging = false
        };

        // Assert
        options.EnableTelemetry.Should().BeFalse();
        options.EnableLogging.Should().BeFalse();
    }

    [Fact]
    public void AddSimpleMediatorPolly_MultipleCalls_ShouldAddDuplicates()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSimpleMediatorPolly();
        services.AddSimpleMediatorPolly();

        // Assert - AddTransient allows duplicates (not Try)
        var behaviorDescriptors = services.Where(sd => sd.ServiceType == typeof(IPipelineBehavior<,>)).ToList();

        behaviorDescriptors.Should().HaveCount(4, "both behaviors should be registered twice (2 + 2)");
    }
}
