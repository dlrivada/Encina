using Encina.AzureFunctions.Durable;
using Encina.Messaging.Health;
using Shouldly;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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

        // Assert - Verify IConfigureOptions<DurableFunctionsOptions> is registered
        services.ShouldContain(sd =>
            sd.ServiceType == typeof(IConfigureOptions<DurableFunctionsOptions>));
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
        var callbackInvoked = false;

        // Act
        services.AddEncinaDurableFunctions(options =>
        {
            options.DefaultMaxRetries = expectedMaxRetries;
            callbackInvoked = true;
        });

        // Assert - Verify callback was registered (IConfigureOptions exists)
        services.ShouldContain(sd =>
            sd.ServiceType == typeof(IConfigureOptions<DurableFunctionsOptions>));

        // Verify callback is invoked when options are created
        var configuredOptions = new DurableFunctionsOptions();
        var configureOptions = services
            .Where(sd => sd.ServiceType == typeof(IConfigureOptions<DurableFunctionsOptions>))
            .Select(sd => sd.ImplementationInstance as IConfigureOptions<DurableFunctionsOptions>)
            .FirstOrDefault(c => c is not null);

        configureOptions?.Configure(configuredOptions);
        callbackInvoked.ShouldBeTrue();
        configuredOptions.DefaultMaxRetries.ShouldBe(expectedMaxRetries);
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

        // Assert - Should register multiple IConfigureOptions (all are applied in order)
        var configureOptionsCount = services.Count(sd =>
            sd.ServiceType == typeof(IConfigureOptions<DurableFunctionsOptions>));
        configureOptionsCount.ShouldBeGreaterThanOrEqualTo(2);

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
        typeof(DurableFunctionsHealthCheck).ShouldBeAssignableTo<IEncinaHealthCheck>();
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
