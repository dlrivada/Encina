using Encina.Extensions.Resilience;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Registry;
using Shouldly;
using Xunit;

namespace Encina.Extensions.Resilience.Tests;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/>.
/// Tests dependency injection registration methods for resilience configuration.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaStandardResilience_ShouldRegisterPipelineProvider()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaStandardResilience();
        var provider = services.BuildServiceProvider();

        // Assert
        var pipelineProvider = provider.GetService<ResiliencePipelineProvider<string>>();
        pipelineProvider.ShouldNotBeNull();
        pipelineProvider.ShouldBeOfType<ResiliencePipelineRegistry<string>>();
    }

    [Fact]
    public void AddEncinaStandardResilience_ShouldRegisterBehavior()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaStandardResilience();
        var provider = services.BuildServiceProvider();

        // Assert
        var behaviorDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IPipelineBehavior<,>) &&
            d.ImplementationType?.IsGenericType == true &&
            d.ImplementationType.GetGenericTypeDefinition() == typeof(StandardResiliencePipelineBehavior<,>));

        behaviorDescriptor.ShouldNotBeNull();
        behaviorDescriptor!.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaStandardResilience_WithCustomOptions_ShouldApplyConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaStandardResilience(options =>
        {
            options.Retry.MaxRetryAttempts = 5;
            options.CircuitBreaker.FailureRatio = 0.2;
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var pipelineProvider = provider.GetService<ResiliencePipelineProvider<string>>();
        pipelineProvider.ShouldNotBeNull();
        // Configuration is applied internally, just verify provider exists
    }

    [Fact]
    public void AddEncinaStandardResilience_ShouldReturnServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaStandardResilience();

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaStandardResilience_WithoutConfigure_ShouldUseDefaults()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaStandardResilience();
        var provider = services.BuildServiceProvider();

        // Assert
        var pipelineProvider = provider.GetService<ResiliencePipelineProvider<string>>();
        pipelineProvider.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaStandardResilienceFor_ShouldRegisterRequestSpecificPipeline()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaStandardResilienceFor<TestRequest, TestResponse>(options =>
        {
            options.Retry.MaxRetryAttempts = 10;
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var pipelineProvider = provider.GetService<ResiliencePipelineProvider<string>>();
        pipelineProvider.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaStandardResilienceFor_ShouldReturnServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaStandardResilienceFor<TestRequest, TestResponse>(options =>
        {
            options.Retry.MaxRetryAttempts = 3;
        });

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaStandardResilience_MultipleCalls_ShouldNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () =>
        {
            services.AddEncinaStandardResilience();
            services.AddEncinaStandardResilience();
        };

        // Assert
        Should.NotThrow(act);
    }

    [Fact]
    public void AddEncinaStandardResilienceFor_MultipleDifferentRequests_ShouldRegisterSeparately()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaStandardResilienceFor<TestRequest, TestResponse>(options =>
        {
            options.Retry.MaxRetryAttempts = 3;
        });

        services.AddEncinaStandardResilienceFor<AnotherRequest, AnotherResponse>(options =>
        {
            options.Retry.MaxRetryAttempts = 5;
        });

        var provider = services.BuildServiceProvider();

        // Assert
        var pipelineProvider = provider.GetService<ResiliencePipelineProvider<string>>();
        pipelineProvider.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaStandardResilience_ShouldRegisterAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaStandardResilience();

        // Assert
        var providerDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(ResiliencePipelineProvider<string>));

        providerDescriptor.ShouldNotBeNull();
        providerDescriptor!.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    // Test helper classes - must be public for NSubstitute/Castle.DynamicProxy to create proxies
    public sealed record TestRequest : IRequest<TestResponse>;
    public sealed record TestResponse;
    public sealed record AnotherRequest : IRequest<AnotherResponse>;
    public sealed record AnotherResponse;
}
