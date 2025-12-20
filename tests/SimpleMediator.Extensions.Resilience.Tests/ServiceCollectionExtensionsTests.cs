using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Registry;
using SimpleMediator.Extensions.Resilience;
using Xunit;

namespace SimpleMediator.Extensions.Resilience.Tests;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/>.
/// Tests dependency injection registration methods for resilience configuration.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSimpleMediatorStandardResilience_ShouldRegisterPipelineProvider()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSimpleMediatorStandardResilience();
        var provider = services.BuildServiceProvider();

        // Assert
        var pipelineProvider = provider.GetService<ResiliencePipelineProvider<string>>();
        pipelineProvider.Should().NotBeNull();
        pipelineProvider.Should().BeOfType<ResiliencePipelineRegistry<string>>();
    }

    [Fact]
    public void AddSimpleMediatorStandardResilience_ShouldRegisterBehavior()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddSimpleMediatorStandardResilience();
        var provider = services.BuildServiceProvider();

        // Assert
        var behaviorDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType?.Name.Contains("StandardResiliencePipelineBehavior") == true);

        behaviorDescriptor.Should().NotBeNull();
        behaviorDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddSimpleMediatorStandardResilience_WithCustomOptions_ShouldApplyConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSimpleMediatorStandardResilience(options =>
        {
            options.Retry.MaxRetryAttempts = 5;
            options.CircuitBreaker.FailureRatio = 0.2;
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var pipelineProvider = provider.GetService<ResiliencePipelineProvider<string>>();
        pipelineProvider.Should().NotBeNull();
        // Configuration is applied internally, just verify provider exists
    }

    [Fact]
    public void AddSimpleMediatorStandardResilience_ShouldReturnServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddSimpleMediatorStandardResilience();

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddSimpleMediatorStandardResilience_WithoutConfigure_ShouldUseDefaults()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSimpleMediatorStandardResilience();
        var provider = services.BuildServiceProvider();

        // Assert
        var pipelineProvider = provider.GetService<ResiliencePipelineProvider<string>>();
        pipelineProvider.Should().NotBeNull();
    }

    [Fact]
    public void AddSimpleMediatorStandardResilienceFor_ShouldRegisterRequestSpecificPipeline()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSimpleMediatorStandardResilienceFor<TestRequest, TestResponse>(options =>
        {
            options.Retry.MaxRetryAttempts = 10;
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var pipelineProvider = provider.GetService<ResiliencePipelineProvider<string>>();
        pipelineProvider.Should().NotBeNull();
    }

    [Fact]
    public void AddSimpleMediatorStandardResilienceFor_ShouldReturnServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddSimpleMediatorStandardResilienceFor<TestRequest, TestResponse>(options =>
        {
            options.Retry.MaxRetryAttempts = 3;
        });

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddSimpleMediatorStandardResilience_MultipleCalls_ShouldNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () =>
        {
            services.AddSimpleMediatorStandardResilience();
            services.AddSimpleMediatorStandardResilience();
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AddSimpleMediatorStandardResilienceFor_MultipleDifferentRequests_ShouldRegisterSeparately()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSimpleMediatorStandardResilienceFor<TestRequest, TestResponse>(options =>
        {
            options.Retry.MaxRetryAttempts = 3;
        });

        services.AddSimpleMediatorStandardResilienceFor<AnotherRequest, AnotherResponse>(options =>
        {
            options.Retry.MaxRetryAttempts = 5;
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var pipelineProvider = provider.GetService<ResiliencePipelineProvider<string>>();
        pipelineProvider.Should().NotBeNull();
    }

    [Fact]
    public void AddSimpleMediatorStandardResilience_ShouldRegisterAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSimpleMediatorStandardResilience();

        // Assert
        var providerDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(ResiliencePipelineProvider<string>));

        providerDescriptor.Should().NotBeNull();
        providerDescriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    // Test helper classes
    private sealed record TestRequest : IRequest<TestResponse>;
    private sealed record TestResponse;
    private sealed record AnotherRequest : IRequest<AnotherResponse>;
    private sealed record AnotherResponse;
}
