using Encina.AzureFunctions.Durable;
using Encina.Messaging.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Encina.AzureFunctions.ContractTests.Durable;

/// <summary>
/// Contract tests to verify that DI registration follows expected patterns.
/// These tests verify ServiceDescriptor registrations without building ServiceProvider.
/// </summary>
[Trait("Category", "Contract")]
public sealed class DurableServiceCollectionContractTests
{
    [Fact]
    public void Contract_AddEncinaDurableFunctions_RegistersDurableFunctionsOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaDurableFunctions();
        var provider = services.BuildServiceProvider();

        // Assert - Verify options can be resolved
        var options = provider.GetService<IOptions<DurableFunctionsOptions>>();
        options.ShouldNotBeNull();
        options.Value.ShouldNotBeNull();
    }

    [Fact]
    public void Contract_AddEncinaDurableFunctions_RegistersHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaDurableFunctions();

        // Assert - Verify DurableFunctionsHealthCheck is registered as IEncinaHealthCheck
        services.ShouldContain(sd =>
            sd.ServiceType == typeof(IEncinaHealthCheck) &&
            sd.ImplementationType == typeof(DurableFunctionsHealthCheck));
    }

    [Fact]
    public void Contract_AddEncinaDurableFunctions_AllowsConfigureCallback()
    {
        // Arrange
        var services = new ServiceCollection();
        var expectedMaxRetries = 10;

        // Act
        services.AddEncinaDurableFunctions(options =>
        {
            options.DefaultMaxRetries = expectedMaxRetries;
        });
        var provider = services.BuildServiceProvider();

        // Assert - Verify callback was applied
        var options = provider.GetRequiredService<IOptions<DurableFunctionsOptions>>();
        options.Value.DefaultMaxRetries.ShouldBe(expectedMaxRetries);
    }

    [Fact]
    public void Contract_AddEncinaDurableFunctions_ReturnsServicesForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaDurableFunctions();

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void Contract_AddEncinaDurableFunctions_CanBeCalledMultipleTimes()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaDurableFunctions(o => o.DefaultMaxRetries = 3);
        services.AddEncinaDurableFunctions(o => o.DefaultBackoffCoefficient = 3.0);
        var provider = services.BuildServiceProvider();

        // Assert - Both configurations should be applied
        var options = provider.GetRequiredService<IOptions<DurableFunctionsOptions>>();
        options.Value.DefaultMaxRetries.ShouldBe(3);
        options.Value.DefaultBackoffCoefficient.ShouldBe(3.0);

        // Health check should be registered only once (idempotent)
        var healthCheckCount = services.Count(sd =>
            sd.ServiceType == typeof(IEncinaHealthCheck) &&
            sd.ImplementationType == typeof(DurableFunctionsHealthCheck));
        healthCheckCount.ShouldBe(1);
    }

    [Fact]
    public void Contract_DurableFunctionsOptions_HasReasonableDefaults()
    {
        // Arrange & Act - Create options directly to verify defaults
        var options = new DurableFunctionsOptions();

        // Assert - Verify defaults are sensible
        options.DefaultMaxRetries.ShouldBeGreaterThanOrEqualTo(0);
        options.DefaultFirstRetryInterval.ShouldBeGreaterThan(TimeSpan.Zero);
        options.DefaultBackoffCoefficient.ShouldBeGreaterThan(0);
        options.DefaultMaxRetryInterval.ShouldBeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void Contract_HealthCheck_ImplementsIEncinaHealthCheck()
    {
        // Assert - Type-level verification without building ServiceProvider
        typeof(IEncinaHealthCheck).IsAssignableFrom(typeof(DurableFunctionsHealthCheck)).ShouldBeTrue();
    }

    [Fact]
    public void Contract_HealthCheck_IsRegisteredAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaDurableFunctions();

        // Assert - Verify lifetime is Singleton
        var descriptor = services.FirstOrDefault(sd =>
            sd.ServiceType == typeof(IEncinaHealthCheck) &&
            sd.ImplementationType == typeof(DurableFunctionsHealthCheck));

        descriptor.ShouldNotBeNull();
        descriptor!.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }
}
