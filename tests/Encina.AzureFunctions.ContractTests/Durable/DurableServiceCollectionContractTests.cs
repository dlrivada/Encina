using Encina.AzureFunctions.Durable;
using Encina.Messaging.Health;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Encina.AzureFunctions.ContractTests.Durable;

/// <summary>
/// Contract tests to verify that DI registration follows expected patterns.
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

        // Assert
        var options = provider.GetService<IOptions<DurableFunctionsOptions>>();
        options.Should().NotBeNull();
        options!.Value.Should().NotBeNull();
    }

    [Fact]
    public void Contract_AddEncinaDurableFunctions_RegistersHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaDurableFunctions();
        var provider = services.BuildServiceProvider();

        // Assert
        var healthChecks = provider.GetServices<IEncinaHealthCheck>();
        healthChecks.Should().Contain(h => h is DurableFunctionsHealthCheck);
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

        // Assert
        var options = provider.GetRequiredService<IOptions<DurableFunctionsOptions>>().Value;
        options.DefaultMaxRetries.Should().Be(expectedMaxRetries);
    }

    [Fact]
    public void Contract_AddEncinaDurableFunctions_ReturnsServicesForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaDurableFunctions();

        // Assert
        result.Should().BeSameAs(services);
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

        // Assert - Should not throw and last configuration wins for Configure<T>
        var options = provider.GetRequiredService<IOptions<DurableFunctionsOptions>>().Value;
        options.Should().NotBeNull();
    }

    [Fact]
    public void Contract_DurableFunctionsOptions_HasReasonableDefaults()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncinaDurableFunctions();
        var provider = services.BuildServiceProvider();

        // Act
        var options = provider.GetRequiredService<IOptions<DurableFunctionsOptions>>().Value;

        // Assert - Verify defaults are sensible
        options.DefaultMaxRetries.Should().BeGreaterThanOrEqualTo(0);
        options.DefaultFirstRetryInterval.Should().BeGreaterThan(TimeSpan.Zero);
        options.DefaultBackoffCoefficient.Should().BeGreaterThan(0);
        options.DefaultMaxRetryInterval.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void Contract_DurableFunctionsHealthCheck_CanBeResolvedViaInterface()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncinaDurableFunctions();
        var provider = services.BuildServiceProvider();

        // Act
        var healthChecks = provider.GetServices<IEncinaHealthCheck>();
        var durableHealthCheck = healthChecks.OfType<DurableFunctionsHealthCheck>().FirstOrDefault();

        // Assert
        durableHealthCheck.Should().NotBeNull();
    }

    [Fact]
    public void Contract_HealthCheck_ImplementsIEncinaHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncinaDurableFunctions();
        var provider = services.BuildServiceProvider();

        // Act
        var healthChecks = provider.GetServices<IEncinaHealthCheck>();
        var durableHealthCheck = healthChecks.OfType<DurableFunctionsHealthCheck>().FirstOrDefault();

        // Assert
        durableHealthCheck.Should().NotBeNull();
        durableHealthCheck.Should().BeAssignableTo<IEncinaHealthCheck>();
    }

    [Fact]
    public void Contract_ConfiguredOptions_AreUsedByHealthCheck()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncinaDurableFunctions(options =>
        {
            options.ProviderHealthCheck.Name = "custom-health-check-name";
        });
        var provider = services.BuildServiceProvider();

        // Act
        var healthChecks = provider.GetServices<IEncinaHealthCheck>();
        var durableHealthCheck = healthChecks.OfType<DurableFunctionsHealthCheck>().First();

        // Assert
        durableHealthCheck.Name.Should().Be("custom-health-check-name");
    }
}
